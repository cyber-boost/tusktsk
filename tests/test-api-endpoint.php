<?php
/**
 * 🧪 Test API Endpoint Functionality
 * ==================================
 * Direct test of web handler without HTTP server
 */

require_once __DIR__ . '/autoload.php';

use TuskPHP\Utils\TuskLangWebHandler;

// Simulate HTTP request
$_SERVER['REQUEST_METHOD'] = 'GET';
$_SERVER['REQUEST_URI'] = '/echo-simple?format=json&test=123';
$_SERVER['REMOTE_ADDR'] = '127.0.0.1';
$_SERVER['HTTP_USER_AGENT'] = 'Test Script';
$_GET = ['format' => 'json', 'test' => '123'];

echo "🧪 Testing API Endpoint Functionality\n";
echo "====================================\n\n";

// Test echo-simple.tsk
$tskFile = __DIR__ . '/api/echo-simple.tsk';
echo "Testing: $tskFile\n";
echo "Request: GET /echo-simple?format=json&test=123\n\n";

// Capture output
ob_start();
$webHandler = TuskLangWebHandler::getInstance();
$webHandler->handleRequest($tskFile);
$output = ob_get_clean();

echo "Response Output:\n";
echo $output . "\n\n";

// Try to decode as JSON
$json = json_decode($output, true);
if ($json !== null) {
    echo "✅ Valid JSON response\n";
    echo "Response data:\n";
    print_r($json);
} else {
    echo "❌ Invalid JSON response\n";
    echo "Raw output: " . var_export($output, true) . "\n";
}

// Test a simpler endpoint
echo "\n\nTesting simple endpoint:\n";
echo "========================\n";

$simpleContent = '#!api
message: "Hello World"
timestamp: php(time())
result: @json({ message: message, time: timestamp })';

file_put_contents(__DIR__ . '/api/test-simple.tsk', $simpleContent);

ob_start();
$webHandler->handleRequest(__DIR__ . '/api/test-simple.tsk');
$output2 = ob_get_clean();

echo "Response: $output2\n";

// Cleanup
unlink(__DIR__ . '/api/test-simple.tsk');