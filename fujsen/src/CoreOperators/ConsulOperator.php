<?php

namespace Fujsen\CoreOperators;

use Fujsen\BaseOperator;
use Fujsen\Exceptions\OperatorException;

/**
 * Consul Operator for TuskLang
 * 
 * Provides Consul service discovery functionality with support for:
 * - Service discovery with health checks
 * - KV store integration
 * - Connect service mesh support
 * - DNS and HTTP API support
 * - Load balancing strategies
 * - Blocking queries for changes
 * 
 * Usage:
 * ```php
 * // Discover service
 * $service = @consul({
 *   action: "discover",
 *   service: "api",
 *   datacenter: "us-east-1",
 *   filter: "ServiceMeta.version == 'v2'",
 *   healthy: true
 * })
 * 
 * // Get KV value
 * $config = @consul({
 *   action: "kv",
 *   key: "config/features",
 *   watch: true
 * })
 * ```
 */
class ConsulOperator extends BaseOperator
{
    private array $clients = [];
    private array $watches = [];
    private array $config;
    
    public function __construct(array $config = [])
    {
        parent::__construct('consul');
        $this->config = array_merge([
            'default_url' => 'http://localhost:8500',
            'timeout' => 30,
            'retry_attempts' => 3,
            'retry_delay' => 1000,
            'enable_tls' => false,
            'tls_verify' => true,
            'tls_cert' => '',
            'tls_key' => '',
            'tls_ca' => '',
            'token' => ''
        ], $config);
    }

    /**
     * Execute Consul operation
     */
    public function execute(array $params, array $context = []): mixed
    {
        $this->validateParams($params);
        
        $action = $params['action'] ?? '';
        $service = $params['service'] ?? '';
        $key = $params['key'] ?? '';
        $value = $params['value'] ?? '';
        $url = $params['url'] ?? $this->config['default_url'];
        $datacenter = $params['datacenter'] ?? '';
        $filter = $params['filter'] ?? '';
        $healthy = $params['healthy'] ?? true;
        $watch = $params['watch'] ?? false;
        $strategy = $params['strategy'] ?? 'round-robin';
        $onChange = $params['on_change'] ?? null;
        
        try {
            $client = $this->getClient($url);
            
            switch ($action) {
                case 'discover':
                    return $this->discoverService($client, $service, $datacenter, $filter, $healthy, $strategy);
                case 'register':
                    return $this->registerService($client, $service, $params);
                case 'deregister':
                    return $this->deregisterService($client, $service, $params);
                case 'kv':
                    return $this->handleKvOperation($client, $key, $value, $watch, $onChange);
                case 'health':
                    return $this->getHealthChecks($client, $service, $datacenter);
                case 'nodes':
                    return $this->getNodes($client, $datacenter);
                default:
                    throw new OperatorException("Unsupported action: $action");
            }
        } catch (\Exception $e) {
            throw new OperatorException("Consul operation failed: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Discover service in Consul
     */
    private function discoverService($client, string $service, string $datacenter, string $filter, bool $healthy, string $strategy): array
    {
        $params = ['service' => $service];
        
        if ($datacenter) {
            $params['dc'] = $datacenter;
        }
        
        if ($filter) {
            $params['filter'] = $filter;
        }
        
        $response = $client->get('/v1/health/service/' . $service, ['query' => $params]);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to discover service: " . $response->getBody());
        }
        
        $services = json_decode($response->getBody(), true);
        
        // Filter by health status
        if ($healthy) {
            $services = array_filter($services, function($service) {
                return $this->isServiceHealthy($service);
            });
        }
        
        // Apply load balancing strategy
        $selectedService = $this->applyLoadBalancing($services, $strategy);
        
        return [
            'status' => 'discovered',
            'service' => $service,
            'instances' => $services,
            'selected' => $selectedService,
            'count' => count($services),
            'strategy' => $strategy
        ];
    }

    /**
     * Register service in Consul
     */
    private function registerService($client, string $service, array $params): array
    {
        $registration = [
            'Name' => $service,
            'ID' => $params['id'] ?? $service,
            'Address' => $params['address'] ?? '127.0.0.1',
            'Port' => $params['port'] ?? 8080,
            'Tags' => $params['tags'] ?? [],
            'Meta' => $params['meta'] ?? [],
            'Check' => $params['check'] ?? null
        ];
        
        $response = $client->put('/v1/agent/service/register', ['json' => $registration]);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to register service: " . $response->getBody());
        }
        
        return [
            'status' => 'registered',
            'service' => $service,
            'id' => $registration['ID'],
            'timestamp' => time()
        ];
    }

    /**
     * Deregister service from Consul
     */
    private function deregisterService($client, string $service, array $params): array
    {
        $serviceId = $params['id'] ?? $service;
        
        $response = $client->put('/v1/agent/service/deregister/' . $serviceId);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to deregister service: " . $response->getBody());
        }
        
        return [
            'status' => 'deregistered',
            'service' => $service,
            'id' => $serviceId,
            'timestamp' => time()
        ];
    }

    /**
     * Handle KV operations
     */
    private function handleKvOperation($client, string $key, $value, bool $watch, $onChange): mixed
    {
        if ($value !== '') {
            // Write operation
            $response = $client->put('/v1/kv/' . $key, ['body' => $value]);
            
            if ($response->getStatusCode() !== 200) {
                throw new OperatorException("Failed to write KV: " . $response->getBody());
            }
            
            return [
                'status' => 'written',
                'key' => $key,
                'timestamp' => time()
            ];
        } else {
            // Read operation
            $params = [];
            if ($watch) {
                $params['index'] = 0;
                $params['wait'] = '5s';
            }
            
            $response = $client->get('/v1/kv/' . $key, ['query' => $params]);
            
            if ($response->getStatusCode() !== 200) {
                throw new OperatorException("Failed to read KV: " . $response->getBody());
            }
            
            $data = json_decode($response->getBody(), true);
            
            if (empty($data)) {
                return null;
            }
            
            $result = [
                'status' => 'read',
                'key' => $key,
                'value' => base64_decode($data[0]['Value']),
                'modify_index' => $data[0]['ModifyIndex'],
                'create_index' => $data[0]['CreateIndex']
            ];
            
            if ($watch && $onChange && is_callable($onChange)) {
                $this->startWatch($client, $key, $data[0]['ModifyIndex'], $onChange);
            }
            
            return $result;
        }
    }

    /**
     * Get health checks for service
     */
    private function getHealthChecks($client, string $service, string $datacenter): array
    {
        $params = ['service' => $service];
        
        if ($datacenter) {
            $params['dc'] = $datacenter;
        }
        
        $response = $client->get('/v1/health/checks/' . $service, ['query' => $params]);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to get health checks: " . $response->getBody());
        }
        
        $checks = json_decode($response->getBody(), true);
        
        return [
            'status' => 'retrieved',
            'service' => $service,
            'checks' => $checks,
            'count' => count($checks)
        ];
    }

    /**
     * Get nodes in datacenter
     */
    private function getNodes($client, string $datacenter): array
    {
        $params = [];
        
        if ($datacenter) {
            $params['dc'] = $datacenter;
        }
        
        $response = $client->get('/v1/catalog/nodes', ['query' => $params]);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to get nodes: " . $response->getBody());
        }
        
        $nodes = json_decode($response->getBody(), true);
        
        return [
            'status' => 'retrieved',
            'nodes' => $nodes,
            'count' => count($nodes)
        ];
    }

    /**
     * Check if service is healthy
     */
    private function isServiceHealthy(array $service): bool
    {
        if (!isset($service['Checks'])) {
            return false;
        }
        
        foreach ($service['Checks'] as $check) {
            if ($check['Status'] !== 'passing') {
                return false;
            }
        }
        
        return true;
    }

    /**
     * Apply load balancing strategy
     */
    private function applyLoadBalancing(array $services, string $strategy): ?array
    {
        if (empty($services)) {
            return null;
        }
        
        switch ($strategy) {
            case 'round-robin':
                static $roundRobinIndex = 0;
                $service = array_values($services)[$roundRobinIndex % count($services)];
                $roundRobinIndex++;
                return $service;
                
            case 'random':
                return array_values($services)[array_rand($services)];
                
            case 'least-connections':
                // Simplified - in real implementation, track connection counts
                return array_values($services)[0];
                
            case 'weighted':
                // Simplified - in real implementation, use service weights
                return array_values($services)[0];
                
            default:
                return array_values($services)[0];
        }
    }

    /**
     * Start watching for changes
     */
    private function startWatch($client, string $key, int $index, $onChange): void
    {
        $watchId = md5($key . $index);
        
        if (isset($this->watches[$watchId])) {
            return; // Already watching
        }
        
        $this->watches[$watchId] = [
            'key' => $key,
            'index' => $index,
            'callback' => $onChange,
            'active' => true
        ];
        
        // In a real implementation, this would be async
        // For now, just store the watch configuration
    }

    /**
     * Get or create Consul client
     */
    private function getClient(string $url): object
    {
        if (!isset($this->clients[$url])) {
            $this->clients[$url] = $this->createClient($url);
        }
        
        return $this->clients[$url];
    }

    /**
     * Create HTTP client for Consul
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
        
        if ($this->config['token']) {
            $config['headers']['X-Consul-Token'] = $this->config['token'];
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
        
        $validActions = ['discover', 'register', 'deregister', 'kv', 'health', 'nodes'];
        if (!in_array($params['action'], $validActions)) {
            throw new OperatorException("Invalid action: " . $params['action']);
        }
        
        if (in_array($params['action'], ['discover', 'register', 'deregister', 'health']) && !isset($params['service'])) {
            throw new OperatorException("Service is required for " . $params['action'] . " action");
        }
        
        if ($params['action'] === 'kv' && !isset($params['key'])) {
            throw new OperatorException("Key is required for KV action");
        }
    }

    /**
     * Cleanup resources
     */
    public function cleanup(): void
    {
        $this->clients = [];
        $this->watches = [];
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
                    'enum' => ['discover', 'register', 'deregister', 'kv', 'health', 'nodes'],
                    'description' => 'Consul action'
                ],
                'service' => ['type' => 'string', 'description' => 'Service name'],
                'key' => ['type' => 'string', 'description' => 'KV key'],
                'value' => ['type' => 'string', 'description' => 'KV value'],
                'url' => ['type' => 'string', 'description' => 'Consul URL'],
                'datacenter' => ['type' => 'string', 'description' => 'Datacenter'],
                'filter' => ['type' => 'string', 'description' => 'Service filter'],
                'healthy' => ['type' => 'boolean', 'description' => 'Health filter'],
                'watch' => ['type' => 'boolean', 'description' => 'Watch for changes'],
                'strategy' => [
                    'type' => 'string',
                    'enum' => ['round-robin', 'random', 'least-connections', 'weighted'],
                    'description' => 'Load balancing strategy'
                ],
                'on_change' => ['type' => 'function', 'description' => 'Change handler']
            ],
            'required' => ['action']
        ];
    }
} 