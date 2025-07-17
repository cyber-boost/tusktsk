<?php
// Peanuts command handler for TuskLang CLI (PHP)

function handlePeanutsCommand($subcommand, $args) {
    global $json, $verbose, $quiet;
    $exitCode = 0;
    try {
        switch ($subcommand) {
            case 'compile':
                $file = $args[0] ?? null;
                if (!$file || !file_exists($file)) {
                    echo status('error', 'File not found for compilation') . "\n";
                    exit(3);
                }
                $outputFile = str_replace(['.peanuts', '.tsk'], '.pnt', $file);
                $config = \TuskLang\PeanutConfig::getInstance();
                $config->compileBinary($file, $outputFile);
                if ($json) jsonOutput(['compiled' => $outputFile]);
                echo status('success', "Compiled to binary: $outputFile") . "\n";
                break;
            case 'auto-compile':
                $directory = $args[0] ?? '.';
                $config = \TuskLang\PeanutConfig::getInstance();
                $config->load($directory);
                if ($json) jsonOutput(['auto_compiled' => true]);
                echo status('success', 'Auto-compiled all peanuts files') . "\n";
                break;
            case 'load':
                $file = $args[0] ?? null;
                if (!$file || !file_exists($file)) {
                    echo status('error', 'Binary peanuts file not found') . "\n";
                    exit(3);
                }
                $config = \TuskLang\PeanutConfig::getInstance();
                $data = $config->loadBinary($file);
                if ($json) jsonOutput(['data' => $data]);
                echo status('success', 'Binary peanuts file loaded') . "\n";
                foreach ($data as $key => $value) {
                    echo status('info', "$key: " . (is_array($value) ? json_encode($value) : $value)) . "\n";
                }
                break;
            default:
                echo status('info', 'Available peanuts commands: compile <file>, auto-compile [dir], load <file.pnt>') . "\n";
                $exitCode = 2;
        }
    } catch (Exception $e) {
        $exitCode = 1;
        if ($json) jsonOutput(['error' => $e->getMessage()], false);
        echo status('error', $e->getMessage()) . "\n";
    }
    exit($exitCode);
} 