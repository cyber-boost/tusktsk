<?php
// Enable error reporting
error_reporting(E_ALL);
ini_set('display_errors', 1);

echo "<h1>TuskLang Debug Test</h1>";
echo "<pre>";
echo "PHP Version: " . PHP_VERSION . "\n";
echo "Current Dir: " . __DIR__ . "\n";

// Test file paths
$tusklangPath = dirname(__DIR__) . '/../reference/TuskLang.php';
echo "TuskLang Path: " . $tusklangPath . "\n";
echo "File Exists: " . (file_exists($tusklangPath) ? 'YES' : 'NO') . "\n";

// Try to include
try {
    require_once $tusklangPath;
    echo "✓ TuskLang included successfully\n";
    
    // Check if namespace exists
    if (class_exists('TuskLang\TuskLang')) {
        echo "✓ TuskLang class found\n";
        
        // Try to create instance
        $tusk = new TuskLang\TuskLang();
        echo "✓ TuskLang instance created\n";
        
        // Try simple parse
        $result = $tusk->parse('test: "value"');
        echo "✓ Parse test successful\n";
        echo "Result: " . print_r($result, true);
    } else {
        echo "✗ TuskLang class not found\n";
    }
    
} catch (Exception $e) {
    echo "✗ Error: " . $e->getMessage() . "\n";
    echo "Trace:\n" . $e->getTraceAsString();
}

echo "</pre>";