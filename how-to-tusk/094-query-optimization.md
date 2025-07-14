# Query Optimization in TuskLang

Optimizing database queries is crucial for application performance. TuskLang provides tools and techniques to identify and resolve common performance issues.

## Understanding Query Performance

```tusk
# Enable query logging
@db.enableQueryLog()

# Run your queries
users: @User.with("posts").get()

# Get executed queries
queries: @db.getQueryLog()

for (query in queries) {
    @log.info("Query", {
        sql: query.sql,
        bindings: query.bindings,
        time: query.time + "ms"
    })
}

# Query profiling
@db.listen((query) => {
    if (query.time > 100) {  # Log slow queries (>100ms)
        @log.warning("Slow query detected", {
            sql: query.sql,
            time: query.time,
            backtrace: @debug_backtrace()
        })
    }
})

# Explain query execution plan
explanation: @User.where("active", true)
    .join("posts", "users.id", "=", "posts.user_id")
    .explain()

# Database-specific explain
if (@db.getDriverName() == "mysql") {
    detailed: @db.select("EXPLAIN EXTENDED " + query.toSql())
} else if (@db.getDriverName() == "pgsql") {
    analyze: @db.select("EXPLAIN ANALYZE " + query.toSql())
}
```

## Preventing N+1 Queries

```tusk
# Bad: N+1 query problem
users: @User.all()  # 1 query

for (user in users) {
    posts_count: user.posts.count()  # N queries (one per user)
    @display_user(user, posts_count)
}

# Good: Eager loading
users: @User.with("posts").get()  # 2 queries total

for (user in users) {
    posts_count: user.posts.count()  # No additional queries
    @display_user(user, posts_count)
}

# Better: Using withCount
users: @User.withCount("posts").get()  # 1 query with JOIN

for (user in users) {
    posts_count: user.posts_count  # Already loaded
    @display_user(user, posts_count)
}

# Multiple relationships
users: @User.with([
    "profile",
    "posts" => (query) => {
        query.where("published", true)
            .orderBy("created_at", "desc")
            .limit(5)
    },
    "posts.comments" => (query) => {
        query.where("approved", true)
    }
]).get()

# Conditional eager loading
users: @User.all()

if (include_posts) {
    users.load("posts")
}

if (include_stats) {
    users.loadCount(["posts", "comments"])
}
```

## Index Optimization

```tusk
# Check existing indexes
indexes: @db.select("""
    SELECT 
        TABLE_NAME,
        INDEX_NAME,
        GROUP_CONCAT(COLUMN_NAME ORDER BY SEQ_IN_INDEX) as COLUMNS
    FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
    GROUP BY TABLE_NAME, INDEX_NAME
""")

# Migration with proper indexes
#migration optimize_users_table {
    up() {
        @schema.table("users", (table) => {
            # Single column indexes
            table.index("email")  # For WHERE email = ?
            table.index("created_at")  # For ORDER BY created_at
            
            # Composite indexes (order matters!)
            table.index(["status", "created_at"])  # For WHERE status = ? ORDER BY created_at
            table.index(["country", "city"])  # For WHERE country = ? AND city = ?
            
            # Partial indexes (PostgreSQL)
            if (@db.getDriverName() == "pgsql") {
                @db.statement("""
                    CREATE INDEX active_users_email 
                    ON users(email) 
                    WHERE deleted_at IS NULL
                """)
            }
            
            # Full-text indexes (MySQL)
            if (@db.getDriverName() == "mysql") {
                table.fullText(["name", "bio"])  # For full-text search
            }
        })
    }
}

# Query using indexes effectively
# Good: Uses index on (status, created_at)
users: @User.where("status", "active")
    .orderBy("created_at", "desc")
    .limit(10)
    .get()

# Bad: Can't use the index effectively
users: @User.orderBy("created_at")  # Missing WHERE on status
    .where("city", "New York")  # Different column
    .get()
```

## Query Optimization Techniques

```tusk
# Select only needed columns
# Bad
users: @User.all()  # SELECT * FROM users

# Good
users: @User.select("id", "name", "email").get()

# Use exists() instead of count()
# Bad
if (@User.where("email", email).count() > 0) {
    // Email exists
}

# Good
if (@User.where("email", email).exists()) {
    // Email exists
}

# Chunk large datasets
# Bad: Loads all into memory
users: @User.all()
for (user in users) {
    @process(user)
}

# Good: Process in chunks
@User.chunk(1000, (users) => {
    for (user in users) {
        @process(user)
    }
})

# Even better: Use cursor for very large datasets
@User.cursor().each((user) => {
    @process(user)
})

# Avoid using whereIn with large arrays
# Bad
user_ids: @get_large_array_of_ids()  # 10000+ IDs
users: @User.whereIn("id", user_ids).get()

# Good: Use joins or chunk the IDs
user_ids.chunk(1000).each(chunk => {
    users: @User.whereIn("id", chunk).get()
    @process_users(users)
})
```

## Caching Strategies

```tusk
# Cache expensive queries
users: @cache.remember("active_users", 3600, () => {
    return @User.where("active", true)
        .with("profile")
        .orderBy("created_at", "desc")
        .get()
})

# Cache with tags for easy invalidation
products: @cache.tags(["products"]).remember("featured_products", 3600, () => {
    return @Product.where("featured", true)
        .with("category")
        .get()
})

# Invalidate when data changes
#on product.saved {
    @cache.tags(["products"]).flush()
}

# Query result caching at model level
class User extends Model {
    # Cache relationship counts
    cachedPostsCount() {
        return @cache.remember("user." + this.id + ".posts_count", 3600, () => {
            return this.posts().count()
        })
    }
    
    # Cache expensive calculations
    totalSpent() {
        return @cache.remember("user." + this.id + ".total_spent", 3600, () => {
            return this.orders()
                .where("status", "completed")
                .sum("total")
        })
    }
}
```

## Database-Specific Optimizations

```tusk
# MySQL specific
if (@db.getDriverName() == "mysql") {
    # Force index usage
    users: @db.table("users")
        .from(@db.raw("users FORCE INDEX (status_created_at_index)"))
        .where("status", "active")
        .get()
    
    # Optimize group by
    @db.statement("SET sql_mode=(SELECT REPLACE(@@sql_mode,'ONLY_FULL_GROUP_BY',''))")
    
    # Buffer pool warming
    @db.statement("SELECT COUNT(*) FROM users")  # Load into buffer pool
}

# PostgreSQL specific
if (@db.getDriverName() == "pgsql") {
    # Use CTEs for complex queries
    results: @db.select("""
        WITH active_users AS (
            SELECT * FROM users WHERE active = true
        ),
        user_stats AS (
            SELECT user_id, COUNT(*) as post_count
            FROM posts
            GROUP BY user_id
        )
        SELECT u.*, s.post_count
        FROM active_users u
        LEFT JOIN user_stats s ON u.id = s.user_id
    """)
    
    # Parallel queries
    @db.statement("SET max_parallel_workers_per_gather = 4")
}
```

## Aggregation Optimization

```tusk
# Use database aggregation instead of collection methods
# Bad: PHP/Application-level aggregation
orders: @Order.where("created_at", ">", @days_ago(30)).get()
total: orders.sum(order => order.total)  # Processes in memory

# Good: Database-level aggregation  
total: @Order.where("created_at", ">", @days_ago(30))
    .sum("total")  # Calculated by database

# Complex aggregations
stats: @db.table("orders")
    .select(
        @db.raw("DATE(created_at) as date"),
        @db.raw("COUNT(*) as order_count"),
        @db.raw("SUM(total) as revenue"),
        @db.raw("AVG(total) as avg_order_value")
    )
    .where("created_at", ">", @days_ago(30))
    .groupBy("date")
    .orderBy("date", "desc")
    .get()

# Window functions for advanced analytics
ranked_users: @db.select("""
    SELECT 
        *,
        ROW_NUMBER() OVER (ORDER BY total_spent DESC) as rank,
        PERCENT_RANK() OVER (ORDER BY total_spent DESC) as percentile
    FROM (
        SELECT 
            users.*,
            COALESCE(SUM(orders.total), 0) as total_spent
        FROM users
        LEFT JOIN orders ON users.id = orders.user_id
        GROUP BY users.id
    ) as user_totals
""")
```

## Join Optimization

```tusk
# Optimize join order and type
# Use specific join types
results: @db.table("users")
    .join("profiles", "users.id", "=", "profiles.user_id")  # INNER JOIN
    .leftJoin("posts", "users.id", "=", "posts.user_id")   # LEFT JOIN
    .where("users.active", true)
    .select("users.*", "profiles.bio", @db.raw("COUNT(posts.id) as post_count"))
    .groupBy("users.id")
    .get()

# Avoid unnecessary joins
# Bad: Join just to check existence
users: @db.table("users")
    .join("posts", "users.id", "=", "posts.user_id")
    .where("posts.id", "!=", null)
    .distinct()
    .get()

# Good: Use whereExists
users: @db.table("users")
    .whereExists((query) => {
        query.select("id")
            .from("posts")
            .whereColumn("posts.user_id", "users.id")
    })
    .get()

# Denormalization for read-heavy queries
#migration add_post_count_to_users {
    up() {
        @schema.table("users", (table) => {
            table.integer("posts_count").default(0)
        })
        
        # Update existing data
        @db.statement("""
            UPDATE users 
            SET posts_count = (
                SELECT COUNT(*) 
                FROM posts 
                WHERE posts.user_id = users.id
            )
        """)
    }
}
```

## Monitoring and Profiling

```tusk
# Query performance monitoring
class QueryMonitor {
    static monitor() {
        queries: []
        
        @db.listen((query) => {
            queries.push({
                sql: query.sql,
                time: query.time,
                memory: @memory_get_usage()
            })
        })
        
        @app.terminating(() => {
            slow_queries: queries.filter(q => q.time > 100)
            
            if (slow_queries.length > 0) {
                @log.warning("Slow queries detected", {
                    count: slow_queries.length,
                    queries: slow_queries
                })
            }
            
            @metrics.gauge("query.average_time", 
                queries.avg(q => q.time)
            )
        })
    }
}

# Database query analyzer
analyzer: {
    analyze_query: (query) => {
        # Get query plan
        plan: query.explain()
        
        # Check for common issues
        issues: []
        
        if (plan.contains("Using filesort")) {
            issues.push("Query uses filesort - consider adding index")
        }
        
        if (plan.contains("Using temporary")) {
            issues.push("Query uses temporary table - optimize GROUP BY")
        }
        
        if (!plan.contains("Using index")) {
            issues.push("Query doesn't use index - add appropriate index")
        }
        
        return {
            plan: plan,
            issues: issues,
            estimated_rows: @extract_row_count(plan)
        }
    }
}
```

## Query Builder Macros

```tusk
# Define reusable query optimizations
@Builder.macro("optimize", () => {
    return @tap((query) => {
        # Add query hints
        if (@db.getDriverName() == "mysql") {
            query.hint("SQL_NO_CACHE")
        }
        
        # Set fetch mode
        query.setFetchMode(PDO::FETCH_ASSOC)
        
        # Enable query caching
        query.remember(300)  # 5 minutes
    })
})

# Use optimization macro
users: @User.where("active", true)
    .optimize()
    .get()

# Create scope for commonly optimized queries
class User extends Model {
    scopeOptimized(query) {
        return query
            .select("id", "name", "email", "created_at")  # Only needed columns
            .with(["profile:id,user_id,avatar"])  # Minimal eager loading
            .remember(300)  # Cache for 5 minutes
    }
}

users: @User.optimized().active().get()
```

## Best Practices

1. **Monitor query performance** - Log and analyze slow queries
2. **Use indexes wisely** - Index columns used in WHERE, ORDER BY, GROUP BY
3. **Avoid N+1 queries** - Use eager loading
4. **Select only needed data** - Don't use SELECT *
5. **Cache expensive queries** - Use query result caching
6. **Optimize at the database level** - Use proper data types and constraints
7. **Profile in production** - Real data reveals real problems
8. **Regular maintenance** - Update statistics, rebuild indexes

## Related Topics

- `database-indexes` - Index strategies
- `eager-loading` - Relationship loading
- `query-caching` - Caching strategies
- `database-monitoring` - Performance monitoring
- `query-profiling` - Profiling tools