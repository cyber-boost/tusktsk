# API Directives - Go

## 🎯 What Are API Directives?

API directives (`#api`) in TuskLang allow you to define REST endpoints, GraphQL schemas, WebSocket handlers, and API-specific configuration directly in your configuration files. They transform static config into executable API logic.

```go
// API directives define your entire API structure
type APIConfig struct {
    RESTEndpoints map[string]string `tsk:"#api_rest"`
    GraphQLSchema string            `tsk:"#api_graphql"`
    WebSocketHandlers []string      `tsk:"#api_websocket"`
    RateLimits    map[string]string `tsk:"#api_rate_limit"`
}
```

## 🚀 Why API Directives Matter

### Traditional API Development
```go
// Old way - scattered across multiple files
func main() {
    router := mux.NewRouter()
    
    // REST endpoints defined in code
    router.HandleFunc("/api/v1/users", handleUsers).Methods("GET")
    router.HandleFunc("/api/v1/users", createUser).Methods("POST")
    
    // GraphQL setup scattered
    schema := loadGraphQLSchema()
    handler := graphql.NewHandler(schema)
    router.HandleFunc("/graphql", handler)
    
    // WebSocket handlers mixed in
    router.HandleFunc("/ws", handleWebSocket)
    
    // Rate limiting hardcoded
    limiter := rate.NewLimiter(rate.Every(time.Second), 100)
    
    http.ListenAndServe(":8080", router)
}
```

### TuskLang API Directives - Declarative & Dynamic
```tsk
# api.tsk - Complete API definition
api_rest: #api("""
    GET /api/v1/users -> handlers.GetUsers
    POST /api/v1/users -> handlers.CreateUser
    GET /api/v1/users/{id} -> handlers.GetUser
    PUT /api/v1/users/{id} -> handlers.UpdateUser
    DELETE /api/v1/users/{id} -> handlers.DeleteUser
""")

api_graphql: #api("""
    type User {
        id: ID!
        name: String!
        email: String!
        posts: [Post!]!
    }
    
    type Query {
        users: [User!]!
        user(id: ID!): User
    }
    
    type Mutation {
        createUser(input: UserInput!): User!
        updateUser(id: ID!, input: UserInput!): User!
    }
""")

api_websocket: #api("""
    /ws/chat -> handlers.ChatHandler
    /ws/notifications -> handlers.NotificationHandler
""")

api_rate_limit: #api("""
    /api/v1/users -> 100/hour
    /api/v1/posts -> 50/hour
    /graphql -> 200/hour
""")
```

## 📋 API Directive Types

### 1. **REST Directives** (`#api_rest`)
- HTTP method and path definitions
- Request/response schemas
- Parameter validation
- Status code handling

### 2. **GraphQL Directives** (`#api_graphql`)
- Schema definitions
- Resolver mappings
- Type definitions
- Query and mutation handlers

### 3. **WebSocket Directives** (`#api_websocket`)
- WebSocket endpoint definitions
- Message type handlers
- Connection management
- Real-time event handling

### 4. **Rate Limit Directives** (`#api_rate_limit`)
- Request rate limiting
- IP-based restrictions
- User-based limits
- Burst handling

## 🔧 Basic API Directive Syntax

### Simple REST Endpoint
```tsk
# Basic REST endpoint
get_users: #api("GET /api/users -> handlers.GetUsers")
```

### REST Endpoint with Parameters
```tsk
# REST endpoint with URL parameters
get_user: #api("GET /api/users/{id} -> handlers.GetUser")
```

### REST Endpoint with Query Parameters
```tsk
# REST endpoint with query string
search_users: #api("GET /api/users?search={query}&limit={limit} -> handlers.SearchUsers")
```

### Multiple REST Endpoints
```tsk
# Multiple REST endpoints
user_api: #api("""
    GET /api/users -> handlers.GetUsers
    POST /api/users -> handlers.CreateUser
    GET /api/users/{id} -> handlers.GetUser
    PUT /api/users/{id} -> handlers.UpdateUser
    DELETE /api/users/{id} -> handlers.DeleteUser
""")
```

## 🎯 Go Integration Patterns

### Struct Tags for API Directives
```go
type APIConfig struct {
    // REST endpoint definitions
    RESTEndpoints string `tsk:"#api_rest"`
    
    // GraphQL schema definition
    GraphQLSchema string `tsk:"#api_graphql"`
    
    // WebSocket handlers
    WebSocketHandlers string `tsk:"#api_websocket"`
    
    // Rate limiting configuration
    RateLimits string `tsk:"#api_rate_limit"`
}
```

### API Application Setup
```go
package main

import (
    "github.com/tusklang/go-sdk"
    "github.com/gorilla/mux"
    "net/http"
)

func main() {
    // Load API configuration
    config := tusk.LoadConfig("api.tsk")
    
    var apiConfig APIConfig
    config.Unmarshal(&apiConfig)
    
    // Create API router from directives
    router := tusk.NewAPIRouter(apiConfig.RESTEndpoints)
    
    // Setup GraphQL from directives
    graphqlHandler := tusk.NewGraphQLHandler(apiConfig.GraphQLSchema)
    router.HandleFunc("/graphql", graphqlHandler)
    
    // Setup WebSocket handlers from directives
    tusk.SetupWebSocketHandlers(router, apiConfig.WebSocketHandlers)
    
    // Apply rate limiting from directives
    tusk.ApplyRateLimiting(router, apiConfig.RateLimits)
    
    // Start API server
    http.ListenAndServe(":8080", router)
}
```

### REST Handler Implementation
```go
package handlers

import (
    "encoding/json"
    "net/http"
    "github.com/gorilla/mux"
)

// REST API handlers
func GetUsers(w http.ResponseWriter, r *http.Request) {
    users := []User{
        {ID: 1, Name: "Alice", Email: "alice@example.com"},
        {ID: 2, Name: "Bob", Email: "bob@example.com"},
    }
    
    w.Header().Set("Content-Type", "application/json")
    json.NewEncoder(w).Encode(users)
}

func CreateUser(w http.ResponseWriter, r *http.Request) {
    var user User
    if err := json.NewDecoder(r.Body).Decode(&user); err != nil {
        http.Error(w, "Invalid JSON", http.StatusBadRequest)
        return
    }
    
    // Validate user data
    if user.Name == "" || user.Email == "" {
        http.Error(w, "Name and email are required", http.StatusBadRequest)
        return
    }
    
    // Create user logic
    user.ID = generateID()
    
    w.Header().Set("Content-Type", "application/json")
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
    
    w.Header().Set("Content-Type", "application/json")
    json.NewEncoder(w).Encode(user)
}
```

## 🔄 GraphQL Integration

### GraphQL Schema Definition
```tsk
# GraphQL schema definition
graphql_schema: #api("""
    type User {
        id: ID!
        name: String!
        email: String!
        createdAt: String!
        posts: [Post!]!
    }
    
    type Post {
        id: ID!
        title: String!
        content: String!
        author: User!
        createdAt: String!
    }
    
    type Query {
        users: [User!]!
        user(id: ID!): User
        posts: [Post!]!
        post(id: ID!): Post
    }
    
    type Mutation {
        createUser(input: UserInput!): User!
        updateUser(id: ID!, input: UserInput!): User!
        deleteUser(id: ID!): Boolean!
        createPost(input: PostInput!): Post!
    }
    
    input UserInput {
        name: String!
        email: String!
    }
    
    input PostInput {
        title: String!
        content: String!
        authorId: ID!
    }
""")
```

### Go GraphQL Resolver Implementation
```go
package resolvers

import (
    "context"
    "fmt"
    "github.com/graphql-go/graphql"
)

// GraphQL resolvers
func GetUsersResolver(p graphql.ResolveParams) (interface{}, error) {
    users := []User{
        {ID: "1", Name: "Alice", Email: "alice@example.com"},
        {ID: "2", Name: "Bob", Email: "bob@example.com"},
    }
    return users, nil
}

func GetUserResolver(p graphql.ResolveParams) (interface{}, error) {
    userID := p.Args["id"].(string)
    
    user := findUserByID(userID)
    if user == nil {
        return nil, fmt.Errorf("user not found")
    }
    
    return user, nil
}

func CreateUserResolver(p graphql.ResolveParams) (interface{}, error) {
    input := p.Args["input"].(map[string]interface{})
    
    user := User{
        ID:    generateID(),
        Name:  input["name"].(string),
        Email: input["email"].(string),
    }
    
    // Save user logic
    saveUser(user)
    
    return user, nil
}

// Register resolvers with TuskLang
func RegisterGraphQLResolvers() {
    tusk.RegisterGraphQLResolver("Query", "users", GetUsersResolver)
    tusk.RegisterGraphQLResolver("Query", "user", GetUserResolver)
    tusk.RegisterGraphQLResolver("Mutation", "createUser", CreateUserResolver)
}
```

## 🔄 WebSocket Integration

### WebSocket Handler Definition
```tsk
# WebSocket handlers definition
websocket_handlers: #api("""
    /ws/chat -> handlers.ChatHandler
    /ws/notifications -> handlers.NotificationHandler
    /ws/realtime -> handlers.RealtimeHandler
""")
```

### Go WebSocket Handler Implementation
```go
package handlers

import (
    "encoding/json"
    "fmt"
    "github.com/gorilla/websocket"
    "net/http"
)

var upgrader = websocket.Upgrader{
    CheckOrigin: func(r *http.Request) bool {
        return true // Allow all origins for development
    },
}

// Chat WebSocket handler
func ChatHandler(w http.ResponseWriter, r *http.Request) {
    conn, err := upgrader.Upgrade(w, r, nil)
    if err != nil {
        return
    }
    defer conn.Close()
    
    for {
        // Read message
        messageType, message, err := conn.ReadMessage()
        if err != nil {
            break
        }
        
        // Process chat message
        var chatMessage ChatMessage
        if err := json.Unmarshal(message, &chatMessage); err != nil {
            continue
        }
        
        // Broadcast to other users
        broadcastChatMessage(chatMessage)
        
        // Send response
        response := ChatResponse{
            Type: "message",
            Data: chatMessage,
        }
        
        responseBytes, _ := json.Marshal(response)
        conn.WriteMessage(messageType, responseBytes)
    }
}

// Notification WebSocket handler
func NotificationHandler(w http.ResponseWriter, r *http.Request) {
    conn, err := upgrader.Upgrade(w, r, nil)
    if err != nil {
        return
    }
    defer conn.Close()
    
    // Subscribe to notifications
    userID := r.URL.Query().Get("user_id")
    subscribeToNotifications(userID, conn)
    
    for {
        // Keep connection alive and send notifications
        select {
        case notification := <-getNotificationChannel(userID):
            notificationBytes, _ := json.Marshal(notification)
            conn.WriteMessage(websocket.TextMessage, notificationBytes)
        }
    }
}
```

## 🛡️ Rate Limiting

### Rate Limit Configuration
```tsk
# Rate limiting configuration
api_rate_limits: #api("""
    /api/users -> 100/hour
    /api/posts -> 50/hour
    /api/admin/* -> 1000/hour
    /graphql -> 200/hour
    /ws/* -> 1000/hour
""")
```

### Go Rate Limiting Implementation
```go
package middleware

import (
    "net/http"
    "sync"
    "time"
    "golang.org/x/time/rate"
)

type RateLimiter struct {
    limiters map[string]*rate.Limiter
    mu       sync.RWMutex
}

func NewRateLimiter() *RateLimiter {
    return &RateLimiter{
        limiters: make(map[string]*rate.Limiter),
    }
}

func (rl *RateLimiter) GetLimiter(key string, requests int, window time.Duration) *rate.Limiter {
    rl.mu.Lock()
    defer rl.mu.Unlock()
    
    if limiter, exists := rl.limiters[key]; exists {
        return limiter
    }
    
    limiter := rate.NewLimiter(rate.Every(window/time.Duration(requests)), requests)
    rl.limiters[key] = limiter
    return limiter
}

func RateLimitMiddleware(rl *RateLimiter) func(http.Handler) http.Handler {
    return func(next http.Handler) http.Handler {
        return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
            // Get client IP or user ID
            key := r.RemoteAddr
            
            // Get rate limit for this endpoint
            limit := getRateLimitForPath(r.URL.Path)
            if limit == nil {
                next.ServeHTTP(w, r)
                return
            }
            
            limiter := rl.GetLimiter(key, limit.Requests, limit.Window)
            
            if !limiter.Allow() {
                http.Error(w, "Rate limit exceeded", http.StatusTooManyRequests)
                return
            }
            
            next.ServeHTTP(w, r)
        })
    }
}
```

## ⚡ API Versioning

### Versioned API Configuration
```tsk
# API versioning configuration
api_v1: #api("""
    # Version 1 API endpoints
    GET /api/v1/users -> handlers.v1.GetUsers
    POST /api/v1/users -> handlers.v1.CreateUser
    GET /api/v1/users/{id} -> handlers.v1.GetUser
""")

api_v2: #api("""
    # Version 2 API endpoints
    GET /api/v2/users -> handlers.v2.GetUsers
    POST /api/v2/users -> handlers.v2.CreateUser
    GET /api/v2/users/{id} -> handlers.v2.GetUser
    PATCH /api/v2/users/{id} -> handlers.v2.PartialUpdateUser
""")
```

### Go Versioned Handler Implementation
```go
package handlers

// Version 1 handlers
type V1Handlers struct{}

func (v1 *V1Handlers) GetUsers(w http.ResponseWriter, r *http.Request) {
    // V1 implementation
    users := []UserV1{
        {ID: 1, Name: "Alice", Email: "alice@example.com"},
    }
    
    w.Header().Set("Content-Type", "application/json")
    json.NewEncoder(w).Encode(users)
}

// Version 2 handlers
type V2Handlers struct{}

func (v2 *V2Handlers) GetUsers(w http.ResponseWriter, r *http.Request) {
    // V2 implementation with additional fields
    users := []UserV2{
        {ID: 1, Name: "Alice", Email: "alice@example.com", CreatedAt: time.Now()},
    }
    
    w.Header().Set("Content-Type", "application/json")
    json.NewEncoder(w).Encode(users)
}

func (v2 *V2Handlers) PartialUpdateUser(w http.ResponseWriter, r *http.Request) {
    // V2-specific PATCH endpoint
    vars := mux.Vars(r)
    userID := vars["id"]
    
    var updates map[string]interface{}
    json.NewDecoder(r.Body).Decode(&updates)
    
    // Partial update logic
    user := partialUpdateUser(userID, updates)
    
    w.Header().Set("Content-Type", "application/json")
    json.NewEncoder(w).Encode(user)
}
```

## 🔧 Error Handling

### API Error Response Configuration
```tsk
# API error handling configuration
api_errors: #api("""
    400 -> handlers.BadRequest
    401 -> handlers.Unauthorized
    403 -> handlers.Forbidden
    404 -> handlers.NotFound
    422 -> handlers.ValidationError
    429 -> handlers.RateLimitExceeded
    500 -> handlers.InternalError
""")
```

### Go Error Handler Implementation
```go
package handlers

import (
    "encoding/json"
    "net/http"
)

type APIError struct {
    Error   string `json:"error"`
    Code    string `json:"code"`
    Message string `json:"message"`
    Details map[string]interface{} `json:"details,omitempty"`
}

func BadRequest(w http.ResponseWriter, r *http.Request) {
    error := APIError{
        Error:   "bad_request",
        Code:    "400",
        Message: "Invalid request parameters",
    }
    
    w.Header().Set("Content-Type", "application/json")
    w.WriteHeader(http.StatusBadRequest)
    json.NewEncoder(w).Encode(error)
}

func ValidationError(w http.ResponseWriter, r *http.Request, errors map[string]string) {
    error := APIError{
        Error:   "validation_error",
        Code:    "422",
        Message: "Validation failed",
        Details: make(map[string]interface{}),
    }
    
    for field, message := range errors {
        error.Details[field] = message
    }
    
    w.Header().Set("Content-Type", "application/json")
    w.WriteHeader(http.StatusUnprocessableEntity)
    json.NewEncoder(w).Encode(error)
}
```

## 🎯 Real-World Example

### Complete API Configuration
```tsk
# api-config.tsk - Complete API configuration

# Environment configuration
environment: #env("ENVIRONMENT", "development")
api_version: #env("API_VERSION", "v1")

# Database configuration
database_url: #env("DATABASE_URL", "sqlite://api.db")

# REST API endpoints
rest_endpoints: #api("""
    # User management
    GET /api/v1/users -> handlers.GetUsers
    POST /api/v1/users -> handlers.CreateUser
    GET /api/v1/users/{id} -> handlers.GetUser
    PUT /api/v1/users/{id} -> handlers.UpdateUser
    DELETE /api/v1/users/{id} -> handlers.DeleteUser
    
    # Post management
    GET /api/v1/posts -> handlers.GetPosts
    POST /api/v1/posts -> handlers.CreatePost
    GET /api/v1/posts/{id} -> handlers.GetPost
    PUT /api/v1/posts/{id} -> handlers.UpdatePost
    DELETE /api/v1/posts/{id} -> handlers.DeletePost
    
    # Authentication
    POST /api/v1/auth/login -> handlers.Login
    POST /api/v1/auth/logout -> handlers.Logout
    POST /api/v1/auth/refresh -> handlers.RefreshToken
""")

# GraphQL schema
graphql_schema: #api("""
    type User {
        id: ID!
        name: String!
        email: String!
        createdAt: String!
        posts: [Post!]!
    }
    
    type Post {
        id: ID!
        title: String!
        content: String!
        author: User!
        createdAt: String!
    }
    
    type Query {
        users: [User!]!
        user(id: ID!): User
        posts: [Post!]!
        post(id: ID!): Post
    }
    
    type Mutation {
        createUser(input: UserInput!): User!
        updateUser(id: ID!, input: UserInput!): User!
        createPost(input: PostInput!): Post!
    }
    
    input UserInput {
        name: String!
        email: String!
    }
    
    input PostInput {
        title: String!
        content: String!
        authorId: ID!
    }
""")

# WebSocket handlers
websocket_handlers: #api("""
    /ws/chat -> handlers.ChatHandler
    /ws/notifications -> handlers.NotificationHandler
    /ws/realtime -> handlers.RealtimeHandler
""")

# Rate limiting
rate_limits: #api("""
    /api/v1/users -> 100/hour
    /api/v1/posts -> 50/hour
    /api/v1/auth/login -> 5/minute
    /graphql -> 200/hour
    /ws/* -> 1000/hour
""")

# Error handlers
error_handlers: #api("""
    400 -> handlers.BadRequest
    401 -> handlers.Unauthorized
    403 -> handlers.Forbidden
    404 -> handlers.NotFound
    422 -> handlers.ValidationError
    429 -> handlers.RateLimitExceeded
    500 -> handlers.InternalError
""")
```

### Go API Application Implementation
```go
package main

import (
    "fmt"
    "log"
    "net/http"
    "github.com/tusklang/go-sdk"
)

type APIConfig struct {
    Environment      string `tsk:"environment"`
    APIVersion       string `tsk:"api_version"`
    DatabaseURL      string `tsk:"database_url"`
    RESTEndpoints    string `tsk:"rest_endpoints"`
    GraphQLSchema    string `tsk:"graphql_schema"`
    WebSocketHandlers string `tsk:"websocket_handlers"`
    RateLimits       string `tsk:"rate_limits"`
    ErrorHandlers    string `tsk:"error_handlers"`
}

func main() {
    // Load API configuration
    config := tusk.LoadConfig("api-config.tsk")
    
    var apiConfig APIConfig
    if err := config.Unmarshal(&apiConfig); err != nil {
        log.Fatal("Failed to load API config:", err)
    }
    
    // Create API application from directives
    api := tusk.NewAPI(apiConfig)
    
    // Setup database connection
    db := tusk.ConnectDatabase(apiConfig.DatabaseURL)
    
    // Register handlers with database context
    handlers.SetDatabase(db)
    
    // Register GraphQL resolvers
    resolvers.RegisterGraphQLResolvers()
    
    // Start the API server
    addr := fmt.Sprintf(":%s", #env("PORT", "8080"))
    log.Printf("Starting API server on %s", addr)
    
    if err := api.ListenAndServe(addr); err != nil {
        log.Fatal("API server failed:", err)
    }
}
```

## 🎯 Best Practices

### 1. **Use Consistent API Patterns**
```tsk
# Consistent REST API patterns
rest_api: #api("""
    # Collection endpoints
    GET /api/v1/{resource} -> handlers.List{Resource}
    POST /api/v1/{resource} -> handlers.Create{Resource}
    
    # Individual endpoints
    GET /api/v1/{resource}/{id} -> handlers.Get{Resource}
    PUT /api/v1/{resource}/{id} -> handlers.Update{Resource}
    DELETE /api/v1/{resource}/{id} -> handlers.Delete{Resource}
""")
```

### 2. **Implement Proper Validation**
```go
// Input validation for API endpoints
func validateUserInput(user User) map[string]string {
    errors := make(map[string]string)
    
    if user.Name == "" {
        errors["name"] = "Name is required"
    }
    
    if user.Email == "" {
        errors["email"] = "Email is required"
    } else if !isValidEmail(user.Email) {
        errors["email"] = "Invalid email format"
    }
    
    return errors
}
```

### 3. **Use Environment-Specific Configuration**
```tsk
# Different rate limits for different environments
rate_limits: #if(
    #env("ENVIRONMENT") == "production",
    #api("""
        /api/v1/users -> 100/hour
        /api/v1/posts -> 50/hour
    """),
    #api("""
        /api/v1/users -> 1000/hour
        /api/v1/posts -> 500/hour
    """)
)
```

### 4. **Implement Comprehensive Error Handling**
```go
// Structured error responses
func handleAPIError(w http.ResponseWriter, err error, statusCode int) {
    apiError := APIError{
        Error:   http.StatusText(statusCode),
        Code:    fmt.Sprintf("%d", statusCode),
        Message: err.Error(),
    }
    
    w.Header().Set("Content-Type", "application/json")
    w.WriteHeader(statusCode)
    json.NewEncoder(w).Encode(apiError)
}
```

## 🎯 Summary

API directives in TuskLang provide a powerful, declarative way to define complete API structures. They enable:

- **Declarative API definitions** that are easy to understand and maintain
- **Multiple API types** including REST, GraphQL, and WebSocket
- **Built-in rate limiting** and security features
- **Environment-specific configuration** that adapts to deployment needs
- **Comprehensive error handling** with structured responses

The Go SDK seamlessly integrates API directives with existing Go web frameworks, making them feel like native Go features while providing the power and flexibility of TuskLang's directive system.

**Next**: Explore CLI directives, cron directives, and other specialized directive types in the following guides. 