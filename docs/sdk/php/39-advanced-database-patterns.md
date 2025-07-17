# Advanced Database Patterns in PHP with TuskLang

## Overview

TuskLang revolutionizes database operations by making them configuration-driven, intelligent, and adaptive. This guide covers advanced database patterns that leverage TuskLang's dynamic capabilities for optimal performance, scalability, and maintainability.

## 🎯 Multi-Database Architecture

### Database Configuration

```ini
# database-architecture.tsk
[database_clusters]
# Primary cluster for writes
primary = {
    master = "mysql://master:3306/app",
    slaves = ["mysql://slave1:3306/app", "mysql://slave2:3306/app"],
    load_balancing = "round_robin"
}

# Read replicas for analytics
analytics = {
    master = "postgresql://analytics:5432/reports",
    slaves = ["postgresql://analytics-slave1:5432/reports"],
    load_balancing = "least_connections"
}

# Cache layer
cache = {
    redis = "redis://cache:6379/0",
    memcached = "memcached://cache:11211"
}

[database_sharding]
enabled = true
strategy = "hash_based"
shard_count = 8
shard_key = "user_id"

[database_pooling]
enabled = true
min_connections = 5
max_connections = 50
connection_timeout = 30
idle_timeout = 300
```

### Multi-Database Manager Implementation

```php
<?php
// MultiDatabaseManager.php
class MultiDatabaseManager
{
    private $config;
    private $connections = [];
    private $shardManager;
    private $poolManager;
    
    public function __construct()
    {
        $this->config = new TuskConfig('database-architecture.tsk');
        $this->shardManager = new ShardManager();
        $this->poolManager = new ConnectionPoolManager();
        $this->initializeConnections();
    }
    
    private function initializeConnections()
    {
        $clusters = $this->config->get('database_clusters');
        
        foreach ($clusters as $clusterName => $clusterConfig) {
            $this->connections[$clusterName] = $this->createClusterConnection($clusterConfig);
        }
    }
    
    private function createClusterConnection($clusterConfig)
    {
        $master = $this->createConnection($clusterConfig['master']);
        $slaves = [];
        
        foreach ($clusterConfig['slaves'] as $slaveUrl) {
            $slaves[] = $this->createConnection($slaveUrl);
        }
        
        return [
            'master' => $master,
            'slaves' => $slaves,
            'load_balancing' => $clusterConfig['load_balancing']
        ];
    }
    
    public function getConnection($cluster = 'primary', $operation = 'read')
    {
        $clusterConfig = $this->connections[$cluster];
        
        if ($operation === 'write') {
            return $clusterConfig['master'];
        }
        
        // Load balancing for reads
        return $this->getLoadBalancedConnection($clusterConfig);
    }
    
    private function getLoadBalancedConnection($clusterConfig)
    {
        $slaves = $clusterConfig['slaves'];
        $strategy = $clusterConfig['load_balancing'];
        
        switch ($strategy) {
            case 'round_robin':
                return $this->getRoundRobinConnection($slaves);
            case 'least_connections':
                return $this->getLeastConnectionsConnection($slaves);
            case 'weighted':
                return $this->getWeightedConnection($slaves);
            default:
                return $slaves[array_rand($slaves)];
        }
    }
    
    public function executeQuery($sql, $params = [], $cluster = 'primary', $operation = 'read')
    {
        $connection = $this->getConnection($cluster, $operation);
        
        // Apply sharding if enabled
        if ($this->config->get('database_sharding.enabled') && $operation === 'write') {
            $shardId = $this->shardManager->getShardId($params);
            $connection = $this->getShardConnection($shardId);
        }
        
        return $connection->execute($sql, $params);
    }
}
```

## 🔄 Database Sharding

### Sharding Configuration

```ini
# database-sharding.tsk
[database_sharding]
enabled = true
strategy = "hash_based"
shard_count = 8

[database_sharding.shards]
shard_0 = "mysql://shard0:3306/app_shard_0"
shard_1 = "mysql://shard1:3306/app_shard_1"
shard_2 = "mysql://shard2:3306/app_shard_2"
shard_3 = "mysql://shard3:3306/app_shard_3"
shard_4 = "mysql://shard4:3306/app_shard_4"
shard_5 = "mysql://shard5:3306/app_shard_5"
shard_6 = "mysql://shard6:3306/app_shard_6"
shard_7 = "mysql://shard7:3306/app_shard_7"

[database_sharding.rules]
user_data = { shard_key = "user_id", strategy = "hash" }
order_data = { shard_key = "user_id", strategy = "hash" }
product_data = { shard_key = "category_id", strategy = "range" }

[database_sharding.rebalancing]
enabled = true
threshold = 0.2
auto_rebalance = true
```

### Sharding Implementation

```php
class ShardManager
{
    private $config;
    private $shardConnections = [];
    
    public function __construct()
    {
        $this->config = new TuskConfig('database-sharding.tsk');
        $this->initializeShardConnections();
    }
    
    private function initializeShardConnections()
    {
        $shards = $this->config->get('database_sharding.shards');
        
        foreach ($shards as $shardId => $shardUrl) {
            $this->shardConnections[$shardId] = $this->createConnection($shardUrl);
        }
    }
    
    public function getShardId($params)
    {
        $strategy = $this->config->get('database_sharding.strategy');
        $shardCount = $this->config->get('database_sharding.shard_count');
        
        switch ($strategy) {
            case 'hash_based':
                return $this->getHashBasedShardId($params, $shardCount);
            case 'range_based':
                return $this->getRangeBasedShardId($params, $shardCount);
            case 'list_based':
                return $this->getListBasedShardId($params, $shardCount);
        }
    }
    
    private function getHashBasedShardId($params, $shardCount)
    {
        $shardKey = $this->config->get('database_sharding.shard_key', 'user_id');
        $keyValue = $params[$shardKey] ?? null;
        
        if (!$keyValue) {
            throw new InvalidArgumentException("Shard key '{$shardKey}' not found in params");
        }
        
        return hash('crc32', $keyValue) % $shardCount;
    }
    
    private function getRangeBasedShardId($params, $shardCount)
    {
        $shardKey = $this->config->get('database_sharding.shard_key', 'id');
        $keyValue = $params[$shardKey] ?? 0;
        
        return floor($keyValue / $shardCount);
    }
    
    public function getShardConnection($shardId)
    {
        $shardKey = "shard_{$shardId}";
        
        if (!isset($this->shardConnections[$shardKey])) {
            throw new InvalidArgumentException("Shard {$shardId} not found");
        }
        
        return $this->shardConnections[$shardKey];
    }
    
    public function executeShardedQuery($sql, $params, $table)
    {
        $rules = $this->config->get('database_sharding.rules');
        $tableRule = $rules[$table] ?? null;
        
        if (!$tableRule) {
            // Execute on all shards
            return $this->executeOnAllShards($sql, $params);
        }
        
        $shardId = $this->getShardId($params);
        $connection = $this->getShardConnection($shardId);
        
        return $connection->execute($sql, $params);
    }
    
    private function executeOnAllShards($sql, $params)
    {
        $results = [];
        
        foreach ($this->shardConnections as $shardId => $connection) {
            $results[$shardId] = $connection->execute($sql, $params);
        }
        
        return $this->mergeShardResults($results);
    }
}
```

## 🧠 Intelligent Query Optimization

### Query Optimization Configuration

```ini
# query-optimization.tsk
[query_optimization]
enabled = true
learning_mode = true
auto_optimize = true

[query_optimization.strategies]
index_hints = true
query_rewriting = true
result_caching = true
connection_pooling = true

[query_optimization.caching]
enabled = true
ttl = 300
max_cache_size = "100MB"
cache_key_strategy = "query_hash"

[query_optimization.monitoring]
slow_query_threshold = 1000
query_logging = true
performance_metrics = true
```

### Intelligent Query Optimizer

```php
class IntelligentQueryOptimizer
{
    private $config;
    private $queryCache;
    private $learningEngine;
    private $metrics;
    
    public function __construct()
    {
        $this->config = new TuskConfig('query-optimization.tsk');
        $this->queryCache = new QueryCache();
        $this->learningEngine = new QueryLearningEngine();
        $this->metrics = new MetricsCollector();
    }
    
    public function optimizeQuery($sql, $params = [], $context = [])
    {
        if (!$this->config->get('query_optimization.enabled')) {
            return ['sql' => $sql, 'params' => $params];
        }
        
        $startTime = microtime(true);
        
        // Check cache first
        $cacheKey = $this->generateCacheKey($sql, $params);
        $cachedResult = $this->queryCache->get($cacheKey);
        
        if ($cachedResult) {
            $this->metrics->record('query_cache_hit', ['cache_key' => $cacheKey]);
            return $cachedResult;
        }
        
        // Apply optimization strategies
        $optimizedQuery = $this->applyOptimizationStrategies($sql, $params, $context);
        
        // Learn from optimization
        if ($this->config->get('query_optimization.learning_mode')) {
            $this->learningEngine->recordOptimization($sql, $optimizedQuery, $context);
        }
        
        // Cache optimized query
        if ($this->config->get('query_optimization.caching.enabled')) {
            $this->queryCache->set($cacheKey, $optimizedQuery, 
                $this->config->get('query_optimization.caching.ttl'));
        }
        
        $duration = (microtime(true) - $startTime) * 1000;
        $this->metrics->record('query_optimization_duration', ['duration' => $duration]);
        
        return $optimizedQuery;
    }
    
    private function applyOptimizationStrategies($sql, $params, $context)
    {
        $optimizedSql = $sql;
        $optimizedParams = $params;
        
        // Index hints
        if ($this->config->get('query_optimization.strategies.index_hints')) {
            $optimizedSql = $this->addIndexHints($optimizedSql, $context);
        }
        
        // Query rewriting
        if ($this->config->get('query_optimization.strategies.query_rewriting')) {
            $optimizedSql = $this->rewriteQuery($optimizedSql, $context);
        }
        
        // Parameter optimization
        $optimizedParams = $this->optimizeParameters($optimizedParams, $context);
        
        return [
            'sql' => $optimizedSql,
            'params' => $optimizedParams,
            'optimizations_applied' => $this->getAppliedOptimizations()
        ];
    }
    
    private function addIndexHints($sql, $context)
    {
        // Analyze query and add appropriate index hints
        $tableHints = $this->learningEngine->getOptimalIndexHints($sql, $context);
        
        foreach ($tableHints as $table => $index) {
            $sql = $this->insertIndexHint($sql, $table, $index);
        }
        
        return $sql;
    }
    
    private function rewriteQuery($sql, $context)
    {
        // Apply query rewriting rules
        $rewritingRules = $this->config->get('query_optimization.rewriting_rules', []);
        
        foreach ($rewritingRules as $rule) {
            if ($this->shouldApplyRule($rule, $sql, $context)) {
                $sql = $this->applyRewritingRule($rule, $sql);
            }
        }
        
        return $sql;
    }
    
    private function optimizeParameters($params, $context)
    {
        // Optimize parameter types and values
        $optimizedParams = [];
        
        foreach ($params as $key => $value) {
            $optimizedParams[$key] = $this->optimizeParameterValue($value, $context);
        }
        
        return $optimizedParams;
    }
}
```

## 🔄 Database Migration and Schema Management

### Migration Configuration

```ini
# database-migration.tsk
[database_migration]
strategy = "versioned"
rollback_enabled = true
backup_before_migration = true

[database_migration.environments]
development = {
    auto_migrate = true,
    seed_data = true,
    backup_strategy = "none"
}

staging = {
    auto_migrate = false,
    seed_data = false,
    backup_strategy = "full"
}

production = {
    auto_migrate = false,
    seed_data = false,
    backup_strategy = "full",
    downtime_window = "02:00-04:00"
}

[database_migration.schemas]
user_schema = "schemas/user_schema.tsk"
product_schema = "schemas/product_schema.tsk"
order_schema = "schemas/order_schema.tsk"
```

### Schema Definition Example

```ini
# schemas/user_schema.tsk
[user_schema]
version = "1.2.0"
description = "User management schema"

[user_schema.tables.users]
columns = [
    { name = "id", type = "BIGINT", primary_key = true, auto_increment = true },
    { name = "email", type = "VARCHAR(255)", unique = true, nullable = false },
    { name = "password_hash", type = "VARCHAR(255)", nullable = false },
    { name = "first_name", type = "VARCHAR(100)", nullable = true },
    { name = "last_name", type = "VARCHAR(100)", nullable = true },
    { name = "created_at", type = "TIMESTAMP", default = "CURRENT_TIMESTAMP" },
    { name = "updated_at", type = "TIMESTAMP", default = "CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP" }
]

indexes = [
    { name = "idx_email", columns = ["email"], type = "UNIQUE" },
    { name = "idx_created_at", columns = ["created_at"], type = "BTREE" }
]

[user_schema.tables.user_profiles]
columns = [
    { name = "user_id", type = "BIGINT", foreign_key = "users.id" },
    { name = "bio", type = "TEXT", nullable = true },
    { name = "avatar_url", type = "VARCHAR(500)", nullable = true },
    { name = "preferences", type = "JSON", nullable = true }
]
```

### Migration Manager Implementation

```php
class MigrationManager
{
    private $config;
    private $database;
    private $schemaManager;
    
    public function __construct()
    {
        $this->config = new TuskConfig('database-migration.tsk');
        $this->database = new MultiDatabaseManager();
        $this->schemaManager = new SchemaManager();
    }
    
    public function migrate($environment = null)
    {
        $environment = $environment ?: $this->getCurrentEnvironment();
        $envConfig = $this->config->get("database_migration.environments.{$environment}");
        
        // Check if migration is allowed
        if (!$envConfig['auto_migrate']) {
            throw new MigrationException("Auto migration not allowed for {$environment}");
        }
        
        // Create backup if required
        if ($envConfig['backup_strategy'] === 'full') {
            $this->createBackup($environment);
        }
        
        // Get pending migrations
        $pendingMigrations = $this->getPendingMigrations();
        
        foreach ($pendingMigrations as $migration) {
            $this->executeMigration($migration, $environment);
        }
        
        // Seed data if required
        if ($envConfig['seed_data']) {
            $this->seedData($environment);
        }
    }
    
    private function executeMigration($migration, $environment)
    {
        $this->database->beginTransaction();
        
        try {
            // Execute migration
            $this->executeMigrationFile($migration);
            
            // Update migration history
            $this->recordMigration($migration);
            
            $this->database->commit();
            
            $this->logMigration($migration, 'success');
        } catch (Exception $e) {
            $this->database->rollback();
            $this->logMigration($migration, 'failed', $e->getMessage());
            throw $e;
        }
    }
    
    private function executeMigrationFile($migration)
    {
        $migrationPath = "migrations/{$migration}.tsk";
        $migrationConfig = new TuskConfig($migrationPath);
        
        $operations = $migrationConfig->get('operations', []);
        
        foreach ($operations as $operation) {
            $this->executeOperation($operation);
        }
    }
    
    private function executeOperation($operation)
    {
        $type = $operation['type'];
        
        switch ($type) {
            case 'create_table':
                $this->createTable($operation);
                break;
            case 'alter_table':
                $this->alterTable($operation);
                break;
            case 'drop_table':
                $this->dropTable($operation);
                break;
            case 'create_index':
                $this->createIndex($operation);
                break;
            case 'execute_sql':
                $this->executeSql($operation['sql'], $operation['params'] ?? []);
                break;
        }
    }
}
```

## 📊 Database Analytics and Monitoring

### Database Analytics Configuration

```ini
# database-analytics.tsk
[database_analytics]
enabled = true
sampling_rate = 0.1
metrics_retention = 30

[database_analytics.metrics]
query_performance = true
connection_usage = true
slow_queries = true
error_rates = true
resource_usage = true

[database_analytics.alerts]
slow_query_threshold = 5000
error_rate_threshold = 0.05
connection_usage_threshold = 0.8
```

### Database Analytics Implementation

```php
class DatabaseAnalytics
{
    private $config;
    private $metrics;
    private $database;
    
    public function __construct()
    {
        $this->config = new TuskConfig('database-analytics.tsk');
        $this->metrics = new MetricsCollector();
        $this->database = new MultiDatabaseManager();
    }
    
    public function recordQuery($sql, $params, $duration, $success, $context = [])
    {
        if (!$this->config->get('database_analytics.enabled')) {
            return;
        }
        
        // Apply sampling
        if (rand(1, 100) > ($this->config->get('database_analytics.sampling_rate') * 100)) {
            return;
        }
        
        $data = [
            'sql' => $sql,
            'params' => json_encode($params),
            'duration' => $duration,
            'success' => $success,
            'timestamp' => time(),
            'context' => json_encode($context)
        ];
        
        $this->database->executeQuery(
            "INSERT INTO query_analytics (sql, params, duration, success, timestamp, context) VALUES (?, ?, ?, ?, ?, ?)",
            [$data['sql'], $data['params'], $data['duration'], $data['success'], $data['timestamp'], $data['context']]
        );
        
        $this->checkAlerts($data);
    }
    
    public function getQueryMetrics($timeRange = 3600)
    {
        $metrics = [];
        
        if ($this->config->get('database_analytics.metrics.query_performance')) {
            $metrics['query_performance'] = $this->getQueryPerformanceMetrics($timeRange);
        }
        
        if ($this->config->get('database_analytics.metrics.slow_queries')) {
            $metrics['slow_queries'] = $this->getSlowQueryMetrics($timeRange);
        }
        
        if ($this->config->get('database_analytics.metrics.error_rates')) {
            $metrics['error_rates'] = $this->getErrorRateMetrics($timeRange);
        }
        
        return $metrics;
    }
    
    private function getQueryPerformanceMetrics($timeRange)
    {
        $sql = "
            SELECT 
                AVG(duration) as avg_duration,
                MAX(duration) as max_duration,
                COUNT(*) as total_queries,
                COUNT(CASE WHEN success = 1 THEN 1 END) as successful_queries
            FROM query_analytics 
            WHERE timestamp > ?
        ";
        
        $result = $this->database->executeQuery($sql, [time() - $timeRange]);
        return $result->fetch();
    }
    
    private function checkAlerts($data)
    {
        $slowQueryThreshold = $this->config->get('database_analytics.alerts.slow_query_threshold');
        
        if ($data['duration'] > $slowQueryThreshold) {
            $this->triggerAlert('slow_query', [
                'sql' => $data['sql'],
                'duration' => $data['duration'],
                'threshold' => $slowQueryThreshold
            ]);
        }
        
        // Check error rate
        $errorRate = $this->getErrorRate(300); // Last 5 minutes
        $errorRateThreshold = $this->config->get('database_analytics.alerts.error_rate_threshold');
        
        if ($errorRate > $errorRateThreshold) {
            $this->triggerAlert('high_error_rate', [
                'error_rate' => $errorRate,
                'threshold' => $errorRateThreshold
            ]);
        }
    }
}
```

## 🔒 Database Security

### Database Security Configuration

```ini
# database-security.tsk
[database_security]
encryption_enabled = true
encryption_algorithm = "AES-256-GCM"
connection_encryption = "SSL"

[database_security.access_control]
row_level_security = true
column_level_security = true
query_filtering = true

[database_security.auditing]
enabled = true
log_all_queries = false
log_sensitive_operations = true
audit_retention = 365

[database_security.sanitization]
sql_injection_protection = true
parameter_binding = true
input_validation = true
```

### Secure Database Operations

```php
class SecureDatabaseManager
{
    private $config;
    private $encryption;
    private $auditLogger;
    private $accessControl;
    
    public function __construct()
    {
        $this->config = new TuskConfig('database-security.tsk');
        $this->encryption = new DatabaseEncryption();
        $this->auditLogger = new AuditLogger();
        $this->accessControl = new DatabaseAccessControl();
    }
    
    public function executeSecureQuery($sql, $params = [], $context = [])
    {
        // Validate access
        if (!$this->accessControl->canExecute($sql, $context)) {
            throw new AccessDeniedException("Access denied for query");
        }
        
        // Sanitize input
        $sanitizedParams = $this->sanitizeParameters($params);
        
        // Apply row-level security
        $securedSql = $this->applyRowLevelSecurity($sql, $context);
        
        // Log sensitive operations
        if ($this->isSensitiveOperation($sql)) {
            $this->auditLogger->logSensitiveOperation($sql, $sanitizedParams, $context);
        }
        
        // Execute query
        $result = $this->database->executeQuery($securedSql, $sanitizedParams);
        
        // Encrypt sensitive data in result
        $encryptedResult = $this->encryptSensitiveData($result, $context);
        
        return $encryptedResult;
    }
    
    private function sanitizeParameters($params)
    {
        $sanitizedParams = [];
        
        foreach ($params as $key => $value) {
            $sanitizedParams[$key] = $this->sanitizeValue($value);
        }
        
        return $sanitizedParams;
    }
    
    private function sanitizeValue($value)
    {
        if (is_string($value)) {
            // Remove potential SQL injection patterns
            $value = preg_replace('/[;\'"]/', '', $value);
            $value = htmlspecialchars($value, ENT_QUOTES, 'UTF-8');
        }
        
        return $value;
    }
    
    private function applyRowLevelSecurity($sql, $context)
    {
        if (!$this->config->get('database_security.access_control.row_level_security')) {
            return $sql;
        }
        
        $userId = $context['user_id'] ?? null;
        $userRole = $context['user_role'] ?? null;
        
        // Add row-level security conditions
        $securityConditions = $this->getSecurityConditions($userId, $userRole);
        
        return $this->injectSecurityConditions($sql, $securityConditions);
    }
    
    private function encryptSensitiveData($result, $context)
    {
        if (!$this->config->get('database_security.encryption_enabled')) {
            return $result;
        }
        
        $sensitiveColumns = $this->getSensitiveColumns($context);
        
        foreach ($result as &$row) {
            foreach ($sensitiveColumns as $column) {
                if (isset($row[$column])) {
                    $row[$column] = $this->encryption->encrypt($row[$column]);
                }
            }
        }
        
        return $result;
    }
}
```

## 📋 Best Practices

### Database Best Practices

1. **Multi-Database Architecture**: Use appropriate databases for different use cases
2. **Sharding Strategy**: Implement intelligent sharding based on data patterns
3. **Query Optimization**: Use learning-based query optimization
4. **Migration Management**: Version-controlled schema migrations
5. **Security First**: Implement comprehensive database security
6. **Monitoring**: Track performance metrics and set up alerts
7. **Backup Strategy**: Regular backups with point-in-time recovery
8. **Connection Pooling**: Optimize database connections

### Integration Examples

```php
// Integration with Laravel
class TuskLangDatabaseManager implements DatabaseManagerInterface
{
    public function connection($name = null)
    {
        return new TuskLangConnection();
    }
}

// Integration with Doctrine
class TuskLangConnection implements ConnectionInterface
{
    public function executeQuery($sql, $params = [])
    {
        $manager = new MultiDatabaseManager();
        return $manager->executeQuery($sql, $params);
    }
}
```

## 🔧 Troubleshooting

### Common Issues

1. **Connection Pool Exhaustion**: Monitor connection usage and adjust pool size
2. **Shard Imbalance**: Implement automatic shard rebalancing
3. **Slow Queries**: Use query optimization and indexing strategies
4. **Migration Failures**: Always backup before migrations
5. **Security Vulnerabilities**: Implement comprehensive input validation

### Debug Configuration

```ini
# debug-database.tsk
[debug]
enabled = true
log_level = "verbose"
trace_queries = true

[debug.output]
console = true
file = "/var/log/tusk-database-debug.log"
```

This comprehensive database system leverages TuskLang's configuration-driven approach to create intelligent, secure, and high-performance database solutions that adapt to application needs automatically. 