# ⚡ TuskLang PHP @ Operators Guide

**"We don't bow to any king" - PHP Edition**

Master TuskLang's revolutionary @ operator system in PHP! This guide covers every @ operator with practical examples, showing how to transform your configuration from static files into dynamic, intelligent systems.

## 🎯 @ Operator Overview

The @ operator system is the heart of TuskLang's power - it transforms static configuration into dynamic, intelligent systems that adapt to your environment, data, and requirements.

```php
<?php
// config/operators-overview.tsk
[basic_examples]
# Environment variables
api_key: @env("API_KEY", "default-key")
debug_mode: @env("DEBUG", "false")

# Database queries
user_count: @query("SELECT COUNT(*) FROM users")
active_users: @query("SELECT COUNT(*) FROM users WHERE active = 1")

# Date and time
current_time: @date.now()
formatted_date: @date("Y-m-d H:i:s")
yesterday: @date.subtract("1d")

# PHP functions
memory_usage: @php("memory_get_usage(true)")
php_version: @php("PHP_VERSION")
```

## 🔧 Environment Variables (@env)

### Basic Environment Access

```php
<?php
// config/environment.tsk
[app]
name: @env("APP_NAME", "MyApp")
version: @env("APP_VERSION", "1.0.0")
environment: @env("APP_ENV", "development")
debug: @env("DEBUG", "false")

[database]
host: @env("DB_HOST", "localhost")
port: @env("DB_PORT", "5432")
name: @env("DB_NAME", "myapp")
user: @env("DB_USER", "postgres")
password: @env("DB_PASSWORD", "secret")
```

```php
<?php

use TuskLang\TuskLang;

// Set environment variables
putenv('APP_NAME=MyAwesomeApp');
putenv('APP_ENV=production');
putenv('DB_HOST=prod-db.example.com');
putenv('DB_PASSWORD=super-secret');

$parser = new TuskLang();
$config = $parser->parseFile('config/environment.tsk');

echo "App: {$config['app']['name']} ({$config['app']['environment']})\n";
echo "Database: {$config['database']['host']}:{$config['database']['port']}\n";
```

### Secure Environment Variables

```php
<?php
// config/secure-env.tsk
[secrets]
# Secure environment variables with encryption
api_key: @env.secure("API_KEY")
database_password: @env.secure("DB_PASSWORD")
session_secret: @env.secure("SESSION_SECRET")
encryption_key: @env.secure("ENCRYPTION_KEY")

# Environment-specific secrets
production_api_key: @env.secure("PROD_API_KEY")
staging_api_key: @env.secure("STAGING_API_KEY")
```

```php
<?php

use TuskLang\TuskLang;
use TuskLang\Security\Encryption;

$parser = new TuskLang();

// Configure encryption for secure environment variables
$encryption = new Encryption([
    'algorithm' => 'AES-256-GCM',
    'key' => getenv('MASTER_ENCRYPTION_KEY')
]);

$parser->setEncryption($encryption);

$config = $parser->parseFile('config/secure-env.tsk');
```

### Environment Validation

```php
<?php
// config/env-validation.tsk
[validation]
# Validate required environment variables
required_vars: @env.required(["API_KEY", "DB_PASSWORD", "SESSION_SECRET"])

# Validate environment variable types
port_number: @env.validate("DB_PORT", "integer", 5432)
debug_flag: @env.validate("DEBUG", "boolean", false)
api_url: @env.validate("API_URL", "url", "https://api.example.com")
```

## 🗄️ Database Queries (@query)

### Basic Queries

```php
<?php
// config/basic-queries.tsk
[analytics]
total_users: @query("SELECT COUNT(*) FROM users")
active_users: @query("SELECT COUNT(*) FROM users WHERE active = 1")
total_orders: @query("SELECT COUNT(*) FROM orders")
revenue: @query("SELECT SUM(amount) FROM orders WHERE status = 'completed'")

[system]
database_size: @query("SELECT pg_database_size(current_database())")
table_count: @query("SELECT COUNT(*) FROM information_schema.tables")
```

### Parameterized Queries

```php
<?php
// config/parameterized-queries.tsk
[user_data]
user_by_id: @query("SELECT * FROM users WHERE id = ?", @request.user_id)
user_orders: @query("SELECT * FROM orders WHERE user_id = ?", @request.user_id)
recent_activity: @query("""
    SELECT * FROM activity 
    WHERE user_id = ? AND created_at >= ?
""", @request.user_id, @date.subtract("7d"))

[analytics]
revenue_period: @query("""
    SELECT SUM(amount) FROM orders 
    WHERE created_at >= ? AND created_at <= ?
""", @date.subtract("30d"), @date.now())
```

### Complex Queries

```php
<?php
// config/complex-queries.tsk
[user_analytics]
user_summary: @query("""
    SELECT 
        u.id,
        u.name,
        u.email,
        COUNT(o.id) as order_count,
        SUM(o.amount) as total_spent,
        MAX(o.created_at) as last_order,
        AVG(o.amount) as avg_order_value
    FROM users u
    LEFT JOIN orders o ON u.id = o.user_id
    WHERE u.active = 1
    GROUP BY u.id, u.name, u.email
    ORDER BY total_spent DESC
    LIMIT 10
""")

[revenue_analytics]
monthly_revenue: @query("""
    SELECT 
        DATE_TRUNC('month', created_at) as month,
        COUNT(*) as order_count,
        SUM(amount) as revenue,
        AVG(amount) as avg_order_value
    FROM orders
    WHERE status = 'completed'
    GROUP BY DATE_TRUNC('month', created_at)
    ORDER BY month DESC
    LIMIT 12
""")
```

### Query Caching

```php
<?php
// config/cached-queries.tsk
[analytics]
# Cache for 5 minutes
total_users: @query.cache("5m", "SELECT COUNT(*) FROM users")
active_users: @query.cache("5m", "SELECT COUNT(*) FROM users WHERE active = 1")

# Cache for 1 hour with parameters
monthly_revenue: @query.cache("1h", """
    SELECT SUM(amount) FROM orders 
    WHERE created_at >= ? AND status = 'completed'
""", @date.start_of_month())

# Cache with custom key
user_stats: @query.cache.key("user:stats:{$user_id}", "10m", """
    SELECT * FROM users WHERE id = ?
""", $user_id)
```

## 📅 Date and Time (@date)

### Current Date/Time

```php
<?php
// config/date-time.tsk
[current]
now: @date.now()
formatted: @date("Y-m-d H:i:s")
timestamp: @date("U")
iso8601: @date("c")
rfc2822: @date("r")

[formats]
date_only: @date("Y-m-d")
time_only: @date("H:i:s")
month_year: @date("F Y")
day_name: @date("l")
```

### Date Calculations

```php
<?php
// config/date-calculations.tsk
[relative_dates]
yesterday: @date.subtract("1d")
tomorrow: @date.add("1d")
last_week: @date.subtract("7d")
next_month: @date.add("1m")
last_year: @date.subtract("1y")

[periods]
start_of_day: @date.start_of_day()
end_of_day: @date.end_of_day()
start_of_week: @date.start_of_week()
end_of_week: @date.end_of_week()
start_of_month: @date.start_of_month()
end_of_month: @date.end_of_month()
```

### Date Comparisons

```php
<?php
// config/date-comparisons.tsk
[comparisons]
is_today: @date.is_today(@request.date)
is_this_week: @date.is_this_week(@request.date)
is_this_month: @date.is_this_month(@request.date)
is_weekend: @date.is_weekend(@date.now())
is_business_day: @date.is_business_day(@date.now())

[time_periods]
business_hours: @date.between("09:00", "17:00")
lunch_break: @date.between("12:00", "13:00")
night_shift: @date.between("22:00", "06:00")
```

## 🐘 PHP Functions (@php)

### System Information

```php
<?php
// config/php-system.tsk
[system]
php_version: @php("PHP_VERSION")
memory_limit: @php("ini_get('memory_limit')")
max_execution_time: @php("ini_get('max_execution_time')")
upload_max_filesize: @php("ini_get('upload_max_filesize')")
post_max_size: @php("ini_get('post_max_size')")

[extensions]
pdo_loaded: @php("extension_loaded('pdo')")
pgsql_loaded: @php("extension_loaded('pdo_pgsql')")
redis_loaded: @php("extension_loaded('redis')")
curl_loaded: @php("extension_loaded('curl')")
```

### Memory and Performance

```php
<?php
// config/php-performance.tsk
[memory]
current_usage: @php("memory_get_usage(true)")
peak_usage: @php("memory_get_peak_usage(true)")
usage_formatted: @php("number_format(memory_get_usage(true))")
peak_formatted: @php("number_format(memory_get_peak_usage(true))")

[performance]
load_average: @php("sys_getloadavg()[0]")
disk_free: @php("disk_free_space('/')")
disk_total: @php("disk_total_space('/')")
disk_usage_percent: @php("round((1 - disk_free_space('/') / disk_total_space('/')) * 100, 2)")
```

### File System Operations

```php
<?php
// config/php-filesystem.tsk
[filesystem]
file_exists: @php("file_exists('/etc/passwd')")
is_readable: @php("is_readable('/etc/passwd')")
is_writable: @php("is_writable('/tmp')")
file_size: @php("filesize('/etc/passwd')")
file_modified: @php("filemtime('/etc/passwd')")

[paths]
current_dir: @php("getcwd()")
temp_dir: @php("sys_get_temp_dir()")
home_dir: @php("getenv('HOME')")
```

### Custom PHP Functions

```php
<?php
// config/custom-php.tsk
[custom]
user_agent: @php("$_SERVER['HTTP_USER_AGENT'] ?? 'Unknown'")
client_ip: @php("$_SERVER['REMOTE_ADDR'] ?? '127.0.0.1'")
request_method: @php("$_SERVER['REQUEST_METHOD'] ?? 'GET'")
server_name: @php("$_SERVER['SERVER_NAME'] ?? 'localhost'")

[calculated]
random_number: @php("rand(1, 100)")
uuid: @php("uniqid()")
hash: @php("md5(uniqid())")
```

## 📁 File Operations (@file)

### File Reading

```php
<?php
// config/file-operations.tsk
[file_reading]
config_json: @file.read("config.json")
log_file: @file.read("/var/log/app.log")
version_file: @file.read("version.txt")
ssl_cert: @file.read("/etc/ssl/certs/cert.pem")

[file_info]
config_size: @file.size("config.json")
config_modified: @file.modified("config.json")
config_exists: @file.exists("config.json")
```

### File Writing

```php
<?php
// config/file-writing.tsk
[file_writing]
write_log: @file.write("/var/log/app.log", "Application started at " . @date.now())
write_config: @file.write("generated-config.json", @json.encode($config))
write_backup: @file.write("backup-" . @date("Y-m-d-H-i-s") . ".json", $data)
```

### File Validation

```php
<?php
// config/file-validation.tsk
[validation]
config_readable: @file.readable("config.json")
log_writable: @file.writable("/var/log/app.log")
ssl_cert_valid: @file.valid_ssl_cert("/etc/ssl/certs/cert.pem")
json_valid: @file.valid_json("config.json")
```

## 🔄 Conditional Logic (@if)

### Basic Conditionals

```php
<?php
// config/conditionals.tsk
$environment: @env("APP_ENV", "development")
$debug: @env("DEBUG", "false")

[app]
debug_mode: @if($environment != "production", true, false)
log_level: @if($environment == "production", "error", "debug")
cache_driver: @if($environment == "production", "redis", "file")

[database]
host: @if($environment == "production", "prod-db.example.com", "localhost")
ssl_mode: @if($environment == "production", "require", "disable")
connection_pool: @if($environment == "production", 20, 5)
```

### Complex Conditionals

```php
<?php
// config/complex-conditionals.tsk
$environment: @env("APP_ENV", "development")
$user_count: @query("SELECT COUNT(*) FROM users")
$is_busy: @if($user_count > 1000, true, false)

[server]
workers: @if($environment == "production", 
    @if($is_busy, 8, 4), 
    2)
timeout: @if($environment == "production", 
    @if($is_busy, 30, 60), 
    120)

[features]
rate_limiting: @if($environment == "production" && $is_busy, true, false)
compression: @if($environment == "production", true, false)
debug_mode: @if($environment != "production" && $user_count < 100, true, false)
```

### Nested Conditionals

```php
<?php
// config/nested-conditionals.tsk
$environment: @env("APP_ENV", "development")
$region: @env("AWS_REGION", "us-east-1")
$instance_type: @env("INSTANCE_TYPE", "t3.micro")

[infrastructure]
database_host: @if($environment == "production",
    @if($region == "us-east-1", "prod-db-us-east-1.example.com",
        @if($region == "eu-west-1", "prod-db-eu-west-1.example.com",
            "prod-db-default.example.com")),
    "localhost")

instance_size: @if($environment == "production",
    @if($instance_type == "t3.micro", "t3.small",
        @if($instance_type == "t3.small", "t3.medium", "t3.large")),
    "t3.micro")
```

## 🔐 Security Operators

### Encryption (@encrypt)

```php
<?php
// config/encryption.tsk
[secrets]
api_key: @encrypt(@env("API_KEY"), "AES-256-GCM")
database_password: @encrypt(@env("DB_PASSWORD"), "AES-256-GCM")
session_secret: @encrypt(@env("SESSION_SECRET"), "AES-256-GCM")

[sensitive_data]
user_token: @encrypt(@request.token, "AES-256-GCM")
payment_info: @encrypt(@request.payment_data, "AES-256-GCM")
```

### Hashing (@hash)

```php
<?php
// config/hashing.tsk
[password_hashes]
user_password: @hash.bcrypt(@request.password, 12)
admin_password: @hash.bcrypt(@request.password, 14)

[token_hashes]
csrf_token: @hash.sha256(@request.csrf_token)
api_token: @hash.sha256(@request.api_token)
session_token: @hash.sha256(@request.session_token)
```

### Validation (@validate)

```php
<?php
// config/validation.tsk
[input_validation]
email: @validate.email(@request.email)
password: @validate.strong_password(@request.password)
age: @validate.range(@request.age, 13, 120)
url: @validate.url(@request.website)
ip_address: @validate.ip(@request.ip)

[data_validation]
user_data: @validate.json_schema(@request.user_data, {
    "type": "object",
    "properties": {
        "name": {"type": "string", "minLength": 1},
        "email": {"type": "string", "format": "email"},
        "age": {"type": "integer", "minimum": 13}
    },
    "required": ["name", "email"]
})
```

## 📊 Metrics and Monitoring (@metrics)

### Performance Metrics

```php
<?php
// config/metrics.tsk
[performance]
parse_time: @metrics.timer("tusklang.parse_time")
memory_usage: @metrics.gauge("tusklang.memory_usage", @php("memory_get_usage(true)"))
cache_hit_rate: @metrics.gauge("tusklang.cache_hit_rate", @cache.hit_rate())

[application]
request_count: @metrics.counter("app.requests")
error_count: @metrics.counter("app.errors")
response_time: @metrics.histogram("app.response_time", @request.response_time)
```

### Business Metrics

```php
<?php
// config/business-metrics.tsk
[analytics]
user_registrations: @metrics.counter("business.user_registrations")
orders_placed: @metrics.counter("business.orders_placed")
revenue_generated: @metrics.counter("business.revenue", @request.order_amount)
conversion_rate: @metrics.gauge("business.conversion_rate", @calculate.conversion_rate())
```

## 🧠 Machine Learning (@learn, @optimize)

### Learning Operators

```php
<?php
// config/machine-learning.tsk
[learning]
optimal_cache_ttl: @learn("cache_ttl", "5m", {
    "factors": ["user_count", "request_rate", "memory_usage"],
    "algorithm": "regression"
})

optimal_workers: @learn("worker_count", 4, {
    "factors": ["cpu_usage", "memory_usage", "request_queue"],
    "algorithm": "classification"
})

[optimization]
cache_strategy: @optimize("cache_strategy", "lru", {
    "metrics": ["hit_rate", "memory_usage", "response_time"],
    "constraints": ["memory_limit", "response_time_limit"]
})
```

## 🔄 Custom Operators

### Creating Custom Operators

```php
<?php

use TuskLang\TuskLang;

$parser = new TuskLang();

// Custom operator for business logic
$parser->addOperator('business_hours', function($time) {
    $hour = (int) date('H', strtotime($time));
    return $hour >= 9 && $hour <= 17;
});

// Custom operator for geographic data
$parser->addOperator('timezone_offset', function($timezone) {
    $dateTime = new DateTime('now', new DateTimeZone($timezone));
    return $dateTime->format('P');
});

// Custom operator for external API calls
$parser->addOperator('weather', function($city) {
    $apiKey = getenv('WEATHER_API_KEY');
    $url = "https://api.weatherapi.com/v1/current.json?key={$apiKey}&q={$city}";
    $response = file_get_contents($url);
    $data = json_decode($response, true);
    return $data['current']['temp_c'] ?? null;
});
```

### Using Custom Operators

```php
<?php
// config/custom-operators.tsk
[business_logic]
is_business_hours: @business_hours(@date.now())
timezone_offset: @timezone_offset("America/New_York")
current_weather: @weather("New York")

[conditional_logic]
show_business_hours_message: @if(@business_hours(@date.now()), 
    "We're open!", 
    "We're closed. Please visit during business hours.")
```

## 🚀 Advanced Patterns

### Operator Chaining

```php
<?php
// config/operator-chaining.tsk
[chained_operations]
user_count_formatted: @php("number_format(" . @query("SELECT COUNT(*) FROM users") . ")")
memory_usage_mb: @php("round(" . @php("memory_get_usage(true)") . " / 1024 / 1024, 2)")
cache_key: @php("md5(" . @env("APP_NAME") . "_" . @date("Y-m-d") . ")")
```

### Conditional Operator Execution

```php
<?php
// config/conditional-execution.tsk
[conditional_queries]
user_data: @if(@env("ENABLE_USER_QUERIES", "true") == "true",
    @query("SELECT * FROM users WHERE id = ?", @request.user_id),
    null)

expensive_operation: @if(@env("ENABLE_EXPENSIVE_OPERATIONS", "false") == "true",
    @query.cache("1h", "SELECT * FROM large_table"),
    [])
```

## 🔧 Error Handling

### Operator Error Handling

```php
<?php

use TuskLang\TuskLang;
use TuskLang\Exceptions\OperatorException;

$parser = new TuskLang();

try {
    $config = $parser->parseFile('config/operators.tsk');
} catch (OperatorException $e) {
    echo "Operator error: " . $e->getMessage() . "\n";
    echo "Operator: " . $e->getOperator() . "\n";
    echo "Parameters: " . json_encode($e->getParameters()) . "\n";
} catch (Exception $e) {
    echo "Unexpected error: " . $e->getMessage() . "\n";
}
```

### Fallback Values

```php
<?php
// config/fallbacks.tsk
[fallback_examples]
user_count: @query.fallback("SELECT COUNT(*) FROM users", 0)
api_response: @http.fallback("GET", "https://api.example.com/status", {"status": "unknown"})
file_content: @file.read.fallback("config.json", "{}")
```

## 📚 Best Practices

### Performance Optimization

```php
<?php
// config/performance-best-practices.tsk
[optimized_usage]
# Cache expensive operations
user_count: @query.cache("5m", "SELECT COUNT(*) FROM users")

# Use indexed queries
active_users: @query.indexed("SELECT COUNT(*) FROM users WHERE active = 1", "idx_users_active")

# Batch related queries
user_stats: @query.batch("""
    SELECT 
        COUNT(*) as total,
        COUNT(CASE WHEN active = 1 THEN 1 END) as active
    FROM users
""")
```

### Security Best Practices

```php
<?php
// config/security-best-practices.tsk
[secure_usage]
# Always validate input
user_id: @validate.integer(@request.user_id)
email: @validate.email(@request.email)

# Use parameterized queries
user_data: @query("SELECT * FROM users WHERE id = ?", @validate.integer(@request.user_id))

# Encrypt sensitive data
api_key: @encrypt(@env("API_KEY"), "AES-256-GCM")
```

## 📚 Next Steps

Now that you've mastered TuskLang's @ operator system in PHP, explore:

1. **Advanced Database Integration** - Complex queries and optimization
2. **Custom Extensions** - Build your own operators
3. **Performance Tuning** - Optimize operator usage
4. **Security Hardening** - Secure operator implementations
5. **Monitoring and Alerting** - Track operator performance

## 🆘 Need Help?

- **Documentation**: [https://docs.tusklang.org/php/operators](https://docs.tusklang.org/php/operators)
- **Examples**: [https://github.com/cyber-boost/php-examples](https://github.com/cyber-boost/php-examples)
- **Community**: [https://community.tusklang.org](https://community.tusklang.org)

---

**Ready to revolutionize your PHP configuration with @ operators? You're now a TuskLang operator master! 🚀** 