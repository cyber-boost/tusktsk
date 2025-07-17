# Cache Hash Directives in TuskLang for Go

## Overview

Cache hash directives in TuskLang provide powerful caching configuration capabilities directly in your configuration files. These directives enable you to define sophisticated caching strategies, cache invalidation policies, and multi-level caching with Go integration for high-performance applications.

## Basic Cache Directive Syntax

```go
// TuskLang cache directive
#cache {
    stores: {
        memory: {
            type: "memory"
            max_size: 1000
            ttl: "1h"
            cleanup_interval: "10m"
        }
        
        redis: {
            type: "redis"
            url: "@env('REDIS_URL')"
            pool_size: 10
            ttl: "24h"
            key_prefix: "myapp:"
        }
        
        disk: {
            type: "disk"
            path: "/tmp/cache"
            max_size: "1GB"
            ttl: "7d"
        }
    }
    
    policies: {
        user_data: {
            store: "redis"
            ttl: "1h"
            invalidation: {
                on_update: true
                on_delete: true
                patterns: ["user:*", "profile:*"]
            }
        }
        
        static_content: {
            store: "disk"
            ttl: "7d"
            compression: true
            invalidation: {
                on_update: false
                patterns: ["static:*"]
            }
        }
        
        session_data: {
            store: "memory"
            ttl: "30m"
            invalidation: {
                on_logout: true
                patterns: ["session:*"]
            }
        }
    }
    
    strategies: {
        write_through: {
            enabled: true
            stores: ["memory", "redis"]
        }
        
        write_behind: {
            enabled: true
            batch_size: 100
            flush_interval: "5s"
        }
        
        cache_aside: {
            enabled: true
            fallback: true
        }
    }
}
```

## Go Integration

```go
package main

import (
    "context"
    "encoding/json"
    "fmt"
    "log"
    "sync"
    "time"
    
    "github.com/go-redis/redis/v8"
    "github.com/tusklang/go-sdk"
)

type CacheDirective struct {
    Stores    map[string]Store    `tsk:"stores"`
    Policies  map[string]Policy   `tsk:"policies"`
    Strategies map[string]Strategy `tsk:"strategies"`
}

type Store struct {
    Type            string                 `tsk:"type"`
    Config          map[string]interface{} `tsk:",inline"`
}

type Policy struct {
    Store       string                 `tsk:"store"`
    TTL         string                 `tsk:"ttl"`
    Compression bool                   `tsk:"compression"`
    Invalidation InvalidationConfig    `tsk:"invalidation"`
}

type InvalidationConfig struct {
    OnUpdate  bool     `tsk:"on_update"`
    OnDelete  bool     `tsk:"on_delete"`
    OnLogout  bool     `tsk:"on_logout"`
    Patterns  []string `tsk:"patterns"`
}

type Strategy struct {
    Enabled       bool     `tsk:"enabled"`
    Stores        []string `tsk:"stores"`
    BatchSize     int      `tsk:"batch_size"`
    FlushInterval string   `tsk:"flush_interval"`
    Fallback      bool     `tsk:"fallback"`
}

type CacheManager struct {
    directive  CacheDirective
    stores     map[string]CacheStore
    policies   map[string]Policy
    strategies map[string]Strategy
    mu         sync.RWMutex
}

type CacheStore interface {
    Get(key string) (interface{}, error)
    Set(key string, value interface{}, ttl time.Duration) error
    Delete(key string) error
    Clear() error
    Close() error
}

type MemoryStore struct {
    data map[string]cacheItem
    mu   sync.RWMutex
    maxSize int
    ttl    time.Duration
}

type cacheItem struct {
    value      interface{}
    expiration time.Time
}

type RedisStore struct {
    client *redis.Client
    prefix string
    ttl    time.Duration
}

type DiskStore struct {
    path    string
    maxSize int64
    ttl     time.Duration
}

func main() {
    // Load cache configuration
    config, err := tusk.LoadFile("cache-config.tsk")
    if err != nil {
        log.Fatalf("Error loading cache config: %v", err)
    }
    
    var cacheDirective CacheDirective
    if err := config.Get("#cache", &cacheDirective); err != nil {
        log.Fatalf("Error parsing cache directive: %v", err)
    }
    
    // Initialize cache manager
    cacheManager := NewCacheManager(cacheDirective)
    defer cacheManager.Close()
    
    // Example usage
    cacheManager.Set("user:123", map[string]interface{}{
        "id":    "123",
        "name":  "John Doe",
        "email": "john@example.com",
    }, time.Hour)
    
    if user, err := cacheManager.Get("user:123"); err == nil {
        log.Printf("Cached user: %+v", user)
    }
    
    // Example with policy
    cacheManager.SetWithPolicy("user:456", map[string]interface{}{
        "id":    "456",
        "name":  "Jane Smith",
        "email": "jane@example.com",
    }, "user_data")
}

func NewCacheManager(directive CacheDirective) *CacheManager {
    manager := &CacheManager{
        directive:  directive,
        stores:     make(map[string]CacheStore),
        policies:   directive.Policies,
        strategies: directive.Strategies,
    }
    
    // Initialize stores
    for name, store := range directive.Stores {
        cacheStore, err := manager.createStore(store)
        if err != nil {
            log.Printf("Error creating store %s: %v", name, err)
            continue
        }
        manager.stores[name] = cacheStore
    }
    
    return manager
}

func (cm *CacheManager) createStore(store Store) (CacheStore, error) {
    switch store.Type {
    case "memory":
        return cm.createMemoryStore(store.Config)
    case "redis":
        return cm.createRedisStore(store.Config)
    case "disk":
        return cm.createDiskStore(store.Config)
    default:
        return nil, fmt.Errorf("unknown store type: %s", store.Type)
    }
}

func (cm *CacheManager) createMemoryStore(config map[string]interface{}) (CacheStore, error) {
    maxSize := config["max_size"].(int)
    ttlStr := config["ttl"].(string)
    ttl, err := time.ParseDuration(ttlStr)
    if err != nil {
        return nil, err
    }
    
    store := &MemoryStore{
        data:    make(map[string]cacheItem),
        maxSize: maxSize,
        ttl:     ttl,
    }
    
    // Start cleanup goroutine
    go store.cleanup()
    
    return store, nil
}

func (cm *CacheManager) createRedisStore(config map[string]interface{}) (CacheStore, error) {
    url := config["url"].(string)
    poolSize := config["pool_size"].(int)
    ttlStr := config["ttl"].(string)
    prefix := config["key_prefix"].(string)
    
    ttl, err := time.ParseDuration(ttlStr)
    if err != nil {
        return nil, err
    }
    
    client := redis.NewClient(&redis.Options{
        Addr:     url,
        PoolSize: poolSize,
    })
    
    // Test connection
    ctx := context.Background()
    if err := client.Ping(ctx).Err(); err != nil {
        return nil, err
    }
    
    return &RedisStore{
        client: client,
        prefix: prefix,
        ttl:    ttl,
    }, nil
}

func (cm *CacheManager) createDiskStore(config map[string]interface{}) (CacheStore, error) {
    path := config["path"].(string)
    maxSizeStr := config["max_size"].(string)
    ttlStr := config["ttl"].(string)
    
    // Parse max size (simplified)
    maxSize := int64(1024 * 1024 * 1024) // 1GB default
    
    ttl, err := time.ParseDuration(ttlStr)
    if err != nil {
        return nil, err
    }
    
    return &DiskStore{
        path:    path,
        maxSize: maxSize,
        ttl:     ttl,
    }, nil
}

// CacheManager methods
func (cm *CacheManager) Get(key string) (interface{}, error) {
    cm.mu.RLock()
    defer cm.mu.RUnlock()
    
    // Try to find appropriate policy
    policy := cm.findPolicy(key)
    if policy != nil {
        return cm.getWithPolicy(key, *policy)
    }
    
    // Default to first available store
    for _, store := range cm.stores {
        if value, err := store.Get(key); err == nil {
            return value, nil
        }
    }
    
    return nil, fmt.Errorf("key not found: %s", key)
}

func (cm *CacheManager) Set(key string, value interface{}, ttl time.Duration) error {
    cm.mu.Lock()
    defer cm.mu.Unlock()
    
    // Try to find appropriate policy
    policy := cm.findPolicy(key)
    if policy != nil {
        return cm.setWithPolicy(key, value, *policy)
    }
    
    // Default to first available store
    for _, store := range cm.stores {
        if err := store.Set(key, value, ttl); err == nil {
            return nil
        }
    }
    
    return fmt.Errorf("failed to set key: %s", key)
}

func (cm *CacheManager) SetWithPolicy(key string, value interface{}, policyName string) error {
    cm.mu.Lock()
    defer cm.mu.Unlock()
    
    policy, exists := cm.policies[policyName]
    if !exists {
        return fmt.Errorf("policy not found: %s", policyName)
    }
    
    return cm.setWithPolicy(key, value, policy)
}

func (cm *CacheManager) Delete(key string) error {
    cm.mu.Lock()
    defer cm.mu.Unlock()
    
    // Delete from all stores
    for _, store := range cm.stores {
        store.Delete(key)
    }
    
    // Handle invalidation patterns
    cm.handleInvalidation(key, "delete")
    
    return nil
}

func (cm *CacheManager) Close() error {
    cm.mu.Lock()
    defer cm.mu.Unlock()
    
    for _, store := range cm.stores {
        store.Close()
    }
    
    return nil
}

func (cm *CacheManager) findPolicy(key string) *Policy {
    for policyName, policy := range cm.policies {
        // Simple pattern matching (could be more sophisticated)
        for _, pattern := range policy.Invalidation.Patterns {
            if cm.matchesPattern(key, pattern) {
                return &policy
            }
        }
    }
    return nil
}

func (cm *CacheManager) matchesPattern(key, pattern string) bool {
    // Simple wildcard matching
    if pattern == "*" {
        return true
    }
    
    if strings.HasSuffix(pattern, "*") {
        prefix := strings.TrimSuffix(pattern, "*")
        return strings.HasPrefix(key, prefix)
    }
    
    return key == pattern
}

func (cm *CacheManager) getWithPolicy(key string, policy Policy) (interface{}, error) {
    store, exists := cm.stores[policy.Store]
    if !exists {
        return nil, fmt.Errorf("store not found: %s", policy.Store)
    }
    
    return store.Get(key)
}

func (cm *CacheManager) setWithPolicy(key string, value interface{}, policy Policy) error {
    store, exists := cm.stores[policy.Store]
    if !exists {
        return fmt.Errorf("store not found: %s", policy.Store)
    }
    
    ttl, err := time.ParseDuration(policy.TTL)
    if err != nil {
        return err
    }
    
    // Handle compression if enabled
    if policy.Compression {
        value = cm.compress(value)
    }
    
    return store.Set(key, value, ttl)
}

func (cm *CacheManager) handleInvalidation(key, action string) {
    for policyName, policy := range cm.policies {
        for _, pattern := range policy.Invalidation.Patterns {
            if cm.matchesPattern(key, pattern) {
                switch action {
                case "update":
                    if policy.Invalidation.OnUpdate {
                        cm.invalidatePattern(pattern)
                    }
                case "delete":
                    if policy.Invalidation.OnDelete {
                        cm.invalidatePattern(pattern)
                    }
                case "logout":
                    if policy.Invalidation.OnLogout {
                        cm.invalidatePattern(pattern)
                    }
                }
            }
        }
    }
}

func (cm *CacheManager) invalidatePattern(pattern string) {
    // This would implement pattern-based invalidation
    // For now, we'll just log it
    log.Printf("Invalidating pattern: %s", pattern)
}

func (cm *CacheManager) compress(value interface{}) interface{} {
    // Implement compression logic
    return value
}

// MemoryStore implementation
func (ms *MemoryStore) Get(key string) (interface{}, error) {
    ms.mu.RLock()
    defer ms.mu.RUnlock()
    
    item, exists := ms.data[key]
    if !exists {
        return nil, fmt.Errorf("key not found")
    }
    
    if time.Now().After(item.expiration) {
        delete(ms.data, key)
        return nil, fmt.Errorf("key expired")
    }
    
    return item.value, nil
}

func (ms *MemoryStore) Set(key string, value interface{}, ttl time.Duration) error {
    ms.mu.Lock()
    defer ms.mu.Unlock()
    
    // Check size limit
    if len(ms.data) >= ms.maxSize {
        ms.evictOldest()
    }
    
    ms.data[key] = cacheItem{
        value:      value,
        expiration: time.Now().Add(ttl),
    }
    
    return nil
}

func (ms *MemoryStore) Delete(key string) error {
    ms.mu.Lock()
    defer ms.mu.Unlock()
    
    delete(ms.data, key)
    return nil
}

func (ms *MemoryStore) Clear() error {
    ms.mu.Lock()
    defer ms.mu.Unlock()
    
    ms.data = make(map[string]cacheItem)
    return nil
}

func (ms *MemoryStore) Close() error {
    return nil
}

func (ms *MemoryStore) evictOldest() {
    var oldestKey string
    var oldestTime time.Time
    
    for key, item := range ms.data {
        if oldestKey == "" || item.expiration.Before(oldestTime) {
            oldestKey = key
            oldestTime = item.expiration
        }
    }
    
    if oldestKey != "" {
        delete(ms.data, oldestKey)
    }
}

func (ms *MemoryStore) cleanup() {
    ticker := time.NewTicker(time.Minute)
    defer ticker.Stop()
    
    for range ticker.C {
        ms.mu.Lock()
        now := time.Now()
        for key, item := range ms.data {
            if now.After(item.expiration) {
                delete(ms.data, key)
            }
        }
        ms.mu.Unlock()
    }
}

// RedisStore implementation
func (rs *RedisStore) Get(key string) (interface{}, error) {
    ctx := context.Background()
    fullKey := rs.prefix + key
    
    data, err := rs.client.Get(ctx, fullKey).Result()
    if err != nil {
        return nil, err
    }
    
    var value interface{}
    if err := json.Unmarshal([]byte(data), &value); err != nil {
        return nil, err
    }
    
    return value, nil
}

func (rs *RedisStore) Set(key string, value interface{}, ttl time.Duration) error {
    ctx := context.Background()
    fullKey := rs.prefix + key
    
    data, err := json.Marshal(value)
    if err != nil {
        return err
    }
    
    return rs.client.Set(ctx, fullKey, data, ttl).Err()
}

func (rs *RedisStore) Delete(key string) error {
    ctx := context.Background()
    fullKey := rs.prefix + key
    
    return rs.client.Del(ctx, fullKey).Err()
}

func (rs *RedisStore) Clear() error {
    ctx := context.Background()
    pattern := rs.prefix + "*"
    
    keys, err := rs.client.Keys(ctx, pattern).Result()
    if err != nil {
        return err
    }
    
    if len(keys) > 0 {
        return rs.client.Del(ctx, keys...).Err()
    }
    
    return nil
}

func (rs *RedisStore) Close() error {
    return rs.client.Close()
}

// DiskStore implementation (simplified)
func (ds *DiskStore) Get(key string) (interface{}, error) {
    // Implement disk-based caching
    return nil, fmt.Errorf("disk store not implemented")
}

func (ds *DiskStore) Set(key string, value interface{}, ttl time.Duration) error {
    // Implement disk-based caching
    return fmt.Errorf("disk store not implemented")
}

func (ds *DiskStore) Delete(key string) error {
    // Implement disk-based caching
    return fmt.Errorf("disk store not implemented")
}

func (ds *DiskStore) Clear() error {
    // Implement disk-based caching
    return fmt.Errorf("disk store not implemented")
}

func (ds *DiskStore) Close() error {
    return nil
}
```

## Advanced Cache Features

### Cache Warming

```go
// TuskLang configuration with cache warming
#cache {
    warming: {
        enabled: true
        strategies: {
            startup: {
                enabled: true
                data_sources: ["database", "api"]
                patterns: ["user:*", "product:*"]
            }
            
            scheduled: {
                enabled: true
                schedule: "0 */6 * * *"
                data_sources: ["database"]
                patterns: ["stats:*", "analytics:*"]
            }
        }
    }
}
```

### Cache Analytics

```go
// TuskLang configuration with cache analytics
#cache {
    analytics: {
        enabled: true
        metrics: {
            hit_rate: true
            miss_rate: true
            eviction_rate: true
            memory_usage: true
        }
        
        reporting: {
            interval: "1m"
            destination: "prometheus"
        }
    }
}
```

## Performance Considerations

- **Memory Management**: Implement proper memory management for in-memory caches
- **Connection Pooling**: Use connection pooling for external cache stores
- **Serialization**: Optimize serialization/deserialization performance
- **Batch Operations**: Use batch operations for bulk cache operations
- **Async Operations**: Use goroutines for non-blocking cache operations

## Security Notes

- **Access Control**: Implement proper access control for cache stores
- **Data Encryption**: Encrypt sensitive cached data
- **Key Validation**: Validate cache keys to prevent injection attacks
- **TTL Management**: Use appropriate TTL values for sensitive data
- **Audit Logging**: Log cache access for security auditing

## Best Practices

1. **Cache Key Design**: Design cache keys for optimal performance
2. **TTL Strategy**: Use appropriate TTL values for different data types
3. **Cache Invalidation**: Implement proper cache invalidation strategies
4. **Monitoring**: Monitor cache performance and hit rates
5. **Testing**: Test cache behavior under load and failure conditions
6. **Documentation**: Document cache policies and strategies

## Integration Examples

### With Gin Framework

```go
import (
    "github.com/gin-gonic/gin"
    "github.com/tusklang/go-sdk"
)

func setupGinCache(config tusk.Config) *gin.Engine {
    var cacheDirective CacheDirective
    config.Get("#cache", &cacheDirective)
    
    cacheManager := NewCacheManager(cacheDirective)
    
    router := gin.Default()
    
    // Add cache middleware
    router.Use(cacheMiddleware(cacheManager))
    
    return router
}

func cacheMiddleware(cacheManager *CacheManager) gin.HandlerFunc {
    return func(c *gin.Context) {
        // Implement cache middleware logic
        c.Set("cache", cacheManager)
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

func setupEchoCache(config tusk.Config) *echo.Echo {
    var cacheDirective CacheDirective
    config.Get("#cache", &cacheDirective)
    
    cacheManager := NewCacheManager(cacheDirective)
    
    e := echo.New()
    
    // Add cache middleware
    e.Use(cacheMiddleware(cacheManager))
    
    return e
}
```

This comprehensive cache hash directives documentation provides Go developers with everything they need to build sophisticated caching systems using TuskLang's powerful directive system. 