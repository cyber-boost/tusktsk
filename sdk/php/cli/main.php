#!/usr/bin/env php
<?php
/**
 * 🐘 TuskLang CLI - Universal Command Interface for PHP
 * ====================================================
 * Strong. Secure. Scalable.
 * 
 * Implements the complete Universal CLI Command Specification
 * with all required commands and functionality.
 */

// Find autoload
$autoloadPaths = [
    __DIR__ . '/../vendor/autoload.php',
    __DIR__ . '/../../../vendor/autoload.php',
    __DIR__ . '/../autoload.php'
];

foreach ($autoloadPaths as $path) {
    if (file_exists($path)) {
        require_once $path;
        break;
    }
}

// Fallback: include directly
if (!class_exists('\TuskLang\Enhanced\TuskLangEnhanced')) {
    require_once __DIR__ . '/../src/TuskLangEnhanced.php';
    require_once __DIR__ . '/../src/functions.php';
    require_once __DIR__ . '/../src/PeanutConfig.php';
}

use TuskLang\Enhanced\TuskLangEnhanced;
use TuskLang\PeanutConfig;

// Global options
$options = getopt('hvq', ['help', 'version', 'verbose', 'quiet', 'json', 'config:']);
$args = array_slice($argv, 1);

// Remove option args
$args = array_filter($args, function($arg) {
    return $arg[0] !== '-';
});
$args = array_values($args);

// Global flags
$verbose = isset($options['v']) || isset($options['verbose']);
$quiet = isset($options['q']) || isset($options['quiet']);
$json = isset($options['json']);
$configPath = $options['config'] ?? null;

// Colors for output
function colorize($text, $color = null) {
    if ($GLOBALS['json']) return $text;
    
    $colors = [
        'red' => "\033[0;31m",
        'green' => "\033[0;32m", 
        'yellow' => "\033[1;33m",
        'blue' => "\033[0;34m",
        'purple' => "\033[0;35m",
        'cyan' => "\033[0;36m",
        'white' => "\033[1;37m",
        'reset' => "\033[0m"
    ];
    
    if (!$color) return $text;
    return ($colors[$color] ?? '') . $text . $colors['reset'];
}

// Status symbols
function status($type, $message) {
    $symbols = [
        'success' => '✅',
        'error' => '❌',
        'warning' => '⚠️',
        'info' => '📍',
        'loading' => '🔄'
    ];
    
    $symbol = $symbols[$type] ?? '📍';
    return colorize("$symbol $message", $type === 'error' ? 'red' : ($type === 'success' ? 'green' : 'yellow'));
}

// JSON output helper
function jsonOutput($data, $success = true) {
    if ($GLOBALS['json']) {
        $output = [
            'success' => $success,
            'data' => $data,
            'timestamp' => date('c')
        ];
        echo json_encode($output, JSON_PRETTY_PRINT) . "\n";
        exit($success ? 0 : 1);
    }
}

// Main command router
$command = $args[0] ?? 'help';
$subcommand = $args[1] ?? null;
$commandArgs = array_slice($args, 2);

// Load command handlers
require_once __DIR__ . '/commands/db.php';
require_once __DIR__ . '/commands/dev.php';
require_once __DIR__ . '/commands/test.php';
require_once __DIR__ . '/commands/service.php';
require_once __DIR__ . '/commands/cache.php';
require_once __DIR__ . '/commands/config.php';
require_once __DIR__ . '/commands/ai.php';
require_once __DIR__ . '/commands/binary.php';
require_once __DIR__ . '/commands/peanuts.php';
require_once __DIR__ . '/commands/css.php';
require_once __DIR__ . '/commands/utils.php';

// Handle global options
if (isset($options['h']) || isset($options['help'])) {
    showHelp();
    exit(0);
}

if (isset($options['v']) || isset($options['version'])) {
    handleVersionCommand();
    exit(0);
}

// Route commands
try {
    switch ($command) {
        // Database commands
        case 'db':
            handleDbCommand($subcommand, $commandArgs);
            break;
            
        // Development commands
        case 'serve':
            handleServeCommand($commandArgs);
            break;
        case 'compile':
            handleCompileCommand($commandArgs);
            break;
        case 'optimize':
            handleOptimizeCommand($commandArgs);
            break;
            
        // Testing commands
        case 'test':
            handleTestCommand($subcommand, $commandArgs);
            break;
            
        // Service commands
        case 'services':
            handleServicesCommand($subcommand, $commandArgs);
            break;
            
        // Cache commands
        case 'cache':
            handleCacheCommand($subcommand, $commandArgs);
            break;
            
        // Configuration commands
        case 'config':
            handleConfigCommand($subcommand, $commandArgs);
            break;
            
        // Binary commands
        case 'binary':
            handleBinaryCommand($subcommand, $commandArgs);
            break;
            
        // Peanuts commands
        case 'peanuts':
            handlePeanutsCommand($subcommand, $commandArgs);
            break;
            
        // CSS commands
        case 'css':
            handleCssCommand($subcommand, $commandArgs);
            break;
            
        // AI commands
        case 'ai':
            handleAiCommand($subcommand, $commandArgs);
            break;
            
        // Utility commands
        case 'parse':
            handleParseCommand($commandArgs);
            break;
        case 'validate':
            handleValidateCommand($commandArgs);
            break;
        case 'convert':
            handleConvertCommand($commandArgs);
            break;
        case 'get':
            handleGetCommand($commandArgs);
            break;
        case 'set':
            handleSetCommand($commandArgs);
            break;
        case 'version':
            handleVersionCommand();
            break;
        case 'help':
            showHelp();
            break;
            
        // Interactive mode
        case '':
            handleInteractiveMode();
            break;
            
        default:
            echo status('error', "Unknown command: $command");
            echo "Run 'tsk help' for available commands\n";
            exit(1);
    }
} catch (Exception $e) {
    $message = $verbose ? $e->getMessage() . "\n" . $e->getTraceAsString() : $e->getMessage();
    echo status('error', $message);
    exit(1);
}

function showHelp() {
    echo colorize("🐘 TuskLang CLI - Universal Command Interface\n", 'blue');
    echo colorize("Strong. Secure. Scalable.\n\n", 'cyan');
    
    echo colorize("Usage: tsk [global-options] <command> [command-options] [arguments]\n\n", 'white');
    
    echo colorize("Global Options:\n", 'yellow');
    echo "  --help, -h          Show help for any command\n";
    echo "  --version, -v       Show version information\n";
    echo "  --verbose           Enable verbose output\n";
    echo "  --quiet, -q         Suppress non-error output\n";
    echo "  --json              Output in JSON format\n";
    echo "  --config <path>     Use alternate config file\n\n";
    
    echo colorize("🗄️  Database Commands:\n", 'yellow');
    echo "  tsk db status       Check database connection\n";
    echo "  tsk db migrate      Run migration file\n";
    echo "  tsk db console      Interactive database console\n";
    echo "  tsk db backup       Backup database\n";
    echo "  tsk db restore      Restore from backup\n";
    echo "  tsk db init         Initialize SQLite database\n\n";
    
    echo colorize("🔧 Development Commands:\n", 'yellow');
    echo "  tsk serve [port]    Start development server\n";
    echo "  tsk compile <file>  Compile .tsk file\n";
    echo "  tsk optimize <file> Optimize .tsk file\n\n";
    
    echo colorize("🧪 Testing Commands:\n", 'yellow');
    echo "  tsk test [suite]    Run test suites\n";
    echo "  tsk test all        Run all tests\n";
    echo "  tsk test parser     Test parser only\n";
    echo "  tsk test fujsen     Test FUJSEN only\n";
    echo "  tsk test sdk        Test SDK only\n";
    echo "  tsk test performance Performance tests\n\n";
    
    echo colorize("⚙️  Service Commands:\n", 'yellow');
    echo "  tsk services start  Start all services\n";
    echo "  tsk services stop   Stop all services\n";
    echo "  tsk services restart Restart services\n";
    echo "  tsk services status Show service status\n\n";
    
    echo colorize("📦 Cache Commands:\n", 'yellow');
    echo "  tsk cache clear     Clear cache\n";
    echo "  tsk cache status    Show cache status\n";
    echo "  tsk cache warm      Warm cache\n";
    echo "  tsk cache memcached Memcached operations\n\n";
    
    echo colorize("🥜 Configuration Commands:\n", 'yellow');
    echo "  tsk config get      Get config value\n";
    echo "  tsk config check    Check hierarchy\n";
    echo "  tsk config validate Validate config\n";
    echo "  tsk config compile  Compile configs\n";
    echo "  tsk config docs     Generate docs\n";
    echo "  tsk config clear-cache Clear cache\n";
    echo "  tsk config stats    Show statistics\n\n";
    
    echo colorize("🚀 Binary Commands:\n", 'yellow');
    echo "  tsk binary compile  Compile to .tskb\n";
    echo "  tsk binary execute  Execute binary\n";
    echo "  tsk binary benchmark Benchmark performance\n";
    echo "  tsk binary optimize Optimize binary\n\n";
    
    echo colorize("🥜 Peanuts Commands:\n", 'yellow');
    echo "  tsk peanuts compile Compile .peanuts to .pnt\n";
    echo "  tsk peanuts auto-compile Auto-compile all peanuts\n";
    echo "  tsk peanuts load    Load binary peanuts file\n\n";
    
    echo colorize("🎨 CSS Commands:\n", 'yellow');
    echo "  tsk css expand      Expand CSS shortcodes\n";
    echo "  tsk css map         Show shortcode mappings\n\n";
    
    echo colorize("🤖 AI Commands:\n", 'yellow');
    echo "  tsk ai claude       Query Claude AI\n";
    echo "  tsk ai chatgpt      Query ChatGPT\n";
    echo "  tsk ai analyze      Analyze code\n";
    echo "  tsk ai optimize     Get optimization suggestions\n";
    echo "  tsk ai security     Security scan\n\n";
    
    echo colorize("🛠️  Utility Commands:\n", 'yellow');
    echo "  tsk parse <file>    Parse TSK file\n";
    echo "  tsk validate <file> Validate syntax\n";
    echo "  tsk convert         Convert formats\n";
    echo "  tsk get <file> <key> Get value\n";
    echo "  tsk set <file> <key> <val> Set value\n";
    echo "  tsk version         Show version\n";
    echo "  tsk help            Show this help\n\n";
    
    echo colorize("Examples:\n", 'purple');
    echo "  tsk db status                    # Check database\n";
    echo "  tsk serve 3000                   # Start server on port 3000\n";
    echo "  tsk test all                     # Run all tests\n";
    echo "  tsk config get server.port       # Get config value\n";
    echo "  tsk binary compile app.tsk       # Compile to binary\n";
    echo "  tsk parse config.tsk --json      # Parse with JSON output\n\n";
    
    echo colorize("For detailed help on any command:\n", 'cyan');
    echo "  tsk <command> --help\n";
}

function handleVersionCommand() {
    $version = '2.0.0';
    if (file_exists(__DIR__ . '/../VERSION')) {
        $version = trim(file_get_contents(__DIR__ . '/../VERSION'));
    }
    
    if ($GLOBALS['json']) {
        jsonOutput(['version' => $version, 'language' => 'PHP']);
    } else {
        echo colorize("🐘 TuskLang CLI v$version (PHP)\n", 'blue');
        echo colorize("Strong. Secure. Scalable.\n", 'cyan');
    }
}

function handleInteractiveMode() {
    echo colorize("🐘 TuskLang v2.0.0 - Interactive Mode\n", 'blue');
    echo colorize("Type 'exit' to quit\n\n", 'cyan');
    
    while (true) {
        echo colorize("tsk> ", 'green');
        $input = trim(fgets(STDIN));
        
        if (empty($input)) continue;
        if ($input === 'exit' || $input === 'quit') break;
        
        // Parse and execute command
        $args = explode(' ', $input);
        $command = $args[0];
        $subcommand = $args[1] ?? null;
        $commandArgs = array_slice($args, 2);
        
        try {
            // Re-route to main command handler
            $GLOBALS['argv'] = array_merge(['tsk'], $args);
            $GLOBALS['argc'] = count($GLOBALS['argv']);
            
            // This is a simplified approach - in a real implementation,
            // you'd want to properly re-route the command
            echo "Command: $command (interactive mode not fully implemented)\n";
        } catch (Exception $e) {
            echo status('error', $e->getMessage()) . "\n";
        }
    }
    
    echo colorize("Goodbye!\n", 'cyan');
} 