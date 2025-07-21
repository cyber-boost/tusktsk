<?php
/**
 * 🥜 TuskLang PHP Agent A8 - Goal G18 Test Implementation
 * ========================================================
 * Agent: a8
 * Goals: g18.1, g18.2, g18.3
 * Language: PHP
 * 
 * This file tests the three goals for the PHP agent g18:
 * - g18.1: Robotic Control Systems and Automation
 * - g18.2: Autonomous Navigation and Path Planning
 * - g18.3: Multi-Agent Coordination and Swarm Intelligence
 */

require_once 'implementation.php';

use TuskLang\AgentA8\G18\AgentA8G18;
use TuskLang\AgentA8\G18\RoboticControlManager;
use TuskLang\AgentA8\G18\AutonomousNavigationManager;
use TuskLang\AgentA8\G18\SwarmIntelligenceManager;

class AgentA8G18Test
{
    private AgentA8G18 $agent;
    private array $testResults = [];
    
    public function __construct()
    {
        $this->agent = new AgentA8G18();
    }
    
    public function runAllTests(): array
    {
        echo "🧪 Starting Agent A8 G18 Tests...\n";
        echo "================================\n\n";
        
        $this->testGoal18_1();
        $this->testGoal18_2();
        $this->testGoal18_3();
        $this->testIntegration();
        
        return $this->testResults;
    }
    
    private function testGoal18_1(): void
    {
        echo "🤖 Testing Goal 18.1: Robotic Control Systems and Automation\n";
        echo "----------------------------------------------------------\n";
        
        try {
            $result = $this->agent->executeGoal18_1();
            
            $this->assert($result['success'], 'Goal 18.1 execution should succeed');
            $this->assert($result['robots_registered'] >= 2, 'Should register at least 2 robots');
            $this->assert($result['controllers_created'] >= 2, 'Should create at least 2 controllers');
            $this->assert($result['sensors_added'] >= 2, 'Should add at least 2 sensors');
            $this->assert($result['actuators_added'] >= 2, 'Should add at least 2 actuators');
            $this->assert($result['commands_executed'] >= 2, 'Should execute at least 2 commands');
            $this->assert(isset($result['robotic_statistics']), 'Should return robotic statistics');
            
            echo "✅ Goal 18.1 Tests Passed\n";
            $this->testResults['goal_18_1'] = ['status' => 'passed', 'result' => $result];
            
        } catch (Exception $e) {
            echo "❌ Goal 18.1 Tests Failed: " . $e->getMessage() . "\n";
            $this->testResults['goal_18_1'] = ['status' => 'failed', 'error' => $e->getMessage()];
        }
        
        echo "\n";
    }
    
    private function testGoal18_2(): void
    {
        echo "🗺️  Testing Goal 18.2: Autonomous Navigation and Path Planning\n";
        echo "------------------------------------------------------------\n";
        
        try {
            $result = $this->agent->executeGoal18_2();
            
            $this->assert($result['success'], 'Goal 18.2 execution should succeed');
            $this->assert($result['maps_created'] >= 2, 'Should create at least 2 maps');
            $this->assert($result['obstacles_added'] >= 3, 'Should add at least 3 obstacles');
            $this->assert($result['paths_planned'] >= 2, 'Should plan at least 2 paths');
            $this->assert($result['navigations_executed'] >= 2, 'Should execute at least 2 navigations');
            $this->assert(isset($result['navigation_statistics']), 'Should return navigation statistics');
            
            echo "✅ Goal 18.2 Tests Passed\n";
            $this->testResults['goal_18_2'] = ['status' => 'passed', 'result' => $result];
            
        } catch (Exception $e) {
            echo "❌ Goal 18.2 Tests Failed: " . $e->getMessage() . "\n";
            $this->testResults['goal_18_2'] = ['status' => 'failed', 'error' => $e->getMessage()];
        }
        
        echo "\n";
    }
    
    private function testGoal18_3(): void
    {
        echo "🐝 Testing Goal 18.3: Multi-Agent Coordination and Swarm Intelligence\n";
        echo "------------------------------------------------------------------\n";
        
        try {
            $result = $this->agent->executeGoal18_3();
            
            $this->assert($result['success'], 'Goal 18.3 execution should succeed');
            $this->assert($result['swarms_created'] >= 2, 'Should create at least 2 swarms');
            $this->assert($result['agents_added'] >= 9, 'Should add at least 9 agents');
            $this->assert($result['tasks_assigned'] >= 2, 'Should assign at least 2 tasks');
            $this->assert($result['behaviors_executed'] >= 2, 'Should execute at least 2 behaviors');
            $this->assert($result['messages_sent'] >= 2, 'Should send at least 2 messages');
            $this->assert(isset($result['swarm_statistics']), 'Should return swarm statistics');
            
            echo "✅ Goal 18.3 Tests Passed\n";
            $this->testResults['goal_18_3'] = ['status' => 'passed', 'result' => $result];
            
        } catch (Exception $e) {
            echo "❌ Goal 18.3 Tests Failed: " . $e->getMessage() . "\n";
            $this->testResults['goal_18_3'] = ['status' => 'failed', 'error' => $e->getMessage()];
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
            $this->assert($result['goal_id'] === 'g18', 'Goal ID should be g18');
            $this->assert(isset($result['results']['goal_18_1']), 'Should include goal 18.1 results');
            $this->assert(isset($result['results']['goal_18_2']), 'Should include goal 18.2 results');
            $this->assert(isset($result['results']['goal_18_3']), 'Should include goal 18.3 results');
            
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
        
        // Test Robotic Control Manager
        try {
            $roboticManager = new RoboticControlManager();
            $robot = $roboticManager->registerRobot('test_robot', [
                'name' => 'Test Robot',
                'type' => 'mobile_robot'
            ]);
            $this->assert($robot['success'], 'Robot registration should succeed');
            
            $controller = $roboticManager->createController('test_controller', 'test_robot', [
                'name' => 'Test Controller',
                'type' => 'pid_controller'
            ]);
            $this->assert($controller['success'], 'Controller creation should succeed');
            
            $sensor = $roboticManager->addSensor('test_sensor', 'test_robot', [
                'name' => 'Test Sensor',
                'type' => 'camera'
            ]);
            $this->assert($sensor['success'], 'Sensor addition should succeed');
            
            $actuator = $roboticManager->addActuator('test_actuator', 'test_robot', [
                'name' => 'Test Actuator',
                'type' => 'servo_motor'
            ]);
            $this->assert($actuator['success'], 'Actuator addition should succeed');
            
            $command = $roboticManager->executeCommand('test_robot', [
                'type' => 'move',
                'position' => [1, 2, 3]
            ]);
            $this->assert($command['success'], 'Command execution should succeed');
            
            $stats = $roboticManager->getRoboticStats();
            $this->assert(isset($stats['total_robots']), 'Should return robotic statistics');
            
            echo "✅ Robotic Control Manager Tests Passed\n";
            $this->testResults['robotic_manager'] = ['status' => 'passed'];
            
        } catch (Exception $e) {
            echo "❌ Robotic Control Manager Tests Failed: " . $e->getMessage() . "\n";
            $this->testResults['robotic_manager'] = ['status' => 'failed', 'error' => $e->getMessage()];
        }
        
        // Test Autonomous Navigation Manager
        try {
            $navigationManager = new AutonomousNavigationManager();
            $map = $navigationManager->createMap('test_map', [
                'name' => 'Test Map',
                'type' => 'grid_map'
            ]);
            $this->assert($map['success'], 'Map creation should succeed');
            
            $obstacle = $navigationManager->addObstacle('test_map', 'test_obstacle', [
                'type' => 'static',
                'shape' => 'rectangle'
            ]);
            $this->assert($obstacle['success'], 'Obstacle addition should succeed');
            
            $path = $navigationManager->planPath('test_map', [0, 0, 0], [10, 10, 0], [
                'algorithm' => 'a_star'
            ]);
            $this->assert($path['success'], 'Path planning should succeed');
            
            $navigation = $navigationManager->executeNavigation($path['path']['id'], 'test_robot', [
                'max_speed' => 2.0
            ]);
            $this->assert($navigation['success'], 'Navigation execution should succeed');
            
            $stats = $navigationManager->getNavigationStats();
            $this->assert(isset($stats['total_maps']), 'Should return navigation statistics');
            
            echo "✅ Autonomous Navigation Manager Tests Passed\n";
            $this->testResults['navigation_manager'] = ['status' => 'passed'];
            
        } catch (Exception $e) {
            echo "❌ Autonomous Navigation Manager Tests Failed: " . $e->getMessage() . "\n";
            $this->testResults['navigation_manager'] = ['status' => 'failed', 'error' => $e->getMessage()];
        }
        
        // Test Swarm Intelligence Manager
        try {
            $swarmManager = new SwarmIntelligenceManager();
            $swarm = $swarmManager->createSwarm('test_swarm', [
                'name' => 'Test Swarm',
                'behavior' => 'flocking'
            ]);
            $this->assert($swarm['success'], 'Swarm creation should succeed');
            
            $agent = $swarmManager->addAgentToSwarm('test_swarm', 'test_agent', [
                'name' => 'Test Agent',
                'type' => 'drone'
            ]);
            $this->assert($agent['success'], 'Agent addition should succeed');
            
            $task = $swarmManager->assignTask('test_swarm', [
                'name' => 'Test Task',
                'type' => 'exploration'
            ]);
            $this->assert($task['success'], 'Task assignment should succeed');
            
            $behavior = $swarmManager->executeSwarmBehavior('test_swarm', [
                'formation_type' => 'line'
            ]);
            $this->assert($behavior['success'], 'Behavior execution should succeed');
            
            $message = $swarmManager->sendSwarmMessage('test_swarm', 'test_agent', [
                'type' => 'status',
                'content' => 'Test message'
            ], [
                'protocol' => 'broadcast'
            ]);
            $this->assert($message['success'], 'Message sending should succeed');
            
            $stats = $swarmManager->getSwarmStats();
            $this->assert(isset($stats['total_swarms']), 'Should return swarm statistics');
            
            echo "✅ Swarm Intelligence Manager Tests Passed\n";
            $this->testResults['swarm_manager'] = ['status' => 'passed'];
            
        } catch (Exception $e) {
            echo "❌ Swarm Intelligence Manager Tests Failed: " . $e->getMessage() . "\n";
            $this->testResults['swarm_manager'] = ['status' => 'failed', 'error' => $e->getMessage()];
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
    $test = new AgentA8G18Test();
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
        echo "\n🎉 All tests passed! Agent A8 G18 is working correctly.\n";
        exit(0);
    } else {
        echo "\n⚠️  Some tests failed. Please review the implementation.\n";
        exit(1);
    }
} 