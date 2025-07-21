<?php
require_once 'implementation.php';

use TuskLang\AgentA8\G15\EdgeAIModelManager;
use TuskLang\AgentA8\G15\MLPipelineManager;
use TuskLang\AgentA8\G15\RealTimeInferenceManager;

echo "Testing Edge AI Model Manager...\n";
$edgeAIManager = new EdgeAIModelManager();

// Test model registration
$model = $edgeAIManager->registerModel('test_model', [
    'name' => 'Test Model',
    'type' => 'image_classification',
    'framework' => 'tensorflow_lite',
    'file_size' => 10 * 1024 * 1024, // 10MB
    'latency' => 0.050
]);

echo "Model registration: " . ($model['success'] ? 'SUCCESS' : 'FAILED') . "\n";

// Test model optimization
$optimization = $edgeAIManager->optimizeModel('test_model', [
    'type' => 'quantization'
]);

echo "Model optimization: " . ($optimization['success'] ? 'SUCCESS' : 'FAILED') . "\n";

echo "Testing ML Pipeline Manager...\n";
$pipelineManager = new MLPipelineManager();

// Test pipeline creation
$pipeline = $pipelineManager->createPipeline('test_pipeline', [
    'name' => 'Test Pipeline',
    'algorithm' => 'random_forest'
]);

echo "Pipeline creation: " . ($pipeline['success'] ? 'SUCCESS' : 'FAILED') . "\n";

// Test dataset registration
$dataset = $pipelineManager->registerDataset('test_dataset', [
    'name' => 'Test Dataset',
    'type' => 'tabular',
    'num_samples' => 1000
]);

echo "Dataset registration: " . ($dataset['success'] ? 'SUCCESS' : 'FAILED') . "\n";

// Test experiment execution
$experiment = $pipelineManager->runExperiment('test_pipeline', 'test_dataset', [
    'epochs' => 10
]);

echo "Experiment execution: " . ($experiment['success'] ? 'SUCCESS' : 'FAILED') . "\n";

echo "Testing Real-time Inference Manager...\n";
$inferenceManager = new RealTimeInferenceManager();

// Test engine creation
$engine = $inferenceManager->createInferenceEngine('test_engine', [
    'name' => 'Test Engine',
    'type' => 'tensorflow_lite'
]);

echo "Engine creation: " . ($engine['success'] ? 'SUCCESS' : 'FAILED') . "\n";

// Test inference processing
$inference = $inferenceManager->processInference('test_engine', [
    'input_data' => base64_encode('test_data')
]);

echo "Inference processing: " . ($inference['success'] ? 'SUCCESS' : 'FAILED') . "\n";

echo "All tests completed!\n"; 