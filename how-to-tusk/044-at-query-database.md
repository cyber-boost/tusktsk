# @ Query Database

The @query function and related database operators provide powerful database interaction capabilities in TuskLang. This guide covers executing queries, handling results, and building robust database applications.

## Basic Database Queries

### Simple Query Execution

```tusk
# Execute a basic query
users = @query("SELECT * FROM users WHERE active = true")

# Query with parameters (prevents SQL injection)
user_id: 123
user = @query("SELECT * FROM users WHERE id = ?", user_id)

# Multiple parameters
age_min: 18
age_max: 65
users = @query(
    "SELECT * FROM users WHERE age BETWEEN ? AND ?",
    age_min,
    age_max
)
```

### Query Result Handling

```tusk
# Query returns array of objects
result = @query("SELECT id, name, email FROM users")
# Result: [
#   { id: 1, name: "John", email: "john@example.com" },
#   { id: 2, name: "Jane", email: "jane@example.com" }
# ]

# Access results
first_user = result[0]
user_count = @len(result)

# Handle empty results
users = @query("SELECT * FROM users WHERE age > ?", 100)
@if(@len(users) == 0, {
    @print("No users found")
})
```

### Single Row Queries

```tusk
# Get single row
query_one = @lambda(sql, ...params, {
    results = @query(sql, ...params)
    return: results[0] ?? null
})

# Usage
user = @query_one("SELECT * FROM users WHERE id = ?", user_id)

@if(user, {
    @print("Found user: ${user.name}")
}, {
    @print("User not found")
})
```

## Database Connection Management

### Connection Configuration

```tusk
# Database configuration
db_config:
    driver: "postgresql"  # or "mysql", "sqlite", etc.
    host: @env.DB_HOST || "localhost"
    port: @env.DB_PORT || 5432
    database: @env.DB_NAME || "myapp"
    username: @env.DB_USER || "dbuser"
    password: @env.DB_PASSWORD
    
    # Connection pool settings
    pool:
        min: 2
        max: 10
        idle_timeout: 30000  # 30 seconds

# Initialize connection
@db.init(@db_config)
```

### Connection Patterns

```tusk
# Ensure connection before queries
with_connection = @lambda(callback, {
    # Check connection
    @if(!@db.connected, {
        @db.connect(@db_config)
    })
    
    try_result = @try({
        return: @callback()
    }, {
        error: @catch
        @log.error("Database error: ${error}")
        
        # Attempt reconnection on connection errors
        @if(@includes(error.message, "connection"), {
            @db.reconnect()
            return: @callback()  # Retry once
        })
        
        @throw(error)
    })
    
    return: try_result
})
```

## Transaction Management

### Basic Transactions

```tusk
# Execute queries in a transaction
transfer_funds = @lambda(from_id, to_id, amount, {
    @db.transaction(@lambda({
        # Deduct from sender
        @query(
            "UPDATE accounts SET balance = balance - ? WHERE id = ? AND balance >= ?",
            amount, from_id, amount
        )
        
        # Check if update succeeded
        @if(@db.affected_rows == 0, {
            @throw("Insufficient funds")
        })
        
        # Add to receiver
        @query(
            "UPDATE accounts SET balance = balance + ? WHERE id = ?",
            amount, to_id
        )
        
        # Log transaction
        @query(
            "INSERT INTO transactions (from_id, to_id, amount, created_at) VALUES (?, ?, ?, ?)",
            from_id, to_id, amount, @time.now()
        )
        
        return: true
    }))
})
```

### Transaction with Savepoints

```tusk
# Nested transactions with savepoints
complex_operation = @lambda({
    @db.transaction(@lambda({
        # Main transaction
        @query("INSERT INTO orders (user_id, total) VALUES (?, ?)", user_id, total)
        order_id = @db.last_insert_id
        
        # Savepoint for items
        @db.savepoint("items", @lambda({
            @each(items, @lambda(item, {
                result = @try({
                    @query(
                        "INSERT INTO order_items (order_id, product_id, quantity) VALUES (?, ?, ?)",
                        order_id, item.product_id, item.quantity
                    )
                }, {
                    # Rollback to savepoint on error
                    @db.rollback_to("items")
                    @throw("Failed to add item: ${item.product_id}")
                })
            }))
        }))
        
        return: order_id
    }))
})
```

## Query Builders

### Dynamic Query Construction

```tusk
# Build queries dynamically
build_select_query = @lambda(table, options = {}, {
    parts: ["SELECT"]
    
    # SELECT clause
    fields = options.fields ?? ["*"]
    @push(parts, @join(fields, ", "))
    
    # FROM clause
    @push(parts, "FROM ${table}")
    
    # WHERE clause
    @if(options.where, {
        conditions = []
        values = []
        
        @each(@entries(options.where), @lambda(entry, {
            [field, value] = entry
            
            @if(@isArray(value), {
                # IN clause
                placeholders = @join(@repeat("?", @len(value)), ", ")
                @push(conditions, "${field} IN (${placeholders})")
                values = @concat(values, value)
            }, {
                # Equal comparison
                @push(conditions, "${field} = ?")
                @push(values, value)
            })
        }))
        
        @if(@len(conditions) > 0, {
            @push(parts, "WHERE ${@join(conditions, ' AND ')}")
        })
    })
    
    # ORDER BY clause
    @if(options.order_by, {
        @push(parts, "ORDER BY ${options.order_by}")
    })
    
    # LIMIT clause
    @if(options.limit, {
        @push(parts, "LIMIT ${options.limit}")
        
        @if(options.offset, {
            @push(parts, "OFFSET ${options.offset}")
        })
    })
    
    return: {
        sql: @join(parts, " ")
        params: values
    }
})

# Usage
query_info = @build_select_query("users", {
    fields: ["id", "name", "email"]
    where: { active: true, role: ["admin", "moderator"] }
    order_by: "created_at DESC"
    limit: 10
})

results = @query(query_info.sql, ...query_info.params)
```

### INSERT Query Builder

```tusk
# Build INSERT queries
build_insert_query = @lambda(table, data, {
    fields = @keys(data)
    values = @values(data)
    
    placeholders = @join(@repeat("?", @len(fields)), ", ")
    field_list = @join(fields, ", ")
    
    sql = "INSERT INTO ${table} (${field_list}) VALUES (${placeholders})"
    
    return: { sql: sql, params: values }
})

# Bulk insert
bulk_insert = @lambda(table, records, {
    @if(@len(records) == 0, return: 0)
    
    fields = @keys(records[0])
    field_list = @join(fields, ", ")
    
    # Build value placeholders
    value_template = "(${@join(@repeat('?', @len(fields)), ', ')})"
    value_clauses = @join(@repeat(value_template, @len(records)), ", ")
    
    # Flatten parameters
    params = []
    @each(records, @lambda(record, {
        @each(fields, @lambda(field, {
            @push(params, record[field])
        }))
    }))
    
    sql = "INSERT INTO ${table} (${field_list}) VALUES ${value_clauses}"
    
    @query(sql, ...params)
    return: @db.affected_rows
})
```

## Prepared Statements

### Using Prepared Statements

```tusk
# Prepare statement for reuse
prepare_user_query = @lambda({
    stmt = @db.prepare("SELECT * FROM users WHERE email = ? AND active = ?")
    
    return: @lambda(email, active = true, {
        return: @stmt.execute(email, active)
    })
})

# Use prepared statement multiple times
get_user = @prepare_user_query()
user1 = @get_user("john@example.com")
user2 = @get_user("jane@example.com", false)
```

### Cached Prepared Statements

```tusk
# Cache prepared statements
statement_cache: {}

execute_prepared = @lambda(key, sql, params, {
    # Get or create prepared statement
    stmt = @statement_cache[key] ?? @db.prepare(sql)
    @statement_cache[key] = stmt
    
    # Execute with parameters
    return: @stmt.execute(...params)
})

# Usage
user = @execute_prepared(
    "get_user_by_id",
    "SELECT * FROM users WHERE id = ?",
    [user_id]
)[0]
```

## Result Processing

### Mapping Results

```tusk
# Transform query results
get_users_with_age = @lambda({
    results = @query("SELECT * FROM users")
    
    return: @map(results, @lambda(user, {
        # Calculate age from birthdate
        age = @date.years_between(user.birthdate, @date.now())
        
        return: {
            ...user
            age: age
            is_adult: age >= 18
        }
    }))
})

# Group results
group_users_by_role = @lambda({
    users = @query("SELECT * FROM users")
    
    return: @group_by(users, @lambda(user, user.role))
})
```

### Pagination

```tusk
# Paginated query helper
paginate = @lambda(query, page = 1, per_page = 20, {
    offset = (page - 1) * per_page
    
    # Get total count
    count_query = "SELECT COUNT(*) as total FROM (${query}) as subquery"
    total = @query(count_query)[0].total
    
    # Get paginated results
    paginated_query = "${query} LIMIT ? OFFSET ?"
    results = @query(paginated_query, per_page, offset)
    
    return: {
        data: results
        pagination: {
            current_page: page
            per_page: per_page
            total: total
            total_pages: @ceil(total / per_page)
            has_next: page < @ceil(total / per_page)
            has_prev: page > 1
        }
    }
})

# Usage
page_results = @paginate(
    "SELECT * FROM products WHERE category = 'electronics' ORDER BY price",
    @request.query.page ?? 1
)
```

## Database Migrations

### Migration System

```tusk
# Simple migration system
run_migrations = @lambda({
    # Create migrations table if not exists
    @query("""
        CREATE TABLE IF NOT EXISTS migrations (
            id INTEGER PRIMARY KEY,
            filename VARCHAR(255) UNIQUE,
            executed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        )
    """)
    
    # Get list of migration files
    migration_files = @file.list("./migrations/*.sql")
    
    # Get executed migrations
    executed = @map(
        @query("SELECT filename FROM migrations"),
        @lambda(row, row.filename)
    )
    
    # Run pending migrations
    @each(migration_files, @lambda(file, {
        filename = @path.basename(file)
        
        @if(!@includes(executed, filename), {
            @log.info("Running migration: ${filename}")
            
            @db.transaction(@lambda({
                # Execute migration
                sql = @file.read(file)
                @query(sql)
                
                # Record migration
                @query(
                    "INSERT INTO migrations (filename) VALUES (?)",
                    filename
                )
            }))
        })
    }))
})
```

## Error Handling

### Comprehensive Error Handling

```tusk
# Database operation with full error handling
safe_query = @lambda(sql, params = [], options = {}, {
    max_retries = options.retries ?? 3
    retry_delay = options.retry_delay ?? 1000
    
    attempt = @lambda(retry_count, {
        return: @try({
            result: @query(sql, ...params)
            return: { success: true, data: result }
        }, {
            error: @catch
            
            # Check error type
            @if(@includes(error.message, "deadlock") && retry_count < max_retries, {
                @log.warn("Deadlock detected, retrying...")
                @time.sleep(retry_delay * retry_count)
                return: @attempt(retry_count + 1)
            })
            
            @if(@includes(error.message, "connection"), {
                @log.error("Connection error: ${error.message}")
                # Attempt reconnection
                @db.reconnect()
                
                @if(retry_count < max_retries, {
                    return: @attempt(retry_count + 1)
                })
            })
            
            return: {
                success: false
                error: error.message
                code: error.code
            }
        })
    })
    
    return: @attempt(0)
})
```

## Performance Optimization

### Query Optimization

```tusk
# Add query analysis
analyze_query = @lambda(sql, params = [], {
    # Explain query
    explain = @query("EXPLAIN ${sql}", ...params)
    
    # Check for performance issues
    issues = []
    
    @each(explain, @lambda(row, {
        @if(@includes(row.Extra ?? "", "Using filesort"), {
            @push(issues, "Query uses filesort - consider adding index")
        })
        
        @if(row.type == "ALL", {
            @push(issues, "Full table scan detected")
        })
    }))
    
    return: {
        plan: explain
        issues: issues
        recommendations: @generate_index_recommendations(sql)
    }
})
```

### Connection Pooling

```tusk
# Connection pool management
connection_pool:
    connections: []
    max_size: 10
    
    acquire: @lambda({
        # Find available connection
        conn = @find(@connections, @lambda(c, !c.in_use))
        
        @if(conn, {
            conn.in_use = true
            return: conn
        })
        
        # Create new if under limit
        @if(@len(@connections) < @max_size, {
            conn = @db.create_connection()
            @push(@connections, {
                connection: conn
                in_use: true
                created_at: @time.now()
            })
            return: conn
        })
        
        # Wait for available connection
        @wait_for_connection()
    })
    
    release: @lambda(conn, {
        conn.in_use = false
        conn.last_used = @time.now()
    })
```

## Best Practices

1. **Always use parameterized queries** to prevent SQL injection
2. **Handle connection errors** gracefully with retries
3. **Use transactions** for data consistency
4. **Implement connection pooling** for performance
5. **Add appropriate indexes** based on query patterns
6. **Monitor query performance** in production
7. **Use prepared statements** for repeated queries
8. **Implement proper error handling** and logging

## Common Patterns

### Repository Pattern

```tusk
# Create a repository for data access
create_repository = @lambda(table_name, {
    return: {
        find_by_id: @lambda(id, {
            @query_one("SELECT * FROM ${table_name} WHERE id = ?", id)
        })
        
        find_all: @lambda(conditions = {}, {
            query_info = @build_select_query(table_name, { where: conditions })
            @query(query_info.sql, ...query_info.params)
        })
        
        create: @lambda(data, {
            query_info = @build_insert_query(table_name, data)
            @query(query_info.sql, ...query_info.params)
            
            return: {
                ...data
                id: @db.last_insert_id
            }
        })
        
        update: @lambda(id, data, {
            fields = @keys(data)
            set_clause = @join(@map(fields, @lambda(f, "${f} = ?")), ", ")
            values = @concat(@values(data), [id])
            
            @query(
                "UPDATE ${table_name} SET ${set_clause} WHERE id = ?",
                ...values
            )
            
            return: @db.affected_rows > 0
        })
        
        delete: @lambda(id, {
            @query("DELETE FROM ${table_name} WHERE id = ?", id)
            return: @db.affected_rows > 0
        })
    }
})

# Usage
user_repo = @create_repository("users")
user = @user_repo.find_by_id(123)
new_user = @user_repo.create({ name: "John", email: "john@example.com" })
```

## Next Steps

- Explore [Cache Functions](046-at-cache-function.md)
- Learn about [Transaction Patterns](044-at-query-database.md)
- Master [Performance Optimization](059-at-operator-performance.md)