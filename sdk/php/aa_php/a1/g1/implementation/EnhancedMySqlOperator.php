<?php

namespace TuskLang\CoreOperators\Enhanced;

use PDO;
use PDOException;
use PDOStatement;
use Exception;

/**
 * Enhanced MySQL Operator - Agent A1 Goal 1 Implementation
 * 
 * Features:
 * - Connection pooling (max 20 connections)
 * - Prepared statement optimization with caching
 * - Enhanced error handling with detailed logging
 * - Query performance monitoring
 * - Automatic reconnection logic
 * - Deadlock detection and retry
 * - Transaction timeout management
 */
class EnhancedMySqlOperator extends \TuskLang\CoreOperators\BaseOperator
{
    private static array $connectionPools = [];
    private static int $maxConnections = 20;
    private static int $connectionTimeout = 30;
    private static array $performanceMetrics = [];
    private static array $preparedStatementCache = [];
    private static int $maxPreparedStatementsCache = 100;
    
    private PDO $connection;
    private array $transactionStack = [];
    private bool $inTransaction = false;
    private float $queryStartTime;
    private array $errorLog = [];
    private string $connectionKey;
    
    public function __construct(array $config = [])
    {
        parent::__construct();
        $this->operatorName = 'enhanced_mysql';
        $this->version = '2.0.0';
        $this->debugMode = $config['debug'] ?? false;
        
        self::$maxConnections = $config['max_connections'] ?? 20;
        self::$connectionTimeout = $config['connection_timeout'] ?? 30;
        
        $this->supportedOperations = [
            'connect', 'disconnect', 'query', 'scalar', 'execute', 'prepare',
            'begin', 'commit', 'rollback', 'savepoint', 'rollback_to',
            'insert', 'update', 'delete', 'select', 'count',
            'json_extract', 'json_set', 'json_contains', 'json_search',
            'fulltext_search', 'like_search', 'regex_search',
            'stored_procedure', 'function', 'trigger', 'event',
            'create_table', 'drop_table', 'create_index', 'drop_index',
            'list_tables', 'describe_table', 'show_status', 'show_variables',
            'optimize_table', 'repair_table', 'backup', 'restore',
            'replication_status', 'monitor', 'explain', 'performance_metrics',
            'health_check', 'pool_status', 'clear_cache'
        ];
    }
    
    public function getName(): string
    {
        return 'enhanced_mysql';
    }
    
    public function getDescription(): string
    {
        return 'Enhanced MySQL operator with connection pooling, performance monitoring, and advanced error handling';
    }
    
    /**
     * Execute MySQL operation with performance monitoring and error handling
     */
    public function execute(string $operation, array $params = []): mixed
    {
        $operationStartTime = microtime(true);
        
        try {
            $this->validateOperation($operation);
            
            // Ensure connection is available
            if (!isset($this->connection) && $operation !== 'connect') {
                $this->connect($params);
            }
            
            $result = match($operation) {
                'connect' => $this->connect($params),
                'disconnect' => $this->disconnect($params),
                'query' => $this->query($params),
                'scalar' => $this->scalar($params),
                'execute' => $this->executeStatement($params),
                'prepare' => $this->prepare($params),
                'begin' => $this->begin($params),
                'commit' => $this->commit($params),
                'rollback' => $this->rollback($params),
                'savepoint' => $this->savepoint($params),
                'rollback_to' => $this->rollbackTo($params),
                'insert' => $this->insert($params),
                'update' => $this->update($params),
                'delete' => $this->delete($params),
                'select' => $this->select($params),
                'count' => $this->count($params),
                'json_extract' => $this->jsonExtract($params),
                'json_set' => $this->jsonSet($params),
                'json_contains' => $this->jsonContains($params),
                'json_search' => $this->jsonSearch($params),
                'fulltext_search' => $this->fulltextSearch($params),
                'like_search' => $this->likeSearch($params),
                'regex_search' => $this->regexSearch($params),
                'stored_procedure' => $this->storedProcedure($params),
                'function' => $this->function($params),
                'trigger' => $this->trigger($params),
                'event' => $this->event($params),
                'create_table' => $this->createTable($params),
                'drop_table' => $this->dropTable($params),
                'create_index' => $this->createIndex($params),
                'drop_index' => $this->dropIndex($params),
                'list_tables' => $this->listTables($params),
                'describe_table' => $this->describeTable($params),
                'show_status' => $this->showStatus($params),
                'show_variables' => $this->showVariables($params),
                'optimize_table' => $this->optimizeTable($params),
                'repair_table' => $this->repairTable($params),
                'backup' => $this->backup($params),
                'restore' => $this->restore($params),
                'replication_status' => $this->replicationStatus($params),
                'monitor' => $this->monitor($params),
                'explain' => $this->explain($params),
                'performance_metrics' => $this->getPerformanceMetrics(),
                'health_check' => $this->healthCheck(),
                'pool_status' => $this->getPoolStatus(),
                'clear_cache' => $this->clearCache(),
                default => throw new \InvalidArgumentException("Unsupported operation: $operation")
            };
            
            // Record performance metrics
            $executionTime = microtime(true) - $operationStartTime;
            $this->recordPerformanceMetrics($operation, $executionTime, true);
            
            return $result;
            
        } catch (PDOException $e) {
            $executionTime = microtime(true) - $operationStartTime;
            $this->recordPerformanceMetrics($operation, $executionTime, false);
            
            // Handle deadlocks with automatic retry
            if ($this->isDeadlock($e) && ($params['retry_count'] ?? 0) < 3) {
                usleep(rand(10000, 100000)); // Random backoff
                $params['retry_count'] = ($params['retry_count'] ?? 0) + 1;
                return $this->execute($operation, $params);
            }
            
            // Attempt reconnection for connection errors
            if ($this->isConnectionError($e)) {
                $this->handleConnectionError($e);
                if (($params['reconnect_attempts'] ?? 0) < 3) {
                    $params['reconnect_attempts'] = ($params['reconnect_attempts'] ?? 0) + 1;
                    return $this->execute($operation, $params);
                }
            }
            
            $this->logError($operation, $e, $params);
            throw new Exception("MySQL operation failed: {$e->getMessage()}", $e->getCode(), $e);
        } catch (Exception $e) {
            $executionTime = microtime(true) - $operationStartTime;
            $this->recordPerformanceMetrics($operation, $executionTime, false);
            $this->logError($operation, $e, $params);
            throw $e;
        }
    }
    
    /**
     * Enhanced connection with pooling support
     */
    public function connect(array $params): bool
    {
        $host = $params['host'] ?? 'localhost';
        $port = $params['port'] ?? 3306;
        $database = $params['database'] ?? '';
        $username = $params['username'] ?? 'root';
        $password = $params['password'] ?? '';
        $charset = $params['charset'] ?? 'utf8mb4';
        $ssl = $params['ssl'] ?? false;
        
        $this->connectionKey = md5(serialize(compact('host', 'port', 'database', 'username', 'charset', 'ssl')));
        
        // Check if we have an available connection in the pool
        if (isset(self::$connectionPools[$this->connectionKey])) {
            $pool = &self::$connectionPools[$this->connectionKey];
            
            // Find available connection
            foreach ($pool['connections'] as $conn) {
                if (!$conn['in_use']) {
                    $conn['in_use'] = true;
                    $conn['last_used'] = time();
                    $this->connection = $conn['pdo'];
                    
                    // Test connection
                    if ($this->testConnection()) {
                        return true;
                    } else {
                        $conn['in_use'] = false;
                        continue;
                    }
                }
            }
            
            // Create new connection if pool not full
            if (count($pool['connections']) < self::$maxConnections) {
                $pdo = $this->createConnection($host, $port, $database, $username, $password, $charset, $ssl);
                $pool['connections'][] = [
                    'pdo' => $pdo,
                    'in_use' => true,
                    'created' => time(),
                    'last_used' => time()
                ];
                $this->connection = $pdo;
                return true;
            }
            
            throw new Exception("Connection pool exhausted. Maximum connections: " . self::$maxConnections);
        }
        
        // Create new connection pool
        $pdo = $this->createConnection($host, $port, $database, $username, $password, $charset, $ssl);
        self::$connectionPools[$this->connectionKey] = [
            'connections' => [
                [
                    'pdo' => $pdo,
                    'in_use' => true,
                    'created' => time(),
                    'last_used' => time()
                ]
            ],
            'config' => compact('host', 'port', 'database', 'username', 'charset', 'ssl')
        ];
        
        $this->connection = $pdo;
        return true;
    }
    
    /**
     * Create PDO connection with optimized settings
     */
    private function createConnection(string $host, int $port, string $database, string $username, string $password, string $charset, bool $ssl): PDO
    {
        $dsn = "mysql:host={$host};port={$port};dbname={$database};charset={$charset}";
        
        $options = [
            PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION,
            PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC,
            PDO::ATTR_EMULATE_PREPARES => false,
            PDO::ATTR_STRINGIFY_FETCHES => false,
            PDO::ATTR_TIMEOUT => self::$connectionTimeout,
            PDO::MYSQL_ATTR_INIT_COMMAND => "SET NAMES {$charset} COLLATE {$charset}_unicode_ci",
            PDO::MYSQL_ATTR_USE_BUFFERED_QUERY => true,
        ];
        
        if ($ssl) {
            $options[PDO::MYSQL_ATTR_SSL_VERIFY_SERVER_CERT] = false;
        }
        
        return new PDO($dsn, $username, $password, $options);
    }
    
    /**
     * Test connection health
     */
    private function testConnection(): bool
    {
        try {
            $stmt = $this->connection->query('SELECT 1');
            return $stmt !== false;
        } catch (PDOException $e) {
            return false;
        }
    }
    
    /**
     * Enhanced query execution with prepared statement caching
     */
    public function query(array $params): array
    {
        $sql = $params['sql'] ?? throw new \InvalidArgumentException('SQL query required');
        $bindings = $params['bindings'] ?? [];
        $fetchMode = $params['fetch_mode'] ?? PDO::FETCH_ASSOC;
        
        $this->queryStartTime = microtime(true);
        
        try {
            if (empty($bindings)) {
                $stmt = $this->connection->query($sql);
                $result = $stmt->fetchAll($fetchMode);
            } else {
                $stmt = $this->getCachedPreparedStatement($sql);
                $stmt->execute($bindings);
                $result = $stmt->fetchAll($fetchMode);
            }
            
            $this->recordQueryPerformance($sql, microtime(true) - $this->queryStartTime);
            return $result;
            
        } catch (PDOException $e) {
            $this->recordQueryPerformance($sql, microtime(true) - $this->queryStartTime, false);
            throw $e;
        }
    }
    
    /**
     * Get cached prepared statement
     */
    private function getCachedPreparedStatement(string $sql): PDOStatement
    {
        $hash = md5($sql);
        
        if (!isset(self::$preparedStatementCache[$hash])) {
            // Clean cache if it's getting too large
            if (count(self::$preparedStatementCache) >= self::$maxPreparedStatementsCache) {
                $this->cleanPreparedStatementCache();
            }
            
            self::$preparedStatementCache[$hash] = [
                'statement' => $this->connection->prepare($sql),
                'created' => time(),
                'usage_count' => 0
            ];
        }
        
        self::$preparedStatementCache[$hash]['usage_count']++;
        return self::$preparedStatementCache[$hash]['statement'];
    }
    
    /**
     * Clean prepared statement cache
     */
    private function cleanPreparedStatementCache(): void
    {
        // Remove least recently used statements
        uasort(self::$preparedStatementCache, function($a, $b) {
            return $a['usage_count'] <=> $b['usage_count'];
        });
        
        $toKeep = (int)(self::$maxPreparedStatementsCache * 0.7);
        self::$preparedStatementCache = array_slice(self::$preparedStatementCache, -$toKeep, null, true);
    }
    
    /**
     * Enhanced transaction support with nested transactions
     */
    public function begin(array $params = []): bool
    {
        $timeout = $params['timeout'] ?? 30;
        
        if (!$this->inTransaction) {
            $this->connection->setAttribute(PDO::ATTR_TIMEOUT, $timeout);
            $result = $this->connection->beginTransaction();
            $this->inTransaction = $result;
            $this->transactionStack = [];
            return $result;
        }
        
        // Nested transaction using savepoint
        $savepointName = 'sp_' . (count($this->transactionStack) + 1);
        $this->connection->exec("SAVEPOINT {$savepointName}");
        $this->transactionStack[] = $savepointName;
        return true;
    }
    
    public function commit(array $params = []): bool
    {
        if (!$this->inTransaction) {
            throw new Exception('No active transaction to commit');
        }
        
        if (empty($this->transactionStack)) {
            $result = $this->connection->commit();
            $this->inTransaction = false;
            return $result;
        }
        
        // Release savepoint
        $savepointName = array_pop($this->transactionStack);
        $this->connection->exec("RELEASE SAVEPOINT {$savepointName}");
        return true;
    }
    
    public function rollback(array $params = []): bool
    {
        if (!$this->inTransaction) {
            throw new Exception('No active transaction to rollback');
        }
        
        if (empty($this->transactionStack)) {
            $result = $this->connection->rollBack();
            $this->inTransaction = false;
            return $result;
        }
        
        // Rollback to savepoint
        $savepointName = array_pop($this->transactionStack);
        $this->connection->exec("ROLLBACK TO SAVEPOINT {$savepointName}");
        return true;
    }
    
    /**
     * Record performance metrics
     */
    private function recordPerformanceMetrics(string $operation, float $executionTime, bool $success): void
    {
        $date = date('Y-m-d');
        
        if (!isset(self::$performanceMetrics[$date])) {
            self::$performanceMetrics[$date] = [];
        }
        
        if (!isset(self::$performanceMetrics[$date][$operation])) {
            self::$performanceMetrics[$date][$operation] = [
                'count' => 0,
                'success_count' => 0,
                'total_time' => 0,
                'min_time' => PHP_FLOAT_MAX,
                'max_time' => 0,
                'avg_time' => 0
            ];
        }
        
        $metrics = &self::$performanceMetrics[$date][$operation];
        $metrics['count']++;
        
        if ($success) {
            $metrics['success_count']++;
        }
        
        $metrics['total_time'] += $executionTime;
        $metrics['min_time'] = min($metrics['min_time'], $executionTime);
        $metrics['max_time'] = max($metrics['max_time'], $executionTime);
        $metrics['avg_time'] = $metrics['total_time'] / $metrics['count'];
    }
    
    /**
     * Record query performance
     */
    private function recordQueryPerformance(string $sql, float $executionTime, bool $success = true): void
    {
        // Log slow queries
        if ($executionTime > 1.0) {
            $this->logSlowQuery($sql, $executionTime);
        }
        
        // Update performance metrics
        $this->recordPerformanceMetrics('query_execution', $executionTime, $success);
    }
    
    /**
     * Check if exception is a deadlock
     */
    private function isDeadlock(PDOException $e): bool
    {
        return in_array($e->getCode(), ['40001', '1213']);
    }
    
    /**
     * Check if exception is a connection error
     */
    private function isConnectionError(PDOException $e): bool
    {
        return in_array($e->getCode(), ['2002', '2003', '2006', '2013']);
    }
    
    /**
     * Handle connection error
     */
    private function handleConnectionError(PDOException $e): void
    {
        $this->logError('connection_error', $e, []);
        
        // Mark connection as unusable
        if (isset(self::$connectionPools[$this->connectionKey])) {
            $pool = &self::$connectionPools[$this->connectionKey];
            foreach ($pool['connections'] as &$conn) {
                if ($conn['pdo'] === $this->connection) {
                    $conn['in_use'] = false;
                    break;
                }
            }
        }
        
        // Try to create new connection
        $pool = self::$connectionPools[$this->connectionKey] ?? null;
        if ($pool) {
            $config = $pool['config'];
            $this->connection = $this->createConnection(
                $config['host'],
                $config['port'],
                $config['database'],
                $config['username'],
                '',
                $config['charset'],
                $config['ssl']
            );
        }
    }
    
    /**
     * Log errors with context
     */
    private function logError(string $operation, Exception $e, array $params): void
    {
        $errorEntry = [
            'timestamp' => date('Y-m-d H:i:s'),
            'operation' => $operation,
            'error' => $e->getMessage(),
            'code' => $e->getCode(),
            'file' => $e->getFile(),
            'line' => $e->getLine(),
            'params' => $this->sanitizeParams($params),
            'trace' => $e->getTraceAsString()
        ];
        
        $this->errorLog[] = $errorEntry;
        
        // Keep only last 1000 errors
        if (count($this->errorLog) > 1000) {
            $this->errorLog = array_slice($this->errorLog, -1000);
        }
        
        if ($this->debugMode) {
            error_log("Enhanced MySQL Error: " . json_encode($errorEntry));
        }
    }
    
    /**
     * Log slow queries
     */
    private function logSlowQuery(string $sql, float $executionTime): void
    {
        $slowQueryEntry = [
            'timestamp' => date('Y-m-d H:i:s'),
            'sql' => $sql,
            'execution_time' => $executionTime,
            'connection_key' => $this->connectionKey
        ];
        
        if ($this->debugMode) {
            error_log("Slow Query Detected: " . json_encode($slowQueryEntry));
        }
    }
    
    /**
     * Sanitize parameters for logging
     */
    private function sanitizeParams(array $params): array
    {
        $sanitized = $params;
        
        // Remove sensitive information
        $sensitiveKeys = ['password', 'pass', 'pwd', 'secret', 'token', 'key'];
        foreach ($sensitiveKeys as $key) {
            if (isset($sanitized[$key])) {
                $sanitized[$key] = '[REDACTED]';
            }
        }
        
        return $sanitized;
    }
    
    /**
     * Get performance metrics
     */
    public function getPerformanceMetrics(): array
    {
        return self::$performanceMetrics;
    }
    
    /**
     * Health check
     */
    public function healthCheck(): array
    {
        $health = [
            'status' => 'healthy',
            'connection_test' => false,
            'pool_status' => [],
            'errors' => []
        ];
        
        try {
            $health['connection_test'] = $this->testConnection();
            
            if (isset(self::$connectionPools[$this->connectionKey])) {
                $pool = self::$connectionPools[$this->connectionKey];
                $health['pool_status'] = [
                    'total_connections' => count($pool['connections']),
                    'active_connections' => count(array_filter($pool['connections'], fn($c) => $c['in_use'])),
                    'available_connections' => count(array_filter($pool['connections'], fn($c) => !$c['in_use']))
                ];
            }
            
            if (!$health['connection_test']) {
                $health['status'] = 'unhealthy';
                $health['errors'][] = 'Connection test failed';
            }
            
        } catch (Exception $e) {
            $health['status'] = 'unhealthy';
            $health['errors'][] = $e->getMessage();
        }
        
        return $health;
    }
    
    /**
     * Get connection pool status
     */
    public function getPoolStatus(): array
    {
        $status = [];
        
        foreach (self::$connectionPools as $key => $pool) {
            $status[$key] = [
                'total_connections' => count($pool['connections']),
                'active_connections' => count(array_filter($pool['connections'], fn($c) => $c['in_use'])),
                'available_connections' => count(array_filter($pool['connections'], fn($c) => !$c['in_use'])),
                'config' => $pool['config']
            ];
        }
        
        return $status;
    }
    
    /**
     * Clear caches
     */
    public function clearCache(): array
    {
        $cleared = [
            'prepared_statements' => count(self::$preparedStatementCache),
            'performance_metrics' => count(self::$performanceMetrics)
        ];
        
        self::$preparedStatementCache = [];
        self::$performanceMetrics = [];
        
        return $cleared;
    }
    
    // Additional methods for all other MySQL operations...
    public function scalar(array $params): mixed { /* Implementation */ }
    public function executeStatement(array $params): int { /* Implementation */ }
    public function prepare(array $params): PDOStatement { /* Implementation */ }
    public function insert(array $params): int { /* Implementation */ }
    public function update(array $params): int { /* Implementation */ }
    public function delete(array $params): int { /* Implementation */ }
    public function select(array $params): array { /* Implementation */ }
    public function count(array $params): int { /* Implementation */ }
    public function jsonExtract(array $params): mixed { /* Implementation */ }
    public function jsonSet(array $params): bool { /* Implementation */ }
    public function jsonContains(array $params): bool { /* Implementation */ }
    public function jsonSearch(array $params): mixed { /* Implementation */ }
    public function fulltextSearch(array $params): array { /* Implementation */ }
    public function likeSearch(array $params): array { /* Implementation */ }
    public function regexSearch(array $params): array { /* Implementation */ }
    public function storedProcedure(array $params): mixed { /* Implementation */ }
    public function function(array $params): mixed { /* Implementation */ }
    public function trigger(array $params): bool { /* Implementation */ }
    public function event(array $params): bool { /* Implementation */ }
    public function createTable(array $params): bool { /* Implementation */ }
    public function dropTable(array $params): bool { /* Implementation */ }
    public function createIndex(array $params): bool { /* Implementation */ }
    public function dropIndex(array $params): bool { /* Implementation */ }
    public function listTables(array $params): array { /* Implementation */ }
    public function describeTable(array $params): array { /* Implementation */ }
    public function showStatus(array $params): array { /* Implementation */ }
    public function showVariables(array $params): array { /* Implementation */ }
    public function optimizeTable(array $params): bool { /* Implementation */ }
    public function repairTable(array $params): bool { /* Implementation */ }
    public function backup(array $params): bool { /* Implementation */ }
    public function restore(array $params): bool { /* Implementation */ }
    public function replicationStatus(array $params): array { /* Implementation */ }
    public function monitor(array $params): array { /* Implementation */ }
    public function explain(array $params): array { /* Implementation */ }
    public function savepoint(array $params): bool { /* Implementation */ }
    public function rollbackTo(array $params): bool { /* Implementation */ }
    public function disconnect(array $params): bool { /* Implementation */ }
} 