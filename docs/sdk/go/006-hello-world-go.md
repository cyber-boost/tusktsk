# 🌍 TuskLang Go Hello World Guide

**"We don't bow to any king" - Go Edition**

Create your first TuskLang application in Go and experience the revolutionary power of configuration with a heartbeat. This guide will take you from zero to a fully functional application in minutes.

## 🚀 Your First TuskLang Go Application

### Step 1: Create Your First TSK File

```go
// hello.tsk
[app]
name: "Hello TuskLang"
version: "1.0.0"
message: "Hello, World from TuskLang!"

[greeting]
text: "Welcome to the future of configuration!"
timestamp: @date.now()
user_count: @query("SELECT COUNT(*) FROM users")
```

### Step 2: Create Your Go Application

```go
// main.go
package main

import (
    "fmt"
    "github.com/tusklang/go"
)

// Define your configuration struct
type HelloConfig struct {
    App struct {
        Name    string `tsk:"name"`
        Version string `tsk:"version"`
        Message string `tsk:"message"`
    } `tsk:"app"`
    
    Greeting struct {
        Text      string      `tsk:"text"`
        Timestamp interface{} `tsk:"timestamp"`
        UserCount interface{} `tsk:"user_count"`
    } `tsk:"greeting"`
}

func main() {
    // Create parser
    parser := tusklanggo.NewEnhancedParser()
    
    // Parse TSK file
    data, err := parser.ParseFile("hello.tsk")
    if err != nil {
        panic(err)
    }
    
    // Unmarshal into struct
    var config HelloConfig
    err = tusklanggo.UnmarshalTSK(data, &config)
    if err != nil {
        panic(err)
    }
    
    // Display your first TuskLang application
    fmt.Printf("🎉 %s v%s\n", config.App.Name, config.App.Version)
    fmt.Printf("💬 %s\n", config.App.Message)
    fmt.Printf("🌟 %s\n", config.Greeting.Text)
    fmt.Printf("⏰ Timestamp: %v\n", config.Greeting.Timestamp)
    fmt.Printf("👥 Users: %v\n", config.Greeting.UserCount)
}
```

### Step 3: Run Your Application

```bash
# Run the application
go run main.go
```

**Output:**
```
🎉 Hello TuskLang v1.0.0
💬 Hello, World from TuskLang!
🌟 Welcome to the future of configuration!
⏰ Timestamp: 2024-12-19T10:30:00Z
👥 Users: 42
```

## 🎨 Multiple Syntax Styles

### Traditional INI-Style

```go
// hello-ini.tsk
[app]
name: "Hello TuskLang"
version: "1.0.0"

[greeting]
text: "Hello from INI-style syntax!"
```

### JSON-Like Objects

```go
// hello-json.tsk
app {
    name: "Hello TuskLang"
    version: "1.0.0"
}

greeting {
    text: "Hello from JSON-style syntax!"
}
```

### XML-Inspired Syntax

```go
// hello-xml.tsk
app >
    name: "Hello TuskLang"
    version: "1.0.0"
<

greeting >
    text: "Hello from XML-style syntax!"
<
```

```go
// main.go - Multi-syntax support
package main

import (
    "fmt"
    "github.com/tusklang/go"
)

func main() {
    parser := tusklanggo.NewEnhancedParser()
    
    // Parse different syntax styles
    files := []string{"hello-ini.tsk", "hello-json.tsk", "hello-xml.tsk"}
    
    for _, file := range files {
        data, err := parser.ParseFile(file)
        if err != nil {
            panic(err)
        }
        
        fmt.Printf("✅ Parsed %s successfully\n", file)
        fmt.Printf("   App: %s v%s\n", 
            data["app"].(map[string]interface{})["name"],
            data["app"].(map[string]interface{})["version"])
    }
}
```

## 🔗 Database Integration from the Start

### Hello World with Database

```go
// hello-db.tsk
[app]
name: "Hello Database"
version: "1.0.0"

[database]
host: "localhost"
port: 5432
name: "hello_world"

[stats]
user_count: @query("SELECT COUNT(*) FROM users")
recent_posts: @query("SELECT * FROM posts WHERE created_at > ?", @date.subtract("7d"))
active_users: @query("SELECT COUNT(*) FROM users WHERE last_login > ?", @date.subtract("24h"))
```

```go
// main.go - Database integration
package main

import (
    "fmt"
    "github.com/tusklang/go"
    "github.com/tusklang/go/adapters"
)

type HelloDBConfig struct {
    App struct {
        Name    string `tsk:"name"`
        Version string `tsk:"version"`
    } `tsk:"app"`
    
    Database struct {
        Host string `tsk:"host"`
        Port int    `tsk:"port"`
        Name string `tsk:"name"`
    } `tsk:"database"`
    
    Stats struct {
        UserCount   interface{} `tsk:"user_count"`
        RecentPosts interface{} `tsk:"recent_posts"`
        ActiveUsers interface{} `tsk:"active_users"`
    } `tsk:"stats"`
}

func main() {
    // Create database adapter
    postgres, err := adapters.NewPostgreSQLAdapter(adapters.PostgreSQLConfig{
        Host:     "localhost",
        Port:     5432,
        Database: "hello_world",
        User:     "postgres",
        Password: "secret",
    })
    if err != nil {
        panic(err)
    }
    
    // Create parser with database
    parser := tusklanggo.NewEnhancedParser()
    parser.SetDatabaseAdapter(postgres)
    
    // Parse configuration with database queries
    data, err := parser.ParseFile("hello-db.tsk")
    if err != nil {
        panic(err)
    }
    
    var config HelloDBConfig
    err = tusklanggo.UnmarshalTSK(data, &config)
    if err != nil {
        panic(err)
    }
    
    fmt.Printf("🗄️ %s v%s\n", config.App.Name, config.App.Version)
    fmt.Printf("📊 Database: %s:%d/%s\n", config.Database.Host, config.Database.Port, config.Database.Name)
    fmt.Printf("👥 Total Users: %v\n", config.Stats.UserCount)
    fmt.Printf("📝 Recent Posts: %v\n", config.Stats.RecentPosts)
    fmt.Printf("🟢 Active Users: %v\n", config.Stats.ActiveUsers)
}
```

## 🌍 Environment-Aware Hello World

### Dynamic Configuration

```go
// hello-env.tsk
$environment: @env("APP_ENV", "development")
$app_name: "Hello TuskLang"

[app]
name: $app_name
version: "1.0.0"
environment: $environment

[greeting]
text: @if($environment == "production", "Welcome to production!", "Hello from development!")
debug_mode: @if($environment == "development", true, false)

[server]
host: @if($environment == "production", "0.0.0.0", "localhost")
port: @if($environment == "production", 80, 8080)
```

```go
// main.go - Environment-aware
package main

import (
    "fmt"
    "os"
    "github.com/tusklang/go"
)

type HelloEnvConfig struct {
    App struct {
        Name        string `tsk:"name"`
        Version     string `tsk:"version"`
        Environment string `tsk:"environment"`
    } `tsk:"app"`
    
    Greeting struct {
        Text      string `tsk:"text"`
        DebugMode bool   `tsk:"debug_mode"`
    } `tsk:"greeting"`
    
    Server struct {
        Host string `tsk:"host"`
        Port int    `tsk:"port"`
    } `tsk:"server"`
}

func main() {
    // Set environment variable
    os.Setenv("APP_ENV", "production")
    
    parser := tusklanggo.NewEnhancedParser()
    data, err := parser.ParseFile("hello-env.tsk")
    if err != nil {
        panic(err)
    }
    
    var config HelloEnvConfig
    err = tusklanggo.UnmarshalTSK(data, &config)
    if err != nil {
        panic(err)
    }
    
    fmt.Printf("🌍 %s v%s (%s)\n", config.App.Name, config.App.Version, config.App.Environment)
    fmt.Printf("💬 %s\n", config.Greeting.Text)
    fmt.Printf("🔧 Debug Mode: %v\n", config.Greeting.DebugMode)
    fmt.Printf("🌐 Server: %s:%d\n", config.Server.Host, config.Server.Port)
}
```

## 🔧 FUJSEN Hello World

### Executable Functions

```go
// hello-fujsen.tsk
[functions]
greet_user: """
function greet(name, time) {
    const hour = new Date(time).getHours();
    let greeting = '';
    
    if (hour < 12) {
        greeting = 'Good morning';
    } else if (hour < 18) {
        greeting = 'Good afternoon';
    } else {
        greeting = 'Good evening';
    }
    
    return greeting + ', ' + name + '!';
}
"""

calculate_score: """
function calculate(points, bonus) {
    return points * (1 + bonus / 100);
}
"""

[user]
name: "Alice"
points: 100
bonus: 15
greeting: @fujsen(greet_user, @user.name, @date.now())
score: @fujsen(calculate_score, @user.points, @user.bonus)
```

```go
// main.go - FUJSEN functions
package main

import (
    "fmt"
    "github.com/tusklang/go"
)

type HelloFujsenConfig struct {
    Functions struct {
        GreetUser     string `tsk:"greet_user"`
        CalculateScore string `tsk:"calculate_score"`
    } `tsk:"functions"`
    
    User struct {
        Name     string  `tsk:"name"`
        Points   int     `tsk:"points"`
        Bonus    int     `tsk:"bonus"`
        Greeting string  `tsk:"greeting"`
        Score    float64 `tsk:"score"`
    } `tsk:"user"`
}

func main() {
    parser := tusklanggo.NewEnhancedParser()
    data, err := parser.ParseFile("hello-fujsen.tsk")
    if err != nil {
        panic(err)
    }
    
    var config HelloFujsenConfig
    err = tusklanggo.UnmarshalTSK(data, &config)
    if err != nil {
        panic(err)
    }
    
    fmt.Printf("🤖 %s\n", config.User.Greeting)
    fmt.Printf("📊 Score: %.2f (Base: %d, Bonus: %d%%)\n", 
        config.User.Score, config.User.Points, config.User.Bonus)
}
```

## 🌐 Web Framework Hello World

### Gin Framework Integration

```go
// hello-web.tsk
[app]
name: "Hello Web"
version: "1.0.0"

[server]
host: "localhost"
port: 8080
debug: true

[features]
enable_cors: true
rate_limit: 100
cache_timeout: "5m"
```

```go
// main.go - Gin web application
package main

import (
    "fmt"
    "github.com/gin-gonic/gin"
    "github.com/tusklang/go"
)

type HelloWebConfig struct {
    App struct {
        Name    string `tsk:"name"`
        Version string `tsk:"version"`
    } `tsk:"app"`
    
    Server struct {
        Host  string `tsk:"host"`
        Port  int    `tsk:"port"`
        Debug bool   `tsk:"debug"`
    } `tsk:"server"`
    
    Features struct {
        EnableCORS    bool   `tsk:"enable_cors"`
        RateLimit     int    `tsk:"rate_limit"`
        CacheTimeout  string `tsk:"cache_timeout"`
    } `tsk:"features"`
}

func main() {
    // Load configuration
    parser := tusklanggo.NewEnhancedParser()
    data, err := parser.ParseFile("hello-web.tsk")
    if err != nil {
        panic(err)
    }
    
    var config HelloWebConfig
    err = tusklanggo.UnmarshalTSK(data, &config)
    if err != nil {
        panic(err)
    }
    
    // Set Gin mode
    if !config.Server.Debug {
        gin.SetMode(gin.ReleaseMode)
    }
    
    // Create Gin router
    r := gin.Default()
    
    // Add CORS if enabled
    if config.Features.EnableCORS {
        r.Use(func(c *gin.Context) {
            c.Header("Access-Control-Allow-Origin", "*")
            c.Next()
        })
    }
    
    // Hello world route
    r.GET("/", func(c *gin.Context) {
        c.JSON(200, gin.H{
            "message": fmt.Sprintf("Hello from %s v%s!", config.App.Name, config.App.Version),
            "config":  config,
        })
    })
    
    // Health check
    r.GET("/health", func(c *gin.Context) {
        c.JSON(200, gin.H{
            "status": "healthy",
            "app":    config.App.Name,
        })
    })
    
    fmt.Printf("🌐 Starting %s v%s on %s:%d\n", 
        config.App.Name, config.App.Version, config.Server.Host, config.Server.Port)
    
    // Start server
    r.Run(fmt.Sprintf("%s:%d", config.Server.Host, config.Server.Port))
}
```

## 🎯 Advanced Hello World

### Multi-File Configuration

```go
// main.tsk
$app_name: "Hello Advanced"
$version: "2.0.0"

[app]
name: $app_name
version: $version

[database]
host: @config.tsk.get("db_host")
port: @config.tsk.get("db_port")
name: @config.tsk.get("db_name")

[features]
enabled: @features.tsk.get("enabled_features")
settings: @features.tsk.get("feature_settings")
```

```go
// config.tsk
db_host: "localhost"
db_port: 5432
db_name: "hello_advanced"
```

```go
// features.tsk
enabled_features: ["database", "caching", "monitoring"]
feature_settings: {
    cache_timeout: "10m"
    max_connections: 100
    debug_mode: true
}
```

```go
// main.go - Multi-file configuration
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
db_name: "hello_advanced"
`)
    
    parser.LinkFile("features.tsk", `
enabled_features: ["database", "caching", "monitoring"]
feature_settings: {
    cache_timeout: "10m"
    max_connections: 100
    debug_mode: true
}
`)
    
    data, err := parser.ParseFile("main.tsk")
    if err != nil {
        panic(err)
    }
    
    fmt.Printf("🎯 %s v%s\n", 
        data["app"].(map[string]interface{})["name"],
        data["app"].(map[string]interface{})["version"])
    fmt.Printf("🗄️ Database: %s:%v/%s\n",
        data["database"].(map[string]interface{})["host"],
        data["database"].(map[string]interface{})["port"],
        data["database"].(map[string]interface{})["name"])
    fmt.Printf("⚡ Features: %v\n", data["features"].(map[string]interface{})["enabled"])
}
```

## 🔍 Testing Your Hello World

### Validation and Testing

```bash
# Validate TSK syntax
tusk validate hello.tsk

# Parse and show structure
tusk parse hello.tsk --format json

# Test with different environments
APP_ENV=production go run main.go
APP_ENV=development go run main.go

# Interactive shell
tusk shell hello.tsk
```

### Performance Testing

```go
// benchmark.go
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
        _, err := parser.ParseFile("hello.tsk")
        if err != nil {
            panic(err)
        }
    }
    
    duration := time.Since(start)
    fmt.Printf("⚡ Performance: %d parses in %v (%.2f parses/sec)\n", 
        1000, duration, float64(1000)/duration.Seconds())
}
```

## 🎉 What You've Accomplished

In this Hello World guide, you've learned:

1. **Basic TSK File Creation** - Simple configuration with multiple syntax styles
2. **Go Integration** - Type-safe struct mapping with TuskLang
3. **Database Integration** - Direct SQL queries in configuration
4. **Environment Awareness** - Dynamic configuration based on environment
5. **FUJSEN Functions** - Executable JavaScript in configuration
6. **Web Framework Integration** - Gin framework with TuskLang
7. **Multi-File Configuration** - Cross-file communication and references
8. **Testing and Validation** - CLI tools and performance testing

## 🚀 Next Steps

Now that you have your Hello World working:

1. **Explore Basic Syntax** - Learn about data types, variables, and operators
2. **Database Integration** - Master database queries and transactions
3. **Advanced Features** - Discover @ operators, caching, and monitoring
4. **Web Development** - Build full web applications with TuskLang
5. **Deployment** - Deploy your applications to production

## 📚 Resources

- **Full Documentation**: [docs.tusklang.org/go](https://docs.tusklang.org/go)
- **Examples Repository**: [github.com/tusklang/go/examples](https://github.com/tusklang/go/examples)
- **Community**: [community.tusklang.org](https://community.tusklang.org)
- **CLI Reference**: [cli.tusklang.org](https://cli.tusklang.org)

---

**"We don't bow to any king"** - You've just created your first TuskLang application in Go! Welcome to the future of configuration management where your config files have a heartbeat and your database queries live in your configuration. The revolution has begun! 🚀 