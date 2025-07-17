# 🚦 Advanced Rate Limiting Strategies with TuskLang & Go

## Introduction
Rate limiting isn't just about protecting your API—it's about fairness, cost control, and system stability. TuskLang and Go let you implement sophisticated rate limiting with config-driven strategies, distributed coordination, and adaptive limits that respond to system load.

## Key Features
- **Token bucket and sliding window algorithms**
- **Distributed rate limiting with Redis/Consul**
- **Adaptive rate limiting based on system metrics**
- **User/API/endpoint-specific limits**
- **Real-time monitoring and alerting**
- **Graceful degradation strategies**

## Example: Rate Limiting Config
```ini
[rate_limit]
algorithm: token_bucket
capacity: @env("RATE_LIMIT_CAPACITY", 100)
rate: @env("RATE_LIMIT_RATE", 10)
window: @env("RATE_LIMIT_WINDOW", "1m")
storage: redis
redis_uri: @env.secure("REDIS_URI")
adaptive: @go("ratelimit.IsAdaptiveEnabled")
metrics: @metrics("rate_limit_hits", 0)
```

## Go: Token Bucket Implementation
```go
package ratelimit

import (
    "context"
    "time"
    "github.com/go-redis/redis/v8"
)

type TokenBucket struct {
    capacity int
    rate     float64
    tokens   int
    lastRefill time.Time
    redis    *redis.Client
}

func (tb *TokenBucket) Allow(ctx context.Context, key string) bool {
    // Implement token bucket algorithm with Redis
    current := time.Now()
    elapsed := current.Sub(tb.lastRefill).Seconds()
    
    // Refill tokens
    newTokens := int(elapsed * tb.rate)
    if newTokens > 0 {
        tb.tokens = min(tb.capacity, tb.tokens+newTokens)
        tb.lastRefill = current
    }
    
    if tb.tokens > 0 {
        tb.tokens--
        return true
    }
    return false
}

func IsAdaptiveEnabled() bool {
    // Check system load and enable adaptive limiting
    return getSystemLoad() > 0.8
}
```

## Sliding Window Rate Limiting
```go
type SlidingWindow struct {
    windowSize time.Duration
    maxRequests int
    redis       *redis.Client
}

func (sw *SlidingWindow) Allow(ctx context.Context, key string) bool {
    now := time.Now()
    windowStart := now.Add(-sw.windowSize)
    
    // Remove old entries and count current requests
    pipe := sw.redis.Pipeline()
    pipe.ZRemRangeByScore(ctx, key, "0", strconv.FormatInt(windowStart.Unix(), 10))
    pipe.ZCard(ctx, key)
    pipe.ZAdd(ctx, key, &redis.Z{Score: float64(now.Unix()), Member: now.UnixNano()})
    pipe.Expire(ctx, key, sw.windowSize)
    
    cmds, err := pipe.Exec(ctx)
    if err != nil {
        return false
    }
    
    currentCount := cmds[1].(*redis.IntCmd).Val()
    return currentCount < int64(sw.maxRequests)
}
```

## Distributed Rate Limiting
```ini
[distributed]
coordinator: redis
shard_key: @go("ratelimit.GenerateShardKey")
sync_interval: @env("SYNC_INTERVAL", "5s")
```

## Adaptive Rate Limiting
```go
func AdaptiveRateLimit(ctx context.Context, baseLimit int) int {
    cpuLoad := getCPULoad()
    memoryUsage := getMemoryUsage()
    errorRate := getErrorRate()
    
    // Reduce limits under high load
    if cpuLoad > 0.8 || memoryUsage > 0.9 || errorRate > 0.05 {
        return int(float64(baseLimit) * 0.5)
    }
    
    // Increase limits under low load
    if cpuLoad < 0.3 && memoryUsage < 0.5 && errorRate < 0.01 {
        return int(float64(baseLimit) * 1.5)
    }
    
    return baseLimit
}
```

## User/API/Endpoint-Specific Limits
```ini
[limits]
default: @env("DEFAULT_LIMIT", 100)
premium_users: @env("PREMIUM_LIMIT", 1000)
admin_users: @env("ADMIN_LIMIT", 10000)
api_v1: @env("API_V1_LIMIT", 50)
api_v2: @env("API_V2_LIMIT", 200)
```

## Integration with Monitoring
```go
func RecordRateLimitEvent(ctx context.Context, key string, allowed bool) {
    if allowed {
        metrics.IncCounter("rate_limit_allowed", map[string]string{"key": key})
    } else {
        metrics.IncCounter("rate_limit_rejected", map[string]string{"key": key})
    }
    
    // Alert if rejection rate is high
    rejectionRate := getRejectionRate(key)
    if rejectionRate > 0.1 {
        alert.Send("High rate limit rejections", map[string]interface{}{
            "key": key,
            "rate": rejectionRate,
        })
    }
}
```

## Graceful Degradation
```go
func GracefulDegradation(ctx context.Context, key string) http.HandlerFunc {
    return func(w http.ResponseWriter, r *http.Request) {
        if !rateLimiter.Allow(ctx, key) {
            // Return cached response if available
            if cached := cache.Get(key); cached != nil {
                w.Header().Set("X-Rate-Limited", "true")
                w.Header().Set("X-Cached-Response", "true")
                w.Write(cached.([]byte))
                return
            }
            
            // Return 429 with retry-after
            w.Header().Set("Retry-After", "60")
            w.WriteHeader(http.StatusTooManyRequests)
            w.Write([]byte(`{"error": "Rate limit exceeded"}`))
            return
        }
        
        // Process normal request
        next.ServeHTTP(w, r)
    }
}
```

## Best Practices
- **Use Redis for distributed coordination**
- **Implement adaptive limits based on system metrics**
- **Monitor rejection rates and alert on anomalies**
- **Provide graceful degradation with cached responses**
- **Use different limits for different user tiers**
- **Implement retry-after headers for client guidance**

## Security Considerations
- **Validate rate limit keys to prevent abuse**
- **Use secure Redis connections with @env.secure**
- **Implement rate limit bypass for emergency access**
- **Monitor for rate limit evasion attempts**

## Performance Optimization
- **Use Redis pipelining for batch operations**
- **Implement local caching for frequently accessed limits**
- **Use efficient data structures (sorted sets for sliding window)**
- **Monitor Redis performance and scale as needed**

## Troubleshooting
- **Check Redis connectivity and performance**
- **Monitor rate limit metrics for anomalies**
- **Verify adaptive limiting logic**
- **Check for memory leaks in rate limit storage**

## Conclusion
TuskLang + Go = rate limiting that's intelligent, distributed, and adaptive. Protect your systems while maintaining performance and user experience. 