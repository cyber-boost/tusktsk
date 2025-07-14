<?php
/**
 * <?tusk> TuskPHP Database - Elephant's PostgreSQL Memory Palace
 * =============================================================
 * "An elephant's memory palace is vast and organized - 
 *  just like PostgreSQL's data architecture"
 */

namespace TuskPHP;

use PDO;
use PDOException;

// Global Query Builder Helper Functions
if (!function_exists('Query')) {
    /**
     * Create a new TuskQuery instance
     * Usage: Query('users')->equalTo('status', 'active')->findAll()
     */
    function Query(string $table): TuskQuery 
    {
        return new TuskQuery($table);
    }
}

if (!function_exists('DB')) {
    /**
     * Alias for Query function  
     * Usage: DB('posts')->where('published', '=', true)->findAll()
     */
    function DB(string $table): TuskQuery 
    {
        return Query($table);
    }
}

if (!function_exists('Table')) {
    /**
     * Another alias for Query function
     * Usage: Table('orders')->greaterThan('amount', 100)->count()
     */
    function Table(string $table): TuskQuery 
    {
        return Query($table);
    }
}

if (!function_exists('Tusk')) {
    /**
     * TuskPHP-specific query builder
     * Usage: Tusk('elephants')->like('name', '%dumbo%')->find()
     */
    function Tusk(string $table): TuskQuery 
    {
        return Query($table);
    }
}

class TuskDb {
    
    private static $herd_connection = null;
    private static $wisdom_cache = [];
    private static $migration_history = [];
    
    /**
     * getInstance() - Get the matriarch's database connection
     */
    public static function getInstance() {
        if (self::$herd_connection === null) {
            self::establishConnection();
        }
        
        return self::$herd_connection;
    }
    
    /**
     * establishConnection() - Connect to the memory palace (PostgreSQL or SQLite)
     */
    private static function establishConnection() {
        global $pdo; // Use existing connection from config
        
        if ($pdo !== null) {
            self::$herd_connection = $pdo;
            return true;
        }
        
        // Read database configuration from .shell or .peanuts (peanut-first!)
        $dbConfig = self::readDbConfigFromPeanuts();
        
        // If no config found, fall back to constants
        if (!$dbConfig) {
            $dbConfig = self::getDbConfigFromConstants();
        }
        
        // Create connection based on database type
        try {
            $dsn = self::buildDsn($dbConfig);
            
            $options = [
                PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION,
                PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC,
                PDO::ATTR_EMULATE_PREPARES => false,
            ];
            
            self::$herd_connection = new PDO($dsn, $dbConfig['user'], $dbConfig['password'], $options);
            
            // Set application name (PostgreSQL-specific, ignore errors for SQLite)
            if ($dbConfig['type'] === 'postgresql') {
                try {
                    self::$herd_connection->exec("SET application_name = 'TuskPHP-Matriarch'");
                } catch (PDOException $e) {
                    // Ignore if PostgreSQL doesn't support this
                }
            }
            
            return true;
            
        } catch (PDOException $e) {
            error_log("<?tusk> Failed to establish connection to memory palace: " . $e->getMessage());
            
            // Auto-failover to SQLite if PostgreSQL fails and not already SQLite
            if ($dbConfig['type'] === 'postgresql') {
                error_log("<?tusk> Attempting SQLite failover...");
                return self::establishSQLiteFailover();
            }
            
            return false;
        }
    }
    
    /**
     * readDbConfigFromPeanuts() - Read database config from .shell/.peanuts (70% faster!)
     */
    private static function readDbConfigFromPeanuts(): ?array {
        // Try .shell first (compiled peanuts for 70% performance boost)
        $shellPath = '.shell';
        $peanutsPath = '.peanuts';
        
        // Check project root and circus/peanuts
        $possiblePaths = [
            $shellPath,
            "circus/peanuts/{$shellPath}",
            $peanutsPath,
            "circus/peanuts/{$peanutsPath}"
        ];
        
        foreach ($possiblePaths as $path) {
            if (file_exists($path)) {
                try {
                    $content = file_get_contents($path);
                    return self::parseDbConfigFromContent($content);
                } catch (Exception $e) {
                    error_log("<?tusk> Failed to read config from {$path}: " . $e->getMessage());
                    continue;
                }
            }
        }
        
        return null;
    }
    
    /**
     * parseDbConfigFromContent() - Parse database config from peanuts content
     */
    private static function parseDbConfigFromContent(string $content): ?array {
        try {
            // Look for [database] section
            if (preg_match('/\[database\](.*?)(?=\[|$)/s', $content, $matches)) {
                $dbSection = $matches[1];
                $config = [];
                
                // Parse key-value pairs
                $lines = explode("\n", $dbSection);
                foreach ($lines as $line) {
                    $line = trim($line);
                    if (empty($line) || $line[0] === '#') continue;
                    
                    if (preg_match('/(\w+)\s*=\s*["\']?([^"\']*)["\']?/', $line, $match)) {
                        $config[trim($match[1])] = trim($match[2]);
                    }
                }
                
                // Ensure required fields
                $config['type'] = $config['type'] ?? 'postgresql';
                $config['host'] = $config['host'] ?? 'localhost';
                $config['port'] = $config['port'] ?? ($config['type'] === 'postgresql' ? '5432' : '0');
                $config['name'] = $config['name'] ?? 'tuskphp';
                $config['user'] = $config['user'] ?? '';
                $config['password'] = $config['password'] ?? '';
                $config['path'] = $config['path'] ?? 'database.sqlite';
                
                return $config;
            }
        } catch (Exception $e) {
            error_log("<?tusk> Failed to parse database config: " . $e->getMessage());
        }
        
        return null;
    }
    
    /**
     * getDbConfigFromConstants() - Fallback to PHP constants
     */
    private static function getDbConfigFromConstants(): array {
        return [
            'type' => 'postgresql',
            'host' => defined('DB_HOST') ? DB_HOST : 'localhost',
            'port' => defined('DB_PORT') ? DB_PORT : '5432',
            'name' => defined('DB_NAME') ? DB_NAME : 'tuskphp',
            'user' => defined('DB_USER') ? DB_USER : '',
            'password' => defined('DB_PASS') ? DB_PASS : '',
            'path' => 'database.sqlite'
        ];
    }
    
    /**
     * buildDsn() - Build DSN string based on database type
     */
    private static function buildDsn(array $config): string {
        switch ($config['type']) {
            case 'sqlite':
                return "sqlite:" . $config['path'];
                
            case 'mysql':
                return "mysql:host={$config['host']};port={$config['port']};dbname={$config['name']};charset=utf8mb4";
                
            case 'postgresql':
            default:
                return "pgsql:host={$config['host']};port={$config['port']};dbname={$config['name']}";
        }
    }
    
    /**
     * establishSQLiteFailover() - Emergency SQLite connection
     */
    private static function establishSQLiteFailover(): bool {
        try {
            $dsn = "sqlite:database.sqlite";
            
            self::$herd_connection = new PDO($dsn, '', '', [
                PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION,
                PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC,
                PDO::ATTR_EMULATE_PREPARES => false,
            ]);
            
            error_log("<?tusk> SQLite failover successful - emergency connection established");
            return true;
            
        } catch (PDOException $e) {
            error_log("<?tusk> SQLite failover failed: " . $e->getMessage());
            return false;
        }
    }
    
    /**
     * remember() - Store data in the memory palace (INSERT/UPDATE)
     */
    public static function remember($table, $data, $conditions = null) {
        $db = self::getInstance();
        
        if ($conditions === null) {
            // INSERT - new memory
            return self::storeFreshMemory($table, $data);
        } else {
            // UPDATE - modify existing memory
            return self::updateMemory($table, $data, $conditions);
        }
    }
    
    /**
     * recall() - Retrieve data from memory palace (SELECT)
     */
    public static function recall($table, $conditions = [], $options = []) {
        $db = self::getInstance();
        
        if (!$db) {
            return null;
        }
        
        // Build SELECT clause
        $sql = "SELECT ";
        if (isset($options['select']) && is_array($options['select'])) {
            $sql .= implode(', ', $options['select']);
        } elseif (isset($options['fields'])) {
            $sql .= $options['fields'];
        } else {
            $sql .= '*';
        }
        
        // FROM clause
        $sql .= " FROM {$table}";
        
        // JOIN clauses
        if (isset($options['joins']) && is_array($options['joins'])) {
            foreach ($options['joins'] as $join) {
                $sql .= " {$join}";
            }
        }
        
        // WHERE clause
        $params = [];
        if (!empty($conditions)) {
            $where_parts = [];
            foreach ($conditions as $field => $value) {
                $where_parts[] = "{$field} = :{$field}";
                $params[$field] = $value;
            }
            $sql .= " WHERE " . implode(' AND ', $where_parts);
        }
        
        // ORDER BY clause
        if (isset($options['order'])) {
            $sql .= " ORDER BY " . $options['order'];
        }
        
        // LIMIT clause
        if (isset($options['limit'])) {
            $sql .= " LIMIT " . intval($options['limit']);
        }
        
        try {
            $stmt = $db->prepare($sql);
            $stmt->execute($params);
            
            if (isset($options['single']) && $options['single']) {
                return $stmt->fetch();
            }
            
            return $stmt->fetchAll();
            
        } catch (PDOException $e) {
            error_log("<?tusk> Memory recall failed: " . $e->getMessage());
            return null;
        }
    }
    
    /**
     * forget() - Remove data from memory palace (DELETE)
     */
    public static function forget($table, $conditions) {
        $db = self::getInstance();
        
        if (!$db || empty($conditions)) {
            return false;
        }
        
        $where_parts = [];
        $params = [];
        
        foreach ($conditions as $field => $value) {
            $where_parts[] = "{$field} = :{$field}";
            $params[$field] = $value;
        }
        
        $sql = "DELETE FROM {$table} WHERE " . implode(' AND ', $where_parts);
        
        try {
            $stmt = $db->prepare($sql);
            return $stmt->execute($params);
            
        } catch (PDOException $e) {
            error_log("<?tusk> Memory forgetting failed: " . $e->getMessage());
            return false;
        }
    }
    
    /**
     * storeFreshMemory() - INSERT new data
     */
    private static function storeFreshMemory($table, $data) {
        $db = self::getInstance();
        
        if (!$db || empty($data)) {
            return false;
        }
        
        $fields = array_keys($data);
        $placeholders = array_map(function($field) { return ":{$field}"; }, $fields);
        
        $sql = "INSERT INTO {$table} (" . implode(', ', $fields) . ") VALUES (" . implode(', ', $placeholders) . ")";
        
        try {
            $stmt = $db->prepare($sql);
            return $stmt->execute($data);
            
        } catch (PDOException $e) {
            error_log("<?tusk> Fresh memory storage failed: " . $e->getMessage());
            return false;
        }
    }
    
    /**
     * updateMemory() - UPDATE existing data
     */
    private static function updateMemory($table, $data, $conditions) {
        $db = self::getInstance();
        
        if (!$db || empty($data) || empty($conditions)) {
            return false;
        }
        
        $set_parts = [];
        foreach ($data as $field => $value) {
            $set_parts[] = "{$field} = :set_{$field}";
        }
        
        $where_parts = [];
        foreach ($conditions as $field => $value) {
            $where_parts[] = "{$field} = :where_{$field}";
        }
        
        $sql = "UPDATE {$table} SET " . implode(', ', $set_parts) . " WHERE " . implode(' AND ', $where_parts);
        
        $params = [];
        foreach ($data as $field => $value) {
            $params["set_{$field}"] = $value;
        }
        foreach ($conditions as $field => $value) {
            $params["where_{$field}"] = $value;
        }
        
        try {
            $stmt = $db->prepare($sql);
            return $stmt->execute($params);
            
        } catch (PDOException $e) {
            error_log("<?tusk> Memory update failed: " . $e->getMessage());
            return false;
        }
    }
    
    /**
     * wisdom() - Get database statistics and insights (PostgreSQL & SQLite compatible)
     */
    public static function wisdom() {
        $db = self::getInstance();
        
        if (!$db) {
            return ['status' => 'disconnected'];
        }
        
        try {
            // Detect database type
            $dbType = self::getDbType();
            
            $wisdom = [
                'status' => 'connected',
                'database_type' => $dbType,
                'connection_alive' => true,
                'application_name' => 'TuskPHP-Matriarch',
            ];
            
            if ($dbType === 'postgresql') {
                // PostgreSQL-specific wisdom
                $stmt = $db->query("SELECT pg_size_pretty(pg_database_size(current_database())) as size");
                $size_info = $stmt->fetch();
                
                $stmt = $db->query("SELECT COUNT(*) as table_count FROM information_schema.tables WHERE table_schema = 'public'");
                $table_info = $stmt->fetch();
                
                $stmt = $db->query("SELECT version() as version");
                $version_info = $stmt->fetch();
                
                $wisdom['database_size'] = $size_info['size'] ?? 'unknown';
                $wisdom['table_count'] = $table_info['table_count'] ?? 0;
                $wisdom['postgresql_version'] = $version_info['version'] ?? 'unknown';
                
            } elseif ($dbType === 'sqlite') {
                // SQLite-specific wisdom
                $stmt = $db->query("SELECT COUNT(*) as table_count FROM sqlite_master WHERE type = 'table'");
                $table_info = $stmt->fetch();
                
                $stmt = $db->query("SELECT sqlite_version() as version");
                $version_info = $stmt->fetch();
                
                // Get database file size
                $dbConfig = self::readDbConfigFromPeanuts() ?? self::getDbConfigFromConstants();
                $dbPath = $dbConfig['path'] ?? 'database.sqlite';
                $fileSize = file_exists($dbPath) ? filesize($dbPath) : 0;
                
                $wisdom['database_size'] = self::formatBytes($fileSize);
                $wisdom['table_count'] = $table_info['table_count'] ?? 0;
                $wisdom['sqlite_version'] = $version_info['version'] ?? 'unknown';
                $wisdom['database_file'] = $dbPath;
                
            } else {
                // Generic database wisdom
                $wisdom['database_size'] = 'unknown';
                $wisdom['table_count'] = 0;
                $wisdom['version'] = 'unknown';
            }
            
            return $wisdom;
            
        } catch (PDOException $e) {
            error_log("<?tusk> Wisdom gathering failed: " . $e->getMessage());
            return ['status' => 'error', 'message' => $e->getMessage()];
        }
    }
    
    /**
     * getDbType() - Detect database type from PDO driver
     */
    private static function getDbType(): string {
        $db = self::getInstance();
        if (!$db) return 'unknown';
        
        try {
            $driver = $db->getAttribute(PDO::ATTR_DRIVER_NAME);
            return match($driver) {
                'pgsql' => 'postgresql',
                'sqlite' => 'sqlite',
                'mysql' => 'mysql',
                default => $driver
            };
        } catch (Exception $e) {
            return 'unknown';
        }
    }
    
    /**
     * formatBytes() - Format bytes into human readable format
     */
    private static function formatBytes(int $bytes): string {
        if ($bytes === 0) return '0 B';
        
        $units = ['B', 'KB', 'MB', 'GB', 'TB'];
        $base = 1024;
        $exp = floor(log($bytes) / log($base));
        
        return round($bytes / pow($base, $exp), 2) . ' ' . $units[$exp];
    }
    
    /**
     * health() - Check database health
     */
    public static function health() {
        $db = self::getInstance();
        
        if (!$db) {
            return [
                'status' => 'unhealthy',
                'connection' => false,
                'message' => 'No database connection'
            ];
        }
        
        try {
            // Simple health check query
            $stmt = $db->query("SELECT 1 as healthy");
            $result = $stmt->fetch();
            
            return [
                'status' => 'healthy',
                'connection' => true,
                'response_time' => 'fast',
                'last_check' => date('Y-m-d H:i:s'),
            ];
            
        } catch (PDOException $e) {
            return [
                'status' => 'unhealthy',
                'connection' => false,
                'error' => $e->getMessage(),
            ];
        }
    }
    
    /**
     * migrate() - Run database migrations (like elephant seasonal moves)
     */
    public static function migrate($migration_file) {
        $db = self::getInstance();
        
        if (!$db) {
            return false;
        }
        
        if (!file_exists($migration_file)) {
            error_log("<?tusk> Migration path not found: {$migration_file}");
            return false;
        }
        
        try {
            $sql = file_get_contents($migration_file);
            $db->exec($sql);
            
            self::$migration_history[] = [
                'file' => $migration_file,
                'executed_at' => date('Y-m-d H:i:s'),
                'status' => 'success',
            ];
            
            return true;
            
        } catch (PDOException $e) {
            error_log("<?tusk> Migration failed: " . $e->getMessage());
            
            self::$migration_history[] = [
                'file' => $migration_file,
                'executed_at' => date('Y-m-d H:i:s'),
                'status' => 'failed',
                'error' => $e->getMessage(),
            ];
            
            return false;
        }
    }
    
    /**
     * stampede() - Bulk operations (when the whole herd moves)
     */
    public static function stampede($table, $operation, $data_sets) {
        $db = self::getInstance();
        
        if (!$db) {
            return false;
        }
        
        try {
            $db->beginTransaction();
            
            $success_count = 0;
            foreach ($data_sets as $data) {
                if ($operation === 'remember') {
                    if (self::remember($table, $data)) {
                        $success_count++;
                    }
                }
            }
            
            $db->commit();
            
            return [
                'total' => count($data_sets),
                'successful' => $success_count,
                'failed' => count($data_sets) - $success_count,
            ];
            
        } catch (PDOException $e) {
            $db->rollBack();
            error_log("<?tusk> Stampede failed: " . $e->getMessage());
            return false;
        }
    }
    
    /**
     * query() - Direct SQL queries (for advanced elephant wisdom)
     */
    public static function query($sql, $params = []) {
        $db = self::getInstance();
        
        if (!$db) {
            return null;
        }
        
        try {
            $stmt = $db->prepare($sql);
            $stmt->execute($params);
            
            // Return appropriate result based on query type
            if (stripos(trim($sql), 'SELECT') === 0) {
                return $stmt->fetchAll();
            } else {
                return $stmt->rowCount();
            }
            
        } catch (PDOException $e) {
            error_log("<?tusk> Custom query failed: " . $e->getMessage());
            return null;
        }
    }
    
    /**
     * getDbConfig() - Get current database configuration (for debugging)
     */
    public static function getDbConfig(): array {
        $config = self::readDbConfigFromPeanuts();
        if (!$config) {
            $config = self::getDbConfigFromConstants();
        }
        
        // Don't expose sensitive data
        $safeConfig = $config;
        if (isset($safeConfig['password'])) {
            $safeConfig['password'] = str_repeat('*', strlen($safeConfig['password']));
        }
        
        return $safeConfig;
    }
    
    /**
     * testConnection() - Test database connection without affecting current connection
     */
    public static function testConnection(?array $testConfig = null): array {
        $config = $testConfig ?? self::readDbConfigFromPeanuts() ?? self::getDbConfigFromConstants();
        
        try {
            $dsn = self::buildDsn($config);
            $testPdo = new PDO($dsn, $config['user'], $config['password'], [
                PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION,
                PDO::ATTR_TIMEOUT => 5 // 5 second timeout for testing
            ]);
            
            $testPdo->query('SELECT 1');
            
            return [
                'success' => true,
                'type' => $config['type'],
                'host' => $config['host'] ?? 'N/A',
                'port' => $config['port'] ?? 'N/A',
                'database' => $config['name'] ?? $config['path'] ?? 'N/A'
            ];
            
        } catch (PDOException $e) {
            return [
                'success' => false,
                'error' => $e->getMessage(),
                'type' => $config['type'],
                'attempted_dsn' => $dsn ?? 'failed to build'
            ];
        }
    }
} 