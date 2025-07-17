# Auth Hash Directives in TuskLang for Go

## Overview

Auth hash directives in TuskLang provide powerful authentication and authorization configuration capabilities directly in your configuration files. These directives enable you to define sophisticated authentication methods, authorization rules, and security policies with Go integration for secure applications.

## Basic Auth Directive Syntax

```go
// TuskLang auth directive
#auth {
    providers: {
        jwt: {
            enabled: true
            secret: "@env('JWT_SECRET')"
            algorithm: "HS256"
            expires_in: "24h"
            refresh_expires_in: "7d"
        }
        
        oauth2: {
            enabled: true
            providers: {
                google: {
                    client_id: "@env('GOOGLE_CLIENT_ID')"
                    client_secret: "@env('GOOGLE_CLIENT_SECRET')"
                    redirect_url: "https://myapp.com/auth/google/callback"
                    scopes: ["email", "profile"]
                }
                
                github: {
                    client_id: "@env('GITHUB_CLIENT_ID')"
                    client_secret: "@env('GITHUB_CLIENT_SECRET')"
                    redirect_url: "https://myapp.com/auth/github/callback"
                    scopes: ["user:email"]
                }
            }
        }
        
        session: {
            enabled: true
            store: "redis"
            secret: "@env('SESSION_SECRET')"
            max_age: 3600
            secure: true
            http_only: true
        }
    }
    
    policies: {
        admin_access: {
            roles: ["admin"]
            permissions: ["read", "write", "delete"]
        }
        
        user_access: {
            roles: ["user"]
            permissions: ["read", "write"]
        }
        
        guest_access: {
            roles: ["guest"]
            permissions: ["read"]
        }
    }
    
    routes: {
        "/api/admin": {
            required: true
            roles: ["admin"]
            permissions: ["read", "write", "delete"]
        }
        
        "/api/users": {
            required: true
            roles: ["user", "admin"]
            permissions: ["read", "write"]
        }
        
        "/api/public": {
            required: false
            roles: ["guest", "user", "admin"]
            permissions: ["read"]
        }
    }
}
```

## Go Integration

```go
package main

import (
    "context"
    "crypto/rand"
    "encoding/base64"
    "fmt"
    "log"
    "net/http"
    "strings"
    "time"
    
    "github.com/golang-jwt/jwt/v4"
    "github.com/gorilla/sessions"
    "github.com/tusklang/go-sdk"
)

type AuthDirective struct {
    Providers map[string]Provider `tsk:"providers"`
    Policies  map[string]Policy   `tsk:"policies"`
    Routes    map[string]Route    `tsk:"routes"`
}

type Provider struct {
    Enabled bool                   `tsk:"enabled"`
    Config  map[string]interface{} `tsk:",inline"`
}

type Policy struct {
    Roles       []string `tsk:"roles"`
    Permissions []string `tsk:"permissions"`
}

type Route struct {
    Required    bool     `tsk:"required"`
    Roles       []string `tsk:"roles"`
    Permissions []string `tsk:"permissions"`
}

type User struct {
    ID          string   `json:"id"`
    Email       string   `json:"email"`
    Roles       []string `json:"roles"`
    Permissions []string `json:"permissions"`
}

type AuthManager struct {
    directive AuthDirective
    jwtSecret string
    sessionStore *sessions.CookieStore
    oauthProviders map[string]OAuthProvider
}

type OAuthProvider struct {
    ClientID     string   `json:"client_id"`
    ClientSecret string   `json:"client_secret"`
    RedirectURL  string   `json:"redirect_url"`
    Scopes       []string `json:"scopes"`
}

func main() {
    // Load auth configuration
    config, err := tusk.LoadFile("auth-config.tsk")
    if err != nil {
        log.Fatalf("Error loading auth config: %v", err)
    }
    
    var authDirective AuthDirective
    if err := config.Get("#auth", &authDirective); err != nil {
        log.Fatalf("Error parsing auth directive: %v", err)
    }
    
    // Initialize auth manager
    authManager := NewAuthManager(authDirective)
    
    // Setup HTTP server with auth middleware
    mux := http.NewServeMux()
    
    // Auth routes
    mux.HandleFunc("/auth/login", authManager.handleLogin)
    mux.HandleFunc("/auth/logout", authManager.handleLogout)
    mux.HandleFunc("/auth/google", authManager.handleGoogleAuth)
    mux.HandleFunc("/auth/github", authManager.handleGitHubAuth)
    
    // Protected routes
    mux.HandleFunc("/api/admin", authManager.withAuth(authManager.handleAdmin))
    mux.HandleFunc("/api/users", authManager.withAuth(authManager.handleUsers))
    mux.HandleFunc("/api/public", authManager.handlePublic)
    
    log.Println("Server starting on :8080")
    log.Fatal(http.ListenAndServe(":8080", mux))
}

func NewAuthManager(directive AuthDirective) *AuthManager {
    manager := &AuthManager{
        directive: directive,
        oauthProviders: make(map[string]OAuthProvider),
    }
    
    // Initialize JWT secret
    if jwt, exists := directive.Providers["jwt"]; exists && jwt.Enabled {
        manager.jwtSecret = jwt.Config["secret"].(string)
    }
    
    // Initialize session store
    if session, exists := directive.Providers["session"]; exists && session.Enabled {
        secret := session.Config["secret"].(string)
        manager.sessionStore = sessions.NewCookieStore([]byte(secret))
    }
    
    // Initialize OAuth providers
    if oauth2, exists := directive.Providers["oauth2"]; exists && oauth2.Enabled {
        providers := oauth2.Config["providers"].(map[string]interface{})
        for name, config := range providers {
            providerConfig := config.(map[string]interface{})
            manager.oauthProviders[name] = OAuthProvider{
                ClientID:     providerConfig["client_id"].(string),
                ClientSecret: providerConfig["client_secret"].(string),
                RedirectURL:  providerConfig["redirect_url"].(string),
                Scopes:       providerConfig["scopes"].([]string),
            }
        }
    }
    
    return manager
}

// Authentication middleware
func (a *AuthManager) withAuth(handler http.HandlerFunc) http.HandlerFunc {
    return func(w http.ResponseWriter, r *http.Request) {
        user, err := a.authenticateRequest(r)
        if err != nil {
            http.Error(w, "Unauthorized", http.StatusUnauthorized)
            return
        }
        
        // Check route requirements
        route := a.getRouteConfig(r.URL.Path)
        if route.Required && user == nil {
            http.Error(w, "Authentication required", http.StatusUnauthorized)
            return
        }
        
        if user != nil {
            // Check roles and permissions
            if !a.authorizeUser(user, route) {
                http.Error(w, "Insufficient permissions", http.StatusForbidden)
                return
            }
            
            // Add user to context
            ctx := context.WithValue(r.Context(), "user", user)
            r = r.WithContext(ctx)
        }
        
        handler(w, r)
    }
}

func (a *AuthManager) authenticateRequest(r *http.Request) (*User, error) {
    // Try JWT authentication
    if user, err := a.authenticateJWT(r); err == nil {
        return user, nil
    }
    
    // Try session authentication
    if user, err := a.authenticateSession(r); err == nil {
        return user, nil
    }
    
    // Try OAuth authentication
    if user, err := a.authenticateOAuth(r); err == nil {
        return user, nil
    }
    
    return nil, fmt.Errorf("no valid authentication found")
}

func (a *AuthManager) authenticateJWT(r *http.Request) (*User, error) {
    authHeader := r.Header.Get("Authorization")
    if authHeader == "" {
        return nil, fmt.Errorf("no authorization header")
    }
    
    if !strings.HasPrefix(authHeader, "Bearer ") {
        return nil, fmt.Errorf("invalid authorization header format")
    }
    
    tokenString := strings.TrimPrefix(authHeader, "Bearer ")
    
    token, err := jwt.Parse(tokenString, func(token *jwt.Token) (interface{}, error) {
        if _, ok := token.Method.(*jwt.SigningMethodHMAC); !ok {
            return nil, fmt.Errorf("unexpected signing method: %v", token.Header["alg"])
        }
        return []byte(a.jwtSecret), nil
    })
    
    if err != nil {
        return nil, err
    }
    
    if claims, ok := token.Claims.(jwt.MapClaims); ok && token.Valid {
        user := &User{
            ID:    claims["user_id"].(string),
            Email: claims["email"].(string),
        }
        
        if roles, ok := claims["roles"].([]interface{}); ok {
            for _, role := range roles {
                user.Roles = append(user.Roles, role.(string))
            }
        }
        
        if permissions, ok := claims["permissions"].([]interface{}); ok {
            for _, perm := range permissions {
                user.Permissions = append(user.Permissions, perm.(string))
            }
        }
        
        return user, nil
    }
    
    return nil, fmt.Errorf("invalid token")
}

func (a *AuthManager) authenticateSession(r *http.Request) (*User, error) {
    if a.sessionStore == nil {
        return nil, fmt.Errorf("session store not configured")
    }
    
    session, err := a.sessionStore.Get(r, "user-session")
    if err != nil {
        return nil, err
    }
    
    userID, ok := session.Values["user_id"].(string)
    if !ok {
        return nil, fmt.Errorf("no user_id in session")
    }
    
    // In a real application, you would fetch user data from database
    user := &User{
        ID:    userID,
        Email: session.Values["email"].(string),
        Roles: []string{"user"},
    }
    
    return user, nil
}

func (a *AuthManager) authenticateOAuth(r *http.Request) (*User, error) {
    // OAuth authentication would be implemented here
    // This is a simplified example
    return nil, fmt.Errorf("oauth authentication not implemented")
}

func (a *AuthManager) authorizeUser(user *User, route Route) bool {
    // Check if user has required roles
    if len(route.Roles) > 0 {
        hasRole := false
        for _, requiredRole := range route.Roles {
            for _, userRole := range user.Roles {
                if requiredRole == userRole {
                    hasRole = true
                    break
                }
            }
            if hasRole {
                break
            }
        }
        if !hasRole {
            return false
        }
    }
    
    // Check if user has required permissions
    if len(route.Permissions) > 0 {
        for _, requiredPerm := range route.Permissions {
            hasPerm := false
            for _, userPerm := range user.Permissions {
                if requiredPerm == userPerm {
                    hasPerm = true
                    break
                }
            }
            if !hasPerm {
                return false
            }
        }
    }
    
    return true
}

func (a *AuthManager) getRouteConfig(path string) Route {
    // Find matching route configuration
    for routePath, config := range a.directive.Routes {
        if strings.HasPrefix(path, routePath) {
            return config
        }
    }
    
    // Default configuration
    return Route{
        Required: false,
        Roles:    []string{"guest"},
        Permissions: []string{"read"},
    }
}

// Handler functions
func (a *AuthManager) handleLogin(w http.ResponseWriter, r *http.Request) {
    if r.Method != "POST" {
        http.Error(w, "Method not allowed", http.StatusMethodNotAllowed)
        return
    }
    
    // Parse login credentials
    email := r.FormValue("email")
    password := r.FormValue("password")
    
    // Validate credentials (simplified)
    if email == "admin@example.com" && password == "password" {
        user := &User{
            ID:    "1",
            Email: email,
            Roles: []string{"admin"},
            Permissions: []string{"read", "write", "delete"},
        }
        
        // Generate JWT token
        token := a.generateJWT(user)
        
        w.Header().Set("Content-Type", "application/json")
        w.Write([]byte(fmt.Sprintf(`{"token": "%s"}`, token)))
    } else {
        http.Error(w, "Invalid credentials", http.StatusUnauthorized)
    }
}

func (a *AuthManager) handleLogout(w http.ResponseWriter, r *http.Request) {
    // Clear session
    if a.sessionStore != nil {
        session, _ := a.sessionStore.Get(r, "user-session")
        session.Options.MaxAge = -1
        session.Save(r, w)
    }
    
    w.Write([]byte(`{"message": "Logged out successfully"}`))
}

func (a *AuthManager) handleGoogleAuth(w http.ResponseWriter, r *http.Request) {
    provider, exists := a.oauthProviders["google"]
    if !exists {
        http.Error(w, "Google OAuth not configured", http.StatusInternalServerError)
        return
    }
    
    // Redirect to Google OAuth
    authURL := fmt.Sprintf(
        "https://accounts.google.com/o/oauth2/auth?client_id=%s&redirect_uri=%s&scope=%s&response_type=code",
        provider.ClientID,
        provider.RedirectURL,
        strings.Join(provider.Scopes, " "),
    )
    
    http.Redirect(w, r, authURL, http.StatusTemporaryRedirect)
}

func (a *AuthManager) handleGitHubAuth(w http.ResponseWriter, r *http.Request) {
    provider, exists := a.oauthProviders["github"]
    if !exists {
        http.Error(w, "GitHub OAuth not configured", http.StatusInternalServerError)
        return
    }
    
    // Redirect to GitHub OAuth
    authURL := fmt.Sprintf(
        "https://github.com/login/oauth/authorize?client_id=%s&redirect_uri=%s&scope=%s",
        provider.ClientID,
        provider.RedirectURL,
        strings.Join(provider.Scopes, " "),
    )
    
    http.Redirect(w, r, authURL, http.StatusTemporaryRedirect)
}

func (a *AuthManager) handleAdmin(w http.ResponseWriter, r *http.Request) {
    user := r.Context().Value("user").(*User)
    w.Header().Set("Content-Type", "application/json")
    w.Write([]byte(fmt.Sprintf(`{"message": "Welcome admin %s", "data": "admin-only-data"}`, user.Email)))
}

func (a *AuthManager) handleUsers(w http.ResponseWriter, r *http.Request) {
    user := r.Context().Value("user").(*User)
    w.Header().Set("Content-Type", "application/json")
    w.Write([]byte(fmt.Sprintf(`{"message": "Welcome %s", "users": []}`, user.Email)))
}

func (a *AuthManager) handlePublic(w http.ResponseWriter, r *http.Request) {
    w.Header().Set("Content-Type", "application/json")
    w.Write([]byte(`{"message": "Public data", "data": "public-data"}`))
}

func (a *AuthManager) generateJWT(user *User) string {
    claims := jwt.MapClaims{
        "user_id": user.ID,
        "email":   user.Email,
        "roles":   user.Roles,
        "permissions": user.Permissions,
        "exp":     time.Now().Add(time.Hour * 24).Unix(),
        "iat":     time.Now().Unix(),
    }
    
    token := jwt.NewWithClaims(jwt.SigningMethodHS256, claims)
    tokenString, _ := token.SignedString([]byte(a.jwtSecret))
    
    return tokenString
}

func (a *AuthManager) generateRandomSecret() string {
    b := make([]byte, 32)
    rand.Read(b)
    return base64.StdEncoding.EncodeToString(b)
}
```

## Advanced Auth Features

### Role-Based Access Control (RBAC)

```go
// TuskLang configuration with RBAC
#auth {
    rbac: {
        roles: {
            admin: {
                permissions: ["read", "write", "delete", "manage_users"]
                inherits: []
            }
            
            manager: {
                permissions: ["read", "write", "manage_team"]
                inherits: ["user"]
            }
            
            user: {
                permissions: ["read", "write"]
                inherits: ["guest"]
            }
            
            guest: {
                permissions: ["read"]
                inherits: []
            }
        }
        
        resources: {
            users: {
                permissions: ["read", "write", "delete"]
                roles: ["admin"]
            }
            
            posts: {
                permissions: ["read", "write", "delete"]
                roles: ["admin", "manager", "user"]
            }
            
            comments: {
                permissions: ["read", "write"]
                roles: ["admin", "manager", "user", "guest"]
            }
        }
    }
}
```

### Multi-Factor Authentication (MFA)

```go
// TuskLang configuration with MFA
#auth {
    mfa: {
        enabled: true
        methods: {
            totp: {
                enabled: true
                issuer: "MyApp"
                algorithm: "SHA1"
                digits: 6
                period: 30
            }
            
            sms: {
                enabled: true
                provider: "twilio"
                template: "Your verification code is: {code}"
            }
            
            email: {
                enabled: true
                template: "verification-email.html"
                subject: "Your verification code"
            }
        }
        
        policies: {
            admin_required: {
                roles: ["admin"]
                required: true
            }
            
            optional: {
                roles: ["user"]
                required: false
            }
        }
    }
}
```

## Performance Considerations

- **Token Caching**: Cache validated tokens to reduce validation overhead
- **Session Storage**: Use efficient session storage (Redis, database)
- **Rate Limiting**: Implement rate limiting for auth endpoints
- **Async Processing**: Use goroutines for non-blocking auth operations
- **Connection Pooling**: Use connection pooling for database auth

## Security Notes

- **Secure Secrets**: Use secure secret management for sensitive data
- **Token Expiration**: Implement proper token expiration and refresh
- **HTTPS Only**: Use HTTPS for all authentication endpoints
- **Input Validation**: Validate all authentication inputs
- **Audit Logging**: Log all authentication events for security auditing

## Best Practices

1. **Principle of Least Privilege**: Grant minimal required permissions
2. **Secure Defaults**: Use secure default configurations
3. **Regular Rotation**: Rotate secrets and tokens regularly
4. **Monitoring**: Monitor authentication events and failures
5. **Testing**: Test authentication flows thoroughly
6. **Documentation**: Document authentication policies and procedures

## Integration Examples

### With Gin Framework

```go
import (
    "github.com/gin-gonic/gin"
    "github.com/tusklang/go-sdk"
)

func setupGinAuth(config tusk.Config) *gin.Engine {
    var authDirective AuthDirective
    config.Get("#auth", &authDirective)
    
    router := gin.Default()
    
    // Apply auth middleware
    router.Use(authMiddleware(authDirective))
    
    return router
}

func authMiddleware(directive AuthDirective) gin.HandlerFunc {
    return func(c *gin.Context) {
        // Implement authentication logic
        user, err := authenticateRequest(c.Request)
        if err != nil {
            c.AbortWithStatus(http.StatusUnauthorized)
            return
        }
        
        c.Set("user", user)
        c.Next()
    }
}
```

### With Echo Framework

```go
import (
    "github.com/labstack/echo/v4"
    "github.com/tusklang/go-sdk"
)

func setupEchoAuth(config tusk.Config) *echo.Echo {
    var authDirective AuthDirective
    config.Get("#auth", &authDirective)
    
    e := echo.New()
    
    // Apply auth middleware
    e.Use(authMiddleware(authDirective))
    
    return e
}
```

This comprehensive auth hash directives documentation provides Go developers with everything they need to build sophisticated authentication and authorization systems using TuskLang's powerful directive system. 