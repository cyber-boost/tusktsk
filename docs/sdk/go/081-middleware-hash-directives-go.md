# Middleware Hash Directives in TuskLang for Go

## Overview

Middleware hash directives in TuskLang provide powerful middleware configuration capabilities directly in your configuration files. These directives enable you to define sophisticated middleware chains, request processing pipelines, and cross-cutting concerns with Go integration for web applications and APIs.

## Basic Middleware Directive Syntax

```go
// TuskLang middleware directive
#middleware {
    global: {
        cors: {
            enabled: true
            origins: ["http://localhost:3000", "https://myapp.com"]
            methods: ["GET", "POST", "PUT", "DELETE"]
            headers: ["Content-Type", "Authorization"]
        }
        
        logging: {
            enabled: true
            level: "info"
            format: "json"
            include_headers: true
        }
        
        compression: {
            enabled: true
            level: 6
            types: ["text/html", "application/json", "text/css"]
        }
    }
    
    routes: {
        "/api/v1": {
            auth: {
                type: "jwt"
                secret: "@env('JWT_SECRET')"
                required: true
            }
            
            rate_limit: {
                enabled: true
                requests_per_minute: 100
                burst: 20
            }
            
            validation: {
                enabled: true
                schema: "api_v1_schema"
            }
        }
        
        "/admin": {
            auth: {
                type: "session"
                required: true
                roles: ["admin"]
            }
            
            audit: {
                enabled: true
                log_actions: true
                log_responses: false
            }
        }
    }
}
```

## Go Integration

```go
package main

import (
    "context"
    "fmt"
    "log"
    "net/http"
    "strings"
    "github.com/gorilla/mux"
    "github.com/tusklang/go-sdk"
)

type MiddlewareDirective struct {
    Global map[string]Middleware `tsk:"global"`
    Routes map[string]RouteConfig `tsk:"routes"`
}

type Middleware struct {
    Enabled bool                   `tsk:"enabled"`
    Config  map[string]interface{} `tsk:",inline"`
}

type RouteConfig struct {
    Auth       *AuthConfig       `tsk:"auth"`
    RateLimit  *RateLimitConfig  `tsk:"rate_limit"`
    Validation *ValidationConfig `tsk:"validation"`
    Audit      *AuditConfig      `tsk:"audit"`
}

type AuthConfig struct {
    Type     string   `tsk:"type"`
    Secret   string   `tsk:"secret"`
    Required bool     `tsk:"required"`
    Roles    []string `tsk:"roles"`
}

type RateLimitConfig struct {
    Enabled           bool `tsk:"enabled"`
    RequestsPerMinute int  `tsk:"requests_per_minute"`
    Burst             int  `tsk:"burst"`
}

type ValidationConfig struct {
    Enabled bool   `tsk:"enabled"`
    Schema  string `tsk:"schema"`
}

type AuditConfig struct {
    Enabled       bool `tsk:"enabled"`
    LogActions    bool `tsk:"log_actions"`
    LogResponses  bool `tsk:"log_responses"`
}

type MiddlewareManager struct {
    directive MiddlewareDirective
    router    *mux.Router
    rateLimiters map[string]*RateLimiter
}

func main() {
    // Load middleware configuration
    config, err := tusk.LoadFile("middleware-config.tsk")
    if err != nil {
        log.Fatalf("Error loading middleware config: %v", err)
    }
    
    var middlewareDirective MiddlewareDirective
    if err := config.Get("#middleware", &middlewareDirective); err != nil {
        log.Fatalf("Error parsing middleware directive: %v", err)
    }
    
    // Initialize middleware manager
    manager := NewMiddlewareManager(middlewareDirective)
    
    // Setup routes with middleware
    manager.SetupRoutes()
    
    // Start server
    log.Println("Server starting on :8080")
    log.Fatal(http.ListenAndServe(":8080", manager.router))
}

func NewMiddlewareManager(directive MiddlewareDirective) *MiddlewareManager {
    return &MiddlewareManager{
        directive:    directive,
        router:       mux.NewRouter(),
        rateLimiters: make(map[string]*RateLimiter),
    }
}

func (m *MiddlewareManager) SetupRoutes() {
    // Apply global middleware
    m.applyGlobalMiddleware()
    
    // Setup route-specific middleware
    for route, config := range m.directive.Routes {
        m.setupRouteMiddleware(route, config)
    }
    
    // Add default routes
    m.router.HandleFunc("/", m.handleHome).Methods("GET")
    m.router.HandleFunc("/api/v1/users", m.handleUsers).Methods("GET", "POST")
    m.router.HandleFunc("/admin/dashboard", m.handleAdminDashboard).Methods("GET")
}

func (m *MiddlewareManager) applyGlobalMiddleware() {
    // CORS middleware
    if cors, exists := m.directive.Global["cors"]; exists && cors.Enabled {
        m.router.Use(m.corsMiddleware(cors.Config))
    }
    
    // Logging middleware
    if logging, exists := m.directive.Global["logging"]; exists && logging.Enabled {
        m.router.Use(m.loggingMiddleware(logging.Config))
    }
    
    // Compression middleware
    if compression, exists := m.directive.Global["compression"]; exists && compression.Enabled {
        m.router.Use(m.compressionMiddleware(compression.Config))
    }
}

func (m *MiddlewareManager) setupRouteMiddleware(route string, config RouteConfig) {
    subrouter := m.router.PathPrefix(route).Subrouter()
    
    // Authentication middleware
    if config.Auth != nil && config.Auth.Required {
        subrouter.Use(m.authMiddleware(*config.Auth))
    }
    
    // Rate limiting middleware
    if config.RateLimit != nil && config.RateLimit.Enabled {
        subrouter.Use(m.rateLimitMiddleware(route, *config.RateLimit))
    }
    
    // Validation middleware
    if config.Validation != nil && config.Validation.Enabled {
        subrouter.Use(m.validationMiddleware(*config.Validation))
    }
    
    // Audit middleware
    if config.Audit != nil && config.Audit.Enabled {
        subrouter.Use(m.auditMiddleware(*config.Audit))
    }
}

// Middleware implementations
func (m *MiddlewareManager) corsMiddleware(config map[string]interface{}) func(http.Handler) http.Handler {
    origins := config["origins"].([]string)
    methods := config["methods"].([]string)
    headers := config["headers"].([]string)
    
    return func(next http.Handler) http.Handler {
        return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
            origin := r.Header.Get("Origin")
            
            // Check if origin is allowed
            allowed := false
            for _, allowedOrigin := range origins {
                if origin == allowedOrigin {
                    allowed = true
                    break
                }
            }
            
            if allowed {
                w.Header().Set("Access-Control-Allow-Origin", origin)
            }
            
            w.Header().Set("Access-Control-Allow-Methods", strings.Join(methods, ", "))
            w.Header().Set("Access-Control-Allow-Headers", strings.Join(headers, ", "))
            
            if r.Method == "OPTIONS" {
                w.WriteHeader(http.StatusOK)
                return
            }
            
            next.ServeHTTP(w, r)
        })
    }
}

func (m *MiddlewareManager) loggingMiddleware(config map[string]interface{}) func(http.Handler) http.Handler {
    level := config["level"].(string)
    format := config["format"].(string)
    includeHeaders := config["include_headers"].(bool)
    
    return func(next http.Handler) http.Handler {
        return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
            start := time.Now()
            
            // Create response writer wrapper to capture status
            wrappedWriter := &responseWriter{ResponseWriter: w}
            
            next.ServeHTTP(wrappedWriter, r)
            
            duration := time.Since(start)
            
            // Log request details
            logData := map[string]interface{}{
                "method":     r.Method,
                "path":       r.URL.Path,
                "status":     wrappedWriter.status,
                "duration":   duration.String(),
                "user_agent": r.UserAgent(),
            }
            
            if includeHeaders {
                logData["headers"] = r.Header
            }
            
            if format == "json" {
                log.Printf("Request: %+v", logData)
            } else {
                log.Printf("%s %s %d %v", r.Method, r.URL.Path, wrappedWriter.status, duration)
            }
        })
    }
}

func (m *MiddlewareManager) authMiddleware(config AuthConfig) func(http.Handler) http.Handler {
    return func(next http.Handler) http.Handler {
        return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
            var user *User
            
            switch config.Type {
            case "jwt":
                user = m.validateJWT(r, config.Secret)
            case "session":
                user = m.validateSession(r)
            default:
                http.Error(w, "Unsupported auth type", http.StatusUnauthorized)
                return
            }
            
            if user == nil {
                http.Error(w, "Unauthorized", http.StatusUnauthorized)
                return
            }
            
            // Check roles if specified
            if len(config.Roles) > 0 {
                hasRole := false
                for _, role := range config.Roles {
                    if user.HasRole(role) {
                        hasRole = true
                        break
                    }
                }
                if !hasRole {
                    http.Error(w, "Insufficient permissions", http.StatusForbidden)
                    return
                }
            }
            
            // Add user to context
            ctx := context.WithValue(r.Context(), "user", user)
            next.ServeHTTP(w, r.WithContext(ctx))
        })
    }
}

func (m *MiddlewareManager) rateLimitMiddleware(route string, config RateLimitConfig) func(http.Handler) http.Handler {
    // Get or create rate limiter for this route
    limiter, exists := m.rateLimiters[route]
    if !exists {
        limiter = NewRateLimiter(config.RequestsPerMinute, config.Burst)
        m.rateLimiters[route] = limiter
    }
    
    return func(next http.Handler) http.Handler {
        return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
            if !limiter.Allow() {
                http.Error(w, "Rate limit exceeded", http.StatusTooManyRequests)
                return
            }
            
            next.ServeHTTP(w, r)
        })
    }
}

func (m *MiddlewareManager) validationMiddleware(config ValidationConfig) func(http.Handler) http.Handler {
    return func(next http.Handler) http.Handler {
        return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
            // Load validation schema
            schema, err := m.loadSchema(config.Schema)
            if err != nil {
                log.Printf("Error loading schema: %v", err)
                next.ServeHTTP(w, r)
                return
            }
            
            // Validate request based on schema
            if err := m.validateRequest(r, schema); err != nil {
                http.Error(w, fmt.Sprintf("Validation error: %v", err), http.StatusBadRequest)
                return
            }
            
            next.ServeHTTP(w, r)
        })
    }
}

func (m *MiddlewareManager) auditMiddleware(config AuditConfig) func(http.Handler) http.Handler {
    return func(next http.Handler) http.Handler {
        return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
            start := time.Now()
            
            // Log action if enabled
            if config.LogActions {
                user := r.Context().Value("user")
                log.Printf("AUDIT: User %v performed action %s %s", user, r.Method, r.URL.Path)
            }
            
            // Create response writer wrapper
            wrappedWriter := &responseWriter{ResponseWriter: w}
            
            next.ServeHTTP(wrappedWriter, r)
            
            // Log response if enabled
            if config.LogResponses {
                duration := time.Since(start)
                log.Printf("AUDIT: Response %d in %v for %s %s", 
                    wrappedWriter.status, duration, r.Method, r.URL.Path)
            }
        })
    }
}

// Helper types and functions
type responseWriter struct {
    http.ResponseWriter
    status int
}

func (rw *responseWriter) WriteHeader(code int) {
    rw.status = code
    rw.ResponseWriter.WriteHeader(code)
}

type User struct {
    ID    string
    Email string
    Roles []string
}

func (u *User) HasRole(role string) bool {
    for _, userRole := range u.Roles {
        if userRole == role {
            return true
        }
    }
    return false
}

type RateLimiter struct {
    tokens chan struct{}
    ticker *time.Ticker
}

func NewRateLimiter(requestsPerMinute, burst int) *RateLimiter {
    tokens := make(chan struct{}, burst)
    ticker := time.NewTicker(time.Minute / time.Duration(requestsPerMinute))
    
    // Fill initial tokens
    for i := 0; i < burst; i++ {
        tokens <- struct{}{}
    }
    
    // Refill tokens
    go func() {
        for range ticker.C {
            select {
            case tokens <- struct{}{}:
            default:
            }
        }
    }()
    
    return &RateLimiter{tokens: tokens, ticker: ticker}
}

func (rl *RateLimiter) Allow() bool {
    select {
    case <-rl.tokens:
        return true
    default:
        return false
    }
}

// Handler functions
func (m *MiddlewareManager) handleHome(w http.ResponseWriter, r *http.Request) {
    w.Write([]byte("Welcome to the API!"))
}

func (m *MiddlewareManager) handleUsers(w http.ResponseWriter, r *http.Request) {
    w.Header().Set("Content-Type", "application/json")
    w.Write([]byte(`{"users": []}`))
}

func (m *MiddlewareManager) handleAdminDashboard(w http.ResponseWriter, r *http.Request) {
    user := r.Context().Value("user").(*User)
    w.Header().Set("Content-Type", "application/json")
    w.Write([]byte(fmt.Sprintf(`{"message": "Welcome admin %s"}`, user.Email)))
}

// Helper functions (implementations would depend on your specific needs)
func (m *MiddlewareManager) validateJWT(r *http.Request, secret string) *User {
    // Implement JWT validation
    return &User{ID: "1", Email: "user@example.com", Roles: []string{"user"}}
}

func (m *MiddlewareManager) validateSession(r *http.Request) *User {
    // Implement session validation
    return &User{ID: "1", Email: "user@example.com", Roles: []string{"user"}}
}

func (m *MiddlewareManager) loadSchema(schemaName string) (interface{}, error) {
    // Implement schema loading
    return nil, nil
}

func (m *MiddlewareManager) validateRequest(r *http.Request, schema interface{}) error {
    // Implement request validation
    return nil
}

func (m *MiddlewareManager) compressionMiddleware(config map[string]interface{}) func(http.Handler) http.Handler {
    // Implement compression middleware
    return func(next http.Handler) http.Handler {
        return next
    }
}
```

## Advanced Middleware Features

### Custom Middleware Creation

```go
// TuskLang configuration with custom middleware
#middleware {
    custom: {
        metrics: {
            enabled: true
            endpoint: "/metrics"
            namespace: "myapp"
        }
        
        cache: {
            enabled: true
            ttl: "5m"
            max_size: 1000
        }
        
        circuit_breaker: {
            enabled: true
            failure_threshold: 5
            recovery_timeout: "30s"
        }
    }
}
```

### Conditional Middleware

```go
// TuskLang configuration with conditional middleware
#middleware {
    conditional: {
        debug: {
            enabled: "@env('DEBUG') == 'true'"
            log_requests: true
            log_responses: true
        }
        
        profiling: {
            enabled: "@env('PROFILE') == 'true'"
            endpoint: "/debug/pprof"
        }
        
        maintenance: {
            enabled: "@file.exists('maintenance.flag')"
            message: "System under maintenance"
        }
    }
}
```

## Performance Considerations

- **Middleware Order**: Order middleware carefully to optimize performance
- **Early Returns**: Use early returns in middleware to avoid unnecessary processing
- **Caching**: Implement caching for expensive middleware operations
- **Async Processing**: Use goroutines for non-blocking middleware operations
- **Resource Cleanup**: Ensure proper cleanup of middleware resources

## Security Notes

- **Input Validation**: Always validate inputs in middleware
- **Authentication**: Implement proper authentication and authorization
- **Rate Limiting**: Use rate limiting to prevent abuse
- **CORS Configuration**: Configure CORS properly for security
- **Audit Logging**: Log security-relevant events

## Best Practices

1. **Middleware Composition**: Compose middleware from small, focused functions
2. **Error Handling**: Implement proper error handling in middleware
3. **Configuration**: Make middleware configurable through TuskLang
4. **Testing**: Test middleware in isolation and as part of the chain
5. **Documentation**: Document middleware behavior and configuration options
6. **Monitoring**: Add monitoring and metrics to middleware

## Integration Examples

### With Gin Framework

```go
import (
    "github.com/gin-gonic/gin"
    "github.com/tusklang/go-sdk"
)

func setupGinMiddleware(config tusk.Config) *gin.Engine {
    var middlewareDirective MiddlewareDirective
    config.Get("#middleware", &middlewareDirective)
    
    router := gin.Default()
    
    // Apply global middleware
    if cors, exists := middlewareDirective.Global["cors"]; exists && cors.Enabled {
        router.Use(corsMiddleware(cors.Config))
    }
    
    if logging, exists := middlewareDirective.Global["logging"]; exists && logging.Enabled {
        router.Use(loggingMiddleware(logging.Config))
    }
    
    return router
}
```

### With Echo Framework

```go
import (
    "github.com/labstack/echo/v4"
    "github.com/tusklang/go-sdk"
)

func setupEchoMiddleware(config tusk.Config) *echo.Echo {
    var middlewareDirective MiddlewareDirective
    config.Get("#middleware", &middlewareDirective)
    
    e := echo.New()
    
    // Apply global middleware
    if cors, exists := middlewareDirective.Global["cors"]; exists && cors.Enabled {
        e.Use(corsMiddleware(cors.Config))
    }
    
    return e
}
```

This comprehensive middleware hash directives documentation provides Go developers with everything they need to build sophisticated middleware systems using TuskLang's powerful directive system. 