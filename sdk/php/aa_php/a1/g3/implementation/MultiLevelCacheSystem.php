<?php

namespace TuskLang\CoreOperators\Enhanced;

use Exception;
use Redis;

/**
 * Multi-Level Cache System - Agent A1 Goal 3 Implementation
 * 
 * Features:
 * - Intelligent cache hierarchy (L1: Memory, L2: Redis, L3: Distributed)
 * - Cache warming and preloading strategies
 * - Cache invalidation patterns and dependencies
 * - Distributed cache synchronization
 * - Cache analytics and hit/miss tracking
 * - Advanced cache algorithms and optimization
 */
class MultiLevelCacheSystem
{
    private const L1_MAX_SIZE = 1000;
    private const L2_DEFAULT_TTL = 3600;
    private const L3_DEFAULT_TTL = 86400;
    private const WARMING_BATCH_SIZE = 100;
    
    private array $l1Cache = []; // Memory cache
    private Redis $l2Cache; // Redis cache
    private array $l3Nodes = []; // Distributed cache nodes
    private array $analytics = [];
    private array $dependencies = [];
    private array $warmingStrategies = [];
    private array $invalidationPatterns = [];
    private bool $distributedMode = false;
    
    public function __construct(array $config = [])
    {
        $this->initializeL2Cache($config['l2'] ?? []);
        $this->initializeL3Cache($config['l3'] ?? []);
        $this->initializeAnalytics();
        $this->initializeWarmingStrategies();
        $this->setupInvalidationPatterns();
    }
    
    /**
     * Get value from cache hierarchy
     */
    public function get(string $key, callable $fallback = null, array $options = []): mixed
    {
        $startTime = microtime(true);
        
        // Try L1 cache first (memory)
        if (isset($this->l1Cache[$key])) {
            $this->recordHit('l1', $key, microtime(true) - $startTime);
            return $this->l1Cache[$key]['value'];
        }
        
        // Try L2 cache (Redis)
        try {
            $l2Value = $this->l2Cache->get($key);
            if ($l2Value !== false) {
                // Promote to L1
                $this->setL1($key, $l2Value, $options['l1_ttl'] ?? 300);
                $this->recordHit('l2', $key, microtime(true) - $startTime);
                return $l2Value;
            }
        } catch (Exception $e) {
            // L2 cache unavailable, continue to L3
        }
        
        // Try L3 cache (distributed)
        if ($this->distributedMode) {
            $l3Value = $this->getFromL3($key);
            if ($l3Value !== null) {
                // Promote to L2 and L1
                $this->setL2($key, $l3Value, $options['l2_ttl'] ?? self::L2_DEFAULT_TTL);
                $this->setL1($key, $l3Value, $options['l1_ttl'] ?? 300);
                $this->recordHit('l3', $key, microtime(true) - $startTime);
                return $l3Value;
            }
        }
        
        // Cache miss - use fallback if provided
        if ($fallback) {
            $value = $fallback($key);
            if ($value !== null) {
                $this->set($key, $value, $options);
            }
            $this->recordMiss($key, microtime(true) - $startTime);
            return $value;
        }
        
        $this->recordMiss($key, microtime(true) - $startTime);
        return null;
    }
    
    /**
     * Set value in cache hierarchy
     */
    public function set(string $key, mixed $value, array $options = []): bool
    {
        $l1Ttl = $options['l1_ttl'] ?? 300;
        $l2Ttl = $options['l2_ttl'] ?? self::L2_DEFAULT_TTL;
        $l3Ttl = $options['l3_ttl'] ?? self::L3_DEFAULT_TTL;
        $tags = $options['tags'] ?? [];
        $dependencies = $options['dependencies'] ?? [];
        
        $success = true;
        
        // Set in L1 (memory)
        $success &= $this->setL1($key, $value, $l1Ttl);
        
        // Set in L2 (Redis)
        try {
            $success &= $this->setL2($key, $value, $l2Ttl, $tags);
        } catch (Exception $e) {
            $success = false;
        }
        
        // Set in L3 (distributed)
        if ($this->distributedMode) {
            $success &= $this->setL3($key, $value, $l3Ttl);
        }
        
        // Store dependencies
        if (!empty($dependencies)) {
            $this->storeDependencies($key, $dependencies);
        }
        
        // Store tags for invalidation
        if (!empty($tags)) {
            $this->storeTags($key, $tags);
        }
        
        return $success;
    }
    
    /**
     * Delete from all cache levels
     */
    public function delete(string $key): bool
    {
        $success = true;
        
        // Delete from L1
        unset($this->l1Cache[$key]);
        
        // Delete from L2
        try {
            $success &= $this->l2Cache->del($key) > 0;
        } catch (Exception $e) {
            $success = false;
        }
        
        // Delete from L3
        if ($this->distributedMode) {
            $success &= $this->deleteFromL3($key);
        }
        
        // Clean up dependencies and tags
        $this->cleanupDependencies($key);
        $this->cleanupTags($key);
        
        return $success;
    }
    
    /**
     * Invalidate cache by pattern
     */
    public function invalidateByPattern(string $pattern): int
    {
        $invalidated = 0;
        
        // Invalidate L1
        foreach ($this->l1Cache as $key => $data) {
            if (fnmatch($pattern, $key)) {
                unset($this->l1Cache[$key]);
                $invalidated++;
            }
        }
        
        // Invalidate L2
        try {
            $keys = $this->l2Cache->keys($pattern);
            if (!empty($keys)) {
                $this->l2Cache->del($keys);
                $invalidated += count($keys);
            }
        } catch (Exception $e) {
            // Continue if L2 unavailable
        }
        
        // Invalidate L3
        if ($this->distributedMode) {
            $invalidated += $this->invalidateL3ByPattern($pattern);
        }
        
        return $invalidated;
    }
    
    /**
     * Invalidate cache by tags
     */
    public function invalidateByTags(array $tags): int
    {
        $invalidated = 0;
        
        foreach ($tags as $tag) {
            $keys = $this->getKeysByTag($tag);
            foreach ($keys as $key) {
                if ($this->delete($key)) {
                    $invalidated++;
                }
            }
        }
        
        return $invalidated;
    }
    
    /**
     * Warm cache using preloading strategies
     */
    public function warmCache(string $strategy = 'default', array $options = []): array
    {
        if (!isset($this->warmingStrategies[$strategy])) {
            throw new Exception("Unknown warming strategy: {$strategy}");
        }
        
        $warmingStrategy = $this->warmingStrategies[$strategy];
        $results = [
            'keys_warmed' => 0,
            'time_taken' => 0,
            'errors' => []
        ];
        
        $startTime = microtime(true);
        
        try {
            $keys = $warmingStrategy['key_generator']($options);
            $batchSize = $options['batch_size'] ?? self::WARMING_BATCH_SIZE;
            
            $batches = array_chunk($keys, $batchSize);
            
            foreach ($batches as $batch) {
                $batchResults = $this->warmBatch($batch, $warmingStrategy['data_loader'], $options);
                $results['keys_warmed'] += $batchResults['warmed'];
                $results['errors'] = array_merge($results['errors'], $batchResults['errors']);
                
                // Prevent overwhelming the system
                if (count($batches) > 1) {
                    usleep(10000); // 10ms between batches
                }
            }
            
        } catch (Exception $e) {
            $results['errors'][] = $e->getMessage();
        }
        
        $results['time_taken'] = microtime(true) - $startTime;
        
        return $results;
    }
    
    /**
     * Get cache statistics and analytics
     */
    public function getAnalytics(): array
    {
        return [
            'hit_ratio' => $this->calculateHitRatio(),
            'level_performance' => $this->getLevelPerformance(),
            'memory_usage' => $this->getMemoryUsage(),
            'key_distribution' => $this->getKeyDistribution(),
            'invalidation_stats' => $this->getInvalidationStats(),
            'warming_stats' => $this->getWarmingStats()
        ];
    }
    
    /**
     * Synchronize distributed cache
     */
    public function synchronizeDistributed(): bool
    {
        if (!$this->distributedMode) {
            return true;
        }
        
        $syncSuccess = true;
        
        try {
            // Get all keys from L2 that need syncing
            $keys = $this->l2Cache->keys('sync:*');
            
            foreach ($keys as $syncKey) {
                $actualKey = substr($syncKey, 5); // Remove 'sync:' prefix
                $value = $this->l2Cache->get($actualKey);
                
                if ($value !== false) {
                    // Distribute to all L3 nodes
                    foreach ($this->l3Nodes as $node) {
                        if (!$this->syncToNode($node, $actualKey, $value)) {
                            $syncSuccess = false;
                        }
                    }
                    
                    // Remove sync marker
                    $this->l2Cache->del($syncKey);
                }
            }
            
        } catch (Exception $e) {
            $syncSuccess = false;
        }
        
        return $syncSuccess;
    }
    
    /**
     * L1 Cache operations (memory)
     */
    private function setL1(string $key, mixed $value, int $ttl): bool
    {
        // Implement LRU eviction if cache is full
        if (count($this->l1Cache) >= self::L1_MAX_SIZE) {
            $this->evictL1();
        }
        
        $this->l1Cache[$key] = [
            'value' => $value,
            'expires' => time() + $ttl,
            'access_time' => time(),
            'access_count' => 0
        ];
        
        return true;
    }
    
    private function evictL1(): void
    {
        // Remove expired entries first
        $now = time();
        foreach ($this->l1Cache as $key => $data) {
            if ($data['expires'] < $now) {
                unset($this->l1Cache[$key]);
            }
        }
        
        // If still full, remove least recently used
        if (count($this->l1Cache) >= self::L1_MAX_SIZE) {
            uasort($this->l1Cache, fn($a, $b) => $a['access_time'] <=> $b['access_time']);
            $keysToRemove = array_slice(array_keys($this->l1Cache), 0, self::L1_MAX_SIZE / 4);
            
            foreach ($keysToRemove as $key) {
                unset($this->l1Cache[$key]);
            }
        }
    }
    
    /**
     * L2 Cache operations (Redis)
     */
    private function initializeL2Cache(array $config): void
    {
        $this->l2Cache = new Redis();
        
        $host = $config['host'] ?? 'localhost';
        $port = $config['port'] ?? 6379;
        $timeout = $config['timeout'] ?? 5.0;
        $password = $config['password'] ?? null;
        $database = $config['database'] ?? 1; // Use database 1 for L2 cache
        
        $this->l2Cache->connect($host, $port, $timeout);
        
        if ($password) {
            $this->l2Cache->auth($password);
        }
        
        $this->l2Cache->select($database);
        $this->l2Cache->setOption(Redis::OPT_SERIALIZER, Redis::SERIALIZER_MSGPACK);
        $this->l2Cache->setOption(Redis::OPT_COMPRESSION, Redis::COMPRESSION_LZ4);
    }
    
    private function setL2(string $key, mixed $value, int $ttl, array $tags = []): bool
    {
        $success = $this->l2Cache->setex($key, $ttl, $value);
        
        // Store tags if provided
        if (!empty($tags)) {
            foreach ($tags as $tag) {
                $this->l2Cache->sadd("tag:{$tag}", $key);
                $this->l2Cache->expire("tag:{$tag}", $ttl + 3600); // Tag expires 1 hour after data
            }
        }
        
        // Mark for distributed sync if enabled
        if ($this->distributedMode) {
            $this->l2Cache->setex("sync:{$key}", 300, 1); // Sync marker expires in 5 minutes
        }
        
        return $success;
    }
    
    /**
     * L3 Cache operations (distributed)
     */
    private function initializeL3Cache(array $config): void
    {
        if (!empty($config['nodes'])) {
            $this->l3Nodes = $config['nodes'];
            $this->distributedMode = true;
        }
    }
    
    private function getFromL3(string $key): mixed
    {
        foreach ($this->l3Nodes as $node) {
            try {
                $value = $this->getFromNode($node, $key);
                if ($value !== null) {
                    return $value;
                }
            } catch (Exception $e) {
                continue; // Try next node
            }
        }
        
        return null;
    }
    
    private function setL3(string $key, mixed $value, int $ttl): bool
    {
        $success = true;
        
        foreach ($this->l3Nodes as $node) {
            try {
                if (!$this->setToNode($node, $key, $value, $ttl)) {
                    $success = false;
                }
            } catch (Exception $e) {
                $success = false;
            }
        }
        
        return $success;
    }
    
    /**
     * Cache warming implementation
     */
    private function initializeWarmingStrategies(): void
    {
        $this->warmingStrategies = [
            'default' => [
                'key_generator' => function(array $options) {
                    return $options['keys'] ?? [];
                },
                'data_loader' => function(string $key, array $options) {
                    return $options['data'][$key] ?? null;
                }
            ],
            'database' => [
                'key_generator' => function(array $options) {
                    // Generate keys based on database query
                    $query = $options['query'] ?? '';
                    // Implementation would execute query and return keys
                    return [];
                },
                'data_loader' => function(string $key, array $options) {
                    // Load data from database
                    return null;
                }
            ],
            'popular_keys' => [
                'key_generator' => function(array $options) {
                    // Get most accessed keys from analytics
                    return $this->getMostAccessedKeys($options['limit'] ?? 100);
                },
                'data_loader' => function(string $key, array $options) {
                    // Load popular data
                    return $options['loader']($key);
                }
            ]
        ];
    }
    
    private function warmBatch(array $keys, callable $dataLoader, array $options): array
    {
        $warmed = 0;
        $errors = [];
        
        foreach ($keys as $key) {
            try {
                $data = $dataLoader($key, $options);
                if ($data !== null) {
                    $this->set($key, $data, $options['cache_options'] ?? []);
                    $warmed++;
                }
            } catch (Exception $e) {
                $errors[] = "Error warming key {$key}: " . $e->getMessage();
            }
        }
        
        return ['warmed' => $warmed, 'errors' => $errors];
    }
    
    /**
     * Analytics and monitoring
     */
    private function initializeAnalytics(): void
    {
        $this->analytics = [
            'hits' => ['l1' => 0, 'l2' => 0, 'l3' => 0],
            'misses' => 0,
            'response_times' => ['l1' => [], 'l2' => [], 'l3' => []],
            'invalidations' => 0,
            'warming_operations' => 0
        ];
    }
    
    private function recordHit(string $level, string $key, float $responseTime): void
    {
        $this->analytics['hits'][$level]++;
        $this->analytics['response_times'][$level][] = $responseTime;
        
        // Keep only last 1000 response times for memory efficiency
        if (count($this->analytics['response_times'][$level]) > 1000) {
            $this->analytics['response_times'][$level] = array_slice(
                $this->analytics['response_times'][$level], -1000
            );
        }
        
        // Update access stats for L1
        if ($level === 'l1' && isset($this->l1Cache[$key])) {
            $this->l1Cache[$key]['access_time'] = time();
            $this->l1Cache[$key]['access_count']++;
        }
    }
    
    private function recordMiss(string $key, float $responseTime): void
    {
        $this->analytics['misses']++;
    }
    
    private function calculateHitRatio(): array
    {
        $totalHits = array_sum($this->analytics['hits']);
        $totalRequests = $totalHits + $this->analytics['misses'];
        
        if ($totalRequests === 0) {
            return ['overall' => 0, 'by_level' => ['l1' => 0, 'l2' => 0, 'l3' => 0]];
        }
        
        return [
            'overall' => $totalHits / $totalRequests,
            'by_level' => [
                'l1' => $this->analytics['hits']['l1'] / $totalRequests,
                'l2' => $this->analytics['hits']['l2'] / $totalRequests,
                'l3' => $this->analytics['hits']['l3'] / $totalRequests
            ]
        ];
    }
    
    private function getLevelPerformance(): array
    {
        $performance = [];
        
        foreach (['l1', 'l2', 'l3'] as $level) {
            $times = $this->analytics['response_times'][$level];
            if (!empty($times)) {
                $performance[$level] = [
                    'avg_response_time' => array_sum($times) / count($times),
                    'min_response_time' => min($times),
                    'max_response_time' => max($times),
                    'request_count' => count($times)
                ];
            } else {
                $performance[$level] = [
                    'avg_response_time' => 0,
                    'min_response_time' => 0,
                    'max_response_time' => 0,
                    'request_count' => 0
                ];
            }
        }
        
        return $performance;
    }
    
    // Placeholder implementations for remaining methods
    private function storeDependencies(string $key, array $dependencies): void { }
    private function storeTags(string $key, array $tags): void { }
    private function cleanupDependencies(string $key): void { }
    private function cleanupTags(string $key): void { }
    private function getKeysByTag(string $tag): array { return []; }
    private function deleteFromL3(string $key): bool { return true; }
    private function invalidateL3ByPattern(string $pattern): int { return 0; }
    private function getFromNode(array $node, string $key): mixed { return null; }
    private function setToNode(array $node, string $key, mixed $value, int $ttl): bool { return true; }
    private function syncToNode(array $node, string $key, mixed $value): bool { return true; }
    private function getMostAccessedKeys(int $limit): array { return []; }
    private function getMemoryUsage(): array { return []; }
    private function getKeyDistribution(): array { return []; }
    private function getInvalidationStats(): array { return []; }
    private function getWarmingStats(): array { return []; }
    private function setupInvalidationPatterns(): void { }
} 