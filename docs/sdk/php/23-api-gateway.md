# API Gateway with TuskLang

TuskLang revolutionizes API gateway development by providing configuration-driven routing, authentication, rate limiting, and intelligent request processing with minimal code complexity.

## Overview

TuskLang's API gateway capabilities combine the power of modern API management with the simplicity of configuration-driven development, enabling robust, scalable, and secure API infrastructure.

```php
// API Gateway Configuration
api_gateway = {
    server = {
        host = "0.0.0.0"
        port = 8080
        workers = 4
        max_connections = 10000
    }
    
    routes = {
        "/api/v1/users" = {
            service = "user-service"
            methods = ["GET", "POST", "PUT", "DELETE"]
            authentication = "jwt"
            rate_limit = {
                requests_per_minute = 100
                burst_size = 20
            }
        }
        
        "/api/v1/orders" = {
            service = "order-service"
            methods = ["GET", "POST"]
            authentication = "oauth2"
            rate_limit = {
                requests_per_minute = 50
                burst_size = 10
            }
            caching = {
                enabled = true
                ttl = 300
                cache_key = "user_id:order_id"
            }
        }
        
        "/api/v1/products" = {
            service = "product-service"
            methods = ["GET"]
            authentication = "api_key"
            rate_limit = {
                requests_per_minute = 200
                burst_size = 50
            }
        }
    }
    
    services = {
        user_service = {
            url = "http://user-service:3001"
            health_check = "/health"
            timeout = 30
            retries = 3
        }
        
        order_service = {
            url = "http://order-service:3002"
            health_check = "/health"
            timeout = 60
            retries = 2
        }
        
        product_service = {
            url = "http://product-service:3003"
            health_check = "/health"
            timeout = 15
            retries = 1
        }
    }
}
```

## Core API Gateway Features

### 1. Request Routing and Load Balancing

```php
// Routing Configuration
routing_config = {
    load_balancing = {
        algorithm = "round_robin"
        health_checks = true
        failover = true
        sticky_sessions = false
    }
    
    path_matching = {
        exact_match = true
        prefix_match = true
        regex_match = true
        case_sensitive = false
    }
    
    service_discovery = {
        type = "consul"
        refresh_interval = "30 seconds"
        health_check_interval = "10 seconds"
    }
    
    circuit_breaker = {
        enabled = true
        failure_threshold = 5
        recovery_timeout = "60 seconds"
        half_open_state = true
    }
}

// Router Implementation
class APIRouter {
    private $config;
    private $loadBalancer;
    private $serviceRegistry;
    
    public function __construct($configPath) {
        $this->config = $this->loadConfig($configPath);
        $this->loadBalancer = new LoadBalancer($this->config->routing_config->load_balancing);
        $this->serviceRegistry = new ServiceRegistry($this->config->routing_config->service_discovery);
    }
    
    public function route($request) {
        $route = $this->findRoute($request->getPath());
        
        if (!$route) {
            throw new RouteNotFoundException("Route not found: {$request->getPath()}");
        }
        
        // Validate method
        if (!in_array($request->getMethod(), $route->methods)) {
            throw new MethodNotAllowedException("Method not allowed: {$request->getMethod()}");
        }
        
        // Get service instance
        $service = $this->config->api_gateway->services->{$route->service};
        $serviceInstance = $this->loadBalancer->getInstance($service);
        
        // Apply middleware
        $request = $this->applyMiddleware($request, $route);
        
        // Forward request
        return $this->forwardRequest($request, $serviceInstance);
    }
    
    private function findRoute($path) {
        foreach ($this->config->api_gateway->routes as $routePath => $routeConfig) {
            if ($this->matchPath($path, $routePath)) {
                return (object) array_merge(['path' => $routePath], (array) $routeConfig);
            }
        }
        return null;
    }
    
    private function matchPath($requestPath, $routePath) {
        // Exact match
        if ($requestPath === $routePath) {
            return true;
        }
        
        // Prefix match
        if (strpos($routePath, '*') !== false) {
            $pattern = str_replace('*', '.*', $routePath);
            return preg_match("#^{$pattern}$#", $requestPath);
        }
        
        // Regex match
        if (strpos($routePath, '/') === 0 && strpos($routePath, '/', 1) !== false) {
            return preg_match($routePath, $requestPath);
        }
        
        return false;
    }
    
    private function applyMiddleware($request, $route) {
        // Authentication
        if (isset($route->authentication)) {
            $request = $this->authenticate($request, $route->authentication);
        }
        
        // Rate limiting
        if (isset($route->rate_limit)) {
            $request = $this->checkRateLimit($request, $route->rate_limit);
        }
        
        // Caching
        if (isset($route->caching) && $route->caching->enabled) {
            $cached = $this->getCachedResponse($request, $route->caching);
            if ($cached) {
                return $cached;
            }
        }
        
        return $request;
    }
}
```

### 2. Authentication and Authorization

```php
// Authentication Configuration
authentication_config = {
    jwt = {
        secret = @env(JWT_SECRET)
        algorithm = "HS256"
        issuer = "api-gateway"
        audience = "api-clients"
        expiration = "1 hour"
        refresh_token = {
            enabled = true
            expiration = "7 days"
        }
    }
    
    oauth2 = {
        provider = "keycloak"
        client_id = @env(OAUTH_CLIENT_ID)
        client_secret = @env(OAUTH_CLIENT_SECRET)
        authorization_url = "https://auth.example.com/auth"
        token_url = "https://auth.example.com/token"
        userinfo_url = "https://auth.example.com/userinfo"
        scopes = ["read", "write"]
    }
    
    api_key = {
        header_name = "X-API-Key"
        validation = {
            type = "database"
            table = "api_keys"
            active_only = true
        }
        rate_limits = {
            free = 100
            premium = 1000
            enterprise = 10000
        }
    }
    
    roles = {
        admin = {
            permissions = ["*"]
            services = ["*"]
        }
        
        user = {
            permissions = ["read:own", "write:own"]
            services = ["user-service", "order-service"]
        }
        
        guest = {
            permissions = ["read:public"]
            services = ["product-service"]
        }
    }
}

// Authentication Manager
class AuthenticationManager {
    private $config;
    private $providers = [];
    
    public function __construct($configPath) {
        $this->config = $this->loadConfig($configPath);
        $this->initializeProviders();
    }
    
    public function authenticate($request, $authType) {
        $provider = $this->providers[$authType];
        
        if (!$provider) {
            throw new AuthenticationException("Unknown authentication type: {$authType}");
        }
        
        return $provider->authenticate($request);
    }
    
    public function authorize($user, $resource, $action) {
        $userRole = $user->getRole();
        $roleConfig = $this->config->authentication_config->roles->$userRole;
        
        if (!$roleConfig) {
            return false;
        }
        
        // Check wildcard permissions
        if (in_array('*', $roleConfig->permissions)) {
            return true;
        }
        
        // Check specific permissions
        $permission = "{$action}:{$resource}";
        return in_array($permission, $roleConfig->permissions);
    }
    
    private function initializeProviders() {
        // JWT Provider
        $this->providers['jwt'] = new JWTProvider($this->config->authentication_config->jwt);
        
        // OAuth2 Provider
        $this->providers['oauth2'] = new OAuth2Provider($this->config->authentication_config->oauth2);
        
        // API Key Provider
        $this->providers['api_key'] = new APIKeyProvider($this->config->authentication_config->api_key);
    }
}

// JWT Provider Implementation
class JWTProvider {
    private $config;
    
    public function __construct($config) {
        $this->config = $config;
    }
    
    public function authenticate($request) {
        $token = $this->extractToken($request);
        
        if (!$token) {
            throw new AuthenticationException("JWT token not found");
        }
        
        try {
            $payload = JWT::decode($token, $this->config->secret, [$this->config->algorithm]);
            
            // Validate issuer and audience
            if ($payload->iss !== $this->config->issuer) {
                throw new AuthenticationException("Invalid issuer");
            }
            
            if ($payload->aud !== $this->config->audience) {
                throw new AuthenticationException("Invalid audience");
            }
            
            // Check expiration
            if ($payload->exp < time()) {
                throw new AuthenticationException("Token expired");
            }
            
            return new User($payload->sub, $payload->role, $payload);
            
        } catch (Exception $e) {
            throw new AuthenticationException("Invalid JWT token: " . $e->getMessage());
        }
    }
    
    private function extractToken($request) {
        $authHeader = $request->getHeader('Authorization');
        
        if (!$authHeader) {
            return null;
        }
        
        if (strpos($authHeader, 'Bearer ') !== 0) {
            return null;
        }
        
        return substr($authHeader, 7);
    }
}
```

### 3. Rate Limiting and Throttling

```php
// Rate Limiting Configuration
rate_limiting_config = {
    storage = {
        type = "redis"
        connection = @env(REDIS_URL)
        key_prefix = "rate_limit:"
    }
    
    algorithms = {
        token_bucket = {
            capacity = 100
            refill_rate = 10
            refill_time = "1 second"
        }
        
        sliding_window = {
            window_size = "1 minute"
            max_requests = 100
        }
        
        leaky_bucket = {
            capacity = 100
            leak_rate = 10
            leak_time = "1 second"
        }
    }
    
    strategies = {
        per_user = {
            key_template = "user:{user_id}:{endpoint}"
            algorithm = "token_bucket"
        }
        
        per_ip = {
            key_template = "ip:{ip_address}:{endpoint}"
            algorithm = "sliding_window"
        }
        
        global = {
            key_template = "global:{endpoint}"
            algorithm = "leaky_bucket"
        }
    }
    
    response_headers = {
        enabled = true
        limit_header = "X-RateLimit-Limit"
        remaining_header = "X-RateLimit-Remaining"
        reset_header = "X-RateLimit-Reset"
    }
}

// Rate Limiter Implementation
class RateLimiter {
    private $config;
    private $storage;
    
    public function __construct($configPath) {
        $this->config = $this->loadConfig($configPath);
        $this->storage = new RedisStorage($this->config->rate_limiting_config->storage);
    }
    
    public function checkRateLimit($request, $limitConfig) {
        $key = $this->generateKey($request, $limitConfig);
        $algorithm = $this->config->rate_limiting_config->algorithms->{$limitConfig->algorithm};
        
        switch ($limitConfig->algorithm) {
            case 'token_bucket':
                return $this->checkTokenBucket($key, $algorithm);
            case 'sliding_window':
                return $this->checkSlidingWindow($key, $algorithm);
            case 'leaky_bucket':
                return $this->checkLeakyBucket($key, $algorithm);
            default:
                throw new RateLimitException("Unknown algorithm: {$limitConfig->algorithm}");
        }
    }
    
    private function checkTokenBucket($key, $algorithm) {
        $current = $this->storage->get($key);
        $now = time();
        
        if ($current === null) {
            $current = [
                'tokens' => $algorithm->capacity,
                'last_refill' => $now
            ];
        }
        
        // Refill tokens
        $timePassed = $now - $current['last_refill'];
        $tokensToAdd = floor($timePassed / $algorithm->refill_time) * $algorithm->refill_rate;
        
        $current['tokens'] = min($algorithm->capacity, $current['tokens'] + $tokensToAdd);
        $current['last_refill'] = $now;
        
        // Check if request can be processed
        if ($current['tokens'] >= 1) {
            $current['tokens'] -= 1;
            $this->storage->set($key, $current, 3600);
            
            return [
                'allowed' => true,
                'remaining' => $current['tokens'],
                'reset_time' => $now + $algorithm->refill_time
            ];
        }
        
        return [
            'allowed' => false,
            'remaining' => 0,
            'reset_time' => $current['last_refill'] + $algorithm->refill_time
        ];
    }
    
    private function generateKey($request, $limitConfig) {
        $template = $this->config->rate_limiting_config->strategies->{$limitConfig->strategy}->key_template;
        
        $key = str_replace('{endpoint}', $request->getPath(), $template);
        
        if (strpos($key, '{user_id}') !== false) {
            $user = $request->getUser();
            $key = str_replace('{user_id}', $user ? $user->getId() : 'anonymous', $key);
        }
        
        if (strpos($key, '{ip_address}') !== false) {
            $key = str_replace('{ip_address}', $request->getClientIP(), $key);
        }
        
        return $this->config->rate_limiting_config->storage->key_prefix . $key;
    }
}
```

## Advanced API Gateway Features

### 1. Request/Response Transformation

```php
// Transformation Configuration
transformation_config = {
    request_transformations = {
        "/api/v1/users" = {
            headers = {
                "X-User-ID" = "user.id"
                "X-User-Role" = "user.role"
            }
            
            body = {
                mapping = {
                    "user_id" = "user.id"
                    "email" = "body.email"
                    "profile" = "body.profile"
                }
                
                validation = {
                    required = ["email"]
                    email_format = true
                    max_length = {
                        "email" = 255
                        "profile.name" = 100
                    }
                }
            }
        }
    }
    
    response_transformations = {
        "/api/v1/users" = {
            headers = {
                "X-Response-Time" = "response_time"
                "X-Cache-Status" = "cache_status"
            }
            
            body = {
                mapping = {
                    "id" = "response.id"
                    "email" = "response.email"
                    "created_at" = "response.created_at"
                    "status" = "response.status"
                }
                
                filtering = {
                    exclude = ["password", "internal_notes"]
                    include = ["id", "email", "profile", "status"]
                }
                
                formatting = {
                    "created_at" = "datetime:Y-m-d H:i:s"
                    "status" = "uppercase"
                }
            }
        }
    }
    
    error_handling = {
        standard_errors = {
            "400" = {
                message = "Bad Request"
                details = "Invalid request parameters"
            }
            "401" = {
                message = "Unauthorized"
                details = "Authentication required"
            }
            "403" = {
                message = "Forbidden"
                details = "Insufficient permissions"
            }
            "404" = {
                message = "Not Found"
                details = "Resource not found"
            }
            "429" = {
                message = "Too Many Requests"
                details = "Rate limit exceeded"
            }
            "500" = {
                message = "Internal Server Error"
                details = "An unexpected error occurred"
            }
        }
        
        custom_errors = {
            "VALIDATION_ERROR" = {
                code = 422
                message = "Validation Failed"
            }
            "SERVICE_UNAVAILABLE" = {
                code = 503
                message = "Service Temporarily Unavailable"
            }
        }
    }
}

// Transformation Engine
class TransformationEngine {
    private $config;
    
    public function __construct($configPath) {
        $this->config = $this->loadConfig($configPath);
    }
    
    public function transformRequest($request, $route) {
        $transformConfig = $this->config->transformation_config->request_transformations->{$route->path};
        
        if (!$transformConfig) {
            return $request;
        }
        
        // Transform headers
        if (isset($transformConfig->headers)) {
            $request = $this->transformHeaders($request, $transformConfig->headers);
        }
        
        // Transform body
        if (isset($transformConfig->body)) {
            $request = $this->transformBody($request, $transformConfig->body);
        }
        
        return $request;
    }
    
    public function transformResponse($response, $route) {
        $transformConfig = $this->config->transformation_config->response_transformations->{$route->path};
        
        if (!$transformConfig) {
            return $response;
        }
        
        // Transform headers
        if (isset($transformConfig->headers)) {
            $response = $this->transformResponseHeaders($response, $transformConfig->headers);
        }
        
        // Transform body
        if (isset($transformConfig->body)) {
            $response = $this->transformResponseBody($response, $transformConfig->body);
        }
        
        return $response;
    }
    
    private function transformHeaders($request, $headerConfig) {
        foreach ($headerConfig as $headerName => $valueTemplate) {
            $value = $this->resolveTemplate($valueTemplate, $request);
            $request->setHeader($headerName, $value);
        }
        
        return $request;
    }
    
    private function transformBody($request, $bodyConfig) {
        $body = $request->getBody();
        
        // Apply mapping
        if (isset($bodyConfig->mapping)) {
            $mappedBody = [];
            foreach ($bodyConfig->mapping as $targetKey => $sourcePath) {
                $value = $this->getValueByPath($sourcePath, $request);
                $this->setValueByPath($targetKey, $value, $mappedBody);
            }
            $body = $mappedBody;
        }
        
        // Validate body
        if (isset($bodyConfig->validation)) {
            $this->validateBody($body, $bodyConfig->validation);
        }
        
        $request->setBody($body);
        return $request;
    }
    
    private function resolveTemplate($template, $request) {
        $user = $request->getUser();
        
        $template = str_replace('user.id', $user ? $user->getId() : '', $template);
        $template = str_replace('user.role', $user ? $user->getRole() : '', $template);
        $template = str_replace('response_time', microtime(true), $template);
        
        return $template;
    }
}
```

### 2. Caching and Performance

```php
// Caching Configuration
caching_config = {
    storage = {
        type = "redis"
        connection = @env(REDIS_CACHE_URL)
        key_prefix = "cache:"
        default_ttl = 300
    }
    
    strategies = {
        response_cache = {
            enabled = true
            cacheable_methods = ["GET"]
            cacheable_status_codes = [200, 201]
            vary_by = ["Authorization", "Accept"]
        }
        
        service_cache = {
            enabled = true
            cache_failures = false
            stale_while_revalidate = true
        }
        
        user_cache = {
            enabled = true
            ttl = 3600
            invalidation = {
                on_update = true
                on_delete = true
            }
        }
    }
    
    cache_keys = {
        user_profile = "user:{user_id}:profile"
        user_orders = "user:{user_id}:orders:{page}:{limit}"
        product_details = "product:{product_id}:details"
        api_usage = "api:{user_id}:usage:{date}"
    }
    
    invalidation = {
        patterns = {
            "user:*" = ["user_profile", "user_orders"]
            "product:*" = ["product_details"]
            "order:*" = ["user_orders"]
        }
        
        events = {
            "user.updated" = ["user_profile"]
            "order.created" = ["user_orders"]
            "product.updated" = ["product_details"]
        }
    }
}

// Cache Manager
class CacheManager {
    private $config;
    private $storage;
    
    public function __construct($configPath) {
        $this->config = $this->loadConfig($configPath);
        $this->storage = new RedisStorage($this->config->caching_config->storage);
    }
    
    public function get($key) {
        return $this->storage->get($key);
    }
    
    public function set($key, $value, $ttl = null) {
        $ttl = $ttl ?: $this->config->caching_config->storage->default_ttl;
        return $this->storage->set($key, $value, $ttl);
    }
    
    public function invalidate($pattern) {
        $keys = $this->storage->keys($pattern);
        foreach ($keys as $key) {
            $this->storage->delete($key);
        }
    }
    
    public function generateCacheKey($request, $cacheConfig) {
        $template = $cacheConfig->cache_key;
        
        // Replace placeholders
        $template = str_replace('{user_id}', $request->getUser()->getId(), $template);
        $template = str_replace('{product_id}', $request->getParameter('product_id'), $template);
        $template = str_replace('{page}', $request->getParameter('page', 1), $template);
        $template = str_replace('{limit}', $request->getParameter('limit', 10), $template);
        $template = str_replace('{date}', date('Y-m-d'), $template);
        
        // Add vary-by parameters
        if (isset($cacheConfig->vary_by)) {
            foreach ($cacheConfig->vary_by as $header) {
                $value = $request->getHeader($header);
                $template .= ":{$header}:{$value}";
            }
        }
        
        return $this->config->caching_config->storage->key_prefix . $template;
    }
}
```

### 3. Monitoring and Observability

```php
// Monitoring Configuration
monitoring_config = {
    metrics = {
        request_count = {
            type = "counter"
            labels = ["method", "path", "status_code"]
        }
        
        response_time = {
            type = "histogram"
            buckets = [0.1, 0.5, 1, 2, 5, 10]
            labels = ["method", "path", "service"]
        }
        
        error_rate = {
            type = "gauge"
            labels = ["method", "path", "error_type"]
        }
        
        active_connections = {
            type = "gauge"
            labels = ["service"]
        }
    }
    
    tracing = {
        enabled = true
        sampler = {
            type = "probabilistic"
            rate = 0.1
        }
        
        propagation = {
            headers = ["X-Trace-ID", "X-Span-ID"]
            format = "w3c"
        }
    }
    
    logging = {
        level = "info"
        format = "json"
        fields = {
            timestamp = true
            trace_id = true
            user_id = true
            request_id = true
            method = true
            path = true
            status_code = true
            response_time = true
        }
        
        destinations = {
            stdout = true
            file = {
                path = "/var/log/api-gateway.log"
                rotation = "daily"
                max_size = "100MB"
            }
            elasticsearch = {
                url = @env(ELASTICSEARCH_URL)
                index = "api-gateway-logs"
            }
        }
    }
    
    alerting = {
        rules = {
            high_error_rate = {
                condition = "error_rate > 0.05"
                duration = "5 minutes"
                severity = "warning"
            }
            
            high_response_time = {
                condition = "response_time_p95 > 2"
                duration = "5 minutes"
                severity = "warning"
            }
            
            service_down = {
                condition = "health_check_failed"
                duration = "1 minute"
                severity = "critical"
            }
        }
        
        notifications = {
            email = ["ops@company.com"]
            slack = "#api-alerts"
            pagerduty = "api-gateway-service"
        }
    }
}

// Monitoring Manager
class MonitoringManager {
    private $config;
    private $metrics;
    private $tracer;
    private $logger;
    
    public function __construct($configPath) {
        $this->config = $this->loadConfig($configPath);
        $this->initializeMonitoring();
    }
    
    public function recordRequest($request, $response, $startTime) {
        $duration = microtime(true) - $startTime;
        $statusCode = $response->getStatusCode();
        $method = $request->getMethod();
        $path = $request->getPath();
        
        // Record metrics
        $this->metrics->increment('request_count', [
            'method' => $method,
            'path' => $path,
            'status_code' => $statusCode
        ]);
        
        $this->metrics->histogram('response_time', $duration, [
            'method' => $method,
            'path' => $path,
            'service' => $this->getServiceName($path)
        ]);
        
        if ($statusCode >= 400) {
            $this->metrics->increment('error_rate', [
                'method' => $method,
                'path' => $path,
                'error_type' => $this->getErrorType($statusCode)
            ]);
        }
        
        // Log request
        $this->logger->info('API Request', [
            'method' => $method,
            'path' => $path,
            'status_code' => $statusCode,
            'response_time' => $duration,
            'user_id' => $request->getUser() ? $request->getUser()->getId() : null,
            'request_id' => $request->getHeader('X-Request-ID'),
            'trace_id' => $this->tracer->getCurrentTraceId()
        ]);
    }
    
    private function initializeMonitoring() {
        // Initialize metrics
        $this->metrics = new PrometheusMetrics();
        
        // Initialize tracing
        if ($this->config->monitoring_config->tracing->enabled) {
            $this->tracer = new OpenTelemetryTracer($this->config->monitoring_config->tracing);
        }
        
        // Initialize logging
        $this->logger = new Logger($this->config->monitoring_config->logging);
    }
}
```

## Integration Patterns

### 1. Database-Driven API Gateway

```php
// Live Database Queries in API Gateway Config
api_gateway_data = {
    route_definitions = @query("
        SELECT 
            path,
            service_name,
            methods,
            authentication_type,
            rate_limit_requests,
            rate_limit_burst,
            cache_enabled,
            cache_ttl
        FROM api_routes 
        WHERE is_active = true
        ORDER BY priority DESC
    ")
    
    service_endpoints = @query("
        SELECT 
            service_name,
            url,
            health_check_path,
            timeout_seconds,
            max_retries,
            circuit_breaker_enabled
        FROM service_endpoints 
        WHERE is_active = true
    ")
    
    user_permissions = @query("
        SELECT 
            user_id,
            role,
            permissions,
            service_access
        FROM user_permissions 
        WHERE is_active = true
    ")
    
    rate_limit_configs = @query("
        SELECT 
            user_id,
            plan_type,
            requests_per_minute,
            burst_size,
            reset_time
        FROM rate_limits 
        WHERE is_active = true
    ")
    
    cache_invalidation_rules = @query("
        SELECT 
            pattern,
            affected_keys,
            invalidation_strategy
        FROM cache_invalidation_rules 
        WHERE is_active = true
    ")
}
```

### 2. Service Mesh Integration

```php
// Service Mesh Configuration
service_mesh_config = {
    istio = {
        enabled = true
        namespace = "api-gateway"
        
        virtual_services = {
            user_service = {
                hosts = ["user-service"]
                http = [
                    {
                        route = [
                            {
                                destination = {
                                    host = "user-service"
                                    subset = "v1"
                                }
                                weight = 80
                            },
                            {
                                destination = {
                                    host = "user-service"
                                    subset = "v2"
                                }
                                weight = 20
                            }
                        ]
                    }
                ]
            }
        }
        
        destination_rules = {
            user_service = {
                host = "user-service"
                subsets = [
                    {
                        name = "v1"
                        labels = ["version=v1"]
                    },
                    {
                        name = "v2"
                        labels = ["version=v2"]
                    }
                ]
            }
        }
        
        policies = {
            circuit_breaker = {
                outlier_detection = {
                    consecutive_errors = 5
                    interval = "30s"
                    base_ejection_time = "60s"
                }
            }
            
            retry = {
                attempts = 3
                per_try_timeout = "2s"
                retry_on = ["connect-failure", "refused-stream"]
            }
        }
    }
    
    envoy = {
        enabled = true
        config_path = "/etc/envoy/envoy.yaml"
        
        listeners = [
            {
                name = "api_gateway_listener"
                address = "0.0.0.0:8080"
                filter_chains = [
                    {
                        filters = [
                            {
                                name = "envoy.filters.network.http_connection_manager"
                                typed_config = {
                                    stat_prefix = "api_gateway"
                                    route_config = {
                                        name = "api_gateway_routes"
                                        virtual_hosts = [
                                            {
                                                name = "api_gateway"
                                                domains = ["*"]
                                                routes = [
                                                    {
                                                        match = {
                                                            prefix = "/api/v1/"
                                                        }
                                                        route = {
                                                            cluster = "api_gateway_cluster"
                                                        }
                                                    }
                                                ]
                                            }
                                        ]
                                    }
                                }
                            }
                        ]
                    }
                ]
            }
        ]
        
        clusters = [
            {
                name = "api_gateway_cluster"
                connect_timeout = "30s"
                type = "STRICT_DNS"
                lb_policy = "ROUND_ROBIN"
                hosts = [
                    {
                        socket_address = {
                            address = "api-gateway"
                            port_value = 8080
                        }
                    }
                ]
            }
        ]
    }
}

// Service Mesh Integration
class ServiceMeshIntegration {
    private $config;
    
    public function __construct($configPath) {
        $this->config = $this->loadConfig($configPath);
    }
    
    public function configureIstio() {
        if (!$this->config->service_mesh_config->istio->enabled) {
            return;
        }
        
        $istioConfig = $this->config->service_mesh_config->istio;
        
        // Generate VirtualService YAML
        foreach ($istioConfig->virtual_services as $serviceName => $config) {
            $this->generateVirtualServiceYAML($serviceName, $config);
        }
        
        // Generate DestinationRule YAML
        foreach ($istioConfig->destination_rules as $serviceName => $config) {
            $this->generateDestinationRuleYAML($serviceName, $config);
        }
        
        // Apply policies
        $this->applyIstioPolicies($istioConfig->policies);
    }
    
    private function generateVirtualServiceYAML($serviceName, $config) {
        $yaml = [
            'apiVersion' => 'networking.istio.io/v1alpha3',
            'kind' => 'VirtualService',
            'metadata' => [
                'name' => $serviceName,
                'namespace' => $this->config->service_mesh_config->istio->namespace
            ],
            'spec' => [
                'hosts' => $config->hosts,
                'http' => $config->http
            ]
        ];
        
        $this->applyKubernetesResource($yaml);
    }
}
```

### 3. API Versioning and Evolution

```php
// API Versioning Configuration
api_versioning_config = {
    version_strategy = "url_path"
    supported_versions = ["v1", "v2", "v3"]
    default_version = "v2"
    
    version_mapping = {
        "v1" = {
            deprecated = true
            sunset_date = "2024-12-31"
            migration_guide = "https://docs.example.com/migration-v1-to-v2"
        }
        
        "v2" = {
            stable = true
            features = ["enhanced_filtering", "bulk_operations", "real_time_updates"]
        }
        
        "v3" = {
            beta = true
            features = ["graphql", "subscriptions", "advanced_analytics"]
        }
    }
    
    backward_compatibility = {
        enabled = true
        deprecated_features = {
            "v1.users.search" = {
                deprecated_in = "v2"
                removed_in = "v4"
                alternative = "v2.users.list"
            }
        }
        
        response_transformations = {
            "v1_to_v2" = {
                "users" = {
                    "id" = "id"
                    "email" = "email"
                    "name" = "profile.name"
                    "created" = "created_at"
                }
            }
        }
    }
    
    deprecation_notices = {
        enabled = true
        header_name = "X-API-Deprecation"
        notice_format = "This API version is deprecated. Please upgrade to {version} by {date}"
    }
}

// API Version Manager
class APIVersionManager {
    private $config;
    
    public function __construct($configPath) {
        $this->config = $this->loadConfig($configPath);
    }
    
    public function extractVersion($request) {
        $path = $request->getPath();
        
        if ($this->config->api_versioning_config->version_strategy === 'url_path') {
            $pathParts = explode('/', trim($path, '/'));
            if (count($pathParts) >= 2 && strpos($pathParts[1], 'v') === 0) {
                return $pathParts[1];
            }
        }
        
        return $this->config->api_versioning_config->default_version;
    }
    
    public function validateVersion($version) {
        return in_array($version, $this->config->api_versioning_config->supported_versions);
    }
    
    public function isDeprecated($version, $endpoint) {
        $deprecatedFeatures = $this->config->api_versioning_config->backward_compatibility->deprecated_features;
        $key = "{$version}.{$endpoint}";
        
        return isset($deprecatedFeatures->$key);
    }
    
    public function getDeprecationNotice($version, $endpoint) {
        $deprecatedFeatures = $this->config->api_versioning_config->backward_compatibility->deprecated_features;
        $key = "{$version}.{$endpoint}";
        
        if (isset($deprecatedFeatures->$key)) {
            $feature = $deprecatedFeatures->$key;
            return str_replace(
                ['{version}', '{date}'],
                [$feature->alternative, $feature->removed_in],
                $this->config->api_versioning_config->deprecation_notices->notice_format
            );
        }
        
        return null;
    }
}
```

## Best Practices

### 1. Security Hardening

```php
// Security Configuration
security_config = {
    input_validation = {
        enabled = true
        sanitization = true
        max_request_size = "10MB"
        allowed_content_types = ["application/json", "application/xml"]
    }
    
    output_encoding = {
        enabled = true
        encoding_type = "html"
        content_security_policy = true
    }
    
    cors = {
        enabled = true
        allowed_origins = ["https://app.example.com", "https://admin.example.com"]
        allowed_methods = ["GET", "POST", "PUT", "DELETE", "OPTIONS"]
        allowed_headers = ["Content-Type", "Authorization", "X-API-Key"]
        max_age = 86400
    }
    
    rate_limiting = {
        enabled = true
        strategies = ["per_user", "per_ip", "global"]
        burst_protection = true
    }
    
    authentication = {
        required = true
        methods = ["jwt", "oauth2", "api_key"]
        session_timeout = 3600
        refresh_token_rotation = true
    }
    
    authorization = {
        enabled = true
        rbac = true
        attribute_based_access_control = true
        audit_logging = true
    }
}
```

### 2. Performance Optimization

```php
// Performance Configuration
performance_config = {
    connection_pooling = {
        enabled = true
        max_connections = 100
        min_connections = 10
        connection_timeout = 30
        idle_timeout = 300
    }
    
    compression = {
        enabled = true
        algorithms = ["gzip", "deflate"]
        min_size = 1024
        content_types = ["application/json", "text/html", "text/plain"]
    }
    
    caching = {
        enabled = true
        strategies = ["response_cache", "service_cache", "user_cache"]
        ttl_optimization = true
        cache_warming = true
    }
    
    load_balancing = {
        algorithm = "least_connections"
        health_checks = true
        failover = true
        sticky_sessions = false
    }
    
    async_processing = {
        enabled = true
        worker_pool_size = 10
        queue_size = 1000
        timeout = 30
    }
}
```

### 3. Monitoring and Alerting

```php
// Monitoring Configuration
monitoring_config = {
    metrics = {
        collection = true
        export = {
            prometheus = true
            statsd = false
            custom_endpoint = false
        }
        
        custom_metrics = {
            business_metrics = true
            user_behavior = true
            performance_indicators = true
        }
    }
    
    tracing = {
        distributed_tracing = true
        sampling_rate = 0.1
        propagation = "w3c"
        backends = ["jaeger", "zipkin"]
    }
    
    logging = {
        structured_logging = true
        log_levels = ["error", "warn", "info", "debug"]
        log_rotation = true
        log_aggregation = true
    }
    
    alerting = {
        rules = {
            error_rate_threshold = 0.05
            response_time_threshold = 2000
            availability_threshold = 0.99
        }
        
        notifications = {
            channels = ["email", "slack", "pagerduty"]
            escalation = true
            on_call_rotation = true
        }
    }
}
```

This comprehensive API gateway documentation demonstrates how TuskLang revolutionizes API management by providing configuration-driven routing, authentication, rate limiting, and intelligent request processing while maintaining the rebellious spirit and technical excellence that defines the TuskLang ecosystem. 