# Middleware Directives - Go

## 🎯 What Are Middleware Directives?

Middleware directives (`#middleware`) in TuskLang allow you to define request processing pipelines, authentication, logging, CORS handling, and middleware composition directly in your configuration files. They transform static config into executable middleware logic.

```go
// Middleware directives define your entire request processing pipeline
type MiddlewareConfig struct {
    Pipeline    []string `tsk:"#middleware_pipeline"`
    Auth        map[string]string `tsk:"#middleware_auth"`
    Logging     map[string]string `tsk:"#middleware_logging"`
    CORS        map[string]string `tsk:"#middleware_cors"`
}
```

## 🚀 Why Middleware Directives Matter

### Traditional Middleware Development
```go
// Old way - scattered across multiple files
func main() {
    router := mux.NewRouter()
    
    // Middleware defined in code
    router.Use(loggingMiddleware)
    router.Use(authMiddleware)
    router.Use(corsMiddleware)
    router.Use(rateLimitMiddleware)
    
    // Route-specific middleware
    router.HandleFunc("/api/admin", adminHandler).Methods("GET")
    router.Use(adminAuthMiddleware)
    
    http.ListenAndServe(":8080", router)
}

func loggingMiddleware(next http.Handler) http.Handler {
    return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
        start := time.Now()
        next.ServeHTTP(w, r)
        log.Printf("%s %s %v", r.Method, r.URL.Path, time.Since(start))
    })
}
```

### TuskLang Middleware Directives - Declarative & Dynamic
```tsk
# middleware.tsk - Complete middleware definition
middleware_pipeline: #middleware("""
    logging -> middleware.Logging
    cors -> middleware.CORS
    auth -> middleware.Auth
    rate_limit -> middleware.RateLimit
    compression -> middleware.Compression
""")

middleware_auth: #middleware("""
    jwt -> middleware.JWT
    api_key -> middleware.APIKey
    session -> middleware.Session
    oauth -> middleware.OAuth
""")

middleware_logging: #middleware("""
    request_logging -> middleware.RequestLogging
    error_logging -> middleware.ErrorLogging
    performance_logging -> middleware.PerformanceLogging
    audit_logging -> middleware.AuditLogging
""")

middleware_cors: #middleware("""
    origins -> ["http://localhost:3000", "https://app.example.com"]
    methods -> ["GET", "POST", "PUT", "DELETE", "OPTIONS"]
    headers -> ["Content-Type", "Authorization"]
    credentials -> true
""")
```

## 📋 Middleware Directive Types

### 1. **Pipeline Directives** (`#middleware_pipeline`)
- Middleware execution order
- Conditional middleware
- Route-specific middleware
- Error handling middleware

### 2. **Auth Directives** (`#middleware_auth`)
- Authentication methods
- Authorization rules
- Token validation
- Session management

### 3. **Logging Directives** (`#middleware_logging`)
- Request logging
- Error logging
- Performance monitoring
- Audit trails

### 4. **CORS Directives** (`#middleware_cors`)
- Cross-origin configuration
- Allowed origins
- HTTP methods
- Headers and credentials

## 🔧 Basic Middleware Directive Syntax

### Simple Pipeline Definition
```tsk
# Basic middleware pipeline
pipeline: #middleware("logging -> auth -> cors")
```

### Pipeline with Configuration
```tsk
# Pipeline with specific middleware
api_pipeline: #middleware("""
    logging -> middleware.RequestLogging
    cors -> middleware.CORS
    auth -> middleware.JWT
    rate_limit -> middleware.RateLimit
""")
```

### Conditional Middleware
```tsk
# Conditional middleware based on environment
production_pipeline: #if(
    #env("ENVIRONMENT") == "production",
    #middleware("logging -> auth -> cors -> rate_limit -> compression"),
    #middleware("logging -> cors")
)
```

## 🎯 Go Integration Patterns

### Struct Tags for Middleware Directives
```go
type MiddlewareConfig struct {
    // Middleware pipeline
    Pipeline string `tsk:"#middleware_pipeline"`
    
    // Authentication configuration
    Auth string `tsk:"#middleware_auth"`
    
    // Logging configuration
    Logging string `tsk:"#middleware_logging"`
    
    // CORS configuration
    CORS string `tsk:"#middleware_cors"`
}
```

### Middleware Application Setup
```go
package main

import (
    "github.com/tusklang/go-sdk"
    "github.com/gorilla/mux"
    "net/http"
)

func main() {
    // Load middleware configuration
    config := tusk.LoadConfig("middleware.tsk")
    
    var middlewareConfig MiddlewareConfig
    config.Unmarshal(&middlewareConfig)
    
    // Create router
    router := mux.NewRouter()
    
    // Apply middleware pipeline from directives
    tusk.ApplyMiddlewarePipeline(router, middlewareConfig.Pipeline)
    
    // Setup routes
    setupRoutes(router)
    
    // Start server
    http.ListenAndServe(":8080", router)
}
```

### Middleware Implementation
```go
package middleware

import (
    "context"
    "encoding/json"
    "log"
    "net/http"
    "time"
    "github.com/gorilla/mux"
)

// Request logging middleware
func RequestLogging(next http.Handler) http.Handler {
    return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
        start := time.Now()
        
        // Create response writer wrapper to capture status code
        wrappedWriter := &responseWriter{ResponseWriter: w, statusCode: http.StatusOK}
        
        next.ServeHTTP(wrappedWriter, r)
        
        duration := time.Since(start)
        
        // Log request details
        log.Printf("%s %s %d %v %s", 
            r.Method, 
            r.URL.Path, 
            wrappedWriter.statusCode, 
            duration, 
            r.RemoteAddr,
        )
    })
}

// CORS middleware
func CORS(next http.Handler) http.Handler {
    return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
        // Set CORS headers
        w.Header().Set("Access-Control-Allow-Origin", "*")
        w.Header().Set("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS")
        w.Header().Set("Access-Control-Allow-Headers", "Content-Type, Authorization")
        
        // Handle preflight requests
        if r.Method == "OPTIONS" {
            w.WriteHeader(http.StatusOK)
            return
        }
        
        next.ServeHTTP(w, r)
    })
}

// JWT authentication middleware
func JWT(next http.Handler) http.Handler {
    return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
        // Extract token from Authorization header
        authHeader := r.Header.Get("Authorization")
        if authHeader == "" {
            http.Error(w, "Authorization header required", http.StatusUnauthorized)
            return
        }
        
        // Validate JWT token
        token, err := validateJWT(authHeader)
        if err != nil {
            http.Error(w, "Invalid token", http.StatusUnauthorized)
            return
        }
        
        // Add user info to request context
        ctx := context.WithValue(r.Context(), "user", token.Claims)
        next.ServeHTTP(w, r.WithContext(ctx))
    })
}

// Rate limiting middleware
func RateLimit(next http.Handler) http.Handler {
    return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
        // Get client identifier
        clientID := getClientID(r)
        
        // Check rate limit
        if !isAllowed(clientID) {
            http.Error(w, "Rate limit exceeded", http.StatusTooManyRequests)
            return
        }
        
        next.ServeHTTP(w, r)
    })
}

// Compression middleware
func Compression(next http.Handler) http.Handler {
    return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
        // Check if client supports compression
        if !supportsCompression(r) {
            next.ServeHTTP(w, r)
            return
        }
        
        // Create compressed response writer
        compressedWriter := newCompressedWriter(w, r)
        defer compressedWriter.Close()
        
        next.ServeHTTP(compressedWriter, r)
    })
}

// Response writer wrapper to capture status code
type responseWriter struct {
    http.ResponseWriter
    statusCode int
}

func (rw *responseWriter) WriteHeader(code int) {
    rw.statusCode = code
    rw.ResponseWriter.WriteHeader(code)
}
```

## 🔄 Advanced Middleware Patterns

### Route-Specific Middleware
```tsk
# Route-specific middleware configuration
route_middleware: #middleware("""
    /api/admin/* -> admin_auth, admin_logging
    /api/public/* -> public_logging
    /api/private/* -> auth, private_logging
    /health -> health_logging
""")
```

### Conditional Middleware Execution
```tsk
# Conditional middleware based on request properties
conditional_middleware: #middleware("""
    # Apply compression only for large responses
    compression -> #if(response_size > 1024, middleware.Compression)
    
    # Apply rate limiting only for API routes
    rate_limit -> #if(path_starts_with("/api"), middleware.RateLimit)
    
    # Apply caching only for GET requests
    caching -> #if(method == "GET", middleware.Caching)
""")
```

### Middleware Composition
```tsk
# Composed middleware chains
composed_middleware: #middleware("""
    # Authentication chain
    auth_chain -> jwt -> session -> permissions
    
    # Logging chain
    logging_chain -> request_logging -> error_logging -> audit_logging
    
    # Security chain
    security_chain -> cors -> csrf -> rate_limit -> compression
""")
```

## 🛡️ Security Middleware

### Security Configuration
```tsk
# Security middleware configuration
security_middleware: #middleware("""
    # CORS configuration
    cors -> middleware.CORS
        origins: ["https://app.example.com", "https://admin.example.com"]
        methods: ["GET", "POST", "PUT", "DELETE", "OPTIONS"]
        headers: ["Content-Type", "Authorization", "X-Requested-With"]
        credentials: true
        max_age: 86400
    
    # CSRF protection
    csrf -> middleware.CSRF
        token_header: "X-CSRF-Token"
        cookie_name: "csrf_token"
        secure: true
    
    # Security headers
    security_headers -> middleware.SecurityHeaders
        hsts: true
        csp: "default-src 'self'"
        xss_protection: true
        content_type_nosniff: true
        frame_options: "DENY"
    
    # Rate limiting
    rate_limit -> middleware.RateLimit
        requests_per_minute: 100
        burst_size: 20
        key_func: "ip_address"
""")
```

### Go Security Middleware Implementation
```go
package security

import (
    "crypto/rand"
    "encoding/base64"
    "net/http"
    "time"
)

// CSRF middleware
func CSRF(next http.Handler) http.Handler {
    return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
        if r.Method == "GET" {
            // Generate CSRF token for GET requests
            token := generateCSRFToken()
            http.SetCookie(w, &http.Cookie{
                Name:     "csrf_token",
                Value:    token,
                Path:     "/",
                Secure:   true,
                HttpOnly: true,
                SameSite: http.SameSiteStrictMode,
            })
        } else {
            // Validate CSRF token for non-GET requests
            token := r.Header.Get("X-CSRF-Token")
            if !validateCSRFToken(token, r) {
                http.Error(w, "CSRF token validation failed", http.StatusForbidden)
                return
            }
        }
        
        next.ServeHTTP(w, r)
    })
}

// Security headers middleware
func SecurityHeaders(next http.Handler) http.Handler {
    return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
        // HSTS header
        w.Header().Set("Strict-Transport-Security", "max-age=31536000; includeSubDomains")
        
        // Content Security Policy
        w.Header().Set("Content-Security-Policy", "default-src 'self'; script-src 'self' 'unsafe-inline'")
        
        // XSS Protection
        w.Header().Set("X-XSS-Protection", "1; mode=block")
        
        // Content Type Sniffing Protection
        w.Header().Set("X-Content-Type-Options", "nosniff")
        
        // Frame Options
        w.Header().Set("X-Frame-Options", "DENY")
        
        // Referrer Policy
        w.Header().Set("Referrer-Policy", "strict-origin-when-cross-origin")
        
        next.ServeHTTP(w, r)
    })
}

// Generate CSRF token
func generateCSRFToken() string {
    b := make([]byte, 32)
    rand.Read(b)
    return base64.StdEncoding.EncodeToString(b)
}

// Validate CSRF token
func validateCSRFToken(token string, r *http.Request) bool {
    cookie, err := r.Cookie("csrf_token")
    if err != nil {
        return false
    }
    
    return token == cookie.Value
}
```

## ⚡ Performance Middleware

### Performance Configuration
```tsk
# Performance middleware configuration
performance_middleware: #middleware("""
    # Caching middleware
    caching -> middleware.Caching
        cache_duration: 300
        cache_headers: ["ETag", "Last-Modified"]
        cache_control: "public, max-age=300"
    
    # Compression middleware
    compression -> middleware.Compression
        min_size: 1024
        content_types: ["text/html", "text/css", "application/json"]
        algorithms: ["gzip", "deflate"]
    
    # Response time monitoring
    response_time -> middleware.ResponseTime
        log_slow_requests: true
        slow_request_threshold: 1000ms
        metrics_enabled: true
""")
```

### Go Performance Middleware Implementation
```go
package performance

import (
    "compress/gzip"
    "io"
    "net/http"
    "strings"
    "time"
)

// Caching middleware
func Caching(next http.Handler) http.Handler {
    return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
        // Check if response should be cached
        if !shouldCache(r) {
            next.ServeHTTP(w, r)
            return
        }
        
        // Check cache
        cacheKey := generateCacheKey(r)
        if cachedResponse := getFromCache(cacheKey); cachedResponse != nil {
            writeCachedResponse(w, cachedResponse)
            return
        }
        
        // Create response writer that captures the response
        responseWriter := &cachingResponseWriter{
            ResponseWriter: w,
            cacheKey:       cacheKey,
        }
        
        next.ServeHTTP(responseWriter, r)
        
        // Cache the response
        cacheResponse(cacheKey, responseWriter.response)
    })
}

// Compression middleware
func Compression(next http.Handler) http.Handler {
    return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
        // Check if client supports compression
        acceptEncoding := r.Header.Get("Accept-Encoding")
        if !strings.Contains(acceptEncoding, "gzip") {
            next.ServeHTTP(w, r)
            return
        }
        
        // Check if response should be compressed
        if !shouldCompress(r) {
            next.ServeHTTP(w, r)
            return
        }
        
        // Create gzip writer
        gzipWriter := gzip.NewWriter(w)
        defer gzipWriter.Close()
        
        // Set headers
        w.Header().Set("Content-Encoding", "gzip")
        w.Header().Set("Vary", "Accept-Encoding")
        
        // Create response writer wrapper
        compressedWriter := &compressedResponseWriter{
            ResponseWriter: w,
            gzipWriter:     gzipWriter,
        }
        
        next.ServeHTTP(compressedWriter, r)
    })
}

// Response time monitoring middleware
func ResponseTime(next http.Handler) http.Handler {
    return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
        start := time.Now()
        
        next.ServeHTTP(w, r)
        
        duration := time.Since(start)
        
        // Log slow requests
        if duration > 1*time.Second {
            log.Printf("Slow request: %s %s took %v", r.Method, r.URL.Path, duration)
        }
        
        // Record metrics
        recordResponseTime(r.URL.Path, duration)
    })
}

// Caching response writer
type cachingResponseWriter struct {
    http.ResponseWriter
    cacheKey  string
    response  []byte
    written   bool
}

func (crw *cachingResponseWriter) Write(data []byte) (int, error) {
    if !crw.written {
        crw.response = make([]byte, len(data))
        copy(crw.response, data)
        crw.written = true
    }
    
    return crw.ResponseWriter.Write(data)
}

// Compressed response writer
type compressedResponseWriter struct {
    http.ResponseWriter
    gzipWriter *gzip.Writer
}

func (crw *compressedResponseWriter) Write(data []byte) (int, error) {
    return crw.gzipWriter.Write(data)
}
```

## 🔧 Error Handling Middleware

### Error Handling Configuration
```tsk
# Error handling middleware configuration
error_middleware: #middleware("""
    # Global error handler
    error_handler -> middleware.ErrorHandler
        log_errors: true
        return_errors: false
        error_format: json
    
    # Panic recovery
    panic_recovery -> middleware.PanicRecovery
        log_panics: true
        return_500: true
    
    # Timeout handling
    timeout -> middleware.Timeout
        timeout_duration: 30s
        timeout_handler: middleware.TimeoutHandler
""")
```

### Go Error Handling Implementation
```go
package errors

import (
    "encoding/json"
    "log"
    "net/http"
    "runtime/debug"
)

// Error handler middleware
func ErrorHandler(next http.Handler) http.Handler {
    return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
        defer func() {
            if err := recover(); err != nil {
                log.Printf("Panic recovered: %v", err)
                log.Printf("Stack trace: %s", debug.Stack())
                
                http.Error(w, "Internal server error", http.StatusInternalServerError)
            }
        }()
        
        next.ServeHTTP(w, r)
    })
}

// Timeout middleware
func Timeout(timeout time.Duration) func(http.Handler) http.Handler {
    return func(next http.Handler) http.Handler {
        return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
            ctx, cancel := context.WithTimeout(r.Context(), timeout)
            defer cancel()
            
            r = r.WithContext(ctx)
            
            done := make(chan bool, 1)
            go func() {
                next.ServeHTTP(w, r)
                done <- true
            }()
            
            select {
            case <-done:
                return
            case <-ctx.Done():
                http.Error(w, "Request timeout", http.StatusRequestTimeout)
                return
            }
        })
    }
}

// Structured error response
type ErrorResponse struct {
    Error   string `json:"error"`
    Code    string `json:"code"`
    Message string `json:"message"`
    Details map[string]interface{} `json:"details,omitempty"`
}

func WriteErrorResponse(w http.ResponseWriter, statusCode int, err error) {
    errorResponse := ErrorResponse{
        Error:   http.StatusText(statusCode),
        Code:    fmt.Sprintf("%d", statusCode),
        Message: err.Error(),
    }
    
    w.Header().Set("Content-Type", "application/json")
    w.WriteHeader(statusCode)
    
    if err := json.NewEncoder(w).Encode(errorResponse); err != nil {
        log.Printf("Failed to encode error response: %v", err)
    }
}
```

## 🎯 Real-World Example

### Complete Middleware Configuration
```tsk
# middleware-config.tsk - Complete middleware configuration

# Environment configuration
environment: #env("ENVIRONMENT", "development")
debug_mode: #env("DEBUG", "false")

# Middleware pipeline
pipeline: #middleware("""
    # Request processing
    request_id -> middleware.RequestID
    logging -> middleware.RequestLogging
    cors -> middleware.CORS
    compression -> middleware.Compression
    
    # Security
    security_headers -> middleware.SecurityHeaders
    csrf -> middleware.CSRF
    rate_limit -> middleware.RateLimit
    
    # Authentication
    auth -> middleware.JWT
    
    # Performance
    caching -> middleware.Caching
    response_time -> middleware.ResponseTime
    
    # Error handling
    error_handler -> middleware.ErrorHandler
    panic_recovery -> middleware.PanicRecovery
""")

# Authentication configuration
auth_config: #middleware("""
    jwt -> middleware.JWT
        secret: #env("JWT_SECRET")
        issuer: "myapp"
        audience: "myapp-users"
        expiration: 24h
    
    session -> middleware.Session
        store: "redis"
        secret: #env("SESSION_SECRET")
        max_age: 7d
    
    api_key -> middleware.APIKey
        header: "X-API-Key"
        keys: #env("API_KEYS").split(",")
""")

# Logging configuration
logging_config: #middleware("""
    request_logging -> middleware.RequestLogging
        level: info
        format: json
        include_headers: ["User-Agent", "X-Forwarded-For"]
        exclude_paths: ["/health", "/metrics"]
    
    error_logging -> middleware.ErrorLogging
        level: error
        format: json
        include_stack_trace: true
    
    audit_logging -> middleware.AuditLogging
        level: info
        format: json
        sensitive_fields: ["password", "token"]
""")

# CORS configuration
cors_config: #middleware("""
    origins -> #if(
        #env("ENVIRONMENT") == "production",
        ["https://app.example.com", "https://admin.example.com"],
        ["http://localhost:3000", "http://localhost:8080"]
    )
    methods -> ["GET", "POST", "PUT", "DELETE", "OPTIONS"]
    headers -> ["Content-Type", "Authorization", "X-Requested-With"]
    credentials -> true
    max_age -> 86400
""")

# Rate limiting configuration
rate_limit_config: #middleware("""
    requests_per_minute -> #if(
        #env("ENVIRONMENT") == "production",
        100,
        1000
    )
    burst_size -> 20
    key_func -> "ip_address"
    excluded_paths -> ["/health", "/metrics"]
""")

# Performance configuration
performance_config: #middleware("""
    cache_duration -> 300
    compression_min_size -> 1024
    response_time_threshold -> 1000ms
    metrics_enabled -> true
""")
```

### Go Middleware Application Implementation
```go
package main

import (
    "fmt"
    "log"
    "net/http"
    "github.com/tusklang/go-sdk"
    "github.com/gorilla/mux"
)

type MiddlewareConfig struct {
    Environment        string `tsk:"environment"`
    DebugMode          bool   `tsk:"debug_mode"`
    Pipeline           string `tsk:"pipeline"`
    AuthConfig         string `tsk:"auth_config"`
    LoggingConfig      string `tsk:"logging_config"`
    CORSConfig         string `tsk:"cors_config"`
    RateLimitConfig    string `tsk:"rate_limit_config"`
    PerformanceConfig  string `tsk:"performance_config"`
}

func main() {
    // Load middleware configuration
    config := tusk.LoadConfig("middleware-config.tsk")
    
    var middlewareConfig MiddlewareConfig
    if err := config.Unmarshal(&middlewareConfig); err != nil {
        log.Fatal("Failed to load middleware config:", err)
    }
    
    // Create router
    router := mux.NewRouter()
    
    // Apply middleware pipeline from directives
    tusk.ApplyMiddlewarePipeline(router, middlewareConfig.Pipeline)
    
    // Setup routes
    setupRoutes(router)
    
    // Start server
    addr := fmt.Sprintf(":%s", #env("PORT", "8080"))
    log.Printf("Starting server on %s in %s mode", addr, middlewareConfig.Environment)
    
    if err := http.ListenAndServe(addr, router); err != nil {
        log.Fatal("Server failed:", err)
    }
}

func setupRoutes(router *mux.Router) {
    // API routes
    api := router.PathPrefix("/api").Subrouter()
    
    // Public routes
    api.HandleFunc("/health", healthHandler).Methods("GET")
    api.HandleFunc("/metrics", metricsHandler).Methods("GET")
    
    // Protected routes
    protected := api.PathPrefix("/v1").Subrouter()
    protected.Use(authMiddleware)
    
    protected.HandleFunc("/users", usersHandler).Methods("GET", "POST")
    protected.HandleFunc("/users/{id}", userHandler).Methods("GET", "PUT", "DELETE")
    
    // Admin routes
    admin := api.PathPrefix("/admin").Subrouter()
    admin.Use(adminAuthMiddleware)
    
    admin.HandleFunc("/stats", statsHandler).Methods("GET")
    admin.HandleFunc("/config", configHandler).Methods("GET", "PUT")
}
```

## 🎯 Best Practices

### 1. **Order Middleware Correctly**
```tsk
# Correct middleware order
correct_order: #middleware("""
    # 1. Request identification and logging
    request_id -> logging
    
    # 2. Security (CORS, headers)
    cors -> security_headers
    
    # 3. Authentication and authorization
    auth -> permissions
    
    # 4. Rate limiting
    rate_limit
    
    # 5. Business logic
    compression -> caching
    
    # 6. Error handling (last)
    error_handler -> panic_recovery
""")
```

### 2. **Use Environment-Specific Configuration**
```tsk
# Different middleware for different environments
environment_middleware: #if(
    #env("ENVIRONMENT") == "production",
    #middleware("logging -> auth -> cors -> rate_limit -> compression"),
    #middleware("logging -> cors")
)
```

### 3. **Implement Proper Error Handling**
```go
// Comprehensive error handling in middleware
func handleMiddlewareError(w http.ResponseWriter, err error, context string) {
    log.Printf("Middleware error in %s: %v", context, err)
    
    // Send structured error response
    errorResponse := ErrorResponse{
        Error:   "middleware_error",
        Code:    "500",
        Message: "Internal middleware error",
        Details: map[string]interface{}{
            "context": context,
            "error":   err.Error(),
        },
    }
    
    w.Header().Set("Content-Type", "application/json")
    w.WriteHeader(http.StatusInternalServerError)
    json.NewEncoder(w).Encode(errorResponse)
}
```

### 4. **Monitor Middleware Performance**
```go
// Middleware performance monitoring
func monitorMiddlewarePerformance(middlewareName string, startTime time.Time) {
    duration := time.Since(startTime)
    
    // Record metrics
    metrics := map[string]interface{}{
        "middleware": middlewareName,
        "duration":   duration.Milliseconds(),
        "timestamp":  time.Now(),
    }
    
    if err := recordMetrics(metrics); err != nil {
        log.Printf("Failed to record middleware metrics: %v", err)
    }
    
    // Alert on slow middleware
    if duration > 100*time.Millisecond {
        log.Printf("Slow middleware: %s took %v", middlewareName, duration)
    }
}
```

## 🎯 Summary

Middleware directives in TuskLang provide a powerful, declarative way to define request processing pipelines. They enable:

- **Declarative middleware composition** that is easy to understand and maintain
- **Flexible middleware ordering** with conditional execution
- **Built-in security features** including CORS, CSRF, and rate limiting
- **Comprehensive logging and monitoring** capabilities
- **Performance optimization** with caching and compression

The Go SDK seamlessly integrates middleware directives with existing Go web frameworks, making them feel like native Go features while providing the power and flexibility of TuskLang's directive system.

**Next**: Explore auth directives, cache directives, and other specialized directive types in the following guides. 