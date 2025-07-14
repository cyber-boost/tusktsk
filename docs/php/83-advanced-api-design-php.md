# 🚀 Advanced API Design with TuskLang & PHP

## Introduction
API design is the foundation of modern applications. TuskLang and PHP let you build sophisticated APIs with config-driven routing, authentication, rate limiting, and documentation that scales from startup to enterprise.

## Key Features
- **RESTful API design patterns**
- **GraphQL integration**
- **API versioning strategies**
- **Authentication and authorization**
- **Rate limiting and throttling**
- **API documentation and testing**
- **Monitoring and analytics**

## Example: API Configuration
```ini
[api]
version: @env("API_VERSION", "v1")
base_url: @env("API_BASE_URL", "https://api.example.com")
auth: @go("api.Authenticate")
rate_limit: @go("api.RateLimit")
documentation: @file.read("api-docs.yaml")
metrics: @metrics("api_requests", 0)
```

## PHP: RESTful API Implementation
```php
<?php

namespace App\API;

use TuskLang\Config;
use TuskLang\Operators\Env;
use TuskLang\Operators\Metrics;
use TuskLang\Operators\Go;

class APIController
{
    private $config;
    private $auth;
    private $rateLimiter;
    
    public function __construct()
    {
        $this->config = new Config();
        $this->auth = new AuthService();
        $this->rateLimiter = new RateLimitService();
    }
    
    public function handleRequest($request)
    {
        // Get API configuration
        $apiVersion = Env::get("API_VERSION", "v1");
        $baseUrl = Env::get("API_BASE_URL", "https://api.example.com");
        
        // Authenticate request
        if (!$this->auth->authenticate($request)) {
            return $this->errorResponse(401, "Unauthorized");
        }
        
        // Check rate limits
        if (!$this->rateLimiter->check($request)) {
            return $this->errorResponse(429, "Rate limit exceeded");
        }
        
        // Route request
        $response = $this->routeRequest($request);
        
        // Record metrics
        Metrics::record("api_requests", 1, [
            "method" => $request->getMethod(),
            "endpoint" => $request->getPath(),
            "version" => $apiVersion
        ]);
        
        return $response;
    }
    
    private function routeRequest($request)
    {
        $path = $request->getPath();
        $method = $request->getMethod();
        
        // Use TuskLang config for routing
        $routes = $this->config->get("api.routes");
        
        foreach ($routes as $route) {
            if ($this->matchRoute($route, $path, $method)) {
                return $this->executeHandler($route['handler'], $request);
            }
        }
        
        return $this->errorResponse(404, "Not found");
    }
    
    private function matchRoute($route, $path, $method)
    {
        $pattern = $route['pattern'];
        $routeMethod = $route['method'];
        
        return $method === $routeMethod && preg_match($pattern, $path);
    }
    
    private function executeHandler($handler, $request)
    {
        // Execute handler with dependency injection
        $container = new Container();
        $handlerInstance = $container->get($handler);
        
        return $handlerInstance->handle($request);
    }
    
    private function errorResponse($status, $message)
    {
        return new Response([
            'error' => $message,
            'status' => $status,
            'timestamp' => date('c')
        ], $status);
    }
}
```

## GraphQL Integration
```php
<?php

namespace App\GraphQL;

use GraphQL\GraphQL;
use GraphQL\Type\Schema;
use TuskLang\Config;

class GraphQLController
{
    private $config;
    private $schema;
    
    public function __construct()
    {
        $this->config = new Config();
        $this->schema = $this->buildSchema();
    }
    
    public function handleQuery($query, $variables = [])
    {
        // Get GraphQL configuration
        $maxDepth = $this->config->get("graphql.max_depth", 10);
        $maxComplexity = $this->config->get("graphql.max_complexity", 1000);
        
        // Execute GraphQL query
        $result = GraphQL::executeQuery(
            $this->schema,
            $query,
            null,
            null,
            $variables
        );
        
        // Check for errors
        if (!empty($result->errors)) {
            return $this->errorResponse($result->errors);
        }
        
        return $result->data;
    }
    
    private function buildSchema()
    {
        // Build GraphQL schema from TuskLang config
        $types = $this->config->get("graphql.types", []);
        $queries = $this->config->get("graphql.queries", []);
        $mutations = $this->config->get("graphql.mutations", []);
        
        return new Schema([
            'query' => $this->buildQueryType($queries),
            'mutation' => $this->buildMutationType($mutations),
            'types' => $this->buildTypes($types)
        ]);
    }
    
    private function buildQueryType($queries)
    {
        $fields = [];
        
        foreach ($queries as $name => $config) {
            $fields[$name] = [
                'type' => $this->getGraphQLType($config['type']),
                'args' => $config['args'] ?? [],
                'resolve' => function ($root, $args) use ($config) {
                    return $this->resolveQuery($config['resolver'], $args);
                }
            ];
        }
        
        return new ObjectType([
            'name' => 'Query',
            'fields' => $fields
        ]);
    }
    
    private function resolveQuery($resolver, $args)
    {
        // Execute resolver with dependency injection
        $container = new Container();
        $resolverInstance = $container->get($resolver);
        
        return $resolverInstance->resolve($args);
    }
}
```

## API Versioning
```php
<?php

namespace App\API\Versioning;

use TuskLang\Config;

class APIVersionManager
{
    private $config;
    private $versions = [];
    
    public function __construct()
    {
        $this->config = new Config();
        $this->loadVersions();
    }
    
    public function getVersion($version)
    {
        if (!isset($this->versions[$version])) {
            throw new \Exception("API version {$version} not found");
        }
        
        return $this->versions[$version];
    }
    
    public function getLatestVersion()
    {
        $latest = $this->config->get("api.latest_version", "v1");
        return $this->getVersion($latest);
    }
    
    public function isDeprecated($version)
    {
        $deprecatedVersions = $this->config->get("api.deprecated_versions", []);
        return in_array($version, $deprecatedVersions);
    }
    
    private function loadVersions()
    {
        $versionConfigs = $this->config->get("api.versions", []);
        
        foreach ($versionConfigs as $version => $config) {
            $this->versions[$version] = new APIVersion($version, $config);
        }
    }
}

class APIVersion
{
    private $version;
    private $config;
    private $routes;
    private $middleware;
    
    public function __construct($version, $config)
    {
        $this->version = $version;
        $this->config = $config;
        $this->routes = $config['routes'] ?? [];
        $this->middleware = $config['middleware'] ?? [];
    }
    
    public function getRoutes()
    {
        return $this->routes;
    }
    
    public function getMiddleware()
    {
        return $this->middleware;
    }
    
    public function getConfig($key, $default = null)
    {
        return $this->config[$key] ?? $default;
    }
}
```

## Authentication and Authorization
```php
<?php

namespace App\Auth;

use TuskLang\Config;
use TuskLang\Operators\Env;
use TuskLang\Operators\Encrypt;

class AuthService
{
    private $config;
    private $jwtSecret;
    
    public function __construct()
    {
        $this->config = new Config();
        $this->jwtSecret = Env::secure("JWT_SECRET");
    }
    
    public function authenticate($request)
    {
        $authHeader = $request->getHeader("Authorization");
        
        if (!$authHeader) {
            return false;
        }
        
        $token = $this->extractToken($authHeader);
        
        if (!$token) {
            return false;
        }
        
        return $this->validateToken($token);
    }
    
    public function authorize($user, $resource, $action)
    {
        // Get user permissions from TuskLang config
        $permissions = $this->config->get("auth.permissions.{$user->role}", []);
        
        $permissionKey = "{$resource}:{$action}";
        
        return in_array($permissionKey, $permissions);
    }
    
    public function generateToken($user)
    {
        $payload = [
            'user_id' => $user->id,
            'email' => $user->email,
            'role' => $user->role,
            'exp' => time() + $this->config->get("auth.token_expiry", 3600)
        ];
        
        return $this->encodeJWT($payload);
    }
    
    private function validateToken($token)
    {
        try {
            $payload = $this->decodeJWT($token);
            
            if ($payload['exp'] < time()) {
                return false;
            }
            
            return $payload;
        } catch (\Exception $e) {
            return false;
        }
    }
    
    private function encodeJWT($payload)
    {
        $header = json_encode(['typ' => 'JWT', 'alg' => 'HS256']);
        $payload = json_encode($payload);
        
        $base64Header = str_replace(['+', '/', '='], ['-', '_', ''], base64_encode($header));
        $base64Payload = str_replace(['+', '/', '='], ['-', '_', ''], base64_encode($payload));
        
        $signature = hash_hmac('sha256', $base64Header . "." . $base64Payload, $this->jwtSecret, true);
        $base64Signature = str_replace(['+', '/', '='], ['-', '_', ''], base64_encode($signature));
        
        return $base64Header . "." . $base64Payload . "." . $base64Signature;
    }
    
    private function decodeJWT($token)
    {
        $parts = explode('.', $token);
        
        if (count($parts) !== 3) {
            throw new \Exception("Invalid JWT format");
        }
        
        $payload = base64_decode(str_replace(['-', '_'], ['+', '/'], $parts[1]));
        
        return json_decode($payload, true);
    }
}
```

## Rate Limiting
```php
<?php

namespace App\RateLimit;

use TuskLang\Config;
use TuskLang\Operators\Cache;
use TuskLang\Operators\Metrics;

class RateLimitService
{
    private $config;
    private $redis;
    
    public function __construct()
    {
        $this->config = new Config();
        $this->redis = new Redis();
    }
    
    public function check($request)
    {
        $key = $this->generateKey($request);
        $limit = $this->getLimit($request);
        $window = $this->config->get("rate_limit.window", 3600);
        
        $current = $this->redis->incr($key);
        
        if ($current === 1) {
            $this->redis->expire($key, $window);
        }
        
        if ($current > $limit) {
            Metrics::record("rate_limit_exceeded", 1, [
                "ip" => $request->getIP(),
                "endpoint" => $request->getPath()
            ]);
            
            return false;
        }
        
        return true;
    }
    
    private function generateKey($request)
    {
        $ip = $request->getIP();
        $endpoint = $request->getPath();
        $user = $request->getUser();
        
        if ($user) {
            return "rate_limit:user:{$user->id}:{$endpoint}";
        }
        
        return "rate_limit:ip:{$ip}:{$endpoint}";
    }
    
    private function getLimit($request)
    {
        $user = $request->getUser();
        
        if ($user) {
            return $this->config->get("rate_limit.users.{$user->role}", 1000);
        }
        
        return $this->config->get("rate_limit.anonymous", 100);
    }
}
```

## API Documentation
```php
<?php

namespace App\Documentation;

use TuskLang\Config;

class APIDocumentation
{
    private $config;
    
    public function __construct()
    {
        $this->config = new Config();
    }
    
    public function generateOpenAPI()
    {
        $spec = [
            'openapi' => '3.0.0',
            'info' => [
                'title' => $this->config->get("api.title", "My API"),
                'version' => $this->config->get("api.version", "1.0.0"),
                'description' => $this->config->get("api.description", "")
            ],
            'paths' => $this->generatePaths(),
            'components' => $this->generateComponents()
        ];
        
        return json_encode($spec, JSON_PRETTY_PRINT);
    }
    
    private function generatePaths()
    {
        $paths = [];
        $routes = $this->config->get("api.routes", []);
        
        foreach ($routes as $route) {
            $path = $route['path'];
            $method = strtolower($route['method']);
            
            if (!isset($paths[$path])) {
                $paths[$path] = [];
            }
            
            $paths[$path][$method] = [
                'summary' => $route['summary'] ?? "",
                'description' => $route['description'] ?? "",
                'parameters' => $route['parameters'] ?? [],
                'responses' => $route['responses'] ?? [],
                'security' => $route['security'] ?? []
            ];
        }
        
        return $paths;
    }
    
    private function generateComponents()
    {
        return [
            'schemas' => $this->config->get("api.schemas", []),
            'securitySchemes' => $this->config->get("api.security_schemes", [])
        ];
    }
}
```

## Best Practices
- **Use consistent naming conventions**
- **Implement proper error handling**
- **Version your APIs from the start**
- **Use authentication and authorization**
- **Implement rate limiting**
- **Document your APIs thoroughly**
- **Monitor API performance**

## Performance Optimization
- **Use caching for frequently accessed data**
- **Implement pagination for large datasets**
- **Use compression for responses**
- **Monitor API response times**

## Security Considerations
- **Validate all input data**
- **Use HTTPS for all API calls**
- **Implement proper authentication**
- **Use rate limiting to prevent abuse**
- **Sanitize error messages**

## Troubleshooting
- **Monitor API logs for errors**
- **Check rate limiting configuration**
- **Verify authentication tokens**
- **Monitor API performance metrics**

## Conclusion
TuskLang + PHP = APIs that are powerful, secure, and well-documented. Build APIs that scale and delight developers. 