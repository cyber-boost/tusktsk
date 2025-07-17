# Web Directives - Go

## 🎯 What Are Web Directives?

Web directives (`#web`) in TuskLang allow you to define web routes, middleware, and server configuration directly in your configuration files. They transform static config into executable web application logic.

```go
// Web directives define your entire web application structure
type WebConfig struct {
    Routes     map[string]string `tsk:"#web_routes"`
    Middleware []string          `tsk:"#web_middleware"`
    Static     string            `tsk:"#web_static"`
    Port       int               `tsk:"#web_port"`
}
```

## 🚀 Why Web Directives Matter

### Traditional Web Development
```go
// Old way - scattered across multiple files
func main() {
    router := mux.NewRouter()
    
    // Routes defined in code
    router.HandleFunc("/api/users", handleUsers).Methods("GET")
    router.HandleFunc("/api/posts", handlePosts).Methods("POST")
    
    // Middleware scattered
    router.Use(loggingMiddleware)
    router.Use(authMiddleware)
    
    // Static files hardcoded
    router.PathPrefix("/static/").Handler(http.StripPrefix("/static/", 
        http.FileServer(http.Dir("public"))))
    
    http.ListenAndServe(":8080", router)
}
```

### TuskLang Web Directives - Declarative & Dynamic
```tsk
# app.tsk - Complete web application definition
web_routes: #web("""
    GET /api/users -> handlers.GetUsers
    POST /api/users -> handlers.CreateUser
    GET /api/posts -> handlers.GetPosts
    POST /api/posts -> handlers.CreatePost
    GET /static/* -> static.Serve
""")

web_middleware: #web("""
    logging -> middleware.Logging
    auth -> middleware.Auth
    cors -> middleware.CORS
""")

web_static: #web("public/")
web_port: #env("PORT", "8080")
```

## 📋 Web Directive Types

### 1. **Route Directives** (`#web_routes`)
- HTTP method and path definitions
- Handler function mappings
- Parameter extraction
- Response formatting

### 2. **Middleware Directives** (`#web_middleware`)
- Request processing pipeline
- Authentication and authorization
- Logging and monitoring
- CORS and security headers

### 3. **Static File Directives** (`#web_static`)
- Static file serving
- Asset optimization
- Cache headers
- Directory structure

### 4. **Server Directives** (`#web_server`)
- Port configuration
- SSL/TLS settings
- Timeout configuration
- Connection limits

## 🔧 Basic Web Directive Syntax

### Simple Route Definition
```tsk
# Basic route with handler
api_users: #web("GET /api/users -> handlers.GetUsers")
```

### Route with Parameters
```tsk
# Route with URL parameters
user_detail: #web("GET /api/users/{id} -> handlers.GetUser")
```

### Route with Query Parameters
```tsk
# Route with query string handling
user_search: #web("GET /api/users?search={query}&limit={limit} -> handlers.SearchUsers")
```

### Multiple Routes
```tsk
# Multiple routes in one directive
api_routes: #web("""
    GET /api/users -> handlers.GetUsers
    POST /api/users -> handlers.CreateUser
    PUT /api/users/{id} -> handlers.UpdateUser
    DELETE /api/users/{id} -> handlers.DeleteUser
""")
```

## 🎯 Go Integration Patterns

### Struct Tags for Web Directives
```go
type WebAppConfig struct {
    // Route definitions
    APIRoutes string `tsk:"#web_routes"`
    
    // Middleware stack
    Middleware string `tsk:"#web_middleware"`
    
    // Static file serving
    StaticDir string `tsk:"#web_static"`
    
    // Server configuration
    Port int `tsk:"#web_port"`
}
```

### Web Application Setup
```go
package main

import (
    "github.com/tusklang/go-sdk"
    "github.com/gorilla/mux"
    "net/http"
)

func main() {
    // Load web configuration
    config := tusk.LoadConfig("app.tsk")
    
    var webConfig WebAppConfig
    config.Unmarshal(&webConfig)
    
    // Create router from directives
    router := tusk.NewWebRouter(webConfig.APIRoutes)
    
    // Apply middleware from directives
    tusk.ApplyMiddleware(router, webConfig.Middleware)
    
    // Setup static file serving
    tusk.SetupStaticFiles(router, webConfig.StaticDir)
    
    // Start server
    http.ListenAndServe(fmt.Sprintf(":%d", webConfig.Port), router)
}
```

### Handler Function Registration
```go
package handlers

import (
    "encoding/json"
    "net/http"
    "github.com/gorilla/mux"
)

// Handler functions referenced in web directives
func GetUsers(w http.ResponseWriter, r *http.Request) {
    users := []User{
        {ID: 1, Name: "Alice"},
        {ID: 2, Name: "Bob"},
    }
    
    json.NewEncoder(w).Encode(users)
}

func CreateUser(w http.ResponseWriter, r *http.Request) {
    var user User
    json.NewDecoder(r.Body).Decode(&user)
    
    // Create user logic
    user.ID = generateID()
    
    w.WriteHeader(http.StatusCreated)
    json.NewEncoder(w).Encode(user)
}

func GetUser(w http.ResponseWriter, r *http.Request) {
    vars := mux.Vars(r)
    userID := vars["id"]
    
    // Get user by ID logic
    user := findUserByID(userID)
    
    if user == nil {
        http.Error(w, "User not found", http.StatusNotFound)
        return
    }
    
    json.NewEncoder(w).Encode(user)
}
```

## 🔄 Middleware Integration

### Middleware Definition
```tsk
# Middleware stack definition
web_middleware: #web("""
    logging -> middleware.Logging
    auth -> middleware.Auth
    cors -> middleware.CORS
    rate_limit -> middleware.RateLimit
""")
```

### Go Middleware Implementation
```go
package middleware

import (
    "log"
    "net/http"
    "time"
)

// Logging middleware
func Logging(next http.Handler) http.Handler {
    return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
        start := time.Now()
        
        next.ServeHTTP(w, r)
        
        log.Printf("%s %s %v", r.Method, r.URL.Path, time.Since(start))
    })
}

// Authentication middleware
func Auth(next http.Handler) http.Handler {
    return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
        token := r.Header.Get("Authorization")
        
        if !isValidToken(token) {
            http.Error(w, "Unauthorized", http.StatusUnauthorized)
            return
        }
        
        next.ServeHTTP(w, r)
    })
}

// CORS middleware
func CORS(next http.Handler) http.Handler {
    return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
        w.Header().Set("Access-Control-Allow-Origin", "*")
        w.Header().Set("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS")
        w.Header().Set("Access-Control-Allow-Headers", "Content-Type, Authorization")
        
        if r.Method == "OPTIONS" {
            w.WriteHeader(http.StatusOK)
            return
        }
        
        next.ServeHTTP(w, r)
    })
}
```

## 🎯 Advanced Route Patterns

### RESTful API Definition
```tsk
# Complete REST API definition
rest_api: #web("""
    # Users
    GET /api/users -> handlers.GetUsers
    POST /api/users -> handlers.CreateUser
    GET /api/users/{id} -> handlers.GetUser
    PUT /api/users/{id} -> handlers.UpdateUser
    DELETE /api/users/{id} -> handlers.DeleteUser
    
    # Posts
    GET /api/posts -> handlers.GetPosts
    POST /api/posts -> handlers.CreatePost
    GET /api/posts/{id} -> handlers.GetPost
    PUT /api/posts/{id} -> handlers.UpdatePost
    DELETE /api/posts/{id} -> handlers.DeletePost
    
    # Comments
    GET /api/posts/{post_id}/comments -> handlers.GetComments
    POST /api/posts/{post_id}/comments -> handlers.CreateComment
""")
```

### Nested Route Groups
```tsk
# Nested route groups with middleware
api_v1: #web("""
    # Group: /api/v1
    middleware: auth, logging
    
    # Users group
    GET /users -> handlers.GetUsers
    POST /users -> handlers.CreateUser
    GET /users/{id} -> handlers.GetUser
    
    # Posts group
    GET /posts -> handlers.GetPosts
    POST /posts -> handlers.CreatePost
    GET /posts/{id} -> handlers.GetPost
""")
```

### Dynamic Route Generation
```tsk
# Dynamic routes based on environment
api_routes: #if(
    #env("ENVIRONMENT") == "production",
    #web("""
        GET /api/users -> handlers.GetUsers
        POST /api/users -> handlers.CreateUser
    """),
    #web("""
        GET /api/users -> handlers.GetUsers
        POST /api/users -> handlers.CreateUser
        GET /api/debug -> handlers.Debug
    """)
)
```

## 🛡️ Security Features

### Authentication Routes
```tsk
# Authentication routes with security
auth_routes: #web("""
    POST /auth/login -> handlers.Login
    POST /auth/logout -> handlers.Logout
    POST /auth/refresh -> handlers.RefreshToken
    GET /auth/profile -> handlers.GetProfile
""")

auth_middleware: #web("""
    jwt -> middleware.JWT
    csrf -> middleware.CSRF
""")
```

### Rate Limiting Configuration
```tsk
# Rate limiting per route
rate_limits: #web("""
    /api/users -> 100/hour
    /api/posts -> 50/hour
    /auth/login -> 5/minute
    /api/admin/* -> 1000/hour
""")
```

## ⚡ Performance Optimization

### Caching Headers
```tsk
# Cache configuration for static assets
static_cache: #web("""
    /static/css/* -> 1h
    /static/js/* -> 1h
    /static/images/* -> 1d
    /api/users -> 5m
""")
```

### Compression Configuration
```tsk
# Compression settings
compression: #web("""
    gzip -> true
    brotli -> true
    min_size -> 1024
""")
```

## 🔧 Error Handling

### Custom Error Handlers
```tsk
# Custom error handling routes
error_handlers: #web("""
    404 -> handlers.NotFound
    500 -> handlers.InternalError
    401 -> handlers.Unauthorized
    403 -> handlers.Forbidden
""")
```

### Go Error Handler Implementation
```go
package handlers

import (
    "net/http"
    "encoding/json"
)

func NotFound(w http.ResponseWriter, r *http.Request) {
    w.WriteHeader(http.StatusNotFound)
    json.NewEncoder(w).Encode(map[string]string{
        "error": "Resource not found",
        "path":  r.URL.Path,
    })
}

func InternalError(w http.ResponseWriter, r *http.Request) {
    w.WriteHeader(http.StatusInternalServerError)
    json.NewEncoder(w).Encode(map[string]string{
        "error": "Internal server error",
    })
}

func Unauthorized(w http.ResponseWriter, r *http.Request) {
    w.WriteHeader(http.StatusUnauthorized)
    json.NewEncoder(w).Encode(map[string]string{
        "error": "Authentication required",
    })
}
```

## 🎯 Real-World Example

### Complete Web Application Configuration
```tsk
# web-app.tsk - Complete web application

# Environment configuration
environment: #env("ENVIRONMENT", "development")
debug_mode: #env("DEBUG", "false")

# Server configuration
port: #env("PORT", "8080")
host: #env("HOST", "localhost")

# Database configuration
database_url: #env("DATABASE_URL", "sqlite://app.db")

# API routes
api_routes: #web("""
    # User management
    GET /api/users -> handlers.GetUsers
    POST /api/users -> handlers.CreateUser
    GET /api/users/{id} -> handlers.GetUser
    PUT /api/users/{id} -> handlers.UpdateUser
    DELETE /api/users/{id} -> handlers.DeleteUser
    
    # Authentication
    POST /auth/login -> handlers.Login
    POST /auth/logout -> handlers.Logout
    POST /auth/refresh -> handlers.RefreshToken
    GET /auth/profile -> handlers.GetProfile
    
    # Content management
    GET /api/posts -> handlers.GetPosts
    POST /api/posts -> handlers.CreatePost
    GET /api/posts/{id} -> handlers.GetPost
    PUT /api/posts/{id} -> handlers.UpdatePost
    DELETE /api/posts/{id} -> handlers.DeletePost
""")

# Middleware stack
middleware: #web("""
    logging -> middleware.Logging
    cors -> middleware.CORS
    auth -> middleware.Auth
    rate_limit -> middleware.RateLimit
""")

# Static file serving
static_dir: #web("public/")
static_cache: #web("""
    /static/css/* -> 1h
    /static/js/* -> 1h
    /static/images/* -> 1d
""")

# Error handlers
error_handlers: #web("""
    404 -> handlers.NotFound
    500 -> handlers.InternalError
    401 -> handlers.Unauthorized
    403 -> handlers.Forbidden
""")

# Rate limiting
rate_limits: #web("""
    /api/users -> 100/hour
    /api/posts -> 50/hour
    /auth/login -> 5/minute
    /api/admin/* -> 1000/hour
""")
```

### Go Web Application Implementation
```go
package main

import (
    "fmt"
    "log"
    "net/http"
    "github.com/tusklang/go-sdk"
)

type WebAppConfig struct {
    Environment   string `tsk:"environment"`
    DebugMode     bool   `tsk:"debug_mode"`
    Port          int    `tsk:"port"`
    Host          string `tsk:"host"`
    DatabaseURL   string `tsk:"database_url"`
    APIRoutes     string `tsk:"api_routes"`
    Middleware    string `tsk:"middleware"`
    StaticDir     string `tsk:"static_dir"`
    StaticCache   string `tsk:"static_cache"`
    ErrorHandlers string `tsk:"error_handlers"`
    RateLimits    string `tsk:"rate_limits"`
}

func main() {
    // Load web application configuration
    config := tusk.LoadConfig("web-app.tsk")
    
    var webConfig WebAppConfig
    if err := config.Unmarshal(&webConfig); err != nil {
        log.Fatal("Failed to load web config:", err)
    }
    
    // Create web application from directives
    app := tusk.NewWebApp(webConfig)
    
    // Setup database connection
    db := tusk.ConnectDatabase(webConfig.DatabaseURL)
    
    // Register handlers with database context
    handlers.SetDatabase(db)
    
    // Start the web server
    addr := fmt.Sprintf("%s:%d", webConfig.Host, webConfig.Port)
    log.Printf("Starting web server on %s", addr)
    
    if err := app.ListenAndServe(addr); err != nil {
        log.Fatal("Server failed:", err)
    }
}
```

## 🎯 Best Practices

### 1. **Organize Routes by Feature**
```tsk
# Group related routes together
user_routes: #web("""
    GET /api/users -> handlers.GetUsers
    POST /api/users -> handlers.CreateUser
    GET /api/users/{id} -> handlers.GetUser
    PUT /api/users/{id} -> handlers.UpdateUser
    DELETE /api/users/{id} -> handlers.DeleteUser
""")

post_routes: #web("""
    GET /api/posts -> handlers.GetPosts
    POST /api/posts -> handlers.CreatePost
    GET /api/posts/{id} -> handlers.GetPost
    PUT /api/posts/{id} -> handlers.UpdatePost
    DELETE /api/posts/{id} -> handlers.DeletePost
""")
```

### 2. **Use Environment-Specific Configuration**
```tsk
# Different middleware for different environments
middleware: #if(
    #env("ENVIRONMENT") == "production",
    #web("logging, auth, cors, rate_limit"),
    #web("logging, cors")
)
```

### 3. **Implement Proper Error Handling**
```tsk
# Comprehensive error handling
error_handlers: #web("""
    400 -> handlers.BadRequest
    401 -> handlers.Unauthorized
    403 -> handlers.Forbidden
    404 -> handlers.NotFound
    422 -> handlers.ValidationError
    500 -> handlers.InternalError
""")
```

### 4. **Configure Security Headers**
```tsk
# Security middleware configuration
security: #web("""
    hsts -> true
    csp -> "default-src 'self'"
    xss_protection -> true
    content_type_nosniff -> true
""")
```

## 🎯 Summary

Web directives in TuskLang provide a powerful, declarative way to define web applications. They enable:

- **Declarative route definitions** that are easy to understand and maintain
- **Middleware composition** with clear dependency chains
- **Environment-specific configuration** that adapts to deployment needs
- **Security features** built into the configuration
- **Performance optimization** through caching and compression directives

The Go SDK seamlessly integrates web directives with existing Go web frameworks, making them feel like native Go features while providing the power and flexibility of TuskLang's directive system.

**Next**: Explore API directives, CLI directives, and other specialized directive types in the following guides. 