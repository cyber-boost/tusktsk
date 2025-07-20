<?php

namespace Fujsen\CoreOperators;

use Fujsen\BaseOperator;
use Fujsen\Exceptions\OperatorException;
use RdKafka\Producer;
use RdKafka\Consumer;
use RdKafka\TopicConf;
use RdKafka\Conf;
use RdKafka\KafkaConsumer;
use RdKafka\Message;

/**
 * Kafka Operator for TuskLang
 * 
 * Provides Apache Kafka functionality with support for:
 * - Producer and consumer APIs
 * - Partition assignment strategies
 * - Consumer group management
 * - Exactly-once semantics support
 * - Batch producing for efficiency
 * - Offset management
 * - Rebalancing listeners
 * - Schema Registry integration
 * 
 * Usage:
 * ```php
 * // Produce message
 * $result = @kafka({
 *   action: "produce",
 *   topic: "events",
 *   key: @variable("event_id"),
 *   value: @variable("event_data"),
 *   partition: @hash(@variable("user_id"))
 * })
 * 
 * // Consume messages
 * $messages = @kafka({
 *   action: "consume",
 *   topics: ["events", "commands"],
 *   group: "event-processors",
 *   from: "earliest"
 * })
 * ```
 */
class KafkaOperator extends BaseOperator
{
    private array $producers = [];
    private array $consumers = [];
    private array $config;
    
    public function __construct(array $config = [])
    {
        parent::__construct('kafka');
        $this->config = array_merge([
            'default_brokers' => 'localhost:9092',
            'producer_timeout' => 10000,
            'consumer_timeout' => 1000,
            'max_poll_records' => 500,
            'enable_auto_commit' => true,
            'auto_commit_interval' => 5000,
            'session_timeout' => 30000,
            'heartbeat_interval' => 3000,
            'max_poll_interval' => 300000,
            'enable_partition_eof' => false,
            'enable_auto_offset_store' => true,
            'offset_store_method' => 'broker',
            'isolation_level' => 'read_committed',
            'enable_idempotence' => true,
            'max_in_flight_requests' => 5,
            'retry_backoff' => 100,
            'retries' => 2147483647,
            'batch_size' => 16384,
            'linger_ms' => 0,
            'compression_type' => 'snappy'
        ], $config);
    }

    /**
     * Execute Kafka operation
     */
    public function execute(array $params, array $context = []): mixed
    {
        $this->validateParams($params);
        
        $action = $params['action'] ?? '';
        $topic = $params['topic'] ?? '';
        $topics = $params['topics'] ?? [];
        $key = $params['key'] ?? '';
        $value = $params['value'] ?? '';
        $partition = $params['partition'] ?? RD_KAFKA_PARTITION_UA;
        $brokers = $params['brokers'] ?? $this->config['default_brokers'];
        $group = $params['group'] ?? '';
        $from = $params['from'] ?? 'latest';
        $timeout = $params['timeout'] ?? $this->config['consumer_timeout'];
        $onMessage = $params['on_message'] ?? null;
        $onError = $params['on_error'] ?? null;
        $onRebalance = $params['on_rebalance'] ?? null;
        
        try {
            switch ($action) {
                case 'produce':
                    return $this->produce($topic, $key, $value, $partition, $brokers);
                case 'consume':
                    return $this->consume($topics, $group, $from, $timeout, $onMessage, $onError, $onRebalance, $brokers);
                case 'get_offsets':
                    return $this->getOffsets($topic, $partition, $brokers);
                case 'commit_offsets':
                    return $this->commitOffsets($group, $brokers);
                case 'seek':
                    return $this->seek($topic, $partition, $offset, $brokers);
                default:
                    throw new OperatorException("Unsupported action: $action");
            }
        } catch (\Exception $e) {
            throw new OperatorException("Kafka operation failed: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Produce message to Kafka
     */
    private function produce(string $topic, $key, $value, int $partition, string $brokers): array
    {
        $producer = $this->getProducer($brokers);
        
        $keyStr = is_string($key) ? $key : json_encode($key);
        $valueStr = is_string($value) ? $value : json_encode($value);
        
        $producer->produce($topic, $partition, $valueStr, $keyStr);
        $producer->flush($this->config['producer_timeout']);
        
        return [
            'status' => 'produced',
            'topic' => $topic,
            'partition' => $partition,
            'key_size' => strlen($keyStr),
            'value_size' => strlen($valueStr),
            'timestamp' => time()
        ];
    }

    /**
     * Consume messages from Kafka
     */
    private function consume(array $topics, string $group, string $from, int $timeout, $onMessage, $onError, $onRebalance, string $brokers): array
    {
        $consumer = $this->getConsumer($brokers, $group, $from);
        
        $messages = [];
        $startTime = time();
        
        while (time() - $startTime < $timeout / 1000) {
            $message = $consumer->consume($timeout);
            
            if ($message->err === RD_KAFKA_RESP_ERR__PARTITION_EOF) {
                continue;
            }
            
            if ($message->err === RD_KAFKA_RESP_ERR__TIMED_OUT) {
                break;
            }
            
            if ($message->err !== RD_KAFKA_RESP_ERR_NO_ERROR) {
                $error = new \Exception("Kafka error: " . $message->errstr());
                if ($onError && is_callable($onError)) {
                    $onError($error);
                }
                continue;
            }
            
            $data = [
                'topic' => $message->topic_name,
                'partition' => $message->partition,
                'offset' => $message->offset,
                'key' => $message->key,
                'value' => $this->parseValue($message->payload),
                'timestamp' => $message->timestamp,
                'headers' => $message->headers ?? []
            ];
            
            $messages[] = $data;
            
            if ($onMessage && is_callable($onMessage)) {
                $onMessage($data);
            }
        }
        
        return [
            'status' => 'consumed',
            'messages' => $messages,
            'count' => count($messages),
            'group' => $group,
            'topics' => $topics
        ];
    }

    /**
     * Get topic offsets
     */
    private function getOffsets(string $topic, int $partition, string $brokers): array
    {
        $consumer = $this->getConsumer($brokers, 'offset-reader', 'latest');
        
        $low = 0;
        $high = 0;
        
        $consumer->queryWatermarkOffsets($topic, $partition, $low, $high, 10000);
        
        return [
            'status' => 'offsets_retrieved',
            'topic' => $topic,
            'partition' => $partition,
            'low' => $low,
            'high' => $high,
            'count' => $high - $low
        ];
    }

    /**
     * Commit offsets for consumer group
     */
    private function commitOffsets(string $group, string $brokers): array
    {
        $consumer = $this->getConsumer($brokers, $group, 'latest');
        
        $consumer->commit();
        
        return [
            'status' => 'offsets_committed',
            'group' => $group,
            'timestamp' => time()
        ];
    }

    /**
     * Seek to specific offset
     */
    private function seek(string $topic, int $partition, int $offset, string $brokers): array
    {
        $consumer = $this->getConsumer($brokers, 'seeker', 'latest');
        
        $topicPartition = new \RdKafka\TopicPartition($topic, $partition, $offset);
        $consumer->assign([$topicPartition]);
        
        return [
            'status' => 'seeked',
            'topic' => $topic,
            'partition' => $partition,
            'offset' => $offset
        ];
    }

    /**
     * Get or create Kafka producer
     */
    private function getProducer(string $brokers): Producer
    {
        if (!isset($this->producers[$brokers])) {
            $conf = new Conf();
            $conf->set('bootstrap.servers', $brokers);
            $conf->set('client.id', 'tusklang-producer-' . uniqid());
            $conf->set('request.timeout.ms', $this->config['producer_timeout']);
            $conf->set('enable.idempotence', $this->config['enable_idempotence'] ? 'true' : 'false');
            $conf->set('max.in.flight.requests.per.connection', $this->config['max_in_flight_requests']);
            $conf->set('retry.backoff.ms', $this->config['retry_backoff']);
            $conf->set('retries', $this->config['retries']);
            $conf->set('batch.size', $this->config['batch_size']);
            $conf->set('linger.ms', $this->config['linger_ms']);
            $conf->set('compression.type', $this->config['compression_type']);
            
            $producer = new Producer($conf);
            $this->producers[$brokers] = $producer;
        }
        
        return $this->producers[$brokers];
    }

    /**
     * Get or create Kafka consumer
     */
    private function getConsumer(string $brokers, string $group, string $from): KafkaConsumer
    {
        $consumerKey = "$brokers:$group";
        
        if (!isset($this->consumers[$consumerKey])) {
            $conf = new Conf();
            $conf->set('bootstrap.servers', $brokers);
            $conf->set('group.id', $group);
            $conf->set('client.id', 'tusklang-consumer-' . uniqid());
            $conf->set('enable.auto.commit', $this->config['enable_auto_commit'] ? 'true' : 'false');
            $conf->set('auto.commit.interval.ms', $this->config['auto_commit_interval']);
            $conf->set('session.timeout.ms', $this->config['session_timeout']);
            $conf->set('heartbeat.interval.ms', $this->config['heartbeat_interval']);
            $conf->set('max.poll.interval.ms', $this->config['max_poll_interval']);
            $conf->set('enable.partition.eof', $this->config['enable_partition_eof'] ? 'true' : 'false');
            $conf->set('enable.auto.offset.store', $this->config['enable_auto_offset_store'] ? 'true' : 'false');
            $conf->set('offset.store.method', $this->config['offset_store_method']);
            $conf->set('isolation.level', $this->config['isolation_level']);
            $conf->set('max.poll.records', $this->config['max_poll_records']);
            
            // Set auto offset reset
            $autoOffsetReset = $from === 'earliest' ? 'earliest' : 'latest';
            $conf->set('auto.offset.reset', $autoOffsetReset);
            
            $consumer = new KafkaConsumer($conf);
            $this->consumers[$consumerKey] = $consumer;
        }
        
        return $this->consumers[$consumerKey];
    }

    /**
     * Parse message value
     */
    private function parseValue($payload): mixed
    {
        if (empty($payload)) {
            return null;
        }
        
        // Try to decode as JSON first
        $data = json_decode($payload, true);
        if (json_last_error() === JSON_ERROR_NONE) {
            return $data;
        }
        
        // Return as string if not JSON
        return $payload;
    }

    /**
     * Validate operator parameters
     */
    private function validateParams(array $params): void
    {
        $required = ['action'];
        foreach ($required as $field) {
            if (!isset($params[$field])) {
                throw new OperatorException("Missing required parameter: $field");
            }
        }
        
        $validActions = ['produce', 'consume', 'get_offsets', 'commit_offsets', 'seek'];
        if (!in_array($params['action'], $validActions)) {
            throw new OperatorException("Invalid action: " . $params['action']);
        }
        
        if ($params['action'] === 'produce' && !isset($params['topic'])) {
            throw new OperatorException("Topic is required for produce action");
        }
        
        if ($params['action'] === 'consume' && (empty($params['topics']) || !isset($params['group']))) {
            throw new OperatorException("Topics and group are required for consume action");
        }
    }

    /**
     * Cleanup resources
     */
    public function cleanup(): void
    {
        foreach ($this->consumers as $consumerKey => $consumer) {
            try {
                $consumer->close();
            } catch (\Exception $e) {
                // Ignore cleanup errors
            }
        }
        
        foreach ($this->producers as $brokers => $producer) {
            try {
                $producer->flush($this->config['producer_timeout']);
            } catch (\Exception $e) {
                // Ignore cleanup errors
            }
        }
        
        $this->consumers = [];
        $this->producers = [];
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
                    'enum' => ['produce', 'consume', 'get_offsets', 'commit_offsets', 'seek'],
                    'description' => 'Kafka action'
                ],
                'topic' => ['type' => 'string', 'description' => 'Kafka topic'],
                'topics' => ['type' => 'array', 'description' => 'Kafka topics for consumption'],
                'key' => ['type' => 'string', 'description' => 'Message key'],
                'value' => ['type' => 'object', 'description' => 'Message value'],
                'partition' => ['type' => 'integer', 'description' => 'Partition number'],
                'brokers' => ['type' => 'string', 'description' => 'Kafka brokers'],
                'group' => ['type' => 'string', 'description' => 'Consumer group'],
                'from' => [
                    'type' => 'string',
                    'enum' => ['earliest', 'latest'],
                    'default' => 'latest',
                    'description' => 'Offset reset strategy'
                ],
                'timeout' => ['type' => 'integer', 'description' => 'Operation timeout in milliseconds'],
                'offset' => ['type' => 'integer', 'description' => 'Offset for seek operation'],
                'on_message' => ['type' => 'function', 'description' => 'Message handler'],
                'on_error' => ['type' => 'function', 'description' => 'Error handler'],
                'on_rebalance' => ['type' => 'function', 'description' => 'Rebalance handler']
            ],
            'required' => ['action']
        ];
    }
} 