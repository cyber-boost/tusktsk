<?php

namespace TuskLang\A3\G1;

/**
 * G1 Implementation - Enhanced Error Handling, Caching, and Plugin System Integration
 * 
 * Integrates all G1 components:
 * - Enhanced Error Handling and Validation
 * - Advanced Caching and Performance Optimization  
 * - Plugin System and Extensibility Framework
 * 
 * @version 2.0.0
 * @package TuskLang\A3\G1
 */
class G1Implementation
{
    private ErrorHandler $errorHandler;
    private CacheManager $cacheManager;
    private PluginSystem $pluginSystem;
    private array $config;
    private array $performance = [];
    
    public function __construct(array $config = [])
    {
        $this->config = array_merge([
            'error_handler' => [
                'debug' => false,
                'log_file' => '/tmp/tusklang_g1_errors.log'
            ],
            'cache_manager' => [
                'max_size' => 10000,
                'max_memory' => 1024 * 1024 * 50, // 50MB
                'compression' => true,
                'cache_dir' => '/tmp/tusklang_g1_cache'
            ],
            'plugin_system' => [
                'plugin_dir' => '/tmp/tusklang_g1_plugins',
                'sandbox' => true
            ]
        ], $config);
        
        $this->initializeComponents();
        $this->registerSystemPlugins();
    }
    
    /**
     * Initialize all G1 components
     */
    private function initializeComponents(): void
    {
        $startTime = microtime(true);
        
        // Initialize Error Handler
        $this->errorHandler = new ErrorHandler($this->config['error_handler']);
        
        // Initialize Cache Manager
        $this->cacheManager = new CacheManager($this->config['cache_manager']);
        
        // Initialize Plugin System with error handler
        $pluginConfig = $this->config['plugin_system'];
        $pluginConfig['error_handler'] = $this->errorHandler;
        $this->pluginSystem = new PluginSystem($pluginConfig);
        
        $this->performance['initialization_time'] = microtime(true) - $startTime;
        
        // Subscribe to error notifications
        $this->errorHandler->subscribe([$this, 'handleErrorNotification']);
    }
    
    /**
     * Register built-in system plugins
     */
    private function registerSystemPlugins(): void
    {
        // Performance Monitor Plugin
        $this->pluginSystem->registerPlugin('performance_monitor', [
            'version' => '1.0.0',
            'description' => 'Monitors system performance and cache efficiency',
            'hooks' => [
                'cache_hit' => 'onCacheHit',
                'cache_miss' => 'onCacheMiss',
                'error_occurred' => 'onErrorOccurred'
            ],
            'auto_load' => true,
            'class' => PerformanceMonitorPlugin::class,
            'config' => [
                'monitor_interval' => 30,
                'alert_threshold' => 0.8
            ]
        ]);
        
        // Cache Optimizer Plugin
        $this->pluginSystem->registerPlugin('cache_optimizer', [
            'version' => '1.0.0',
            'description' => 'Automatically optimizes cache performance',
            'hooks' => [
                'cache_full' => 'optimizeCache',
                'memory_pressure' => 'evictLeastUsed'
            ],
            'auto_load' => true,
            'class' => CacheOptimizerPlugin::class,
            'config' => [
                'optimization_interval' => 60,
                'memory_threshold' => 0.85
            ]
        ]);
    }
    
    /**
     * Execute G1 functionality with integrated components
     */
    public function execute(string $operation, array $params = []): mixed
    {
        $startTime = microtime(true);
        
        try {
            switch ($operation) {
                case 'cache_set':
                    $result = $this->cacheManager->set(
                        $params['key'],
                        $params['value'],
                        $params['ttl'] ?? 0
                    );
                    $this->pluginSystem->triggerHook('cache_set', [$params['key'], $result]);
                    break;
                    
                case 'cache_get':
                    $result = $this->cacheManager->get($params['key']);
                    if ($result !== null) {
                        $this->pluginSystem->triggerHook('cache_hit', [$params['key']]);
                    } else {
                        $this->pluginSystem->triggerHook('cache_miss', [$params['key']]);
                    }
                    break;
                    
                case 'load_plugin':
                    $result = $this->pluginSystem->loadPlugin($params['name']);
                    break;
                    
                case 'register_plugin':
                    $result = $this->pluginSystem->registerPlugin($params['name'], $params['config']);
                    break;
                    
                case 'get_stats':
                    $result = $this->getSystemStats();
                    break;
                    
                case 'optimize':
                    $result = $this->optimizeSystem();
                    break;
                    
                default:
                    throw new \InvalidArgumentException("Unknown operation: $operation");
            }
            
            $executionTime = microtime(true) - $startTime;
            $this->recordPerformance($operation, $executionTime);
            
            return $result;
            
        } catch (\Exception $e) {
            $this->errorHandler->trackError('G1_OPERATION_FAILED', $e->getMessage(), [
                'operation' => $operation,
                'params' => $params
            ]);
            
            // Attempt recovery
            if ($this->errorHandler->attemptRecovery(get_class($e), ['operation' => $operation])) {
                // Retry once after recovery
                return $this->execute($operation, $params);
            }
            
            throw $e;
        }
    }
    
    /**
     * Get comprehensive system statistics
     */
    public function getSystemStats(): array
    {
        return [
            'g1_performance' => $this->performance,
            'error_stats' => $this->errorHandler->getMetrics(),
            'cache_stats' => $this->cacheManager->getStats(),
            'plugin_stats' => $this->pluginSystem->getSystemStats(),
            'system_health' => $this->calculateSystemHealth()
        ];
    }
    
    /**
     * Optimize the entire G1 system
     */
    public function optimizeSystem(): array
    {
        $results = [];
        
        // Optimize cache
        $results['cache_optimization'] = $this->cacheManager->optimize();
        
        // Clear old errors (with backup)
        $this->errorHandler->clearErrors(true);
        $results['error_cleanup'] = true;
        
        // Trigger plugin optimization
        $results['plugin_optimization'] = $this->pluginSystem->triggerHook('system_optimize');
        
        return $results;
    }
    
    /**
     * Handle error notifications from error handler
     */
    public function handleErrorNotification(array $error): void
    {
        // Trigger plugin hooks for error handling
        $this->pluginSystem->triggerHook('error_occurred', [$error]);
        
        // Check for critical errors that need immediate attention
        if (in_array($error['type'], ['FATAL_ERROR', 'MEMORY_EXHAUSTED', 'DATABASE_CONNECTION_FAILED'])) {
            $this->pluginSystem->triggerHook('critical_error', [$error]);
        }
    }
    
    /**
     * Calculate overall system health score
     */
    private function calculateSystemHealth(): array
    {
        $cacheStats = $this->cacheManager->getStats();
        $errorStats = $this->errorHandler->getMetrics();
        $pluginStats = $this->pluginSystem->getSystemStats();
        
        // Cache health (based on hit rate)
        $cacheHealth = min(100, $cacheStats['hit_rate']);
        
        // Error health (inverse of error rate)
        $errorRate = $errorStats['error_rate'];
        $errorHealth = max(0, 100 - ($errorRate * 10));
        
        // Plugin health (based on loaded vs total plugins)
        $pluginHealth = $pluginStats['total_plugins'] > 0 
            ? ($pluginStats['loaded_plugins'] / $pluginStats['total_plugins']) * 100
            : 100;
        
        // Overall health score
        $overallHealth = ($cacheHealth + $errorHealth + $pluginHealth) / 3;
        
        return [
            'overall_score' => round($overallHealth, 2),
            'cache_health' => round($cacheHealth, 2),
            'error_health' => round($errorHealth, 2),
            'plugin_health' => round($pluginHealth, 2),
            'status' => $this->getHealthStatus($overallHealth)
        ];
    }
    
    private function getHealthStatus(float $score): string
    {
        if ($score >= 90) return 'EXCELLENT';
        if ($score >= 75) return 'GOOD';
        if ($score >= 60) return 'FAIR';
        if ($score >= 40) return 'POOR';
        return 'CRITICAL';
    }
    
    private function recordPerformance(string $operation, float $executionTime): void
    {
        if (!isset($this->performance[$operation])) {
            $this->performance[$operation] = [
                'calls' => 0,
                'total_time' => 0,
                'average_time' => 0,
                'min_time' => $executionTime,
                'max_time' => $executionTime
            ];
        }
        
        $perf = &$this->performance[$operation];
        $perf['calls']++;
        $perf['total_time'] += $executionTime;
        $perf['average_time'] = $perf['total_time'] / $perf['calls'];
        $perf['min_time'] = min($perf['min_time'], $executionTime);
        $perf['max_time'] = max($perf['max_time'], $executionTime);
    }
}

/**
 * Performance Monitor Plugin
 */
class PerformanceMonitorPlugin
{
    private array $metrics = [];
    private array $config;
    
    public function __construct(array $config = [])
    {
        $this->config = $config;
    }
    
    public function onCacheHit(string $key): void
    {
        $this->recordMetric('cache_hits');
    }
    
    public function onCacheMiss(string $key): void
    {
        $this->recordMetric('cache_misses');
    }
    
    public function onErrorOccurred(array $error): void
    {
        $this->recordMetric('errors');
    }
    
    private function recordMetric(string $metric): void
    {
        if (!isset($this->metrics[$metric])) {
            $this->metrics[$metric] = 0;
        }
        $this->metrics[$metric]++;
    }
}

/**
 * Cache Optimizer Plugin
 */
class CacheOptimizerPlugin
{
    private array $config;
    
    public function __construct(array $config = [])
    {
        $this->config = $config;
    }
    
    public function optimizeCache(): void
    {
        // Cache optimization logic
    }
    
    public function evictLeastUsed(): void
    {
        // LRU eviction logic
    }
} 