<?php
// Database command handler for TuskLang CLI (PHP)

function handleDbCommand($subcommand, $args) {
    global $json, $verbose, $quiet;
    
    $dbFile = __DIR__ . '/../../../tusklang.db';
    $pdo = null;
    $exitCode = 0;
    
    // Helper to connect to SQLite
    $connect = function() use ($dbFile) {
        return new PDO('sqlite:' . $dbFile);
    };
    
    try {
        switch ($subcommand) {
            case 'status':
                try {
                    $pdo = $connect();
                    $pdo->query('SELECT 1');
                    if ($json) jsonOutput(['status' => 'connected', 'db' => $dbFile]);
                    echo status('success', 'Database connected') . ($verbose ? " ($dbFile)" : '') . "\n";
                } catch (Exception $e) {
                    $exitCode = 5;
                    if ($json) jsonOutput(['status' => 'error', 'error' => $e->getMessage()], false);
                    echo status('error', 'Database connection failed: ' . $e->getMessage()) . "\n";
                }
                break;
            case 'migrate':
                $file = $args[0] ?? null;
                if (!$file || !file_exists($file)) {
                    $exitCode = 3;
                    echo status('error', 'Migration file not found') . "\n";
                    break;
                }
                $pdo = $connect();
                $sql = file_get_contents($file);
                $pdo->exec($sql);
                echo status('success', 'Migration applied: ' . $file) . "\n";
                break;
            case 'console':
                echo colorize("SQLite Interactive Console (type .exit to quit)\n", 'cyan');
                $pdo = $connect();
                while (true) {
                    echo colorize('db> ', 'green');
                    $line = trim(fgets(STDIN));
                    if ($line === '.exit') break;
                    try {
                        $stmt = $pdo->query($line);
                        $result = $stmt ? $stmt->fetchAll(PDO::FETCH_ASSOC) : null;
                        if ($result) print_r($result);
                    } catch (Exception $e) {
                        echo status('error', $e->getMessage()) . "\n";
                    }
                }
                break;
            case 'backup':
                $file = $args[0] ?? ('tusklang_backup_' . date('Ymd_His') . '.sql');
                $pdo = $connect();
                $backup = $pdo->query('SELECT sql FROM sqlite_master WHERE type="table"')->fetchAll(PDO::FETCH_COLUMN);
                file_put_contents($file, implode(";\n", $backup));
                echo status('success', 'Database backup saved: ' . $file) . "\n";
                break;
            case 'restore':
                $file = $args[0] ?? null;
                if (!$file || !file_exists($file)) {
                    $exitCode = 3;
                    echo status('error', 'Backup file not found') . "\n";
                    break;
                }
                $pdo = $connect();
                $sql = file_get_contents($file);
                $pdo->exec($sql);
                echo status('success', 'Database restored from: ' . $file) . "\n";
                break;
            case 'init':
                $pdo = $connect();
                $pdo->exec('CREATE TABLE IF NOT EXISTS tusk_test (id INTEGER PRIMARY KEY, value TEXT)');
                echo status('success', 'SQLite database initialized') . "\n";
                break;
            default:
                echo status('info', 'Available db commands: status, migrate <file>, console, backup [file], restore <file>, init') . "\n";
                $exitCode = 2;
        }
    } catch (Exception $e) {
        $exitCode = 1;
        if ($json) jsonOutput(['error' => $e->getMessage()], false);
        echo status('error', $e->getMessage()) . "\n";
    }
    exit($exitCode);
} 