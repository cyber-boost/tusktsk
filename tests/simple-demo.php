<?php
/**
 * Simple TuskLang Demo
 * Shows basic parsing functionality
 */

// Enable error reporting
error_reporting(E_ALL);
ini_set('display_errors', 1);

// Include TuskLang parser
require_once dirname(__DIR__) . '/../reference/TuskLang.php';

use TuskPHP\Utils\TuskLang;

// Create simple TSK content
$tskContent = <<<'TSK'
# Simple TuskLang Demo
app_name: "TuskLang Demo"
version: "1.0.0"

# Database config
database {
    host: "localhost"
    port: 5432
    name: "myapp"
}

# Features list
features: ["api", "auth", "cache"]
TSK;

// Parse it
$parser = new TuskLang();
$parsed = $parser->parse($tskContent);

?>
<!DOCTYPE html>
<html>
<head>
    <title>TuskLang Demo</title>
    <style>
        body {
            font-family: Arial, sans-serif;
            max-width: 800px;
            margin: 50px auto;
            padding: 20px;
            background: #f5f5f5;
        }
        .container {
            background: white;
            padding: 30px;
            border-radius: 8px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }
        h1 {
            color: #333;
            display: flex;
            align-items: center;
            gap: 10px;
        }
        pre {
            background: #2d2d2d;
            color: #f8f8f2;
            padding: 20px;
            border-radius: 4px;
            overflow-x: auto;
        }
        .success {
            color: #27ae60;
            font-weight: bold;
        }
    </style>
</head>
<body>
    <div class="container">
        <h1>🐘 TuskLang Simple Demo</h1>
        
        <p class="success">✓ TuskLang is working!</p>
        
        <h2>Input (.tsk format):</h2>
        <pre><?php echo htmlspecialchars($tskContent); ?></pre>
        
        <h2>Parsed Output (PHP array):</h2>
        <pre><?php echo htmlspecialchars(print_r($parsed, true)); ?></pre>
        
        <h2>Re-serialized:</h2>
        <pre><?php echo htmlspecialchars($parser->serialize($parsed)); ?></pre>
        
        <p><a href="/">Back to Home</a> | <a href="dynamic-features.php">Try Dynamic Features</a></p>
    </div>
</body>
</html>