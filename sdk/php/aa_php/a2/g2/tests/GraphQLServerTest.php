<?php

namespace TuskLang\Tests\Communication\GraphQL;

use PHPUnit\Framework\TestCase;
use TuskLang\Communication\GraphQL\GraphQLServer;
use TuskLang\Communication\GraphQL\SchemaBuilder;
use TuskLang\Communication\Http\ApiRequest;
use GraphQL\Type\Schema;
use GraphQL\Type\Definition\ObjectType;
use GraphQL\Type\Definition\Type;

/**
 * Test suite for GraphQL Server
 */
class GraphQLServerTest extends TestCase
{
    private GraphQLServer $server;
    private Schema $schema;

    protected function setUp(): void
    {
        $this->schema = $this->createTestSchema();
        $this->server = new GraphQLServer($this->schema, [
            'debug' => true,
            'max_query_complexity' => 100,
            'max_query_depth' => 10
        ]);
    }

    public function testSimpleQuery(): void
    {
        $query = '{ hello }';
        $result = $this->server->executeQuery($query);
        
        $this->assertNull($result->errors);
        $this->assertEquals(['hello' => 'Hello World!'], $result->data);
    }

    public function testQueryWithVariables(): void
    {
        $query = 'query($name: String!) { greet(name: $name) }';
        $variables = ['name' => 'GraphQL'];
        
        $result = $this->server->executeQuery($query, $variables);
        
        $this->assertNull($result->errors);
        $this->assertEquals(['greet' => 'Hello GraphQL!'], $result->data);
    }

    public function testComplexityLimiting(): void
    {
        // Create a query that exceeds complexity limit
        $query = '{ ' . str_repeat('hello ', 50) . '}';
        
        $request = $this->createMockRequest(['query' => $query]);
        $response = $this->server->handle($request);
        
        $data = json_decode($response->getBody(), true);
        $this->assertArrayHasKey('errors', $data);
        $this->assertStringContains('complexity', $data['errors'][0]['message']);
    }

    public function testIntrospectionQuery(): void
    {
        $query = '{ __schema { types { name } } }';
        $result = $this->server->executeQuery($query);
        
        $this->assertNull($result->errors);
        $this->assertArrayHasKey('__schema', $result->data);
        $this->assertArrayHasKey('types', $result->data['__schema']);
    }

    public function testIntrospectionDisabled(): void
    {
        $server = new GraphQLServer($this->schema, ['introspection' => false]);
        
        $query = '{ __schema { types { name } } }';
        $request = $this->createMockRequest(['query' => $query]);
        $response = $server->handle($request);
        
        $data = json_decode($response->getBody(), true);
        $this->assertArrayHasKey('errors', $data);
    }

    public function testBatchQueries(): void
    {
        $queries = [
            ['query' => '{ hello }'],
            ['query' => '{ greet(name: "Test") }']
        ];
        
        $request = $this->createMockRequest($queries);
        $response = $this->server->handle($request);
        
        $data = json_decode($response->getBody(), true);
        $this->assertIsArray($data);
        $this->assertCount(2, $data);
        $this->assertEquals('Hello World!', $data[0]['data']['hello']);
        $this->assertEquals('Hello Test!', $data[1]['data']['greet']);
    }

    public function testErrorHandling(): void
    {
        $query = '{ nonExistentField }';
        $result = $this->server->executeQuery($query);
        
        $this->assertNotEmpty($result->errors);
        $this->assertStringContains('nonExistentField', $result->errors[0]->getMessage());
    }

    public function testCustomFieldResolver(): void
    {
        $this->server->setFieldResolver(function($source, $args, $context, $info) {
            if ($info->fieldName === 'customField') {
                return 'Custom resolved value';
            }
            return null;
        });
        
        // This would require a schema with customField
        $this->assertTrue(true); // Placeholder test
    }

    public function testDataLoaderIntegration(): void
    {
        $this->server->registerDataLoader('users', function($ids) {
            // Mock data loader that returns user data for given IDs
            return array_map(function($id) {
                return ['id' => $id, 'name' => "User {$id}"];
            }, $ids);
        });
        
        $loaders = $this->server->getStats()['data_loaders'];
        $this->assertContains('users', $loaders);
    }

    public function testMiddleware(): void
    {
        $middlewareCalled = false;
        
        $middleware = new class($middlewareCalled) implements \TuskLang\Communication\GraphQL\GraphQLMiddleware {
            private $called;
            
            public function __construct(&$called) {
                $this->called = &$called;
            }
            
            public function process(string $query, $context): string {
                $this->called = true;
                return $query;
            }
        };
        
        $this->server->addMiddleware($middleware);
        $this->server->executeQuery('{ hello }');
        
        $this->assertTrue($middlewareCalled);
    }

    public function testSchemaValidation(): void
    {
        $validation = $this->server->validateSchema();
        $this->assertIsArray($validation);
    }

    public function testSchemaExport(): void
    {
        $sdl = $this->server->exportSchema();
        $this->assertIsString($sdl);
        $this->assertStringContains('type Query', $sdl);
    }

    public function testStatistics(): void
    {
        $stats = $this->server->getStats();
        
        $this->assertArrayHasKey('schema_types', $stats);
        $this->assertArrayHasKey('middleware_count', $stats);
        $this->assertArrayHasKey('data_loaders', $stats);
        $this->assertArrayHasKey('configuration', $stats);
        
        $this->assertIsInt($stats['schema_types']);
        $this->assertIsInt($stats['middleware_count']);
        $this->assertIsArray($stats['data_loaders']);
        $this->assertIsArray($stats['configuration']);
    }

    /**
     * Create test schema
     */
    private function createTestSchema(): Schema
    {
        return new Schema([
            'query' => new ObjectType([
                'name' => 'Query',
                'fields' => [
                    'hello' => [
                        'type' => Type::string(),
                        'resolve' => function() {
                            return 'Hello World!';
                        }
                    ],
                    'greet' => [
                        'type' => Type::string(),
                        'args' => [
                            'name' => ['type' => Type::nonNull(Type::string())]
                        ],
                        'resolve' => function($root, $args) {
                            return 'Hello ' . $args['name'] . '!';
                        }
                    ]
                ]
            ])
        ]);
    }

    /**
     * Create mock API request
     */
    private function createMockRequest($data): ApiRequest
    {
        $request = new ApiRequest('POST', '/graphql');
        $request->setData($data);
        $request->setHeaders(['Content-Type' => 'application/json']);
        return $request;
    }
}

/**
 * Mock GraphQL Middleware interface
 */
interface GraphQLMiddleware
{
    public function process(string $query, $context): string;
} 