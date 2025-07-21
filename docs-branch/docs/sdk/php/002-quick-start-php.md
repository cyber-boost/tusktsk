# 🚀 TuskLang PHP Quick Start Guide

**"We don't bow to any king" - PHP Edition**

Welcome to the fastest way to get started with TuskLang in PHP! This guide will show you how to revolutionize your configuration management with practical, real-world examples.

## ⚡ 5-Minute Setup

### 1. Install TuskLang

```bash
# Install via Composer
composer require cyber-boost/tusktsk
```

### 2. Create Your First TSK File

```php
<?php
// config/app.tsk
app_name: "MyAwesomeApp"
version: "2.0.0"
environment: @env("APP_ENV", "development")

[database]
host: "localhost"
port: 5432
name: "myapp"
user: @env("DB_USER", "postgres")
password: @env("DB_PASSWORD", "secret")

[server]
host: "0.0.0.0"
port: @if($environment == "production", 80, 8080)
workers: @if($environment == "production", 4, 1)

[features]
debug: @if($environment != "production", true, false)
cache_enabled: true
rate_limiting: true
```

### 3. Parse and Use

```php
<?php

require_once 'vendor/autoload.php';

use TuskLang\TuskLang;

// Create parser instance
$parser = new TuskLang();

// Parse your configuration
$config = $parser->parseFile('config/app.tsk');

// Use the configuration
echo "App: {$config['app_name']} v{$config['version']}\n";
echo "Database: {$config['database']['host']}:{$config['database']['port']}\n";
echo "Server: {$config['server']['host']}:{$config['server']['port']}\n";
```

## 🎯 Core Features in Action

### 1. Database Queries in Configuration

```php
<?php
// config/stats.tsk
[analytics]
total_users: @query("SELECT COUNT(*) FROM users")
active_users: @query("SELECT COUNT(*) FROM users WHERE last_login > ?", @date.subtract("7d"))
revenue_today: @query("SELECT SUM(amount) FROM orders WHERE created_at >= ?", @date.today())

[system]
memory_usage: @php("memory_get_usage(true)")
disk_free: @php("disk_free_space('/')")
load_average: @php("sys_getloadavg()[0]")
```

```php
<?php

use TuskLang\TuskLang;
use TuskLang\Adapters\PostgreSQLAdapter;

// Set up database adapter
$db = new PostgreSQLAdapter([
    'host' => 'localhost',
    'port' => 5432,
    'database' => 'myapp',
    'user' => 'postgres',
    'password' => 'secret'
]);

$parser = new TuskLang();
$parser->setDatabaseAdapter($db);

// Parse configuration with live database queries
$stats = $parser->parseFile('config/stats.tsk');

echo "Total users: {$stats['analytics']['total_users']}\n";
echo "Active users: {$stats['analytics']['active_users']}\n";
echo "Revenue today: \${$stats['analytics']['revenue_today']}\n";
echo "Memory usage: " . number_format($stats['system']['memory_usage']) . " bytes\n";
```

### 2. Cross-File Communication

```php
<?php
// config/database.tsk
db_host: "localhost"
db_port: 5432
db_name: "myapp"
db_user: "postgres"
db_password: @env("DB_PASSWORD", "secret")

// config/app.tsk
app_name: "MyApp"
version: "1.0.0"

[database]
host: @config.database.tsk.get("db_host")
port: @config.database.tsk.get("db_port")
name: @config.database.tsk.get("db_name")
user: @config.database.tsk.get("db_user")
password: @config.database.tsk.get("db_password")
```

```php
<?php

use TuskLang\TuskLang;

$parser = new TuskLang();

// Link configuration files
$parser->linkFile('config/database.tsk', file_get_contents('config/database.tsk'));

// Parse main configuration
$config = $parser->parseFile('config/app.tsk');

echo "Database: {$config['database']['host']}:{$config['database']['port']}\n";
```

### 3. Dynamic Configuration with @ Operators

```php
<?php
// config/dynamic.tsk
$current_time: @date.now()
$user_agent: @request.headers.User-Agent
$client_ip: @request.ip

[logging]
timestamp: @date("Y-m-d H:i:s")
level: @if(@env("DEBUG", "false") == "true", "debug", "info")
file: "/var/log/app-@date('Y-m-d').log"

[security]
allowed_ips: ["127.0.0.1", "192.168.1.0/24"]
is_allowed: @security.allowed_ips.includes($client_ip)
user_agent_safe: @validate.user_agent($user_agent)
```

```php
<?php

use TuskLang\TuskLang;

$parser = new TuskLang();

// Set request context
$context = [
    'request' => [
        'ip' => $_SERVER['REMOTE_ADDR'] ?? '127.0.0.1',
        'headers' => [
            'User-Agent' => $_SERVER['HTTP_USER_AGENT'] ?? 'Unknown'
        ]
    ]
];

// Parse with context
$config = $parser->parseWithContext(file_get_contents('config/dynamic.tsk'), $context);

echo "Current time: {$config['logging']['timestamp']}\n";
echo "Client IP: {$config['security']['is_allowed'] ? 'Allowed' : 'Blocked'}\n";
```

### 4. Conditional Logic and Environment-Specific Config

```php
<?php
// config/environment.tsk
$environment: @env("APP_ENV", "development")
$is_production: @if($environment == "production", true, false)
$is_staging: @if($environment == "staging", true, false)
$is_development: @if($environment == "development", true, false)

[app]
debug: @if($is_development, true, false)
log_level: @if($is_production, "error", @if($is_staging, "warning", "debug"))
cache_driver: @if($is_production, "redis", "file")

[database]
host: @if($is_production, "prod-db.example.com", "localhost")
port: @if($is_production, 5432, 5432)
ssl_mode: @if($is_production, "require", "disable")
connection_pool: @if($is_production, 20, 5)

[security]
https_only: @if($is_production, true, false)
cors_origin: @if($is_production, ["https://myapp.com"], ["*"])
session_secure: @if($is_production, true, false)
```

```php
<?php

use TuskLang\TuskLang;

// Set environment
putenv('APP_ENV=production');

$parser = new TuskLang();
$config = $parser->parseFile('config/environment.tsk');

echo "Environment: " . $config['$environment'] . "\n";
echo "Debug mode: " . ($config['app']['debug'] ? 'Enabled' : 'Disabled') . "\n";
echo "Database host: {$config['database']['host']}\n";
echo "HTTPS only: " . ($config['security']['https_only'] ? 'Yes' : 'No') . "\n";
```

## 🏗️ Framework Integration Examples

### Laravel Integration

```php
<?php
// app/Providers/TuskLangServiceProvider.php
namespace App\Providers;

use Illuminate\Support\ServiceProvider;
use TuskLang\TuskLang;
use TuskLang\Adapters\PostgreSQLAdapter;

class TuskLangServiceProvider extends ServiceProvider
{
    public function register()
    {
        $this->app->singleton(TuskLang::class, function ($app) {
            $parser = new TuskLang();
            
            // Configure database adapter
            $dbConfig = config('database.connections.pgsql');
            $adapter = new PostgreSQLAdapter([
                'host' => $dbConfig['host'],
                'port' => $dbConfig['port'],
                'database' => $dbConfig['database'],
                'user' => $dbConfig['username'],
                'password' => $dbConfig['password'],
            ]);
            
            $parser->setDatabaseAdapter($adapter);
            return $parser;
        });
    }
}
```

```php
<?php
// app/Http/Controllers/ConfigController.php
namespace App\Http\Controllers;

use Illuminate\Http\Request;
use TuskLang\TuskLang;

class ConfigController extends Controller
{
    public function index(TuskLang $parser)
    {
        $config = $parser->parseFile('config/app.tsk');
        return response()->json($config);
    }
    
    public function stats(TuskLang $parser)
    {
        $stats = $parser->parseFile('config/stats.tsk');
        return response()->json($stats);
    }
}
```

### Symfony Integration

```php
<?php
// src/Controller/ConfigController.php
namespace App\Controller;

use Symfony\Bundle\FrameworkBundle\Controller\AbstractController;
use Symfony\Component\HttpFoundation\JsonResponse;
use Symfony\Component\Routing\Annotation\Route;
use TuskLang\TuskLang;

class ConfigController extends AbstractController
{
    #[Route('/config', name: 'app_config')]
    public function index(TuskLang $parser): JsonResponse
    {
        $config = $parser->parseFile('config/app.tsk');
        return $this->json($config);
    }
    
    #[Route('/config/stats', name: 'app_config_stats')]
    public function stats(TuskLang $parser): JsonResponse
    {
        $stats = $parser->parseFile('config/stats.tsk');
        return $this->json($stats);
    }
}
```

## 🔧 Advanced Features

### 1. Caching and Performance

```php
<?php

use TuskLang\TuskLang;

$parser = new TuskLang();

// Enable caching
$parser->enableCache(true);
$parser->setCacheDirectory('/tmp/tusklang-cache');
$parser->setCacheTTL(300); // 5 minutes

// Parse with caching
$config = $parser->parseFile('config/app.tsk');
```

### 2. Custom Validators

```php
<?php

use TuskLang\TuskLang;

$parser = new TuskLang();

// Add custom validators
$parser->addValidator('strong_password', function($password) {
    return strlen($password) >= 8 && 
           preg_match('/[A-Z]/', $password) && 
           preg_match('/[a-z]/', $password) && 
           preg_match('/[0-9]/', $password) &&
           preg_match('/[^A-Za-z0-9]/', $password);
});

$parser->addValidator('valid_email', function($email) {
    return filter_var($email, FILTER_VALIDATE_EMAIL) !== false;
});

// Use in configuration
$config = '
[user]
email: @validate.valid_email(@request.email)
password: @validate.strong_password(@request.password)
';

$data = $parser->parse($config);
```

### 3. Error Handling

```php
<?php

use TuskLang\TuskLang;
use TuskLang\Exceptions\ParseException;
use TuskLang\Exceptions\ValidationException;

$parser = new TuskLang();

try {
    $config = $parser->parseFile('config/app.tsk');
    echo "Configuration loaded successfully\n";
} catch (ParseException $e) {
    echo "Parse error: " . $e->getMessage() . "\n";
    echo "Line: " . $e->getLine() . "\n";
} catch (ValidationException $e) {
    echo "Validation error: " . $e->getMessage() . "\n";
} catch (Exception $e) {
    echo "Unexpected error: " . $e->getMessage() . "\n";
}
```

## 🚀 CLI Usage

```bash
# Parse a TSK file
php tusk.php parse config/app.tsk

# Validate syntax
php tusk.php validate config/app.tsk

# Generate PHP classes from TSK
php tusk.php generate config/app.tsk --type php

# Convert to JSON
php tusk.php convert config/app.tsk --format json

# Interactive shell
php tusk.php shell config/app.tsk
```

## 📊 Performance Benchmarks

```php
<?php

use TuskLang\TuskLang;

$parser = new TuskLang();

// Benchmark parsing
$start = microtime(true);
$config = $parser->parseFile('config/app.tsk');
$end = microtime(true);

echo "Parse time: " . round(($end - $start) * 1000, 2) . "ms\n";
echo "Memory usage: " . number_format(memory_get_peak_usage(true)) . " bytes\n";
```

## 🔐 Security Best Practices

```php
<?php
// config/secure.tsk
[secrets]
api_key: @env.secure("API_KEY")
database_password: @env.secure("DB_PASSWORD")
encryption_key: @env.secure("ENCRYPTION_KEY")

[validation]
required_fields: ["api_key", "database_password"]
allowed_origins: @validate.origins(@request.headers.Origin)
csrf_token: @validate.csrf(@request.headers.X-CSRF-Token)
```

## 📚 Next Steps

Now that you've mastered the basics, explore:

1. **Advanced @ Operators** - Master the full operator system
2. **Database Integration** - Build complex queries in configuration
3. **Framework Integration** - Integrate with your favorite PHP framework
4. **Performance Optimization** - Optimize for production use
5. **Security Features** - Implement secure configuration practices

## 🆘 Need Help?

- **Documentation**: [https://docs.tusklang.org/php](https://docs.tusklang.org/php)
- **Examples**: [https://github.com/cyber-boost/php-examples](https://github.com/cyber-boost/php-examples)
- **Community**: [https://community.tusklang.org](https://community.tusklang.org)

---

**Ready to revolutionize your PHP configuration? You're now a TuskLang master! 🚀** 