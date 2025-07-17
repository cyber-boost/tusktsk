# 🗃️ Advanced Database Patterns with TuskLang & PHP

## Introduction
Database patterns are the backbone of scalable, performant applications. TuskLang and PHP let you implement sophisticated database patterns with config-driven connection pooling, query optimization, sharding, and ORM patterns that handle complex data operations.

## Key Features
- **Connection pooling and management**
- **Query optimization and caching**
- **Database sharding strategies**
- **Read replica patterns**
- **Database migrations**
- **ORM patterns and relationships**

## Example: Database Configuration
```ini
[database]
primary: @env.secure("DB_PRIMARY_DSN")
replicas: @go("database.GetReplicas")
sharding: @go("database.ConfigureSharding")
pooling: @go("database.ConfigurePooling")
migrations: @go("database.ConfigureMigrations")
```

## PHP: Connection Pool Implementation
```php
<?php

namespace App\Database;

use TuskLang\Config;
use TuskLang\Operators\Env;
use TuskLang\Operators\Metrics;
use TuskLang\Operators\Go;

class ConnectionPool
{
    private $config;
    private $connections = [];
    private $pool = [];
    private $maxConnections;
    private $minConnections;
    
    public function __construct()
    {
        $this->config = new Config();
        $this->maxConnections = $this->config->get('database.pool.max_connections', 20);
        $this->minConnections = $this->config->get('database.pool.min_connections', 5);
        $this->initializePool();
    }
    
    public function getConnection($type = 'primary')
    {
        $poolKey = "pool_{$type}";
        
        if (empty($this->pool[$poolKey])) {
            if (count($this->connections[$type] ?? []) < $this->maxConnections) {
                $connection = $this->createConnection($type);
                $this->connections[$type][] = $connection;
                return $connection;
            } else {
                throw new \Exception("No available connections in pool");
            }
        }
        
        return array_pop($this->pool[$poolKey]);
    }
    
    public function releaseConnection($connection, $type = 'primary')
    {
        $poolKey = "pool_{$type}";
        
        if (count($this->pool[$poolKey] ?? []) < $this->maxConnections) {
            $this->pool[$poolKey][] = $connection;
        } else {
            $this->closeConnection($connection);
        }
    }
    
    private function createConnection($type)
    {
        $dsn = $this->getDsn($type);
        $options = $this->getConnectionOptions();
        
        $pdo = new PDO($dsn, $options['username'], $options['password'], $options['pdo_options']);
        
        // Set connection attributes
        $pdo->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);
        $pdo->setAttribute(PDO::ATTR_DEFAULT_FETCH_MODE, PDO::FETCH_ASSOC);
        
        // Record metrics
        Metrics::record("database_connections", 1, ["type" => $type]);
        
        return $pdo;
    }
    
    private function getDsn($type)
    {
        switch ($type) {
            case 'primary':
                return Env::secure('DB_PRIMARY_DSN');
            case 'replica':
                return $this->getReplicaDsn();
            default:
                throw new \Exception("Unknown connection type: {$type}");
        }
    }
    
    private function getReplicaDsn()
    {
        $replicas = $this->config->get('database.replicas', []);
        
        if (empty($replicas)) {
            return Env::secure('DB_PRIMARY_DSN');
        }
        
        // Load balance between replicas
        $replica = $replicas[array_rand($replicas)];
        return $replica['dsn'];
    }
    
    private function getConnectionOptions()
    {
        return [
            'username' => Env::get('DB_USERNAME', 'root'),
            'password' => Env::secure('DB_PASSWORD'),
            'pdo_options' => [
                PDO::ATTR_TIMEOUT => 5,
                PDO::ATTR_PERSISTENT => false,
                PDO::MYSQL_ATTR_INIT_COMMAND => "SET NAMES utf8mb4"
            ]
        ];
    }
    
    private function closeConnection($connection)
    {
        $connection = null;
        Metrics::record("database_connections_closed", 1);
    }
    
    private function initializePool()
    {
        // Pre-create minimum connections
        for ($i = 0; $i < $this->minConnections; $i++) {
            $connection = $this->createConnection('primary');
            $this->pool['pool_primary'][] = $connection;
        }
    }
}

class DatabaseManager
{
    private $config;
    private $pool;
    private $queryCache;
    
    public function __construct(ConnectionPool $pool)
    {
        $this->config = new Config();
        $this->pool = $pool;
        $this->queryCache = new QueryCache();
    }
    
    public function query($sql, $params = [], $type = 'primary')
    {
        $startTime = microtime(true);
        
        try {
            $connection = $this->pool->getConnection($type);
            
            // Check query cache
            $cacheKey = $this->generateCacheKey($sql, $params);
            $cachedResult = $this->queryCache->get($cacheKey);
            
            if ($cachedResult !== null) {
                $this->pool->releaseConnection($connection, $type);
                return $cachedResult;
            }
            
            // Execute query
            $stmt = $connection->prepare($sql);
            $stmt->execute($params);
            $result = $stmt->fetchAll();
            
            // Cache result
            $this->queryCache->set($cacheKey, $result);
            
            $duration = (microtime(true) - $startTime) * 1000;
            
            // Record metrics
            Metrics::record("database_query_duration", $duration, [
                "type" => $type,
                "sql" => $this->normalizeSql($sql)
            ]);
            
            $this->pool->releaseConnection($connection, $type);
            
            return $result;
            
        } catch (\Exception $e) {
            $this->pool->releaseConnection($connection, $type);
            
            Metrics::record("database_query_errors", 1, [
                "type" => $type,
                "error" => get_class($e)
            ]);
            
            throw $e;
        }
    }
    
    public function transaction(callable $callback, $type = 'primary')
    {
        $connection = $this->pool->getConnection($type);
        
        try {
            $connection->beginTransaction();
            $result = $callback($connection);
            $connection->commit();
            
            $this->pool->releaseConnection($connection, $type);
            return $result;
            
        } catch (\Exception $e) {
            $connection->rollBack();
            $this->pool->releaseConnection($connection, $type);
            throw $e;
        }
    }
    
    private function generateCacheKey($sql, $params)
    {
        return md5($sql . serialize($params));
    }
    
    private function normalizeSql($sql)
    {
        // Remove extra whitespace and normalize
        return preg_replace('/\s+/', ' ', trim($sql));
    }
}
```

## Database Sharding
```php
<?php

namespace App\Database\Sharding;

use TuskLang\Config;

class ShardingManager
{
    private $config;
    private $shards = [];
    private $shardingStrategy;
    
    public function __construct()
    {
        $this->config = new Config();
        $this->loadShards();
        $this->loadShardingStrategy();
    }
    
    public function getShard($key)
    {
        return $this->shardingStrategy->getShard($key, $this->shards);
    }
    
    public function query($sql, $params = [], $key = null)
    {
        if ($key === null) {
            // Query all shards
            return $this->queryAllShards($sql, $params);
        }
        
        $shard = $this->getShard($key);
        return $shard->query($sql, $params);
    }
    
    public function queryAllShards($sql, $params = [])
    {
        $results = [];
        
        foreach ($this->shards as $shard) {
            $shardResults = $shard->query($sql, $params);
            $results = array_merge($results, $shardResults);
        }
        
        return $results;
    }
    
    private function loadShards()
    {
        $shardConfigs = $this->config->get('database.shards', []);
        
        foreach ($shardConfigs as $name => $config) {
            $this->shards[$name] = new DatabaseShard($config);
        }
    }
    
    private function loadShardingStrategy()
    {
        $strategy = $this->config->get('database.sharding.strategy', 'hash');
        
        switch ($strategy) {
            case 'hash':
                $this->shardingStrategy = new HashShardingStrategy();
                break;
            case 'range':
                $this->shardingStrategy = new RangeShardingStrategy();
                break;
            case 'consistent_hash':
                $this->shardingStrategy = new ConsistentHashShardingStrategy();
                break;
            default:
                throw new \Exception("Unknown sharding strategy: {$strategy}");
        }
    }
}

class HashShardingStrategy
{
    public function getShard($key, $shards)
    {
        $hash = crc32($key);
        $shardIndex = $hash % count($shards);
        $shardNames = array_keys($shards);
        
        return $shards[$shardNames[$shardIndex]];
    }
}

class RangeShardingStrategy
{
    public function getShard($key, $shards)
    {
        $ranges = [
            'shard1' => ['min' => 0, 'max' => 1000],
            'shard2' => ['min' => 1001, 'max' => 2000],
            'shard3' => ['min' => 2001, 'max' => 3000]
        ];
        
        foreach ($ranges as $shardName => $range) {
            if ($key >= $range['min'] && $key <= $range['max']) {
                return $shards[$shardName];
            }
        }
        
        throw new \Exception("No shard found for key: {$key}");
    }
}

class DatabaseShard
{
    private $config;
    private $connection;
    
    public function __construct($config)
    {
        $this->config = $config;
        $this->connection = new PDO($config['dsn']);
    }
    
    public function query($sql, $params = [])
    {
        $stmt = $this->connection->prepare($sql);
        $stmt->execute($params);
        return $stmt->fetchAll();
    }
}
```

## Read Replica Pattern
```php
<?php

namespace App\Database\Replicas;

use TuskLang\Config;

class ReadReplicaManager
{
    private $config;
    private $primary;
    private $replicas = [];
    private $loadBalancer;
    
    public function __construct()
    {
        $this->config = new Config();
        $this->setupConnections();
        $this->loadBalancer = new LoadBalancer();
    }
    
    public function read($sql, $params = [])
    {
        // Use replica for read operations
        $replica = $this->getReplica();
        return $replica->query($sql, $params);
    }
    
    public function write($sql, $params = [])
    {
        // Use primary for write operations
        return $this->primary->query($sql, $params);
    }
    
    public function transaction(callable $callback)
    {
        // Always use primary for transactions
        return $this->primary->transaction($callback);
    }
    
    private function getReplica()
    {
        return $this->loadBalancer->select($this->replicas);
    }
    
    private function setupConnections()
    {
        // Setup primary connection
        $this->primary = new DatabaseConnection(
            Env::secure('DB_PRIMARY_DSN')
        );
        
        // Setup replica connections
        $replicaConfigs = $this->config->get('database.replicas', []);
        
        foreach ($replicaConfigs as $config) {
            $this->replicas[] = new DatabaseConnection($config['dsn']);
        }
    }
}

class LoadBalancer
{
    private $strategy;
    
    public function __construct($strategy = 'round_robin')
    {
        $this->strategy = $strategy;
    }
    
    public function select($replicas)
    {
        switch ($this->strategy) {
            case 'round_robin':
                return $this->roundRobin($replicas);
            case 'least_connections':
                return $this->leastConnections($replicas);
            case 'weighted':
                return $this->weighted($replicas);
            default:
                return $replicas[0];
        }
    }
    
    private function roundRobin($replicas)
    {
        static $index = 0;
        $replica = $replicas[$index % count($replicas)];
        $index++;
        return $replica;
    }
    
    private function leastConnections($replicas)
    {
        $minConnections = PHP_INT_MAX;
        $selectedReplica = null;
        
        foreach ($replicas as $replica) {
            $connections = $replica->getActiveConnections();
            if ($connections < $minConnections) {
                $minConnections = $connections;
                $selectedReplica = $replica;
            }
        }
        
        return $selectedReplica;
    }
    
    private function weighted($replicas)
    {
        $totalWeight = 0;
        $weights = [];
        
        foreach ($replicas as $replica) {
            $weight = $replica->getWeight();
            $totalWeight += $weight;
            $weights[] = $weight;
        }
        
        $random = mt_rand(1, $totalWeight);
        $currentWeight = 0;
        
        foreach ($replicas as $index => $replica) {
            $currentWeight += $weights[$index];
            if ($random <= $currentWeight) {
                return $replica;
            }
        }
        
        return $replicas[0];
    }
}
```

## Database Migrations
```php
<?php

namespace App\Database\Migrations;

use TuskLang\Config;

class MigrationManager
{
    private $config;
    private $connection;
    private $migrations = [];
    
    public function __construct()
    {
        $this->config = new Config();
        $this->connection = new PDO(Env::secure('DB_PRIMARY_DSN'));
        $this->loadMigrations();
    }
    
    public function migrate($target = null)
    {
        $this->createMigrationsTable();
        
        $appliedMigrations = $this->getAppliedMigrations();
        $pendingMigrations = $this->getPendingMigrations($appliedMigrations);
        
        if ($target !== null) {
            $pendingMigrations = $this->filterToTarget($pendingMigrations, $target);
        }
        
        foreach ($pendingMigrations as $migration) {
            $this->runMigration($migration);
        }
        
        return count($pendingMigrations);
    }
    
    public function rollback($steps = 1)
    {
        $appliedMigrations = $this->getAppliedMigrations();
        $migrationsToRollback = array_slice($appliedMigrations, -$steps);
        
        foreach (array_reverse($migrationsToRollback) as $migration) {
            $this->rollbackMigration($migration);
        }
        
        return count($migrationsToRollback);
    }
    
    private function createMigrationsTable()
    {
        $sql = "
            CREATE TABLE IF NOT EXISTS migrations (
                id INT AUTO_INCREMENT PRIMARY KEY,
                migration VARCHAR(255) NOT NULL,
                batch INT NOT NULL,
                executed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            )
        ";
        
        $this->connection->exec($sql);
    }
    
    private function loadMigrations()
    {
        $migrationPath = $this->config->get('database.migrations.path', 'migrations');
        $files = glob("{$migrationPath}/*.php");
        
        foreach ($files as $file) {
            $this->migrations[] = new Migration($file);
        }
        
        usort($this->migrations, function($a, $b) {
            return strcmp($a->getTimestamp(), $b->getTimestamp());
        });
    }
    
    private function getAppliedMigrations()
    {
        $stmt = $this->connection->query("SELECT migration FROM migrations ORDER BY id");
        return $stmt->fetchAll(PDO::FETCH_COLUMN);
    }
    
    private function getPendingMigrations($appliedMigrations)
    {
        return array_filter($this->migrations, function($migration) use ($appliedMigrations) {
            return !in_array($migration->getName(), $appliedMigrations);
        });
    }
    
    private function runMigration($migration)
    {
        try {
            $this->connection->beginTransaction();
            
            $migration->up($this->connection);
            
            $batch = $this->getNextBatch();
            $stmt = $this->connection->prepare("
                INSERT INTO migrations (migration, batch) VALUES (?, ?)
            ");
            $stmt->execute([$migration->getName(), $batch]);
            
            $this->connection->commit();
            
            echo "Migrated: {$migration->getName()}\n";
            
        } catch (\Exception $e) {
            $this->connection->rollBack();
            throw $e;
        }
    }
    
    private function rollbackMigration($migrationName)
    {
        $migration = $this->findMigration($migrationName);
        
        if (!$migration) {
            throw new \Exception("Migration not found: {$migrationName}");
        }
        
        try {
            $this->connection->beginTransaction();
            
            $migration->down($this->connection);
            
            $stmt = $this->connection->prepare("
                DELETE FROM migrations WHERE migration = ?
            ");
            $stmt->execute([$migrationName]);
            
            $this->connection->commit();
            
            echo "Rolled back: {$migrationName}\n";
            
        } catch (\Exception $e) {
            $this->connection->rollBack();
            throw $e;
        }
    }
    
    private function getNextBatch()
    {
        $stmt = $this->connection->query("SELECT MAX(batch) FROM migrations");
        $maxBatch = $stmt->fetchColumn();
        return ($maxBatch ?? 0) + 1;
    }
    
    private function findMigration($name)
    {
        foreach ($this->migrations as $migration) {
            if ($migration->getName() === $name) {
                return $migration;
            }
        }
        return null;
    }
}

class Migration
{
    private $file;
    private $name;
    private $timestamp;
    
    public function __construct($file)
    {
        $this->file = $file;
        $this->name = basename($file, '.php');
        $this->timestamp = $this->extractTimestamp($this->name);
    }
    
    public function up($connection)
    {
        $migration = $this->loadMigrationClass();
        $migration->up($connection);
    }
    
    public function down($connection)
    {
        $migration = $this->loadMigrationClass();
        $migration->down($connection);
    }
    
    public function getName()
    {
        return $this->name;
    }
    
    public function getTimestamp()
    {
        return $this->timestamp;
    }
    
    private function loadMigrationClass()
    {
        require_once $this->file;
        $className = $this->getClassName();
        return new $className();
    }
    
    private function getClassName()
    {
        return 'Migration_' . str_replace(['-', '_'], '', $this->name);
    }
    
    private function extractTimestamp($name)
    {
        if (preg_match('/^(\d{4}_\d{2}_\d{2}_\d{6})_/', $name, $matches)) {
            return $matches[1];
        }
        return $name;
    }
}
```

## Best Practices
- **Use connection pooling for performance**
- **Implement proper sharding strategies**
- **Use read replicas for scaling reads**
- **Implement database migrations**
- **Monitor query performance**
- **Use transactions appropriately**

## Performance Optimization
- **Optimize database queries**
- **Use query caching**
- **Implement connection pooling**
- **Monitor database performance**

## Security Considerations
- **Use prepared statements**
- **Validate all inputs**
- **Use secure connections**
- **Implement proper access controls**

## Troubleshooting
- **Monitor connection pool health**
- **Check query performance**
- **Verify shard distribution**
- **Monitor replica lag**

## Conclusion
TuskLang + PHP = database patterns that are scalable, performant, and reliable. Build data layers that handle complexity with grace. 