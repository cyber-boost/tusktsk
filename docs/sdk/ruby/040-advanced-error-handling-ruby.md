# 🛑 TuskLang Ruby Advanced Error Handling Guide

**"We don't bow to any king" - Ruby Edition**

Build robust error handling with TuskLang in Ruby. Master error recovery, circuit breakers, and fault tolerance patterns for production applications.

## 🔄 Error Recovery Strategies

### 1. Retry Mechanisms
```ruby
# config/error_recovery.tsk
[error_recovery]
enabled: true

[retry_strategies]
database_retry: {
    max_attempts: 3
    backoff_multiplier: 2
    initial_delay: "1s"
    max_delay: "30s"
}

api_retry: {
    max_attempts: 5
    backoff_multiplier: 1.5
    initial_delay: "500ms"
    max_delay: "10s"
}

cache_retry: {
    max_attempts: 2
    backoff_multiplier: 1
    initial_delay: "100ms"
    max_delay: "1s"
}

[recoverable_errors]
database: ["connection_timeout", "deadlock", "temporary_failure"]
api: ["timeout", "rate_limit", "server_error"]
cache: ["connection_failure", "timeout"]
```

### 2. Fallback Strategies
```ruby
# config/fallback_strategies.tsk
[fallback_strategies]
enabled: true

[database_fallbacks]
primary_db: @fallback("primary_db", {
    primary: @query("SELECT * FROM users WHERE id = ?", @request.user_id)
    fallback: @cache.get("user:#{@request.user_id}")
    error_types: ["connection_timeout", "deadlock"]
})

[api_fallbacks]
external_api: @fallback("external_api", {
    primary: @http("GET", "https://api.external.com/data")
    fallback: @cache.get("external_data")
    error_types: ["timeout", "server_error"]
})

[cache_fallbacks]
redis_cache: @fallback("redis_cache", {
    primary: @cache.redis.get("key")
    fallback: @cache.memory.get("key")
    error_types: ["connection_failure", "timeout"]
})
```

## ⚡ Circuit Breaker Pattern

### 1. Circuit Breaker Configuration
```ruby
# config/circuit_breaker.tsk
[circuit_breaker]
enabled: true

[circuit_breakers]
database_circuit: {
    failure_threshold: 5
    timeout: "30s"
    half_open_timeout: "60s"
    error_types: ["connection_timeout", "deadlock", "query_timeout"]
}

api_circuit: {
    failure_threshold: 10
    timeout: "10s"
    half_open_timeout: "30s"
    error_types: ["timeout", "server_error", "rate_limit"]
}

cache_circuit: {
    failure_threshold: 3
    timeout: "5s"
    half_open_timeout: "15s"
    error_types: ["connection_failure", "timeout"]
}

[circuit_states]
database_state: @circuit.state("database_circuit")
api_state: @circuit.state("api_circuit")
cache_state: @circuit.state("cache_circuit")
```

### 2. Circuit Breaker Implementation
```ruby
# app/services/circuit_breaker_service.rb
require 'tusklang'

class CircuitBreakerService
  def self.load_circuit_config
    parser = TuskLang.new
    parser.parse_file('config/circuit_breaker.tsk')
  end

  def self.execute_with_circuit(circuit_name, operation)
    config = load_circuit_config
    circuit_config = config['circuit_breakers'][circuit_name]
    
    return nil unless circuit_config
    
    circuit_state = get_circuit_state(circuit_name)
    
    case circuit_state
    when 'closed'
      execute_operation(operation, circuit_config)
    when 'open'
      handle_open_circuit(circuit_name, circuit_config)
    when 'half_open'
      execute_half_open_operation(operation, circuit_config)
    end
  end

  private

  def self.execute_operation(operation, circuit_config)
    begin
      result = operation.call
      record_success(circuit_name)
      result
    rescue => e
      record_failure(circuit_name, e)
      raise e
    end
  end

  def self.handle_open_circuit(circuit_name, circuit_config)
    timeout = circuit_config['half_open_timeout']
    
    if should_attempt_half_open?(circuit_name, timeout)
      set_circuit_state(circuit_name, 'half_open')
      execute_operation(operation, circuit_config)
    else
      raise CircuitBreakerOpenError, "Circuit breaker is open for #{circuit_name}"
    end
  end

  def self.execute_half_open_operation(operation, circuit_config)
    begin
      result = operation.call
      record_success(circuit_name)
      set_circuit_state(circuit_name, 'closed')
      result
    rescue => e
      record_failure(circuit_name, e)
      set_circuit_state(circuit_name, 'open')
      raise e
    end
  end
end
```

## 🛡️ Fault Tolerance Patterns

### 1. Bulkhead Pattern
```ruby
# config/bulkhead.tsk
[bulkhead]
enabled: true

[bulkheads]
database_pool: {
    max_connections: 20
    max_queue_size: 100
    timeout: "30s"
}

api_pool: {
    max_connections: 10
    max_queue_size: 50
    timeout: "10s"
}

cache_pool: {
    max_connections: 5
    max_queue_size: 25
    timeout: "5s"
}

[pool_monitoring]
database_pool_usage: @metrics("database_pool_usage", @pool.usage("database_pool"))
api_pool_usage: @metrics("api_pool_usage", @pool.usage("api_pool"))
cache_pool_usage: @metrics("cache_pool_usage", @pool.usage("cache_pool"))
```

### 2. Timeout Pattern
```ruby
# config/timeout.tsk
[timeout]
enabled: true

[timeouts]
database_query: "30s"
api_request: "10s"
cache_operation: "5s"
file_operation: "60s"
background_job: "300s"

[timeout_handlers]
database_timeout: @timeout.handler("database_query", {
    fallback: @cache.get("query_result")
    error_message: "Database query timed out"
})

api_timeout: @timeout.handler("api_request", {
    fallback: @cache.get("api_response")
    error_message: "API request timed out"
})
```

## 🔍 Error Monitoring

### 1. Error Tracking
```ruby
# config/error_tracking.tsk
[error_tracking]
enabled: true

[error_categories]
database_errors: {
    connection_timeout: "critical"
    deadlock: "warning"
    query_timeout: "warning"
    constraint_violation: "error"
}

api_errors: {
    timeout: "warning"
    server_error: "error"
    rate_limit: "warning"
    authentication_error: "critical"
}

cache_errors: {
    connection_failure: "warning"
    timeout: "warning"
    memory_full: "error"
}

[error_metrics]
error_count: @metrics("error_count", @error.count)
error_rate: @metrics("error_rate", @error.rate)
error_by_type: @metrics("error_by_type", @error.count_by_type)
error_by_service: @metrics("error_by_service", @error.count_by_service)
```

### 2. Error Alerting
```ruby
# config/error_alerts.tsk
[error_alerts]
enabled: true

[alerts]
high_error_rate: @alert("error_rate > 0.05", {
    severity: "critical",
    message: "Error rate is above 5%",
    notification: ["slack", "pagerduty"]
})

critical_errors: @alert("error_count_by_type.critical > 10", {
    severity: "critical",
    message: "Critical errors detected",
    notification: ["slack", "pagerduty", "email"]
})

circuit_breaker_open: @alert("circuit_state.database == 'open' OR circuit_state.api == 'open'", {
    severity: "warning",
    message: "Circuit breaker is open",
    notification: ["slack", "email"]
})
```

## 🛠️ Ruby Integration Example
```ruby
# app/services/advanced_error_handler.rb
require 'tusklang'

class AdvancedErrorHandler
  def self.load_error_config
    parser = TuskLang.new
    parser.parse_file('config/advanced_error_handling.tsk')
  end

  def self.handle_with_retry(operation, retry_config)
    config = load_error_config
    max_attempts = retry_config['max_attempts']
    backoff_multiplier = retry_config['backoff_multiplier']
    initial_delay = retry_config['initial_delay']
    
    attempt = 0
    delay = initial_delay
    
    loop do
      begin
        return operation.call
      rescue => e
        attempt += 1
        
        if attempt >= max_attempts
          raise e
        end
        
        if recoverable_error?(e, retry_config['error_types'])
          sleep(delay)
          delay *= backoff_multiplier
        else
          raise e
        end
      end
    end
  end

  def self.handle_with_fallback(operation, fallback_config)
    config = load_error_config
    
    begin
      fallback_config['primary'].call
    rescue => e
      if fallback_config['error_types'].include?(e.class.name)
        Rails.logger.warn("Using fallback for #{operation}: #{e.message}")
        fallback_config['fallback'].call
      else
        raise e
      end
    end
  end

  def self.execute_with_circuit_breaker(circuit_name, operation)
    CircuitBreakerService.execute_with_circuit(circuit_name, operation)
  end

  def self.track_error(error, category = nil)
    config = load_error_config
    
    if config['error_tracking']['enabled']
      error_config = config['error_categories'][category]
      severity = error_config ? error_config[error.class.name] : 'error'
      
      ErrorTrackingService.track_error(error, severity, category)
    end
  end

  def self.check_error_alerts
    config = load_error_config
    
    if config['error_alerts']['enabled']
      alerts = config['error_alerts']['alerts']
      
      alerts.each do |alert_name, alert_config|
        if alert_config['condition'].evaluate
          send_error_alert(alert_name, alert_config)
        end
      end
    end
  end

  private

  def self.recoverable_error?(error, error_types)
    error_types.include?(error.class.name)
  end

  def self.send_error_alert(alert_name, alert_config)
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
config = AdvancedErrorHandler.load_error_config

# Retry with exponential backoff
result = AdvancedErrorHandler.handle_with_retry(
  -> { User.find(1) },
  config['retry_strategies']['database_retry']
)

# Fallback strategy
result = AdvancedErrorHandler.handle_with_fallback(
  'database_query',
  config['fallback_strategies']['database_fallbacks']['primary_db']
)

# Circuit breaker
result = AdvancedErrorHandler.execute_with_circuit_breaker(
  'database_circuit',
  -> { User.find(1) }
)

# Error tracking
begin
  User.find(1)
rescue => e
  AdvancedErrorHandler.track_error(e, 'database_errors')
end

# Check alerts
AdvancedErrorHandler.check_error_alerts
```

## 🛡️ Best Practices
- Implement retry mechanisms with exponential backoff.
- Use circuit breakers to prevent cascading failures.
- Implement fallback strategies for critical operations.
- Monitor error rates and patterns.
- Use bulkhead pattern to isolate failures.

**Ready to handle any error? Let's Tusk! 🚀** 