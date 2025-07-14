<?php
/**
 * 🚀 TuskLang Query Bridge - The Magic Connector
 * ==============================================
 * "Bridging configuration language with database intelligence"
 * 
 * This bridge connects TuskLang @ operators with the db-energy ORM system
 * Enables @Query(), @TuskObject(), @cache(), @learn() in .tsk files
 * 
 * FUJSEN Sprint - Hour 2 Implementation
 */

namespace TuskPHP\Utils;

use TuskPHP\TuskQuery;
use TuskPHP\TuskObject;
use PDO;
use SQLite3;

class TuskLangQueryBridge
{
    private static $instance = null;
    private $sqliteCache = null;
    private $dbConnection = null;
    private $cacheEnabled = true;
    private $memcached = null;
    private $memcachedEnabled = false;
    
    /**
     * Singleton pattern for bridge instance
     */
    public static function getInstance(): self
    {
        if (self::$instance === null) {
            self::$instance = new self();
        }
        return self::$instance;
    }
    
    /**
     * Initialize the bridge with SQLite cache
     */
    public function __construct()
    {
        $this->initializeSQLiteCache();
        $this->initializeMemcached();
    }
    
    /**
     * Initialize SQLite cache database for elephant memory
     */
    private function initializeSQLiteCache(): void
    {
        try {
            $cacheDir = '/var/lib/tusklang';
            if (!is_dir($cacheDir)) {
                mkdir($cacheDir, 0755, true);
            }
            
            $this->sqliteCache = new SQLite3($cacheDir . '/fujsen_cache.db');
            
            // Create cache table
            $this->sqliteCache->exec('
                CREATE TABLE IF NOT EXISTS tusk_cache (
                    key TEXT PRIMARY KEY,
                    value TEXT,
                    expires INTEGER,
                    dependencies TEXT,
                    hit_count INTEGER DEFAULT 0,
                    last_accessed INTEGER,
                    created_at INTEGER DEFAULT ' . time() . '
                )
            ');
            
            // Create metrics table
            $this->sqliteCache->exec('
                CREATE TABLE IF NOT EXISTS tusk_metrics (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    metric_name TEXT,
                    value REAL,
                    timestamp INTEGER DEFAULT ' . time() . ',
                    context TEXT
                )
            ');
            
            // Create learning table
            $this->sqliteCache->exec('
                CREATE TABLE IF NOT EXISTS tusk_learning (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    pattern_name TEXT,
                    input_data TEXT,
                    output_value TEXT,
                    confidence REAL DEFAULT 1.0,
                    created_at INTEGER DEFAULT ' . time() . '
                )
            ');
            
        } catch (\Exception $e) {
            error_log("TuskLang SQLite Cache Error: " . $e->getMessage());
            $this->cacheEnabled = false;
        }
    }
    
    /**
     * Initialize Memcached for distributed caching
     */
    private function initializeMemcached(): void
    {
        try {
            // Check if Memcached extension is available
            if (!extension_loaded('memcached')) {
                error_log("TuskLang Memcached: Extension not loaded");
                return;
            }
            
            $this->memcached = new \Memcached();
            $this->memcached->addServer('localhost', 11211);
            
            // Test connection
            if ($this->memcached->getStats()) {
                $this->memcachedEnabled = true;
                error_log("TuskLang Memcached: Connected successfully");
            } else {
                error_log("TuskLang Memcached: Connection failed");
            }
            
        } catch (\Exception $e) {
            error_log("TuskLang Memcached Error: " . $e->getMessage());
        }
    }
    
    // ========================================
    // @ OPERATOR HANDLERS
    // ========================================
    
    /**
     * Handle @Query() operator
     * Usage: @Query("users").equalTo("active", true).findAll()
     */
    public function handleQuery(string $className, string $queryChain): mixed
    {
        try {
            // Create TuskQuery instance
            $query = new TuskQuery($className);
            
            // Parse and execute query chain
            $result = $this->executeQueryChain($query, $queryChain);
            
            // Convert TuskObject results to arrays for .tsk compatibility
            if (is_array($result)) {
                return array_map(function($item) {
                    if ($item instanceof TuskObject) {
                        return $item->toArray();
                    }
                    return $item;
                }, $result);
            }
            
            // Handle different result types
            if ($result instanceof TuskObject) {
                return $result->toArray();
            }
            
            // Return primitives (int, string, etc.) as-is
            return $result;
            
        } catch (\Exception $e) {
            error_log("TuskLang @Query Error: " . $e->getMessage());
            return [];
        }
    }
    
    /**
     * Handle @TuskObject() operator
     * Usage: @TuskObject("User", user_id).get("name")
     */
    public function handleTuskObject(string $className, mixed $objectId = null, ?string $operation = null): mixed
    {
        try {
            $object = new TuskObject($className, $objectId);
            
            if ($operation) {
                // Parse operation like .get("field") or .save()
                if (preg_match('/^\.([a-zA-Z]+)\(([^)]*)\)$/', $operation, $matches)) {
                    $method = $matches[1];
                    $params = $this->parseOperationParams($matches[2]);
                    
                    if (method_exists($object, $method)) {
                        return call_user_func_array([$object, $method], $params);
                    }
                }
            }
            
            return $object->toArray();
            
        } catch (\Exception $e) {
            error_log("TuskLang @TuskObject Error: " . $e->getMessage());
            return null;
        }
    }
    
    /**
     * Handle @cache() operator
     * Usage: @cache("1h", expensive_operation())
     */
    public function handleCache(string $ttl, mixed $value, ?string $key = null): mixed
    {
        if (!$this->cacheEnabled) {
            return $value;
        }
        
        try {
            // Generate cache key if not provided
            if ($key === null) {
                $key = 'auto_' . md5(serialize($value));
            }
            
            // Check if cached value exists and is valid
            $cached = $this->getCachedValue($key);
            if ($cached !== null) {
                $this->updateCacheHit($key);
                return $cached;
            }
            
            // Cache the new value
            $expires = $this->parseTTL($ttl);
            $this->setCachedValue($key, $value, $expires);
            
            return $value;
            
        } catch (\Exception $e) {
            error_log("TuskLang @cache Error: " . $e->getMessage());
            return $value;
        }
    }
    
    /**
     * Handle @metrics() operator
     * Usage: @metrics.cpu.current() or @metrics("response_time", 150)
     */
    public function handleMetrics(?string $metric = null, mixed $value = null): mixed
    {
        if (!$this->cacheEnabled) {
            return null;
        }
        
        try {
            if ($value !== null) {
                // Store metric
                $stmt = $this->sqliteCache->prepare('
                    INSERT INTO tusk_metrics (metric_name, value, timestamp) 
                    VALUES (?, ?, ?)
                ');
                $stmt->bindValue(1, $metric);
                $stmt->bindValue(2, (float)$value);
                $stmt->bindValue(3, time());
                $stmt->execute();
                
                return $value;
            } else {
                // Retrieve metric
                return $this->getMetricValue($metric);
            }
            
        } catch (\Exception $e) {
            error_log("TuskLang @metrics Error: " . $e->getMessage());
            return null;
        }
    }
    
    /**
     * Handle @learn() operator
     * Usage: @learn("optimal_workers", {metrics: ["cpu", "memory"]}) or @learn("pattern", array_value)
     */
    public function handleLearn(string $pattern, mixed $config = []): mixed
    {
        if (!$this->cacheEnabled) {
            return null;
        }
        
        try {
            // Simple learning: return average of recent values
            $stmt = $this->sqliteCache->prepare('
                SELECT AVG(CAST(output_value AS REAL)) as avg_value 
                FROM tusk_learning 
                WHERE pattern_name = ? 
                AND created_at > ?
            ');
            $stmt->bindValue(1, $pattern);
            $stmt->bindValue(2, time() - 86400); // Last 24 hours
            $result = $stmt->execute();
            
            $row = $result->fetchArray(SQLITE3_ASSOC);
            
            if ($row && $row['avg_value'] !== null) {
                return (float)$row['avg_value'];
            }
            
            // Return default or fallback value
            if (is_array($config)) {
                return $config['default'] ?? count($config);
            } else {
                return is_numeric($config) ? (float)$config : 0;
            }
            
        } catch (\Exception $e) {
            error_log("TuskLang @learn Error: " . $e->getMessage());
            if (is_array($config)) {
                return $config['default'] ?? count($config);
            } else {
                return is_numeric($config) ? (float)$config : 0;
            }
        }
    }
    
    /**
     * Handle @optimize() operator
     * Usage: @optimize("cache_size", {target: 0.85, current: 0.75}) or @optimize("connections", array_value)
     */
    public function handleOptimize(string $parameter, mixed $config = []): mixed
    {
        try {
            // If config is not an array, treat it as the current value
            if (!is_array($config)) {
                $current = is_numeric($config) ? (float)$config : (is_array($config) ? count($config) : 0);
                $target = $current * 1.2; // Default target is 20% higher
            } else {
                $current = $config['current'] ?? 0;
                $target = $config['target'] ?? 1;
                
                // Handle array values by using their count
                if (is_array($current)) {
                    $current = count($current);
                }
                if (is_array($target)) {
                    $target = count($target);
                }
            }
            
            // Ensure we have numeric values
            $current = (float)$current;
            $target = (float)$target;
            
            // Simple optimization: adjust toward target
            if ($current < $target) {
                return $current * 1.1; // Increase by 10%
            } elseif ($current > $target) {
                return $current * 0.9; // Decrease by 10%
            }
            
            return $current;
            
        } catch (\Exception $e) {
            error_log("TuskLang @optimize Error: " . $e->getMessage());
            return is_array($config) ? ($config['current'] ?? 0) : $config;
        }
    }
    
    // ========================================
    // HELPER METHODS
    // ========================================
    
    /**
     * Execute query chain like .equalTo("active", true).findAll()
     */
    private function executeQueryChain(TuskQuery $query, string $chain): mixed
    {
        // Remove leading dot if present
        $chain = ltrim($chain, '.');
        
        // Split chain into method calls
        $methods = [];
        preg_match_all('/([a-zA-Z]+)\(([^)]*)\)/', $chain, $matches, PREG_SET_ORDER);
        
        $lastMethod = '';
        foreach ($matches as $match) {
            $method = $match[1];
            $params = $this->parseOperationParams($match[2]);
            $lastMethod = $method;
            
            if (method_exists($query, $method)) {
                $result = call_user_func_array([$query, $method], $params);
                
                // If this is a terminating method (not a query builder method), return the result
                if (in_array($method, ['find', 'findAll', 'first', 'count', 'distinct'])) {
                    return $result;
                }
                
                // Otherwise, continue building the query
                if ($result instanceof TuskQuery) {
                    $query = $result;
                }
            }
        }
        
        // If no terminating method was called, return empty array
        // This handles cases where the chain doesn't end with find/count/etc
        return [];
    }
    
    /**
     * Parse operation parameters from string
     */
    private function parseOperationParams(string $params): array
    {
        if (empty(trim($params))) {
            return [];
        }
        
        $result = [];
        $parts = explode(',', $params);
        
        foreach ($parts as $part) {
            $part = trim($part);
            
            // String parameter
            if (preg_match('/^"([^"]*)"$/', $part, $matches)) {
                $result[] = $matches[1];
            }
            // Number parameter
            elseif (is_numeric($part)) {
                $result[] = strpos($part, '.') !== false ? (float)$part : (int)$part;
            }
            // Boolean parameter
            elseif ($part === 'true') {
                $result[] = true;
            }
            elseif ($part === 'false') {
                $result[] = false;
            }
            // Null parameter
            elseif ($part === 'null') {
                $result[] = null;
            }
            else {
                $result[] = $part;
            }
        }
        
        return $result;
    }
    
    /**
     * Parse TTL string to timestamp
     */
    private function parseTTL(string $ttl): int
    {
        $multipliers = [
            's' => 1,
            'm' => 60,
            'h' => 3600,
            'd' => 86400,
            'w' => 604800
        ];
        
        if (preg_match('/^(\d+)([smhdw])$/', $ttl, $matches)) {
            $value = (int)$matches[1];
            $unit = $matches[2];
            return time() + ($value * $multipliers[$unit]);
        }
        
        // Default to 1 hour
        return time() + 3600;
    }
    
    /**
     * Get cached value if valid
     */
    private function getCachedValue(string $key): mixed
    {
        // Try Memcached first if available
        if ($this->memcachedEnabled && $this->memcached) {
            try {
                $value = $this->memcached->get($key);
                if ($value !== false) {
                    return $value;
                }
            } catch (\Exception $e) {
                error_log("TuskLang Memcached Get Error: " . $e->getMessage());
            }
        }
        
        // Fallback to SQLite
        if ($this->sqliteCache) {
            $stmt = $this->sqliteCache->prepare('
                SELECT value, expires FROM tusk_cache 
                WHERE key = ? AND expires > ?
            ');
            $stmt->bindValue(1, $key);
            $stmt->bindValue(2, time());
            $result = $stmt->execute();
            
            $row = $result->fetchArray(SQLITE3_ASSOC);
            
            if ($row) {
                return unserialize($row['value']);
            }
        }
        
        return null;
    }
    
    /**
     * Set cached value
     */
    private function setCachedValue(string $key, mixed $value, int $expires): void
    {
        // Try Memcached first if available
        if ($this->memcachedEnabled && $this->memcached) {
            try {
                $ttl = $expires - time();
                if ($ttl > 0) {
                    $this->memcached->set($key, $value, $ttl);
                    return;
                }
            } catch (\Exception $e) {
                error_log("TuskLang Memcached Set Error: " . $e->getMessage());
            }
        }
        
        // Fallback to SQLite
        if ($this->sqliteCache) {
            $stmt = $this->sqliteCache->prepare('
                INSERT OR REPLACE INTO tusk_cache 
                (key, value, expires, last_accessed) 
                VALUES (?, ?, ?, ?)
            ');
            $stmt->bindValue(1, $key);
            $stmt->bindValue(2, serialize($value));
            $stmt->bindValue(3, $expires);
            $stmt->bindValue(4, time());
            $stmt->execute();
        }
    }
    
    /**
     * Update cache hit count
     */
    private function updateCacheHit(string $key): void
    {
        // For Memcached, we don't track hit counts (it's handled internally)
        // Only update SQLite hit counts for analytics
        if ($this->sqliteCache) {
            $stmt = $this->sqliteCache->prepare('
                UPDATE tusk_cache 
                SET hit_count = hit_count + 1, last_accessed = ? 
                WHERE key = ?
            ');
            $stmt->bindValue(1, time());
            $stmt->bindValue(2, $key);
            $stmt->execute();
        }
    }
    
    /**
     * Get metric value
     */
    private function getMetricValue(string $metric): mixed
    {
        $stmt = $this->sqliteCache->prepare('
            SELECT value FROM tusk_metrics 
            WHERE metric_name = ? 
            ORDER BY timestamp DESC 
            LIMIT 1
        ');
        $stmt->bindValue(1, $metric);
        $result = $stmt->execute();
        
        $row = $result->fetchArray(SQLITE3_ASSOC);
        
        return $row ? (float)$row['value'] : null;
    }
    
    /**
     * Get cache statistics for both Memcached and SQLite
     */
    public function getCacheStats(): array
    {
        $stats = [
            'memcached' => [
                'enabled' => $this->memcachedEnabled,
                'connected' => false,
                'stats' => null
            ],
            'sqlite' => [
                'enabled' => $this->cacheEnabled,
                'connected' => false,
                'stats' => null
            ]
        ];
        
        // Memcached stats
        if ($this->memcachedEnabled && $this->memcached) {
            try {
                $memcachedStats = $this->memcached->getStats();
                if ($memcachedStats) {
                    $stats['memcached']['connected'] = true;
                    $stats['memcached']['stats'] = $memcachedStats;
                }
            } catch (\Exception $e) {
                error_log("TuskLang Memcached Stats Error: " . $e->getMessage());
            }
        }
        
        // SQLite stats
        if ($this->cacheEnabled && $this->sqliteCache) {
            try {
                $result = $this->sqliteCache->query('
                    SELECT 
                        COUNT(*) as total_keys,
                        SUM(hit_count) as total_hits,
                        AVG(hit_count) as avg_hits,
                        COUNT(CASE WHEN expires > ' . time() . ' THEN 1 END) as valid_keys
                    FROM tusk_cache
                ');
                
                $row = $result->fetchArray(SQLITE3_ASSOC);
                if ($row) {
                    $stats['sqlite']['connected'] = true;
                    $stats['sqlite']['stats'] = $row;
                }
            } catch (\Exception $e) {
                error_log("TuskLang SQLite Stats Error: " . $e->getMessage());
            }
        }
        
        return $stats;
    }
    
    /**
     * Check if Memcached is available and working
     */
    public function isMemcachedAvailable(): bool
    {
        return $this->memcachedEnabled && $this->memcached && $this->memcached->getStats();
    }
} 