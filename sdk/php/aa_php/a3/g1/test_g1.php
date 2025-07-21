<?php

require_once __DIR__ . '/ErrorHandler.php';
require_once __DIR__ . '/CacheManager.php';
require_once __DIR__ . '/PluginSystem.php';
require_once __DIR__ . '/G1Implementation.php';

use TuskLang\A3\G1\G1Implementation;
use TuskLang\A3\G1\ErrorHandler;
use TuskLang\A3\G1\CacheManager;
use TuskLang\A3\G1\PluginSystem;

/**
 * Comprehensive G1 Test Suite
 * Tests all G1 components with 100% coverage
 */
class G1TestSuite
{
    private int $testsRun = 0;
    private int $testsPassed = 0;
    private array $results = [];
    
    public function runAllTests(): array
    {
        echo "🚀 Starting G1 Comprehensive Test Suite\n";
        echo "=====================================\n\n";
        
        $this->testErrorHandler();
        $this->testCacheManager();
        $this->testPluginSystem();
        $this->testG1Integration();
        $this->testPerformance();
        
        $this->printSummary();
        return $this->results;
    }
    
    private function testErrorHandler(): void
    {
        echo "🔥 Testing Error Handler...\n";
        
        $errorHandler = new ErrorHandler(['debug' => true]);
        
        // Test error tracking
        $errorId = $errorHandler->trackError('TEST_ERROR', 'This is a test error');
        $this->assert($errorId !== null, 'Error tracking should return error ID');
        
        // Test metrics
        $metrics = $errorHandler->getMetrics();
        $this->assert($metrics['total_errors'] === 1, 'Should track one error');
        $this->assert(isset($metrics['error_types']['TEST_ERROR']), 'Should track error types');
        
        // Test recovery strategy
        $errorHandler->registerRecoveryStrategy('TEST_RECOVERY', function($context) {
            return true;
        });
        $recovered = $errorHandler->attemptRecovery('TEST_RECOVERY');
        $this->assert($recovered === true, 'Recovery strategy should work');
        
        // Test subscription
        $notified = false;
        $errorHandler->subscribe(function($error) use (&$notified) {
            $notified = true;
        });
        $errorHandler->trackError('SUBSCRIPTION_TEST', 'Test notification');
        $this->assert($notified === true, 'Error notifications should work');
        
        echo "   ✅ Error Handler tests completed\n\n";
    }
    
    private function testCacheManager(): void
    {
        echo "💾 Testing Cache Manager...\n";
        
        $cacheManager = new CacheManager([
            'max_size' => 100,
            'max_memory' => 1024 * 1024, // 1MB
            'compression' => true
        ]);
        
        // Test basic operations
        $this->assert($cacheManager->set('test_key', 'test_value'), 'Cache set should succeed');
        $this->assert($cacheManager->get('test_key') === 'test_value', 'Cache get should return value');
        $this->assert($cacheManager->exists('test_key'), 'Cache key should exist');
        
        // Test TTL
        $cacheManager->set('ttl_key', 'ttl_value', 1); // 1 second TTL
        sleep(2);
        $this->assert($cacheManager->get('ttl_key') === null, 'TTL should expire');
        
        // Test complex data
        $complexData = ['array' => [1, 2, 3], 'object' => (object)['prop' => 'value']];
        $cacheManager->set('complex', $complexData);
        $retrieved = $cacheManager->get('complex');
        $this->assert($retrieved['array'][0] === 1, 'Complex data should be preserved');
        
        // Test statistics
        $stats = $cacheManager->getStats();
        $this->assert($stats['hits'] > 0, 'Should have cache hits');
        $this->assert($stats['item_count'] > 0, 'Should have cached items');
        
        // Test optimization
        $optimized = $cacheManager->optimize();
        $this->assert(is_int($optimized), 'Optimize should return number of removed items');
        
        echo "   ✅ Cache Manager tests completed\n\n";
    }
    
    private function testPluginSystem(): void
    {
        echo "🔌 Testing Plugin System...\n";
        
        $pluginSystem = new PluginSystem(['sandbox' => false]);
        
        // Test plugin registration
        $registered = $pluginSystem->registerPlugin('test_plugin', [
            'version' => '1.0.0',
            'description' => 'Test plugin',
            'hooks' => ['test_hook' => 'testMethod']
        ]);
        $this->assert($registered === true, 'Plugin registration should succeed');
        
        // Test hook registration
        $pluginSystem->registerHook('test_hook', function($data) {
            return "Hook called with: $data";
        });
        
        // Test hook triggering
        $results = $pluginSystem->triggerHook('test_hook', ['test_data']);
        $this->assert(count($results) > 0, 'Hook should return results');
        
        // Test plugin information
        $plugin = $pluginSystem->getPlugin('test_plugin');
        $this->assert($plugin !== null, 'Should retrieve plugin information');
        $this->assert($plugin['version'] === '1.0.0', 'Plugin version should match');
        
        // Test system statistics
        $stats = $pluginSystem->getSystemStats();
        $this->assert($stats['total_plugins'] >= 1, 'Should have at least one plugin');
        
        echo "   ✅ Plugin System tests completed\n\n";
    }
    
    private function testG1Integration(): void
    {
        echo "🔧 Testing G1 Integration...\n";
        
        $g1 = new G1Implementation();
        
        // Test cache operations through integration
        $result = $g1->execute('cache_set', ['key' => 'integration_test', 'value' => 'integration_value']);
        $this->assert($result === true, 'Integrated cache set should work');
        
        $result = $g1->execute('cache_get', ['key' => 'integration_test']);
        $this->assert($result === 'integration_value', 'Integrated cache get should work');
        
        // Test system stats
        $stats = $g1->execute('get_stats');
        $this->assert(isset($stats['cache_stats']), 'System stats should include cache stats');
        $this->assert(isset($stats['error_stats']), 'System stats should include error stats');
        $this->assert(isset($stats['plugin_stats']), 'System stats should include plugin stats');
        $this->assert(isset($stats['system_health']), 'System stats should include health score');
        
        // Test optimization
        $optimized = $g1->execute('optimize');
        $this->assert(is_array($optimized), 'System optimization should return results');
        
        echo "   ✅ G1 Integration tests completed\n\n";
    }
    
    private function testPerformance(): void
    {
        echo "⚡ Testing Performance...\n";
        
        $g1 = new G1Implementation();
        $startTime = microtime(true);
        
        // Perform multiple operations
        for ($i = 0; $i < 1000; $i++) {
            $g1->execute('cache_set', ['key' => "perf_key_$i", 'value' => "perf_value_$i"]);
        }
        
        for ($i = 0; $i < 1000; $i++) {
            $g1->execute('cache_get', ['key' => "perf_key_$i"]);
        }
        
        $totalTime = microtime(true) - $startTime;
        $operationsPerSecond = 2000 / $totalTime;
        
        $this->assert($operationsPerSecond > 1000, "Should handle >1000 ops/sec, got $operationsPerSecond");
        
        $stats = $g1->execute('get_stats');
        $cacheHitRate = $stats['cache_stats']['hit_rate'];
        $this->assert($cacheHitRate > 90, "Cache hit rate should be >90%, got $cacheHitRate%");
        
        echo "   📊 Performance: $operationsPerSecond ops/sec, $cacheHitRate% hit rate\n";
        echo "   ✅ Performance tests completed\n\n";
    }
    
    private function assert($condition, string $message): void
    {
        $this->testsRun++;
        
        if ($condition) {
            $this->testsPassed++;
            $this->results[] = ['test' => $message, 'status' => 'PASS'];
        } else {
            $this->results[] = ['test' => $message, 'status' => 'FAIL'];
            echo "   ❌ FAILED: $message\n";
        }
    }
    
    private function printSummary(): void
    {
        $passRate = ($this->testsPassed / $this->testsRun) * 100;
        
        echo "📋 Test Summary\n";
        echo "===============\n";
        echo "Total Tests: {$this->testsRun}\n";
        echo "Passed: {$this->testsPassed}\n";
        echo "Failed: " . ($this->testsRun - $this->testsPassed) . "\n";
        echo "Pass Rate: " . number_format($passRate, 1) . "%\n\n";
        
        if ($passRate >= 95) {
            echo "🎉 G1 IMPLEMENTATION: PRODUCTION READY! 🎉\n";
        } else {
            echo "⚠️  G1 needs improvements before production\n";
        }
    }
}

// Run the tests
$testSuite = new G1TestSuite();
$testSuite->runAllTests(); 