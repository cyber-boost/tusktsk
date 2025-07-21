# ⚠️ @ Operator Errors in Rust

TuskLang provides comprehensive error handling for @ operators in Rust, ensuring robust applications with proper error recovery and debugging capabilities.

## Error Types

```rust
// Define custom error types for @ operators
#[derive(Debug, thiserror::Error)]
pub enum TuskOperatorError {
    #[error("Invalid argument count: expected {expected}, got {actual}")]
    InvalidArgumentCount { expected: usize, actual: usize },
    
    #[error("Invalid argument type: expected {expected}, got {actual}")]
    InvalidArgumentType { expected: String, actual: String },
    
    #[error("Database error: {message}")]
    DatabaseError { message: String },
    
    #[error("Network error: {message}")]
    NetworkError { message: String },
    
    #[error("Validation error: {field} - {message}")]
    ValidationError { field: String, message: String },
    
    #[error("Cache error: {message}")]
    CacheError { message: String },
    
    #[error("File system error: {message}")]
    FileSystemError { message: String },
    
    #[error("Custom operator error: {message}")]
    CustomOperatorError { message: String },
}

// Error context for better debugging
#[derive(Debug)]
pub struct ErrorContext {
    pub operator: String,
    pub arguments: Vec<String>,
    pub file: String,
    pub line: u32,
    pub column: u32,
}
```

## Basic Error Handling

```rust
// Handle @ operator errors with Result
fn process_user_data(user_id: u32) -> Result<UserData, TuskOperatorError> {
    let user = @query("SELECT * FROM users WHERE id = ?", vec![user_id])
        .map_err(|e| TuskOperatorError::DatabaseError {
            message: format!("Failed to fetch user: {}", e)
        })?;
    
    let posts = @query("SELECT * FROM posts WHERE user_id = ?", vec![user_id])
        .map_err(|e| TuskOperatorError::DatabaseError {
            message: format!("Failed to fetch posts: {}", e)
        })?;
    
    Ok(UserData { user, posts })
}

// Error handling with context
fn safe_operator_call<F, T>(operator: &str, args: &[Value], f: F) -> Result<T, TuskOperatorError>
where
    F: FnOnce(&[Value]) -> Result<T, Box<dyn std::error::Error>>,
{
    f(args).map_err(|e| TuskOperatorError::CustomOperatorError {
        message: format!("@{} failed: {}", operator, e)
    })
}
```

## Argument Validation Errors

```rust
// Validate @ operator arguments
fn validate_arguments(args: &[Value], expected_count: usize, expected_types: &[&str]) -> Result<(), TuskOperatorError> {
    // Check argument count
    if args.len() != expected_count {
        return Err(TuskOperatorError::InvalidArgumentCount {
            expected: expected_count,
            actual: args.len(),
        });
    }
    
    // Check argument types
    for (i, (arg, expected_type)) in args.iter().zip(expected_types.iter()).enumerate() {
        let actual_type = match arg {
            Value::String(_) => "string",
            Value::Number(_) => "number",
            Value::Bool(_) => "boolean",
            Value::Array(_) => "array",
            Value::Object(_) => "object",
            Value::Null => "null",
        };
        
        if actual_type != *expected_type {
            return Err(TuskOperatorError::InvalidArgumentType {
                expected: expected_type.to_string(),
                actual: actual_type.to_string(),
            });
        }
    }
    
    Ok(())
}

// Usage in custom operators
impl CustomOperator for MyOperator {
    fn execute(&self, args: &[Value]) -> Result<Value, Box<dyn std::error::Error>> {
        validate_arguments(args, 2, &["string", "number"])?;
        
        let text = args[0].as_str().unwrap();
        let count = args[1].as_u64().unwrap();
        
        // Process arguments...
        Ok(Value::String(text.repeat(count as usize)))
    }
}
```

## Database Error Handling

```rust
// Handle database @ operator errors
fn safe_database_query(sql: &str, params: Vec<Value>) -> Result<Vec<Value>, TuskOperatorError> {
    @query(sql, params).map_err(|e| {
        TuskOperatorError::DatabaseError {
            message: format!("Query failed: {} - SQL: {}", e, sql)
        }
    })
}

// Transaction error handling
fn execute_transaction<F, T>(f: F) -> Result<T, TuskOperatorError>
where
    F: FnOnce(&mut Transaction) -> Result<T, Box<dyn std::error::Error>>,
{
    @db.transaction(|tx| {
        f(tx).map_err(|e| TuskOperatorError::DatabaseError {
            message: format!("Transaction failed: {}", e)
        })
    }).map_err(|e| TuskOperatorError::DatabaseError {
        message: format!("Transaction error: {}", e)
    })
}

// Usage
let result = execute_transaction(|tx| {
    let user = tx.table("users").insert(@user_data)?;
    let profile = tx.table("profiles").insert(ProfileData {
        user_id: user.id,
        bio: @bio,
    })?;
    Ok((user, profile))
})?;
```

## Network Error Handling

```rust
// Handle HTTP @ operator errors
async fn safe_http_request(method: &str, url: &str, body: Option<Value>) -> Result<Value, TuskOperatorError> {
    @http(method, url, body).await.map_err(|e| {
        TuskOperatorError::NetworkError {
            message: format!("HTTP request failed: {} {} - {}", method, url, e)
        }
    })
}

// Retry logic for network operations
async fn retry_http_request(method: &str, url: &str, max_retries: u32) -> Result<Value, TuskOperatorError> {
    let mut last_error = None;
    
    for attempt in 1..=max_retries {
        match @http(method, url, None).await {
            Ok(response) => return Ok(response),
            Err(e) => {
                last_error = Some(e);
                if attempt < max_retries {
                    tokio::time::sleep(tokio::time::Duration::from_secs(2u64.pow(attempt))).await;
                }
            }
        }
    }
    
    Err(TuskOperatorError::NetworkError {
        message: format!("HTTP request failed after {} attempts: {}", max_retries, 
                        last_error.unwrap_or_else(|| "Unknown error".into()))
    })
}
```

## Validation Error Handling

```rust
// Handle validation @ operator errors
fn validate_user_data(data: &Value) -> Result<(), Vec<TuskOperatorError>> {
    let mut errors = Vec::new();
    
    // Validate email
    if let Some(email) = data.get("email").and_then(|e| e.as_str()) {
        if !@is_valid_email(email) {
            errors.push(TuskOperatorError::ValidationError {
                field: "email".to_string(),
                message: "Invalid email format".to_string(),
            });
        }
    } else {
        errors.push(TuskOperatorError::ValidationError {
            field: "email".to_string(),
            message: "Email is required".to_string(),
        });
    }
    
    // Validate age
    if let Some(age) = data.get("age").and_then(|a| a.as_u64()) {
        if age < 18 {
            errors.push(TuskOperatorError::ValidationError {
                field: "age".to_string(),
                message: "User must be 18 or older".to_string(),
            });
        }
    }
    
    if errors.is_empty() {
        Ok(())
    } else {
        Err(errors)
    }
}

// Usage
let validation_result = validate_user_data(&@user_data);
match validation_result {
    Ok(()) => {
        @save_user(@user_data)?;
    }
    Err(errors) => {
        for error in errors {
            @log("Validation error: {:?}", error);
        }
        return Err(TuskOperatorError::ValidationError {
            field: "user_data".to_string(),
            message: "User data validation failed".to_string(),
        });
    }
}
```

## Cache Error Handling

```rust
// Handle cache @ operator errors
fn safe_cache_operation<F, T>(key: &str, f: F) -> Result<T, TuskOperatorError>
where
    F: FnOnce() -> Result<T, Box<dyn std::error::Error>>,
{
    @cache.get(key).map_err(|e| TuskOperatorError::CacheError {
        message: format!("Cache get failed for key '{}': {}", key, e)
    }).or_else(|_| {
        // Cache miss, execute function and store result
        let result = f().map_err(|e| TuskOperatorError::CacheError {
            message: format!("Cache fallback failed for key '{}': {}", key, e)
        })?;
        
        @cache.put(key, &result, Duration::from_secs(3600))
            .map_err(|e| TuskOperatorError::CacheError {
                message: format!("Cache put failed for key '{}': {}", key, e)
            })?;
        
        Ok(result)
    })
}

// Usage
let user_data = safe_cache_operation(&format!("user:{}", @user_id), || {
    @query("SELECT * FROM users WHERE id = ?", vec![@user_id])
})?;
```

## File System Error Handling

```rust
// Handle file @ operator errors
fn safe_file_operation(action: &str, path: &str, content: Option<&str>) -> Result<Value, TuskOperatorError> {
    match action {
        "read" => {
            std::fs::read_to_string(path)
                .map(Value::String)
                .map_err(|e| TuskOperatorError::FileSystemError {
                    message: format!("Failed to read file '{}': {}", path, e)
                })
        }
        "write" => {
            let content = content.ok_or_else(|| TuskOperatorError::FileSystemError {
                message: "Content required for write operation".to_string()
            })?;
            
            std::fs::write(path, content)
                .map(|_| Value::Bool(true))
                .map_err(|e| TuskOperatorError::FileSystemError {
                    message: format!("Failed to write file '{}': {}", path, e)
                })
        }
        "exists" => {
            Ok(Value::Bool(std::path::Path::new(path).exists()))
        }
        _ => Err(TuskOperatorError::FileSystemError {
            message: format!("Unknown file action: {}", action)
        })
    }
}

// Usage
let config_content = safe_file_operation("read", "config.tusk", None)?;
safe_file_operation("write", "log.txt", Some(&@log_content))?;
```

## Error Recovery Strategies

```rust
// Implement error recovery strategies
enum RecoveryStrategy {
    Retry { max_attempts: u32, delay_ms: u64 },
    Fallback { value: Value },
    CircuitBreaker { threshold: u32, timeout_ms: u64 },
    GracefulDegradation { degraded_function: Box<dyn Fn() -> Result<Value, TuskOperatorError>> },
}

fn execute_with_recovery<F>(operation: F, strategy: RecoveryStrategy) -> Result<Value, TuskOperatorError>
where
    F: Fn() -> Result<Value, TuskOperatorError>,
{
    match strategy {
        RecoveryStrategy::Retry { max_attempts, delay_ms } => {
            let mut last_error = None;
            
            for attempt in 1..=max_attempts {
                match operation() {
                    Ok(result) => return Ok(result),
                    Err(e) => {
                        last_error = Some(e);
                        if attempt < max_attempts {
                            std::thread::sleep(std::time::Duration::from_millis(delay_ms));
                        }
                    }
                }
            }
            
            Err(last_error.unwrap_or_else(|| TuskOperatorError::CustomOperatorError {
                message: "Operation failed after all retry attempts".to_string()
            }))
        }
        
        RecoveryStrategy::Fallback { value } => {
            operation().or(Ok(value))
        }
        
        RecoveryStrategy::CircuitBreaker { threshold, timeout_ms } => {
            // Implement circuit breaker pattern
            static mut FAILURE_COUNT: u32 = 0;
            static mut LAST_FAILURE_TIME: u64 = 0;
            
            unsafe {
                let now = std::time::SystemTime::now()
                    .duration_since(std::time::UNIX_EPOCH)
                    .unwrap()
                    .as_millis() as u64;
                
                if FAILURE_COUNT >= threshold && (now - LAST_FAILURE_TIME) < timeout_ms {
                    return Err(TuskOperatorError::CustomOperatorError {
                        message: "Circuit breaker is open".to_string()
                    });
                }
                
                match operation() {
                    Ok(result) => {
                        FAILURE_COUNT = 0;
                        Ok(result)
                    }
                    Err(e) => {
                        FAILURE_COUNT += 1;
                        LAST_FAILURE_TIME = now;
                        Err(e)
                    }
                }
            }
        }
        
        RecoveryStrategy::GracefulDegradation { degraded_function } => {
            operation().or_else(|_| degraded_function())
        }
    }
}

// Usage
let result = execute_with_recovery(
    || @query("SELECT * FROM users"),
    RecoveryStrategy::Retry { max_attempts: 3, delay_ms: 1000 }
)?;

let cached_result = execute_with_recovery(
    || @cache.get("expensive_operation"),
    RecoveryStrategy::Fallback { value: Value::String("default".to_string()) }
)?;
```

## Error Logging and Monitoring

```rust
// Comprehensive error logging
fn log_operator_error(error: &TuskOperatorError, context: &ErrorContext) {
    let error_message = match error {
        TuskOperatorError::DatabaseError { message } => {
            format!("Database error in @{}: {}", context.operator, message)
        }
        TuskOperatorError::NetworkError { message } => {
            format!("Network error in @{}: {}", context.operator, message)
        }
        TuskOperatorError::ValidationError { field, message } => {
            format!("Validation error in @{} for field '{}': {}", context.operator, field, message)
        }
        _ => format!("Error in @{}: {:?}", context.operator, error),
    };
    
    @log("ERROR: {} at {}:{}:{}", 
         error_message, 
         context.file, 
         context.line, 
         context.column);
    
    // Send to monitoring service
    @metrics.increment("tusk_operator_errors", 1);
    @metrics.increment(&format!("tusk_operator_errors.{}", context.operator), 1);
}

// Error context capture
macro_rules! operator_call {
    ($operator:expr, $($args:expr),*) => {{
        let context = ErrorContext {
            operator: stringify!($operator).to_string(),
            arguments: vec![$(format!("{:?}", $args)),*],
            file: file!().to_string(),
            line: line!(),
            column: column!(),
        };
        
        match $operator($($args),*) {
            Ok(result) => Ok(result),
            Err(e) => {
                log_operator_error(&e, &context);
                Err(e)
            }
        }
    }};
}

// Usage
let user_data = operator_call!(@query, "SELECT * FROM users WHERE id = ?", @user_id)?;
```

## Best Practices

### 1. Use Specific Error Types
```rust
// Define specific error types for different operations
#[derive(Debug, thiserror::Error)]
pub enum UserOperationError {
    #[error("User not found: {id}")]
    UserNotFound { id: u32 },
    
    #[error("User already exists: {email}")]
    UserAlreadyExists { email: String },
    
    #[error("Invalid user data: {message}")]
    InvalidUserData { message: String },
}

// Use specific error types in operations
fn create_user(user_data: UserData) -> Result<User, UserOperationError> {
    if @user_exists(user_data.email) {
        return Err(UserOperationError::UserAlreadyExists {
            email: user_data.email,
        });
    }
    
    @save_user(user_data).map_err(|e| UserOperationError::InvalidUserData {
        message: e.to_string(),
    })
}
```

### 2. Provide Context in Errors
```rust
// Include context in error messages
fn process_order(order_id: u32) -> Result<Order, TuskOperatorError> {
    @query("SELECT * FROM orders WHERE id = ?", vec![order_id])
        .map_err(|e| TuskOperatorError::DatabaseError {
            message: format!("Failed to fetch order {}: {}", order_id, e)
        })?
        .ok_or_else(|| TuskOperatorError::CustomOperatorError {
            message: format!("Order {} not found", order_id)
        })
}
```

### 3. Implement Proper Error Recovery
```rust
// Implement appropriate recovery strategies
fn get_user_data(user_id: u32) -> Result<UserData, TuskOperatorError> {
    // Try cache first
    if let Ok(cached) = @cache.get(&format!("user:{}", user_id)) {
        return Ok(cached);
    }
    
    // Fallback to database
    let user_data = @query("SELECT * FROM users WHERE id = ?", vec![user_id])
        .map_err(|e| TuskOperatorError::DatabaseError {
            message: format!("Failed to fetch user {}: {}", user_id, e)
        })?;
    
    // Cache for next time
    @cache.put(&format!("user:{}", user_id), &user_data, Duration::from_secs(3600))
        .map_err(|e| TuskOperatorError::CacheError {
            message: format!("Failed to cache user data: {}", e)
        })?;
    
    Ok(user_data)
}
```

### 4. Use Error Propagation
```rust
// Use the ? operator for clean error propagation
fn process_user_request(request: UserRequest) -> Result<UserResponse, TuskOperatorError> {
    let user = @validate_user(request.user_id)?;
    let permissions = @get_user_permissions(user.id)?;
    let data = @process_user_data(user, permissions)?;
    
    Ok(UserResponse { user, data })
}
```

### 5. Monitor and Alert on Errors
```rust
// Set up error monitoring
fn monitor_operator_errors() {
    @metrics.gauge("tusk_operator_error_rate", @calculate_error_rate());
    
    if @error_rate_exceeds_threshold() {
        @alert.send("High TuskLang operator error rate detected");
    }
}
```

The @ operator error handling in Rust provides robust error management with proper recovery strategies, comprehensive logging, and type-safe error propagation. 