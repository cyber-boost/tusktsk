<?php
// Service command handler for TuskLang CLI (PHP)

function handleServicesCommand($subcommand, $args) {
    global $json, $verbose, $quiet;
    $exitCode = 0;
    $services = ['tusk', 'fujsen', 'cache'];
    try {
        switch ($subcommand) {
            case 'start':
                foreach ($services as $service) {
                    echo status('loading', "Starting $service service...") . "\n";
                    // Simulate service start
                    sleep(1);
                    echo status('success', "$service service started") . "\n";
                }
                break;
            case 'stop':
                foreach ($services as $service) {
                    echo status('loading', "Stopping $service service...") . "\n";
                    // Simulate service stop
                    sleep(1);
                    echo status('success', "$service service stopped") . "\n";
                }
                break;
            case 'restart':
                handleServicesCommand('stop', $args);
                handleServicesCommand('start', $args);
                break;
            case 'status':
                $status = [];
                foreach ($services as $service) {
                    $status[$service] = 'running';
                }
                if ($json) jsonOutput($status);
                foreach ($status as $service => $state) {
                    echo status('success', "$service: $state") . "\n";
                }
                break;
            default:
                echo status('info', 'Available service commands: start, stop, restart, status') . "\n";
                $exitCode = 2;
        }
    } catch (Exception $e) {
        $exitCode = 1;
        if ($json) jsonOutput(['error' => $e->getMessage()], false);
        echo status('error', $e->getMessage()) . "\n";
    }
    exit($exitCode);
} 