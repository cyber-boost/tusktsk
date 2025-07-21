<?php
/**
 * 🥜 TuskLang PHP Agent A8 - Goal G14 Test Implementation
 * ========================================================
 * Agent: a8
 * Goals: g14.1, g14.2, g14.3
 * Language: PHP
 * 
 * This file tests the three goals for the PHP agent g14:
 * - g14.1: AR/VR Development Frameworks and SDKs
 * - g14.2: Spatial Computing and 3D Rendering
 * - g14.3: Immersive Experience Management
 */

require_once 'implementation.php';

use TuskLang\AgentA8\G14\AgentA8G14;
use TuskLang\AgentA8\G14\ARVRFrameworkManager;
use TuskLang\AgentA8\G14\SpatialComputingManager;
use TuskLang\AgentA8\G14\ImmersiveExperienceManager;

class AgentA8G14Test
{
    private AgentA8G14 $agent;
    private array $testResults = [];
    
    public function __construct()
    {
        $this->agent = new AgentA8G14();
    }
    
    public function runAllTests(): array
    {
        echo "🧪 Starting Agent A8 G14 Tests...\n";
        echo "================================\n\n";
        
        $this->testGoal14_1();
        $this->testGoal14_2();
        $this->testGoal14_3();
        $this->testIntegration();
        
        return $this->testResults;
    }
    
    private function testGoal14_1(): void
    {
        echo "🥽 Testing Goal 14.1: AR/VR Development Frameworks and SDKs\n";
        echo "---------------------------------------------------------\n";
        
        try {
            $result = $this->agent->executeGoal14_1();
            
            $this->assert($result['success'], 'Goal 14.1 execution should succeed');
            $this->assert($result['frameworks_registered'] >= 2, 'Should register at least 2 frameworks');
            $this->assert($result['sdks_registered'] >= 2, 'Should register at least 2 SDKs');
            $this->assert($result['devices_registered'] >= 2, 'Should register at least 2 devices');
            $this->assert($result['projects_created'] >= 2, 'Should create at least 2 projects');
            $this->assert(isset($result['framework_statistics']), 'Should return framework statistics');
            
            echo "✅ Goal 14.1 Tests Passed\n";
            $this->testResults['goal_14_1'] = ['status' => 'passed', 'result' => $result];
            
        } catch (Exception $e) {
            echo "❌ Goal 14.1 Tests Failed: " . $e->getMessage() . "\n";
            $this->testResults['goal_14_1'] = ['status' => 'failed', 'error' => $e->getMessage()];
        }
        
        echo "\n";
    }
    
    private function testGoal14_2(): void
    {
        echo "🌐 Testing Goal 14.2: Spatial Computing and 3D Rendering\n";
        echo "------------------------------------------------------\n";
        
        try {
            $result = $this->agent->executeGoal14_2();
            
            $this->assert($result['success'], 'Goal 14.2 execution should succeed');
            $this->assert($result['scenes_created'] >= 2, 'Should create at least 2 scenes');
            $this->assert($result['objects_added'] >= 3, 'Should add at least 3 objects');
            $this->assert($result['cameras_added'] >= 2, 'Should add at least 2 cameras');
            $this->assert($result['lights_added'] >= 2, 'Should add at least 2 lights');
            $this->assert($result['scenes_rendered'] >= 2, 'Should render at least 2 scenes');
            $this->assert(isset($result['spatial_statistics']), 'Should return spatial statistics');
            
            echo "✅ Goal 14.2 Tests Passed\n";
            $this->testResults['goal_14_2'] = ['status' => 'passed', 'result' => $result];
            
        } catch (Exception $e) {
            echo "❌ Goal 14.2 Tests Failed: " . $e->getMessage() . "\n";
            $this->testResults['goal_14_2'] = ['status' => 'failed', 'error' => $e->getMessage()];
        }
        
        echo "\n";
    }
    
    private function testGoal14_3(): void
    {
        echo "🎮 Testing Goal 14.3: Immersive Experience Management\n";
        echo "---------------------------------------------------\n";
        
        try {
            $result = $this->agent->executeGoal14_3();
            
            $this->assert($result['success'], 'Goal 14.3 execution should succeed');
            $this->assert($result['experiences_created'] >= 2, 'Should create at least 2 experiences');
            $this->assert($result['users_registered'] >= 2, 'Should register at least 2 users');
            $this->assert($result['sessions_started'] >= 2, 'Should start at least 2 sessions');
            $this->assert($result['interactions_recorded'] >= 3, 'Should record at least 3 interactions');
            $this->assert($result['sessions_completed'] >= 2, 'Should complete at least 2 sessions');
            $this->assert($result['analytics_generated'] >= 2, 'Should generate at least 2 analytics');
            $this->assert(isset($result['immersive_statistics']), 'Should return immersive statistics');
            
            echo "✅ Goal 14.3 Tests Passed\n";
            $this->testResults['goal_14_3'] = ['status' => 'passed', 'result' => $result];
            
        } catch (Exception $e) {
            echo "❌ Goal 14.3 Tests Failed: " . $e->getMessage() . "\n";
            $this->testResults['goal_14_3'] = ['status' => 'failed', 'error' => $e->getMessage()];
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
            $this->assert($result['goal_id'] === 'g14', 'Goal ID should be g14');
            $this->assert(isset($result['results']['goal_14_1']), 'Should include goal 14.1 results');
            $this->assert(isset($result['results']['goal_14_2']), 'Should include goal 14.2 results');
            $this->assert(isset($result['results']['goal_14_3']), 'Should include goal 14.3 results');
            
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
        
        // Test AR/VR Framework Manager
        try {
            $frameworkManager = new ARVRFrameworkManager();
            $framework = $frameworkManager->registerFramework('test_framework', [
                'name' => 'Test Framework',
                'type' => 'custom',
                'platforms' => ['Test Platform']
            ]);
            $this->assert($framework['success'], 'Framework registration should succeed');
            
            $sdk = $frameworkManager->registerSDK('test_sdk', [
                'name' => 'Test SDK',
                'vendor' => 'Test Vendor',
                'platforms' => ['Test Platform']
            ]);
            $this->assert($sdk['success'], 'SDK registration should succeed');
            
            $device = $frameworkManager->registerDevice('test_device', [
                'name' => 'Test Device',
                'type' => 'test_type',
                'manufacturer' => 'Test Manufacturer'
            ]);
            $this->assert($device['success'], 'Device registration should succeed');
            
            $project = $frameworkManager->createProject('test_project', 'test_framework', [
                'name' => 'Test Project'
            ]);
            $this->assert($project['success'], 'Project creation should succeed');
            
            $stats = $frameworkManager->getFrameworkStats();
            $this->assert(isset($stats['total_frameworks']), 'Should return framework statistics');
            
            echo "✅ AR/VR Framework Manager Tests Passed\n";
            $this->testResults['framework_manager'] = ['status' => 'passed'];
            
        } catch (Exception $e) {
            echo "❌ AR/VR Framework Manager Tests Failed: " . $e->getMessage() . "\n";
            $this->testResults['framework_manager'] = ['status' => 'failed', 'error' => $e->getMessage()];
        }
        
        // Test Spatial Computing Manager
        try {
            $spatialManager = new SpatialComputingManager();
            $scene = $spatialManager->createScene('test_scene', [
                'name' => 'Test Scene'
            ]);
            $this->assert($scene['success'], 'Scene creation should succeed');
            
            $object = $spatialManager->addObject('test_scene', 'test_object', [
                'name' => 'Test Object',
                'type' => 'cube'
            ]);
            $this->assert($object['success'], 'Object addition should succeed');
            
            $camera = $spatialManager->addCamera('test_scene', 'test_camera', [
                'name' => 'Test Camera',
                'type' => 'perspective'
            ]);
            $this->assert($camera['success'], 'Camera addition should succeed');
            
            $light = $spatialManager->addLight('test_scene', 'test_light', [
                'name' => 'Test Light',
                'type' => 'point'
            ]);
            $this->assert($light['success'], 'Light addition should succeed');
            
            $render = $spatialManager->renderScene('test_scene', 'test_camera', [
                'resolution' => [1920, 1080]
            ]);
            $this->assert($render['success'], 'Scene rendering should succeed');
            
            $stats = $spatialManager->getSpatialStats();
            $this->assert(isset($stats['total_scenes']), 'Should return spatial statistics');
            
            echo "✅ Spatial Computing Manager Tests Passed\n";
            $this->testResults['spatial_manager'] = ['status' => 'passed'];
            
        } catch (Exception $e) {
            echo "❌ Spatial Computing Manager Tests Failed: " . $e->getMessage() . "\n";
            $this->testResults['spatial_manager'] = ['status' => 'failed', 'error' => $e->getMessage()];
        }
        
        // Test Immersive Experience Manager
        try {
            $experienceManager = new ImmersiveExperienceManager();
            $experience = $experienceManager->createExperience('test_experience', [
                'name' => 'Test Experience',
                'type' => 'virtual_reality'
            ]);
            $this->assert($experience['success'], 'Experience creation should succeed');
            
            $user = $experienceManager->registerUser('test_user', [
                'name' => 'Test User',
                'device_type' => 'vr_headset'
            ]);
            $this->assert($user['success'], 'User registration should succeed');
            
            $session = $experienceManager->startSession('test_experience', 'test_user', [
                'device' => 'test_device'
            ]);
            $this->assert($session['success'], 'Session start should succeed');
            
            $interaction = $experienceManager->recordInteraction($session['session']['id'], [
                'type' => 'hand_controller',
                'target' => 'test_target',
                'position' => [0, 0, 0]
            ]);
            $this->assert($interaction['success'], 'Interaction recording should succeed');
            
            $endedSession = $experienceManager->endSession($session['session']['id']);
            $this->assert($endedSession['success'], 'Session end should succeed');
            
            $analytics = $experienceManager->getExperienceAnalytics('test_experience');
            $this->assert($analytics['success'], 'Analytics generation should succeed');
            
            $stats = $experienceManager->getImmersiveStats();
            $this->assert(isset($stats['total_experiences']), 'Should return immersive statistics');
            
            echo "✅ Immersive Experience Manager Tests Passed\n";
            $this->testResults['experience_manager'] = ['status' => 'passed'];
            
        } catch (Exception $e) {
            echo "❌ Immersive Experience Manager Tests Failed: " . $e->getMessage() . "\n";
            $this->testResults['experience_manager'] = ['status' => 'failed', 'error' => $e->getMessage()];
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
    $test = new AgentA8G14Test();
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
        echo "\n🎉 All tests passed! Agent A8 G14 is working correctly.\n";
        exit(0);
    } else {
        echo "\n⚠️  Some tests failed. Please review the implementation.\n";
        exit(1);
    }
} 