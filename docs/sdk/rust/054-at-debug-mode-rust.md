# 🐛 @debug - Debug Mode and Tools in Rust

The `@debug` operator provides comprehensive debugging capabilities for Rust applications, helping developers identify issues, trace execution, and optimize performance with zero-cost abstractions.

## Basic Syntax

```rust
// Check if debug mode is enabled
if @debug {
    @log("Debug mode active");
}

// Debug output
@debug.log("Variable value", @my_var);

// Dump and die
@debug.dd(@complex_object);

// Conditional debugging
@debug.when(@condition, "This only logs when condition is true");
```

## Debug Configuration

```rust
// Enable/disable debug mode
@debug.enable();
@debug.disable();

// Configure debug settings
@debug.config({
    // Output settings
    output: "console",      // console, file, browser
    format: "pretty",       // pretty, json, raw
    
    // What to include
    show_types: true,
    show_memory: true,
    show_time: true,
    show_backtrace: false,
    max_depth: 10,
    
    // Performance
    profile: true,
    slow_query_threshold: 0.1,
    
    // Error handling
    display_errors: true,
    error_level: E_ALL,
});
```

## Variable Inspection

```rust
// Debug dump
@debug.dump(@variable);
/* Output:
String("hello") - 5 bytes
Memory: 1.2MB
Time: 0.0023s
*/

// Dump with label
@debug.dump(@user, "Current User");
/* Output:
=== Current User ===
User {
    id: 123,
    name: "John Doe",
    email: "john@example.com",
}
*/

// Dump and die
@debug.dd(@data);  // Stops execution

// Dump multiple variables
@debug.dump_all({
    user: @user,
    request: @request,
    session: @session,
});

// Pretty print
@debug.pretty(@complex_array);
```

## Execution Tracing

```rust
// Trace function calls
@debug.trace("Starting user authentication");

// Trace with data
@debug.trace("Processing order", {
    order_id: @order.id,
    items: @count(@order.items),
    total: @order.total,
});

// Function execution trace
let calculate_total = @debug.trace_function(|items| {
    let mut total = 0;
    for item in items {
        total += item.price * item.quantity;
    }
    total
});

// Backtrace
@debug.backtrace();
/* Output:
#0 /app/controllers/UserController.rs:45 process_request()
#1 /app/middleware/Auth.rs:23 authenticate()
#2 /app/routes.rs:12 handle()
*/

// Get call stack
let stack = @debug.get_stack();
for frame in stack {
    @log(format!("{}:{} {}", frame.file, frame.line, frame.function));
}
```

## Performance Profiling

```rust
// Profile code block
let profile = @debug.profile("database_operations", || {
    let users = @query("SELECT * FROM users");
    for user in users {
        @update_user_stats(user);
    }
});

// Get profile results
let profile_data = @debug.get_profile("database_operations");
/* Output:
{
    total_time: 0.234,
    memory_used: 2048576,
    memory_peak: 3145728,
    queries: 101,
    query_time: 0.189,
}
*/

// Benchmark comparison
let benchmark = @debug.benchmark({
    "method1": || @process_with_method1(),
    "method2": || @process_with_method2(),
    "method3": || @process_with_method3(),
}, 1000);  // Run 1000 iterations
/* Output:
method1: 0.0023s average
method2: 0.0019s average (fastest)
method3: 0.0045s average
*/
```

## Query Debugging

```rust
// Enable query logging
@debug.enable_query_log();

// Execute queries
let users = @query("SELECT * FROM users WHERE active = ?", vec![1]);
let orders = @query("SELECT * FROM orders WHERE user_id = ?", vec![@user.id]);

// Get query log
let queries = @debug.get_queries();
/* Output:
[
    {
        sql: "SELECT * FROM users WHERE active = ?",
        params: [1],
        time: 0.0023,
        rows: 150,
        file: "/app/models/User.rs:34",
    },
    {
        sql: "SELECT * FROM orders WHERE user_id = ?",
        params: [123],
        time: 0.0156,
        rows: 12,
        file: "/app/models/Order.rs:78",
    }
]
*/

// Explain query
@debug.explain("SELECT * FROM users WHERE email = ?", vec!["test@example.com"]);
/* Output:
+----+-------------+-------+------+---------------+-------+---------+-------+------+-------------+
| id | select_type | table | type | possible_keys | key   | key_len | ref   | rows | Extra       |
+----+-------------+-------+------+---------------+-------+---------+-------+------+-------------+
| 1  | SIMPLE      | users | ref  | email_idx     | email | 767     | const | 1    | Using where |
+----+-------------+-------+------+---------------+-------+---------+-------+------+-------------+
*/
```

## Memory Debugging

```rust
// Memory snapshot
@debug.memory_snapshot("before_operation");

// Do memory-intensive operation
let large_data = @load_large_dataset();
let processed = @process_data(large_data);

// Compare memory
@debug.memory_snapshot("after_operation");
let diff = @debug.memory_diff("before_operation", "after_operation");
/* Output:
Memory increased by 45.3MB
Peak memory: 128.5MB
Objects created: 15,234
*/

// Memory usage tracking
@debug.track_memory(|| {
    @load_users();
    @process_users();
    @save_results();
});
/* Output:
Step 1: +12.3MB (12.3MB total)
Step 2: +34.2MB (46.5MB total)
Step 3: -15.1MB (31.4MB total)
*/

// Find memory leaks
@debug.find_leaks({
    iterations: 1000,
    threshold: 1024,  // 1KB
});
```

## Error Debugging

```rust
// Enhanced error reporting
@debug.on_error(|error| {
    @debug.log("Error occurred", {
        message: error.message,
        file: error.file,
        line: error.line,
        backtrace: error.backtrace,
    });
});

// Error context
@debug.with_context("user_operation", || {
    @process_user_data(@user_id);
});

// Error recovery
@debug.recover(|| {
    @risky_operation();
}, |error| {
    @log("Recovered from error: {}", error);
    @fallback_operation();
});
```

## Async Debugging

```rust
// Async trace
@debug.async_trace("Starting async operation");

// Debug async operations
let result = @debug.async_profile("api_call", async {
    let response = @http.get("https://api.example.com").await?;
    response.json().await
}).await;

// Async error handling
@debug.async_on_error(async |error| {
    @log("Async error: {}", error);
    @notify_admin(error).await;
});
```

## WebAssembly Debugging

```rust
// WASM debug output
#[cfg(target_arch = "wasm32")]
@debug.wasm_log("WASM operation started");

// WASM memory tracking
#[cfg(target_arch = "wasm32")]
@debug.wasm_memory_track(|| {
    @process_large_dataset();
});
```

## Integration with Rust Tools

```rust
// Integration with tracing crate
use tracing::{info, error, debug};

@debug.integrate_tracing(|| {
    info!("Operation started");
    @process_data();
    debug!("Operation completed");
});

// Integration with log crate
use log::{info, error};

@debug.integrate_log(|| {
    info!("Processing user: {}", @user.id);
    @process_user(@user);
});
```

## Performance Monitoring

```rust
// CPU profiling
@debug.cpu_profile("data_processing", || {
    @process_large_dataset();
});

// Memory profiling
@debug.memory_profile("cache_operations", || {
    @load_cache();
    @process_cache();
    @save_cache();
});

// Network profiling
@debug.network_profile("api_calls", || {
    @call_external_api();
    @process_response();
});
```

## Debug Macros

```rust
// Debug macro for quick inspection
@debug.macro!("user_data", @user);

// Conditional debug macro
@debug.macro_if!(DEBUG_MODE, "sensitive_data", @sensitive_data);

// Debug macro with custom formatter
@debug.macro_format!("user_summary", @user, |u| format!("User {} ({})", u.name, u.email));
```

## Debug Configuration Examples

```rust
// Development configuration
@debug.config({
    output: "console",
    format: "pretty",
    show_types: true,
    show_memory: true,
    profile: true,
    display_errors: true,
});

// Production configuration
@debug.config({
    output: "file",
    format: "json",
    show_types: false,
    show_memory: false,
    profile: false,
    display_errors: false,
    log_level: "error",
});

// Testing configuration
@debug.config({
    output: "console",
    format: "raw",
    show_types: true,
    show_memory: false,
    profile: true,
    display_errors: true,
    log_level: "debug",
});
```

## Best Practices

### 1. Use Debug Levels Appropriately
```rust
// Use appropriate debug levels
@debug.trace("Function entry");  // For function tracing
@debug.log("Variable value");    // For general logging
@debug.error("Critical error");  // For errors only
```

### 2. Profile Performance-Critical Code
```rust
// Profile expensive operations
@debug.profile("database_query", || {
    @query("SELECT * FROM large_table");
});
```

### 3. Handle Async Debugging Properly
```rust
// Use async-aware debugging
@debug.async_trace("Starting async operation");
let result = async {
    @process_async_data().await
}.await;
@debug.async_trace("Async operation completed");
```

### 4. Memory Management
```rust
// Track memory usage in WASM
#[cfg(target_arch = "wasm32")]
@debug.wasm_memory_track(|| {
    @process_data();
});
```

### 5. Error Recovery
```rust
// Use error recovery for robustness
@debug.recover(|| {
    @risky_operation();
}, |error| {
    @log("Recovered: {}", error);
    @fallback_operation();
});
```

## Integration with Rust Ecosystem

### Serde Integration
```rust
use serde::{Deserialize, Serialize};

#[derive(Debug, Deserialize, Serialize)]
struct User {
    id: u32,
    name: String,
    email: String,
}

// Debug with serde
@debug.dump_serde(@user);
```

### Tokio Integration
```rust
use tokio;

// Debug async runtime
@debug.tokio_profile(|| {
    tokio::spawn(async {
        @async_operation().await;
    });
});
```

### Actix Integration
```rust
use actix_web;

// Debug web requests
@debug.actix_middleware(|req, srv| {
    @log("Request: {} {}", req.method(), req.uri());
    srv.call(req)
});
```

The `@debug` operator in Rust provides zero-cost debugging abstractions that integrate seamlessly with Rust's performance characteristics while offering comprehensive debugging capabilities for complex applications. 