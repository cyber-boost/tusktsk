<?php
/**
 * Cache Operator
 * 
 * Enhanced caching operator with TTL support, multiple backends,
 * and advanced caching strategies.
 * 
 * @package TuskPHP\CoreOperators
 * @version 2.0.0
 */

namespace TuskPHP\CoreOperators;

use TuskPHP\Utils\TuskLangCache;

/**
 * Cache Operator
 * 
 * Provides advanced caching capabilities with multiple backends,
 * TTL support, and intelligent cache management.
 */
class CacheOperator extends BaseOperator
{
    private TuskLangCache $cache;
    private array $backends = [];
    private array $connectionPools = [];
    
    public function __construct()
    {
        $this->version = '2.0.0';
        $this->requiredFields = ['key'];
        $this->optionalFields = [
            'ttl', 'backend', 'compute', 'refresh_ahead', 'tags',
            'compression', 'serialization', 'fallback'
        ];
        
        $this->defaultConfig = [
            'ttl' => 3600,
            'backend' => 'memory',
            'compression' => false,
            'serialization' => 'json',
            'refresh_ahead' => 0,
            'tags' => []
        ];
        
        $this->initializeCache();
    }
    
    public function getName(): string
    {
        return 'cache';
    }
    
    protected function getDescription(): string
    {
        return 'Advanced caching operator with TTL support, multiple backends, and intelligent cache management';
    }
    
    protected function getExamples(): array
    {
        return [
            'basic' => '@cache("user:123", user_data)',
            'with_ttl' => '@cache({key: "user:123", ttl: "1h", compute: expensive_operation()})',
            'redis_backend' => '@cache({key: "data", backend: "redis", ttl: "5m"})',
            'with_tags' => '@cache({key: "posts", tags: ["content", "public"], ttl: "30m"})',
            'refresh_ahead' => '@cache({key: "config", ttl: "1h", refresh_ahead: "5m"})'
        ];
    }
    
    protected function getErrorCodes(): array
    {
        return array_merge(parent::getErrorCodes(), [
            'CACHE_MISS' => 'Cache key not found',
            'BACKEND_UNAVAILABLE' => 'Cache backend is not available',
            'SERIALIZATION_FAILED' => 'Failed to serialize cache value',
            'DESERIALIZATION_FAILED' => 'Failed to deserialize cache value',
            'COMPRESSION_FAILED' => 'Failed to compress cache value',
            'DECOMPRESSION_FAILED' => 'Failed to decompress cache value'
        ]);
    }
    
    /**
     * Initialize cache system
     */
    private function initializeCache(): void
    {
        $this->cache = TuskLangCache::getInstance();
        
        // Initialize backends
        $this->initializeBackends();
    }
    
    /**
     * Initialize cache backends
     */
    private function initializeBackends(): void
    {
        // Memory backend (default)
        $this->backends['memory'] = new CacheBackends\MemoryBackend();
        
        // Redis backend
        if (extension_loaded('redis')) {
            $this->backends['redis'] = new CacheBackends\RedisBackend();
        }
        
        // Memcached backend
        if (extension_loaded('memcached')) {
            $this->backends['memcached'] = new CacheBackends\MemcachedBackend();
        }
        
        // File backend
        $this->backends['file'] = new CacheBackends\FileBackend();
    }
    
    /**
     * Execute cache operator
     */
    protected function executeOperator(array $config, array $context): mixed
    {
        $key = $this->resolveVariable($config['key'], $context);
        $backend = $config['backend'];
        $ttl = $this->parseTTL($config['ttl']);
        
        // Get backend instance
        $backendInstance = $this->getBackend($backend);
        
        // Check if value exists in cache
        $cachedValue = $backendInstance->get($key);
        
        if ($cachedValue !== null) {
            $this->log('info', 'Cache hit', ['key' => $key, 'backend' => $backend]);
            return $cachedValue;
        }
        
        // Cache miss - compute value if provided
        if (isset($config['compute'])) {
            $computedValue = $this->resolveVariable($config['compute'], $context);
            
            // Store in cache
            $backendInstance->set($key, $computedValue, $ttl, $config);
            
            $this->log('info', 'Cache miss - computed and stored', [
                'key' => $key,
                'backend' => $backend,
                'ttl' => $ttl
            ]);
            
            return $computedValue;
        }
        
        // No compute function provided
        $this->log('warning', 'Cache miss - no compute function', ['key' => $key]);
        
        // Return fallback value if provided
        if (isset($config['fallback'])) {
            return $this->resolveVariable($config['fallback'], $context);
        }
        
        return null;
    }
    
    /**
     * Get cache backend
     */
    private function getBackend(string $name): CacheBackends\CacheBackendInterface
    {
        if (!isset($this->backends[$name])) {
            throw new \InvalidArgumentException("Unknown cache backend: {$name}");
        }
        
        return $this->backends[$name];
    }
    
    /**
     * Parse TTL string to seconds
     */
    private function parseTTL(mixed $ttl): int
    {
        if (is_numeric($ttl)) {
            return (int)$ttl;
        }
        
        if (is_string($ttl)) {
            return $this->parseTTLString($ttl);
        }
        
        return 3600; // Default 1 hour
    }
    
    /**
     * Parse TTL string (e.g., "1h", "30m", "2d")
     */
    private function parseTTLString(string $ttl): int
    {
        $units = [
            's' => 1,
            'm' => 60,
            'h' => 3600,
            'd' => 86400,
            'w' => 604800
        ];
        
        if (preg_match('/^(\d+)([smhdw])$/', strtolower($ttl), $matches)) {
            $value = (int)$matches[1];
            $unit = $matches[2];
            
            if (isset($units[$unit])) {
                return $value * $units[$unit];
            }
        }
        
        // Try to parse as seconds
        if (is_numeric($ttl)) {
            return (int)$ttl;
        }
        
        return 3600; // Default 1 hour
    }
    
    /**
     * Custom validation
     */
    protected function customValidate(array $config): array
    {
        $errors = [];
        $warnings = [];
        
        // Validate backend
        if (isset($config['backend']) && !isset($this->backends[$config['backend']])) {
            $errors[] = "Unknown cache backend: {$config['backend']}";
        }
        
        // Validate TTL
        if (isset($config['ttl'])) {
            $ttl = $this->parseTTL($config['ttl']);
            if ($ttl <= 0) {
                $errors[] = "TTL must be positive";
            }
        }
        
        // Validate tags
        if (isset($config['tags']) && !is_array($config['tags'])) {
            $errors[] = "Tags must be an array";
        }
        
        return ['errors' => $errors, 'warnings' => $warnings];
    }
    
    /**
     * Get cache statistics
     */
    public function getStatistics(): array
    {
        $stats = [];
        
        foreach ($this->backends as $name => $backend) {
            $stats[$name] = $backend->getStatistics();
        }
        
        return $stats;
    }
    
    /**
     * Invalidate cache by tags
     */
    public function invalidateByTags(array $tags): int
    {
        $invalidated = 0;
        
        foreach ($this->backends as $backend) {
            $invalidated += $backend->invalidateByTags($tags);
        }
        
        return $invalidated;
    }
    
    /**
     * Clear all caches
     */
    public function clear(): void
    {
        foreach ($this->backends as $backend) {
            $backend->clear();
        }
    }
    
    /**
     * Cleanup resources
     */
    public function cleanup(): void
    {
        foreach ($this->backends as $backend) {
            $backend->cleanup();
        }
    }
}

/**
 * Cache Backend Interface
 */
interface CacheBackendInterface
{
    public function get(string $key): mixed;
    public function set(string $key, mixed $value, int $ttl, array $options = []): bool;
    public function delete(string $key): bool;
    public function clear(): void;
    public function getStatistics(): array;
    public function invalidateByTags(array $tags): int;
    public function cleanup(): void;
}

/**
 * Memory Cache Backend
 */
class MemoryBackend implements CacheBackendInterface
{
    private array $cache = [];
    private array $expiry = [];
    private array $tags = [];
    private array $stats = [
        'hits' => 0,
        'misses' => 0,
        'sets' => 0,
        'deletes' => 0
    ];
    
    public function get(string $key): mixed
    {
        // Check if key exists and is not expired
        if (!isset($this->cache[$key])) {
            $this->stats['misses']++;
            return null;
        }
        
        if (isset($this->expiry[$key]) && $this->expiry[$key] < time()) {
            $this->delete($key);
            $this->stats['misses']++;
            return null;
        }
        
        $this->stats['hits']++;
        return $this->cache[$key];
    }
    
    public function set(string $key, mixed $value, int $ttl, array $options = []): bool
    {
        $this->cache[$key] = $value;
        $this->expiry[$key] = time() + $ttl;
        
        // Store tags if provided
        if (isset($options['tags']) && is_array($options['tags'])) {
            foreach ($options['tags'] as $tag) {
                $this->tags[$tag][] = $key;
            }
        }
        
        $this->stats['sets']++;
        return true;
    }
    
    public function delete(string $key): bool
    {
        unset($this->cache[$key], $this->expiry[$key]);
        $this->stats['deletes']++;
        return true;
    }
    
    public function clear(): void
    {
        $this->cache = [];
        $this->expiry = [];
        $this->tags = [];
    }
    
    public function getStatistics(): array
    {
        return array_merge($this->stats, [
            'size' => count($this->cache),
            'expired' => count(array_filter($this->expiry, fn($exp) => $exp < time()))
        ]);
    }
    
    public function invalidateByTags(array $tags): int
    {
        $invalidated = 0;
        
        foreach ($tags as $tag) {
            if (isset($this->tags[$tag])) {
                foreach ($this->tags[$tag] as $key) {
                    $this->delete($key);
                    $invalidated++;
                }
                unset($this->tags[$tag]);
            }
        }
        
        return $invalidated;
    }
    
    public function cleanup(): void
    {
        // Remove expired entries
        $now = time();
        foreach ($this->expiry as $key => $expiry) {
            if ($expiry < $now) {
                $this->delete($key);
            }
        }
    }
}

/**
 * Redis Cache Backend
 */
class RedisBackend implements CacheBackendInterface
{
    private ?\Redis $redis = null;
    private array $stats = [
        'hits' => 0,
        'misses' => 0,
        'sets' => 0,
        'deletes' => 0
    ];
    
    public function __construct()
    {
        $this->initializeRedis();
    }
    
    private function initializeRedis(): void
    {
        try {
            $this->redis = new \Redis();
            $this->redis->connect(
                $_ENV['REDIS_HOST'] ?? 'localhost',
                $_ENV['REDIS_PORT'] ?? 6379
            );
            
            if (isset($_ENV['REDIS_PASSWORD'])) {
                $this->redis->auth($_ENV['REDIS_PASSWORD']);
            }
            
            if (isset($_ENV['REDIS_DATABASE'])) {
                $this->redis->select($_ENV['REDIS_DATABASE']);
            }
        } catch (\Exception $e) {
            error_log("Redis connection failed: " . $e->getMessage());
            $this->redis = null;
        }
    }
    
    public function get(string $key): mixed
    {
        if (!$this->redis) {
            return null;
        }
        
        try {
            $value = $this->redis->get($key);
            if ($value === false) {
                $this->stats['misses']++;
                return null;
            }
            
            $this->stats['hits']++;
            return json_decode($value, true);
        } catch (\Exception $e) {
            error_log("Redis get failed: " . $e->getMessage());
            return null;
        }
    }
    
    public function set(string $key, mixed $value, int $ttl, array $options = []): bool
    {
        if (!$this->redis) {
            return false;
        }
        
        try {
            $serialized = json_encode($value);
            $result = $this->redis->setex($key, $ttl, $serialized);
            
            if ($result && isset($options['tags'])) {
                foreach ($options['tags'] as $tag) {
                    $this->redis->sAdd("tag:{$tag}", $key);
                }
            }
            
            $this->stats['sets']++;
            return $result;
        } catch (\Exception $e) {
            error_log("Redis set failed: " . $e->getMessage());
            return false;
        }
    }
    
    public function delete(string $key): bool
    {
        if (!$this->redis) {
            return false;
        }
        
        try {
            $result = $this->redis->del($key);
            $this->stats['deletes']++;
            return $result > 0;
        } catch (\Exception $e) {
            error_log("Redis delete failed: " . $e->getMessage());
            return false;
        }
    }
    
    public function clear(): void
    {
        if (!$this->redis) {
            return;
        }
        
        try {
            $this->redis->flushDB();
        } catch (\Exception $e) {
            error_log("Redis clear failed: " . $e->getMessage());
        }
    }
    
    public function getStatistics(): array
    {
        if (!$this->redis) {
            return array_merge($this->stats, ['connected' => false]);
        }
        
        try {
            $info = $this->redis->info();
            return array_merge($this->stats, [
                'connected' => true,
                'used_memory' => $info['used_memory'] ?? 0,
                'connected_clients' => $info['connected_clients'] ?? 0
            ]);
        } catch (\Exception $e) {
            return array_merge($this->stats, ['connected' => false]);
        }
    }
    
    public function invalidateByTags(array $tags): int
    {
        if (!$this->redis) {
            return 0;
        }
        
        $invalidated = 0;
        
        try {
            foreach ($tags as $tag) {
                $keys = $this->redis->sMembers("tag:{$tag}");
                foreach ($keys as $key) {
                    $this->delete($key);
                    $invalidated++;
                }
                $this->redis->del("tag:{$tag}");
            }
        } catch (\Exception $e) {
            error_log("Redis tag invalidation failed: " . $e->getMessage());
        }
        
        return $invalidated;
    }
    
    public function cleanup(): void
    {
        // Redis handles cleanup automatically
    }
}

/**
 * File Cache Backend
 */
class FileBackend implements CacheBackendInterface
{
    private string $cacheDir;
    private array $stats = [
        'hits' => 0,
        'misses' => 0,
        'sets' => 0,
        'deletes' => 0
    ];
    
    public function __construct()
    {
        $this->cacheDir = sys_get_temp_dir() . '/tusk_cache/';
        if (!is_dir($this->cacheDir)) {
            mkdir($this->cacheDir, 0755, true);
        }
    }
    
    public function get(string $key): mixed
    {
        $filename = $this->getFilename($key);
        
        if (!file_exists($filename)) {
            $this->stats['misses']++;
            return null;
        }
        
        $data = file_get_contents($filename);
        if ($data === false) {
            $this->stats['misses']++;
            return null;
        }
        
        $cacheData = json_decode($data, true);
        if (!$cacheData || !isset($cacheData['expiry']) || $cacheData['expiry'] < time()) {
            $this->delete($key);
            $this->stats['misses']++;
            return null;
        }
        
        $this->stats['hits']++;
        return $cacheData['value'];
    }
    
    public function set(string $key, mixed $value, int $ttl, array $options = []): bool
    {
        $filename = $this->getFilename($key);
        
        $cacheData = [
            'value' => $value,
            'expiry' => time() + $ttl,
            'created' => time(),
            'tags' => $options['tags'] ?? []
        ];
        
        $data = json_encode($cacheData);
        if ($data === false) {
            return false;
        }
        
        $result = file_put_contents($filename, $data, LOCK_EX);
        $this->stats['sets']++;
        
        return $result !== false;
    }
    
    public function delete(string $key): bool
    {
        $filename = $this->getFilename($key);
        
        if (file_exists($filename)) {
            unlink($filename);
            $this->stats['deletes']++;
            return true;
        }
        
        return false;
    }
    
    public function clear(): void
    {
        $files = glob($this->cacheDir . '*');
        foreach ($files as $file) {
            if (is_file($file)) {
                unlink($file);
            }
        }
    }
    
    public function getStatistics(): array
    {
        $files = glob($this->cacheDir . '*');
        $size = 0;
        $expired = 0;
        
        foreach ($files as $file) {
            if (is_file($file)) {
                $size += filesize($file);
                $data = file_get_contents($file);
                $cacheData = json_decode($data, true);
                if ($cacheData && isset($cacheData['expiry']) && $cacheData['expiry'] < time()) {
                    $expired++;
                }
            }
        }
        
        return array_merge($this->stats, [
            'files' => count($files),
            'size' => $size,
            'expired' => $expired
        ]);
    }
    
    public function invalidateByTags(array $tags): int
    {
        $files = glob($this->cacheDir . '*');
        $invalidated = 0;
        
        foreach ($files as $file) {
            if (is_file($file)) {
                $data = file_get_contents($file);
                $cacheData = json_decode($data, true);
                
                if ($cacheData && isset($cacheData['tags'])) {
                    foreach ($tags as $tag) {
                        if (in_array($tag, $cacheData['tags'])) {
                            unlink($file);
                            $invalidated++;
                            break;
                        }
                    }
                }
            }
        }
        
        return $invalidated;
    }
    
    public function cleanup(): void
    {
        $files = glob($this->cacheDir . '*');
        
        foreach ($files as $file) {
            if (is_file($file)) {
                $data = file_get_contents($file);
                $cacheData = json_decode($data, true);
                
                if ($cacheData && isset($cacheData['expiry']) && $cacheData['expiry'] < time()) {
                    unlink($file);
                }
            }
        }
    }
    
    private function getFilename(string $key): string
    {
        return $this->cacheDir . md5($key) . '.cache';
    }
} 