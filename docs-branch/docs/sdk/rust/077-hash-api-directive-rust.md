# 🦀 #api - API Endpoint Directive - Rust Edition

**"We don't bow to any king" - Rust Edition**

The `#api` directive in Rust creates RESTful API endpoints with automatic JSON serialization, deserialization, and HTTP response formatting. Built with zero-copy performance and Rust's type safety guarantees.

## Basic Syntax

```rust
use tusklang_rust::{parse, ApiDirective, JsonResponse};
use serde::{Deserialize, Serialize};

// Simple API endpoint with automatic JSON serialization
let api_config = r#"
#api /hello {
    handler: "HelloController::greet"
    response: {
        message: "Hello, World!"
        timestamp: @now()
    }
}
"#;

// With explicit return using Rust types
let users_api = r#"
#api /users {
    handler: "UserController::index"
    return: @User.all()
}
"#;

// Implicit JSON response with Rust structs
let status_api = r#"
#api /status {
    handler: "StatusController::health"
    return: {
        status: "online"
        timestamp: @now()
        version: "1.0.0"
        uptime: @uptime()
    }
}
"#;
```

## Route Parameters with Rust Types

```rust
use tusklang_rust::{ApiDirective, RouteParams};
use uuid::Uuid;

// Single parameter with type validation
let user_api = r#"
#api /users/{id} {
    handler: "UserController::show"
    params: {
        id: "uuid"
    }
    validation: {
        id: "required|uuid"
    }
}
"#;

// Multiple parameters with Rust path extraction
let posts_api = r#"
#api /posts/{year}/{month}/{day} {
    handler: "PostController::by_date"
    params: {
        year: "u32"
        month: "u8"
        day: "u8"
    }
    validation: {
        year: "required|integer|min:2020|max:2030"
        month: "required|integer|min:1|max:12"
        day: "required|integer|min:1|max:31"
    }
}
"#;

// Optional parameters with Rust Option types
let search_api = r#"
#api /search/{query}/{page?} {
    handler: "SearchController::query"
    params: {
        query: "string"
        page: "Option<u32>"
    }
    defaults: {
        page: 1
    }
}
"#;
```

## HTTP Methods with Rust Enums

```rust
use tusklang_rust::{ApiDirective, HttpMethod};
use actix_web::{web, HttpResponse, Responder};

#[derive(Debug, Deserialize, Serialize)]
enum ApiMethod {
    GET,
    POST,
    PUT,
    DELETE,
    PATCH,
}

// GET request (default)
let get_users = r#"
#api /users {
    method: "GET"
    handler: "UserController::index"
    response_type: "json"
    cache: "5m"
}
"#;

// POST request with validation
let create_user = r#"
#api /users method: POST {
    handler: "UserController::create"
    validation: {
        name: "required|string|max:255"
        email: "required|email|unique:users"
        password: "required|min:8|confirmed"
        age: "integer|min:18|max:120"
    }
    response_status: 201
}
"#;

// Multiple methods with Rust pattern matching
let user_crud = r#"
#api /users/{id} method: [GET, PUT, DELETE] {
    handler: "UserController::handle"
    params: {
        id: "uuid"
    }
    switch: {
        GET: {
            cache: "2m"
            response_type: "json"
        }
        PUT: {
            validation: {
                name: "string|max:255"
                email: "email|unique:users,id"
            }
        }
        DELETE: {
            response_status: 204
            require_auth: true
        }
    }
}
"#;
```

## Request Handling with Rust Types

```rust
use tusklang_rust::{ApiDirective, Request, RequestData};
use serde::{Deserialize, Serialize};
use std::collections::HashMap;

#[derive(Debug, Deserialize)]
struct ApiRequest {
    query: HashMap<String, String>,
    headers: HashMap<String, String>,
    body: Option<serde_json::Value>,
    files: HashMap<String, UploadedFile>,
}

#[derive(Debug, Deserialize)]
struct UploadedFile {
    name: String,
    content_type: String,
    size: usize,
    path: String,
}

// Access request data with Rust types
let process_api = r#"
#api /process {
    handler: "ProcessController::handle"
    
    # Query parameters with type conversion
    query_params: {
        sort: "string|default:created_at"
        order: "string|default:desc|in:asc,desc"
        limit: "u32|default:20|max:100"
        page: "u32|default:1"
    }
    
    # Headers with validation
    required_headers: {
        authorization: "string"
        content_type: "string|in:application/json,multipart/form-data"
    }
    
    # Body validation with Rust structs
    body_validation: {
        data: "required|json"
        metadata: "optional|json"
    }
    
    # File upload handling
    file_validation: {
        document: "optional|file|max:10MB|mimes:pdf,doc,docx"
        image: "optional|file|max:5MB|mimes:jpg,jpeg,png"
    }
}
"#;
```

## Response Control with Rust HTTP Types

```rust
use tusklang_rust::{ApiDirective, Response, ResponseBuilder};
use actix_web::{HttpResponse, http::StatusCode};

// Set status codes with Rust HTTP types
let resource_api = r#"
#api /resource/{id} {
    handler: "ResourceController::show"
    params: {
        id: "uuid"
    }
    
    responses: {
        200: {
            description: "Resource found"
            schema: "Resource"
        }
        404: {
            description: "Resource not found"
            schema: "ErrorResponse"
        }
        500: {
            description: "Internal server error"
            schema: "ErrorResponse"
        }
    }
}
"#;

// Set headers with Rust header types
let download_api = r#"
#api /download/{file} {
    handler: "FileController::download"
    params: {
        file: "string"
    }
    
    response_headers: {
        "Content-Type": "application/octet-stream"
        "Content-Disposition": "attachment; filename=\"{file}\""
        "Cache-Control": "no-cache"
        "X-File-Size": "@file_size(@params.file)"
    }
    
    response_type: "file"
    file_path: "@storage_path(@params.file)"
}
"#;

// Custom response format with Rust serialization
let custom_api = r#"
#api /custom {
    handler: "CustomController::format"
    
    response_format: "xml"
    response_headers: {
        "Content-Type": "application/xml"
    }
    
    transform: {
        data: "@to_xml(@response.data)"
        encoding: "UTF-8"
    }
}
"#;
```

## Authentication with Rust Security

```rust
use tusklang_rust::{ApiDirective, AuthMiddleware, SecurityContext};
use jsonwebtoken::{decode, encode, Header, Validation, DecodingKey, EncodingKey};

// API authentication middleware
let protected_api = r#"
#api /protected middleware: [authenticate] {
    handler: "ProtectedController::data"
    
    # @user is set by authenticate middleware
    auth_required: true
    roles: ["user", "admin"]
    
    return: {
        message: "Welcome, " + @user.name
        user: @user
        permissions: @user.permissions
    }
}
"#;

// Inline authentication with Rust JWT
let admin_api = r#"
#api /admin/users {
    handler: "AdminController::list_users"
    
    # Check authentication
    auth_check: {
        type: "jwt"
        secret: @env.JWT_SECRET
        required: true
    }
    
    # Check authorization
    authorization: {
        required: true
        permissions: ["manage-users"]
        roles: ["admin"]
    }
    
    return: @User.all()
}
"#;

// Token-based auth with Rust validation
let data_api = r#"
#api /data {
    handler: "DataController::sensitive"
    
    token_auth: {
        header: "Authorization"
        prefix: "Bearer "
        validation: {
            algorithm: "HS256"
            secret: @env.API_SECRET
            required_claims: ["user_id", "exp"]
        }
    }
    
    return: @get_sensitive_data(@user.id)
}
"#;
```

## Validation with Rust Validators

```rust
use tusklang_rust::{ApiDirective, Validator, ValidationRule};
use validator::{Validate, ValidationError};
use serde::{Deserialize, Serialize};

#[derive(Debug, Deserialize, Validate)]
struct UserCreateRequest {
    #[validate(length(min = 3, max = 50))]
    username: String,
    
    #[validate(email)]
    email: String,
    
    #[validate(length(min = 8))]
    password: String,
    
    #[validate(range(min = 18, max = 120))]
    age: Option<u8>,
}

// Input validation with Rust structs
let register_api = r#"
#api /register method: POST {
    handler: "AuthController::register"
    
    validation: {
        username: "required|string|min:3|max:50|unique:users"
        email: "required|email|unique:users"
        password: "required|min:8|confirmed"
        password_confirmation: "required|same:password"
        age: "integer|min:18|max:120"
        terms_accepted: "required|boolean|accepted"
    }
    
    custom_validation: {
        username: "@validate_username_availability(@request.username)"
        email: "@validate_email_domain(@request.email)"
        password: "@validate_password_strength(@request.password)"
    }
    
    response_status: 201
    return: {
        message: "User registered successfully"
        user: @created_user
        token: @generate_jwt(@created_user)
    }
}
"#;

// Custom validation rules with Rust functions
let custom_validation = r#"
#api /custom-validation method: POST {
    handler: "CustomController::validate"
    
    validation_rules: {
        custom_field: {
            required: true
            custom: "@validate_custom_field"
            message: "Custom field validation failed"
        }
        
        conditional_field: {
            required_if: "other_field == 'value'"
            custom: "@validate_conditional"
        }
    }
}
"#;
```

## Error Handling with Rust Result Types

```rust
use tusklang_rust::{ApiDirective, ApiError, ErrorHandler};
use thiserror::Error;

#[derive(Error, Debug)]
pub enum ApiError {
    #[error("Validation failed: {0}")]
    ValidationError(String),
    
    #[error("Authentication failed: {0}")]
    AuthError(String),
    
    #[error("Resource not found: {0}")]
    NotFoundError(String),
    
    #[error("Internal server error: {0}")]
    InternalError(String),
}

// Error handling with Rust Result types
let error_handling_api = r#"
#api /users/{id} {
    handler: "UserController::show"
    params: {
        id: "uuid"
    }
    
    error_handling: {
        validation_errors: {
            status: 422
            format: "json"
            schema: "ValidationErrorResponse"
        }
        
        not_found: {
            status: 404
            format: "json"
            schema: "ErrorResponse"
        }
        
        auth_errors: {
            status: 401
            format: "json"
            schema: "AuthErrorResponse"
        }
        
        server_errors: {
            status: 500
            format: "json"
            schema: "ErrorResponse"
            log: true
        }
    }
    
    return: @User.find_or_404(@params.id)
}
"#;
```

## Caching with Rust Cache Implementations

```rust
use tusklang_rust::{ApiDirective, CacheManager, CacheStrategy};
use redis::AsyncCommands;
use std::time::Duration;

// Caching strategies with Rust cache implementations
let cached_api = r#"
#api /users {
    handler: "UserController::index"
    
    cache: {
        strategy: "redis"
        ttl: "5m"
        key: "users:list:{@request.query.sort}:{@request.query.page}"
        tags: ["users", "list"]
    }
    
    cache_headers: {
        "Cache-Control": "public, max-age=300"
        "ETag": "@generate_etag(@response.data)"
    }
    
    return: @User.all()
}
"#;

// Conditional caching with Rust logic
let conditional_cache = r#"
#api /posts/{id} {
    handler: "PostController::show"
    params: {
        id: "uuid"
    }
    
    cache: {
        strategy: "conditional"
        ttl: "10m"
        key: "post:{@params.id}"
        condition: "@user.is_authenticated == false"
        tags: ["posts", "public"]
    }
    
    return: @Post.find_or_404(@params.id)
}
"#;
```

## Rate Limiting with Rust Rate Limiters

```rust
use tusklang_rust::{ApiDirective, RateLimiter, RateLimitConfig};
use std::collections::HashMap;
use tokio::time::{Duration, Instant};

// Rate limiting with Rust implementations
let rate_limited_api = r#"
#api /api/data {
    handler: "DataController::fetch"
    
    rate_limit: {
        strategy: "sliding_window"
        limit: 100
        window: "1h"
        key: "@request.ip"
        headers: {
            "X-RateLimit-Limit": "100"
            "X-RateLimit-Remaining": "@rate_limit_remaining"
            "X-RateLimit-Reset": "@rate_limit_reset"
        }
    }
    
    return: @fetch_data()
}
"#;

// User-based rate limiting
let user_rate_limit = r#"
#api /api/user-data {
    handler: "UserController::data"
    
    rate_limit: {
        strategy: "token_bucket"
        limit: 1000
        window: "1d"
        key: "@user.id"
        burst: 50
    }
    
    return: @get_user_data(@user.id)
}
"#;
```

## Integration with Rust Web Frameworks

```rust
use actix_web::{web, App, HttpServer, HttpResponse};
use tusklang_rust::{ApiDirective, ActixIntegration};

// Actix-web integration
async fn create_actix_app() -> App<()> {
    let api_directives = parse(r#"
#api /api/users {
    handler: "UserController::index"
    middleware: ["auth"]
    cache: "5m"
}

#api /api/users/{id} {
    handler: "UserController::show"
    params: {
        id: "uuid"
    }
}
"#)?;
    
    App::new()
        .configure(|cfg| {
            ApiDirective::configure_actix(cfg, api_directives);
        })
}

// Axum integration
use axum::{Router, routing::get, Json};
use tusklang_rust::AxumIntegration;

async fn create_axum_app() -> Router {
    let api_directives = parse(r#"
#api /api/users {
    handler: "UserController::index"
    response_type: "json"
}
"#)?;
    
    ApiDirective::create_axum_router(api_directives)
}

// Rocket integration
use rocket::{Rocket, Build};
use tusklang_rust::RocketIntegration;

fn create_rocket_app() -> Rocket<Build> {
    let api_directives = parse(r#"
#api /api/users {
    handler: "UserController::index"
}
"#)?;
    
    rocket::build()
        .attach(ApiDirective::fairing(api_directives))
}
```

## Testing API Directives with Rust

```rust
use tusklang_rust::{ApiDirectiveTester, TestRequest, TestResponse};
use tokio::test;
use serde_json::json;

#[tokio::test]
async fn test_api_directive() {
    let api_directive = r#"
#api /api/test {
    handler: "TestController::json"
    return: {
        message: "Success"
        data: [1, 2, 3]
    }
}
"#;
    
    let tester = ApiDirectiveTester::new();
    let response = tester
        .test_api_directive(api_directive, "/api/test")
        .method("GET")
        .execute()
        .await?;
    
    assert_eq!(response.status_code, 200);
    assert_eq!(response.content_type, "application/json");
    
    let body: serde_json::Value = response.json().await?;
    assert_eq!(body["message"], "Success");
    assert_eq!(body["data"], json!([1, 2, 3]));
}

#[tokio::test]
async fn test_api_with_validation() {
    let api_directive = r#"
#api /api/register method: POST {
    handler: "AuthController::register"
    validation: {
        email: "required|email"
        password: "required|min:8"
    }
}
"#;
    
    let tester = ApiDirectiveTester::new();
    
    // Test valid request
    let valid_response = tester
        .test_api_directive(api_directive, "/api/register")
        .method("POST")
        .json(json!({
            "email": "test@example.com",
            "password": "password123"
        }))
        .execute()
        .await?;
    
    assert_eq!(valid_response.status_code, 200);
    
    // Test invalid request
    let invalid_response = tester
        .test_api_directive(api_directive, "/api/register")
        .method("POST")
        .json(json!({
            "email": "invalid-email",
            "password": "123"
        }))
        .execute()
        .await?;
    
    assert_eq!(invalid_response.status_code, 422);
}
```

## Performance Optimization with Rust

```rust
use tusklang_rust::{ApiDirective, PerformanceOptimizer};
use std::sync::Arc;
use tokio::sync::RwLock;

// Zero-copy API processing
fn process_api_zero_copy<'a>(directive: &'a str) -> ApiDirectiveResult<ApiContext<'a>> {
    let context = ApiContext::from_str(directive)?;
    Ok(context)
}

// Async API processing with Rust futures
async fn process_api_async(directive: &ApiDirective) -> ApiDirectiveResult<()> {
    let handler = find_handler_async(&directive.handler).await?;
    handler.execute_async().await?;
    Ok(())
}

// Connection pooling for database operations
let db_api = r#"
#api /api/users {
    handler: "UserController::index"
    
    database: {
        pool: "default"
        timeout: "5s"
        max_connections: 10
    }
    
    return: @User.all()
}
"#;
```

## Security Best Practices with Rust

```rust
use tusklang_rust::{ApiDirective, SecurityValidator};
use std::collections::HashSet;

// Security validation for API directives
struct ApiSecurityValidator {
    allowed_handlers: HashSet<String>,
    allowed_methods: HashSet<String>,
    max_payload_size: usize,
    required_headers: HashSet<String>,
}

impl ApiSecurityValidator {
    fn validate_api_directive(&self, directive: &ApiDirective) -> ApiDirectiveResult<()> {
        // Validate handler
        if !self.allowed_handlers.contains(&directive.handler) {
            return Err(ApiError::SecurityError(
                format!("Handler not allowed: {}", directive.handler)
            ));
        }
        
        // Validate method
        if let Some(method) = &directive.method {
            if !self.allowed_methods.contains(method) {
                return Err(ApiError::SecurityError(
                    format!("Method not allowed: {}", method)
                ));
            }
        }
        
        // Validate payload size
        if let Some(max_size) = directive.max_payload_size {
            if max_size > self.max_payload_size {
                return Err(ApiError::SecurityError(
                    format!("Payload size too large: {}", max_size)
                ));
            }
        }
        
        Ok(())
    }
}
```

## Best Practices for Rust API Directives

```rust
// 1. Use strong typing for API configurations
#[derive(Debug, Deserialize, Serialize)]
struct ApiDirectiveConfig {
    route: String,
    method: HttpMethod,
    handler: String,
    validation: Option<ValidationRules>,
    cache: Option<CacheConfig>,
    rate_limit: Option<RateLimitConfig>,
}

// 2. Implement proper error handling
fn process_api_directive_safe(directive: &str) -> Result<ApiDirective, Box<dyn std::error::Error>> {
    let parsed = parse(directive)?;
    
    // Validate directive
    let validator = ApiSecurityValidator::new();
    validator.validate_api_directive(&parsed)?;
    
    Ok(parsed)
}

// 3. Use async/await for I/O operations
async fn execute_api_directive_async(directive: &ApiDirective) -> ApiDirectiveResult<()> {
    let handler = find_handler_async(&directive.handler).await?;
    handler.execute_async().await?;
    Ok(())
}

// 4. Implement proper logging and monitoring
use tracing::{info, warn, error};

fn log_api_execution(directive: &ApiDirective, result: &ApiDirectiveResult<()>) {
    match result {
        Ok(_) => info!("API directive executed successfully: {}", directive.route),
        Err(e) => error!("API directive execution failed: {} - {}", directive.route, e),
    }
}
```

## Next Steps

Now that you understand the `#api` directive in Rust, explore other directive types:

- **[#web Directive](./078-hash-web-directive-rust.md)** - Web routes with full HTTP control
- **[#cli Directive](./079-hash-cli-directive-rust.md)** - Command-line interface integration
- **[#cron Directive](./080-hash-cron-directive-rust.md)** - Scheduled task execution
- **[#middleware Directive](./081-hash-middleware-directive-rust.md)** - Request processing pipeline

**Ready to build blazing-fast APIs with Rust and TuskLang? Let's continue with the next directive!** 