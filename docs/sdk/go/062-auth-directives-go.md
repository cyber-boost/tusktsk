# Auth Directives - Go

## 🎯 What Are Auth Directives?

Auth directives (`#auth`) in TuskLang allow you to define authentication methods, authorization rules, session management, and token handling directly in your configuration files. They transform static config into executable authentication logic.

```go
// Auth directives define your entire authentication and authorization system
type AuthConfig struct {
    Methods     map[string]string `tsk:"#auth_methods"`
    Rules       map[string]string `tsk:"#auth_rules"`
    Sessions    map[string]string `tsk:"#auth_sessions"`
    Tokens      map[string]string `tsk:"#auth_tokens"`
}
```

## 🚀 Why Auth Directives Matter

### Traditional Auth Development
```go
// Old way - scattered across multiple files
func main() {
    // Auth configuration scattered
    jwtSecret := os.Getenv("JWT_SECRET")
    sessionStore := redis.NewClient(&redis.Options{
        Addr: os.Getenv("REDIS_ADDR"),
    })
    
    // Auth middleware defined in code
    authMiddleware := func(next http.Handler) http.Handler {
        return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
            token := r.Header.Get("Authorization")
            if !validateJWT(token, jwtSecret) {
                http.Error(w, "Unauthorized", http.StatusUnauthorized)
                return
            }
            next.ServeHTTP(w, r)
        })
    }
    
    // Authorization rules hardcoded
    adminOnly := func(next http.Handler) http.Handler {
        return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
            user := getUserFromContext(r.Context())
            if user.Role != "admin" {
                http.Error(w, "Forbidden", http.StatusForbidden)
                return
            }
            next.ServeHTTP(w, r)
        })
    }
}
```

### TuskLang Auth Directives - Declarative & Dynamic
```tsk
# auth.tsk - Complete auth definition
auth_methods: #auth("""
    jwt -> JWT token authentication
        secret: #env("JWT_SECRET")
        issuer: "myapp"
        audience: "myapp-users"
        expiration: 24h
        refresh_threshold: 1h
    
    session -> Session-based authentication
        store: "redis"
        secret: #env("SESSION_SECRET")
        max_age: 7d
        secure: true
        http_only: true
    
    api_key -> API key authentication
        header: "X-API-Key"
        keys: #env("API_KEYS").split(",")
        rate_limit: 1000/hour
    
    oauth -> OAuth 2.0 authentication
        providers: ["google", "github", "facebook"]
        client_id: #env("OAUTH_CLIENT_ID")
        client_secret: #env("OAUTH_CLIENT_SECRET")
        redirect_uri: #env("OAUTH_REDIRECT_URI")
""")

auth_rules: #auth("""
    admin_only -> Role-based access control
        roles: ["admin"]
        resources: ["/api/admin/*", "/api/config/*"]
    
    user_own_data -> User can only access own data
        rule: "user.id == resource.user_id"
        resources: ["/api/users/{id}/*"]
    
    public_read -> Public read access
        roles: ["*"]
        resources: ["/api/public/*"]
        methods: ["GET"]
    
    authenticated_only -> Require authentication
        roles: ["user", "admin"]
        resources: ["/api/private/*"]
""")

auth_sessions: #auth("""
    session_store -> Redis session store
        url: #env("REDIS_URL")
        prefix: "session:"
        ttl: 7d
    
    session_config -> Session configuration
        name: "sid"
        secure: true
        http_only: true
        same_site: "strict"
        max_age: 604800
""")
```

## 📋 Auth Directive Types

### 1. **Method Directives** (`#auth_methods`)
- Authentication method definitions
- JWT configuration
- Session management
- OAuth providers

### 2. **Rule Directives** (`#auth_rules`)
- Authorization rules
- Role-based access control
- Resource-based permissions
- Method restrictions

### 3. **Session Directives** (`#auth_sessions`)
- Session store configuration
- Session security settings
- Session lifecycle management
- Session data storage

### 4. **Token Directives** (`#auth_tokens`)
- Token generation and validation
- Token refresh logic
- Token storage and cleanup
- Token security features

## 🔧 Basic Auth Directive Syntax

### Simple Authentication Method
```tsk
# Basic JWT authentication
jwt_auth: #auth("jwt -> JWT token authentication")
```

### Authentication Method with Configuration
```tsk
# JWT with detailed configuration
jwt_config: #auth("""
    jwt -> JWT token authentication
        secret: #env("JWT_SECRET")
        issuer: "myapp"
        audience: "myapp-users"
        expiration: 24h
        algorithm: "HS256"
""")
```

### Multiple Authentication Methods
```tsk
# Multiple auth methods
auth_methods: #auth("""
    jwt -> JWT token authentication
    session -> Session-based authentication
    api_key -> API key authentication
    oauth -> OAuth 2.0 authentication
""")
```

## 🎯 Go Integration Patterns

### Struct Tags for Auth Directives
```go
type AuthConfig struct {
    // Authentication methods
    Methods string `tsk:"#auth_methods"`
    
    // Authorization rules
    Rules string `tsk:"#auth_rules"`
    
    // Session configuration
    Sessions string `tsk:"#auth_sessions"`
    
    // Token configuration
    Tokens string `tsk:"#auth_tokens"`
}
```

### Auth Application Setup
```go
package main

import (
    "github.com/tusklang/go-sdk"
    "github.com/gorilla/mux"
    "net/http"
)

func main() {
    // Load auth configuration
    config := tusk.LoadConfig("auth.tsk")
    
    var authConfig AuthConfig
    config.Unmarshal(&authConfig)
    
    // Create auth system from directives
    auth := tusk.NewAuthSystem(authConfig)
    
    // Create router
    router := mux.NewRouter()
    
    // Apply auth middleware
    tusk.ApplyAuthMiddleware(router, auth)
    
    // Start server
    http.ListenAndServe(":8080", router)
}
```

### Auth Handler Implementation
```go
package auth

import (
    "context"
    "encoding/json"
    "fmt"
    "net/http"
    "time"
    "github.com/golang-jwt/jwt/v4"
)

// JWT authentication handler
func JWTAuthHandler(secret string) func(http.Handler) http.Handler {
    return func(next http.Handler) http.Handler {
        return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
            // Extract token from Authorization header
            authHeader := r.Header.Get("Authorization")
            if authHeader == "" {
                http.Error(w, "Authorization header required", http.StatusUnauthorized)
                return
            }
            
            // Remove "Bearer " prefix
            tokenString := authHeader
            if len(authHeader) > 7 && authHeader[:7] == "Bearer " {
                tokenString = authHeader[7:]
            }
            
            // Parse and validate JWT
            token, err := jwt.Parse(tokenString, func(token *jwt.Token) (interface{}, error) {
                if _, ok := token.Method.(*jwt.SigningMethodHMAC); !ok {
                    return nil, fmt.Errorf("unexpected signing method: %v", token.Header["alg"])
                }
                return []byte(secret), nil
            })
            
            if err != nil || !token.Valid {
                http.Error(w, "Invalid token", http.StatusUnauthorized)
                return
            }
            
            // Extract claims
            claims, ok := token.Claims.(jwt.MapClaims)
            if !ok {
                http.Error(w, "Invalid token claims", http.StatusUnauthorized)
                return
            }
            
            // Add user info to context
            user := User{
                ID:    claims["sub"].(string),
                Email: claims["email"].(string),
                Role:  claims["role"].(string),
            }
            
            ctx := context.WithValue(r.Context(), "user", user)
            next.ServeHTTP(w, r.WithContext(ctx))
        })
    }
}

// Session authentication handler
func SessionAuthHandler(store SessionStore) func(http.Handler) http.Handler {
    return func(next http.Handler) http.Handler {
        return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
            // Get session from cookie
            sessionID := getSessionID(r)
            if sessionID == "" {
                http.Error(w, "Session required", http.StatusUnauthorized)
                return
            }
            
            // Get session from store
            session, err := store.Get(sessionID)
            if err != nil || session == nil {
                http.Error(w, "Invalid session", http.StatusUnauthorized)
                return
            }
            
            // Check if session is expired
            if session.ExpiresAt.Before(time.Now()) {
                store.Delete(sessionID)
                http.Error(w, "Session expired", http.StatusUnauthorized)
                return
            }
            
            // Add user info to context
            ctx := context.WithValue(r.Context(), "user", session.User)
            next.ServeHTTP(w, r.WithContext(ctx))
        })
    }
}

// API key authentication handler
func APIKeyAuthHandler(validKeys []string) func(http.Handler) http.Handler {
    return func(next http.Handler) http.Handler {
        return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
            // Extract API key from header
            apiKey := r.Header.Get("X-API-Key")
            if apiKey == "" {
                http.Error(w, "API key required", http.StatusUnauthorized)
                return
            }
            
            // Validate API key
            valid := false
            for _, key := range validKeys {
                if apiKey == key {
                    valid = true
                    break
                }
            }
            
            if !valid {
                http.Error(w, "Invalid API key", http.StatusUnauthorized)
                return
            }
            
            next.ServeHTTP(w, r)
        })
    }
}

// Authorization rule handler
func AuthorizationHandler(rules []AuthRule) func(http.Handler) http.Handler {
    return func(next http.Handler) http.Handler {
        return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
            // Get user from context
            user, ok := r.Context().Value("user").(User)
            if !ok {
                http.Error(w, "User not found in context", http.StatusUnauthorized)
                return
            }
            
            // Check authorization rules
            for _, rule := range rules {
                if rule.Matches(r.URL.Path, r.Method) {
                    if !rule.IsAuthorized(user, r) {
                        http.Error(w, "Forbidden", http.StatusForbidden)
                        return
                    }
                }
            }
            
            next.ServeHTTP(w, r)
        })
    }
}
```

## 🔄 Advanced Auth Patterns

### Role-Based Access Control
```tsk
# Role-based access control configuration
rbac_config: #auth("""
    admin_role -> Administrator access
        permissions: ["read", "write", "delete", "admin"]
        resources: ["*"]
        methods: ["*"]
    
    user_role -> User access
        permissions: ["read", "write"]
        resources: ["/api/users/{id}/*", "/api/posts/*"]
        methods: ["GET", "POST", "PUT"]
        conditions: ["user.id == resource.user_id"]
    
    guest_role -> Guest access
        permissions: ["read"]
        resources: ["/api/public/*"]
        methods: ["GET"]
""")
```

### Resource-Based Permissions
```tsk
# Resource-based permissions
resource_permissions: #auth("""
    user_own_data -> Users can only access their own data
        rule: "user.id == resource.user_id"
        resources: ["/api/users/{id}/*", "/api/profiles/{id}/*"]
        methods: ["GET", "PUT", "DELETE"]
    
    public_resources -> Public read access
        rule: "true"
        resources: ["/api/public/*", "/api/blog/*"]
        methods: ["GET"]
    
    admin_resources -> Admin only access
        rule: "user.role == 'admin'"
        resources: ["/api/admin/*", "/api/config/*"]
        methods: ["*"]
""")
```

### Conditional Authorization
```tsk
# Conditional authorization based on context
conditional_auth: #auth("""
    time_based -> Time-based access control
        rule: "current_time.hour >= 9 && current_time.hour <= 17"
        resources: ["/api/business/*"]
        message: "Access only during business hours"
    
    ip_based -> IP-based access control
        rule: "client_ip in allowed_ips"
        resources: ["/api/internal/*"]
        message: "Access only from internal network"
    
    load_based -> Load-based access control
        rule: "system_load < 80"
        resources: ["/api/heavy/*"]
        message: "Access restricted during high load"
""")
```

## 🛡️ Security Features

### Token Security Configuration
```tsk
# Token security configuration
token_security: #auth("""
    jwt_security -> JWT security settings
        algorithm: "HS256"
        key_rotation: true
        rotation_interval: 7d
        blacklist_enabled: true
        blacklist_ttl: 24h
    
    session_security -> Session security settings
        secure: true
        http_only: true
        same_site: "strict"
        max_age: 604800
        regenerate_id: true
        regenerate_interval: 1h
""")
```

### Go Security Implementation
```go
package security

import (
    "crypto/rand"
    "encoding/base64"
    "time"
    "github.com/golang-jwt/jwt/v4"
)

// Secure JWT token generation
func GenerateSecureJWT(claims jwt.MapClaims, secret string) (string, error) {
    // Add standard claims
    claims["iat"] = time.Now().Unix()
    claims["exp"] = time.Now().Add(24 * time.Hour).Unix()
    claims["jti"] = generateTokenID()
    
    // Create token
    token := jwt.NewWithClaims(jwt.SigningMethodHS256, claims)
    
    // Sign token
    return token.SignedString([]byte(secret))
}

// Secure session management
func CreateSecureSession(user User, store SessionStore) (*Session, error) {
    // Generate secure session ID
    sessionID := generateSecureSessionID()
    
    // Create session
    session := &Session{
        ID:        sessionID,
        User:      user,
        CreatedAt: time.Now(),
        ExpiresAt: time.Now().Add(7 * 24 * time.Hour),
        IP:        getClientIP(),
        UserAgent: getUserAgent(),
    }
    
    // Store session
    if err := store.Set(sessionID, session); err != nil {
        return nil, err
    }
    
    return session, nil
}

// Token blacklist management
type TokenBlacklist struct {
    store map[string]time.Time
    mu    sync.RWMutex
}

func NewTokenBlacklist() *TokenBlacklist {
    return &TokenBlacklist{
        store: make(map[string]time.Time),
    }
}

func (tb *TokenBlacklist) Blacklist(tokenID string, expiration time.Time) {
    tb.mu.Lock()
    defer tb.mu.Unlock()
    tb.store[tokenID] = expiration
}

func (tb *TokenBlacklist) IsBlacklisted(tokenID string) bool {
    tb.mu.RLock()
    defer tb.mu.RUnlock()
    
    if expiration, exists := tb.store[tokenID]; exists {
        if time.Now().Before(expiration) {
            return true
        }
        // Clean up expired entry
        delete(tb.store, tokenID)
    }
    
    return false
}

// Generate secure random token ID
func generateTokenID() string {
    b := make([]byte, 32)
    rand.Read(b)
    return base64.URLEncoding.EncodeToString(b)
}

// Generate secure session ID
func generateSecureSessionID() string {
    b := make([]byte, 32)
    rand.Read(b)
    return base64.URLEncoding.EncodeToString(b)
}
```

## ⚡ Performance Optimization

### Auth Performance Configuration
```tsk
# Auth performance configuration
auth_performance: #auth("""
    caching -> Auth result caching
        enabled: true
        ttl: 5m
        cache_key: "auth:{user_id}:{resource}"
    
    rate_limiting -> Auth rate limiting
        login_attempts: 5/minute
        token_requests: 100/minute
        session_creations: 10/minute
    
    connection_pooling -> Database connection pooling
        max_connections: 100
        idle_connections: 10
        connection_timeout: 30s
""")
```

### Go Performance Implementation
```go
package performance

import (
    "context"
    "sync"
    "time"
)

// Auth result cache
type AuthCache struct {
    cache map[string]AuthResult
    mu    sync.RWMutex
    ttl   time.Duration
}

type AuthResult struct {
    Authorized bool
    ExpiresAt  time.Time
}

func NewAuthCache(ttl time.Duration) *AuthCache {
    ac := &AuthCache{
        cache: make(map[string]AuthResult),
        ttl:   ttl,
    }
    
    // Start cleanup goroutine
    go ac.cleanup()
    
    return ac
}

func (ac *AuthCache) Get(key string) (AuthResult, bool) {
    ac.mu.RLock()
    defer ac.mu.RUnlock()
    
    result, exists := ac.cache[key]
    if !exists {
        return AuthResult{}, false
    }
    
    if time.Now().After(result.ExpiresAt) {
        return AuthResult{}, false
    }
    
    return result, true
}

func (ac *AuthCache) Set(key string, result AuthResult) {
    ac.mu.Lock()
    defer ac.mu.Unlock()
    
    result.ExpiresAt = time.Now().Add(ac.ttl)
    ac.cache[key] = result
}

func (ac *AuthCache) cleanup() {
    ticker := time.NewTicker(ac.ttl)
    defer ticker.Stop()
    
    for range ticker.C {
        ac.mu.Lock()
        now := time.Now()
        for key, result := range ac.cache {
            if now.After(result.ExpiresAt) {
                delete(ac.cache, key)
            }
        }
        ac.mu.Unlock()
    }
}

// Rate limiter for auth operations
type AuthRateLimiter struct {
    limiters map[string]*rate.Limiter
    mu       sync.RWMutex
}

func NewAuthRateLimiter() *AuthRateLimiter {
    return &AuthRateLimiter{
        limiters: make(map[string]*rate.Limiter),
    }
}

func (arl *AuthRateLimiter) Allow(operation string, key string) bool {
    limiterKey := fmt.Sprintf("%s:%s", operation, key)
    
    arl.mu.Lock()
    limiter, exists := arl.limiters[limiterKey]
    if !exists {
        // Create new limiter based on operation
        var limit rate.Limit
        switch operation {
        case "login":
            limit = rate.Every(time.Minute / 5) // 5 per minute
        case "token":
            limit = rate.Every(time.Minute / 100) // 100 per minute
        case "session":
            limit = rate.Every(time.Minute / 10) // 10 per minute
        default:
            limit = rate.Every(time.Minute / 60) // 60 per minute
        }
        limiter = rate.NewLimiter(limit, 1)
        arl.limiters[limiterKey] = limiter
    }
    arl.mu.Unlock()
    
    return limiter.Allow()
}
```

## 🔧 Error Handling

### Auth Error Configuration
```tsk
# Auth error handling configuration
auth_errors: #auth("""
    invalid_token -> Invalid or expired token
        status_code: 401
        message: "Invalid or expired token"
        log_level: warn
    
    insufficient_permissions -> Insufficient permissions
        status_code: 403
        message: "Insufficient permissions for this resource"
        log_level: info
    
    session_expired -> Session expired
        status_code: 401
        message: "Session has expired, please login again"
        log_level: info
    
    rate_limit_exceeded -> Rate limit exceeded
        status_code: 429
        message: "Too many authentication attempts"
        log_level: warn
""")
```

### Go Error Handler Implementation
```go
package errors

import (
    "encoding/json"
    "log"
    "net/http"
)

// Auth error types
type AuthError struct {
    Type        string `json:"type"`
    Message     string `json:"message"`
    StatusCode  int    `json:"status_code"`
    LogLevel    string `json:"log_level"`
    Details     map[string]interface{} `json:"details,omitempty"`
}

// Auth error handlers
func HandleAuthError(w http.ResponseWriter, err AuthError) {
    // Log error
    switch err.LogLevel {
    case "debug":
        log.Printf("Auth debug: %s", err.Message)
    case "info":
        log.Printf("Auth info: %s", err.Message)
    case "warn":
        log.Printf("Auth warning: %s", err.Message)
    case "error":
        log.Printf("Auth error: %s", err.Message)
    }
    
    // Send error response
    w.Header().Set("Content-Type", "application/json")
    w.WriteHeader(err.StatusCode)
    
    if err := json.NewEncoder(w).Encode(err); err != nil {
        log.Printf("Failed to encode auth error: %v", err)
    }
}

// Common auth errors
var (
    ErrInvalidToken = AuthError{
        Type:       "invalid_token",
        Message:    "Invalid or expired token",
        StatusCode: http.StatusUnauthorized,
        LogLevel:   "warn",
    }
    
    ErrInsufficientPermissions = AuthError{
        Type:       "insufficient_permissions",
        Message:    "Insufficient permissions for this resource",
        StatusCode: http.StatusForbidden,
        LogLevel:   "info",
    }
    
    ErrSessionExpired = AuthError{
        Type:       "session_expired",
        Message:    "Session has expired, please login again",
        StatusCode: http.StatusUnauthorized,
        LogLevel:   "info",
    }
    
    ErrRateLimitExceeded = AuthError{
        Type:       "rate_limit_exceeded",
        Message:    "Too many authentication attempts",
        StatusCode: http.StatusTooManyRequests,
        LogLevel:   "warn",
    }
)
```

## 🎯 Real-World Example

### Complete Auth Configuration
```tsk
# auth-config.tsk - Complete auth configuration

# Environment configuration
environment: #env("ENVIRONMENT", "development")
debug_mode: #env("DEBUG", "false")

# Authentication methods
auth_methods: #auth("""
    jwt -> JWT token authentication
        secret: #env("JWT_SECRET")
        issuer: "myapp"
        audience: "myapp-users"
        expiration: 24h
        refresh_threshold: 1h
        algorithm: "HS256"
        key_rotation: true
        rotation_interval: 7d
    
    session -> Session-based authentication
        store: "redis"
        url: #env("REDIS_URL")
        secret: #env("SESSION_SECRET")
        max_age: 7d
        secure: true
        http_only: true
        same_site: "strict"
        regenerate_id: true
        regenerate_interval: 1h
    
    api_key -> API key authentication
        header: "X-API-Key"
        keys: #env("API_KEYS").split(",")
        rate_limit: 1000/hour
        cache_ttl: 5m
    
    oauth -> OAuth 2.0 authentication
        providers: ["google", "github", "facebook"]
        client_id: #env("OAUTH_CLIENT_ID")
        client_secret: #env("OAUTH_CLIENT_SECRET")
        redirect_uri: #env("OAUTH_REDIRECT_URI")
        scopes: ["email", "profile"]
""")

# Authorization rules
auth_rules: #auth("""
    admin_only -> Administrator access
        roles: ["admin"]
        resources: ["/api/admin/*", "/api/config/*", "/api/system/*"]
        methods: ["*"]
        conditions: ["user.role == 'admin'"]
    
    user_own_data -> User can only access own data
        rule: "user.id == resource.user_id"
        resources: ["/api/users/{id}/*", "/api/profiles/{id}/*", "/api/settings/{id}/*"]
        methods: ["GET", "PUT", "DELETE"]
        message: "You can only access your own data"
    
    public_read -> Public read access
        roles: ["*"]
        resources: ["/api/public/*", "/api/blog/*", "/api/docs/*"]
        methods: ["GET"]
        conditions: ["true"]
    
    authenticated_only -> Require authentication
        roles: ["user", "admin"]
        resources: ["/api/private/*", "/api/dashboard/*"]
        methods: ["*"]
        conditions: ["user.id != null"]
    
    time_based -> Time-based access control
        rule: "current_time.hour >= 9 && current_time.hour <= 17"
        resources: ["/api/business/*"]
        methods: ["*"]
        message: "Access only during business hours (9 AM - 5 PM)"
""")

# Session configuration
session_config: #auth("""
    session_store -> Redis session store
        url: #env("REDIS_URL")
        prefix: "session:"
        ttl: 7d
        max_connections: 100
        idle_connections: 10
    
    session_security -> Session security settings
        name: "sid"
        secure: true
        http_only: true
        same_site: "strict"
        max_age: 604800
        regenerate_id: true
        regenerate_interval: 1h
        cleanup_interval: 1h
""")

# Performance configuration
performance_config: #auth("""
    caching -> Auth result caching
        enabled: true
        ttl: 5m
        cache_key: "auth:{user_id}:{resource}:{method}"
        max_entries: 10000
    
    rate_limiting -> Auth rate limiting
        login_attempts: 5/minute
        token_requests: 100/minute
        session_creations: 10/minute
        oauth_requests: 20/minute
    
    monitoring -> Auth monitoring
        metrics_enabled: true
        metrics_endpoint: "/metrics"
        alerting_enabled: true
        slow_auth_threshold: 100ms
""")

# Error handling
error_handling: #auth("""
    invalid_token -> Invalid or expired token
        status_code: 401
        message: "Invalid or expired token"
        log_level: warn
        retry_after: 0
    
    insufficient_permissions -> Insufficient permissions
        status_code: 403
        message: "Insufficient permissions for this resource"
        log_level: info
        retry_after: 0
    
    session_expired -> Session expired
        status_code: 401
        message: "Session has expired, please login again"
        log_level: info
        retry_after: 0
    
    rate_limit_exceeded -> Rate limit exceeded
        status_code: 429
        message: "Too many authentication attempts"
        log_level: warn
        retry_after: 60
""")
```

### Go Auth Application Implementation
```go
package main

import (
    "fmt"
    "log"
    "net/http"
    "github.com/tusklang/go-sdk"
    "github.com/gorilla/mux"
)

type AuthConfig struct {
    Environment       string `tsk:"environment"`
    DebugMode         bool   `tsk:"debug_mode"`
    AuthMethods       string `tsk:"auth_methods"`
    AuthRules         string `tsk:"auth_rules"`
    SessionConfig     string `tsk:"session_config"`
    PerformanceConfig string `tsk:"performance_config"`
    ErrorHandling     string `tsk:"error_handling"`
}

func main() {
    // Load auth configuration
    config := tusk.LoadConfig("auth-config.tsk")
    
    var authConfig AuthConfig
    if err := config.Unmarshal(&authConfig); err != nil {
        log.Fatal("Failed to load auth config:", err)
    }
    
    // Create auth system from directives
    auth := tusk.NewAuthSystem(authConfig)
    
    // Create router
    router := mux.NewRouter()
    
    // Apply auth middleware
    tusk.ApplyAuthMiddleware(router, auth)
    
    // Setup routes
    setupRoutes(router, auth)
    
    // Start server
    addr := fmt.Sprintf(":%s", #env("PORT", "8080"))
    log.Printf("Starting auth server on %s in %s mode", addr, authConfig.Environment)
    
    if err := http.ListenAndServe(addr, router); err != nil {
        log.Fatal("Auth server failed:", err)
    }
}

func setupRoutes(router *mux.Router, auth *tusk.AuthSystem) {
    // Public routes
    router.HandleFunc("/health", healthHandler).Methods("GET")
    router.HandleFunc("/metrics", metricsHandler).Methods("GET")
    
    // Auth routes
    authRouter := router.PathPrefix("/auth").Subrouter()
    authRouter.HandleFunc("/login", auth.LoginHandler).Methods("POST")
    authRouter.HandleFunc("/logout", auth.LogoutHandler).Methods("POST")
    authRouter.HandleFunc("/refresh", auth.RefreshHandler).Methods("POST")
    authRouter.HandleFunc("/oauth/{provider}", auth.OAuthHandler).Methods("GET")
    authRouter.HandleFunc("/oauth/{provider}/callback", auth.OAuthCallbackHandler).Methods("GET")
    
    // API routes with auth
    api := router.PathPrefix("/api").Subrouter()
    
    // Public API routes
    public := api.PathPrefix("/public").Subrouter()
    public.HandleFunc("/blog", blogHandler).Methods("GET")
    public.HandleFunc("/docs", docsHandler).Methods("GET")
    
    // Protected API routes
    protected := api.PathPrefix("/v1").Subrouter()
    protected.Use(auth.AuthenticateMiddleware)
    
    protected.HandleFunc("/users", usersHandler).Methods("GET", "POST")
    protected.HandleFunc("/users/{id}", userHandler).Methods("GET", "PUT", "DELETE")
    protected.HandleFunc("/dashboard", dashboardHandler).Methods("GET")
    
    // Admin API routes
    admin := api.PathPrefix("/admin").Subrouter()
    admin.Use(auth.AuthenticateMiddleware)
    admin.Use(auth.AuthorizeMiddleware("admin"))
    
    admin.HandleFunc("/stats", statsHandler).Methods("GET")
    admin.HandleFunc("/config", configHandler).Methods("GET", "PUT")
    admin.HandleFunc("/users", adminUsersHandler).Methods("GET", "POST", "PUT", "DELETE")
}
```

## 🎯 Best Practices

### 1. **Use Secure Defaults**
```tsk
# Secure auth defaults
secure_defaults: #auth("""
    jwt -> JWT with secure defaults
        algorithm: "HS256"
        expiration: 1h
        refresh_threshold: 15m
        secure: true
    
    session -> Session with secure defaults
        secure: true
        http_only: true
        same_site: "strict"
        max_age: 3600
""")
```

### 2. **Implement Proper Error Handling**
```go
// Comprehensive auth error handling
func handleAuthError(w http.ResponseWriter, err error, context string) {
    log.Printf("Auth error in %s: %v", context, err)
    
    // Determine error type and response
    var authError AuthError
    switch {
    case strings.Contains(err.Error(), "token"):
        authError = ErrInvalidToken
    case strings.Contains(err.Error(), "permission"):
        authError = ErrInsufficientPermissions
    case strings.Contains(err.Error(), "session"):
        authError = ErrSessionExpired
    case strings.Contains(err.Error(), "rate limit"):
        authError = ErrRateLimitExceeded
    default:
        authError = AuthError{
            Type:       "auth_error",
            Message:    "Authentication error",
            StatusCode: http.StatusInternalServerError,
            LogLevel:   "error",
        }
    }
    
    HandleAuthError(w, authError)
}
```

### 3. **Use Environment-Specific Configuration**
```tsk
# Different auth settings for different environments
environment_auth: #if(
    #env("ENVIRONMENT") == "production",
    #auth("""
        jwt_expiration: 1h
        session_max_age: 3600
        rate_limit_login: 3/minute
        secure_cookies: true
    """),
    #auth("""
        jwt_expiration: 24h
        session_max_age: 86400
        rate_limit_login: 10/minute
        secure_cookies: false
    """)
)
```

### 4. **Monitor Auth Performance**
```go
// Auth performance monitoring
func monitorAuthPerformance(operation string, startTime time.Time) {
    duration := time.Since(startTime)
    
    // Record metrics
    metrics := map[string]interface{}{
        "operation": operation,
        "duration":  duration.Milliseconds(),
        "timestamp": time.Now(),
    }
    
    if err := recordAuthMetrics(metrics); err != nil {
        log.Printf("Failed to record auth metrics: %v", err)
    }
    
    // Alert on slow auth operations
    if duration > 100*time.Millisecond {
        log.Printf("Slow auth operation: %s took %v", operation, duration)
    }
}
```

## 🎯 Summary

Auth directives in TuskLang provide a powerful, declarative way to define authentication and authorization systems. They enable:

- **Declarative auth configuration** that is easy to understand and maintain
- **Flexible authentication methods** including JWT, sessions, API keys, and OAuth
- **Comprehensive authorization rules** with role-based and resource-based access control
- **Built-in security features** including token rotation, session security, and rate limiting
- **Performance optimization** with caching and connection pooling

The Go SDK seamlessly integrates auth directives with existing Go web frameworks, making them feel like native Go features while providing the power and flexibility of TuskLang's directive system.

**Next**: Explore cache directives, rate limit directives, and other specialized directive types in the following guides. 