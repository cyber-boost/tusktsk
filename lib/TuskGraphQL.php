<?php
/**
 * 🐘 TuskGraphQL - GraphQL Client for TuskLang
 * ============================================
 * "Making GraphQL queries as simple as elephant memory"
 * 
 * A powerful GraphQL client that integrates seamlessly with TuskLang
 * providing authentication, caching, error handling, and optimization.
 * 
 * Strong. Secure. Scalable. 🐘
 */

namespace TuskPHP\Utils;

class TuskGraphQL
{
    private static $config = [
        'default_endpoint' => null,
        'timeout' => 30,
        'cache_ttl' => 300, // 5 minutes
        'max_retries' => 3,
        'retry_delay' => 1,
        'enable_cache' => true,
        'enable_logging' => true
    ];
    
    private static $cache = [];
    private static $endpoints = [];
    private static $auth_tokens = [];
    
    /**
     * Execute a GraphQL query
     */
    public static function query(string $query, array $variables = [], array $options = []): array
    {
        $endpoint = $options['endpoint'] ?? self::$config['default_endpoint'];
        if (!$endpoint) {
            throw new \Exception('GraphQL endpoint not configured');
        }
        
        $cacheKey = self::generateCacheKey($query, $variables, $endpoint);
        
        // Check cache first
        if (self::$config['enable_cache'] && isset(self::$cache[$cacheKey])) {
            $cached = self::$cache[$cacheKey];
            if (time() - $cached['timestamp'] < self::$config['cache_ttl']) {
                return $cached['data'];
            }
        }
        
        // Execute query with retry logic
        $result = self::executeQuery($endpoint, $query, $variables, $options);
        
        // Cache the result
        if (self::$config['enable_cache']) {
            self::$cache[$cacheKey] = [
                'data' => $result,
                'timestamp' => time()
            ];
        }
        
        return $result;
    }
    
    /**
     * Execute GraphQL mutation
     */
    public static function mutation(string $mutation, array $variables = [], array $options = []): array
    {
        $options['operation_type'] = 'mutation';
        return self::query($mutation, $variables, $options);
    }
    
    /**
     * Execute GraphQL subscription (WebSocket)
     */
    public static function subscription(string $subscription, array $variables = [], array $options = []): array
    {
        $options['operation_type'] = 'subscription';
        return self::query($subscription, $variables, $options);
    }
    
    /**
     * Set GraphQL endpoint
     */
    public static function setEndpoint(string $name, string $url, array $headers = []): void
    {
        self::$endpoints[$name] = [
            'url' => $url,
            'headers' => $headers
        ];
        
        if ($name === 'default') {
            self::$config['default_endpoint'] = $url;
        }
    }
    
    /**
     * Set authentication token
     */
    public static function setAuthToken(string $endpoint, string $token, string $type = 'Bearer'): void
    {
        self::$auth_tokens[$endpoint] = [
            'token' => $token,
            'type' => $type
        ];
    }
    
    /**
     * Configure GraphQL client
     */
    public static function configure(array $options): void
    {
        self::$config = array_merge(self::$config, $options);
    }
    
    /**
     * Clear cache
     */
    public static function clearCache(string $pattern = '*'): void
    {
        if ($pattern === '*') {
            self::$cache = [];
        } else {
            foreach (array_keys(self::$cache) as $key) {
                if (fnmatch($pattern, $key)) {
                    unset(self::$cache[$key]);
                }
            }
        }
    }
    
    /**
     * Get cache statistics
     */
    public static function getCacheStats(): array
    {
        return [
            'total_entries' => count(self::$cache),
            'memory_usage' => memory_get_usage(true),
            'cache_hits' => 0, // TODO: Implement hit tracking
            'cache_misses' => 0 // TODO: Implement miss tracking
        ];
    }
    
    /**
     * Execute GraphQL query with retry logic
     */
    private static function executeQuery(string $endpoint, string $query, array $variables, array $options): array
    {
        $attempts = 0;
        $lastError = null;
        
        while ($attempts < self::$config['max_retries']) {
            try {
                $result = self::makeHttpRequest($endpoint, $query, $variables, $options);
                
                // Check for GraphQL errors
                if (isset($result['errors']) && !empty($result['errors'])) {
                    $errorMessages = array_map(function($error) {
                        return $error['message'] ?? 'Unknown GraphQL error';
                    }, $result['errors']);
                    
                    throw new \Exception('GraphQL errors: ' . implode(', ', $errorMessages));
                }
                
                return $result['data'] ?? [];
                
            } catch (\Exception $e) {
                $lastError = $e;
                $attempts++;
                
                if ($attempts < self::$config['max_retries']) {
                    sleep(self::$config['retry_delay'] * $attempts);
                }
            }
        }
        
        throw new \Exception('GraphQL query failed after ' . self::$config['max_retries'] . ' attempts: ' . $lastError->getMessage());
    }
    
    /**
     * Make HTTP request to GraphQL endpoint
     */
    private static function makeHttpRequest(string $endpoint, string $query, array $variables, array $options): array
    {
        $headers = [
            'Content-Type: application/json',
            'Accept: application/json'
        ];
        
        // Add authentication
        if (isset(self::$auth_tokens[$endpoint])) {
            $auth = self::$auth_tokens[$endpoint];
            $headers[] = 'Authorization: ' . $auth['type'] . ' ' . $auth['token'];
        }
        
        // Add custom headers
        if (isset($options['headers'])) {
            foreach ($options['headers'] as $key => $value) {
                $headers[] = "$key: $value";
            }
        }
        
        // Prepare request data
        $data = [
            'query' => $query,
            'variables' => $variables
        ];
        
        if (isset($options['operationName'])) {
            $data['operationName'] = $options['operationName'];
        }
        
        // Make HTTP request
        $ch = curl_init();
        curl_setopt_array($ch, [
            CURLOPT_URL => $endpoint,
            CURLOPT_POST => true,
            CURLOPT_POSTFIELDS => json_encode($data),
            CURLOPT_HTTPHEADER => $headers,
            CURLOPT_RETURNTRANSFER => true,
            CURLOPT_TIMEOUT => self::$config['timeout'],
            CURLOPT_FOLLOWLOCATION => true,
            CURLOPT_SSL_VERIFYPEER => true,
            CURLOPT_USERAGENT => 'TuskGraphQL/1.0'
        ]);
        
        $response = curl_exec($ch);
        $httpCode = curl_getinfo($ch, CURLINFO_HTTP_CODE);
        $error = curl_error($ch);
        curl_close($ch);
        
        if ($error) {
            throw new \Exception('cURL error: ' . $error);
        }
        
        if ($httpCode >= 400) {
            throw new \Exception('HTTP error ' . $httpCode . ': ' . $response);
        }
        
        $result = json_decode($response, true);
        if (json_last_error() !== JSON_ERROR_NONE) {
            throw new \Exception('Invalid JSON response: ' . json_last_error_msg());
        }
        
        return $result;
    }
    
    /**
     * Generate cache key for query
     */
    private static function generateCacheKey(string $query, array $variables, string $endpoint): string
    {
        $data = [
            'query' => $query,
            'variables' => $variables,
            'endpoint' => $endpoint
        ];
        
        return 'graphql_' . md5(serialize($data));
    }
    
    /**
     * Validate GraphQL query syntax
     */
    public static function validateQuery(string $query): bool
    {
        // Basic GraphQL query validation
        $query = trim($query);
        
        // Must start with query, mutation, or subscription
        if (!preg_match('/^(query|mutation|subscription)\s+\w+\s*\{/', $query) &&
            !preg_match('/^\s*\{/', $query)) {
            return false;
        }
        
        // Must have balanced braces
        $braceCount = 0;
        for ($i = 0; $i < strlen($query); $i++) {
            if ($query[$i] === '{') {
                $braceCount++;
            } elseif ($query[$i] === '}') {
                $braceCount--;
                if ($braceCount < 0) {
                    return false;
                }
            }
        }
        
        return $braceCount === 0;
    }
    
    /**
     * Parse GraphQL query to extract field names
     */
    public static function parseQueryFields(string $query): array
    {
        $fields = [];
        
        // Extract field names from query
        preg_match_all('/\b(\w+)\s*\{/', $query, $matches);
        if (isset($matches[1])) {
            $fields = array_unique($matches[1]);
        }
        
        return $fields;
    }
    
    /**
     * Get query complexity (simple estimation)
     */
    public static function getQueryComplexity(string $query): int
    {
        $complexity = 1;
        
        // Count nested levels
        $nesting = 0;
        for ($i = 0; $i < strlen($query); $i++) {
            if ($query[$i] === '{') {
                $nesting++;
                $complexity += $nesting;
            } elseif ($query[$i] === '}') {
                $nesting--;
            }
        }
        
        return $complexity;
    }
    
    /**
     * Log GraphQL operation
     */
    private static function log(string $message, array $context = []): void
    {
        if (!self::$config['enable_logging']) {
            return;
        }
        
        $logEntry = [
            'timestamp' => date('Y-m-d H:i:s'),
            'message' => $message,
            'context' => $context
        ];
        
        // Log to file or system log
        error_log('TuskGraphQL: ' . json_encode($logEntry));
    }
}

/**
 * GraphQL Query Builder for TuskLang
 */
class GraphQLQueryBuilder
{
    private $query = '';
    private $variables = [];
    private $operationName = '';
    private $operationType = 'query';
    
    public function query(string $name): self
    {
        $this->operationType = 'query';
        $this->operationName = $name;
        return $this;
    }
    
    public function mutation(string $name): self
    {
        $this->operationType = 'mutation';
        $this->operationName = $name;
        return $this;
    }
    
    public function subscription(string $name): self
    {
        $this->operationType = 'subscription';
        $this->operationName = $name;
        return $this;
    }
    
    public function fields(array $fields): self
    {
        $this->query = $this->buildQuery($fields);
        return $this;
    }
    
    public function variable(string $name, string $type, $defaultValue = null): self
    {
        $this->variables[$name] = [
            'type' => $type,
            'default' => $defaultValue
        ];
        return $this;
    }
    
    public function build(): array
    {
        $query = $this->operationType;
        if ($this->operationName) {
            $query .= ' ' . $this->operationName;
        }
        
        if (!empty($this->variables)) {
            $vars = [];
            foreach ($this->variables as $name => $config) {
                $var = '$' . $name . ': ' . $config['type'];
                if ($config['default'] !== null) {
                    $var .= ' = ' . $this->formatValue($config['default']);
                }
                $vars[] = $var;
            }
            $query .= '(' . implode(', ', $vars) . ')';
        }
        
        $query .= ' ' . $this->query;
        
        return [
            'query' => $query,
            'variables' => $this->variables
        ];
    }
    
    private function buildQuery(array $fields, int $depth = 0): string
    {
        $indent = str_repeat('  ', $depth);
        $query = "{\n";
        
        foreach ($fields as $field => $value) {
            if (is_array($value)) {
                $query .= $indent . '  ' . $field . ' ' . $this->buildQuery($value, $depth + 1);
            } else {
                $query .= $indent . '  ' . $field . "\n";
            }
        }
        
        $query .= $indent . "}";
        return $query;
    }
    
    private function formatValue($value): string
    {
        if (is_string($value)) {
            return '"' . addslashes($value) . '"';
        } elseif (is_bool($value)) {
            return $value ? 'true' : 'false';
        } elseif (is_null($value)) {
            return 'null';
        } else {
            return (string) $value;
        }
    }
} 