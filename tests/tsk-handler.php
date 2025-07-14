<?php
/**
 * TuskLang File Handler
 * Processes .tsk files and executes them
 */

// Include TuskLang parser
require_once dirname(__DIR__) . '/../reference/TuskLang.php';

use TuskPHP\Utils\TuskLang;

// Get the requested .tsk file
$requestedFile = $_SERVER['REQUEST_URI'];
$tskFile = __DIR__ . '/' . basename($requestedFile);

// Security check - ensure it's a .tsk file in the test directory
if (!preg_match('/\.tsk$/', $tskFile) || !file_exists($tskFile)) {
    http_response_code(404);
    die("File not found");
}

try {
    // Read the .tsk file
    $tskContent = file_get_contents($tskFile);
    
    // Parse it
    $parser = new TuskLang();
    $config = $parser->parse($tskContent);
    
    // Make config available to PHP expressions
    $GLOBALS['config'] = $config;
    
    // Execute any PHP output
    if (isset($config['output'])) {
        echo $config['output'];
    } else {
        // Default output if no specific output defined
        header('Content-Type: text/html; charset=UTF-8');
        ?>
        <!DOCTYPE html>
        <html>
        <head>
            <title>TuskLang File</title>
            <style>
                body {
                    font-family: Arial, sans-serif;
                    max-width: 800px;
                    margin: 50px auto;
                    padding: 20px;
                }
                pre {
                    background: #f5f5f5;
                    padding: 20px;
                    border-radius: 5px;
                    overflow-x: auto;
                }
            </style>
        </head>
        <body>
            <h1>🐘 TuskLang File: <?= basename($tskFile) ?></h1>
            <h2>Parsed Configuration:</h2>
            <pre><?= htmlspecialchars(print_r($config, true)) ?></pre>
        </body>
        </html>
        <?php
    }
    
} catch (Exception $e) {
    http_response_code(500);
    echo "<h1>Error processing .tsk file</h1>";
    echo "<pre>" . htmlspecialchars($e->getMessage()) . "</pre>";
}