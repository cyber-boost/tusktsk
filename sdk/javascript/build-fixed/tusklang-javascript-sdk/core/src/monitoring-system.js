/**
 * TuskLang Advanced Monitoring and Observability System
 * Provides comprehensive monitoring, logging, and observability capabilities
 */

const { EventEmitter } = require('events');

class MonitoringSystem {
  constructor(options = {}) {
    this.options = {
      logLevel: options.logLevel || 'info',
      metricsInterval: options.metricsInterval || 60000,
      alertThreshold: options.alertThreshold || 0.9,
      ...options
    };
    
    this.metrics = new Map();
    this.logs = [];
    this.alerts = [];
    this.traces = [];
    this.healthChecks = new Map();
    this.eventEmitter = new EventEmitter();
    this.isRunning = false;
  }

  /**
   * Record a metric
   */
  recordMetric(name, value, tags = {}) {
    const metric = {
      name,
      value,
      tags,
      timestamp: Date.now()
    };

    if (!this.metrics.has(name)) {
      this.metrics.set(name, []);
    }

    this.metrics.get(name).push(metric);

    // Keep only last 1000 metrics per name
    if (this.metrics.get(name).length > 1000) {
      this.metrics.get(name).shift();
    }

    // Check for alerts
    this.checkAlerts(name, value, tags);

    return metric;
  }

  /**
   * Record a log entry
   */
  log(level, message, data = {}) {
    const logEntry = {
      level,
      message,
      data,
      timestamp: new Date().toISOString(),
      traceId: data.traceId || this.generateTraceId()
    };

    this.logs.push(logEntry);

    // Keep only last 10000 logs
    if (this.logs.length > 10000) {
      this.logs.shift();
    }

    // Emit log event
    this.eventEmitter.emit('log', logEntry);

    return logEntry;
  }

  /**
   * Start a trace
   */
  startTrace(name, data = {}) {
    const trace = {
      id: this.generateTraceId(),
      name,
      data,
      startTime: Date.now(),
      spans: [],
      status: 'active'
    };

    this.traces.push(trace);

    // Keep only last 1000 traces
    if (this.traces.length > 1000) {
      this.traces.shift();
    }

    return trace.id;
  }

  /**
   * Add span to trace
   */
  addSpan(traceId, name, data = {}) {
    const trace = this.traces.find(t => t.id === traceId);
    if (!trace) {
      throw new Error(`Trace not found: ${traceId}`);
    }

    const span = {
      name,
      data,
      startTime: Date.now(),
      endTime: null,
      duration: null
    };

    trace.spans.push(span);
    return span;
  }

  /**
   * End span
   */
  endSpan(traceId, spanName) {
    const trace = this.traces.find(t => t.id === traceId);
    if (!trace) {
      throw new Error(`Trace not found: ${traceId}`);
    }

    const span = trace.spans.find(s => s.name === spanName && !s.endTime);
    if (!span) {
      throw new Error(`Span not found: ${spanName}`);
    }

    span.endTime = Date.now();
    span.duration = span.endTime - span.startTime;

    return span;
  }

  /**
   * End trace
   */
  endTrace(traceId, status = 'completed') {
    const trace = this.traces.find(t => t.id === traceId);
    if (!trace) {
      throw new Error(`Trace not found: ${traceId}`);
    }

    trace.endTime = Date.now();
    trace.duration = trace.endTime - trace.startTime;
    trace.status = status;

    return trace;
  }

  /**
   * Add health check
   */
  addHealthCheck(name, check) {
    this.healthChecks.set(name, {
      name,
      check,
      lastCheck: null,
      status: 'unknown',
      lastError: null
    });
  }

  /**
   * Run health checks
   */
  async runHealthChecks() {
    const results = {};

    for (const [name, healthCheck] of this.healthChecks) {
      try {
        const result = await healthCheck.check();
        healthCheck.status = result.healthy ? 'healthy' : 'unhealthy';
        healthCheck.lastCheck = Date.now();
        healthCheck.lastError = null;
        results[name] = healthCheck.status;
      } catch (error) {
        healthCheck.status = 'unhealthy';
        healthCheck.lastCheck = Date.now();
        healthCheck.lastError = error.message;
        results[name] = 'unhealthy';
      }
    }

    return results;
  }

  /**
   * Get metrics summary
   */
  getMetricsSummary(name, duration = 3600000) { // 1 hour default
    const metrics = this.metrics.get(name);
    if (!metrics || metrics.length === 0) {
      return null;
    }

    const now = Date.now();
    const recentMetrics = metrics.filter(m => now - m.timestamp < duration);

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
      duration
    };
  }

  /**
   * Get logs by level
   */
  getLogsByLevel(level, limit = 100) {
    return this.logs
      .filter(log => log.level === level)
      .slice(-limit);
  }

  /**
   * Get logs by time range
   */
  getLogsByTimeRange(startTime, endTime, limit = 100) {
    return this.logs
      .filter(log => {
        const logTime = new Date(log.timestamp).getTime();
        return logTime >= startTime && logTime <= endTime;
      })
      .slice(-limit);
  }

  /**
   * Get trace by ID
   */
  getTrace(traceId) {
    return this.traces.find(t => t.id === traceId);
  }

  /**
   * Get traces by name
   */
  getTracesByName(name, limit = 100) {
    return this.traces
      .filter(t => t.name === name)
      .slice(-limit);
  }

  /**
   * Add alert rule
   */
  addAlertRule(name, condition, action) {
    this.alerts.push({
      name,
      condition,
      action,
      enabled: true,
      lastTriggered: null
    });
  }

  /**
   * Check alerts
   */
  checkAlerts(metricName, value, tags) {
    for (const alert of this.alerts) {
      if (!alert.enabled) continue;

      try {
        if (alert.condition(metricName, value, tags)) {
          alert.lastTriggered = Date.now();
          alert.action(metricName, value, tags);
          
          this.log('alert', `Alert triggered: ${alert.name}`, {
            metricName,
            value,
            tags
          });
        }
      } catch (error) {
        this.log('error', `Alert check failed: ${alert.name}`, { error: error.message });
      }
    }
  }

  /**
   * Get system statistics
   */
  getStats() {
    return {
      isRunning: this.isRunning,
      metrics: this.metrics.size,
      logs: this.logs.length,
      traces: this.traces.length,
      healthChecks: this.healthChecks.size,
      alerts: this.alerts.length
    };
  }

  /**
   * Start monitoring
   */
  async start() {
    if (this.isRunning) {
      throw new Error('Monitoring system is already running');
    }

    this.isRunning = true;
    
    // Start periodic health checks
    this.healthCheckInterval = setInterval(async () => {
      await this.runHealthChecks();
    }, this.options.metricsInterval);

    this.eventEmitter.emit('started');
    console.log('Monitoring system started');
  }

  /**
   * Stop monitoring
   */
  async stop() {
    this.isRunning = false;
    
    if (this.healthCheckInterval) {
      clearInterval(this.healthCheckInterval);
    }

    this.eventEmitter.emit('stopped');
    console.log('Monitoring system stopped');
  }

  generateTraceId() {
    return `trace_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }
}

class PrometheusExporter {
  constructor(monitoringSystem) {
    this.monitoringSystem = monitoringSystem;
  }

  /**
   * Export metrics in Prometheus format
   */
  exportMetrics() {
    let output = '';

    for (const [name, metrics] of this.monitoringSystem.metrics) {
      for (const metric of metrics) {
        const tags = Object.entries(metric.tags)
          .map(([k, v]) => `${k}="${v}"`)
          .join(',');
        
        const tagsStr = tags ? `{${tags}}` : '';
        output += `${name}${tagsStr} ${metric.value} ${metric.timestamp}\n`;
      }
    }

    return output;
  }
}

class LogAggregator {
  constructor(options = {}) {
    this.options = {
      batchSize: options.batchSize || 100,
      flushInterval: options.flushInterval || 5000,
      ...options
    };
    
    this.batch = [];
    this.flushTimer = null;
  }

  /**
   * Add log to batch
   */
  addLog(log) {
    this.batch.push(log);

    if (this.batch.length >= this.options.batchSize) {
      this.flush();
    }
  }

  /**
   * Flush batch
   */
  flush() {
    if (this.batch.length === 0) return;

    // Process batch (in real implementation, send to external service)
    console.log(`Flushing ${this.batch.length} logs`);
    
    this.batch = [];
  }

  /**
   * Start aggregator
   */
  start() {
    this.flushTimer = setInterval(() => {
      this.flush();
    }, this.options.flushInterval);
  }

  /**
   * Stop aggregator
   */
  stop() {
    if (this.flushTimer) {
      clearInterval(this.flushTimer);
      this.flush();
    }
  }
}

module.exports = {
  MonitoringSystem,
  PrometheusExporter,
  LogAggregator
}; 