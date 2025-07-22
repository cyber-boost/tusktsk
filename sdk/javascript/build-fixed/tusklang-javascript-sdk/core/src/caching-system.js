/**
 * TuskLang Advanced Caching and Performance Optimization System
 * Provides comprehensive caching strategies and performance optimization capabilities
 */

const { EventEmitter } = require('events');

class CacheManager {
  constructor(options = {}) {
    this.options = {
      defaultTTL: options.defaultTTL || 300000, // 5 minutes
      maxSize: options.maxSize || 10000,
      cleanupInterval: options.cleanupInterval || 60000, // 1 minute
      ...options
    };
    
    this.caches = new Map();
    this.eventEmitter = new EventEmitter();
    this.cleanupTimer = null;
    this.isRunning = false;
  }

  /**
   * Create a cache
   */
  createCache(name, options = {}) {
    const cache = {
      name,
      data: new Map(),
      options: {
        ttl: options.ttl || this.options.defaultTTL,
        maxSize: options.maxSize || this.options.maxSize,
        strategy: options.strategy || 'lru', // lru, fifo, lfu
        ...options
      },
      metrics: {
        hits: 0,
        misses: 0,
        sets: 0,
        deletes: 0,
        evictions: 0
      },
      accessOrder: [],
      frequency: new Map()
    };

    this.caches.set(name, cache);
    return cache;
  }

  /**
   * Set a value in cache
   */
  set(cacheName, key, value, ttl = null) {
    const cache = this.caches.get(cacheName);
    if (!cache) {
      throw new Error(`Cache not found: ${cacheName}`);
    }

    const itemTTL = ttl || cache.options.ttl;
    const expiresAt = Date.now() + itemTTL;

    // Check if cache is full
    if (cache.data.size >= cache.options.maxSize) {
      this.evictItem(cache);
    }

    // Store the item
    cache.data.set(key, {
      value,
      expiresAt,
      createdAt: Date.now(),
      lastAccessed: Date.now()
    });

    // Update access order for LRU
    this.updateAccessOrder(cache, key);

    // Update frequency for LFU
    this.updateFrequency(cache, key);

    cache.metrics.sets++;
    this.eventEmitter.emit('cacheSet', { cacheName, key, value });

    return value;
  }

  /**
   * Get a value from cache
   */
  get(cacheName, key) {
    const cache = this.caches.get(cacheName);
    if (!cache) {
      throw new Error(`Cache not found: ${cacheName}`);
    }

    const item = cache.data.get(key);
    
    if (!item) {
      cache.metrics.misses++;
      this.eventEmitter.emit('cacheMiss', { cacheName, key });
      return null;
    }

    // Check if expired
    if (Date.now() > item.expiresAt) {
      cache.data.delete(key);
      cache.metrics.misses++;
      this.eventEmitter.emit('cacheExpired', { cacheName, key });
      return null;
    }

    // Update access time
    item.lastAccessed = Date.now();
    this.updateAccessOrder(cache, key);
    this.updateFrequency(cache, key);

    cache.metrics.hits++;
    this.eventEmitter.emit('cacheHit', { cacheName, key, value: item.value });

    return item.value;
  }

  /**
   * Delete a value from cache
   */
  delete(cacheName, key) {
    const cache = this.caches.get(cacheName);
    if (!cache) {
      throw new Error(`Cache not found: ${cacheName}`);
    }

    const deleted = cache.data.delete(key);
    if (deleted) {
      cache.metrics.deletes++;
      this.removeFromAccessOrder(cache, key);
      this.removeFromFrequency(cache, key);
      this.eventEmitter.emit('cacheDelete', { cacheName, key });
    }

    return deleted;
  }

  /**
   * Clear a cache
   */
  clear(cacheName) {
    const cache = this.caches.get(cacheName);
    if (!cache) {
      throw new Error(`Cache not found: ${cacheName}`);
    }

    const size = cache.data.size;
    cache.data.clear();
    cache.accessOrder = [];
    cache.frequency.clear();
    
    this.eventEmitter.emit('cacheClear', { cacheName, size });
    return size;
  }

  /**
   * Get cache statistics
   */
  getCacheStats(cacheName) {
    const cache = this.caches.get(cacheName);
    if (!cache) {
      return null;
    }

    const hitRate = cache.metrics.hits + cache.metrics.misses > 0 
      ? cache.metrics.hits / (cache.metrics.hits + cache.metrics.misses) 
      : 0;

    return {
      name: cache.name,
      size: cache.data.size,
      maxSize: cache.options.maxSize,
      hitRate,
      metrics: { ...cache.metrics }
    };
  }

  /**
   * Evict an item based on strategy
   */
  evictItem(cache) {
    let keyToEvict = null;

    switch (cache.options.strategy) {
      case 'lru':
        keyToEvict = cache.accessOrder[0];
        break;
      case 'fifo':
        keyToEvict = cache.accessOrder[0];
        break;
      case 'lfu':
        keyToEvict = this.getLeastFrequentlyUsed(cache);
        break;
      default:
        keyToEvict = cache.accessOrder[0];
    }

    if (keyToEvict) {
      cache.data.delete(keyToEvict);
      cache.metrics.evictions++;
      this.removeFromAccessOrder(cache, keyToEvict);
      this.removeFromFrequency(cache, keyToEvict);
    }
  }

  /**
   * Update access order for LRU
   */
  updateAccessOrder(cache, key) {
    this.removeFromAccessOrder(cache, key);
    cache.accessOrder.push(key);
  }

  /**
   * Remove from access order
   */
  removeFromAccessOrder(cache, key) {
    const index = cache.accessOrder.indexOf(key);
    if (index > -1) {
      cache.accessOrder.splice(index, 1);
    }
  }

  /**
   * Update frequency for LFU
   */
  updateFrequency(cache, key) {
    const current = cache.frequency.get(key) || 0;
    cache.frequency.set(key, current + 1);
  }

  /**
   * Remove from frequency
   */
  removeFromFrequency(cache, key) {
    cache.frequency.delete(key);
  }

  /**
   * Get least frequently used key
   */
  getLeastFrequentlyUsed(cache) {
    let minFreq = Infinity;
    let lfuKey = null;

    for (const [key, freq] of cache.frequency) {
      if (freq < minFreq) {
        minFreq = freq;
        lfuKey = key;
      }
    }

    return lfuKey;
  }

  /**
   * Start cache manager
   */
  start() {
    if (this.isRunning) return;

    this.isRunning = true;
    this.cleanupTimer = setInterval(() => {
      this.cleanup();
    }, this.options.cleanupInterval);

    this.eventEmitter.emit('started');
  }

  /**
   * Stop cache manager
   */
  stop() {
    this.isRunning = false;
    
    if (this.cleanupTimer) {
      clearInterval(this.cleanupTimer);
      this.cleanupTimer = null;
    }

    this.eventEmitter.emit('stopped');
  }

  /**
   * Cleanup expired items
   */
  cleanup() {
    const now = Date.now();
    let totalCleaned = 0;

    for (const [cacheName, cache] of this.caches) {
      let cleaned = 0;
      
      for (const [key, item] of cache.data) {
        if (now > item.expiresAt) {
          cache.data.delete(key);
          this.removeFromAccessOrder(cache, key);
          this.removeFromFrequency(cache, key);
          cleaned++;
        }
      }

      totalCleaned += cleaned;
      if (cleaned > 0) {
        this.eventEmitter.emit('cacheCleanup', { cacheName, cleaned });
      }
    }

    return totalCleaned;
  }
}

class PerformanceOptimizer {
  constructor() {
    this.optimizations = new Map();
    this.metrics = new Map();
    this.eventEmitter = new EventEmitter();
  }

  /**
   * Register an optimization
   */
  registerOptimization(name, optimizationFunction, options = {}) {
    this.optimizations.set(name, {
      function: optimizationFunction,
      options: {
        enabled: options.enabled !== false,
        priority: options.priority || 'medium',
        ...options
      }
    });
  }

  /**
   * Apply optimization
   */
  async applyOptimization(name, data, context = {}) {
    const optimization = this.optimizations.get(name);
    if (!optimization || !optimization.options.enabled) {
      return data;
    }

    const startTime = Date.now();
    
    try {
      const result = await optimization.function(data, context);
      const processingTime = Date.now() - startTime;

      // Store metrics
      if (!this.metrics.has(name)) {
        this.metrics.set(name, []);
      }
      this.metrics.get(name).push({
        timestamp: Date.now(),
        processingTime,
        dataSize: Array.isArray(data) ? data.length : 1
      });

      this.eventEmitter.emit('optimizationApplied', { name, processingTime });
      return result;
    } catch (error) {
      this.eventEmitter.emit('optimizationFailed', { name, error });
      return data; // Return original data if optimization fails
    }
  }

  /**
   * Batch processing optimization
   */
  async batchProcess(data, batchSize = 100, processor) {
    const results = [];
    
    for (let i = 0; i < data.length; i += batchSize) {
      const batch = data.slice(i, i + batchSize);
      const batchResult = await processor(batch);
      results.push(...batchResult);
    }

    return results;
  }

  /**
   * Memory optimization
   */
  optimizeMemory(data, options = {}) {
    const maxSize = options.maxSize || 1000;
    
    if (Array.isArray(data) && data.length > maxSize) {
      return data.slice(-maxSize);
    }

    return data;
  }

  /**
   * Get optimization metrics
   */
  getOptimizationMetrics(name) {
    const metrics = this.metrics.get(name);
    if (!metrics || metrics.length === 0) {
      return null;
    }

    const processingTimes = metrics.map(m => m.processingTime);
    const averageTime = processingTimes.reduce((a, b) => a + b, 0) / processingTimes.length;

    return {
      name,
      totalApplications: metrics.length,
      averageProcessingTime: averageTime,
      lastApplied: metrics[metrics.length - 1]
    };
  }
}

class QueryOptimizer {
  constructor() {
    this.optimizations = new Map();
    this.queryCache = new Map();
    this.statistics = new Map();
  }

  /**
   * Register query optimization
   */
  registerQueryOptimization(name, optimizer) {
    this.optimizations.set(name, optimizer);
  }

  /**
   * Optimize query
   */
  optimizeQuery(query, context = {}) {
    let optimizedQuery = query;

    for (const [name, optimizer] of this.optimizations) {
      try {
        optimizedQuery = optimizer(optimizedQuery, context);
      } catch (error) {
        console.warn(`Query optimization ${name} failed:`, error.message);
      }
    }

    return optimizedQuery;
  }

  /**
   * Cache query result
   */
  cacheQueryResult(query, result, ttl = 300000) {
    const cacheKey = this.generateQueryKey(query);
    this.queryCache.set(cacheKey, {
      result,
      timestamp: Date.now(),
      ttl
    });
  }

  /**
   * Get cached query result
   */
  getCachedQueryResult(query) {
    const cacheKey = this.generateQueryKey(query);
    const cached = this.queryCache.get(cacheKey);
    
    if (!cached) return null;

    if (Date.now() - cached.timestamp > cached.ttl) {
      this.queryCache.delete(cacheKey);
      return null;
    }

    return cached.result;
  }

  /**
   * Update query statistics
   */
  updateQueryStatistics(query, executionTime, resultSize) {
    const key = this.generateQueryKey(query);
    
    if (!this.statistics.has(key)) {
      this.statistics.set(key, {
        executions: 0,
        totalTime: 0,
        averageTime: 0,
        totalResults: 0,
        averageResults: 0
      });
    }

    const stats = this.statistics.get(key);
    stats.executions++;
    stats.totalTime += executionTime;
    stats.averageTime = stats.totalTime / stats.executions;
    stats.totalResults += resultSize;
    stats.averageResults = stats.totalResults / stats.executions;
  }

  /**
   * Generate query cache key
   */
  generateQueryKey(query) {
    return typeof query === 'string' ? query : JSON.stringify(query);
  }

  /**
   * Get query statistics
   */
  getQueryStatistics(query) {
    const key = this.generateQueryKey(query);
    return this.statistics.get(key);
  }
}

// Built-in query optimizations
const builtInQueryOptimizations = {
  // Limit optimization
  limit: (query, context) => {
    if (query.limit && query.limit > context.maxLimit) {
      query.limit = context.maxLimit;
    }
    return query;
  },

  // Sort optimization
  sort: (query, context) => {
    if (query.sort && query.sort.length > context.maxSortFields) {
      query.sort = query.sort.slice(0, context.maxSortFields);
    }
    return query;
  },

  // Field selection optimization
  fields: (query, context) => {
    if (query.fields && query.fields.length > context.maxFields) {
      query.fields = query.fields.slice(0, context.maxFields);
    }
    return query;
  }
};

module.exports = {
  CacheManager,
  PerformanceOptimizer,
  QueryOptimizer,
  builtInQueryOptimizations
}; 