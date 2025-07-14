# query() - Database Query Function

The `query()` function provides a powerful and secure way to execute database queries in TuskLang with built-in protection against SQL injection.

## Basic Syntax

```tusk
# Simple query
users: query("SELECT * FROM users")

# Query with parameters (safe from SQL injection)
user: query("SELECT * FROM users WHERE id = ?", [user_id])

# Multiple parameters
results: query(
    "SELECT * FROM orders WHERE user_id = ? AND status = ?", 
    [user_id, "completed"]
)
```

## SELECT Queries

```tusk
# Basic SELECT
all_users: query("SELECT * FROM users")

# SELECT with conditions
active_users: query("SELECT * FROM users WHERE active = ?", [true])

# Complex SELECT
orders: query("
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
", [last_week])

# Single row
user: query("SELECT * FROM users WHERE email = ?", [email]).first()

# Single value
count: query("SELECT COUNT(*) as total FROM users").value("total")
```

## INSERT Queries

```tusk
# Basic INSERT
query("
    INSERT INTO users (name, email, created_at) 
    VALUES (?, ?, ?)
", [name, email, now()])

# Get insert ID
user_id: query("
    INSERT INTO users (name, email) 
    VALUES (?, ?)
", [name, email]).insert_id()

# Insert multiple rows
users_data: [
    ["John", "john@example.com"],
    ["Jane", "jane@example.com"],
    ["Bob", "bob@example.com"]
]

foreach (users_data as user) {
    query("INSERT INTO users (name, email) VALUES (?, ?)", user)
}

# Insert with ON DUPLICATE KEY UPDATE
query("
    INSERT INTO settings (user_id, key, value) 
    VALUES (?, ?, ?)
    ON DUPLICATE KEY UPDATE value = VALUES(value)
", [user_id, setting_key, setting_value])
```

## UPDATE Queries

```tusk
# Basic UPDATE
affected: query("
    UPDATE users 
    SET last_login = ? 
    WHERE id = ?
", [now(), user_id]).affected_rows()

# Update multiple fields
query("
    UPDATE products 
    SET 
        name = ?,
        price = ?,
        updated_at = ?
    WHERE id = ?
", [name, price, now(), product_id])

# Conditional update
query("
    UPDATE orders 
    SET status = ?
    WHERE id = ? AND status = ?
", ["shipped", order_id, "pending"])

# Increment value
query("
    UPDATE users 
    SET login_count = login_count + 1 
    WHERE id = ?
", [user_id])
```

## DELETE Queries

```tusk
# Basic DELETE
deleted: query("DELETE FROM users WHERE id = ?", [user_id]).affected_rows()

# Delete with condition
query("DELETE FROM sessions WHERE expires_at < ?", [now()])

# Soft delete
query("UPDATE users SET deleted_at = ? WHERE id = ?", [now(), user_id])

# Delete with JOIN
query("
    DELETE orders 
    FROM orders 
    JOIN users ON orders.user_id = users.id 
    WHERE users.inactive = 1
")
```

## Transactions

```tusk
# Basic transaction
transaction(() => {
    # Deduct from account
    query("UPDATE accounts SET balance = balance - ? WHERE id = ?", [amount, from_id])
    
    # Add to account
    query("UPDATE accounts SET balance = balance + ? WHERE id = ?", [amount, to_id])
    
    # Log transaction
    query("INSERT INTO transactions (from_id, to_id, amount) VALUES (?, ?, ?)", 
          [from_id, to_id, amount])
})

# Manual transaction control
begin_transaction()

try {
    # Multiple queries
    query("INSERT INTO orders (user_id, total) VALUES (?, ?)", [user_id, total])
    order_id: last_insert_id()
    
    foreach (items as item) {
        query("INSERT INTO order_items (order_id, product_id, quantity) VALUES (?, ?, ?)",
              [order_id, item.product_id, item.quantity])
    }
    
    commit()
} catch (e) {
    rollback()
    throw e
}
```

## Prepared Statements

```tusk
# Prepare once, execute many
stmt: prepare("INSERT INTO logs (level, message, created_at) VALUES (?, ?, ?)")

foreach (log_entries as entry) {
    stmt.execute([entry.level, entry.message, now()])
}

stmt.close()

# Named parameters
stmt: prepare("
    SELECT * FROM users 
    WHERE age BETWEEN :min_age AND :max_age 
    AND status = :status
")

results: stmt.execute({
    min_age: 18,
    max_age: 65,
    status: "active"
})
```

## Query Builder Integration

```tusk
# Build query dynamically
conditions: []
params: []

if (search_name) {
    conditions[] = "name LIKE ?"
    params[] = "%" + search_name + "%"
}

if (min_price) {
    conditions[] = "price >= ?"
    params[] = min_price
}

if (category_id) {
    conditions[] = "category_id = ?"
    params[] = category_id
}

sql: "SELECT * FROM products"
if (count(conditions) > 0) {
    sql += " WHERE " + join(" AND ", conditions)
}

results: query(sql, params)
```

## Result Processing

```tusk
# Get all results
users: query("SELECT * FROM users").all()

# Get first result
first_user: query("SELECT * FROM users ORDER BY created_at").first()

# Get single column
emails: query("SELECT email FROM users").column("email")

# Get key-value pairs
user_names: query("SELECT id, name FROM users").pairs("id", "name")

# Custom result processing
query("SELECT * FROM orders").each((row) => {
    # Process each row
    process_order(row)
})

# Map results
totals: query("SELECT * FROM orders").map((order) => order.total)
```

## Advanced Features

```tusk
# Query with timeout
results: query("SELECT * FROM large_table", [], {
    timeout: 5000  # 5 seconds
})

# Read from replica
users: query("SELECT * FROM users", [], {
    connection: "read_replica"
})

# Query profiling
profile: query("SELECT * FROM complex_view", [], {
    profile: true
})
# profile.time, profile.rows, profile.explain

# Streaming results
query("SELECT * FROM huge_table", [], {
    stream: true,
    chunk_size: 1000
}).each_chunk((chunk) => {
    # Process chunk of 1000 rows
    process_chunk(chunk)
})
```

## Error Handling

```tusk
# Basic error handling
try {
    result: query("SELECT * FROM users WHERE id = ?", [user_id])
} catch (QueryException e) {
    log.error("Query failed", {
        error: e.message,
        sql: e.sql,
        params: e.params
    })
    return null
}

# Check query success
result: query("UPDATE users SET active = ? WHERE id = ?", [false, user_id])

if (result.success && result.affected_rows() > 0) {
    log.info("User deactivated", {user_id: user_id})
} else {
    log.warning("No user found to deactivate", {user_id: user_id})
}

# Handle deadlocks
retry_on_deadlock: (sql, params, max_retries: 3) => {
    attempts: 0
    
    while (attempts < max_retries) {
        try {
            return query(sql, params)
        } catch (e) {
            if (e.code == 1213 && attempts < max_retries - 1) {  # Deadlock
                attempts++
                sleep(0.1 * attempts)  # Exponential backoff
            } else {
                throw e
            }
        }
    }
}
```

## Query Caching

```tusk
# Cache query results
cached_query: (sql, params, ttl: 3600) => {
    cache_key: "query:" + md5(sql + json_encode(params))
    
    cached: cache.get(cache_key)
    if (cached !== null) {
        return cached
    }
    
    result: query(sql, params)
    cache.set(cache_key, result, ttl)
    
    return result
}

# Invalidate cache on update
update_user: (id, data) => {
    query("UPDATE users SET ? WHERE id = ?", [data, id])
    
    # Clear related caches
    cache.delete("query:" + md5("SELECT * FROM users WHERE id = ?" + json_encode([id])))
    cache.tags(["users"]).flush()
}
```

## Database Agnostic

```tusk
# Works with different databases
# MySQL
users: query("SELECT * FROM users LIMIT ?", [10])

# PostgreSQL  
users: query("SELECT * FROM users LIMIT $1", [10])

# SQLite
users: query("SELECT * FROM users LIMIT ?", [10])

# SQL Server
users: query("SELECT TOP (?) * FROM users", [10])

# Use database abstraction
users: query.select("users").limit(10).get()
```

## Performance Tips

```tusk
# Use EXPLAIN to analyze queries
explain: query("EXPLAIN SELECT * FROM orders WHERE user_id = ?", [user_id])

# Batch operations
query.batch((batch) => {
    foreach (items as item) {
        batch.add("INSERT INTO items (name, price) VALUES (?, ?)", 
                  [item.name, item.price])
    }
}).execute()

# Use indexes effectively
# Create index
query("CREATE INDEX idx_user_email ON users(email)")

# Query will use index
user: query("SELECT * FROM users WHERE email = ?", [email])
```

## Best Practices

1. **Always use parameterized queries** - Prevent SQL injection
2. **Use transactions for related operations** - Maintain data integrity
3. **Handle errors gracefully** - Queries can fail
4. **Cache when appropriate** - Reduce database load
5. **Profile slow queries** - Identify bottlenecks
6. **Use indexes effectively** - Speed up queries
7. **Limit result sets** - Don't fetch more than needed
8. **Close connections** - Free resources

## Related Functions

- `prepare()` - Prepared statements
- `transaction()` - Transaction management
- `db()` - Database connection
- `schema()` - Schema builder
- `migrate()` - Database migrations