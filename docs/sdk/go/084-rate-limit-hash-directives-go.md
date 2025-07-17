# Rate Limit Hash Directives in TuskLang for Go

## Overview

Rate limit hash directives in TuskLang provide powerful rate limiting configuration capabilities directly in your configuration files. These directives enable you to define sophisticated rate limiting strategies, throttling policies, and protection mechanisms with Go integration for secure and scalable applications.

## Basic Rate Limit Directive Syntax

```go
// TuskLang rate limit directive
#rate_limit {
    strategies: {
        token_bucket: {
            enabled: true
            capacity: 100
            rate: 10
            refill_interval: "1s"
        }
        
        sliding_window: {
            enabled: true
            window_size: "1m"
            max_requests: 60
        }
        
        fixed_window: {
            enabled: true
            window_size: "1h"
            max_requests: 1000
        }
    }
    
    policies: {
        api_requests: {
            strategy: "token_bucket"
            limits: {
                anonymous: {
                    requests_per_minute: 10
                    burst: 5
                }
                
                authenticated: {
                    requests_per_minute: 100
                    burst: 20
                }
                
                premium: {
                    requests_per_minute: 1000
                    burst: 100
                }
            }
        }
        
        login_attempts: {
            strategy: "sliding_window"
            limits: {
                default: {
                    requests_per_minute: 5
                    window_size: "15m"
                    block_duration: "30m"
                }
            }
        }
        
        file_uploads: {
            strategy: "fixed_window"
            limits: {
                default: {
                    requests_per_hour: 10
                    max_file_size: "10MB"
                }
            }
        }
    }
    
    routes: {
        "/api/v1": {
            policy: "api_requests"
            key_source: "ip"
            headers: ["X-API-Key", "Authorization"]
        }
        
        "/auth/login": {
            policy: "login_attempts"
            key_source: "ip"
            block_on_violation: true
        }
        
        "/upload": {
            policy: "file_uploads"
            key_source: "user_id"
            track_bandwidth: true
        }
    }
    
    storage: {
        redis: {
            enabled: true
            url: "@env('REDIS_URL')"
            key_prefix: "rate_limit:"
        }
        
        memory: {
            enabled: true
            cleanup_interval: "1m"
        }
    }
}
```

## Go Integration

```go
package main

import (
    "context"
    "fmt"
    "log"
    "net/http"
    "strconv"
    "strings"
    "sync"
    "time"
    
    "github.com/go-redis/redis/v8"
    "github.com/tusklang/go-sdk"
)

type RateLimitDirective struct {
    Strategies map[string]Strategy `tsk:"strategies"`
    Policies   map[string]Policy   `tsk:"policies"`
    Routes     map[string]Route    `tsk:"routes"`
    Storage    StorageConfig       `tsk:"storage"`
}

type Strategy struct {
    Enabled bool                   `tsk:"enabled"`
    Config  map[string]interface{} `tsk:",inline"`
}

type Policy struct {
    Strategy string                 `tsk:"strategy"`
    Limits   map[string]Limit      `tsk:"limits"`
}

type Limit struct {
    RequestsPerMinute int    `tsk:"requests_per_minute"`
    Burst             int    `tsk:"burst"`
    WindowSize        string `tsk:"window_size"`
    BlockDuration     string `tsk:"block_duration"`
    MaxFileSize       string `tsk:"max_file_size"`
}

type Route struct {
    Policy           string `tsk:"policy"`
    KeySource        string `tsk:"key_source"`
    BlockOnViolation bool   `tsk:"block_on_violation"`
    TrackBandwidth   bool   `tsk:"track_bandwidth"`
    Headers          []string `tsk:"headers"`
}

type StorageConfig struct {
    Redis  RedisConfig  `tsk:"redis"`
    Memory MemoryConfig `tsk:"memory"`
}

type RedisConfig struct {
    Enabled  bool   `tsk:"enabled"`
    URL      string `tsk:"url"`
    KeyPrefix string `tsk:"key_prefix"`
}

type MemoryConfig struct {
    Enabled         bool   `tsk:"enabled"`
    CleanupInterval string `tsk:"cleanup_interval"`
}

type RateLimitManager struct {
    directive RateLimitDirective
    strategies map[string]RateLimitStrategy
    policies   map[string]Policy
    routes     map[string]Route
    storage    RateLimitStorage
    mu         sync.RWMutex
}

type RateLimitStrategy interface {
    Allow(key string, limit Limit) (bool, error)
    Reset(key string) error
}

type RateLimitStorage interface {
    Get(key string) (int, error)
    Increment(key string, ttl time.Duration) (int, error)
    Set(key string, value int, ttl time.Duration) error
    Delete(key string) error
}

type TokenBucketStrategy struct {
    storage RateLimitStorage
}

type SlidingWindowStrategy struct {
    storage RateLimitStorage
}

type FixedWindowStrategy struct {
    storage RateLimitStorage
}

type RedisStorage struct {
    client *redis.Client
    prefix string
}

type MemoryStorage struct {
    data map[string]memoryItem
    mu   sync.RWMutex
}

type memoryItem struct {
    count     int
    resetTime time.Time
}

func main() {
    // Load rate limit configuration
    config, err := tusk.LoadFile("rate-limit-config.tsk")
    if err != nil {
        log.Fatalf("Error loading rate limit config: %v", err)
    }
    
    var rateLimitDirective RateLimitDirective
    if err := config.Get("#rate_limit", &rateLimitDirective); err != nil {
        log.Fatalf("Error parsing rate limit directive: %v", err)
    }
    
    // Initialize rate limit manager
    rateLimitManager := NewRateLimitManager(rateLimitDirective)
    
    // Setup HTTP server with rate limiting
    mux := http.NewServeMux()
    
    // Add rate limiting middleware
    mux.HandleFunc("/api/v1/users", rateLimitManager.withRateLimit(handleUsers))
    mux.HandleFunc("/auth/login", rateLimitManager.withRateLimit(handleLogin))
    mux.HandleFunc("/upload", rateLimitManager.withRateLimit(handleUpload))
    
    log.Println("Server starting on :8080")
    log.Fatal(http.ListenAndServe(":8080", mux))
}

func NewRateLimitManager(directive RateLimitDirective) *RateLimitManager {
    manager := &RateLimitManager{
        directive:  directive,
        strategies: make(map[string]RateLimitStrategy),
        policies:   directive.Policies,
        routes:     directive.Routes,
    }
    
    // Initialize storage
    if directive.Storage.Redis.Enabled {
        manager.storage = manager.createRedisStorage(directive.Storage.Redis)
    } else if directive.Storage.Memory.Enabled {
        manager.storage = manager.createMemoryStorage(directive.Storage.Memory)
    }
    
    // Initialize strategies
    for name, strategy := range directive.Strategies {
        if !strategy.Enabled {
            continue
        }
        
        var rateLimitStrategy RateLimitStrategy
        switch name {
        case "token_bucket":
            rateLimitStrategy = &TokenBucketStrategy{storage: manager.storage}
        case "sliding_window":
            rateLimitStrategy = &SlidingWindowStrategy{storage: manager.storage}
        case "fixed_window":
            rateLimitStrategy = &FixedWindowStrategy{storage: manager.storage}
        }
        
        if rateLimitStrategy != nil {
            manager.strategies[name] = rateLimitStrategy
        }
    }
    
    return manager
}

func (rlm *RateLimitManager) createRedisStorage(config RedisConfig) RateLimitStorage {
    client := redis.NewClient(&redis.Options{
        Addr: config.URL,
    })
    
    return &RedisStorage{
        client: client,
        prefix: config.KeyPrefix,
    }
}

func (rlm *RateLimitManager) createMemoryStorage(config MemoryConfig) RateLimitStorage {
    storage := &MemoryStorage{
        data: make(map[string]memoryItem),
    }
    
    // Start cleanup goroutine
    cleanupInterval, _ := time.ParseDuration(config.CleanupInterval)
    go storage.cleanup(cleanupInterval)
    
    return storage
}

// Rate limiting middleware
func (rlm *RateLimitManager) withRateLimit(handler http.HandlerFunc) http.HandlerFunc {
    return func(w http.ResponseWriter, r *http.Request) {
        // Find matching route configuration
        route := rlm.findRoute(r.URL.Path)
        if route == nil {
            handler(w, r)
            return
        }
        
        // Get rate limit key
        key := rlm.getRateLimitKey(r, *route)
        if key == "" {
            handler(w, r)
            return
        }
        
        // Get policy
        policy, exists := rlm.policies[route.Policy]
        if !exists {
            handler(w, r)
            return
        }
        
        // Get strategy
        strategy, exists := rlm.strategies[policy.Strategy]
        if !exists {
            handler(w, r)
            return
        }
        
        // Get limit based on user type
        limit := rlm.getLimit(policy, r)
        
        // Check rate limit
        allowed, err := strategy.Allow(key, limit)
        if err != nil {
            log.Printf("Rate limit error: %v", err)
            http.Error(w, "Internal server error", http.StatusInternalServerError)
            return
        }
        
        if !allowed {
            if route.BlockOnViolation {
                http.Error(w, "Rate limit exceeded", http.StatusTooManyRequests)
                return
            }
            
            // Add rate limit headers
            w.Header().Set("X-RateLimit-Limit", strconv.Itoa(limit.RequestsPerMinute))
            w.Header().Set("X-RateLimit-Remaining", "0")
            w.Header().Set("X-RateLimit-Reset", time.Now().Add(time.Minute).Format(time.RFC3339))
        }
        
        handler(w, r)
    }
}

func (rlm *RateLimitManager) findRoute(path string) *Route {
    for routePath, route := range rlm.routes {
        if strings.HasPrefix(path, routePath) {
            return &route
        }
    }
    return nil
}

func (rlm *RateLimitManager) getRateLimitKey(r *http.Request, route Route) string {
    switch route.KeySource {
    case "ip":
        return rlm.getClientIP(r)
    case "user_id":
        return rlm.getUserID(r)
    case "api_key":
        return rlm.getAPIKey(r, route.Headers)
    default:
        return rlm.getClientIP(r)
    }
}

func (rlm *RateLimitManager) getClientIP(r *http.Request) string {
    // Check for forwarded headers
    if ip := r.Header.Get("X-Forwarded-For"); ip != "" {
        return strings.Split(ip, ",")[0]
    }
    if ip := r.Header.Get("X-Real-IP"); ip != "" {
        return ip
    }
    
    // Get remote address
    addr := r.RemoteAddr
    if colonIndex := strings.LastIndex(addr, ":"); colonIndex != -1 {
        return addr[:colonIndex]
    }
    
    return addr
}

func (rlm *RateLimitManager) getUserID(r *http.Request) string {
    // Extract user ID from context or headers
    // This would depend on your authentication system
    return r.Header.Get("X-User-ID")
}

func (rlm *RateLimitManager) getAPIKey(r *http.Request, headers []string) string {
    for _, header := range headers {
        if value := r.Header.Get(header); value != "" {
            return value
        }
    }
    return ""
}

func (rlm *RateLimitManager) getLimit(policy Policy, r *http.Request) Limit {
    // Determine user type based on request
    userType := rlm.getUserType(r)
    
    if limit, exists := policy.Limits[userType]; exists {
        return limit
    }
    
    // Return default limit
    for _, limit := range policy.Limits {
        return limit
    }
    
    // Fallback limit
    return Limit{
        RequestsPerMinute: 10,
        Burst:             5,
    }
}

func (rlm *RateLimitManager) getUserType(r *http.Request) string {
    // Determine user type based on authentication
    if r.Header.Get("Authorization") != "" {
        return "authenticated"
    }
    
    if r.Header.Get("X-API-Key") != "" {
        return "premium"
    }
    
    return "anonymous"
}

// TokenBucketStrategy implementation
func (tb *TokenBucketStrategy) Allow(key string, limit Limit) (bool, error) {
    bucketKey := fmt.Sprintf("bucket:%s", key)
    
    // Get current token count
    current, err := tb.storage.Get(bucketKey)
    if err != nil {
        // Initialize bucket
        current = limit.Burst
        tb.storage.Set(bucketKey, current, time.Minute)
    }
    
    if current > 0 {
        // Consume token
        tb.storage.Increment(bucketKey, time.Minute)
        return true, nil
    }
    
    return false, nil
}

func (tb *TokenBucketStrategy) Reset(key string) error {
    bucketKey := fmt.Sprintf("bucket:%s", key)
    return tb.storage.Delete(bucketKey)
}

// SlidingWindowStrategy implementation
func (sw *SlidingWindowStrategy) Allow(key string, limit Limit) (bool, error) {
    windowKey := fmt.Sprintf("window:%s", key)
    
    // Get current request count
    current, err := sw.storage.Get(windowKey)
    if err != nil {
        current = 0
    }
    
    if current < limit.RequestsPerMinute {
        // Increment counter
        sw.storage.Increment(windowKey, time.Minute)
        return true, nil
    }
    
    return false, nil
}

func (sw *SlidingWindowStrategy) Reset(key string) error {
    windowKey := fmt.Sprintf("window:%s", key)
    return sw.storage.Delete(windowKey)
}

// FixedWindowStrategy implementation
func (fw *FixedWindowStrategy) Allow(key string, limit Limit) (bool, error) {
    // Create window key based on current hour
    window := time.Now().Truncate(time.Hour)
    windowKey := fmt.Sprintf("fixed:%s:%d", key, window.Unix())
    
    // Get current request count
    current, err := fw.storage.Get(windowKey)
    if err != nil {
        current = 0
    }
    
    if current < limit.RequestsPerMinute {
        // Increment counter
        fw.storage.Increment(windowKey, time.Hour)
        return true, nil
    }
    
    return false, nil
}

func (fw *FixedWindowStrategy) Reset(key string) error {
    // Reset all windows for this key
    return nil
}

// RedisStorage implementation
func (rs *RedisStorage) Get(key string) (int, error) {
    ctx := context.Background()
    fullKey := rs.prefix + key
    
    result, err := rs.client.Get(ctx, fullKey).Result()
    if err == redis.Nil {
        return 0, fmt.Errorf("key not found")
    }
    if err != nil {
        return 0, err
    }
    
    return strconv.Atoi(result)
}

func (rs *RedisStorage) Increment(key string, ttl time.Duration) (int, error) {
    ctx := context.Background()
    fullKey := rs.prefix + key
    
    pipe := rs.client.Pipeline()
    incr := pipe.Incr(ctx, fullKey)
    pipe.Expire(ctx, fullKey, ttl)
    
    _, err := pipe.Exec(ctx)
    if err != nil {
        return 0, err
    }
    
    return int(incr.Val()), nil
}

func (rs *RedisStorage) Set(key string, value int, ttl time.Duration) error {
    ctx := context.Background()
    fullKey := rs.prefix + key
    
    return rs.client.Set(ctx, fullKey, value, ttl).Err()
}

func (rs *RedisStorage) Delete(key string) error {
    ctx := context.Background()
    fullKey := rs.prefix + key
    
    return rs.client.Del(ctx, fullKey).Err()
}

// MemoryStorage implementation
func (ms *MemoryStorage) Get(key string) (int, error) {
    ms.mu.RLock()
    defer ms.mu.RUnlock()
    
    item, exists := ms.data[key]
    if !exists {
        return 0, fmt.Errorf("key not found")
    }
    
    if time.Now().After(item.resetTime) {
        delete(ms.data, key)
        return 0, fmt.Errorf("key expired")
    }
    
    return item.count, nil
}

func (ms *MemoryStorage) Increment(key string, ttl time.Duration) (int, error) {
    ms.mu.Lock()
    defer ms.mu.Unlock()
    
    item, exists := ms.data[key]
    if !exists {
        item = memoryItem{
            count:     0,
            resetTime: time.Now().Add(ttl),
        }
    }
    
    item.count++
    ms.data[key] = item
    
    return item.count, nil
}

func (ms *MemoryStorage) Set(key string, value int, ttl time.Duration) error {
    ms.mu.Lock()
    defer ms.mu.Unlock()
    
    ms.data[key] = memoryItem{
        count:     value,
        resetTime: time.Now().Add(ttl),
    }
    
    return nil
}

func (ms *MemoryStorage) Delete(key string) error {
    ms.mu.Lock()
    defer ms.mu.Unlock()
    
    delete(ms.data, key)
    return nil
}

func (ms *MemoryStorage) cleanup(interval time.Duration) {
    ticker := time.NewTicker(interval)
    defer ticker.Stop()
    
    for range ticker.C {
        ms.mu.Lock()
        now := time.Now()
        for key, item := range ms.data {
            if now.After(item.resetTime) {
                delete(ms.data, key)
            }
        }
        ms.mu.Unlock()
    }
}

// Handler functions
func handleUsers(w http.ResponseWriter, r *http.Request) {
    w.Header().Set("Content-Type", "application/json")
    w.Write([]byte(`{"users": []}`))
}

func handleLogin(w http.ResponseWriter, r *http.Request) {
    w.Header().Set("Content-Type", "application/json")
    w.Write([]byte(`{"message": "Login endpoint"}`))
}

func handleUpload(w http.ResponseWriter, r *http.Request) {
    w.Header().Set("Content-Type", "application/json")
    w.Write([]byte(`{"message": "Upload endpoint"}`))
}
```

## Advanced Rate Limit Features

### Adaptive Rate Limiting

```go
// TuskLang configuration with adaptive rate limiting
#rate_limit {
    adaptive: {
        enabled: true
        factors: {
            user_reputation: {
                weight: 0.3
                thresholds: {
                    high: 1000
                    medium: 100
                    low: 10
                }
            }
            
            time_of_day: {
                weight: 0.2
                peak_hours: ["09:00-12:00", "14:00-17:00"]
            }
            
            request_complexity: {
                weight: 0.5
                simple: 100
                complex: 10
            }
        }
    }
}
```

### Rate Limit Analytics

```go
// TuskLang configuration with rate limit analytics
#rate_limit {
    analytics: {
        enabled: true
        metrics: {
            requests_per_second: true
            blocked_requests: true
            top_offenders: true
        }
        
        alerts: {
            threshold: 1000
            window: "1m"
            channels: ["slack", "email"]
        }
    }
}
```

## Performance Considerations

- **Storage Optimization**: Use efficient storage backends for rate limit data
- **Key Design**: Design rate limit keys for optimal performance
- **Caching**: Cache rate limit results to reduce storage lookups
- **Batch Operations**: Use batch operations for bulk rate limit checks
- **Async Processing**: Use goroutines for non-blocking rate limit operations

## Security Notes

- **Key Validation**: Validate rate limit keys to prevent manipulation
- **Storage Security**: Secure rate limit storage to prevent tampering
- **Monitoring**: Monitor rate limit violations for security threats
- **Whitelisting**: Implement whitelisting for trusted clients
- **Audit Logging**: Log rate limit events for security auditing

## Best Practices

1. **Appropriate Limits**: Set appropriate rate limits for different endpoints
2. **User Segmentation**: Use different limits for different user types
3. **Monitoring**: Monitor rate limit effectiveness and adjust as needed
4. **Documentation**: Document rate limit policies and procedures
5. **Testing**: Test rate limiting under various load conditions
6. **Graceful Degradation**: Implement graceful degradation when limits are exceeded

## Integration Examples

### With Gin Framework

```go
import (
    "github.com/gin-gonic/gin"
    "github.com/tusklang/go-sdk"
)

func setupGinRateLimit(config tusk.Config) *gin.Engine {
    var rateLimitDirective RateLimitDirective
    config.Get("#rate_limit", &rateLimitDirective)
    
    rateLimitManager := NewRateLimitManager(rateLimitDirective)
    
    router := gin.Default()
    
    // Add rate limit middleware
    router.Use(rateLimitMiddleware(rateLimitManager))
    
    return router
}

func rateLimitMiddleware(rateLimitManager *RateLimitManager) gin.HandlerFunc {
    return func(c *gin.Context) {
        // Implement rate limiting logic
        c.Set("rate_limit", rateLimitManager)
        c.Next()
    }
}
```

### With Echo Framework

```go
import (
    "github.com/labstack/echo/v4"
    "github.com/tusklang/go-sdk"
)

func setupEchoRateLimit(config tusk.Config) *echo.Echo {
    var rateLimitDirective RateLimitDirective
    config.Get("#rate_limit", &rateLimitDirective)
    
    rateLimitManager := NewRateLimitManager(rateLimitDirective)
    
    e := echo.New()
    
    // Add rate limit middleware
    e.Use(rateLimitMiddleware(rateLimitManager))
    
    return e
}
```

This comprehensive rate limit hash directives documentation provides Go developers with everything they need to build sophisticated rate limiting systems using TuskLang's powerful directive system. 