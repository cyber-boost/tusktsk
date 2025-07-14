# TuskLang JavaScript Documentation: #monitoring Directive

## Overview

The `#monitoring` directive in TuskLang defines monitoring configurations and metrics collection, enabling declarative application monitoring with JavaScript/Node.js integration.

## TuskLang Syntax

```tsk
#monitoring prometheus
  port: 9090
  path: /metrics
  collect_default: true
  custom_metrics:
    - name: http_requests_total
      type: counter
      help: "Total HTTP requests"
    - name: http_request_duration_seconds
      type: histogram
      help: "HTTP request duration"

#monitoring datadog
  api_key: ${DATADOG_API_KEY}
  app_key: ${DATADOG_APP_KEY}
  host: ${HOSTNAME}
  tags: ["env:production", "service:api"]
  flush_interval: 10000

#monitoring newrelic
  license_key: ${NEWRELIC_LICENSE_KEY}
  app_name: ${NEWRELIC_APP_NAME}
  distributed_tracing: true
  transaction_tracer: true

#monitoring custom
  endpoint: http://localhost:8080/metrics
  format: json
  interval: 5000
  batch_size: 100
```

## JavaScript Integration

### Prometheus Monitoring Handler

```javascript
// prometheus-monitoring-handler.js
const prometheus = require('prom-client');

class PrometheusMonitoringHandler {
  constructor(config) {
    this.config = config;
    this.port = config.port || 9090;
    this.path = config.path || '/metrics';
    this.collectDefault = config.collect_default !== false;
    this.metrics = new Map();
    
    this.initializeMetrics();
  }

  async connect() {
    if (this.collectDefault) {
      prometheus.collectDefaultMetrics();
    }
    
    console.log(`Prometheus monitoring started on port ${this.port}`);
  }

  initializeMetrics() {
    if (this.config.custom_metrics) {
      this.config.custom_metrics.forEach(metricConfig => {
        this.createMetric(metricConfig);
      });
    }
  }

  createMetric(config) {
    let metric;
    
    switch (config.type) {
      case 'counter':
        metric = new prometheus.Counter({
          name: config.name,
          help: config.help,
          labelNames: config.labels || []
        });
        break;
        
      case 'gauge':
        metric = new prometheus.Gauge({
          name: config.name,
          help: config.help,
          labelNames: config.labels || []
        });
        break;
        
      case 'histogram':
        metric = new prometheus.Histogram({
          name: config.name,
          help: config.help,
          labelNames: config.labels || [],
          buckets: config.buckets || [0.1, 0.5, 1, 2, 5]
        });
        break;
        
      case 'summary':
        metric = new prometheus.Summary({
          name: config.name,
          help: config.help,
          labelNames: config.labels || [],
          percentiles: config.percentiles || [0.5, 0.9, 0.99]
        });
        break;
    }
    
    if (metric) {
      this.metrics.set(config.name, metric);
    }
  }

  getMetric(name) {
    return this.metrics.get(name);
  }

  increment(name, labels = {}) {
    const metric = this.getMetric(name);
    if (metric && metric instanceof prometheus.Counter) {
      metric.inc(labels);
    }
  }

  set(name, value, labels = {}) {
    const metric = this.getMetric(name);
    if (metric && metric instanceof prometheus.Gauge) {
      metric.set(labels, value);
    }
  }

  observe(name, value, labels = {}) {
    const metric = this.getMetric(name);
    if (metric && (metric instanceof prometheus.Histogram || metric instanceof prometheus.Summary)) {
      metric.observe(labels, value);
    }
  }

  createMiddleware() {
    return (req, res, next) => {
      const start = Date.now();
      
      res.on('finish', () => {
        const duration = (Date.now() - start) / 1000;
        
        this.increment('http_requests_total', {
          method: req.method,
          status: res.statusCode,
          path: req.path
        });
        
        this.observe('http_request_duration_seconds', duration, {
          method: req.method,
          path: req.path
        });
      });
      
      next();
    };
  }

  async getMetrics() {
    return await prometheus.register.metrics();
  }

  createServer() {
    const express = require('express');
    const app = express();
    
    app.get(this.path, async (req, res) => {
      res.set('Content-Type', prometheus.register.contentType);
      res.end(await this.getMetrics());
    });
    
    return app.listen(this.port);
  }
}

module.exports = PrometheusMonitoringHandler;
```

### Datadog Monitoring Handler

```javascript
// datadog-monitoring-handler.js
const StatsD = require('hot-shots');

class DatadogMonitoringHandler {
  constructor(config) {
    this.config = config;
    this.client = new StatsD({
      host: config.host || 'localhost',
      port: config.port || 8125,
      prefix: config.prefix || 'app.',
      suffix: config.suffix || '',
      globalize: false,
      cacheDns: true,
      mock: false,
      globalTags: config.tags || []
    });
  }

  async connect() {
    console.log('Datadog monitoring connected');
  }

  increment(metric, value = 1, tags = []) {
    this.client.increment(metric, value, tags);
  }

  gauge(metric, value, tags = []) {
    this.client.gauge(metric, value, tags);
  }

  histogram(metric, value, tags = []) {
    this.client.histogram(metric, value, tags);
  }

  timing(metric, value, tags = []) {
    this.client.timing(metric, value, tags);
  }

  createMiddleware() {
    return (req, res, next) => {
      const start = Date.now();
      
      res.on('finish', () => {
        const duration = Date.now() - start;
        
        this.increment('http.requests', 1, [
          `method:${req.method}`,
          `status:${res.statusCode}`,
          `path:${req.path}`
        ]);
        
        this.timing('http.request.duration', duration, [
          `method:${req.method}`,
          `path:${req.path}`
        ]);
      });
      
      next();
    };
  }

  async disconnect() {
    this.client.close();
  }
}

module.exports = DatadogMonitoringHandler;
```

### New Relic Monitoring Handler

```javascript
// newrelic-monitoring-handler.js
const newrelic = require('newrelic');

class NewRelicMonitoringHandler {
  constructor(config) {
    this.config = config;
    this.licenseKey = config.license_key;
    this.appName = config.app_name;
    this.distributedTracing = config.distributed_tracing || false;
    this.transactionTracer = config.transaction_tracer || false;
  }

  async connect() {
    // New Relic is configured via environment variables
    process.env.NEW_RELIC_LICENSE_KEY = this.licenseKey;
    process.env.NEW_RELIC_APP_NAME = this.appName;
    process.env.NEW_RELIC_DISTRIBUTED_TRACING_ENABLED = this.distributedTracing;
    process.env.NEW_RELIC_TRANSACTION_TRACER_ENABLED = this.transactionTracer;
    
    console.log('New Relic monitoring initialized');
  }

  recordMetric(name, value) {
    newrelic.recordMetric(name, value);
  }

  recordCustomEvent(eventType, attributes) {
    newrelic.recordCustomEvent(eventType, attributes);
  }

  addCustomAttribute(key, value) {
    newrelic.addCustomAttribute(key, value);
  }

  addCustomAttributes(attributes) {
    newrelic.addCustomAttributes(attributes);
  }

  setTransactionName(name) {
    newrelic.setTransactionName(name);
  }

  createMiddleware() {
    return (req, res, next) => {
      newrelic.setTransactionName(`${req.method} ${req.path}`);
      
      newrelic.addCustomAttributes({
        method: req.method,
        path: req.path,
        userAgent: req.get('User-Agent'),
        ip: req.ip
      });
      
      next();
    };
  }
}

module.exports = NewRelicMonitoringHandler;
```

### Custom Monitoring Handler

```javascript
// custom-monitoring-handler.js
const axios = require('axios');

class CustomMonitoringHandler {
  constructor(config) {
    this.config = config;
    this.endpoint = config.endpoint;
    this.format = config.format || 'json';
    this.interval = config.interval || 5000;
    this.batchSize = config.batch_size || 100;
    this.metrics = [];
    this.timer = null;
  }

  async connect() {
    this.startBatchTimer();
    console.log('Custom monitoring started');
  }

  recordMetric(name, value, tags = {}) {
    this.metrics.push({
      name,
      value,
      tags,
      timestamp: Date.now()
    });

    if (this.metrics.length >= this.batchSize) {
      this.flush();
    }
  }

  async flush() {
    if (this.metrics.length === 0) return;

    const batch = [...this.metrics];
    this.metrics = [];

    try {
      await axios.post(this.endpoint, {
        format: this.format,
        metrics: batch
      });
    } catch (error) {
      console.error('Error sending metrics:', error);
      // Re-add metrics to queue
      this.metrics.unshift(...batch);
    }
  }

  startBatchTimer() {
    this.timer = setInterval(() => {
      this.flush();
    }, this.interval);
  }

  createMiddleware() {
    return (req, res, next) => {
      const start = Date.now();
      
      res.on('finish', () => {
        const duration = Date.now() - start;
        
        this.recordMetric('http_requests_total', 1, {
          method: req.method,
          status: res.statusCode,
          path: req.path
        });
        
        this.recordMetric('http_request_duration_ms', duration, {
          method: req.method,
          path: req.path
        });
      });
      
      next();
    };
  }

  async disconnect() {
    if (this.timer) {
      clearInterval(this.timer);
      this.timer = null;
    }
    
    await this.flush();
  }
}

module.exports = CustomMonitoringHandler;
```

## TypeScript Implementation

```typescript
// monitoring-handler.types.ts
export interface MonitoringConfig {
  port?: number;
  path?: string;
  collect_default?: boolean;
  custom_metrics?: MetricConfig[];
  api_key?: string;
  app_key?: string;
  host?: string;
  tags?: string[];
  flush_interval?: number;
  license_key?: string;
  app_name?: string;
  distributed_tracing?: boolean;
  transaction_tracer?: boolean;
  endpoint?: string;
  format?: string;
  interval?: number;
  batch_size?: number;
}

export interface MetricConfig {
  name: string;
  type: 'counter' | 'gauge' | 'histogram' | 'summary';
  help: string;
  labels?: string[];
  buckets?: number[];
  percentiles?: number[];
}

export interface MonitoringHandler {
  connect(): Promise<void>;
  recordMetric(name: string, value: number, tags?: any): void;
  createMiddleware(): any;
  disconnect(): Promise<void>;
}

// monitoring-handler.ts
import { MonitoringConfig, MonitoringHandler, MetricConfig } from './monitoring-handler.types';

export class TypeScriptMonitoringHandler implements MonitoringHandler {
  protected config: MonitoringConfig;

  constructor(config: MonitoringConfig) {
    this.config = config;
  }

  async connect(): Promise<void> {
    throw new Error('Method not implemented');
  }

  recordMetric(name: string, value: number, tags: any = {}): void {
    throw new Error('Method not implemented');
  }

  createMiddleware(): any {
    throw new Error('Method not implemented');
  }

  async disconnect(): Promise<void> {
    throw new Error('Method not implemented');
  }
}

export class TypeScriptPrometheusHandler extends TypeScriptMonitoringHandler {
  private metrics: Map<string, any> = new Map();
  private port: number;
  private path: string;

  constructor(config: MonitoringConfig) {
    super(config);
    this.port = config.port || 9090;
    this.path = config.path || '/metrics';
  }

  async connect(): Promise<void> {
    const prometheus = require('prom-client');
    
    if (this.config.collect_default !== false) {
      prometheus.collectDefaultMetrics();
    }
    
    console.log(`Prometheus monitoring started on port ${this.port}`);
  }

  recordMetric(name: string, value: number, tags: any = {}): void {
    const metric = this.metrics.get(name);
    if (metric) {
      if (metric instanceof require('prom-client').Counter) {
        metric.inc(tags);
      } else if (metric instanceof require('prom-client').Gauge) {
        metric.set(tags, value);
      } else if (metric instanceof require('prom-client').Histogram) {
        metric.observe(tags, value);
      }
    }
  }

  createMiddleware(): any {
    return (req: any, res: any, next: any) => {
      const start = Date.now();
      
      res.on('finish', () => {
        const duration = (Date.now() - start) / 1000;
        
        this.recordMetric('http_requests_total', 1, {
          method: req.method,
          status: res.statusCode,
          path: req.path
        });
        
        this.recordMetric('http_request_duration_seconds', duration, {
          method: req.method,
          path: req.path
        });
      });
      
      next();
    };
  }

  async disconnect(): Promise<void> {
    // Prometheus doesn't need explicit disconnection
  }
}
```

## Advanced Usage Scenarios

### Multi-Platform Monitoring

```javascript
// multi-platform-monitoring.js
class MultiPlatformMonitoring {
  constructor(configs) {
    this.handlers = new Map();
    this.initializeHandlers(configs);
  }

  initializeHandlers(configs) {
    if (configs.prometheus) {
      const PrometheusHandler = require('./prometheus-monitoring-handler');
      this.handlers.set('prometheus', new PrometheusHandler(configs.prometheus));
    }

    if (configs.datadog) {
      const DatadogHandler = require('./datadog-monitoring-handler');
      this.handlers.set('datadog', new DatadogHandler(configs.datadog));
    }

    if (configs.newrelic) {
      const NewRelicHandler = require('./newrelic-monitoring-handler');
      this.handlers.set('newrelic', new NewRelicHandler(configs.newrelic));
    }
  }

  async connect() {
    for (const [name, handler] of this.handlers.entries()) {
      await handler.connect();
    }
  }

  recordMetric(name, value, tags = {}) {
    for (const handler of this.handlers.values()) {
      handler.recordMetric(name, value, tags);
    }
  }

  createMiddleware() {
    const middlewares = [];
    
    for (const handler of this.handlers.values()) {
      middlewares.push(handler.createMiddleware());
    }
    
    return middlewares;
  }

  async disconnect() {
    for (const handler of this.handlers.values()) {
      await handler.disconnect();
    }
  }
}
```

### Business Metrics Tracking

```javascript
// business-metrics.js
class BusinessMetrics {
  constructor(monitoringHandler) {
    this.monitoring = monitoringHandler;
  }

  trackUserRegistration(userId, source) {
    this.monitoring.recordMetric('user_registrations_total', 1, {
      source,
      userId
    });
  }

  trackPurchase(amount, currency, productId) {
    this.monitoring.recordMetric('purchases_total', 1, {
      currency,
      productId
    });
    
    this.monitoring.recordMetric('purchase_amount_total', amount, {
      currency,
      productId
    });
  }

  trackPageView(page, userId) {
    this.monitoring.recordMetric('page_views_total', 1, {
      page,
      userId: userId || 'anonymous'
    });
  }

  trackError(error, context) {
    this.monitoring.recordMetric('errors_total', 1, {
      error: error.name,
      context
    });
  }
}
```

## Real-World Examples

### Express.js Monitoring Setup

```javascript
// express-monitoring-setup.js
const express = require('express');
const MultiPlatformMonitoring = require('./multi-platform-monitoring');

class ExpressMonitoringSetup {
  constructor(app, config) {
    this.app = app;
    this.monitoring = new MultiPlatformMonitoring(config);
  }

  async setupMonitoring() {
    await this.monitoring.connect();
    
    const middlewares = this.monitoring.createMiddleware();
    middlewares.forEach(middleware => {
      this.app.use(middleware);
    });

    // Health check endpoint
    this.app.get('/health', (req, res) => {
      res.json({ status: 'healthy', timestamp: new Date().toISOString() });
    });
  }

  setupBusinessMetrics() {
    const businessMetrics = new BusinessMetrics(this.monitoring);
    
    this.app.post('/api/users', (req, res) => {
      businessMetrics.trackUserRegistration(req.body.id, req.body.source);
      res.json({ success: true });
    });
    
    this.app.post('/api/purchases', (req, res) => {
      businessMetrics.trackPurchase(req.body.amount, req.body.currency, req.body.productId);
      res.json({ success: true });
    });
  }
}
```

### Performance Monitoring

```javascript
// performance-monitoring.js
class PerformanceMonitoring {
  constructor(monitoringHandler) {
    this.monitoring = monitoringHandler;
  }

  trackDatabaseQuery(query, duration, success) {
    this.monitoring.recordMetric('database_queries_total', 1, {
      query: query.substring(0, 50),
      success: success.toString()
    });
    
    this.monitoring.recordMetric('database_query_duration_ms', duration, {
      query: query.substring(0, 50)
    });
  }

  trackCacheHit(cacheType, hit) {
    this.monitoring.recordMetric('cache_operations_total', 1, {
      type: cacheType,
      hit: hit.toString()
    });
  }

  trackMemoryUsage() {
    const usage = process.memoryUsage();
    
    this.monitoring.recordMetric('memory_heap_used_bytes', usage.heapUsed);
    this.monitoring.recordMetric('memory_heap_total_bytes', usage.heapTotal);
    this.monitoring.recordMetric('memory_rss_bytes', usage.rss);
  }
}
```

## Performance Considerations

### Metrics Buffering

```javascript
// metrics-buffer.js
class MetricsBuffer {
  constructor(monitoringHandler, options = {}) {
    this.monitoring = monitoringHandler;
    this.buffer = [];
    this.maxBufferSize = options.maxBufferSize || 100;
    this.flushInterval = options.flushInterval || 5000;
    this.timer = null;
  }

  start() {
    this.timer = setInterval(() => {
      this.flush();
    }, this.flushInterval);
  }

  stop() {
    if (this.timer) {
      clearInterval(this.timer);
      this.timer = null;
    }
  }

  addMetric(name, value, tags) {
    this.buffer.push({ name, value, tags, timestamp: Date.now() });
    
    if (this.buffer.length >= this.maxBufferSize) {
      this.flush();
    }
  }

  flush() {
    if (this.buffer.length === 0) return;
    
    const metrics = [...this.buffer];
    this.buffer = [];
    
    metrics.forEach(metric => {
      this.monitoring.recordMetric(metric.name, metric.value, metric.tags);
    });
  }
}
```

### Sampling

```javascript
// metrics-sampler.js
class MetricsSampler {
  constructor(sampleRate = 1.0) {
    this.sampleRate = sampleRate;
  }

  shouldSample() {
    return Math.random() < this.sampleRate;
  }

  wrapMonitoring(monitoringHandler) {
    const wrapped = {};
    
    wrapped.recordMetric = (name, value, tags) => {
      if (this.shouldSample()) {
        monitoringHandler.recordMetric(name, value, tags);
      }
    };
    
    return wrapped;
  }
}
```

## Security Notes

### Metrics Sanitization

```javascript
// metrics-sanitizer.js
class MetricsSanitizer {
  constructor() {
    this.sensitiveFields = ['password', 'token', 'secret', 'key'];
  }

  sanitizeTags(tags) {
    const sanitized = { ...tags };
    
    for (const field of this.sensitiveFields) {
      if (sanitized[field]) {
        sanitized[field] = '[REDACTED]';
      }
    }
    
    return sanitized;
  }

  sanitizeMetricName(name) {
    return name.replace(/[^a-zA-Z0-9_:]/g, '_');
  }
}
```

## Best Practices

### Monitoring Configuration Management

```javascript
// monitoring-config-manager.js
class MonitoringConfigManager {
  constructor() {
    this.configs = new Map();
  }

  setConfig(environment, config) {
    this.configs.set(environment, this.validateConfig(config));
  }

  getConfig(environment) {
    const config = this.configs.get(environment);
    if (!config) {
      throw new Error(`No configuration found for environment: ${environment}`);
    }
    return config;
  }

  validateConfig(config) {
    if (!config.type) {
      throw new Error('Monitoring type is required');
    }
    
    return config;
  }

  getCurrentConfig() {
    const environment = process.env.NODE_ENV || 'development';
    return this.getConfig(environment);
  }
}
```

### Monitoring Health Check

```javascript
// monitoring-health-check.js
class MonitoringHealthCheck {
  constructor(monitoringHandler) {
    this.monitoring = monitoringHandler;
  }

  async checkHealth() {
    try {
      this.monitoring.recordMetric('health_check', 1);
      return { status: 'healthy' };
    } catch (error) {
      return { status: 'unhealthy', error: error.message };
    }
  }
}
```

## Related Topics

- [@monitor Operator](./51-tsklang-javascript-operator-monitor.md)
- [@metrics Operator](./49-tsklang-javascript-operator-metrics.md)
- [@trace Operator](./53-tsklang-javascript-operator-trace.md)
- [@debug Operator](./52-tsklang-javascript-operator-debug.md)
- [@logging Directive](./85-tsklang-javascript-directives-logging.md) 