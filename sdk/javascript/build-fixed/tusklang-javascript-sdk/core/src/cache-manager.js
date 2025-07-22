/**
 * TuskLang Advanced Cache Manager
 * Provides intelligent caching and performance optimization for the JavaScript SDK
 */

const crypto = require('crypto');

class CacheEntry {
  constructor(key, value, ttl = 3600000) { // Default 1 hour TTL
    this.key = key;
    this.value = value;
    this.createdAt = Date.now();
    this.lastAccessed = Date.now();
    this.accessCount = 0;
    this.ttl = ttl;
    this.expiresAt = this.createdAt + ttl;
  }

  isExpired() {
    return Date.now() > this.expiresAt;
  }

  touch() {
    this.lastAccessed = Date.now();
    this.accessCount++;
  }

  getAge() {
    return Date.now() - this.createdAt;
  }

  getTimeToLive() {
    return Math.max(0, this.expiresAt - Date.now());
  }
}

class CacheManager {
  constructor(options = {}) {
    this.maxSize = options.maxSize || 1000;
    this.defaultTTL = options.defaultTTL || 3600000; // 1 hour
    this.enableCompression = options.enableCompression !== false;
    this.enableMetrics = options.enableMetrics !== false;
    this.cache = new Map();
    this.accessOrder = [];
    this.metrics = {
      hits: 0,
      misses: 0,
      sets: 0,
      deletes: 0,
      evictions: 0,
      compressions: 0,
      decompressions: 0
    };
    this.compressionThreshold = options.compressionThreshold || 1024; // 1KB
  }

  /**
   * Generate cache key from various inputs
   */
  generateKey(...args) {
    const keyString = JSON.stringify(args);
    return crypto.createHash('sha256').update(keyString).digest('hex');
  }

  /**
   * Set a value in cache
   */
  set(key, value, ttl = this.defaultTTL) {
    // Check if we need to evict entries
    if (this.cache.size >= this.maxSize) {
      this.evictLRU();
    }

    // Compress value if enabled and above threshold
    let finalValue = value;
    if (this.enableCompression && typeof value === 'string' && value.length > this.compressionThreshold) {
      finalValue = this.compress(value);
      this.metrics.compressions++;
    }

    const entry = new CacheEntry(key, finalValue, ttl);
    this.cache.set(key, entry);
    this.updateAccessOrder(key);
    this.metrics.sets++;

    return entry;
  }

  /**
   * Get a value from cache
   */
  get(key) {
    const entry = this.cache.get(key);
    
    if (!entry) {
      this.metrics.misses++;
      return null;
    }

    if (entry.isExpired()) {
      this.delete(key);
      this.metrics.misses++;
      return null;
    }

    entry.touch();
    this.updateAccessOrder(key);
    this.metrics.hits++;

    // Decompress if needed
    let value = entry.value;
    if (this.enableCompression && typeof value === 'string' && value.startsWith('COMPRESSED:')) {
      value = this.decompress(value);
      this.metrics.decompressions++;
    }

    return value;
  }

  /**
   * Get cache entry with metadata
   */
  getEntry(key) {
    const entry = this.cache.get(key);
    
    if (!entry || entry.isExpired()) {
      if (entry && entry.isExpired()) {
        this.delete(key);
      }
      return null;
    }

    entry.touch();
    this.updateAccessOrder(key);
    this.metrics.hits++;

    return {
      value: entry.value,
      createdAt: entry.createdAt,
      lastAccessed: entry.lastAccessed,
      accessCount: entry.accessCount,
      age: entry.getAge(),
      ttl: entry.getTimeToLive(),
      isExpired: entry.isExpired()
    };
  }

  /**
   * Delete a value from cache
   */
  delete(key) {
    const deleted = this.cache.delete(key);
    if (deleted) {
      this.removeFromAccessOrder(key);
      this.metrics.deletes++;
    }
    return deleted;
  }

  /**
   * Clear all cache
   */
  clear() {
    this.cache.clear();
    this.accessOrder = [];
    this.metrics.sets = 0;
    this.metrics.deletes = 0;
  }

  /**
   * Check if key exists and is not expired
   */
  has(key) {
    const entry = this.cache.get(key);
    return entry && !entry.isExpired();
  }

  /**
   * Get cache size
   */
  size() {
    return this.cache.size;
  }

  /**
   * Get cache statistics
   */
  getStats() {
    const totalRequests = this.metrics.hits + this.metrics.misses;
    const hitRate = totalRequests > 0 ? (this.metrics.hits / totalRequests) * 100 : 0;

    return {
      size: this.cache.size,
      maxSize: this.maxSize,
      hitRate: hitRate.toFixed(2) + '%',
      metrics: { ...this.metrics },
      memoryUsage: this.getMemoryUsage()
    };
  }

  /**
   * Get memory usage estimation
   */
  getMemoryUsage() {
    let totalSize = 0;
    for (const [key, entry] of this.cache) {
      totalSize += this.estimateSize(key) + this.estimateSize(entry.value);
    }
    return totalSize;
  }

  /**
   * Estimate size of a value in bytes
   */
  estimateSize(value) {
    if (typeof value === 'string') {
      return Buffer.byteLength(value, 'utf8');
    } else if (typeof value === 'object') {
      return Buffer.byteLength(JSON.stringify(value), 'utf8');
    } else {
      return 8; // Assume 8 bytes for numbers/booleans
    }
  }

  /**
   * Update access order for LRU
   */
  updateAccessOrder(key) {
    this.removeFromAccessOrder(key);
    this.accessOrder.push(key);
  }

  /**
   * Remove key from access order
   */
  removeFromAccessOrder(key) {
    const index = this.accessOrder.indexOf(key);
    if (index > -1) {
      this.accessOrder.splice(index, 1);
    }
  }

  /**
   * Evict least recently used entry
   */
  evictLRU() {
    if (this.accessOrder.length === 0) return;

    const lruKey = this.accessOrder.shift();
    this.cache.delete(lruKey);
    this.metrics.evictions++;
  }

  /**
   * Simple compression (for demo purposes - in production use proper compression)
   */
  compress(value) {
    // This is a simple compression for demonstration
    // In production, use proper compression libraries like zlib
    return 'COMPRESSED:' + Buffer.from(value).toString('base64');
  }

  /**
   * Simple decompression
   */
  decompress(value) {
    if (!value.startsWith('COMPRESSED:')) {
      return value;
    }
    const compressed = value.substring(11);
    return Buffer.from(compressed, 'base64').toString('utf8');
  }

  /**
   * Get all keys
   */
  keys() {
    return Array.from(this.cache.keys());
  }

  /**
   * Get all values
   */
  values() {
    return Array.from(this.cache.values()).map(entry => entry.value);
  }

  /**
   * Get cache entries with metadata
   */
  entries() {
    const result = [];
    for (const [key, entry] of this.cache) {
      result.push({
        key,
        value: entry.value,
        createdAt: entry.createdAt,
        lastAccessed: entry.lastAccessed,
        accessCount: entry.accessCount,
        age: entry.getAge(),
        ttl: entry.getTimeToLive(),
        isExpired: entry.isExpired()
      });
    }
    return result;
  }

  /**
   * Set multiple values at once
   */
  setMultiple(entries, ttl = this.defaultTTL) {
    const results = {};
    for (const [key, value] of Object.entries(entries)) {
      results[key] = this.set(key, value, ttl);
    }
    return results;
  }

  /**
   * Get multiple values at once
   */
  getMultiple(keys) {
    const results = {};
    for (const key of keys) {
      results[key] = this.get(key);
    }
    return results;
  }

  /**
   * Delete multiple keys at once
   */
  deleteMultiple(keys) {
    const results = {};
    for (const key of keys) {
      results[key] = this.delete(key);
    }
    return results;
  }

  /**
   * Clean expired entries
   */
  cleanup() {
    const expiredKeys = [];
    for (const [key, entry] of this.cache) {
      if (entry.isExpired()) {
        expiredKeys.push(key);
      }
    }

    for (const key of expiredKeys) {
      this.delete(key);
    }

    return expiredKeys.length;
  }

  /**
   * Set cache with callback for lazy loading
   */
  async getOrSet(key, callback, ttl = this.defaultTTL) {
    let value = this.get(key);
    
    if (value === null) {
      try {
        value = await callback();
        this.set(key, value, ttl);
      } catch (error) {
        throw error;
      }
    }

    return value;
  }

  /**
   * Invalidate cache by pattern
   */
  invalidatePattern(pattern) {
    const regex = new RegExp(pattern);
    const keysToDelete = this.keys().filter(key => regex.test(key));
    return this.deleteMultiple(keysToDelete);
  }
}

module.exports = {
  CacheManager,
  CacheEntry
}; 