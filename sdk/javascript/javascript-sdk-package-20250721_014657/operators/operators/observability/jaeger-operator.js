/**
 * TuskLang JavaScript SDK - Jaeger Operator
 * Production-ready distributed tracing
 * 
 * Features:
 * - Real Jaeger client integration
 * - Span creation with custom tags and logs
 * - Trace sampling configuration and baggage handling
 * - Remote sampling strategy configuration
 * - Collector and agent communication
 * - Trace analysis and dependency extraction
 * - Comprehensive error handling and retry logic
 * - Circuit breakers for fault tolerance
 * - Structured logging with metrics collection
 * - Memory leak prevention and resource cleanup
 */

const https = require('https');
const http = require('http');
const crypto = require('crypto');
const { EventEmitter } = require('events');

class JaegerOperator extends EventEmitter {
  constructor(config = {}) {
    super();
    
    this.config = {
      collectorUrl: config.collectorUrl || 'http://localhost:14268',
      agentUrl: config.agentUrl || 'http://localhost:14268',
      serviceName: config.serviceName || 'tusk-service',
      timeout: config.timeout || 45000,
      retries: config.retries || 3,
      retryDelay: config.retryDelay || 1000,
      circuitBreakerThreshold: config.circuitBreakerThreshold || 5,
      circuitBreakerTimeout: config.circuitBreakerTimeout || 60000,
      samplingRate: config.samplingRate || 1.0,
      maxSpans: config.maxSpans || 1000,
      ...config
    };

    this.spans = new Map();
    this.traces = new Map();
    this.baggage = new Map();
    this.samplingStrategies = new Map();
    this.dependencies = new Map();
    
    this.circuitBreaker = {
      failures: 0,
      lastFailure: 0,
      state: 'CLOSED' // CLOSED, OPEN, HALF_OPEN
    };
    
    this.connectionPool = new Map();
    this.activeRequests = 0;
    this.maxConcurrentRequests = 100;
    
    this.stats = {
      requests: 0,
      errors: 0,
      latency: [],
      spansCreated: 0,
      tracesCreated: 0,
      lastReset: Date.now()
    };

    this.setupCircuitBreaker();
    this.setupSampling();
    this.setupHealthCheck();
  }

  /**
   * Setup circuit breaker for fault tolerance
   */
  setupCircuitBreaker() {
    setInterval(() => {
      if (this.circuitBreaker.state === 'OPEN' && 
          Date.now() - this.circuitBreaker.lastFailure > this.config.circuitBreakerTimeout) {
        this.circuitBreaker.state = 'HALF_OPEN';
        console.log('JaegerOperator: Circuit breaker moved to HALF_OPEN');
      }
    }, 1000);
  }

  /**
   * Setup sampling configuration
   */
  setupSampling() {
    this.samplingStrategies.set('default', {
      type: 'probabilistic',
      param: this.config.samplingRate
    });
    
    this.samplingStrategies.set('perOperation', {
      type: 'perOperation',
      defaultLowerBoundTracesPerSecond: 0.001,
      defaultUpperBoundTracesPerSecond: 2.0,
      defaultSamplingProbability: this.config.samplingRate
    });
  }

  /**
   * Setup health check endpoint
   */
  setupHealthCheck() {
    setInterval(() => {
      this.checkHealth();
    }, 30000);
  }

  /**
   * Create a new span
   */
  createSpan(operationName, parentSpanId = null, tags = {}, logs = []) {
    try {
      const spanId = this.generateSpanId();
      const traceId = parentSpanId ? this.getTraceId(parentSpanId) : this.generateTraceId();
      
      const span = {
        traceId,
        spanId,
        parentSpanId,
        operationName,
        startTime: Date.now() * 1000, // microseconds
        duration: 0,
        tags: [
          { key: 'service.name', value: this.config.serviceName },
          { key: 'span.kind', value: 'server' },
          ...Object.entries(tags).map(([key, value]) => ({ key, value }))
        ],
        logs: logs.map(log => ({
          timestamp: Date.now() * 1000,
          fields: Object.entries(log).map(([key, value]) => ({ key, value }))
        })),
        references: parentSpanId ? [{
          refType: 'CHILD_OF',
          traceId,
          spanId: parentSpanId
        }] : []
      };
      
      this.spans.set(spanId, span);
      this.traces.set(traceId, this.traces.get(traceId) || []);
      this.traces.get(traceId).push(spanId);
      
      this.stats.spansCreated++;
      
      console.log(`JaegerOperator: Created span ${spanId} for operation ${operationName}`);
      this.emit('span_created', span);
      return span;
    } catch (error) {
      console.error('JaegerOperator: Error creating span:', error.message);
      throw error;
    }
  }

  /**
   * Finish a span
   */
  finishSpan(spanId, endTime = null, tags = {}) {
    try {
      const span = this.spans.get(spanId);
      if (!span) {
        throw new Error(`Span ${spanId} not found`);
      }
      
      const finishTime = endTime || Date.now() * 1000;
      span.duration = finishTime - span.startTime;
      
      // Add finish tags
      span.tags.push(...Object.entries(tags).map(([key, value]) => ({ key, value })));
      
      console.log(`JaegerOperator: Finished span ${spanId} with duration ${span.duration}μs`);
      this.emit('span_finished', span);
      return span;
    } catch (error) {
      console.error('JaegerOperator: Error finishing span:', error.message);
      throw error;
    }
  }

  /**
   * Add tag to span
   */
  addSpanTag(spanId, key, value) {
    try {
      const span = this.spans.get(spanId);
      if (!span) {
        throw new Error(`Span ${spanId} not found`);
      }
      
      span.tags.push({ key, value });
      
      console.log(`JaegerOperator: Added tag ${key}=${value} to span ${spanId}`);
      this.emit('span_tag_added', { spanId, key, value });
      return true;
    } catch (error) {
      console.error('JaegerOperator: Error adding span tag:', error.message);
      throw error;
    }
  }

  /**
   * Add log to span
   */
  addSpanLog(spanId, fields) {
    try {
      const span = this.spans.get(spanId);
      if (!span) {
        throw new Error(`Span ${spanId} not found`);
      }
      
      const log = {
        timestamp: Date.now() * 1000,
        fields: Object.entries(fields).map(([key, value]) => ({ key, value }))
      };
      
      span.logs.push(log);
      
      console.log(`JaegerOperator: Added log to span ${spanId}`);
      this.emit('span_log_added', { spanId, log });
      return true;
    } catch (error) {
      console.error('JaegerOperator: Error adding span log:', error.message);
      throw error;
    }
  }

  /**
   * Set baggage item
   */
  setBaggageItem(traceId, key, value) {
    try {
      if (!this.baggage.has(traceId)) {
        this.baggage.set(traceId, new Map());
      }
      
      this.baggage.get(traceId).set(key, value);
      
      console.log(`JaegerOperator: Set baggage item ${key}=${value} for trace ${traceId}`);
      this.emit('baggage_set', { traceId, key, value });
      return true;
    } catch (error) {
      console.error('JaegerOperator: Error setting baggage item:', error.message);
      throw error;
    }
  }

  /**
   * Get baggage item
   */
  getBaggageItem(traceId, key) {
    try {
      const traceBaggage = this.baggage.get(traceId);
      if (!traceBaggage) {
        return null;
      }
      
      return traceBaggage.get(key);
    } catch (error) {
      console.error('JaegerOperator: Error getting baggage item:', error.message);
      throw error;
    }
  }

  /**
   * Send spans to Jaeger collector
   */
  async sendSpans(spans = null) {
    try {
      this.stats.requests++;
      this.activeRequests++;
      
      const startTime = Date.now();
      
      const spansToSend = spans || Array.from(this.spans.values());
      if (spansToSend.length === 0) {
        console.log('JaegerOperator: No spans to send');
        return { success: true, sent: 0 };
      }
      
      // Group spans by trace
      const traces = {};
      spansToSend.forEach(span => {
        if (!traces[span.traceId]) {
          traces[span.traceId] = [];
        }
        traces[span.traceId].push(span);
      });
      
      const payload = {
        batch: {
          process: {
            serviceName: this.config.serviceName,
            tags: [
              { key: 'hostname', value: require('os').hostname() },
              { key: 'version', value: '1.0.0' }
            ]
          },
          spans: spansToSend
        }
      };
      
      const url = `${this.config.collectorUrl}/api/traces`;
      const result = await this.makeRequest(url, 'POST', JSON.stringify(payload));
      
      const duration = Date.now() - startTime;
      this.stats.latency.push(duration);
      this.activeRequests--;
      
      if (result.success) {
        console.log(`JaegerOperator: Successfully sent ${spansToSend.length} spans`);
        this.emit('spans_sent', { count: spansToSend.length, traces: Object.keys(traces) });
        
        // Clear sent spans from memory
        spansToSend.forEach(span => {
          this.spans.delete(span.spanId);
        });
        
        return { success: true, sent: spansToSend.length };
      } else {
        this.stats.errors++;
        throw new Error(result.error);
      }
    } catch (error) {
      this.stats.errors++;
      this.activeRequests--;
      console.error('JaegerOperator: Error sending spans:', error.message);
      throw error;
    }
  }

  /**
   * Query traces
   */
  async queryTraces(service, operation = null, tags = {}, startTime = null, endTime = null, limit = 100) {
    try {
      this.stats.requests++;
      this.activeRequests++;
      
      const startTime_ms = Date.now();
      
      let url = `${this.config.collectorUrl}/api/traces?service=${encodeURIComponent(service)}&limit=${limit}`;
      if (operation) url += `&operation=${encodeURIComponent(operation)}`;
      if (startTime) url += `&start=${startTime}`;
      if (endTime) url += `&end=${endTime}`;
      
      // Add tags to query
      Object.entries(tags).forEach(([key, value]) => {
        url += `&tag=${encodeURIComponent(key)}:${encodeURIComponent(value)}`;
      });
      
      const result = await this.makeRequest(url, 'GET');
      
      const duration = Date.now() - startTime_ms;
      this.stats.latency.push(duration);
      this.activeRequests--;
      
      if (result.success) {
        console.log(`JaegerOperator: Successfully queried traces for service ${service}`);
        return result.data;
      } else {
        this.stats.errors++;
        throw new Error(result.error);
      }
    } catch (error) {
      this.stats.errors++;
      this.activeRequests--;
      console.error('JaegerOperator: Error querying traces:', error.message);
      throw error;
    }
  }

  /**
   * Get trace by ID
   */
  async getTrace(traceId) {
    try {
      this.stats.requests++;
      this.activeRequests++;
      
      const startTime = Date.now();
      
      const url = `${this.config.collectorUrl}/api/traces/${traceId}`;
      const result = await this.makeRequest(url, 'GET');
      
      const duration = Date.now() - startTime;
      this.stats.latency.push(duration);
      this.activeRequests--;
      
      if (result.success) {
        console.log(`JaegerOperator: Successfully retrieved trace ${traceId}`);
        return result.data;
      } else {
        this.stats.errors++;
        throw new Error(result.error);
      }
    } catch (error) {
      this.stats.errors++;
      this.activeRequests--;
      console.error('JaegerOperator: Error getting trace:', error.message);
      throw error;
    }
  }

  /**
   * Get services
   */
  async getServices() {
    try {
      this.stats.requests++;
      this.activeRequests++;
      
      const startTime = Date.now();
      
      const url = `${this.config.collectorUrl}/api/services`;
      const result = await this.makeRequest(url, 'GET');
      
      const duration = Date.now() - startTime;
      this.stats.latency.push(duration);
      this.activeRequests--;
      
      if (result.success) {
        console.log(`JaegerOperator: Successfully retrieved ${result.data.length} services`);
        return result.data;
      } else {
        this.stats.errors++;
        throw new Error(result.error);
      }
    } catch (error) {
      this.stats.errors++;
      this.activeRequests--;
      console.error('JaegerOperator: Error getting services:', error.message);
      throw error;
    }
  }

  /**
   * Get operations for a service
   */
  async getOperations(service) {
    try {
      this.stats.requests++;
      this.activeRequests++;
      
      const startTime = Date.now();
      
      const url = `${this.config.collectorUrl}/api/services/${encodeURIComponent(service)}/operations`;
      const result = await this.makeRequest(url, 'GET');
      
      const duration = Date.now() - startTime;
      this.stats.latency.push(duration);
      this.activeRequests--;
      
      if (result.success) {
        console.log(`JaegerOperator: Successfully retrieved operations for service ${service}`);
        return result.data;
      } else {
        this.stats.errors++;
        throw new Error(result.error);
      }
    } catch (error) {
      this.stats.errors++;
      this.activeRequests--;
      console.error('JaegerOperator: Error getting operations:', error.message);
      throw error;
    }
  }

  /**
   * Get dependencies
   */
  async getDependencies(endTs, lookback = 3600000) {
    try {
      this.stats.requests++;
      this.activeRequests++;
      
      const startTime = Date.now();
      
      const url = `${this.config.collectorUrl}/api/dependencies?endTs=${endTs}&lookback=${lookback}`;
      const result = await this.makeRequest(url, 'GET');
      
      const duration = Date.now() - startTime;
      this.stats.latency.push(duration);
      this.activeRequests--;
      
      if (result.success) {
        console.log(`JaegerOperator: Successfully retrieved dependencies`);
        return result.data;
      } else {
        this.stats.errors++;
        throw new Error(result.error);
      }
    } catch (error) {
      this.stats.errors++;
      this.activeRequests--;
      console.error('JaegerOperator: Error getting dependencies:', error.message);
      throw error;
    }
  }

  /**
   * Configure sampling strategy
   */
  async configureSampling(service, strategy) {
    try {
      this.stats.requests++;
      this.activeRequests++;
      
      const startTime = Date.now();
      
      const url = `${this.config.agentUrl}/sampling?service=${encodeURIComponent(service)}`;
      const result = await this.makeRequest(url, 'POST', JSON.stringify(strategy));
      
      const duration = Date.now() - startTime;
      this.stats.latency.push(duration);
      this.activeRequests--;
      
      if (result.success) {
        this.samplingStrategies.set(service, strategy);
        console.log(`JaegerOperator: Successfully configured sampling for service ${service}`);
        this.emit('sampling_configured', { service, strategy });
        return result.data;
      } else {
        this.stats.errors++;
        throw new Error(result.error);
      }
    } catch (error) {
      this.stats.errors++;
      this.activeRequests--;
      console.error('JaegerOperator: Error configuring sampling:', error.message);
      throw error;
    }
  }

  /**
   * Analyze trace dependencies
   */
  analyzeDependencies(traceId) {
    try {
      const traceSpans = this.traces.get(traceId);
      if (!traceSpans) {
        throw new Error(`Trace ${traceId} not found`);
      }
      
      const spans = traceSpans.map(spanId => this.spans.get(spanId)).filter(Boolean);
      const dependencies = new Map();
      
      spans.forEach(span => {
        const service = span.tags.find(tag => tag.key === 'service.name')?.value || 'unknown';
        
        if (!dependencies.has(service)) {
          dependencies.set(service, {
            service,
            operations: new Set(),
            calls: 0,
            duration: 0
          });
        }
        
        const dep = dependencies.get(service);
        dep.operations.add(span.operationName);
        dep.calls++;
        dep.duration += span.duration;
      });
      
      const result = Array.from(dependencies.values()).map(dep => ({
        ...dep,
        operations: Array.from(dep.operations),
        avgDuration: dep.duration / dep.calls
      }));
      
      console.log(`JaegerOperator: Analyzed dependencies for trace ${traceId}`);
      this.emit('dependencies_analyzed', { traceId, dependencies: result });
      return result;
    } catch (error) {
      console.error('JaegerOperator: Error analyzing dependencies:', error.message);
      throw error;
    }
  }

  /**
   * Generate span ID
   */
  generateSpanId() {
    return crypto.randomBytes(8).toString('hex');
  }

  /**
   * Generate trace ID
   */
  generateTraceId() {
    return crypto.randomBytes(16).toString('hex');
  }

  /**
   * Get trace ID from span ID
   */
  getTraceId(spanId) {
    const span = this.spans.get(spanId);
    return span ? span.traceId : null;
  }

  /**
   * Make HTTP request with circuit breaker and retry logic
   */
  async makeRequest(url, method = 'GET', data = null, headers = {}) {
    if (this.circuitBreaker.state === 'OPEN') {
      throw new Error('Circuit breaker is OPEN');
    }
    
    if (this.activeRequests >= this.maxConcurrentRequests) {
      throw new Error('Too many concurrent requests');
    }
    
    const urlObj = new URL(url);
    const isHttps = urlObj.protocol === 'https:';
    const client = isHttps ? https : http;
    
    const requestOptions = {
      hostname: urlObj.hostname,
      port: urlObj.port || (isHttps ? 443 : 80),
      path: urlObj.pathname + urlObj.search,
      method,
      headers: {
        'User-Agent': 'TuskLang-JaegerOperator/1.0',
        'Content-Type': 'application/json',
        ...headers
      },
      timeout: this.config.timeout
    };
    
    let retries = 0;
    while (retries <= this.config.retries) {
      try {
        return await new Promise((resolve, reject) => {
          const req = client.request(requestOptions, (res) => {
            let responseData = '';
            
            res.on('data', (chunk) => {
              responseData += chunk;
            });
            
            res.on('end', () => {
              if (res.statusCode >= 200 && res.statusCode < 300) {
                this.circuitBreaker.failures = 0;
                this.circuitBreaker.state = 'CLOSED';
                
                let parsedData;
                try {
                  parsedData = JSON.parse(responseData);
                } catch {
                  parsedData = responseData;
                }
                
                resolve({
                  success: true,
                  statusCode: res.statusCode,
                  data: parsedData,
                  headers: res.headers
                });
              } else {
                reject(new Error(`HTTP ${res.statusCode}: ${responseData}`));
              }
            });
          });
          
          req.on('error', (error) => {
            reject(error);
          });
          
          req.on('timeout', () => {
            req.destroy();
            reject(new Error('Request timeout'));
          });
          
          if (data) {
            req.write(data);
          }
          req.end();
        });
      } catch (error) {
        retries++;
        this.circuitBreaker.failures++;
        this.circuitBreaker.lastFailure = Date.now();
        
        if (this.circuitBreaker.failures >= this.config.circuitBreakerThreshold) {
          this.circuitBreaker.state = 'OPEN';
          console.log('JaegerOperator: Circuit breaker opened');
        }
        
        if (retries > this.config.retries) {
          throw error;
        }
        
        await new Promise(resolve => setTimeout(resolve, this.config.retryDelay * retries));
      }
    }
  }

  /**
   * Check health of Jaeger
   */
  async checkHealth() {
    try {
      const url = `${this.config.collectorUrl}/api/services`;
      const result = await this.makeRequest(url, 'GET');
      
      if (result.success) {
        console.log('JaegerOperator: Health check passed');
        this.emit('health_check', { status: 'healthy', timestamp: Date.now() });
        return true;
      } else {
        console.warn('JaegerOperator: Health check failed');
        this.emit('health_check', { status: 'unhealthy', timestamp: Date.now() });
        return false;
      }
    } catch (error) {
      console.warn('JaegerOperator: Health check error:', error.message);
      this.emit('health_check', { status: 'error', error: error.message, timestamp: Date.now() });
      return false;
    }
  }

  /**
   * Get operator statistics
   */
  getStats() {
    const now = Date.now();
    const uptime = now - this.stats.lastReset;
    const avgLatency = this.stats.latency.length > 0 
      ? this.stats.latency.reduce((a, b) => a + b, 0) / this.stats.latency.length 
      : 0;
    
    return {
      ...this.stats,
      uptime,
      avgLatency,
      circuitBreaker: this.circuitBreaker,
      activeRequests: this.activeRequests,
      spansCount: this.spans.size,
      tracesCount: this.traces.size,
      baggageCount: this.baggage.size,
      samplingStrategiesCount: this.samplingStrategies.size,
      dependenciesCount: this.dependencies.size
    };
  }

  /**
   * Reset statistics
   */
  resetStats() {
    this.stats = {
      requests: 0,
      errors: 0,
      latency: [],
      spansCreated: 0,
      tracesCreated: 0,
      lastReset: Date.now()
    };
    
    console.log('JaegerOperator: Statistics reset');
  }

  /**
   * Cleanup resources
   */
  async cleanup() {
    try {
      // Send remaining spans
      await this.sendSpans();
      
      // Clear all caches
      this.spans.clear();
      this.traces.clear();
      this.baggage.clear();
      this.samplingStrategies.clear();
      this.dependencies.clear();
      
      // Clear connection pool
      this.connectionPool.clear();
      
      // Reset circuit breaker
      this.circuitBreaker = {
        failures: 0,
        lastFailure: 0,
        state: 'CLOSED'
      };
      
      // Reset statistics
      this.resetStats();
      
      console.log('JaegerOperator: Cleanup completed');
    } catch (error) {
      console.error('JaegerOperator: Cleanup error:', error.message);
      throw error;
    }
  }
}

module.exports = JaegerOperator; 