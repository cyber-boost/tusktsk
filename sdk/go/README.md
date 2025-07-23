# TuskLang Go SDK

A high-performance Go implementation of the TuskLang SDK with advanced features for parsing, executing, and optimizing TuskLang code.

[![Go Version](https://img.shields.io/badge/go-%3E%3D1.21-blue)](https://go.dev/)
[![Documentation](https://img.shields.io/badge/docs-tusklang.org-orange)](https://docs.tusklang.org)

## Overview

The TuskLang Go SDK provides a comprehensive toolkit for working with TuskLang, featuring:

- **High Performance**: Optimized execution with JIT compilation and multi-level caching
- **Multiple Database Support**: Built-in adapters for SQLite, PostgreSQL, MySQL, MongoDB, and Redis
- **Rich Operator Set**: 104+ operators for data manipulation, logic, and calculations
- **CLI Tools**: 42 commands for development, deployment, and management
- **Web Framework**: Built-in HTTP server with REST, WebSocket, and GraphQL support
- **Enterprise Ready**: Security framework, monitoring, and scalability features

## Table of Contents

- [Installation](#installation)
- [Quick Start](#quick-start)
- [Core Features](#core-features)
- [CLI Reference](#cli-reference)
- [Operators](#operators)
- [Database Support](#database-support)
- [Web Framework](#web-framework)
- [Performance](#performance)
- [Configuration](#configuration)
- [Examples](#examples)
- [Contributing](#contributing)
- [License](#license)

## Installation

```bash
go get github.com/cyber-boost/tusktsk
```

**Requirements:**
- Go 1.21 or higher
- Optional: Redis, Memcached for advanced caching features

## Quick Start

### Basic Usage

```go
package main

import (
    "fmt"
    "log"
    "github.com/cyber-boost/tusktsk/pkg/core"
)

func main() {
    // Initialize the SDK
    sdk := core.New()
    
    // Parse TuskLang code
    code := `@if(@env("DEBUG"), "Debug mode", "Production mode")`
    result, err := sdk.Parse(code)
    if err != nil {
        log.Fatal(err)
    }
    
    // Execute parsed code
    output, err := sdk.Execute(result)
    if err != nil {
        log.Fatal(err)
    }
    
    fmt.Println(output)
}
```

### CLI Quick Start

```bash
# Install the CLI tool
go install github.com/cyber-boost/tusktsk/cmd/tsk@latest

# Parse a TuskLang file
tsk parse myfile.tsk

# Start development server
tsk dev serve

# Run with AI assistance
tsk ai analyze myfile.tsk
```

## Core Features

### Parser and Executor

The SDK provides a robust parser and executor for TuskLang code:

```go
// Initialize with configuration
config := &core.Config{
    EnableCache: true,
    EnableJIT:   true,
}
sdk := core.NewWithConfig(config)

// Parse with context
ctx := core.NewContext()
ctx.Set("user", map[string]interface{}{
    "name": "John Doe",
    "role": "admin",
})

result, err := sdk.ParseWithContext(code, ctx)
```

### Performance Optimization

Enable various performance features:

```go
import "github.com/cyber-boost/tusktsk/pkg/performance"

// Configure performance framework
perf := performance.NewFramework(&performance.FrameworkConfig{
    JITEnabled:       true,
    CacheEnabled:     true,
    MemoryPooling:    true,
    ProfilingEnabled: true,
})

// Apply optimizations
sdk.WithPerformance(perf)
```

## CLI Reference

The TuskLang CLI provides 42 commands organized into categories:

### AI Integration
```bash
tsk ai claude <prompt>      # Claude AI integration
tsk ai chatgpt <prompt>     # ChatGPT integration  
tsk ai analyze <file>       # Analyze code with AI
tsk ai optimize <file>      # Get optimization suggestions
```

### Development Tools
```bash
tsk dev serve              # Start development server
tsk dev compile <file>     # Compile TuskLang files
tsk dev watch <path>       # Watch for file changes
```

### Database Management
```bash
tsk db status              # Check database status
tsk db migrate             # Run migrations
tsk db console             # Open database console
tsk db backup <file>       # Create backup
```

### Web Server
```bash
tsk web start              # Start web server
tsk web status             # Check server status
tsk web logs               # View server logs
```

[View Full CLI Documentation →](https://docs.tusklang.org/cli)

## Operators

TuskLang provides 104+ operators for various operations:

### Variable and Data Access
- `@variable` - Variable references with fallback support
- `@env` - Environment variable access
- `@request` - HTTP request data
- `@session` - Session management
- `@cookie` - Cookie operations

### String and Data Manipulation
- `@string` - String operations
- `@regex` - Regular expressions
- `@json` - JSON parsing/encoding
- `@base64` - Base64 encoding/decoding
- `@hash` - Hashing functions

### Logic and Control Flow
- `@if` - Conditional expressions
- `@switch` - Switch statements  
- `@and`, `@or`, `@not` - Logical operations

### Date and Time
- `@date` - Date formatting
- `@time` - Time operations
- `@now` - Current timestamp
- `@timezone` - Timezone conversions

[View Complete Operator Reference →](https://docs.tusklang.org/operators)

## Database Support

### Supported Databases

The SDK includes built-in support for multiple databases:

```go
import "github.com/cyber-boost/tusktsk/pkg/database"

// SQLite
db, err := database.Connect("sqlite:/path/to/database.db")

// PostgreSQL
db, err := database.Connect("postgresql://user:pass@host:port/dbname")

// MySQL
db, err := database.Connect("mysql://user:pass@host:port/dbname")

// MongoDB
db, err := database.Connect("mongodb://user:pass@host:port/dbname")

// Redis
db, err := database.Connect("redis://host:port")
```

### ORM Features

```go
// Define models
type User struct {
    ID        int       `tusk:"primary_key"`
    Name      string    `tusk:"not_null"`
    Email     string    `tusk:"unique"`
    CreatedAt time.Time `tusk:"auto_now_add"`
}

// Query builder
users, err := db.Query("users").
    Where("role", "=", "admin").
    OrderBy("created_at", "DESC").
    Limit(10).
    Find(&[]User{})

// Relationships
db.Model(&User{}).HasMany(&Post{}, "user_id")
```

## Web Framework

### HTTP Server

```go
import "github.com/cyber-boost/tusktsk/pkg/web"

// Create server
server := web.NewServer(&web.Config{
    Port: 8080,
    Host: "localhost",
})

// Add routes
server.GET("/api/users", handleUsers)
server.POST("/api/users", createUser)

// Middleware
server.Use(web.Logger())
server.Use(web.RateLimiter(100)) // 100 req/min
server.Use(web.Auth())

// Start server
server.Start()
```

### WebSocket Support

```go
server.WebSocket("/ws", func(conn *web.WSConn) {
    conn.On("message", func(data []byte) {
        // Handle message
        conn.Send("response", responseData)
    })
})
```

### GraphQL Server

```go
server.GraphQL("/graphql", &web.GraphQLConfig{
    Schema:     schema,
    Playground: true,
})
```

## Performance

### Benchmarks

Performance characteristics based on internal benchmarks:

| Operation | Performance | Notes |
|-----------|------------|-------|
| Parse (1KB) | ~200μs | With caching enabled |
| Execute (simple) | ~50μs | Basic operator execution |
| Database Query | ~5ms | PostgreSQL with connection pooling |
| HTTP Request | ~2ms | Including middleware |
| Cache Hit | ~100ns | In-memory L1 cache |

### Optimization Strategies

1. **JIT Compilation**: Frequently executed code is compiled for better performance
2. **Multi-Level Caching**: L1 (in-memory), L2 (Memcached), L3 (Redis)
3. **Connection Pooling**: Database connections are pooled and reused
4. **Goroutine Pools**: Concurrent operations use worker pools

## Configuration

### Configuration File

Create a `tusklang.yaml` file:

```yaml
# General settings
debug: false
log_level: info

# Performance settings
performance:
  jit_enabled: true
  cache_enabled: true
  max_goroutines: 100

# Database settings
database:
  default: postgresql
  connections:
    postgresql:
      url: "postgresql://localhost/tusklang"
      pool_size: 10

# Web server settings
web:
  port: 8080
  rate_limit: 100
  enable_cors: true
```

### Environment Variables

```bash
TUSK_DEBUG=true
TUSK_DATABASE_URL=postgresql://localhost/tusklang
TUSK_CACHE_REDIS=redis://localhost:6379
TUSK_WEB_PORT=8080
```

## Examples

### REST API Server

```go
func main() {
    // Initialize SDK and web server
    sdk := core.New()
    server := web.NewServer(&web.Config{Port: 8080})
    
    // API endpoint that uses TuskLang
    server.POST("/api/process", func(ctx *web.Context) {
        code := ctx.Body("code")
        
        result, err := sdk.Parse(code)
        if err != nil {
            ctx.JSON(400, map[string]string{"error": err.Error()})
            return
        }
        
        output, err := sdk.Execute(result)
        if err != nil {
            ctx.JSON(500, map[string]string{"error": err.Error()})
            return
        }
        
        ctx.JSON(200, map[string]interface{}{"result": output})
    })
    
    server.Start()
}
```

### Batch Processing

```go
func processBatch(files []string) error {
    sdk := core.New()
    
    // Process files concurrently
    results := make(chan string, len(files))
    errors := make(chan error, len(files))
    
    for _, file := range files {
        go func(f string) {
            content, err := os.ReadFile(f)
            if err != nil {
                errors <- err
                return
            }
            
            result, err := sdk.Parse(string(content))
            if err != nil {
                errors <- err
                return
            }
            
            output, err := sdk.Execute(result)
            if err != nil {
                errors <- err
                return
            }
            
            results <- output
        }(file)
    }
    
    // Collect results
    for i := 0; i < len(files); i++ {
        select {
        case result := <-results:
            fmt.Println(result)
        case err := <-errors:
            return err
        }
    }
    
    return nil
}
```

[View More Examples →](https://github.com/cyber-boost/tusktsk/tree/main/examples)

## Architecture

```
tusktsk/
├── cmd/
│   └── tsk/              # CLI application
├── pkg/
│   ├── core/             # Core SDK functionality
│   ├── operators/        # Operator implementations
│   ├── database/         # Database adapters and ORM
│   ├── web/              # Web framework
│   ├── performance/      # Performance optimizations
│   ├── security/         # Security features
│   └── config/           # Configuration management
├── examples/             # Example applications
├── docs/                 # Documentation
└── tests/                # Test suites
```

## Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

### Development Setup

```bash
# Clone the repository
git clone https://github.com/cyber-boost/tusktsk.git
cd tusktsk

# Install dependencies
go mod download

# Run tests
go test ./...

# Run benchmarks
go test -bench=. ./...

# Build CLI
go build -o tsk cmd/tsk/main.go
```

## Community

- **Documentation**: [https://docs.tusklang.org](https://docs.tusklang.org)
- **GitHub**: [https://github.com/cyber-boost/tusktsk](https://github.com/cyber-boost/tusktsk)
- **Discord**: [https://discord.gg/tusklang](https://discord.gg/tusklang)
- **Email**: hello@tusklang.org

## License

Please visit www.tuskt.sk for more information * by using this software you agree to the license available at tuskt.sk 

---

Built with ❤️ by the TuskLang community