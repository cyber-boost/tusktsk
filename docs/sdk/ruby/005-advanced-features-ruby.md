# 🚀 TuskLang Ruby Advanced Features Guide

**"We don't bow to any king" - Ruby Edition**

Master the advanced features of TuskLang in Ruby. From caching and monitoring to performance optimization and machine learning integration, discover the full power of revolutionary configuration.

## 🔄 Advanced Caching

### 1. Multi-Level Caching
```ruby
# config/caching.tsk
$environment: @env("RAILS_ENV", "development")

[cache]
# Primary cache (Redis)
primary {
    driver: "redis"
    host: @env("REDIS_HOST", "localhost")
    port: @env("REDIS_PORT", 6379)
    db: 0
    ttl: "5m"
}

# Secondary cache (Memory)
secondary {
    driver: "memory"
    max_size: 1000
    ttl: "1m"
}

# Distributed cache (Memcached)
distributed {
    driver: "memcached"
    servers: ["memcached1:11211", "memcached2:11211"]
    ttl: "10m"
}

[analytics]
# Multi-level cached queries
total_users: @cache.primary("5m", @query("SELECT COUNT(*) FROM users"))
active_users: @cache.secondary("1m", @query("SELECT COUNT(*) FROM users WHERE active = true"))
premium_users: @cache.distributed("10m", @query("SELECT COUNT(*) FROM users WHERE subscription_type = 'premium'"))

# Cache with custom keys
user_stats: @cache.primary("user_stats:#{@date.today()}", "1h", @query("""
    SELECT 
        COUNT(*) as total,
        COUNT(CASE WHEN active = true THEN 1 END) as active,
        COUNT(CASE WHEN subscription_type = 'premium' THEN 1 END) as premium
    FROM users
"""))
```

### 2. Cache Invalidation Strategies
```ruby
# config/cache_invalidation.tsk
[cache]
# Cache with automatic invalidation
user_count: @cache("5m", @query("SELECT COUNT(*) FROM users"), {
    invalidate_on: ["users.created", "users.deleted"],
    tags: ["users", "analytics"]
})

order_stats: @cache("1m", @query("SELECT COUNT(*) FROM orders"), {
    invalidate_on: ["orders.created", "orders.updated"],
    tags: ["orders", "analytics"]
})

# Cache with manual invalidation
premium_users: @cache("10m", @query("SELECT COUNT(*) FROM users WHERE subscription_type = 'premium'"), {
    key: "premium_users_count",
    tags: ["users", "premium"]
})

# Cache with conditional invalidation
revenue_stats: @cache("1h", @query("SELECT SUM(amount) FROM orders"), {
    invalidate_if: @query("SELECT COUNT(*) FROM orders WHERE updated_at > ?", @cache.get("revenue_stats_last_update")),
    tags: ["orders", "revenue"]
})
```

### 3. Ruby Cache Integration
```ruby
# app/services/cache_service.rb
class CacheService
  def self.primary_cache
    @primary_cache ||= TuskLang::Cache::RedisCache.new(
      host: ENV['REDIS_HOST'] || 'localhost',
      port: ENV['REDIS_PORT'] || 6379,
      db: 0
    )
  end
  
  def self.secondary_cache
    @secondary_cache ||= TuskLang::Cache::MemoryCache.new(
      max_size: 1000,
      ttl: 60
    )
  end
  
  def self.distributed_cache
    @distributed_cache ||= TuskLang::Cache::MemcachedCache.new(
      servers: ENV['MEMCACHED_SERVERS']&.split(',') || ['localhost:11211']
    )
  end
  
  def self.invalidate_by_tags(tags)
    primary_cache.invalidate_tags(tags)
    secondary_cache.invalidate_tags(tags)
    distributed_cache.invalidate_tags(tags)
  end
  
  def self.invalidate_by_pattern(pattern)
    primary_cache.invalidate_pattern(pattern)
    secondary_cache.invalidate_pattern(pattern)
    distributed_cache.invalidate_pattern(pattern)
  end
end

# app/controllers/api/v1/analytics_controller.rb
class Api::V1::AnalyticsController < ApplicationController
  def refresh_cache
    # Invalidate specific tags
    CacheService.invalidate_by_tags(['users', 'analytics'])
    
    # Invalidate by pattern
    CacheService.invalidate_by_pattern('user_stats:*')
    
    render json: { message: 'Cache refreshed successfully' }
  end
end
```

## 📊 Advanced Monitoring

### 1. Metrics Collection
```ruby
# config/monitoring.tsk
$app_name: "MyApp"
$environment: @env("RAILS_ENV", "development")

[monitoring]
enabled: @if($environment == "production", true, false)
metrics_endpoint: "/metrics"
health_endpoint: "/health"

[metrics]
# Application metrics
response_time: @metrics("response_time_ms", @request.response_time)
request_count: @metrics("requests_total", @request.count)
error_rate: @metrics("errors_total", @request.error_count)

# Database metrics
db_connections: @metrics("db_connections", @query("SELECT COUNT(*) FROM pg_stat_activity"))
db_query_time: @metrics("db_query_time_ms", @query.execution_time)

# Cache metrics
cache_hits: @metrics("cache_hits", @cache.hits)
cache_misses: @metrics("cache_misses", @cache.misses)
cache_hit_rate: @metrics("cache_hit_rate", @cache.hit_rate)

# Business metrics
active_users: @metrics("active_users", @query("SELECT COUNT(*) FROM users WHERE active = true"))
revenue_today: @metrics("revenue_today", @query("SELECT SUM(amount) FROM orders WHERE DATE(created_at) = ?", @date.today()))
```

### 2. Health Checks
```ruby
# config/health_checks.tsk
[health]
# Database health
database_healthy: @health.check("database", @query("SELECT 1"))
redis_healthy: @health.check("redis", @cache.ping)
memcached_healthy: @health.check("memcached", @cache.ping)

# External service health
api_healthy: @health.check("external_api", @http("GET", "https://api.example.com/health"))
payment_gateway_healthy: @health.check("payment_gateway", @http("GET", "https://payments.example.com/health"))

# Custom health checks
user_service_healthy: @health.check("user_service", @query("SELECT COUNT(*) FROM users") > 0)
order_service_healthy: @health.check("order_service", @query("SELECT COUNT(*) FROM orders") >= 0)

# Composite health status
overall_health: @health.composite([
    "database_healthy",
    "redis_healthy",
    "api_healthy"
])
```

### 3. Ruby Monitoring Integration
```ruby
# app/services/monitoring_service.rb
class MonitoringService
  def self.record_metric(name, value, tags = {})
    TuskLang.config.metrics_adapter.record(name, value, tags)
  end
  
  def self.record_timing(name, duration, tags = {})
    TuskLang.config.metrics_adapter.record_timing(name, duration, tags)
  end
  
  def self.increment_counter(name, tags = {})
    TuskLang.config.metrics_adapter.increment(name, tags)
  end
  
  def self.health_check(service_name)
    TuskLang.config.health_adapter.check(service_name)
  end
  
  def self.overall_health
    TuskLang.config.health_adapter.overall_status
  end
end

# app/controllers/health_controller.rb
class HealthController < ApplicationController
  def show
    health_status = MonitoringService.overall_health
    
    render json: {
      status: health_status.healthy? ? 'healthy' : 'unhealthy',
      checks: health_status.checks,
      timestamp: Time.current
    }
  end
  
  def metrics
    metrics = TuskLang.config.metrics_adapter.collect
    
    render json: {
      metrics: metrics,
      timestamp: Time.current
    }
  end
end

# app/controllers/application_controller.rb
class ApplicationController < ActionController::Base
  around_action :record_metrics
  
  private
  
  def record_metrics
    start_time = Time.current
    
    yield
    
    duration = (Time.current - start_time) * 1000 # Convert to milliseconds
    
    MonitoringService.record_timing('request_duration', duration, {
      controller: controller_name,
      action: action_name,
      status: response.status
    })
    
    MonitoringService.increment_counter('requests_total', {
      controller: controller_name,
      action: action_name,
      status: response.status
    })
  end
end
```

## ⚡ Performance Optimization

### 1. Query Optimization
```ruby
# config/optimized_queries.tsk
[analytics]
# Use indexes effectively
active_users: @query("""
    SELECT COUNT(*) 
    FROM users 
    WHERE active = true 
    AND last_login > ? 
    AND subscription_type IN ('basic', 'premium')
""", @date.subtract("30d"))

# Limit result sets
recent_orders: @query("""
    SELECT id, user_id, amount, status, created_at
    FROM orders 
    WHERE created_at > ? 
    ORDER BY created_at DESC 
    LIMIT 100
""", @date.subtract("7d"))

# Use specific columns
user_summary: @query("""
    SELECT 
        id, 
        email, 
        first_name, 
        last_name, 
        subscription_type, 
        created_at,
        last_login
    FROM users 
    WHERE active = true
""")

# Aggregated queries
daily_stats: @query("""
    SELECT 
        DATE(created_at) as date,
        COUNT(*) as new_users,
        COUNT(CASE WHEN subscription_type = 'premium' THEN 1 END) as premium_signups,
        SUM(CASE WHEN subscription_type = 'premium' THEN 1 ELSE 0 END) as premium_count
    FROM users 
    WHERE created_at > ?
    GROUP BY DATE(created_at)
    ORDER BY date DESC
""", @date.subtract("30d"))
```

### 2. Connection Pooling
```ruby
# config/connection_pool.tsk
[database]
host: "localhost"
port: 5432
name: "myapp"
user: "postgres"
password: @env("DATABASE_PASSWORD")

[pool]
# Connection pool settings
max_open_conns: @env("DB_MAX_OPEN_CONNS", 20)
max_idle_conns: @env("DB_MAX_IDLE_CONNS", 10)
conn_max_lifetime: @env("DB_CONN_MAX_LIFETIME", 30000)
conn_max_idle_time: @env("DB_CONN_MAX_IDLE_TIME", 10000)

# Read replica pool
read_replica_pool {
    max_open_conns: @env("DB_READ_MAX_OPEN_CONNS", 10)
    max_idle_conns: @env("DB_READ_MAX_IDLE_CONNS", 5)
    conn_max_lifetime: @env("DB_READ_CONN_MAX_LIFETIME", 30000)
}

# Analytics database pool
analytics_pool {
    max_open_conns: @env("DB_ANALYTICS_MAX_OPEN_CONNS", 5)
    max_idle_conns: @env("DB_ANALYTICS_MAX_IDLE_CONNS", 2)
    conn_max_lifetime: @env("DB_ANALYTICS_CONN_MAX_LIFETIME", 60000)
}
```

### 3. Ruby Performance Integration
```ruby
# app/services/performance_service.rb
class PerformanceService
  def self.optimize_queries
    # Enable query logging
    ActiveRecord::Base.logger = Rails.logger if Rails.env.development?
    
    # Set connection pool size
    ActiveRecord::Base.connection_pool.disconnect!
    ActiveRecord::Base.establish_connection(
      Rails.application.config.database_configuration[Rails.env].merge(
        pool: TuskLang.config['pool']['max_open_conns']
      )
    )
  end
  
  def self.monitor_query_performance
    queries = []
    
    ActiveSupport::Notifications.subscribe('sql.active_record') do |*args|
      event = ActiveSupport::Notifications::Event.new(*args)
      queries << {
        sql: event.payload[:sql],
        duration: event.duration,
        name: event.payload[:name]
      }
    end
    
    queries
  end
  
  def self.slow_query_alert(threshold = 1000)
    queries = monitor_query_performance
    
    slow_queries = queries.select { |q| q[:duration] > threshold }
    
    if slow_queries.any?
      Rails.logger.warn("Slow queries detected: #{slow_queries}")
      # Send alert to monitoring service
      MonitoringService.record_metric('slow_queries', slow_queries.count)
    end
  end
end
```

## 🤖 Machine Learning Integration

### 1. Predictive Configuration
```ruby
# config/ml_config.tsk
[ml]
enabled: @if($environment == "production", true, false)
model_path: "/models/config_predictor.pkl"

[predictions]
# Predict optimal cache TTL
optimal_cache_ttl: @learn("cache_ttl", "5m", {
    features: ["user_count", "request_rate", "cache_hit_rate"],
    algorithm: "random_forest"
})

# Predict database connection pool size
optimal_pool_size: @learn("pool_size", 20, {
    features: ["concurrent_users", "query_complexity", "response_time"],
    algorithm: "gradient_boosting"
})

# Predict rate limiting
optimal_rate_limit: @learn("rate_limit", 1000, {
    features: ["user_count", "api_calls", "error_rate"],
    algorithm: "linear_regression"
})

# Predict scaling decisions
should_scale: @learn("scaling_decision", false, {
    features: ["cpu_usage", "memory_usage", "response_time", "error_rate"],
    algorithm: "decision_tree"
})
```

### 2. Adaptive Configuration
```ruby
# config/adaptive.tsk
[adaptive]
# Adaptive cache TTL based on usage patterns
cache_ttl: @optimize("cache_ttl", "5m", {
    metric: "cache_hit_rate",
    target: 0.8,
    min_value: "1m",
    max_value: "1h"
})

# Adaptive connection pool size
pool_size: @optimize("pool_size", 20, {
    metric: "db_connection_wait_time",
    target: 100, # milliseconds
    min_value: 5,
    max_value: 100
})

# Adaptive rate limiting
rate_limit: @optimize("rate_limit", 1000, {
    metric: "response_time",
    target: 200, # milliseconds
    min_value: 100,
    max_value: 10000
})

# Adaptive worker count
worker_count: @optimize("worker_count", 4, {
    metric: "request_queue_length",
    target: 0,
    min_value: 1,
    max_value: 16
})
```

### 3. Ruby ML Integration
```ruby
# app/services/ml_service.rb
class MLService
  def self.predict_cache_ttl
    features = {
      user_count: User.count,
      request_rate: MonitoringService.get_metric('requests_per_second'),
      cache_hit_rate: MonitoringService.get_metric('cache_hit_rate')
    }
    
    TuskLang.config.ml_adapter.predict('cache_ttl', features)
  end
  
  def self.optimize_pool_size
    features = {
      concurrent_users: MonitoringService.get_metric('concurrent_users'),
      query_complexity: MonitoringService.get_metric('avg_query_complexity'),
      response_time: MonitoringService.get_metric('avg_response_time')
    }
    
    TuskLang.config.ml_adapter.predict('pool_size', features)
  end
  
  def self.should_scale?
    features = {
      cpu_usage: MonitoringService.get_metric('cpu_usage'),
      memory_usage: MonitoringService.get_metric('memory_usage'),
      response_time: MonitoringService.get_metric('avg_response_time'),
      error_rate: MonitoringService.get_metric('error_rate')
    }
    
    TuskLang.config.ml_adapter.predict('scaling_decision', features)
  end
  
  def self.adaptive_optimization
    # Optimize cache TTL
    optimal_ttl = predict_cache_ttl
    TuskLang.config.cache_adapter.set_ttl(optimal_ttl)
    
    # Optimize pool size
    optimal_pool_size = optimize_pool_size
    TuskLang.config.database_adapter.set_pool_size(optimal_pool_size)
    
    # Check if scaling is needed
    if should_scale?
      ScalingService.scale_up
    end
  end
end

# app/services/scaling_service.rb
class ScalingService
  def self.scale_up
    # Increase worker count
    current_workers = TuskLang.config['server']['workers']
    new_workers = [current_workers * 2, 16].min
    
    TuskLang.config['server']['workers'] = new_workers
    
    # Restart application with new configuration
    Rails.application.config.reload_configuration
    
    Rails.logger.info("Scaled up to #{new_workers} workers")
  end
  
  def self.scale_down
    # Decrease worker count
    current_workers = TuskLang.config['server']['workers']
    new_workers = [current_workers / 2, 1].max
    
    TuskLang.config['server']['workers'] = new_workers
    
    # Restart application with new configuration
    Rails.application.config.reload_configuration
    
    Rails.logger.info("Scaled down to #{new_workers} workers")
  end
end
```

## 🔒 Advanced Security

### 1. Encryption and Decryption
```ruby
# config/security.tsk
[security]
encryption_key: @env("ENCRYPTION_KEY")
encryption_algorithm: "AES-256-GCM"

[sensitive_data]
# Encrypted database credentials
db_password: @encrypt(@env("DATABASE_PASSWORD"), "AES-256-GCM")
api_key: @encrypt(@env("API_KEY"), "AES-256-GCM")
jwt_secret: @encrypt(@env("JWT_SECRET"), "AES-256-GCM")

# Encrypted configuration values
payment_gateway_key: @encrypt(@env("PAYMENT_GATEWAY_KEY"), "AES-256-GCM")
email_password: @encrypt(@env("EMAIL_PASSWORD"), "AES-256-GCM")
```

### 2. Input Validation
```ruby
# config/validation.tsk
[validation]
# User input validation
email: @validate.email(@request.email)
password: @validate.password(@request.password, {
    min_length: 8,
    require_uppercase: true,
    require_lowercase: true,
    require_numbers: true,
    require_special: true
})
age: @validate.range(@request.age, 13, 120)
website: @validate.url(@request.website)

# Custom validation rules
strong_password: @validate.custom(@request.password, "strong_password_rule")
valid_username: @validate.custom(@request.username, "username_rule")
```

### 3. Rate Limiting
```ruby
# config/rate_limiting.tsk
[rate_limiting]
enabled: @if($environment == "production", true, false)

[limits]
# Global rate limits
global_requests: @if($environment == "production", 1000, 10000)
api_requests: @if($environment == "production", 100, 1000)
login_attempts: 5
password_reset: 3

# User-specific rate limits
user_requests: @if($environment == "production", 100, 1000)
user_api_calls: @if($environment == "production", 50, 500)

# IP-based rate limits
ip_requests: @if($environment == "production", 500, 5000)
ip_login_attempts: 10

# Custom rate limiting rules
premium_user_requests: @if(@request.user.subscription_type == "premium", 200, 100)
admin_requests: @if(@request.user.role == "admin", 1000, 100)
```

## 🎯 Next Steps

Now that you understand advanced features, explore:

1. **@ Operators Guide** - Master all the powerful built-in functions
2. **Security Features** - Comprehensive security and validation
3. **Deployment Guide** - Production deployment and scaling
4. **Rails Integration** - Deep integration with Rails applications
5. **API Documentation** - Complete API reference

**Ready to build revolutionary applications? Let's Tusk! 🚀** 