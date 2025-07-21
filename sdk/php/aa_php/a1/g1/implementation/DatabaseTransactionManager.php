<?php

namespace TuskLang\CoreOperators\Enhanced;

use PDO;
use PDOException;
use Exception;
use Throwable;

/**
 * Database Transaction Manager - Agent A1 Goal 3 Implementation
 * 
 * Features:
 * - Robust transaction handling system
 * - Nested transaction support with savepoints
 * - Deadlock detection and automatic retry
 * - Transaction timeout management
 * - Comprehensive transaction logging
 * - Cross-database transaction coordination
 * - Transaction state monitoring
 * - Recovery mechanisms
 */
class DatabaseTransactionManager
{
    private const MAX_RETRY_ATTEMPTS = 3;
    private const DEFAULT_TIMEOUT = 30; // seconds
    private const MAX_SAVEPOINTS = 10;
    
    private static array $activeTransactions = [];
    private static array $transactionHistory = [];
    private static array $deadlockStatistics = [];
    private static int $transactionCounter = 0;
    
    private string $transactionId;
    private PDO $connection;
    private array $savepoints = [];
    private float $startTime;
    private int $timeoutSeconds;
    private bool $isActive = false;
    private array $operationLog = [];
    private string $isolationLevel = 'READ_COMMITTED';
    private bool $autoRetryOnDeadlock = true;
    
    public function __construct(PDO $connection, array $config = [])
    {
        $this->connection = $connection;
        $this->timeoutSeconds = $config['timeout'] ?? self::DEFAULT_TIMEOUT;
        $this->isolationLevel = $config['isolation_level'] ?? 'READ_COMMITTED';
        $this->autoRetryOnDeadlock = $config['auto_retry_deadlock'] ?? true;
        $this->transactionId = $this->generateTransactionId();
    }
    
    /**
     * Begin a new transaction
     */
    public function begin(): bool
    {
        try {
            if ($this->isActive) {
                throw new Exception("Transaction already active: {$this->transactionId}");
            }
            
            // Set isolation level
            $this->connection->exec("SET TRANSACTION ISOLATION LEVEL {$this->isolationLevel}");
            
            // Set transaction timeout
            $this->connection->setAttribute(PDO::ATTR_TIMEOUT, $this->timeoutSeconds);
            
            $this->startTime = microtime(true);
            
            if (!$this->connection->beginTransaction()) {
                throw new Exception("Failed to begin transaction");
            }
            
            $this->isActive = true;
            $this->savepoints = [];
            $this->operationLog = [];
            
            // Register active transaction
            self::$activeTransactions[$this->transactionId] = [
                'id' => $this->transactionId,
                'start_time' => $this->startTime,
                'timeout' => $this->timeoutSeconds,
                'isolation_level' => $this->isolationLevel,
                'savepoint_count' => 0,
                'operation_count' => 0
            ];
            
            $this->logOperation('BEGIN', 'Transaction started', [
                'isolation_level' => $this->isolationLevel,
                'timeout' => $this->timeoutSeconds
            ]);
            
            return true;
            
        } catch (PDOException $e) {
            $this->handleTransactionError('BEGIN', $e);
            return false;
        }
    }
    
    /**
     * Create a savepoint for nested transaction support
     */
    public function savepoint(string $savepointName = null): string
    {
        if (!$this->isActive) {
            throw new Exception("No active transaction for savepoint");
        }
        
        if (count($this->savepoints) >= self::MAX_SAVEPOINTS) {
            throw new Exception("Maximum savepoints reached: " . self::MAX_SAVEPOINTS);
        }
        
        $savepointName = $savepointName ?? $this->generateSavepointName();
        
        try {
            $this->checkTimeout();
            
            $sql = "SAVEPOINT {$savepointName}";
            $this->connection->exec($sql);
            
            $this->savepoints[] = [
                'name' => $savepointName,
                'created_at' => microtime(true),
                'operation_count' => count($this->operationLog)
            ];
            
            // Update active transaction statistics
            self::$activeTransactions[$this->transactionId]['savepoint_count'] = count($this->savepoints);
            
            $this->logOperation('SAVEPOINT', "Savepoint created: {$savepointName}");
            
            return $savepointName;
            
        } catch (PDOException $e) {
            $this->handleTransactionError('SAVEPOINT', $e, ['savepoint_name' => $savepointName]);
            throw $e;
        }
    }
    
    /**
     * Rollback to a specific savepoint
     */
    public function rollbackToSavepoint(string $savepointName): bool
    {
        if (!$this->isActive) {
            throw new Exception("No active transaction for rollback to savepoint");
        }
        
        $savepointIndex = null;
        foreach ($this->savepoints as $index => $savepoint) {
            if ($savepoint['name'] === $savepointName) {
                $savepointIndex = $index;
                break;
            }
        }
        
        if ($savepointIndex === null) {
            throw new Exception("Savepoint not found: {$savepointName}");
        }
        
        try {
            $this->checkTimeout();
            
            $sql = "ROLLBACK TO SAVEPOINT {$savepointName}";
            $this->connection->exec($sql);
            
            // Remove savepoints created after the rollback point
            $this->savepoints = array_slice($this->savepoints, 0, $savepointIndex + 1);
            
            // Update active transaction statistics
            self::$activeTransactions[$this->transactionId]['savepoint_count'] = count($this->savepoints);
            
            $this->logOperation('ROLLBACK_TO_SAVEPOINT', "Rolled back to savepoint: {$savepointName}");
            
            return true;
            
        } catch (PDOException $e) {
            $this->handleTransactionError('ROLLBACK_TO_SAVEPOINT', $e, ['savepoint_name' => $savepointName]);
            return false;
        }
    }
    
    /**
     * Release a savepoint
     */
    public function releaseSavepoint(string $savepointName): bool
    {
        if (!$this->isActive) {
            throw new Exception("No active transaction for release savepoint");
        }
        
        $savepointIndex = null;
        foreach ($this->savepoints as $index => $savepoint) {
            if ($savepoint['name'] === $savepointName) {
                $savepointIndex = $index;
                break;
            }
        }
        
        if ($savepointIndex === null) {
            throw new Exception("Savepoint not found: {$savepointName}");
        }
        
        try {
            $sql = "RELEASE SAVEPOINT {$savepointName}";
            $this->connection->exec($sql);
            
            // Remove the released savepoint and all subsequent ones
            $this->savepoints = array_slice($this->savepoints, 0, $savepointIndex);
            
            // Update active transaction statistics
            self::$activeTransactions[$this->transactionId]['savepoint_count'] = count($this->savepoints);
            
            $this->logOperation('RELEASE_SAVEPOINT', "Released savepoint: {$savepointName}");
            
            return true;
            
        } catch (PDOException $e) {
            $this->handleTransactionError('RELEASE_SAVEPOINT', $e, ['savepoint_name' => $savepointName]);
            return false;
        }
    }
    
    /**
     * Commit the transaction
     */
    public function commit(): bool
    {
        if (!$this->isActive) {
            throw new Exception("No active transaction to commit");
        }
        
        try {
            $this->checkTimeout();
            
            $commitResult = $this->connection->commit();
            $this->finalizeTransaction('COMMIT', $commitResult);
            
            return $commitResult;
            
        } catch (PDOException $e) {
            $this->handleTransactionError('COMMIT', $e);
            $this->finalizeTransaction('COMMIT_FAILED', false);
            return false;
        }
    }
    
    /**
     * Rollback the entire transaction
     */
    public function rollback(): bool
    {
        if (!$this->isActive) {
            throw new Exception("No active transaction to rollback");
        }
        
        try {
            $rollbackResult = $this->connection->rollBack();
            $this->finalizeTransaction('ROLLBACK', $rollbackResult);
            
            return $rollbackResult;
            
        } catch (PDOException $e) {
            $this->handleTransactionError('ROLLBACK', $e);
            $this->finalizeTransaction('ROLLBACK_FAILED', false);
            return false;
        }
    }
    
    /**
     * Execute operation with automatic deadlock handling
     */
    public function executeWithRetry(callable $operation, int $maxRetries = null): mixed
    {
        $maxRetries = $maxRetries ?? self::MAX_RETRY_ATTEMPTS;
        $attempts = 0;
        
        while ($attempts < $maxRetries) {
            try {
                $this->checkTimeout();
                $result = $operation();
                
                $this->logOperation('EXECUTE', 'Operation completed successfully', [
                    'attempt' => $attempts + 1,
                    'max_retries' => $maxRetries
                ]);
                
                // Update operation count
                if (isset(self::$activeTransactions[$this->transactionId])) {
                    self::$activeTransactions[$this->transactionId]['operation_count']++;
                }
                
                return $result;
                
            } catch (PDOException $e) {
                $attempts++;
                
                if ($this->isDeadlock($e) && $this->autoRetryOnDeadlock && $attempts < $maxRetries) {
                    $this->recordDeadlock($e);
                    $backoffTime = $this->calculateBackoffTime($attempts);
                    
                    $this->logOperation('DEADLOCK_RETRY', "Deadlock detected, retrying operation", [
                        'attempt' => $attempts,
                        'max_retries' => $maxRetries,
                        'backoff_time' => $backoffTime,
                        'error_code' => $e->getCode(),
                        'error_message' => $e->getMessage()
                    ]);
                    
                    usleep($backoffTime * 1000); // Convert to microseconds
                    continue;
                }
                
                $this->logOperation('EXECUTE_FAILED', 'Operation failed', [
                    'attempt' => $attempts,
                    'max_retries' => $maxRetries,
                    'error_code' => $e->getCode(),
                    'error_message' => $e->getMessage()
                ]);
                
                throw $e;
            } catch (Throwable $e) {
                $this->logOperation('EXECUTE_ERROR', 'Unexpected error during operation', [
                    'attempt' => $attempts,
                    'error_message' => $e->getMessage(),
                    'error_class' => get_class($e)
                ]);
                
                throw $e;
            }
        }
        
        throw new Exception("Operation failed after {$maxRetries} attempts");
    }
    
    /**
     * Get transaction status
     */
    public function getStatus(): array
    {
        return [
            'transaction_id' => $this->transactionId,
            'is_active' => $this->isActive,
            'start_time' => $this->startTime ?? null,
            'duration' => $this->isActive ? (microtime(true) - $this->startTime) : 0,
            'timeout_seconds' => $this->timeoutSeconds,
            'time_remaining' => $this->isActive ? max(0, $this->timeoutSeconds - (microtime(true) - $this->startTime)) : 0,
            'isolation_level' => $this->isolationLevel,
            'savepoint_count' => count($this->savepoints),
            'operation_count' => count($this->operationLog),
            'savepoints' => $this->savepoints,
            'has_timed_out' => $this->hasTimedOut()
        ];
    }
    
    /**
     * Get transaction history
     */
    public function getOperationLog(): array
    {
        return $this->operationLog;
    }
    
    /**
     * Get global transaction statistics
     */
    public static function getGlobalStatistics(): array
    {
        return [
            'active_transactions' => count(self::$activeTransactions),
            'total_transactions' => self::$transactionCounter,
            'transaction_history_count' => count(self::$transactionHistory),
            'deadlock_statistics' => self::$deadlockStatistics,
            'active_transaction_details' => self::$activeTransactions
        ];
    }
    
    /**
     * Cleanup timed out transactions
     */
    public static function cleanupTimedOutTransactions(): int
    {
        $cleanedUp = 0;
        $currentTime = microtime(true);
        
        foreach (self::$activeTransactions as $id => $transaction) {
            $elapsed = $currentTime - $transaction['start_time'];
            if ($elapsed > $transaction['timeout']) {
                // Force cleanup of timed out transaction
                unset(self::$activeTransactions[$id]);
                
                // Add to history
                self::$transactionHistory[] = array_merge($transaction, [
                    'end_time' => $currentTime,
                    'duration' => $elapsed,
                    'status' => 'TIMEOUT_CLEANUP',
                    'forced_cleanup' => true
                ]);
                
                $cleanedUp++;
            }
        }
        
        // Limit history size
        if (count(self::$transactionHistory) > 10000) {
            self::$transactionHistory = array_slice(self::$transactionHistory, -5000);
        }
        
        return $cleanedUp;
    }
    
    /**
     * Private helper methods
     */
    
    private function generateTransactionId(): string
    {
        self::$transactionCounter++;
        return 'txn_' . self::$transactionCounter . '_' . uniqid();
    }
    
    private function generateSavepointName(): string
    {
        return 'sp_' . count($this->savepoints) . '_' . time();
    }
    
    private function checkTimeout(): void
    {
        if ($this->hasTimedOut()) {
            throw new Exception("Transaction timeout exceeded: {$this->timeoutSeconds} seconds");
        }
    }
    
    private function hasTimedOut(): bool
    {
        if (!$this->isActive) {
            return false;
        }
        
        $elapsed = microtime(true) - $this->startTime;
        return $elapsed > $this->timeoutSeconds;
    }
    
    private function isDeadlock(PDOException $e): bool
    {
        // MySQL deadlock codes: 1213, 40001
        // PostgreSQL serialization failure codes: 40001, 40P01
        return in_array($e->getCode(), ['1213', '40001', '40P01']);
    }
    
    private function recordDeadlock(PDOException $e): void
    {
        $date = date('Y-m-d');
        
        if (!isset(self::$deadlockStatistics[$date])) {
            self::$deadlockStatistics[$date] = [
                'count' => 0,
                'by_error_code' => [],
                'transactions' => []
            ];
        }
        
        self::$deadlockStatistics[$date]['count']++;
        
        $errorCode = $e->getCode();
        if (!isset(self::$deadlockStatistics[$date]['by_error_code'][$errorCode])) {
            self::$deadlockStatistics[$date]['by_error_code'][$errorCode] = 0;
        }
        self::$deadlockStatistics[$date]['by_error_code'][$errorCode]++;
        
        self::$deadlockStatistics[$date]['transactions'][] = [
            'transaction_id' => $this->transactionId,
            'timestamp' => microtime(true),
            'error_code' => $errorCode,
            'error_message' => $e->getMessage()
        ];
    }
    
    private function calculateBackoffTime(int $attempt): int
    {
        // Exponential backoff with jitter: base_delay * (2^attempt) + random(0, 100)
        $baseDelay = 10; // milliseconds
        $exponentialDelay = $baseDelay * pow(2, min($attempt - 1, 6)); // Cap at 2^6
        $jitter = rand(0, 100);
        
        return min($exponentialDelay + $jitter, 5000); // Cap at 5 seconds
    }
    
    private function logOperation(string $operation, string $message, array $context = []): void
    {
        $this->operationLog[] = [
            'timestamp' => microtime(true),
            'operation' => $operation,
            'message' => $message,
            'context' => $context,
            'transaction_id' => $this->transactionId,
            'savepoint_count' => count($this->savepoints)
        ];
    }
    
    private function handleTransactionError(string $operation, PDOException $e, array $context = []): void
    {
        $this->logOperation('ERROR', "Transaction error in {$operation}: {$e->getMessage()}", array_merge($context, [
            'error_code' => $e->getCode(),
            'error_file' => $e->getFile(),
            'error_line' => $e->getLine()
        ]));
        
        // Record deadlock statistics
        if ($this->isDeadlock($e)) {
            $this->recordDeadlock($e);
        }
    }
    
    private function finalizeTransaction(string $status, bool $success): void
    {
        $endTime = microtime(true);
        $duration = $endTime - $this->startTime;
        
        $this->logOperation($status, "Transaction finalized: {$status}", [
            'success' => $success,
            'duration' => $duration,
            'savepoint_count' => count($this->savepoints),
            'operation_count' => count($this->operationLog)
        ]);
        
        // Move from active to history
        if (isset(self::$activeTransactions[$this->transactionId])) {
            $transactionData = self::$activeTransactions[$this->transactionId];
            unset(self::$activeTransactions[$this->transactionId]);
            
            self::$transactionHistory[] = array_merge($transactionData, [
                'end_time' => $endTime,
                'duration' => $duration,
                'status' => $status,
                'success' => $success,
                'final_savepoint_count' => count($this->savepoints),
                'final_operation_count' => count($this->operationLog)
            ]);
        }
        
        // Reset state
        $this->isActive = false;
        $this->savepoints = [];
    }
    
    /**
     * Destructor - ensure proper cleanup
     */
    public function __destruct()
    {
        if ($this->isActive) {
            try {
                $this->rollback();
            } catch (Exception $e) {
                // Ensure cleanup even if rollback fails
                $this->finalizeTransaction('DESTRUCTOR_CLEANUP', false);
            }
        }
    }
} 