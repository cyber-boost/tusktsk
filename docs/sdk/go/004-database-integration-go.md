# 🗄️ TuskLang Go Database Integration Guide

**"We don't bow to any king" - Go Edition**

Execute database queries directly in your TuskLang configuration files. This guide covers SQLite, PostgreSQL, MySQL, MongoDB, and Redis integration with comprehensive Go examples.

## 🚀 Quick Database Start

### Basic Database Query

```go
// config.tsk
[app]
name: "Database App"

[stats]
total_users: @query("SELECT COUNT(*) FROM users")
active_users: @query("SELECT COUNT(*) FROM users WHERE active = 1")
recent_orders: @query("SELECT * FROM orders WHERE created_at > ?", @date.subtract("7d"))
```

```go
// main.go
package main

import (
    "fmt"
    "github.com/tusklang/go"
    "github.com/tusklang/go/adapters"
)

func main() {
    // Create SQLite adapter
    sqlite, err := adapters.NewSQLiteAdapter("app.db")
    if err != nil {
        panic(err)
    }
    
    // Create parser with database
    parser := tusklanggo.NewEnhancedParser()
    parser.SetDatabaseAdapter(sqlite)
    
    // Parse configuration with database queries
    data, err := parser.ParseFile("config.tsk")
    if err != nil {
        panic(err)
    }
    
    stats := data["stats"].(map[string]interface{})
    fmt.Printf("Total users: %v\n", stats["total_users"])
    fmt.Printf("Active users: %v\n", stats["active_users"])
}
```

## 🗃️ Database Adapters

### SQLite Adapter

```go
package main

import (
    "fmt"
    "github.com/tusklang/go/adapters"
)

func main() {
    // Basic SQLite connection
    sqlite, err := adapters.NewSQLiteAdapter("app.db")
    if err != nil {
        panic(err)
    }
    
    // With options
    sqlite, err = adapters.NewSQLiteAdapterWithOptions(adapters.SQLiteConfig{
        Filename: "app.db",
        Timeout:  30000,
        Verbose:  true,
        JournalMode: "WAL",
        CacheSize: 1000,
    })
    if err != nil {
        panic(err)
    }
    
    // Execute queries
    result, err := sqlite.Query("SELECT * FROM users WHERE active = ?", true)
    if err != nil {
        panic(err)
    }
    
    count, err := sqlite.Query("SELECT COUNT(*) FROM orders")
    if err != nil {
        panic(err)
    }
    
    fmt.Printf("Found %d active users\n", len(result))
    fmt.Printf("Total orders: %v\n", count[0]["COUNT(*)"])
}
```

### PostgreSQL Adapter

```go
package main

import (
    "fmt"
    "github.com/tusklang/go/adapters"
)

func main() {
    // Basic PostgreSQL connection
    postgres, err := adapters.NewPostgreSQLAdapter(adapters.PostgreSQLConfig{
        Host:     "localhost",
        Port:     5432,
        Database: "myapp",
        User:     "postgres",
        Password: "secret",
        SSLMode:  "require",
    })
    if err != nil {
        panic(err)
    }
    
    // With connection pooling
    postgres, err = adapters.NewPostgreSQLAdapterWithPool(adapters.PostgreSQLConfig{
        Host:     "localhost",
        Database: "myapp",
        User:     "postgres",
        Password: "secret",
    }, adapters.PoolConfig{
        MaxOpenConns:    20,
        MaxIdleConns:    10,
        ConnMaxLifetime: 3600,
        ConnMaxIdleTime: 1800,
    })
    if err != nil {
        panic(err)
    }
    
    // Execute queries
    result, err := postgres.Query("SELECT * FROM users WHERE active = $1", true)
    if err != nil {
        panic(err)
    }
    
    // Use connection string
    postgres, err = adapters.NewPostgreSQLAdapterFromURL("postgres://user:pass@localhost:5432/myapp?sslmode=require")
    if err != nil {
        panic(err)
    }
    
    fmt.Printf("Found %d active users\n", len(result))
}
```

### MySQL Adapter

```go
package main

import (
    "fmt"
    "github.com/tusklang/go/adapters"
)

func main() {
    // Basic MySQL connection
    mysql, err := adapters.NewMySQLAdapter(adapters.MySQLConfig{
        Host:     "localhost",
        Port:     3306,
        Database: "myapp",
        User:     "root",
        Password: "secret",
        Charset:  "utf8mb4",
    })
    if err != nil {
        panic(err)
    }
    
    // With connection pooling
    mysql, err = adapters.NewMySQLAdapterWithPool(adapters.MySQLConfig{
        Host:     "localhost",
        Database: "myapp",
        User:     "root",
        Password: "secret",
    }, adapters.PoolConfig{
        MaxOpenConns:    20,
        MaxIdleConns:    10,
        ConnMaxLifetime: 3600,
        ConnMaxIdleTime: 1800,
    })
    if err != nil {
        panic(err)
    }
    
    // Execute queries
    result, err := mysql.Query("SELECT * FROM users WHERE active = ?", true)
    if err != nil {
        panic(err)
    }
    
    fmt.Printf("Found %d active users\n", len(result))
}
```

### MongoDB Adapter

```go
package main

import (
    "fmt"
    "github.com/tusklang/go/adapters"
)

func main() {
    // Basic MongoDB connection
    mongo, err := adapters.NewMongoDBAdapter(adapters.MongoDBConfig{
        URI:      "mongodb://localhost:27017/",
        Database: "myapp",
    })
    if err != nil {
        panic(err)
    }
    
    // With authentication
    mongo, err = adapters.NewMongoDBAdapter(adapters.MongoDBConfig{
        URI:        "mongodb://user:pass@localhost:27017/",
        Database:   "myapp",
        AuthSource: "admin",
    })
    if err != nil {
        panic(err)
    }
    
    // Execute queries
    users, err := mongo.Query("users", map[string]interface{}{"active": true})
    if err != nil {
        panic(err)
    }
    
    count, err := mongo.Query("users", map[string]interface{}{}, map[string]interface{}{"count": true})
    if err != nil {
        panic(err)
    }
    
    fmt.Printf("Found %d active users\n", len(users))
    fmt.Printf("Total users: %v\n", count)
}
```

### Redis Adapter

```go
package main

import (
    "fmt"
    "github.com/tusklang/go/adapters"
)

func main() {
    // Basic Redis connection
    redis, err := adapters.NewRedisAdapter(adapters.RedisConfig{
        Host: "localhost",
        Port: 6379,
        DB:   0,
    })
    if err != nil {
        panic(err)
    }
    
    // With authentication
    redis, err = adapters.NewRedisAdapter(adapters.RedisConfig{
        Host:     "localhost",
        Port:     6379,
        Password: "secret",
        DB:       0,
    })
    if err != nil {
        panic(err)
    }
    
    // Execute commands
    err = redis.Set("key", "value")
    if err != nil {
        panic(err)
    }
    
    value, err := redis.Get("key")
    if err != nil {
        panic(err)
    }
    
    err = redis.Del("key")
    if err != nil {
        panic(err)
    }
    
    fmt.Printf("Value: %s\n", value)
}
```

## 🔍 Query Execution in TSK Files

### Basic Queries

```go
// config.tsk
[app]
name: "Database App"

[stats]
total_users: @query("SELECT COUNT(*) FROM users")
active_users: @query("SELECT COUNT(*) FROM users WHERE active = 1")
recent_orders: @query("SELECT * FROM orders WHERE created_at > ?", @date.subtract("7d"))
user_list: @query("SELECT id, name, email FROM users LIMIT 10")
```

```go
// main.go
type Config struct {
    App struct {
        Name string `tsk:"name"`
    } `tsk:"app"`
    
    Stats struct {
        TotalUsers   interface{} `tsk:"total_users"`
        ActiveUsers  interface{} `tsk:"active_users"`
        RecentOrders interface{} `tsk:"recent_orders"`
        UserList     interface{} `tsk:"user_list"`
    } `tsk:"stats"`
} `tsk:""`
```

### Parameterized Queries

```go
// config.tsk
[queries]
user_by_id: @query("SELECT * FROM users WHERE id = ?", @request.user_id)
users_by_role: @query("SELECT * FROM users WHERE role = ?", @request.role)
orders_by_date: @query("SELECT * FROM orders WHERE created_at BETWEEN ? AND ?", @request.start_date, @request.end_date)
search_users: @query("SELECT * FROM users WHERE name LIKE ? OR email LIKE ?", @request.search_term, @request.search_term)
```

```go
// main.go
func main() {
    parser := tusklanggo.NewEnhancedParser()
    parser.SetDatabaseAdapter(sqlite)
    
    // Execute with request context
    context := map[string]interface{}{
        "request": map[string]interface{}{
            "user_id":    123,
            "role":       "admin",
            "start_date": "2024-01-01",
            "end_date":   "2024-01-31",
            "search_term": "%john%",
        },
    }
    
    data, err := parser.ParseStringWithContext(tskContent, context)
    if err != nil {
        panic(err)
    }
    
    fmt.Printf("Query results: %+v\n", data["queries"])
}
```

### Complex Queries

```go
// config.tsk
[analytics]
user_stats: @query("""
    SELECT 
        u.id,
        u.name,
        COUNT(o.id) as order_count,
        SUM(o.total) as total_spent,
        AVG(o.total) as avg_order_value
    FROM users u
    LEFT JOIN orders o ON u.id = o.user_id
    WHERE u.active = 1
    GROUP BY u.id, u.name
    ORDER BY total_spent DESC
    LIMIT 10
""")

revenue_by_month: @query("""
    SELECT 
        DATE_FORMAT(created_at, '%Y-%m') as month,
        COUNT(*) as order_count,
        SUM(total) as revenue
    FROM orders
    WHERE created_at >= DATE_SUB(NOW(), INTERVAL 12 MONTH)
    GROUP BY month
    ORDER BY month
""")
```

## 🔄 Transaction Support

### Basic Transactions

```go
// config.tsk
[operations]
create_user: @transaction("""
    INSERT INTO users (name, email, created_at) VALUES (?, ?, NOW());
    INSERT INTO user_profiles (user_id, bio) VALUES (LAST_INSERT_ID(), ?);
""", @request.name, @request.email, @request.bio)

update_order: @transaction("""
    UPDATE orders SET status = ?, updated_at = NOW() WHERE id = ?;
    INSERT INTO order_history (order_id, status, created_at) VALUES (?, ?, NOW());
""", @request.status, @request.order_id, @request.order_id, @request.status)
```

```go
// main.go
func main() {
    parser := tusklanggo.NewEnhancedParser()
    parser.SetDatabaseAdapter(postgres)
    
    context := map[string]interface{}{
        "request": map[string]interface{}{
            "name":  "John Doe",
            "email": "john@example.com",
            "bio":   "Software developer",
        },
    }
    
    data, err := parser.ParseStringWithContext(tskContent, context)
    if err != nil {
        panic(err)
    }
    
    fmt.Printf("User created successfully\n")
}
```

### Rollback on Error

```go
// config.tsk
[operations]
safe_transfer: @transaction.rollback("""
    UPDATE accounts SET balance = balance - ? WHERE id = ? AND balance >= ?;
    UPDATE accounts SET balance = balance + ? WHERE id = ?;
""", @request.amount, @request.from_account, @request.amount, @request.amount, @request.to_account)
```

## 📊 Advanced Query Features

### Aggregation Queries

```go
// config.tsk
[analytics]
daily_revenue: @query("""
    SELECT 
        DATE(created_at) as date,
        COUNT(*) as orders,
        SUM(total) as revenue,
        AVG(total) as avg_order
    FROM orders
    WHERE created_at >= DATE_SUB(NOW(), INTERVAL 30 DAY)
    GROUP BY DATE(created_at)
    ORDER BY date DESC
""")

user_activity: @query("""
    SELECT 
        u.name,
        COUNT(DISTINCT o.id) as order_count,
        COUNT(DISTINCT DATE(o.created_at)) as active_days,
        MAX(o.created_at) as last_order
    FROM users u
    LEFT JOIN orders o ON u.id = o.user_id
    GROUP BY u.id, u.name
    HAVING order_count > 0
    ORDER BY order_count DESC
""")
```

### Subqueries and Joins

```go
// config.tsk
[reports]
top_customers: @query("""
    SELECT 
        u.name,
        u.email,
        COUNT(o.id) as order_count,
        SUM(o.total) as total_spent
    FROM users u
    INNER JOIN (
        SELECT user_id, COUNT(*) as order_count
        FROM orders
        GROUP BY user_id
        HAVING order_count >= 5
    ) active_users ON u.id = active_users.user_id
    LEFT JOIN orders o ON u.id = o.user_id
    GROUP BY u.id, u.name, u.email
    ORDER BY total_spent DESC
    LIMIT 10
""")

recent_activity: @query("""
    SELECT 
        'order' as type,
        o.id as item_id,
        u.name as user_name,
        o.created_at as timestamp
    FROM orders o
    JOIN users u ON o.user_id = u.id
    WHERE o.created_at >= DATE_SUB(NOW(), INTERVAL 7 DAY)
    
    UNION ALL
    
    SELECT 
        'review' as type,
        r.id as item_id,
        u.name as user_name,
        r.created_at as timestamp
    FROM reviews r
    JOIN users u ON r.user_id = u.id
    WHERE r.created_at >= DATE_SUB(NOW(), INTERVAL 7 DAY)
    
    ORDER BY timestamp DESC
    LIMIT 50
""")
```

## 🔐 Security Features

### SQL Injection Prevention

```go
// config.tsk
[secure_queries]
user_by_id: @query("SELECT * FROM users WHERE id = ?", @request.user_id)
search_users: @query("SELECT * FROM users WHERE name LIKE ?", @request.search_term)
filter_orders: @query("SELECT * FROM orders WHERE status = ? AND user_id = ?", @request.status, @request.user_id)
```

```go
// main.go
func main() {
    parser := tusklanggo.NewEnhancedParser()
    parser.SetDatabaseAdapter(postgres)
    
    // Safe execution with parameterized queries
    context := map[string]interface{}{
        "request": map[string]interface{}{
            "user_id":     123,
            "search_term": "%john%",
            "status":      "completed",
        },
    }
    
    data, err := parser.ParseStringWithContext(tskContent, context)
    if err != nil {
        panic(err)
    }
    
    // All queries are automatically parameterized
    fmt.Printf("Secure query results: %+v\n", data["secure_queries"])
}
```

### Query Validation

```go
// config.tsk
[validated_queries]
user_data: @query.validate("SELECT", "users", "WHERE id = ?", @request.user_id)
order_summary: @query.validate("SELECT", "orders", "WHERE user_id = ?", @request.user_id)
```

## 🚀 Performance Optimization

### Query Caching

```go
// config.tsk
[performance]
expensive_stats: @cache("5m", @query("SELECT COUNT(*) FROM large_table"))
user_analytics: @cache("1h", @query("""
    SELECT user_id, COUNT(*) as order_count, SUM(total) as total_spent
    FROM orders
    GROUP BY user_id
"""))
daily_revenue: @cache("1d", @query("""
    SELECT DATE(created_at) as date, SUM(total) as revenue
    FROM orders
    WHERE created_at >= DATE_SUB(NOW(), INTERVAL 30 DAY)
    GROUP BY DATE(created_at)
"""))
```

### Connection Pooling

```go
// main.go
func main() {
    // Configure connection pooling
    postgres, err := adapters.NewPostgreSQLAdapterWithPool(adapters.PostgreSQLConfig{
        Host:     "localhost",
        Database: "myapp",
        User:     "postgres",
        Password: "secret",
    }, adapters.PoolConfig{
        MaxOpenConns:    20,
        MaxIdleConns:    10,
        ConnMaxLifetime: 3600,
        ConnMaxIdleTime: 1800,
    })
    if err != nil {
        panic(err)
    }
    
    parser := tusklanggo.NewEnhancedParser()
    parser.SetDatabaseAdapter(postgres)
    
    // Execute multiple queries efficiently
    data, err := parser.ParseFile("config.tsk")
    if err != nil {
        panic(err)
    }
    
    fmt.Printf("All queries executed with connection pooling\n")
}
```

## 🔧 Error Handling

### Graceful Query Failures

```go
// config.tsk
[robust_queries]
user_count: @query.fallback(@query("SELECT COUNT(*) FROM users"), 0)
active_users: @query.fallback(@query("SELECT COUNT(*) FROM users WHERE active = 1"), 0)
recent_orders: @query.fallback(@query("SELECT * FROM orders WHERE created_at > ?", @date.subtract("7d")), [])
```

```go
// main.go
func main() {
    parser := tusklanggo.NewEnhancedParser()
    parser.SetDatabaseAdapter(sqlite)
    
    // Handle query errors gracefully
    data, err := parser.ParseFile("config.tsk")
    if err != nil {
        log.Printf("Some queries failed: %v", err)
        // Continue with fallback values
    }
    
    stats := data["robust_queries"].(map[string]interface{})
    fmt.Printf("User count: %v\n", stats["user_count"])
    fmt.Printf("Active users: %v\n", stats["active_users"])
}
```

### Query Timeout

```go
// config.tsk
[timeout_queries]
slow_query: @query.timeout("30s", @query("SELECT * FROM very_large_table"))
complex_analytics: @query.timeout("2m", @query("""
    SELECT 
        u.name,
        COUNT(o.id) as orders,
        SUM(o.total) as revenue
    FROM users u
    LEFT JOIN orders o ON u.id = o.user_id
    GROUP BY u.id, u.name
    ORDER BY revenue DESC
"""))
```

## 📊 Monitoring and Metrics

### Query Performance Tracking

```go
// config.tsk
[monitoring]
query_metrics: @metrics("database_query_time", @query("SELECT COUNT(*) FROM users"))
slow_queries: @metrics("slow_query_count", @query("""
    SELECT COUNT(*) 
    FROM information_schema.processlist 
    WHERE time > 10
"""))
```

```go
// main.go
func main() {
    parser := tusklanggo.NewEnhancedParser()
    parser.SetDatabaseAdapter(postgres)
    
    // Enable query monitoring
    parser.SetQueryMonitor(func(query string, duration time.Duration, err error) {
        if err != nil {
            log.Printf("Query failed: %s (%.2fms): %v", query, duration.Seconds()*1000, err)
        } else if duration > time.Second {
            log.Printf("Slow query: %s (%.2fms)", query, duration.Seconds()*1000)
        }
    })
    
    data, err := parser.ParseFile("config.tsk")
    if err != nil {
        panic(err)
    }
    
    fmt.Printf("Queries executed with monitoring\n")
}
```

## 🎯 Best Practices

### 1. Use Parameterized Queries

```go
// Good - Parameterized
user_data: @query("SELECT * FROM users WHERE id = ?", @request.user_id)

// Bad - String concatenation
user_data: @query("SELECT * FROM users WHERE id = " + @request.user_id)
```

### 2. Cache Expensive Queries

```go
// Cache expensive operations
expensive_stats: @cache("5m", @query("SELECT COUNT(*) FROM large_table"))
user_analytics: @cache("1h", @query("SELECT * FROM user_statistics"))
```

### 3. Use Appropriate Timeouts

```go
// Set timeouts for long-running queries
complex_report: @query.timeout("2m", @query("SELECT * FROM complex_analytics"))
```

### 4. Handle Errors Gracefully

```go
// Use fallbacks for critical queries
user_count: @query.fallback(@query("SELECT COUNT(*) FROM users"), 0)
```

### 5. Monitor Query Performance

```go
// Track query metrics
query_time: @metrics("database_query_time", @query("SELECT COUNT(*) FROM users"))
```

## 🔍 Debugging Database Queries

### Enable Query Logging

```go
// main.go
func main() {
    parser := tusklanggo.NewEnhancedParser()
    parser.SetDatabaseAdapter(postgres)
    
    // Enable query logging
    parser.SetQueryLogger(func(query string, args []interface{}, duration time.Duration) {
        log.Printf("Query: %s | Args: %v | Duration: %.2fms", query, args, duration.Seconds()*1000)
    })
    
    data, err := parser.ParseFile("config.tsk")
    if err != nil {
        panic(err)
    }
    
    fmt.Printf("Queries logged successfully\n")
}
```

### Validate Queries

```bash
# Validate TSK file with database queries
tusk validate config.tsk --database

# Show query plan
tusk explain config.tsk --query "user_stats"
```

## 📚 Summary

You've learned:

1. **Database Adapters** - SQLite, PostgreSQL, MySQL, MongoDB, Redis
2. **Query Execution** - Basic and parameterized queries in TSK files
3. **Transactions** - ACID operations with rollback support
4. **Advanced Queries** - Aggregations, subqueries, and complex joins
5. **Security** - SQL injection prevention and query validation
6. **Performance** - Caching, connection pooling, and optimization
7. **Error Handling** - Graceful failures and timeouts
8. **Monitoring** - Query performance tracking and metrics
9. **Best Practices** - Secure and efficient database operations
10. **Debugging** - Query logging and validation

## 🚀 Next Steps

Now that you understand database integration:

1. **Explore @ Operators** - Learn about @date, @cache, @metrics, etc.
2. **Advanced Features** - FUJSEN, machine learning, real-time monitoring
3. **Web Framework Integration** - Use with Gin, Echo, and other frameworks
4. **Deployment** - Docker, Kubernetes, and cloud deployment
5. **Performance Tuning** - Query optimization and caching strategies

---

**"We don't bow to any king"** - You now have the power to execute database queries directly in your configuration files with type-safe Go integration! 