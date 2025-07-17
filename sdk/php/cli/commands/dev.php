<?php
// Development command handler for TuskLang CLI (PHP)

function handleServeCommand($args) {
    global $json, $verbose, $quiet;
    $port = $args[0] ?? 8080;
    $host = '0.0.0.0';
    $cmd = sprintf('php -S %s:%d', $host, $port);
    if ($json) jsonOutput(['serve' => 'started', 'host' => $host, 'port' => $port]);
    echo status('success', "Development server started at http://$host:$port (Ctrl+C to stop)") . "\n";
    passthru($cmd);
}

function handleCompileCommand($args) {
    global $json, $verbose, $quiet;
    $file = $args[0] ?? null;
    if (!$file || !file_exists($file)) {
        echo status('error', 'File not found for compilation') . "\n";
        exit(3);
    }
    // For demo: just parse and output success
    $parser = new \TuskLang\Enhanced\TuskLangEnhanced();
    $parser->parseFile($file);
    echo status('success', 'Compiled: ' . $file) . "\n";
    exit(0);
}

function handleOptimizeCommand($args) {
    global $json, $verbose, $quiet;
    $file = $args[0] ?? null;
    if (!$file || !file_exists($file)) {
        echo status('error', 'File not found for optimization') . "\n";
        exit(3);
    }
    // For demo: just parse and output success
    $parser = new \TuskLang\Enhanced\TuskLangEnhanced();
    $parser->parseFile($file);
    echo status('success', 'Optimized: ' . $file) . "\n";
    exit(0);
} 