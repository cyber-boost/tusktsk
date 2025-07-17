# Query Builder in TuskLang for Rust

TuskLang's Rust query builder provides a type-safe, fluent interface for building complex database queries with compile-time guarantees, zero-cost abstractions, and full async/await support.

## 🚀 **Why Rust Query Builder?**

Rust's type system and ownership model make it the perfect language for building safe, efficient database queries:

- **Compile-Time Safety**: Catch query errors before runtime
- **Type Inference**: Automatic type deduction for query results
- **Memory Safety**: No null pointer dereferences or data races
- **Performance**: Zero-cost abstractions with native speed
- **Async/Await**: Non-blocking query execution

## Basic Query Building

```rust
use tusk_db::{QueryBuilder, Database, Result};
use serde::{Deserialize, Serialize};

#[derive(Debug, Serialize, Deserialize)]
struct User {
    id: i32,
    name: String,
    email: String,
    active: bool,
    created_at: chrono::DateTime<Utc>,
}

// Simple select query
async fn get_all_users() -> Result<Vec<User>> {
    let users = @db.table::<User>("users")
        .get()
        .await?;
    
    Ok(users)
}

// Select specific columns
async fn get_user_names() -> Result<Vec<String>> {
    let names = @db.table("users")
        .select(&["name"])
        .get::<String>()
        .await?;
    
    Ok(names)
}

// Where clauses with type safety
async fn get_active_users() -> Result<Vec<User>> {
    let users = @db.table::<User>("users")
        .where_eq("active", true)
        .get()
        .await?;
    
    Ok(users)
}

// Multiple where conditions
async fn get_recent_active_users() -> Result<Vec<User>> {
    let users = @db.table::<User>("users")
        .where_eq("active", true)
        .where_gt("created_at", chrono::Utc::now() - chrono::Duration::days(30))
        .get()
        .await?;
    
    Ok(users)
}
```

## Advanced Where Clauses

```rust
// Complex where conditions with type safety
async fn complex_user_query() -> Result<Vec<User>> {
    let users = @db.table::<User>("users")
        .where_eq("active", true)
        .where_in("id", &[1, 2, 3, 4, 5])
        .where_not_in("email", &["admin@example.com", "test@example.com"])
        .where_between("created_at", &[
            chrono::Utc::now() - chrono::Duration::days(90),
            chrono::Utc::now()
        ])
        .where_like("name", "%John%")
        .where_not_null("email_verified_at")
        .get()
        .await?;
    
    Ok(users)
}

// Raw where clauses for complex conditions
async fn raw_where_query() -> Result<Vec<User>> {
    let users = @db.table::<User>("users")
        .where_raw("LENGTH(name) > ? AND email LIKE ?", &[&10, &"%@gmail.com"])
        .where_raw("created_at > DATE_SUB(NOW(), INTERVAL ? DAY)", &[&30])
        .get()
        .await?;
    
    Ok(users)
}

// Null handling with Rust's Option type
async fn handle_null_values() -> Result<Vec<User>> {
    let users = @db.table::<User>("users")
        .where_null("deleted_at")
        .where_not_null("email")
        .or_where_null("phone")
        .get()
        .await?;
    
    Ok(users)
}
```

## Joins and Relationships

```rust
use tusk_db::{JoinType, JoinClause};

#[derive(Debug, Serialize, Deserialize)]
struct Post {
    id: i32,
    title: String,
    content: String,
    user_id: i32,
    published: bool,
}

#[derive(Debug, Serialize, Deserialize)]
struct UserWithPosts {
    id: i32,
    name: String,
    email: String,
    post_count: i64,
    latest_post: Option<String>,
}

// Inner join with type safety
async fn get_users_with_posts() -> Result<Vec<UserWithPosts>> {
    let users = @db.table::<UserWithPosts>("users")
        .select(&[
            "users.*",
            "COUNT(posts.id) as post_count",
            "MAX(posts.title) as latest_post"
        ])
        .join("posts", "users.id", "=", "posts.user_id")
        .where_eq("posts.published", true)
        .group_by("users.id")
        .having("COUNT(posts.id)", ">", 0)
        .get()
        .await?;
    
    Ok(users)
}

// Multiple joins with complex conditions
async fn complex_join_query() -> Result<Vec<UserWithPosts>> {
    let users = @db.table::<UserWithPosts>("users")
        .select(&[
            "users.*",
            "COUNT(DISTINCT posts.id) as post_count",
            "COUNT(DISTINCT comments.id) as comment_count"
        ])
        .left_join("posts", "users.id", "=", "posts.user_id")
        .left_join("comments", "posts.id", "=", "comments.post_id")
        .where_eq("users.active", true)
        .where_eq("posts.published", true)
        .group_by("users.id")
        .order_by("post_count", "DESC")
        .limit(10)
        .get()
        .await?;
    
    Ok(users)
}

// Cross join for complex relationships
async fn cross_join_example() -> Result<Vec<UserWithPosts>> {
    let users = @db.table::<UserWithPosts>("users")
        .cross_join("user_roles")
        .cross_join("roles")
        .where_raw("users.id = user_roles.user_id AND user_roles.role_id = roles.id", &[])
        .where_eq("roles.name", "admin")
        .get()
        .await?;
    
    Ok(users)
}
```

## Aggregations and Grouping

```rust
#[derive(Debug, Serialize, Deserialize)]
struct UserStats {
    total_users: i64,
    active_users: i64,
    avg_age: Option<f64>,
    max_created_at: Option<chrono::DateTime<Utc>>,
}

// Basic aggregations
async fn get_user_statistics() -> Result<UserStats> {
    let stats = @db.table("users")
        .select(&[
            "COUNT(*) as total_users",
            "SUM(CASE WHEN active = true THEN 1 ELSE 0 END) as active_users",
            "AVG(age) as avg_age",
            "MAX(created_at) as max_created_at"
        ])
        .get_one::<UserStats>()
        .await?;
    
    Ok(stats)
}

// Grouped aggregations
async fn get_users_by_month() -> Result<Vec<UserStats>> {
    let stats = @db.table("users")
        .select(&[
            "DATE_FORMAT(created_at, '%Y-%m') as month",
            "COUNT(*) as total_users",
            "SUM(CASE WHEN active = true THEN 1 ELSE 0 END) as active_users"
        ])
        .group_by("DATE_FORMAT(created_at, '%Y-%m')")
        .order_by("month", "DESC")
        .get()
        .await?;
    
    Ok(stats)
}

// Having clauses for grouped results
async fn get_popular_users() -> Result<Vec<UserWithPosts>> {
    let users = @db.table::<UserWithPosts>("users")
        .select(&[
            "users.*",
            "COUNT(posts.id) as post_count"
        ])
        .left_join("posts", "users.id", "=", "posts.user_id")
        .group_by("users.id")
        .having("COUNT(posts.id)", ">", 5)
        .having("COUNT(posts.id)", "<", 100)
        .order_by("post_count", "DESC")
        .get()
        .await?;
    
    Ok(users)
}
```

## Ordering and Limiting

```rust
// Basic ordering
async fn get_users_ordered() -> Result<Vec<User>> {
    let users = @db.table::<User>("users")
        .order_by("name", "ASC")
        .order_by("created_at", "DESC")
        .get()
        .await?;
    
    Ok(users)
}

// Raw ordering for complex expressions
async fn complex_ordering() -> Result<Vec<User>> {
    let users = @db.table::<User>("users")
        .order_by_raw("CASE WHEN active = true THEN 0 ELSE 1 END")
        .order_by("name", "ASC")
        .get()
        .await?;
    
    Ok(users)
}

// Limiting and offset for pagination
async fn paginated_users(page: i32, per_page: i32) -> Result<Vec<User>> {
    let offset = (page - 1) * per_page;
    
    let users = @db.table::<User>("users")
        .where_eq("active", true)
        .order_by("created_at", "DESC")
        .limit(per_page as usize)
        .offset(offset as usize)
        .get()
        .await?;
    
    Ok(users)
}

// Cursor-based pagination for large datasets
async fn cursor_paginated_users(last_id: Option<i32>, limit: usize) -> Result<Vec<User>> {
    let mut query = @db.table::<User>("users")
        .where_eq("active", true)
        .order_by("id", "ASC")
        .limit(limit);
    
    if let Some(id) = last_id {
        query = query.where_gt("id", id);
    }
    
    let users = query.get().await?;
    Ok(users)
}
```

## Subqueries and Complex Queries

```rust
// Subquery in where clause
async fn users_with_recent_posts() -> Result<Vec<User>> {
    let users = @db.table::<User>("users")
        .where_in("id", |query| {
            query.table("posts")
                .select(&["user_id"])
                .where_gt("created_at", chrono::Utc::now() - chrono::Duration::days(7))
                .group_by("user_id")
        })
        .get()
        .await?;
    
    Ok(users)
}

// EXISTS subquery
async fn users_with_posts() -> Result<Vec<User>> {
    let users = @db.table::<User>("users")
        .where_exists(|query| {
            query.table("posts")
                .where_raw("posts.user_id = users.id", &[])
                .where_eq("published", true)
        })
        .get()
        .await?;
    
    Ok(users)
}

// UNION queries
async fn combined_user_data() -> Result<Vec<User>> {
    let users = @db.table::<User>("users")
        .select(&["id", "name", "email", "created_at"])
        .where_eq("active", true)
        .union(|query| {
            query.table("archived_users")
                .select(&["id", "name", "email", "created_at"])
                .where_eq("restore_eligible", true)
        })
        .order_by("created_at", "DESC")
        .get()
        .await?;
    
    Ok(users)
}
```

## Insert, Update, and Delete Operations

```rust
// Insert with query builder
async fn create_user(name: &str, email: &str) -> Result<i32> {
    let user_id = @db.table("users")
        .insert(&[
            ("name", name),
            ("email", email),
            ("active", &true),
            ("created_at", &chrono::Utc::now()),
        ])
        .await?;
    
    Ok(user_id)
}

// Batch insert for performance
async fn create_multiple_users(users: Vec<(String, String)>) -> Result<Vec<i32>> {
    let user_data: Vec<_> = users.into_iter()
        .map(|(name, email)| {
            vec![
                ("name", name.as_str()),
                ("email", email.as_str()),
                ("active", "true"),
                ("created_at", &chrono::Utc::now().to_rfc3339()),
            ]
        })
        .collect();
    
    let user_ids = @db.table("users")
        .insert_batch(&user_data)
        .await?;
    
    Ok(user_ids)
}

// Update with conditions
async fn update_user_status(user_id: i32, active: bool) -> Result<u64> {
    let affected = @db.table("users")
        .where_eq("id", user_id)
        .update(&[
            ("active", &active),
            ("updated_at", &chrono::Utc::now()),
        ])
        .await?;
    
    Ok(affected)
}

// Increment/decrement operations
async fn increment_user_post_count(user_id: i32) -> Result<u64> {
    let affected = @db.table("users")
        .where_eq("id", user_id)
        .increment("post_count", 1)
        .await?;
    
    Ok(affected)
}

// Delete with conditions
async fn delete_inactive_users() -> Result<u64> {
    let deleted = @db.table("users")
        .where_eq("active", false)
        .where_lt("last_login", chrono::Utc::now() - chrono::Duration::days(365))
        .delete()
        .await?;
    
    Ok(deleted)
}
```

## Raw SQL and Custom Queries

```rust
// Raw SQL with type safety
async fn complex_raw_query() -> Result<Vec<User>> {
    let users = @db.raw::<User>(
        "SELECT u.*, COUNT(p.id) as post_count 
         FROM users u 
         LEFT JOIN posts p ON u.id = p.user_id 
         WHERE u.active = ? 
         GROUP BY u.id 
         HAVING COUNT(p.id) > ? 
         ORDER BY post_count DESC",
        &[&true, &5]
    ).await?;
    
    Ok(users)
}

// Stored procedures
async fn call_stored_procedure(user_id: i32) -> Result<Vec<User>> {
    let users = @db.raw::<User>(
        "CALL GetUserWithPosts(?)",
        &[&user_id]
    ).await?;
    
    Ok(users)
}

// Dynamic query building
async fn dynamic_query(filters: HashMap<String, String>) -> Result<Vec<User>> {
    let mut query = @db.table::<User>("users");
    
    for (field, value) in filters {
        match field.as_str() {
            "name" => query = query.where_like("name", &format!("%{}%", value)),
            "email" => query = query.where_like("email", &format!("%{}%", value)),
            "active" => {
                if let Ok(active) = value.parse::<bool>() {
                    query = query.where_eq("active", active);
                }
            }
            _ => {} // Ignore unknown filters
        }
    }
    
    let users = query.get().await?;
    Ok(users)
}
```

## Query Optimization and Performance

```rust
// Eager loading to avoid N+1 queries
async fn users_with_posts_eager() -> Result<Vec<User>> {
    let users = @db.table::<User>("users")
        .with(&["posts", "posts.comments"])
        .where_eq("active", true)
        .get()
        .await?;
    
    Ok(users)
}

// Query caching
async fn cached_user_query() -> Result<Vec<User>> {
    let cache_key = "active_users";
    
    let users = @cache::remember(cache_key, 3600, || async {
        @db.table::<User>("users")
            .where_eq("active", true)
            .order_by("name")
            .get()
            .await
    }).await?;
    
    Ok(users)
}

// Chunk processing for large datasets
async fn process_large_user_dataset() -> Result<()> {
    @db.table::<User>("users")
        .where_eq("active", true)
        .chunk(1000, |users| async {
            for user in users {
                // Process each user
                @process_user(user).await?;
            }
            Ok::<(), Box<dyn std::error::Error>>(())
        })
        .await?;
    
    Ok(())
}

// Query monitoring and logging
async fn monitored_query() -> Result<Vec<User>> {
    let start = std::time::Instant::now();
    
    let users = @db.table::<User>("users")
        .where_eq("active", true)
        .get()
        .await?;
    
    let duration = start.elapsed();
    
    // Log slow queries
    if duration > std::time::Duration::from_millis(100) {
        @log::warn!("Slow query detected", {
            query: "get_active_users",
            duration: duration.as_millis(),
            count: users.len()
        });
    }
    
    Ok(users)
}
```

## Error Handling and Recovery

```rust
use tusk_db::{QueryError, DatabaseError};
use thiserror::Error;

#[derive(Error, Debug)]
pub enum QueryBuilderError {
    #[error("Query execution failed: {0}")]
    Query(#[from] QueryError),
    
    #[error("No results found")]
    NoResults,
    
    #[error("Multiple results found when expecting one")]
    MultipleResults,
}

// Robust query execution with error handling
async fn safe_user_query(user_id: i32) -> Result<User, QueryBuilderError> {
    let user = @db.table::<User>("users")
        .where_eq("id", user_id)
        .first()
        .await
        .map_err(QueryBuilderError::Query)?;
    
    user.ok_or(QueryBuilderError::NoResults)
}

// Retry logic for transient failures
async fn retry_query<F, T>(mut f: F, max_attempts: u32) -> Result<T>
where
    F: FnMut() -> std::pin::Pin<Box<dyn std::future::Future<Output = Result<T>> + Send>>,
{
    let mut attempts = 0;
    
    loop {
        match f().await {
            Ok(result) => return Ok(result),
            Err(e) if attempts < max_attempts => {
                attempts += 1;
                let delay = std::time::Duration::from_secs(2_u64.pow(attempts));
                tokio::time::sleep(delay).await;
                continue;
            }
            Err(e) => return Err(e),
        }
    }
}
```

## Testing Query Builder

```rust
use tusk_db::test_utils::{TestDatabase, TestTransaction};

#[tokio::test]
async fn test_query_builder() -> Result<()> {
    let test_db = @TestDatabase::new().await?;
    let mut tx = test_db.begin_transaction().await?;
    
    // Test basic query
    let users = @db.table::<User>("users")
        .where_eq("active", true)
        .get()
        .await?;
    
    assert!(users.is_empty()); // Should be empty in test database
    
    // Test insert and query
    let user_id = @db.table("users")
        .insert(&[
            ("name", "Test User"),
            ("email", "test@example.com"),
            ("active", &true),
        ])
        .await?;
    
    let user = @db.table::<User>("users")
        .where_eq("id", user_id)
        .first()
        .await?
        .expect("User should exist");
    
    assert_eq!(user.name, "Test User");
    assert_eq!(user.email, "test@example.com");
    
    tx.rollback().await?;
    Ok(())
}
```

## Best Practices for Rust Query Builder

1. **Use Type Safety**: Leverage Rust's type system for compile-time validation
2. **Handle Errors**: Use proper error types and Result handling
3. **Async/Await**: Use non-blocking operations for better performance
4. **Connection Pooling**: Let the query builder manage connections
5. **Prepared Statements**: Use parameterized queries (automatic)
6. **Indexing**: Ensure proper database indexes for your queries
7. **Monitoring**: Track query performance and errors
8. **Testing**: Use test databases and transactions
9. **Caching**: Cache frequently accessed data
10. **Pagination**: Use proper pagination for large datasets

## Related Topics

- `database-overview-rust` - Database integration overview
- `orm-models-rust` - Model definition and usage
- `migrations-rust` - Database schema versioning
- `relationships-rust` - Model relationships
- `database-transactions-rust` - Transaction handling

---

**Ready to build type-safe, performant database queries with Rust and TuskLang?** 