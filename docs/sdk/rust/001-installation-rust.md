# 🦀 TuskLang Rust Installation Guide

**"We don't bow to any king" - Rust Edition**

Welcome to the TuskLang Rust SDK - where configuration meets zero-copy performance, WebAssembly support, and uncompromising speed. This guide will get you up and running with TuskLang in your Rust projects.

## 🚀 Quick Installation

### Method 1: Cargo Add (Recommended)

```bash
# Add to your Cargo.toml dependencies
cargo add tusklang

# Install CLI tool globally
cargo install tusklang-cli

# Verify installation
tusk --version
```

### Method 2: One-Line Install

```bash
# Direct install script
curl -sSL https://rust.tusklang.org | bash

# Alternative with wget
wget -qO- https://rust.tusklang.org | bash
```

### Method 3: Manual Installation

```bash
# Clone the repository
git clone https://github.com/cyber-boost/rust.git
cd rust

# Build and install
cargo build --release
cargo install --path .

# Verify installation
tusk --help
```

## 📦 Cargo.toml Configuration

### Basic Setup

```toml
[dependencies]
tusklang = "1.0"
serde = { version = "1.0", features = ["derive"] }
tokio = { version = "1.0", features = ["full"] }

[dev-dependencies]
tokio-test = "0.4"
```

### Advanced Setup with Features

```toml
[dependencies]
tusklang = { version = "1.0", features = [
    "webassembly",
    "postgresql", 
    "mysql",
    "mongodb",
    "redis",
    "actix-web",
    "axum",
    "rocket"
] }
serde = { version = "1.0", features = ["derive"] }
tokio = { version = "1.0", features = ["full"] }
actix-web = "4.0"
axum = "0.6"
rocket = "0.5"

[dev-dependencies]
tokio-test = "0.4"
```

## 🔧 Environment Setup

### Prerequisites

```bash
# Ensure you have Rust 1.70+ installed
rustc --version

# Update Rust if needed
rustup update

# Install build tools
rustup component add rust-src
rustup component add rust-analysis
```

### Development Tools

```bash
# Install useful development tools
cargo install cargo-watch
cargo install cargo-audit
cargo install cargo-tarpaulin

# Install TuskLang development tools
cargo install tusklang-cli
cargo install tusklang-test
```

## 🎯 First Steps

### 1. Create Your First TSK File

Create `config.tsk` in your project root:

```tsk
# Application configuration
app_name: "MyRustApp"
version: "1.0.0"
debug: true

[server]
host: "0.0.0.0"
port: 8080
workers: 4

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

### 2. Basic Rust Integration

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
    let config: AppConfig = parse_into(include_str!("config.tsk"))?;
    
    println!("🚀 Starting {} v{}", config.app_name, config.version);
    println!("🌐 Server: {}:{}", config.srv.host, config.srv.port);
    println!("🗄️ Database: {}:{}", config.db.host, config.db.port);
    println!("⚡ Cache: {}:{}", config.cache.host, config.cache.port);
    
    Ok(())
}
```

### 3. CLI Usage

```bash
# Parse and validate your config
tusk parse config.tsk

# Generate Rust structs from TSK
tusk generate --type rust config.tsk

# Convert to other formats
tusk convert config.tsk --format json
tusk convert config.tsk --format yaml

# Interactive shell
tusk shell config.tsk

# Benchmark parsing performance
tusk benchmark config.tsk --iterations 10000
```

## 🔍 Verification

### Test Your Installation

```rust
use tusklang_rust::{parse, Parser};

#[tokio::test]
async fn test_installation() {
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
    
    println!("✅ TuskLang Rust installation verified!");
}
```

Run the test:

```bash
cargo test test_installation
```

## 🛠️ IDE Setup

### VS Code Configuration

Create `.vscode/settings.json`:

```json
{
    "rust-analyzer.cargo.features": "all",
    "rust-analyzer.checkOnSave.command": "clippy",
    "files.associations": {
        "*.tsk": "ini"
    },
    "emmet.includeLanguages": {
        "tsk": "ini"
    }
}
```

### Recommended Extensions

- **rust-analyzer**: Rust language support
- **crates**: Cargo.toml dependency management
- **Better TOML**: TOML file support
- **INI**: INI file syntax highlighting

## 🔧 Troubleshooting

### Common Issues

#### 1. Compilation Errors

```bash
# Clean and rebuild
cargo clean
cargo build

# Check Rust version
rustc --version

# Update dependencies
cargo update
```

#### 2. Feature Flag Issues

```toml
# Ensure you have the right features enabled
[dependencies]
tusklang = { version = "1.0", features = ["postgresql"] }
```

#### 3. Database Connection Issues

```rust
// Test database connectivity
use tusklang_rust::adapters::sqlite::SQLiteAdapter;

#[tokio::test]
async fn test_db_connection() {
    let db = SQLiteAdapter::new(":memory:").await.expect("Failed to connect");
    let result = db.query("SELECT 1", &[]).await.expect("Failed to query");
    assert_eq!(result[0]["1"], 1);
}
```

### Debug Mode

```rust
use tusklang_rust::{parse, Parser};

fn main() -> Result<(), Box<dyn std::error::Error>> {
    // Enable debug logging
    let mut parser = Parser::new();
    parser.set_debug(true);
    
    let config = parser.parse_file("config.tsk")?;
    println!("Config: {:?}", config);
    
    Ok(())
}
```

## 📊 Performance Verification

### Benchmark Your Setup

```bash
# Run performance benchmarks
tusk benchmark config.tsk --iterations 10000

# Expected results on modern hardware:
# - Simple config (1KB): < 0.1ms
# - Complex config (10KB): < 0.5ms
# - Large config (100KB): < 3ms
```

### Memory Usage Check

```rust
use std::alloc::{alloc, dealloc, Layout};

fn check_memory_usage() {
    let layout = Layout::new::<tusklang_rust::Parser>();
    let ptr = unsafe { alloc(layout) };
    
    if !ptr.is_null() {
        println!("✅ Memory allocation successful");
        unsafe { dealloc(ptr, layout) };
    }
}
```

## 🚀 Next Steps

1. **Explore Basic Syntax**: Read `002-quick-start-rust.md`
2. **Learn Advanced Features**: Check `003-basic-syntax-rust.md`
3. **Database Integration**: See `004-database-integration-rust.md`
4. **Web Framework Setup**: Review `005-advanced-features-rust.md`

## 📚 Resources

- **Official Documentation**: [docs.tusklang.org/rust](https://docs.tusklang.org/rust)
- **GitHub Repository**: [github.com/tusklang/rust](https://github.com/cyber-boost/rust)
- **Crates.io**: [crates.io/crates/tusklang](https://crates.io/crates/tusklang)
- **Examples**: [examples.tusklang.org/rust](https://examples.tusklang.org/rust)
- **Community**: [discord.gg/tusklang](https://discord.gg/tusklang)

---

**Ready to revolutionize your Rust configuration?** TuskLang gives you zero-copy parsing, WebAssembly support, and the performance you demand. No compromises, no excuses - just pure Rust power with configuration that adapts to YOUR syntax preferences. 