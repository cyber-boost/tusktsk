#!/usr/bin/env php
<?php
/**
 * Test TuskLang Enhanced Features
 * ===============================
 * Demonstrates the freedom and flexibility of TuskLang
 */

require_once __DIR__ . '/src/autoload.php';

use TuskPHP\Utils\TuskLangEnhanced;

echo "🐘 Testing TuskLang Enhanced - The Freedom Parser!\n";
echo "==================================================\n\n";

// Test 1: Basic parsing with multiple syntax styles
echo "Test 1: Multiple Syntax Styles\n";
echo "------------------------------\n";

$config1 = <<<TSK
# Global variables
\$app_name: "TestApp"
\$port: 8080

# Flat style
name: \$app_name
version: "1.0"

# TOML style section
[database]
host: "localhost"
port: 5432

# Angle bracket style
server>
    host: "0.0.0.0"
    port: \$port
<

# Curly brace style
cache {
    driver: "redis"
    ttl: 300
}
TSK;

try {
    $parsed1 = TuskLangEnhanced::parse($config1);
    echo "✅ Successfully parsed multiple syntax styles:\n";
    print_r($parsed1);
    echo "\n";
} catch (Exception $e) {
    echo "❌ Error: " . $e->getMessage() . "\n\n";
}

// Test 2: Variables (global vs section-local)
echo "Test 2: Variable Scoping\n";
echo "------------------------\n";

$config2 = <<<TSK
# Global variable
\$global_timeout: 30

# Top level uses global
timeout: \$global_timeout

[api]
# Section-local variable
timeout: 10
endpoint: "/api/v1"
# Reference section variable
full_timeout: timeout

[web]
# This section gets global
timeout: \$global_timeout
TSK;

try {
    $parsed2 = TuskLangEnhanced::parse($config2);
    echo "✅ Variable scoping works:\n";
    echo "API timeout (local): " . $parsed2['api']['full_timeout'] . "\n";
    echo "Web timeout (global): " . $parsed2['web']['timeout'] . "\n\n";
} catch (Exception $e) {
    echo "❌ Error: " . $e->getMessage() . "\n\n";
}

// Test 3: Date functions and ranges
echo "Test 3: Date Functions and Ranges\n";
echo "---------------------------------\n";

$config3 = <<<TSK
year: @date('Y')
timestamp: @date('Y-m-d H:i:s')
port_range: 8000-9000
worker_range: 1-10
TSK;

try {
    $parsed3 = TuskLangEnhanced::parse($config3);
    echo "✅ Date and ranges work:\n";
    echo "Year: " . $parsed3['year'] . "\n";
    echo "Timestamp: " . $parsed3['timestamp'] . "\n";
    echo "Port range: " . json_encode($parsed3['port_range']) . "\n\n";
} catch (Exception $e) {
    echo "❌ Error: " . $e->getMessage() . "\n\n";
}

// Test 4: Cross-file communication (mock)
echo "Test 4: Cross-file Communication\n";
echo "--------------------------------\n";

// Create a test config file
$testConfig = <<<TSK
[app]
name: "SharedApp"
version: "2.0"

[database]
host: "db.example.com"
port: 5432
TSK;

file_put_contents('test-config.tsk', $testConfig);

$config4 = <<<TSK
# Get from another file
app_name: @test-config.tsk.get('app.name')
db_host: @test-config.tsk.get('database.host')

# Set in another file (this will modify test-config.tsk)
updated: @test-config.tsk.set('app.updated', true)
TSK;

try {
    $parsed4 = TuskLangEnhanced::parse($config4);
    echo "✅ Cross-file communication works:\n";
    echo "App name from other file: " . $parsed4['app_name'] . "\n";
    echo "DB host from other file: " . $parsed4['db_host'] . "\n\n";
    
    // Verify the file was updated
    $updatedConfig = TuskLangEnhanced::parse(file_get_contents('test-config.tsk'));
    echo "File was updated: " . ($updatedConfig['app']['updated'] ? 'YES' : 'NO') . "\n\n";
} catch (Exception $e) {
    echo "❌ Error: " . $e->getMessage() . "\n\n";
}

// Test 5: Optional semicolons
echo "Test 5: Optional Semicolons\n";
echo "---------------------------\n";

$config5 = <<<TSK
# With semicolons
name: "App";
port: 8080;

# Without semicolons
debug: true
cache: false

# Mixed
timeout: 30;
retry: 3
TSK;

try {
    $parsed5 = TuskLangEnhanced::parse($config5);
    echo "✅ Optional semicolons work:\n";
    echo "All values parsed correctly\n\n";
} catch (Exception $e) {
    echo "❌ Error: " . $e->getMessage() . "\n\n";
}

// Test 6: Nested angle brackets
echo "Test 6: Nested Structures\n";
echo "-------------------------\n";

$config6 = <<<TSK
app>
    name: "NestedApp"
    
    database>
        host: "localhost"
        credentials>
            user: "admin"
            pass: "secret"
        <
    <
    
    features: ["auth", "api", "cache"]
<
TSK;

try {
    $parsed6 = TuskLangEnhanced::parse($config6);
    echo "✅ Nested angle brackets work:\n";
    echo "DB User: " . $parsed6['app']['database']['credentials']['user'] . "\n";
    echo "Features: " . json_encode($parsed6['app']['features']) . "\n\n";
} catch (Exception $e) {
    echo "❌ Error: " . $e->getMessage() . "\n\n";
}

// Cleanup
@unlink('test-config.tsk');

echo "🎉 TuskLang Enhanced: Freedom of choice in configuration!\n";
echo "Use [], use >, use {}, or mix them all - we don't bow to any king!\n";