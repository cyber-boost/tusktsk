# 🎛️ Basic Syntax - TuskLang for C# - "Your Syntax, Your Rules"

**Master the flexibility of TuskLang syntax - Choose your style, break the rules!**

TuskLang gives you unprecedented syntax flexibility. Use traditional INI-style, JSON-like objects, XML-inspired tags, or mix them all together. Your configuration, your preferred syntax.

## 🎯 Syntax Philosophy

### "We Don't Bow to Any King"
- **Traditional INI**: `[section]` with `key: value`
- **JSON-like**: `{ "section": { "key": "value" } }`
- **XML-inspired**: `<section><key>value</key></section>`
- **Mixed styles**: Combine all three in the same file

### Why Multiple Syntax Styles?
- **Developer preference** - Use what feels natural
- **Legacy compatibility** - Migrate existing configs gradually
- **Team flexibility** - Different developers, different styles
- **Context appropriateness** - Choose the right tool for the job

## 📝 Basic Syntax Styles

### 1. Traditional INI Style (Default)

```ini
# app.tsk - Traditional INI style
[app]
name: "MyAwesomeApp"
version: "1.0.0"
debug: true

[database]
host: "localhost"
port: 5432
name: "myapp"
user: "postgres"
password: "secret"

[features]
cache_enabled: true
cache_ttl: "5m"
max_connections: 100
```

### 2. JSON-like Style

```json
// app.tsk - JSON-like style
{
    "app": {
        "name": "MyAwesomeApp",
        "version": "1.0.0",
        "debug": true
    },
    "database": {
        "host": "localhost",
        "port": 5432,
        "name": "myapp",
        "user": "postgres",
        "password": "secret"
    },
    "features": {
        "cache_enabled": true,
        "cache_ttl": "5m",
        "max_connections": 100
    }
}
```

### 3. XML-inspired Style

```xml
<!-- app.tsk - XML-inspired style -->
<app>
    <name>MyAwesomeApp</name>
    <version>1.0.0</version>
    <debug>true</debug>
</app>
<database>
    <host>localhost</host>
    <port>5432</port>
    <name>myapp</name>
    <user>postgres</user>
    <password>secret</password>
</database>
<features>
    <cache_enabled>true</cache_enabled>
    <cache_ttl>5m</cache_ttl>
    <max_connections>100</max_connections>
</features>
```

### 4. Mixed Style (Revolutionary!)

```ini
# app.tsk - Mixed syntax styles
[app]
name: "MyAwesomeApp"
version: "1.0.0"

# JSON-style nested objects
database {
    host: "localhost"
    port: 5432
    credentials {
        user: "postgres"
        password: "secret"
    }
}

# XML-style for complex structures
<features>
    <cache>
        <enabled>true</enabled>
        <ttl>5m</ttl>
    </cache>
    <security>
        <ssl_enabled>true</ssl_enabled>
        <max_connections>100</max_connections>
    </security>
</features>
```

## 🔧 C# Integration Examples

### Basic Parsing

```csharp
using TuskLang;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        var parser = new TuskLang();
        
        // Parse any syntax style
        var config = parser.ParseFile("app.tsk");
        
        // Access values consistently regardless of syntax
        var appName = config["app"]["name"];
        var dbHost = config["database"]["host"];
        var cacheEnabled = config["features"]["cache_enabled"];
        
        Console.WriteLine($"App: {appName}");
        Console.WriteLine($"Database: {dbHost}");
        Console.WriteLine($"Cache: {cacheEnabled}");
    }
}
```

### Strongly-Typed Configuration

```csharp
// Configuration classes
public class AppConfig
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public bool Debug { get; set; }
}

public class DatabaseConfig
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Name { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class FeaturesConfig
{
    public bool CacheEnabled { get; set; }
    public string CacheTtl { get; set; } = string.Empty;
    public int MaxConnections { get; set; }
}

public class TuskConfig
{
    public AppConfig App { get; set; } = new();
    public DatabaseConfig Database { get; set; } = new();
    public FeaturesConfig Features { get; set; } = new();
}

// Usage
var parser = new TuskLang();
var config = parser.ParseFile<TuskConfig>("app.tsk");

Console.WriteLine($"App: {config.App.Name} v{config.App.Version}");
Console.WriteLine($"Database: {config.Database.Host}:{config.Database.Port}");
Console.WriteLine($"Cache TTL: {config.Features.CacheTtl}");
```

## 🔄 Data Types and Values

### Supported Data Types

```ini
# app.tsk - All data types
[types]
# Strings
app_name: "MyAwesomeApp"
description: "A revolutionary configuration system"

# Numbers
port: 8080
max_connections: 100
timeout: 30.5

# Booleans
debug: true
ssl_enabled: false
cache_enabled: true

# Arrays
allowed_hosts: ["localhost", "127.0.0.1", "0.0.0.0"]
ports: [80, 443, 8080, 8443]

# Objects
database {
    host: "localhost"
    port: 5432
}

# Null values
optional_setting: null
```

### C# Type Mapping

```csharp
// TuskLang automatically maps types
var config = parser.ParseFile("app.tsk");

// String values
string appName = config["types"]["app_name"].ToString();
string description = config["types"]["description"].ToString();

// Numeric values
int port = Convert.ToInt32(config["types"]["port"]);
double timeout = Convert.ToDouble(config["types"]["timeout"]);

// Boolean values
bool debug = Convert.ToBoolean(config["types"]["debug"]);
bool sslEnabled = Convert.ToBoolean(config["types"]["ssl_enabled"]);

// Arrays
var allowedHosts = config["types"]["allowed_hosts"] as List<object>;
var ports = config["types"]["ports"] as List<object>;

// Objects
var database = config["types"]["database"] as Dictionary<string, object>;
string dbHost = database["host"].ToString();
int dbPort = Convert.ToInt32(database["port"]);
```

## 🌐 Global Variables and Interpolation

### Global Variable Declaration

```ini
# app.tsk - Global variables
$app_name: "MyAwesomeApp"
$version: "1.0.0"
$environment: @env("APP_ENV", "development")

[app]
name: $app_name
version: $version
environment: $environment

[paths]
logs: "/var/log/${app_name}"
data: "/var/lib/${app_name}/v${version}"
cache: "/tmp/${app_name}_${environment}"
```

### C# Variable Access

```csharp
var parser = new TuskLang();
var config = parser.ParseFile("app.tsk");

// Access global variables
var appName = config["$app_name"];
var version = config["$version"];
var environment = config["$environment"];

// Access interpolated values
var logPath = config["paths"]["logs"];
var dataPath = config["paths"]["data"];
var cachePath = config["paths"]["cache"];

Console.WriteLine($"App: {appName}");
Console.WriteLine($"Version: {version}");
Console.WriteLine($"Environment: {environment}");
Console.WriteLine($"Logs: {logPath}");
```

## 🔗 Cross-File References

### File Inclusion

```ini
# main.tsk - Main configuration
@include("global.tsk")
@include("database.tsk")
@include("features.tsk")

[app]
name: $app_name
version: $version
```

```ini
# global.tsk - Global variables
$app_name: "MyAwesomeApp"
$version: "1.0.0"
$company: "TuskLang Inc."
```

```ini
# database.tsk - Database configuration
[database]
host: "localhost"
port: 5432
name: "myapp"
```

```ini
# features.tsk - Feature flags
[features]
cache_enabled: true
debug_mode: @if($environment == "development", true, false)
```

### C# File Management

```csharp
var parser = new TuskLang();

// Link external files
parser.LinkFile("global.tsk", File.ReadAllText("global.tsk"));
parser.LinkFile("database.tsk", File.ReadAllText("database.tsk"));
parser.LinkFile("features.tsk", File.ReadAllText("features.tsk"));

// Parse main file with includes
var config = parser.ParseFile("main.tsk");

// Access values from included files
var appName = config["app"]["name"];
var dbHost = config["database"]["host"];
var cacheEnabled = config["features"]["cache_enabled"];
```

## 🎛️ Conditional Logic

### Basic Conditionals

```ini
# app.tsk - Conditional configuration
$environment: @env("APP_ENV", "development")

[server]
host: @if($environment == "production", "0.0.0.0", "localhost")
port: @if($environment == "production", 80, 8080)
ssl: @if($environment == "production", true, false)

[logging]
level: @if($environment == "production", "error", "debug")
format: @if($environment == "production", "json", "text")
file: @if($environment == "production", "/var/log/app.log", "console")
```

### Complex Conditionals

```ini
# app.tsk - Advanced conditionals
$environment: @env("APP_ENV", "development")
$debug: @env("DEBUG", "false")

[features]
debug_mode: @if($environment == "development" || $debug == "true", true, false)
cache_enabled: @if($environment == "production", true, false)
log_level: @if($environment == "production", "error", @if($debug == "true", "debug", "info"))

[security]
ssl_required: @if($environment == "production", true, false)
cors_origins: @if($environment == "production", ["https://myapp.com"], ["*"])
```

### C# Conditional Processing

```csharp
// Set environment variables
Environment.SetEnvironmentVariable("APP_ENV", "production");
Environment.SetEnvironmentVariable("DEBUG", "true");

var parser = new TuskLang();
var config = parser.ParseFile("app.tsk");

// Access conditional values
var serverHost = config["server"]["host"];
var serverPort = config["server"]["port"];
var sslEnabled = config["server"]["ssl"];
var logLevel = config["logging"]["level"];
var debugMode = config["features"]["debug_mode"];

Console.WriteLine($"Server: {serverHost}:{serverPort}");
Console.WriteLine($"SSL: {sslEnabled}");
Console.WriteLine($"Log Level: {logLevel}");
Console.WriteLine($"Debug Mode: {debugMode}");
```

## 📊 Arrays and Objects

### Array Syntax

```ini
# app.tsk - Arrays in different syntax styles
[traditional]
allowed_hosts: ["localhost", "127.0.0.1", "0.0.0.0"]
ports: [80, 443, 8080, 8443]

json_style {
    users: ["alice", "bob", "charlie"]
    roles: ["admin", "user", "guest"]
}

<xml_style>
    <servers>
        <server>server1.example.com</server>
        <server>server2.example.com</server>
        <server>server3.example.com</server>
    </servers>
</xml_style>
```

### Object Syntax

```ini
# app.tsk - Objects in different syntax styles
[traditional]
database {
    host: "localhost"
    port: 5432
    credentials {
        user: "postgres"
        password: "secret"
    }
}

json_style {
    "cache": {
        "enabled": true,
        "ttl": "5m",
        "backend": {
            "type": "redis",
            "host": "localhost",
            "port": 6379
        }
    }
}

<xml_style>
    <security>
        <ssl>
            <enabled>true</enabled>
            <cert_path>/etc/ssl/cert.pem</cert_path>
            <key_path>/etc/ssl/key.pem</key_path>
        </ssl>
    </security>
</xml_style>
```

### C# Array and Object Access

```csharp
var parser = new TuskLang();
var config = parser.ParseFile("app.tsk");

// Access arrays
var allowedHosts = config["traditional"]["allowed_hosts"] as List<object>;
var ports = config["traditional"]["ports"] as List<object>;

foreach (var host in allowedHosts)
{
    Console.WriteLine($"Allowed host: {host}");
}

foreach (var port in ports)
{
    Console.WriteLine($"Port: {port}");
}

// Access nested objects
var database = config["traditional"]["database"] as Dictionary<string, object>;
var credentials = database["credentials"] as Dictionary<string, object>;

string dbHost = database["host"].ToString();
int dbPort = Convert.ToInt32(database["port"]);
string dbUser = credentials["user"].ToString();
string dbPassword = credentials["password"].ToString();

Console.WriteLine($"Database: {dbHost}:{dbPort}");
Console.WriteLine($"User: {dbUser}");
```

## 🔒 Comments and Documentation

### Comment Styles

```ini
# app.tsk - Comments in TuskLang
# This is a traditional comment

[app]
name: "MyAwesomeApp"  # Inline comment
version: "1.0.0"      # Another inline comment

# Multi-line comment block
# This section contains database configuration
# for the application
[database]
host: "localhost"     # Database host
port: 5432           # Database port
name: "myapp"        # Database name

/*
 * This is a block comment
 * It can span multiple lines
 * Useful for complex documentation
 */
[features]
cache_enabled: true   # Enable caching
cache_ttl: "5m"      # Cache TTL in minutes
```

### C# Comment Processing

```csharp
var parser = new TuskLang();

// Comments are automatically stripped during parsing
var config = parser.ParseFile("app.tsk");

// Access values (comments are ignored)
var appName = config["app"]["name"];
var dbHost = config["database"]["host"];
var cacheEnabled = config["features"]["cache_enabled"];

Console.WriteLine($"App: {appName}");
Console.WriteLine($"Database: {dbHost}");
Console.WriteLine($"Cache: {cacheEnabled}");
```

## 🚨 Error Handling

### Syntax Error Handling

```csharp
var parser = new TuskLang();

try
{
    var config = parser.ParseFile("app.tsk");
    Console.WriteLine("Configuration parsed successfully");
}
catch (TuskLangParseException ex)
{
    Console.WriteLine($"Parse error: {ex.Message}");
    Console.WriteLine($"Line: {ex.LineNumber}");
    Console.WriteLine($"Column: {ex.ColumnNumber}");
}
catch (Exception ex)
{
    Console.WriteLine($"Unexpected error: {ex.Message}");
}
```

### Validation

```csharp
var parser = new TuskLang();

// Validate syntax without parsing
bool isValid = parser.Validate("app.tsk");
if (isValid)
{
    Console.WriteLine("Configuration file is valid");
}
else
{
    Console.WriteLine("Configuration file has syntax errors");
}

// Validate with detailed errors
var validationResult = parser.ValidateWithDetails("app.tsk");
if (!validationResult.IsValid)
{
    foreach (var error in validationResult.Errors)
    {
        Console.WriteLine($"Error: {error.Message} at line {error.Line}");
    }
}
```

## 🎯 Best Practices

### 1. Choose Your Style Consistently

```ini
# Good: Consistent INI style
[app]
name: "MyApp"
version: "1.0.0"

[database]
host: "localhost"
port: 5432
```

```ini
# Good: Consistent JSON style
{
    "app": {
        "name": "MyApp",
        "version": "1.0.0"
    },
    "database": {
        "host": "localhost",
        "port": 5432
    }
}
```

### 2. Use Global Variables for Reusability

```ini
# Good: Global variables
$app_name: "MyApp"
$version: "1.0.0"

[app]
name: $app_name
version: $version

[paths]
logs: "/var/log/${app_name}"
data: "/var/lib/${app_name}"
```

### 3. Leverage Conditional Logic

```ini
# Good: Environment-aware configuration
$environment: @env("APP_ENV", "development")

[server]
host: @if($environment == "production", "0.0.0.0", "localhost")
port: @if($environment == "production", 80, 8080)
```

### 4. Document Your Configuration

```ini
# Good: Well-documented configuration
# Application Configuration
# This file contains the main application settings
[app]
name: "MyApp"        # Application name
version: "1.0.0"     # Application version

# Database Configuration
# Connection settings for the primary database
[database]
host: "localhost"    # Database host
port: 5432          # Database port
```

## 🔥 Advanced Syntax Features

### Template Strings

```ini
# app.tsk - Template strings
$user: "alice"
$domain: "example.com"

[email]
from: "${user}@${domain}"
to: "admin@${domain}"
subject: "Hello from ${user}"
```

### Dynamic Arrays

```ini
# app.tsk - Dynamic arrays
$environments: ["dev", "staging", "prod"]

[deployment]
targets: $environments
urls: ["https://${environments[0]}.example.com", "https://${environments[1]}.example.com", "https://${environments[2]}.example.com"]
```

### Nested Conditionals

```ini
# app.tsk - Nested conditionals
$environment: @env("APP_ENV", "development")
$debug: @env("DEBUG", "false")

[logging]
level: @if($environment == "production", "error", @if($debug == "true", "debug", "info"))
format: @if($environment == "production", "json", @if($debug == "true", "detailed", "simple"))
```

## 🎉 You're Ready!

You've mastered the basic syntax of TuskLang! You can now:

- ✅ **Choose your syntax style** - INI, JSON, XML, or mixed
- ✅ **Use global variables** - For reusability and consistency
- ✅ **Implement conditionals** - Environment-aware configuration
- ✅ **Work with arrays and objects** - Complex data structures
- ✅ **Handle errors gracefully** - Robust error handling
- ✅ **Follow best practices** - Clean, maintainable configuration

## 🔥 What's Next?

Ready to dive deeper? Explore:

1. **[Database Integration](004-database-integration-csharp.md)** - Connect to real databases
2. **[Advanced Features](005-advanced-features-csharp.md)** - Unleash the full power
3. **[@ Operator System](006-operators-csharp.md)** - Master the @ operator ecosystem
4. **[Performance Optimization](007-performance-csharp.md)** - Optimize your configuration

---

**"We don't bow to any king" - Your syntax, your rules.**

Choose your style and break free from configuration constraints! 🚀 