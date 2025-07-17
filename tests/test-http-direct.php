<?php
/**
 * Test HTTP request directly
 */

// Simulate HTTP environment
$_SERVER['REQUEST_METHOD'] = 'GET';
$_SERVER['REQUEST_URI'] = '/echo-simple?format=json&test=123';
$_SERVER['SCRIPT_NAME'] = '/api-router.php';
$_GET = ['format' => 'json', 'test' => '123'];
$_SERVER['REMOTE_ADDR'] = '127.0.0.1';
$_SERVER['HTTP_USER_AGENT'] = 'Test/1.0';

// Capture output
ob_start();

try {
    // Include and execute the router
    require_once 'api-router.php';
} catch (Exception $e) {
    echo "ERROR: " . $e->getMessage() . "\n";
    echo "Stack trace: " . $e->getTraceAsString() . "\n";
}

$output = ob_get_clean();

echo "🔧 HTTP Direct Test\n";
echo "==================\n\n";
echo "Output (" . strlen($output) . " bytes):\n";
echo $output . "\n"; 