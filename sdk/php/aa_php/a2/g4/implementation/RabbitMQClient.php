<?php

namespace TuskLang\Communication\MessageQueue;

use PhpAmqpLib\Connection\AMQPStreamConnection;
use PhpAmqpLib\Channel\AMQPChannel;
use PhpAmqpLib\Message\AMQPMessage;
use PhpAmqpLib\Exception\AMQPTimeoutException;

/**
 * Advanced RabbitMQ Client
 * 
 * Features:
 * - AMQP 0.9.1 protocol support
 * - Exchange and queue management
 * - Message routing with patterns
 * - Dead letter queue handling
 * - Message acknowledgments
 * - Connection pooling and clustering
 */
class RabbitMQClient implements MessageQueueInterface
{
    private array $config;
    private ?AMQPStreamConnection $connection = null;
    private ?AMQPChannel $channel = null;
    private array $exchanges = [];
    private array $queues = [];
    private RetryManager $retryManager;

    public function __construct(array $config = [])
    {
        $this->config = array_merge([
            'host' => 'localhost',
            'port' => 5672,
            'user' => 'guest',
            'password' => 'guest',
            'vhost' => '/',
            'timeout' => 30,
            'read_timeout' => 30,
            'write_timeout' => 30,
            'heartbeat' => 60,
            'exchange_type' => 'topic',
            'durable' => true,
            'auto_delete' => false,
            'delivery_mode' => 2, // Persistent messages
            'dead_letter_exchange' => 'dlx',
            'retry_attempts' => 3,
            'retry_delay' => 1000
        ], $config);

        $this->retryManager = new RetryManager($this->config);
        $this->connect();
    }

    /**
     * Publish message to exchange
     */
    public function publish(string $topic, $message, array $options = []): bool
    {
        try {
            $exchange = $options['exchange'] ?? 'default';
            $routingKey = $options['routing_key'] ?? $topic;
            
            // Ensure exchange exists
            $this->ensureExchange($exchange, $options);
            
            // Serialize message
            $messageBody = $this->serializeMessage($message, $options);
            
            // Create AMQP message
            $amqpMessage = new AMQPMessage($messageBody, [
                'delivery_mode' => $this->config['delivery_mode'],
                'timestamp' => time(),
                'message_id' => $this->generateMessageId(),
                'content_type' => $options['content_type'] ?? 'application/json',
                'headers' => $options['headers'] ?? []
            ]);

            // Add expiration if set
            if (isset($options['ttl'])) {
                $amqpMessage->set('expiration', (string)$options['ttl']);
            }

            // Publish message
            $this->channel->basic_publish($amqpMessage, $exchange, $routingKey);
            
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
            // Enable publisher confirms
            $this->channel->confirm_select();
            
            $result = $this->publish($topic, $message, $options);
            
            if ($result) {
                // Wait for confirmation
                $this->channel->wait_for_pending_acks(5); // 5 second timeout
            }
            
            return $result;
            
        } catch (\Exception $e) {
            throw new MessageQueueException("Synchronous publish failed: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Subscribe to topics with callback
     */
    public function subscribe(array $topics, callable $callback, array $options = []): void
    {
        try {
            $exchange = $options['exchange'] ?? 'default';
            $queueName = $options['queue'] ?? '';
            $consumerTag = $options['consumer_tag'] ?? '';
            
            // Ensure exchange exists
            $this->ensureExchange($exchange, $options);
            
            // Declare queue
            list($queueName,) = $this->channel->queue_declare(
                $queueName,
                false, // passive
                $this->config['durable'], // durable
                false, // exclusive
                $this->config['auto_delete'], // auto_delete
                false, // nowait
                $this->getQueueArguments($options)
            );

            // Bind queue to topics
            foreach ($topics as $topic) {
                $this->channel->queue_bind($queueName, $exchange, $topic);
            }

            // Set QoS
            $prefetchCount = $options['prefetch_count'] ?? 1;
            $this->channel->basic_qos(null, $prefetchCount, false);

            // Create consumer callback
            $consumerCallback = function (AMQPMessage $msg) use ($callback, $options) {
                $this->handleMessage($msg, $callback, $options);
            };

            // Start consuming
            $this->channel->basic_consume(
                $queueName,
                $consumerTag,
                false, // no_local
                false, // no_ack (manual ack)
                false, // exclusive
                false, // nowait
                $consumerCallback
            );

            // Wait for messages
            while ($this->channel->is_consuming()) {
                $this->channel->wait(null, false, $options['timeout'] ?? 0);
            }
            
        } catch (\Exception $e) {
            throw new MessageQueueException("Subscription failed: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Create exchange
     */
    public function createTopic(string $topicName, array $config = []): bool
    {
        try {
            $exchangeName = $config['exchange'] ?? $topicName;
            $this->ensureExchange($exchangeName, $config);
            return true;
        } catch (\Exception $e) {
            return false;
        }
    }

    /**
     * List exchanges (topics)
     */
    public function listTopics(): array
    {
        // RabbitMQ doesn't provide direct API for listing exchanges
        // Would need management API integration
        return array_keys($this->exchanges);
    }

    /**
     * Get exchange information
     */
    public function getTopicInfo(string $topic): array
    {
        return $this->exchanges[$topic] ?? [];
    }

    /**
     * Declare exchange if not exists
     */
    private function ensureExchange(string $exchangeName, array $options = []): void
    {
        if (isset($this->exchanges[$exchangeName])) {
            return;
        }

        $type = $options['exchange_type'] ?? $this->config['exchange_type'];
        $passive = $options['passive'] ?? false;
        $durable = $options['durable'] ?? $this->config['durable'];
        $autoDelete = $options['auto_delete'] ?? $this->config['auto_delete'];
        $internal = $options['internal'] ?? false;
        $nowait = $options['nowait'] ?? false;
        $arguments = $options['arguments'] ?? [];

        $this->channel->exchange_declare(
            $exchangeName,
            $type,
            $passive,
            $durable,
            $autoDelete,
            $internal,
            $nowait,
            $arguments
        );

        $this->exchanges[$exchangeName] = [
            'name' => $exchangeName,
            'type' => $type,
            'durable' => $durable,
            'auto_delete' => $autoDelete,
            'created_at' => time()
        ];
    }

    /**
     * Get queue arguments
     */
    private function getQueueArguments(array $options): array
    {
        $arguments = [];

        // Dead letter exchange
        if (isset($options['dead_letter_exchange'])) {
            $arguments['x-dead-letter-exchange'] = $options['dead_letter_exchange'];
        } elseif ($this->config['dead_letter_exchange']) {
            $arguments['x-dead-letter-exchange'] = $this->config['dead_letter_exchange'];
        }

        // Message TTL
        if (isset($options['message_ttl'])) {
            $arguments['x-message-ttl'] = $options['message_ttl'];
        }

        // Queue TTL
        if (isset($options['queue_ttl'])) {
            $arguments['x-expires'] = $options['queue_ttl'];
        }

        // Max length
        if (isset($options['max_length'])) {
            $arguments['x-max-length'] = $options['max_length'];
        }

        // Priority queue
        if (isset($options['max_priority'])) {
            $arguments['x-max-priority'] = $options['max_priority'];
        }

        return $arguments;
    }

    /**
     * Handle incoming message
     */
    private function handleMessage(AMQPMessage $msg, callable $callback, array $options): void
    {
        try {
            // Deserialize message
            $messageData = $this->deserializeMessage($msg->getBody(), $options);
            
            // Prepare message info
            $messageInfo = [
                'body' => $messageData,
                'properties' => $msg->get_properties(),
                'delivery_info' => $msg->getDeliveryInfo(),
                'routing_key' => $msg->getRoutingKey(),
                'exchange' => $msg->getExchange()
            ];

            // Call user callback
            $result = $callback($messageInfo);

            // Acknowledge message if processing succeeded
            if ($result !== false) {
                $msg->ack();
            } else {
                // Reject and requeue
                $msg->nack(false, true);
            }
            
        } catch (\Exception $e) {
            $this->handleConsumerError($msg, $e, $options);
        }
    }

    /**
     * Connect to RabbitMQ
     */
    private function connect(): void
    {
        try {
            $this->connection = new AMQPStreamConnection(
                $this->config['host'],
                $this->config['port'],
                $this->config['user'],
                $this->config['password'],
                $this->config['vhost'],
                false, // insist
                'AMQPLAIN', // login_method
                null, // login_response
                'en_US', // locale
                $this->config['timeout'],
                $this->config['read_timeout'],
                null, // context
                false, // keepalive
                $this->config['heartbeat']
            );

            $this->channel = $this->connection->channel();
            
        } catch (\Exception $e) {
            throw new MessageQueueException("Failed to connect to RabbitMQ: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Serialize message
     */
    private function serializeMessage($message, array $options): string
    {
        $format = $options['format'] ?? 'json';
        
        switch ($format) {
            case 'json':
                return json_encode($message);
                
            case 'string':
                return (string) $message;
                
            case 'serialize':
                return serialize($message);
                
            default:
                return json_encode($message);
        }
    }

    /**
     * Deserialize message
     */
    private function deserializeMessage(string $data, array $options)
    {
        $format = $options['format'] ?? 'json';
        
        switch ($format) {
            case 'json':
                return json_decode($data, true);
                
            case 'string':
                return $data;
                
            case 'serialize':
                return unserialize($data);
                
            default:
                return json_decode($data, true);
        }
    }

    /**
     * Generate unique message ID
     */
    private function generateMessageId(): string
    {
        return uniqid('msg_', true);
    }

    /**
     * Handle publish errors
     */
    private function handlePublishError(string $topic, $message, \Exception $e, array $options): void
    {
        echo "RabbitMQ publish error on topic {$topic}: " . $e->getMessage() . "\n";
        
        // Try to send to dead letter queue
        if ($this->config['dead_letter_exchange']) {
            try {
                $deadLetterMessage = [
                    'original_topic' => $topic,
                    'error' => $e->getMessage(),
                    'timestamp' => time(),
                    'payload' => $message
                ];
                
                $this->publish('dlq', $deadLetterMessage, [
                    'exchange' => $this->config['dead_letter_exchange']
                ]);
            } catch (\Exception $dlqError) {
                echo "Failed to send to dead letter queue: " . $dlqError->getMessage() . "\n";
            }
        }
    }

    /**
     * Handle consumer errors
     */
    private function handleConsumerError(AMQPMessage $msg, \Exception $e, array $options): void
    {
        echo "RabbitMQ consumer error: " . $e->getMessage() . "\n";
        
        // Reject message and send to DLQ
        $msg->nack(false, false);
        
        // Implement retry logic
        $this->retryManager->handleFailedMessage($msg, $e);
    }

    /**
     * Close connections
     */
    public function __destruct()
    {
        if ($this->channel) {
            $this->channel->close();
        }
        
        if ($this->connection) {
            $this->connection->close();
        }
    }
} 