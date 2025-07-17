# 📊 TuskLang Ruby Advanced Monitoring Guide

**"We don't bow to any king" - Ruby Edition**

Build comprehensive monitoring for TuskLang-powered Ruby applications. Learn custom metrics, alerting, and integration with popular monitoring platforms.

## 📈 Custom Metrics

### 1. Business Metrics
```ruby
# config/business_metrics.tsk
[business_metrics]
# User engagement metrics
daily_active_users: @metrics("daily_active_users", @query("SELECT COUNT(DISTINCT user_id) FROM user_activities WHERE DATE(created_at) = ?", @date.today()))
monthly_revenue: @metrics("monthly_revenue", @query("SELECT SUM(amount) FROM orders WHERE MONTH(created_at) = ? AND YEAR(created_at) = ?", @date.month(), @date.year()))
conversion_rate: @metrics("conversion_rate", @query("SELECT (COUNT(CASE WHEN status = 'completed' THEN 1 END) * 100.0 / COUNT(*)) FROM orders WHERE created_at > ?", @date.subtract("30d")))

# Feature usage metrics
feature_adoption: @metrics("feature_adoption", @query("SELECT COUNT(DISTINCT user_id) FROM feature_usage WHERE feature_name = ? AND created_at > ?", @request.feature_name, @date.subtract("7d")))
user_retention: @metrics("user_retention", @query("SELECT COUNT(DISTINCT user_id) FROM users WHERE last_login > ? AND created_at < ?", @date.subtract("7d"), @date.subtract("30d")))

# Product metrics
product_views: @metrics("product_views", @query("SELECT COUNT(*) FROM product_views WHERE created_at > ?", @date.subtract("1h")))
cart_abandonment: @metrics("cart_abandonment", @query("SELECT COUNT(*) FROM carts WHERE status = 'abandoned' AND updated_at > ?", @date.subtract("24h")))
```

### 2. Performance Metrics
```ruby
# config/performance_metrics.tsk
[performance_metrics]
# Response time metrics
avg_response_time: @metrics("avg_response_time_ms", @request.response_time)
p95_response_time: @metrics("p95_response_time_ms", @request.p95_response_time)
p99_response_time: @metrics("p99_response_time_ms", @request.p99_response_time)

# Database metrics
db_connection_pool: @metrics("db_connection_pool_size", @query("SELECT COUNT(*) FROM pg_stat_activity"))
db_query_time: @metrics("db_query_time_ms", @query.execution_time)
db_deadlocks: @metrics("db_deadlocks", @query("SELECT COUNT(*) FROM pg_stat_database WHERE deadlocks > 0"))
db_cache_hit_ratio: @metrics("db_cache_hit_ratio", @query("SELECT (sum(heap_blks_hit) / (sum(heap_blks_hit) + sum(heap_blks_read))) * 100 FROM pg_statio_user_tables"))

# Cache metrics
cache_hit_rate: @metrics("cache_hit_rate", @cache.hit_rate)
cache_miss_rate: @metrics("cache_miss_rate", @cache.miss_rate)
cache_evictions: @metrics("cache_evictions", @cache.evictions)
cache_memory_usage: @metrics("cache_memory_usage_mb", @cache.memory_usage)

# System metrics
cpu_usage: @metrics("cpu_usage_percent", @system.cpu_usage)
memory_usage: @metrics("memory_usage_percent", @system.memory_usage)
disk_usage: @metrics("disk_usage_percent", @system.disk_usage)
network_io: @metrics("network_io_mbps", @system.network_io)
```

### 3. Error Metrics
```ruby
# config/error_metrics.tsk
[error_metrics]
# Error rate metrics
error_rate: @metrics("error_rate", @request.error_count / @request.total_count)
exception_count: @metrics("exception_count", @request.exception_count)
timeout_count: @metrics("timeout_count", @request.timeout_count)

# Specific error metrics
validation_errors: @metrics("validation_errors", @request.validation_error_count)
authentication_errors: @metrics("authentication_errors", @request.auth_error_count)
authorization_errors: @metrics("authorization_errors", @request.authorization_error_count)
database_errors: @metrics("database_errors", @request.database_error_count)
cache_errors: @metrics("cache_errors", @request.cache_error_count)

# Error by type
sql_errors: @metrics("sql_errors", @query("SELECT COUNT(*) FROM error_logs WHERE error_type = 'sql' AND created_at > ?", @date.subtract("1h")))
network_errors: @metrics("network_errors", @query("SELECT COUNT(*) FROM error_logs WHERE error_type = 'network' AND created_at > ?", @date.subtract("1h")))
```

## 🚨 Advanced Alerting

### 1. Complex Alert Rules
```ruby
# config/advanced_alerts.tsk
[advanced_alerts]
# Performance alerts
high_response_time: @alert("avg_response_time_ms > 500 AND p95_response_time_ms > 1000", {
    severity: "warning",
    message: "Response time is degrading",
    notification: ["slack", "email"],
    cooldown: "5m"
})

high_error_rate: @alert("error_rate > 0.05 OR exception_count > 10", {
    severity: "critical",
    message: "Error rate is above threshold",
    notification: ["slack", "email", "pagerduty"],
    cooldown: "2m"
})

# Business alerts
low_conversion_rate: @alert("conversion_rate < 0.02 AND daily_active_users > 1000", {
    severity: "warning",
    message: "Conversion rate is below 2% with high traffic",
    notification: ["slack", "email"]
})

revenue_drop: @alert("monthly_revenue < @query('SELECT AVG(monthly_revenue) * 0.8 FROM revenue_history WHERE month < ?', @date.month())", {
    severity: "critical",
    message: "Revenue has dropped significantly",
    notification: ["slack", "email", "pagerduty"]
})

# Infrastructure alerts
high_resource_usage: @alert("cpu_usage_percent > 80 OR memory_usage_percent > 85", {
    severity: "warning",
    message: "High resource usage detected",
    notification: ["slack", "email"]
})

database_issues: @alert("db_connection_pool_size > 80 OR db_cache_hit_ratio < 0.8", {
    severity: "warning",
    message: "Database performance issues detected",
    notification: ["slack", "email"]
})
```

### 2. Anomaly Detection
```ruby
# config/anomaly_detection.tsk
[anomaly_detection]
enabled: true
sensitivity: 0.3

[anomaly_alerts]
traffic_anomaly: @alert("@anomaly.detect('daily_active_users', 0.3)", {
    severity: "warning",
    message: "Unusual traffic pattern detected",
    notification: ["slack", "email"]
})

revenue_anomaly: @alert("@anomaly.detect('monthly_revenue', 0.2)", {
    severity: "critical",
    message: "Unusual revenue pattern detected",
    notification: ["slack", "email", "pagerduty"]
})

error_anomaly: @alert("@anomaly.detect('error_rate', 0.4)", {
    severity: "critical",
    message: "Unusual error pattern detected",
    notification: ["slack", "pagerduty"]
})
```

## 🔗 Monitoring Platform Integration

### 1. Prometheus Integration
```ruby
# config/prometheus.tsk
[prometheus]
enabled: true
port: 9090
path: "/metrics"

[metrics]
# Prometheus-compatible metrics
http_requests_total: @metrics("http_requests_total", @request.count, {
    labels: ["method", "endpoint", "status"]
})

http_request_duration_seconds: @metrics("http_request_duration_seconds", @request.response_time / 1000.0, {
    labels: ["method", "endpoint"]
})

db_connections_active: @metrics("db_connections_active", @query("SELECT COUNT(*) FROM pg_stat_activity"))

cache_hit_ratio: @metrics("cache_hit_ratio", @cache.hit_rate, {
    labels: ["cache_type"]
})

business_revenue_total: @metrics("business_revenue_total", @query("SELECT SUM(amount) FROM orders WHERE status = 'completed'"), {
    labels: ["currency"]
})
```

### 2. Datadog Integration
```ruby
# config/datadog.tsk
[datadog]
enabled: true
api_key: @env.secure("DATADOG_API_KEY")
app_key: @env.secure("DATADOG_APP_KEY")

[metrics]
# Datadog metrics
custom.metric.response_time: @metrics("custom.metric.response_time", @request.response_time, {
    tags: ["service:myapp", "env:production", "endpoint:#{@request.endpoint}"]
})

custom.metric.user_count: @metrics("custom.metric.user_count", @query("SELECT COUNT(*) FROM users"), {
    tags: ["service:myapp", "env:production"]
})

custom.metric.revenue: @metrics("custom.metric.revenue", @query("SELECT SUM(amount) FROM orders WHERE status = 'completed'"), {
    tags: ["service:myapp", "env:production", "currency:usd"]
})

# Custom events
deployment_event: @event("deployment", {
    title: "Application deployed",
    text: "Version #{@env('APP_VERSION')} deployed to #{@env('ENVIRONMENT')}",
    tags: ["service:myapp", "env:#{@env('ENVIRONMENT')}", "version:#{@env('APP_VERSION')}"]
})
```

### 3. New Relic Integration
```ruby
# config/newrelic.tsk
[newrelic]
enabled: true
license_key: @env.secure("NEWRELIC_LICENSE_KEY")
app_name: "MyApp"

[metrics]
# New Relic custom metrics
Custom/ResponseTime: @metrics("Custom/ResponseTime", @request.response_time)
Custom/UserCount: @metrics("Custom/UserCount", @query("SELECT COUNT(*) FROM users"))
Custom/ErrorRate: @metrics("Custom/ErrorRate", @request.error_rate)
Custom/Revenue: @metrics("Custom/Revenue", @query("SELECT SUM(amount) FROM orders WHERE status = 'completed'"))

# Custom events
Custom/UserRegistration: @event("UserRegistration", {
    user_id: @request.user_id,
    email: @request.email,
    source: @request.source
})

Custom/OrderCompleted: @event("OrderCompleted", {
    order_id: @request.order_id,
    amount: @request.amount,
    user_id: @request.user_id
})
```

## 🛠️ Ruby Integration Example
```ruby
# app/services/advanced_monitoring_service.rb
require 'tusklang'

class AdvancedMonitoringService
  def self.load_monitoring_config
    parser = TuskLang.new
    parser.parse_file('config/advanced_monitoring.tsk')
  end

  def self.record_business_metric(name, value, tags = {})
    config = load_monitoring_config
    metric_config = config['business_metrics'][name]
    
    if metric_config
      # Send to monitoring platform
      MonitoringPlatform.record_metric(name, value, tags)
    end
  end

  def self.record_performance_metric(name, value, tags = {})
    config = load_monitoring_config
    metric_config = config['performance_metrics'][name]
    
    if metric_config
      # Send to monitoring platform
      MonitoringPlatform.record_metric(name, value, tags)
    end
  end

  def self.check_alerts
    config = load_monitoring_config
    alerts = config['advanced_alerts']
    
    alerts.each do |alert_name, alert_config|
      if alert_config['condition'].evaluate
        send_alert(alert_name, alert_config)
      end
    end
  end

  def self.detect_anomalies
    config = load_monitoring_config
    
    if config['anomaly_detection']['enabled']
      anomalies = config['anomaly_alerts']
      
      anomalies.each do |anomaly_name, anomaly_config|
        if anomaly_config['condition'].evaluate
          send_alert(anomaly_name, anomaly_config)
        end
      end
    end
  end

  def self.generate_monitoring_report
    config = load_monitoring_config
    
    # Collect all metrics
    business_metrics = collect_business_metrics(config['business_metrics'])
    performance_metrics = collect_performance_metrics(config['performance_metrics'])
    error_metrics = collect_error_metrics(config['error_metrics'])
    
    # Generate report
    {
      timestamp: Time.current,
      business_metrics: business_metrics,
      performance_metrics: performance_metrics,
      error_metrics: error_metrics,
      alerts: get_active_alerts(),
      anomalies: get_detected_anomalies()
    }
  end

  private

  def self.send_alert(alert_name, alert_config)
    message = alert_config['message']
    severity = alert_config['severity']
    notifications = alert_config['notification']
    
    notifications.each do |notification|
      case notification
      when 'slack'
        SlackNotifier.send(message, severity)
      when 'email'
        EmailNotifier.send(message, severity)
      when 'pagerduty'
        PagerDutyNotifier.send(message, severity)
      end
    end
  end

  def self.collect_business_metrics(metrics_config)
    metrics_config.map do |name, config|
      { name: name, value: config.evaluate }
    end
  end

  def self.collect_performance_metrics(metrics_config)
    metrics_config.map do |name, config|
      { name: name, value: config.evaluate }
    end
  end

  def self.collect_error_metrics(metrics_config)
    metrics_config.map do |name, config|
      { name: name, value: config.evaluate }
    end
  end

  def self.get_active_alerts
    # Implementation to get active alerts
    []
  end

  def self.get_detected_anomalies
    # Implementation to get detected anomalies
    []
  end
end

# Usage
config = AdvancedMonitoringService.load_monitoring_config
AdvancedMonitoringService.record_business_metric('daily_active_users', User.active_today.count)
AdvancedMonitoringService.record_performance_metric('avg_response_time', 150)
AdvancedMonitoringService.check_alerts
AdvancedMonitoringService.detect_anomalies
report = AdvancedMonitoringService.generate_monitoring_report
```

## 🛡️ Best Practices
- Define meaningful business and technical metrics.
- Set up appropriate alert thresholds and cooldowns.
- Use tags and labels for metric organization.
- Monitor alert fatigue and adjust thresholds accordingly.
- Implement anomaly detection for proactive monitoring.

**Ready to monitor everything? Let's Tusk! 🚀** 