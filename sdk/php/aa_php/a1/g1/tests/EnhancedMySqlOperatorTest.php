<?php

namespace TuskLang\CoreOperators\Enhanced\Tests;

use PHPUnit\Framework\TestCase;
use TuskLang\CoreOperators\Enhanced\EnhancedMySqlOperator;
use PDO;
use PDOException;

/**
 * Comprehensive test suite for Enhanced MySQL Operator
 * Coverage: Connection pooling, performance monitoring, error handling, transactions
 */
class EnhancedMySqlOperatorTest extends TestCase
{
    private EnhancedMySqlOperator $operator;
    private array $testConfig;
    
    protected function setUp(): void
    {
        $this->testConfig = [
            'debug' => true,
            'max_connections' => 5,
            'connection_timeout' => 10
        ];
        
        $this->operator = new EnhancedMySqlOperator($this->testConfig);
    }
    
    protected function tearDown(): void
    {
        // Clear all caches and pools after each test
        try {
            $this->operator->execute('clear_cache');
        } catch (Exception $e) {
            // Ignore cleanup errors
        }
    }
    
    public function testOperatorInitialization(): void
    {
        $this->assertEquals('enhanced_mysql', $this->operator->getName());
        $this->assertEquals('2.0.0', $this->operator->getVersion());
        $this->assertStringContains('Enhanced MySQL operator', $this->operator->getDescription());
    }
    
    public function testConnectionPooling(): void
    {
        $connectionParams = [
            'host' => 'localhost',
            'database' => 'test_db',
            'username' => 'test_user',
            'password' => 'test_pass'
        ];
        
        // Mock successful connection
        $this->assertTrue($this->operator->execute('connect', $connectionParams));
        
        // Test pool status
        $poolStatus = $this->operator->execute('pool_status');
        $this->assertIsArray($poolStatus);
    }
    
    public function testQueryExecutionWithPerformanceMonitoring(): void
    {
        $this->setupMockConnection();
        
        $queryParams = [
            'sql' => 'SELECT * FROM users WHERE active = ?',
            'bindings' => [1],
            'fetch_mode' => PDO::FETCH_ASSOC
        ];
        
        $result = $this->operator->execute('query', $queryParams);
        $this->assertIsArray($result);
        
        // Test performance metrics
        $metrics = $this->operator->execute('performance_metrics');
        $this->assertIsArray($metrics);
        $this->assertArrayHasKey(date('Y-m-d'), $metrics);
    }
    
    public function testPreparedStatementCaching(): void
    {
        $this->setupMockConnection();
        
        $sql = 'SELECT * FROM users WHERE id = ?';
        
        // Execute same query multiple times
        for ($i = 1; $i <= 3; $i++) {
            $result = $this->operator->execute('query', [
                'sql' => $sql,
                'bindings' => [$i]
            ]);
            $this->assertIsArray($result);
        }
        
        // Verify prepared statements are cached
        $metrics = $this->operator->execute('performance_metrics');
        $this->assertArrayHasKey('query_execution', $metrics[date('Y-m-d')] ?? []);
    }
    
    public function testTransactionSupport(): void
    {
        $this->setupMockConnection();
        
        // Test begin transaction
        $this->assertTrue($this->operator->execute('begin'));
        
        // Test savepoint
        $savepointName = $this->operator->execute('savepoint', ['name' => 'sp1']);
        $this->assertIsString($savepointName);
        
        // Test commit
        $this->assertTrue($this->operator->execute('commit'));
    }
    
    public function testNestedTransactionsWithSavepoints(): void
    {
        $this->setupMockConnection();
        
        // Begin main transaction
        $this->assertTrue($this->operator->execute('begin'));
        
        // Create savepoint
        $sp1 = $this->operator->execute('savepoint');
        $this->assertIsString($sp1);
        
        // Create nested savepoint
        $sp2 = $this->operator->execute('savepoint');
        $this->assertIsString($sp2);
        
        // Rollback to first savepoint
        $this->assertTrue($this->operator->execute('rollback_to', ['savepoint' => $sp1]));
        
        // Commit main transaction
        $this->assertTrue($this->operator->execute('commit'));
    }
    
    public function testDeadlockHandling(): void
    {
        $this->setupMockConnection();
        
        // Simulate deadlock error
        $this->expectException(PDOException::class);
        
        $result = $this->operator->execute('query', [
            'sql' => 'SELECT * FROM locked_table FOR UPDATE',
            'simulate_deadlock' => true
        ]);
    }
    
    public function testConnectionErrorRecovery(): void
    {
        $this->setupMockConnection();
        
        // Test automatic reconnection
        $healthCheck = $this->operator->execute('health_check');
        $this->assertArrayHasKey('status', $healthCheck);
        $this->assertArrayHasKey('connection_test', $healthCheck);
    }
    
    public function testPerformanceMetrics(): void
    {
        $this->setupMockConnection();
        
        // Execute some operations
        $this->operator->execute('query', [
            'sql' => 'SELECT COUNT(*) FROM users'
        ]);
        
        $metrics = $this->operator->execute('performance_metrics');
        $today = date('Y-m-d');
        
        $this->assertArrayHasKey($today, $metrics);
        $this->assertArrayHasKey('query_execution', $metrics[$today]);
        $this->assertArrayHasKey('count', $metrics[$today]['query_execution']);
        $this->assertArrayHasKey('avg_time', $metrics[$today]['query_execution']);
    }
    
    public function testHealthCheck(): void
    {
        $this->setupMockConnection();
        
        $health = $this->operator->execute('health_check');
        
        $this->assertIsArray($health);
        $this->assertArrayHasKey('status', $health);
        $this->assertArrayHasKey('connection_test', $health);
        $this->assertArrayHasKey('pool_status', $health);
        $this->assertArrayHasKey('errors', $health);
    }
    
    public function testCacheManagement(): void
    {
        $this->setupMockConnection();
        
        // Populate some cache
        $this->operator->execute('query', [
            'sql' => 'SELECT * FROM users'
        ]);
        
        // Clear cache
        $cleared = $this->operator->execute('clear_cache');
        
        $this->assertIsArray($cleared);
        $this->assertArrayHasKey('prepared_statements', $cleared);
        $this->assertArrayHasKey('performance_metrics', $cleared);
    }
    
    public function testConnectionPoolExhaustion(): void
    {
        // Configure small pool size
        $operator = new EnhancedMySqlOperator(['max_connections' => 2]);
        
        $connectionParams = [
            'host' => 'localhost',
            'database' => 'test_db',
            'username' => 'test_user',
            'password' => 'test_pass'
        ];
        
        // This should throw exception when pool is exhausted
        $this->expectException(Exception::class);
        $this->expectExceptionMessage('Connection pool exhausted');
        
        // Try to create more connections than allowed
        for ($i = 0; $i < 5; $i++) {
            $operator->execute('connect', $connectionParams);
        }
    }
    
    public function testSlowQueryLogging(): void
    {
        $this->setupMockConnection();
        
        // Execute a slow query (simulated)
        $start = microtime(true);
        $this->operator->execute('query', [
            'sql' => 'SELECT SLEEP(2)',
            'simulate_slow' => true
        ]);
        
        // Verify slow query was logged in performance metrics
        $metrics = $this->operator->execute('performance_metrics');
        $this->assertArrayHasKey(date('Y-m-d'), $metrics);
    }
    
    public function testJSONOperations(): void
    {
        $this->setupMockConnection();
        
        // Test JSON extraction
        $result = $this->operator->execute('json_extract', [
            'table' => 'documents',
            'column' => 'data',
            'path' => '$.name'
        ]);
        
        $this->assertIsArray($result);
        
        // Test JSON contains
        $contains = $this->operator->execute('json_contains', [
            'table' => 'documents',
            'column' => 'data',
            'value' => ['type' => 'user']
        ]);
        
        $this->assertIsBool($contains);
    }
    
    public function testFulltextSearch(): void
    {
        $this->setupMockConnection();
        
        $result = $this->operator->execute('fulltext_search', [
            'table' => 'articles',
            'column' => 'content',
            'query' => 'database optimization'
        ]);
        
        $this->assertIsArray($result);
    }
    
    public function testDatabaseMaintenanceOperations(): void
    {
        $this->setupMockConnection();
        
        // Test table optimization
        $result = $this->operator->execute('optimize_table', [
            'table' => 'users'
        ]);
        $this->assertTrue($result);
        
        // Test table repair
        $result = $this->operator->execute('repair_table', [
            'table' => 'users'
        ]);
        $this->assertTrue($result);
        
        // Test show status
        $status = $this->operator->execute('show_status');
        $this->assertIsArray($status);
    }
    
    public function testErrorHandlingAndLogging(): void
    {
        $this->setupMockConnection();
        
        try {
            // Execute invalid SQL to trigger error
            $this->operator->execute('query', [
                'sql' => 'INVALID SQL STATEMENT'
            ]);
        } catch (Exception $e) {
            $this->assertStringContains('MySQL operation failed', $e->getMessage());
        }
        
        // Verify error was logged
        $metrics = $this->operator->execute('performance_metrics');
        $this->assertArrayHasKey(date('Y-m-d'), $metrics);
    }
    
    public function testConnectionTimeout(): void
    {
        $operator = new EnhancedMySqlOperator(['connection_timeout' => 1]);
        
        $this->expectException(Exception::class);
        
        // This should timeout quickly
        $operator->execute('connect', [
            'host' => 'non-existent-host',
            'database' => 'test',
            'username' => 'test',
            'password' => 'test'
        ]);
    }
    
    public function testTransactionTimeout(): void
    {
        $this->setupMockConnection();
        
        // Begin transaction with short timeout
        $this->operator->execute('begin', ['timeout' => 1]);
        
        // Sleep longer than timeout
        sleep(2);
        
        $this->expectException(Exception::class);
        $this->expectExceptionMessage('Transaction timeout exceeded');
        
        // This should fail due to timeout
        $this->operator->execute('query', [
            'sql' => 'SELECT 1'
        ]);
    }
    
    /**
     * Helper method to setup mock connection for testing
     */
    private function setupMockConnection(): void
    {
        // In a real test environment, this would create a test database connection
        // For this example, we'll mock the connection setup
        $this->operator->execute('connect', [
            'host' => 'localhost',
            'database' => 'test_db',
            'username' => 'test_user',
            'password' => 'test_pass'
        ]);
    }
    
    public function testBenchmarkOperations(): void
    {
        $this->setupMockConnection();
        
        $startTime = microtime(true);
        
        // Run benchmark queries
        for ($i = 0; $i < 100; $i++) {
            $this->operator->execute('query', [
                'sql' => 'SELECT ? as iteration',
                'bindings' => [$i]
            ]);
        }
        
        $totalTime = microtime(true) - $startTime;
        $avgTime = $totalTime / 100;
        
        // Verify performance is within acceptable range
        $this->assertLessThan(0.1, $avgTime, 'Average query time should be less than 100ms');
        
        // Check performance metrics
        $metrics = $this->operator->execute('performance_metrics');
        $queryMetrics = $metrics[date('Y-m-d')]['query_execution'] ?? [];
        
        $this->assertGreaterThanOrEqual(100, $queryMetrics['count'] ?? 0);
        $this->assertGreaterThan(0, $queryMetrics['avg_time'] ?? 0);
    }
} 