<?php
/**
 * Working TuskLang Demo
 * Correctly uses TuskPHP\Utils\TuskLang namespace
 */

// Enable error reporting
error_reporting(E_ALL);
ini_set('display_errors', 1);

// Include TuskLang parser
require_once dirname(__DIR__) . '/../reference/TuskLang.php';

// Use the CORRECT namespace
use TuskPHP\Utils\TuskLang;

// Simple test
try {
    $parser = new TuskLang();
    
    // Test content
    $tskContent = <<<'TSK'
# 🐘 TuskLang Working Demo
app_name = "TuskLang Demo"
version = "1.0.0"
environment = "production"

# Database config with nested objects
database {
    host: "localhost"
    port: 5432
    name: "tusklang_demo"
    
    # Connection pool
    pool {
        min: 5
        max: 20
    }
}

# Feature flags array
features = ["parser", "serializer", "validator"]

# Mixed syntax (both : and = work)
debug: false
cache_ttl = 3600

# Comments are supported
# This is ignored

# Advanced features (these work with basic TuskLang)
api_key = env("API_KEY", "demo-key-12345")
timestamp = php(date('Y-m-d H:i:s'))
TSK;

    // Parse the content
    $parsed = $parser->parse($tskContent);
    
    // Re-serialize
    $serialized = $parser->serialize($parsed);
    
} catch (Exception $e) {
    die("Error: " . $e->getMessage());
}
?>
<!DOCTYPE html>
<html>
<head>
    <title>TuskLang Working Demo</title>
    <style>
        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 1200px;
            margin: 0 auto;
            padding: 20px;
            background: #f5f5f5;
        }
        .container {
            background: white;
            padding: 30px;
            border-radius: 8px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
        }
        h1 {
            color: #2c3e50;
            border-bottom: 3px solid #3498db;
            padding-bottom: 10px;
        }
        .elephant {
            font-size: 48px;
            vertical-align: middle;
            margin-right: 10px;
        }
        .success {
            background: #d4edda;
            color: #155724;
            padding: 15px;
            border-radius: 4px;
            margin: 20px 0;
        }
        .grid {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 20px;
            margin-top: 20px;
        }
        .panel {
            background: #f8f9fa;
            padding: 20px;
            border-radius: 4px;
            border: 1px solid #dee2e6;
        }
        .panel h2 {
            margin-top: 0;
            color: #495057;
        }
        pre {
            background: #282c34;
            color: #abb2bf;
            padding: 15px;
            border-radius: 4px;
            overflow-x: auto;
            font-size: 14px;
            line-height: 1.5;
        }
        .comment { color: #5c6370; font-style: italic; }
        .string { color: #98c379; }
        .number { color: #d19a66; }
        .key { color: #e06c75; }
        .keyword { color: #c678dd; }
        .features {
            display: flex;
            flex-wrap: wrap;
            gap: 10px;
            margin: 20px 0;
        }
        .feature {
            background: #e3f2fd;
            padding: 5px 15px;
            border-radius: 20px;
            font-size: 14px;
            color: #1976d2;
        }
        .next-steps {
            margin-top: 30px;
            padding: 20px;
            background: #fff3cd;
            border-radius: 4px;
            border: 1px solid #ffeeba;
        }
        .button {
            display: inline-block;
            padding: 10px 20px;
            background: #3498db;
            color: white;
            text-decoration: none;
            border-radius: 4px;
            margin: 5px;
        }
        .button:hover {
            background: #2980b9;
        }
    </style>
</head>
<body>
    <div class="container">
        <h1><span class="elephant">🐘</span>TuskLang is Working!</h1>
        
        <div class="success">
            <strong>✓ Success!</strong> TuskLang parser is working correctly with namespace TuskPHP\Utils\TuskLang
        </div>
        
        <div class="features">
            <span class="feature">✓ Comments</span>
            <span class="feature">✓ Key-Value (= and :)</span>
            <span class="feature">✓ Nested Objects {}</span>
            <span class="feature">✓ Arrays []</span>
            <span class="feature">✓ env() function</span>
            <span class="feature">✓ php() expressions</span>
        </div>
        
        <div class="grid">
            <div class="panel">
                <h2>📝 Original TuskLang</h2>
                <pre><?php 
                // Simple syntax highlighting
                $highlighted = htmlspecialchars($tskContent);
                $highlighted = preg_replace('/^#.*$/m', '<span class="comment">$0</span>', $highlighted);
                $highlighted = preg_replace('/"([^"]*)"/', '<span class="string">"$1"</span>', $highlighted);
                $highlighted = preg_replace('/\b(\d+)\b/', '<span class="number">$1</span>', $highlighted);
                $highlighted = preg_replace('/\b(true|false|null)\b/', '<span class="keyword">$0</span>', $highlighted);
                $highlighted = preg_replace('/\b(env|php|file)\b/', '<span class="keyword">$0</span>', $highlighted);
                echo $highlighted;
                ?></pre>
            </div>
            
            <div class="panel">
                <h2>🔄 Parsed Array</h2>
                <pre><?php 
                echo htmlspecialchars(print_r($parsed, true));
                ?></pre>
            </div>
        </div>
        
        <div class="panel" style="margin-top: 20px;">
            <h2>📤 Re-serialized Output</h2>
            <pre><?php echo htmlspecialchars($serialized); ?></pre>
        </div>
        
        <div class="next-steps">
            <h3>🚀 Next Steps</h3>
            <p>Now that basic TuskLang is working, try these demos:</p>
            <div>
                <a href="env-demo.php" class="button">Environment Variables</a>
                <a href="php-expressions.php" class="button">PHP Expressions</a>
                <a href="nested-objects.php" class="button">Complex Nesting</a>
                <a href="live-editor.php" class="button">Live Editor</a>
            </div>
            <p style="margin-top: 15px;">
                <strong>Note:</strong> @ operators like @if, @query, @cache require FUJSEN enhancement layer.
            </p>
        </div>
    </div>
</body>
</html>