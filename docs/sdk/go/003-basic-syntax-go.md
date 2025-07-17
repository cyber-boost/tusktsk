# 📝 TuskLang Go Basic Syntax Guide

**"We don't bow to any king" - Go Edition**

Master TuskLang syntax with Go. This guide covers all syntax styles, data types, struct mapping, and advanced features with comprehensive Go examples.

## 🎨 Syntax Flexibility

TuskLang supports multiple syntax styles - choose what feels natural to you:

### Traditional INI-Style (Default)

```go
// config.tsk
[app]
name: "My Go App"
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
```

```go
// main.go
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
        Host string `tsk:"host"`
        Port int    `tsk:"port"`
        Name string `tsk:"name"`
    } `tsk:"database"`
}
```

### JSON-Like Objects

```go
// config.tsk
app {
    name: "My Go App"
    version: "1.0.0"
    debug: true
}

server {
    host: "localhost"
    port: 8080
    ssl: false
}

database {
    host: "localhost"
    port: 5432
    name: "myapp"
}
```

### XML-Inspired Syntax

```go
// config.tsk
app >
    name: "My Go App"
    version: "1.0.0"
    debug: true
<

server >
    host: "localhost"
    port: 8080
    ssl: false
<

database >
    host: "localhost"
    port: 5432
    name: "myapp"
<
```

## 📊 Data Types

### Basic Types

```go
// config.tsk
[types]
string_value: "Hello, TuskLang!"
integer_value: 42
float_value: 3.14159
boolean_value: true
null_value: null
```

```go
// main.go
type Types struct {
    StringValue  string  `tsk:"string_value"`
    IntegerValue int     `tsk:"integer_value"`
    FloatValue   float64 `tsk:"float_value"`
    BooleanValue bool    `tsk:"boolean_value"`
    NullValue    *string `tsk:"null_value"` // Use pointer for nullable values
} `tsk:"types"`
```

### Arrays

```go
// config.tsk
[arrays]
string_array: ["apple", "banana", "cherry"]
number_array: [1, 2, 3, 4, 5]
mixed_array: ["hello", 42, true, null]
nested_array: [["a", "b"], ["c", "d"]]
```

```go
// main.go
type Arrays struct {
    StringArray  []string      `tsk:"string_array"`
    NumberArray  []int         `tsk:"number_array"`
    MixedArray   []interface{} `tsk:"mixed_array"`
    NestedArray  [][]string    `tsk:"nested_array"`
} `tsk:"arrays"`
```

### Objects/Maps

```go
// config.tsk
[objects]
simple_object: {
    name: "John"
    age: 30
    active: true
}

nested_object: {
    user: {
        id: 1
        name: "Alice"
        settings: {
            theme: "dark"
            notifications: true
        }
    }
}

mixed_object: {
    strings: ["a", "b", "c"]
    numbers: [1, 2, 3]
    nested: {
        value: "nested"
    }
}
```

```go
// main.go
type Objects struct {
    SimpleObject map[string]interface{} `tsk:"simple_object"`
    NestedObject struct {
        User struct {
            ID       int    `tsk:"id"`
            Name     string `tsk:"name"`
            Settings struct {
                Theme          string `tsk:"theme"`
                Notifications  bool   `tsk:"notifications"`
            } `tsk:"settings"`
        } `tsk:"user"`
    } `tsk:"nested_object"`
    MixedObject map[string]interface{} `tsk:"mixed_object"`
} `tsk:"objects"`
```

## 🔗 Variables and Interpolation

### Global Variables

```go
// config.tsk
$app_name: "My Go App"
$version: "1.0.0"
$environment: @env("APP_ENV", "development")

[app]
name: $app_name
version: $version
debug: @if($environment == "development", true, false)

[paths]
log_file: "/var/log/${app_name}.log"
config_file: "/etc/${app_name}/config.json"
data_dir: "/var/lib/${app_name}/v${version}"
```

```go
// main.go
type Config struct {
    App struct {
        Name    string `tsk:"name"`
        Version string `tsk:"version"`
        Debug   bool   `tsk:"debug"`
    } `tsk:"app"`
    
    Paths struct {
        LogFile    string `tsk:"log_file"`
        ConfigFile string `tsk:"config_file"`
        DataDir    string `tsk:"data_dir"`
    } `tsk:"paths"`
} `tsk:""`
```

### Cross-Section References

```go
// config.tsk
[database]
host: "localhost"
port: 5432
name: "myapp"

[connection]
url: "postgres://${database.host}:${database.port}/${database.name}"
timeout: 30
retries: 3
```

```go
// main.go
type Config struct {
    Database struct {
        Host string `tsk:"host"`
        Port int    `tsk:"port"`
        Name string `tsk:"name"`
    } `tsk:"database"`
    
    Connection struct {
        URL     string `tsk:"url"`
        Timeout int    `tsk:"timeout"`
        Retries int    `tsk:"retries"`
    } `tsk:"connection"`
} `tsk:""`
```

## 🔧 Advanced Struct Mapping

### Custom Field Names

```go
// config.tsk
[user]
first_name: "John"
last_name: "Doe"
email_address: "john@example.com"
is_active: true
created_at: "2024-01-15T10:30:00Z"
```

```go
// main.go
type User struct {
    FirstName   string    `tsk:"first_name"`
    LastName    string    `tsk:"last_name"`
    Email       string    `tsk:"email_address"`
    Active      bool      `tsk:"is_active"`
    CreatedAt   time.Time `tsk:"created_at"`
} `tsk:"user"`
```

### Nested Structs

```go
// config.tsk
[application]
name: "My App"
version: "1.0.0"

[application.server]
host: "localhost"
port: 8080
ssl: false

[application.database]
host: "localhost"
port: 5432
name: "myapp"
```

```go
// main.go
type Application struct {
    Name     string `tsk:"name"`
    Version  string `tsk:"version"`
    Server   struct {
        Host string `tsk:"host"`
        Port int    `tsk:"port"`
        SSL  bool   `tsk:"ssl"`
    } `tsk:"server"`
    Database struct {
        Host string `tsk:"host"`
        Port int    `tsk:"port"`
        Name string `tsk:"name"`
    } `tsk:"database"`
} `tsk:"application"`
```

### Arrays of Structs

```go
// config.tsk
[servers]
production {
    host: "prod.example.com"
    port: 80
    ssl: true
}

staging {
    host: "staging.example.com"
    port: 8080
    ssl: false
}

development {
    host: "localhost"
    port: 3000
    ssl: false
}
```

```go
// main.go
type Server struct {
    Host string `tsk:"host"`
    Port int    `tsk:"port"`
    SSL  bool   `tsk:"ssl"`
}

type Config struct {
    Servers map[string]Server `tsk:"servers"`
} `tsk:""`
```

## 🎯 Conditional Logic

### Basic Conditionals

```go
// config.tsk
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
```

```go
// main.go
type Config struct {
    Logging struct {
        Level  string `tsk:"level"`
        Format string `tsk:"format"`
        File   string `tsk:"file"`
    } `tsk:"logging"`
    
    Security struct {
        SSL bool                   `tsk:"ssl"`
        CORS map[string]interface{} `tsk:"cors"`
    } `tsk:"security"`
} `tsk:""`
```

### Complex Conditionals

```go
// config.tsk
$environment: @env("APP_ENV", "development")
$debug: @env("DEBUG", "false")

[features]
cache_enabled: @if($environment == "production" && $debug == "false", true, false)
rate_limiting: @if($environment == "production", {
    requests_per_minute: 1000
    burst_size: 100
}, {
    requests_per_minute: 10000
    burst_size: 1000
})
```

## 🔄 Loops and Iterations

### Array Operations

```go
// config.tsk
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
```

```go
// main.go
type Config struct {
    Users struct {
        AdminUsers []string            `tsk:"admin_users"`
        Roles      map[string][]string `tsk:"roles"`
    } `tsk:"users"`
    
    Permissions struct {
        UserPermissions []string `tsk:"user_permissions"`
        IsAdmin         bool     `tsk:"is_admin"`
    } `tsk:"permissions"`
} `tsk:""`
```

## 📁 File Organization

### Multiple Files

```go
// main.tsk
$app_name: "My App"
$version: "1.0.0"

[database]
host: @config.tsk.get("db_host")
port: @config.tsk.get("db_port")
name: @config.tsk.get("db_name")
```

```go
// config.tsk
db_host: "localhost"
db_port: 5432
db_name: "myapp"
db_user: "postgres"
db_password: @env("DB_PASSWORD")
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
db_user: "postgres"
db_password: @env("DB_PASSWORD")
`)
    
    data, err := parser.ParseFile("main.tsk")
    if err != nil {
        panic(err)
    }
    
    fmt.Printf("Database: %s:%v/%s\n", 
        data["database"].(map[string]interface{})["host"],
        data["database"].(map[string]interface{})["port"],
        data["database"].(map[string]interface{})["name"])
}
```

## 🔐 Security Features

### Environment Variables

```go
// config.tsk
[secrets]
api_key: @env("API_KEY")
database_password: @env("DB_PASSWORD")
jwt_secret: @env("JWT_SECRET")

[config]
debug_mode: @env("DEBUG", "false")
log_level: @env("LOG_LEVEL", "info")
```

### Validation

```go
// config.tsk
[user]
email: @validate.email(@request.email)
website: @validate.url(@request.website)
age: @validate.range(@request.age, 0, 150)
password: @validate.password(@request.password)
```

```go
// main.go
type User struct {
    Email    string `tsk:"email"`
    Website  string `tsk:"website"`
    Age      int    `tsk:"age"`
    Password string `tsk:"password"`
} `tsk:"user"`
```

## 🚀 Performance Features

### Caching

```go
// config.tsk
[expensive_operation]
result: @cache("5m", @query("SELECT COUNT(*) FROM large_table"))
user_stats: @cache("1h", @query("SELECT * FROM user_statistics"))
```

### Lazy Loading

```go
// config.tsk
[features]
expensive_feature: @lazy(@query("SELECT * FROM expensive_table"))
optional_data: @lazy(@file.read("large_data.json"))
```

## 🔧 Error Handling

### Graceful Fallbacks

```go
// config.tsk
[config]
database_url: @env("DATABASE_URL", "postgres://localhost/myapp")
api_endpoint: @env("API_ENDPOINT", "https://api.example.com")
timeout: @env("TIMEOUT", "30")
```

```go
// main.go
type Config struct {
    DatabaseURL  string `tsk:"database_url"`
    APIEndpoint  string `tsk:"api_endpoint"`
    Timeout      int    `tsk:"timeout"`
} `tsk:"config"`

func main() {
    parser := tusklanggo.NewEnhancedParser()
    
    data, err := parser.ParseFile("config.tsk")
    if err != nil {
        // Handle parsing errors
        log.Printf("Failed to parse config: %v", err)
        return
    }
    
    var config Config
    err = tusklanggo.UnmarshalTSK(data, &config)
    if err != nil {
        // Handle unmarshaling errors
        log.Printf("Failed to unmarshal config: %v", err)
        return
    }
    
    // Use configuration safely
    fmt.Printf("Using database: %s\n", config.DatabaseURL)
}
```

## 📊 Type Conversion

### Automatic Type Conversion

```go
// config.tsk
[conversions]
string_to_int: "42"
string_to_float: "3.14159"
string_to_bool: "true"
int_to_string: 123
float_to_string: 2.718
```

```go
// main.go
type Conversions struct {
    StringToInt    int     `tsk:"string_to_int"`
    StringToFloat  float64 `tsk:"string_to_float"`
    StringToBool   bool    `tsk:"string_to_bool"`
    IntToString    string  `tsk:"int_to_string"`
    FloatToString  string  `tsk:"float_to_string"`
} `tsk:"conversions"`
```

## 🎯 Best Practices

### 1. Use Descriptive Section Names

```go
// Good
[application_server]
host: "localhost"
port: 8080

[database_connection]
host: "localhost"
port: 5432

// Avoid
[server]
host: "localhost"
port: 8080

[db]
host: "localhost"
port: 5432
```

### 2. Group Related Settings

```go
// config.tsk
[application]
name: "My App"
version: "1.0.0"
environment: @env("APP_ENV", "development")

[application.server]
host: "localhost"
port: 8080
ssl: false

[application.database]
host: "localhost"
port: 5432
name: "myapp"
```

### 3. Use Environment Variables for Secrets

```go
// config.tsk
[secrets]
api_key: @env("API_KEY")
database_password: @env("DB_PASSWORD")
jwt_secret: @env("JWT_SECRET")

[config]
debug: @env("DEBUG", "false")
log_level: @env("LOG_LEVEL", "info")
```

### 4. Validate Configuration

```go
// main.go
func validateConfig(config *Config) error {
    if config.Server.Port <= 0 || config.Server.Port > 65535 {
        return fmt.Errorf("invalid server port: %d", config.Server.Port)
    }
    
    if config.Database.Host == "" {
        return fmt.Errorf("database host is required")
    }
    
    return nil
}

func main() {
    // ... parse config ...
    
    if err := validateConfig(&config); err != nil {
        log.Fatalf("Invalid configuration: %v", err)
    }
}
```

## 🔍 Debugging

### Enable Debug Mode

```go
// main.go
func main() {
    parser := tusklanggo.NewEnhancedParser()
    parser.SetDebug(true) // Enable debug logging
    
    data, err := parser.ParseFile("config.tsk")
    if err != nil {
        panic(err)
    }
    
    fmt.Printf("Parsed data: %+v\n", data)
}
```

### Validate Syntax

```bash
# Validate TSK file syntax
tusk validate config.tsk

# Show parsed structure
tusk parse config.tsk --format json

# Interactive shell
tusk shell config.tsk
```

## 📚 Summary

You've learned:

1. **Multiple Syntax Styles** - Traditional, JSON-like, and XML-inspired
2. **Data Types** - Strings, numbers, booleans, arrays, objects
3. **Variables and Interpolation** - Global variables and cross-references
4. **Struct Mapping** - Type-safe configuration with Go structs
5. **Conditional Logic** - Environment-based configuration
6. **File Organization** - Multi-file configurations
7. **Security Features** - Environment variables and validation
8. **Performance Features** - Caching and lazy loading
9. **Error Handling** - Graceful fallbacks and validation
10. **Best Practices** - Clean, maintainable configurations

## 🚀 Next Steps

Now that you understand basic syntax:

1. **Explore @ Operators** - Learn about @date, @cache, @metrics, etc.
2. **Database Integration** - Execute queries in configuration
3. **Advanced Features** - FUJSEN, machine learning, real-time monitoring
4. **Web Framework Integration** - Use with Gin, Echo, and other frameworks
5. **Deployment** - Docker, Kubernetes, and cloud deployment

---

**"We don't bow to any king"** - You now have the power to create flexible, type-safe configurations that adapt to your preferred syntax style! 