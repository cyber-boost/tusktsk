# 📊 Monitoring & Observability - Python

**"We don't bow to any king" - Monitoring Edition**

TuskLang provides comprehensive monitoring and observability features to ensure your applications run smoothly and efficiently.

## 📈 Metrics Collection

### Application Metrics

```python
from tsk import TSK
import time
import psutil

# Monitoring configuration
monitoring_config = TSK.from_string("""
[metrics]
# Application metrics
request_count: @metric("counter", "requests_total")
response_time: @metric("histogram", "response_time_seconds")
error_count: @metric("counter", "errors_total")
active_connections: @metric("gauge", "active_connections")

# Business metrics
user_registrations: @metric("counter", "user_registrations_total")
payment_volume: @metric("counter", "payment_volume_total")
api_calls: @metric("counter", "api_calls_total")

collect_metrics_fujsen = '''
def collect_metrics():
    # System metrics
    cpu_percent = psutil.cpu_percent(interval=1)
    memory = psutil.virtual_memory()
    disk = psutil.disk_usage('/')
    
    # Application metrics
    app_metrics = {
        'cpu_usage': cpu_percent,
        'memory_usage': memory.percent,
        'disk_usage': disk.percent,
        'memory_available': memory.available,
        'disk_free': disk.free
    }
    
    # Record metrics
    for metric_name, value in app_metrics.items():
        record_metric(metric_name, value)
    
    return app_metrics
'''

record_metric_fujsen = '''
def record_metric(metric_name, value, labels=None):
    # Record metric to monitoring system
    metric_data = {
        'name': metric_name,
        'value': value,
        'timestamp': time.time(),
        'labels': labels or {}
    }
    
    # Store in database
    execute("""
        INSERT INTO metrics (metric_name, value, timestamp, labels)
        VALUES (?, ?, datetime('now'), ?)
    """, metric_name, value, json.dumps(labels or {}))
    
    return metric_data
'''

get_metrics_fujsen = '''
def get_metrics(metric_name, time_range='1h', aggregation='avg'):
    # Get metrics from database
    if time_range == '1h':
        time_filter = "timestamp > datetime('now', '-1 hour')"
    elif time_range == '24h':
        time_filter = "timestamp > datetime('now', '-1 day')"
    elif time_range == '7d':
        time_filter = "timestamp > datetime('now', '-7 days')"
    else:
        time_filter = "1=1"
    
    if aggregation == 'avg':
        query_str = f"SELECT AVG(value) FROM metrics WHERE metric_name = ? AND {time_filter}"
    elif aggregation == 'max':
        query_str = f"SELECT MAX(value) FROM metrics WHERE metric_name = ? AND {time_filter}"
    elif aggregation == 'min':
        query_str = f"SELECT MIN(value) FROM metrics WHERE metric_name = ? AND {time_filter}"
    elif aggregation == 'sum':
        query_str = f"SELECT SUM(value) FROM metrics WHERE metric_name = ? AND {time_filter}"
    else:
        query_str = f"SELECT value FROM metrics WHERE metric_name = ? AND {time_filter} ORDER BY timestamp DESC LIMIT 100"
    
    results = query(query_str, metric_name)
    
    return [row[0] for row in results]
'''
""")

# Test metrics collection
def test_metrics():
    # Collect system metrics
    metrics = monitoring_config.execute_fujsen('metrics', 'collect_metrics')
    print(f"CPU Usage: {metrics['cpu_usage']}%")
    print(f"Memory Usage: {metrics['memory_usage']}%")
    
    # Record custom metric
    monitoring_config.execute_fujsen('metrics', 'record_metric', 'custom_metric', 42.5, {'service': 'api'})
    
    # Get metrics
    cpu_avg = monitoring_config.execute_fujsen('metrics', 'get_metrics', 'cpu_usage', '1h', 'avg')
    print(f"Average CPU (1h): {cpu_avg[0] if cpu_avg else 'N/A'}")
```

### Custom Business Metrics

```python
# Business metrics configuration
business_metrics = TSK.from_string("""
[business_metrics]
# User engagement metrics
daily_active_users: @metric("gauge", "daily_active_users")
user_session_duration: @metric("histogram", "user_session_duration_seconds")
feature_usage: @metric("counter", "feature_usage_total")

# Financial metrics
revenue_daily: @metric("counter", "revenue_daily_total")
transaction_volume: @metric("counter", "transaction_volume_total")
payment_success_rate: @metric("gauge", "payment_success_rate")

track_user_activity_fujsen = '''
def track_user_activity(user_id, action, duration=None):
    # Track user activity
    activity_data = {
        'user_id': user_id,
        'action': action,
        'timestamp': time.time(),
        'duration': duration
    }
    
    # Record activity
    execute("""
        INSERT INTO user_activities (user_id, action, timestamp, duration)
        VALUES (?, ?, datetime('now'), ?)
    """, user_id, action, duration)
    
    # Update metrics
    if action == 'login':
        increment_metric('daily_active_users', 1)
    elif action == 'logout' and duration:
        record_metric('user_session_duration', duration)
    
    return activity_data
'''

track_feature_usage_fujsen = '''
def track_feature_usage(user_id, feature_name):
    # Track feature usage
    usage_data = {
        'user_id': user_id,
        'feature': feature_name,
        'timestamp': time.time()
    }
    
    # Record usage
    execute("""
        INSERT INTO feature_usage (user_id, feature_name, timestamp)
        VALUES (?, ?, datetime('now'))
    """, user_id, feature_name)
    
    # Increment counter
    increment_metric('feature_usage', 1, {'feature': feature_name})
    
    return usage_data
'''

track_payment_fujsen = '''
def track_payment(amount, success, payment_method):
    # Track payment metrics
    payment_data = {
        'amount': amount,
        'success': success,
        'payment_method': payment_method,
        'timestamp': time.time()
    }
    
    # Record payment
    execute("""
        INSERT INTO payments (amount, success, payment_method, timestamp)
        VALUES (?, ?, ?, datetime('now'))
    """, amount, success, payment_method)
    
    # Update metrics
    if success:
        increment_metric('revenue_daily', amount)
        increment_metric('transaction_volume', 1)
    
    # Calculate success rate
    success_rate = calculate_payment_success_rate()
    record_metric('payment_success_rate', success_rate)
    
    return payment_data
'''

calculate_payment_success_rate_fujsen = '''
def calculate_payment_success_rate():
    # Calculate payment success rate for last 24 hours
    total_payments = query("""
        SELECT COUNT(*) FROM payments 
        WHERE timestamp > datetime('now', '-1 day')
    """)[0][0]
    
    successful_payments = query("""
        SELECT COUNT(*) FROM payments 
        WHERE success = 1 AND timestamp > datetime('now', '-1 day')
    """)[0][0]
    
    if total_payments == 0:
        return 0
    
    return (successful_payments / total_payments) * 100
'''
""")
```

## 📊 Logging & Tracing

### Structured Logging

```python
# Logging configuration
logging_config = TSK.from_string("""
[logging]
# Log levels
log_level: @env("LOG_LEVEL", "info")
log_format: @env("LOG_FORMAT", "json")
log_file: @env("LOG_FILE", "/app/logs/app.log")

# Logging functions
log_info_fujsen = '''
def log_info(message, context=None):
    log_entry = {
        'level': 'INFO',
        'message': message,
        'timestamp': time.time(),
        'context': context or {}
    }
    
    # Write to database
    execute("""
        INSERT INTO application_logs (level, message, timestamp, context)
        VALUES (?, ?, datetime('now'), ?)
    """, 'INFO', message, json.dumps(context or {}))
    
    # Write to file if configured
    if log_file:
        write_log_to_file(log_entry)
    
    return log_entry
'''

log_error_fujsen = '''
def log_error(message, error=None, context=None):
    log_entry = {
        'level': 'ERROR',
        'message': message,
        'error': str(error) if error else None,
        'timestamp': time.time(),
        'context': context or {}
    }
    
    # Write to database
    execute("""
        INSERT INTO application_logs (level, message, error, timestamp, context)
        VALUES (?, ?, ?, datetime('now'), ?)
    """, 'ERROR', message, str(error) if error else None, json.dumps(context or {}))
    
    # Write to file if configured
    if log_file:
        write_log_to_file(log_entry)
    
    # Increment error counter
    increment_metric('errors_total', 1)
    
    return log_entry
'''

log_request_fujsen = '''
def log_request(request_id, method, path, status_code, duration, user_id=None):
    log_entry = {
        'level': 'INFO',
        'message': 'HTTP Request',
        'request_id': request_id,
        'method': method,
        'path': path,
        'status_code': status_code,
        'duration': duration,
        'user_id': user_id,
        'timestamp': time.time()
    }
    
    # Write to database
    execute("""
        INSERT INTO request_logs (request_id, method, path, status_code, duration, user_id, timestamp)
        VALUES (?, ?, ?, ?, ?, ?, datetime('now'))
    """, request_id, method, path, status_code, duration, user_id)
    
    # Update metrics
    increment_metric('requests_total', 1)
    record_metric('response_time', duration)
    
    if status_code >= 400:
        increment_metric('errors_total', 1)
    
    return log_entry
'''

write_log_to_file_fujsen = '''
def write_log_to_file(log_entry):
    import json
    
    if log_format == 'json':
        log_line = json.dumps(log_entry) + '\\n'
    else:
        log_line = f"[{log_entry['timestamp']}] {log_entry['level']}: {log_entry['message']}\\n"
    
    with open(log_file, 'a') as f:
        f.write(log_line)
'''
""")
```

### Distributed Tracing

```python
# Tracing configuration
tracing_config = TSK.from_string("""
[tracing]
# Trace configuration
trace_enabled: @env("TRACE_ENABLED", true)
trace_sampler: @env("TRACE_SAMPLER", 0.1)

start_trace_fujsen = '''
def start_trace(operation_name, trace_id=None, parent_id=None):
    if not trace_enabled:
        return None
    
    # Generate trace ID if not provided
    if not trace_id:
        trace_id = generate_trace_id()
    
    # Create span
    span = {
        'trace_id': trace_id,
        'span_id': generate_span_id(),
        'parent_id': parent_id,
        'operation_name': operation_name,
        'start_time': time.time(),
        'tags': {}
    }
    
    # Store span
    execute("""
        INSERT INTO traces (trace_id, span_id, parent_id, operation_name, start_time, tags)
        VALUES (?, ?, ?, ?, datetime('now'), ?)
    """, trace_id, span['span_id'], parent_id, operation_name, json.dumps({}))
    
    return span
'''

end_trace_fujsen = '''
def end_trace(span, status='OK', error=None):
    if not span:
        return
    
    end_time = time.time()
    duration = end_time - span['start_time']
    
    # Update span
    execute("""
        UPDATE traces 
        SET end_time = datetime('now'), duration = ?, status = ?, error = ?
        WHERE trace_id = ? AND span_id = ?
    """, duration, status, str(error) if error else None, span['trace_id'], span['span_id'])
    
    # Record trace metric
    record_metric('trace_duration', duration, {'operation': span['operation_name']})
    
    return {
        'trace_id': span['trace_id'],
        'span_id': span['span_id'],
        'duration': duration,
        'status': status
    }
'''

add_trace_tag_fujsen = '''
def add_trace_tag(span, key, value):
    if not span:
        return
    
    # Update tags
    current_tags = query("""
        SELECT tags FROM traces WHERE trace_id = ? AND span_id = ?
    """, span['trace_id'], span['span_id'])[0][0]
    
    tags = json.loads(current_tags) if current_tags else {}
    tags[key] = value
    
    # Update database
    execute("""
        UPDATE traces SET tags = ? WHERE trace_id = ? AND span_id = ?
    """, json.dumps(tags), span['trace_id'], span['span_id'])
    
    return tags
'''

generate_trace_id_fujsen = '''
def generate_trace_id():
    import uuid
    return str(uuid.uuid4())
'''

generate_span_id_fujsen = '''
def generate_span_id():
    import uuid
    return str(uuid.uuid4())[:16]
'''
""")
```

## 🔍 Health Checks & Alerts

### Health Check System

```python
# Health check configuration
health_config = TSK.from_string("""
[health_checks]
# Health check endpoints
health_endpoint: "/health"
ready_endpoint: "/ready"
live_endpoint: "/live"

# Health check functions
check_health_fujsen = '''
def check_health():
    health_status = {
        'status': 'healthy',
        'timestamp': time.time(),
        'checks': {}
    }
    
    # Check database
    db_health = check_database_health()
    health_status['checks']['database'] = db_health
    
    # Check cache
    cache_health = check_cache_health()
    health_status['checks']['cache'] = cache_health
    
    # Check external services
    external_health = check_external_services()
    health_status['checks']['external'] = external_health
    
    # Determine overall status
    all_healthy = all(check['status'] == 'healthy' for check in health_status['checks'].values())
    health_status['status'] = 'healthy' if all_healthy else 'unhealthy'
    
    return health_status
'''

check_database_health_fujsen = '''
def check_database_health():
    try:
        # Test database connection
        result = query("SELECT 1")
        
        # Check connection pool
        pool_stats = connection_pool_stats()
        
        return {
            'status': 'healthy',
            'details': {
                'connection': 'OK',
                'pool_size': pool_stats.get('total_connections', 0),
                'active_connections': pool_stats.get('active_connections', 0)
            }
        }
    except Exception as e:
        return {
            'status': 'unhealthy',
            'error': str(e)
        }
'''

check_cache_health_fujsen = '''
def check_cache_health():
    try:
        # Test cache connection
        cache.set('health_check', 'ok', 10)
        value = cache.get('health_check')
        
        if value == 'ok':
            return {
                'status': 'healthy',
                'details': {
                    'connection': 'OK',
                    'read_write': 'OK'
                }
            }
        else:
            return {
                'status': 'unhealthy',
                'error': 'Cache read/write test failed'
            }
    except Exception as e:
        return {
            'status': 'unhealthy',
            'error': str(e)
        }
'''

check_external_services_fujsen = '''
def check_external_services():
    services = {
        'api_gateway': 'https://api.example.com/health',
        'payment_service': 'https://payments.example.com/health',
        'email_service': 'https://email.example.com/health'
    }
    
    results = {}
    
    for service_name, url in services.items():
        try:
            response = requests.get(url, timeout=5)
            if response.status_code == 200:
                results[service_name] = {
                    'status': 'healthy',
                    'response_time': response.elapsed.total_seconds()
                }
            else:
                results[service_name] = {
                    'status': 'unhealthy',
                    'error': f'HTTP {response.status_code}'
                }
        except Exception as e:
            results[service_name] = {
                'status': 'unhealthy',
                'error': str(e)
            }
    
    return results
'''
""")
```

### Alerting System

```python
# Alerting configuration
alerting_config = TSK.from_string("""
[alerting]
# Alert thresholds
cpu_threshold: @env("CPU_THRESHOLD", 80)
memory_threshold: @env("MEMORY_THRESHOLD", 85)
error_rate_threshold: @env("ERROR_RATE_THRESHOLD", 5)
response_time_threshold: @env("RESPONSE_TIME_THRESHOLD", 2.0)

# Alert functions
check_alerts_fujsen = '''
def check_alerts():
    alerts = []
    
    # Check CPU usage
    cpu_usage = get_system_metrics('cpu_usage')
    if cpu_usage > cpu_threshold:
        alerts.append({
            'type': 'high_cpu',
            'severity': 'warning',
            'message': f'CPU usage is {cpu_usage}% (threshold: {cpu_threshold}%)',
            'value': cpu_usage,
            'threshold': cpu_threshold
        })
    
    # Check memory usage
    memory_usage = get_system_metrics('memory_usage')
    if memory_usage > memory_threshold:
        alerts.append({
            'type': 'high_memory',
            'severity': 'warning',
            'message': f'Memory usage is {memory_usage}% (threshold: {memory_threshold}%)',
            'value': memory_usage,
            'threshold': memory_threshold
        })
    
    # Check error rate
    error_rate = calculate_error_rate()
    if error_rate > error_rate_threshold:
        alerts.append({
            'type': 'high_error_rate',
            'severity': 'critical',
            'message': f'Error rate is {error_rate}% (threshold: {error_rate_threshold}%)',
            'value': error_rate,
            'threshold': error_rate_threshold
        })
    
    # Check response time
    avg_response_time = get_metrics('response_time', '5m', 'avg')
    if avg_response_time and avg_response_time[0] > response_time_threshold:
        alerts.append({
            'type': 'slow_response',
            'severity': 'warning',
            'message': f'Average response time is {avg_response_time[0]:.2f}s (threshold: {response_time_threshold}s)',
            'value': avg_response_time[0],
            'threshold': response_time_threshold
        })
    
    return alerts
'''

send_alert_fujsen = '''
def send_alert(alert):
    # Store alert in database
    execute("""
        INSERT INTO alerts (type, severity, message, value, threshold, timestamp)
        VALUES (?, ?, ?, ?, ?, datetime('now'))
    """, alert['type'], alert['severity'], alert['message'], 
        alert['value'], alert['threshold'])
    
    # Send notification based on severity
    if alert['severity'] == 'critical':
        send_critical_alert(alert)
    elif alert['severity'] == 'warning':
        send_warning_alert(alert)
    
    return alert
'''

send_critical_alert_fujsen = '''
def send_critical_alert(alert):
    # Send critical alert via multiple channels
    
    # Email
    send_email_alert(alert, 'critical')
    
    # Slack
    send_slack_alert(alert, 'critical')
    
    # PagerDuty
    send_pagerduty_alert(alert)
    
    return True
'''

send_warning_alert_fujsen = '''
def send_warning_alert(alert):
    # Send warning alert
    
    # Email
    send_email_alert(alert, 'warning')
    
    # Slack
    send_slack_alert(alert, 'warning')
    
    return True
'''

calculate_error_rate_fujsen = '''
def calculate_error_rate():
    # Calculate error rate for last 5 minutes
    total_requests = query("""
        SELECT COUNT(*) FROM request_logs 
        WHERE timestamp > datetime('now', '-5 minutes')
    """)[0][0]
    
    error_requests = query("""
        SELECT COUNT(*) FROM request_logs 
        WHERE status_code >= 400 AND timestamp > datetime('now', '-5 minutes')
    """)[0][0]
    
    if total_requests == 0:
        return 0
    
    return (error_requests / total_requests) * 100
'''
""")
```

## 📊 Dashboard & Visualization

### Metrics Dashboard

```python
# Dashboard configuration
dashboard_config = TSK.from_string("""
[dashboard]
# Dashboard endpoints
metrics_endpoint: "/metrics"
dashboard_endpoint: "/dashboard"
status_endpoint: "/status"

generate_dashboard_data_fujsen = '''
def generate_dashboard_data():
    # Generate comprehensive dashboard data
    
    # System metrics
    system_metrics = {
        'cpu_usage': get_metrics('cpu_usage', '1h', 'avg'),
        'memory_usage': get_metrics('memory_usage', '1h', 'avg'),
        'disk_usage': get_metrics('disk_usage', '1h', 'avg')
    }
    
    # Application metrics
    app_metrics = {
        'request_rate': calculate_request_rate(),
        'error_rate': calculate_error_rate(),
        'response_time': get_metrics('response_time', '1h', 'avg'),
        'active_users': get_metrics('daily_active_users', '1h', 'max')
    }
    
    # Business metrics
    business_metrics = {
        'revenue_today': calculate_revenue_today(),
        'user_registrations': get_metrics('user_registrations', '24h', 'sum'),
        'payment_success_rate': get_metrics('payment_success_rate', '1h', 'avg')
    }
    
    # Recent alerts
    recent_alerts = get_recent_alerts()
    
    return {
        'system': system_metrics,
        'application': app_metrics,
        'business': business_metrics,
        'alerts': recent_alerts,
        'generated_at': time.time()
    }
'''

calculate_request_rate_fujsen = '''
def calculate_request_rate():
    # Calculate requests per second for last minute
    requests_last_minute = query("""
        SELECT COUNT(*) FROM request_logs 
        WHERE timestamp > datetime('now', '-1 minute')
    """)[0][0]
    
    return requests_last_minute / 60  # requests per second
'''

calculate_revenue_today_fujsen = '''
def calculate_revenue_today():
    # Calculate total revenue for today
    revenue = query("""
        SELECT COALESCE(SUM(amount), 0) FROM payments 
        WHERE success = 1 AND DATE(timestamp) = DATE('now')
    """)[0][0]
    
    return revenue
'''

get_recent_alerts_fujsen = '''
def get_recent_alerts():
    # Get recent alerts (last hour)
    alerts = query("""
        SELECT type, severity, message, timestamp 
        FROM alerts 
        WHERE timestamp > datetime('now', '-1 hour')
        ORDER BY timestamp DESC
        LIMIT 10
    """)
    
    return [{
        'type': alert[0],
        'severity': alert[1],
        'message': alert[2],
        'timestamp': alert[3]
    } for alert in alerts]
'''

export_metrics_fujsen = '''
def export_metrics():
    # Export metrics in Prometheus format
    metrics = []
    
    # System metrics
    cpu_usage = get_metrics('cpu_usage', '1m', 'avg')
    if cpu_usage:
        metrics.append(f'cpu_usage {cpu_usage[0]}')
    
    memory_usage = get_metrics('memory_usage', '1m', 'avg')
    if memory_usage:
        metrics.append(f'memory_usage {memory_usage[0]}')
    
    # Application metrics
    request_count = get_metrics('requests_total', '1m', 'sum')
    if request_count:
        metrics.append(f'requests_total {request_count[0]}')
    
    error_count = get_metrics('errors_total', '1m', 'sum')
    if error_count:
        metrics.append(f'errors_total {error_count[0]}')
    
    return '\\n'.join(metrics)
'''
""")
```

## 🎯 Monitoring Best Practices

### 1. Metrics Collection
- Collect both system and business metrics
- Use appropriate metric types (counter, gauge, histogram)
- Implement proper labeling and tagging
- Regular metric aggregation and cleanup

### 2. Logging Strategy
- Use structured logging with consistent format
- Include correlation IDs for request tracing
- Implement log rotation and retention policies
- Separate application and access logs

### 3. Health Checks
- Implement comprehensive health checks
- Check all dependencies and external services
- Use different endpoints for liveness and readiness
- Regular health check execution

### 4. Alerting
- Set appropriate thresholds for alerts
- Use different severity levels
- Implement alert aggregation and deduplication
- Regular alert review and tuning

### 5. Visualization
- Create meaningful dashboards
- Use appropriate chart types for different metrics
- Implement real-time updates
- Regular dashboard review and optimization

## 🚀 Next Steps

1. **Implement metrics collection** for key application metrics
2. **Set up structured logging** with correlation IDs
3. **Configure health checks** for all dependencies
4. **Create alerting rules** with appropriate thresholds
5. **Build monitoring dashboards** for visualization

---

**"We don't bow to any king"** - TuskLang provides comprehensive monitoring and observability features to ensure your applications run smoothly. Implement proper metrics, logging, and alerting to maintain operational excellence! 