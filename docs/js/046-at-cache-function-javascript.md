# @cache Function - JavaScript

## Overview

The `@cache` function in TuskLang provides intelligent caching capabilities for expensive operations, database queries, and computed values. This is essential for JavaScript applications that need to optimize performance by reducing redundant computations and database calls.

## Basic Syntax

```tsk
# Simple caching with duration
cached_user_count: @cache("5m", @query("SELECT COUNT(*) as count FROM users"))
cached_config: @cache("1h", @json('{"api_key": "abc123", "endpoint": "https://api.example.com"}'))

# Cache with custom key
user_profile_cache: @cache("10m", @query("SELECT * FROM users WHERE id = ?", [$user_id]), "user_profile_$user_id")
```

## JavaScript Integration

### Node.js Caching Integration

```javascript
const tusk = require('tusklang');

// Load configuration with cached values
const config = tusk.load('cache.tsk');

// Access cached data
console.log(config.cached_user_count); // { count: 150 }
console.log(config.cached_config); // { api_key: "abc123", endpoint: "https://api.example.com" }

// Use cached data in application
const totalUsers = config.cached_user_count.count;
const apiConfig = config.cached_config;

// Dynamic caching
const expensiveOperation = async () => {
  // Simulate expensive operation
  await new Promise(resolve => setTimeout(resolve, 2000));
  return { result: 'expensive_data' };
};

const cachedResult = await tusk.cache("5m", expensiveOperation());
```

### Browser Caching Integration

```javascript
// Load TuskLang configuration
const config = await tusk.load('cache.tsk');

// Use cached data in frontend
class CacheManager {
  constructor() {
    this.userCount = config.cached_user_count.count;
    this.apiConfig = config.cached_config;
  }
  
  async getCachedData(key, operation, duration = "5m") {
    try {
      const cachedResult = await tusk.cache(duration, operation(), key);
      return cachedResult;
    } catch (error) {
      console.error('Cache error:', error);
      return await operation();
    }
  }
  
  async getUserProfile(userId) {
    return await this.getCachedData(
      `user_profile_${userId}`,
      () => fetch(`/api/users/${userId}`).then(r => r.json()),
      "10m"
    );
  }
}
```

## Advanced Usage

### Conditional Caching

```tsk
# Cache based on conditions
conditional_cache: @cache("5m", @query("SELECT * FROM users WHERE status = ?", ["active"]), "active_users", {"condition": "cache_enabled"})

# Environment-specific caching
prod_cache: @cache("1h", @query("SELECT COUNT(*) FROM orders"), "order_count", {"environment": "production"})
dev_cache: @cache("1m", @query("SELECT COUNT(*) FROM orders"), "order_count", {"environment": "development"})
```

### Cache Invalidation

```tsk
# Cache with invalidation triggers
user_data_cache: @cache("30m", @query("SELECT * FROM users WHERE id = ?", [$user_id]), "user_$user_id", {"invalidate_on": ["user_update", "user_delete"]})

# Cache with dependencies
order_cache: @cache("15m", @query("SELECT * FROM orders WHERE user_id = ?", [$user_id]), "orders_$user_id", {"dependencies": ["user_$user_id"]})
```

### Multi-level Caching

```tsk
# Memory and disk caching
fast_cache: @cache("1m", @query("SELECT * FROM settings"), "settings", {"level": "memory"})
persistent_cache: @cache("1h", @query("SELECT * FROM products"), "products", {"level": "disk"})
distributed_cache: @cache("30m", @query("SELECT * FROM analytics"), "analytics", {"level": "redis"})
```

## JavaScript Implementation

### Custom Cache Manager

```javascript
class TuskCacheManager {
  constructor() {
    this.memoryCache = new Map();
    this.diskCache = new Map();
    this.redisCache = null;
    this.invalidationCallbacks = new Map();
  }
  
  async cache(duration, operation, key = null, options = {}) {
    const cacheKey = key || this.generateKey(operation);
    const cacheLevel = options.level || 'memory';
    
    // Check if cached value exists and is valid
    const cached = await this.getCachedValue(cacheKey, cacheLevel);
    if (cached && !this.isExpired(cached)) {
      return cached.value;
    }
    
    // Execute operation and cache result
    const result = await operation();
    await this.setCachedValue(cacheKey, result, duration, cacheLevel, options);
    
    return result;
  }
  
  async getCachedValue(key, level) {
    switch (level) {
      case 'memory':
        return this.memoryCache.get(key);
      case 'disk':
        return this.diskCache.get(key);
      case 'redis':
        return await this.redisCache.get(key);
      default:
        return this.memoryCache.get(key);
    }
  }
  
  async setCachedValue(key, value, duration, level, options) {
    const cacheEntry = {
      value,
      timestamp: Date.now(),
      duration: this.parseDuration(duration),
      options
    };
    
    switch (level) {
      case 'memory':
        this.memoryCache.set(key, cacheEntry);
        break;
      case 'disk':
        this.diskCache.set(key, cacheEntry);
        break;
      case 'redis':
        await this.redisCache.set(key, JSON.stringify(cacheEntry), 'EX', this.parseDuration(duration));
        break;
    }
    
    // Set up invalidation callbacks
    if (options.invalidate_on) {
      this.setupInvalidation(key, options.invalidate_on);
    }
  }
  
  isExpired(cacheEntry) {
    return Date.now() - cacheEntry.timestamp > cacheEntry.duration;
  }
  
  parseDuration(duration) {
    const units = {
      's': 1000,
      'm': 60 * 1000,
      'h': 60 * 60 * 1000,
      'd': 24 * 60 * 60 * 1000
    };
    
    const match = duration.match(/^(\d+)([smhd])$/);
    if (match) {
      const [, value, unit] = match;
      return parseInt(value) * units[unit];
    }
    
    return 5 * 60 * 1000; // Default 5 minutes
  }
  
  generateKey(operation) {
    return `cache_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }
  
  setupInvalidation(key, events) {
    events.forEach(event => {
      if (!this.invalidationCallbacks.has(event)) {
        this.invalidationCallbacks.set(event, []);
      }
      this.invalidationCallbacks.get(event).push(key);
    });
  }
  
  invalidate(event) {
    const keysToInvalidate = this.invalidationCallbacks.get(event) || [];
    keysToInvalidate.forEach(key => {
      this.memoryCache.delete(key);
      this.diskCache.delete(key);
      if (this.redisCache) {
        this.redisCache.del(key);
      }
    });
  }
}
```

### TypeScript Support

```typescript
interface CacheOptions {
  level?: 'memory' | 'disk' | 'redis';
  condition?: string;
  environment?: string;
  invalidate_on?: string[];
  dependencies?: string[];
}

interface CacheEntry<T> {
  value: T;
  timestamp: number;
  duration: number;
  options: CacheOptions;
}

class TypedCacheManager {
  private memoryCache: Map<string, CacheEntry<any>>;
  private diskCache: Map<string, CacheEntry<any>>;
  private redisCache: any;
  
  constructor() {
    this.memoryCache = new Map();
    this.diskCache = new Map();
  }
  
  async cache<T>(
    duration: string,
    operation: () => Promise<T>,
    key?: string,
    options: CacheOptions = {}
  ): Promise<T> {
    const cacheKey = key || this.generateKey();
    const cached = await this.getCachedValue<T>(cacheKey, options.level);
    
    if (cached && !this.isExpired(cached)) {
      return cached.value;
    }
    
    const result = await operation();
    await this.setCachedValue(cacheKey, result, duration, options);
    
    return result;
  }
  
  private async getCachedValue<T>(key: string, level?: string): Promise<CacheEntry<T> | null> {
    switch (level) {
      case 'memory':
        return this.memoryCache.get(key) || null;
      case 'disk':
        return this.diskCache.get(key) || null;
      default:
        return this.memoryCache.get(key) || null;
    }
  }
  
  private async setCachedValue<T>(
    key: string,
    value: T,
    duration: string,
    options: CacheOptions
  ): Promise<void> {
    const cacheEntry: CacheEntry<T> = {
      value,
      timestamp: Date.now(),
      duration: this.parseDuration(duration),
      options
    };
    
    switch (options.level) {
      case 'memory':
        this.memoryCache.set(key, cacheEntry);
        break;
      case 'disk':
        this.diskCache.set(key, cacheEntry);
        break;
      default:
        this.memoryCache.set(key, cacheEntry);
    }
  }
  
  private isExpired(cacheEntry: CacheEntry<any>): boolean {
    return Date.now() - cacheEntry.timestamp > cacheEntry.duration;
  }
  
  private parseDuration(duration: string): number {
    // Implementation similar to JavaScript version
    return 5 * 60 * 1000;
  }
  
  private generateKey(): string {
    return `cache_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }
}
```

## Real-World Examples

### API Response Caching

```tsk
# Cache API responses
api_users_cache: @cache("10m", @http("GET", "https://api.example.com/users"), "api_users")
api_products_cache: @cache("30m", @http("GET", "https://api.example.com/products"), "api_products")

# Cache with authentication
authenticated_api_cache: @cache("5m", @http("GET", "https://api.example.com/user/profile", {"headers": {"Authorization": "Bearer $token"}}), "user_profile")
```

### Database Query Caching

```tsk
# Cache expensive database queries
user_analytics_cache: @cache("1h", @query("""
  SELECT 
    COUNT(*) as total_users,
    COUNT(CASE WHEN status = 'active' THEN 1 END) as active_users,
    AVG(age) as avg_age
  FROM users
"""), "user_analytics")

# Cache with user-specific data
user_posts_cache: @cache("15m", @query("SELECT * FROM posts WHERE user_id = ? ORDER BY created_at DESC", [$user_id]), "user_posts_$user_id")
```

### Configuration Caching

```tsk
# Cache configuration data
app_config_cache: @cache("1h", @json('{"version": "1.0.0", "features": ["cache", "database", "api"]}'), "app_config")
feature_flags_cache: @cache("30m", @query("SELECT * FROM feature_flags WHERE enabled = true"), "feature_flags")
```

## Performance Considerations

### Cache Warming

```tsk
# Pre-warm frequently accessed caches
warm_user_cache: @cache("1h", @query("SELECT * FROM users WHERE status = 'active'"), "active_users", {"warm": true})
warm_product_cache: @cache("2h", @query("SELECT * FROM products WHERE featured = true"), "featured_products", {"warm": true})
```

### Cache Optimization

```javascript
// Implement cache warming
class CacheWarmer {
  constructor(cacheManager) {
    this.cacheManager = cacheManager;
    this.warmupQueue = [];
  }
  
  async warmCache() {
    const warmupTasks = this.warmupQueue.map(async (task) => {
      try {
        await this.cacheManager.cache(
          task.duration,
          task.operation,
          task.key,
          { ...task.options, warm: true }
        );
      } catch (error) {
        console.error(`Cache warming failed for ${task.key}:`, error);
      }
    });
    
    await Promise.all(warmupTasks);
  }
  
  addWarmupTask(duration, operation, key, options = {}) {
    this.warmupQueue.push({ duration, operation, key, options });
  }
}
```

## Security Notes

- **Cache Security**: Secure cached data, especially sensitive information
- **Cache Poisoning**: Validate cached data to prevent poisoning attacks
- **Access Control**: Implement proper access controls for cache operations

```javascript
// Secure cache implementation
class SecureCacheManager extends TuskCacheManager {
  constructor() {
    super();
    this.sensitiveKeys = new Set(['password', 'token', 'secret']);
  }
  
  async setCachedValue(key, value, duration, level, options) {
    // Don't cache sensitive data
    if (this.containsSensitiveData(value)) {
      console.warn(`Attempted to cache sensitive data with key: ${key}`);
      return;
    }
    
    // Encrypt cached data if needed
    const encryptedValue = options.encrypt ? this.encrypt(value) : value;
    
    await super.setCachedValue(key, encryptedValue, duration, level, options);
  }
  
  containsSensitiveData(value) {
    if (typeof value === 'object' && value !== null) {
      return Object.keys(value).some(key => this.sensitiveKeys.has(key));
    }
    return false;
  }
  
  encrypt(data) {
    // Implement encryption logic
    return Buffer.from(JSON.stringify(data)).toString('base64');
  }
  
  decrypt(data) {
    // Implement decryption logic
    return JSON.parse(Buffer.from(data, 'base64').toString());
  }
}
```

## Best Practices

1. **Cache Duration**: Choose appropriate cache durations based on data volatility
2. **Cache Keys**: Use descriptive and unique cache keys
3. **Cache Invalidation**: Implement proper cache invalidation strategies
4. **Memory Management**: Monitor cache memory usage
5. **Error Handling**: Handle cache failures gracefully
6. **Monitoring**: Monitor cache hit rates and performance

## Next Steps

- Master [@metrics function](./047-at-metrics-function-javascript.md) for performance monitoring
- Learn about [@learn function](./048-at-learn-function-javascript.md) for machine learning integration
- Explore [@optimize function](./049-at-optimize-function-javascript.md) for performance optimization 