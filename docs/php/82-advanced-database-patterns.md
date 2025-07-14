# Advanced Database Patterns

TuskLang enables PHP developers to implement sophisticated database patterns with confidence. This guide covers advanced database strategies, optimization, and architectural patterns.

## Table of Contents
- [Database Configuration](#database-configuration)
- [Connection Pooling](#connection-pooling)
- [Query Optimization](#query-optimization)
- [Database Patterns](#database-patterns)
- [Best Practices](#best-practices)

## Database Configuration

```php
// config/database.tsk
database = {
    connections = {
        primary = {
            host = "@env('DB_HOST')"
            port = 3306
            database = "@env('DB_NAME')"
            username = "@env('DB_USER')"
            password = "@env('DB_PASSWORD')"
            charset = "utf8mb4"
            collation = "utf8mb4_unicode_ci"
        }
        
        read_replicas = [
            {
                host = "@env('DB_REPLICA_1_HOST')"
                port = 3306
                database = "@env('DB_NAME')"
                username = "@env('DB_USER')"
                password = "@env('DB_PASSWORD')"
                weight = 1
            }
        ]
    }
    
    pooling = {
        enabled = true
        min_connections = 5
        max_connections = 50
        idle_timeout = 300
        connection_timeout = 30
    }
    
    optimization = {
        query_cache = true
        prepared_statements = true
        connection_pooling = true
        read_replicas = true
    }
}
```

## Connection Pooling

```php
<?php
// app/Infrastructure/Database/ConnectionPool.php

namespace App\Infrastructure\Database;

use TuskLang\Database\ConnectionPoolInterface;

class ConnectionPool implements ConnectionPoolInterface
{
    private array $connections = [];
    private array $config;
    private int $currentConnections = 0;
    
    public function __construct(array $config)
    {
        $this->config = $config;
    }
    
    public function getConnection(): \PDO
    {
        if (empty($this->connections)) {
            if ($this->currentConnections < $this->config['pooling']['max_connections']) {
                return $this->createConnection();
            }
            throw new \RuntimeException('No available connections');
        }
        
        return array_pop($this->connections);
    }
    
    public function releaseConnection(\PDO $connection): void
    {
        if (count($this->connections) < $this->config['pooling']['max_connections']) {
            $this->connections[] = $connection;
        } else {
            $connection = null;
            $this->currentConnections--;
        }
    }
    
    private function createConnection(): \PDO
    {
        $config = $this->config['connections']['primary'];
        $dsn = "mysql:host={$config['host']};port={$config['port']};dbname={$config['database']};charset={$config['charset']}";
        
        $pdo = new \PDO($dsn, $config['username'], $config['password'], [
            \PDO::ATTR_ERRMODE => \PDO::ERRMODE_EXCEPTION,
            \PDO::ATTR_DEFAULT_FETCH_MODE => \PDO::FETCH_ASSOC,
            \PDO::ATTR_EMULATE_PREPARES => false
        ]);
        
        $this->currentConnections++;
        return $pdo;
    }
}
```

## Query Optimization

```php
<?php
// app/Infrastructure/Database/QueryOptimizer.php

namespace App\Infrastructure\Database;

class QueryOptimizer
{
    private \PDO $connection;
    
    public function __construct(\PDO $connection)
    {
        $this->connection = $connection;
    }
    
    public function optimizeQuery(string $sql): string
    {
        // Remove unnecessary whitespace
        $sql = preg_replace('/\s+/', ' ', trim($sql));
        
        // Add query hints
        if (preg_match('/SELECT\s+/i', $sql)) {
            $sql = preg_replace('/SELECT\s+/i', 'SELECT SQL_CALC_FOUND_ROWS ', $sql, 1);
        }
        
        return $sql;
    }
    
    public function analyzeQuery(string $sql): array
    {
        $stmt = $this->connection->prepare("EXPLAIN " . $sql);
        $stmt->execute();
        
        return $stmt->fetchAll();
    }
    
    public function suggestIndexes(string $sql): array
    {
        $analysis = $this->analyzeQuery($sql);
        $suggestions = [];
        
        foreach ($analysis as $row) {
            if ($row['type'] === 'ALL') {
                $suggestions[] = "Add index on {$row['table']}.{$row['possible_keys']}";
            }
        }
        
        return $suggestions;
    }
}
```

## Database Patterns

```php
<?php
// app/Infrastructure/Database/Repository/UserRepository.php

namespace App\Infrastructure\Database\Repository;

use App\Domain\Entities\User;
use App\Domain\Repositories\UserRepositoryInterface;

class UserRepository implements UserRepositoryInterface
{
    private \PDO $connection;
    private QueryOptimizer $optimizer;
    
    public function __construct(\PDO $connection, QueryOptimizer $optimizer)
    {
        $this->connection = $connection;
        $this->optimizer = $optimizer;
    }
    
    public function findById(int $id): ?User
    {
        $sql = "SELECT * FROM users WHERE id = ?";
        $sql = $this->optimizer->optimizeQuery($sql);
        
        $stmt = $this->connection->prepare($sql);
        $stmt->execute([$id]);
        
        $data = $stmt->fetch();
        return $data ? $this->mapToEntity($data) : null;
    }
    
    public function findByEmail(string $email): ?User
    {
        $sql = "SELECT * FROM users WHERE email = ?";
        $sql = $this->optimizer->optimizeQuery($sql);
        
        $stmt = $this->connection->prepare($sql);
        $stmt->execute([$email]);
        
        $data = $stmt->fetch();
        return $data ? $this->mapToEntity($data) : null;
    }
    
    public function save(User $user): void
    {
        if ($user->getId()) {
            $this->update($user);
        } else {
            $this->insert($user);
        }
    }
    
    private function insert(User $user): void
    {
        $sql = "INSERT INTO users (email, name, created_at) VALUES (?, ?, ?)";
        $sql = $this->optimizer->optimizeQuery($sql);
        
        $stmt = $this->connection->prepare($sql);
        $stmt->execute([
            $user->getEmail(),
            $user->getName(),
            $user->getCreatedAt()->format('Y-m-d H:i:s')
        ]);
        
        $user->setId($this->connection->lastInsertId());
    }
    
    private function update(User $user): void
    {
        $sql = "UPDATE users SET email = ?, name = ?, updated_at = ? WHERE id = ?";
        $sql = $this->optimizer->optimizeQuery($sql);
        
        $stmt = $this->connection->prepare($sql);
        $stmt->execute([
            $user->getEmail(),
            $user->getName(),
            date('Y-m-d H:i:s'),
            $user->getId()
        ]);
    }
    
    private function mapToEntity(array $data): User
    {
        return new User(
            $data['id'],
            $data['email'],
            $data['name'],
            new \DateTimeImmutable($data['created_at'])
        );
    }
}
```

## Best Practices

```php
// config/database-best-practices.tsk
database_best_practices = {
    connection_management = {
        use_connection_pooling = true
        implement_read_replicas = true
        handle_connection_failures = true
        monitor_connection_health = true
    }
    
    query_optimization = {
        use_prepared_statements = true
        optimize_query_structure = true
        implement_query_caching = true
        monitor_query_performance = true
    }
    
    data_integrity = {
        use_transactions = true
        implement_constraints = true
        validate_data = true
        handle_concurrency = true
    }
    
    performance = {
        use_indexes_properly = true
        optimize_table_structure = true
        implement_partitioning = true
        monitor_performance_metrics = true
    }
}

// Example usage in PHP
class DatabaseBestPractices
{
    public function implementBestPractices(): void
    {
        // 1. Configure database
        $config = TuskLang::load('database');
        $pool = new ConnectionPool($config);
        
        // 2. Get connection from pool
        $connection = $pool->getConnection();
        
        // 3. Use query optimizer
        $optimizer = new QueryOptimizer($connection);
        
        // 4. Implement repository pattern
        $userRepo = new UserRepository($connection, $optimizer);
        
        // 5. Use transactions
        $connection->beginTransaction();
        
        try {
            $user = $userRepo->findById(123);
            $user->updateName('New Name');
            $userRepo->save($user);
            
            $connection->commit();
        } catch (\Exception $e) {
            $connection->rollBack();
            throw $e;
        } finally {
            $pool->releaseConnection($connection);
        }
        
        // 6. Monitor performance
        $analysis = $optimizer->analyzeQuery("SELECT * FROM users WHERE email = 'test@example.com'");
        $suggestions = $optimizer->suggestIndexes("SELECT * FROM users WHERE email = 'test@example.com'");
        
        // 7. Log and monitor
        $this->logger->info('Database operations completed', [
            'analysis' => $analysis,
            'suggestions' => $suggestions,
            'connection_pool_size' => count($pool->getConnections())
        ]);
    }
}
```

This comprehensive guide covers advanced database patterns in TuskLang with PHP integration. The database system is designed to be performant, reliable, and maintainable while maintaining the rebellious spirit of TuskLang development. 