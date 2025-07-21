# 🐘 TuskLang PHP Installation Guide

**"We don't bow to any king" - PHP Edition**

Welcome to the revolutionary world of TuskLang for PHP! This guide will get you up and running with TuskLang's powerful configuration capabilities in your PHP environment.

## 🚀 Quick Installation

### Method 1: Composer Installation (Recommended)

```bash
# Install via Composer
composer require cyber-boost/tusktsk

# Or add to your composer.json
{
    "require": {
        "cyber-boost/tusktsk": "^2.0"
    }
}
```

### Method 2: One-Line Install

```bash
# Direct install script
curl -sSL https://php.tusklang.org | php

# Or with wget
wget -qO- https://php.tusklang.org | php
```

### Method 3: Manual Installation

```bash
# Clone the repository
git clone https://github.com/cyber-boost/php.git
cd php

# Install dependencies
composer install

# Add to your PHP include path
export PHP_INCLUDE_PATH="/path/to/tusklang/php/src:$PHP_INCLUDE_PATH"
```

## 🔧 System Requirements

### PHP Version
- **Minimum**: PHP 8.1+
- **Recommended**: PHP 8.4+
- **Extensions**: JSON, PDO, SQLite3 (for database features)

### Framework Compatibility
- ✅ Laravel 10+
- ✅ Symfony 6+
- ✅ CodeIgniter 4+
- ✅ Slim Framework 4+
- ✅ Lumen 10+
- ✅ Custom PHP applications

### Database Support
- ✅ SQLite 3.x
- ✅ PostgreSQL 12+
- ✅ MySQL 8.0+
- ✅ MariaDB 10.5+
- ✅ MongoDB 4.4+
- ✅ Redis 6.0+

## 📦 Installation Verification

### Basic Verification

```php
<?php

// Test basic installation
require_once 'vendor/autoload.php';

use TuskLang\TuskLang;

try {
    $parser = new TuskLang();
    echo "✅ TuskLang PHP SDK installed successfully!\n";
    echo "Version: " . TuskLang::VERSION . "\n";
} catch (Exception $e) {
    echo "❌ Installation failed: " . $e->getMessage() . "\n";
}
```

### Advanced Verification

```php
<?php

require_once 'vendor/autoload.php';

use TuskLang\TuskLang;
use TuskLang\Adapters\SQLiteAdapter;

// Test parser functionality
$parser = new TuskLang();

$testConfig = '
app_name: "TestApp"
version: "1.0.0"
debug: true

[database]
host: "localhost"
port: 5432
';

try {
    $data = $parser->parse($testConfig);
    echo "✅ Parser working correctly\n";
    echo "App: {$data['app_name']} v{$data['version']}\n";
} catch (Exception $e) {
    echo "❌ Parser test failed: " . $e->getMessage() . "\n";
}

// Test database adapter
try {
    $db = new SQLiteAdapter(':memory:');
    $result = $db->query("SELECT 1 as test");
    echo "✅ Database adapter working\n";
} catch (Exception $e) {
    echo "❌ Database test failed: " . $e->getMessage() . "\n";
}
```

## 🏗️ Framework Integration

### Laravel Integration

```bash
# Install via Composer
composer require cyber-boost/tusktsk

# Publish configuration
php artisan vendor:publish --provider="TuskLang\Laravel\TuskLangServiceProvider"
```

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
    ],
];
```

```php
<?php
// In your Laravel service provider
use TuskLang\TuskLang;

class AppServiceProvider extends ServiceProvider
{
    public function register()
    {
        $this->app->singleton(TuskLang::class, function ($app) {
            $parser = new TuskLang();
            
            // Configure database adapter
            $adapter = config('tusklang.default_adapter');
            $config = config("tusklang.adapters.{$adapter}");
            
            if ($adapter === 'sqlite') {
                $parser->setDatabaseAdapter(new SQLiteAdapter($config['database']));
            } elseif ($adapter === 'postgres') {
                $parser->setDatabaseAdapter(new PostgreSQLAdapter($config));
            }
            
            return $parser;
        });
    }
}
```

### Symfony Integration

```yaml
# config/services.yaml
services:
    TuskLang\TuskLang:
        arguments:
            $databaseAdapter: '@tusklang.database_adapter'
    
    tusklang.database_adapter:
        class: TuskLang\Adapters\SQLiteAdapter
        arguments:
            $database: '%kernel.project_dir%/var/tusklang.db'
```

```php
<?php
// In your Symfony controller
use TuskLang\TuskLang;

class ConfigController extends AbstractController
{
    public function index(TuskLang $parser): Response
    {
        $config = $parser->parseFile('config/app.tsk');
        return $this->json($config);
    }
}
```

### CodeIgniter Integration

```php
<?php
// app/Config/TuskLang.php
namespace Config;

use CodeIgniter\Config\BaseConfig;

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
    ];
}
```

## 🔐 Security Configuration

### Environment Variables

```bash
# .env file
TUSKLANG_DB_ADAPTER=postgres
TUSKLANG_DB_HOST=localhost
TUSKLANG_DB_PORT=5432
TUSKLANG_DB_NAME=myapp
TUSKLANG_DB_USER=postgres
TUSKLANG_DB_PASSWORD=secret
TUSKLANG_ENCRYPTION_KEY=your-secret-key-here
```

### SSL/TLS Configuration

```php
<?php

use TuskLang\Adapters\PostgreSQLAdapter;

$postgres = new PostgreSQLAdapter([
    'host' => 'localhost',
    'port' => 5432,
    'database' => 'myapp',
    'user' => 'postgres',
    'password' => 'secret',
    'sslmode' => 'require',
    'sslcert' => '/path/to/client-cert.pem',
    'sslkey' => '/path/to/client-key.pem',
    'sslrootcert' => '/path/to/ca-cert.pem',
]);
```

## 🐳 Docker Installation

### Dockerfile Example

```dockerfile
FROM php:8.4-fpm

# Install system dependencies
RUN apt-get update && apt-get install -y \
    libpq-dev \
    libsqlite3-dev \
    && docker-php-ext-install pdo pdo_pgsql pdo_sqlite

# Install Composer
COPY --from=composer:latest /usr/bin/composer /usr/bin/composer

# Install TuskLang
RUN composer require cyber-boost/tusktsk

# Copy application files
COPY . /var/www/html
WORKDIR /var/www/html

# Set permissions
RUN chown -R www-data:www-data /var/www/html
```

### Docker Compose Example

```yaml
version: '3.8'

services:
  app:
    build: .
    ports:
      - "8000:8000"
    environment:
      - TUSKLANG_DB_ADAPTER=postgres
      - TUSKLANG_DB_HOST=postgres
      - TUSKLANG_DB_PORT=5432
      - TUSKLANG_DB_NAME=myapp
      - TUSKLANG_DB_USER=postgres
      - TUSKLANG_DB_PASSWORD=secret
    depends_on:
      - postgres
    volumes:
      - ./config:/var/www/html/config

  postgres:
    image: postgres:15
    environment:
      POSTGRES_DB: myapp
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: secret
    volumes:
      - postgres_data:/var/lib/postgresql/data

volumes:
  postgres_data:
```

## 🔧 Troubleshooting

### Common Issues

#### 1. Composer Autoload Issues
```bash
# Regenerate autoloader
composer dump-autoload

# Clear Composer cache
composer clear-cache
```

#### 2. Database Connection Issues
```php
<?php

// Test database connection
try {
    $db = new SQLiteAdapter(':memory:');
    echo "✅ SQLite connection successful\n";
} catch (Exception $e) {
    echo "❌ SQLite connection failed: " . $e->getMessage() . "\n";
}

try {
    $db = new PostgreSQLAdapter([
        'host' => 'localhost',
        'port' => 5432,
        'database' => 'test',
        'user' => 'postgres',
        'password' => 'secret'
    ]);
    echo "✅ PostgreSQL connection successful\n";
} catch (Exception $e) {
    echo "❌ PostgreSQL connection failed: " . $e->getMessage() . "\n";
}
```

#### 3. Permission Issues
```bash
# Fix file permissions
chmod -R 755 /path/to/tusklang
chown -R www-data:www-data /path/to/tusklang

# Fix database file permissions
chmod 664 /path/to/tusklang.db
chown www-data:www-data /path/to/tusklang.db
```

#### 4. PHP Extension Issues
```bash
# Install required extensions
sudo apt-get install php8.4-pdo php8.4-pdo-pgsql php8.4-sqlite3

# Or for macOS
brew install php@8.4
brew install postgresql
```

### Performance Optimization

```php
<?php

use TuskLang\TuskLang;

// Enable caching
$parser = new TuskLang();
$parser->enableCache(true);
$parser->setCacheDirectory('/tmp/tusklang-cache');

// Connection pooling for databases
$postgres = new PostgreSQLAdapter([
    'host' => 'localhost',
    'database' => 'myapp',
    'user' => 'postgres',
    'password' => 'secret'
], [
    'max_connections' => 20,
    'idle_timeout' => 30000,
    'connection_timeout' => 2000
]);
```

## 📚 Next Steps

Now that you have TuskLang installed, you can:

1. **Read the Quick Start Guide** - Learn basic syntax and concepts
2. **Explore Basic Syntax** - Master TuskLang's flexible syntax styles
3. **Integrate with Your Framework** - Set up TuskLang in your existing PHP application
4. **Learn Database Integration** - Use database queries in your configuration files
5. **Master @ Operators** - Leverage TuskLang's powerful operator system

## 🆘 Getting Help

- **Documentation**: [https://docs.tusklang.org/php](https://docs.tusklang.org/php)
- **GitHub Issues**: [https://github.com/cyber-boost/php/issues](https://github.com/cyber-boost/php/issues)
- **Community**: [https://community.tusklang.org](https://community.tusklang.org)
- **Discord**: [https://discord.gg/tusklang](https://discord.gg/tusklang)

---

**Ready to revolutionize your PHP configuration? Let's Tusk! 🚀** 