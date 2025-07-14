# @metrics Function - JavaScript

## Overview

The `@metrics` function in TuskLang provides comprehensive performance monitoring and metrics collection capabilities. This is essential for JavaScript applications that need to track performance, monitor system health, and gather insights about application behavior in real-time.

## Basic Syntax

```tsk
# Simple metrics collection
response_time: @metrics("api.response_time", 150)
error_rate: @metrics("api.error_rate", 0.02)
throughput: @metrics("api.requests_per_second", 1000)

# Metrics with tags
user_login_metric: @metrics("auth.login_success", 1, {"user_type": "premium", "method": "oauth"})
database_query_metric: @metrics("db.query_duration", 45, {"table": "users", "operation": "select"})
```

## JavaScript Integration

### Node.js Metrics Integration

```javascript
const tusk = require('tusklang');

// Load configuration with metrics
const config = tusk.load('metrics.tsk');

// Access metrics data
console.log(config.response_time); // 150
console.log(config.error_rate); // 0.02
console.log(config.throughput); // 1000

// Use metrics in application monitoring
const performanceMonitor = {
  responseTime: config.response_time,
  errorRate: config.error_rate,
  throughput: config.throughput
};

// Dynamic metrics collection
const startTime = Date.now();
// ... perform operation
const duration = Date.now() - startTime;
await tusk.metrics("operation.duration", duration, { operation: "data_processing" });
```

### Browser Metrics Integration

```javascript
// Load TuskLang configuration
const config = await tusk.load('metrics.tsk');

// Use metrics in frontend monitoring
class FrontendMetrics {
  constructor() {
    this.responseTime = config.response_time;
    this.errorRate = config.error_rate;
  }
  
  async trackUserInteraction(action, duration) {
    await tusk.metrics("ui.interaction", duration, {
      action: action,
      page: window.location.pathname
    });
  }
  
  async trackPageLoad(loadTime) {
    await tusk.metrics("ui.page_load", loadTime, {
      page: window.location.pathname,
      user_agent: navigator.userAgent
    });
  }
  
  async trackError(error, context) {
    await tusk.metrics("ui.error", 1, {
      error_type: error.name,
      error_message: error.message,
      context: context
    });
  }
}
```

## Advanced Usage

### Custom Metrics

```tsk
# Business metrics
revenue_metric: @metrics("business.daily_revenue", 15000.50, {"currency": "USD", "region": "US"})
user_engagement: @metrics("user.session_duration", 1800, {"user_type": "active"})

# System metrics
cpu_usage: @metrics("system.cpu_percentage", 65.5, {"core": "all"})
memory_usage: @metrics("system.memory_mb", 2048, {"type": "heap"})
disk_usage: @metrics("system.disk_percentage", 75.2, {"partition": "root"})
```

### Aggregated Metrics

```tsk
# Time-based aggregations
hourly_requests: @metrics("api.hourly_requests", 3600, {"aggregation": "sum", "window": "1h"})
daily_active_users: @metrics("user.daily_active", 15000, {"aggregation": "unique", "window": "24h"})

# Statistical aggregations
avg_response_time: @metrics("api.avg_response_time", 125.5, {"aggregation": "mean", "window": "5m"})
p95_response_time: @metrics("api.p95_response_time", 250.0, {"aggregation": "percentile", "window": "5m"})
```

### Conditional Metrics

```tsk
# Environment-specific metrics
prod_metrics: @metrics("production.error_rate", 0.01, {"environment": "production"})
dev_metrics: @metrics("development.error_rate", 0.05, {"environment": "development"})

# Feature flag metrics
feature_usage: @metrics("feature.usage", 1, {"feature": "new_ui", "enabled": "true"})
```

## JavaScript Implementation

### Custom Metrics Manager

```javascript
class TuskMetricsManager {
  constructor() {
    this.metrics = new Map();
    this.aggregators = new Map();
    this.exporters = [];
  }
  
  async record(metricName, value, tags = {}) {
    const metricKey = this.generateMetricKey(metricName, tags);
    
    // Store metric
    if (!this.metrics.has(metricKey)) {
      this.metrics.set(metricKey, {
        name: metricName,
        values: [],
        tags: tags,
        timestamp: Date.now()
      });
    }
    
    const metric = this.metrics.get(metricKey);
    metric.values.push({
      value: value,
      timestamp: Date.now()
    });
    
    // Apply aggregations
    await this.applyAggregations(metricName, value, tags);
    
    // Export metrics
    await this.exportMetrics(metricName, value, tags);
    
    return metric;
  }
  
  async applyAggregations(metricName, value, tags) {
    const aggregatorKey = `${metricName}_${JSON.stringify(tags)}`;
    
    if (!this.aggregators.has(aggregatorKey)) {
      this.aggregators.set(aggregatorKey, {
        sum: 0,
        count: 0,
        min: Infinity,
        max: -Infinity,
        values: []
      });
    }
    
    const aggregator = this.aggregators.get(aggregatorKey);
    aggregator.sum += value;
    aggregator.count += 1;
    aggregator.min = Math.min(aggregator.min, value);
    aggregator.max = Math.max(aggregator.max, value);
    aggregator.values.push(value);
    
    // Keep only recent values for rolling windows
    if (aggregator.values.length > 1000) {
      aggregator.values = aggregator.values.slice(-1000);
    }
  }
  
  async exportMetrics(metricName, value, tags) {
    const metricData = {
      name: metricName,
      value: value,
      tags: tags,
      timestamp: Date.now()
    };
    
    // Export to all configured exporters
    for (const exporter of this.exporters) {
      try {
        await exporter.export(metricData);
      } catch (error) {
        console.error(`Failed to export metric to ${exporter.name}:`, error);
      }
    }
  }
  
  generateMetricKey(metricName, tags) {
    const sortedTags = Object.keys(tags).sort().map(key => `${key}=${tags[key]}`).join(',');
    return `${metricName}:${sortedTags}`;
  }
  
  getAggregatedMetrics(metricName, tags = {}) {
    const aggregatorKey = `${metricName}_${JSON.stringify(tags)}`;
    const aggregator = this.aggregators.get(aggregatorKey);
    
    if (!aggregator) {
      return null;
    }
    
    return {
      sum: aggregator.sum,
      count: aggregator.count,
      min: aggregator.min,
      max: aggregator.max,
      mean: aggregator.sum / aggregator.count,
      p95: this.calculatePercentile(aggregator.values, 95),
      p99: this.calculatePercentile(aggregator.values, 99)
    };
  }
  
  calculatePercentile(values, percentile) {
    if (values.length === 0) return 0;
    
    const sorted = values.slice().sort((a, b) => a - b);
    const index = Math.ceil((percentile / 100) * sorted.length) - 1;
    return sorted[index];
  }
  
  addExporter(exporter) {
    this.exporters.push(exporter);
  }
}
```

### TypeScript Support

```typescript
interface MetricData {
  name: string;
  value: number;
  tags: Record<string, string | number>;
  timestamp: number;
}

interface AggregatedMetrics {
  sum: number;
  count: number;
  min: number;
  max: number;
  mean: number;
  p95: number;
  p99: number;
}

interface MetricExporter {
  name: string;
  export(metric: MetricData): Promise<void>;
}

class TypedMetricsManager {
  private metrics: Map<string, any>;
  private aggregators: Map<string, any>;
  private exporters: MetricExporter[];
  
  constructor() {
    this.metrics = new Map();
    this.aggregators = new Map();
    this.exporters = [];
  }
  
  async record(metricName: string, value: number, tags: Record<string, string | number> = {}): Promise<any> {
    const metricKey = this.generateMetricKey(metricName, tags);
    
    if (!this.metrics.has(metricKey)) {
      this.metrics.set(metricKey, {
        name: metricName,
        values: [],
        tags: tags,
        timestamp: Date.now()
      });
    }
    
    const metric = this.metrics.get(metricKey);
    metric.values.push({
      value: value,
      timestamp: Date.now()
    });
    
    await this.applyAggregations(metricName, value, tags);
    await this.exportMetrics(metricName, value, tags);
    
    return metric;
  }
  
  getAggregatedMetrics(metricName: string, tags: Record<string, string | number> = {}): AggregatedMetrics | null {
    const aggregatorKey = `${metricName}_${JSON.stringify(tags)}`;
    const aggregator = this.aggregators.get(aggregatorKey);
    
    if (!aggregator) {
      return null;
    }
    
    return {
      sum: aggregator.sum,
      count: aggregator.count,
      min: aggregator.min,
      max: aggregator.max,
      mean: aggregator.sum / aggregator.count,
      p95: this.calculatePercentile(aggregator.values, 95),
      p99: this.calculatePercentile(aggregator.values, 99)
    };
  }
  
  addExporter(exporter: MetricExporter): void {
    this.exporters.push(exporter);
  }
  
  private generateMetricKey(metricName: string, tags: Record<string, string | number>): string {
    const sortedTags = Object.keys(tags).sort().map(key => `${key}=${tags[key]}`).join(',');
    return `${metricName}:${sortedTags}`;
  }
  
  private calculatePercentile(values: number[], percentile: number): number {
    if (values.length === 0) return 0;
    
    const sorted = values.slice().sort((a, b) => a - b);
    const index = Math.ceil((percentile / 100) * sorted.length) - 1;
    return sorted[index];
  }
  
  private async applyAggregations(metricName: string, value: number, tags: Record<string, string | number>): Promise<void> {
    // Implementation similar to JavaScript version
  }
  
  private async exportMetrics(metricName: string, value: number, tags: Record<string, string | number>): Promise<void> {
    // Implementation similar to JavaScript version
  }
}
```

## Real-World Examples

### API Performance Monitoring

```tsk
# API endpoint metrics
api_response_time: @metrics("api.endpoint.response_time", 125, {"endpoint": "/users", "method": "GET"})
api_error_rate: @metrics("api.endpoint.error_rate", 0.01, {"endpoint": "/users", "method": "GET"})
api_throughput: @metrics("api.endpoint.requests_per_second", 150, {"endpoint": "/users", "method": "GET"})

# Database performance metrics
db_query_time: @metrics("database.query.duration", 45, {"table": "users", "operation": "SELECT"})
db_connection_pool: @metrics("database.connections.active", 8, {"pool": "main"})
db_cache_hit_rate: @metrics("database.cache.hit_rate", 0.85, {"cache": "query_cache"})
```

### User Behavior Analytics

```tsk
# User engagement metrics
session_duration: @metrics("user.session.duration_minutes", 25, {"user_type": "premium"})
page_views: @metrics("user.page.views", 1, {"page": "/dashboard", "user_type": "active"})
feature_usage: @metrics("user.feature.usage", 1, {"feature": "advanced_search", "user_type": "premium"})

# Conversion metrics
signup_conversion: @metrics("conversion.signup.rate", 0.15, {"source": "organic"})
purchase_conversion: @metrics("conversion.purchase.rate", 0.08, {"product_category": "electronics"})
```

### System Health Monitoring

```tsk
# System resource metrics
cpu_utilization: @metrics("system.cpu.utilization_percent", 65.5, {"core": "all"})
memory_usage: @metrics("system.memory.usage_mb", 2048, {"type": "heap"})
disk_usage: @metrics("system.disk.usage_percent", 75.2, {"partition": "root"})

# Application health metrics
app_uptime: @metrics("application.uptime.seconds", 86400, {"instance": "web-1"})
error_count: @metrics("application.errors.count", 5, {"severity": "high", "instance": "web-1"})
```

## Performance Considerations

### Metrics Batching

```tsk
# Batch metrics for better performance
batched_metrics: @metrics("batch.operations", 100, {"batch_size": "large", "operation": "bulk_insert"})
```

### Metrics Sampling

```javascript
// Implement metrics sampling for high-volume applications
class SampledMetricsManager extends TuskMetricsManager {
  constructor(samplingRate = 0.1) {
    super();
    this.samplingRate = samplingRate;
  }
  
  async record(metricName, value, tags = {}) {
    // Apply sampling
    if (Math.random() > this.samplingRate) {
      return;
    }
    
    // Scale the value based on sampling rate
    const scaledValue = value / this.samplingRate;
    
    return await super.record(metricName, scaledValue, tags);
  }
}
```

## Security Notes

- **Data Privacy**: Ensure metrics don't contain sensitive information
- **Access Control**: Implement proper access controls for metrics endpoints
- **Data Retention**: Implement appropriate data retention policies

```javascript
// Secure metrics implementation
class SecureMetricsManager extends TuskMetricsManager {
  constructor() {
    super();
    this.sensitivePatterns = [
      /password/i,
      /token/i,
      /secret/i,
      /key/i
    ];
  }
  
  async record(metricName, value, tags = {}) {
    // Sanitize metric name and tags
    const sanitizedName = this.sanitizeMetricName(metricName);
    const sanitizedTags = this.sanitizeTags(tags);
    
    return await super.record(sanitizedName, value, sanitizedTags);
  }
  
  sanitizeMetricName(name) {
    // Remove sensitive patterns from metric names
    return this.sensitivePatterns.reduce((sanitized, pattern) => {
      return sanitized.replace(pattern, '[REDACTED]');
    }, name);
  }
  
  sanitizeTags(tags) {
    const sanitized = {};
    
    Object.keys(tags).forEach(key => {
      const value = tags[key];
      
      // Check if key or value contains sensitive information
      if (this.sensitivePatterns.some(pattern => 
        pattern.test(key) || (typeof value === 'string' && pattern.test(value))
      )) {
        sanitized[key] = '[REDACTED]';
      } else {
        sanitized[key] = value;
      }
    });
    
    return sanitized;
  }
}
```

## Best Practices

1. **Metric Naming**: Use consistent and descriptive metric names
2. **Tagging Strategy**: Use tags for filtering and grouping metrics
3. **Sampling**: Implement sampling for high-volume metrics
4. **Aggregation**: Use appropriate aggregation methods
5. **Monitoring**: Set up alerts for critical metrics
6. **Documentation**: Document all metrics and their meanings

## Next Steps

- Learn about [@learn function](./048-at-learn-function-javascript.md) for machine learning integration
- Explore [@optimize function](./049-at-optimize-function-javascript.md) for performance optimization
- Master [@http host](./050-at-http-host-javascript.md) for HTTP operations 