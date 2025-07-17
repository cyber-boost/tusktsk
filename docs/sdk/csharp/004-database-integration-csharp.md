# 🗄️ Database Integration - TuskLang for C# - "Configuration with a Heartbeat"

**Put your database to work in your configuration - Real-time queries, dynamic values, and live data integration!**

TuskLang revolutionizes configuration by allowing you to embed database queries directly in your configuration files. No more static values - your configuration adapts to your data in real-time.

## 🎯 Why Database Integration?

### The Traditional Problem
- **Static configuration** that doesn't reflect current data
- **Hard-coded values** that require redeployment to change
- **Manual synchronization** between database and configuration
- **No real-time updates** when data changes

### The TuskLang Solution
- **Dynamic configuration** that queries your database in real-time
- **Live data integration** - configuration adapts to current data
- **No redeployment needed** - changes in database automatically reflect in config
- **Complex queries** embedded directly in configuration files

## 🔌 Supported Databases

### 1. SQLite
- **Lightweight** - Perfect for embedded applications
- **Zero configuration** - File-based database
- **Cross-platform** - Works everywhere

### 2. PostgreSQL
- **Enterprise-grade** - Production-ready database
- **Advanced features** - JSON, arrays, complex queries
- **High performance** - Optimized for complex workloads

### 3. MySQL
- **Widely adopted** - Large ecosystem and community
- **Easy setup** - Simple installation and configuration
- **Good performance** - Optimized for web applications

### 4. MongoDB
- **Document-based** - Flexible schema design
- **Scalable** - Horizontal scaling capabilities
- **JSON-native** - Natural fit for modern applications

### 5. Redis
- **In-memory** - Ultra-fast key-value storage
- **Caching** - Perfect for performance optimization
- **Real-time** - Sub-millisecond response times

## 🚀 Basic Database Integration

### SQLite Setup

```csharp
using TuskLang;
using TuskLang.Adapters;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        // Create SQLite adapter
        var sqliteAdapter = new SQLiteAdapter("app.db");
        
        // Create parser with database adapter
        var parser = new TuskLang();
        parser.SetDatabaseAdapter(sqliteAdapter);
        
        // TSK file with database queries
        var tskContent = @"
[stats]
total_users: @query(""SELECT COUNT(*) FROM users"")
active_users: @query(""SELECT COUNT(*) FROM users WHERE active = 1"")
recent_users: @query(""SELECT COUNT(*) FROM users WHERE created_at > ?"", @date.subtract(""7d""))
";
        
        // Parse configuration with database queries
        var config = parser.Parse(tskContent);
        
        // Display dynamic stats
        Console.WriteLine($"Total Users: {config["stats"]["total_users"]}");
        Console.WriteLine($"Active Users: {config["stats"]["active_users"]}");
        Console.WriteLine($"Recent Users: {config["stats"]["recent_users"]}");
    }
}
```

### PostgreSQL Setup

```csharp
using TuskLang;
using TuskLang.Adapters;

class Program
{
    static void Main()
    {
        // Create PostgreSQL adapter
        var postgresAdapter = new PostgreSQLAdapter(new PostgreSQLConfig
        {
            Host = "localhost",
            Port = 5432,
            Database = "myapp",
            User = "postgres",
            Password = "secret",
            SslMode = "require"
        });
        
        // Create parser with database adapter
        var parser = new TuskLang();
        parser.SetDatabaseAdapter(postgresAdapter);
        
        // TSK file with PostgreSQL-specific queries
        var tskContent = @"
[analytics]
user_count: @query(""SELECT COUNT(*) FROM users"")
revenue_today: @query(""SELECT COALESCE(SUM(amount), 0) FROM orders WHERE DATE(created_at) = CURRENT_DATE"")
top_products: @query(""SELECT name, COUNT(*) as sales FROM order_items GROUP BY name ORDER BY sales DESC LIMIT 5"")
";
        
        var config = parser.Parse(tskContent);
        
        Console.WriteLine($"User Count: {config["analytics"]["user_count"]}");
        Console.WriteLine($"Today's Revenue: ${config["analytics"]["revenue_today"]}");
    }
}
```

## 📊 Advanced Query Patterns

### Parameterized Queries

```ini
# app.tsk - Parameterized queries
$user_id: @env("USER_ID", "1")
$days_back: @env("DAYS_BACK", "7")

[user_stats]
user_info: @query("SELECT name, email, created_at FROM users WHERE id = ?", $user_id)
recent_activity: @query("SELECT COUNT(*) FROM user_activity WHERE user_id = ? AND created_at > ?", $user_id, @date.subtract("${days_back}d"))
user_orders: @query("SELECT * FROM orders WHERE user_id = ? ORDER BY created_at DESC LIMIT 10", $user_id)
```

### C# Parameter Handling

```csharp
var parser = new TuskLang();
var sqliteAdapter = new SQLiteAdapter("app.db");
parser.SetDatabaseAdapter(sqliteAdapter);

// Set environment variables for parameters
Environment.SetEnvironmentVariable("USER_ID", "1");
Environment.SetEnvironmentVariable("DAYS_BACK", "30");

var config = parser.ParseFile("app.tsk");

// Access parameterized query results
var userInfo = config["user_stats"]["user_info"] as List<Dictionary<string, object>>;
var recentActivity = config["user_stats"]["recent_activity"];
var userOrders = config["user_stats"]["user_orders"] as List<Dictionary<string, object>>;

if (userInfo.Count > 0)
{
    var user = userInfo[0];
    Console.WriteLine($"User: {user["name"]} ({user["email"]})");
    Console.WriteLine($"Recent Activity: {recentActivity} actions");
    Console.WriteLine($"Orders: {userOrders.Count} total");
}
```

### Complex Queries with Joins

```ini
# app.tsk - Complex queries with joins
[analytics]
user_orders: @query("""
    SELECT 
        u.name as user_name,
        COUNT(o.id) as order_count,
        SUM(o.total) as total_spent
    FROM users u
    LEFT JOIN orders o ON u.id = o.user_id
    WHERE u.active = 1
    GROUP BY u.id, u.name
    ORDER BY total_spent DESC
    LIMIT 10
""")

product_performance: @query("""
    SELECT 
        p.name as product_name,
        COUNT(oi.id) as units_sold,
        SUM(oi.price * oi.quantity) as revenue
    FROM products p
    JOIN order_items oi ON p.id = oi.product_id
    JOIN orders o ON oi.order_id = o.id
    WHERE o.created_at >= ?
    GROUP BY p.id, p.name
    ORDER BY revenue DESC
""", @date.subtract("30d"))
```

### C# Complex Query Processing

```csharp
var parser = new TuskLang();
var postgresAdapter = new PostgreSQLAdapter(new PostgreSQLConfig
{
    Host = "localhost",
    Database = "myapp",
    User = "postgres",
    Password = "secret"
});
parser.SetDatabaseAdapter(postgresAdapter);

var config = parser.ParseFile("app.tsk");

// Process complex query results
var userOrders = config["analytics"]["user_orders"] as List<Dictionary<string, object>>;
var productPerformance = config["analytics"]["product_performance"] as List<Dictionary<string, object>>;

Console.WriteLine("Top Users by Spending:");
foreach (var user in userOrders)
{
    Console.WriteLine($"  {user["user_name"]}: {user["order_count"]} orders, ${user["total_spent"]}");
}

Console.WriteLine("\nTop Products by Revenue:");
foreach (var product in productPerformance)
{
    Console.WriteLine($"  {product["product_name"]}: {product["units_sold"]} units, ${product["revenue"]}");
}
```

## 🔄 Real-Time Configuration Updates

### Live Data Integration

```ini
# app.tsk - Real-time configuration
[system]
current_load: @query("SELECT AVG(cpu_usage) FROM system_metrics WHERE timestamp > ?", @date.subtract("5m"))
memory_usage: @query("SELECT AVG(memory_usage) FROM system_metrics WHERE timestamp > ?", @date.subtract("5m"))
active_sessions: @query("SELECT COUNT(*) FROM user_sessions WHERE last_activity > ?", @date.subtract("30m"))

[alerts]
high_load: @if($system.current_load > 80, true, false)
low_memory: @if($system.memory_usage > 90, true, false)
session_warning: @if($system.active_sessions > 1000, true, false)
```

### C# Real-Time Processing

```csharp
public class SystemMonitor
{
    private readonly TuskLang _parser;
    private readonly IDatabaseAdapter _adapter;
    
    public SystemMonitor()
    {
        _adapter = new SQLiteAdapter("system.db");
        _parser = new TuskLang();
        _parser.SetDatabaseAdapter(_adapter);
    }
    
    public async Task MonitorSystemAsync()
    {
        while (true)
        {
            var config = _parser.ParseFile("app.tsk");
            
            var currentLoad = Convert.ToDouble(config["system"]["current_load"]);
            var memoryUsage = Convert.ToDouble(config["system"]["memory_usage"]);
            var activeSessions = Convert.ToInt32(config["system"]["active_sessions"]);
            
            var highLoad = Convert.ToBoolean(config["alerts"]["high_load"]);
            var lowMemory = Convert.ToBoolean(config["alerts"]["low_memory"]);
            var sessionWarning = Convert.ToBoolean(config["alerts"]["session_warning"]);
            
            Console.WriteLine($"Load: {currentLoad:F1}%, Memory: {memoryUsage:F1}%, Sessions: {activeSessions}");
            
            if (highLoad) Console.WriteLine("⚠️  High load detected!");
            if (lowMemory) Console.WriteLine("⚠️  Low memory detected!");
            if (sessionWarning) Console.WriteLine("⚠️  High session count!");
            
            await Task.Delay(TimeSpan.FromMinutes(1));
        }
    }
}
```

## 🗃️ Multi-Database Support

### Multiple Database Adapters

```csharp
using TuskLang;
using TuskLang.Adapters;

public class MultiDatabaseConfig
{
    private readonly TuskLang _parser;
    private readonly Dictionary<string, IDatabaseAdapter> _adapters;
    
    public MultiDatabaseConfig()
    {
        _parser = new TuskLang();
        _adapters = new Dictionary<string, IDatabaseAdapter>
        {
            ["users"] = new PostgreSQLAdapter(new PostgreSQLConfig
            {
                Host = "user-db.example.com",
                Database = "users",
                User = "user_app",
                Password = "secret"
            }),
            ["analytics"] = new MongoDBAdapter(new MongoDBConfig
            {
                ConnectionString = "mongodb://analytics-db.example.com:27017",
                Database = "analytics"
            }),
            ["cache"] = new RedisAdapter(new RedisConfig
            {
                Host = "cache.example.com",
                Port = 6379,
                Password = "cache_secret"
            })
        };
    }
    
    public Dictionary<string, object> ParseConfiguration(string filePath)
    {
        // Set up database routing
        foreach (var adapter in _adapters)
        {
            _parser.SetDatabaseAdapter(adapter.Key, adapter.Value);
        }
        
        return _parser.ParseFile(filePath);
    }
}
```

### Multi-Database TSK Configuration

```ini
# app.tsk - Multi-database configuration
[user_stats]
total_users: @query.users("SELECT COUNT(*) FROM users")
active_users: @query.users("SELECT COUNT(*) FROM users WHERE active = 1")

[analytics_data]
page_views: @query.analytics("db.pageViews.find({date: {$gte: ?}}).count()", @date.subtract("7d"))
user_engagement: @query.analytics("db.userEngagement.aggregate([{$match: {date: {$gte: ?}}}, {$group: {_id: null, avg: {$avg: '$score'}}}])", @date.subtract("7d"))

[cache_stats]
hit_rate: @query.cache("GET cache:hit_rate")
miss_rate: @query.cache("GET cache:miss_rate")
total_requests: @query.cache("GET cache:total_requests")
```

## 🔒 Security and Best Practices

### Parameterized Queries (SQL Injection Prevention)

```ini
# Good: Parameterized queries
[secure]
user_by_id: @query("SELECT * FROM users WHERE id = ?", $user_id)
user_by_email: @query("SELECT * FROM users WHERE email = ?", $email)

# Bad: String concatenation (vulnerable to SQL injection)
[insecure]
user_by_id: @query("SELECT * FROM users WHERE id = ${user_id}")
user_by_email: @query("SELECT * FROM users WHERE email = '${email}'")
```

### Connection Pooling

```csharp
// PostgreSQL with connection pooling
var postgresAdapter = new PostgreSQLAdapter(new PostgreSQLConfig
{
    Host = "localhost",
    Database = "myapp",
    User = "postgres",
    Password = "secret"
}, new PoolConfig
{
    MaxOpenConns = 20,
    MaxIdleConns = 10,
    ConnMaxLifetime = 30000
});

// MySQL with connection pooling
var mysqlAdapter = new MySQLAdapter(new MySQLConfig
{
    Host = "localhost",
    Database = "myapp",
    User = "mysql_user",
    Password = "secret"
}, new PoolConfig
{
    MaxOpenConns = 25,
    MaxIdleConns = 15,
    ConnMaxLifetime = 60000
});
```

### Error Handling

```csharp
public class DatabaseConfigurationService
{
    private readonly TuskLang _parser;
    private readonly IDatabaseAdapter _adapter;
    
    public DatabaseConfigurationService()
    {
        _adapter = new SQLiteAdapter("app.db");
        _parser = new TuskLang();
        _parser.SetDatabaseAdapter(_adapter);
    }
    
    public Dictionary<string, object> GetConfiguration(string filePath)
    {
        try
        {
            return _parser.ParseFile(filePath);
        }
        catch (DatabaseConnectionException ex)
        {
            Console.WriteLine($"Database connection failed: {ex.Message}");
            // Fall back to cached configuration
            return GetCachedConfiguration();
        }
        catch (QueryExecutionException ex)
        {
            Console.WriteLine($"Query execution failed: {ex.Message}");
            // Use default values for failed queries
            return GetDefaultConfiguration();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
            throw;
        }
    }
    
    private Dictionary<string, object> GetCachedConfiguration()
    {
        // Return cached configuration when database is unavailable
        return new Dictionary<string, object>
        {
            ["stats"] = new Dictionary<string, object>
            {
                ["total_users"] = 0,
                ["active_users"] = 0
            }
        };
    }
    
    private Dictionary<string, object> GetDefaultConfiguration()
    {
        // Return default values when queries fail
        return new Dictionary<string, object>
        {
            ["stats"] = new Dictionary<string, object>
            {
                ["total_users"] = -1,
                ["active_users"] = -1
            }
        };
    }
}
```

## 🚀 Performance Optimization

### Query Caching

```ini
# app.tsk - Cached database queries
[performance]
expensive_stats: @cache("5m", @query("""
    SELECT 
        COUNT(*) as total_users,
        COUNT(CASE WHEN active = 1 THEN 1 END) as active_users,
        AVG(created_at) as avg_join_date
    FROM users
"""))

user_activity: @cache("1m", @query("""
    SELECT user_id, COUNT(*) as activity_count
    FROM user_activity
    WHERE created_at > ?
    GROUP BY user_id
    ORDER BY activity_count DESC
    LIMIT 100
""", @date.subtract("24h")))
```

### Connection Management

```csharp
public class OptimizedDatabaseService
{
    private readonly TuskLang _parser;
    private readonly IDatabaseAdapter _adapter;
    private readonly Timer _healthCheckTimer;
    
    public OptimizedDatabaseService()
    {
        _adapter = new PostgreSQLAdapter(new PostgreSQLConfig
        {
            Host = "localhost",
            Database = "myapp",
            User = "postgres",
            Password = "secret"
        }, new PoolConfig
        {
            MaxOpenConns = 50,
            MaxIdleConns = 20,
            ConnMaxLifetime = 300000, // 5 minutes
            ConnMaxIdleTime = 60000   // 1 minute
        });
        
        _parser = new TuskLang();
        _parser.SetDatabaseAdapter(_adapter);
        
        // Health check every 30 seconds
        _healthCheckTimer = new Timer(HealthCheck, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
    }
    
    private void HealthCheck(object state)
    {
        try
        {
            var result = _adapter.Query("SELECT 1");
            // Connection is healthy
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database health check failed: {ex.Message}");
            // Implement reconnection logic
        }
    }
    
    public Dictionary<string, object> GetConfiguration(string filePath)
    {
        return _parser.ParseFile(filePath);
    }
}
```

## 🧪 Testing Database Integration

### Unit Tests

```csharp
using Xunit;
using TuskLang;
using TuskLang.Adapters;

public class DatabaseIntegrationTests
{
    [Fact]
    public void TestBasicQuery()
    {
        // Arrange
        var adapter = new SQLiteAdapter(":memory:"); // In-memory database
        var parser = new TuskLang();
        parser.SetDatabaseAdapter(adapter);
        
        // Create test data
        adapter.Execute("CREATE TABLE users (id INTEGER PRIMARY KEY, name TEXT)");
        adapter.Execute("INSERT INTO users (name) VALUES ('Alice'), ('Bob')");
        
        // Act
        var tskContent = @"
[stats]
user_count: @query(""SELECT COUNT(*) FROM users"")
";
        var config = parser.Parse(tskContent);
        
        // Assert
        Assert.Equal(2, Convert.ToInt32(config["stats"]["user_count"]));
    }
    
    [Fact]
    public void TestParameterizedQuery()
    {
        // Arrange
        var adapter = new SQLiteAdapter(":memory:");
        var parser = new TuskLang();
        parser.SetDatabaseAdapter(adapter);
        
        adapter.Execute("CREATE TABLE users (id INTEGER PRIMARY KEY, name TEXT, active BOOLEAN)");
        adapter.Execute("INSERT INTO users (name, active) VALUES ('Alice', 1), ('Bob', 0)");
        
        Environment.SetEnvironmentVariable("USER_ID", "1");
        
        // Act
        var tskContent = @"
[user]
info: @query(""SELECT * FROM users WHERE id = ?"", $USER_ID)
";
        var config = parser.Parse(tskContent);
        
        // Assert
        var userInfo = config["user"]["info"] as List<Dictionary<string, object>>;
        Assert.Single(userInfo);
        Assert.Equal("Alice", userInfo[0]["name"]);
    }
    
    [Fact]
    public void TestComplexQuery()
    {
        // Arrange
        var adapter = new SQLiteAdapter(":memory:");
        var parser = new TuskLang();
        parser.SetDatabaseAdapter(adapter);
        
        // Create test schema
        adapter.Execute(@"
            CREATE TABLE users (id INTEGER PRIMARY KEY, name TEXT, created_at DATETIME);
            CREATE TABLE orders (id INTEGER PRIMARY KEY, user_id INTEGER, total REAL, created_at DATETIME);
        ");
        
        adapter.Execute(@"
            INSERT INTO users (name, created_at) VALUES 
            ('Alice', '2024-01-01'), ('Bob', '2024-01-02');
            INSERT INTO orders (user_id, total, created_at) VALUES 
            (1, 100.0, '2024-01-15'), (1, 200.0, '2024-01-16'), (2, 150.0, '2024-01-15');
        ");
        
        // Act
        var tskContent = @"
[analytics]
user_orders: @query("""
    SELECT 
        u.name as user_name,
        COUNT(o.id) as order_count,
        SUM(o.total) as total_spent
    FROM users u
    LEFT JOIN orders o ON u.id = o.user_id
    GROUP BY u.id, u.name
    ORDER BY total_spent DESC
""")
";
        var config = parser.Parse(tskContent);
        
        // Assert
        var userOrders = config["analytics"]["user_orders"] as List<Dictionary<string, object>>;
        Assert.Equal(2, userOrders.Count);
        Assert.Equal("Alice", userOrders[0]["user_name"]);
        Assert.Equal(300.0, Convert.ToDouble(userOrders[0]["total_spent"]));
    }
}
```

## 🎉 You're Ready!

You've mastered database integration with TuskLang! You can now:

- ✅ **Connect to multiple databases** - SQLite, PostgreSQL, MySQL, MongoDB, Redis
- ✅ **Write dynamic queries** - Real-time data in your configuration
- ✅ **Use parameterized queries** - Secure and flexible database access
- ✅ **Implement caching** - Optimize performance for expensive queries
- ✅ **Handle errors gracefully** - Robust error handling and fallbacks
- ✅ **Test thoroughly** - Comprehensive testing strategies

## 🔥 What's Next?

Ready to unleash the full power? Explore:

1. **[Advanced Features](005-advanced-features-csharp.md)** - Machine learning, encryption, and more
2. **[@ Operator System](006-operators-csharp.md)** - Master the complete @ operator ecosystem
3. **[Performance Optimization](007-performance-csharp.md)** - Optimize your configuration system
4. **[Production Deployment](008-production-csharp.md)** - Deploy to production environments

---

**"We don't bow to any king" - Your database, your configuration, your rules.**

Transform your static configuration into a living, breathing system! 🚀 