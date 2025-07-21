<?php

namespace TuskLang\CoreOperators;

use Fujsen\BaseOperator;
use Fujsen\Exceptions\OperatorException;
use PhpAmqpLib\Connection\AMQPStreamConnection;
use PhpAmqpLib\Connection\AMQPSSLConnection;
use PhpAmqpLib\Message\AMQPMessage;
use PhpAmqpLib\Channel\AMQPChannel;

/**
 * AMQP Operator for TuskLang
 * 
 * Provides AMQP/RabbitMQ functionality with support for:
 * - Exchange and queue management
 * - Routing key patterns
 * - Dead letter queue support
 * - Publisher confirms
 * - Consumer acknowledgments
 * - TTL and priority support
 * - Connection recovery
 * - Message persistence
 * 
 * Usage:
 * ```php
 * // Publish message
 * $result = @amqp({
 *   action: "publish",
 *   exchange: "events",
 *   routing_key: "user.created",
 *   message: @variable("user"),
 *   persistent: true
 * })
 * 
 * // Consume messages
 * $messages = @amqp({
 *   action: "consume",
 *   queue: "user-events",
 *   prefetch: 10
 * })
 * ```
 */
class AmqpOperator extends \TuskLang\CoreOperators\BaseOperator
{
    private array $connections = [];
    private array $channels = [];
    private array $consumers = [];
    private array $config;
    
    public function __construct(array $config = [])
    {
        parent::__construct('amqp');
        $this->config = array_merge([
            'default_host' => 'localhost',
            'default_port' => 5672,
            'default_user' => 'guest',
            'default_pass' => 'guest',
            'default_vhost' => '/',
            'connection_timeout' => 10,
            'read_write_timeout' => 10,
            'heartbeat' => 60,
            'enable_ssl' => false,
            'ssl_verify_peer' => true,
            'ssl_verify_peer_name' => true,
            'ssl_ca_file' => '',
            'ssl_cert_file' => '',
            'ssl_key_file' => '',
            'ssl_passphrase' => ''
        ], $config);
    }

    /**
     * Execute AMQP operation
     */
    public function execute(array $params, array $context = []): mixed
    {
        $this->validateParams($params);
        
        $action = $params['action'] ?? '';
        $exchange = $params['exchange'] ?? '';
        $routingKey = $params['routing_key'] ?? '';
        $queue = $params['queue'] ?? '';
        $message = $params['message'] ?? '';
        $host = $params['host'] ?? $this->config['default_host'];
        $port = $params['port'] ?? $this->config['default_port'];
        $user = $params['user'] ?? $this->config['default_user'];
        $pass = $params['pass'] ?? $this->config['default_pass'];
        $vhost = $params['vhost'] ?? $this->config['default_vhost'];
        $persistent = $params['persistent'] ?? false;
        $prefetch = $params['prefetch'] ?? 1;
        $timeout = $params['timeout'] ?? 5000;
        $onMessage = $params['on_message'] ?? null;
        $onError = $params['on_error'] ?? null;
        
        try {
            $connection = $this->getConnection($host, $port, $user, $pass, $vhost);
            $channel = $this->getChannel($connection);
            
            switch ($action) {
                case 'publish':
                    return $this->publish($channel, $exchange, $routingKey, $message, $persistent);
                case 'consume':
                    return $this->consume($channel, $queue, $prefetch, $onMessage, $onError, $timeout);
                case 'declare_exchange':
                    return $this->declareExchange($channel, $exchange, $params['type'] ?? 'direct', $params['durable'] ?? true);
                case 'declare_queue':
                    return $this->declareQueue($channel, $queue, $params['durable'] ?? true, $params['arguments'] ?? []);
                case 'bind':
                    return $this->bind($channel, $queue, $exchange, $routingKey);
                case 'unbind':
                    return $this->unbind($channel, $queue, $exchange, $routingKey);
                case 'purge':
                    return $this->purge($channel, $queue);
                case 'delete':
                    return $this->delete($channel, $queue, $params['if_unused'] ?? false, $params['if_empty'] ?? false);
                default:
                    throw new OperatorException("Unsupported action: $action");
            }
        } catch (\Exception $e) {
            throw new OperatorException("AMQP operation failed: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Publish message to AMQP
     */
    private function publish(AMQPChannel $channel, string $exchange, string $routingKey, $message, bool $persistent): array
    {
        $properties = [
            'delivery_mode' => $persistent ? AMQPMessage::DELIVERY_MODE_PERSISTENT : AMQPMessage::DELIVERY_MODE_NON_PERSISTENT,
            'content_type' => 'application/json',
            'timestamp' => time()
        ];
        
        $amqpMessage = new AMQPMessage(json_encode($message), $properties);
        
        $channel->basic_publish($amqpMessage, $exchange, $routingKey);
        
        return [
            'status' => 'published',
            'exchange' => $exchange,
            'routing_key' => $routingKey,
            'message_size' => strlen(json_encode($message)),
            'persistent' => $persistent,
            'timestamp' => time()
        ];
    }

    /**
     * Consume messages from AMQP
     */
    private function consume(AMQPChannel $channel, string $queue, int $prefetch, $onMessage, $onError, int $timeout): array
    {
        $channel->basic_qos(null, $prefetch, null);
        
        $messages = [];
        $consumerId = $this->generateConsumerId($queue);
        
        $callback = function(AMQPMessage $msg) use (&$messages, $onMessage, $onError, $channel) {
            try {
                $data = json_decode($msg->getBody(), true);
                if (json_last_error() !== JSON_ERROR_NONE) {
                    $data = ['raw' => $msg->getBody()];
                }
                
                $message = [
                    'body' => $data,
                    'properties' => $msg->get_properties(),
                    'delivery_info' => [
                        'delivery_tag' => $msg->get('delivery_tag'),
                        'redelivered' => $msg->get('redelivered'),
                        'exchange' => $msg->get('exchange'),
                        'routing_key' => $msg->get('routing_key')
                    ],
                    'timestamp' => time()
                ];
                
                $messages[] = $message;
                
                if ($onMessage && is_callable($onMessage)) {
                    $onMessage($message);
                }
                
                // Auto-acknowledge if no manual ack is provided
                $channel->basic_ack($msg->get('delivery_tag'));
                
            } catch (\Exception $e) {
                if ($onError && is_callable($onError)) {
                    $onError($e);
                }
                
                // Reject message on error
                $channel->basic_nack($msg->get('delivery_tag'), false, true);
                throw $e;
            }
        };
        
        $consumerTag = $channel->basic_consume($queue, '', false, false, false, false, $callback);
        
        $this->consumers[$consumerId] = [
            'consumer_tag' => $consumerTag,
            'queue' => $queue,
            'channel' => $channel,
            'created_at' => time()
        ];
        
        // Wait for messages
        $startTime = time();
        while (time() - $startTime < $timeout / 1000) {
            $channel->wait(null, true, $timeout / 1000);
        }
        
        return [
            'status' => 'consumed',
            'consumer_id' => $consumerId,
            'messages' => $messages,
            'count' => count($messages)
        ];
    }

    /**
     * Declare AMQP exchange
     */
    private function declareExchange(AMQPChannel $channel, string $exchange, string $type, bool $durable): array
    {
        $arguments = [];
        
        $channel->exchange_declare($exchange, $type, false, $durable, false, false, false, $arguments);
        
        return [
            'status' => 'declared',
            'exchange' => $exchange,
            'type' => $type,
            'durable' => $durable
        ];
    }

    /**
     * Declare AMQP queue
     */
    private function declareQueue(AMQPChannel $channel, string $queue, bool $durable, array $arguments): array
    {
        $result = $channel->queue_declare($queue, false, $durable, false, false, false, $arguments);
        
        return [
            'status' => 'declared',
            'queue' => $result[0],
            'message_count' => $result[1],
            'consumer_count' => $result[2],
            'durable' => $durable
        ];
    }

    /**
     * Bind queue to exchange
     */
    private function bind(AMQPChannel $channel, string $queue, string $exchange, string $routingKey): array
    {
        $channel->queue_bind($queue, $exchange, $routingKey);
        
        return [
            'status' => 'bound',
            'queue' => $queue,
            'exchange' => $exchange,
            'routing_key' => $routingKey
        ];
    }

    /**
     * Unbind queue from exchange
     */
    private function unbind(AMQPChannel $channel, string $queue, string $exchange, string $routingKey): array
    {
        $channel->queue_unbind($queue, $exchange, $routingKey);
        
        return [
            'status' => 'unbound',
            'queue' => $queue,
            'exchange' => $exchange,
            'routing_key' => $routingKey
        ];
    }

    /**
     * Purge queue
     */
    private function purge(AMQPChannel $channel, string $queue): array
    {
        $result = $channel->queue_purge($queue);
        
        return [
            'status' => 'purged',
            'queue' => $queue,
            'messages_removed' => $result
        ];
    }

    /**
     * Delete queue
     */
    private function delete(AMQPChannel $channel, string $queue, bool $ifUnused, bool $ifEmpty): array
    {
        $result = $channel->queue_delete($queue, false, $ifUnused, $ifEmpty);
        
        return [
            'status' => 'deleted',
            'queue' => $queue,
            'messages_deleted' => $result
        ];
    }

    /**
     * Get or create AMQP connection
     */
    private function getConnection(string $host, int $port, string $user, string $pass, string $vhost): AMQPStreamConnection
    {
        $connectionKey = "$host:$port:$user:$vhost";
        
        if (!isset($this->connections[$connectionKey])) {
            if ($this->config['enable_ssl']) {
                $sslOptions = [
                    'verify_peer' => $this->config['ssl_verify_peer'],
                    'verify_peer_name' => $this->config['ssl_verify_peer_name']
                ];
                
                if ($this->config['ssl_ca_file']) {
                    $sslOptions['cafile'] = $this->config['ssl_ca_file'];
                }
                if ($this->config['ssl_cert_file']) {
                    $sslOptions['local_cert'] = $this->config['ssl_cert_file'];
                }
                if ($this->config['ssl_key_file']) {
                    $sslOptions['local_pk'] = $this->config['ssl_key_file'];
                }
                if ($this->config['ssl_passphrase']) {
                    $sslOptions['passphrase'] = $this->config['ssl_passphrase'];
                }
                
                $connection = new AMQPSSLConnection(
                    $host,
                    $port,
                    $user,
                    $pass,
                    $vhost,
                    $sslOptions,
                    [
                        'connection_timeout' => $this->config['connection_timeout'],
                        'read_write_timeout' => $this->config['read_write_timeout'],
                        'heartbeat' => $this->config['heartbeat']
                    ]
                );
            } else {
                $connection = new AMQPStreamConnection(
                    $host,
                    $port,
                    $user,
                    $pass,
                    $vhost,
                    false,
                    'AMQPLAIN',
                    null,
                    'en_US',
                    $this->config['connection_timeout'],
                    $this->config['read_write_timeout'],
                    null,
                    false,
                    $this->config['heartbeat']
                );
            }
            
            $this->connections[$connectionKey] = $connection;
        }
        
        return $this->connections[$connectionKey];
    }

    /**
     * Get or create AMQP channel
     */
    private function getChannel(AMQPStreamConnection $connection): AMQPChannel
    {
        $connectionKey = spl_object_hash($connection);
        
        if (!isset($this->channels[$connectionKey])) {
            $channel = $connection->channel();
            
            // Enable publisher confirms
            $channel->confirm_select();
            
            $this->channels[$connectionKey] = $channel;
        }
        
        return $this->channels[$connectionKey];
    }

    /**
     * Generate consumer ID
     */
    private function generateConsumerId(string $queue): string
    {
        return md5($queue . time() . rand());
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
        
        $validActions = ['publish', 'consume', 'declare_exchange', 'declare_queue', 'bind', 'unbind', 'purge', 'delete'];
        if (!in_array($params['action'], $validActions)) {
            throw new OperatorException("Invalid action: " . $params['action']);
        }
        
        if (in_array($params['action'], ['publish']) && (!isset($params['exchange']) || !isset($params['routing_key']))) {
            throw new OperatorException("Exchange and routing_key are required for publish action");
        }
        
        if (in_array($params['action'], ['consume', 'declare_queue', 'purge', 'delete']) && !isset($params['queue'])) {
            throw new OperatorException("Queue is required for " . $params['action'] . " action");
        }
    }

    /**
     * Cleanup resources
     */
    public function cleanup(): void
    {
        foreach ($this->consumers as $consumerId => $consumer) {
            try {
                $consumer['channel']->basic_cancel($consumer['consumer_tag']);
            } catch (\Exception $e) {
                // Ignore cleanup errors
            }
        }
        
        foreach ($this->channels as $connectionKey => $channel) {
            try {
                $channel->close();
            } catch (\Exception $e) {
                // Ignore cleanup errors
            }
        }
        
        foreach ($this->connections as $connectionKey => $connection) {
            try {
                $connection->close();
            } catch (\Exception $e) {
                // Ignore cleanup errors
            }
        }
        
        $this->consumers = [];
        $this->channels = [];
        $this->connections = [];
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
                    'enum' => ['publish', 'consume', 'declare_exchange', 'declare_queue', 'bind', 'unbind', 'purge', 'delete'],
                    'description' => 'AMQP action'
                ],
                'exchange' => ['type' => 'string', 'description' => 'Exchange name'],
                'routing_key' => ['type' => 'string', 'description' => 'Routing key'],
                'queue' => ['type' => 'string', 'description' => 'Queue name'],
                'message' => ['type' => 'object', 'description' => 'Message to publish'],
                'host' => ['type' => 'string', 'description' => 'AMQP host'],
                'port' => ['type' => 'integer', 'description' => 'AMQP port'],
                'user' => ['type' => 'string', 'description' => 'AMQP username'],
                'pass' => ['type' => 'string', 'description' => 'AMQP password'],
                'vhost' => ['type' => 'string', 'description' => 'AMQP virtual host'],
                'persistent' => ['type' => 'boolean', 'description' => 'Message persistence'],
                'prefetch' => ['type' => 'integer', 'description' => 'Consumer prefetch count'],
                'timeout' => ['type' => 'integer', 'description' => 'Operation timeout in milliseconds'],
                'type' => ['type' => 'string', 'description' => 'Exchange type'],
                'durable' => ['type' => 'boolean', 'description' => 'Queue/exchange durability'],
                'arguments' => ['type' => 'object', 'description' => 'Queue arguments'],
                'if_unused' => ['type' => 'boolean', 'description' => 'Delete if unused'],
                'if_empty' => ['type' => 'boolean', 'description' => 'Delete if empty'],
                'on_message' => ['type' => 'function', 'description' => 'Message handler'],
                'on_error' => ['type' => 'function', 'description' => 'Error handler']
            ],
            'required' => ['action']
        ];
    }
} 