# 🧮 Math Operations in TuskLang - Ruby Edition

**"We don't bow to any king" - Ruby Edition**

TuskLang's math operations provide powerful computational capabilities that integrate seamlessly with Ruby's numeric system, enabling dynamic calculations, performance metrics, and real-time data processing.

## 🎯 Overview

Math operations in TuskLang allow you to perform calculations directly in configuration files, enabling dynamic values, performance monitoring, and complex business logic. When combined with Ruby's rich numeric types and mathematical libraries, these operations become incredibly powerful.

## 🚀 Basic Math Operations

### Arithmetic Operations

```ruby
# TuskLang configuration with basic math operations
tsk_content = <<~TSK
  [calculations]
  # Basic arithmetic
  total_users: @add(@query("SELECT COUNT(*) FROM users"), @query("SELECT COUNT(*) FROM pending_users"))
  average_response_time: @divide(@query("SELECT SUM(response_time) FROM requests"), @query("SELECT COUNT(*) FROM requests"))
  memory_usage_mb: @divide(@php("memory_get_usage(true)"), 1048576)
  
  # Percentage calculations
  success_rate: @multiply(@divide(@query("SELECT COUNT(*) FROM successful_requests"), @query("SELECT COUNT(*) FROM requests")), 100)
  cpu_usage_percent: @multiply(@php("sys_getloadavg()[0]"), 100)
  
  # Increment/decrement
  next_user_id: @add(@query("SELECT MAX(id) FROM users"), 1)
  previous_version: @subtract(@env("APP_VERSION"), 1)
TSK

# Ruby integration
require 'tusklang'

parser = TuskLang.new
config = parser.parse(tsk_content)

# Use in Ruby classes
class MetricsCalculator
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def total_users
    @config['calculations']['total_users']
  end
  
  def average_response_time
    @config['calculations']['average_response_time']
  end
  
  def memory_usage_mb
    @config['calculations']['memory_usage_mb']
  end
  
  def success_rate
    @config['calculations']['success_rate']
  end
  
  def format_metrics
    {
      total_users: total_users,
      avg_response_time: "#{average_response_time.round(2)}ms",
      memory_usage: "#{memory_usage_mb.round(2)}MB",
      success_rate: "#{success_rate.round(2)}%"
    }
  end
end

# Usage
calculator = MetricsCalculator.new(config)
puts calculator.format_metrics
```

### Advanced Mathematical Functions

```ruby
# TuskLang with advanced math functions
tsk_content = <<~TSK
  [advanced_math]
  # Power and roots
  memory_gb: @divide(@php("memory_get_usage(true)"), @power(1024, 3))
  response_time_seconds: @divide(@metrics("response_time_ms"), 1000)
  
  # Logarithmic functions
  log_memory: @log(@php("memory_get_usage(true)"))
  log_response_time: @log(@metrics("response_time_ms"))
  
  # Trigonometric functions
  angle_radians: @multiply(@env("ANGLE_DEGREES"), @divide(@pi, 180))
  sine_value: @sin(@angle_radians)
  cosine_value: @cos(@angle_radians)
  
  # Rounding functions
  rounded_users: @round(@query("SELECT COUNT(*) FROM users"))
  ceiling_memory: @ceil(@divide(@php("memory_get_usage(true)"), 1048576))
  floor_cpu: @floor(@php("sys_getloadavg()[0]"))
TSK

# Ruby integration with advanced math
class AdvancedMathProcessor
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def memory_gb
    @config['advanced_math']['memory_gb']
  end
  
  def log_memory
    @config['advanced_math']['log_memory']
  end
  
  def sine_value
    @config['advanced_math']['sine_value']
  end
  
  def rounded_users
    @config['advanced_math']['rounded_users']
  end
  
  def analyze_performance
    {
      memory_gb: memory_gb.round(3),
      log_memory: log_memory.round(3),
      sine_value: sine_value.round(3),
      total_users: rounded_users
    }
  end
end
```

## 🔧 Statistical Operations

### Descriptive Statistics

```ruby
# TuskLang with statistical operations
tsk_content = <<~TSK
  [statistics]
  # Basic statistics
  user_count: @query("SELECT COUNT(*) FROM users")
  avg_age: @query("SELECT AVG(age) FROM users")
  min_age: @query("SELECT MIN(age) FROM users")
  max_age: @query("SELECT MAX(age) FROM users")
  
  # Variance and standard deviation
  age_variance: @query("SELECT VARIANCE(age) FROM users")
  age_std_dev: @query("SELECT STDDEV(age) FROM users")
  
  # Percentiles
  median_age: @query("SELECT PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY age) FROM users")
  p95_response_time: @query("SELECT PERCENTILE_CONT(0.95) WITHIN GROUP (ORDER BY response_time) FROM requests")
  
  # Moving averages
  avg_response_time_1h: @query("SELECT AVG(response_time) FROM requests WHERE created_at > ?", @date.subtract("1h"))
  avg_response_time_24h: @query("SELECT AVG(response_time) FROM requests WHERE created_at > ?", @date.subtract("24h"))
TSK

# Ruby integration with statistics
class StatisticsAnalyzer
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def user_statistics
    {
      count: @config['statistics']['user_count'],
      average_age: @config['statistics']['avg_age']&.round(2),
      min_age: @config['statistics']['min_age'],
      max_age: @config['statistics']['max_age'],
      median_age: @config['statistics']['median_age']&.round(2),
      age_variance: @config['statistics']['age_variance']&.round(2),
      age_std_dev: @config['statistics']['age_std_dev']&.round(2)
    }
  end
  
  def performance_metrics
    {
      p95_response_time: @config['statistics']['p95_response_time']&.round(2),
      avg_1h: @config['statistics']['avg_response_time_1h']&.round(2),
      avg_24h: @config['statistics']['avg_response_time_24h']&.round(2)
    }
  end
  
  def generate_report
    {
      users: user_statistics,
      performance: performance_metrics,
      generated_at: Time.current
    }
  end
end
```

### Time Series Analysis

```ruby
# TuskLang with time series calculations
tsk_content = <<~TSK
  [time_series]
  # Growth rates
  user_growth_rate: @divide(@subtract(@query("SELECT COUNT(*) FROM users WHERE created_at > ?", @date.subtract("30d")), @query("SELECT COUNT(*) FROM users WHERE created_at > ?", @date.subtract("60d"))), @query("SELECT COUNT(*) FROM users WHERE created_at > ?", @date.subtract("60d")))
  
  # Rate of change
  request_rate_change: @divide(@subtract(@query("SELECT COUNT(*) FROM requests WHERE created_at > ?", @date.subtract("1h")), @query("SELECT COUNT(*) FROM requests WHERE created_at > ? AND created_at < ?", @date.subtract("2h"), @date.subtract("1h"))), @query("SELECT COUNT(*) FROM requests WHERE created_at > ? AND created_at < ?", @date.subtract("2h"), @date.subtract("1h")))
  
  # Exponential moving average
  ema_response_time: @multiply(@query("SELECT AVG(response_time) FROM requests WHERE created_at > ?", @date.subtract("1h")), 0.1) + @multiply(@query("SELECT AVG(response_time) FROM requests WHERE created_at > ? AND created_at < ?", @date.subtract("2h"), @date.subtract("1h")), 0.9)
TSK

# Ruby integration with time series
class TimeSeriesAnalyzer
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def user_growth_rate
    @config['time_series']['user_growth_rate']
  end
  
  def request_rate_change
    @config['time_series']['request_rate_change']
  end
  
  def ema_response_time
    @config['time_series']['ema_response_time']
  end
  
  def analyze_trends
    {
      user_growth: "#{(user_growth_rate * 100).round(2)}%",
      request_change: "#{(request_rate_change * 100).round(2)}%",
      ema_response: "#{ema_response_time.round(2)}ms"
    }
  end
end
```

## 🎛️ Financial Calculations

### Business Metrics

```ruby
# TuskLang with financial calculations
tsk_content = <<~TSK
  [financial]
  # Revenue calculations
  total_revenue: @query("SELECT SUM(amount) FROM transactions WHERE status = 'completed'")
  monthly_revenue: @query("SELECT SUM(amount) FROM transactions WHERE status = 'completed' AND created_at > ?", @date.subtract("30d"))
  daily_revenue: @query("SELECT SUM(amount) FROM transactions WHERE status = 'completed' AND created_at > ?", @date.subtract("1d"))
  
  # Conversion rates
  conversion_rate: @multiply(@divide(@query("SELECT COUNT(*) FROM transactions WHERE status = 'completed'"), @query("SELECT COUNT(*) FROM users")), 100)
  
  # Average order value
  aov: @divide(@query("SELECT SUM(amount) FROM transactions WHERE status = 'completed'"), @query("SELECT COUNT(*) FROM transactions WHERE status = 'completed'"))
  
  # Customer lifetime value
  clv: @multiply(@aov, @query("SELECT AVG(transaction_count) FROM (SELECT user_id, COUNT(*) as transaction_count FROM transactions WHERE status = 'completed' GROUP BY user_id) as user_transactions"))
TSK

# Ruby integration with financial metrics
class FinancialAnalyzer
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def revenue_metrics
    {
      total: @config['financial']['total_revenue']&.round(2),
      monthly: @config['financial']['monthly_revenue']&.round(2),
      daily: @config['financial']['daily_revenue']&.round(2)
    }
  end
  
  def business_metrics
    {
      conversion_rate: "#{@config['financial']['conversion_rate']&.round(2)}%",
      aov: "$#{@config['financial']['aov']&.round(2)}",
      clv: "$#{@config['financial']['clv']&.round(2)}"
    }
  end
  
  def generate_financial_report
    {
      revenue: revenue_metrics,
      metrics: business_metrics,
      report_date: Time.current
    }
  end
end
```

### Performance Ratios

```ruby
# TuskLang with performance ratios
tsk_content = <<~TSK
  [performance_ratios]
  # Efficiency ratios
  memory_efficiency: @divide(@query("SELECT COUNT(*) FROM active_sessions"), @divide(@php("memory_get_usage(true)"), 1048576))
  cpu_efficiency: @divide(@query("SELECT COUNT(*) FROM requests"), @php("sys_getloadavg()[0]"))
  
  # Error rates
  error_rate: @multiply(@divide(@query("SELECT COUNT(*) FROM error_logs WHERE created_at > ?", @date.subtract("1h")), @query("SELECT COUNT(*) FROM requests WHERE created_at > ?", @date.subtract("1h"))), 100)
  
  # Availability
  uptime_percentage: @multiply(@divide(@query("SELECT COUNT(*) FROM health_checks WHERE status = 'healthy' AND created_at > ?", @date.subtract("24h")), @query("SELECT COUNT(*) FROM health_checks WHERE created_at > ?", @date.subtract("24h"))), 100)
TSK

# Ruby integration with performance ratios
class PerformanceRatioAnalyzer
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def efficiency_metrics
    {
      memory_efficiency: @config['performance_ratios']['memory_efficiency']&.round(2),
      cpu_efficiency: @config['performance_ratios']['cpu_efficiency']&.round(2)
    }
  end
  
  def reliability_metrics
    {
      error_rate: "#{@config['performance_ratios']['error_rate']&.round(3)}%",
      uptime: "#{@config['performance_ratios']['uptime_percentage']&.round(2)}%"
    }
  end
  
  def health_status
    error_rate = @config['performance_ratios']['error_rate']
    uptime = @config['performance_ratios']['uptime_percentage']
    
    if error_rate && error_rate > 5.0
      "CRITICAL"
    elsif uptime && uptime < 99.9
      "WARNING"
    else
      "HEALTHY"
    end
  end
end
```

## 🔄 Dynamic Calculations

### Real-Time Metrics

```ruby
# TuskLang with real-time calculations
tsk_content = <<~TSK
  [real_time]
  # Current load calculations
  current_load_percent: @multiply(@php("sys_getloadavg()[0]"), 100)
  memory_usage_percent: @multiply(@divide(@php("memory_get_usage(true)"), @php("memory_get_peak_usage(true)")), 100)
  
  # Request rate calculations
  requests_per_second: @divide(@query("SELECT COUNT(*) FROM requests WHERE created_at > ?", @date.subtract("1m")), 60)
  requests_per_minute: @query("SELECT COUNT(*) FROM requests WHERE created_at > ?", @date.subtract("1m"))
  
  # Queue depth calculations
  queue_depth: @query("SELECT COUNT(*) FROM job_queue WHERE status = 'pending'")
  queue_processing_rate: @divide(@query("SELECT COUNT(*) FROM job_queue WHERE status = 'completed' AND updated_at > ?", @date.subtract("1m")), 60)
TSK

# Ruby integration with real-time monitoring
class RealTimeMonitor
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def system_metrics
    {
      cpu_load: "#{@config['real_time']['current_load_percent']&.round(1)}%",
      memory_usage: "#{@config['real_time']['memory_usage_percent']&.round(1)}%"
    }
  end
  
  def request_metrics
    {
      per_second: @config['real_time']['requests_per_second']&.round(2),
      per_minute: @config['real_time']['requests_per_minute']
    }
  end
  
  def queue_metrics
    {
      depth: @config['real_time']['queue_depth'],
      processing_rate: @config['real_time']['queue_processing_rate']&.round(2)
    }
  end
  
  def alert_if_needed
    cpu_load = @config['real_time']['current_load_percent']
    memory_usage = @config['real_time']['memory_usage_percent']
    
    if cpu_load && cpu_load > 80
      Rails.logger.warn "High CPU load: #{cpu_load}%"
    end
    
    if memory_usage && memory_usage > 90
      Rails.logger.error "Critical memory usage: #{memory_usage}%"
    end
  end
end
```

### Predictive Calculations

```ruby
# TuskLang with predictive calculations
tsk_content = <<~TSK
  [predictive]
  # Linear trend prediction
  predicted_users_next_month: @add(@query("SELECT COUNT(*) FROM users"), @multiply(@query("SELECT COUNT(*) FROM users WHERE created_at > ?", @date.subtract("30d")), 1))
  
  # Exponential growth prediction
  predicted_revenue_next_quarter: @multiply(@query("SELECT SUM(amount) FROM transactions WHERE created_at > ?", @date.subtract("90d")), @power(1.1, 3))
  
  # Capacity planning
  predicted_storage_needed: @multiply(@query("SELECT AVG(storage_used) FROM users"), @predicted_users_next_month)
  predicted_bandwidth_needed: @multiply(@query("SELECT AVG(bandwidth_used) FROM users"), @predicted_users_next_month)
TSK

# Ruby integration with predictive analytics
class PredictiveAnalyzer
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def growth_predictions
    {
      users_next_month: @config['predictive']['predicted_users_next_month']&.round,
      revenue_next_quarter: @config['predictive']['predicted_revenue_next_quarter']&.round(2)
    }
  end
  
  def capacity_predictions
    {
      storage_needed: @config['predictive']['predicted_storage_needed']&.round(2),
      bandwidth_needed: @config['predictive']['predicted_bandwidth_needed']&.round(2)
    }
  end
  
  def generate_forecast_report
    {
      growth: growth_predictions,
      capacity: capacity_predictions,
      forecast_date: Time.current,
      confidence_level: "85%"
    }
  end
end
```

## 🛡️ Error Handling and Validation

### Mathematical Error Handling

```ruby
# TuskLang with mathematical error handling
tsk_content = <<~TSK
  [safe_math]
  # Safe division (avoid division by zero)
  safe_average: @safe_divide(@query("SELECT SUM(response_time) FROM requests"), @query("SELECT COUNT(*) FROM requests"), 0)
  
  # Safe logarithm (avoid log of zero or negative)
  safe_log: @safe_log(@php("memory_get_usage(true)"), 0)
  
  # Safe power (avoid overflow)
  safe_power: @safe_power(@env("BASE_VALUE"), @env("EXPONENT"), 1000000)
  
  # Bounded calculations
  bounded_percentage: @clamp(@multiply(@divide(@query("SELECT COUNT(*) FROM successful_requests"), @query("SELECT COUNT(*) FROM requests")), 100), 0, 100)
  bounded_memory: @clamp(@divide(@php("memory_get_usage(true)"), 1048576), 0, 1000)
TSK

# Ruby integration with safe math
class SafeMathProcessor
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def safe_average
    @config['safe_math']['safe_average']
  end
  
  def safe_log
    @config['safe_math']['safe_log']
  end
  
  def bounded_percentage
    @config['safe_math']['bounded_percentage']
  end
  
  def validate_calculations
    errors = []
    
    if safe_average.nil? || safe_average < 0
      errors << "Invalid average calculation"
    end
    
    if safe_log.nil? || safe_log.infinite?
      errors << "Invalid logarithm calculation"
    end
    
    if bounded_percentage.nil? || bounded_percentage < 0 || bounded_percentage > 100
      errors << "Percentage out of valid range"
    end
    
    errors
  end
end
```

## 🚀 Performance Optimization

### Efficient Mathematical Operations

```ruby
# TuskLang with performance optimizations
tsk_content = <<~TSK
  [optimized_math]
  # Cached calculations
  cached_user_count: @cache("5m", @query("SELECT COUNT(*) FROM users"))
  cached_average_response: @cache("1m", @query("SELECT AVG(response_time) FROM requests WHERE created_at > ?", @date.subtract("1h")))
  
  # Batch calculations
  user_stats_batch: @batch([
    @query("SELECT COUNT(*) FROM users"),
    @query("SELECT AVG(age) FROM users"),
    @query("SELECT SUM(login_count) FROM users")
  ])
  
  # Incremental calculations
  incremental_revenue: @add(@cache("1h", @query("SELECT SUM(amount) FROM transactions WHERE created_at > ?", @date.subtract("24h"))), @query("SELECT SUM(amount) FROM transactions WHERE created_at > ?", @date.subtract("1h")))
TSK

# Ruby integration with optimized math
class OptimizedMathProcessor
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def cached_user_count
    @config['optimized_math']['cached_user_count']
  end
  
  def user_stats_batch
    results = @config['optimized_math']['user_stats_batch']
    {
      count: results[0],
      avg_age: results[1]&.round(2),
      total_logins: results[2]
    }
  end
  
  def incremental_revenue
    @config['optimized_math']['incremental_revenue']
  end
  
  def perform_optimized_calculations
    Rails.cache.fetch("optimized_calculations", expires_in: 5.minutes) do
      {
        users: user_stats_batch,
        revenue: incremental_revenue,
        cached_count: cached_user_count
      }
    end
  end
end
```

## 🎯 Best Practices

### 1. Use Descriptive Variable Names
```ruby
# Good
daily_active_users: @query("SELECT COUNT(*) FROM users WHERE last_login > ?", @date.subtract("1d"))
monthly_revenue_growth: @multiply(@divide(@subtract(@monthly_revenue, @previous_monthly_revenue), @previous_monthly_revenue), 100)

# Avoid
dau: @query("SELECT COUNT(*) FROM users WHERE last_login > ?", @date.subtract("1d"))
mrg: @multiply(@divide(@subtract(@mr, @pmr), @pmr), 100)
```

### 2. Handle Edge Cases
```ruby
# TuskLang with edge case handling
tsk_content = <<~TSK
  [edge_cases]
  # Handle division by zero
  safe_ratio: @safe_divide(@query("SELECT COUNT(*) FROM successful_requests"), @query("SELECT COUNT(*) FROM requests"), 0)
  
  # Handle null values
  safe_average: @coalesce(@query("SELECT AVG(amount) FROM transactions"), 0)
  
  # Handle overflow
  safe_product: @clamp(@multiply(@env("FACTOR_A"), @env("FACTOR_B")), 0, 1000000)
TSK
```

### 3. Use Caching for Expensive Operations
```ruby
# Cache expensive calculations
expensive_calculation: @cache("10m", @query("SELECT AVG(complex_calculation) FROM large_table"))
```

### 4. Validate Results
```ruby
# Validate mathematical results
tsk_content = <<~TSK
  [validation]
  # Ensure positive values
  positive_count: @max(@query("SELECT COUNT(*) FROM users"), 0)
  
  # Ensure reasonable ranges
  reasonable_percentage: @clamp(@multiply(@divide(@successful_requests, @total_requests), 100), 0, 100)
  
  # Check for NaN or infinite values
  valid_average: @is_finite(@query("SELECT AVG(response_time) FROM requests"))
TSK
```

## 🔧 Troubleshooting

### Common Mathematical Issues

```ruby
# Issue: Division by zero
# Solution: Use safe division
tsk_content = <<~TSK
  [safe_division]
  # Instead of direct division
  # average: @divide(@sum, @count)  # May fail if count is 0
  
  # Use safe division
  average: @safe_divide(@sum, @count, 0)  # Returns 0 if count is 0
TSK

# Issue: Overflow in calculations
# Solution: Use bounded operations
tsk_content = <<~TSK
  [bounded_calculations]
  # Prevent overflow
  safe_product: @clamp(@multiply(@large_number_a, @large_number_b), 0, 1000000)
  safe_power: @safe_power(@base, @exponent, 1000000)
TSK

# Issue: Precision loss in floating point
# Solution: Use appropriate precision
tsk_content = <<~TSK
  [precision_control]
  # Control precision
  precise_percentage: @round(@multiply(@divide(@successful, @total), 100), 2)
  precise_average: @round(@divide(@sum, @count), 4)
TSK
```

## 🎯 Summary

TuskLang's math operations provide powerful computational capabilities that integrate seamlessly with Ruby applications. By leveraging these operations, you can:

- **Perform dynamic calculations** directly in configuration files
- **Monitor system performance** with real-time metrics
- **Calculate business metrics** and financial ratios
- **Implement predictive analytics** and forecasting
- **Handle mathematical edge cases** safely and efficiently

The Ruby integration makes these operations even more powerful by combining TuskLang's declarative syntax with Ruby's rich mathematical libraries and object-oriented features.

**Remember**: TuskLang math operations are designed to be expressive, performant, and Ruby-friendly. Use them to create dynamic, data-driven configurations that adapt to your application's needs.

**Key Takeaways**:
- Use safe mathematical operations to handle edge cases
- Cache expensive calculations for better performance
- Validate results to ensure data integrity
- Combine with Ruby's mathematical libraries for advanced functionality
- Monitor performance and handle errors gracefully 