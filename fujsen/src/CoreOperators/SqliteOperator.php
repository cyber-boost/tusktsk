<?php

namespace Fujsen\CoreOperators;

use PDO;
use PDOException;
use PDOStatement;

/**
 * @sqlite Operator for SQLite Database Operations
 * 
 * Provides comprehensive SQLite integration including:
 * - File-based database management
 * - WAL mode and transaction support
 * - JSON operations
 * - Full-text search with FTS5
 * - Virtual tables and extensions
 * - Backup and recovery
 * - Performance optimization
 * 
 * Usage:
 * users: @sqlite("query", "SELECT * FROM users WHERE active = ?", [1])
 * user_count: @sqlite("scalar", "SELECT COUNT(*) FROM users WHERE status = ?", ["active"])
 * json_data: @sqlite("query", "SELECT json_extract(data, '$.name') as name FROM documents WHERE json_valid(data)")
 */
class SqliteOperator extends BaseOperator
{
    private PDO $connection;
    private array $connections = [];
    private array $preparedStatements = [];
    private bool $inTransaction = false;
    private string $databasePath;
    
    public function __construct()
    {
        parent::__construct();
        $this->operatorName = 'sqlite';
        $this->supportedOperations = [
            'connect', 'disconnect', 'query', 'scalar', 'execute', 'prepare',
            'begin', 'commit', 'rollback', 'savepoint', 'rollback_to',
            'insert', 'update', 'delete', 'select', 'count',
            'json_extract', 'json_set', 'json_valid', 'json_each',
            'fts_search', 'fts_create', 'fts_drop',
            'create_table', 'drop_table', 'create_index', 'drop_index',
            'list_tables', 'describe_table', 'pragma', 'vacuum',
            'backup', 'restore', 'integrity_check', 'optimize',
            'attach', 'detach', 'monitor', 'explain'
        ];
    }
    
    /**
     * Execute SQLite operation
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
                'json_valid' => $this->jsonValid($params),
                'json_each' => $this->jsonEach($params),
                'fts_search' => $this->ftsSearch($params),
                'fts_create' => $this->ftsCreate($params),
                'fts_drop' => $this->ftsDrop($params),
                'create_table' => $this->createTable($params),
                'drop_table' => $this->dropTable($params),
                'create_index' => $this->createIndex($params),
                'drop_index' => $this->dropIndex($params),
                'list_tables' => $this->listTables($params),
                'describe_table' => $this->describeTable($params),
                'pragma' => $this->pragma($params),
                'vacuum' => $this->vacuum($params),
                'backup' => $this->backup($params),
                'restore' => $this->restore($params),
                'integrity_check' => $this->integrityCheck($params),
                'optimize' => $this->optimize($params),
                'attach' => $this->attach($params),
                'detach' => $this->detach($params),
                'monitor' => $this->monitor($params),
                'explain' => $this->explain($params),
                default => throw new \InvalidArgumentException("Unsupported operation: $operation")
            };
        } catch (PDOException $e) {
            $this->logError("SQLite operation failed: " . $e->getMessage(), [
                'operation' => $operation,
                'params' => $params,
                'error' => $e->getMessage(),
                'code' => $e->getCode()
            ]);
            throw $e;
        }
    }
    
    /**
     * Connect to SQLite database
     */
    private function connect(array $params): bool
    {
        $path = $params['path'] ?? ':memory:';
        $options = $params['options'] ?? [];
        
        $this->databasePath = $path;
        
        $defaultOptions = [
            PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION,
            PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC,
            PDO::ATTR_EMULATE_PREPARES => false,
            PDO::SQLITE_ATTR_OPEN_FLAGS => PDO::SQLITE_OPEN_READWRITE | PDO::SQLITE_OPEN_CREATE
        ];
        
        $this->connection = new PDO("sqlite:$path", null, null, array_merge($defaultOptions, $options));
        
        // Enable WAL mode for better concurrency
        $this->connection->exec('PRAGMA journal_mode=WAL');
        
        // Enable foreign keys
        $this->connection->exec('PRAGMA foreign_keys=ON');
        
        // Set busy timeout
        $this->connection->exec('PRAGMA busy_timeout=30000');
        
        $this->logInfo("Connected to SQLite", [
            'path' => $path
        ]);
        
        return true;
    }
    
    /**
     * Disconnect from SQLite
     */
    private function disconnect(array $params = []): bool
    {
        if (isset($this->connection)) {
            $this->connection = null;
            $this->preparedStatements = [];
            $this->inTransaction = false;
        }
        
        $this->logInfo("Disconnected from SQLite");
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
            $sql .= " OR IGNORE";
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
        
        $sql = "SELECT json_extract($column, '$path') as value FROM $table";
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
        
        $sql = "UPDATE $table SET $column = json_set($column, '$path', ?)";
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
     * Validate JSON
     */
    private function jsonValid(array $params): array
    {
        $table = $params['table'];
        $column = $params['column'];
        
        $sql = "SELECT * FROM $table WHERE json_valid($column)";
        
        $stmt = $this->connection->query($sql);
        
        return $stmt->fetchAll();
    }
    
    /**
     * JSON each operation
     */
    private function jsonEach(array $params): array
    {
        $table = $params['table'];
        $column = $params['column'];
        $where = $params['where'] ?? [];
        
        $sql = "SELECT json_each.* FROM $table, json_each($column)";
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
     * Full-text search
     */
    private function ftsSearch(array $params): array
    {
        $table = $params['table'];
        $query = $params['query'];
        $columns = $params['columns'] ?? '*';
        $limit = $params['limit'] ?? 10;
        
        $sql = "SELECT $columns FROM $table WHERE $table MATCH ? ORDER BY rank LIMIT ?";
        $bindings = [$query, $limit];
        
        $stmt = $this->connection->prepare($sql);
        $stmt->execute($bindings);
        
        return $stmt->fetchAll();
    }
    
    /**
     * Create FTS table
     */
    private function ftsCreate(array $params): bool
    {
        $name = $params['name'];
        $columns = $params['columns'];
        $content = $params['content'] ?? null;
        $tokenizer = $params['tokenizer'] ?? 'porter';
        
        $sql = "CREATE VIRTUAL TABLE $name USING fts5(";
        if ($content) {
            $sql .= "content='$content', ";
        }
        $sql .= "content=$columns, tokenize='$tokenizer')";
        
        $this->connection->exec($sql);
        
        return true;
    }
    
    /**
     * Drop FTS table
     */
    private function ftsDrop(array $params): bool
    {
        $name = $params['name'];
        
        $sql = "DROP TABLE IF EXISTS $name";
        
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
        $withoutRowid = $params['without_rowid'] ?? false;
        
        $columnDefs = [];
        foreach ($columns as $column => $definition) {
            $columnDefs[] = "$column $definition";
        }
        
        $sql = "CREATE TABLE $name (" . implode(', ', $columnDefs) . ")";
        if ($withoutRowid) {
            $sql .= " WITHOUT ROWID";
        }
        
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
        $unique = $params['unique'] ?? false;
        $where = $params['where'] ?? null;
        
        $sql = "CREATE";
        if ($unique) {
            $sql .= " UNIQUE";
        }
        $sql .= " INDEX $name ON $table ($columns)";
        if ($where) {
            $sql .= " WHERE $where";
        }
        
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
        $sql = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'";
        
        $stmt = $this->connection->query($sql);
        
        return $stmt->fetchAll(PDO::FETCH_COLUMN);
    }
    
    /**
     * Describe table
     */
    private function describeTable(array $params): array
    {
        $table = $params['table'];
        
        $sql = "PRAGMA table_info($table)";
        
        $stmt = $this->connection->query($sql);
        
        return $stmt->fetchAll();
    }
    
    /**
     * Execute PRAGMA
     */
    private function pragma(array $params): array
    {
        $pragma = $params['pragma'];
        $value = $params['value'] ?? null;
        
        if ($value !== null) {
            $sql = "PRAGMA $pragma = $value";
            $this->connection->exec($sql);
            return ['success' => true];
        } else {
            $sql = "PRAGMA $pragma";
            $stmt = $this->connection->query($sql);
            return $stmt->fetchAll();
        }
    }
    
    /**
     * Vacuum database
     */
    private function vacuum(array $params): bool
    {
        $into = $params['into'] ?? null;
        
        $sql = "VACUUM";
        if ($into) {
            $sql .= " INTO '$into'";
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
        
        // Use VACUUM INTO for backup
        $sql = "VACUUM INTO '$file'";
        
        $this->connection->exec($sql);
        
        return $file;
    }
    
    /**
     * Restore database
     */
    private function restore(array $params): bool
    {
        $file = $params['file'];
        
        // Close current connection
        $this->connection = null;
        
        // Copy backup file to current database
        if ($this->databasePath !== ':memory:') {
            copy($file, $this->databasePath);
        }
        
        // Reconnect
        $this->connect(['path' => $this->databasePath]);
        
        return true;
    }
    
    /**
     * Integrity check
     */
    private function integrityCheck(array $params): array
    {
        $sql = "PRAGMA integrity_check";
        
        $stmt = $this->connection->query($sql);
        
        return $stmt->fetchAll();
    }
    
    /**
     * Optimize database
     */
    private function optimize(array $params): bool
    {
        // Analyze tables
        $this->connection->exec("ANALYZE");
        
        // Optimize indexes
        $this->connection->exec("REINDEX");
        
        return true;
    }
    
    /**
     * Attach database
     */
    private function attach(array $params): bool
    {
        $path = $params['path'];
        $name = $params['name'];
        
        $sql = "ATTACH DATABASE '$path' AS $name";
        
        $this->connection->exec($sql);
        
        return true;
    }
    
    /**
     * Detach database
     */
    private function detach(array $params): bool
    {
        $name = $params['name'];
        
        $sql = "DETACH DATABASE $name";
        
        $this->connection->exec($sql);
        
        return true;
    }
    
    /**
     * Monitor database performance
     */
    private function monitor(array $params): array
    {
        $metrics = [];
        
        // Database size
        if ($this->databasePath !== ':memory:') {
            $metrics['database_size'] = filesize($this->databasePath);
        }
        
        // Page count
        $sql = "PRAGMA page_count";
        $stmt = $this->connection->query($sql);
        $metrics['page_count'] = $stmt->fetchColumn();
        
        // Page size
        $sql = "PRAGMA page_size";
        $stmt = $this->connection->query($sql);
        $metrics['page_size'] = $stmt->fetchColumn();
        
        // Cache size
        $sql = "PRAGMA cache_size";
        $stmt = $this->connection->query($sql);
        $metrics['cache_size'] = $stmt->fetchColumn();
        
        // Journal mode
        $sql = "PRAGMA journal_mode";
        $stmt = $this->connection->query($sql);
        $metrics['journal_mode'] = $stmt->fetchColumn();
        
        return $metrics;
    }
    
    /**
     * Explain query plan
     */
    private function explain(array $params): array
    {
        $sql = $params['sql'];
        $bindings = $params['bindings'] ?? [];
        
        $explainSql = "EXPLAIN QUERY PLAN $sql";
        
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
            'description' => 'SQLite database operations with file-based storage and advanced features',
            'version' => '1.0.0',
            'supported_operations' => $this->supportedOperations,
            'examples' => [
                'users: @sqlite("query", "SELECT * FROM users WHERE active = ?", [1])',
                'user_count: @sqlite("scalar", "SELECT COUNT(*) FROM users WHERE status = ?", ["active"])',
                'json_data: @sqlite("query", "SELECT json_extract(data, \'$.name\') as name FROM documents WHERE json_valid(data)")',
                'search_results: @sqlite("fts_search", "documents_fts", "search term")',
                'integrity: @sqlite("integrity_check")'
            ]
        ];
    }
} 