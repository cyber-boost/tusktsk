# 🔑 TuskLang Go Key-Value Basics Guide

**"We don't bow to any king" - Go Edition**

Master the fundamental key-value syntax in TuskLang and learn how to work with it effectively in Go applications. This guide covers basic syntax, data types, struct mapping, and best practices.

## 🎯 Key-Value Fundamentals

### Basic Key-Value Syntax

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

### Go Struct Mapping

```go
// main.go
package main

import (
    "fmt"
    "github.com/tusklang/go"
)

// Define configuration structs
type AppConfig struct {
    Name    string `tsk:"name"`    // Application name
    Version string `tsk:"version"` // Application version
    Debug   bool   `tsk:"debug"`   // Debug mode
    Port    int    `tsk:"port"`    // Server port
}

type DatabaseConfig struct {
    Host string `tsk:"host"` // Database host
    Port int    `tsk:"port"` // Database port
    Name string `tsk:"name"` // Database name
    SSL  bool   `tsk:"ssl"`  // SSL connection
}

type Config struct {
    App      AppConfig      `tsk:"app"`      // Application settings
    Database DatabaseConfig `tsk:"database"` // Database configuration
}

func main() {
    // Create parser
    parser := tusklanggo.NewEnhancedParser()
    
    // Parse configuration
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
    
    // Use configuration
    fmt.Printf("App: %s v%s\n", config.App.Name, config.App.Version)
    fmt.Printf("Database: %s:%d/%s\n", config.Database.Host, config.Database.Port, config.Database.Name)
    fmt.Printf("Debug: %v\n", config.App.Debug)
}
```

## 📊 Data Types

### String Values

```go
// config.tsk
[strings]
simple: "Hello World"
quoted: "This is a 'quoted' string"
escaped: "Line 1\nLine 2\tTabbed"
unicode: "Hello 世界"
empty: ""

[paths]
config_dir: "/etc/myapp"
data_dir: "/var/lib/myapp"
log_dir: "/var/log/myapp"
```

```go
// main.go
type StringConfig struct {
    Simple  string `tsk:"simple"`  // Simple string
    Quoted  string `tsk:"quoted"`  // String with quotes
    Escaped string `tsk:"escaped"` // String with escapes
    Unicode string `tsk:"unicode"` // Unicode string
    Empty   string `tsk:"empty"`   // Empty string
}

type PathConfig struct {
    ConfigDir string `tsk:"config_dir"` // Configuration directory
    DataDir   string `tsk:"data_dir"`   // Data directory
    LogDir    string `tsk:"log_dir"`    // Log directory
}
```

### Numeric Values

```go
// config.tsk
[numbers]
integer: 42
negative: -100
zero: 0
large: 999999999

[floats]
pi: 3.14159
negative_pi: -3.14159
zero_float: 0.0
scientific: 1.23e-4

[ports]
http_port: 80
https_port: 443
app_port: 8080
db_port: 5432
```

```go
// main.go
type NumberConfig struct {
    Integer int `tsk:"integer"` // Positive integer
    Negative int `tsk:"negative"` // Negative integer
    Zero int `tsk:"zero"` // Zero value
    Large int64 `tsk:"large"` // Large integer
}

type FloatConfig struct {
    Pi float64 `tsk:"pi"` // Pi constant
    NegativePi float64 `tsk:"negative_pi"` // Negative pi
    ZeroFloat float64 `tsk:"zero_float"` // Zero float
    Scientific float64 `tsk:"scientific"` // Scientific notation
}

type PortConfig struct {
    HTTPPort  int `tsk:"http_port"`  // HTTP port
    HTTPSPort int `tsk:"https_port"` // HTTPS port
    AppPort   int `tsk:"app_port"`   // Application port
    DBPort    int `tsk:"db_port"`    // Database port
}
```

### Boolean Values

```go
// config.tsk
[booleans]
enabled: true
disabled: false
debug_mode: true
production: false

[features]
caching: true
logging: true
monitoring: false
ssl: true
```

```go
// main.go
type BooleanConfig struct {
    Enabled bool `tsk:"enabled"` // Feature enabled
    Disabled bool `tsk:"disabled"` // Feature disabled
    DebugMode bool `tsk:"debug_mode"` // Debug mode
    Production bool `tsk:"production"` // Production mode
}

type FeatureConfig struct {
    Caching    bool `tsk:"caching"`    // Enable caching
    Logging    bool `tsk:"logging"`    // Enable logging
    Monitoring bool `tsk:"monitoring"` // Enable monitoring
    SSL        bool `tsk:"ssl"`        // Enable SSL
}
```

### Null Values

```go
// config.tsk
[nullable]
optional_string: null
optional_number: null
optional_boolean: null
optional_array: null
optional_object: null
```

```go
// main.go
type NullableConfig struct {
    OptionalString  *string `tsk:"optional_string"`  // Optional string
    OptionalNumber  *int    `tsk:"optional_number"`  // Optional number
    OptionalBoolean *bool   `tsk:"optional_boolean"` // Optional boolean
    OptionalArray   []interface{} `tsk:"optional_array"`   // Optional array
    OptionalObject  map[string]interface{} `tsk:"optional_object"`  // Optional object
}
```

## 🔗 Advanced Key-Value Features

### Nested Objects

```go
// config.tsk
[server]
host: "localhost"
port: 8080
ssl: true

[server.ssl]
cert_file: "/etc/ssl/cert.pem"
key_file: "/etc/ssl/key.pem"
ca_file: "/etc/ssl/ca.pem"

[server.logging]
level: "info"
file: "/var/log/server.log"
max_size: "100MB"
max_age: "30d"
```

```go
// main.go
type SSLConfig struct {
    CertFile string `tsk:"cert_file"` // SSL certificate file
    KeyFile  string `tsk:"key_file"`  // SSL private key file
    CAFile   string `tsk:"ca_file"`   // SSL CA certificate file
}

type LoggingConfig struct {
    Level   string `tsk:"level"`   // Log level
    File    string `tsk:"file"`    // Log file path
    MaxSize string `tsk:"max_size"` // Maximum log file size
    MaxAge  string `tsk:"max_age"`  // Maximum log file age
}

type ServerConfig struct {
    Host     string        `tsk:"host"`     // Server host
    Port     int           `tsk:"port"`     // Server port
    SSL      bool          `tsk:"ssl"`      // Enable SSL
    SSLConfig SSLConfig    `tsk:"ssl"`      // SSL configuration
    Logging  LoggingConfig `tsk:"logging"`  // Logging configuration
}
```

### Arrays and Lists

```go
// config.tsk
[arrays]
numbers: [1, 2, 3, 4, 5]
strings: ["apple", "banana", "cherry"]
mixed: [1, "two", true, null]

[features]
enabled: ["database", "caching", "logging"]
disabled: ["monitoring", "analytics"]
```

```go
// main.go
type ArrayConfig struct {
    Numbers []int         `tsk:"numbers"` // Array of integers
    Strings []string      `tsk:"strings"` // Array of strings
    Mixed   []interface{} `tsk:"mixed"`   // Mixed type array
}

type FeatureListConfig struct {
    Enabled  []string `tsk:"enabled"`  // Enabled features
    Disabled []string `tsk:"disabled"` // Disabled features
}
```

### Maps and Objects

```go
// config.tsk
[objects]
user: {
    name: "John Doe"
    email: "john@example.com"
    age: 30
    active: true
}

settings: {
    theme: "dark"
    language: "en"
    timezone: "UTC"
    notifications: true
}
```

```go
// main.go
type User struct {
    Name   string `tsk:"name"`   // User name
    Email  string `tsk:"email"`  // User email
    Age    int    `tsk:"age"`    // User age
    Active bool   `tsk:"active"` // User active status
}

type Settings struct {
    Theme         string `tsk:"theme"`         // UI theme
    Language      string `tsk:"language"`      // Language
    Timezone      string `tsk:"timezone"`      // Timezone
    Notifications bool   `tsk:"notifications"` // Enable notifications
}

type ObjectConfig struct {
    User     User     `tsk:"user"`     // User object
    Settings Settings `tsk:"settings"` // Settings object
}
```

## 🔧 Dynamic Values

### Environment Variables

```go
// config.tsk
[environment]
app_name: @env("APP_NAME", "Default App")
debug_mode: @env("DEBUG", "false")
port: @env("PORT", "8080")
database_url: @env("DATABASE_URL")
```

```go
// main.go
type EnvironmentConfig struct {
    AppName     string `tsk:"app_name"`     // App name from env
    DebugMode   bool   `tsk:"debug_mode"`   // Debug mode from env
    Port        int    `tsk:"port"`         // Port from env
    DatabaseURL string `tsk:"database_url"` // Database URL from env
}

func main() {
    // Set environment variables
    os.Setenv("APP_NAME", "My Production App")
    os.Setenv("DEBUG", "true")
    os.Setenv("PORT", "9090")
    os.Setenv("DATABASE_URL", "postgres://user:pass@localhost/db")
    
    parser := tusklanggo.NewEnhancedParser()
    data, err := parser.ParseFile("config.tsk")
    if err != nil {
        panic(err)
    }
    
    var config EnvironmentConfig
    err = tusklanggo.UnmarshalTSK(data["environment"].(map[string]interface{}), &config)
    if err != nil {
        panic(err)
    }
    
    fmt.Printf("App: %s\n", config.AppName)
    fmt.Printf("Debug: %v\n", config.DebugMode)
    fmt.Printf("Port: %d\n", config.Port)
    fmt.Printf("DB URL: %s\n", config.DatabaseURL)
}
```

### Computed Values

```go
// config.tsk
[computed]
timestamp: @date.now()
formatted_date: @date("Y-m-d H:i:s")
user_count: @query("SELECT COUNT(*) FROM users")
cache_key: @cache("5m", "expensive_operation")
```

```go
// main.go
type ComputedConfig struct {
    Timestamp      interface{} `tsk:"timestamp"`       // Current timestamp
    FormattedDate  string      `tsk:"formatted_date"`  // Formatted date
    UserCount      interface{} `tsk:"user_count"`      // User count from DB
    CacheKey       interface{} `tsk:"cache_key"`       // Cache key
}
```

## 🎯 Validation and Type Safety

### Type Validation

```go
// main.go
type ValidatedConfig struct {
    Port    int    `tsk:"port" validate:"min=1,max=65535"`    // Port validation
    Timeout int    `tsk:"timeout" validate:"min=1,max=3600"`  // Timeout validation
    Email   string `tsk:"email" validate:"email"`             // Email validation
    URL     string `tsk:"url" validate:"url"`                 // URL validation
}

func validateConfig(config *ValidatedConfig) error {
    if config.Port < 1 || config.Port > 65535 {
        return fmt.Errorf("port must be between 1 and 65535")
    }
    
    if config.Timeout < 1 || config.Timeout > 3600 {
        return fmt.Errorf("timeout must be between 1 and 3600 seconds")
    }
    
    // Email validation
    if config.Email != "" {
        if !isValidEmail(config.Email) {
            return fmt.Errorf("invalid email format: %s", config.Email)
        }
    }
    
    // URL validation
    if config.URL != "" {
        if !isValidURL(config.URL) {
            return fmt.Errorf("invalid URL format: %s", config.URL)
        }
    }
    
    return nil
}

func isValidEmail(email string) bool {
    // Simple email validation
    return strings.Contains(email, "@") && strings.Contains(email, ".")
}

func isValidURL(url string) bool {
    // Simple URL validation
    return strings.HasPrefix(url, "http://") || strings.HasPrefix(url, "https://")
}
```

### Required Fields

```go
// config.tsk
[required]
app_name: "My App"           # Required
database_host: "localhost"   # Required
api_key: @env("API_KEY")     # Required from env
```

```go
// main.go
type RequiredConfig struct {
    AppName      string `tsk:"app_name"`      // Required application name
    DatabaseHost string `tsk:"database_host"` // Required database host
    APIKey       string `tsk:"api_key"`       // Required API key
}

func validateRequired(config *RequiredConfig) error {
    if config.AppName == "" {
        return fmt.Errorf("app_name is required")
    }
    
    if config.DatabaseHost == "" {
        return fmt.Errorf("database_host is required")
    }
    
    if config.APIKey == "" {
        return fmt.Errorf("api_key is required")
    }
    
    return nil
}
```

## 🔍 Error Handling

### Parsing Errors

```go
// main.go
func loadConfig(filename string) (*Config, error) {
    parser := tusklanggo.NewEnhancedParser()
    
    // Parse configuration
    data, err := parser.ParseFile(filename)
    if err != nil {
        return nil, fmt.Errorf("failed to parse config file %s: %w", filename, err)
    }
    
    // Map to struct
    var config Config
    err = tusklanggo.UnmarshalTSK(data, &config)
    if err != nil {
        return nil, fmt.Errorf("failed to unmarshal config: %w", err)
    }
    
    // Validate configuration
    if err := validateConfig(&config); err != nil {
        return nil, fmt.Errorf("config validation failed: %w", err)
    }
    
    return &config, nil
}

func validateConfig(config *Config) error {
    // Validate app configuration
    if config.App.Name == "" {
        return fmt.Errorf("app.name is required")
    }
    
    if config.App.Port < 1 || config.App.Port > 65535 {
        return fmt.Errorf("app.port must be between 1 and 65535")
    }
    
    // Validate database configuration
    if config.Database.Host == "" {
        return fmt.Errorf("database.host is required")
    }
    
    if config.Database.Port < 1 || config.Database.Port > 65535 {
        return fmt.Errorf("database.port must be between 1 and 65535")
    }
    
    return nil
}
```

### Type Conversion Errors

```go
// main.go
func safeGetString(data map[string]interface{}, key string) (string, error) {
    value, exists := data[key]
    if !exists {
        return "", fmt.Errorf("key '%s' not found", key)
    }
    
    str, ok := value.(string)
    if !ok {
        return "", fmt.Errorf("key '%s' is not a string, got %T", key, value)
    }
    
    return str, nil
}

func safeGetInt(data map[string]interface{}, key string) (int, error) {
    value, exists := data[key]
    if !exists {
        return 0, fmt.Errorf("key '%s' not found", key)
    }
    
    switch v := value.(type) {
    case int:
        return v, nil
    case float64:
        return int(v), nil
    case string:
        if i, err := strconv.Atoi(v); err == nil {
            return i, nil
        }
        return 0, fmt.Errorf("key '%s' cannot be converted to int: %s", key, v)
    default:
        return 0, fmt.Errorf("key '%s' is not a number, got %T", key, value)
    }
}

func safeGetBool(data map[string]interface{}, key string) (bool, error) {
    value, exists := data[key]
    if !exists {
        return false, fmt.Errorf("key '%s' not found", key)
    }
    
    switch v := value.(type) {
    case bool:
        return v, nil
    case string:
        switch strings.ToLower(v) {
        case "true", "1", "yes", "on":
            return true, nil
        case "false", "0", "no", "off":
            return false, nil
        default:
            return false, fmt.Errorf("key '%s' cannot be converted to bool: %s", key, v)
        }
    default:
        return false, fmt.Errorf("key '%s' is not a boolean, got %T", key, value)
    }
}
```

## 🎯 Best Practices

### 1. Consistent Naming

```go
// Good - Consistent naming convention
[app]
name: "My Application"
version: "1.0.0"
debug_mode: true
log_level: "info"

[database]
host: "localhost"
port: 5432
database_name: "myapp"
ssl_enabled: true

# Bad - Inconsistent naming
[app]
name: "My Application"
version: "1.0.0"
debugMode: true
logLevel: "info"

[database]
host: "localhost"
port: 5432
databaseName: "myapp"
sslEnabled: true
```

### 2. Type Safety

```go
// Good - Proper type definitions
type Config struct {
    App struct {
        Name      string `tsk:"name"`
        Version   string `tsk:"version"`
        DebugMode bool   `tsk:"debug_mode"`
        LogLevel  string `tsk:"log_level"`
    } `tsk:"app"`
    
    Database struct {
        Host         string `tsk:"host"`
        Port         int    `tsk:"port"`
        DatabaseName string `tsk:"database_name"`
        SSLEnabled   bool   `tsk:"ssl_enabled"`
    } `tsk:"database"`
}

# Bad - Using interface{} everywhere
type Config struct {
    App      map[string]interface{} `tsk:"app"`
    Database map[string]interface{} `tsk:"database"`
}
```

### 3. Default Values

```go
// Good - Provide sensible defaults
[app]
name: @env("APP_NAME", "Default App")
version: @env("APP_VERSION", "1.0.0")
debug_mode: @env("DEBUG", "false")
port: @env("PORT", "8080")

[database]
host: @env("DB_HOST", "localhost")
port: @env("DB_PORT", "5432")
name: @env("DB_NAME", "myapp")
ssl: @env("DB_SSL", "false")

# Bad - No defaults, will fail if env vars not set
[app]
name: @env("APP_NAME")
version: @env("APP_VERSION")
debug_mode: @env("DEBUG")
port: @env("PORT")
```

### 4. Validation

```go
// Good - Validate configuration
func validateConfig(config *Config) error {
    // Validate app configuration
    if config.App.Name == "" {
        return fmt.Errorf("app.name is required")
    }
    
    if config.App.Port < 1 || config.App.Port > 65535 {
        return fmt.Errorf("app.port must be between 1 and 65535")
    }
    
    // Validate database configuration
    if config.Database.Host == "" {
        return fmt.Errorf("database.host is required")
    }
    
    if config.Database.Port < 1 || config.Database.Port > 65535 {
        return fmt.Errorf("database.port must be between 1 and 65535")
    }
    
    return nil
}

# Bad - No validation
func loadConfig(filename string) (*Config, error) {
    // Load config without validation
    return config, nil
}
```

## 📊 Complete Example

### Configuration File

```go
// config.tsk
# ========================================
# APPLICATION CONFIGURATION
# ========================================
[app]
name: @env("APP_NAME", "My TuskLang App")
version: @env("APP_VERSION", "1.0.0")
debug_mode: @env("DEBUG", "false")
port: @env("PORT", "8080")
log_level: @env("LOG_LEVEL", "info")

# ========================================
# DATABASE CONFIGURATION
# ========================================
[database]
host: @env("DB_HOST", "localhost")
port: @env("DB_PORT", "5432")
name: @env("DB_NAME", "myapp")
user: @env("DB_USER", "postgres")
password: @env("DB_PASSWORD", "")
ssl_enabled: @env("DB_SSL", "false")

# ========================================
# SERVER CONFIGURATION
# ========================================
[server]
host: @env("SERVER_HOST", "0.0.0.0")
port: @env("SERVER_PORT", "8080")
ssl_enabled: @env("SERVER_SSL", "false")
timeout: @env("SERVER_TIMEOUT", "30")

[server.ssl]
cert_file: @env("SSL_CERT_FILE", "/etc/ssl/cert.pem")
key_file: @env("SSL_KEY_FILE", "/etc/ssl/key.pem")
ca_file: @env("SSL_CA_FILE", "/etc/ssl/ca.pem")

# ========================================
# FEATURE FLAGS
# ========================================
[features]
caching: @env("FEATURE_CACHING", "true")
logging: @env("FEATURE_LOGGING", "true")
monitoring: @env("FEATURE_MONITORING", "false")
analytics: @env("FEATURE_ANALYTICS", "false")

# ========================================
# COMPUTED VALUES
# ========================================
[computed]
startup_time: @date.now()
user_count: @query("SELECT COUNT(*) FROM users")
cache_key: @cache("5m", "app_config")
```

### Go Application

```go
// main.go
package main

import (
    "fmt"
    "log"
    "os"
    "strconv"
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
    Host       string   `tsk:"host"`
    Port       int      `tsk:"port"`
    SSLEnabled bool     `tsk:"ssl_enabled"`
    Timeout    int      `tsk:"timeout"`
    SSL        SSLConfig `tsk:"ssl"`
}

type FeatureConfig struct {
    Caching    bool `tsk:"caching"`
    Logging    bool `tsk:"logging"`
    Monitoring bool `tsk:"monitoring"`
    Analytics  bool `tsk:"analytics"`
}

type ComputedConfig struct {
    StartupTime interface{} `tsk:"startup_time"`
    UserCount   interface{} `tsk:"user_count"`
    CacheKey    interface{} `tsk:"cache_key"`
}

type Config struct {
    App      AppConfig      `tsk:"app"`
    Database DatabaseConfig `tsk:"database"`
    Server   ServerConfig   `tsk:"server"`
    Features FeatureConfig  `tsk:"features"`
    Computed ComputedConfig `tsk:"computed"`
}

func main() {
    // Load configuration
    config, err := loadConfig("config.tsk")
    if err != nil {
        log.Fatalf("Failed to load configuration: %v", err)
    }
    
    // Validate configuration
    if err := validateConfig(config); err != nil {
        log.Fatalf("Configuration validation failed: %v", err)
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

func validateConfig(config *Config) error {
    // Validate app configuration
    if config.App.Name == "" {
        return fmt.Errorf("app.name is required")
    }
    
    if config.App.Port < 1 || config.App.Port > 65535 {
        return fmt.Errorf("app.port must be between 1 and 65535")
    }
    
    // Validate database configuration
    if config.Database.Host == "" {
        return fmt.Errorf("database.host is required")
    }
    
    if config.Database.Port < 1 || config.Database.Port > 65535 {
        return fmt.Errorf("database.port must be between 1 and 65535")
    }
    
    // Validate server configuration
    if config.Server.Port < 1 || config.Server.Port > 65535 {
        return fmt.Errorf("server.port must be between 1 and 65535")
    }
    
    if config.Server.Timeout < 1 || config.Server.Timeout > 3600 {
        return fmt.Errorf("server.timeout must be between 1 and 3600 seconds")
    }
    
    return nil
}
```

## 📚 Summary

You've learned:

1. **Key-Value Fundamentals** - Basic syntax and struct mapping
2. **Data Types** - Strings, numbers, booleans, and null values
3. **Advanced Features** - Nested objects, arrays, and maps
4. **Dynamic Values** - Environment variables and computed values
5. **Validation** - Type safety and required field validation
6. **Error Handling** - Robust error handling for parsing and validation
7. **Best Practices** - Consistent naming, type safety, and defaults
8. **Complete Examples** - Real-world configuration management

## 🚀 Next Steps

Now that you understand key-value basics:

1. **Create Configurations** - Build comprehensive configuration files
2. **Implement Validation** - Add robust validation to your applications
3. **Use Environment Variables** - Make configurations environment-aware
4. **Add Type Safety** - Use proper Go structs for type safety
5. **Handle Errors** - Implement comprehensive error handling

---

**"We don't bow to any king"** - You now have the foundation to build powerful, type-safe configuration management with TuskLang in Go! 