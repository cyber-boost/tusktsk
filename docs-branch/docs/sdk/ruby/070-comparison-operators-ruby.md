# 🔍 Comparison Operators in TuskLang - Ruby Edition

**"We don't bow to any king" - Ruby Edition**

TuskLang's comparison operators work seamlessly with Ruby's rich object system, providing powerful data validation, conditional logic, and dynamic configuration capabilities.

## 🎯 Overview

Comparison operators in TuskLang allow you to compare values, validate data types, and create dynamic configurations that adapt based on your application's state. When integrated with Ruby, these operators become even more powerful through Ruby's object-oriented features.

## 🚀 Basic Comparison Operators

### Equality Operators

```ruby
# TuskLang configuration with comparison operators
tsk_content = <<~TSK
  [validation]
  # Basic equality
  strict_mode: @equals(@env("RAILS_ENV"), "production")
  debug_enabled: @equals(@env("DEBUG"), "true")
  
  # Inequality
  cache_disabled: @not_equals(@env("CACHE_ENABLED"), "true")
  api_version: @not_equals(@env("API_VERSION"), "v1")
  
  # Case-insensitive equality
  database_type: @equals_ignore_case(@env("DB_TYPE"), "postgresql")
  log_level: @equals_ignore_case(@env("LOG_LEVEL"), "DEBUG")
TSK

# Ruby integration
require 'tusklang'

parser = TuskLang.new
config = parser.parse(tsk_content)

# Use in Ruby classes
class AppConfig
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def strict_mode?
    @config['validation']['strict_mode']
  end
  
  def debug_enabled?
    @config['validation']['debug_enabled']
  end
  
  def cache_disabled?
    @config['validation']['cache_disabled']
  end
end

# Usage
app_config = AppConfig.new(config)
puts "Strict mode: #{app_config.strict_mode?}"
puts "Debug enabled: #{app_config.debug_enabled?}"
```

### Numeric Comparisons

```ruby
# TuskLang with numeric comparisons
tsk_content = <<~TSK
  [performance]
  # Greater than
  high_memory: @greater_than(@php("memory_get_usage(true)"), 100000000)
  slow_response: @greater_than(@metrics("response_time_ms"), 500)
  
  # Less than
  low_memory: @less_than(@php("memory_get_usage(true)"), 50000000)
  fast_response: @less_than(@metrics("response_time_ms"), 100)
  
  # Greater than or equal
  sufficient_cpu: @greater_than_or_equal(@php("sys_getloadavg()[0]"), 0.5)
  minimum_users: @greater_than_or_equal(@query("SELECT COUNT(*) FROM users"), 100)
  
  # Less than or equal
  max_connections: @less_than_or_equal(@query("SELECT COUNT(*) FROM connections"), 1000)
  acceptable_latency: @less_than_or_equal(@metrics("latency_ms"), 200)
TSK

# Ruby integration with performance monitoring
class PerformanceMonitor
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def check_memory_usage
    current_memory = `ps -o rss= -p #{Process.pid}`.to_i * 1024
    
    if @config['performance']['high_memory']
      Rails.logger.warn "High memory usage detected: #{current_memory} bytes"
    end
    
    if @config['performance']['low_memory']
      Rails.logger.info "Memory usage is optimal: #{current_memory} bytes"
    end
  end
  
  def check_response_time(response_time)
    if @config['performance']['slow_response']
      Rails.logger.warn "Slow response detected: #{response_time}ms"
    end
    
    if @config['performance']['fast_response']
      Rails.logger.info "Fast response: #{response_time}ms"
    end
  end
end
```

## 🔧 Advanced Comparison Patterns

### Type-Safe Comparisons

```ruby
# TuskLang with type checking
tsk_content = <<~TSK
  [type_validation]
  # Type comparisons
  is_string: @is_string(@env("API_KEY"))
  is_number: @is_number(@env("PORT"))
  is_boolean: @is_boolean(@env("DEBUG"))
  is_array: @is_array(@env("ALLOWED_HOSTS"))
  is_object: @is_object(@env("DATABASE_CONFIG"))
  
  # Null checks
  has_api_key: @is_not_null(@env("API_KEY"))
  missing_config: @is_null(@env("DATABASE_URL"))
  
  # Empty checks
  has_content: @is_not_empty(@env("CONTENT"))
  empty_string: @is_empty(@env("OPTIONAL_CONFIG"))
TSK

# Ruby integration with type validation
class TypeValidator
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def validate_environment
    errors = []
    
    unless @config['type_validation']['is_string']
      errors << "API_KEY must be a string"
    end
    
    unless @config['type_validation']['is_number']
      errors << "PORT must be a number"
    end
    
    unless @config['type_validation']['has_api_key']
      errors << "API_KEY is required"
    end
    
    errors
  end
  
  def validate_config(config_hash)
    config_hash.each do |key, value|
      case key
      when 'api_key'
        unless @config['type_validation']['is_string']
          raise TypeError, "#{key} must be a string"
        end
      when 'port'
        unless @config['type_validation']['is_number']
          raise TypeError, "#{key} must be a number"
        end
      end
    end
  end
end
```

### Range Comparisons

```ruby
# TuskLang with range operations
tsk_content = <<~TSK
  [ranges]
  # In range checks
  port_valid: @in_range(@env("PORT"), 1024, 65535)
  memory_ok: @in_range(@php("memory_get_usage(true)"), 10000000, 1000000000)
  cpu_usage: @in_range(@php("sys_getloadavg()[0]"), 0.0, 5.0)
  
  # Between checks (inclusive)
  response_time_acceptable: @between(@metrics("response_time_ms"), 50, 500)
  user_count_healthy: @between(@query("SELECT COUNT(*) FROM users"), 100, 10000)
  
  # Outside range checks
  port_invalid: @outside_range(@env("PORT"), 1024, 65535)
  memory_critical: @outside_range(@php("memory_get_usage(true)"), 10000000, 1000000000)
TSK

# Ruby integration with range validation
class RangeValidator
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def validate_port(port)
    unless @config['ranges']['port_valid']
      raise ArgumentError, "Port #{port} is outside valid range (1024-65535)"
    end
  end
  
  def check_memory_health
    current_memory = `ps -o rss= -p #{Process.pid}`.to_i * 1024
    
    if @config['ranges']['memory_critical']
      Rails.logger.error "Critical memory usage: #{current_memory} bytes"
      # Trigger memory cleanup
      GC.start
    end
  end
  
  def validate_response_time(response_time)
    unless @config['ranges']['response_time_acceptable']
      Rails.logger.warn "Response time #{response_time}ms is outside acceptable range"
    end
  end
end
```

## 🎛️ String Comparison Operations

### Pattern Matching

```ruby
# TuskLang with string pattern matching
tsk_content = <<~TSK
  [patterns]
  # Regex matching
  valid_email: @matches(@env("ADMIN_EMAIL"), "^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$")
  valid_url: @matches(@env("API_URL"), "^https?://[^\\s/$.?#].[^\\s]*$")
  valid_version: @matches(@env("APP_VERSION"), "^\\d+\\.\\d+\\.\\d+$")
  
  # Contains checks
  has_ssl: @contains(@env("DATABASE_URL"), "ssl=true")
  has_debug: @contains(@env("LOG_LEVEL"), "debug")
  
  # Starts/ends with
  is_https: @starts_with(@env("API_URL"), "https://")
  is_json: @ends_with(@env("RESPONSE_FORMAT"), "json")
  
  # Case-sensitive string operations
  exact_match: @equals_case_sensitive(@env("ENVIRONMENT"), "PRODUCTION")
  case_insensitive: @equals_ignore_case(@env("ENVIRONMENT"), "production")
TSK

# Ruby integration with pattern validation
class PatternValidator
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def validate_email(email)
    unless @config['patterns']['valid_email']
      raise ArgumentError, "Invalid email format: #{email}"
    end
  end
  
  def validate_url(url)
    unless @config['patterns']['valid_url']
      raise ArgumentError, "Invalid URL format: #{url}"
    end
  end
  
  def check_ssl_requirement(database_url)
    if @config['patterns']['has_ssl'] && !database_url.include?('ssl=true')
      Rails.logger.warn "SSL is required but not configured in database URL"
    end
  end
  
  def validate_environment(env)
    unless @config['patterns']['exact_match']
      Rails.logger.warn "Environment should be exactly 'PRODUCTION'"
    end
  end
end
```

## 🔄 Dynamic Comparisons

### Time-Based Comparisons

```ruby
# TuskLang with time comparisons
tsk_content = <<~TSK
  [time_checks]
  # Time comparisons
  is_business_hours: @between(@date("H"), 9, 17)
  is_weekend: @in_array(@date("N"), [6, 7])
  is_month_end: @equals(@date("j"), @date("t"))
  
  # Date range checks
  is_holiday: @in_array(@date("Y-m-d"), ["2024-12-25", "2024-01-01", "2024-07-04"])
  is_maintenance_window: @between(@date("H"), 2, 4)
  
  # Relative time checks
  is_recent: @less_than(@date.diff(@file.modified("config.tsk")), "1h")
  is_old_cache: @greater_than(@date.diff(@file.modified("cache.db")), "24h")
TSK

# Ruby integration with time-based logic
class TimeBasedLogic
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def business_hours?
    @config['time_checks']['is_business_hours']
  end
  
  def weekend?
    @config['time_checks']['is_weekend']
  end
  
  def maintenance_mode?
    @config['time_checks']['is_maintenance_window']
  end
  
  def should_send_notifications?
    business_hours? && !weekend? && !maintenance_mode?
  end
  
  def check_cache_freshness
    if @config['time_checks']['is_old_cache']
      Rails.logger.info "Cache is stale, refreshing..."
      # Trigger cache refresh
      Rails.cache.clear
    end
  end
end
```

### Database-Driven Comparisons

```ruby
# TuskLang with database comparisons
tsk_content = <<~TSK
  [database_checks]
  # Record existence
  has_active_users: @greater_than(@query("SELECT COUNT(*) FROM users WHERE active = 1"), 0)
  has_pending_orders: @greater_than(@query("SELECT COUNT(*) FROM orders WHERE status = 'pending'"), 0)
  
  # Data freshness
  recent_activity: @less_than(@date.diff(@query("SELECT MAX(created_at) FROM user_activities")), "1h")
  stale_data: @greater_than(@date.diff(@query("SELECT MAX(updated_at) FROM cache_table")), "24h")
  
  # Performance metrics
  high_error_rate: @greater_than(@query("SELECT COUNT(*) FROM error_logs WHERE created_at > ?", @date.subtract("1h")), 100)
  low_performance: @greater_than(@query("SELECT AVG(response_time) FROM performance_logs WHERE created_at > ?", @date.subtract("1h")), 500)
TSK

# Ruby integration with database monitoring
class DatabaseMonitor
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def check_system_health
    health_status = {}
    
    if @config['database_checks']['has_active_users']
      health_status[:active_users] = "OK"
    else
      health_status[:active_users] = "WARNING: No active users"
    end
    
    if @config['database_checks']['high_error_rate']
      health_status[:error_rate] = "CRITICAL: High error rate detected"
    else
      health_status[:error_rate] = "OK"
    end
    
    if @config['database_checks']['low_performance']
      health_status[:performance] = "WARNING: Low performance detected"
    else
      health_status[:performance] = "OK"
    end
    
    health_status
  end
  
  def trigger_maintenance_if_needed
    if @config['database_checks']['stale_data']
      Rails.logger.warn "Stale data detected, triggering maintenance"
      # Trigger data refresh
      DataRefreshJob.perform_later
    end
  end
end
```

## 🛡️ Error Handling and Validation

### Comprehensive Validation System

```ruby
# TuskLang with comprehensive validation
tsk_content = <<~TSK
  [validation_rules]
  # Required fields
  api_key_required: @is_not_null(@env("API_KEY"))
  database_url_required: @is_not_empty(@env("DATABASE_URL"))
  
  # Format validation
  email_format: @matches(@env("ADMIN_EMAIL"), "^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$")
  port_format: @matches(@env("PORT"), "^\\d+$")
  
  # Range validation
  port_range: @in_range(@env("PORT"), 1024, 65535)
  memory_limit: @less_than(@php("memory_get_usage(true)"), 1000000000)
  
  # Business logic
  production_ssl: @implies(@equals(@env("RAILS_ENV"), "production"), @contains(@env("DATABASE_URL"), "ssl=true"))
  debug_logging: @implies(@equals(@env("DEBUG"), "true"), @equals(@env("LOG_LEVEL"), "debug"))
TSK

# Ruby integration with validation system
class ConfigurationValidator
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def validate_all
    errors = []
    
    # Required field validation
    unless @config['validation_rules']['api_key_required']
      errors << "API_KEY is required"
    end
    
    unless @config['validation_rules']['database_url_required']
      errors << "DATABASE_URL is required"
    end
    
    # Format validation
    unless @config['validation_rules']['email_format']
      errors << "ADMIN_EMAIL has invalid format"
    end
    
    unless @config['validation_rules']['port_format']
      errors << "PORT must be a number"
    end
    
    # Range validation
    unless @config['validation_rules']['port_range']
      errors << "PORT must be between 1024 and 65535"
    end
    
    # Business logic validation
    unless @config['validation_rules']['production_ssl']
      errors << "Production environment requires SSL in database URL"
    end
    
    errors
  end
  
  def validate_and_raise
    errors = validate_all
    unless errors.empty?
      raise ConfigurationError, "Configuration validation failed:\n#{errors.join("\n")}"
    end
  end
end

# Custom exception class
class ConfigurationError < StandardError; end
```

## 🚀 Performance Optimization

### Efficient Comparison Strategies

```ruby
# TuskLang with performance optimizations
tsk_content = <<~TSK
  [performance_optimizations]
  # Cached comparisons
  cached_user_count: @cache("5m", @query("SELECT COUNT(*) FROM users"))
  cached_memory_usage: @cache("1m", @php("memory_get_usage(true)"))
  
  # Lazy evaluation
  expensive_check: @when(@env("DEBUG"), @query("SELECT COUNT(*) FROM debug_logs"))
  conditional_validation: @when(@env("VALIDATE_CONFIG"), @validate.required(["api_key", "database_url"]))
  
  # Batch operations
  user_stats: @batch([
    @query("SELECT COUNT(*) FROM users"),
    @query("SELECT COUNT(*) FROM users WHERE active = 1"),
    @query("SELECT COUNT(*) FROM users WHERE created_at > ?", @date.subtract("30d"))
  ])
TSK

# Ruby integration with performance optimizations
class OptimizedValidator
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def cached_validation
    Rails.cache.fetch("validation_results", expires_in: 5.minutes) do
      perform_expensive_validations
    end
  end
  
  def lazy_validation
    return unless ENV['VALIDATE_CONFIG'] == 'true'
    
    @config['performance_optimizations']['conditional_validation']
  end
  
  def batch_validation
    results = @config['performance_optimizations']['user_stats']
    
    {
      total_users: results[0],
      active_users: results[1],
      recent_users: results[2]
    }
  end
  
  private
  
  def perform_expensive_validations
    # Expensive validation logic here
    {
      database_connection: test_database_connection,
      api_endpoints: test_api_endpoints,
      file_permissions: check_file_permissions
    }
  end
end
```

## 🎯 Best Practices

### 1. Use Descriptive Names
```ruby
# Good
is_production_environment: @equals(@env("RAILS_ENV"), "production")
has_valid_api_key: @is_not_empty(@env("API_KEY"))

# Avoid
prod: @equals(@env("RAILS_ENV"), "production")
key: @is_not_empty(@env("API_KEY"))
```

### 2. Combine Multiple Conditions
```ruby
# TuskLang with logical combinations
tsk_content = <<~TSK
  [complex_validation]
  # Logical AND
  production_ready: @and(
    @equals(@env("RAILS_ENV"), "production"),
    @is_not_empty(@env("API_KEY")),
    @contains(@env("DATABASE_URL"), "ssl=true")
  )
  
  # Logical OR
  debug_mode: @or(
    @equals(@env("DEBUG"), "true"),
    @equals(@env("LOG_LEVEL"), "debug")
  )
  
  # Logical NOT
  not_maintenance: @not(@equals(@env("MAINTENANCE_MODE"), "true"))
TSK
```

### 3. Use Caching for Expensive Operations
```ruby
# Cache expensive database queries
expensive_validation: @cache("10m", @query("SELECT COUNT(*) FROM large_table WHERE complex_condition = 1"))
```

### 4. Handle Edge Cases
```ruby
# TuskLang with edge case handling
tsk_content = <<~TSK
  [edge_cases]
  # Null-safe comparisons
  safe_string_compare: @equals(@env("OPTIONAL_CONFIG") || "", "expected_value")
  safe_number_compare: @greater_than(@env("PORT") || "0", 1024)
  
  # Default values
  with_default: @equals(@env("LOG_LEVEL") || "info", "debug")
  fallback_check: @is_not_empty(@env("API_KEY") || @file.read("backup_key.txt"))
TSK
```

## 🔧 Troubleshooting

### Common Issues and Solutions

```ruby
# Issue: Type mismatches in comparisons
# Solution: Use type-safe operators
tsk_content = <<~TSK
  [type_safe_comparisons]
  # Instead of direct comparison
  # port_check: @equals(@env("PORT"), 8080)  # May fail if PORT is string
  
  # Use type-safe comparison
  port_check: @equals(@env("PORT").to_i, 8080)
  string_check: @equals(@env("PORT").to_s, "8080")
TSK

# Issue: Performance problems with frequent database queries
# Solution: Use caching
tsk_content = <<~TSK
  [cached_comparisons]
  # Cache expensive operations
  user_count: @cache("5m", @query("SELECT COUNT(*) FROM users"))
  active_sessions: @cache("1m", @query("SELECT COUNT(*) FROM sessions WHERE active = 1"))
TSK

# Issue: Complex validation logic
# Solution: Break into smaller, reusable parts
tsk_content = <<~TSK
  [modular_validation]
  # Break complex validation into parts
  has_required_fields: @and(
    @is_not_empty(@env("API_KEY")),
    @is_not_empty(@env("DATABASE_URL"))
  )
  
  has_valid_format: @and(
    @matches(@env("EMAIL"), "^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$"),
    @in_range(@env("PORT"), 1024, 65535)
  )
  
  is_production_ready: @and(
    @equals(@env("RAILS_ENV"), "production"),
    @contains(@env("DATABASE_URL"), "ssl=true"),
    @is_not_empty(@env("API_KEY"))
  )
TSK
```

## 🎯 Summary

TuskLang's comparison operators provide powerful validation and conditional logic capabilities that integrate seamlessly with Ruby applications. By leveraging these operators, you can:

- **Validate configuration** with type-safe comparisons
- **Monitor system health** with real-time metrics
- **Implement business logic** with complex conditional rules
- **Optimize performance** with caching and lazy evaluation
- **Handle edge cases** with null-safe operations

The Ruby integration makes these operators even more powerful by combining TuskLang's declarative syntax with Ruby's object-oriented features and rich ecosystem.

**Remember**: TuskLang comparison operators are designed to be expressive, performant, and Ruby-friendly. Use them to create robust, maintainable configurations that adapt to your application's needs. 