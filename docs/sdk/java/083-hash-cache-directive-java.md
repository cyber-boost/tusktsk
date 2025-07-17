# #cache - Caching Directive (Java)

The `#cache` directive provides enterprise-grade caching capabilities for Java applications, enabling high-performance data storage and retrieval with Spring Boot integration and Redis backend support.

## Basic Syntax

```tusk
# Basic caching - 5 minute TTL
#cache 5m {
    #api /data {
        return @fetch_expensive_data()
    }
}

# Cache with custom key
#cache 1h key: @request.user.id {
    #api /user-profile {
        return @get_user_profile(@request.user.id)
    }
}

# Cache with conditions
#cache 30m if: @request.query.cache != "false" {
    #api /search {
        return @search_database(@request.query.q)
    }
}
```

## Java Implementation

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.directives.CacheDirective;
import org.springframework.web.bind.annotation.*;
import org.springframework.stereotype.Controller;
import org.springframework.cache.annotation.Cacheable;
import org.springframework.cache.annotation.CacheEvict;

@Controller
public class CachedController {
    
    private final TuskLang tuskLang;
    private final CacheDirective cacheDirective;
    private final DataService dataService;
    
    public CachedController(TuskLang tuskLang, DataService dataService) {
        this.tuskLang = tuskLang;
        this.cacheDirective = new CacheDirective();
        this.dataService = dataService;
    }
    
    // Basic caching with Spring annotations
    @GetMapping("/api/data")
    @Cacheable(value = "data", key = "#root.methodName", unless = "#result == null")
    public ResponseEntity<DataResponse> getData() {
        return ResponseEntity.ok(dataService.fetchExpensiveData());
    }
    
    // Cache with custom key
    @GetMapping("/api/user-profile/{userId}")
    @Cacheable(value = "user-profiles", key = "#userId", unless = "#result == null")
    public ResponseEntity<UserProfile> getUserProfile(@PathVariable Long userId) {
        return ResponseEntity.ok(dataService.getUserProfile(userId));
    }
    
    // Cache with conditions
    @GetMapping("/api/search")
    public ResponseEntity<SearchResults> search(
            @RequestParam String q,
            @RequestParam(required = false) String cache,
            HttpServletRequest request) {
        
        if ("false".equals(cache)) {
            // Skip cache
            return ResponseEntity.ok(dataService.searchDatabase(q));
        }
        
        // Use cache
        String cacheKey = "search:" + q;
        SearchResults results = cacheDirective.get(cacheKey);
        
        if (results == null) {
            results = dataService.searchDatabase(q);
            cacheDirective.set(cacheKey, results, Duration.ofMinutes(30));
        }
        
        return ResponseEntity.ok(results);
    }
}
```

## Cache Configuration

```tusk
# Detailed cache configuration
#cache {
    ttl: 3600           # Time to live in seconds
    key: @request.path  # Cache key
    condition: @request.method == "GET"  # When to cache
    tags: ["api", "data"]  # Cache tags for invalidation
} {
    #api /endpoint {
        @process_request()
    }
}

# Multiple cache strategies
#cache {
    strategies: [
        {ttl: 300, key: "short-term"},
        {ttl: 3600, key: "medium-term"},
        {ttl: 86400, key: "long-term"}
    ]
    strategy: @select_strategy(@request.path)
} {
    #api /* {
        @handle_request()
    }
}
```

## Java Cache Configuration

```java
import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.stereotype.Component;
import org.springframework.cache.CacheManager;
import org.springframework.cache.annotation.EnableCaching;
import org.springframework.context.annotation.Bean;
import org.springframework.data.redis.cache.RedisCacheConfiguration;
import org.springframework.data.redis.cache.RedisCacheManager;
import org.springframework.data.redis.connection.RedisConnectionFactory;
import java.time.Duration;
import java.util.Map;
import java.util.HashMap;

@Component
@ConfigurationProperties(prefix = "tusk.cache")
public class CacheConfig {
    
    private int defaultTtl = 3600;
    private String defaultKey = "default";
    private Map<String, CacheStrategy> strategies;
    private boolean enabled = true;
    
    // Getters and setters
    public int getDefaultTtl() { return defaultTtl; }
    public void setDefaultTtl(int defaultTtl) { this.defaultTtl = defaultTtl; }
    
    public String getDefaultKey() { return defaultKey; }
    public void setDefaultKey(String defaultKey) { this.defaultKey = defaultKey; }
    
    public Map<String, CacheStrategy> getStrategies() { return strategies; }
    public void setStrategies(Map<String, CacheStrategy> strategies) { this.strategies = strategies; }
    
    public boolean isEnabled() { return enabled; }
    public void setEnabled(boolean enabled) { this.enabled = enabled; }
    
    public static class CacheStrategy {
        private int ttl;
        private String key;
        private String condition;
        private List<String> tags;
        
        // Getters and setters
        public int getTtl() { return ttl; }
        public void setTtl(int ttl) { this.ttl = ttl; }
        
        public String getKey() { return key; }
        public void setKey(String key) { this.key = key; }
        
        public String getCondition() { return condition; }
        public void setCondition(String condition) { this.condition = condition; }
        
        public List<String> getTags() { return tags; }
        public void setTags(List<String> tags) { this.tags = tags; }
    }
}

@Configuration
@EnableCaching
public class CacheConfiguration {
    
    @Bean
    public CacheManager cacheManager(RedisConnectionFactory connectionFactory,
                                   CacheConfig cacheConfig) {
        
        RedisCacheConfiguration defaultConfig = RedisCacheConfiguration.defaultCacheConfig()
            .entryTtl(Duration.ofSeconds(cacheConfig.getDefaultTtl()))
            .serializeKeysWith(RedisSerializationContext.SerializationPair
                .fromSerializer(new StringRedisSerializer()))
            .serializeValuesWith(RedisSerializationContext.SerializationPair
                .fromSerializer(new GenericJackson2JsonRedisSerializer()));
        
        Map<String, RedisCacheConfiguration> cacheConfigurations = new HashMap<>();
        
        // Configure different cache strategies
        if (cacheConfig.getStrategies() != null) {
            for (Map.Entry<String, CacheConfig.CacheStrategy> entry : 
                 cacheConfig.getStrategies().entrySet()) {
                
                CacheConfig.CacheStrategy strategy = entry.getValue();
                RedisCacheConfiguration config = defaultConfig
                    .entryTtl(Duration.ofSeconds(strategy.getTtl()));
                
                cacheConfigurations.put(entry.getKey(), config);
            }
        }
        
        return RedisCacheManager.builder(connectionFactory)
            .cacheDefaults(defaultConfig)
            .withInitialCacheConfigurations(cacheConfigurations)
            .build();
    }
}
```

## Cache Key Generation

```tusk
# Dynamic cache keys
#cache 1h key: @generate_cache_key(@request.path, @request.query) {
    #api /dynamic-data {
        return @fetch_data(@request.query.params)
    }
}

# User-specific caching
#cache 30m key: "user-{@auth.id}-{@request.path}" {
    #api /user-data {
        return @get_user_specific_data(@auth.id)
    }
}

# Complex key generation
#cache 1h key: @hash(@request.path + @request.query + @auth.role) {
    #api /complex-endpoint {
        return @process_complex_request()
    }
}
```

## Java Cache Key Generation

```java
import org.springframework.stereotype.Service;
import java.security.MessageDigest;
import java.util.stream.Collectors;

@Service
public class CacheKeyGenerator {
    
    public String generateKey(String path, Map<String, String> queryParams) {
        StringBuilder keyBuilder = new StringBuilder(path);
        
        if (queryParams != null && !queryParams.isEmpty()) {
            keyBuilder.append("?");
            String queryString = queryParams.entrySet().stream()
                .map(entry -> entry.getKey() + "=" + entry.getValue())
                .sorted()
                .collect(Collectors.joining("&"));
            keyBuilder.append(queryString);
        }
        
        return keyBuilder.toString();
    }
    
    public String generateUserKey(Long userId, String path) {
        return String.format("user-%d-%s", userId, path);
    }
    
    public String generateComplexKey(String path, Map<String, String> queryParams, String role) {
        StringBuilder input = new StringBuilder(path);
        
        if (queryParams != null) {
            queryParams.entrySet().stream()
                .sorted(Map.Entry.comparingByKey())
                .forEach(entry -> input.append(entry.getKey()).append(entry.getValue()));
        }
        
        input.append(role);
        
        return hashString(input.toString());
    }
    
    private String hashString(String input) {
        try {
            MessageDigest digest = MessageDigest.getInstance("SHA-256");
            byte[] hash = digest.digest(input.getBytes("UTF-8"));
            StringBuilder hexString = new StringBuilder();
            
            for (byte b : hash) {
                String hex = Integer.toHexString(0xff & b);
                if (hex.length() == 1) {
                    hexString.append('0');
                }
                hexString.append(hex);
            }
            
            return hexString.toString();
        } catch (Exception e) {
            throw new RuntimeException("Failed to hash string", e);
        }
    }
}

@RestController
public class DynamicCacheController {
    
    private final CacheKeyGenerator keyGenerator;
    private final CacheService cacheService;
    private final DataService dataService;
    
    public DynamicCacheController(CacheKeyGenerator keyGenerator,
                                CacheService cacheService,
                                DataService dataService) {
        this.keyGenerator = keyGenerator;
        this.cacheService = cacheService;
        this.dataService = dataService;
    }
    
    @GetMapping("/api/dynamic-data")
    public ResponseEntity<DataResponse> getDynamicData(
            @RequestParam Map<String, String> queryParams,
            HttpServletRequest request) {
        
        String path = request.getRequestURI();
        String cacheKey = keyGenerator.generateKey(path, queryParams);
        
        DataResponse data = cacheService.get(cacheKey, DataResponse.class);
        if (data == null) {
            data = dataService.fetchData(queryParams);
            cacheService.set(cacheKey, data, Duration.ofHours(1));
        }
        
        return ResponseEntity.ok(data);
    }
    
    @GetMapping("/api/user-data")
    public ResponseEntity<UserDataResponse> getUserData(
            @AuthenticationPrincipal User user,
            HttpServletRequest request) {
        
        String path = request.getRequestURI();
        String cacheKey = keyGenerator.generateUserKey(user.getId(), path);
        
        UserDataResponse data = cacheService.get(cacheKey, UserDataResponse.class);
        if (data == null) {
            data = dataService.getUserSpecificData(user.getId());
            cacheService.set(cacheKey, data, Duration.ofMinutes(30));
        }
        
        return ResponseEntity.ok(data);
    }
}
```

## Cache Invalidation

```tusk
# Automatic cache invalidation
#cache 1h tags: ["users", "profiles"] {
    #api /user-profile {
        return @get_user_profile(@request.user.id)
    }
}

# Manual invalidation
#cache_evict tags: ["users"] {
    #api /update-user {
        @update_user_data(@request.user.id)
        @invalidate_cache("users")
    }
}

# Conditional invalidation
#cache_evict if: @request.method == "POST" tags: ["data"] {
    #api /data {
        @update_data()
    }
}
```

## Java Cache Invalidation

```java
import org.springframework.cache.annotation.CacheEvict;
import org.springframework.cache.annotation.CachePut;
import org.springframework.stereotype.Service;

@Service
public class CacheInvalidationService {
    
    private final CacheService cacheService;
    
    public CacheInvalidationService(CacheService cacheService) {
        this.cacheService = cacheService;
    }
    
    // Automatic cache eviction with Spring annotations
    @CacheEvict(value = "user-profiles", key = "#userId")
    public void updateUserProfile(Long userId, UserProfile profile) {
        // Update user profile
        userRepository.save(profile);
    }
    
    // Evict by tags
    @CacheEvict(value = "users", allEntries = true)
    public void invalidateAllUserCaches() {
        // This will evict all entries in the "users" cache
    }
    
    // Conditional eviction
    @CacheEvict(value = "data", condition = "#result != null")
    public DataResponse updateData(DataRequest request) {
        DataResponse response = dataService.updateData(request);
        return response;
    }
    
    // Manual cache invalidation
    public void invalidateByTag(String tag) {
        cacheService.evictByTag(tag);
    }
    
    public void invalidateByPattern(String pattern) {
        cacheService.evictByPattern(pattern);
    }
}

@RestController
public class CacheInvalidationController {
    
    private final CacheInvalidationService invalidationService;
    private final UserService userService;
    
    public CacheInvalidationController(CacheInvalidationService invalidationService,
                                     UserService userService) {
        this.invalidationService = invalidationService;
        this.userService = userService;
    }
    
    @PostMapping("/api/update-user")
    public ResponseEntity<UserProfile> updateUser(
            @RequestBody UserProfile profile,
            @AuthenticationPrincipal User user) {
        
        // This will automatically evict the cache
        invalidationService.updateUserProfile(user.getId(), profile);
        
        return ResponseEntity.ok(profile);
    }
    
    @PostMapping("/api/invalidate-cache")
    public ResponseEntity<String> invalidateCache(@RequestParam String tag) {
        invalidationService.invalidateByTag(tag);
        return ResponseEntity.ok("Cache invalidated");
    }
}
```

## Cache Strategies

```tusk
# Write-through caching
#cache 1h strategy: "write-through" {
    #api /data {
        data: @fetch_data()
        @cache_write("data", data)
        return data
    }
}

# Write-behind caching
#cache 1h strategy: "write-behind" {
    #api /data {
        data: @fetch_data()
        @cache_queue_write("data", data)
        return data
    }
}

# Cache-aside pattern
#cache 1h strategy: "cache-aside" {
    #api /data {
        cached: @cache_get("data")
        if (cached) return cached
        
        data: @fetch_data()
        @cache_set("data", data)
        return data
    }
}
```

## Java Cache Strategies

```java
import org.springframework.cache.annotation.CachePut;
import org.springframework.cache.annotation.Cacheable;
import org.springframework.stereotype.Service;
import java.util.concurrent.CompletableFuture;

@Service
public class CacheStrategyService {
    
    private final CacheService cacheService;
    private final DataService dataService;
    private final AsyncTaskExecutor taskExecutor;
    
    public CacheStrategyService(CacheService cacheService,
                              DataService dataService,
                              AsyncTaskExecutor taskExecutor) {
        this.cacheService = cacheService;
        this.dataService = dataService;
        this.taskExecutor = taskExecutor;
    }
    
    // Write-through strategy
    @CachePut(value = "data", key = "#data.id")
    public DataResponse writeThrough(DataRequest request) {
        DataResponse data = dataService.fetchData(request);
        // Cache is automatically updated
        return data;
    }
    
    // Write-behind strategy
    public DataResponse writeBehind(DataRequest request) {
        DataResponse data = dataService.fetchData(request);
        
        // Queue the write operation
        CompletableFuture.runAsync(() -> {
            cacheService.set("data:" + data.getId(), data, Duration.ofHours(1));
        }, taskExecutor);
        
        return data;
    }
    
    // Cache-aside pattern
    public DataResponse cacheAside(String dataId) {
        // Try to get from cache first
        DataResponse cached = cacheService.get("data:" + dataId, DataResponse.class);
        if (cached != null) {
            return cached;
        }
        
        // Fetch from database
        DataResponse data = dataService.fetchDataById(dataId);
        
        // Store in cache
        cacheService.set("data:" + dataId, data, Duration.ofHours(1));
        
        return data;
    }
    
    // Read-through strategy
    @Cacheable(value = "data", key = "#dataId", unless = "#result == null")
    public DataResponse readThrough(String dataId) {
        return dataService.fetchDataById(dataId);
    }
}
```

## Distributed Caching

```tusk
# Distributed cache configuration
#cache {
    ttl: 3600
    distributed: true
    nodes: ["redis-1:6379", "redis-2:6379", "redis-3:6379"]
    strategy: "consistent-hashing"
} {
    #api /distributed-data {
        return @fetch_distributed_data()
    }
}

# Cache replication
#cache {
    ttl: 3600
    replication: true
    master: "redis-master:6379"
    slaves: ["redis-slave-1:6379", "redis-slave-2:6379"]
} {
    #api /replicated-data {
        return @fetch_replicated_data()
    }
}
```

## Java Distributed Caching

```java
import org.springframework.data.redis.connection.RedisClusterConfiguration;
import org.springframework.data.redis.connection.RedisSentinelConfiguration;
import org.springframework.data.redis.connection.lettuce.LettuceConnectionFactory;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

@Configuration
public class DistributedCacheConfiguration {
    
    @Bean
    public RedisConnectionFactory redisClusterConnectionFactory() {
        RedisClusterConfiguration clusterConfig = new RedisClusterConfiguration();
        clusterConfig.clusterNode("redis-1", 6379);
        clusterConfig.clusterNode("redis-2", 6379);
        clusterConfig.clusterNode("redis-3", 6379);
        
        return new LettuceConnectionFactory(clusterConfig);
    }
    
    @Bean
    public RedisConnectionFactory redisSentinelConnectionFactory() {
        RedisSentinelConfiguration sentinelConfig = new RedisSentinelConfiguration()
            .master("mymaster")
            .sentinel("redis-sentinel-1", 26379)
            .sentinel("redis-sentinel-2", 26379)
            .sentinel("redis-sentinel-3", 26379);
        
        return new LettuceConnectionFactory(sentinelConfig);
    }
    
    @Bean
    public CacheManager distributedCacheManager(RedisConnectionFactory connectionFactory) {
        RedisCacheConfiguration config = RedisCacheConfiguration.defaultCacheConfig()
            .entryTtl(Duration.ofHours(1))
            .serializeKeysWith(RedisSerializationContext.SerializationPair
                .fromSerializer(new StringRedisSerializer()))
            .serializeValuesWith(RedisSerializationContext.SerializationPair
                .fromSerializer(new GenericJackson2JsonRedisSerializer()));
        
        return RedisCacheManager.builder(connectionFactory)
            .cacheDefaults(config)
            .transactionAware()
            .build();
    }
}

@Service
public class DistributedCacheService {
    
    private final RedisTemplate<String, Object> redisTemplate;
    
    public DistributedCacheService(RedisTemplate<String, Object> redisTemplate) {
        this.redisTemplate = redisTemplate;
    }
    
    public <T> T get(String key, Class<T> type) {
        return (T) redisTemplate.opsForValue().get(key);
    }
    
    public void set(String key, Object value, Duration ttl) {
        redisTemplate.opsForValue().set(key, value, ttl);
    }
    
    public void delete(String key) {
        redisTemplate.delete(key);
    }
    
    public boolean hasKey(String key) {
        return redisTemplate.hasKey(key);
    }
    
    public Set<String> keys(String pattern) {
        return redisTemplate.keys(pattern);
    }
}
```

## Cache Performance Monitoring

```java
import io.micrometer.core.instrument.MeterRegistry;
import io.micrometer.core.instrument.Counter;
import io.micrometer.core.instrument.Timer;

@Service
public class MonitoredCacheService {
    
    private final CacheService cacheService;
    private final Counter cacheHits;
    private final Counter cacheMisses;
    private final Timer cacheGetTimer;
    private final Timer cacheSetTimer;
    
    public MonitoredCacheService(CacheService cacheService, MeterRegistry meterRegistry) {
        this.cacheService = cacheService;
        this.cacheHits = Counter.builder("cache.hits")
            .description("Number of cache hits")
            .register(meterRegistry);
        this.cacheMisses = Counter.builder("cache.misses")
            .description("Number of cache misses")
            .register(meterRegistry);
        this.cacheGetTimer = Timer.builder("cache.get.duration")
            .description("Time taken to get from cache")
            .register(meterRegistry);
        this.cacheSetTimer = Timer.builder("cache.set.duration")
            .description("Time taken to set in cache")
            .register(meterRegistry);
    }
    
    public <T> T getWithMetrics(String key, Class<T> type) {
        return cacheGetTimer.record(() -> {
            T value = cacheService.get(key, type);
            
            if (value != null) {
                cacheHits.increment();
            } else {
                cacheMisses.increment();
            }
            
            return value;
        });
    }
    
    public void setWithMetrics(String key, Object value, Duration ttl) {
        cacheSetTimer.record(() -> {
            cacheService.set(key, value, ttl);
        });
    }
    
    public double getHitRate() {
        double total = cacheHits.count() + cacheMisses.count();
        return total > 0 ? cacheHits.count() / total : 0.0;
    }
}
```

## Cache Testing

```java
import org.junit.jupiter.api.Test;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.test.context.TestPropertySource;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.mock.mockito.MockBean;

@SpringBootTest
@TestPropertySource(properties = {
    "spring.cache.type=simple",
    "tusk.cache.default-ttl=60"
})
public class CacheTest {
    
    @Autowired
    private CacheService cacheService;
    
    @MockBean
    private DataService dataService;
    
    @Test
    public void testBasicCaching() {
        String key = "test-key";
        String value = "test-value";
        
        // Set value
        cacheService.set(key, value, Duration.ofMinutes(1));
        
        // Get value
        String retrieved = cacheService.get(key, String.class);
        assertEquals(value, retrieved);
    }
    
    @Test
    public void testCacheExpiration() throws InterruptedException {
        String key = "expiring-key";
        String value = "expiring-value";
        
        // Set with short TTL
        cacheService.set(key, value, Duration.ofMillis(100));
        
        // Should be available immediately
        assertNotNull(cacheService.get(key, String.class));
        
        // Wait for expiration
        Thread.sleep(150);
        
        // Should be null after expiration
        assertNull(cacheService.get(key, String.class));
    }
    
    @Test
    public void testCacheInvalidation() {
        String key = "invalidation-key";
        String value = "invalidation-value";
        
        // Set value
        cacheService.set(key, value, Duration.ofMinutes(1));
        assertNotNull(cacheService.get(key, String.class));
        
        // Invalidate
        cacheService.delete(key);
        assertNull(cacheService.get(key, String.class));
    }
}
```

## Configuration Properties

```yaml
# application.yml
tusk:
  cache:
    enabled: true
    default-ttl: 3600
    default-key: "default"
    
    strategies:
      short-term:
        ttl: 300
        key: "short"
        tags: ["temp"]
      medium-term:
        ttl: 3600
        key: "medium"
        tags: ["data"]
      long-term:
        ttl: 86400
        key: "long"
        tags: ["persistent"]
    
    redis:
      host: localhost
      port: 6379
      database: 0
      timeout: 2000
      pool:
        max-active: 8
        max-idle: 8
        min-idle: 0
        max-wait: -1

spring:
  cache:
    type: redis
    redis:
      time-to-live: 3600000
      cache-null-values: false
      use-key-prefix: true
      key-prefix: "tusk:"
```

## Summary

The `#cache` directive in TuskLang provides comprehensive caching capabilities for Java applications. With Spring Boot integration, Redis backend, and advanced caching strategies, you can implement high-performance caching that significantly improves application performance.

Key features include:
- **Multiple caching strategies**: Write-through, write-behind, cache-aside, and read-through
- **Spring Boot integration**: Seamless integration with Spring Boot caching annotations
- **Redis backend**: High-performance, distributed caching
- **Flexible key generation**: Dynamic and complex cache key generation
- **Cache invalidation**: Automatic and manual cache invalidation strategies
- **Distributed caching**: Support for Redis clusters and sentinel configurations
- **Performance monitoring**: Built-in metrics and monitoring capabilities
- **Testing support**: Comprehensive testing utilities

The Java implementation provides enterprise-grade caching that scales with your application while maintaining the simplicity and power of TuskLang's declarative syntax. 