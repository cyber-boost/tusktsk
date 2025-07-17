# 🥜 Peanut Binary Configuration Guide for Go

A comprehensive guide to using TuskLang's high-performance binary configuration system with Go.

## Table of Contents

- [What is Peanut Configuration?](#what-is-peanut-configuration)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Core Concepts](#core-concepts)
- [API Reference](#api-reference)
- [Advanced Usage](#advanced-usage)
- [Go-Specific Features](#go-specific-features)
- [Integration Examples](#integration-examples)
- [Binary Format Details](#binary-format-specification)
- [Performance Guide](#performance-optimization)
- [Troubleshooting](#troubleshooting)
- [Migration Guide](#migration-guide)
- [Complete Examples](#complete-examples)
- [Quick Reference](#quick-reference)

## What is Peanut Configuration?

Peanut Configuration is TuskLang's high-performance binary configuration system that provides:

- **85% faster loading** compared to text-based formats
- **Hierarchical configuration** with CSS-like cascading
- **Type-safe access** with Go struct mapping
- **Binary format** for production deployments
- **File watching** for development environments

The system supports three file formats:
- `.peanuts` - Human-readable configuration (INI-style)
- `.tsk` - TuskLang syntax (advanced features)
- `.pnt` - Compiled binary format (fastest)

## Installation

### Prerequisites

- Go 1.21 or higher
- TuskLang Go SDK installed

### Installing the SDK

```bash
# Clone the repository
git clone https://github.com/cyber-boost/go-sdk.git
cd go-sdk

# Install dependencies
go mod tidy

# Build and install
make install
```

### Importing PeanutConfig

```go
import (
    "github.com/tusklang/go-sdk/peanut"
)
```

## Quick Start

### Your First Peanut Configuration

1. Create a `peanu.peanuts` file:

```ini
[app]
name: "My Go App"
version: "1.0.0"

[server]
host: "localhost"
port: 8080
debug: true

[database]
host: "localhost"
port: 5432
name: "myapp"
```

2. Load the configuration:

```go
package main

import (
    "fmt"
    "log"
    "github.com/tusklang/go-sdk/peanut"
)

func main() {
    // Load configuration from current directory
    config, err := peanut.Load(".")
    if err != nil {
        log.Fatal(err)
    }

    // Access values
    appName := config.Get("app.name", "default")
    port := config.GetInt("server.port", 3000)
    
    fmt.Printf("App: %s, Port: %d\n", appName, port)
}
```

3. Compile to binary for production:

```go
// Compile to binary format
err = config.CompileToBinary("peanu.peanuts", "peanu.pnt")
if err != nil {
    log.Fatal(err)
}
```

## Core Concepts

### File Types

- **`.peanuts`** - Human-readable configuration using INI-style syntax
- **`.tsk`** - TuskLang syntax with advanced features like expressions and imports
- **`.pnt`** - Compiled binary format for maximum performance

### Hierarchical Loading

Configuration files are loaded in a hierarchical manner, similar to CSS cascading:

1. System-wide configuration (`/etc/peanu.peanuts`)
2. User configuration (`~/.peanu.peanuts`)
3. Project root (`./peanu.peanuts`)
4. Subdirectory overrides (`./config/peanu.peanuts`)

Later files override earlier ones, allowing for environment-specific configurations.

### Type System

Peanut Configuration supports automatic type inference:

```go
// String values
name := config.Get("app.name", "default")

// Numeric values
port := config.GetInt("server.port", 3000)
timeout := config.GetFloat("server.timeout", 30.0)

// Boolean values
debug := config.GetBool("server.debug", false)

// Arrays
hosts := config.GetStringSlice("database.hosts", []string{"localhost"})

// Maps
settings := config.GetStringMap("app.settings", map[string]interface{}{})
```

## API Reference

### PeanutConfig Struct

#### Constructor/Initialization

```go
// Load from directory
config, err := peanut.Load(".")

// Load from specific file
config, err := peanut.LoadFromFile("config.peanuts")

// Load from binary file
config, err := peanut.LoadFromBinary("config.pnt")

// Create empty config
config := peanut.New()
```

#### Methods

##### Load(directory string) (*PeanutConfig, error)

Loads configuration from a directory, following the hierarchical loading pattern.

**Parameters:**
- `directory` (string) - Directory path to search for configuration files

**Returns:**
- `*PeanutConfig` - Configuration object
- `error` - Error if loading fails

**Example:**
```go
config, err := peanut.Load(".")
if err != nil {
    log.Fatal(err)
}
```

##### Get(keyPath string, defaultValue interface{}) interface{}

Retrieves a value from the configuration using dot notation.

**Parameters:**
- `keyPath` (string) - Configuration key path (e.g., "server.port")
- `defaultValue` (interface{}) - Default value if key not found

**Returns:**
- `interface{}` - Configuration value

**Example:**
```go
port := config.Get("server.port", 3000)
name := config.Get("app.name", "default")
```

##### GetString(keyPath string, defaultValue string) string

Retrieves a string value from the configuration.

**Example:**
```go
name := config.GetString("app.name", "default")
```

##### GetInt(keyPath string, defaultValue int) int

Retrieves an integer value from the configuration.

**Example:**
```go
port := config.GetInt("server.port", 3000)
```

##### GetBool(keyPath string, defaultValue bool) bool

Retrieves a boolean value from the configuration.

**Example:**
```go
debug := config.GetBool("server.debug", false)
```

##### GetFloat(keyPath string, defaultValue float64) float64

Retrieves a float value from the configuration.

**Example:**
```go
timeout := config.GetFloat("server.timeout", 30.0)
```

##### GetStringSlice(keyPath string, defaultValue []string) []string

Retrieves a string slice from the configuration.

**Example:**
```go
hosts := config.GetStringSlice("database.hosts", []string{"localhost"})
```

##### GetStringMap(keyPath string, defaultValue map[string]interface{}) map[string]interface{}

Retrieves a map from the configuration.

**Example:**
```go
settings := config.GetStringMap("app.settings", map[string]interface{}{})
```

##### Set(keyPath string, value interface{}) error

Sets a value in the configuration.

**Parameters:**
- `keyPath` (string) - Configuration key path
- `value` (interface{}) - Value to set

**Returns:**
- `error` - Error if setting fails

**Example:**
```go
err := config.Set("server.port", 8080)
if err != nil {
    log.Fatal(err)
}
```

##### CompileToBinary(inputFile, outputFile string) error

Compiles a text configuration file to binary format.

**Parameters:**
- `inputFile` (string) - Input file path (.peanuts or .tsk)
- `outputFile` (string) - Output binary file path (.pnt)

**Returns:**
- `error` - Error if compilation fails

**Example:**
```go
err := config.CompileToBinary("config.peanuts", "config.pnt")
if err != nil {
    log.Fatal(err)
}
```

##### Watch(callback func(*PeanutConfig)) error

Watches for configuration file changes and calls the callback when changes are detected.

**Parameters:**
- `callback` (func(*PeanutConfig)) - Function called when configuration changes

**Returns:**
- `error` - Error if watching fails

**Example:**
```go
err := config.Watch(func(newConfig *peanut.PeanutConfig) {
    fmt.Println("Configuration updated!")
    // Handle configuration changes
})
if err != nil {
    log.Fatal(err)
}
```

## Advanced Usage

### File Watching

```go
package main

import (
    "fmt"
    "log"
    "github.com/tusklang/go-sdk/peanut"
)

func main() {
    config, err := peanut.Load(".")
    if err != nil {
        log.Fatal(err)
    }

    // Watch for changes
    err = config.Watch(func(newConfig *peanut.PeanutConfig) {
        fmt.Println("Configuration updated!")
        
        // Reload configuration
        port := newConfig.GetInt("server.port", 3000)
        fmt.Printf("New port: %d\n", port)
    })
    if err != nil {
        log.Fatal(err)
    }

    // Keep the program running
    select {}
}
```

### Custom Serialization

```go
package main

import (
    "encoding/json"
    "fmt"
    "github.com/tusklang/go-sdk/peanut"
)

type ServerConfig struct {
    Host string `json:"host"`
    Port int    `json:"port"`
    SSL  bool   `json:"ssl"`
}

func main() {
    config, err := peanut.Load(".")
    if err != nil {
        log.Fatal(err)
    }

    // Get server configuration as map
    serverMap := config.GetStringMap("server", map[string]interface{}{})
    
    // Convert to JSON
    jsonData, err := json.Marshal(serverMap)
    if err != nil {
        log.Fatal(err)
    }

    // Unmarshal to struct
    var serverConfig ServerConfig
    err = json.Unmarshal(jsonData, &serverConfig)
    if err != nil {
        log.Fatal(err)
    }

    fmt.Printf("Server: %s:%d (SSL: %t)\n", 
        serverConfig.Host, serverConfig.Port, serverConfig.SSL)
}
```

### Performance Optimization

```go
package main

import (
    "sync"
    "github.com/tusklang/go-sdk/peanut"
)

// Singleton pattern for configuration
var (
    configInstance *peanut.PeanutConfig
    configOnce     sync.Once
    configMutex    sync.RWMutex
)

func GetConfig() *peanut.PeanutConfig {
    configOnce.Do(func() {
        var err error
        configInstance, err = peanut.Load(".")
        if err != nil {
            panic(err)
        }
    })
    return configInstance
}

func GetConfigValue(key string, defaultValue interface{}) interface{} {
    configMutex.RLock()
    defer configMutex.RUnlock()
    return GetConfig().Get(key, defaultValue)
}

func main() {
    // Thread-safe configuration access
    port := GetConfigValue("server.port", 3000)
    fmt.Printf("Port: %v\n", port)
}
```

### Thread Safety

```go
package main

import (
    "sync"
    "github.com/tusklang/go-sdk/peanut"
)

type SafeConfig struct {
    config *peanut.PeanutConfig
    mutex  sync.RWMutex
}

func NewSafeConfig() (*SafeConfig, error) {
    config, err := peanut.Load(".")
    if err != nil {
        return nil, err
    }
    
    return &SafeConfig{
        config: config,
    }, nil
}

func (sc *SafeConfig) Get(key string, defaultValue interface{}) interface{} {
    sc.mutex.RLock()
    defer sc.mutex.RUnlock()
    return sc.config.Get(key, defaultValue)
}

func (sc *SafeConfig) Set(key string, value interface{}) error {
    sc.mutex.Lock()
    defer sc.mutex.Unlock()
    return sc.config.Set(key, value)
}

func (sc *SafeConfig) Reload() error {
    sc.mutex.Lock()
    defer sc.mutex.Unlock()
    
    config, err := peanut.Load(".")
    if err != nil {
        return err
    }
    
    sc.config = config
    return nil
}
```

## Go-Specific Features

### Struct Mapping

```go
package main

import (
    "fmt"
    "github.com/tusklang/go-sdk/peanut"
)

type AppConfig struct {
    Name    string `peanut:"name"`
    Version string `peanut:"version"`
    Server  struct {
        Host string `peanut:"host"`
        Port int    `peanut:"port"`
    } `peanut:"server"`
    Database struct {
        Host string `peanut:"host"`
        Port int    `peanut:"port"`
        Name string `peanut:"name"`
    } `peanut:"database"`
}

func main() {
    config, err := peanut.Load(".")
    if err != nil {
        log.Fatal(err)
    }

    var appConfig AppConfig
    err = config.UnmarshalKey("app", &appConfig)
    if err != nil {
        log.Fatal(err)
    }

    fmt.Printf("App: %s v%s\n", appConfig.Name, appConfig.Version)
    fmt.Printf("Server: %s:%d\n", appConfig.Server.Host, appConfig.Server.Port)
}
```

### Context Support

```go
package main

import (
    "context"
    "time"
    "github.com/tusklang/go-sdk/peanut"
)

func main() {
    ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
    defer cancel()

    // Load configuration with context
    config, err := peanut.LoadWithContext(ctx, ".")
    if err != nil {
        log.Fatal(err)
    }

    // Use configuration
    port := config.GetInt("server.port", 3000)
    fmt.Printf("Port: %d\n", port)
}
```

### Error Handling

```go
package main

import (
    "fmt"
    "github.com/tusklang/go-sdk/peanut"
)

func main() {
    config, err := peanut.Load(".")
    if err != nil {
        switch err.(type) {
        case *peanut.FileNotFoundError:
            fmt.Println("Configuration file not found")
        case *peanut.ParseError:
            fmt.Println("Configuration file has syntax errors")
        case *peanut.ValidationError:
            fmt.Println("Configuration validation failed")
        default:
            fmt.Printf("Unknown error: %v\n", err)
        }
        return
    }

    // Use configuration
    port := config.GetInt("server.port", 3000)
    fmt.Printf("Port: %d\n", port)
}
```

## Integration Examples

### Web Framework Integration (Gin)

```go
package main

import (
    "github.com/gin-gonic/gin"
    "github.com/tusklang/go-sdk/peanut"
)

func main() {
    // Load configuration
    config, err := peanut.Load(".")
    if err != nil {
        log.Fatal(err)
    }

    // Set Gin mode based on configuration
    if config.GetBool("server.debug", false) {
        gin.SetMode(gin.DebugMode)
    } else {
        gin.SetMode(gin.ReleaseMode)
    }

    // Create router
    r := gin.Default()

    // Add routes
    r.GET("/", func(c *gin.Context) {
        c.JSON(200, gin.H{
            "message": "Hello from " + config.GetString("app.name", "Go App"),
        })
    })

    // Start server
    port := config.GetInt("server.port", 3000)
    r.Run(fmt.Sprintf(":%d", port))
}
```

### Database Integration (GORM)

```go
package main

import (
    "fmt"
    "gorm.io/driver/postgres"
    "gorm.io/gorm"
    "github.com/tusklang/go-sdk/peanut"
)

func main() {
    // Load configuration
    config, err := peanut.Load(".")
    if err != nil {
        log.Fatal(err)
    }

    // Build database connection string
    dbHost := config.GetString("database.host", "localhost")
    dbPort := config.GetInt("database.port", 5432)
    dbName := config.GetString("database.name", "myapp")
    dbUser := config.GetString("database.user", "postgres")
    dbPass := config.GetString("database.password", "")

    dsn := fmt.Sprintf("host=%s port=%d user=%s password=%s dbname=%s sslmode=disable",
        dbHost, dbPort, dbUser, dbPass, dbName)

    // Connect to database
    db, err := gorm.Open(postgres.Open(dsn), &gorm.Config{})
    if err != nil {
        log.Fatal(err)
    }

    fmt.Println("Connected to database successfully!")
}
```

## Binary Format Specification

### File Structure

| Offset | Size | Description |
|--------|------|-------------|
| 0 | 4 | Magic: "PNUT" |
| 4 | 4 | Version (LE) |
| 8 | 8 | Timestamp (LE) |
| 16 | 8 | SHA256 checksum |
| 24 | N | Serialized data |

### Serialization Format

The Go implementation uses a custom binary serialization format optimized for:

- **Fast deserialization** using reflection
- **Type safety** with Go's type system
- **Memory efficiency** with compact encoding
- **Cross-platform compatibility**

### Binary File Creation

```go
package main

import (
    "github.com/tusklang/go-sdk/peanut"
)

func main() {
    config, err := peanut.Load(".")
    if err != nil {
        log.Fatal(err)
    }

    // Compile to binary
    err = config.CompileToBinary("config.peanuts", "config.pnt")
    if err != nil {
        log.Fatal(err)
    }

    fmt.Println("Binary configuration created successfully!")
}
```

## Performance Optimization

### Benchmarks

```go
package main

import (
    "fmt"
    "time"
    "github.com/tusklang/go-sdk/peanut"
)

func benchmarkLoading() {
    // Benchmark text loading
    start := time.Now()
    config, err := peanut.LoadFromFile("config.peanuts")
    textDuration := time.Since(start)
    
    if err != nil {
        log.Fatal(err)
    }

    // Benchmark binary loading
    start = time.Now()
    binaryConfig, err := peanut.LoadFromBinary("config.pnt")
    binaryDuration := time.Since(start)
    
    if err != nil {
        log.Fatal(err)
    }

    fmt.Printf("Text loading: %v\n", textDuration)
    fmt.Printf("Binary loading: %v\n", binaryDuration)
    fmt.Printf("Speed improvement: %.2fx\n", float64(textDuration)/float64(binaryDuration))
}
```

### Best Practices

1. **Always use .pnt in production** for maximum performance
2. **Cache configuration objects** to avoid repeated loading
3. **Use file watching wisely** - disable in production
4. **Implement proper error handling** for configuration loading
5. **Use struct mapping** for type-safe configuration access
6. **Consider thread safety** in concurrent applications

## Troubleshooting

### Common Issues

#### File Not Found

**Problem:** Configuration file not found in expected location.

**Solution:**
```go
// Check if file exists before loading
if _, err := os.Stat("peanu.peanuts"); os.IsNotExist(err) {
    // Create default configuration
    config := peanut.New()
    config.Set("app.name", "Default App")
    config.Set("server.port", 3000)
    
    // Save to file
    err = config.SaveToFile("peanu.peanuts")
    if err != nil {
        log.Fatal(err)
    }
}
```

#### Checksum Mismatch

**Problem:** Binary file corruption detected.

**Solution:**
```go
// Verify binary file integrity
err := config.VerifyBinary("config.pnt")
if err != nil {
    // Recompile from source
    err = config.CompileToBinary("config.peanuts", "config.pnt")
    if err != nil {
        log.Fatal(err)
    }
}
```

#### Performance Issues

**Problem:** Slow configuration loading.

**Solution:**
```go
// Use binary format in production
if os.Getenv("ENV") == "production" {
    config, err = peanut.LoadFromBinary("config.pnt")
} else {
    config, err = peanut.Load(".")
}
```

### Debug Mode

```go
package main

import (
    "github.com/tusklang/go-sdk/peanut"
)

func main() {
    // Enable debug logging
    peanut.SetDebug(true)
    
    config, err := peanut.Load(".")
    if err != nil {
        log.Fatal(err)
    }
    
    // Debug information will be printed
    fmt.Printf("Loaded %d configuration files\n", config.FileCount())
}
```

## Migration Guide

### From JSON

```go
package main

import (
    "encoding/json"
    "github.com/tusklang/go-sdk/peanut"
)

func migrateFromJSON() error {
    // Read JSON file
    jsonData, err := os.ReadFile("config.json")
    if err != nil {
        return err
    }
    
    // Parse JSON
    var jsonConfig map[string]interface{}
    err = json.Unmarshal(jsonData, &jsonConfig)
    if err != nil {
        return err
    }
    
    // Create PeanutConfig
    config := peanut.New()
    
    // Convert JSON structure to PeanutConfig
    for key, value := range jsonConfig {
        config.Set(key, value)
    }
    
    // Save as peanuts file
    return config.SaveToFile("peanu.peanuts")
}
```

### From YAML

```go
package main

import (
    "gopkg.in/yaml.v3"
    "github.com/tusklang/go-sdk/peanut"
)

func migrateFromYAML() error {
    // Read YAML file
    yamlData, err := os.ReadFile("config.yaml")
    if err != nil {
        return err
    }
    
    // Parse YAML
    var yamlConfig map[string]interface{}
    err = yaml.Unmarshal(yamlData, &yamlConfig)
    if err != nil {
        return err
    }
    
    // Create PeanutConfig
    config := peanut.New()
    
    // Convert YAML structure to PeanutConfig
    for key, value := range yamlConfig {
        config.Set(key, value)
    }
    
    // Save as peanuts file
    return config.SaveToFile("peanu.peanuts")
}
```

### From .env

```go
package main

import (
    "bufio"
    "os"
    "strings"
    "github.com/tusklang/go-sdk/peanut"
)

func migrateFromEnv() error {
    // Read .env file
    file, err := os.Open(".env")
    if err != nil {
        return err
    }
    defer file.Close()
    
    // Create PeanutConfig
    config := peanut.New()
    
    scanner := bufio.NewScanner(file)
    for scanner.Scan() {
        line := strings.TrimSpace(scanner.Text())
        
        // Skip comments and empty lines
        if line == "" || strings.HasPrefix(line, "#") {
            continue
        }
        
        // Parse key=value
        parts := strings.SplitN(line, "=", 2)
        if len(parts) == 2 {
            key := strings.TrimSpace(parts[0])
            value := strings.TrimSpace(parts[1])
            
            // Remove quotes if present
            value = strings.Trim(value, `"'`)
            
            config.Set(key, value)
        }
    }
    
    // Save as peanuts file
    return config.SaveToFile("peanu.peanuts")
}
```

## Complete Examples

### Web Application Configuration

**File Structure:**
```
myapp/
├── peanu.peanuts
├── config/
│   ├── development.peanuts
│   └── production.peanuts
├── main.go
└── go.mod
```

**peanu.peanuts:**
```ini
[app]
name: "My Web App"
version: "1.0.0"

[server]
host: "localhost"
port: 3000
debug: true

[database]
host: "localhost"
port: 5432
name: "myapp"
user: "postgres"
password: ""

[redis]
host: "localhost"
port: 6379
db: 0
```

**main.go:**
```go
package main

import (
    "fmt"
    "log"
    "os"
    "github.com/tusklang/go-sdk/peanut"
)

type Config struct {
    App struct {
        Name    string `peanut:"name"`
        Version string `peanut:"version"`
    } `peanut:"app"`
    Server struct {
        Host  string `peanut:"host"`
        Port  int    `peanut:"port"`
        Debug bool   `peanut:"debug"`
    } `peanut:"server"`
    Database struct {
        Host     string `peanut:"host"`
        Port     int    `peanut:"port"`
        Name     string `peanut:"name"`
        User     string `peanut:"user"`
        Password string `peanut:"password"`
    } `peanut:"database"`
    Redis struct {
        Host string `peanut:"host"`
        Port int    `peanut:"port"`
        DB   int    `peanut:"db"`
    } `peanut:"redis"`
}

func main() {
    // Load configuration
    config, err := peanut.Load(".")
    if err != nil {
        log.Fatal(err)
    }

    // Parse into struct
    var cfg Config
    err = config.Unmarshal(&cfg)
    if err != nil {
        log.Fatal(err)
    }

    // Use configuration
    fmt.Printf("Starting %s v%s\n", cfg.App.Name, cfg.App.Version)
    fmt.Printf("Server: %s:%d (Debug: %t)\n", 
        cfg.Server.Host, cfg.Server.Port, cfg.Server.Debug)
    fmt.Printf("Database: %s:%d/%s\n", 
        cfg.Database.Host, cfg.Database.Port, cfg.Database.Name)
    fmt.Printf("Redis: %s:%d/%d\n", 
        cfg.Redis.Host, cfg.Redis.Port, cfg.Redis.DB)

    // Compile to binary for production
    if os.Getenv("ENV") == "production" {
        err = config.CompileToBinary("peanu.peanuts", "peanu.pnt")
        if err != nil {
            log.Printf("Warning: Failed to compile binary: %v", err)
        }
    }
}
```

### Microservice Configuration

**File Structure:**
```
microservice/
├── peanu.peanuts
├── cmd/
│   └── server/
│       └── main.go
├── internal/
│   └── config/
│       └── config.go
└── go.mod
```

**peanu.peanuts:**
```ini
[service]
name: "user-service"
version: "1.0.0"
environment: "development"

[server]
host: "0.0.0.0"
port: 8080
timeout: 30
max_connections: 1000

[database]
host: "localhost"
port: 5432
name: "users"
user: "postgres"
password: ""
max_open_conns: 25
max_idle_conns: 5

[redis]
host: "localhost"
port: 6379
db: 0
password: ""
pool_size: 10

[logging]
level: "info"
format: "json"
output: "stdout"

[monitoring]
enabled: true
port: 9090
metrics_path: "/metrics"
```

**internal/config/config.go:**
```go
package config

import (
    "sync"
    "github.com/tusklang/go-sdk/peanut"
)

type Config struct {
    config *peanut.PeanutConfig
    mutex  sync.RWMutex
}

var (
    instance *Config
    once     sync.Once
)

func GetInstance() *Config {
    once.Do(func() {
        config, err := peanut.Load(".")
        if err != nil {
            panic(err)
        }
        
        instance = &Config{
            config: config,
        }
    })
    return instance
}

func (c *Config) GetString(key string, defaultValue string) string {
    c.mutex.RLock()
    defer c.mutex.RUnlock()
    return c.config.GetString(key, defaultValue)
}

func (c *Config) GetInt(key string, defaultValue int) int {
    c.mutex.RLock()
    defer c.mutex.RUnlock()
    return c.config.GetInt(key, defaultValue)
}

func (c *Config) GetBool(key string, defaultValue bool) bool {
    c.mutex.RLock()
    defer c.mutex.RUnlock()
    return c.config.GetBool(key, defaultValue)
}

func (c *Config) Reload() error {
    c.mutex.Lock()
    defer c.mutex.Unlock()
    
    config, err := peanut.Load(".")
    if err != nil {
        return err
    }
    
    c.config = config
    return nil
}
```

**cmd/server/main.go:**
```go
package main

import (
    "fmt"
    "log"
    "net/http"
    "time"
    "myapp/internal/config"
)

func main() {
    cfg := config.GetInstance()
    
    // Configure server
    server := &http.Server{
        Addr:         fmt.Sprintf("%s:%d", 
            cfg.GetString("server.host", "0.0.0.0"),
            cfg.GetInt("server.port", 8080)),
        ReadTimeout:  time.Duration(cfg.GetInt("server.timeout", 30)) * time.Second,
        WriteTimeout: time.Duration(cfg.GetInt("server.timeout", 30)) * time.Second,
        MaxHeaderBytes: 1 << 20,
    }
    
    // Add routes
    http.HandleFunc("/health", func(w http.ResponseWriter, r *http.Request) {
        w.WriteHeader(http.StatusOK)
        w.Write([]byte("OK"))
    })
    
    // Start server
    log.Printf("Starting %s v%s on %s", 
        cfg.GetString("service.name", "service"),
        cfg.GetString("service.version", "1.0.0"),
        server.Addr)
    
    log.Fatal(server.ListenAndServe())
}
```

### CLI Tool Configuration

**File Structure:**
```
cli-tool/
├── peanu.peanuts
├── cmd/
│   └── main.go
├── internal/
│   └── config/
│       └── config.go
└── go.mod
```

**peanu.peanuts:**
```ini
[app]
name: "My CLI Tool"
version: "1.0.0"
description: "A powerful command-line tool"

[output]
format: "text"
color: true
verbose: false
quiet: false

[api]
base_url: "https://api.example.com"
timeout: 30
retries: 3
api_key: ""

[files]
input_dir: "./input"
output_dir: "./output"
temp_dir: "./temp"
backup: true

[logging]
level: "info"
file: "cli.log"
max_size: 10
max_backups: 5
```

**internal/config/config.go:**
```go
package config

import (
    "flag"
    "github.com/tusklang/go-sdk/peanut"
)

type Config struct {
    config *peanut.PeanutConfig
}

func New() (*Config, error) {
    config, err := peanut.Load(".")
    if err != nil {
        return nil, err
    }
    
    return &Config{
        config: config,
    }, nil
}

func (c *Config) ParseFlags() {
    // Override config with command-line flags
    verbose := flag.Bool("verbose", c.config.GetBool("output.verbose", false), "Enable verbose output")
    quiet := flag.Bool("quiet", c.config.GetBool("output.quiet", false), "Suppress output")
    format := flag.String("format", c.config.GetString("output.format", "text"), "Output format")
    
    flag.Parse()
    
    // Update configuration with flag values
    c.config.Set("output.verbose", *verbose)
    c.config.Set("output.quiet", *quiet)
    c.config.Set("output.format", *format)
}

func (c *Config) GetString(key string, defaultValue string) string {
    return c.config.GetString(key, defaultValue)
}

func (c *Config) GetBool(key string, defaultValue bool) bool {
    return c.config.GetBool(key, defaultValue)
}

func (c *Config) GetInt(key string, defaultValue int) int {
    return c.config.GetInt(key, defaultValue)
}
```

**cmd/main.go:**
```go
package main

import (
    "fmt"
    "log"
    "os"
    "myapp/internal/config"
)

func main() {
    // Load configuration
    cfg, err := config.New()
    if err != nil {
        log.Fatal(err)
    }
    
    // Parse command-line flags
    cfg.ParseFlags()
    
    // Check if verbose mode is enabled
    if cfg.GetBool("output.verbose", false) {
        fmt.Printf("Configuration loaded successfully\n")
        fmt.Printf("App: %s v%s\n", 
            cfg.GetString("app.name", "CLI Tool"),
            cfg.GetString("app.version", "1.0.0"))
    }
    
    // Check if quiet mode is enabled
    if cfg.GetBool("output.quiet", false) {
        // Suppress output
        return
    }
    
    // Main application logic
    fmt.Printf("Hello from %s!\n", cfg.GetString("app.name", "CLI Tool"))
}
```

## Quick Reference

### Common Operations

```go
// Load config
config, err := peanut.Load(".")

// Get value
value := config.Get("key.path", defaultValue)

// Compile to binary
err = config.CompileToBinary("config.peanuts", "config.pnt")

// Watch for changes
err = config.Watch(func(newConfig *peanut.PeanutConfig) {
    // Handle changes
})

// Struct mapping
var cfg MyConfig
err = config.Unmarshal(&cfg)

// Thread-safe access
safeConfig := NewSafeConfig()
value := safeConfig.Get("key", defaultValue)
```

### Configuration File Format

```ini
[section]
key: "value"
number: 42
boolean: true
array: ["item1", "item2", "item3"]

[subsection]
nested_key: "nested_value"
```

### CLI Usage

```bash
# Compile configuration to binary
tsk peanuts compile config.peanuts

# Load and display binary file
tsk peanuts load config.pnt

# Validate configuration
tsk config validate .

# Get configuration value
tsk config get server.port
```

### Performance Tips

1. **Use binary format (.pnt) in production**
2. **Cache configuration objects**
3. **Implement proper error handling**
4. **Use struct mapping for type safety**
5. **Consider thread safety in concurrent apps**
6. **Disable file watching in production**

---

This guide provides everything you need to use Peanut Configuration effectively with Go. For more information, refer to the [TuskLang documentation](https://tusklang.org) or the [Go SDK repository](https://github.com/cyber-boost/go-sdk). 