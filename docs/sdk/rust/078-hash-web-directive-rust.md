# 🦀 #web - Web Route Directive - Rust Edition

**"We don't bow to any king" - Rust Edition**

The `#web` directive in Rust creates web routes with full HTTP control, enabling you to build high-performance web applications with zero-copy routing and Rust's memory safety guarantees.

## Basic Syntax

```rust
use tusklang_rust::{parse, WebDirective, RouteHandler};
use actix_web::{web, HttpResponse, Responder};

// Simple web route with Rust handler
let web_config = r#"
#web /hello {
    handler: "HelloController::greet"
    method: "GET"
    response: "Hello, World!"
}
"#;

// Route with parameters
let user_route = r#"
#web /users/{id} {
    handler: "UserController::show"
    method: "GET"
    params: {
        id: "uuid"
    }
}
"#;

// Route with middleware
let protected_route = r#"
#web /admin {
    handler: "AdminController::dashboard"
    method: "GET"
    middleware: ["auth", "admin"]
    require_auth: true
}
"#;
```

## Route Parameters with Rust Types

```rust
use tusklang_rust::{WebDirective, RouteParams};
use uuid::Uuid;
use serde::{Deserialize, Serialize};

#[derive(Debug, Deserialize, Serialize)]
struct RouteParams {
    id: Uuid,
    slug: String,
    page: Option<u32>,
}

// Single parameter with type validation
let single_param = r#"
#web /users/{id} {
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
let multi_params = r#"
#web /posts/{year}/{month}/{day} {
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
let optional_params = r#"
#web /search/{query}/{page?} {
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
use tusklang_rust::{WebDirective, HttpMethod};
use actix_web::{web, HttpResponse, Responder};

#[derive(Debug, Deserialize, Serialize, Clone)]
enum HttpMethod {
    GET,
    POST,
    PUT,
    DELETE,
    PATCH,
    HEAD,
    OPTIONS,
}

// GET request (default)
let get_route = r#"
#web /users {
    method: "GET"
    handler: "UserController::index"
    response_type: "html"
    template: "users/index.html"
}
"#;

// POST request with form handling
let post_route = r#"
#web /users method: POST {
    handler: "UserController::create"
    content_type: "application/x-www-form-urlencoded"
    validation: {
        name: "required|string|max:255"
        email: "required|email|unique:users"
        password: "required|min:8"
    }
    redirect: "/users"
}
"#;

// Multiple methods with Rust pattern matching
let crud_route = r#"
#web /users/{id} method: [GET, PUT, DELETE] {
    handler: "UserController::handle"
    params: {
        id: "uuid"
    }
    switch: {
        GET: {
            response_type: "html"
            template: "users/show.html"
        }
        PUT: {
            content_type: "application/json"
            validation: {
                name: "string|max:255"
                email: "email|unique:users,id"
            }
        }
        DELETE: {
            redirect: "/users"
            flash_message: "User deleted successfully"
        }
    }
}
"#;
```

## Request Handling with Rust Types

```rust
use tusklang_rust::{WebDirective, Request, RequestData};
use serde::{Deserialize, Serialize};
use std::collections::HashMap;

#[derive(Debug, Deserialize)]
struct WebRequest {
    query: HashMap<String, String>,
    headers: HashMap<String, String>,
    body: Option<String>,
    files: HashMap<String, UploadedFile>,
    session: HashMap<String, String>,
    cookies: HashMap<String, String>,
}

#[derive(Debug, Deserialize)]
struct UploadedFile {
    name: String,
    content_type: String,
    size: usize,
    path: String,
}

// Access request data with Rust types
let request_handling = r#"
#web /process {
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
        user_agent: "string"
        accept: "string|in:text/html,application/json"
    }
    
    # Form data handling
    form_validation: {
        name: "required|string|max:255"
        email: "required|email"
        message: "string|max:1000"
    }
    
    # File upload handling
    file_validation: {
        avatar: "optional|file|max:5MB|mimes:jpg,jpeg,png"
        document: "optional|file|max:10MB|mimes:pdf,doc,docx"
    }
    
    # Session data
    session_required: ["user_id"]
    
    # Cookie handling
    cookies: {
        theme: "string|default:light"
        language: "string|default:en"
    }
}
"#;
```

## Response Control with Rust HTTP Types

```rust
use tusklang_rust::{WebDirective, Response, ResponseBuilder};
use actix_web::{HttpResponse, http::StatusCode};

// HTML responses with templates
let html_route = r#"
#web /dashboard {
    handler: "DashboardController::index"
    response_type: "html"
    template: "dashboard/index.html"
    layout: "layouts/app.html"
    
    template_data: {
        user: @user
        stats: @get_user_stats(@user.id)
        notifications: @get_notifications(@user.id)
    }
    
    response_headers: {
        "Cache-Control": "private, max-age=300"
        "X-Frame-Options": "DENY"
    }
}
"#;

// JSON responses with Rust serialization
let json_route = r#"
#web /api/data {
    handler: "DataController::fetch"
    response_type: "json"
    
    response_data: {
        status: "success"
        data: @fetch_data()
        timestamp: @now()
    }
    
    response_headers: {
        "Content-Type": "application/json"
        "Cache-Control": "public, max-age=60"
    }
}
"#;

// File downloads with Rust file handling
let download_route = r#"
#web /download/{file} {
    handler: "FileController::download"
    params: {
        file: "string"
    }
    
    response_type: "file"
    file_path: "@storage_path(@params.file)"
    
    response_headers: {
        "Content-Type": "application/octet-stream"
        "Content-Disposition": "attachment; filename=\"{file}\""
        "Cache-Control": "no-cache"
    }
    
    validation: {
        file: "required|string|exists:storage"
    }
}
"#;

// Redirects with flash messages
let redirect_route = r#"
#web /login method: POST {
    handler: "AuthController::login"
    
    validation: {
        email: "required|email"
        password: "required|string"
    }
    
    on_success: {
        redirect: "/dashboard"
        flash_message: "Welcome back!"
        flash_type: "success"
    }
    
    on_error: {
        redirect: "/login"
        flash_message: "Invalid credentials"
        flash_type: "error"
        preserve_input: true
    }
}
"#;
```

## Middleware Integration with Rust

```rust
use tusklang_rust::{WebDirective, Middleware, MiddlewareChain};
use actix_web::{web, HttpRequest, HttpResponse, Error};

// Middleware with Rust trait system
trait WebMiddleware {
    async fn process(&self, req: &HttpRequest) -> Result<HttpResponse, Error>;
    fn priority(&self) -> u32;
    fn name(&self) -> &str;
}

// Authentication middleware
let auth_middleware = r#"
#middleware auth {
    handler: "AuthMiddleware::authenticate"
    priority: 100
    async: true
    
    config: {
        session_key: "user_id"
        redirect_url: "/login"
        exclude_paths: ["/login", "/register", "/public"]
    }
}
"#;

// Logging middleware
let logging_middleware = r#"
#middleware logging {
    handler: "LoggingMiddleware::log_request"
    priority: 50
    async: true
    
    config: {
        log_level: "info"
        include_headers: ["user-agent", "x-forwarded-for"]
        exclude_paths: ["/health", "/metrics"]
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
    }
}
"#;

// Apply middleware to routes
let protected_routes = r#"
#web /admin {
    handler: "AdminController::dashboard"
    middleware: ["auth", "logging", "csrf"]
    require_auth: true
    roles: ["admin"]
}

#web /profile {
    handler: "UserController::profile"
    middleware: ["auth", "logging"]
    require_auth: true
}

#web /public {
    handler: "PublicController::index"
    middleware: ["logging"]
    public: true
}
"#;
```

## Template Integration with Rust

```rust
use tusklang_rust::{WebDirective, TemplateEngine, TemplateContext};
use tera::{Tera, Context};

// Template rendering with Rust template engines
let template_route = r#"
#web /users {
    handler: "UserController::index"
    response_type: "html"
    template: "users/index.html"
    layout: "layouts/app.html"
    
    template_data: {
        users: @User.all()
        pagination: @get_pagination(@request.query.page)
        search: @request.query.search
    }
    
    template_functions: {
        format_date: "@format_date"
        user_avatar: "@get_user_avatar"
        user_status: "@get_user_status"
    }
    
    cache: {
        enabled: true
        ttl: "5m"
        key: "users:list:{@request.query.page}:{@request.query.search}"
    }
}
"#;

// Dynamic template selection
let dynamic_template = r#"
#web /content/{slug} {
    handler: "ContentController::show"
    params: {
        slug: "string"
    }
    
    template: "@get_template_for_content(@params.slug)"
    layout: "@get_layout_for_user(@user)"
    
    template_data: {
        content: @get_content(@params.slug)
        related: @get_related_content(@params.slug)
        comments: @get_comments(@params.slug)
    }
    
    cache: {
        enabled: true
        ttl: "10m"
        key: "content:{@params.slug}"
    }
}
"#;
```

## Session Management with Rust

```rust
use tusklang_rust::{WebDirective, SessionManager, SessionData};
use std::collections::HashMap;

// Session handling with Rust types
let session_route = r#"
#web /login method: POST {
    handler: "AuthController::login"
    
    validation: {
        email: "required|email"
        password: "required|string"
    }
    
    session: {
        user_id: "@user.id"
        user_email: "@user.email"
        user_role: "@user.role"
        login_time: "@now()"
    }
    
    session_config: {
        secure: true
        http_only: true
        same_site: "strict"
        max_age: "24h"
    }
    
    on_success: {
        redirect: "/dashboard"
        flash_message: "Welcome back!"
    }
}
"#;

// Session validation
let session_validation = r#"
#web /dashboard {
    handler: "DashboardController::index"
    
    session_required: ["user_id"]
    session_validation: {
        user_id: "required|uuid"
        user_role: "required|string|in:user,admin,moderator"
    }
    
    on_session_invalid: {
        redirect: "/login"
        flash_message: "Please log in to continue"
    }
}
"#;
```

## Error Handling with Rust Result Types

```rust
use tusklang_rust::{WebDirective, WebError, ErrorHandler};
use thiserror::Error;

#[derive(Error, Debug)]
pub enum WebError {
    #[error("Route not found: {0}")]
    NotFoundError(String),
    
    #[error("Authentication required")]
    AuthRequiredError,
    
    #[error("Permission denied: {0}")]
    PermissionError(String),
    
    #[error("Validation failed: {0}")]
    ValidationError(String),
    
    #[error("Internal server error: {0}")]
    InternalError(String),
}

// Error handling with Rust Result types
let error_handling = r#"
#web /users/{id} {
    handler: "UserController::show"
    params: {
        id: "uuid"
    }
    
    error_handling: {
        not_found: {
            status: 404
            template: "errors/404.html"
            log: true
        }
        
        auth_required: {
            status: 401
            redirect: "/login"
            flash_message: "Please log in to view this page"
        }
        
        permission_denied: {
            status: 403
            template: "errors/403.html"
            log: true
        }
        
        validation_errors: {
            status: 422
            template: "errors/validation.html"
            preserve_input: true
        }
        
        server_errors: {
            status: 500
            template: "errors/500.html"
            log: true
            notify_admin: true
        }
    }
    
    return: @User.find_or_404(@params.id)
}
"#;
```

## Integration with Rust Web Frameworks

```rust
use actix_web::{web, App, HttpServer, HttpResponse};
use tusklang_rust::{WebDirective, ActixIntegration};

// Actix-web integration
async fn create_actix_app() -> App<()> {
    let web_directives = parse(r#"
#web /users {
    handler: "UserController::index"
    middleware: ["auth"]
    template: "users/index.html"
}

#web /users/{id} {
    handler: "UserController::show"
    params: {
        id: "uuid"
    }
    template: "users/show.html"
}
"#)?;
    
    App::new()
        .configure(|cfg| {
            WebDirective::configure_actix(cfg, web_directives);
        })
}

// Axum integration
use axum::{Router, routing::get, extract::Path};
use tusklang_rust::AxumIntegration;

async fn create_axum_app() -> Router {
    let web_directives = parse(r#"
#web /users {
    handler: "UserController::index"
    response_type: "html"
}
"#)?;
    
    WebDirective::create_axum_router(web_directives)
}

// Rocket integration
use rocket::{Rocket, Build, get, post};
use tusklang_rust::RocketIntegration;

fn create_rocket_app() -> Rocket<Build> {
    let web_directives = parse(r#"
#web /users {
    handler: "UserController::index"
}
"#)?;
    
    rocket::build()
        .attach(WebDirective::fairing(web_directives))
}
```

## Testing Web Directives with Rust

```rust
use tusklang_rust::{WebDirectiveTester, TestRequest, TestResponse};
use tokio::test;

#[tokio::test]
async fn test_web_directive() {
    let web_directive = r#"
#web /test {
    handler: "TestController::index"
    response_type: "html"
    template: "test/index.html"
    template_data: {
        message: "Hello, Test!"
    }
}
"#;
    
    let tester = WebDirectiveTester::new();
    let response = tester
        .test_web_directive(web_directive, "/test")
        .method("GET")
        .execute()
        .await?;
    
    assert_eq!(response.status_code, 200);
    assert_eq!(response.content_type, "text/html");
    assert!(response.body.contains("Hello, Test!"));
}

#[tokio::test]
async fn test_web_directive_with_params() {
    let web_directive = r#"
#web /users/{id} {
    handler: "UserController::show"
    params: {
        id: "uuid"
    }
    template: "users/show.html"
}
"#;
    
    let tester = WebDirectiveTester::new();
    let user_id = uuid::Uuid::new_v4();
    
    let response = tester
        .test_web_directive(web_directive, &format!("/users/{}", user_id))
        .method("GET")
        .execute()
        .await?;
    
    assert_eq!(response.status_code, 200);
}
```

## Performance Optimization with Rust

```rust
use tusklang_rust::{WebDirective, PerformanceOptimizer};
use std::sync::Arc;
use tokio::sync::RwLock;

// Zero-copy web processing
fn process_web_zero_copy<'a>(directive: &'a str) -> WebDirectiveResult<WebContext<'a>> {
    let context = WebContext::from_str(directive)?;
    Ok(context)
}

// Async web processing with Rust futures
async fn process_web_async(directive: &WebDirective) -> WebDirectiveResult<()> {
    let handler = find_handler_async(&directive.handler).await?;
    handler.execute_async().await?;
    Ok(())
}

// Template caching with Rust
let template_cache = r#"
#web /cached {
    handler: "CachedController::index"
    template: "cached/index.html"
    
    cache: {
        strategy: "template"
        ttl: "10m"
        key: "template:cached:index"
    }
    
    template_data: {
        data: @get_cached_data()
    }
}
"#;
```

## Security Best Practices with Rust

```rust
use tusklang_rust::{WebDirective, SecurityValidator};
use std::collections::HashSet;

// Security validation for web directives
struct WebSecurityValidator {
    allowed_handlers: HashSet<String>,
    allowed_methods: HashSet<String>,
    max_payload_size: usize,
    required_headers: HashSet<String>,
    secure_paths: HashSet<String>,
}

impl WebSecurityValidator {
    fn validate_web_directive(&self, directive: &WebDirective) -> WebDirectiveResult<()> {
        // Validate handler
        if !self.allowed_handlers.contains(&directive.handler) {
            return Err(WebError::SecurityError(
                format!("Handler not allowed: {}", directive.handler)
            ));
        }
        
        // Validate method
        if let Some(method) = &directive.method {
            if !self.allowed_methods.contains(method) {
                return Err(WebError::SecurityError(
                    format!("Method not allowed: {}", method)
                ));
            }
        }
        
        // Validate secure paths
        if directive.require_auth && !self.secure_paths.contains(&directive.route) {
            return Err(WebError::SecurityError(
                format!("Route not in secure paths: {}", directive.route)
            ));
        }
        
        Ok(())
    }
}
```

## Best Practices for Rust Web Directives

```rust
// 1. Use strong typing for web configurations
#[derive(Debug, Deserialize, Serialize)]
struct WebDirectiveConfig {
    route: String,
    method: HttpMethod,
    handler: String,
    template: Option<String>,
    middleware: Vec<String>,
    require_auth: bool,
    cache: Option<CacheConfig>,
}

// 2. Implement proper error handling
fn process_web_directive_safe(directive: &str) -> Result<WebDirective, Box<dyn std::error::Error>> {
    let parsed = parse(directive)?;
    
    // Validate directive
    let validator = WebSecurityValidator::new();
    validator.validate_web_directive(&parsed)?;
    
    Ok(parsed)
}

// 3. Use async/await for I/O operations
async fn execute_web_directive_async(directive: &WebDirective) -> WebDirectiveResult<()> {
    let handler = find_handler_async(&directive.handler).await?;
    handler.execute_async().await?;
    Ok(())
}

// 4. Implement proper logging and monitoring
use tracing::{info, warn, error};

fn log_web_execution(directive: &WebDirective, result: &WebDirectiveResult<()>) {
    match result {
        Ok(_) => info!("Web directive executed successfully: {}", directive.route),
        Err(e) => error!("Web directive execution failed: {} - {}", directive.route, e),
    }
}
```

## Next Steps

Now that you understand the `#web` directive in Rust, explore other directive types:

- **[#cli Directive](./079-hash-cli-directive-rust.md)** - Command-line interface integration
- **[#cron Directive](./080-hash-cron-directive-rust.md)** - Scheduled task execution
- **[#middleware Directive](./081-hash-middleware-directive-rust.md)** - Request processing pipeline
- **[#auth Directive](./082-hash-auth-directive-rust.md)** - Authentication and authorization

**Ready to build lightning-fast web applications with Rust and TuskLang? Let's continue with the next directive!** 