# Error Handling Patterns in TuskLang with Rust

## ❌ Error Handling Foundation

Error handling with TuskLang and Rust provides robust, type-safe error management through Result types, custom error types, and comprehensive error handling strategies. This guide covers error patterns, propagation, and recovery mechanisms.

## 🏗️ Error Architecture

### Error Types

```rust
[error_architecture]
result_types: true
custom_errors: true
error_propagation: true
error_recovery: true

[error_components]
thiserror: "custom_error_macros"
anyhow: "error_handling"
eyre: "error_reporting"
snafu: "error_context"
```

### Error Configuration

```rust
[error_configuration]
error_logging: true
error_reporting: true
error_recovery: true
error_metrics: true

[error_implementation]
use std::error::Error;
use std::fmt;

// Basic error type
#[derive(Debug)]
pub struct BasicError {
    message: String,
    kind: ErrorKind,
}

#[derive(Debug, Clone, Copy)]
pub enum ErrorKind {
    Validation,
    Network,
    Database,
    Configuration,
    Unknown,
}

impl BasicError {
    pub fn new(message: String, kind: ErrorKind) -> Self {
        Self { message, kind }
    }
    
    pub fn validation(message: String) -> Self {
        Self::new(message, ErrorKind::Validation)
    }
    
    pub fn network(message: String) -> Self {
        Self::new(message, ErrorKind::Network)
    }
    
    pub fn database(message: String) -> Self {
        Self::new(message, ErrorKind::Database)
    }
    
    pub fn configuration(message: String) -> Self {
        Self::new(message, ErrorKind::Configuration)
    }
}

impl fmt::Display for BasicError {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        write!(f, "{}: {}", self.kind, self.message)
    }
}

impl Error for BasicError {}

impl fmt::Display for ErrorKind {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        match self {
            ErrorKind::Validation => write!(f, "Validation Error"),
            ErrorKind::Network => write!(f, "Network Error"),
            ErrorKind::Database => write!(f, "Database Error"),
            ErrorKind::Configuration => write!(f, "Configuration Error"),
            ErrorKind::Unknown => write!(f, "Unknown Error"),
        }
    }
}
```

## 🔧 Basic Error Patterns

### Result Types

```rust
[basic_error_patterns]
result_handling: true
error_mapping: true
error_combining: true

[basic_implementation]
use std::result::Result;

// Basic Result usage
pub fn basic_result_function(input: i32) -> Result<i32, String> {
    if input < 0 {
        Err("Input must be positive".to_string())
    } else {
        Ok(input * 2)
    }
}

// Result with custom error
pub fn result_with_custom_error(input: i32) -> Result<i32, BasicError> {
    if input < 0 {
        Err(BasicError::validation("Input must be positive".to_string()))
    } else if input > 1000 {
        Err(BasicError::validation("Input too large".to_string()))
    } else {
        Ok(input * 2)
    }
}

// Result mapping
pub fn result_mapping(input: i32) -> Result<String, BasicError> {
    result_with_custom_error(input)
        .map(|value| format!("Result: {}", value))
}

// Result chaining
pub fn result_chaining(input: i32) -> Result<String, BasicError> {
    result_with_custom_error(input)
        .and_then(|value| {
            if value > 100 {
                Err(BasicError::validation("Result too large".to_string()))
            } else {
                Ok(value)
            }
        })
        .map(|value| format!("Final result: {}", value))
}

// Result with multiple error types
pub fn multiple_error_types(input: &str) -> Result<i32, Box<dyn Error>> {
    let parsed = input.parse::<i32>()
        .map_err(|e| BasicError::validation(format!("Parse error: {}", e)))?;
    
    if parsed < 0 {
        return Err(Box::new(BasicError::validation("Negative number".to_string())));
    }
    
    Ok(parsed)
}

// Result with context
pub fn result_with_context(input: i32) -> Result<i32, BasicError> {
    result_with_custom_error(input)
        .map_err(|e| {
            BasicError::new(
                format!("Failed to process input {}: {}", input, e.message),
                e.kind
            )
        })
}
```

### Error Propagation

```rust
[error_propagation]
question_mark: true
error_conversion: true
error_wrapping: true

[propagation_implementation]
use std::io;

// Error propagation with ?
pub fn error_propagation(input: &str) -> Result<i32, BasicError> {
    let parsed = input.parse::<i32>()
        .map_err(|e| BasicError::validation(format!("Parse error: {}", e)))?;
    
    let result = result_with_custom_error(parsed)?;
    Ok(result)
}

// Error conversion
impl From<io::Error> for BasicError {
    fn from(err: io::Error) -> Self {
        BasicError::new(
            format!("IO error: {}", err),
            ErrorKind::Network
        )
    }
}

impl From<std::num::ParseIntError> for BasicError {
    fn from(err: std::num::ParseIntError) -> Self {
        BasicError::validation(format!("Parse error: {}", err))
    }
}

// Error conversion with From
pub fn error_conversion(input: &str) -> Result<i32, BasicError> {
    let parsed: i32 = input.parse()?; // Uses From implementation
    Ok(parsed)
}

// Error wrapping
pub fn error_wrapping(input: &str) -> Result<i32, BasicError> {
    let parsed = input.parse::<i32>()
        .map_err(|e| BasicError::validation(format!("Failed to parse '{}': {}", input, e)))?;
    
    result_with_custom_error(parsed)
        .map_err(|e| BasicError::new(
            format!("Failed to process parsed value {}: {}", parsed, e.message),
            e.kind
        ))
}
```

## 🎯 Custom Error Types

### ThisError Integration

```rust
[custom_error_types]
thiserror_macros: true
error_kinds: true
error_context: true

[custom_implementation]
use thiserror::Error;

#[derive(Error, Debug)]
pub enum AppError {
    #[error("Validation error: {message}")]
    Validation { message: String },
    
    #[error("Network error: {message}")]
    Network { message: String },
    
    #[error("Database error: {message}")]
    Database { message: String },
    
    #[error("Configuration error: {message}")]
    Configuration { message: String },
    
    #[error("IO error: {0}")]
    Io(#[from] io::Error),
    
    #[error("Parse error: {0}")]
    Parse(#[from] std::num::ParseIntError),
    
    #[error("Unknown error: {message}")]
    Unknown { message: String },
}

impl AppError {
    pub fn validation(message: impl Into<String>) -> Self {
        Self::Validation { message: message.into() }
    }
    
    pub fn network(message: impl Into<String>) -> Self {
        Self::Network { message: message.into() }
    }
    
    pub fn database(message: impl Into<String>) -> Self {
        Self::Database { message: message.into() }
    }
    
    pub fn configuration(message: impl Into<String>) -> Self {
        Self::Configuration { message: message.into() }
    }
    
    pub fn unknown(message: impl Into<String>) -> Self {
        Self::Unknown { message: message.into() }
    }
    
    pub fn is_retryable(&self) -> bool {
        matches!(self, Self::Network { .. } | Self::Database { .. })
    }
    
    pub fn is_critical(&self) -> bool {
        matches!(self, Self::Configuration { .. })
    }
}

// Using custom error types
pub fn custom_error_function(input: i32) -> Result<i32, AppError> {
    if input < 0 {
        return Err(AppError::validation("Input must be positive"));
    }
    
    if input > 1000 {
        return Err(AppError::validation("Input too large"));
    }
    
    Ok(input * 2)
}

// Error with context
pub fn error_with_context(input: &str) -> Result<i32, AppError> {
    let parsed: i32 = input.parse()?;
    custom_error_function(parsed)
        .map_err(|e| match e {
            AppError::Validation { message } => {
                AppError::validation(format!("Failed to process '{}': {}", input, message))
            }
            _ => e,
        })
}
```

### Error Context and Backtraces

```rust
[error_context]
context_information: true
backtrace_support: true
error_chaining: true

[context_implementation]
use std::backtrace::Backtrace;

#[derive(Error, Debug)]
pub enum ContextualError {
    #[error("Database operation failed: {operation}")]
    Database {
        operation: String,
        #[source]
        source: Box<dyn Error + Send + Sync>,
        #[backtrace]
        backtrace: Backtrace,
    },
    
    #[error("Network request failed: {url}")]
    Network {
        url: String,
        #[source]
        source: Box<dyn Error + Send + Sync>,
        #[backtrace]
        backtrace: Backtrace,
    },
    
    #[error("Configuration error: {field}")]
    Configuration {
        field: String,
        value: String,
        #[source]
        source: Option<Box<dyn Error + Send + Sync>>,
        #[backtrace]
        backtrace: Backtrace,
    },
}

impl ContextualError {
    pub fn database(operation: impl Into<String>, source: impl Error + Send + Sync + 'static) -> Self {
        Self::Database {
            operation: operation.into(),
            source: Box::new(source),
            backtrace: Backtrace::capture(),
        }
    }
    
    pub fn network(url: impl Into<String>, source: impl Error + Send + Sync + 'static) -> Self {
        Self::Network {
            url: url.into(),
            source: Box::new(source),
            backtrace: Backtrace::capture(),
        }
    }
    
    pub fn configuration(field: impl Into<String>, value: impl Into<String>) -> Self {
        Self::Configuration {
            field: field.into(),
            value: value.into(),
            source: None,
            backtrace: Backtrace::capture(),
        }
    }
}

// Error with context
pub fn contextual_error_function(input: i32) -> Result<i32, ContextualError> {
    if input < 0 {
        return Err(ContextualError::configuration("input", input.to_string()));
    }
    
    // Simulate database operation
    if input == 42 {
        return Err(ContextualError::database(
            "SELECT * FROM users",
            io::Error::new(io::ErrorKind::ConnectionRefused, "Database connection failed")
        ));
    }
    
    Ok(input * 2)
}
```

## 🔄 Error Recovery Patterns

### Retry Mechanisms

```rust
[error_recovery]
retry_patterns: true
circuit_breakers: true
fallback_strategies: true

[recovery_implementation]
use std::time::Duration;
use tokio::time::sleep;

// Retry with exponential backoff
pub async fn retry_with_backoff<F, Fut, T, E>(
    mut operation: F,
    max_retries: usize,
    initial_delay: Duration,
) -> Result<T, E>
where
    F: FnMut() -> Fut,
    Fut: Future<Output = Result<T, E>>,
    E: std::fmt::Debug,
{
    let mut delay = initial_delay;
    
    for attempt in 0..=max_retries {
        match operation().await {
            Ok(result) => return Ok(result),
            Err(e) => {
                if attempt == max_retries {
                    return Err(e);
                }
                
                println!("Attempt {} failed: {:?}, retrying in {:?}", attempt + 1, e, delay);
                sleep(delay).await;
                delay *= 2; // Exponential backoff
            }
        }
    }
    
    unreachable!()
}

// Retry with specific error types
pub async fn retry_on_specific_errors<F, Fut, T>(
    mut operation: F,
    max_retries: usize,
) -> Result<T, AppError>
where
    F: FnMut() -> Fut,
    Fut: Future<Output = Result<T, AppError>>,
{
    for attempt in 0..=max_retries {
        match operation().await {
            Ok(result) => return Ok(result),
            Err(e) => {
                if !e.is_retryable() || attempt == max_retries {
                    return Err(e);
                }
                
                println!("Retryable error on attempt {}: {:?}", attempt + 1, e);
                sleep(Duration::from_millis(100 * (attempt + 1) as u64)).await;
            }
        }
    }
    
    unreachable!()
}

// Circuit breaker pattern
pub struct CircuitBreaker {
    failure_count: std::sync::atomic::AtomicUsize,
    last_failure: std::sync::Mutex<Option<std::time::Instant>>,
    threshold: usize,
    timeout: Duration,
    state: std::sync::atomic::AtomicU8, // 0: Closed, 1: Open, 2: HalfOpen
}

impl CircuitBreaker {
    pub fn new(threshold: usize, timeout: Duration) -> Self {
        Self {
            failure_count: std::sync::atomic::AtomicUsize::new(0),
            last_failure: std::sync::Mutex::new(None),
            threshold,
            timeout,
            state: std::sync::atomic::AtomicU8::new(0),
        }
    }
    
    pub async fn call<F, Fut, T, E>(&self, operation: F) -> Result<T, E>
    where
        F: FnOnce() -> Fut,
        Fut: Future<Output = Result<T, E>>,
        E: std::fmt::Debug,
    {
        match self.state.load(std::sync::atomic::Ordering::Acquire) {
            0 => self.call_closed(operation).await, // Closed
            1 => self.call_open().await, // Open
            2 => self.call_half_open(operation).await, // HalfOpen
            _ => unreachable!(),
        }
    }
    
    async fn call_closed<F, Fut, T, E>(&self, operation: F) -> Result<T, E>
    where
        F: FnOnce() -> Fut,
        Fut: Future<Output = Result<T, E>>,
        E: std::fmt::Debug,
    {
        match operation().await {
            Ok(result) => {
                self.failure_count.store(0, std::sync::atomic::Ordering::Release);
                Ok(result)
            }
            Err(e) => {
                let count = self.failure_count.fetch_add(1, std::sync::atomic::Ordering::Release);
                if count + 1 >= self.threshold {
                    self.state.store(1, std::sync::atomic::Ordering::Release);
                    let mut last_failure = self.last_failure.lock().unwrap();
                    *last_failure = Some(std::time::Instant::now());
                }
                Err(e)
            }
        }
    }
    
    async fn call_open<E>(&self) -> Result<(), E> {
        let last_failure = self.last_failure.lock().unwrap();
        if let Some(time) = *last_failure {
            if time.elapsed() >= self.timeout {
                self.state.store(2, std::sync::atomic::Ordering::Release);
                return self.call_half_open(|| async { Ok(()) }).await;
            }
        }
        Err(anyhow::anyhow!("Circuit breaker is open").into())
    }
    
    async fn call_half_open<F, Fut, T, E>(&self, operation: F) -> Result<T, E>
    where
        F: FnOnce() -> Fut,
        Fut: Future<Output = Result<T, E>>,
        E: std::fmt::Debug,
    {
        match operation().await {
            Ok(result) => {
                self.state.store(0, std::sync::atomic::Ordering::Release);
                self.failure_count.store(0, std::sync::atomic::Ordering::Release);
                Ok(result)
            }
            Err(e) => {
                self.state.store(1, std::sync::atomic::Ordering::Release);
                let mut last_failure = self.last_failure.lock().unwrap();
                *last_failure = Some(std::time::Instant::now());
                Err(e)
            }
        }
    }
}
```

### Fallback Strategies

```rust
[fallback_strategies]
fallback_operations: true
degraded_modes: true
error_handling_strategies: true

[fallback_implementation]
// Fallback operation
pub async fn operation_with_fallback<F, G, Fut1, Fut2, T>(
    primary: F,
    fallback: G,
) -> Result<T, AppError>
where
    F: FnOnce() -> Fut1,
    G: FnOnce() -> Fut2,
    Fut1: Future<Output = Result<T, AppError>>,
    Fut2: Future<Output = Result<T, AppError>>,
{
    match primary().await {
        Ok(result) => Ok(result),
        Err(primary_error) => {
            println!("Primary operation failed: {:?}, trying fallback", primary_error);
            fallback().await
        }
    }
}

// Degraded mode
pub struct DegradedMode {
    primary_available: std::sync::atomic::AtomicBool,
    fallback_data: std::sync::Mutex<Option<String>>,
}

impl DegradedMode {
    pub fn new() -> Self {
        Self {
            primary_available: std::sync::atomic::AtomicBool::new(true),
            fallback_data: std::sync::Mutex::new(Some("Fallback data".to_string())),
        }
    }
    
    pub async fn get_data(&self) -> Result<String, AppError> {
        if self.primary_available.load(std::sync::atomic::Ordering::Acquire) {
            match self.primary_operation().await {
                Ok(data) => Ok(data),
                Err(e) => {
                    println!("Primary operation failed, switching to degraded mode: {:?}", e);
                    self.primary_available.store(false, std::sync::atomic::Ordering::Release);
                    self.fallback_operation().await
                }
            }
        } else {
            self.fallback_operation().await
        }
    }
    
    async fn primary_operation(&self) -> Result<String, AppError> {
        // Simulate primary operation
        if rand::random::<bool>() {
            Ok("Primary data".to_string())
        } else {
            Err(AppError::network("Primary service unavailable"))
        }
    }
    
    async fn fallback_operation(&self) -> Result<String, AppError> {
        let fallback_data = self.fallback_data.lock().unwrap();
        fallback_data.clone().ok_or_else(|| {
            AppError::unknown("No fallback data available")
        })
    }
    
    pub fn reset_primary(&self) {
        self.primary_available.store(true, std::sync::atomic::Ordering::Release);
    }
}
```

## 📊 Error Monitoring and Reporting

### Error Metrics

```rust
[error_monitoring]
error_metrics: true
error_reporting: true
error_aggregation: true

[monitoring_implementation]
use std::collections::HashMap;
use std::sync::atomic::{AtomicU64, Ordering};

// Error metrics collector
pub struct ErrorMetrics {
    error_counts: std::sync::Mutex<HashMap<String, AtomicU64>>,
    total_errors: AtomicU64,
    critical_errors: AtomicU64,
}

impl ErrorMetrics {
    pub fn new() -> Self {
        Self {
            error_counts: std::sync::Mutex::new(HashMap::new()),
            total_errors: AtomicU64::new(0),
            critical_errors: AtomicU64::new(0),
        }
    }
    
    pub fn record_error(&self, error: &AppError) {
        self.total_errors.fetch_add(1, Ordering::Relaxed);
        
        if error.is_critical() {
            self.critical_errors.fetch_add(1, Ordering::Relaxed);
        }
        
        let error_type = match error {
            AppError::Validation { .. } => "validation",
            AppError::Network { .. } => "network",
            AppError::Database { .. } => "database",
            AppError::Configuration { .. } => "configuration",
            AppError::Io(_) => "io",
            AppError::Parse(_) => "parse",
            AppError::Unknown { .. } => "unknown",
        };
        
        let mut counts = self.error_counts.lock().unwrap();
        let counter = counts.entry(error_type.to_string()).or_insert_with(|| AtomicU64::new(0));
        counter.fetch_add(1, Ordering::Relaxed);
    }
    
    pub fn get_stats(&self) -> ErrorStats {
        let counts = self.error_counts.lock().unwrap();
        let mut error_counts = HashMap::new();
        
        for (error_type, counter) in counts.iter() {
            error_counts.insert(error_type.clone(), counter.load(Ordering::Relaxed));
        }
        
        ErrorStats {
            total_errors: self.total_errors.load(Ordering::Relaxed),
            critical_errors: self.critical_errors.load(Ordering::Relaxed),
            error_counts,
        }
    }
}

#[derive(Debug, Clone)]
pub struct ErrorStats {
    pub total_errors: u64,
    pub critical_errors: u64,
    pub error_counts: HashMap<String, u64>,
}

// Error reporter
pub struct ErrorReporter {
    metrics: ErrorMetrics,
    error_log: std::sync::Mutex<Vec<ErrorReport>>,
}

#[derive(Debug, Clone)]
pub struct ErrorReport {
    pub timestamp: std::time::Instant,
    pub error_type: String,
    pub message: String,
    pub context: HashMap<String, String>,
}

impl ErrorReporter {
    pub fn new() -> Self {
        Self {
            metrics: ErrorMetrics::new(),
            error_log: std::sync::Mutex::new(Vec::new()),
        }
    }
    
    pub fn report_error(&self, error: &AppError, context: HashMap<String, String>) {
        self.metrics.record_error(error);
        
        let report = ErrorReport {
            timestamp: std::time::Instant::now(),
            error_type: match error {
                AppError::Validation { .. } => "validation".to_string(),
                AppError::Network { .. } => "network".to_string(),
                AppError::Database { .. } => "database".to_string(),
                AppError::Configuration { .. } => "configuration".to_string(),
                AppError::Io(_) => "io".to_string(),
                AppError::Parse(_) => "parse".to_string(),
                AppError::Unknown { .. } => "unknown".to_string(),
            },
            message: error.to_string(),
            context,
        };
        
        let mut log = self.error_log.lock().unwrap();
        log.push(report);
        
        // Keep only last 1000 errors
        if log.len() > 1000 {
            log.remove(0);
        }
    }
    
    pub fn get_recent_errors(&self, count: usize) -> Vec<ErrorReport> {
        let log = self.error_log.lock().unwrap();
        log.iter().rev().take(count).cloned().collect()
    }
    
    pub fn get_stats(&self) -> ErrorStats {
        self.metrics.get_stats()
    }
}
```

## 🎯 Best Practices

### 1. **Error Type Design**
- Use specific error types for different domains
- Implement proper error conversion with From
- Provide context and backtrace information
- Use thiserror for custom error types

### 2. **Error Propagation**
- Use the ? operator for concise error propagation
- Implement proper error conversion
- Add context to errors when propagating
- Handle errors at appropriate levels

### 3. **Error Recovery**
- Implement retry mechanisms with exponential backoff
- Use circuit breakers for external dependencies
- Provide fallback strategies
- Implement degraded modes

### 4. **Error Monitoring**
- Collect error metrics and statistics
- Implement error reporting and logging
- Monitor error patterns and trends
- Set up alerts for critical errors

### 5. **Testing**
- Test error conditions thoroughly
- Mock error scenarios
- Test error recovery mechanisms
- Validate error messages and context

Error handling patterns with TuskLang and Rust provide robust, type-safe error management with comprehensive recovery mechanisms and monitoring capabilities. 