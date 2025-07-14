<?php
/**
 * Test simplified echo endpoint
 */

// Capture all output
ob_start();

// Simulate HTTP request
$_SERVER['REQUEST_METHOD'] = 'GET';
$_SERVER['REQUEST_URI'] = '/echo-simple?format=json&test=123';
$_GET = ['format' => 'json', 'test' => '123'];
$_SERVER['REMOTE_ADDR'] = '127.0.0.1';
$_SERVER['HTTP_USER_AGENT'] = 'Test/1.0';

try {
    require_once 'autoload.php';
    
    $webHandler = \TuskPHP\Utils\TuskLangWebHandler::getInstance();
    $tskFile = __DIR__ . '/api/echo-simple.tsk';
    $webHandler->handleRequest($tskFile);
    
} catch (Exception $e) {
    echo "ERROR: " . $e->getMessage();
}

$output = ob_get_clean();

// Clean up the output (remove header warnings)
$lines = explode("\n", $output);
$cleanLines = [];

foreach ($lines as $line) {
    if (!str_contains($line, 'Warning: Cannot modify header') && 
        !str_contains($line, 'headers already sent') &&
        !empty(trim($line))) {
        $cleanLines[] = $line;
    }
}

$cleanOutput = implode("\n", $cleanLines);

echo "🔧 Simple Echo Test\n";
echo "==================\n\n";
echo "Clean Output (" . strlen($cleanOutput) . " bytes):\n";
echo $cleanOutput . "\n\n";

if (!empty($cleanOutput)) {
    $decoded = json_decode($cleanOutput, true);
    if (json_last_error() === JSON_ERROR_NONE) {
        echo "✅ Valid JSON output!\n";
        echo "Keys: " . implode(', ', array_keys($decoded)) . "\n";
        
        // Check specific values
        echo "\nValue Analysis:\n";
        echo "- echo: " . json_encode($decoded['echo'] ?? 'NOT FOUND') . "\n";
        echo "- method: " . json_encode($decoded['method'] ?? 'NOT FOUND') . "\n";
        echo "- query: " . json_encode($decoded['query'] ?? 'NOT FOUND') . "\n";
        echo "- timestamp: " . json_encode($decoded['timestamp'] ?? 'NOT FOUND') . "\n";
        echo "- cached_time: " . json_encode($decoded['cached_time'] ?? 'NOT FOUND') . "\n";
        echo "- metrics: " . json_encode($decoded['metrics'] ?? 'NOT FOUND') . "\n";
    } else {
        echo "❌ Not valid JSON: " . json_last_error_msg() . "\n";
    }
} else {
    echo "❌ No output received\n";
} 