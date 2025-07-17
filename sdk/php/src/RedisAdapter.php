<?php
/**
 * Redis Adapter for TuskLang Enhanced
 * ==================================
 * Enables @query operations with Redis key-value store
 * 
 * DEFAULT CONFIG: peanut.tsk (the bridge of language grace)
 */

namespace TuskLang\Enhanced;

class RedisAdapter
{
    private array $config;
    private $redis = null;
    
    public function __construct(array $options = [])
    {
        $this->config = array_merge([
            'host' => 'localhost',
            'port' => 6379,
            'password' => null,
            'database' => 0,
            'timeout' => 5.0,
            'persistent' => false
        ], $options);
        
        // Check if Redis extension is available
        if (!extension_loaded('redis') && !class_exists('\Predis\Client')) {
            throw new \Exception('Redis adapter requires either php-redis extension or predis/predis package. Install with: composer require predis/predis');
        }
    }
    
    /**
     * Connect to Redis
     */
    public function connect(): void
    {
        if (!$this->redis) {
            if (extension_loaded('redis')) {
                // Use php-redis extension
                $this->redis = new \Redis();
                
                if ($this->config['persistent']) {
                    $this->redis->pconnect($this->config['host'], $this->config['port'], $this->config['timeout']);
                } else {
                    $this->redis->connect($this->config['host'], $this->config['port'], $this->config['timeout']);
                }
                
                if ($this->config['password']) {
                    $this->redis->auth($this->config['password']);
                }
                
                $this->redis->select($this->config['database']);
                
            } elseif (class_exists('\Predis\Client')) {
                // Use Predis client
                $this->redis = new \Predis\Client([
                    'scheme' => 'tcp',
                    'host' => $this->config['host'],
                    'port' => $this->config['port'],
                    'password' => $this->config['password'],
                    'database' => $this->config['database'],
                    'timeout' => $this->config['timeout']
                ]);
                
            } else {
                throw new \Exception('No Redis client available');
            }
        }
    }
    
    /**
     * Execute a Redis query
     * Redis uses a special query syntax for TuskLang:
     * @query("get", "user:count")
     * @query("hget", "settings", "max_users")
     * @query("smembers", "features:enabled")
     * @query("zrange", "leaderboard", 0, 10)
     */
    public function query(string $command, ...$args): mixed
    {
        $this->connect();
        
        $cmd = strtolower($command);
        
        try {
            // Handle special TuskLang commands
            switch ($cmd) {
                // Count operations
                case 'count':
                    $pattern = $args[0] ?? '*';
                    $keys = $this->redis->keys($pattern);
                    return is_array($keys) ? count($keys) : 0;
                    
                // Sum numeric values
                case 'sum':
                    $pattern = $args[0] ?? '*';
                    $keys = $this->redis->keys($pattern);
                    $sum = 0;
                    if (is_array($keys)) {
                        foreach ($keys as $key) {
                            $val = $this->redis->get($key);
                            if (is_numeric($val)) {
                                $sum += floatval($val);
                            }
                        }
                    }
                    return $sum;
                    
                // Get all matching keys and values
                case 'getall':
                    $pattern = $args[0] ?? '*';
                    $keys = $this->redis->keys($pattern);
                    $result = [];
                    if (is_array($keys)) {
                        foreach ($keys as $key) {
                            $result[$key] = $this->redis->get($key);
                        }
                    }
                    return $result;
                    
                // Check if key exists
                case 'exists':
                    return (bool) $this->redis->exists($args[0] ?? '');
                    
                // Standard Redis commands
                case 'get':
                    $result = $this->redis->get($args[0] ?? '');
                    return $this->convertRedisResult($result);
                    
                case 'set':
                    return $this->redis->set($args[0] ?? '', $args[1] ?? '');
                    
                case 'hget':
                    $result = $this->redis->hget($args[0] ?? '', $args[1] ?? '');
                    return $this->convertRedisResult($result);
                    
                case 'hgetall':
                    $result = $this->redis->hgetall($args[0] ?? '');
                    return is_array($result) ? $result : [];
                    
                case 'hset':
                    return $this->redis->hset($args[0] ?? '', $args[1] ?? '', $args[2] ?? '');
                    
                case 'smembers':
                    $result = $this->redis->smembers($args[0] ?? '');
                    return is_array($result) ? $result : [];
                    
                case 'sadd':
                    return $this->redis->sadd($args[0] ?? '', ...$args[1] ?? []);
                    
                case 'zrange':
                    $start = $args[1] ?? 0;
                    $stop = $args[2] ?? -1;
                    $withScores = $args[3] ?? false;
                    $result = $this->redis->zrange($args[0] ?? '', $start, $stop, $withScores);
                    return is_array($result) ? $result : [];
                    
                case 'zadd':
                    $key = $args[0] ?? '';
                    $score = $args[1] ?? 0;
                    $member = $args[2] ?? '';
                    return $this->redis->zadd($key, $score, $member);
                    
                case 'lrange':
                    $start = $args[1] ?? 0;
                    $stop = $args[2] ?? -1;
                    $result = $this->redis->lrange($args[0] ?? '', $start, $stop);
                    return is_array($result) ? $result : [];
                    
                case 'lpush':
                    return $this->redis->lpush($args[0] ?? '', ...$args[1] ?? []);
                    
                case 'rpush':
                    return $this->redis->rpush($args[0] ?? '', ...$args[1] ?? []);
                    
                case 'keys':
                    $pattern = $args[0] ?? '*';
                    $result = $this->redis->keys($pattern);
                    return is_array($result) ? $result : [];
                    
                default:
                    // Try to call the method directly
                    if (method_exists($this->redis, $cmd)) {
                        $result = call_user_func_array([$this->redis, $cmd], $args);
                        return $this->convertRedisResult($result);
                    }
                    
                    throw new \Exception("Unknown Redis command: $command");
            }
            
        } catch (\Exception $e) {
            throw new \Exception("Redis query error: " . $e->getMessage());
        }
    }
    
    /**
     * Convert Redis result to appropriate PHP type
     */
    private function convertRedisResult(mixed $result): mixed
    {
        if ($result === false || $result === null) {
            return null;
        }
        
        if ($result === 'true') {
            return true;
        }
        
        if ($result === 'false') {
            return false;
        }
        
        if (is_string($result) && is_numeric($result)) {
            return str_contains($result, '.') ? floatval($result) : intval($result);
        }
        
        return $result;
    }
    
    /**
     * Create test data for Redis
     */
    public function createTestData(): void
    {
        $this->connect();
        
        // Clear existing test data
        $testKeys = $this->redis->keys('test:*');
        if (!empty($testKeys)) {
            $this->redis->del($testKeys);
        }
        
        // Settings (strings)
        $this->redis->set('settings:max_users', '1000');
        $this->redis->set('settings:app_name', 'TuskLang PHP Demo');
        $this->redis->set('settings:maintenance_mode', 'false');
        
        // User counters
        $this->redis->set('stats:user_count', '3');
        $this->redis->set('stats:active_users', '2');
        
        // Feature flags (set)
        $this->redis->sadd('features:enabled', 'new_dashboard', 'dark_mode');
        $this->redis->sadd('features:disabled', 'api_v2');
        
        // Plans (hash)
        $this->redis->hset('plan:basic', 'rate_limit', '100');
        $this->redis->hset('plan:basic', 'price', '9.99');
        
        $this->redis->hset('plan:premium', 'rate_limit', '1000');
        $this->redis->hset('plan:premium', 'price', '29.99');
        
        $this->redis->hset('plan:enterprise', 'rate_limit', '10000');
        $this->redis->hset('plan:enterprise', 'price', '99.99');
        
        // Users (hash)
        $this->redis->hset('user:1', 'name', 'John Doe');
        $this->redis->hset('user:1', 'email', 'john@example.com');
        $this->redis->hset('user:1', 'active', 'true');
        
        $this->redis->hset('user:2', 'name', 'Jane Smith');
        $this->redis->hset('user:2', 'email', 'jane@example.com');
        $this->redis->hset('user:2', 'active', 'true');
        
        $this->redis->hset('user:3', 'name', 'Bob Wilson');
        $this->redis->hset('user:3', 'email', 'bob@example.com');
        $this->redis->hset('user:3', 'active', 'false');
        
        // Leaderboard (sorted set)
        $this->redis->zadd('leaderboard', 100, 'John');
        $this->redis->zadd('leaderboard', 85, 'Jane');
        $this->redis->zadd('leaderboard', 92, 'Bob');
        
        // Lists
        $this->redis->lpush('queue:tasks', 'task1', 'task2', 'task3');
        $this->redis->lpush('recent:activities', 'login', 'view_dashboard', 'update_profile');
        
        // Expiring keys
        $this->redis->setex('cache:homepage', 300, 'cached_content'); // 5 minutes
        
        echo "Redis test data created successfully\n";
    }
    
    /**
     * Check if connected
     */
    public function isConnected(): bool
    {
        try {
            $this->connect();
            $pong = $this->redis->ping();
            return $pong === 'PONG' || $pong === '+PONG';
        } catch (\Exception $e) {
            return false;
        }
    }
    
    /**
     * TuskLang specific helpers
     */
    public function getSettings(): array
    {
        $keys = $this->redis->keys('settings:*');
        $settings = [];
        
        if (is_array($keys)) {
            foreach ($keys as $key) {
                $name = str_replace('settings:', '', $key);
                $settings[$name] = $this->redis->get($key);
            }
        }
        
        return $settings;
    }
    
    public function getFeatures(): array
    {
        $enabled = $this->redis->smembers('features:enabled');
        $disabled = $this->redis->smembers('features:disabled');
        
        return [
            'enabled' => is_array($enabled) ? $enabled : [],
            'disabled' => is_array($disabled) ? $disabled : [],
            'all' => array_merge(
                is_array($enabled) ? $enabled : [],
                is_array($disabled) ? $disabled : []
            )
        ];
    }
    
    /**
     * Load configuration from peanut.tsk
     */
    public static function loadFromPeanut(): self
    {
        $parser = new TuskLangEnhanced();
        $parser->loadPeanut();
        
        $config = [];
        
        // Look for Redis configuration in peanut.tsk
        if ($parser->get('database.redis.host')) {
            $config['host'] = $parser->get('database.redis.host');
        }
        if ($parser->get('database.redis.port')) {
            $config['port'] = (int) $parser->get('database.redis.port');
        }
        if ($parser->get('database.redis.password')) {
            $config['password'] = $parser->get('database.redis.password');
        }
        if ($parser->get('database.redis.db')) {
            $config['database'] = (int) $parser->get('database.redis.db');
        }
        
        // Also check cache.redis configuration
        if ($parser->get('cache.redis.host')) {
            $config['host'] = $parser->get('cache.redis.host');
        }
        if ($parser->get('cache.redis.port')) {
            $config['port'] = (int) $parser->get('cache.redis.port');
        }
        
        if (empty($config)) {
            throw new \Exception('No Redis configuration found in peanut.tsk');
        }
        
        return new self($config);
    }
}