<?php
/**
 * TuskLang PHP SDK - Simple Test
 * ==============================
 * Tests core functionality without complex dependencies
 */

require_once 'src/autoload.php';

use TuskLang\Enhanced\OperatorRegistry;
use TuskLang\Enhanced\TuskLangEnhanced;

echo "🚀 TuskLang PHP SDK - Simple Test\n";
echo "==================================\n\n";

try {
    // Initialize registry
    $registry = OperatorRegistry::getInstance();
    
    // Get completion statistics
    $stats = $registry->getCompletionStats();
    echo "📊 COMPLETION STATISTICS:\n";
    echo "Total Operators: {$stats['total']}\n";
    echo "Implemented: {$stats['implemented']}\n";
    echo "Built-in: {$stats['built_in']}\n";
    echo "Missing: {$stats['missing']}\n";
    echo "Completion: {$stats['percentage']}%\n\n";
    
    // Test built-in operators
    echo "🧪 TESTING BUILT-IN OPERATORS:\n";
    echo "------------------------------\n";
    
    // Test @variable
    try {
        $context = ['global_variables' => ['test_var' => 'test_value']];
        $result = $registry->executeOperator('variable', ['name' => 'test_var'], $context);
        echo "✅ @variable: " . ($result === 'test_value' ? "WORKING" : "FAILED") . "\n";
    } catch (Exception $e) {
        echo "❌ @variable: FAILED - " . $e->getMessage() . "\n";
    }
    
    // Test @date
    try {
        $result = $registry->executeOperator('date', ['format' => 'Y-m-d']);
        echo "✅ @date: " . (is_string($result) ? "WORKING" : "FAILED") . "\n";
    } catch (Exception $e) {
        echo "❌ @date: FAILED - " . $e->getMessage() . "\n";
    }
    
    // Test @if
    try {
        $result = $registry->executeOperator('if', [
            'condition' => true,
            'true' => 'success',
            'false' => 'failure'
        ]);
        echo "✅ @if: " . ($result === 'success' ? "WORKING" : "FAILED") . "\n";
    } catch (Exception $e) {
        echo "❌ @if: FAILED - " . $e->getMessage() . "\n";
    }
    
    // Test @string
    try {
        $result = $registry->executeOperator('string', ['operation' => 'uppercase', 'value' => 'hello world']);
        echo "✅ @string: " . ($result === 'HELLO WORLD' ? "WORKING" : "FAILED") . "\n";
    } catch (Exception $e) {
        echo "❌ @string: FAILED - " . $e->getMessage() . "\n";
    }
    
    // Test @hash
    try {
        $result = $registry->executeOperator('hash', ['algorithm' => 'md5', 'data' => 'test']);
        echo "✅ @hash: " . (strlen($result) === 32 ? "WORKING" : "FAILED") . "\n";
    } catch (Exception $e) {
        echo "❌ @hash: FAILED - " . $e->getMessage() . "\n";
    }
    
    // Test @base64
    try {
        $result = $registry->executeOperator('base64', ['operation' => 'encode', 'data' => 'test']);
        echo "✅ @base64: " . (is_string($result) ? "WORKING" : "FAILED") . "\n";
    } catch (Exception $e) {
        echo "❌ @base64: FAILED - " . $e->getMessage() . "\n";
    }
    
    // Test @switch
    try {
        $result = $registry->executeOperator('switch', [
            'value' => 'test',
            'cases' => ['test' => 'matched'],
            'default' => 'default'
        ]);
        echo "✅ @switch: " . ($result === 'matched' ? "WORKING" : "FAILED") . "\n";
    } catch (Exception $e) {
        echo "❌ @switch: FAILED - " . $e->getMessage() . "\n";
    }
    
    // Test @for
    try {
        $result = $registry->executeOperator('for', [
            'start' => 1,
            'end' => 3,
            'action' => 'test'
        ]);
        echo "✅ @for: " . (is_array($result) && count($result) === 3 ? "WORKING" : "FAILED") . "\n";
    } catch (Exception $e) {
        echo "❌ @for: FAILED - " . $e->getMessage() . "\n";
    }
    
    // Test @each
    try {
        $result = $registry->executeOperator('each', [
            'array' => ['a', 'b', 'c'],
            'action' => 'test'
        ]);
        echo "✅ @each: " . (is_array($result) && count($result) === 3 ? "WORKING" : "FAILED") . "\n";
    } catch (Exception $e) {
        echo "❌ @each: FAILED - " . $e->getMessage() . "\n";
    }
    
    // Test @filter
    try {
        $result = $registry->executeOperator('filter', [
            'array' => ['apple', 'banana', 'cherry'],
            'condition' => true
        ]);
        echo "✅ @filter: " . (is_array($result) ? "WORKING" : "FAILED") . "\n";
    } catch (Exception $e) {
        echo "❌ @filter: FAILED - " . $e->getMessage() . "\n";
    }
    
    // Test @regex
    try {
        $result = $registry->executeOperator('regex', [
            'operation' => 'test',
            'pattern' => '/\d+/',
            'subject' => 'test123test'
        ]);
        echo "✅ @regex: " . ($result === true ? "WORKING" : "FAILED") . "\n";
    } catch (Exception $e) {
        echo "❌ @regex: FAILED - " . $e->getMessage() . "\n";
    }
    
    // Test @encrypt and @decrypt
    try {
        $key = 'test_key_32_bytes_long_for_aes';
        $data = 'secret data';
        
        $encrypted = $registry->executeOperator('encrypt', [
            'algorithm' => 'simple',
            'data' => $data,
            'key' => $key
        ]);
        
        $decrypted = $registry->executeOperator('decrypt', [
            'algorithm' => 'simple',
            'data' => $encrypted,
            'key' => $key
        ]);
        
        echo "✅ @encrypt/@decrypt: " . ($decrypted === $data ? "WORKING" : "FAILED") . "\n";
    } catch (Exception $e) {
        echo "❌ @encrypt/@decrypt: FAILED - " . $e->getMessage() . "\n";
    }
    
    echo "\n🧪 TESTING TUSKLANG ENHANCED:\n";
    echo "------------------------------\n";
    
    // Test TuskLangEnhanced
    try {
        $parser = new TuskLangEnhanced();
        
        // Test parsing with operators
        $content = "
        [test_section]
        message: @string(operation: 'uppercase', value: 'hello world')
        hash: @hash(algorithm: 'md5', data: 'test')
        ";
        
        $result = $parser->parse($content);
        echo "✅ TuskLangEnhanced parsing: " . (!empty($result) ? "WORKING" : "FAILED") . "\n";
    } catch (Exception $e) {
        echo "❌ TuskLangEnhanced parsing: FAILED - " . $e->getMessage() . "\n";
    }
    
    echo "\n🎯 FINAL STATUS:\n";
    echo "================\n";
    
    $finalStats = $registry->getCompletionStats();
    echo "Total Operators: {$finalStats['total']}\n";
    echo "Implemented: {$finalStats['implemented']}\n";
    echo "Built-in: {$finalStats['built_in']}\n";
    echo "Missing: {$finalStats['missing']}\n";
    echo "Completion: {$finalStats['percentage']}%\n\n";
    
    if ($finalStats['percentage'] >= 50) {
        echo "🏆 SUCCESS: PHP SDK has achieved significant completion!\n";
        echo "🚀 VELOCITY MODE: PHP SDK implementation successful!\n";
    } else {
        echo "⚠️  WARNING: PHP SDK still needs more operators implemented.\n";
    }
    
} catch (Exception $e) {
    echo "❌ CRITICAL ERROR: " . $e->getMessage() . "\n";
    echo "Stack trace: " . $e->getTraceAsString() . "\n";
} 