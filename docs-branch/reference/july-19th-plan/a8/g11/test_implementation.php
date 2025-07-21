<?php
/**
 * 🥜 TuskLang PHP Agent A8 - Goal G11 Test Implementation
 * ========================================================
 * Agent: a8
 * Goals: g11.1, g11.2, g11.3
 * Language: PHP
 * 
 * This file tests the three goals for the PHP agent g11:
 * - g11.1: IoT Device Management and Communication
 * - g11.2: Edge Computing and Data Processing
 * - g11.3: IoT Analytics and Predictive Maintenance
 */

require_once 'implementation.php';

use TuskLang\AgentA8\G11\AgentA8G11;
use TuskLang\AgentA8\G11\IoTDeviceManager;
use TuskLang\AgentA8\G11\EdgeComputingManager;
use TuskLang\AgentA8\G11\IoTAnalyticsManager;

class AgentA8G11Test
{
    private AgentA8G11 $agent;
    private array $testResults = [];
    
    public function __construct()
    {
        $this->agent = new AgentA8G11();
    }
    
    public function runAllTests(): array
    {
        echo "🧪 Starting Agent A8 G11 Tests...\n";
        echo "================================\n\n";
        
        $this->testGoal11_1();
        $this->testGoal11_2();
        $this->testGoal11_3();
        $this->testIntegration();
        
        return $this->testResults;
    }
    
    private function testGoal11_1(): void
    {
        echo "🔧 Testing Goal 11.1: IoT Device Management and Communication\n";
        echo "------------------------------------------------------------\n";
        
        try {
            $result = $this->agent->executeGoal11_1();
            
            $this->assert($result['success'], 'Goal 11.1 execution should succeed');
            $this->assert($result['devices_registered'] >= 2, 'Should register at least 2 devices');
            $this->assert($result['commands_sent'] >= 2, 'Should send at least 2 commands');
            $this->assert(isset($result['device_statistics']), 'Should return device statistics');
            
            echo "✅ Goal 11.1 Tests Passed\n";
            $this->testResults['goal_11_1'] = ['status' => 'passed', 'result' => $result];
            
        } catch (Exception $e) {
            echo "❌ Goal 11.1 Tests Failed: " . $e->getMessage() . "\n";
            $this->testResults['goal_11_1'] = ['status' => 'failed', 'error' => $e->getMessage()];
        }
        
        echo "\n";
    }
    
    private function testGoal11_2(): void
    {
        echo "⚡ Testing Goal 11.2: Edge Computing and Data Processing\n";
        echo "-------------------------------------------------------\n";
        
        try {
            $result = $this->agent->executeGoal11_2();
            
            $this->assert($result['success'], 'Goal 11.2 execution should succeed');
            $this->assert($result['edge_nodes_created'] >= 2, 'Should create at least 2 edge nodes');
            $this->assert($result['data_processors_created'] >= 2, 'Should create at least 2 data processors');
            $this->assert(isset($result['edge_statistics']), 'Should return edge statistics');
            
            echo "✅ Goal 11.2 Tests Passed\n";
            $this->testResults['goal_11_2'] = ['status' => 'passed', 'result' => $result];
            
        } catch (Exception $e) {
            echo "❌ Goal 11.2 Tests Failed: " . $e->getMessage() . "\n";
            $this->testResults['goal_11_2'] = ['status' => 'failed', 'error' => $e->getMessage()];
        }
        
        echo "\n";
    }
    
    private function testGoal11_3(): void
    {
        echo "📊 Testing Goal 11.3: IoT Analytics and Predictive Maintenance\n";
        echo "-------------------------------------------------------------\n";
        
        try {
            $result = $this->agent->executeGoal11_3();
            
            $this->assert($result['success'], 'Goal 11.3 execution should succeed');
            $this->assert($result['analyses_performed'] >= 1, 'Should perform at least 1 analysis');
            $this->assert($result['predictions_generated'] >= 1, 'Should generate at least 1 prediction');
            $this->assert(isset($result['analytics_statistics']), 'Should return analytics statistics');
            
            echo "✅ Goal 11.3 Tests Passed\n";
            $this->testResults['goal_11_3'] = ['status' => 'passed', 'result' => $result];
            
        } catch (Exception $e) {
            echo "❌ Goal 11.3 Tests Failed: " . $e->getMessage() . "\n";
            $this->testResults['goal_11_3'] = ['status' => 'failed', 'error' => $e->getMessage()];
        }
        
        echo "\n";
    }
    
    private function testIntegration(): void
    {
        echo "🔗 Testing Integration: All Goals Together\n";
        echo "------------------------------------------\n";
        
        try {
            $result = $this->agent->executeAllGoals();
            
            $this->assert($result['success'], 'Integration execution should succeed');
            $this->assert($result['agent_id'] === 'a8', 'Agent ID should be a8');
            $this->assert($result['language'] === 'PHP', 'Language should be PHP');
            $this->assert($result['goal_id'] === 'g11', 'Goal ID should be g11');
            $this->assert(isset($result['results']['goal_11_1']), 'Should include goal 11.1 results');
            $this->assert(isset($result['results']['goal_11_2']), 'Should include goal 11.2 results');
            $this->assert(isset($result['results']['goal_11_3']), 'Should include goal 11.3 results');
            
            echo "✅ Integration Tests Passed\n";
            $this->testResults['integration'] = ['status' => 'passed', 'result' => $result];
            
        } catch (Exception $e) {
            echo "❌ Integration Tests Failed: " . $e->getMessage() . "\n";
            $this->testResults['integration'] = ['status' => 'failed', 'error' => $e->getMessage()];
        }
        
        echo "\n";
    }
    
    public function testIndividualClasses(): void
    {
        echo "🏗️  Testing Individual Classes\n";
        echo "-----------------------------\n";
        
        // Test IoT Device Manager
        try {
            $iotManager = new IoTDeviceManager();
            $device = $iotManager->registerDevice('test_device', 'sensor', ['protocol' => 'mqtt']);
            $this->assert($device['success'], 'Device registration should succeed');
            
            $command = $iotManager->sendCommand('test_device', 'read_sensor');
            $this->assert($command['success'], 'Command sending should succeed');
            
            $stats = $iotManager->getDeviceStats();
            $this->assert(isset($stats['total_devices']), 'Should return device statistics');
            
            echo "✅ IoT Device Manager Tests Passed\n";
            $this->testResults['iot_manager'] = ['status' => 'passed'];
            
        } catch (Exception $e) {
            echo "❌ IoT Device Manager Tests Failed: " . $e->getMessage() . "\n";
            $this->testResults['iot_manager'] = ['status' => 'failed', 'error' => $e->getMessage()];
        }
        
        // Test Edge Computing Manager
        try {
            $edgeManager = new EdgeComputingManager();
            $node = $edgeManager->createEdgeNode('test_node', ['location' => 'test_location']);
            $this->assert($node['success'], 'Edge node creation should succeed');
            
            $processor = $edgeManager->processData('test_node', [1, 2, 3, 4, 5], 'filtering');
            $this->assert($processor['success'], 'Data processing should succeed');
            
            $stats = $edgeManager->getEdgeStats();
            $this->assert(isset($stats['total_nodes']), 'Should return edge statistics');
            
            echo "✅ Edge Computing Manager Tests Passed\n";
            $this->testResults['edge_manager'] = ['status' => 'passed'];
            
        } catch (Exception $e) {
            echo "❌ Edge Computing Manager Tests Failed: " . $e->getMessage() . "\n";
            $this->testResults['edge_manager'] = ['status' => 'failed', 'error' => $e->getMessage()];
        }
        
        // Test IoT Analytics Manager
        try {
            $analyticsManager = new IoTAnalyticsManager();
            $analysis = $analyticsManager->analyzeDeviceData('test_device', [20, 22, 25, 23, 21]);
            $this->assert($analysis['success'], 'Data analysis should succeed');
            
            $prediction = $analyticsManager->predictMaintenance('test_device', [20, 22, 25, 23, 21]);
            $this->assert($prediction['success'], 'Maintenance prediction should succeed');
            
            $stats = $analyticsManager->getAnalyticsStats();
            $this->assert(isset($stats['total_analyses']), 'Should return analytics statistics');
            
            echo "✅ IoT Analytics Manager Tests Passed\n";
            $this->testResults['analytics_manager'] = ['status' => 'passed'];
            
        } catch (Exception $e) {
            echo "❌ IoT Analytics Manager Tests Failed: " . $e->getMessage() . "\n";
            $this->testResults['analytics_manager'] = ['status' => 'failed', 'error' => $e->getMessage()];
        }
        
        echo "\n";
    }
    
    private function assert(bool $condition, string $message): void
    {
        if (!$condition) {
            throw new Exception("Assertion failed: $message");
        }
    }
    
    public function getTestSummary(): array
    {
        $passed = 0;
        $failed = 0;
        
        foreach ($this->testResults as $test) {
            if ($test['status'] === 'passed') {
                $passed++;
            } else {
                $failed++;
            }
        }
        
        return [
            'total_tests' => count($this->testResults),
            'passed' => $passed,
            'failed' => $failed,
            'success_rate' => $passed / count($this->testResults) * 100,
            'results' => $this->testResults
        ];
    }
}

// Run tests if this file is executed directly
if (basename(__FILE__) === basename($_SERVER['SCRIPT_NAME'])) {
    $test = new AgentA8G11Test();
    $test->runAllTests();
    $test->testIndividualClasses();
    
    $summary = $test->getTestSummary();
    
    echo "📋 Test Summary\n";
    echo "===============\n";
    echo "Total Tests: {$summary['total_tests']}\n";
    echo "Passed: {$summary['passed']}\n";
    echo "Failed: {$summary['failed']}\n";
    echo "Success Rate: {$summary['success_rate']}%\n";
    
    if ($summary['failed'] === 0) {
        echo "\n🎉 All tests passed! Agent A8 G11 is working correctly.\n";
        exit(0);
    } else {
        echo "\n⚠️  Some tests failed. Please review the implementation.\n";
        exit(1);
    }
} 