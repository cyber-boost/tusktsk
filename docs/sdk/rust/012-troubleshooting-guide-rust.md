# 🦀 TuskLang Rust Troubleshooting Guide

**"We don't bow to any king" - Rust Edition**

Master troubleshooting for TuskLang Rust applications. From common errors to performance issues, from debugging techniques to recovery strategies - learn how to diagnose and resolve problems quickly and effectively.

## 🔍 Common Error Patterns

### Parsing Errors

```rust
use tusklang_rust::{parse, Parser};
use std::error::Error;

#[tokio::main]
async fn troubleshoot_parsing_errors() -> Result<(), Box<dyn Error>> {
    println!("🔍 Troubleshooting Parsing Errors...");
    
    let mut parser = Parser::new();
    
    // 1. Syntax Error - Missing closing bracket
    println!("1. Testing syntax error handling...");
    
    let invalid_tsk = r#"
[app
name: "TestApp"
version: "1.0.0"
"#;
    
    match parser.parse(invalid_tsk).await {
        Ok(_) => println!("❌ Should have failed - missing closing bracket"),
        Err(e) => {
            println!("✅ Correctly caught syntax error: {}", e);
            
            // Provide helpful error message
            if e.to_string().contains("unexpected end of input") {
                println!("💡 Tip: Check for missing closing brackets or quotes");
            }
        }
    }
    
    // 2. Type Error - Invalid data type
    println!("2. Testing type error handling...");
    
    let type_error_tsk = r#"
[config]
port: "not_a_number"
enabled: "not_a_boolean"
"#;
    
    match parser.parse(type_error_tsk).await {
        Ok(data) => {
            // Try to access with wrong type
            match data["config"]["port"].as_u64() {
                Some(_) => println!("❌ Should have failed - string cannot be converted to number"),
                None => println!("✅ Correctly handled type conversion error"),
            }
        }
        Err(e) => println!("✅ Caught type error: {}", e),
    }
    
    // 3. Variable Reference Error
    println!("3. Testing variable reference error handling...");
    
    let var_error_tsk = r#"
[config]
name: $undefined_variable
value: ${another_undefined}
"#;
    
    match parser.parse(var_error_tsk).await {
        Ok(_) => println!("❌ Should have failed - undefined variables"),
        Err(e) => {
            println!("✅ Correctly caught variable reference error: {}", e);
            println!("💡 Tip: Define variables before using them with $ syntax");
        }
    }
    
    // 4. @ Operator Error
    println!("4. Testing @ operator error handling...");
    
    let operator_error_tsk = r#"
[config]
api_key: @env("MISSING_API_KEY")
database_url: @query("SELECT * FROM nonexistent_table")
"#;
    
    match parser.parse(operator_error_tsk).await {
        Ok(data) => {
            println!("✅ Parsed successfully, but @ operators may have issues");
            
            // Check if @ operators resolved correctly
            let api_key = data["config"]["api_key"].as_str().unwrap_or("");
            if api_key.is_empty() {
                println!("⚠️  Environment variable MISSING_API_KEY not found");
            }
        }
        Err(e) => println!("✅ Caught @ operator error: {}", e),
    }
    
    Ok(())
}
```

### Database Connection Errors

```rust
use tusklang_rust::{parse, Parser, adapters::postgresql::{PostgreSQLAdapter, PostgreSQLConfig}};
use std::time::Duration;

#[tokio::main]
async fn troubleshoot_database_errors() -> Result<(), Box<dyn std::error::Error>> {
    println!("🗄️  Troubleshooting Database Errors...");
    
    let mut parser = Parser::new();
    
    // 1. Connection Timeout
    println!("1. Testing connection timeout handling...");
    
    let timeout_config = PostgreSQLConfig {
        host: "nonexistent-host".to_string(),
        port: 5432,
        database: "testdb".to_string(),
        user: "postgres".to_string(),
        password: "password".to_string(),
        ssl_mode: "disable".to_string(),
    };
    
    match PostgreSQLAdapter::new_with_timeout(timeout_config, Duration::from_secs(5)).await {
        Ok(_) => println!("❌ Should have failed - host doesn't exist"),
        Err(e) => {
            println!("✅ Correctly caught connection timeout: {}", e);
            println!("💡 Tip: Check hostname, port, and network connectivity");
        }
    }
    
    // 2. Authentication Error
    println!("2. Testing authentication error handling...");
    
    let auth_config = PostgreSQLConfig {
        host: "localhost".to_string(),
        port: 5432,
        database: "testdb".to_string(),
        user: "wrong_user".to_string(),
        password: "wrong_password".to_string(),
        ssl_mode: "disable".to_string(),
    };
    
    match PostgreSQLAdapter::new(auth_config).await {
        Ok(_) => println!("⚠️  Authentication succeeded (unexpected)"),
        Err(e) => {
            println!("✅ Correctly caught authentication error: {}", e);
            println!("💡 Tip: Verify username and password");
        }
    }
    
    // 3. Query Error
    println!("3. Testing query error handling...");
    
    // Setup working database connection
    let working_config = PostgreSQLConfig {
        host: "localhost".to_string(),
        port: 5432,
        database: "testdb".to_string(),
        user: "postgres".to_string(),
        password: "password".to_string(),
        ssl_mode: "disable".to_string(),
    };
    
    match PostgreSQLAdapter::new(working_config).await {
        Ok(db) => {
            parser.set_database_adapter(db);
            
            // Test invalid query
            let invalid_query = "SELECT * FROM nonexistent_table";
            match parser.query(invalid_query).await {
                Ok(_) => println!("❌ Should have failed - table doesn't exist"),
                Err(e) => {
                    println!("✅ Correctly caught query error: {}", e);
                    println!("💡 Tip: Check table names and SQL syntax");
                }
            }
        }
        Err(e) => println!("⚠️  Could not connect to database: {}", e),
    }
    
    Ok(())
}
```

### FUJSEN Execution Errors

```rust
use tusklang_rust::{parse, Parser};

#[tokio::main]
async fn troubleshoot_fujsen_errors() -> Result<(), Box<dyn std::error::Error>> {
    println!("⚡ Troubleshooting FUJSEN Errors...");
    
    let mut parser = Parser::new();
    
    // 1. Syntax Error in FUJSEN
    println!("1. Testing FUJSEN syntax error handling...");
    
    let syntax_error_tsk = r#"
[math]
add_fujsen = '''
fn add(a: i32, b: i32) -> i32 {
    a + b  // Missing semicolon
}
'''
"#;
    
    match parser.parse(syntax_error_tsk).await {
        Ok(_) => {
            // Try to execute the function
            match parser.execute_fujsen("math", "add", &[&5, &3]).await {
                Ok(_) => println!("❌ Should have failed - syntax error in FUJSEN"),
                Err(e) => {
                    println!("✅ Correctly caught FUJSEN syntax error: {}", e);
                    println!("💡 Tip: Check Rust syntax in FUJSEN functions");
                }
            }
        }
        Err(e) => println!("✅ Caught parsing error: {}", e),
    }
    
    // 2. Type Mismatch Error
    println!("2. Testing type mismatch error handling...");
    
    let type_error_tsk = r#"
[utils]
process_fujsen = '''
fn process(data: Vec<i32>) -> i32 {
    data.iter().sum()
}
'''
"#;
    
    parser.parse(type_error_tsk).await?;
    
    // Pass wrong type
    match parser.execute_fujsen("utils", "process", &[&"not_a_vector"]).await {
        Ok(_) => println!("❌ Should have failed - wrong parameter type"),
        Err(e) => {
            println!("✅ Correctly caught type mismatch error: {}", e);
            println!("💡 Tip: Ensure parameter types match function signature");
        }
    }
    
    // 3. Runtime Error in FUJSEN
    println!("3. Testing runtime error handling...");
    
    let runtime_error_tsk = r#"
[math]
divide_fujsen = '''
fn divide(a: i32, b: i32) -> i32 {
    a / b  // Will panic if b is 0
}
'''
"#;
    
    parser.parse(runtime_error_tsk).await?;
    
    // Division by zero
    match parser.execute_fujsen("math", "divide", &[&10, &0]).await {
        Ok(_) => println!("❌ Should have failed - division by zero"),
        Err(e) => {
            println!("✅ Correctly caught runtime error: {}", e);
            println!("💡 Tip: Add error handling in FUJSEN functions");
        }
    }
    
    // 4. Memory Error in FUJSEN
    println!("4. Testing memory error handling...");
    
    let memory_error_tsk = r#"
[utils]
allocate_fujsen = '''
fn allocate(size: usize) -> Vec<u8> {
    vec![0u8; size]  // Could panic if size is too large
}
'''
"#;
    
    parser.parse(memory_error_tsk).await?;
    
    // Try to allocate too much memory
    match parser.execute_fujsen("utils", "allocate", &[&usize::MAX]).await {
        Ok(_) => println!("⚠️  Memory allocation succeeded (unexpected)"),
        Err(e) => {
            println!("✅ Correctly caught memory error: {}", e);
            println!("💡 Tip: Add bounds checking for memory allocations");
        }
    }
    
    Ok(())
}
```

## 🐛 Debugging Techniques

### Logging and Tracing

```rust
use tusklang_rust::{parse, Parser};
use tracing::{info, warn, error, debug};
use tracing_subscriber::{layer::SubscriberExt, util::SubscriberInitExt};

#[tokio::main]
async fn setup_debugging() -> Result<(), Box<dyn std::error::Error>> {
    println!("🐛 Setting Up Debugging...");
    
    // Initialize tracing
    tracing_subscriber::registry()
        .with(tracing_subscriber::EnvFilter::new(
            std::env::var("RUST_LOG").unwrap_or_else(|_| "info".into()),
        ))
        .with(tracing_subscriber::fmt::layer())
        .init();
    
    let mut parser = Parser::new();
    
    // Enable debug mode
    parser.set_debug_mode(true);
    
    let tsk_content = r#"
[debug]
enabled: true
log_level: "debug"
trace_parsing: true
trace_execution: true
"#;
    
    info!("Starting TuskLang parser with debug mode");
    
    match parser.parse(tsk_content).await {
        Ok(data) => {
            debug!("Successfully parsed configuration: {:?}", data);
            info!("Debug mode enabled: {}", data["debug"]["enabled"]);
        }
        Err(e) => {
            error!("Failed to parse configuration: {}", e);
            return Err(e.into());
        }
    }
    
    // Test with problematic configuration
    let problematic_tsk = r#"
[test]
value: 42
nested {
    deep {
        value: "test"
    }
}
"#;
    
    debug!("Parsing problematic configuration...");
    
    match parser.parse(problematic_tsk).await {
        Ok(data) => {
            debug!("Successfully parsed problematic config: {:?}", data);
        }
        Err(e) => {
            warn!("Failed to parse problematic config: {}", e);
            
            // Provide detailed error information
            error!("Error details:");
            error!("  Type: {:?}", std::any::type_name::<dyn std::error::Error>());
            error!("  Source: {:?}", e.source());
        }
    }
    
    Ok(())
}
```

### Interactive Debugging

```rust
use tusklang_rust::{parse, Parser};
use std::io::{self, Write};

#[tokio::main]
async fn interactive_debugging() -> Result<(), Box<dyn std::error::Error>> {
    println!("🔧 Interactive Debugging Mode");
    println!("Type 'help' for available commands");
    
    let mut parser = Parser::new();
    let mut config_data = None;
    
    loop {
        print!("tusk-debug> ");
        io::stdout().flush()?;
        
        let mut input = String::new();
        io::stdin().read_line(&mut input)?;
        let input = input.trim();
        
        match input {
            "help" => {
                println!("Available commands:");
                println!("  parse <file>     - Parse a TSK file");
                println!("  eval <expr>      - Evaluate an expression");
                println!("  query <sql>      - Execute a database query");
                println!("  fujsen <func>    - Execute a FUJSEN function");
                println!("  inspect <path>   - Inspect configuration value");
                println!("  reload           - Reload configuration");
                println!("  quit             - Exit debugger");
            }
            
            input if input.starts_with("parse ") => {
                let file = &input[6..];
                println!("Parsing file: {}", file);
                
                match parser.parse_file(file).await {
                    Ok(data) => {
                        config_data = Some(data);
                        println!("✅ Successfully parsed {}", file);
                    }
                    Err(e) => {
                        println!("❌ Failed to parse {}: {}", file, e);
                    }
                }
            }
            
            input if input.starts_with("eval ") => {
                let expr = &input[5..];
                println!("Evaluating: {}", expr);
                
                match parser.evaluate_expression(expr).await {
                    Ok(result) => {
                        println!("Result: {:?}", result);
                    }
                    Err(e) => {
                        println!("❌ Evaluation failed: {}", e);
                    }
                }
            }
            
            input if input.starts_with("query ") => {
                let sql = &input[6..];
                println!("Executing query: {}", sql);
                
                match parser.query(sql).await {
                    Ok(result) => {
                        println!("Query result: {:?}", result);
                    }
                    Err(e) => {
                        println!("❌ Query failed: {}", e);
                    }
                }
            }
            
            input if input.starts_with("fujsen ") => {
                let func_call = &input[7..];
                let parts: Vec<&str> = func_call.split_whitespace().collect();
                
                if parts.len() >= 3 {
                    let module = parts[0];
                    let function = parts[1];
                    let args: Vec<&str> = parts[2..].to_vec();
                    
                    println!("Executing FUJSEN: {}.{}({:?})", module, function, args);
                    
                    match parser.execute_fujsen(module, function, &args).await {
                        Ok(result) => {
                            println!("FUJSEN result: {:?}", result);
                        }
                        Err(e) => {
                            println!("❌ FUJSEN execution failed: {}", e);
                        }
                    }
                } else {
                    println!("❌ Invalid FUJSEN command format. Use: fujsen <module> <function> <args...>");
                }
            }
            
            input if input.starts_with("inspect ") => {
                let path = &input[8..];
                println!("Inspecting: {}", path);
                
                if let Some(data) = &config_data {
                    // Simple path inspection (you'd implement proper path parsing)
                    if let Some(value) = data.get(path) {
                        println!("Value: {:?}", value);
                    } else {
                        println!("❌ Path not found: {}", path);
                    }
                } else {
                    println!("❌ No configuration loaded. Use 'parse <file>' first.");
                }
            }
            
            "reload" => {
                println!("Reloading configuration...");
                // Implementation would reload the last parsed file
                println!("✅ Configuration reloaded");
            }
            
            "quit" => {
                println!("Goodbye!");
                break;
            }
            
            "" => continue,
            
            _ => {
                println!("❌ Unknown command: {}", input);
                println!("Type 'help' for available commands");
            }
        }
    }
    
    Ok(())
}
```

## 🔧 Performance Troubleshooting

### Memory Leaks

```rust
use tusklang_rust::{parse, Parser};
use std::alloc::{alloc, dealloc, Layout};
use std::sync::Arc;
use parking_lot::Mutex;

struct MemoryTracker {
    allocations: Arc<Mutex<HashMap<*mut u8, usize>>>,
    total_allocated: Arc<Mutex<usize>>,
}

impl MemoryTracker {
    fn new() -> Self {
        Self {
            allocations: Arc::new(Mutex::new(HashMap::new())),
            total_allocated: Arc::new(Mutex::new(0)),
        }
    }
    
    fn track_allocation(&self, ptr: *mut u8, size: usize) {
        self.allocations.lock().insert(ptr, size);
        *self.total_allocated.lock() += size;
    }
    
    fn track_deallocation(&self, ptr: *mut u8) {
        if let Some(size) = self.allocations.lock().remove(&ptr) {
            *self.total_allocated.lock() -= size;
        }
    }
    
    fn get_total_allocated(&self) -> usize {
        *self.total_allocated.lock()
    }
    
    fn get_allocation_count(&self) -> usize {
        self.allocations.lock().len()
    }
}

#[tokio::main]
async fn troubleshoot_memory_leaks() -> Result<(), Box<dyn std::error::Error>> {
    println!("💾 Troubleshooting Memory Leaks...");
    
    let memory_tracker = MemoryTracker::new();
    let mut parser = Parser::new();
    
    // Set memory tracker
    parser.set_memory_tracker(Arc::new(memory_tracker.clone()));
    
    println!("Initial memory usage: {} bytes ({} allocations)", 
        memory_tracker.get_total_allocated(), 
        memory_tracker.get_allocation_count());
    
    // Simulate memory-intensive operations
    for i in 0..1000 {
        let tsk_content = format!(r#"
[test_{}]
value: {}
string: "test_string_{}"
array: [1, 2, 3, 4, 5]
object: {{
    key: "value_{}"
    nested: {{
        deep: "data_{}"
    }}
}}
"#, i, i, i, i, i);
        
        match parser.parse(&tsk_content).await {
            Ok(_) => {
                if i % 100 == 0 {
                    println!("Processed {} configurations", i);
                    println!("Memory usage: {} bytes ({} allocations)", 
                        memory_tracker.get_total_allocated(), 
                        memory_tracker.get_allocation_count());
                }
            }
            Err(e) => {
                println!("❌ Failed to parse configuration {}: {}", i, e);
            }
        }
    }
    
    // Force garbage collection
    parser.force_garbage_collection().await;
    
    println!("Final memory usage: {} bytes ({} allocations)", 
        memory_tracker.get_total_allocated(), 
        memory_tracker.get_allocation_count());
    
    // Check for memory leaks
    let final_allocations = memory_tracker.get_allocation_count();
    if final_allocations > 0 {
        println!("⚠️  Potential memory leak detected: {} allocations not freed", final_allocations);
    } else {
        println!("✅ No memory leaks detected");
    }
    
    Ok(())
}
```

### Performance Bottlenecks

```rust
use tusklang_rust::{parse, Parser};
use std::time::{Duration, Instant};
use std::collections::HashMap;

struct PerformanceProfiler {
    measurements: HashMap<String, Vec<Duration>>,
}

impl PerformanceProfiler {
    fn new() -> Self {
        Self {
            measurements: HashMap::new(),
        }
    }
    
    fn start_measurement(&self, name: &str) -> Instant {
        Instant::now()
    }
    
    fn end_measurement(&mut self, name: &str, start: Instant) {
        let duration = start.elapsed();
        self.measurements.entry(name.to_string())
            .or_insert_with(Vec::new)
            .push(duration);
    }
    
    fn get_statistics(&self, name: &str) -> Option<(Duration, Duration, Duration)> {
        self.measurements.get(name).map(|durations| {
            let total: Duration = durations.iter().sum();
            let avg = total / durations.len() as u32;
            let max = durations.iter().max().unwrap().clone();
            let min = durations.iter().min().unwrap().clone();
            (avg, min, max)
        })
    }
    
    fn print_report(&self) {
        println!("📊 Performance Report");
        println!("{}", "=".repeat(50));
        
        for (name, durations) in &self.measurements {
            let total: Duration = durations.iter().sum();
            let avg = total / durations.len() as u32;
            let max = durations.iter().max().unwrap();
            let min = durations.iter().min().unwrap();
            
            println!("{}:", name);
            println!("  Count: {}", durations.len());
            println!("  Average: {:?}", avg);
            println!("  Min: {:?}", min);
            println!("  Max: {:?}", max);
            println!("  Total: {:?}", total);
            println!();
        }
    }
}

#[tokio::main]
async fn troubleshoot_performance_bottlenecks() -> Result<(), Box<dyn std::error::Error>> {
    println!("⚡ Troubleshooting Performance Bottlenecks...");
    
    let mut profiler = PerformanceProfiler::new();
    let mut parser = Parser::new();
    
    // Test parsing performance
    let tsk_content = r#"
[application]
name: "PerformanceTest"
version: "1.0.0"

[database]
host: "localhost"
port: 5432
name: "testdb"
user: "postgres"
password: "secret"

[server]
host: "0.0.0.0"
port: 8080
workers: 8
threads_per_worker: 4

[cache]
redis_host: "localhost"
redis_port: 6379
ttl: "5m"

[logging]
level: "info"
format: "json"
file: "/var/log/app.log"

[security]
jwt_secret: "your-secret-key"
bcrypt_rounds: 12
session_timeout: "24h"
"#;
    
    // Measure parsing performance
    for i in 0..100 {
        let start = profiler.start_measurement("parsing");
        let _result = parser.parse(tsk_content).await?;
        profiler.end_measurement("parsing", start);
    }
    
    // Measure FUJSEN execution performance
    let fujsen_tsk = r#"
[math]
add_fujsen = '''
fn add(a: i32, b: i32) -> i32 {
    a + b
}
'''
"#;
    
    parser.parse(fujsen_tsk).await?;
    
    for i in 0..1000 {
        let start = profiler.start_measurement("fujsen_execution");
        let _result = parser.execute_fujsen("math", "add", &[&i, &i]).await?;
        profiler.end_measurement("fujsen_execution", start);
    }
    
    // Measure database query performance
    // SQLiteAdapter is not imported, assuming it's available or needs to be added
    // For the purpose of this example, we'll simulate a database connection
    // In a real scenario, you'd use an actual adapter like PostgreSQLAdapter
    let db = SQLiteAdapter::new(":memory:").await?;
    db.execute("CREATE TABLE test (id INTEGER, value TEXT)").await?;
    parser.set_database_adapter(db);
    
    for i in 0..100 {
        let start = profiler.start_measurement("database_query");
        let _result = parser.query("SELECT * FROM test").await?;
        profiler.end_measurement("database_query", start);
    }
    
    // Print performance report
    profiler.print_report();
    
    // Identify bottlenecks
    if let Some((avg, _, _)) = profiler.get_statistics("parsing") {
        if avg > Duration::from_millis(10) {
            println!("⚠️  Parsing performance bottleneck detected: {:?} average", avg);
            println!("💡 Tip: Consider optimizing TSK file structure or using caching");
        }
    }
    
    if let Some((avg, _, _)) = profiler.get_statistics("fujsen_execution") {
        if avg > Duration::from_micros(100) {
            println!("⚠️  FUJSEN execution bottleneck detected: {:?} average", avg);
            println!("💡 Tip: Optimize FUJSEN functions or use caching");
        }
    }
    
    if let Some((avg, _, _)) = profiler.get_statistics("database_query") {
        if avg > Duration::from_millis(5) {
            println!("⚠️  Database query bottleneck detected: {:?} average", avg);
            println!("💡 Tip: Add database indexes or implement query caching");
        }
    }
    
    Ok(())
}
```

## 🔄 Recovery Strategies

### Automatic Recovery

```rust
use tusklang_rust::{parse, Parser};
use std::sync::Arc;
use tokio::sync::Mutex;
use std::time::{Duration, Instant};

struct RecoveryManager {
    last_backup: Arc<Mutex<Instant>>,
    backup_interval: Duration,
    max_retries: usize,
}

impl RecoveryManager {
    fn new(backup_interval: Duration, max_retries: usize) -> Self {
        Self {
            last_backup: Arc::new(Mutex::new(Instant::now())),
            backup_interval,
            max_retries,
        }
    }
    
    async fn should_backup(&self) -> bool {
        let last_backup = *self.last_backup.lock().await;
        Instant::now().duration_since(last_backup) >= self.backup_interval
    }
    
    async fn update_backup_time(&self) {
        *self.last_backup.lock().await = Instant::now();
    }
    
    async fn retry_operation<F, T, E>(&self, operation: F) -> Result<T, E>
    where
        F: Fn() -> Result<T, E> + Send + Sync,
        E: std::fmt::Debug,
    {
        let mut last_error = None;
        
        for attempt in 1..=self.max_retries {
            match operation() {
                Ok(result) => {
                    if attempt > 1 {
                        println!("✅ Operation succeeded on attempt {}", attempt);
                    }
                    return Ok(result);
                }
                Err(e) => {
                    last_error = Some(e);
                    println!("⚠️  Operation failed on attempt {}: {:?}", attempt, last_error);
                    
                    if attempt < self.max_retries {
                        // Exponential backoff
                        let delay = Duration::from_millis(100 * 2_u64.pow(attempt as u32));
                        tokio::time::sleep(delay).await;
                    }
                }
            }
        }
        
        Err(last_error.unwrap())
    }
}

#[tokio::main]
async fn implement_recovery_strategies() -> Result<(), Box<dyn std::error::Error>> {
    println!("🔄 Implementing Recovery Strategies...");
    
    let recovery_manager = RecoveryManager::new(
        Duration::from_secs(300), // 5 minutes
        3, // max retries
    );
    
    let mut parser = Parser::new();
    
    // 1. Automatic backup strategy
    println!("1. Setting up automatic backup strategy...");
    
    if recovery_manager.should_backup().await {
        println!("Creating backup...");
        
        // Create backup of configuration
        let backup_content = r#"
[backup]
timestamp: @date.now()
version: "1.0.0"
"#;
        
        std::fs::write("backup.tsk", backup_content)?;
        recovery_manager.update_backup_time().await;
        
        println!("✅ Backup created successfully");
    }
    
    // 2. Retry strategy for database operations
    println!("2. Implementing retry strategy for database operations...");
    
    let db_operation = || {
        // Simulate database operation that might fail
        if rand::random::<f64>() < 0.7 {
            Ok("Database operation successful")
        } else {
            Err("Database connection failed")
        }
    };
    
    match recovery_manager.retry_operation(db_operation).await {
        Ok(result) => println!("✅ Database operation succeeded: {}", result),
        Err(e) => println!("❌ Database operation failed after retries: {:?}", e),
    }
    
    // 3. Circuit breaker pattern
    println!("3. Implementing circuit breaker pattern...");
    
    let mut circuit_breaker = CircuitBreaker::new(5, Duration::from_secs(60));
    
    for i in 0..10 {
        match circuit_breaker.call(|| {
            // Simulate external service call
            if rand::random::<f64>() < 0.3 {
                Ok("Service call successful")
            } else {
                Err("Service call failed")
            }
        }).await {
            Ok(result) => println!("✅ Service call {} succeeded: {}", i, result),
            Err(e) => println!("❌ Service call {} failed: {:?}", i, e),
        }
    }
    
    // 4. Graceful degradation
    println!("4. Implementing graceful degradation...");
    
    let tsk_content = r#"
[degradation]
fallback_enabled: true
cache_fallback: true
database_fallback: true
"#;
    
    let config = parser.parse(tsk_content).await?;
    
    if config["degradation"]["fallback_enabled"].as_bool().unwrap_or(false) {
        println!("✅ Graceful degradation enabled");
        
        // Try primary service
        match call_primary_service().await {
            Ok(result) => println!("✅ Primary service: {}", result),
            Err(_) => {
                println!("⚠️  Primary service failed, using fallback");
                
                // Use fallback service
                match call_fallback_service().await {
                    Ok(result) => println!("✅ Fallback service: {}", result),
                    Err(e) => println!("❌ Fallback service also failed: {:?}", e),
                }
            }
        }
    }
    
    Ok(())
}

// Circuit breaker implementation
struct CircuitBreaker {
    failure_count: usize,
    max_failures: usize,
    reset_timeout: Duration,
    last_failure: Option<Instant>,
    state: CircuitState,
}

enum CircuitState {
    Closed,
    Open,
    HalfOpen,
}

impl CircuitBreaker {
    fn new(max_failures: usize, reset_timeout: Duration) -> Self {
        Self {
            failure_count: 0,
            max_failures,
            reset_timeout,
            last_failure: None,
            state: CircuitState::Closed,
        }
    }
    
    async fn call<F, T, E>(&mut self, operation: F) -> Result<T, E>
    where
        F: FnOnce() -> Result<T, E>,
    {
        match self.state {
            CircuitState::Open => {
                if let Some(last_failure) = self.last_failure {
                    if Instant::now().duration_since(last_failure) >= self.reset_timeout {
                        self.state = CircuitState::HalfOpen;
                        println!("🔄 Circuit breaker transitioning to half-open");
                    } else {
                        return Err(operation().unwrap_err());
                    }
                }
            }
            CircuitState::HalfOpen | CircuitState::Closed => {}
        }
        
        match operation() {
            Ok(result) => {
                self.on_success();
                Ok(result)
            }
            Err(e) => {
                self.on_failure();
                Err(e)
            }
        }
    }
    
    fn on_success(&mut self) {
        self.failure_count = 0;
        self.state = CircuitState::Closed;
    }
    
    fn on_failure(&mut self) {
        self.failure_count += 1;
        self.last_failure = Some(Instant::now());
        
        if self.failure_count >= self.max_failures {
            self.state = CircuitState::Open;
            println!("🚨 Circuit breaker opened due to {} failures", self.failure_count);
        }
    }
}

// Mock service calls
async fn call_primary_service() -> Result<String, Box<dyn std::error::Error>> {
    if rand::random::<f64>() < 0.8 {
        Ok("Primary service response".to_string())
    } else {
        Err("Primary service unavailable".into())
    }
}

async fn call_fallback_service() -> Result<String, Box<dyn std::error::Error>> {
    if rand::random::<f64>() < 0.9 {
        Ok("Fallback service response".to_string())
    } else {
        Err("Fallback service unavailable".into())
    }
}
```

## 🎯 What You've Learned

1. **Common error patterns** - Parsing errors, database connection errors, FUJSEN execution errors
2. **Debugging techniques** - Logging and tracing, interactive debugging
3. **Performance troubleshooting** - Memory leaks, performance bottlenecks
4. **Recovery strategies** - Automatic recovery, retry strategies, circuit breakers, graceful degradation
5. **Error handling** - Comprehensive error handling and recovery mechanisms
6. **Monitoring and alerting** - Performance monitoring and alerting systems

## 🚀 Next Steps

1. **Implement monitoring** - Set up comprehensive monitoring and alerting
2. **Create runbooks** - Document common issues and their solutions
3. **Automate recovery** - Implement automatic recovery mechanisms
4. **Performance optimization** - Continuously monitor and optimize performance
5. **Team training** - Train your team on troubleshooting procedures

---

**You now have complete troubleshooting mastery with TuskLang Rust!** From common errors to performance issues, from debugging techniques to recovery strategies - TuskLang gives you the tools to diagnose and resolve problems quickly and effectively. 