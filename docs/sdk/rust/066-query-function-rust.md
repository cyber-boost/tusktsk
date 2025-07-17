# 🦀 query() - Database Query Function in Rust

**"We don't bow to any king" - Database Edition**

The `query()` function in TuskLang Rust provides ultra-fast, zero-copy database operations with compile-time safety and built-in protection against SQL injection. Say goodbye to ORM complexity and hello to raw power with type safety.

## 🚀 Basic Syntax

```rust
use tusklang_rust::{query, QueryBuilder, DatabaseConnection};

// Simple query with zero-copy parsing
let users: Vec<User> = query!("SELECT * FROM users")?;

// Parameterized query (SQL injection protection)
let user: Option<User> = query!(
    "SELECT * FROM users WHERE id = ?", 
    user_id
).first()?;

// Multiple parameters with type safety
let results: Vec<Order> = query!(
    "SELECT * FROM orders WHERE user_id = ? AND status = ?", 
    user_id, 
    "completed"
)?;
```

## 🎯 SELECT Queries

```rust
use tusklang_rust::{query, QueryResult};

// Basic SELECT with automatic deserialization
#[derive(Debug, Deserialize)]
struct User {
    id: i32,
    name: String,
    email: String,
    active: bool,
}

let all_users: Vec<User> = query!("SELECT * FROM users")?;

// SELECT with conditions
let active_users: Vec<User> = query!(
    "SELECT * FROM users WHERE active = ?", 
    true
)?;

// Complex SELECT with joins
#[derive(Debug, Deserialize)]
struct OrderWithDetails {
    id: i32,
    total: f64,
    customer_name: String,
    product_name: String,
}

let orders: Vec<OrderWithDetails> = query!(r#"
    SELECT 
        o.id,
        o.total,
        u.name as customer_name,
        p.name as product_name
    FROM orders o
    JOIN users u ON o.user_id = u.id
    JOIN products p ON o.product_id = p.id
    WHERE o.created_at > ?
    ORDER BY o.created_at DESC
    LIMIT 100
"#, last_week)?;

// Single row with Option
let user: Option<User> = query!(
    "SELECT * FROM users WHERE email = ?", 
    email
).first()?;

// Single value extraction
let count: i64 = query!(
    "SELECT COUNT(*) as total FROM users"
).value("total")?;
```

## ⚡ INSERT Queries

```rust
use tusklang_rust::{query, InsertResult};

// Basic INSERT with automatic parameter binding
let insert_result: InsertResult = query!(
    "INSERT INTO users (name, email, created_at) VALUES (?, ?, ?)",
    name, 
    email, 
    chrono::Utc::now()
)?;

// Get insert ID
let user_id: i64 = query!(
    "INSERT INTO users (name, email) VALUES (?, ?)",
    name, 
    email
).insert_id()?;

// Batch insert with iterator
let users_data = vec![
    ("John", "john@example.com"),
    ("Jane", "jane@example.com"),
    ("Bob", "bob@example.com"),
];

for (name, email) in users_data {
    query!(
        "INSERT INTO users (name, email) VALUES (?, ?)",
        name, 
        email
    )?;
}

// Insert with ON DUPLICATE KEY UPDATE
query!(
    "INSERT INTO settings (user_id, key, value) VALUES (?, ?, ?) 
     ON DUPLICATE KEY UPDATE value = VALUES(value)",
    user_id, 
    setting_key, 
    setting_value
)?;
```

## 🔄 UPDATE Queries

```rust
use tusklang_rust::{query, UpdateResult};

// Basic UPDATE with affected rows
let update_result: UpdateResult = query!(
    "UPDATE users SET last_login = ? WHERE id = ?",
    chrono::Utc::now(), 
    user_id
)?;

println!("Affected rows: {}", update_result.affected_rows());

// Update multiple fields
query!(
    "UPDATE products SET name = ?, price = ?, updated_at = ? WHERE id = ?",
    name, 
    price, 
    chrono::Utc::now(), 
    product_id
)?;

// Conditional update
query!(
    "UPDATE orders SET status = ? WHERE id = ? AND status = ?",
    "shipped", 
    order_id, 
    "pending"
)?;

// Increment value
query!(
    "UPDATE users SET login_count = login_count + 1 WHERE id = ?",
    user_id
)?;
```

## 🗑️ DELETE Queries

```rust
use tusklang_rust::{query, DeleteResult};

// Basic DELETE with affected rows
let delete_result: DeleteResult = query!(
    "DELETE FROM users WHERE id = ?", 
    user_id
)?;

println!("Deleted rows: {}", delete_result.affected_rows());

// Delete with condition
query!(
    "DELETE FROM sessions WHERE expires_at < ?",
    chrono::Utc::now()
)?;

// Soft delete
query!(
    "UPDATE users SET deleted_at = ? WHERE id = ?",
    chrono::Utc::now(), 
    user_id
)?;

// Delete with JOIN
query!(r#"
    DELETE orders 
    FROM orders 
    JOIN users ON orders.user_id = users.id 
    WHERE users.inactive = 1
"#)?;
```

## 🔒 Transactions

```rust
use tusklang_rust::{transaction, Transaction};

// Basic transaction with automatic rollback
let result: Result<(), Box<dyn std::error::Error>> = transaction(|tx| {
    // Deduct from account
    query!(
        "UPDATE accounts SET balance = balance - ? WHERE id = ?",
        amount, 
        from_id
    ).execute_on(tx)?;
    
    // Add to account
    query!(
        "UPDATE accounts SET balance = balance + ? WHERE id = ?",
        amount, 
        to_id
    ).execute_on(tx)?;
    
    // Log transaction
    query!(
        "INSERT INTO transactions (from_id, to_id, amount) VALUES (?, ?, ?)",
        from_id, 
        to_id, 
        amount
    ).execute_on(tx)?;
    
    Ok(())
});

// Manual transaction control
let mut tx = db.begin_transaction()?;

match (|| -> Result<(), Box<dyn std::error::Error>> {
    // Multiple queries
    query!(
        "INSERT INTO orders (user_id, total) VALUES (?, ?)",
        user_id, 
        total
    ).execute_on(&mut tx)?;
    
    let order_id = tx.last_insert_id()?;
    
    for item in items {
        query!(
            "INSERT INTO order_items (order_id, product_id, quantity) VALUES (?, ?, ?)",
            order_id, 
            item.product_id, 
            item.quantity
        ).execute_on(&mut tx)?;
    }
    
    tx.commit()?;
    Ok(())
})() {
    Ok(_) => println!("Transaction successful"),
    Err(e) => {
        tx.rollback()?;
        return Err(e);
    }
}
```

## 🎯 Prepared Statements

```rust
use tusklang_rust::{prepare, PreparedStatement};

// Prepare once, execute many
let mut stmt = prepare!(
    "INSERT INTO logs (level, message, created_at) VALUES (?, ?, ?)"
)?;

for entry in log_entries {
    stmt.execute(&[&entry.level, &entry.message, &chrono::Utc::now()])?;
}

stmt.close()?;

// Named parameters with struct
#[derive(Debug)]
struct UserQuery {
    min_age: i32,
    max_age: i32,
    status: String,
}

let mut stmt = prepare!(
    "SELECT * FROM users WHERE age BETWEEN :min_age AND :max_age AND status = :status"
)?;

let query_params = UserQuery {
    min_age: 18,
    max_age: 65,
    status: "active".to_string(),
};

let results: Vec<User> = stmt.execute_named(&query_params)?;
```

## 🔧 Query Builder Integration

```rust
use tusklang_rust::{QueryBuilder, Condition};

// Build query dynamically with type safety
let mut builder = QueryBuilder::new("SELECT * FROM products");
let mut conditions = Vec::new();
let mut params = Vec::new();

if let Some(search_name) = search_name {
    conditions.push("name LIKE ?");
    params.push(format!("%{}%", search_name));
}

if let Some(min_price) = min_price {
    conditions.push("price >= ?");
    params.push(min_price);
}

if let Some(category_id) = category_id {
    conditions.push("category_id = ?");
    params.push(category_id);
}

if !conditions.is_empty() {
    builder = builder.where_clause(&conditions.join(" AND "));
}

let results: Vec<Product> = builder.execute(&params)?;
```

## 📊 Result Processing

```rust
use tusklang_rust::{query, QueryResult};

// Get all results with automatic deserialization
let users: Vec<User> = query!("SELECT * FROM users").all()?;

// Get first result
let first_user: Option<User> = query!(
    "SELECT * FROM users ORDER BY created_at"
).first()?;

// Get single column
let emails: Vec<String> = query!(
    "SELECT email FROM users"
).column("email")?;

// Get key-value pairs
let user_names: HashMap<i32, String> = query!(
    "SELECT id, name FROM users"
).pairs("id", "name")?;

// Custom result processing with iterator
query!("SELECT * FROM orders").each(|row: Order| {
    process_order(row);
})?;

// Map results with functional programming
let totals: Vec<f64> = query!("SELECT * FROM orders")
    .map(|order: Order| order.total)?;
```

## ⚡ Advanced Features

```rust
use tusklang_rust::{query, QueryOptions};

// Query with timeout
let results: Vec<User> = query!(
    "SELECT * FROM large_table"
).with_options(QueryOptions {
    timeout: Some(Duration::from_secs(5)),
    ..Default::default()
})?;

// Read from replica
let users: Vec<User> = query!(
    "SELECT * FROM users"
).with_options(QueryOptions {
    connection: Some("read_replica".to_string()),
    ..Default::default()
})?;

// Query profiling
let profile_result = query!(
    "SELECT * FROM complex_view"
).with_options(QueryOptions {
    profile: true,
    ..Default::default()
})?;

println!("Query time: {:?}", profile_result.profile.time);
println!("Rows returned: {}", profile_result.profile.rows);

// Streaming results for large datasets
query!("SELECT * FROM huge_table")
    .with_options(QueryOptions {
        stream: true,
        chunk_size: Some(1000),
        ..Default::default()
    })
    .stream_chunks(|chunk: Vec<User>| {
        process_chunk(chunk);
    })?;
```

## 🛡️ Error Handling

```rust
use tusklang_rust::{query, QueryError, DatabaseError};

// Basic error handling with custom types
#[derive(Debug, thiserror::Error)]
enum AppError {
    #[error("Database query failed: {0}")]
    QueryError(#[from] QueryError),
    #[error("User not found: {0}")]
    UserNotFound(i32),
}

let result: Result<Option<User>, AppError> = (|| {
    let user = query!(
        "SELECT * FROM users WHERE id = ?", 
        user_id
    ).first()?;
    
    Ok(user)
})();

match result {
    Ok(Some(user)) => println!("Found user: {:?}", user),
    Ok(None) => Err(AppError::UserNotFound(user_id))?,
    Err(e) => {
        log::error!("Query failed: {:?}", e);
        return Err(e);
    }
}

// Check query success with result types
let update_result = query!(
    "UPDATE users SET active = ? WHERE id = ?",
    false, 
    user_id
)?;

if update_result.affected_rows() > 0 {
    log::info!("User deactivated: {}", user_id);
} else {
    log::warn!("No user found to deactivate: {}", user_id);
}

// Handle deadlocks with retry logic
fn retry_on_deadlock<F, T>(mut f: F, max_retries: u32) -> Result<T, Box<dyn std::error::Error>>
where
    F: FnMut() -> Result<T, QueryError>,
{
    let mut attempts = 0;
    
    loop {
        match f() {
            Ok(result) => return Ok(result),
            Err(e) => {
                if e.is_deadlock() && attempts < max_retries {
                    attempts += 1;
                    std::thread::sleep(Duration::from_millis(100 * attempts));
                } else {
                    return Err(e.into());
                }
            }
        }
    }
}
```

## 🚀 Query Caching

```rust
use tusklang_rust::{query, cache::QueryCache};
use std::collections::HashMap;

// Cache query results with TTL
struct CachedQuery {
    cache: QueryCache,
}

impl CachedQuery {
    fn cached_query<T>(
        &self,
        sql: &str,
        params: &[&dyn std::any::Any],
        ttl: Duration,
    ) -> Result<T, Box<dyn std::error::Error>>
    where
        T: for<'de> Deserialize<'de> + Clone + Send + Sync + 'static,
    {
        let cache_key = format!("query:{}", md5::compute(sql));
        
        if let Some(cached) = self.cache.get::<T>(&cache_key)? {
            return Ok(cached);
        }
        
        let result: T = query!(sql, params)?;
        self.cache.set(&cache_key, &result, ttl)?;
        
        Ok(result)
    }
    
    fn update_user(&self, id: i32, data: UserUpdate) -> Result<(), Box<dyn std::error::Error>> {
        query!(
            "UPDATE users SET ? WHERE id = ?",
            data, 
            id
        )?;
        
        // Clear related caches
        let cache_key = format!("query:{}", md5::compute("SELECT * FROM users WHERE id = ?"));
        self.cache.delete(&cache_key)?;
        self.cache.flush_tag("users")?;
        
        Ok(())
    }
}
```

## 🌐 Database Agnostic

```rust
use tusklang_rust::{query, DatabaseType};

// Works with different databases automatically
// MySQL
let users: Vec<User> = query!("SELECT * FROM users LIMIT ?", 10)?;

// PostgreSQL  
let users: Vec<User> = query!("SELECT * FROM users LIMIT $1", 10)?;

// SQLite
let users: Vec<User> = query!("SELECT * FROM users LIMIT ?", 10)?;

// SQL Server
let users: Vec<User> = query!("SELECT TOP (?) * FROM users", 10)?;

// Use database abstraction with type safety
let users: Vec<User> = QueryBuilder::new("users")
    .select("*")
    .limit(10)
    .execute()?;
```

## ⚡ Performance Tips

```rust
use tusklang_rust::{query, ExplainResult};

// Use EXPLAIN to analyze queries
let explain: ExplainResult = query!(
    "EXPLAIN SELECT * FROM orders WHERE user_id = ?",
    user_id
)?;

println!("Query plan: {:?}", explain.plan);

// Batch operations with connection pooling
let pool = ConnectionPool::new(10)?;

pool.batch(|conn| {
    for item in items {
        query!(
            "INSERT INTO items (name, price) VALUES (?, ?)",
            item.name, 
            item.price
        ).execute_on(conn)?;
    }
    Ok(())
})?;

// Use indexes effectively
// Create index
query!("CREATE INDEX idx_user_email ON users(email)")?;

// Query will use index automatically
let user: Option<User> = query!(
    "SELECT * FROM users WHERE email = ?",
    email
).first()?;
```

## 🎯 Best Practices

1. **Always use parameterized queries** - Zero SQL injection risk with compile-time safety
2. **Use transactions for related operations** - ACID compliance with automatic rollback
3. **Handle errors with proper types** - Leverage Rust's Result type for safety
4. **Cache when appropriate** - Built-in caching with TTL and tag invalidation
5. **Profile slow queries** - Automatic query profiling and analysis
6. **Use indexes effectively** - Query optimizer automatically selects best indexes
7. **Limit result sets** - Streaming support for large datasets
8. **Use connection pooling** - Automatic connection management and reuse

## 🔗 Related Functions

- `prepare!()` - Prepared statements with compile-time safety
- `transaction!()` - Transaction management with automatic rollback
- `db!()` - Database connection with connection pooling
- `schema!()` - Schema builder with migrations
- `migrate!()` - Database migrations with versioning

## 🚀 Performance Benchmarks

```rust
use tusklang_rust::{query, benchmark};

// Benchmark query performance
let benchmark_result = benchmark!(
    "SELECT * FROM users WHERE email = ?",
    &["test@example.com"]
)?;

println!("Average time: {:?}", benchmark_result.average_time);
println!("Throughput: {} queries/sec", benchmark_result.throughput);
```

**TuskLang Rust: Where zero-copy meets zero-compromise. Your database operations will never be the same.** 