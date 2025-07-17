# Quick Start Guide

Get up and running with TuskLang Go CLI in minutes.

## Prerequisites

- Go 1.21 or higher
- Git
- Basic knowledge of command line

## Installation

```bash
# Clone the repository
git clone https://github.com/tusklang/go-sdk.git
cd go-sdk

# Install dependencies and build CLI
go mod tidy
make install

# Verify installation
tsk version
```

## Your First Project

### 1. Create Project Structure

```bash
# Create project directory
mkdir my-tusklang-app
cd my-tusklang-app

# Initialize TuskLang project
tsk init
```

### 2. Create Configuration

Create `peanu.peanuts`:

```ini
[app]
name: "My TuskLang App"
version: "1.0.0"

[server]
host: "localhost"
port: 8080
debug: true

[database]
host: "localhost"
port: 5432
name: "myapp"
user: "postgres"
password: ""
```

### 3. Initialize Database

```bash
# Initialize SQLite database
tsk db init

# Check database status
tsk db status
```

### 4. Start Development Server

```bash
# Start development server
tsk serve 8080
```

### 5. Run Tests

```bash
# Run all tests
tsk test all

# Run specific test suite
tsk test parser
```

## Basic Commands

### Configuration Management

```bash
# Get configuration value
tsk config get server.port

# Check configuration hierarchy
tsk config check .

# Validate configuration
tsk config validate .

# Compile to binary for production
tsk peanuts compile peanu.peanuts
```

### Database Operations

```bash
# Check database status
tsk db status

# Run migrations
tsk db migrate schema.sql

# Interactive console
tsk db console

# Create backup
tsk db backup myapp_backup.sql
```

### Development Tools

```bash
# Start development server
tsk serve 3000

# Compile TSK file
tsk compile config.tsk

# Optimize configuration
tsk optimize config.tsk
```

### Testing

```bash
# Run all tests
tsk test all

# Test parser only
tsk test parser

# Performance tests
tsk test performance
```

## Go Integration

### Using in Go Code

```go
package main

import (
    "fmt"
    "log"
    "github.com/tusklang/go-sdk/peanut"
)

func main() {
    // Load configuration
    config, err := peanut.Load(".")
    if err != nil {
        log.Fatal(err)
    }

    // Access configuration values
    appName := config.GetString("app.name", "default")
    port := config.GetInt("server.port", 3000)
    
    fmt.Printf("Starting %s on port %d\n", appName, port)
}
```

### Struct Mapping

```go
type AppConfig struct {
    App struct {
        Name    string `peanut:"name"`
        Version string `peanut:"version"`
    } `peanut:"app"`
    Server struct {
        Host string `peanut:"host"`
        Port int    `peanut:"port"`
    } `peanut:"server"`
}

func main() {
    config, err := peanut.Load(".")
    if err != nil {
        log.Fatal(err)
    }

    var cfg AppConfig
    err = config.Unmarshal(&cfg)
    if err != nil {
        log.Fatal(err)
    }

    fmt.Printf("App: %s v%s\n", cfg.App.Name, cfg.App.Version)
}
```

## Common Workflows

### Development Workflow

```bash
# 1. Start development
tsk serve 8080

# 2. Make changes to configuration
# Edit peanu.peanuts

# 3. Test changes
tsk test all

# 4. Compile for production
tsk peanuts compile peanu.peanuts
```

### Database Workflow

```bash
# 1. Initialize database
tsk db init

# 2. Run migrations
tsk db migrate migrations/*.sql

# 3. Check status
tsk db status

# 4. Create backup
tsk db backup backup.sql
```

### Testing Workflow

```bash
# 1. Run all tests
tsk test all

# 2. Run specific tests
tsk test parser
tsk test fujsen

# 3. Performance testing
tsk test performance

# 4. Generate test report
tsk test --report test-report.json
```

## Configuration Examples

### Web Application

```ini
[app]
name: "Web App"
version: "1.0.0"

[server]
host: "0.0.0.0"
port: 8080
timeout: 30
max_connections: 1000

[database]
host: "localhost"
port: 5432
name: "webapp"
user: "postgres"
password: ""
max_open_conns: 25

[redis]
host: "localhost"
port: 6379
db: 0

[logging]
level: "info"
format: "json"
output: "stdout"
```

### Microservice

```ini
[service]
name: "user-service"
version: "1.0.0"
environment: "development"

[server]
host: "0.0.0.0"
port: 8080
timeout: 30

[database]
host: "localhost"
port: 5432
name: "users"
user: "postgres"
password: ""

[monitoring]
enabled: true
port: 9090
metrics_path: "/metrics"
```

## Troubleshooting

### Common Issues

1. **Command not found**
   ```bash
   # Check if tsk is installed
   which tsk
   
   # Reinstall if needed
   make install
   ```

2. **Configuration not found**
   ```bash
   # Check if peanu.peanuts exists
   ls -la peanu.peanuts
   
   # Create if missing
   tsk init
   ```

3. **Database connection failed**
   ```bash
   # Check database status
   tsk db status
   
   # Initialize if needed
   tsk db init
   ```

### Getting Help

```bash
# General help
tsk help

# Command-specific help
tsk db help
tsk config help

# Verbose output
tsk --verbose db status
```

## Next Steps

1. **Explore Commands**: Check out the [Command Reference](./commands/README.md)
2. **Read Documentation**: See the [PNT Guide](../go/docs/PNT_GUIDE.md)
3. **Try Examples**: Look at [Examples](./examples/README.md)
4. **Join Community**: Visit [TuskLang Community](https://tusklang.org/community)

## Support

- **Documentation**: [TuskLang Docs](https://tusklang.org/docs)
- **GitHub**: [Go SDK Repository](https://github.com/tusklang/go-sdk)
- **Issues**: [GitHub Issues](https://github.com/tusklang/go-sdk/issues)
- **Discussions**: [GitHub Discussions](https://github.com/tusklang/go-sdk/discussions) 