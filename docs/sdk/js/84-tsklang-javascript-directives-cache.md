# TuskLang JavaScript Documentation: #cache Directive

## Overview

The `#cache` directive in TuskLang defines caching strategies and configurations, enabling declarative cache management with JavaScript/Node.js integration.

## TuskLang Syntax

```tsk
#cache redis
  host: localhost
  port: 6379
  password: ${REDIS_PASSWORD}
  db: 0
  ttl: 3600
  prefix: app:
  compression: true
  serialization: json

#cache memory
  max_size: 1000
  ttl: 1800
  eviction_policy: lru
  compression: false
  serialization: none

#cache file
  path: /tmp/cache
  ttl: 7200
  compression: true
  serialization: json
  max_files: 10000
  cleanup_interval: 3600

#cache multi_level
  levels:
    - type: memory
      ttl: 300
      max_size: 100
    - type: redis
      ttl: 3600
      host: localhost
      port: 6379
    - type: file
      ttl: 86400
      path: /tmp/cache
```

## JavaScript Integration

### Redis Cache Handler

```javascript
// redis-cache-handler.js
const Redis = require('ioredis');
const zlib = require('zlib');
const { promisify } = require('util');

class RedisCacheHandler {
  constructor(config) {
    this.config = config;
    this.client = null;
    this.prefix = config.prefix || 'cache:';
    this.defaultTTL = config.ttl || 3600;
    this.compression = config.compression || false;
    this.serialization = config.serialization || 'json';
  }

  async connect() {
    const redisConfig = {
      host: this.config.host,
      port: this.config.port,
      password: this.config.password,
      db: this.config.db || 0,
      retryDelayOnFailover: 100,
      maxRetriesPerRequest: 3,
      lazyConnect: true
    };

    this.client = new Redis(redisConfig);
    
    // Test connection
    await this.client.ping();
    
    console.log('Redis cache connected successfully');
  }

  async get(key) {
    if (!this.client) {
      throw new Error('Cache not connected');
    }

    const fullKey = this.prefix + key;
    let value = await this.client.get(fullKey);
    
    if (!value) {
      return null;
    }

    // Decompress if needed
    if (this.compression) {
      value = await this.decompress(value);
    }

    // Deserialize
    return this.deserialize(value);
  }

  async set(key, value, ttl = null) {
    if (!this.client) {
      throw new Error('Cache not connected');
    }

    const fullKey = this.prefix + key;
    const ttlToUse = ttl || this.defaultTTL;

    // Serialize
    let serialized = this.serialize(value);

    // Compress if needed
    if (this.compression) {
      serialized = await this.compress(serialized);
    }

    if (ttlToUse > 0) {
      await this.client.setex(fullKey, ttlToUse, serialized);
    } else {
      await this.client.set(fullKey, serialized);
    }
  }

  async delete(key) {
    if (!this.client) {
      throw new Error('Cache not connected');
    }

    const fullKey = this.prefix + key;
    return await this.client.del(fullKey);
  }

  async exists(key) {
    if (!this.client) {
      throw new Error('Cache not connected');
    }

    const fullKey = this.prefix + key;
    return await this.client.exists(fullKey);
  }

  async expire(key, ttl) {
    if (!this.client) {
      throw new Error('Cache not connected');
    }

    const fullKey = this.prefix + key;
    return await this.client.expire(fullKey, ttl);
  }

  async ttl(key) {
    if (!this.client) {
      throw new Error('Cache not connected');
    }

    const fullKey = this.prefix + key;
    return await this.client.ttl(fullKey);
  }

  async clear() {
    if (!this.client) {
      throw new Error('Cache not connected');
    }

    const keys = await this.client.keys(this.prefix + '*');
    if (keys.length > 0) {
      await this.client.del(...keys);
    }
  }

  async getStats() {
    if (!this.client) {
      throw new Error('Cache not connected');
    }

    const info = await this.client.info();
    const keys = await this.client.keys(this.prefix + '*');
    
    return {
      keys: keys.length,
      info: info,
      prefix: this.prefix,
      compression: this.compression,
      serialization: this.serialization
    };
  }

  async compress(data) {
    return await promisify(zlib.gzip)(data);
  }

  async decompress(data) {
    return await promisify(zlib.gunzip)(data);
  }

  serialize(value) {
    switch (this.serialization) {
      case 'json':
        return JSON.stringify(value);
      case 'msgpack':
        const msgpack = require('msgpack-lite');
        return msgpack.encode(value);
      default:
        return String(value);
    }
  }

  deserialize(value) {
    switch (this.serialization) {
      case 'json':
        return JSON.parse(value);
      case 'msgpack':
        const msgpack = require('msgpack-lite');
        return msgpack.decode(value);
      default:
        return value;
    }
  }

  async disconnect() {
    if (this.client) {
      await this.client.quit();
      this.client = null;
    }
  }
}

module.exports = RedisCacheHandler;
```

### Memory Cache Handler

```javascript
// memory-cache-handler.js
const LRU = require('lru-cache');

class MemoryCacheHandler {
  constructor(config) {
    this.config = config;
    this.cache = null;
    this.stats = {
      hits: 0,
      misses: 0,
      sets: 0,
      deletes: 0
    };
  }

  async connect() {
    const options = {
      max: this.config.max_size || 1000,
      ttl: (this.config.ttl || 1800) * 1000, // Convert to milliseconds
      updateAgeOnGet: true,
      allowStale: false,
      dispose: (key, value) => {
        // Cleanup callback
        console.log(`Evicted key: ${key}`);
      }
    };

    this.cache = new LRU(options);
    
    console.log('Memory cache initialized successfully');
  }

  async get(key) {
    if (!this.cache) {
      throw new Error('Cache not initialized');
    }

    const value = this.cache.get(key);
    
    if (value !== undefined) {
      this.stats.hits++;
      return value;
    } else {
      this.stats.misses++;
      return null;
    }
  }

  async set(key, value, ttl = null) {
    if (!this.cache) {
      throw new Error('Cache not initialized');
    }

    const ttlToUse = ttl ? ttl * 1000 : this.config.ttl * 1000;
    this.cache.set(key, value, { ttl: ttlToUse });
    this.stats.sets++;
  }

  async delete(key) {
    if (!this.cache) {
      throw new Error('Cache not initialized');
    }

    const deleted = this.cache.delete(key);
    if (deleted) {
      this.stats.deletes++;
    }
    return deleted;
  }

  async exists(key) {
    if (!this.cache) {
      throw new Error('Cache not initialized');
    }

    return this.cache.has(key);
  }

  async expire(key, ttl) {
    if (!this.cache) {
      throw new Error('Cache not initialized');
    }

    const value = this.cache.get(key);
    if (value !== undefined) {
      this.cache.set(key, value, { ttl: ttl * 1000 });
      return true;
    }
    return false;
  }

  async ttl(key) {
    if (!this.cache) {
      throw new Error('Cache not initialized');
    }

    // LRU cache doesn't expose TTL directly
    // This is a simplified implementation
    return this.cache.has(key) ? this.config.ttl : -1;
  }

  async clear() {
    if (!this.cache) {
      throw new Error('Cache not initialized');
    }

    this.cache.clear();
  }

  async getStats() {
    if (!this.cache) {
      throw new Error('Cache not initialized');
    }

    return {
      ...this.stats,
      size: this.cache.size,
      maxSize: this.cache.max,
      hitRate: this.stats.hits / (this.stats.hits + this.stats.misses) || 0
    };
  }

  async disconnect() {
    if (this.cache) {
      this.cache.clear();
      this.cache = null;
    }
  }
}

module.exports = MemoryCacheHandler;
```

### File Cache Handler

```javascript
// file-cache-handler.js
const fs = require('fs').promises;
const path = require('path');
const crypto = require('crypto');
const zlib = require('zlib');
const { promisify } = require('util');

class FileCacheHandler {
  constructor(config) {
    this.config = config;
    this.cachePath = config.path || '/tmp/cache';
    this.defaultTTL = config.ttl || 7200;
    this.compression = config.compression || false;
    this.serialization = config.serialization || 'json';
    this.maxFiles = config.max_files || 10000;
    this.cleanupInterval = config.cleanup_interval || 3600;
    this.cleanupTimer = null;
  }

  async connect() {
    // Ensure cache directory exists
    await fs.mkdir(this.cachePath, { recursive: true });
    
    // Start cleanup timer
    this.startCleanupTimer();
    
    console.log('File cache initialized successfully');
  }

  async get(key) {
    const filePath = this.getFilePath(key);
    
    try {
      const stats = await fs.stat(filePath);
      
      // Check if file is expired
      if (Date.now() - stats.mtime.getTime() > this.defaultTTL * 1000) {
        await this.delete(key);
        return null;
      }

      let data = await fs.readFile(filePath);
      
      // Decompress if needed
      if (this.compression) {
        data = await this.decompress(data);
      }

      // Deserialize
      return this.deserialize(data.toString());
    } catch (error) {
      if (error.code === 'ENOENT') {
        return null;
      }
      throw error;
    }
  }

  async set(key, value, ttl = null) {
    const filePath = this.getFilePath(key);
    const ttlToUse = ttl || this.defaultTTL;

    // Serialize
    let data = this.serialize(value);

    // Compress if needed
    if (this.compression) {
      data = await this.compress(data);
    }

    // Write to file
    await fs.writeFile(filePath, data);
    
    // Set file modification time for TTL tracking
    const mtime = new Date(Date.now() + ttlToUse * 1000);
    await fs.utimes(filePath, mtime, mtime);
  }

  async delete(key) {
    const filePath = this.getFilePath(key);
    
    try {
      await fs.unlink(filePath);
      return true;
    } catch (error) {
      if (error.code === 'ENOENT') {
        return false;
      }
      throw error;
    }
  }

  async exists(key) {
    const filePath = this.getFilePath(key);
    
    try {
      await fs.access(filePath);
      return true;
    } catch (error) {
      return false;
    }
  }

  async expire(key, ttl) {
    const filePath = this.getFilePath(key);
    
    try {
      const mtime = new Date(Date.now() + ttl * 1000);
      await fs.utimes(filePath, mtime, mtime);
      return true;
    } catch (error) {
      return false;
    }
  }

  async ttl(key) {
    const filePath = this.getFilePath(key);
    
    try {
      const stats = await fs.stat(filePath);
      const remaining = Math.ceil((stats.mtime.getTime() - Date.now()) / 1000);
      return remaining > 0 ? remaining : -1;
    } catch (error) {
      return -1;
    }
  }

  async clear() {
    const files = await fs.readdir(this.cachePath);
    
    for (const file of files) {
      const filePath = path.join(this.cachePath, file);
      try {
        await fs.unlink(filePath);
      } catch (error) {
        console.error(`Error deleting file ${filePath}:`, error);
      }
    }
  }

  async getStats() {
    const files = await fs.readdir(this.cachePath);
    const stats = await fs.stat(this.cachePath);
    
    return {
      files: files.length,
      maxFiles: this.maxFiles,
      directorySize: stats.size,
      compression: this.compression,
      serialization: this.serialization,
      path: this.cachePath
    };
  }

  getFilePath(key) {
    const hash = crypto.createHash('md5').update(key).digest('hex');
    return path.join(this.cachePath, hash);
  }

  async compress(data) {
    return await promisify(zlib.gzip)(data);
  }

  async decompress(data) {
    return await promisify(zlib.gunzip)(data);
  }

  serialize(value) {
    switch (this.serialization) {
      case 'json':
        return JSON.stringify(value);
      case 'msgpack':
        const msgpack = require('msgpack-lite');
        return msgpack.encode(value);
      default:
        return String(value);
    }
  }

  deserialize(value) {
    switch (this.serialization) {
      case 'json':
        return JSON.parse(value);
      case 'msgpack':
        const msgpack = require('msgpack-lite');
        return msgpack.decode(value);
      default:
        return value;
    }
  }

  startCleanupTimer() {
    this.cleanupTimer = setInterval(async () => {
      await this.cleanup();
    }, this.cleanupInterval * 1000);
  }

  async cleanup() {
    try {
      const files = await fs.readdir(this.cachePath);
      const now = Date.now();
      
      for (const file of files) {
        const filePath = path.join(this.cachePath, file);
        try {
          const stats = await fs.stat(filePath);
          
          // Remove expired files
          if (now - stats.mtime.getTime() > this.defaultTTL * 1000) {
            await fs.unlink(filePath);
          }
        } catch (error) {
          console.error(`Error during cleanup for ${filePath}:`, error);
        }
      }
      
      // Remove excess files if over limit
      if (files.length > this.maxFiles) {
        const sortedFiles = await this.getFilesByAge();
        const filesToRemove = sortedFiles.slice(this.maxFiles);
        
        for (const file of filesToRemove) {
          try {
            await fs.unlink(file);
          } catch (error) {
            console.error(`Error removing excess file ${file}:`, error);
          }
        }
      }
    } catch (error) {
      console.error('Error during cache cleanup:', error);
    }
  }

  async getFilesByAge() {
    const files = await fs.readdir(this.cachePath);
    const fileStats = [];
    
    for (const file of files) {
      const filePath = path.join(this.cachePath, file);
      try {
        const stats = await fs.stat(filePath);
        fileStats.push({ path: filePath, mtime: stats.mtime });
      } catch (error) {
        // Skip files that can't be stat'd
      }
    }
    
    return fileStats
      .sort((a, b) => a.mtime.getTime() - b.mtime.getTime())
      .map(file => file.path);
  }

  async disconnect() {
    if (this.cleanupTimer) {
      clearInterval(this.cleanupTimer);
      this.cleanupTimer = null;
    }
  }
}

module.exports = FileCacheHandler;
```

### Multi-Level Cache Handler

```javascript
// multi-level-cache-handler.js
class MultiLevelCacheHandler {
  constructor(config) {
    this.config = config;
    this.levels = [];
    this.stats = {
      hits: 0,
      misses: 0,
      sets: 0
    };
  }

  async connect() {
    for (const levelConfig of this.config.levels) {
      const handler = await this.createHandler(levelConfig);
      this.levels.push(handler);
    }
    
    console.log(`Multi-level cache initialized with ${this.levels.length} levels`);
  }

  async createHandler(config) {
    switch (config.type) {
      case 'memory':
        const MemoryCacheHandler = require('./memory-cache-handler');
        const memoryHandler = new MemoryCacheHandler(config);
        await memoryHandler.connect();
        return memoryHandler;
        
      case 'redis':
        const RedisCacheHandler = require('./redis-cache-handler');
        const redisHandler = new RedisCacheHandler(config);
        await redisHandler.connect();
        return redisHandler;
        
      case 'file':
        const FileCacheHandler = require('./file-cache-handler');
        const fileHandler = new FileCacheHandler(config);
        await fileHandler.connect();
        return fileHandler;
        
      default:
        throw new Error(`Unsupported cache type: ${config.type}`);
    }
  }

  async get(key) {
    // Try each level from fastest to slowest
    for (let i = 0; i < this.levels.length; i++) {
      try {
        const value = await this.levels[i].get(key);
        
        if (value !== null) {
          this.stats.hits++;
          
          // Populate faster levels with the value
          await this.populateFasterLevels(key, value, i);
          
          return value;
        }
      } catch (error) {
        console.error(`Error reading from cache level ${i}:`, error);
      }
    }
    
    this.stats.misses++;
    return null;
  }

  async set(key, value, ttl = null) {
    this.stats.sets++;
    
    // Set in all levels
    const promises = this.levels.map(level => 
      level.set(key, value, ttl).catch(error => {
        console.error('Error setting cache value:', error);
      })
    );
    
    await Promise.all(promises);
  }

  async delete(key) {
    // Delete from all levels
    const promises = this.levels.map(level => 
      level.delete(key).catch(error => {
        console.error('Error deleting cache value:', error);
      })
    );
    
    await Promise.all(promises);
  }

  async exists(key) {
    // Check each level
    for (const level of this.levels) {
      try {
        if (await level.exists(key)) {
          return true;
        }
      } catch (error) {
        console.error('Error checking cache existence:', error);
      }
    }
    
    return false;
  }

  async clear() {
    // Clear all levels
    const promises = this.levels.map(level => 
      level.clear().catch(error => {
        console.error('Error clearing cache:', error);
      })
    );
    
    await Promise.all(promises);
  }

  async getStats() {
    const levelStats = await Promise.all(
      this.levels.map(level => level.getStats().catch(() => ({})))
    );
    
    return {
      levels: levelStats,
      totalHits: this.stats.hits,
      totalMisses: this.stats.misses,
      totalSets: this.stats.sets,
      hitRate: this.stats.hits / (this.stats.hits + this.stats.misses) || 0
    };
  }

  async populateFasterLevels(key, value, sourceLevel) {
    // Populate faster levels (lower indices) with the value
    const promises = [];
    
    for (let i = 0; i < sourceLevel; i++) {
      const level = this.levels[i];
      const levelConfig = this.config.levels[i];
      
      promises.push(
        level.set(key, value, levelConfig.ttl).catch(error => {
          console.error(`Error populating level ${i}:`, error);
        })
      );
    }
    
    await Promise.all(promises);
  }

  async disconnect() {
    // Disconnect all levels
    const promises = this.levels.map(level => 
      level.disconnect().catch(error => {
        console.error('Error disconnecting cache level:', error);
      })
    );
    
    await Promise.all(promises);
    this.levels = [];
  }
}

module.exports = MultiLevelCacheHandler;
```

## TypeScript Implementation

```typescript
// cache-handler.types.ts
export interface CacheConfig {
  host?: string;
  port?: number;
  password?: string;
  db?: number;
  ttl?: number;
  prefix?: string;
  compression?: boolean;
  serialization?: 'json' | 'msgpack' | 'none';
  max_size?: number;
  eviction_policy?: 'lru' | 'fifo' | 'lfu';
  path?: string;
  max_files?: number;
  cleanup_interval?: number;
  levels?: CacheLevelConfig[];
}

export interface CacheLevelConfig {
  type: 'memory' | 'redis' | 'file';
  ttl?: number;
  max_size?: number;
  host?: string;
  port?: number;
  path?: string;
  [key: string]: any;
}

export interface CacheStats {
  hits: number;
  misses: number;
  sets: number;
  deletes?: number;
  size?: number;
  maxSize?: number;
  hitRate?: number;
  keys?: number;
  info?: string;
  files?: number;
  maxFiles?: number;
  directorySize?: number;
  levels?: any[];
  totalHits?: number;
  totalMisses?: number;
  totalSets?: number;
}

export interface CacheHandler {
  connect(): Promise<void>;
  get(key: string): Promise<any>;
  set(key: string, value: any, ttl?: number): Promise<void>;
  delete(key: string): Promise<boolean>;
  exists(key: string): Promise<boolean>;
  expire(key: string, ttl: number): Promise<boolean>;
  ttl(key: string): Promise<number>;
  clear(): Promise<void>;
  getStats(): Promise<CacheStats>;
  disconnect(): Promise<void>;
}

// cache-handler.ts
import { CacheConfig, CacheHandler, CacheStats, CacheLevelConfig } from './cache-handler.types';

export class TypeScriptCacheHandler implements CacheHandler {
  protected config: CacheConfig;

  constructor(config: CacheConfig) {
    this.config = config;
  }

  async connect(): Promise<void> {
    throw new Error('Method not implemented');
  }

  async get(key: string): Promise<any> {
    throw new Error('Method not implemented');
  }

  async set(key: string, value: any, ttl?: number): Promise<void> {
    throw new Error('Method not implemented');
  }

  async delete(key: string): Promise<boolean> {
    throw new Error('Method not implemented');
  }

  async exists(key: string): Promise<boolean> {
    throw new Error('Method not implemented');
  }

  async expire(key: string, ttl: number): Promise<boolean> {
    throw new Error('Method not implemented');
  }

  async ttl(key: string): Promise<number> {
    throw new Error('Method not implemented');
  }

  async clear(): Promise<void> {
    throw new Error('Method not implemented');
  }

  async getStats(): Promise<CacheStats> {
    throw new Error('Method not implemented');
  }

  async disconnect(): Promise<void> {
    throw new Error('Method not implemented');
  }
}

export class TypeScriptRedisCacheHandler extends TypeScriptCacheHandler {
  private client: any;
  private prefix: string;
  private defaultTTL: number;
  private compression: boolean;
  private serialization: string;

  constructor(config: CacheConfig) {
    super(config);
    this.prefix = config.prefix || 'cache:';
    this.defaultTTL = config.ttl || 3600;
    this.compression = config.compression || false;
    this.serialization = config.serialization || 'json';
  }

  async connect(): Promise<void> {
    const Redis = require('ioredis');
    
    this.client = new Redis({
      host: this.config.host,
      port: this.config.port,
      password: this.config.password,
      db: this.config.db || 0
    });
    
    await this.client.ping();
  }

  async get(key: string): Promise<any> {
    const fullKey = this.prefix + key;
    let value = await this.client.get(fullKey);
    
    if (!value) {
      return null;
    }

    if (this.compression) {
      value = await this.decompress(value);
    }

    return this.deserialize(value);
  }

  async set(key: string, value: any, ttl?: number): Promise<void> {
    const fullKey = this.prefix + key;
    const ttlToUse = ttl || this.defaultTTL;

    let serialized = this.serialize(value);

    if (this.compression) {
      serialized = await this.compress(serialized);
    }

    if (ttlToUse > 0) {
      await this.client.setex(fullKey, ttlToUse, serialized);
    } else {
      await this.client.set(fullKey, serialized);
    }
  }

  async delete(key: string): Promise<boolean> {
    const fullKey = this.prefix + key;
    return await this.client.del(fullKey);
  }

  async exists(key: string): Promise<boolean> {
    const fullKey = this.prefix + key;
    return await this.client.exists(fullKey);
  }

  async expire(key: string, ttl: number): Promise<boolean> {
    const fullKey = this.prefix + key;
    return await this.client.expire(fullKey, ttl);
  }

  async ttl(key: string): Promise<number> {
    const fullKey = this.prefix + key;
    return await this.client.ttl(fullKey);
  }

  async clear(): Promise<void> {
    const keys = await this.client.keys(this.prefix + '*');
    if (keys.length > 0) {
      await this.client.del(...keys);
    }
  }

  async getStats(): Promise<CacheStats> {
    const info = await this.client.info();
    const keys = await this.client.keys(this.prefix + '*');
    
    return {
      hits: 0,
      misses: 0,
      sets: 0,
      keys: keys.length,
      info: info
    };
  }

  async disconnect(): Promise<void> {
    if (this.client) {
      await this.client.quit();
      this.client = null;
    }
  }

  private async compress(data: string): Promise<Buffer> {
    const zlib = require('zlib');
    const { promisify } = require('util');
    return await promisify(zlib.gzip)(data);
  }

  private async decompress(data: Buffer): Promise<string> {
    const zlib = require('zlib');
    const { promisify } = require('util');
    return await promisify(zlib.gunzip)(data);
  }

  private serialize(value: any): string {
    switch (this.serialization) {
      case 'json':
        return JSON.stringify(value);
      case 'msgpack':
        const msgpack = require('msgpack-lite');
        return msgpack.encode(value);
      default:
        return String(value);
    }
  }

  private deserialize(value: string): any {
    switch (this.serialization) {
      case 'json':
        return JSON.parse(value);
      case 'msgpack':
        const msgpack = require('msgpack-lite');
        return msgpack.decode(value);
      default:
        return value;
    }
  }
}
```

## Advanced Usage Scenarios

### Cache Warming

```javascript
// cache-warmer.js
class CacheWarmer {
  constructor(cacheHandler) {
    this.cache = cacheHandler;
    this.warmingQueue = [];
    this.isWarming = false;
  }

  async warmCache(key, dataProvider, ttl = null) {
    this.warmingQueue.push({ key, dataProvider, ttl });
    
    if (!this.isWarming) {
      await this.processWarmingQueue();
    }
  }

  async processWarmingQueue() {
    this.isWarming = true;
    
    while (this.warmingQueue.length > 0) {
      const { key, dataProvider, ttl } = this.warmingQueue.shift();
      
      try {
        const data = await dataProvider();
        await this.cache.set(key, data, ttl);
        console.log(`Cache warmed for key: ${key}`);
      } catch (error) {
        console.error(`Failed to warm cache for key ${key}:`, error);
      }
    }
    
    this.isWarming = false;
  }

  async warmFromDatabase(table, keyField, valueField, ttl = null) {
    const db = require('./database');
    const rows = await db.query(`SELECT ${keyField}, ${valueField} FROM ${table}`);
    
    for (const row of rows) {
      await this.warmCache(row[keyField], () => row[valueField], ttl);
    }
  }

  async warmFromAPI(endpoints, ttl = null) {
    const axios = require('axios');
    
    for (const endpoint of endpoints) {
      const key = `api:${endpoint}`;
      await this.warmCache(key, async () => {
        const response = await axios.get(endpoint);
        return response.data;
      }, ttl);
    }
  }
}
```

### Cache Invalidation Strategies

```javascript
// cache-invalidator.js
class CacheInvalidator {
  constructor(cacheHandler) {
    this.cache = cacheHandler;
    this.invalidationPatterns = new Map();
  }

  registerPattern(pattern, keys) {
    this.invalidationPatterns.set(pattern, keys);
  }

  async invalidateByPattern(pattern) {
    const keys = this.invalidationPatterns.get(pattern);
    if (!keys) {
      throw new Error(`Pattern '${pattern}' not registered`);
    }
    
    const promises = keys.map(key => this.cache.delete(key));
    await Promise.all(promises);
    
    console.log(`Invalidated ${keys.length} keys for pattern: ${pattern}`);
  }

  async invalidateByPrefix(prefix) {
    if (this.cache.client && this.cache.client.keys) {
      // Redis-specific implementation
      const keys = await this.cache.client.keys(`${prefix}*`);
      if (keys.length > 0) {
        await this.cache.client.del(...keys);
      }
    } else {
      // Generic implementation
      console.warn('Prefix-based invalidation not supported for this cache type');
    }
  }

  async invalidateByTags(tags) {
    for (const tag of tags) {
      await this.invalidateByPattern(`tag:${tag}`);
    }
  }

  async invalidateByTimeRange(startTime, endTime) {
    // Implementation depends on cache type and data structure
    console.log(`Invalidating cache entries between ${startTime} and ${endTime}`);
  }
}
```

## Real-World Examples

### E-commerce Cache Strategy

```javascript
// ecommerce-cache.js
class EcommerceCache {
  constructor(cacheHandler) {
    this.cache = cacheHandler;
    this.warmer = new CacheWarmer(cacheHandler);
    this.invalidator = new CacheInvalidator(cacheHandler);
  }

  async cacheProduct(productId) {
    const key = `product:${productId}`;
    const ttl = 3600; // 1 hour
    
    return await this.cache.get(key) || await this.fetchAndCacheProduct(productId, key, ttl);
  }

  async fetchAndCacheProduct(productId, key, ttl) {
    const db = require('./database');
    const product = await db.query('SELECT * FROM products WHERE id = ?', [productId]);
    
    if (product) {
      await this.cache.set(key, product, ttl);
      return product;
    }
    
    return null;
  }

  async cacheCategory(categoryId) {
    const key = `category:${categoryId}`;
    const ttl = 1800; // 30 minutes
    
    return await this.cache.get(key) || await this.fetchAndCacheCategory(categoryId, key, ttl);
  }

  async fetchAndCacheCategory(categoryId, key, ttl) {
    const db = require('./database');
    const products = await db.query(
      'SELECT * FROM products WHERE category_id = ? ORDER BY created_at DESC LIMIT 50',
      [categoryId]
    );
    
    await this.cache.set(key, products, ttl);
    return products;
  }

  async invalidateProduct(productId) {
    await this.cache.delete(`product:${productId}`);
    await this.invalidator.invalidateByPattern('category:*');
  }

  async warmPopularProducts() {
    const db = require('./database');
    const popularProducts = await db.query(`
      SELECT id FROM products 
      ORDER BY views DESC, sales_count DESC 
      LIMIT 100
    `);
    
    for (const product of popularProducts) {
      await this.warmer.warmCache(
        `product:${product.id}`,
        () => this.fetchAndCacheProduct(product.id, `product:${product.id}`, 3600)
      );
    }
  }
}
```

### API Response Caching

```javascript
// api-cache.js
class APICache {
  constructor(cacheHandler) {
    this.cache = cacheHandler;
  }

  async cacheResponse(endpoint, params, response, ttl = 300) {
    const key = this.generateKey(endpoint, params);
    await this.cache.set(key, response, ttl);
  }

  async getCachedResponse(endpoint, params) {
    const key = this.generateKey(endpoint, params);
    return await this.cache.get(key);
  }

  generateKey(endpoint, params) {
    const paramString = JSON.stringify(params);
    const hash = require('crypto').createHash('md5').update(paramString).digest('hex');
    return `api:${endpoint}:${hash}`;
  }

  async cacheMiddleware(req, res, next) {
    if (req.method !== 'GET') {
      return next();
    }

    const cached = await this.getCachedResponse(req.path, req.query);
    
    if (cached) {
      res.set('X-Cache', 'HIT');
      return res.json(cached);
    }

    res.set('X-Cache', 'MISS');
    
    // Store original send method
    const originalSend = res.json;
    
    // Override send method to cache response
    res.json = function(data) {
      this.cacheResponse(req.path, req.query, data);
      return originalSend.call(this, data);
    }.bind(this);
    
    next();
  }
}
```

## Performance Considerations

### Cache Hit Rate Optimization

```javascript
// cache-optimizer.js
class CacheOptimizer {
  constructor(cacheHandler) {
    this.cache = cacheHandler;
    this.analytics = new Map();
  }

  async trackAccess(key, hit) {
    if (!this.analytics.has(key)) {
      this.analytics.set(key, { hits: 0, misses: 0, lastAccess: Date.now() });
    }
    
    const stats = this.analytics.get(key);
    if (hit) {
      stats.hits++;
    } else {
      stats.misses++;
    }
    stats.lastAccess = Date.now();
  }

  async getHitRate(key) {
    const stats = this.analytics.get(key);
    if (!stats) return 0;
    
    const total = stats.hits + stats.misses;
    return total > 0 ? stats.hits / total : 0;
  }

  async getHotKeys(limit = 10) {
    const keys = Array.from(this.analytics.entries())
      .sort((a, b) => {
        const aTotal = a[1].hits + a[1].misses;
        const bTotal = b[1].hits + b[1].misses;
        return bTotal - aTotal;
      })
      .slice(0, limit)
      .map(([key]) => key);
    
    return keys;
  }

  async getColdKeys(limit = 10) {
    const now = Date.now();
    const keys = Array.from(this.analytics.entries())
      .filter(([_, stats]) => now - stats.lastAccess > 24 * 60 * 60 * 1000) // 24 hours
      .sort((a, b) => a[1].lastAccess - b[1].lastAccess)
      .slice(0, limit)
      .map(([key]) => key);
    
    return keys;
  }

  async optimizeTTL(key, baseTTL = 3600) {
    const hitRate = await this.getHitRate(key);
    
    if (hitRate > 0.8) {
      // High hit rate, increase TTL
      return baseTTL * 2;
    } else if (hitRate < 0.2) {
      // Low hit rate, decrease TTL
      return Math.max(baseTTL / 2, 300);
    }
    
    return baseTTL;
  }
}
```

### Memory Usage Optimization

```javascript
// memory-optimizer.js
class MemoryOptimizer {
  constructor(cacheHandler) {
    this.cache = cacheHandler;
    this.memoryThreshold = 0.8; // 80% memory usage threshold
  }

  async checkMemoryUsage() {
    if (process.memoryUsage) {
      const usage = process.memoryUsage();
      const heapUsed = usage.heapUsed / usage.heapTotal;
      
      if (heapUsed > this.memoryThreshold) {
        await this.optimizeMemory();
      }
    }
  }

  async optimizeMemory() {
    console.log('Memory usage high, optimizing cache...');
    
    // Clear least recently used items
    if (this.cache.cache && this.cache.cache.prune) {
      this.cache.cache.prune();
    }
    
    // Force garbage collection if available
    if (global.gc) {
      global.gc();
    }
  }

  async setMemoryThreshold(threshold) {
    this.memoryThreshold = threshold;
  }
}
```

## Security Notes

### Cache Poisoning Prevention

```javascript
// cache-security.js
class CacheSecurity {
  constructor(cacheHandler) {
    this.cache = cacheHandler;
  }

  sanitizeKey(key) {
    // Remove dangerous characters
    return key.replace(/[^a-zA-Z0-9\-_.]/g, '');
  }

  validateValue(value) {
    // Check for potentially dangerous content
    if (typeof value === 'string' && value.length > 1000000) {
      throw new Error('Value too large for cache');
    }
    
    return value;
  }

  async secureSet(key, value, ttl = null) {
    const sanitizedKey = this.sanitizeKey(key);
    const validatedValue = this.validateValue(value);
    
    return await this.cache.set(sanitizedKey, validatedValue, ttl);
  }

  async secureGet(key) {
    const sanitizedKey = this.sanitizeKey(key);
    return await this.cache.get(sanitizedKey);
  }
}
```

### Cache Encryption

```javascript
// cache-encryption.js
const crypto = require('crypto');

class CacheEncryption {
  constructor(secretKey) {
    this.secretKey = secretKey;
    this.algorithm = 'aes-256-cbc';
  }

  encrypt(data) {
    const iv = crypto.randomBytes(16);
    const cipher = crypto.createCipher(this.algorithm, this.secretKey);
    
    let encrypted = cipher.update(JSON.stringify(data), 'utf8', 'hex');
    encrypted += cipher.final('hex');
    
    return {
      iv: iv.toString('hex'),
      data: encrypted
    };
  }

  decrypt(encryptedData) {
    const decipher = crypto.createDecipher(this.algorithm, this.secretKey);
    
    let decrypted = decipher.update(encryptedData.data, 'hex', 'utf8');
    decrypted += decipher.final('utf8');
    
    return JSON.parse(decrypted);
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
    if (!config.type) {
      throw new Error('Cache type is required');
    }
    
    if (config.ttl && config.ttl < 0) {
      throw new Error('TTL must be positive');
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
  constructor(cacheHandler) {
    this.cache = cacheHandler;
    this.metrics = {
      requests: 0,
      hits: 0,
      misses: 0,
      errors: 0,
      avgResponseTime: 0
    };
  }

  async checkHealth() {
    try {
      const start = Date.now();
      await this.cache.set('health_check', 'ok', 60);
      const setTime = Date.now() - start;
      
      const getStart = Date.now();
      await this.cache.get('health_check');
      const getTime = Date.now() - getStart;
      
      return {
        status: 'healthy',
        setTime,
        getTime,
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

  resetMetrics() {
    this.metrics = {
      requests: 0,
      hits: 0,
      misses: 0,
      errors: 0,
      avgResponseTime: 0
    };
  }
}
```

## Related Topics

- [@cache Operator](./48-tsklang-javascript-operator-cache.md)
- [@redis Operator](./26-tsklang-javascript-operator-redis.md)
- [@metrics Operator](./49-tsklang-javascript-operator-metrics.md)
- [@optimize Operator](./50-tsklang-javascript-operator-optimize.md)
- [@database Directive](./83-tsklang-javascript-directives-database.md) 