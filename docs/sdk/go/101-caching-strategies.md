# Caching Strategies

TuskLang provides powerful caching capabilities that enable high-performance, scalable applications. This guide covers comprehensive caching strategies for Go applications.

## Caching Philosophy

### Cache-First Design
```go
// Cache-first design with TuskLang
type CacheManager struct {
    config *tusk.Config
    cache  *Cache
    store  *DataStore
}

func NewCacheManager(config *tusk.Config) *CacheManager {
    return &CacheManager{
        config: config,
        cache:  NewCache(config),
        store:  NewDataStore(config),
    }
}

// GetData retrieves data with cache-first strategy
func (cm *CacheManager) GetData(key string) (interface{}, error) {
    // Try cache first
    if data, found := cm.cache.Get(key); found {
        return data, nil
    }
    
    // Cache miss - get from store
    data, err := cm.store.Get(key)
    if err != nil {
        return nil, fmt.Errorf("failed to get data from store: %w", err)
    }
    
    // Store in cache
    cm.cache.Set(key, data)
    
    return data, nil
}

type Cache struct {
    config *tusk.Config
    redis  *redis.Client
    memory *sync.Map
}
```

### Multi-Level Caching
```go
// Multi-level caching implementation
type MultiLevelCache struct {
    config *tusk.Config
    l1     *L1Cache // Memory cache
    l2     *L2Cache // Redis cache
    l3     *L3Cache // Database
}

func (mlc *MultiLevelCache) Get(key string) (interface{}, error) {
    // Try L1 cache (memory)
    if data, found := mlc.l1.Get(key); found {
        return data, nil
    }
    
    // Try L2 cache (Redis)
    if data, found := mlc.l2.Get(key); found {
        // Update L1 cache
        mlc.l1.Set(key, data)
        return data, nil
    }
    
    // Try L3 cache (database)
    data, err := mlc.l3.Get(key)
    if err != nil {
        return nil, fmt.Errorf("failed to get data from L3: %w", err)
    }
    
    // Update L1 and L2 caches
    mlc.l1.Set(key, data)
    mlc.l2.Set(key, data)
    
    return data, nil
}

type L1Cache struct {
    cache *sync.Map
    ttl   time.Duration
}

type L2Cache struct {
    redis *redis.Client
    ttl   time.Duration
}

type L3Cache struct {
    db *sql.DB
}
```

## TuskLang Cache Configuration

### Cache Configuration
```tsk
# Caching configuration
caching {
    # Cache levels
    levels {
        # L1 cache (memory)
        l1 {
            enabled = true
            type = "memory"
            max_size = "100MB"
            ttl = "5m"
            eviction_policy = "lru"
        }
        
        # L2 cache (Redis)
        l2 {
            enabled = true
            type = "redis"
            address = "localhost:6379"
            password = ""
            db = 0
            pool_size = 10
            ttl = "1h"
            key_prefix = "app:"
        }
        
        # L3 cache (database)
        l3 {
            enabled = true
            type = "database"
            connection_string = "postgresql://user:pass@localhost:5432/cache"
            ttl = "24h"
        }
    }
    
    # Cache strategies
    strategies {
        # Cache-aside pattern
        cache_aside {
            enabled = true
            read_through = false
            write_through = false
            write_behind = false
        }
        
        # Write-through pattern
        write_through {
            enabled = false
            sync_write = true
            batch_size = 100
        }
        
        # Write-behind pattern
        write_behind {
            enabled = false
            batch_size = 100
            flush_interval = "30s"
        }
    }
    
    # Cache invalidation
    invalidation {
        # Time-based invalidation
        ttl {
            enabled = true
            default_ttl = "1h"
            max_ttl = "24h"
        }
        
        # Event-based invalidation
        events {
            enabled = true
            invalidate_on_write = true
            invalidate_on_delete = true
            pattern_matching = true
        }
        
        # Manual invalidation
        manual {
            enabled = true
            wildcard_support = true
            batch_invalidation = true
        }
    }
}
```

### Cache Definitions
```tsk
# Cache definitions
caches {
    # User cache
    user_cache {
        name = "user"
        key_pattern = "user:{id}"
        ttl = "30m"
        max_size = "50MB"
        strategy = "cache_aside"
        invalidation = {
            on_user_update = true
            on_user_delete = true
        }
    }
    
    # Product cache
    product_cache {
        name = "product"
        key_pattern = "product:{id}"
        ttl = "1h"
        max_size = "100MB"
        strategy = "write_through"
        invalidation = {
            on_product_update = true
            on_product_delete = true
            on_category_update = true
        }
    }
    
    # Session cache
    session_cache {
        name = "session"
        key_pattern = "session:{token}"
        ttl = "2h"
        max_size = "200MB"
        strategy = "cache_aside"
        invalidation = {
            on_logout = true
            on_session_expire = true
        }
    }
}
```

## Go Cache Implementation

### Memory Cache Implementation
```go
// Memory cache implementation with LRU eviction
type MemoryCache struct {
    config *tusk.Config
    cache  *lru.Cache
    ttl    time.Duration
}

func NewMemoryCache(config *tusk.Config) *MemoryCache {
    maxSize := config.GetInt("caching.levels.l1.max_size_mb", 100) * 1024 * 1024
    ttl := config.GetDuration("caching.levels.l1.ttl", 5*time.Minute)
    
    cache, err := lru.New(maxSize)
    if err != nil {
        log.Fatalf("Failed to create LRU cache: %v", err)
    }
    
    return &MemoryCache{
        config: config,
        cache:  cache,
        ttl:    ttl,
    }
}

func (mc *MemoryCache) Get(key string) (interface{}, bool) {
    item, found := mc.cache.Get(key)
    if !found {
        return nil, false
    }
    
    cacheItem := item.(*CacheItem)
    
    // Check TTL
    if time.Now().After(cacheItem.ExpiresAt) {
        mc.cache.Remove(key)
        return nil, false
    }
    
    return cacheItem.Value, true
}

func (mc *MemoryCache) Set(key string, value interface{}) {
    cacheItem := &CacheItem{
        Value:     value,
        ExpiresAt: time.Now().Add(mc.ttl),
    }
    
    mc.cache.Add(key, cacheItem)
}

type CacheItem struct {
    Value     interface{}
    ExpiresAt time.Time
}
```

### Redis Cache Implementation
```go
// Redis cache implementation
type RedisCache struct {
    config *tusk.Config
    client *redis.Client
    ttl    time.Duration
}

func NewRedisCache(config *tusk.Config) *RedisCache {
    address := config.GetString("caching.levels.l2.address")
    password := config.GetString("caching.levels.l2.password")
    db := config.GetInt("caching.levels.l2.db", 0)
    poolSize := config.GetInt("caching.levels.l2.pool_size", 10)
    ttl := config.GetDuration("caching.levels.l2.ttl", 1*time.Hour)
    
    client := redis.NewClient(&redis.Options{
        Addr:     address,
        Password: password,
        DB:       db,
        PoolSize: poolSize,
    })
    
    return &RedisCache{
        config: config,
        client: client,
        ttl:    ttl,
    }
}

func (rc *RedisCache) Get(key string) (interface{}, bool) {
    // Add key prefix
    fullKey := rc.getFullKey(key)
    
    // Get from Redis
    result, err := rc.client.Get(fullKey).Result()
    if err != nil {
        if err == redis.Nil {
            return nil, false
        }
        log.Printf("Redis get error: %v", err)
        return nil, false
    }
    
    // Deserialize value
    var value interface{}
    if err := json.Unmarshal([]byte(result), &value); err != nil {
        log.Printf("Failed to deserialize value: %v", err)
        return nil, false
    }
    
    return value, true
}

func (rc *RedisCache) Set(key string, value interface{}) error {
    // Add key prefix
    fullKey := rc.getFullKey(key)
    
    // Serialize value
    data, err := json.Marshal(value)
    if err != nil {
        return fmt.Errorf("failed to serialize value: %w", err)
    }
    
    // Set in Redis with TTL
    err = rc.client.Set(fullKey, data, rc.ttl).Err()
    if err != nil {
        return fmt.Errorf("failed to set value in Redis: %w", err)
    }
    
    return nil
}

func (rc *RedisCache) getFullKey(key string) string {
    prefix := rc.config.GetString("caching.levels.l2.key_prefix", "app:")
    return prefix + key
}
```

### Cache-Aside Pattern Implementation
```go
// Cache-aside pattern implementation
type CacheAside struct {
    config *tusk.Config
    cache  *Cache
    store  *DataStore
}

func (ca *CacheAside) Get(key string) (interface{}, error) {
    // Try cache first
    if data, found := ca.cache.Get(key); found {
        return data, nil
    }
    
    // Cache miss - get from store
    data, err := ca.store.Get(key)
    if err != nil {
        return nil, fmt.Errorf("failed to get data from store: %w", err)
    }
    
    // Store in cache
    ca.cache.Set(key, data)
    
    return data, nil
}

func (ca *CacheAside) Set(key string, value interface{}) error {
    // Update store first
    if err := ca.store.Set(key, value); err != nil {
        return fmt.Errorf("failed to update store: %w", err)
    }
    
    // Update cache
    ca.cache.Set(key, value)
    
    return nil
}

func (ca *CacheAside) Delete(key string) error {
    // Delete from store first
    if err := ca.store.Delete(key); err != nil {
        return fmt.Errorf("failed to delete from store: %w", err)
    }
    
    // Invalidate cache
    ca.cache.Delete(key)
    
    return nil
}
```

## Advanced Caching Features

### Write-Through Pattern
```go
// Write-through pattern implementation
type WriteThrough struct {
    config *tusk.Config
    cache  *Cache
    store  *DataStore
}

func (wt *WriteThrough) Set(key string, value interface{}) error {
    // Update cache and store simultaneously
    cacheErr := make(chan error, 1)
    storeErr := make(chan error, 1)
    
    // Update cache
    go func() {
        wt.cache.Set(key, value)
        cacheErr <- nil
    }()
    
    // Update store
    go func() {
        storeErr <- wt.store.Set(key, value)
    }()
    
    // Wait for both operations
    if err := <-storeErr; err != nil {
        return fmt.Errorf("failed to update store: %w", err)
    }
    
    if err := <-cacheErr; err != nil {
        return fmt.Errorf("failed to update cache: %w", err)
    }
    
    return nil
}
```

### Write-Behind Pattern
```go
// Write-behind pattern implementation
type WriteBehind struct {
    config *tusk.Config
    cache  *Cache
    store  *DataStore
    queue  chan WriteOperation
}

type WriteOperation struct {
    Key   string
    Value interface{}
    Type  string // "set" or "delete"
}

func NewWriteBehind(config *tusk.Config) *WriteBehind {
    wb := &WriteBehind{
        config: config,
        cache:  NewCache(config),
        store:  NewDataStore(config),
        queue:  make(chan WriteOperation, 1000),
    }
    
    // Start background processor
    go wb.processWrites()
    
    return wb
}

func (wb *WriteBehind) Set(key string, value interface{}) error {
    // Update cache immediately
    wb.cache.Set(key, value)
    
    // Queue write operation
    wb.queue <- WriteOperation{
        Key:   key,
        Value: value,
        Type:  "set",
    }
    
    return nil
}

func (wb *WriteBehind) processWrites() {
    batchSize := wb.config.GetInt("caching.strategies.write_behind.batch_size", 100)
    flushInterval := wb.config.GetDuration("caching.strategies.write_behind.flush_interval", 30*time.Second)
    
    var operations []WriteOperation
    ticker := time.NewTicker(flushInterval)
    defer ticker.Stop()
    
    for {
        select {
        case op := <-wb.queue:
            operations = append(operations, op)
            
            if len(operations) >= batchSize {
                wb.flushOperations(operations)
                operations = operations[:0]
            }
            
        case <-ticker.C:
            if len(operations) > 0 {
                wb.flushOperations(operations)
                operations = operations[:0]
            }
        }
    }
}

func (wb *WriteBehind) flushOperations(operations []WriteOperation) {
    for _, op := range operations {
        switch op.Type {
        case "set":
            if err := wb.store.Set(op.Key, op.Value); err != nil {
                log.Printf("Failed to write operation: %v", err)
            }
        case "delete":
            if err := wb.store.Delete(op.Key); err != nil {
                log.Printf("Failed to delete operation: %v", err)
            }
        }
    }
}
```

### Cache Invalidation
```go
// Cache invalidation implementation
type CacheInvalidator struct {
    config *tusk.Config
    cache  *Cache
}

func (ci *CacheInvalidator) InvalidatePattern(pattern string) error {
    // Get all keys matching pattern
    keys, err := ci.getKeysByPattern(pattern)
    if err != nil {
        return fmt.Errorf("failed to get keys by pattern: %w", err)
    }
    
    // Invalidate each key
    for _, key := range keys {
        ci.cache.Delete(key)
    }
    
    return nil
}

func (ci *CacheInvalidator) InvalidateByEvent(event string, data map[string]interface{}) error {
    // Get invalidation rules for event
    rules := ci.getInvalidationRules(event)
    
    for _, rule := range rules {
        if err := ci.applyInvalidationRule(rule, data); err != nil {
            return fmt.Errorf("failed to apply invalidation rule: %w", err)
        }
    }
    
    return nil
}

func (ci *CacheInvalidator) getInvalidationRules(event string) []InvalidationRule {
    // Get rules from configuration
    events := ci.config.GetMap("caching.invalidation.events")
    if eventConfig, exists := events[event]; exists {
        if config, ok := eventConfig.(map[string]interface{}); ok {
            // Parse rules from configuration
            return ci.parseInvalidationRules(config)
        }
    }
    
    return []InvalidationRule{}
}

type InvalidationRule struct {
    Pattern string
    Keys    []string
    Action  string
}
```

### Cache Warming
```go
// Cache warming implementation
type CacheWarmer struct {
    config *tusk.Config
    cache  *Cache
    store  *DataStore
}

func (cw *CacheWarmer) WarmCache(cacheName string) error {
    // Get cache configuration
    cacheConfig := cw.getCacheConfig(cacheName)
    if cacheConfig == nil {
        return fmt.Errorf("cache configuration not found: %s", cacheName)
    }
    
    // Get warming strategy
    strategy := cw.getWarmingStrategy(cacheConfig)
    
    // Execute warming strategy
    switch strategy {
    case "preload":
        return cw.preloadCache(cacheConfig)
    case "predictive":
        return cw.predictiveWarming(cacheConfig)
    case "scheduled":
        return cw.scheduledWarming(cacheConfig)
    default:
        return fmt.Errorf("unknown warming strategy: %s", strategy)
    }
}

func (cw *CacheWarmer) preloadCache(config *CacheConfig) error {
    // Get frequently accessed data
    keys, err := cw.getFrequentlyAccessedKeys(config)
    if err != nil {
        return fmt.Errorf("failed to get frequently accessed keys: %w", err)
    }
    
    // Preload data into cache
    for _, key := range keys {
        data, err := cw.store.Get(key)
        if err != nil {
            log.Printf("Failed to preload key %s: %v", key, err)
            continue
        }
        
        cw.cache.Set(key, data)
    }
    
    return nil
}

type CacheConfig struct {
    Name     string
    Strategy string
    Pattern  string
    TTL      time.Duration
}
```

## Cache Tools and Utilities

### Cache Monitoring
```go
// Cache monitoring and metrics
type CacheMonitor struct {
    config *tusk.Config
    metrics map[string]*CacheMetrics
}

type CacheMetrics struct {
    CacheName    string
    HitCount     int64
    MissCount    int64
    HitRate      float64
    Size         int64
    EvictionCount int64
    LastUpdated  time.Time
}

func (cm *CacheMonitor) RecordHit(cacheName string) {
    metrics := cm.getOrCreateMetrics(cacheName)
    atomic.AddInt64(&metrics.HitCount, 1)
    cm.updateHitRate(metrics)
}

func (cm *CacheMonitor) RecordMiss(cacheName string) {
    metrics := cm.getOrCreateMetrics(cacheName)
    atomic.AddInt64(&metrics.MissCount, 1)
    cm.updateHitRate(metrics)
}

func (cm *CacheMonitor) updateHitRate(metrics *CacheMetrics) {
    total := metrics.HitCount + metrics.MissCount
    if total > 0 {
        metrics.HitRate = float64(metrics.HitCount) / float64(total)
    }
    metrics.LastUpdated = time.Now()
}

func (cm *CacheMonitor) getOrCreateMetrics(cacheName string) *CacheMetrics {
    if metrics, exists := cm.metrics[cacheName]; exists {
        return metrics
    }
    
    metrics := &CacheMetrics{CacheName: cacheName}
    cm.metrics[cacheName] = metrics
    return metrics
}
```

### Cache Analytics
```go
// Cache analytics and insights
type CacheAnalytics struct {
    config *tusk.Config
    monitor *CacheMonitor
}

func (ca *CacheAnalytics) GenerateReport() (*CacheReport, error) {
    report := &CacheReport{
        GeneratedAt: time.Now(),
        Caches:      make(map[string]*CacheStats),
    }
    
    // Generate stats for each cache
    for cacheName, metrics := range ca.monitor.metrics {
        stats := &CacheStats{
            Name:        cacheName,
            HitRate:     metrics.HitRate,
            HitCount:    metrics.HitCount,
            MissCount:   metrics.MissCount,
            Size:        metrics.Size,
            Evictions:   metrics.EvictionCount,
        }
        
        report.Caches[cacheName] = stats
    }
    
    // Calculate overall statistics
    ca.calculateOverallStats(report)
    
    return report, nil
}

func (ca *CacheAnalytics) calculateOverallStats(report *CacheReport) {
    var totalHits, totalMisses int64
    var totalSize int64
    
    for _, stats := range report.Caches {
        totalHits += stats.HitCount
        totalMisses += stats.MissCount
        totalSize += stats.Size
    }
    
    report.OverallStats = &OverallStats{
        TotalHits:   totalHits,
        TotalMisses: totalMisses,
        TotalSize:   totalSize,
        HitRate:     float64(totalHits) / float64(totalHits+totalMisses),
    }
}

type CacheReport struct {
    GeneratedAt   time.Time
    Caches        map[string]*CacheStats
    OverallStats  *OverallStats
}

type CacheStats struct {
    Name       string
    HitRate    float64
    HitCount   int64
    MissCount  int64
    Size       int64
    Evictions  int64
}

type OverallStats struct {
    TotalHits   int64
    TotalMisses int64
    TotalSize   int64
    HitRate     float64
}
```

## Validation and Error Handling

### Cache Configuration Validation
```go
// Validate cache configuration
func ValidateCacheConfig(config *tusk.Config) error {
    if config == nil {
        return errors.New("cache config cannot be nil")
    }
    
    // Validate cache levels configuration
    if !config.Has("caching.levels") {
        return errors.New("missing cache levels configuration")
    }
    
    // Validate cache strategies
    if !config.Has("caching.strategies") {
        return errors.New("missing cache strategies configuration")
    }
    
    return nil
}
```

### Error Handling
```go
// Handle cache errors gracefully
func handleCacheError(err error, context string) {
    log.Printf("Cache error in %s: %v", context, err)
    
    // Log additional context if available
    if cacheErr, ok := err.(*CacheError); ok {
        log.Printf("Cache context: %s", cacheErr.Context)
    }
}
```

## Performance Considerations

### Cache Performance Optimization
```go
// Optimize cache performance
type CacheOptimizer struct {
    config *tusk.Config
}

func (co *CacheOptimizer) OptimizeCache() error {
    // Enable connection pooling
    if co.config.GetBool("caching.performance.connection_pooling") {
        co.setupConnectionPooling()
    }
    
    // Enable compression
    if co.config.GetBool("caching.performance.compression") {
        co.setupCompression()
    }
    
    // Optimize serialization
    if err := co.optimizeSerialization(); err != nil {
        return fmt.Errorf("failed to optimize serialization: %w", err)
    }
    
    return nil
}

func (co *CacheOptimizer) setupConnectionPooling() {
    // Setup connection pooling for better performance
    // This is a simplified implementation
}

func (co *CacheOptimizer) setupCompression() {
    // Setup compression for better storage efficiency
    // This is a simplified implementation
}
```

## Caching Notes

- **Cache Levels**: Use multi-level caching for optimal performance
- **Cache Strategies**: Choose appropriate caching strategies
- **Cache Invalidation**: Implement proper cache invalidation
- **Cache Warming**: Use cache warming for better performance
- **Cache Monitoring**: Monitor cache performance and hit rates
- **Cache Analytics**: Analyze cache usage patterns
- **Cache Compression**: Use compression for large data
- **Cache Serialization**: Optimize serialization for performance

## Best Practices

1. **Cache Design**: Design caches with clear purposes and boundaries
2. **Cache Levels**: Use appropriate cache levels for different data types
3. **Cache Strategies**: Choose caching strategies based on access patterns
4. **Cache Invalidation**: Implement proper cache invalidation strategies
5. **Cache Warming**: Use cache warming for frequently accessed data
6. **Cache Monitoring**: Monitor cache performance comprehensively
7. **Cache Analytics**: Analyze cache usage for optimization
8. **Cache Performance**: Optimize cache performance and efficiency

## Integration with TuskLang

```go
// Load cache configuration from TuskLang
func LoadCacheConfig(configPath string) (*tusk.Config, error) {
    config, err := tusk.LoadConfig(configPath)
    if err != nil {
        return nil, fmt.Errorf("failed to load cache config: %w", err)
    }
    
    // Validate cache configuration
    if err := ValidateCacheConfig(config); err != nil {
        return nil, fmt.Errorf("invalid cache config: %w", err)
    }
    
    return config, nil
}
```

This caching strategies guide provides comprehensive caching capabilities for your Go applications using TuskLang. Remember, good caching design is essential for high-performance applications. 