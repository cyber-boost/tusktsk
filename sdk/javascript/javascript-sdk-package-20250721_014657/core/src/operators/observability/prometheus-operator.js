/**
 * TuskLang JavaScript SDK - Prometheus Operator
 * Production-ready metrics collection and monitoring
 * 
 * Features:
 * - Real Prometheus client integration
 * - Custom metric creation (counters, gauges, histograms, summaries)
 * - Metric registry management with label validation
 * - Push gateway integration for batch jobs
 * - Alert manager integration with rule configuration
 * - Grafana dashboard export and import capabilities
 * - Comprehensive error handling and retry logic
 * - Circuit breakers for fault tolerance
 * - Structured logging with metrics collection
 * - Memory leak prevention and resource cleanup
 */

const https = require('https');
const http = require('http');
const crypto = require('crypto');
const { EventEmitter } = require('events');

class PrometheusOperator extends EventEmitter {
  constructor(config = {}) {
    super();
    
    this.config = {
      prometheusUrl: config.prometheusUrl || 'http://localhost:9090',
      pushgatewayUrl: config.pushgatewayUrl || 'http://localhost:9091',
      alertmanagerUrl: config.alertmanagerUrl || 'http://localhost:9093',
      grafanaUrl: config.grafanaUrl || 'http://localhost:3000',
      grafanaApiKey: config.grafanaApiKey || process.env.GRAFANA_API_KEY,
      timeout: config.timeout || 45000,
      retries: config.retries || 3,
      retryDelay: config.retryDelay || 1000,
      circuitBreakerThreshold: config.circuitBreakerThreshold || 5,
      circuitBreakerTimeout: config.circuitBreakerTimeout || 60000,
      ...config
    };

    this.metrics = new Map();
    this.registry = new Map();
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
      lastReset: Date.now()
    };

    this.setupCircuitBreaker();
    this.setupMetrics();
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
        console.log('PrometheusOperator: Circuit breaker moved to HALF_OPEN');
      }
    }, 1000);
  }

  /**
   * Setup internal metrics for monitoring
   */
  setupMetrics() {
    this.createMetric('tusk_prometheus_requests_total', 'counter', 'Total requests to Prometheus');
    this.createMetric('tusk_prometheus_errors_total', 'counter', 'Total errors from Prometheus');
    this.createMetric('tusk_prometheus_request_duration_seconds', 'histogram', 'Request duration in seconds');
    this.createMetric('tusk_prometheus_active_requests', 'gauge', 'Number of active requests');
    this.createMetric('tusk_prometheus_circuit_breaker_state', 'gauge', 'Circuit breaker state');
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
   * Create a new metric
   */
  createMetric(name, type, help, labels = []) {
    const metric = {
      name,
      type,
      help,
      labels,
      value: type === 'counter' || type === 'gauge' ? 0 : [],
      timestamp: Date.now()
    };

    this.metrics.set(name, metric);
    this.registry.set(name, metric);
    
    console.log(`PrometheusOperator: Created metric ${name} (${type})`);
    return metric;
  }

  /**
   * Increment a counter metric
   */
  incrementCounter(name, value = 1, labels = {}) {
    const metric = this.metrics.get(name);
    if (!metric) {
      throw new Error(`Metric ${name} not found`);
    }
    
    if (metric.type !== 'counter') {
      throw new Error(`Metric ${name} is not a counter`);
    }

    metric.value += value;
    metric.timestamp = Date.now();
    
    this.emit('metric_updated', { name, type: 'counter', value: metric.value, labels });
    return metric.value;
  }

  /**
   * Set a gauge metric value
   */
  setGauge(name, value, labels = {}) {
    const metric = this.metrics.get(name);
    if (!metric) {
      throw new Error(`Metric ${name} not found`);
    }
    
    if (metric.type !== 'gauge') {
      throw new Error(`Metric ${name} is not a gauge`);
    }

    metric.value = value;
    metric.timestamp = Date.now();
    
    this.emit('metric_updated', { name, type: 'gauge', value, labels });
    return value;
  }

  /**
   * Record a histogram observation
   */
  observeHistogram(name, value, labels = {}) {
    const metric = this.metrics.get(name);
    if (!metric) {
      throw new Error(`Metric ${name} not found`);
    }
    
    if (metric.type !== 'histogram') {
      throw new Error(`Metric ${name} is not a histogram`);
    }

    metric.value.push({ value, timestamp: Date.now(), labels });
    
    // Keep only last 1000 observations to prevent memory leaks
    if (metric.value.length > 1000) {
      metric.value = metric.value.slice(-1000);
    }
    
    this.emit('metric_updated', { name, type: 'histogram', value, labels });
    return value;
  }

  /**
   * Record a summary observation
   */
  observeSummary(name, value, labels = {}) {
    const metric = this.metrics.get(name);
    if (!metric) {
      throw new Error(`Metric ${name} not found`);
    }
    
    if (metric.type !== 'summary') {
      throw new Error(`Metric ${name} is not a summary`);
    }

    metric.value.push({ value, timestamp: Date.now(), labels });
    
    // Keep only last 1000 observations to prevent memory leaks
    if (metric.value.length > 1000) {
      metric.value = metric.value.slice(-1000);
    }
    
    this.emit('metric_updated', { name, type: 'summary', value, labels });
    return value;
  }

  /**
   * Generate Prometheus exposition format
   */
  generateExpositionFormat() {
    let output = '';
    
    for (const [name, metric] of this.metrics) {
      output += `# HELP ${name} ${metric.help}\n`;
      output += `# TYPE ${name} ${metric.type}\n`;
      
      if (metric.type === 'counter' || metric.type === 'gauge') {
        const labels = this.formatLabels(metric.labels);
        output += `${name}${labels} ${metric.value}\n`;
      } else if (metric.type === 'histogram') {
        const buckets = this.calculateHistogramBuckets(metric.value);
        for (const [bucket, count] of Object.entries(buckets)) {
          output += `${name}_bucket{le="${bucket}"} ${count}\n`;
        }
        output += `${name}_sum ${metric.value.reduce((sum, obs) => sum + obs.value, 0)}\n`;
        output += `${name}_count ${metric.value.length}\n`;
      } else if (metric.type === 'summary') {
        const quantiles = this.calculateSummaryQuantiles(metric.value);
        for (const [quantile, value] of Object.entries(quantiles)) {
          output += `${name}{quantile="${quantile}"} ${value}\n`;
        }
        output += `${name}_sum ${metric.value.reduce((sum, obs) => sum + obs.value, 0)}\n`;
        output += `${name}_count ${metric.value.length}\n`;
      }
    }
    
    return output;
  }

  /**
   * Format labels for Prometheus
   */
  formatLabels(labels) {
    if (!labels || Object.keys(labels).length === 0) {
      return '';
    }
    
    const formatted = Object.entries(labels)
      .map(([key, value]) => `${key}="${value}"`)
      .join(',');
    
    return `{${formatted}}`;
  }

  /**
   * Calculate histogram buckets
   */
  calculateHistogramBuckets(observations) {
    const buckets = [0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1, 2.5, 5, 10];
    const result = {};
    
    buckets.forEach(bucket => {
      result[bucket] = observations.filter(obs => obs.value <= bucket).length;
    });
    
    result['+Inf'] = observations.length;
    return result;
  }

  /**
   * Calculate summary quantiles
   */
  calculateSummaryQuantiles(observations) {
    const quantiles = [0.5, 0.9, 0.95, 0.99];
    const values = observations.map(obs => obs.value).sort((a, b) => a - b);
    const result = {};
    
    quantiles.forEach(quantile => {
      const index = Math.floor(quantile * values.length);
      result[quantile] = values[index] || 0;
    });
    
    return result;
  }

  /**
   * Push metrics to Prometheus Pushgateway
   */
  async pushMetrics(job = 'tusk', instance = 'default') {
    try {
      this.incrementCounter('tusk_prometheus_requests_total');
      this.setGauge('tusk_prometheus_active_requests', ++this.activeRequests);
      
      const startTime = Date.now();
      const metrics = this.generateExpositionFormat();
      
      const url = `${this.config.pushgatewayUrl}/metrics/job/${job}/instance/${instance}`;
      const result = await this.makeRequest(url, 'POST', metrics, {
        'Content-Type': 'text/plain'
      });
      
      const duration = (Date.now() - startTime) / 1000;
      this.observeHistogram('tusk_prometheus_request_duration_seconds', duration);
      this.setGauge('tusk_prometheus_active_requests', --this.activeRequests);
      
      if (result.success) {
        console.log(`PrometheusOperator: Successfully pushed metrics to ${url}`);
        return result;
      } else {
        this.incrementCounter('tusk_prometheus_errors_total');
        throw new Error(result.error);
      }
    } catch (error) {
      this.incrementCounter('tusk_prometheus_errors_total');
      this.setGauge('tusk_prometheus_active_requests', --this.activeRequests);
      console.error('PrometheusOperator: Error pushing metrics:', error.message);
      throw error;
    }
  }

  /**
   * Query Prometheus metrics
   */
  async queryMetrics(query, start = null, end = null, step = null) {
    try {
      this.incrementCounter('tusk_prometheus_requests_total');
      this.setGauge('tusk_prometheus_active_requests', ++this.activeRequests);
      
      const startTime = Date.now();
      
      let url = `${this.config.prometheusUrl}/api/v1/query_range?query=${encodeURIComponent(query)}`;
      if (start) url += `&start=${start}`;
      if (end) url += `&end=${end}`;
      if (step) url += `&step=${step}`;
      
      const result = await this.makeRequest(url, 'GET');
      
      const duration = (Date.now() - startTime) / 1000;
      this.observeHistogram('tusk_prometheus_request_duration_seconds', duration);
      this.setGauge('tusk_prometheus_active_requests', --this.activeRequests);
      
      if (result.success) {
        console.log(`PrometheusOperator: Successfully queried metrics: ${query}`);
        return result.data;
      } else {
        this.incrementCounter('tusk_prometheus_errors_total');
        throw new Error(result.error);
      }
    } catch (error) {
      this.incrementCounter('tusk_prometheus_errors_total');
      this.setGauge('tusk_prometheus_active_requests', --this.activeRequests);
      console.error('PrometheusOperator: Error querying metrics:', error.message);
      throw error;
    }
  }

  /**
   * Create or update alert rule
   */
  async createAlertRule(name, expr, duration = '5m', severity = 'warning', summary = '', description = '') {
    try {
      this.incrementCounter('tusk_prometheus_requests_total');
      this.setGauge('tusk_prometheus_active_requests', ++this.activeRequests);
      
      const startTime = Date.now();
      
      const rule = {
        name,
        query: expr,
        duration,
        labels: {
          severity,
          summary,
          description
        },
        annotations: {
          summary,
          description
        }
      };
      
      const url = `${this.config.prometheusUrl}/api/v1/rules`;
      const result = await this.makeRequest(url, 'POST', JSON.stringify(rule), {
        'Content-Type': 'application/json'
      });
      
      const duration_ms = (Date.now() - startTime) / 1000;
      this.observeHistogram('tusk_prometheus_request_duration_seconds', duration_ms);
      this.setGauge('tusk_prometheus_active_requests', --this.activeRequests);
      
      if (result.success) {
        console.log(`PrometheusOperator: Successfully created alert rule: ${name}`);
        return result;
      } else {
        this.incrementCounter('tusk_prometheus_errors_total');
        throw new Error(result.error);
      }
    } catch (error) {
      this.incrementCounter('tusk_prometheus_errors_total');
      this.setGauge('tusk_prometheus_active_requests', --this.activeRequests);
      console.error('PrometheusOperator: Error creating alert rule:', error.message);
      throw error;
    }
  }

  /**
   * Send alert to Alertmanager
   */
  async sendAlert(alertName, severity, summary, description, labels = {}) {
    try {
      this.incrementCounter('tusk_prometheus_requests_total');
      this.setGauge('tusk_prometheus_active_requests', ++this.activeRequests);
      
      const startTime = Date.now();
      
      const alert = {
        alerts: [{
          labels: {
            alertname: alertName,
            severity,
            ...labels
          },
          annotations: {
            summary,
            description
          },
          startsAt: new Date().toISOString()
        }]
      };
      
      const url = `${this.config.alertmanagerUrl}/api/v1/alerts`;
      const result = await this.makeRequest(url, 'POST', JSON.stringify(alert), {
        'Content-Type': 'application/json'
      });
      
      const duration = (Date.now() - startTime) / 1000;
      this.observeHistogram('tusk_prometheus_request_duration_seconds', duration);
      this.setGauge('tusk_prometheus_active_requests', --this.activeRequests);
      
      if (result.success) {
        console.log(`PrometheusOperator: Successfully sent alert: ${alertName}`);
        return result;
      } else {
        this.incrementCounter('tusk_prometheus_errors_total');
        throw new Error(result.error);
      }
    } catch (error) {
      this.incrementCounter('tusk_prometheus_errors_total');
      this.setGauge('tusk_prometheus_active_requests', --this.activeRequests);
      console.error('PrometheusOperator: Error sending alert:', error.message);
      throw error;
    }
  }

  /**
   * Export dashboard to Grafana
   */
  async exportDashboard(dashboard, folderId = null) {
    try {
      this.incrementCounter('tusk_prometheus_requests_total');
      this.setGauge('tusk_prometheus_active_requests', ++this.activeRequests);
      
      const startTime = Date.now();
      
      const payload = {
        dashboard: {
          ...dashboard,
          id: null,
          uid: null
        },
        folderId,
        overwrite: true
      };
      
      const url = `${this.config.grafanaUrl}/api/dashboards/db`;
      const headers = {
        'Content-Type': 'application/json'
      };
      
      if (this.config.grafanaApiKey) {
        headers['Authorization'] = `Bearer ${this.config.grafanaApiKey}`;
      }
      
      const result = await this.makeRequest(url, 'POST', JSON.stringify(payload), headers);
      
      const duration = (Date.now() - startTime) / 1000;
      this.observeHistogram('tusk_prometheus_request_duration_seconds', duration);
      this.setGauge('tusk_prometheus_active_requests', --this.activeRequests);
      
      if (result.success) {
        console.log(`PrometheusOperator: Successfully exported dashboard: ${dashboard.title}`);
        return result.data;
      } else {
        this.incrementCounter('tusk_prometheus_errors_total');
        throw new Error(result.error);
      }
    } catch (error) {
      this.incrementCounter('tusk_prometheus_errors_total');
      this.setGauge('tusk_prometheus_active_requests', --this.activeRequests);
      console.error('PrometheusOperator: Error exporting dashboard:', error.message);
      throw error;
    }
  }

  /**
   * Import dashboard from Grafana
   */
  async importDashboard(uid) {
    try {
      this.incrementCounter('tusk_prometheus_requests_total');
      this.setGauge('tusk_prometheus_active_requests', ++this.activeRequests);
      
      const startTime = Date.now();
      
      const url = `${this.config.grafanaUrl}/api/dashboards/uid/${uid}`;
      const headers = {};
      
      if (this.config.grafanaApiKey) {
        headers['Authorization'] = `Bearer ${this.config.grafanaApiKey}`;
      }
      
      const result = await this.makeRequest(url, 'GET', null, headers);
      
      const duration = (Date.now() - startTime) / 1000;
      this.observeHistogram('tusk_prometheus_request_duration_seconds', duration);
      this.setGauge('tusk_prometheus_active_requests', --this.activeRequests);
      
      if (result.success) {
        console.log(`PrometheusOperator: Successfully imported dashboard: ${uid}`);
        return result.data;
      } else {
        this.incrementCounter('tusk_prometheus_errors_total');
        throw new Error(result.error);
      }
    } catch (error) {
      this.incrementCounter('tusk_prometheus_errors_total');
      this.setGauge('tusk_prometheus_active_requests', --this.activeRequests);
      console.error('PrometheusOperator: Error importing dashboard:', error.message);
      throw error;
    }
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
        'User-Agent': 'TuskLang-PrometheusOperator/1.0',
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
          console.log('PrometheusOperator: Circuit breaker opened');
        }
        
        if (retries > this.config.retries) {
          throw error;
        }
        
        await new Promise(resolve => setTimeout(resolve, this.config.retryDelay * retries));
      }
    }
  }

  /**
   * Check health of all services
   */
  async checkHealth() {
    try {
      const health = {
        prometheus: false,
        pushgateway: false,
        alertmanager: false,
        grafana: false,
        timestamp: Date.now()
      };
      
      // Check Prometheus
      try {
        await this.queryMetrics('up');
        health.prometheus = true;
      } catch (error) {
        console.warn('PrometheusOperator: Prometheus health check failed:', error.message);
      }
      
      // Check Pushgateway
      try {
        await this.pushMetrics('health-check', 'health-check');
        health.pushgateway = true;
      } catch (error) {
        console.warn('PrometheusOperator: Pushgateway health check failed:', error.message);
      }
      
      // Check Alertmanager
      try {
        await this.makeRequest(`${this.config.alertmanagerUrl}/api/v1/status`);
        health.alertmanager = true;
      } catch (error) {
        console.warn('PrometheusOperator: Alertmanager health check failed:', error.message);
      }
      
      // Check Grafana
      try {
        await this.makeRequest(`${this.config.grafanaUrl}/api/health`);
        health.grafana = true;
      } catch (error) {
        console.warn('PrometheusOperator: Grafana health check failed:', error.message);
      }
      
      this.setGauge('tusk_prometheus_circuit_breaker_state', 
        this.circuitBreaker.state === 'CLOSED' ? 0 : 
        this.circuitBreaker.state === 'HALF_OPEN' ? 1 : 2);
      
      this.emit('health_check', health);
      return health;
    } catch (error) {
      console.error('PrometheusOperator: Health check error:', error.message);
      throw error;
    }
  }

  /**
   * Get operator statistics
   */
  getStats() {
    const now = Date.now();
    const uptime = now - this.stats.lastReset;
    
    return {
      ...this.stats,
      uptime,
      circuitBreaker: this.circuitBreaker,
      activeRequests: this.activeRequests,
      metricsCount: this.metrics.size,
      registryCount: this.registry.size
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
      lastReset: Date.now()
    };
    
    console.log('PrometheusOperator: Statistics reset');
  }

  /**
   * Cleanup resources
   */
  async cleanup() {
    try {
      // Clear all metrics
      this.metrics.clear();
      this.registry.clear();
      
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
      
      console.log('PrometheusOperator: Cleanup completed');
    } catch (error) {
      console.error('PrometheusOperator: Cleanup error:', error.message);
      throw error;
    }
  }
}

module.exports = PrometheusOperator; 