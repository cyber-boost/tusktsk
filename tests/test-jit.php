<?php
/**
 * Simple JIT Test Script
 */

require_once 'fujsen/src/autoload.php';

echo "🚀 TuskLang JIT Optimizer Test\n";
echo "================================\n\n";

// Test 1: Check if JIT Optimizer is available
echo "1. Checking JIT Optimizer availability...\n";
if (class_exists('TuskPHP\\Utils\\TuskLangJITOptimizer')) {
    echo "   ✅ TuskLangJITOptimizer class loaded\n";
    
    try {
        $jitOptimizer = \TuskPHP\Utils\TuskLangJITOptimizer::getInstance();
        echo "   ✅ JIT Optimizer instance created\n";
        
        // Get JIT stats
        $stats = $jitOptimizer->getJITStats();
        echo "   📊 JIT Status:\n";
        echo "      - JIT Enabled: " . ($stats['jit_enabled'] ? 'Yes' : 'No') . "\n";
        echo "      - JIT Mode: " . ($stats['jit_mode'] ?: 'Not set') . "\n";
        echo "      - JIT Buffer: " . ($stats['jit_buffer_size'] ?: 'Not set') . "\n";
        
    } catch (Exception $e) {
        echo "   ❌ Error creating JIT Optimizer: " . $e->getMessage() . "\n";
    }
} else {
    echo "   ❌ TuskLangJITOptimizer class not found\n";
}

echo "\n2. Testing basic TuskLang parsing...\n";

// Test 2: Basic TuskLang parsing
$testCode = 'name: "TuskLang JIT Test"
version: "1.0.0"
enabled: true
numbers: [1, 2, 3, 4, 5]';

try {
    $result = \TuskPHP\Utils\TuskLang::parse($testCode);
    echo "   ✅ Basic parsing successful\n";
    echo "   📄 Result: " . json_encode($result, JSON_PRETTY_PRINT) . "\n";
} catch (Exception $e) {
    echo "   ❌ Parsing error: " . $e->getMessage() . "\n";
}

echo "\n3. Testing JIT compilation...\n";

// Test 3: JIT compilation
if (class_exists('TuskPHP\\Utils\\TuskLangJITOptimizer')) {
    try {
        $jitOptimizer = \TuskPHP\Utils\TuskLangJITOptimizer::getInstance();
        
        $optimizedCode = $jitOptimizer->compileWithJIT($testCode);
        echo "   ✅ JIT compilation completed\n";
        echo "   📝 Optimized code length: " . strlen($optimizedCode) . " characters\n";
        
        // Show first 200 characters of optimized code
        if (strlen($optimizedCode) > 200) {
            echo "   📄 Preview: " . substr($optimizedCode, 0, 200) . "...\n";
        } else {
            echo "   📄 Optimized code: " . $optimizedCode . "\n";
        }
        
    } catch (Exception $e) {
        echo "   ❌ JIT compilation error: " . $e->getMessage() . "\n";
    }
}

echo "\n4. PHP and OPcache information...\n";

// Test 4: PHP and OPcache info
echo "   🐘 PHP Version: " . PHP_VERSION . "\n";
echo "   📦 OPcache loaded: " . (extension_loaded('Zend OPcache') ? 'Yes' : 'No') . "\n";

if (function_exists('opcache_get_status')) {
    $opcacheStatus = opcache_get_status();
    if ($opcacheStatus) {
        echo "   🚀 OPcache enabled: Yes\n";
        echo "   🎯 JIT enabled: " . (isset($opcacheStatus['jit']['enabled']) && $opcacheStatus['jit']['enabled'] ? 'Yes' : 'No') . "\n";
        
        if (isset($opcacheStatus['jit'])) {
            echo "   💾 JIT buffer size: " . ($opcacheStatus['jit']['buffer_size'] ?? 'Unknown') . "\n";
            echo "   📈 JIT buffer free: " . ($opcacheStatus['jit']['buffer_free'] ?? 'Unknown') . "\n";
        }
    } else {
        echo "   ❌ OPcache not active\n";
    }
} else {
    echo "   ❌ opcache_get_status() not available\n";
}

echo "\n5. Current configuration...\n";
echo "   opcache.jit: " . ini_get('opcache.jit') . "\n";
echo "   opcache.jit_buffer_size: " . ini_get('opcache.jit_buffer_size') . "\n";
echo "   opcache.jit_hot_func: " . ini_get('opcache.jit_hot_func') . "\n";
echo "   opcache.jit_hot_loop: " . ini_get('opcache.jit_hot_loop') . "\n";

echo "\n🎉 JIT Test completed!\n"; 