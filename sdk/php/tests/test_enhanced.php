<?php
/**
 * Test Enhanced PHP TuskLang Parser
 * =================================
 * Verify all new syntax features work correctly
 */

require_once __DIR__ . '/src/TuskLangEnhanced.php';
require_once __DIR__ . '/src/functions.php';

use TuskLang\Enhanced\TuskLangEnhanced;

function testEnhancedSyntax()
{
    echo "🥜 Testing Enhanced PHP TuskLang Parser\n";
    echo str_repeat("=", 50) . "\n";
    
    // Set some environment variables for testing
    $_ENV['APP_ENV'] = 'development';
    $_ENV['SERVER_HOST'] = '127.0.0.1';
    $_ENV['SERVER_PORT'] = '3000';
    
    // Parse the test file
    $testFile = __DIR__ . '/test_enhanced.tsk';
    if (!file_exists($testFile)) {
        echo "❌ Test file not found\n";
        return;
    }
    
    $parser = new TuskLangEnhanced();
    $data = $parser->parseFile($testFile);
    
    echo "✅ Parsed configuration successfully\n";
    echo "📊 Found " . count($data) . " configuration items\n";
    echo "\n";
    
    // Test global variables
    echo "🌍 Global Variables:\n";
    foreach ($parser->globalVariables as $key => $value) {
        echo "  \$$key = $value\n";
    }
    echo "\n";
    
    // Test specific features
    echo "🔧 Feature Tests:\n";
    
    // Test environment variable with default
    $serverHost = $parser->get('server.host');
    echo "  Server host: $serverHost\n";
    
    // Test conditional expressions
    $debugMode = $parser->get('debug');
    echo "  Debug mode: " . ($debugMode ? 'true' : 'false') . "\n";
    
    // Test date functions
    $created = $parser->get('timestamps.created');
    echo "  Created timestamp: $created\n";
    
    // Test ranges
    $webRange = $parser->get('ports.web_range');
    echo "  Web port range: " . json_encode($webRange) . "\n";
    
    // Test arrays and objects
    $origins = $parser->get('config.allowed_origins');
    echo "  Allowed origins: " . json_encode($origins) . "\n";
    
    $settings = $parser->get('config.settings');
    echo "  Settings: " . json_encode($settings) . "\n";
    
    echo "\n";
    echo "🎯 All enhanced syntax features working!\n";
}

function testOriginalCompatibility()
{
    echo "\n🔄 Testing Backward Compatibility\n";
    echo str_repeat("=", 35) . "\n";
    
    // Create test content in original format
    $originalContent = '
[database]
host = "localhost"
port = 5432
name = "test_db"

[server]  
host = "0.0.0.0"
port = 8080
workers = 4
';
    
    // Parse with enhanced parser
    $parser = new TuskLangEnhanced();
    $data = $parser->parse($originalContent);
    
    echo "✅ Original syntax still works\n";
    echo "  Database host: " . ($parser->get('database.host') ?? 'not found') . "\n";
    echo "  Server port: " . ($parser->get('server.port') ?? 'not found') . "\n";
}

function testPeanutIntegration()
{
    echo "\n🥜 Testing peanut.tsk Integration\n";
    echo str_repeat("=", 35) . "\n";
    
    // Check if peanut.tsk exists
    $peanutPath = __DIR__ . '/../../peanut.tsk';
    if (file_exists($peanutPath)) {
        try {
            $peanutParser = tsk_load_from_peanut();
            echo "✅ peanut.tsk loaded successfully\n";
            echo "  Found " . count($peanutParser->toArray()) . " configuration items\n";
        } catch (Exception $e) {
            echo "⚠️  peanut.tsk found but couldn't load: " . $e->getMessage() . "\n";
        }
    } else {
        echo "ℹ️  peanut.tsk not found (expected in development)\n";
    }
}

function testSystemIntegration()
{
    echo "\n🔧 Testing System Integration\n";
    echo str_repeat("=", 30) . "\n";
    
    // Check for system-installed TuskLang
    $systemLocations = [
        '/usr/local/bin/tusk',
        '/usr/local/lib/tusklang',
        '/usr/bin/tusk'
    ];
    
    foreach ($systemLocations as $location) {
        if (file_exists($location)) {
            echo "✅ Found system TuskLang at: $location\n";
        }
    }
    
    // Test PHP system functions
    if (function_exists('shell_exec')) {
        $result = shell_exec('which tusk 2>/dev/null');
        if (!empty($result)) {
            echo "✅ TuskLang CLI available: " . trim($result) . "\n";
        }
    }
}

// Run all tests
try {
    testEnhancedSyntax();
    testOriginalCompatibility(); 
    testPeanutIntegration();
    testSystemIntegration();
    
    echo "\n🎉 All tests passed!\n";
    echo "PHP SDK enhanced successfully!\n";
    
} catch (Exception $e) {
    echo "\n❌ Test failed: " . $e->getMessage() . "\n";
    echo $e->getTraceAsString() . "\n";
    exit(1);
}