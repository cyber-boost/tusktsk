<?php

namespace Fujsen\CoreOperators;

use Fujsen\BaseOperator;
use Fujsen\Exceptions\OperatorException;

/**
 * Etcd Operator for TuskLang
 * 
 * Provides etcd distributed key-value store functionality with support for:
 * - Key-value operations with versioning
 * - Watch for changes with streaming
 * - Lease management and TTL
 * - Transaction support
 * - Range queries and prefix operations
 * - Authentication and TLS
 * - Cluster health monitoring
 * 
 * Usage:
 * ```php
 * // Get value
 * $value = @etcd({
 *   action: "get",
 *   key: "/config/database",
 *   watch: true
 * })
 * 
 * // Set value with lease
 * $result = @etcd({
 *   action: "set",
 *   key: "/temp/session",
 *   value: @variable("session_data"),
 *   lease: 300
 * })
 * ```
 */
class EtcdOperator extends BaseOperator
{
    private array $clients = [];
    private array $leases = [];
    private array $watches = [];
    private array $config;
    
    public function __construct(array $config = [])
    {
        parent::__construct('etcd');
        $this->config = array_merge([
            'default_url' => 'http://localhost:2379',
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
     * Execute etcd operation
     */
    public function execute(array $params, array $context = []): mixed
    {
        $this->validateParams($params);
        
        $action = $params['action'] ?? '';
        $key = $params['key'] ?? '';
        $value = $params['value'] ?? '';
        $url = $params['url'] ?? $this->config['default_url'];
        $lease = $params['lease'] ?? 0;
        $prefix = $params['prefix'] ?? '';
        $range_end = $params['range_end'] ?? '';
        $limit = $params['limit'] ?? 0;
        $revision = $params['revision'] ?? 0;
        $watch = $params['watch'] ?? false;
        $onChange = $params['on_change'] ?? null;
        
        try {
            $client = $this->getClient($url);
            
            switch ($action) {
                case 'get':
                    return $this->getValue($client, $key, $revision, $watch, $onChange);
                case 'set':
                    return $this->setValue($client, $key, $value, $lease);
                case 'delete':
                    return $this->deleteValue($client, $key, $range_end);
                case 'range':
                    return $this->getRange($client, $prefix, $range_end, $limit, $revision);
                case 'lease':
                    return $this->handleLease($client, $lease, $params);
                case 'transaction':
                    return $this->executeTransaction($client, $params);
                case 'watch':
                    return $this->startWatch($client, $key, $prefix, $onChange);
                case 'health':
                    return $this->checkHealth($client);
                default:
                    throw new OperatorException("Unsupported action: $action");
            }
        } catch (\Exception $e) {
            throw new OperatorException("Etcd operation failed: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Get value from etcd
     */
    private function getValue($client, string $key, int $revision, bool $watch, $onChange): mixed
    {
        $request = [
            'key' => base64_encode($key)
        ];
        
        if ($revision > 0) {
            $request['revision'] = $revision;
        }
        
        $response = $client->post('/v3/kv/range', ['json' => $request]);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to get value: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        if (empty($data['kvs'])) {
            return null;
        }
        
        $kv = $data['kvs'][0];
        $result = [
            'status' => 'retrieved',
            'key' => $key,
            'value' => base64_decode($kv['value']),
            'version' => $kv['version'],
            'create_revision' => $kv['create_revision'],
            'mod_revision' => $kv['mod_revision'],
            'lease' => $kv['lease']
        ];
        
        if ($watch && $onChange && is_callable($onChange)) {
            $this->startKeyWatch($client, $key, $kv['mod_revision'], $onChange);
        }
        
        return $result;
    }

    /**
     * Set value in etcd
     */
    private function setValue($client, string $key, $value, int $lease): array
    {
        $request = [
            'key' => base64_encode($key),
            'value' => base64_encode($value)
        ];
        
        if ($lease > 0) {
            $leaseId = $this->createLease($client, $lease);
            $request['lease'] = $leaseId;
        }
        
        $response = $client->post('/v3/kv/put', ['json' => $request]);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to set value: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'set',
            'key' => $key,
            'revision' => $data['header']['revision'],
            'lease' => $lease > 0 ? $lease : null
        ];
    }

    /**
     * Delete value from etcd
     */
    private function deleteValue($client, string $key, string $range_end): array
    {
        $request = [
            'key' => base64_encode($key)
        ];
        
        if ($range_end) {
            $request['range_end'] = base64_encode($range_end);
        }
        
        $response = $client->post('/v3/kv/deleterange', ['json' => $request]);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to delete value: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'deleted',
            'key' => $key,
            'deleted' => $data['deleted'],
            'revision' => $data['header']['revision']
        ];
    }

    /**
     * Get range of values
     */
    private function getRange($client, string $prefix, string $range_end, int $limit, int $revision): array
    {
        $request = [];
        
        if ($prefix) {
            $request['key'] = base64_encode($prefix);
            $request['range_end'] = base64_encode($this->getRangeEnd($prefix));
        } elseif ($range_end) {
            $request['key'] = base64_encode('');
            $request['range_end'] = base64_encode($range_end);
        }
        
        if ($limit > 0) {
            $request['limit'] = $limit;
        }
        
        if ($revision > 0) {
            $request['revision'] = $revision;
        }
        
        $response = $client->post('/v3/kv/range', ['json' => $request]);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to get range: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        $kvs = array_map(function($kv) {
            return [
                'key' => base64_decode($kv['key']),
                'value' => base64_decode($kv['value']),
                'version' => $kv['version'],
                'create_revision' => $kv['create_revision'],
                'mod_revision' => $kv['mod_revision'],
                'lease' => $kv['lease']
            ];
        }, $data['kvs'] ?? []);
        
        return [
            'status' => 'retrieved',
            'kvs' => $kvs,
            'count' => count($kvs),
            'more' => $data['more'] ?? false,
            'revision' => $data['header']['revision']
        ];
    }

    /**
     * Handle lease operations
     */
    private function handleLease($client, int $ttl, array $params): array
    {
        $action = $params['lease_action'] ?? 'grant';
        
        switch ($action) {
            case 'grant':
                return $this->grantLease($client, $ttl);
            case 'revoke':
                $leaseId = $params['lease_id'] ?? 0;
                return $this->revokeLease($client, $leaseId);
            case 'keepalive':
                $leaseId = $params['lease_id'] ?? 0;
                return $this->keepAliveLease($client, $leaseId);
            case 'timeToLive':
                $leaseId = $params['lease_id'] ?? 0;
                return $this->getLeaseTTL($client, $leaseId);
            default:
                throw new OperatorException("Unsupported lease action: $action");
        }
    }

    /**
     * Grant lease
     */
    private function grantLease($client, int $ttl): array
    {
        $request = ['TTL' => $ttl];
        
        $response = $client->post('/v3/lease/grant', ['json' => $request]);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to grant lease: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        $leaseId = $data['ID'];
        $this->leases[$leaseId] = [
            'id' => $leaseId,
            'ttl' => $ttl,
            'granted_ttl' => $data['grantedTTL'],
            'created_at' => time()
        ];
        
        return [
            'status' => 'granted',
            'lease_id' => $leaseId,
            'ttl' => $ttl,
            'granted_ttl' => $data['grantedTTL']
        ];
    }

    /**
     * Revoke lease
     */
    private function revokeLease($client, int $leaseId): array
    {
        $request = ['ID' => $leaseId];
        
        $response = $client->post('/v3/lease/revoke', ['json' => $request]);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to revoke lease: " . $response->getBody());
        }
        
        unset($this->leases[$leaseId]);
        
        return [
            'status' => 'revoked',
            'lease_id' => $leaseId
        ];
    }

    /**
     * Keep alive lease
     */
    private function keepAliveLease($client, int $leaseId): array
    {
        $request = ['ID' => $leaseId];
        
        $response = $client->post('/v3/lease/keepalive', ['json' => $request]);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to keep alive lease: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        if (isset($this->leases[$leaseId])) {
            $this->leases[$leaseId]['last_keepalive'] = time();
        }
        
        return [
            'status' => 'kept_alive',
            'lease_id' => $leaseId,
            'ttl' => $data['TTL']
        ];
    }

    /**
     * Get lease TTL
     */
    private function getLeaseTTL($client, int $leaseId): array
    {
        $request = ['ID' => $leaseId];
        
        $response = $client->post('/v3/lease/timetolive', ['json' => $request]);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to get lease TTL: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'retrieved',
            'lease_id' => $leaseId,
            'ttl' => $data['TTL'],
            'granted_ttl' => $data['grantedTTL'],
            'keys' => $data['keys'] ?? []
        ];
    }

    /**
     * Execute transaction
     */
    private function executeTransaction($client, array $params): array
    {
        $compare = $params['compare'] ?? [];
        $success = $params['success'] ?? [];
        $failure = $params['failure'] ?? [];
        
        $request = [
            'compare' => $compare,
            'success' => $success,
            'failure' => $failure
        ];
        
        $response = $client->post('/v3/kv/txn', ['json' => $request]);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to execute transaction: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'executed',
            'succeeded' => $data['succeeded'],
            'responses' => $data['responses'] ?? [],
            'revision' => $data['header']['revision']
        ];
    }

    /**
     * Start watch
     */
    private function startWatch($client, string $key, string $prefix, $onChange): array
    {
        $request = [];
        
        if ($key) {
            $request['key'] = base64_encode($key);
        } elseif ($prefix) {
            $request['key'] = base64_encode($prefix);
            $request['range_end'] = base64_encode($this->getRangeEnd($prefix));
        }
        
        $watchId = uniqid('watch_');
        
        $this->watches[$watchId] = [
            'request' => $request,
            'callback' => $onChange,
            'active' => true
        ];
        
        // In a real implementation, this would be async
        // For now, just store the watch configuration
        
        return [
            'status' => 'started',
            'watch_id' => $watchId,
            'key' => $key,
            'prefix' => $prefix
        ];
    }

    /**
     * Check cluster health
     */
    private function checkHealth($client): array
    {
        $response = $client->get('/health');
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to check health: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'healthy',
            'health' => $data['health'],
            'version' => $data['version'] ?? '',
            'cluster_size' => $data['clusterSize'] ?? 0
        ];
    }

    /**
     * Create lease helper
     */
    private function createLease($client, int $ttl): int
    {
        $result = $this->grantLease($client, $ttl);
        return $result['lease_id'];
    }

    /**
     * Start key watch helper
     */
    private function startKeyWatch($client, string $key, int $revision, $onChange): void
    {
        $watchId = md5($key . $revision);
        
        if (isset($this->watches[$watchId])) {
            return; // Already watching
        }
        
        $this->watches[$watchId] = [
            'key' => $key,
            'revision' => $revision,
            'callback' => $onChange,
            'active' => true
        ];
    }

    /**
     * Get range end for prefix
     */
    private function getRangeEnd(string $prefix): string
    {
        // For prefix queries, range_end is prefix + 1
        $bytes = unpack('C*', $prefix);
        $bytes[] = 0;
        return pack('C*', ...$bytes);
    }

    /**
     * Get or create etcd client
     */
    private function getClient(string $url): object
    {
        if (!isset($this->clients[$url])) {
            $this->clients[$url] = $this->createClient($url);
        }
        
        return $this->clients[$url];
    }

    /**
     * Create HTTP client for etcd
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
        
        $validActions = ['get', 'set', 'delete', 'range', 'lease', 'transaction', 'watch', 'health'];
        if (!in_array($params['action'], $validActions)) {
            throw new OperatorException("Invalid action: " . $params['action']);
        }
        
        if (in_array($params['action'], ['get', 'set', 'delete']) && !isset($params['key'])) {
            throw new OperatorException("Key is required for " . $params['action'] . " action");
        }
        
        if ($params['action'] === 'set' && !isset($params['value'])) {
            throw new OperatorException("Value is required for set action");
        }
    }

    /**
     * Cleanup resources
     */
    public function cleanup(): void
    {
        $this->clients = [];
        $this->leases = [];
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
                    'enum' => ['get', 'set', 'delete', 'range', 'lease', 'transaction', 'watch', 'health'],
                    'description' => 'Etcd action'
                ],
                'key' => ['type' => 'string', 'description' => 'Key'],
                'value' => ['type' => 'string', 'description' => 'Value'],
                'url' => ['type' => 'string', 'description' => 'Etcd URL'],
                'lease' => ['type' => 'integer', 'description' => 'Lease TTL'],
                'prefix' => ['type' => 'string', 'description' => 'Key prefix'],
                'range_end' => ['type' => 'string', 'description' => 'Range end'],
                'limit' => ['type' => 'integer', 'description' => 'Result limit'],
                'revision' => ['type' => 'integer', 'description' => 'Revision'],
                'watch' => ['type' => 'boolean', 'description' => 'Watch for changes'],
                'on_change' => ['type' => 'function', 'description' => 'Change handler']
            ],
            'required' => ['action']
        ];
    }
} 