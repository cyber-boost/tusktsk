# 🚀 TuskLang Go Quick Start Guide

**"We don't bow to any king" - Go Edition**

Get up and running with TuskLang in Go in under 5 minutes. This guide will show you how to create your first TSK configuration, parse it with type-safe structs, and integrate with databases.

## ⚡ 5-Minute Quick Start

### Step 1: Install TuskLang Go SDK

```bash
# Install the SDK
go get github.com/tusklang/go

# Install CLI tool
go install github.com/tusklang/go/cmd/tusk@latest
```

### Step 2: Create Your First TSK File

```go
// config.tsk
[app]
name: "My Awesome App"
version: "1.0.0"
debug: true

[server]
host: "localhost"
port: 8080
ssl: false

[database]
host: "localhost"
port: 5432
name: "myapp"
user: "postgres"
password: @env("DB_PASSWORD", "secret")
```

### Step 3: Parse with Type-Safe Structs

```go
// main.go
package main

import (
    "fmt"
    "github.com/tusklang/go"
)

// Define your configuration struct
type Config struct {
    App struct {
        Name    string `tsk:"name"`
        Version string `tsk:"version"`
        Debug   bool   `tsk:"debug"`
    } `tsk:"app"`
    
    Server struct {
        Host string `tsk:"host"`
        Port int    `tsk:"port"`
        SSL  bool   `tsk:"ssl"`
    } `tsk:"server"`
    
    Database struct {
        Host     string `tsk:"host"`
        Port     int    `tsk:"port"`
        Name     string `tsk:"name"`
        User     string `tsk:"user"`
        Password string `tsk:"password"`
    } `tsk:"database"`
}

func main() {
    // Create parser
    parser := tusklanggo.NewEnhancedParser()
    
    // Parse TSK file
    data, err := parser.ParseFile("config.tsk")
    if err != nil {
        panic(err)
    }
    
    // Unmarshal into struct
    var config Config
    err = tusklanggo.UnmarshalTSK(data, &config)
    if err != nil {
        panic(err)
    }
    
    // Use your configuration
    fmt.Printf("Starting %s v%s on %s:%d\n", 
        config.App.Name, config.App.Version, 
        config.Server.Host, config.Server.Port)
    
    if config.App.Debug {
        fmt.Printf("Database: %s:%d/%s\n", 
            config.Database.Host, config.Database.Port, config.Database.Name)
    }
}
```

### Step 4: Run Your Application

```bash
# Set environment variable
export DB_PASSWORD="mysecretpassword"

# Run the application
go run main.go
```

**Output:**
```
Starting My Awesome App v1.0.0 on localhost:8080
Database: localhost:5432/myapp
```

## 🎯 Core Concepts in 5 Minutes

### 1. Multiple Syntax Styles

TuskLang supports your preferred syntax style:

```go
// Traditional INI-style
[section]
key: "value"

// JSON-like objects
section {
    key: "value"
}

// XML-inspired syntax
section >
    key: "value"
<
```

### 2. Environment Variables

```go
// config.tsk
[secrets]
api_key: @env("API_KEY")
database_url: @env("DATABASE_URL", "postgres://localhost/myapp")
debug_mode: @env("DEBUG", "false")
```

### 3. Database Queries in Config

```go
// config.tsk
[stats]
user_count: @query("SELECT COUNT(*) FROM users")
active_users: @query("SELECT COUNT(*) FROM users WHERE active = 1")
recent_orders: @query("SELECT * FROM orders WHERE created_at > ?", @date.subtract("7d"))
```

### 4. Conditional Logic

```go
// config.tsk
$environment: @env("APP_ENV", "development")

[logging]
level: @if($environment == "production", "error", "debug")
file: @if($environment == "production", "/var/log/app.log", "console")

[security]
ssl: @if($environment == "production", true, false)
```

## 🔧 Advanced Quick Start

### Database Integration

```go
package main

import (
    "fmt"
    "github.com/tusklang/go"
    "github.com/tusklang/go/adapters"
)

func main() {
    // Create database adapter
    sqlite, err := adapters.NewSQLiteAdapter("app.db")
    if err != nil {
        panic(err)
    }
    
    // Create parser with database
    parser := tusklanggo.NewEnhancedParser()
    parser.SetDatabaseAdapter(sqlite)
    
    // TSK with database queries
    tskContent := `
[app]
name: "Database App"

[stats]
total_users: @query("SELECT COUNT(*) FROM users")
active_users: @query("SELECT COUNT(*) FROM users WHERE active = 1")
`
    
    data, err := parser.ParseString(tskContent)
    if err != nil {
        panic(err)
    }
    
    fmt.Printf("Total users: %v\n", data["stats"].(map[string]interface{})["total_users"])
}
```

### Cross-File Communication

```go
// main.tsk
$app_name: "My App"
$version: "1.0.0"

[database]
host: @config.tsk.get("db_host")
port: @config.tsk.get("db_port")
```

```go
// config.tsk
db_host: "localhost"
db_port: 5432
db_name: "myapp"
```

```go
// main.go
package main

import (
    "fmt"
    "github.com/tusklang/go"
)

func main() {
    parser := tusklanggo.NewEnhancedParser()
    
    // Link configuration files
    parser.LinkFile("config.tsk", `
db_host: "localhost"
db_port: 5432
db_name: "myapp"
`)
    
    data, err := parser.ParseFile("main.tsk")
    if err != nil {
        panic(err)
    }
    
    fmt.Printf("Database: %s:%v\n", 
        data["database"].(map[string]interface{})["host"],
        data["database"].(map[string]interface{})["port"])
}
```

### Executable Functions (FUJSEN)

```go
// config.tsk
[processing]
calculate_tax: """
function calculate(amount, rate) {
    return amount * (rate / 100);
}
"""

[order]
amount: 100.00
tax_rate: 8.5
tax_amount: @fujsen(calculate_tax, @order.amount, @order.tax_rate)
```

```go
// main.go
package main

import (
    "fmt"
    "github.com/tusklang/go"
)

func main() {
    parser := tusklanggo.NewEnhancedParser()
    
    data, err := parser.ParseFile("config.tsk")
    if err != nil {
        panic(err)
    }
    
    order := data["order"].(map[string]interface{})
    fmt.Printf("Order: $%.2f + $%.2f tax = $%.2f\n",
        order["amount"], order["tax_amount"], 
        order["amount"].(float64) + order["tax_amount"].(float64))
}
```

## 🚀 Web Framework Integration

### Gin Framework

```go
package main

import (
    "github.com/gin-gonic/gin"
    "github.com/tusklang/go"
)

type AppConfig struct {
    Server struct {
        Port int    `tsk:"port"`
        Host string `tsk:"host"`
    } `tsk:"server"`
    
    Database struct {
        Host string `tsk:"host"`
        Port int    `tsk:"port"`
        Name string `tsk:"name"`
    } `tsk:"database"`
}

func main() {
    // Load configuration
    parser := tusklanggo.NewEnhancedParser()
    data, err := parser.ParseFile("config.tsk")
    if err != nil {
        panic(err)
    }
    
    var config AppConfig
    err = tusklanggo.UnmarshalTSK(data, &config)
    if err != nil {
        panic(err)
    }
    
    // Create Gin router
    r := gin.Default()
    
    r.GET("/", func(c *gin.Context) {
        c.JSON(200, gin.H{
            "message": "Hello from TuskLang + Gin!",
            "config":  config,
        })
    })
    
    // Start server
    r.Run(fmt.Sprintf("%s:%d", config.Server.Host, config.Server.Port))
}
```

### Echo Framework

```go
package main

import (
    "github.com/labstack/echo/v4"
    "github.com/tusklang/go"
)

func main() {
    // Load configuration
    parser := tusklanggo.NewEnhancedParser()
    data, err := parser.ParseFile("config.tsk")
    if err != nil {
        panic(err)
    }
    
    var config AppConfig
    err = tusklanggo.UnmarshalTSK(data, &config)
    if err != nil {
        panic(err)
    }
    
    // Create Echo instance
    e := echo.New()
    
    e.GET("/", func(c echo.Context) error {
        return c.JSON(200, map[string]interface{}{
            "message": "Hello from TuskLang + Echo!",
            "config":  config,
        })
    })
    
    // Start server
    e.Start(fmt.Sprintf("%s:%d", config.Server.Host, config.Server.Port))
}
```

## 🔍 CLI Quick Commands

```bash
# Parse TSK file
tusk parse config.tsk

# Validate syntax
tusk validate config.tsk

# Convert to JSON
tusk convert config.tsk --format json

# Generate Go structs
tusk generate --type go config.tsk

# Interactive shell
tusk shell config.tsk
```

## 📊 Performance Quick Test

```go
package main

import (
    "fmt"
    "time"
    "github.com/tusklang/go"
)

func main() {
    parser := tusklanggo.NewEnhancedParser()
    
    // Performance test
    start := time.Now()
    
    for i := 0; i < 1000; i++ {
        _, err := parser.ParseString(`
[test]
value: "quick test"
count: ${i}
`)
        if err != nil {
            panic(err)
        }
    }
    
    duration := time.Since(start)
    fmt.Printf("Performance: %d parses in %v (%.2f parses/sec)\n", 
        1000, duration, float64(1000)/duration.Seconds())
}
```

## 🎯 What You've Learned

In this quick start, you've learned:

1. **Installation** - How to install TuskLang Go SDK
2. **Basic Parsing** - Parse TSK files into Go structs
3. **Environment Variables** - Use @env() for configuration
4. **Database Integration** - Execute queries in configuration
5. **Cross-File Communication** - Link multiple TSK files
6. **Executable Functions** - Use FUJSEN for dynamic logic
7. **Web Framework Integration** - Use with Gin and Echo
8. **CLI Tools** - Command-line utilities for TSK files

## 🚀 Next Steps

Now that you're up and running:

1. **Explore @ Operators** - Learn about @date, @cache, @metrics, etc.
2. **Advanced Database Features** - Complex queries and relationships
3. **Security Features** - Encryption, validation, and secure environment variables
4. **Performance Optimization** - Caching and optimization strategies
5. **Deployment** - Docker, Kubernetes, and cloud deployment

## 📚 Resources

- **Full Documentation**: [docs.tusklang.org/go](https://docs.tusklang.org/go)
- **Examples Repository**: [github.com/tusklang/go/examples](https://github.com/tusklang/go/examples)
- **Community**: [community.tusklang.org](https://community.tusklang.org)
- **CLI Reference**: [cli.tusklang.org](https://cli.tusklang.org)

---

**"We don't bow to any king"** - You're now ready to build powerful, type-safe applications with TuskLang in Go. Configuration with a heartbeat, indeed! 