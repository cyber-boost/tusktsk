<?php

namespace TuskLang\Communication\Gateway;

use TuskLang\Communication\Http\ApiRequest;
use TuskLang\Communication\Http\ApiResponse;
use TuskLang\Communication\Http\HttpSecurity;

/**
 * Advanced API Gateway
 * 
 * Features:
 * - Service routing and discovery
 * - Load balancing (round-robin, weighted, least connections)
 * - Rate limiting and throttling
 * - Authentication and authorization
 * - Request/response transformation
 * - Circuit breaker pattern
 * - Health checking and failover
 * - Caching and performance optimization
 * - Metrics and analytics
 */
class ApiGateway
{
    private array $config;
    private ServiceDiscovery $serviceDiscovery;
    private LoadBalancer $loadBalancer;
    private RateLimiter $rateLimiter;
    private CircuitBreaker $circuitBreaker;
    private RequestTransformer $requestTransformer;
    private ResponseTransformer $responseTransformer;
    private CacheManager $cacheManager;
    private array $middlewares = [];

    public function __construct(array $config = [])
    {
        $this->config = array_merge([
            'enable_service_discovery' => true,
            'enable_load_balancing' => true,
            'enable_rate_limiting' => true,
            'enable_circuit_breaker' => true,
            'enable_caching' => true,
            'enable_metrics' => true,
            'default_timeout' => 30,
            'max_retries' => 3,
            'retry_delay' => 1000,
            'health_check_interval' => 30,
            'circuit_breaker_threshold' => 5,
            'circuit_breaker_timeout' => 60,
            'cache_ttl' => 300,
            'rate_limit_window' => 3600,
            'rate_limit_max_requests' => 1000
        ], $config);

        $this->initializeComponents();
    }

    /**
     * Handle incoming request
     */
    public function handle(ApiRequest $request): ApiResponse
    {
        try {
            // Apply middleware pipeline
            $request = $this->applyMiddlewares($request);
            
            // Route resolution
            $route = $this->resolveRoute($request);
            if (!$route) {
                return new ApiResponse(404, [], ['error' => 'Route not found']);
            }

            // Rate limiting
            if ($this->config['enable_rate_limiting'] && !$this->checkRateLimit($request, $route)) {
                return new ApiResponse(429, [], ['error' => 'Rate limit exceeded']);
            }

            // Authentication & Authorization
            $authResult = $this->authenticateRequest($request, $route);
            if (!$authResult['success']) {
                return new ApiResponse(401, [], ['error' => $authResult['message']]);
            }

            // Service discovery
            $serviceUrl = $this->discoverService($route['service'], $request);
            if (!$serviceUrl) {
                return new ApiResponse(503, [], ['error' => 'Service unavailable']);
            }

            // Circuit breaker check
            if ($this->config['enable_circuit_breaker'] && !$this->circuitBreaker->canExecute($route['service'])) {
                return new ApiResponse(503, [], ['error' => 'Circuit breaker open']);
            }

            // Check cache
            $cacheKey = $this->generateCacheKey($request, $route);
            if ($this->config['enable_caching'] && $cachedResponse = $this->cacheManager->get($cacheKey)) {
                return $this->deserializeResponse($cachedResponse);
            }

            // Transform request
            $transformedRequest = $this->requestTransformer->transform($request, $route);

            // Execute request with retries
            $response = $this->executeWithRetries($serviceUrl, $transformedRequest, $route);

            // Transform response
            $transformedResponse = $this->responseTransformer->transform($response, $route);

            // Cache response
            if ($this->config['enable_caching'] && $this->shouldCache($transformedResponse, $route)) {
                $this->cacheManager->set($cacheKey, $this->serializeResponse($transformedResponse), $this->config['cache_ttl']);
            }

            // Record success for circuit breaker
            if ($this->config['enable_circuit_breaker']) {
                $this->circuitBreaker->recordSuccess($route['service']);
            }

            // Update metrics
            $this->updateMetrics($request, $transformedResponse, $route, microtime(true) - $_SERVER['REQUEST_TIME_FLOAT']);

            return $transformedResponse;

        } catch (\Exception $e) {
            return $this->handleError($e, $request, $route ?? null);
        }
    }

    /**
     * Register route
     */
    public function addRoute(string $path, string $service, array $options = []): self
    {
        $route = array_merge([
            'path' => $path,
            'service' => $service,
            'methods' => ['GET', 'POST', 'PUT', 'DELETE'],
            'auth_required' => true,
            'rate_limit' => $this->config['rate_limit_max_requests'],
            'timeout' => $this->config['default_timeout'],
            'retries' => $this->config['max_retries'],
            'cache_ttl' => $this->config['cache_ttl'],
            'circuit_breaker' => true,
            'transform_request' => null,
            'transform_response' => null
        ], $options);

        $this->serviceDiscovery->registerRoute($path, $route);
        return $this;
    }

    /**
     * Add middleware
     */
    public function addMiddleware(MiddlewareInterface $middleware, int $priority = 0): self
    {
        $this->middlewares[] = ['middleware' => $middleware, 'priority' => $priority];
        
        // Sort by priority
        usort($this->middlewares, function($a, $b) {
            return $b['priority'] <=> $a['priority'];
        });
        
        return $this;
    }

    /**
     * Register upstream service
     */
    public function registerService(string $name, array $endpoints, array $options = []): self
    {
        $this->serviceDiscovery->registerService($name, $endpoints, $options);
        return $this;
    }

    /**
     * Apply middleware pipeline
     */
    private function applyMiddlewares(ApiRequest $request): ApiRequest
    {
        $processedRequest = $request;
        
        foreach ($this->middlewares as $middlewareData) {
            $middleware = $middlewareData['middleware'];
            $processedRequest = $middleware->process($processedRequest);
        }
        
        return $processedRequest;
    }

    /**
     * Resolve route from request
     */
    private function resolveRoute(ApiRequest $request): ?array
    {
        return $this->serviceDiscovery->resolveRoute($request->getPath(), $request->getMethod());
    }

    /**
     * Check rate limiting
     */
    private function checkRateLimit(ApiRequest $request, array $route): bool
    {
        $identifier = $this->getRateLimitIdentifier($request);
        $limit = $route['rate_limit'] ?? $this->config['rate_limit_max_requests'];
        
        return $this->rateLimiter->allow($identifier, $limit, $this->config['rate_limit_window']);
    }

    /**
     * Authenticate request
     */
    private function authenticateRequest(ApiRequest $request, array $route): array
    {
        if (!($route['auth_required'] ?? true)) {
            return ['success' => true];
        }

        $authHeader = $request->getHeader('Authorization');
        if (!$authHeader) {
            return ['success' => false, 'message' => 'Authorization header required'];
        }

        // JWT validation (integrate with your auth system)
        if (str_starts_with($authHeader, 'Bearer ')) {
            $token = substr($authHeader, 7);
            $user = $this->validateJWT($token);
            
            if ($user) {
                $request->setAttribute('user', $user);
                return ['success' => true, 'user' => $user];
            }
        }

        return ['success' => false, 'message' => 'Invalid authentication'];
    }

    /**
     * Discover service endpoint
     */
    private function discoverService(string $serviceName, ApiRequest $request): ?string
    {
        if (!$this->config['enable_service_discovery']) {
            return $serviceName; // Assume direct URL
        }

        $endpoints = $this->serviceDiscovery->getServiceEndpoints($serviceName);
        if (empty($endpoints)) {
            return null;
        }

        // Load balancing
        if ($this->config['enable_load_balancing']) {
            return $this->loadBalancer->selectEndpoint($serviceName, $endpoints, $request);
        }

        return $endpoints[0]['url'];
    }

    /**
     * Execute request with retry logic
     */
    private function executeWithRetries(string $serviceUrl, ApiRequest $request, array $route): ApiResponse
    {
        $maxRetries = $route['retries'] ?? $this->config['max_retries'];
        $retryDelay = $this->config['retry_delay'];
        $lastException = null;

        for ($attempt = 0; $attempt <= $maxRetries; $attempt++) {
            try {
                if ($attempt > 0) {
                    usleep($retryDelay * 1000); // Convert to microseconds
                    $retryDelay *= 2; // Exponential backoff
                }

                return $this->executeRequest($serviceUrl, $request, $route);

            } catch (\Exception $e) {
                $lastException = $e;
                
                // Record failure for circuit breaker
                if ($this->config['enable_circuit_breaker']) {
                    $this->circuitBreaker->recordFailure($route['service']);
                }

                // Don't retry on 4xx errors
                if ($e instanceof HttpException && $e->getCode() >= 400 && $e->getCode() < 500) {
                    break;
                }
            }
        }

        throw $lastException;
    }

    /**
     * Execute single request
     */
    private function executeRequest(string $serviceUrl, ApiRequest $request, array $route): ApiResponse
    {
        $timeout = $route['timeout'] ?? $this->config['default_timeout'];
        
        $ch = curl_init();
        curl_setopt_array($ch, [
            CURLOPT_URL => $serviceUrl . $request->getPath(),
            CURLOPT_CUSTOMREQUEST => $request->getMethod(),
            CURLOPT_RETURNTRANSFER => true,
            CURLOPT_TIMEOUT => $timeout,
            CURLOPT_HTTPHEADER => $this->buildHeaders($request),
            CURLOPT_POSTFIELDS => $request->getBody(),
            CURLOPT_FOLLOWLOCATION => true,
            CURLOPT_SSL_VERIFYPEER => false
        ]);

        $response = curl_exec($ch);
        $httpCode = curl_getinfo($ch, CURLINFO_HTTP_CODE);
        $error = curl_error($ch);
        curl_close($ch);

        if ($error) {
            throw new \RuntimeException("HTTP request failed: {$error}");
        }

        return new ApiResponse($httpCode, [], json_decode($response, true));
    }

    /**
     * Build headers for upstream request
     */
    private function buildHeaders(ApiRequest $request): array
    {
        $headers = [];
        
        foreach ($request->getHeaders() as $name => $value) {
            // Skip hop-by-hop headers
            if (!in_array(strtolower($name), ['connection', 'keep-alive', 'proxy-authenticate', 'proxy-authorization', 'te', 'trailers', 'transfer-encoding', 'upgrade'])) {
                $headers[] = "{$name}: {$value}";
            }
        }

        // Add gateway identification
        $headers[] = 'X-Gateway: TuskLang-Gateway/1.0';
        
        return $headers;
    }

    /**
     * Generate cache key
     */
    private function generateCacheKey(ApiRequest $request, array $route): string
    {
        $data = [
            'service' => $route['service'],
            'path' => $request->getPath(),
            'method' => $request->getMethod(),
            'query' => $request->getQueryParams(),
            'headers' => array_intersect_key($request->getHeaders(), ['Accept', 'Accept-Language'])
        ];
        
        return 'gateway:' . md5(json_encode($data));
    }

    /**
     * Check if response should be cached
     */
    private function shouldCache(ApiResponse $response, array $route): bool
    {
        // Only cache successful GET requests
        return $response->getStatusCode() === 200 && 
               in_array('GET', $route['methods'] ?? []) &&
               ($route['cache_ttl'] ?? 0) > 0;
    }

    /**
     * Get rate limit identifier
     */
    private function getRateLimitIdentifier(ApiRequest $request): string
    {
        // Use IP address or user ID if authenticated
        $user = $request->getAttribute('user');
        return $user ? "user:{$user['id']}" : "ip:{$request->getClientIp()}";
    }

    /**
     * Validate JWT token
     */
    private function validateJWT(string $token): ?array
    {
        // Integrate with your JWT library
        // This is a mock implementation
        if ($token === 'valid_token') {
            return ['id' => 'user123', 'role' => 'user'];
        }
        return null;
    }

    /**
     * Handle errors
     */
    private function handleError(\Exception $e, ApiRequest $request, ?array $route): ApiResponse
    {
        $statusCode = 500;
        $message = 'Internal server error';

        if ($e instanceof HttpException) {
            $statusCode = $e->getCode();
            $message = $e->getMessage();
        }

        // Log error
        error_log("Gateway error: {$e->getMessage()}");

        // Update error metrics
        $this->updateErrorMetrics($request, $e, $route);

        return new ApiResponse($statusCode, [], ['error' => $message]);
    }

    /**
     * Update metrics
     */
    private function updateMetrics(ApiRequest $request, ApiResponse $response, array $route, float $duration): void
    {
        if (!$this->config['enable_metrics']) {
            return;
        }

        // Implement your metrics collection here
        $metrics = [
            'service' => $route['service'],
            'path' => $request->getPath(),
            'method' => $request->getMethod(),
            'status_code' => $response->getStatusCode(),
            'duration' => $duration,
            'timestamp' => time()
        ];

        // Send to metrics collector (Prometheus, InfluxDB, etc.)
        $this->recordMetrics($metrics);
    }

    /**
     * Update error metrics
     */
    private function updateErrorMetrics(ApiRequest $request, \Exception $e, ?array $route): void
    {
        if (!$this->config['enable_metrics']) {
            return;
        }

        $metrics = [
            'service' => $route['service'] ?? 'unknown',
            'path' => $request->getPath(),
            'method' => $request->getMethod(),
            'error_type' => get_class($e),
            'error_message' => $e->getMessage(),
            'timestamp' => time()
        ];

        $this->recordErrorMetrics($metrics);
    }

    /**
     * Record metrics (placeholder)
     */
    private function recordMetrics(array $metrics): void
    {
        // Implement actual metrics recording
    }

    /**
     * Record error metrics (placeholder)
     */
    private function recordErrorMetrics(array $metrics): void
    {
        // Implement actual error metrics recording
    }

    /**
     * Serialize response for caching
     */
    private function serializeResponse(ApiResponse $response): string
    {
        return serialize([
            'status_code' => $response->getStatusCode(),
            'headers' => $response->getHeaders(),
            'body' => $response->getBody()
        ]);
    }

    /**
     * Deserialize cached response
     */
    private function deserializeResponse(string $data): ApiResponse
    {
        $responseData = unserialize($data);
        return new ApiResponse(
            $responseData['status_code'],
            $responseData['headers'],
            $responseData['body']
        );
    }

    /**
     * Initialize components
     */
    private function initializeComponents(): void
    {
        $this->serviceDiscovery = new ServiceDiscovery($this->config);
        $this->loadBalancer = new LoadBalancer($this->config);
        $this->rateLimiter = new RateLimiter($this->config);
        $this->circuitBreaker = new CircuitBreaker($this->config);
        $this->requestTransformer = new RequestTransformer($this->config);
        $this->responseTransformer = new ResponseTransformer($this->config);
        $this->cacheManager = new CacheManager($this->config);
    }

    /**
     * Get gateway statistics
     */
    public function getStats(): array
    {
        return [
            'registered_services' => $this->serviceDiscovery->getServiceCount(),
            'active_routes' => $this->serviceDiscovery->getRouteCount(),
            'middleware_count' => count($this->middlewares),
            'configuration' => $this->config,
            'memory_usage' => memory_get_usage(true),
            'uptime' => time() - $_SERVER['REQUEST_TIME']
        ];
    }
}

/**
 * Middleware interface
 */
interface MiddlewareInterface
{
    public function process(ApiRequest $request): ApiRequest;
}

/**
 * HTTP Exception
 */
class HttpException extends \Exception
{
    public function __construct(int $statusCode, string $message = '')
    {
        parent::__construct($message, $statusCode);
    }
} 