<?php
/**
 * 🐘 TuskGraphQL Operator Tests
 * =============================
 * Comprehensive test suite for @graphql operator functionality
 */

require_once __DIR__ . '/../lib/TuskLang.php';
require_once __DIR__ . '/../lib/TuskGraphQL.php';

use TuskPHP\Utils\TuskLang;
use TuskPHP\Utils\TuskGraphQL;
use TuskPHP\Utils\GraphQLQueryBuilder;

class TuskGraphQLTests
{
    private $testResults = [];
    private $passed = 0;
    private $failed = 0;
    
    public function runAllTests(): void
    {
        echo "🚀 Starting TuskGraphQL Operator Tests\n";
        echo "=====================================\n\n";
        
        // Start mock server for testing
        $this->startMockServer();
        
        // Test GraphQL client functionality
        $this->testGraphQLClient();
        
        // Test @graphql operator parsing
        $this->testGraphQLOperatorParsing();
        
        // Test query validation
        $this->testQueryValidation();
        
        // Test caching functionality
        $this->testCaching();
        
        // Test error handling
        $this->testErrorHandling();
        
        // Test authentication
        $this->testAuthentication();
        
        // Test query builder
        $this->testQueryBuilder();
        
        // Test integration with TuskLang
        $this->testTuskLangIntegration();
        
        // Test with mock server
        $this->testWithMockServer();
        
        $this->printResults();
        
        // Stop mock server
        $this->stopMockServer();
    }
    
    private function startMockServer(): void
    {
        // Set up mock server endpoint
        TuskGraphQL::setEndpoint('test', 'http://127.0.0.1:8080/graphql');
        TuskGraphQL::configure(['default_endpoint' => 'http://127.0.0.1:8080/graphql']);
        
        echo "📡 Mock server configured at http://127.0.0.1:8080/graphql\n\n";
    }
    
    private function stopMockServer(): void
    {
        echo "\n📡 Mock server cleanup completed\n";
    }
    
    private function testGraphQLClient(): void
    {
        echo "📋 Testing GraphQL Client Functionality\n";
        echo "--------------------------------------\n";
        
        // Test configuration
        $this->test('Configure GraphQL client', function() {
            TuskGraphQL::configure([
                'timeout' => 60,
                'cache_ttl' => 600,
                'max_retries' => 5
            ]);
            return true;
        });
        
        // Test endpoint configuration
        $this->test('Set GraphQL endpoint', function() {
            TuskGraphQL::setEndpoint('test', 'https://api.example.com/graphql');
            return true;
        });
        
        // Test authentication
        $this->test('Set authentication token', function() {
            TuskGraphQL::setAuthToken('test', 'test-token-123', 'Bearer');
            return true;
        });
        
        // Test cache management
        $this->test('Cache statistics', function() {
            $stats = TuskGraphQL::getCacheStats();
            return is_array($stats) && isset($stats['total_entries']);
        });
        
        echo "\n";
    }
    
    private function testGraphQLOperatorParsing(): void
    {
        echo "📋 Testing @graphql Operator Parsing\n";
        echo "-----------------------------------\n";
        
        // Test basic query parsing
        $this->test('Parse basic GraphQL query', function() {
            $content = 'users: @graphql("{ users { id name email } }")';
            $result = TuskLang::parse($content);
            return isset($result['users']) && is_array($result['users']);
        });
        
        // Test query with variables
        $this->test('Parse GraphQL query with variables', function() {
            $content = 'user: @graphql("{ user(id: \$userId) { id name email } }", {"userId": 123})';
            $result = TuskLang::parse($content);
            return isset($result['user']) && is_array($result['user']);
        });
        
        // Test query with options
        $this->test('Parse GraphQL query with options', function() {
            $content = 'users: @graphql("{ users { id name } }", {}, {"endpoint": "https://api.example.com/graphql"})';
            $result = TuskLang::parse($content);
            return isset($result['users']) && is_array($result['users']);
        });
        
        // Test complex query
        $this->test('Parse complex GraphQL query', function() {
            $content = 'data: @graphql("
                query GetUserData(\$id: ID!) {
                    user(id: \$id) {
                        id
                        name
                        email
                        posts {
                            id
                            title
                            content
                        }
                    }
                }
            ", {"id": "123"})';
            $result = TuskLang::parse($content);
            return isset($result['data']) && is_array($result['data']);
        });
        
        echo "\n";
    }
    
    private function testQueryValidation(): void
    {
        echo "📋 Testing Query Validation\n";
        echo "---------------------------\n";
        
        // Test valid queries
        $this->test('Validate simple query', function() {
            return TuskGraphQL::validateQuery('{ users { id name } }');
        });
        
        $this->test('Validate query with operation name', function() {
            return TuskGraphQL::validateQuery('query GetUsers { users { id name } }');
        });
        
        $this->test('Validate mutation', function() {
            return TuskGraphQL::validateQuery('mutation CreateUser($name: String!) { createUser(name: $name) { id name } }');
        });
        
        // Test invalid queries
        $this->test('Reject invalid query syntax', function() {
            return !TuskGraphQL::validateQuery('{ users { id name }'); // Missing closing brace
        });
        
        $this->test('Reject malformed query', function() {
            return !TuskGraphQL::validateQuery('invalid query syntax');
        });
        
        // Test query field extraction
        $this->test('Extract query fields', function() {
            $fields = TuskGraphQL::parseQueryFields('{ users { id name email } posts { id title } }');
            return in_array('users', $fields) && in_array('posts', $fields);
        });
        
        // Test query complexity
        $this->test('Calculate query complexity', function() {
            $complexity = TuskGraphQL::getQueryComplexity('{ users { id posts { id } } }');
            return $complexity > 0;
        });
        
        echo "\n";
    }
    
    private function testCaching(): void
    {
        echo "📋 Testing Caching Functionality\n";
        echo "--------------------------------\n";
        
        // Test cache configuration
        $this->test('Configure cache TTL', function() {
            TuskGraphQL::configure(['cache_ttl' => 60]);
            return true;
        });
        
        // Test cache clearing
        $this->test('Clear cache', function() {
            TuskGraphQL::clearCache();
            $stats = TuskGraphQL::getCacheStats();
            return $stats['total_entries'] === 0;
        });
        
        // Test cache statistics
        $this->test('Get cache statistics', function() {
            $stats = TuskGraphQL::getCacheStats();
            return is_array($stats) && 
                   isset($stats['total_entries']) && 
                   isset($stats['memory_usage']);
        });
        
        echo "\n";
    }
    
    private function testErrorHandling(): void
    {
        echo "📋 Testing Error Handling\n";
        echo "-------------------------\n";
        
        // Test invalid endpoint
        $this->test('Handle invalid endpoint', function() {
            try {
                TuskGraphQL::query('{ users { id } }', [], ['endpoint' => 'invalid-url']);
                return false; // Should throw exception
            } catch (\Exception $e) {
                return strpos($e->getMessage(), 'GraphQL endpoint not configured') !== false;
            }
        });
        
        // Test invalid query syntax
        $this->test('Handle invalid query syntax', function() {
            try {
                $content = 'data: @graphql("invalid query syntax")';
                TuskLang::parse($content);
                return false; // Should throw exception
            } catch (\Exception $e) {
                return strpos($e->getMessage(), 'GraphQL query error') !== false;
            }
        });
        
        // Test network errors
        $this->test('Handle network errors gracefully', function() {
            try {
                TuskGraphQL::query('{ users { id } }', [], ['endpoint' => 'https://nonexistent.example.com/graphql']);
                return false; // Should throw exception
            } catch (\Exception $e) {
                return true; // Should catch network error
            }
        });
        
        echo "\n";
    }
    
    private function testAuthentication(): void
    {
        echo "📋 Testing Authentication\n";
        echo "-------------------------\n";
        
        // Test token setting
        $this->test('Set authentication token', function() {
            TuskGraphQL::setAuthToken('test-endpoint', 'test-token', 'Bearer');
            return true;
        });
        
        // Test different auth types
        $this->test('Set API key authentication', function() {
            TuskGraphQL::setAuthToken('api-endpoint', 'api-key-123', 'ApiKey');
            return true;
        });
        
        echo "\n";
    }
    
    private function testQueryBuilder(): void
    {
        echo "📋 Testing Query Builder\n";
        echo "------------------------\n";
        
        // Test basic query builder
        $this->test('Build simple query', function() {
            $builder = new GraphQLQueryBuilder();
            $query = $builder->query('GetUsers')
                           ->fields(['users' => ['id', 'name', 'email']])
                           ->build();
            
            return isset($query['query']) && strpos($query['query'], 'query GetUsers') !== false;
        });
        
        // Test query with variables
        $this->test('Build query with variables', function() {
            $builder = new GraphQLQueryBuilder();
            $query = $builder->query('GetUser')
                           ->variable('id', 'ID!')
                           ->fields(['user' => ['id', 'name', 'email']])
                           ->build();
            
            return isset($query['query']) && strpos($query['query'], '$id: ID!') !== false;
        });
        
        // Test mutation builder
        $this->test('Build mutation', function() {
            $builder = new GraphQLQueryBuilder();
            $query = $builder->mutation('CreateUser')
                           ->variable('name', 'String!')
                           ->fields(['createUser' => ['id', 'name']])
                           ->build();
            
            return isset($query['query']) && strpos($query['query'], 'mutation CreateUser') !== false;
        });
        
        echo "\n";
    }
    
    private function testTuskLangIntegration(): void
    {
        echo "📋 Testing TuskLang Integration\n";
        echo "-------------------------------\n";
        
        // Test basic integration
        $this->test('Parse TuskLang with GraphQL', function() {
            $content = '
                [api]
                endpoint: "https://api.example.com/graphql"
                
                [data]
                users: @graphql("{ users { id name email } }")
                user_count: @graphql("{ users { id } }").length
            ';
            
            $result = TuskLang::parse($content);
            return isset($result['api']['endpoint']) && isset($result['data']['users']);
        });
        
        // Test with environment variables
        $this->test('GraphQL with environment variables', function() {
            $_ENV['GRAPHQL_ENDPOINT'] = 'https://api.example.com/graphql';
            
            $content = '
                [config]
                graphql_endpoint: env("GRAPHQL_ENDPOINT")
                
                [data]
                users: @graphql("{ users { id name } }", {}, {"endpoint": env("GRAPHQL_ENDPOINT")})
            ';
            
            $result = TuskLang::parse($content);
            return isset($result['config']['graphql_endpoint']) && isset($result['data']['users']);
        });
        
        // Test complex configuration
        $this->test('Complex TuskLang GraphQL config', function() {
            $content = '
                [graphql]
                endpoint: "https://api.example.com/graphql"
                timeout: 30
                cache_ttl: 300
                
                [queries]
                users: @graphql("
                    query GetUsers {
                        users {
                            id
                            name
                            email
                            posts {
                                id
                                title
                            }
                        }
                    }
                ")
                
                user_by_id: @graphql("
                    query GetUser($id: ID!) {
                        user(id: $id) {
                            id
                            name
                            email
                        }
                    }
                ", {"id": "123"})
            ';
            
            $result = TuskLang::parse($content);
            return isset($result['graphql']['endpoint']) && 
                   isset($result['queries']['users']) && 
                   isset($result['queries']['user_by_id']);
        });
        
        echo "\n";
    }
    
    private function testWithMockServer(): void
    {
        echo "📋 Testing with Mock Server\n";
        echo "---------------------------\n";
        
        // Test basic query with mock server
        $this->test('Execute query with mock server', function() {
            $result = TuskGraphQL::query('{ users { id name email } }');
            return isset($result['users']) && count($result['users']) > 0;
        });
        
        // Test query with variables
        $this->test('Execute query with variables', function() {
            $result = TuskGraphQL::query('
                query GetUser($id: ID!) {
                    user(id: $id) {
                        id
                        name
                        email
                    }
                }
            ', ['id' => '123']);
            return isset($result['user']) && $result['user']['id'] === '123';
        });
        
        // Test mutation
        $this->test('Execute mutation', function() {
            $result = TuskGraphQL::mutation('
                mutation CreateUser($name: String!, $email: String!) {
                    createUser(input: { name: $name, email: $email }) {
                        id
                        name
                        email
                    }
                }
            ', ['name' => 'Test User', 'email' => 'test@example.com']);
            return isset($result['createUser']) && $result['createUser']['name'] === 'Test User';
        });
        
        // Test schema introspection
        $this->test('Schema introspection', function() {
            $result = TuskGraphQL::query('{ __schema { types { name } } }');
            return isset($result['__schema']['types']) && count($result['__schema']['types']) > 0;
        });
        
        // Test TuskLang integration with mock server
        $this->test('TuskLang with mock server', function() {
            $content = 'users: @graphql("{ users { id name email } }")';
            $result = TuskLang::parse($content);
            return isset($result['users']) && is_array($result['users']);
        });
        
        echo "\n";
    }
    
    private function test(string $name, callable $testFunction): void
    {
        try {
            $result = $testFunction();
            if ($result) {
                echo "✅ PASS: $name\n";
                $this->passed++;
            } else {
                echo "❌ FAIL: $name\n";
                $this->failed++;
            }
        } catch (\Exception $e) {
            echo "❌ FAIL: $name - " . $e->getMessage() . "\n";
            $this->failed++;
        }
        
        $this->testResults[] = [
            'name' => $name,
            'passed' => $result ?? false,
            'error' => $e ?? null
        ];
    }
    
    private function printResults(): void
    {
        echo "\n📊 Test Results Summary\n";
        echo "======================\n";
        echo "✅ Passed: {$this->passed}\n";
        echo "❌ Failed: {$this->failed}\n";
        echo "📈 Success Rate: " . round(($this->passed / ($this->passed + $this->failed)) * 100, 2) . "%\n\n";
        
        if ($this->failed > 0) {
            echo "🔍 Failed Tests:\n";
            foreach ($this->testResults as $result) {
                if (!$result['passed']) {
                    echo "  - {$result['name']}\n";
                    if ($result['error']) {
                        echo "    Error: {$result['error']->getMessage()}\n";
                    }
                }
            }
        }
        
        echo "\n🎉 TuskGraphQL Operator Tests Complete!\n";
    }
}

// Run tests if executed directly
if (php_sapi_name() === 'cli' || defined('RUNNING_TESTS')) {
    $tests = new TuskGraphQLTests();
    $tests->runAllTests();
} 