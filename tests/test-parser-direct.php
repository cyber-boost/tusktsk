<?php
// Direct parser test without headers
require_once 'autoload.php';

echo "🔧 FUJSEN Parser Direct Test\n";
echo "============================\n\n";

// Test simple @ operator
$simple_content = '@request.method';
echo "Testing simple @ operator: $simple_content\n";

$handler = new TuskLangWebHandler();
$result = $handler->processAtOperators($simple_content, []);
echo "Result: " . json_encode($result) . "\n\n";

// Test cache operator
$cache_content = '@cache("1m", php(time()))';
echo "Testing cache operator: $cache_content\n";

$result = $handler->processAtOperators($cache_content, []);
echo "Result: " . json_encode($result) . "\n\n";

// Test echo-simple.tsk
echo "Testing echo-simple.tsk file...\n";
$content = file_get_contents('api/echo-simple.tsk');
echo "File content:\n" . substr($content, 0, 200) . "...\n\n";

// Mock request data
$_SERVER['REQUEST_METHOD'] = 'GET';
$_SERVER['REQUEST_URI'] = '/api/echo-simple?test=1';
$_GET = ['test' => '1'];
$_POST = [];
$_SERVER['HTTP_USER_AGENT'] = 'Test Agent';
$_SERVER['REMOTE_ADDR'] = '127.0.0.1';

// Process the file
ob_start();
$handler->handleRequest('/api/echo-simple');
$output = ob_get_clean();

echo "Output length: " . strlen($output) . "\n";
echo "Output preview:\n" . substr($output, 0, 500) . "\n"; 