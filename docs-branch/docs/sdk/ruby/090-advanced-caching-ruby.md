# 💾 Advanced Caching with TuskLang Ruby

**"We don't bow to any king" - Ruby Edition**

Build sophisticated caching systems with TuskLang's advanced caching features. From multi-level caching to intelligent cache invalidation, TuskLang provides the flexibility and power you need to optimize performance in your Ruby applications.

## 🚀 Quick Start

### Basic Caching Setup
```ruby
require 'tusklang'
require 'tusklang/caching'

# Initialize caching system
cache_system = TuskLang::Caching::System.new

# Configure caching
cache_system.configure do |config|
  config.default_strategy = 'multi_level'
  config.redis_enabled = true
  config.memory_enabled = true
  config.compression_enabled = true
end

# Register caching strategies
cache_system.register_strategy(:multi_level, TuskLang::Caching::Strategies::MultiLevelStrategy.new)
cache_system.register_strategy(:write_through, TuskLang::Caching::Strategies::WriteThroughStrategy.new)
cache_system.register_strategy(:write_behind, TuskLang::Caching::Strategies::WriteBehindStrategy.new)
```

### TuskLang Configuration
```tsk
# config/caching.tsk
[caching]
enabled: true
default_strategy: "multi_level"
redis_enabled: true
memory_enabled: true
compression_enabled: true

[caching.strategies]
multi_level: {
    enabled: true,
    levels: ["memory", "redis", "database"],
    ttl: {
        memory: "5m",
        redis: "1h",
        database: "24h"
    }
}
write_through: {
    enabled: true,
    write_to_cache: true,
    write_to_database: true,
    atomic_operations: true
}
write_behind: {
    enabled: true,
    batch_size: 100,
    flush_interval: "30s",
    max_queue_size: 1000
}

[caching.backends]
memory: {
    enabled: true,
    max_size: "100MB",
    eviction_policy: "lru",
    cleanup_interval: "5m"
}
redis: {
    enabled: true,
    host: @env("REDIS_HOST", "localhost"),
    port: @env("REDIS_PORT", 6379),
    db: @env("REDIS_DB", 0),
    password: @env("REDIS_PASSWORD"),
    pool_size: 10,
    timeout: "5s"
}
memcached: {
    enabled: false,
    servers: ["localhost:11211"],
    pool_size: 5
}

[caching.invalidation]
enabled: true
strategies: ["time_based", "event_based", "dependency_based"]
default_ttl: "1h"
max_ttl: "24h"

[caching.compression]
enabled: true
algorithm: "gzip"
min_size: "1KB"
compression_level: 6

[caching.monitoring]
enabled: true
metrics_enabled: true
hit_rate_threshold: 0.8
miss_rate_threshold: 0.2
```

## 🎯 Core Features

### 1. Multi-Level Caching Strategy
```ruby
require 'tusklang/caching'

class MultiLevelStrategy
  include TuskLang::Caching::Strategy
  
  def initialize
    @config = TuskLang.parse_file('config/caching.tsk')
    @levels = setup_cache_levels
    @monitoring = CacheMonitoring.new
  end
  
  def get(key, context = {})
    # Try each cache level
    @levels.each_with_index do |level, index|
      begin
        value = level.get(key)
        if value
          @monitoring.record_hit(level.name, key)
          return CacheResult.hit(value, level.name)
        else
          @monitoring.record_miss(level.name, key)
        end
      rescue => e
        @monitoring.record_error(level.name, key, e)
        next # Try next level
      end
    end
    
    # All levels missed
    CacheResult.miss(key)
  end
  
  def set(key, value, context = {})
    ttl = context[:ttl] || @config['caching']['invalidation']['default_ttl']
    
    # Set in all levels (write-through)
    results = @levels.map do |level|
      begin
        level_ttl = get_level_ttl(level.name, ttl)
        level.set(key, value, ttl: level_ttl)
        @monitoring.record_set(level.name, key)
        true
      rescue => e
        @monitoring.record_error(level.name, key, e)
        false
      end
    end
    
    CacheResult.set(key, results.any?)
  end
  
  def delete(key, context = {})
    # Delete from all levels
    results = @levels.map do |level|
      begin
        level.delete(key)
        @monitoring.record_delete(level.name, key)
        true
      rescue => e
        @monitoring.record_error(level.name, key, e)
        false
      end
    end
    
    CacheResult.delete(key, results.any?)
  end
  
  def invalidate(pattern, context = {})
    # Invalidate matching keys from all levels
    results = @levels.map do |level|
      begin
        level.invalidate(pattern)
        @monitoring.record_invalidation(level.name, pattern)
        true
      rescue => e
        @monitoring.record_error(level.name, pattern, e)
        false
      end
    end
    
    CacheResult.invalidate(pattern, results.any?)
  end
  
  private
  
  def setup_cache_levels
    levels_config = @config['caching']['strategies']['multi_level']['levels']
    
    levels_config.map do |level_name|
      case level_name
      when 'memory'
        MemoryCacheLevel.new(@config['caching']['backends']['memory'])
      when 'redis'
        RedisCacheLevel.new(@config['caching']['backends']['redis'])
      when 'database'
        DatabaseCacheLevel.new
      else
        raise "Unknown cache level: #{level_name}"
      end
    end
  end
  
  def get_level_ttl(level_name, base_ttl)
    level_ttls = @config['caching']['strategies']['multi_level']['ttl']
    level_ttls[level_name] || base_ttl
  end
end
```

### 2. Write-Through Caching Strategy
```ruby
require 'tusklang/caching'

class WriteThroughStrategy
  include TuskLang::Caching::Strategy
  
  def initialize
    @config = TuskLang.parse_file('config/caching.tsk')
    @write_config = @config['caching']['strategies']['write_through']
    @cache = setup_cache_backend
    @database = DatabaseConnection.new
    @monitoring = CacheMonitoring.new
  end
  
  def get(key, context = {})
    # Try cache first
    cached_value = @cache.get(key)
    
    if cached_value
      @monitoring.record_hit('write_through', key)
      return CacheResult.hit(cached_value, 'cache')
    end
    
    # Cache miss, try database
    begin
      db_value = @database.get(key)
      if db_value
        # Write to cache for next time
        @cache.set(key, db_value, ttl: context[:ttl])
        @monitoring.record_miss('write_through', key)
        return CacheResult.hit(db_value, 'database')
      end
    rescue => e
      @monitoring.record_error('write_through', key, e)
    end
    
    CacheResult.miss(key)
  end
  
  def set(key, value, context = {})
    if @write_config['atomic_operations']
      # Use database transaction for atomicity
      @database.transaction do
        # Write to database first
        @database.set(key, value)
        
        # Then write to cache
        @cache.set(key, value, ttl: context[:ttl])
      end
    else
      # Write to both independently
      @database.set(key, value)
      @cache.set(key, value, ttl: context[:ttl])
    end
    
    @monitoring.record_set('write_through', key)
    CacheResult.set(key, true)
  end
  
  def delete(key, context = {})
    if @write_config['atomic_operations']
      @database.transaction do
        @database.delete(key)
        @cache.delete(key)
      end
    else
      @database.delete(key)
      @cache.delete(key)
    end
    
    @monitoring.record_delete('write_through', key)
    CacheResult.delete(key, true)
  end
  
  private
  
  def setup_cache_backend
    if @config['caching']['redis_enabled']
      RedisCacheBackend.new(@config['caching']['backends']['redis'])
    elsif @config['caching']['memory_enabled']
      MemoryCacheBackend.new(@config['caching']['backends']['memory'])
    else
      raise "No cache backend configured"
    end
  end
end
```

### 3. Write-Behind Caching Strategy
```ruby
require 'tusklang/caching'
require 'thread'

class WriteBehindStrategy
  include TuskLang::Caching::Strategy
  
  def initialize
    @config = TuskLang.parse_file('config/caching.tsk')
    @write_config = @config['caching']['strategies']['write_behind']
    @cache = setup_cache_backend
    @database = DatabaseConnection.new
    @write_queue = Queue.new
    @monitoring = CacheMonitoring.new
    
    # Start background writer thread
    start_background_writer
  end
  
  def get(key, context = {})
    # Always read from cache
    cached_value = @cache.get(key)
    
    if cached_value
      @monitoring.record_hit('write_behind', key)
      return CacheResult.hit(cached_value, 'cache')
    end
    
    # Cache miss, try database
    begin
      db_value = @database.get(key)
      if db_value
        @cache.set(key, db_value, ttl: context[:ttl])
        @monitoring.record_miss('write_behind', key)
        return CacheResult.hit(db_value, 'database')
      end
    rescue => e
      @monitoring.record_error('write_behind', key, e)
    end
    
    CacheResult.miss(key)
  end
  
  def set(key, value, context = {})
    # Write to cache immediately
    @cache.set(key, value, ttl: context[:ttl])
    
    # Queue for background database write
    queue_write_operation(:set, key, value)
    
    @monitoring.record_set('write_behind', key)
    CacheResult.set(key, true)
  end
  
  def delete(key, context = {})
    # Delete from cache immediately
    @cache.delete(key)
    
    # Queue for background database delete
    queue_write_operation(:delete, key)
    
    @monitoring.record_delete('write_behind', key)
    CacheResult.delete(key, true)
  end
  
  def flush
    # Force flush of all pending writes
    until @write_queue.empty?
      process_write_operation(@write_queue.pop)
    end
  end
  
  private
  
  def start_background_writer
    @writer_thread = Thread.new do
      loop do
        begin
          # Process batch of writes
          batch = []
          batch_size = @write_config['batch_size']
          
          # Collect operations for batch processing
          batch_size.times do
            break if @write_queue.empty?
            batch << @write_queue.pop
          end
          
          if batch.any?
            process_write_batch(batch)
          else
            # No operations, sleep for flush interval
            sleep(parse_duration(@write_config['flush_interval']))
          end
        rescue => e
          @monitoring.record_error('write_behind', 'background_writer', e)
          sleep(1) # Brief pause on error
        end
      end
    end
  end
  
  def queue_write_operation(operation, key, value = nil)
    # Check queue size limit
    if @write_queue.size >= @write_config['max_queue_size']
      @monitoring.record_queue_full('write_behind')
      # Process some operations immediately to make room
      process_write_operation(@write_queue.pop) if @write_queue.size > 0
    end
    
    @write_queue << { operation: operation, key: key, value: value, timestamp: Time.now }
  end
  
  def process_write_batch(batch)
    @database.transaction do
      batch.each do |operation|
        process_write_operation(operation)
      end
    end
  end
  
  def process_write_operation(operation)
    case operation[:operation]
    when :set
      @database.set(operation[:key], operation[:value])
    when :delete
      @database.delete(operation[:key])
    end
    
    @monitoring.record_background_write('write_behind', operation[:operation], operation[:key])
  end
  
  def setup_cache_backend
    if @config['caching']['redis_enabled']
      RedisCacheBackend.new(@config['caching']['backends']['redis'])
    elsif @config['caching']['memory_enabled']
      MemoryCacheBackend.new(@config['caching']['backends']['memory'])
    else
      raise "No cache backend configured"
    end
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
      30 # Default 30 seconds
    end
  end
end
```

### 4. Cache Backend Implementations
```ruby
require 'tusklang/caching'
require 'redis'
require 'zlib'

class RedisCacheBackend
  def initialize(config)
    @redis = Redis.new(
      host: config['host'],
      port: config['port'],
      db: config['db'],
      password: config['password'],
      timeout: parse_duration(config['timeout'])
    )
    @pool = ConnectionPool.new(size: config['pool_size']) { @redis }
    @compression = CacheCompression.new
  end
  
  def get(key)
    @pool.with do |redis|
      data = redis.get(key)
      return nil unless data
      
      # Decompress if needed
      @compression.decompress(data)
    end
  rescue => e
    Rails.logger.error "Redis get error: #{e.message}"
    nil
  end
  
  def set(key, value, ttl: nil)
    @pool.with do |redis|
      # Compress if needed
      data = @compression.compress(value)
      
      if ttl
        redis.setex(key, parse_duration(ttl), data)
      else
        redis.set(key, data)
      end
    end
  rescue => e
    Rails.logger.error "Redis set error: #{e.message}"
    false
  end
  
  def delete(key)
    @pool.with do |redis|
      redis.del(key)
    end
  rescue => e
    Rails.logger.error "Redis delete error: #{e.message}"
    false
  end
  
  def invalidate(pattern)
    @pool.with do |redis|
      keys = redis.keys(pattern)
      redis.del(*keys) if keys.any?
    end
  rescue => e
    Rails.logger.error "Redis invalidate error: #{e.message}"
    false
  end
  
  private
  
  def parse_duration(duration_str)
    case duration_str
    when /(\d+)s/
      $1.to_i
    when /(\d+)m/
      $1.to_i * 60
    when /(\d+)h/
      $1.to_i * 3600
    else
      3600 # Default 1 hour
    end
  end
end

class MemoryCacheBackend
  def initialize(config)
    @cache = {}
    @max_size = parse_size(config['max_size'])
    @eviction_policy = config['eviction_policy']
    @cleanup_interval = parse_duration(config['cleanup_interval'])
    @access_times = {}
    @mutex = Mutex.new
    
    # Start cleanup thread
    start_cleanup_thread
  end
  
  def get(key)
    @mutex.synchronize do
      value = @cache[key]
      if value
        @access_times[key] = Time.now
        value
      else
        nil
      end
    end
  end
  
  def set(key, value, ttl: nil)
    @mutex.synchronize do
      # Check size limit
      if @cache.size >= @max_size
        evict_entries
      end
      
      @cache[key] = {
        value: value,
        ttl: ttl ? Time.now + parse_duration(ttl) : nil,
        created_at: Time.now
      }
      @access_times[key] = Time.now
    end
  end
  
  def delete(key)
    @mutex.synchronize do
      @cache.delete(key)
      @access_times.delete(key)
    end
  end
  
  def invalidate(pattern)
    @mutex.synchronize do
      keys_to_delete = @cache.keys.select { |key| key.match?(pattern) }
      keys_to_delete.each do |key|
        @cache.delete(key)
        @access_times.delete(key)
      end
    end
  end
  
  private
  
  def evict_entries
    case @eviction_policy
    when 'lru'
      # Remove least recently used
      oldest_key = @access_times.min_by { |_, time| time }&.first
      @cache.delete(oldest_key)
      @access_times.delete(oldest_key)
    when 'lfu'
      # Remove least frequently used
      # Implementation would track access frequency
    when 'random'
      # Remove random entry
      random_key = @cache.keys.sample
      @cache.delete(random_key)
      @access_times.delete(random_key)
    end
  end
  
  def start_cleanup_thread
    Thread.new do
      loop do
        sleep(@cleanup_interval)
        cleanup_expired_entries
      end
    end
  end
  
  def cleanup_expired_entries
    @mutex.synchronize do
      current_time = Time.now
      expired_keys = @cache.select do |key, entry|
        entry[:ttl] && entry[:ttl] < current_time
      end.keys
      
      expired_keys.each do |key|
        @cache.delete(key)
        @access_times.delete(key)
      end
    end
  end
  
  def parse_size(size_str)
    case size_str
    when /(\d+)MB/
      $1.to_i * 1024 * 1024
    when /(\d+)KB/
      $1.to_i * 1024
    when /(\d+)B/
      $1.to_i
    else
      100 * 1024 * 1024 # Default 100MB
    end
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
      3600 # Default 1 hour
    end
  end
end
```

## 🔧 Advanced Configuration

### Cache Compression
```ruby
require 'tusklang/caching'

class CacheCompression
  def initialize
    @config = TuskLang.parse_file('config/caching.tsk')
    @compression_config = @config['caching']['compression']
    @min_size = parse_size(@compression_config['min_size'])
  end
  
  def compress(data)
    return data unless should_compress?(data)
    
    case @compression_config['algorithm']
    when 'gzip'
      compress_gzip(data)
    when 'lz4'
      compress_lz4(data)
    else
      data
    end
  end
  
  def decompress(data)
    # Detect compression and decompress
    if data.start_with?('gzip:')
      decompress_gzip(data[5..-1])
    elsif data.start_with?('lz4:')
      decompress_lz4(data[4..-1])
    else
      data
    end
  end
  
  private
  
  def should_compress?(data)
    data.bytesize >= @min_size
  end
  
  def compress_gzip(data)
    compressed = Zlib::Deflate.deflate(data, @compression_config['compression_level'])
    "gzip:#{compressed}"
  end
  
  def decompress_gzip(data)
    Zlib::Inflate.inflate(data)
  end
  
  def compress_lz4(data)
    # LZ4 compression implementation
    # This would use the lz4-ruby gem
    "lz4:#{data}"
  end
  
  def decompress_lz4(data)
    # LZ4 decompression implementation
    data
  end
  
  def parse_size(size_str)
    case size_str
    when /(\d+)KB/
      $1.to_i * 1024
    when /(\d+)MB/
      $1.to_i * 1024 * 1024
    else
      1024 # Default 1KB
    end
  end
end
```

### Cache Invalidation Strategies
```ruby
require 'tusklang/caching'

class CacheInvalidation
  def initialize
    @config = TuskLang.parse_file('config/caching.tsk')
    @invalidation_config = @config['caching']['invalidation']
    @strategies = setup_invalidation_strategies
  end
  
  def invalidate(key, strategy = nil)
    strategy ||= @invalidation_config['strategies'].first
    
    case strategy
    when 'time_based'
      time_based_invalidation(key)
    when 'event_based'
      event_based_invalidation(key)
    when 'dependency_based'
      dependency_based_invalidation(key)
    else
      raise "Unknown invalidation strategy: #{strategy}"
    end
  end
  
  def time_based_invalidation(key)
    # Set TTL for automatic expiration
    ttl = @invalidation_config['default_ttl']
    @cache.set(key, @cache.get(key), ttl: ttl)
  end
  
  def event_based_invalidation(key)
    # Subscribe to events that should invalidate this key
    EventBus.subscribe("cache_invalidate:#{key}") do |event|
      @cache.delete(key)
    end
  end
  
  def dependency_based_invalidation(key)
    # Track dependencies and invalidate when dependencies change
    dependencies = get_key_dependencies(key)
    
    dependencies.each do |dependency|
      EventBus.subscribe("dependency_changed:#{dependency}") do |event|
        @cache.delete(key)
      end
    end
  end
  
  private
  
  def setup_invalidation_strategies
    @invalidation_config['strategies'].map do |strategy|
      case strategy
      when 'time_based'
        TimeBasedInvalidation.new
      when 'event_based'
        EventBasedInvalidation.new
      when 'dependency_based'
        DependencyBasedInvalidation.new
      end
    end
  end
  
  def get_key_dependencies(key)
    # Analyze key to determine dependencies
    # This could be based on key patterns, data analysis, etc.
    []
  end
end
```

## 📊 Performance Optimization

### Cache Warming and Preloading
```ruby
require 'tusklang/caching'

class CacheWarming
  def initialize
    @config = TuskLang.parse_file('config/caching.tsk')
    @cache = TuskLang::Caching::System.new
    @monitoring = CacheMonitoring.new
  end
  
  def warm_cache(patterns = [])
    patterns.each do |pattern|
      warm_pattern(pattern)
    end
  end
  
  def warm_pattern(pattern)
    case pattern
    when 'users:popular'
      warm_popular_users
    when 'products:featured'
      warm_featured_products
    when 'content:homepage'
      warm_homepage_content
    else
      warm_custom_pattern(pattern)
    end
  end
  
  def warm_popular_users
    # Preload popular users into cache
    popular_users = User.popular.limit(100)
    
    popular_users.each do |user|
      key = "user:#{user.id}"
      @cache.set(key, user.to_json, ttl: '1h')
    end
    
    @monitoring.record_cache_warming('users:popular', popular_users.count)
  end
  
  def warm_featured_products
    # Preload featured products
    featured_products = Product.featured.includes(:category, :images)
    
    featured_products.each do |product|
      key = "product:#{product.id}"
      @cache.set(key, product.to_json, ttl: '2h')
    end
    
    @monitoring.record_cache_warming('products:featured', featured_products.count)
  end
  
  def warm_homepage_content
    # Preload homepage content
    content = {
      featured_products: Product.featured.limit(10),
      categories: Category.active,
      banners: Banner.active,
      stats: get_homepage_stats
    }
    
    @cache.set('content:homepage', content.to_json, ttl: '30m')
    @monitoring.record_cache_warming('content:homepage', 1)
  end
  
  def warm_custom_pattern(pattern)
    # Custom pattern warming logic
    # This could be based on configuration or dynamic analysis
  end
  
  private
  
  def get_homepage_stats
    {
      total_users: User.count,
      total_products: Product.count,
      total_orders: Order.count
    }
  end
end
```

### Cache Analytics and Monitoring
```ruby
require 'tusklang/caching'

class CacheAnalytics
  def initialize
    @metrics = TuskLang::Metrics::System.new
    @cache = TuskLang::Caching::System.new
  end
  
  def get_cache_performance(time_range = '24h')
    {
      hit_rate: calculate_hit_rate(time_range),
      miss_rate: calculate_miss_rate(time_range),
      average_response_time: calculate_average_response_time(time_range),
      cache_size: get_cache_size,
      eviction_rate: calculate_eviction_rate(time_range)
    }
  end
  
  def get_cache_usage_by_pattern(time_range = '24h')
    # Analyze cache usage by key patterns
    patterns = ['user:*', 'product:*', 'order:*', 'category:*']
    
    patterns.map do |pattern|
      {
        pattern: pattern,
        hit_count: get_pattern_hit_count(pattern, time_range),
        miss_count: get_pattern_miss_count(pattern, time_range),
        hit_rate: calculate_pattern_hit_rate(pattern, time_range)
      }
    end
  end
  
  def get_cache_recommendations
    recommendations = []
    
    # Analyze hit rates
    hit_rate = calculate_hit_rate('24h')
    if hit_rate < 0.8
      recommendations << {
        type: 'low_hit_rate',
        message: "Cache hit rate is #{hit_rate * 100}%. Consider adjusting TTL or warming strategies.",
        priority: 'high'
      }
    end
    
    # Analyze memory usage
    memory_usage = get_memory_usage
    if memory_usage > 0.9
      recommendations << {
        type: 'high_memory_usage',
        message: "Cache memory usage is #{memory_usage * 100}%. Consider increasing memory or optimizing keys.",
        priority: 'medium'
      }
    end
    
    # Analyze eviction rate
    eviction_rate = calculate_eviction_rate('24h')
    if eviction_rate > 0.1
      recommendations << {
        type: 'high_eviction_rate',
        message: "High eviction rate detected. Consider increasing cache size or optimizing eviction policy.",
        priority: 'medium'
      }
    end
    
    recommendations
  end
  
  private
  
  def calculate_hit_rate(time_range)
    hits = @metrics.get_counter_value("cache_hits_total", time_range)
    misses = @metrics.get_counter_value("cache_misses_total", time_range)
    total = hits + misses
    
    total > 0 ? hits.to_f / total : 0
  end
  
  def calculate_miss_rate(time_range)
    1 - calculate_hit_rate(time_range)
  end
  
  def calculate_average_response_time(time_range)
    # Calculate average cache response time
    @metrics.get_histogram_value("cache_response_time_seconds", time_range)
  end
  
  def get_cache_size
    # Get current cache size
    @cache.get_stats[:size] || 0
  end
  
  def calculate_eviction_rate(time_range)
    evictions = @metrics.get_counter_value("cache_evictions_total", time_range)
    total_operations = @metrics.get_counter_value("cache_operations_total", time_range)
    
    total_operations > 0 ? evictions.to_f / total_operations : 0
  end
  
  def get_memory_usage
    # Get memory usage percentage
    @cache.get_stats[:memory_usage] || 0
  end
end
```

## 🔍 Monitoring and Analytics

### Cache Health Dashboard
```ruby
require 'tusklang/caching'

class CacheHealthDashboard
  def initialize
    @analytics = CacheAnalytics.new
    @monitoring = CacheMonitoring.new
  end
  
  def get_health_status
    {
      overall_status: calculate_overall_status,
      performance_metrics: get_performance_metrics,
      usage_patterns: get_usage_patterns,
      recommendations: get_recommendations,
      alerts: get_active_alerts
    }
  end
  
  def get_performance_metrics
    {
      hit_rate: @analytics.calculate_hit_rate('1h'),
      response_time: @analytics.calculate_average_response_time('1h'),
      throughput: calculate_throughput('1h'),
      error_rate: calculate_error_rate('1h')
    }
  end
  
  def get_usage_patterns
    {
      top_keys: get_top_accessed_keys('1h'),
      key_patterns: get_key_pattern_usage('1h'),
      memory_distribution: get_memory_distribution,
      eviction_patterns: get_eviction_patterns('1h')
    }
  end
  
  def get_recommendations
    @analytics.get_cache_recommendations
  end
  
  def get_active_alerts
    # Get active cache-related alerts
    [
      {
        id: 'cache_hit_rate_low',
        severity: 'warning',
        message: 'Cache hit rate below threshold',
        timestamp: Time.now.iso8601
      }
    ]
  end
  
  private
  
  def calculate_overall_status
    hit_rate = @analytics.calculate_hit_rate('1h')
    error_rate = calculate_error_rate('1h')
    
    if error_rate > 0.05
      'critical'
    elsif hit_rate < 0.7
      'warning'
    else
      'healthy'
    end
  end
  
  def calculate_throughput(time_range)
    # Calculate operations per second
    operations = @metrics.get_counter_value("cache_operations_total", time_range)
    duration = parse_duration(time_range)
    
    operations.to_f / duration
  end
  
  def calculate_error_rate(time_range)
    errors = @metrics.get_counter_value("cache_errors_total", time_range)
    total = @metrics.get_counter_value("cache_operations_total", time_range)
    
    total > 0 ? errors.to_f / total : 0
  end
  
  def get_top_accessed_keys(time_range)
    # Get most frequently accessed cache keys
    [
      { key: 'user:123', access_count: 150, hit_rate: 0.95 },
      { key: 'product:456', access_count: 120, hit_rate: 0.88 },
      { key: 'category:789', access_count: 89, hit_rate: 0.92 }
    ]
  end
  
  def get_key_pattern_usage(time_range)
    # Get usage by key patterns
    [
      { pattern: 'user:*', count: 500, hit_rate: 0.85 },
      { pattern: 'product:*', count: 300, hit_rate: 0.78 },
      { pattern: 'order:*', count: 200, hit_rate: 0.92 }
    ]
  end
  
  def get_memory_distribution
    # Get memory usage distribution
    {
      user_data: 0.4,
      product_data: 0.3,
      order_data: 0.2,
      other: 0.1
    }
  end
  
  def get_eviction_patterns(time_range)
    # Get eviction patterns
    [
      { reason: 'ttl_expired', count: 150 },
      { reason: 'memory_pressure', count: 25 },
      { reason: 'manual_invalidation', count: 10 }
    ]
  end
  
  def parse_duration(duration_str)
    case duration_str
    when /(\d+)h/
      $1.to_i * 3600
    when /(\d+)m/
      $1.to_i * 60
    when /(\d+)s/
      $1.to_i
    else
      3600 # Default 1 hour
    end
  end
end
```

## 🎯 Best Practices

### 1. Cache Key Design
- **Consistent Naming:** Use consistent, predictable key patterns
- **Namespace Separation:** Separate different data types with namespaces
- **Versioning:** Include version numbers for cache invalidation
- **Granularity:** Balance between cache efficiency and memory usage

### 2. TTL Strategy
- **Short TTL for Dynamic Data:** Use short TTL for frequently changing data
- **Long TTL for Static Data:** Use longer TTL for rarely changing data
- **Progressive TTL:** Use different TTL levels for different cache tiers
- **Adaptive TTL:** Adjust TTL based on access patterns

### 3. Cache Invalidation
- **Time-Based:** Use TTL for automatic expiration
- **Event-Based:** Invalidate on specific events or data changes
- **Dependency-Based:** Track and invalidate dependent cache entries
- **Pattern-Based:** Use wildcards for bulk invalidation

### 4. Performance Optimization
- **Compression:** Compress large cache entries
- **Serialization:** Use efficient serialization formats
- **Connection Pooling:** Pool connections to cache backends
- **Batch Operations:** Use batch operations for multiple keys

### 5. Monitoring and Alerting
- **Hit Rate Monitoring:** Track cache hit rates and alert on low performance
- **Memory Usage:** Monitor memory usage and eviction rates
- **Response Times:** Track cache response times
- **Error Rates:** Monitor cache errors and failures

## 🚀 Conclusion

TuskLang's advanced caching provides comprehensive tools for optimizing Ruby application performance. From multi-level caching to intelligent invalidation strategies, TuskLang enables developers to build high-performance applications with sophisticated caching capabilities.

The combination of multiple caching strategies, compression, monitoring, and analytics ensures that your Ruby applications can deliver optimal performance while maintaining data consistency and reliability. 