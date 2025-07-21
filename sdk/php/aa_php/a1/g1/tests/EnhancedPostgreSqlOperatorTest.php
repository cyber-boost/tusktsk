<?php

namespace TuskLang\CoreOperators\Enhanced\Tests;

use PHPUnit\Framework\TestCase;
use TuskLang\CoreOperators\Enhanced\EnhancedPostgreSqlOperator;
use PDO;
use PDOException;

/**
 * Comprehensive test suite for Enhanced PostgreSQL Operator
 * Coverage: JSON/JSONB operations, arrays, LISTEN/NOTIFY, advanced PostgreSQL features
 */
class EnhancedPostgreSqlOperatorTest extends TestCase
{
    private EnhancedPostgreSqlOperator $operator;
    private array $testConfig;
    
    protected function setUp(): void
    {
        $this->testConfig = [
            'debug' => true,
            'max_connections' => 5,
            'connection_timeout' => 10
        ];
        
        $this->operator = new EnhancedPostgreSqlOperator($this->testConfig);
    }
    
    protected function tearDown(): void
    {
        try {
            $this->operator->execute('clear_cache');
            $this->operator->execute('unlisten', ['channel' => '*']);
        } catch (Exception $e) {
            // Ignore cleanup errors
        }
    }
    
    public function testOperatorInitialization(): void
    {
        $this->assertEquals('enhanced_postgresql', $this->operator->getName());
        $this->assertEquals('2.0.0', $this->operator->getVersion());
        $this->assertStringContains('Enhanced PostgreSQL operator', $this->operator->getDescription());
    }
    
    public function testJSONOperations(): void
    {
        $this->setupMockConnection();
        
        // Test JSON get
        $result = $this->operator->execute('json_get', [
            'table' => 'documents',
            'column' => 'data',
            'path' => 'name',
            'where' => ['active' => true]
        ]);
        $this->assertIsArray($result);
        
        // Test JSON path query
        $pathResult = $this->operator->execute('json_path', [
            'table' => 'documents',
            'column' => 'data',
            'path' => '$.users[*].name'
        ]);
        $this->assertIsArray($pathResult);
    }
    
    public function testJSONBOperations(): void
    {
        $this->setupMockConnection();
        
        // Test JSONB contains
        $contains = $this->operator->execute('jsonb_contains', [
            'table' => 'profiles',
            'column' => 'metadata',
            'value' => ['type' => 'premium'],
            'where' => ['user_id' => 123]
        ]);
        $this->assertIsBool($contains);
        
        // Test JSONB path query
        $pathResult = $this->operator->execute('jsonb_path', [
            'table' => 'profiles',
            'column' => 'metadata',
            'path' => '$.settings.notifications'
        ]);
        $this->assertIsArray($pathResult);
    }
    
    public function testArrayOperations(): void
    {
        $this->setupMockConnection();
        
        // Test array contains
        $contains = $this->operator->execute('array_contains', [
            'table' => 'user_tags',
            'column' => 'tags',
            'value' => 'premium',
            'where' => ['user_id' => 123]
        ]);
        $this->assertIsBool($contains);
        
        // Test array length
        $length = $this->operator->execute('array_length', [
            'table' => 'user_tags',
            'column' => 'tags',
            'dimension' => 1,
            'where' => ['user_id' => 123]
        ]);
        $this->assertIsInt($length);
    }
    
    public function testListenNotifyOperations(): void
    {
        $this->setupMockConnection();
        
        // Test listen to channel
        $this->assertTrue($this->operator->execute('listen', [
            'channel' => 'test_channel'
        ]));
        
        // Test notify
        $this->assertTrue($this->operator->execute('notify', [
            'channel' => 'test_channel',
            'payload' => 'test message'
        ]));
        
        // Test get notifications
        $notifications = $this->operator->execute('get_notifications');
        $this->assertIsArray($notifications);
        
        // Test unlisten
        $this->assertTrue($this->operator->execute('unlisten', [
            'channel' => 'test_channel'
        ]));
    }
    
    public function testListenWithCallback(): void
    {
        $this->setupMockConnection();
        
        $callbackExecuted = false;
        $callback = function($notification) use (&$callbackExecuted) {
            $callbackExecuted = true;
            $this->assertArrayHasKey('channel', $notification);
            $this->assertArrayHasKey('payload', $notification);
        };
        
        // Listen with callback
        $this->assertTrue($this->operator->execute('listen', [
            'channel' => 'callback_test',
            'callback' => $callback
        ]));
        
        // Send notification
        $this->operator->execute('notify', [
            'channel' => 'callback_test',
            'payload' => 'callback test message'
        ]);
        
        // Poll for notifications
        $notifications = $this->operator->execute('poll_notifications', [
            'timeout' => 1
        ]);
        
        $this->assertIsArray($notifications);
    }
    
    public function testFullTextSearch(): void
    {
        $this->setupMockConnection();
        
        // Test enhanced fulltext search with ranking
        $results = $this->operator->execute('fulltext_search', [
            'table' => 'articles',
            'column' => 'content',
            'query' => 'postgresql database',
            'config' => 'english',
            'rank' => true,
            'limit' => 10
        ]);
        
        $this->assertIsArray($results);
        
        // Test trigram similarity search
        $similar = $this->operator->execute('trigram_search', [
            'table' => 'articles',
            'column' => 'title',
            'query' => 'PostgreSQL tutorial',
            'threshold' => 0.3,
            'limit' => 5
        ]);
        
        $this->assertIsArray($similar);
    }
    
    public function testWindowFunctions(): void
    {
        $this->setupMockConnection();
        
        // Test window function execution
        $result = $this->operator->execute('window_function', [
            'sql' => 'SELECT id, name, ROW_NUMBER() OVER (ORDER BY created_at DESC) as row_num FROM users',
            'bindings' => []
        ]);
        
        $this->assertIsArray($result);
    }
    
    public function testCommonTableExpressions(): void
    {
        $this->setupMockConnection();
        
        // Test WITH query
        $result = $this->operator->execute('with_query', [
            'sql' => 'WITH recent_users AS (SELECT * FROM users WHERE created_at > ?) SELECT count(*) FROM recent_users',
            'bindings' => ['2024-01-01']
        ]);
        
        $this->assertIsArray($result);
        
        // Test recursive query
        $recursiveResult = $this->operator->execute('recursive_query', [
            'sql' => 'WITH RECURSIVE hierarchy AS (SELECT id, parent_id, name, 1 as level FROM categories WHERE parent_id IS NULL UNION ALL SELECT c.id, c.parent_id, c.name, h.level + 1 FROM categories c JOIN hierarchy h ON c.parent_id = h.id) SELECT * FROM hierarchy',
            'bindings' => []
        ]);
        
        $this->assertIsArray($recursiveResult);
    }
    
    public function testAdvancedTypeOperations(): void
    {
        $this->setupMockConnection();
        
        // Test enum creation
        $this->assertTrue($this->operator->execute('create_enum', [
            'name' => 'user_status',
            'values' => ['active', 'inactive', 'suspended']
        ]));
        
        // Test domain creation
        $this->assertTrue($this->operator->execute('create_domain', [
            'name' => 'email_domain',
            'type' => 'varchar(255)',
            'constraint' => 'CHECK (VALUE ~* \'^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$\')'
        ]));
    }
    
    public function testMonitoringOperations(): void
    {
        $this->setupMockConnection();
        
        // Test pg_stat_activity
        $activity = $this->operator->execute('pg_stat_activity');
        $this->assertIsArray($activity);
        
        // Test pg_stat_database
        $dbStats = $this->operator->execute('pg_stat_database');
        $this->assertIsArray($dbStats);
        
        // Test pg_locks
        $locks = $this->operator->execute('pg_locks');
        $this->assertIsArray($locks);
    }
    
    public function testHealthCheck(): void
    {
        $this->setupMockConnection();
        
        $health = $this->operator->execute('health_check');
        
        $this->assertIsArray($health);
        $this->assertArrayHasKey('status', $health);
        $this->assertArrayHasKey('connection_test', $health);
        $this->assertArrayHasKey('pool_status', $health);
        $this->assertArrayHasKey('version', $health);
        $this->assertArrayHasKey('errors', $health);
    }
    
    public function testMaintenanceOperations(): void
    {
        $this->setupMockConnection();
        
        // Test vacuum
        $this->assertTrue($this->operator->execute('vacuum', [
            'table' => 'users',
            'analyze' => true
        ]));
        
        // Test analyze
        $this->assertTrue($this->operator->execute('analyze', [
            'table' => 'users'
        ]));
        
        // Test reindex
        $this->assertTrue($this->operator->execute('reindex', [
            'table' => 'users'
        ]));
    }
    
    public function testSerializationFailureRetry(): void
    {
        $this->setupMockConnection();
        
        // Test automatic retry on serialization failure
        try {
            $result = $this->operator->execute('query', [
                'sql' => 'SELECT * FROM concurrent_table FOR UPDATE',
                'simulate_serialization_failure' => true
            ]);
        } catch (Exception $e) {
            $this->assertStringContains('PostgreSQL operation failed', $e->getMessage());
        }
    }
    
    public function testConnectionPooling(): void
    {
        $connectionParams = [
            'host' => 'localhost',
            'database' => 'test_db',
            'username' => 'test_user',
            'password' => 'test_pass',
            'sslmode' => 'prefer'
        ];
        
        $this->assertTrue($this->operator->execute('connect', $connectionParams));
        
        // Test pool status
        $poolStatus = $this->operator->execute('pool_status');
        $this->assertIsArray($poolStatus);
    }
    
    public function testMaterializedViews(): void
    {
        $this->setupMockConnection();
        
        // Test materialized view creation
        $this->assertTrue($this->operator->execute('create_materialized_view', [
            'name' => 'user_stats_mv',
            'query' => 'SELECT user_id, COUNT(*) as post_count FROM posts GROUP BY user_id'
        ]));
        
        // Test materialized view refresh
        $this->assertTrue($this->operator->execute('refresh_materialized_view', [
            'name' => 'user_stats_mv',
            'concurrently' => false
        ]));
    }
    
    public function testAdvancedJSONOperations(): void
    {
        $this->setupMockConnection();
        
        // Test json_each
        $result = $this->operator->execute('json_each', [
            'table' => 'settings',
            'column' => 'config'
        ]);
        $this->assertIsArray($result);
        
        // Test json_object_keys
        $keys = $this->operator->execute('json_object_keys', [
            'table' => 'settings',
            'column' => 'config'
        ]);
        $this->assertIsArray($keys);
        
        // Test json_strip_nulls
        $cleaned = $this->operator->execute('json_strip_nulls', [
            'table' => 'profiles',
            'column' => 'metadata'
        ]);
        $this->assertIsArray($cleaned);
    }
    
    public function testArrayAggregations(): void
    {
        $this->setupMockConnection();
        
        // Test array_agg
        $result = $this->operator->execute('array_agg', [
            'table' => 'user_tags',
            'column' => 'tag_name',
            'group_by' => 'user_id'
        ]);
        $this->assertIsArray($result);
        
        // Test array_to_string
        $stringResult = $this->operator->execute('array_to_string', [
            'table' => 'user_tags',
            'column' => 'tags',
            'delimiter' => ','
        ]);
        $this->assertIsArray($stringResult);
    }
    
    public function testCustomFunctionOperations(): void
    {
        $this->setupMockConnection();
        
        // Test function creation
        $this->assertTrue($this->operator->execute('create_function', [
            'name' => 'calculate_age',
            'parameters' => ['birth_date DATE'],
            'return_type' => 'INTEGER',
            'language' => 'SQL',
            'body' => 'SELECT EXTRACT(YEAR FROM AGE($1))'
        ]));
        
        // Test trigger creation
        $this->assertTrue($this->operator->execute('create_trigger', [
            'name' => 'update_timestamp',
            'table' => 'users',
            'timing' => 'BEFORE',
            'event' => 'UPDATE',
            'function' => 'update_modified_column'
        ]));
    }
    
    public function testExplainAnalyze(): void
    {
        $this->setupMockConnection();
        
        // Test explain analyze
        $plan = $this->operator->execute('explain_analyze', [
            'sql' => 'SELECT * FROM users WHERE email = ?',
            'bindings' => ['test@example.com']
        ]);
        
        $this->assertIsArray($plan);
    }
    
    public function testPerformanceBenchmark(): void
    {
        $this->setupMockConnection();
        
        $startTime = microtime(true);
        
        // Run benchmark operations
        for ($i = 0; $i < 50; $i++) {
            $this->operator->execute('query', [
                'sql' => 'SELECT ?::INTEGER as iteration, ?::TEXT as message',
                'bindings' => [$i, "Iteration $i"]
            ]);
        }
        
        $totalTime = microtime(true) - $startTime;
        $avgTime = $totalTime / 50;
        
        // PostgreSQL should be fast
        $this->assertLessThan(0.2, $avgTime, 'Average query time should be less than 200ms');
        
        // Check performance metrics
        $metrics = $this->operator->execute('performance_metrics');
        $this->assertIsArray($metrics);
    }
    
    /**
     * Helper method to setup mock connection for testing
     */
    private function setupMockConnection(): void
    {
        $this->operator->execute('connect', [
            'host' => 'localhost',
            'database' => 'test_db',
            'username' => 'test_user',
            'password' => 'test_pass',
            'sslmode' => 'prefer'
        ]);
    }
} 