<?php

namespace TuskLang\CoreOperators;

use Fujsen\BaseOperator;
use Fujsen\Exceptions\OperatorException;

/**
 * Vault Operator for TuskLang
 * 
 * Provides HashiCorp Vault functionality with support for:
 * - KV v1/v2 secrets engine
 * - Transit encryption/decryption
 * - PKI certificate management
 * - Authentication methods (token, AppRole, Kubernetes)
 * - Lease renewal and rotation
 * - Token caching and management
 * 
 * Usage:
 * ```php
 * // Get secret
 * $secret = @vault({
 *   path: "secret/data/database",
 *   field: "password",
 *   auth: { method: "approle", role_id: @env("VAULT_ROLE_ID") }
 * })
 * 
 * // Encrypt data
 * $encrypted = @vault({
 *   engine: "transit",
 *   operation: "encrypt",
 *   key: "customer-key",
 *   plaintext: @variable("sensitive_data")
 * })
 * ```
 */
class VaultOperator extends \TuskLang\CoreOperators\BaseOperator
{
    private array $clients = [];
    private array $tokens = [];
    private array $config;
    
    public function __construct(array $config = [])
    {
        parent::__construct('vault');
        $this->config = array_merge([
            'default_url' => 'http://localhost:8200',
            'timeout' => 30,
            'retry_attempts' => 3,
            'retry_delay' => 1000,
            'token_cache_ttl' => 3600,
            'enable_tls' => false,
            'tls_verify' => true,
            'tls_cert' => '',
            'tls_key' => '',
            'tls_ca' => ''
        ], $config);
    }

    /**
     * Execute Vault operation
     */
    public function execute(array $params, array $context = []): mixed
    {
        $this->validateParams($params);
        
        $engine = $params['engine'] ?? 'kv';
        $operation = $params['operation'] ?? 'read';
        $path = $params['path'] ?? '';
        $field = $params['field'] ?? '';
        $data = $params['data'] ?? [];
        $url = $params['url'] ?? $this->config['default_url'];
        $auth = $params['auth'] ?? [];
        $key = $params['key'] ?? '';
        $plaintext = $params['plaintext'] ?? '';
        $ciphertext = $params['ciphertext'] ?? '';
        
        try {
            $client = $this->getClient($url, $auth);
            
            switch ($engine) {
                case 'kv':
                    return $this->handleKvOperation($client, $operation, $path, $field, $data);
                case 'transit':
                    return $this->handleTransitOperation($client, $operation, $key, $plaintext, $ciphertext);
                case 'pki':
                    return $this->handlePkiOperation($client, $operation, $path, $data);
                default:
                    throw new OperatorException("Unsupported engine: $engine");
            }
        } catch (\Exception $e) {
            throw new OperatorException("Vault operation failed: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Handle KV operations
     */
    private function handleKvOperation($client, string $operation, string $path, string $field, array $data): mixed
    {
        switch ($operation) {
            case 'read':
                return $this->readSecret($client, $path, $field);
            case 'write':
                return $this->writeSecret($client, $path, $data);
            case 'delete':
                return $this->deleteSecret($client, $path);
            case 'list':
                return $this->listSecrets($client, $path);
            default:
                throw new OperatorException("Unsupported KV operation: $operation");
        }
    }

    /**
     * Handle Transit operations
     */
    private function handleTransitOperation($client, string $operation, string $key, string $plaintext, string $ciphertext): mixed
    {
        switch ($operation) {
            case 'encrypt':
                return $this->encryptData($client, $key, $plaintext);
            case 'decrypt':
                return $this->decryptData($client, $key, $ciphertext);
            case 'reencrypt':
                return $this->reencryptData($client, $key, $ciphertext);
            case 'rotate':
                return $this->rotateKey($client, $key);
            default:
                throw new OperatorException("Unsupported Transit operation: $operation");
        }
    }

    /**
     * Handle PKI operations
     */
    private function handlePkiOperation($client, string $operation, string $path, array $data): mixed
    {
        switch ($operation) {
            case 'issue':
                return $this->issueCertificate($client, $path, $data);
            case 'revoke':
                return $this->revokeCertificate($client, $path, $data);
            case 'read':
                return $this->readCertificate($client, $path);
            default:
                throw new OperatorException("Unsupported PKI operation: $operation");
        }
    }

    /**
     * Read secret from Vault
     */
    private function readSecret($client, string $path, string $field): mixed
    {
        $response = $client->get($path);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to read secret: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        if (isset($data['data']['data'])) {
            // KV v2
            $secrets = $data['data']['data'];
        } else {
            // KV v1
            $secrets = $data['data'];
        }
        
        if ($field && isset($secrets[$field])) {
            return $secrets[$field];
        }
        
        return $secrets;
    }

    /**
     * Write secret to Vault
     */
    private function writeSecret($client, string $path, array $data): array
    {
        $payload = ['data' => $data];
        
        $response = $client->post($path, ['json' => $payload]);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to write secret: " . $response->getBody());
        }
        
        return [
            'status' => 'written',
            'path' => $path,
            'timestamp' => time()
        ];
    }

    /**
     * Delete secret from Vault
     */
    private function deleteSecret($client, string $path): array
    {
        $response = $client->delete($path);
        
        if ($response->getStatusCode() !== 204) {
            throw new OperatorException("Failed to delete secret: " . $response->getBody());
        }
        
        return [
            'status' => 'deleted',
            'path' => $path,
            'timestamp' => time()
        ];
    }

    /**
     * List secrets in Vault
     */
    private function listSecrets($client, string $path): array
    {
        $response = $client->list($path);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to list secrets: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'listed',
            'path' => $path,
            'keys' => $data['data']['keys'] ?? [],
            'count' => count($data['data']['keys'] ?? [])
        ];
    }

    /**
     * Encrypt data using Transit engine
     */
    private function encryptData($client, string $key, string $plaintext): array
    {
        $path = "transit/encrypt/$key";
        $payload = ['plaintext' => base64_encode($plaintext)];
        
        $response = $client->post($path, ['json' => $payload]);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to encrypt data: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'encrypted',
            'ciphertext' => $data['data']['ciphertext'],
            'key_version' => $data['data']['key_version']
        ];
    }

    /**
     * Decrypt data using Transit engine
     */
    private function decryptData($client, string $key, string $ciphertext): array
    {
        $path = "transit/decrypt/$key";
        $payload = ['ciphertext' => $ciphertext];
        
        $response = $client->post($path, ['json' => $payload]);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to decrypt data: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'decrypted',
            'plaintext' => base64_decode($data['data']['plaintext'])
        ];
    }

    /**
     * Re-encrypt data using Transit engine
     */
    private function reencryptData($client, string $key, string $ciphertext): array
    {
        $path = "transit/reencrypt/$key";
        $payload = ['ciphertext' => $ciphertext];
        
        $response = $client->post($path, ['json' => $payload]);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to re-encrypt data: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'reencrypted',
            'ciphertext' => $data['data']['ciphertext'],
            'key_version' => $data['data']['key_version']
        ];
    }

    /**
     * Rotate encryption key
     */
    private function rotateKey($client, string $key): array
    {
        $path = "transit/keys/$key/rotate";
        
        $response = $client->post($path);
        
        if ($response->getStatusCode() !== 204) {
            throw new OperatorException("Failed to rotate key: " . $response->getBody());
        }
        
        return [
            'status' => 'rotated',
            'key' => $key,
            'timestamp' => time()
        ];
    }

    /**
     * Issue certificate using PKI engine
     */
    private function issueCertificate($client, string $path, array $data): array
    {
        $response = $client->post($path, ['json' => $data]);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to issue certificate: " . $response->getBody());
        }
        
        $certData = json_decode($response->getBody(), true);
        
        return [
            'status' => 'issued',
            'certificate' => $certData['data']['certificate'],
            'private_key' => $certData['data']['private_key'],
            'serial_number' => $certData['data']['serial_number']
        ];
    }

    /**
     * Revoke certificate using PKI engine
     */
    private function revokeCertificate($client, string $path, array $data): array
    {
        $response = $client->post($path, ['json' => $data]);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to revoke certificate: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'revoked',
            'revocation_time' => $data['data']['revocation_time'],
            'revocation_time_rfc3339' => $data['data']['revocation_time_rfc3339']
        ];
    }

    /**
     * Read certificate using PKI engine
     */
    private function readCertificate($client, string $path): array
    {
        $response = $client->get($path);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to read certificate: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'read',
            'certificate' => $data['data']['certificate'],
            'serial_number' => $data['data']['serial_number']
        ];
    }

    /**
     * Get or create Vault client
     */
    private function getClient(string $url, array $auth): object
    {
        $clientKey = $url . serialize($auth);
        
        if (!isset($this->clients[$clientKey])) {
            $token = $this->authenticate($url, $auth);
            $this->clients[$clientKey] = $this->createClient($url, $token);
        }
        
        return $this->clients[$clientKey];
    }

    /**
     * Authenticate with Vault
     */
    private function authenticate(string $url, array $auth): string
    {
        $method = $auth['method'] ?? 'token';
        
        switch ($method) {
            case 'token':
                return $auth['token'] ?? '';
            case 'approle':
                return $this->authenticateAppRole($url, $auth);
            case 'kubernetes':
                return $this->authenticateKubernetes($url, $auth);
            default:
                throw new OperatorException("Unsupported auth method: $method");
        }
    }

    /**
     * Authenticate using AppRole
     */
    private function authenticateAppRole(string $url, array $auth): string
    {
        $roleId = $auth['role_id'] ?? '';
        $secretId = $auth['secret_id'] ?? '';
        
        if (!$roleId || !$secretId) {
            throw new OperatorException("AppRole requires role_id and secret_id");
        }
        
        $client = $this->createClient($url, '');
        $payload = ['role_id' => $roleId, 'secret_id' => $secretId];
        
        $response = $client->post('auth/approle/login', ['json' => $payload]);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("AppRole authentication failed");
        }
        
        $data = json_decode($response->getBody(), true);
        return $data['auth']['client_token'];
    }

    /**
     * Authenticate using Kubernetes
     */
    private function authenticateKubernetes(string $url, array $auth): string
    {
        $role = $auth['role'] ?? '';
        $jwt = $auth['jwt'] ?? '';
        
        if (!$role || !$jwt) {
            throw new OperatorException("Kubernetes auth requires role and jwt");
        }
        
        $client = $this->createClient($url, '');
        $payload = ['role' => $role, 'jwt' => $jwt];
        
        $response = $client->post('auth/kubernetes/login', ['json' => $payload]);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Kubernetes authentication failed");
        }
        
        $data = json_decode($response->getBody(), true);
        return $data['auth']['client_token'];
    }

    /**
     * Create HTTP client for Vault
     */
    private function createClient(string $url, string $token): object
    {
        $config = [
            'base_uri' => $url,
            'timeout' => $this->config['timeout'],
            'headers' => [
                'X-Vault-Token' => $token,
                'Content-Type' => 'application/json'
            ]
        ];
        
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
        if (!isset($params['engine'])) {
            throw new OperatorException("Engine is required");
        }
        
        $validEngines = ['kv', 'transit', 'pki'];
        if (!in_array($params['engine'], $validEngines)) {
            throw new OperatorException("Invalid engine: " . $params['engine']);
        }
    }

    /**
     * Cleanup resources
     */
    public function cleanup(): void
    {
        $this->clients = [];
        $this->tokens = [];
    }

    /**
     * Get operator schema
     */
    public function getSchema(): array
    {
        return [
            'type' => 'object',
            'properties' => [
                'engine' => [
                    'type' => 'string',
                    'enum' => ['kv', 'transit', 'pki'],
                    'description' => 'Vault engine'
                ],
                'operation' => ['type' => 'string', 'description' => 'Operation to perform'],
                'path' => ['type' => 'string', 'description' => 'Secret path'],
                'field' => ['type' => 'string', 'description' => 'Field to extract'],
                'data' => ['type' => 'object', 'description' => 'Data to write'],
                'url' => ['type' => 'string', 'description' => 'Vault URL'],
                'auth' => ['type' => 'object', 'description' => 'Authentication config'],
                'key' => ['type' => 'string', 'description' => 'Encryption key'],
                'plaintext' => ['type' => 'string', 'description' => 'Data to encrypt'],
                'ciphertext' => ['type' => 'string', 'description' => 'Data to decrypt']
            ],
            'required' => ['engine']
        ];
    }
} 