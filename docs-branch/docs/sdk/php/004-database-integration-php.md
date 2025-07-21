# 🗄️ TuskLang PHP Database Integration Guide

**"We don't bow to any king" - PHP Edition**

Revolutionize your database configuration with TuskLang! This guide shows you how to embed live database queries directly in your configuration files, making your PHP applications more dynamic and powerful than ever before.

## 🚀 Quick Start

### Basic Database Query

```php
<?php
// config/stats.tsk
[analytics]
total_users: @query("SELECT COUNT(*) FROM users")
active_users: @query("SELECT COUNT(*) FROM users WHERE active = 1")
revenue_today: @query("SELECT SUM(amount) FROM orders WHERE created_at >= ?", @date.today())
```

```php
<?php

require_once 'vendor/autoload.php';

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
```

## 🗃️ Database Adapters

### PostgreSQL Adapter

```php
<?php

use TuskLang\Adapters\PostgreSQLAdapter;

// Basic connection
$postgres = new PostgreSQLAdapter([
    'host' => 'localhost',
    'port' => 5432,
    'database' => 'myapp',
    'user' => 'postgres',
    'password' => 'secret'
]);

// With SSL/TLS
$postgres = new PostgreSQLAdapter([
    'host' => 'localhost',
    'port' => 5432,
    'database' => 'myapp',
    'user' => 'postgres',
    'password' => 'secret',
    'sslmode' => 'require',
    'sslcert' => '/path/to/client-cert.pem',
    'sslkey' => '/path/to/client-key.pem',
    'sslrootcert' => '/path/to/ca-cert.pem'
]);

// With connection pooling
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

### MySQL Adapter

```php
<?php

use TuskLang\Adapters\MySQLAdapter;

// Basic connection
$mysql = new MySQLAdapter([
    'host' => 'localhost',
    'port' => 3306,
    'database' => 'myapp',
    'user' => 'root',
    'password' => 'secret'
]);

// With connection pooling
$mysql = new MySQLAdapter([
    'host' => 'localhost',
    'database' => 'myapp',
    'user' => 'root',
    'password' => 'secret'
], [
    'connection_limit' => 10,
    'acquire_timeout' => 60000
]);

// With SSL
$mysql = new MySQLAdapter([
    'host' => 'localhost',
    'database' => 'myapp',
    'user' => 'root',
    'password' => 'secret',
    'ssl_ca' => '/path/to/ca-cert.pem',
    'ssl_cert' => '/path/to/client-cert.pem',
    'ssl_key' => '/path/to/client-key.pem'
]);
```

### SQLite Adapter

```php
<?php

use TuskLang\Adapters\SQLiteAdapter;

// File-based database
$sqlite = new SQLiteAdapter('app.db');

// In-memory database
$sqlite = new SQLiteAdapter(':memory:');

// With options
$sqlite = new SQLiteAdapter('app.db', [
    'timeout' => 30000,
    'verbose' => true,
    'foreign_keys' => true
]);
```

### MongoDB Adapter

```php
<?php

use TuskLang\Adapters\MongoDBAdapter;

// Basic connection
$mongo = new MongoDBAdapter([
    'uri' => 'mongodb://localhost:27017/',
    'database' => 'myapp'
]);

// With authentication
$mongo = new MongoDBAdapter([
    'uri' => 'mongodb://user:pass@localhost:27017/',
    'database' => 'myapp',
    'auth_source' => 'admin'
]);

// With SSL/TLS
$mongo = new MongoDBAdapter([
    'uri' => 'mongodb://localhost:27017/',
    'database' => 'myapp',
    'ssl' => true,
    'ssl_cert' => '/path/to/cert.pem',
    'ssl_key' => '/path/to/key.pem'
]);
```

### Redis Adapter

```php
<?php

use TuskLang\Adapters\RedisAdapter;

// Basic connection
$redis = new RedisAdapter([
    'host' => 'localhost',
    'port' => 6379,
    'db' => 0
]);

// With authentication
$redis = new RedisAdapter([
    'host' => 'localhost',
    'port' => 6379,
    'password' => 'secret',
    'db' => 0
]);

// With SSL
$redis = new RedisAdapter([
    'host' => 'localhost',
    'port' => 6380,
    'ssl' => true,
    'ssl_cert' => '/path/to/cert.pem',
    'ssl_key' => '/path/to/key.pem'
]);
```

## 🔍 Query Building

### Basic Queries

```php
<?php
// config/queries.tsk
[users]
total_count: @query("SELECT COUNT(*) FROM users")
active_count: @query("SELECT COUNT(*) FROM users WHERE active = 1")
recent_users: @query("SELECT COUNT(*) FROM users WHERE created_at >= ?", @date.subtract("7d"))

[orders]
total_orders: @query("SELECT COUNT(*) FROM orders")
pending_orders: @query("SELECT COUNT(*) FROM orders WHERE status = 'pending'")
total_revenue: @query("SELECT SUM(amount) FROM orders WHERE status = 'completed'")
```

### Parameterized Queries

```php
<?php
// config/parameterized.tsk
[user_stats]
user_by_id: @query("SELECT * FROM users WHERE id = ?", @request.user_id)
orders_by_user: @query("SELECT * FROM orders WHERE user_id = ?", @request.user_id)
recent_activity: @query("SELECT * FROM activity WHERE user_id = ? AND created_at >= ?", 
    @request.user_id, @date.subtract("30d"))
```

```php
<?php

use TuskLang\TuskLang;

$parser = new TuskLang();

// Set request context
$context = [
    'request' => [
        'user_id' => 123
    ]
];

$config = $parser->parseWithContext(file_get_contents('config/parameterized.tsk'), $context);
```

### Complex Queries

```php
<?php
// config/complex.tsk
[analytics]
user_activity: @query("""
    SELECT 
        u.id,
        u.name,
        COUNT(o.id) as order_count,
        SUM(o.amount) as total_spent,
        MAX(o.created_at) as last_order
    FROM users u
    LEFT JOIN orders o ON u.id = o.user_id
    WHERE u.active = 1
    GROUP BY u.id, u.name
    ORDER BY total_spent DESC
    LIMIT 10
""")

revenue_by_month: @query("""
    SELECT 
        DATE_TRUNC('month', created_at) as month,
        COUNT(*) as order_count,
        SUM(amount) as revenue
    FROM orders
    WHERE status = 'completed'
    GROUP BY DATE_TRUNC('month', created_at)
    ORDER BY month DESC
""")
```

### Aggregation Queries

```php
<?php
// config/aggregations.tsk
[stats]
user_summary: @query("""
    SELECT 
        COUNT(*) as total_users,
        COUNT(CASE WHEN active = 1 THEN 1 END) as active_users,
        COUNT(CASE WHEN created_at >= ? THEN 1 END) as new_users,
        AVG(EXTRACT(YEAR FROM AGE(birth_date))) as avg_age
    FROM users
""", @date.subtract("30d"))

order_summary: @query("""
    SELECT 
        COUNT(*) as total_orders,
        COUNT(CASE WHEN status = 'pending' THEN 1 END) as pending_orders,
        COUNT(CASE WHEN status = 'completed' THEN 1 END) as completed_orders,
        SUM(CASE WHEN status = 'completed' THEN amount ELSE 0 END) as total_revenue,
        AVG(amount) as avg_order_value
    FROM orders
    WHERE created_at >= ?
""", @date.subtract("7d"))
```

## 🔄 Real-Time Data Integration

### Live Dashboard Configuration

```php
<?php
// config/dashboard.tsk
[dashboard]
last_updated: @date.now()

[metrics]
total_users: @query("SELECT COUNT(*) FROM users")
active_users: @query("SELECT COUNT(*) FROM users WHERE last_login >= ?", @date.subtract("24h"))
total_orders: @query("SELECT COUNT(*) FROM orders")
pending_orders: @query("SELECT COUNT(*) FROM orders WHERE status = 'pending'")
total_revenue: @query("SELECT SUM(amount) FROM orders WHERE status = 'completed'")

[system]
memory_usage: @php("memory_get_usage(true)")
disk_usage: @php("disk_free_space('/')")
load_average: @php("sys_getloadavg()[0]")
```

### Dynamic Configuration Based on Data

```php
<?php
// config/dynamic.tsk
$user_count: @query("SELECT COUNT(*) FROM users")
$is_busy: @if($user_count > 1000, true, false)

[server]
workers: @if($is_busy, 8, 4)
timeout: @if($is_busy, 30, 60)
cache_ttl: @if($is_busy, 300, 600)

[features]
rate_limiting: @if($is_busy, true, false)
compression: @if($is_busy, true, false)
debug_mode: @if($is_busy, false, true)
```

## 🏗️ Framework Integration

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
            
            // Use Laravel's database configuration
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
// app/Http/Controllers/DashboardController.php
namespace App\Http\Controllers;

use Illuminate\Http\Request;
use TuskLang\TuskLang;

class DashboardController extends Controller
{
    public function index(TuskLang $parser)
    {
        $dashboard = $parser->parseFile('config/dashboard.tsk');
        return view('dashboard', compact('dashboard'));
    }
    
    public function stats(TuskLang $parser)
    {
        $stats = $parser->parseFile('config/stats.tsk');
        return response()->json($stats);
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
        class: TuskLang\Adapters\PostgreSQLAdapter
        arguments:
            $config:
                host: '%env(DB_HOST)%'
                port: '%env(DB_PORT)%'
                database: '%env(DB_NAME)%'
                user: '%env(DB_USER)%'
                password: '%env(DB_PASSWORD)%'
```

```php
<?php
// src/Controller/DashboardController.php
namespace App\Controller;

use Symfony\Bundle\FrameworkBundle\Controller\AbstractController;
use Symfony\Component\HttpFoundation\JsonResponse;
use Symfony\Component\Routing\Annotation\Route;
use TuskLang\TuskLang;

class DashboardController extends AbstractController
{
    #[Route('/dashboard', name: 'app_dashboard')]
    public function index(TuskLang $parser): JsonResponse
    {
        $dashboard = $parser->parseFile('config/dashboard.tsk');
        return $this->json($dashboard);
    }
}
```

## 🔐 Security Features

### SQL Injection Prevention

```php
<?php
// config/secure.tsk
[user_data]
user_profile: @query("SELECT * FROM users WHERE id = ?", @validate.integer(@request.user_id))
user_orders: @query("SELECT * FROM orders WHERE user_id = ? AND status = ?", 
    @validate.integer(@request.user_id), @validate.enum(@request.status, ["pending", "completed"]))
```

### Input Validation

```php
<?php

use TuskLang\TuskLang;

$parser = new TuskLang();

// Add custom validators
$parser->addValidator('integer', function($value) {
    return is_numeric($value) && floor($value) == $value;
});

$parser->addValidator('enum', function($value, $allowed) {
    return in_array($value, $allowed);
});

$parser->addValidator('email', function($value) {
    return filter_var($value, FILTER_VALIDATE_EMAIL) !== false;
});
```

### Encrypted Queries

```php
<?php
// config/encrypted.tsk
[secrets]
api_key: @query.encrypted("SELECT api_key FROM secrets WHERE user_id = ?", @request.user_id)
password_hash: @query.encrypted("SELECT password_hash FROM users WHERE id = ?", @request.user_id)
```

## 📊 Performance Optimization

### Query Caching

```php
<?php
// config/cached.tsk
[analytics]
# Cache for 5 minutes
total_users: @query.cache("5m", "SELECT COUNT(*) FROM users")
active_users: @query.cache("5m", "SELECT COUNT(*) FROM users WHERE active = 1")

# Cache for 1 hour
monthly_revenue: @query.cache("1h", """
    SELECT SUM(amount) FROM orders 
    WHERE created_at >= ? AND status = 'completed'
""", @date.start_of_month())
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
    'max_connections' => 20,
    'idle_timeout' => 30000,
    'connection_timeout' => 2000,
    'min_connections' => 5
]);
```

### Query Optimization

```php
<?php
// config/optimized.tsk
[analytics]
# Use indexed columns
user_count: @query("SELECT COUNT(*) FROM users WHERE active = 1")

# Avoid SELECT *
user_summary: @query("SELECT id, name, email, created_at FROM users WHERE active = 1")

# Use LIMIT for large datasets
recent_orders: @query("SELECT id, amount, status FROM orders ORDER BY created_at DESC LIMIT 100")
```

## 🔧 Error Handling

### Database Connection Errors

```php
<?php

use TuskLang\TuskLang;
use TuskLang\Adapters\PostgreSQLAdapter;
use TuskLang\Exceptions\DatabaseException;

try {
    $db = new PostgreSQLAdapter([
        'host' => 'localhost',
        'port' => 5432,
        'database' => 'myapp',
        'user' => 'postgres',
        'password' => 'secret'
    ]);
    
    $parser = new TuskLang();
    $parser->setDatabaseAdapter($db);
    
    $config = $parser->parseFile('config/stats.tsk');
    echo "Configuration loaded successfully\n";
} catch (DatabaseException $e) {
    echo "Database error: " . $e->getMessage() . "\n";
    echo "Code: " . $e->getCode() . "\n";
} catch (Exception $e) {
    echo "Unexpected error: " . $e->getMessage() . "\n";
}
```

### Query Error Handling

```php
<?php
// config/error-handling.tsk
[analytics]
# Use fallback values for failed queries
total_users: @query.fallback("SELECT COUNT(*) FROM users", 0)
active_users: @query.fallback("SELECT COUNT(*) FROM users WHERE active = 1", 0)
revenue: @query.fallback("SELECT SUM(amount) FROM orders WHERE status = 'completed'", 0.0)
```

## 📈 Monitoring and Logging

### Query Performance Monitoring

```php
<?php
// config/monitoring.tsk
[performance]
slow_queries: @query("""
    SELECT query, mean_time, calls 
    FROM pg_stat_statements 
    WHERE mean_time > 1000 
    ORDER BY mean_time DESC 
    LIMIT 10
""")

connection_stats: @query("""
    SELECT 
        state,
        COUNT(*) as count
    FROM pg_stat_activity 
    GROUP BY state
""")
```

### Query Logging

```php
<?php

use TuskLang\TuskLang;

$parser = new TuskLang();

// Enable query logging
$parser->enableQueryLogging(true);
$parser->setQueryLogFile('/var/log/tusklang-queries.log');

$config = $parser->parseFile('config/stats.tsk');

// Get query log
$queries = $parser->getQueryLog();
foreach ($queries as $query) {
    echo "Query: {$query['sql']}\n";
    echo "Time: {$query['time']}ms\n";
    echo "Parameters: " . json_encode($query['params']) . "\n";
}
```

## 🚀 Advanced Features

### Transaction Support

```php
<?php
// config/transaction.tsk
[user_creation]
create_user: @query.transaction("""
    INSERT INTO users (name, email, created_at) VALUES (?, ?, ?);
    INSERT INTO user_profiles (user_id, bio) VALUES (LAST_INSERT_ID(), ?);
""", @request.name, @request.email, @date.now(), @request.bio)
```

### Batch Operations

```php
<?php
// config/batch.tsk
[bulk_operations]
update_status: @query.batch("""
    UPDATE orders 
    SET status = ?, updated_at = ? 
    WHERE id IN (?, ?, ?)
""", "completed", @date.now(), @request.order_ids)
```

## 📚 Next Steps

Now that you've mastered TuskLang's database integration in PHP, explore:

1. **Advanced @ Operators** - Master the full operator system
2. **Caching Strategies** - Optimize performance with intelligent caching
3. **Security Best Practices** - Implement secure database operations
4. **Performance Tuning** - Optimize queries and connections
5. **Monitoring and Alerting** - Set up comprehensive monitoring

## 🆘 Need Help?

- **Documentation**: [https://docs.tusklang.org/php/database](https://docs.tusklang.org/php/database)
- **Examples**: [https://github.com/cyber-boost/php-examples](https://github.com/cyber-boost/php-examples)
- **Community**: [https://community.tusklang.org](https://community.tusklang.org)

---

**Ready to revolutionize your database configuration? You're now a TuskLang database master! 🚀** 