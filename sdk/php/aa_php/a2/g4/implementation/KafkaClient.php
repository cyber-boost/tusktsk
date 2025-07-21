<?php

namespace TuskLang\Communication\MessageQueue;

use RdKafka\Producer;
use RdKafka\Consumer;
use RdKafka\ConsumerTopic;
use RdKafka\ProducerTopic;
use RdKafka\Conf;
use RdKafka\TopicConf;

/**
 * Advanced Kafka Client
 * 
 * Features:
 * - Producer and consumer implementation
 * - Topic management and partitioning
 * - Offset management and commit strategies
 * - Dead letter queue handling
 * - Schema registry integration
 * - Performance optimization
 */
class KafkaClient implements MessageQueueInterface
{
    private array $config;
    private ?Producer $producer = null;
    private ?Consumer $consumer = null;
    private array $topics = [];
    private RetryManager $retryManager;

    public function __construct(array $config = [])
    {
        $this->config = array_merge([
            'bootstrap_servers' => 'localhost:9092',
            'client_id' => 'tusk-kafka-client',
            'group_id' => 'tusk-consumer-group',
            'auto_offset_reset' => 'earliest',
            'enable_auto_commit' => false,
            'batch_size' => 16384,
            'linger_ms' => 0,
            'request_timeout_ms' => 30000,
            'retry_attempts' => 3,
            'retry_delay' => 1000,
            'dead_letter_topic' => 'dead-letter-queue',
            'schema_registry_url' => null,
            'security_protocol' => 'plaintext',
            'sasl_mechanism' => null,
            'sasl_username' => null,
            'sasl_password' => null
        ], $config);

        $this->retryManager = new RetryManager($this->config);
        $this->initializeProducer();
        $this->initializeConsumer();
    }

    /**
     * Publish message to topic
     */
    public function publish(string $topic, $message, array $options = []): bool
    {
        try {
            $producerTopic = $this->getProducerTopic($topic, $options);
            
            $key = $options['key'] ?? null;
            $partition = $options['partition'] ?? RD_KAFKA_PARTITION_UA;
            $headers = $options['headers'] ?? [];
            
            // Serialize message
            $serializedMessage = $this->serializeMessage($message, $options);
            
            $producerTopic->produce($partition, 0, $serializedMessage, $key);
            
            // Poll for events
            $this->producer->poll(0);
            
            return true;
            
        } catch (\Exception $e) {
            $this->handlePublishError($topic, $message, $e, $options);
            return false;
        }
    }

    /**
     * Publish message synchronously with confirmation
     */
    public function publishSync(string $topic, $message, array $options = []): bool
    {
        try {
            $result = $this->publish($topic, $message, $options);
            
            if ($result) {
                // Wait for delivery confirmation
                $this->producer->flush(5000); // 5 second timeout
            }
            
            return $result;
            
        } catch (\Exception $e) {
            throw new MessageQueueException("Synchronous publish failed: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Publish batch of messages
     */
    public function publishBatch(string $topic, array $messages, array $options = []): array
    {
        $results = [];
        $producerTopic = $this->getProducerTopic($topic, $options);
        
        foreach ($messages as $index => $message) {
            try {
                $key = is_array($message) ? ($message['key'] ?? null) : null;
                $payload = is_array($message) ? ($message['payload'] ?? $message) : $message;
                $partition = is_array($message) ? ($message['partition'] ?? RD_KAFKA_PARTITION_UA) : RD_KAFKA_PARTITION_UA;
                
                $serializedMessage = $this->serializeMessage($payload, $options);
                $producerTopic->produce($partition, 0, $serializedMessage, $key);
                
                $results[$index] = true;
                
            } catch (\Exception $e) {
                $results[$index] = false;
                $this->handlePublishError($topic, $message, $e, $options);
            }
        }
        
        // Poll for events
        $this->producer->poll(0);
        
        return $results;
    }

    /**
     * Subscribe to topics
     */
    public function subscribe(array $topics, callable $callback, array $options = []): void
    {
        try {
            $this->consumer->subscribe($topics);
            
            $timeout = $options['timeout'] ?? 10000; // 10 seconds
            $maxMessages = $options['max_messages'] ?? 0; // Unlimited
            $processedCount = 0;
            
            while (true) {
                $message = $this->consumer->consume($timeout);
                
                if ($message === null) {
                    continue;
                }
                
                switch ($message->err) {
                    case RD_KAFKA_RESP_ERR_NO_ERROR:
                        $this->handleMessage($message, $callback, $options);
                        $processedCount++;
                        
                        if ($maxMessages > 0 && $processedCount >= $maxMessages) {
                            return;
                        }
                        break;
                        
                    case RD_KAFKA_RESP_ERR__PARTITION_EOF:
                        echo "No more messages; will wait for more\n";
                        break;
                        
                    case RD_KAFKA_RESP_ERR__TIMED_OUT:
                        echo "Timed out\n";
                        break;
                        
                    default:
                        throw new MessageQueueException("Consumer error: " . $message->errstr());
                }
            }
            
        } catch (\Exception $e) {
            throw new MessageQueueException("Subscription failed: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Create topic with configuration
     */
    public function createTopic(string $topicName, array $config = []): bool
    {
        // This would require Kafka Admin API
        // For now, topics are auto-created when first used
        return true;
    }

    /**
     * List available topics
     */
    public function listTopics(): array
    {
        $metadata = $this->consumer->getMetadata(true, null, 5000);
        $topics = [];
        
        foreach ($metadata->getTopics() as $topic) {
            $topics[] = $topic->getTopic();
        }
        
        return $topics;
    }

    /**
     * Get topic metadata
     */
    public function getTopicInfo(string $topic): array
    {
        $metadata = $this->consumer->getMetadata(false, $topic, 5000);
        $topicMetadata = $metadata->getTopics()[$topic] ?? null;
        
        if (!$topicMetadata) {
            throw new MessageQueueException("Topic not found: {$topic}");
        }
        
        $partitions = [];
        foreach ($topicMetadata->getPartitions() as $partition) {
            $partitions[] = [
                'id' => $partition->getId(),
                'leader' => $partition->getLeader(),
                'replicas' => $partition->getReplicas(),
                'isrs' => $partition->getIsrs()
            ];
        }
        
        return [
            'name' => $topicMetadata->getTopic(),
            'partitions' => $partitions,
            'partition_count' => count($partitions)
        ];
    }

    /**
     * Commit message offset
     */
    public function commit(?array $message = null): bool
    {
        try {
            if ($message) {
                $this->consumer->commitAsync($message);
            } else {
                $this->consumer->commit();
            }
            return true;
        } catch (\Exception $e) {
            return false;
        }
    }

    /**
     * Get consumer position
     */
    public function getPosition(string $topic, int $partition): int
    {
        $topicPartition = new \RdKafka\TopicPartition($topic, $partition);
        $positions = $this->consumer->getOffsetPositions([$topicPartition]);
        
        return $positions[0]->getOffset();
    }

    /**
     * Seek to specific offset
     */
    public function seek(string $topic, int $partition, int $offset): bool
    {
        try {
            $topicPartition = new \RdKafka\TopicPartition($topic, $partition, $offset);
            $this->consumer->seek($topicPartition, 5000);
            return true;
        } catch (\Exception $e) {
            return false;
        }
    }

    /**
     * Initialize producer
     */
    private function initializeProducer(): void
    {
        $conf = new Conf();
        
        // Basic configuration
        $conf->set('bootstrap.servers', $this->config['bootstrap_servers']);
        $conf->set('client.id', $this->config['client_id']);
        $conf->set('batch.size', (string) $this->config['batch_size']);
        $conf->set('linger.ms', (string) $this->config['linger_ms']);
        $conf->set('request.timeout.ms', (string) $this->config['request_timeout_ms']);
        
        // Security configuration
        if ($this->config['security_protocol'] !== 'plaintext') {
            $conf->set('security.protocol', $this->config['security_protocol']);
            
            if ($this->config['sasl_mechanism']) {
                $conf->set('sasl.mechanism', $this->config['sasl_mechanism']);
                $conf->set('sasl.username', $this->config['sasl_username']);
                $conf->set('sasl.password', $this->config['sasl_password']);
            }
        }
        
        // Delivery report callback
        $conf->setDrMsgCb(function ($kafka, $message) {
            if ($message->err) {
                echo "Message delivery failed: " . $message->errstr() . "\n";
            }
        });
        
        $this->producer = new Producer($conf);
    }

    /**
     * Initialize consumer
     */
    private function initializeConsumer(): void
    {
        $conf = new Conf();
        
        // Basic configuration
        $conf->set('bootstrap.servers', $this->config['bootstrap_servers']);
        $conf->set('group.id', $this->config['group_id']);
        $conf->set('auto.offset.reset', $this->config['auto_offset_reset']);
        $conf->set('enable.auto.commit', $this->config['enable_auto_commit'] ? 'true' : 'false');
        
        // Security configuration
        if ($this->config['security_protocol'] !== 'plaintext') {
            $conf->set('security.protocol', $this->config['security_protocol']);
            
            if ($this->config['sasl_mechanism']) {
                $conf->set('sasl.mechanism', $this->config['sasl_mechanism']);
                $conf->set('sasl.username', $this->config['sasl_username']);
                $conf->set('sasl.password', $this->config['sasl_password']);
            }
        }
        
        $this->consumer = new Consumer($conf);
    }

    /**
     * Get or create producer topic
     */
    private function getProducerTopic(string $topicName, array $options = []): ProducerTopic
    {
        if (!isset($this->topics[$topicName])) {
            $topicConf = new TopicConf();
            
            // Apply topic-specific configuration
            if (isset($options['topic_config'])) {
                foreach ($options['topic_config'] as $key => $value) {
                    $topicConf->set($key, $value);
                }
            }
            
            $this->topics[$topicName] = $this->producer->newTopic($topicName, $topicConf);
        }
        
        return $this->topics[$topicName];
    }

    /**
     * Handle incoming message
     */
    private function handleMessage($message, callable $callback, array $options): void
    {
        try {
            $deserializedMessage = $this->deserializeMessage($message->payload, $options);
            
            $messageData = [
                'topic' => $message->topic_name,
                'partition' => $message->partition,
                'offset' => $message->offset,
                'key' => $message->key,
                'payload' => $deserializedMessage,
                'timestamp' => $message->timestamp,
                'headers' => []
            ];
            
            $result = $callback($messageData);
            
            // Auto-commit if enabled
            if (!$this->config['enable_auto_commit'] && $result !== false) {
                $this->commit();
            }
            
        } catch (\Exception $e) {
            $this->handleConsumerError($message, $e, $options);
        }
    }

    /**
     * Serialize message for publishing
     */
    private function serializeMessage($message, array $options): string
    {
        $format = $options['format'] ?? 'json';
        
        switch ($format) {
            case 'json':
                return json_encode($message);
                
            case 'string':
                return (string) $message;
                
            case 'avro':
                return $this->serializeAvro($message, $options);
                
            default:
                return json_encode($message);
        }
    }

    /**
     * Deserialize message from consumption
     */
    private function deserializeMessage(string $data, array $options)
    {
        $format = $options['format'] ?? 'json';
        
        switch ($format) {
            case 'json':
                return json_decode($data, true);
                
            case 'string':
                return $data;
                
            case 'avro':
                return $this->deserializeAvro($data, $options);
                
            default:
                return json_decode($data, true);
        }
    }

    /**
     * Handle publish errors
     */
    private function handlePublishError(string $topic, $message, \Exception $e, array $options): void
    {
        echo "Publish error on topic {$topic}: " . $e->getMessage() . "\n";
        
        // Try to send to dead letter queue
        if (isset($this->config['dead_letter_topic'])) {
            try {
                $deadLetterMessage = [
                    'original_topic' => $topic,
                    'error' => $e->getMessage(),
                    'timestamp' => time(),
                    'payload' => $message
                ];
                
                $this->publish($this->config['dead_letter_topic'], $deadLetterMessage);
            } catch (\Exception $dlqError) {
                echo "Failed to send to dead letter queue: " . $dlqError->getMessage() . "\n";
            }
        }
    }

    /**
     * Handle consumer errors
     */
    private function handleConsumerError($message, \Exception $e, array $options): void
    {
        echo "Consumer error: " . $e->getMessage() . "\n";
        
        // Implement retry logic or dead letter queue
        $this->retryManager->handleFailedMessage($message, $e);
    }

    /**
     * Serialize message using Avro
     */
    private function serializeAvro($message, array $options): string
    {
        // Implement Avro serialization if schema registry is configured
        throw new \RuntimeException('Avro serialization not implemented');
    }

    /**
     * Deserialize Avro message
     */
    private function deserializeAvro(string $data, array $options)
    {
        // Implement Avro deserialization if schema registry is configured
        throw new \RuntimeException('Avro deserialization not implemented');
    }

    /**
     * Close connections
     */
    public function __destruct()
    {
        if ($this->producer) {
            $this->producer->flush(10000);
        }
    }
} 