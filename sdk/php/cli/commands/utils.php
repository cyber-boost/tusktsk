<?php
// Utility command handler for TuskLang CLI (PHP)

function handleParseCommand($args) {
    global $json, $verbose, $quiet;
    $file = $args[0] ?? null;
    if (!$file || !file_exists($file)) {
        echo status('error', 'File not found') . "\n";
        exit(3);
    }
    try {
        $parser = new \TuskLang\Enhanced\TuskLangEnhanced();
        $data = $parser->parseFile($file);
        if ($json) jsonOutput($data);
        foreach ($data as $key => $value) {
            echo "$key = " . (is_array($value) ? json_encode($value) : $value) . "\n";
        }
    } catch (Exception $e) {
        echo status('error', $e->getMessage()) . "\n";
        exit(1);
    }
}

function handleValidateCommand($args) {
    global $json, $verbose, $quiet;
    $file = $args[0] ?? null;
    if (!$file || !file_exists($file)) {
        echo status('error', 'File not found') . "\n";
        exit(3);
    }
    try {
        $parser = new \TuskLang\Enhanced\TuskLangEnhanced();
        $parser->parseFile($file);
        if ($json) jsonOutput(['valid' => true]);
        echo status('success', 'Valid TuskLang syntax') . "\n";
    } catch (Exception $e) {
        if ($json) jsonOutput(['valid' => false, 'error' => $e->getMessage()], false);
        echo status('error', 'Syntax error: ' . $e->getMessage()) . "\n";
        exit(1);
    }
}

function handleConvertCommand($args) {
    global $json, $verbose, $quiet;
    $input = null;
    $output = null;
    
    // Parse -i and -o arguments
    for ($i = 0; $i < count($args); $i++) {
        if ($args[$i] === '-i' && isset($args[$i + 1])) {
            $input = $args[$i + 1];
        } elseif ($args[$i] === '-o' && isset($args[$i + 1])) {
            $output = $args[$i + 1];
        }
    }
    
    if (!$input || !file_exists($input)) {
        echo status('error', 'Input file not found') . "\n";
        exit(3);
    }
    
    try {
        $parser = new \TuskLang\Enhanced\TuskLangEnhanced();
        $data = $parser->parseFile($input);
        
        if ($output) {
            file_put_contents($output, json_encode($data, JSON_PRETTY_PRINT));
            if ($json) jsonOutput(['converted' => $output]);
            echo status('success', "Converted to: $output") . "\n";
        } else {
            if ($json) jsonOutput($data);
            echo json_encode($data, JSON_PRETTY_PRINT) . "\n";
        }
    } catch (Exception $e) {
        echo status('error', $e->getMessage()) . "\n";
        exit(1);
    }
}

function handleGetCommand($args) {
    global $json, $verbose, $quiet;
    $file = $args[0] ?? null;
    $keyPath = $args[1] ?? null;
    
    if (!$file || !file_exists($file)) {
        echo status('error', 'File not found') . "\n";
        exit(3);
    }
    
    if (!$keyPath) {
        echo status('error', 'Key path required') . "\n";
        exit(2);
    }
    
    try {
        $parser = new \TuskLang\Enhanced\TuskLangEnhanced();
        $parser->parseFile($file);
        $value = $parser->get($keyPath);
        
        if ($json) jsonOutput(['key' => $keyPath, 'value' => $value]);
        echo $value !== null ? (is_array($value) ? json_encode($value) : $value) : '' . "\n";
    } catch (Exception $e) {
        echo status('error', $e->getMessage()) . "\n";
        exit(1);
    }
}

function handleSetCommand($args) {
    global $json, $verbose, $quiet;
    $file = $args[0] ?? null;
    $keyPath = $args[1] ?? null;
    $value = $args[2] ?? null;
    
    if (!$file || !file_exists($file)) {
        echo status('error', 'File not found') . "\n";
        exit(3);
    }
    
    if (!$keyPath || $value === null) {
        echo status('error', 'Key path and value required') . "\n";
        exit(2);
    }
    
    try {
        $parser = new \TuskLang\Enhanced\TuskLangEnhanced();
        $parser->parseFile($file);
        $parser->set($keyPath, $value);
        
        // Save back to file (simplified - in real implementation you'd want to preserve formatting)
        $data = $parser->toArray();
        file_put_contents($file, json_encode($data, JSON_PRETTY_PRINT));
        
        if ($json) jsonOutput(['set' => $keyPath, 'value' => $value]);
        echo status('success', "Set $keyPath = $value") . "\n";
    } catch (Exception $e) {
        echo status('error', $e->getMessage()) . "\n";
        exit(1);
    }
} 