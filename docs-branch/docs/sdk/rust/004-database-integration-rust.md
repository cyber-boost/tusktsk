# 🦀 TuskLang Rust Database Integration

**"We don't bow to any king" - Rust Edition**

Unlock the power of database queries directly in your TuskLang configuration. From SQLite to PostgreSQL, MySQL to MongoDB, and Redis caching - TuskLang Rust provides zero-copy database integration with type safety and performance you demand.

## 🗄️ Database Adapters Overview

### Supported Databases

```rust
use tusklang_rust::adapters::{
    sqlite::SQLiteAdapter,
    postgresql::PostgreSQLAdapter,
    mysql::MySQLAdapter,
    mongodb::MongoDBAdapter,
    redis::RedisAdapter,
    DatabaseAdapter,
    CacheAdapter,
};
```

## 🔧 SQLite Integration

### Basic Setup

```rust
use tusklang_rust::{parse, Parser};
use tusklang_rust::adapters::sqlite::SQLiteAdapter;
use std::time::Duration;

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    let mut parser = Parser::new();
    
    // Basic SQLite connection
    let sqlite = SQLiteAdapter::new("app.db").await?;
    
    // With advanced options
    let sqlite = SQLiteAdapter::with_options(SQLiteConfig {
        filename: "app.db".to_string(),
        timeout: Duration::from_secs(30),
        verbose: true,
        pragmas: vec![
            "PRAGMA journal_mode=WAL".to_string(),
            "PRAGMA synchronous=NORMAL".to_string(),
            "PRAGMA cache_size=10000".to_string(),
        ],
    }).await?;
    
    parser.set_database_adapter(sqlite);
    
    let tsk_content = r#"
[users]
total_count: @query("SELECT COUNT(*) FROM users")
active_users: @query("SELECT COUNT(*) FROM users WHERE active = 1")
recent_users: @query("SELECT * FROM users WHERE created_at > datetime('now', '-7 days')")
"#;
    
    let data = parser.parse(tsk_content).await?;
    
    println!("Total users: {}", data["users"]["total_count"]);
    println!("Active users: {}", data["users"]["active_users"]);
    println!("Recent users: {:?}", data["users"]["recent_users"]);
    
    Ok(())
}
```

### Advanced SQLite Features

```rust
use tusklang_rust::{parse, Parser};
use tusklang_rust::adapters::sqlite::SQLiteAdapter;

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    let mut parser = Parser::new();
    let sqlite = SQLiteAdapter::new("app.db").await?;
    parser.set_database_adapter(sqlite);
    
    // Setup test data
    sqlite.execute(r#"
        CREATE TABLE IF NOT EXISTS users (
            id INTEGER PRIMARY KEY,
            name TEXT NOT NULL,
            email TEXT UNIQUE,
            active BOOLEAN DEFAULT 1,
            created_at DATETIME DEFAULT CURRENT_TIMESTAMP
        );
        
        INSERT OR IGNORE INTO users (name, email) VALUES 
            ('Alice', 'alice@example.com'),
            ('Bob', 'bob@example.com'),
            ('Charlie', 'charlie@example.com');
    "#).await?;
    
    let tsk_content = r#"
[user_stats]
total: @query("SELECT COUNT(*) FROM users")
active: @query("SELECT COUNT(*) FROM users WHERE active = 1")
inactive: @query("SELECT COUNT(*) FROM users WHERE active = 0")

[user_data]
all_users: @query("SELECT * FROM users ORDER BY created_at DESC")
active_users: @query("SELECT name, email FROM users WHERE active = 1")
recent_users: @query("SELECT * FROM users WHERE created_at > datetime('now', '-1 day')")

[user_analytics]
avg_name_length: @query("SELECT AVG(LENGTH(name)) FROM users")
email_domains: @query("SELECT DISTINCT SUBSTR(email, INSTR(email, '@') + 1) as domain FROM users")
"#;
    
    let data = parser.parse(tsk_content).await?;
    
    println!("User Statistics:");
    println!("  Total: {}", data["user_stats"]["total"]);
    println!("  Active: {}", data["user_stats"]["active"]);
    println!("  Inactive: {}", data["user_stats"]["inactive"]);
    
    println!("User Data:");
    println!("  All users: {:?}", data["user_data"]["all_users"]);
    println!("  Active users: {:?}", data["user_data"]["active_users"]);
    
    println!("Analytics:");
    println!("  Average name length: {}", data["user_analytics"]["avg_name_length"]);
    println!("  Email domains: {:?}", data["user_analytics"]["email_domains"]);
    
    Ok(())
}
```

## 🐘 PostgreSQL Integration

### Basic Setup

```rust
use tusklang_rust::{parse, Parser};
use tusklang_rust::adapters::postgresql::{PostgreSQLAdapter, PostgreSQLConfig, PoolConfig};
use std::time::Duration;

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    let mut parser = Parser::new();
    
    // Basic PostgreSQL connection
    let postgres = PostgreSQLAdapter::new(PostgreSQLConfig {
        host: "localhost".to_string(),
        port: 5432,
        database: "myapp".to_string(),
        user: "postgres".to_string(),
        password: "secret".to_string(),
        ssl_mode: "require".to_string(),
    }).await?;
    
    // With connection pooling
    let postgres = PostgreSQLAdapter::with_pool(PostgreSQLConfig {
        host: "localhost".to_string(),
        port: 5432,
        database: "myapp".to_string(),
        user: "postgres".to_string(),
        password: "secret".to_string(),
        ssl_mode: "disable".to_string(),
    }, PoolConfig {
        max_open_conns: 20,
        max_idle_conns: 10,
        conn_max_lifetime: Duration::from_secs(30),
        conn_max_idle_time: Duration::from_secs(10),
    }).await?;
    
    parser.set_database_adapter(postgres);
    
    let tsk_content = r#"
[orders]
total_orders: @query("SELECT COUNT(*) FROM orders")
total_revenue: @query("SELECT SUM(amount) FROM orders WHERE status = 'completed'")
avg_order_value: @query("SELECT AVG(amount) FROM orders WHERE status = 'completed'")

[customers]
total_customers: @query("SELECT COUNT(*) FROM customers")
active_customers: @query("SELECT COUNT(*) FROM customers WHERE last_order_date > NOW() - INTERVAL '30 days'")
"#;
    
    let data = parser.parse(tsk_content).await?;
    
    println!("Orders: {}", data["orders"]["total_orders"]);
    println!("Revenue: ${}", data["orders"]["total_revenue"]);
    println!("Avg Order: ${}", data["orders"]["avg_order_value"]);
    println!("Customers: {}", data["customers"]["total_customers"]);
    
    Ok(())
}
```

### Advanced PostgreSQL Features

```rust
use tusklang_rust::{parse, Parser};
use tusklang_rust::adapters::postgresql::PostgreSQLAdapter;

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    let mut parser = Parser::new();
    let postgres = PostgreSQLAdapter::new(PostgreSQLConfig {
        host: "localhost".to_string(),
        port: 5432,
        database: "myapp".to_string(),
        user: "postgres".to_string(),
        password: "secret".to_string(),
        ssl_mode: "disable".to_string(),
    }).await?;
    
    parser.set_database_adapter(postgres);
    
    // Setup test data
    postgres.execute(r#"
        CREATE TABLE IF NOT EXISTS products (
            id SERIAL PRIMARY KEY,
            name VARCHAR(255) NOT NULL,
            price DECIMAL(10,2) NOT NULL,
            category VARCHAR(100),
            stock_quantity INTEGER DEFAULT 0,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        );
        
        INSERT INTO products (name, price, category, stock_quantity) VALUES 
            ('Laptop', 999.99, 'Electronics', 50),
            ('Mouse', 29.99, 'Electronics', 100),
            ('Desk', 199.99, 'Furniture', 25),
            ('Chair', 149.99, 'Furniture', 30);
    "#).await?;
    
    let tsk_content = r#"
[product_stats]
total_products: @query("SELECT COUNT(*) FROM products")
total_value: @query("SELECT SUM(price * stock_quantity) FROM products")
avg_price: @query("SELECT AVG(price) FROM products")

[category_stats]
electronics_count: @query("SELECT COUNT(*) FROM products WHERE category = 'Electronics'")
electronics_value: @query("SELECT SUM(price * stock_quantity) FROM products WHERE category = 'Electronics'")
furniture_count: @query("SELECT COUNT(*) FROM products WHERE category = 'Furniture'")
furniture_value: @query("SELECT SUM(price * stock_quantity) FROM products WHERE category = 'Furniture'")

[inventory_alerts]
low_stock: @query("SELECT name, stock_quantity FROM products WHERE stock_quantity < 10")
out_of_stock: @query("SELECT name FROM products WHERE stock_quantity = 0")
"#;
    
    let data = parser.parse(tsk_content).await?;
    
    println!("Product Statistics:");
    println!("  Total products: {}", data["product_stats"]["total_products"]);
    println!("  Total inventory value: ${}", data["product_stats"]["total_value"]);
    println!("  Average price: ${}", data["product_stats"]["avg_price"]);
    
    println!("Category Breakdown:");
    println!("  Electronics: {} items, ${} value", 
        data["category_stats"]["electronics_count"],
        data["category_stats"]["electronics_value"]
    );
    println!("  Furniture: {} items, ${} value",
        data["category_stats"]["furniture_count"],
        data["category_stats"]["furniture_value"]
    );
    
    println!("Inventory Alerts:");
    println!("  Low stock: {:?}", data["inventory_alerts"]["low_stock"]);
    println!("  Out of stock: {:?}", data["inventory_alerts"]["out_of_stock"]);
    
    Ok(())
}
```

## 🐬 MySQL Integration

### Basic Setup

```rust
use tusklang_rust::{parse, Parser};
use tusklang_rust::adapters::mysql::{MySQLAdapter, MySQLConfig, PoolConfig};
use std::time::Duration;

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    let mut parser = Parser::new();
    
    // Basic MySQL connection
    let mysql = MySQLAdapter::new(MySQLConfig {
        host: "localhost".to_string(),
        port: 3306,
        database: "myapp".to_string(),
        user: "root".to_string(),
        password: "secret".to_string(),
    }).await?;
    
    // With connection pooling
    let mysql = MySQLAdapter::with_pool(MySQLConfig {
        host: "localhost".to_string(),
        port: 3306,
        database: "myapp".to_string(),
        user: "root".to_string(),
        password: "secret".to_string(),
    }, PoolConfig {
        max_open_conns: 10,
        max_idle_conns: 5,
        conn_max_lifetime: Duration::from_secs(60),
        conn_max_idle_time: Duration::from_secs(30),
    }).await?;
    
    parser.set_database_adapter(mysql);
    
    let tsk_content = r#"
[analytics]
page_views: @query("SELECT COUNT(*) FROM page_views WHERE DATE(created_at) = CURDATE()")
unique_visitors: @query("SELECT COUNT(DISTINCT user_id) FROM page_views WHERE DATE(created_at) = CURDATE()")
avg_session_duration: @query("SELECT AVG(session_duration) FROM user_sessions WHERE DATE(created_at) = CURDATE()")
"#;
    
    let data = parser.parse(tsk_content).await?;
    
    println!("Today's Analytics:");
    println!("  Page views: {}", data["analytics"]["page_views"]);
    println!("  Unique visitors: {}", data["analytics"]["unique_visitors"]);
    println!("  Avg session: {} seconds", data["analytics"]["avg_session_duration"]);
    
    Ok(())
}
```

## 🍃 MongoDB Integration

### Basic Setup

```rust
use tusklang_rust::{parse, Parser};
use tusklang_rust::adapters::mongodb::{MongoDBAdapter, MongoDBConfig};

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    let mut parser = Parser::new();
    
    // Basic MongoDB connection
    let mongo = MongoDBAdapter::new(MongoDBConfig {
        uri: "mongodb://localhost:27017/".to_string(),
        database: "myapp".to_string(),
    }).await?;
    
    // With authentication
    let mongo = MongoDBAdapter::new(MongoDBConfig {
        uri: "mongodb://user:pass@localhost:27017/".to_string(),
        database: "myapp".to_string(),
        auth_source: "admin".to_string(),
    }).await?;
    
    parser.set_database_adapter(mongo);
    
    let tsk_content = r#"
[users]
total_users: @query("users", {}, {"count": true})
active_users: @query("users", {"status": "active"}, {"count": true})
recent_users: @query("users", {"created_at": {"$gte": {"$date": "2024-01-01"}}}, {})

[posts]
total_posts: @query("posts", {}, {"count": true})
published_posts: @query("posts", {"status": "published"}, {"count": true})
popular_posts: @query("posts", {"views": {"$gte": 1000}}, {})
"#;
    
    let data = parser.parse(tsk_content).await?;
    
    println!("Users: {}", data["users"]["total_users"]);
    println!("Active users: {}", data["users"]["active_users"]);
    println!("Posts: {}", data["posts"]["total_posts"]);
    println!("Published posts: {}", data["posts"]["published_posts"]);
    
    Ok(())
}
```

## 🔴 Redis Integration

### Basic Setup

```rust
use tusklang_rust::{parse, Parser};
use tusklang_rust::adapters::redis::{RedisAdapter, RedisConfig};

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    let mut parser = Parser::new();
    
    // Basic Redis connection
    let redis = RedisAdapter::new(RedisConfig {
        host: "localhost".to_string(),
        port: 6379,
        db: 0,
    }).await?;
    
    // With authentication
    let redis = RedisAdapter::new(RedisConfig {
        host: "localhost".to_string(),
        port: 6379,
        password: "secret".to_string(),
        db: 0,
    }).await?;
    
    parser.set_cache_adapter(redis);
    
    let tsk_content = r#"
[cache_stats]
total_keys: @cache.keys("*")
memory_usage: @cache.memory_usage()
connected_clients: @cache.info("clients")
"#;
    
    let data = parser.parse(tsk_content).await?;
    
    println!("Cache Statistics:");
    println!("  Total keys: {}", data["cache_stats"]["total_keys"]);
    println!("  Memory usage: {} bytes", data["cache_stats"]["memory_usage"]);
    println!("  Connected clients: {}", data["cache_stats"]["connected_clients"]);
    
    Ok(())
}
```

## 🔄 Cross-Database Operations

### Multi-Database Setup

```rust
use tusklang_rust::{parse, Parser};
use tusklang_rust::adapters::{
    sqlite::SQLiteAdapter,
    postgresql::PostgreSQLAdapter,
    redis::RedisAdapter,
};

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    let mut parser = Parser::new();
    
    // Setup multiple database adapters
    let sqlite = SQLiteAdapter::new("local.db").await?;
    let postgres = PostgreSQLAdapter::new(PostgreSQLConfig {
        host: "localhost".to_string(),
        port: 5432,
        database: "production".to_string(),
        user: "postgres".to_string(),
        password: "secret".to_string(),
        ssl_mode: "disable".to_string(),
    }).await?;
    let redis = RedisAdapter::new(RedisConfig {
        host: "localhost".to_string(),
        port: 6379,
        db: 0,
    }).await?;
    
    // Set primary database and cache
    parser.set_database_adapter(postgres);
    parser.set_cache_adapter(redis);
    
    // Add additional databases
    parser.add_database("local", sqlite);
    
    let tsk_content = r#"
[production_data]
users: @query("SELECT COUNT(*) FROM users")
orders: @query("SELECT COUNT(*) FROM orders")

[local_data]
temp_data: @query("local", "SELECT COUNT(*) FROM temp_table")
logs: @query("local", "SELECT COUNT(*) FROM logs")

[cache_data]
session_count: @cache.get("active_sessions")
api_calls: @cache.get("api_call_count")
"#;
    
    let data = parser.parse(tsk_content).await?;
    
    println!("Production Data:");
    println!("  Users: {}", data["production_data"]["users"]);
    println!("  Orders: {}", data["production_data"]["orders"]);
    
    println!("Local Data:");
    println!("  Temp data: {}", data["local_data"]["temp_data"]);
    println!("  Logs: {}", data["local_data"]["logs"]);
    
    println!("Cache Data:");
    println!("  Sessions: {}", data["cache_data"]["session_count"]);
    println!("  API calls: {}", data["cache_data"]["api_calls"]);
    
    Ok(())
}
```

## 🧪 Database Testing

### Unit Tests

```rust
use tusklang_rust::{parse, Parser, adapters::sqlite::SQLiteAdapter};
use tokio_test;

#[tokio::test]
async fn test_database_integration() {
    // Setup test database
    let db = SQLiteAdapter::new(":memory:").await.expect("Failed to create database");
    
    let mut parser = Parser::new();
    parser.set_database_adapter(db);
    
    // Setup test data
    db.execute(r#"
        CREATE TABLE users (id INTEGER, name TEXT, active BOOLEAN);
        INSERT INTO users VALUES (1, 'Alice', 1), (2, 'Bob', 0);
    "#).await.expect("Failed to setup test data");
    
    let tsk_content = r#"
[users]
count: @query("SELECT COUNT(*) FROM users")
active_count: @query("SELECT COUNT(*) FROM users WHERE active = 1")
"#;
    
    let data = parser.parse(tsk_content).await.expect("Failed to parse");
    
    assert_eq!(data["users"]["count"], 2);
    assert_eq!(data["users"]["active_count"], 1);
    
    println!("✅ Database integration test passed!");
}
```

### Integration Tests

```rust
use tusklang_rust::{parse, Parser, adapters::postgresql::PostgreSQLAdapter};
use tokio_test;

#[tokio::test]
async fn test_postgresql_integration() {
    let postgres = PostgreSQLAdapter::new(PostgreSQLConfig {
        host: "localhost".to_string(),
        port: 5432,
        database: "test_db".to_string(),
        user: "postgres".to_string(),
        password: "secret".to_string(),
        ssl_mode: "disable".to_string(),
    }).await.expect("Failed to connect to PostgreSQL");
    
    let mut parser = Parser::new();
    parser.set_database_adapter(postgres);
    
    // Setup test data
    postgres.execute(r#"
        CREATE TABLE IF NOT EXISTS test_products (
            id SERIAL PRIMARY KEY,
            name VARCHAR(255),
            price DECIMAL(10,2)
        );
        
        INSERT INTO test_products (name, price) VALUES 
            ('Test Product 1', 10.99),
            ('Test Product 2', 20.99);
    "#).await.expect("Failed to setup test data");
    
    let tsk_content = r#"
[products]
count: @query("SELECT COUNT(*) FROM test_products")
total_value: @query("SELECT SUM(price) FROM test_products")
"#;
    
    let data = parser.parse(tsk_content).await.expect("Failed to parse");
    
    assert_eq!(data["products"]["count"], 2);
    assert_eq!(data["products"]["total_value"], 31.98);
    
    println!("✅ PostgreSQL integration test passed!");
}
```

## 🚀 Performance Optimization

### Connection Pooling

```rust
use tusklang_rust::adapters::postgresql::{PostgreSQLAdapter, PostgreSQLConfig, PoolConfig};
use std::time::Duration;

async fn setup_optimized_postgres() -> Result<PostgreSQLAdapter, Box<dyn std::error::Error>> {
    let postgres = PostgreSQLAdapter::with_pool(PostgreSQLConfig {
        host: "localhost".to_string(),
        port: 5432,
        database: "myapp".to_string(),
        user: "postgres".to_string(),
        password: "secret".to_string(),
        ssl_mode: "disable".to_string(),
    }, PoolConfig {
        max_open_conns: 50,
        max_idle_conns: 20,
        conn_max_lifetime: Duration::from_secs(300),
        conn_max_idle_time: Duration::from_secs(60),
    }).await?;
    
    Ok(postgres)
}
```

### Query Caching

```rust
use tusklang_rust::{parse, Parser};
use tusklang_rust::adapters::{sqlite::SQLiteAdapter, redis::RedisAdapter};

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    let mut parser = Parser::new();
    
    let sqlite = SQLiteAdapter::new("app.db").await?;
    let redis = RedisAdapter::new(RedisConfig {
        host: "localhost".to_string(),
        port: 6379,
        db: 0,
    }).await?;
    
    parser.set_database_adapter(sqlite);
    parser.set_cache_adapter(redis);
    
    let tsk_content = r#"
[expensive_queries]
# Cache expensive queries
user_stats: @cache("5m", @query("SELECT COUNT(*), AVG(age) FROM users"))
order_stats: @cache("10m", @query("SELECT COUNT(*), SUM(amount) FROM orders"))
"#;
    
    let data = parser.parse(tsk_content).await?;
    
    println!("User stats: {:?}", data["expensive_queries"]["user_stats"]);
    println!("Order stats: {:?}", data["expensive_queries"]["order_stats"]);
    
    Ok(())
}
```

## 🔒 Security Best Practices

### Parameterized Queries

```rust
use tusklang_rust::{parse, Parser};
use tusklang_rust::adapters::sqlite::SQLiteAdapter;

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    let mut parser = Parser::new();
    let sqlite = SQLiteAdapter::new("app.db").await?;
    parser.set_database_adapter(sqlite);
    
    // Setup test data
    sqlite.execute(r#"
        CREATE TABLE IF NOT EXISTS users (
            id INTEGER PRIMARY KEY,
            name TEXT,
            email TEXT
        );
        INSERT INTO users (name, email) VALUES 
            ('Alice', 'alice@example.com'),
            ('Bob', 'bob@example.com');
    "#).await?;
    
    let tsk_content = r#"
[secure_queries]
# Parameterized queries prevent SQL injection
user_by_email: @query("SELECT * FROM users WHERE email = ?", @request.email)
users_by_name: @query("SELECT * FROM users WHERE name LIKE ?", @request.search_term)
"#;
    
    // Execute with request context
    let mut context = std::collections::HashMap::new();
    let mut request = std::collections::HashMap::new();
    request.insert("email".to_string(), "alice@example.com".to_string());
    request.insert("search_term".to_string(), "%alice%".to_string());
    context.insert("request".to_string(), request);
    
    let data = parser.parse_with_context(tsk_content, &context).await?;
    
    println!("User by email: {:?}", data["secure_queries"]["user_by_email"]);
    println!("Users by name: {:?}", data["secure_queries"]["users_by_name"]);
    
    Ok(())
}
```

## 📊 Database Monitoring

### Health Checks

```rust
use tusklang_rust::{parse, Parser};
use tusklang_rust::adapters::postgresql::PostgreSQLAdapter;

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    let mut parser = Parser::new();
    let postgres = PostgreSQLAdapter::new(PostgreSQLConfig {
        host: "localhost".to_string(),
        port: 5432,
        database: "myapp".to_string(),
        user: "postgres".to_string(),
        password: "secret".to_string(),
        ssl_mode: "disable".to_string(),
    }).await?;
    
    parser.set_database_adapter(postgres);
    
    let tsk_content = r#"
[health_check]
connection: @query("SELECT 1")
version: @query("SELECT version()")
uptime: @query("SELECT EXTRACT(EPOCH FROM (NOW() - pg_postmaster_start_time()))")
active_connections: @query("SELECT count(*) FROM pg_stat_activity WHERE state = 'active'")
"#;
    
    let data = parser.parse(tsk_content).await?;
    
    println!("Database Health:");
    println!("  Connection: {}", data["health_check"]["connection"]);
    println!("  Version: {}", data["health_check"]["version"]);
    println!("  Uptime: {} seconds", data["health_check"]["uptime"]);
    println!("  Active connections: {}", data["health_check"]["active_connections"]);
    
    Ok(())
}
```

## 🎯 What You've Learned

1. **Multiple database support** - SQLite, PostgreSQL, MySQL, MongoDB, Redis
2. **Connection pooling** for optimal performance
3. **Cross-database operations** with multiple adapters
4. **Query caching** with Redis integration
5. **Security best practices** with parameterized queries
6. **Database monitoring** and health checks
7. **Testing strategies** for database integration

## 🚀 Next Steps

1. **Advanced Features**: Read `005-advanced-features-rust.md`
2. **Web Framework Integration**: See the examples in the quick start
3. **Performance Tuning**: Implement connection pooling and caching
4. **Security Hardening**: Use parameterized queries and encryption

---

**You now have complete database integration mastery with TuskLang Rust!** From simple SQLite to complex multi-database setups, TuskLang gives you the power to query databases directly in your configuration with zero-copy performance and type safety. 