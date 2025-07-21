# 🚦 Advanced Rate Limiting with TuskLang Ruby

**"We don't bow to any king" - Ruby Edition**

Build sophisticated rate limiting systems with TuskLang's advanced rate limiting features. From sliding window algorithms to distributed rate limiting, TuskLang provides the flexibility and power you need to protect your Ruby applications from abuse and ensure fair resource usage.

## 🚀 Quick Start

### Basic Rate Limiting Setup
```ruby
require 'tusklang'
require 'tusklang/rate_limiting'

# Initialize rate limiting system
rate_limiter = TuskLang::RateLimiting::System.new

# Configure rate limiting
rate_limiter.configure do |config|
  config.default_strategy = 'sliding_window'
  config.storage_backend = 'redis'
  config.cache_enabled = true
  config.monitoring_enabled = true
end

# Register rate limiting strategies
rate_limiter.register_strategy(:sliding_window, TuskLang::RateLimiting::Strategies::SlidingWindowStrategy.new)
rate_limiter.register_strategy(:token_bucket, TuskLang::RateLimiting::Strategies::TokenBucketStrategy.new)
rate_limiter.register_strategy(:leaky_bucket, TuskLang::RateLimiting::Strategies::LeakyBucketStrategy.new)
```

### TuskLang Configuration
```tsk
# config/rate_limiting.tsk
[rate_limiting]
enabled: true
default_strategy: "sliding_window"
storage_backend: "redis"
cache_enabled: true
monitoring_enabled: true

[rate_limiting.global]
requests_per_minute: 1000
requests_per_hour: 10000
requests_per_day: 100000

[rate_limiting.endpoints]
api_v1_users: {
    requests_per_minute: 60,
    requests_per_hour: 1000,
    burst_limit: 10,
    strategy: "sliding_window"
}
api_v1_orders: {
    requests_per_minute: 30,
    requests_per_hour: 500,
    burst_limit: 5,
    strategy: "token_bucket"
}
api_v1_payments: {
    requests_per_minute: 10,
    requests_per_hour: 100,
    burst_limit: 2,
    strategy: "leaky_bucket"
}

[rate_limiting.user_tiers]
free: {
    requests_per_minute: 10,
    requests_per_hour: 100,
    requests_per_day: 1000
}
premium: {
    requests_per_minute: 100,
    requests_per_hour: 1000,
    requests_per_day: 10000
}
enterprise: {
    requests_per_minute: 1000,
    requests_per_hour: 10000,
    requests_per_day: 100000
}

[rate_limiting.storage]
redis: {
    host: @env("REDIS_HOST", "localhost"),
    port: @env("REDIS_PORT", 6379),
    db: @env("REDIS_DB", 0),
    password: @env("REDIS_PASSWORD"),
    pool_size: 10
}
memory: {
    max_entries: 10000,
    cleanup_interval: "5m"
}
```

## 🎯 Core Features

### 1. Sliding Window Rate Limiting
```ruby
require 'tusklang/rate_limiting'

class SlidingWindowStrategy
  include TuskLang::RateLimiting::Strategy
  
  def initialize
    @config = TuskLang.parse_file('config/rate_limiting.tsk')
    @storage = RateLimitingStorage.new
  end
  
  def allow?(identifier, limits, context = {})
    window_size = limits[:window_size] || 60 # seconds
    max_requests = limits[:max_requests]
    
    # Get current window boundaries
    current_time = Time.now.to_i
    window_start = current_time - window_size
    
    # Get requests in current window
    requests = get_requests_in_window(identifier, window_start, current_time)
    
    # Check if limit is exceeded
    if requests.count >= max_requests
      return RateLimitResult.denied(
        "Rate limit exceeded",
        {
          limit: max_requests,
          remaining: 0,
          reset_time: window_start + window_size,
          retry_after: window_start + window_size - current_time
        }
      )
    end
    
    # Record this request
    record_request(identifier, current_time, context)
    
    RateLimitResult.allowed(
      "Request allowed",
      {
        limit: max_requests,
        remaining: max_requests - requests.count - 1,
        reset_time: window_start + window_size,
        retry_after: window_start + window_size - current_time
      }
    )
  end
  
  def get_requests_in_window(identifier, window_start, window_end)
    key = generate_key(identifier, window_start, window_end)
    @storage.get_requests(key)
  end
  
  def record_request(identifier, timestamp, context)
    key = generate_key(identifier, timestamp)
    @storage.record_request(key, timestamp, context)
  end
  
  private
  
  def generate_key(identifier, *components)
    "rate_limit:#{identifier}:#{components.join(':')}"
  end
end
```

### 2. Token Bucket Rate Limiting
```ruby
require 'tusklang/rate_limiting'

class TokenBucketStrategy
  include TuskLang::RateLimiting::Strategy
  
  def initialize
    @config = TuskLang.parse_file('config/rate_limiting.tsk')
    @storage = RateLimitingStorage.new
  end
  
  def allow?(identifier, limits, context = {})
    capacity = limits[:capacity] || 100
    refill_rate = limits[:refill_rate] || 10 # tokens per second
    refill_time = limits[:refill_time] || 1 # seconds
    
    # Get current bucket state
    bucket = get_bucket_state(identifier)
    current_time = Time.now.to_f
    
    # Calculate tokens to add
    time_passed = current_time - bucket[:last_refill]
    tokens_to_add = (time_passed * refill_rate).floor
    
    # Update bucket
    bucket[:tokens] = [capacity, bucket[:tokens] + tokens_to_add].min
    bucket[:last_refill] = current_time
    
    # Check if we have enough tokens
    if bucket[:tokens] >= 1
      bucket[:tokens] -= 1
      save_bucket_state(identifier, bucket)
      
      RateLimitResult.allowed(
        "Request allowed",
        {
          limit: capacity,
          remaining: bucket[:tokens],
          reset_time: current_time + (capacity - bucket[:tokens]) / refill_rate,
          retry_after: 0
        }
      )
    else
      # Calculate when next token will be available
      tokens_needed = 1 - bucket[:tokens]
      wait_time = tokens_needed / refill_rate
      
      RateLimitResult.denied(
        "Rate limit exceeded",
        {
          limit: capacity,
          remaining: bucket[:tokens],
          reset_time: current_time + wait_time,
          retry_after: wait_time
        }
      )
    end
  end
  
  def get_bucket_state(identifier)
    key = generate_key(identifier)
    state = @storage.get_bucket_state(key)
    
    state || {
      tokens: 100, # Default capacity
      last_refill: Time.now.to_f
    }
  end
  
  def save_bucket_state(identifier, bucket)
    key = generate_key(identifier)
    @storage.save_bucket_state(key, bucket)
  end
  
  private
  
  def generate_key(identifier)
    "token_bucket:#{identifier}"
  end
end
```

### 3. Leaky Bucket Rate Limiting
```ruby
require 'tusklang/rate_limiting'

class LeakyBucketStrategy
  include TuskLang::RateLimiting::Strategy
  
  def initialize
    @config = TuskLang.parse_file('config/rate_limiting.tsk')
    @storage = RateLimitingStorage.new
  end
  
  def allow?(identifier, limits, context = {})
    capacity = limits[:capacity] || 100
    leak_rate = limits[:leak_rate] || 10 # requests per second
    
    # Get current bucket state
    bucket = get_bucket_state(identifier)
    current_time = Time.now.to_f
    
    # Calculate leaked requests
    time_passed = current_time - bucket[:last_leak]
    leaked_requests = (time_passed * leak_rate).floor
    
    # Update bucket
    bucket[:requests] = [0, bucket[:requests] - leaked_requests].max
    bucket[:last_leak] = current_time
    
    # Check if bucket has capacity
    if bucket[:requests] < capacity
      bucket[:requests] += 1
      save_bucket_state(identifier, bucket)
      
      RateLimitResult.allowed(
        "Request allowed",
        {
          limit: capacity,
          remaining: capacity - bucket[:requests],
          reset_time: current_time + bucket[:requests] / leak_rate,
          retry_after: 0
        }
      )
    else
      # Calculate when bucket will have space
      wait_time = (bucket[:requests] - capacity + 1) / leak_rate
      
      RateLimitResult.denied(
        "Rate limit exceeded",
        {
          limit: capacity,
          remaining: 0,
          reset_time: current_time + wait_time,
          retry_after: wait_time
        }
      )
    end
  end
  
  def get_bucket_state(identifier)
    key = generate_key(identifier)
    state = @storage.get_bucket_state(key)
    
    state || {
      requests: 0,
      last_leak: Time.now.to_f
    }
  end
  
  def save_bucket_state(identifier, bucket)
    key = generate_key(identifier)
    @storage.save_bucket_state(key, bucket)
  end
  
  private
  
  def generate_key(identifier)
    "leaky_bucket:#{identifier}"
  end
end
```

### 4. Rate Limiting Middleware
```ruby
require 'tusklang/rate_limiting'

class RateLimitingMiddleware
  def initialize(app)
    @app = app
    @rate_limiter = TuskLang::RateLimiting::System.new
    @config = TuskLang.parse_file('config/rate_limiting.tsk')
  end
  
  def call(env)
    request = Rack::Request.new(env)
    
    # Skip rate limiting for certain paths
    return @app.call(env) if skip_rate_limiting?(request.path)
    
    # Get rate limiting configuration
    limits = get_rate_limits(request)
    return @app.call(env) unless limits
    
    # Get identifier for rate limiting
    identifier = get_identifier(request)
    
    # Check rate limit
    result = @rate_limiter.allow?(identifier, limits, request_context(request))
    
    if result.allowed?
      # Add rate limit headers
      response = @app.call(env)
      add_rate_limit_headers(response, result)
      response
    else
      # Return rate limit exceeded response
      rate_limit_exceeded_response(result)
    end
  end
  
  private
  
  def skip_rate_limiting?(path)
    skip_paths = @config['rate_limiting']['skip_paths'] || []
    skip_paths.any? { |skip_path| path.start_with?(skip_path) }
  end
  
  def get_rate_limits(request)
    # Get endpoint-specific limits
    endpoint_limits = get_endpoint_limits(request.path)
    return endpoint_limits if endpoint_limits
    
    # Get user tier limits
    user_tier_limits = get_user_tier_limits(request)
    return user_tier_limits if user_tier_limits
    
    # Return global limits
    @config['rate_limiting']['global']
  end
  
  def get_endpoint_limits(path)
    endpoints = @config['rate_limiting']['endpoints']
    
    endpoints.each do |endpoint, limits|
      if path.start_with?("/api/#{endpoint}")
        return limits
      end
    end
    
    nil
  end
  
  def get_user_tier_limits(request)
    user = extract_user(request)
    return nil unless user
    
    user_tiers = @config['rate_limiting']['user_tiers']
    user_tiers[user.tier]
  end
  
  def get_identifier(request)
    user = extract_user(request)
    
    if user
      "user:#{user.id}"
    else
      "ip:#{request.ip}"
    end
  end
  
  def extract_user(request)
    # Extract user from request (JWT token, session, etc.)
    token = request.env['HTTP_AUTHORIZATION']&.gsub('Bearer ', '')
    return nil unless token
    
    begin
      decoded = JWT.decode(token, ENV['JWT_SECRET'], true, { algorithm: 'HS256' })
      user_id = decoded[0]['user_id']
      User.find(user_id)
    rescue JWT::DecodeError, ActiveRecord::RecordNotFound
      nil
    end
  end
  
  def request_context(request)
    {
      ip: request.ip,
      user_agent: request.user_agent,
      method: request.method,
      path: request.path
    }
  end
  
  def add_rate_limit_headers(response, result)
    status, headers, body = response
    
    headers.merge!({
      'X-RateLimit-Limit' => result.metadata[:limit].to_s,
      'X-RateLimit-Remaining' => result.metadata[:remaining].to_s,
      'X-RateLimit-Reset' => result.metadata[:reset_time].to_s,
      'X-RateLimit-RetryAfter' => result.metadata[:retry_after].to_s
    })
    
    [status, headers, body]
  end
  
  def rate_limit_exceeded_response(result)
    [
      429,
      {
        'Content-Type' => 'application/json',
        'X-RateLimit-Limit' => result.metadata[:limit].to_s,
        'X-RateLimit-Remaining' => result.metadata[:remaining].to_s,
        'X-RateLimit-Reset' => result.metadata[:reset_time].to_s,
        'X-RateLimit-RetryAfter' => result.metadata[:retry_after].to_s,
        'Retry-After' => result.metadata[:retry_after].to_s
      },
      [{
        error: 'Rate limit exceeded',
        message: result.reason,
        retry_after: result.metadata[:retry_after]
      }.to_json]
    ]
  end
end
```

### 5. Distributed Rate Limiting
```ruby
require 'tusklang/rate_limiting'

class DistributedRateLimiter
  def initialize
    @config = TuskLang.parse_file('config/rate_limiting.tsk')
    @storage = DistributedRateLimitingStorage.new
    @node_id = SecureRandom.uuid
  end
  
  def allow?(identifier, limits, context = {})
    # Use distributed storage for rate limiting
    key = generate_key(identifier)
    
    case limits[:strategy]
    when 'sliding_window'
      distributed_sliding_window(key, limits, context)
    when 'token_bucket'
      distributed_token_bucket(key, limits, context)
    when 'leaky_bucket'
      distributed_leaky_bucket(key, limits, context)
    else
      distributed_sliding_window(key, limits, context)
    end
  end
  
  private
  
  def distributed_sliding_window(key, limits, context)
    window_size = limits[:window_size] || 60
    max_requests = limits[:max_requests]
    
    current_time = Time.now.to_i
    window_start = current_time - window_size
    
    # Use Redis for distributed storage
    pipeline = @storage.redis.pipelined do |pipe|
      pipe.zremrangebyscore(key, 0, window_start)
      pipe.zadd(key, current_time, "#{current_time}:#{@node_id}")
      pipe.zcard(key)
      pipe.expire(key, window_size * 2)
    end
    
    request_count = pipeline[2]
    
    if request_count >= max_requests
      RateLimitResult.denied(
        "Rate limit exceeded",
        {
          limit: max_requests,
          remaining: 0,
          reset_time: window_start + window_size,
          retry_after: window_start + window_size - current_time
        }
      )
    else
      RateLimitResult.allowed(
        "Request allowed",
        {
          limit: max_requests,
          remaining: max_requests - request_count,
          reset_time: window_start + window_size,
          retry_after: window_start + window_size - current_time
        }
      )
    end
  end
  
  def distributed_token_bucket(key, limits, context)
    capacity = limits[:capacity] || 100
    refill_rate = limits[:refill_rate] || 10
    
    current_time = Time.now.to_f
    
    # Use Lua script for atomic operations
    lua_script = <<~LUA
      local key = KEYS[1]
      local capacity = tonumber(ARGV[1])
      local refill_rate = tonumber(ARGV[2])
      local current_time = tonumber(ARGV[3])
      local node_id = ARGV[4]
      
      local bucket = redis.call('HMGET', key, 'tokens', 'last_refill')
      local tokens = tonumber(bucket[1]) or capacity
      local last_refill = tonumber(bucket[2]) or current_time
      
      local time_passed = current_time - last_refill
      local tokens_to_add = math.floor(time_passed * refill_rate)
      
      tokens = math.min(capacity, tokens + tokens_to_add)
      last_refill = current_time
      
      if tokens >= 1 then
        tokens = tokens - 1
        redis.call('HMSET', key, 'tokens', tokens, 'last_refill', last_refill)
        redis.call('EXPIRE', key, 3600)
        return {1, tokens}
      else
        return {0, tokens}
      end
    LUA
    
    result = @storage.redis.eval(lua_script, [key], [capacity, refill_rate, current_time, @node_id])
    allowed = result[0] == 1
    remaining_tokens = result[1]
    
    if allowed
      RateLimitResult.allowed(
        "Request allowed",
        {
          limit: capacity,
          remaining: remaining_tokens,
          reset_time: current_time + (capacity - remaining_tokens) / refill_rate,
          retry_after: 0
        }
      )
    else
      wait_time = (1 - remaining_tokens) / refill_rate
      RateLimitResult.denied(
        "Rate limit exceeded",
        {
          limit: capacity,
          remaining: remaining_tokens,
          reset_time: current_time + wait_time,
          retry_after: wait_time
        }
      )
    end
  end
  
  def distributed_leaky_bucket(key, limits, context)
    capacity = limits[:capacity] || 100
    leak_rate = limits[:leak_rate] || 10
    
    current_time = Time.now.to_f
    
    # Use Lua script for atomic operations
    lua_script = <<~LUA
      local key = KEYS[1]
      local capacity = tonumber(ARGV[1])
      local leak_rate = tonumber(ARGV[2])
      local current_time = tonumber(ARGV[3])
      local node_id = ARGV[4]
      
      local bucket = redis.call('HMGET', key, 'requests', 'last_leak')
      local requests = tonumber(bucket[1]) or 0
      local last_leak = tonumber(bucket[2]) or current_time
      
      local time_passed = current_time - last_leak
      local leaked_requests = math.floor(time_passed * leak_rate)
      
      requests = math.max(0, requests - leaked_requests)
      last_leak = current_time
      
      if requests < capacity then
        requests = requests + 1
        redis.call('HMSET', key, 'requests', requests, 'last_leak', last_leak)
        redis.call('EXPIRE', key, 3600)
        return {1, requests}
      else
        return {0, requests}
      end
    LUA
    
    result = @storage.redis.eval(lua_script, [key], [capacity, leak_rate, current_time, @node_id])
    allowed = result[0] == 1
    current_requests = result[1]
    
    if allowed
      RateLimitResult.allowed(
        "Request allowed",
        {
          limit: capacity,
          remaining: capacity - current_requests,
          reset_time: current_time + current_requests / leak_rate,
          retry_after: 0
        }
      )
    else
      wait_time = (current_requests - capacity + 1) / leak_rate
      RateLimitResult.denied(
        "Rate limit exceeded",
        {
          limit: capacity,
          remaining: 0,
          reset_time: current_time + wait_time,
          retry_after: wait_time
        }
      )
    end
  end
  
  def generate_key(identifier)
    "distributed_rate_limit:#{identifier}"
  end
end
```

### 6. Rate Limiting Decorators
```ruby
require 'tusklang/rate_limiting'

module RateLimitingDecorators
  def self.included(base)
    base.extend(ClassMethods)
  end
  
  module ClassMethods
    def rate_limit(limits, options = {})
      before_action :check_rate_limit, only: options[:only], except: options[:except]
      
      define_method :rate_limit_config do
        limits
      end
    end
  end
  
  private
  
  def check_rate_limit
    limits = rate_limit_config
    identifier = rate_limit_identifier
    
    result = TuskLang::RateLimiting::System.new.allow?(
      identifier,
      limits,
      request_context
    )
    
    unless result.allowed?
      render json: {
        error: 'Rate limit exceeded',
        message: result.reason,
        retry_after: result.metadata[:retry_after]
      }, status: :too_many_requests
    end
  end
  
  def rate_limit_identifier
    if current_user
      "user:#{current_user.id}"
    else
      "ip:#{request.remote_ip}"
    end
  end
  
  def request_context
    {
      ip: request.remote_ip,
      user_agent: request.user_agent,
      method: request.method,
      path: request.path
    }
  end
end

# Usage in controllers
class Api::V1::UsersController < ApplicationController
  include RateLimitingDecorators
  
  rate_limit(
    { requests_per_minute: 60, requests_per_hour: 1000 },
    only: [:index, :show]
  )
  
  rate_limit(
    { requests_per_minute: 10, requests_per_hour: 100 },
    only: [:create, :update, :destroy]
  )
  
  def index
    @users = User.all
    render json: @users
  end
  
  def show
    @user = User.find(params[:id])
    render json: @user
  end
  
  def create
    @user = User.create!(user_params)
    render json: @user, status: :created
  end
  
  def update
    @user = User.find(params[:id])
    @user.update!(user_params)
    render json: @user
  end
  
  def destroy
    @user = User.find(params[:id])
    @user.destroy
    head :no_content
  end
  
  private
  
  def user_params
    params.require(:user).permit(:name, :email, :role)
  end
end
```

## 🔧 Advanced Configuration

### Rate Limiting Storage
```ruby
require 'tusklang/rate_limiting'

class RateLimitingStorage
  def initialize
    @config = TuskLang.parse_file('config/rate_limiting.tsk')
    setup_storage
  end
  
  def get_requests(key)
    case @config['rate_limiting']['storage_backend']
    when 'redis'
      get_requests_from_redis(key)
    when 'memory'
      get_requests_from_memory(key)
    else
      []
    end
  end
  
  def record_request(key, timestamp, context)
    case @config['rate_limiting']['storage_backend']
    when 'redis'
      record_request_to_redis(key, timestamp, context)
    when 'memory'
      record_request_to_memory(key, timestamp, context)
    end
  end
  
  def get_bucket_state(key)
    case @config['rate_limiting']['storage_backend']
    when 'redis'
      get_bucket_state_from_redis(key)
    when 'memory'
      get_bucket_state_from_memory(key)
    end
  end
  
  def save_bucket_state(key, bucket)
    case @config['rate_limiting']['storage_backend']
    when 'redis'
      save_bucket_state_to_redis(key, bucket)
    when 'memory'
      save_bucket_state_to_memory(key, bucket)
    end
  end
  
  private
  
  def setup_storage
    case @config['rate_limiting']['storage_backend']
    when 'redis'
      setup_redis_storage
    when 'memory'
      setup_memory_storage
    end
  end
  
  def setup_redis_storage
    redis_config = @config['rate_limiting']['storage']['redis']
    @redis = Redis.new(
      host: redis_config['host'],
      port: redis_config['port'],
      db: redis_config['db'],
      password: redis_config['password'],
      pool_size: redis_config['pool_size']
    )
  end
  
  def setup_memory_storage
    @memory_storage = {}
    @cleanup_interval = parse_duration(@config['rate_limiting']['storage']['memory']['cleanup_interval'])
    start_cleanup_thread
  end
  
  def get_requests_from_redis(key)
    requests = @redis.zrange(key, 0, -1, withscores: true)
    requests.map { |request, score| { timestamp: score.to_i, data: request } }
  end
  
  def record_request_to_redis(key, timestamp, context)
    @redis.zadd(key, timestamp, "#{timestamp}:#{context.to_json}")
    @redis.expire(key, 3600) # 1 hour TTL
  end
  
  def get_bucket_state_from_redis(key)
    state = @redis.hgetall(key)
    return nil if state.empty?
    
    {
      tokens: state['tokens'].to_f,
      last_refill: state['last_refill'].to_f
    }
  end
  
  def save_bucket_state_to_redis(key, bucket)
    @redis.hmset(key, 'tokens', bucket[:tokens], 'last_refill', bucket[:last_refill])
    @redis.expire(key, 3600) # 1 hour TTL
  end
  
  def get_requests_from_memory(key)
    @memory_storage[key] || []
  end
  
  def record_request_to_memory(key, timestamp, context)
    @memory_storage[key] ||= []
    @memory_storage[key] << { timestamp: timestamp, data: context }
  end
  
  def get_bucket_state_from_memory(key)
    @memory_storage[key]
  end
  
  def save_bucket_state_to_memory(key, bucket)
    @memory_storage[key] = bucket
  end
  
  def start_cleanup_thread
    Thread.new do
      loop do
        sleep @cleanup_interval
        cleanup_memory_storage
      end
    end
  end
  
  def cleanup_memory_storage
    current_time = Time.now.to_i
    cutoff_time = current_time - 3600 # 1 hour
    
    @memory_storage.each do |key, data|
      if data.is_a?(Array)
        @memory_storage[key] = data.select { |item| item[:timestamp] > cutoff_time }
      end
    end
    
    # Remove empty entries
    @memory_storage.delete_if { |key, data| data.nil? || (data.is_a?(Array) && data.empty?) }
  end
  
  def parse_duration(duration_string)
    case duration_string
    when /(\d+)m/
      $1.to_i * 60
    when /(\d+)h/
      $1.to_i * 3600
    else
      300 # Default 5 minutes
    end
  end
end
```

## 🚀 Performance Optimization

### Rate Limiting Caching
```ruby
require 'tusklang/rate_limiting'

class RateLimitingCache
  def initialize
    @cache = TuskLang::Cache::RedisCache.new
    @config = TuskLang.parse_file('config/rate_limiting.tsk')
  end
  
  def cache_rate_limit_result(identifier, limits_hash, result)
    cache_key = generate_cache_key(identifier, limits_hash)
    ttl = 60 # 1 minute cache
    
    @cache.set(cache_key, result, ttl)
  end
  
  def get_cached_rate_limit_result(identifier, limits_hash)
    cache_key = generate_cache_key(identifier, limits_hash)
    @cache.get(cache_key)
  end
  
  def invalidate_identifier_cache(identifier)
    pattern = "rate_limit:#{identifier}:*"
    @cache.delete_pattern(pattern)
  end
  
  private
  
  def generate_cache_key(identifier, limits_hash)
    "rate_limit:#{identifier}:#{limits_hash}"
  end
end
```

## 📊 Monitoring and Analytics

### Rate Limiting Analytics
```ruby
require 'tusklang/rate_limiting'

class RateLimitingAnalytics
  def initialize
    @metrics = TuskLang::Metrics::Collector.new
  end
  
  def track_rate_limit_check(identifier, strategy, allowed)
    @metrics.increment("rate_limiting.checks.total")
    @metrics.increment("rate_limiting.checks.#{strategy}")
    @metrics.increment("rate_limiting.checks.#{strategy}.#{allowed ? 'allowed' : 'denied'}")
    
    if identifier.start_with?('user:')
      @metrics.increment("rate_limiting.checks.user")
    elsif identifier.start_with?('ip:')
      @metrics.increment("rate_limiting.checks.ip")
    end
  end
  
  def track_rate_limit_violation(identifier, endpoint, limits)
    @metrics.increment("rate_limiting.violations.total")
    @metrics.increment("rate_limiting.violations.endpoint.#{endpoint}")
    
    if identifier.start_with?('user:')
      @metrics.increment("rate_limiting.violations.user")
    elsif identifier.start_with?('ip:')
      @metrics.increment("rate_limiting.violations.ip")
    end
  end
  
  def get_rate_limiting_stats
    {
      total_checks: @metrics.get("rate_limiting.checks.total"),
      allow_rate: @metrics.get_rate("rate_limiting.checks.allowed", "rate_limiting.checks.total"),
      popular_strategies: @metrics.get_top("rate_limiting.checks", 5),
      violation_rate: @metrics.get_rate("rate_limiting.violations.total", "rate_limiting.checks.total")
    }
  end
end
```

This comprehensive rate limiting system provides enterprise-grade protection features while maintaining the flexibility and power of TuskLang. The combination of multiple rate limiting strategies, distributed storage, and performance optimizations creates a robust foundation for protecting Ruby applications from abuse. 