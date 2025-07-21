<?php

namespace TuskLang\CoreOperators;

use Fujsen\BaseOperator;
use Fujsen\Exceptions\OperatorException;

/**
 * Prometheus Operator for TuskLang
 * 
 * Provides Prometheus functionality with support for:
 * - Metrics collection and exposition
 * - PromQL querying
 * - Alerting rules and notifications
 * - Service discovery
 * - Recording rules
 * - Metric validation and formatting
 * 
 * Usage:
 * ```php
 * // Query metrics
 * $cpu_usage = @prometheus({
 *   action: "query",
 *   query: "rate(node_cpu_seconds_total{mode='idle'}[5m])",
 *   time: "2024-01-17T10:00:00Z"
 * })
 * 
 * // Record metric
 * $result = @prometheus({
 *   action: "record",
 *   metric: "http_requests_total",
 *   value: 42,
 *   labels: { method: "GET", endpoint: "/api/users" }
 * })
 * ```
 */
class PrometheusOperator extends \TuskLang\CoreOperators\BaseOperator
{
    private array $clients = [];
    private array $metrics = [];
    private array $alerts = [];
    private array $config;
    
    public function __construct(array $config = [])
    {
        parent::__construct('prometheus');
        $this->config = array_merge([
            'default_url' => 'http://localhost:9090',
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
     * Execute Prometheus operation
     */
    public function execute(array $params, array $context = []): mixed
    {
        $this->validateParams($params);
        
        $action = $params['action'] ?? '';
        $query = $params['query'] ?? '';
        $metric = $params['metric'] ?? '';
        $value = $params['value'] ?? 0;
        $labels = $params['labels'] ?? [];
        $url = $params['url'] ?? $this->config['default_url'];
        $time = $params['time'] ?? '';
        $start = $params['start'] ?? '';
        $end = $params['end'] ?? '';
        $step = $params['step'] ?? '';
        $rule = $params['rule'] ?? '';
        $alert = $params['alert'] ?? '';
        
        try {
            $client = $this->getClient($url);
            
            switch ($action) {
                case 'query':
                    return $this->queryMetrics($client, $query, $time);
                case 'query_range':
                    return $this->queryRange($client, $query, $start, $end, $step);
                case 'record':
                    return $this->recordMetric($client, $metric, $value, $labels);
                case 'series':
                    return $this->getSeries($client, $params);
                case 'labels':
                    return $this->getLabels($client, $params);
                case 'targets':
                    return $this->getTargets($client);
                case 'rules':
                    return $this->getRules($client);
                case 'alerts':
                    return $this->getAlerts($client);
                case 'status':
                    return $this->getStatus($client);
                case 'config':
                    return $this->getConfig($client);
                default:
                    throw new OperatorException("Unsupported action: $action");
            }
        } catch (\Exception $e) {
            throw new OperatorException("Prometheus operation failed: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Query metrics
     */
    private function queryMetrics($client, string $query, string $time): array
    {
        $params = ['query' => $query];
        
        if ($time) {
            $params['time'] = $time;
        }
        
        $response = $client->get('/api/v1/query', ['query' => $params]);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to query metrics: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        if ($data['status'] !== 'success') {
            throw new OperatorException("Query failed: " . ($data['error'] ?? 'Unknown error'));
        }
        
        return [
            'status' => 'queried',
            'query' => $query,
            'result_type' => $data['data']['resultType'],
            'result' => $this->formatQueryResult($data['data']['result'])
        ];
    }

    /**
     * Query range
     */
    private function queryRange($client, string $query, string $start, string $end, string $step): array
    {
        $params = [
            'query' => $query,
            'start' => $start,
            'end' => $end,
            'step' => $step
        ];
        
        $response = $client->get('/api/v1/query_range', ['query' => $params]);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to query range: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        if ($data['status'] !== 'success') {
            throw new OperatorException("Query range failed: " . ($data['error'] ?? 'Unknown error'));
        }
        
        return [
            'status' => 'queried',
            'query' => $query,
            'start' => $start,
            'end' => $end,
            'step' => $step,
            'result' => $this->formatQueryResult($data['data']['result'])
        ];
    }

    /**
     * Record metric
     */
    private function recordMetric($client, string $metric, $value, array $labels): array
    {
        // Format metric in Prometheus exposition format
        $metricLine = $this->formatMetric($metric, $value, $labels);
        
        // In a real implementation, this would be sent to a metrics endpoint
        // For now, just store it locally
        $metricId = uniqid('metric_');
        
        $this->metrics[$metricId] = [
            'metric' => $metric,
            'value' => $value,
            'labels' => $labels,
            'timestamp' => time(),
            'formatted' => $metricLine
        ];
        
        return [
            'status' => 'recorded',
            'metric' => $metric,
            'value' => $value,
            'labels' => $labels,
            'formatted' => $metricLine
        ];
    }

    /**
     * Get series
     */
    private function getSeries($client, array $params): array
    {
        $match = $params['match'] ?? [];
        $start = $params['start'] ?? '';
        $end = $params['end'] ?? '';
        
        $queryParams = [];
        
        if (!empty($match)) {
            foreach ($match as $matcher) {
                $queryParams['match[]'] = $matcher;
            }
        }
        
        if ($start) {
            $queryParams['start'] = $start;
        }
        
        if ($end) {
            $queryParams['end'] = $end;
        }
        
        $response = $client->get('/api/v1/series', ['query' => $queryParams]);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to get series: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'retrieved',
            'series' => $data['data'] ?? [],
            'count' => count($data['data'] ?? [])
        ];
    }

    /**
     * Get labels
     */
    private function getLabels($client, array $params): array
    {
        $match = $params['match'] ?? [];
        $start = $params['start'] ?? '';
        $end = $params['end'] ?? '';
        
        $queryParams = [];
        
        if (!empty($match)) {
            foreach ($match as $matcher) {
                $queryParams['match[]'] = $matcher;
            }
        }
        
        if ($start) {
            $queryParams['start'] = $start;
        }
        
        if ($end) {
            $queryParams['end'] = $end;
        }
        
        $response = $client->get('/api/v1/labels', ['query' => $queryParams]);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to get labels: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'retrieved',
            'labels' => $data['data'] ?? []
        ];
    }

    /**
     * Get targets
     */
    private function getTargets($client): array
    {
        $response = $client->get('/api/v1/targets');
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to get targets: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'retrieved',
            'active_targets' => $data['data']['activeTargets'] ?? [],
            'dropped_targets' => $data['data']['droppedTargets'] ?? []
        ];
    }

    /**
     * Get rules
     */
    private function getRules($client): array
    {
        $response = $client->get('/api/v1/rules');
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to get rules: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'retrieved',
            'groups' => $data['data']['groups'] ?? []
        ];
    }

    /**
     * Get alerts
     */
    private function getAlerts($client): array
    {
        $response = $client->get('/api/v1/alerts');
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to get alerts: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'retrieved',
            'alerts' => $data['data']['alerts'] ?? []
        ];
    }

    /**
     * Get status
     */
    private function getStatus($client): array
    {
        $response = $client->get('/api/v1/status/config');
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to get status: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'retrieved',
            'config' => $data['data']['yaml'] ?? '',
            'version' => $data['data']['version'] ?? ''
        ];
    }

    /**
     * Get config
     */
    private function getConfig($client): array
    {
        $response = $client->get('/api/v1/status/config');
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to get config: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'retrieved',
            'config' => $data['data']['yaml'] ?? ''
        ];
    }

    /**
     * Format query result
     */
    private function formatQueryResult($result): array
    {
        if (is_array($result)) {
            return array_map(function($item) {
                return [
                    'metric' => $item['metric'] ?? [],
                    'value' => $item['value'] ?? null,
                    'values' => $item['values'] ?? []
                ];
            }, $result);
        }
        
        return $result;
    }

    /**
     * Format metric in Prometheus exposition format
     */
    private function formatMetric(string $metric, $value, array $labels): string
    {
        $line = $metric;
        
        if (!empty($labels)) {
            $labelPairs = [];
            foreach ($labels as $key => $val) {
                $labelPairs[] = $key . '="' . addslashes($val) . '"';
            }
            $line .= '{' . implode(',', $labelPairs) . '}';
        }
        
        $line .= ' ' . $value;
        
        return $line;
    }

    /**
     * Get or create Prometheus client
     */
    private function getClient(string $url): object
    {
        if (!isset($this->clients[$url])) {
            $this->clients[$url] = $this->createClient($url);
        }
        
        return $this->clients[$url];
    }

    /**
     * Create HTTP client for Prometheus
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
        
        $validActions = ['query', 'query_range', 'record', 'series', 'labels', 'targets', 'rules', 'alerts', 'status', 'config'];
        if (!in_array($params['action'], $validActions)) {
            throw new OperatorException("Invalid action: " . $params['action']);
        }
        
        if (in_array($params['action'], ['query']) && !isset($params['query'])) {
            throw new OperatorException("Query is required for query action");
        }
        
        if (in_array($params['action'], ['query_range']) && (!isset($params['query']) || !isset($params['start']) || !isset($params['end']) || !isset($params['step']))) {
            throw new OperatorException("Query, start, end, and step are required for query_range action");
        }
        
        if (in_array($params['action'], ['record']) && (!isset($params['metric']) || !isset($params['value']))) {
            throw new OperatorException("Metric and value are required for record action");
        }
    }

    /**
     * Cleanup resources
     */
    public function cleanup(): void
    {
        $this->clients = [];
        $this->metrics = [];
        $this->alerts = [];
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
                    'enum' => ['query', 'query_range', 'record', 'series', 'labels', 'targets', 'rules', 'alerts', 'status', 'config'],
                    'description' => 'Prometheus action'
                ],
                'query' => ['type' => 'string', 'description' => 'PromQL query'],
                'metric' => ['type' => 'string', 'description' => 'Metric name'],
                'value' => ['type' => 'number', 'description' => 'Metric value'],
                'labels' => ['type' => 'object', 'description' => 'Metric labels'],
                'url' => ['type' => 'string', 'description' => 'Prometheus URL'],
                'time' => ['type' => 'string', 'description' => 'Query time'],
                'start' => ['type' => 'string', 'description' => 'Range start'],
                'end' => ['type' => 'string', 'description' => 'Range end'],
                'step' => ['type' => 'string', 'description' => 'Range step'],
                'match' => ['type' => 'array', 'description' => 'Series matchers']
            ],
            'required' => ['action']
        ];
    }
} 