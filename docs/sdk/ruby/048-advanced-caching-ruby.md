# Advanced Caching with TuskLang and Ruby

## ⚡ **Cache Your Way to Lightning Speed**

TuskLang enables sophisticated caching strategies for Ruby applications, providing multi-level caching, intelligent invalidation, and performance optimization. Build applications that respond instantly while maintaining data consistency and reducing infrastructure costs.

## 🚀 **Quick Start: Multi-Level Cache**

### Basic Cache Configuration

```ruby
# config/caching.tsk
[caching]
enabled: @env("CACHING_ENABLED", "true")
default_ttl: @env("CACHE_DEFAULT_TTL", "300")
max_memory_size: @env("CACHE_MAX_MEMORY", "100MB")

[layers]
l1_enabled: @env("L1_CACHE_ENABLED", "true") # Memory cache
l2_enabled: @env("L2_CACHE_ENABLED", "true") # Redis cache
l3_enabled: @env("L3_CACHE_ENABLED", "true") # Memcached cache

[l1_cache]
max_size: @env("L1_CACHE_MAX_SIZE", "1000")
ttl: @env("L1_CACHE_TTL", "60")

[l2_cache]
redis_url: @env.secure("REDIS_URL")
ttl: @env("L2_CACHE_TTL", "300")
compression: @env("L2_CACHE_COMPRESSION", "true")

[l3_cache]
memcached_servers: @env("MEMCACHED_SERVERS", "localhost:11211")
ttl: @env("L3_CACHE_TTL", "3600")
compression: @env("L3_CACHE_COMPRESSION", "true")

[invalidation]
strategy: @env("CACHE_INVALIDATION_STRATEGY", "time_based") # time_based, event_based, manual
batch_size: @env("CACHE_INVALIDATION_BATCH_SIZE", "100")
```

### Multi-Level Cache Implementation

```ruby
# lib/multi_level_cache.rb
require 'tusk'
require 'redis'
require 'dalli'
require 'zlib'
require 'json'

class MultiLevelCache
  def initialize(config_path = 'config/caching.tsk')
    @config = Tusk.load(config_path)
    setup_cache_layers
    @stats = CacheStats.new
  end

  def get(key, options = {})
    return nil unless @config['caching']['enabled'] == 'true'

    # Try L1 cache (memory)
    if @config['layers']['l1_enabled'] == 'true'
      value = get_from_l1(key)
      if value
        @stats.record_hit(:l1)
        return value
      end
    end

    # Try L2 cache (Redis)
    if @config['layers']['l2_enabled'] == 'true'
      value = get_from_l2(key)
      if value
        @stats.record_hit(:l2)
        set_l1(key, value, options[:ttl]) if @config['layers']['l1_enabled'] == 'true'
        return value
      end
    end

    # Try L3 cache (Memcached)
    if @config['layers']['l3_enabled'] == 'true'
      value = get_from_l3(key)
      if value
        @stats.record_hit(:l3)
        set_l2(key, value, options[:ttl]) if @config['layers']['l2_enabled'] == 'true'
        set_l1(key, value, options[:ttl]) if @config['layers']['l1_enabled'] == 'true'
        return value
      end
    end

    @stats.record_miss
    nil
  end

  def set(key, value, options = {})
    return unless @config['caching']['enabled'] == 'true'

    ttl = options[:ttl] || @config['caching']['default_ttl'].to_i
    compression = options[:compression] || false

    # Set in all enabled cache layers
    set_l1(key, value, ttl, compression) if @config['layers']['l1_enabled'] == 'true'
    set_l2(key, value, ttl, compression) if @config['layers']['l2_enabled'] == 'true'
    set_l3(key, value, ttl, compression) if @config['layers']['l3_enabled'] == 'true'

    @stats.record_set
  end

  def delete(key)
    delete_from_l1(key) if @config['layers']['l1_enabled'] == 'true'
    delete_from_l2(key) if @config['layers']['l2_enabled'] == 'true'
    delete_from_l3(key) if @config['layers']['l3_enabled'] == 'true'

    @stats.record_delete
  end

  def clear_pattern(pattern)
    clear_l1_pattern(pattern) if @config['layers']['l1_enabled'] == 'true'
    clear_l2_pattern(pattern) if @config['layers']['l2_enabled'] == 'true'
    clear_l3_pattern(pattern) if @config['layers']['l3_enabled'] == 'true'
  end

  def get_stats
    @stats.get_stats
  end

  def warm_cache(keys_and_values, options = {})
    keys_and_values.each do |key, value|
      set(key, value, options)
    end
  end

  private

  def setup_cache_layers
    setup_l1_cache
    setup_l2_cache
    setup_l3_cache
  end

  def setup_l1_cache
    @l1_cache = {}
    @l1_ttl = {}
    @l1_max_size = @config['l1_cache']['max_size'].to_i
  end

  def setup_l2_cache
    @l2_cache = Redis.new(url: @config['l2_cache']['redis_url'])
  end

  def setup_l3_cache
    servers = @config['l3_cache']['memcached_servers'].split(',')
    @l3_cache = Dalli::Client.new(servers)
  end

  def get_from_l1(key)
    return nil unless @l1_cache[key]
    return nil if @l1_ttl[key] && Time.now.to_i > @l1_ttl[key]
    @l1_cache[key]
  end

  def set_l1(key, value, ttl, compression = false)
    # Implement LRU eviction
    evict_l1_if_needed

    serialized_value = serialize_value(value, compression)
    @l1_cache[key] = serialized_value
    @l1_ttl[key] = Time.now.to_i + ttl if ttl > 0
  end

  def delete_from_l1(key)
    @l1_cache.delete(key)
    @l1_ttl.delete(key)
  end

  def clear_l1_pattern(pattern)
    @l1_cache.keys.grep(Regexp.new(pattern)).each do |key|
      delete_from_l1(key)
    end
  end

  def evict_l1_if_needed
    return if @l1_cache.size < @l1_max_size

    # Simple LRU: remove oldest entry
    oldest_key = @l1_cache.keys.first
    delete_from_l1(oldest_key) if oldest_key
  end

  def get_from_l2(key)
    value = @l2_cache.get(key)
    return nil unless value
    deserialize_value(value, @config['l2_cache']['compression'] == 'true')
  end

  def set_l2(key, value, ttl, compression = false)
    serialized_value = serialize_value(value, compression)
    if ttl > 0
      @l2_cache.setex(key, ttl, serialized_value)
    else
      @l2_cache.set(key, serialized_value)
    end
  end

  def delete_from_l2(key)
    @l2_cache.del(key)
  end

  def clear_l2_pattern(pattern)
    keys = @l2_cache.keys(pattern)
    @l2_cache.del(*keys) unless keys.empty?
  end

  def get_from_l3(key)
    value = @l3_cache.get(key)
    return nil unless value
    deserialize_value(value, @config['l3_cache']['compression'] == 'true')
  end

  def set_l3(key, value, ttl, compression = false)
    serialized_value = serialize_value(value, compression)
    @l3_cache.set(key, serialized_value, ttl)
  end

  def delete_from_l3(key)
    @l3_cache.delete(key)
  end

  def clear_l3_pattern(pattern)
    # Memcached doesn't support pattern deletion
    # This would need to be implemented with a key registry
  end

  def serialize_value(value, compression)
    json_value = value.to_json
    
    if compression
      compressed = Zlib::Deflate.deflate(json_value)
      Base64.strict_encode64(compressed)
    else
      json_value
    end
  end

  def deserialize_value(value, compression)
    if compression
      decoded = Base64.strict_decode64(value)
      decompressed = Zlib::Inflate.inflate(decoded)
      JSON.parse(decompressed)
    else
      JSON.parse(value)
    end
  rescue => e
    Rails.logger.error "Cache deserialization error: #{e.message}"
    nil
  end
end

class CacheStats
  def initialize
    @hits = { l1: 0, l2: 0, l3: 0 }
    @misses = 0
    @sets = 0
    @deletes = 0
    @mutex = Mutex.new
  end

  def record_hit(layer)
    @mutex.synchronize { @hits[layer] += 1 }
  end

  def record_miss
    @mutex.synchronize { @misses += 1 }
  end

  def record_set
    @mutex.synchronize { @sets += 1 }
  end

  def record_delete
    @mutex.synchronize { @deletes += 1 }
  end

  def get_stats
    @mutex.synchronize do
      total_requests = @hits.values.sum + @misses
      hit_rate = total_requests > 0 ? (@hits.values.sum.to_f / total_requests * 100).round(2) : 0

      {
        hits: @hits.dup,
        misses: @misses,
        sets: @sets,
        deletes: @deletes,
        total_requests: total_requests,
        hit_rate: hit_rate,
        timestamp: Time.now.iso8601
      }
    end
  end
end
```

## 🔄 **Cache Invalidation Strategies**

### Event-Based Invalidation

```ruby
# lib/cache_invalidator.rb
require 'tusk'
require 'redis'
require 'json'

class CacheInvalidator
  def initialize(config_path = 'config/caching.tsk')
    @config = Tusk.load(config_path)
    @redis = Redis.new(url: @config['l2_cache']['redis_url'])
    @cache = MultiLevelCache.new(config_path)
    setup_invalidation_strategies
  end

  def invalidate_on_event(event_type, event_data)
    case event_type
    when 'user.updated'
      invalidate_user_cache(event_data['user_id'])
    when 'post.created'
      invalidate_post_cache(event_data['post_id'])
    when 'comment.added'
      invalidate_comment_cache(event_data['comment_id'])
    when 'data.changed'
      invalidate_pattern(event_data['pattern'])
    else
      Rails.logger.warn "Unknown event type for cache invalidation: #{event_type}"
    end
  end

  def invalidate_user_cache(user_id)
    patterns = [
      "user:#{user_id}:*",
      "profile:#{user_id}:*",
      "posts:user:#{user_id}:*",
      "comments:user:#{user_id}:*"
    ]

    patterns.each { |pattern| invalidate_pattern(pattern) }
  end

  def invalidate_post_cache(post_id)
    patterns = [
      "post:#{post_id}:*",
      "posts:list:*",
      "feed:*",
      "comments:post:#{post_id}:*"
    ]

    patterns.each { |pattern| invalidate_pattern(pattern) }
  end

  def invalidate_comment_cache(comment_id)
    patterns = [
      "comment:#{comment_id}:*",
      "comments:list:*"
    ]

    patterns.each { |pattern| invalidate_pattern(pattern) }
  end

  def invalidate_pattern(pattern)
    @cache.clear_pattern(pattern)
    publish_invalidation_event(pattern)
  end

  def batch_invalidate(patterns)
    patterns.each_slice(@config['invalidation']['batch_size'].to_i) do |batch|
      batch.each { |pattern| invalidate_pattern(pattern) }
      sleep(0.1) # Small delay to prevent overwhelming the cache
    end
  end

  def schedule_invalidation(pattern, delay_seconds)
    Thread.new do
      sleep delay_seconds
      invalidate_pattern(pattern)
    end
  end

  private

  def setup_invalidation_strategies
    case @config['invalidation']['strategy']
    when 'event_based'
      setup_event_based_invalidation
    when 'time_based'
      setup_time_based_invalidation
    when 'manual'
      setup_manual_invalidation
    end
  end

  def setup_event_based_invalidation
    # Subscribe to Redis pub/sub for cache invalidation events
    Thread.new do
      @redis.subscribe('cache:invalidate') do |on|
        on.message do |channel, message|
          data = JSON.parse(message)
          invalidate_pattern(data['pattern'])
        end
      end
    end
  end

  def setup_time_based_invalidation
    # Set up periodic cache cleanup
    Thread.new do
      loop do
        cleanup_expired_entries
        sleep 3600 # Run every hour
      end
    end
  end

  def setup_manual_invalidation
    # Manual invalidation only
    Rails.logger.info "Manual cache invalidation enabled"
  end

  def publish_invalidation_event(pattern)
    @redis.publish('cache:invalidate', {
      pattern: pattern,
      timestamp: Time.now.iso8601
    }.to_json)
  end

  def cleanup_expired_entries
    # Clean up expired entries from L1 cache
    current_time = Time.now.to_i
    expired_keys = @cache.instance_variable_get(:@l1_ttl).select do |key, ttl|
      ttl && current_time > ttl
    end.keys

    expired_keys.each { |key| @cache.delete(key) }
  end
end
```

## 📊 **Cache Performance Monitoring**

### Cache Metrics Collector

```ruby
# lib/cache_metrics.rb
require 'prometheus/client'
require 'tusk'

class CacheMetrics
  def initialize(config_path = 'config/caching.tsk')
    @config = Tusk.load(config_path)
    setup_metrics
  end

  def record_cache_hit(layer)
    @cache_hits.increment(labels: { layer: layer.to_s })
  end

  def record_cache_miss
    @cache_misses.increment
  end

  def record_cache_set
    @cache_sets.increment
  end

  def record_cache_delete
    @cache_deletes.increment
  end

  def record_cache_size(layer, size)
    @cache_size.set(size, labels: { layer: layer.to_s })
  end

  def record_cache_latency(layer, duration)
    @cache_latency.observe(duration, labels: { layer: layer.to_s })
  end

  def record_cache_eviction(layer)
    @cache_evictions.increment(labels: { layer: layer.to_s })
  end

  def get_cache_stats
    {
      hit_rate: calculate_hit_rate,
      average_latency: calculate_average_latency,
      cache_sizes: get_cache_sizes,
      eviction_rate: calculate_eviction_rate
    }
  end

  private

  def setup_metrics
    @cache_hits = Prometheus::Client::Counter.new(
      :cache_hits_total,
      docstring: 'Total number of cache hits',
      labels: [:layer]
    )

    @cache_misses = Prometheus::Client::Counter.new(
      :cache_misses_total,
      docstring: 'Total number of cache misses'
    )

    @cache_sets = Prometheus::Client::Counter.new(
      :cache_sets_total,
      docstring: 'Total number of cache sets'
    )

    @cache_deletes = Prometheus::Client::Counter.new(
      :cache_deletes_total,
      docstring: 'Total number of cache deletes'
    )

    @cache_size = Prometheus::Client::Gauge.new(
      :cache_size,
      docstring: 'Current cache size',
      labels: [:layer]
    )

    @cache_latency = Prometheus::Client::Histogram.new(
      :cache_operation_duration_seconds,
      docstring: 'Cache operation duration in seconds',
      labels: [:layer]
    )

    @cache_evictions = Prometheus::Client::Counter.new(
      :cache_evictions_total,
      docstring: 'Total number of cache evictions',
      labels: [:layer]
    )
  end

  def calculate_hit_rate
    total_hits = @cache_hits.values.values.sum
    total_misses = @cache_misses.value
    total_requests = total_hits + total_misses

    return 0 if total_requests == 0
    (total_hits.to_f / total_requests * 100).round(2)
  end

  def calculate_average_latency
    # This would need to be implemented based on your metrics storage
    0.0
  end

  def get_cache_sizes
    {
      l1: @cache_size.values['l1'] || 0,
      l2: @cache_size.values['l2'] || 0,
      l3: @cache_size.values['l3'] || 0
    }
  end

  def calculate_eviction_rate
    # This would need to be implemented based on your metrics storage
    0.0
  end
end
```

## 🔄 **Cache Warming Strategies**

### Intelligent Cache Warming

```ruby
# lib/cache_warmer.rb
require 'tusk'
require 'redis'

class CacheWarmer
  def initialize(config_path = 'config/caching.tsk')
    @config = Tusk.load(config_path)
    @cache = MultiLevelCache.new(config_path)
    @redis = Redis.new(url: @config['l2_cache']['redis_url'])
  end

  def warm_popular_content
    # Warm cache with popular content
    popular_users = get_popular_users
    popular_posts = get_popular_posts
    trending_topics = get_trending_topics

    warm_user_profiles(popular_users)
    warm_post_content(popular_posts)
    warm_topic_pages(trending_topics)
  end

  def warm_user_content(user_id)
    # Warm cache for specific user
    user_profile = get_user_profile(user_id)
    user_posts = get_user_posts(user_id)
    user_followers = get_user_followers(user_id)

    @cache.set("user:#{user_id}:profile", user_profile, ttl: 3600)
    @cache.set("user:#{user_id}:posts", user_posts, ttl: 1800)
    @cache.set("user:#{user_id}:followers", user_followers, ttl: 1800)
  end

  def warm_post_content(post_id)
    # Warm cache for specific post
    post_data = get_post_data(post_id)
    post_comments = get_post_comments(post_id)
    post_author = get_post_author(post_id)

    @cache.set("post:#{post_id}:data", post_data, ttl: 3600)
    @cache.set("post:#{post_id}:comments", post_comments, ttl: 1800)
    @cache.set("post:#{post_id}:author", post_author, ttl: 3600)
  end

  def warm_feed_content(user_id)
    # Warm personalized feed for user
    feed_items = get_user_feed(user_id)
    @cache.set("feed:#{user_id}:items", feed_items, ttl: 900)
  end

  def schedule_cache_warming
    # Schedule periodic cache warming
    Thread.new do
      loop do
        warm_popular_content
        sleep 3600 # Warm every hour
      end
    end
  end

  def warm_on_demand(content_type, content_id)
    case content_type
    when 'user'
      warm_user_content(content_id)
    when 'post'
      warm_post_content(content_id)
    when 'feed'
      warm_feed_content(content_id)
    end
  end

  private

  def get_popular_users
    # Implementation to get popular users
    User.joins(:posts)
        .group('users.id')
        .order('COUNT(posts.id) DESC')
        .limit(100)
        .pluck(:id)
  end

  def get_popular_posts
    # Implementation to get popular posts
    Post.joins(:likes)
        .group('posts.id')
        .order('COUNT(likes.id) DESC')
        .limit(100)
        .pluck(:id)
  end

  def get_trending_topics
    # Implementation to get trending topics
    Topic.joins(:posts)
         .where('posts.created_at > ?', 24.hours.ago)
         .group('topics.id')
         .order('COUNT(posts.id) DESC')
         .limit(50)
         .pluck(:id)
  end

  def warm_user_profiles(user_ids)
    user_ids.each do |user_id|
      user = User.find(user_id)
      @cache.set("user:#{user_id}:profile", user.as_json, ttl: 3600)
    end
  end

  def warm_post_content(post_ids)
    post_ids.each do |post_id|
      post = Post.find(post_id)
      @cache.set("post:#{post_id}:data", post.as_json, ttl: 3600)
    end
  end

  def warm_topic_pages(topic_ids)
    topic_ids.each do |topic_id|
      topic = Topic.find(topic_id)
      @cache.set("topic:#{topic_id}:data", topic.as_json, ttl: 3600)
    end
  end

  def get_user_profile(user_id)
    User.find(user_id).as_json
  end

  def get_user_posts(user_id)
    User.find(user_id).posts.limit(20).as_json
  end

  def get_user_followers(user_id)
    User.find(user_id).followers.limit(100).as_json
  end

  def get_post_data(post_id)
    Post.find(post_id).as_json
  end

  def get_post_comments(post_id)
    Post.find(post_id).comments.limit(50).as_json
  end

  def get_post_author(post_id)
    Post.find(post_id).user.as_json
  end

  def get_user_feed(user_id)
    # Implementation to get user's personalized feed
    []
  end
end
```

## 🔄 **Cache Consistency Strategies**

### Cache Consistency Manager

```ruby
# lib/cache_consistency.rb
require 'tusk'
require 'redis'
require 'json'

class CacheConsistencyManager
  def initialize(config_path = 'config/caching.tsk')
    @config = Tusk.load(config_path)
    @redis = Redis.new(url: @config['l2_cache']['redis_url'])
    @cache = MultiLevelCache.new(config_path)
    setup_consistency_monitoring
  end

  def ensure_consistency(key, data_source)
    # Check if cache is consistent with data source
    cached_value = @cache.get(key)
    source_value = data_source.call

    if cached_value != source_value
      # Cache is inconsistent, update it
      @cache.set(key, source_value)
      log_inconsistency(key, cached_value, source_value)
    end

    source_value
  end

  def validate_cache_integrity
    # Validate cache integrity across all layers
    inconsistencies = []

    # Check L1 vs L2 consistency
    inconsistencies.concat(check_layer_consistency(:l1, :l2))
    
    # Check L2 vs L3 consistency
    inconsistencies.concat(check_layer_consistency(:l2, :l3))

    report_inconsistencies(inconsistencies)
    inconsistencies
  end

  def repair_cache_inconsistencies
    # Repair detected inconsistencies
    inconsistencies = validate_cache_integrity
    
    inconsistencies.each do |inconsistency|
      repair_inconsistency(inconsistency)
    end
  end

  def setup_cache_versioning
    # Set up cache versioning for better consistency
    @cache_version = @redis.get('cache:version').to_i || 1
  end

  def increment_cache_version
    @cache_version = @redis.incr('cache:version')
    publish_version_change(@cache_version)
  end

  private

  def setup_consistency_monitoring
    Thread.new do
      loop do
        validate_cache_integrity
        sleep 300 # Check every 5 minutes
      end
    end
  end

  def check_layer_consistency(layer1, layer2)
    inconsistencies = []
    
    # This is a simplified implementation
    # In practice, you'd need to compare actual cache contents
    
    inconsistencies
  end

  def repair_inconsistency(inconsistency)
    case inconsistency[:type]
    when 'missing_key'
      # Key exists in one layer but not another
      source_value = get_from_source_layer(inconsistency[:key], inconsistency[:source_layer])
      set_in_target_layer(inconsistency[:key], source_value, inconsistency[:target_layer])
    when 'value_mismatch'
      # Values differ between layers
      source_value = get_from_source_layer(inconsistency[:key], inconsistency[:source_layer])
      set_in_target_layer(inconsistency[:key], source_value, inconsistency[:target_layer])
    end
  end

  def get_from_source_layer(key, layer)
    case layer
    when :l1
      @cache.instance_variable_get(:@l1_cache)[key]
    when :l2
      @cache.get_from_l2(key)
    when :l3
      @cache.get_from_l3(key)
    end
  end

  def set_in_target_layer(key, value, layer)
    case layer
    when :l1
      @cache.set_l1(key, value, 3600)
    when :l2
      @cache.set_l2(key, value, 3600)
    when :l3
      @cache.set_l3(key, value, 3600)
    end
  end

  def log_inconsistency(key, cached_value, source_value)
    Rails.logger.warn "Cache inconsistency detected for key: #{key}"
    Rails.logger.warn "Cached: #{cached_value.inspect}"
    Rails.logger.warn "Source: #{source_value.inspect}"
  end

  def report_inconsistencies(inconsistencies)
    return if inconsistencies.empty?

    Rails.logger.error "Found #{inconsistencies.length} cache inconsistencies"
    inconsistencies.each do |inconsistency|
      Rails.logger.error "Inconsistency: #{inconsistency.inspect}"
    end
  end

  def publish_version_change(version)
    @redis.publish('cache:version_change', {
      version: version,
      timestamp: Time.now.iso8601
    }.to_json)
  end
end
```

## 🎯 **Configuration Management**

### Advanced Cache Configuration

```ruby
# config/advanced_caching.tsk
[caching]
enabled: @env("CACHING_ENABLED", "true")
strategy: @env("CACHING_STRATEGY", "multi_level") # multi_level, write_through, write_behind

[performance]
compression_threshold: @env("COMPRESSION_THRESHOLD", "1024") # bytes
serialization_format: @env("SERIALIZATION_FORMAT", "json") # json, msgpack, protobuf
connection_pool_size: @env("CACHE_CONNECTION_POOL_SIZE", "10")

[consistency]
validation_interval: @env("CONSISTENCY_VALIDATION_INTERVAL", "300")
repair_automatically: @env("REPAIR_AUTOMATICALLY", "true")
versioning_enabled: @env("CACHE_VERSIONING_ENABLED", "true")

[warming]
enabled: @env("CACHE_WARMING_ENABLED", "true")
schedule_interval: @env("WARMING_SCHEDULE_INTERVAL", "3600")
popular_content_threshold: @env("POPULAR_CONTENT_THRESHOLD", "100")

[monitoring]
metrics_enabled: @env("CACHE_METRICS_ENABLED", "true")
alerting_enabled: @env("CACHE_ALERTING_ENABLED", "true")
hit_rate_threshold: @env("HIT_RATE_THRESHOLD", "80")
latency_threshold: @env("LATENCY_THRESHOLD", "100") # milliseconds
```

## 🚀 **Cache Optimization Techniques**

### Cache Key Optimization

```ruby
# lib/cache_key_optimizer.rb
require 'digest'

class CacheKeyOptimizer
  def self.generate_key(*components)
    # Generate optimized cache keys
    key_parts = components.compact.map(&:to_s)
    return nil if key_parts.empty?

    # Use SHA256 for long keys to save space
    if key_parts.join(':').length > 100
      "cache:#{Digest::SHA256.hexdigest(key_parts.join(':'))}"
    else
      "cache:#{key_parts.join(':')}"
    end
  end

  def self.generate_pattern(*components)
    # Generate cache key patterns for invalidation
    key_parts = components.compact.map(&:to_s)
    return nil if key_parts.empty?

    "cache:#{key_parts.join(':')}:*"
  end

  def self.normalize_key(key)
    # Normalize cache keys for consistency
    key.to_s.downcase.gsub(/[^a-z0-9:_-]/, '_')
  end
end
```

### Cache Compression

```ruby
# lib/cache_compression.rb
require 'zlib'
require 'base64'

class CacheCompression
  def self.compress(data, threshold = 1024)
    return data if data.to_s.length < threshold

    compressed = Zlib::Deflate.deflate(data.to_json)
    {
      compressed: true,
      data: Base64.strict_encode64(compressed)
    }.to_json
  end

  def self.decompress(data)
    begin
      parsed = JSON.parse(data)
      return parsed unless parsed['compressed']

      decoded = Base64.strict_decode64(parsed['data'])
      decompressed = Zlib::Inflate.inflate(decoded)
      JSON.parse(decompressed)
    rescue => e
      Rails.logger.error "Cache decompression error: #{e.message}"
      data
    end
  end
end
```

## 🎯 **Summary**

This comprehensive guide covers advanced caching strategies with TuskLang and Ruby, including:

- **Multi-Level Caching**: L1 (memory), L2 (Redis), L3 (Memcached) cache layers
- **Cache Invalidation**: Event-based, time-based, and manual invalidation strategies
- **Performance Monitoring**: Comprehensive metrics collection and analysis
- **Cache Warming**: Intelligent pre-loading of popular content
- **Cache Consistency**: Validation and repair of cache inconsistencies
- **Configuration Management**: Advanced cache configuration options
- **Optimization Techniques**: Key optimization and compression strategies

The advanced caching strategies with TuskLang provide a robust foundation for building high-performance applications with intelligent caching, ensuring fast response times while maintaining data consistency and reducing infrastructure costs. 