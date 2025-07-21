<?php

namespace TuskLang\CoreOperators;

use Fujsen\BaseOperator;
use Fujsen\Exceptions\OperatorException;

/**
 * Jaeger Operator for TuskLang
 * 
 * Provides Jaeger distributed tracing functionality with support for:
 * - Span creation and management
 * - Trace querying and retrieval
 * - Service dependency analysis
 * - Sampling configuration
 * - Baggage and tags
 * - Trace context propagation
 * 
 * Usage:
 * ```php
 * // Create span
 * $span = @jaeger({
 *   action: "start_span",
 *   operation: "database_query",
 *   service: "user-service",
 *   tags: { db.type: "postgres", query: "SELECT * FROM users" }
 * })
 * 
 * // Query traces
 * $traces = @jaeger({
 *   action: "query",
 *   service: "api-gateway",
 *   operation: "GET /users",
 *   start_time: "2024-01-17T10:00:00Z",
 *   end_time: "2024-01-17T11:00:00Z"
 * })
 * ```
 */
class JaegerOperator extends \TuskLang\CoreOperators\BaseOperator
{
    private array $clients = [];
    private array $spans = [];
    private array $traces = [];
    private array $config;
    
    public function __construct(array $config = [])
    {
        parent::__construct('jaeger');
        $this->config = array_merge([
            'default_url' => 'http://localhost:16686',
            'collector_url' => 'http://localhost:14268',
            'timeout' => 30,
            'retry_attempts' => 3,
            'retry_delay' => 1000,
            'enable_tls' => false,
            'tls_verify' => true,
            'tls_cert' => '',
            'tls_key' => '',
            'tls_ca' => '',
            'username' => '',
            'password' => '',
            'api_key' => ''
        ], $config);
    }

    /**
     * Execute Jaeger operation
     */
    public function execute(array $params, array $context = []): mixed
    {
        $this->validateParams($params);
        
        $action = $params['action'] ?? '';
        $operation = $params['operation'] ?? '';
        $service = $params['service'] ?? '';
        $tags = $params['tags'] ?? [];
        $url = $params['url'] ?? $this->config['default_url'];
        $collectorUrl = $params['collector_url'] ?? $this->config['collector_url'];
        $startTime = $params['start_time'] ?? '';
        $endTime = $params['end_time'] ?? '';
        $traceId = $params['trace_id'] ?? '';
        $spanId = $params['span_id'] ?? '';
        $parentId = $params['parent_id'] ?? '';
        $baggage = $params['baggage'] ?? [];
        
        try {
            $client = $this->getClient($url);
            $collectorClient = $this->getCollectorClient($collectorUrl);
            
            switch ($action) {
                case 'start_span':
                    return $this->startSpan($collectorClient, $operation, $service, $tags, $parentId, $baggage);
                case 'finish_span':
                    return $this->finishSpan($collectorClient, $spanId, $tags);
                case 'add_tag':
                    return $this->addTag($collectorClient, $spanId, $tags);
                case 'add_log':
                    return $this->addLog($collectorClient, $spanId, $params);
                case 'query':
                    return $this->queryTraces($client, $service, $operation, $startTime, $endTime, $params);
                case 'get_trace':
                    return $this->getTrace($client, $traceId);
                case 'get_services':
                    return $this->getServices($client);
                case 'get_operations':
                    return $this->getOperations($client, $service);
                case 'get_dependencies':
                    return $this->getDependencies($client, $startTime, $endTime);
                case 'get_sampling':
                    return $this->getSampling($client, $service);
                default:
                    throw new OperatorException("Unsupported action: $action");
            }
        } catch (\Exception $e) {
            throw new OperatorException("Jaeger operation failed: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Start span
     */
    private function startSpan($client, string $operation, string $service, array $tags, string $parentId, array $baggage): array
    {
        $spanId = $this->generateSpanId();
        $traceId = $parentId ? $this->getTraceIdFromParent($parentId) : $this->generateTraceId();
        
        $span = [
            'traceId' => $traceId,
            'spanId' => $spanId,
            'operationName' => $operation,
            'references' => $parentId ? [
                [
                    'refType' => 'CHILD_OF',
                    'traceId' => $traceId,
                    'spanId' => $parentId
                ]
            ] : [],
            'startTime' => microtime(true) * 1000000, // microseconds
            'tags' => $this->formatTags($tags),
            'baggage' => $baggage,
            'logs' => [],
            'process' => [
                'serviceName' => $service,
                'tags' => []
            ]
        ];
        
        $this->spans[$spanId] = $span;
        
        return [
            'status' => 'started',
            'span_id' => $spanId,
            'trace_id' => $traceId,
            'operation' => $operation,
            'service' => $service
        ];
    }

    /**
     * Finish span
     */
    private function finishSpan($client, string $spanId, array $tags): array
    {
        if (!isset($this->spans[$spanId])) {
            throw new OperatorException("Span not found: $spanId");
        }
        
        $span = $this->spans[$spanId];
        $span['finishTime'] = microtime(true) * 1000000; // microseconds
        
        if (!empty($tags)) {
            $span['tags'] = array_merge($span['tags'], $this->formatTags($tags));
        }
        
        // Send span to collector
        $this->sendSpan($client, $span);
        
        unset($this->spans[$spanId]);
        
        return [
            'status' => 'finished',
            'span_id' => $spanId,
            'trace_id' => $span['traceId'],
            'duration' => $span['finishTime'] - $span['startTime']
        ];
    }

    /**
     * Add tag to span
     */
    private function addTag($client, string $spanId, array $tags): array
    {
        if (!isset($this->spans[$spanId])) {
            throw new OperatorException("Span not found: $spanId");
        }
        
        $this->spans[$spanId]['tags'] = array_merge(
            $this->spans[$spanId]['tags'],
            $this->formatTags($tags)
        );
        
        return [
            'status' => 'tag_added',
            'span_id' => $spanId,
            'tags' => $tags
        ];
    }

    /**
     * Add log to span
     */
    private function addLog($client, string $spanId, array $params): array
    {
        if (!isset($this->spans[$spanId])) {
            throw new OperatorException("Span not found: $spanId");
        }
        
        $timestamp = microtime(true) * 1000000;
        $fields = $params['fields'] ?? [];
        
        $log = [
            'timestamp' => $timestamp,
            'fields' => $this->formatTags($fields)
        ];
        
        $this->spans[$spanId]['logs'][] = $log;
        
        return [
            'status' => 'log_added',
            'span_id' => $spanId,
            'timestamp' => $timestamp,
            'fields' => $fields
        ];
    }

    /**
     * Query traces
     */
    private function queryTraces($client, string $service, string $operation, string $startTime, string $endTime, array $params): array
    {
        $queryParams = [
            'service' => $service,
            'start' => $startTime,
            'end' => $endTime,
            'limit' => $params['limit'] ?? 20
        ];
        
        if ($operation) {
            $queryParams['operation'] = $operation;
        }
        
        if (isset($params['tags'])) {
            $queryParams['tags'] = json_encode($params['tags']);
        }
        
        $response = $client->get('/api/traces', ['query' => $queryParams]);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to query traces: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'queried',
            'service' => $service,
            'operation' => $operation,
            'traces' => $data['data'] ?? [],
            'total' => count($data['data'] ?? [])
        ];
    }

    /**
     * Get trace by ID
     */
    private function getTrace($client, string $traceId): array
    {
        $response = $client->get("/api/traces/$traceId");
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to get trace: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'retrieved',
            'trace_id' => $traceId,
            'trace' => $data['data'][0] ?? null
        ];
    }

    /**
     * Get services
     */
    private function getServices($client): array
    {
        $response = $client->get('/api/services');
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to get services: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'retrieved',
            'services' => $data['data'] ?? []
        ];
    }

    /**
     * Get operations for service
     */
    private function getOperations($client, string $service): array
    {
        $response = $client->get("/api/services/$service/operations");
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to get operations: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'retrieved',
            'service' => $service,
            'operations' => $data['data'] ?? []
        ];
    }

    /**
     * Get service dependencies
     */
    private function getDependencies($client, string $startTime, string $endTime): array
    {
        $params = [
            'start' => $startTime,
            'end' => $endTime
        ];
        
        $response = $client->get('/api/dependencies', ['query' => $params]);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to get dependencies: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'retrieved',
            'dependencies' => $data['data'] ?? []
        ];
    }

    /**
     * Get sampling configuration
     */
    private function getSampling($client, string $service): array
    {
        $response = $client->get("/api/sampling?service=$service");
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to get sampling: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'retrieved',
            'service' => $service,
            'sampling' => $data['data'] ?? []
        ];
    }

    /**
     * Send span to collector
     */
    private function sendSpan($client, array $span): void
    {
        $batch = [
            'process' => $span['process'],
            'spans' => [$span]
        ];
        
        $response = $client->post('/api/traces', ['json' => $batch]);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to send span: " . $response->getBody());
        }
    }

    /**
     * Format tags for Jaeger
     */
    private function formatTags(array $tags): array
    {
        $formatted = [];
        
        foreach ($tags as $key => $value) {
            $formatted[] = [
                'key' => $key,
                'type' => is_numeric($value) ? 'float64' : 'string',
                'value' => $value
            ];
        }
        
        return $formatted;
    }

    /**
     * Generate span ID
     */
    private function generateSpanId(): string
    {
        return sprintf('%016x', mt_rand(0, 0xffffffffffffffff));
    }

    /**
     * Generate trace ID
     */
    private function generateTraceId(): string
    {
        return sprintf('%032x', mt_rand(0, 0xffffffffffffffffffffffffffffffff));
    }

    /**
     * Get trace ID from parent span
     */
    private function getTraceIdFromParent(string $parentId): string
    {
        if (isset($this->spans[$parentId])) {
            return $this->spans[$parentId]['traceId'];
        }
        
        return $this->generateTraceId();
    }

    /**
     * Get or create Jaeger client
     */
    private function getClient(string $url): object
    {
        if (!isset($this->clients[$url])) {
            $this->clients[$url] = $this->createClient($url);
        }
        
        return $this->clients[$url];
    }

    /**
     * Get or create collector client
     */
    private function getCollectorClient(string $url): object
    {
        $collectorKey = 'collector_' . $url;
        
        if (!isset($this->clients[$collectorKey])) {
            $this->clients[$collectorKey] = $this->createClient($url);
        }
        
        return $this->clients[$collectorKey];
    }

    /**
     * Create HTTP client for Jaeger
     */
    private function createClient(string $url): object
    {
        $config = [
            'base_uri' => $url,
            'timeout' => $this->config['timeout'],
            'headers' => [
                'Content-Type' => 'application/json'
            ]
        ];
        
        if ($this->config['username'] && $this->config['password']) {
            $config['auth'] = [$this->config['username'], $this->config['password']];
        }
        
        if ($this->config['api_key']) {
            $config['headers']['Authorization'] = 'Bearer ' . $this->config['api_key'];
        }
        
        if ($this->config['enable_tls']) {
            $config['verify'] = $this->config['tls_verify'];
            if ($this->config['tls_cert'] && $this->config['tls_key']) {
                $config['cert'] = [$this->config['tls_cert'], $this->config['tls_key']];
            }
            if ($this->config['tls_ca']) {
                $config['verify'] = $this->config['tls_ca'];
            }
        }
        
        return new \GuzzleHttp\Client($config);
    }

    /**
     * Validate operator parameters
     */
    private function validateParams(array $params): void
    {
        if (!isset($params['action'])) {
            throw new OperatorException("Action is required");
        }
        
        $validActions = ['start_span', 'finish_span', 'add_tag', 'add_log', 'query', 'get_trace', 'get_services', 'get_operations', 'get_dependencies', 'get_sampling'];
        if (!in_array($params['action'], $validActions)) {
            throw new OperatorException("Invalid action: " . $params['action']);
        }
        
        if (in_array($params['action'], ['start_span']) && (!isset($params['operation']) || !isset($params['service']))) {
            throw new OperatorException("Operation and service are required for start_span action");
        }
        
        if (in_array($params['action'], ['finish_span', 'add_tag', 'add_log']) && !isset($params['span_id'])) {
            throw new OperatorException("Span ID is required for " . $params['action'] . " action");
        }
        
        if (in_array($params['action'], ['query']) && (!isset($params['service']) || !isset($params['start_time']) || !isset($params['end_time']))) {
            throw new OperatorException("Service, start_time, and end_time are required for query action");
        }
        
        if (in_array($params['action'], ['get_trace']) && !isset($params['trace_id'])) {
            throw new OperatorException("Trace ID is required for get_trace action");
        }
        
        if (in_array($params['action'], ['get_operations']) && !isset($params['service'])) {
            throw new OperatorException("Service is required for get_operations action");
        }
        
        if (in_array($params['action'], ['get_dependencies']) && (!isset($params['start_time']) || !isset($params['end_time']))) {
            throw new OperatorException("Start_time and end_time are required for get_dependencies action");
        }
    }

    /**
     * Cleanup resources
     */
    public function cleanup(): void
    {
        $this->clients = [];
        $this->spans = [];
        $this->traces = [];
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
                    'enum' => ['start_span', 'finish_span', 'add_tag', 'add_log', 'query', 'get_trace', 'get_services', 'get_operations', 'get_dependencies', 'get_sampling'],
                    'description' => 'Jaeger action'
                ],
                'operation' => ['type' => 'string', 'description' => 'Operation name'],
                'service' => ['type' => 'string', 'description' => 'Service name'],
                'tags' => ['type' => 'object', 'description' => 'Span tags'],
                'url' => ['type' => 'string', 'description' => 'Jaeger URL'],
                'collector_url' => ['type' => 'string', 'description' => 'Collector URL'],
                'start_time' => ['type' => 'string', 'description' => 'Start time'],
                'end_time' => ['type' => 'string', 'description' => 'End time'],
                'trace_id' => ['type' => 'string', 'description' => 'Trace ID'],
                'span_id' => ['type' => 'string', 'description' => 'Span ID'],
                'parent_id' => ['type' => 'string', 'description' => 'Parent span ID'],
                'baggage' => ['type' => 'object', 'description' => 'Baggage items']
            ],
            'required' => ['action']
        ];
    }
} 