# Rate Limit Directives in TuskLang - Bash Guide

## 🚦 **Revolutionary Rate Limiting Configuration**

Rate limit directives in TuskLang transform your configuration files into intelligent traffic control systems. No more separate rate limiting frameworks or complex throttling configurations - everything lives in your TuskLang configuration with dynamic rate limiting strategies, automatic burst handling, and intelligent traffic shaping.

> **"We don't bow to any king"** - TuskLang rate limit directives break free from traditional rate limiting constraints and bring modern traffic control capabilities to your Bash applications.

## 🚀 **Core Rate Limit Directives**

### **Basic Rate Limit Setup**
```bash
#rate-limit: 100/min              # Basic rate limit
#rate-limit-window: 60            # Time window (seconds)
#rate-limit-burst: 200            # Burst allowance
#rate-limit-strategy: sliding     # Rate limiting strategy
#rate-limit-storage: redis        # Storage backend
#rate-limit-key: ip               # Rate limit key type
```

### **Advanced Rate Limit Configuration**
```bash
#rate-limit-enabled: true         # Enable rate limiting
#rate-limit-algorithm: token-bucket # Rate limiting algorithm
#rate-limit-penalty: block        # Penalty for violations
#rate-limit-whitelist: ["127.0.0.1"] # Whitelisted IPs
#rate-limit-blacklist: []         # Blacklisted IPs
#rate-limit-headers: true         # Include rate limit headers
#rate-limit-logging: true         # Enable rate limit logging
#rate-limit-metrics: true         # Enable rate limit metrics
```

## 🔧 **Bash Rate Limit Implementation**

### **Basic Rate Limit Manager**
```bash
#!/bin/bash

# Load rate limit configuration
source <(tsk load rate-limit.tsk)

# Rate limit configuration
RATE_LIMIT_ENABLED="${rate_limit_enabled:-true}"
RATE_LIMIT_ALGORITHM="${rate_limit_algorithm:-token-bucket}"
RATE_LIMIT_PENALTY="${rate_limit_penalty:-block}"
RATE_LIMIT_HEADERS="${rate_limit_headers:-true}"
RATE_LIMIT_LOGGING="${rate_limit_logging:-true}"

# Rate limit manager
class RateLimitManager {
    constructor() {
        this.enabled = RATE_LIMIT_ENABLED
        this.algorithm = RATE_LIMIT_ALGORITHM
        this.penalty = RATE_LIMIT_PENALTY
        this.headers = RATE_LIMIT_HEADERS
        this.logging = RATE_LIMIT_LOGGING
        this.limits = new Map()
        this.whitelist = new Set()
        this.blacklist = new Set()
        this.stats = {
            allowed: 0,
            blocked: 0,
            whitelisted: 0,
            blacklisted: 0
        }
    }
    
    checkLimit(key, limit, window, burst = 0) {
        if (!this.enabled) {
            return { allowed: true, remaining: limit, reset: Date.now() + window * 1000 }
        }
        
        // Check whitelist
        if (this.whitelist.has(key)) {
            this.stats.whitelisted++
            return { allowed: true, remaining: limit, reset: Date.now() + window * 1000, whitelisted: true }
        }
        
        // Check blacklist
        if (this.blacklist.has(key)) {
            this.stats.blacklisted++
            return { allowed: false, reason: 'blacklisted' }
        }
        
        // Apply rate limiting algorithm
        switch (this.algorithm) {
            case 'token-bucket':
                return this.tokenBucketCheck(key, limit, window, burst)
            case 'leaky-bucket':
                return this.leakyBucketCheck(key, limit, window)
            case 'sliding-window':
                return this.slidingWindowCheck(key, limit, window)
            case 'fixed-window':
                return this.fixedWindowCheck(key, limit, window)
            default:
                return this.tokenBucketCheck(key, limit, window, burst)
        }
    }
    
    tokenBucketCheck(key, limit, window, burst) {
        const now = Date.now()
        const bucket = this.getBucket(key)
        
        // Calculate tokens to add
        const tokensToAdd = Math.floor((now - bucket.lastRefill) / (window * 1000) * limit)
        bucket.tokens = Math.min(bucket.capacity + burst, bucket.tokens + tokensToAdd)
        bucket.lastRefill = now
        
        if (bucket.tokens >= 1) {
            bucket.tokens--
            this.stats.allowed++
            return {
                allowed: true,
                remaining: Math.floor(bucket.tokens),
                reset: now + window * 1000,
                limit: limit
            }
        } else {
            this.stats.blocked++
            return {
                allowed: false,
                reason: 'rate_limit_exceeded',
                retry_after: Math.ceil((1 - bucket.tokens) / limit * window)
            }
        }
    }
    
    leakyBucketCheck(key, limit, window) {
        const now = Date.now()
        const bucket = this.getBucket(key)
        
        // Calculate leak rate
        const leakRate = limit / window
        const timePassed = (now - bucket.lastLeak) / 1000
        const leaked = timePassed * leakRate
        
        bucket.tokens = Math.max(0, bucket.tokens - leaked)
        bucket.lastLeak = now
        
        if (bucket.tokens < bucket.capacity) {
            bucket.tokens++
            this.stats.allowed++
            return {
                allowed: true,
                remaining: bucket.capacity - bucket.tokens,
                reset: now + window * 1000,
                limit: limit
            }
        } else {
            this.stats.blocked++
            return {
                allowed: false,
                reason: 'rate_limit_exceeded',
                retry_after: Math.ceil((bucket.tokens - bucket.capacity + 1) / leakRate)
            }
        }
    }
    
    slidingWindowCheck(key, limit, window) {
        const now = Date.now()
        const windowStart = now - window * 1000
        
        // Get or create window
        let window = this.getWindow(key, windowStart)
        
        // Remove old entries
        window.requests = window.requests.filter(timestamp => timestamp > windowStart)
        
        if (window.requests.length < limit) {
            window.requests.push(now)
            this.stats.allowed++
            return {
                allowed: true,
                remaining: limit - window.requests.length,
                reset: windowStart + window * 1000,
                limit: limit
            }
        } else {
            this.stats.blocked++
            return {
                allowed: false,
                reason: 'rate_limit_exceeded',
                retry_after: Math.ceil((window.requests[0] + window * 1000 - now) / 1000)
            }
        }
    }
    
    fixedWindowCheck(key, limit, window) {
        const now = Date.now()
        const windowStart = Math.floor(now / (window * 1000)) * (window * 1000)
        
        // Get or create window
        let window = this.getFixedWindow(key, windowStart)
        
        if (window.count < limit) {
            window.count++
            this.stats.allowed++
            return {
                allowed: true,
                remaining: limit - window.count,
                reset: windowStart + window * 1000,
                limit: limit
            }
        } else {
            this.stats.blocked++
            return {
                allowed: false,
                reason: 'rate_limit_exceeded',
                retry_after: Math.ceil((windowStart + window * 1000 - now) / 1000)
            }
        }
    }
    
    getBucket(key) {
        if (!this.limits.has(key)) {
            this.limits.set(key, {
                tokens: 0,
                capacity: 0,
                lastRefill: Date.now(),
                lastLeak: Date.now()
            })
        }
        return this.limits.get(key)
    }
    
    getWindow(key, windowStart) {
        const windowKey = `${key}:${windowStart}`
        if (!this.limits.has(windowKey)) {
            this.limits.set(windowKey, { requests: [] })
        }
        return this.limits.get(windowKey)
    }
    
    getFixedWindow(key, windowStart) {
        const windowKey = `${key}:${windowStart}`
        if (!this.limits.has(windowKey)) {
            this.limits.set(windowKey, { count: 0 })
        }
        return this.limits.get(windowKey)
    }
    
    addToWhitelist(key) {
        this.whitelist.add(key)
    }
    
    removeFromWhitelist(key) {
        this.whitelist.delete(key)
    }
    
    addToBlacklist(key) {
        this.blacklist.add(key)
    }
    
    removeFromBlacklist(key) {
        this.blacklist.delete(key)
    }
    
    getStats() {
        return { ...this.stats }
    }
    
    resetStats() {
        this.stats = { allowed: 0, blocked: 0, whitelisted: 0, blacklisted: 0 }
    }
}

# Initialize rate limit manager
const rateLimitManager = new RateLimitManager()
```

### **Token Bucket Algorithm**
```bash
#!/bin/bash

# Token bucket rate limiting implementation
token_bucket_rate_limit() {
    local key="$1"
    local limit="$2"
    local window="$3"
    local burst="$4"
    
    # Get current bucket state
    local bucket=$(get_token_bucket "$key")
    
    # Calculate current time
    local current_time=$(date +%s)
    
    # Calculate tokens to add based on time passed
    local time_passed=$((current_time - bucket["last_refill"]))
    local tokens_to_add=$((time_passed * limit / window))
    
    # Update bucket tokens
    local new_tokens=$((bucket["tokens"] + tokens_to_add))
    local max_tokens=$((limit + burst))
    
    if [[ "$new_tokens" -gt "$max_tokens" ]]; then
        new_tokens="$max_tokens"
    fi
    
    # Check if request can be processed
    if [[ "$new_tokens" -ge 1 ]]; then
        # Consume one token
        new_tokens=$((new_tokens - 1))
        
        # Update bucket
        update_token_bucket "$key" "$new_tokens" "$current_time"
        
        echo "ALLOWED:$new_tokens:$((current_time + window))"
        return 0
    else
        # Calculate retry after time
        local tokens_needed=$((1 - new_tokens))
        local retry_after=$((tokens_needed * window / limit))
        
        echo "BLOCKED:$retry_after"
        return 1
    fi
}

get_token_bucket() {
    local key="$1"
    local storage_backend="${rate_limit_storage:-memory}"
    
    case "$storage_backend" in
        "redis")
            get_redis_token_bucket "$key"
            ;;
        "file")
            get_file_token_bucket "$key"
            ;;
        "memory")
            get_memory_token_bucket "$key"
            ;;
        *)
            get_memory_token_bucket "$key"
            ;;
    esac
}

get_memory_token_bucket() {
    local key="$1"
    
    # Use associative array for memory storage
    declare -gA TOKEN_BUCKETS
    declare -gA TOKEN_BUCKETS_LAST_REFILL
    
    # Initialize if not exists
    if [[ -z "${TOKEN_BUCKETS[$key]}" ]]; then
        TOKEN_BUCKETS["$key"]=0
        TOKEN_BUCKETS_LAST_REFILL["$key"]=$(date +%s)
    fi
    
    # Return bucket state
    echo "tokens:${TOKEN_BUCKETS[$key]},last_refill:${TOKEN_BUCKETS_LAST_REFILL[$key]}"
}

update_token_bucket() {
    local key="$1"
    local tokens="$2"
    local last_refill="$3"
    
    local storage_backend="${rate_limit_storage:-memory}"
    
    case "$storage_backend" in
        "redis")
            update_redis_token_bucket "$key" "$tokens" "$last_refill"
            ;;
        "file")
            update_file_token_bucket "$key" "$tokens" "$last_refill"
            ;;
        "memory")
            update_memory_token_bucket "$key" "$tokens" "$last_refill"
            ;;
        *)
            update_memory_token_bucket "$key" "$tokens" "$last_refill"
            ;;
    esac
}

update_memory_token_bucket() {
    local key="$1"
    local tokens="$2"
    local last_refill="$3"
    
    TOKEN_BUCKETS["$key"]="$tokens"
    TOKEN_BUCKETS_LAST_REFILL["$key"]="$last_refill"
}
```

### **Sliding Window Algorithm**
```bash
#!/bin/bash

# Sliding window rate limiting implementation
sliding_window_rate_limit() {
    local key="$1"
    local limit="$2"
    local window="$3"
    
    # Get current time
    local current_time=$(date +%s)
    local window_start=$((current_time - window))
    
    # Get request timestamps for this window
    local requests=$(get_sliding_window_requests "$key" "$window_start")
    
    # Count requests in current window
    local request_count=$(echo "$requests" | wc -l)
    
    # Check if limit exceeded
    if [[ "$request_count" -lt "$limit" ]]; then
        # Add current request
        add_sliding_window_request "$key" "$current_time"
        
        echo "ALLOWED:$((limit - request_count - 1)):$((current_time + window))"
        return 0
    else
        # Find oldest request to calculate retry after
        local oldest_request=$(echo "$requests" | head -1)
        local retry_after=$((oldest_request + window - current_time))
        
        echo "BLOCKED:$retry_after"
        return 1
    fi
}

get_sliding_window_requests() {
    local key="$1"
    local window_start="$2"
    
    local storage_backend="${rate_limit_storage:-memory}"
    
    case "$storage_backend" in
        "redis")
            get_redis_sliding_window_requests "$key" "$window_start"
            ;;
        "file")
            get_file_sliding_window_requests "$key" "$window_start"
            ;;
        "memory")
            get_memory_sliding_window_requests "$key" "$window_start"
            ;;
        *)
            get_memory_sliding_window_requests "$key" "$window_start"
            ;;
    esac
}

get_memory_sliding_window_requests() {
    local key="$1"
    local window_start="$2"
    
    # Use associative array for memory storage
    declare -gA SLIDING_WINDOW_REQUESTS
    
    # Get requests for this key
    local requests="${SLIDING_WINDOW_REQUESTS[$key]:-}"
    
    if [[ -n "$requests" ]]; then
        # Filter requests within window
        echo "$requests" | tr ',' '\n' | awk -v start="$window_start" '$1 > start'
    fi
}

add_sliding_window_request() {
    local key="$1"
    local timestamp="$2"
    
    local storage_backend="${rate_limit_storage:-memory}"
    
    case "$storage_backend" in
        "redis")
            add_redis_sliding_window_request "$key" "$timestamp"
            ;;
        "file")
            add_file_sliding_window_request "$key" "$timestamp"
            ;;
        "memory")
            add_memory_sliding_window_request "$key" "$timestamp"
            ;;
        *)
            add_memory_sliding_window_request "$key" "$timestamp"
            ;;
    esac
}

add_memory_sliding_window_request() {
    local key="$1"
    local timestamp="$2"
    
    # Get existing requests
    local existing_requests="${SLIDING_WINDOW_REQUESTS[$key]:-}"
    
    # Add new request
    if [[ -n "$existing_requests" ]]; then
        SLIDING_WINDOW_REQUESTS["$key"]="$existing_requests,$timestamp"
    else
        SLIDING_WINDOW_REQUESTS["$key"]="$timestamp"
    fi
}
```

### **Redis Storage Backend**
```bash
#!/bin/bash

# Redis rate limit storage implementation
redis_rate_limit_storage() {
    local operation="$1"
    local key="$2"
    local data="$3"
    
    # Redis configuration
    local redis_host="${redis_host:-localhost}"
    local redis_port="${redis_port:-6379}"
    local redis_db="${redis_db:-0}"
    local redis_password="${redis_password:-}"
    
    local redis_cmd="redis-cli -h $redis_host -p $redis_port"
    
    if [[ -n "$redis_password" ]]; then
        redis_cmd="$redis_cmd -a $redis_password"
    fi
    
    if [[ -n "$redis_db" ]]; then
        redis_cmd="$redis_cmd -n $redis_db"
    fi
    
    case "$operation" in
        "get")
            $redis_cmd GET "$key"
            ;;
        "set")
            $redis_cmd SET "$key" "$data"
            ;;
        "incr")
            $redis_cmd INCR "$key"
            ;;
        "expire")
            local ttl="$4"
            $redis_cmd EXPIRE "$key" "$ttl"
            ;;
        "zadd")
            local score="$4"
            $redis_cmd ZADD "$key" "$score" "$data"
            ;;
        "zremrangebyscore")
            local min="$4"
            local max="$5"
            $redis_cmd ZREMRANGEBYSCORE "$key" "$min" "$max"
            ;;
        "zcard")
            $redis_cmd ZCARD "$key"
            ;;
        *)
            echo "Unknown Redis operation: $operation"
            return 1
            ;;
    esac
}

get_redis_token_bucket() {
    local key="$1"
    local bucket_key="rate_limit:token_bucket:$key"
    
    local bucket_data=$(redis_rate_limit_storage "get" "$bucket_key")
    
    if [[ -n "$bucket_data" ]]; then
        echo "$bucket_data"
    else
        echo "tokens:0,last_refill:$(date +%s)"
    fi
}

update_redis_token_bucket() {
    local key="$1"
    local tokens="$2"
    local last_refill="$3"
    
    local bucket_key="rate_limit:token_bucket:$key"
    local bucket_data="tokens:$tokens,last_refill:$last_refill"
    
    redis_rate_limit_storage "set" "$bucket_key" "$bucket_data"
    redis_rate_limit_storage "expire" "$bucket_key" 3600
}

get_redis_sliding_window_requests() {
    local key="$1"
    local window_start="$2"
    
    local window_key="rate_limit:sliding_window:$key"
    
    # Remove old requests
    redis_rate_limit_storage "zremrangebyscore" "$window_key" "0" "$window_start"
    
    # Get remaining requests
    redis_rate_limit_storage "zrange" "$window_key" "0" "-1"
}

add_redis_sliding_window_request() {
    local key="$1"
    local timestamp="$2"
    
    local window_key="rate_limit:sliding_window:$key"
    
    # Add request with timestamp as score
    redis_rate_limit_storage "zadd" "$window_key" "$timestamp" "$timestamp"
    redis_rate_limit_storage "expire" "$window_key" 3600
}
```

### **Rate Limit Headers**
```bash
#!/bin/bash

# Rate limit headers implementation
add_rate_limit_headers() {
    local response="$1"
    local result="$2"
    
    if [[ "$RATE_LIMIT_HEADERS" != "true" ]]; then
        return
    fi
    
    # Parse result
    IFS=':' read -r status remaining reset <<< "$result"
    
    case "$status" in
        "ALLOWED")
            response["headers"]+="X-RateLimit-Limit: $limit\n"
            response["headers"]+="X-RateLimit-Remaining: $remaining\n"
            response["headers"]+="X-RateLimit-Reset: $reset\n"
            ;;
        "BLOCKED")
            response["headers"]+="X-RateLimit-Limit: $limit\n"
            response["headers"]+="X-RateLimit-Remaining: 0\n"
            response["headers"]+="X-RateLimit-Reset: $reset\n"
            response["headers"]+="Retry-After: $remaining\n"
            ;;
    esac
}

log_rate_limit_event() {
    local key="$1"
    local result="$2"
    local request_info="$3"
    
    if [[ "$RATE_LIMIT_LOGGING" != "true" ]]; then
        return
    fi
    
    local timestamp=$(date '+%Y-%m-%d %H:%M:%S')
    local log_file="${rate_limit_log_file:-/var/log/rate-limit.log}"
    
    # Parse result
    IFS=':' read -r status remaining reset <<< "$result"
    
    local log_entry="$timestamp [$status] $key $remaining $reset $request_info"
    
    echo "$log_entry" >> "$log_file"
}
```

## 🎯 **Real-World Configuration Examples**

### **Complete Rate Limit Configuration**
```bash
# rate-limit-config.tsk
rate_limit_config:
  enabled: true
  algorithm: token-bucket
  penalty: block
  headers: true
  logging: true

#rate-limit: 100/min
#rate-limit-window: 60
#rate-limit-burst: 200
#rate-limit-strategy: sliding
#rate-limit-storage: redis
#rate-limit-key: ip

#rate-limit-enabled: true
#rate-limit-algorithm: token-bucket
#rate-limit-penalty: block
#rate-limit-whitelist: ["127.0.0.1"]
#rate-limit-blacklist: []
#rate-limit-headers: true
#rate-limit-logging: true
#rate-limit-metrics: true

#rate-limit-config:
#  global:
#    limit: 100
#    window: 60
#    burst: 200
#    algorithm: token-bucket
#  api:
#    limit: 1000
#    window: 3600
#    burst: 500
#    algorithm: sliding-window
#  auth:
#    limit: 5
#    window: 300
#    burst: 0
#    algorithm: fixed-window
#  upload:
#    limit: 10
#    window: 3600
#    burst: 5
#    algorithm: leaky-bucket
#  storage:
#    backend: redis
#    host: localhost
#    port: 6379
#    db: 1
#  whitelist:
#    - "127.0.0.1"
#    - "10.0.0.0/8"
#  blacklist:
#    - "192.168.1.100"
```

### **API Rate Limiting**
```bash
# api-rate-limit.tsk
api_config:
  name: "API Gateway"
  version: "1.0.0"

#rate-limit: 1000/hour
#rate-limit-window: 3600
#rate-limit-burst: 500
#rate-limit-algorithm: sliding-window
#rate-limit-storage: redis

#rate-limit-config:
#  endpoints:
#    "/api/users":
#      limit: 100
#      window: 60
#      burst: 50
#    "/api/posts":
#      limit: 200
#      window: 60
#      burst: 100
#    "/api/admin":
#      limit: 10
#      window: 60
#      burst: 0
#  users:
#    "free":
#      limit: 100
#      window: 3600
#    "premium":
#      limit: 1000
#      window: 3600
#    "enterprise":
#      limit: 10000
#      window: 3600
```

### **Authentication Rate Limiting**
```bash
# auth-rate-limit.tsk
auth_config:
  name: "Authentication Service"

#rate-limit: 5/min
#rate-limit-window: 300
#rate-limit-burst: 0
#rate-limit-algorithm: fixed-window
#rate-limit-penalty: block

#rate-limit-config:
#  login:
#    limit: 5
#    window: 300
#    burst: 0
#    algorithm: fixed-window
#    penalty: block
#  register:
#    limit: 3
#    window: 3600
#    burst: 0
#    algorithm: fixed-window
#    penalty: block
#  password_reset:
#    limit: 2
#    window: 3600
#    burst: 0
#    algorithm: fixed-window
#    penalty: block
#  mfa:
#    limit: 10
#    window: 300
#    burst: 0
#    algorithm: fixed-window
#    penalty: block
```

## 🚨 **Troubleshooting Rate Limit Directives**

### **Common Issues and Solutions**

**1. Rate Limit Not Working**
```bash
# Debug rate limiting
debug_rate_limiting() {
    local key="$1"
    local limit="$2"
    local window="$3"
    
    echo "Debugging rate limiting for key: $key"
    echo "Limit: $limit requests per ${window}s"
    
    # Check if rate limiting is enabled
    if [[ "$RATE_LIMIT_ENABLED" != "true" ]]; then
        echo "✗ Rate limiting is disabled"
        return 1
    fi
    echo "✓ Rate limiting is enabled"
    
    # Check algorithm
    echo "Algorithm: $RATE_LIMIT_ALGORITHM"
    
    # Check storage backend
    local storage_backend="${rate_limit_storage:-memory}"
    echo "Storage backend: $storage_backend"
    
    case "$storage_backend" in
        "redis")
            debug_redis_rate_limit "$key"
            ;;
        "file")
            debug_file_rate_limit "$key"
            ;;
        "memory")
            debug_memory_rate_limit "$key"
            ;;
    esac
    
    # Check whitelist/blacklist
    check_rate_limit_lists "$key"
}

debug_redis_rate_limit() {
    local key="$1"
    
    echo "Testing Redis rate limit storage..."
    
    local redis_cmd="redis-cli -h ${redis_host:-localhost} -p ${redis_port:-6379}"
    
    if [[ -n "${redis_password}" ]]; then
        redis_cmd="$redis_cmd -a ${redis_password}"
    fi
    
    # Test connection
    if $redis_cmd ping >/dev/null 2>&1; then
        echo "✓ Redis connection successful"
        
        # Check rate limit keys
        local token_key="rate_limit:token_bucket:$key"
        local window_key="rate_limit:sliding_window:$key"
        
        local token_data=$($redis_cmd GET "$token_key")
        if [[ -n "$token_data" ]]; then
            echo "✓ Token bucket data found: $token_data"
        else
            echo "⚠ No token bucket data found"
        fi
        
        local window_count=$($redis_cmd ZCARD "$window_key")
        echo "✓ Sliding window requests: $window_count"
    else
        echo "✗ Redis connection failed"
    fi
}

debug_memory_rate_limit() {
    local key="$1"
    
    echo "Testing memory rate limit storage..."
    
    # Check token bucket
    local bucket=$(get_memory_token_bucket "$key")
    echo "Token bucket: $bucket"
    
    # Check sliding window
    local requests=$(get_memory_sliding_window_requests "$key" "$(date +%s)")
    local request_count=$(echo "$requests" | wc -l)
    echo "Sliding window requests: $request_count"
}

check_rate_limit_lists() {
    local key="$1"
    
    echo "Checking rate limit lists..."
    
    # Check whitelist
    local whitelist=(${rate_limit_whitelist[@]})
    for whitelisted in "${whitelist[@]}"; do
        if [[ "$key" == "$whitelisted" ]]; then
            echo "✓ Key is whitelisted: $key"
            return 0
        fi
    done
    
    # Check blacklist
    local blacklist=(${rate_limit_blacklist[@]})
    for blacklisted in "${blacklist[@]}"; do
        if [[ "$key" == "$blacklisted" ]]; then
            echo "✗ Key is blacklisted: $key"
            return 1
        fi
    done
    
    echo "✓ Key is not in whitelist or blacklist"
}
```

**2. Performance Issues**
```bash
# Debug rate limit performance
debug_rate_limit_performance() {
    local key="$1"
    local limit="$2"
    local window="$3"
    
    echo "Testing rate limit performance..."
    
    # Test multiple requests
    local start_time=$(date +%s%N)
    
    for i in {1..100}; do
        case "$RATE_LIMIT_ALGORITHM" in
            "token-bucket")
                token_bucket_rate_limit "$key" "$limit" "$window" 0 >/dev/null
                ;;
            "sliding-window")
                sliding_window_rate_limit "$key" "$limit" "$window" >/dev/null
                ;;
            *)
                token_bucket_rate_limit "$key" "$limit" "$window" 0 >/dev/null
                ;;
        esac
    done
    
    local end_time=$(date +%s%N)
    local duration=$(( (end_time - start_time) / 1000000 ))
    
    echo "Rate limit performance: 100 operations in ${duration}ms"
    
    # Check storage performance
    local storage_backend="${rate_limit_storage:-memory}"
    case "$storage_backend" in
        "redis")
            test_redis_rate_limit_performance "$key"
            ;;
        "memory")
            test_memory_rate_limit_performance "$key"
            ;;
    esac
}

test_redis_rate_limit_performance() {
    local key="$1"
    
    echo "Testing Redis rate limit performance..."
    
    local redis_cmd="redis-cli -h ${redis_host:-localhost} -p ${redis_port:-6379}"
    
    if [[ -n "${redis_password}" ]]; then
        redis_cmd="$redis_cmd -a ${redis_password}"
    fi
    
    # Test write performance
    local write_start=$(date +%s%N)
    for i in {1..100}; do
        $redis_cmd SET "rate_limit:test:$i" "value" >/dev/null 2>&1
    done
    local write_end=$(date +%s%N)
    local write_duration=$(( (write_end - write_start) / 1000000 ))
    echo "  Redis write: 100 operations in ${write_duration}ms"
    
    # Test read performance
    local read_start=$(date +%s%N)
    for i in {1..100}; do
        $redis_cmd GET "rate_limit:test:$i" >/dev/null 2>&1
    done
    local read_end=$(date +%s%N)
    local read_duration=$(( (read_end - read_start) / 1000000 ))
    echo "  Redis read: 100 operations in ${read_duration}ms"
    
    # Cleanup
    for i in {1..100}; do
        $redis_cmd DEL "rate_limit:test:$i" >/dev/null 2>&1
    done
}

test_memory_rate_limit_performance() {
    echo "Testing memory rate limit performance..."
    
    # Test associative array operations
    declare -A test_array
    
    local write_start=$(date +%s%N)
    for i in {1..100}; do
        test_array["test_key_$i"]="value_$i"
    done
    local write_end=$(date +%s%N)
    local write_duration=$(( (write_end - write_start) / 1000000 ))
    echo "  Memory write: 100 operations in ${write_duration}ms"
    
    local read_start=$(date +%s%N)
    for i in {1..100}; do
        local value="${test_array[test_key_$i]}"
    done
    local read_end=$(date +%s%N)
    local read_duration=$(( (read_end - read_start) / 1000000 ))
    echo "  Memory read: 100 operations in ${read_duration}ms"
    
    unset test_array
}
```

## 🔒 **Security Best Practices**

### **Rate Limit Security Checklist**
```bash
# Security validation
validate_rate_limit_security() {
    echo "Validating rate limit security configuration..."
    
    # Check whitelist configuration
    if [[ -n "${rate_limit_whitelist}" ]]; then
        echo "✓ Rate limit whitelist configured"
        
        local whitelist=(${rate_limit_whitelist[@]})
        for item in "${whitelist[@]}"; do
            if [[ "$item" == "0.0.0.0/0" ]]; then
                echo "⚠ Whitelist includes 0.0.0.0/0 (allows all)"
            fi
        done
    else
        echo "⚠ No rate limit whitelist configured"
    fi
    
    # Check blacklist configuration
    if [[ -n "${rate_limit_blacklist}" ]]; then
        echo "✓ Rate limit blacklist configured"
    else
        echo "⚠ No rate limit blacklist configured"
    fi
    
    # Check storage security
    local storage_backend="${rate_limit_storage:-memory}"
    case "$storage_backend" in
        "redis")
            if [[ -n "${redis_password}" ]]; then
                echo "✓ Redis password configured"
            else
                echo "⚠ Redis password not configured"
            fi
            
            if [[ "${redis_ssl}" == "true" ]]; then
                echo "✓ Redis SSL enabled"
            else
                echo "⚠ Redis SSL not enabled"
            fi
            ;;
        "file")
            local cache_dir="${cache_dir:-/tmp/rate-limit}"
            if [[ -d "$cache_dir" ]]; then
                local perms=$(stat -c %a "$cache_dir")
                if [[ "$perms" == "700" ]]; then
                    echo "✓ Rate limit cache directory permissions secure: $perms"
                else
                    echo "⚠ Rate limit cache directory permissions should be 700, got: $perms"
                fi
            fi
            ;;
    esac
    
    # Check algorithm security
    if [[ "$RATE_LIMIT_ALGORITHM" == "fixed-window" ]]; then
        echo "⚠ Fixed window algorithm may allow burst at window boundaries"
    else
        echo "✓ Using secure rate limiting algorithm: $RATE_LIMIT_ALGORITHM"
    fi
}
```

## 📈 **Performance Optimization Tips**

### **Rate Limit Performance Checklist**
```bash
# Performance validation
validate_rate_limit_performance() {
    echo "Validating rate limit performance configuration..."
    
    # Check storage backend performance
    local storage_backend="${rate_limit_storage:-memory}"
    case "$storage_backend" in
        "memory")
            echo "✓ Memory storage backend (fastest)"
            ;;
        "redis")
            echo "✓ Redis storage backend (high performance)"
            ;;
        "file")
            echo "⚠ File storage backend (slower, persistent)"
            ;;
    esac
    
    # Check algorithm performance
    case "$RATE_LIMIT_ALGORITHM" in
        "token-bucket")
            echo "✓ Token bucket algorithm (efficient)"
            ;;
        "sliding-window")
            echo "✓ Sliding window algorithm (accurate)"
            ;;
        "fixed-window")
            echo "✓ Fixed window algorithm (simple)"
            ;;
        "leaky-bucket")
            echo "✓ Leaky bucket algorithm (smooth)"
            ;;
    esac
    
    # Check burst configuration
    if [[ -n "${rate_limit_burst}" ]]; then
        echo "✓ Burst allowance configured: ${rate_limit_burst}"
    else
        echo "⚠ No burst allowance configured"
    fi
    
    # Check monitoring
    if [[ "${rate_limit_metrics}" == "true" ]]; then
        echo "✓ Rate limit metrics enabled"
    else
        echo "⚠ Rate limit metrics not enabled"
    fi
}
```

## 🎯 **Next Steps**

- **Advanced Features**: Learn about advanced TuskLang features
- **Plugin Integration**: Explore rate limit plugins
- **Advanced Patterns**: Understand complex rate limiting patterns
- **Testing Rate Limit Directives**: Test rate limit functionality
- **Performance Tuning**: Optimize rate limit performance

---

**Rate limit directives transform your TuskLang configuration into a powerful traffic control system. They bring modern rate limiting capabilities to your Bash applications with intelligent algorithms, flexible storage backends, and comprehensive security policies!** 