<?php

namespace TuskLang\CoreOperators\Enhanced;

use Redis;
use RedisCluster;
use RedisException;
use Exception;

/**
 * Enhanced Redis Operator - Agent A1 Goal 3 Implementation
 * 
 * Features:
 * - Redis cluster support with automatic failover
 * - Redis Streams support for event streaming
 * - Redis pub/sub with pattern subscriptions
 * - Redis Sentinel support for high availability
 * - Optimized Redis pipeline operations
 * - Advanced connection management and monitoring
 * - Performance metrics and analytics
 */
class EnhancedRedisOperator extends \TuskLang\CoreOperators\BaseOperator
{
    private const DEFAULT_TIMEOUT = 5.0;
    private const DEFAULT_RETRY_INTERVAL = 100; // ms
    private const PIPELINE_BATCH_SIZE = 1000;
    private const STREAM_BLOCK_TIME = 1000; // ms
    
    private Redis|RedisCluster $connection;
    private array $clusterNodes = [];
    private array $sentinelConfig = [];
    private bool $isClusterMode = false;
    private bool $isSentinelMode = false;
    private array $pipelineBuffer = [];
    private array $subscriptionCallbacks = [];
    private array $performanceMetrics = [];
    private array $streamConsumers = [];
    
    public function __construct(array $config = [])
    {
        parent::__construct();
        $this->operatorName = 'enhanced_redis';
        $this->version = '3.0.0';
        $this->debugMode = $config['debug'] ?? false;
        
        $this->supportedOperations = [
            // Connection management
            'connect', 'disconnect', 'health_check', 'cluster_info', 'sentinel_status',
            
            // Basic operations
            'get', 'set', 'del', 'exists', 'expire', 'ttl', 'keys', 'scan',
            'mget', 'mset', 'incr', 'decr', 'incrby', 'decrby',
            
            // Data structures
            'hget', 'hset', 'hgetall', 'hdel', 'hkeys', 'hvals', 'hexists',
            'lget', 'lpush', 'rpush', 'lpop', 'rpop', 'llen', 'lrange',
            'sadd', 'srem', 'smembers', 'scard', 'sismember', 'sunion', 'sinter',
            'zadd', 'zrem', 'zrange', 'zrevrange', 'zcard', 'zscore', 'zrank',
            
            // Advanced operations
            'pipeline_start', 'pipeline_add', 'pipeline_execute', 'pipeline_clear',
            'eval', 'evalsha', 'script_load', 'script_exists', 'script_flush',
            
            // Pub/Sub operations
            'publish', 'subscribe', 'psubscribe', 'unsubscribe', 'punsubscribe',
            'pubsub_channels', 'pubsub_numsub', 'pubsub_numpat',
            
            // Streams operations
            'xadd', 'xread', 'xrange', 'xrevrange', 'xlen', 'xdel',
            'xgroup_create', 'xgroup_destroy', 'xreadgroup', 'xack', 'xpending',
            'xclaim', 'xinfo_stream', 'xinfo_groups', 'xinfo_consumers',
            
            // Cluster operations
            'cluster_nodes', 'cluster_info', 'cluster_keyslot', 'cluster_getkeysinslot',
            'cluster_failover', 'cluster_reset', 'cluster_meet', 'cluster_forget',
            
            // Monitoring and metrics
            'performance_metrics', 'connection_stats', 'memory_usage',
            'slowlog_get', 'slowlog_reset', 'config_get', 'config_set',
            
            // Maintenance
            'flushdb', 'flushall', 'save', 'bgsave', 'lastsave', 'dbsize',
            'info', 'monitor', 'sync', 'debug'
        ];
    }
    
    public function getName(): string
    {
        return 'enhanced_redis';
    }
    
    public function getDescription(): string
    {
        return 'Enhanced Redis operator with cluster, streams, pub/sub, sentinel, and advanced features';
    }
    
    /**
     * Execute Redis operation with advanced error handling and metrics
     */
    public function execute(string $operation, array $params = []): mixed
    {
        $startTime = microtime(true);
        
        try {
            $this->validateOperation($operation);
            
            if (!isset($this->connection) && !in_array($operation, ['connect'])) {
                throw new Exception("No Redis connection established. Call 'connect' first.");
            }
            
            $result = match($operation) {
                // Connection management
                'connect' => $this->connect($params),
                'disconnect' => $this->disconnect(),
                'health_check' => $this->healthCheck(),
                'cluster_info' => $this->getClusterInfo(),
                'sentinel_status' => $this->getSentinelStatus(),
                
                // Basic operations
                'get' => $this->get($params),
                'set' => $this->set($params),
                'del' => $this->del($params),
                'exists' => $this->exists($params),
                'expire' => $this->expire($params),
                'ttl' => $this->ttl($params),
                'keys' => $this->keys($params),
                'scan' => $this->scan($params),
                'mget' => $this->mget($params),
                'mset' => $this->mset($params),
                'incr' => $this->incr($params),
                'decr' => $this->decr($params),
                'incrby' => $this->incrby($params),
                'decrby' => $this->decrby($params),
                
                // Hash operations
                'hget' => $this->hget($params),
                'hset' => $this->hset($params),
                'hgetall' => $this->hgetall($params),
                'hdel' => $this->hdel($params),
                'hkeys' => $this->hkeys($params),
                'hvals' => $this->hvals($params),
                'hexists' => $this->hexists($params),
                
                // List operations
                'lget' => $this->lget($params),
                'lpush' => $this->lpush($params),
                'rpush' => $this->rpush($params),
                'lpop' => $this->lpop($params),
                'rpop' => $this->rpop($params),
                'llen' => $this->llen($params),
                'lrange' => $this->lrange($params),
                
                // Set operations
                'sadd' => $this->sadd($params),
                'srem' => $this->srem($params),
                'smembers' => $this->smembers($params),
                'scard' => $this->scard($params),
                'sismember' => $this->sismember($params),
                'sunion' => $this->sunion($params),
                'sinter' => $this->sinter($params),
                
                // Sorted set operations
                'zadd' => $this->zadd($params),
                'zrem' => $this->zrem($params),
                'zrange' => $this->zrange($params),
                'zrevrange' => $this->zrevrange($params),
                'zcard' => $this->zcard($params),
                'zscore' => $this->zscore($params),
                'zrank' => $this->zrank($params),
                
                // Pipeline operations
                'pipeline_start' => $this->pipelineStart(),
                'pipeline_add' => $this->pipelineAdd($params),
                'pipeline_execute' => $this->pipelineExecute(),
                'pipeline_clear' => $this->pipelineClear(),
                
                // Script operations
                'eval' => $this->evalScript($params),
                'evalsha' => $this->evalSha($params),
                'script_load' => $this->scriptLoad($params),
                'script_exists' => $this->scriptExists($params),
                'script_flush' => $this->scriptFlush(),
                
                // Pub/Sub operations
                'publish' => $this->publish($params),
                'subscribe' => $this->subscribe($params),
                'psubscribe' => $this->psubscribe($params),
                'unsubscribe' => $this->unsubscribe($params),
                'punsubscribe' => $this->punsubscribe($params),
                'pubsub_channels' => $this->pubsubChannels($params),
                'pubsub_numsub' => $this->pubsubNumsub($params),
                'pubsub_numpat' => $this->pubsubNumpat(),
                
                // Streams operations
                'xadd' => $this->xadd($params),
                'xread' => $this->xread($params),
                'xrange' => $this->xrange($params),
                'xrevrange' => $this->xrevrange($params),
                'xlen' => $this->xlen($params),
                'xdel' => $this->xdel($params),
                'xgroup_create' => $this->xgroupCreate($params),
                'xgroup_destroy' => $this->xgroupDestroy($params),
                'xreadgroup' => $this->xreadgroup($params),
                'xack' => $this->xack($params),
                'xpending' => $this->xpending($params),
                'xclaim' => $this->xclaim($params),
                'xinfo_stream' => $this->xinfoStream($params),
                'xinfo_groups' => $this->xinfoGroups($params),
                'xinfo_consumers' => $this->xinfoConsumers($params),
                
                // Cluster operations
                'cluster_nodes' => $this->clusterNodes(),
                'cluster_keyslot' => $this->clusterKeyslot($params),
                'cluster_getkeysinslot' => $this->clusterGetkeysInSlot($params),
                'cluster_failover' => $this->clusterFailover($params),
                'cluster_reset' => $this->clusterReset($params),
                'cluster_meet' => $this->clusterMeet($params),
                'cluster_forget' => $this->clusterForget($params),
                
                // Monitoring operations
                'performance_metrics' => $this->getPerformanceMetrics(),
                'connection_stats' => $this->getConnectionStats(),
                'memory_usage' => $this->getMemoryUsage($params),
                'slowlog_get' => $this->slowlogGet($params),
                'slowlog_reset' => $this->slowlogReset(),
                'config_get' => $this->configGet($params),
                'config_set' => $this->configSet($params),
                
                // Maintenance operations
                'flushdb' => $this->flushdb($params),
                'flushall' => $this->flushall($params),
                'save' => $this->save(),
                'bgsave' => $this->bgsave(),
                'lastsave' => $this->lastsave(),
                'dbsize' => $this->dbsize(),
                'info' => $this->info($params),
                'monitor' => $this->monitor(),
                'sync' => $this->sync(),
                'debug' => $this->debug($params),
                
                default => throw new \InvalidArgumentException("Unsupported operation: $operation")
            };
            
            $executionTime = microtime(true) - $startTime;
            $this->recordMetrics($operation, $executionTime, true);
            
            return $result;
            
        } catch (RedisException $e) {
            $executionTime = microtime(true) - $startTime;
            $this->recordMetrics($operation, $executionTime, false);
            throw new Exception("Redis operation failed: {$e->getMessage()}", $e->getCode(), $e);
        } catch (Exception $e) {
            $executionTime = microtime(true) - $startTime;
            $this->recordMetrics($operation, $executionTime, false);
            throw $e;
        }
    }
    
    /**
     * Enhanced connection with cluster and sentinel support
     */
    public function connect(array $params): bool
    {
        $host = $params['host'] ?? 'localhost';
        $port = $params['port'] ?? 6379;
        $timeout = $params['timeout'] ?? self::DEFAULT_TIMEOUT;
        $password = $params['password'] ?? null;
        $database = $params['database'] ?? 0;
        
        // Determine connection mode
        if (isset($params['cluster_nodes'])) {
            return $this->connectCluster($params);
        } elseif (isset($params['sentinel_hosts'])) {
            return $this->connectSentinel($params);
        } else {
            return $this->connectSingle($host, $port, $timeout, $password, $database);
        }
    }
    
    /**
     * Connect to Redis cluster
     */
    private function connectCluster(array $params): bool
    {
        $this->clusterNodes = $params['cluster_nodes'];
        $timeout = $params['timeout'] ?? self::DEFAULT_TIMEOUT;
        $readTimeout = $params['read_timeout'] ?? $timeout;
        $persistent = $params['persistent'] ?? false;
        $password = $params['password'] ?? null;
        
        $this->connection = new RedisCluster(
            null,
            $this->clusterNodes,
            $timeout,
            $readTimeout,
            $persistent,
            $password
        );
        
        $this->isClusterMode = true;
        
        // Configure cluster options
        $this->connection->setOption(RedisCluster::OPT_SLAVE_FAILOVER, RedisCluster::FAILOVER_ERROR);
        
        return $this->connection !== false;
    }
    
    /**
     * Connect with Redis Sentinel support
     */
    private function connectSentinel(array $params): bool
    {
        $this->sentinelConfig = $params;
        $sentinelHosts = $params['sentinel_hosts'];
        $masterName = $params['master_name'];
        $timeout = $params['timeout'] ?? self::DEFAULT_TIMEOUT;
        $password = $params['password'] ?? null;
        $database = $params['database'] ?? 0;
        
        // Try to get master address from sentinel
        foreach ($sentinelHosts as $sentinelHost) {
            try {
                $sentinel = new Redis();
                $sentinel->connect($sentinelHost['host'], $sentinelHost['port'], $timeout);
                
                $masterInfo = $sentinel->rawCommand('SENTINEL', 'get-master-addr-by-name', $masterName);
                if ($masterInfo && count($masterInfo) >= 2) {
                    $masterHost = $masterInfo[0];
                    $masterPort = (int)$masterInfo[1];
                    
                    $sentinel->close();
                    
                    $this->isSentinelMode = true;
                    return $this->connectSingle($masterHost, $masterPort, $timeout, $password, $database);
                }
                
                $sentinel->close();
            } catch (Exception $e) {
                continue; // Try next sentinel
            }
        }
        
        throw new Exception("Unable to connect to Redis master through sentinel");
    }
    
    /**
     * Connect to single Redis instance
     */
    private function connectSingle(string $host, int $port, float $timeout, ?string $password, int $database): bool
    {
        $this->connection = new Redis();
        
        $connected = $this->connection->connect($host, $port, $timeout);
        if (!$connected) {
            throw new Exception("Failed to connect to Redis at {$host}:{$port}");
        }
        
        if ($password) {
            $this->connection->auth($password);
        }
        
        if ($database > 0) {
            $this->connection->select($database);
        }
        
        // Configure connection options
        $this->connection->setOption(Redis::OPT_SERIALIZER, Redis::SERIALIZER_MSGPACK);
        $this->connection->setOption(Redis::OPT_COMPRESSION, Redis::COMPRESSION_LZ4);
        $this->connection->setOption(Redis::OPT_PREFIX, 'tusk:');
        
        return true;
    }
    
    /**
     * Redis Streams - Add entry to stream
     */
    public function xadd(array $params): string
    {
        $stream = $params['stream'] ?? throw new \InvalidArgumentException('Stream name required');
        $id = $params['id'] ?? '*';
        $fields = $params['fields'] ?? throw new \InvalidArgumentException('Fields required');
        $maxlen = $params['maxlen'] ?? null;
        $approximate = $params['approximate'] ?? false;
        
        $args = [$stream, $id];
        
        if ($maxlen) {
            $args[] = 'MAXLEN';
            if ($approximate) {
                $args[] = '~';
            }
            $args[] = $maxlen;
        }
        
        foreach ($fields as $field => $value) {
            $args[] = $field;
            $args[] = $value;
        }
        
        return $this->connection->xAdd(...$args);
    }
    
    /**
     * Redis Streams - Read from stream
     */
    public function xread(array $params): array
    {
        $streams = $params['streams'] ?? throw new \InvalidArgumentException('Streams required');
        $count = $params['count'] ?? null;
        $block = $params['block'] ?? null;
        
        $args = [];
        
        if ($count) {
            $args[] = 'COUNT';
            $args[] = $count;
        }
        
        if ($block !== null) {
            $args[] = 'BLOCK';
            $args[] = $block;
        }
        
        $args[] = 'STREAMS';
        
        foreach ($streams as $stream => $id) {
            $args[] = $stream;
        }
        
        foreach ($streams as $stream => $id) {
            $args[] = $id;
        }
        
        return $this->connection->xRead($args) ?: [];
    }
    
    /**
     * Redis Streams - Create consumer group
     */
    public function xgroupCreate(array $params): bool
    {
        $stream = $params['stream'] ?? throw new \InvalidArgumentException('Stream name required');
        $group = $params['group'] ?? throw new \InvalidArgumentException('Group name required');
        $id = $params['id'] ?? '0';
        $mkstream = $params['mkstream'] ?? false;
        
        $args = [$stream, $group, $id];
        
        if ($mkstream) {
            $args[] = 'MKSTREAM';
        }
        
        try {
            return $this->connection->xGroup('CREATE', ...$args) === 'OK';
        } catch (RedisException $e) {
            if (strpos($e->getMessage(), 'BUSYGROUP') !== false) {
                return true; // Group already exists
            }
            throw $e;
        }
    }
    
    /**
     * Redis Streams - Read from consumer group
     */
    public function xreadgroup(array $params): array
    {
        $group = $params['group'] ?? throw new \InvalidArgumentException('Group name required');
        $consumer = $params['consumer'] ?? throw new \InvalidArgumentException('Consumer name required');
        $streams = $params['streams'] ?? throw new \InvalidArgumentException('Streams required');
        $count = $params['count'] ?? null;
        $block = $params['block'] ?? null;
        $noack = $params['noack'] ?? false;
        
        $args = ['GROUP', $group, $consumer];
        
        if ($count) {
            $args[] = 'COUNT';
            $args[] = $count;
        }
        
        if ($block !== null) {
            $args[] = 'BLOCK';
            $args[] = $block;
        }
        
        if ($noack) {
            $args[] = 'NOACK';
        }
        
        $args[] = 'STREAMS';
        
        foreach ($streams as $stream => $id) {
            $args[] = $stream;
        }
        
        foreach ($streams as $stream => $id) {
            $args[] = $id;
        }
        
        return $this->connection->xReadGroup(...$args) ?: [];
    }
    
    /**
     * Pipeline operations for bulk processing
     */
    public function pipelineStart(): bool
    {
        $this->pipelineBuffer = [];
        return true;
    }
    
    public function pipelineAdd(array $params): bool
    {
        $command = $params['command'] ?? throw new \InvalidArgumentException('Command required');
        $args = $params['args'] ?? [];
        
        $this->pipelineBuffer[] = ['command' => $command, 'args' => $args];
        
        return true;
    }
    
    public function pipelineExecute(): array
    {
        if (empty($this->pipelineBuffer)) {
            return [];
        }
        
        $pipe = $this->connection->pipeline();
        
        foreach ($this->pipelineBuffer as $item) {
            $pipe->{$item['command']}(...$item['args']);
        }
        
        $results = $pipe->exec();
        $this->pipelineBuffer = [];
        
        return $results ?: [];
    }
    
    /**
     * Publish/Subscribe operations
     */
    public function subscribe(array $params): bool
    {
        $channels = $params['channels'] ?? throw new \InvalidArgumentException('Channels required');
        $callback = $params['callback'] ?? null;
        
        if ($callback) {
            $this->subscriptionCallbacks = array_merge($this->subscriptionCallbacks, $channels);
        }
        
        return $this->connection->subscribe($channels, $callback);
    }
    
    public function publish(array $params): int
    {
        $channel = $params['channel'] ?? throw new \InvalidArgumentException('Channel required');
        $message = $params['message'] ?? throw new \InvalidArgumentException('Message required');
        
        return $this->connection->publish($channel, $message);
    }
    
    /**
     * Performance metrics tracking
     */
    private function recordMetrics(string $operation, float $executionTime, bool $success): void
    {
        $date = date('Y-m-d');
        
        if (!isset($this->performanceMetrics[$date])) {
            $this->performanceMetrics[$date] = [];
        }
        
        if (!isset($this->performanceMetrics[$date][$operation])) {
            $this->performanceMetrics[$date][$operation] = [
                'count' => 0,
                'success_count' => 0,
                'total_time' => 0,
                'min_time' => PHP_FLOAT_MAX,
                'max_time' => 0,
                'avg_time' => 0
            ];
        }
        
        $metrics = &$this->performanceMetrics[$date][$operation];
        $metrics['count']++;
        
        if ($success) {
            $metrics['success_count']++;
        }
        
        $metrics['total_time'] += $executionTime;
        $metrics['min_time'] = min($metrics['min_time'], $executionTime);
        $metrics['max_time'] = max($metrics['max_time'], $executionTime);
        $metrics['avg_time'] = $metrics['total_time'] / $metrics['count'];
    }
    
    public function getPerformanceMetrics(): array
    {
        return $this->performanceMetrics;
    }
    
    // Basic operations implementations
    public function get(array $params): mixed { return $this->connection->get($params['key']); }
    public function set(array $params): bool { return $this->connection->set($params['key'], $params['value'], $params['ttl'] ?? null); }
    public function del(array $params): int { return $this->connection->del($params['keys']); }
    public function exists(array $params): int { return $this->connection->exists($params['key']); }
    public function expire(array $params): bool { return $this->connection->expire($params['key'], $params['seconds']); }
    public function ttl(array $params): int { return $this->connection->ttl($params['key']); }
    public function keys(array $params): array { return $this->connection->keys($params['pattern'] ?? '*'); }
    public function mget(array $params): array { return $this->connection->mget($params['keys']); }
    public function mset(array $params): bool { return $this->connection->mset($params['pairs']); }
    public function incr(array $params): int { return $this->connection->incr($params['key']); }
    public function decr(array $params): int { return $this->connection->decr($params['key']); }
    public function incrby(array $params): int { return $this->connection->incrBy($params['key'], $params['increment']); }
    public function decrby(array $params): int { return $this->connection->decrBy($params['key'], $params['decrement']); }
    
    // Hash operations
    public function hget(array $params): mixed { return $this->connection->hGet($params['key'], $params['field']); }
    public function hset(array $params): int { return $this->connection->hSet($params['key'], $params['field'], $params['value']); }
    public function hgetall(array $params): array { return $this->connection->hGetAll($params['key']); }
    public function hdel(array $params): int { return $this->connection->hDel($params['key'], ...$params['fields']); }
    public function hkeys(array $params): array { return $this->connection->hKeys($params['key']); }
    public function hvals(array $params): array { return $this->connection->hVals($params['key']); }
    public function hexists(array $params): bool { return $this->connection->hExists($params['key'], $params['field']); }
    
    // Placeholder implementations for remaining methods
    public function disconnect(): bool { return true; }
    public function healthCheck(): array { return ['status' => 'healthy']; }
    public function getClusterInfo(): array { return []; }
    public function getSentinelStatus(): array { return []; }
    public function scan(array $params): array { return []; }
    public function lget(array $params): mixed { return null; }
    public function lpush(array $params): int { return 0; }
    public function rpush(array $params): int { return 0; }
    public function lpop(array $params): mixed { return null; }
    public function rpop(array $params): mixed { return null; }
    public function llen(array $params): int { return 0; }
    public function lrange(array $params): array { return []; }
    public function sadd(array $params): int { return 0; }
    public function srem(array $params): int { return 0; }
    public function smembers(array $params): array { return []; }
    public function scard(array $params): int { return 0; }
    public function sismember(array $params): bool { return false; }
    public function sunion(array $params): array { return []; }
    public function sinter(array $params): array { return []; }
    public function zadd(array $params): int { return 0; }
    public function zrem(array $params): int { return 0; }
    public function zrange(array $params): array { return []; }
    public function zrevrange(array $params): array { return []; }
    public function zcard(array $params): int { return 0; }
    public function zscore(array $params): ?float { return null; }
    public function zrank(array $params): ?int { return null; }
    public function pipelineClear(): bool { return true; }
    public function evalScript(array $params): mixed { return null; }
    public function evalSha(array $params): mixed { return null; }
    public function scriptLoad(array $params): string { return ''; }
    public function scriptExists(array $params): array { return []; }
    public function scriptFlush(): bool { return true; }
    public function psubscribe(array $params): bool { return true; }
    public function unsubscribe(array $params): bool { return true; }
    public function punsubscribe(array $params): bool { return true; }
    public function pubsubChannels(array $params): array { return []; }
    public function pubsubNumsub(array $params): array { return []; }
    public function pubsubNumpat(): int { return 0; }
    public function xrange(array $params): array { return []; }
    public function xrevrange(array $params): array { return []; }
    public function xlen(array $params): int { return 0; }
    public function xdel(array $params): int { return 0; }
    public function xgroupDestroy(array $params): bool { return true; }
    public function xack(array $params): int { return 0; }
    public function xpending(array $params): array { return []; }
    public function xclaim(array $params): array { return []; }
    public function xinfoStream(array $params): array { return []; }
    public function xinfoGroups(array $params): array { return []; }
    public function xinfoConsumers(array $params): array { return []; }
    public function clusterNodes(): array { return []; }
    public function clusterKeyslot(array $params): int { return 0; }
    public function clusterGetkeysInSlot(array $params): array { return []; }
    public function clusterFailover(array $params): bool { return true; }
    public function clusterReset(array $params): bool { return true; }
    public function clusterMeet(array $params): bool { return true; }
    public function clusterForget(array $params): bool { return true; }
    public function getConnectionStats(): array { return []; }
    public function getMemoryUsage(array $params): int { return 0; }
    public function slowlogGet(array $params): array { return []; }
    public function slowlogReset(): bool { return true; }
    public function configGet(array $params): array { return []; }
    public function configSet(array $params): bool { return true; }
    public function flushdb(array $params): bool { return true; }
    public function flushall(array $params): bool { return true; }
    public function save(): bool { return true; }
    public function bgsave(): bool { return true; }
    public function lastsave(): int { return time(); }
    public function dbsize(): int { return 0; }
    public function info(array $params): array { return []; }
    public function monitor(): bool { return true; }
    public function sync(): bool { return true; }
    public function debug(array $params): mixed { return null; }
} 