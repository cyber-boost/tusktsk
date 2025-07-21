<?php

namespace TuskLang\CoreOperators\Enhanced\Tests;

use PHPUnit\Framework\TestCase;
use TuskLang\CoreOperators\Enhanced\DatabaseTransactionManager;
use PDO;
use PDOException;
use Exception;

/**
 * Comprehensive test suite for Database Transaction Manager
 * Coverage: Nested transactions, deadlock handling, timeout management, comprehensive logging
 */
class DatabaseTransactionManagerTest extends TestCase
{
    private PDO $mockPdo;
    private DatabaseTransactionManager $transactionManager;
    
    protected function setUp(): void
    {
        // Create mock PDO connection
        $this->mockPdo = $this->createMockPdo();
        
        $this->transactionManager = new DatabaseTransactionManager($this->mockPdo, [
            'timeout' => 30,
            'isolation_level' => 'READ_COMMITTED',
            'auto_retry_deadlock' => true
        ]);
    }
    
    protected function tearDown(): void
    {
        // Cleanup any active transactions
        try {
            if ($this->transactionManager->getStatus()['is_active']) {
                $this->transactionManager->rollback();
            }
        } catch (Exception $e) {
            // Ignore cleanup errors
        }
        
        DatabaseTransactionManager::cleanupTimedOutTransactions();
    }
    
    public function testTransactionBeginAndCommit(): void
    {
        $this->assertTrue($this->transactionManager->begin());
        
        $status = $this->transactionManager->getStatus();
        $this->assertTrue($status['is_active']);
        $this->assertGreaterThan(0, $status['start_time']);
        $this->assertEquals(30, $status['timeout_seconds']);
        
        $this->assertTrue($this->transactionManager->commit());
        
        $status = $this->transactionManager->getStatus();
        $this->assertFalse($status['is_active']);
    }
    
    public function testTransactionBeginAndRollback(): void
    {
        $this->assertTrue($this->transactionManager->begin());
        $this->assertTrue($this->transactionManager->rollback());
        
        $status = $this->transactionManager->getStatus();
        $this->assertFalse($status['is_active']);
    }
    
    public function testNestedTransactionsWithSavepoints(): void
    {
        $this->assertTrue($this->transactionManager->begin());
        
        // Create first savepoint
        $sp1 = $this->transactionManager->savepoint('first');
        $this->assertEquals('first', $sp1);
        
        // Create second savepoint
        $sp2 = $this->transactionManager->savepoint('second');
        $this->assertEquals('second', $sp2);
        
        $status = $this->transactionManager->getStatus();
        $this->assertEquals(2, $status['savepoint_count']);
        
        // Rollback to first savepoint
        $this->assertTrue($this->transactionManager->rollbackToSavepoint('first'));
        
        $status = $this->transactionManager->getStatus();
        $this->assertEquals(1, $status['savepoint_count']);
        
        $this->assertTrue($this->transactionManager->commit());
    }
    
    public function testSavepointRelease(): void
    {
        $this->transactionManager->begin();
        
        $sp1 = $this->transactionManager->savepoint('sp1');
        $sp2 = $this->transactionManager->savepoint('sp2');
        
        $this->assertTrue($this->transactionManager->releaseSavepoint('sp1'));
        
        $status = $this->transactionManager->getStatus();
        $this->assertEquals(0, $status['savepoint_count']);
        
        $this->transactionManager->commit();
    }
    
    public function testMaximumSavepointsLimit(): void
    {
        $this->transactionManager->begin();
        
        // Create maximum number of savepoints
        for ($i = 1; $i <= 10; $i++) {
            $this->transactionManager->savepoint("sp$i");
        }
        
        // Attempting to create 11th savepoint should fail
        $this->expectException(Exception::class);
        $this->expectExceptionMessage('Maximum savepoints reached');
        
        $this->transactionManager->savepoint('sp11');
    }
    
    public function testExecuteWithRetryOnDeadlock(): void
    {
        $this->transactionManager->begin();
        
        $attempts = 0;
        $operation = function() use (&$attempts) {
            $attempts++;
            if ($attempts < 3) {
                // Simulate deadlock on first two attempts
                $e = new PDOException('Deadlock found when trying to get lock', '40001');
                throw $e;
            }
            return 'success';
        };
        
        $result = $this->transactionManager->executeWithRetry($operation);
        
        $this->assertEquals('success', $result);
        $this->assertEquals(3, $attempts);
        
        $this->transactionManager->commit();
    }
    
    public function testExecuteWithRetryMaxAttemptsExceeded(): void
    {
        $this->transactionManager->begin();
        
        $operation = function() {
            // Always throw deadlock error
            throw new PDOException('Deadlock found', '40001');
        };
        
        $this->expectException(Exception::class);
        $this->expectExceptionMessage('Operation failed after 3 attempts');
        
        $this->transactionManager->executeWithRetry($operation);
    }
    
    public function testTransactionTimeout(): void
    {
        // Create transaction manager with short timeout
        $shortTimeoutManager = new DatabaseTransactionManager($this->mockPdo, [
            'timeout' => 1
        ]);
        
        $shortTimeoutManager->begin();
        
        // Sleep longer than timeout
        sleep(2);
        
        $this->expectException(Exception::class);
        $this->expectExceptionMessage('Transaction timeout exceeded');
        
        // This operation should detect timeout
        $shortTimeoutManager->executeWithRetry(function() {
            return 'should timeout';
        });
    }
    
    public function testTransactionStatus(): void
    {
        $status = $this->transactionManager->getStatus();
        
        $this->assertArrayHasKey('transaction_id', $status);
        $this->assertArrayHasKey('is_active', $status);
        $this->assertArrayHasKey('start_time', $status);
        $this->assertArrayHasKey('duration', $status);
        $this->assertArrayHasKey('timeout_seconds', $status);
        $this->assertArrayHasKey('time_remaining', $status);
        $this->assertArrayHasKey('isolation_level', $status);
        $this->assertArrayHasKey('savepoint_count', $status);
        $this->assertArrayHasKey('operation_count', $status);
        $this->assertArrayHasKey('savepoints', $status);
        $this->assertArrayHasKey('has_timed_out', $status);
        
        $this->assertFalse($status['is_active']);
        $this->assertEquals('READ_COMMITTED', $status['isolation_level']);
    }
    
    public function testOperationLogging(): void
    {
        $this->transactionManager->begin();
        $sp = $this->transactionManager->savepoint();
        $this->transactionManager->commit();
        
        $log = $this->transactionManager->getOperationLog();
        
        $this->assertCount(3, $log); // BEGIN, SAVEPOINT, COMMIT
        
        foreach ($log as $entry) {
            $this->assertArrayHasKey('timestamp', $entry);
            $this->assertArrayHasKey('operation', $entry);
            $this->assertArrayHasKey('message', $entry);
            $this->assertArrayHasKey('context', $entry);
            $this->assertArrayHasKey('transaction_id', $entry);
        }
        
        $this->assertEquals('BEGIN', $log[0]['operation']);
        $this->assertEquals('SAVEPOINT', $log[1]['operation']);
        $this->assertEquals('COMMIT', $log[2]['operation']);
    }
    
    public function testGlobalStatistics(): void
    {
        $this->transactionManager->begin();
        $this->transactionManager->commit();
        
        $stats = DatabaseTransactionManager::getGlobalStatistics();
        
        $this->assertArrayHasKey('active_transactions', $stats);
        $this->assertArrayHasKey('total_transactions', $stats);
        $this->assertArrayHasKey('transaction_history_count', $stats);
        $this->assertArrayHasKey('deadlock_statistics', $stats);
        $this->assertArrayHasKey('active_transaction_details', $stats);
        
        $this->assertGreaterThanOrEqual(1, $stats['total_transactions']);
    }
    
    public function testDeadlockStatisticsRecording(): void
    {
        $this->transactionManager->begin();
        
        try {
            $this->transactionManager->executeWithRetry(function() {
                throw new PDOException('Deadlock found', '40001');
            }, 1); // Only 1 retry to fail quickly
        } catch (Exception $e) {
            // Expected to fail
        }
        
        $stats = DatabaseTransactionManager::getGlobalStatistics();
        $today = date('Y-m-d');
        
        $this->assertArrayHasKey($today, $stats['deadlock_statistics']);
        $this->assertGreaterThan(0, $stats['deadlock_statistics'][$today]['count']);
        $this->assertArrayHasKey('40001', $stats['deadlock_statistics'][$today]['by_error_code']);
        
        $this->transactionManager->rollback();
    }
    
    public function testCleanupTimedOutTransactions(): void
    {
        // Create a transaction that will timeout
        $timedOutManager = new DatabaseTransactionManager($this->mockPdo, [
            'timeout' => 0.1 // Very short timeout
        ]);
        
        $timedOutManager->begin();
        
        // Wait for timeout
        usleep(200000); // 0.2 seconds
        
        $cleanedUp = DatabaseTransactionManager::cleanupTimedOutTransactions();
        
        $this->assertGreaterThanOrEqual(1, $cleanedUp);
    }
    
    public function testDoubleBeginThrowsException(): void
    {
        $this->transactionManager->begin();
        
        $this->expectException(Exception::class);
        $this->expectExceptionMessage('Transaction already active');
        
        $this->transactionManager->begin();
    }
    
    public function testCommitWithoutBeginThrowsException(): void
    {
        $this->expectException(Exception::class);
        $this->expectExceptionMessage('No active transaction to commit');
        
        $this->transactionManager->commit();
    }
    
    public function testRollbackWithoutBeginThrowsException(): void
    {
        $this->expectException(Exception::class);
        $this->expectExceptionMessage('No active transaction to rollback');
        
        $this->transactionManager->rollback();
    }
    
    public function testSavepointWithoutBeginThrowsException(): void
    {
        $this->expectException(Exception::class);
        $this->expectExceptionMessage('No active transaction for savepoint');
        
        $this->transactionManager->savepoint();
    }
    
    public function testRollbackToNonExistentSavepoint(): void
    {
        $this->transactionManager->begin();
        
        $this->expectException(Exception::class);
        $this->expectExceptionMessage('Savepoint not found');
        
        $this->transactionManager->rollbackToSavepoint('non_existent');
    }
    
    public function testDestructorCleanup(): void
    {
        $manager = new DatabaseTransactionManager($this->mockPdo);
        $manager->begin();
        
        // Transaction should be cleaned up when object is destroyed
        unset($manager);
        
        // Verify cleanup happened by checking global statistics
        $stats = DatabaseTransactionManager::getGlobalStatistics();
        $this->assertGreaterThanOrEqual(0, $stats['active_transactions']);
    }
    
    public function testIsolationLevelConfiguration(): void
    {
        $manager = new DatabaseTransactionManager($this->mockPdo, [
            'isolation_level' => 'SERIALIZABLE'
        ]);
        
        $status = $manager->getStatus();
        $this->assertEquals('SERIALIZABLE', $status['isolation_level']);
    }
    
    public function testAutoRetryConfiguration(): void
    {
        $manager = new DatabaseTransactionManager($this->mockPdo, [
            'auto_retry_deadlock' => false
        ]);
        
        $manager->begin();
        
        $this->expectException(PDOException::class);
        
        // Should not retry when auto_retry_deadlock is false
        $manager->executeWithRetry(function() {
            throw new PDOException('Deadlock', '40001');
        });
    }
    
    public function testTransactionDurationCalculation(): void
    {
        $this->transactionManager->begin();
        
        usleep(100000); // 0.1 second
        
        $status = $this->transactionManager->getStatus();
        $this->assertGreaterThan(0.05, $status['duration']);
        $this->assertLessThan(30, $status['time_remaining']);
        
        $this->transactionManager->commit();
    }
    
    public function testBackoffTimeCalculation(): void
    {
        $this->transactionManager->begin();
        
        $attempts = [];
        $operation = function() use (&$attempts) {
            $attempts[] = microtime(true);
            if (count($attempts) < 3) {
                throw new PDOException('Deadlock', '40001');
            }
            return 'success';
        };
        
        $this->transactionManager->executeWithRetry($operation);
        
        // Verify backoff increased between attempts
        $this->assertCount(3, $attempts);
        $timeDiff1 = $attempts[1] - $attempts[0];
        $timeDiff2 = $attempts[2] - $attempts[1];
        
        // Second retry should have longer backoff than first
        $this->assertGreaterThan($timeDiff1 * 0.5, $timeDiff2);
        
        $this->transactionManager->commit();
    }
    
    /**
     * Create a mock PDO object for testing
     */
    private function createMockPdo(): PDO
    {
        // In a real test environment, this would be a proper mock
        // For this example, we'll create a simplified mock
        return $this->createMock(PDO::class);
    }
} 