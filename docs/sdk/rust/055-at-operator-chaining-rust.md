# 🔗 @ Operator Chaining in Rust

TuskLang supports elegant chaining of @ operators in Rust, allowing you to compose complex operations in a readable, fluent manner with zero-cost abstractions.

## Basic Chaining

```rust
// Simple property chaining
let user_city = @user.address.city;

// Method chaining
let formatted = @string.trim().to_lowercase().replace(" ", "-");

// Mixed chaining
let result = @query("SELECT * FROM users").first().name.to_uppercase();
```

## Null-Safe Chaining

```rust
// Safe navigation operator
let city = @user?.address?.city;

// With fallback
let city = @user?.address?.city.unwrap_or("Unknown");

// Multiple levels
let manager_email = @employee?.department?.manager?.email.unwrap_or("no-manager@example.com");

// Array access
let first_tag = @post?.tags?.get(0)?.name;
```

## Collection Chaining

```rust
// Vector operations
let result = @users
    .iter()
    .filter(|u| u.active)
    .map(|u| &u.email)
    .collect::<std::collections::HashSet<_>>()
    .into_iter()
    .collect::<Vec<_>>()
    .sort();

// More complex example
let top_customers = @orders
    .iter()
    .group_by(|o| o.customer_id)
    .map(|(customer_id, orders)| {
        CustomerSummary {
            id: customer_id,
            total: orders.iter().map(|o| o.total).sum(),
            count: orders.len(),
        }
    })
    .sorted_by(|a, b| b.total.partial_cmp(&a.total).unwrap())
    .take(10)
    .collect::<Vec<_>>();

// With conditions
let active_premium_users = @users
    .iter()
    .filter(|u| u.status == "active")
    .filter(|u| u.subscription == "premium")
    .sorted_by(|a, b| b.created_at.cmp(&a.created_at))
    .collect::<Vec<_>>();
```

## Query Builder Chaining

```rust
// Database query chaining
let users = @db.table("users")
    .select(vec!["id", "name", "email"])
    .where("active", true)
    .where("created_at", ">", @last_week)
    .where_in("role", vec!["admin", "editor"])
    .order_by("name")
    .limit(50)
    .get();

// With joins
let orders = @db.table("orders")
    .join("users", "orders.user_id", "users.id")
    .join("products", "orders.product_id", "products.id")
    .select(vec![
        "orders.*",
        "users.name as customer_name",
        "products.name as product_name"
    ])
    .where("orders.status", "completed")
    .where_between("orders.created_at", @start_date, @end_date)
    .get();
```

## Transform Chains

```rust
// String transformations
let slug = @title
    .trim()
    .to_lowercase()
    .chars()
    .map(|c| if c.is_alphanumeric() { c } else { '-' })
    .collect::<String>()
    .trim_matches('-')
    .to_string();

// Date transformations  
let formatted_date = @user.created_at
    .format("%Y-%m-%d")
    .to_string()
    .replace("-", "/");

// Number formatting
let price_display = format!("${:.2}", @product.price)
    .replace(".", ",");
```

## Conditional Chaining

```rust
// Chain with conditions
let query = @db.table("products")
    .when(@category.is_some(), |q| q.where("category_id", @category.unwrap()))
    .when(@min_price.is_some(), |q| q.where("price", ">=", @min_price.unwrap()))
    .when(@search.is_some(), |q| q.where("name", "like", &format!("%{}%", @search.unwrap())))
    .when(@sort == "price", |q| q.order_by("price", @direction))
    .get();

// Conditional method calls
let result = @data
    .if(@should_filter, |d| d.iter().filter(|x| x.active).collect())
    .if(@should_sort, |d| d.iter().sorted().collect())
    .if(@limit.is_some(), |d| d.iter().take(@limit.unwrap()).collect());
```

## Pipeline Chaining

```rust
// Unix-style pipeline
let result = @input
    .pipe(|s| s.trim())
    .pipe(|s| s.split(','))
    .pipe(|v| v.iter().map(|s| s.parse::<i32>().unwrap()).collect::<Vec<_>>())
    .pipe(|v| v.iter().filter(|&&x| x > 0).collect::<Vec<_>>())
    .pipe(|v| v.iter().sum::<i32>());

// With custom functions
fn process_text(text: &str) -> Vec<String> {
    text
        .pipe(|s| normalize_whitespace(s))
        .pipe(|s| remove_punctuation(s))
        .pipe(|s| tokenize(s))
        .pipe(|v| remove_stopwords(v))
        .pipe(|v| stem(v))
}

// Data processing pipeline
let report = @raw_data
    .pipe(|d| validate(d))
    .pipe(|d| clean(d))
    .pipe(|d| transform(d))
    .pipe(|d| aggregate(d))
    .pipe(|d| format(d));
```

## Async Chain Operations

```rust
// Async/await chaining
let result = @fetch(url)
    .await?
    .json::<serde_json::Value>()
    .await?
    .pipe(|data| process(data))
    .pipe(|processed| save(processed))
    .await?;

// Parallel operations
let results = futures::future::join_all(vec![
    @fetch_user_data(id),
    @fetch_user_posts(id),
    @fetch_user_stats(id)
]).await;

let (user, posts, stats) = match results.as_slice() {
    [user, posts, stats] => (user, posts, stats),
    _ => return Err("Failed to fetch user data"),
};
```

## Cache Chaining

```rust
// Cache with chaining
let user = @cache
    .remember(&format!("user:{}", @id), Duration::from_secs(3600))
    .get(|| @db.table("users").find(@id))
    .with(vec!["posts", "comments"])
    .append(|u| {
        let mut user = u.clone();
        user.calculated_field = @calculate_something();
        user
    });

// Tagged cache chaining
@cache
    .tags(vec!["users", &format!("user:{}", @id)])
    .put(&format!("user_profile:{}", @id), @profile, Duration::from_secs(3600));
```

## Validation Chaining

```rust
// Input validation chains
let validation = @validator
    .input(@request.all())
    .rules(ValidationRules {
        name: "required|string|max:255",
        email: "required|email|unique:users",
        age: "required|integer|min:18",
    })
    .validate()
    .on_fail(|errors| @response.json(errors, 422))
    .on_pass(|data| @create_user(data));
```

## HTTP Client Chaining

```rust
// HTTP request building
let response = @http
    .post("https://api.example.com/users")
    .headers({
        let mut headers = reqwest::header::HeaderMap::new();
        headers.insert("Authorization", format!("Bearer {}", @token).parse().unwrap());
        headers.insert("Content-Type", "application/json".parse().unwrap());
        headers
    })
    .timeout(Duration::from_secs(5))
    .retry(3)
    .body(serde_json::json!({
        "name": @name,
        "email": @email,
    }))
    .send()
    .await?;

// Response handling
let data = @response
    .ensure_success()?
    .json::<serde_json::Value>()
    .await?
    .get("data")
    .unwrap()
    .as_array()
    .unwrap()
    .iter()
    .map(|item| transform(item))
    .collect::<Vec<_>>();
```

## Form Builder Chaining

```rust
// Form building
let form = @form("user")
    .method("POST")
    .action("/users")
    .field("name")
        .field_type("text")
        .required()
        .placeholder("Enter name")
    .field("email")
        .field_type("email")
        .required()
        .placeholder("Enter email")
    .field("age")
        .field_type("number")
        .min(18)
        .max(120)
    .submit("Create User");
```

## Iterator Chaining

```rust
// Complex iterator chains
let processed_data = @raw_data
    .iter()
    .filter(|item| item.is_valid())
    .map(|item| item.transform())
    .filter_map(|item| item.ok())
    .take_while(|item| item.priority > 0)
    .collect::<Vec<_>>();

// Parallel iterator chains
let results = @data
    .par_iter()
    .map(|item| process_item(item))
    .filter(|result| result.is_success())
    .collect::<Vec<_>>();
```

## Error Handling Chains

```rust
// Result chaining
let result = @operation()
    .and_then(|data| process_data(data))
    .and_then(|processed| validate_data(processed))
    .and_then(|valid| save_data(valid))
    .map_err(|error| {
        @log("Error in chain: {}", error);
        error
    });

// Option chaining
let user_email = @user
    .and_then(|u| u.profile)
    .and_then(|p| p.contact)
    .and_then(|c| c.email)
    .unwrap_or("no-email@example.com");
```

## Database Transaction Chaining

```rust
// Transaction chains
let result = @db.transaction(|tx| {
    tx.table("users")
        .insert(@user_data)
        .and_then(|user_id| {
            tx.table("profiles")
                .insert(ProfileData {
                    user_id,
                    bio: @bio,
                })
        })
        .and_then(|profile_id| {
            tx.table("preferences")
                .insert(PreferenceData {
                    user_id,
                    theme: @theme,
                })
        })
        .commit()
});
```

## File System Chaining

```rust
// File operations chaining
let content = @file
    .read("data.txt")
    .and_then(|content| content.parse::<String>())
    .and_then(|content| content.lines().collect::<Vec<_>>())
    .and_then(|lines| lines.iter().filter(|line| !line.is_empty()).collect::<Vec<_>>())
    .and_then(|lines| lines.join("\n"))
    .unwrap_or_else(|_| "Default content".to_string());
```

## Configuration Chaining

```rust
// Configuration building
let config = @config
    .load("app.tusk")
    .and_then(|cfg| cfg.merge(@env_config))
    .and_then(|cfg| cfg.validate())
    .and_then(|cfg| cfg.resolve_placeholders())
    .unwrap_or_else(|_| @default_config);
```

## Best Practices

### 1. Use Meaningful Variable Names
```rust
// Good
let active_users = @users.iter().filter(|u| u.active).collect::<Vec<_>>();

// Better
let active_users = @users
    .iter()
    .filter(|user| user.is_active())
    .collect::<Vec<_>>();
```

### 2. Break Long Chains
```rust
// Break long chains for readability
let processed_users = @users
    .iter()
    .filter(|user| user.is_active())
    .collect::<Vec<_>>();

let user_emails = processed_users
    .iter()
    .map(|user| &user.email)
    .collect::<Vec<_>>();
```

### 3. Handle Errors Gracefully
```rust
// Use Result chaining for error handling
let result = @operation()
    .and_then(|data| process(data))
    .map_err(|error| {
        @log("Operation failed: {}", error);
        error
    });
```

### 4. Use Type Annotations
```rust
// Explicit type annotations for clarity
let users: Vec<User> = @db
    .table("users")
    .where("active", true)
    .get();
```

### 5. Leverage Rust's Type System
```rust
// Use Rust's type system for safety
let user_ids: Vec<u32> = @users
    .iter()
    .map(|user| user.id)
    .collect();
```

## Performance Considerations

### 1. Lazy Evaluation
```rust
// Use iterators for lazy evaluation
let expensive_operation = @data
    .iter()
    .filter(|item| item.is_expensive())
    .map(|item| item.process())
    .collect::<Vec<_>>();
```

### 2. Parallel Processing
```rust
// Use parallel iterators for CPU-intensive tasks
use rayon::prelude::*;

let results = @data
    .par_iter()
    .map(|item| process_item(item))
    .collect::<Vec<_>>();
```

### 3. Memory Efficiency
```rust
// Use references to avoid cloning
let user_names: Vec<&str> = @users
    .iter()
    .map(|user| user.name.as_str())
    .collect();
```

The @ operator chaining in Rust provides a powerful, type-safe way to compose complex operations while maintaining Rust's performance characteristics and memory safety guarantees. 