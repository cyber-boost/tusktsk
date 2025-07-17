<?php
/**
 * Debug API Test Script
 */

// Simulate request environment
$_SERVER['REQUEST_METHOD'] = 'GET';
$_SERVER['REQUEST_URI'] = '/echo';
$_SERVER['SCRIPT_NAME'] = 'test-api-debug.php';
$_GET = ['format' => 'json'];
$_POST = [];
$_SERVER['REMOTE_ADDR'] = '127.0.0.1';
$_SERVER['HTTP_USER_AGENT'] = 'FUJSEN-Test/1.0';

echo "🔥 FUJSEN API Debug Test\n";
echo "========================\n\n";

try {
    // Test the router components step by step
    require_once 'autoload.php';
    
    // Test 1: Check if API directory exists
    $apiDir = __DIR__ . '/api';
    echo "API Directory: $apiDir\n";
    echo "Directory exists: " . (is_dir($apiDir) ? "YES" : "NO") . "\n";
    
    if (is_dir($apiDir)) {
        $files = scandir($apiDir);
        echo "Files in API directory: " . implode(', ', array_filter($files, fn($f) => $f !== '.' && $f !== '..')) . "\n";
    }
    
    // Test 2: Check path parsing
    $uri = $_SERVER['REQUEST_URI'] ?? '/';
    $path = parse_url($uri, PHP_URL_PATH);
    $path = ltrim($path, '/');
    echo "Parsed path: '$path'\n";
    
    // Test 3: Find .tsk file
    $tskFile = $apiDir . '/' . $path . '.tsk';
    echo "Looking for file: $tskFile\n";
    echo "File exists: " . (file_exists($tskFile) ? "YES" : "NO") . "\n";
    
    if (file_exists($tskFile)) {
        echo "File contents preview:\n";
        echo substr(file_get_contents($tskFile), 0, 200) . "...\n";
        
        // Test 4: Try to execute the file
        echo "\nTesting web handler...\n";
        $webHandler = \TuskPHP\Utils\TuskLangWebHandler::getInstance();
        $webHandler->handleRequest($tskFile);
    }
    
} catch (Exception $e) {
    echo "Error: " . $e->getMessage() . "\n";
    echo "Stack trace:\n" . $e->getTraceAsString() . "\n";
} 