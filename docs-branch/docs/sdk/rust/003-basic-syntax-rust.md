# 🦀 TuskLang Rust Basic Syntax

**"We don't bow to any king" - Rust Edition**

Master the flexible syntax of TuskLang in Rust. From traditional INI-style to JSON-like objects and XML-inspired syntax - choose what works for you. This guide covers all the syntax styles and features you need to build powerful configurations.

## 🎨 Syntax Flexibility

### 1. Traditional INI-Style (Default)

```rust
use tusklang_rust::{parse, Parser};

fn main() -> Result<(), Box<dyn std::error::Error>> {
    let mut parser = Parser::new();
    
    let tsk_content = r#"
# Application configuration
app_name: "MyRustApp"
version: "1.0.0"
debug: true

[database]
host: "localhost"
port: 5432
name: "myapp"
user: "postgres"
password: "secret"

[server]
host: "0.0.0.0"
port: 8080
ssl: false
"#;
    
    let data = parser.parse(tsk_content)?;
    
    println!("App: {} v{}", data["app_name"], data["version"]);
    println!("Database: {}:{}", data["database"]["host"], data["database"]["port"]);
    println!("Server: {}:{}", data["server"]["host"], data["server"]["port"]);
    
    Ok(())
}
```

### 2. Curly Brace Objects (JSON-like)

```rust
use tusklang_rust::{parse, Parser};

fn main() -> Result<(), Box<dyn std::error::Error>> {
    let mut parser = Parser::new();
    
    let tsk_content = r#"
app {
    name: "MyRustApp"
    version: "1.0.0"
    debug: true
}

database {
    host: "localhost"
    port: 5432
    name: "myapp"
    user: "postgres"
    password: "secret"
}

server {
    host: "0.0.0.0"
    port: 8080
    ssl: false
}
"#;
    
    let data = parser.parse(tsk_content)?;
    
    println!("App: {} v{}", data["app"]["name"], data["app"]["version"]);
    println!("Database: {}:{}", data["database"]["host"], data["database"]["port"]);
    println!("Server: {}:{}", data["server"]["host"], data["server"]["port"]);
    
    Ok(())
}
```

### 3. Angle Bracket Objects (XML-inspired)

```rust
use tusklang_rust::{parse, Parser};

fn main() -> Result<(), Box<dyn std::error::Error>> {
    let mut parser = Parser::new();
    
    let tsk_content = r#"
app >
    name: "MyRustApp"
    version: "1.0.0"
    debug: true
<

database >
    host: "localhost"
    port: 5432
    name: "myapp"
    user: "postgres"
    password: "secret"
<

server >
    host: "0.0.0.0"
    port: 8080
    ssl: false
<
"#;
    
    let data = parser.parse(tsk_content)?;
    
    println!("App: {} v{}", data["app"]["name"], data["app"]["version"]);
    println!("Database: {}:{}", data["database"]["host"], data["database"]["port"]);
    println!("Server: {}:{}", data["server"]["host"], data["server"]["port"]);
    
    Ok(())
}
```

### 4. Mixed Syntax Styles

```rust
use tusklang_rust::{parse, Parser};

fn main() -> Result<(), Box<dyn std::error::Error>> {
    let mut parser = Parser::new();
    
    let tsk_content = r#"
# Global variables
$app_name: "MyRustApp"
$version: "1.0.0"

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

# Nested structures
features {
    auth {
        enabled: true
        provider: "jwt"
    }
    
    api {
        rate_limit: 1000
        timeout: "30s"
    }
}
"#;
    
    let data = parser.parse(tsk_content)?;
    
    println!("App: {} v{}", data["app_name"], data["version"]);
    println!("Database: {}:{}", data["database"]["host"], data["database"]["port"]);
    println!("Server: {}:{}", data["server"]["host"], data["server"]["port"]);
    println!("Cache: {} (TTL: {})", data["cache"]["driver"], data["cache"]["ttl"]);
    println!("Auth enabled: {}", data["features"]["auth"]["enabled"]);
    
    Ok(())
}
```

## 🔤 Data Types

### 1. Basic Types

```rust
use tusklang_rust::{parse, Parser};

fn main() -> Result<(), Box<dyn std::error::Error>> {
    let mut parser = Parser::new();
    
    let tsk_content = r#"
[types]
# Strings
string_value: "Hello, TuskLang!"
multiline_string: """
This is a
multiline string
"""

# Numbers
integer: 42
float: 3.14159
negative: -123
scientific: 1.23e-4

# Booleans
boolean_true: true
boolean_false: false

# Null/None
null_value: null
"#;
    
    let data = parser.parse(tsk_content)?;
    
    println!("String: {}", data["types"]["string_value"]);
    println!("Integer: {}", data["types"]["integer"]);
    println!("Float: {}", data["types"]["float"]);
    println!("Boolean: {}", data["types"]["boolean_true"]);
    println!("Null: {:?}", data["types"]["null_value"]);
    
    Ok(())
}
```

### 2. Arrays

```rust
use tusklang_rust::{parse, Parser};

fn main() -> Result<(), Box<dyn std::error::Error>> {
    let mut parser = Parser::new();
    
    let tsk_content = r#"
[arrays]
# Simple arrays
numbers: [1, 2, 3, 4, 5]
strings: ["apple", "banana", "cherry"]
mixed: [1, "hello", true, 3.14]

# Nested arrays
matrix: [
    [1, 2, 3],
    [4, 5, 6],
    [7, 8, 9]
]

# Arrays with objects
users: [
    {name: "Alice", age: 30},
    {name: "Bob", age: 25},
    {name: "Charlie", age: 35}
]
"#;
    
    let data = parser.parse(tsk_content)?;
    
    println!("Numbers: {:?}", data["arrays"]["numbers"]);
    println!("Strings: {:?}", data["arrays"]["strings"]);
    println!("Mixed: {:?}", data["arrays"]["mixed"]);
    println!("Matrix: {:?}", data["arrays"]["matrix"]);
    println!("Users: {:?}", data["arrays"]["users"]);
    
    Ok(())
}
```

### 3. Objects and Nested Structures

```rust
use tusklang_rust::{parse, Parser};

fn main() -> Result<(), Box<dyn std::error::Error>> {
    let mut parser = Parser::new();
    
    let tsk_content = r#"
[objects]
# Simple object
user {
    name: "Alice"
    age: 30
    email: "alice@example.com"
}

# Nested objects
company {
    name: "TechCorp"
    address {
        street: "123 Main St"
        city: "San Francisco"
        state: "CA"
        zip: "94105"
    }
    employees: [
        {name: "Alice", role: "Engineer"},
        {name: "Bob", role: "Designer"}
    ]
}

# Complex nested structure
api {
    endpoints {
        users {
            get: "/api/users"
            post: "/api/users"
            put: "/api/users/{id}"
            delete: "/api/users/{id}"
        }
        auth {
            login: "/api/auth/login"
            logout: "/api/auth/logout"
            refresh: "/api/auth/refresh"
        }
    }
    rate_limits {
        default: 1000
        auth: 100
        upload: 10
    }
}
"#;
    
    let data = parser.parse(tsk_content)?;
    
    println!("User: {} ({})", data["objects"]["user"]["name"], data["objects"]["user"]["age"]);
    println!("Company: {}", data["objects"]["company"]["name"]);
    println!("Address: {} {}, {}", 
        data["objects"]["company"]["address"]["street"],
        data["objects"]["company"]["address"]["city"],
        data["objects"]["company"]["address"]["state"]
    );
    println!("API endpoint: {}", data["objects"]["api"]["endpoints"]["users"]["get"]);
    
    Ok(())
}
```

## 🔗 Variables and Interpolation

### 1. Global Variables

```rust
use tusklang_rust::{parse, Parser};

fn main() -> Result<(), Box<dyn std::error::Error>> {
    let mut parser = Parser::new();
    
    let tsk_content = r#"
# Global variables (prefixed with $)
$app_name: "MyRustApp"
$version: "1.0.0"
$environment: "production"

[config]
name: $app_name
version: $version
env: $environment

[paths]
log_file: "/var/log/${app_name}.log"
config_file: "/etc/${app_name}/config.json"
data_dir: "/var/lib/${app_name}/v${version}"
"#;
    
    let data = parser.parse(tsk_content)?;
    
    println!("App: {} v{}", data["config"]["name"], data["config"]["version"]);
    println!("Log file: {}", data["paths"]["log_file"]);
    println!("Config file: {}", data["paths"]["config_file"]);
    println!("Data directory: {}", data["paths"]["data_dir"]);
    
    Ok(())
}
```

### 2. Cross-Reference Variables

```rust
use tusklang_rust::{parse, Parser};

fn main() -> Result<(), Box<dyn std::error::Error>> {
    let mut parser = Parser::new();
    
    let tsk_content = r#"
[database]
host: "localhost"
port: 5432
name: "myapp"

[server]
host: "0.0.0.0"
port: 8080

[connections]
db_url: "postgresql://${database.user}:${database.password}@${database.host}:${database.port}/${database.name}"
api_url: "http://${server.host}:${server.port}/api"
"#;
    
    let data = parser.parse(tsk_content)?;
    
    println!("Database URL: {}", data["connections"]["db_url"]);
    println!("API URL: {}", data["connections"]["api_url"]);
    
    Ok(())
}
```

## ⚡ @ Operator System

### 1. Environment Variables

```rust
use tusklang_rust::{parse, Parser};
use std::env;

fn main() -> Result<(), Box<dyn std::error::Error>> {
    let mut parser = Parser::new();
    
    // Set environment variables
    env::set_var("APP_ENV", "production");
    env::set_var("DB_PASSWORD", "secret123");
    env::set_var("API_KEY", "abc123");
    
    let tsk_content = r#"
$environment: @env("APP_ENV", "development")

[config]
env: $environment
debug: @if($environment == "production", false, true)

[secrets]
db_password: @env("DB_PASSWORD")
api_key: @env("API_KEY")
missing_var: @env("MISSING_VAR", "default_value")
"#;
    
    let data = parser.parse(tsk_content)?;
    
    println!("Environment: {}", data["config"]["env"]);
    println!("Debug mode: {}", data["config"]["debug"]);
    println!("DB Password: {}", data["secrets"]["db_password"]);
    println!("API Key: {}", data["secrets"]["api_key"]);
    println!("Missing var: {}", data["secrets"]["missing_var"]);
    
    Ok(())
}
```

### 2. Date and Time Operations

```rust
use tusklang_rust::{parse, Parser};

fn main() -> Result<(), Box<dyn std::error::Error>> {
    let mut parser = Parser::new();
    
    let tsk_content = r#"
[timestamps]
current_time: @date.now()
formatted_date: @date("Y-m-d H:i:s")
iso_date: @date("c")
unix_timestamp: @date("U")

[expiry]
token_expiry: @date.add("+1 hour")
session_expiry: @date.add("+24 hours")
backup_expiry: @date.add("+30 days")
"#;
    
    let data = parser.parse(tsk_content)?;
    
    println!("Current time: {}", data["timestamps"]["current_time"]);
    println!("Formatted date: {}", data["timestamps"]["formatted_date"]);
    println!("Token expiry: {}", data["expiry"]["token_expiry"]);
    println!("Session expiry: {}", data["expiry"]["session_expiry"]);
    
    Ok(())
}
```

### 3. Conditional Logic

```rust
use tusklang_rust::{parse, Parser};
use std::env;

fn main() -> Result<(), Box<dyn std::error::Error>> {
    let mut parser = Parser::new();
    
    // Set environment variable
    env::set_var("APP_ENV", "production");
    
    let tsk_content = r#"
$environment: @env("APP_ENV", "development")

[server]
host: "0.0.0.0"
port: @if($environment == "production", 80, 8080)
workers: @if($environment == "production", 4, 1)
debug: @if($environment != "production", true, false)

[logging]
level: @if($environment == "production", "error", "debug")
format: @if($environment == "production", "json", "text")
file: @if($environment == "production", "/var/log/app.log", "console")

[security]
ssl: @if($environment == "production", true, false)
cors: @if($environment == "production", {
    origin: ["https://myapp.com"],
    credentials: true
}, {
    origin: "*",
    credentials: false
})
"#;
    
    let data = parser.parse(tsk_content)?;
    
    println!("Server port: {}", data["server"]["port"]);
    println!("Workers: {}", data["server"]["workers"]);
    println!("Debug mode: {}", data["server"]["debug"]);
    println!("Log level: {}", data["logging"]["level"]);
    println!("SSL enabled: {}", data["security"]["ssl"]);
    
    Ok(())
}
```

### 4. Mathematical Operations

```rust
use tusklang_rust::{parse, Parser};

fn main() -> Result<(), Box<dyn std::error::Error>> {
    let mut parser = Parser::new();
    
    let tsk_content = r#"
[math]
addition: @math.add(5, 3)
subtraction: @math.sub(10, 4)
multiplication: @math.mul(6, 7)
division: @math.div(20, 4)
modulo: @math.mod(17, 5)
power: @math.pow(2, 8)

[calculations]
total: @math.add(@math.mul(5, 10), @math.div(100, 4))
percentage: @math.mul(@math.div(25, 100), 200)
average: @math.div(@math.add(10, 20, 30), 3)
"#;
    
    let data = parser.parse(tsk_content)?;
    
    println!("5 + 3 = {}", data["math"]["addition"]);
    println!("10 - 4 = {}", data["math"]["subtraction"]);
    println!("6 × 7 = {}", data["math"]["multiplication"]);
    println!("20 ÷ 4 = {}", data["math"]["division"]);
    println!("17 % 5 = {}", data["math"]["modulo"]);
    println!("2^8 = {}", data["math"]["power"]);
    println!("Total: {}", data["calculations"]["total"]);
    
    Ok(())
}
```

## 🔒 Validation and Security

### 1. Input Validation

```rust
use tusklang_rust::{parse, Parser, validators};

fn main() -> Result<(), Box<dyn std::error::Error>> {
    let mut parser = Parser::new();
    
    let tsk_content = r#"
[validation]
email: @validate.email("user@example.com")
url: @validate.url("https://example.com")
ip_address: @validate.ip("192.168.1.1")
port: @validate.range(8080, 1, 65535)
password: @validate.password("StrongPass123!")
"#;
    
    // Custom validators
    parser.add_validator("strong_password", |password: &str| {
        password.len() >= 8 && 
        password.chars().any(|c| c.is_uppercase()) &&
        password.chars().any(|c| c.is_lowercase()) &&
        password.chars().any(|c| c.is_numeric()) &&
        password.chars().any(|c| "!@#$%^&*".contains(c))
    });
    
    let data = parser.parse(tsk_content)?;
    
    println!("Email valid: {}", data["validation"]["email"]);
    println!("URL valid: {}", data["validation"]["url"]);
    println!("IP valid: {}", data["validation"]["ip_address"]);
    println!("Port valid: {}", data["validation"]["port"]);
    println!("Password valid: {}", data["validation"]["password"]);
    
    Ok(())
}
```

### 2. Encryption and Security

```rust
use tusklang_rust::{parse, Parser};

fn main() -> Result<(), Box<dyn std::error::Error>> {
    let mut parser = Parser::new();
    
    let tsk_content = r#"
[security]
# Encrypt sensitive data
encrypted_password: @encrypt("mysecretpassword", "AES-256-GCM")
encrypted_api_key: @encrypt("abc123def456", "AES-256-GCM")

# Hash passwords
hashed_password: @hash("mysecretpassword", "bcrypt")

# Secure environment variables
secure_api_key: @env.secure("API_KEY")
secure_db_password: @env.secure("DB_PASSWORD")
"#;
    
    let data = parser.parse(tsk_content)?;
    
    println!("Encrypted password: {}", data["security"]["encrypted_password"]);
    println!("Hashed password: {}", data["security"]["hashed_password"]);
    println!("Secure API key: {}", data["security"]["secure_api_key"]);
    
    Ok(())
}
```

## 🧪 Testing Your Syntax

### Syntax Validation Test

```rust
use tusklang_rust::{parse, Parser};

#[tokio::test]
async fn test_syntax_styles() {
    let mut parser = Parser::new();
    
    // Test traditional syntax
    let traditional = r#"
[test]
value: 42
string: "hello"
"#;
    
    let data1 = parser.parse(traditional).expect("Failed to parse traditional");
    assert_eq!(data1["test"]["value"], 42);
    
    // Test curly brace syntax
    let curly = r#"
test {
    value: 42
    string: "hello"
}
"#;
    
    let data2 = parser.parse(curly).expect("Failed to parse curly");
    assert_eq!(data2["test"]["value"], 42);
    
    // Test angle bracket syntax
    let angle = r#"
test >
    value: 42
    string: "hello"
<
"#;
    
    let data3 = parser.parse(angle).expect("Failed to parse angle");
    assert_eq!(data3["test"]["value"], 42);
    
    println!("✅ All syntax styles work correctly!");
}
```

### Complex Structure Test

```rust
use tusklang_rust::{parse, Parser};

#[tokio::test]
async fn test_complex_structures() {
    let mut parser = Parser::new();
    
    let tsk_content = r#"
$app_name: "TestApp"

app {
    name: $app_name
    version: "1.0.0"
    features: ["auth", "api", "cache"]
}

database {
    host: "localhost"
    port: 5432
    credentials {
        user: "postgres"
        password: @env("DB_PASSWORD", "default")
    }
}

api {
    endpoints: [
        {path: "/users", method: "GET"},
        {path: "/users", method: "POST"},
        {path: "/users/{id}", method: "PUT"}
    ]
    rate_limit: @if(@env("APP_ENV") == "production", 1000, 10000)
}
"#;
    
    let data = parser.parse(tsk_content).expect("Failed to parse complex structure");
    
    assert_eq!(data["app"]["name"], "TestApp");
    assert_eq!(data["app"]["version"], "1.0.0");
    assert_eq!(data["database"]["host"], "localhost");
    assert_eq!(data["database"]["port"], 5432);
    assert_eq!(data["api"]["endpoints"][0]["path"], "/users");
    
    println!("✅ Complex structure parsing works!");
}
```

## 🚀 CLI Syntax Tools

```bash
# Validate syntax
tusk validate config.tsk

# Format TSK file
tusk format config.tsk

# Convert between syntax styles
tusk convert config.tsk --style curly
tusk convert config.tsk --style angle
tusk convert config.tsk --style traditional

# Syntax highlighting
tusk highlight config.tsk

# Syntax check with detailed errors
tusk check config.tsk --verbose
```

## 📊 Performance Comparison

```rust
use std::time::Instant;
use tusklang_rust::{parse, Parser};

fn benchmark_syntax_styles() {
    let mut parser = Parser::new();
    
    let traditional = r#"
[test]
value: 42
string: "hello"
boolean: true
"#;
    
    let curly = r#"
test {
    value: 42
    string: "hello"
    boolean: true
}
"#;
    
    let angle = r#"
test >
    value: 42
    string: "hello"
    boolean: true
<
"#;
    
    let iterations = 10000;
    
    // Benchmark traditional
    let start = Instant::now();
    for _ in 0..iterations {
        let _data = parser.parse(traditional).expect("Failed to parse");
    }
    let traditional_time = start.elapsed();
    
    // Benchmark curly
    let start = Instant::now();
    for _ in 0..iterations {
        let _data = parser.parse(curly).expect("Failed to parse");
    }
    let curly_time = start.elapsed();
    
    // Benchmark angle
    let start = Instant::now();
    for _ in 0..iterations {
        let _data = parser.parse(angle).expect("Failed to parse");
    }
    let angle_time = start.elapsed();
    
    println!("Traditional: {:?}", traditional_time);
    println!("Curly: {:?}", curly_time);
    println!("Angle: {:?}", angle_time);
}
```

## 🎯 What You've Learned

1. **Multiple syntax styles** - Traditional, curly brace, and angle bracket
2. **Data types** - Strings, numbers, booleans, arrays, objects
3. **Variables and interpolation** - Global variables and cross-references
4. **@ Operator system** - Environment variables, dates, conditionals, math
5. **Validation and security** - Input validation and encryption
6. **Testing strategies** - Syntax validation and complex structure testing
7. **Performance considerations** - Benchmarking different syntax styles

## 🚀 Next Steps

1. **Database Integration**: Read `004-database-integration-rust.md`
2. **Advanced Features**: See `005-advanced-features-rust.md`
3. **Web Framework Integration**: Check the examples in the quick start
4. **Build Your Application**: Start with the syntax that feels most natural

---

**You now have complete mastery of TuskLang syntax in Rust!** Choose your preferred style, mix and match as needed, and build configurations that adapt to YOUR workflow. No more syntax constraints - just pure flexibility and power. 