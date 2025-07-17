# 🗄️ Advanced Caching Strategies with TuskLang & PHP

## Introduction
Caching is the secret weapon of high-performance applications. TuskLang and PHP let you implement sophisticated caching strategies with config-driven multi-level caches, intelligent invalidation, and distributed coordination that scales from startup to enterprise.

## Key Features
- **Multi-level caching (L1, L2, L3)**
- **Cache invalidation strategies**
- **Cache warming and preloading**
- **Distributed caching with Redis/Memcached**
- **Cache compression and optimization**
- **Cache-aside patterns**

## Example: Caching Configuration
```ini
[caching]
l1_backend: memory
l1_size: @env("L1_CACHE_SIZE", "100MB")
l2_backend: redis
l2_uri: @env.secure("REDIS_URI")
l3_backend: database
l3_query: @query("SELECT * FROM cache_store WHERE key = ?")
compression: @env("CACHE_COMPRESSION", true)
metrics: @metrics("cache_hit_rate", 0)
```

## PHP: Multi-Level Cache Implementation
```php
<?php

namespace App\Caching;

use TuskLang\Config;
use TuskLang\Operators\Env;
use TuskLang\Operators\Metrics;
use TuskLang\Operators\Go;

class MultiLevelCache
{
    private $config;
    private $l1;
    private $l2;
    private $l3;
    private $stats;
    
    public function __construct()
    {
        $this->config = new Config();
        $this->setupCaches();
        $this->stats = new CacheStats();
    }
    
    public function get($key, $default = null)
    {
        // Try L1 cache first
        $value = $this->l1->get($key);
        if ($value !== null) {
            $this->stats->recordL1Hit();
            return $value;
        }
        
        // Try L2 cache
        $value = $this->l2->get($key);
        if ($value !== null) {
            // Store in L1 for next time
            $this->l1->set($key, $value);
            $this->stats->recordL2Hit();
            return $value;
        }
        
        // Try L3 cache
        $value = $this->l3->get($key);
        if ($value !== null) {
            // Store in L2 and L1
            $this->l2->set($key, $value);
            $this->l1->set($key, $value);
            $this->stats->recordL3Hit();
            return $value;
        }
        
        $this->stats->recordMiss();
        return $default;
    }
    
    public function set($key, $value, $ttl = null)
    {
        $ttl = $ttl ?? $this->config->get('caching.default_ttl', 3600);
        
        // Set in all levels
        $this->l1->set($key, $value, $ttl);
        $this->l2->set($key, $value, $ttl);
        $this->l3->set($key, $value, $ttl);
        
        // Record metrics
        Metrics::record("cache_sets", 1, ["key" => $key]);
    }
    
    public function delete($key)
    {
        // Delete from all levels
        $this->l1->delete($key);
        $this->l2->delete($key);
        $this->l3->delete($key);
        
        // Record metrics
        Metrics::record("cache_deletes", 1, ["key" => $key]);
    }
    
    public function invalidatePattern($pattern)
    {
        // Invalidate by pattern
        $this->l1->invalidatePattern($pattern);
        $this->l2->invalidatePattern($pattern);
        $this->l3->invalidatePattern($pattern);
    }
    
    private function setupCaches()
    {
        $this->l1 = $this->createL1Cache();
        $this->l2 = $this->createL2Cache();
        $this->l3 = $this->createL3Cache();
    }
    
    private function createL1Cache()
    {
        $type = $this->config->get('caching.l1_backend', 'memory');
        
        switch ($type) {
            case 'memory':
                return new MemoryCache();
            case 'apcu':
                return new APCuCache();
            default:
                throw new \Exception("Unknown L1 cache backend: {$type}");
        }
    }
    
    private function createL2Cache()
    {
        $type = $this->config->get('caching.l2_backend', 'redis');
        
        switch ($type) {
            case 'redis':
                return new RedisCache(Env::secure('REDIS_URI'));
            case 'memcached':
                return new MemcachedCache(Env::get('MEMCACHED_HOST'));
            default:
                throw new \Exception("Unknown L2 cache backend: {$type}");
        }
    }
    
    private function createL3Cache()
    {
        $type = $this->config->get('caching.l3_backend', 'database');
        
        switch ($type) {
            case 'database':
                return new DatabaseCache();
            case 'file':
                return new FileCache();
            default:
                throw new \Exception("Unknown L3 cache backend: {$type}");
        }
    }
}

class CacheStats
{
    private $l1Hits = 0;
    private $l2Hits = 0;
    private $l3Hits = 0;
    private $misses = 0;
    
    public function recordL1Hit() { $this->l1Hits++; }
    public function recordL2Hit() { $this->l2Hits++; }
    public function recordL3Hit() { $this->l3Hits++; }
    public function recordMiss() { $this->misses++; }
    
    public function getHitRate()
    {
        $total = $this->l1Hits + $this->l2Hits + $this->l3Hits + $this->misses;
        return $total > 0 ? ($this->l1Hits + $this->l2Hits + $this->l3Hits) / $total : 0;
    }
    
    public function getStats()
    {
        return [
            'l1_hits' => $this->l1Hits,
            'l2_hits' => $this->l2Hits,
            'l3_hits' => $this->l3Hits,
            'misses' => $this->misses,
            'hit_rate' => $this->getHitRate()
        ];
    }
}
```

## Cache Invalidation Strategies
```php
<?php

namespace App\Caching\Invalidation;

use TuskLang\Config;

class CacheInvalidator
{
    private $config;
    private $strategies = [];
    
    public function __construct()
    {
        $this->config = new Config();
        $this->loadStrategies();
    }
    
    public function invalidate($pattern, $strategy = 'time_based')
    {
        if (!isset($this->strategies[$strategy])) {
            throw new \Exception("Invalidation strategy not found: {$strategy}");
        }
        
        return $this->strategies[$strategy]->invalidate($pattern);
    }
    
    private function loadStrategies()
    {
        $this->strategies['time_based'] = new TimeBasedInvalidation();
        $this->strategies['event_based'] = new EventBasedInvalidation();
        $this->strategies['version_based'] = new VersionBasedInvalidation();
    }
}

class TimeBasedInvalidation
{
    public function invalidate($pattern)
    {
        // Set TTL for matching keys
        $cache = new MultiLevelCache();
        $keys = $this->getMatchingKeys($pattern);
        
        foreach ($keys as $key) {
            $cache->set($key, null, 1); // Set to expire in 1 second
        }
        
        return count($keys);
    }
    
    private function getMatchingKeys($pattern)
    {
        // This would typically query Redis or other cache backend
        return ['key1', 'key2', 'key3']; // Mock implementation
    }
}

class EventBasedInvalidation
{
    private $eventBus;
    
    public function __construct()
    {
        $this->eventBus = new EventBus();
    }
    
    public function invalidate($event)
    {
        // Publish invalidation event
        $this->eventBus->publish('cache:invalidate', [
            'event' => $event,
            'timestamp' => date('c')
        ]);
    }
}

class VersionBasedInvalidation
{
    private $versionStore;
    
    public function __construct()
    {
        $this->versionStore = new VersionStore();
    }
    
    public function invalidate($pattern)
    {
        // Increment version for pattern
        $version = $this->versionStore->incrementVersion($pattern);
        
        // Invalidate all keys with older versions
        return $this->invalidateByVersion($pattern, $version);
    }
    
    private function invalidateByVersion($pattern, $currentVersion)
    {
        $cache = new MultiLevelCache();
        $keys = $this->getKeysWithVersion($pattern, $currentVersion);
        
        foreach ($keys as $key) {
            $cache->delete($key);
        }
        
        return count($keys);
    }
}
```

## Cache Warming
```php
<?php

namespace App\Caching\Warming;

use TuskLang\Config;

class CacheWarmer
{
    private $config;
    private $cache;
    
    public function __construct(MultiLevelCache $cache)
    {
        $this->config = new Config();
        $this->cache = $cache;
    }
    
    public function warmCache($patterns = [])
    {
        if (empty($patterns)) {
            $patterns = $this->config->get('caching.warm_patterns', []);
        }
        
        foreach ($patterns as $pattern) {
            $this->warmPattern($pattern);
        }
    }
    
    private function warmPattern($pattern)
    {
        $data = $this->loadDataForPattern($pattern);
        
        foreach ($data as $key => $value) {
            $this->cache->set($key, $value);
        }
    }
    
    private function loadDataForPattern($pattern)
    {
        switch ($pattern['type']) {
            case 'database':
                return $this->loadFromDatabase($pattern);
                
            case 'api':
                return $this->loadFromApi($pattern);
                
            case 'file':
                return $this->loadFromFile($pattern);
                
            default:
                throw new \Exception("Unknown pattern type: {$pattern['type']}");
        }
    }
    
    private function loadFromDatabase($pattern)
    {
        $pdo = new PDO(Env::get('DB_DSN'));
        $stmt = $pdo->prepare($pattern['query']);
        $stmt->execute($pattern['params'] ?? []);
        
        $data = [];
        while ($row = $stmt->fetch()) {
            $key = $this->generateKey($pattern['key_template'], $row);
            $data[$key] = $row;
        }
        
        return $data;
    }
    
    private function loadFromApi($pattern)
    {
        $client = new HttpClient();
        $response = $client->get($pattern['url']);
        
        $data = json_decode($response->getBody(), true);
        
        $result = [];
        foreach ($data as $item) {
            $key = $this->generateKey($pattern['key_template'], $item);
            $result[$key] = $item;
        }
        
        return $result;
    }
    
    private function loadFromFile($pattern)
    {
        $content = file_get_contents($pattern['file']);
        $data = json_decode($content, true);
        
        $result = [];
        foreach ($data as $item) {
            $key = $this->generateKey($pattern['key_template'], $item);
            $result[$key] = $item;
        }
        
        return $result;
    }
    
    private function generateKey($template, $data)
    {
        return str_replace(
            array_map(function($key) { return "{{$key}}"; }, array_keys($data)),
            array_values($data),
            $template
        );
    }
}
```

## Cache Compression
```php
<?php

namespace App\Caching\Compression;

use TuskLang\Config;

class CacheCompressor
{
    private $config;
    private $algorithms = [];
    
    public function __construct()
    {
        $this->config = new Config();
        $this->loadAlgorithms();
    }
    
    public function compress($data, $algorithm = 'gzip')
    {
        if (!isset($this->algorithms[$algorithm])) {
            throw new \Exception("Compression algorithm not found: {$algorithm}");
        }
        
        return $this->algorithms[$algorithm]->compress($data);
    }
    
    public function decompress($data, $algorithm = 'gzip')
    {
        if (!isset($this->algorithms[$algorithm])) {
            throw new \Exception("Compression algorithm not found: {$algorithm}");
        }
        
        return $this->algorithms[$algorithm]->decompress($data);
    }
    
    private function loadAlgorithms()
    {
        $this->algorithms['gzip'] = new GzipCompressor();
        $this->algorithms['lz4'] = new LZ4Compressor();
        $this->algorithms['zstd'] = new ZstdCompressor();
    }
}

class GzipCompressor
{
    public function compress($data)
    {
        return gzencode($data, 6);
    }
    
    public function decompress($data)
    {
        return gzdecode($data);
    }
}

class LZ4Compressor
{
    public function compress($data)
    {
        // Requires LZ4 extension
        if (!extension_loaded('lz4')) {
            throw new \Exception("LZ4 extension not loaded");
        }
        
        return lz4_compress($data);
    }
    
    public function decompress($data)
    {
        return lz4_uncompress($data);
    }
}

class ZstdCompressor
{
    public function compress($data)
    {
        // Requires Zstd extension
        if (!extension_loaded('zstd')) {
            throw new \Exception("Zstd extension not loaded");
        }
        
        return zstd_compress($data);
    }
    
    public function decompress($data)
    {
        return zstd_uncompress($data);
    }
}
```

## Cache-Aside Pattern
```php
<?php

namespace App\Caching\Patterns;

use TuskLang\Config;

class CacheAside
{
    private $config;
    private $cache;
    
    public function __construct(MultiLevelCache $cache)
    {
        $this->config = new Config();
        $this->cache = $cache;
    }
    
    public function getOrSet($key, callable $loader, $ttl = null)
    {
        // Try cache first
        $value = $this->cache->get($key);
        if ($value !== null) {
            return $value;
        }
        
        // Load from source
        $value = $loader();
        
        // Store in cache
        $this->cache->set($key, $value, $ttl);
        
        return $value;
    }
    
    public function getOrSetAsync($key, callable $loader, $ttl = null)
    {
        // Try cache first
        $value = $this->cache->get($key);
        if ($value !== null) {
            return $value;
        }
        
        // Return promise for async loading
        return new Promise(function ($resolve, $reject) use ($key, $loader, $ttl) {
            try {
                $value = $loader();
                $this->cache->set($key, $value, $ttl);
                $resolve($value);
            } catch (\Exception $e) {
                $reject($e);
            }
        });
    }
}

class WriteThrough
{
    private $cache;
    private $source;
    
    public function __construct(MultiLevelCache $cache, $source)
    {
        $this->cache = $cache;
        $this->source = $source;
    }
    
    public function set($key, $value, $ttl = null)
    {
        // Write to source first
        $this->source->set($key, $value);
        
        // Then update cache
        $this->cache->set($key, $value, $ttl);
    }
}

class WriteBehind
{
    private $cache;
    private $queue;
    private $source;
    
    public function __construct(MultiLevelCache $cache, $source)
    {
        $this->cache = $cache;
        $this->source = $source;
        $this->queue = new MessageQueue();
    }
    
    public function set($key, $value, $ttl = null)
    {
        // Update cache immediately
        $this->cache->set($key, $value, $ttl);
        
        // Queue for background write to source
        $this->queue->publish('cache:write_behind', [
            'key' => $key,
            'value' => $value,
            'timestamp' => date('c')
        ]);
    }
    
    public function processWriteBehind()
    {
        $messages = $this->queue->consume('cache:write_behind');
        
        foreach ($messages as $message) {
            $this->source->set($message['key'], $message['value']);
        }
    }
}
```

## Best Practices
- **Use appropriate cache levels for different data types**
- **Implement intelligent invalidation strategies**
- **Monitor cache hit rates and adjust TTLs**
- **Use compression for large objects**
- **Implement cache warming for critical data**
- **Use different TTLs for different data types**

## Performance Optimization
- **Use connection pooling for Redis**
- **Implement batch operations for cache updates**
- **Use efficient serialization**
- **Monitor memory usage and implement LRU eviction**

## Security Considerations
- **Validate cache keys to prevent injection**
- **Use secure connections for distributed caches**
- **Implement cache poisoning protection**
- **Encrypt sensitive cached data**

## Troubleshooting
- **Monitor cache hit rates and adjust strategies**
- **Check for memory leaks in local caches**
- **Verify Redis connectivity and performance**
- **Monitor cache invalidation patterns**

## Conclusion
TuskLang + PHP = caching that's intelligent, distributed, and blazing fast. Cache everything, cache smart. 