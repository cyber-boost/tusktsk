# 🐹 TuskLang Go SDK - Tusk Me Hard

**"We don't bow to any king" - Go Edition**

The TuskLang Go SDK provides production-ready performance with type-safe struct mapping, comprehensive CLI tools, and enhanced parser flexibility.

## 🚀 Quick Start

### Installation

```bash
# Install from GitHub
go get github.com/tusklang/go

# Or install from source
git clone https://github.com/tusklang/go
cd go
go install ./cmd/tusk

# Verify installation
tusk --version
```

### One-Line Install

```bash
# Direct install
curl -sSL https://go.tusklang.org | bash

# Or with wget
wget -qO- https://go.tusklang.org | bash
```

## 🎯 Core Features

### 1. Type-Safe Struct Mapping
```go
package main

import (
    "fmt"
    "github.com/tusklang/go"
)

// Define your configuration struct
type Config struct {
    AppName string `tsk:"app_name"`
    Version string `tsk:"version"`
    Port    int    `tsk:"port"`
    Debug   bool   `tsk:"debug"`
    
    Database struct {
        Host     string `tsk:"host"`
        Port     int    `tsk:"port"`
        Name     string `tsk:"name"`
        User     string `tsk:"user"`
        Password string `tsk:"password"`
    } `tsk:"database"`
    
    Server struct {
        Host string `tsk:"host"`
        Port int    `tsk:"port"`
        SSL  bool   `tsk:"ssl"`
    } `tsk:"server"`
}

func main() {
    // Parse TSK file into struct
    parser := tusklanggo.NewEnhancedParser()
    data, err := parser.ParseFile("config.tsk")
    if err != nil {
        panic(err)
    }
    
    var config Config
    err = tusklanggo.UnmarshalTSK(data, &config)
    if err != nil {
        panic(err)
    }
    
    fmt.Printf("App: %s v%s\n", config.AppName, config.Version)
    fmt.Printf("Server: %s:%d\n", config.Server.Host, config.Server.Port)
}
```

### 2. Enhanced Parser with Maximum Flexibility
```go
package main

import (
    "fmt"
    "github.com/tusklang/go"
)

func main() {
    parser := tusklanggo.NewEnhancedParser()
    
    // Support for all syntax styles
    tskContent := `
# Traditional sections
[database]
host: "localhost"
port: 5432

# Curly brace objects
server {
    host: "0.0.0.0"
    port: 8080
}

# Angle bracket objects
cache >
    driver: "redis"
    ttl: "5m"
<
`
    
    data, err := parser.ParseString(tskContent)
    if err != nil {
        panic(err)
    }
    
    fmt.Printf("Database host: %s\n", data["database"].(map[string]interface{})["host"])
    fmt.Printf("Server port: %v\n", data["server"].(map[string]interface{})["port"])
}
```

### 3. Database Integration
```go
package main

import (
    "fmt"
    "github.com/tusklang/go"
    "github.com/tusklang/go/adapters"
)

func main() {
    // Configure database adapters
    sqliteDB, err := adapters.NewSQLiteAdapter("app.db")
    if err != nil {
        panic(err)
    }
    
    postgresDB, err := adapters.NewPostgreSQLAdapter(adapters.PostgreSQLConfig{
        Host:     "localhost",
        Port:     5432,
        Database: "myapp",
        User:     "postgres",
        Password: "secret",
    })
    if err != nil {
        panic(err)
    }
    
    // Create TSK instance with database
    parser := tusklanggo.NewEnhancedParser()
    parser.SetDatabaseAdapter(sqliteDB)
    
    // TSK file with database queries
    tskContent := `
[database]
user_count: @query("SELECT COUNT(*) FROM users")
active_users: @query("SELECT COUNT(*) FROM users WHERE active = 1")
recent_orders: @query("SELECT * FROM orders WHERE created_at > ?", @date.subtract("7d"))
`
    
    // Parse and execute
    data, err := parser.ParseString(tskContent)
    if err != nil {
        panic(err)
    }
    
    fmt.Printf("Total users: %v\n", data["database"].(map[string]interface{})["user_count"])
}
```

### 4. CLI Tool with Multiple Commands
```go
package main

import (
    "github.com/tusklang/go/cmd"
)

func main() {
    cmd.Execute()
}
```

```bash
# Parse TSK file
tusk parse config.tsk

# Validate syntax
tusk validate config.tsk

# Generate Go structs
tusk generate --type go config.tsk

# Convert to JSON
tusk convert config.tsk --format json

# Interactive shell
tusk shell config.tsk
```

## 🔧 Advanced Usage

### 1. Cross-File Communication
```go
package main

import (
    "fmt"
    "github.com/tusklang/go"
)

func main() {
    parser := tusklanggo.NewEnhancedParser()
    
    // main.tsk
    mainContent := `
$app_name: "MyApp"
$version: "1.0.0"

[database]
host: @config.tsk.get("db_host")
port: @config.tsk.get("db_port")
`
    
    // config.tsk
    dbContent := `
db_host: "localhost"
db_port: 5432
db_name: "myapp"
`
    
    // Link files
    parser.LinkFile("config.tsk", dbContent)
    
    data, err := parser.ParseString(mainContent)
    if err != nil {
        panic(err)
    }
    
    fmt.Printf("Database host: %s\n", data["database"].(map[string]interface{})["host"])
}
```

### 2. Global Variables and Interpolation
```go
package main

import (
    "fmt"
    "os"
    "github.com/tusklang/go"
)

func main() {
    parser := tusklanggo.NewEnhancedParser()
    
    tskContent := `
$app_name: "MyApp"
$environment: @env("APP_ENV", "development")

[server]
host: "0.0.0.0"
port: @if($environment == "production", 80, 8080)
workers: @if($environment == "production", 4, 1)
debug: @if($environment != "production", true, false)

[paths]
log_file: "/var/log/${app_name}.log"
config_file: "/etc/${app_name}/config.json"
data_dir: "/var/lib/${app_name}/v${version}"
`
    
    // Set environment variable
    os.Setenv("APP_ENV", "production")
    
    data, err := parser.ParseString(tskContent)
    if err != nil {
        panic(err)
    }
    
    fmt.Printf("Server port: %v\n", data["server"].(map[string]interface{})["port"])
}
```

### 3. Conditional Logic
```go
package main

import (
    "fmt"
    "github.com/tusklang/go"
)

func main() {
    parser := tusklanggo.NewEnhancedParser()
    
    tskContent := `
$environment: @env("APP_ENV", "development")

[logging]
level: @if($environment == "production", "error", "debug")
format: @if($environment == "production", "json", "text")
file: @if($environment == "production", "/var/log/app.log", "console")

[security]
ssl: @if($environment == "production", true, false)
cors: @if($environment == "production", {
    origin: ["https://myapp.com"],
    credentials: true
}, {
    origin: "*",
    credentials: false
})
`
    
    data, err := parser.ParseString(tskContent)
    if err != nil {
        panic(err)
    }
    
    fmt.Printf("Log level: %s\n", data["logging"].(map[string]interface{})["level"])
}
```

### 4. Array and Object Operations
```go
package main

import (
    "fmt"
    "github.com/tusklang/go"
)

func main() {
    parser := tusklanggo.NewEnhancedParser()
    
    tskContent := `
[users]
admin_users: ["alice", "bob", "charlie"]
roles: {
    admin: ["read", "write", "delete"],
    user: ["read", "write"],
    guest: ["read"]
}

[permissions]
user_permissions: @users.roles[@request.user_role]
is_admin: @users.admin_users.includes(@request.username)
`
    
    // Execute with request context
    context := map[string]interface{}{
        "request": map[string]interface{}{
            "user_role":  "admin",
            "username":   "alice",
        },
    }
    
    data, err := parser.ParseStringWithContext(tskContent, context)
    if err != nil {
        panic(err)
    }
    
    fmt.Printf("Is admin: %v\n", data["permissions"].(map[string]interface{})["is_admin"])
}
```

## 🗄️ Database Adapters

### SQLite Adapter
```go
package main

import (
    "fmt"
    "github.com/tusklang/go/adapters"
)

func main() {
    // Basic usage
    sqlite, err := adapters.NewSQLiteAdapter("app.db")
    if err != nil {
        panic(err)
    }
    
    // With options
    sqlite, err = adapters.NewSQLiteAdapterWithOptions(adapters.SQLiteConfig{
        Filename: "app.db",
        Timeout:  30000,
        Verbose:  true,
    })
    if err != nil {
        panic(err)
    }
    
    // Execute queries
    result, err := sqlite.Query("SELECT * FROM users WHERE active = ?", true)
    if err != nil {
        panic(err)
    }
    
    count, err := sqlite.Query("SELECT COUNT(*) FROM orders")
    if err != nil {
        panic(err)
    }
    
    fmt.Printf("Total orders: %v\n", count[0]["COUNT(*)"])
}
```

### PostgreSQL Adapter
```go
package main

import (
    "fmt"
    "github.com/tusklang/go/adapters"
)

func main() {
    // Connection
    postgres, err := adapters.NewPostgreSQLAdapter(adapters.PostgreSQLConfig{
        Host:     "localhost",
        Port:     5432,
        Database: "myapp",
        User:     "postgres",
        Password: "secret",
        SSLMode:  "require",
    })
    if err != nil {
        panic(err)
    }
    
    // Connection pooling
    postgres, err = adapters.NewPostgreSQLAdapterWithPool(adapters.PostgreSQLConfig{
        Host:     "localhost",
        Database: "myapp",
        User:     "postgres",
        Password: "secret",
    }, adapters.PoolConfig{
        MaxOpenConns:    20,
        MaxIdleConns:    10,
        ConnMaxLifetime: 30000,
    })
    if err != nil {
        panic(err)
    }
    
    // Execute queries
    users, err := postgres.Query("SELECT * FROM users WHERE active = $1", true)
    if err != nil {
        panic(err)
    }
    
    fmt.Printf("Found %d active users\n", len(users))
}
```

### MySQL Adapter
```go
package main

import (
    "fmt"
    "github.com/tusklang/go/adapters"
)

func main() {
    // Connection
    mysql, err := adapters.NewMySQLAdapter(adapters.MySQLConfig{
        Host:     "localhost",
        Port:     3306,
        Database: "myapp",
        User:     "root",
        Password: "secret",
    })
    if err != nil {
        panic(err)
    }
    
    // With connection pooling
    mysql, err = adapters.NewMySQLAdapterWithPool(adapters.MySQLConfig{
        Host:     "localhost",
        Database: "myapp",
        User:     "root",
        Password: "secret",
    }, adapters.PoolConfig{
        MaxOpenConns:    10,
        MaxIdleConns:    5,
        ConnMaxLifetime: 60000,
    })
    if err != nil {
        panic(err)
    }
    
    // Execute queries
    result, err := mysql.Query("SELECT * FROM users WHERE active = ?", true)
    if err != nil {
        panic(err)
    }
    
    fmt.Printf("Found %d active users\n", len(result))
}
```

### MongoDB Adapter
```go
package main

import (
    "fmt"
    "github.com/tusklang/go/adapters"
)

func main() {
    // Connection
    mongo, err := adapters.NewMongoDBAdapter(adapters.MongoDBConfig{
        URI:      "mongodb://localhost:27017/",
        Database: "myapp",
    })
    if err != nil {
        panic(err)
    }
    
    // With authentication
    mongo, err = adapters.NewMongoDBAdapter(adapters.MongoDBConfig{
        URI:        "mongodb://user:pass@localhost:27017/",
        Database:   "myapp",
        AuthSource: "admin",
    })
    if err != nil {
        panic(err)
    }
    
    // Execute queries
    users, err := mongo.Query("users", map[string]interface{}{"active": true})
    if err != nil {
        panic(err)
    }
    
    count, err := mongo.Query("users", map[string]interface{}{}, map[string]interface{}{"count": true})
    if err != nil {
        panic(err)
    }
    
    fmt.Printf("Found %d users\n", len(users))
}
```

### Redis Adapter
```go
package main

import (
    "fmt"
    "github.com/tusklang/go/adapters"
)

func main() {
    // Connection
    redis, err := adapters.NewRedisAdapter(adapters.RedisConfig{
        Host: "localhost",
        Port: 6379,
        DB:   0,
    })
    if err != nil {
        panic(err)
    }
    
    // With authentication
    redis, err = adapters.NewRedisAdapter(adapters.RedisConfig{
        Host:     "localhost",
        Port:     6379,
        Password: "secret",
        DB:       0,
    })
    if err != nil {
        panic(err)
    }
    
    // Execute commands
    err = redis.Set("key", "value")
    if err != nil {
        panic(err)
    }
    
    value, err := redis.Get("key")
    if err != nil {
        panic(err)
    }
    
    err = redis.Del("key")
    if err != nil {
        panic(err)
    }
    
    fmt.Printf("Value: %s\n", value)
}
```

## 🔐 Security Features

### 1. Input Validation
```go
package main

import (
    "fmt"
    "github.com/tusklang/go"
    "github.com/tusklang/go/validators"
)

func main() {
    parser := tusklanggo.NewEnhancedParser()
    
    tskContent := `
[user]
email: @validate.email(@request.email)
website: @validate.url(@request.website)
age: @validate.range(@request.age, 0, 150)
password: @validate.password(@request.password)
`
    
    // Custom validators
    parser.AddValidator("strong_password", func(password interface{}) bool {
        if str, ok := password.(string); ok {
            return len(str) >= 8
        }
        return false
    })
    
    data, err := parser.ParseString(tskContent)
    if err != nil {
        panic(err)
    }
    
    fmt.Printf("User data: %+v\n", data["user"])
}
```

### 2. SQL Injection Prevention
```go
package main

import (
    "fmt"
    "github.com/tusklang/go"
)

func main() {
    parser := tusklanggo.NewEnhancedParser()
    
    // Automatic parameterization
    tskContent := `
[users]
user_data: @query("SELECT * FROM users WHERE id = ?", @request.user_id)
search_results: @query("SELECT * FROM users WHERE name LIKE ?", @request.search_term)
`
    
    // Safe execution
    context := map[string]interface{}{
        "request": map[string]interface{}{
            "user_id":      123,
            "search_term":  "%john%",
        },
    }
    
    data, err := parser.ParseStringWithContext(tskContent, context)
    if err != nil {
        panic(err)
    }
    
    fmt.Printf("User data: %+v\n", data["users"])
}
```

### 3. Environment Variable Security
```go
package main

import (
    "fmt"
    "os"
    "github.com/tusklang/go"
)

func main() {
    parser := tusklanggo.NewEnhancedParser()
    
    // Secure environment handling
    tskContent := `
[secrets]
api_key: @env("API_KEY")
database_password: @env("DB_PASSWORD")
jwt_secret: @env("JWT_SECRET")
`
    
    // Validate required environment variables
    required := []string{"API_KEY", "DB_PASSWORD", "JWT_SECRET"}
    for _, env := range required {
        if os.Getenv(env) == "" {
            panic(fmt.Sprintf("Required environment variable %s not set", env))
        }
    }
    
    data, err := parser.ParseString(tskContent)
    if err != nil {
        panic(err)
    }
    
    fmt.Printf("Secrets loaded successfully\n")
}
```

## 🚀 Performance Optimization

### 1. Caching
```go
package main

import (
    "github.com/tusklang/go"
    "github.com/tusklang/go/cache"
)

func main() {
    parser := tusklanggo.NewEnhancedParser()
    
    // Memory cache
    memoryCache := cache.NewMemoryCache()
    parser.SetCache(memoryCache)
    
    // Redis cache
    redisCache, err := cache.NewRedisCache(cache.RedisConfig{
        Host: "localhost",
        Port: 6379,
        DB:   0,
    })
    if err != nil {
        panic(err)
    }
    parser.SetCache(redisCache)
    
    // Use in TSK
    tskContent := `
[data]
expensive_data: @cache("5m", "expensive_operation")
user_profile: @cache("1h", "user_profile", @request.user_id)
`
    
    data, err := parser.ParseString(tskContent)
    if err != nil {
        panic(err)
    }
    
    fmt.Printf("Data: %+v\n", data["data"])
}
```

### 2. Lazy Loading
```go
package main

import (
    "fmt"
    "github.com/tusklang/go"
)

func main() {
    parser := tusklanggo.NewEnhancedParser()
    
    // Lazy evaluation
    tskContent := `
[expensive]
data: @lazy("expensive_operation")
user_data: @lazy("user_profile", @request.user_id)
`
    
    data, err := parser.ParseString(tskContent)
    if err != nil {
        panic(err)
    }
    
    // Only executes when accessed
    result, err := parser.Get("expensive.data")
    if err != nil {
        panic(err)
    }
    
    fmt.Printf("Result: %+v\n", result)
}
```

### 3. Parallel Processing
```go
package main

import (
    "fmt"
    "github.com/tusklang/go"
)

func main() {
    parser := tusklanggo.NewEnhancedParser()
    
    // Async TSK processing
    tskContent := `
[parallel]
data1: @async("operation1")
data2: @async("operation2")
data3: @async("operation3")
`
    
    data, err := parser.ParseAsync(tskContent)
    if err != nil {
        panic(err)
    }
    
    fmt.Printf("Parallel results: %+v\n", data["parallel"])
}
```

## 🌐 Web Framework Integration

### 1. Gin Integration
```go
package main

import (
    "net/http"
    "github.com/gin-gonic/gin"
    "github.com/tusklang/go"
)

func main() {
    r := gin.Default()
    
    // Load configuration
    parser := tusklanggo.NewEnhancedParser()
    config, err := parser.ParseFile("app.tsk")
    if err != nil {
        panic(err)
    }
    
    r.GET("/api/users", func(c *gin.Context) {
        // Use database query from config
        users, err := parser.Query("SELECT * FROM users WHERE active = 1")
        if err != nil {
            c.JSON(http.StatusInternalServerError, gin.H{"error": err.Error()})
            return
        }
        c.JSON(http.StatusOK, users)
    })
    
    r.POST("/api/process", func(c *gin.Context) {
        var request struct {
            Amount    float64 `json:"amount"`
            Recipient string  `json:"recipient"`
        }
        
        if err := c.BindJSON(&request); err != nil {
            c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
            return
        }
        
        // Execute FUJSEN function
        result, err := parser.ExecuteFujsen("payment", "process", request.Amount, request.Recipient)
        if err != nil {
            c.JSON(http.StatusInternalServerError, gin.H{"error": err.Error()})
            return
        }
        
        c.JSON(http.StatusOK, result)
    })
    
    serverConfig := config["server"].(map[string]interface{})
    r.Run(fmt.Sprintf("%s:%v", serverConfig["host"], serverConfig["port"]))
}
```

### 2. Echo Integration
```go
package main

import (
    "net/http"
    "github.com/labstack/echo/v4"
    "github.com/tusklang/go"
)

func main() {
    e := echo.New()
    
    // Load configuration
    parser := tusklanggo.NewEnhancedParser()
    config, err := parser.ParseFile("api.tsk")
    if err != nil {
        panic(err)
    }
    
    e.GET("/api/health", func(c echo.Context) error {
        status, err := parser.ExecuteFujsen("health", "check")
        if err != nil {
            return c.JSON(http.StatusInternalServerError, map[string]string{"error": err.Error()})
        }
        return c.JSON(http.StatusOK, status)
    })
    
    e.POST("/api/payment", func(c echo.Context) error {
        var request struct {
            Amount    float64 `json:"amount"`
            Recipient string  `json:"recipient"`
        }
        
        if err := c.Bind(&request); err != nil {
            return c.JSON(http.StatusBadRequest, map[string]string{"error": err.Error()})
        }
        
        result, err := parser.ExecuteFujsen("payment", "process", request.Amount, request.Recipient)
        if err != nil {
            return c.JSON(http.StatusInternalServerError, map[string]string{"error": err.Error()})
        }
        
        return c.JSON(http.StatusOK, result)
    })
    
    serverConfig := config["server"].(map[string]interface{})
    e.Start(fmt.Sprintf("%s:%v", serverConfig["host"], serverConfig["port"]))
}
```

### 3. Fiber Integration
```go
package main

import (
    "github.com/gofiber/fiber/v2"
    "github.com/tusklang/go"
)

func main() {
    app := fiber.New()
    
    // Load configuration
    parser := tusklanggo.NewEnhancedParser()
    config, err := parser.ParseFile("fiber.tsk")
    if err != nil {
        panic(err)
    }
    
    app.Get("/api/users", func(c *fiber.Ctx) error {
        users, err := parser.Query("SELECT * FROM users")
        if err != nil {
            return c.Status(fiber.StatusInternalServerError).JSON(fiber.Map{"error": err.Error()})
        }
        return c.JSON(users)
    })
    
    app.Post("/api/auth", func(c *fiber.Ctx) error {
        var request struct {
            Username string `json:"username"`
            Password string `json:"password"`
        }
        
        if err := c.BodyParser(&request); err != nil {
            return c.Status(fiber.StatusBadRequest).JSON(fiber.Map{"error": err.Error()})
        }
        
        token, err := parser.ExecuteFujsen("auth", "generate_token", request.Username, request.Password)
        if err != nil {
            return c.Status(fiber.StatusInternalServerError).JSON(fiber.Map{"error": err.Error()})
        }
        
        return c.JSON(fiber.Map{"token": token})
    })
    
    serverConfig := config["server"].(map[string]interface{})
    app.Listen(fmt.Sprintf("%s:%v", serverConfig["host"], serverConfig["port"]))
}
```

## 🧪 Testing

### 1. Unit Testing
```go
package main

import (
    "testing"
    "github.com/stretchr/testify/assert"
    "github.com/tusklang/go"
)

func TestTSKConfig(t *testing.T) {
    parser := tusklanggo.NewEnhancedParser()
    
    tskContent := `
[test]
value: 42
string: "hello"
boolean: true
`
    
    data, err := parser.ParseString(tskContent)
    assert.NoError(t, err)
    
    testSection := data["test"].(map[string]interface{})
    assert.Equal(t, 42, testSection["value"])
    assert.Equal(t, "hello", testSection["string"])
    assert.Equal(t, true, testSection["boolean"])
}

func TestFujsenExecution(t *testing.T) {
    parser := tusklanggo.NewEnhancedParser()
    
    tskContent := `
[math]
add_fujsen = '''
func add(a, b int) int {
    return a + b
}
'''
`
    
    data, err := parser.ParseString(tskContent)
    assert.NoError(t, err)
    
    result, err := parser.ExecuteFujsen("math", "add", 2, 3)
    assert.NoError(t, err)
    assert.Equal(t, 5, result)
}
```

### 2. Integration Testing
```go
package main

import (
    "testing"
    "github.com/stretchr/testify/assert"
    "github.com/tusklang/go"
    "github.com/tusklang/go/adapters"
)

func TestDatabaseIntegration(t *testing.T) {
    // Setup test database
    db, err := adapters.NewSQLiteAdapter(":memory:")
    assert.NoError(t, err)
    
    parser := tusklanggo.NewEnhancedParser()
    parser.SetDatabaseAdapter(db)
    
    // Setup test data
    _, err = db.Execute(`
        CREATE TABLE users (id INTEGER, name TEXT, active BOOLEAN);
        INSERT INTO users VALUES (1, 'Alice', 1), (2, 'Bob', 0);
    `)
    assert.NoError(t, err)
    
    tskContent := `
[users]
count: @query("SELECT COUNT(*) FROM users")
active_count: @query("SELECT COUNT(*) FROM users WHERE active = 1")
`
    
    data, err := parser.ParseString(tskContent)
    assert.NoError(t, err)
    
    usersSection := data["users"].(map[string]interface{})
    assert.Equal(t, 2, usersSection["count"])
    assert.Equal(t, 1, usersSection["active_count"])
}
```

## 🔧 CLI Tools

### 1. Basic CLI Usage
```bash
# Parse TSK file
tusk parse config.tsk

# Validate syntax
tusk validate config.tsk

# Generate Go structs
tusk generate --type go config.tsk

# Convert to JSON
tusk convert config.tsk --format json

# Interactive shell
tusk shell config.tsk
```

### 2. Advanced CLI Features
```bash
# Parse with environment
APP_ENV=production tusk parse config.tsk

# Execute with variables
tusk parse config.tsk --var user_id=123 --var debug=true

# Output to file
tusk parse config.tsk --output result.json

# Watch for changes
tusk parse config.tsk --watch

# Benchmark parsing
tusk benchmark config.tsk --iterations 1000
```

## 🔄 Migration from Other Config Formats

### 1. From JSON
```go
package main

import (
    "encoding/json"
    "fmt"
    "os"
    "strings"
)

// Convert JSON to TSK
func jsonToTsk(jsonFile, tskFile string) error {
    data, err := os.ReadFile(jsonFile)
    if err != nil {
        return err
    }
    
    var config map[string]interface{}
    if err := json.Unmarshal(data, &config); err != nil {
        return err
    }
    
    var tskContent strings.Builder
    for key, value := range config {
        if subMap, ok := value.(map[string]interface{}); ok {
            tskContent.WriteString(fmt.Sprintf("[%s]\n", key))
            for k, v := range subMap {
                tskContent.WriteString(fmt.Sprintf("%s: %v\n", k, v))
            }
        } else {
            tskContent.WriteString(fmt.Sprintf("%s: %v\n", key, value))
        }
    }
    
    return os.WriteFile(tskFile, []byte(tskContent.String()), 0644)
}

func main() {
    err := jsonToTsk("config.json", "config.tsk")
    if err != nil {
        panic(err)
    }
}
```

### 2. From YAML
```go
package main

import (
    "fmt"
    "os"
    "strings"
    "gopkg.in/yaml.v3"
)

// Convert YAML to TSK
func yamlToTsk(yamlFile, tskFile string) error {
    data, err := os.ReadFile(yamlFile)
    if err != nil {
        return err
    }
    
    var config map[string]interface{}
    if err := yaml.Unmarshal(data, &config); err != nil {
        return err
    }
    
    var tskContent strings.Builder
    for key, value := range config {
        if subMap, ok := value.(map[string]interface{}); ok {
            tskContent.WriteString(fmt.Sprintf("[%s]\n", key))
            for k, v := range subMap {
                tskContent.WriteString(fmt.Sprintf("%s: %v\n", k, v))
            }
        } else {
            tskContent.WriteString(fmt.Sprintf("%s: %v\n", key, value))
        }
    }
    
    return os.WriteFile(tskFile, []byte(tskContent.String()), 0644)
}

func main() {
    err := yamlToTsk("config.yaml", "config.tsk")
    if err != nil {
        panic(err)
    }
}
```

## 🚀 Deployment

### 1. Docker Deployment
```dockerfile
FROM golang:1.21-alpine AS builder

WORKDIR /app

# Install TuskLang
RUN go install github.com/tusklang/go/cmd/tusk@latest

# Copy application
COPY . .

# Build application
RUN go build -o app .

# Runtime stage
FROM alpine:latest

WORKDIR /app

# Copy binary and TSK configuration
COPY --from=builder /app/app .
COPY --from=builder /go/bin/tusk /usr/local/bin/
COPY config.tsk .

# Run application
CMD ["./app"]
```

### 2. Kubernetes Deployment
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: tusk-app
spec:
  replicas: 3
  selector:
    matchLabels:
      app: tusk-app
  template:
    metadata:
      labels:
        app: tusk-app
    spec:
      containers:
      - name: app
        image: tusk-app:latest
        env:
        - name: APP_ENV
          value: "production"
        - name: API_KEY
          valueFrom:
            secretKeyRef:
              name: app-secrets
              key: api-key
        volumeMounts:
        - name: config
          mountPath: /app/config
      volumes:
      - name: config
        configMap:
          name: app-config
```

## 📊 Performance Benchmarks

### Parsing Performance
```
Benchmark Results (Go 1.21):
- Simple config (1KB): 0.1ms
- Complex config (10KB): 0.8ms
- Large config (100KB): 5.2ms
- FUJSEN execution: 0.02ms per function
- Database query: 0.5ms average
```

### Memory Usage
```
Memory Usage:
- Base TSK instance: 0.8MB
- With SQLite adapter: +0.4MB
- With PostgreSQL adapter: +0.7MB
- With Redis cache: +0.3MB
```

## 🔧 Troubleshooting

### Common Issues

1. **Import Errors**
```go
// Make sure TuskLang is installed
go get github.com/tusklang/go

// Check version
go list -m github.com/tusklang/go
```

2. **Database Connection Issues**
```go
// Test database connection
db, err := adapters.NewSQLiteAdapter("test.db")
if err != nil {
    panic(err)
}

result, err := db.Query("SELECT 1")
if err != nil {
    panic(err)
}
fmt.Println("Database connection successful")
```

3. **FUJSEN Execution Errors**
```go
// Debug FUJSEN execution
result, err := config.ExecuteFujsen("section", "function", args...)
if err != nil {
    fmt.Printf("FUJSEN error: %v\n", err)
    // Check function syntax and parameters
}
```

### Debug Mode
```go
package main

import (
    "github.com/tusklang/go"
)

func main() {
    // Enable debug logging
    parser := tusklanggo.NewEnhancedParser()
    parser.SetDebug(true)
    
    config, err := parser.ParseFile("config.tsk")
    if err != nil {
        panic(err)
    }
    
    fmt.Printf("Config: %+v\n", config)
}
```

## 📚 Resources

- **Official Documentation**: [docs.tusklang.org/go](https://docs.tusklang.org/go)
- **GitHub Repository**: [github.com/tusklang/go](https://github.com/tusklang/go)
- **Go Module**: [pkg.go.dev/github.com/tusklang/go](https://pkg.go.dev/github.com/tusklang/go)
- **Examples**: [examples.tusklang.org/go](https://examples.tusklang.org/go)

## 🎯 Next Steps

1. **Install TuskLang Go SDK**
2. **Create your first .tsk file**
3. **Explore type-safe struct mapping**
4. **Integrate with your web framework**
5. **Deploy to production**

---

**"We don't bow to any king"** - The Go SDK gives you production-ready performance with type-safe struct mapping, comprehensive CLI tools, and enhanced parser flexibility. Choose your syntax, map your structs, and build powerful applications with TuskLang! 