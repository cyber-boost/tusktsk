# 🚀 TuskLang PHP Advanced Features Guide

**"We don't bow to any king" - PHP Edition**

Master TuskLang's most powerful features in PHP! This guide covers advanced caching, validation, security, performance optimization, and cutting-edge patterns that will revolutionize your configuration management.

## ⚡ Advanced Caching

### Intelligent Cache Management

```php
<?php
// config/caching.tsk
[analytics]
# Cache with intelligent invalidation
total_users: @cache("5m", @query("SELECT COUNT(*) FROM users"))
active_users: @cache("5m", @query("SELECT COUNT(*) FROM users WHERE active = 1"))

# Cache with dependencies
user_stats: @cache.depends("users", "5m", """
    SELECT 
        COUNT(*) as total,
        COUNT(CASE WHEN active = 1 THEN 1 END) as active,
        AVG(age) as avg_age
    FROM users
""")

# Cache with custom key
monthly_revenue: @cache.key("revenue:monthly", "1h", """
    SELECT SUM(amount) FROM orders 
    WHERE created_at >= ? AND status = 'completed'
""", @date.start_of_month())
```

```php
<?php

use TuskLang\TuskLang;
use TuskLang\Cache\RedisCache;
use TuskLang\Cache\FileCache;

$parser = new TuskLang();

// Configure multiple cache backends
$redisCache = new RedisCache([
    'host' => 'localhost',
    'port' => 6379,
    'db' => 0
]);

$fileCache = new FileCache('/tmp/tusklang-cache');

$parser->setCacheBackend($redisCache);
$parser->setFallbackCache($fileCache);

// Enable intelligent caching
$parser->enableIntelligentCaching(true);
$parser->setCacheTTL(300); // 5 minutes default
```

### Cache Invalidation Strategies

```php
<?php
// config/cache-invalidation.tsk
[user_data]
# Cache with automatic invalidation on user updates
user_profile: @cache.invalidate("user:{$user_id}", "10m", 
    @query("SELECT * FROM users WHERE id = ?", $user_id))

# Cache with manual invalidation
order_summary: @cache.manual("orders:summary", "30m", """
    SELECT 
        COUNT(*) as total_orders,
        SUM(amount) as total_revenue
    FROM orders
    WHERE created_at >= ?
""", @date.subtract("7d"))
```

```php
<?php

use TuskLang\TuskLang;

$parser = new TuskLang();

// Manual cache invalidation
$parser->invalidateCache('user:123');
$parser->invalidateCachePattern('user:*');
$parser->clearAllCache();

// Cache statistics
$stats = $parser->getCacheStats();
echo "Hit rate: {$stats['hit_rate']}%\n";
echo "Total hits: {$stats['hits']}\n";
echo "Total misses: {$stats['misses']}\n";
```

## 🔒 Advanced Validation

### Custom Validators

```php
<?php

use TuskLang\TuskLang;
use TuskLang\Validators\Validator;

$parser = new TuskLang();

// Complex validators
$parser->addValidator('strong_password', function($password) {
    if (strlen($password) < 8) return false;
    if (!preg_match('/[A-Z]/', $password)) return false;
    if (!preg_match('/[a-z]/', $password)) return false;
    if (!preg_match('/[0-9]/', $password)) return false;
    if (!preg_match('/[^A-Za-z0-9]/', $password)) return false;
    return true;
});

$parser->addValidator('valid_email', function($email) {
    return filter_var($email, FILTER_VALIDATE_EMAIL) !== false;
});

$parser->addValidator('ip_range', function($ip, $range) {
    return in_array($ip, $range);
});

$parser->addValidator('file_exists', function($path) {
    return file_exists($path) && is_readable($path);
});

$parser->addValidator('json_schema', function($data, $schema) {
    // Validate against JSON schema
    $validator = new JsonSchema\Validator();
    $validator->validate($data, $schema);
    return $validator->isValid();
});
```

### Validation in Configuration

```php
<?php
// config/validation.tsk
[user_input]
email: @validate.valid_email(@request.email)
password: @validate.strong_password(@request.password)
age: @validate.range(@request.age, 13, 120)
ip_address: @validate.ip_range(@request.ip, ["127.0.0.1", "192.168.1.0/24"])

[file_validation]
config_file: @validate.file_exists(@request.config_path)
log_file: @validate.writable(@request.log_path)
ssl_cert: @validate.file_exists(@request.ssl_cert)

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

### Validation Error Handling

```php
<?php

use TuskLang\TuskLang;
use TuskLang\Exceptions\ValidationException;

$parser = new TuskLang();

try {
    $config = $parser->parseFile('config/validation.tsk');
} catch (ValidationException $e) {
    echo "Validation error: " . $e->getMessage() . "\n";
    echo "Field: " . $e->getField() . "\n";
    echo "Value: " . $e->getValue() . "\n";
    echo "Rule: " . $e->getRule() . "\n";
    
    // Get all validation errors
    $errors = $e->getErrors();
    foreach ($errors as $error) {
        echo "Error: {$error['field']} - {$error['message']}\n";
    }
}
```

## 🔐 Advanced Security

### Encryption and Hashing

```php
<?php
// config/security.tsk
[secrets]
# Encrypt sensitive data
api_key: @encrypt(@env("API_KEY"), "AES-256-GCM")
database_password: @encrypt(@env("DB_PASSWORD"), "AES-256-GCM")
session_secret: @encrypt(@env("SESSION_SECRET"), "AES-256-GCM")

# Hash sensitive data
password_hash: @hash.bcrypt(@request.password, 12)
token_hash: @hash.sha256(@request.token)

# Secure environment variables
secure_api_key: @env.secure("API_KEY")
secure_db_password: @env.secure("DB_PASSWORD")
```

```php
<?php

use TuskLang\TuskLang;
use TuskLang\Security\Encryption;
use TuskLang\Security\Hashing;

$parser = new TuskLang();

// Configure encryption
$encryption = new Encryption([
    'algorithm' => 'AES-256-GCM',
    'key' => getenv('ENCRYPTION_KEY'),
    'iv_length' => 16
]);

$parser->setEncryption($encryption);

// Configure hashing
$hashing = new Hashing([
    'bcrypt_cost' => 12,
    'salt_length' => 32
]);

$parser->setHashing($hashing);
```

### Access Control

```php
<?php
// config/access-control.tsk
[permissions]
# Role-based access control
user_permissions: @rbac.get_permissions(@request.user_role)
can_edit: @rbac.can(@request.user_role, "edit", @request.resource)
can_delete: @rbac.can(@request.user_role, "delete", @request.resource)

# IP-based access control
allowed_ips: ["127.0.0.1", "192.168.1.0/24", "10.0.0.0/8"]
is_allowed_ip: @security.ip_allowed(@request.ip, $allowed_ips)

# Time-based access control
business_hours: @time.between("09:00", "17:00")
is_business_hours: @time.is_business_hours()
```

### CSRF Protection

```php
<?php
// config/csrf.tsk
[security]
csrf_token: @csrf.generate()
csrf_valid: @csrf.validate(@request.csrf_token)
csrf_field: @csrf.field()

[forms]
login_form: @form.csrf_protected({
    "action": "/login",
    "method": "POST",
    "fields": ["email", "password"]
})
```

## ⚡ Performance Optimization

### Query Optimization

```php
<?php
// config/optimized-queries.tsk
[analytics]
# Use indexed columns
user_count: @query.indexed("SELECT COUNT(*) FROM users WHERE active = 1", "idx_users_active")

# Batch queries
user_stats: @query.batch("""
    SELECT 
        COUNT(*) as total_users,
        COUNT(CASE WHEN active = 1 THEN 1 END) as active_users,
        AVG(age) as avg_age
    FROM users
""")

# Paginated queries
recent_users: @query.paginated("""
    SELECT id, name, email, created_at 
    FROM users 
    ORDER BY created_at DESC
""", @request.page, @request.limit)
```

### Memory Optimization

```php
<?php

use TuskLang\TuskLang;

$parser = new TuskLang();

// Memory optimization settings
$parser->setMemoryLimit('512M');
$parser->enableGarbageCollection(true);
$parser->setGarbageCollectionThreshold(1000);

// Lazy loading
$parser->enableLazyLoading(true);
$parser->setLazyLoadingDepth(3);

// Stream processing for large files
$parser->enableStreamProcessing(true);
$parser->setStreamBufferSize(8192);
```

### Connection Pooling

```php
<?php

use TuskLang\Adapters\PostgreSQLAdapter;

$postgres = new PostgreSQLAdapter([
    'host' => 'localhost',
    'database' => 'myapp',
    'user' => 'postgres',
    'password' => 'secret'
], [
    'max_connections' => 50,
    'min_connections' => 5,
    'idle_timeout' => 30000,
    'connection_timeout' => 2000,
    'max_lifetime' => 3600000,
    'connection_retry_attempts' => 3,
    'connection_retry_delay' => 1000
]);
```

## 🔄 Advanced Patterns

### Dependency Injection

```php
<?php
// config/di.tsk
[services]
database: @service.resolve("DatabaseService")
cache: @service.resolve("CacheService")
logger: @service.resolve("LoggerService")

[configuration]
db_config: @service.config("DatabaseService")
cache_config: @service.config("CacheService")
log_config: @service.config("LoggerService")
```

```php
<?php

use TuskLang\TuskLang;
use TuskLang\DI\Container;

$container = new Container();

// Register services
$container->register('DatabaseService', function() {
    return new DatabaseService([
        'host' => 'localhost',
        'database' => 'myapp'
    ]);
});

$container->register('CacheService', function() {
    return new CacheService([
        'driver' => 'redis',
        'host' => 'localhost'
    ]);
});

$parser = new TuskLang();
$parser->setContainer($container);
```

### Event-Driven Configuration

```php
<?php
// config/events.tsk
[event_handlers]
user_created: @event.handle("user.created", {
    "action": "send_welcome_email",
    "data": @request.user_data
})

order_completed: @event.handle("order.completed", {
    "action": "update_inventory",
    "data": @request.order_data
})

system_error: @event.handle("system.error", {
    "action": "send_alert",
    "data": @request.error_data
})
```

```php
<?php

use TuskLang\TuskLang;
use TuskLang\Events\EventDispatcher;

$dispatcher = new EventDispatcher();

// Register event listeners
$dispatcher->listen('user.created', function($data) {
    // Send welcome email
    MailService::sendWelcomeEmail($data['user']);
});

$dispatcher->listen('order.completed', function($data) {
    // Update inventory
    InventoryService::updateStock($data['order']);
});

$parser = new TuskLang();
$parser->setEventDispatcher($dispatcher);
```

### Template Engine Integration

```php
<?php
// config/templates.tsk
[email_templates]
welcome_email: @template.render("emails/welcome.twig", {
    "user": @request.user_data,
    "app_name": $app_name,
    "login_url": @url.generate("login")
})

order_confirmation: @template.render("emails/order.twig", {
    "order": @request.order_data,
    "user": @request.user_data
})
```

```php
<?php

use TuskLang\TuskLang;
use TuskLang\Templates\TwigTemplateEngine;

$twig = new TwigTemplateEngine([
    'cache' => '/tmp/twig-cache',
    'debug' => true
]);

$parser = new TuskLang();
$parser->setTemplateEngine($twig);
```

## 🧪 Testing and Debugging

### Unit Testing

```php
<?php

use TuskLang\TuskLang;
use PHPUnit\Framework\TestCase;

class TuskLangTest extends TestCase
{
    private TuskLang $parser;
    
    protected function setUp(): void
    {
        $this->parser = new TuskLang();
    }
    
    public function testBasicParsing()
    {
        $config = $this->parser->parse('app_name: "TestApp"');
        $this->assertEquals('TestApp', $config['app_name']);
    }
    
    public function testDatabaseQuery()
    {
        $db = new SQLiteAdapter(':memory:');
        $this->parser->setDatabaseAdapter($db);
        
        $config = $this->parser->parse('count: @query("SELECT COUNT(*) FROM sqlite_master")');
        $this->assertIsInt($config['count']);
    }
    
    public function testValidation()
    {
        $this->parser->addValidator('test', function($value) {
            return $value === 'valid';
        });
        
        $config = $this->parser->parse('value: @validate.test("valid")');
        $this->assertEquals('valid', $config['value']);
    }
}
```

### Debugging Tools

```php
<?php

use TuskLang\TuskLang;

$parser = new TuskLang();

// Enable debugging
$parser->enableDebug(true);
$parser->setDebugLevel('verbose');

// Parse with debugging
$config = $parser->parseFile('config/app.tsk');

// Get debug information
$debug = $parser->getDebugInfo();
echo "Parse time: {$debug['parse_time']}ms\n";
echo "Memory usage: {$debug['memory_usage']} bytes\n";
echo "Cache hits: {$debug['cache_hits']}\n";
echo "Cache misses: {$debug['cache_misses']}\n";

// Get query log
$queries = $parser->getQueryLog();
foreach ($queries as $query) {
    echo "Query: {$query['sql']}\n";
    echo "Time: {$query['time']}ms\n";
    echo "Parameters: " . json_encode($query['params']) . "\n";
}
```

## 📊 Monitoring and Metrics

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

```php
<?php

use TuskLang\TuskLang;
use TuskLang\Metrics\PrometheusMetrics;

$metrics = new PrometheusMetrics([
    'namespace' => 'tusklang',
    'subsystem' => 'php'
]);

$parser = new TuskLang();
$parser->setMetrics($metrics);

// Parse configuration
$config = $parser->parseFile('config/metrics.tsk');

// Export metrics
$metricsData = $metrics->export();
echo $metricsData;
```

### Health Checks

```php
<?php
// config/health.tsk
[health]
database: @health.check("database", {
    "query": "SELECT 1",
    "timeout": 5000
})

cache: @health.check("cache", {
    "operation": "ping",
    "timeout": 1000
})

external_api: @health.check("http", {
    "url": "https://api.example.com/health",
    "timeout": 5000
})
```

```php
<?php

use TuskLang\TuskLang;
use TuskLang\Health\HealthChecker;

$healthChecker = new HealthChecker();

$parser = new TuskLang();
$parser->setHealthChecker($healthChecker);

$health = $parser->parseFile('config/health.tsk');

foreach ($health['health'] as $service => $status) {
    echo "{$service}: " . ($status ? 'Healthy' : 'Unhealthy') . "\n";
}
```

## 🚀 Production Deployment

### Environment-Specific Configuration

```php
<?php
// config/environments/production.tsk
$environment: "production"
$debug: false

[app]
debug: false
log_level: "error"
cache_driver: "redis"
session_driver: "redis"

[database]
host: @env("DB_HOST")
port: @env("DB_PORT")
database: @env("DB_NAME")
user: @env("DB_USER")
password: @env("DB_PASSWORD")
ssl_mode: "require"

[security]
https_only: true
cors_origin: ["https://myapp.com"]
session_secure: true
```

### Docker Integration

```dockerfile
FROM php:8.4-fpm

# Install system dependencies
RUN apt-get update && apt-get install -y \
    libpq-dev \
    libsqlite3-dev \
    redis-server \
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

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD php /var/www/html/health-check.php
```

### CI/CD Integration

```yaml
# .github/workflows/tusklang.yml
name: TuskLang PHP Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    
    services:
      postgres:
        image: postgres:15
        env:
          POSTGRES_PASSWORD: postgres
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5
      
      redis:
        image: redis:7
        options: >-
          --health-cmd "redis-cli ping"
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup PHP
      uses: shivammathur/setup-php@v2
      with:
        php-version: '8.4'
        extensions: pdo, pdo_pgsql, pdo_sqlite, redis
    
    - name: Install dependencies
      run: composer install --prefer-dist --no-progress
    
    - name: Run tests
      run: vendor/bin/phpunit
    
    - name: Validate TSK files
      run: php tusk.php validate config/*.tsk
    
    - name: Test configuration parsing
      run: php tusk.php test config/*.tsk
```

## 📚 Next Steps

Now that you've mastered TuskLang's advanced features in PHP, explore:

1. **Custom Extensions** - Build your own TuskLang extensions
2. **Plugin System** - Create reusable plugins for common patterns
3. **Performance Tuning** - Optimize for high-traffic applications
4. **Security Hardening** - Implement enterprise-grade security
5. **Monitoring and Alerting** - Set up comprehensive observability

## 🆘 Need Help?

- **Documentation**: [https://docs.tusklang.org/php/advanced](https://docs.tusklang.org/php/advanced)
- **Examples**: [https://github.com/cyber-boost/php-examples](https://github.com/cyber-boost/php-examples)
- **Community**: [https://community.tusklang.org](https://community.tusklang.org)

---

**Ready to revolutionize your PHP configuration? You're now a TuskLang advanced features master! 🚀** 