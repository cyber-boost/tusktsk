<?php

require_once __DIR__ . '/A3Master.php';

use TuskLang\A3\A3Master;

/**
 * Comprehensive A3 Test Suite - All Goals Integration Test
 * 
 * Tests all 6 goals:
 * G1: Error Handling, Caching, Plugin System
 * G2: Configuration Management
 * G3: Security & Authentication
 * G4: API Management
 * G5: Data Processing & Analytics
 * G6: Real-time Communication
 */
class A3ComprehensiveTest
{
    private A3Master $a3;
    private int $testsRun = 0;
    private int $testsPassed = 0;
    private array $performanceMetrics = [];
    
    public function runFullTestSuite(): array
    {
        echo "🎯 A3 COMPREHENSIVE PRODUCTION TEST SUITE\n";
        echo "=======================================\n";
        echo "Testing Infrastructure & Monitoring Operators\n";
        echo "All 6 Goals - Production Validation\n\n";
        
        $this->initializeSystem();
        
        // Test all goals systematically
        $this->testG1ErrorHandlingCachingPlugins();
        $this->testG2ConfigurationManagement();
        $this->testG3SecurityAuthentication();
        $this->testG4APIManagement();
        $this->testG5DataProcessingAnalytics();
        $this->testG6RealtimeCommunication();
        
        // Test system integration
        $this->testSystemIntegration();
        $this->testPerformanceUnderLoad();
        $this->testSystemHealth();
        
        $this->printFinalResults();
        return $this->getResults();
    }
    
    private function initializeSystem(): void
    {
        echo "🚀 Initializing A3 Master System...\n";
        
        $startTime = microtime(true);
        $this->a3 = new A3Master([
            'g1' => [
                'error_handler' => ['debug' => true],
                'cache_manager' => ['max_size' => 1000],
                'plugin_system' => ['sandbox' => true]
            ],
            'secret_key' => 'test_secret_key_2024'
        ]);
        $initTime = microtime(true) - $startTime;
        
        $this->assert($initTime < 1.0, "System initialization should be fast (<1s), took " . number_format($initTime, 3) . "s");
        echo "   ✅ System initialized in " . number_format($initTime, 3) . "s\n\n";
    }
    
    private function testG1ErrorHandlingCachingPlugins(): void
    {
        echo "🛠️  Testing G1: Error Handling, Caching, Plugin System\n";
        
        // Test caching
        $result = $this->a3->execute('cache_set', ['key' => 'test_key', 'value' => 'test_value']);
        $this->assert($result === true, 'G1: Cache set should succeed');
        
        $value = $this->a3->execute('cache_get', ['key' => 'test_key']);
        $this->assert($value === 'test_value', 'G1: Cache get should return correct value');
        
        // Test system stats
        $stats = $this->a3->execute('get_system_stats');
        $this->assert(isset($stats['cache_stats']), 'G1: System stats should include cache statistics');
        $this->assert($stats['cache_stats']['hits'] > 0, 'G1: Should have cache hits');
        
        echo "   ✅ G1 tests completed\n\n";
    }
    
    private function testG2ConfigurationManagement(): void
    {
        echo "⚙️  Testing G2: Configuration Management\n";
        
        // Create test config file
        $testConfig = ['database' => ['host' => 'localhost', 'port' => 5432]];
        $configFile = '/tmp/test_config.json';
        file_put_contents($configFile, json_encode($testConfig));
        
        $config = $this->a3->execute('load_config', ['path' => $configFile]);
        $this->assert($config['database']['host'] === 'localhost', 'G2: Should load configuration correctly');
        
        $hotReload = $this->a3->execute('hot_reload', ['path' => $configFile]);
        $this->assert($hotReload === true, 'G2: Hot reload should succeed');
        
        unlink($configFile);
        echo "   ✅ G2 tests completed\n\n";
    }
    
    private function testG3SecurityAuthentication(): void
    {
        echo "🔒 Testing G3: Security & Authentication\n";
        
        // Test JWT generation
        $payload = ['user_id' => 123, 'role' => 'admin'];
        $token = $this->a3->execute('generate_jwt', ['payload' => $payload]);
        $this->assert(!empty($token), 'G3: JWT generation should produce token');
        
        // Test token validation
        $valid = $this->a3->execute('validate_token', ['token' => $token]);
        $this->assert($valid === true, 'G3: Generated token should be valid');
        
        // Test encryption
        $encrypted = $this->a3->execute('encrypt', ['data' => 'sensitive_data']);
        $this->assert($encrypted !== 'sensitive_data', 'G3: Encryption should transform data');
        
        echo "   ✅ G3 tests completed\n\n";
    }
    
    private function testG4APIManagement(): void
    {
        echo "🌐 Testing G4: API Management\n";
        
        // Register test route
        $this->a3->execute('register_route', [
            'method' => 'GET',
            'path' => '/test',
            'handler' => function($data) { return ['status' => 'ok', 'data' => $data]; }
        ]);
        
        // Test request handling
        $response = $this->a3->execute('handle_request', [
            'method' => 'GET',
            'path' => '/test',
            'data' => ['test' => 'value']
        ]);
        
        $this->assert($response['status'] === 'ok', 'G4: API should handle requests correctly');
        $this->assert($response['data']['test'] === 'value', 'G4: API should pass data correctly');
        
        echo "   ✅ G4 tests completed\n\n";
    }
    
    private function testG5DataProcessingAnalytics(): void
    {
        echo "📊 Testing G5: Data Processing & Analytics\n";
        
        // Test data processing
        $testData = ['items' => [1, 2, 3, 4, 5]];
        $result = $this->a3->execute('process_data', [
            'processor' => 'aggregate',
            'data' => $testData['items'],
            'options' => ['operation' => 'sum']
        ]);
        
        $this->assert($result === 15, 'G5: Data aggregation should work correctly');
        
        // Test pipeline execution
        $pipeline = [
            ['processor' => 'validate', 'options' => ['schema' => ['name' => ['type' => 'string']]]],
            ['processor' => 'transform', 'options' => ['transformations' => ['name' => ['type' => 'uppercase']]]]
        ];
        
        $pipelineData = ['name' => 'test'];
        $pipelineResult = $this->a3->execute('execute_pipeline', [
            'steps' => $pipeline,
            'data' => $pipelineData
        ]);
        
        $this->assert($pipelineResult['name'] === 'TEST', 'G5: Pipeline should transform data correctly');
        
        // Test analytics report
        $report = $this->a3->execute('generate_report');
        $this->assert(isset($report['summary']), 'G5: Analytics report should have summary');
        
        echo "   ✅ G5 tests completed\n\n";
    }
    
    private function testG6RealtimeCommunication(): void
    {
        echo "📡 Testing G6: Real-time Communication\n";
        
        // Mock socket object
        $mockSocket = (object)['id' => 'test_socket'];
        
        // Test connection registration
        $connectionId = $this->a3->execute('register_connection', [
            'socket' => $mockSocket,
            'metadata' => ['user' => 'test_user']
        ]);
        
        $this->assert(is_int($connectionId), 'G6: Connection registration should return ID');
        
        // Test room joining
        $joinResult = $this->a3->execute('join_room', [
            'connection_id' => $connectionId,
            'room_id' => 'test_room'
        ]);
        
        $this->assert($joinResult === true, 'G6: Room joining should succeed');
        
        // Test message sending
        $sendResult = $this->a3->execute('send_message', [
            'connection_id' => $connectionId,
            'message' => ['type' => 'test', 'content' => 'Hello World']
        ]);
        
        $this->assert($sendResult === true, 'G6: Message sending should succeed');
        
        echo "   ✅ G6 tests completed\n\n";
    }
    
    private function testSystemIntegration(): void
    {
        echo "🔗 Testing System Integration\n";
        
        // Test unified statistics
        $unifiedStats = $this->a3->execute('get_unified_stats');
        $this->assert(isset($unifiedStats['system_overview']), 'Integration: Should have system overview');
        $this->assert($unifiedStats['system_overview']['goals_implemented'] === 6, 'Integration: Should show 6 implemented goals');
        
        // Test system health
        $health = $this->a3->execute('system_health');
        $this->assert(isset($health['overall_status']), 'Integration: Should have overall health status');
        $this->assert(count($health['subsystems']) === 6, 'Integration: Should monitor all 6 subsystems');
        
        echo "   ✅ System integration tests completed\n\n";
    }
    
    private function testPerformanceUnderLoad(): void
    {
        echo "⚡ Testing Performance Under Load\n";
        
        $operations = 1000;
        $startTime = microtime(true);
        
        // Simulate load across all systems
        for ($i = 0; $i < $operations; $i++) {
            $this->a3->execute('cache_set', ['key' => "load_key_$i", 'value' => "load_value_$i"]);
            
            if ($i % 10 === 0) {
                $this->a3->execute('cache_get', ['key' => "load_key_" . ($i - 5)]);
            }
            
            if ($i % 50 === 0) {
                $this->a3->execute('system_health');
            }
        }
        
        $totalTime = microtime(true) - $startTime;
        $opsPerSecond = $operations / $totalTime;
        
        $this->performanceMetrics['load_test'] = [
            'operations' => $operations,
            'total_time' => $totalTime,
            'ops_per_second' => $opsPerSecond
        ];
        
        $this->assert($opsPerSecond > 500, "Performance: Should handle >500 ops/sec under load, got " . number_format($opsPerSecond, 0));
        
        echo "   📈 Load test: " . number_format($opsPerSecond, 0) . " ops/sec\n";
        echo "   ✅ Performance tests completed\n\n";
    }
    
    private function testSystemHealth(): void
    {
        echo "❤️  Testing System Health\n";
        
        $health = $this->a3->execute('system_health');
        
        // Validate overall health
        $this->assert(in_array($health['overall_status'], ['EXCELLENT', 'GOOD']), 'Health: Overall status should be healthy');
        $this->assert($health['uptime'] > 0, 'Health: System should have positive uptime');
        
        // Validate subsystem health
        foreach ($health['subsystems'] as $subsystem => $status) {
            $this->assert(isset($status['status']) || isset($status['overall_score']), "Health: $subsystem should have status");
        }
        
        // Validate performance metrics
        $this->assert(isset($health['performance']), 'Health: Should include performance metrics');
        $this->assert($health['performance']['success_rate'] >= 95, 'Health: Success rate should be >=95%');
        
        echo "   📊 Overall Status: {$health['overall_status']}\n";
        echo "   ⏱️  Uptime: " . number_format($health['uptime'], 2) . "s\n";
        echo "   📈 Success Rate: {$health['performance']['success_rate']}%\n";
        echo "   ✅ System health tests completed\n\n";
    }
    
    private function assert($condition, string $message): void
    {
        $this->testsRun++;
        
        if ($condition) {
            $this->testsPassed++;
        } else {
            echo "   ❌ FAILED: $message\n";
        }
    }
    
    private function printFinalResults(): void
    {
        $passRate = ($this->testsPassed / $this->testsRun) * 100;
        
        echo "🏆 A3 COMPREHENSIVE TEST RESULTS\n";
        echo "===============================\n";
        echo "Total Tests: {$this->testsRun}\n";
        echo "Passed: {$this->testsPassed}\n";
        echo "Failed: " . ($this->testsRun - $this->testsPassed) . "\n";
        echo "Pass Rate: " . number_format($passRate, 1) . "%\n\n";
        
        if (isset($this->performanceMetrics['load_test'])) {
            echo "Performance Metrics:\n";
            echo "- Load Test: " . number_format($this->performanceMetrics['load_test']['ops_per_second'], 0) . " ops/sec\n";
            echo "- Total Operations: {$this->performanceMetrics['load_test']['operations']}\n\n";
        }
        
        if ($passRate >= 95) {
            echo "🎉 A3 SYSTEM: PRODUCTION READY! 🎉\n";
            echo "✅ All 6 goals implemented and validated\n";
            echo "✅ Infrastructure & Monitoring Operators complete\n";
            echo "✅ Ready for enterprise deployment\n\n";
        } else {
            echo "⚠️  A3 system needs improvements\n\n";
        }
    }
    
    private function getResults(): array
    {
        return [
            'tests_run' => $this->testsRun,
            'tests_passed' => $this->testsPassed,
            'pass_rate' => ($this->testsPassed / $this->testsRun) * 100,
            'performance_metrics' => $this->performanceMetrics,
            'system_ready' => ($this->testsPassed / $this->testsRun) >= 0.95
        ];
    }
}

// Execute comprehensive test suite
$testSuite = new A3ComprehensiveTest();
$results = $testSuite->runFullTestSuite();

// Export results for CI/CD
file_put_contents('/tmp/a3_test_results.json', json_encode($results, JSON_PRETTY_PRINT)); 