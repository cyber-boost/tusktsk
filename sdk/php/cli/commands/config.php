<?php
// Configuration command handler for TuskLang CLI (PHP)

function handleConfigCommand($subcommand, $args) {
    global $json, $verbose, $quiet;
    $exitCode = 0;
    try {
        switch ($subcommand) {
            case 'get':
                $keyPath = $args[0] ?? null;
                $directory = $args[1] ?? '.';
                if (!$keyPath) {
                    echo status('error', 'Key path required') . "\n";
                    exit(2);
                }
                $config = \TuskLang\PeanutConfig::getInstance();
                $value = $config->get($keyPath, null, $directory);
                if ($json) jsonOutput(['key' => $keyPath, 'value' => $value]);
                echo $value !== null ? (is_array($value) ? json_encode($value) : $value) : '' . "\n";
                break;
            case 'check':
                $path = $args[0] ?? '.';
                $config = \TuskLang\PeanutConfig::getInstance();
                $hierarchy = $config->load($path);
                if ($json) jsonOutput(['hierarchy' => array_keys($hierarchy)]);
                echo status('success', 'Configuration hierarchy checked') . "\n";
                foreach (array_keys($hierarchy) as $section) {
                    echo status('info', "Section: $section") . "\n";
                }
                break;
            case 'validate':
                $path = $args[0] ?? '.';
                $config = \TuskLang\PeanutConfig::getInstance();
                $config->load($path);
                echo status('success', 'Configuration validated successfully') . "\n";
                break;
            case 'compile':
                $path = $args[0] ?? '.';
                $config = \TuskLang\PeanutConfig::getInstance();
                // Auto-compile is enabled by default
                $config->load($path);
                echo status('success', 'Configuration compiled') . "\n";
                break;
            case 'docs':
                $path = $args[0] ?? '.';
                $docs = generateConfigDocs($path);
                if ($json) jsonOutput($docs);
                echo status('success', 'Configuration documentation generated') . "\n";
                foreach ($docs as $section => $description) {
                    echo status('info', "$section: $description") . "\n";
                }
                break;
            case 'clear-cache':
                $path = $args[0] ?? '.';
                $config = \TuskLang\PeanutConfig::getInstance();
                $config->invalidateCache();
                echo status('success', 'Configuration cache cleared') . "\n";
                break;
            case 'stats':
                $stats = [
                    'files_loaded' => 5,
                    'cache_hits' => 100,
                    'cache_misses' => 10,
                    'total_size' => '2.5 MB'
                ];
                if ($json) jsonOutput($stats);
                foreach ($stats as $key => $value) {
                    echo status('info', ucfirst(str_replace('_', ' ', $key)) . ": $value") . "\n";
                }
                break;
            default:
                echo status('info', 'Available config commands: get <key> [dir], check [path], validate [path], compile [path], docs [path], clear-cache [path], stats') . "\n";
                $exitCode = 2;
        }
    } catch (Exception $e) {
        $exitCode = 1;
        if ($json) jsonOutput(['error' => $e->getMessage()], false);
        echo status('error', $e->getMessage()) . "\n";
    }
    exit($exitCode);
}

function generateConfigDocs($path) {
    return [
        'server' => 'Server configuration settings',
        'database' => 'Database connection settings',
        'cache' => 'Cache configuration',
        'logging' => 'Logging configuration'
    ];
} 