<?php

namespace TuskLang\A3\G1;

/**
 * Advanced Cache Manager - High Performance Caching System
 * 
 * Features:
 * - LRU (Least Recently Used) eviction policy
 * - Automatic compression for large values
 * - Memory usage monitoring
 * - Cache statistics and analytics
 * - Multi-tier caching (memory + file)
 * - Atomic operations with locking
 * 
 * @version 2.0.0
 * @package TuskLang\A3\G1
 */
class CacheManager
{
    private array $cache = [];
    private array $accessTimes = [];
    private array $hitStats = [];
    private int $maxSize;
    private int $maxMemory;
    private bool $compressionEnabled;
    private string $cacheDir;
    private int $currentMemoryUsage = 0;
    private array $locks = [];
    
    public function __construct(array $config = [])
    {
        $this->maxSize = $config['max_size'] ?? 10000;
        $this->maxMemory = $config['max_memory'] ?? 1024 * 1024 * 50; // 50MB
        $this->compressionEnabled = $config['compression'] ?? true;
        $this->cacheDir = $config['cache_dir'] ?? '/tmp/tusklang_cache';
        
        $this->initializeStats();
        $this->ensureCacheDir();
    }
    
    /**
     * Store value in cache with optional TTL
     */
    public function set(string $key, mixed $value, int $ttl = 0): bool
    {
        $this->acquireLock($key);
        
        try {
            $serialized = $this->serialize($value);
            $compressed = $this->compress($serialized);
            $size = strlen($compressed);
            
            // Check memory limits
            if ($this->currentMemoryUsage + $size > $this->maxMemory) {
                $this->evictLRU();
            }
            
            // Check size limits
            if (count($this->cache) >= $this->maxSize) {
                $this->evictLRU();
            }
            
            $cacheItem = [
                'data' => $compressed,
                'size' => $size,
                'ttl' => $ttl > 0 ? time() + $ttl : 0,
                'compressed' => $this->compressionEnabled && $size > 1024,
                'created' => time(),
                'hits' => 0
            ];
            
            // Remove old value if exists
            if (isset($this->cache[$key])) {
                $this->currentMemoryUsage -= $this->cache[$key]['size'];
            }
            
            $this->cache[$key] = $cacheItem;
            $this->accessTimes[$key] = microtime(true);
            $this->currentMemoryUsage += $size;
            
            // Also store in file cache for persistence
            $this->storeInFileCache($key, $cacheItem);
            
            $this->updateStats('sets');
            return true;
            
        } finally {
            $this->releaseLock($key);
        }
    }
    
    /**
     * Retrieve value from cache
     */
    public function get(string $key): mixed
    {
        $this->acquireLock($key);
        
        try {
            // Check memory cache first
            if (isset($this->cache[$key])) {
                $item = $this->cache[$key];
                
                // Check TTL
                if ($item['ttl'] > 0 && time() > $item['ttl']) {
                    $this->delete($key);
                    $this->updateStats('misses');
                    return null;
                }
                
                // Update access time and hit count
                $this->accessTimes[$key] = microtime(true);
                $this->cache[$key]['hits']++;
                
                $data = $this->decompress($item['data'], $item['compressed']);
                $value = $this->unserialize($data);
                
                $this->updateStats('hits');
                return $value;
            }
            
            // Check file cache
            $fileItem = $this->getFromFileCache($key);
            if ($fileItem !== null) {
                // Restore to memory cache
                $this->cache[$key] = $fileItem;
                $this->accessTimes[$key] = microtime(true);
                $this->currentMemoryUsage += $fileItem['size'];
                
                $data = $this->decompress($fileItem['data'], $fileItem['compressed']);
                $value = $this->unserialize($data);
                
                $this->updateStats('file_hits');
                return $value;
            }
            
            $this->updateStats('misses');
            return null;
            
        } finally {
            $this->releaseLock($key);
        }
    }
    
    /**
     * Delete key from cache
     */
    public function delete(string $key): bool
    {
        $this->acquireLock($key);
        
        try {
            if (isset($this->cache[$key])) {
                $this->currentMemoryUsage -= $this->cache[$key]['size'];
                unset($this->cache[$key]);
                unset($this->accessTimes[$key]);
                
                // Also remove from file cache
                $this->deleteFromFileCache($key);
                
                $this->updateStats('deletes');
                return true;
            }
            return false;
            
        } finally {
            $this->releaseLock($key);
        }
    }
    
    /**
     * Check if key exists in cache
     */
    public function exists(string $key): bool
    {
        if (isset($this->cache[$key])) {
            $item = $this->cache[$key];
            if ($item['ttl'] > 0 && time() > $item['ttl']) {
                $this->delete($key);
                return false;
            }
            return true;
        }
        
        return $this->existsInFileCache($key);
    }
    
    /**
     * Clear all cache entries
     */
    public function clear(): void
    {
        $this->cache = [];
        $this->accessTimes = [];
        $this->currentMemoryUsage = 0;
        $this->clearFileCache();
        $this->updateStats('clears');
    }
    
    /**
     * Get cache statistics
     */
    public function getStats(): array
    {
        $totalRequests = $this->hitStats['hits'] + $this->hitStats['misses'];
        $hitRate = $totalRequests > 0 ? ($this->hitStats['hits'] / $totalRequests) * 100 : 0;
        
        return [
            'memory_usage' => $this->currentMemoryUsage,
            'memory_limit' => $this->maxMemory,
            'memory_usage_percent' => ($this->currentMemoryUsage / $this->maxMemory) * 100,
            'item_count' => count($this->cache),
            'max_items' => $this->maxSize,
            'hit_rate' => round($hitRate, 2),
            'hits' => $this->hitStats['hits'],
            'misses' => $this->hitStats['misses'],
            'file_hits' => $this->hitStats['file_hits'],
            'sets' => $this->hitStats['sets'],
            'deletes' => $this->hitStats['deletes'],
            'clears' => $this->hitStats['clears'],
            'evictions' => $this->hitStats['evictions']
        ];
    }
    
    /**
     * Get top accessed keys
     */
    public function getTopKeys(int $limit = 10): array
    {
        $keyHits = [];
        foreach ($this->cache as $key => $item) {
            $keyHits[$key] = $item['hits'];
        }
        
        arsort($keyHits);
        return array_slice($keyHits, 0, $limit, true);
    }
    
    /**
     * Optimize cache by removing expired entries
     */
    public function optimize(): int
    {
        $removed = 0;
        $now = time();
        
        foreach ($this->cache as $key => $item) {
            if ($item['ttl'] > 0 && $now > $item['ttl']) {
                $this->delete($key);
                $removed++;
            }
        }
        
        return $removed;
    }
    
    private function evictLRU(): void
    {
        if (empty($this->accessTimes)) {
            return;
        }
        
        // Find least recently used key
        $lruKey = array_keys($this->accessTimes, min($this->accessTimes))[0];
        $this->delete($lruKey);
        $this->updateStats('evictions');
    }
    
    private function serialize(mixed $value): string
    {
        return serialize($value);
    }
    
    private function unserialize(string $data): mixed
    {
        return unserialize($data);
    }
    
    private function compress(string $data): string
    {
        if (!$this->compressionEnabled || strlen($data) < 1024) {
            return $data;
        }
        
        if (function_exists('gzcompress')) {
            return gzcompress($data, 6);
        }
        
        return $data;
    }
    
    private function decompress(string $data, bool $wasCompressed): string
    {
        if (!$wasCompressed) {
            return $data;
        }
        
        if (function_exists('gzuncompress')) {
            $result = gzuncompress($data);
            return $result !== false ? $result : $data;
        }
        
        return $data;
    }
    
    private function storeInFileCache(string $key, array $item): void
    {
        $filename = $this->getFileCachePath($key);
        file_put_contents($filename, json_encode($item), LOCK_EX);
    }
    
    private function getFromFileCache(string $key): ?array
    {
        $filename = $this->getFileCachePath($key);
        if (!file_exists($filename)) {
            return null;
        }
        
        $data = file_get_contents($filename);
        if ($data === false) {
            return null;
        }
        
        $item = json_decode($data, true);
        if (!$item) {
            return null;
        }
        
        // Check TTL
        if ($item['ttl'] > 0 && time() > $item['ttl']) {
            unlink($filename);
            return null;
        }
        
        return $item;
    }
    
    private function deleteFromFileCache(string $key): void
    {
        $filename = $this->getFileCachePath($key);
        if (file_exists($filename)) {
            unlink($filename);
        }
    }
    
    private function existsInFileCache(string $key): bool
    {
        $filename = $this->getFileCachePath($key);
        return file_exists($filename);
    }
    
    private function clearFileCache(): void
    {
        if (is_dir($this->cacheDir)) {
            $files = glob($this->cacheDir . '/*');
            foreach ($files as $file) {
                if (is_file($file)) {
                    unlink($file);
                }
            }
        }
    }
    
    private function getFileCachePath(string $key): string
    {
        $hash = md5($key);
        return $this->cacheDir . '/' . $hash . '.cache';
    }
    
    private function ensureCacheDir(): void
    {
        if (!is_dir($this->cacheDir)) {
            mkdir($this->cacheDir, 0755, true);
        }
    }
    
    private function acquireLock(string $key): void
    {
        $lockKey = "lock_$key";
        while (isset($this->locks[$lockKey])) {
            usleep(1000); // Wait 1ms
        }
        $this->locks[$lockKey] = true;
    }
    
    private function releaseLock(string $key): void
    {
        $lockKey = "lock_$key";
        unset($this->locks[$lockKey]);
    }
    
    private function initializeStats(): void
    {
        $this->hitStats = [
            'hits' => 0,
            'misses' => 0,
            'file_hits' => 0,
            'sets' => 0,
            'deletes' => 0,
            'clears' => 0,
            'evictions' => 0
        ];
    }
    
    private function updateStats(string $type): void
    {
        if (isset($this->hitStats[$type])) {
            $this->hitStats[$type]++;
        }
    }
} 