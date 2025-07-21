# 🏗️ TuskLang PHP Framework Integration Guide

**"We don't bow to any king" - PHP Edition**

Master TuskLang integration with every major PHP framework! This guide shows you how to seamlessly integrate TuskLang's revolutionary configuration capabilities into Laravel, Symfony, CodeIgniter, and other popular PHP frameworks.

## 🎯 Framework Integration Overview

TuskLang integrates seamlessly with all major PHP frameworks, providing revolutionary configuration capabilities while maintaining framework conventions and best practices.

```php
<?php
// config/framework-overview.tsk
[integration_types]
laravel: "Service Provider + Facade"
symfony: "Service Container + Bundle"
codeigniter: "Config Class + Helper"
slim: "Middleware + Container"
lumen: "Service Provider + Facade"
```

## 🦄 Laravel Integration

### Service Provider Setup

```php
<?php
// app/Providers/TuskLangServiceProvider.php
namespace App\Providers;

use Illuminate\Support\ServiceProvider;
use TuskLang\TuskLang;
use TuskLang\Adapters\PostgreSQLAdapter;
use TuskLang\Cache\RedisCache;

class TuskLangServiceProvider extends ServiceProvider
{
    public function register()
    {
        $this->app->singleton(TuskLang::class, function ($app) {
            $parser = new TuskLang();
            
            // Configure database adapter using Laravel's config
            $dbConfig = config('database.connections.pgsql');
            $adapter = new PostgreSQLAdapter([
                'host' => $dbConfig['host'],
                'port' => $dbConfig['port'],
                'database' => $dbConfig['database'],
                'user' => $dbConfig['username'],
                'password' => $dbConfig['password'],
            ]);
            
            $parser->setDatabaseAdapter($adapter);
            
            // Configure cache using Laravel's Redis
            if (config('cache.default') === 'redis') {
                $redisConfig = config('database.connections.redis.default');
                $redisCache = new RedisCache([
                    'host' => $redisConfig['host'],
                    'port' => $redisConfig['port'],
                    'db' => $redisConfig['database'] ?? 0,
                ]);
                $parser->setCacheBackend($redisCache);
            }
            
            return $parser;
        });
        
        // Register facade
        $this->app->alias(TuskLang::class, 'tusklang');
    }
    
    public function boot()
    {
        // Publish configuration
        $this->publishes([
            __DIR__.'/../../config/tusklang.php' => config_path('tusklang.php'),
        ], 'tusklang-config');
        
        // Load TuskLang configuration
        $this->loadTuskLangConfig();
    }
    
    private function loadTuskLangConfig()
    {
        $parser = app(TuskLang::class);
        
        // Load environment-specific configuration
        $environment = app()->environment();
        $configFile = "config/environments/{$environment}.tsk";
        
        if (file_exists($configFile)) {
            $config = $parser->parseFile($configFile);
            
            // Merge with Laravel's config
            foreach ($config as $key => $value) {
                config([$key => $value]);
            }
        }
    }
}
```

### Configuration Publishing

```php
<?php
// config/tusklang.php
return [
    'default_adapter' => env('TUSKLANG_DB_ADAPTER', 'sqlite'),
    'adapters' => [
        'sqlite' => [
            'database' => storage_path('tusklang.db'),
        ],
        'postgres' => [
            'host' => env('DB_HOST', 'localhost'),
            'port' => env('DB_PORT', 5432),
            'database' => env('DB_DATABASE'),
            'username' => env('DB_USERNAME'),
            'password' => env('DB_PASSWORD'),
        ],
        'mysql' => [
            'host' => env('DB_HOST', 'localhost'),
            'port' => env('DB_PORT', 3306),
            'database' => env('DB_DATABASE'),
            'username' => env('DB_USERNAME'),
            'password' => env('DB_PASSWORD'),
        ],
    ],
    'cache' => [
        'enabled' => env('TUSKLANG_CACHE_ENABLED', true),
        'ttl' => env('TUSKLANG_CACHE_TTL', 300),
        'backend' => env('TUSKLANG_CACHE_BACKEND', 'redis'),
    ],
    'security' => [
        'encryption_key' => env('TUSKLANG_ENCRYPTION_KEY'),
        'validation_enabled' => env('TUSKLANG_VALIDATION_ENABLED', true),
    ],
];
```

### Facade Usage

```php
<?php
// app/Facades/TuskLang.php
namespace App\Facades;

use Illuminate\Support\Facades\Facade;

class TuskLang extends Facade
{
    protected static function getFacadeAccessor()
    {
        return 'tusklang';
    }
}
```

### Controller Integration

```php
<?php
// app/Http/Controllers/ConfigController.php
namespace App\Http\Controllers;

use Illuminate\Http\Request;
use App\Facades\TuskLang;

class ConfigController extends Controller
{
    public function index()
    {
        $config = TuskLang::parseFile('config/app.tsk');
        return response()->json($config);
    }
    
    public function stats()
    {
        $stats = TuskLang::parseFile('config/stats.tsk');
        return response()->json($stats);
    }
    
    public function dynamic(Request $request)
    {
        $context = [
            'request' => [
                'user_id' => $request->user()->id,
                'ip' => $request->ip(),
                'user_agent' => $request->userAgent(),
            ]
        ];
        
        $config = TuskLang::parseWithContext(
            file_get_contents('config/dynamic.tsk'), 
            $context
        );
        
        return response()->json($config);
    }
}
```

### Blade Integration

```php
<?php
// app/Providers/AppServiceProvider.php
namespace App\Providers;

use Illuminate\Support\ServiceProvider;
use Illuminate\Support\Facades\Blade;
use App\Facades\TuskLang;

class AppServiceProvider extends ServiceProvider
{
    public function boot()
    {
        // Blade directive for TuskLang
        Blade::directive('tusklang', function ($expression) {
            return "<?php echo App\Facades\TuskLang::parseFile({$expression}); ?>";
        });
        
        // Blade directive for TuskLang config
        Blade::directive('tuskconfig', function ($expression) {
            return "<?php echo App\Facades\TuskLang::getConfig({$expression}); ?>";
        });
    }
}
```

```blade
{{-- resources/views/dashboard.blade.php --}}
@extends('layouts.app')

@section('content')
<div class="container">
    <h1>Dashboard</h1>
    
    @tusklang('config/dashboard.tsk')
    
    <div class="stats">
        <p>Total Users: {{ @tuskconfig('analytics.total_users') }}</p>
        <p>Active Users: {{ @tuskconfig('analytics.active_users') }}</p>
        <p>Revenue: ${{ @tuskconfig('analytics.revenue') }}</p>
    </div>
</div>
@endsection
```

### Artisan Commands

```php
<?php
// app/Console/Commands/TuskLangCommand.php
namespace App\Console\Commands;

use Illuminate\Console\Command;
use App\Facades\TuskLang;

class TuskLangCommand extends Command
{
    protected $signature = 'tusk:parse {file} {--format=json}';
    protected $description = 'Parse a TuskLang configuration file';

    public function handle()
    {
        $file = $this->argument('file');
        $format = $this->option('format');
        
        try {
            $config = TuskLang::parseFile($file);
            
            if ($format === 'json') {
                $this->line(json_encode($config, JSON_PRETTY_PRINT));
            } else {
                $this->table(['Key', 'Value'], $this->flattenConfig($config));
            }
        } catch (\Exception $e) {
            $this->error("Error parsing {$file}: " . $e->getMessage());
            return 1;
        }
    }
    
    private function flattenConfig($config, $prefix = '')
    {
        $result = [];
        foreach ($config as $key => $value) {
            $fullKey = $prefix ? "{$prefix}.{$key}" : $key;
            
            if (is_array($value)) {
                $result = array_merge($result, $this->flattenConfig($value, $fullKey));
            } else {
                $result[] = [$fullKey, is_bool($value) ? ($value ? 'true' : 'false') : $value];
            }
        }
        return $result;
    }
}
```

## 🎭 Symfony Integration

### Bundle Setup

```php
<?php
// src/TuskLangBundle/TuskLangBundle.php
namespace App\TuskLangBundle;

use Symfony\Component\HttpKernel\Bundle\Bundle;

class TuskLangBundle extends Bundle
{
    public function getPath(): string
    {
        return \dirname(__DIR__);
    }
}
```

### Service Configuration

```yaml
# config/services.yaml
services:
    TuskLang\TuskLang:
        arguments:
            $databaseAdapter: '@tusklang.database_adapter'
            $cacheBackend: '@tusklang.cache_backend'
    
    tusklang.database_adapter:
        class: TuskLang\Adapters\PostgreSQLAdapter
        arguments:
            $config:
                host: '%env(DB_HOST)%'
                port: '%env(DB_PORT)%'
                database: '%env(DB_NAME)%'
                user: '%env(DB_USER)%'
                password: '%env(DB_PASSWORD)%'
    
    tusklang.cache_backend:
        class: TuskLang\Cache\RedisCache
        arguments:
            $config:
                host: '%env(REDIS_HOST)%'
                port: '%env(REDIS_PORT)%'
                db: '%env(REDIS_DB)%'
    
    tusklang.parser:
        class: TuskLang\TuskLang
        calls:
            - method: setDatabaseAdapter
              arguments: ['@tusklang.database_adapter']
            - method: setCacheBackend
              arguments: ['@tusklang.cache_backend']
```

### Controller Integration

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
    
    #[Route('/config/dynamic', name: 'app_config_dynamic', methods: ['POST'])]
    public function dynamic(TuskLang $parser, Request $request): JsonResponse
    {
        $context = [
            'request' => [
                'user_id' => $this->getUser()->getId(),
                'ip' => $request->getClientIp(),
                'user_agent' => $request->headers->get('User-Agent'),
            ]
        ];
        
        $config = $parser->parseWithContext(
            file_get_contents('config/dynamic.tsk'), 
            $context
        );
        
        return $this->json($config);
    }
}
```

### Twig Extension

```php
<?php
// src/Twig/TuskLangExtension.php
namespace App\Twig;

use Twig\Extension\AbstractExtension;
use Twig\TwigFunction;
use TuskLang\TuskLang;

class TuskLangExtension extends AbstractExtension
{
    private TuskLang $parser;
    
    public function __construct(TuskLang $parser)
    {
        $this->parser = $parser;
    }
    
    public function getFunctions(): array
    {
        return [
            new TwigFunction('tusklang', [$this, 'parseFile']),
            new TwigFunction('tuskconfig', [$this, 'getConfig']),
        ];
    }
    
    public function parseFile(string $file): array
    {
        return $this->parser->parseFile($file);
    }
    
    public function getConfig(string $key, $default = null)
    {
        $config = $this->parser->getConfig();
        return $this->getNestedValue($config, $key, $default);
    }
    
    private function getNestedValue(array $array, string $key, $default = null)
    {
        $keys = explode('.', $key);
        $value = $array;
        
        foreach ($keys as $k) {
            if (!isset($value[$k])) {
                return $default;
            }
            $value = $value[$k];
        }
        
        return $value;
    }
}
```

### Console Commands

```php
<?php
// src/Command/TuskLangParseCommand.php
namespace App\Command;

use Symfony\Component\Console\Command\Command;
use Symfony\Component\Console\Input\InputArgument;
use Symfony\Component\Console\Input\InputInterface;
use Symfony\Component\Console\Input\InputOption;
use Symfony\Component\Console\Output\OutputInterface;
use Symfony\Component\Console\Style\SymfonyStyle;
use TuskLang\TuskLang;

class TuskLangParseCommand extends Command
{
    protected static $defaultName = 'tusk:parse';
    protected static $defaultDescription = 'Parse a TuskLang configuration file';

    private TuskLang $parser;

    public function __construct(TuskLang $parser)
    {
        parent::__construct();
        $this->parser = $parser;
    }

    protected function configure()
    {
        $this
            ->addArgument('file', InputArgument::REQUIRED, 'The TuskLang file to parse')
            ->addOption('format', 'f', InputOption::VALUE_OPTIONAL, 'Output format (json, table)', 'table')
            ->addOption('output', 'o', InputOption::VALUE_OPTIONAL, 'Output file');
    }

    protected function execute(InputInterface $input, OutputInterface $output): int
    {
        $io = new SymfonyStyle($input, $output);
        $file = $input->getArgument('file');
        $format = $input->getOption('format');
        $outputFile = $input->getOption('output');

        try {
            $config = $this->parser->parseFile($file);
            
            if ($format === 'json') {
                $result = json_encode($config, JSON_PRETTY_PRINT);
            } else {
                $result = $this->formatTable($config);
            }
            
            if ($outputFile) {
                file_put_contents($outputFile, $result);
                $io->success("Configuration written to {$outputFile}");
            } else {
                $io->text($result);
            }
            
            return Command::SUCCESS;
        } catch (\Exception $e) {
            $io->error("Error parsing {$file}: " . $e->getMessage());
            return Command::FAILURE;
        }
    }
    
    private function formatTable(array $config): string
    {
        $rows = [];
        $this->flattenConfig($config, '', $rows);
        
        $table = "Configuration:\n";
        $table .= str_repeat('-', 50) . "\n";
        
        foreach ($rows as [$key, $value]) {
            $table .= sprintf("%-30s %s\n", $key, $value);
        }
        
        return $table;
    }
    
    private function flattenConfig(array $config, string $prefix, array &$rows): void
    {
        foreach ($config as $key => $value) {
            $fullKey = $prefix ? "{$prefix}.{$key}" : $key;
            
            if (is_array($value)) {
                $this->flattenConfig($value, $fullKey, $rows);
            } else {
                $rows[] = [$fullKey, is_bool($value) ? ($value ? 'true' : 'false') : $value];
            }
        }
    }
}
```

## 🔥 CodeIgniter Integration

### Config Class

```php
<?php
// app/Config/TuskLang.php
namespace Config;

use CodeIgniter\Config\BaseConfig;
use TuskLang\TuskLang;
use TuskLang\Adapters\PostgreSQLAdapter;

class TuskLang extends BaseConfig
{
    public $defaultAdapter = 'sqlite';
    public $adapters = [
        'sqlite' => [
            'database' => WRITEPATH . 'tusklang.db',
        ],
        'postgres' => [
            'host' => 'localhost',
            'port' => 5432,
            'database' => 'myapp',
            'username' => 'postgres',
            'password' => 'secret',
        ],
        'mysql' => [
            'host' => 'localhost',
            'port' => 3306,
            'database' => 'myapp',
            'username' => 'root',
            'password' => 'secret',
        ],
    ];
    
    public $cache = [
        'enabled' => true,
        'ttl' => 300,
        'backend' => 'file',
    ];
    
    public $security = [
        'encryption_key' => null,
        'validation_enabled' => true,
    ];
    
    private static $instance = null;
    
    public static function getInstance(): TuskLang
    {
        if (self::$instance === null) {
            $config = new self();
            $parser = new \TuskLang\TuskLang();
            
            // Configure database adapter
            $adapterConfig = $config->adapters[$config->defaultAdapter];
            $adapter = new PostgreSQLAdapter($adapterConfig);
            $parser->setDatabaseAdapter($adapter);
            
            // Configure cache
            if ($config->cache['enabled']) {
                $cacheBackend = $config->getCacheBackend();
                $parser->setCacheBackend($cacheBackend);
            }
            
            self::$instance = $parser;
        }
        
        return self::$instance;
    }
    
    private function getCacheBackend()
    {
        switch ($this->cache['backend']) {
            case 'redis':
                return new \TuskLang\Cache\RedisCache([
                    'host' => 'localhost',
                    'port' => 6379,
                    'db' => 0,
                ]);
            case 'file':
            default:
                return new \TuskLang\Cache\FileCache(WRITEPATH . 'cache/tusklang');
        }
    }
}
```

### Controller Integration

```php
<?php
// app/Controllers/Config.php
namespace App\Controllers;

use CodeIgniter\Controller;
use Config\TuskLang as TuskLangConfig;

class Config extends Controller
{
    public function index()
    {
        $parser = TuskLangConfig::getInstance();
        $config = $parser->parseFile('config/app.tsk');
        
        return $this->response->setJSON($config);
    }
    
    public function stats()
    {
        $parser = TuskLangConfig::getInstance();
        $stats = $parser->parseFile('config/stats.tsk');
        
        return $this->response->setJSON($stats);
    }
    
    public function dynamic()
    {
        $parser = TuskLangConfig::getInstance();
        
        $context = [
            'request' => [
                'user_id' => session()->get('user_id'),
                'ip' => $this->request->getIPAddress(),
                'user_agent' => $this->request->getUserAgent()->getAgentString(),
            ]
        ];
        
        $config = $parser->parseWithContext(
            file_get_contents('config/dynamic.tsk'), 
            $context
        );
        
        return $this->response->setJSON($config);
    }
}
```

### Helper Functions

```php
<?php
// app/Helpers/tusklang_helper.php
<?php

if (!function_exists('tusklang_parse')) {
    function tusklang_parse(string $file): array
    {
        $parser = \Config\TuskLang::getInstance();
        return $parser->parseFile($file);
    }
}

if (!function_exists('tusklang_get')) {
    function tusklang_get(string $key, $default = null)
    {
        $parser = \Config\TuskLang::getInstance();
        $config = $parser->getConfig();
        
        $keys = explode('.', $key);
        $value = $config;
        
        foreach ($keys as $k) {
            if (!isset($value[$k])) {
                return $default;
            }
            $value = $value[$k];
        }
        
        return $value;
    }
}

if (!function_exists('tusklang_config')) {
    function tusklang_config(string $file, string $key = null, $default = null)
    {
        $config = tusklang_parse($file);
        
        if ($key === null) {
            return $config;
        }
        
        return tusklang_get($key, $default);
    }
}
```

### View Integration

```php
<!-- app/Views/dashboard.php -->
<!DOCTYPE html>
<html>
<head>
    <title>Dashboard</title>
</head>
<body>
    <h1>Dashboard</h1>
    
    <?php
    $config = tusklang_parse('config/dashboard.tsk');
    ?>
    
    <div class="stats">
        <p>Total Users: <?= tusklang_get('analytics.total_users', 0) ?></p>
        <p>Active Users: <?= tusklang_get('analytics.active_users', 0) ?></p>
        <p>Revenue: $<?= tusklang_get('analytics.revenue', 0) ?></p>
    </div>
    
    <div class="system-info">
        <p>Memory Usage: <?= tusklang_get('system.memory_usage', 'Unknown') ?></p>
        <p>Load Average: <?= tusklang_get('system.load_average', 'Unknown') ?></p>
    </div>
</body>
</html>
```

## 🎯 Slim Framework Integration

### Container Configuration

```php
<?php
// config/container.php
use DI\ContainerBuilder;
use TuskLang\TuskLang;
use TuskLang\Adapters\PostgreSQLAdapter;

return function (ContainerBuilder $containerBuilder) {
    $containerBuilder->addDefinitions([
        TuskLang::class => function () {
            $parser = new TuskLang();
            
            // Configure database adapter
            $adapter = new PostgreSQLAdapter([
                'host' => $_ENV['DB_HOST'] ?? 'localhost',
                'port' => $_ENV['DB_PORT'] ?? 5432,
                'database' => $_ENV['DB_NAME'] ?? 'myapp',
                'user' => $_ENV['DB_USER'] ?? 'postgres',
                'password' => $_ENV['DB_PASSWORD'] ?? 'secret',
            ]);
            
            $parser->setDatabaseAdapter($adapter);
            
            return $parser;
        },
    ]);
};
```

### Middleware Integration

```php
<?php
// src/Middleware/TuskLangMiddleware.php
namespace App\Middleware;

use Psr\Http\Message\ServerRequestInterface as Request;
use Psr\Http\Server\RequestHandlerInterface as RequestHandler;
use Slim\Psr7\Response;
use TuskLang\TuskLang;

class TuskLangMiddleware
{
    private TuskLang $parser;
    
    public function __construct(TuskLang $parser)
    {
        $this->parser = $parser;
    }
    
    public function __invoke(Request $request, RequestHandler $handler): Response
    {
        // Load configuration based on route
        $route = $request->getAttribute('route');
        if ($route) {
            $routeName = $route->getName();
            $configFile = "config/routes/{$routeName}.tsk";
            
            if (file_exists($configFile)) {
                $config = $this->parser->parseFile($configFile);
                
                // Add configuration to request attributes
                $request = $request->withAttribute('tusklang_config', $config);
            }
        }
        
        return $handler->handle($request);
    }
}
```

### Route Integration

```php
<?php
// public/index.php
use Slim\Factory\AppFactory;
use App\Middleware\TuskLangMiddleware;

require __DIR__ . '/../vendor/autoload.php';

$container = require __DIR__ . '/../config/container.php';
$app = AppFactory::createFromContainer($container);

// Add TuskLang middleware
$app->add(TuskLangMiddleware::class);

// Routes
$app->get('/config', function (Request $request, Response $response) {
    $parser = $this->get(TuskLang::class);
    $config = $parser->parseFile('config/app.tsk');
    
    $response->getBody()->write(json_encode($config));
    return $response->withHeader('Content-Type', 'application/json');
});

$app->get('/stats', function (Request $request, Response $response) {
    $parser = $this->get(TuskLang::class);
    $stats = $parser->parseFile('config/stats.tsk');
    
    $response->getBody()->write(json_encode($stats));
    return $response->withHeader('Content-Type', 'application/json');
});

$app->run();
```

## 🌙 Lumen Integration

### Service Provider

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
            $adapter = new PostgreSQLAdapter([
                'host' => env('DB_HOST', 'localhost'),
                'port' => env('DB_PORT', 5432),
                'database' => env('DB_DATABASE', 'myapp'),
                'user' => env('DB_USERNAME', 'postgres'),
                'password' => env('DB_PASSWORD', 'secret'),
            ]);
            
            $parser->setDatabaseAdapter($adapter);
            
            return $parser;
        });
        
        // Register facade
        $this->app->alias(TuskLang::class, 'tusklang');
    }
}
```

### Bootstrap Configuration

```php
<?php
// bootstrap/app.php
use App\Providers\TuskLangServiceProvider;

$app = new Laravel\Lumen\Application(
    dirname(__DIR__)
);

// Register TuskLang service provider
$app->register(TuskLangServiceProvider::class);

// Load TuskLang configuration
$parser = app(TuskLang::class);
$config = $parser->parseFile('config/app.tsk');

// Merge with Lumen's config
foreach ($config as $key => $value) {
    config([$key => $value]);
}

return $app;
```

## 🔧 Custom Framework Integration

### Generic Integration Pattern

```php
<?php
// Generic TuskLang integration for any PHP framework
class TuskLangIntegration
{
    private TuskLang $parser;
    private array $config = [];
    
    public function __construct(array $options = [])
    {
        $this->parser = new TuskLang();
        
        // Configure database adapter
        if (isset($options['database'])) {
            $adapter = $this->createDatabaseAdapter($options['database']);
            $this->parser->setDatabaseAdapter($adapter);
        }
        
        // Configure cache
        if (isset($options['cache'])) {
            $cacheBackend = $this->createCacheBackend($options['cache']);
            $this->parser->setCacheBackend($cacheBackend);
        }
        
        // Load configuration
        $this->loadConfiguration($options['config_files'] ?? []);
    }
    
    private function createDatabaseAdapter(array $config)
    {
        switch ($config['driver']) {
            case 'postgres':
                return new PostgreSQLAdapter($config);
            case 'mysql':
                return new MySQLAdapter($config);
            case 'sqlite':
                return new SQLiteAdapter($config['database']);
            default:
                throw new \InvalidArgumentException("Unsupported database driver: {$config['driver']}");
        }
    }
    
    private function createCacheBackend(array $config)
    {
        switch ($config['driver']) {
            case 'redis':
                return new RedisCache($config);
            case 'file':
                return new FileCache($config['path']);
            default:
                throw new \InvalidArgumentException("Unsupported cache driver: {$config['driver']}");
        }
    }
    
    private function loadConfiguration(array $files)
    {
        foreach ($files as $file) {
            if (file_exists($file)) {
                $config = $this->parser->parseFile($file);
                $this->config = array_merge($this->config, $config);
            }
        }
    }
    
    public function get(string $key, $default = null)
    {
        $keys = explode('.', $key);
        $value = $this->config;
        
        foreach ($keys as $k) {
            if (!isset($value[$k])) {
                return $default;
            }
            $value = $value[$k];
        }
        
        return $value;
    }
    
    public function parse(string $content, array $context = []): array
    {
        return $this->parser->parseWithContext($content, $context);
    }
    
    public function parseFile(string $file, array $context = []): array
    {
        return $this->parser->parseFile($file, $context);
    }
}
```

## 📚 Best Practices

### Framework-Specific Considerations

```php
<?php
// config/framework-best-practices.tsk
[laravel]
# Use Laravel's environment system
environment: @env("APP_ENV", "local")
debug: @env("APP_DEBUG", "false")

# Use Laravel's database configuration
database_host: @env("DB_HOST", "127.0.0.1")
database_port: @env("DB_PORT", "3306")

[symfony]
# Use Symfony's environment system
environment: @env("APP_ENV", "dev")
debug: @env("APP_DEBUG", "1")

# Use Symfony's database configuration
database_host: @env("DATABASE_HOST", "127.0.0.1")
database_port: @env("DATABASE_PORT", "3306")

[codeigniter]
# Use CodeIgniter's environment system
environment: @env("CI_ENVIRONMENT", "development")
debug: @env("CI_DEBUG", "1")

# Use CodeIgniter's database configuration
database_host: @env("database.default.hostname", "localhost")
database_port: @env("database.default.port", "3306")
```

### Performance Optimization

```php
<?php
// config/performance-optimization.tsk
[caching]
# Framework-specific cache configuration
laravel_cache: @if(@env("CACHE_DRIVER") == "redis", true, false)
symfony_cache: @if(@env("CACHE_ADAPTER") == "redis", true, false)
codeigniter_cache: @if(@env("cache.handler") == "redis", true, false)

[optimization]
# Framework-specific optimizations
laravel_optimize: @if(@env("APP_ENV") == "production", true, false)
symfony_optimize: @if(@env("APP_ENV") == "prod", true, false)
codeigniter_optimize: @if(@env("CI_ENVIRONMENT") == "production", true, false)
```

## 📚 Next Steps

Now that you've mastered TuskLang integration with PHP frameworks, explore:

1. **Advanced Framework Features** - Deep integration with framework-specific features
2. **Custom Extensions** - Build framework-specific extensions
3. **Performance Tuning** - Optimize for framework-specific patterns
4. **Security Integration** - Leverage framework security features
5. **Testing Strategies** - Framework-specific testing approaches

## 🆘 Need Help?

- **Documentation**: [https://docs.tusklang.org/php/frameworks](https://docs.tusklang.org/php/frameworks)
- **Examples**: [https://github.com/cyber-boost/php-examples](https://github.com/cyber-boost/php-examples)
- **Community**: [https://community.tusklang.org](https://community.tusklang.org)

---

**Ready to revolutionize your PHP framework configuration? You're now a TuskLang framework integration master! 🚀** 