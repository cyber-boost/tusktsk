<?php
/**
 * 🚀 FUJSEN Autoloader - Unified Component Loading
 * ===============================================
 * "Loading intelligence into configuration"
 * 
 * Autoloader for TuskLang + db-energy integration
 * FUJSEN Sprint - Hour 2 Implementation
 */

// Autoloader function
spl_autoload_register(function ($className) {
    // Convert namespace to file path
    $classPath = str_replace('\\', '/', $className);
    
    // Define base directories
    $baseDirs = [
        __DIR__ . '/',
        __DIR__ . '/db-energy/',
    ];
    
    // Map specific classes to files
    $classMap = [
        'TuskPHP\\Utils\\TuskLang' => __DIR__ . '/TuskLang.php',
        'TuskPHP\\Utils\\TuskLangQueryBridge' => __DIR__ . '/TuskLangQueryBridge.php',
        'TuskPHP\\TuskQuery' => __DIR__ . '/db-energy/TuskQuery.php',
        'TuskPHP\\TuskObject' => __DIR__ . '/db-energy/TuskObject.php',
        'TuskPHP\\TuskDb' => __DIR__ . '/db-energy/TuskDb.php',
    ];
    
    // Check class map first
    if (isset($classMap[$className])) {
        $file = $classMap[$className];
        if (file_exists($file)) {
            require_once $file;
            return;
        }
    }
    
    // Try standard PSR-4 loading
    foreach ($baseDirs as $baseDir) {
        $file = $baseDir . $classPath . '.php';
        if (file_exists($file)) {
            require_once $file;
            return;
        }
    }
    
    // Try with different extensions and paths
    $possibleFiles = [
        __DIR__ . '/' . basename($classPath) . '.php',
        __DIR__ . '/db-energy/' . basename($classPath) . '.php',
    ];
    
    foreach ($possibleFiles as $file) {
        if (file_exists($file)) {
            require_once $file;
            return;
        }
    }
});

// Load core files explicitly
$coreFiles = [
    __DIR__ . '/TuskLang.php',
    __DIR__ . '/TuskLangQueryBridge.php',
    __DIR__ . '/TuskLangJITOptimizer.php',
    __DIR__ . '/db-energy/TuskDb.php',
    __DIR__ . '/db-energy/TuskQuery.php',
    __DIR__ . '/db-energy/TuskObject.php',
];

foreach ($coreFiles as $file) {
    if (file_exists($file)) {
        require_once $file;
    }
}

// Initialize bridge on autoload
if (class_exists('TuskPHP\\Utils\\TuskLangQueryBridge')) {
    // Ensure SQLite cache is initialized
    \TuskPHP\Utils\TuskLangQueryBridge::getInstance();
}

// Initialize JIT optimizer
if (class_exists('TuskPHP\\Utils\\TuskLangJITOptimizer')) {
    // Initialize JIT optimizer (warmup on demand)
    $jitOptimizer = \TuskPHP\Utils\TuskLangJITOptimizer::getInstance();
} 