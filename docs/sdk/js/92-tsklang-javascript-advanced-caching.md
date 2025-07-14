# TuskLang JavaScript Documentation: Advanced Caching

## Overview

Advanced caching in TuskLang provides sophisticated caching strategies, multi-level caching, and intelligent cache management with JavaScript/Node.js integration.

## TuskLang Syntax

```tsk
#cache advanced
  levels:
    l1:
      type: memory
      max_size: 100MB
      ttl: 300s
      eviction_policy: lru
    l2:
      type: redis
      host: localhost
      port: 6379
      ttl: 3600s
      max_memory: 1GB
    l3:
      type: file
      directory: /var/cache
      ttl: 86400s
      compression: true

  strategies:
    write_through: true
    write_behind: false
    cache_aside: true
    read_through: true

  invalidation:
    automatic: true
    patterns:
      - "user:*"
      - "product:*"
    events:
      - database_update
      - user_logout
      - product_change

  analytics:
    enabled: true
    metrics:
      - hit_rate
      - miss_rate
      - eviction_rate
      - response_time
```

## JavaScript Integration

### Advanced Cache Manager

```javascript
// advanced-cache-manager.js
const Redis = require('ioredis');
const fs = require('fs').promises;
const path = require('path');
const crypto = require('crypto');
const EventEmitter = require('events');

class AdvancedCacheManager extends EventEmitter {
  constructor(config) {
    super();
    this.config = config;
    this.levels = new Map();
    this.strategies = config.strategies || {};
    this.invalidation = config.invalidation || {};
    this.analytics = config.analytics || {};
    
    this.memoryCache = new Map();
    this.cacheStats = {
      hits: 0,
      misses: 0,
      evictions: 0,
      totalRequests: 0
    };
  }

  async initialize() {
    await this.setupCacheLevels();
    await this.setupInvalidation();
    await this.setupAnalytics();
    
    console.log('Advanced cache manager initialized');
  }

  async setupCacheLevels() {
    for (const [level, config] of Object.entries(this.config.levels)) {
      const cacheLevel = await this.createCacheLevel(level, config);
      this.levels.set(level, cacheLevel);
    }
  }

  async createCacheLevel(level, config) {
    switch (config.type) {
      case 'memory':
        return new MemoryCacheLevel(config);
        
      case 'redis':
        return new RedisCacheLevel(config);
        
      case 'file':
        return new FileCacheLevel(config);
        
      default:
        throw new Error(`Unsupported cache type: ${config.type}`);
    }
  }

  async setupInvalidation() {
    if (this.invalidation.automatic) {
      this.setupAutomaticInvalidation();
    }
  }

  setupAutomaticInvalidation() {
    // Setup event listeners for automatic invalidation
    this.on('database_update', (data) => {
      this.invalidatePatterns(data.table);
    });
    
    this.on('user_logout', (userId) => {
      this.invalidatePattern(`user:${userId}:*`);
    });
    
    this.on('product_change', (productId) => {
      this.invalidatePattern(`product:${productId}:*`);
    });
  }

  async setupAnalytics() {
    if (this.analytics.enabled) {
      this.startAnalyticsCollection();
    }
  }

  startAnalyticsCollection() {
    setInterval(() => {
      this.emit('analytics', this.getCacheStats());
    }, 60000); // Collect every minute
  }

  async get(key, options = {}) {
    const start = Date.now();
    this.cacheStats.totalRequests++;
    
    try {
      // Try each cache level
      for (const [levelName, level] of this.levels.entries()) {
        const value = await level.get(key);
        
        if (value !== null) {
          this.cacheStats.hits++;
          this.recordHit(key, levelName, Date.now() - start);
          
          // Populate higher levels if using read-through
          if (this.strategies.read_through && levelName !== 'l1') {
            await this.populateHigherLevels(key, value, levelName);
          }
          
          return value;
        }
      }
      
      this.cacheStats.misses++;
      this.recordMiss(key, Date.now() - start);
      
      return null;
    } catch (error) {
      this.emit('error', { key, error });
      throw error;
    }
  }

  async set(key, value, options = {}) {
    const start = Date.now();
    
    try {
      if (this.strategies.write_through) {
        // Write to all levels
        await Promise.all(
          Array.from(this.levels.values()).map(level => level.set(key, value, options))
        );
      } else if (this.strategies.write_behind) {
        // Write to L1 only, queue for background write
        await this.levels.get('l1').set(key, value, options);
        this.queueBackgroundWrite(key, value, options);
      } else {
        // Write to L1 only
        await this.levels.get('l1').set(key, value, options);
      }
      
      this.recordSet(key, Date.now() - start);
    } catch (error) {
      this.emit('error', { key, error });
      throw error;
    }
  }

  async delete(key) {
    try {
      // Delete from all levels
      await Promise.all(
        Array.from(this.levels.values()).map(level => level.delete(key))
      );
      
      this.emit('delete', { key });
    } catch (error) {
      this.emit('error', { key, error });
      throw error;
    }
  }

  async invalidatePattern(pattern) {
    try {
      for (const [levelName, level] of this.levels.entries()) {
        if (level.invalidatePattern) {
          await level.invalidatePattern(pattern);
        }
      }
      
      this.emit('invalidate', { pattern });
    } catch (error) {
      this.emit('error', { pattern, error });
      throw error;
    }
  }

  async populateHigherLevels(key, value, sourceLevel) {
    const levels = Array.from(this.levels.entries());
    const sourceIndex = levels.findIndex(([name]) => name === sourceLevel);
    
    for (let i = sourceIndex - 1; i >= 0; i--) {
      const [levelName, level] = levels[i];
      await level.set(key, value);
    }
  }

  async queueBackgroundWrite(key, value, options) {
    // This would implement a background write queue
    setTimeout(async () => {
      try {
        for (let i = 1; i < this.levels.size; i++) {
          const level = Array.from(this.levels.values())[i];
          await level.set(key, value, options);
        }
      } catch (error) {
        this.emit('background_write_error', { key, error });
      }
    }, 1000);
  }

  recordHit(key, level, duration) {
    this.emit('hit', { key, level, duration });
  }

  recordMiss(key, duration) {
    this.emit('miss', { key, duration });
  }

  recordSet(key, duration) {
    this.emit('set', { key, duration });
  }

  getCacheStats() {
    const hitRate = this.cacheStats.totalRequests > 0 
      ? (this.cacheStats.hits / this.cacheStats.totalRequests) * 100 
      : 0;
    
    return {
      ...this.cacheStats,
      hitRate: hitRate.toFixed(2) + '%',
      missRate: (100 - hitRate).toFixed(2) + '%'
    };
  }

  async clear() {
    await Promise.all(
      Array.from(this.levels.values()).map(level => level.clear())
    );
    
    this.cacheStats = {
      hits: 0,
      misses: 0,
      evictions: 0,
      totalRequests: 0
    };
  }
}

module.exports = AdvancedCacheManager;
```

### Memory Cache Level

```javascript
// memory-cache-level.js
class MemoryCacheLevel {
  constructor(config) {
    this.config = config;
    this.cache = new Map();
    this.maxSize = this.parseSize(config.max_size || '100MB');
    this.ttl = this.parseTime(config.ttl || '300s');
    this.evictionPolicy = config.eviction_policy || 'lru';
    
    this.accessOrder = [];
    this.currentSize = 0;
  }

  parseSize(sizeStr) {
    const match = sizeStr.match(/^(\d+)([KMGT]?B)$/);
    if (!match) return 100 * 1024 * 1024; // Default 100MB

    const [, value, unit] = match;
    const multipliers = {
      'B': 1,
      'KB': 1024,
      'MB': 1024 * 1024,
      'GB': 1024 * 1024 * 1024,
      'TB': 1024 * 1024 * 1024 * 1024
    };

    return parseInt(value) * (multipliers[unit] || 1);
  }

  parseTime(timeStr) {
    const match = timeStr.match(/^(\d+)([smhd])$/);
    if (!match) return 300; // Default 300 seconds

    const [, value, unit] = match;
    const multipliers = {
      's': 1,
      'm': 60,
      'h': 60 * 60,
      'd': 24 * 60 * 60
    };

    return parseInt(value) * (multipliers[unit] || 1);
  }

  async get(key) {
    const entry = this.cache.get(key);
    
    if (!entry) {
      return null;
    }
    
    // Check TTL
    if (Date.now() > entry.expires) {
      this.cache.delete(key);
      this.removeFromAccessOrder(key);
      return null;
    }
    
    // Update access order for LRU
    if (this.evictionPolicy === 'lru') {
      this.updateAccessOrder(key);
    }
    
    return entry.value;
  }

  async set(key, value, options = {}) {
    const ttl = options.ttl || this.ttl;
    const expires = Date.now() + (ttl * 1000);
    
    const entry = {
      value,
      expires,
      size: this.calculateSize(value)
    };
    
    // Check if we need to evict
    while (this.currentSize + entry.size > this.maxSize) {
      await this.evict();
    }
    
    this.cache.set(key, entry);
    this.currentSize += entry.size;
    this.updateAccessOrder(key);
  }

  async delete(key) {
    const entry = this.cache.get(key);
    if (entry) {
      this.currentSize -= entry.size;
      this.cache.delete(key);
      this.removeFromAccessOrder(key);
    }
  }

  async clear() {
    this.cache.clear();
    this.accessOrder = [];
    this.currentSize = 0;
  }

  calculateSize(value) {
    // Simplified size calculation
    return JSON.stringify(value).length;
  }

  async evict() {
    if (this.evictionPolicy === 'lru' && this.accessOrder.length > 0) {
      const keyToEvict = this.accessOrder.shift();
      await this.delete(keyToEvict);
    } else {
      // Random eviction
      const keys = Array.from(this.cache.keys());
      if (keys.length > 0) {
        const randomKey = keys[Math.floor(Math.random() * keys.length)];
        await this.delete(randomKey);
      }
    }
  }

  updateAccessOrder(key) {
    this.removeFromAccessOrder(key);
    this.accessOrder.push(key);
  }

  removeFromAccessOrder(key) {
    const index = this.accessOrder.indexOf(key);
    if (index > -1) {
      this.accessOrder.splice(index, 1);
    }
  }

  getStats() {
    return {
      size: this.currentSize,
      maxSize: this.maxSize,
      entries: this.cache.size,
      evictionPolicy: this.evictionPolicy
    };
  }
}

module.exports = MemoryCacheLevel;
```

### Redis Cache Level

```javascript
// redis-cache-level.js
class RedisCacheLevel {
  constructor(config) {
    this.config = config;
    this.redis = new Redis({
      host: config.host || 'localhost',
      port: config.port || 6379,
      password: config.password,
      db: config.db || 0,
      retryDelayOnFailover: 100,
      maxRetriesPerRequest: 3
    });
    
    this.ttl = this.parseTime(config.ttl || '3600s');
    this.maxMemory = this.parseSize(config.max_memory || '1GB');
  }

  parseTime(timeStr) {
    const match = timeStr.match(/^(\d+)([smhd])$/);
    if (!match) return 3600; // Default 1 hour

    const [, value, unit] = match;
    const multipliers = {
      's': 1,
      'm': 60,
      'h': 60 * 60,
      'd': 24 * 60 * 60
    };

    return parseInt(value) * (multipliers[unit] || 1);
  }

  parseSize(sizeStr) {
    const match = sizeStr.match(/^(\d+)([KMGT]?B)$/);
    if (!match) return 1024 * 1024 * 1024; // Default 1GB

    const [, value, unit] = match;
    const multipliers = {
      'B': 1,
      'KB': 1024,
      'MB': 1024 * 1024,
      'GB': 1024 * 1024 * 1024,
      'TB': 1024 * 1024 * 1024 * 1024
    };

    return parseInt(value) * (multipliers[unit] || 1);
  }

  async get(key) {
    try {
      const value = await this.redis.get(key);
      return value ? JSON.parse(value) : null;
    } catch (error) {
      console.error('Redis get error:', error);
      return null;
    }
  }

  async set(key, value, options = {}) {
    try {
      const ttl = options.ttl || this.ttl;
      const serializedValue = JSON.stringify(value);
      
      if (ttl > 0) {
        await this.redis.setex(key, ttl, serializedValue);
      } else {
        await this.redis.set(key, serializedValue);
      }
    } catch (error) {
      console.error('Redis set error:', error);
      throw error;
    }
  }

  async delete(key) {
    try {
      await this.redis.del(key);
    } catch (error) {
      console.error('Redis delete error:', error);
      throw error;
    }
  }

  async clear() {
    try {
      await this.redis.flushdb();
    } catch (error) {
      console.error('Redis clear error:', error);
      throw error;
    }
  }

  async invalidatePattern(pattern) {
    try {
      const keys = await this.redis.keys(pattern);
      if (keys.length > 0) {
        await this.redis.del(...keys);
      }
    } catch (error) {
      console.error('Redis pattern invalidation error:', error);
      throw error;
    }
  }

  async getStats() {
    try {
      const info = await this.redis.info();
      const memory = await this.redis.memory('usage');
      
      return {
        info: info,
        memory: memory,
        maxMemory: this.maxMemory
      };
    } catch (error) {
      console.error('Redis stats error:', error);
      return {};
    }
  }
}

module.exports = RedisCacheLevel;
```

### File Cache Level

```javascript
// file-cache-level.js
const fs = require('fs').promises;
const path = require('path');
const crypto = require('crypto');
const zlib = require('zlib');
const { promisify } = require('util');

const gzip = promisify(zlib.gzip);
const gunzip = promisify(zlib.gunzip);

class FileCacheLevel {
  constructor(config) {
    this.config = config;
    this.directory = config.directory || '/var/cache';
    this.ttl = this.parseTime(config.ttl || '86400s');
    this.compression = config.compression !== false;
  }

  parseTime(timeStr) {
    const match = timeStr.match(/^(\d+)([smhd])$/);
    if (!match) return 86400; // Default 1 day

    const [, value, unit] = match;
    const multipliers = {
      's': 1,
      'm': 60,
      'h': 60 * 60,
      'd': 24 * 60 * 60
    };

    return parseInt(value) * (multipliers[unit] || 1);
  }

  getFilePath(key) {
    const hash = crypto.createHash('md5').update(key).digest('hex');
    return path.join(this.directory, `${hash}.cache`);
  }

  async get(key) {
    try {
      const filePath = this.getFilePath(key);
      const stats = await fs.stat(filePath);
      
      // Check TTL
      if (Date.now() - stats.mtime.getTime() > this.ttl * 1000) {
        await this.delete(key);
        return null;
      }
      
      let data = await fs.readFile(filePath);
      
      // Decompress if needed
      if (this.compression) {
        data = await gunzip(data);
      }
      
      const entry = JSON.parse(data.toString());
      return entry.value;
    } catch (error) {
      if (error.code !== 'ENOENT') {
        console.error('File cache get error:', error);
      }
      return null;
    }
  }

  async set(key, value, options = {}) {
    try {
      const ttl = options.ttl || this.ttl;
      const entry = {
        key,
        value,
        timestamp: Date.now(),
        expires: Date.now() + (ttl * 1000)
      };
      
      let data = Buffer.from(JSON.stringify(entry));
      
      // Compress if enabled
      if (this.compression) {
        data = await gzip(data);
      }
      
      const filePath = this.getFilePath(key);
      await fs.writeFile(filePath, data);
    } catch (error) {
      console.error('File cache set error:', error);
      throw error;
    }
  }

  async delete(key) {
    try {
      const filePath = this.getFilePath(key);
      await fs.unlink(filePath);
    } catch (error) {
      if (error.code !== 'ENOENT') {
        console.error('File cache delete error:', error);
      }
    }
  }

  async clear() {
    try {
      const files = await fs.readdir(this.directory);
      const cacheFiles = files.filter(file => file.endsWith('.cache'));
      
      await Promise.all(
        cacheFiles.map(file => 
          fs.unlink(path.join(this.directory, file))
        )
      );
    } catch (error) {
      console.error('File cache clear error:', error);
      throw error;
    }
  }

  async invalidatePattern(pattern) {
    try {
      const files = await fs.readdir(this.directory);
      const cacheFiles = files.filter(file => file.endsWith('.cache'));
      
      for (const file of cacheFiles) {
        const filePath = path.join(this.directory, file);
        let data = await fs.readFile(filePath);
        
        if (this.compression) {
          data = await gunzip(data);
        }
        
        const entry = JSON.parse(data.toString());
        
        if (this.matchesPattern(entry.key, pattern)) {
          await fs.unlink(filePath);
        }
      }
    } catch (error) {
      console.error('File cache pattern invalidation error:', error);
      throw error;
    }
  }

  matchesPattern(key, pattern) {
    const regex = new RegExp(pattern.replace(/\*/g, '.*'));
    return regex.test(key);
  }

  async getStats() {
    try {
      const files = await fs.readdir(this.directory);
      const cacheFiles = files.filter(file => file.endsWith('.cache'));
      
      let totalSize = 0;
      for (const file of cacheFiles) {
        const stats = await fs.stat(path.join(this.directory, file));
        totalSize += stats.size;
      }
      
      return {
        files: cacheFiles.length,
        totalSize,
        compression: this.compression
      };
    } catch (error) {
      console.error('File cache stats error:', error);
      return {};
    }
  }
}

module.exports = FileCacheLevel;
```

## TypeScript Implementation

```typescript
// advanced-cache.types.ts
export interface CacheConfig {
  levels: Record<string, CacheLevelConfig>;
  strategies?: CacheStrategies;
  invalidation?: InvalidationConfig;
  analytics?: AnalyticsConfig;
}

export interface CacheLevelConfig {
  type: 'memory' | 'redis' | 'file';
  max_size?: string;
  ttl?: string;
  eviction_policy?: 'lru' | 'lfu' | 'random';
  host?: string;
  port?: number;
  directory?: string;
  compression?: boolean;
  max_memory?: string;
}

export interface CacheStrategies {
  write_through?: boolean;
  write_behind?: boolean;
  cache_aside?: boolean;
  read_through?: boolean;
}

export interface InvalidationConfig {
  automatic?: boolean;
  patterns?: string[];
  events?: string[];
}

export interface AnalyticsConfig {
  enabled?: boolean;
  metrics?: string[];
}

export interface CacheEntry {
  value: any;
  expires: number;
  size?: number;
}

export interface CacheLevel {
  get(key: string): Promise<any>;
  set(key: string, value: any, options?: any): Promise<void>;
  delete(key: string): Promise<void>;
  clear(): Promise<void>;
  invalidatePattern?(pattern: string): Promise<void>;
  getStats?(): Promise<any>;
}

export interface CacheManager {
  get(key: string, options?: any): Promise<any>;
  set(key: string, value: any, options?: any): Promise<void>;
  delete(key: string): Promise<void>;
  invalidatePattern(pattern: string): Promise<void>;
  getCacheStats(): any;
  clear(): Promise<void>;
}

// advanced-cache.ts
import { CacheConfig, CacheManager, CacheLevel } from './advanced-cache.types';

export class TypeScriptAdvancedCacheManager implements CacheManager {
  private config: CacheConfig;
  private levels: Map<string, CacheLevel> = new Map();
  private cacheStats = {
    hits: 0,
    misses: 0,
    evictions: 0,
    totalRequests: 0
  };

  constructor(config: CacheConfig) {
    this.config = config;
  }

  async get(key: string, options: any = {}): Promise<any> {
    this.cacheStats.totalRequests++;
    
    for (const [levelName, level] of this.levels.entries()) {
      const value = await level.get(key);
      
      if (value !== null) {
        this.cacheStats.hits++;
        return value;
      }
    }
    
    this.cacheStats.misses++;
    return null;
  }

  async set(key: string, value: any, options: any = {}): Promise<void> {
    if (this.config.strategies?.write_through) {
      await Promise.all(
        Array.from(this.levels.values()).map(level => level.set(key, value, options))
      );
    } else {
      const l1 = this.levels.get('l1');
      if (l1) {
        await l1.set(key, value, options);
      }
    }
  }

  async delete(key: string): Promise<void> {
    await Promise.all(
      Array.from(this.levels.values()).map(level => level.delete(key))
    );
  }

  async invalidatePattern(pattern: string): Promise<void> {
    for (const level of this.levels.values()) {
      if (level.invalidatePattern) {
        await level.invalidatePattern(pattern);
      }
    }
  }

  getCacheStats(): any {
    const hitRate = this.cacheStats.totalRequests > 0 
      ? (this.cacheStats.hits / this.cacheStats.totalRequests) * 100 
      : 0;
    
    return {
      ...this.cacheStats,
      hitRate: hitRate.toFixed(2) + '%',
      missRate: (100 - hitRate).toFixed(2) + '%'
    };
  }

  async clear(): Promise<void> {
    await Promise.all(
      Array.from(this.levels.values()).map(level => level.clear())
    );
    
    this.cacheStats = {
      hits: 0,
      misses: 0,
      evictions: 0,
      totalRequests: 0
    };
  }
}
```

## Advanced Usage Scenarios

### Multi-Level Cache Strategy

```javascript
// multi-level-cache-strategy.js
class MultiLevelCacheStrategy {
  constructor(cacheManager) {
    this.cache = cacheManager;
  }

  async getWithFallback(key, fetchFunction) {
    // Try cache first
    let value = await this.cache.get(key);
    
    if (value !== null) {
      return value;
    }
    
    // Fetch from source
    value = await fetchFunction();
    
    // Store in cache
    await this.cache.set(key, value);
    
    return value;
  }

  async getWithStaleWhileRevalidate(key, fetchFunction, staleTime = 300) {
    const value = await this.cache.get(key);
    
    if (value !== null) {
      // Return stale data immediately
      fetchFunction().then(async (freshValue) => {
        await this.cache.set(key, freshValue);
      });
      
      return value;
    }
    
    // Fetch fresh data
    const freshValue = await fetchFunction();
    await this.cache.set(key, freshValue);
    
    return freshValue;
  }

  async getWithBackgroundRefresh(key, fetchFunction, refreshTime = 60) {
    const value = await this.cache.get(key);
    
    if (value !== null) {
      // Check if refresh is needed
      const entry = await this.cache.getEntry(key);
      if (entry && Date.now() - entry.timestamp > refreshTime * 1000) {
        // Refresh in background
        fetchFunction().then(async (freshValue) => {
          await this.cache.set(key, freshValue);
        });
      }
      
      return value;
    }
    
    // Fetch fresh data
    const freshValue = await fetchFunction();
    await this.cache.set(key, freshValue);
    
    return freshValue;
  }
}
```

### Cache Warming

```javascript
// cache-warmer.js
class CacheWarmer {
  constructor(cacheManager) {
    this.cache = cacheManager;
    this.warmingQueue = [];
    this.isWarming = false;
  }

  async warmCache(keys, fetchFunction) {
    this.warmingQueue.push(...keys);
    
    if (!this.isWarming) {
      await this.processWarmingQueue(fetchFunction);
    }
  }

  async processWarmingQueue(fetchFunction) {
    this.isWarming = true;
    
    while (this.warmingQueue.length > 0) {
      const batch = this.warmingQueue.splice(0, 10); // Process in batches
      
      await Promise.all(
        batch.map(async (key) => {
          try {
            const value = await fetchFunction(key);
            await this.cache.set(key, value);
          } catch (error) {
            console.error(`Failed to warm cache for key ${key}:`, error);
          }
        })
      );
      
      // Small delay between batches
      await new Promise(resolve => setTimeout(resolve, 100));
    }
    
    this.isWarming = false;
  }

  async warmPopularItems() {
    const popularKeys = await this.getPopularKeys();
    await this.warmCache(popularKeys, this.fetchItem);
  }

  async getPopularKeys() {
    // This would analyze access patterns to determine popular keys
    return ['user:1', 'user:2', 'product:1', 'product:2'];
  }

  async fetchItem(key) {
    // This would fetch the actual item
    return { key, data: 'fetched data' };
  }
}
```

## Real-World Examples

### E-commerce Cache Integration

```javascript
// ecommerce-cache.js
class EcommerceCache {
  constructor(cacheManager) {
    this.cache = cacheManager;
  }

  async getProduct(productId) {
    const cacheKey = `product:${productId}`;
    
    return await this.cache.get(cacheKey, {
      fallback: async () => {
        // Fetch from database
        const product = await this.fetchProductFromDB(productId);
        return product;
      }
    });
  }

  async getUserCart(userId) {
    const cacheKey = `cart:${userId}`;
    
    return await this.cache.get(cacheKey, {
      fallback: async () => {
        // Fetch from database
        const cart = await this.fetchCartFromDB(userId);
        return cart;
      }
    });
  }

  async updateProduct(productId, updates) {
    // Update database
    await this.updateProductInDB(productId, updates);
    
    // Invalidate cache
    await this.cache.invalidatePattern(`product:${productId}*`);
    
    // Update cache with new data
    const updatedProduct = await this.fetchProductFromDB(productId);
    await this.cache.set(`product:${productId}`, updatedProduct);
  }

  async addToCart(userId, productId, quantity) {
    // Update database
    await this.addToCartInDB(userId, productId, quantity);
    
    // Invalidate cart cache
    await this.cache.delete(`cart:${userId}`);
  }

  async getProductRecommendations(userId) {
    const cacheKey = `recommendations:${userId}`;
    
    return await this.cache.get(cacheKey, {
      ttl: 3600, // 1 hour
      fallback: async () => {
        // Generate recommendations
        const recommendations = await this.generateRecommendations(userId);
        return recommendations;
      }
    });
  }

  async fetchProductFromDB(productId) {
    // Database query implementation
    return { id: productId, name: 'Product', price: 99.99 };
  }

  async fetchCartFromDB(userId) {
    // Database query implementation
    return { userId, items: [] };
  }

  async updateProductInDB(productId, updates) {
    // Database update implementation
  }

  async addToCartInDB(userId, productId, quantity) {
    // Database update implementation
  }

  async generateRecommendations(userId) {
    // Recommendation algorithm implementation
    return ['product1', 'product2', 'product3'];
  }
}
```

### API Response Caching

```javascript
// api-response-cache.js
class APIResponseCache {
  constructor(cacheManager) {
    this.cache = cacheManager;
  }

  async cacheMiddleware(req, res, next) {
    const cacheKey = this.generateCacheKey(req);
    
    // Check cache
    const cachedResponse = await this.cache.get(cacheKey);
    
    if (cachedResponse) {
      res.json(cachedResponse);
      return;
    }
    
    // Store original send method
    const originalSend = res.json;
    
    // Override send method to cache response
    res.json = async (data) => {
      // Cache the response
      await this.cache.set(cacheKey, data, {
        ttl: this.getCacheTTL(req)
      });
      
      // Call original send method
      originalSend.call(res, data);
    };
    
    next();
  }

  generateCacheKey(req) {
    const { method, url, query, body } = req;
    const keyData = { method, url, query, body };
    return `api:${JSON.stringify(keyData)}`;
  }

  getCacheTTL(req) {
    // Different TTL based on endpoint
    const ttlMap = {
      '/api/products': 3600,    // 1 hour
      '/api/users': 1800,       // 30 minutes
      '/api/orders': 300        // 5 minutes
    };
    
    return ttlMap[req.url] || 300;
  }

  async invalidateUserData(userId) {
    await this.cache.invalidatePattern(`api:*users*${userId}*`);
    await this.cache.invalidatePattern(`api:*orders*${userId}*`);
  }

  async invalidateProductData(productId) {
    await this.cache.invalidatePattern(`api:*products*${productId}*`);
  }
}
```

## Performance Considerations

### Cache Performance Monitoring

```javascript
// cache-performance-monitor.js
class CachePerformanceMonitor {
  constructor(cacheManager) {
    this.cache = cacheManager;
    this.metrics = {
      responseTimes: [],
      hitRates: [],
      memoryUsage: []
    };
  }

  async monitorPerformance() {
    setInterval(async () => {
      const start = Date.now();
      const stats = this.cache.getCacheStats();
      const responseTime = Date.now() - start;
      
      this.metrics.responseTimes.push(responseTime);
      this.metrics.hitRates.push(parseFloat(stats.hitRate));
      
      // Keep only last 1000 metrics
      if (this.metrics.responseTimes.length > 1000) {
        this.metrics.responseTimes.shift();
        this.metrics.hitRates.shift();
      }
    }, 60000); // Every minute
  }

  getPerformanceMetrics() {
    const avgResponseTime = this.metrics.responseTimes.reduce((a, b) => a + b, 0) / this.metrics.responseTimes.length;
    const avgHitRate = this.metrics.hitRates.reduce((a, b) => a + b, 0) / this.metrics.hitRates.length;
    
    return {
      avgResponseTime: avgResponseTime.toFixed(2) + 'ms',
      avgHitRate: avgHitRate.toFixed(2) + '%',
      totalMetrics: this.metrics.responseTimes.length
    };
  }
}
```

### Cache Optimization

```javascript
// cache-optimizer.js
class CacheOptimizer {
  constructor(cacheManager) {
    this.cache = cacheManager;
    this.optimizationRules = [];
  }

  addOptimizationRule(rule) {
    this.optimizationRules.push(rule);
  }

  async optimizeCache() {
    const stats = this.cache.getCacheStats();
    
    for (const rule of this.optimizationRules) {
      if (rule.shouldOptimize(stats)) {
        await rule.optimize(this.cache);
      }
    }
  }

  createTTLOptimizationRule() {
    return {
      name: 'TTL Optimization',
      shouldOptimize: (stats) => stats.hitRate < 80,
      optimize: async (cache) => {
        // Increase TTL for better hit rates
        console.log('Optimizing TTL for better hit rates');
      }
    };
  }

  createEvictionOptimizationRule() {
    return {
      name: 'Eviction Optimization',
      shouldOptimize: (stats) => stats.evictions > 100,
      optimize: async (cache) => {
        // Adjust eviction policy
        console.log('Optimizing eviction policy');
      }
    };
  }
}
```

## Security Notes

### Cache Security

```javascript
// cache-security.js
class CacheSecurity {
  constructor() {
    this.sensitivePatterns = [
      /password/i,
      /token/i,
      /secret/i,
      /key/i
    ];
  }

  validateCacheKey(key) {
    // Check for sensitive data in keys
    for (const pattern of this.sensitivePatterns) {
      if (pattern.test(key)) {
        throw new Error('Sensitive data detected in cache key');
      }
    }
    
    return true;
  }

  sanitizeCacheValue(value) {
    // Remove sensitive fields from cached data
    if (typeof value === 'object' && value !== null) {
      const sanitized = { ...value };
      
      for (const field of ['password', 'token', 'secret', 'apiKey']) {
        if (sanitized[field]) {
          sanitized[field] = '[REDACTED]';
        }
      }
      
      return sanitized;
    }
    
    return value;
  }

  encryptSensitiveData(data) {
    const crypto = require('crypto');
    const algorithm = 'aes-256-gcm';
    const key = crypto.randomBytes(32);
    const iv = crypto.randomBytes(16);
    
    const cipher = crypto.createCipher(algorithm, key);
    let encrypted = cipher.update(JSON.stringify(data), 'utf8', 'hex');
    encrypted += cipher.final('hex');
    
    return {
      encrypted,
      key: key.toString('hex'),
      iv: iv.toString('hex')
    };
  }
}
```

## Best Practices

### Cache Configuration Management

```javascript
// cache-config-manager.js
class CacheConfigManager {
  constructor() {
    this.configs = new Map();
  }

  setConfig(environment, config) {
    this.configs.set(environment, this.validateConfig(config));
  }

  getConfig(environment) {
    const config = this.configs.get(environment);
    if (!config) {
      throw new Error(`No configuration found for environment: ${environment}`);
    }
    return config;
  }

  validateConfig(config) {
    if (!config.levels || Object.keys(config.levels).length === 0) {
      throw new Error('At least one cache level is required');
    }
    
    return config;
  }

  getCurrentConfig() {
    const environment = process.env.NODE_ENV || 'development';
    return this.getConfig(environment);
  }
}
```

### Cache Health Monitoring

```javascript
// cache-health-monitor.js
class CacheHealthMonitor {
  constructor(cacheManager) {
    this.cache = cacheManager;
    this.metrics = {
      operations: 0,
      errors: 0,
      avgResponseTime: 0
    };
  }

  async checkHealth() {
    try {
      const start = Date.now();
      await this.cache.set('health_check', { timestamp: Date.now() });
      const responseTime = Date.now() - start;
      
      this.metrics.operations++;
      this.metrics.avgResponseTime = 
        (this.metrics.avgResponseTime * (this.metrics.operations - 1) + responseTime) / this.metrics.operations;
      
      return {
        status: 'healthy',
        responseTime,
        metrics: this.metrics
      };
    } catch (error) {
      this.metrics.errors++;
      return {
        status: 'unhealthy',
        error: error.message,
        metrics: this.metrics
      };
    }
  }

  getMetrics() {
    return this.metrics;
  }
}
```

## Related Topics

- [@cache Operator](./48-tsklang-javascript-operator-cache.md)
- [@redis Operator](./26-tsklang-javascript-operator-redis.md)
- [@memory Operator](./27-tsklang-javascript-operator-memory.md)
- [@metrics Operator](./49-tsklang-javascript-operator-metrics.md)
- [@cache Directive](./84-tsklang-javascript-directives-cache.md) 