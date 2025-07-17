# Advanced Database Patterns

TuskLang provides sophisticated database integration capabilities that go beyond simple CRUD operations. This guide covers advanced database patterns, query optimization, and integration with PHP frameworks.

## Table of Contents

- [Repository Pattern](#repository-pattern)
- [Unit of Work Pattern](#unit-of-work-pattern)
- [Query Builder Patterns](#query-builder-patterns)
- [Database Sharding](#database-sharding)
- [Read/Write Splitting](#readwrite-splitting)
- [Connection Pooling](#connection-pooling)
- [Query Optimization](#query-optimization)
- [Database Migrations](#database-migrations)
- [Best Practices](#best-practices)

## Repository Pattern

Implement the repository pattern with TuskLang:

```php
// config/database.tsk
database_config = {
    repositories = {
        user_repository = {
            table = "users"
            primary_key = "id"
            relationships = {
                posts = {
                    type = "has_many"
                    table = "posts"
                    foreign_key = "user_id"
                }
                profile = {
                    type = "has_one"
                    table = "user_profiles"
                    foreign_key = "user_id"
                }
            }
            scopes = {
                active = "WHERE active = 1"
                verified = "WHERE email_verified = 1"
                premium = "WHERE subscription_type = 'premium'"
            }
        }
        
        product_repository = {
            table = "products"
            primary_key = "id"
            relationships = {
                category = {
                    type = "belongs_to"
                    table = "categories"
                    foreign_key = "category_id"
                }
                reviews = {
                    type = "has_many"
                    table = "product_reviews"
                    foreign_key = "product_id"
                }
            }
            scopes = {
                in_stock = "WHERE stock_quantity > 0"
                featured = "WHERE featured = 1"
                on_sale = "WHERE sale_price IS NOT NULL"
            }
        }
    }
}
```

```php
<?php
// app/Repositories/BaseRepository.php

namespace App\Repositories;

use TuskLang\Database\QueryBuilder;
use TuskLang\Database\Connection;

abstract class BaseRepository
{
    protected QueryBuilder $queryBuilder;
    protected Connection $connection;
    protected array $config;
    protected string $table;
    protected string $primaryKey;
    protected array $relationships;
    protected array $scopes;
    
    public function __construct(Connection $connection, array $config)
    {
        $this->connection = $connection;
        $this->queryBuilder = new QueryBuilder($connection);
        $this->config = $config;
        $this->table = $config['table'];
        $this->primaryKey = $config['primary_key'] ?? 'id';
        $this->relationships = $config['relationships'] ?? [];
        $this->scopes = $config['scopes'] ?? [];
    }
    
    public function find(int $id): ?array
    {
        return $this->queryBuilder
            ->table($this->table)
            ->where($this->primaryKey, $id)
            ->first();
    }
    
    public function findBy(array $criteria): array
    {
        $query = $this->queryBuilder->table($this->table);
        
        foreach ($criteria as $field => $value) {
            if (is_array($value)) {
                $query->whereIn($field, $value);
            } else {
                $query->where($field, $value);
            }
        }
        
        return $query->get();
    }
    
    public function findOneBy(array $criteria): ?array
    {
        $query = $this->queryBuilder->table($this->table);
        
        foreach ($criteria as $field => $value) {
            if (is_array($value)) {
                $query->whereIn($field, $value);
            } else {
                $query->where($field, $value);
            }
        }
        
        return $query->first();
    }
    
    public function findAll(): array
    {
        return $this->queryBuilder->table($this->table)->get();
    }
    
    public function create(array $data): int
    {
        $data['created_at'] = date('Y-m-d H:i:s');
        $data['updated_at'] = date('Y-m-d H:i:s');
        
        return $this->queryBuilder->table($this->table)->insertGetId($data);
    }
    
    public function update(int $id, array $data): bool
    {
        $data['updated_at'] = date('Y-m-d H:i:s');
        
        return $this->queryBuilder
            ->table($this->table)
            ->where($this->primaryKey, $id)
            ->update($data);
    }
    
    public function delete(int $id): bool
    {
        return $this->queryBuilder
            ->table($this->table)
            ->where($this->primaryKey, $id)
            ->delete();
    }
    
    public function scope(string $scopeName): self
    {
        if (isset($this->scopes[$scopeName])) {
            $this->queryBuilder->whereRaw($this->scopes[$scopeName]);
        }
        
        return $this;
    }
    
    public function with(string $relationship): self
    {
        if (isset($this->relationships[$relationship])) {
            $this->loadRelationship($relationship);
        }
        
        return $this;
    }
    
    protected function loadRelationship(string $relationship): void
    {
        $config = $this->relationships[$relationship];
        
        switch ($config['type']) {
            case 'has_many':
                $this->loadHasMany($relationship, $config);
                break;
            case 'has_one':
                $this->loadHasOne($relationship, $config);
                break;
            case 'belongs_to':
                $this->loadBelongsTo($relationship, $config);
                break;
        }
    }
    
    protected function loadHasMany(string $relationship, array $config): void
    {
        $foreignKey = $config['foreign_key'];
        $relatedTable = $config['table'];
        
        $this->queryBuilder->leftJoin($relatedTable, function($join) use ($foreignKey) {
            $join->on("{$this->table}.{$this->primaryKey}", '=', "{$relatedTable}.{$foreignKey}");
        });
    }
    
    protected function loadHasOne(string $relationship, array $config): void
    {
        $foreignKey = $config['foreign_key'];
        $relatedTable = $config['table'];
        
        $this->queryBuilder->leftJoin($relatedTable, function($join) use ($foreignKey) {
            $join->on("{$this->table}.{$this->primaryKey}", '=', "{$relatedTable}.{$foreignKey}");
        });
    }
    
    protected function loadBelongsTo(string $relationship, array $config): void
    {
        $foreignKey = $config['foreign_key'];
        $relatedTable = $config['table'];
        
        $this->queryBuilder->leftJoin($relatedTable, function($join) use ($foreignKey) {
            $join->on("{$this->table}.{$foreignKey}", '=', "{$relatedTable}.id");
        });
    }
}
```

## Unit of Work Pattern

Implement the Unit of Work pattern for transaction management:

```php
<?php
// app/Database/UnitOfWork.php

namespace App\Database;

use TuskLang\Database\Connection;
use TuskLang\Database\Transaction;

class UnitOfWork
{
    private Connection $connection;
    private array $newEntities = [];
    private array $dirtyEntities = [];
    private array $removedEntities = [];
    private array $repositories = [];
    
    public function __construct(Connection $connection)
    {
        $this->connection = $connection;
    }
    
    public function registerNew(string $entity, array $data): void
    {
        $this->newEntities[$entity][] = $data;
    }
    
    public function registerDirty(string $entity, int $id, array $data): void
    {
        $this->dirtyEntities[$entity][$id] = $data;
    }
    
    public function registerRemoved(string $entity, int $id): void
    {
        $this->removedEntities[$entity][] = $id;
    }
    
    public function getRepository(string $entity): BaseRepository
    {
        if (!isset($this->repositories[$entity])) {
            $config = $this->getEntityConfig($entity);
            $this->repositories[$entity] = new BaseRepository($this->connection, $config);
        }
        
        return $this->repositories[$entity];
    }
    
    public function commit(): bool
    {
        try {
            $this->connection->beginTransaction();
            
            // Insert new entities
            foreach ($this->newEntities as $entity => $entities) {
                $repository = $this->getRepository($entity);
                foreach ($entities as $data) {
                    $repository->create($data);
                }
            }
            
            // Update dirty entities
            foreach ($this->dirtyEntities as $entity => $entities) {
                $repository = $this->getRepository($entity);
                foreach ($entities as $id => $data) {
                    $repository->update($id, $data);
                }
            }
            
            // Delete removed entities
            foreach ($this->removedEntities as $entity => $ids) {
                $repository = $this->getRepository($entity);
                foreach ($ids as $id) {
                    $repository->delete($id);
                }
            }
            
            $this->connection->commit();
            $this->clear();
            
            return true;
        } catch (\Exception $e) {
            $this->connection->rollback();
            throw $e;
        }
    }
    
    public function rollback(): void
    {
        $this->connection->rollback();
        $this->clear();
    }
    
    private function clear(): void
    {
        $this->newEntities = [];
        $this->dirtyEntities = [];
        $this->removedEntities = [];
    }
    
    private function getEntityConfig(string $entity): array
    {
        // Load configuration from TuskLang config
        $config = $this->loadConfig();
        return $config['repositories'][$entity] ?? [];
    }
    
    private function loadConfig(): array
    {
        // Load TuskLang configuration
        return TuskLang::load('database_config');
    }
}
```

## Query Builder Patterns

Create advanced query builder patterns:

```php
<?php
// app/Database/AdvancedQueryBuilder.php

namespace App\Database;

use TuskLang\Database\QueryBuilder;

class AdvancedQueryBuilder extends QueryBuilder
{
    private array $eagerLoads = [];
    private array $scopes = [];
    private array $macros = [];
    
    public function with(string $relationship): self
    {
        $this->eagerLoads[] = $relationship;
        return $this;
    }
    
    public function scope(string $scopeName, ...$parameters): self
    {
        if (isset($this->scopes[$scopeName])) {
            $scope = $this->scopes[$scopeName];
            if (is_callable($scope)) {
                $scope($this, ...$parameters);
            } else {
                $this->whereRaw($scope);
            }
        }
        
        return $this;
    }
    
    public function macro(string $name, callable $callback): void
    {
        $this->macros[$name] = $callback;
    }
    
    public function __call(string $name, array $arguments)
    {
        if (isset($this->macros[$name])) {
            return $this->macros[$name]($this, ...$arguments);
        }
        
        return parent::__call($name, $arguments);
    }
    
    public function whereInSubquery(string $column, callable $callback): self
    {
        $subquery = $callback(new QueryBuilder($this->connection));
        $this->whereIn($column, $subquery);
        
        return $this;
    }
    
    public function whereExists(callable $callback): self
    {
        $subquery = $callback(new QueryBuilder($this->connection));
        $this->whereExists($subquery);
        
        return $this;
    }
    
    public function whereNotExists(callable $callback): self
    {
        $subquery = $callback(new QueryBuilder($this->connection));
        $this->whereNotExists($subquery);
        
        return $this;
    }
    
    public function union(callable $callback): self
    {
        $unionQuery = $callback(new QueryBuilder($this->connection));
        $this->union($unionQuery);
        
        return $this;
    }
    
    public function unionAll(callable $callback): self
    {
        $unionQuery = $callback(new QueryBuilder($this->connection));
        $this->unionAll($unionQuery);
        
        return $this;
    }
    
    public function chunk(int $count, callable $callback): void
    {
        $offset = 0;
        
        do {
            $results = $this->limit($count)->offset($offset)->get();
            $callback($results);
            $offset += $count;
        } while (count($results) === $count);
    }
    
    public function cursor(callable $callback): void
    {
        $this->connection->cursor($this->toSql(), $this->getBindings(), $callback);
    }
    
    public function when($condition, callable $callback): self
    {
        if ($condition) {
            $callback($this);
        }
        
        return $this;
    }
    
    public function unless($condition, callable $callback): self
    {
        if (!$condition) {
            $callback($this);
        }
        
        return $this;
    }
    
    public function tap(callable $callback): self
    {
        $callback($this);
        return $this;
    }
}
```

## Database Sharding

Implement database sharding strategies:

```php
<?php
// app/Database/ShardingStrategy.php

namespace App\Database;

class ShardingStrategy
{
    private array $shards = [];
    private array $config;
    
    public function __construct(array $config)
    {
        $this->config = $config;
        $this->initializeShards();
    }
    
    public function getShard(string $key): Connection
    {
        $shardId = $this->getShardId($key);
        return $this->shards[$shardId];
    }
    
    public function getShardId(string $key): int
    {
        $hash = crc32($key);
        $shardCount = count($this->shards);
        
        return $hash % $shardCount;
    }
    
    public function queryAllShards(callable $callback): array
    {
        $results = [];
        
        foreach ($this->shards as $shardId => $connection) {
            $results[$shardId] = $callback($connection);
        }
        
        return $results;
    }
    
    public function queryShardRange(int $startShard, int $endShard, callable $callback): array
    {
        $results = [];
        
        for ($i = $startShard; $i <= $endShard; $i++) {
            if (isset($this->shards[$i])) {
                $results[$i] = $callback($this->shards[$i]);
            }
        }
        
        return $results;
    }
    
    public function migrateData(string $fromKey, string $toKey): bool
    {
        $fromShard = $this->getShard($fromKey);
        $toShard = $this->getShard($toKey);
        
        if ($fromShard === $toShard) {
            return true;
        }
        
        // Copy data from source shard to target shard
        $data = $this->getDataFromShard($fromShard, $fromKey);
        $this->insertDataToShard($toShard, $toKey, $data);
        
        // Delete from source shard
        $this->deleteDataFromShard($fromShard, $fromKey);
        
        return true;
    }
    
    public function rebalanceShards(): array
    {
        $stats = $this->getShardStats();
        $targetLoad = array_sum($stats) / count($stats);
        $migrations = [];
        
        foreach ($stats as $shardId => $load) {
            if ($load > $targetLoad * 1.2) {
                // Shard is overloaded, migrate some data
                $migrations = array_merge($migrations, $this->migrateFromOverloadedShard($shardId, $targetLoad));
            }
        }
        
        return $migrations;
    }
    
    private function initializeShards(): void
    {
        foreach ($this->config['shards'] as $shardId => $shardConfig) {
            $this->shards[$shardId] = new Connection($shardConfig);
        }
    }
    
    private function getDataFromShard(Connection $shard, string $key): array
    {
        $queryBuilder = new QueryBuilder($shard);
        return $queryBuilder->table('data')->where('key', $key)->first();
    }
    
    private function insertDataToShard(Connection $shard, string $key, array $data): bool
    {
        $queryBuilder = new QueryBuilder($shard);
        return $queryBuilder->table('data')->insert($data);
    }
    
    private function deleteDataFromShard(Connection $shard, string $key): bool
    {
        $queryBuilder = new QueryBuilder($shard);
        return $queryBuilder->table('data')->where('key', $key)->delete();
    }
    
    private function getShardStats(): array
    {
        $stats = [];
        
        foreach ($this->shards as $shardId => $connection) {
            $queryBuilder = new QueryBuilder($connection);
            $stats[$shardId] = $queryBuilder->table('data')->count();
        }
        
        return $stats;
    }
    
    private function migrateFromOverloadedShard(int $shardId, float $targetLoad): array
    {
        $migrations = [];
        $connection = $this->shards[$shardId];
        $queryBuilder = new QueryBuilder($connection);
        
        $excessRecords = $queryBuilder->table('data')
            ->limit(100)
            ->get();
        
        foreach ($excessRecords as $record) {
            $newShardId = $this->findOptimalShard($targetLoad);
            $migrations[] = [
                'from_shard' => $shardId,
                'to_shard' => $newShardId,
                'key' => $record['key']
            ];
        }
        
        return $migrations;
    }
    
    private function findOptimalShard(float $targetLoad): int
    {
        $stats = $this->getShardStats();
        $minLoad = min($stats);
        
        foreach ($stats as $shardId => $load) {
            if ($load <= $minLoad) {
                return $shardId;
            }
        }
        
        return array_key_first($stats);
    }
}
```

## Read/Write Splitting

Implement read/write splitting for database optimization:

```php
<?php
// app/Database/ReadWriteSplitter.php

namespace App\Database;

class ReadWriteSplitter
{
    private Connection $master;
    private array $slaves = [];
    private array $config;
    private int $currentSlave = 0;
    
    public function __construct(array $config)
    {
        $this->config = $config;
        $this->initializeConnections();
    }
    
    public function getReadConnection(): Connection
    {
        if (empty($this->slaves)) {
            return $this->master;
        }
        
        // Round-robin load balancing
        $slave = $this->slaves[$this->currentSlave];
        $this->currentSlave = ($this->currentSlave + 1) % count($this->slaves);
        
        return $slave;
    }
    
    public function getWriteConnection(): Connection
    {
        return $this->master;
    }
    
    public function executeRead(callable $callback)
    {
        $connection = $this->getReadConnection();
        return $callback($connection);
    }
    
    public function executeWrite(callable $callback)
    {
        $connection = $this->getWriteConnection();
        return $callback($connection);
    }
    
    public function executeTransaction(callable $callback)
    {
        $connection = $this->getWriteConnection();
        
        try {
            $connection->beginTransaction();
            $result = $callback($connection);
            $connection->commit();
            return $result;
        } catch (\Exception $e) {
            $connection->rollback();
            throw $e;
        }
    }
    
    public function replicateToSlaves(string $sql, array $bindings = []): void
    {
        foreach ($this->slaves as $slave) {
            try {
                $slave->statement($sql, $bindings);
            } catch (\Exception $e) {
                // Log replication error but don't fail
                error_log("Replication error: " . $e->getMessage());
            }
        }
    }
    
    public function checkSlaveHealth(): array
    {
        $health = [];
        
        foreach ($this->slaves as $index => $slave) {
            try {
                $slave->select('SELECT 1');
                $health[$index] = 'healthy';
            } catch (\Exception $e) {
                $health[$index] = 'unhealthy';
            }
        }
        
        return $health;
    }
    
    public function promoteSlaveToMaster(int $slaveIndex): bool
    {
        if (!isset($this->slaves[$slaveIndex])) {
            return false;
        }
        
        $newMaster = $this->slaves[$slaveIndex];
        unset($this->slaves[$slaveIndex]);
        
        $this->master = $newMaster;
        $this->slaves = array_values($this->slaves);
        
        return true;
    }
    
    private function initializeConnections(): void
    {
        $this->master = new Connection($this->config['master']);
        
        foreach ($this->config['slaves'] as $slaveConfig) {
            $this->slaves[] = new Connection($slaveConfig);
        }
    }
}
```

## Connection Pooling

Implement connection pooling for better performance:

```php
<?php
// app/Database/ConnectionPool.php

namespace App\Database;

class ConnectionPool
{
    private array $connections = [];
    private array $config;
    private int $maxConnections;
    private int $minConnections;
    private array $availableConnections = [];
    private array $busyConnections = [];
    
    public function __construct(array $config)
    {
        $this->config = $config;
        $this->maxConnections = $config['max_connections'] ?? 10;
        $this->minConnections = $config['min_connections'] ?? 2;
        $this->initializePool();
    }
    
    public function getConnection(): Connection
    {
        if (!empty($this->availableConnections)) {
            $connection = array_pop($this->availableConnections);
            $this->busyConnections[] = $connection;
            return $connection;
        }
        
        if (count($this->connections) < $this->maxConnections) {
            $connection = $this->createConnection();
            $this->connections[] = $connection;
            $this->busyConnections[] = $connection;
            return $connection;
        }
        
        // Wait for a connection to become available
        return $this->waitForConnection();
    }
    
    public function releaseConnection(Connection $connection): void
    {
        $index = array_search($connection, $this->busyConnections);
        
        if ($index !== false) {
            unset($this->busyConnections[$index]);
            $this->busyConnections = array_values($this->busyConnections);
            
            if ($this->isConnectionHealthy($connection)) {
                $this->availableConnections[] = $connection;
            } else {
                $this->removeConnection($connection);
            }
        }
    }
    
    public function execute(callable $callback)
    {
        $connection = $this->getConnection();
        
        try {
            return $callback($connection);
        } finally {
            $this->releaseConnection($connection);
        }
    }
    
    public function getPoolStats(): array
    {
        return [
            'total_connections' => count($this->connections),
            'available_connections' => count($this->availableConnections),
            'busy_connections' => count($this->busyConnections),
            'max_connections' => $this->maxConnections,
            'min_connections' => $this->minConnections
        ];
    }
    
    public function cleanup(): void
    {
        // Remove unhealthy connections
        foreach ($this->availableConnections as $index => $connection) {
            if (!$this->isConnectionHealthy($connection)) {
                unset($this->availableConnections[$index]);
                $this->removeConnection($connection);
            }
        }
        
        $this->availableConnections = array_values($this->availableConnections);
        
        // Ensure minimum connections
        while (count($this->connections) < $this->minConnections) {
            $connection = $this->createConnection();
            $this->connections[] = $connection;
            $this->availableConnections[] = $connection;
        }
    }
    
    private function initializePool(): void
    {
        for ($i = 0; $i < $this->minConnections; $i++) {
            $connection = $this->createConnection();
            $this->connections[] = $connection;
            $this->availableConnections[] = $connection;
        }
    }
    
    private function createConnection(): Connection
    {
        return new Connection($this->config);
    }
    
    private function waitForConnection(): Connection
    {
        $timeout = $this->config['timeout'] ?? 30;
        $startTime = time();
        
        while (time() - $startTime < $timeout) {
            if (!empty($this->availableConnections)) {
                $connection = array_pop($this->availableConnections);
                $this->busyConnections[] = $connection;
                return $connection;
            }
            
            usleep(100000); // Sleep for 100ms
        }
        
        throw new \RuntimeException('Connection pool timeout');
    }
    
    private function isConnectionHealthy(Connection $connection): bool
    {
        try {
            $connection->select('SELECT 1');
            return true;
        } catch (\Exception $e) {
            return false;
        }
    }
    
    private function removeConnection(Connection $connection): void
    {
        $index = array_search($connection, $this->connections);
        
        if ($index !== false) {
            unset($this->connections[$index]);
            $this->connections = array_values($this->connections);
        }
    }
}
```

## Query Optimization

Implement query optimization strategies:

```php
<?php
// app/Database/QueryOptimizer.php

namespace App\Database;

class QueryOptimizer
{
    private array $config;
    private array $queryCache = [];
    
    public function __construct(array $config)
    {
        $this->config = $config;
    }
    
    public function optimizeQuery(string $sql): string
    {
        // Remove unnecessary whitespace
        $sql = preg_replace('/\s+/', ' ', trim($sql));
        
        // Add hints if configured
        if ($this->config['add_hints'] ?? false) {
            $sql = $this->addQueryHints($sql);
        }
        
        // Optimize WHERE clauses
        $sql = $this->optimizeWhereClause($sql);
        
        // Optimize JOINs
        $sql = $this->optimizeJoins($sql);
        
        return $sql;
    }
    
    public function analyzeQuery(string $sql): array
    {
        $analysis = [
            'complexity' => $this->calculateComplexity($sql),
            'estimated_cost' => $this->estimateCost($sql),
            'suggestions' => $this->generateSuggestions($sql),
            'indexes_needed' => $this->identifyIndexes($sql)
        ];
        
        return $analysis;
    }
    
    public function suggestIndexes(string $sql): array
    {
        $tables = $this->extractTables($sql);
        $whereConditions = $this->extractWhereConditions($sql);
        $joinConditions = $this->extractJoinConditions($sql);
        
        $suggestions = [];
        
        foreach ($tables as $table) {
            $tableSuggestions = [];
            
            // Index suggestions for WHERE clauses
            foreach ($whereConditions as $condition) {
                if (strpos($condition, $table) !== false) {
                    $columns = $this->extractColumns($condition);
                    $tableSuggestions[] = [
                        'type' => 'where',
                        'columns' => $columns,
                        'priority' => 'high'
                    ];
                }
            }
            
            // Index suggestions for JOINs
            foreach ($joinConditions as $condition) {
                if (strpos($condition, $table) !== false) {
                    $columns = $this->extractColumns($condition);
                    $tableSuggestions[] = [
                        'type' => 'join',
                        'columns' => $columns,
                        'priority' => 'medium'
                    ];
                }
            }
            
            if (!empty($tableSuggestions)) {
                $suggestions[$table] = $tableSuggestions;
            }
        }
        
        return $suggestions;
    }
    
    public function cacheQuery(string $sql, $result, int $ttl = 3600): void
    {
        $hash = md5($sql);
        $this->queryCache[$hash] = [
            'result' => $result,
            'expires' => time() + $ttl
        ];
    }
    
    public function getCachedQuery(string $sql)
    {
        $hash = md5($sql);
        
        if (isset($this->queryCache[$hash])) {
            $cached = $this->queryCache[$hash];
            
            if ($cached['expires'] > time()) {
                return $cached['result'];
            } else {
                unset($this->queryCache[$hash]);
            }
        }
        
        return null;
    }
    
    private function addQueryHints(string $sql): string
    {
        // Add MySQL hints for optimization
        if (preg_match('/SELECT\s+/i', $sql)) {
            $sql = preg_replace('/SELECT\s+/i', 'SELECT SQL_CALC_FOUND_ROWS ', $sql, 1);
        }
        
        return $sql;
    }
    
    private function optimizeWhereClause(string $sql): string
    {
        // Move indexed columns to the beginning of WHERE clause
        $pattern = '/WHERE\s+(.*?)(ORDER BY|GROUP BY|LIMIT|$)/i';
        
        if (preg_match($pattern, $sql, $matches)) {
            $whereClause = $matches[1];
            $conditions = explode('AND', $whereClause);
            
            // Sort conditions by index priority
            usort($conditions, function($a, $b) {
                return $this->getConditionPriority($b) - $this->getConditionPriority($a);
            });
            
            $optimizedWhere = implode(' AND ', $conditions);
            $sql = preg_replace($pattern, "WHERE {$optimizedWhere} $2", $sql);
        }
        
        return $sql;
    }
    
    private function optimizeJoins(string $sql): string
    {
        // Ensure smaller tables are joined first
        $pattern = '/FROM\s+(\w+)\s+JOIN\s+(\w+)/i';
        
        if (preg_match($pattern, $sql, $matches)) {
            $table1 = $matches[1];
            $table2 = $matches[2];
            
            $size1 = $this->getTableSize($table1);
            $size2 = $this->getTableSize($table2);
            
            if ($size1 > $size2) {
                $sql = preg_replace($pattern, "FROM {$table2} JOIN {$table1}", $sql);
            }
        }
        
        return $sql;
    }
    
    private function calculateComplexity(string $sql): int
    {
        $complexity = 0;
        
        // Count tables
        $complexity += substr_count($sql, 'JOIN') * 2;
        
        // Count subqueries
        $complexity += substr_count($sql, '(SELECT') * 3;
        
        // Count aggregations
        $complexity += substr_count($sql, 'GROUP BY') * 2;
        $complexity += substr_count($sql, 'ORDER BY') * 1;
        
        return $complexity;
    }
    
    private function estimateCost(string $sql): float
    {
        $complexity = $this->calculateComplexity($sql);
        $tables = $this->extractTables($sql);
        
        $baseCost = count($tables) * 10;
        $complexityCost = $complexity * 5;
        
        return $baseCost + $complexityCost;
    }
    
    private function generateSuggestions(string $sql): array
    {
        $suggestions = [];
        
        if (substr_count($sql, 'SELECT *') > 0) {
            $suggestions[] = 'Avoid SELECT * - specify only needed columns';
        }
        
        if (substr_count($sql, 'DISTINCT') > 0) {
            $suggestions[] = 'Consider if DISTINCT is necessary';
        }
        
        if (substr_count($sql, 'ORDER BY') > 0 && substr_count($sql, 'LIMIT') === 0) {
            $suggestions[] = 'Add LIMIT clause to ORDER BY queries';
        }
        
        return $suggestions;
    }
    
    private function identifyIndexes(string $sql): array
    {
        return $this->suggestIndexes($sql);
    }
    
    private function extractTables(string $sql): array
    {
        preg_match_all('/FROM\s+(\w+)|JOIN\s+(\w+)/i', $sql, $matches);
        return array_filter(array_merge($matches[1], $matches[2]));
    }
    
    private function extractWhereConditions(string $sql): array
    {
        preg_match('/WHERE\s+(.*?)(ORDER BY|GROUP BY|LIMIT|$)/i', $sql, $matches);
        return $matches ? explode('AND', $matches[1]) : [];
    }
    
    private function extractJoinConditions(string $sql): array
    {
        preg_match_all('/ON\s+(.*?)(JOIN|WHERE|ORDER BY|GROUP BY|LIMIT|$)/i', $sql, $matches);
        return $matches[1] ?? [];
    }
    
    private function extractColumns(string $condition): array
    {
        preg_match_all('/(\w+)\.(\w+)/', $condition, $matches);
        return $matches[2] ?? [];
    }
    
    private function getConditionPriority(string $condition): int
    {
        // Higher priority for indexed columns
        if (strpos($condition, 'id') !== false) return 10;
        if (strpos($condition, 'user_id') !== false) return 9;
        if (strpos($condition, 'created_at') !== false) return 8;
        return 1;
    }
    
    private function getTableSize(string $table): int
    {
        // This would typically query the database for table statistics
        // For now, return a default size
        return 1000;
    }
}
```

## Database Migrations

Implement database migration system:

```php
<?php
// app/Database/MigrationManager.php

namespace App\Database;

class MigrationManager
{
    private Connection $connection;
    private array $config;
    private string $migrationsPath;
    
    public function __construct(Connection $connection, array $config)
    {
        $this->connection = $connection;
        $this->config = $config;
        $this->migrationsPath = $config['migrations_path'] ?? 'database/migrations';
        $this->ensureMigrationsTable();
    }
    
    public function migrate(): array
    {
        $pendingMigrations = $this->getPendingMigrations();
        $executed = [];
        
        foreach ($pendingMigrations as $migration) {
            try {
                $this->executeMigration($migration);
                $executed[] = $migration;
            } catch (\Exception $e) {
                throw new \RuntimeException("Migration failed: {$migration['file']} - " . $e->getMessage());
            }
        }
        
        return $executed;
    }
    
    public function rollback(int $steps = 1): array
    {
        $executedMigrations = $this->getExecutedMigrations();
        $rolledBack = [];
        
        for ($i = 0; $i < $steps && !empty($executedMigrations); $i++) {
            $migration = array_pop($executedMigrations);
            
            try {
                $this->rollbackMigration($migration);
                $rolledBack[] = $migration;
            } catch (\Exception $e) {
                throw new \RuntimeException("Rollback failed: {$migration['file']} - " . $e->getMessage());
            }
        }
        
        return $rolledBack;
    }
    
    public function status(): array
    {
        $allMigrations = $this->getAllMigrations();
        $executedMigrations = $this->getExecutedMigrations();
        $executedFiles = array_column($executedMigrations, 'file');
        
        $status = [];
        
        foreach ($allMigrations as $migration) {
            $status[] = [
                'file' => $migration['file'],
                'executed' => in_array($migration['file'], $executedFiles),
                'executed_at' => $this->getExecutionTime($migration['file'])
            ];
        }
        
        return $status;
    }
    
    private function ensureMigrationsTable(): void
    {
        $sql = "CREATE TABLE IF NOT EXISTS migrations (
            id INT AUTO_INCREMENT PRIMARY KEY,
            file VARCHAR(255) NOT NULL UNIQUE,
            executed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        )";
        
        $this->connection->statement($sql);
    }
    
    private function getPendingMigrations(): array
    {
        $allMigrations = $this->getAllMigrations();
        $executedMigrations = $this->getExecutedMigrations();
        $executedFiles = array_column($executedMigrations, 'file');
        
        return array_filter($allMigrations, function($migration) use ($executedFiles) {
            return !in_array($migration['file'], $executedFiles);
        });
    }
    
    private function getExecutedMigrations(): array
    {
        $sql = "SELECT file, executed_at FROM migrations ORDER BY executed_at ASC";
        $results = $this->connection->select($sql);
        
        return $results;
    }
    
    private function getAllMigrations(): array
    {
        $migrations = [];
        $files = glob($this->migrationsPath . '/*.php');
        
        foreach ($files as $file) {
            $migrations[] = [
                'file' => basename($file),
                'path' => $file
            ];
        }
        
        usort($migrations, function($a, $b) {
            return $a['file'] <=> $b['file'];
        });
        
        return $migrations;
    }
    
    private function executeMigration(array $migration): void
    {
        $migrationClass = $this->loadMigrationClass($migration['path']);
        
        if (method_exists($migrationClass, 'up')) {
            $migrationClass->up();
        }
        
        $this->recordMigration($migration['file']);
    }
    
    private function rollbackMigration(array $migration): void
    {
        $migrationClass = $this->loadMigrationClass($migration['path']);
        
        if (method_exists($migrationClass, 'down')) {
            $migrationClass->down();
        }
        
        $this->removeMigration($migration['file']);
    }
    
    private function loadMigrationClass(string $path): object
    {
        require_once $path;
        
        $className = $this->getClassNameFromFile($path);
        return new $className($this->connection);
    }
    
    private function getClassNameFromFile(string $path): string
    {
        $content = file_get_contents($path);
        preg_match('/class\s+(\w+)/', $content, $matches);
        
        return $matches[1] ?? 'Migration';
    }
    
    private function recordMigration(string $file): void
    {
        $sql = "INSERT INTO migrations (file) VALUES (?)";
        $this->connection->statement($sql, [$file]);
    }
    
    private function removeMigration(string $file): void
    {
        $sql = "DELETE FROM migrations WHERE file = ?";
        $this->connection->statement($sql, [$file]);
    }
    
    private function getExecutionTime(string $file): ?string
    {
        $sql = "SELECT executed_at FROM migrations WHERE file = ?";
        $result = $this->connection->select($sql, [$file]);
        
        return $result[0]['executed_at'] ?? null;
    }
}
```

## Best Practices

Follow these best practices for database operations:

```php
<?php
// config/database-best-practices.tsk

database_best_practices = {
    connection_management = {
        use_connection_pooling = true
        max_connections = 20
        min_connections = 5
        connection_timeout = 30
        query_timeout = 60
    }
    
    query_optimization = {
        use_indexes = true
        avoid_select_star = true
        limit_results = true
        use_prepared_statements = true
        cache_frequent_queries = true
    }
    
    transaction_management = {
        use_transactions = true
        keep_transactions_short = true
        handle_deadlocks = true
        use_appropriate_isolation_levels = true
    }
    
    security = {
        use_prepared_statements = true
        validate_input = true
        encrypt_sensitive_data = true
        use_least_privilege_principle = true
        audit_database_operations = true
    }
    
    monitoring = {
        track_slow_queries = true
        monitor_connection_usage = true
        alert_on_errors = true
        log_database_operations = true
    }
    
    backup_recovery = {
        regular_backups = true
        test_restore_procedures = true
        point_in_time_recovery = true
        backup_verification = true
    }
}

// Example usage in PHP
class DatabaseBestPractices
{
    public function implementBestPractices(): void
    {
        // 1. Use connection pooling
        $pool = new ConnectionPool($this->config);
        
        // 2. Use transactions for data integrity
        $pool->execute(function($connection) {
            $connection->beginTransaction();
            
            try {
                // Perform database operations
                $connection->commit();
            } catch (\Exception $e) {
                $connection->rollback();
                throw $e;
            }
        });
        
        // 3. Use prepared statements
        $sql = "SELECT * FROM users WHERE id = ? AND active = ?";
        $params = [$userId, true];
        
        // 4. Optimize queries
        $optimizer = new QueryOptimizer($this->config);
        $optimizedSql = $optimizer->optimizeQuery($sql);
        
        // 5. Monitor performance
        $startTime = microtime(true);
        $result = $connection->select($optimizedSql, $params);
        $executionTime = microtime(true) - $startTime;
        
        if ($executionTime > 1.0) {
            $this->logSlowQuery($optimizedSql, $executionTime);
        }
    }
    
    private function logSlowQuery(string $sql, float $executionTime): void
    {
        error_log("Slow query detected: {$sql} - {$executionTime}s");
    }
}
```

This comprehensive guide covers advanced database patterns in TuskLang with PHP integration. The database system is designed to be scalable, secure, and performant while maintaining the rebellious spirit of TuskLang development. 