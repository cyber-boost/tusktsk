<?php
/**
 * Test parser output without header conflicts
 */

// Capture all output
ob_start();

// Simulate HTTP request
$_SERVER['REQUEST_METHOD'] = 'GET';
$_SERVER['REQUEST_URI'] = '/echo?format=json';
$_GET = ['format' => 'json'];
$_SERVER['REMOTE_ADDR'] = '127.0.0.1';
$_SERVER['HTTP_USER_AGENT'] = 'Test/1.0';

try {
    require_once 'autoload.php';
    
    // Create web handler (suppress header warnings)
    $webHandler = \TuskPHP\Utils\TuskLangWebHandler::getInstance();
    
    // Execute echo.tsk
    $tskFile = __DIR__ . '/api/echo.tsk';
    $webHandler->handleRequest($tskFile);
    
} catch (Exception $e) {
    echo "ERROR: " . $e->getMessage();
}

// Get the output
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

echo "🔧 FUJSEN Parser Output Test\n";
echo "===========================\n\n";
echo "Clean Output (" . strlen($cleanOutput) . " bytes):\n";
echo $cleanOutput . "\n\n";

// Try to parse as JSON
if (!empty($cleanOutput)) {
    $decoded = json_decode($cleanOutput, true);
    if (json_last_error() === JSON_ERROR_NONE) {
        echo "✅ Valid JSON output!\n";
        echo "Keys: " . implode(', ', array_keys($decoded)) . "\n";
    } else {
        echo "❌ Not valid JSON: " . json_last_error_msg() . "\n";
        echo "First 200 chars: " . substr($cleanOutput, 0, 200) . "\n";
    }
} else {
    echo "❌ No output received\n";
} 