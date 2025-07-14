# Advanced Caching Strategies

TuskLang provides sophisticated caching capabilities that go beyond simple key-value storage. This guide covers advanced caching patterns, multi-level caching, and integration with PHP frameworks.

## Table of Contents

- [Multi-Level Caching](#multi-level-caching)
- [Cache Invalidation Strategies](#cache-invalidation-strategies)
- [Distributed Caching](#distributed-caching)
- [Cache Warming](#cache-warming)
- [Cache Compression](#cache-compression)
- [Cache Analytics](#cache-analytics)
- [Cache Security](#cache-security)
- [Performance Optimization](#performance-optimization)
- [Best Practices](#best-practices)

## Multi-Level Caching

Implement sophisticated multi-level caching strategies:

```php
// config/caching.tsk
caching_strategy = {
    levels = {
        l1 = {
            type = "memory"
            driver = "apcu"
            ttl = 300
            max_size = "256MB"
            compression = true
        }
        
        l2 = {
            type = "redis"
            driver = "redis"
            ttl = 3600
            max_size = "2GB"
            compression = true
            persistence = true
        }
        
        l3 = {
            type = "database"
            driver = "mysql"
            ttl = 86400
            max_size = "10GB"
            compression = true
            backup = true
        }
    }
    
    policies = {
        user_data = {
            levels = ["l1", "l2"]
            ttl = 1800
            invalidation = "event_driven"
            compression = true
        }
        
        product_catalog = {
            levels = ["l1", "l2", "l3"]
            ttl = 7200
            invalidation = "time_based"
            compression = true
            warming = true
        }
        
        api_responses = {
            levels = ["l1"]
            ttl = 300
            invalidation = "manual"
            compression = false
        }
    }
}
```

## Cache Invalidation Strategies

Implement intelligent cache invalidation:

```php
<?php
// app/Caching/AdvancedCacheManager.php

namespace App\Caching;

use TuskLang\Cache\CacheInterface;
use TuskLang\Events\EventDispatcher;

class AdvancedCacheManager
{
    private array $levels = [];
    private EventDispatcher $events;
    private array $config;
    
    public function __construct(array $config, EventDispatcher $events)
    {
        $this->config = $config;
        $this->events = $events;
        $this->initializeLevels();
    }
    
    public function get(string $key, string $policy = 'default'): mixed
    {
        $policyConfig = $this->config['policies'][$policy] ?? $this->config['policies']['default'];
        $levels = $policyConfig['levels'] ?? ['l1'];
        
        // Try each level in order
        foreach ($levels as $level) {
            $cache = $this->levels[$level] ?? null;
            if (!$cache) continue;
            
            $value = $cache->get($key);
            if ($value !== null) {
                // Promote to higher levels if needed
                $this->promoteToHigherLevels($key, $value, $level, $levels);
                return $value;
            }
        }
        
        return null;
    }
    
    public function set(string $key, mixed $value, string $policy = 'default'): bool
    {
        $policyConfig = $this->config['policies'][$policy] ?? $this->config['policies']['default'];
        $levels = $policyConfig['levels'] ?? ['l1'];
        $ttl = $policyConfig['ttl'] ?? 3600;
        
        $success = true;
        
        // Set in all configured levels
        foreach ($levels as $level) {
            $cache = $this->levels[$level] ?? null;
            if (!$cache) continue;
            
            $levelConfig = $this->config['levels'][$level];
            $levelTtl = $levelConfig['ttl'] ?? $ttl;
            
            if ($levelConfig['compression'] ?? false) {
                $value = $this->compress($value);
            }
            
            $success = $cache->set($key, $value, $levelTtl) && $success;
        }
        
        return $success;
    }
    
    public function invalidate(string $key, string $strategy = 'all'): bool
    {
        switch ($strategy) {
            case 'all':
                return $this->invalidateAllLevels($key);
            case 'pattern':
                return $this->invalidateByPattern($key);
            case 'tags':
                return $this->invalidateByTags($key);
            case 'event':
                return $this->invalidateByEvent($key);
            default:
                return $this->invalidateAllLevels($key);
        }
    }
    
    public function warm(string $pattern, string $policy = 'default'): array
    {
        $policyConfig = $this->config['policies'][$policy] ?? $this->config['policies']['default'];
        
        if (!($policyConfig['warming'] ?? false)) {
            return ['status' => 'warming_disabled'];
        }
        
        $keys = $this->getKeysByPattern($pattern);
        $warmed = [];
        $failed = [];
        
        foreach ($keys as $key) {
            try {
                $value = $this->generateValue($key);
                $this->set($key, $value, $policy);
                $warmed[] = $key;
            } catch (\Exception $e) {
                $failed[] = $key;
            }
        }
        
        return [
            'status' => 'completed',
            'warmed' => $warmed,
            'failed' => $failed,
            'total' => count($keys)
        ];
    }
    
    private function initializeLevels(): void
    {
        foreach ($this->config['levels'] as $name => $config) {
            $this->levels[$name] = $this->createCacheDriver($config);
        }
    }
    
    private function createCacheDriver(array $config): CacheInterface
    {
        $driver = $config['driver'] ?? 'memory';
        
        switch ($driver) {
            case 'apcu':
                return new ApcuCache($config);
            case 'redis':
                return new RedisCache($config);
            case 'mysql':
                return new DatabaseCache($config);
            default:
                return new MemoryCache($config);
        }
    }
    
    private function promoteToHigherLevels(string $key, mixed $value, string $sourceLevel, array $levels): void
    {
        $sourceIndex = array_search($sourceLevel, $levels);
        
        for ($i = 0; $i < $sourceIndex; $i++) {
            $targetLevel = $levels[$i];
            $cache = $this->levels[$targetLevel] ?? null;
            
            if ($cache) {
                $levelConfig = $this->config['levels'][$targetLevel];
                $ttl = $levelConfig['ttl'] ?? 3600;
                
                if ($levelConfig['compression'] ?? false) {
                    $value = $this->compress($value);
                }
                
                $cache->set($key, $value, $ttl);
            }
        }
    }
    
    private function invalidateAllLevels(string $key): bool
    {
        $success = true;
        
        foreach ($this->levels as $cache) {
            $success = $cache->delete($key) && $success;
        }
        
        return $success;
    }
    
    private function invalidateByPattern(string $pattern): bool
    {
        $success = true;
        
        foreach ($this->levels as $cache) {
            if (method_exists($cache, 'deletePattern')) {
                $success = $cache->deletePattern($pattern) && $success;
            }
        }
        
        return $success;
    }
    
    private function invalidateByTags(string $key): bool
    {
        $tags = $this->getTagsForKey($key);
        $success = true;
        
        foreach ($tags as $tag) {
            foreach ($this->levels as $cache) {
                if (method_exists($cache, 'deleteByTag')) {
                    $success = $cache->deleteByTag($tag) && $success;
                }
            }
        }
        
        return $success;
    }
    
    private function invalidateByEvent(string $key): bool
    {
        $this->events->dispatch('cache.invalidate', ['key' => $key]);
        return true;
    }
    
    private function compress(mixed $value): string
    {
        return gzencode(serialize($value), 9);
    }
    
    private function decompress(string $value): mixed
    {
        return unserialize(gzdecode($value));
    }
    
    private function getKeysByPattern(string $pattern): array
    {
        $keys = [];
        
        foreach ($this->levels as $cache) {
            if (method_exists($cache, 'getKeys')) {
                $keys = array_merge($keys, $cache->getKeys($pattern));
            }
        }
        
        return array_unique($keys);
    }
    
    private function generateValue(string $key): mixed
    {
        // This would typically call your data generation logic
        // For example, database queries, API calls, etc.
        return $this->dataGenerator->generate($key);
    }
    
    private function getTagsForKey(string $key): array
    {
        // Extract tags from key or metadata
        $parts = explode(':', $key);
        return array_slice($parts, 0, -1);
    }
}
```

## Cache Invalidation Strategies

Implement sophisticated invalidation patterns:

```php
<?php
// app/Caching/InvalidationStrategies.php

namespace App\Caching;

class InvalidationStrategies
{
    private CacheManager $cache;
    private EventDispatcher $events;
    
    public function __construct(CacheManager $cache, EventDispatcher $events)
    {
        $this->cache = $cache;
        $this->events = $events;
    }
    
    public function timeBasedInvalidation(string $key, int $ttl): void
    {
        $this->cache->set($key, $this->cache->get($key), $ttl);
    }
    
    public function eventDrivenInvalidation(string $key, array $events): void
    {
        foreach ($events as $event) {
            $this->events->listen($event, function () use ($key) {
                $this->cache->invalidate($key);
            });
        }
    }
    
    public function dependencyInvalidation(string $key, array $dependencies): void
    {
        foreach ($dependencies as $dependency) {
            $this->events->listen("cache.invalidate.{$dependency}", function () use ($key) {
                $this->cache->invalidate($key);
            });
        }
    }
    
    public function versionBasedInvalidation(string $key, string $version): void
    {
        $versionedKey = "{$key}:v{$version}";
        $this->cache->set($versionedKey, $this->cache->get($key));
        $this->cache->invalidate($key);
    }
    
    public function slidingInvalidation(string $key, int $window): void
    {
        $this->events->listen("cache.access.{$key}", function () use ($key, $window) {
            $this->cache->extend($key, $window);
        });
    }
    
    public function batchInvalidation(array $keys, string $strategy = 'all'): array
    {
        $results = [];
        
        foreach ($keys as $key) {
            $results[$key] = $this->cache->invalidate($key, $strategy);
        }
        
        return $results;
    }
    
    public function conditionalInvalidation(string $key, callable $condition): bool
    {
        if ($condition()) {
            return $this->cache->invalidate($key);
        }
        
        return true;
    }
    
    public function cascadeInvalidation(string $key, array $cascadeRules): bool
    {
        $invalidated = [$key];
        $success = $this->cache->invalidate($key);
        
        foreach ($cascadeRules as $rule) {
            $pattern = $rule['pattern'] ?? '';
            $condition = $rule['condition'] ?? null;
            
            if ($condition && !$condition($key)) {
                continue;
            }
            
            $relatedKeys = $this->cache->getKeysByPattern($pattern);
            
            foreach ($relatedKeys as $relatedKey) {
                if (!in_array($relatedKey, $invalidated)) {
                    $success = $this->cache->invalidate($relatedKey) && $success;
                    $invalidated[] = $relatedKey;
                }
            }
        }
        
        return $success;
    }
}
```

## Distributed Caching

Handle distributed caching scenarios:

```php
<?php
// app/Caching/DistributedCache.php

namespace App\Caching;

use Redis;
use Predis\Client;

class DistributedCache
{
    private array $nodes = [];
    private array $config;
    private array $connections = [];
    
    public function __construct(array $config)
    {
        $this->config = $config;
        $this->initializeNodes();
    }
    
    public function get(string $key): mixed
    {
        $node = $this->getNodeForKey($key);
        $connection = $this->getConnection($node);
        
        $value = $connection->get($key);
        
        if ($value === null) {
            // Try other nodes as fallback
            $value = $this->getFromFallbackNodes($key, $node);
        }
        
        return $this->deserialize($value);
    }
    
    public function set(string $key, mixed $value, int $ttl = 3600): bool
    {
        $node = $this->getNodeForKey($key);
        $connection = $this->getConnection($node);
        
        $serialized = $this->serialize($value);
        $success = $connection->setex($key, $ttl, $serialized);
        
        // Replicate to other nodes if configured
        if ($success && ($this->config['replication'] ?? false)) {
            $this->replicateToOtherNodes($key, $serialized, $ttl, $node);
        }
        
        return $success;
    }
    
    public function delete(string $key): bool
    {
        $node = $this->getNodeForKey($key);
        $connection = $this->getConnection($node);
        
        $success = $connection->del($key) > 0;
        
        // Delete from other nodes if configured
        if ($success && ($this->config['replication'] ?? false)) {
            $this->deleteFromOtherNodes($key, $node);
        }
        
        return $success;
    }
    
    public function exists(string $key): bool
    {
        $node = $this->getNodeForKey($key);
        $connection = $this->getConnection($node);
        
        return $connection->exists($key) > 0;
    }
    
    public function increment(string $key, int $value = 1): int
    {
        $node = $this->getNodeForKey($key);
        $connection = $this->getConnection($node);
        
        return $connection->incrby($key, $value);
    }
    
    public function expire(string $key, int $ttl): bool
    {
        $node = $this->getNodeForKey($key);
        $connection = $this->getConnection($node);
        
        return $connection->expire($key, $ttl);
    }
    
    private function initializeNodes(): void
    {
        foreach ($this->config['nodes'] as $node) {
            $this->nodes[] = [
                'host' => $node['host'],
                'port' => $node['port'] ?? 6379,
                'weight' => $node['weight'] ?? 1,
                'password' => $node['password'] ?? null,
                'database' => $node['database'] ?? 0
            ];
        }
    }
    
    private function getNodeForKey(string $key): array
    {
        $hash = crc32($key);
        $totalWeight = array_sum(array_column($this->nodes, 'weight'));
        $targetWeight = $hash % $totalWeight;
        
        $currentWeight = 0;
        foreach ($this->nodes as $node) {
            $currentWeight += $node['weight'];
            if ($currentWeight > $targetWeight) {
                return $node;
            }
        }
        
        return $this->nodes[0];
    }
    
    private function getConnection(array $node): Client
    {
        $key = "{$node['host']}:{$node['port']}";
        
        if (!isset($this->connections[$key])) {
            $this->connections[$key] = new Client([
                'host' => $node['host'],
                'port' => $node['port'],
                'password' => $node['password'],
                'database' => $node['database']
            ]);
        }
        
        return $this->connections[$key];
    }
    
    private function getFromFallbackNodes(string $key, array $excludeNode): mixed
    {
        foreach ($this->nodes as $node) {
            if ($node === $excludeNode) continue;
            
            $connection = $this->getConnection($node);
            $value = $connection->get($key);
            
            if ($value !== null) {
                return $this->deserialize($value);
            }
        }
        
        return null;
    }
    
    private function replicateToOtherNodes(string $key, string $value, int $ttl, array $excludeNode): void
    {
        foreach ($this->nodes as $node) {
            if ($node === $excludeNode) continue;
            
            $connection = $this->getConnection($node);
            $connection->setex($key, $ttl, $value);
        }
    }
    
    private function deleteFromOtherNodes(string $key, array $excludeNode): void
    {
        foreach ($this->nodes as $node) {
            if ($node === $excludeNode) continue;
            
            $connection = $this->getConnection($node);
            $connection->del($key);
        }
    }
    
    private function serialize(mixed $value): string
    {
        return serialize($value);
    }
    
    private function deserialize(string $value): mixed
    {
        return unserialize($value);
    }
}
```

## Cache Warming

Implement intelligent cache warming strategies:

```php
<?php
// app/Caching/CacheWarmer.php

namespace App\Caching;

class CacheWarmer
{
    private CacheManager $cache;
    private array $config;
    
    public function __construct(CacheManager $cache, array $config)
    {
        $this->cache = $cache;
        $this->config = $config;
    }
    
    public function warmByPattern(string $pattern, string $policy = 'default'): array
    {
        $keys = $this->getKeysByPattern($pattern);
        return $this->warmKeys($keys, $policy);
    }
    
    public function warmByPriority(array $priorities): array
    {
        $results = [];
        
        foreach ($priorities as $priority => $keys) {
            $results[$priority] = $this->warmKeys($keys, 'high_priority');
        }
        
        return $results;
    }
    
    public function warmByUsage(): array
    {
        $usageStats = $this->getUsageStatistics();
        $popularKeys = $this->getPopularKeys($usageStats);
        
        return $this->warmKeys($popularKeys, 'usage_based');
    }
    
    public function warmBySchedule(array $schedule): array
    {
        $results = [];
        
        foreach ($schedule as $time => $keys) {
            if ($this->isTimeToWarm($time)) {
                $results[$time] = $this->warmKeys($keys, 'scheduled');
            }
        }
        
        return $results;
    }
    
    public function warmByDependencies(array $dependencies): array
    {
        $results = [];
        
        foreach ($dependencies as $key => $deps) {
            $depKeys = $this->resolveDependencies($deps);
            $results[$key] = $this->warmKeys($depKeys, 'dependency');
        }
        
        return $results;
    }
    
    private function warmKeys(array $keys, string $policy): array
    {
        $warmed = [];
        $failed = [];
        
        foreach ($keys as $key) {
            try {
                $value = $this->generateValue($key);
                $this->cache->set($key, $value, $policy);
                $warmed[] = $key;
            } catch (\Exception $e) {
                $failed[] = $key;
            }
        }
        
        return [
            'warmed' => $warmed,
            'failed' => $failed,
            'total' => count($keys)
        ];
    }
    
    private function getKeysByPattern(string $pattern): array
    {
        // Implementation depends on your cache driver
        return $this->cache->getKeysByPattern($pattern);
    }
    
    private function getUsageStatistics(): array
    {
        // Implementation depends on your monitoring system
        return $this->cache->getUsageStats();
    }
    
    private function getPopularKeys(array $usageStats): array
    {
        arsort($usageStats);
        return array_slice(array_keys($usageStats), 0, 100);
    }
    
    private function isTimeToWarm(string $time): bool
    {
        $now = new DateTime();
        $scheduleTime = new DateTime($time);
        
        return $now->format('H:i') === $scheduleTime->format('H:i');
    }
    
    private function resolveDependencies(array $deps): array
    {
        $keys = [];
        
        foreach ($deps as $dep) {
            if (is_string($dep)) {
                $keys[] = $dep;
            } elseif (is_array($dep)) {
                $keys = array_merge($keys, $this->resolveDependencies($dep));
            }
        }
        
        return array_unique($keys);
    }
    
    private function generateValue(string $key): mixed
    {
        // Implementation depends on your data generation logic
        return $this->dataGenerator->generate($key);
    }
}
```

## Cache Compression

Implement compression strategies:

```php
<?php
// app/Caching/CompressionStrategies.php

namespace App\Caching;

class CompressionStrategies
{
    public function compressGzip(mixed $value): string
    {
        return gzencode(serialize($value), 9);
    }
    
    public function decompressGzip(string $value): mixed
    {
        return unserialize(gzdecode($value));
    }
    
    public function compressLZ4(mixed $value): string
    {
        if (!extension_loaded('lz4')) {
            throw new \RuntimeException('LZ4 extension not loaded');
        }
        
        return lz4_compress(serialize($value));
    }
    
    public function decompressLZ4(string $value): mixed
    {
        if (!extension_loaded('lz4')) {
            throw new \RuntimeException('LZ4 extension not loaded');
        }
        
        return unserialize(lz4_uncompress($value));
    }
    
    public function compressZstd(mixed $value): string
    {
        if (!extension_loaded('zstd')) {
            throw new \RuntimeException('Zstd extension not loaded');
        }
        
        return zstd_compress(serialize($value));
    }
    
    public function decompressZstd(string $value): mixed
    {
        if (!extension_loaded('zstd')) {
            throw new \RuntimeException('Zstd extension not loaded');
        }
        
        return unserialize(zstd_uncompress($value));
    }
    
    public function shouldCompress(mixed $value, int $threshold = 1024): bool
    {
        $size = strlen(serialize($value));
        return $size > $threshold;
    }
    
    public function getCompressionRatio(string $original, string $compressed): float
    {
        return strlen($compressed) / strlen($original);
    }
}
```

## Cache Analytics

Monitor and analyze cache performance:

```php
<?php
// app/Caching/CacheAnalytics.php

namespace App\Caching;

class CacheAnalytics
{
    private CacheManager $cache;
    private array $metrics = [];
    
    public function __construct(CacheManager $cache)
    {
        $this->cache = $cache;
    }
    
    public function recordHit(string $key): void
    {
        $this->incrementMetric("hits.{$key}");
        $this->incrementMetric('hits.total');
    }
    
    public function recordMiss(string $key): void
    {
        $this->incrementMetric("misses.{$key}");
        $this->incrementMetric('misses.total');
    }
    
    public function recordSet(string $key, int $size): void
    {
        $this->incrementMetric("sets.{$key}");
        $this->incrementMetric('sets.total');
        $this->addMetric("size.{$key}", $size);
    }
    
    public function recordDelete(string $key): void
    {
        $this->incrementMetric("deletes.{$key}");
        $this->incrementMetric('deletes.total');
    }
    
    public function getHitRate(): float
    {
        $hits = $this->getMetric('hits.total') ?: 0;
        $misses = $this->getMetric('misses.total') ?: 0;
        $total = $hits + $misses;
        
        return $total > 0 ? $hits / $total : 0;
    }
    
    public function getPopularKeys(int $limit = 10): array
    {
        $keys = [];
        $metrics = $this->getMetricsByPattern('hits.*');
        
        foreach ($metrics as $key => $hits) {
            $keyName = str_replace('hits.', '', $key);
            $keys[$keyName] = $hits;
        }
        
        arsort($keys);
        return array_slice($keys, 0, $limit);
    }
    
    public function getCacheSize(): int
    {
        $metrics = $this->getMetricsByPattern('size.*');
        return array_sum($metrics);
    }
    
    public function getPerformanceMetrics(): array
    {
        return [
            'hit_rate' => $this->getHitRate(),
            'total_requests' => $this->getMetric('hits.total') + $this->getMetric('misses.total'),
            'total_sets' => $this->getMetric('sets.total'),
            'total_deletes' => $this->getMetric('deletes.total'),
            'cache_size' => $this->getCacheSize(),
            'popular_keys' => $this->getPopularKeys()
        ];
    }
    
    private function incrementMetric(string $key): void
    {
        $this->metrics[$key] = ($this->metrics[$key] ?? 0) + 1;
    }
    
    private function addMetric(string $key, int $value): void
    {
        $this->metrics[$key] = ($this->metrics[$key] ?? 0) + $value;
    }
    
    private function getMetric(string $key): int
    {
        return $this->metrics[$key] ?? 0;
    }
    
    private function getMetricsByPattern(string $pattern): array
    {
        $metrics = [];
        $regex = str_replace('*', '.*', $pattern);
        
        foreach ($this->metrics as $key => $value) {
            if (preg_match("/^{$regex}$/", $key)) {
                $metrics[$key] = $value;
            }
        }
        
        return $metrics;
    }
}
```

## Cache Security

Implement security measures for caching:

```php
<?php
// app/Caching/CacheSecurity.php

namespace App\Caching;

class CacheSecurity
{
    private string $encryptionKey;
    private array $config;
    
    public function __construct(string $encryptionKey, array $config)
    {
        $this->encryptionKey = $encryptionKey;
        $this->config = $config;
    }
    
    public function encryptValue(mixed $value): string
    {
        $serialized = serialize($value);
        $iv = random_bytes(16);
        
        $encrypted = openssl_encrypt(
            $serialized,
            'AES-256-CBC',
            $this->encryptionKey,
            OPENSSL_RAW_DATA,
            $iv
        );
        
        return base64_encode($iv . $encrypted);
    }
    
    public function decryptValue(string $encryptedValue): mixed
    {
        $data = base64_decode($encryptedValue);
        $iv = substr($data, 0, 16);
        $encrypted = substr($data, 16);
        
        $decrypted = openssl_decrypt(
            $encrypted,
            'AES-256-CBC',
            $this->encryptionKey,
            OPENSSL_RAW_DATA,
            $iv
        );
        
        return unserialize($decrypted);
    }
    
    public function hashKey(string $key): string
    {
        return hash('sha256', $key . $this->config['key_salt'] ?? '');
    }
    
    public function validateKey(string $key): bool
    {
        $forbiddenPatterns = [
            '/^system:/',
            '/^internal:/',
            '/^admin:/',
            '/[<>"\']/'
        ];
        
        foreach ($forbiddenPatterns as $pattern) {
            if (preg_match($pattern, $key)) {
                return false;
            }
        }
        
        return true;
    }
    
    public function sanitizeValue(mixed $value): mixed
    {
        if (is_string($value)) {
            return htmlspecialchars($value, ENT_QUOTES, 'UTF-8');
        } elseif (is_array($value)) {
            return array_map([$this, 'sanitizeValue'], $value);
        }
        
        return $value;
    }
    
    public function shouldEncrypt(string $key, mixed $value): bool
    {
        $sensitivePatterns = [
            '/password/',
            '/token/',
            '/secret/',
            '/key/',
            '/credential/'
        ];
        
        foreach ($sensitivePatterns as $pattern) {
            if (preg_match($pattern, strtolower($key))) {
                return true;
            }
        }
        
        return false;
    }
}
```

## Performance Optimization

Optimize cache performance:

```php
<?php
// app/Caching/PerformanceOptimizer.php

namespace App\Caching;

class PerformanceOptimizer
{
    private CacheManager $cache;
    private array $config;
    
    public function __construct(CacheManager $cache, array $config)
    {
        $this->cache = $cache;
        $this->config = $config;
    }
    
    public function optimizeTtl(string $key, int $accessCount): int
    {
        $baseTtl = $this->config['base_ttl'] ?? 3600;
        $maxTtl = $this->config['max_ttl'] ?? 86400;
        
        // Increase TTL based on access frequency
        $multiplier = min(2, 1 + ($accessCount / 100));
        
        return min($maxTtl, (int)($baseTtl * $multiplier));
    }
    
    public function batchGet(array $keys): array
    {
        $results = [];
        $batches = array_chunk($keys, 100);
        
        foreach ($batches as $batch) {
            $batchResults = $this->cache->mget($batch);
            $results = array_merge($results, $batchResults);
        }
        
        return $results;
    }
    
    public function batchSet(array $data, int $ttl = 3600): bool
    {
        $batches = array_chunk($data, 100, true);
        $success = true;
        
        foreach ($batches as $batch) {
            $success = $this->cache->mset($batch, $ttl) && $success;
        }
        
        return $success;
    }
    
    public function prefetch(array $keys): void
    {
        foreach ($keys as $key) {
            if (!$this->cache->exists($key)) {
                $this->cache->set($key, $this->generateValue($key));
            }
        }
    }
    
    public function optimizeStorage(): array
    {
        $stats = $this->cache->getStats();
        $optimizations = [];
        
        // Remove expired keys
        $expired = $this->cache->cleanup();
        $optimizations['expired_removed'] = $expired;
        
        // Compress large values
        $compressed = $this->compressLargeValues();
        $optimizations['compressed'] = $compressed;
        
        // Move hot data to faster storage
        $moved = $this->moveHotData();
        $optimizations['hot_data_moved'] = $moved;
        
        return $optimizations;
    }
    
    private function compressLargeValues(): int
    {
        $compressed = 0;
        $threshold = $this->config['compression_threshold'] ?? 1024;
        
        $keys = $this->cache->getKeysByPattern('*');
        
        foreach ($keys as $key) {
            $value = $this->cache->get($key);
            
            if (is_string($value) && strlen($value) > $threshold) {
                $compressedValue = gzencode($value, 9);
                $this->cache->set($key, $compressedValue);
                $compressed++;
            }
        }
        
        return $compressed;
    }
    
    private function moveHotData(): int
    {
        $moved = 0;
        $analytics = new CacheAnalytics($this->cache);
        $popularKeys = $analytics->getPopularKeys(50);
        
        foreach ($popularKeys as $key => $hits) {
            if ($hits > 100) {
                $value = $this->cache->get($key);
                $this->cache->set($key, $value, 'fast_storage');
                $moved++;
            }
        }
        
        return $moved;
    }
    
    private function generateValue(string $key): mixed
    {
        // Implementation depends on your data generation logic
        return $this->dataGenerator->generate($key);
    }
}
```

## Best Practices

Follow these best practices for optimal caching:

```php
<?php
// config/caching-best-practices.tsk

caching_best_practices = {
    key_naming = {
        use_namespaces = true
        include_version = true
        use_consistent_separators = true
        avoid_special_characters = true
    }
    
    ttl_strategy = {
        short_ttl = 300      // 5 minutes for frequently changing data
        medium_ttl = 3600    // 1 hour for moderately stable data
        long_ttl = 86400     // 24 hours for stable data
        infinite_ttl = 0     // No expiration for reference data
    }
    
    invalidation = {
        use_events = true
        implement_patterns = true
        cascade_invalidation = true
        version_based = true
    }
    
    compression = {
        enable_for_large_values = true
        threshold_bytes = 1024
        algorithm = "gzip"
        compression_level = 9
    }
    
    security = {
        encrypt_sensitive_data = true
        hash_keys = true
        validate_input = true
        sanitize_output = true
    }
    
    monitoring = {
        track_hit_rates = true
        monitor_performance = true
        alert_on_failures = true
        log_operations = true
    }
    
    optimization = {
        use_batching = true
        implement_prefetching = true
        optimize_storage = true
        balance_memory_usage = true
    }
}

// Example usage in PHP
class CacheBestPractices
{
    public function implementBestPractices(): void
    {
        // 1. Use proper key naming
        $key = $this->generateKey('user', 'profile', 123, 'v2');
        
        // 2. Choose appropriate TTL
        $ttl = $this->getOptimalTtl('user_profile');
        
        // 3. Implement proper invalidation
        $this->setupInvalidation($key, ['user.updated', 'profile.changed']);
        
        // 4. Use compression for large values
        if ($this->shouldCompress($value)) {
            $value = $this->compress($value);
        }
        
        // 5. Encrypt sensitive data
        if ($this->isSensitive($key)) {
            $value = $this->encrypt($value);
        }
        
        // 6. Monitor performance
        $this->recordOperation('set', $key, strlen($value));
    }
    
    private function generateKey(string $namespace, string $type, int $id, string $version): string
    {
        return "{$namespace}:{$type}:{$id}:v{$version}";
    }
    
    private function getOptimalTtl(string $dataType): int
    {
        $ttlMap = [
            'user_profile' => 3600,
            'product_catalog' => 7200,
            'api_response' => 300,
            'reference_data' => 0
        ];
        
        return $ttlMap[$dataType] ?? 3600;
    }
    
    private function setupInvalidation(string $key, array $events): void
    {
        foreach ($events as $event) {
            $this->eventDispatcher->listen($event, function () use ($key) {
                $this->cache->invalidate($key);
            });
        }
    }
    
    private function shouldCompress(mixed $value): bool
    {
        return is_string($value) && strlen($value) > 1024;
    }
    
    private function isSensitive(string $key): bool
    {
        $sensitivePatterns = ['/password/', '/token/', '/secret/'];
        
        foreach ($sensitivePatterns as $pattern) {
            if (preg_match($pattern, $key)) {
                return true;
            }
        }
        
        return false;
    }
    
    private function recordOperation(string $operation, string $key, int $size): void
    {
        $this->analytics->recordOperation($operation, $key, $size);
    }
}
```

This comprehensive guide covers advanced caching strategies in TuskLang with PHP integration. The caching system is designed to be flexible, secure, and performant while maintaining the rebellious spirit of TuskLang development. 