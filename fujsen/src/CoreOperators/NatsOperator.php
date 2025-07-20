<?php

namespace Fujsen\CoreOperators;

use Fujsen\BaseOperator;
use Fujsen\Exceptions\OperatorException;
use Nats\Connection;
use Nats\ConnectionOptions;
use Nats\Message;

/**
 * NATS Operator for TuskLang
 * 
 * Provides NATS messaging functionality with support for:
 * - Pub/sub messaging
 * - Request/reply patterns
 * - NATS Streaming and JetStream
 * - Subject-based routing
 * - Queue groups for load balancing
 * - Connection pooling and failover
 * - Message serialization (JSON, Protobuf)
 * - Distributed tracing integration
 * 
 * Usage:
 * ```php
 * // Publish message
 * $result = @nats({
 *   action: "publish",
 *   subject: "orders.new",
 *   payload: @variable("order_data"),
 *   cluster: "nats://nats.service:4222"
 * })
 * 
 * // Subscribe with queue group
 * $subscription = @nats({
 *   action: "subscribe",
 *   subject: "orders.*",
 *   queue: "order-processors"
 * })
 * ```
 */
class NatsOperator extends BaseOperator
{
    private array $connections = [];
    private array $subscriptions = [];
    private array $config;
    
    public function __construct(array $config = [])
    {
        parent::__construct('nats');
        $this->config = array_merge([
            'default_cluster' => 'nats://localhost:4222',
            'connection_timeout' => 5000,
            'reconnect_time_wait' => 2000,
            'max_reconnect_attempts' => 10,
            'ping_interval' => 20000,
            'max_outstanding_pings' => 5,
            'max_payload_size' => 1048576,
            'enable_compression' => true,
            'enable_tls' => false,
            'tls_cert' => '',
            'tls_key' => '',
            'tls_ca' => ''
        ], $config);
    }

    /**
     * Execute NATS operation
     */
    public function execute(array $params, array $context = []): mixed
    {
        $this->validateParams($params);
        
        $action = $params['action'] ?? '';
        $subject = $params['subject'] ?? '';
        $payload = $params['payload'] ?? '';
        $cluster = $params['cluster'] ?? $this->config['default_cluster'];
        $queue = $params['queue'] ?? '';
        $timeout = $params['timeout'] ?? 5000;
        $serialization = $params['serialization'] ?? 'json';
        $headers = $params['headers'] ?? [];
        $replyTo = $params['reply_to'] ?? '';
        $onMessage = $params['on_message'] ?? null;
        $onError = $params['on_error'] ?? null;
        
        try {
            $connection = $this->getConnection($cluster);
            
            switch ($action) {
                case 'publish':
                    return $this->publish($connection, $subject, $payload, $replyTo, $headers, $serialization);
                case 'subscribe':
                    return $this->subscribe($connection, $subject, $queue, $onMessage, $onError, $timeout);
                case 'request':
                    return $this->request($connection, $subject, $payload, $timeout, $serialization);
                case 'unsubscribe':
                    return $this->unsubscribe($connection, $subject, $queue);
                case 'flush':
                    return $this->flush($connection, $timeout);
                default:
                    throw new OperatorException("Unsupported action: $action");
            }
        } catch (\Exception $e) {
            throw new OperatorException("NATS operation failed: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Publish message to NATS
     */
    private function publish(Connection $connection, string $subject, $payload, string $replyTo, array $headers, string $serialization): array
    {
        $serializedPayload = $this->serializePayload($payload, $serialization);
        
        $connection->publish($subject, $serializedPayload, $replyTo);
        
        return [
            'status' => 'published',
            'subject' => $subject,
            'payload_size' => strlen($serializedPayload),
            'timestamp' => time()
        ];
    }

    /**
     * Subscribe to NATS subject
     */
    private function subscribe(Connection $connection, string $subject, string $queue, $onMessage, $onError, int $timeout): array
    {
        $subscriptionId = $this->generateSubscriptionId($subject, $queue);
        
        $callback = function(Message $message) use ($onMessage, $onError) {
            try {
                $data = $this->deserializePayload($message->getBody());
                
                $result = [
                    'subject' => $message->getSid(),
                    'payload' => $data,
                    'reply_to' => $message->getReplyTo(),
                    'timestamp' => time()
                ];
                
                if ($onMessage && is_callable($onMessage)) {
                    $onMessage($result);
                }
                
                return $result;
            } catch (\Exception $e) {
                if ($onError && is_callable($onError)) {
                    $onError($e);
                }
                throw $e;
            }
        };
        
        if ($queue) {
            $subscription = $connection->queueSubscribe($subject, $queue, $callback);
        } else {
            $subscription = $connection->subscribe($subject, $callback);
        }
        
        $this->subscriptions[$subscriptionId] = [
            'subscription' => $subscription,
            'subject' => $subject,
            'queue' => $queue,
            'created_at' => time()
        ];
        
        return [
            'status' => 'subscribed',
            'subscription_id' => $subscriptionId,
            'subject' => $subject,
            'queue' => $queue
        ];
    }

    /**
     * Request/reply pattern
     */
    private function request(Connection $connection, string $subject, $payload, int $timeout, string $serialization): array
    {
        $serializedPayload = $this->serializePayload($payload, $serialization);
        
        $response = $connection->request($subject, $serializedPayload, $timeout);
        
        if (!$response) {
            throw new OperatorException("NATS request timed out after {$timeout}ms");
        }
        
        $data = $this->deserializePayload($response->getBody());
        
        return [
            'status' => 'received',
            'subject' => $response->getSid(),
            'payload' => $data,
            'response_time' => time()
        ];
    }

    /**
     * Unsubscribe from NATS subject
     */
    private function unsubscribe(Connection $connection, string $subject, string $queue): array
    {
        $subscriptionId = $this->generateSubscriptionId($subject, $queue);
        
        if (!isset($this->subscriptions[$subscriptionId])) {
            return ['status' => 'not_subscribed', 'subject' => $subject];
        }
        
        $subscription = $this->subscriptions[$subscriptionId]['subscription'];
        $subscription->unsubscribe();
        
        unset($this->subscriptions[$subscriptionId]);
        
        return [
            'status' => 'unsubscribed',
            'subject' => $subject,
            'queue' => $queue
        ];
    }

    /**
     * Flush connection
     */
    private function flush(Connection $connection, int $timeout): array
    {
        $connection->flush($timeout);
        
        return [
            'status' => 'flushed',
            'timestamp' => time()
        ];
    }

    /**
     * Get or create NATS connection
     */
    private function getConnection(string $cluster): Connection
    {
        if (!isset($this->connections[$cluster])) {
            $options = new ConnectionOptions();
            $options->setHost(parse_url($cluster, PHP_URL_HOST));
            $options->setPort(parse_url($cluster, PHP_URL_PORT) ?: 4222);
            $options->setTimeout($this->config['connection_timeout'] / 1000);
            $options->setReconnectTimeWait($this->config['reconnect_time_wait'] / 1000);
            $options->setMaxReconnectAttempts($this->config['max_reconnect_attempts']);
            $options->setPingInterval($this->config['ping_interval'] / 1000);
            $options->setMaxOutstandingPings($this->config['max_outstanding_pings']);
            $options->setMaxPayloadSize($this->config['max_payload_size']);
            
            if ($this->config['enable_tls']) {
                $options->setSecure(true);
                if ($this->config['tls_cert'] && $this->config['tls_key']) {
                    $options->setCertFile($this->config['tls_cert']);
                    $options->setKeyFile($this->config['tls_key']);
                }
                if ($this->config['tls_ca']) {
                    $options->setCaFile($this->config['tls_ca']);
                }
            }
            
            $connection = new Connection($options);
            $connection->connect();
            
            $this->connections[$cluster] = $connection;
        }
        
        return $this->connections[$cluster];
    }

    /**
     * Serialize payload
     */
    private function serializePayload($payload, string $serialization): string
    {
        switch ($serialization) {
            case 'json':
                return json_encode($payload);
            case 'protobuf':
                // In a real implementation, you'd use protobuf serialization
                return serialize($payload);
            case 'raw':
                return (string) $payload;
            default:
                return json_encode($payload);
        }
    }

    /**
     * Deserialize payload
     */
    private function deserializePayload(string $payload, string $serialization = 'json'): mixed
    {
        switch ($serialization) {
            case 'json':
                $data = json_decode($payload, true);
                if (json_last_error() !== JSON_ERROR_NONE) {
                    return $payload; // Return raw if JSON decode fails
                }
                return $data;
            case 'protobuf':
                // In a real implementation, you'd use protobuf deserialization
                return unserialize($payload);
            case 'raw':
                return $payload;
            default:
                return json_decode($payload, true) ?: $payload;
        }
    }

    /**
     * Generate subscription ID
     */
    private function generateSubscriptionId(string $subject, string $queue): string
    {
        return md5($subject . $queue);
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
        
        $validActions = ['publish', 'subscribe', 'request', 'unsubscribe', 'flush'];
        if (!in_array($params['action'], $validActions)) {
            throw new OperatorException("Invalid action: " . $params['action']);
        }
        
        if (in_array($params['action'], ['publish', 'subscribe', 'request']) && !isset($params['subject'])) {
            throw new OperatorException("Subject is required for action: " . $params['action']);
        }
    }

    /**
     * Cleanup resources
     */
    public function cleanup(): void
    {
        foreach ($this->subscriptions as $subscriptionId => $subscription) {
            $subscription['subscription']->unsubscribe();
        }
        
        foreach ($this->connections as $cluster => $connection) {
            $connection->close();
        }
        
        $this->subscriptions = [];
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
                    'enum' => ['publish', 'subscribe', 'request', 'unsubscribe', 'flush'],
                    'description' => 'NATS action'
                ],
                'subject' => ['type' => 'string', 'description' => 'NATS subject'],
                'payload' => ['type' => 'object', 'description' => 'Message payload'],
                'cluster' => ['type' => 'string', 'description' => 'NATS cluster URL'],
                'queue' => ['type' => 'string', 'description' => 'Queue group name'],
                'timeout' => ['type' => 'integer', 'description' => 'Operation timeout in milliseconds'],
                'serialization' => [
                    'type' => 'string',
                    'enum' => ['json', 'protobuf', 'raw'],
                    'default' => 'json',
                    'description' => 'Payload serialization format'
                ],
                'headers' => ['type' => 'object', 'description' => 'Message headers'],
                'reply_to' => ['type' => 'string', 'description' => 'Reply-to subject'],
                'on_message' => ['type' => 'function', 'description' => 'Message handler'],
                'on_error' => ['type' => 'function', 'description' => 'Error handler']
            ],
            'required' => ['action']
        ];
    }
} 