# 📝 TuskLang Ruby Advanced Logging Guide

**"We don't bow to any king" - Ruby Edition**

Master advanced logging with TuskLang in Ruby. Learn structured logging, log aggregation, and comprehensive log analysis patterns.

## 📊 Structured Logging

### 1. Log Configuration
```ruby
# config/logging.tsk
[logging]
enabled: true
level: @env("LOG_LEVEL", "info")
format: @env("LOG_FORMAT", "json")
output: @env("LOG_OUTPUT", "stdout")

[log_levels]
trace: 0
debug: 1
info: 2
warn: 3
error: 4
fatal: 5

[log_formats]
json: {
    timestamp: true
    level: true
    message: true
    context: true
    metadata: true
}

text: {
    timestamp: true
    level: true
    message: true
    context: false
    metadata: false
}

[log_outputs]
stdout: {
    type: "console"
    encoding: "utf-8"
}

file: {
    type: "file"
    path: "/var/log/myapp.log"
    max_size: "100MB"
    max_files: 10
    encoding: "utf-8"
}

syslog: {
    type: "syslog"
    facility: "local0"
    tag: "myapp"
}
```

### 2. Structured Log Messages
```ruby
# config/structured_logging.tsk
[structured_logging]
enabled: true

[log_contexts]
request_context: {
    request_id: @request.id
    user_id: @request.user_id
    ip_address: @request.ip
    user_agent: @request.user_agent
    method: @request.method
    path: @request.path
    status: @response.status
    duration: @request.duration
}

business_context: {
    user_id: @request.user_id
    order_id: @request.order_id
    action: @request.action
    resource: @request.resource
    amount: @request.amount
}

system_context: {
    hostname: @system.hostname
    pid: @system.pid
    memory_usage: @system.memory_usage
    cpu_usage: @system.cpu_usage
    uptime: @system.uptime
}

[log_events]
user_login: @log.event("user_login", {
    user_id: @request.user_id
    ip_address: @request.ip
    success: @request.login_success
    timestamp: @date.now()
})

order_created: @log.event("order_created", {
    order_id: @request.order_id
    user_id: @request.user_id
    amount: @request.amount
    items: @request.items_count
    timestamp: @date.now()
})

payment_processed: @log.event("payment_processed", {
    payment_id: @request.payment_id
    order_id: @request.order_id
    amount: @request.amount
    status: @request.payment_status
    timestamp: @date.now()
})
```

## 🔍 Log Aggregation

### 1. Centralized Logging
```ruby
# config/log_aggregation.tsk
[log_aggregation]
enabled: true

[aggregation_targets]
elasticsearch: {
    host: @env("ELASTICSEARCH_HOST", "localhost")
    port: @env("ELASTICSEARCH_PORT", 9200)
    index: "myapp-logs"
    bulk_size: 100
    flush_interval: "5s"
}

logstash: {
    host: @env("LOGSTASH_HOST", "localhost")
    port: @env("LOGSTASH_PORT", 5000)
    protocol: "tcp"
}

fluentd: {
    host: @env("FLUENTD_HOST", "localhost")
    port: @env("FLUENTD_PORT", 24224)
    tag: "myapp"
}

[log_shipping]
batch_size: 100
batch_timeout: "5s"
retry_attempts: 3
retry_delay: "1s"
```

### 2. Log Buffering
```ruby
# config/log_buffering.tsk
[log_buffering]
enabled: true

[buffer_settings]
max_size: 1000
flush_interval: "5s"
max_age: "30s"

[buffer_strategies]
memory_buffer: {
    max_entries: 1000
    flush_interval: "5s"
}

file_buffer: {
    max_size: "10MB"
    flush_interval: "10s"
    backup_count: 5
}

[log_persistence]
persist_on_shutdown: true
persist_on_error: true
persist_interval: "30s"
```

## 📈 Log Analysis

### 1. Log Metrics
```ruby
# config/log_metrics.tsk
[log_metrics]
enabled: true

[metrics]
log_count: @metrics("log_count", @log.count)
log_rate: @metrics("log_rate", @log.rate)
error_rate: @metrics("error_rate", @log.error_rate)
warning_rate: @metrics("warning_rate", @log.warning_rate)

[log_patterns]
error_patterns: @log.patterns("error", {
    database_errors: "database.*error"
    api_errors: "api.*error"
    validation_errors: "validation.*error"
    authentication_errors: "auth.*error"
})

performance_patterns: @log.patterns("performance", {
    slow_queries: "query.*slow"
    timeouts: ".*timeout.*"
    memory_usage: "memory.*high"
    cpu_usage: "cpu.*high"
})

business_patterns: @log.patterns("business", {
    user_registrations: "user.*registered"
    orders_created: "order.*created"
    payments_processed: "payment.*processed"
    logins: "user.*login"
})
```

### 2. Log Alerts
```ruby
# config/log_alerts.tsk
[log_alerts]
enabled: true

[alerts]
high_error_rate: @alert("error_rate > 0.05", {
    severity: "critical",
    message: "Error rate is above 5%",
    notification: ["slack", "pagerduty"]
})

high_log_volume: @alert("log_rate > 1000", {
    severity: "warning",
    message: "Log volume is high",
    notification: ["slack"]
})

error_pattern_detected: @alert("log_patterns.error_patterns.database_errors > 10", {
    severity: "critical",
    message: "Database errors detected",
    notification: ["slack", "pagerduty"]
})
```

## 🛠️ Ruby Integration Example
```ruby
# app/services/advanced_logging_service.rb
require 'tusklang'

class AdvancedLoggingService
  def self.load_logging_config
    parser = TuskLang.new
    parser.parse_file('config/advanced_logging.tsk')
  end

  def self.log_structured_event(event_name, data = {})
    config = load_logging_config
    
    if config['structured_logging']['enabled']
      event_config = config['structured_logging']['log_events'][event_name]
      
      if event_config
        log_data = {
          event: event_name,
          timestamp: Time.current.iso8601,
          data: data,
          context: get_log_context(config)
        }
        
        send_to_log_aggregation(log_data)
      end
    end
  end

  def self.log_with_context(level, message, context = {})
    config = load_logging_config
    
    log_entry = {
      level: level,
      message: message,
      timestamp: Time.current.iso8601,
      context: context.merge(get_log_context(config))
    }
    
    write_log(log_entry, config)
  end

  def self.analyze_logs
    config = load_logging_config
    
    if config['log_metrics']['enabled']
      metrics = config['log_metrics']['metrics']
      
      metrics.each do |metric_name, metric_config|
        value = calculate_metric(metric_name, metric_config)
        MonitoringService.record_metric(metric_name, value)
      end
    end
  end

  def self.detect_log_patterns
    config = load_logging_config
    
    if config['log_metrics']['enabled']
      patterns = config['log_metrics']['log_patterns']
      
      patterns.each do |pattern_type, pattern_config|
        pattern_config.each do |pattern_name, pattern_regex|
          count = count_log_pattern(pattern_regex)
          MonitoringService.record_metric("log_pattern_#{pattern_name}", count)
        end
      end
    end
  end

  def self.check_log_alerts
    config = load_logging_config
    
    if config['log_alerts']['enabled']
      alerts = config['log_alerts']['alerts']
      
      alerts.each do |alert_name, alert_config|
        if alert_config['condition'].evaluate
          send_log_alert(alert_name, alert_config)
        end
      end
    end
  end

  private

  def self.get_log_context(config)
    {
      request_context: get_request_context(config),
      business_context: get_business_context(config),
      system_context: get_system_context(config)
    }
  end

  def self.get_request_context(config)
    {
      request_id: SecureRandom.uuid,
      user_id: Current.user&.id,
      ip_address: Current.request&.remote_ip,
      user_agent: Current.request&.user_agent,
      method: Current.request&.method,
      path: Current.request&.path,
      status: Current.response&.status,
      duration: Current.request&.duration
    }
  end

  def self.get_business_context(config)
    {
      user_id: Current.user&.id,
      order_id: Current.request&.params[:order_id],
      action: Current.request&.action_name,
      resource: Current.request&.controller_name
    }
  end

  def self.get_system_context(config)
    {
      hostname: Socket.gethostname,
      pid: Process.pid,
      memory_usage: GetProcessMem.new.mb,
      cpu_usage: System::CPU.usage,
      uptime: Time.current - Process.clock_gettime(Process::CLOCK_MONOTONIC)
    }
  end

  def self.write_log(log_entry, config)
    case config['logging']['output']
    when 'stdout'
      puts log_entry.to_json
    when 'file'
      File.open(config['log_outputs']['file']['path'], 'a') do |f|
        f.puts log_entry.to_json
      end
    when 'syslog'
      Syslog.open(config['log_outputs']['syslog']['tag']) do |s|
        s.log(Syslog::LOG_INFO, log_entry.to_json)
      end
    end
  end

  def self.send_to_log_aggregation(log_data)
    # Send to log aggregation systems
    LogAggregationService.send(log_data)
  end

  def self.calculate_metric(metric_name, metric_config)
    # Implementation for calculating log metrics
    case metric_name
    when 'log_count'
      LogAnalysisService.get_log_count
    when 'error_rate'
      LogAnalysisService.get_error_rate
    when 'warning_rate'
      LogAnalysisService.get_warning_rate
    end
  end

  def self.count_log_pattern(pattern_regex)
    # Implementation for counting log patterns
    LogAnalysisService.count_pattern(pattern_regex)
  end

  def self.send_log_alert(alert_name, alert_config)
    message = alert_config['message']
    severity = alert_config['severity']
    notifications = alert_config['notification']
    
    notifications.each do |notification|
      case notification
      when 'slack'
        SlackNotifier.send(message, severity)
      when 'pagerduty'
        PagerDutyNotifier.send(message, severity)
      end
    end
  end
end

# Usage
config = AdvancedLoggingService.load_logging_config

# Structured logging
AdvancedLoggingService.log_structured_event('user_login', {
  user_id: 123,
  ip_address: '192.168.1.1',
  success: true
})

AdvancedLoggingService.log_structured_event('order_created', {
  order_id: 456,
  user_id: 123,
  amount: 99.99
})

# Contextual logging
AdvancedLoggingService.log_with_context('info', 'User logged in successfully', {
  user_id: 123,
  session_id: 'abc123'
})

AdvancedLoggingService.log_with_context('error', 'Database connection failed', {
  error_code: 'DB001',
  retry_count: 3
})

# Log analysis
AdvancedLoggingService.analyze_logs
AdvancedLoggingService.detect_log_patterns
AdvancedLoggingService.check_log_alerts
```

## 🛡️ Best Practices
- Use structured logging for better analysis.
- Implement log aggregation for centralized monitoring.
- Set up log metrics and alerts for proactive monitoring.
- Use appropriate log levels and contexts.
- Implement log buffering for performance.

**Ready to log everything? Let's Tusk! 🚀** 