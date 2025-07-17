# ⚡ Quick Start with TuskLang for C# - "Configuration with a Heartbeat"

**Get up and running in 5 minutes - No more static configuration hell!**

This guide will take you from zero to hero with TuskLang in C#. You'll build a dynamic configuration system that adapts to your environment, connects to databases, and uses your preferred syntax style.

## 🎯 What You'll Build

By the end of this guide, you'll have:
- ✅ A dynamic configuration system
- ✅ Database integration with real-time queries
- ✅ Environment-aware settings
- ✅ Cross-file configuration communication
- ✅ ASP.NET Core integration

## 🚀 Step 1: Create Your Project

### Create a New Console Application

```bash
# Create a new directory
mkdir TuskLangDemo
cd TuskLangDemo

# Create a new console application
dotnet new console

# Add TuskLang package
dotnet add package TuskLang
```

### Create a New ASP.NET Core Web Application

```bash
# Create a new web application
dotnet new web

# Add TuskLang package
dotnet add package TuskLang
```

## 📝 Step 2: Your First TSK Configuration

### Create `app.tsk`

```ini
# app.tsk - Your first TuskLang configuration
[app]
name: "TuskLang Demo"
version: "1.0.0"
environment: @env("APP_ENV", "development")

[database]
host: @if($environment == "production", "prod-db.example.com", "localhost")
port: 5432
name: "tuskdemo"
user: @env("DB_USER", "postgres")
password: @env.secure("DB_PASSWORD")

[features]
debug_mode: @if($environment != "production", true, false)
cache_enabled: true
cache_ttl: "5m"
```

## 🔧 Step 3: Basic C# Integration

### Console Application

```csharp
// Program.cs
using TuskLang;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        // Create parser instance
        var parser = new TuskLang();
        
        // Parse configuration file
        var config = parser.ParseFile("app.tsk");
        
        // Access configuration values
        var appName = config["app"]["name"];
        var environment = config["app"]["environment"];
        var dbHost = config["database"]["host"];
        
        Console.WriteLine($"🚀 {appName}");
        Console.WriteLine($"🌍 Environment: {environment}");
        Console.WriteLine($"🗄️ Database: {dbHost}");
        Console.WriteLine($"🐛 Debug Mode: {config["features"]["debug_mode"]}");
    }
}
```

### ASP.NET Core Application

```csharp
// Program.cs
using TuskLang;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add TuskLang services
builder.Services.AddTuskLang("app.tsk");

var app = builder.Build();

// Use configuration in endpoints
app.MapGet("/", (TuskConfig config) => 
{
    return Results.Ok(new
    {
        message = $"Hello from {config.App.Name}!",
        environment = config.App.Environment,
        version = config.App.Version,
        debugMode = config.Features.DebugMode
    });
});

app.MapGet("/config", (TuskConfig config) => Results.Ok(config));

app.Run();

// Configuration classes
public class TuskConfig
{
    public AppConfig App { get; set; } = new();
    public DatabaseConfig Database { get; set; } = new();
    public FeaturesConfig Features { get; set; } = new();
}

public class AppConfig
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
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
    public bool DebugMode { get; set; }
    public bool CacheEnabled { get; set; }
    public string CacheTtl { get; set; } = string.Empty;
}
```

## 🗄️ Step 4: Add Database Integration

### Create Database Configuration

```csharp
// DatabaseSetup.cs
using TuskLang.Adapters;
using System.Collections.Generic;

public class DatabaseSetup
{
    public static void Initialize()
    {
        // Create SQLite adapter
        var sqliteAdapter = new SQLiteAdapter("demo.db");
        
        // Create tables
        sqliteAdapter.Execute(@"
            CREATE TABLE IF NOT EXISTS users (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL,
                email TEXT UNIQUE,
                active BOOLEAN DEFAULT 1
            )
        ");
        
        // Insert sample data
        sqliteAdapter.Execute(@"
            INSERT OR IGNORE INTO users (name, email) VALUES 
            ('Alice', 'alice@example.com'),
            ('Bob', 'bob@example.com'),
            ('Charlie', 'charlie@example.com')
        ");
    }
}
```

### Update Your TSK File with Database Queries

```ini
# app.tsk - Now with database integration
[app]
name: "TuskLang Demo"
version: "1.0.0"
environment: @env("APP_ENV", "development")

[database]
host: @if($environment == "production", "prod-db.example.com", "localhost")
port: 5432
name: "tuskdemo"
user: @env("DB_USER", "postgres")
password: @env.secure("DB_PASSWORD")

[features]
debug_mode: @if($environment != "production", true, false)
cache_enabled: true
cache_ttl: "5m"

[stats]
total_users: @query("SELECT COUNT(*) FROM users")
active_users: @query("SELECT COUNT(*) FROM users WHERE active = 1")
recent_users: @query("SELECT COUNT(*) FROM users WHERE created_at > ?", @date.subtract("7d"))
```

### Update Your C# Code

```csharp
// Program.cs
using TuskLang;
using TuskLang.Adapters;

class Program
{
    static void Main()
    {
        // Initialize database
        DatabaseSetup.Initialize();
        
        // Create parser with database adapter
        var parser = new TuskLang();
        var sqliteAdapter = new SQLiteAdapter("demo.db");
        parser.SetDatabaseAdapter(sqliteAdapter);
        
        // Parse configuration with database queries
        var config = parser.ParseFile("app.tsk");
        
        // Display dynamic stats
        Console.WriteLine($"👥 Total Users: {config["stats"]["total_users"]}");
        Console.WriteLine($"✅ Active Users: {config["stats"]["active_users"]}");
        Console.WriteLine($"🆕 Recent Users: {config["stats"]["recent_users"]}");
    }
}
```

## 🔄 Step 5: Cross-File Communication

### Create Global Configuration

```ini
# global.tsk - Global variables and settings
$app_name: "TuskLang Demo"
$version: "1.0.0"
$company: "TuskLang Inc."

[paths]
logs: "/var/log/${app_name}"
data: "/var/lib/${app_name}"
cache: "/tmp/${app_name}"
```

### Create Environment-Specific Configuration

```ini
# development.tsk - Development environment
$environment: "development"

[server]
host: "localhost"
port: 8080
ssl: false

[logging]
level: "debug"
format: "text"
```

```ini
# production.tsk - Production environment
$environment: "production"

[server]
host: "0.0.0.0"
port: 80
ssl: true

[logging]
level: "error"
format: "json"
```

### Create Main Configuration

```ini
# app.tsk - Main configuration with cross-file references
@include("global.tsk")
@include("${environment}.tsk")

[app]
name: $app_name
version: $version
environment: $environment

[database]
host: @if($environment == "production", "prod-db.example.com", "localhost")
port: 5432
name: "tuskdemo"

[paths]
logs: $paths.logs
data: $paths.data
cache: $paths.cache
```

### Update Your C# Code

```csharp
// Program.cs
using TuskLang;

class Program
{
    static void Main()
    {
        // Set environment variable
        Environment.SetEnvironmentVariable("APP_ENV", "development");
        
        var parser = new TuskLang();
        var config = parser.ParseFile("app.tsk");
        
        Console.WriteLine($"🚀 {config["app"]["name"]} v{config["app"]["version"]}");
        Console.WriteLine($"🌍 Environment: {config["app"]["environment"]}");
        Console.WriteLine($"🌐 Server: {config["server"]["host"]}:{config["server"]["port"]}");
        Console.WriteLine($"📁 Logs: {config["paths"]["logs"]}");
    }
}
```

## 🎛️ Step 6: Advanced Features

### Environment Variables and Security

```csharp
// Set environment variables
Environment.SetEnvironmentVariable("APP_ENV", "production");
Environment.SetEnvironmentVariable("DB_USER", "admin");
Environment.SetEnvironmentVariable("DB_PASSWORD", "secret123");

// TSK file with secure variables
var tskContent = @"
[database]
user: @env(""DB_USER"")
password: @env.secure(""DB_PASSWORD"")
connection_string: @encrypt(""${user}:${password}@${host}"", ""AES-256-GCM"")
";
```

### Caching and Performance

```csharp
// TSK file with caching
var tskContent = @"
[performance]
expensive_query: @cache(""5m"", @query(""SELECT COUNT(*) FROM large_table""))
user_stats: @cache(""1h"", @query(""SELECT * FROM user_statistics""))
api_response: @cache(""30s"", @http(""GET"", ""https://api.example.com/data""))
";
```

### Machine Learning Integration

```csharp
// TSK file with ML features
var tskContent = @"
[ml]
optimal_cache_ttl: @learn(""cache_ttl"", ""5m"")
best_worker_count: @learn(""worker_count"", 4)
predicted_load: @predict(""server_load"", @metrics(""cpu_usage"", 75))
";
```

## 🧪 Step 7: Testing Your Configuration

### Create a Test Suite

```csharp
// ConfigurationTests.cs
using TuskLang;
using Xunit;

public class ConfigurationTests
{
    [Fact]
    public void TestBasicParsing()
    {
        var parser = new TuskLang();
        var config = parser.ParseFile("app.tsk");
        
        Assert.NotNull(config);
        Assert.Equal("TuskLang Demo", config["app"]["name"]);
    }
    
    [Fact]
    public void TestEnvironmentVariables()
    {
        Environment.SetEnvironmentVariable("APP_ENV", "test");
        
        var parser = new TuskLang();
        var config = parser.ParseFile("app.tsk");
        
        Assert.Equal("test", config["app"]["environment"]);
    }
    
    [Fact]
    public void TestDatabaseQueries()
    {
        DatabaseSetup.Initialize();
        
        var parser = new TuskLang();
        var sqliteAdapter = new SQLiteAdapter("demo.db");
        parser.SetDatabaseAdapter(sqliteAdapter);
        
        var config = parser.ParseFile("app.tsk");
        
        Assert.True(int.Parse(config["stats"]["total_users"].ToString()) > 0);
    }
}
```

### Run Tests

```bash
# Add test package
dotnet add package xunit
dotnet add package Microsoft.NET.Test.Sdk

# Run tests
dotnet test
```

## 🚀 Step 8: Run Your Application

### Console Application

```bash
dotnet run
```

Expected output:
```
🚀 TuskLang Demo v1.0.0
🌍 Environment: development
🗄️ Database: localhost
🐛 Debug Mode: True
👥 Total Users: 3
✅ Active Users: 3
🆕 Recent Users: 3
🌐 Server: localhost:8080
📁 Logs: /var/log/TuskLang Demo
```

### Web Application

```bash
dotnet run
```

Visit `http://localhost:8080` to see your configuration in action!

## 🎉 Congratulations!

You've successfully built a dynamic configuration system with TuskLang! Here's what you've accomplished:

- ✅ **Dynamic Configuration** - Environment-aware settings
- ✅ **Database Integration** - Real-time queries in configuration
- ✅ **Cross-File Communication** - Modular configuration structure
- ✅ **Security Features** - Encrypted and secure environment variables
- ✅ **Performance Optimization** - Caching and ML integration
- ✅ **ASP.NET Core Integration** - Seamless web application support

## 🔥 What's Next?

Now that you have the basics, explore:

1. **[Basic Syntax](003-basic-syntax-csharp.md)** - Master all syntax styles and features
2. **[Database Integration](004-database-integration-csharp.md)** - Advanced database patterns
3. **[Advanced Features](005-advanced-features-csharp.md)** - Unleash the full power
4. **[@ Operator System](006-operators-csharp.md)** - Master the @ operator ecosystem
5. **[Performance Optimization](007-performance-csharp.md)** - Optimize your configuration

## 💡 Pro Tips

- **Use multiple syntax styles** - Mix and match `[]`, `{}`, and `<>` as you prefer
- **Leverage database queries** - Put complex logic in your database, not your code
- **Cache expensive operations** - Use `@cache()` for performance-critical queries
- **Secure sensitive data** - Always use `@env.secure()` for passwords and keys
- **Test your configuration** - Write tests for your TSK files

---

**"We don't bow to any king" - Your configuration, your rules.**

Welcome to the future of .NET configuration! 🚀 