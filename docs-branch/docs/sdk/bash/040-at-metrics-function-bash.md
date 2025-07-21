# 📊 TuskLang Bash @metrics Function Guide

**"We don't bow to any king" – Metrics are your configuration's pulse.**

The @metrics function in TuskLang is your monitoring and analytics powerhouse, enabling comprehensive metrics collection, analysis, and visualization directly within your configuration files. Whether you're tracking performance, monitoring system health, or analyzing user behavior, @metrics provides the insights and intelligence to understand and optimize your systems.

## 🎯 What is @metrics?
The @metrics function provides metrics collection and analysis in TuskLang. It offers:
- **Metrics collection** - Collect and store system and application metrics
- **Real-time monitoring** - Monitor metrics in real-time with alerts
- **Historical analysis** - Analyze trends and patterns over time
- **Performance tracking** - Track response times, throughput, and efficiency
- **Custom metrics** - Define and track custom business metrics

## 📝 Basic @metrics Syntax

### Simple Metrics Collection
```ini
[simple_metrics]
# Collect basic system metrics
cpu_usage: @metrics.collect("cpu_usage", @shell("top -bn1 | grep 'Cpu(s)' | awk '{print $2}' | cut -d'%' -f1"))
memory_usage: @metrics.collect("memory_usage", @shell("free | grep Mem | awk '{printf \"%.2f\", $3/$2 * 100.0}'"))
disk_usage: @metrics.collect("disk_usage", @shell("df -h / | awk 'NR==2{print $5}' | cut -d'%' -f1"))

# Collect application metrics
response_time: @metrics.collect("response_time", @query("SELECT AVG(response_time) FROM api_logs WHERE created_at >= DATE_SUB(NOW(), INTERVAL 5 MINUTE)"))
error_rate: @metrics.collect("error_rate", @query("SELECT COUNT(*) * 1.0 / (SELECT COUNT(*) FROM api_logs WHERE created_at >= DATE_SUB(NOW(), INTERVAL 5 MINUTE)) FROM api_logs WHERE status_code >= 400 AND created_at >= DATE_SUB(NOW(), INTERVAL 5 MINUTE)"))
```

### Custom Metrics
```ini
[custom_metrics]
# Define custom business metrics
active_users: @metrics.collect("active_users", @query("SELECT COUNT(*) FROM active_sessions"))
orders_per_hour: @metrics.collect("orders_per_hour", @query("SELECT COUNT(*) FROM orders WHERE created_at >= DATE_SUB(NOW(), INTERVAL 1 HOUR)"))
revenue_today: @metrics.collect("revenue_today", @query("SELECT SUM(amount) FROM orders WHERE DATE(created_at) = CURDATE()"))

# Custom calculated metrics
conversion_rate: @metrics.calculate("conversion_rate", {
    "numerator": @metrics.get("orders_per_hour"),
    "denominator": @metrics.get("active_users"),
    "multiplier": 100
})
```

### Metrics Analysis
```ini
[metrics_analysis]
# Analyze metrics over time
cpu_trend: @metrics.trend("cpu_usage", "1h")
memory_trend: @metrics.trend("memory_usage", "24h")
response_time_trend: @metrics.trend("response_time", "7d")

# Calculate statistics
cpu_stats: @metrics.stats("cpu_usage", "24h")
memory_stats: @metrics.stats("memory_usage", "24h")
response_time_stats: @metrics.stats("response_time", "24h")
```

## 🚀 Quick Start Example

```bash
#!/bin/bash
source tusk-bash.sh

cat > metrics-quickstart.tsk << 'EOF'
[basic_monitoring]
# Collect basic system metrics
cpu_usage: @metrics.collect("cpu_usage", @shell("top -bn1 | grep 'Cpu(s)' | awk '{print $2}' | cut -d'%' -f1"))
memory_usage: @metrics.collect("memory_usage", @shell("free | grep Mem | awk '{printf \"%.2f\", $3/$2 * 100.0}'"))
disk_usage: @metrics.collect("disk_usage", @shell("df -h / | awk 'NR==2{print $5}' | cut -d'%' -f1"))

[application_metrics]
# Collect application metrics
active_connections: @metrics.collect("active_connections", @shell("netstat -an | wc -l"))
process_count: @metrics.collect("process_count", @shell("ps aux | wc -l"))
load_average: @metrics.collect("load_average", @shell("uptime | awk -F'load average:' '{print $2}'"))

[performance_metrics]
# Performance tracking
response_time: @metrics.collect("response_time", 150)  # milliseconds
throughput: @metrics.collect("throughput", 1000)  # requests per second
error_count: @metrics.collect("error_count", 5)

# Calculate derived metrics
error_rate: @metrics.calculate("error_rate", {
    "numerator": $error_count,
    "denominator": $throughput,
    "multiplier": 100
})

[metrics_analysis]
# Analyze trends
cpu_trend: @metrics.trend("cpu_usage", "1h")
memory_trend: @metrics.trend("memory_usage", "1h")

# Get statistics
cpu_stats: @metrics.stats("cpu_usage", "24h")
memory_stats: @metrics.stats("memory_usage", "24h")
EOF

config=$(tusk_parse metrics-quickstart.tsk)

echo "=== Basic Monitoring ==="
echo "CPU Usage: $(tusk_get "$config" basic_monitoring.cpu_usage)"
echo "Memory Usage: $(tusk_get "$config" basic_monitoring.memory_usage)"
echo "Disk Usage: $(tusk_get "$config" basic_monitoring.disk_usage)"

echo ""
echo "=== Application Metrics ==="
echo "Active Connections: $(tusk_get "$config" application_metrics.active_connections)"
echo "Process Count: $(tusk_get "$config" application_metrics.process_count)"
echo "Load Average: $(tusk_get "$config" application_metrics.load_average)"

echo ""
echo "=== Performance Metrics ==="
echo "Response Time: $(tusk_get "$config" performance_metrics.response_time)"
echo "Throughput: $(tusk_get "$config" performance_metrics.throughput)"
echo "Error Rate: $(tusk_get "$config" performance_metrics.error_rate)"

echo ""
echo "=== Metrics Analysis ==="
echo "CPU Trend: $(tusk_get "$config" metrics_analysis.cpu_trend)"
echo "Memory Trend: $(tusk_get "$config" metrics_analysis.memory_trend)"
echo "CPU Stats: $(tusk_get "$config" metrics_analysis.cpu_stats)"
echo "Memory Stats: $(tusk_get "$config" metrics_analysis.memory_stats)"
```

## 🔗 Real-World Use Cases

### 1. System Health Monitoring
```ini
[system_health]
# Comprehensive system monitoring
$system_metrics: {
    "cpu_usage": @metrics.collect("cpu_usage", @shell("top -bn1 | grep 'Cpu(s)' | awk '{print $2}' | cut -d'%' -f1")),
    "memory_usage": @metrics.collect("memory_usage", @shell("free | grep Mem | awk '{printf \"%.2f\", $3/$2 * 100.0}'")),
    "disk_usage": @metrics.collect("disk_usage", @shell("df -h / | awk 'NR==2{print $5}' | cut -d'%' -f1")),
    "load_average": @metrics.collect("load_average", @shell("uptime | awk -F'load average:' '{print $2}' | awk '{print $1}'")),
    "network_connections": @metrics.collect("network_connections", @shell("netstat -an | wc -l"))
}

# Health score calculation
health_score: @metrics.calculate("system_health_score", {
    "cpu_weight": 0.3,
    "memory_weight": 0.3,
    "disk_weight": 0.2,
    "load_weight": 0.1,
    "network_weight": 0.1,
    "cpu_value": @math(100 - $system_metrics.cpu_usage),
    "memory_value": @math(100 - $system_metrics.memory_usage),
    "disk_value": @math(100 - $system_metrics.disk_usage),
    "load_value": @if($system_metrics.load_average < 1, 100, @math(100 - ($system_metrics.load_average * 20))),
    "network_value": @if($system_metrics.network_connections < 1000, 100, @math(100 - ($system_metrics.network_connections / 10)))
})

# Health alerts
health_status: @if($health_score >= 90, "excellent", @if($health_score >= 70, "good", @if($health_score >= 50, "fair", "poor")))
alert_needed: @validate.less_than($health_score, 70)
```

### 2. Application Performance Monitoring
```ini
[app_performance]
# Application performance metrics
$performance_metrics: {
    "response_time": @metrics.collect("response_time", @query("SELECT AVG(response_time) FROM api_logs WHERE created_at >= DATE_SUB(NOW(), INTERVAL 5 MINUTE)")),
    "throughput": @metrics.collect("throughput", @query("SELECT COUNT(*) FROM api_logs WHERE created_at >= DATE_SUB(NOW(), INTERVAL 1 MINUTE)")),
    "error_rate": @metrics.collect("error_rate", @query("SELECT COUNT(*) * 1.0 / (SELECT COUNT(*) FROM api_logs WHERE created_at >= DATE_SUB(NOW(), INTERVAL 5 MINUTE)) FROM api_logs WHERE status_code >= 400 AND created_at >= DATE_SUB(NOW(), INTERVAL 5 MINUTE)")),
    "active_sessions": @metrics.collect("active_sessions", @query("SELECT COUNT(*) FROM active_sessions")),
    "database_connections": @metrics.collect("db_connections", @query("SELECT COUNT(*) FROM information_schema.processlist"))
}

# Performance trends
response_time_trend: @metrics.trend("response_time", "1h")
throughput_trend: @metrics.trend("throughput", "1h")
error_rate_trend: @metrics.trend("error_rate", "1h")

# Performance alerts
performance_alerts: {
    "high_response_time": @validate.greater_than($performance_metrics.response_time, 1000),
    "low_throughput": @validate.less_than($performance_metrics.throughput, 100),
    "high_error_rate": @validate.greater_than($performance_metrics.error_rate, 0.05)
}
```

### 3. Business Metrics Tracking
```ini
[business_metrics]
# Business-critical metrics
$business_metrics: {
    "active_users": @metrics.collect("active_users", @query("SELECT COUNT(*) FROM active_sessions")),
    "orders_per_hour": @metrics.collect("orders_per_hour", @query("SELECT COUNT(*) FROM orders WHERE created_at >= DATE_SUB(NOW(), INTERVAL 1 HOUR)")),
    "revenue_today": @metrics.collect("revenue_today", @query("SELECT SUM(amount) FROM orders WHERE DATE(created_at) = CURDATE()")),
    "conversion_rate": @metrics.collect("conversion_rate", @query("SELECT (COUNT(DISTINCT o.user_id) * 1.0 / COUNT(DISTINCT s.user_id)) * 100 FROM sessions s LEFT JOIN orders o ON s.user_id = o.user_id WHERE s.created_at >= DATE_SUB(NOW(), INTERVAL 24 HOUR)")),
    "customer_satisfaction": @metrics.collect("satisfaction", @query("SELECT AVG(rating) FROM reviews WHERE created_at >= DATE_SUB(NOW(), INTERVAL 7 DAY)"))
}

# Business trends
user_growth: @metrics.trend("active_users", "7d")
revenue_trend: @metrics.trend("revenue_today", "30d")
conversion_trend: @metrics.trend("conversion_rate", "7d")

# Business alerts
business_alerts: {
    "low_conversion": @validate.less_than($business_metrics.conversion_rate, 2.0),
    "declining_users": @metrics.trend_direction("active_users", "7d") == "declining",
    "revenue_drop": @metrics.trend_direction("revenue_today", "7d") == "declining"
}
```

### 4. Infrastructure Monitoring
```ini
[infrastructure_monitoring]
# Infrastructure metrics
$infrastructure_metrics: {
    "server_count": @metrics.collect("server_count", @query("SELECT COUNT(*) FROM servers WHERE active = 1")),
    "database_size": @metrics.collect("db_size", @query("SELECT SUM(data_length + index_length) / 1024 / 1024 FROM information_schema.tables")),
    "backup_status": @metrics.collect("backup_status", @if(@file.exists("/var/backups/latest.sql"), 1, 0)),
    "ssl_expiry": @metrics.collect("ssl_expiry", @shell("openssl x509 -enddate -noout -in /etc/ssl/certs/app.crt | cut -d= -f2")),
    "disk_space": @metrics.collect("disk_space", @shell("df -h / | awk 'NR==2{print $4}' | sed 's/G//'"))
}

# Infrastructure health
infrastructure_health: @metrics.calculate("infrastructure_health", {
    "server_weight": 0.3,
    "database_weight": 0.3,
    "backup_weight": 0.2,
    "ssl_weight": 0.1,
    "disk_weight": 0.1,
    "server_value": @if($infrastructure_metrics.server_count > 0, 100, 0),
    "database_value": @if($infrastructure_metrics.database_size < 1000, 100, @math(100 - ($infrastructure_metrics.database_size / 10))),
    "backup_value": $infrastructure_metrics.backup_status * 100,
    "ssl_value": @if(@date.diff($infrastructure_metrics.ssl_expiry, @date("Y-m-d")) > 30, 100, 50),
    "disk_value": @if($infrastructure_metrics.disk_space > 10, 100, @math($infrastructure_metrics.disk_space * 10))
})
```

## 🧠 Advanced @metrics Patterns

### Real-Time Alerting
```ini
[real_time_alerts]
# Real-time alerting system
$alert_thresholds: {
    "cpu_critical": 90,
    "cpu_warning": 80,
    "memory_critical": 95,
    "memory_warning": 85,
    "disk_critical": 95,
    "disk_warning": 85
}

# Check thresholds
alerts: {
    "cpu_critical": @validate.greater_than(@metrics.get("cpu_usage"), $alert_thresholds.cpu_critical),
    "cpu_warning": @validate.greater_than(@metrics.get("cpu_usage"), $alert_thresholds.cpu_warning),
    "memory_critical": @validate.greater_than(@metrics.get("memory_usage"), $alert_thresholds.memory_critical),
    "memory_warning": @validate.greater_than(@metrics.get("memory_usage"), $alert_thresholds.memory_warning),
    "disk_critical": @validate.greater_than(@metrics.get("disk_usage"), $alert_thresholds.disk_critical),
    "disk_warning": @validate.greater_than(@metrics.get("disk_usage"), $alert_thresholds.disk_warning)
}

# Send alerts
@if($alerts.cpu_critical || $alerts.memory_critical || $alerts.disk_critical, {
    "alert": "critical",
    "message": "Critical system metrics exceeded",
    "metrics": {
        "cpu": @metrics.get("cpu_usage"),
        "memory": @metrics.get("memory_usage"),
        "disk": @metrics.get("disk_usage")
    },
    "timestamp": @date("Y-m-d H:i:s")
}, "no_critical_alerts")
```

### Metrics Aggregation
```ini
[metrics_aggregation]
# Aggregate metrics across multiple servers
$servers: @array.from_query("SELECT hostname FROM servers WHERE active = 1")
$server_metrics: @array.map($servers, {
    "server": item,
    "cpu": @metrics.collect("cpu_" + item, @shell("ssh " + item + " 'top -bn1 | grep \"Cpu(s)\" | awk \"{print \\$2}\" | cut -d\"%\" -f1'")),
    "memory": @metrics.collect("memory_" + item, @shell("ssh " + item + " 'free | grep Mem | awk \"{printf \\\"%.2f\\\", \\$3/\\$2 * 100.0}\"")),
    "disk": @metrics.collect("disk_" + item, @shell("ssh " + item + " 'df -h / | awk \"NR==2{print \\$5}\" | cut -d\"%\" -f1'"))
})

# Aggregate metrics
aggregated_metrics: {
    "avg_cpu": @array.average(@array.map($server_metrics, item.cpu)),
    "avg_memory": @array.average(@array.map($server_metrics, item.memory)),
    "avg_disk": @array.average(@array.map($server_metrics, item.disk)),
    "max_cpu": @array.max(@array.map($server_metrics, item.cpu)),
    "max_memory": @array.max(@array.map($server_metrics, item.memory)),
    "max_disk": @array.max(@array.map($server_metrics, item.disk))
}
```

### Predictive Analytics
```ini
[predictive_analytics]
# Use metrics for predictive analytics
$historical_metrics: @metrics.history("cpu_usage", "7d")
$prediction_model: @learn("cpu_prediction", $historical_metrics)

# Predict future usage
predicted_cpu: @learn.predict("cpu_prediction", {
    "time": @date("H"),
    "day_of_week": @date("N"),
    "current_cpu": @metrics.get("cpu_usage")
})

# Proactive scaling
scaling_needed: @validate.greater_than($predicted_cpu, 80)
scaling_action: @if($scaling_needed, {
    "action": "proactive_scale",
    "reason": "predicted_high_usage",
    "predicted_cpu": $predicted_cpu,
    "timestamp": @date("Y-m-d H:i:s")
}, "no_scaling_needed")
```

## 🛡️ Security & Performance Notes
- **Data privacy:** Ensure sensitive metrics are properly anonymized
- **Storage efficiency:** Use appropriate retention policies for metrics data
- **Performance impact:** Monitor the overhead of metrics collection
- **Access control:** Restrict access to sensitive metrics data
- **Data validation:** Validate metrics data for accuracy and consistency
- **Backup strategy:** Implement regular backups of metrics data

## 🐞 Troubleshooting
- **Missing metrics:** Check data sources and collection intervals
- **Performance issues:** Optimize metrics collection frequency
- **Storage problems:** Monitor metrics storage usage and implement cleanup
- **Alert fatigue:** Tune alert thresholds to reduce false positives
- **Data accuracy:** Validate metrics data sources and calculations

## 💡 Best Practices
- **Define clear metrics:** Establish clear definitions for all metrics
- **Set appropriate thresholds:** Configure meaningful alert thresholds
- **Monitor trends:** Focus on trends rather than absolute values
- **Document metrics:** Document the purpose and calculation of each metric
- **Regular review:** Regularly review and update metrics and thresholds
- **Automate responses:** Implement automated responses to common alerts

## 🔗 Cross-References
- [@ Operator Introduction](024-at-operator-intro-bash.md)
- [@learn Function](039-at-learn-function-bash.md)
- [@cache Function](033-at-cache-function-bash.md)
- [Performance Optimization](095-performance-optimization-bash.md)
- [Monitoring and Alerting](096-monitoring-alerting-bash.md)

---

**Master @metrics in TuskLang and gain deep insights into your system's performance. 📊** 