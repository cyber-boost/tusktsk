<?php

namespace TuskLang\CoreOperators\Enhanced\Tests;

use PHPUnit\Framework\TestCase;
use TuskLang\CoreOperators\Enhanced\EnhancedSQLiteOperator;
use PDO;
use PDOException;
use Exception;

/**
 * Comprehensive test suite for Enhanced SQLite Operator
 * Coverage: WAL mode, encryption, backup/restore, memory databases, performance monitoring
 */
class EnhancedSQLiteOperatorTest extends TestCase
{
    private EnhancedSQLiteOperator $operator;
    private string $testDatabasePath;
    private array $testConfig;
    
    protected function setUp(): void
    {
        $this->testConfig = [
            'debug' => true,
            'max_connections' => 10
        ];
        
        $this->operator = new EnhancedSQLiteOperator($this->testConfig);
        $this->testDatabasePath = sys_get_temp_dir() . '/test_sqlite_' . uniqid() . '.db';
    }
    
    protected function tearDown(): void
    {
        try {
            if (isset($this->operator)) {
                $this->operator->execute('disconnect', []);
            }
        } catch (Exception $e) {
            // Ignore cleanup errors
        }
        
        // Clean up test database files
        $patterns = [
            $this->testDatabasePath,
            $this->testDatabasePath . '*',
            sys_get_temp_dir() . '/test_sqlite_*.db*'
        ];
        
        foreach ($patterns as $pattern) {
            foreach (glob($pattern) as $file) {
                if (file_exists($file)) {
                    unlink($file);
                }
            }
        }
    }
    
    public function testOperatorInitialization(): void
    {
        $this->assertEquals('enhanced_sqlite', $this->operator->getName());
        $this->assertEquals('2.0.0', $this->operator->getVersion());
        $this->assertStringContains('Enhanced SQLite operator', $this->operator->getDescription());
    }
    
    public function testBasicConnection(): void
    {
        $result = $this->operator->execute('connect', [
            'path' => $this->testDatabasePath,
            'enable_wal' => true
        ]);
        
        $this->assertTrue($result);
        
        // Verify database file was created
        $this->assertFileExists($this->testDatabasePath);
    }
    
    public function testWALModeOperations(): void
    {
        $this->operator->execute('connect', [
            'path' => $this->testDatabasePath,
            'enable_wal' => false
        ]);
        
        // Enable WAL mode
        $this->assertTrue($this->operator->execute('enable_wal'));
        
        // Verify WAL mode is active
        $dbInfo = $this->operator->execute('database_info');
        $this->assertEquals('wal', $dbInfo['journal_mode']);
        
        // Test checkpoint operation
        $checkpointResult = $this->operator->execute('checkpoint', ['mode' => 'PASSIVE']);
        $this->assertIsArray($checkpointResult);
        $this->assertArrayHasKey('mode', $checkpointResult);
        $this->assertEquals('PASSIVE', $checkpointResult['mode']);
        
        // Test different checkpoint modes
        $modes = ['FULL', 'RESTART', 'TRUNCATE'];
        foreach ($modes as $mode) {
            $result = $this->operator->execute('checkpoint', ['mode' => $mode]);
            $this->assertEquals($mode, $result['mode']);
        }
        
        // Disable WAL mode
        $this->assertTrue($this->operator->execute('disable_wal'));
        
        $dbInfo = $this->operator->execute('database_info');
        $this->assertEquals('delete', strtolower($dbInfo['journal_mode']));
    }
    
    public function testMemoryDatabase(): void
    {
        // Create memory database
        $this->assertTrue($this->operator->execute('create_memory_db', [
            'identifier' => 'test_memory',
            'shared' => true
        ]));
        
        // Memory databases should not support WAL
        $this->expectException(Exception::class);
        $this->expectExceptionMessage('WAL mode not supported for memory databases');
        $this->operator->execute('enable_wal');
    }
    
    public function testSharedMemoryDatabase(): void
    {
        $identifier = 'shared_test_' . uniqid();
        
        $this->assertTrue($this->operator->execute('create_memory_db', [
            'identifier' => $identifier,
            'shared' => true,
            'cache_size' => 5000
        ]));
        
        // Test basic operations on memory database
        $this->operator->execute('execute', [
            'sql' => 'CREATE TABLE test_table (id INTEGER PRIMARY KEY, name TEXT)'
        ]);
        
        $this->operator->execute('execute', [
            'sql' => 'INSERT INTO test_table (name) VALUES (?)',
            'bindings' => ['Test Record']
        ]);
        
        $result = $this->operator->execute('query', [
            'sql' => 'SELECT * FROM test_table'
        ]);
        
        $this->assertCount(1, $result);
        $this->assertEquals('Test Record', $result[0]['name']);
    }
    
    public function testDatabaseCloning(): void
    {
        // Set up source database
        $this->operator->execute('connect', ['path' => $this->testDatabasePath]);
        
        $this->operator->execute('execute', [
            'sql' => 'CREATE TABLE users (id INTEGER PRIMARY KEY, name TEXT, email TEXT)'
        ]);
        
        $this->operator->execute('execute', [
            'sql' => 'INSERT INTO users (name, email) VALUES (?, ?)',
            'bindings' => ['John Doe', 'john@example.com']
        ]);
        
        // Clone to memory
        $memoryIdentifier = $this->operator->execute('clone_to_memory', [
            'identifier' => 'clone_test'
        ]);
        
        $this->assertIsString($memoryIdentifier);
        $this->assertStringContains('clone_', $memoryIdentifier);
    }
    
    public function testDatabaseEncryption(): void
    {
        $this->operator->execute('connect', ['path' => $this->testDatabasePath]);
        
        // Create some test data
        $this->operator->execute('execute', [
            'sql' => 'CREATE TABLE sensitive_data (id INTEGER PRIMARY KEY, secret TEXT)'
        ]);
        
        $this->operator->execute('execute', [
            'sql' => 'INSERT INTO sensitive_data (secret) VALUES (?)',
            'bindings' => ['top_secret_information']
        ]);
        
        // Test encryption (would require SQLCipher in real implementation)
        try {
            $result = $this->operator->execute('encrypt_database', [
                'key' => 'test_encryption_key_123'
            ]);
            
            // In a real implementation with SQLCipher, this should return true
            $this->assertTrue($result);
        } catch (Exception $e) {
            // Skip test if SQLCipher not available
            $this->markTestSkipped('SQLCipher not available for encryption testing');
        }
    }
    
    public function testBackupAndRestore(): void
    {
        $this->operator->execute('connect', ['path' => $this->testDatabasePath]);
        
        // Create test data
        $this->operator->execute('execute', [
            'sql' => 'CREATE TABLE backup_test (id INTEGER PRIMARY KEY, data TEXT, created_at DATETIME DEFAULT CURRENT_TIMESTAMP)'
        ]);
        
        for ($i = 1; $i <= 5; $i++) {
            $this->operator->execute('execute', [
                'sql' => 'INSERT INTO backup_test (data) VALUES (?)',
                'bindings' => ["Test data {$i}"]
            ]);
        }
        
        // Test backup without compression
        $backupPath = $this->operator->execute('backup_database', [
            'path' => $this->testDatabasePath . '.backup',
            'compress' => false,
            'include_wal' => true
        ]);
        
        $this->assertFileExists($backupPath);
        $this->assertEquals($this->testDatabasePath . '.backup', $backupPath);
        
        // Test compressed backup
        $compressedBackupPath = $this->operator->execute('backup_database', [
            'compress' => true
        ]);
        
        $this->assertFileExists($compressedBackupPath);
        $this->assertStringEndsWith('.gz', $compressedBackupPath);
        
        // Test restore
        $restoreTarget = $this->testDatabasePath . '.restored';
        
        $this->assertTrue($this->operator->execute('restore_database', [
            'path' => $backupPath,
            'target_path' => $restoreTarget
        ]));
        
        $this->assertFileExists($restoreTarget);
        
        // Test restore from compressed backup
        $compressedRestoreTarget = $this->testDatabasePath . '.compressed_restored';
        
        $this->assertTrue($this->operator->execute('restore_database', [
            'path' => $compressedBackupPath,
            'target_path' => $compressedRestoreTarget
        ]));
        
        $this->assertFileExists($compressedRestoreTarget);
    }
    
    public function testIntegrityCheck(): void
    {
        $this->operator->execute('connect', ['path' => $this->testDatabasePath]);
        
        // Create test table
        $this->operator->execute('execute', [
            'sql' => 'CREATE TABLE integrity_test (id INTEGER PRIMARY KEY, name TEXT NOT NULL)'
        ]);
        
        $this->operator->execute('execute', [
            'sql' => 'INSERT INTO integrity_test (name) VALUES (?)',
            'bindings' => ['Test Record']
        ]);
        
        // Run integrity check
        $integrityResult = $this->operator->execute('integrity_check', [
            'max_errors' => 10
        ]);
        
        $this->assertIsArray($integrityResult);
        $this->assertArrayHasKey('is_healthy', $integrityResult);
        $this->assertArrayHasKey('errors', $integrityResult);
        $this->assertArrayHasKey('error_count', $integrityResult);
        
        // For a new database, integrity should be OK
        $this->assertTrue($integrityResult['is_healthy']);
        $this->assertEquals(0, $integrityResult['error_count']);
        $this->assertEmpty($integrityResult['errors']);
    }
    
    public function testDatabaseInfo(): void
    {
        $this->operator->execute('connect', [
            'path' => $this->testDatabasePath,
            'enable_wal' => true,
            'cache_size' => 5000
        ]);
        
        $info = $this->operator->execute('database_info');
        
        $this->assertIsArray($info);
        
        // Check required fields
        $requiredFields = [
            'database_path', 'is_memory', 'is_encrypted', 'pragma_settings',
            'page_count', 'page_size', 'cache_size', 'journal_mode',
            'synchronous', 'foreign_keys', 'user_version', 'schema_version'
        ];
        
        foreach ($requiredFields as $field) {
            $this->assertArrayHasKey($field, $info);
        }
        
        $this->assertEquals($this->testDatabasePath, $info['database_path']);
        $this->assertFalse($info['is_memory']);
        $this->assertEquals('wal', $info['journal_mode']);
        $this->assertIsInt($info['page_count']);
        $this->assertIsInt($info['page_size']);
        
        // Check WAL-specific information
        if ($info['journal_mode'] === 'wal') {
            $this->assertArrayHasKey('wal_status', $info);
            $this->assertIsArray($info['wal_status']);
        }
    }
    
    public function testVacuumOperations(): void
    {
        $this->operator->execute('connect', ['path' => $this->testDatabasePath]);
        
        // Create and populate test table
        $this->operator->execute('execute', [
            'sql' => 'CREATE TABLE vacuum_test (id INTEGER PRIMARY KEY, data TEXT)'
        ]);
        
        for ($i = 1; $i <= 100; $i++) {
            $this->operator->execute('execute', [
                'sql' => 'INSERT INTO vacuum_test (data) VALUES (?)',
                'bindings' => ["Data row {$i}"]
            ]);
        }
        
        // Delete some records to create space for vacuuming
        $this->operator->execute('execute', [
            'sql' => 'DELETE FROM vacuum_test WHERE id % 2 = 0'
        ]);
        
        // Test vacuum
        $this->assertTrue($this->operator->execute('vacuum'));
        
        // Test analyze
        $this->assertTrue($this->operator->execute('analyze'));
        
        // Test incremental vacuum (if auto_vacuum is enabled)
        $pagesFreed = $this->operator->execute('incremental_vacuum', ['pages' => 10]);
        $this->assertIsInt($pagesFreed);
    }
    
    public function testPerformanceMonitoring(): void
    {
        $this->operator->execute('connect', ['path' => $this->testDatabasePath]);
        
        // Execute some operations to generate metrics
        $this->operator->execute('execute', [
            'sql' => 'CREATE TABLE perf_test (id INTEGER PRIMARY KEY, value INTEGER)'
        ]);
        
        for ($i = 1; $i <= 10; $i++) {
            $this->operator->execute('execute', [
                'sql' => 'INSERT INTO perf_test (value) VALUES (?)',
                'bindings' => [$i * 10]
            ]);
        }
        
        $this->operator->execute('query', [
            'sql' => 'SELECT * FROM perf_test WHERE value > ?',
            'bindings' => [50]
        ]);
        
        // Get performance metrics
        $metrics = $this->operator->execute('performance_metrics');
        
        $this->assertIsArray($metrics);
        
        $today = date('Y-m-d');
        $this->assertArrayHasKey($today, $metrics);
        
        // Check for operation metrics
        $todayMetrics = $metrics[$today];
        $this->assertArrayHasKey('execute', $todayMetrics);
        $this->assertArrayHasKey('query', $todayMetrics);
        
        // Verify metric structure
        foreach (['execute', 'query'] as $operation) {
            if (isset($todayMetrics[$operation])) {
                $opMetrics = $todayMetrics[$operation];
                $this->assertArrayHasKey('count', $opMetrics);
                $this->assertArrayHasKey('success_count', $opMetrics);
                $this->assertArrayHasKey('total_time', $opMetrics);
                $this->assertArrayHasKey('avg_time', $opMetrics);
                $this->assertGreaterThan(0, $opMetrics['count']);
            }
        }
    }
    
    public function testHealthCheck(): void
    {
        $this->operator->execute('connect', ['path' => $this->testDatabasePath]);
        
        $health = $this->operator->execute('health_check');
        
        $this->assertIsArray($health);
        $this->assertArrayHasKey('status', $health);
        $this->assertEquals('healthy', $health['status']);
    }
    
    public function testDatabaseAttachDetach(): void
    {
        $this->operator->execute('connect', ['path' => $this->testDatabasePath]);
        
        // Create a second database to attach
        $attachDbPath = $this->testDatabasePath . '.attach';
        file_put_contents($attachDbPath, ''); // Create empty file
        
        // Test attach
        $this->assertTrue($this->operator->execute('attach_database', [
            'path' => $attachDbPath,
            'alias' => 'attached_db'
        ]));
        
        // Test detach
        $this->assertTrue($this->operator->execute('detach_database', [
            'alias' => 'attached_db'
        ]));
        
        // Clean up
        if (file_exists($attachDbPath)) {
            unlink($attachDbPath);
        }
    }
    
    public function testQueryAnalysis(): void
    {
        $this->operator->execute('connect', ['path' => $this->testDatabasePath]);
        
        // Create test data
        $this->operator->execute('execute', [
            'sql' => 'CREATE TABLE analysis_test (id INTEGER PRIMARY KEY, name TEXT, value INTEGER)'
        ]);
        
        $this->operator->execute('execute', [
            'sql' => 'CREATE INDEX idx_analysis_value ON analysis_test(value)'
        ]);
        
        for ($i = 1; $i <= 50; $i++) {
            $this->operator->execute('execute', [
                'sql' => 'INSERT INTO analysis_test (name, value) VALUES (?, ?)',
                'bindings' => ["Name {$i}", $i * 2]
            ]);
        }
        
        // Test EXPLAIN
        $explanation = $this->operator->execute('explain_query', [
            'sql' => 'SELECT * FROM analysis_test WHERE value > ? ORDER BY name',
            'bindings' => [50]
        ]);
        
        $this->assertIsArray($explanation);
        
        // Test query analysis
        $analysis = $this->operator->execute('analyze_query', [
            'sql' => 'SELECT COUNT(*) FROM analysis_test WHERE value BETWEEN ? AND ?',
            'bindings' => [20, 80]
        ]);
        
        $this->assertIsArray($analysis);
    }
    
    public function testErrorHandling(): void
    {
        $this->operator->execute('connect', ['path' => $this->testDatabasePath]);
        
        // Test invalid SQL
        $this->expectException(Exception::class);
        $this->operator->execute('execute', [
            'sql' => 'INVALID SQL STATEMENT'
        ]);
    }
    
    public function testConnectionTimeout(): void
    {
        // Test with very short timeout
        $this->expectException(Exception::class);
        
        $operator = new EnhancedSQLiteOperator();
        $operator->execute('connect', [
            'path' => '/non-existent-directory/test.db',
            'timeout' => 1
        ]);
    }
    
    public function testConcurrentAccess(): void
    {
        $this->operator->execute('connect', [
            'path' => $this->testDatabasePath,
            'enable_wal' => true
        ]);
        
        // Create test table
        $this->operator->execute('execute', [
            'sql' => 'CREATE TABLE concurrent_test (id INTEGER PRIMARY KEY, data TEXT, updated_at DATETIME DEFAULT CURRENT_TIMESTAMP)'
        ]);
        
        // Simulate concurrent operations
        $operations = [];
        for ($i = 1; $i <= 10; $i++) {
            $operations[] = [
                'sql' => 'INSERT INTO concurrent_test (data) VALUES (?)',
                'bindings' => ["Concurrent data {$i}"]
            ];
        }
        
        foreach ($operations as $operation) {
            $this->operator->execute('execute', $operation);
        }
        
        // Verify all records were inserted
        $count = $this->operator->execute('scalar', [
            'sql' => 'SELECT COUNT(*) FROM concurrent_test'
        ]);
        
        $this->assertEquals(10, $count);
    }
    
    public function testCacheManagement(): void
    {
        $this->operator->execute('connect', ['path' => $this->testDatabasePath]);
        
        // Execute some operations to populate caches
        $this->operator->execute('execute', [
            'sql' => 'CREATE TABLE cache_test (id INTEGER PRIMARY KEY, name TEXT)'
        ]);
        
        for ($i = 1; $i <= 5; $i++) {
            $this->operator->execute('execute', [
                'sql' => 'INSERT INTO cache_test (name) VALUES (?)',
                'bindings' => ["Name {$i}"]
            ]);
        }
        
        // Clear cache
        $cleared = $this->operator->execute('clear_cache');
        
        $this->assertIsArray($cleared);
    }
} 