# Cache Directives - Go

## 🎯 What Are Cache Directives?

Cache directives (`#cache`) in TuskLang allow you to define caching strategies, TTL configuration, invalidation rules, and storage backends directly in your configuration files. They transform static config into executable caching logic.

```go
// Cache directives define your entire caching system
type CacheConfig struct {
    Strategies  map[string]string `tsk:"#cache_strategies"`
    TTL         map[string]string `tsk:"#cache_ttl"`
    Invalidation map[string]string `tsk:"#cache_invalidation"`
    Storage     map[string]string `tsk:"#cache_storage"`
}
```

## 🚀 Why Cache Directives Matter

### Traditional Caching Development
```go
// Old way - scattered across multiple files
func main() {
    // Cache configuration scattered
    redisClient := redis.NewClient(&redis.Options{
        Addr: os.Getenv("REDIS_ADDR"),
        DB:   0,
    })
    
    // Cache logic defined in code
    cache := cache.New(redisClient)
    
    // Manual cache operations
    func getUser(id string) (*User, error) {
        // Check cache
        if cached, err := cache.Get("user:" + id); err == nil {
            return cached.(*User), nil
        }
        
        // Fetch from database
        user, err := db.GetUser(id)
        if err != nil {
            return nil, err
        }
        
        // Store in cache
        cache.Set("user:"+id, user, 30*time.Minute)
        return user, nil
    }
}
```

### TuskLang Cache Directives - Declarative & Dynamic
```tsk
# cache.tsk - Complete cache definition
cache_strategies: #cache("""
    user_cache -> User data caching
        ttl: 30m
        key_pattern: "user:{id}"
        storage: "redis"
        invalidation: "on_update"
    
    post_cache -> Post data caching
        ttl: 1h
        key_pattern: "post:{id}"
        storage: "redis"
        invalidation: "on_update"
        compression: true
    
    api_cache -> API response caching
        ttl: 5m
        key_pattern: "api:{method}:{path}:{params}"
        storage: "memory"
        invalidation: "time_based"
        headers: ["ETag", "Last-Modified"]
    
    session_cache -> Session data caching
        ttl: 24h
        key_pattern: "session:{id}"
        storage: "redis"
        invalidation: "on_logout"
        security: "encrypted"
""")

cache_storage: #cache("""
    redis -> Redis cache storage
        url: #env("REDIS_URL")
        db: 0
        pool_size: 10
        max_retries: 3
    
    memory -> In-memory cache storage
        max_size: 1000
        cleanup_interval: 5m
        eviction_policy: "lru"
    
    file -> File-based cache storage
        path: "/tmp/cache"
        max_size: 100MB
        compression: true
""")

cache_invalidation: #cache("""
    on_update -> Invalidate on data update
        events: ["user_updated", "post_updated"]
        patterns: ["user:{id}", "post:{id}"]
    
    time_based -> Time-based invalidation
        interval: 1h
        patterns: ["api:*"]
    
    manual -> Manual invalidation
        endpoints: ["/cache/clear", "/cache/invalidate"]
        patterns: ["*"]
""")
```

## 📋 Cache Directive Types

### 1. **Strategy Directives** (`#cache_strategies`)
- Cache strategy definitions
- TTL configuration
- Key patterns
- Storage backends

### 2. **TTL Directives** (`#cache_ttl`)
- Time-to-live configuration
- Dynamic TTL based on data
- TTL inheritance
- TTL overrides

### 3. **Invalidation Directives** (`#cache_invalidation`)
- Cache invalidation rules
- Event-based invalidation
- Pattern-based invalidation
- Manual invalidation

### 4. **Storage Directives** (`#cache_storage`)
- Storage backend configuration
- Connection pooling
- Performance settings
- Security features

## 🔧 Basic Cache Directive Syntax

### Simple Cache Strategy
```tsk
# Basic cache strategy
user_cache: #cache("user:{id} -> 30m")
```

### Cache Strategy with Configuration
```tsk
# Cache strategy with detailed configuration
post_cache: #cache("""
    key_pattern: "post:{id}"
    ttl: 1h
    storage: "redis"
    compression: true
    invalidation: "on_update"
""")
```

### Multiple Cache Strategies
```tsk
# Multiple cache strategies
cache_strategies: #cache("""
    users -> "user:{id}" -> 30m -> redis
    posts -> "post:{id}" -> 1h -> redis
    api -> "api:{method}:{path}" -> 5m -> memory
    sessions -> "session:{id}" -> 24h -> redis
""")
```

## 🎯 Go Integration Patterns

### Struct Tags for Cache Directives
```go
type CacheConfig struct {
    // Cache strategies
    Strategies string `tsk:"#cache_strategies"`
    
    // TTL configuration
    TTL string `tsk:"#cache_ttl"`
    
    // Invalidation rules
    Invalidation string `tsk:"#cache_invalidation"`
    
    // Storage configuration
    Storage string `tsk:"#cache_storage"`
}
```

### Cache Application Setup
```go
package main

import (
    "github.com/tusklang/go-sdk"
    "github.com/gorilla/mux"
    "net/http"
)

func main() {
    // Load cache configuration
    config := tusk.LoadConfig("cache.tsk")
    
    var cacheConfig CacheConfig
    config.Unmarshal(&cacheConfig)
    
    // Create cache system from directives
    cache := tusk.NewCacheSystem(cacheConfig)
    
    // Create router
    router := mux.NewRouter()
    
    // Apply cache middleware
    tusk.ApplyCacheMiddleware(router, cache)
    
    // Start server
    http.ListenAndServe(":8080", router)
}
```

### Cache Handler Implementation
```go
package cache

import (
    "context"
    "encoding/json"
    "fmt"
    "time"
    "github.com/go-redis/redis/v8"
)

// Cache interface
type Cache interface {
    Get(ctx context.Context, key string) (interface{}, error)
    Set(ctx context.Context, key string, value interface{}, ttl time.Duration) error
    Delete(ctx context.Context, key string) error
    Clear(ctx context.Context, pattern string) error
}

// Redis cache implementation
type RedisCache struct {
    client *redis.Client
}

func NewRedisCache(url string) (*RedisCache, error) {
    client := redis.NewClient(&redis.Options{
        Addr: url,
        DB:   0,
    })
    
    // Test connection
    ctx := context.Background()
    if err := client.Ping(ctx).Err(); err != nil {
        return nil, fmt.Errorf("failed to connect to Redis: %v", err)
    }
    
    return &RedisCache{client: client}, nil
}

func (rc *RedisCache) Get(ctx context.Context, key string) (interface{}, error) {
    data, err := rc.client.Get(ctx, key).Bytes()
    if err != nil {
        if err == redis.Nil {
            return nil, ErrCacheMiss
        }
        return nil, err
    }
    
    // Deserialize data
    var value interface{}
    if err := json.Unmarshal(data, &value); err != nil {
        return nil, fmt.Errorf("failed to deserialize cached data: %v", err)
    }
    
    return value, nil
}

func (rc *RedisCache) Set(ctx context.Context, key string, value interface{}, ttl time.Duration) error {
    // Serialize data
    data, err := json.Marshal(value)
    if err != nil {
        return fmt.Errorf("failed to serialize data for caching: %v", err)
    }
    
    // Store in Redis
    return rc.client.Set(ctx, key, data, ttl).Err()
}

func (rc *RedisCache) Delete(ctx context.Context, key string) error {
    return rc.client.Del(ctx, key).Err()
}

func (rc *RedisCache) Clear(ctx context.Context, pattern string) error {
    keys, err := rc.client.Keys(ctx, pattern).Result()
    if err != nil {
        return err
    }
    
    if len(keys) > 0 {
        return rc.client.Del(ctx, keys...).Err()
    }
    
    return nil
}

// Memory cache implementation
type MemoryCache struct {
    store map[string]cacheEntry
    mu    sync.RWMutex
    ttl   time.Duration
}

type cacheEntry struct {
    value      interface{}
    expiration time.Time
}

func NewMemoryCache(ttl time.Duration) *MemoryCache {
    mc := &MemoryCache{
        store: make(map[string]cacheEntry),
        ttl:   ttl,
    }
    
    // Start cleanup goroutine
    go mc.cleanup()
    
    return mc
}

func (mc *MemoryCache) Get(ctx context.Context, key string) (interface{}, error) {
    mc.mu.RLock()
    defer mc.mu.RUnlock()
    
    entry, exists := mc.store[key]
    if !exists {
        return nil, ErrCacheMiss
    }
    
    if time.Now().After(entry.expiration) {
        delete(mc.store, key)
        return nil, ErrCacheMiss
    }
    
    return entry.value, nil
}

func (mc *MemoryCache) Set(ctx context.Context, key string, value interface{}, ttl time.Duration) error {
    mc.mu.Lock()
    defer mc.mu.Unlock()
    
    expiration := time.Now().Add(ttl)
    mc.store[key] = cacheEntry{
        value:      value,
        expiration: expiration,
    }
    
    return nil
}

func (mc *MemoryCache) Delete(ctx context.Context, key string) error {
    mc.mu.Lock()
    defer mc.mu.Unlock()
    
    delete(mc.store, key)
    return nil
}

func (mc *MemoryCache) Clear(ctx context.Context, pattern string) error {
    mc.mu.Lock()
    defer mc.mu.Unlock()
    
    // Simple pattern matching for memory cache
    for key := range mc.store {
        if matchesPattern(key, pattern) {
            delete(mc.store, key)
        }
    }
    
    return nil
}

func (mc *MemoryCache) cleanup() {
    ticker := time.NewTicker(mc.ttl)
    defer ticker.Stop()
    
    for range ticker.C {
        mc.mu.Lock()
        now := time.Now()
        for key, entry := range mc.store {
            if now.After(entry.expiration) {
                delete(mc.store, key)
            }
        }
        mc.mu.Unlock()
    }
}

// Cache errors
var (
    ErrCacheMiss = fmt.Errorf("cache miss")
    ErrCacheFull = fmt.Errorf("cache full")
)
```

## 🔄 Advanced Caching Patterns

### Cache-Aside Pattern
```tsk
# Cache-aside pattern configuration
cache_aside: #cache("""
    user_cache -> Cache-aside for user data
        strategy: "cache_aside"
        ttl: 30m
        key_pattern: "user:{id}"
        storage: "redis"
        fallback: "database"
        invalidation: "on_update"
    
    post_cache -> Cache-aside for post data
        strategy: "cache_aside"
        ttl: 1h
        key_pattern: "post:{id}"
        storage: "redis"
        fallback: "database"
        invalidation: "on_update"
        compression: true
""")
```

### Write-Through Pattern
```tsk
# Write-through pattern configuration
write_through: #cache("""
    config_cache -> Write-through for configuration
        strategy: "write_through"
        ttl: 24h
        key_pattern: "config:{key}"
        storage: "redis"
        primary: "database"
        invalidation: "manual"
    
    settings_cache -> Write-through for user settings
        strategy: "write_through"
        ttl: 1h
        key_pattern: "settings:{user_id}"
        storage: "redis"
        primary: "database"
        invalidation: "on_update"
""")
```

### Cache-Only Pattern
```tsk
# Cache-only pattern configuration
cache_only: #cache("""
    session_cache -> Cache-only for sessions
        strategy: "cache_only"
        ttl: 24h
        key_pattern: "session:{id}"
        storage: "redis"
        invalidation: "on_logout"
        security: "encrypted"
    
    temp_cache -> Cache-only for temporary data
        strategy: "cache_only"
        ttl: 5m
        key_pattern: "temp:{id}"
        storage: "memory"
        invalidation: "time_based"
""")
```

## 🛡️ Cache Security

### Cache Security Configuration
```tsk
# Cache security configuration
cache_security: #cache("""
    encryption -> Cache data encryption
        algorithm: "AES-256-GCM"
        key: #env("CACHE_ENCRYPTION_KEY")
        enabled: true
    
    access_control -> Cache access control
        authentication: true
        authorization: true
        roles: ["admin", "cache_manager"]
    
    data_validation -> Cache data validation
        schema_validation: true
        size_limits: true
        max_size: 1MB
        allowed_types: ["string", "number", "object"]
""")
```

### Go Cache Security Implementation
```go
package security

import (
    "crypto/aes"
    "crypto/cipher"
    "crypto/rand"
    "encoding/base64"
    "fmt"
    "io"
)

// Encrypted cache wrapper
type EncryptedCache struct {
    cache Cache
    key   []byte
    gcm   cipher.AEAD
}

func NewEncryptedCache(cache Cache, key string) (*EncryptedCache, error) {
    // Decode key
    decodedKey, err := base64.StdEncoding.DecodeString(key)
    if err != nil {
        return nil, fmt.Errorf("invalid encryption key: %v", err)
    }
    
    // Create cipher
    block, err := aes.NewCipher(decodedKey)
    if err != nil {
        return nil, fmt.Errorf("failed to create cipher: %v", err)
    }
    
    // Create GCM
    gcm, err := cipher.NewGCM(block)
    if err != nil {
        return nil, fmt.Errorf("failed to create GCM: %v", err)
    }
    
    return &EncryptedCache{
        cache: cache,
        key:   decodedKey,
        gcm:   gcm,
    }, nil
}

func (ec *EncryptedCache) Get(ctx context.Context, key string) (interface{}, error) {
    // Get encrypted data
    encryptedData, err := ec.cache.Get(ctx, key)
    if err != nil {
        return nil, err
    }
    
    // Decrypt data
    data, err := ec.decrypt(encryptedData.(string))
    if err != nil {
        return nil, fmt.Errorf("failed to decrypt cached data: %v", err)
    }
    
    // Deserialize
    var value interface{}
    if err := json.Unmarshal(data, &value); err != nil {
        return nil, fmt.Errorf("failed to deserialize decrypted data: %v", err)
    }
    
    return value, nil
}

func (ec *EncryptedCache) Set(ctx context.Context, key string, value interface{}, ttl time.Duration) error {
    // Serialize data
    data, err := json.Marshal(value)
    if err != nil {
        return fmt.Errorf("failed to serialize data: %v", err)
    }
    
    // Encrypt data
    encryptedData, err := ec.encrypt(data)
    if err != nil {
        return fmt.Errorf("failed to encrypt data: %v", err)
    }
    
    // Store encrypted data
    return ec.cache.Set(ctx, key, encryptedData, ttl)
}

func (ec *EncryptedCache) Delete(ctx context.Context, key string) error {
    return ec.cache.Delete(ctx, key)
}

func (ec *EncryptedCache) Clear(ctx context.Context, pattern string) error {
    return ec.cache.Clear(ctx, pattern)
}

// Encrypt data
func (ec *EncryptedCache) encrypt(data []byte) (string, error) {
    nonce := make([]byte, ec.gcm.NonceSize())
    if _, err := io.ReadFull(rand.Reader, nonce); err != nil {
        return "", err
    }
    
    ciphertext := ec.gcm.Seal(nonce, nonce, data, nil)
    return base64.StdEncoding.EncodeToString(ciphertext), nil
}

// Decrypt data
func (ec *EncryptedCache) decrypt(encryptedData string) ([]byte, error) {
    ciphertext, err := base64.StdEncoding.DecodeString(encryptedData)
    if err != nil {
        return nil, err
    }
    
    nonceSize := ec.gcm.NonceSize()
    if len(ciphertext) < nonceSize {
        return nil, fmt.Errorf("ciphertext too short")
    }
    
    nonce, ciphertext := ciphertext[:nonceSize], ciphertext[nonceSize:]
    return ec.gcm.Open(nil, nonce, ciphertext, nil)
}
```

## ⚡ Performance Optimization

### Cache Performance Configuration
```tsk
# Cache performance configuration
cache_performance: #cache("""
    connection_pooling -> Connection pooling
        max_connections: 100
        idle_connections: 10
        connection_timeout: 30s
        read_timeout: 5s
        write_timeout: 5s
    
    compression -> Data compression
        enabled: true
        algorithm: "gzip"
        min_size: 1024
        compression_level: 6
    
    prefetching -> Cache prefetching
        enabled: true
        patterns: ["user:*", "post:*"]
        batch_size: 100
        interval: 5m
    
    monitoring -> Cache monitoring
        metrics_enabled: true
        hit_rate_threshold: 0.8
        miss_rate_alert: true
        slow_query_threshold: 100ms
""")
```

### Go Performance Implementation
```go
package performance

import (
    "compress/gzip"
    "bytes"
    "context"
    "io"
    "sync"
    "time"
)

// Compressed cache wrapper
type CompressedCache struct {
    cache Cache
    mu    sync.RWMutex
}

func NewCompressedCache(cache Cache) *CompressedCache {
    return &CompressedCache{cache: cache}
}

func (cc *CompressedCache) Get(ctx context.Context, key string) (interface{}, error) {
    // Get compressed data
    compressedData, err := cc.cache.Get(ctx, key)
    if err != nil {
        return nil, err
    }
    
    // Decompress data
    data, err := cc.decompress(compressedData.([]byte))
    if err != nil {
        return nil, fmt.Errorf("failed to decompress cached data: %v", err)
    }
    
    // Deserialize
    var value interface{}
    if err := json.Unmarshal(data, &value); err != nil {
        return nil, fmt.Errorf("failed to deserialize decompressed data: %v", err)
    }
    
    return value, nil
}

func (cc *CompressedCache) Set(ctx context.Context, key string, value interface{}, ttl time.Duration) error {
    // Serialize data
    data, err := json.Marshal(value)
    if err != nil {
        return fmt.Errorf("failed to serialize data: %v", err)
    }
    
    // Compress data if large enough
    if len(data) > 1024 {
        compressedData, err := cc.compress(data)
        if err != nil {
            return fmt.Errorf("failed to compress data: %v", err)
        }
        data = compressedData
    }
    
    // Store compressed data
    return cc.cache.Set(ctx, key, data, ttl)
}

func (cc *CompressedCache) Delete(ctx context.Context, key string) error {
    return cc.cache.Delete(ctx, key)
}

func (cc *CompressedCache) Clear(ctx context.Context, pattern string) error {
    return cc.cache.Clear(ctx, pattern)
}

// Compress data
func (cc *CompressedCache) compress(data []byte) ([]byte, error) {
    var buf bytes.Buffer
    gz := gzip.NewWriter(&buf)
    
    if _, err := gz.Write(data); err != nil {
        return nil, err
    }
    
    if err := gz.Close(); err != nil {
        return nil, err
    }
    
    return buf.Bytes(), nil
}

// Decompress data
func (cc *CompressedCache) decompress(data []byte) ([]byte, error) {
    gz, err := gzip.NewReader(bytes.NewReader(data))
    if err != nil {
        return nil, err
    }
    defer gz.Close()
    
    return io.ReadAll(gz)
}

// Cache prefetcher
type CachePrefetcher struct {
    cache Cache
    patterns []string
    interval time.Duration
    stopChan chan bool
}

func NewCachePrefetcher(cache Cache, patterns []string, interval time.Duration) *CachePrefetcher {
    return &CachePrefetcher{
        cache:    cache,
        patterns: patterns,
        interval: interval,
        stopChan: make(chan bool),
    }
}

func (cp *CachePrefetcher) Start() {
    ticker := time.NewTicker(cp.interval)
    defer ticker.Stop()
    
    for {
        select {
        case <-ticker.C:
            cp.prefetch()
        case <-cp.stopChan:
            return
        }
    }
}

func (cp *CachePrefetcher) Stop() {
    close(cp.stopChan)
}

func (cp *CachePrefetcher) prefetch() {
    // Implementation depends on your data source
    // This is a simplified example
    for _, pattern := range cp.patterns {
        // Fetch data that matches pattern
        // and store in cache
    }
}
```

## 🔧 Error Handling

### Cache Error Configuration
```tsk
# Cache error handling configuration
cache_errors: #cache("""
    connection_error -> Cache connection error
        retry_attempts: 3
        retry_delay: 1s
        fallback: "memory"
        log_level: error
    
    timeout_error -> Cache timeout error
        timeout: 5s
        retry_attempts: 2
        fallback: "database"
        log_level: warn
    
    storage_error -> Cache storage error
        max_size: 100MB
        eviction_policy: "lru"
        fallback: "memory"
        log_level: error
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

// Cache error types
type CacheError struct {
    Type    string `json:"type"`
    Message string `json:"message"`
    Key     string `json:"key,omitempty"`
    Retries int    `json:"retries,omitempty"`
}

// Cache error handlers
func HandleCacheError(err error, key string, retries int) {
    cacheError := CacheError{
        Type:    "cache_error",
        Message: err.Error(),
        Key:     key,
        Retries: retries,
    }
    
    log.Printf("Cache error: %+v", cacheError)
    
    // Record metrics
    recordCacheError(cacheError)
    
    // Alert if too many retries
    if retries > 3 {
        sendCacheAlert(cacheError)
    }
}

// Retry wrapper for cache operations
func RetryCacheOperation(operation func() error, maxRetries int, delay time.Duration) error {
    var lastErr error
    
    for attempt := 0; attempt <= maxRetries; attempt++ {
        if err := operation(); err == nil {
            return nil
        } else {
            lastErr = err
            log.Printf("Cache operation failed (attempt %d/%d): %v", attempt+1, maxRetries+1, err)
            
            if attempt < maxRetries {
                time.Sleep(delay)
            }
        }
    }
    
    return fmt.Errorf("cache operation failed after %d attempts: %v", maxRetries+1, lastErr)
}

// Fallback cache implementation
type FallbackCache struct {
    primary Cache
    fallback Cache
}

func NewFallbackCache(primary, fallback Cache) *FallbackCache {
    return &FallbackCache{
        primary:  primary,
        fallback: fallback,
    }
}

func (fc *FallbackCache) Get(ctx context.Context, key string) (interface{}, error) {
    // Try primary cache
    value, err := fc.primary.Get(ctx, key)
    if err == nil {
        return value, nil
    }
    
    // Try fallback cache
    value, err = fc.fallback.Get(ctx, key)
    if err == nil {
        // Store in primary cache for next time
        go func() {
            fc.primary.Set(context.Background(), key, value, 5*time.Minute)
        }()
        return value, nil
    }
    
    return nil, err
}

func (fc *FallbackCache) Set(ctx context.Context, key string, value interface{}, ttl time.Duration) error {
    // Set in both caches
    if err := fc.primary.Set(ctx, key, value, ttl); err != nil {
        log.Printf("Failed to set in primary cache: %v", err)
    }
    
    if err := fc.fallback.Set(ctx, key, value, ttl); err != nil {
        log.Printf("Failed to set in fallback cache: %v", err)
    }
    
    return nil
}

func (fc *FallbackCache) Delete(ctx context.Context, key string) error {
    // Delete from both caches
    fc.primary.Delete(ctx, key)
    fc.fallback.Delete(ctx, key)
    return nil
}

func (fc *FallbackCache) Clear(ctx context.Context, pattern string) error {
    // Clear both caches
    fc.primary.Clear(ctx, pattern)
    fc.fallback.Clear(ctx, pattern)
    return nil
}
```

## 🎯 Real-World Example

### Complete Cache Configuration
```tsk
# cache-config.tsk - Complete cache configuration

# Environment configuration
environment: #env("ENVIRONMENT", "development")
debug_mode: #env("DEBUG", "false")

# Cache strategies
strategies: #cache("""
    # User data caching
    user_cache -> User data caching
        ttl: 30m
        key_pattern: "user:{id}"
        storage: "redis"
        strategy: "cache_aside"
        invalidation: "on_update"
        compression: true
        encryption: true
    
    # Post data caching
    post_cache -> Post data caching
        ttl: 1h
        key_pattern: "post:{id}"
        storage: "redis"
        strategy: "cache_aside"
        invalidation: "on_update"
        compression: true
    
    # API response caching
    api_cache -> API response caching
        ttl: 5m
        key_pattern: "api:{method}:{path}:{params}"
        storage: "memory"
        strategy: "cache_only"
        invalidation: "time_based"
        headers: ["ETag", "Last-Modified"]
    
    # Session data caching
    session_cache -> Session data caching
        ttl: 24h
        key_pattern: "session:{id}"
        storage: "redis"
        strategy: "cache_only"
        invalidation: "on_logout"
        encryption: true
        security: "high"
    
    # Configuration caching
    config_cache -> Configuration caching
        ttl: 24h
        key_pattern: "config:{key}"
        storage: "redis"
        strategy: "write_through"
        invalidation: "manual"
        compression: true
""")

# Storage configuration
storage: #cache("""
    redis -> Redis cache storage
        url: #env("REDIS_URL")
        db: 0
        pool_size: 10
        max_retries: 3
        connection_timeout: 30s
        read_timeout: 5s
        write_timeout: 5s
    
    memory -> In-memory cache storage
        max_size: 1000
        cleanup_interval: 5m
        eviction_policy: "lru"
        max_memory: 100MB
    
    file -> File-based cache storage
        path: "/tmp/cache"
        max_size: 100MB
        compression: true
        cleanup_interval: 1h
""")

# Invalidation configuration
invalidation: #cache("""
    on_update -> Invalidate on data update
        events: ["user_updated", "post_updated", "config_updated"]
        patterns: ["user:{id}", "post:{id}", "config:{key}"]
        delay: 1s
    
    time_based -> Time-based invalidation
        interval: 1h
        patterns: ["api:*", "temp:*"]
        cleanup: true
    
    manual -> Manual invalidation
        endpoints: ["/cache/clear", "/cache/invalidate"]
        patterns: ["*"]
        authentication: true
        authorization: ["admin"]
""")

# Performance configuration
performance: #cache("""
    connection_pooling -> Connection pooling
        max_connections: 100
        idle_connections: 10
        connection_timeout: 30s
        read_timeout: 5s
        write_timeout: 5s
    
    compression -> Data compression
        enabled: true
        algorithm: "gzip"
        min_size: 1024
        compression_level: 6
        threshold: 0.1
    
    prefetching -> Cache prefetching
        enabled: true
        patterns: ["user:*", "post:*", "config:*"]
        batch_size: 100
        interval: 5m
        concurrency: 5
    
    monitoring -> Cache monitoring
        metrics_enabled: true
        hit_rate_threshold: 0.8
        miss_rate_alert: true
        slow_query_threshold: 100ms
        memory_usage_alert: true
        memory_threshold: 80%
""")

# Security configuration
security: #cache("""
    encryption -> Cache data encryption
        algorithm: "AES-256-GCM"
        key: #env("CACHE_ENCRYPTION_KEY")
        enabled: true
        patterns: ["session:*", "user:*"]
    
    access_control -> Cache access control
        authentication: true
        authorization: true
        roles: ["admin", "cache_manager"]
        endpoints: ["/cache/*"]
    
    data_validation -> Cache data validation
        schema_validation: true
        size_limits: true
        max_size: 1MB
        allowed_types: ["string", "number", "object", "array"]
        sanitization: true
""")

# Error handling
error_handling: #cache("""
    connection_error -> Cache connection error
        retry_attempts: 3
        retry_delay: 1s
        exponential_backoff: true
        fallback: "memory"
        log_level: error
        alert: true
    
    timeout_error -> Cache timeout error
        timeout: 5s
        retry_attempts: 2
        fallback: "database"
        log_level: warn
        alert: false
    
    storage_error -> Cache storage error
        max_size: 100MB
        eviction_policy: "lru"
        fallback: "memory"
        log_level: error
        alert: true
    
    data_corruption -> Cache data corruption
        validation: true
        checksum: true
        fallback: "database"
        log_level: error
        alert: true
""")
```

### Go Cache Application Implementation
```go
package main

import (
    "fmt"
    "log"
    "net/http"
    "github.com/tusklang/go-sdk"
    "github.com/gorilla/mux"
)

type CacheConfig struct {
    Environment    string `tsk:"environment"`
    DebugMode      bool   `tsk:"debug_mode"`
    Strategies     string `tsk:"strategies"`
    Storage        string `tsk:"storage"`
    Invalidation   string `tsk:"invalidation"`
    Performance    string `tsk:"performance"`
    Security       string `tsk:"security"`
    ErrorHandling  string `tsk:"error_handling"`
}

func main() {
    // Load cache configuration
    config := tusk.LoadConfig("cache-config.tsk")
    
    var cacheConfig CacheConfig
    if err := config.Unmarshal(&cacheConfig); err != nil {
        log.Fatal("Failed to load cache config:", err)
    }
    
    // Create cache system from directives
    cache := tusk.NewCacheSystem(cacheConfig)
    
    // Create router
    router := mux.NewRouter()
    
    // Apply cache middleware
    tusk.ApplyCacheMiddleware(router, cache)
    
    // Setup routes
    setupRoutes(router, cache)
    
    // Start server
    addr := fmt.Sprintf(":%s", #env("PORT", "8080"))
    log.Printf("Starting cache server on %s in %s mode", addr, cacheConfig.Environment)
    
    if err := http.ListenAndServe(addr, router); err != nil {
        log.Fatal("Cache server failed:", err)
    }
}

func setupRoutes(router *mux.Router, cache *tusk.CacheSystem) {
    // Health check
    router.HandleFunc("/health", healthHandler).Methods("GET")
    router.HandleFunc("/metrics", metricsHandler).Methods("GET")
    
    // Cache management endpoints
    cacheRouter := router.PathPrefix("/cache").Subrouter()
    cacheRouter.Use(authMiddleware)
    
    cacheRouter.HandleFunc("/clear", cache.ClearHandler).Methods("POST")
    cacheRouter.HandleFunc("/invalidate", cache.InvalidateHandler).Methods("POST")
    cacheRouter.HandleFunc("/stats", cache.StatsHandler).Methods("GET")
    cacheRouter.HandleFunc("/keys", cache.KeysHandler).Methods("GET")
    
    // API routes with caching
    api := router.PathPrefix("/api").Subrouter()
    
    // User routes with caching
    api.HandleFunc("/users/{id}", cache.WrapHandler(userHandler, "user_cache")).Methods("GET")
    api.HandleFunc("/users", cache.WrapHandler(usersHandler, "user_cache")).Methods("GET")
    
    // Post routes with caching
    api.HandleFunc("/posts/{id}", cache.WrapHandler(postHandler, "post_cache")).Methods("GET")
    api.HandleFunc("/posts", cache.WrapHandler(postsHandler, "post_cache")).Methods("GET")
    
    // Configuration routes with caching
    api.HandleFunc("/config/{key}", cache.WrapHandler(configHandler, "config_cache")).Methods("GET", "PUT")
}
```

## 🎯 Best Practices

### 1. **Use Appropriate Cache Strategies**
```tsk
# Choose the right cache strategy for each use case
strategy_selection: #cache("""
    # Cache-aside for frequently read, rarely updated data
    user_profile -> "user:{id}" -> 30m -> cache_aside
    
    # Write-through for critical data that must be consistent
    user_settings -> "settings:{user_id}" -> 1h -> write_through
    
    # Cache-only for temporary data
    session_data -> "session:{id}" -> 24h -> cache_only
""")
```

### 2. **Implement Proper Error Handling**
```go
// Comprehensive cache error handling
func handleCacheError(err error, operation string, key string) {
    log.Printf("Cache %s error for key %s: %v", operation, key, err)
    
    // Record metrics
    recordCacheError(operation, key, err)
    
    // Use fallback if available
    if fallbackCache != nil {
        log.Printf("Using fallback cache for key %s", key)
        // Implement fallback logic
    }
    
    // Alert on critical errors
    if isCriticalError(err) {
        sendCacheAlert(operation, key, err)
    }
}
```

### 3. **Use Environment-Specific Configuration**
```tsk
# Different cache settings for different environments
environment_cache: #if(
    #env("ENVIRONMENT") == "production",
    #cache("""
        ttl_multiplier: 1
        compression: true
        encryption: true
        monitoring: true
    """),
    #cache("""
        ttl_multiplier: 0.1
        compression: false
        encryption: false
        monitoring: false
    """)
)
```

### 4. **Monitor Cache Performance**
```go
// Cache performance monitoring
func monitorCachePerformance(operation string, startTime time.Time, hit bool) {
    duration := time.Since(startTime)
    
    // Record metrics
    metrics := map[string]interface{}{
        "operation": operation,
        "duration":  duration.Milliseconds(),
        "hit":       hit,
        "timestamp": time.Now(),
    }
    
    if err := recordCacheMetrics(metrics); err != nil {
        log.Printf("Failed to record cache metrics: %v", err)
    }
    
    // Alert on slow operations
    if duration > 100*time.Millisecond {
        log.Printf("Slow cache operation: %s took %v", operation, duration)
    }
    
    // Alert on low hit rates
    if !hit && getHitRate() < 0.8 {
        log.Printf("Low cache hit rate: %.2f%%", getHitRate()*100)
    }
}
```

## 🎯 Summary

Cache directives in TuskLang provide a powerful, declarative way to define caching systems. They enable:

- **Declarative cache configuration** that is easy to understand and maintain
- **Flexible cache strategies** including cache-aside, write-through, and cache-only
- **Comprehensive storage options** including Redis, memory, and file-based storage
- **Built-in security features** including encryption and access control
- **Performance optimization** with compression, prefetching, and connection pooling

The Go SDK seamlessly integrates cache directives with existing Go caching libraries, making them feel like native Go features while providing the power and flexibility of TuskLang's directive system.

**Next**: Explore rate limit directives, monitoring directives, and other specialized directive types in the following guides. 