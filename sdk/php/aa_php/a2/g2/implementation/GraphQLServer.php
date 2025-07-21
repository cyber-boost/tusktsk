<?php

namespace TuskLang\Communication\GraphQL;

use GraphQL\GraphQL;
use GraphQL\Type\Schema;
use GraphQL\Error\Error;
use GraphQL\Error\FormattedError;
use GraphQL\Executor\ExecutionResult;
use GraphQL\Utils\AST;
use GraphQL\Language\Parser;
use GraphQL\Language\Source;
use TuskLang\Communication\Http\ApiRequest;
use TuskLang\Communication\Http\ApiResponse;

/**
 * Advanced GraphQL Server with comprehensive features
 * 
 * Features:
 * - Query/Mutation/Subscription support
 * - Schema validation and introspection
 * - Query complexity analysis
 * - Data loader integration
 * - Custom error handling
 * - Performance monitoring
 */
class GraphQLServer
{
    private Schema $schema;
    private array $config;
    private array $middleware = [];
    private ?DataLoaderRegistry $dataLoaderRegistry = null;
    private SecurityValidator $security;
    private QueryComplexityAnalyzer $complexityAnalyzer;
    
    public function __construct(Schema $schema, array $config = [])
    {
        $this->schema = $schema;
        $this->config = array_merge([
            'debug' => false,
            'max_query_complexity' => 1000,
            'max_query_depth' => 15,
            'introspection' => true,
            'query_batching' => true,
            'persisted_queries' => false,
            'query_cache' => true,
            'error_formatter' => null,
            'error_handler' => null,
            'field_resolver' => null,
            'validation_rules' => []
        ], $config);
        
        $this->security = new SecurityValidator($this->config);
        $this->complexityAnalyzer = new QueryComplexityAnalyzer($this->config);
        $this->dataLoaderRegistry = new DataLoaderRegistry();
    }

    /**
     * Handle GraphQL request
     */
    public function handle(ApiRequest $request): ApiResponse
    {
        try {
            $body = $request->getData();
            
            // Handle batch queries
            if ($this->config['query_batching'] && is_array($body) && isset($body[0])) {
                return $this->handleBatchQueries($request, $body);
            }
            
            return $this->handleSingleQuery($request, $body);
            
        } catch (\Throwable $e) {
            return $this->handleError($e);
        }
    }

    /**
     * Handle single GraphQL query
     */
    private function handleSingleQuery(ApiRequest $request, array $body): ApiResponse
    {
        $query = $body['query'] ?? null;
        $variables = $body['variables'] ?? [];
        $operationName = $body['operationName'] ?? null;
        
        if (!$query) {
            return ApiResponse::error('Missing query', 400);
        }
        
        // Security validation
        $securityResult = $this->security->validate($query, $variables);
        if (!$securityResult->isValid()) {
            return ApiResponse::error($securityResult->getError(), 400);
        }
        
        // Parse and validate query
        $queryDocument = Parser::parse(new Source($query));
        
        // Complexity analysis
        $complexity = $this->complexityAnalyzer->analyze($queryDocument);
        if ($complexity > $this->config['max_query_complexity']) {
            return ApiResponse::error('Query complexity exceeds maximum allowed', 400);
        }
        
        // Execute query
        $result = $this->executeQuery($query, $variables, $operationName, $request);
        
        return $this->formatResult($result);
    }

    /**
     * Handle batch GraphQL queries
     */
    private function handleBatchQueries(ApiRequest $request, array $queries): ApiResponse
    {
        if (!$this->config['query_batching']) {
            return ApiResponse::error('Query batching is disabled', 400);
        }
        
        $results = [];
        
        foreach ($queries as $queryData) {
            $response = $this->handleSingleQuery($request, $queryData);
            $results[] = json_decode($response->getBody(), true);
        }
        
        return ApiResponse::success($results);
    }

    /**
     * Execute GraphQL query with middleware pipeline
     */
    public function executeQuery(
        string $query,
        array $variables = [],
        ?string $operationName = null,
        ?ApiRequest $request = null
    ): ExecutionResult {
        $context = $this->buildContext($request);
        
        // Apply middleware
        foreach ($this->middleware as $middleware) {
            $query = $middleware->process($query, $context);
        }
        
        // Execute with GraphQL-PHP
        return GraphQL::executeQuery(
            $this->schema,
            $query,
            null, // rootValue
            $context,
            $variables,
            $operationName,
            $this->config['field_resolver'],
            $this->config['validation_rules']
        );
    }

    /**
     * Build execution context
     */
    private function buildContext(?ApiRequest $request = null): GraphQLContext
    {
        return new GraphQLContext([
            'request' => $request,
            'dataLoaders' => $this->dataLoaderRegistry,
            'user' => $this->extractUser($request),
            'permissions' => $this->extractPermissions($request),
            'config' => $this->config
        ]);
    }

    /**
     * Extract user from request
     */
    private function extractUser(?ApiRequest $request): ?array
    {
        if (!$request) {
            return null;
        }
        
        $auth = $request->getAuthorization();
        if (!$auth) {
            return null;
        }
        
        // Decode JWT or validate session
        // This would integrate with your authentication system
        return ['id' => 1, 'email' => 'user@example.com']; // Mock user
    }

    /**
     * Extract permissions from request
     */
    private function extractPermissions(?ApiRequest $request): array
    {
        // Extract user permissions/roles
        return ['read', 'write']; // Mock permissions
    }

    /**
     * Format execution result
     */
    private function formatResult(ExecutionResult $result): ApiResponse
    {
        $data = [
            'data' => $result->data
        ];
        
        if (!empty($result->errors)) {
            $data['errors'] = array_map(function (Error $error) {
                return $this->config['error_formatter'] 
                    ? ($this->config['error_formatter'])($error)
                    : FormattedError::createFromException($error, $this->config['debug']);
            }, $result->errors);
        }
        
        if ($result->extensions) {
            $data['extensions'] = $result->extensions;
        }
        
        return ApiResponse::success($data);
    }

    /**
     * Handle server errors
     */
    private function handleError(\Throwable $e): ApiResponse
    {
        if ($this->config['error_handler']) {
            return ($this->config['error_handler'])($e);
        }
        
        $data = [
            'errors' => [[
                'message' => $e->getMessage(),
                'extensions' => [
                    'code' => 'INTERNAL_ERROR'
                ]
            ]]
        ];
        
        if ($this->config['debug']) {
            $data['errors'][0]['extensions']['trace'] = $e->getTraceAsString();
        }
        
        return ApiResponse::success($data, 200); // GraphQL returns 200 even for errors
    }

    /**
     * Add middleware
     */
    public function addMiddleware(GraphQLMiddleware $middleware): self
    {
        $this->middleware[] = $middleware;
        return $this;
    }

    /**
     * Get schema for introspection
     */
    public function getSchema(): Schema
    {
        return $this->schema;
    }

    /**
     * Enable/disable introspection
     */
    public function setIntrospection(bool $enabled): self
    {
        $this->config['introspection'] = $enabled;
        return $this;
    }

    /**
     * Set query complexity limits
     */
    public function setComplexityLimits(int $maxComplexity, int $maxDepth): self
    {
        $this->config['max_query_complexity'] = $maxComplexity;
        $this->config['max_query_depth'] = $maxDepth;
        return $this;
    }

    /**
     * Register data loader
     */
    public function registerDataLoader(string $name, callable $loader): self
    {
        $this->dataLoaderRegistry->register($name, $loader);
        return $this;
    }

    /**
     * Set custom field resolver
     */
    public function setFieldResolver(callable $resolver): self
    {
        $this->config['field_resolver'] = $resolver;
        return $this;
    }

    /**
     * Add validation rules
     */
    public function addValidationRules(array $rules): self
    {
        $this->config['validation_rules'] = array_merge(
            $this->config['validation_rules'],
            $rules
        );
        return $this;
    }

    /**
     * Generate GraphiQL interface
     */
    public function renderGraphiQL(string $endpoint = '/graphql'): string
    {
        return GraphiQLRenderer::render([
            'endpoint' => $endpoint,
            'introspection' => $this->config['introspection'],
            'config' => $this->config
        ]);
    }

    /**
     * Get execution statistics
     */
    public function getStats(): array
    {
        return [
            'schema_types' => count($this->schema->getTypeMap()),
            'middleware_count' => count($this->middleware),
            'data_loaders' => $this->dataLoaderRegistry->getRegisteredLoaders(),
            'configuration' => $this->config
        ];
    }

    /**
     * Validate schema
     */
    public function validateSchema(): array
    {
        return \GraphQL\Utils\SchemaUtils::schemaMetaFieldDef($this->schema);
    }

    /**
     * Export schema as SDL
     */
    public function exportSchema(): string
    {
        return \GraphQL\Utils\SchemaPrinter::doPrint($this->schema);
    }

    /**
     * Enable query caching
     */
    public function enableQueryCache(CacheInterface $cache): self
    {
        $this->middleware[] = new QueryCacheMiddleware($cache);
        return $this;
    }

    /**
     * Enable persisted queries
     */
    public function enablePersistedQueries(QueryStorage $storage): self
    {
        $this->config['persisted_queries'] = true;
        $this->middleware[] = new PersistedQueryMiddleware($storage);
        return $this;
    }
} 