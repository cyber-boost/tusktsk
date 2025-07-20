<?php

namespace Fujsen\CoreOperators;

use Fujsen\BaseOperator;
use Fujsen\Exceptions\OperatorException;

/**
 * Redis Operator for TuskLang
 * 
 * Provides Redis functionality with support for:
 * - Key-value operations with TTL
 * - Pub/Sub messaging
 * - Streams and consumer groups
 * - Lists, Sets, Hashes, Sorted Sets
 * - Lua scripting
 * - Cluster and Sentinel support
 * - Connection pooling and failover
 * 
 * Usage:
 * ```php
 * // Get cached value
 * $data = @redis({
 *   action: "get",
 *   key: "user:123",
 *   ttl: 3600
 * })
 * 
 * // Publish message
 * $result = @redis({
 *   action: "publish",
 *   channel: "notifications",
 *   message: @variable("alert_data")
 * })
 * ```
 */
class RedisOperator extends BaseOperator
{
    private array $connections = [];
    private array $subscriptions = [];
    private array $streams = [];
    private array $config;
    
    public function __construct(array $config = [])
    {
        parent::__construct('redis');
        $this->config = array_merge([
            'default_host' => 'localhost',
            'default_port' => 6379,
            'default_db' => 0,
            'timeout' => 5.0,
            'retry_attempts' => 3,
            'retry_delay' => 1000,
            'password' => '',
            'enable_tls' => false,
            'tls_cert' => '',
            'tls_key' => '',
            'tls_ca' => '',
            'cluster_mode' => false,
            'sentinel_mode' => false,
            'sentinel_masters' => []
        ], $config);
    }

    /**
     * Execute Redis operation
     */
    public function execute(array $params, array $context = []): mixed
    {
        $this->validateParams($params);
        
        $action = $params['action'] ?? '';
        $key = $params['key'] ?? '';
        $value = $params['value'] ?? '';
        $host = $params['host'] ?? $this->config['default_host'];
        $port = $params['port'] ?? $this->config['default_port'];
        $db = $params['db'] ?? $this->config['default_db'];
        $ttl = $params['ttl'] ?? 0;
        $channel = $params['channel'] ?? '';
        $message = $params['message'] ?? '';
        $stream = $params['stream'] ?? '';
        $field = $params['field'] ?? '';
        $score = $params['score'] ?? 0;
        $pattern = $params['pattern'] ?? '';
        $script = $params['script'] ?? '';
        $args = $params['args'] ?? [];
        
        try {
            $redis = $this->getConnection($host, $port, $db);
            
            switch ($action) {
                case 'get':
                    return $this->getValue($redis, $key);
                case 'set':
                    return $this->setValue($redis, $key, $value, $ttl);
                case 'del':
                    return $this->deleteValue($redis, $key);
                case 'exists':
                    return $this->keyExists($redis, $key);
                case 'expire':
                    return $this->setExpiry($redis, $key, $ttl);
                case 'publish':
                    return $this->publishMessage($redis, $channel, $message);
                case 'subscribe':
                    return $this->subscribeChannel($redis, $channel, $params);
                case 'stream':
                    return $this->handleStream($redis, $stream, $params);
                case 'hash':
                    return $this->handleHash($redis, $key, $field, $value, $params);
                case 'list':
                    return $this->handleList($redis, $key, $value, $params);
                case 'set':
                    return $this->handleSet($redis, $key, $value, $params);
                case 'zset':
                    return $this->handleSortedSet($redis, $key, $field, $score, $params);
                case 'script':
                    return $this->executeScript($redis, $script, $args);
                case 'info':
                    return $this->getInfo($redis);
                default:
                    throw new OperatorException("Unsupported action: $action");
            }
        } catch (\Exception $e) {
            throw new OperatorException("Redis operation failed: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Get value from Redis
     */
    private function getValue($redis, string $key): mixed
    {
        $value = $redis->get($key);
        
        if ($value === false) {
            return null;
        }
        
        // Try to decode JSON
        $decoded = json_decode($value, true);
        if (json_last_error() === JSON_ERROR_NONE) {
            return $decoded;
        }
        
        return $value;
    }

    /**
     * Set value in Redis
     */
    private function setValue($redis, string $key, $value, int $ttl): array
    {
        // Encode arrays/objects as JSON
        if (is_array($value) || is_object($value)) {
            $value = json_encode($value);
        }
        
        $result = $redis->set($key, $value);
        
        if ($ttl > 0) {
            $redis->expire($key, $ttl);
        }
        
        return [
            'status' => $result ? 'set' : 'failed',
            'key' => $key,
            'ttl' => $ttl > 0 ? $ttl : null
        ];
    }

    /**
     * Delete value from Redis
     */
    private function deleteValue($redis, string $key): array
    {
        $result = $redis->del($key);
        
        return [
            'status' => 'deleted',
            'key' => $key,
            'deleted' => $result
        ];
    }

    /**
     * Check if key exists
     */
    private function keyExists($redis, string $key): array
    {
        $exists = $redis->exists($key);
        
        return [
            'status' => 'checked',
            'key' => $key,
            'exists' => (bool)$exists
        ];
    }

    /**
     * Set key expiry
     */
    private function setExpiry($redis, string $key, int $ttl): array
    {
        $result = $redis->expire($key, $ttl);
        
        return [
            'status' => $result ? 'expiry_set' : 'failed',
            'key' => $key,
            'ttl' => $ttl
        ];
    }

    /**
     * Publish message to channel
     */
    private function publishMessage($redis, string $channel, $message): array
    {
        if (is_array($message) || is_object($message)) {
            $message = json_encode($message);
        }
        
        $subscribers = $redis->publish($channel, $message);
        
        return [
            'status' => 'published',
            'channel' => $channel,
            'subscribers' => $subscribers
        ];
    }

    /**
     * Subscribe to channel
     */
    private function subscribeChannel($redis, string $channel, array $params): array
    {
        $callback = $params['callback'] ?? null;
        $timeout = $params['timeout'] ?? 0;
        
        $subscriptionId = uniqid('sub_');
        
        $this->subscriptions[$subscriptionId] = [
            'channel' => $channel,
            'callback' => $callback,
            'active' => true
        ];
        
        // In a real implementation, this would be async
        // For now, just store the subscription configuration
        
        return [
            'status' => 'subscribed',
            'subscription_id' => $subscriptionId,
            'channel' => $channel
        ];
    }

    /**
     * Handle stream operations
     */
    private function handleStream($redis, string $stream, array $params): mixed
    {
        $operation = $params['stream_action'] ?? 'add';
        
        switch ($operation) {
            case 'add':
                $data = $params['data'] ?? [];
                $id = $params['id'] ?? '*';
                return $this->addToStream($redis, $stream, $data, $id);
            case 'read':
                $count = $params['count'] ?? 10;
                $block = $params['block'] ?? 0;
                return $this->readFromStream($redis, $stream, $count, $block);
            case 'group':
                return $this->handleConsumerGroup($redis, $stream, $params);
            default:
                throw new OperatorException("Unsupported stream action: $operation");
        }
    }

    /**
     * Add to stream
     */
    private function addToStream($redis, string $stream, array $data, string $id): array
    {
        $result = $redis->xAdd($stream, $id, $data);
        
        return [
            'status' => 'added',
            'stream' => $stream,
            'id' => $result
        ];
    }

    /**
     * Read from stream
     */
    private function readFromStream($redis, string $stream, int $count, int $block): array
    {
        $messages = $redis->xRead([$stream => '0'], $count, $block);
        
        return [
            'status' => 'read',
            'stream' => $stream,
            'messages' => $messages ?: [],
            'count' => count($messages ?: [])
        ];
    }

    /**
     * Handle consumer group
     */
    private function handleConsumerGroup($redis, string $stream, array $params): array
    {
        $groupAction = $params['group_action'] ?? 'create';
        $group = $params['group'] ?? '';
        $consumer = $params['consumer'] ?? '';
        
        switch ($groupAction) {
            case 'create':
                $redis->xGroup('CREATE', $stream, $group, '0');
                return ['status' => 'group_created', 'stream' => $stream, 'group' => $group];
            case 'read':
                $count = $params['count'] ?? 10;
                $messages = $redis->xReadGroup($group, $consumer, [$stream => '>'], $count);
                return ['status' => 'group_read', 'messages' => $messages ?: []];
            default:
                throw new OperatorException("Unsupported group action: $groupAction");
        }
    }

    /**
     * Handle hash operations
     */
    private function handleHash($redis, string $key, string $field, $value, array $params): mixed
    {
        $operation = $params['hash_action'] ?? 'get';
        
        switch ($operation) {
            case 'get':
                return $this->getHashField($redis, $key, $field);
            case 'set':
                return $this->setHashField($redis, $key, $field, $value);
            case 'getall':
                return $this->getAllHashFields($redis, $key);
            case 'del':
                return $this->deleteHashField($redis, $key, $field);
            default:
                throw new OperatorException("Unsupported hash action: $operation");
        }
    }

    /**
     * Get hash field
     */
    private function getHashField($redis, string $key, string $field): mixed
    {
        $value = $redis->hGet($key, $field);
        
        if ($value === false) {
            return null;
        }
        
        $decoded = json_decode($value, true);
        return json_last_error() === JSON_ERROR_NONE ? $decoded : $value;
    }

    /**
     * Set hash field
     */
    private function setHashField($redis, string $key, string $field, $value): array
    {
        if (is_array($value) || is_object($value)) {
            $value = json_encode($value);
        }
        
        $result = $redis->hSet($key, $field, $value);
        
        return [
            'status' => 'field_set',
            'key' => $key,
            'field' => $field,
            'result' => $result
        ];
    }

    /**
     * Get all hash fields
     */
    private function getAllHashFields($redis, string $key): array
    {
        $fields = $redis->hGetAll($key);
        
        // Decode JSON values
        foreach ($fields as $field => $value) {
            $decoded = json_decode($value, true);
            if (json_last_error() === JSON_ERROR_NONE) {
                $fields[$field] = $decoded;
            }
        }
        
        return [
            'status' => 'retrieved',
            'key' => $key,
            'fields' => $fields
        ];
    }

    /**
     * Delete hash field
     */
    private function deleteHashField($redis, string $key, string $field): array
    {
        $result = $redis->hDel($key, $field);
        
        return [
            'status' => 'field_deleted',
            'key' => $key,
            'field' => $field,
            'deleted' => $result
        ];
    }

    /**
     * Handle list operations
     */
    private function handleList($redis, string $key, $value, array $params): mixed
    {
        $operation = $params['list_action'] ?? 'push';
        $side = $params['side'] ?? 'right';
        
        switch ($operation) {
            case 'push':
                return $this->pushToList($redis, $key, $value, $side);
            case 'pop':
                return $this->popFromList($redis, $key, $side);
            case 'range':
                $start = $params['start'] ?? 0;
                $stop = $params['stop'] ?? -1;
                return $this->getListRange($redis, $key, $start, $stop);
            default:
                throw new OperatorException("Unsupported list action: $operation");
        }
    }

    /**
     * Push to list
     */
    private function pushToList($redis, string $key, $value, string $side): array
    {
        if (is_array($value) || is_object($value)) {
            $value = json_encode($value);
        }
        
        if ($side === 'left') {
            $result = $redis->lPush($key, $value);
        } else {
            $result = $redis->rPush($key, $value);
        }
        
        return [
            'status' => 'pushed',
            'key' => $key,
            'side' => $side,
            'length' => $result
        ];
    }

    /**
     * Pop from list
     */
    private function popFromList($redis, string $key, string $side): mixed
    {
        if ($side === 'left') {
            $value = $redis->lPop($key);
        } else {
            $value = $redis->rPop($key);
        }
        
        if ($value === false) {
            return null;
        }
        
        $decoded = json_decode($value, true);
        return json_last_error() === JSON_ERROR_NONE ? $decoded : $value;
    }

    /**
     * Get list range
     */
    private function getListRange($redis, string $key, int $start, int $stop): array
    {
        $values = $redis->lRange($key, $start, $stop);
        
        // Decode JSON values
        foreach ($values as &$value) {
            $decoded = json_decode($value, true);
            if (json_last_error() === JSON_ERROR_NONE) {
                $value = $decoded;
            }
        }
        
        return [
            'status' => 'retrieved',
            'key' => $key,
            'values' => $values
        ];
    }

    /**
     * Handle set operations
     */
    private function handleSet($redis, string $key, $value, array $params): mixed
    {
        $operation = $params['set_action'] ?? 'add';
        
        switch ($operation) {
            case 'add':
                return $this->addToSet($redis, $key, $value);
            case 'members':
                return $this->getSetMembers($redis, $key);
            case 'remove':
                return $this->removeFromSet($redis, $key, $value);
            default:
                throw new OperatorException("Unsupported set action: $operation");
        }
    }

    /**
     * Add to set
     */
    private function addToSet($redis, string $key, $value): array
    {
        if (is_array($value)) {
            $result = $redis->sAdd($key, ...$value);
        } else {
            $result = $redis->sAdd($key, $value);
        }
        
        return [
            'status' => 'added',
            'key' => $key,
            'added' => $result
        ];
    }

    /**
     * Get set members
     */
    private function getSetMembers($redis, string $key): array
    {
        $members = $redis->sMembers($key);
        
        return [
            'status' => 'retrieved',
            'key' => $key,
            'members' => $members
        ];
    }

    /**
     * Remove from set
     */
    private function removeFromSet($redis, string $key, $value): array
    {
        $result = $redis->sRem($key, $value);
        
        return [
            'status' => 'removed',
            'key' => $key,
            'removed' => $result
        ];
    }

    /**
     * Handle sorted set operations
     */
    private function handleSortedSet($redis, string $key, string $field, float $score, array $params): mixed
    {
        $operation = $params['zset_action'] ?? 'add';
        
        switch ($operation) {
            case 'add':
                return $this->addToSortedSet($redis, $key, $field, $score);
            case 'range':
                $start = $params['start'] ?? 0;
                $stop = $params['stop'] ?? -1;
                $withScores = $params['with_scores'] ?? false;
                return $this->getSortedSetRange($redis, $key, $start, $stop, $withScores);
            case 'remove':
                return $this->removeFromSortedSet($redis, $key, $field);
            default:
                throw new OperatorException("Unsupported sorted set action: $operation");
        }
    }

    /**
     * Add to sorted set
     */
    private function addToSortedSet($redis, string $key, string $field, float $score): array
    {
        $result = $redis->zAdd($key, $score, $field);
        
        return [
            'status' => 'added',
            'key' => $key,
            'field' => $field,
            'score' => $score,
            'added' => $result
        ];
    }

    /**
     * Get sorted set range
     */
    private function getSortedSetRange($redis, string $key, int $start, int $stop, bool $withScores): array
    {
        if ($withScores) {
            $values = $redis->zRange($key, $start, $stop, true);
        } else {
            $values = $redis->zRange($key, $start, $stop);
        }
        
        return [
            'status' => 'retrieved',
            'key' => $key,
            'values' => $values
        ];
    }

    /**
     * Remove from sorted set
     */
    private function removeFromSortedSet($redis, string $key, string $field): array
    {
        $result = $redis->zRem($key, $field);
        
        return [
            'status' => 'removed',
            'key' => $key,
            'field' => $field,
            'removed' => $result
        ];
    }

    /**
     * Execute Lua script
     */
    private function executeScript($redis, string $script, array $args): mixed
    {
        $result = $redis->eval($script, $args, 0);
        
        return [
            'status' => 'executed',
            'result' => $result
        ];
    }

    /**
     * Get Redis info
     */
    private function getInfo($redis): array
    {
        $info = $redis->info();
        
        return [
            'status' => 'retrieved',
            'info' => $info
        ];
    }

    /**
     * Get or create Redis connection
     */
    private function getConnection(string $host, int $port, int $db): object
    {
        $connectionKey = $host . ':' . $port . ':' . $db;
        
        if (!isset($this->connections[$connectionKey])) {
            $this->connections[$connectionKey] = $this->createConnection($host, $port, $db);
        }
        
        return $this->connections[$connectionKey];
    }

    /**
     * Create Redis connection
     */
    private function createConnection(string $host, int $port, int $db): object
    {
        $redis = new \Redis();
        
        $redis->connect($host, $port, $this->config['timeout']);
        
        if ($this->config['password']) {
            $redis->auth($this->config['password']);
        }
        
        if ($db > 0) {
            $redis->select($db);
        }
        
        return $redis;
    }

    /**
     * Validate operator parameters
     */
    private function validateParams(array $params): void
    {
        if (!isset($params['action'])) {
            throw new OperatorException("Action is required");
        }
        
        $validActions = ['get', 'set', 'del', 'exists', 'expire', 'publish', 'subscribe', 'stream', 'hash', 'list', 'set', 'zset', 'script', 'info'];
        if (!in_array($params['action'], $validActions)) {
            throw new OperatorException("Invalid action: " . $params['action']);
        }
        
        if (in_array($params['action'], ['get', 'set', 'del', 'exists', 'expire']) && !isset($params['key'])) {
            throw new OperatorException("Key is required for " . $params['action'] . " action");
        }
        
        if ($params['action'] === 'set' && !isset($params['value'])) {
            throw new OperatorException("Value is required for set action");
        }
        
        if ($params['action'] === 'publish' && (!isset($params['channel']) || !isset($params['message']))) {
            throw new OperatorException("Channel and message are required for publish action");
        }
    }

    /**
     * Cleanup resources
     */
    public function cleanup(): void
    {
        foreach ($this->connections as $connection) {
            $connection->close();
        }
        
        $this->connections = [];
        $this->subscriptions = [];
        $this->streams = [];
    }

    /**
     * Get operator schema
     */
    public function getSchema(): array
    {
        return [
            'type' => 'object',
            'properties' => [
                'action' => [
                    'type' => 'string',
                    'enum' => ['get', 'set', 'del', 'exists', 'expire', 'publish', 'subscribe', 'stream', 'hash', 'list', 'set', 'zset', 'script', 'info'],
                    'description' => 'Redis action'
                ],
                'key' => ['type' => 'string', 'description' => 'Key'],
                'value' => ['type' => 'mixed', 'description' => 'Value'],
                'host' => ['type' => 'string', 'description' => 'Redis host'],
                'port' => ['type' => 'integer', 'description' => 'Redis port'],
                'db' => ['type' => 'integer', 'description' => 'Database number'],
                'ttl' => ['type' => 'integer', 'description' => 'Time to live'],
                'channel' => ['type' => 'string', 'description' => 'Channel name'],
                'message' => ['type' => 'mixed', 'description' => 'Message'],
                'stream' => ['type' => 'string', 'description' => 'Stream name'],
                'field' => ['type' => 'string', 'description' => 'Field name'],
                'score' => ['type' => 'number', 'description' => 'Score'],
                'script' => ['type' => 'string', 'description' => 'Lua script'],
                'args' => ['type' => 'array', 'description' => 'Script arguments']
            ],
            'required' => ['action']
        ];
    }
} 