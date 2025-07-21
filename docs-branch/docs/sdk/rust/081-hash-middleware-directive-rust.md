# 🦀 #middleware - Request Processing Pipeline - Rust Edition

**"We don't bow to any king" - Rust Edition**

The `#middleware` directive in Rust creates powerful request processing pipelines with zero-copy execution, async support, and seamless integration with Rust's trait system for composable middleware chains.

## Basic Syntax

```rust
use tusklang_rust::{parse, MiddlewareDirective, MiddlewareChain};
use actix_web::{HttpRequest, HttpResponse, Error};

// Simple middleware with Rust handler
let middleware_config = r#"
#middleware auth {
    handler: "AuthMiddleware::authenticate"
    priority: 100
    async: true
}
"#;

// Middleware with configuration
let logging_middleware = r#"
#middleware logging {
    handler: "LoggingMiddleware::log_request"
    priority: 50
    config: {
        log_level: "info"
        include_headers: ["user-agent", "x-forwarded-for"]
    }
}
"#;

// Middleware with conditions
let conditional_middleware = r#"
#middleware rate_limit {
    handler: "RateLimitMiddleware::check"
    priority: 75
    condition: "@request.path.starts_with('/api/')"
    async: true
}
"#;
```

## Middleware Chain with Rust Traits

```rust
use tusklang_rust::{MiddlewareDirective, MiddlewareChain, MiddlewareTrait};
use std::pin::Pin;
use std::future::Future;

// Middleware trait with Rust async support
trait Middleware {
    async fn process(
        &self,
        req: &HttpRequest,
        next: Next<'_>,
    ) -> Result<HttpResponse, Error>;
    
    fn priority(&self) -> u32;
    fn name(&self) -> &str;
}

// Middleware chain configuration
let middleware_chain = r#"
#middleware_chain api_pipeline {
    middleware: ["cors", "auth", "logging", "rate_limit"]
    order: "priority"
    async: true
    
    error_handling: {
        on_failure: "continue"
        log_errors: true
        fallback_response: "error"
    }
}
"#;

// Conditional middleware chain
let conditional_chain = r#"
#middleware_chain secure_pipeline {
    middleware: ["ssl", "auth", "admin_check", "audit"]
    condition: "@request.path.starts_with('/admin/')"
    
    middleware: ["cors", "auth", "logging"]
    condition: "@request.path.starts_with('/api/')"
    
    middleware: ["logging"]
    condition: "default"
}
"#;
```

## Authentication Middleware with Rust

```rust
use tusklang_rust::{MiddlewareDirective, AuthMiddleware, SecurityContext};
use jsonwebtoken::{decode, encode, Header, Validation, DecodingKey};

// JWT authentication middleware
let jwt_auth = r#"
#middleware jwt_auth {
    handler: "JwtAuthMiddleware::authenticate"
    priority: 100
    async: true
    
    config: {
        secret: @env.JWT_SECRET
        algorithm: "HS256"
        header: "Authorization"
        prefix: "Bearer "
        
        claims: {
            user_id: "required"
            exp: "required"
            role: "optional"
        }
    }
    
    on_success: {
        set_user: true
        set_claims: true
        continue: true
    }
    
    on_failure: {
        status: 401
        message: "Invalid or missing token"
        redirect: "/login"
    }
}
"#;

// Session-based authentication
let session_auth = r#"
#middleware session_auth {
    handler: "SessionAuthMiddleware::authenticate"
    priority: 100
    async: true
    
    config: {
        session_key: "user_id"
        session_store: "redis"
        session_ttl: "24h"
        
        exclude_paths: ["/login", "/register", "/public"]
        redirect_url: "/login"
    }
    
    validation: {
        session_valid: "required"
        user_exists: "required"
        user_active: "required"
    }
}
"#;

// OAuth authentication middleware
let oauth_auth = r#"
#middleware oauth_auth {
    handler: "OAuthAuthMiddleware::authenticate"
    priority: 100
    async: true
    
    config: {
        providers: {
            google: {
                client_id: @env.GOOGLE_CLIENT_ID
                client_secret: @env.GOOGLE_CLIENT_SECRET
                scopes: ["email", "profile"]
            }
            
            github: {
                client_id: @env.GITHUB_CLIENT_ID
                client_secret: @env.GITHUB_CLIENT_SECRET
                scopes: ["user:email"]
            }
        }
        
        callback_url: "/auth/callback"
        success_redirect: "/dashboard"
        failure_redirect: "/login"
    }
}
"#;
```

## Logging and Monitoring Middleware

```rust
use tusklang_rust::{MiddlewareDirective, LoggingMiddleware, MetricsCollector};
use tracing::{info, warn, error, instrument};

// Request logging middleware
let request_logging = r#"
#middleware request_logging {
    handler: "RequestLoggingMiddleware::log"
    priority: 50
    async: true
    
    config: {
        log_level: "info"
        include_headers: ["user-agent", "x-forwarded-for", "x-real-ip"]
        exclude_paths: ["/health", "/metrics", "/favicon.ico"]
        
        format: {
            timestamp: true
            method: true
            path: true
            status: true
            duration: true
            ip: true
            user_agent: true
        }
    }
    
    metrics: {
        request_count: "counter"
        response_time: "histogram"
        status_codes: "counter"
        error_rate: "gauge"
    }
}
"#;

// Performance monitoring middleware
let performance_monitoring = r#"
#middleware performance_monitoring {
    handler: "PerformanceMonitoringMiddleware::monitor"
    priority: 25
    async: true
    
    config: {
        metrics: {
            response_time: "histogram"
            memory_usage: "gauge"
            cpu_usage: "gauge"
            database_queries: "counter"
            cache_hits: "counter"
            cache_misses: "counter"
        }
        
        alerts: {
            response_time: {
                threshold: "5s"
                action: "alert"
            }
            
            error_rate: {
                threshold: "5%"
                action: "alert"
            }
        }
    }
}
"#;
```

## Security Middleware with Rust

```rust
use tusklang_rust::{MiddlewareDirective, SecurityMiddleware, SecurityValidator};

// CORS middleware
let cors_middleware = r#"
#middleware cors {
    handler: "CorsMiddleware::handle"
    priority: 10
    async: true
    
    config: {
        allowed_origins: ["https://example.com", "https://api.example.com"]
        allowed_methods: ["GET", "POST", "PUT", "DELETE", "OPTIONS"]
        allowed_headers: ["Content-Type", "Authorization", "X-Requested-With"]
        allow_credentials: true
        max_age: "86400"
    }
    
    preflight: {
        enabled: true
        cache_duration: "24h"
    }
}
"#;

// CSRF protection middleware
let csrf_middleware = r#"
#middleware csrf {
    handler: "CsrfMiddleware::protect"
    priority: 75
    async: true
    
    config: {
        token_name: "_token"
        token_length: 32
        exclude_methods: ["GET", "HEAD", "OPTIONS"]
        exclude_paths: ["/api/webhook"]
        
        validation: {
            token_present: "required"
            token_valid: "required"
            token_not_expired: "required"
        }
    }
    
    on_failure: {
        status: 403
        message: "CSRF token validation failed"
        log: true
    }
}
"#;

// Rate limiting middleware
let rate_limit_middleware = r#"
#middleware rate_limit {
    handler: "RateLimitMiddleware::check"
    priority: 75
    async: true
    
    config: {
        strategy: "sliding_window"
        store: "redis"
        
        limits: {
            default: "100/hour"
            api: "1000/hour"
            auth: "10/minute"
            upload: "10/hour"
        }
        
        key_generator: "@request.ip"
        headers: {
            "X-RateLimit-Limit": "@limit"
            "X-RateLimit-Remaining": "@remaining"
            "X-RateLimit-Reset": "@reset"
        }
    }
    
    on_exceeded: {
        status: 429
        message: "Rate limit exceeded"
        retry_after: "@reset_time"
    }
}
"#;
```

## Data Transformation Middleware

```rust
use tusklang_rust::{MiddlewareDirective, TransformMiddleware, DataProcessor};

// Request transformation middleware
let request_transform = r#"
#middleware request_transform {
    handler: "RequestTransformMiddleware::transform"
    priority: 90
    async: true
    
    config: {
        transformations: {
            body: {
                json_to_struct: true
                validate_schema: true
                sanitize_input: true
            }
            
            headers: {
                normalize_case: true
                remove_invalid: true
                add_defaults: true
            }
            
            query: {
                parse_types: true
                validate_parameters: true
                set_defaults: true
            }
        }
    }
}
"#;

// Response transformation middleware
let response_transform = r#"
#middleware response_transform {
    handler: "ResponseTransformMiddleware::transform"
    priority: 110
    async: true
    
    config: {
        transformations: {
            body: {
                struct_to_json: true
                pretty_print: "@request.query.pretty"
                compress: "@request.headers.accept_encoding"
            }
            
            headers: {
                add_cors: true
                add_cache_headers: true
                add_security_headers: true
            }
            
            status: {
                normalize_errors: true
                add_error_details: "@debug_mode"
            }
        }
    }
}
"#;
```

## Error Handling Middleware

```rust
use tusklang_rust::{MiddlewareDirective, ErrorMiddleware, ErrorHandler};
use thiserror::Error;

#[derive(Error, Debug)]
pub enum MiddlewareError {
    #[error("Authentication failed: {0}")]
    AuthError(String),
    
    #[error("Validation failed: {0}")]
    ValidationError(String),
    
    #[error("Rate limit exceeded: {0}")]
    RateLimitError(String),
    
    #[error("Internal server error: {0}")]
    InternalError(String),
}

// Global error handling middleware
let error_handling = r#"
#middleware error_handler {
    handler: "ErrorHandlerMiddleware::handle"
    priority: 200
    async: true
    
    config: {
        error_types: {
            auth_error: {
                status: 401
                log: true
                notify: false
            }
            
            validation_error: {
                status: 422
                log: true
                notify: false
            }
            
            rate_limit_error: {
                status: 429
                log: true
                notify: false
            }
            
            internal_error: {
                status: 500
                log: true
                notify: true
            }
        }
        
        response_format: {
            error: true
            message: "@error.message"
            code: "@error.code"
            details: "@debug_mode ? @error.details : null"
        }
    }
}
"#;

// Circuit breaker middleware
let circuit_breaker = r#"
#middleware circuit_breaker {
    handler: "CircuitBreakerMiddleware::handle"
    priority: 80
    async: true
    
    config: {
        failure_threshold: 5
        recovery_timeout: "5m"
        half_open_max_calls: 3
        
        exclude_paths: ["/health", "/metrics"]
        
        on_open: {
            status: 503
            message: "Service temporarily unavailable"
            retry_after: "5m"
        }
    }
}
"#;
```

## Integration with Rust Web Frameworks

```rust
use actix_web::{web, App, HttpServer, middleware};
use tusklang_rust::{MiddlewareDirective, ActixIntegration};

// Actix-web integration
async fn create_actix_app() -> App<()> {
    let middleware_directives = parse(r#"
#middleware auth {
    handler: "AuthMiddleware::authenticate"
    priority: 100
    async: true
}

#middleware logging {
    handler: "LoggingMiddleware::log"
    priority: 50
    async: true
}
"#)?;
    
    App::new()
        .wrap(MiddlewareDirective::create_actix_middleware(middleware_directives))
}

// Axum integration
use axum::{Router, middleware as axum_middleware};
use tusklang_rust::AxumIntegration;

async fn create_axum_app() -> Router {
    let middleware_directives = parse(r#"
#middleware auth {
    handler: "AuthMiddleware::authenticate"
    priority: 100
    async: true
}
"#)?;
    
    Router::new()
        .layer(MiddlewareDirective::create_axum_layer(middleware_directives))
}

// Rocket integration
use rocket::{Rocket, Build};
use tusklang_rust::RocketIntegration;

fn create_rocket_app() -> Rocket<Build> {
    let middleware_directives = parse(r#"
#middleware auth {
    handler: "AuthMiddleware::authenticate"
    priority: 100
    async: true
}
"#)?;
    
    rocket::build()
        .attach(MiddlewareDirective::fairing(middleware_directives))
}
```

## Testing Middleware with Rust

```rust
use tusklang_rust::{MiddlewareDirectiveTester, TestRequest, TestResponse};
use tokio::test;

#[tokio::test]
async fn test_middleware_directive() {
    let middleware_directive = r#"
#middleware test_middleware {
    handler: "TestMiddleware::process"
    priority: 50
    async: true
    
    config: {
        add_header: "X-Test-Middleware"
        header_value: "processed"
    }
}
"#;
    
    let tester = MiddlewareDirectiveTester::new();
    let response = tester
        .test_middleware_directive(middleware_directive, "/test")
        .method("GET")
        .execute()
        .await?;
    
    assert_eq!(response.status_code, 200);
    assert_eq!(response.headers.get("X-Test-Middleware"), Some("processed"));
}

#[tokio::test]
async fn test_middleware_chain() {
    let middleware_chain = r#"
#middleware_chain test_chain {
    middleware: ["auth", "logging", "transform"]
    order: "priority"
    async: true
}
"#;
    
    let tester = MiddlewareDirectiveTester::new();
    let response = tester
        .test_middleware_chain(middleware_chain, "/test")
        .method("GET")
        .execute()
        .await?;
    
    assert_eq!(response.status_code, 200);
}
```

## Performance Optimization with Rust

```rust
use tusklang_rust::{MiddlewareDirective, PerformanceOptimizer};
use std::sync::Arc;
use tokio::sync::RwLock;

// Zero-copy middleware processing
fn process_middleware_zero_copy<'a>(directive: &'a str) -> MiddlewareDirectiveResult<MiddlewareContext<'a>> {
    let context = MiddlewareContext::from_str(directive)?;
    Ok(context)
}

// Async middleware processing with Rust futures
async fn process_middleware_async(directive: &MiddlewareDirective) -> MiddlewareDirectiveResult<()> {
    let handler = find_handler_async(&directive.handler).await?;
    handler.execute_async().await?;
    Ok(())
}

// Middleware caching
let cached_middleware = r#"
#middleware cached_middleware {
    handler: "CachedMiddleware::process"
    priority: 50
    async: true
    
    cache: {
        enabled: true
        ttl: "5m"
        key: "middleware:{@request.path}"
        strategy: "redis"
    }
}
"#;
```

## Security Best Practices with Rust

```rust
use tusklang_rust::{MiddlewareDirective, SecurityValidator};
use std::collections::HashSet;

// Security validation for middleware directives
struct MiddlewareSecurityValidator {
    allowed_handlers: HashSet<String>,
    allowed_priorities: HashSet<u32>,
    max_priority: u32,
    restricted_configs: HashSet<String>,
}

impl MiddlewareSecurityValidator {
    fn validate_middleware_directive(&self, directive: &MiddlewareDirective) -> MiddlewareDirectiveResult<()> {
        // Validate handler
        if !self.allowed_handlers.contains(&directive.handler) {
            return Err(MiddlewareError::SecurityError(
                format!("Handler not allowed: {}", directive.handler)
            ));
        }
        
        // Validate priority
        if directive.priority > self.max_priority {
            return Err(MiddlewareError::SecurityError(
                format!("Priority too high: {}", directive.priority)
            ));
        }
        
        // Validate configuration
        for config in &directive.config {
            if self.restricted_configs.contains(config) {
                return Err(MiddlewareError::SecurityError(
                    format!("Restricted config: {}", config)
                ));
            }
        }
        
        Ok(())
    }
}
```

## Best Practices for Rust Middleware Directives

```rust
// 1. Use strong typing for middleware configurations
#[derive(Debug, Deserialize, Serialize)]
struct MiddlewareDirectiveConfig {
    handler: String,
    priority: u32,
    async: bool,
    config: HashMap<String, serde_json::Value>,
    error_handling: Option<ErrorHandlingConfig>,
}

// 2. Implement proper error handling
fn process_middleware_directive_safe(directive: &str) -> Result<MiddlewareDirective, Box<dyn std::error::Error>> {
    let parsed = parse(directive)?;
    
    // Validate directive
    let validator = MiddlewareSecurityValidator::new();
    validator.validate_middleware_directive(&parsed)?;
    
    Ok(parsed)
}

// 3. Use async/await for I/O operations
async fn execute_middleware_directive_async(directive: &MiddlewareDirective) -> MiddlewareDirectiveResult<()> {
    let handler = find_handler_async(&directive.handler).await?;
    handler.execute_async().await?;
    Ok(())
}

// 4. Implement proper logging and monitoring
use tracing::{info, warn, error};

fn log_middleware_execution(directive: &MiddlewareDirective, result: &MiddlewareDirectiveResult<()>) {
    match result {
        Ok(_) => info!("Middleware directive executed successfully: {}", directive.handler),
        Err(e) => error!("Middleware directive execution failed: {} - {}", directive.handler, e),
    }
}
```

## Next Steps

Now that you understand the `#middleware` directive in Rust, explore other directive types:

- **[#auth Directive](./082-hash-auth-directive-rust.md)** - Authentication and authorization
- **[#cache Directive](./083-hash-cache-directive-rust.md)** - Caching strategies
- **[#rate-limit Directive](./084-hash-rate-limit-directive-rust.md)** - Rate limiting and throttling
- **[#custom Directives](./085-hash-custom-directives-rust.md)** - Building your own directives

**Ready to build powerful request processing pipelines with Rust and TuskLang? Let's continue with the next directive!** 