# 🥜 TuskLang Enhanced for Go

**The Freedom Configuration Language - "We don't bow to any king"**

TuskLang Enhanced brings flexible syntax and intelligent configuration to Go. With support for multiple grouping styles, global variables, cross-file communication, and database queries right in your config files.

[![Go Reference](https://pkg.go.dev/badge/github.com/cyber-boost/tusktsk.svg)](https://pkg.go.dev/github.com/cyber-boost/tusktsk)
[![Go Version](https://img.shields.io/badge/go-1.19+-blue.svg)](https://golang.org/)
[![License: Proprietary](https://img.shields.io/badge/License-Proprietary-red.svg)](https://tuskt.sk/license)
[![Documentation](https://img.shields.io/badge/docs-tuskt.sk-green.svg)](https://tuskt.sk/docs)

## 🚀 Installation

### Via go get (Recommended)
```bash
go get github.com/cyber-boost/tusktsk
```

### Via go.mod
```go
require github.com/cyber-boost/tusktsk v2.0.1
```

### Manual Installation
```bash
git clone https://github.com/cyber-boost/tusktsk
cd tusktsk/sdk/go
go build -o tusktsk .
```

## ✨ Features

- **Flexible Syntax**: Use `[]`, `{}`, or `<>` grouping - your choice!
- **Global Variables**: `$variables` accessible across all sections
- **Cross-File Communication**: `@file.tsk.get()` and `@file.tsk.set()`
- **Database Queries**: Query databases directly in config files
- **Environment Variables**: `@env("VAR", "default")` with defaults
- **Date Functions**: `@date("Y-m-d H:i:s")` with Go formatting
- **Conditional Expressions**: `condition ? true_val : false_val`
- **Range Syntax**: `8000-9000` notation
- **peanut.tsk Integration**: Universal configuration file support
- **Fujsen Support**: Function serialization and execution
- **Concurrent safe**: Thread-safe configuration access
- **High performance**: Optimized for Go's runtime

## 📖 Quick Start

### Basic Usage
```go
package main

import (
    "fmt"
    "log"
    "github.com/cyber-boost/tusktsk"
)

func main() {
    // Parse a configuration file
    parser := tusktsk.NewParser()
    config, err := parser.ParseFile("config.tsk")
    if err != nil {
        log.Fatal(err)
    }

    // Get values
    dbHost, err := parser.Get("database.host")
    if err != nil {
        log.Fatal(err)
    }
    
    serverPort, err := parser.Get("server.port")
    if err != nil {
        log.Fatal(err)
    }

    // Or use TSK struct
    tsk := tusktsk.New()
    err = tsk.LoadFromFile("config.tsk")
    if err != nil {
        log.Fatal(err)
    }
    
    dbHost, err = tsk.GetValue("database", "host")
    if err != nil {
        log.Fatal(err)
    }

    fmt.Printf("Database host: %s\n", dbHost)
    fmt.Printf("Server port: %v\n", serverPort)
}
```

### Example Configuration
```tsk
# Global variables
$app_name: "My Application"
$environment: @env("APP_ENV", "development")

# Traditional sections
[database]
host: "localhost"
port: 5432

# Curly brace objects
server {
    host: @env("SERVER_HOST", "0.0.0.0")
    port: @env("SERVER_PORT", 8080)
    workers: $environment == "production" ? 4 : 1
}

# Angle bracket objects
cache >
    driver: "redis"
    ttl: "5m"
    enabled: true
<

# Database queries
[stats]
user_count: @query("SELECT COUNT(*) FROM users")
active_sessions: @query("SELECT COUNT(*) FROM sessions WHERE active = 1")

# Conditional expressions
[scaling]
instances: $environment == "production" ? 10 : 2
log_level: $environment == "production" ? "error" : "debug"

# Arrays and ranges
[config]
allowed_ips: ["127.0.0.1", "192.168.1.0/24"]
port_range: 8000-9000
```

## 🔧 CLI Tool

The package includes a command-line tool for working with TuskLang files:

```bash
# Install CLI globally
go install github.com/cyber-boost/tusktsk/cmd/tusktsk@latest

# Parse a file
tusktsk parse config.tsk

# Get a specific value
tusktsk get config.tsk database.host

# List all keys
tusktsk keys config.tsk

# Load from peanut.tsk
tusktsk peanut

# Validate syntax
tusktsk validate config.tsk
```

## 🥜 peanut.tsk - Universal Configuration

TuskLang Enhanced automatically looks for `peanut.tsk` in standard locations:
- `./peanut.tsk`
- `../peanut.tsk`
- `/etc/tusktsk/peanut.tsk`
- `~/.config/tusktsk/peanut.tsk`
- `$TUSKTSK_CONFIG` environment variable

This universal configuration file provides default settings for database connections, caching, and other common configuration needs.

## 💾 Database Integration

TuskLang Enhanced supports database queries directly in configuration files:

```go
package main

import (
    "github.com/cyber-boost/tusktsk"
)

func main() {
    // Setup database connection
    tsk := tusktsk.New()
    err := tsk.LoadPeanut() // Loads database config from peanut.tsk
    if err != nil {
        log.Fatal(err)
    }

    // Use @query() in your .tsk files
    config, err := tsk.ParseFile("app.tsk")
    if err != nil {
        log.Fatal(err)
    }
}
```

Supported databases:
- SQLite (via modernc.org/sqlite)
- PostgreSQL (via lib/pq)
- MySQL/MariaDB (via go-sql-driver/mysql)
- MongoDB (via mongo-go-driver)

## 🌍 System Integration

TuskLang Enhanced can reference system-installed TuskLang tools:
- `/usr/local/bin/tusk` - System CLI
- `/usr/local/lib/tusktsk` - System libraries
- `/usr/bin/tusk` - Alternative installation location

## 🚀 Go-Specific Features

### Concurrent Access
```go
// Thread-safe configuration access
config := tusktsk.New()
config.SetMutex(&sync.RWMutex{})

// Safe concurrent reads
go func() {
    value, _ := config.GetValue("database", "host")
    fmt.Println("Host:", value)
}()

go func() {
    value, _ := config.GetValue("server", "port")
    fmt.Println("Port:", value)
}()
```

### Context Support
```go
ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
defer cancel()

// Parse with context
config, err := parser.ParseFileWithContext(ctx, "config.tsk")
if err != nil {
    log.Fatal(err)
}
```

### Error Handling
```go
// Rich error types
config, err := parser.ParseFile("config.tsk")
if err != nil {
    switch e := err.(type) {
    case *tusktsk.ParseError:
        fmt.Printf("Parse error at line %d: %s\n", e.Line, e.Message)
    case *tusktsk.ValidationError:
        fmt.Printf("Validation error: %s\n", e.Message)
    default:
        fmt.Printf("Unknown error: %v\n", err)
    }
    return
}
```

## 📚 Documentation

For complete documentation and examples:
- [Official Documentation](https://tuskt.sk/docs)
- [GitHub Repository](https://github.com/cyber-boost/tusktsk)
- [Go Package Documentation](https://pkg.go.dev/github.com/cyber-boost/tusktsk)

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

## 📄 License

Proprietary License - see [LICENSE](https://tuskt.sk/license) for details.

---

**"We don't bow to any king"** - TuskLang gives developers the freedom to choose their preferred syntax while maintaining intelligent configuration capabilities. 