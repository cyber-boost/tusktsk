# Rate Limit Directives - Go

## 🎯 What Are Rate Limit Directives?

Rate limit directives (`#rate_limit`) in TuskLang allow you to define request limiting, throttling rules, IP-based restrictions, and user-based limits directly in your configuration files. They transform static config into executable rate limiting logic.

```go
// Rate limit directives define your entire rate limiting system
type RateLimitConfig struct {
    Rules       map[string]string `tsk:"#rate_limit_rules"`
    Strategies  map[string]string `tsk:"#rate_limit_strategies"`
    Storage     map[string]string `tsk:"#rate_limit_storage"`
    Actions     map[string]string `tsk:"#rate_limit_actions"`
}
```

## 🚀 Why Rate Limit Directives Matter

### Traditional Rate Limiting Development
```go
// Old way - scattered across multiple files
func main() {
    // Rate limiting configuration scattered
    limiter := rate.NewLimiter(rate.Every(time.Minute), 100)
    
    // Rate limiting middleware defined in code
    rateLimitMiddleware := func(next http.Handler) http.Handler {
        return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
            if !limiter.Allow() {
                http.Error(w, "Rate limit exceeded", http.StatusTooManyRequests)
                return
            }
            next.ServeHTTP(w, r)
        })
    }
    
    // Different limits for different endpoints
    apiLimiter := rate.NewLimiter(rate.Every(time.Minute), 50)
    loginLimiter := rate.NewLimiter(rate.Every(time.Minute), 5)
    
    // Manual IP-based limiting
    ipLimiters := make(map[string]*rate.Limiter)
    // ... complex IP tracking logic
}
```

### TuskLang Rate Limit Directives - Declarative & Dynamic
```tsk
# rate-limit.tsk - Complete rate limit definition
rate_limit_rules: #rate_limit("""
    api_requests -> API request limiting
        limit: 100/minute
        burst: 20
        key: "ip_address"
        storage: "redis"
        strategy: "sliding_window"
    
    login_attempts -> Login attempt limiting
        limit: 5/minute
        burst: 1
        key: "ip_address"
        storage: "redis"
        strategy: "fixed_window"
        action: "block_15m"
    
    user_actions -> User action limiting
        limit: 1000/hour
        burst: 100
        key: "user_id"
        storage: "redis"
        strategy: "token_bucket"
    
    admin_actions -> Admin action limiting
        limit: 10000/hour
        burst: 1000
        key: "user_id"
        storage: "redis"
        strategy: "sliding_window"
        condition: "user.role == 'admin'"
""")

rate_limit_storage: #rate_limit("""
    redis -> Redis rate limit storage
        url: #env("REDIS_URL")
        db: 1
        prefix: "rate_limit:"
        ttl: 1h
    
    memory -> In-memory rate limit storage
        max_entries: 10000
        cleanup_interval: 5m
        eviction_policy: "lru"
""")

rate_limit_actions: #rate_limit("""
    block_15m -> Block for 15 minutes
        action: "block"
        duration: 15m
        message: "Too many requests, try again in 15 minutes"
    
    throttle -> Throttle requests
        action: "throttle"
        delay: 1s
        message: "Request throttled"
    
    challenge -> Require CAPTCHA
        action: "challenge"
        type: "captcha"
        message: "Please complete CAPTCHA"
""")
```

## 📋 Rate Limit Directive Types

### 1. **Rule Directives** (`#rate_limit_rules`)
- Rate limit rule definitions
- Limit and burst configuration
- Key generation strategies
- Storage backends

### 2. **Strategy Directives** (`#rate_limit_strategies`)
- Rate limiting algorithms
- Window types (fixed, sliding)
- Token bucket configuration
- Leaky bucket settings

### 3. **Storage Directives** (`#rate_limit_storage`)
- Storage backend configuration
- Connection pooling
- Data persistence
- Cleanup strategies

### 4. **Action Directives** (`#rate_limit_actions`)
- Rate limit violation actions
- Blocking strategies
- Throttling mechanisms
- Challenge responses

## 🔧 Basic Rate Limit Directive Syntax

### Simple Rate Limit Rule
```tsk
# Basic rate limit rule
api_limit: #rate_limit("100/minute -> ip_address")
```

### Rate Limit Rule with Configuration
```tsk
# Rate limit rule with detailed configuration
login_limit: #rate_limit("""
    limit: 5/minute
    burst: 1
    key: "ip_address"
    storage: "redis"
    strategy: "fixed_window"
    action: "block_15m"
""")
```

### Multiple Rate Limit Rules
```tsk
# Multiple rate limit rules
rate_limits: #rate_limit("""
    api -> 100/minute -> ip_address -> redis
    login -> 5/minute -> ip_address -> redis
    upload -> 10/hour -> user_id -> redis
    admin -> 1000/hour -> user_id -> redis
""")
```

## 🎯 Go Integration Patterns

### Struct Tags for Rate Limit Directives
```go
type RateLimitConfig struct {
    // Rate limit rules
    Rules string `tsk:"#rate_limit_rules"`
    
    // Rate limiting strategies
    Strategies string `tsk:"#rate_limit_strategies"`
    
    // Storage configuration
    Storage string `tsk:"#rate_limit_storage"`
    
    // Action configuration
    Actions string `tsk:"#rate_limit_actions"`
}
```

### Rate Limit Application Setup
```go
package main

import (
    "github.com/tusklang/go-sdk"
    "github.com/gorilla/mux"
    "net/http"
)

func main() {
    // Load rate limit configuration
    config := tusk.LoadConfig("rate-limit.tsk")
    
    var rateLimitConfig RateLimitConfig
    config.Unmarshal(&rateLimitConfig)
    
    // Create rate limit system from directives
    rateLimiter := tusk.NewRateLimitSystem(rateLimitConfig)
    
    // Create router
    router := mux.NewRouter()
    
    // Apply rate limit middleware
    tusk.ApplyRateLimitMiddleware(router, rateLimiter)
    
    // Start server
    http.ListenAndServe(":8080", router)
}
```

### Rate Limit Handler Implementation
```go
package ratelimit

import (
    "context"
    "fmt"
    "net/http"
    "strconv"
    "strings"
    "time"
    "github.com/go-redis/redis/v8"
    "golang.org/x/time/rate"
)

// Rate limiter interface
type RateLimiter interface {
    Allow(ctx context.Context, key string, rule string) (bool, error)
    Reset(ctx context.Context, key string, rule string) error
    GetRemaining(ctx context.Context, key string, rule string) (int, error)
}

// Redis-based rate limiter
type RedisRateLimiter struct {
    client *redis.Client
    rules  map[string]RateLimitRule
}

type RateLimitRule struct {
    Limit     int           `json:"limit"`
    Burst     int           `json:"burst"`
    Window    time.Duration `json:"window"`
    Strategy  string        `json:"strategy"`
    Action    string        `json:"action"`
    Storage   string        `json:"storage"`
}

func NewRedisRateLimiter(url string, rules map[string]RateLimitRule) (*RedisRateLimiter, error) {
    client := redis.NewClient(&redis.Options{
        Addr: url,
        DB:   1,
    })
    
    // Test connection
    ctx := context.Background()
    if err := client.Ping(ctx).Err(); err != nil {
        return nil, fmt.Errorf("failed to connect to Redis: %v", err)
    }
    
    return &RedisRateLimiter{
        client: client,
        rules:  rules,
    }, nil
}

func (rrl *RedisRateLimiter) Allow(ctx context.Context, key string, rule string) (bool, error) {
    ruleConfig, exists := rrl.rules[rule]
    if !exists {
        return true, nil // No rule found, allow request
    }
    
    redisKey := fmt.Sprintf("rate_limit:%s:%s", rule, key)
    
    switch ruleConfig.Strategy {
    case "fixed_window":
        return rrl.fixedWindowAllow(ctx, redisKey, ruleConfig)
    case "sliding_window":
        return rrl.slidingWindowAllow(ctx, redisKey, ruleConfig)
    case "token_bucket":
        return rrl.tokenBucketAllow(ctx, redisKey, ruleConfig)
    default:
        return rrl.fixedWindowAllow(ctx, redisKey, ruleConfig)
    }
}

func (rrl *RedisRateLimiter) fixedWindowAllow(ctx context.Context, key string, rule RateLimitRule) (bool, error) {
    // Get current window
    now := time.Now()
    windowStart := now.Truncate(rule.Window)
    windowKey := fmt.Sprintf("%s:%d", key, windowStart.Unix())
    
    // Get current count
    count, err := rrl.client.Get(ctx, windowKey).Int()
    if err != nil && err != redis.Nil {
        return false, err
    }
    
    if count >= rule.Limit {
        return false, nil
    }
    
    // Increment count
    pipe := rrl.client.Pipeline()
    pipe.Incr(ctx, windowKey)
    pipe.Expire(ctx, windowKey, rule.Window)
    _, err = pipe.Exec(ctx)
    
    return err == nil, err
}

func (rrl *RedisRateLimiter) slidingWindowAllow(ctx context.Context, key string, rule RateLimitRule) (bool, error) {
    now := time.Now()
    windowStart := now.Add(-rule.Window)
    
    // Remove old entries
    pipe := rrl.client.Pipeline()
    pipe.ZRemRangeByScore(ctx, key, "0", fmt.Sprintf("%d", windowStart.Unix()))
    
    // Get current count
    countCmd := pipe.ZCard(ctx, key)
    _, err := pipe.Exec(ctx)
    if err != nil {
        return false, err
    }
    
    count := int(countCmd.Val())
    if count >= rule.Limit {
        return false, nil
    }
    
    // Add current request
    pipe = rrl.client.Pipeline()
    pipe.ZAdd(ctx, key, &redis.Z{
        Score:  float64(now.Unix()),
        Member: now.UnixNano(),
    })
    pipe.Expire(ctx, key, rule.Window)
    _, err = pipe.Exec(ctx)
    
    return err == nil, err
}

func (rrl *RedisRateLimiter) tokenBucketAllow(ctx context.Context, key string, rule RateLimitRule) (bool, error) {
    now := time.Now()
    
    // Get current tokens and last refill time
    pipe := rrl.client.Pipeline()
    tokensCmd := pipe.HGet(ctx, key, "tokens")
    lastRefillCmd := pipe.HGet(ctx, key, "last_refill")
    _, err := pipe.Exec(ctx)
    if err != nil && err != redis.Nil {
        return false, err
    }
    
    tokens := rule.Burst
    lastRefill := now
    
    if tokensCmd.Val() != "" {
        tokens, _ = strconv.Atoi(tokensCmd.Val())
    }
    if lastRefillCmd.Val() != "" {
        lastRefillUnix, _ := strconv.ParseInt(lastRefillCmd.Val(), 10, 64)
        lastRefill = time.Unix(lastRefillUnix, 0)
    }
    
    // Refill tokens
    timePassed := now.Sub(lastRefill)
    tokensToAdd := int(timePassed.Seconds() * float64(rule.Limit) / rule.Window.Seconds())
    tokens = min(tokens+tokensToAdd, rule.Burst)
    
    if tokens <= 0 {
        return false, nil
    }
    
    // Consume token
    pipe = rrl.client.Pipeline()
    pipe.HSet(ctx, key, "tokens", tokens-1)
    pipe.HSet(ctx, key, "last_refill", now.Unix())
    pipe.Expire(ctx, key, rule.Window)
    _, err = pipe.Exec(ctx)
    
    return err == nil, err
}

func (rrl *RedisRateLimiter) Reset(ctx context.Context, key string, rule string) error {
    redisKey := fmt.Sprintf("rate_limit:%s:%s", rule, key)
    return rrl.client.Del(ctx, redisKey).Err()
}

func (rrl *RedisRateLimiter) GetRemaining(ctx context.Context, key string, rule string) (int, error) {
    ruleConfig, exists := rrl.rules[rule]
    if !exists {
        return 0, fmt.Errorf("rule not found: %s", rule)
    }
    
    redisKey := fmt.Sprintf("rate_limit:%s:%s", rule, key)
    
    // Get current count based on strategy
    var count int
    var err error
    
    switch ruleConfig.Strategy {
    case "fixed_window":
        count, err = rrl.client.Get(ctx, redisKey).Int()
    case "sliding_window":
        count, err = rrl.client.ZCard(ctx, redisKey).Result()
    case "token_bucket":
        tokens, err := rrl.client.HGet(ctx, redisKey, "tokens").Int()
        if err != nil {
            count = 0
        } else {
            count = tokens
        }
    }
    
    if err != nil && err != redis.Nil {
        return 0, err
    }
    
    return max(0, ruleConfig.Limit-count), nil
}

// Memory-based rate limiter
type MemoryRateLimiter struct {
    limiters map[string]*rate.Limiter
    mu       sync.RWMutex
    rules    map[string]RateLimitRule
}

func NewMemoryRateLimiter(rules map[string]RateLimitRule) *MemoryRateLimiter {
    return &MemoryRateLimiter{
        limiters: make(map[string]*rate.Limiter),
        rules:    rules,
    }
}

func (mrl *MemoryRateLimiter) Allow(ctx context.Context, key string, rule string) (bool, error) {
    ruleConfig, exists := mrl.rules[rule]
    if !exists {
        return true, nil
    }
    
    limiterKey := fmt.Sprintf("%s:%s", rule, key)
    
    mrl.mu.Lock()
    limiter, exists := mrl.limiters[limiterKey]
    if !exists {
        // Create new limiter
        limit := rate.Every(ruleConfig.Window / time.Duration(ruleConfig.Limit))
        limiter = rate.NewLimiter(limit, ruleConfig.Burst)
        mrl.limiters[limiterKey] = limiter
    }
    mrl.mu.Unlock()
    
    return limiter.Allow(), nil
}

func (mrl *MemoryRateLimiter) Reset(ctx context.Context, key string, rule string) error {
    limiterKey := fmt.Sprintf("%s:%s", rule, key)
    
    mrl.mu.Lock()
    delete(mrl.limiters, limiterKey)
    mrl.mu.Unlock()
    
    return nil
}

func (mrl *MemoryRateLimiter) GetRemaining(ctx context.Context, key string, rule string) (int, error) {
    // Memory limiter doesn't track remaining tokens
    return 0, fmt.Errorf("not implemented for memory limiter")
}

// Helper functions
func min(a, b int) int {
    if a < b {
        return a
    }
    return b
}

func max(a, b int) int {
    if a > b {
        return a
    }
    return b
}
```

## 🔄 Advanced Rate Limiting Patterns

### Dynamic Rate Limiting
```tsk
# Dynamic rate limiting based on conditions
dynamic_limits: #rate_limit("""
    adaptive_api -> Adaptive API rate limiting
        base_limit: 100/minute
        premium_limit: 1000/minute
        key: "user_id"
        condition: "user.tier == 'premium'"
        strategy: "sliding_window"
    
    load_based -> Load-based rate limiting
        normal_limit: 100/minute
        high_load_limit: 50/minute
        key: "ip_address"
        condition: "system_load > 80"
        strategy: "token_bucket"
    
    time_based -> Time-based rate limiting
        peak_limit: 50/minute
        off_peak_limit: 200/minute
        key: "ip_address"
        condition: "hour >= 9 && hour <= 17"
        strategy: "fixed_window"
""")
```

### Multi-Level Rate Limiting
```tsk
# Multi-level rate limiting
multi_level: #rate_limit("""
    global -> Global rate limiting
        limit: 10000/minute
        key: "global"
        strategy: "sliding_window"
        storage: "redis"
    
    user -> User-level rate limiting
        limit: 1000/minute
        key: "user_id"
        strategy: "token_bucket"
        storage: "redis"
        parent: "global"
    
    endpoint -> Endpoint-level rate limiting
        limit: 100/minute
        key: "user_id:endpoint"
        strategy: "fixed_window"
        storage: "redis"
        parent: "user"
""")
```

### Rate Limit Bypass
```tsk
# Rate limit bypass rules
bypass_rules: #rate_limit("""
    whitelist -> Whitelisted IPs
        ips: ["127.0.0.1", "192.168.1.0/24"]
        bypass: true
        reason: "Internal network"
    
    admin_bypass -> Admin user bypass
        condition: "user.role == 'admin'"
        bypass: true
        reason: "Administrator access"
    
    emergency_bypass -> Emergency bypass
        condition: "emergency_mode == true"
        bypass: true
        reason: "Emergency mode"
        log: true
""")
```

## 🛡️ Security Features

### Rate Limit Security Configuration
```tsk
# Rate limit security configuration
rate_limit_security: #rate_limit("""
    ip_whitelist -> IP whitelist
        ips: ["127.0.0.1", "10.0.0.0/8"]
        bypass: true
        reason: "Trusted network"
    
    user_whitelist -> User whitelist
        users: ["admin", "system"]
        bypass: true
        reason: "System users"
    
    rate_limit_evasion -> Prevent rate limit evasion
        fingerprinting: true
        user_agent_tracking: true
        cookie_tracking: true
        ip_rotation_detection: true
""")
```

### Go Security Implementation
```go
package security

import (
    "net"
    "net/http"
    "strings"
)

// IP whitelist checker
type IPWhitelist struct {
    networks []*net.IPNet
}

func NewIPWhitelist(ips []string) (*IPWhitelist, error) {
    var networks []*net.IPNet
    
    for _, ip := range ips {
        if strings.Contains(ip, "/") {
            // CIDR notation
            _, network, err := net.ParseCIDR(ip)
            if err != nil {
                return nil, fmt.Errorf("invalid CIDR: %s", ip)
            }
            networks = append(networks, network)
        } else {
            // Single IP
            parsedIP := net.ParseIP(ip)
            if parsedIP == nil {
                return nil, fmt.Errorf("invalid IP: %s", ip)
            }
            
            // Create /32 network for single IP
            network := &net.IPNet{
                IP:   parsedIP,
                Mask: net.CIDRMask(32, 32),
            }
            networks = append(networks, network)
        }
    }
    
    return &IPWhitelist{networks: networks}, nil
}

func (iw *IPWhitelist) IsWhitelisted(ip string) bool {
    parsedIP := net.ParseIP(ip)
    if parsedIP == nil {
        return false
    }
    
    for _, network := range iw.networks {
        if network.Contains(parsedIP) {
            return true
        }
    }
    
    return false
}

// Rate limit evasion detection
type EvasionDetector struct {
    fingerprints map[string]int
    mu           sync.RWMutex
}

func NewEvasionDetector() *EvasionDetector {
    return &EvasionDetector{
        fingerprints: make(map[string]int),
    }
}

func (ed *EvasionDetector) GenerateFingerprint(r *http.Request) string {
    // Create fingerprint from multiple factors
    factors := []string{
        r.Header.Get("User-Agent"),
        r.Header.Get("Accept-Language"),
        r.Header.Get("Accept-Encoding"),
        r.RemoteAddr,
    }
    
    // Add cookie-based fingerprint
    if cookie := r.Header.Get("Cookie"); cookie != "" {
        factors = append(factors, cookie)
    }
    
    // Create hash of factors
    hash := sha256.Sum256([]byte(strings.Join(factors, "|")))
    return hex.EncodeToString(hash[:])
}

func (ed *EvasionDetector) DetectEvasion(r *http.Request) bool {
    fingerprint := ed.GenerateFingerprint(r)
    
    ed.mu.Lock()
    count := ed.fingerprints[fingerprint]
    ed.fingerprints[fingerprint] = count + 1
    ed.mu.Unlock()
    
    // Detect suspicious patterns
    if count > 10 {
        return true // Too many requests with same fingerprint
    }
    
    // Check for IP rotation
    if ed.detectIPRotation(r) {
        return true
    }
    
    return false
}

func (ed *EvasionDetector) detectIPRotation(r *http.Request) bool {
    // Implementation depends on your IP tracking logic
    // This is a simplified example
    return false
}
```

## ⚡ Performance Optimization

### Rate Limit Performance Configuration
```tsk
# Rate limit performance configuration
rate_limit_performance: #rate_limit("""
    connection_pooling -> Connection pooling
        max_connections: 100
        idle_connections: 10
        connection_timeout: 30s
        read_timeout: 5s
        write_timeout: 5s
    
    caching -> Rate limit result caching
        enabled: true
        ttl: 1m
        cache_key: "rate_limit_result:{key}:{rule}"
    
    batching -> Batch operations
        enabled: true
        batch_size: 100
        batch_timeout: 10ms
    
    monitoring -> Rate limit monitoring
        metrics_enabled: true
        hit_rate_threshold: 0.1
        alert_on_high_rate: true
        slow_query_threshold: 10ms
""")
```

### Go Performance Implementation
```go
package performance

import (
    "context"
    "sync"
    "time"
)

// Cached rate limiter
type CachedRateLimiter struct {
    limiter RateLimiter
    cache   map[string]cacheEntry
    mu      sync.RWMutex
    ttl     time.Duration
}

type cacheEntry struct {
    allowed   bool
    remaining int
    expiresAt time.Time
}

func NewCachedRateLimiter(limiter RateLimiter, ttl time.Duration) *CachedRateLimiter {
    crl := &CachedRateLimiter{
        limiter: limiter,
        cache:   make(map[string]cacheEntry),
        ttl:     ttl,
    }
    
    // Start cleanup goroutine
    go crl.cleanup()
    
    return crl
}

func (crl *CachedRateLimiter) Allow(ctx context.Context, key string, rule string) (bool, error) {
    cacheKey := fmt.Sprintf("%s:%s", key, rule)
    
    // Check cache first
    crl.mu.RLock()
    if entry, exists := crl.cache[cacheKey]; exists && time.Now().Before(entry.expiresAt) {
        crl.mu.RUnlock()
        return entry.allowed, nil
    }
    crl.mu.RUnlock()
    
    // Check with underlying limiter
    allowed, err := crl.limiter.Allow(ctx, key, rule)
    if err != nil {
        return false, err
    }
    
    // Cache result
    crl.mu.Lock()
    crl.cache[cacheKey] = cacheEntry{
        allowed:   allowed,
        expiresAt: time.Now().Add(crl.ttl),
    }
    crl.mu.Unlock()
    
    return allowed, nil
}

func (crl *CachedRateLimiter) cleanup() {
    ticker := time.NewTicker(crl.ttl)
    defer ticker.Stop()
    
    for range ticker.C {
        crl.mu.Lock()
        now := time.Now()
        for key, entry := range crl.cache {
            if now.After(entry.expiresAt) {
                delete(crl.cache, key)
            }
        }
        crl.mu.Unlock()
    }
}

// Batch rate limiter
type BatchRateLimiter struct {
    limiter RateLimiter
    batch   chan batchRequest
    workers int
}

type batchRequest struct {
    key     string
    rule    string
    result  chan batchResult
}

type batchResult struct {
    allowed bool
    err     error
}

func NewBatchRateLimiter(limiter RateLimiter, workers int) *BatchRateLimiter {
    brl := &BatchRateLimiter{
        limiter: limiter,
        batch:   make(chan batchRequest, 1000),
        workers: workers,
    }
    
    // Start workers
    for i := 0; i < workers; i++ {
        go brl.worker()
    }
    
    return brl
}

func (brl *BatchRateLimiter) Allow(ctx context.Context, key string, rule string) (bool, error) {
    resultChan := make(chan batchResult, 1)
    
    select {
    case brl.batch <- batchRequest{key: key, rule: rule, result: resultChan}:
        select {
        case result := <-resultChan:
            return result.allowed, result.err
        case <-ctx.Done():
            return false, ctx.Err()
        }
    default:
        // Fallback to direct call if batch is full
        return brl.limiter.Allow(ctx, key, rule)
    }
}

func (brl *BatchRateLimiter) worker() {
    for req := range brl.batch {
        allowed, err := brl.limiter.Allow(context.Background(), req.key, req.rule)
        req.result <- batchResult{allowed: allowed, err: err}
    }
}
```

## 🔧 Error Handling

### Rate Limit Error Configuration
```tsk
# Rate limit error handling configuration
rate_limit_errors: #rate_limit("""
    connection_error -> Rate limit storage connection error
        retry_attempts: 3
        retry_delay: 1s
        fallback: "allow"
        log_level: error
    
    timeout_error -> Rate limit timeout error
        timeout: 5s
        retry_attempts: 2
        fallback: "allow"
        log_level: warn
    
    storage_error -> Rate limit storage error
        fallback: "memory"
        log_level: error
        alert: true
    
    bypass_on_error -> Bypass rate limiting on error
        enabled: true
        log_level: warn
        alert: true
""")
```

### Go Error Handler Implementation
```go
package errors

import (
    "context"
    "log"
    "time"
)

// Rate limit error types
type RateLimitError struct {
    Type    string `json:"type"`
    Message string `json:"message"`
    Key     string `json:"key,omitempty"`
    Rule    string `json:"rule,omitempty"`
    Retries int    `json:"retries,omitempty"`
}

// Rate limit error handlers
func HandleRateLimitError(err error, key string, rule string, retries int) {
    rateLimitError := RateLimitError{
        Type:    "rate_limit_error",
        Message: err.Error(),
        Key:     key,
        Rule:    rule,
        Retries: retries,
    }
    
    log.Printf("Rate limit error: %+v", rateLimitError)
    
    // Record metrics
    recordRateLimitError(rateLimitError)
    
    // Alert if too many retries
    if retries > 3 {
        sendRateLimitAlert(rateLimitError)
    }
}

// Fallback rate limiter
type FallbackRateLimiter struct {
    primary  RateLimiter
    fallback RateLimiter
}

func NewFallbackRateLimiter(primary, fallback RateLimiter) *FallbackRateLimiter {
    return &FallbackRateLimiter{
        primary:  primary,
        fallback: fallback,
    }
}

func (frl *FallbackRateLimiter) Allow(ctx context.Context, key string, rule string) (bool, error) {
    // Try primary limiter
    allowed, err := frl.primary.Allow(ctx, key, rule)
    if err == nil {
        return allowed, nil
    }
    
    // Log error and use fallback
    log.Printf("Primary rate limiter failed, using fallback: %v", err)
    
    // Try fallback limiter
    allowed, err = frl.fallback.Allow(ctx, key, rule)
    if err != nil {
        // If fallback also fails, allow request (fail open)
        log.Printf("Fallback rate limiter also failed, allowing request: %v", err)
        return true, nil
    }
    
    return allowed, nil
}

func (frl *FallbackRateLimiter) Reset(ctx context.Context, key string, rule string) error {
    // Reset both limiters
    frl.primary.Reset(ctx, key, rule)
    frl.fallback.Reset(ctx, key, rule)
    return nil
}

func (frl *FallbackRateLimiter) GetRemaining(ctx context.Context, key string, rule string) (int, error) {
    // Try primary first
    remaining, err := frl.primary.GetRemaining(ctx, key, rule)
    if err == nil {
        return remaining, nil
    }
    
    // Use fallback
    return frl.fallback.GetRemaining(ctx, key, rule)
}
```

## 🎯 Real-World Example

### Complete Rate Limit Configuration
```tsk
# rate-limit-config.tsk - Complete rate limit configuration

# Environment configuration
environment: #env("ENVIRONMENT", "development")
debug_mode: #env("DEBUG", "false")

# Rate limit rules
rules: #rate_limit("""
    # API rate limiting
    api_requests -> API request limiting
        limit: 100/minute
        burst: 20
        key: "ip_address"
        storage: "redis"
        strategy: "sliding_window"
        action: "throttle"
    
    # Authentication rate limiting
    login_attempts -> Login attempt limiting
        limit: 5/minute
        burst: 1
        key: "ip_address"
        storage: "redis"
        strategy: "fixed_window"
        action: "block_15m"
    
    # User action rate limiting
    user_actions -> User action limiting
        limit: 1000/hour
        burst: 100
        key: "user_id"
        storage: "redis"
        strategy: "token_bucket"
        action: "throttle"
        condition: "user.authenticated"
    
    # Admin action rate limiting
    admin_actions -> Admin action limiting
        limit: 10000/hour
        burst: 1000
        key: "user_id"
        storage: "redis"
        strategy: "sliding_window"
        action: "throttle"
        condition: "user.role == 'admin'"
    
    # File upload rate limiting
    file_uploads -> File upload limiting
        limit: 10/hour
        burst: 2
        key: "user_id"
        storage: "redis"
        strategy: "fixed_window"
        action: "block_1h"
    
    # Search rate limiting
    search_requests -> Search request limiting
        limit: 50/minute
        burst: 10
        key: "user_id"
        storage: "redis"
        strategy: "sliding_window"
        action: "throttle"
""")

# Storage configuration
storage: #rate_limit("""
    redis -> Redis rate limit storage
        url: #env("REDIS_URL")
        db: 1
        prefix: "rate_limit:"
        ttl: 1h
        pool_size: 10
        max_retries: 3
        connection_timeout: 30s
        read_timeout: 5s
        write_timeout: 5s
    
    memory -> In-memory rate limit storage
        max_entries: 10000
        cleanup_interval: 5m
        eviction_policy: "lru"
        max_memory: 100MB
""")

# Actions configuration
actions: #rate_limit("""
    throttle -> Throttle requests
        action: "throttle"
        delay: 1s
        message: "Request throttled, please wait"
        headers: ["Retry-After: 60"]
    
    block_15m -> Block for 15 minutes
        action: "block"
        duration: 15m
        message: "Too many requests, try again in 15 minutes"
        headers: ["Retry-After: 900"]
    
    block_1h -> Block for 1 hour
        action: "block"
        duration: 1h
        message: "Rate limit exceeded, try again in 1 hour"
        headers: ["Retry-After: 3600"]
    
    challenge -> Require CAPTCHA
        action: "challenge"
        type: "captcha"
        message: "Please complete CAPTCHA to continue"
        headers: ["X-RateLimit-Challenge: captcha"]
""")

# Bypass rules
bypass: #rate_limit("""
    whitelist -> Whitelisted IPs
        ips: ["127.0.0.1", "192.168.1.0/24", "10.0.0.0/8"]
        bypass: true
        reason: "Trusted network"
        log: true
    
    admin_bypass -> Admin user bypass
        condition: "user.role == 'admin'"
        bypass: true
        reason: "Administrator access"
        log: true
    
    emergency_bypass -> Emergency bypass
        condition: "emergency_mode == true"
        bypass: true
        reason: "Emergency mode"
        log: true
        alert: true
""")

# Performance configuration
performance: #rate_limit("""
    connection_pooling -> Connection pooling
        max_connections: 100
        idle_connections: 10
        connection_timeout: 30s
        read_timeout: 5s
        write_timeout: 5s
    
    caching -> Rate limit result caching
        enabled: true
        ttl: 1m
        cache_key: "rate_limit_result:{key}:{rule}"
        max_entries: 10000
    
    batching -> Batch operations
        enabled: true
        batch_size: 100
        batch_timeout: 10ms
        workers: 5
    
    monitoring -> Rate limit monitoring
        metrics_enabled: true
        hit_rate_threshold: 0.1
        alert_on_high_rate: true
        slow_query_threshold: 10ms
        memory_usage_alert: true
        memory_threshold: 80%
""")

# Error handling
error_handling: #rate_limit("""
    connection_error -> Rate limit storage connection error
        retry_attempts: 3
        retry_delay: 1s
        exponential_backoff: true
        fallback: "allow"
        log_level: error
        alert: true
    
    timeout_error -> Rate limit timeout error
        timeout: 5s
        retry_attempts: 2
        fallback: "allow"
        log_level: warn
        alert: false
    
    storage_error -> Rate limit storage error
        fallback: "memory"
        log_level: error
        alert: true
        bypass_on_error: true
    
    bypass_on_error -> Bypass rate limiting on error
        enabled: true
        log_level: warn
        alert: true
        duration: 5m
""")
```

### Go Rate Limit Application Implementation
```go
package main

import (
    "fmt"
    "log"
    "net/http"
    "github.com/tusklang/go-sdk"
    "github.com/gorilla/mux"
)

type RateLimitConfig struct {
    Environment   string `tsk:"environment"`
    DebugMode     bool   `tsk:"debug_mode"`
    Rules         string `tsk:"rules"`
    Storage       string `tsk:"storage"`
    Actions       string `tsk:"actions"`
    Bypass        string `tsk:"bypass"`
    Performance   string `tsk:"performance"`
    ErrorHandling string `tsk:"error_handling"`
}

func main() {
    // Load rate limit configuration
    config := tusk.LoadConfig("rate-limit-config.tsk")
    
    var rateLimitConfig RateLimitConfig
    if err := config.Unmarshal(&rateLimitConfig); err != nil {
        log.Fatal("Failed to load rate limit config:", err)
    }
    
    // Create rate limit system from directives
    rateLimiter := tusk.NewRateLimitSystem(rateLimitConfig)
    
    // Create router
    router := mux.NewRouter()
    
    // Apply rate limit middleware
    tusk.ApplyRateLimitMiddleware(router, rateLimiter)
    
    // Setup routes
    setupRoutes(router, rateLimiter)
    
    // Start server
    addr := fmt.Sprintf(":%s", #env("PORT", "8080"))
    log.Printf("Starting rate limit server on %s in %s mode", addr, rateLimitConfig.Environment)
    
    if err := http.ListenAndServe(addr, router); err != nil {
        log.Fatal("Rate limit server failed:", err)
    }
}

func setupRoutes(router *mux.Router, rateLimiter *tusk.RateLimitSystem) {
    // Health check
    router.HandleFunc("/health", healthHandler).Methods("GET")
    router.HandleFunc("/metrics", metricsHandler).Methods("GET")
    
    // Rate limit management endpoints
    rateLimitRouter := router.PathPrefix("/rate-limit").Subrouter()
    rateLimitRouter.Use(authMiddleware)
    
    rateLimitRouter.HandleFunc("/reset", rateLimiter.ResetHandler).Methods("POST")
    rateLimitRouter.HandleFunc("/status", rateLimiter.StatusHandler).Methods("GET")
    rateLimitRouter.HandleFunc("/stats", rateLimiter.StatsHandler).Methods("GET")
    
    // API routes with rate limiting
    api := router.PathPrefix("/api").Subrouter()
    
    // Apply rate limiting to all API routes
    api.Use(rateLimiter.Middleware("api_requests"))
    
    // User routes with specific rate limits
    api.HandleFunc("/login", rateLimiter.WrapHandler(loginHandler, "login_attempts")).Methods("POST")
    api.HandleFunc("/users", rateLimiter.WrapHandler(usersHandler, "user_actions")).Methods("GET", "POST")
    api.HandleFunc("/users/{id}", rateLimiter.WrapHandler(userHandler, "user_actions")).Methods("GET", "PUT", "DELETE")
    
    // Admin routes with admin rate limits
    admin := api.PathPrefix("/admin").Subrouter()
    admin.Use(rateLimiter.Middleware("admin_actions"))
    admin.Use(authMiddleware)
    admin.Use(adminAuthMiddleware)
    
    admin.HandleFunc("/stats", statsHandler).Methods("GET")
    admin.HandleFunc("/config", configHandler).Methods("GET", "PUT")
    
    // File upload routes with upload rate limits
    api.HandleFunc("/upload", rateLimiter.WrapHandler(uploadHandler, "file_uploads")).Methods("POST")
    
    // Search routes with search rate limits
    api.HandleFunc("/search", rateLimiter.WrapHandler(searchHandler, "search_requests")).Methods("GET")
}
```

## 🎯 Best Practices

### 1. **Use Appropriate Rate Limit Strategies**
```tsk
# Choose the right rate limit strategy for each use case
strategy_selection: #rate_limit("""
    # Fixed window for simple cases
    simple_limit -> 100/minute -> fixed_window
    
    # Sliding window for smooth limiting
    smooth_limit -> 100/minute -> sliding_window
    
    # Token bucket for burst handling
    burst_limit -> 100/minute -> token_bucket
""")
```

### 2. **Implement Proper Error Handling**
```go
// Comprehensive rate limit error handling
func handleRateLimitError(err error, key string, rule string) {
    log.Printf("Rate limit error for key %s, rule %s: %v", key, rule, err)
    
    // Record metrics
    recordRateLimitError(key, rule, err)
    
    // Use fallback if available
    if fallbackLimiter != nil {
        log.Printf("Using fallback rate limiter for key %s", key)
        // Implement fallback logic
    }
    
    // Alert on critical errors
    if isCriticalRateLimitError(err) {
        sendRateLimitAlert(key, rule, err)
    }
}
```

### 3. **Use Environment-Specific Configuration**
```tsk
# Different rate limits for different environments
environment_limits: #if(
    #env("ENVIRONMENT") == "production",
    #rate_limit("""
        api_requests: 100/minute
        login_attempts: 5/minute
        user_actions: 1000/hour
    """),
    #rate_limit("""
        api_requests: 1000/minute
        login_attempts: 50/minute
        user_actions: 10000/hour
    """)
)
```

### 4. **Monitor Rate Limit Performance**
```go
// Rate limit performance monitoring
func monitorRateLimitPerformance(rule string, key string, allowed bool, startTime time.Time) {
    duration := time.Since(startTime)
    
    // Record metrics
    metrics := map[string]interface{}{
        "rule":     rule,
        "key":      key,
        "allowed":  allowed,
        "duration": duration.Milliseconds(),
        "timestamp": time.Now(),
    }
    
    if err := recordRateLimitMetrics(metrics); err != nil {
        log.Printf("Failed to record rate limit metrics: %v", err)
    }
    
    // Alert on high rejection rates
    if !allowed && getRejectionRate(rule) > 0.1 {
        log.Printf("High rejection rate for rule %s: %.2f%%", rule, getRejectionRate(rule)*100)
    }
    
    // Alert on slow rate limiting
    if duration > 10*time.Millisecond {
        log.Printf("Slow rate limiting for rule %s: %v", rule, duration)
    }
}
```

## 🎯 Summary

Rate limit directives in TuskLang provide a powerful, declarative way to define rate limiting systems. They enable:

- **Declarative rate limit configuration** that is easy to understand and maintain
- **Flexible rate limiting strategies** including fixed window, sliding window, and token bucket
- **Comprehensive storage options** including Redis and memory-based storage
- **Built-in security features** including IP whitelisting and evasion detection
- **Performance optimization** with caching, batching, and connection pooling

The Go SDK seamlessly integrates rate limit directives with existing Go rate limiting libraries, making them feel like native Go features while providing the power and flexibility of TuskLang's directive system.

**Next**: Explore monitoring directives, deployment directives, and other specialized directive types in the following guides. 