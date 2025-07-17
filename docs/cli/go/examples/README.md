# Examples

Practical examples and use cases for TuskLang Go CLI.

## Available Examples

| Example | Description |
|---------|-------------|
| [Basic Usage](./basic-usage.md) | Common usage patterns |
| [Workflows](./workflows.md) | Complete development workflows |
| [Integrations](./integrations.md) | Framework integrations |

## Quick Start Examples

### Web Application

```bash
# Initialize project
tsk init

# Start development server
tsk serve 8080

# Run tests
tsk test all

# Compile configuration
tsk peanuts compile peanu.peanuts
```

### Microservice

```bash
# Check database status
tsk db status

# Run migrations
tsk db migrate migrations/

# Start services
tsk services start

# Monitor configuration
tsk config check .
```

### CLI Tool

```bash
# Parse configuration
tsk parse config.tsk

# Validate syntax
tsk validate app.tsk

# Optimize for production
tsk optimize config.tsk

# Compile to binary
tsk compile app.tsk
```

## Go Integration Examples

### Configuration Management

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

    // Access values
    port := config.GetInt("server.port", 3000)
    fmt.Printf("Server port: %d\n", port)
}
```

### Struct Mapping

```go
type AppConfig struct {
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
}
```

## Framework Integrations

### Gin Web Framework

```go
package main

import (
    "github.com/gin-gonic/gin"
    "github.com/tusklang/go-sdk/peanut"
)

func main() {
    config, err := peanut.Load(".")
    if err != nil {
        log.Fatal(err)
    }

    if config.GetBool("server.debug", false) {
        gin.SetMode(gin.DebugMode)
    }

    r := gin.Default()
    port := config.GetInt("server.port", 3000)
    r.Run(fmt.Sprintf(":%d", port))
}
```

### GORM Database

```go
package main

import (
    "gorm.io/gorm"
    "gorm.io/driver/postgres"
    "github.com/tusklang/go-sdk/peanut"
)

func main() {
    config, err := peanut.Load(".")
    if err != nil {
        log.Fatal(err)
    }

    dsn := fmt.Sprintf("host=%s port=%d user=%s password=%s dbname=%s",
        config.GetString("database.host", "localhost"),
        config.GetInt("database.port", 5432),
        config.GetString("database.user", "postgres"),
        config.GetString("database.password", ""),
        config.GetString("database.name", "myapp"))

    db, err := gorm.Open(postgres.Open(dsn), &gorm.Config{})
    if err != nil {
        log.Fatal(err)
    }
}
```

## Best Practices

### Configuration Management

1. **Use Binary Format in Production**
   ```bash
   tsk peanuts compile config.peanuts
   ```

2. **Implement Type Safety**
   ```go
   var cfg AppConfig
   config.Unmarshal(&cfg)
   ```

3. **Use Hierarchical Configuration**
   ```ini
   # Global config
   [app]
   name: "My App"
   
   # Environment-specific overrides
   [server]
   port: 8080
   ```

### Development Workflow

1. **Start with Development Server**
   ```bash
   tsk serve --hot-reload 3000
   ```

2. **Run Tests Regularly**
   ```bash
   tsk test all --coverage
   ```

3. **Validate Configuration**
   ```bash
   tsk config validate .
   ```

4. **Compile for Production**
   ```bash
   tsk peanuts compile config.peanuts
   tsk optimize config.tsk
   ```

## Troubleshooting

### Common Issues

1. **Configuration Not Found**
   ```bash
   tsk config check .
   tsk init
   ```

2. **Database Connection Failed**
   ```bash
   tsk db status
   tsk db init
   ```

3. **Compilation Errors**
   ```bash
   tsk validate config.tsk
   tsk parse config.tsk
   ```

### Getting Help

```bash
# General help
tsk help

# Command-specific help
tsk config help
tsk db help

# Verbose output
tsk --verbose config get server.port
``` 