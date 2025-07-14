<?php
// Direct test without server
require_once 'autoload.php';

echo "🔧 Direct Web Handler Test\n";
echo "==========================\n";

// Mock request data
$_SERVER['REQUEST_METHOD'] = 'GET';
$_SERVER['REQUEST_URI'] = '/api/test';
$_GET = [];
$_POST = [];
$_SERVER['HTTP_USER_AGENT'] = 'Test Agent';
$_SERVER['REMOTE_ADDR'] = '127.0.0.1';

try {
    $handler = new TuskPHP\Utils\TuskLangWebHandler();
    
    echo "1. Handler created successfully\n";
    
    // Test the simple test.tsk file
    $file = 'api/test.tsk';
    echo "2. Testing file: $file\n";
    
    if (file_exists($file)) {
        echo "3. File exists\n";
        $content = file_get_contents($file);
        echo "4. File content: " . trim($content) . "\n";
        
        // Capture output
        ob_start();
        $handler->handleRequest($file);
        $output = ob_get_clean();
        
        echo "5. Output length: " . strlen($output) . "\n";
        echo "6. Output: " . $output . "\n";
        
    } else {
        echo "3. File does not exist!\n";
    }
    
} catch (Exception $e) {
    echo "ERROR: " . $e->getMessage() . "\n";
    echo "Stack trace:\n" . $e->getTraceAsString() . "\n";
} 