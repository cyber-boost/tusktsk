<?php
/**
 * 🧪 COMPREHENSIVE FUJSEN FUNCTION TESTER
 * =======================================
 * Tests ALL TuskLang and FUJSEN capabilities in Docker
 */

echo "🐳 FUJSEN Docker Comprehensive Test Suite\n";
echo "==========================================\n\n";

// Include autoloader
require_once __DIR__ . '/src/autoload.php';

$tests = [];
$passed = 0;
$failed = 0;

function runTest($name, $callable) {
    global $tests, $passed, $failed;
    
    echo "🧪 Testing: $name\n";
    echo str_repeat("-", 50) . "\n";
    
    try {
        $result = $callable();
        if ($result === true || (is_array($result) && $result['success'] === true)) {
            echo "✅ PASSED\n\n";
            $passed++;
            $tests[$name] = 'PASSED';
        } else {
            echo "❌ FAILED: " . (is_string($result) ? $result : json_encode($result)) . "\n\n";
            $failed++;
            $tests[$name] = 'FAILED';
        }
    } catch (Exception $e) {
        echo "❌ ERROR: " . $e->getMessage() . "\n\n";
        $failed++;
        $tests[$name] = 'ERROR: ' . $e->getMessage();
    }
}

// Test 1: Basic TuskLang Parser
runTest("Basic TuskLang Parser", function() {
    $content = "name: FUJSEN\nversion: 1.0.0\ntype: configuration_system";
    $parser = new \TuskPHP\Utils\TuskLangParser($content);
    $result = $parser->parse();
    
    return isset($result['name']) && $result['name'] === 'FUJSEN';
});

// Test 2: @ Variable References
runTest("@ Variable References", function() {
    $content = "base_url: https://api.example.com\napi_endpoint: @base_url/v1/users";
    $parser = new \TuskPHP\Utils\TuskLangParser($content);
    $result = $parser->parse();
    
    return $result['api_endpoint'] === 'https://api.example.com/v1/users';
});

// Test 3: QueryBridge Initialization
runTest("QueryBridge Initialization", function() {
    $bridge = \TuskPHP\Utils\TuskLangQueryBridge::getInstance();
    return $bridge !== null;
});

// Test 4: SQLite Cache System
runTest("SQLite Cache System", function() {
    $bridge = \TuskPHP\Utils\TuskLangQueryBridge::getInstance();
    $result = $bridge->handleCache("5m", "test_value_" . time());
    return $result !== null;
});

// Test 5: Metrics System
runTest("Metrics System", function() {
    $bridge = \TuskPHP\Utils\TuskLangQueryBridge::getInstance();
    $result = $bridge->handleMetrics("test_metric", 42.5);
    return $result !== null;
});

// Test 6: Learning System
runTest("Learning System", function() {
    $bridge = \TuskPHP\Utils\TuskLangQueryBridge::getInstance();
    $result = $bridge->handleLearn("test_pattern", "default_value");
    return $result !== null;
});

// Test 7: Optimization System
runTest("Optimization System", function() {
    $bridge = \TuskPHP\Utils\TuskLangQueryBridge::getInstance();
    $result = $bridge->handleOptimize("test_param", 100);
    return $result !== null && $result != 100; // Should be optimized
});

// Test 8: Web Handler Initialization
runTest("Web Handler Initialization", function() {
    $handler = \TuskPHP\Utils\TuskLangWebHandler::getInstance();
    return $handler !== null;
});

// Test 9: Request Data Processing
runTest("Request Data Processing", function() {
    $_SERVER['REQUEST_METHOD'] = 'POST';
    $_SERVER['REQUEST_URI'] = '/test?param=value';
    $_GET = ['param' => 'value'];
    
    $handler = \TuskPHP\Utils\TuskLangWebHandler::getInstance();
    $data = $handler->getRequestData();
    
    return isset($data['method']) && $data['method'] === 'POST';
});

// Test 10: JSON Response Handling
runTest("JSON Response Handling", function() {
    $handler = \TuskPHP\Utils\TuskLangWebHandler::getInstance();
    $result = $handler->handleJson(['test' => 'data']);
    
    return isset($result['__json_response']) && $result['data']['test'] === 'data';
});

// Test 11: Complex @ Operators in Web Context
runTest("Complex @ Operators in Web Context", function() {
    $content = 'result: @json({"message": "Hello Docker!", "timestamp": @cache("1m", php(time()))})';
    
    $_SERVER['REQUEST_METHOD'] = 'GET';
    $_SERVER['REQUEST_URI'] = '/test';
    
    $handler = \TuskPHP\Utils\TuskLangWebHandler::getInstance();
    $parser = new \TuskPHP\Utils\TuskLangWebParser($content, $handler);
    $result = $parser->parse();
    
    return isset($result['result']['__json_response']);
});

// Test 12: Database Query System (Mock)
runTest("Database Query System", function() {
    $bridge = \TuskPHP\Utils\TuskLangQueryBridge::getInstance();
    // Test with a simple query that should return empty result
    $result = $bridge->handleQuery("users", '.equalTo("id", "nonexistent").findAll()');
    return is_array($result); // Should return array even if empty
});

// Test 13: File-based .tsk Endpoint
runTest("File-based .tsk Endpoint", function() {
    $tskContent = '#!api' . "\n" . 'message: "Hello from Docker!"' . "\n" . 'status: "running"';
    file_put_contents('/app/test-endpoint.tsk', $tskContent);
    
    $handler = \TuskPHP\Utils\TuskLangWebHandler::getInstance();
    
    // Capture output
    ob_start();
    $handler->handleRequest('/app/test-endpoint.tsk');
    $output = ob_get_clean();
    
    $decoded = json_decode($output, true);
    return isset($decoded['message']) && $decoded['message'] === 'Hello from Docker!';
});

// Test 14: Variable Resolution in Object Literals
runTest("Variable Resolution in Object Literals", function() {
    $content = 'name: "FUJSEN"' . "\n" . 'response: @json({"system": name, "docker": true})';
    
    $_SERVER['REQUEST_METHOD'] = 'GET';
    $handler = \TuskPHP\Utils\TuskLangWebHandler::getInstance();
    $parser = new \TuskPHP\Utils\TuskLangWebParser($content, $handler);
    $result = $parser->parse();
    
    return isset($result['response']['data']['system']) && $result['response']['data']['system'] === 'FUJSEN';
});

// Test 15: PHP Expression Evaluation
runTest("PHP Expression Evaluation", function() {
    $content = 'timestamp: php(time())' . "\n" . 'random: php(rand(1, 100))';
    
    $parser = new \TuskPHP\Utils\TuskLangParser($content);
    $result = $parser->parse();
    
    return is_numeric($result['timestamp']) && is_numeric($result['random']);
});

// Test 16: Nested Object Parsing
runTest("Nested Object Parsing", function() {
    $content = 'config: { database: { host: "localhost", port: 5432 }, cache: { ttl: 3600 } }';
    
    $parser = new \TuskPHP\Utils\TuskLangParser($content);
    $result = $parser->parse();
    
    return isset($result['config']['database']['host']) && $result['config']['database']['host'] === 'localhost';
});

// Test 17: Array Processing
runTest("Array Processing", function() {
    $content = 'servers: ["web1", "web2", "web3"]' . "\n" . 'ports: [80, 443, 8080]';
    
    $parser = new \TuskPHP\Utils\TuskLangParser($content);
    $result = $parser->parse();
    
    return is_array($result['servers']) && count($result['servers']) === 3;
});

// Test 18: Conditional Logic
runTest("Conditional Logic", function() {
    $content = 'env: "production"' . "\n" . 'debug: env == "development" ? true : false';
    
    $parser = new \TuskPHP\Utils\TuskLangParser($content);
    $result = $parser->parse();
    
    return $result['debug'] === false;
});

// Test 19: Cache TTL Functionality
runTest("Cache TTL Functionality", function() {
    $bridge = \TuskPHP\Utils\TuskLangQueryBridge::getInstance();
    
    // Store value with short TTL
    $value1 = $bridge->handleCache("1s", "first_value");
    sleep(2);
    $value2 = $bridge->handleCache("1s", "second_value");
    
    // Should be different due to TTL expiration
    return $value1 !== $value2;
});

// Test 20: Complete End-to-End Workflow
runTest("Complete End-to-End Workflow", function() {
    // Create a complex .tsk file
    $tskContent = '#!api
# Complex FUJSEN endpoint
method: @request.method
timestamp: @cache("5m", php(time()))
metrics: @metrics("e2e_test", 1)
optimized: @optimize("response_time", 100)
learned: @learn("user_preference", "json")

result: @json({
    "system": "FUJSEN",
    "docker": true,
    "method": method,
    "timestamp": timestamp,
    "metrics": metrics,
    "optimized": optimized,
    "learned": learned,
    "message": "End-to-end test successful!"
})';
    
    file_put_contents('/app/e2e-test.tsk', $tskContent);
    
    $_SERVER['REQUEST_METHOD'] = 'POST';
    $_SERVER['REQUEST_URI'] = '/e2e-test';
    
    $handler = \TuskPHP\Utils\TuskLangWebHandler::getInstance();
    
    ob_start();
    $handler->handleRequest('/app/e2e-test.tsk');
    $output = ob_get_clean();
    
    $decoded = json_decode($output, true);
    
    return isset($decoded['system']) && 
           $decoded['system'] === 'FUJSEN' && 
           isset($decoded['docker']) && 
           $decoded['docker'] === true &&
           isset($decoded['message']);
});

// Print final results
echo "🏆 FINAL TEST RESULTS\n";
echo "=====================\n\n";

foreach ($tests as $name => $status) {
    $icon = str_contains($status, 'PASSED') ? '✅' : '❌';
    echo "$icon $name: $status\n";
}

echo "\n📊 SUMMARY:\n";
echo "- Total Tests: " . ($passed + $failed) . "\n";
echo "- Passed: $passed\n";
echo "- Failed: $failed\n";
echo "- Success Rate: " . round(($passed / ($passed + $failed)) * 100, 2) . "%\n\n";

if ($failed === 0) {
    echo "🎉 ALL TESTS PASSED! FUJSEN IS 110% FUNCTIONAL IN DOCKER! 🚀\n";
} else {
    echo "⚠️  Some tests failed. Check the output above for details.\n";
}

echo "\n🐳 Docker container testing complete!\n"; 