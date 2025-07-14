# TuskLang JavaScript Documentation: Advanced Monitoring

## Overview

Advanced monitoring in TuskLang provides comprehensive application monitoring, alerting, and observability with JavaScript/Node.js integration.

## TuskLang Syntax

```tsk
#monitoring advanced
  metrics:
    enabled: true
    collection_interval: 30s
    retention: 30d
    storage: prometheus
    
  logging:
    enabled: true
    level: info
    format: json
    output:
      - console
      - file
      - elasticsearch
    
  tracing:
    enabled: true
    sampler: probabilistic
    sample_rate: 0.1
    backend: jaeger
    
  alerting:
    enabled: true
    rules:
      - name: high_cpu
        condition: cpu_usage > 80
        duration: 5m
        severity: warning
      - name: high_memory
        condition: memory_usage > 85
        duration: 3m
        severity: critical
      - name: high_error_rate
        condition: error_rate > 5
        duration: 2m
        severity: critical
    
  dashboards:
    enabled: true
    provider: grafana
    auto_discovery: true
    refresh_interval: 30s
```

## JavaScript Integration

### Advanced Monitoring Manager

```javascript
// advanced-monitoring-manager.js
const os = require('os');
const v8 = require('v8');

class AdvancedMonitoringManager {
  constructor(config) {
    this.config = config;
    this.metrics = config.metrics || {};
    this.logging = config.logging || {};
    this.tracing = config.tracing || {};
    this.alerting = config.alerting || {};
    this.dashboards = config.dashboards || {};
    
    this.metricsCollector = new MetricsCollector(this.metrics);
    this.logManager = new LogManager(this.logging);
    this.traceManager = new TraceManager(this.tracing);
    this.alertManager = new AlertManager(this.alerting);
    this.dashboardManager = new DashboardManager(this.dashboards);
  }

  async initialize() {
    await this.metricsCollector.initialize();
    await this.logManager.initialize();
    await this.traceManager.initialize();
    await this.alertManager.initialize();
    await this.dashboardManager.initialize();
    
    console.log('Advanced monitoring manager initialized');
  }

  async collectMetrics() {
    return await this.metricsCollector.collect();
  }

  async log(level, message, context = {}) {
    return await this.logManager.log(level, message, context);
  }

  async startTrace(name, context = {}) {
    return await this.traceManager.startTrace(name, context);
  }

  async checkAlerts() {
    return await this.alertManager.checkAlerts();
  }

  async getDashboard() {
    return await this.dashboardManager.getDashboard();
  }
}

module.exports = AdvancedMonitoringManager;
```

### Metrics Collector

```javascript
// metrics-collector.js
class MetricsCollector {
  constructor(config) {
    this.config = config;
    this.enabled = config.enabled || false;
    this.interval = this.parseTime(config.collection_interval || '30s');
    this.retention = this.parseTime(config.retention || '30d');
    this.storage = config.storage || 'memory';
    
    this.metrics = new Map();
    this.collectionInterval = null;
  }

  parseTime(timeStr) {
    const match = timeStr.match(/^(\d+)([smhd])$/);
    if (!match) return 30000; // Default 30 seconds

    const [, value, unit] = match;
    const multipliers = {
      's': 1000,
      'm': 60 * 1000,
      'h': 60 * 60 * 1000,
      'd': 24 * 60 * 60 * 1000
    };

    return parseInt(value) * multipliers[unit];
  }

  async initialize() {
    if (!this.enabled) return;
    
    this.collectionInterval = setInterval(async () => {
      await this.collect();
    }, this.interval);
    
    console.log(`Metrics collection started with ${this.interval}ms interval`);
  }

  async collect() {
    const metrics = {
      timestamp: Date.now(),
      system: await this.collectSystemMetrics(),
      application: await this.collectApplicationMetrics(),
      custom: await this.collectCustomMetrics()
    };
    
    this.storeMetrics(metrics);
    return metrics;
  }

  async collectSystemMetrics() {
    const usage = process.memoryUsage();
    const cpus = os.cpus();
    
    let totalIdle = 0;
    let totalTick = 0;
    
    cpus.forEach(cpu => {
      for (const type in cpu.times) {
        totalTick += cpu.times[type];
      }
      totalIdle += cpu.times.idle;
    });
    
    const idle = totalIdle / cpus.length;
    const total = totalTick / cpus.length;
    const cpuUsage = 100 - (100 * idle / total);
    
    return {
      cpu_usage: Math.round(cpuUsage),
      memory_usage: Math.round((usage.heapUsed / usage.heapTotal) * 100),
      memory_rss: usage.rss,
      memory_heap_used: usage.heapUsed,
      memory_heap_total: usage.heapTotal,
      uptime: process.uptime(),
      load_average: os.loadavg()
    };
  }

  async collectApplicationMetrics() {
    return {
      active_connections: Math.floor(Math.random() * 100),
      requests_per_second: Math.floor(Math.random() * 1000),
      response_time_avg: Math.random() * 500 + 50,
      error_rate: Math.random() * 5,
      active_users: Math.floor(Math.random() * 1000)
    };
  }

  async collectCustomMetrics() {
    // Custom metrics implementation
    return {
      business_metrics: {
        orders_per_minute: Math.floor(Math.random() * 10),
        revenue_per_hour: Math.random() * 1000
      }
    };
  }

  storeMetrics(metrics) {
    const key = Math.floor(metrics.timestamp / (60 * 1000)); // Minute-based key
    this.metrics.set(key, metrics);
    
    // Clean old metrics
    const cutoff = Date.now() - this.retention;
    for (const [k, v] of this.metrics.entries()) {
      if (v.timestamp < cutoff) {
        this.metrics.delete(k);
      }
    }
  }

  getMetrics(timeRange = '1h') {
    const cutoff = Date.now() - this.parseTime(timeRange);
    const filtered = [];
    
    for (const metrics of this.metrics.values()) {
      if (metrics.timestamp >= cutoff) {
        filtered.push(metrics);
      }
    }
    
    return filtered;
  }

  getMetricsSummary() {
    const metrics = Array.from(this.metrics.values());
    if (metrics.length === 0) return null;
    
    const latest = metrics[metrics.length - 1];
    return {
      current: latest,
      average: this.calculateAverages(metrics),
      trends: this.calculateTrends(metrics)
    };
  }

  calculateAverages(metrics) {
    const sums = {
      cpu_usage: 0,
      memory_usage: 0,
      response_time_avg: 0,
      error_rate: 0
    };
    
    metrics.forEach(m => {
      sums.cpu_usage += m.system.cpu_usage;
      sums.memory_usage += m.system.memory_usage;
      sums.response_time_avg += m.application.response_time_avg;
      sums.error_rate += m.application.error_rate;
    });
    
    const count = metrics.length;
    return {
      cpu_usage: (sums.cpu_usage / count).toFixed(2),
      memory_usage: (sums.memory_usage / count).toFixed(2),
      response_time_avg: (sums.response_time_avg / count).toFixed(2),
      error_rate: (sums.error_rate / count).toFixed(2)
    };
  }

  calculateTrends(metrics) {
    if (metrics.length < 2) return {};
    
    const recent = metrics.slice(-5);
    const older = metrics.slice(-10, -5);
    
    const recentAvg = this.calculateAverages(recent);
    const olderAvg = this.calculateAverages(older);
    
    return {
      cpu_trend: this.calculateTrend(olderAvg.cpu_usage, recentAvg.cpu_usage),
      memory_trend: this.calculateTrend(olderAvg.memory_usage, recentAvg.memory_usage),
      response_time_trend: this.calculateTrend(olderAvg.response_time_avg, recentAvg.response_time_avg)
    };
  }

  calculateTrend(oldValue, newValue) {
    const change = ((newValue - oldValue) / oldValue) * 100;
    if (change > 5) return 'increasing';
    if (change < -5) return 'decreasing';
    return 'stable';
  }
}

module.exports = MetricsCollector;
```

### Log Manager

```javascript
// log-manager.js
const fs = require('fs').promises;
const path = require('path');

class LogManager {
  constructor(config) {
    this.config = config;
    this.enabled = config.enabled || false;
    this.level = config.level || 'info';
    this.format = config.format || 'json';
    this.outputs = config.output || ['console'];
    
    this.levels = {
      error: 0,
      warn: 1,
      info: 2,
      debug: 3
    };
  }

  async initialize() {
    if (!this.enabled) return;
    
    // Setup file output
    if (this.outputs.includes('file')) {
      await this.setupFileOutput();
    }
    
    console.log('Log manager initialized');
  }

  async setupFileOutput() {
    const logDir = 'logs';
    try {
      await fs.mkdir(logDir, { recursive: true });
    } catch (error) {
      console.error('Failed to create log directory:', error);
    }
  }

  async log(level, message, context = {}) {
    if (!this.enabled) return;
    
    if (this.levels[level] > this.levels[this.level]) {
      return; // Skip if level is higher than configured level
    }
    
    const logEntry = this.createLogEntry(level, message, context);
    
    for (const output of this.outputs) {
      await this.writeToOutput(output, logEntry);
    }
  }

  createLogEntry(level, message, context) {
    const entry = {
      timestamp: new Date().toISOString(),
      level: level.toUpperCase(),
      message: message,
      context: context,
      pid: process.pid,
      hostname: require('os').hostname()
    };
    
    if (this.format === 'json') {
      return JSON.stringify(entry);
    } else {
      return `${entry.timestamp} [${entry.level}] ${entry.message} ${JSON.stringify(context)}`;
    }
  }

  async writeToOutput(output, logEntry) {
    switch (output) {
      case 'console':
        this.writeToConsole(logEntry);
        break;
      case 'file':
        await this.writeToFile(logEntry);
        break;
      case 'elasticsearch':
        await this.writeToElasticsearch(logEntry);
        break;
    }
  }

  writeToConsole(logEntry) {
    if (this.format === 'json') {
      console.log(logEntry);
    } else {
      console.log(logEntry);
    }
  }

  async writeToFile(logEntry) {
    const logFile = path.join('logs', `app-${new Date().toISOString().split('T')[0]}.log`);
    
    try {
      await fs.appendFile(logFile, logEntry + '\n');
    } catch (error) {
      console.error('Failed to write to log file:', error);
    }
  }

  async writeToElasticsearch(logEntry) {
    // Elasticsearch integration would go here
    console.log('Writing to Elasticsearch:', logEntry);
  }

  async error(message, context = {}) {
    return await this.log('error', message, context);
  }

  async warn(message, context = {}) {
    return await this.log('warn', message, context);
  }

  async info(message, context = {}) {
    return await this.log('info', message, context);
  }

  async debug(message, context = {}) {
    return await this.log('debug', message, context);
  }
}

module.exports = LogManager;
```

### Trace Manager

```javascript
// trace-manager.js
class TraceManager {
  constructor(config) {
    this.config = config;
    this.enabled = config.enabled || false;
    this.sampler = config.sampler || 'probabilistic';
    this.sampleRate = config.sample_rate || 0.1;
    this.backend = config.backend || 'jaeger';
    
    this.activeTraces = new Map();
    this.traceId = 0;
  }

  async initialize() {
    if (!this.enabled) return;
    
    console.log('Trace manager initialized');
  }

  async startTrace(name, context = {}) {
    if (!this.enabled) return null;
    
    if (this.sampler === 'probabilistic' && Math.random() > this.sampleRate) {
      return null; // Skip trace based on sample rate
    }
    
    const traceId = this.generateTraceId();
    const spanId = this.generateSpanId();
    
    const trace = {
      traceId,
      spanId,
      name,
      startTime: Date.now(),
      context,
      spans: []
    };
    
    this.activeTraces.set(traceId, trace);
    
    return trace;
  }

  async endTrace(traceId) {
    if (!this.enabled) return;
    
    const trace = this.activeTraces.get(traceId);
    if (!trace) return;
    
    trace.endTime = Date.now();
    trace.duration = trace.endTime - trace.startTime;
    
    await this.sendTrace(trace);
    this.activeTraces.delete(traceId);
  }

  async addSpan(traceId, name, context = {}) {
    if (!this.enabled) return;
    
    const trace = this.activeTraces.get(traceId);
    if (!trace) return;
    
    const span = {
      spanId: this.generateSpanId(),
      name,
      startTime: Date.now(),
      context
    };
    
    trace.spans.push(span);
    return span;
  }

  async endSpan(traceId, spanId) {
    if (!this.enabled) return;
    
    const trace = this.activeTraces.get(traceId);
    if (!trace) return;
    
    const span = trace.spans.find(s => s.spanId === spanId);
    if (!span) return;
    
    span.endTime = Date.now();
    span.duration = span.endTime - span.startTime;
  }

  generateTraceId() {
    return `trace-${++this.traceId}-${Date.now()}`;
  }

  generateSpanId() {
    return `span-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
  }

  async sendTrace(trace) {
    switch (this.backend) {
      case 'jaeger':
        await this.sendToJaeger(trace);
        break;
      default:
        console.log('Sending trace:', trace);
    }
  }

  async sendToJaeger(trace) {
    // Jaeger integration would go here
    console.log('Sending trace to Jaeger:', trace);
  }
}

module.exports = TraceManager;
```

### Alert Manager

```javascript
// alert-manager.js
class AlertManager {
  constructor(config) {
    this.config = config;
    this.enabled = config.enabled || false;
    this.rules = config.rules || [];
    
    this.activeAlerts = new Map();
    this.alertHistory = [];
  }

  async initialize() {
    if (!this.enabled) return;
    
    // Start alert checking
    setInterval(async () => {
      await this.checkAlerts();
    }, 60000); // Check every minute
    
    console.log('Alert manager initialized');
  }

  async checkAlerts() {
    const metrics = await this.getCurrentMetrics();
    
    for (const rule of this.rules) {
      const triggered = this.evaluateRule(rule, metrics);
      
      if (triggered) {
        await this.triggerAlert(rule, metrics);
      } else {
        await this.clearAlert(rule);
      }
    }
  }

  evaluateRule(rule, metrics) {
    const condition = rule.condition;
    
    // Simple condition evaluation
    if (condition.includes('cpu_usage >')) {
      const threshold = parseInt(condition.match(/\d+/)[0]);
      return metrics.system?.cpu_usage > threshold;
    }
    
    if (condition.includes('memory_usage >')) {
      const threshold = parseInt(condition.match(/\d+/)[0]);
      return metrics.system?.memory_usage > threshold;
    }
    
    if (condition.includes('error_rate >')) {
      const threshold = parseInt(condition.match(/\d+/)[0]);
      return metrics.application?.error_rate > threshold;
    }
    
    return false;
  }

  async triggerAlert(rule, metrics) {
    const alertId = `${rule.name}-${Date.now()}`;
    
    if (this.activeAlerts.has(rule.name)) {
      return; // Alert already active
    }
    
    const alert = {
      id: alertId,
      rule: rule,
      triggeredAt: Date.now(),
      metrics: metrics,
      severity: rule.severity
    };
    
    this.activeAlerts.set(rule.name, alert);
    this.alertHistory.push(alert);
    
    await this.sendAlert(alert);
  }

  async clearAlert(rule) {
    if (this.activeAlerts.has(rule.name)) {
      const alert = this.activeAlerts.get(rule.name);
      alert.clearedAt = Date.now();
      alert.duration = alert.clearedAt - alert.triggeredAt;
      
      this.activeAlerts.delete(rule.name);
      await this.sendAlertClear(alert);
    }
  }

  async sendAlert(alert) {
    console.log(`ALERT: ${alert.rule.name} - ${alert.rule.severity.toUpperCase()}`);
    console.log(`Condition: ${alert.rule.condition}`);
    console.log(`Metrics:`, alert.metrics);
    
    // Send to notification systems
    await this.sendToSlack(alert);
    await this.sendToEmail(alert);
  }

  async sendAlertClear(alert) {
    console.log(`ALERT CLEARED: ${alert.rule.name} (Duration: ${alert.duration}ms)`);
  }

  async sendToSlack(alert) {
    // Slack integration would go here
    console.log('Sending alert to Slack');
  }

  async sendToEmail(alert) {
    // Email integration would go here
    console.log('Sending alert to email');
  }

  async getCurrentMetrics() {
    // This would get current metrics from the metrics collector
    return {
      system: {
        cpu_usage: Math.random() * 100,
        memory_usage: Math.random() * 100
      },
      application: {
        error_rate: Math.random() * 10
      }
    };
  }

  getActiveAlerts() {
    return Array.from(this.activeAlerts.values());
  }

  getAlertHistory() {
    return this.alertHistory;
  }
}

module.exports = AlertManager;
```

## TypeScript Implementation

```typescript
// advanced-monitoring.types.ts
export interface MonitoringConfig {
  metrics?: MetricsConfig;
  logging?: LoggingConfig;
  tracing?: TracingConfig;
  alerting?: AlertingConfig;
  dashboards?: DashboardConfig;
}

export interface MetricsConfig {
  enabled?: boolean;
  collection_interval?: string;
  retention?: string;
  storage?: string;
}

export interface LoggingConfig {
  enabled?: boolean;
  level?: string;
  format?: string;
  output?: string[];
}

export interface TracingConfig {
  enabled?: boolean;
  sampler?: string;
  sample_rate?: number;
  backend?: string;
}

export interface AlertingConfig {
  enabled?: boolean;
  rules?: AlertRule[];
}

export interface AlertRule {
  name: string;
  condition: string;
  duration: string;
  severity: string;
}

export interface DashboardConfig {
  enabled?: boolean;
  provider?: string;
  auto_discovery?: boolean;
  refresh_interval?: string;
}

export interface MonitoringManager {
  collectMetrics(): Promise<any>;
  log(level: string, message: string, context?: any): Promise<void>;
  startTrace(name: string, context?: any): Promise<any>;
  checkAlerts(): Promise<any>;
  getDashboard(): Promise<any>;
}

// advanced-monitoring.ts
import { MonitoringConfig, MonitoringManager } from './advanced-monitoring.types';

export class TypeScriptAdvancedMonitoringManager implements MonitoringManager {
  private config: MonitoringConfig;

  constructor(config: MonitoringConfig) {
    this.config = config;
  }

  async collectMetrics(): Promise<any> {
    return {
      timestamp: Date.now(),
      system: { cpu_usage: 50, memory_usage: 60 },
      application: { response_time_avg: 200, error_rate: 2 }
    };
  }

  async log(level: string, message: string, context: any = {}): Promise<void> {
    console.log(`[${level.toUpperCase()}] ${message}`, context);
  }

  async startTrace(name: string, context: any = {}): Promise<any> {
    return { traceId: 'trace-1', name, context };
  }

  async checkAlerts(): Promise<any> {
    return { activeAlerts: [] };
  }

  async getDashboard(): Promise<any> {
    return { metrics: await this.collectMetrics() };
  }
}
```

## Advanced Usage Scenarios

### Application Performance Monitoring

```javascript
// apm-manager.js
class APMManager {
  constructor(monitoringManager) {
    this.monitoring = monitoringManager;
  }

  async monitorRequest(req, res, next) {
    const startTime = Date.now();
    const trace = await this.monitoring.startTrace('http_request', {
      method: req.method,
      url: req.url,
      userAgent: req.headers['user-agent']
    });
    
    // Override res.end to capture response
    const originalEnd = res.end;
    res.end = async (data) => {
      const duration = Date.now() - startTime;
      
      await this.monitoring.log('info', 'Request completed', {
        method: req.method,
        url: req.url,
        statusCode: res.statusCode,
        duration: duration
      });
      
      if (trace) {
        await this.monitoring.endTrace(trace.traceId);
      }
      
      originalEnd.call(res, data);
    };
    
    next();
  }

  async monitorDatabase(query, duration) {
    await this.monitoring.log('debug', 'Database query', {
      query: query,
      duration: duration
    });
  }

  async monitorError(error, context = {}) {
    await this.monitoring.log('error', error.message, {
      stack: error.stack,
      ...context
    });
  }
}
```

### Business Metrics Monitoring

```javascript
// business-metrics-monitor.js
class BusinessMetricsMonitor {
  constructor(monitoringManager) {
    this.monitoring = monitoringManager;
  }

  async trackOrder(order) {
    await this.monitoring.log('info', 'Order created', {
      orderId: order.id,
      amount: order.amount,
      customerId: order.customerId
    });
  }

  async trackUserRegistration(user) {
    await this.monitoring.log('info', 'User registered', {
      userId: user.id,
      email: user.email,
      source: user.source
    });
  }

  async trackConversion(event) {
    await this.monitoring.log('info', 'Conversion event', {
      eventType: event.type,
      userId: event.userId,
      value: event.value
    });
  }
}
```

## Real-World Examples

### Express.js Monitoring Setup

```javascript
// express-monitoring-setup.js
const express = require('express');
const AdvancedMonitoringManager = require('./advanced-monitoring-manager');

class ExpressMonitoringSetup {
  constructor(app, config) {
    this.app = app;
    this.monitoring = new AdvancedMonitoringManager(config);
    this.apm = new APMManager(this.monitoring);
  }

  setupMonitoring() {
    // Setup request monitoring
    this.app.use((req, res, next) => {
      this.apm.monitorRequest(req, res, next);
    });
    
    // Setup error monitoring
    this.app.use((error, req, res, next) => {
      this.apm.monitorError(error, {
        method: req.method,
        url: req.url
      });
      next(error);
    });
    
    // Setup health check endpoint
    this.app.get('/health', async (req, res) => {
      const metrics = await this.monitoring.collectMetrics();
      res.json({
        status: 'healthy',
        metrics: metrics
      });
    });
  }

  async logApplicationEvent(event, data) {
    await this.monitoring.log('info', event, data);
  }
}
```

### Microservices Monitoring

```javascript
// microservices-monitoring.js
class MicroservicesMonitoring {
  constructor(monitoringManager) {
    this.monitoring = monitoringManager;
    this.services = new Map();
  }

  registerService(name, config) {
    this.services.set(name, config);
  }

  async monitorServiceCall(serviceName, method, duration, success) {
    await this.monitoring.log('info', 'Service call', {
      service: serviceName,
      method: method,
      duration: duration,
      success: success
    });
  }

  async monitorServiceHealth(serviceName, health) {
    await this.monitoring.log('info', 'Service health check', {
      service: serviceName,
      health: health
    });
  }
}
```

## Performance Considerations

### Monitoring Performance Impact

```javascript
// monitoring-performance-monitor.js
class MonitoringPerformanceMonitor {
  constructor() {
    this.metrics = {
      monitoringCalls: 0,
      avgResponseTime: 0,
      overhead: 0
    };
  }

  async measureMonitoringOverhead(operation) {
    const start = Date.now();
    const result = await operation();
    const duration = Date.now() - start;
    
    this.metrics.monitoringCalls++;
    this.metrics.avgResponseTime = 
      (this.metrics.avgResponseTime * (this.metrics.monitoringCalls - 1) + duration) / this.metrics.monitoringCalls;
    
    return result;
  }

  getMetrics() {
    return this.metrics;
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
      throw new Error(`No monitoring configuration found for environment: ${environment}`);
    }
    return config;
  }

  validateConfig(config) {
    if (!config.metrics && !config.logging && !config.tracing) {
      throw new Error('At least one monitoring component must be enabled');
    }
    
    return config;
  }
}
```

### Monitoring Health Check

```javascript
// monitoring-health-monitor.js
class MonitoringHealthMonitor {
  constructor(monitoringManager) {
    this.monitoring = monitoringManager;
    this.metrics = {
      healthChecks: 0,
      failures: 0,
      avgResponseTime: 0
    };
  }

  async checkHealth() {
    try {
      const start = Date.now();
      
      // Test metrics collection
      await this.monitoring.collectMetrics();
      
      // Test logging
      await this.monitoring.log('info', 'Health check');
      
      // Test tracing
      const trace = await this.monitoring.startTrace('health_check');
      if (trace) {
        await this.monitoring.endTrace(trace.traceId);
      }
      
      const responseTime = Date.now() - start;
      
      this.metrics.healthChecks++;
      this.metrics.avgResponseTime = 
        (this.metrics.avgResponseTime * (this.metrics.healthChecks - 1) + responseTime) / this.metrics.healthChecks;
      
      return {
        status: 'healthy',
        responseTime,
        metrics: this.metrics
      };
    } catch (error) {
      this.metrics.failures++;
      return {
        status: 'unhealthy',
        error: error.message,
        metrics: this.metrics
      };
    }
  }
}
```

## Related Topics

- [@monitor Operator](./51-tsklang-javascript-operator-monitor.md)
- [@metrics Operator](./49-tsklang-javascript-operator-metrics.md)
- [@log Operator](./47-tsklang-javascript-operator-log.md)
- [@trace Operator](./53-tsklang-javascript-operator-trace.md) 