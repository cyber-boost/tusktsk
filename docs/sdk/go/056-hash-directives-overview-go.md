# Hash Directives Overview - Go

## 🎯 What Are Hash Directives?

Hash directives are TuskLang's powerful system for embedding executable logic directly in configuration files. They use the `#` symbol to trigger special behaviors that go beyond simple key-value pairs.

```go
// Hash directives transform static config into dynamic, executable logic
type Config struct {
    // Traditional key-value
    DatabaseURL string `tsk:"database_url"`
    
    // Hash directive - executable logic
    CacheTTL string `tsk:"#cache_ttl"`
    AuthToken string `tsk:"#auth_token"`
}
```

## 🚀 Why Hash Directives Matter

### Traditional Configuration Limitations
```go
// Old way - static, inflexible
type StaticConfig struct {
    CacheTTL    int    `json:"cache_ttl"`     // Always 300
    DatabaseURL string `json:"database_url"`  // Always the same
    AuthToken   string `json:"auth_token"`    // Hardcoded
}
```

### TuskLang Hash Directives - Dynamic & Intelligent
```go
// New way - dynamic, intelligent, executable
type DynamicConfig struct {
    CacheTTL    string `tsk:"#cache_ttl"`     // Adapts to load
    DatabaseURL string `tsk:"#database_url"`  // Environment-aware
    AuthToken   string `tsk:"#auth_token"`    // Auto-refreshing
}
```

## 📋 Hash Directive Categories

### 1. **Web Directives** (`#web`)
- Route definitions
- Middleware configuration
- Static file serving
- API endpoints

### 2. **API Directives** (`#api`)
- REST endpoint definitions
- GraphQL schemas
- WebSocket handlers
- Rate limiting

### 3. **CLI Directives** (`#cli`)
- Command definitions
- Argument parsing
- Help text generation
- Subcommand structure

### 4. **Cron Directives** (`#cron`)
- Scheduled task definitions
- Job execution timing
- Error handling
- Logging configuration

### 5. **Middleware Directives** (`#middleware`)
- Request processing
- Authentication
- Logging
- CORS handling

### 6. **Auth Directives** (`#auth`)
- Authentication methods
- Authorization rules
- Session management
- Token handling

### 7. **Cache Directives** (`#cache`)
- Caching strategies
- TTL configuration
- Invalidation rules
- Storage backends

### 8. **Rate Limit Directives** (`#rate_limit`)
- Request limiting
- Throttling rules
- IP-based restrictions
- User-based limits

## 🔧 Basic Hash Directive Syntax

### Simple Directive
```tsk
# Simple directive with no parameters
api_key: #env("API_KEY")
```

### Directive with Parameters
```tsk
# Directive with multiple parameters
cache_ttl: #cache("5m", "user_data")
```

### Directive with Complex Logic
```tsk
# Directive with embedded logic
process_data: #fujsen("""
    function process(input) {
        return input.map(item => item * 2);
    }
""")
```

## 🎯 Go Integration Patterns

### Struct Tags for Hash Directives
```go
type AppConfig struct {
    // Simple directive
    APIKey string `tsk:"#env('API_KEY')"`
    
    // Directive with parameters
    CacheTTL string `tsk:"#cache('5m', 'user_data')"`
    
    // Complex directive
    ProcessFunc string `tsk:"#fujsen('function(x) { return x * 2; }')"`
}
```

### Runtime Directive Processing
```go
package main

import (
    "fmt"
    "github.com/tusklang/go-sdk"
)

func main() {
    config := tusk.LoadConfig("app.tsk")
    
    // Directives are processed automatically
    apiKey := config.GetString("api_key")     // Resolved from #env
    cacheTTL := config.GetString("cache_ttl") // Resolved from #cache
    
    fmt.Printf("API Key: %s\n", apiKey)
    fmt.Printf("Cache TTL: %s\n", cacheTTL)
}
```

### Custom Directive Handlers
```go
package main

import (
    "github.com/tusklang/go-sdk"
)

// Custom directive handler
func handleCustomDirective(params []string) (interface{}, error) {
    if len(params) < 1 {
        return nil, fmt.Errorf("custom directive requires at least one parameter")
    }
    
    // Custom logic here
    return fmt.Sprintf("custom_%s", params[0]), nil
}

func main() {
    // Register custom directive
    tusk.RegisterDirective("custom", handleCustomDirective)
    
    config := tusk.LoadConfig("app.tsk")
    // Now #custom directives will work
}
```

## 🔄 Directive Processing Flow

### 1. **Parse Phase**
```go
// TuskLang parses directives during config loading
config := tusk.LoadConfig("app.tsk")
// All #directives are identified and queued for processing
```

### 2. **Resolution Phase**
```go
// Directives are resolved in dependency order
for _, directive := range config.Directives {
    result := directive.Resolve()
    config.SetValue(directive.Key, result)
}
```

### 3. **Execution Phase**
```go
// Final config is ready for use
apiKey := config.GetString("api_key") // Fully resolved value
```

## 🛡️ Security Considerations

### Directive Validation
```go
// Validate directive parameters
func validateDirective(directive string, params []string) error {
    switch directive {
    case "#env":
        if len(params) < 1 {
            return fmt.Errorf("#env requires at least one parameter")
        }
    case "#http":
        if len(params) < 2 {
            return fmt.Errorf("#http requires method and URL")
        }
    }
    return nil
}
```

### Sandboxed Execution
```go
// FUJSEN directives run in sandboxed environment
fujsenConfig := tusk.FujsenConfig{
    Timeout:     time.Second * 5,
    MemoryLimit: 10 * 1024 * 1024, // 10MB
    AllowNetwork: false,
}
```

## ⚡ Performance Optimization

### Directive Caching
```go
// Cache directive results to avoid repeated processing
type DirectiveCache struct {
    cache map[string]interface{}
    ttl   time.Duration
}

func (dc *DirectiveCache) Get(key string) (interface{}, bool) {
    if result, exists := dc.cache[key]; exists {
        return result, true
    }
    return nil, false
}
```

### Lazy Loading
```go
// Only process directives when values are accessed
type LazyConfig struct {
    directives map[string]*Directive
    resolved   map[string]interface{}
}

func (lc *LazyConfig) GetString(key string) string {
    if value, exists := lc.resolved[key]; exists {
        return value.(string)
    }
    
    // Process directive on first access
    if directive, exists := lc.directives[key]; exists {
        result := directive.Resolve()
        lc.resolved[key] = result
        return result.(string)
    }
    
    return ""
}
```

## 🔧 Error Handling

### Directive Error Recovery
```go
func handleDirectiveError(directive string, err error) interface{} {
    log.Printf("Directive error: %s - %v", directive, err)
    
    // Return fallback values
    switch directive {
    case "#env":
        return "default_value"
    case "#http":
        return nil
    default:
        return ""
    }
}
```

### Validation Errors
```go
type DirectiveError struct {
    Directive string
    Message   string
    Params    []string
}

func (de *DirectiveError) Error() string {
    return fmt.Sprintf("Directive %s error: %s (params: %v)", 
        de.Directive, de.Message, de.Params)
}
```

## 🎯 Best Practices

### 1. **Use Descriptive Names**
```tsk
# Good - clear purpose
user_cache_ttl: #cache("10m", "user_data")

# Bad - unclear purpose
cache1: #cache("10m", "data")
```

### 2. **Handle Errors Gracefully**
```tsk
# Provide fallbacks for critical directives
api_key: #env("API_KEY", "fallback_key")
database_url: #env("DATABASE_URL", "sqlite://local.db")
```

### 3. **Document Complex Directives**
```tsk
# Complex FUJSEN directive with documentation
process_users: #fujsen("""
    // Process user data with validation
    function process(users) {
        return users.filter(user => user.active)
                   .map(user => ({
                       id: user.id,
                       name: user.name.toUpperCase()
                   }));
    }
""")
```

### 4. **Test Directive Logic**
```go
func TestDirectiveProcessing(t *testing.T) {
    config := tusk.LoadConfig("test.tsk")
    
    // Test directive resolution
    result := config.GetString("test_directive")
    expected := "expected_value"
    
    if result != expected {
        t.Errorf("Directive result %s != expected %s", result, expected)
    }
}
```

## 🚀 Advanced Patterns

### Directive Composition
```tsk
# Compose multiple directives
complex_value: #transform(
    #http("GET", "https://api.example.com/data"),
    "json",
    "extract_field"
)
```

### Conditional Directives
```tsk
# Conditional directive execution
cache_strategy: #if(
    #env("ENVIRONMENT") == "production",
    #cache("1h", "data"),
    #cache("5m", "data")
)
```

### Recursive Directives
```tsk
# Directive that references other directives
nested_value: #process(
    #env("BASE_URL"),
    #env("API_VERSION")
)
```

## 📊 Monitoring and Debugging

### Directive Performance Metrics
```go
type DirectiveMetrics struct {
    Name       string
    Duration   time.Duration
    Success    bool
    Error      error
}

func trackDirectiveMetrics(directive string, fn func() (interface{}, error)) (interface{}, error) {
    start := time.Now()
    result, err := fn()
    duration := time.Since(start)
    
    metrics := DirectiveMetrics{
        Name:     directive,
        Duration: duration,
        Success:  err == nil,
        Error:    err,
    }
    
    // Log or send metrics
    logDirectiveMetrics(metrics)
    
    return result, err
}
```

### Debug Mode
```go
// Enable debug mode for directive processing
tusk.SetDebugMode(true)

// Now all directive processing is logged
config := tusk.LoadConfig("app.tsk")
// Logs: Processing directive #env("API_KEY")
// Logs: Directive #env resolved to "abc123"
```

## 🎯 Real-World Example

### Complete Application Configuration
```tsk
# app.tsk - Complete application with hash directives

# Environment-aware configuration
environment: #env("ENVIRONMENT", "development")
debug_mode: #env("DEBUG", "false")

# Database configuration
database_url: #env("DATABASE_URL", "sqlite://local.db")
database_pool: #if(
    #env("ENVIRONMENT") == "production",
    20,
    5
)

# API configuration
api_key: #env("API_KEY")
api_base_url: #env("API_BASE_URL", "https://api.example.com")

# Caching strategy
cache_ttl: #if(
    #env("ENVIRONMENT") == "production",
    "1h",
    "5m"
)

# Rate limiting
rate_limit: #if(
    #env("ENVIRONMENT") == "production",
    "1000/hour",
    "unlimited"
)

# Custom processing
user_processor: #fujsen("""
    function processUser(user) {
        return {
            id: user.id,
            name: user.name,
            email: user.email.toLowerCase(),
            created: new Date(user.created_at)
        };
    }
""")
```

### Go Application Using Hash Directives
```go
package main

import (
    "fmt"
    "log"
    "github.com/tusklang/go-sdk"
)

type AppConfig struct {
    Environment   string `tsk:"environment"`
    DebugMode     bool   `tsk:"debug_mode"`
    DatabaseURL   string `tsk:"database_url"`
    DatabasePool  int    `tsk:"database_pool"`
    APIKey        string `tsk:"api_key"`
    APIBaseURL    string `tsk:"api_base_url"`
    CacheTTL      string `tsk:"cache_ttl"`
    RateLimit     string `tsk:"rate_limit"`
    UserProcessor string `tsk:"user_processor"`
}

func main() {
    // Load configuration with all directives processed
    config := tusk.LoadConfig("app.tsk")
    
    var appConfig AppConfig
    if err := config.Unmarshal(&appConfig); err != nil {
        log.Fatal("Failed to load config:", err)
    }
    
    // All directives are resolved automatically
    fmt.Printf("Environment: %s\n", appConfig.Environment)
    fmt.Printf("Database Pool: %d\n", appConfig.DatabasePool)
    fmt.Printf("Cache TTL: %s\n", appConfig.CacheTTL)
    fmt.Printf("Rate Limit: %s\n", appConfig.RateLimit)
    
    // Use the FUJSEN processor
    processor := tusk.NewFujsenProcessor(appConfig.UserProcessor)
    // Process users with the embedded JavaScript function
}
```

## 🎯 Summary

Hash directives transform TuskLang from a simple configuration format into a powerful, executable configuration system. They enable:

- **Dynamic configuration** that adapts to environment
- **Executable logic** embedded in config files
- **Cross-file communication** and dependency resolution
- **Security features** with validation and sandboxing
- **Performance optimization** with caching and lazy loading

The Go SDK provides seamless integration with hash directives, making them feel like native Go features while maintaining the power and flexibility of TuskLang's directive system.

**Next**: Explore specific directive types like `#web`, `#api`, `#cli`, and more in the following guides. 