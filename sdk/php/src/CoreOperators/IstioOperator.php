<?php

namespace TuskLang\CoreOperators;

use Fujsen\BaseOperator;
use Fujsen\Exceptions\OperatorException;

/**
 * Istio Operator for TuskLang
 * 
 * Provides Istio service mesh functionality with support for:
 * - Traffic management (VirtualService, DestinationRule)
 * - Security policies (AuthorizationPolicy, PeerAuthentication)
 * - Observability (Telemetry, AccessLog)
 * - Gateway and ServiceEntry management
 * - Workload and Service management
 * - Mesh configuration and status
 * 
 * Usage:
 * ```php
 * // Create virtual service
 * $virtualService = @istio({
 *   action: "create_virtual_service",
 *   name: "user-service",
 *   hosts: ["user-service.example.com"],
 *   gateways: ["user-gateway"],
 *   routes: @variable("service_routes")
 * })
 * 
 * // Apply destination rule
 * $destinationRule = @istio({
 *   action: "create_destination_rule",
 *   name: "user-service",
 *   host: "user-service",
 *   subsets: @variable("service_subsets")
 * })
 * ```
 */
class IstioOperator extends \TuskLang\CoreOperators\BaseOperator
{
    private array $clients = [];
    private array $resources = [];
    private array $config;
    
    public function __construct(array $config = [])
    {
        parent::__construct('istio');
        $this->config = array_merge([
            'default_url' => 'http://localhost:8080',
            'timeout' => 30,
            'retry_attempts' => 3,
            'retry_delay' => 1000,
            'enable_tls' => false,
            'tls_verify' => true,
            'tls_cert' => '',
            'tls_key' => '',
            'tls_ca' => '',
            'api_key' => '',
            'namespace' => 'default'
        ], $config);
    }

    /**
     * Execute Istio operation
     */
    public function execute(array $params, array $context = []): mixed
    {
        $this->validateParams($params);
        
        $action = $params['action'] ?? '';
        $name = $params['name'] ?? '';
        $namespace = $params['namespace'] ?? $this->config['namespace'];
        $url = $params['url'] ?? $this->config['default_url'];
        $hosts = $params['hosts'] ?? [];
        $gateways = $params['gateways'] ?? [];
        $routes = $params['routes'] ?? [];
        $host = $params['host'] ?? '';
        $subsets = $params['subsets'] ?? [];
        $policy = $params['policy'] ?? [];
        
        try {
            $client = $this->getClient($url);
            
            switch ($action) {
                case 'create_virtual_service':
                    return $this->createVirtualService($client, $name, $namespace, $hosts, $gateways, $routes);
                case 'get_virtual_service':
                    return $this->getVirtualService($client, $name, $namespace);
                case 'update_virtual_service':
                    return $this->updateVirtualService($client, $name, $namespace, $params);
                case 'delete_virtual_service':
                    return $this->deleteVirtualService($client, $name, $namespace);
                case 'create_destination_rule':
                    return $this->createDestinationRule($client, $name, $namespace, $host, $subsets);
                case 'get_destination_rule':
                    return $this->getDestinationRule($client, $name, $namespace);
                case 'update_destination_rule':
                    return $this->updateDestinationRule($client, $name, $namespace, $params);
                case 'delete_destination_rule':
                    return $this->deleteDestinationRule($client, $name, $namespace);
                case 'create_gateway':
                    return $this->createGateway($client, $name, $namespace, $params);
                case 'get_gateway':
                    return $this->getGateway($client, $name, $namespace);
                case 'create_authorization_policy':
                    return $this->createAuthorizationPolicy($client, $name, $namespace, $policy);
                case 'get_authorization_policy':
                    return $this->getAuthorizationPolicy($client, $name, $namespace);
                case 'create_peer_authentication':
                    return $this->createPeerAuthentication($client, $name, $namespace, $params);
                case 'get_peer_authentication':
                    return $this->getPeerAuthentication($client, $name, $namespace);
                case 'create_service_entry':
                    return $this->createServiceEntry($client, $name, $namespace, $params);
                case 'get_service_entry':
                    return $this->getServiceEntry($client, $name, $namespace);
                case 'get_workloads':
                    return $this->getWorkloads($client, $namespace);
                case 'get_services':
                    return $this->getServices($client, $namespace);
                case 'get_mesh_status':
                    return $this->getMeshStatus($client);
                default:
                    throw new OperatorException("Unsupported action: $action");
            }
        } catch (\Exception $e) {
            throw new OperatorException("Istio operation failed: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Create VirtualService
     */
    private function createVirtualService($client, string $name, string $namespace, array $hosts, array $gateways, array $routes): array
    {
        $virtualService = [
            'apiVersion' => 'networking.istio.io/v1beta1',
            'kind' => 'VirtualService',
            'metadata' => [
                'name' => $name,
                'namespace' => $namespace
            ],
            'spec' => [
                'hosts' => $hosts,
                'gateways' => $gateways,
                'http' => $routes
            ]
        ];
        
        $response = $client->post("/apis/networking.istio.io/v1beta1/namespaces/$namespace/virtualservices", [
            'json' => $virtualService
        ]);
        
        if ($response->getStatusCode() !== 200 && $response->getStatusCode() !== 201) {
            throw new OperatorException("Failed to create VirtualService: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        $this->resources['virtualservice'][$name] = [
            'name' => $name,
            'namespace' => $namespace,
            'uid' => $data['metadata']['uid']
        ];
        
        return [
            'status' => 'created',
            'name' => $name,
            'namespace' => $namespace,
            'uid' => $data['metadata']['uid']
        ];
    }

    /**
     * Get VirtualService
     */
    private function getVirtualService($client, string $name, string $namespace): array
    {
        $response = $client->get("/apis/networking.istio.io/v1beta1/namespaces/$namespace/virtualservices/$name");
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to get VirtualService: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'retrieved',
            'virtual_service' => $data
        ];
    }

    /**
     * Update VirtualService
     */
    private function updateVirtualService($client, string $name, string $namespace, array $params): array
    {
        $virtualService = $params['virtual_service'] ?? [];
        
        $response = $client->put("/apis/networking.istio.io/v1beta1/namespaces/$namespace/virtualservices/$name", [
            'json' => $virtualService
        ]);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to update VirtualService: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'updated',
            'name' => $name,
            'namespace' => $namespace,
            'uid' => $data['metadata']['uid']
        ];
    }

    /**
     * Delete VirtualService
     */
    private function deleteVirtualService($client, string $name, string $namespace): array
    {
        $response = $client->delete("/apis/networking.istio.io/v1beta1/namespaces/$namespace/virtualservices/$name");
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to delete VirtualService: " . $response->getBody());
        }
        
        unset($this->resources['virtualservice'][$name]);
        
        return [
            'status' => 'deleted',
            'name' => $name,
            'namespace' => $namespace
        ];
    }

    /**
     * Create DestinationRule
     */
    private function createDestinationRule($client, string $name, string $namespace, string $host, array $subsets): array
    {
        $destinationRule = [
            'apiVersion' => 'networking.istio.io/v1beta1',
            'kind' => 'DestinationRule',
            'metadata' => [
                'name' => $name,
                'namespace' => $namespace
            ],
            'spec' => [
                'host' => $host,
                'subsets' => $subsets
            ]
        ];
        
        $response = $client->post("/apis/networking.istio.io/v1beta1/namespaces/$namespace/destinationrules", [
            'json' => $destinationRule
        ]);
        
        if ($response->getStatusCode() !== 200 && $response->getStatusCode() !== 201) {
            throw new OperatorException("Failed to create DestinationRule: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        $this->resources['destinationrule'][$name] = [
            'name' => $name,
            'namespace' => $namespace,
            'uid' => $data['metadata']['uid']
        ];
        
        return [
            'status' => 'created',
            'name' => $name,
            'namespace' => $namespace,
            'uid' => $data['metadata']['uid']
        ];
    }

    /**
     * Get DestinationRule
     */
    private function getDestinationRule($client, string $name, string $namespace): array
    {
        $response = $client->get("/apis/networking.istio.io/v1beta1/namespaces/$namespace/destinationrules/$name");
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to get DestinationRule: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'retrieved',
            'destination_rule' => $data
        ];
    }

    /**
     * Update DestinationRule
     */
    private function updateDestinationRule($client, string $name, string $namespace, array $params): array
    {
        $destinationRule = $params['destination_rule'] ?? [];
        
        $response = $client->put("/apis/networking.istio.io/v1beta1/namespaces/$namespace/destinationrules/$name", [
            'json' => $destinationRule
        ]);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to update DestinationRule: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'updated',
            'name' => $name,
            'namespace' => $namespace,
            'uid' => $data['metadata']['uid']
        ];
    }

    /**
     * Delete DestinationRule
     */
    private function deleteDestinationRule($client, string $name, string $namespace): array
    {
        $response = $client->delete("/apis/networking.istio.io/v1beta1/namespaces/$namespace/destinationrules/$name");
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to delete DestinationRule: " . $response->getBody());
        }
        
        unset($this->resources['destinationrule'][$name]);
        
        return [
            'status' => 'deleted',
            'name' => $name,
            'namespace' => $namespace
        ];
    }

    /**
     * Create Gateway
     */
    private function createGateway($client, string $name, string $namespace, array $params): array
    {
        $gateway = [
            'apiVersion' => 'networking.istio.io/v1beta1',
            'kind' => 'Gateway',
            'metadata' => [
                'name' => $name,
                'namespace' => $namespace
            ],
            'spec' => $params['spec'] ?? []
        ];
        
        $response = $client->post("/apis/networking.istio.io/v1beta1/namespaces/$namespace/gateways", [
            'json' => $gateway
        ]);
        
        if ($response->getStatusCode() !== 200 && $response->getStatusCode() !== 201) {
            throw new OperatorException("Failed to create Gateway: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'created',
            'name' => $name,
            'namespace' => $namespace,
            'uid' => $data['metadata']['uid']
        ];
    }

    /**
     * Get Gateway
     */
    private function getGateway($client, string $name, string $namespace): array
    {
        $response = $client->get("/apis/networking.istio.io/v1beta1/namespaces/$namespace/gateways/$name");
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to get Gateway: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'retrieved',
            'gateway' => $data
        ];
    }

    /**
     * Create AuthorizationPolicy
     */
    private function createAuthorizationPolicy($client, string $name, string $namespace, array $policy): array
    {
        $authorizationPolicy = [
            'apiVersion' => 'security.istio.io/v1beta1',
            'kind' => 'AuthorizationPolicy',
            'metadata' => [
                'name' => $name,
                'namespace' => $namespace
            ],
            'spec' => $policy
        ];
        
        $response = $client->post("/apis/security.istio.io/v1beta1/namespaces/$namespace/authorizationpolicies", [
            'json' => $authorizationPolicy
        ]);
        
        if ($response->getStatusCode() !== 200 && $response->getStatusCode() !== 201) {
            throw new OperatorException("Failed to create AuthorizationPolicy: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'created',
            'name' => $name,
            'namespace' => $namespace,
            'uid' => $data['metadata']['uid']
        ];
    }

    /**
     * Get AuthorizationPolicy
     */
    private function getAuthorizationPolicy($client, string $name, string $namespace): array
    {
        $response = $client->get("/apis/security.istio.io/v1beta1/namespaces/$namespace/authorizationpolicies/$name");
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to get AuthorizationPolicy: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'retrieved',
            'authorization_policy' => $data
        ];
    }

    /**
     * Create PeerAuthentication
     */
    private function createPeerAuthentication($client, string $name, string $namespace, array $params): array
    {
        $peerAuthentication = [
            'apiVersion' => 'security.istio.io/v1beta1',
            'kind' => 'PeerAuthentication',
            'metadata' => [
                'name' => $name,
                'namespace' => $namespace
            ],
            'spec' => $params['spec'] ?? []
        ];
        
        $response = $client->post("/apis/security.istio.io/v1beta1/namespaces/$namespace/peerauthentications", [
            'json' => $peerAuthentication
        ]);
        
        if ($response->getStatusCode() !== 200 && $response->getStatusCode() !== 201) {
            throw new OperatorException("Failed to create PeerAuthentication: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'created',
            'name' => $name,
            'namespace' => $namespace,
            'uid' => $data['metadata']['uid']
        ];
    }

    /**
     * Get PeerAuthentication
     */
    private function getPeerAuthentication($client, string $name, string $namespace): array
    {
        $response = $client->get("/apis/security.istio.io/v1beta1/namespaces/$namespace/peerauthentications/$name");
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to get PeerAuthentication: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'retrieved',
            'peer_authentication' => $data
        ];
    }

    /**
     * Create ServiceEntry
     */
    private function createServiceEntry($client, string $name, string $namespace, array $params): array
    {
        $serviceEntry = [
            'apiVersion' => 'networking.istio.io/v1beta1',
            'kind' => 'ServiceEntry',
            'metadata' => [
                'name' => $name,
                'namespace' => $namespace
            ],
            'spec' => $params['spec'] ?? []
        ];
        
        $response = $client->post("/apis/networking.istio.io/v1beta1/namespaces/$namespace/serviceentries", [
            'json' => $serviceEntry
        ]);
        
        if ($response->getStatusCode() !== 200 && $response->getStatusCode() !== 201) {
            throw new OperatorException("Failed to create ServiceEntry: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'created',
            'name' => $name,
            'namespace' => $namespace,
            'uid' => $data['metadata']['uid']
        ];
    }

    /**
     * Get ServiceEntry
     */
    private function getServiceEntry($client, string $name, string $namespace): array
    {
        $response = $client->get("/apis/networking.istio.io/v1beta1/namespaces/$namespace/serviceentries/$name");
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to get ServiceEntry: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'retrieved',
            'service_entry' => $data
        ];
    }

    /**
     * Get workloads
     */
    private function getWorkloads($client, string $namespace): array
    {
        $response = $client->get("/api/v1/namespaces/$namespace/pods");
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to get workloads: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'retrieved',
            'namespace' => $namespace,
            'workloads' => $data['items'] ?? []
        ];
    }

    /**
     * Get services
     */
    private function getServices($client, string $namespace): array
    {
        $response = $client->get("/api/v1/namespaces/$namespace/services");
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to get services: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'retrieved',
            'namespace' => $namespace,
            'services' => $data['items'] ?? []
        ];
    }

    /**
     * Get mesh status
     */
    private function getMeshStatus($client): array
    {
        $response = $client->get("/apis/networking.istio.io/v1beta1/namespaces/istio-system/virtualservices");
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to get mesh status: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'retrieved',
            'virtual_services' => $data['items'] ?? [],
            'total' => count($data['items'] ?? [])
        ];
    }

    /**
     * Get or create Istio client
     */
    private function getClient(string $url): object
    {
        if (!isset($this->clients[$url])) {
            $this->clients[$url] = $this->createClient($url);
        }
        
        return $this->clients[$url];
    }

    /**
     * Create HTTP client for Istio
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
        
        $validActions = ['create_virtual_service', 'get_virtual_service', 'update_virtual_service', 'delete_virtual_service', 'create_destination_rule', 'get_destination_rule', 'update_destination_rule', 'delete_destination_rule', 'create_gateway', 'get_gateway', 'create_authorization_policy', 'get_authorization_policy', 'create_peer_authentication', 'get_peer_authentication', 'create_service_entry', 'get_service_entry', 'get_workloads', 'get_services', 'get_mesh_status'];
        if (!in_array($params['action'], $validActions)) {
            throw new OperatorException("Invalid action: " . $params['action']);
        }
        
        if (in_array($params['action'], ['create_virtual_service']) && (!isset($params['name']) || !isset($params['hosts']) || !isset($params['routes']))) {
            throw new OperatorException("Name, hosts, and routes are required for create_virtual_service action");
        }
        
        if (in_array($params['action'], ['get_virtual_service', 'update_virtual_service', 'delete_virtual_service']) && !isset($params['name'])) {
            throw new OperatorException("Name is required for " . $params['action'] . " action");
        }
        
        if (in_array($params['action'], ['create_destination_rule']) && (!isset($params['name']) || !isset($params['host']) || !isset($params['subsets']))) {
            throw new OperatorException("Name, host, and subsets are required for create_destination_rule action");
        }
        
        if (in_array($params['action'], ['get_destination_rule', 'update_destination_rule', 'delete_destination_rule']) && !isset($params['name'])) {
            throw new OperatorException("Name is required for " . $params['action'] . " action");
        }
        
        if (in_array($params['action'], ['create_gateway', 'get_gateway']) && !isset($params['name'])) {
            throw new OperatorException("Name is required for " . $params['action'] . " action");
        }
        
        if (in_array($params['action'], ['create_authorization_policy']) && (!isset($params['name']) || !isset($params['policy']))) {
            throw new OperatorException("Name and policy are required for create_authorization_policy action");
        }
        
        if (in_array($params['action'], ['get_authorization_policy']) && !isset($params['name'])) {
            throw new OperatorException("Name is required for get_authorization_policy action");
        }
        
        if (in_array($params['action'], ['create_peer_authentication', 'get_peer_authentication']) && !isset($params['name'])) {
            throw new OperatorException("Name is required for " . $params['action'] . " action");
        }
        
        if (in_array($params['action'], ['create_service_entry', 'get_service_entry']) && !isset($params['name'])) {
            throw new OperatorException("Name is required for " . $params['action'] . " action");
        }
    }

    /**
     * Cleanup resources
     */
    public function cleanup(): void
    {
        $this->clients = [];
        $this->resources = [];
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
                    'enum' => ['create_virtual_service', 'get_virtual_service', 'update_virtual_service', 'delete_virtual_service', 'create_destination_rule', 'get_destination_rule', 'update_destination_rule', 'delete_destination_rule', 'create_gateway', 'get_gateway', 'create_authorization_policy', 'get_authorization_policy', 'create_peer_authentication', 'get_peer_authentication', 'create_service_entry', 'get_service_entry', 'get_workloads', 'get_services', 'get_mesh_status'],
                    'description' => 'Istio action'
                ],
                'name' => ['type' => 'string', 'description' => 'Resource name'],
                'namespace' => ['type' => 'string', 'description' => 'Namespace'],
                'url' => ['type' => 'string', 'description' => 'Istio API URL'],
                'hosts' => ['type' => 'array', 'description' => 'VirtualService hosts'],
                'gateways' => ['type' => 'array', 'description' => 'VirtualService gateways'],
                'routes' => ['type' => 'array', 'description' => 'VirtualService routes'],
                'host' => ['type' => 'string', 'description' => 'DestinationRule host'],
                'subsets' => ['type' => 'array', 'description' => 'DestinationRule subsets'],
                'policy' => ['type' => 'object', 'description' => 'AuthorizationPolicy spec']
            ],
            'required' => ['action']
        ];
    }
} 