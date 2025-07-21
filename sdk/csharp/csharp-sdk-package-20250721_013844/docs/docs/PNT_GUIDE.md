# 🥜 Peanut Binary Configuration Guide for C#

A comprehensive guide to using TuskLang's high-performance binary configuration system with C#.

## Table of Contents

- [What is Peanut Configuration?](#what-is-peanut-configuration)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Core Concepts](#core-concepts)
- [API Reference](#api-reference)
- [Advanced Usage](#advanced-usage)
- [C#-Specific Features](#c-specific-features)
- [Integration Examples](#integration-examples)
- [Binary Format Details](#binary-format-details)
- [Performance Guide](#performance-guide)
- [Troubleshooting](#troubleshooting)
- [Migration Guide](#migration-guide)
- [Complete Examples](#complete-examples)
- [Quick Reference](#quick-reference)

## What is Peanut Configuration?

Peanut Configuration is TuskLang's high-performance binary configuration system that provides:

- **85% faster loading** compared to text-based formats
- **Hierarchical configuration** with CSS-like cascading
- **Type-safe access** with C# generics
- **File watching** for hot reloading
- **Binary format** (.pnt) for production use
- **Text format** (.peanuts) for development

The system automatically compiles human-readable `.peanuts` files into optimized `.pnt` binary files, providing the best of both worlds: developer-friendly text editing and production-ready performance.

## Installation

### Prerequisites

- .NET 6.0 or higher
- TuskLang C# SDK installed

### Installing the SDK

```bash
# Using dotnet CLI
dotnet add package TuskLang.CSharp

# Using NuGet Package Manager
Install-Package TuskLang.CSharp
```

### Importing PeanutConfig

```csharp
using TuskLang;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
```

## Quick Start

### Your First Peanut Configuration

1. Create a `peanu.peanuts` file:

```ini
[app]
name: "My C# App"
version: "1.0.0"

[server]
host: "localhost"
port: 8080
```

2. Load the configuration:

```csharp
var config = new PeanutConfig();
var settings = await config.LoadAsync();
```

3. Access values:

```csharp
var appName = await config.GetAsync<string>("app.name");
var port = await config.GetAsync<int>("server.port", 3000);
```

## Core Concepts

### File Types

- **`.peanuts`** - Human-readable configuration (INI-like syntax)
- **`.tsk`** - TuskLang syntax (advanced features)
- **`.pnt`** - Compiled binary format (85% faster)

### Hierarchical Loading

Configuration files are loaded in a hierarchical manner, similar to CSS cascading:

```
/project-root/
├── peanu.peanuts          # Base configuration
├── /src/
│   └── peanu.peanuts      # Overrides for src directory
└── /src/api/
    └── peanu.peanuts      # API-specific overrides
```

Child directory configurations override parent configurations, allowing for environment-specific settings.

### Type System

The system supports automatic type inference for common .NET types:

- **Primitive types**: `string`, `int`, `long`, `double`, `bool`
- **Complex types**: `Dictionary<string, object>`, arrays
- **Nullable types**: `string?`, `int?`, etc.

## API Reference

### PeanutConfig Class

#### Constructor

```csharp
// Default constructor
var config = new PeanutConfig();

// With logging
var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<PeanutConfig>();
var config = new PeanutConfig(logger);
```

#### Properties

| Property | Type | Description | Default |
|----------|------|-------------|---------|
| `AutoCompile` | `bool` | Auto-compile text files to binary | `true` |
| `Watch` | `bool` | Enable file watching | `true` |

#### Methods

##### LoadAsync(directory?)

```csharp
// Load from current directory
var config = await peanutConfig.LoadAsync();

// Load from specific directory
var config = await peanutConfig.LoadAsync("/path/to/project");
```

**Parameters:**
- `directory` (optional): Starting directory for configuration hierarchy

**Returns:** `Dictionary<string, object>` - Merged configuration

**Example:**
```csharp
var config = new PeanutConfig();
var settings = await config.LoadAsync();

Console.WriteLine($"App: {settings["app"]["name"]}");
Console.WriteLine($"Port: {settings["server"]["port"]}");
```

##### GetAsync<T>(keyPath, defaultValue?, directory?)

```csharp
// Get string value
var appName = await config.GetAsync<string>("app.name");

// Get int with default
var port = await config.GetAsync<int>("server.port", 3000);

// Get from specific directory
var dbHost = await config.GetAsync<string>("database.host", "localhost", "/config");
```

**Parameters:**
- `keyPath`: Dot-notation path to configuration value
- `defaultValue` (optional): Default value if key not found
- `directory` (optional): Directory to load from

**Returns:** `T?` - Typed configuration value

**Example:**
```csharp
var config = new PeanutConfig();

// Simple access
var name = await config.GetAsync<string>("app.name");

// With default value
var workers = await config.GetAsync<int>("server.workers", 4);

// Complex path
var dbConfig = await config.GetAsync<Dictionary<string, object>>("database");
```

##### CompileToBinaryAsync(config, outputPath)

```csharp
var config = new PeanutConfig();
var settings = await config.LoadAsync();
await config.CompileToBinaryAsync(settings, "config.pnt");
```

**Parameters:**
- `config`: Configuration dictionary to compile
- `outputPath`: Output file path

**Returns:** `Task` - Compilation task

##### FindConfigHierarchyAsync(startDir)

```csharp
var config = new PeanutConfig();
var hierarchy = await config.FindConfigHierarchyAsync("/project");
```

**Parameters:**
- `startDir`: Starting directory

**Returns:** `List<ConfigFile>` - List of configuration files in hierarchy

## Advanced Usage

### File Watching

```csharp
var config = new PeanutConfig { Watch = true };

// Configuration automatically reloads when files change
var settings = await config.LoadAsync();

// Watch specific file
config.WatchFile("config.peanuts", (path) => {
    Console.WriteLine($"Configuration changed: {path}");
    // Reload configuration
    var newSettings = await config.LoadAsync();
});
```

### Custom Serialization

```csharp
// Custom type handling
public class ServerConfig
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 8080;
}

// Register custom type converter
var config = new PeanutConfig();
var serverConfig = await config.GetAsync<ServerConfig>("server");
```

### Performance Optimization

```csharp
// Singleton pattern for application-wide config
public static class AppConfig
{
    private static readonly Lazy<PeanutConfig> _config = new(() => new PeanutConfig());
    public static PeanutConfig Instance => _config.Value;
}

// Usage
var settings = await AppConfig.Instance.LoadAsync();
```

### Thread Safety

```csharp
// Thread-safe configuration access
public class ThreadSafeConfig
{
    private readonly PeanutConfig _config;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<T?> GetAsync<T>(string keyPath, T? defaultValue = default)
    {
        await _semaphore.WaitAsync();
        try
        {
            return await _config.GetAsync(keyPath, defaultValue);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
```

## C#-Specific Features

### Dependency Injection Integration

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add PeanutConfig to DI container
builder.Services.AddPeanutConfig(config => {
    config.AutoCompile = true;
    config.Watch = true;
});

var app = builder.Build();

// In controller or service
public class ConfigController : ControllerBase
{
    private readonly PeanutConfig _config;

    public ConfigController(PeanutConfig config)
    {
        _config = config;
    }

    public async Task<IActionResult> GetConfig()
    {
        var settings = await _config.LoadAsync();
        return Ok(settings);
    }
}
```

### Async/Await Support

```csharp
// Full async support
public async Task<AppSettings> LoadAppSettingsAsync()
{
    var config = new PeanutConfig();
    
    var settings = new AppSettings
    {
        Name = await config.GetAsync<string>("app.name"),
        Version = await config.GetAsync<string>("app.version"),
        Server = new ServerSettings
        {
            Host = await config.GetAsync<string>("server.host"),
            Port = await config.GetAsync<int>("server.port")
        }
    };
    
    return settings;
}
```

### Strongly Typed Configuration

```csharp
// Define configuration classes
public class AppSettings
{
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public ServerSettings Server { get; set; } = new();
    public DatabaseSettings Database { get; set; } = new();
}

public class ServerSettings
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 8080;
    public bool Debug { get; set; } = false;
}

public class DatabaseSettings
{
    public string ConnectionString { get; set; } = "";
    public int PoolSize { get; set; } = 10;
}

// Load strongly typed configuration
public async Task<AppSettings> LoadTypedConfigAsync()
{
    var config = new PeanutConfig();
    var settings = await config.LoadAsync();
    
    return new AppSettings
    {
        Name = settings.GetValueOrDefault("app", new Dictionary<string, object>())
            .GetValueOrDefault("name", "").ToString() ?? "",
        Version = settings.GetValueOrDefault("app", new Dictionary<string, object>())
            .GetValueOrDefault("version", "").ToString() ?? "",
        Server = new ServerSettings
        {
            Host = settings.GetValueOrDefault("server", new Dictionary<string, object>())
                .GetValueOrDefault("host", "localhost").ToString() ?? "localhost",
            Port = Convert.ToInt32(settings.GetValueOrDefault("server", new Dictionary<string, object>())
                .GetValueOrDefault("port", 8080)),
            Debug = Convert.ToBoolean(settings.GetValueOrDefault("server", new Dictionary<string, object>())
                .GetValueOrDefault("debug", false))
        }
    };
}
```

## Integration Examples

### ASP.NET Core Integration

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add PeanutConfig
builder.Services.AddPeanutConfig(config => {
    config.AutoCompile = true;
    config.Watch = !builder.Environment.IsProduction();
});

// Configure strongly typed options
builder.Services.Configure<AppSettings>(async provider => {
    var config = provider.GetRequiredService<PeanutConfig>();
    var settings = await config.LoadAsync();
    
    return new AppSettings
    {
        Name = await config.GetAsync<string>("app.name"),
        Version = await config.GetAsync<string>("app.version"),
        Server = new ServerSettings
        {
            Host = await config.GetAsync<string>("server.host"),
            Port = await config.GetAsync<int>("server.port")
        }
    };
});

var app = builder.Build();

// Controller usage
[ApiController]
[Route("[controller]")]
public class ConfigController : ControllerBase
{
    private readonly PeanutConfig _config;
    private readonly AppSettings _settings;

    public ConfigController(PeanutConfig config, IOptions<AppSettings> settings)
    {
        _config = config;
        _settings = settings.Value;
    }

    [HttpGet]
    public async Task<IActionResult> GetConfig()
    {
        var config = await _config.LoadAsync();
        return Ok(new { 
            Config = config, 
            TypedSettings = _settings 
        });
    }
}
```

### Console Application Integration

```csharp
// Program.cs
class Program
{
    static async Task Main(string[] args)
    {
        var config = new PeanutConfig();
        
        // Load configuration
        var settings = await config.LoadAsync();
        
        // Access values
        var appName = await config.GetAsync<string>("app.name");
        var port = await config.GetAsync<int>("server.port", 3000);
        
        Console.WriteLine($"Starting {appName} on port {port}");
        
        // Start your application...
    }
}
```

## Binary Format Details

### File Structure

| Offset | Size | Description |
|--------|------|-------------|
| 0 | 4 | Magic: "PNUT" |
| 4 | 4 | Version (LE) |
| 8 | 8 | Timestamp (LE) |
| 16 | 8 | SHA256 checksum |
| 24 | N | MessagePack serialized data |

### Serialization Format

The C# implementation uses MessagePack for binary serialization:

```csharp
// Serialization
var data = MessagePackSerializer.Serialize(config);

// Deserialization
var config = MessagePackSerializer.Deserialize<Dictionary<string, object>>(data);
```

### File Header

```csharp
public static readonly byte[] Magic = Encoding.ASCII.GetBytes("PNUT");
public const int Version = 1;
public const int HeaderSize = 16;
public const int ChecksumSize = 8;
```

## Performance Guide

### Benchmarks

```csharp
// Performance benchmark
public static async Task BenchmarkAsync()
{
    var config = new PeanutConfig();
    var testContent = @"[server]
host: ""localhost""
port: 8080
workers: 4
debug: true

[database]
driver: ""postgresql""
host: ""db.example.com""
port: 5432
pool_size: 10

[cache]
enabled: true
ttl: 3600
backend: ""redis""";
    
    Console.WriteLine("🥜 Peanut Configuration Performance Test\n");
    
    // Test text parsing
    var sw = System.Diagnostics.Stopwatch.StartNew();
    for (int i = 0; i < 1000; i++)
    {
        config.ParseTextConfig(testContent);
    }
    sw.Stop();
    var textTime = sw.ElapsedMilliseconds;
    Console.WriteLine($"Text parsing (1000 iterations): {textTime}ms");
    
    // Test binary loading
    var parsed = config.ParseTextConfig(testContent);
    var binaryData = MessagePackSerializer.Serialize(parsed);
    
    sw.Restart();
    for (int i = 0; i < 1000; i++)
    {
        MessagePackSerializer.Deserialize<Dictionary<string, object>>(binaryData);
    }
    sw.Stop();
    var binaryTime = sw.ElapsedMilliseconds;
    Console.WriteLine($"Binary loading (1000 iterations): {binaryTime}ms");
    
    var improvement = ((double)(textTime - binaryTime) / textTime) * 100;
    Console.WriteLine($"\n✨ Binary format is {improvement:F0}% faster than text parsing!");
}
```

### Best Practices

1. **Always use .pnt in production** - Binary format provides 85% performance improvement
2. **Cache configuration objects** - Use singleton pattern for application-wide config
3. **Use file watching wisely** - Enable only in development environments
4. **Leverage async/await** - All operations are async for better performance
5. **Use strongly typed configuration** - Define classes for better type safety

## Troubleshooting

### Common Issues

#### File Not Found

```csharp
try
{
    var config = new PeanutConfig();
    var settings = await config.LoadAsync();
}
catch (FileNotFoundException ex)
{
    Console.WriteLine($"Configuration file not found: {ex.Message}");
    // Create default configuration
    var defaultConfig = new Dictionary<string, object>
    {
        ["app"] = new Dictionary<string, object>
        {
            ["name"] = "Default App",
            ["version"] = "1.0.0"
        }
    };
}
```

#### Checksum Mismatch

```csharp
try
{
    var config = new PeanutConfig();
    var settings = await config.LoadAsync();
}
catch (InvalidOperationException ex) when (ex.Message.Contains("checksum"))
{
    Console.WriteLine("Binary file corrupted, recompiling from source...");
    // Force recompilation
    var hierarchy = await config.FindConfigHierarchyAsync();
    await config.AutoCompileConfigsAsync(hierarchy);
}
```

#### Performance Issues

```csharp
// Enable caching
var config = new PeanutConfig();
config.AutoCompile = true; // Ensures binary files are used

// Use singleton pattern
public static class AppConfig
{
    private static readonly Lazy<PeanutConfig> _config = new(() => new PeanutConfig());
    public static PeanutConfig Instance => _config.Value;
}
```

### Debug Mode

```csharp
// Enable debug logging
var loggerFactory = LoggerFactory.Create(builder => 
    builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
var logger = loggerFactory.CreateLogger<PeanutConfig>();

var config = new PeanutConfig(logger);
```

## Migration Guide

### From JSON

```csharp
// Old JSON approach
var json = await File.ReadAllTextAsync("appsettings.json");
var settings = JsonSerializer.Deserialize<AppSettings>(json);

// New Peanut approach
var config = new PeanutConfig();
var settings = await config.LoadAsync();
var appName = await config.GetAsync<string>("app.name");
```

### From YAML

```csharp
// Old YAML approach
var yaml = await File.ReadAllTextAsync("config.yml");
var deserializer = new YamlDotNet.Serialization.Deserializer();
var settings = deserializer.Deserialize<Dictionary<string, object>>(yaml);

// New Peanut approach
var config = new PeanutConfig();
var settings = await config.LoadAsync();
```

### From .env

```csharp
// Old .env approach
var envVars = Environment.GetEnvironmentVariables();
var dbHost = envVars["DB_HOST"]?.ToString();

// New Peanut approach
var config = new PeanutConfig();
var dbHost = await config.GetAsync<string>("database.host");
```

## Complete Examples

### Web Application Configuration

**File Structure:**
```
/MyWebApp/
├── peanu.peanuts
├── /src/
│   ├── peanu.peanuts
│   └── /api/
│       └── peanu.peanuts
└── Program.cs
```

**peanu.peanuts (root):**
```ini
[app]
name: "My Web App"
version: "1.0.0"

[server]
host: "0.0.0.0"
port: 5000
workers: 4

[database]
driver: "postgresql"
host: "localhost"
port: 5432
```

**src/peanu.peanuts:**
```ini
[server]
port: 8080
debug: true

[database]
pool_size: 20
```

**src/api/peanu.peanuts:**
```ini
[api]
rate_limit: 1000
timeout: 30

[database]
pool_size: 50
```

**Program.cs:**
```csharp
using TuskLang;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add PeanutConfig
builder.Services.AddPeanutConfig(config => {
    config.AutoCompile = true;
    config.Watch = !builder.Environment.IsProduction();
});

// Configure strongly typed options
builder.Services.Configure<AppSettings>(async provider => {
    var config = provider.GetRequiredService<PeanutConfig>();
    return new AppSettings
    {
        Name = await config.GetAsync<string>("app.name"),
        Version = await config.GetAsync<string>("app.version"),
        Server = new ServerSettings
        {
            Host = await config.GetAsync<string>("server.host"),
            Port = await config.GetAsync<int>("server.port"),
            Workers = await config.GetAsync<int>("server.workers"),
            Debug = await config.GetAsync<bool>("server.debug", false)
        },
        Database = new DatabaseSettings
        {
            Driver = await config.GetAsync<string>("database.driver"),
            Host = await config.GetAsync<string>("database.host"),
            Port = await config.GetAsync<int>("database.port"),
            PoolSize = await config.GetAsync<int>("database.pool_size", 10)
        },
        Api = new ApiSettings
        {
            RateLimit = await config.GetAsync<int>("api.rate_limit", 100),
            Timeout = await config.GetAsync<int>("api.timeout", 30)
        }
    };
});

var app = builder.Build();

// Use configuration
var settings = app.Services.GetRequiredService<IOptions<AppSettings>>().Value;
Console.WriteLine($"Starting {settings.Name} v{settings.Version} on {settings.Server.Host}:{settings.Server.Port}");

app.Run();
```

### Microservice Configuration

**File Structure:**
```
/UserService/
├── peanu.peanuts
├── /config/
│   ├── development.peanuts
│   ├── staging.peanuts
│   └── production.peanuts
└── Program.cs
```

**peanu.peanuts:**
```ini
[service]
name: "User Service"
version: "2.1.0"

[server]
host: "0.0.0.0"
port: 3001

[database]
connection_string: "postgresql://user:pass@localhost/users"

[redis]
host: "localhost"
port: 6379
db: 0

[logging]
level: "info"
format: "json"
```

**config/development.peanuts:**
```ini
[server]
port: 3001

[database]
connection_string: "postgresql://dev:dev@localhost/users_dev"

[logging]
level: "debug"
```

**Program.cs:**
```csharp
using TuskLang;

var builder = WebApplication.CreateBuilder(args);

// Determine environment
var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
var configDir = Path.Combine(Directory.GetCurrentDirectory(), "config", environment.ToLower());

// Add PeanutConfig with environment-specific directory
builder.Services.AddPeanutConfig(config => {
    config.AutoCompile = true;
    config.Watch = environment == "Development";
});

var app = builder.Build();

// Load configuration
var config = app.Services.GetRequiredService<PeanutConfig>();
var settings = await config.LoadAsync(configDir);

// Configure services
var serviceName = await config.GetAsync<string>("service.name");
var serviceVersion = await config.GetAsync<string>("service.version");
var serverPort = await config.GetAsync<int>("server.port");

Console.WriteLine($"Starting {serviceName} v{serviceVersion} on port {serverPort}");

app.Run();
```

### CLI Tool Configuration

**File Structure:**
```
/MyCLITool/
├── peanu.peanuts
├── /commands/
│   ├── build.peanuts
│   └── deploy.peanuts
└── Program.cs
```

**peanu.peanuts:**
```ini
[tool]
name: "My CLI Tool"
version: "1.0.0"

[output]
format: "text"
color: true
verbose: false

[api]
base_url: "https://api.example.com"
timeout: 30
```

**commands/build.peanuts:**
```ini
[build]
target: "release"
optimize: true
parallel: true

[output]
format: "json"
```

**Program.cs:**
```csharp
using TuskLang;
using System.CommandLine;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var config = new PeanutConfig();
        var settings = await config.LoadAsync();

        // Create CLI commands
        var rootCommand = new RootCommand(await config.GetAsync<string>("tool.name", "CLI Tool"));

        // Build command
        var buildCommand = new Command("build", "Build the project");
        buildCommand.SetHandler(async () => {
            var target = await config.GetAsync<string>("build.target", "debug");
            var optimize = await config.GetAsync<bool>("build.optimize", false);
            var parallel = await config.GetAsync<bool>("build.parallel", false);
            
            Console.WriteLine($"Building with target: {target}, optimize: {optimize}, parallel: {parallel}");
        });

        // Deploy command
        var deployCommand = new Command("deploy", "Deploy the project");
        deployCommand.SetHandler(async () => {
            var apiUrl = await config.GetAsync<string>("api.base_url");
            var timeout = await config.GetAsync<int>("api.timeout", 30);
            
            Console.WriteLine($"Deploying to {apiUrl} with timeout {timeout}s");
        });

        rootCommand.AddCommand(buildCommand);
        rootCommand.AddCommand(deployCommand);

        return await rootCommand.InvokeAsync(args);
    }
}
```

## Quick Reference

### Common Operations

```csharp
// Load config
var config = new PeanutConfig();
var settings = await config.LoadAsync();

// Get value
var value = await config.GetAsync<string>("key.path", "default");

// Compile to binary
await config.CompileToBinaryAsync(settings, "config.pnt");

// Watch for changes
config.Watch = true;
var settings = await config.LoadAsync(); // Auto-reloads on changes
```

### Configuration File Format

```ini
[section]
key: "value"
number: 42
boolean: true
array: ["item1", "item2"]

[subsection]
nested_key: "nested_value"
```

### Performance Tips

1. Use `.pnt` files in production
2. Enable `AutoCompile` for automatic binary generation
3. Use singleton pattern for application-wide config
4. Leverage async/await for all operations
5. Enable file watching only in development

### Error Handling

```csharp
try
{
    var config = new PeanutConfig();
    var settings = await config.LoadAsync();
}
catch (FileNotFoundException)
{
    // Handle missing configuration files
}
catch (InvalidOperationException ex) when (ex.Message.Contains("checksum"))
{
    // Handle corrupted binary files
}
catch (Exception ex)
{
    // Handle other errors
}
```

---

This guide provides everything you need to use Peanut Configuration effectively in your C# applications. The system is designed to be both developer-friendly and production-ready, offering the best performance and flexibility for configuration management. 