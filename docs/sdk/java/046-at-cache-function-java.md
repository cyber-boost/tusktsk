# @cache - Caching Function in Java

The `@cache` operator provides powerful caching capabilities for Java applications, integrating with Spring Boot's caching framework, Redis, Caffeine, and enterprise caching solutions.

## Basic Syntax

```java
// TuskLang configuration
cached_value: @cache("user_profile", 3600)
cached_data: @cache("product_data", 1800, "default_value")
cache_key: @cache.key("user:{id}:profile")
```

```java
// Java Spring Boot integration
@Configuration
@EnableCaching
public class CacheConfig {
    
    @Bean
    public CacheManager cacheManager() {
        return new ConcurrentMapCacheManager();
    }
    
    @Bean
    public CacheService cacheService(CacheManager cacheManager) {
        return CacheService.builder()
            .cacheManager(cacheManager)
            .defaultTtl(3600)
            .build();
    }
}
```

## Basic Caching

```java
// Java cache service
@Component
public class CacheService {
    
    @Autowired
    private CacheManager cacheManager;
    
    private final Map<String, Long> defaultTtls = new ConcurrentHashMap<>();
    
    public <T> T get(String key, Class<T> type) {
        Cache cache = cacheManager.getCache("default");
        if (cache != null) {
            Cache.ValueWrapper wrapper = cache.get(key);
            if (wrapper != null) {
                return type.cast(wrapper.get());
            }
        }
        return null;
    }
    
    public <T> T get(String key, Class<T> type, T defaultValue) {
        T value = get(key, type);
        return value != null ? value : defaultValue;
    }
    
    public void put(String key, Object value) {
        put(key, value, getDefaultTtl(key));
    }
    
    public void put(String key, Object value, long ttlSeconds) {
        Cache cache = cacheManager.getCache("default");
        if (cache != null) {
            cache.put(key, value);
            defaultTtls.put(key, ttlSeconds);
        }
    }
    
    public void delete(String key) {
        Cache cache = cacheManager.getCache("default");
        if (cache != null) {
            cache.evict(key);
            defaultTtls.remove(key);
        }
    }
    
    public void clear() {
        Cache cache = cacheManager.getCache("default");
        if (cache != null) {
            cache.clear();
            defaultTtls.clear();
        }
    }
    
    public boolean exists(String key) {
        Cache cache = cacheManager.getCache("default");
        return cache != null && cache.get(key) != null;
    }
    
    private long getDefaultTtl(String key) {
        return defaultTtls.getOrDefault(key, 3600L);
    }
}
```

```java
// TuskLang caching
cache_config: {
    # Basic caching
    user_profile: @cache("user_profile", 3600)
    product_data: @cache("product_data", 1800, "default_value")
    
    # Cache with key generation
    user_cache_key: @cache.key("user:{id}:profile")
    product_cache_key: @cache.key("product:{id}:details")
    
    # Cache with conditions
    conditional_cache: @cache.conditional("data_cache", 3600, {
        condition: "data != null"
        fallback: "default_data"
    })
}
```

## Cache Stores

```java
// Java cache stores configuration
@Configuration
public class CacheStoresConfig {
    
    @Bean
    @Primary
    public CacheManager redisCacheManager(RedisConnectionFactory connectionFactory) {
        RedisCacheConfiguration config = RedisCacheConfiguration.defaultCacheConfig()
            .entryTtl(Duration.ofHours(1))
            .serializeKeysWith(RedisSerializationContext.SerializationPair.fromSerializer(new StringRedisSerializer()))
            .serializeValuesWith(RedisSerializationContext.SerializationPair.fromSerializer(new GenericJackson2JsonRedisSerializer()));
        
        return RedisCacheManager.builder(connectionFactory)
            .cacheDefaults(config)
            .build();
    }
    
    @Bean
    public CacheManager caffeineCacheManager() {
        CaffeineCacheManager cacheManager = new CaffeineCacheManager();
        cacheManager.setCaffeine(Caffeine.newBuilder()
            .maximumSize(1000)
            .expireAfterWrite(Duration.ofHours(1))
            .recordStats());
        return cacheManager;
    }
    
    @Bean
    public CacheManager ehCacheManager() {
        return new EhCacheCacheManager(ehCacheManager());
    }
    
    private net.sf.ehcache.CacheManager ehCacheManager() {
        return net.sf.ehcache.CacheManager.newInstance();
    }
}
```

```java
// TuskLang cache stores
cache_stores: {
    # Redis cache
    redis_cache: @cache.store("redis", {
        host: "localhost"
        port: 6379
        database: 0
        ttl: 3600
    })
    
    # Caffeine cache
    caffeine_cache: @cache.store("caffeine", {
        max_size: 1000
        ttl: 3600
        record_stats: true
    })
    
    # EhCache
    ehcache: @cache.store("ehcache", {
        max_elements: 10000
        time_to_live: 3600
        time_to_idle: 1800
    })
    
    # Memory cache
    memory_cache: @cache.store("memory", {
        max_size: 100
        ttl: 1800
    })
}
```

## Caching Patterns

```java
// Java caching patterns
@Component
public class CachingPatternsService {
    
    @Autowired
    private CacheService cacheService;
    
    @Autowired
    private UserService userService;
    
    // Cache-aside pattern
    public User getUserById(Long userId) {
        String cacheKey = "user:" + userId;
        
        // Try cache first
        User user = cacheService.get(cacheKey, User.class);
        if (user != null) {
            return user;
        }
        
        // Cache miss - load from database
        user = userService.findById(userId);
        if (user != null) {
            // Store in cache for 1 hour
            cacheService.put(cacheKey, user, 3600);
        }
        
        return user;
    }
    
    // Write-through pattern
    public User createUser(User user) {
        // Save to database
        User savedUser = userService.save(user);
        
        // Update cache immediately
        String cacheKey = "user:" + savedUser.getId();
        cacheService.put(cacheKey, savedUser, 3600);
        
        return savedUser;
    }
    
    // Write-behind pattern
    public void updateUserAsync(User user) {
        // Update cache immediately
        String cacheKey = "user:" + user.getId();
        cacheService.put(cacheKey, user, 3600);
        
        // Queue for database update
        asyncUpdateDatabase(user);
    }
    
    // Cache invalidation pattern
    public void deleteUser(Long userId) {
        // Delete from database
        userService.deleteById(userId);
        
        // Invalidate cache
        String cacheKey = "user:" + userId;
        cacheService.delete(cacheKey);
        
        // Clear related caches
        cacheService.delete("users:list");
        cacheService.delete("users:count");
    }
    
    private void asyncUpdateDatabase(User user) {
        // Asynchronous database update implementation
        CompletableFuture.runAsync(() -> {
            userService.save(user);
        });
    }
}
```

```java
// TuskLang caching patterns
caching_patterns: {
    # Cache-aside pattern
    get_user: (userId) => {
        cache_key: "user:" + @userId
        user: @cache.get(@cache_key)
        
        @if(!@user) {
            user: @query("SELECT * FROM users WHERE id = ?", [@userId])
            @if(@user) {
                @cache.set(@cache_key, @user, 3600)
            }
        }
        
        return @user
    }
    
    # Write-through pattern
    create_user: (userData) => {
        user: @User.create(@userData)
        cache_key: "user:" + @user.id
        @cache.set(@cache_key, @user, 3600)
        return @user
    }
    
    # Cache invalidation
    delete_user: (userId) => {
        @query("DELETE FROM users WHERE id = ?", [@userId])
        @cache.delete("user:" + @userId)
        @cache.delete("users:list")
        @cache.delete("users:count")
    }
}
```

## Cache Tags

```java
// Java cache tags
@Component
public class CacheTagsService {
    
    @Autowired
    private CacheService cacheService;
    
    public void setWithTags(String key, Object value, Set<String> tags, long ttl) {
        // Store the value
        cacheService.put(key, value, ttl);
        
        // Store tag associations
        for (String tag : tags) {
            String tagKey = "tag:" + tag;
            Set<String> taggedKeys = cacheService.get(tagKey, Set.class);
            if (taggedKeys == null) {
                taggedKeys = new HashSet<>();
            }
            taggedKeys.add(key);
            cacheService.put(tagKey, taggedKeys, ttl);
        }
    }
    
    public void invalidateByTag(String tag) {
        String tagKey = "tag:" + tag;
        Set<String> taggedKeys = cacheService.get(tagKey, Set.class);
        
        if (taggedKeys != null) {
            for (String key : taggedKeys) {
                cacheService.delete(key);
            }
            cacheService.delete(tagKey);
        }
    }
    
    public void invalidateByTags(Set<String> tags) {
        for (String tag : tags) {
            invalidateByTag(tag);
        }
    }
    
    public Set<String> getKeysByTag(String tag) {
        String tagKey = "tag:" + tag;
        return cacheService.get(tagKey, Set.class);
    }
}
```

```java
// TuskLang cache tags
cache_tags: {
    # Tag-based caching
    @cache.tags(["products", "homepage"]).set("featured_products", @products, 3600)
    
    # Get tagged cache
    products: @cache.tags(["products"]).get("featured_products")
    
    # Flush by tag
    @cache.tags(["products"]).flush()
    
    # Multiple tags
    @cache.tags(["user:123", "posts"]).set("user_posts", @posts)
    
    # Clear specific user's caches
    @cache.tags(["user:123"]).flush()
}
```

## Cache Invalidation

```java
// Java cache invalidation
@Component
public class CacheInvalidationService {
    
    @Autowired
    private CacheService cacheService;
    
    public void invalidateUserCaches(Long userId) {
        // Clear specific user caches
        cacheService.delete("user:" + userId);
        cacheService.delete("user:" + userId + ":profile");
        cacheService.delete("user:" + userId + ":posts");
        cacheService.delete("user:" + userId + ":friends");
        
        // Clear related caches
        cacheService.delete("users:list");
        cacheService.delete("users:count");
        cacheService.delete("users:active");
    }
    
    public void invalidateProductCaches(Long productId) {
        // Clear product-specific caches
        cacheService.delete("product:" + productId);
        cacheService.delete("product:" + productId + ":details");
        cacheService.delete("product:" + productId + ":reviews");
        
        // Clear category caches
        cacheService.delete("products:category:all");
        cacheService.delete("products:featured");
        cacheService.delete("products:recent");
    }
    
    public void invalidateAllCaches() {
        cacheService.clear();
    }
    
    public void invalidateByPattern(String pattern) {
        // Pattern-based invalidation (implementation depends on cache store)
        if (pattern.contains("*")) {
            // Use cache store specific pattern matching
            invalidateByPatternInternal(pattern);
        } else {
            cacheService.delete(pattern);
        }
    }
    
    private void invalidateByPatternInternal(String pattern) {
        // Implementation for pattern-based invalidation
        // This would depend on the specific cache store being used
    }
}
```

```java
// TuskLang cache invalidation
cache_invalidation: {
    # Update product and clear related caches
    update_product: (id, data) => {
        @query("UPDATE products SET ? WHERE id = ?", [@data, @id])
        
        # Clear specific caches
        @cache.delete("product:" + @id)
        @cache.delete("products:all")
        @cache.tags(["products", "category:" + @data.category_id]).flush()
        
        # Clear page caches
        @cache.delete("page:home")
        @cache.delete("page:category:" + @data.category_id)
    }
    
    # Time-based invalidation
    @cache.set("stats:daily", @daily_stats, @seconds_until_midnight())
    
    # Event-based invalidation
    on_order_placed: (order) => {
        @cache.delete("product:stock:" + @order.product_id)
        @cache.delete("user:orders:" + @order.user_id)
        @cache.increment("stats:orders:today")
    }
}
```

## Atomic Operations

```java
// Java atomic cache operations
@Component
public class AtomicCacheOperationsService {
    
    @Autowired
    private CacheService cacheService;
    
    public long increment(String key) {
        return increment(key, 1);
    }
    
    public long increment(String key, long delta) {
        String counterKey = "counter:" + key;
        Long currentValue = cacheService.get(counterKey, Long.class);
        long newValue = (currentValue != null ? currentValue : 0) + delta;
        cacheService.put(counterKey, newValue);
        return newValue;
    }
    
    public long decrement(String key) {
        return decrement(key, 1);
    }
    
    public long decrement(String key, long delta) {
        return increment(key, -delta);
    }
    
    public boolean addIfAbsent(String key, Object value, long ttl) {
        if (!cacheService.exists(key)) {
            cacheService.put(key, value, ttl);
            return true;
        }
        return false;
    }
    
    public boolean compareAndSet(String key, Object expectedValue, Object newValue) {
        Object currentValue = cacheService.get(key, Object.class);
        if (Objects.equals(currentValue, expectedValue)) {
            cacheService.put(key, newValue);
            return true;
        }
        return false;
    }
    
    public Object getAndSet(String key, Object newValue) {
        Object currentValue = cacheService.get(key, Object.class);
        cacheService.put(key, newValue);
        return currentValue;
    }
}
```

```java
// TuskLang atomic operations
atomic_operations: {
    # Increment/decrement
    @cache.increment("page_views")
    @cache.increment("product:views:123", 1)
    @cache.decrement("stock:product:456", 1)
    
    # Atomic add (only if doesn't exist)
    @if(@cache.add("lock:process", 1, 60)) {
        # Got the lock, process...
        @do_exclusive_operation()
        @cache.delete("lock:process")
    } else {
        # Another process has the lock
        error: "Process already running"
    }
    
    # Compare and swap
    current: @cache.get("counter")
    @cache.cas("counter", @current, @current + 1)
}
```

## Cache Warming

```java
// Java cache warming
@Component
public class CacheWarmingService {
    
    @Autowired
    private CacheService cacheService;
    
    @Autowired
    private UserService userService;
    
    @Autowired
    private ProductService productService;
    
    @PostConstruct
    public void warmCacheOnStartup() {
        warmUserCache();
        warmProductCache();
        warmConfigurationCache();
    }
    
    private void warmUserCache() {
        // Pre-load frequently accessed user data
        List<User> activeUsers = userService.findActiveUsers();
        for (User user : activeUsers) {
            String cacheKey = "user:" + user.getId();
            cacheService.put(cacheKey, user, 3600);
        }
        
        // Cache user count
        long userCount = userService.count();
        cacheService.put("users:count", userCount, 1800);
    }
    
    private void warmProductCache() {
        // Pre-load popular products
        List<Product> popularProducts = productService.findPopularProducts();
        cacheService.put("products:popular", popularProducts, 3600);
        
        // Pre-load product categories
        List<Category> categories = productService.findAllCategories();
        cacheService.put("categories:all", categories, 7200);
    }
    
    private void warmConfigurationCache() {
        // Pre-load application configuration
        Map<String, Object> config = loadConfiguration();
        cacheService.put("app:config", config, 3600);
    }
    
    @Scheduled(fixedRate = 3600000) // Every hour
    public void scheduledCacheWarming() {
        warmPopularData();
        warmUserStats();
    }
    
    private void warmPopularData() {
        // Refresh popular data cache
        List<Product> popularProducts = productService.findPopularProducts();
        cacheService.put("products:popular", popularProducts, 3600);
    }
    
    private void warmUserStats() {
        // Refresh user statistics
        Map<String, Object> userStats = userService.getStatistics();
        cacheService.put("users:stats", userStats, 1800);
    }
    
    private Map<String, Object> loadConfiguration() {
        // Load application configuration
        return Map.of(
            "app_name", "MyApp",
            "version", "1.0.0",
            "environment", "production"
        );
    }
}
```

```java
// TuskLang cache warming
cache_warming: {
    # Warm cache on startup
    warm_cache: {
        # Pre-load frequently accessed data
        categories: @query("SELECT * FROM categories WHERE active = 1")
        @cache.set("categories:all", @categories, 86400)  # 24 hours
        
        # Pre-load configuration
        config: @load_config()
        @cache.set("app:config", @config, 3600)
        
        # Pre-generate expensive computations
        @foreach(@categories as @category) {
            products: @query("SELECT * FROM products WHERE category_id = ?", [@category.id])
            @cache.set("products:category:" + @category.id, @products, 3600)
        }
    }
    
    # Scheduled cache warming
    #cron "0 * * * *" {
        # Refresh cache every hour
        @warm_popular_products_cache()
        @warm_user_stats_cache()
    }
}
```

## Cache Testing

```java
// JUnit test for caching
@SpringBootTest
class CacheServiceTest {
    
    @Autowired
    private CacheService cacheService;
    
    @Test
    void testBasicCaching() {
        String key = "test_key";
        String value = "test_value";
        
        // Test put and get
        cacheService.put(key, value);
        String retrievedValue = cacheService.get(key, String.class);
        
        assertThat(retrievedValue).isEqualTo(value);
    }
    
    @Test
    void testCacheWithDefaultValue() {
        String key = "non_existent_key";
        String defaultValue = "default_value";
        
        String value = cacheService.get(key, String.class, defaultValue);
        
        assertThat(value).isEqualTo(defaultValue);
    }
    
    @Test
    void testCacheExpiration() throws InterruptedException {
        String key = "expiring_key";
        String value = "expiring_value";
        
        // Set with short TTL
        cacheService.put(key, value, 1);
        
        // Should exist immediately
        assertThat(cacheService.exists(key)).isTrue();
        
        // Wait for expiration
        Thread.sleep(2000);
        
        // Should not exist after expiration
        assertThat(cacheService.exists(key)).isFalse();
    }
    
    @Test
    void testCacheDeletion() {
        String key = "delete_key";
        String value = "delete_value";
        
        cacheService.put(key, value);
        assertThat(cacheService.exists(key)).isTrue();
        
        cacheService.delete(key);
        assertThat(cacheService.exists(key)).isFalse();
    }
    
    @Test
    void testCacheClear() {
        cacheService.put("key1", "value1");
        cacheService.put("key2", "value2");
        
        assertThat(cacheService.exists("key1")).isTrue();
        assertThat(cacheService.exists("key2")).isTrue();
        
        cacheService.clear();
        
        assertThat(cacheService.exists("key1")).isFalse();
        assertThat(cacheService.exists("key2")).isFalse();
    }
}
```

```java
// TuskLang cache testing
test_cache: {
    # Test basic caching
    test_key: "test_key"
    test_value: "test_value"
    @cache.set(@test_key, @test_value, 3600)
    cached_value: @cache.get(@test_key)
    assert(@cached_value == @test_value, "Should retrieve cached value")
    
    # Test cache with default
    non_existent: @cache.get("non_existent", "default_value")
    assert(@non_existent == "default_value", "Should return default value")
    
    # Test cache deletion
    @cache.set("delete_key", "delete_value", 3600)
    @cache.delete("delete_key")
    deleted_value: @cache.get("delete_key")
    assert(@deleted_value == null, "Should return null after deletion")
    
    # Test cache expiration
    @cache.set("expire_key", "expire_value", 1)
    @sleep(2)
    expired_value: @cache.get("expire_key")
    assert(@expired_value == null, "Should return null after expiration")
}
```

## Best Practices

### 1. Cache Key Design
```java
// Design cache keys for optimal performance
@Component
public class CacheKeyDesignService {
    
    @Autowired
    private CacheService cacheService;
    
    public String generateUserKey(Long userId) {
        return String.format("user:%d", userId);
    }
    
    public String generateUserProfileKey(Long userId) {
        return String.format("user:%d:profile", userId);
    }
    
    public String generateProductKey(Long productId) {
        return String.format("product:%d", productId);
    }
    
    public String generateProductListKey(String category, int page, int size) {
        return String.format("products:category:%s:page:%d:size:%d", category, page, size);
    }
    
    public String generateSearchKey(String query, String filters, int page) {
        String filterHash = generateFilterHash(filters);
        return String.format("search:query:%s:filters:%s:page:%d", query, filterHash, page);
    }
    
    private String generateFilterHash(String filters) {
        // Generate hash for filters to keep key length manageable
        return String.valueOf(filters.hashCode());
    }
}
```

### 2. Cache Performance Monitoring
```java
// Monitor cache performance
@Component
public class CachePerformanceMonitor {
    
    @Autowired
    private CacheService cacheService;
    
    @Autowired
    private MeterRegistry meterRegistry;
    
    public void recordCacheHit(String cacheName) {
        Counter.builder("cache.hits")
            .tag("cache", cacheName)
            .register(meterRegistry)
            .increment();
    }
    
    public void recordCacheMiss(String cacheName) {
        Counter.builder("cache.misses")
            .tag("cache", cacheName)
            .register(meterRegistry)
            .increment();
    }
    
    public void recordCacheSize(String cacheName, long size) {
        Gauge.builder("cache.size")
            .tag("cache", cacheName)
            .register(meterRegistry, () -> size);
    }
    
    public double getHitRate(String cacheName) {
        Counter hits = Counter.builder("cache.hits")
            .tag("cache", cacheName)
            .register(meterRegistry);
        
        Counter misses = Counter.builder("cache.misses")
            .tag("cache", cacheName)
            .register(meterRegistry);
        
        double total = hits.count() + misses.count();
        return total > 0 ? hits.count() / total : 0.0;
    }
}
```

### 3. Cache Security
```java
// Implement cache security measures
@Component
public class CacheSecurityService {
    
    @Autowired
    private CacheService cacheService;
    
    public void setSecureCache(String key, Object value, long ttl) {
        // Validate key format
        if (!isValidKey(key)) {
            throw new IllegalArgumentException("Invalid cache key format: " + key);
        }
        
        // Sanitize value
        Object sanitizedValue = sanitizeValue(value);
        
        // Set with encryption if needed
        if (isSensitiveData(key)) {
            sanitizedValue = encryptValue(sanitizedValue);
        }
        
        cacheService.put(key, sanitizedValue, ttl);
    }
    
    public Object getSecureCache(String key, Class<?> type) {
        Object value = cacheService.get(key, type);
        
        if (value != null && isSensitiveData(key)) {
            value = decryptValue(value);
        }
        
        return value;
    }
    
    private boolean isValidKey(String key) {
        // Validate key format (no special characters, reasonable length)
        return key != null && key.matches("^[a-zA-Z0-9:_-]+$") && key.length() <= 250;
    }
    
    private Object sanitizeValue(Object value) {
        // Sanitize cache value to prevent injection attacks
        if (value instanceof String) {
            return ((String) value).replaceAll("[<>\"']", "");
        }
        return value;
    }
    
    private boolean isSensitiveData(String key) {
        return key.contains("password") || key.contains("token") || key.contains("secret");
    }
    
    private Object encryptValue(Object value) {
        // Implementation for value encryption
        return value; // Placeholder
    }
    
    private Object decryptValue(Object value) {
        // Implementation for value decryption
        return value; // Placeholder
    }
}
```

The `@cache` operator in Java provides comprehensive caching capabilities that enable applications to improve performance, reduce database load, and enhance user experience through intelligent data caching strategies. 