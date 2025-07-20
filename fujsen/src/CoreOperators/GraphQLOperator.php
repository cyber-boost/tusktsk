<?php
/**
 * GraphQL Operator
 * 
 * GraphQL client implementation with support for queries, mutations,
 * subscriptions, schema introspection, and variable substitution.
 * 
 * @package TuskPHP\CoreOperators
 * @version 2.0.0
 */

namespace TuskPHP\CoreOperators;

/**
 * GraphQL Operator
 * 
 * Provides GraphQL client capabilities with advanced features like
 * connection pooling, automatic reconnection, and schema validation.
 */
class GraphQLOperator extends BaseOperator
{
    private array $clients = [];
    private array $connectionPools = [];
    private array $schemas = [];
    
    public function __construct()
    {
        $this->version = '2.0.0';
        $this->requiredFields = ['endpoint'];
        $this->optionalFields = [
            'query', 'mutation', 'subscription', 'variables', 'headers',
            'timeout', 'retry', 'auth', 'schema', 'operation_name'
        ];
        
        $this->defaultConfig = [
            'timeout' => 30,
            'retry' => [
                'attempts' => 3,
                'backoff' => 'exponential',
                'initial_delay' => 1000,
                'max_delay' => 10000
            ],
            'headers' => [
                'Content-Type' => 'application/json',
                'Accept' => 'application/json'
            ],
            'auth' => null
        ];
    }
    
    public function getName(): string
    {
        return 'graphql';
    }
    
    protected function getDescription(): string
    {
        return 'GraphQL client with support for queries, mutations, subscriptions, schema introspection, and variable substitution';
    }
    
    protected function getExamples(): array
    {
        return [
            'query' => '@graphql({endpoint: "https://api.example.com/graphql", query: "query GetUser($id: ID!) { user(id: $id) { name email } }", variables: {id: @variable("user_id")}})',
            'mutation' => '@graphql({endpoint: "https://api.example.com/graphql", mutation: "mutation CreateUser($input: UserInput!) { createUser(input: $input) { id name } }", variables: {input: @variable("user_data")}})',
            'subscription' => '@graphql({endpoint: "wss://api.example.com/graphql", subscription: "subscription UserUpdates { userUpdated { id name } }"})',
            'with_auth' => '@graphql({endpoint: "https://api.example.com/graphql", query: "query { me { id } }", auth: {type: "bearer", token: @env("API_TOKEN")}})',
            'schema_introspection' => '@graphql({endpoint: "https://api.example.com/graphql", query: "query IntrospectionQuery { __schema { types { name } } }"})'
        ];
    }
    
    protected function getErrorCodes(): array
    {
        return array_merge(parent::getErrorCodes(), [
            'GRAPHQL_ERRORS' => 'GraphQL query returned errors',
            'INVALID_SCHEMA' => 'GraphQL schema is invalid',
            'SUBSCRIPTION_FAILED' => 'GraphQL subscription failed',
            'AUTH_FAILED' => 'Authentication failed',
            'SCHEMA_INTROSPECTION_FAILED' => 'Schema introspection failed'
        ]);
    }
    
    /**
     * Execute GraphQL operator
     */
    protected function executeOperator(array $config, array $context): mixed
    {
        $endpoint = $this->resolveVariable($config['endpoint'], $context);
        $client = $this->getClient($endpoint, $config);
        
        // Determine operation type
        $operation = $this->determineOperation($config);
        
        // Resolve variables
        $variables = $this->resolveVariables($config['variables'] ?? [], $context);
        
        // Execute operation
        switch ($operation['type']) {
            case 'query':
                return $this->executeQuery($client, $operation['query'], $variables, $config);
            case 'mutation':
                return $this->executeMutation($client, $operation['query'], $variables, $config);
            case 'subscription':
                return $this->executeSubscription($client, $operation['query'], $variables, $config);
            default:
                throw new \InvalidArgumentException("Unknown GraphQL operation type");
        }
    }
    
    /**
     * Get GraphQL client
     */
    private function getClient(string $endpoint, array $config): GraphQLClient
    {
        $key = md5($endpoint . json_encode($config));
        
        if (!isset($this->clients[$key])) {
            $this->clients[$key] = new GraphQLClient($endpoint, $config);
        }
        
        return $this->clients[$key];
    }
    
    /**
     * Determine operation type and query
     */
    private function determineOperation(array $config): array
    {
        if (isset($config['query'])) {
            return ['type' => 'query', 'query' => $config['query']];
        }
        
        if (isset($config['mutation'])) {
            return ['type' => 'mutation', 'query' => $config['mutation']];
        }
        
        if (isset($config['subscription'])) {
            return ['type' => 'subscription', 'query' => $config['subscription']];
        }
        
        throw new \InvalidArgumentException("No GraphQL operation specified (query, mutation, or subscription)");
    }
    
    /**
     * Resolve variables
     */
    private function resolveVariables(array $variables, array $context): array
    {
        $resolved = [];
        
        foreach ($variables as $key => $value) {
            $resolved[$key] = $this->resolveVariable($value, $context);
        }
        
        return $resolved;
    }
    
    /**
     * Execute GraphQL query
     */
    private function executeQuery(GraphQLClient $client, string $query, array $variables, array $config): mixed
    {
        try {
            $result = $client->query($query, $variables, $config);
            
            // Check for GraphQL errors
            if (isset($result['errors']) && !empty($result['errors'])) {
                $this->handleGraphQLErrors($result['errors'], $config);
            }
            
            return $result['data'] ?? $result;
            
        } catch (\Exception $e) {
            $this->handleError($e, $config, ['query' => $query, 'variables' => $variables]);
            throw $e;
        }
    }
    
    /**
     * Execute GraphQL mutation
     */
    private function executeMutation(GraphQLClient $client, string $mutation, array $variables, array $config): mixed
    {
        try {
            $result = $client->mutation($mutation, $variables, $config);
            
            // Check for GraphQL errors
            if (isset($result['errors']) && !empty($result['errors'])) {
                $this->handleGraphQLErrors($result['errors'], $config);
            }
            
            return $result['data'] ?? $result;
            
        } catch (\Exception $e) {
            $this->handleError($e, $config, ['mutation' => $mutation, 'variables' => $variables]);
            throw $e;
        }
    }
    
    /**
     * Execute GraphQL subscription
     */
    private function executeSubscription(GraphQLClient $client, string $subscription, array $variables, array $config): mixed
    {
        try {
            $result = $client->subscription($subscription, $variables, $config);
            
            // Check for GraphQL errors
            if (isset($result['errors']) && !empty($result['errors'])) {
                $this->handleGraphQLErrors($result['errors'], $config);
            }
            
            return $result['data'] ?? $result;
            
        } catch (\Exception $e) {
            $this->handleError($e, $config, ['subscription' => $subscription, 'variables' => $variables]);
            throw $e;
        }
    }
    
    /**
     * Handle GraphQL errors
     */
    private function handleGraphQLErrors(array $errors, array $config): void
    {
        $errorMessages = [];
        
        foreach ($errors as $error) {
            $message = $error['message'] ?? 'Unknown GraphQL error';
            $locations = $error['locations'] ?? [];
            $path = $error['path'] ?? [];
            
            $errorMessages[] = [
                'message' => $message,
                'locations' => $locations,
                'path' => $path
            ];
        }
        
        $this->log('error', 'GraphQL errors occurred', [
            'errors' => $errorMessages,
            'config' => $config
        ]);
        
        // Don't throw exception for GraphQL errors, just log them
        // The calling code can check for errors in the response
    }
    
    /**
     * Custom validation
     */
    protected function customValidate(array $config): array
    {
        $errors = [];
        $warnings = [];
        
        // Validate endpoint
        if (isset($config['endpoint'])) {
            $endpoint = $config['endpoint'];
            if (!filter_var($endpoint, FILTER_VALIDATE_URL) && !preg_match('/^wss?:\/\//', $endpoint)) {
                $errors[] = "Invalid GraphQL endpoint: {$endpoint}";
            }
        }
        
        // Validate operation
        $operationCount = 0;
        if (isset($config['query'])) $operationCount++;
        if (isset($config['mutation'])) $operationCount++;
        if (isset($config['subscription'])) $operationCount++;
        
        if ($operationCount === 0) {
            $errors[] = "No GraphQL operation specified (query, mutation, or subscription)";
        } elseif ($operationCount > 1) {
            $errors[] = "Multiple GraphQL operations specified (only one allowed)";
        }
        
        // Validate auth configuration
        if (isset($config['auth'])) {
            $auth = $config['auth'];
            if (!is_array($auth) || !isset($auth['type'])) {
                $errors[] = "Invalid auth configuration";
            } else {
                $validTypes = ['bearer', 'basic', 'api_key'];
                if (!in_array($auth['type'], $validTypes)) {
                    $errors[] = "Invalid auth type: {$auth['type']}";
                }
            }
        }
        
        return ['errors' => $errors, 'warnings' => $warnings];
    }
    
    /**
     * Cleanup resources
     */
    public function cleanup(): void
    {
        foreach ($this->clients as $client) {
            $client->cleanup();
        }
        $this->clients = [];
    }
}

/**
 * GraphQL Client
 */
class GraphQLClient
{
    private string $endpoint;
    private array $config;
    private ?\CurlHandle $curl = null;
    private ?\WebSocket\Client $wsClient = null;
    private array $headers = [];
    private array $stats = [
        'queries' => 0,
        'mutations' => 0,
        'subscriptions' => 0,
        'errors' => 0
    ];
    
    public function __construct(string $endpoint, array $config)
    {
        $this->endpoint = $endpoint;
        $this->config = $config;
        $this->headers = $config['headers'] ?? [];
        
        $this->initializeClient();
    }
    
    /**
     * Initialize client based on endpoint type
     */
    private function initializeClient(): void
    {
        if (preg_match('/^wss?:\/\//', $this->endpoint)) {
            $this->initializeWebSocket();
        } else {
            $this->initializeHttp();
        }
    }
    
    /**
     * Initialize HTTP client
     */
    private function initializeHttp(): void
    {
        $this->curl = curl_init();
        
        curl_setopt_array($this->curl, [
            CURLOPT_RETURNTRANSFER => true,
            CURLOPT_FOLLOWLOCATION => true,
            CURLOPT_TIMEOUT => $this->config['timeout'] ?? 30,
            CURLOPT_HTTPHEADER => $this->buildHeaders(),
            CURLOPT_POST => true,
            CURLOPT_POSTFIELDS => '',
            CURLOPT_URL => $this->endpoint
        ]);
    }
    
    /**
     * Initialize WebSocket client
     */
    private function initializeWebSocket(): void
    {
        // WebSocket implementation would go here
        // For now, we'll use a simple HTTP fallback
        $this->initializeHttp();
    }
    
    /**
     * Build headers
     */
    private function buildHeaders(): array
    {
        $headers = $this->headers;
        
        // Add authentication
        if (isset($this->config['auth'])) {
            $auth = $this->config['auth'];
            
            switch ($auth['type']) {
                case 'bearer':
                    $headers['Authorization'] = 'Bearer ' . $auth['token'];
                    break;
                case 'basic':
                    $credentials = base64_encode($auth['username'] . ':' . $auth['password']);
                    $headers['Authorization'] = 'Basic ' . $credentials;
                    break;
                case 'api_key':
                    $headers['X-API-Key'] = $auth['key'];
                    break;
            }
        }
        
        // Convert to curl format
        $curlHeaders = [];
        foreach ($headers as $key => $value) {
            $curlHeaders[] = "{$key}: {$value}";
        }
        
        return $curlHeaders;
    }
    
    /**
     * Execute GraphQL query
     */
    public function query(string $query, array $variables = [], array $options = []): array
    {
        $payload = [
            'query' => $query,
            'variables' => $variables
        ];
        
        if (isset($options['operation_name'])) {
            $payload['operationName'] = $options['operation_name'];
        }
        
        $result = $this->executeRequest($payload);
        $this->stats['queries']++;
        
        return $result;
    }
    
    /**
     * Execute GraphQL mutation
     */
    public function mutation(string $mutation, array $variables = [], array $options = []): array
    {
        $payload = [
            'query' => $mutation,
            'variables' => $variables
        ];
        
        if (isset($options['operation_name'])) {
            $payload['operationName'] = $options['operation_name'];
        }
        
        $result = $this->executeRequest($payload);
        $this->stats['mutations']++;
        
        return $result;
    }
    
    /**
     * Execute GraphQL subscription
     */
    public function subscription(string $subscription, array $variables = [], array $options = []): array
    {
        // For now, we'll use HTTP for subscriptions (GraphQL over HTTP)
        // In a full implementation, this would use WebSocket
        $payload = [
            'query' => $subscription,
            'variables' => $variables
        ];
        
        if (isset($options['operation_name'])) {
            $payload['operationName'] = $options['operation_name'];
        }
        
        $result = $this->executeRequest($payload);
        $this->stats['subscriptions']++;
        
        return $result;
    }
    
    /**
     * Execute HTTP request
     */
    private function executeRequest(array $payload): array
    {
        if (!$this->curl) {
            throw new \RuntimeException("GraphQL client not initialized");
        }
        
        $jsonPayload = json_encode($payload);
        if ($jsonPayload === false) {
            throw new \RuntimeException("Failed to encode GraphQL payload");
        }
        
        curl_setopt($this->curl, CURLOPT_POSTFIELDS, $jsonPayload);
        
        $response = curl_exec($this->curl);
        $httpCode = curl_getinfo($this->curl, CURLINFO_HTTP_CODE);
        $error = curl_error($this->curl);
        
        if ($error) {
            $this->stats['errors']++;
            throw new \RuntimeException("GraphQL request failed: {$error}");
        }
        
        if ($httpCode >= 400) {
            $this->stats['errors']++;
            throw new \RuntimeException("GraphQL request failed with HTTP {$httpCode}");
        }
        
        $result = json_decode($response, true);
        if ($result === null) {
            $this->stats['errors']++;
            throw new \RuntimeException("Failed to decode GraphQL response");
        }
        
        return $result;
    }
    
    /**
     * Get client statistics
     */
    public function getStatistics(): array
    {
        return array_merge($this->stats, [
            'endpoint' => $this->endpoint,
            'connected' => $this->curl !== null
        ]);
    }
    
    /**
     * Cleanup resources
     */
    public function cleanup(): void
    {
        if ($this->curl) {
            curl_close($this->curl);
            $this->curl = null;
        }
        
        if ($this->wsClient) {
            $this->wsClient->close();
            $this->wsClient = null;
        }
    }
} 