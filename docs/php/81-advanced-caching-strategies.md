# Advanced Caching Strategies

TuskLang enables PHP developers to implement sophisticated caching strategies with confidence. This guide covers advanced caching patterns, cache optimization, and performance strategies.

## Table of Contents
- [Cache Configuration](#cache-configuration)
- [Multi-Level Caching](#multi-level-caching)
- [Cache Patterns](#cache-patterns)
- [Cache Invalidation](#cache-invalidation)
- [Cache Optimization](#cache-optimization)
- [Best Practices](#best-practices)

## Cache Configuration

```php
// config/caching.tsk
caching = {
    strategy = "multi_level"
    
    levels = {
        l1 = {
            type = "memory"
            provider = "apcu"
            ttl = 300
            max_size = "256MB"
            compression = true
            serialization = "igbinary"
        }
        
        l2 = {
            type = "redis"
            provider = "redis"
            ttl = 3600
            max_size = "2GB"
            compression = true
            persistence = true
            replication = true
            cluster = {
                enabled = false
                nodes = ["redis1:6379", "redis2:6379", "redis3:6379"]
            }
        }
        
        l3 = {
            type = "database"
            provider = "mysql"
            ttl = 86400
            max_size = "10GB"
            compression = true
            table = "cache_store"
        }
    }
    
    patterns = {
        cache_aside = {
            enabled = true
            write_through = false
            write_behind = false
        }
        
        write_through = {
            enabled = false
            consistency = "strong"
            batch_size = 100
        }
        
        write_behind = {
            enabled = false
            batch_size = 100
            flush_interval = 60
            error_handling = "retry"
        }
        
        read_through = {
            enabled = true
            fallback_strategy = "graceful"
        }
    }
    
    invalidation = {
        ttl_based = true
        event_driven = true
        pattern_based = true
        version_based = true
        
        events = [
            "user.updated",
            "order.created",
            "product.modified",
            "cache.cleared"
        ]
        
        patterns = [
            "user:*",
            "order:*",
            "product:*",
            "category:*"
        ]
    }
    
    optimization = {
        cache_warming = true
        cache_preloading = true
        cache_compression = true
        cache_serialization = "igbinary"
        
        warming = {
            strategies = ["predictive", "scheduled", "on_demand"]
            batch_size = 1000
            concurrency = 10
        }
        
        compression = {
            algorithm = "lz4"
            threshold = 1024
            level = 6
        }
    }
    
    monitoring = {
        enabled = true
        metrics = [
            "hit_ratio",
            "miss_ratio",
            "eviction_rate",
            "memory_usage",
            "response_time"
        ]
        
        alerts = {
            hit_ratio_threshold = 0.8
            memory_usage_threshold = 0.9
            response_time_threshold = 100
        }
    }
}
```

## Multi-Level Caching

```php
<?php
// app/Infrastructure/Cache/MultiLevelCache.php

namespace App\Infrastructure\Cache;

use TuskLang\Cache\CacheInterface;
use TuskLang\Cache\CacheLevel;

class MultiLevelCache implements CacheInterface
{
    private array $levels = [];
    private array $config;
    private array $statistics = [];
    
    public function __construct(array $config)
    {
        $this->config = $config;
        $this->initializeLevels();
    }
    
    public function get(string $key, callable $callback = null): mixed
    {
        $startTime = microtime(true);
        
        // Try each level from fastest to slowest
        foreach ($this->levels as $level => $cache) {
            $value = $cache->get($key);
            
            if ($value !== null) {
                $this->recordHit($level);
                $this->recordResponseTime($level, microtime(true) - $startTime);
                
                // Populate faster levels
                $this->populateFasterLevels($key, $value, $level);
                
                return $value;
            }
            
            $this->recordMiss($level);
        }
        
        // If callback provided, execute and cache result
        if ($callback !== null) {
            $value = $callback();
            $this->set($key, $value);
            return $value;
        }
        
        $this->recordResponseTime('total', microtime(true) - $startTime);
        return null;
    }
    
    public function set(string $key, mixed $value, ?int $ttl = null): bool
    {
        $success = true;
        
        // Set in all levels
        foreach ($this->levels as $level => $cache) {
            $levelTtl = $this->getLevelTtl($level, $ttl);
            $success = $cache->set($key, $value, $levelTtl) && $success;
        }
        
        return $success;
    }
    
    public function delete(string $key): bool
    {
        $success = true;
        
        // Delete from all levels
        foreach ($this->levels as $cache) {
            $success = $cache->delete($key) && $success;
        }
        
        return $success;
    }
    
    public function clear(): bool
    {
        $success = true;
        
        // Clear all levels
        foreach ($this->levels as $cache) {
            $success = $cache->clear() && $success;
        }
        
        return $success;
    }
    
    public function has(string $key): bool
    {
        foreach ($this->levels as $cache) {
            if ($cache->has($key)) {
                return true;
            }
        }
        
        return false;
    }
    
    public function getMultiple(array $keys): array
    {
        $results = [];
        $missingKeys = [];
        
        // Try to get from fastest level first
        foreach ($this->levels as $level => $cache) {
            if (empty($missingKeys)) {
                $missingKeys = $keys;
            }
            
            $levelResults = $cache->getMultiple($missingKeys);
            
            foreach ($levelResults as $key => $value) {
                if ($value !== null) {
                    $results[$key] = $value;
                    unset($missingKeys[array_search($key, $missingKeys)]);
                    $this->recordHit($level);
                } else {
                    $this->recordMiss($level);
                }
            }
            
            if (empty($missingKeys)) {
                break;
            }
        }
        
        return $results;
    }
    
    public function setMultiple(array $values, ?int $ttl = null): bool
    {
        $success = true;
        
        foreach ($this->levels as $level => $cache) {
            $levelTtl = $this->getLevelTtl($level, $ttl);
            $success = $cache->setMultiple($values, $levelTtl) && $success;
        }
        
        return $success;
    }
    
    public function deleteMultiple(array $keys): bool
    {
        $success = true;
        
        foreach ($this->levels as $cache) {
            $success = $cache->deleteMultiple($keys) && $success;
        }
        
        return $success;
    }
    
    public function getStatistics(): array
    {
        $stats = [];
        
        foreach ($this->levels as $level => $cache) {
            $stats[$level] = [
                'hits' => $this->statistics[$level]['hits'] ?? 0,
                'misses' => $this->statistics[$level]['misses'] ?? 0,
                'hit_ratio' => $this->calculateHitRatio($level),
                'response_time' => $this->statistics[$level]['response_time'] ?? 0
            ];
        }
        
        $stats['total'] = [
            'hits' => array_sum(array_column($stats, 'hits')),
            'misses' => array_sum(array_column($stats, 'misses')),
            'hit_ratio' => $this->calculateTotalHitRatio(),
            'response_time' => $this->statistics['total']['response_time'] ?? 0
        ];
        
        return $stats;
    }
    
    private function initializeLevels(): void
    {
        foreach ($this->config['levels'] as $level => $config) {
            $this->levels[$level] = $this->createCacheLevel($config);
        }
    }
    
    private function createCacheLevel(array $config): CacheInterface
    {
        return match($config['type']) {
            'memory' => new ApcuCache($config),
            'redis' => new RedisCache($config),
            'database' => new DatabaseCache($config),
            default => throw new \RuntimeException("Unknown cache type: {$config['type']}")
        };
    }
    
    private function populateFasterLevels(string $key, mixed $value, string $foundLevel): void
    {
        $levelOrder = array_keys($this->levels);
        $foundIndex = array_search($foundLevel, $levelOrder);
        
        // Populate faster levels
        for ($i = 0; $i < $foundIndex; $i++) {
            $level = $levelOrder[$i];
            $ttl = $this->getLevelTtl($level);
            $this->levels[$level]->set($key, $value, $ttl);
        }
    }
    
    private function getLevelTtl(string $level, ?int $customTtl = null): int
    {
        if ($customTtl !== null) {
            return $customTtl;
        }
        
        return $this->config['levels'][$level]['ttl'] ?? 3600;
    }
    
    private function recordHit(string $level): void
    {
        $this->statistics[$level]['hits'] = ($this->statistics[$level]['hits'] ?? 0) + 1;
    }
    
    private function recordMiss(string $level): void
    {
        $this->statistics[$level]['misses'] = ($this->statistics[$level]['misses'] ?? 0) + 1;
    }
    
    private function recordResponseTime(string $level, float $time): void
    {
        $this->statistics[$level]['response_time'] = $time;
    }
    
    private function calculateHitRatio(string $level): float
    {
        $hits = $this->statistics[$level]['hits'] ?? 0;
        $misses = $this->statistics[$level]['misses'] ?? 0;
        $total = $hits + $misses;
        
        return $total > 0 ? $hits / $total : 0.0;
    }
    
    private function calculateTotalHitRatio(): float
    {
        $totalHits = 0;
        $totalMisses = 0;
        
        foreach ($this->statistics as $level => $stats) {
            if ($level !== 'total') {
                $totalHits += $stats['hits'] ?? 0;
                $totalMisses += $stats['misses'] ?? 0;
            }
        }
        
        $total = $totalHits + $totalMisses;
        return $total > 0 ? $totalHits / $total : 0.0;
    }
}

// app/Infrastructure/Cache/RedisCache.php

namespace App\Infrastructure\Cache;

use TuskLang\Cache\CacheInterface;
use Redis;

class RedisCache implements CacheInterface
{
    private Redis $redis;
    private array $config;
    
    public function __construct(array $config)
    {
        $this->config = $config;
        $this->initializeRedis();
    }
    
    public function get(string $key, callable $callback = null): mixed
    {
        $value = $this->redis->get($this->normalizeKey($key));
        
        if ($value !== false) {
            return $this->deserialize($value);
        }
        
        if ($callback !== null) {
            $value = $callback();
            $this->set($key, $value);
            return $value;
        }
        
        return null;
    }
    
    public function set(string $key, mixed $value, ?int $ttl = null): bool
    {
        $serialized = $this->serialize($value);
        $normalizedKey = $this->normalizeKey($key);
        $ttl = $ttl ?? $this->config['ttl'];
        
        if ($ttl > 0) {
            return $this->redis->setex($normalizedKey, $ttl, $serialized);
        }
        
        return $this->redis->set($normalizedKey, $serialized);
    }
    
    public function delete(string $key): bool
    {
        return $this->redis->del($this->normalizeKey($key)) > 0;
    }
    
    public function clear(): bool
    {
        return $this->redis->flushDB();
    }
    
    public function has(string $key): bool
    {
        return $this->redis->exists($this->normalizeKey($key));
    }
    
    public function getMultiple(array $keys): array
    {
        $normalizedKeys = array_map([$this, 'normalizeKey'], $keys);
        $values = $this->redis->mget($normalizedKeys);
        
        $results = [];
        foreach ($keys as $index => $key) {
            $value = $values[$index] ?? null;
            $results[$key] = $value !== false ? $this->deserialize($value) : null;
        }
        
        return $results;
    }
    
    public function setMultiple(array $values, ?int $ttl = null): bool
    {
        $pipeline = $this->redis->multi();
        $ttl = $ttl ?? $this->config['ttl'];
        
        foreach ($values as $key => $value) {
            $serialized = $this->serialize($value);
            $normalizedKey = $this->normalizeKey($key);
            
            if ($ttl > 0) {
                $pipeline->setex($normalizedKey, $ttl, $serialized);
            } else {
                $pipeline->set($normalizedKey, $serialized);
            }
        }
        
        $results = $pipeline->exec();
        return !in_array(false, $results, true);
    }
    
    public function deleteMultiple(array $keys): bool
    {
        $normalizedKeys = array_map([$this, 'normalizeKey'], $keys);
        return $this->redis->del($normalizedKeys) > 0;
    }
    
    private function initializeRedis(): void
    {
        $this->redis = new Redis();
        
        if (isset($this->config['cluster']['enabled']) && $this->config['cluster']['enabled']) {
            $this->redis->connect($this->config['cluster']['nodes'][0]);
        } else {
            $this->redis->connect('127.0.0.1', 6379);
        }
        
        if (isset($this->config['compression']) && $this->config['compression']) {
            $this->redis->setOption(Redis::OPT_SERIALIZER, Redis::SERIALIZER_IGBINARY);
        }
    }
    
    private function normalizeKey(string $key): string
    {
        return 'cache:' . md5($key);
    }
    
    private function serialize(mixed $value): string
    {
        if ($this->config['serialization'] === 'igbinary' && function_exists('igbinary_serialize')) {
            return igbinary_serialize($value);
        }
        
        return serialize($value);
    }
    
    private function deserialize(string $value): mixed
    {
        if ($this->config['serialization'] === 'igbinary' && function_exists('igbinary_unserialize')) {
            return igbinary_unserialize($value);
        }
        
        return unserialize($value);
    }
}
```

## Cache Patterns

```php
<?php
// app/Infrastructure/Cache/Patterns/CacheAsidePattern.php

namespace App\Infrastructure\Cache\Patterns;

use TuskLang\Cache\CacheInterface;

class CacheAsidePattern
{
    private CacheInterface $cache;
    private array $config;
    
    public function __construct(CacheInterface $cache, array $config)
    {
        $this->cache = $cache;
        $this->config = $config;
    }
    
    public function get(string $key, callable $dataLoader): mixed
    {
        // Try to get from cache first
        $value = $this->cache->get($key);
        
        if ($value !== null) {
            return $value;
        }
        
        // Load from data source
        $value = $dataLoader();
        
        // Store in cache
        if ($value !== null) {
            $this->cache->set($key, $value);
        }
        
        return $value;
    }
    
    public function set(string $key, mixed $value): void
    {
        $this->cache->set($key, $value);
    }
    
    public function invalidate(string $key): void
    {
        $this->cache->delete($key);
    }
    
    public function invalidatePattern(string $pattern): void
    {
        // Implementation depends on cache provider
        // For Redis, you could use SCAN + DEL
        // For other providers, you might need to track keys
    }
}

// app/Infrastructure/Cache/Patterns/WriteThroughPattern.php

namespace App\Infrastructure\Cache\Patterns;

use TuskLang\Cache\CacheInterface;

class WriteThroughPattern
{
    private CacheInterface $cache;
    private array $config;
    
    public function __construct(CacheInterface $cache, array $config)
    {
        $this->cache = $cache;
        $this->config = $config;
    }
    
    public function write(string $key, mixed $value, callable $dataWriter): bool
    {
        // Write to data source first
        $success = $dataWriter($key, $value);
        
        if ($success) {
            // Then update cache
            $this->cache->set($key, $value);
        }
        
        return $success;
    }
    
    public function writeMultiple(array $data, callable $dataWriter): bool
    {
        // Write to data source first
        $success = $dataWriter($data);
        
        if ($success) {
            // Then update cache
            $this->cache->setMultiple($data);
        }
        
        return $success;
    }
    
    public function delete(string $key, callable $dataDeleter): bool
    {
        // Delete from data source first
        $success = $dataDeleter($key);
        
        if ($success) {
            // Then remove from cache
            $this->cache->delete($key);
        }
        
        return $success;
    }
}

// app/Infrastructure/Cache/Patterns/WriteBehindPattern.php

namespace App\Infrastructure\Cache\Patterns;

use TuskLang\Cache\CacheInterface;

class WriteBehindPattern
{
    private CacheInterface $cache;
    private array $config;
    private array $writeQueue = [];
    private bool $isProcessing = false;
    
    public function __construct(CacheInterface $cache, array $config)
    {
        $this->cache = $cache;
        $this->config = $config;
        
        // Start background processing
        $this->startBackgroundProcessor();
    }
    
    public function write(string $key, mixed $value): bool
    {
        // Update cache immediately
        $this->cache->set($key, $value);
        
        // Queue for background write to data source
        $this->writeQueue[] = [
            'type' => 'write',
            'key' => $key,
            'value' => $value,
            'timestamp' => time()
        ];
        
        return true;
    }
    
    public function delete(string $key): bool
    {
        // Remove from cache immediately
        $this->cache->delete($key);
        
        // Queue for background delete from data source
        $this->writeQueue[] = [
            'type' => 'delete',
            'key' => $key,
            'timestamp' => time()
        ];
        
        return true;
    }
    
    public function flush(): bool
    {
        return $this->processWriteQueue();
    }
    
    private function startBackgroundProcessor(): void
    {
        if ($this->config['write_behind']['enabled']) {
            register_shutdown_function([$this, 'flush']);
            
            // Start background thread/process for periodic flushing
            $this->schedulePeriodicFlush();
        }
    }
    
    private function schedulePeriodicFlush(): void
    {
        $interval = $this->config['write_behind']['flush_interval'];
        
        // In a real implementation, you'd use a proper background job system
        // For now, we'll use a simple approach
        if (function_exists('pcntl_fork')) {
            $pid = pcntl_fork();
            if ($pid === 0) {
                // Child process
                while (true) {
                    sleep($interval);
                    $this->processWriteQueue();
                }
            }
        }
    }
    
    private function processWriteQueue(): bool
    {
        if ($this->isProcessing || empty($this->writeQueue)) {
            return true;
        }
        
        $this->isProcessing = true;
        
        try {
            $batchSize = $this->config['write_behind']['batch_size'];
            $batch = array_splice($this->writeQueue, 0, $batchSize);
            
            foreach ($batch as $operation) {
                $this->processOperation($operation);
            }
            
            return true;
        } catch (\Exception $e) {
            // Handle error based on configuration
            if ($this->config['write_behind']['error_handling'] === 'retry') {
                // Re-queue failed operations
                $this->writeQueue = array_merge($batch, $this->writeQueue);
            }
            
            return false;
        } finally {
            $this->isProcessing = false;
        }
    }
    
    private function processOperation(array $operation): void
    {
        // Implementation depends on your data source
        // This is a placeholder for the actual data source operations
        switch ($operation['type']) {
            case 'write':
                // Write to database
                break;
            case 'delete':
                // Delete from database
                break;
        }
    }
}
```

## Cache Invalidation

```php
<?php
// app/Infrastructure/Cache/Invalidation/CacheInvalidator.php

namespace App\Infrastructure\Cache\Invalidation;

use TuskLang\Cache\CacheInterface;
use TuskLang\Events\EventDispatcher;

class CacheInvalidator
{
    private CacheInterface $cache;
    private EventDispatcher $eventDispatcher;
    private array $config;
    private array $invalidationRules = [];
    
    public function __construct(CacheInterface $cache, EventDispatcher $eventDispatcher, array $config)
    {
        $this->cache = $cache;
        $this->eventDispatcher = $eventDispatcher;
        $this->config = $config;
        $this->loadInvalidationRules();
        $this->subscribeToEvents();
    }
    
    public function invalidateByKey(string $key): bool
    {
        return $this->cache->delete($key);
    }
    
    public function invalidateByPattern(string $pattern): bool
    {
        if ($this->cache instanceof RedisCache) {
            return $this->invalidateRedisPattern($pattern);
        }
        
        // For other cache types, you might need to track keys
        return $this->invalidateTrackedKeys($pattern);
    }
    
    public function invalidateByEvent(string $event, array $data = []): bool
    {
        $rules = $this->getInvalidationRules($event);
        $success = true;
        
        foreach ($rules as $rule) {
            $keys = $this->generateKeysFromRule($rule, $data);
            
            foreach ($keys as $key) {
                $success = $this->cache->delete($key) && $success;
            }
        }
        
        return $success;
    }
    
    public function invalidateByVersion(string $version): bool
    {
        $pattern = "version:{$version}:*";
        return $this->invalidateByPattern($pattern);
    }
    
    public function invalidateByTags(array $tags): bool
    {
        $success = true;
        
        foreach ($tags as $tag) {
            $pattern = "tag:{$tag}:*";
            $success = $this->invalidateByPattern($pattern) && $success;
        }
        
        return $success;
    }
    
    public function addInvalidationRule(string $event, array $rule): void
    {
        $this->invalidationRules[$event][] = $rule;
    }
    
    private function loadInvalidationRules(): void
    {
        $this->invalidationRules = [
            'user.updated' => [
                ['type' => 'pattern', 'pattern' => 'user:*'],
                ['type' => 'key', 'key' => 'user:{id}']
            ],
            'order.created' => [
                ['type' => 'pattern', 'pattern' => 'orders:*'],
                ['type' => 'pattern', 'pattern' => 'user:{user_id}:orders:*']
            ],
            'product.modified' => [
                ['type' => 'pattern', 'pattern' => 'product:*'],
                ['type' => 'pattern', 'pattern' => 'category:*'],
                ['type' => 'key', 'key' => 'product:{id}']
            ]
        ];
    }
    
    private function subscribeToEvents(): void
    {
        foreach ($this->config['invalidation']['events'] as $event) {
            $this->eventDispatcher->subscribe($event, function($eventData) use ($event) {
                $this->invalidateByEvent($event, $eventData);
            });
        }
    }
    
    private function getInvalidationRules(string $event): array
    {
        return $this->invalidationRules[$event] ?? [];
    }
    
    private function generateKeysFromRule(array $rule, array $data): array
    {
        switch ($rule['type']) {
            case 'key':
                return [$this->interpolateKey($rule['key'], $data)];
            case 'pattern':
                return [$this->interpolatePattern($rule['pattern'], $data)];
            case 'tags':
                return $this->generateKeysFromTags($rule['tags'], $data);
            default:
                return [];
        }
    }
    
    private function interpolateKey(string $key, array $data): string
    {
        foreach ($data as $placeholder => $value) {
            $key = str_replace("{{$placeholder}}", $value, $key);
        }
        
        return $key;
    }
    
    private function interpolatePattern(string $pattern, array $data): string
    {
        foreach ($data as $placeholder => $value) {
            $pattern = str_replace("{{$placeholder}}", $value, $pattern);
        }
        
        return $pattern;
    }
    
    private function generateKeysFromTags(array $tags, array $data): array
    {
        $keys = [];
        
        foreach ($tags as $tag) {
            $tag = $this->interpolateKey($tag, $data);
            $keys[] = "tag:{$tag}:*";
        }
        
        return $keys;
    }
    
    private function invalidateRedisPattern(string $pattern): bool
    {
        // Implementation for Redis pattern invalidation
        // This would use SCAN + DEL commands
        return true;
    }
    
    private function invalidateTrackedKeys(string $pattern): bool
    {
        // Implementation for tracking keys and pattern matching
        // This would require maintaining a key registry
        return true;
    }
}

// app/Infrastructure/Cache/Invalidation/VersionedCache.php

namespace App\Infrastructure\Cache\Invalidation;

use TuskLang\Cache\CacheInterface;

class VersionedCache
{
    private CacheInterface $cache;
    private array $versions = [];
    
    public function __construct(CacheInterface $cache)
    {
        $this->cache = $cache;
    }
    
    public function get(string $key, string $version = null): mixed
    {
        $version = $version ?? $this->getCurrentVersion($key);
        $versionedKey = $this->createVersionedKey($key, $version);
        
        return $this->cache->get($versionedKey);
    }
    
    public function set(string $key, mixed $value, string $version = null): bool
    {
        $version = $version ?? $this->generateVersion();
        $versionedKey = $this->createVersionedKey($key, $version);
        
        $success = $this->cache->set($versionedKey, $value);
        
        if ($success) {
            $this->setCurrentVersion($key, $version);
        }
        
        return $success;
    }
    
    public function invalidateVersion(string $version): bool
    {
        $pattern = "version:{$version}:*";
        return $this->cache->deletePattern($pattern);
    }
    
    public function getCurrentVersion(string $key): string
    {
        return $this->versions[$key] ?? 'v1';
    }
    
    public function setCurrentVersion(string $key, string $version): void
    {
        $this->versions[$key] = $version;
    }
    
    private function createVersionedKey(string $key, string $version): string
    {
        return "version:{$version}:{$key}";
    }
    
    private function generateVersion(): string
    {
        return 'v' . time();
    }
}
```

## Cache Optimization

```php
<?php
// app/Infrastructure/Cache/Optimization/CacheOptimizer.php

namespace App\Infrastructure\Cache\Optimization;

use TuskLang\Cache\CacheInterface;
use TuskLang\Cache\CacheStatistics;

class CacheOptimizer
{
    private CacheInterface $cache;
    private array $config;
    private CacheStatistics $statistics;
    
    public function __construct(CacheInterface $cache, array $config)
    {
        $this->cache = $cache;
        $this->config = $config;
        $this->statistics = new CacheStatistics();
    }
    
    public function optimize(): array
    {
        $optimizations = [];
        
        // Analyze cache performance
        $stats = $this->cache->getStatistics();
        
        // Optimize based on hit ratio
        if ($stats['hit_ratio'] < 0.8) {
            $optimizations[] = $this->optimizeHitRatio($stats);
        }
        
        // Optimize memory usage
        if ($stats['memory_usage'] > 0.9) {
            $optimizations[] = $this->optimizeMemoryUsage($stats);
        }
        
        // Optimize response time
        if ($stats['response_time'] > 100) {
            $optimizations[] = $this->optimizeResponseTime($stats);
        }
        
        return $optimizations;
    }
    
    public function warmCache(array $keys, callable $dataLoader): void
    {
        $batchSize = $this->config['optimization']['warming']['batch_size'];
        $concurrency = $this->config['optimization']['warming']['concurrency'];
        
        $batches = array_chunk($keys, $batchSize);
        
        foreach ($batches as $batch) {
            $this->warmBatch($batch, $dataLoader, $concurrency);
        }
    }
    
    public function preloadCache(array $preloadRules): void
    {
        foreach ($preloadRules as $rule) {
            $this->preloadByRule($rule);
        }
    }
    
    public function compressCache(): bool
    {
        if (!$this->config['optimization']['cache_compression']) {
            return false;
        }
        
        // Implementation for cache compression
        // This would compress cached data to save memory
        return true;
    }
    
    private function optimizeHitRatio(array $stats): array
    {
        $suggestions = [];
        
        if ($stats['hit_ratio'] < 0.5) {
            $suggestions[] = 'Consider increasing cache size';
            $suggestions[] = 'Review cache eviction policy';
            $suggestions[] = 'Implement cache warming for frequently accessed data';
        }
        
        if ($stats['miss_ratio'] > 0.3) {
            $suggestions[] = 'Add more cache keys for granular caching';
            $suggestions[] = 'Implement predictive caching';
        }
        
        return [
            'type' => 'hit_ratio_optimization',
            'current_ratio' => $stats['hit_ratio'],
            'suggestions' => $suggestions
        ];
    }
    
    private function optimizeMemoryUsage(array $stats): array
    {
        $suggestions = [];
        
        if ($stats['memory_usage'] > 0.95) {
            $suggestions[] = 'Increase cache memory limit';
            $suggestions[] = 'Implement more aggressive eviction';
            $suggestions[] = 'Enable cache compression';
        }
        
        if ($stats['eviction_rate'] > 0.1) {
            $suggestions[] = 'Review cache size and eviction policy';
            $suggestions[] = 'Implement cache partitioning';
        }
        
        return [
            'type' => 'memory_optimization',
            'current_usage' => $stats['memory_usage'],
            'suggestions' => $suggestions
        ];
    }
    
    private function optimizeResponseTime(array $stats): array
    {
        $suggestions = [];
        
        if ($stats['response_time'] > 200) {
            $suggestions[] = 'Consider using faster cache backend';
            $suggestions[] = 'Implement cache clustering';
            $suggestions[] = 'Optimize serialization format';
        }
        
        return [
            'type' => 'response_time_optimization',
            'current_time' => $stats['response_time'],
            'suggestions' => $suggestions
        ];
    }
    
    private function warmBatch(array $keys, callable $dataLoader, int $concurrency): void
    {
        // Implementation for concurrent cache warming
        // This would use multiple threads/processes to load data
        foreach ($keys as $key) {
            $this->cache->get($key, function() use ($dataLoader, $key) {
                return $dataLoader($key);
            });
        }
    }
    
    private function preloadByRule(array $rule): void
    {
        // Implementation for cache preloading based on rules
        // This would predict and load data before it's needed
    }
}

// app/Infrastructure/Cache/Optimization/CacheStatistics.php

namespace App\Infrastructure\Cache\Optimization;

class CacheStatistics
{
    private array $metrics = [];
    
    public function recordHit(string $key): void
    {
        $this->incrementMetric('hits');
        $this->recordKeyAccess($key);
    }
    
    public function recordMiss(string $key): void
    {
        $this->incrementMetric('misses');
        $this->recordKeyAccess($key);
    }
    
    public function recordResponseTime(float $time): void
    {
        $this->updateMetric('response_time', $time);
    }
    
    public function recordMemoryUsage(float $usage): void
    {
        $this->updateMetric('memory_usage', $usage);
    }
    
    public function getStatistics(): array
    {
        $hits = $this->metrics['hits'] ?? 0;
        $misses = $this->metrics['misses'] ?? 0;
        $total = $hits + $misses;
        
        return [
            'hits' => $hits,
            'misses' => $misses,
            'hit_ratio' => $total > 0 ? $hits / $total : 0.0,
            'miss_ratio' => $total > 0 ? $misses / $total : 0.0,
            'response_time' => $this->metrics['response_time'] ?? 0.0,
            'memory_usage' => $this->metrics['memory_usage'] ?? 0.0,
            'most_accessed_keys' => $this->getMostAccessedKeys()
        ];
    }
    
    private function incrementMetric(string $metric): void
    {
        $this->metrics[$metric] = ($this->metrics[$metric] ?? 0) + 1;
    }
    
    private function updateMetric(string $metric, float $value): void
    {
        $this->metrics[$metric] = $value;
    }
    
    private function recordKeyAccess(string $key): void
    {
        $this->metrics['key_accesses'][$key] = ($this->metrics['key_accesses'][$key] ?? 0) + 1;
    }
    
    private function getMostAccessedKeys(): array
    {
        $accesses = $this->metrics['key_accesses'] ?? [];
        arsort($accesses);
        
        return array_slice($accesses, 0, 10, true);
    }
}
```

## Best Practices

```php
// config/caching-best-practices.tsk
caching_best_practices = {
    strategy = {
        use_appropriate_cache_levels = true
        implement_cache_patterns = true
        optimize_cache_keys = true
        use_cache_invalidation = true
    }
    
    performance = {
        minimize_cache_overhead = true
        use_async_operations = true
        implement_compression = true
        optimize_serialization = true
    }
    
    memory_management = {
        set_appropriate_ttl = true
        implement_eviction_policies = true
        monitor_memory_usage = true
        use_cache_partitioning = true
    }
    
    consistency = {
        handle_cache_staleness = true
        implement_cache_coherence = true
        use_cache_versioning = true
        handle_cache_failures = true
    }
    
    monitoring = {
        track_cache_metrics = true
        monitor_cache_performance = true
        alert_on_cache_issues = true
        optimize_cache_usage = true
    }
    
    security = {
        secure_cache_access = true
        encrypt_sensitive_data = true
        implement_cache_isolation = true
        audit_cache_operations = true
    }
}

// Example usage in PHP
class CachingBestPractices
{
    public function implementBestPractices(): void
    {
        // 1. Configure multi-level cache
        $config = TuskLang::load('caching');
        $cache = new MultiLevelCache($config);
        
        // 2. Implement cache patterns
        $cacheAside = new CacheAsidePattern($cache, $config['patterns']);
        $writeThrough = new WriteThroughPattern($cache, $config['patterns']);
        $writeBehind = new WriteBehindPattern($cache, $config['patterns']);
        
        // 3. Set up cache invalidation
        $invalidator = new CacheInvalidator($cache, $this->eventDispatcher, $config['invalidation']);
        $versionedCache = new VersionedCache($cache);
        
        // 4. Implement cache optimization
        $optimizer = new CacheOptimizer($cache, $config['optimization']);
        $statistics = new CacheStatistics();
        
        // 5. Use cache aside pattern for data access
        $userData = $cacheAside->get('user:123', function() {
            return $this->userRepository->findById(123);
        });
        
        // 6. Use write-through pattern for data updates
        $writeThrough->write('user:123', $updatedUser, function($key, $value) {
            return $this->userRepository->update($value);
        });
        
        // 7. Implement cache warming
        $frequentlyAccessedKeys = ['user:123', 'user:456', 'product:789'];
        $optimizer->warmCache($frequentlyAccessedKeys, function($key) {
            return $this->loadDataForKey($key);
        });
        
        // 8. Monitor and optimize
        $stats = $cache->getStatistics();
        $optimizations = $optimizer->optimize();
        
        // 9. Log and monitor
        $this->logger->info('Caching implemented', [
            'hit_ratio' => $stats['total']['hit_ratio'],
            'response_time' => $stats['total']['response_time'],
            'optimizations' => count($optimizations),
            'cache_levels' => count($config['levels'])
        ]);
    }
    
    private function loadDataForKey(string $key): mixed
    {
        // Implementation to load data for a given key
        return null;
    }
}
```

This comprehensive guide covers advanced caching strategies in TuskLang with PHP integration. The caching system is designed to be performant, scalable, and maintainable while maintaining the rebellious spirit of TuskLang development. 