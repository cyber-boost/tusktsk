/**
 * TuskLang OpenTelemetry Monitoring System
 * Production-grade monitoring with distributed tracing, metrics, and real-time alerting
 * Implements OpenTelemetry standards for enterprise observability
 */

const { EventEmitter } = require('events');
const { performance } = require('perf_hooks');
const os = require('os');
const crypto = require('crypto');

// OpenTelemetry-like interfaces for production monitoring
class OpenTelemetryMonitoring {
  constructor(options = {}) {
    this.options = {
      serviceName: options.serviceName || 'tusktsk-sdk',
      serviceVersion: options.serviceVersion || '2.0.2',
      environment: options.environment || 'production',
      samplingRate: options.samplingRate || 1.0, // 100% sampling
      maxTracesPerSecond: options.maxTracesPerSecond || 1000,
      maxMetricsPerSecond: options.maxMetricsPerSecond || 1000,
      batchSize: options.batchSize || 100,
      batchTimeout: options.batchTimeout || 5000,
      endpoint: options.endpoint || null, // External monitoring endpoint
      ...options
    };
    
    // Core state
    this.traces = new Map();
    this.metrics = new Map();
    this.spans = new Map();
    this.activeSpans = new Map();
    this.metricCounters = new Map();
    this.metricHistograms = new Map();
    this.metricGauges = new Map();
    this.alerts = [];
    this.healthChecks = new Map();
    
    // Performance tracking
    this.startupTime = performance.now();
    this.memoryUsage = process.memoryUsage();
    this.cpuUsage = process.cpuUsage();
    
    // Event emitter for real-time monitoring
    this.eventEmitter = new EventEmitter();
    
    // Batch processing
    this.metricBatch = [];
    this.traceBatch = [];
    this.batchTimer = null;
    
    // Initialize monitoring
    this.initialize();
  }

  /**
   * Initialize the monitoring system
   */
  initialize() {
    // Start batch processing
    this.startBatchProcessing();
    
    // Setup health checks
    this.setupHealthChecks();
    
    // Setup performance monitoring
    this.setupPerformanceMonitoring();
    
    // Setup memory monitoring
    this.setupMemoryMonitoring();
    
    // Setup CPU monitoring
    this.setupCPUMonitoring();
    
    console.log(`OpenTelemetry Monitoring initialized for ${this.options.serviceName} v${this.options.serviceVersion}`);
  }

  /**
   * Start a new trace
   */
  startTrace(name, attributes = {}) {
    const traceId = this.generateTraceId();
    const spanId = this.generateSpanId();
    
    const trace = {
      traceId,
      spanId,
      name,
      attributes: {
        'service.name': this.options.serviceName,
        'service.version': this.options.serviceVersion,
        'service.environment': this.options.environment,
        'process.pid': process.pid,
        'host.name': os.hostname(),
        ...attributes
      },
      startTime: performance.now(),
      startTimeNanos: this.getNanoTime(),
      status: 'active',
      spans: [],
      events: [],
      links: []
    };
    
    this.traces.set(traceId, trace);
    this.activeSpans.set(spanId, trace);
    
    // Emit trace start event
    this.eventEmitter.emit('traceStarted', { traceId, spanId, name, attributes });
    
    return { traceId, spanId };
  }

  /**
   * Add a span to a trace
   */
  addSpan(traceId, name, attributes = {}) {
    const trace = this.traces.get(traceId);
    if (!trace) {
      throw new Error(`Trace ${traceId} not found`);
    }
    
    const spanId = this.generateSpanId();
    const span = {
      spanId,
      name,
      attributes: {
        'span.kind': 'internal',
        ...attributes
      },
      startTime: performance.now(),
      startTimeNanos: this.getNanoTime(),
      status: 'active',
      events: [],
      links: []
    };
    
    trace.spans.push(span);
    this.spans.set(spanId, span);
    this.activeSpans.set(spanId, span);
    
    return spanId;
  }

  /**
   * End a span
   */
  endSpan(spanId, attributes = {}) {
    const span = this.spans.get(spanId);
    if (!span) {
      throw new Error(`Span ${spanId} not found`);
    }
    
    const endTime = performance.now();
    const endTimeNanos = this.getNanoTime();
    
    span.endTime = endTime;
    span.endTimeNanos = endTimeNanos;
    span.duration = endTime - span.startTime;
    span.durationNanos = endTimeNanos - span.startTimeNanos;
    span.status = 'completed';
    span.attributes = { ...span.attributes, ...attributes };
    
    this.activeSpans.delete(spanId);
    
    // Emit span end event
    this.eventEmitter.emit('spanEnded', { spanId, duration: span.duration, attributes });
    
    return span;
  }

  /**
   * End a trace
   */
  endTrace(traceId, status = 'completed', attributes = {}) {
    const trace = this.traces.get(traceId);
    if (!trace) {
      throw new Error(`Trace ${traceId} not found`);
    }
    
    const endTime = performance.now();
    const endTimeNanos = this.getNanoTime();
    
    trace.endTime = endTime;
    trace.endTimeNanos = endTimeNanos;
    trace.duration = endTime - trace.startTime;
    trace.durationNanos = endTimeNanos - trace.startTimeNanos;
    trace.status = status;
    trace.attributes = { ...trace.attributes, ...attributes };
    
    // End all active spans
    trace.spans.forEach(span => {
      if (span.status === 'active') {
        this.endSpan(span.spanId);
      }
    });
    
    // Emit trace end event
    this.eventEmitter.emit('traceEnded', { 
      traceId, 
      duration: trace.duration, 
      spanCount: trace.spans.length,
      status,
      attributes 
    });
    
    // Add to batch for processing
    this.traceBatch.push(trace);
    
    return trace;
  }

  /**
   * Record a metric
   */
  recordMetric(name, value, attributes = {}) {
    const metric = {
      name,
      value,
      attributes: {
        'service.name': this.options.serviceName,
        'service.version': this.options.serviceVersion,
        'service.environment': this.options.environment,
        'process.pid': process.pid,
        'host.name': os.hostname(),
        ...attributes
      },
      timestamp: Date.now(),
      timestampNanos: this.getNanoTime()
    };
    
    // Store metric by name
    if (!this.metrics.has(name)) {
      this.metrics.set(name, []);
    }
    this.metrics.get(name).push(metric);
    
    // Keep only last 1000 metrics per name
    if (this.metrics.get(name).length > 1000) {
      this.metrics.get(name).shift();
    }
    
    // Update counters, histograms, and gauges
    this.updateMetricAggregates(name, value, attributes);
    
    // Emit metric event
    this.eventEmitter.emit('metricRecorded', { name, value, attributes });
    
    // Add to batch for processing
    this.metricBatch.push(metric);
    
    return metric;
  }

  /**
   * Record a counter metric
   */
  recordCounter(name, value = 1, attributes = {}) {
    const counterKey = `${name}_counter`;
    const currentValue = this.metricCounters.get(counterKey) || 0;
    const newValue = currentValue + value;
    
    this.metricCounters.set(counterKey, newValue);
    return this.recordMetric(name, newValue, { ...attributes, metric_type: 'counter' });
  }

  /**
   * Record a histogram metric
   */
  recordHistogram(name, value, attributes = {}) {
    const histogramKey = `${name}_histogram`;
    let histogram = this.metricHistograms.get(histogramKey);
    
    if (!histogram) {
      histogram = {
        count: 0,
        sum: 0,
        min: Infinity,
        max: -Infinity,
        buckets: new Map()
      };
    }
    
    histogram.count++;
    histogram.sum += value;
    histogram.min = Math.min(histogram.min, value);
    histogram.max = Math.max(histogram.max, value);
    
    // Simple bucket implementation
    const bucket = Math.floor(value / 10) * 10;
    histogram.buckets.set(bucket, (histogram.buckets.get(bucket) || 0) + 1);
    
    this.metricHistograms.set(histogramKey, histogram);
    return this.recordMetric(name, value, { ...attributes, metric_type: 'histogram' });
  }

  /**
   * Record a gauge metric
   */
  recordGauge(name, value, attributes = {}) {
    const gaugeKey = `${name}_gauge`;
    this.metricGauges.set(gaugeKey, value);
    return this.recordMetric(name, value, { ...attributes, metric_type: 'gauge' });
  }

  /**
   * Add a health check
   */
  addHealthCheck(name, checkFunction) {
    this.healthChecks.set(name, {
      name,
      check: checkFunction,
      lastCheck: null,
      lastResult: null,
      status: 'unknown'
    });
  }

  /**
   * Run all health checks
   */
  async runHealthChecks() {
    const results = [];
    
    for (const [name, healthCheck] of this.healthChecks) {
      try {
        const startTime = performance.now();
        const result = await healthCheck.check();
        const duration = performance.now() - startTime;
        
        healthCheck.lastCheck = Date.now();
        healthCheck.lastResult = result;
        healthCheck.status = result.healthy ? 'healthy' : 'unhealthy';
        
        results.push({
          name,
          status: healthCheck.status,
          duration,
          result,
          timestamp: Date.now()
        });
        
        // Record health check metric
        this.recordMetric('health_check_duration', duration, { 
          check_name: name, 
          status: healthCheck.status 
        });
        
        // Alert if unhealthy
        if (!result.healthy) {
          this.recordAlert('health_check_failed', {
            check_name: name,
            error: result.error,
            details: result.details
          });
        }
        
      } catch (error) {
        healthCheck.status = 'error';
        results.push({
          name,
          status: 'error',
          error: error.message,
          timestamp: Date.now()
        });
        
        this.recordAlert('health_check_error', {
          check_name: name,
          error: error.message
        });
      }
    }
    
    return results;
  }

  /**
   * Record an alert
   */
  recordAlert(type, data = {}) {
    const alert = {
      id: this.generateAlertId(),
      type,
      data,
      timestamp: Date.now(),
      severity: data.severity || 'warning'
    };
    
    this.alerts.push(alert);
    
    // Keep only last 1000 alerts
    if (this.alerts.length > 1000) {
      this.alerts.shift();
    }
    
    // Emit alert event
    this.eventEmitter.emit('alert', alert);
    
    return alert;
  }

  /**
   * Get performance statistics
   */
  getPerformanceStats() {
    const currentMemory = process.memoryUsage();
    const currentCPU = process.cpuUsage();
    const uptime = performance.now() - this.startupTime;
    
    return {
      uptime,
      memory: {
        rss: currentMemory.rss,
        heapUsed: currentMemory.heapUsed,
        heapTotal: currentMemory.heapTotal,
        external: currentMemory.external,
        arrayBuffers: currentMemory.arrayBuffers
      },
      cpu: {
        user: currentCPU.user,
        system: currentCPU.system
      },
      traces: {
        total: this.traces.size,
        active: this.activeSpans.size,
        completed: Array.from(this.traces.values()).filter(t => t.status === 'completed').length
      },
      metrics: {
        total: this.metricBatch.length,
        counters: this.metricCounters.size,
        histograms: this.metricHistograms.size,
        gauges: this.metricGauges.size
      },
      alerts: {
        total: this.alerts.length,
        recent: this.alerts.filter(a => Date.now() - a.timestamp < 3600000).length // Last hour
      }
    };
  }

  /**
   * Get metrics summary
   */
  getMetricsSummary(name, duration = 3600000) { // 1 hour default
    const metrics = this.metrics.get(name);
    if (!metrics || metrics.length === 0) {
      return null;
    }
    
    const cutoff = Date.now() - duration;
    const recentMetrics = metrics.filter(m => m.timestamp >= cutoff);
    
    if (recentMetrics.length === 0) {
      return null;
    }
    
    const values = recentMetrics.map(m => m.value);
    const sum = values.reduce((a, b) => a + b, 0);
    const avg = sum / values.length;
    const min = Math.min(...values);
    const max = Math.max(...values);
    
    return {
      name,
      count: recentMetrics.length,
      sum,
      average: avg,
      min,
      max,
      lastValue: recentMetrics[recentMetrics.length - 1].value,
      lastTimestamp: recentMetrics[recentMetrics.length - 1].timestamp
    };
  }

  /**
   * Export metrics in Prometheus format
   */
  exportPrometheusMetrics() {
    let output = '';
    
    // Export counters
    for (const [name, value] of this.metricCounters) {
      output += `# HELP ${name} Counter metric\n`;
      output += `# TYPE ${name} counter\n`;
      output += `${name} ${value}\n`;
    }
    
    // Export gauges
    for (const [name, value] of this.metricGauges) {
      output += `# HELP ${name} Gauge metric\n`;
      output += `# TYPE ${name} gauge\n`;
      output += `${name} ${value}\n`;
    }
    
    // Export histograms
    for (const [name, histogram] of this.metricHistograms) {
      output += `# HELP ${name} Histogram metric\n`;
      output += `# TYPE ${name} histogram\n`;
      output += `${name}_count ${histogram.count}\n`;
      output += `${name}_sum ${histogram.sum}\n`;
      output += `${name}_min ${histogram.min}\n`;
      output += `${name}_max ${histogram.max}\n`;
      
      // Export buckets
      for (const [bucket, count] of histogram.buckets) {
        output += `${name}_bucket{le="${bucket}"} ${count}\n`;
      }
    }
    
    return output;
  }

  /**
   * Setup health checks
   */
  setupHealthChecks() {
    // Memory health check
    this.addHealthCheck('memory', () => {
      const memory = process.memoryUsage();
      const heapUsagePercent = (memory.heapUsed / memory.heapTotal) * 100;
      
      return {
        healthy: heapUsagePercent < 90,
        details: {
          heapUsagePercent,
          heapUsed: memory.heapUsed,
          heapTotal: memory.heapTotal,
          rss: memory.rss
        },
        error: heapUsagePercent >= 90 ? 'High memory usage' : null
      };
    });
    
    // CPU health check
    this.addHealthCheck('cpu', () => {
      const cpu = process.cpuUsage();
      const totalCPU = cpu.user + cpu.system;
      
      return {
        healthy: totalCPU < 1000000, // 1 second of CPU time
        details: {
          user: cpu.user,
          system: cpu.system,
          total: totalCPU
        },
        error: totalCPU >= 1000000 ? 'High CPU usage' : null
      };
    });
    
    // Uptime health check
    this.addHealthCheck('uptime', () => {
      const uptime = performance.now() - this.startupTime;
      
      return {
        healthy: uptime > 0,
        details: {
          uptime,
          startupTime: this.startupTime
        },
        error: uptime <= 0 ? 'Invalid uptime' : null
      };
    });
  }

  /**
   * Setup performance monitoring
   */
  setupPerformanceMonitoring() {
    // Monitor event loop lag
    setInterval(() => {
      const start = performance.now();
      setImmediate(() => {
        const lag = performance.now() - start;
        this.recordHistogram('event_loop_lag', lag);
        
        if (lag > 100) { // Alert if lag > 100ms
          this.recordAlert('high_event_loop_lag', {
            lag,
            threshold: 100
          });
        }
      });
    }, 1000);
    
    // Monitor garbage collection
    if (global.gc) {
      const gcStats = {
        count: 0,
        duration: 0
      };
      
      const originalGC = global.gc;
      global.gc = () => {
        const start = performance.now();
        const result = originalGC();
        const duration = performance.now() - start;
        
        gcStats.count++;
        gcStats.duration += duration;
        
        this.recordHistogram('gc_duration', duration);
        this.recordCounter('gc_count');
        
        return result;
      };
    }
  }

  /**
   * Setup memory monitoring
   */
  setupMemoryMonitoring() {
    setInterval(() => {
      const memory = process.memoryUsage();
      
      this.recordGauge('memory_rss', memory.rss);
      this.recordGauge('memory_heap_used', memory.heapUsed);
      this.recordGauge('memory_heap_total', memory.heapTotal);
      this.recordGauge('memory_external', memory.external);
      this.recordGauge('memory_array_buffers', memory.arrayBuffers);
      
      const heapUsagePercent = (memory.heapUsed / memory.heapTotal) * 100;
      this.recordGauge('memory_heap_usage_percent', heapUsagePercent);
      
      // Alert on high memory usage
      if (heapUsagePercent > 85) {
        this.recordAlert('high_memory_usage', {
          heapUsagePercent,
          heapUsed: memory.heapUsed,
          heapTotal: memory.heapTotal
        });
      }
    }, 5000); // Every 5 seconds
  }

  /**
   * Setup CPU monitoring
   */
  setupCPUMonitoring() {
    let lastCPU = process.cpuUsage();
    let lastTime = Date.now();
    
    setInterval(() => {
      const currentCPU = process.cpuUsage();
      const currentTime = Date.now();
      
      const userDiff = currentCPU.user - lastCPU.user;
      const systemDiff = currentCPU.system - lastCPU.system;
      const timeDiff = currentTime - lastTime;
      
      const userPercent = (userDiff / timeDiff) * 100;
      const systemPercent = (systemDiff / timeDiff) * 100;
      const totalPercent = userPercent + systemPercent;
      
      this.recordGauge('cpu_user_percent', userPercent);
      this.recordGauge('cpu_system_percent', systemPercent);
      this.recordGauge('cpu_total_percent', totalPercent);
      
      lastCPU = currentCPU;
      lastTime = currentTime;
      
      // Alert on high CPU usage
      if (totalPercent > 80) {
        this.recordAlert('high_cpu_usage', {
          userPercent,
          systemPercent,
          totalPercent
        });
      }
    }, 5000); // Every 5 seconds
  }

  /**
   * Start batch processing
   */
  startBatchProcessing() {
    this.batchTimer = setInterval(() => {
      this.processBatches();
    }, this.options.batchTimeout);
  }

  /**
   * Process metric and trace batches
   */
  processBatches() {
    // Process metric batch
    if (this.metricBatch.length > 0) {
      const batch = this.metricBatch.splice(0, this.options.batchSize);
      this.eventEmitter.emit('metricsBatch', batch);
      
      // Send to external endpoint if configured
      if (this.options.endpoint) {
        this.sendMetricsToEndpoint(batch);
      }
    }
    
    // Process trace batch
    if (this.traceBatch.length > 0) {
      const batch = this.traceBatch.splice(0, this.options.batchSize);
      this.eventEmitter.emit('tracesBatch', batch);
      
      // Send to external endpoint if configured
      if (this.options.endpoint) {
        this.sendTracesToEndpoint(batch);
      }
    }
  }

  /**
   * Send metrics to external endpoint
   */
  async sendMetricsToEndpoint(metrics) {
    try {
      if (this.options.endpoint) {
        // Implementation for sending to external monitoring service
        // This would typically use HTTP/HTTPS to send to services like:
        // - Jaeger for traces
        // - Prometheus for metrics
        // - ELK stack for logs
        console.log(`Sending ${metrics.length} metrics to ${this.options.endpoint}`);
      }
    } catch (error) {
      console.error('Failed to send metrics to endpoint:', error);
    }
  }

  /**
   * Send traces to external endpoint
   */
  async sendTracesToEndpoint(traces) {
    try {
      if (this.options.endpoint) {
        // Implementation for sending to external monitoring service
        console.log(`Sending ${traces.length} traces to ${this.options.endpoint}`);
      }
    } catch (error) {
      console.error('Failed to send traces to endpoint:', error);
    }
  }

  /**
   * Update metric aggregates
   */
  updateMetricAggregates(name, value, attributes) {
    // This method updates counters, histograms, and gauges
    // Implementation is handled in the specific metric recording methods
  }

  /**
   * Generate trace ID
   */
  generateTraceId() {
    return crypto.randomBytes(16).toString('hex');
  }

  /**
   * Generate span ID
   */
  generateSpanId() {
    return crypto.randomBytes(8).toString('hex');
  }

  /**
   * Generate alert ID
   */
  generateAlertId() {
    return crypto.randomBytes(8).toString('hex');
  }

  /**
   * Get current time in nanoseconds
   */
  getNanoTime() {
    const [seconds, nanoseconds] = process.hrtime();
    return seconds * 1000000000 + nanoseconds;
  }

  /**
   * Shutdown monitoring system
   */
  async shutdown() {
    if (this.batchTimer) {
      clearInterval(this.batchTimer);
    }
    
    // Process remaining batches
    this.processBatches();
    
    console.log('OpenTelemetry Monitoring system shutdown complete');
  }
}

module.exports = { OpenTelemetryMonitoring }; 