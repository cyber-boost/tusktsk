# Advanced API Design

TuskLang provides sophisticated API design capabilities that go beyond basic REST endpoints. This guide covers advanced API patterns, versioning strategies, and integration with PHP frameworks.

## Table of Contents

- [RESTful API Design](#restful-api-design)
- [GraphQL Integration](#graphql-integration)
- [API Versioning](#api-versioning)
- [Rate Limiting](#rate-limiting)
- [API Documentation](#api-documentation)
- [API Testing](#api-testing)
- [API Security](#api-security)
- [Performance Optimization](#performance-optimization)
- [Best Practices](#best-practices)

## RESTful API Design

Design comprehensive RESTful APIs with TuskLang:

```php
// config/api.tsk
api_config = {
    version = "v1"
    base_url = "https://api.example.com"
    
    endpoints = {
        users = {
            path = "/users"
            methods = ["GET", "POST", "PUT", "DELETE"]
            authentication = "bearer"
            rate_limit = {
                requests = 100
                window = "1h"
            }
            validation = {
                create = "user_create_schema"
                update = "user_update_schema"
            }
            responses = {
                success = "user_response_schema"
                error = "error_response_schema"
            }
        }
        
        products = {
            path = "/products"
            methods = ["GET", "POST", "PUT", "DELETE"]
            authentication = "bearer"
            rate_limit = {
                requests = 200
                window = "1h"
            }
            pagination = {
                enabled = true
                default_limit = 20
                max_limit = 100
            }
            filtering = {
                enabled = true
                fields = ["name", "category", "price", "status"]
            }
            sorting = {
                enabled = true
                fields = ["name", "price", "created_at"]
                default = "created_at"
                direction = "desc"
            }
        }
    }
    
    middleware = {
        authentication = "bearer_token"
        rate_limiting = "token_bucket"
        logging = "request_response"
        cors = "allow_all"
        compression = "gzip"
    }
}
```

```php
<?php
// app/Controllers/ApiController.php

namespace App\Controllers;

use TuskLang\Api\ApiController;
use TuskLang\Validation\Validator;
use TuskLang\Response\ResponseBuilder;

class UsersController extends ApiController
{
    private Validator $validator;
    private ResponseBuilder $responseBuilder;
    
    public function __construct(Validator $validator, ResponseBuilder $responseBuilder)
    {
        $this->validator = $validator;
        $this->responseBuilder = $responseBuilder;
    }
    
    public function index(): array
    {
        $query = $this->request->query();
        $page = $query['page'] ?? 1;
        $limit = min($query['limit'] ?? 20, 100);
        
        $users = $this->userService->getPaginated($page, $limit);
        
        return $this->responseBuilder
            ->success($users)
            ->pagination($page, $limit, $this->userService->getTotal())
            ->build();
    }
    
    public function show(int $id): array
    {
        $user = $this->userService->find($id);
        
        if (!$user) {
            return $this->responseBuilder
                ->error('User not found', 404)
                ->build();
        }
        
        return $this->responseBuilder
            ->success($user)
            ->build();
    }
    
    public function store(): array
    {
        $data = $this->request->json();
        
        $validation = $this->validator->validate($data, 'user_create_schema');
        
        if (!$validation->isValid()) {
            return $this->responseBuilder
                ->error('Validation failed', 422)
                ->errors($validation->getErrors())
                ->build();
        }
        
        $user = $this->userService->create($data);
        
        return $this->responseBuilder
            ->success($user, 201)
            ->build();
    }
    
    public function update(int $id): array
    {
        $data = $this->request->json();
        
        $validation = $this->validator->validate($data, 'user_update_schema');
        
        if (!$validation->isValid()) {
            return $this->responseBuilder
                ->error('Validation failed', 422)
                ->errors($validation->getErrors())
                ->build();
        }
        
        $user = $this->userService->update($id, $data);
        
        if (!$user) {
            return $this->responseBuilder
                ->error('User not found', 404)
                ->build();
        }
        
        return $this->responseBuilder
            ->success($user)
            ->build();
    }
    
    public function destroy(int $id): array
    {
        $deleted = $this->userService->delete($id);
        
        if (!$deleted) {
            return $this->responseBuilder
                ->error('User not found', 404)
                ->build();
        }
        
        return $this->responseBuilder
            ->success(null, 204)
            ->build();
    }
}
```

## GraphQL Integration

Integrate GraphQL with TuskLang:

```php
<?php
// app/GraphQL/Schema.php

namespace App\GraphQL;

use GraphQL\Type\Definition\ObjectType;
use GraphQL\Type\Definition\Type;
use GraphQL\Type\Schema as GraphQLSchema;

class Schema
{
    private GraphQLSchema $schema;
    private array $config;
    
    public function __construct(array $config)
    {
        $this->config = $config;
        $this->buildSchema();
    }
    
    public function getSchema(): GraphQLSchema
    {
        return $this->schema;
    }
    
    private function buildSchema(): void
    {
        $queryType = new ObjectType([
            'name' => 'Query',
            'fields' => [
                'user' => [
                    'type' => $this->getUserType(),
                    'args' => [
                        'id' => Type::nonNull(Type::int())
                    ],
                    'resolve' => function($root, $args) {
                        return $this->resolveUser($args['id']);
                    }
                ],
                'users' => [
                    'type' => Type::listOf($this->getUserType()),
                    'args' => [
                        'limit' => Type::int(),
                        'offset' => Type::int(),
                        'filter' => $this->getUserFilterType()
                    ],
                    'resolve' => function($root, $args) {
                        return $this->resolveUsers($args);
                    }
                ],
                'product' => [
                    'type' => $this->getProductType(),
                    'args' => [
                        'id' => Type::nonNull(Type::int())
                    ],
                    'resolve' => function($root, $args) {
                        return $this->resolveProduct($args['id']);
                    }
                ]
            ]
        ]);
        
        $mutationType = new ObjectType([
            'name' => 'Mutation',
            'fields' => [
                'createUser' => [
                    'type' => $this->getUserType(),
                    'args' => [
                        'input' => $this->getUserInputType()
                    ],
                    'resolve' => function($root, $args) {
                        return $this->createUser($args['input']);
                    }
                ],
                'updateUser' => [
                    'type' => $this->getUserType(),
                    'args' => [
                        'id' => Type::nonNull(Type::int()),
                        'input' => $this->getUserInputType()
                    ],
                    'resolve' => function($root, $args) {
                        return $this->updateUser($args['id'], $args['input']);
                    }
                ],
                'deleteUser' => [
                    'type' => Type::boolean(),
                    'args' => [
                        'id' => Type::nonNull(Type::int())
                    ],
                    'resolve' => function($root, $args) {
                        return $this->deleteUser($args['id']);
                    }
                ]
            ]
        ]);
        
        $this->schema = new GraphQLSchema([
            'query' => $queryType,
            'mutation' => $mutationType
        ]);
    }
    
    private function getUserType(): ObjectType
    {
        return new ObjectType([
            'name' => 'User',
            'fields' => [
                'id' => Type::int(),
                'name' => Type::string(),
                'email' => Type::string(),
                'created_at' => Type::string(),
                'updated_at' => Type::string(),
                'posts' => [
                    'type' => Type::listOf($this->getPostType()),
                    'resolve' => function($user) {
                        return $this->resolveUserPosts($user['id']);
                    }
                ]
            ]
        ]);
    }
    
    private function getProductType(): ObjectType
    {
        return new ObjectType([
            'name' => 'Product',
            'fields' => [
                'id' => Type::int(),
                'name' => Type::string(),
                'description' => Type::string(),
                'price' => Type::float(),
                'category' => [
                    'type' => $this->getCategoryType(),
                    'resolve' => function($product) {
                        return $this->resolveProductCategory($product['category_id']);
                    }
                ]
            ]
        ]);
    }
    
    private function getUserInputType(): ObjectType
    {
        return new ObjectType([
            'name' => 'UserInput',
            'fields' => [
                'name' => Type::nonNull(Type::string()),
                'email' => Type::nonNull(Type::string()),
                'password' => Type::string()
            ]
        ]);
    }
    
    private function getUserFilterType(): ObjectType
    {
        return new ObjectType([
            'name' => 'UserFilter',
            'fields' => [
                'name' => Type::string(),
                'email' => Type::string(),
                'status' => Type::string()
            ]
        ]);
    }
    
    private function resolveUser(int $id): ?array
    {
        return $this->userService->find($id);
    }
    
    private function resolveUsers(array $args): array
    {
        $limit = $args['limit'] ?? 20;
        $offset = $args['offset'] ?? 0;
        $filter = $args['filter'] ?? [];
        
        return $this->userService->getFiltered($filter, $limit, $offset);
    }
    
    private function resolveProduct(int $id): ?array
    {
        return $this->productService->find($id);
    }
    
    private function createUser(array $input): array
    {
        $validation = $this->validator->validate($input, 'user_create_schema');
        
        if (!$validation->isValid()) {
            throw new \Exception('Validation failed');
        }
        
        return $this->userService->create($input);
    }
    
    private function updateUser(int $id, array $input): array
    {
        $validation = $this->validator->validate($input, 'user_update_schema');
        
        if (!$validation->isValid()) {
            throw new \Exception('Validation failed');
        }
        
        return $this->userService->update($id, $input);
    }
    
    private function deleteUser(int $id): bool
    {
        return $this->userService->delete($id);
    }
    
    private function resolveUserPosts(int $userId): array
    {
        return $this->postService->getByUserId($userId);
    }
    
    private function resolveProductCategory(int $categoryId): ?array
    {
        return $this->categoryService->find($categoryId);
    }
}
```

## API Versioning

Implement API versioning strategies:

```php
<?php
// app/Api/VersionManager.php

namespace App\Api;

class VersionManager
{
    private array $versions = [];
    private array $config;
    
    public function __construct(array $config)
    {
        $this->config = $config;
        $this->initializeVersions();
    }
    
    public function getVersion(string $version): ?array
    {
        return $this->versions[$version] ?? null;
    }
    
    public function getLatestVersion(): string
    {
        return $this->config['latest_version'] ?? 'v1';
    }
    
    public function isVersionSupported(string $version): bool
    {
        return isset($this->versions[$version]);
    }
    
    public function getDeprecatedVersions(): array
    {
        $deprecated = [];
        
        foreach ($this->versions as $version => $config) {
            if ($config['deprecated'] ?? false) {
                $deprecated[] = $version;
            }
        }
        
        return $deprecated;
    }
    
    public function migrateRequest(array $request, string $fromVersion, string $toVersion): array
    {
        if ($fromVersion === $toVersion) {
            return $request;
        }
        
        $migrationPath = $this->getMigrationPath($fromVersion, $toVersion);
        
        foreach ($migrationPath as $migration) {
            $request = $this->applyMigration($request, $migration);
        }
        
        return $request;
    }
    
    public function migrateResponse(array $response, string $fromVersion, string $toVersion): array
    {
        if ($fromVersion === $toVersion) {
            return $response;
        }
        
        $migrationPath = $this->getMigrationPath($toVersion, $fromVersion);
        
        foreach (array_reverse($migrationPath) as $migration) {
            $response = $this->applyMigration($response, $migration);
        }
        
        return $response;
    }
    
    private function initializeVersions(): void
    {
        foreach ($this->config['versions'] as $version => $config) {
            $this->versions[$version] = $config;
        }
    }
    
    private function getMigrationPath(string $fromVersion, string $toVersion): array
    {
        $path = [];
        $currentVersion = $fromVersion;
        
        while ($currentVersion !== $toVersion) {
            $nextVersion = $this->getNextVersion($currentVersion, $toVersion);
            
            if (!$nextVersion) {
                throw new \Exception("No migration path from {$fromVersion} to {$toVersion}");
            }
            
            $path[] = [
                'from' => $currentVersion,
                'to' => $nextVersion
            ];
            
            $currentVersion = $nextVersion;
        }
        
        return $path;
    }
    
    private function getNextVersion(string $currentVersion, string $targetVersion): ?string
    {
        $versionOrder = $this->config['version_order'] ?? [];
        $currentIndex = array_search($currentVersion, $versionOrder);
        $targetIndex = array_search($targetVersion, $versionOrder);
        
        if ($currentIndex === false || $targetIndex === false) {
            return null;
        }
        
        if ($currentIndex < $targetIndex) {
            return $versionOrder[$currentIndex + 1] ?? null;
        } else {
            return $versionOrder[$currentIndex - 1] ?? null;
        }
    }
    
    private function applyMigration(array $data, array $migration): array
    {
        $migrationClass = $this->loadMigrationClass($migration);
        
        if (method_exists($migrationClass, 'migrate')) {
            return $migrationClass->migrate($data);
        }
        
        return $data;
    }
    
    private function loadMigrationClass(array $migration): object
    {
        $className = "App\\Api\\Migrations\\{$migration['from']}To{$migration['to']}Migration";
        
        if (!class_exists($className)) {
            throw new \Exception("Migration class {$className} not found");
        }
        
        return new $className();
    }
}
```

## Rate Limiting

Implement sophisticated rate limiting:

```php
<?php
// app/Api/RateLimiter.php

namespace App\Api;

use Redis;

class RateLimiter
{
    private Redis $redis;
    private array $config;
    
    public function __construct(Redis $redis, array $config)
    {
        $this->redis = $redis;
        $this->config = $config;
    }
    
    public function checkLimit(string $key, int $limit, int $window): bool
    {
        $current = $this->redis->get($key) ?: 0;
        
        if ($current >= $limit) {
            return false;
        }
        
        $this->redis->incr($key);
        $this->redis->expire($key, $window);
        
        return true;
    }
    
    public function checkTokenBucket(string $key, int $capacity, int $refillRate, int $refillTime): bool
    {
        $now = time();
        $lastRefill = $this->redis->get("{$key}:last_refill") ?: $now;
        $tokens = $this->redis->get("{$key}:tokens") ?: $capacity;
        
        $timePassed = $now - $lastRefill;
        $tokensToAdd = ($timePassed / $refillTime) * $refillRate;
        
        $tokens = min($capacity, $tokens + $tokensToAdd);
        
        if ($tokens < 1) {
            return false;
        }
        
        $tokens--;
        
        $this->redis->set("{$key}:tokens", $tokens);
        $this->redis->set("{$key}:last_refill", $now);
        
        return true;
    }
    
    public function checkSlidingWindow(string $key, int $limit, int $window): bool
    {
        $now = time();
        $windowStart = $now - $window;
        
        // Remove old entries
        $this->redis->zremrangebyscore($key, 0, $windowStart);
        
        // Count current requests
        $current = $this->redis->zcard($key);
        
        if ($current >= $limit) {
            return false;
        }
        
        // Add current request
        $this->redis->zadd($key, $now, uniqid());
        $this->redis->expire($key, $window);
        
        return true;
    }
    
    public function getRemainingRequests(string $key): int
    {
        $limit = $this->getLimitForKey($key);
        $current = $this->redis->get($key) ?: 0;
        
        return max(0, $limit - $current);
    }
    
    public function getResetTime(string $key): int
    {
        $ttl = $this->redis->ttl($key);
        return $ttl > 0 ? time() + $ttl : 0;
    }
    
    public function getRateLimitHeaders(string $key): array
    {
        $limit = $this->getLimitForKey($key);
        $remaining = $this->getRemainingRequests($key);
        $reset = $this->getResetTime($key);
        
        return [
            'X-RateLimit-Limit' => $limit,
            'X-RateLimit-Remaining' => $remaining,
            'X-RateLimit-Reset' => $reset
        ];
    }
    
    private function getLimitForKey(string $key): int
    {
        $endpoint = $this->extractEndpointFromKey($key);
        $config = $this->config['endpoints'][$endpoint] ?? [];
        
        return $config['rate_limit']['requests'] ?? 100;
    }
    
    private function extractEndpointFromKey(string $key): string
    {
        $parts = explode(':', $key);
        return $parts[1] ?? 'default';
    }
}
```

## API Documentation

Generate comprehensive API documentation:

```php
<?php
// app/Api/DocumentationGenerator.php

namespace App\Api;

class DocumentationGenerator
{
    private array $config;
    private array $schemas;
    
    public function __construct(array $config, array $schemas)
    {
        $this->config = $config;
        $this->schemas = $schemas;
    }
    
    public function generateOpenApiSpec(): array
    {
        return [
            'openapi' => '3.0.0',
            'info' => [
                'title' => $this->config['title'] ?? 'API Documentation',
                'version' => $this->config['version'] ?? '1.0.0',
                'description' => $this->config['description'] ?? ''
            ],
            'servers' => [
                ['url' => $this->config['base_url'] ?? 'https://api.example.com']
            ],
            'paths' => $this->generatePaths(),
            'components' => [
                'schemas' => $this->generateSchemas(),
                'securitySchemes' => $this->generateSecuritySchemes()
            ]
        ];
    }
    
    public function generateMarkdown(): string
    {
        $markdown = "# API Documentation\n\n";
        $markdown .= "Base URL: {$this->config['base_url']}\n\n";
        
        foreach ($this->config['endpoints'] as $name => $endpoint) {
            $markdown .= $this->generateEndpointMarkdown($name, $endpoint);
        }
        
        return $markdown;
    }
    
    private function generatePaths(): array
    {
        $paths = [];
        
        foreach ($this->config['endpoints'] as $name => $endpoint) {
            $path = $endpoint['path'];
            
            foreach ($endpoint['methods'] as $method) {
                $method = strtolower($method);
                
                if (!isset($paths[$path])) {
                    $paths[$path] = [];
                }
                
                $paths[$path][$method] = $this->generateOperation($name, $endpoint, $method);
            }
        }
        
        return $paths;
    }
    
    private function generateOperation(string $name, array $endpoint, string $method): array
    {
        $operation = [
            'summary' => ucfirst($method) . ' ' . $name,
            'tags' => [$name],
            'responses' => $this->generateResponses($endpoint)
        ];
        
        if (in_array($method, ['post', 'put', 'patch'])) {
            $operation['requestBody'] = $this->generateRequestBody($endpoint);
        }
        
        if ($endpoint['authentication'] ?? false) {
            $operation['security'] = [
                ['bearerAuth' => []]
            ];
        }
        
        return $operation;
    }
    
    private function generateResponses(array $endpoint): array
    {
        $responses = [
            '200' => [
                'description' => 'Success',
                'content' => [
                    'application/json' => [
                        'schema' => [
                            '$ref' => "#/components/schemas/{$endpoint['responses']['success']}"
                        ]
                    ]
                ]
            ],
            '400' => [
                'description' => 'Bad Request',
                'content' => [
                    'application/json' => [
                        'schema' => [
                            '$ref' => "#/components/schemas/{$endpoint['responses']['error']}"
                        ]
                    ]
                ]
            ],
            '401' => [
                'description' => 'Unauthorized'
            ],
            '404' => [
                'description' => 'Not Found'
            ],
            '422' => [
                'description' => 'Validation Error',
                'content' => [
                    'application/json' => [
                        'schema' => [
                            '$ref' => "#/components/schemas/{$endpoint['responses']['error']}"
                        ]
                    ]
                ]
            ]
        ];
        
        return $responses;
    }
    
    private function generateRequestBody(array $endpoint): array
    {
        $schema = $endpoint['validation']['create'] ?? $endpoint['validation']['update'] ?? null;
        
        if (!$schema) {
            return [];
        }
        
        return [
            'required' => true,
            'content' => [
                'application/json' => [
                    'schema' => [
                        '$ref' => "#/components/schemas/{$schema}"
                    ]
                ]
            ]
        ];
    }
    
    private function generateSchemas(): array
    {
        return $this->schemas;
    }
    
    private function generateSecuritySchemes(): array
    {
        return [
            'bearerAuth' => [
                'type' => 'http',
                'scheme' => 'bearer',
                'bearerFormat' => 'JWT'
            ]
        ];
    }
    
    private function generateEndpointMarkdown(string $name, array $endpoint): string
    {
        $markdown = "## {$name}\n\n";
        $markdown .= "**Path:** `{$endpoint['path']}`\n\n";
        $markdown .= "**Methods:** " . implode(', ', $endpoint['methods']) . "\n\n";
        
        if ($endpoint['authentication'] ?? false) {
            $markdown .= "**Authentication:** Required\n\n";
        }
        
        if ($endpoint['rate_limit'] ?? false) {
            $rateLimit = $endpoint['rate_limit'];
            $markdown .= "**Rate Limit:** {$rateLimit['requests']} requests per {$rateLimit['window']}\n\n";
        }
        
        foreach ($endpoint['methods'] as $method) {
            $markdown .= $this->generateMethodMarkdown($name, $endpoint, $method);
        }
        
        return $markdown;
    }
    
    private function generateMethodMarkdown(string $name, array $endpoint, string $method): string
    {
        $markdown = "### {$method} {$endpoint['path']}\n\n";
        
        if (in_array($method, ['POST', 'PUT', 'PATCH'])) {
            $schema = $endpoint['validation']['create'] ?? $endpoint['validation']['update'] ?? null;
            
            if ($schema) {
                $markdown .= "**Request Body:**\n\n";
                $markdown .= "```json\n";
                $markdown .= json_encode($this->schemas[$schema], JSON_PRETTY_PRINT);
                $markdown .= "\n```\n\n";
            }
        }
        
        $markdown .= "**Response:**\n\n";
        $markdown .= "```json\n";
        $markdown .= json_encode($this->schemas[$endpoint['responses']['success']], JSON_PRETTY_PRINT);
        $markdown .= "\n```\n\n";
        
        return $markdown;
    }
}
```

## API Testing

Implement comprehensive API testing:

```php
<?php
// app/Tests/ApiTestCase.php

namespace App\Tests;

use PHPUnit\Framework\TestCase;
use TuskLang\Testing\ApiTestClient;

class ApiTestCase extends TestCase
{
    protected ApiTestClient $client;
    protected array $config;
    
    protected function setUp(): void
    {
        parent::setUp();
        
        $this->config = TuskLang::load('api_config');
        $this->client = new ApiTestClient($this->config);
    }
    
    protected function assertApiResponse(array $response, int $expectedStatus = 200): void
    {
        $this->assertEquals($expectedStatus, $response['status']);
        $this->assertArrayHasKey('data', $response);
    }
    
    protected function assertApiError(array $response, int $expectedStatus = 400): void
    {
        $this->assertEquals($expectedStatus, $response['status']);
        $this->assertArrayHasKey('error', $response);
    }
    
    protected function assertValidationError(array $response, array $expectedErrors): void
    {
        $this->assertEquals(422, $response['status']);
        $this->assertArrayHasKey('errors', $response);
        
        foreach ($expectedErrors as $field => $message) {
            $this->assertArrayHasKey($field, $response['errors']);
            $this->assertContains($message, $response['errors'][$field]);
        }
    }
    
    protected function assertRateLimited(array $response): void
    {
        $this->assertEquals(429, $response['status']);
        $this->assertArrayHasKey('error', $response);
        $this->assertStringContainsString('rate limit', $response['error']);
    }
    
    protected function assertUnauthorized(array $response): void
    {
        $this->assertEquals(401, $response['status']);
        $this->assertArrayHasKey('error', $response);
    }
    
    protected function assertForbidden(array $response): void
    {
        $this->assertEquals(403, $response['status']);
        $this->assertArrayHasKey('error', $response);
    }
    
    protected function assertNotFound(array $response): void
    {
        $this->assertEquals(404, $response['status']);
        $this->assertArrayHasKey('error', $response);
    }
    
    protected function getAuthToken(): string
    {
        $response = $this->client->post('/auth/login', [
            'email' => 'test@example.com',
            'password' => 'password'
        ]);
        
        return $response['data']['token'];
    }
    
    protected function createTestUser(): array
    {
        $userData = [
            'name' => 'Test User',
            'email' => 'test' . uniqid() . '@example.com',
            'password' => 'password123'
        ];
        
        $response = $this->client->post('/users', $userData);
        return $response['data'];
    }
    
    protected function createTestProduct(): array
    {
        $productData = [
            'name' => 'Test Product',
            'description' => 'Test Description',
            'price' => 99.99,
            'category_id' => 1
        ];
        
        $response = $this->client->post('/products', $productData);
        return $response['data'];
    }
}
```

## API Security

Implement comprehensive API security:

```php
<?php
// app/Api/SecurityManager.php

namespace App\Api;

class SecurityManager
{
    private array $config;
    private array $blacklist = [];
    private array $whitelist = [];
    
    public function __construct(array $config)
    {
        $this->config = $config;
        $this->loadSecurityLists();
    }
    
    public function validateRequest(array $request): array
    {
        $validation = [
            'valid' => true,
            'errors' => []
        ];
        
        // Check IP blacklist
        if ($this->isIpBlacklisted($request['ip'])) {
            $validation['valid'] = false;
            $validation['errors'][] = 'IP address is blacklisted';
        }
        
        // Check request size
        if ($this->isRequestTooLarge($request['size'])) {
            $validation['valid'] = false;
            $validation['errors'][] = 'Request too large';
        }
        
        // Check for suspicious patterns
        if ($this->hasSuspiciousPatterns($request['body'])) {
            $validation['valid'] = false;
            $validation['errors'][] = 'Suspicious request patterns detected';
        }
        
        // Validate authentication
        if ($this->requiresAuthentication($request['endpoint'])) {
            if (!$this->validateAuthentication($request['headers'])) {
                $validation['valid'] = false;
                $validation['errors'][] = 'Authentication required';
            }
        }
        
        return $validation;
    }
    
    public function sanitizeInput(array $input): array
    {
        $sanitized = [];
        
        foreach ($input as $key => $value) {
            if (is_string($value)) {
                $sanitized[$key] = $this->sanitizeString($value);
            } elseif (is_array($value)) {
                $sanitized[$key] = $this->sanitizeInput($value);
            } else {
                $sanitized[$key] = $value;
            }
        }
        
        return $sanitized;
    }
    
    public function validateCors(string $origin): bool
    {
        $allowedOrigins = $this->config['cors']['allowed_origins'] ?? [];
        
        if (empty($allowedOrigins)) {
            return true;
        }
        
        return in_array($origin, $allowedOrigins);
    }
    
    public function generateCsrfToken(): string
    {
        return bin2hex(random_bytes(32));
    }
    
    public function validateCsrfToken(string $token, string $sessionToken): bool
    {
        return hash_equals($sessionToken, $token);
    }
    
    public function logSecurityEvent(string $event, array $context = []): void
    {
        $logEntry = [
            'timestamp' => date('Y-m-d H:i:s'),
            'event' => $event,
            'context' => $context
        ];
        
        error_log(json_encode($logEntry));
    }
    
    private function loadSecurityLists(): void
    {
        $this->blacklist = $this->config['security']['blacklist'] ?? [];
        $this->whitelist = $this->config['security']['whitelist'] ?? [];
    }
    
    private function isIpBlacklisted(string $ip): bool
    {
        return in_array($ip, $this->blacklist['ips'] ?? []);
    }
    
    private function isRequestTooLarge(int $size): bool
    {
        $maxSize = $this->config['security']['max_request_size'] ?? 1048576; // 1MB
        return $size > $maxSize;
    }
    
    private function hasSuspiciousPatterns(string $body): bool
    {
        $patterns = [
            '/<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>/mi',
            '/javascript:/i',
            '/on\w+\s*=/i',
            '/union\s+select/i',
            '/drop\s+table/i'
        ];
        
        foreach ($patterns as $pattern) {
            if (preg_match($pattern, $body)) {
                return true;
            }
        }
        
        return false;
    }
    
    private function requiresAuthentication(string $endpoint): bool
    {
        $publicEndpoints = $this->config['security']['public_endpoints'] ?? [];
        return !in_array($endpoint, $publicEndpoints);
    }
    
    private function validateAuthentication(array $headers): bool
    {
        $token = $headers['Authorization'] ?? '';
        
        if (empty($token)) {
            return false;
        }
        
        if (!preg_match('/^Bearer\s+(.+)$/', $token, $matches)) {
            return false;
        }
        
        return $this->validateJwtToken($matches[1]);
    }
    
    private function validateJwtToken(string $token): bool
    {
        try {
            $decoded = JWT::decode($token, $this->config['security']['jwt_secret'], ['HS256']);
            return !empty($decoded);
        } catch (\Exception $e) {
            return false;
        }
    }
    
    private function sanitizeString(string $value): string
    {
        // Remove HTML tags
        $value = strip_tags($value);
        
        // Convert special characters
        $value = htmlspecialchars($value, ENT_QUOTES, 'UTF-8');
        
        // Remove null bytes
        $value = str_replace("\0", '', $value);
        
        return $value;
    }
}
```

## Performance Optimization

Optimize API performance:

```php
<?php
// app/Api/PerformanceOptimizer.php

namespace App\Api;

class PerformanceOptimizer
{
    private array $config;
    private array $cache;
    
    public function __construct(array $config)
    {
        $this->config = $config;
        $this->cache = [];
    }
    
    public function optimizeResponse(array $response): array
    {
        // Compress response if enabled
        if ($this->config['compression']['enabled'] ?? false) {
            $response = $this->compressResponse($response);
        }
        
        // Add performance headers
        $response['headers'] = array_merge(
            $response['headers'] ?? [],
            $this->getPerformanceHeaders()
        );
        
        return $response;
    }
    
    public function cacheResponse(string $key, array $response, int $ttl = 3600): void
    {
        $this->cache[$key] = [
            'response' => $response,
            'expires' => time() + $ttl
        ];
    }
    
    public function getCachedResponse(string $key): ?array
    {
        if (isset($this->cache[$key])) {
            $cached = $this->cache[$key];
            
            if ($cached['expires'] > time()) {
                return $cached['response'];
            } else {
                unset($this->cache[$key]);
            }
        }
        
        return null;
    }
    
    public function optimizeQuery(string $sql): string
    {
        // Add query hints
        if ($this->config['query_optimization']['add_hints'] ?? false) {
            $sql = $this->addQueryHints($sql);
        }
        
        // Limit results
        if ($this->config['query_optimization']['limit_results'] ?? false) {
            $sql = $this->addDefaultLimit($sql);
        }
        
        return $sql;
    }
    
    public function getPerformanceMetrics(): array
    {
        return [
            'response_time' => $this->getResponseTime(),
            'memory_usage' => memory_get_usage(true),
            'peak_memory' => memory_get_peak_usage(true),
            'cache_hit_rate' => $this->getCacheHitRate()
        ];
    }
    
    private function compressResponse(array $response): array
    {
        if (isset($response['body']) && is_string($response['body'])) {
            $response['body'] = gzencode($response['body'], 9);
            $response['headers']['Content-Encoding'] = 'gzip';
        }
        
        return $response;
    }
    
    private function getPerformanceHeaders(): array
    {
        return [
            'X-Response-Time' => $this->getResponseTime() . 'ms',
            'X-Cache-Hit' => $this->isCacheHit() ? 'true' : 'false',
            'X-Memory-Usage' => memory_get_usage(true)
        ];
    }
    
    private function addQueryHints(string $sql): string
    {
        if (preg_match('/SELECT\s+/i', $sql)) {
            $sql = preg_replace('/SELECT\s+/i', 'SELECT SQL_CALC_FOUND_ROWS ', $sql, 1);
        }
        
        return $sql;
    }
    
    private function addDefaultLimit(string $sql): string
    {
        if (!preg_match('/LIMIT\s+\d+/i', $sql)) {
            $limit = $this->config['query_optimization']['default_limit'] ?? 100;
            $sql .= " LIMIT {$limit}";
        }
        
        return $sql;
    }
    
    private function getResponseTime(): float
    {
        $startTime = $_SERVER['REQUEST_TIME_FLOAT'] ?? microtime(true);
        return round((microtime(true) - $startTime) * 1000, 2);
    }
    
    private function isCacheHit(): bool
    {
        return !empty($this->cache);
    }
    
    private function getCacheHitRate(): float
    {
        // This would typically track cache hits vs misses
        return 0.85; // Example value
    }
}
```

## Best Practices

Follow these best practices for API design:

```php
<?php
// config/api-best-practices.tsk

api_best_practices = {
    design_principles = {
        restful = true
        stateless = true
        cacheable = true
        layered_system = true
        uniform_interface = true
    }
    
    response_format = {
        consistent_structure = true
        include_status_codes = true
        provide_error_details = true
        use_standard_headers = true
    }
    
    versioning = {
        use_url_versioning = true
        maintain_backward_compatibility = true
        deprecate_gracefully = true
        provide_migration_paths = true
    }
    
    security = {
        use_https = true
        implement_authentication = true
        validate_input = true
        sanitize_output = true
        rate_limiting = true
        cors_policy = true
    }
    
    performance = {
        enable_caching = true
        compress_responses = true
        paginate_results = true
        optimize_queries = true
        use_cdn = true
    }
    
    documentation = {
        auto_generate = true
        include_examples = true
        provide_schemas = true
        interactive_docs = true
    }
    
    testing = {
        unit_tests = true
        integration_tests = true
        load_tests = true
        security_tests = true
    }
    
    monitoring = {
        track_metrics = true
        log_requests = true
        alert_on_errors = true
        performance_monitoring = true
    }
}

// Example usage in PHP
class ApiBestPractices
{
    public function implementBestPractices(): void
    {
        // 1. Use consistent response format
        $response = $this->formatResponse($data, $statusCode);
        
        // 2. Implement proper error handling
        $this->handleErrors($exception);
        
        // 3. Add security headers
        $this->addSecurityHeaders($response);
        
        // 4. Enable caching
        $this->cacheResponse($key, $response);
        
        // 5. Compress response
        $this->compressResponse($response);
        
        // 6. Log request
        $this->logRequest($request, $response);
        
        // 7. Track metrics
        $this->trackMetrics($request, $response);
    }
    
    private function formatResponse($data, int $statusCode): array
    {
        return [
            'status' => $statusCode,
            'data' => $data,
            'timestamp' => date('c'),
            'version' => 'v1'
        ];
    }
    
    private function handleErrors(\Exception $exception): array
    {
        return [
            'status' => 500,
            'error' => $exception->getMessage(),
            'timestamp' => date('c')
        ];
    }
    
    private function addSecurityHeaders(array &$response): void
    {
        $response['headers'] = array_merge($response['headers'] ?? [], [
            'X-Content-Type-Options' => 'nosniff',
            'X-Frame-Options' => 'DENY',
            'X-XSS-Protection' => '1; mode=block',
            'Strict-Transport-Security' => 'max-age=31536000; includeSubDomains'
        ]);
    }
    
    private function cacheResponse(string $key, array $response): void
    {
        // Implementation depends on your caching system
    }
    
    private function compressResponse(array &$response): void
    {
        // Implementation depends on your compression strategy
    }
    
    private function logRequest(array $request, array $response): void
    {
        // Implementation depends on your logging system
    }
    
    private function trackMetrics(array $request, array $response): void
    {
        // Implementation depends on your metrics system
    }
}
```

This comprehensive guide covers advanced API design patterns in TuskLang with PHP integration. The API system is designed to be scalable, secure, and performant while maintaining the rebellious spirit of TuskLang development. 