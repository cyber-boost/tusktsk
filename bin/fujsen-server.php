#!/usr/bin/env php
<?php
/**
 * 🌐 FUJSEN Development Server
 * ===========================
 * Router script for PHP built-in server that handles .tsk files as API endpoints
 * 
 * This script processes .tsk files with #!api directive as HTTP endpoints
 * with full @ operator support including @request, @json(), @render(), etc.
 */

// Auto-detect FUJSEN installation path
$fujsenPaths = [
    __DIR__ . '/../fujsen/src',
    __DIR__ . '/../../fujsen/src',
    '/usr/local/lib/tusklang/fujsen/src',
    '/opt/tusklang/fujsen/src'
];

$fujsenPath = null;
foreach ($fujsenPaths as $path) {
    if (file_exists($path . '/autoload.php')) {
        $fujsenPath = $path;
        break;
    }
}

if (!$fujsenPath) {
    http_response_code(500);
    die("❌ FUJSEN installation not found");
}

// Load FUJSEN core
require_once $fujsenPath . '/autoload.php';

use TuskPHP\Utils\TuskLangWebHandler;

// Get the requested file
$requestUri = $_SERVER['REQUEST_URI'];
$parsedUrl = parse_url($requestUri);
$path = $parsedUrl['path'];

// Remove leading slash
$path = ltrim($path, '/');

// If no path or directory, try index files
if (empty($path) || is_dir($path)) {
    $indexFiles = ['index.tsk', 'index.php', 'index.html'];
    $basePath = empty($path) ? '.' : $path;
    
    foreach ($indexFiles as $indexFile) {
        $fullPath = $basePath . '/' . $indexFile;
        if (file_exists($fullPath)) {
            $path = $fullPath;
            break;
        }
    }
}

// Check if file exists
if (!file_exists($path)) {
    // Try adding .tsk extension for API endpoints
    if (file_exists($path . '.tsk')) {
        $path = $path . '.tsk';
    } else {
        // Return false to let PHP built-in server handle static files
        return false;
    }
}

// Handle .tsk files as FUJSEN endpoints
if (pathinfo($path, PATHINFO_EXTENSION) === 'tsk') {
    try {
        $webHandler = TuskLangWebHandler::getInstance();
        $webHandler->handleRequest($path);
    } catch (Exception $e) {
        http_response_code(500);
        header('Content-Type: application/json');
        echo json_encode([
            'error' => true,
            'message' => 'FUJSEN Error: ' . $e->getMessage(),
            'file' => $path,
            'timestamp' => time()
        ], JSON_PRETTY_PRINT);
    }
    return true;
}

// For non-.tsk files, let PHP built-in server handle them
return false; 