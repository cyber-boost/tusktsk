# ⚡ TuskLang Ruby Advanced Caching Guide

**"We don't bow to any king" - Ruby Edition**

Master advanced caching strategies with TuskLang in Ruby. Learn distributed caching, intelligent invalidation, and high-performance cache patterns.

## 🌐 Distributed Caching

### 1. Redis Cluster Configuration
```ruby
# config/distributed_cache.tsk
[distributed_cache]
driver: "redis_cluster"
enabled: true

[redis_cluster]
nodes: [
    "redis-node-1:6379",
    "redis-node-2:6379",
    "redis-node-3:6379",
    "redis-node-4:6379",
    "redis-node-5:6379",
    "redis-node-6:6379"
]
max_connections: 20
timeout: "5s"
retry_attempts: 3

[cache_settings]
default_ttl: "1h"
namespace: "myapp"
compression: true
serialization: "json"

[analytics]
# Distributed cached queries
user_stats: @cache.distributed("user_stats:#{@date.today()}", "1h", @query("""
    SELECT 
        COUNT(*) as total,
        COUNT(CASE WHEN active = true THEN 1 END) as active,
        COUNT(CASE WHEN subscription_type = 'premium' THEN 1 END) as premium
    FROM users
"""))

order_stats: @cache.distributed("order_stats:#{@date.today()}", "30m", @query("""
    SELECT 
        COUNT(*) as total_orders,
        SUM(amount) as total_revenue,
        AVG(amount) as avg_order_value
    FROM orders 
    WHERE DATE(created_at) = ?
""", @date.today()))
```

### 2. Memcached Configuration
```ruby
# config/memcached_cache.tsk
[memcached_cache]
driver: "memcached"
enabled: true

[memcached_servers]
servers: [
    "memcached-1:11211",
    "memcached-2:11211",
    "memcached-3:11211"
]
weights: [1, 1, 1]
timeout: "5s"
connect_timeout: "2s"

[cache_settings]
default_ttl: "2h"
namespace: "myapp"
compression: true
serialization: "binary"

[analytics]
# Memcached cached queries
session_count: @cache.memcached("session_count", "5m", @query("SELECT COUNT(*) FROM sessions WHERE expires_at > NOW()"))
active_users: @cache.memcached("active_users", "10m", @query("SELECT COUNT(*) FROM users WHERE last_login > ?", @date.subtract("1h")))
```

## 🧠 Intelligent Cache Invalidation

### 1. Event-Based Invalidation
```ruby
# config/cache_invalidation.tsk
[cache_invalidation]
enabled: true
strategy: "event_based"

[invalidation_events]
user_created: ["user_count", "user_stats", "active_users"]
user_updated: ["user_profile", "user_preferences"]
user_deleted: ["user_count", "user_stats", "active_users"]

order_created: ["order_count", "revenue_stats", "order_stats"]
order_updated: ["order_details", "order_stats"]
order_cancelled: ["order_count", "revenue_stats"]

payment_processed: ["revenue_stats", "payment_stats"]
payment_failed: ["payment_stats"]

[analytics]
# Event-based cached queries
user_count: @cache("user_count", "5m", @query("SELECT COUNT(*) FROM users"), {
    invalidate_on: ["users.created", "users.deleted"],
    tags: ["users", "analytics"]
})

order_stats: @cache("order_stats", "1m", @query("SELECT COUNT(*) FROM orders"), {
    invalidate_on: ["orders.created", "orders.updated", "orders.cancelled"],
    tags: ["orders", "analytics"]
})

revenue_stats: @cache("revenue_stats", "10m", @query("SELECT SUM(amount) FROM orders WHERE status = 'completed'"), {
    invalidate_on: ["orders.created", "payments.processed"],
    tags: ["orders", "revenue", "analytics"]
})
```

### 2. Time-Based Invalidation
```ruby
# config/time_based_invalidation.tsk
[time_based_invalidation]
enabled: true

[invalidation_schedules]
user_stats: "5m"
order_stats: "1m"
revenue_stats: "10m"
session_stats: "30s"
system_stats: "1h"

[analytics]
# Time-based cached queries
recent_users: @cache("5m", @query("SELECT COUNT(*) FROM users WHERE created_at > ?", @date.subtract("1h")))
daily_orders: @cache("1h", @query("SELECT COUNT(*) FROM orders WHERE DATE(created_at) = ?", @date.today()))
monthly_revenue: @cache("6h", @query("SELECT SUM(amount) FROM orders WHERE MONTH(created_at) = ? AND YEAR(created_at) = ?", @date.month(), @date.year()))
```

### 3. Conditional Invalidation
```ruby
# config/conditional_invalidation.tsk
[conditional_invalidation]
enabled: true

[invalidation_conditions]
revenue_stats: @cache("revenue_stats", "1h", @query("SELECT SUM(amount) FROM orders"), {
    invalidate_if: @query("SELECT COUNT(*) FROM orders WHERE updated_at > ?", @cache.get("revenue_stats_last_update")),
    tags: ["orders", "revenue"]
})

user_engagement: @cache("user_engagement", "30m", @query("SELECT COUNT(*) FROM user_activities"), {
    invalidate_if: @query("SELECT COUNT(*) FROM user_activities WHERE created_at > ?", @date.subtract("30m")),
    tags: ["users", "engagement"]
})
```

## 🔄 Multi-Level Caching

### 1. L1 (Memory) + L2 (Redis) + L3 (Database)
```ruby
# config/multi_level_cache.tsk
[multi_level_cache]
enabled: true

[l1_cache]
driver: "memory"
max_size: 1000
ttl: "1m"
eviction_policy: "lru"

[l2_cache]
driver: "redis"
host: @env("REDIS_HOST", "localhost")
port: @env("REDIS_PORT", 6379)
ttl: "5m"
namespace: "myapp_l2"

[l3_cache]
driver: "database"
ttl: "1h"
table: "cache_store"

[analytics]
# Multi-level cached queries
total_users: @cache.l1("30s", @cache.l2("5m", @query("SELECT COUNT(*) FROM users")))
active_users: @cache.l1("1m", @cache.l2("10m", @query("SELECT COUNT(*) FROM users WHERE active = true")))
premium_users: @cache.l2("15m", @cache.l3("1h", @query("SELECT COUNT(*) FROM users WHERE subscription_type = 'premium'")))
```

### 2. Ruby Multi-Level Cache Implementation
```ruby
# app/services/multi_level_cache.rb
require 'tusklang'

class MultiLevelCache
  def self.l1_cache
    @l1_cache ||= TuskLang::Cache::MemoryCache.new(max_size: 1000, ttl: 60)
  end

  def self.l2_cache
    @l2_cache ||= TuskLang::Cache::RedisCache.new(
      host: ENV['REDIS_HOST'] || 'localhost',
      port: ENV['REDIS_PORT'] || 6379,
      namespace: 'myapp_l2'
    )
  end

  def self.l3_cache
    @l3_cache ||= TuskLang::Cache::DatabaseCache.new(
      table: 'cache_store',
      ttl: 3600
    )
  end

  def self.get(key)
    # Try L1 first
    value = l1_cache.get(key)
    return value if value

    # Try L2
    value = l2_cache.get(key)
    if value
      l1_cache.set(key, value)
      return value
    end

    # Try L3
    value = l3_cache.get(key)
    if value
      l2_cache.set(key, value)
      l1_cache.set(key, value)
      return value
    end

    nil
  end

  def self.set(key, value, ttl = nil)
    # Set in all levels
    l1_cache.set(key, value, ttl)
    l2_cache.set(key, value, ttl)
    l3_cache.set(key, value, ttl)
  end

  def self.invalidate(key)
    # Invalidate in all levels
    l1_cache.delete(key)
    l2_cache.delete(key)
    l3_cache.delete(key)
  end

  def self.invalidate_by_tags(tags)
    # Invalidate by tags in all levels
    l1_cache.invalidate_tags(tags)
    l2_cache.invalidate_tags(tags)
    l3_cache.invalidate_tags(tags)
  end
end
```

## 🧠 Intelligent Caching Patterns

### 1. Cache-Aside Pattern
```ruby
# config/cache_aside.tsk
[cache_aside]
enabled: true

[patterns]
user_profile: @cache.aside("user_profile:#{@request.user_id}", "1h", @query("SELECT * FROM users WHERE id = ?", @request.user_id))
order_details: @cache.aside("order_details:#{@request.order_id}", "30m", @query("SELECT * FROM orders WHERE id = ?", @request.order_id))
product_info: @cache.aside("product_info:#{@request.product_id}", "2h", @query("SELECT * FROM products WHERE id = ?", @request.product_id))
```

### 2. Write-Through Pattern
```ruby
# config/write_through.tsk
[write_through]
enabled: true

[patterns]
user_update: @cache.write_through("user_profile:#{@request.user_id}", @query("UPDATE users SET name = ? WHERE id = ?", @request.name, @request.user_id))
order_update: @cache.write_through("order_details:#{@request.order_id}", @query("UPDATE orders SET status = ? WHERE id = ?", @request.status, @request.order_id))
```

### 3. Write-Behind Pattern
```ruby
# config/write_behind.tsk
[write_behind]
enabled: true
batch_size: 100
flush_interval: "5s"

[patterns]
user_activity: @cache.write_behind("user_activity", @query("INSERT INTO user_activities (user_id, action, created_at) VALUES (?, ?, ?)", @request.user_id, @request.action, @date.now()))
analytics_event: @cache.write_behind("analytics_event", @query("INSERT INTO analytics_events (event_type, data, created_at) VALUES (?, ?, ?)", @request.event_type, @request.data, @date.now()))
```

## 🛠️ Ruby Integration Example
```ruby
# app/services/advanced_cache_service.rb
require 'tusklang'

class AdvancedCacheService
  def self.load_cache_config
    parser = TuskLang.new
    parser.parse_file('config/advanced_cache.tsk')
  end

  def self.get_cached_data(key, fallback_proc, ttl = nil)
    config = load_cache_config
    
    # Try multi-level cache
    value = MultiLevelCache.get(key)
    return value if value

    # Execute fallback and cache result
    value = fallback_proc.call
    MultiLevelCache.set(key, value, ttl)
    
    value
  end

  def self.invalidate_cache_by_event(event_type, resource_id = nil)
    config = load_cache_config
    
    if config['cache_invalidation']['enabled']
      events = config['invalidation_events'][event_type]
      
      if events
        events.each do |cache_key|
          if resource_id
            MultiLevelCache.invalidate("#{cache_key}:#{resource_id}")
          else
            MultiLevelCache.invalidate(cache_key)
          end
        end
      end
    end
  end

  def self.monitor_cache_performance
    config = load_cache_config
    
    # Monitor cache metrics
    l1_hit_rate = MultiLevelCache.l1_cache.hit_rate
    l2_hit_rate = MultiLevelCache.l2_cache.hit_rate
    l3_hit_rate = MultiLevelCache.l3_cache.hit_rate
    
    # Log performance metrics
    Rails.logger.info("Cache Performance - L1: #{l1_hit_rate}%, L2: #{l2_hit_rate}%, L3: #{l3_hit_rate}%")
    
    # Alert if performance degrades
    if l1_hit_rate < 0.8
      Rails.logger.warn("L1 cache hit rate is low: #{l1_hit_rate}%")
    end
  end
end

# Usage
config = AdvancedCacheService.load_cache_config
user_count = AdvancedCacheService.get_cached_data('user_count') { User.count }
AdvancedCacheService.invalidate_cache_by_event('user_created', 123)
AdvancedCacheService.monitor_cache_performance
```

## 🛡️ Best Practices
- Use distributed caching for high availability.
- Implement intelligent cache invalidation strategies.
- Use multi-level caching for optimal performance.
- Monitor cache hit rates and performance metrics.
- Use cache patterns appropriate for your use case.

**Ready to cache like a pro? Let's Tusk! 🚀** 