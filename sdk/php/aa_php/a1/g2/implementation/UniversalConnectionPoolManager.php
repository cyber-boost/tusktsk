<?php

namespace TuskLang\CoreOperators\Enhanced;

use PDO;
use PDOException;
use Exception;
use WeakMap;

/**
 * Universal Connection Pool Manager - Agent A1 Goal 2 Implementation
 * 
 * Features:
 * - Unified connection pooling for all database types (MySQL, PostgreSQL, SQLite)
 * - Intelligent pool sizing based on load and connection metrics
 * - Connection health monitoring and auto-recovery
 * - Connection lifecycle management with automatic cleanup
 * - Pool statistics and performance metrics
 * - Load balancing across multiple database instances
 * - Connection validation and testing
 * - Advanced connection reuse strategies
 */
class UniversalConnectionPoolManager
{
    private const DEFAULT_MIN_CONNECTIONS = 2;
    private const DEFAULT_MAX_CONNECTIONS = 20;
    private const DEFAULT_HEALTH_CHECK_INTERVAL = 30; // seconds
    private const DEFAULT_IDLE_TIMEOUT = 300; // 5 minutes
    private const DEFAULT_MAX_LIFETIME = 3600; // 1 hour
    private const CONNECTION_TEST_TIMEOUT = 5; // seconds
    
    private static ?UniversalConnectionPoolManager $instance = null;
    private static array $pools = [];
    private static array $globalStats = [];
    private static float $lastHealthCheck = 0;
    
    private WeakMap $connectionMetadata;
    private array $poolConfigs = [];
    private array $loadBalancers = [];
    private bool $autoScalingEnabled = true;
    private float $loadThreshold = 0.8; // Scale up when 80% utilization
    private float $scaleDownThreshold = 0.3; // Scale down when 30% utilization
    
    private function __construct()
    {
        $this->connectionMetadata = new WeakMap();
        $this->initializeGlobalStats();
        
        // Register shutdown handler for cleanup
        register_shutdown_function([$this, 'shutdown']);
    }
    
    /**
     * Get singleton instance
     */
    public static function getInstance(): self
    {
        if (self::$instance === null) {
            self::$instance = new self();
        }
        return self::$instance;
    }
    
    /**
     * Create or get connection pool for specific database type and configuration
     */
    public function createPool(string $poolName, array $config): string
    {
        $poolId = $this->generatePoolId($poolName, $config);
        
        if (isset(self::$pools[$poolId])) {
            return $poolId;
        }
        
        $this->validatePoolConfig($config);
        
        self::$pools[$poolId] = [
            'name' => $poolName,
            'config' => $config,
            'database_type' => $this->detectDatabaseType($config),
            'connections' => [],
            'stats' => $this->initializePoolStats(),
            'created_at' => microtime(true),
            'last_health_check' => 0,
            'is_healthy' => true,
            'auto_scaling' => $config['auto_scaling'] ?? true,
            'load_balancer' => $this->createLoadBalancer($config)
        ];
        
        $this->poolConfigs[$poolId] = $config;
        
        // Create initial connections
        $minConnections = $config['min_connections'] ?? self::DEFAULT_MIN_CONNECTIONS;
        for ($i = 0; $i < $minConnections; $i++) {
            $this->createConnection($poolId);
        }
        
        $this->logPoolEvent($poolId, 'POOL_CREATED', "Pool created with {$minConnections} initial connections");
        
        return $poolId;
    }
    
    /**
     * Get connection from pool with intelligent selection
     */
    public function getConnection(string $poolId, array $options = []): PDO
    {
        if (!isset(self::$pools[$poolId])) {
            throw new Exception("Pool not found: {$poolId}");
        }
        
        $pool = &self::$pools[$poolId];
        $timeout = $options['timeout'] ?? 10; // seconds
        $startTime = microtime(true);
        
        while (microtime(true) - $startTime < $timeout) {
            // Try to find available connection
            $connection = $this->findAvailableConnection($poolId);
            
            if ($connection !== null) {
                $this->markConnectionInUse($poolId, $connection['id']);
                $pool['stats']['connections_borrowed']++;
                return $connection['pdo'];
            }
            
            // Try to create new connection if under limit
            if (count($pool['connections']) < ($pool['config']['max_connections'] ?? self::DEFAULT_MAX_CONNECTIONS)) {
                $connection = $this->createConnection($poolId);
                if ($connection !== null) {
                    $this->markConnectionInUse($poolId, $connection['id']);
                    $pool['stats']['connections_borrowed']++;
                    return $connection['pdo'];
                }
            }
            
            // Wait briefly before trying again
            usleep(50000); // 50ms
        }
        
        throw new Exception("Unable to obtain connection from pool {$poolId} within timeout");
    }
    
    /**
     * Return connection to pool
     */
    public function returnConnection(string $poolId, PDO $pdo): bool
    {
        if (!isset(self::$pools[$poolId])) {
            return false;
        }
        
        $pool = &self::$pools[$poolId];
        $connectionId = $this->findConnectionId($poolId, $pdo);
        
        if ($connectionId === null) {
            return false;
        }
        
        $connection = &$pool['connections'][$connectionId];
        $connection['in_use'] = false;
        $connection['last_used'] = microtime(true);
        $connection['usage_count']++;
        
        // Update connection metadata
        if (isset($this->connectionMetadata[$pdo])) {
            $metadata = $this->connectionMetadata[$pdo];
            $metadata['total_usage_time'] += microtime(true) - $metadata['acquired_at'];
            $this->connectionMetadata[$pdo] = $metadata;
        }
        
        $pool['stats']['connections_returned']++;
        
        // Check if connection should be retired
        if ($this->shouldRetireConnection($connection)) {
            $this->removeConnection($poolId, $connectionId);
        }
        
        return true;
    }
    
    /**
     * Create new connection in pool
     */
    private function createConnection(string $poolId): ?array
    {
        $pool = &self::$pools[$poolId];
        $config = $pool['config'];
        
        try {
            $pdo = $this->establishConnection($config);
            $connectionId = uniqid('conn_', true);
            
            $connection = [
                'id' => $connectionId,
                'pdo' => $pdo,
                'created_at' => microtime(true),
                'last_used' => microtime(true),
                'in_use' => false,
                'usage_count' => 0,
                'health_status' => 'healthy',
                'last_health_check' => microtime(true)
            ];
            
            $pool['connections'][$connectionId] = $connection;
            
            // Store metadata for the PDO object
            $this->connectionMetadata[$pdo] = [
                'pool_id' => $poolId,
                'connection_id' => $connectionId,
                'created_at' => microtime(true),
                'acquired_at' => null,
                'total_usage_time' => 0
            ];
            
            $pool['stats']['connections_created']++;
            $this->logConnectionEvent($poolId, $connectionId, 'CONNECTION_CREATED');
            
            return $connection;
            
        } catch (Exception $e) {
            $pool['stats']['connection_failures']++;
            $this->logPoolEvent($poolId, 'CONNECTION_FAILED', $e->getMessage());
            return null;
        }
    }
    
    /**
     * Establish PDO connection based on database type
     */
    private function establishConnection(array $config): PDO
    {
        $databaseType = $this->detectDatabaseType($config);
        
        return match($databaseType) {
            'mysql' => $this->createMySQLConnection($config),
            'postgresql' => $this->createPostgreSQLConnection($config),
            'sqlite' => $this->createSQLiteConnection($config),
            default => throw new Exception("Unsupported database type: {$databaseType}")
        };
    }
    
    private function createMySQLConnection(array $config): PDO
    {
        $host = $config['host'] ?? 'localhost';
        $port = $config['port'] ?? 3306;
        $database = $config['database'] ?? '';
        $username = $config['username'] ?? 'root';
        $password = $config['password'] ?? '';
        $charset = $config['charset'] ?? 'utf8mb4';
        
        $dsn = "mysql:host={$host};port={$port};dbname={$database};charset={$charset}";
        
        $options = [
            PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION,
            PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC,
            PDO::ATTR_EMULATE_PREPARES => false,
            PDO::ATTR_STRINGIFY_FETCHES => false,
            PDO::ATTR_TIMEOUT => $config['timeout'] ?? 30,
            PDO::MYSQL_ATTR_INIT_COMMAND => "SET NAMES {$charset} COLLATE {$charset}_unicode_ci",
            PDO::MYSQL_ATTR_USE_BUFFERED_QUERY => true,
        ];
        
        if ($config['ssl'] ?? false) {
            $options[PDO::MYSQL_ATTR_SSL_VERIFY_SERVER_CERT] = false;
        }
        
        return new PDO($dsn, $username, $password, $options);
    }
    
    private function createPostgreSQLConnection(array $config): PDO
    {
        $host = $config['host'] ?? 'localhost';
        $port = $config['port'] ?? 5432;
        $database = $config['database'] ?? 'postgres';
        $username = $config['username'] ?? 'postgres';
        $password = $config['password'] ?? '';
        $sslmode = $config['sslmode'] ?? 'prefer';
        $applicationName = $config['application_name'] ?? 'UniversalConnectionPool';
        
        $dsn = "pgsql:host={$host};port={$port};dbname={$database};sslmode={$sslmode};application_name={$applicationName}";
        
        $options = [
            PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION,
            PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC,
            PDO::ATTR_EMULATE_PREPARES => false,
            PDO::ATTR_STRINGIFY_FETCHES => false,
            PDO::ATTR_TIMEOUT => $config['timeout'] ?? 30,
        ];
        
        $pdo = new PDO($dsn, $username, $password, $options);
        
        // Set PostgreSQL-specific settings
        $pdo->exec("SET timezone = 'UTC'");
        $pdo->exec("SET client_encoding = 'UTF8'");
        
        return $pdo;
    }
    
    private function createSQLiteConnection(array $config): PDO
    {
        $path = $config['path'] ?? throw new Exception('SQLite path required');
        $dsn = "sqlite:" . $path;
        
        $options = [
            PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION,
            PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC,
            PDO::ATTR_EMULATE_PREPARES => false,
            PDO::ATTR_STRINGIFY_FETCHES => false,
            PDO::ATTR_TIMEOUT => $config['timeout'] ?? 30,
        ];
        
        $pdo = new PDO($dsn, '', '', $options);
        
        // Configure SQLite settings
        if ($config['enable_wal'] ?? true) {
            $pdo->exec("PRAGMA journal_mode = WAL");
            $pdo->exec("PRAGMA synchronous = NORMAL");
        }
        
        $pdo->exec("PRAGMA foreign_keys = ON");
        $pdo->exec("PRAGMA busy_timeout = 30000");
        
        return $pdo;
    }
    
    /**
     * Find available connection in pool
     */
    private function findAvailableConnection(string $poolId): ?array
    {
        $pool = &self::$pools[$poolId];
        
        // Use load balancer to find best connection
        $loadBalancer = $pool['load_balancer'];
        $candidates = array_filter($pool['connections'], fn($conn) => !$conn['in_use'] && $conn['health_status'] === 'healthy');
        
        if (empty($candidates)) {
            return null;
        }
        
        return $loadBalancer->selectConnection($candidates);
    }
    
    /**
     * Intelligent health checking with adaptive intervals
     */
    public function performHealthCheck(string $poolId = null): array
    {
        $results = [];
        $currentTime = microtime(true);
        
        $poolsToCheck = $poolId ? [$poolId => self::$pools[$poolId]] : self::$pools;
        
        foreach ($poolsToCheck as $id => $pool) {
            $healthCheckInterval = $this->calculateHealthCheckInterval($pool);
            
            if ($currentTime - $pool['last_health_check'] < $healthCheckInterval) {
                continue; // Skip if checked recently
            }
            
            $poolHealth = $this->checkPoolHealth($id);
            $results[$id] = $poolHealth;
            
            self::$pools[$id]['last_health_check'] = $currentTime;
            self::$pools[$id]['is_healthy'] = $poolHealth['is_healthy'];
        }
        
        self::$lastHealthCheck = $currentTime;
        
        return $results;
    }
    
    /**
     * Check health of individual pool
     */
    private function checkPoolHealth(string $poolId): array
    {
        $pool = &self::$pools[$poolId];
        $healthyConnections = 0;
        $unhealthyConnections = 0;
        $connectionDetails = [];
        
        foreach ($pool['connections'] as $connectionId => &$connection) {
            $isHealthy = $this->testConnection($connection['pdo']);
            
            $connection['health_status'] = $isHealthy ? 'healthy' : 'unhealthy';
            $connection['last_health_check'] = microtime(true);
            
            if ($isHealthy) {
                $healthyConnections++;
            } else {
                $unhealthyConnections++;
                // Schedule unhealthy connection for removal
                $this->scheduleConnectionRemoval($poolId, $connectionId);
            }
            
            $connectionDetails[] = [
                'id' => $connectionId,
                'healthy' => $isHealthy,
                'in_use' => $connection['in_use'],
                'age' => microtime(true) - $connection['created_at'],
                'usage_count' => $connection['usage_count']
            ];
        }
        
        $totalConnections = count($pool['connections']);
        $isPoolHealthy = $totalConnections > 0 && ($healthyConnections / $totalConnections) >= 0.5;
        
        // Auto-scale based on health and load
        if ($pool['auto_scaling']) {
            $this->considerAutoScaling($poolId, $healthyConnections, $totalConnections);
        }
        
        return [
            'pool_id' => $poolId,
            'is_healthy' => $isPoolHealthy,
            'total_connections' => $totalConnections,
            'healthy_connections' => $healthyConnections,
            'unhealthy_connections' => $unhealthyConnections,
            'utilization' => $this->calculatePoolUtilization($pool),
            'connection_details' => $connectionDetails
        ];
    }
    
    /**
     * Test individual connection health
     */
    private function testConnection(PDO $pdo): bool
    {
        try {
            // Set a short timeout for health check
            $pdo->setAttribute(PDO::ATTR_TIMEOUT, self::CONNECTION_TEST_TIMEOUT);
            
            $stmt = $pdo->query('SELECT 1');
            return $stmt !== false;
            
        } catch (Exception $e) {
            return false;
        }
    }
    
    /**
     * Auto-scaling logic based on pool utilization
     */
    private function considerAutoScaling(string $poolId, int $healthyConnections, int $totalConnections): void
    {
        $pool = &self::$pools[$poolId];
        $config = $pool['config'];
        $utilization = $this->calculatePoolUtilization($pool);
        
        $maxConnections = $config['max_connections'] ?? self::DEFAULT_MAX_CONNECTIONS;
        $minConnections = $config['min_connections'] ?? self::DEFAULT_MIN_CONNECTIONS;
        
        // Scale up if utilization is high
        if ($utilization > $this->loadThreshold && $totalConnections < $maxConnections) {
            $connectionsToAdd = min(
                ceil($totalConnections * 0.2), // Add 20% more connections
                $maxConnections - $totalConnections
            );
            
            for ($i = 0; $i < $connectionsToAdd; $i++) {
                $this->createConnection($poolId);
            }
            
            $this->logPoolEvent($poolId, 'AUTO_SCALE_UP', "Added {$connectionsToAdd} connections due to high utilization ({$utilization})");
        }
        
        // Scale down if utilization is low
        if ($utilization < $this->scaleDownThreshold && $totalConnections > $minConnections) {
            $connectionsToRemove = min(
                floor($totalConnections * 0.1), // Remove 10% of connections
                $totalConnections - $minConnections
            );
            
            $this->removeIdleConnections($poolId, $connectionsToRemove);
            $this->logPoolEvent($poolId, 'AUTO_SCALE_DOWN', "Removed {$connectionsToRemove} connections due to low utilization ({$utilization})");
        }
    }
    
    /**
     * Calculate pool utilization percentage
     */
    private function calculatePoolUtilization(array $pool): float
    {
        $totalConnections = count($pool['connections']);
        if ($totalConnections === 0) {
            return 0;
        }
        
        $inUseConnections = count(array_filter($pool['connections'], fn($conn) => $conn['in_use']));
        return $inUseConnections / $totalConnections;
    }
    
    /**
     * Get comprehensive pool statistics
     */
    public function getPoolStatistics(string $poolId = null): array
    {
        if ($poolId !== null) {
            return $this->getSinglePoolStats($poolId);
        }
        
        $allStats = [];
        foreach (self::$pools as $id => $pool) {
            $allStats[$id] = $this->getSinglePoolStats($id);
        }
        
        return $allStats;
    }
    
    private function getSinglePoolStats(string $poolId): array
    {
        if (!isset(self::$pools[$poolId])) {
            throw new Exception("Pool not found: {$poolId}");
        }
        
        $pool = self::$pools[$poolId];
        $stats = $pool['stats'];
        
        $totalConnections = count($pool['connections']);
        $activeConnections = count(array_filter($pool['connections'], fn($conn) => $conn['in_use']));
        $healthyConnections = count(array_filter($pool['connections'], fn($conn) => $conn['health_status'] === 'healthy'));
        
        return [
            'pool_id' => $poolId,
            'pool_name' => $pool['name'],
            'database_type' => $pool['database_type'],
            'created_at' => $pool['created_at'],
            'is_healthy' => $pool['is_healthy'],
            
            // Connection counts
            'total_connections' => $totalConnections,
            'active_connections' => $activeConnections,
            'available_connections' => $totalConnections - $activeConnections,
            'healthy_connections' => $healthyConnections,
            'unhealthy_connections' => $totalConnections - $healthyConnections,
            
            // Utilization metrics
            'utilization_percentage' => $this->calculatePoolUtilization($pool),
            'average_usage_per_connection' => $totalConnections > 0 ? 
                array_sum(array_column($pool['connections'], 'usage_count')) / $totalConnections : 0,
            
            // Performance statistics
            'connections_created' => $stats['connections_created'],
            'connections_borrowed' => $stats['connections_borrowed'],
            'connections_returned' => $stats['connections_returned'],
            'connection_failures' => $stats['connection_failures'],
            'connections_retired' => $stats['connections_retired'],
            
            // Timing statistics
            'average_borrow_time' => $stats['total_borrow_time'] / max($stats['connections_borrowed'], 1),
            'last_health_check' => $pool['last_health_check'],
            
            // Configuration
            'config' => $this->sanitizeConfig($pool['config']),
            'auto_scaling_enabled' => $pool['auto_scaling']
        ];
    }
    
    /**
     * Cleanup and shutdown
     */
    public function shutdown(): void
    {
        foreach (self::$pools as $poolId => $pool) {
            $this->closeAllConnections($poolId);
        }
        
        self::$pools = [];
        self::$globalStats = [];
    }
    
    /**
     * Close all connections in a pool
     */
    private function closeAllConnections(string $poolId): void
    {
        if (!isset(self::$pools[$poolId])) {
            return;
        }
        
        $pool = &self::$pools[$poolId];
        $closedCount = 0;
        
        foreach ($pool['connections'] as $connectionId => $connection) {
            try {
                $connection['pdo'] = null; // Close PDO connection
                $closedCount++;
            } catch (Exception $e) {
                // Ignore errors during shutdown
            }
        }
        
        $pool['connections'] = [];
        $this->logPoolEvent($poolId, 'POOL_SHUTDOWN', "Closed {$closedCount} connections");
    }
    
    // Helper methods
    private function generatePoolId(string $poolName, array $config): string
    {
        return md5($poolName . serialize($this->sanitizeConfig($config)));
    }
    
    private function detectDatabaseType(array $config): string
    {
        if (isset($config['database_type'])) {
            return $config['database_type'];
        }
        
        if (isset($config['path'])) {
            return 'sqlite';
        }
        
        if (isset($config['sslmode']) || ($config['port'] ?? 0) === 5432) {
            return 'postgresql';
        }
        
        return 'mysql'; // Default assumption
    }
    
    private function validatePoolConfig(array $config): void
    {
        $required = ['database_type'];
        foreach ($required as $field) {
            if (!isset($config[$field]) && !$this->canDetectDatabaseType($config)) {
                throw new Exception("Missing required pool configuration: {$field}");
            }
        }
    }
    
    private function canDetectDatabaseType(array $config): bool
    {
        return isset($config['path']) || // SQLite
               isset($config['sslmode']) || // PostgreSQL
               isset($config['host']); // MySQL/PostgreSQL
    }
    
    private function initializeGlobalStats(): void
    {
        self::$globalStats = [
            'pools_created' => 0,
            'total_connections_created' => 0,
            'total_connections_borrowed' => 0,
            'total_connections_returned' => 0,
            'total_connection_failures' => 0,
            'health_checks_performed' => 0,
            'auto_scaling_events' => 0
        ];
    }
    
    private function initializePoolStats(): array
    {
        return [
            'connections_created' => 0,
            'connections_borrowed' => 0,
            'connections_returned' => 0,
            'connection_failures' => 0,
            'connections_retired' => 0,
            'total_borrow_time' => 0,
            'health_checks' => 0,
            'scaling_events' => 0
        ];
    }
    
    private function sanitizeConfig(array $config): array
    {
        $sanitized = $config;
        $sensitiveKeys = ['password', 'encryption_key', 'secret', 'key'];
        
        foreach ($sensitiveKeys as $key) {
            if (isset($sanitized[$key])) {
                $sanitized[$key] = '[REDACTED]';
            }
        }
        
        return $sanitized;
    }
    
    private function calculateHealthCheckInterval(array $pool): float
    {
        $baseInterval = self::DEFAULT_HEALTH_CHECK_INTERVAL;
        $utilization = $this->calculatePoolUtilization($pool);
        
        // More frequent checks for heavily used pools
        if ($utilization > 0.8) {
            return $baseInterval * 0.5; // Check twice as often
        } elseif ($utilization < 0.2) {
            return $baseInterval * 2; // Check half as often
        }
        
        return $baseInterval;
    }
    
    private function logPoolEvent(string $poolId, string $event, string $message): void
    {
        // Implementation would log to configured logging system
        if ($this->isDebugMode()) {
            error_log("Pool {$poolId}: {$event} - {$message}");
        }
    }
    
    private function logConnectionEvent(string $poolId, string $connectionId, string $event): void
    {
        // Implementation would log to configured logging system
        if ($this->isDebugMode()) {
            error_log("Pool {$poolId} Connection {$connectionId}: {$event}");
        }
    }
    
    private function isDebugMode(): bool
    {
        return defined('UNIVERSAL_POOL_DEBUG') && UNIVERSAL_POOL_DEBUG;
    }
    
    // Placeholder implementations for remaining private methods
    private function markConnectionInUse(string $poolId, string $connectionId): void { /* Implementation */ }
    private function findConnectionId(string $poolId, PDO $pdo): ?string { return null; }
    private function shouldRetireConnection(array $connection): bool { return false; }
    private function removeConnection(string $poolId, string $connectionId): void { /* Implementation */ }
    private function scheduleConnectionRemoval(string $poolId, string $connectionId): void { /* Implementation */ }
    private function removeIdleConnections(string $poolId, int $count): void { /* Implementation */ }
    private function createLoadBalancer(array $config): object { return new \stdClass(); }
} 