# 🦀 TuskLang Rust Testing Strategies

**"We don't bow to any king" - Rust Edition**

Master comprehensive testing strategies for TuskLang Rust applications. From unit tests to integration tests, performance benchmarks to security testing - learn how to build reliable, high-performance applications with confidence.

## 🧪 Unit Testing

### Basic Parser Testing

```rust
use tusklang_rust::{parse, Parser};
use tokio_test;

#[tokio::test]
async fn test_basic_parsing() {
    let mut parser = Parser::new();
    
    let tsk_content = r#"
[test]
value: 42
string: "hello"
boolean: true
array: [1, 2, 3]
object: {
    key: "value"
    nested: {
        deep: "data"
    }
}
"#;
    
    let data = parser.parse(tsk_content).expect("Failed to parse");
    
    assert_eq!(data["test"]["value"], 42);
    assert_eq!(data["test"]["string"], "hello");
    assert_eq!(data["test"]["boolean"], true);
    assert_eq!(data["test"]["array"][0], 1);
    assert_eq!(data["test"]["object"]["key"], "value");
    assert_eq!(data["test"]["object"]["nested"]["deep"], "data");
}

#[tokio::test]
async fn test_syntax_styles() {
    let mut parser = Parser::new();
    
    // Test traditional syntax
    let traditional = r#"
[test]
value: 42
"#;
    
    let data1 = parser.parse(traditional).expect("Failed to parse traditional");
    assert_eq!(data1["test"]["value"], 42);
    
    // Test curly brace syntax
    let curly = r#"
test {
    value: 42
}
"#;
    
    let data2 = parser.parse(curly).expect("Failed to parse curly");
    assert_eq!(data2["test"]["value"], 42);
    
    // Test angle bracket syntax
    let angle = r#"
test >
    value: 42
<
"#;
    
    let data3 = parser.parse(angle).expect("Failed to parse angle");
    assert_eq!(data3["test"]["value"], 42);
}

#[tokio::test]
async fn test_variables_and_interpolation() {
    let mut parser = Parser::new();
    
    let tsk_content = r#"
$app_name: "TestApp"
$version: "1.0.0"

[config]
name: $app_name
version: $version
full_name: "${app_name} v${version}"
"#;
    
    let data = parser.parse(tsk_content).expect("Failed to parse");
    
    assert_eq!(data["config"]["name"], "TestApp");
    assert_eq!(data["config"]["version"], "1.0.0");
    assert_eq!(data["config"]["full_name"], "TestApp v1.0.0");
}
```

### @ Operator Testing

```rust
use tusklang_rust::{parse, Parser};
use std::env;
use tokio_test;

#[tokio::test]
async fn test_environment_variables() {
    let mut parser = Parser::new();
    
    // Set test environment variables
    env::set_var("TEST_API_KEY", "test_key_123");
    env::set_var("TEST_DB_HOST", "localhost");
    
    let tsk_content = r#"
[config]
api_key: @env("TEST_API_KEY", "default_key")
db_host: @env("TEST_DB_HOST", "default_host")
missing_var: @env("MISSING_VAR", "fallback_value")
"#;
    
    let data = parser.parse(tsk_content).expect("Failed to parse");
    
    assert_eq!(data["config"]["api_key"], "test_key_123");
    assert_eq!(data["config"]["db_host"], "localhost");
    assert_eq!(data["config"]["missing_var"], "fallback_value");
}

#[tokio::test]
async fn test_conditional_logic() {
    let mut parser = Parser::new();
    
    env::set_var("APP_ENV", "production");
    
    let tsk_content = r#"
$environment: @env("APP_ENV", "development")

[server]
port: @if($environment == "production", 80, 8080)
debug: @if($environment != "production", true, false)
workers: @if($environment == "production", 4, 1)
"#;
    
    let data = parser.parse(tsk_content).expect("Failed to parse");
    
    assert_eq!(data["server"]["port"], 80);
    assert_eq!(data["server"]["debug"], false);
    assert_eq!(data["server"]["workers"], 4);
}

#[tokio::test]
async fn test_mathematical_operations() {
    let mut parser = Parser::new();
    
    let tsk_content = r#"
[math]
addition: @math.add(5, 3)
subtraction: @math.sub(10, 4)
multiplication: @math.mul(6, 7)
division: @math.div(20, 4)
modulo: @math.mod(17, 5)
power: @math.pow(2, 8)
"#;
    
    let data = parser.parse(tsk_content).expect("Failed to parse");
    
    assert_eq!(data["math"]["addition"], 8);
    assert_eq!(data["math"]["subtraction"], 6);
    assert_eq!(data["math"]["multiplication"], 42);
    assert_eq!(data["math"]["division"], 5);
    assert_eq!(data["math"]["modulo"], 2);
    assert_eq!(data["math"]["power"], 256);
}
```

### FUJSEN Testing

```rust
use tusklang_rust::{parse, Parser};
use tokio_test;

#[tokio::test]
async fn test_fujsen_execution() {
    let mut parser = Parser::new();
    
    let tsk_content = r#"
[math]
add_fujsen = '''
fn add(a: i32, b: i32) -> i32 {
    a + b
}
'''

[utils]
format_name_fujsen = '''
fn format_name(first: &str, last: &str) -> String {
    format!("{} {}", first, last)
}
'''

[validation]
validate_email_fujsen = '''
fn validate_email(email: &str) -> bool {
    email.contains("@") && email.contains(".")
}
'''
"#;
    
    let data = parser.parse(tsk_content).expect("Failed to parse");
    
    // Test math function
    let sum = parser.execute_fujsen("math", "add", &[&5, &3])
        .await
        .expect("Failed to execute add function");
    assert_eq!(sum, 8);
    
    // Test string formatting
    let full_name = parser.execute_fujsen("utils", "format_name", &[&"John", &"Doe"])
        .await
        .expect("Failed to execute format_name function");
    assert_eq!(full_name, "John Doe");
    
    // Test validation function
    let valid_email = parser.execute_fujsen("validation", "validate_email", &[&"test@example.com"])
        .await
        .expect("Failed to execute validate_email function");
    assert_eq!(valid_email, true);
    
    let invalid_email = parser.execute_fujsen("validation", "validate_email", &[&"invalid-email"])
        .await
        .expect("Failed to execute validate_email function");
    assert_eq!(invalid_email, false);
}

#[tokio::test]
async fn test_complex_fujsen() {
    let mut parser = Parser::new();
    
    let tsk_content = r#"
[data_processing]
process_users_fujsen = '''
fn process_users(users: Vec<serde_json::Value>) -> Vec<serde_json::Value> {
    users.into_iter()
        .filter(|user| user["active"].as_bool().unwrap_or(false))
        .map(|user| {
            let mut processed = user.clone();
            processed["processed"] = serde_json::Value::Bool(true);
            processed["name_upper"] = serde_json::Value::String(
                user["name"].as_str().unwrap_or("").to_uppercase()
            );
            processed
        })
        .collect()
}
'''
"#;
    
    let data = parser.parse(tsk_content).expect("Failed to parse");
    
    let test_users = vec![
        serde_json::json!({"id": 1, "name": "Alice", "active": true}),
        serde_json::json!({"id": 2, "name": "Bob", "active": false}),
        serde_json::json!({"id": 3, "name": "Charlie", "active": true}),
    ];
    
    let processed_users = parser.execute_fujsen("data_processing", "process_users", &[&test_users])
        .await
        .expect("Failed to execute process_users function");
    
    let processed: Vec<serde_json::Value> = serde_json::from_value(processed_users).unwrap();
    
    assert_eq!(processed.len(), 2); // Only active users
    assert_eq!(processed[0]["name_upper"], "ALICE");
    assert_eq!(processed[1]["name_upper"], "CHARLIE");
    assert_eq!(processed[0]["processed"], true);
}
```

## 🔗 Integration Testing

### Database Integration Testing

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
        CREATE TABLE users (
            id INTEGER PRIMARY KEY,
            name TEXT NOT NULL,
            email TEXT UNIQUE,
            active BOOLEAN DEFAULT 1,
            created_at DATETIME DEFAULT CURRENT_TIMESTAMP
        );
        
        INSERT INTO users (name, email) VALUES 
            ('Alice', 'alice@example.com'),
            ('Bob', 'bob@example.com'),
            ('Charlie', 'charlie@example.com');
    "#).await.expect("Failed to setup test data");
    
    let tsk_content = r#"
[users]
total_count: @query("SELECT COUNT(*) FROM users")
active_count: @query("SELECT COUNT(*) FROM users WHERE active = 1")
user_list: @query("SELECT name, email FROM users WHERE active = 1")
recent_users: @query("SELECT * FROM users WHERE created_at > datetime('now', '-1 day')")
"#;
    
    let data = parser.parse(tsk_content).await.expect("Failed to parse");
    
    assert_eq!(data["users"]["total_count"], 3);
    assert_eq!(data["users"]["active_count"], 3);
    
    let user_list: Vec<serde_json::Value> = serde_json::from_value(data["users"]["user_list"].clone()).unwrap();
    assert_eq!(user_list.len(), 3);
    assert_eq!(user_list[0]["name"], "Alice");
    assert_eq!(user_list[0]["email"], "alice@example.com");
}

#[tokio::test]
async fn test_postgresql_integration() {
    use tusklang_rust::adapters::postgresql::{PostgreSQLAdapter, PostgreSQLConfig};
    
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
avg_price: @query("SELECT AVG(price) FROM test_products")
"#;
    
    let data = parser.parse(tsk_content).await.expect("Failed to parse");
    
    assert_eq!(data["products"]["count"], 2);
    assert_eq!(data["products"]["total_value"], 31.98);
    assert_eq!(data["products"]["avg_price"], 15.99);
}
```

### Cache Integration Testing

```rust
use tusklang_rust::{parse, Parser, cache::{MemoryCache, RedisCache}};
use tokio_test;

#[tokio::test]
async fn test_memory_cache_integration() {
    let mut parser = Parser::new();
    let memory_cache = MemoryCache::new();
    parser.set_cache(memory_cache);
    
    let tsk_content = r#"
[cached_data]
user_profile: @cache("5m", "user_profile", @request.user_id)
expensive_calculation: @cache("1m", "expensive_operation")
"#;
    
    // Mock request context
    let mut context = std::collections::HashMap::new();
    let mut request = std::collections::HashMap::new();
    request.insert("user_id".to_string(), 123);
    context.insert("request".to_string(), request);
    
    let data = parser.parse_with_context(tsk_content, &context).await.expect("Failed to parse");
    
    // Verify cache keys were created
    assert!(data["cached_data"]["user_profile"].is_string());
    assert!(data["cached_data"]["expensive_calculation"].is_string());
}

#[tokio::test]
async fn test_redis_cache_integration() {
    use tusklang_rust::adapters::redis::RedisConfig;
    
    let redis_cache = RedisCache::new(RedisConfig {
        host: "localhost".to_string(),
        port: 6379,
        db: 0,
    }).await.expect("Failed to connect to Redis");
    
    let mut parser = Parser::new();
    parser.set_cache(redis_cache);
    
    let tsk_content = r#"
[cache_stats]
total_keys: @cache.keys("*")
memory_usage: @cache.memory_usage()
"#;
    
    let data = parser.parse(tsk_content).await.expect("Failed to parse");
    
    assert!(data["cache_stats"]["total_keys"].is_number());
    assert!(data["cache_stats"]["memory_usage"].is_number());
}
```

## 🚀 Performance Testing

### Parsing Performance Tests

```rust
use tusklang_rust::{parse, Parser};
use std::time::Instant;
use tokio_test;

#[tokio::test]
async fn test_parsing_performance() {
    let mut parser = Parser::new();
    
    // Create test data of different sizes
    let small_config = r#"
[app]
name: "TestApp"
version: "1.0.0"
"#;
    
    let medium_config = r#"
[app]
name: "TestApp"
version: "1.0.0"

[database]
host: "localhost"
port: 5432
name: "testdb"
user: "testuser"
password: "testpass"

[server]
host: "0.0.0.0"
port: 8080
workers: 4

[logging]
level: "info"
format: "json"
file: "/var/log/app.log"

[cache]
redis_host: "localhost"
redis_port: 6379
ttl: "5m"
"#;
    
    let large_config = format!("{}\n[data]\n{}", medium_config, 
        (0..1000).map(|i| format!("key_{}: \"value_{}\"", i, i)).collect::<Vec<_>>().join("\n")
    );
    
    // Benchmark small config
    let start = Instant::now();
    for _ in 0..1000 {
        let _data = parser.parse(small_config).expect("Failed to parse");
    }
    let small_duration = start.elapsed();
    
    // Benchmark medium config
    let start = Instant::now();
    for _ in 0..1000 {
        let _data = parser.parse(medium_config).expect("Failed to parse");
    }
    let medium_duration = start.elapsed();
    
    // Benchmark large config
    let start = Instant::now();
    for _ in 0..100 {
        let _data = parser.parse(&large_config).expect("Failed to parse");
    }
    let large_duration = start.elapsed();
    
    println!("Performance Results:");
    println!("  Small config (1KB): {:?} for 1000 iterations", small_duration);
    println!("  Medium config (2KB): {:?} for 1000 iterations", medium_duration);
    println!("  Large config (50KB): {:?} for 100 iterations", large_duration);
    
    // Assert reasonable performance
    assert!(small_duration.as_millis() < 100); // Should be very fast
    assert!(medium_duration.as_millis() < 500); // Should be reasonably fast
    assert!(large_duration.as_millis() < 5000); // Should handle large configs
}

#[tokio::test]
async fn test_fujsen_performance() {
    let mut parser = Parser::new();
    
    let tsk_content = r#"
[math]
add_fujsen = '''
fn add(a: i32, b: i32) -> i32 {
    a + b
}
'''

[complex]
process_data_fujsen = '''
fn process_data(data: Vec<i32>) -> i32 {
    data.iter().sum()
}
'''
"#;
    
    parser.parse(tsk_content).expect("Failed to parse");
    
    // Benchmark simple function
    let start = Instant::now();
    for _ in 0..10000 {
        let _result = parser.execute_fujsen("math", "add", &[&5, &3])
            .await
            .expect("Failed to execute");
    }
    let simple_duration = start.elapsed();
    
    // Benchmark complex function
    let test_data: Vec<i32> = (0..1000).collect();
    let start = Instant::now();
    for _ in 0..1000 {
        let _result = parser.execute_fujsen("complex", "process_data", &[&test_data])
            .await
            .expect("Failed to execute");
    }
    let complex_duration = start.elapsed();
    
    println!("FUJSEN Performance Results:");
    println!("  Simple function: {:?} for 10000 iterations", simple_duration);
    println!("  Complex function: {:?} for 1000 iterations", complex_duration);
    
    // Assert reasonable performance
    assert!(simple_duration.as_millis() < 1000); // Should be very fast
    assert!(complex_duration.as_millis() < 5000); // Should handle complex operations
}
```

### Memory Usage Tests

```rust
use tusklang_rust::{parse, Parser};
use std::alloc::{alloc, dealloc, Layout};
use tokio_test;

#[tokio::test]
async fn test_memory_usage() {
    let mut parser = Parser::new();
    
    // Test memory allocation for parser
    let layout = Layout::new::<Parser>();
    let ptr = unsafe { alloc(layout) };
    
    if !ptr.is_null() {
        println!("✅ Memory allocation successful for Parser");
        unsafe { dealloc(ptr, layout) };
    }
    
    // Test memory usage with different config sizes
    let small_config = r#"
[test]
value: 42
"#;
    
    let large_config = format!("[test]\n{}", 
        (0..10000).map(|i| format!("key_{}: \"value_{}\"", i, i)).collect::<Vec<_>>().join("\n")
    );
    
    // Parse small config
    let data_small = parser.parse(small_config).expect("Failed to parse small config");
    println!("Small config parsed successfully: {} bytes", std::mem::size_of_val(&data_small));
    
    // Parse large config
    let data_large = parser.parse(&large_config).expect("Failed to parse large config");
    println!("Large config parsed successfully: {} bytes", std::mem::size_of_val(&data_large));
    
    // Verify memory efficiency
    assert!(std::mem::size_of_val(&data_small) < 1000); // Small config should be efficient
    assert!(std::mem::size_of_val(&data_large) < 1000000); // Large config should be reasonable
}
```

## 🔒 Security Testing

### Input Validation Tests

```rust
use tusklang_rust::{parse, Parser, validators};
use tokio_test;

#[tokio::test]
async fn test_input_validation() {
    let mut parser = Parser::new();
    
    let tsk_content = r#"
[validation]
email: @validate.email("user@example.com")
url: @validate.url("https://example.com")
ip_address: @validate.ip("192.168.1.1")
port: @validate.range(8080, 1, 65535)
password: @validate.password("StrongPass123!")
"#;
    
    // Add custom validators
    parser.add_validator("strong_password", |password: &str| {
        password.len() >= 8 && 
        password.chars().any(|c| c.is_uppercase()) &&
        password.chars().any(|c| c.is_lowercase()) &&
        password.chars().any(|c| c.is_numeric()) &&
        password.chars().any(|c| "!@#$%^&*".contains(c))
    });
    
    let data = parser.parse(tsk_content).expect("Failed to parse");
    
    assert_eq!(data["validation"]["email"], true);
    assert_eq!(data["validation"]["url"], true);
    assert_eq!(data["validation"]["ip_address"], true);
    assert_eq!(data["validation"]["port"], true);
    assert_eq!(data["validation"]["password"], true);
}

#[tokio::test]
async fn test_sql_injection_prevention() {
    let mut parser = Parser::new();
    
    // Setup test database
    let db = SQLiteAdapter::new(":memory:").await.expect("Failed to create database");
    parser.set_database_adapter(db);
    
    // Setup test data
    db.execute(r#"
        CREATE TABLE users (id INTEGER, name TEXT, email TEXT);
        INSERT INTO users VALUES (1, 'Alice', 'alice@example.com');
    "#).await.expect("Failed to setup test data");
    
    // Test malicious input
    let malicious_input = "'; DROP TABLE users; --";
    
    let tsk_content = format!(r#"
[users]
user_data: @query("SELECT * FROM users WHERE name = ?", "{}")
"#, malicious_input);
    
    let data = parser.parse(tsk_content).await.expect("Failed to parse");
    
    // Verify table still exists
    let table_check = parser.query("SELECT name FROM sqlite_master WHERE type='table' AND name='users'").await.expect("Failed to check table");
    assert!(!table_check.is_empty(), "Table should still exist after malicious input");
    
    // Verify no data was returned for malicious input
    let user_data: Vec<serde_json::Value> = serde_json::from_value(data["users"]["user_data"].clone()).unwrap();
    assert!(user_data.is_empty(), "No data should be returned for malicious input");
}
```

### Encryption and Hashing Tests

```rust
use tusklang_rust::{parse, Parser};
use tokio_test;

#[tokio::test]
async fn test_encryption_and_hashing() {
    let mut parser = Parser::new();
    
    let tsk_content = r#"
[security]
encrypted_data: @encrypt("sensitive_data", "AES-256-GCM")
hashed_password: @hash("mysecretpassword", "bcrypt")
sha256_hash: @hash("data", "sha256")
sha512_hash: @hash("data", "sha512")
"#;
    
    let data = parser.parse(tsk_content).expect("Failed to parse");
    
    // Verify encryption
    let encrypted = data["security"]["encrypted_data"].as_str().unwrap();
    assert_ne!(encrypted, "sensitive_data"); // Should be encrypted
    assert!(encrypted.len() > 20); // Should be reasonably long
    
    // Verify hashing
    let hashed = data["security"]["hashed_password"].as_str().unwrap();
    assert_ne!(hashed, "mysecretpassword"); // Should be hashed
    assert!(hashed.starts_with("$2b$")); // Should be bcrypt format
    
    // Verify SHA hashes
    let sha256 = data["security"]["sha256_hash"].as_str().unwrap();
    let sha512 = data["security"]["sha512_hash"].as_str().unwrap();
    
    assert_eq!(sha256.len(), 64); // SHA-256 is 64 characters
    assert_eq!(sha512.len(), 128); // SHA-512 is 128 characters
}
```

## 📊 Load Testing

### Concurrent Request Testing

```rust
use tusklang_rust::{parse, Parser};
use tokio::sync::Semaphore;
use std::sync::Arc;
use tokio_test;

#[tokio::test]
async fn test_concurrent_parsing() {
    let mut parser = Parser::new();
    
    let tsk_content = r#"
[test]
value: 42
string: "hello"
boolean: true
"#;
    
    let parser = Arc::new(parser);
    let semaphore = Arc::new(Semaphore::new(100)); // Limit concurrent requests
    
    let mut handles = vec![];
    
    // Spawn 1000 concurrent parsing tasks
    for i in 0..1000 {
        let parser = Arc::clone(&parser);
        let semaphore = Arc::clone(&semaphore);
        let content = tsk_content.to_string();
        
        let handle = tokio::spawn(async move {
            let _permit = semaphore.acquire().await.unwrap();
            let data = parser.parse(&content).expect("Failed to parse");
            assert_eq!(data["test"]["value"], 42);
            i
        });
        
        handles.push(handle);
    }
    
    // Wait for all tasks to complete
    let results = futures::future::join_all(handles).await;
    
    // Verify all tasks completed successfully
    for result in results {
        assert!(result.is_ok());
    }
    
    println!("✅ All 1000 concurrent parsing tasks completed successfully");
}

#[tokio::test]
async fn test_concurrent_fujsen_execution() {
    let mut parser = Parser::new();
    
    let tsk_content = r#"
[math]
add_fujsen = '''
fn add(a: i32, b: i32) -> i32 {
    a + b
}
'''
"#;
    
    parser.parse(tsk_content).expect("Failed to parse");
    let parser = Arc::new(parser);
    let semaphore = Arc::new(Semaphore::new(50)); // Limit concurrent executions
    
    let mut handles = vec![];
    
    // Spawn 500 concurrent FUJSEN executions
    for i in 0..500 {
        let parser = Arc::clone(&parser);
        let semaphore = Arc::clone(&semaphore);
        
        let handle = tokio::spawn(async move {
            let _permit = semaphore.acquire().await.unwrap();
            let result = parser.execute_fujsen("math", "add", &[&i, &i])
                .await
                .expect("Failed to execute FUJSEN");
            assert_eq!(result, i + i);
            i
        });
        
        handles.push(handle);
    }
    
    // Wait for all tasks to complete
    let results = futures::future::join_all(handles).await;
    
    // Verify all tasks completed successfully
    for result in results {
        assert!(result.is_ok());
    }
    
    println!("✅ All 500 concurrent FUJSEN executions completed successfully");
}
```

## 🔄 Integration Test Suites

### Complete Application Testing

```rust
use tusklang_rust::{parse, Parser, adapters::sqlite::SQLiteAdapter};
use tokio_test;

#[tokio::test]
async fn test_complete_application_flow() {
    // Setup complete application environment
    let db = SQLiteAdapter::new(":memory:").await.expect("Failed to create database");
    let mut parser = Parser::new();
    parser.set_database_adapter(db);
    
    // Setup application schema
    db.execute(r#"
        CREATE TABLE users (
            id INTEGER PRIMARY KEY,
            name TEXT NOT NULL,
            email TEXT UNIQUE,
            password_hash TEXT NOT NULL,
            active BOOLEAN DEFAULT 1,
            created_at DATETIME DEFAULT CURRENT_TIMESTAMP
        );
        
        CREATE TABLE orders (
            id INTEGER PRIMARY KEY,
            user_id INTEGER,
            amount DECIMAL(10,2),
            status TEXT DEFAULT 'pending',
            created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
            FOREIGN KEY (user_id) REFERENCES users (id)
        );
    "#).await.expect("Failed to setup schema");
    
    // Test user registration flow
    let registration_tsk = r#"
[registration]
validate_user_fujsen = '''
fn validate_user(name: &str, email: &str, password: &str) -> bool {
    name.len() >= 2 && 
    email.contains("@") && 
    password.len() >= 8
}
'''

[user_creation]
create_user: @query("INSERT INTO users (name, email, password_hash) VALUES (?, ?, ?) RETURNING id", @request.name, @request.email, @hash(@request.password, "bcrypt"))
"#;
    
    parser.parse(registration_tsk).expect("Failed to parse registration config");
    
    // Test user validation
    let validation_result = parser.execute_fujsen("registration", "validate_user", &[&"Alice", &"alice@example.com", &"password123"])
        .await
        .expect("Failed to validate user");
    assert_eq!(validation_result, true);
    
    // Test invalid user
    let invalid_result = parser.execute_fujsen("registration", "validate_user", &[&"A", &"invalid-email", &"123"])
        .await
        .expect("Failed to validate user");
    assert_eq!(invalid_result, false);
    
    // Test order processing flow
    let order_tsk = r#"
[orders]
total_orders: @query("SELECT COUNT(*) FROM orders")
total_revenue: @query("SELECT SUM(amount) FROM orders WHERE status = 'completed'")
user_orders: @query("SELECT * FROM orders WHERE user_id = ?", @request.user_id)
"#;
    
    let data = parser.parse(order_tsk).await.expect("Failed to parse order config");
    
    assert_eq!(data["orders"]["total_orders"], 0);
    assert_eq!(data["orders"]["total_revenue"], 0);
    
    println!("✅ Complete application flow test passed");
}
```

## 📈 Benchmarking Tools

### Custom Benchmarking Framework

```rust
use tusklang_rust::{parse, Parser};
use std::time::{Duration, Instant};
use std::collections::HashMap;

struct BenchmarkResult {
    name: String,
    iterations: usize,
    total_time: Duration,
    avg_time: Duration,
    min_time: Duration,
    max_time: Duration,
}

struct BenchmarkSuite {
    results: Vec<BenchmarkResult>,
}

impl BenchmarkSuite {
    fn new() -> Self {
        Self { results: Vec::new() }
    }
    
    fn benchmark<F>(&mut self, name: &str, iterations: usize, mut test_fn: F)
    where
        F: FnMut() -> Result<(), Box<dyn std::error::Error>>,
    {
        let mut times = Vec::new();
        
        for _ in 0..iterations {
            let start = Instant::now();
            test_fn().expect("Benchmark test failed");
            times.push(start.elapsed());
        }
        
        let total_time: Duration = times.iter().sum();
        let avg_time = total_time / iterations as u32;
        let min_time = times.iter().min().unwrap().clone();
        let max_time = times.iter().max().unwrap().clone();
        
        self.results.push(BenchmarkResult {
            name: name.to_string(),
            iterations,
            total_time,
            avg_time,
            min_time,
            max_time,
        });
    }
    
    fn print_results(&self) {
        println!("Benchmark Results:");
        println!("{:<20} {:<10} {:<15} {:<15} {:<15} {:<15}", 
            "Test", "Iterations", "Total Time", "Avg Time", "Min Time", "Max Time");
        println!("{}", "-".repeat(90));
        
        for result in &self.results {
            println!("{:<20} {:<10} {:<15?} {:<15?} {:<15?} {:<15?}", 
                result.name, 
                result.iterations, 
                result.total_time, 
                result.avg_time, 
                result.min_time, 
                result.max_time
            );
        }
    }
}

#[tokio::test]
async fn test_benchmarking_framework() {
    let mut suite = BenchmarkSuite::new();
    let mut parser = Parser::new();
    
    let tsk_content = r#"
[test]
value: 42
string: "hello"
boolean: true
array: [1, 2, 3, 4, 5]
object: {
    key: "value"
    nested: {
        deep: "data"
    }
}
"#;
    
    // Benchmark parsing
    suite.benchmark("TSK Parsing", 1000, || {
        parser.parse(tsk_content)?;
        Ok(())
    });
    
    // Benchmark FUJSEN execution
    let fujsen_tsk = r#"
[math]
add_fujsen = '''
fn add(a: i32, b: i32) -> i32 {
    a + b
}
'''
"#;
    
    parser.parse(fujsen_tsk).expect("Failed to parse FUJSEN");
    
    suite.benchmark("FUJSEN Execution", 1000, || {
        parser.execute_fujsen("math", "add", &[&5, &3]).await?;
        Ok(())
    });
    
    // Print results
    suite.print_results();
}
```

## 🎯 What You've Learned

1. **Unit testing** - Basic parsing, syntax styles, variables, @ operators, FUJSEN
2. **Integration testing** - Database integration, cache integration, complete application flows
3. **Performance testing** - Parsing performance, FUJSEN performance, memory usage
4. **Security testing** - Input validation, SQL injection prevention, encryption and hashing
5. **Load testing** - Concurrent parsing, concurrent FUJSEN execution
6. **Benchmarking** - Custom benchmarking framework for comprehensive performance analysis

## 🚀 Next Steps

1. **Implement test suites** - Use these testing strategies in your applications
2. **Set up CI/CD** - Integrate tests into your continuous integration pipeline
3. **Monitor performance** - Use benchmarking tools to track performance over time
4. **Security audits** - Regularly test security features and validate inputs
5. **Load testing** - Test your applications under realistic load conditions

---

**You now have complete testing mastery with TuskLang Rust!** From unit tests to load testing, from security validation to performance benchmarking - TuskLang gives you the tools to build reliable, secure, and high-performance applications with confidence. 