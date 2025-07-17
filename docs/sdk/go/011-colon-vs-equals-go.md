# ⚖️ TuskLang Go Colon vs Equals Guide

**"We don't bow to any king" - Go Edition**

Master the different assignment syntaxes in TuskLang and understand when to use colons (`:`) versus equals (`=`) in your Go applications. This guide covers syntax differences, use cases, and best practices.

## 🎯 Syntax Overview

### Colon Syntax (Traditional INI-Style)

```go
// config.tsk
[app]
name: "My Application"
version: "1.0.0"
debug: true
port: 8080

[database]
host: "localhost"
port: 5432
name: "myapp"
ssl: false
```

### Equals Syntax (Modern Style)

```go
// config.tsk
[app]
name = "My Application"
version = "1.0.0"
debug = true
port = 8080

[database]
host = "localhost"
port = 5432
name = "myapp"
ssl = false
```

### Mixed Syntax (Flexible)

```go
// config.tsk
[app]
name: "My Application"
version = "1.0.0"
debug: true
port = 8080

[database]
host: "localhost"
port = 5432
name: "myapp"
ssl = false
```

## 🔧 Go Integration

### Parser Support

```go
// main.go
package main

import (
    "fmt"
    "github.com/tusklang/go"
)

func main() {
    // Create parser that supports both syntaxes
    parser := tusklanggo.NewEnhancedParser()
    
    // Parse configuration with mixed syntax
    data, err := parser.ParseFile("config.tsk")
    if err != nil {
        panic(err)
    }
    
    // Map to struct
    var config Config
    err = tusklanggo.UnmarshalTSK(data, &config)
    if err != nil {
        panic(err)
    }
    
    fmt.Printf("App: %s v%s\n", config.App.Name, config.App.Version)
    fmt.Printf("Database: %s:%d/%s\n", config.Database.Host, config.Database.Port, config.Database.Name)
}

type Config struct {
    App      AppConfig      `tsk:"app"`
    Database DatabaseConfig `tsk:"database"`
}

type AppConfig struct {
    Name    string `tsk:"name"`
    Version string `tsk:"version"`
    Debug   bool   `tsk:"debug"`
    Port    int    `tsk:"port"`
}

type DatabaseConfig struct {
    Host string `tsk:"host"`
    Port int    `tsk:"port"`
    Name string `tsk:"name"`
    SSL  bool   `tsk:"ssl"`
}
```

## 📊 Syntax Comparison

### Colon Syntax (`:`)

```go
// config-colon.tsk
[app]
name: "My Application"
version: "1.0.0"
debug: true
port: 8080

[server]
host: "localhost"
port: 8080
ssl: true

[database]
host: "localhost"
port: 5432
name: "myapp"
user: "postgres"
password: "secret"
ssl: false

[features]
caching: true
logging: true
monitoring: false
analytics: false
```

**Advantages:**
- Traditional INI-style syntax
- Widely recognized and familiar
- Compact and readable
- Good for simple key-value pairs

**Use Cases:**
- Configuration files
- Simple settings
- Legacy system compatibility
- Quick prototyping

### Equals Syntax (`=`)

```go
// config-equals.tsk
[app]
name = "My Application"
version = "1.0.0"
debug = true
port = 8080

[server]
host = "localhost"
port = 8080
ssl = true

[database]
host = "localhost"
port = 5432
name = "myapp"
user = "postgres"
password = "secret"
ssl = false

[features]
caching = true
logging = true
monitoring = false
analytics = false
```

**Advantages:**
- Modern and clean syntax
- Consistent with many programming languages
- Clear assignment semantics
- Good for complex configurations

**Use Cases:**
- Modern applications
- Complex configurations
- Team projects
- Production systems

## 🎯 When to Use Each Syntax

### Use Colon (`:`) When:

```go
// Simple configuration files
[app]
name: "My App"
version: "1.0.0"
debug: true

[server]
host: "localhost"
port: 8080

# Legacy system compatibility
[database]
host: "localhost"
port: 5432
name: "myapp"

# Quick prototyping
[features]
caching: true
logging: true
```

### Use Equals (`=`) When:

```go
// Complex configurations
[app]
name = "My Application"
version = "1.0.0"
debug = true
port = 8080
log_level = "info"

[server]
host = "localhost"
port = 8080
ssl = true
timeout = 30
max_connections = 100

[database]
host = "localhost"
port = 5432
name = "myapp"
user = "postgres"
password = "secret"
ssl = false
pool_size = 10
max_idle_connections = 5

# Modern applications
[features]
caching = true
logging = true
monitoring = false
analytics = false
rate_limiting = true
compression = true
```

### Use Mixed Syntax When:

```go
// Gradual migration
[app]
name: "My Application"  # Legacy style
version = "1.0.0"       # Modern style
debug: true             # Legacy style
port = 8080             # Modern style

[database]
host: "localhost"       # Legacy style
port = 5432             # Modern style
name: "myapp"           # Legacy style
ssl = false             # Modern style

# Team preference
[features]
caching: true           # Simple boolean
logging = true          # Simple boolean
monitoring: false       # Simple boolean
analytics = false       # Simple boolean
```

## 🔧 Advanced Usage

### Dynamic Values with Different Syntax

```go
// config-dynamic.tsk
[environment]
app_name: @env("APP_NAME", "Default App")
debug_mode = @env("DEBUG", "false")
port: @env("PORT", "8080")
database_url = @env("DATABASE_URL")

[computed]
timestamp: @date.now()
formatted_date = @date("Y-m-d H:i:s")
user_count: @query("SELECT COUNT(*) FROM users")
cache_key = @cache("5m", "expensive_operation")

[conditional]
host: @if(@env("ENVIRONMENT") == "production", "0.0.0.0", "localhost")
port = @if(@env("ENVIRONMENT") == "production", 80, 8080)
ssl: @if(@env("ENVIRONMENT") == "production", true, false)
```

```go
// main.go
type EnvironmentConfig struct {
    AppName     string `tsk:"app_name"`
    DebugMode   bool   `tsk:"debug_mode"`
    Port        int    `tsk:"port"`
    DatabaseURL string `tsk:"database_url"`
}

type ComputedConfig struct {
    Timestamp      interface{} `tsk:"timestamp"`
    FormattedDate  string      `tsk:"formatted_date"`
    UserCount      interface{} `tsk:"user_count"`
    CacheKey       interface{} `tsk:"cache_key"`
}

type ConditionalConfig struct {
    Host string `tsk:"host"`
    Port int    `tsk:"port"`
    SSL  bool   `tsk:"ssl"`
}
```

### Nested Objects with Different Syntax

```go
// config-nested.tsk
[server]
host: "localhost"
port = 8080
ssl: true

[server.ssl]
cert_file: "/etc/ssl/cert.pem"
key_file = "/etc/ssl/key.pem"
ca_file: "/etc/ssl/ca.pem"

[server.logging]
level: "info"
file = "/var/log/server.log"
max_size: "100MB"
max_age = "30d"
```

```go
// main.go
type SSLConfig struct {
    CertFile string `tsk:"cert_file"`
    KeyFile  string `tsk:"key_file"`
    CAFile   string `tsk:"ca_file"`
}

type LoggingConfig struct {
    Level   string `tsk:"level"`
    File    string `tsk:"file"`
    MaxSize string `tsk:"max_size"`
    MaxAge  string `tsk:"max_age"`
}

type ServerConfig struct {
    Host     string        `tsk:"host"`
    Port     int           `tsk:"port"`
    SSL      bool          `tsk:"ssl"`
    SSLConfig SSLConfig    `tsk:"ssl"`
    Logging  LoggingConfig `tsk:"logging"`
}
```

## 🎯 Best Practices

### 1. Consistency Within Sections

```go
// Good - Consistent within sections
[app]
name: "My Application"
version: "1.0.0"
debug: true
port: 8080

[database]
host = "localhost"
port = 5432
name = "myapp"
ssl = false

# Bad - Mixed within sections
[app]
name: "My Application"
version = "1.0.0"
debug: true
port = 8080
```

### 2. Use Context-Appropriate Syntax

```go
// Good - Use equals for complex configurations
[server]
host = "localhost"
port = 8080
ssl = true
timeout = 30
max_connections = 100
keep_alive = true
read_timeout = 10
write_timeout = 10

# Good - Use colons for simple configurations
[app]
name: "My App"
version: "1.0.0"
debug: true
```

### 3. Team Standards

```go
// Good - Document team preferences
# Team Standard: Use equals (=) for all new configurations
# Legacy files may use colons (:)
[app]
name = "My Application"
version = "1.0.0"
debug = true
port = 8080

[database]
host = "localhost"
port = 5432
name = "myapp"
ssl = false
```

### 4. Migration Strategy

```go
// Phase 1: Mixed syntax (transition period)
[app]
name: "My Application"  # Legacy
version = "1.0.0"       # New
debug: true             # Legacy
port = 8080             # New

// Phase 2: Consistent equals syntax
[app]
name = "My Application"
version = "1.0.0"
debug = true
port = 8080
```

## 🔍 Error Handling

### Syntax Validation

```go
// main.go
func validateSyntax(filename string) error {
    parser := tusklanggo.NewEnhancedParser()
    
    // Parse configuration
    data, err := parser.ParseFile(filename)
    if err != nil {
        return fmt.Errorf("syntax error in %s: %w", filename, err)
    }
    
    // Check for mixed syntax if strict mode
    if strictMode {
        return checkSyntaxConsistency(data)
    }
    
    return nil
}

func checkSyntaxConsistency(data map[string]interface{}) error {
    // Implementation to check for consistent syntax
    // This would analyze the original file content
    return nil
}
```

### Parser Configuration

```go
// main.go
func createParser(syntaxMode string) *tusklanggo.EnhancedParser {
    parser := tusklanggo.NewEnhancedParser()
    
    switch syntaxMode {
    case "colon":
        parser.SetSyntaxMode(tusklanggo.ColonOnly)
    case "equals":
        parser.SetSyntaxMode(tusklanggo.EqualsOnly)
    case "mixed":
        parser.SetSyntaxMode(tusklanggo.MixedSyntax)
    default:
        parser.SetSyntaxMode(tusklanggo.MixedSyntax) // Default
    }
    
    return parser
}
```

## 📊 Performance Considerations

### Parsing Performance

```go
// main.go
func benchmarkParsing() {
    parser := tusklanggo.NewEnhancedParser()
    
    // Benchmark colon syntax
    start := time.Now()
    for i := 0; i < 1000; i++ {
        _, err := parser.ParseFile("config-colon.tsk")
        if err != nil {
            panic(err)
        }
    }
    colonDuration := time.Since(start)
    
    // Benchmark equals syntax
    start = time.Now()
    for i := 0; i < 1000; i++ {
        _, err := parser.ParseFile("config-equals.tsk")
        if err != nil {
            panic(err)
        }
    }
    equalsDuration := time.Since(start)
    
    fmt.Printf("Colon syntax: %v\n", colonDuration)
    fmt.Printf("Equals syntax: %v\n", equalsDuration)
    fmt.Printf("Difference: %v\n", colonDuration-equalsDuration)
}
```

## 🎯 Complete Example

### Configuration File

```go
// config.tsk
# ========================================
# APPLICATION CONFIGURATION
# ========================================
[app]
name: "My TuskLang App"           # Legacy style for simple values
version = "1.0.0"                 # Modern style for version
debug_mode: true                  # Legacy style for boolean
port = 8080                       # Modern style for number
log_level = "info"                # Modern style for string

# ========================================
# DATABASE CONFIGURATION
# ========================================
[database]
host: "localhost"                 # Legacy style
port = 5432                       # Modern style
name: "myapp"                     # Legacy style
user = "postgres"                 # Modern style
password: @env("DB_PASSWORD")     # Legacy style with dynamic value
ssl_enabled = false               # Modern style

# ========================================
# SERVER CONFIGURATION
# ========================================
[server]
host = "0.0.0.0"                 # Modern style for complex config
port = 8080                       # Modern style
ssl_enabled = true                # Modern style
timeout = 30                      # Modern style
max_connections = 100             # Modern style

[server.ssl]
cert_file: "/etc/ssl/cert.pem"    # Legacy style
key_file = "/etc/ssl/key.pem"     # Modern style
ca_file: "/etc/ssl/ca.pem"        # Legacy style

# ========================================
# FEATURE FLAGS
# ========================================
[features]
caching: true                     # Legacy style for simple boolean
logging = true                    # Modern style for simple boolean
monitoring: false                 # Legacy style
analytics = false                 # Modern style
rate_limiting = true              # Modern style
compression = true                # Modern style
```

### Go Application

```go
// main.go
package main

import (
    "fmt"
    "log"
    "github.com/tusklang/go"
)

// Configuration structures
type AppConfig struct {
    Name      string `tsk:"name"`
    Version   string `tsk:"version"`
    DebugMode bool   `tsk:"debug_mode"`
    Port      int    `tsk:"port"`
    LogLevel  string `tsk:"log_level"`
}

type DatabaseConfig struct {
    Host        string `tsk:"host"`
    Port        int    `tsk:"port"`
    Name        string `tsk:"name"`
    User        string `tsk:"user"`
    Password    string `tsk:"password"`
    SSLEnabled  bool   `tsk:"ssl_enabled"`
}

type SSLConfig struct {
    CertFile string `tsk:"cert_file"`
    KeyFile  string `tsk:"key_file"`
    CAFile   string `tsk:"ca_file"`
}

type ServerConfig struct {
    Host            string   `tsk:"host"`
    Port            int      `tsk:"port"`
    SSLEnabled      bool     `tsk:"ssl_enabled"`
    Timeout         int      `tsk:"timeout"`
    MaxConnections  int      `tsk:"max_connections"`
    SSL             SSLConfig `tsk:"ssl"`
}

type FeatureConfig struct {
    Caching       bool `tsk:"caching"`
    Logging       bool `tsk:"logging"`
    Monitoring    bool `tsk:"monitoring"`
    Analytics     bool `tsk:"analytics"`
    RateLimiting  bool `tsk:"rate_limiting"`
    Compression   bool `tsk:"compression"`
}

type Config struct {
    App      AppConfig      `tsk:"app"`
    Database DatabaseConfig `tsk:"database"`
    Server   ServerConfig   `tsk:"server"`
    Features FeatureConfig  `tsk:"features"`
}

func main() {
    // Load configuration
    config, err := loadConfig("config.tsk")
    if err != nil {
        log.Fatalf("Failed to load configuration: %v", err)
    }
    
    // Use configuration
    fmt.Printf("🚀 Starting %s v%s\n", config.App.Name, config.App.Version)
    fmt.Printf("🌐 Server: %s:%d\n", config.Server.Host, config.Server.Port)
    fmt.Printf("🗄️ Database: %s:%d/%s\n", config.Database.Host, config.Database.Port, config.Database.Name)
    fmt.Printf("🔧 Debug Mode: %v\n", config.App.DebugMode)
    fmt.Printf("📊 Features: Caching=%v, Logging=%v, Monitoring=%v\n", 
        config.Features.Caching, config.Features.Logging, config.Features.Monitoring)
}

func loadConfig(filename string) (*Config, error) {
    parser := tusklanggo.NewEnhancedParser()
    
    data, err := parser.ParseFile(filename)
    if err != nil {
        return nil, fmt.Errorf("failed to parse config file: %w", err)
    }
    
    var config Config
    err = tusklanggo.UnmarshalTSK(data, &config)
    if err != nil {
        return nil, fmt.Errorf("failed to unmarshal config: %w", err)
    }
    
    return &config, nil
}
```

## 📚 Summary

You've learned:

1. **Syntax Differences** - Colon vs equals syntax in TuskLang
2. **Use Cases** - When to use each syntax style
3. **Go Integration** - How both syntaxes work with Go structs
4. **Best Practices** - Consistency and team standards
5. **Migration Strategy** - Transitioning between syntax styles
6. **Error Handling** - Syntax validation and error handling
7. **Performance** - Parsing performance considerations
8. **Complete Examples** - Real-world configuration management

## 🚀 Next Steps

Now that you understand colon vs equals syntax:

1. **Choose Your Style** - Decide on syntax preferences for your project
2. **Set Team Standards** - Establish consistent syntax guidelines
3. **Migrate Gradually** - Transition existing configurations
4. **Validate Syntax** - Implement syntax validation in your applications
5. **Document Standards** - Document your syntax preferences

---

**"We don't bow to any king"** - You now have the flexibility to choose the syntax that works best for your TuskLang Go applications! 