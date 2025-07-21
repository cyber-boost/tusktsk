# Advanced API Design in PHP with TuskLang

## Overview

TuskLang revolutionizes API design by making it configuration-driven, intelligent, and adaptive. This guide covers advanced API design patterns that leverage TuskLang's dynamic capabilities for optimal performance, security, and developer experience.

## 🎯 API Architecture Patterns

### API Configuration

```ini
# api-architecture.tsk
[api_architecture]
version = "v2"
base_url = @env("API_BASE_URL", "https://api.example.com")
timeout = 30
retry_attempts = 3

[api_architecture.patterns]
rest = {
    enabled = true,
    versioning = "url",
    pagination = "cursor_based",
    filtering = "query_params"
}

graphql = {
    enabled = true,
    introspection = true,
    subscriptions = true,
    rate_limiting = true
}

grpc = {
    enabled = false,
    protobuf_version = "3",
    streaming = true
}

[api_architecture.layers]
presentation = "controllers"
business = "services"
data = "repositories"
infrastructure = "adapters"

[api_architecture.responses]
format = "json"
compression = true
caching = true
etags = true
```

### API Manager Implementation

```php
<?php
// APIManager.php
class APIManager
{
    private $config;
    private $router;
    private $middleware;
    private $rateLimiter;
    private $cache;
    
    public function __construct()
    {
        $this->config = new TuskConfig('api-architecture.tsk');
        $this->router = new APIRouter();
        $this->middleware = new MiddlewareStack();
        $this->rateLimiter = new RateLimiter();
        $this->cache = new APICache();
        $this->initializeAPI();
    }
    
    private function initializeAPI()
    {
        // Register API patterns
        if ($this->config->get('api_architecture.patterns.rest.enabled')) {
            $this->registerRESTPattern();
        }
        
        if ($this->config->get('api_architecture.patterns.graphql.enabled')) {
            $this->registerGraphQLPattern();
        }
        
        if ($this->config->get('api_architecture.patterns.grpc.enabled')) {
            $this->registerGRPCPattern();
        }
    }
    
    public function handleRequest($request)
    {
        $startTime = microtime(true);
        
        try {
            // Apply middleware
            $request = $this->middleware->processRequest($request);
            
            // Rate limiting
            if (!$this->rateLimiter->allowRequest($request)) {
                return $this->createRateLimitResponse();
            }
            
            // Route request
            $route = $this->router->route($request);
            
            // Check cache
            $cachedResponse = $this->cache->get($request);
            if ($cachedResponse) {
                return $cachedResponse;
            }
            
            // Execute handler
            $response = $this->executeHandler($route, $request);
            
            // Cache response
            $this->cache->set($request, $response);
            
            // Apply response middleware
            $response = $this->middleware->processResponse($response);
            
            $duration = (microtime(true) - $startTime) * 1000;
            $this->recordMetrics($request, $response, $duration);
            
            return $response;
            
        } catch (Exception $e) {
            return $this->handleError($e, $request);
        }
    }
    
    private function executeHandler($route, $request)
    {
        $handler = $route['handler'];
        $params = $route['params'];
        
        // Dependency injection
        $dependencies = $this->resolveDependencies($handler);
        
        // Execute with context
        $context = [
            'request' => $request,
            'user' => $this->getCurrentUser(),
            'timestamp' => time()
        ];
        
        return call_user_func_array($handler, array_merge($dependencies, [$context]));
    }
}
```

## 🔄 RESTful API Design

### REST API Configuration

```ini
# rest-api.tsk
[rest_api]
version = "v2"
base_path = "/api/v2"
content_type = "application/json"

[rest_api.resources]
users = {
    path = "/users",
    methods = ["GET", "POST", "PUT", "DELETE", "PATCH"],
    pagination = true,
    filtering = true,
    sorting = true
}

products = {
    path = "/products",
    methods = ["GET", "POST", "PUT", "DELETE"],
    pagination = true,
    filtering = true,
    search = true
}

orders = {
    path = "/orders",
    methods = ["GET", "POST", "PUT"],
    pagination = true,
    filtering = true,
    webhooks = true
}

[rest_api.pagination]
strategy = "cursor_based"
page_size = 20
max_page_size = 100

[rest_api.filtering]
operators = ["eq", "ne", "gt", "gte", "lt", "lte", "in", "nin", "like", "regex"]
case_sensitive = false
```

### REST API Implementation

```php
class RESTAPI
{
    private $config;
    private $resources = [];
    
    public function __construct()
    {
        $this->config = new TuskConfig('rest-api.tsk');
        $this->initializeResources();
    }
    
    private function initializeResources()
    {
        $resources = $this->config->get('rest_api.resources');
        
        foreach ($resources as $resourceName => $resourceConfig) {
            $this->resources[$resourceName] = new RESTResource($resourceName, $resourceConfig);
        }
    }
    
    public function handleRequest($request)
    {
        $path = $request->getPath();
        $method = $request->getMethod();
        
        // Parse resource from path
        $resource = $this->parseResource($path);
        
        if (!isset($this->resources[$resource])) {
            return $this->createNotFoundResponse();
        }
        
        $resourceHandler = $this->resources[$resource];
        
        // Parse query parameters
        $queryParams = $this->parseQueryParams($request);
        
        // Apply pagination
        if ($resourceHandler->supportsPagination()) {
            $queryParams = $this->applyPagination($queryParams);
        }
        
        // Apply filtering
        if ($resourceHandler->supportsFiltering()) {
            $queryParams = $this->applyFiltering($queryParams);
        }
        
        // Apply sorting
        if ($resourceHandler->supportsSorting()) {
            $queryParams = $this->applySorting($queryParams);
        }
        
        // Execute resource method
        return $resourceHandler->execute($method, $request, $queryParams);
    }
    
    private function parseResource($path)
    {
        $pathParts = explode('/', trim($path, '/'));
        return $pathParts[1] ?? null; // /api/v2/users -> users
    }
    
    private function parseQueryParams($request)
    {
        $params = $request->getQueryParams();
        
        // Parse filtering
        $filters = [];
        foreach ($params as $key => $value) {
            if (strpos($key, 'filter[') === 0) {
                $field = substr($key, 7, -1); // filter[field] -> field
                $filters[$field] = $value;
            }
        }
        
        // Parse sorting
        $sorting = [];
        if (isset($params['sort'])) {
            $sortFields = explode(',', $params['sort']);
            foreach ($sortFields as $field) {
                $direction = 'asc';
                if (strpos($field, '-') === 0) {
                    $direction = 'desc';
                    $field = substr($field, 1);
                }
                $sorting[$field] = $direction;
            }
        }
        
        return [
            'filters' => $filters,
            'sorting' => $sorting,
            'pagination' => $this->parsePagination($params),
            'search' => $params['search'] ?? null
        ];
    }
    
    private function applyPagination($queryParams)
    {
        $paginationConfig = $this->config->get('rest_api.pagination');
        $strategy = $paginationConfig['strategy'];
        
        if ($strategy === 'cursor_based') {
            $queryParams['pagination'] = [
                'cursor' => $queryParams['pagination']['cursor'] ?? null,
                'limit' => min(
                    $queryParams['pagination']['limit'] ?? $paginationConfig['page_size'],
                    $paginationConfig['max_page_size']
                )
            ];
        }
        
        return $queryParams;
    }
    
    private function applyFiltering($queryParams)
    {
        $operators = $this->config->get('rest_api.filtering.operators');
        $caseSensitive = $this->config->get('rest_api.filtering.case_sensitive');
        
        $processedFilters = [];
        
        foreach ($queryParams['filters'] as $field => $value) {
            $processedFilters[$field] = $this->processFilter($field, $value, $operators, $caseSensitive);
        }
        
        $queryParams['filters'] = $processedFilters;
        return $queryParams;
    }
}

class RESTResource
{
    private $name;
    private $config;
    private $service;
    
    public function __construct($name, $config)
    {
        $this->name = $name;
        $this->config = $config;
        $this->service = $this->createService($name);
    }
    
    public function execute($method, $request, $queryParams)
    {
        $supportedMethods = $this->config['methods'];
        
        if (!in_array($method, $supportedMethods)) {
            return $this->createMethodNotAllowedResponse($supportedMethods);
        }
        
        switch ($method) {
            case 'GET':
                return $this->handleGet($request, $queryParams);
            case 'POST':
                return $this->handlePost($request);
            case 'PUT':
                return $this->handlePut($request);
            case 'DELETE':
                return $this->handleDelete($request);
            case 'PATCH':
                return $this->handlePatch($request);
        }
    }
    
    private function handleGet($request, $queryParams)
    {
        $id = $this->extractId($request);
        
        if ($id) {
            // Get single resource
            $resource = $this->service->findById($id);
            return $this->createResponse($resource, 200);
        } else {
            // Get collection
            $resources = $this->service->findAll($queryParams);
            return $this->createPaginatedResponse($resources, $queryParams);
        }
    }
    
    private function handlePost($request)
    {
        $data = $request->getBody();
        $resource = $this->service->create($data);
        
        return $this->createResponse($resource, 201, [
            'Location' => "/api/v2/{$this->name}/{$resource['id']}"
        ]);
    }
    
    private function handlePut($request)
    {
        $id = $this->extractId($request);
        $data = $request->getBody();
        
        $resource = $this->service->update($id, $data);
        return $this->createResponse($resource, 200);
    }
    
    private function handleDelete($request)
    {
        $id = $this->extractId($request);
        $this->service->delete($id);
        
        return $this->createResponse(null, 204);
    }
    
    private function handlePatch($request)
    {
        $id = $this->extractId($request);
        $data = $request->getBody();
        
        $resource = $this->service->patch($id, $data);
        return $this->createResponse($resource, 200);
    }
}
```

## 🧠 GraphQL API Design

### GraphQL Configuration

```ini
# graphql-api.tsk
[graphql_api]
enabled = true
endpoint = "/graphql"
introspection = true
playground = true

[graphql_api.schema]
auto_generate = true
cache_schema = true
validation_level = "strict"

[graphql_api.resolvers]
users = "UserResolver"
products = "ProductResolver"
orders = "OrderResolver"

[graphql_api.subscriptions]
enabled = true
transport = "websocket"
endpoint = "/graphql/subscriptions"

[graphql_api.security]
depth_limit = 10
complexity_limit = 1000
rate_limiting = true
```

### GraphQL Implementation

```php
class GraphQLAPI
{
    private $config;
    private $schema;
    private $resolvers = [];
    
    public function __construct()
    {
        $this->config = new TuskConfig('graphql-api.tsk');
        $this->initializeSchema();
        $this->initializeResolvers();
    }
    
    private function initializeSchema()
    {
        if ($this->config->get('graphql_api.schema.auto_generate')) {
            $this->schema = $this->generateSchema();
        } else {
            $this->schema = $this->loadSchema();
        }
    }
    
    private function generateSchema()
    {
        $schemaBuilder = new SchemaBuilder();
        
        // Add types
        $schemaBuilder->addType('User', [
            'id' => 'ID!',
            'email' => 'String!',
            'firstName' => 'String',
            'lastName' => 'String',
            'orders' => '[Order!]!'
        ]);
        
        $schemaBuilder->addType('Product', [
            'id' => 'ID!',
            'name' => 'String!',
            'price' => 'Float!',
            'category' => 'Category!'
        ]);
        
        $schemaBuilder->addType('Order', [
            'id' => 'ID!',
            'user' => 'User!',
            'products' => '[OrderItem!]!',
            'total' => 'Float!',
            'status' => 'OrderStatus!'
        ]);
        
        // Add queries
        $schemaBuilder->addQuery('users', 'User', ['limit' => 'Int', 'offset' => 'Int']);
        $schemaBuilder->addQuery('user', 'User', ['id' => 'ID!']);
        $schemaBuilder->addQuery('products', 'Product', ['category' => 'String']);
        $schemaBuilder->addQuery('orders', 'Order', ['userId' => 'ID!']);
        
        // Add mutations
        $schemaBuilder->addMutation('createUser', 'User', ['input' => 'UserInput!']);
        $schemaBuilder->addMutation('updateUser', 'User', ['id' => 'ID!', 'input' => 'UserInput!']);
        $schemaBuilder->addMutation('createOrder', 'Order', ['input' => 'OrderInput!']);
        
        return $schemaBuilder->build();
    }
    
    public function executeQuery($query, $variables = [], $context = [])
    {
        // Validate query complexity
        $this->validateComplexity($query);
        
        // Apply rate limiting
        if ($this->config->get('graphql_api.security.rate_limiting')) {
            $this->checkRateLimit($context);
        }
        
        // Execute query
        $result = $this->executeGraphQLQuery($query, $variables, $context);
        
        // Handle errors
        if (!empty($result['errors'])) {
            $result['errors'] = $this->sanitizeErrors($result['errors']);
        }
        
        return $result;
    }
    
    private function executeGraphQLQuery($query, $variables, $context)
    {
        $executor = new GraphQLExecutor($this->schema, $this->resolvers);
        
        return $executor->execute($query, $variables, $context);
    }
    
    private function validateComplexity($query)
    {
        $depthLimit = $this->config->get('graphql_api.security.depth_limit');
        $complexityLimit = $this->config->get('graphql_api.security.complexity_limit');
        
        $analyzer = new QueryAnalyzer();
        $depth = $analyzer->getQueryDepth($query);
        $complexity = $analyzer->getQueryComplexity($query);
        
        if ($depth > $depthLimit) {
            throw new GraphQLException("Query depth limit exceeded: {$depth} > {$depthLimit}");
        }
        
        if ($complexity > $complexityLimit) {
            throw new GraphQLException("Query complexity limit exceeded: {$complexity} > {$complexityLimit}");
        }
    }
}

class GraphQLResolver
{
    private $service;
    
    public function __construct($service)
    {
        $this->service = $service;
    }
    
    public function resolve($field, $parent, $args, $context)
    {
        $method = "resolve" . ucfirst($field);
        
        if (method_exists($this, $method)) {
            return $this->$method($parent, $args, $context);
        }
        
        // Default resolution
        return $parent[$field] ?? null;
    }
    
    public function resolveUsers($parent, $args, $context)
    {
        $limit = $args['limit'] ?? 20;
        $offset = $args['offset'] ?? 0;
        
        return $this->service->findAll(['limit' => $limit, 'offset' => $offset]);
    }
    
    public function resolveUser($parent, $args, $context)
    {
        $id = $args['id'];
        return $this->service->findById($id);
    }
    
    public function resolveOrders($parent, $args, $context)
    {
        $userId = $args['userId'];
        return $this->service->findOrdersByUserId($userId);
    }
}
```

## 🔄 API Versioning and Evolution

### API Versioning Configuration

```ini
# api-versioning.tsk
[api_versioning]
strategy = "url_based"
current_version = "v2"
supported_versions = ["v1", "v2", "v3"]
deprecation_policy = "graceful"

[api_versioning.migration]
auto_migrate = true
migration_endpoints = true
version_compatibility = true

[api_versioning.deprecation]
v1 = {
    deprecated_since = "2024-01-01",
    sunset_date = "2024-12-31",
    migration_guide = "https://docs.example.com/migrate-v1-to-v2"
}

[api_versioning.changes]
v1_to_v2 = [
    { type = "field_renamed", old = "user_name", new = "username" },
    { type = "field_removed", field = "legacy_id" },
    { type = "field_added", field = "email_verified", default = false }
]
```

### API Versioning Implementation

```php
class APIVersioning
{
    private $config;
    private $migrators = [];
    
    public function __construct()
    {
        $this->config = new TuskConfig('api-versioning.tsk');
        $this->initializeMigrators();
    }
    
    public function handleVersionedRequest($request)
    {
        $version = $this->extractVersion($request);
        
        // Validate version
        if (!$this->isVersionSupported($version)) {
            return $this->createUnsupportedVersionResponse($version);
        }
        
        // Check deprecation
        if ($this->isVersionDeprecated($version)) {
            $this->addDeprecationWarning($request);
        }
        
        // Migrate request if needed
        $migratedRequest = $this->migrateRequest($request, $version);
        
        // Execute request
        $response = $this->executeRequest($migratedRequest);
        
        // Migrate response back
        $migratedResponse = $this->migrateResponse($response, $version);
        
        return $migratedResponse;
    }
    
    private function extractVersion($request)
    {
        $path = $request->getPath();
        $pathParts = explode('/', trim($path, '/'));
        
        // Extract version from URL: /api/v2/users -> v2
        if (isset($pathParts[1]) && strpos($pathParts[1], 'v') === 0) {
            return $pathParts[1];
        }
        
        // Extract from header
        $version = $request->getHeader('API-Version');
        if ($version) {
            return $version;
        }
        
        // Default to current version
        return $this->config->get('api_versioning.current_version');
    }
    
    private function migrateRequest($request, $version)
    {
        $currentVersion = $this->config->get('api_versioning.current_version');
        
        if ($version === $currentVersion) {
            return $request;
        }
        
        $migrator = $this->getMigrator($version, $currentVersion);
        return $migrator->migrateRequest($request);
    }
    
    private function migrateResponse($response, $version)
    {
        $currentVersion = $this->config->get('api_versioning.current_version');
        
        if ($version === $currentVersion) {
            return $response;
        }
        
        $migrator = $this->getMigrator($currentVersion, $version);
        return $migrator->migrateResponse($response);
    }
    
    private function getMigrator($fromVersion, $toVersion)
    {
        $key = "{$fromVersion}_to_{$toVersion}";
        
        if (!isset($this->migrators[$key])) {
            $this->migrators[$key] = $this->createMigrator($fromVersion, $toVersion);
        }
        
        return $this->migrators[$key];
    }
}

class APIMigrator
{
    private $changes;
    
    public function __construct($changes)
    {
        $this->changes = $changes;
    }
    
    public function migrateRequest($request)
    {
        $data = $request->getBody();
        
        foreach ($this->changes as $change) {
            $data = $this->applyChange($data, $change, 'request');
        }
        
        $request->setBody($data);
        return $request;
    }
    
    public function migrateResponse($response)
    {
        $data = $response->getBody();
        
        foreach ($this->changes as $change) {
            $data = $this->applyChange($data, $change, 'response');
        }
        
        $response->setBody($data);
        return $response;
    }
    
    private function applyChange($data, $change, $direction)
    {
        switch ($change['type']) {
            case 'field_renamed':
                return $this->renameField($data, $change, $direction);
            case 'field_removed':
                return $this->removeField($data, $change, $direction);
            case 'field_added':
                return $this->addField($data, $change, $direction);
        }
        
        return $data;
    }
    
    private function renameField($data, $change, $direction)
    {
        if ($direction === 'request') {
            // v1 -> v2: user_name -> username
            if (isset($data[$change['old']])) {
                $data[$change['new']] = $data[$change['old']];
                unset($data[$change['old']]);
            }
        } else {
            // v2 -> v1: username -> user_name
            if (isset($data[$change['new']])) {
                $data[$change['old']] = $data[$change['new']];
                unset($data[$change['new']]);
            }
        }
        
        return $data;
    }
}
```

## 📊 API Analytics and Monitoring

### API Analytics Configuration

```ini
# api-analytics.tsk
[api_analytics]
enabled = true
sampling_rate = 0.1
metrics_retention = 30

[api_analytics.metrics]
request_count = true
response_time = true
error_rate = true
endpoint_usage = true
user_activity = true

[api_analytics.alerts]
error_rate_threshold = 0.05
response_time_threshold = 5000
rate_limit_threshold = 0.8
```

### API Analytics Implementation

```php
class APIAnalytics
{
    private $config;
    private $metrics;
    private $database;
    
    public function __construct()
    {
        $this->config = new TuskConfig('api-analytics.tsk');
        $this->metrics = new MetricsCollector();
        $this->database = new Database();
    }
    
    public function recordRequest($request, $response, $duration, $context = [])
    {
        if (!$this->config->get('api_analytics.enabled')) {
            return;
        }
        
        // Apply sampling
        if (rand(1, 100) > ($this->config->get('api_analytics.sampling_rate') * 100)) {
            return;
        }
        
        $data = [
            'method' => $request->getMethod(),
            'path' => $request->getPath(),
            'status_code' => $response->getStatusCode(),
            'duration' => $duration,
            'user_id' => $context['user_id'] ?? null,
            'ip_address' => $request->getClientIP(),
            'user_agent' => $request->getUserAgent(),
            'timestamp' => time()
        ];
        
        $this->database->insert('api_analytics', $data);
        $this->checkAlerts($data);
    }
    
    public function getAPIMetrics($timeRange = 3600)
    {
        $metrics = [];
        
        if ($this->config->get('api_analytics.metrics.request_count')) {
            $metrics['request_count'] = $this->getRequestCount($timeRange);
        }
        
        if ($this->config->get('api_analytics.metrics.response_time')) {
            $metrics['response_time'] = $this->getResponseTimeMetrics($timeRange);
        }
        
        if ($this->config->get('api_analytics.metrics.error_rate')) {
            $metrics['error_rate'] = $this->getErrorRate($timeRange);
        }
        
        if ($this->config->get('api_analytics.metrics.endpoint_usage')) {
            $metrics['endpoint_usage'] = $this->getEndpointUsage($timeRange);
        }
        
        return $metrics;
    }
    
    private function getRequestCount($timeRange)
    {
        $sql = "
            SELECT COUNT(*) as count
            FROM api_analytics 
            WHERE timestamp > ?
        ";
        
        $result = $this->database->query($sql, [time() - $timeRange]);
        return $result->fetch()['count'];
    }
    
    private function getResponseTimeMetrics($timeRange)
    {
        $sql = "
            SELECT 
                AVG(duration) as avg_duration,
                MAX(duration) as max_duration,
                MIN(duration) as min_duration,
                PERCENTILE(duration, 95) as p95_duration
            FROM api_analytics 
            WHERE timestamp > ?
        ";
        
        $result = $this->database->query($sql, [time() - $timeRange]);
        return $result->fetch();
    }
    
    private function getErrorRate($timeRange)
    {
        $sql = "
            SELECT 
                COUNT(CASE WHEN status_code >= 400 THEN 1 END) as error_count,
                COUNT(*) as total_count
            FROM api_analytics 
            WHERE timestamp > ?
        ";
        
        $result = $this->database->query($sql, [time() - $timeRange]);
        $row = $result->fetch();
        
        return $row['total_count'] > 0 ? $row['error_count'] / $row['total_count'] : 0;
    }
    
    private function checkAlerts($data)
    {
        $errorRate = $this->getErrorRate(300); // Last 5 minutes
        $errorRateThreshold = $this->config->get('api_analytics.alerts.error_rate_threshold');
        
        if ($errorRate > $errorRateThreshold) {
            $this->triggerAlert('high_error_rate', [
                'error_rate' => $errorRate,
                'threshold' => $errorRateThreshold
            ]);
        }
        
        $responseTimeThreshold = $this->config->get('api_analytics.alerts.response_time_threshold');
        
        if ($data['duration'] > $responseTimeThreshold) {
            $this->triggerAlert('slow_response_time', [
                'duration' => $data['duration'],
                'threshold' => $responseTimeThreshold,
                'endpoint' => $data['path']
            ]);
        }
    }
}
```

## 📋 Best Practices

### API Design Best Practices

1. **RESTful Design**: Follow REST principles for resource-based APIs
2. **GraphQL for Complex Queries**: Use GraphQL for flexible data fetching
3. **Versioning Strategy**: Implement proper API versioning and migration
4. **Security First**: Implement comprehensive authentication and authorization
5. **Rate Limiting**: Protect APIs from abuse with intelligent rate limiting
6. **Caching Strategy**: Implement appropriate caching for performance
7. **Documentation**: Maintain comprehensive API documentation
8. **Monitoring**: Track API usage, performance, and errors

### Integration Examples

```php
// Integration with Laravel
class TuskLangAPIController extends Controller
{
    public function handle($request)
    {
        $apiManager = new APIManager();
        return $apiManager->handleRequest($request);
    }
}

// Integration with Symfony
class TuskLangAPIController extends AbstractController
{
    public function handle(Request $request): Response
    {
        $apiManager = new APIManager();
        return $apiManager->handleRequest($request);
    }
}
```

## 🔧 Troubleshooting

### Common Issues

1. **Version Compatibility**: Ensure proper request/response migration
2. **Rate Limiting**: Monitor and adjust rate limits based on usage
3. **Performance Issues**: Use caching and query optimization
4. **Security Vulnerabilities**: Implement proper input validation
5. **Documentation Drift**: Keep documentation in sync with code

### Debug Configuration

```ini
# debug-api.tsk
[debug]
enabled = true
log_level = "verbose"
trace_requests = true

[debug.output]
console = true
file = "/var/log/tusk-api-debug.log"
```

This comprehensive API design system leverages TuskLang's configuration-driven approach to create intelligent, secure, and high-performance APIs that adapt to application needs automatically. 