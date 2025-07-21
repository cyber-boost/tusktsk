# 🚨 Advanced Error Handling with TuskLang Ruby

**"We don't bow to any king" - Ruby Edition**

Build robust error handling systems with TuskLang's advanced error handling features. From custom exception hierarchies to automatic error recovery, TuskLang provides the flexibility and power you need to handle errors gracefully in your Ruby applications.

## 🚀 Quick Start

### Basic Error Handling Setup
```ruby
require 'tusklang'
require 'tusklang/error_handling'

# Initialize error handling system
error_system = TuskLang::ErrorHandling::System.new

# Configure error handling
error_system.configure do |config|
  config.default_strategy = 'graceful_degradation'
  config.retry_enabled = true
  config.max_retries = 3
  config.circuit_breaker_enabled = true
end

# Register error handling strategies
error_system.register_strategy(:graceful_degradation, TuskLang::ErrorHandling::Strategies::GracefulDegradationStrategy.new)
error_system.register_strategy(:circuit_breaker, TuskLang::ErrorHandling::Strategies::CircuitBreakerStrategy.new)
error_system.register_strategy(:retry, TuskLang::ErrorHandling::Strategies::RetryStrategy.new)
```

### TuskLang Configuration
```tsk
# config/error_handling.tsk
[error_handling]
enabled: true
default_strategy: "graceful_degradation"
retry_enabled: true
max_retries: 3
circuit_breaker_enabled: true

[error_handling.strategies]
graceful_degradation: {
    enabled: true,
    fallback_responses: {
        "api/users": {"users": [], "message": "Service temporarily unavailable"},
        "api/orders": {"orders": [], "status": "degraded"}
    }
}
circuit_breaker: {
    enabled: true,
    failure_threshold: 5,
    recovery_timeout: "30s",
    half_open_max_calls: 3
}
retry: {
    enabled: true,
    max_attempts: 3,
    backoff_strategy: "exponential",
    base_delay: "1s",
    max_delay: "30s"
}

[error_handling.exceptions]
custom_exceptions: {
    "ValidationError": {
        code: "VALIDATION_ERROR",
        status_code: 400,
        retryable: false
    },
    "AuthenticationError": {
        code: "AUTH_ERROR",
        status_code: 401,
        retryable: false
    },
    "RateLimitError": {
        code: "RATE_LIMIT",
        status_code: 429,
        retryable: true,
        retry_after: "60s"
    },
    "ServiceUnavailableError": {
        code: "SERVICE_UNAVAILABLE",
        status_code: 503,
        retryable: true,
        max_retries: 5
    }
}

[error_handling.monitoring]
enabled: true
metrics_enabled: true
alerting_enabled: true
error_threshold: 5
alert_cooldown: "5m"
```

## 🎯 Core Features

### 1. Custom Exception Hierarchy
```ruby
require 'tusklang/error_handling'

module TuskLang
  module Errors
    # Base exception class
    class TuskLangError < StandardError
      attr_reader :code, :status_code, :retryable, :context
      
      def initialize(message = nil, code: nil, status_code: 500, retryable: false, context: {})
        super(message)
        @code = code || self.class.name.demodulize.underscore.upcase
        @status_code = status_code
        @retryable = retryable
        @context = context
      end
      
      def to_h
        {
          error: @code,
          message: message,
          status_code: @status_code,
          retryable: @retryable,
          context: @context,
          timestamp: Time.now.iso8601
        }
      end
    end
    
    # Client errors (4xx)
    class ClientError < TuskLangError
      def initialize(message = nil, code: nil, status_code: 400, retryable: false, context: {})
        super(message, code: code, status_code: status_code, retryable: retryable, context: context)
      end
    end
    
    class ValidationError < ClientError
      def initialize(message = "Validation failed", context: {})
        super(message, code: "VALIDATION_ERROR", status_code: 400, retryable: false, context: context)
      end
    end
    
    class AuthenticationError < ClientError
      def initialize(message = "Authentication failed", context: {})
        super(message, code: "AUTH_ERROR", status_code: 401, retryable: false, context: context)
      end
    end
    
    class AuthorizationError < ClientError
      def initialize(message = "Authorization failed", context: {})
        super(message, code: "FORBIDDEN", status_code: 403, retryable: false, context: context)
      end
    end
    
    class NotFoundError < ClientError
      def initialize(message = "Resource not found", context: {})
        super(message, code: "NOT_FOUND", status_code: 404, retryable: false, context: context)
      end
    end
    
    class RateLimitError < ClientError
      def initialize(message = "Rate limit exceeded", retry_after: nil, context: {})
        super(message, code: "RATE_LIMIT", status_code: 429, retryable: true, context: context)
        @retry_after = retry_after
      end
      
      def to_h
        super.merge(retry_after: @retry_after)
      end
    end
    
    # Server errors (5xx)
    class ServerError < TuskLangError
      def initialize(message = nil, code: nil, status_code: 500, retryable: true, context: {})
        super(message, code: code, status_code: status_code, retryable: retryable, context: context)
      end
    end
    
    class ServiceUnavailableError < ServerError
      def initialize(message = "Service temporarily unavailable", context: {})
        super(message, code: "SERVICE_UNAVAILABLE", status_code: 503, retryable: true, context: context)
      end
    end
    
    class DatabaseError < ServerError
      def initialize(message = "Database error occurred", context: {})
        super(message, code: "DATABASE_ERROR", status_code: 500, retryable: true, context: context)
      end
    end
    
    class ExternalServiceError < ServerError
      def initialize(message = "External service error", context: {})
        super(message, code: "EXTERNAL_SERVICE_ERROR", status_code: 502, retryable: true, context: context)
      end
    end
  end
end
```

### 2. Graceful Degradation Strategy
```ruby
require 'tusklang/error_handling'

class GracefulDegradationStrategy
  include TuskLang::ErrorHandling::Strategy
  
  def initialize
    @config = TuskLang.parse_file('config/error_handling.tsk')
    @fallback_responses = @config['error_handling']['strategies']['graceful_degradation']['fallback_responses']
    @monitoring = ErrorMonitoring.new
  end
  
  def handle_error(error, context = {})
    # Log the error
    log_error(error, context)
    
    # Record metrics
    record_error_metrics(error, context)
    
    # Check if we have a fallback response
    fallback_response = get_fallback_response(context[:endpoint])
    
    if fallback_response
      @monitoring.record_degradation(context[:endpoint])
      return ErrorHandlingResult.degraded(fallback_response, error)
    end
    
    # No fallback available, re-raise or return error
    ErrorHandlingResult.failed(error)
  end
  
  def log_error(error, context)
    logger = TuskLang::Logging::System.new
    
    log_data = {
      error_class: error.class.name,
      error_message: error.message,
      error_code: error.respond_to?(:code) ? error.code : nil,
      status_code: error.respond_to?(:status_code) ? error.status_code : nil,
      retryable: error.respond_to?(:retryable) ? error.retryable : false,
      context: context,
      stack_trace: error.backtrace&.first(10)
    }
    
    if error.respond_to?(:status_code) && error.status_code >= 500
      logger.error("Server error occurred", log_data)
    else
      logger.warn("Client error occurred", log_data)
    end
  end
  
  def record_error_metrics(error, context)
    metrics = TuskLang::Metrics::System.new
    
    # Increment error counter
    metrics.increment("errors_total", 1, {
      error_type: error.class.name.demodulize.underscore,
      endpoint: context[:endpoint],
      status_code: error.respond_to?(:status_code) ? error.status_code.to_s : "unknown"
    })
    
    # Record error rate
    metrics.gauge("error_rate", calculate_error_rate(context[:endpoint]), {
      endpoint: context[:endpoint]
    })
  end
  
  def get_fallback_response(endpoint)
    return nil unless endpoint && @fallback_responses[endpoint]
    
    @fallback_responses[endpoint]
  end
  
  private
  
  def calculate_error_rate(endpoint)
    # Implementation to calculate error rate for the endpoint
    # This would typically query metrics storage
    0.05 # Placeholder
  end
end
```

### 3. Circuit Breaker Strategy
```ruby
require 'tusklang/error_handling'
require 'redis'

class CircuitBreakerStrategy
  include TuskLang::ErrorHandling::Strategy
  
  def initialize
    @config = TuskLang.parse_file('config/error_handling.tsk')
    @circuit_config = @config['error_handling']['strategies']['circuit_breaker']
    @redis = Redis.new
    @monitoring = ErrorMonitoring.new
  end
  
  def handle_error(error, context = {})
    service_key = context[:service] || 'default'
    
    # Record the failure
    record_failure(service_key)
    
    # Check if circuit should be opened
    if should_open_circuit?(service_key)
      open_circuit(service_key)
      return ErrorHandlingResult.circuit_open(error, get_circuit_state(service_key))
    end
    
    ErrorHandlingResult.failed(error)
  end
  
  def can_execute?(service_key = 'default')
    state = get_circuit_state(service_key)
    
    case state[:status]
    when 'closed'
      true
    when 'open'
      # Check if recovery timeout has passed
      if Time.now.to_i - state[:opened_at] > parse_duration(@circuit_config['recovery_timeout'])
        half_open_circuit(service_key)
        true
      else
        false
      end
    when 'half_open'
      # Allow limited calls in half-open state
      current_calls = @redis.get("circuit:#{service_key}:half_open_calls").to_i
      if current_calls < @circuit_config['half_open_max_calls']
        @redis.incr("circuit:#{service_key}:half_open_calls")
        true
      else
        false
      end
    else
      true
    end
  end
  
  def record_success(service_key = 'default')
    @redis.del("circuit:#{service_key}:failures")
    @redis.del("circuit:#{service_key}:half_open_calls")
    
    # Close circuit if it was half-open
    state = get_circuit_state(service_key)
    if state[:status] == 'half_open'
      close_circuit(service_key)
    end
    
    @monitoring.record_circuit_success(service_key)
  end
  
  private
  
  def record_failure(service_key)
    @redis.incr("circuit:#{service_key}:failures")
    @redis.expire("circuit:#{service_key}:failures", 60) # 1 minute window
  end
  
  def should_open_circuit?(service_key)
    failures = @redis.get("circuit:#{service_key}:failures").to_i
    failures >= @circuit_config['failure_threshold']
  end
  
  def open_circuit(service_key)
    circuit_data = {
      status: 'open',
      opened_at: Time.now.to_i,
      failure_count: @redis.get("circuit:#{service_key}:failures").to_i
    }
    
    @redis.setex("circuit:#{service_key}:state", 300, circuit_data.to_json) # 5 minute TTL
    @monitoring.record_circuit_opened(service_key)
  end
  
  def half_open_circuit(service_key)
    circuit_data = {
      status: 'half_open',
      opened_at: Time.now.to_i,
      half_open_calls: 0
    }
    
    @redis.setex("circuit:#{service_key}:state", 60, circuit_data.to_json) # 1 minute TTL
    @monitoring.record_circuit_half_open(service_key)
  end
  
  def close_circuit(service_key)
    @redis.del("circuit:#{service_key}:state")
    @monitoring.record_circuit_closed(service_key)
  end
  
  def get_circuit_state(service_key)
    state_data = @redis.get("circuit:#{service_key}:state")
    return { status: 'closed' } unless state_data
    
    JSON.parse(state_data, symbolize_names: true)
  end
  
  def parse_duration(duration_str)
    # Parse duration strings like "30s", "5m", "1h"
    case duration_str
    when /(\d+)s/
      $1.to_i
    when /(\d+)m/
      $1.to_i * 60
    when /(\d+)h/
      $1.to_i * 3600
    else
      30 # Default 30 seconds
    end
  end
end
```

### 4. Retry Strategy
```ruby
require 'tusklang/error_handling'

class RetryStrategy
  include TuskLang::ErrorHandling::Strategy
  
  def initialize
    @config = TuskLang.parse_file('config/error_handling.tsk')
    @retry_config = @config['error_handling']['strategies']['retry']
    @monitoring = ErrorMonitoring.new
  end
  
  def execute_with_retry(operation, context = {})
    max_attempts = context[:max_attempts] || @retry_config['max_attempts']
    attempt = 1
    
    loop do
      begin
        result = operation.call
        @monitoring.record_retry_success(context[:operation], attempt)
        return ErrorHandlingResult.success(result)
      rescue => error
        # Check if error is retryable
        unless should_retry?(error, context)
          @monitoring.record_retry_failure(context[:operation], attempt, error)
          return ErrorHandlingResult.failed(error)
        end
        
        # Check if we've exceeded max attempts
        if attempt >= max_attempts
          @monitoring.record_retry_exhausted(context[:operation], attempt, error)
          return ErrorHandlingResult.failed(error)
        end
        
        # Calculate delay
        delay = calculate_delay(attempt, context)
        
        # Log retry attempt
        log_retry_attempt(attempt, max_attempts, delay, error, context)
        
        # Wait before retrying
        sleep(delay)
        attempt += 1
      end
    end
  end
  
  private
  
  def should_retry?(error, context)
    # Check if error is explicitly retryable
    return false if error.respond_to?(:retryable) && !error.retryable
    
    # Check if error is in retryable list
    retryable_errors = context[:retryable_errors] || [
      'Net::TimeoutError',
      'Net::ReadTimeout',
      'Net::OpenTimeout',
      'Errno::ECONNRESET',
      'Errno::ECONNREFUSED',
      'Errno::ETIMEDOUT'
    ]
    
    retryable_errors.any? { |error_class| error.is_a?(Object.const_get(error_class)) }
  end
  
  def calculate_delay(attempt, context)
    base_delay = parse_duration(@retry_config['base_delay'])
    max_delay = parse_duration(@retry_config['max_delay'])
    
    case @retry_config['backoff_strategy']
    when 'exponential'
      delay = base_delay * (2 ** (attempt - 1))
    when 'linear'
      delay = base_delay * attempt
    when 'constant'
      delay = base_delay
    else
      delay = base_delay
    end
    
    # Add jitter to prevent thundering herd
    jitter = delay * 0.1 * rand
    
    [delay + jitter, max_delay].min
  end
  
  def log_retry_attempt(attempt, max_attempts, delay, error, context)
    logger = TuskLang::Logging::System.new
    
    logger.warn("Retry attempt #{attempt}/#{max_attempts}", {
      operation: context[:operation],
      error_class: error.class.name,
      error_message: error.message,
      delay: delay,
      attempt: attempt,
      max_attempts: max_attempts
    })
  end
  
  def parse_duration(duration_str)
    case duration_str
    when /(\d+)s/
      $1.to_i
    when /(\d+)m/
      $1.to_i * 60
    when /(\d+)h/
      $1.to_i * 3600
    else
      1 # Default 1 second
    end
  end
end
```

## 🔧 Advanced Configuration

### Error Monitoring and Alerting
```ruby
require 'tusklang/error_handling'

class ErrorMonitoring
  def initialize
    @config = TuskLang.parse_file('config/error_handling.tsk')
    @metrics = TuskLang::Metrics::System.new
    @alerts = []
  end
  
  def record_error(error, context = {})
    # Record error metrics
    @metrics.increment("errors_total", 1, {
      error_type: error.class.name.demodulize.underscore,
      endpoint: context[:endpoint],
      service: context[:service]
    })
    
    # Check for alerting conditions
    check_alert_conditions(error, context)
  end
  
  def record_circuit_opened(service)
    @metrics.increment("circuit_breaker_opened_total", 1, { service: service })
    check_circuit_alert(service, 'opened')
  end
  
  def record_circuit_closed(service)
    @metrics.increment("circuit_breaker_closed_total", 1, { service: service })
    check_circuit_alert(service, 'closed')
  end
  
  def record_degradation(endpoint)
    @metrics.increment("degradation_total", 1, { endpoint: endpoint })
  end
  
  private
  
  def check_alert_conditions(error, context)
    error_rate = calculate_error_rate(context[:endpoint])
    threshold = @config['error_handling']['monitoring']['error_threshold']
    
    if error_rate > threshold
      send_alert("High error rate detected", {
        endpoint: context[:endpoint],
        error_rate: error_rate,
        threshold: threshold,
        error: error.class.name
      })
    end
  end
  
  def check_circuit_alert(service, state)
    send_alert("Circuit breaker #{state}", {
      service: service,
      state: state,
      timestamp: Time.now.iso8601
    })
  end
  
  def send_alert(message, data)
    # Implementation to send alerts (email, Slack, PagerDuty, etc.)
    alert = {
      message: message,
      data: data,
      timestamp: Time.now.iso8601
    }
    
    @alerts << alert
    
    # Send to external alerting system
    send_to_alerting_system(alert)
  end
  
  def send_to_alerting_system(alert)
    # Integration with external alerting systems
    # This could be Slack, PagerDuty, email, etc.
  end
  
  def calculate_error_rate(endpoint)
    # Calculate error rate for the endpoint
    # This would typically query metrics storage
    0.05 # Placeholder
  end
end
```

### Rails Integration
```ruby
require 'tusklang/error_handling'

class ApplicationController < ActionController::Base
  include TuskLang::ErrorHandling::Controller
  
  rescue_from StandardError, with: :handle_error
  
  private
  
  def handle_error(error)
    error_system = TuskLang::ErrorHandling::System.new
    context = {
      endpoint: "#{request.method} #{request.path}",
      user_id: current_user&.id,
      request_id: request.request_id,
      service: 'web'
    }
    
    result = error_system.handle_error(error, context)
    
    case result.status
    when :success
      render json: result.data, status: :ok
    when :degraded
      render json: result.data, status: :ok
    when :circuit_open
      render json: { error: 'Service temporarily unavailable' }, status: :service_unavailable
    when :failed
      render_error_response(error)
    end
  end
  
  def render_error_response(error)
    if error.respond_to?(:status_code)
      render json: error.to_h, status: error.status_code
    else
      render json: { error: 'Internal server error' }, status: :internal_server_error
    end
  end
end
```

## 📊 Performance Optimization

### Error Caching and Deduplication
```ruby
require 'tusklang/error_handling'

class ErrorCache
  def initialize
    @redis = Redis.new
    @cache_ttl = 300 # 5 minutes
  end
  
  def cache_error(error, context)
    cache_key = generate_cache_key(error, context)
    
    # Check if similar error was recently cached
    cached_error = @redis.get(cache_key)
    
    if cached_error
      # Increment occurrence count
      @redis.incr("#{cache_key}:count")
      return true # Error was cached
    else
      # Cache new error
      error_data = {
        error_class: error.class.name,
        error_message: error.message,
        context: context,
        first_occurrence: Time.now.iso8601,
        count: 1
      }
      
      @redis.setex(cache_key, @cache_ttl, error_data.to_json)
      @redis.setex("#{cache_key}:count", @cache_ttl, 1)
      return false # New error
    end
  end
  
  def get_error_stats(error, context)
    cache_key = generate_cache_key(error, context)
    cached_data = @redis.get(cache_key)
    count = @redis.get("#{cache_key}:count")
    
    if cached_data && count
      data = JSON.parse(cached_data)
      data['count'] = count.to_i
      data
    else
      nil
    end
  end
  
  private
  
  def generate_cache_key(error, context)
    # Generate a cache key based on error type and context
    # This helps deduplicate similar errors
    components = [
      error.class.name,
      context[:endpoint],
      context[:service],
      Digest::MD5.hexdigest(error.message)[0..7]
    ]
    
    "error_cache:#{components.join(':')}"
  end
end
```

## 🔍 Monitoring and Analytics

### Error Analytics Dashboard
```ruby
require 'tusklang/error_handling'

class ErrorAnalytics
  def initialize
    @metrics = TuskLang::Metrics::System.new
    @cache = ErrorCache.new
  end
  
  def get_error_summary(time_range = '24h')
    {
      total_errors: get_total_errors(time_range),
      error_rate: get_error_rate(time_range),
      top_errors: get_top_errors(time_range),
      circuit_breaker_status: get_circuit_breaker_status,
      degradation_events: get_degradation_events(time_range)
    }
  end
  
  def get_error_trends(time_range = '7d')
    # Get error trends over time
    # This would typically query time-series metrics storage
    {
      daily_errors: get_daily_error_counts(time_range),
      error_rate_trend: get_error_rate_trend(time_range),
      circuit_breaker_events: get_circuit_breaker_events(time_range)
    }
  end
  
  def get_service_health
    # Get health status of all services
    services = ['api', 'database', 'cache', 'external_services']
    
    services.map do |service|
      {
        service: service,
        status: get_service_status(service),
        error_rate: get_service_error_rate(service),
        circuit_breaker_state: get_circuit_breaker_state(service)
      }
    end
  end
  
  private
  
  def get_total_errors(time_range)
    # Query metrics for total error count
    @metrics.get_counter_value("errors_total", time_range)
  end
  
  def get_error_rate(time_range)
    # Calculate error rate over time range
    # This would compare error count to total request count
    0.02 # Placeholder
  end
  
  def get_top_errors(time_range)
    # Get most frequent errors
    [
      { error_class: 'ValidationError', count: 150, percentage: 25.5 },
      { error_class: 'DatabaseError', count: 89, percentage: 15.1 },
      { error_class: 'ExternalServiceError', count: 67, percentage: 11.4 }
    ]
  end
  
  def get_circuit_breaker_status
    # Get current circuit breaker states
    [
      { service: 'api', state: 'closed', failure_count: 0 },
      { service: 'database', state: 'open', failure_count: 8 },
      { service: 'cache', state: 'half_open', failure_count: 3 }
    ]
  end
  
  def get_degradation_events(time_range)
    # Get degradation events
    [
      { endpoint: '/api/users', count: 12, duration: '2h 15m' },
      { endpoint: '/api/orders', count: 8, duration: '1h 30m' }
    ]
  end
end
```

## 🎯 Best Practices

### 1. Error Classification
- **Client Errors (4xx):** Validation, authentication, authorization, rate limiting
- **Server Errors (5xx):** Database, external services, infrastructure issues
- **Retryable vs Non-Retryable:** Distinguish between transient and permanent failures

### 2. Circuit Breaker Patterns
- **Failure Threshold:** Number of failures before opening circuit
- **Recovery Timeout:** Time to wait before attempting recovery
- **Half-Open State:** Limited requests to test service recovery

### 3. Graceful Degradation
- **Fallback Responses:** Provide degraded but functional responses
- **Feature Flags:** Disable non-critical features during outages
- **Cached Data:** Serve stale data when fresh data is unavailable

### 4. Monitoring and Alerting
- **Error Rate Thresholds:** Alert when error rates exceed acceptable levels
- **Circuit Breaker Alerts:** Notify when circuits open/close
- **Performance Impact:** Monitor impact of error handling on performance

### 5. Testing Error Scenarios
- **Unit Tests:** Test error handling logic in isolation
- **Integration Tests:** Test error handling with external dependencies
- **Chaos Engineering:** Intentionally inject failures to test resilience

## 🚀 Conclusion

TuskLang's advanced error handling provides comprehensive tools for building resilient Ruby applications. From custom exception hierarchies to circuit breakers and graceful degradation, TuskLang enables developers to handle errors gracefully while maintaining application availability and user experience.

The combination of multiple error handling strategies, comprehensive monitoring, and automated recovery mechanisms ensures that your Ruby applications can withstand failures and provide reliable service to users. 