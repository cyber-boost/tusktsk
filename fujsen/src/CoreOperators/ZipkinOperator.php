<?php

namespace Fujsen\CoreOperators;

use Fujsen\BaseOperator;
use Fujsen\Exceptions\OperatorException;

/**
 * Zipkin Operator for TuskLang
 * 
 * Provides Zipkin distributed tracing functionality with support for:
 * - Span creation and management
 * - Trace querying and retrieval
 * - Service dependency analysis
 * - Sampling configuration
 * - Annotations and binary annotations
 * - Trace context propagation
 * 
 * Usage:
 * ```php
 * // Create span
 * $span = @zipkin({
 *   action: "start_span",
 *   name: "database_query",
 *   service: "user-service",
 *   annotations: { cs: "client_send", sr: "server_receive" }
 * })
 * 
 * // Query traces
 * $traces = @zipkin({
 *   action: "query",
 *   service: "api-gateway",
 *   span_name: "GET /users",
 *   start_time: "2024-01-17T10:00:00Z",
 *   end_time: "2024-01-17T11:00:00Z"
 * })
 * ```
 */
class ZipkinOperator extends BaseOperator
{
    private array $clients = [];
    private array $spans = [];
    private array $traces = [];
    private array $config;
    
    public function __construct(array $config = [])
    {
        parent::__construct('zipkin');
        $this->config = array_merge([
            'default_url' => 'http://localhost:9411',
            'timeout' => 30,
            'retry_attempts' => 3,
            'retry_delay' => 1000,
            'enable_tls' => false,
            'tls_verify' => true,
            'tls_cert' => '',
            'tls_key' => '',
            'tls_ca' => '',
            'username' => '',
            'password' => ''
        ], $config);
    }

    /**
     * Execute Zipkin operation
     */
    public function execute(array $params, array $context = []): mixed
    {
        $this->validateParams($params);
        
        $action = $params['action'] ?? '';
        $name = $params['name'] ?? '';
        $service = $params['service'] ?? '';
        $annotations = $params['annotations'] ?? [];
        $binaryAnnotations = $params['binary_annotations'] ?? [];
        $url = $params['url'] ?? $this->config['default_url'];
        $startTime = $params['start_time'] ?? '';
        $endTime = $params['end_time'] ?? '';
        $traceId = $params['trace_id'] ?? '';
        $spanId = $params['span_id'] ?? '';
        $parentId = $params['parent_id'] ?? '';
        $spanName = $params['span_name'] ?? '';
        
        try {
            $client = $this->getClient($url);
            
            switch ($action) {
                case 'start_span':
                    return $this->startSpan($client, $name, $service, $annotations, $binaryAnnotations, $parentId);
                case 'finish_span':
                    return $this->finishSpan($client, $spanId, $annotations, $binaryAnnotations);
                case 'add_annotation':
                    return $this->addAnnotation($client, $spanId, $annotations);
                case 'add_binary_annotation':
                    return $this->addBinaryAnnotation($client, $spanId, $binaryAnnotations);
                case 'query':
                    return $this->queryTraces($client, $service, $spanName, $startTime, $endTime, $params);
                case 'get_trace':
                    return $this->getTrace($client, $traceId);
                case 'get_services':
                    return $this->getServices($client);
                case 'get_spans':
                    return $this->getSpans($client, $service);
                case 'get_dependencies':
                    return $this->getDependencies($client, $startTime, $endTime);
                case 'get_sampling':
                    return $this->getSampling($client);
                default:
                    throw new OperatorException("Unsupported action: $action");
            }
        } catch (\Exception $e) {
            throw new OperatorException("Zipkin operation failed: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Start span
     */
    private function startSpan($client, string $name, string $service, array $annotations, array $binaryAnnotations, string $parentId): array
    {
        $spanId = $this->generateSpanId();
        $traceId = $parentId ? $this->getTraceIdFromParent($parentId) : $this->generateTraceId();
        
        $span = [
            'traceId' => $traceId,
            'id' => $spanId,
            'name' => $name,
            'parentId' => $parentId ?: null,
            'timestamp' => microtime(true) * 1000000, // microseconds
            'duration' => null,
            'annotations' => $this->formatAnnotations($annotations),
            'binaryAnnotations' => $this->formatBinaryAnnotations($binaryAnnotations)
        ];
        
        $this->spans[$spanId] = $span;
        
        return [
            'status' => 'started',
            'span_id' => $spanId,
            'trace_id' => $traceId,
            'name' => $name,
            'service' => $service
        ];
    }

    /**
     * Finish span
     */
    private function finishSpan($client, string $spanId, array $annotations, array $binaryAnnotations): array
    {
        if (!isset($this->spans[$spanId])) {
            throw new OperatorException("Span not found: $spanId");
        }
        
        $span = $this->spans[$spanId];
        $endTime = microtime(true) * 1000000; // microseconds
        $span['duration'] = $endTime - $span['timestamp'];
        
        if (!empty($annotations)) {
            $span['annotations'] = array_merge($span['annotations'], $this->formatAnnotations($annotations));
        }
        
        if (!empty($binaryAnnotations)) {
            $span['binaryAnnotations'] = array_merge($span['binaryAnnotations'], $this->formatBinaryAnnotations($binaryAnnotations));
        }
        
        // Send span to Zipkin
        $this->sendSpan($client, $span);
        
        unset($this->spans[$spanId]);
        
        return [
            'status' => 'finished',
            'span_id' => $spanId,
            'trace_id' => $span['traceId'],
            'duration' => $span['duration']
        ];
    }

    /**
     * Add annotation to span
     */
    private function addAnnotation($client, string $spanId, array $annotations): array
    {
        if (!isset($this->spans[$spanId])) {
            throw new OperatorException("Span not found: $spanId");
        }
        
        $this->spans[$spanId]['annotations'] = array_merge(
            $this->spans[$spanId]['annotations'],
            $this->formatAnnotations($annotations)
        );
        
        return [
            'status' => 'annotation_added',
            'span_id' => $spanId,
            'annotations' => $annotations
        ];
    }

    /**
     * Add binary annotation to span
     */
    private function addBinaryAnnotation($client, string $spanId, array $binaryAnnotations): array
    {
        if (!isset($this->spans[$spanId])) {
            throw new OperatorException("Span not found: $spanId");
        }
        
        $this->spans[$spanId]['binaryAnnotations'] = array_merge(
            $this->spans[$spanId]['binaryAnnotations'],
            $this->formatBinaryAnnotations($binaryAnnotations)
        );
        
        return [
            'status' => 'binary_annotation_added',
            'span_id' => $spanId,
            'binary_annotations' => $binaryAnnotations
        ];
    }

    /**
     * Query traces
     */
    private function queryTraces($client, string $service, string $spanName, string $startTime, string $endTime, array $params): array
    {
        $queryParams = [
            'serviceName' => $service,
            'startTs' => strtotime($startTime) * 1000000, // microseconds
            'endTs' => strtotime($endTime) * 1000000, // microseconds
            'limit' => $params['limit'] ?? 10
        ];
        
        if ($spanName) {
            $queryParams['spanName'] = $spanName;
        }
        
        if (isset($params['annotation_query'])) {
            $queryParams['annotationQuery'] = $params['annotation_query'];
        }
        
        if (isset($params['min_duration'])) {
            $queryParams['minDuration'] = $params['min_duration'];
        }
        
        if (isset($params['max_duration'])) {
            $queryParams['maxDuration'] = $params['max_duration'];
        }
        
        $response = $client->get('/api/v1/traces', ['query' => $queryParams]);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to query traces: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'queried',
            'service' => $service,
            'span_name' => $spanName,
            'traces' => $data,
            'total' => count($data)
        ];
    }

    /**
     * Get trace by ID
     */
    private function getTrace($client, string $traceId): array
    {
        $response = $client->get("/api/v1/trace/$traceId");
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to get trace: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'retrieved',
            'trace_id' => $traceId,
            'trace' => $data
        ];
    }

    /**
     * Get services
     */
    private function getServices($client): array
    {
        $response = $client->get('/api/v1/services');
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to get services: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'retrieved',
            'services' => $data
        ];
    }

    /**
     * Get spans for service
     */
    private function getSpans($client, string $service): array
    {
        $response = $client->get("/api/v1/spans?serviceName=$service");
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to get spans: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'retrieved',
            'service' => $service,
            'spans' => $data
        ];
    }

    /**
     * Get service dependencies
     */
    private function getDependencies($client, string $startTime, string $endTime): array
    {
        $params = [
            'startTs' => strtotime($startTime) * 1000000, // microseconds
            'endTs' => strtotime($endTime) * 1000000 // microseconds
        ];
        
        $response = $client->get('/api/v1/dependencies', ['query' => $params]);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to get dependencies: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'retrieved',
            'dependencies' => $data
        ];
    }

    /**
     * Get sampling configuration
     */
    private function getSampling($client): array
    {
        $response = $client->get('/api/v1/sampling');
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to get sampling: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'retrieved',
            'sampling' => $data
        ];
    }

    /**
     * Send span to Zipkin
     */
    private function sendSpan($client, array $span): void
    {
        $response = $client->post('/api/v1/spans', ['json' => [$span]]);
        
        if ($response->getStatusCode() !== 202) {
            throw new OperatorException("Failed to send span: " . $response->getBody());
        }
    }

    /**
     * Format annotations for Zipkin
     */
    private function formatAnnotations(array $annotations): array
    {
        $formatted = [];
        
        foreach ($annotations as $key => $value) {
            $formatted[] = [
                'timestamp' => microtime(true) * 1000000, // microseconds
                'value' => $value,
                'endpoint' => [
                    'serviceName' => 'unknown',
                    'ipv4' => '127.0.0.1',
                    'port' => 8080
                ]
            ];
        }
        
        return $formatted;
    }

    /**
     * Format binary annotations for Zipkin
     */
    private function formatBinaryAnnotations(array $binaryAnnotations): array
    {
        $formatted = [];
        
        foreach ($binaryAnnotations as $key => $value) {
            $formatted[] = [
                'key' => $key,
                'value' => is_string($value) ? $value : json_encode($value),
                'endpoint' => [
                    'serviceName' => 'unknown',
                    'ipv4' => '127.0.0.1',
                    'port' => 8080
                ]
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
        return sprintf('%016x', mt_rand(0, 0xffffffffffffffff));
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
     * Get or create Zipkin client
     */
    private function getClient(string $url): object
    {
        if (!isset($this->clients[$url])) {
            $this->clients[$url] = $this->createClient($url);
        }
        
        return $this->clients[$url];
    }

    /**
     * Create HTTP client for Zipkin
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
        
        $validActions = ['start_span', 'finish_span', 'add_annotation', 'add_binary_annotation', 'query', 'get_trace', 'get_services', 'get_spans', 'get_dependencies', 'get_sampling'];
        if (!in_array($params['action'], $validActions)) {
            throw new OperatorException("Invalid action: " . $params['action']);
        }
        
        if (in_array($params['action'], ['start_span']) && (!isset($params['name']) || !isset($params['service']))) {
            throw new OperatorException("Name and service are required for start_span action");
        }
        
        if (in_array($params['action'], ['finish_span', 'add_annotation', 'add_binary_annotation']) && !isset($params['span_id'])) {
            throw new OperatorException("Span ID is required for " . $params['action'] . " action");
        }
        
        if (in_array($params['action'], ['query']) && (!isset($params['service']) || !isset($params['start_time']) || !isset($params['end_time']))) {
            throw new OperatorException("Service, start_time, and end_time are required for query action");
        }
        
        if (in_array($params['action'], ['get_trace']) && !isset($params['trace_id'])) {
            throw new OperatorException("Trace ID is required for get_trace action");
        }
        
        if (in_array($params['action'], ['get_spans']) && !isset($params['service'])) {
            throw new OperatorException("Service is required for get_spans action");
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
                    'enum' => ['start_span', 'finish_span', 'add_annotation', 'add_binary_annotation', 'query', 'get_trace', 'get_services', 'get_spans', 'get_dependencies', 'get_sampling'],
                    'description' => 'Zipkin action'
                ],
                'name' => ['type' => 'string', 'description' => 'Span name'],
                'service' => ['type' => 'string', 'description' => 'Service name'],
                'annotations' => ['type' => 'object', 'description' => 'Span annotations'],
                'binary_annotations' => ['type' => 'object', 'description' => 'Binary annotations'],
                'url' => ['type' => 'string', 'description' => 'Zipkin URL'],
                'start_time' => ['type' => 'string', 'description' => 'Start time'],
                'end_time' => ['type' => 'string', 'description' => 'End time'],
                'trace_id' => ['type' => 'string', 'description' => 'Trace ID'],
                'span_id' => ['type' => 'string', 'description' => 'Span ID'],
                'parent_id' => ['type' => 'string', 'description' => 'Parent span ID'],
                'span_name' => ['type' => 'string', 'description' => 'Span name for querying']
            ],
            'required' => ['action']
        ];
    }
} 