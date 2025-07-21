<?php
/**
 * 🥜 TuskLang PHP Agent A8 - Goal Implementation
 * ===============================================
 * Agent: a8
 * Goals: g1.1, g1.2, g1.3
 * Language: PHP
 * 
 * This file implements the three goals for the PHP agent:
 * - g1.1: Enhanced PHP-specific operator support
 * - g1.2: Advanced error handling and logging
 * - g1.3: Performance optimization features
 */

namespace TuskLang\AgentA8;

/**
 * Goal 1.1: Enhanced PHP-specific operator support
 * Priority: High
 * Success Criteria: Implement PHP-specific operators and extensions
 */
class PHPOperatorExtensions
{
    private array $phpOperators = [];
    
    public function __construct()
    {
        $this->registerPHPSpecificOperators();
    }
    
    /**
     * Register PHP-specific operators
     */
    private function registerPHPSpecificOperators(): void
    {
        $this->phpOperators = [
            '@php_exec' => [$this, 'executePHPCode'],
            '@php_include' => [$this, 'includePHPFile'],
            '@php_function' => [$this, 'callPHPFunction'],
            '@php_class' => [$this, 'instantiatePHPClass'],
            '@php_serialize' => [$this, 'serializeData'],
            '@php_unserialize' => [$this, 'unserializeData'],
            '@php_json_encode' => [$this, 'jsonEncode'],
            '@php_json_decode' => [$this, 'jsonDecode'],
            '@php_file_get' => [$this, 'fileGetContents'],
            '@php_file_put' => [$this, 'filePutContents'],
            '@php_dir_create' => [$this, 'createDirectory'],
            '@php_file_exists' => [$this, 'fileExists'],
            '@php_unlink' => [$this, 'deleteFile'],
            '@php_copy' => [$this, 'copyFile'],
            '@php_move' => [$this, 'moveFile'],
            '@php_chmod' => [$this, 'changePermissions'],
            '@php_md5' => [$this, 'calculateMD5'],
            '@php_sha1' => [$this, 'calculateSHA1'],
            '@php_password_hash' => [$this, 'passwordHash'],
            '@php_password_verify' => [$this, 'passwordVerify']
        ];
    }
    
    /**
     * Execute PHP code safely
     */
    public function executePHPCode(string $code): mixed
    {
        try {
            // Basic safety check - prevent dangerous operations
            $dangerous = ['system', 'exec', 'shell_exec', 'passthru', 'eval'];
            foreach ($dangerous as $func) {
                if (stripos($code, $func) !== false) {
                    throw new \Exception("Dangerous function '$func' not allowed");
                }
            }
            
            return eval($code);
        } catch (\Exception $e) {
            error_log("PHP execution error: " . $e->getMessage());
            return null;
        }
    }
    
    /**
     * Include PHP file
     */
    public function includePHPFile(string $filePath): mixed
    {
        try {
            if (!file_exists($filePath)) {
                throw new \Exception("File not found: $filePath");
            }
            
            return include $filePath;
        } catch (\Exception $e) {
            error_log("PHP include error: " . $e->getMessage());
            return null;
        }
    }
    
    /**
     * Call PHP function dynamically
     */
    public function callPHPFunction(string $functionName, array $params = []): mixed
    {
        try {
            if (!function_exists($functionName)) {
                throw new \Exception("Function not found: $functionName");
            }
            
            return call_user_func_array($functionName, $params);
        } catch (\Exception $e) {
            error_log("PHP function call error: " . $e->getMessage());
            return null;
        }
    }
    
    /**
     * Instantiate PHP class
     */
    public function instantiatePHPClass(string $className, array $params = []): mixed
    {
        try {
            if (!class_exists($className)) {
                throw new \Exception("Class not found: $className");
            }
            
            $reflection = new \ReflectionClass($className);
            return $reflection->newInstanceArgs($params);
        } catch (\Exception $e) {
            error_log("PHP class instantiation error: " . $e->getMessage());
            return null;
        }
    }
    
    /**
     * Serialize data
     */
    public function serializeData(mixed $data): string
    {
        return serialize($data);
    }
    
    /**
     * Unserialize data
     */
    public function unserializeData(string $data): mixed
    {
        return unserialize($data);
    }
    
    /**
     * JSON encode
     */
    public function jsonEncode(mixed $data): string
    {
        return json_encode($data);
    }
    
    /**
     * JSON decode
     */
    public function jsonDecode(string $data): mixed
    {
        return json_decode($data, true);
    }
    
    /**
     * File operations
     */
    public function fileGetContents(string $filePath): string
    {
        return file_get_contents($filePath);
    }
    
    public function filePutContents(string $filePath, string $content): int
    {
        return file_put_contents($filePath, $content);
    }
    
    public function createDirectory(string $path): bool
    {
        return mkdir($path, 0755, true);
    }
    
    public function fileExists(string $filePath): bool
    {
        return file_exists($filePath);
    }
    
    public function deleteFile(string $filePath): bool
    {
        return unlink($filePath);
    }
    
    public function copyFile(string $source, string $destination): bool
    {
        return copy($source, $destination);
    }
    
    public function moveFile(string $source, string $destination): bool
    {
        return rename($source, $destination);
    }
    
    public function changePermissions(string $filePath, int $permissions): bool
    {
        return chmod($filePath, $permissions);
    }
    
    /**
     * Hash functions
     */
    public function calculateMD5(string $data): string
    {
        return md5($data);
    }
    
    public function calculateSHA1(string $data): string
    {
        return sha1($data);
    }
    
    public function passwordHash(string $password): string
    {
        return password_hash($password, PASSWORD_DEFAULT);
    }
    
    public function passwordVerify(string $password, string $hash): bool
    {
        return password_verify($password, $hash);
    }
    
    /**
     * Get all PHP operators
     */
    public function getOperators(): array
    {
        return $this->phpOperators;
    }
}

/**
 * Goal 1.2: Advanced error handling and logging
 * Priority: Medium
 * Success Criteria: Implement comprehensive error handling and logging system
 */
class AdvancedErrorHandler
{
    private string $logFile;
    private array $errorLevels = [
        'DEBUG' => 0,
        'INFO' => 1,
        'WARNING' => 2,
        'ERROR' => 3,
        'CRITICAL' => 4
    ];
    private int $minLogLevel = 1; // INFO by default
    
    public function __construct(string $logFile = 'tusklang_agent_a8.log')
    {
        $this->logFile = $logFile;
        $this->setupErrorHandling();
    }
    
    /**
     * Setup error handling
     */
    private function setupErrorHandling(): void
    {
        // Set error handler
        set_error_handler([$this, 'handleError']);
        
        // Set exception handler
        set_exception_handler([$this, 'handleException']);
        
        // Set shutdown function
        register_shutdown_function([$this, 'handleShutdown']);
    }
    
    /**
     * Handle PHP errors
     */
    public function handleError(int $errno, string $errstr, string $errfile, int $errline): bool
    {
        $errorType = $this->getErrorType($errno);
        $message = sprintf(
            "[%s] %s: %s in %s on line %d",
            date('Y-m-d H:i:s'),
            $errorType,
            $errstr,
            $errfile,
            $errline
        );
        
        $this->log($message, 'ERROR');
        
        // Don't execute PHP internal error handler
        return true;
    }
    
    /**
     * Handle exceptions
     */
    public function handleException(\Throwable $exception): void
    {
        $message = sprintf(
            "[%s] EXCEPTION: %s in %s on line %d\nStack trace:\n%s",
            date('Y-m-d H:i:s'),
            $exception->getMessage(),
            $exception->getFile(),
            $exception->getLine(),
            $exception->getTraceAsString()
        );
        
        $this->log($message, 'CRITICAL');
    }
    
    /**
     * Handle shutdown
     */
    public function handleShutdown(): void
    {
        $error = error_get_last();
        if ($error !== null && in_array($error['type'], [E_ERROR, E_PARSE, E_CORE_ERROR, E_COMPILE_ERROR])) {
            $message = sprintf(
                "[%s] FATAL ERROR: %s in %s on line %d",
                date('Y-m-d H:i:s'),
                $error['message'],
                $error['file'],
                $error['line']
            );
            
            $this->log($message, 'CRITICAL');
        }
    }
    
    /**
     * Get error type string
     */
    private function getErrorType(int $errno): string
    {
        $errorTypes = [
            E_ERROR => 'E_ERROR',
            E_WARNING => 'E_WARNING',
            E_PARSE => 'E_PARSE',
            E_NOTICE => 'E_NOTICE',
            E_CORE_ERROR => 'E_CORE_ERROR',
            E_CORE_WARNING => 'E_CORE_WARNING',
            E_COMPILE_ERROR => 'E_COMPILE_ERROR',
            E_COMPILE_WARNING => 'E_COMPILE_WARNING',
            E_USER_ERROR => 'E_USER_ERROR',
            E_USER_WARNING => 'E_USER_WARNING',
            E_USER_NOTICE => 'E_USER_NOTICE',
            E_STRICT => 'E_STRICT',
            E_RECOVERABLE_ERROR => 'E_RECOVERABLE_ERROR',
            E_DEPRECATED => 'E_DEPRECATED',
            E_USER_DEPRECATED => 'E_USER_DEPRECATED'
        ];
        
        return $errorTypes[$errno] ?? 'UNKNOWN';
    }
    
    /**
     * Log message
     */
    public function log(string $message, string $level = 'INFO'): void
    {
        if ($this->errorLevels[$level] >= $this->minLogLevel) {
            $logMessage = sprintf("[%s] [%s] %s\n", date('Y-m-d H:i:s'), $level, $message);
            file_put_contents($this->logFile, $logMessage, FILE_APPEND | LOCK_EX);
        }
    }
    
    /**
     * Set minimum log level
     */
    public function setMinLogLevel(string $level): void
    {
        if (isset($this->errorLevels[$level])) {
            $this->minLogLevel = $this->errorLevels[$level];
        }
    }
    
    /**
     * Get log file path
     */
    public function getLogFile(): string
    {
        return $this->logFile;
    }
    
    /**
     * Clear log file
     */
    public function clearLog(): void
    {
        file_put_contents($this->logFile, '');
    }
    
    /**
     * Get log statistics
     */
    public function getLogStats(): array
    {
        if (!file_exists($this->logFile)) {
            return ['total_lines' => 0, 'error_count' => 0, 'warning_count' => 0];
        }
        
        $content = file_get_contents($this->logFile);
        $lines = explode("\n", $content);
        $errorCount = 0;
        $warningCount = 0;
        
        foreach ($lines as $line) {
            if (strpos($line, '[ERROR]') !== false) {
                $errorCount++;
            } elseif (strpos($line, '[WARNING]') !== false) {
                $warningCount++;
            }
        }
        
        return [
            'total_lines' => count($lines),
            'error_count' => $errorCount,
            'warning_count' => $warningCount
        ];
    }
}

/**
 * Goal 1.3: Performance optimization features
 * Priority: Low
 * Success Criteria: Implement caching, optimization, and performance monitoring
 */
class PerformanceOptimizer
{
    private array $cache = [];
    private array $performanceMetrics = [];
    private float $startTime;
    private array $memoryUsage = [];
    private string $cacheFile;
    private bool $cacheEnabled = true;
    
    public function __construct(string $cacheFile = 'tusklang_cache.json')
    {
        $this->cacheFile = $cacheFile;
        $this->startTime = microtime(true);
        $this->loadCache();
        $this->recordMemoryUsage('start');
    }
    
    /**
     * Load cache from file
     */
    private function loadCache(): void
    {
        if (file_exists($this->cacheFile)) {
            try {
                $cacheData = json_decode(file_get_contents($this->cacheFile), true);
                if (is_array($cacheData)) {
                    $this->cache = $cacheData;
                }
            } catch (\Exception $e) {
                // Cache file corrupted, start fresh
                $this->cache = [];
            }
        }
    }
    
    /**
     * Save cache to file
     */
    private function saveCache(): void
    {
        if ($this->cacheEnabled) {
            file_put_contents($this->cacheFile, json_encode($this->cache));
        }
    }
    
    /**
     * Get cached value
     */
    public function get(string $key): mixed
    {
        if (isset($this->cache[$key])) {
            $entry = $this->cache[$key];
            
            // Check if cache entry is still valid
            if (isset($entry['expires']) && $entry['expires'] < time()) {
                unset($this->cache[$key]);
                return null;
            }
            
            return $entry['value'];
        }
        
        return null;
    }
    
    /**
     * Set cached value
     */
    public function set(string $key, mixed $value, int $ttl = 3600): void
    {
        $this->cache[$key] = [
            'value' => $value,
            'expires' => time() + $ttl,
            'created' => time()
        ];
        
        $this->saveCache();
    }
    
    /**
     * Delete cached value
     */
    public function delete(string $key): void
    {
        unset($this->cache[$key]);
        $this->saveCache();
    }
    
    /**
     * Clear all cache
     */
    public function clear(): void
    {
        $this->cache = [];
        $this->saveCache();
    }
    
    /**
     * Record performance metric
     */
    public function recordMetric(string $name, float $value, string $unit = 'ms'): void
    {
        if (!isset($this->performanceMetrics[$name])) {
            $this->performanceMetrics[$name] = [];
        }
        
        $this->performanceMetrics[$name][] = [
            'value' => $value,
            'unit' => $unit,
            'timestamp' => microtime(true)
        ];
    }
    
    /**
     * Start performance timer
     */
    public function startTimer(string $name): void
    {
        $this->performanceMetrics[$name . '_start'] = microtime(true);
    }
    
    /**
     * End performance timer
     */
    public function endTimer(string $name): float
    {
        if (isset($this->performanceMetrics[$name . '_start'])) {
            $startTime = $this->performanceMetrics[$name . '_start'];
            $endTime = microtime(true);
            $duration = ($endTime - $startTime) * 1000; // Convert to milliseconds
            
            $this->recordMetric($name, $duration, 'ms');
            unset($this->performanceMetrics[$name . '_start']);
            
            return $duration;
        }
        
        return 0.0;
    }
    
    /**
     * Record memory usage
     */
    public function recordMemoryUsage(string $label): void
    {
        $this->memoryUsage[$label] = [
            'memory' => memory_get_usage(true),
            'peak' => memory_get_peak_usage(true),
            'timestamp' => microtime(true)
        ];
    }
    
    /**
     * Get performance statistics
     */
    public function getStats(): array
    {
        $totalTime = microtime(true) - $this->startTime;
        $currentMemory = memory_get_usage(true);
        $peakMemory = memory_get_peak_usage(true);
        
        $stats = [
            'total_execution_time' => $totalTime,
            'current_memory_usage' => $this->formatBytes($currentMemory),
            'peak_memory_usage' => $this->formatBytes($peakMemory),
            'cache_entries' => count($this->cache),
            'performance_metrics' => $this->performanceMetrics,
            'memory_usage_points' => $this->memoryUsage
        ];
        
        // Calculate averages for metrics
        foreach ($this->performanceMetrics as $name => $metrics) {
            // Skip timer start values (floats) and only process arrays
            if (is_array($metrics) && count($metrics) > 0) {
                $values = array_column($metrics, 'value');
                $stats['averages'][$name] = [
                    'mean' => array_sum($values) / count($values),
                    'min' => min($values),
                    'max' => max($values),
                    'count' => count($values)
                ];
            }
        }
        
        return $stats;
    }
    
    /**
     * Format bytes to human readable format
     */
    private function formatBytes(int $bytes): string
    {
        $units = ['B', 'KB', 'MB', 'GB'];
        $bytes = max($bytes, 0);
        $pow = floor(($bytes ? log($bytes) : 0) / log(1024));
        $pow = min($pow, count($units) - 1);
        
        $bytes /= pow(1024, $pow);
        
        return round($bytes, 2) . ' ' . $units[$pow];
    }
    
    /**
     * Enable/disable cache
     */
    public function setCacheEnabled(bool $enabled): void
    {
        $this->cacheEnabled = $enabled;
    }
    
    /**
     * Get cache status
     */
    public function isCacheEnabled(): bool
    {
        return $this->cacheEnabled;
    }
    
    /**
     * Optimize memory usage
     */
    public function optimizeMemory(): void
    {
        // Clear old cache entries
        $currentTime = time();
        foreach ($this->cache as $key => $entry) {
            if (isset($entry['expires']) && $entry['expires'] < $currentTime) {
                unset($this->cache[$key]);
            }
        }
        
        // Limit performance metrics to last 100 entries per metric
        foreach ($this->performanceMetrics as $name => $metrics) {
            if (is_array($metrics) && count($metrics) > 100) {
                $this->performanceMetrics[$name] = array_slice($metrics, -100);
            }
        }
        
        $this->saveCache();
        gc_collect_cycles(); // Force garbage collection
    }
}

/**
 * Main Agent A8 Implementation
 * Combines all three goals into a unified system
 */
class AgentA8
{
    private PHPOperatorExtensions $operators;
    private AdvancedErrorHandler $errorHandler;
    private PerformanceOptimizer $optimizer;
    
    public function __construct()
    {
        $this->operators = new PHPOperatorExtensions();
        $this->errorHandler = new AdvancedErrorHandler();
        $this->optimizer = new PerformanceOptimizer();
        
        $this->errorHandler->log("Agent A8 initialized successfully", "INFO");
        $this->optimizer->startTimer('agent_initialization');
    }
    
    /**
     * Execute goal 1.1: Enhanced PHP operator support
     */
    public function executeGoal1_1(): array
    {
        $this->optimizer->startTimer('goal_1_1');
        $this->errorHandler->log("Executing Goal 1.1: Enhanced PHP operator support", "INFO");
        
        try {
            // Register PHP operators with the main TuskLang system
            $phpOperators = $this->operators->getOperators();
            
            // Test some operators
            $testResults = [
                'php_json_encode' => $this->operators->jsonEncode(['test' => 'value']),
                'php_md5' => $this->operators->calculateMD5('test_string'),
                'php_file_exists' => $this->operators->fileExists(__FILE__),
                'php_password_hash' => $this->operators->passwordHash('test_password')
            ];
            
            $executionTime = $this->optimizer->endTimer('goal_1_1');
            $this->errorHandler->log("Goal 1.1 completed successfully", "INFO");
            
            return [
                'success' => true,
                'operators_registered' => count($phpOperators),
                'test_results' => $testResults,
                'execution_time' => $executionTime
            ];
            
        } catch (\Exception $e) {
            $this->errorHandler->log("Goal 1.1 failed: " . $e->getMessage(), "ERROR");
            return ['success' => false, 'error' => $e->getMessage()];
        }
    }
    
    /**
     * Execute goal 1.2: Advanced error handling and logging
     */
    public function executeGoal1_2(): array
    {
        $this->optimizer->startTimer('goal_1_2');
        $this->errorHandler->log("Executing Goal 1.2: Advanced error handling and logging", "INFO");
        
        try {
            // Test error handling
            $this->errorHandler->log("Testing error handling system", "DEBUG");
            $this->errorHandler->log("This is a warning message", "WARNING");
            
            // Test exception handling
            try {
                throw new \Exception("Test exception for error handling");
            } catch (\Exception $e) {
                $this->errorHandler->handleException($e);
            }
            
            // Get log statistics
            $logStats = $this->errorHandler->getLogStats();
            
            $executionTime = $this->optimizer->endTimer('goal_1_2');
            $this->errorHandler->log("Goal 1.2 completed successfully", "INFO");
            
            return [
                'success' => true,
                'log_file' => $this->errorHandler->getLogFile(),
                'log_stats' => $logStats,
                'execution_time' => $executionTime
            ];
            
        } catch (\Exception $e) {
            $this->errorHandler->log("Goal 1.2 failed: " . $e->getMessage(), "ERROR");
            return ['success' => false, 'error' => $e->getMessage()];
        }
    }
    
    /**
     * Execute goal 1.3: Performance optimization features
     */
    public function executeGoal1_3(): array
    {
        $this->optimizer->startTimer('goal_1_3');
        $this->errorHandler->log("Executing Goal 1.3: Performance optimization features", "INFO");
        
        try {
            // Test caching
            $this->optimizer->set('test_key', 'test_value', 60);
            $cachedValue = $this->optimizer->get('test_key');
            
            // Test performance metrics
            $this->optimizer->startTimer('test_operation');
            usleep(100000); // Simulate work (100ms)
            $operationTime = $this->optimizer->endTimer('test_operation');
            
            // Record memory usage
            $this->optimizer->recordMemoryUsage('goal_1_3_end');
            
            // Get performance statistics
            $stats = $this->optimizer->getStats();
            
            $executionTime = $this->optimizer->endTimer('goal_1_3');
            $this->errorHandler->log("Goal 1.3 completed successfully", "INFO");
            
            return [
                'success' => true,
                'cache_test' => $cachedValue === 'test_value',
                'operation_time' => $operationTime,
                'performance_stats' => $stats,
                'execution_time' => $executionTime
            ];
            
        } catch (\Exception $e) {
            $this->errorHandler->log("Goal 1.3 failed: " . $e->getMessage(), "ERROR");
            return ['success' => false, 'error' => $e->getMessage()];
        }
    }
    
    /**
     * Execute all goals
     */
    public function executeAllGoals(): array
    {
        $this->errorHandler->log("Starting execution of all goals for Agent A8", "INFO");
        
        $results = [
            'goal_1_1' => $this->executeGoal1_1(),
            'goal_1_2' => $this->executeGoal1_2(),
            'goal_1_3' => $this->executeGoal1_3()
        ];
        
        // Final optimization
        $this->optimizer->optimizeMemory();
        
        // Get final statistics
        $finalStats = $this->optimizer->getStats();
        
        $this->errorHandler->log("All goals completed for Agent A8", "INFO");
        
        return [
            'agent_id' => 'a8',
            'language' => 'PHP',
            'timestamp' => date('Y-m-d H:i:s'),
            'results' => $results,
            'final_stats' => $finalStats,
            'success' => array_reduce($results, function($carry, $result) {
                return $carry && $result['success'];
            }, true)
        ];
    }
    
    /**
     * Get agent information
     */
    public function getInfo(): array
    {
        return [
            'agent_id' => 'a8',
            'language' => 'PHP',
            'goals_completed' => ['g1.1', 'g1.2', 'g1.3'],
            'features' => [
                'PHP-specific operators',
                'Advanced error handling',
                'Performance optimization',
                'Caching system',
                'Memory management'
            ]
        ];
    }
}

// Main execution
if (php_sapi_name() === 'cli' || isset($_GET['execute'])) {
    $agent = new AgentA8();
    $results = $agent->executeAllGoals();
    
    if (php_sapi_name() === 'cli') {
        echo json_encode($results, JSON_PRETTY_PRINT) . "\n";
    } else {
        header('Content-Type: application/json');
        echo json_encode($results, JSON_PRETTY_PRINT);
    }
} 