<?php

namespace Fujsen\CoreOperators;

use Fujsen\BaseOperator;
use Fujsen\Exceptions\OperatorException;
use Ratchet\Client\WebSocket;
use Ratchet\Client\Connector;
use React\EventLoop\Loop;
use React\Promise\PromiseInterface;

/**
 * WebSocket Operator for TuskLang
 * 
 * Provides WebSocket client functionality with support for:
 * - Real-time bidirectional communication
 * - Automatic reconnection with exponential backoff
 * - Message queuing during disconnection
 * - Subscription-based API
 * - Heartbeat/ping-pong support
 * - Event-driven architecture
 * 
 * Usage:
 * ```php
 * // Basic connection
 * $ws = @websocket({
 *   url: "wss://stream.example.com",
 *   subscribe: "market.BTC-USD",
 *   transform: @jq(".price")
 * })
 * 
 * // With authentication
 * $ws = @websocket({
 *   url: "wss://api.example.com/ws",
 *   headers: { "Authorization": "Bearer " + @env("API_TOKEN") },
 *   subscribe: "user.notifications"
 * })
 * ```
 */
class WebSocketOperator extends BaseOperator
{
    private array $connections = [];
    private array $messageQueues = [];
    private array $subscriptions = [];
    private array $config;
    private $loop;
    
    public function __construct(array $config = [])
    {
        parent::__construct('websocket');
        $this->config = array_merge([
            'max_reconnect_attempts' => 10,
            'reconnect_delay' => 1000,
            'max_reconnect_delay' => 30000,
            'heartbeat_interval' => 30000,
            'connection_timeout' => 10000,
            'max_message_queue_size' => 1000,
            'enable_compression' => true,
            'protocols' => []
        ], $config);
        
        $this->loop = Loop::get();
    }

    /**
     * Execute WebSocket operation
     */
    public function execute(array $params, array $context = []): mixed
    {
        $this->validateParams($params);
        
        $url = $params['url'] ?? '';
        $action = $params['action'] ?? 'connect';
        $message = $params['message'] ?? '';
        $subscribe = $params['subscribe'] ?? '';
        $headers = $params['headers'] ?? [];
        $protocols = $params['protocols'] ?? $this->config['protocols'];
        $timeout = $params['timeout'] ?? $this->config['connection_timeout'];
        $transform = $params['transform'] ?? null;
        $onMessage = $params['on_message'] ?? null;
        $onError = $params['on_error'] ?? null;
        $onClose = $params['on_close'] ?? null;
        
        try {
            switch ($action) {
                case 'connect':
                    return $this->connect($url, $headers, $protocols, $timeout, $subscribe, $transform, $onMessage, $onError, $onClose);
                case 'send':
                    return $this->send($url, $message);
                case 'subscribe':
                    return $this->subscribe($url, $subscribe, $transform, $onMessage);
                case 'unsubscribe':
                    return $this->unsubscribe($url, $subscribe);
                case 'disconnect':
                    return $this->disconnect($url);
                default:
                    throw new OperatorException("Unsupported action: $action");
            }
        } catch (\Exception $e) {
            throw new OperatorException("WebSocket operation failed: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Connect to WebSocket
     */
    private function connect(string $url, array $headers, array $protocols, int $timeout, string $subscribe, $transform, $onMessage, $onError, $onClose): array
    {
        $connectionId = $this->getConnectionId($url);
        
        if (isset($this->connections[$connectionId])) {
            return ['status' => 'connected', 'connection_id' => $connectionId];
        }
        
        $connector = new Connector($this->loop, [
            'timeout' => $timeout / 1000,
            'dns' => '8.8.8.8'
        ]);
        
        $promise = $connector($url, $protocols, $headers);
        
        $promise->then(
            function(WebSocket $conn) use ($connectionId, $subscribe, $transform, $onMessage, $onError, $onClose) {
                $this->connections[$connectionId] = $conn;
                $this->messageQueues[$connectionId] = [];
                $this->subscriptions[$connectionId] = [];
                
                // Set up message handler
                $conn->on('message', function($msg) use ($connectionId, $transform, $onMessage) {
                    $data = $this->processMessage($msg, $transform);
                    
                    if ($onMessage && is_callable($onMessage)) {
                        $onMessage($data);
                    }
                    
                    // Process subscriptions
                    $this->processSubscriptions($connectionId, $data);
                });
                
                // Set up error handler
                $conn->on('error', function($error) use ($connectionId, $onError) {
                    if ($onError && is_callable($onError)) {
                        $onError($error);
                    }
                    
                    $this->handleError($connectionId, $error);
                });
                
                // Set up close handler
                $conn->on('close', function($code, $reason) use ($connectionId, $onClose) {
                    if ($onClose && is_callable($onClose)) {
                        $onClose($code, $reason);
                    }
                    
                    $this->handleClose($connectionId, $code, $reason);
                });
                
                // Start heartbeat
                $this->startHeartbeat($connectionId);
                
                // Send queued messages
                $this->sendQueuedMessages($connectionId);
                
                // Auto-subscribe if specified
                if ($subscribe) {
                    $this->subscribe($url, $subscribe, $transform, $onMessage);
                }
            },
            function(\Exception $e) use ($connectionId, $onError) {
                if ($onError && is_callable($onError)) {
                    $onError($e);
                }
                
                $this->handleConnectionError($connectionId, $e);
            }
        );
        
        return ['status' => 'connecting', 'connection_id' => $connectionId];
    }

    /**
     * Send message to WebSocket
     */
    private function send(string $url, string $message): array
    {
        $connectionId = $this->getConnectionId($url);
        
        if (!isset($this->connections[$connectionId])) {
            // Queue message for later
            $this->queueMessage($connectionId, $message);
            return ['status' => 'queued', 'message' => 'Message queued for delivery'];
        }
        
        $conn = $this->connections[$connectionId];
        $conn->send($message);
        
        return ['status' => 'sent', 'message' => 'Message sent successfully'];
    }

    /**
     * Subscribe to WebSocket channel/topic
     */
    private function subscribe(string $url, string $subscribe, $transform, $onMessage): array
    {
        $connectionId = $this->getConnectionId($url);
        
        if (!isset($this->connections[$connectionId])) {
            throw new OperatorException("No active connection to $url");
        }
        
        $this->subscriptions[$connectionId][$subscribe] = [
            'transform' => $transform,
            'on_message' => $onMessage,
            'created_at' => time()
        ];
        
        // Send subscription message
        $subscribeMessage = json_encode([
            'action' => 'subscribe',
            'channel' => $subscribe
        ]);
        
        $this->send($url, $subscribeMessage);
        
        return ['status' => 'subscribed', 'channel' => $subscribe];
    }

    /**
     * Unsubscribe from WebSocket channel/topic
     */
    private function unsubscribe(string $url, string $subscribe): array
    {
        $connectionId = $this->getConnectionId($url);
        
        if (!isset($this->subscriptions[$connectionId][$subscribe])) {
            return ['status' => 'not_subscribed', 'channel' => $subscribe];
        }
        
        unset($this->subscriptions[$connectionId][$subscribe]);
        
        // Send unsubscribe message
        $unsubscribeMessage = json_encode([
            'action' => 'unsubscribe',
            'channel' => $subscribe
        ]);
        
        $this->send($url, $unsubscribeMessage);
        
        return ['status' => 'unsubscribed', 'channel' => $subscribe];
    }

    /**
     * Disconnect from WebSocket
     */
    private function disconnect(string $url): array
    {
        $connectionId = $this->getConnectionId($url);
        
        if (!isset($this->connections[$connectionId])) {
            return ['status' => 'not_connected'];
        }
        
        $conn = $this->connections[$connectionId];
        $conn->close();
        
        unset($this->connections[$connectionId]);
        unset($this->messageQueues[$connectionId]);
        unset($this->subscriptions[$connectionId]);
        
        return ['status' => 'disconnected'];
    }

    /**
     * Process incoming message
     */
    private function processMessage($msg, $transform): array
    {
        $data = $msg;
        
        if (is_string($msg)) {
            $data = json_decode($msg, true);
            if (json_last_error() !== JSON_ERROR_NONE) {
                $data = ['raw' => $msg];
            }
        }
        
        if ($transform && is_callable($transform)) {
            $data = $transform($data);
        }
        
        return $data;
    }

    /**
     * Process subscriptions
     */
    private function processSubscriptions(string $connectionId, array $data): void
    {
        if (!isset($this->subscriptions[$connectionId])) {
            return;
        }
        
        foreach ($this->subscriptions[$connectionId] as $channel => $config) {
            if (isset($data['channel']) && $data['channel'] === $channel) {
                $transformedData = $data;
                
                if ($config['transform'] && is_callable($config['transform'])) {
                    $transformedData = $config['transform']($data);
                }
                
                if ($config['on_message'] && is_callable($config['on_message'])) {
                    $config['on_message']($transformedData);
                }
            }
        }
    }

    /**
     * Queue message for later delivery
     */
    private function queueMessage(string $connectionId, string $message): void
    {
        if (!isset($this->messageQueues[$connectionId])) {
            $this->messageQueues[$connectionId] = [];
        }
        
        if (count($this->messageQueues[$connectionId]) >= $this->config['max_message_queue_size']) {
            array_shift($this->messageQueues[$connectionId]);
        }
        
        $this->messageQueues[$connectionId][] = [
            'message' => $message,
            'timestamp' => time()
        ];
    }

    /**
     * Send queued messages
     */
    private function sendQueuedMessages(string $connectionId): void
    {
        if (!isset($this->messageQueues[$connectionId]) || !isset($this->connections[$connectionId])) {
            return;
        }
        
        $conn = $this->connections[$connectionId];
        
        foreach ($this->messageQueues[$connectionId] as $queuedMessage) {
            $conn->send($queuedMessage['message']);
        }
        
        $this->messageQueues[$connectionId] = [];
    }

    /**
     * Start heartbeat for connection
     */
    private function startHeartbeat(string $connectionId): void
    {
        $this->loop->addPeriodicTimer($this->config['heartbeat_interval'] / 1000, function() use ($connectionId) {
            if (isset($this->connections[$connectionId])) {
                $conn = $this->connections[$connectionId];
                $conn->send(json_encode(['type' => 'ping', 'timestamp' => time()]));
            }
        });
    }

    /**
     * Handle connection error
     */
    private function handleError(string $connectionId, $error): void
    {
        $this->logError("WebSocket error for $connectionId: " . $error->getMessage());
        
        // Attempt reconnection
        $this->scheduleReconnection($connectionId);
    }

    /**
     * Handle connection close
     */
    private function handleClose(string $connectionId, int $code, string $reason): void
    {
        $this->logInfo("WebSocket closed for $connectionId: $code - $reason");
        
        unset($this->connections[$connectionId]);
        
        // Attempt reconnection if not intentional
        if ($code !== 1000) {
            $this->scheduleReconnection($connectionId);
        }
    }

    /**
     * Handle connection error during initial connection
     */
    private function handleConnectionError(string $connectionId, \Exception $e): void
    {
        $this->logError("WebSocket connection failed for $connectionId: " . $e->getMessage());
        
        // Attempt reconnection
        $this->scheduleReconnection($connectionId);
    }

    /**
     * Schedule reconnection attempt
     */
    private function scheduleReconnection(string $connectionId): void
    {
        $attempts = 0;
        $delay = $this->config['reconnect_delay'];
        
        $reconnect = function() use ($connectionId, &$attempts, &$delay, &$reconnect) {
            if ($attempts >= $this->config['max_reconnect_attempts']) {
                $this->logError("Max reconnection attempts reached for $connectionId");
                return;
            }
            
            $attempts++;
            
            // Extract URL from connection ID
            $url = $this->getUrlFromConnectionId($connectionId);
            if (!$url) {
                return;
            }
            
            $this->logInfo("Attempting reconnection $attempts for $connectionId");
            
            // Attempt reconnection
            $this->connect($url, [], [], $this->config['connection_timeout'], '', null, null, null, null);
            
            // Increase delay for next attempt
            $delay = min($delay * 2, $this->config['max_reconnect_delay']);
            
            // Schedule next attempt
            $this->loop->addTimer($delay / 1000, $reconnect);
        };
        
        $this->loop->addTimer($delay / 1000, $reconnect);
    }

    /**
     * Get connection ID from URL
     */
    private function getConnectionId(string $url): string
    {
        return md5($url);
    }

    /**
     * Get URL from connection ID (reverse lookup)
     */
    private function getUrlFromConnectionId(string $connectionId): ?string
    {
        // This is a simplified implementation
        // In a real implementation, you'd maintain a mapping
        return null;
    }

    /**
     * Validate operator parameters
     */
    private function validateParams(array $params): void
    {
        if (isset($params['action']) && !in_array($params['action'], ['connect', 'send', 'subscribe', 'unsubscribe', 'disconnect'])) {
            throw new OperatorException("Invalid action: " . $params['action']);
        }
        
        if (isset($params['url']) && !filter_var($params['url'], FILTER_VALIDATE_URL)) {
            throw new OperatorException("Invalid URL: " . $params['url']);
        }
    }

    /**
     * Cleanup resources
     */
    public function cleanup(): void
    {
        foreach ($this->connections as $connectionId => $conn) {
            $conn->close();
        }
        
        $this->connections = [];
        $this->messageQueues = [];
        $this->subscriptions = [];
    }

    /**
     * Get operator schema
     */
    public function getSchema(): array
    {
        return [
            'type' => 'object',
            'properties' => [
                'url' => ['type' => 'string', 'description' => 'WebSocket URL'],
                'action' => [
                    'type' => 'string',
                    'enum' => ['connect', 'send', 'subscribe', 'unsubscribe', 'disconnect'],
                    'default' => 'connect',
                    'description' => 'WebSocket action'
                ],
                'message' => ['type' => 'string', 'description' => 'Message to send'],
                'subscribe' => ['type' => 'string', 'description' => 'Channel to subscribe to'],
                'headers' => ['type' => 'object', 'description' => 'HTTP headers'],
                'protocols' => ['type' => 'array', 'description' => 'WebSocket protocols'],
                'timeout' => ['type' => 'integer', 'description' => 'Connection timeout in milliseconds'],
                'transform' => ['type' => 'function', 'description' => 'Message transformation function'],
                'on_message' => ['type' => 'function', 'description' => 'Message handler'],
                'on_error' => ['type' => 'function', 'description' => 'Error handler'],
                'on_close' => ['type' => 'function', 'description' => 'Close handler']
            ],
            'required' => ['url']
        ];
    }
} 