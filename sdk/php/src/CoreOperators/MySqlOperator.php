<?php

namespace TuskLang\CoreOperators;

use PDO;
use PDOException;
use PDOStatement;

/**
 * @mysql Operator for MySQL Database Operations
 * 
 * Provides comprehensive MySQL integration including:
 * - Connection pooling and management
 * - Prepared statements and parameterized queries
 * - Transaction support with savepoints
 * - JSON operations
 * - Full-text search
 * - Stored procedures
 * - Replication monitoring
 * - Performance optimization
 * 
 * Usage:
 * users: @mysql("query", "SELECT * FROM users WHERE active = ?", [1])
 * user_count: @mysql("scalar", "SELECT COUNT(*) FROM users WHERE status = ?", ["active"])
 * json_data: @mysql("query", "SELECT JSON_EXTRACT(data, '$.name') as name FROM documents WHERE JSON_CONTAINS(data, ?)", ['{"type": "user"}'])
 */
class MySqlOperator extends \TuskLang\CoreOperators\BaseOperator
{
    private PDO $connection;
    private array $connections = [];
    private array $preparedStatements = [];
    private bool $inTransaction = false;
    
    public function __construct()
    {
        parent::__construct();
        $this->operatorName = 'mysql';
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
            'replication_status', 'monitor', 'explain'
        ];
    }
    
    /**
     * Execute MySQL operation
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
                default => throw new \InvalidArgumentException("Unsupported operation: $operation")
            };
        } catch (PDOException $e) {
            $this->logError("MySQL operation failed: " . $e->getMessage(), [
                'operation' => $operation,
                'params' => $params,
                'error' => $e->getMessage(),
                'code' => $e->getCode()
            ]);
            throw $e;
        }
    }
    
    /**
     * Connect to MySQL
     */
    private function connect(array $params): bool
    {
        $host = $params['host'] ?? 'localhost';
        $port = $params['port'] ?? 3306;
        $database = $params['database'] ?? 'mysql';
        $username = $params['username'] ?? 'root';
        $password = $params['password'] ?? '';
        $options = $params['options'] ?? [];
        
        $dsn = "mysql:host=$host;port=$port;dbname=$database;charset=utf8mb4";
        
        $defaultOptions = [
            PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION,
            PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC,
            PDO::ATTR_EMULATE_PREPARES => false,
            PDO::ATTR_PERSISTENT => true,
            PDO::MYSQL_ATTR_INIT_COMMAND => "SET NAMES utf8mb4"
        ];
        
        $this->connection = new PDO($dsn, $username, $password, array_merge($defaultOptions, $options));
        
        $this->logInfo("Connected to MySQL", [
            'host' => $host,
            'port' => $port,
            'database' => $database,
            'username' => $username
        ]);
        
        return true;
    }
    
    /**
     * Disconnect from MySQL
     */
    private function disconnect(array $params = []): bool
    {
        if (isset($this->connection)) {
            $this->connection = null;
            $this->preparedStatements = [];
            $this->inTransaction = false;
        }
        
        $this->logInfo("Disconnected from MySQL");
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
        $ignore = $params['ignore'] ?? false;
        
        $columns = array_keys($data);
        $placeholders = ':' . implode(', :', $columns);
        $columnList = implode(', ', $columns);
        
        $sql = "INSERT";
        if ($ignore) {
            $sql .= " IGNORE";
        }
        $sql .= " INTO $table ($columnList) VALUES ($placeholders)";
        
        $stmt = $this->connection->prepare($sql);
        $stmt->execute($data);
        
        return [
            'lastInsertId' => $this->connection->lastInsertId(),
            'rowCount' => $stmt->rowCount()
        ];
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
        $groupBy = $params['group_by'] ?? null;
        $having = $params['having'] ?? null;
        
        $sql = "SELECT $columns FROM $table";
        $bindings = [];
        
        if (!empty($where)) {
            $whereClause = implode(' AND ', array_map(fn($col) => "$col = :$col", array_keys($where)));
            $sql .= " WHERE $whereClause";
            $bindings = $where;
        }
        
        if ($groupBy) {
            $sql .= " GROUP BY $groupBy";
        }
        
        if ($having) {
            $sql .= " HAVING $having";
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
        $distinct = $params['distinct'] ?? null;
        
        $sql = "SELECT COUNT(";
        if ($distinct) {
            $sql .= "DISTINCT $distinct";
        } else {
            $sql .= "*";
        }
        $sql .= ") FROM $table";
        
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
     * Extract JSON value
     */
    private function jsonExtract(array $params): mixed
    {
        $column = $params['column'];
        $path = $params['path'];
        $table = $params['table'];
        $where = $params['where'] ?? [];
        
        $sql = "SELECT JSON_EXTRACT($column, '$path') as value FROM $table";
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
        
        $sql = "UPDATE $table SET $column = JSON_SET($column, '$path', ?)";
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
        
        $sql = "SELECT * FROM $table WHERE JSON_CONTAINS($column, ?)";
        $bindings = [json_encode($value)];
        
        $stmt = $this->connection->prepare($sql);
        $stmt->execute($bindings);
        
        return $stmt->fetchAll();
    }
    
    /**
     * Search in JSON
     */
    private function jsonSearch(array $params): array
    {
        $table = $params['table'];
        $column = $params['column'];
        $value = $params['value'];
        $path = $params['path'] ?? '$';
        
        $sql = "SELECT * FROM $table WHERE JSON_SEARCH($column, 'one', ?, NULL, '$path') IS NOT NULL";
        $bindings = [$value];
        
        $stmt = $this->connection->prepare($sql);
        $stmt->execute($bindings);
        
        return $stmt->fetchAll();
    }
    
    /**
     * Full-text search
     */
    private function fulltextSearch(array $params): array
    {
        $table = $params['table'];
        $column = $params['column'];
        $query = $params['query'];
        $mode = $params['mode'] ?? 'NATURAL LANGUAGE';
        $limit = $params['limit'] ?? 10;
        
        $sql = "SELECT *, MATCH($column) AGAINST(? IN $mode MODE) as relevance 
                FROM $table 
                WHERE MATCH($column) AGAINST(? IN $mode MODE) 
                ORDER BY relevance DESC 
                LIMIT ?";
        
        $bindings = [$query, $query, $limit];
        
        $stmt = $this->connection->prepare($sql);
        $stmt->execute($bindings);
        
        return $stmt->fetchAll();
    }
    
    /**
     * LIKE search
     */
    private function likeSearch(array $params): array
    {
        $table = $params['table'];
        $column = $params['column'];
        $pattern = $params['pattern'];
        $limit = $params['limit'] ?? 10;
        
        $sql = "SELECT * FROM $table WHERE $column LIKE ? LIMIT ?";
        $bindings = [$pattern, $limit];
        
        $stmt = $this->connection->prepare($sql);
        $stmt->execute($bindings);
        
        return $stmt->fetchAll();
    }
    
    /**
     * REGEX search
     */
    private function regexSearch(array $params): array
    {
        $table = $params['table'];
        $column = $params['column'];
        $pattern = $params['pattern'];
        $limit = $params['limit'] ?? 10;
        
        $sql = "SELECT * FROM $table WHERE $column REGEXP ? LIMIT ?";
        $bindings = [$pattern, $limit];
        
        $stmt = $this->connection->prepare($sql);
        $stmt->execute($bindings);
        
        return $stmt->fetchAll();
    }
    
    /**
     * Call stored procedure
     */
    private function storedProcedure(array $params): array
    {
        $name = $params['name'];
        $arguments = $params['arguments'] ?? [];
        
        $placeholders = str_repeat('?,', count($arguments) - 1) . '?';
        $sql = "CALL $name($placeholders)";
        
        $stmt = $this->connection->prepare($sql);
        $stmt->execute($arguments);
        
        return $stmt->fetchAll();
    }
    
    /**
     * Create function
     */
    private function function(array $params): bool
    {
        $name = $params['name'];
        $definition = $params['definition'];
        
        $sql = "CREATE FUNCTION $name $definition";
        
        $this->connection->exec($sql);
        
        return true;
    }
    
    /**
     * Create trigger
     */
    private function trigger(array $params): bool
    {
        $name = $params['name'];
        $timing = $params['timing']; // BEFORE, AFTER
        $event = $params['event']; // INSERT, UPDATE, DELETE
        $table = $params['table'];
        $definition = $params['definition'];
        
        $sql = "CREATE TRIGGER $name $timing $event ON $table FOR EACH ROW $definition";
        
        $this->connection->exec($sql);
        
        return true;
    }
    
    /**
     * Create event
     */
    private function event(array $params): bool
    {
        $name = $params['name'];
        $schedule = $params['schedule'];
        $definition = $params['definition'];
        
        $sql = "CREATE EVENT $name ON SCHEDULE $schedule DO $definition";
        
        $this->connection->exec($sql);
        
        return true;
    }
    
    /**
     * Create table
     */
    private function createTable(array $params): bool
    {
        $name = $params['name'];
        $columns = $params['columns'];
        $engine = $params['engine'] ?? 'InnoDB';
        $charset = $params['charset'] ?? 'utf8mb4';
        $collation = $params['collation'] ?? 'utf8mb4_unicode_ci';
        
        $columnDefs = [];
        foreach ($columns as $column => $definition) {
            $columnDefs[] = "$column $definition";
        }
        
        $sql = "CREATE TABLE $name (" . implode(', ', $columnDefs) . ") 
                ENGINE=$engine 
                DEFAULT CHARSET=$charset 
                COLLATE=$collation";
        
        $this->connection->exec($sql);
        
        return true;
    }
    
    /**
     * Drop table
     */
    private function dropTable(array $params): bool
    {
        $name = $params['name'];
        $ifExists = $params['if_exists'] ?? false;
        
        $sql = "DROP TABLE";
        if ($ifExists) {
            $sql .= " IF EXISTS";
        }
        $sql .= " $name";
        
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
        
        $sql = "CREATE INDEX $name ON $table ($columns) USING $type";
        
        $this->connection->exec($sql);
        
        return true;
    }
    
    /**
     * Drop index
     */
    private function dropIndex(array $params): bool
    {
        $name = $params['name'];
        $table = $params['table'];
        
        $sql = "DROP INDEX $name ON $table";
        
        $this->connection->exec($sql);
        
        return true;
    }
    
    /**
     * List tables
     */
    private function listTables(array $params): array
    {
        $database = $params['database'] ?? null;
        
        if ($database) {
            $sql = "SHOW TABLES FROM $database";
        } else {
            $sql = "SHOW TABLES";
        }
        
        $stmt = $this->connection->query($sql);
        
        return $stmt->fetchAll(PDO::FETCH_COLUMN);
    }
    
    /**
     * Describe table
     */
    private function describeTable(array $params): array
    {
        $table = $params['table'];
        
        $sql = "DESCRIBE $table";
        
        $stmt = $this->connection->query($sql);
        
        return $stmt->fetchAll();
    }
    
    /**
     * Show status
     */
    private function showStatus(array $params): array
    {
        $like = $params['like'] ?? null;
        
        $sql = "SHOW STATUS";
        if ($like) {
            $sql .= " LIKE ?";
        }
        
        $stmt = $this->connection->prepare($sql);
        if ($like) {
            $stmt->execute([$like]);
        } else {
            $stmt->execute();
        }
        
        return $stmt->fetchAll();
    }
    
    /**
     * Show variables
     */
    private function showVariables(array $params): array
    {
        $like = $params['like'] ?? null;
        
        $sql = "SHOW VARIABLES";
        if ($like) {
            $sql .= " LIKE ?";
        }
        
        $stmt = $this->connection->prepare($sql);
        if ($like) {
            $stmt->execute([$like]);
        } else {
            $stmt->execute();
        }
        
        return $stmt->fetchAll();
    }
    
    /**
     * Optimize table
     */
    private function optimizeTable(array $params): array
    {
        $table = $params['table'];
        
        $sql = "OPTIMIZE TABLE $table";
        
        $stmt = $this->connection->query($sql);
        
        return $stmt->fetchAll();
    }
    
    /**
     * Repair table
     */
    private function repairTable(array $params): array
    {
        $table = $params['table'];
        
        $sql = "REPAIR TABLE $table";
        
        $stmt = $this->connection->query($sql);
        
        return $stmt->fetchAll();
    }
    
    /**
     * Backup database
     */
    private function backup(array $params): string
    {
        $file = $params['file'];
        $database = $params['database'];
        $host = $params['host'] ?? 'localhost';
        $username = $params['username'] ?? 'root';
        $password = $params['password'] ?? '';
        
        $command = "mysqldump -h $host -u $username";
        if ($password) {
            $command .= " -p$password";
        }
        $command .= " $database > $file";
        
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
        $database = $params['database'];
        $host = $params['host'] ?? 'localhost';
        $username = $params['username'] ?? 'root';
        $password = $params['password'] ?? '';
        
        $command = "mysql -h $host -u $username";
        if ($password) {
            $command .= " -p$password";
        }
        $command .= " $database < $file";
        
        exec($command, $output, $returnCode);
        
        if ($returnCode !== 0) {
            throw new \RuntimeException("Restore failed");
        }
        
        return true;
    }
    
    /**
     * Get replication status
     */
    private function replicationStatus(array $params): array
    {
        $sql = "SHOW SLAVE STATUS";
        
        $stmt = $this->connection->query($sql);
        
        return $stmt->fetch();
    }
    
    /**
     * Monitor database performance
     */
    private function monitor(array $params): array
    {
        $metrics = [];
        
        // Threads connected
        $sql = "SHOW STATUS LIKE 'Threads_connected'";
        $stmt = $this->connection->query($sql);
        $metrics['threads_connected'] = $stmt->fetchColumn();
        
        // Questions (queries)
        $sql = "SHOW STATUS LIKE 'Questions'";
        $stmt = $this->connection->query($sql);
        $metrics['questions'] = $stmt->fetchColumn();
        
        // Slow queries
        $sql = "SHOW STATUS LIKE 'Slow_queries'";
        $stmt = $this->connection->query($sql);
        $metrics['slow_queries'] = $stmt->fetchColumn();
        
        // Uptime
        $sql = "SHOW STATUS LIKE 'Uptime'";
        $stmt = $this->connection->query($sql);
        $metrics['uptime'] = $stmt->fetchColumn();
        
        return $metrics;
    }
    
    /**
     * Explain query plan
     */
    private function explain(array $params): array
    {
        $sql = $params['sql'];
        $bindings = $params['bindings'] ?? [];
        $format = $params['format'] ?? 'TRADITIONAL';
        
        $explainSql = "EXPLAIN FORMAT=$format $sql";
        
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
            'description' => 'MySQL database operations with advanced features and performance optimization',
            'version' => '1.0.0',
            'supported_operations' => $this->supportedOperations,
            'examples' => [
                'users: @mysql("query", "SELECT * FROM users WHERE active = ?", [1])',
                'user_count: @mysql("scalar", "SELECT COUNT(*) FROM users WHERE status = ?", ["active"])',
                'json_data: @mysql("query", "SELECT JSON_EXTRACT(data, \'$.name\') as name FROM documents WHERE JSON_CONTAINS(data, ?)", [\'{"type": "user"}\'])',
                'search_results: @mysql("fulltext_search", "documents", "content", "search term")',
                'status: @mysql("show_status", "Threads_connected")'
            ]
        ];
    }
} 