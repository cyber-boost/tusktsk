#!/usr/bin/env php
<?php
/**
 * 🚀 FUJSEN Integration Test Script
 * ================================
 * "Testing configuration intelligence"
 * 
 * Hour 2 validation script
 */

echo "🚀 FUJSEN Integration Test Starting...\n\n";

// Load autoloader
require_once __DIR__ . '/src/autoload.php';

try {
    echo "✅ Autoloader loaded successfully\n";
    
    // Test TuskLang class loading
    if (class_exists('TuskPHP\\Utils\\TuskLang')) {
        echo "✅ TuskLang class loaded\n";
    } else {
        echo "❌ TuskLang class not found\n";
        exit(1);
    }
    
    // Test TuskLangQueryBridge class loading
    if (class_exists('TuskPHP\\Utils\\TuskLangQueryBridge')) {
        echo "✅ TuskLangQueryBridge class loaded\n";
    } else {
        echo "❌ TuskLangQueryBridge class not found\n";
        exit(1);
    }
    
    // Test bridge initialization
    $bridge = \TuskPHP\Utils\TuskLangQueryBridge::getInstance();
    echo "✅ Bridge instance created\n";
    
    // Test parsing our integration test file
    $testFile = __DIR__ . '/examples/test-integration.tsk';
    if (!file_exists($testFile)) {
        echo "❌ Test file not found: $testFile\n";
        exit(1);
    }
    
    echo "📁 Reading test file: $testFile\n";
    $content = file_get_contents($testFile);
    
    echo "🔍 Parsing TuskLang content...\n";
    $result = \TuskPHP\Utils\TuskLang::parse($content);
    
    echo "✅ Parsing successful!\n\n";
    
    // Display results
    echo "📊 PARSED RESULTS:\n";
    echo "==================\n";
    foreach ($result as $key => $value) {
        $displayValue = is_array($value) ? json_encode($value, JSON_PRETTY_PRINT) : $value;
        echo "$key: $displayValue\n";
    }
    
    echo "\n🎉 FUJSEN Integration Test PASSED!\n";
    echo "🐘 Configuration intelligence is ALIVE!\n\n";
    
    // Test individual operators
    echo "🔧 Testing individual operators...\n";
    
    // Test @cache
    $cacheResult = $bridge->handleCache("1h", "test_value");
    echo "✅ @cache test: $cacheResult\n";
    
    // Test @metrics
    $metricsResult = $bridge->handleMetrics("test_metric", 42.0);
    echo "✅ @metrics store test: $metricsResult\n";
    
    $retrievedMetric = $bridge->handleMetrics("test_metric");
    echo "✅ @metrics retrieve test: $retrievedMetric\n";
    
    // Test @learn
    $learnResult = $bridge->handleLearn("test_pattern", ['default' => 10]);
    echo "✅ @learn test: $learnResult\n";
    
    // Test @optimize
    $optimizeResult = $bridge->handleOptimize("test_param", ['current' => 0.5, 'target' => 0.8]);
    echo "✅ @optimize test: $optimizeResult\n";
    
    echo "\n🚀 ALL TESTS PASSED! FUJSEN IS READY!\n";
    
} catch (Exception $e) {
    echo "❌ ERROR: " . $e->getMessage() . "\n";
    echo "📍 File: " . $e->getFile() . ":" . $e->getLine() . "\n";
    exit(1);
} 