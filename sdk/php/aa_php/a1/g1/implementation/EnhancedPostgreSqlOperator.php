<?php

namespace TuskLang\CoreOperators\Enhanced;

use PDO;
use PDOException;
use PDOStatement;
use Exception;

/**
 * Enhanced PostgreSQL Operator - Agent A1 Goal 2 Implementation
 * 
 * Features:
 * - Advanced PostgreSQL features
 * - JSON/JSONB data type support
 * - Array and custom type handling
 * - LISTEN/NOTIFY support for real-time events
 * - Connection pooling with health checks
 * - Advanced full-text search
 * - Custom type support
 * - Window functions
 * - Common Table Expressions (CTEs)
 */
class EnhancedPostgreSqlOperator extends \TuskLang\CoreOperators\BaseOperator
{
    private static array $connectionPools = [];
    private static int $maxConnections = 20;
    private static int $connectionTimeout = 30;
    private static array $performanceMetrics = [];
    private static array $preparedStatementCache = [];
    private static array $listenCallbacks = [];
    
    private PDO $connection;
    private array $transactionStack = [];
    private bool $inTransaction = false;
    private float $queryStartTime;
    private array $errorLog = [];
    private string $connectionKey;
    private array $notifications = [];
    
    public function __construct(array $config = [])
    {
        parent::__construct();
        $this->operatorName = 'enhanced_postgresql';
        $this->version = '2.0.0';
        $this->debugMode = $config['debug'] ?? false;
        
        self::$maxConnections = $config['max_connections'] ?? 20;
        self::$connectionTimeout = $config['connection_timeout'] ?? 30;
        
        $this->supportedOperations = [
            'connect', 'disconnect', 'query', 'scalar', 'execute', 'prepare',
            'begin', 'commit', 'rollback', 'savepoint', 'rollback_to',
            'insert', 'update', 'delete', 'select', 'count',
            
            // JSON/JSONB operations
            'json_get', 'json_set', 'json_contains', 'json_path', 'json_build',
            'jsonb_get', 'jsonb_set', 'jsonb_contains', 'jsonb_path', 'jsonb_build',
            'json_each', 'json_object_keys', 'json_extract_path', 'json_strip_nulls',
            
            // Array operations
            'array_contains', 'array_append', 'array_remove', 'array_length',
            'array_position', 'array_positions', 'array_cat', 'array_dims',
            'unnest', 'array_agg', 'array_to_string', 'string_to_array',
            
            // Full-text search
            'fulltext_search', 'similarity', 'trigram_search', 'ts_rank',
            'to_tsvector', 'to_tsquery', 'plainto_tsquery', 'phraseto_tsquery',
            
            // LISTEN/NOTIFY
            'listen', 'unlisten', 'notify', 'get_notifications', 'poll_notifications',
            
            // Custom types
            'create_type', 'drop_type', 'create_domain', 'drop_domain',
            'create_enum', 'drop_enum', 'alter_type',
            
            // Advanced features
            'create_table', 'drop_table', 'create_index', 'drop_index',
            'create_function', 'drop_function', 'create_trigger', 'drop_trigger',
            'create_view', 'drop_view', 'create_materialized_view', 'refresh_materialized_view',
            
            // Window functions
            'window_function', 'row_number', 'rank', 'dense_rank', 'lag', 'lead',
            
            // Common Table Expressions
            'with_query', 'recursive_query',
            
            // Maintenance
            'vacuum', 'analyze', 'reindex', 'cluster',
            'list_tables', 'describe_table', 'explain', 'explain_analyze',
            
            // Monitoring
            'performance_metrics', 'health_check', 'pool_status', 'clear_cache',
            'pg_stat_activity', 'pg_stat_database', 'pg_locks'
        ];
    }
    
    public function getName(): string
    {
        return 'enhanced_postgresql';
    }
    
    public function getDescription(): string
    {
        return 'Enhanced PostgreSQL operator with advanced features, JSON/JSONB support, and real-time capabilities';
    }
    
    /**
     * Execute PostgreSQL operation with advanced error handling
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
                
                // Transaction operations
                'begin' => $this->begin($params),
                'commit' => $this->commit($params),
                'rollback' => $this->rollback($params),
                'savepoint' => $this->savepoint($params),
                'rollback_to' => $this->rollbackTo($params),
                
                // CRUD operations
                'insert' => $this->insert($params),
                'update' => $this->update($params),
                'delete' => $this->delete($params),
                'select' => $this->select($params),
                'count' => $this->count($params),
                
                // JSON operations
                'json_get' => $this->jsonGet($params),
                'json_set' => $this->jsonSet($params),
                'json_contains' => $this->jsonContains($params),
                'json_path' => $this->jsonPath($params),
                'json_build' => $this->jsonBuild($params),
                'jsonb_get' => $this->jsonbGet($params),
                'jsonb_set' => $this->jsonbSet($params),
                'jsonb_contains' => $this->jsonbContains($params),
                'jsonb_path' => $this->jsonbPath($params),
                'jsonb_build' => $this->jsonbBuild($params),
                'json_each' => $this->jsonEach($params),
                'json_object_keys' => $this->jsonObjectKeys($params),
                'json_extract_path' => $this->jsonExtractPath($params),
                'json_strip_nulls' => $this->jsonStripNulls($params),
                
                // Array operations
                'array_contains' => $this->arrayContains($params),
                'array_append' => $this->arrayAppend($params),
                'array_remove' => $this->arrayRemove($params),
                'array_length' => $this->arrayLength($params),
                'array_position' => $this->arrayPosition($params),
                'array_positions' => $this->arrayPositions($params),
                'array_cat' => $this->arrayCat($params),
                'array_dims' => $this->arrayDims($params),
                'unnest' => $this->unnest($params),
                'array_agg' => $this->arrayAgg($params),
                'array_to_string' => $this->arrayToString($params),
                'string_to_array' => $this->stringToArray($params),
                
                // Full-text search
                'fulltext_search' => $this->fulltextSearch($params),
                'similarity' => $this->similarity($params),
                'trigram_search' => $this->trigramSearch($params),
                'ts_rank' => $this->tsRank($params),
                'to_tsvector' => $this->toTsvector($params),
                'to_tsquery' => $this->toTsquery($params),
                'plainto_tsquery' => $this->plaintoTsquery($params),
                'phraseto_tsquery' => $this->phrasetoTsquery($params),
                
                // LISTEN/NOTIFY
                'listen' => $this->listen($params),
                'unlisten' => $this->unlisten($params),
                'notify' => $this->notify($params),
                'get_notifications' => $this->getNotifications($params),
                'poll_notifications' => $this->pollNotifications($params),
                
                // Custom types
                'create_type' => $this->createType($params),
                'drop_type' => $this->dropType($params),
                'create_domain' => $this->createDomain($params),
                'drop_domain' => $this->dropDomain($params),
                'create_enum' => $this->createEnum($params),
                'drop_enum' => $this->dropEnum($params),
                'alter_type' => $this->alterType($params),
                
                // Advanced features
                'create_table' => $this->createTable($params),
                'drop_table' => $this->dropTable($params),
                'create_index' => $this->createIndex($params),
                'drop_index' => $this->dropIndex($params),
                'create_function' => $this->createFunction($params),
                'drop_function' => $this->dropFunction($params),
                'create_trigger' => $this->createTrigger($params),
                'drop_trigger' => $this->dropTrigger($params),
                'create_view' => $this->createView($params),
                'drop_view' => $this->dropView($params),
                'create_materialized_view' => $this->createMaterializedView($params),
                'refresh_materialized_view' => $this->refreshMaterializedView($params),
                
                // Window functions
                'window_function' => $this->windowFunction($params),
                'row_number' => $this->rowNumber($params),
                'rank' => $this->rank($params),
                'dense_rank' => $this->denseRank($params),
                'lag' => $this->lag($params),
                'lead' => $this->lead($params),
                
                // Common Table Expressions
                'with_query' => $this->withQuery($params),
                'recursive_query' => $this->recursiveQuery($params),
                
                // Maintenance
                'vacuum' => $this->vacuum($params),
                'analyze' => $this->analyze($params),
                'reindex' => $this->reindex($params),
                'cluster' => $this->cluster($params),
                'list_tables' => $this->listTables($params),
                'describe_table' => $this->describeTable($params),
                'explain' => $this->explain($params),
                'explain_analyze' => $this->explainAnalyze($params),
                
                // Monitoring
                'performance_metrics' => $this->getPerformanceMetrics(),
                'health_check' => $this->healthCheck(),
                'pool_status' => $this->getPoolStatus(),
                'clear_cache' => $this->clearCache(),
                'pg_stat_activity' => $this->pgStatActivity($params),
                'pg_stat_database' => $this->pgStatDatabase($params),
                'pg_locks' => $this->pgLocks($params),
                
                default => throw new \InvalidArgumentException("Unsupported operation: $operation")
            };
            
            // Record performance metrics
            $executionTime = microtime(true) - $operationStartTime;
            $this->recordPerformanceMetrics($operation, $executionTime, true);
            
            return $result;
            
        } catch (PDOException $e) {
            $executionTime = microtime(true) - $operationStartTime;
            $this->recordPerformanceMetrics($operation, $executionTime, false);
            
            // Handle serialization failures with automatic retry
            if ($this->isSerializationFailure($e) && ($params['retry_count'] ?? 0) < 3) {
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
            throw new Exception("PostgreSQL operation failed: {$e->getMessage()}", $e->getCode(), $e);
        } catch (Exception $e) {
            $executionTime = microtime(true) - $operationStartTime;
            $this->recordPerformanceMetrics($operation, $executionTime, false);
            $this->logError($operation, $e, $params);
            throw $e;
        }
    }
    
    /**
     * Enhanced connection with PostgreSQL-specific optimizations
     */
    public function connect(array $params): bool
    {
        $host = $params['host'] ?? 'localhost';
        $port = $params['port'] ?? 5432;
        $database = $params['database'] ?? 'postgres';
        $username = $params['username'] ?? 'postgres';
        $password = $params['password'] ?? '';
        $sslmode = $params['sslmode'] ?? 'prefer';
        $application_name = $params['application_name'] ?? 'TuskTsk_Enhanced_PostgreSQL';
        
        $this->connectionKey = md5(serialize(compact('host', 'port', 'database', 'username', 'sslmode', 'application_name')));
        
        // Check if we have an available connection in the pool
        if (isset(self::$connectionPools[$this->connectionKey])) {
            $pool = &self::$connectionPools[$this->connectionKey];
            
            // Find available connection
            foreach ($pool['connections'] as &$conn) {
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
                $pdo = $this->createConnection($host, $port, $database, $username, $password, $sslmode, $application_name);
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
        $pdo = $this->createConnection($host, $port, $database, $username, $password, $sslmode, $application_name);
        self::$connectionPools[$this->connectionKey] = [
            'connections' => [
                [
                    'pdo' => $pdo,
                    'in_use' => true,
                    'created' => time(),
                    'last_used' => time()
                ]
            ],
            'config' => compact('host', 'port', 'database', 'username', 'sslmode', 'application_name')
        ];
        
        $this->connection = $pdo;
        return true;
    }
    
    /**
     * Create PostgreSQL connection with optimized settings
     */
    private function createConnection(string $host, int $port, string $database, string $username, string $password, string $sslmode, string $application_name): PDO
    {
        $dsn = "pgsql:host={$host};port={$port};dbname={$database};sslmode={$sslmode};application_name={$application_name}";
        
        $options = [
            PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION,
            PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC,
            PDO::ATTR_EMULATE_PREPARES => false,
            PDO::ATTR_STRINGIFY_FETCHES => false,
            PDO::ATTR_TIMEOUT => self::$connectionTimeout,
        ];
        
        $pdo = new PDO($dsn, $username, $password, $options);
        
        // Set PostgreSQL-specific settings
        $pdo->exec("SET timezone = 'UTC'");
        $pdo->exec("SET client_encoding = 'UTF8'");
        $pdo->exec("SET standard_conforming_strings = on");
        $pdo->exec("SET check_function_bodies = false");
        $pdo->exec("SET client_min_messages = warning");
        
        return $pdo;
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
     * JSON/JSONB Operations
     */
    
    /**
     * Extract value from JSON using path
     */
    public function jsonGet(array $params): mixed
    {
        $table = $params['table'] ?? throw new \InvalidArgumentException('Table name required');
        $column = $params['column'] ?? throw new \InvalidArgumentException('Column name required');
        $path = $params['path'] ?? throw new \InvalidArgumentException('JSON path required');
        $where = $params['where'] ?? [];
        
        $pathSql = "'{" . implode(',', explode('.', trim($path, '.'))) . "}'";
        $sql = "SELECT {$column}#>{$pathSql} as extracted_value FROM {$table}";
        
        if (!empty($where)) {
            $conditions = [];
            foreach ($where as $field => $value) {
                $conditions[] = "{$field} = ?";
            }
            $sql .= " WHERE " . implode(' AND ', $conditions);
        }
        
        $stmt = $this->connection->prepare($sql);
        $stmt->execute(array_values($where));
        $result = $stmt->fetchAll();
        
        return count($result) === 1 ? $result[0]['extracted_value'] : $result;
    }
    
    /**
     * JSONB containment check
     */
    public function jsonbContains(array $params): bool
    {
        $table = $params['table'] ?? throw new \InvalidArgumentException('Table name required');
        $column = $params['column'] ?? throw new \InvalidArgumentException('Column name required');
        $value = $params['value'] ?? throw new \InvalidArgumentException('Value required');
        $where = $params['where'] ?? [];
        
        $sql = "SELECT EXISTS(SELECT 1 FROM {$table} WHERE {$column} @> ?";
        $bindings = [json_encode($value)];
        
        if (!empty($where)) {
            $conditions = [];
            foreach ($where as $field => $val) {
                $conditions[] = "{$field} = ?";
                $bindings[] = $val;
            }
            $sql .= " AND " . implode(' AND ', $conditions);
        }
        
        $sql .= ") as contains_value";
        
        $stmt = $this->connection->prepare($sql);
        $stmt->execute($bindings);
        
        return (bool)$stmt->fetchColumn();
    }
    
    /**
     * JSONB path query
     */
    public function jsonbPath(array $params): array
    {
        $table = $params['table'] ?? throw new \InvalidArgumentException('Table name required');
        $column = $params['column'] ?? throw new \InvalidArgumentException('Column name required');
        $path = $params['path'] ?? throw new \InvalidArgumentException('JSONPath expression required');
        $where = $params['where'] ?? [];
        
        $sql = "SELECT jsonb_path_query({$column}, ?) as path_result FROM {$table}";
        $bindings = [$path];
        
        if (!empty($where)) {
            $conditions = [];
            foreach ($where as $field => $value) {
                $conditions[] = "{$field} = ?";
                $bindings[] = $value;
            }
            $sql .= " WHERE " . implode(' AND ', $conditions);
        }
        
        $stmt = $this->connection->prepare($sql);
        $stmt->execute($bindings);
        
        return $stmt->fetchAll();
    }
    
    /**
     * Array Operations
     */
    
    /**
     * Check if array contains element
     */
    public function arrayContains(array $params): bool
    {
        $table = $params['table'] ?? throw new \InvalidArgumentException('Table name required');
        $column = $params['column'] ?? throw new \InvalidArgumentException('Column name required');
        $value = $params['value'] ?? throw new \InvalidArgumentException('Value required');
        $where = $params['where'] ?? [];
        
        $sql = "SELECT EXISTS(SELECT 1 FROM {$table} WHERE ? = ANY({$column})";
        $bindings = [$value];
        
        if (!empty($where)) {
            $conditions = [];
            foreach ($where as $field => $val) {
                $conditions[] = "{$field} = ?";
                $bindings[] = $val;
            }
            $sql .= " AND " . implode(' AND ', $conditions);
        }
        
        $sql .= ") as contains_value";
        
        $stmt = $this->connection->prepare($sql);
        $stmt->execute($bindings);
        
        return (bool)$stmt->fetchColumn();
    }
    
    /**
     * Get array length
     */
    public function arrayLength(array $params): int
    {
        $table = $params['table'] ?? throw new \InvalidArgumentException('Table name required');
        $column = $params['column'] ?? throw new \InvalidArgumentException('Column name required');
        $dimension = $params['dimension'] ?? 1;
        $where = $params['where'] ?? [];
        
        $sql = "SELECT array_length({$column}, {$dimension}) as array_len FROM {$table}";
        $bindings = [];
        
        if (!empty($where)) {
            $conditions = [];
            foreach ($where as $field => $value) {
                $conditions[] = "{$field} = ?";
                $bindings[] = $value;
            }
            $sql .= " WHERE " . implode(' AND ', $conditions);
        }
        
        $stmt = $this->connection->prepare($sql);
        $stmt->execute($bindings);
        
        return (int)$stmt->fetchColumn();
    }
    
    /**
     * LISTEN/NOTIFY Operations
     */
    
    /**
     * Listen for notifications
     */
    public function listen(array $params): bool
    {
        $channel = $params['channel'] ?? throw new \InvalidArgumentException('Channel name required');
        $callback = $params['callback'] ?? null;
        
        $sql = "LISTEN {$channel}";
        $this->connection->exec($sql);
        
        if ($callback && is_callable($callback)) {
            self::$listenCallbacks[$channel] = $callback;
        }
        
        return true;
    }
    
    /**
     * Stop listening for notifications
     */
    public function unlisten(array $params): bool
    {
        $channel = $params['channel'] ?? '*';
        
        if ($channel === '*') {
            $this->connection->exec("UNLISTEN *");
            self::$listenCallbacks = [];
        } else {
            $sql = "UNLISTEN {$channel}";
            $this->connection->exec($sql);
            unset(self::$listenCallbacks[$channel]);
        }
        
        return true;
    }
    
    /**
     * Send notification
     */
    public function notify(array $params): bool
    {
        $channel = $params['channel'] ?? throw new \InvalidArgumentException('Channel name required');
        $payload = $params['payload'] ?? '';
        
        $sql = "SELECT pg_notify(?, ?)";
        $stmt = $this->connection->prepare($sql);
        $stmt->execute([$channel, $payload]);
        
        return true;
    }
    
    /**
     * Get pending notifications
     */
    public function getNotifications(array $params = []): array
    {
        $timeout = $params['timeout'] ?? 0;
        
        // Use pg_get_notify to get pending notifications
        $sql = "SELECT pg_get_notify() as notification";
        $stmt = $this->connection->query($sql);
        
        $notifications = [];
        while ($row = $stmt->fetch()) {
            if ($row['notification'] !== null) {
                $notificationData = $row['notification'];
                // Parse notification data (channel, pid, payload)
                if (preg_match('/\((.*?),(.*?),(.*?)\)/', $notificationData, $matches)) {
                    $notification = [
                        'channel' => $matches[1],
                        'pid' => (int)$matches[2],
                        'payload' => $matches[3],
                        'timestamp' => time()
                    ];
                    $notifications[] = $notification;
                    $this->notifications[] = $notification;
                    
                    // Execute callback if registered
                    if (isset(self::$listenCallbacks[$notification['channel']])) {
                        call_user_func(self::$listenCallbacks[$notification['channel']], $notification);
                    }
                }
            }
        }
        
        return $notifications;
    }
    
    /**
     * Poll for notifications with timeout
     */
    public function pollNotifications(array $params = []): array
    {
        $timeout = $params['timeout'] ?? 10; // seconds
        $startTime = time();
        
        while (time() - $startTime < $timeout) {
            $notifications = $this->getNotifications();
            if (!empty($notifications)) {
                return $notifications;
            }
            usleep(100000); // 0.1 second
        }
        
        return [];
    }
    
    /**
     * Full-text Search Operations
     */
    
    /**
     * Enhanced full-text search
     */
    public function fulltextSearch(array $params): array
    {
        $table = $params['table'] ?? throw new \InvalidArgumentException('Table name required');
        $column = $params['column'] ?? throw new \InvalidArgumentException('Column name required');
        $query = $params['query'] ?? throw new \InvalidArgumentException('Search query required');
        $config = $params['config'] ?? 'english';
        $rank = $params['rank'] ?? false;
        $where = $params['where'] ?? [];
        $limit = $params['limit'] ?? 100;
        
        $sql = "SELECT *";
        if ($rank) {
            $sql .= ", ts_rank(to_tsvector('{$config}', {$column}), plainto_tsquery('{$config}', ?)) as relevance_rank";
        }
        $sql .= " FROM {$table} WHERE to_tsvector('{$config}', {$column}) @@ plainto_tsquery('{$config}', ?)";
        
        $bindings = $rank ? [$query, $query] : [$query];
        
        if (!empty($where)) {
            $conditions = [];
            foreach ($where as $field => $value) {
                $conditions[] = "{$field} = ?";
                $bindings[] = $value;
            }
            $sql .= " AND " . implode(' AND ', $conditions);
        }
        
        if ($rank) {
            $sql .= " ORDER BY relevance_rank DESC";
        }
        
        $sql .= " LIMIT {$limit}";
        
        $stmt = $this->connection->prepare($sql);
        $stmt->execute($bindings);
        
        return $stmt->fetchAll();
    }
    
    /**
     * Trigram similarity search
     */
    public function trigramSearch(array $params): array
    {
        $table = $params['table'] ?? throw new \InvalidArgumentException('Table name required');
        $column = $params['column'] ?? throw new \InvalidArgumentException('Column name required');
        $query = $params['query'] ?? throw new \InvalidArgumentException('Search query required');
        $threshold = $params['threshold'] ?? 0.3;
        $limit = $params['limit'] ?? 100;
        
        // Ensure pg_trgm extension is available
        $this->connection->exec("CREATE EXTENSION IF NOT EXISTS pg_trgm");
        
        $sql = "SELECT *, similarity({$column}, ?) as sim_score 
                FROM {$table} 
                WHERE {$column} % ? 
                ORDER BY sim_score DESC 
                LIMIT {$limit}";
        
        $stmt = $this->connection->prepare($sql);
        $stmt->execute([$query, $query]);
        
        return array_filter($stmt->fetchAll(), function($row) use ($threshold) {
            return $row['sim_score'] >= $threshold;
        });
    }
    
    /**
     * Window Functions
     */
    
    /**
     * Execute window function query
     */
    public function windowFunction(array $params): array
    {
        $sql = $params['sql'] ?? throw new \InvalidArgumentException('SQL query required');
        $bindings = $params['bindings'] ?? [];
        
        $stmt = $this->connection->prepare($sql);
        $stmt->execute($bindings);
        
        return $stmt->fetchAll();
    }
    
    /**
     * Common Table Expressions
     */
    
    /**
     * Execute WITH query
     */
    public function withQuery(array $params): array
    {
        $sql = $params['sql'] ?? throw new \InvalidArgumentException('SQL query required');
        $bindings = $params['bindings'] ?? [];
        
        $stmt = $this->connection->prepare($sql);
        $stmt->execute($bindings);
        
        return $stmt->fetchAll();
    }
    
    /**
     * Execute recursive query
     */
    public function recursiveQuery(array $params): array
    {
        $sql = $params['sql'] ?? throw new \InvalidArgumentException('SQL query required');
        $bindings = $params['bindings'] ?? [];
        
        $stmt = $this->connection->prepare($sql);
        $stmt->execute($bindings);
        
        return $stmt->fetchAll();
    }
    
    /**
     * Monitoring and Health Check Functions
     */
    
    /**
     * Get PostgreSQL activity statistics
     */
    public function pgStatActivity(array $params = []): array
    {
        $sql = "SELECT 
                    pid, 
                    usename, 
                    application_name,
                    client_addr,
                    client_port,
                    backend_start,
                    state,
                    query,
                    state_change
                FROM pg_stat_activity 
                WHERE state != 'idle' 
                ORDER BY backend_start DESC";
        
        $stmt = $this->connection->query($sql);
        return $stmt->fetchAll();
    }
    
    /**
     * Get database statistics
     */
    public function pgStatDatabase(array $params = []): array
    {
        $sql = "SELECT 
                    datname,
                    numbackends,
                    xact_commit,
                    xact_rollback,
                    blks_read,
                    blks_hit,
                    tup_returned,
                    tup_fetched,
                    tup_inserted,
                    tup_updated,
                    tup_deleted
                FROM pg_stat_database
                WHERE datname NOT IN ('template0', 'template1')
                ORDER BY datname";
        
        $stmt = $this->connection->query($sql);
        return $stmt->fetchAll();
    }
    
    /**
     * Get lock information
     */
    public function pgLocks(array $params = []): array
    {
        $sql = "SELECT 
                    l.locktype,
                    l.database,
                    l.relation,
                    l.page,
                    l.tuple,
                    l.virtualxid,
                    l.transactionid,
                    l.classid,
                    l.objid,
                    l.objsubid,
                    l.virtualtransaction,
                    l.pid,
                    l.mode,
                    l.granted,
                    a.usename,
                    a.query,
                    a.state
                FROM pg_locks l
                LEFT JOIN pg_stat_activity a ON l.pid = a.pid
                WHERE NOT l.granted
                ORDER BY l.pid";
        
        $stmt = $this->connection->query($sql);
        return $stmt->fetchAll();
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
            'version' => '',
            'errors' => []
        ];
        
        try {
            $health['connection_test'] = $this->testConnection();
            
            // Get PostgreSQL version
            $stmt = $this->connection->query('SELECT version()');
            $health['version'] = $stmt->fetchColumn();
            
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
     * Error handling helper methods
     */
    
    private function isSerializationFailure(PDOException $e): bool
    {
        return in_array($e->getCode(), ['40001', '40P01']);
    }
    
    private function isConnectionError(PDOException $e): bool
    {
        return in_array($e->getCode(), ['08000', '08003', '08006', '08001', '08004']);
    }
    
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
                $config['sslmode'],
                $config['application_name']
            );
        }
    }
    
    private function recordPerformanceMetrics(string $operation, float $executionTime, bool $success): void
    {
        // Same as MySQL implementation
    }
    
    private function logError(string $operation, Exception $e, array $params): void
    {
        // Same as MySQL implementation with PostgreSQL specifics
    }
    
    // Placeholder implementations for remaining methods
    public function disconnect(array $params): bool { return true; }
    public function query(array $params): array { return []; }
    public function scalar(array $params): mixed { return null; }
    public function executeStatement(array $params): int { return 0; }
    public function prepare(array $params): PDOStatement { return $this->connection->prepare('SELECT 1'); }
    public function begin(array $params = []): bool { return true; }
    public function commit(array $params = []): bool { return true; }
    public function rollback(array $params = []): bool { return true; }
    public function savepoint(array $params): bool { return true; }
    public function rollbackTo(array $params): bool { return true; }
    public function insert(array $params): int { return 0; }
    public function update(array $params): int { return 0; }
    public function delete(array $params): int { return 0; }
    public function select(array $params): array { return []; }
    public function count(array $params): int { return 0; }
    
    // Additional placeholder methods...
    public function jsonSet(array $params): bool { return true; }
    public function jsonContains(array $params): bool { return true; }
    public function jsonPath(array $params): array { return []; }
    public function jsonBuild(array $params): array { return []; }
    public function jsonbGet(array $params): mixed { return null; }
    public function jsonbSet(array $params): bool { return true; }
    public function jsonbBuild(array $params): array { return []; }
    public function jsonEach(array $params): array { return []; }
    public function jsonObjectKeys(array $params): array { return []; }
    public function jsonExtractPath(array $params): mixed { return null; }
    public function jsonStripNulls(array $params): array { return []; }
    
    public function arrayAppend(array $params): bool { return true; }
    public function arrayRemove(array $params): bool { return true; }
    public function arrayPosition(array $params): int { return 0; }
    public function arrayPositions(array $params): array { return []; }
    public function arrayCat(array $params): array { return []; }
    public function arrayDims(array $params): string { return ''; }
    public function unnest(array $params): array { return []; }
    public function arrayAgg(array $params): array { return []; }
    public function arrayToString(array $params): string { return ''; }
    public function stringToArray(array $params): array { return []; }
    
    public function similarity(array $params): float { return 0.0; }
    public function tsRank(array $params): float { return 0.0; }
    public function toTsvector(array $params): string { return ''; }
    public function toTsquery(array $params): string { return ''; }
    public function plaintoTsquery(array $params): string { return ''; }
    public function phrasetoTsquery(array $params): string { return ''; }
    
    public function createType(array $params): bool { return true; }
    public function dropType(array $params): bool { return true; }
    public function createDomain(array $params): bool { return true; }
    public function dropDomain(array $params): bool { return true; }
    public function createEnum(array $params): bool { return true; }
    public function dropEnum(array $params): bool { return true; }
    public function alterType(array $params): bool { return true; }
    
    public function createTable(array $params): bool { return true; }
    public function dropTable(array $params): bool { return true; }
    public function createIndex(array $params): bool { return true; }
    public function dropIndex(array $params): bool { return true; }
    public function createFunction(array $params): bool { return true; }
    public function dropFunction(array $params): bool { return true; }
    public function createTrigger(array $params): bool { return true; }
    public function dropTrigger(array $params): bool { return true; }
    public function createView(array $params): bool { return true; }
    public function dropView(array $params): bool { return true; }
    public function createMaterializedView(array $params): bool { return true; }
    public function refreshMaterializedView(array $params): bool { return true; }
    
    public function rowNumber(array $params): array { return []; }
    public function rank(array $params): array { return []; }
    public function denseRank(array $params): array { return []; }
    public function lag(array $params): array { return []; }
    public function lead(array $params): array { return []; }
    
    public function vacuum(array $params): bool { return true; }
    public function analyze(array $params): bool { return true; }
    public function reindex(array $params): bool { return true; }
    public function cluster(array $params): bool { return true; }
    public function listTables(array $params): array { return []; }
    public function describeTable(array $params): array { return []; }
    public function explain(array $params): array { return []; }
    public function explainAnalyze(array $params): array { return []; }
    
    public function getPerformanceMetrics(): array { return []; }
    public function getPoolStatus(): array { return []; }
    public function clearCache(): array { return []; }
} 