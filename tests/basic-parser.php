<?php
/**
 * TuskLang Basic Parser Demo
 * Shows how TuskLang parses .tsk files with real syntax
 */

// Include TuskLang parser
require_once dirname(__DIR__) . '/../reference/TuskLang.php';

use TuskLang\TuskLang;

// Create demo .tsk content
$tskContent = <<<'TSK'
# 🐘 TuskLang Demo Configuration
# This demonstrates basic TuskLang features

# Simple key-value pairs
app_name = "TuskLang Demo"
version = "1.0.0"
environment = "production"

# Nested objects with {}
database {
    host: "localhost"
    port: 5432
    name: "tusklang_demo"
    
    # Nested deeper
    pool {
        min: 5
        max: 20
        timeout: 30
    }
}

# Arrays
features = ["parser", "serializer", "validator", "@ operators"]
allowed_ips = [
    "127.0.0.1"
    "192.168.1.0/24"
    "10.0.0.0/8"
]

# Mixed nested structure
server {
    workers: 4
    memory: "256M"
    
    # Array of objects
    endpoints: [
        { path: "/api/users", method: "GET" }
        { path: "/api/posts", method: "POST" }
    ]
}

# Environment variables (requires FUJSEN for full @ operator support)
api_key = env("API_KEY", "demo-key-12345")

# PHP expressions
timestamp = php(date('Y-m-d H:i:s'))
random_number = php(rand(1, 100))

# File inclusion (demo only - would load actual file)
# config = file("./additional-config.tsk")

# Comments are preserved and ignored
# This is a comment
TSK;

// Initialize parser
$parser = new TuskLang();

// Parse the content
try {
    $startTime = microtime(true);
    $parsed = $parser->parse($tskContent);
    $parseTime = round((microtime(true) - $startTime) * 1000, 2);
    
    // Convert back to TSK format
    $serialized = $parser->serialize($parsed);
    
    // Display results
    ?>
    <!DOCTYPE html>
    <html>
    <head>
        <title>TuskLang Parser Demo</title>
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
                box-shadow: 0 2px 4px rgba(0,0,0,0.1);
            }
            h1 {
                color: #2c3e50;
                display: flex;
                align-items: center;
                gap: 10px;
            }
            .elephant {
                font-size: 1.5em;
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
                font-size: 1.2em;
            }
            pre {
                background: #282c34;
                color: #abb2bf;
                padding: 15px;
                border-radius: 4px;
                overflow-x: auto;
                margin: 0;
                font-size: 14px;
                line-height: 1.5;
            }
            .highlight {
                color: #61dafb;
            }
            .stats {
                display: flex;
                gap: 20px;
                margin: 20px 0;
            }
            .stat {
                background: #e3f2fd;
                padding: 10px 20px;
                border-radius: 4px;
                text-align: center;
            }
            .stat-value {
                font-size: 24px;
                font-weight: bold;
                color: #1976d2;
            }
            .stat-label {
                font-size: 14px;
                color: #666;
            }
            .features {
                margin: 20px 0;
            }
            .feature {
                display: inline-block;
                background: #e8f5e9;
                padding: 5px 15px;
                margin: 5px;
                border-radius: 20px;
                font-size: 14px;
                color: #2e7d32;
            }
            .try-more {
                margin-top: 30px;
                padding: 20px;
                background: #fff3cd;
                border-radius: 4px;
                border: 1px solid #ffeeba;
            }
            .comment { color: #5c6370; font-style: italic; }
            .string { color: #98c379; }
            .number { color: #d19a66; }
            .key { color: #e06c75; }
            .keyword { color: #c678dd; }
        </style>
    </head>
    <body>
        <div class="container">
            <h1><span class="elephant">🐘</span> TuskLang Parser Live Demo</h1>
            
            <div class="stats">
                <div class="stat">
                    <div class="stat-value"><?php echo $parseTime; ?>ms</div>
                    <div class="stat-label">Parse Time</div>
                </div>
                <div class="stat">
                    <div class="stat-value"><?php echo count($parsed); ?></div>
                    <div class="stat-label">Top-level Keys</div>
                </div>
                <div class="stat">
                    <div class="stat-value"><?php echo count(explode("\n", $tskContent)); ?></div>
                    <div class="stat-label">Lines of TSK</div>
                </div>
            </div>
            
            <div class="features">
                <h3>Features Demonstrated:</h3>
                <span class="feature">✓ Comments</span>
                <span class="feature">✓ Key-Value Pairs</span>
                <span class="feature">✓ Nested Objects</span>
                <span class="feature">✓ Arrays</span>
                <span class="feature">✓ Environment Variables</span>
                <span class="feature">✓ PHP Expressions</span>
                <span class="feature">✓ Mixed Syntax (: and =)</span>
            </div>
            
            <div class="grid">
                <div class="panel">
                    <h2>📄 Original TuskLang (.tsk)</h2>
                    <pre><?php 
                    // Syntax highlight the TSK
                    $highlighted = htmlspecialchars($tskContent);
                    $highlighted = preg_replace('/^#.*$/m', '<span class="comment">$0</span>', $highlighted);
                    $highlighted = preg_replace('/"([^"]*)"/', '<span class="string">"$1"</span>', $highlighted);
                    $highlighted = preg_replace('/\b(\d+)\b/', '<span class="number">$1</span>', $highlighted);
                    $highlighted = preg_replace('/^(\w+)\s*[:=]/m', '<span class="key">$1</span>$0', $highlighted);
                    $highlighted = preg_replace('/\b(env|php|file|query)\b/', '<span class="keyword">$0</span>', $highlighted);
                    echo $highlighted;
                    ?></pre>
                </div>
                
                <div class="panel">
                    <h2>🔄 Parsed PHP Array</h2>
                    <pre><?php 
                    $output = var_export($parsed, true);
                    // Syntax highlight PHP
                    $output = htmlspecialchars($output);
                    $output = preg_replace('/=&gt;/', '<span class="keyword">=&gt;</span>', $output);
                    $output = preg_replace('/\'([^\']+)\'/', '<span class="string">\'$1\'</span>', $output);
                    $output = preg_replace('/\b(\d+)\b/', '<span class="number">$1</span>', $output);
                    $output = preg_replace('/\b(array|true|false|null)\b/', '<span class="keyword">$0</span>', $output);
                    echo $output;
                    ?></pre>
                </div>
            </div>
            
            <div class="panel" style="margin-top: 20px;">
                <h2>🔄 Re-serialized TuskLang</h2>
                <p style="color: #666; font-size: 14px;">This shows that TuskLang can parse and regenerate the format:</p>
                <pre><?php echo htmlspecialchars($serialized); ?></pre>
            </div>
            
            <div class="try-more">
                <h3>🚀 Try More Examples!</h3>
                <p>This demo shows basic TuskLang parsing. Check out these other demos:</p>
                <ul>
                    <li><a href="dynamic-features.php">Dynamic Features</a> - env(), php(), file() functions</li>
                    <li><a href="fujsen-operators.php">FUJSEN @ Operators</a> - @if, @query, @cache, etc.</li>
                    <li><a href="peanuts-demo.php">Peanuts Integration</a> - Binary config with @p()</li>
                    <li><a href="api-endpoint.php">API Endpoints</a> - Create APIs from .tsk files</li>
                    <li><a href="live-editor.php">Live Editor</a> - Try writing TuskLang yourself!</li>
                </ul>
            </div>
        </div>
    </body>
    </html>
    <?php
    
} catch (Exception $e) {
    echo "<h1>Error parsing TuskLang</h1>";
    echo "<pre>" . htmlspecialchars($e->getMessage()) . "</pre>";
    echo "<pre>" . htmlspecialchars($e->getTraceAsString()) . "</pre>";
}