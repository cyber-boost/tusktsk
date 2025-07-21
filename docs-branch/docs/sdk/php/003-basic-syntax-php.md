# 📝 TuskLang PHP Basic Syntax Guide

**"We don't bow to any king" - PHP Edition**

Master TuskLang's revolutionary syntax flexibility in PHP! This guide covers all syntax styles, data types, and PHP-specific features that make TuskLang the most powerful configuration language ever created.

## 🎨 Syntax Flexibility

TuskLang supports **three distinct syntax styles** - choose the one that matches your team's preferences:

### Style 1: Traditional INI-Style (Square Brackets)

```php
<?php
// config/app.tsk
app_name: "MyApp"
version: "2.0.0"
debug: true

[database]
host: "localhost"
port: 5432
name: "myapp"
user: "postgres"
password: @env("DB_PASSWORD", "secret")

[server]
host: "0.0.0.0"
port: 8080
workers: 4

[features]
cache_enabled: true
rate_limiting: true
ssl_required: false
```

### Style 2: JSON-Like Objects (Curly Braces)

```php
<?php
// config/app.tsk
app_name: "MyApp"
version: "2.0.0"
debug: true

database {
    host: "localhost"
    port: 5432
    name: "myapp"
    user: "postgres"
    password: @env("DB_PASSWORD", "secret")
}

server {
    host: "0.0.0.0"
    port: 8080
    workers: 4
}

features {
    cache_enabled: true
    rate_limiting: true
    ssl_required: false
}
```

### Style 3: XML-Inspired (Angle Brackets)

```php
<?php
// config/app.tsk
app_name: "MyApp"
version: "2.0.0"
debug: true

database >
    host: "localhost"
    port: 5432
    name: "myapp"
    user: "postgres"
    password: @env("DB_PASSWORD", "secret")
<

server >
    host: "0.0.0.0"
    port: 8080
    workers: 4
<

features >
    cache_enabled: true
    rate_limiting: true
    ssl_required: false
<
```

## 📊 Data Types

### Strings

```php
<?php
// config/strings.tsk
simple_string: "Hello World"
quoted_string: 'Single quotes work too'
multiline_string: """
    This is a multiline
    string that spans
    multiple lines
"""

escaped_string: "Line 1\nLine 2\tTabbed"
unicode_string: "Hello 世界 🌍"
```

```php
<?php

use TuskLang\TuskLang;

$parser = new TuskLang();
$config = $parser->parseFile('config/strings.tsk');

echo $config['simple_string'] . "\n";        // Hello World
echo $config['quoted_string'] . "\n";        // Single quotes work too
echo $config['multiline_string'] . "\n";     // Multiline content
echo $config['escaped_string'] . "\n";       // With escape sequences
echo $config['unicode_string'] . "\n";       // Unicode support
```

### Numbers

```php
<?php
// config/numbers.tsk
integer_value: 42
float_value: 3.14159
negative_number: -17
scientific_notation: 1.23e-4
hex_number: 0xFF
binary_number: 0b1010
octal_number: 0o755
```

```php
<?php

use TuskLang\TuskLang;

$parser = new TuskLang();
$config = $parser->parseFile('config/numbers.tsk');

var_dump($config['integer_value']);      // int(42)
var_dump($config['float_value']);        // float(3.14159)
var_dump($config['negative_number']);    // int(-17)
var_dump($config['scientific_notation']); // float(0.000123)
var_dump($config['hex_number']);         // int(255)
var_dump($config['binary_number']);      // int(10)
var_dump($config['octal_number']);       // int(493)
```

### Booleans

```php
<?php
// config/booleans.tsk
true_value: true
false_value: false
explicit_true: TRUE
explicit_false: FALSE
```

```php
<?php

use TuskLang\TuskLang;

$parser = new TuskLang();
$config = $parser->parseFile('config/booleans.tsk');

var_dump($config['true_value']);    // bool(true)
var_dump($config['false_value']);   // bool(false)
var_dump($config['explicit_true']); // bool(true)
var_dump($config['explicit_false']); // bool(false)
```

### Null Values

```php
<?php
// config/null.tsk
null_value: null
explicit_null: NULL
empty_value: ""
```

```php
<?php

use TuskLang\TuskLang;

$parser = new TuskLang();
$config = $parser->parseFile('config/null.tsk');

var_dump($config['null_value']);    // NULL
var_dump($config['explicit_null']); // NULL
var_dump($config['empty_value']);   // string(0) ""
```

### Arrays

```php
<?php
// config/arrays.tsk
simple_array: ["apple", "banana", "cherry"]
mixed_array: [1, "two", 3.0, true, null]
nested_array: [
    ["a", "b", "c"],
    [1, 2, 3],
    [true, false]
]
associative_array: {
    "name": "John",
    "age": 30,
    "city": "New York"
}
mixed_associative: {
    "users": ["alice", "bob", "charlie"],
    "settings": {
        "debug": true,
        "port": 8080
    },
    "count": 42
}
```

```php
<?php

use TuskLang\TuskLang;

$parser = new TuskLang();
$config = $parser->parseFile('config/arrays.tsk');

// Access array elements
echo $config['simple_array'][0] . "\n";           // apple
echo $config['mixed_array'][1] . "\n";            // two
echo $config['nested_array'][0][1] . "\n";        // b
echo $config['associative_array']['name'] . "\n"; // John
echo $config['mixed_associative']['users'][0] . "\n"; // alice
```

## 🔗 Variable References and Interpolation

### Global Variables

```php
<?php
// config/variables.tsk
$app_name: "MyApp"
$version: "2.0.0"
$environment: @env("APP_ENV", "development")

[app]
name: $app_name
version: $version
env: $environment

[database]
host: "localhost"
port: 5432
name: "${app_name}_db"
user: "postgres"
password: @env("DB_PASSWORD", "secret")
```

```php
<?php

use TuskLang\TuskLang;

$parser = new TuskLang();
$config = $parser->parseFile('config/variables.tsk');

echo "App: {$config['app']['name']} v{$config['app']['version']}\n";
echo "Database: {$config['database']['name']}\n";
```

### Cross-File References

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

## 🔧 Comments and Documentation

### Single-Line Comments

```php
<?php
// config/comments.tsk
app_name: "MyApp"  # This is a single-line comment
version: "2.0.0"   // This also works
debug: true        # Boolean flag for debugging

[database]
host: "localhost"  # Database host
port: 5432         # Database port
name: "myapp"      # Database name
```

### Multi-Line Comments

```php
<?php
// config/multiline-comments.tsk
/*
 * This is a multi-line comment
 * It can span multiple lines
 * Useful for documentation
 */
app_name: "MyApp"
version: "2.0.0"

[database]
/*
 * Database configuration section
 * Contains all database-related settings
 */
host: "localhost"
port: 5432
name: "myapp"
```

### Section Documentation

```php
<?php
// config/documentation.tsk
# Application Configuration
# This file contains the main application settings
app_name: "MyApp"
version: "2.0.0"

# Database Configuration
# PostgreSQL database settings
[database]
host: "localhost"
port: 5432
name: "myapp"

# Server Configuration
# Web server settings
[server]
host: "0.0.0.0"
port: 8080
workers: 4
```

## 🎯 PHP-Specific Features

### Type Annotations

```php
<?php
// config/types.tsk
[string] app_name: "MyApp"
[int] version: 2
[float] pi: 3.14159
[bool] debug: true
[array] users: ["alice", "bob", "charlie"]
[object] settings: {
    "debug": true,
    "port": 8080
}
```

```php
<?php

use TuskLang\TuskLang;
use TuskLang\Config;

// Define typed configuration class
class AppConfig extends Config
{
    public string $app_name;
    public int $version;
    public float $pi;
    public bool $debug;
    public array $users;
    public object $settings;
}

$parser = new TuskLang();
$config = $parser->parseFile('config/types.tsk', AppConfig::class);

// Type-safe access
echo $config->app_name . "\n";    // string
echo $config->version . "\n";     // int
echo $config->pi . "\n";          // float
var_dump($config->debug);         // bool
var_dump($config->users);         // array
var_dump($config->settings);      // object
```

### PHP Function Integration

```php
<?php
// config/php-functions.tsk
[system]
memory_usage: @php("memory_get_usage(true)")
peak_memory: @php("memory_get_peak_usage(true)")
current_time: @php("date('Y-m-d H:i:s')")
php_version: @php("PHP_VERSION")
extension_loaded: @php("extension_loaded('pdo')")
file_exists: @php("file_exists('/etc/passwd')")
```

```php
<?php

use TuskLang\TuskLang;

$parser = new TuskLang();
$config = $parser->parseFile('config/php-functions.tsk');

echo "Memory usage: " . number_format($config['system']['memory_usage']) . " bytes\n";
echo "Current time: {$config['system']['current_time']}\n";
echo "PHP version: {$config['system']['php_version']}\n";
echo "PDO loaded: " . ($config['system']['extension_loaded'] ? 'Yes' : 'No') . "\n";
```

### Environment Variables

```php
<?php
// config/environment.tsk
[env]
app_env: @env("APP_ENV", "development")
debug: @env("DEBUG", "false")
database_url: @env("DATABASE_URL", "postgresql://localhost/myapp")
api_key: @env("API_KEY", "")
secret_key: @env("SECRET_KEY", "")
```

```php
<?php

use TuskLang\TuskLang;

// Set environment variables
putenv('APP_ENV=production');
putenv('DEBUG=true');
putenv('API_KEY=your-secret-key');

$parser = new TuskLang();
$config = $parser->parseFile('config/environment.tsk');

echo "Environment: {$config['env']['app_env']}\n";
echo "Debug: {$config['env']['debug']}\n";
echo "API Key: " . substr($config['env']['api_key'], 0, 8) . "...\n";
```

## 🔄 Conditional Logic

### Basic Conditionals

```php
<?php
// config/conditionals.tsk
$environment: @env("APP_ENV", "development")
$is_production: @if($environment == "production", true, false)

[app]
debug: @if($environment != "production", true, false)
log_level: @if($is_production, "error", "debug")
cache_driver: @if($is_production, "redis", "file")

[database]
host: @if($is_production, "prod-db.example.com", "localhost")
ssl_mode: @if($is_production, "require", "disable")
```

```php
<?php

use TuskLang\TuskLang;

// Test different environments
putenv('APP_ENV=production');
$parser = new TuskLang();
$config = $parser->parseFile('config/conditionals.tsk');

echo "Debug: " . ($config['app']['debug'] ? 'Enabled' : 'Disabled') . "\n";
echo "Log level: {$config['app']['log_level']}\n";
echo "Database host: {$config['database']['host']}\n";
```

### Complex Conditionals

```php
<?php
// config/complex-conditionals.tsk
$environment: @env("APP_ENV", "development")
$debug: @env("DEBUG", "false")

[logging]
level: @if($environment == "production", "error", 
    @if($environment == "staging", "warning", "debug"))
format: @if($environment == "production", "json", "text")
file: @if($environment == "production", "/var/log/app.log", "console")

[security]
https_only: @if($environment == "production", true, false)
cors_origin: @if($environment == "production", 
    ["https://myapp.com"], ["*"])
session_secure: @if($environment == "production", true, false)
```

## 📝 Best Practices

### 1. Consistent Naming

```php
<?php
// Good: Consistent naming convention
app_name: "MyApp"
database_host: "localhost"
api_key: @env("API_KEY")

// Avoid: Inconsistent naming
appName: "MyApp"
databaseHost: "localhost"
apiKey: @env("API_KEY")
```

### 2. Group Related Settings

```php
<?php
// Good: Logical grouping
[database]
host: "localhost"
port: 5432
name: "myapp"

[server]
host: "0.0.0.0"
port: 8080
workers: 4

// Avoid: Mixed settings
host: "localhost"
port: 5432
server_host: "0.0.0.0"
server_port: 8080
```

### 3. Use Environment Variables for Secrets

```php
<?php
// Good: Use environment variables for secrets
[database]
host: "localhost"
port: 5432
name: "myapp"
user: "postgres"
password: @env("DB_PASSWORD", "secret")

// Avoid: Hardcoded secrets
[database]
host: "localhost"
port: 5432
name: "myapp"
user: "postgres"
password: "my-secret-password"
```

### 4. Document Complex Configurations

```php
<?php
# Application Configuration
# This file contains all application settings
app_name: "MyApp"
version: "2.0.0"

# Database Configuration
# PostgreSQL database settings with connection pooling
[database]
host: "localhost"      # Database host
port: 5432             # Database port
name: "myapp"          # Database name
user: "postgres"       # Database user
password: @env("DB_PASSWORD", "secret")  # Database password
pool_size: 20          # Connection pool size
timeout: 30            # Connection timeout in seconds
```

## 🚀 Performance Tips

### 1. Use Caching

```php
<?php

use TuskLang\TuskLang;

$parser = new TuskLang();

// Enable caching for better performance
$parser->enableCache(true);
$parser->setCacheDirectory('/tmp/tusklang-cache');
$parser->setCacheTTL(300); // 5 minutes

// Parse with caching
$config = $parser->parseFile('config/app.tsk');
```

### 2. Minimize Database Queries

```php
<?php
// Good: Batch queries
[analytics]
user_stats: @query("""
    SELECT 
        COUNT(*) as total_users,
        COUNT(CASE WHEN active = 1 THEN 1 END) as active_users,
        SUM(revenue) as total_revenue
    FROM users
""")

// Avoid: Multiple separate queries
[analytics]
total_users: @query("SELECT COUNT(*) FROM users")
active_users: @query("SELECT COUNT(*) FROM users WHERE active = 1")
total_revenue: @query("SELECT SUM(revenue) FROM users")
```

## 🔧 Error Handling

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
    echo "Column: " . $e->getColumn() . "\n";
} catch (ValidationException $e) {
    echo "Validation error: " . $e->getMessage() . "\n";
    echo "Field: " . $e->getField() . "\n";
} catch (Exception $e) {
    echo "Unexpected error: " . $e->getMessage() . "\n";
}
```

## 📚 Next Steps

Now that you've mastered TuskLang's basic syntax in PHP, explore:

1. **@ Operators** - Master the powerful operator system
2. **Database Integration** - Use database queries in configuration
3. **Advanced Features** - Explore caching, validation, and security
4. **Framework Integration** - Integrate with Laravel, Symfony, and more
5. **Performance Optimization** - Optimize for production use

---

**Ready to revolutionize your PHP configuration syntax? You're now a TuskLang syntax master! 🚀** 