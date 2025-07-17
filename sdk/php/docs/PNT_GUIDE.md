# 🥜 Peanut Binary Configuration Guide for PHP

A comprehensive guide to using TuskLang's high-performance binary configuration system with PHP.

## Table of Contents

1. [Installation and Setup](#installation-and-setup)
2. [Quick Start](#quick-start)
3. [Core Concepts](#core-concepts)
4. [API Reference](#api-reference)
5. [Advanced Usage](#advanced-usage)
6. [PHP-Specific Features](#php-specific-features)
7. [Framework Integration](#framework-integration)
8. [Binary Format Details](#binary-format-details)
9. [Performance Guide](#performance-guide)
10. [Troubleshooting](#troubleshooting)
11. [Migration Guide](#migration-guide)
12. [Complete Examples](#complete-examples)
13. [Quick Reference](#quick-reference)

## What is Peanut Configuration?

Peanut Configuration is TuskLang's high-performance binary configuration system that provides:

- **85% faster loading** compared to text-based formats
- **CSS-like inheritance** with directory hierarchy
- **Cross-language compatibility** using MessagePack serialization
- **Automatic compilation** from human-readable formats
- **Type inference** and validation
- **File watching** for real-time updates

The system supports three file formats:
- `.peanuts` - Simple, human-readable configuration
- `.tsk` - Advanced TuskLang syntax with operators
- `.pnt` - Compiled binary format for production

## Installation and Setup

### Prerequisites

- PHP 8.1 or higher
- TuskLang PHP SDK installed
- PDO extension (for database operations)
- ext-msgpack extension (optional, for enhanced performance)

### Installing the SDK

```bash
# Via Composer (recommended)
composer require cyber-boost/tusktsk

# Manual installation
git clone https://github.com/tuskphp/tusklang
cd tusklang/sdk/php
composer install
```

### Installing MessagePack Extension (Optional)

```bash
# Ubuntu/Debian
sudo apt-get install php-msgpack

# macOS with Homebrew
brew install php-msgpack

# Manual compilation
pecl install msgpack
```

### Importing PeanutConfig

```php
<?php
// Autoload via Composer
require_once 'vendor/autoload.php';

use TuskLang\PeanutConfig;

// Or include directly
require_once 'src/PeanutConfig.php';
```

## Quick Start

### Your First Peanut Configuration

1. Create a `peanu.peanuts` file:

```ini
[app]
name: "My PHP App"
version: "1.0.0"
debug: true

[server]
host: "localhost"
port: 8080
workers: 4

[database]
driver: "mysql"
host: "db.example.com"
port: 3306
name: "myapp"
```

2. Load the configuration:

```php
<?php
use TuskLang\PeanutConfig;

// Get singleton instance
$config = PeanutConfig::getInstance();

// Load configuration from current directory
$data = $config->load();

// Access values
echo $data['app']['name']; // "My PHP App"
echo $data['server']['port']; // 8080
```

3. Use dot notation for nested access:

```php
<?php
// Get specific values
$appName = $config->get('app.name');
$dbHost = $config->get('database.host', 'localhost'); // with default

echo $appName; // "My PHP App"
echo $dbHost; // "db.example.com"
```

## Core Concepts

### File Types

| Format | Extension | Purpose | Performance |
|--------|-----------|---------|-------------|
| Peanuts | `.peanuts` | Human-readable, simple syntax | Standard |
| TuskLang | `.tsk` | Advanced features, operators | Standard |
| Binary | `.pnt` | Compiled, production-ready | 85% faster |

### Hierarchical Loading

PeanutConfig uses CSS-like cascading, loading configuration files from the current directory up to the root:

```
/project/
├── peanu.peanuts          # Project defaults
├── app/
│   ├── peanu.peanuts      # App-specific overrides
│   └── api/
│       └── peanu.peanuts  # API-specific settings
```

```php
<?php
// Load from /project/app/api/
$config = PeanutConfig::getInstance();
$data = $config->load('/project/app/api');

// Values are merged from all levels:
// 1. /project/peanu.peanuts (base)
// 2. /project/app/peanu.peanuts (overrides)
// 3. /project/app/api/peanu.peanuts (final overrides)
```

### Type System

PeanutConfig automatically infers types from values:

```ini
# String values
name: "My App"
description: 'A great application'

# Numeric values
port: 8080
timeout: 30.5

# Boolean values
debug: true
production: false

# Null values
optional_setting: null

# Arrays
allowed_hosts: ["localhost", "127.0.0.1", "*.example.com"]

# Objects
server: {
    host: "localhost",
    port: 8080,
    ssl: true
}
```

## API Reference

### PeanutConfig Class

#### Constructor/Initialization

```php
<?php
use TuskLang\PeanutConfig;

// Get singleton instance (recommended)
$config = PeanutConfig::getInstance();

// The class is a singleton to ensure consistent state
// across your application
```

#### Methods

##### load(directory = '.')

Loads configuration from the specified directory using hierarchical inheritance.

```php
<?php
$config = PeanutConfig::getInstance();

// Load from current directory
$data = $config->load();

// Load from specific directory
$data = $config->load('/path/to/config');

// Load with custom options
$config->autoCompile = false; // Disable auto-compilation
$config->watchMode = false;   // Disable file watching
$data = $config->load();
```

**Parameters:**
- `$directory` (string): Starting directory for configuration search

**Returns:** array - Merged configuration data

**Example:**
```php
<?php
$config = PeanutConfig::getInstance();
$data = $config->load('/var/www/myapp');

if (isset($data['database'])) {
    $dbConfig = $data['database'];
    echo "Database: {$dbConfig['host']}:{$dbConfig['port']}";
}
```

##### get(keyPath, defaultValue = null, directory = '.')

Gets a specific configuration value using dot notation.

```php
<?php
$config = PeanutConfig::getInstance();

// Get simple value
$appName = $config->get('app.name');

// Get with default value
$port = $config->get('server.port', 8080);

// Get from specific directory
$dbHost = $config->get('database.host', 'localhost', '/etc/myapp');
```

**Parameters:**
- `$keyPath` (string): Dot-separated key path (e.g., "server.host")
- `$defaultValue` (mixed): Default value if key not found
- `$directory` (string): Directory to load configuration from

**Returns:** mixed - Configuration value or default

**Example:**
```php
<?php
$config = PeanutConfig::getInstance();

// Nested access
$sslEnabled = $config->get('server.ssl.enabled', false);
$maxConnections = $config->get('database.pool.max_connections', 10);

// Array access
$allowedOrigins = $config->get('cors.allowed_origins', []);
foreach ($allowedOrigins as $origin) {
    echo "Allowed: $origin\n";
}
```

##### compileBinary(inputFile, outputFile)

Compiles a text configuration file to binary format.

```php
<?php
$config = PeanutConfig::getInstance();

// Compile .peanuts to .pnt
$config->compileBinary('config.peanuts', 'config.pnt');

// Compile .tsk to .pnt
$config->compileBinary('app.tsk', 'app.pnt');
```

**Parameters:**
- `$inputFile` (string): Path to input text file
- `$outputFile` (string): Path to output binary file

**Returns:** void

**Example:**
```php
<?php
$config = PeanutConfig::getInstance();

try {
    $config->compileBinary('config.peanuts', 'config.pnt');
    echo "✅ Configuration compiled successfully\n";
} catch (Exception $e) {
    echo "❌ Compilation failed: " . $e->getMessage() . "\n";
}
```

##### loadBinary(path)

Loads a binary configuration file directly.

```php
<?php
$config = PeanutConfig::getInstance();

// Load binary file
$data = $config->loadBinary('config.pnt');

// Access data
echo $data['app']['name'];
```

**Parameters:**
- `$path` (string): Path to binary configuration file

**Returns:** array|null - Configuration data or null if failed

**Example:**
```php
<?php
$config = PeanutConfig::getInstance();

$binaryFile = 'config.pnt';
if (file_exists($binaryFile)) {
    $data = $config->loadBinary($binaryFile);
    if ($data !== null) {
        echo "✅ Binary config loaded: " . count($data) . " sections\n";
    } else {
        echo "❌ Failed to load binary config\n";
    }
}
```

##### invalidateCache()

Clears all cached configuration data.

```php
<?php
$config = PeanutConfig::getInstance();

// Clear cache
$config->invalidateCache();

// Force reload on next access
$data = $config->load();
```

**Returns:** void

##### checkForChanges()

Checks if any watched configuration files have changed.

```php
<?php
$config = PeanutConfig::getInstance();

// Check for changes
if ($config->checkForChanges()) {
    echo "Configuration files have changed, reloading...\n";
    $config->invalidateCache();
    $data = $config->load();
}
```

**Returns:** bool - True if files have changed

## Advanced Usage

### File Watching

PeanutConfig can monitor configuration files for changes:

```php
<?php
$config = PeanutConfig::getInstance();

// Enable file watching (default: true)
$config->watchMode = true;

// Load configuration (sets up watchers)
$data = $config->load();

// In a loop or timer, check for changes
while (true) {
    if ($config->checkForChanges()) {
        echo "Configuration updated, reloading...\n";
        $config->invalidateCache();
        $data = $config->load();
    }
    sleep(5); // Check every 5 seconds
}
```

### Custom Serialization

For advanced use cases, you can customize serialization:

```php
<?php
class CustomPeanutConfig extends PeanutConfig
{
    protected function parseValue(string $value)
    {
        // Custom type parsing
        if (preg_match('/^@env\((.+)\)$/', $value, $matches)) {
            $envVar = trim($matches[1], '"\'');
            return $_ENV[$envVar] ?? null;
        }
        
        return parent::parseValue($value);
    }
}

// Usage
$config = new CustomPeanutConfig();
$data = $config->load();
```

### Performance Optimization

```php
<?php
$config = PeanutConfig::getInstance();

// Disable features for maximum performance
$config->autoCompile = false;  // Disable auto-compilation
$config->watchMode = false;    // Disable file watching

// Use binary files in production
$data = $config->loadBinary('config.pnt');

// Cache configuration object
class ConfigManager
{
    private static $instance = null;
    private $config = null;
    
    public static function getInstance()
    {
        if (self::$instance === null) {
            self::$instance = new self();
        }
        return self::$instance;
    }
    
    private function __construct()
    {
        $this->config = PeanutConfig::getInstance();
        $this->data = $this->config->load();
    }
    
    public function get($key, $default = null)
    {
        return $this->config->get($key, $default);
    }
}

// Usage
$config = ConfigManager::getInstance();
$value = $config->get('app.name');
```

### Thread Safety

PHP's PeanutConfig is thread-safe when used properly:

```php
<?php
// In a multi-threaded environment
class ThreadSafeConfig
{
    private static $lock = null;
    
    public static function get($key, $default = null)
    {
        if (self::$lock === null) {
            self::$lock = new \SplMutex();
        }
        
        self::$lock->synchronized(function() use ($key, $default) {
            $config = PeanutConfig::getInstance();
            return $config->get($key, $default);
        });
    }
}
```

## PHP-Specific Features

### Namespace Support

```php
<?php
namespace MyApp\Config;

use TuskLang\PeanutConfig;

class AppConfig
{
    private $config;
    
    public function __construct()
    {
        $this->config = PeanutConfig::getInstance();
    }
    
    public function getDatabaseConfig(): array
    {
        return [
            'host' => $this->config->get('database.host'),
            'port' => $this->config->get('database.port'),
            'name' => $this->config->get('database.name'),
        ];
    }
}
```

### Type Declarations

```php
<?php
declare(strict_types=1);

use TuskLang\PeanutConfig;

class TypedConfig
{
    private PeanutConfig $config;
    
    public function __construct()
    {
        $this->config = PeanutConfig::getInstance();
    }
    
    public function getPort(): int
    {
        return (int) $this->config->get('server.port', 8080);
    }
    
    public function isDebug(): bool
    {
        return (bool) $this->config->get('app.debug', false);
    }
    
    public function getAllowedHosts(): array
    {
        return (array) $this->config->get('server.allowed_hosts', []);
    }
}
```

### Error Handling

```php
<?php
use TuskLang\PeanutConfig;

class ConfigLoader
{
    private PeanutConfig $config;
    
    public function __construct()
    {
        $this->config = PeanutConfig::getInstance();
    }
    
    public function loadSafely(string $directory): array
    {
        try {
            return $this->config->load($directory);
        } catch (Exception $e) {
            error_log("Failed to load config from $directory: " . $e->getMessage());
            return $this->getDefaultConfig();
        }
    }
    
    public function getWithValidation(string $key, callable $validator, $default = null)
    {
        $value = $this->config->get($key, $default);
        
        if (!$validator($value)) {
            throw new InvalidArgumentException("Invalid value for key: $key");
        }
        
        return $value;
    }
    
    private function getDefaultConfig(): array
    {
        return [
            'app' => ['name' => 'Default App', 'debug' => false],
            'server' => ['host' => 'localhost', 'port' => 8080],
        ];
    }
}
```

## Framework Integration

### Laravel Integration

```php
<?php
// config/peanut.php
return [
    'auto_compile' => env('PEANUT_AUTO_COMPILE', true),
    'watch_mode' => env('PEANUT_WATCH_MODE', false),
    'default_directory' => base_path('config'),
];

// app/Providers/PeanutServiceProvider.php
namespace App\Providers;

use Illuminate\Support\ServiceProvider;
use TuskLang\PeanutConfig;

class PeanutServiceProvider extends ServiceProvider
{
    public function register()
    {
        $this->app->singleton(PeanutConfig::class, function ($app) {
            $config = PeanutConfig::getInstance();
            
            // Configure from Laravel config
            $peanutConfig = config('peanut');
            $config->autoCompile = $peanutConfig['auto_compile'];
            $config->watchMode = $peanutConfig['watch_mode'];
            
            return $config;
        });
    }
    
    public function boot()
    {
        // Load configuration on boot
        $config = app(PeanutConfig::class);
        $data = $config->load(config('peanut.default_directory'));
        
        // Make available globally
        config(['peanut_data' => $data]);
    }
}

// Usage in controllers
class UserController extends Controller
{
    public function index()
    {
        $config = app(PeanutConfig::class);
        $dbConfig = $config->get('database');
        
        return view('users.index', compact('dbConfig'));
    }
}
```

### Symfony Integration

```php
<?php
// config/services.yaml
services:
    TuskLang\PeanutConfig:
        arguments:
            $autoCompile: '%env(bool:PEANUT_AUTO_COMPILE)%'
            $watchMode: '%env(bool:PEANUT_WATCH_MODE)%'

// src/Service/ConfigurationService.php
namespace App\Service;

use TuskLang\PeanutConfig;
use Symfony\Component\DependencyInjection\ParameterBag\ParameterBagInterface;

class ConfigurationService
{
    private PeanutConfig $config;
    private ParameterBagInterface $params;
    
    public function __construct(PeanutConfig $config, ParameterBagInterface $params)
    {
        $this->config = $config;
        $this->params = $params;
    }
    
    public function load(): array
    {
        $configDir = $this->params->get('kernel.project_dir') . '/config';
        return $this->config->load($configDir);
    }
    
    public function get(string $key, $default = null)
    {
        return $this->config->get($key, $default);
    }
}
```

## Binary Format Details

### File Structure

| Offset | Size | Type | Description |
|--------|------|------|-------------|
| 0 | 4 | char[4] | Magic: "PNUT" |
| 4 | 4 | uint32 | Version (little-endian) |
| 8 | 8 | uint64 | Timestamp (little-endian) |
| 16 | 8 | bytes | SHA256 checksum (first 8 bytes) |
| 24 | N | bytes | Serialized data |

### Serialization Format

PHP uses MessagePack for serialization when available, falling back to JSON:

```php
<?php
// MessagePack serialization (if ext-msgpack available)
if (extension_loaded('msgpack')) {
    $serialized = msgpack_pack($configData);
} else {
    $serialized = json_encode($configData);
}

// Writing binary file
$handle = fopen('config.pnt', 'wb');
fwrite($handle, 'PNUT');                    // Magic
fwrite($handle, pack('V', 1));              // Version
fwrite($handle, pack('P', time()));         // Timestamp
fwrite($handle, substr(hash('sha256', $serialized, true), 0, 8)); // Checksum
fwrite($handle, $serialized);               // Data
fclose($handle);
```

### Validation

```php
<?php
class BinaryValidator
{
    const MAGIC = 'PNUT';
    const VERSION = 1;
    
    public static function validate(string $filePath): bool
    {
        if (!file_exists($filePath)) {
            return false;
        }
        
        $handle = fopen($filePath, 'rb');
        
        // Check magic
        $magic = fread($handle, 4);
        if ($magic !== self::MAGIC) {
            fclose($handle);
            return false;
        }
        
        // Check version
        $version = unpack('V', fread($handle, 4))[1];
        if ($version > self::VERSION) {
            fclose($handle);
            return false;
        }
        
        // Skip timestamp
        fseek($handle, 8, SEEK_CUR);
        
        // Read and verify checksum
        $storedChecksum = fread($handle, 8);
        $data = stream_get_contents($handle);
        $calculatedChecksum = substr(hash('sha256', $data, true), 0, 8);
        
        fclose($handle);
        
        return $storedChecksum === $calculatedChecksum;
    }
}
```

## Performance Guide

### Benchmarks

```php
<?php
class PerformanceBenchmark
{
    public static function run()
    {
        $testConfig = [
            'app' => ['name' => 'Test App', 'version' => '1.0.0'],
            'server' => ['host' => 'localhost', 'port' => 8080],
            'database' => ['host' => 'db.example.com', 'port' => 3306],
        ];
        
        $iterations = 10000;
        
        // JSON benchmark
        $start = microtime(true);
        for ($i = 0; $i < $iterations; $i++) {
            json_encode($testConfig);
            json_decode(json_encode($testConfig), true);
        }
        $jsonTime = (microtime(true) - $start) * 1000;
        
        // MessagePack benchmark
        if (extension_loaded('msgpack')) {
            $start = microtime(true);
            for ($i = 0; $i < $iterations; $i++) {
                msgpack_pack($testConfig);
                msgpack_unpack(msgpack_pack($testConfig));
            }
            $msgpackTime = (microtime(true) - $start) * 1000;
            
            $improvement = (($jsonTime - $msgpackTime) / $jsonTime) * 100;
            echo "MessagePack is " . round($improvement) . "% faster than JSON\n";
        }
    }
}
```

### Best Practices

1. **Use .pnt files in production:**
```php
<?php
// Development
$config = PeanutConfig::getInstance();
$data = $config->load();

// Production
$data = $config->loadBinary('config.pnt');
```

2. **Cache configuration objects:**
```php
<?php
class ConfigCache
{
    private static $cache = [];
    
    public static function get(string $key)
    {
        if (!isset(self::$cache[$key])) {
            $config = PeanutConfig::getInstance();
            self::$cache[$key] = $config->get($key);
        }
        return self::$cache[$key];
    }
}
```

3. **Disable features in production:**
```php
<?php
$config = PeanutConfig::getInstance();

if (app()->environment('production')) {
    $config->autoCompile = false;
    $config->watchMode = false;
}
```

4. **Use appropriate file sizes:**
```php
<?php
// For large configurations, split into multiple files
$appConfig = $config->load('config/app');
$dbConfig = $config->load('config/database');
$cacheConfig = $config->load('config/cache');
```

## Troubleshooting

### Common Issues

#### File Not Found

**Problem:** Configuration file not found
```php
<?php
try {
    $config = PeanutConfig::getInstance();
    $data = $config->load('/nonexistent/path');
} catch (Exception $e) {
    echo "Error: " . $e->getMessage() . "\n";
    // Fallback to default configuration
    $data = ['app' => ['name' => 'Default App']];
}
```

#### Checksum Mismatch

**Problem:** Binary file corruption
```php
<?php
$config = PeanutConfig::getInstance();

try {
    $data = $config->loadBinary('config.pnt');
} catch (Exception $e) {
    if (strpos($e->getMessage(), 'Checksum mismatch') !== false) {
        echo "Binary file corrupted, recompiling...\n";
        $config->compileBinary('config.peanuts', 'config.pnt');
        $data = $config->loadBinary('config.pnt');
    } else {
        throw $e;
    }
}
```

#### Performance Issues

**Problem:** Slow configuration loading
```php
<?php
// Enable debug mode to identify bottlenecks
$config = PeanutConfig::getInstance();

// Profile loading time
$start = microtime(true);
$data = $config->load();
$loadTime = (microtime(true) - $start) * 1000;

echo "Configuration loaded in {$loadTime}ms\n";

// If too slow, consider:
// 1. Using binary files
// 2. Reducing file size
// 3. Disabling file watching
// 4. Caching configuration objects
```

### Debug Mode

```php
<?php
// Enable debug logging
error_reporting(E_ALL);
ini_set('display_errors', 1);

$config = PeanutConfig::getInstance();

// Debug configuration loading
$data = $config->load();
echo "Loaded " . count($data) . " configuration sections\n";

// Debug specific values
$value = $config->get('app.name');
echo "app.name = " . var_export($value, true) . "\n";
```

## Migration Guide

### From JSON

```php
<?php
// Old JSON configuration
$jsonConfig = json_decode(file_get_contents('config.json'), true);

// Convert to Peanuts format
$peanutsContent = "";
foreach ($jsonConfig as $section => $values) {
    $peanutsContent .= "[$section]\n";
    foreach ($values as $key => $value) {
        if (is_string($value)) {
            $peanutsContent .= "$key: \"$value\"\n";
        } else {
            $peanutsContent .= "$key: " . json_encode($value) . "\n";
        }
    }
    $peanutsContent .= "\n";
}

file_put_contents('peanu.peanuts', $peanutsContent);

// Use PeanutConfig
$config = PeanutConfig::getInstance();
$data = $config->load();
```

### From YAML

```php
<?php
// Old YAML configuration (requires yaml extension)
$yamlConfig = yaml_parse_file('config.yaml');

// Convert to Peanuts format
$peanutsContent = "";
foreach ($yamlConfig as $section => $values) {
    $peanutsContent .= "[$section]\n";
    foreach ($values as $key => $value) {
        if (is_string($value)) {
            $peanutsContent .= "$key: \"$value\"\n";
        } else {
            $peanutsContent .= "$key: " . json_encode($value) . "\n";
        }
    }
    $peanutsContent .= "\n";
}

file_put_contents('peanu.peanuts', $peanutsContent);
```

### From .env

```php
<?php
// Old .env file
$envContent = file_get_contents('.env');
$envVars = [];

foreach (explode("\n", $envContent) as $line) {
    if (strpos($line, '=') !== false) {
        [$key, $value] = explode('=', $line, 2);
        $envVars[trim($key)] = trim($value);
    }
}

// Convert to Peanuts format
$peanutsContent = "[app]\n";
foreach ($envVars as $key => $value) {
    $peanutsContent .= strtolower($key) . ": \"$value\"\n";
}

file_put_contents('peanu.peanuts', $peanutsContent);
```

## Complete Examples

### Web Application Configuration

**File Structure:**
```
myapp/
├── peanu.peanuts
├── config/
│   ├── peanu.peanuts
│   ├── database.peanuts
│   └── cache.peanuts
├── public/
│   └── index.php
└── src/
    └── Config/
        └── AppConfig.php
```

**peanu.peanuts (root):**
```ini
[app]
name: "My Web App"
version: "1.0.0"
environment: "development"

[server]
host: "0.0.0.0"
port: 8080
workers: 4
```

**config/peanu.peanuts:**
```ini
[app]
environment: "production"

[server]
port: 80
workers: 8

[database]
driver: "mysql"
host: "db.example.com"
port: 3306
name: "myapp"
user: "app_user"
password: "secure_password"

[cache]
driver: "redis"
host: "cache.example.com"
port: 6379
ttl: 3600
```

**src/Config/AppConfig.php:**
```php
<?php
namespace MyApp\Config;

use TuskLang\PeanutConfig;

class AppConfig
{
    private PeanutConfig $config;
    private array $data;
    
    public function __construct()
    {
        $this->config = PeanutConfig::getInstance();
        $this->data = $this->config->load();
    }
    
    public function getDatabaseConfig(): array
    {
        return [
            'driver' => $this->config->get('database.driver'),
            'host' => $this->config->get('database.host'),
            'port' => $this->config->get('database.port'),
            'database' => $this->config->get('database.name'),
            'username' => $this->config->get('database.user'),
            'password' => $this->config->get('database.password'),
        ];
    }
    
    public function getCacheConfig(): array
    {
        return [
            'driver' => $this->config->get('cache.driver'),
            'host' => $this->config->get('cache.host'),
            'port' => $this->config->get('cache.port'),
            'ttl' => $this->config->get('cache.ttl'),
        ];
    }
    
    public function isProduction(): bool
    {
        return $this->config->get('app.environment') === 'production';
    }
    
    public function getServerConfig(): array
    {
        return [
            'host' => $this->config->get('server.host'),
            'port' => $this->config->get('server.port'),
            'workers' => $this->config->get('server.workers'),
        ];
    }
}
```

**public/index.php:**
```php
<?php
require_once '../vendor/autoload.php';

use MyApp\Config\AppConfig;

$config = new AppConfig();

// Use configuration
$dbConfig = $config->getDatabaseConfig();
$serverConfig = $config->getServerConfig();

echo "Starting {$config->data['app']['name']} on {$serverConfig['host']}:{$serverConfig['port']}\n";
```

### Microservice Configuration

**File Structure:**
```
microservice/
├── peanu.peanuts
├── docker/
│   └── peanu.peanuts
├── src/
│   ├── Config/
│   │   └── ServiceConfig.php
│   └── Service.php
└── docker-compose.yml
```

**peanu.peanuts:**
```ini
[service]
name: "user-service"
version: "1.0.0"

[api]
port: 3000
rate_limit: 1000
timeout: 30

[database]
host: "user-db"
port: 5432
name: "users"
pool_size: 10

[redis]
host: "redis"
port: 6379
database: 0

[logging]
level: "info"
format: "json"
```

**src/Config/ServiceConfig.php:**
```php
<?php
namespace UserService\Config;

use TuskLang\PeanutConfig;

class ServiceConfig
{
    private PeanutConfig $config;
    
    public function __construct()
    {
        $this->config = PeanutConfig::getInstance();
        
        // Load configuration based on environment
        $env = $_ENV['APP_ENV'] ?? 'development';
        $configDir = $env === 'docker' ? 'docker' : '.';
        $this->config->load($configDir);
    }
    
    public function getApiConfig(): array
    {
        return [
            'port' => $this->config->get('api.port'),
            'rate_limit' => $this->config->get('api.rate_limit'),
            'timeout' => $this->config->get('api.timeout'),
        ];
    }
    
    public function getDatabaseConfig(): array
    {
        return [
            'host' => $this->config->get('database.host'),
            'port' => $this->config->get('database.port'),
            'database' => $this->config->get('database.name'),
            'pool_size' => $this->config->get('database.pool_size'),
        ];
    }
    
    public function getRedisConfig(): array
    {
        return [
            'host' => $this->config->get('redis.host'),
            'port' => $this->config->get('redis.port'),
            'database' => $this->config->get('redis.database'),
        ];
    }
}
```

### CLI Tool Configuration

**File Structure:**
```
cli-tool/
├── peanu.peanuts
├── src/
│   ├── Config/
│   │   └── CliConfig.php
│   └── Command.php
└── bin/
    └── mycli
```

**peanu.peanuts:**
```ini
[cli]
name: "My CLI Tool"
version: "1.0.0"
description: "A powerful command-line tool"

[output]
format: "text"
colors: true
verbose: false

[commands]
default: "help"
timeout: 30

[logging]
level: "warning"
file: "cli.log"
```

**src/Config/CliConfig.php:**
```php
<?php
namespace MyCli\Config;

use TuskLang\PeanutConfig;

class CliConfig
{
    private PeanutConfig $config;
    
    public function __construct()
    {
        $this->config = PeanutConfig::getInstance();
        $this->config->load();
    }
    
    public function getOutputConfig(): array
    {
        return [
            'format' => $this->config->get('output.format', 'text'),
            'colors' => $this->config->get('output.colors', true),
            'verbose' => $this->config->get('output.verbose', false),
        ];
    }
    
    public function getCommandConfig(): array
    {
        return [
            'default' => $this->config->get('commands.default', 'help'),
            'timeout' => $this->config->get('commands.timeout', 30),
        ];
    }
    
    public function shouldUseColors(): bool
    {
        return $this->config->get('output.colors', true);
    }
    
    public function isVerbose(): bool
    {
        return $this->config->get('output.verbose', false);
    }
}
```

## Quick Reference

### Common Operations

```php
<?php
use TuskLang\PeanutConfig;

// Initialize
$config = PeanutConfig::getInstance();

// Load configuration
$data = $config->load();                    // From current directory
$data = $config->load('/path/to/config');   // From specific directory

// Get values
$value = $config->get('key');               // Simple key
$value = $config->get('section.key');       // Nested key
$value = $config->get('key', 'default');    // With default

// Compile to binary
$config->compileBinary('config.peanuts', 'config.pnt');

// Load binary
$data = $config->loadBinary('config.pnt');

// Cache management
$config->invalidateCache();                 // Clear cache
$changed = $config->checkForChanges();      // Check for updates

// Configuration options
$config->autoCompile = false;               // Disable auto-compilation
$config->watchMode = false;                 // Disable file watching
```

### File Format Examples

**peanu.peanuts:**
```ini
[app]
name: "My App"
version: "1.0.0"
debug: true

[server]
host: "localhost"
port: 8080
ssl: true

[database]
host: "db.example.com"
port: 3306
name: "myapp"
pool_size: 10

[features]
api_v2: true
websockets: false
caching: true
```

**peanu.tsk (Advanced):**
```tsk
# Global variables
$app_name: "My Advanced App"
$environment: @env("APP_ENV", "development")

[app]
name: $app_name
version: "1.0.0"
debug: $environment != "production"

[server]
host: @env("SERVER_HOST", "0.0.0.0")
port: @env("SERVER_PORT", 8080)
workers: $environment == "production" ? 8 : 2

[database]
host: @env("DB_HOST", "localhost")
port: @env("DB_PORT", 3306)
name: @env("DB_NAME", "myapp")
```

### Error Handling Patterns

```php
<?php
class SafeConfigLoader
{
    public static function load(string $directory): array
    {
        try {
            $config = PeanutConfig::getInstance();
            return $config->load($directory);
        } catch (Exception $e) {
            error_log("Config loading failed: " . $e->getMessage());
            return self::getDefaultConfig();
        }
    }
    
    public static function get(string $key, $default = null)
    {
        try {
            $config = PeanutConfig::getInstance();
            return $config->get($key, $default);
        } catch (Exception $e) {
            error_log("Config access failed for key '$key': " . $e->getMessage());
            return $default;
        }
    }
    
    private static function getDefaultConfig(): array
    {
        return [
            'app' => ['name' => 'Default App', 'debug' => false],
            'server' => ['host' => 'localhost', 'port' => 8080],
        ];
    }
}
```

---

**Strong. Secure. Scalable.** 🐘

This guide provides everything you need to use Peanut Configuration effectively in your PHP applications. For more information, visit the [TuskLang documentation](https://docs.tusklang.org). 