# 🦀 Custom Directives in TuskLang - Rust Edition

**"Extend the language with zero-cost abstractions" - Rust Edition**

TuskLang allows you to create custom directives to extend the language with domain-specific functionality, making your Rust code more expressive and maintainable while leveraging Rust's powerful macro system and type safety.

## Defining Custom Directives with Rust Macros

```rust
use tusklang_rust::{define_directive, DirectiveHandler, DirectiveContext};
use proc_macro::TokenStream;
use quote::quote;
use syn::{parse_macro_input, Ident, LitStr};

// Basic custom directive with Rust proc macro
#[define_directive(log_execution)]
pub fn log_execution_directive(input: TokenStream) -> TokenStream {
    let input = parse_macro_input!(input as DirectiveInput);
    
    let expanded = quote! {
        {
            let start_time = std::time::Instant::now();
            tracing::debug!("Executing: {}", stringify!(#input));
            
            let result = {
                #input
            };
            
            let duration = start_time.elapsed();
            tracing::debug!("Completed in {:?}", duration);
            
            result
        }
    };
    
    TokenStream::from(expanded)
}

// Usage in TuskLang
let tsk_content = r#"
#log_execution {
    result: @complex_calculation()
    @save_result(result)
}
"#;
```

## Directive Structure with Rust Types

```rust
use tusklang_rust::{DirectiveDefinition, DirectiveParser, DirectiveHandler};
use serde::{Deserialize, Serialize};
use std::collections::HashMap;

#[derive(Debug, Deserialize, Serialize)]
struct FeatureDirectiveArgs {
    name: String,
    condition: Option<String>,
}

#[derive(Debug)]
struct FeatureDirective {
    pattern: regex::Regex,
    parser: Box<dyn DirectiveParser<FeatureDirectiveArgs>>,
    handler: Box<dyn DirectiveHandler<FeatureDirectiveArgs>>,
}

impl FeatureDirective {
    fn new() -> Self {
        Self {
            pattern: regex::Regex::new(r"^(\w+)(?:\s+if:\s*(.+))?$").unwrap(),
            parser: Box::new(FeatureParser),
            handler: Box::new(FeatureHandler),
        }
    }
}

struct FeatureParser;

impl DirectiveParser<FeatureDirectiveArgs> for FeatureParser {
    fn parse(&self, matches: &regex::Captures) -> Result<FeatureDirectiveArgs, Box<dyn std::error::Error>> {
        Ok(FeatureDirectiveArgs {
            name: matches[1].to_string(),
            condition: matches.get(2).map(|m| m.as_str().to_string()),
        })
    }
}

struct FeatureHandler;

impl DirectiveHandler<FeatureDirectiveArgs> for FeatureHandler {
    fn handle(
        &self,
        args: &FeatureDirectiveArgs,
        code: &str,
        context: &DirectiveContext,
    ) -> Result<String, Box<dyn std::error::Error>> {
        let condition = args.condition.as_deref().unwrap_or("true");
        
        Ok(format!(
            r#"
            if (feature_enabled("{}") && ({})) {{
                {}
            }}
            "#,
            args.name, condition, code
        ))
    }
}

// Register custom directive
let feature_directive = FeatureDirective::new();
directive_registry.register("feature", feature_directive)?;

// Usage
let tsk_content = r#"
#feature dark_mode if: @user.preferences.theme == "dark" {
    @apply_dark_theme()
}
"#;
```

## Route-Style Directives with Rust Web Frameworks

```rust
use tusklang_rust::{WebSocketDirective, WebSocketHandler};
use actix_web::{web, HttpRequest, HttpResponse};
use actix_web_actors::ws;
use serde::{Deserialize, Serialize};
use tokio::sync::broadcast;

#[derive(Debug, Deserialize, Serialize)]
struct ChatMessage {
    room: String,
    message: String,
    user_id: String,
}

#[derive(Debug)]
struct ChatWebSocket {
    room: String,
    user_id: String,
    tx: broadcast::Sender<ChatMessage>,
}

impl ChatWebSocket {
    async fn handle_message(&self, msg: ChatMessage) -> Result<(), Box<dyn std::error::Error>> {
        if msg.room == self.room {
            self.tx.send(msg)?;
        }
        Ok(())
    }
}

// Custom WebSocket directive
#[define_directive(websocket)]
pub fn websocket_directive(input: TokenStream) -> TokenStream {
    let input = parse_macro_input!(input as WebSocketDirectiveInput);
    
    let expanded = quote! {
        pub async fn websocket_handler(
            req: HttpRequest,
            stream: web::Payload,
            path: web::Path<String>,
        ) -> Result<HttpResponse, actix_web::Error> {
            let chat_ws = ChatWebSocket {
                room: path.into_inner(),
                user_id: "user_id".to_string(), // Extract from auth
                tx: broadcast::channel::<ChatMessage>(100).0,
            };
            
            let resp = ws::start(chat_ws, &req, stream)?;
            Ok(resp)
        }
    };
    
    TokenStream::from(expanded)
}

// Usage in TuskLang
let tsk_content = r#"
#websocket /chat {
    @ws.on("message", (data) => {
        message: @json.decode(data)
        @broadcast_to_room(message.room, message)
    })
    
    @ws.on("close", () => {
        @remove_from_room(@request.user)
    })
}
"#;
```

## Decorator Directives with Rust Attributes

```rust
use tusklang_rust::{CachedDirective, CacheBackend};
use std::collections::HashMap;
use std::sync::Arc;
use tokio::sync::RwLock;

#[derive(Debug, Clone)]
struct CacheEntry {
    value: serde_json::Value,
    expires_at: std::time::SystemTime,
}

#[derive(Debug)]
struct MemoryCache {
    store: Arc<RwLock<HashMap<String, CacheEntry>>>,
}

impl MemoryCache {
    fn new() -> Self {
        Self {
            store: Arc::new(RwLock::new(HashMap::new())),
        }
    }
    
    async fn get(&self, key: &str) -> Option<serde_json::Value> {
        let store = self.store.read().await;
        if let Some(entry) = store.get(key) {
            if entry.expires_at > std::time::SystemTime::now() {
                return Some(entry.value.clone());
            }
        }
        None
    }
    
    async fn set(&self, key: String, value: serde_json::Value, ttl: u64) {
        let expires_at = std::time::SystemTime::now() + std::time::Duration::from_secs(ttl);
        let entry = CacheEntry { value, expires_at };
        
        let mut store = self.store.write().await;
        store.insert(key, entry);
    }
}

// Cached directive with Rust async support
#[define_directive(cached)]
pub fn cached_directive(input: TokenStream) -> TokenStream {
    let input = parse_macro_input!(input as CachedDirectiveInput);
    
    let expanded = quote! {
        async fn cached_function() -> Result<serde_json::Value, Box<dyn std::error::Error>> {
            let cache = MemoryCache::new();
            let cache_key = format!("{}:{}", stringify!(#input), #input.ttl);
            
            if let Some(cached_value) = cache.get(&cache_key).await {
                return Ok(cached_value);
            }
            
            let result = {
                #input
            };
            
            cache.set(cache_key, result.clone(), #input.ttl).await;
            Ok(result)
        }
    };
    
    TokenStream::from(expanded)
}

// Usage
let tsk_content = r#"
#cached 7200 key: "user_stats:{user_id}" {
    get_user_stats: (user_id) => {
        stats: @db.query("SELECT ... expensive query ...")
        return @process_stats(stats)
    }
}
"#;
```

## Validation Directives with Rust Serde

```rust
use tusklang_rust::{ValidationDirective, ValidationRule};
use serde::{Deserialize, Serialize};
use validator::{Validate, ValidationError};
use std::collections::HashMap;

#[derive(Debug, Deserialize, Serialize, Validate)]
struct UserInput {
    #[validate(length(min = 1, max = 255))]
    name: String,
    
    #[validate(email)]
    email: String,
    
    #[validate(range(min = 18))]
    age: u32,
}

#[derive(Debug)]
struct ValidationDirectiveHandler {
    rules: HashMap<String, Vec<ValidationRule>>,
}

impl ValidationDirectiveHandler {
    fn new() -> Self {
        let mut rules = HashMap::new();
        rules.insert("name".to_string(), vec![
            ValidationRule::Required,
            ValidationRule::String,
            ValidationRule::MaxLength(255),
        ]);
        rules.insert("email".to_string(), vec![
            ValidationRule::Required,
            ValidationRule::Email,
        ]);
        rules.insert("age".to_string(), vec![
            ValidationRule::Required,
            ValidationRule::Integer,
            ValidationRule::Min(18),
        ]);
        
        Self { rules }
    }
    
    fn validate(&self, input: &UserInput) -> Result<(), Vec<ValidationError>> {
        input.validate()
    }
}

// Validation directive with Rust trait system
#[define_directive(validate)]
pub fn validate_directive(input: TokenStream) -> TokenStream {
    let input = parse_macro_input!(input as ValidationDirectiveInput);
    
    let expanded = quote! {
        {
            let user_input = UserInput {
                name: #input.name,
                email: #input.email,
                age: #input.age,
            };
            
            if let Err(errors) = user_input.validate() {
                return Err(ValidationError::new(errors));
            }
            
            // Continue with validated input
            #input
        }
    };
    
    TokenStream::from(expanded)
}

// Usage
let tsk_content = r#"
#validate {
    name: "required|string|max:255"
    email: "required|email"
    age: "required|integer|min:18"
} {
    #api /users method: POST {
        # Validation runs first
        user: @User.create(@request.validated)
        return user
    }
}
"#;
```

## Async Directives with Rust Tokio

```rust
use tusklang_rust::{AsyncDirective, AsyncPool};
use tokio::runtime::Runtime;
use std::sync::Arc;
use futures::future::BoxFuture;

#[derive(Debug)]
struct AsyncPoolManager {
    runtime: Arc<Runtime>,
    pools: HashMap<String, Arc<Runtime>>,
}

impl AsyncPoolManager {
    fn new() -> Self {
        Self {
            runtime: Arc::new(Runtime::new().unwrap()),
            pools: HashMap::new(),
        }
    }
    
    fn get_pool(&mut self, name: &str) -> Arc<Runtime> {
        if let Some(pool) = self.pools.get(name) {
            pool.clone()
        } else {
            let pool = Arc::new(Runtime::new().unwrap());
            self.pools.insert(name.to_string(), pool.clone());
            pool
        }
    }
    
    async fn execute_with_timeout<F, T>(
        &self,
        pool: &str,
        timeout: Option<std::time::Duration>,
        task: F,
    ) -> Result<T, Box<dyn std::error::Error>>
    where
        F: FnOnce() -> BoxFuture<'static, Result<T, Box<dyn std::error::Error>>> + Send + 'static,
        T: Send + 'static,
    {
        let pool = self.get_pool(pool);
        
        if let Some(timeout_duration) = timeout {
            tokio::time::timeout(timeout_duration, pool.spawn(task())).await??
        } else {
            pool.spawn(task()).await?
        }
    }
}

// Async directive with Rust async/await
#[define_directive(async)]
pub fn async_directive(input: TokenStream) -> TokenStream {
    let input = parse_macro_input!(input as AsyncDirectiveInput);
    
    let expanded = quote! {
        {
            let pool_manager = AsyncPoolManager::new();
            let result = pool_manager
                .execute_with_timeout(
                    #input.pool,
                    #input.timeout.map(|t| std::time::Duration::from_secs(t)),
                    || Box::pin(async {
                        #input
                    }),
                )
                .await?;
            
            result
        }
    };
    
    TokenStream::from(expanded)
}

// Usage
let tsk_content = r#"
#async pool: heavy_tasks timeout: 30 {
    result: await @expensive_async_operation()
    await @save_result(result)
}
"#;
```

## Database Directives with Rust ORMs

```rust
use tusklang_rust::{DatabaseDirective, DatabaseConnection};
use sqlx::{PgPool, Row};
use serde::{Deserialize, Serialize};

#[derive(Debug, Deserialize, Serialize)]
struct User {
    id: i32,
    name: String,
    email: String,
    created_at: chrono::DateTime<chrono::Utc>,
}

#[derive(Debug)]
struct DatabaseDirectiveHandler {
    pool: PgPool,
}

impl DatabaseDirectiveHandler {
    fn new(pool: PgPool) -> Self {
        Self { pool }
    }
    
    async fn transaction<F, T>(&self, callback: F) -> Result<T, Box<dyn std::error::Error>>
    where
        F: for<'a> FnOnce(&'a mut sqlx::Transaction<'_, sqlx::Postgres>) -> BoxFuture<'a, Result<T, Box<dyn std::error::Error>>>,
    {
        let mut tx = self.pool.begin().await?;
        let result = callback(&mut tx).await?;
        tx.commit().await?;
        Ok(result)
    }
}

// Database transaction directive
#[define_directive(transaction)]
pub fn transaction_directive(input: TokenStream) -> TokenStream {
    let input = parse_macro_input!(input as TransactionDirectiveInput);
    
    let expanded = quote! {
        {
            let db_handler = DatabaseDirectiveHandler::new(pool.clone());
            db_handler.transaction(|tx| Box::pin(async move {
                #input
            })).await?
        }
    };
    
    TokenStream::from(expanded)
}

// Usage
let tsk_content = r#"
#transaction {
    # Create user
    user: @User.create({
        name: @request.post.name,
        email: @request.post.email
    })
    
    # Create profile
    profile: @Profile.create({
        user_id: user.id,
        bio: @request.post.bio
    })
    
    return {user, profile}
}
"#;
```

## Security Directives with Rust Cryptography

```rust
use tusklang_rust::{SecurityDirective, SecurityContext};
use argon2::{Argon2, PasswordHash, PasswordHasher, PasswordVerifier};
use jsonwebtoken::{decode, encode, DecodingKey, EncodingKey, Header, Validation};
use serde::{Deserialize, Serialize};
use rand::Rng;

#[derive(Debug, Deserialize, Serialize)]
struct Claims {
    sub: String,
    exp: usize,
    iat: usize,
}

#[derive(Debug)]
struct SecurityDirectiveHandler {
    jwt_secret: String,
    argon2: Argon2<'static>,
}

impl SecurityDirectiveHandler {
    fn new(jwt_secret: String) -> Self {
        Self {
            jwt_secret,
            argon2: Argon2::default(),
        }
    }
    
    fn hash_password(&self, password: &str) -> Result<String, Box<dyn std::error::Error>> {
        let salt = rand::thread_rng().gen::<[u8; 16]>();
        let hash = self.argon2.hash_password(password.as_bytes(), &salt)?;
        Ok(hash.to_string())
    }
    
    fn verify_password(&self, password: &str, hash: &str) -> Result<bool, Box<dyn std::error::Error>> {
        let parsed_hash = PasswordHash::new(hash)?;
        Ok(Argon2::default().verify_password(password.as_bytes(), &parsed_hash).is_ok())
    }
    
    fn generate_jwt(&self, claims: &Claims) -> Result<String, Box<dyn std::error::Error>> {
        let token = encode(
            &Header::default(),
            claims,
            &EncodingKey::from_secret(self.jwt_secret.as_ref()),
        )?;
        Ok(token)
    }
    
    fn verify_jwt(&self, token: &str) -> Result<Claims, Box<dyn std::error::Error>> {
        let token_data = decode::<Claims>(
            token,
            &DecodingKey::from_secret(self.jwt_secret.as_ref()),
            &Validation::default(),
        )?;
        Ok(token_data.claims)
    }
}

// Security directive with Rust cryptography
#[define_directive(secure)]
pub fn secure_directive(input: TokenStream) -> TokenStream {
    let input = parse_macro_input!(input as SecureDirectiveInput);
    
    let expanded = quote! {
        {
            let security_handler = SecurityDirectiveHandler::new(
                std::env::var("JWT_SECRET").expect("JWT_SECRET must be set")
            );
            
            // Apply security measures
            #input
        }
    };
    
    TokenStream::from(expanded)
}

// Usage
let tsk_content = r#"
#secure {
    #api /login method: POST {
        password_hash: @security.hash_password(@request.post.password)
        token: @security.generate_jwt({
            sub: @request.post.email,
            exp: @now() + 3600
        })
        
        return {token}
    }
}
"#;
```

## Testing Directives with Rust Testing Frameworks

```rust
use tusklang_rust::{TestDirective, TestContext};
use tokio_test::{assert_ok, assert_err};
use serde_json::Value;

#[derive(Debug)]
struct TestDirectiveHandler {
    test_context: TestContext,
}

impl TestDirectiveHandler {
    fn new() -> Self {
        Self {
            test_context: TestContext::new(),
        }
    }
    
    async fn run_test<F>(&self, name: &str, test_fn: F) -> Result<(), Box<dyn std::error::Error>>
    where
        F: FnOnce(&TestContext) -> BoxFuture<'static, Result<(), Box<dyn std::error::Error>>>,
    {
        println!("Running test: {}", name);
        let result = test_fn(&self.test_context).await;
        
        match &result {
            Ok(_) => println!("✓ Test passed: {}", name),
            Err(e) => println!("✗ Test failed: {}", e),
        }
        
        result
    }
}

// Test directive with Rust testing
#[define_directive(test)]
pub fn test_directive(input: TokenStream) -> TokenStream {
    let input = parse_macro_input!(input as TestDirectiveInput);
    
    let expanded = quote! {
        #[tokio::test]
        async fn #input.test_name() -> Result<(), Box<dyn std::error::Error>> {
            let test_handler = TestDirectiveHandler::new();
            test_handler.run_test(stringify!(#input.test_name), |ctx| Box::pin(async move {
                #input
            })).await
        }
    };
    
    TokenStream::from(expanded)
}

// Usage
let tsk_content = r#"
#test "user_creation" {
    # Create test user
    user: @User.create({
        name: "Test User",
        email: "test@example.com"
    })
    
    # Assert user was created
    assert: @user.id != null
    assert: @user.name == "Test User"
    assert: @user.email == "test@example.com"
}
"#;
```

## Performance Monitoring Directives

```rust
use tusklang_rust::{PerformanceDirective, MetricsCollector};
use std::time::Instant;
use metrics::{counter, histogram, gauge};

#[derive(Debug)]
struct PerformanceDirectiveHandler {
    metrics: MetricsCollector,
}

impl PerformanceDirectiveHandler {
    fn new() -> Self {
        Self {
            metrics: MetricsCollector::new(),
        }
    }
    
    async fn measure_performance<F, T>(
        &self,
        operation_name: &str,
        operation: F,
    ) -> Result<T, Box<dyn std::error::Error>>
    where
        F: FnOnce() -> BoxFuture<'static, Result<T, Box<dyn std::error::Error>>>,
    {
        let start = Instant::now();
        counter!("operations_total", 1, "operation" => operation_name.to_string());
        
        let result = operation().await;
        
        let duration = start.elapsed();
        histogram!("operation_duration", duration.as_secs_f64(), "operation" => operation_name.to_string());
        
        match &result {
            Ok(_) => counter!("operations_success", 1, "operation" => operation_name.to_string()),
            Err(_) => counter!("operations_error", 1, "operation" => operation_name.to_string()),
        }
        
        result
    }
}

// Performance monitoring directive
#[define_directive(monitor)]
pub fn monitor_directive(input: TokenStream) -> TokenStream {
    let input = parse_macro_input!(input as MonitorDirectiveInput);
    
    let expanded = quote! {
        {
            let perf_handler = PerformanceDirectiveHandler::new();
            perf_handler.measure_performance(stringify!(#input.operation), || Box::pin(async {
                #input
            })).await?
        }
    };
    
    TokenStream::from(expanded)
}

// Usage
let tsk_content = r#"
#monitor "database_query" {
    users: @User.all()
    return users
}
"#;
```

## Error Handling Directives

```rust
use tusklang_rust::{ErrorDirective, ErrorHandler};
use thiserror::Error;
use tracing::{error, warn, info};

#[derive(Error, Debug)]
enum AppError {
    #[error("Database error: {0}")]
    Database(#[from] sqlx::Error),
    
    #[error("Validation error: {0}")]
    Validation(String),
    
    #[error("Authentication error: {0}")]
    Authentication(String),
}

#[derive(Debug)]
struct ErrorDirectiveHandler {
    error_handler: ErrorHandler,
}

impl ErrorDirectiveHandler {
    fn new() -> Self {
        Self {
            error_handler: ErrorHandler::new(),
        }
    }
    
    async fn handle_with_retry<F, T>(
        &self,
        max_retries: u32,
        operation: F,
    ) -> Result<T, Box<dyn std::error::Error>>
    where
        F: Fn() -> BoxFuture<'static, Result<T, Box<dyn std::error::Error>>> + Send + Sync + 'static,
    {
        let mut attempts = 0;
        loop {
            match operation().await {
                Ok(result) => return Ok(result),
                Err(e) => {
                    attempts += 1;
                    if attempts >= max_retries {
                        return Err(e);
                    }
                    warn!("Retry attempt {} after error: {}", attempts, e);
                    tokio::time::sleep(tokio::time::Duration::from_secs(2u64.pow(attempts))).await;
                }
            }
        }
    }
}

// Error handling directive
#[define_directive(retry)]
pub fn retry_directive(input: TokenStream) -> TokenStream {
    let input = parse_macro_input!(input as RetryDirectiveInput);
    
    let expanded = quote! {
        {
            let error_handler = ErrorDirectiveHandler::new();
            error_handler.handle_with_retry(#input.max_retries, || Box::pin(async {
                #input
            })).await?
        }
    };
    
    TokenStream::from(expanded)
}

// Usage
let tsk_content = r#"
#retry 3 {
    result: @external_api.call()
    return result
}
"#;
```

## Integration with Rust Web Frameworks

```rust
use tusklang_rust::{WebFrameworkIntegration, ActixIntegration, AxumIntegration};
use actix_web::{web, App, HttpServer, Responder};
use axum::{routing, Router};

// Actix Web integration
#[define_directive(actix_route)]
pub fn actix_route_directive(input: TokenStream) -> TokenStream {
    let input = parse_macro_input!(input as ActixRouteInput);
    
    let expanded = quote! {
        async fn #input.handler_name(
            req: web::HttpRequest,
            payload: web::Payload,
        ) -> impl Responder {
            #input
        }
    };
    
    TokenStream::from(expanded)
}

// Axum integration
#[define_directive(axum_route)]
pub fn axum_route_directive(input: TokenStream) -> TokenStream {
    let input = parse_macro_input!(input as AxumRouteInput);
    
    let expanded = quote! {
        async fn #input.handler_name(
            req: axum::extract::Request,
        ) -> axum::response::Response {
            #input
        }
    };
    
    TokenStream::from(expanded)
}

// Usage with Actix Web
let tsk_content = r#"
#actix_route /api/users {
    #api /users {
        users: @User.all()
        return users
    }
}
"#;

// Usage with Axum
let tsk_content = r#"
#axum_route /api/users {
    #api /users {
        users: @User.all()
        return users
    }
}
"#;
```

## Best Practices for Custom Directives

### 1. **Type Safety**
```rust
// Always use strong typing for directive arguments
#[derive(Debug, Deserialize, Serialize)]
struct DirectiveArgs {
    required_field: String,
    optional_field: Option<String>,
    numeric_field: u32,
}
```

### 2. **Error Handling**
```rust
// Provide meaningful error messages
impl std::fmt::Display for DirectiveError {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            DirectiveError::InvalidArgs(msg) => write!(f, "Invalid arguments: {}", msg),
            DirectiveError::ExecutionFailed(e) => write!(f, "Execution failed: {}", e),
        }
    }
}
```

### 3. **Performance Considerations**
```rust
// Use async/await for I/O operations
async fn handle_directive(&self, args: &DirectiveArgs) -> Result<String, Box<dyn std::error::Error>> {
    // Use async operations
    let result = self.async_operation(args).await?;
    Ok(result)
}
```

### 4. **Testing**
```rust
#[cfg(test)]
mod tests {
    use super::*;
    
    #[tokio::test]
    async fn test_custom_directive() {
        let directive = CustomDirective::new();
        let result = directive.handle(&test_args).await;
        assert!(result.is_ok());
    }
}
```

### 5. **Documentation**
```rust
/// Custom directive for feature flag management
/// 
/// # Examples
/// 
/// ```
/// #feature dark_mode if: @user.preferences.theme == "dark" {
///     @apply_dark_theme()
/// }
/// ```
#[define_directive(feature)]
pub fn feature_directive(input: TokenStream) -> TokenStream {
    // Implementation
}
```

## Security Considerations

### 1. **Input Validation**
```rust
// Always validate directive inputs
fn validate_args(args: &DirectiveArgs) -> Result<(), ValidationError> {
    if args.required_field.is_empty() {
        return Err(ValidationError::new("required_field cannot be empty"));
    }
    Ok(())
}
```

### 2. **Access Control**
```rust
// Check permissions before executing directives
fn check_permissions(&self, context: &DirectiveContext) -> Result<(), PermissionError> {
    if !context.user.has_permission("execute_custom_directive") {
        return Err(PermissionError::new("Insufficient permissions"));
    }
    Ok(())
}
```

### 3. **Resource Limits**
```rust
// Implement resource limits for custom directives
fn check_resource_limits(&self, directive: &str) -> Result<(), ResourceLimitError> {
    let usage = self.get_current_usage(directive);
    if usage > self.get_limit(directive) {
        return Err(ResourceLimitError::new("Resource limit exceeded"));
    }
    Ok(())
}
```

## Performance Optimization

### 1. **Caching**
```rust
// Cache directive results when appropriate
impl DirectiveCache for CustomDirective {
    fn cache_key(&self, args: &DirectiveArgs) -> String {
        format!("directive:{}:{}", self.name(), args.hash())
    }
    
    fn cache_ttl(&self) -> std::time::Duration {
        std::time::Duration::from_secs(300) // 5 minutes
    }
}
```

### 2. **Async Execution**
```rust
// Use async execution for I/O-bound operations
async fn execute_directive(&self, args: &DirectiveArgs) -> Result<String, Box<dyn std::error::Error>> {
    let result = tokio::spawn(async move {
        // Expensive operation
        expensive_operation(args).await
    }).await??;
    
    Ok(result)
}
```

### 3. **Resource Pooling**
```rust
// Use connection pools for database operations
#[derive(Debug)]
struct DatabaseDirective {
    pool: PgPool,
}

impl DatabaseDirective {
    async fn execute(&self, query: &str) -> Result<Vec<Row>, sqlx::Error> {
        sqlx::query(query).fetch_all(&self.pool).await
    }
}
```

## Testing Strategies

### 1. **Unit Testing**
```rust
#[cfg(test)]
mod tests {
    use super::*;
    
    #[test]
    fn test_directive_parsing() {
        let directive = CustomDirective::new();
        let args = directive.parse("#custom arg1 arg2").unwrap();
        assert_eq!(args.arg1, "arg1");
        assert_eq!(args.arg2, "arg2");
    }
}
```

### 2. **Integration Testing**
```rust
#[tokio::test]
async fn test_directive_integration() {
    let app = create_test_app().await;
    let client = test::init_service(app).await;
    
    let response = client
        .post("/api/test")
        .json(&test_data)
        .send()
        .await
        .unwrap();
    
    assert_eq!(response.status(), StatusCode::OK);
}
```

### 3. **Performance Testing**
```rust
#[tokio::test]
async fn test_directive_performance() {
    let directive = CustomDirective::new();
    let start = Instant::now();
    
    for _ in 0..1000 {
        directive.execute(&test_args).await.unwrap();
    }
    
    let duration = start.elapsed();
    assert!(duration < std::time::Duration::from_secs(1));
}
```

## Conclusion

Custom directives in TuskLang with Rust provide a powerful way to extend the language with domain-specific functionality while leveraging Rust's type safety, performance, and ecosystem. By following the patterns and best practices outlined in this guide, you can create robust, maintainable, and performant custom directives that integrate seamlessly with your Rust applications.

Remember to:
- Use Rust's type system for safety
- Implement proper error handling
- Follow async/await patterns for I/O operations
- Write comprehensive tests
- Consider security implications
- Optimize for performance
- Document your directives thoroughly

With these principles, you can build custom directives that enhance your TuskLang applications while maintaining the reliability and performance that Rust provides. 