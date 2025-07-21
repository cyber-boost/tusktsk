# ⚡ TuskLang Ruby Advanced Caching Guide

**"We don't bow to any king" - Ruby Edition**

Master advanced caching strategies with TuskLang in Ruby. Learn multi-level, distributed, and intelligent caching patterns for high-performance applications.

## 🏗️ Multi-Level Caching

### 1. L1 (Memory) + L2 (Redis) + L3 (Database)
```ruby
# config/multi_level_cache.tsk
[cache]
l1 {
    driver: "memory"
    max_size: 1000
    ttl: "1m"
}
l2 {
    driver: "redis"
    host: @env("REDIS_HOST", "localhost")
    port: @env("REDIS_PORT", 6379)
    ttl: "5m"
}
l3 {
    driver: "database"
    ttl: "1h"
}

[analytics]
# Multi-level cached queries
total_users: @cache.l1("30s", @cache.l2("5m", @query("SELECT COUNT(*) FROM users")))
active_users: @cache.l1("1m", @cache.l2("10m", @query("SELECT COUNT(*) FROM users WHERE active = true")))
premium_users: @cache.l2("15m", @cache.l3("1h", @query("SELECT COUNT(*) FROM users WHERE subscription_type = 'premium'")))
```

### 2. Ruby Integration
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
      port: ENV['REDIS_PORT'] || 6379
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

    # Fallback to database
    nil
  end
end
```

## 🌐 Distributed Caching

### 1. Redis Cluster Configuration
```ruby
# config/distributed_cache.tsk
[distributed_cache]
driver: "redis_cluster"
nodes: [
    "redis-node-1:6379",
    "redis-node-2:6379",
    "redis-node-3:6379"
]
ttl: "10m"
namespace: "myapp"

[analytics]
# Distributed cached queries
user_stats: @cache.distributed("user_stats:#{@date.today()}", "1h", @query("""
    SELECT 
        COUNT(*) as total,
        COUNT(CASE WHEN active = true THEN 1 END) as active,
        COUNT(CASE WHEN subscription_type = 'premium' THEN 1 END) as premium
    FROM users
"""))
```

### 2. Memcached Configuration
```ruby
# config/memcached_cache.tsk
[memcached_cache]
driver: "memcached"
servers: [
    "memcached-1:11211",
    "memcached-2:11211",
    "memcached-3:11211"
]
ttl: "15m"
namespace: "myapp"

[analytics]
# Memcached cached queries
order_stats: @cache.memcached("order_stats", "30m", @query("""
    SELECT 
        COUNT(*) as total_orders,
        SUM(amount) as total_revenue,
        AVG(amount) as avg_order_value
    FROM orders 
    WHERE created_at > ?
""", @date.subtract("30d")))
```

## 🧠 Intelligent Caching

### 1. Adaptive TTL
```ruby
# config/intelligent_cache.tsk
[intelligent_cache]
# Adaptive TTL based on usage patterns
adaptive_ttl: @optimize("cache_ttl", "5m", {
    metric: "cache_hit_rate",
    target: 0.8,
    min_value: "1m",
    max_value: "1h"
})

[analytics]
# Intelligent cached queries
user_engagement: @cache.adaptive("user_engagement", @adaptive_ttl, @query("""
    SELECT 
        user_id,
        COUNT(*) as activity_count,
        MAX(created_at) as last_activity
    FROM user_activities 
    WHERE created_at > ?
    GROUP BY user_id
    ORDER BY activity_count DESC
    LIMIT 100
""", @date.subtract("7d")))
```

### 2. Predictive Caching
```ruby
# config/predictive_cache.tsk
[predictive_cache]
# Predict what data will be needed
predicted_queries: @learn("cache_predictions", [], {
    features: ["time_of_day", "day_of_week", "user_activity"],
    algorithm: "random_forest"
})

[analytics]
# Pre-cache predicted data
predicted_user_data: @cache.predictive("user_data", "30m", @predicted_queries)
```

## 🔄 Cache Invalidation Strategies

### 1. Time-Based Invalidation
```ruby
# config/cache_invalidation.tsk
[cache_invalidation]
# Time-based invalidation
time_based {
    user_data: "5m"
    order_data: "1m"
    analytics: "1h"
}

[analytics]
# Time-based cached queries
recent_users: @cache("5m", @query("SELECT COUNT(*) FROM users WHERE created_at > ?", @date.subtract("1h")))
daily_orders: @cache("1h", @query("SELECT COUNT(*) FROM orders WHERE DATE(created_at) = ?", @date.today()))
```

### 2. Event-Based Invalidation
```ruby
# config/event_invalidation.tsk
[cache_invalidation]
# Event-based invalidation
event_based {
    user_created: ["user_count", "user_stats"]
    order_created: ["order_count", "revenue_stats"]
    user_updated: ["user_profile", "user_preferences"]
}

[analytics]
# Event-based cached queries
user_count: @cache("5m", @query("SELECT COUNT(*) FROM users"), {
    invalidate_on: ["users.created", "users.deleted"],
    tags: ["users", "analytics"]
})
order_stats: @cache("1m", @query("SELECT COUNT(*) FROM orders"), {
    invalidate_on: ["orders.created", "orders.updated"],
    tags: ["orders", "analytics"]
})
```

## 🛠️ Ruby Integration Example
```ruby
# app/services/advanced_cache_service.rb
require 'tusklang'

class AdvancedCacheService
  def self.load_config
    parser = TuskLang.new
    parser.parse_file('config/advanced_cache.tsk')
  end

  def self.get_cached_data(key, fallback_proc)
    config = load_config
    
    # Try multi-level cache
    value = MultiLevelCache.get(key)
    return value if value

    # Execute fallback and cache result
    value = fallback_proc.call
    MultiLevelCache.l1_cache.set(key, value)
    MultiLevelCache.l2_cache.set(key, value)
    
    value
  end

  def self.invalidate_by_tags(tags)
    config = load_config
    tags.each do |tag|
      MultiLevelCache.l1_cache.invalidate_tag(tag)
      MultiLevelCache.l2_cache.invalidate_tag(tag)
    end
  end
end

# Usage
config = AdvancedCacheService.load_config
user_count = AdvancedCacheService.get_cached_data('user_count') do
  User.count
end
```

## 🛡️ Best Practices
- Use multi-level caching for optimal performance.
- Implement intelligent cache invalidation strategies.
- Monitor cache hit rates and adjust TTL accordingly.
- Use distributed caching for high availability.

**Ready to cache like a pro? Let's Tusk! 🚀** 