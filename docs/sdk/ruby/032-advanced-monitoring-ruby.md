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

# Cache metrics
cache_hit_rate: @metrics("cache_hit_rate", @cache.hit_rate)
cache_miss_rate: @metrics("cache_miss_rate", @cache.miss_rate)
cache_evictions: @metrics("cache_evictions", @cache.evictions)
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
```

## 🚨 Alerting

### 1. Alert Rules
```ruby
# config/alerts.tsk
[alerts]
# Performance alerts
high_response_time: @alert("avg_response_time_ms > 500", {
    severity: "warning",
    message: "Average response time is above 500ms",
    notification: ["slack", "email"]
})

high_error_rate: @alert("error_rate > 0.05", {
    severity: "critical",
    message: "Error rate is above 5%",
    notification: ["slack", "email", "pagerduty"]
})

# Business alerts
low_conversion_rate: @alert("conversion_rate < 0.02", {
    severity: "warning",
    message: "Conversion rate is below 2%",
    notification: ["slack", "email"]
})

# Infrastructure alerts
high_db_connections: @alert("db_connection_pool_size > 80", {
    severity: "warning",
    message: "Database connection pool is 80% full",
    notification: ["slack"]
})
```

### 2. Custom Alert Conditions
```ruby
# config/custom_alerts.tsk
[custom_alerts]
# Complex alert conditions
service_degradation: @alert("avg_response_time_ms > 1000 AND error_rate > 0.1", {
    severity: "critical",
    message: "Service is experiencing severe degradation",
    notification: ["slack", "pagerduty"],
    cooldown: "5m"
})

anomaly_detection: @alert("@anomaly.detect('daily_active_users', 0.3)", {
    severity: "warning",
    message: "Unusual drop in daily active users detected",
    notification: ["slack", "email"]
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
    tags: ["service:myapp", "env:production"]
})

custom.metric.user_count: @metrics("custom.metric.user_count", @query("SELECT COUNT(*) FROM users"), {
    tags: ["service:myapp", "env:production"]
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
```

## 🛠️ Ruby Integration Example
```ruby
# app/services/advanced_monitoring_service.rb
require 'tusklang'

class AdvancedMonitoringService
  def self.load_config
    parser = TuskLang.new
    parser.parse_file('config/advanced_monitoring.tsk')
  end

  def self.record_business_metric(name, value, tags = {})
    config = load_config
    metric_config = config['business_metrics'][name]
    
    if metric_config
      # Send to monitoring platform
      MonitoringPlatform.record_metric(name, value, tags)
    end
  end

  def self.check_alerts
    config = load_config
    alerts = config['alerts']
    
    alerts.each do |alert_name, alert_config|
      if alert_config['condition'].evaluate
        send_alert(alert_name, alert_config)
      end
    end
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
end

# Usage
config = AdvancedMonitoringService.load_config
AdvancedMonitoringService.record_business_metric('daily_active_users', User.active_today.count)
AdvancedMonitoringService.check_alerts
```

## 🛡️ Best Practices
- Define meaningful business and technical metrics.
- Set up appropriate alert thresholds and cooldowns.
- Use tags and labels for metric organization.
- Monitor alert fatigue and adjust thresholds accordingly.

**Ready to monitor everything? Let's Tusk! 🚀** 