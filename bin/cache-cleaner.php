#!/usr/bin/env php
<?php
/**
 * 🧹 FUJSEN Cache Cleaner & Smart Cache Manager
 * =============================================
 * "Keeping the elephant's memory tidy and intelligent"
 * 
 * This service provides comprehensive cache management including:
 * - Cache cleanup and optimization
 * - Cache invalidation strategies
 * - Cache warming and preloading
 * - Performance analytics and optimization
 */

// Auto-detect FUJSEN installation path
$fujsenPaths = [
    __DIR__ . '/../fujsen/src',
    __DIR__ . '/../../fujsen/src',
    '/usr/local/lib/tusklang/fujsen/src',
    '/opt/tusklang/fujsen/src'
];

$fujsenPath = null;
foreach ($fujsenPaths as $path) {
    if (file_exists($path . '/autoload.php')) {
        $fujsenPath = $path;
        break;
    }
}

if (!$fujsenPath) {
    die("❌ FUJSEN installation not found\n");
}

require_once $fujsenPath . '/autoload.php';

class CacheCleaner 
{
    private $logFile;
    private $isService;
    private $cacheStats;
    private $invalidationStrategies;
    private $warmingStrategies;
    private $performanceMetrics;
    
    public function __construct($isService = false) 
    {
        $this->isService = $isService;
        $this->logFile = __DIR__ . '/../logs/cache-cleaner.log';
        $this->cacheStats = [];
        $this->invalidationStrategies = [];
        $this->warmingStrategies = [];
        $this->performanceMetrics = [];
        
        if (!$isService) {
            echo "🧹 FUJSEN Smart Cache Manager v3.0.0-Elder\n";
            echo "   Intelligent Cache Management & Analytics\n\n";
        }
    }
    
    public function runService() 
    {
        $this->log("🔄 Smart cache manager service started");
        
        while (true) {
            try {
                $this->runCleanup();
                $this->runInvalidationStrategies();
                $this->runCacheWarming();
                $this->collectPerformanceMetrics();
                $this->log("✅ Smart cache cycle completed");
            } catch (Exception $e) {
                $this->log("❌ Smart cache error: " . $e->getMessage());
            }
            
            // Sleep for 5 minutes
            sleep(300);
        }
    }
    
    public function runOnce() 
    {
        echo "🧹 Running smart cache management...\n\n";
        $this->runCleanup();
        $this->runInvalidationStrategies();
        $this->runCacheWarming();
        $this->collectPerformanceMetrics();
        $this->showAnalytics();
        echo "\n✅ Smart cache management completed!\n";
    }
    
    public function runInvalidationStrategies() 
    {
        echo "🔄 Running cache invalidation strategies...\n";
        
        // Strategy 1: Time-based invalidation
        $this->timeBasedInvalidation();
        
        // Strategy 2: Pattern-based invalidation
        $this->patternBasedInvalidation();
        
        // Strategy 3: Tag-based invalidation
        $this->tagBasedInvalidation();
        
        // Strategy 4: Event-based invalidation
        $this->eventBasedInvalidation();
        
        // Strategy 5: Memory-based invalidation
        $this->memoryBasedInvalidation();
        
        echo "✅ Cache invalidation strategies completed\n";
    }
    
    public function runCacheWarming() 
    {
        echo "🔥 Running cache warming strategies...\n";
        
        // Strategy 1: Preload frequently accessed data
        $this->preloadFrequentData();
        
        // Strategy 2: Warm JIT cache
        $this->warmJITCache();
        
        // Strategy 3: Warm database query cache
        $this->warmDatabaseCache();
        
        // Strategy 4: Warm API endpoints
        $this->warmAPIEndpoints();
        
        // Strategy 5: Warm configuration cache
        $this->warmConfigurationCache();
        
        echo "✅ Cache warming strategies completed\n";
    }
    
    public function collectPerformanceMetrics() 
    {
        echo "📊 Collecting performance metrics...\n";
        
        $this->performanceMetrics = [
            'timestamp' => time(),
            'memory_usage' => memory_get_usage(true),
            'peak_memory' => memory_get_peak_usage(true),
            'cache_hit_rate' => $this->calculateCacheHitRate(),
            'cache_miss_rate' => $this->calculateCacheMissRate(),
            'cache_size' => $this->calculateCacheSize(),
            'invalidation_count' => $this->getInvalidationCount(),
            'warming_count' => $this->getWarmingCount(),
            'performance_score' => $this->calculatePerformanceScore()
        ];
        
        // Store metrics for historical analysis
        $this->storePerformanceMetrics();
        
        echo "✅ Performance metrics collected\n";
    }
    
    public function showAnalytics() 
    {
        echo "\n📊 Cache Performance Analytics:\n";
        echo "================================\n";
        
        if (!empty($this->performanceMetrics)) {
            echo "Memory Usage: " . $this->formatBytes($this->performanceMetrics['memory_usage']) . "\n";
            echo "Peak Memory: " . $this->formatBytes($this->performanceMetrics['peak_memory']) . "\n";
            echo "Cache Hit Rate: " . round($this->performanceMetrics['cache_hit_rate'], 2) . "%\n";
            echo "Cache Miss Rate: " . round($this->performanceMetrics['cache_miss_rate'], 2) . "%\n";
            echo "Cache Size: " . $this->formatBytes($this->performanceMetrics['cache_size']) . "\n";
            echo "Performance Score: " . round($this->performanceMetrics['performance_score'], 2) . "/100\n";
        }
        
        echo "\n🔄 Invalidation Strategies:\n";
        foreach ($this->invalidationStrategies as $strategy => $count) {
            echo "  $strategy: $count items invalidated\n";
        }
        
        echo "\n🔥 Warming Strategies:\n";
        foreach ($this->warmingStrategies as $strategy => $count) {
            echo "  $strategy: $count items warmed\n";
        }
    }
    
    // Cache Invalidation Strategies
    
    private function timeBasedInvalidation() 
    {
        $invalidated = 0;
        $cacheDirs = $this->getCacheDirectories();
        
        foreach ($cacheDirs as $dir) {
            if (is_dir($dir)) {
                $files = glob($dir . '/*');
                foreach ($files as $file) {
                    if (is_file($file)) {
                        $age = time() - filemtime($file);
                        $maxAge = $this->getMaxAgeForFile($file);
                        
                        if ($age > $maxAge) {
                            if (unlink($file)) {
                                $invalidated++;
                            }
                        }
                    }
                }
            }
        }
        
        $this->invalidationStrategies['time_based'] = $invalidated;
        $this->log("Time-based invalidation: $invalidated items");
    }
    
    private function patternBasedInvalidation() 
    {
        $invalidated = 0;
        $patterns = [
            'temp:*' => 1800,        // 30 minutes
            'session:*' => 3600,     // 1 hour
            'debug:*' => 300,        // 5 minutes
            'test:*' => 600          // 10 minutes
        ];
        
        $cacheDirs = $this->getCacheDirectories();
        
        foreach ($cacheDirs as $dir) {
            if (is_dir($dir)) {
                foreach ($patterns as $pattern => $maxAge) {
                    $files = glob($dir . '/' . str_replace('*', '*', $pattern));
                    foreach ($files as $file) {
                        if (is_file($file) && (time() - filemtime($file)) > $maxAge) {
                            if (unlink($file)) {
                                $invalidated++;
                            }
                        }
                    }
                }
            }
        }
        
        $this->invalidationStrategies['pattern_based'] = $invalidated;
        $this->log("Pattern-based invalidation: $invalidated items");
    }
    
    private function tagBasedInvalidation() 
    {
        $invalidated = 0;
        
        // Read cache tags from metadata
        $tagFile = __DIR__ . '/../cache/tags.json';
        if (file_exists($tagFile)) {
            $tags = json_decode(file_get_contents($tagFile), true);
            
            foreach ($tags as $tag => $files) {
                if ($this->shouldInvalidateTag($tag)) {
                    foreach ($files as $file) {
                        if (file_exists($file) && unlink($file)) {
                            $invalidated++;
                        }
                    }
                    // Remove tag from metadata
                    unset($tags[$tag]);
                }
            }
            
            // Update tag metadata
            file_put_contents($tagFile, json_encode($tags));
        }
        
        $this->invalidationStrategies['tag_based'] = $invalidated;
        $this->log("Tag-based invalidation: $invalidated items");
    }
    
    private function eventBasedInvalidation() 
    {
        $invalidated = 0;
        
        // Check for invalidation events
        $eventFile = __DIR__ . '/../cache/events.json';
        if (file_exists($eventFile)) {
            $events = json_decode(file_get_contents($eventFile), true);
            
            foreach ($events as $event => $files) {
                if ($this->shouldProcessEvent($event)) {
                    foreach ($files as $file) {
                        if (file_exists($file) && unlink($file)) {
                            $invalidated++;
                        }
                    }
                }
            }
            
            // Clear processed events
            file_put_contents($eventFile, json_encode([]));
        }
        
        $this->invalidationStrategies['event_based'] = $invalidated;
        $this->log("Event-based invalidation: $invalidated items");
    }
    
    private function memoryBasedInvalidation() 
    {
        $invalidated = 0;
        $memoryLimit = 0.8; // 80% of available memory
        $currentMemory = memory_get_usage(true);
        $memoryLimitBytes = ini_get('memory_limit');
        
        if ($currentMemory > ($memoryLimitBytes * $memoryLimit)) {
            // Remove oldest cache entries
            $cacheDirs = $this->getCacheDirectories();
            
            foreach ($cacheDirs as $dir) {
                if (is_dir($dir)) {
                    $files = glob($dir . '/*');
                    usort($files, function($a, $b) {
                        return filemtime($a) - filemtime($b);
                    });
                    
                    // Remove oldest 20% of files
                    $removeCount = ceil(count($files) * 0.2);
                    for ($i = 0; $i < $removeCount; $i++) {
                        if (isset($files[$i]) && unlink($files[$i])) {
                            $invalidated++;
                        }
                    }
                }
            }
        }
        
        $this->invalidationStrategies['memory_based'] = $invalidated;
        $this->log("Memory-based invalidation: $invalidated items");
    }
    
    // Cache Warming Strategies
    
    private function preloadFrequentData() 
    {
        $warmed = 0;
        
        // Preload frequently accessed configuration
        $frequentConfigs = [
            'app_config' => __DIR__ . '/../config/app.tsk',
            'database_config' => __DIR__ . '/../config/database.tsk',
            'cache_config' => __DIR__ . '/../config/cache.tsk'
        ];
        
        foreach ($frequentConfigs as $key => $file) {
            if (file_exists($file)) {
                $content = file_get_contents($file);
                $cacheFile = __DIR__ . '/../cache/' . $key . '.cache';
                file_put_contents($cacheFile, $content);
                $warmed++;
            }
        }
        
        $this->warmingStrategies['frequent_data'] = $warmed;
        $this->log("Frequent data warming: $warmed items");
    }
    
    private function warmJITCache() 
    {
        $warmed = 0;
        
        // Warm JIT cache with common patterns
        if (class_exists('TuskPHP\Utils\TuskLangJITOptimizer')) {
            $optimizer = new \TuskPHP\Utils\TuskLangJITOptimizer();
            $optimizer->warmupJIT();
            $warmed = 1; // JIT warmup completed
        }
        
        $this->warmingStrategies['jit_cache'] = $warmed;
        $this->log("JIT cache warming: $warmed items");
    }
    
    private function warmDatabaseCache() 
    {
        $warmed = 0;
        
        // Warm database query cache
        if (class_exists('TuskPHP\TuskDb')) {
            try {
                $db = \TuskPHP\TuskDb::getInstance();
                
                // Preload common queries
                $commonQueries = [
                    'SELECT COUNT(*) FROM users',
                    'SELECT * FROM config WHERE active = 1',
                    'SELECT * FROM cache_stats ORDER BY created_at DESC LIMIT 10'
                ];
                
                foreach ($commonQueries as $query) {
                    $db->query($query);
                    $warmed++;
                }
            } catch (Exception $e) {
                $this->log("Database cache warming error: " . $e->getMessage());
            }
        }
        
        $this->warmingStrategies['database_cache'] = $warmed;
        $this->log("Database cache warming: $warmed items");
    }
    
    private function warmAPIEndpoints() 
    {
        $warmed = 0;
        
        // Warm API endpoints
        $apiEndpoints = [
            '/api/status',
            '/api/health',
            '/api/config'
        ];
        
        foreach ($apiEndpoints as $endpoint) {
            $cacheFile = __DIR__ . '/../cache/api' . str_replace('/', '_', $endpoint) . '.cache';
            if (!file_exists($cacheFile)) {
                file_put_contents($cacheFile, json_encode(['warmed_at' => time()]));
                $warmed++;
            }
        }
        
        $this->warmingStrategies['api_endpoints'] = $warmed;
        $this->log("API endpoints warming: $warmed items");
    }
    
    private function warmConfigurationCache() 
    {
        $warmed = 0;
        
        // Warm configuration cache
        $configFiles = glob(__DIR__ . '/../config/*.tsk');
        
        foreach ($configFiles as $file) {
            $key = 'config_' . basename($file, '.tsk');
            $cacheFile = __DIR__ . '/../cache/' . $key . '.cache';
            
            if (!file_exists($cacheFile) || (time() - filemtime($cacheFile)) > 3600) {
                $content = file_get_contents($file);
                file_put_contents($cacheFile, $content);
                $warmed++;
            }
        }
        
        $this->warmingStrategies['configuration_cache'] = $warmed;
        $this->log("Configuration cache warming: $warmed items");
    }
    
    // Performance Analytics
    
    private function calculateCacheHitRate() 
    {
        $statsFile = __DIR__ . '/../cache/stats.json';
        if (file_exists($statsFile)) {
            $stats = json_decode(file_get_contents($statsFile), true);
            $hits = $stats['hits'] ?? 0;
            $misses = $stats['misses'] ?? 0;
            $total = $hits + $misses;
            
            return $total > 0 ? ($hits / $total) * 100 : 0;
        }
        
        return 0;
    }
    
    private function calculateCacheMissRate() 
    {
        return 100 - $this->calculateCacheHitRate();
    }
    
    private function calculateCacheSize() 
    {
        $size = 0;
        $cacheDirs = $this->getCacheDirectories();
        
        foreach ($cacheDirs as $dir) {
            if (is_dir($dir)) {
                $iterator = new RecursiveIteratorIterator(
                    new RecursiveDirectoryIterator($dir, RecursiveDirectoryIterator::SKIP_DOTS)
                );
                
                foreach ($iterator as $file) {
                    if ($file->isFile()) {
                        $size += $file->getSize();
                    }
                }
            }
        }
        
        return $size;
    }
    
    private function calculatePerformanceScore() 
    {
        $score = 100;
        
        // Deduct points for low cache hit rate
        $hitRate = $this->calculateCacheHitRate();
        if ($hitRate < 80) {
            $score -= (80 - $hitRate) * 0.5;
        }
        
        // Deduct points for high memory usage
        $memoryUsage = memory_get_usage(true);
        $memoryLimit = ini_get('memory_limit');
        $memoryPercent = ($memoryUsage / $memoryLimit) * 100;
        if ($memoryPercent > 70) {
            $score -= ($memoryPercent - 70) * 0.3;
        }
        
        // Deduct points for large cache size
        $cacheSize = $this->calculateCacheSize();
        if ($cacheSize > 100 * 1024 * 1024) { // 100MB
            $score -= 10;
        }
        
        return max(0, $score);
    }
    
    private function getInvalidationCount() 
    {
        return array_sum($this->invalidationStrategies);
    }
    
    private function getWarmingCount() 
    {
        return array_sum($this->warmingStrategies);
    }
    
    private function storePerformanceMetrics() 
    {
        $metricsFile = __DIR__ . '/../cache/performance_metrics.json';
        $metrics = [];
        
        if (file_exists($metricsFile)) {
            $metrics = json_decode(file_get_contents($metricsFile), true);
        }
        
        $metrics[] = $this->performanceMetrics;
        
        // Keep only last 1000 metrics
        if (count($metrics) > 1000) {
            $metrics = array_slice($metrics, -1000);
        }
        
        file_put_contents($metricsFile, json_encode($metrics));
    }
    
    // Helper Methods
    
    private function getCacheDirectories() 
    {
        return [
            __DIR__ . '/../cache',
            __DIR__ . '/../tmp',
            sys_get_temp_dir() . '/fujsen',
            '/tmp/tusklang'
        ];
    }
    
    private function getMaxAgeForFile($file) 
    {
        $filename = basename($file);
        
        if (strpos($filename, 'temp_') === 0) {
            return 1800; // 30 minutes
        } elseif (strpos($filename, 'session_') === 0) {
            return 3600; // 1 hour
        } elseif (strpos($filename, 'debug_') === 0) {
            return 300; // 5 minutes
        } elseif (strpos($filename, 'test_') === 0) {
            return 600; // 10 minutes
        } else {
            return 7200; // 2 hours default
        }
    }
    
    private function shouldInvalidateTag($tag) 
    {
        // Implement tag invalidation logic
        $tagRules = [
            'user_data' => 3600,      // 1 hour
            'product_data' => 7200,   // 2 hours
            'system_data' => 1800,    // 30 minutes
            'temporary' => 300        // 5 minutes
        ];
        
        $tagFile = __DIR__ . '/../cache/tags.json';
        if (file_exists($tagFile)) {
            $tags = json_decode(file_get_contents($tagFile), true);
            $tagData = $tags[$tag] ?? null;
            
            if ($tagData && isset($tagData['created_at'])) {
                $age = time() - $tagData['created_at'];
                $maxAge = $tagRules[$tag] ?? 3600;
                return $age > $maxAge;
            }
        }
        
        return false;
    }
    
    private function shouldProcessEvent($event) 
    {
        // Implement event processing logic
        $eventTypes = ['cache_clear', 'config_update', 'user_logout'];
        return in_array($event, $eventTypes);
    }
    
    private function formatBytes($bytes) 
    {
        $units = ['B', 'KB', 'MB', 'GB'];
        $bytes = max($bytes, 0);
        $pow = floor(($bytes ? log($bytes) : 0) / log(1024));
        $pow = min($pow, count($units) - 1);
        
        $bytes /= pow(1024, $pow);
        return round($bytes, 2) . ' ' . $units[$pow];
    }
    
    // Original cleanup methods (preserved)
    
    private function runCleanup() 
    {
        $totalCleaned = 0;
        
        // Clean various cache directories
        $cacheDirs = [
            __DIR__ . '/../cache',
            __DIR__ . '/../tmp',
            sys_get_temp_dir() . '/fujsen',
            '/tmp/tusklang'
        ];
        
        foreach ($cacheDirs as $dir) {
            if (is_dir($dir)) {
                $cleaned = $this->cleanDirectory($dir);
                $totalCleaned += $cleaned;
            }
        }
        
        // Clean JIT optimizer cache
        $jitCacheDir = __DIR__ . '/../fujsen/cache/jit';
        if (is_dir($jitCacheDir)) {
            $cleaned = $this->cleanJITCache($jitCacheDir);
            $totalCleaned += $cleaned;
        }
        
        // Clean old log files
        $logDir = __DIR__ . '/../logs';
        if (is_dir($logDir)) {
            $cleaned = $this->cleanOldLogs($logDir);
            $totalCleaned += $cleaned;
        }
        
        // Clean session files
        $sessionDir = session_save_path() ?: '/tmp';
        if (is_dir($sessionDir)) {
            $cleaned = $this->cleanSessions($sessionDir);
            $totalCleaned += $cleaned;
        }
        
        // Optimize database if available
        $this->optimizeDatabase();
        
        $message = "Cleaned $totalCleaned files/items";
        if (!$this->isService) {
            echo "🧹 $message\n";
        }
        $this->log($message);
        
        return $totalCleaned;
    }
    
    private function cleanDirectory($dir, $maxAge = 3600) 
    {
        $cleaned = 0;
        
        if (!is_dir($dir)) {
            return 0;
        }
        
        $iterator = new RecursiveIteratorIterator(
            new RecursiveDirectoryIterator($dir, RecursiveDirectoryIterator::SKIP_DOTS),
            RecursiveIteratorIterator::CHILD_FIRST
        );
        
        foreach ($iterator as $file) {
            if ($file->isFile()) {
                // Check if file is older than maxAge
                if (time() - $file->getMTime() > $maxAge) {
                    if (unlink($file->getPathname())) {
                        $cleaned++;
                    }
                }
            } elseif ($file->isDir()) {
                // Remove empty directories
                if ($this->isDirEmpty($file->getPathname())) {
                    if (rmdir($file->getPathname())) {
                        $cleaned++;
                    }
                }
            }
        }
        
        return $cleaned;
    }
    
    private function cleanJITCache($dir) 
    {
        $cleaned = 0;
        
        if (!is_dir($dir)) {
            return 0;
        }
        
        // JIT cache files older than 1 hour
        $maxAge = 3600;
        
        $files = glob($dir . '/*.cache');
        foreach ($files as $file) {
            if (time() - filemtime($file) > $maxAge) {
                if (unlink($file)) {
                    $cleaned++;
                }
            }
        }
        
        if (!$this->isService) {
            echo "🚀 Cleaned $cleaned JIT cache files\n";
        }
        
        return $cleaned;
    }
    
    private function cleanOldLogs($dir) 
    {
        $cleaned = 0;
        
        if (!is_dir($dir)) {
            return 0;
        }
        
        // Keep logs for 7 days
        $maxAge = 7 * 24 * 3600;
        
        $files = glob($dir . '/*.log');
        foreach ($files as $file) {
            // Don't delete current log files
            if (basename($file) !== basename($this->logFile)) {
                if (time() - filemtime($file) > $maxAge) {
                    if (unlink($file)) {
                        $cleaned++;
                    }
                }
            }
        }
        
        if (!$this->isService && $cleaned > 0) {
            echo "📝 Cleaned $cleaned old log files\n";
        }
        
        return $cleaned;
    }
    
    private function cleanSessions($dir) 
    {
        $cleaned = 0;
        
        // Clean PHP session files older than 24 hours
        $maxAge = 24 * 3600;
        
        $files = glob($dir . '/sess_*');
        foreach ($files as $file) {
            if (time() - filemtime($file) > $maxAge) {
                if (unlink($file)) {
                    $cleaned++;
                }
            }
        }
        
        if (!$this->isService && $cleaned > 0) {
            echo "🔐 Cleaned $cleaned session files\n";
        }
        
        return $cleaned;
    }
    
    private function optimizeDatabase() 
    {
        try {
            // Try to optimize using TuskDb if available
            if (class_exists('TuskPHP\TuskDb')) {
                $config = \TuskPHP\TuskDb::getDbConfig();
                
                if ($config['type'] === 'sqlite') {
                    \TuskPHP\TuskDb::query('VACUUM');
                    \TuskPHP\TuskDb::query('ANALYZE');
                    
                    if (!$this->isService) {
                        echo "🗄️ Optimized SQLite database\n";
                    }
                    $this->log("Optimized SQLite database");
                } elseif ($config['type'] === 'postgresql') {
                    \TuskPHP\TuskDb::query('VACUUM ANALYZE');
                    
                    if (!$this->isService) {
                        echo "🗄️ Optimized PostgreSQL database\n";
                    }
                    $this->log("Optimized PostgreSQL database");
                }
            }
        } catch (Exception $e) {
            $this->log("Database optimization failed: " . $e->getMessage());
        }
    }
    
    private function isDirEmpty($dir) 
    {
        if (!is_readable($dir)) {
            return false;
        }
        
        $handle = opendir($dir);
        while (false !== ($entry = readdir($handle))) {
            if ($entry != "." && $entry != "..") {
                closedir($handle);
                return false;
            }
        }
        closedir($handle);
        return true;
    }
    
    private function log($message) 
    {
        $timestamp = date('Y-m-d H:i:s');
        $logEntry = "[$timestamp] $message\n";
        
        if (!$this->isService) {
            echo $logEntry;
        }
        
        file_put_contents($this->logFile, $logEntry, FILE_APPEND | LOCK_EX);
    }
}

// CLI handling
if (php_sapi_name() === 'cli') {
    $cleaner = new CacheCleaner();
    
    if (isset($argv[1])) {
        switch ($argv[1]) {
            case '--service':
                $cleaner = new CacheCleaner(true);
                $cleaner->runService();
                break;
            case '--invalidate':
                $cleaner->runInvalidationStrategies();
                break;
            case '--warm':
                $cleaner->runCacheWarming();
                break;
            case '--analytics':
                $cleaner->collectPerformanceMetrics();
                $cleaner->showAnalytics();
                break;
            case '--help':
                echo "FUJSEN Smart Cache Manager\n\n";
                echo "Usage:\n";
                echo "  php cache-cleaner.php              # Run full cleanup\n";
                echo "  php cache-cleaner.php --service    # Run as service\n";
                echo "  php cache-cleaner.php --invalidate # Run invalidation only\n";
                echo "  php cache-cleaner.php --warm       # Run warming only\n";
                echo "  php cache-cleaner.php --analytics  # Show analytics\n";
                break;
            default:
                $cleaner->runOnce();
                break;
        }
    } else {
        $cleaner->runOnce();
    }
} 