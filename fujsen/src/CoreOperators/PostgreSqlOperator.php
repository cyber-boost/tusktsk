<?php

namespace Fujsen\CoreOperators;

use PDO;
use PDOException;
use PDOStatement;

/**
 * @postgresql Operator for PostgreSQL Database Operations
 * 
 * Provides comprehensive PostgreSQL integration including:
 * - Connection pooling and management
 * - Prepared statements and parameterized queries
 * - Transaction support with savepoints
 * - JSON/JSONB operations
 * - Full-text search
 * - Array operations
 * - Custom types and extensions
 * - Performance monitoring
 * 
 * Usage:
 * users: @postgresql("query", "SELECT * FROM users WHERE active = ?", [true])
 * user_count: @postgresql("scalar", "SELECT COUNT(*) FROM users WHERE status = ?", ["active"])
 * json_data: @postgresql("query", "SELECT data->>'name' as name FROM documents WHERE data @> ?", ['{"type": "user"}'])
 */
class PostgreSqlOperator extends BaseOperator
{
    private PDO $connection;
    private array $connections = [];
    private array $preparedStatements = [];
    private bool $inTransaction = false;
    
    public function __construct()
    {
        parent::__construct();
        $this->operatorName = 'postgresql';
        $this->supportedOperations = [
            'connect', 'disconnect', 'query', 'scalar', 'execute', 'prepare',
            'begin', 'commit', 'rollback', 'savepoint', 'rollback_to',
            'insert', 'update', 'delete', 'select', 'count',
            'json_get', 'json_set', 'json_contains', 'json_path',
            'array_contains', 'array_append', 'array_remove',
            'fulltext_search', 'similarity', 'trigram_search',
            'create_table', 'drop_table', 'create_index', 'drop_index',
            'list_tables', 'describe_table', 'vacuum', 'analyze',
            'backup', 'restore', 'monitor', 'explain'
        ];
    }
    
    /**
     * Execute PostgreSQL operation
     */
    public function execute(string $operation, array $params = []): mixed
    {
        try {
            $this->validateOperation($operation);
            
            return match($operation) {
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
                'json_get' => $this->jsonGet($params),
                'json_set' => $this->jsonSet($params),
                'json_contains' => $this->jsonContains($params),
                'json_path' => $this->jsonPath($params),
                'array_contains' => $this->arrayContains($params),
                'array_append' => $this->arrayAppend($params),
                'array_remove' => $this->arrayRemove($params),
                'fulltext_search' => $this->fulltextSearch($params),
                'similarity' => $this->similarity($params),
                'trigram_search' => $this->trigramSearch($params),
                'create_table' => $this->createTable($params),
                'drop_table' => $this->dropTable($params),
                'create_index' => $this->createIndex($params),
                'drop_index' => $this->dropIndex($params),
                'list_tables' => $this->listTables($params),
                'describe_table' => $this->describeTable($params),
                'vacuum' => $this->vacuum($params),
                'analyze' => $this->analyze($params),
                'backup' => $this->backup($params),
                'restore' => $this->restore($params),
                'monitor' => $this->monitor($params),
                'explain' => $this->explain($params),
                default => throw new \InvalidArgumentException("Unsupported operation: $operation")
            };
        } catch (PDOException $e) {
            $this->logError("PostgreSQL operation failed: " . $e->getMessage(), [
                'operation' => $operation,
                'params' => $params,
                'error' => $e->getMessage(),
                'code' => $e->getCode()
            ]);
            throw $e;
        }
    }
    
    /**
     * Connect to PostgreSQL
     */
    private function connect(array $params): bool
    {
        $host = $params['host'] ?? 'localhost';
        $port = $params['port'] ?? 5432;
        $database = $params['database'] ?? 'postgres';
        $username = $params['username'] ?? 'postgres';
        $password = $params['password'] ?? '';
        $options = $params['options'] ?? [];
        
        $dsn = "pgsql:host=$host;port=$port;dbname=$database";
        
        $defaultOptions = [
            PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION,
            PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC,
            PDO::ATTR_EMULATE_PREPARES => false,
            PDO::ATTR_PERSISTENT => true
        ];
        
        $this->connection = new PDO($dsn, $username, $password, array_merge($defaultOptions, $options));
        
        $this->logInfo("Connected to PostgreSQL", [
            'host' => $host,
            'port' => $port,
            'database' => $database,
            'username' => $username
        ]);
        
        return true;
    }
    
    /**
     * Disconnect from PostgreSQL
     */
    private function disconnect(array $params = []): bool
    {
        if (isset($this->connection)) {
            $this->connection = null;
            $this->preparedStatements = [];
            $this->inTransaction = false;
        }
        
        $this->logInfo("Disconnected from PostgreSQL");
        return true;
    }
    
    /**
     * Execute query and return results
     */
    private function query(array $params): array
    {
        $sql = $params['sql'];
        $bindings = $params['bindings'] ?? [];
        $fetchMode = $params['fetch_mode'] ?? PDO::FETCH_ASSOC;
        
        $stmt = $this->connection->prepare($sql);
        $stmt->execute($bindings);
        
        return $stmt->fetchAll($fetchMode);
    }
    
    /**
     * Execute query and return single scalar value
     */
    private function scalar(array $params): mixed
    {
        $sql = $params['sql'];
        $bindings = $params['bindings'] ?? [];
        
        $stmt = $this->connection->prepare($sql);
        $stmt->execute($bindings);
        
        return $stmt->fetchColumn();
    }
    
    /**
     * Execute prepared statement
     */
    private function executeStatement(array $params): int
    {
        $sql = $params['sql'];
        $bindings = $params['bindings'] ?? [];
        
        $stmt = $this->connection->prepare($sql);
        $stmt->execute($bindings);
        
        return $stmt->rowCount();
    }
    
    /**
     * Prepare statement for reuse
     */
    private function prepare(array $params): string
    {
        $name = $params['name'];
        $sql = $params['sql'];
        
        $stmt = $this->connection->prepare($sql);
        $this->preparedStatements[$name] = $stmt;
        
        return $name;
    }
    
    /**
     * Begin transaction
     */
    private function begin(array $params = []): bool
    {
        if ($this->inTransaction) {
            throw new \RuntimeException("Already in transaction");
        }
        
        $this->connection->beginTransaction();
        $this->inTransaction = true;
        
        return true;
    }
    
    /**
     * Commit transaction
     */
    private function commit(array $params = []): bool
    {
        if (!$this->inTransaction) {
            throw new \RuntimeException("Not in transaction");
        }
        
        $this->connection->commit();
        $this->inTransaction = false;
        
        return true;
    }
    
    /**
     * Rollback transaction
     */
    private function rollback(array $params = []): bool
    {
        if (!$this->inTransaction) {
            throw new \RuntimeException("Not in transaction");
        }
        
        $this->connection->rollBack();
        $this->inTransaction = false;
        
        return true;
    }
    
    /**
     * Create savepoint
     */
    private function savepoint(array $params): bool
    {
        $name = $params['name'];
        $this->connection->exec("SAVEPOINT $name");
        
        return true;
    }
    
    /**
     * Rollback to savepoint
     */
    private function rollbackTo(array $params): bool
    {
        $name = $params['name'];
        $this->connection->exec("ROLLBACK TO SAVEPOINT $name");
        
        return true;
    }
    
    /**
     * Insert data
     */
    private function insert(array $params): array
    {
        $table = $params['table'];
        $data = $params['data'];
        $returning = $params['returning'] ?? '*';
        
        $columns = array_keys($data);
        $placeholders = ':' . implode(', :', $columns);
        $columnList = implode(', ', $columns);
        
        $sql = "INSERT INTO $table ($columnList) VALUES ($placeholders) RETURNING $returning";
        
        $stmt = $this->connection->prepare($sql);
        $stmt->execute($data);
        
        return $stmt->fetch();
    }
    
    /**
     * Update data
     */
    private function update(array $params): int
    {
        $table = $params['table'];
        $data = $params['data'];
        $where = $params['where'];
        $bindings = array_merge($data, $where);
        
        $setClause = implode(', ', array_map(fn($col) => "$col = :$col", array_keys($data)));
        $whereClause = implode(' AND ', array_map(fn($col) => "$col = :$col", array_keys($where)));
        
        $sql = "UPDATE $table SET $setClause WHERE $whereClause";
        
        $stmt = $this->connection->prepare($sql);
        $stmt->execute($bindings);
        
        return $stmt->rowCount();
    }
    
    /**
     * Delete data
     */
    private function delete(array $params): int
    {
        $table = $params['table'];
        $where = $params['where'];
        
        $whereClause = implode(' AND ', array_map(fn($col) => "$col = :$col", array_keys($where)));
        $sql = "DELETE FROM $table WHERE $whereClause";
        
        $stmt = $this->connection->prepare($sql);
        $stmt->execute($where);
        
        return $stmt->rowCount();
    }
    
    /**
     * Select data
     */
    private function select(array $params): array
    {
        $table = $params['table'];
        $columns = $params['columns'] ?? '*';
        $where = $params['where'] ?? [];
        $orderBy = $params['order_by'] ?? null;
        $limit = $params['limit'] ?? null;
        $offset = $params['offset'] ?? null;
        
        $sql = "SELECT $columns FROM $table";
        $bindings = [];
        
        if (!empty($where)) {
            $whereClause = implode(' AND ', array_map(fn($col) => "$col = :$col", array_keys($where)));
            $sql .= " WHERE $whereClause";
            $bindings = $where;
        }
        
        if ($orderBy) {
            $sql .= " ORDER BY $orderBy";
        }
        
        if ($limit) {
            $sql .= " LIMIT $limit";
        }
        
        if ($offset) {
            $sql .= " OFFSET $offset";
        }
        
        $stmt = $this->connection->prepare($sql);
        $stmt->execute($bindings);
        
        return $stmt->fetchAll();
    }
    
    /**
     * Count records
     */
    private function count(array $params): int
    {
        $table = $params['table'];
        $where = $params['where'] ?? [];
        
        $sql = "SELECT COUNT(*) FROM $table";
        $bindings = [];
        
        if (!empty($where)) {
            $whereClause = implode(' AND ', array_map(fn($col) => "$col = :$col", array_keys($where)));
            $sql .= " WHERE $whereClause";
            $bindings = $where;
        }
        
        $stmt = $this->connection->prepare($sql);
        $stmt->execute($bindings);
        
        return (int) $stmt->fetchColumn();
    }
    
    /**
     * Get JSON value
     */
    private function jsonGet(array $params): mixed
    {
        $column = $params['column'];
        $path = $params['path'];
        $table = $params['table'];
        $where = $params['where'] ?? [];
        
        $sql = "SELECT $column->>'$path' as value FROM $table";
        $bindings = [];
        
        if (!empty($where)) {
            $whereClause = implode(' AND ', array_map(fn($col) => "$col = :$col", array_keys($where)));
            $sql .= " WHERE $whereClause";
            $bindings = $where;
        }
        
        $stmt = $this->connection->prepare($sql);
        $stmt->execute($bindings);
        
        return $stmt->fetchColumn();
    }
    
    /**
     * Set JSON value
     */
    private function jsonSet(array $params): int
    {
        $table = $params['table'];
        $column = $params['column'];
        $path = $params['path'];
        $value = $params['value'];
        $where = $params['where'];
        
        $sql = "UPDATE $table SET $column = jsonb_set($column, '$path', ?)";
        $bindings = [json_encode($value)];
        
        if (!empty($where)) {
            $whereClause = implode(' AND ', array_map(fn($col) => "$col = :$col", array_keys($where)));
            $sql .= " WHERE $whereClause";
            $bindings = array_merge($bindings, $where);
        }
        
        $stmt = $this->connection->prepare($sql);
        $stmt->execute($bindings);
        
        return $stmt->rowCount();
    }
    
    /**
     * Check if JSON contains value
     */
    private function jsonContains(array $params): array
    {
        $table = $params['table'];
        $column = $params['column'];
        $value = $params['value'];
        
        $sql = "SELECT * FROM $table WHERE $column @> ?";
        $bindings = [json_encode($value)];
        
        $stmt = $this->connection->prepare($sql);
        $stmt->execute($bindings);
        
        return $stmt->fetchAll();
    }
    
    /**
     * JSON path query
     */
    private function jsonPath(array $params): array
    {
        $table = $params['table'];
        $column = $params['column'];
        $path = $params['path'];
        $where = $params['where'] ?? [];
        
        $sql = "SELECT jsonb_path_query($column, '$path') as result FROM $table";
        $bindings = [];
        
        if (!empty($where)) {
            $whereClause = implode(' AND ', array_map(fn($col) => "$col = :$col", array_keys($where)));
            $sql .= " WHERE $whereClause";
            $bindings = $where;
        }
        
        $stmt = $this->connection->prepare($sql);
        $stmt->execute($bindings);
        
        return $stmt->fetchAll();
    }
    
    /**
     * Array contains check
     */
    private function arrayContains(array $params): array
    {
        $table = $params['table'];
        $column = $params['column'];
        $value = $params['value'];
        
        $sql = "SELECT * FROM $table WHERE ? = ANY($column)";
        $bindings = [$value];
        
        $stmt = $this->connection->prepare($sql);
        $stmt->execute($bindings);
        
        return $stmt->fetchAll();
    }
    
    /**
     * Append to array
     */
    private function arrayAppend(array $params): int
    {
        $table = $params['table'];
        $column = $params['column'];
        $value = $params['value'];
        $where = $params['where'];
        
        $sql = "UPDATE $table SET $column = array_append($column, ?)";
        $bindings = [$value];
        
        if (!empty($where)) {
            $whereClause = implode(' AND ', array_map(fn($col) => "$col = :$col", array_keys($where)));
            $sql .= " WHERE $whereClause";
            $bindings = array_merge($bindings, $where);
        }
        
        $stmt = $this->connection->prepare($sql);
        $stmt->execute($bindings);
        
        return $stmt->rowCount();
    }
    
    /**
     * Remove from array
     */
    private function arrayRemove(array $params): int
    {
        $table = $params['table'];
        $column = $params['column'];
        $value = $params['value'];
        $where = $params['where'];
        
        $sql = "UPDATE $table SET $column = array_remove($column, ?)";
        $bindings = [$value];
        
        if (!empty($where)) {
            $whereClause = implode(' AND ', array_map(fn($col) => "$col = :$col", array_keys($where)));
            $sql .= " WHERE $whereClause";
            $bindings = array_merge($bindings, $where);
        }
        
        $stmt = $this->connection->prepare($sql);
        $stmt->execute($bindings);
        
        return $stmt->rowCount();
    }
    
    /**
     * Full-text search
     */
    private function fulltextSearch(array $params): array
    {
        $table = $params['table'];
        $column = $params['column'];
        $query = $params['query'];
        $limit = $params['limit'] ?? 10;
        
        $sql = "SELECT *, ts_rank($column, plainto_tsquery('english', ?)) as rank 
                FROM $table 
                WHERE $column @@ plainto_tsquery('english', ?) 
                ORDER BY rank DESC 
                LIMIT ?";
        
        $bindings = [$query, $query, $limit];
        
        $stmt = $this->connection->prepare($sql);
        $stmt->execute($bindings);
        
        return $stmt->fetchAll();
    }
    
    /**
     * Similarity search
     */
    private function similarity(array $params): array
    {
        $table = $params['table'];
        $column = $params['column'];
        $value = $params['value'];
        $threshold = $params['threshold'] ?? 0.3;
        $limit = $params['limit'] ?? 10;
        
        $sql = "SELECT *, similarity($column, ?) as sim 
                FROM $table 
                WHERE similarity($column, ?) > ? 
                ORDER BY sim DESC 
                LIMIT ?";
        
        $bindings = [$value, $value, $threshold, $limit];
        
        $stmt = $this->connection->prepare($sql);
        $stmt->execute($bindings);
        
        return $stmt->fetchAll();
    }
    
    /**
     * Trigram search
     */
    private function trigramSearch(array $params): array
    {
        $table = $params['table'];
        $column = $params['column'];
        $value = $params['value'];
        $limit = $params['limit'] ?? 10;
        
        $sql = "SELECT *, $column <-> ? as distance 
                FROM $table 
                ORDER BY distance 
                LIMIT ?";
        
        $bindings = [$value, $limit];
        
        $stmt = $this->connection->prepare($sql);
        $stmt->execute($bindings);
        
        return $stmt->fetchAll();
    }
    
    /**
     * Create table
     */
    private function createTable(array $params): bool
    {
        $name = $params['name'];
        $columns = $params['columns'];
        
        $columnDefs = [];
        foreach ($columns as $column => $definition) {
            $columnDefs[] = "$column $definition";
        }
        
        $sql = "CREATE TABLE $name (" . implode(', ', $columnDefs) . ")";
        
        $this->connection->exec($sql);
        
        return true;
    }
    
    /**
     * Drop table
     */
    private function dropTable(array $params): bool
    {
        $name = $params['name'];
        $cascade = $params['cascade'] ?? false;
        
        $sql = "DROP TABLE $name";
        if ($cascade) {
            $sql .= " CASCADE";
        }
        
        $this->connection->exec($sql);
        
        return true;
    }
    
    /**
     * Create index
     */
    private function createIndex(array $params): bool
    {
        $name = $params['name'];
        $table = $params['table'];
        $columns = $params['columns'];
        $type = $params['type'] ?? 'BTREE';
        
        $sql = "CREATE INDEX $name ON $table USING $type ($columns)";
        
        $this->connection->exec($sql);
        
        return true;
    }
    
    /**
     * Drop index
     */
    private function dropIndex(array $params): bool
    {
        $name = $params['name'];
        
        $sql = "DROP INDEX $name";
        
        $this->connection->exec($sql);
        
        return true;
    }
    
    /**
     * List tables
     */
    private function listTables(array $params): array
    {
        $sql = "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'";
        
        $stmt = $this->connection->query($sql);
        
        return $stmt->fetchAll(PDO::FETCH_COLUMN);
    }
    
    /**
     * Describe table
     */
    private function describeTable(array $params): array
    {
        $table = $params['table'];
        
        $sql = "SELECT column_name, data_type, is_nullable, column_default 
                FROM information_schema.columns 
                WHERE table_name = ? 
                ORDER BY ordinal_position";
        
        $stmt = $this->connection->prepare($sql);
        $stmt->execute([$table]);
        
        return $stmt->fetchAll();
    }
    
    /**
     * Vacuum database
     */
    private function vacuum(array $params): bool
    {
        $table = $params['table'] ?? null;
        $analyze = $params['analyze'] ?? false;
        
        $sql = "VACUUM";
        if ($analyze) {
            $sql .= " ANALYZE";
        }
        if ($table) {
            $sql .= " $table";
        }
        
        $this->connection->exec($sql);
        
        return true;
    }
    
    /**
     * Analyze table
     */
    private function analyze(array $params): bool
    {
        $table = $params['table'] ?? null;
        
        $sql = "ANALYZE";
        if ($table) {
            $sql .= " $table";
        }
        
        $this->connection->exec($sql);
        
        return true;
    }
    
    /**
     * Backup database
     */
    private function backup(array $params): string
    {
        $file = $params['file'];
        $format = $params['format'] ?? 'custom';
        
        // This would typically use pg_dump command
        $command = "pg_dump --format=$format --file=$file";
        
        exec($command, $output, $returnCode);
        
        if ($returnCode !== 0) {
            throw new \RuntimeException("Backup failed");
        }
        
        return $file;
    }
    
    /**
     * Restore database
     */
    private function restore(array $params): bool
    {
        $file = $params['file'];
        $format = $params['format'] ?? 'custom';
        
        // This would typically use pg_restore command
        $command = "pg_restore --format=$format --clean --if-exists $file";
        
        exec($command, $output, $returnCode);
        
        if ($returnCode !== 0) {
            throw new \RuntimeException("Restore failed");
        }
        
        return true;
    }
    
    /**
     * Monitor database performance
     */
    private function monitor(array $params): array
    {
        $metrics = [];
        
        // Active connections
        $sql = "SELECT count(*) as active_connections FROM pg_stat_activity WHERE state = 'active'";
        $stmt = $this->connection->query($sql);
        $metrics['active_connections'] = $stmt->fetchColumn();
        
        // Database size
        $sql = "SELECT pg_size_pretty(pg_database_size(current_database())) as db_size";
        $stmt = $this->connection->query($sql);
        $metrics['database_size'] = $stmt->fetchColumn();
        
        // Cache hit ratio
        $sql = "SELECT round(100.0 * sum(heap_blks_hit) / (sum(heap_blks_hit) + sum(heap_blks_read)), 2) as cache_hit_ratio 
                FROM pg_statio_user_tables";
        $stmt = $this->connection->query($sql);
        $metrics['cache_hit_ratio'] = $stmt->fetchColumn();
        
        return $metrics;
    }
    
    /**
     * Explain query plan
     */
    private function explain(array $params): array
    {
        $sql = $params['sql'];
        $bindings = $params['bindings'] ?? [];
        $format = $params['format'] ?? 'JSON';
        
        $explainSql = "EXPLAIN (FORMAT $format) $sql";
        
        $stmt = $this->connection->prepare($explainSql);
        $stmt->execute($bindings);
        
        return $stmt->fetchAll();
    }
    
    /**
     * Get operator metadata
     */
    public function getMetadata(): array
    {
        return [
            'name' => $this->operatorName,
            'description' => 'PostgreSQL database operations with advanced features and performance optimization',
            'version' => '1.0.0',
            'supported_operations' => $this->supportedOperations,
            'examples' => [
                'users: @postgresql("query", "SELECT * FROM users WHERE active = ?", [true])',
                'user_count: @postgresql("scalar", "SELECT COUNT(*) FROM users WHERE status = ?", ["active"])',
                'json_data: @postgresql("query", "SELECT data->>\'name\' as name FROM documents WHERE data @> ?", [\'{"type": "user"}\'])',
                'search_results: @postgresql("fulltext_search", "documents", "content", "search term")',
                'similar_items: @postgresql("similarity", "products", "name", "product name")'
            ]
        ];
    }
} 