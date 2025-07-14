# TuskLang JavaScript Documentation: Advanced Performance

## Overview

Advanced performance optimization in TuskLang provides sophisticated performance monitoring, optimization strategies, and performance tuning with JavaScript/Node.js integration.

## TuskLang Syntax

```tsk
#performance advanced
  monitoring:
    enabled: true
    metrics:
      - response_time
      - throughput
      - memory_usage
      - cpu_usage
      - error_rate
    sampling_rate: 0.1
    retention: 7d
    
  optimization:
    caching:
      enabled: true
      strategy: lru
      max_size: 100MB
    compression:
      enabled: true
      algorithm: gzip
      level: 6
    minification:
      enabled: true
      javascript: true
      css: true
      html: true
    
  profiling:
    enabled: true
    cpu_profiling: true
    memory_profiling: true
    heap_dumps: true
    interval: 60s
    
  load_balancing:
    enabled: true
    algorithm: round_robin
    health_checks: true
    sticky_sessions: false
```

## JavaScript Integration

### Performance Monitor

```javascript
// performance-monitor.js
const os = require('os');
const v8 = require('v8');

class PerformanceMonitor {
  constructor(config) {
    this.config = config;
    this.metrics = new Map();
    this.samplingRate = config.monitoring?.sampling_rate || 0.1;
    this.retention = this.parseTime(config.monitoring?.retention || '7d');
  }

  parseTime(timeStr) {
    const match = timeStr.match(/^(\d+)([dmh])$/);
    if (!match) return 7 * 24 * 60 * 60 * 1000; // 7 days
    const [, value, unit] = match;
    const multipliers = { d: 24 * 60 * 60 * 1000, m: 30 * 24 * 60 * 60 * 1000, h: 60 * 60 * 1000 };
    return parseInt(value) * multipliers[unit];
  }

  async collectMetrics() {
    const metrics = {
      timestamp: Date.now(),
      responseTime: await this.getResponseTime(),
      throughput: await this.getThroughput(),
      memoryUsage: this.getMemoryUsage(),
      cpuUsage: await this.getCPUUsage(),
      errorRate: await this.getErrorRate()
    };

    if (Math.random() < this.samplingRate) {
      this.storeMetrics(metrics);
    }

    return metrics;
  }

  async getResponseTime() {
    // Simulate response time measurement
    return Math.random() * 100 + 10;
  }

  async getThroughput() {
    // Simulate throughput measurement
    return Math.random() * 1000 + 100;
  }

  getMemoryUsage() {
    const usage = process.memoryUsage();
    return {
      rss: usage.rss,
      heapUsed: usage.heapUsed,
      heapTotal: usage.heapTotal,
      external: usage.external
    };
  }

  async getCPUUsage() {
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
    const utilization = 100 - (100 * idle / total);
    
    return Math.round(utilization);
  }

  async getErrorRate() {
    // Simulate error rate calculation
    return Math.random() * 5;
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

  getPerformanceReport() {
    const metrics = Array.from(this.metrics.values());
    if (metrics.length === 0) return null;
    
    const avgResponseTime = metrics.reduce((sum, m) => sum + m.responseTime, 0) / metrics.length;
    const avgThroughput = metrics.reduce((sum, m) => sum + m.throughput, 0) / metrics.length;
    const avgErrorRate = metrics.reduce((sum, m) => sum + m.errorRate, 0) / metrics.length;
    
    return {
      avgResponseTime: avgResponseTime.toFixed(2) + 'ms',
      avgThroughput: avgThroughput.toFixed(2) + ' req/s',
      avgErrorRate: avgErrorRate.toFixed(2) + '%',
      totalMetrics: metrics.length
    };
  }
}

module.exports = PerformanceMonitor;
```

### Performance Optimizer

```javascript
// performance-optimizer.js
const zlib = require('zlib');
const { promisify } = require('util');

const gzip = promisify(zlib.gzip);
const gunzip = promisify(zlib.gunzip);

class PerformanceOptimizer {
  constructor(config) {
    this.config = config;
    this.cache = new Map();
    this.compressionCache = new Map();
  }

  async optimizeResponse(data, options = {}) {
    let optimized = data;
    
    if (this.config.optimization?.caching?.enabled) {
      optimized = await this.applyCaching(optimized, options);
    }
    
    if (this.config.optimization?.compression?.enabled) {
      optimized = await this.applyCompression(optimized, options);
    }
    
    if (this.config.optimization?.minification?.enabled) {
      optimized = await this.applyMinification(optimized, options);
    }
    
    return optimized;
  }

  async applyCaching(data, options) {
    const key = this.generateCacheKey(data, options);
    
    if (this.cache.has(key)) {
      return this.cache.get(key);
    }
    
    this.cache.set(key, data);
    return data;
  }

  async applyCompression(data, options) {
    const key = this.generateCacheKey(data, options);
    
    if (this.compressionCache.has(key)) {
      return this.compressionCache.get(key);
    }
    
    const algorithm = this.config.optimization?.compression?.algorithm || 'gzip';
    const level = this.config.optimization?.compression?.level || 6;
    
    let compressed;
    if (algorithm === 'gzip') {
      compressed = await gzip(data, { level });
    } else {
      compressed = data; // No compression
    }
    
    this.compressionCache.set(key, compressed);
    return compressed;
  }

  async applyMinification(data, options) {
    if (typeof data === 'string') {
      if (this.config.optimization?.minification?.javascript && options.type === 'js') {
        return this.minifyJavaScript(data);
      }
      
      if (this.config.optimization?.minification?.css && options.type === 'css') {
        return this.minifyCSS(data);
      }
      
      if (this.config.optimization?.minification?.html && options.type === 'html') {
        return this.minifyHTML(data);
      }
    }
    
    return data;
  }

  minifyJavaScript(code) {
    // Basic JavaScript minification
    return code
      .replace(/\/\*[\s\S]*?\*\//g, '') // Remove comments
      .replace(/\s+/g, ' ') // Collapse whitespace
      .replace(/\s*([{}:;,=])\s*/g, '$1') // Remove spaces around operators
      .trim();
  }

  minifyCSS(css) {
    // Basic CSS minification
    return css
      .replace(/\/\*[\s\S]*?\*\//g, '') // Remove comments
      .replace(/\s+/g, ' ') // Collapse whitespace
      .replace(/\s*([{}:;,])\s*/g, '$1') // Remove spaces around operators
      .trim();
  }

  minifyHTML(html) {
    // Basic HTML minification
    return html
      .replace(/\s+/g, ' ') // Collapse whitespace
      .replace(/>\s+</g, '><') // Remove spaces between tags
      .trim();
  }

  generateCacheKey(data, options) {
    const hash = require('crypto').createHash('md5');
    hash.update(JSON.stringify({ data, options }));
    return hash.digest('hex');
  }

  clearCache() {
    this.cache.clear();
    this.compressionCache.clear();
  }
}

module.exports = PerformanceOptimizer;
```

### Performance Profiler

```javascript
// performance-profiler.js
const v8 = require('v8');

class PerformanceProfiler {
  constructor(config) {
    this.config = config;
    this.profiles = new Map();
    this.heapDumps = [];
  }

  async startProfiling() {
    if (!this.config.profiling?.enabled) return;
    
    if (this.config.profiling?.cpu_profiling) {
      await this.startCPUProfiling();
    }
    
    if (this.config.profiling?.memory_profiling) {
      await this.startMemoryProfiling();
    }
  }

  async startCPUProfiling() {
    console.log('CPU profiling started');
    // CPU profiling implementation would go here
  }

  async startMemoryProfiling() {
    console.log('Memory profiling started');
    // Memory profiling implementation would go here
  }

  async takeHeapDump() {
    if (!this.config.profiling?.heap_dumps) return;
    
    const heapStats = v8.getHeapStatistics();
    const heapDump = {
      timestamp: Date.now(),
      heapUsed: heapStats.used_heap_size,
      heapTotal: heapStats.total_heap_size,
      heapLimit: heapStats.heap_size_limit,
      external: heapStats.external_memory
    };
    
    this.heapDumps.push(heapDump);
    
    // Keep only last 10 heap dumps
    if (this.heapDumps.length > 10) {
      this.heapDumps.shift();
    }
    
    return heapDump;
  }

  async getProfilingReport() {
    const report = {
      cpuProfiling: this.config.profiling?.cpu_profiling || false,
      memoryProfiling: this.config.profiling?.memory_profiling || false,
      heapDumps: this.heapDumps.length,
      lastHeapDump: this.heapDumps[this.heapDumps.length - 1]
    };
    
    return report;
  }
}

module.exports = PerformanceProfiler;
```

### Load Balancer

```javascript
// load-balancer.js
class LoadBalancer {
  constructor(config) {
    this.config = config;
    this.servers = [];
    this.currentIndex = 0;
    this.healthChecks = new Map();
  }

  addServer(server) {
    this.servers.push(server);
    this.healthChecks.set(server.id, { healthy: true, lastCheck: Date.now() });
  }

  removeServer(serverId) {
    this.servers = this.servers.filter(s => s.id !== serverId);
    this.healthChecks.delete(serverId);
  }

  async getNextServer() {
    if (this.servers.length === 0) {
      throw new Error('No servers available');
    }
    
    const healthyServers = this.servers.filter(server => 
      this.healthChecks.get(server.id)?.healthy
    );
    
    if (healthyServers.length === 0) {
      throw new Error('No healthy servers available');
    }
    
    switch (this.config.load_balancing?.algorithm || 'round_robin') {
      case 'round_robin':
        return this.roundRobin(healthyServers);
      case 'least_connections':
        return this.leastConnections(healthyServers);
      case 'weighted':
        return this.weighted(healthyServers);
      default:
        return this.roundRobin(healthyServers);
    }
  }

  roundRobin(servers) {
    const server = servers[this.currentIndex % servers.length];
    this.currentIndex = (this.currentIndex + 1) % servers.length;
    return server;
  }

  leastConnections(servers) {
    return servers.reduce((min, server) => 
      (server.connections || 0) < (min.connections || 0) ? server : min
    );
  }

  weighted(servers) {
    const totalWeight = servers.reduce((sum, server) => sum + (server.weight || 1), 0);
    let random = Math.random() * totalWeight;
    
    for (const server of servers) {
      random -= (server.weight || 1);
      if (random <= 0) {
        return server;
      }
    }
    
    return servers[0];
  }

  async performHealthCheck(server) {
    try {
      const response = await fetch(`${server.url}/health`);
      const healthy = response.ok;
      
      this.healthChecks.set(server.id, {
        healthy,
        lastCheck: Date.now()
      });
      
      return healthy;
    } catch (error) {
      this.healthChecks.set(server.id, {
        healthy: false,
        lastCheck: Date.now(),
        error: error.message
      });
      
      return false;
    }
  }

  async performHealthChecks() {
    if (!this.config.load_balancing?.health_checks) return;
    
    const checks = this.servers.map(server => this.performHealthCheck(server));
    await Promise.all(checks);
  }

  getHealthStatus() {
    const status = {};
    
    for (const [serverId, health] of this.healthChecks.entries()) {
      status[serverId] = health;
    }
    
    return status;
  }
}

module.exports = LoadBalancer;
```

## TypeScript Implementation

```typescript
// advanced-performance.types.ts
export interface PerformanceConfig {
  monitoring?: MonitoringConfig;
  optimization?: OptimizationConfig;
  profiling?: ProfilingConfig;
  load_balancing?: LoadBalancingConfig;
}

export interface MonitoringConfig {
  enabled?: boolean;
  metrics?: string[];
  sampling_rate?: number;
  retention?: string;
}

export interface OptimizationConfig {
  caching?: CachingConfig;
  compression?: CompressionConfig;
  minification?: MinificationConfig;
}

export interface CachingConfig {
  enabled?: boolean;
  strategy?: string;
  max_size?: string;
}

export interface CompressionConfig {
  enabled?: boolean;
  algorithm?: string;
  level?: number;
}

export interface MinificationConfig {
  enabled?: boolean;
  javascript?: boolean;
  css?: boolean;
  html?: boolean;
}

export interface ProfilingConfig {
  enabled?: boolean;
  cpu_profiling?: boolean;
  memory_profiling?: boolean;
  heap_dumps?: boolean;
  interval?: string;
}

export interface LoadBalancingConfig {
  enabled?: boolean;
  algorithm?: string;
  health_checks?: boolean;
  sticky_sessions?: boolean;
}

export interface PerformanceManager {
  collectMetrics(): Promise<any>;
  optimizeResponse(data: any, options?: any): Promise<any>;
  startProfiling(): Promise<void>;
  getNextServer(): Promise<any>;
}

// advanced-performance.ts
import { PerformanceConfig, PerformanceManager } from './advanced-performance.types';

export class TypeScriptAdvancedPerformanceManager implements PerformanceManager {
  private config: PerformanceConfig;

  constructor(config: PerformanceConfig) {
    this.config = config;
  }

  async collectMetrics(): Promise<any> {
    return {
      timestamp: Date.now(),
      responseTime: Math.random() * 100,
      throughput: Math.random() * 1000,
      memoryUsage: process.memoryUsage(),
      cpuUsage: Math.random() * 100
    };
  }

  async optimizeResponse(data: any, options: any = {}): Promise<any> {
    return data; // Basic optimization
  }

  async startProfiling(): Promise<void> {
    console.log('Profiling started');
  }

  async getNextServer(): Promise<any> {
    return { id: 1, url: 'http://localhost:3000' };
  }
}
```

## Advanced Usage Scenarios

### Performance Monitoring Dashboard

```javascript
// performance-dashboard.js
class PerformanceDashboard {
  constructor(performanceManager) {
    this.performance = performanceManager;
    this.metrics = [];
  }

  async updateDashboard() {
    const metrics = await this.performance.collectMetrics();
    this.metrics.push(metrics);
    
    // Keep only last 1000 metrics
    if (this.metrics.length > 1000) {
      this.metrics.shift();
    }
    
    return this.generateReport();
  }

  generateReport() {
    const recentMetrics = this.metrics.slice(-100);
    
    const avgResponseTime = recentMetrics.reduce((sum, m) => sum + m.responseTime, 0) / recentMetrics.length;
    const avgThroughput = recentMetrics.reduce((sum, m) => sum + m.throughput, 0) / recentMetrics.length;
    const avgCPUUsage = recentMetrics.reduce((sum, m) => sum + m.cpuUsage, 0) / recentMetrics.length;
    
    return {
      avgResponseTime: avgResponseTime.toFixed(2) + 'ms',
      avgThroughput: avgThroughput.toFixed(2) + ' req/s',
      avgCPUUsage: avgCPUUsage.toFixed(2) + '%',
      totalRequests: recentMetrics.length,
      alerts: this.generateAlerts(recentMetrics)
    };
  }

  generateAlerts(metrics) {
    const alerts = [];
    
    const avgResponseTime = metrics.reduce((sum, m) => sum + m.responseTime, 0) / metrics.length;
    if (avgResponseTime > 500) {
      alerts.push('High response time detected');
    }
    
    const avgCPUUsage = metrics.reduce((sum, m) => sum + m.cpuUsage, 0) / metrics.length;
    if (avgCPUUsage > 80) {
      alerts.push('High CPU usage detected');
    }
    
    return alerts;
  }
}
```

### Performance Optimization Pipeline

```javascript
// performance-pipeline.js
class PerformancePipeline {
  constructor(optimizer, profiler, loadBalancer) {
    this.optimizer = optimizer;
    this.profiler = profiler;
    this.loadBalancer = loadBalancer;
  }

  async processRequest(request) {
    const start = Date.now();
    
    // Get server
    const server = await this.loadBalancer.getNextServer();
    
    // Optimize request
    const optimizedRequest = await this.optimizer.optimizeResponse(request);
    
    // Process request
    const response = await this.processOnServer(server, optimizedRequest);
    
    // Optimize response
    const optimizedResponse = await this.optimizer.optimizeResponse(response);
    
    const duration = Date.now() - start;
    
    // Record metrics
    this.recordMetrics(duration, server);
    
    return optimizedResponse;
  }

  async processOnServer(server, request) {
    // Simulate server processing
    return { data: 'processed', server: server.id };
  }

  recordMetrics(duration, server) {
    // Record performance metrics
    console.log(`Request processed in ${duration}ms on server ${server.id}`);
  }
}
```

## Real-World Examples

### Express.js Performance Middleware

```javascript
// express-performance.js
const express = require('express');
const PerformanceMonitor = require('./performance-monitor');
const PerformanceOptimizer = require('./performance-optimizer');

class ExpressPerformance {
  constructor(app, config) {
    this.app = app;
    this.monitor = new PerformanceMonitor(config);
    this.optimizer = new PerformanceOptimizer(config);
  }

  setupPerformanceMiddleware() {
    this.app.use(async (req, res, next) => {
      const start = Date.now();
      
      // Monitor request
      req.startTime = start;
      
      // Override res.json to optimize response
      const originalJson = res.json;
      res.json = async (data) => {
        const optimized = await this.optimizer.optimizeResponse(data, {
          type: 'json',
          compress: true
        });
        
        originalJson.call(res, optimized);
      };
      
      next();
    });
    
    this.app.use(async (req, res, next) => {
      const duration = Date.now() - req.startTime;
      
      // Record metrics
      await this.monitor.collectMetrics();
      
      next();
    });
  }

  async getPerformanceReport() {
    return this.monitor.getPerformanceReport();
  }
}
```

### API Performance Optimization

```javascript
// api-performance.js
class APIPerformance {
  constructor(performanceManager) {
    this.performance = performanceManager;
  }

  async optimizeEndpoint(req, res, next) {
    const start = Date.now();
    
    try {
      // Optimize request processing
      const optimizedData = await this.performance.optimizeResponse(req.body);
      req.body = optimizedData;
      
      next();
    } catch (error) {
      res.status(500).json({ error: 'Performance optimization failed' });
    }
  }

  async cacheResponse(req, res, next) {
    const cacheKey = this.generateCacheKey(req);
    
    // Check cache
    const cached = await this.performance.getCachedResponse(cacheKey);
    if (cached) {
      return res.json(cached);
    }
    
    // Override res.json to cache response
    const originalJson = res.json;
    res.json = async (data) => {
      await this.performance.cacheResponse(cacheKey, data);
      originalJson.call(res, data);
    };
    
    next();
  }

  generateCacheKey(req) {
    return `${req.method}:${req.path}:${JSON.stringify(req.query)}`;
  }
}
```

## Performance Considerations

### Performance Monitoring

```javascript
// performance-monitoring.js
class PerformanceMonitoring {
  constructor() {
    this.metrics = {
      requests: 0,
      errors: 0,
      avgResponseTime: 0,
      peakResponseTime: 0
    };
  }

  recordRequest(duration, success) {
    this.metrics.requests++;
    if (!success) {
      this.metrics.errors++;
    }
    
    this.metrics.avgResponseTime = 
      (this.metrics.avgResponseTime * (this.metrics.requests - 1) + duration) / this.metrics.requests;
    
    if (duration > this.metrics.peakResponseTime) {
      this.metrics.peakResponseTime = duration;
    }
  }

  getMetrics() {
    return {
      ...this.metrics,
      errorRate: this.metrics.requests > 0 
        ? (this.metrics.errors / this.metrics.requests * 100).toFixed(2) + '%'
        : '0%'
    };
  }
}
```

## Best Practices

### Performance Configuration Management

```javascript
// performance-config-manager.js
class PerformanceConfigManager {
  constructor() {
    this.configs = new Map();
  }

  setConfig(environment, config) {
    this.configs.set(environment, this.validateConfig(config));
  }

  getConfig(environment) {
    const config = this.configs.get(environment);
    if (!config) {
      throw new Error(`No performance configuration found for environment: ${environment}`);
    }
    return config;
  }

  validateConfig(config) {
    if (config.monitoring?.sampling_rate < 0 || config.monitoring?.sampling_rate > 1) {
      throw new Error('Sampling rate must be between 0 and 1');
    }
    
    return config;
  }
}
```

### Performance Health Monitoring

```javascript
// performance-health-monitor.js
class PerformanceHealthMonitor {
  constructor(performanceManager) {
    this.performance = performanceManager;
    this.metrics = {
      performanceChecks: 0,
      violations: 0,
      avgResponseTime: 0
    };
  }

  async checkHealth() {
    try {
      const start = Date.now();
      const metrics = await this.performance.collectMetrics();
      const responseTime = Date.now() - start;
      
      this.metrics.performanceChecks++;
      this.metrics.avgResponseTime = 
        (this.metrics.avgResponseTime * (this.metrics.performanceChecks - 1) + responseTime) / this.metrics.performanceChecks;
      
      // Check for performance violations
      if (metrics.responseTime > 1000) {
        this.metrics.violations++;
      }
      
      return {
        status: 'healthy',
        responseTime,
        metrics: this.metrics,
        currentMetrics: metrics
      };
    } catch (error) {
      this.metrics.violations++;
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

- [@performance Operator](./50-tsklang-javascript-operator-performance.md)
- [@optimize Operator](./52-tsklang-javascript-operator-optimize.md)
- [@metrics Operator](./49-tsklang-javascript-operator-metrics.md)
- [@monitor Operator](./51-tsklang-javascript-operator-monitor.md) 