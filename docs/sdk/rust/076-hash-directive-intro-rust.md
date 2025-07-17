# 🦀 Hash (#) Directives Introduction - Rust Edition

**"We don't bow to any king" - Rust Edition**

Hash directives in TuskLang provide powerful meta-programming capabilities for Rust applications, enabling declarative API definitions, middleware composition, and execution context management with zero-copy performance.

## What are Hash Directives?

Hash directives are special instructions that start with `#` and define how your Rust code should be executed. They provide execution contexts, routing rules, scheduling, and more - all with Rust's safety guarantees and performance characteristics.

```rust
use tusklang_rust::{parse, Directive, DirectiveContext};

// Basic directive syntax in Rust
let tsk_content = r#"
#web /hello {
    response: "Hello, World!"
    status_code: 200
}
"#;

let directives = parse(tsk_content)?;
```

## Core Directives Overview

```rust
use tusklang_rust::{Directive, WebDirective, ApiDirective, CliDirective, CronDirective};

// Web endpoints with Rust integration
let web_directive = r#"
#web /api/users {
    handler: "UserController::index"
    middleware: ["auth", "logging"]
    rate_limit: "100/hour"
}
"#;

// API endpoints with automatic JSON serialization
let api_directive = r#"
#api /api/data {
    handler: "DataController::show"
    response_type: "json"
    cache: "5m"
}
"#;

// Command-line interface with Rust CLI integration
let cli_directive = r#"
#cli process --file {
    handler: "FileProcessor::process"
    args: ["file_path"]
    help: "Process a file"
}
"#;

// Scheduled tasks with Rust async support
let cron_directive = r#"
#cron "0 * * * *" {
    handler: "BackgroundJob::hourly_cleanup"
    async: true
    timeout: "30s"
}
"#;

// Middleware with Rust trait system
let middleware_directive = r#"
#middleware auth {
    handler: "AuthMiddleware::authenticate"
    priority: 100
    async: true
}
"#;
```

## Directive Structure in Rust

```rust
use tusklang_rust::{Directive, DirectiveContext, DirectiveHandler};
use serde::{Deserialize, Serialize};

#[derive(Debug, Deserialize, Serialize)]
struct WebDirectiveConfig {
    route: String,
    method: Option<String>,
    middleware: Option<Vec<String>>,
    handler: String,
    rate_limit: Option<String>,
}

// Full directive syntax with Rust types
let directive = r#"
#web /admin/users {
    method: "GET"
    middleware: ["auth", "admin"]
    handler: "AdminController::list_users"
    rate_limit: "50/hour"
    cache: "2m"
}
"#;

// Parse into Rust struct
let config: WebDirectiveConfig = parse_into(directive)?;
```

## Common Patterns with Rust Integration

```rust
use tusklang_rust::{parse, DirectiveRouter, RouteHandler};
use actix_web::{web, HttpResponse, Responder};

// RESTful routes with Actix-web integration
let rest_routes = r#"
#web /users {
    method: "GET"
    handler: "UserController::index"
    response_type: "json"
}

#web /users/{id} {
    method: "GET"
    handler: "UserController::show"
    params: ["id"]
}

#api /users method: POST {
    handler: "UserController::create"
    validation: {
        name: "required|string|max:255"
        email: "required|email|unique:users"
    }
}
"#;

// Grouped routes with Rust module system
let grouped_routes = r#"
#group /api/v1 {
    #web /users {
        handler: "v1::UserController::index"
    }
    
    #web /posts {
        handler: "v1::PostController::index"
    }
}
"#;
```

## Directive Modifiers with Rust Types

```rust
use tusklang_rust::{DirectiveModifier, Method, Middleware};

#[derive(Debug, Deserialize)]
struct DirectiveConfig {
    method: Option<Method>,
    middleware: Option<Vec<Middleware>>,
    rate_limit: Option<String>,
    cache: Option<String>,
    async: Option<bool>,
}

// Method specification with Rust enums
let method_directive = r#"
#web /users method: GET {
    handler: "UserController::index"
}
"#;

// Multiple methods with Rust array syntax
let multi_method = r#"
#web /users method: [GET, POST] {
    handler: "UserController::handle"
}
"#;

// Middleware application with Rust trait system
let middleware_directive = r#"
#web /secure middleware: [auth, logging] {
    handler: "SecureController::index"
}
"#;
```

## Custom Directives with Rust Macros

```rust
use tusklang_rust::{define_directive, DirectiveHandler};
use proc_macro::TokenStream;

// Define custom directive with Rust macro
#[define_directive(webhook)]
pub fn webhook_directive(input: TokenStream) -> TokenStream {
    let tsk_content = r#"
#define_directive webhook {
    pattern: /^\/webhooks\/(.+)$/
    
    handler: (match, block) => {
        webhook_type: match[1]
        
        return {
            method: "POST"
            middleware: ["verify_webhook"]
            handler: "WebhookHandler::process"
        }
    }
}
"#;
    
    // Generate Rust code from directive
    generate_webhook_handler(input)
}

// Use custom directive
let webhook_config = r#"
#webhook /github {
    handler: "GitHubWebhook::handle"
    events: ["push", "pull_request"]
    secret: @env.GITHUB_WEBHOOK_SECRET
}
"#;
```

## Directive Context with Rust Traits

```rust
use tusklang_rust::{DirectiveContext, Request, Response, Params};
use std::collections::HashMap;

// Each directive provides context through Rust traits
trait WebContext {
    fn request(&self) -> &Request;
    fn response(&self) -> &mut Response;
    fn params(&self) -> &HashMap<String, String>;
    fn session(&self) -> &HashMap<String, String>;
}

// Web context implementation
impl WebContext for WebDirectiveContext {
    fn request(&self) -> &Request {
        &self.request
    }
    
    fn response(&self) -> &mut Response {
        &mut self.response
    }
    
    fn params(&self) -> &HashMap<String, String> {
        &self.params
    }
    
    fn session(&self) -> &HashMap<String, String> {
        &self.session
    }
}

// CLI context with Rust CLI patterns
trait CliContext {
    fn args(&self) -> &Vec<String>;
    fn options(&self) -> &HashMap<String, String>;
    fn input(&self) -> &str;
    fn output(&mut self) -> &mut String;
}

// Cron context with Rust async patterns
trait CronContext {
    fn schedule(&self) -> &str;
    fn last_run(&self) -> Option<chrono::DateTime<chrono::Utc>>;
    fn next_run(&self) -> chrono::DateTime<chrono::Utc>;
}
```

## Directive Composition with Rust Traits

```rust
use tusklang_rust::{DirectiveComposer, MiddlewareChain};

// Combine multiple directives with Rust trait composition
trait Authenticated {
    fn requires_auth(&self) -> bool;
    fn auth_middleware(&self) -> Vec<String>;
}

impl Authenticated for WebDirective {
    fn requires_auth(&self) -> bool {
        self.middleware.contains(&"auth".to_string())
    }
    
    fn auth_middleware(&self) -> Vec<String> {
        vec!["auth".to_string()]
    }
}

// Directive inheritance with Rust trait bounds
trait BaseApi: Authenticated + RateLimited + Cached {
    fn base_middleware(&self) -> Vec<String> {
        let mut middleware = self.auth_middleware();
        middleware.extend(self.rate_limit_middleware());
        middleware.extend(self.cache_middleware());
        middleware
    }
}

// Conditional composition with Rust conditional compilation
#[cfg(feature = "feature_x")]
let feature_directive = r#"
#web /new-feature {
    handler: "FeatureController::index"
    feature_flag: "new_ui"
}
"#;
```

## Error Handling with Rust Result Types

```rust
use tusklang_rust::{DirectiveError, DirectiveResult};
use thiserror::Error;

#[derive(Error, Debug)]
pub enum DirectiveError {
    #[error("Invalid directive syntax: {0}")]
    SyntaxError(String),
    
    #[error("Handler not found: {0}")]
    HandlerNotFound(String),
    
    #[error("Middleware error: {0}")]
    MiddlewareError(String),
    
    #[error("Validation error: {0}")]
    ValidationError(String),
}

// Error handling in directive processing
fn process_directive(directive: &str) -> DirectiveResult<()> {
    let parsed = parse(directive)
        .map_err(|e| DirectiveError::SyntaxError(e.to_string()))?;
    
    let handler = find_handler(&parsed.handler)
        .ok_or_else(|| DirectiveError::HandlerNotFound(parsed.handler.clone()))?;
    
    handler.execute()
        .map_err(|e| DirectiveError::HandlerError(e.to_string()))
}
```

## Performance Optimization with Rust

```rust
use tusklang_rust::{DirectiveCache, DirectiveOptimizer};
use std::collections::HashMap;
use std::sync::Arc;
use tokio::sync::RwLock;

// Directive caching with Rust performance patterns
#[derive(Clone)]
struct DirectiveCache {
    cache: Arc<RwLock<HashMap<String, Directive>>>,
}

impl DirectiveCache {
    async fn get_or_parse(&self, key: &str, content: &str) -> DirectiveResult<Directive> {
        // Check cache first
        if let Some(cached) = self.cache.read().await.get(key) {
            return Ok(cached.clone());
        }
        
        // Parse and cache
        let directive = parse(content)?;
        self.cache.write().await.insert(key.to_string(), directive.clone());
        
        Ok(directive)
    }
}

// Zero-copy directive processing
fn process_directive_zero_copy<'a>(directive: &'a str) -> DirectiveResult<DirectiveContext<'a>> {
    // Process directive without copying data
    let context = DirectiveContext::from_str(directive)?;
    Ok(context)
}
```

## Integration with Rust Web Frameworks

```rust
use actix_web::{web, App, HttpServer};
use tusklang_rust::{DirectiveRouter, ActixIntegration};

// Actix-web integration
async fn create_app() -> App<()> {
    let directives = parse(r#"
#web /api/users {
    handler: "UserController::index"
    middleware: ["auth"]
}
"#)?;
    
    App::new()
        .configure(|cfg| {
            DirectiveRouter::configure(cfg, directives);
        })
}

// Axum integration
use axum::{Router, routing::get};
use tusklang_rust::AxumIntegration;

async fn create_axum_app() -> Router {
    let directives = parse(r#"
#web /api/users {
    handler: "UserController::index"
}
"#)?;
    
    DirectiveRouter::create_axum_router(directives)
}
```

## Testing Directives with Rust Testing

```rust
use tusklang_rust::{DirectiveTester, TestContext};
use tokio::test;

#[test]
async fn test_web_directive() {
    let directive = r#"
#web /test {
    handler: "TestController::index"
    response: "Hello, Test!"
}
"#;
    
    let tester = DirectiveTester::new();
    let result = tester.test_web_directive(directive, "/test").await?;
    
    assert_eq!(result.status_code, 200);
    assert_eq!(result.body, "Hello, Test!");
}

#[tokio::test]
async fn test_api_directive() {
    let directive = r#"
#api /api/test {
    handler: "TestController::json"
    return: {message: "Success"}
}
"#;
    
    let tester = DirectiveTester::new();
    let result = tester.test_api_directive(directive, "/api/test").await?;
    
    assert_eq!(result.status_code, 200);
    assert_eq!(result.content_type, "application/json");
}
```

## Security Considerations with Rust

```rust
use tusklang_rust::{DirectiveSecurity, SecurityValidator};
use std::collections::HashSet;

// Security validation for directives
struct SecurityValidator {
    allowed_handlers: HashSet<String>,
    allowed_middleware: HashSet<String>,
    max_rate_limit: u32,
}

impl SecurityValidator {
    fn validate_directive(&self, directive: &Directive) -> DirectiveResult<()> {
        // Validate handler
        if !self.allowed_handlers.contains(&directive.handler) {
            return Err(DirectiveError::SecurityError(
                format!("Handler not allowed: {}", directive.handler)
            ));
        }
        
        // Validate middleware
        for middleware in &directive.middleware {
            if !self.allowed_middleware.contains(middleware) {
                return Err(DirectiveError::SecurityError(
                    format!("Middleware not allowed: {}", middleware)
                ));
            }
        }
        
        // Validate rate limit
        if let Some(rate_limit) = &directive.rate_limit {
            let limit: u32 = rate_limit.parse()
                .map_err(|_| DirectiveError::ValidationError("Invalid rate limit".to_string()))?;
            
            if limit > self.max_rate_limit {
                return Err(DirectiveError::SecurityError(
                    format!("Rate limit too high: {}", limit)
                ));
            }
        }
        
        Ok(())
    }
}
```

## Best Practices for Rust Directives

```rust
// 1. Use strong typing for directive configurations
#[derive(Debug, Deserialize, Serialize)]
struct WebDirectiveConfig {
    route: String,
    method: HttpMethod,
    handler: String,
    middleware: Vec<String>,
    rate_limit: Option<RateLimit>,
    cache: Option<CacheConfig>,
}

// 2. Implement proper error handling
fn process_directive_safe(directive: &str) -> Result<Directive, Box<dyn std::error::Error>> {
    let parsed = parse(directive)?;
    
    // Validate directive
    let validator = SecurityValidator::new();
    validator.validate_directive(&parsed)?;
    
    Ok(parsed)
}

// 3. Use async/await for I/O operations
async fn execute_directive_async(directive: &Directive) -> DirectiveResult<()> {
    let handler = find_handler_async(&directive.handler).await?;
    handler.execute_async().await?;
    Ok(())
}

// 4. Implement proper logging
use tracing::{info, warn, error};

fn log_directive_execution(directive: &Directive, result: &DirectiveResult<()>) {
    match result {
        Ok(_) => info!("Directive executed successfully: {}", directive.route),
        Err(e) => error!("Directive execution failed: {} - {}", directive.route, e),
    }
}
```

## Next Steps

Now that you understand the basics of hash directives in Rust, explore the specific directive types:

- **[#api Directive](./077-hash-api-directive-rust.md)** - RESTful API endpoints with automatic JSON handling
- **[#web Directive](./078-hash-web-directive-rust.md)** - Web routes with full HTTP control
- **[#cli Directive](./079-hash-cli-directive-rust.md)** - Command-line interface integration
- **[#cron Directive](./080-hash-cron-directive-rust.md)** - Scheduled task execution
- **[#middleware Directive](./081-hash-middleware-directive-rust.md)** - Request processing pipeline
- **[#auth Directive](./082-hash-auth-directive-rust.md)** - Authentication and authorization
- **[#cache Directive](./083-hash-cache-directive-rust.md)** - Caching strategies
- **[#rate-limit Directive](./084-hash-rate-limit-directive-rust.md)** - Rate limiting and throttling
- **[#custom Directives](./085-hash-custom-directives-rust.md)** - Building your own directives

**Ready to revolutionize your Rust applications with TuskLang directives? Let's dive deeper into each directive type!** 