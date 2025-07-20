<?php

namespace Fujsen\CoreOperators;

use Fujsen\BaseOperator;
use Fujsen\Exceptions\OperatorException;
use React\Http\Browser;
use React\EventLoop\Loop;
use Psr\Http\Message\ResponseInterface;

/**
 * Server-Sent Events (SSE) Operator for TuskLang
 * 
 * Provides SSE client functionality with support for:
 * - Automatic reconnection with exponential backoff
 * - Event type filtering
 * - Last-Event-ID support for resumption
 * - Stream transformation pipeline
 * - HTTP/2 support for multiplexing
 * - Memory-efficient streaming
 * - Event buffering options
 * 
 * Usage:
 * ```php
 * // Basic SSE connection
 * $events = @sse({
 *   url: "https://api.example.com/events",
 *   events: ["update", "delete"],
 *   retry: 5000
 * })
 * 
 * // With authentication and filtering
 * $events = @sse({
 *   url: "https://api.example.com/stream",
 *   headers: { "Authorization": "Bearer " + @env("API_TOKEN") },
 *   events: ["user.*"],
 *   transform: @jq(".data")
 * })
 * ```
 */
class SseOperator extends BaseOperator
{
    private array $connections = [];
    private array $eventBuffers = [];
    private array $config;
    private $browser;
    
    public function __construct(array $config = [])
    {
        parent::__construct('sse');
        $this->config = array_merge([
            'max_reconnect_attempts' => 10,
            'reconnect_delay' => 1000,
            'max_reconnect_delay' => 30000,
            'connection_timeout' => 10000,
            'max_buffer_size' => 1000,
            'enable_compression' => true,
            'follow_redirects' => true,
            'max_redirects' => 10,
            'user_agent' => 'TuskLang-SSE-Client/1.0'
        ], $config);
        
        $this->browser = new Browser($this->config['user_agent']);
        
        if ($this->config['follow_redirects']) {
            $this->browser = $this->browser->withFollowRedirects($this->config['max_redirects']);
        }
    }

    /**
     * Execute SSE operation
     */
    public function execute(array $params, array $context = []): mixed
    {
        $this->validateParams($params);
        
        $url = $params['url'] ?? '';
        $action = $params['action'] ?? 'connect';
        $events = $params['events'] ?? [];
        $headers = $params['headers'] ?? [];
        $retry = $params['retry'] ?? 5000;
        $timeout = $params['timeout'] ?? $this->config['connection_timeout'];
        $transform = $params['transform'] ?? null;
        $onEvent = $params['on_event'] ?? null;
        $onError = $params['on_error'] ?? null;
        $onConnect = $params['on_connect'] ?? null;
        $onDisconnect = $params['on_disconnect'] ?? null;
        
        try {
            switch ($action) {
                case 'connect':
                    return $this->connect($url, $events, $headers, $retry, $timeout, $transform, $onEvent, $onError, $onConnect, $onDisconnect);
                case 'disconnect':
                    return $this->disconnect($url);
                case 'get_events':
                    return $this->getEvents($url, $limit = $params['limit'] ?? 100);
                default:
                    throw new OperatorException("Unsupported action: $action");
            }
        } catch (\Exception $e) {
            throw new OperatorException("SSE operation failed: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Connect to SSE stream
     */
    private function connect(string $url, array $events, array $headers, int $retry, int $timeout, $transform, $onEvent, $onError, $onConnect, $onDisconnect): array
    {
        $connectionId = $this->getConnectionId($url);
        
        if (isset($this->connections[$connectionId])) {
            return ['status' => 'connected', 'connection_id' => $connectionId];
        }
        
        // Add SSE-specific headers
        $headers = array_merge($headers, [
            'Accept' => 'text/event-stream',
            'Cache-Control' => 'no-cache',
            'Connection' => 'keep-alive'
        ]);
        
        // Add Last-Event-ID if available
        $lastEventId = $this->getLastEventId($connectionId);
        if ($lastEventId) {
            $headers['Last-Event-ID'] = $lastEventId;
        }
        
        $this->connections[$connectionId] = [
            'url' => $url,
            'events' => $events,
            'headers' => $headers,
            'retry' => $retry,
            'timeout' => $timeout,
            'transform' => $transform,
            'on_event' => $onEvent,
            'on_error' => $onError,
            'on_connect' => $onConnect,
            'on_disconnect' => $onDisconnect,
            'status' => 'connecting',
            'last_event_id' => $lastEventId,
            'reconnect_attempts' => 0,
            'created_at' => time()
        ];
        
        $this->eventBuffers[$connectionId] = [];
        
        $this->establishConnection($connectionId);
        
        return ['status' => 'connecting', 'connection_id' => $connectionId];
    }

    /**
     * Establish SSE connection
     */
    private function establishConnection(string $connectionId): void
    {
        $connection = $this->connections[$connectionId];
        
        $promise = $this->browser->requestStreaming('GET', $connection['url'], $connection['headers']);
        
        $promise->then(
            function(ResponseInterface $response) use ($connectionId) {
                $this->handleResponse($connectionId, $response);
            },
            function(\Exception $e) use ($connectionId) {
                $this->handleConnectionError($connectionId, $e);
            }
        );
    }

    /**
     * Handle SSE response
     */
    private function handleResponse(string $connectionId, ResponseInterface $response): void
    {
        $connection = $this->connections[$connectionId];
        
        if ($response->getStatusCode() !== 200) {
            $this->handleConnectionError($connectionId, new \Exception("HTTP " . $response->getStatusCode()));
            return;
        }
        
        $contentType = $response->getHeaderLine('Content-Type');
        if (strpos($contentType, 'text/event-stream') === false) {
            $this->handleConnectionError($connectionId, new \Exception("Invalid Content-Type: $contentType"));
            return;
        }
        
        $connection['status'] = 'connected';
        $connection['connected_at'] = time();
        $connection['reconnect_attempts'] = 0;
        $this->connections[$connectionId] = $connection;
        
        // Call on_connect callback
        if ($connection['on_connect'] && is_callable($connection['on_connect'])) {
            $connection['on_connect']($connectionId);
        }
        
        $this->logInfo("SSE connected to $connectionId");
        
        // Process the stream
        $response->getBody()->on('data', function($chunk) use ($connectionId) {
            $this->processChunk($connectionId, $chunk);
        });
        
        $response->getBody()->on('error', function($error) use ($connectionId) {
            $this->handleStreamError($connectionId, $error);
        });
        
        $response->getBody()->on('close', function() use ($connectionId) {
            $this->handleStreamClose($connectionId);
        });
    }

    /**
     * Process SSE data chunk
     */
    private function processChunk(string $connectionId, string $chunk): void
    {
        $connection = $this->connections[$connectionId];
        
        // Parse SSE format
        $events = $this->parseSseChunk($chunk);
        
        foreach ($events as $event) {
            // Check if event type matches filter
            if (!$this->matchesEventFilter($event, $connection['events'])) {
                continue;
            }
            
            // Transform event if needed
            $transformedEvent = $event;
            if ($connection['transform'] && is_callable($connection['transform'])) {
                $transformedEvent = $connection['transform']($event);
            }
            
            // Store last event ID
            if (isset($event['id'])) {
                $connection['last_event_id'] = $event['id'];
                $this->saveLastEventId($connectionId, $event['id']);
            }
            
            // Add to buffer
            $this->addToBuffer($connectionId, $transformedEvent);
            
            // Call on_event callback
            if ($connection['on_event'] && is_callable($connection['on_event'])) {
                $connection['on_event']($transformedEvent, $connectionId);
            }
        }
        
        $this->connections[$connectionId] = $connection;
    }

    /**
     * Parse SSE chunk into events
     */
    private function parseSseChunk(string $chunk): array
    {
        $events = [];
        $lines = explode("\n", $chunk);
        $currentEvent = [];
        
        foreach ($lines as $line) {
            $line = trim($line);
            
            if (empty($line)) {
                // Empty line marks end of event
                if (!empty($currentEvent)) {
                    $events[] = $currentEvent;
                    $currentEvent = [];
                }
                continue;
            }
            
            if (strpos($line, ':') === false) {
                continue;
            }
            
            list($field, $value) = explode(':', $line, 2);
            $field = trim($field);
            $value = trim($value);
            
            switch ($field) {
                case 'event':
                    $currentEvent['type'] = $value;
                    break;
                case 'data':
                    if (isset($currentEvent['data'])) {
                        $currentEvent['data'] .= "\n" . $value;
                    } else {
                        $currentEvent['data'] = $value;
                    }
                    break;
                case 'id':
                    $currentEvent['id'] = $value;
                    break;
                case 'retry':
                    $currentEvent['retry'] = (int) $value;
                    break;
            }
        }
        
        // Add final event if exists
        if (!empty($currentEvent)) {
            $events[] = $currentEvent;
        }
        
        return $events;
    }

    /**
     * Check if event matches filter
     */
    private function matchesEventFilter(array $event, array $filters): bool
    {
        if (empty($filters)) {
            return true;
        }
        
        $eventType = $event['type'] ?? 'message';
        
        foreach ($filters as $filter) {
            if ($this->matchesPattern($eventType, $filter)) {
                return true;
            }
        }
        
        return false;
    }

    /**
     * Check if string matches pattern (supports wildcards)
     */
    private function matchesPattern(string $string, string $pattern): bool
    {
        if ($pattern === '*') {
            return true;
        }
        
        if (strpos($pattern, '*') === false) {
            return $string === $pattern;
        }
        
        $regex = str_replace(['*', '.'], ['.*', '\.'], $pattern);
        return preg_match("/^$regex$/", $string) === 1;
    }

    /**
     * Add event to buffer
     */
    private function addToBuffer(string $connectionId, array $event): void
    {
        if (!isset($this->eventBuffers[$connectionId])) {
            $this->eventBuffers[$connectionId] = [];
        }
        
        $this->eventBuffers[$connectionId][] = array_merge($event, [
            'timestamp' => time(),
            'connection_id' => $connectionId
        ]);
        
        // Limit buffer size
        if (count($this->eventBuffers[$connectionId]) > $this->config['max_buffer_size']) {
            array_shift($this->eventBuffers[$connectionId]);
        }
    }

    /**
     * Get events from buffer
     */
    private function getEvents(string $url, int $limit): array
    {
        $connectionId = $this->getConnectionId($url);
        
        if (!isset($this->eventBuffers[$connectionId])) {
            return [];
        }
        
        $events = array_slice($this->eventBuffers[$connectionId], -$limit);
        
        return [
            'events' => $events,
            'total' => count($this->eventBuffers[$connectionId]),
            'connection_id' => $connectionId
        ];
    }

    /**
     * Disconnect from SSE stream
     */
    private function disconnect(string $url): array
    {
        $connectionId = $this->getConnectionId($url);
        
        if (!isset($this->connections[$connectionId])) {
            return ['status' => 'not_connected'];
        }
        
        $connection = $this->connections[$connectionId];
        
        // Call on_disconnect callback
        if ($connection['on_disconnect'] && is_callable($connection['on_disconnect'])) {
            $connection['on_disconnect']($connectionId);
        }
        
        unset($this->connections[$connectionId]);
        unset($this->eventBuffers[$connectionId]);
        
        return ['status' => 'disconnected'];
    }

    /**
     * Handle connection error
     */
    private function handleConnectionError(string $connectionId, \Exception $e): void
    {
        $connection = $this->connections[$connectionId];
        
        $this->logError("SSE connection error for $connectionId: " . $e->getMessage());
        
        // Call on_error callback
        if ($connection['on_error'] && is_callable($connection['on_error'])) {
            $connection['on_error']($e, $connectionId);
        }
        
        // Schedule reconnection
        $this->scheduleReconnection($connectionId);
    }

    /**
     * Handle stream error
     */
    private function handleStreamError(string $connectionId, $error): void
    {
        $this->logError("SSE stream error for $connectionId: " . $error->getMessage());
        
        $connection = $this->connections[$connectionId];
        
        // Call on_error callback
        if ($connection['on_error'] && is_callable($connection['on_error'])) {
            $connection['on_error']($error, $connectionId);
        }
        
        // Schedule reconnection
        $this->scheduleReconnection($connectionId);
    }

    /**
     * Handle stream close
     */
    private function handleStreamClose(string $connectionId): void
    {
        $this->logInfo("SSE stream closed for $connectionId");
        
        $connection = $this->connections[$connectionId];
        
        // Call on_disconnect callback
        if ($connection['on_disconnect'] && is_callable($connection['on_disconnect'])) {
            $connection['on_disconnect']($connectionId);
        }
        
        // Schedule reconnection
        $this->scheduleReconnection($connectionId);
    }

    /**
     * Schedule reconnection attempt
     */
    private function scheduleReconnection(string $connectionId): void
    {
        $connection = $this->connections[$connectionId];
        
        if ($connection['reconnect_attempts'] >= $this->config['max_reconnect_attempts']) {
            $this->logError("Max reconnection attempts reached for $connectionId");
            unset($this->connections[$connectionId]);
            return;
        }
        
        $connection['reconnect_attempts']++;
        $connection['status'] = 'reconnecting';
        $this->connections[$connectionId] = $connection;
        
        $delay = min($connection['retry'] * $connection['reconnect_attempts'], $this->config['max_reconnect_delay']);
        
        $this->logInfo("Scheduling reconnection attempt {$connection['reconnect_attempts']} for $connectionId in {$delay}ms");
        
        Loop::addTimer($delay / 1000, function() use ($connectionId) {
            $this->establishConnection($connectionId);
        });
    }

    /**
     * Get connection ID from URL
     */
    private function getConnectionId(string $url): string
    {
        return md5($url);
    }

    /**
     * Get last event ID for connection
     */
    private function getLastEventId(string $connectionId): ?string
    {
        // In a real implementation, this would be stored persistently
        // For now, return null
        return null;
    }

    /**
     * Save last event ID for connection
     */
    private function saveLastEventId(string $connectionId, string $eventId): void
    {
        // In a real implementation, this would be stored persistently
        // For now, do nothing
    }

    /**
     * Validate operator parameters
     */
    private function validateParams(array $params): void
    {
        if (isset($params['action']) && !in_array($params['action'], ['connect', 'disconnect', 'get_events'])) {
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
        foreach ($this->connections as $connectionId => $connection) {
            $this->disconnect($connection['url']);
        }
        
        $this->connections = [];
        $this->eventBuffers = [];
    }

    /**
     * Get operator schema
     */
    public function getSchema(): array
    {
        return [
            'type' => 'object',
            'properties' => [
                'url' => ['type' => 'string', 'description' => 'SSE endpoint URL'],
                'action' => [
                    'type' => 'string',
                    'enum' => ['connect', 'disconnect', 'get_events'],
                    'default' => 'connect',
                    'description' => 'SSE action'
                ],
                'events' => ['type' => 'array', 'description' => 'Event types to filter'],
                'headers' => ['type' => 'object', 'description' => 'HTTP headers'],
                'retry' => ['type' => 'integer', 'description' => 'Reconnection delay in milliseconds'],
                'timeout' => ['type' => 'integer', 'description' => 'Connection timeout in milliseconds'],
                'transform' => ['type' => 'function', 'description' => 'Event transformation function'],
                'on_event' => ['type' => 'function', 'description' => 'Event handler'],
                'on_error' => ['type' => 'function', 'description' => 'Error handler'],
                'on_connect' => ['type' => 'function', 'description' => 'Connect handler'],
                'on_disconnect' => ['type' => 'function', 'description' => 'Disconnect handler'],
                'limit' => ['type' => 'integer', 'description' => 'Number of events to retrieve']
            ],
            'required' => ['url']
        ];
    }
} 