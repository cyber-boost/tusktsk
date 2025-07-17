# 🦀 TuskLang Rust Quick Start

**"We don't bow to any king" - Rust Edition**

Get up and running with TuskLang in Rust in under 5 minutes. This guide will show you the core concepts and get you building powerful applications with zero-copy parsing and WebAssembly support.

## ⚡ Lightning Fast Setup

### 1. Add to Your Project

```bash
# Add TuskLang to your Cargo.toml
cargo add tusklang

# Or add with specific features
cargo add tusklang --features postgresql,redis,webassembly
```

### 2. Create Your First TSK File

Create `app.tsk` in your project root:

```tsk
# Your first TuskLang configuration
app_name: "MyRustApp"
version: "1.0.0"
debug: true

[server]
host: "0.0.0.0"
port: 8080
workers: @if(@env("APP_ENV") == "production", 4, 1)

[database]
host: "localhost"
port: 5432
name: "myapp"
user: "postgres"
password: @env("DB_PASSWORD", "default")

[cache]
driver: "redis"
host: "localhost"
port: 6379
ttl: "5m"
```

### 3. Parse in Rust

```rust
use tusklang_rust::{parse, parse_into, Config};
use serde::Deserialize;

#[derive(Debug, Deserialize)]
struct AppConfig {
    app_name: String,
    version: String,
    debug: bool,
    
    #[serde(rename = "server")]
    srv: ServerConfig,
    
    #[serde(rename = "database")]
    db: DatabaseConfig,
    
    #[serde(rename = "cache")]
    cache: CacheConfig,
}

#[derive(Debug, Deserialize)]
struct ServerConfig {
    host: String,
    port: u16,
    workers: u32,
}

#[derive(Debug, Deserialize)]
struct DatabaseConfig {
    host: String,
    port: u16,
    name: String,
    user: String,
    password: String,
}

#[derive(Debug, Deserialize)]
struct CacheConfig {
    driver: String,
    host: String,
    port: u16,
    ttl: String,
}

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    // Parse TSK file into strongly-typed struct
    let config: AppConfig = parse_into(include_str!("app.tsk"))?;
    
    println!("🚀 {} v{}", config.app_name, config.version);
    println!("🌐 Server: {}:{}", config.srv.host, config.srv.port);
    println!("🗄️ Database: {}:{}", config.db.host, config.db.port);
    println!("⚡ Cache: {}:{}", config.cache.host, config.cache.port);
    
    Ok(())
}
```

## 🎯 Core Concepts in 5 Minutes

### 1. Zero-Copy Parsing

```rust
use tusklang_rust::{parse, Parser};

fn main() -> Result<(), Box<dyn std::error::Error>> {
    let mut parser = Parser::new();
    
    // Zero-copy parsing - no memory allocation for strings
    let tsk_content = r#"
[user]
name: "Alice"
age: 30
active: true
"#;
    
    let data = parser.parse(tsk_content)?;
    
    // Direct access without cloning
    println!("User: {}", data["user"]["name"]);
    println!("Age: {}", data["user"]["age"]);
    
    Ok(())
}
```

### 2. Multiple Syntax Styles

```rust
use tusklang_rust::{parse, Parser};

fn main() -> Result<(), Box<dyn std::error::Error>> {
    let mut parser = Parser::new();
    
    // Mix and match syntax styles
    let tsk_content = r#"
# Traditional sections
[database]
host: "localhost"
port: 5432

# Curly brace objects
server {
    host: "0.0.0.0"
    port: 8080
}

# Angle bracket objects
cache >
    driver: "redis"
    ttl: "5m"
<
"#;
    
    let data = parser.parse(tsk_content)?;
    
    println!("Database: {}:{}", data["database"]["host"], data["database"]["port"]);
    println!("Server: {}:{}", data["server"]["host"], data["server"]["port"]);
    println!("Cache: {} (TTL: {})", data["cache"]["driver"], data["cache"]["ttl"]);
    
    Ok(())
}
```

### 3. Environment Variables

```rust
use tusklang_rust::{parse, Parser};
use std::env;

fn main() -> Result<(), Box<dyn std::error::Error>> {
    let mut parser = Parser::new();
    
    // Set environment variable
    env::set_var("APP_ENV", "production");
    env::set_var("DB_PASSWORD", "secret123");
    
    let tsk_content = r#"
$environment: @env("APP_ENV", "development")

[server]
host: "0.0.0.0"
port: @if($environment == "production", 80, 8080)
debug: @if($environment != "production", true, false)

[database]
password: @env("DB_PASSWORD", "default")
"#;
    
    let data = parser.parse(tsk_content)?;
    
    println!("Environment: {}", data["environment"]);
    println!("Server port: {}", data["server"]["port"]);
    println!("Debug mode: {}", data["server"]["debug"]);
    
    Ok(())
}
```

### 4. Database Queries in Config

```rust
use tusklang_rust::{parse, Parser};
use tusklang_rust::adapters::sqlite::SQLiteAdapter;

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    let mut parser = Parser::new();
    
    // Setup database adapter
    let db = SQLiteAdapter::new(":memory:").await?;
    db.execute(r#"
        CREATE TABLE users (id INTEGER, name TEXT, active BOOLEAN);
        INSERT INTO users VALUES (1, 'Alice', 1), (2, 'Bob', 0);
    "#).await?;
    
    parser.set_database_adapter(db);
    
    let tsk_content = r#"
[stats]
total_users: @query("SELECT COUNT(*) FROM users")
active_users: @query("SELECT COUNT(*) FROM users WHERE active = 1")
user_list: @query("SELECT name FROM users WHERE active = 1")
"#;
    
    let data = parser.parse(tsk_content).await?;
    
    println!("Total users: {}", data["stats"]["total_users"]);
    println!("Active users: {}", data["stats"]["active_users"]);
    println!("Active user names: {:?}", data["stats"]["user_list"]);
    
    Ok(())
}
```

### 5. FUJSEN - Executable Functions

```rust
use tusklang_rust::{parse, Parser};

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
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
"#;
    
    let data = parser.parse(tsk_content)?;
    
    // Execute FUJSEN functions
    let sum = parser.execute_fujsen("math", "add", &[&5, &3]).await?;
    let full_name = parser.execute_fujsen("utils", "format_name", &[&"John", &"Doe"]).await?;
    
    println!("5 + 3 = {}", sum);
    println!("Full name: {}", full_name);
    
    Ok(())
}
```

## 🔧 Web Framework Integration

### Actix-web Example

```rust
use actix_web::{web, App, HttpServer, HttpResponse, Result};
use serde::{Deserialize, Serialize};
use tusklang_rust::{parse, Parser};

#[derive(Deserialize)]
struct PaymentRequest {
    amount: f64,
    recipient: String,
}

#[derive(Serialize)]
struct PaymentResponse {
    success: bool,
    transaction_id: String,
    amount: f64,
    recipient: String,
}

async fn process_payment(
    req: web::Json<PaymentRequest>,
    parser: web::Data<Parser>,
) -> Result<HttpResponse> {
    let result = parser.execute_fujsen(
        "payment",
        "process",
        &[&req.amount, &req.recipient]
    ).await?;
    
    Ok(HttpResponse::Ok().json(result))
}

async fn get_users(parser: web::Data<Parser>) -> Result<HttpResponse> {
    let users = parser.query("SELECT * FROM users WHERE active = 1").await?;
    Ok(HttpResponse::Ok().json(users))
}

#[actix_web::main]
async fn main() -> std::io::Result<()> {
    let mut parser = Parser::new();
    let config = parser.parse_file("app.tsk").expect("Failed to parse config");
    
    let server_config = &config["server"];
    let host = server_config["host"].as_str().unwrap();
    let port = server_config["port"].as_u64().unwrap();
    
    HttpServer::new(move || {
        App::new()
            .app_data(web::Data::new(parser.clone()))
            .route("/api/users", web::get().to(get_users))
            .route("/api/payment", web::post().to(process_payment))
    })
    .bind(format!("{}:{}", host, port))?
    .run()
    .await
}
```

### Axum Example

```rust
use axum::{
    extract::Json,
    http::StatusCode,
    response::Json as JsonResponse,
    routing::{get, post},
    Router,
};
use serde::{Deserialize, Serialize};
use tusklang_rust::{parse, Parser};
use std::sync::Arc;

#[derive(Deserialize)]
struct PaymentRequest {
    amount: f64,
    recipient: String,
}

#[derive(Serialize)]
struct PaymentResponse {
    success: bool,
    transaction_id: String,
    amount: f64,
    recipient: String,
}

async fn process_payment(
    Json(req): Json<PaymentRequest>,
    axum::extract::State(parser): axum::extract::State<Arc<Parser>>,
) -> Result<JsonResponse<PaymentResponse>, StatusCode> {
    let result = parser.execute_fujsen(
        "payment",
        "process",
        &[&req.amount, &req.recipient]
    ).await
    .map_err(|_| StatusCode::INTERNAL_SERVER_ERROR)?;
    
    Ok(JsonResponse(result))
}

async fn get_users(
    axum::extract::State(parser): axum::extract::State<Arc<Parser>>,
) -> Result<JsonResponse<Vec<serde_json::Value>>, StatusCode> {
    let users = parser.query("SELECT * FROM users WHERE active = 1").await
        .map_err(|_| StatusCode::INTERNAL_SERVER_ERROR)?;
    
    Ok(JsonResponse(users))
}

#[tokio::main]
async fn main() {
    let mut parser = Parser::new();
    let config = parser.parse_file("app.tsk").expect("Failed to parse config");
    
    let app = Router::new()
        .route("/api/users", get(get_users))
        .route("/api/payment", post(process_payment))
        .with_state(Arc::new(parser));
    
    let server_config = &config["server"];
    let host = server_config["host"].as_str().unwrap();
    let port = server_config["port"].as_u64().unwrap();
    
    axum::Server::bind(&format!("{}:{}", host, port).parse().unwrap())
        .serve(app.into_make_service())
        .await
        .unwrap();
}
```

## 🧪 Testing Your Setup

### Unit Test

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
"#;
    
    let data = parser.parse(tsk_content).expect("Failed to parse");
    
    assert_eq!(data["test"]["value"], 42);
    assert_eq!(data["test"]["string"], "hello");
    assert_eq!(data["test"]["boolean"], true);
    
    println!("✅ Basic parsing test passed!");
}
```

### Integration Test

```rust
use tusklang_rust::{parse, Parser, adapters::sqlite::SQLiteAdapter};
use tokio_test;

#[tokio::test]
async fn test_database_integration() {
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
    
    let data = parser.parse(tsk_content).expect("Failed to parse");
    
    assert_eq!(data["users"]["count"], 2);
    assert_eq!(data["users"]["active_count"], 1);
    
    println!("✅ Database integration test passed!");
}
```

Run your tests:

```bash
cargo test
```

## 🚀 CLI Quick Commands

```bash
# Parse and validate
tusk parse app.tsk

# Generate Rust structs
tusk generate --type rust app.tsk

# Convert formats
tusk convert app.tsk --format json
tusk convert app.tsk --format yaml

# Interactive shell
tusk shell app.tsk

# Benchmark performance
tusk benchmark app.tsk --iterations 10000
```

## 📊 Performance Check

```rust
use std::time::Instant;
use tusklang_rust::{parse, Parser};

fn benchmark_parsing() {
    let mut parser = Parser::new();
    
    let tsk_content = r#"
[benchmark]
value: 42
string: "test"
boolean: true
"#;
    
    let start = Instant::now();
    
    for _ in 0..10000 {
        let _data = parser.parse(tsk_content).expect("Failed to parse");
    }
    
    let duration = start.elapsed();
    println!("✅ Parsed 10,000 times in {:?}", duration);
    println!("   Average: {:?} per parse", duration / 10000);
}
```

## 🎯 What You've Learned

1. **Zero-copy parsing** for maximum performance
2. **Multiple syntax styles** - choose what works for you
3. **Environment variables** with fallbacks
4. **Database queries** directly in configuration
5. **FUJSEN functions** for executable logic
6. **Web framework integration** with Actix-web and Axum
7. **Testing strategies** for reliable code
8. **CLI tools** for development workflow

## 🚀 Next Steps

1. **Explore Advanced Syntax**: Read `003-basic-syntax-rust.md`
2. **Database Integration**: See `004-database-integration-rust.md`
3. **Advanced Features**: Check `005-advanced-features-rust.md`
4. **Build Your First App**: Follow the web framework examples

---

**You're now ready to build powerful Rust applications with TuskLang!** Zero-copy parsing, WebAssembly support, and the performance you demand - all with configuration that adapts to YOUR syntax preferences. No compromises, no excuses. 