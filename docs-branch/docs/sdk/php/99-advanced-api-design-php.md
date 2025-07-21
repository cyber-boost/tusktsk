# 🌐 Advanced API Design with TuskLang & PHP

## Introduction
API design is the foundation of modern application architecture. TuskLang and PHP let you implement sophisticated API designs with config-driven RESTful endpoints, GraphQL schemas, versioning strategies, and comprehensive documentation that provides powerful, scalable, and developer-friendly interfaces.

## Key Features
- **RESTful API design**
- **GraphQL implementation**
- **API versioning strategies**
- **Rate limiting and throttling**
- **Authentication and authorization**
- **Comprehensive documentation**

## Example: API Configuration
```ini
[api]
design: @go("api.ConfigureDesign")
versioning: @go("api.ConfigureVersioning")
rate_limiting: @go("api.ConfigureRateLimiting")
authentication: @go("api.ConfigureAuthentication")
documentation: @go("api.ConfigureDocumentation")
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
    private $router;
    private $authenticator;
    private $rateLimiter;
    private $validator;
    
    public function __construct()
    {
        $this->config = new Config();
        $this->router = new APIRouter();
        $this->authenticator = new APIAuthenticator();
        $this->rateLimiter = new RateLimiter();
        $this->validator = new RequestValidator();
        
        $this->setupRoutes();
    }
    
    public function handleRequest($request)
    {
        $startTime = microtime(true);
        
        try {
            // Authenticate request
            $user = $this->authenticator->authenticate($request);
            
            // Rate limiting
            $this->rateLimiter->checkLimit($request, $user);
            
            // Validate request
            $this->validator->validate($request);
            
            // Route request
            $response = $this->router->route($request);
            
            // Add response headers
            $this->addResponseHeaders($response);
            
            $duration = (microtime(true) - $startTime) * 1000;
            
            // Record metrics
            Metrics::record("api_request_duration", $duration, [
                "method" => $request->getMethod(),
                "endpoint" => $request->getPathInfo(),
                "status_code" => $response->getStatusCode()
            ]);
            
            return $response;
            
        } catch (AuthenticationException $e) {
            return $this->handleAuthenticationError($e);
        } catch (RateLimitException $e) {
            return $this->handleRateLimitError($e);
        } catch (ValidationException $e) {
            return $this->handleValidationError($e);
        } catch (\Exception $e) {
            return $this->handleGenericError($e);
        }
    }
    
    private function setupRoutes()
    {
        $routes = $this->config->get('api.routes', []);
        
        foreach ($routes as $route) {
            $this->router->addRoute(
                $route['method'],
                $route['path'],
                $route['handler'],
                $route['middleware'] ?? []
            );
        }
    }
    
    private function addResponseHeaders($response)
    {
        $headers = $this->config->get('api.response_headers', []);
        
        foreach ($headers as $name => $value) {
            $response->headers->set($name, $value);
        }
        
        // Add CORS headers
        $this->addCORSHeaders($response);
    }
    
    private function addCORSHeaders($response)
    {
        $corsConfig = $this->config->get('api.cors', []);
        
        $response->headers->set('Access-Control-Allow-Origin', $corsConfig['allow_origin'] ?? '*');
        $response->headers->set('Access-Control-Allow-Methods', $corsConfig['allow_methods'] ?? 'GET, POST, PUT, DELETE, OPTIONS');
        $response->headers->set('Access-Control-Allow-Headers', $corsConfig['allow_headers'] ?? 'Content-Type, Authorization');
    }
    
    private function handleAuthenticationError(AuthenticationException $e)
    {
        return new JsonResponse([
            'error' => 'Authentication required',
            'code' => 'AUTHENTICATION_REQUIRED',
            'message' => $e->getMessage()
        ], 401);
    }
    
    private function handleRateLimitError(RateLimitException $e)
    {
        $response = new JsonResponse([
            'error' => 'Rate limit exceeded',
            'code' => 'RATE_LIMIT_EXCEEDED',
            'message' => $e->getMessage()
        ], 429);
        
        $response->headers->set('Retry-After', $e->getRetryAfter());
        
        return $response;
    }
    
    private function handleValidationError(ValidationException $e)
    {
        return new JsonResponse([
            'error' => 'Validation failed',
            'code' => 'VALIDATION_ERROR',
            'message' => $e->getMessage(),
            'details' => $e->getDetails()
        ], 400);
    }
    
    private function handleGenericError(\Exception $e)
    {
        $statusCode = $e->getCode() ?: 500;
        
        return new JsonResponse([
            'error' => 'Internal server error',
            'code' => 'INTERNAL_ERROR',
            'message' => $this->config->get('api.debug') ? $e->getMessage() : 'An unexpected error occurred'
        ], $statusCode);
    }
}

class APIRouter
{
    private $routes = [];
    
    public function addRoute($method, $path, $handler, $middleware = [])
    {
        $this->routes[] = [
            'method' => $method,
            'path' => $path,
            'handler' => $handler,
            'middleware' => $middleware
        ];
    }
    
    public function route($request)
    {
        $method = $request->getMethod();
        $path = $request->getPathInfo();
        
        foreach ($this->routes as $route) {
            if ($this->matchesRoute($route, $method, $path)) {
                return $this->executeHandler($route, $request);
            }
        }
        
        throw new NotFoundException("Route not found: {$method} {$path}");
    }
    
    private function matchesRoute($route, $method, $path)
    {
        if ($route['method'] !== $method) {
            return false;
        }
        
        $pattern = $this->convertPathToRegex($route['path']);
        return preg_match($pattern, $path);
    }
    
    private function convertPathToRegex($path)
    {
        $pattern = preg_replace('/\{(\w+)\}/', '([^/]+)', $path);
        return "#^{$pattern}$#";
    }
    
    private function executeHandler($route, $request)
    {
        $handler = $route['handler'];
        
        // Execute middleware
        foreach ($route['middleware'] as $middleware) {
            $middlewareInstance = new $middleware();
            $middlewareInstance->process($request);
        }
        
        // Execute handler
        if (is_callable($handler)) {
            return call_user_func($handler, $request);
        }
        
        if (is_string($handler)) {
            list($class, $method) = explode('::', $handler);
            $instance = new $class();
            return $instance->$method($request);
        }
        
        throw new \Exception("Invalid handler: " . json_encode($handler));
    }
}

class UserController
{
    private $userService;
    
    public function __construct()
    {
        $this->userService = new UserService();
    }
    
    public function getUsers($request)
    {
        $page = $request->query->get('page', 1);
        $limit = $request->query->get('limit', 10);
        $filters = $request->query->all();
        
        $users = $this->userService->getUsers($page, $limit, $filters);
        
        return new JsonResponse([
            'data' => $users,
            'pagination' => [
                'page' => $page,
                'limit' => $limit,
                'total' => $this->userService->getTotalCount($filters)
            ]
        ]);
    }
    
    public function getUser($request, $id)
    {
        $user = $this->userService->getUser($id);
        
        if (!$user) {
            throw new NotFoundException("User not found: {$id}");
        }
        
        return new JsonResponse(['data' => $user]);
    }
    
    public function createUser($request)
    {
        $data = json_decode($request->getContent(), true);
        
        $user = $this->userService->createUser($data);
        
        return new JsonResponse(['data' => $user], 201);
    }
    
    public function updateUser($request, $id)
    {
        $data = json_decode($request->getContent(), true);
        
        $user = $this->userService->updateUser($id, $data);
        
        if (!$user) {
            throw new NotFoundException("User not found: {$id}");
        }
        
        return new JsonResponse(['data' => $user]);
    }
    
    public function deleteUser($request, $id)
    {
        $deleted = $this->userService->deleteUser($id);
        
        if (!$deleted) {
            throw new NotFoundException("User not found: {$id}");
        }
        
        return new JsonResponse(null, 204);
    }
}
```

## GraphQL Implementation
```php
<?php

namespace App\API\GraphQL;

use TuskLang\Config;
use GraphQL\GraphQL;
use GraphQL\Type\Schema;
use GraphQL\Type\Definition\ObjectType;
use GraphQL\Type\Definition\Type;

class GraphQLController
{
    private $config;
    private $schema;
    private $resolvers;
    
    public function __construct()
    {
        $this->config = new Config();
        $this->resolvers = new ResolverRegistry();
        $this->buildSchema();
    }
    
    public function handleRequest($request)
    {
        $input = json_decode($request->getContent(), true);
        
        $query = $input['query'] ?? '';
        $variables = $input['variables'] ?? [];
        $operationName = $input['operationName'] ?? null;
        
        try {
            $result = GraphQL::executeQuery(
                $this->schema,
                $query,
                null,
                null,
                $variables,
                $operationName
            );
            
            return new JsonResponse($result->toArray());
            
        } catch (\Exception $e) {
            return new JsonResponse([
                'errors' => [
                    [
                        'message' => $e->getMessage(),
                        'extensions' => [
                            'code' => 'GRAPHQL_ERROR'
                        ]
                    ]
                ]
            ], 400);
        }
    }
    
    private function buildSchema()
    {
        $this->schema = new Schema([
            'query' => $this->buildQueryType(),
            'mutation' => $this->buildMutationType(),
            'subscription' => $this->buildSubscriptionType()
        ]);
    }
    
    private function buildQueryType()
    {
        return new ObjectType([
            'name' => 'Query',
            'fields' => [
                'users' => [
                    'type' => Type::listOf($this->getUserType()),
                    'args' => [
                        'limit' => Type::int(),
                        'offset' => Type::int(),
                        'filter' => $this->getUserFilterType()
                    ],
                    'resolve' => function($root, $args) {
                        return $this->resolvers->resolveUsers($args);
                    }
                ],
                'user' => [
                    'type' => $this->getUserType(),
                    'args' => [
                        'id' => Type::nonNull(Type::id())
                    ],
                    'resolve' => function($root, $args) {
                        return $this->resolvers->resolveUser($args['id']);
                    }
                ]
            ]
        ]);
    }
    
    private function buildMutationType()
    {
        return new ObjectType([
            'name' => 'Mutation',
            'fields' => [
                'createUser' => [
                    'type' => $this->getUserType(),
                    'args' => [
                        'input' => Type::nonNull($this->getUserInputType())
                    ],
                    'resolve' => function($root, $args) {
                        return $this->resolvers->resolveCreateUser($args['input']);
                    }
                ],
                'updateUser' => [
                    'type' => $this->getUserType(),
                    'args' => [
                        'id' => Type::nonNull(Type::id()),
                        'input' => Type::nonNull($this->getUserInputType())
                    ],
                    'resolve' => function($root, $args) {
                        return $this->resolvers->resolveUpdateUser($args['id'], $args['input']);
                    }
                ],
                'deleteUser' => [
                    'type' => Type::boolean(),
                    'args' => [
                        'id' => Type::nonNull(Type::id())
                    ],
                    'resolve' => function($root, $args) {
                        return $this->resolvers->resolveDeleteUser($args['id']);
                    }
                ]
            ]
        ]);
    }
    
    private function getUserType()
    {
        return new ObjectType([
            'name' => 'User',
            'fields' => [
                'id' => Type::id(),
                'email' => Type::string(),
                'name' => Type::string(),
                'created_at' => Type::string(),
                'updated_at' => Type::string(),
                'posts' => [
                    'type' => Type::listOf($this->getPostType()),
                    'resolve' => function($user) {
                        return $this->resolvers->resolveUserPosts($user['id']);
                    }
                ]
            ]
        ]);
    }
    
    private function getUserInputType()
    {
        return new InputObjectType([
            'name' => 'UserInput',
            'fields' => [
                'email' => Type::nonNull(Type::string()),
                'name' => Type::nonNull(Type::string()),
                'password' => Type::string()
            ]
        ]);
    }
    
    private function getUserFilterType()
    {
        return new InputObjectType([
            'name' => 'UserFilter',
            'fields' => [
                'email' => Type::string(),
                'name' => Type::string(),
                'created_after' => Type::string()
            ]
        ]);
    }
}

class ResolverRegistry
{
    private $userService;
    private $postService;
    
    public function __construct()
    {
        $this->userService = new UserService();
        $this->postService = new PostService();
    }
    
    public function resolveUsers($args)
    {
        $limit = $args['limit'] ?? 10;
        $offset = $args['offset'] ?? 0;
        $filter = $args['filter'] ?? [];
        
        return $this->userService->getUsers($offset, $limit, $filter);
    }
    
    public function resolveUser($id)
    {
        return $this->userService->getUser($id);
    }
    
    public function resolveCreateUser($input)
    {
        return $this->userService->createUser($input);
    }
    
    public function resolveUpdateUser($id, $input)
    {
        return $this->userService->updateUser($id, $input);
    }
    
    public function resolveDeleteUser($id)
    {
        return $this->userService->deleteUser($id);
    }
    
    public function resolveUserPosts($userId)
    {
        return $this->postService->getPostsByUser($userId);
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
    
    public function getVersion($request)
    {
        // Check header first
        $version = $request->headers->get('X-API-Version');
        
        if (!$version) {
            // Check URL path
            $path = $request->getPathInfo();
            if (preg_match('/\/v(\d+)\//', $path, $matches)) {
                $version = $matches[1];
            }
        }
        
        if (!$version) {
            // Check query parameter
            $version = $request->query->get('version');
        }
        
        if (!$version) {
            // Use default version
            $version = $this->config->get('api.versioning.default', '1');
        }
        
        return $this->validateVersion($version);
    }
    
    public function getHandler($version, $endpoint)
    {
        if (!isset($this->versions[$version])) {
            throw new UnsupportedVersionException("API version {$version} is not supported");
        }
        
        $versionConfig = $this->versions[$version];
        
        if (!isset($versionConfig['endpoints'][$endpoint])) {
            throw new EndpointNotFoundException("Endpoint {$endpoint} not found in version {$version}");
        }
        
        return $versionConfig['endpoints'][$endpoint];
    }
    
    public function getSchema($version)
    {
        if (!isset($this->versions[$version])) {
            throw new UnsupportedVersionException("API version {$version} is not supported");
        }
        
        return $this->versions[$version]['schema'];
    }
    
    public function getDeprecationInfo($version)
    {
        if (!isset($this->versions[$version])) {
            return null;
        }
        
        return $this->versions[$version]['deprecation'] ?? null;
    }
    
    private function loadVersions()
    {
        $versions = $this->config->get('api.versioning.versions', []);
        
        foreach ($versions as $version => $config) {
            $this->versions[$version] = $config;
        }
    }
    
    private function validateVersion($version)
    {
        $supportedVersions = array_keys($this->versions);
        
        if (!in_array($version, $supportedVersions)) {
            throw new UnsupportedVersionException("API version {$version} is not supported. Supported versions: " . implode(', ', $supportedVersions));
        }
        
        return $version;
    }
}

class VersionedController
{
    private $versionManager;
    
    public function __construct(APIVersionManager $versionManager)
    {
        $this->versionManager = $versionManager;
    }
    
    public function handleRequest($request)
    {
        $version = $this->versionManager->getVersion($request);
        $endpoint = $this->getEndpoint($request);
        
        $handler = $this->versionManager->getHandler($version, $endpoint);
        
        // Check deprecation
        $deprecation = $this->versionManager->getDeprecationInfo($version);
        if ($deprecation) {
            $this->addDeprecationWarning($request, $deprecation);
        }
        
        return $this->executeHandler($handler, $request);
    }
    
    private function getEndpoint($request)
    {
        $method = $request->getMethod();
        $path = $request->getPathInfo();
        
        // Remove version from path
        $path = preg_replace('/\/v\d+\//', '/', $path);
        
        return $method . ':' . $path;
    }
    
    private function addDeprecationWarning($request, $deprecation)
    {
        $warning = "This API version is deprecated. ";
        
        if (isset($deprecation['message'])) {
            $warning .= $deprecation['message'];
        }
        
        if (isset($deprecation['sunset_date'])) {
            $warning .= " Sunset date: {$deprecation['sunset_date']}";
        }
        
        $request->headers->set('X-API-Deprecation-Warning', $warning);
    }
    
    private function executeHandler($handler, $request)
    {
        if (is_callable($handler)) {
            return call_user_func($handler, $request);
        }
        
        if (is_string($handler)) {
            list($class, $method) = explode('::', $handler);
            $instance = new $class();
            return $instance->$method($request);
        }
        
        throw new \Exception("Invalid handler: " . json_encode($handler));
    }
}
```

## Rate Limiting
```php
<?php

namespace App\API\RateLimiting;

use TuskLang\Config;

class RateLimiter
{
    private $config;
    private $storage;
    
    public function __construct()
    {
        $this->config = new Config();
        $this->storage = $this->getStorage();
    }
    
    public function checkLimit($request, $user = null)
    {
        $key = $this->generateKey($request, $user);
        $limit = $this->getLimit($request, $user);
        $window = $this->getWindow($request, $user);
        
        $current = $this->storage->get($key);
        
        if ($current >= $limit) {
            $retryAfter = $this->calculateRetryAfter($key, $window);
            throw new RateLimitException("Rate limit exceeded", $retryAfter);
        }
        
        $this->storage->increment($key, $window);
    }
    
    public function getRemaining($request, $user = null)
    {
        $key = $this->generateKey($request, $user);
        $limit = $this->getLimit($request, $user);
        $current = $this->storage->get($key);
        
        return max(0, $limit - $current);
    }
    
    public function getResetTime($request, $user = null)
    {
        $key = $this->generateKey($request, $user);
        $window = $this->getWindow($request, $user);
        
        return $this->storage->getExpiry($key);
    }
    
    private function generateKey($request, $user)
    {
        $identifier = $user ? $user->getId() : $request->getClientIp();
        $endpoint = $request->getMethod() . ':' . $request->getPathInfo();
        
        return "rate_limit:{$identifier}:{$endpoint}";
    }
    
    private function getLimit($request, $user)
    {
        $limits = $this->config->get('api.rate_limiting.limits', []);
        
        // Check user-specific limits
        if ($user && isset($limits['users'][$user->getRole()])) {
            return $limits['users'][$user->getRole()];
        }
        
        // Check endpoint-specific limits
        $endpoint = $request->getPathInfo();
        foreach ($limits['endpoints'] as $pattern => $limit) {
            if (preg_match($pattern, $endpoint)) {
                return $limit;
            }
        }
        
        // Default limit
        return $limits['default'] ?? 100;
    }
    
    private function getWindow($request, $user)
    {
        $windows = $this->config->get('api.rate_limiting.windows', []);
        
        // Check user-specific windows
        if ($user && isset($windows['users'][$user->getRole()])) {
            return $windows['users'][$user->getRole()];
        }
        
        // Check endpoint-specific windows
        $endpoint = $request->getPathInfo();
        foreach ($windows['endpoints'] as $pattern => $window) {
            if (preg_match($pattern, $endpoint)) {
                return $window;
            }
        }
        
        // Default window
        return $windows['default'] ?? 3600;
    }
    
    private function calculateRetryAfter($key, $window)
    {
        $expiry = $this->storage->getExpiry($key);
        return max(0, $expiry - time());
    }
    
    private function getStorage()
    {
        $type = $this->config->get('api.rate_limiting.storage', 'redis');
        
        switch ($type) {
            case 'redis':
                return new RedisStorage($this->config);
            case 'database':
                return new DatabaseStorage($this->config);
            case 'memory':
                return new MemoryStorage($this->config);
            default:
                throw new \Exception("Unknown rate limiting storage: {$type}");
        }
    }
}

class RedisStorage
{
    private $redis;
    
    public function __construct($config)
    {
        $this->redis = new Redis();
        $this->redis->connect(
            $config->get('api.rate_limiting.redis.host', 'localhost'),
            $config->get('api.rate_limiting.redis.port', 6379)
        );
    }
    
    public function get($key)
    {
        return (int) $this->redis->get($key);
    }
    
    public function increment($key, $window)
    {
        $this->redis->multi();
        $this->redis->incr($key);
        $this->redis->expire($key, $window);
        $this->redis->exec();
    }
    
    public function getExpiry($key)
    {
        $ttl = $this->redis->ttl($key);
        return $ttl > 0 ? time() + $ttl : time();
    }
}
```

## Best Practices
- **Design RESTful APIs with proper HTTP methods**
- **Use consistent URL patterns**
- **Implement proper error handling**
- **Version your APIs appropriately**
- **Use rate limiting to prevent abuse**
- **Provide comprehensive documentation**

## Performance Optimization
- **Use caching for frequently accessed data**
- **Implement pagination for large datasets**
- **Optimize database queries**
- **Use compression for responses**

## Security Considerations
- **Implement proper authentication**
- **Use HTTPS for all communications**
- **Validate all inputs**
- **Implement rate limiting**

## Troubleshooting
- **Monitor API performance**
- **Check rate limiting logs**
- **Verify authentication**
- **Test API versions**

## Conclusion
TuskLang + PHP = API design that's powerful, scalable, and developer-friendly. Build APIs that developers love to use. 