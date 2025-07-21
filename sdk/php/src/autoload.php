<?php
/**
 * TuskLang PHP SDK - Autoloader
 * =============================
 * Autoloads all CoreOperators and classes
 */

spl_autoload_register(function ($class) {
    // Convert namespace to file path
    $file = __DIR__ . '/' . str_replace('\\', '/', $class) . '.php';
    
    // Check if file exists and load it
    if (file_exists($file)) {
        require_once $file;
        return true;
    }
    
    return false;
});

// Load all CoreOperators
$coreOperatorsDir = __DIR__ . '/CoreOperators/';
if (is_dir($coreOperatorsDir)) {
    $files = glob($coreOperatorsDir . '*.php');
    foreach ($files as $file) {
        require_once $file;
    }
} 