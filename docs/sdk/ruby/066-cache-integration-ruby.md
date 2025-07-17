# Cache Integration with TuskLang and Ruby

This guide covers integrating various caching systems with TuskLang and Ruby applications for improved performance and scalability.

## Table of Contents

1. [Overview](#overview)
2. [Installation](#installation)
3. [Basic Setup](#basic-setup)
4. [Cache Strategies](#cache-strategies)
5. [Cache Implementation](#cache-implementation)
6. [Advanced Features](#advanced-features)
7. [Performance Optimization](#performance-optimization)
8. [Testing](#testing)
9. [Deployment](#deployment)

## Overview

Caching is essential for improving application performance and reducing database load. This guide shows how to integrate various caching systems with TuskLang and Ruby applications.

### Key Features

- **Multiple cache backends** (Redis, Memcached, PostgreSQL)
- **Cache strategies** (write-through, write-behind, cache-aside)
- **Cache invalidation** patterns
- **Distributed caching** support
- **Cache warming** and preloading
- **Cache monitoring** and analytics

## Installation

### Dependencies

```ruby
# Gemfile
gem 'redis'
gem 'dalli'
gem 'connection_pool'
gem 'activesupport'
gem 'json'
```

### TuskLang Configuration

```tusk
# config/cache.tusk
cache:
  backend: "redis"  # redis, memcached, postgresql
  
  redis:
    url: "redis://localhost:6379/2"
    pool_size: 10
    pool_timeout: 5
    retry_attempts: 3
    retry_delay: 1
  
  memcached:
    servers: ["localhost:11211"]
    pool_size: 5
    timeout: 5
    retry_attempts: 3
  
  postgresql:
    table_name: "cache_entries"
    cleanup_interval: 3600
    max_size: 1000000
  
  strategies:
    default: "cache_aside"
    write_through: false
    write_behind: false
  
  invalidation:
    enabled: true
    patterns: ["user:*", "post:*", "comment:*"]
    batch_size: 100
  
  warming:
    enabled: true
    background_jobs: true
    preload_patterns: ["user:popular:*", "post:trending:*"]
  
  monitoring:
    enabled: true
    metrics_port: 9090
    health_check_interval: 30
```

## Basic Setup

### Cache Manager

```ruby
# app/cache/cache_manager.rb
class CacheManager
  include Singleton

  def initialize
    @config = Rails.application.config.cache
    @backend = create_backend
  end

  def get(key, options = {})
    @backend.get(key, options)
  end

  def set(key, value, options = {})
    @backend.set(key, value, options)
  end

  def delete(key)
    @backend.delete(key)
  end

  def exists?(key)
    @backend.exists?(key)
  end

  def increment(key, amount = 1)
    @backend.increment(key, amount)
  end

  def decrement(key, amount = 1)
    @backend.decrement(key, amount)
  end

  def clear(pattern = nil)
    @backend.clear(pattern)
  end

  def health_check
    @backend.health_check
  end

  private

  def create_backend
    case @config[:backend]
    when 'redis'
      RedisCacheBackend.new(@config[:redis])
    when 'memcached'
      MemcachedBackend.new(@config[:memcached])
    when 'postgresql'
      PostgreSQLCacheBackend.new(@config[:postgresql])
    else
      raise "Unsupported cache backend: #{@config[:backend]}"
    end
  end
end
```

### Base Cache Backend

```ruby
# app/cache/backends/base_cache_backend.rb
class BaseCacheBackend
  def get(key, options = {})
    raise NotImplementedError, "#{self.class} must implement get"
  end

  def set(key, value, options = {})
    raise NotImplementedError, "#{self.class} must implement set"
  end

  def delete(key)
    raise NotImplementedError, "#{self.class} must implement delete"
  end

  def exists?(key)
    raise NotImplementedError, "#{self.class} must implement exists?"
  end

  def increment(key, amount = 1)
    raise NotImplementedError, "#{self.class} must implement increment"
  end

  def decrement(key, amount = 1)
    raise NotImplementedError, "#{self.class} must implement decrement"
  end

  def clear(pattern = nil)
    raise NotImplementedError, "#{self.class} must implement clear"
  end

  def health_check
    raise NotImplementedError, "#{self.class} must implement health_check"
  end

  protected

  def serialize(value)
    case value
    when String, Numeric, TrueClass, FalseClass, NilClass
      value
    else
      value.to_json
    end
  end

  def deserialize(value)
    return value if value.is_a?(String) && !value.start_with?('{', '[')
    
    begin
      JSON.parse(value)
    rescue JSON::ParserError
      value
    end
  end

  def normalize_key(key)
    key.to_s
  end

  def normalize_ttl(ttl)
    return nil unless ttl
    ttl.is_a?(ActiveSupport::Duration) ? ttl.to_i : ttl
  end
end
```

## Cache Strategies

### Cache-Aside Strategy

```ruby
# app/cache/strategies/cache_aside_strategy.rb
class CacheAsideStrategy
  def initialize(cache_manager)
    @cache_manager = cache_manager
  end

  def fetch(key, options = {}, &block)
    # Try to get from cache first
    cached_value = @cache_manager.get(key)
    return cached_value if cached_value

    # If not in cache, execute block and cache result
    value = block.call
    @cache_manager.set(key, value, options)
    value
  end

  def write(key, value, options = {})
    @cache_manager.set(key, value, options)
  end

  def delete(key)
    @cache_manager.delete(key)
  end

  def invalidate_pattern(pattern)
    @cache_manager.clear(pattern)
  end
end
```

### Write-Through Strategy

```ruby
# app/cache/strategies/write_through_strategy.rb
class WriteThroughStrategy
  def initialize(cache_manager, data_store)
    @cache_manager = cache_manager
    @data_store = data_store
  end

  def write(key, value, options = {})
    # Write to data store first
    @data_store.write(key, value, options)
    
    # Then write to cache
    @cache_manager.set(key, value, options)
  end

  def delete(key)
    # Delete from data store first
    @data_store.delete(key)
    
    # Then delete from cache
    @cache_manager.delete(key)
  end

  def fetch(key, options = {}, &block)
    # Try cache first
    cached_value = @cache_manager.get(key)
    return cached_value if cached_value

    # If not in cache, get from data store
    value = @data_store.read(key)
    if value
      @cache_manager.set(key, value, options)
      return value
    end

    # If not in data store, execute block
    value = block.call
    write(key, value, options)
    value
  end
end
```

### Write-Behind Strategy

```ruby
# app/cache/strategies/write_behind_strategy.rb
class WriteBehindStrategy
  def initialize(cache_manager, data_store, queue_manager)
    @cache_manager = cache_manager
    @data_store = data_store
    @queue_manager = queue_manager
  end

  def write(key, value, options = {})
    # Write to cache immediately
    @cache_manager.set(key, value, options)
    
    # Queue write to data store
    @queue_manager.enqueue('cache_write_behind', {
      key: key,
      value: value,
      options: options
    })
  end

  def delete(key)
    # Delete from cache immediately
    @cache_manager.delete(key)
    
    # Queue delete from data store
    @queue_manager.enqueue('cache_write_behind', {
      action: 'delete',
      key: key
    })
  end

  def fetch(key, options = {}, &block)
    # Try cache first
    cached_value = @cache_manager.get(key)
    return cached_value if cached_value

    # If not in cache, get from data store
    value = @data_store.read(key)
    if value
      @cache_manager.set(key, value, options)
      return value
    end

    # If not in data store, execute block
    value = block.call
    write(key, value, options)
    value
  end
end
```

## Cache Implementation

### Redis Cache Backend

```ruby
# app/cache/backends/redis_cache_backend.rb
class RedisCacheBackend < BaseCacheBackend
  def initialize(config)
    @config = config
    @pool = ConnectionPool.new(
      size: config[:pool_size],
      timeout: config[:pool_timeout]
    ) do
      Redis.new(url: config[:url])
    end
  end

  def get(key, options = {})
    @pool.with do |redis|
      value = redis.get(normalize_key(key))
      deserialize(value) if value
    end
  rescue => e
    Rails.logger.error "Redis get error: #{e.message}"
    nil
  end

  def set(key, value, options = {})
    @pool.with do |redis|
      ttl = normalize_ttl(options[:ttl])
      serialized_value = serialize(value)
      
      if ttl
        redis.setex(normalize_key(key), ttl, serialized_value)
      else
        redis.set(normalize_key(key), serialized_value)
      end
    end
  rescue => e
    Rails.logger.error "Redis set error: #{e.message}"
    false
  end

  def delete(key)
    @pool.with do |redis|
      redis.del(normalize_key(key))
    end
  rescue => e
    Rails.logger.error "Redis delete error: #{e.message}"
    false
  end

  def exists?(key)
    @pool.with do |redis|
      redis.exists(normalize_key(key))
    end
  rescue => e
    Rails.logger.error "Redis exists error: #{e.message}"
    false
  end

  def increment(key, amount = 1)
    @pool.with do |redis|
      redis.incrby(normalize_key(key), amount)
    end
  rescue => e
    Rails.logger.error "Redis increment error: #{e.message}"
    nil
  end

  def decrement(key, amount = 1)
    @pool.with do |redis|
      redis.decrby(normalize_key(key), amount)
    end
  rescue => e
    Rails.logger.error "Redis decrement error: #{e.message}"
    nil
  end

  def clear(pattern = nil)
    @pool.with do |redis|
      if pattern
        keys = redis.keys(pattern)
        redis.del(*keys) if keys.any?
      else
        redis.flushdb
      end
    end
  rescue => e
    Rails.logger.error "Redis clear error: #{e.message}"
    false
  end

  def health_check
    @pool.with do |redis|
      redis.ping
      { status: 'healthy' }
    end
  rescue => e
    { status: 'error', error: e.message }
  end
end
```

### Memcached Backend

```ruby
# app/cache/backends/memcached_backend.rb
class MemcachedBackend < BaseCacheBackend
  def initialize(config)
    @config = config
    @pool = ConnectionPool.new(
      size: config[:pool_size],
      timeout: config[:timeout]
    ) do
      Dalli::Client.new(
        config[:servers],
        expires_in: 0,
        compress: true,
        compression_threshold: 1024
      )
    end
  end

  def get(key, options = {})
    @pool.with do |client|
      value = client.get(normalize_key(key))
      deserialize(value) if value
    end
  rescue => e
    Rails.logger.error "Memcached get error: #{e.message}"
    nil
  end

  def set(key, value, options = {})
    @pool.with do |client|
      ttl = normalize_ttl(options[:ttl])
      serialized_value = serialize(value)
      
      client.set(normalize_key(key), serialized_value, ttl)
    end
  rescue => e
    Rails.logger.error "Memcached set error: #{e.message}"
    false
  end

  def delete(key)
    @pool.with do |client|
      client.delete(normalize_key(key))
    end
  rescue => e
    Rails.logger.error "Memcached delete error: #{e.message}"
    false
  end

  def exists?(key)
    @pool.with do |client|
      client.get(normalize_key(key)) != nil
    end
  rescue => e
    Rails.logger.error "Memcached exists error: #{e.message}"
    false
  end

  def increment(key, amount = 1)
    @pool.with do |client|
      client.incr(normalize_key(key), amount)
    end
  rescue => e
    Rails.logger.error "Memcached increment error: #{e.message}"
    nil
  end

  def decrement(key, amount = 1)
    @pool.with do |client|
      client.decr(normalize_key(key), amount)
    end
  rescue => e
    Rails.logger.error "Memcached decrement error: #{e.message}"
    nil
  end

  def clear(pattern = nil)
    @pool.with do |client|
      client.flush
    end
  rescue => e
    Rails.logger.error "Memcached clear error: #{e.message}"
    false
  end

  def health_check
    @pool.with do |client|
      client.stats
      { status: 'healthy' }
    end
  rescue => e
    { status: 'error', error: e.message }
  end
end
```

### PostgreSQL Cache Backend

```ruby
# app/cache/backends/postgresql_cache_backend.rb
class PostgreSQLCacheBackend < BaseCacheBackend
  def initialize(config)
    @config = config
    ensure_cache_table_exists
  end

  def get(key, options = {})
    sql = "SELECT value, expires_at FROM #{@config[:table_name]} WHERE key = ?"
    result = ActiveRecord::Base.connection.execute(sql, [normalize_key(key)]).first
    
    return nil unless result
    
    # Check if expired
    if result['expires_at'] && Time.parse(result['expires_at']) < Time.current
      delete(key)
      return nil
    end
    
    deserialize(result['value'])
  rescue => e
    Rails.logger.error "PostgreSQL cache get error: #{e.message}"
    nil
  end

  def set(key, value, options = {})
    ttl = normalize_ttl(options[:ttl])
    expires_at = ttl ? Time.current + ttl.seconds : nil
    serialized_value = serialize(value)
    
    sql = <<~SQL
      INSERT INTO #{@config[:table_name]} (key, value, expires_at, created_at, updated_at)
      VALUES (?, ?, ?, ?, ?)
      ON CONFLICT (key) DO UPDATE SET
        value = EXCLUDED.value,
        expires_at = EXCLUDED.expires_at,
        updated_at = EXCLUDED.updated_at
    SQL
    
    ActiveRecord::Base.connection.execute(sql, [
      normalize_key(key),
      serialized_value,
      expires_at,
      Time.current,
      Time.current
    ])
    
    cleanup_expired_entries
    true
  rescue => e
    Rails.logger.error "PostgreSQL cache set error: #{e.message}"
    false
  end

  def delete(key)
    sql = "DELETE FROM #{@config[:table_name]} WHERE key = ?"
    ActiveRecord::Base.connection.execute(sql, [normalize_key(key)])
    true
  rescue => e
    Rails.logger.error "PostgreSQL cache delete error: #{e.message}"
    false
  end

  def exists?(key)
    sql = "SELECT 1 FROM #{@config[:table_name]} WHERE key = ? AND (expires_at IS NULL OR expires_at > ?)"
    result = ActiveRecord::Base.connection.execute(sql, [normalize_key(key), Time.current])
    result.any?
  rescue => e
    Rails.logger.error "PostgreSQL cache exists error: #{e.message}"
    false
  end

  def increment(key, amount = 1)
    current_value = get(key) || 0
    new_value = current_value + amount
    set(key, new_value)
    new_value
  rescue => e
    Rails.logger.error "PostgreSQL cache increment error: #{e.message}"
    nil
  end

  def decrement(key, amount = 1)
    current_value = get(key) || 0
    new_value = current_value - amount
    set(key, new_value)
    new_value
  rescue => e
    Rails.logger.error "PostgreSQL cache decrement error: #{e.message}"
    nil
  end

  def clear(pattern = nil)
    if pattern
      sql = "DELETE FROM #{@config[:table_name]} WHERE key LIKE ?"
      ActiveRecord::Base.connection.execute(sql, [pattern])
    else
      sql = "DELETE FROM #{@config[:table_name]}"
      ActiveRecord::Base.connection.execute(sql)
    end
    true
  rescue => e
    Rails.logger.error "PostgreSQL cache clear error: #{e.message}"
    false
  end

  def health_check
    begin
      ActiveRecord::Base.connection.execute("SELECT 1 FROM #{@config[:table_name]} LIMIT 1")
      { status: 'healthy' }
    rescue => e
      { status: 'error', error: e.message }
    end
  end

  private

  def ensure_cache_table_exists
    sql = <<~SQL
      CREATE TABLE IF NOT EXISTS #{@config[:table_name]} (
        key VARCHAR(255) PRIMARY KEY,
        value TEXT NOT NULL,
        expires_at TIMESTAMP,
        created_at TIMESTAMP NOT NULL,
        updated_at TIMESTAMP NOT NULL
      )
    SQL
    
    ActiveRecord::Base.connection.execute(sql)
    
    # Create index on expires_at for cleanup
    index_name = "#{@config[:table_name]}_expires_at_idx"
    ActiveRecord::Base.connection.execute(
      "CREATE INDEX IF NOT EXISTS #{index_name} ON #{@config[:table_name]} (expires_at)"
    )
  rescue => e
    Rails.logger.error "Failed to create cache table: #{e.message}"
  end

  def cleanup_expired_entries
    # Only cleanup occasionally to avoid performance impact
    return unless rand < 0.01 # 1% chance
    
    sql = "DELETE FROM #{@config[:table_name]} WHERE expires_at < ?"
    ActiveRecord::Base.connection.execute(sql, [Time.current])
  rescue => e
    Rails.logger.error "Cache cleanup error: #{e.message}"
  end
end
```

## Advanced Features

### Cache Invalidation

```ruby
# app/cache/invalidation/cache_invalidator.rb
class CacheInvalidator
  include Singleton

  def initialize
    @cache_manager = CacheManager.instance
    @config = Rails.application.config.cache
  end

  def invalidate_pattern(pattern)
    return unless @config[:invalidation][:enabled]
    
    Rails.logger.info "Invalidating cache pattern: #{pattern}"
    @cache_manager.clear(pattern)
    
    track_invalidation(pattern)
  end

  def invalidate_user(user_id)
    patterns = [
      "user:#{user_id}:*",
      "user_profile:#{user_id}:*",
      "user_posts:#{user_id}:*"
    ]
    
    patterns.each { |pattern| invalidate_pattern(pattern) }
  end

  def invalidate_post(post_id)
    patterns = [
      "post:#{post_id}:*",
      "post_comments:#{post_id}:*",
      "post_likes:#{post_id}:*"
    ]
    
    patterns.each { |pattern| invalidate_pattern(pattern) }
  end

  def invalidate_comment(comment_id)
    patterns = [
      "comment:#{comment_id}:*",
      "post_comments:*"
    ]
    
    patterns.each { |pattern| invalidate_pattern(pattern) }
  end

  def batch_invalidate(patterns)
    return unless @config[:invalidation][:enabled]
    
    batch_size = @config[:invalidation][:batch_size]
    patterns.each_slice(batch_size) do |batch|
      batch.each { |pattern| invalidate_pattern(pattern) }
    end
  end

  private

  def track_invalidation(pattern)
    return unless @config[:monitoring][:enabled]
    
    # Implementation would track invalidation metrics
    Rails.logger.debug "Cache invalidation: #{pattern}"
  end
end
```

### Cache Warming

```ruby
# app/cache/warming/cache_warmer.rb
class CacheWarmer
  include Singleton

  def initialize
    @cache_manager = CacheManager.instance
    @config = Rails.application.config.cache
  end

  def warm_cache
    return unless @config[:warming][:enabled]
    
    Rails.logger.info "Starting cache warming"
    
    @config[:warming][:preload_patterns].each do |pattern|
      warm_pattern(pattern)
    end
    
    Rails.logger.info "Cache warming completed"
  end

  def warm_user_cache(user_id)
    user = User.find(user_id)
    
    # Warm user profile
    CacheService.instance.get_user_profile(user)
    
    # Warm user posts
    CacheService.instance.get_user_posts(user)
    
    # Warm user statistics
    CacheService.instance.get_user_statistics(user)
  end

  def warm_post_cache(post_id)
    post = Post.find(post_id)
    
    # Warm post details
    CacheService.instance.get_post_details(post)
    
    # Warm post comments
    CacheService.instance.get_post_comments(post)
    
    # Warm post likes
    CacheService.instance.get_post_likes(post)
  end

  def warm_popular_content
    # Warm popular users
    User.popular.limit(10).each do |user|
      warm_user_cache(user.id)
    end
    
    # Warm trending posts
    Post.trending.limit(20).each do |post|
      warm_post_cache(post.id)
    end
  end

  private

  def warm_pattern(pattern)
    case pattern
    when /^user:popular:(\d+)$/
      limit = $1.to_i
      User.popular.limit(limit).each { |user| warm_user_cache(user.id) }
    when /^post:trending:(\d+)$/
      limit = $1.to_i
      Post.trending.limit(limit).each { |post| warm_post_cache(post.id) }
    else
      Rails.logger.warn "Unknown warming pattern: #{pattern}"
    end
  rescue => e
    Rails.logger.error "Cache warming error for pattern #{pattern}: #{e.message}"
  end
end
```

### Cache Service

```ruby
# app/cache/services/cache_service.rb
class CacheService
  include Singleton

  def initialize
    @cache_manager = CacheManager.instance
    @strategy = CacheAsideStrategy.new(@cache_manager)
  end

  def get_user_profile(user, options = {})
    key = "user:#{user.id}:profile"
    ttl = options[:ttl] || 1.hour
    
    @strategy.fetch(key, { ttl: ttl }) do
      {
        id: user.id,
        name: user.name,
        email: user.email,
        avatar_url: user.avatar_url,
        bio: user.bio,
        created_at: user.created_at,
        updated_at: user.updated_at
      }
    end
  end

  def get_user_posts(user, options = {})
    key = "user:#{user.id}:posts"
    ttl = options[:ttl] || 30.minutes
    
    @strategy.fetch(key, { ttl: ttl }) do
      user.posts.includes(:comments, :likes).map do |post|
        {
          id: post.id,
          title: post.title,
          content: post.content,
          category: post.category,
          created_at: post.created_at,
          comments_count: post.comments.count,
          likes_count: post.likes.count
        }
      end
    end
  end

  def get_user_statistics(user, options = {})
    key = "user:#{user.id}:statistics"
    ttl = options[:ttl] || 1.hour
    
    @strategy.fetch(key, { ttl: ttl }) do
      {
        posts_count: user.posts.count,
        comments_count: user.comments.count,
        likes_count: user.likes.count,
        followers_count: user.followers.count,
        following_count: user.following.count
      }
    end
  end

  def get_post_details(post, options = {})
    key = "post:#{post.id}:details"
    ttl = options[:ttl] || 1.hour
    
    @strategy.fetch(key, { ttl: ttl }) do
      {
        id: post.id,
        title: post.title,
        content: post.content,
        category: post.category,
        user_id: post.user_id,
        user_name: post.user.name,
        created_at: post.created_at,
        updated_at: post.updated_at
      }
    end
  end

  def get_post_comments(post, options = {})
    key = "post:#{post.id}:comments"
    ttl = options[:ttl] || 30.minutes
    
    @strategy.fetch(key, { ttl: ttl }) do
      post.comments.includes(:user).map do |comment|
        {
          id: comment.id,
          content: comment.content,
          user_id: comment.user_id,
          user_name: comment.user.name,
          created_at: comment.created_at
        }
      end
    end
  end

  def get_post_likes(post, options = {})
    key = "post:#{post.id}:likes"
    ttl = options[:ttl] || 30.minutes
    
    @strategy.fetch(key, { ttl: ttl }) do
      post.likes.includes(:user).map do |like|
        {
          id: like.id,
          user_id: like.user_id,
          user_name: like.user.name,
          created_at: like.created_at
        }
      end
    end
  end

  def increment_post_views(post_id)
    key = "post:#{post_id}:views"
    @cache_manager.increment(key)
  end

  def get_post_views(post_id)
    key = "post:#{post_id}:views"
    @cache_manager.get(key) || 0
  end

  def invalidate_user_cache(user_id)
    CacheInvalidator.instance.invalidate_user(user_id)
  end

  def invalidate_post_cache(post_id)
    CacheInvalidator.instance.invalidate_post(post_id)
  end
end
```

## Performance Optimization

### Cache Analytics

```ruby
# app/cache/analytics/cache_analytics.rb
class CacheAnalytics
  include Singleton

  def initialize
    @redis = Redis.new
    @config = Rails.application.config.cache
  end

  def track_hit(key)
    return unless @config[:monitoring][:enabled]
    
    @redis.incr("cache:hits:#{Date.current}")
    @redis.incr("cache:hits:total")
  end

  def track_miss(key)
    return unless @config[:monitoring][:enabled]
    
    @redis.incr("cache:misses:#{Date.current}")
    @redis.incr("cache:misses:total")
  end

  def track_set(key)
    return unless @config[:monitoring][:enabled]
    
    @redis.incr("cache:sets:#{Date.current}")
    @redis.incr("cache:sets:total")
  end

  def track_delete(key)
    return unless @config[:monitoring][:enabled]
    
    @redis.incr("cache:deletes:#{Date.current}")
    @redis.incr("cache:deletes:total")
  end

  def get_hit_rate(days = 7)
    end_date = Date.current
    start_date = end_date - days.days
    
    total_hits = get_total_hits(start_date, end_date)
    total_misses = get_total_misses(start_date, end_date)
    total_requests = total_hits + total_misses
    
    return 0.0 if total_requests == 0
    
    (total_hits.to_f / total_requests * 100).round(2)
  end

  def get_cache_stats(days = 7)
    end_date = Date.current
    start_date = end_date - days.days
    
    {
      hit_rate: get_hit_rate(days),
      total_hits: get_total_hits(start_date, end_date),
      total_misses: get_total_misses(start_date, end_date),
      total_sets: get_total_sets(start_date, end_date),
      total_deletes: get_total_deletes(start_date, end_date)
    }
  end

  private

  def get_total_hits(start_date, end_date)
    total = 0
    (start_date..end_date).each do |date|
      total += @redis.get("cache:hits:#{date}").to_i
    end
    total
  end

  def get_total_misses(start_date, end_date)
    total = 0
    (start_date..end_date).each do |date|
      total += @redis.get("cache:misses:#{date}").to_i
    end
    total
  end

  def get_total_sets(start_date, end_date)
    total = 0
    (start_date..end_date).each do |date|
      total += @redis.get("cache:sets:#{date}").to_i
    end
    total
  end

  def get_total_deletes(start_date, end_date)
    total = 0
    (start_date..end_date).each do |date|
      total += @redis.get("cache:deletes:#{date}").to_i
    end
    total
  end
end
```

## Testing

### Cache Test Helper

```ruby
# spec/support/cache_helper.rb
module CacheHelper
  def clear_all_caches
    CacheManager.instance.clear
  end

  def cache_key_exists?(key)
    CacheManager.instance.exists?(key)
  end

  def get_cached_value(key)
    CacheManager.instance.get(key)
  end

  def set_cached_value(key, value, options = {})
    CacheManager.instance.set(key, value, options)
  end

  def expect_cache_hit(key)
    expect(cache_key_exists?(key)).to be true
  end

  def expect_cache_miss(key)
    expect(cache_key_exists?(key)).to be false
  end
end

RSpec.configure do |config|
  config.include CacheHelper, type: :cache
  
  config.before(:each, type: :cache) do
    clear_all_caches
  end
end
```

### Cache Tests

```ruby
# spec/cache/cache_service_spec.rb
RSpec.describe CacheService, type: :cache do
  let(:service) { CacheService.instance }
  let(:user) { create(:user) }
  let(:post) { create(:post, user: user) }

  describe '#get_user_profile' do
    it 'caches user profile' do
      profile = service.get_user_profile(user)
      
      expect(profile[:id]).to eq(user.id)
      expect(profile[:name]).to eq(user.name)
      expect_cache_hit("user:#{user.id}:profile")
    end

    it 'returns cached profile on subsequent calls' do
      # First call should cache
      service.get_user_profile(user)
      
      # Second call should use cache
      expect(CacheManager.instance).not_to receive(:set)
      service.get_user_profile(user)
    end
  end

  describe '#get_user_posts' do
    it 'caches user posts' do
      posts = service.get_user_posts(user)
      
      expect(posts).to be_an(Array)
      expect_cache_hit("user:#{user.id}:posts")
    end
  end

  describe '#increment_post_views' do
    it 'increments post view count' do
      service.increment_post_views(post.id)
      
      expect(service.get_post_views(post.id)).to eq(1)
    end
  end
end
```

## Deployment

### Production Configuration

```ruby
# config/environments/production.rb
Rails.application.configure do
  # Cache configuration
  config.cache = {
    backend: ENV['CACHE_BACKEND'] || 'redis',
    redis: {
      url: ENV['REDIS_URL'] || 'redis://localhost:6379/2',
      pool_size: ENV['REDIS_POOL_SIZE'] || 10,
      pool_timeout: ENV['REDIS_POOL_TIMEOUT'] || 5,
      retry_attempts: ENV['REDIS_RETRY_ATTEMPTS'] || 3,
      retry_delay: ENV['REDIS_RETRY_DELAY'] || 1
    },
    memcached: {
      servers: ENV['MEMCACHED_SERVERS']&.split(',') || ['localhost:11211'],
      pool_size: ENV['MEMCACHED_POOL_SIZE'] || 5,
      timeout: ENV['MEMCACHED_TIMEOUT'] || 5,
      retry_attempts: ENV['MEMCACHED_RETRY_ATTEMPTS'] || 3
    },
    postgresql: {
      table_name: ENV['CACHE_TABLE_NAME'] || 'cache_entries',
      cleanup_interval: ENV['CACHE_CLEANUP_INTERVAL'] || 3600,
      max_size: ENV['CACHE_MAX_SIZE'] || 1000000
    },
    strategies: {
      default: ENV['CACHE_STRATEGY'] || 'cache_aside',
      write_through: ENV['CACHE_WRITE_THROUGH'] == 'true',
      write_behind: ENV['CACHE_WRITE_BEHIND'] == 'true'
    },
    invalidation: {
      enabled: ENV['CACHE_INVALIDATION_ENABLED'] != 'false',
      patterns: ENV['CACHE_INVALIDATION_PATTERNS']&.split(',') || ['user:*', 'post:*', 'comment:*'],
      batch_size: ENV['CACHE_INVALIDATION_BATCH_SIZE'] || 100
    },
    warming: {
      enabled: ENV['CACHE_WARMING_ENABLED'] != 'false',
      background_jobs: ENV['CACHE_WARMING_BACKGROUND_JOBS'] != 'false',
      preload_patterns: ENV['CACHE_WARMING_PATTERNS']&.split(',') || ['user:popular:10', 'post:trending:20']
    },
    monitoring: {
      enabled: ENV['CACHE_MONITORING_ENABLED'] != 'false',
      metrics_port: ENV['CACHE_METRICS_PORT'] || 9090,
      health_check_interval: ENV['CACHE_HEALTH_CHECK_INTERVAL'] || 30
    }
  }
end
```

### Docker Configuration

```dockerfile
# Dockerfile.cache
FROM ruby:3.2-alpine

RUN apk add --no-cache \
    build-base \
    redis

WORKDIR /app

COPY Gemfile Gemfile.lock ./
RUN bundle install --jobs 4 --retry 3

COPY . .

CMD ["bundle", "exec", "ruby", "app/cache/cache_runner.rb"]
```

```yaml
# docker-compose.cache.yml
version: '3.8'

services:
  cache-service:
    build:
      context: .
      dockerfile: Dockerfile.cache
    environment:
      - RAILS_ENV=production
      - REDIS_URL=redis://redis:6379/2
      - CACHE_BACKEND=redis
    depends_on:
      - redis

  redis:
    image: redis:7-alpine
    volumes:
      - redis_data:/data

volumes:
  redis_data:
```

This comprehensive cache integration guide provides everything needed to build high-performance caching systems with TuskLang and Ruby, including multiple backend support, cache strategies, invalidation patterns, warming, analytics, testing, and deployment strategies. 