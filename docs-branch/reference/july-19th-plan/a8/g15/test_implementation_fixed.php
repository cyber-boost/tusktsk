<?php
/**
 * 🥜 TuskLang PHP Agent A8 - Goal G15 Test Implementation (Fixed)
 * ================================================================
 * Agent: a8
 * Goals: g15.1, g15.2, g15.3
 * Language: PHP
 * 
 * This file tests the three goals for the PHP agent g15:
 * - g15.1: Edge AI Model Deployment and Management
 * - g15.2: Machine Learning Pipeline and Training
 * - g15.3: Real-time Inference and Edge Processing
 */

require_once 'implementation.php';

use TuskLang\AgentA8\G15\AgentA8G15;
use TuskLang\AgentA8\G15\EdgeAIModelManager;
use TuskLang\AgentA8\G15\MLPipelineManager;
use TuskLang\AgentA8\G15\RealTimeInferenceManager;

class AgentA8G15TestFixed
{
    private AgentA8G15 $agent;
    private array $testResults = [];
    
    public function __construct()
    {
        $this->agent = new AgentA8G15();
    }
    
    public function runAllTests(): array
    {
        echo "🧪 Starting Agent A8 G15 Tests (Fixed)...\n";
        echo "========================================\n\n";
        
        $this->testGoal15_1();
        $this->testGoal15_2();
        $this->testGoal15_3();
        $this->testIntegration();
        
        return $this->testResults;
    }
    
    private function testGoal15_1(): void
    {
        echo "🤖 Testing Goal 15.1: Edge AI Model Deployment and Management\n";
        echo "-----------------------------------------------------------\n";
        
        try {
            $result = $this->agent->executeGoal15_1();
            
            $this->assert($result['success'], 'Goal 15.1 execution should succeed');
            $this->assert($result['models_registered'] >= 2, 'Should register at least 2 models');
            $this->assert($result['models_optimized'] >= 2, 'Should optimize at least 2 models');
            $this->assert($result['models_deployed'] >= 2, 'Should deploy at least 2 models');
            $this->assert($result['devices_registered'] >= 2, 'Should register at least 2 devices');
            $this->assert(isset($result['edge_ai_statistics']), 'Should return edge AI statistics');
            
            echo "✅ Goal 15.1 Tests Passed\n";
            $this->testResults['goal_15_1'] = ['status' => 'passed', 'result' => $result];
            
        } catch (Exception $e) {
            echo "❌ Goal 15.1 Tests Failed: " . $e->getMessage() . "\n";
            $this->testResults['goal_15_1'] = ['status' => 'failed', 'error' => $e->getMessage()];
        }
        
        echo "\n";
    }
    
    private function testGoal15_2(): void
    {
        echo "🔬 Testing Goal 15.2: Machine Learning Pipeline and Training\n";
        echo "----------------------------------------------------------\n";
        
        try {
            $result = $this->agent->executeGoal15_2();
            
            $this->assert($result['success'], 'Goal 15.2 execution should succeed');
            $this->assert($result['pipelines_created'] >= 2, 'Should create at least 2 pipelines');
            $this->assert($result['datasets_registered'] >= 2, 'Should register at least 2 datasets');
            $this->assert($result['experiments_run'] >= 2, 'Should run at least 2 experiments');
            $this->assert(isset($result['pipeline_statistics']), 'Should return pipeline statistics');
            
            echo "✅ Goal 15.2 Tests Passed\n";
            $this->testResults['goal_15_2'] = ['status' => 'passed', 'result' => $result];
            
        } catch (Exception $e) {
            echo "❌ Goal 15.2 Tests Failed: " . $e->getMessage() . "\n";
            $this->testResults['goal_15_2'] = ['status' => 'failed', 'error' => $e->getMessage()];
        }
        
        echo "\n";
    }
    
    private function testGoal15_3(): void
    {
        echo "⚡ Testing Goal 15.3: Real-time Inference and Edge Processing\n";
        echo "-----------------------------------------------------------\n";
        
        try {
            $result = $this->agent->executeGoal15_3();
            
            $this->assert($result['success'], 'Goal 15.3 execution should succeed');
            $this->assert($result['engines_created'] >= 2, 'Should create at least 2 engines');
            $this->assert($result['inference_requests'] >= 2, 'Should process at least 2 inference requests');
            $this->assert($result['batch_processing_runs'] >= 1, 'Should run at least 1 batch processing');
            $this->assert(isset($result['inference_statistics']), 'Should return inference statistics');
            
            echo "✅ Goal 15.3 Tests Passed\n";
            $this->testResults['goal_15_3'] = ['status' => 'passed', 'result' => $result];
            
        } catch (Exception $e) {
            echo "❌ Goal 15.3 Tests Failed: " . $e->getMessage() . "\n";
            $this->testResults['goal_15_3'] = ['status' => 'failed', 'error' => $e->getMessage()];
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
            $this->assert($result['goal_id'] === 'g15', 'Goal ID should be g15');
            $this->assert(isset($result['results']['goal_15_1']), 'Should include goal 15.1 results');
            $this->assert(isset($result['results']['goal_15_2']), 'Should include goal 15.2 results');
            $this->assert(isset($result['results']['goal_15_3']), 'Should include goal 15.3 results');
            
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
        
        // Test Edge AI Model Manager
        try {
            $edgeAIManager = new EdgeAIModelManager();
            $model = $edgeAIManager->registerModel('test_model', [
                'name' => 'Test Model',
                'type' => 'image_classification',
                'framework' => 'tensorflow_lite',
                'file_size' => 10 * 1024 * 1024, // 10MB
                'latency' => 0.050 // 50ms
            ]);
            $this->assert($model['success'], 'Model registration should succeed');
            
            $optimization = $edgeAIManager->optimizeModel('test_model', [
                'type' => 'quantization'
            ]);
            $this->assert($optimization['success'], 'Model optimization should succeed');
            
            $deployment = $edgeAIManager->deployModel('test_model', 'edge_device', [
                'optimization_level' => 'high'
            ]);
            $this->assert($deployment['success'], 'Model deployment should succeed');
            
            $device = $edgeAIManager->registerDevice('test_device', [
                'name' => 'Test Device',
                'type' => 'edge_device'
            ]);
            $this->assert($device['success'], 'Device registration should succeed');
            
            $stats = $edgeAIManager->getEdgeAIStats();
            $this->assert(isset($stats['total_models']), 'Should return edge AI statistics');
            
            echo "✅ Edge AI Model Manager Tests Passed\n";
            $this->testResults['edge_ai_manager'] = ['status' => 'passed'];
            
        } catch (Exception $e) {
            echo "❌ Edge AI Model Manager Tests Failed: " . $e->getMessage() . "\n";
            $this->testResults['edge_ai_manager'] = ['status' => 'failed', 'error' => $e->getMessage()];
        }
        
        // Test ML Pipeline Manager
        try {
            $pipelineManager = new MLPipelineManager();
            $pipeline = $pipelineManager->createPipeline('test_pipeline', [
                'name' => 'Test Pipeline',
                'algorithm' => 'random_forest'
            ]);
            $this->assert($pipeline['success'], 'Pipeline creation should succeed');
            
            $dataset = $pipelineManager->registerDataset('test_dataset', [
                'name' => 'Test Dataset',
                'type' => 'tabular',
                'num_samples' => 1000
            ]);
            $this->assert($dataset['success'], 'Dataset registration should succeed');
            
            $experiment = $pipelineManager->runExperiment('test_pipeline', 'test_dataset', [
                'epochs' => 10
            ]);
            $this->assert($experiment['success'], 'Experiment execution should succeed');
            
            $stats = $pipelineManager->getPipelineStats();
            $this->assert(isset($stats['total_pipelines']), 'Should return pipeline statistics');
            
            echo "✅ ML Pipeline Manager Tests Passed\n";
            $this->testResults['pipeline_manager'] = ['status' => 'passed'];
            
        } catch (Exception $e) {
            echo "❌ ML Pipeline Manager Tests Failed: " . $e->getMessage() . "\n";
            $this->testResults['pipeline_manager'] = ['status' => 'failed', 'error' => $e->getMessage()];
        }
        
        // Test Real-time Inference Manager
        try {
            $inferenceManager = new RealTimeInferenceManager();
            $engine = $inferenceManager->createInferenceEngine('test_engine', [
                'name' => 'Test Engine',
                'type' => 'tensorflow_lite'
            ]);
            $this->assert($engine['success'], 'Engine creation should succeed');
            
            $inference = $inferenceManager->processInference('test_engine', [
                'input_data' => base64_encode('test_data')
            ]);
            $this->assert($inference['success'], 'Inference processing should succeed');
            
            $batchProcessing = $inferenceManager->batchProcess('test_engine', [
                'batch_1' => base64_encode('batch_data_1'),
                'batch_2' => base64_encode('batch_data_2')
            ]);
            $this->assert($batchProcessing['success'], 'Batch processing should succeed');
            
            $stats = $inferenceManager->getInferenceStats();
            $this->assert(isset($stats['total_engines']), 'Should return inference statistics');
            
            echo "✅ Real-time Inference Manager Tests Passed\n";
            $this->testResults['inference_manager'] = ['status' => 'passed'];
            
        } catch (Exception $e) {
            echo "❌ Real-time Inference Manager Tests Failed: " . $e->getMessage() . "\n";
            $this->testResults['inference_manager'] = ['status' => 'failed', 'error' => $e->getMessage()];
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
    $test = new AgentA8G15TestFixed();
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
        echo "\n🎉 All tests passed! Agent A8 G15 is working correctly.\n";
        exit(0);
    } else {
        echo "\n⚠️  Some tests failed. Please review the implementation.\n";
        exit(1);
    }
} 