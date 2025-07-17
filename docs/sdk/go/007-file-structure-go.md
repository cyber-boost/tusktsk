# 📁 TuskLang Go File Structure Guide

**"We don't bow to any king" - Go Edition**

Organize your TuskLang Go projects with a clean, scalable file structure that follows Go best practices and TuskLang conventions. This guide shows you how to structure projects from simple applications to complex enterprise systems.

## 🏗️ Project Structure Overview

### Basic Project Structure

```
myapp/
├── go.mod                 # Go module definition
├── go.sum                 # Go module checksums
├── main.go               # Application entry point
├── config.tsk            # Main configuration file
├── .gitignore            # Git ignore rules
├── README.md             # Project documentation
├── Makefile              # Build and deployment scripts
└── docs/                 # Project documentation
    ├── api.md
    └── deployment.md
```

### Standard Project Structure

```
myapp/
├── cmd/                  # Application entry points
│   ├── server/
│   │   └── main.go
│   ├── worker/
│   │   └── main.go
│   └── cli/
│       └── main.go
├── internal/             # Private application code
│   ├── config/
│   │   ├── parser.go
│   │   └── structs.go
│   ├── handlers/
│   │   ├── user.go
│   │   └── order.go
│   ├── models/
│   │   ├── user.go
│   │   └── order.go
│   └── services/
│       ├── auth.go
│       └── database.go
├── pkg/                  # Public library code
│   ├── tusk/
│   │   ├── parser.go
│   │   └── adapters.go
│   └── utils/
│       ├── logger.go
│       └── validator.go
├── configs/              # Configuration files
│   ├── main.tsk
│   ├── database.tsk
│   ├── server.tsk
│   ├── development.tsk
│   ├── staging.tsk
│   └── production.tsk
├── scripts/              # Build and deployment scripts
│   ├── build.sh
│   ├── deploy.sh
│   └── migrate.sh
├── tests/                # Test files
│   ├── integration/
│   │   └── api_test.go
│   └── unit/
│       └── config_test.go
├── web/                  # Web assets (if applicable)
│   ├── static/
│   └── templates/
├── go.mod
├── go.sum
├── .gitignore
├── README.md
├── Makefile
└── Dockerfile
```

## 📋 Configuration File Organization

### Main Configuration File

```go
// configs/main.tsk
$app_name: "My TuskLang App"
$version: "1.0.0"
$environment: @env("APP_ENV", "development")

[app]
name: $app_name
version: $version
environment: $environment

[database]
host: @configs/database.tsk.get("host")
port: @configs/database.tsk.get("port")
name: @configs/database.tsk.get("name")

[server]
host: @configs/server.tsk.get("host")
port: @configs/server.tsk.get("port")
ssl: @configs/server.tsk.get("ssl")

[features]
enabled: @configs/features.tsk.get("enabled")
settings: @configs/features.tsk.get("settings")
```

### Database Configuration

```go
// configs/database.tsk
$db_environment: @env("DB_ENV", "development")

[development]
host: "localhost"
port: 5432
name: "myapp_dev"
user: "postgres"
password: @env("DB_PASSWORD", "dev_password")
ssl: false

[staging]
host: "staging-db.example.com"
port: 5432
name: "myapp_staging"
user: "myapp"
password: @env("DB_PASSWORD")
ssl: true

[production]
host: "prod-db.example.com"
port: 5432
name: "myapp_production"
user: "myapp"
password: @env("DB_PASSWORD")
ssl: true

# Use environment-specific settings
host: @database.${db_environment}.host
port: @database.${db_environment}.port
name: @database.${db_environment}.name
user: @database.${db_environment}.user
password: @database.${db_environment}.password
ssl: @database.${db_environment}.ssl
```

### Server Configuration

```go
// configs/server.tsk
$server_environment: @env("SERVER_ENV", "development")

[development]
host: "localhost"
port: 8080
ssl: false
debug: true
workers: 1

[staging]
host: "0.0.0.0"
port: 8080
ssl: false
debug: false
workers: 4

[production]
host: "0.0.0.0"
port: 80
ssl: true
debug: false
workers: 8

# Use environment-specific settings
host: @server.${server_environment}.host
port: @server.${server_environment}.port
ssl: @server.${server_environment}.ssl
debug: @server.${server_environment}.debug
workers: @server.${server_environment}.workers
```

### Features Configuration

```go
// configs/features.tsk
$feature_environment: @env("FEATURE_ENV", "development")

[development]
enabled: ["database", "caching", "logging", "debug"]
settings: {
    cache_timeout: "5m"
    log_level: "debug"
    debug_mode: true
}

[staging]
enabled: ["database", "caching", "logging", "monitoring"]
settings: {
    cache_timeout: "10m"
    log_level: "info"
    debug_mode: false
}

[production]
enabled: ["database", "caching", "logging", "monitoring", "security"]
settings: {
    cache_timeout: "30m"
    log_level: "warn"
    debug_mode: false
}

# Use environment-specific settings
enabled: @features.${feature_environment}.enabled
settings: @features.${feature_environment}.settings
```

## 🔧 Go Code Organization

### Main Application Entry Point

```go
// cmd/server/main.go
package main

import (
    "fmt"
    "log"
    "os"
    
    "myapp/internal/config"
    "myapp/internal/handlers"
    "myapp/internal/services"
    "github.com/gin-gonic/gin"
)

func main() {
    // Load configuration
    cfg, err := config.Load("configs/main.tsk")
    if err != nil {
        log.Fatalf("Failed to load configuration: %v", err)
    }
    
    // Initialize services
    dbService, err := services.NewDatabaseService(cfg.Database)
    if err != nil {
        log.Fatalf("Failed to initialize database: %v", err)
    }
    
    // Create Gin router
    r := gin.Default()
    
    // Setup routes
    handlers.SetupRoutes(r, dbService, cfg)
    
    // Start server
    addr := fmt.Sprintf("%s:%d", cfg.Server.Host, cfg.Server.Port)
    log.Printf("Starting server on %s", addr)
    r.Run(addr)
}
```

### Configuration Package

```go
// internal/config/parser.go
package config

import (
    "github.com/tusklang/go"
)

type Config struct {
    App      AppConfig      `tsk:"app"`
    Database DatabaseConfig `tsk:"database"`
    Server   ServerConfig   `tsk:"server"`
    Features FeaturesConfig `tsk:"features"`
}

type AppConfig struct {
    Name        string `tsk:"name"`
    Version     string `tsk:"version"`
    Environment string `tsk:"environment"`
}

type DatabaseConfig struct {
    Host     string `tsk:"host"`
    Port     int    `tsk:"port"`
    Name     string `tsk:"name"`
    User     string `tsk:"user"`
    Password string `tsk:"password"`
    SSL      bool   `tsk:"ssl"`
}

type ServerConfig struct {
    Host    string `tsk:"host"`
    Port    int    `tsk:"port"`
    SSL     bool   `tsk:"ssl"`
    Debug   bool   `tsk:"debug"`
    Workers int    `tsk:"workers"`
}

type FeaturesConfig struct {
    Enabled []string                 `tsk:"enabled"`
    Settings map[string]interface{} `tsk:"settings"`
}

func Load(configPath string) (*Config, error) {
    parser := tusklanggo.NewEnhancedParser()
    
    // Link configuration files
    parser.LinkFile("configs/database.tsk", `
    $db_environment: @env("DB_ENV", "development")
    
    [development]
    host: "localhost"
    port: 5432
    name: "myapp_dev"
    user: "postgres"
    password: @env("DB_PASSWORD", "dev_password")
    ssl: false
    
    host: @database.${db_environment}.host
    port: @database.${db_environment}.port
    name: @database.${db_environment}.name
    user: @database.${db_environment}.user
    password: @database.${db_environment}.password
    ssl: @database.${db_environment}.ssl
    `)
    
    parser.LinkFile("configs/server.tsk", `
    $server_environment: @env("SERVER_ENV", "development")
    
    [development]
    host: "localhost"
    port: 8080
    ssl: false
    debug: true
    workers: 1
    
    host: @server.${server_environment}.host
    port: @server.${server_environment}.port
    ssl: @server.${server_environment}.ssl
    debug: @server.${server_environment}.debug
    workers: @server.${server_environment}.workers
    `)
    
    parser.LinkFile("configs/features.tsk", `
    $feature_environment: @env("FEATURE_ENV", "development")
    
    [development]
    enabled: ["database", "caching", "logging", "debug"]
    settings: {
        cache_timeout: "5m"
        log_level: "debug"
        debug_mode: true
    }
    
    enabled: @features.${feature_environment}.enabled
    settings: @features.${feature_environment}.settings
    `)
    
    data, err := parser.ParseFile(configPath)
    if err != nil {
        return nil, err
    }
    
    var config Config
    err = tusklanggo.UnmarshalTSK(data, &config)
    if err != nil {
        return nil, err
    }
    
    return &config, nil
}
```

### Handlers Package

```go
// internal/handlers/user.go
package handlers

import (
    "net/http"
    "myapp/internal/config"
    "myapp/internal/services"
    "github.com/gin-gonic/gin"
)

type UserHandler struct {
    dbService *services.DatabaseService
    config    *config.Config
}

func NewUserHandler(dbService *services.DatabaseService, config *config.Config) *UserHandler {
    return &UserHandler{
        dbService: dbService,
        config:    config,
    }
}

func (h *UserHandler) GetUsers(c *gin.Context) {
    users, err := h.dbService.GetUsers()
    if err != nil {
        c.JSON(http.StatusInternalServerError, gin.H{"error": err.Error()})
        return
    }
    
    c.JSON(http.StatusOK, gin.H{
        "users": users,
        "app":   h.config.App.Name,
    })
}

func (h *UserHandler) GetUser(c *gin.Context) {
    id := c.Param("id")
    user, err := h.dbService.GetUser(id)
    if err != nil {
        c.JSON(http.StatusNotFound, gin.H{"error": "User not found"})
        return
    }
    
    c.JSON(http.StatusOK, gin.H{"user": user})
}
```

### Services Package

```go
// internal/services/database.go
package services

import (
    "myapp/internal/config"
    "github.com/tusklang/go/adapters"
)

type DatabaseService struct {
    adapter adapters.DatabaseAdapter
    config  *config.DatabaseConfig
}

func NewDatabaseService(config *config.DatabaseConfig) (*DatabaseService, error) {
    adapter, err := adapters.NewPostgreSQLAdapter(adapters.PostgreSQLConfig{
        Host:     config.Host,
        Port:     config.Port,
        Database: config.Name,
        User:     config.User,
        Password: config.Password,
        SSLMode:  config.SSL,
    })
    if err != nil {
        return nil, err
    }
    
    return &DatabaseService{
        adapter: adapter,
        config:  config,
    }, nil
}

func (s *DatabaseService) GetUsers() ([]map[string]interface{}, error) {
    return s.adapter.Query("SELECT * FROM users")
}

func (s *DatabaseService) GetUser(id string) (map[string]interface{}, error) {
    results, err := s.adapter.Query("SELECT * FROM users WHERE id = ?", id)
    if err != nil {
        return nil, err
    }
    
    if len(results) == 0 {
        return nil, fmt.Errorf("user not found")
    }
    
    return results[0], nil
}
```

## 🏢 Enterprise Project Structure

### Microservices Architecture

```
enterprise-app/
├── services/
│   ├── user-service/
│   │   ├── cmd/
│   │   │   └── main.go
│   │   ├── internal/
│   │   │   ├── config/
│   │   │   ├── handlers/
│   │   │   ├── models/
│   │   │   └── services/
│   │   ├── configs/
│   │   │   ├── main.tsk
│   │   │   ├── database.tsk
│   │   │   └── service.tsk
│   │   ├── go.mod
│   │   └── Dockerfile
│   ├── order-service/
│   │   ├── cmd/
│   │   ├── internal/
│   │   ├── configs/
│   │   ├── go.mod
│   │   └── Dockerfile
│   └── payment-service/
│       ├── cmd/
│       ├── internal/
│       ├── configs/
│       ├── go.mod
│       └── Dockerfile
├── shared/
│   ├── pkg/
│   │   ├── tusk/
│   │   ├── database/
│   │   └── utils/
│   └── configs/
│       ├── common.tsk
│       └── environments/
│           ├── development.tsk
│           ├── staging.tsk
│           └── production.tsk
├── infrastructure/
│   ├── docker/
│   ├── kubernetes/
│   └── terraform/
├── scripts/
│   ├── build.sh
│   ├── deploy.sh
│   └── migrate.sh
└── docs/
    ├── architecture.md
    ├── api.md
    └── deployment.md
```

### Shared Configuration

```go
// shared/configs/common.tsk
$app_name: "Enterprise App"
$version: "2.0.0"

[common]
app_name: $app_name
version: $version
environment: @env("APP_ENV", "development")

[logging]
level: @env("LOG_LEVEL", "info")
format: @env("LOG_FORMAT", "json")
output: @env("LOG_OUTPUT", "stdout")

[monitoring]
enabled: @env("MONITORING_ENABLED", "true")
metrics_port: @env("METRICS_PORT", "9090")
health_check_interval: @env("HEALTH_CHECK_INTERVAL", "30s")
```

### Service-Specific Configuration

```go
// services/user-service/configs/service.tsk
$service_name: "user-service"
$service_port: 8081

[service]
name: $service_name
port: $service_port
version: @shared/configs/common.tsk.get("version")

[database]
host: @shared/configs/environments/${environment}.tsk.get("user_db_host")
port: @shared/configs/environments/${environment}.tsk.get("user_db_port")
name: @shared/configs/environments/${environment}.tsk.get("user_db_name")

[api]
rate_limit: @env("API_RATE_LIMIT", "1000")
timeout: @env("API_TIMEOUT", "30s")
cors_origins: @env("CORS_ORIGINS", "*")
```

## 🔧 Build and Deployment

### Makefile

```makefile
# Makefile
.PHONY: build test deploy clean

# Build targets
build:
	go build -o bin/server cmd/server/main.go
	go build -o bin/worker cmd/worker/main.go
	go build -o bin/cli cmd/cli/main.go

build-docker:
	docker build -t myapp:latest .

# Test targets
test:
	go test ./...

test-integration:
	go test ./tests/integration/...

# Deployment targets
deploy-dev:
	APP_ENV=development go run cmd/server/main.go

deploy-staging:
	APP_ENV=staging go run cmd/server/main.go

deploy-prod:
	APP_ENV=production go run cmd/server/main.go

# Utility targets
clean:
	rm -rf bin/
	go clean

lint:
	golangci-lint run

fmt:
	go fmt ./...

# Database targets
migrate:
	go run scripts/migrate.go

seed:
	go run scripts/seed.go
```

### Docker Configuration

```dockerfile
# Dockerfile
FROM golang:1.21-alpine AS builder

WORKDIR /app

# Install TuskLang
RUN go install github.com/tusklang/go/cmd/tusk@latest

# Copy go mod files
COPY go.mod go.sum ./
RUN go mod download

# Copy source code
COPY . .

# Build application
RUN go build -o server cmd/server/main.go

# Runtime stage
FROM alpine:latest

WORKDIR /app

# Install TuskLang CLI
COPY --from=builder /go/bin/tusk /usr/local/bin/

# Copy binary and configuration
COPY --from=builder /app/server .
COPY --from=builder /app/configs ./configs

# Create non-root user
RUN adduser -D appuser
USER appuser

# Expose port
EXPOSE 8080

# Run application
CMD ["./server"]
```

### Kubernetes Configuration

```yaml
# k8s/deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: myapp
spec:
  replicas: 3
  selector:
    matchLabels:
      app: myapp
  template:
    metadata:
      labels:
        app: myapp
    spec:
      containers:
      - name: myapp
        image: myapp:latest
        ports:
        - containerPort: 8080
        env:
        - name: APP_ENV
          value: "production"
        - name: DB_PASSWORD
          valueFrom:
            secretKeyRef:
              name: app-secrets
              key: db-password
        volumeMounts:
        - name: config
          mountPath: /app/configs
      volumes:
      - name: config
        configMap:
          name: app-config
```

## 📊 Testing Structure

### Unit Tests

```go
// tests/unit/config_test.go
package config_test

import (
    "testing"
    "myapp/internal/config"
)

func TestLoadConfig(t *testing.T) {
    cfg, err := config.Load("../../configs/main.tsk")
    if err != nil {
        t.Fatalf("Failed to load config: %v", err)
    }
    
    if cfg.App.Name == "" {
        t.Error("App name should not be empty")
    }
    
    if cfg.Database.Host == "" {
        t.Error("Database host should not be empty")
    }
}

func TestEnvironmentSpecificConfig(t *testing.T) {
    os.Setenv("APP_ENV", "production")
    defer os.Unsetenv("APP_ENV")
    
    cfg, err := config.Load("../../configs/main.tsk")
    if err != nil {
        t.Fatalf("Failed to load config: %v", err)
    }
    
    if cfg.App.Environment != "production" {
        t.Errorf("Expected environment 'production', got '%s'", cfg.App.Environment)
    }
}
```

### Integration Tests

```go
// tests/integration/api_test.go
package integration_test

import (
    "testing"
    "net/http"
    "net/http/httptest"
    "myapp/internal/config"
    "myapp/internal/handlers"
    "myapp/internal/services"
    "github.com/gin-gonic/gin"
)

func TestUserAPI(t *testing.T) {
    // Load test configuration
    cfg, err := config.Load("../../configs/test.tsk")
    if err != nil {
        t.Fatalf("Failed to load test config: %v", err)
    }
    
    // Setup test database
    dbService, err := services.NewDatabaseService(cfg.Database)
    if err != nil {
        t.Fatalf("Failed to setup test database: %v", err)
    }
    
    // Setup Gin router
    gin.SetMode(gin.TestMode)
    r := gin.New()
    handlers.SetupRoutes(r, dbService, cfg)
    
    // Test GET /users
    req, _ := http.NewRequest("GET", "/users", nil)
    w := httptest.NewRecorder()
    r.ServeHTTP(w, req)
    
    if w.Code != http.StatusOK {
        t.Errorf("Expected status 200, got %d", w.Code)
    }
}
```

## 🎯 Best Practices

### 1. Configuration Organization

```go
// Good - Organized by environment
configs/
├── main.tsk
├── database.tsk
├── server.tsk
├── features.tsk
└── environments/
    ├── development.tsk
    ├── staging.tsk
    └── production.tsk

// Bad - All in one file
config.tsk  # Everything mixed together
```

### 2. Go Package Structure

```go
// Good - Clear separation of concerns
internal/
├── config/     # Configuration handling
├── handlers/   # HTTP handlers
├── models/     # Data models
└── services/   # Business logic

// Bad - Everything in one package
main.go  # All code in one file
```

### 3. Environment Variables

```go
// Good - Use environment variables for secrets
password: @env("DB_PASSWORD")

// Bad - Hardcoded secrets
password: "secret123"
```

### 4. Error Handling

```go
// Good - Proper error handling
cfg, err := config.Load("configs/main.tsk")
if err != nil {
    log.Fatalf("Failed to load config: %v", err)
}

// Bad - Ignoring errors
cfg, _ := config.Load("configs/main.tsk")
```

## 📚 Summary

You've learned:

1. **Basic Project Structure** - Simple applications with clear organization
2. **Standard Project Structure** - Scalable applications with proper separation
3. **Configuration Organization** - Environment-specific and modular configs
4. **Go Code Organization** - Proper package structure and separation of concerns
5. **Enterprise Structure** - Microservices and shared components
6. **Build and Deployment** - Makefiles, Docker, and Kubernetes
7. **Testing Structure** - Unit and integration tests
8. **Best Practices** - Clean, maintainable, and scalable code

## 🚀 Next Steps

Now that you understand file structure:

1. **Choose Your Structure** - Pick the right structure for your project size
2. **Organize Configuration** - Create environment-specific configs
3. **Set Up Build Pipeline** - Create Makefiles and deployment scripts
4. **Add Testing** - Implement unit and integration tests
5. **Deploy** - Use Docker and Kubernetes for deployment

---

**"We don't bow to any king"** - Your TuskLang Go projects are now organized for success, from simple applications to complex enterprise systems! 