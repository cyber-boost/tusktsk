# TuskTsk C# SDK - Comprehensive API Documentation

## 🚀 Complete Developer Guide & API Reference

**Version**: 2.0.1  
**Target Frameworks**: .NET 6.0, .NET 8.0  
**Author**: Cyberboost LLC  

---

## Table of Contents

1. [Quick Start](#quick-start)
2. [Installation](#installation)
3. [Core API Reference](#core-api-reference)
4. [Configuration Management](#configuration-management)
5. [Advanced Features](#advanced-features)
6. [Performance Optimization](#performance-optimization)
7. [Error Handling](#error-handling)
8. [Best Practices](#best-practices)
9. [Real-World Examples](#real-world-examples)
10. [Troubleshooting](#troubleshooting)

---

## Quick Start

### Basic Usage Example

```csharp
using TuskLang;

// Load configuration from string
var configContent = @"
[database]
host = ""localhost""
port = 5432
name = ""myapp""
user = ""admin""
password = ""secret""

[cache]
provider = ""redis""
ttl = 3600
max_connections = 100
";

var tsk = TSK.FromString(configContent);

// Access configuration values
var dbHost = tsk.GetValue("database", "host");        // "localhost"
var dbPort = tsk.GetValue("database", "port");        // 5432
var cacheProvider = tsk.GetValue("cache", "provider"); // "redis"

Console.WriteLine($"Connecting to database at {dbHost}:{dbPort}");
```

### File-Based Configuration

```csharp
// Load from file
var tsk = TSK.FromFile("appsettings.tsk");

// Modify configuration
tsk.SetValue("database", "host", "production.db.com");
tsk.SetValue("logging", "level", "Info");

// Save changes
File.WriteAllText("appsettings.tsk", tsk.ToString());
```

---

## Installation

### Package Manager

```bash
Install-Package TuskTsk
```

### .NET CLI

```bash
dotnet add package TuskTsk
```

### Package Reference

```xml
<PackageReference Include="TuskTsk" Version="2.0.1" />
```

---

## Core API Reference

### TSK Class

The main class for parsing and managing TuskLang configuration files.

#### Constructors

```csharp
// Create empty TSK instance
public TSK()

// Create TSK with initial data
public TSK(Dictionary<string, object> data)
```

#### Static Factory Methods

```csharp
// Create from string content
public static TSK FromString(string content)

// Create from file
public static TSK FromFile(string filepath)
```

**Example:**
```csharp
// From string
var tsk1 = TSK.FromString("[app]\nname = \"MyApp\"");

// From file
var tsk2 = TSK.FromFile("/path/to/config.tsk");

// Empty instance
var tsk3 = new TSK();
```

#### Core Methods

##### GetSection(string name)

Retrieves an entire configuration section.

```csharp
public Dictionary<string, object> GetSection(string name)
```

**Parameters:**
- `name`: Section name (case-sensitive)

**Returns:**
- `Dictionary<string, object>`: Section data, or `null` if not found

**Example:**
```csharp
var dbSection = tsk.GetSection("database");
if (dbSection != null)
{
    foreach (var kvp in dbSection)
    {
        Console.WriteLine($"{kvp.Key}: {kvp.Value}");
    }
}
```

##### GetValue(string section, string key)

Retrieves a specific value from a section.

```csharp
public object GetValue(string section, string key)
```

**Parameters:**
- `section`: Section name
- `key`: Key name within the section

**Returns:**
- `object`: Value (string, int, bool, List<object>), or `null` if not found

**Type Casting Examples:**
```csharp
// String values
var host = (string)tsk.GetValue("database", "host");

// Numeric values
var port = (int)tsk.GetValue("database", "port");

// Boolean values
var ssl = (bool)tsk.GetValue("database", "ssl");

// Array values
var features = (List<object>)tsk.GetValue("app", "features");

// Safe casting with null checks
var timeout = tsk.GetValue("api", "timeout") as int? ?? 30000;
```

##### SetSection(string name, Dictionary<string, object> values)

Sets or replaces an entire section.

```csharp
public void SetSection(string name, Dictionary<string, object> values)
```

**Example:**
```csharp
var newDbConfig = new Dictionary<string, object>
{
    {"host", "db.example.com"},
    {"port", 3306},
    {"ssl", true},
    {"pool_size", 20}
};

tsk.SetSection("database", newDbConfig);
```

##### SetValue(string section, string key, object value)

Sets a specific value in a section.

```csharp
public void SetValue(string section, string key, object value)
```

**Example:**
```csharp
// Create section if it doesn't exist
tsk.SetValue("logging", "level", "Debug");
tsk.SetValue("logging", "file", "/var/log/app.log");
tsk.SetValue("logging", "max_size", 50000000); // 50MB
```

##### ToString()

Converts the TSK instance back to TuskLang format.

```csharp
public override string ToString()
```

**Example:**
```csharp
var configString = tsk.ToString();
File.WriteAllText("output.tsk", configString);
```

---

## Configuration Management

### Configuration File Format

TuskLang uses a TOML-like syntax with enhanced features:

```tsk
# Comments start with #
[section_name]
string_value = "text"
number_value = 42
boolean_value = true
array_value = ["item1", "item2", "item3"]

# Nested sections (if supported)
[parent.child]
nested_value = "data"

# Special characters and escaping
special_text = "Line 1\nLine 2\tTabbed"
quoted_string = "He said \"Hello!\""
unicode_text = "Hello 世界 🌍"

# Fujsen (function serialization) - Advanced feature
[functions]
transformer = 'x => x.ToString().ToUpper()'
validator = 'input => !string.IsNullOrEmpty(input)'
```

### Data Types

| TuskLang Type | C# Type | Example |
|--------------|---------|---------|
| String | `string` | `"Hello World"` |
| Integer | `int` | `42` |
| Boolean | `bool` | `true` / `false` |
| Array | `List<object>` | `["a", "b", "c"]` |

### Real-World Configuration Examples

#### Database Configuration

```tsk
[database]
# Primary database connection
primary_host = "db-primary.company.com"
primary_port = 5432
database_name = "production_db"
username = "app_user"
password = "secure_password_123"
ssl_mode = "require"
connection_timeout = 30
pool_min_size = 5
pool_max_size = 100

# Read replica configuration
replica_host = "db-replica.company.com"
replica_port = 5432
enable_read_replica = true

# Backup settings
backup_enabled = true
backup_schedule = "0 2 * * *"  # Daily at 2 AM
backup_retention_days = 30
```

#### Microservices Configuration

```tsk
[services]
# Service discovery
discovery_url = "http://consul.company.com:8500"
service_name = "user-management-api"
service_port = 8080
health_check_interval = 30

# External service endpoints
auth_service = "https://auth.company.com/api/v1"
notification_service = "https://notify.company.com/api/v2"
analytics_service = "https://analytics.company.com/api/v1"

# Circuit breaker settings
circuit_breaker_timeout = 5000
circuit_breaker_threshold = 10
circuit_breaker_recovery_time = 60000

[monitoring]
# Metrics and logging
metrics_enabled = true
metrics_port = 9090
log_level = "Info"
log_file = "/var/log/app/service.log"
log_max_size = 100000000  # 100MB
log_retention = 7  # days

# Tracing
tracing_enabled = true
jaeger_endpoint = "http://jaeger:14268/api/traces"
sampling_rate = 0.1
```

### Configuration Loading Patterns

#### Environment-Specific Configuration

```csharp
public class ConfigurationManager
{
    public TSK LoadConfiguration(string environment = "development")
    {
        var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var configFiles = new[]
        {
            Path.Combine(basePath, "config", "base.tsk"),
            Path.Combine(basePath, "config", $"{environment}.tsk"),
            Path.Combine(basePath, "config", "local.tsk") // Optional override
        };

        var merged = new TSK();
        
        foreach (var file in configFiles)
        {
            if (!File.Exists(file)) continue;
            
            var config = TSK.FromFile(file);
            MergeConfigurations(merged, config);
        }
        
        return merged;
    }
    
    private void MergeConfigurations(TSK target, TSK source)
    {
        // Implementation for merging configurations
        // This is a simplified example - real implementation would be more robust
        var sourceString = source.ToString();
        var mergedConfig = TSK.FromString(sourceString);
        // Merge logic here...
    }
}
```

#### Configuration with Validation

```csharp
public class DatabaseConfig
{
    public string Host { get; set; }
    public int Port { get; set; }
    public string Database { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public bool SSL { get; set; }
    public int PoolSize { get; set; }
    
    public static DatabaseConfig FromTSK(TSK tsk, string section = "database")
    {
        var config = new DatabaseConfig
        {
            Host = (string)tsk.GetValue(section, "host") ?? throw new ArgumentNullException("host"),
            Port = (int)(tsk.GetValue(section, "port") ?? 5432),
            Database = (string)tsk.GetValue(section, "database") ?? throw new ArgumentNullException("database"),
            Username = (string)tsk.GetValue(section, "username") ?? throw new ArgumentNullException("username"),
            Password = (string)tsk.GetValue(section, "password") ?? throw new ArgumentNullException("password"),
            SSL = (bool)(tsk.GetValue(section, "ssl") ?? false),
            PoolSize = (int)(tsk.GetValue(section, "pool_size") ?? 10)
        };
        
        Validate(config);
        return config;
    }
    
    private static void Validate(DatabaseConfig config)
    {
        if (config.Port < 1 || config.Port > 65535)
            throw new ArgumentOutOfRangeException(nameof(Port), "Port must be between 1 and 65535");
            
        if (config.PoolSize < 1 || config.PoolSize > 1000)
            throw new ArgumentOutOfRangeException(nameof(PoolSize), "Pool size must be between 1 and 1000");
            
        if (string.IsNullOrWhiteSpace(config.Host))
            throw new ArgumentException("Host cannot be empty", nameof(Host));
    }
    
    public string GetConnectionString()
    {
        var builder = new StringBuilder();
        builder.Append($"Host={Host};Port={Port};Database={Database};");
        builder.Append($"Username={Username};Password={Password};");
        builder.Append($"SSL Mode={SSL}");
        return builder.ToString();
    }
}
```

---

## Advanced Features

### Concurrent Configuration Access

```csharp
public class ThreadSafeConfigurationManager
{
    private readonly ReaderWriterLockSlim _lock = new();
    private TSK _config;
    
    public ThreadSafeConfigurationManager(TSK config)
    {
        _config = config;
    }
    
    public T GetValue<T>(string section, string key, T defaultValue = default)
    {
        _lock.EnterReadLock();
        try
        {
            var value = _config.GetValue(section, key);
            return value is T typed ? typed : defaultValue;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }
    
    public void SetValue(string section, string key, object value)
    {
        _lock.EnterWriteLock();
        try
        {
            _config.SetValue(section, key, value);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
    
    public void UpdateConfiguration(Action<TSK> updateAction)
    {
        _lock.EnterWriteLock();
        try
        {
            updateAction(_config);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _lock?.Dispose();
        }
    }
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
```

### Configuration Change Monitoring

```csharp
public class ConfigurationWatcher : IDisposable
{
    private readonly FileSystemWatcher _watcher;
    private readonly string _filePath;
    private TSK _currentConfig;
    
    public event EventHandler<ConfigurationChangedEventArgs> ConfigurationChanged;
    
    public ConfigurationWatcher(string filePath)
    {
        _filePath = filePath;
        _currentConfig = TSK.FromFile(filePath);
        
        var directory = Path.GetDirectoryName(filePath);
        var fileName = Path.GetFileName(filePath);
        
        _watcher = new FileSystemWatcher(directory, fileName)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true
        };
        
        _watcher.Changed += OnFileChanged;
    }
    
    private async void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        // Debounce file changes
        await Task.Delay(100);
        
        try
        {
            var newConfig = TSK.FromFile(_filePath);
            var oldConfig = _currentConfig;
            _currentConfig = newConfig;
            
            ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs
            {
                OldConfiguration = oldConfig,
                NewConfiguration = newConfig,
                FilePath = _filePath
            });
        }
        catch (Exception ex)
        {
            // Log error but don't crash
            Console.WriteLine($"Error reloading configuration: {ex.Message}");
        }
    }
    
    public TSK CurrentConfiguration => _currentConfig;
    
    public void Dispose()
    {
        _watcher?.Dispose();
    }
}

public class ConfigurationChangedEventArgs : EventArgs
{
    public TSK OldConfiguration { get; set; }
    public TSK NewConfiguration { get; set; }
    public string FilePath { get; set; }
}
```

### Configuration Serialization and Caching

```csharp
public class ConfigurationCache
{
    private readonly MemoryCache _cache;
    private readonly TimeSpan _defaultExpiration;
    
    public ConfigurationCache(TimeSpan? defaultExpiration = null)
    {
        _cache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = 1000 // Limit number of cached items
        });
        _defaultExpiration = defaultExpiration ?? TimeSpan.FromMinutes(15);
    }
    
    public TSK GetOrLoad(string cacheKey, Func<TSK> loader, TimeSpan? expiration = null)
    {
        if (_cache.TryGetValue(cacheKey, out TSK cached))
        {
            return cached;
        }
        
        var config = loader();
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? _defaultExpiration,
            Size = 1,
            Priority = CacheItemPriority.Normal
        };
        
        _cache.Set(cacheKey, config, options);
        return config;
    }
    
    public async Task<TSK> GetOrLoadAsync(string cacheKey, Func<Task<TSK>> loader, TimeSpan? expiration = null)
    {
        if (_cache.TryGetValue(cacheKey, out TSK cached))
        {
            return cached;
        }
        
        var config = await loader();
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? _defaultExpiration,
            Size = 1
        };
        
        _cache.Set(cacheKey, config, options);
        return config;
    }
    
    public void Invalidate(string cacheKey)
    {
        _cache.Remove(cacheKey);
    }
    
    public void InvalidateAll()
    {
        if (_cache is MemoryCache mc)
        {
            mc.Compact(1.0); // Remove all entries
        }
    }
}
```

---

## Performance Optimization

### Best Practices for High Performance

#### 1. Configuration Caching

```csharp
// Good: Cache frequently accessed configurations
private static readonly ConcurrentDictionary<string, TSK> ConfigCache = new();

public TSK GetConfiguration(string key)
{
    return ConfigCache.GetOrAdd(key, k => TSK.FromFile($"{k}.tsk"));
}

// Bad: Re-parsing configuration every time
public TSK GetConfiguration(string key)
{
    return TSK.FromFile($"{key}.tsk"); // Expensive file I/O and parsing
}
```

#### 2. Bulk Configuration Access

```csharp
// Good: Get section once and access multiple values
var dbSection = tsk.GetSection("database");
if (dbSection != null)
{
    var host = (string)dbSection["host"];
    var port = (int)dbSection["port"];
    var database = (string)dbSection["database"];
}

// Bad: Multiple individual calls
var host = (string)tsk.GetValue("database", "host");
var port = (int)tsk.GetValue("database", "port");
var database = (string)tsk.GetValue("database", "database");
```

#### 3. Pre-compiled Configuration Classes

```csharp
public class CompiledAppConfig
{
    public string DatabaseHost { get; }
    public int DatabasePort { get; }
    public string ApiEndpoint { get; }
    public bool EnableCaching { get; }
    
    // Pre-compile all configuration values
    public CompiledAppConfig(TSK tsk)
    {
        DatabaseHost = (string)tsk.GetValue("database", "host");
        DatabasePort = (int)tsk.GetValue("database", "port");
        ApiEndpoint = (string)tsk.GetValue("api", "endpoint");
        EnableCaching = (bool)(tsk.GetValue("cache", "enabled") ?? false);
    }
}
```

### Memory Optimization

```csharp
public class MemoryEfficientConfigManager : IDisposable
{
    private readonly Dictionary<string, WeakReference> _configReferences = new();
    private readonly Timer _cleanupTimer;
    
    public MemoryEfficientConfigManager()
    {
        // Clean up unreferenced configurations every 5 minutes
        _cleanupTimer = new Timer(CleanupUnusedConfigurations, null, 
            TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }
    
    public TSK GetConfiguration(string key)
    {
        if (_configReferences.TryGetValue(key, out var weakRef) && 
            weakRef.Target is TSK config)
        {
            return config;
        }
        
        // Load and cache with weak reference
        var newConfig = TSK.FromFile($"{key}.tsk");
        _configReferences[key] = new WeakReference(newConfig);
        return newConfig;
    }
    
    private void CleanupUnusedConfigurations(object state)
    {
        var keysToRemove = new List<string>();
        
        foreach (var kvp in _configReferences)
        {
            if (!kvp.Value.IsAlive)
            {
                keysToRemove.Add(kvp.Key);
            }
        }
        
        foreach (var key in keysToRemove)
        {
            _configReferences.Remove(key);
        }
        
        GC.Collect(); // Force garbage collection
    }
    
    public void Dispose()
    {
        _cleanupTimer?.Dispose();
        _configReferences.Clear();
    }
}
```

---

## Error Handling

### Common Exceptions and Solutions

#### Configuration File Not Found

```csharp
try
{
    var config = TSK.FromFile("config.tsk");
}
catch (FileNotFoundException ex)
{
    Console.WriteLine($"Configuration file not found: {ex.FileName}");
    
    // Create default configuration
    var defaultConfig = new TSK();
    defaultConfig.SetValue("app", "name", "MyApp");
    defaultConfig.SetValue("app", "version", "1.0.0");
    
    // Save default configuration
    File.WriteAllText("config.tsk", defaultConfig.ToString());
}
```

#### Invalid Configuration Format

```csharp
try
{
    var config = TSK.FromString(configContent);
}
catch (FormatException ex)
{
    Console.WriteLine($"Invalid configuration format: {ex.Message}");
    // Handle parsing errors gracefully
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Configuration error: {ex.Message}");
    // Handle validation errors
}
```

#### Missing Configuration Values

```csharp
public static T GetRequiredValue<T>(this TSK tsk, string section, string key)
{
    var value = tsk.GetValue(section, key);
    if (value == null)
    {
        throw new ConfigurationException($"Required configuration value not found: [{section}].{key}");
    }
    
    if (!(value is T))
    {
        throw new ConfigurationException($"Configuration value [{section}].{key} is not of type {typeof(T).Name}");
    }
    
    return (T)value;
}

public class ConfigurationException : Exception
{
    public ConfigurationException(string message) : base(message) { }
    public ConfigurationException(string message, Exception innerException) : base(message, innerException) { }
}
```

### Comprehensive Error Handling Pattern

```csharp
public class RobustConfigurationLoader
{
    private readonly ILogger _logger;
    private readonly string[] _searchPaths;
    
    public RobustConfigurationLoader(ILogger logger, params string[] searchPaths)
    {
        _logger = logger;
        _searchPaths = searchPaths ?? new[] { "." };
    }
    
    public async Task<TSK> LoadConfigurationAsync(string configName, CancellationToken cancellationToken = default)
    {
        var errors = new List<Exception>();
        
        foreach (var searchPath in _searchPaths)
        {
            var filePath = Path.Combine(searchPath, $"{configName}.tsk");
            
            try
            {
                _logger.LogDebug("Attempting to load configuration from: {FilePath}", filePath);
                
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("Configuration file not found: {FilePath}", filePath);
                    continue;
                }
                
                var content = await File.ReadAllTextAsync(filePath, cancellationToken);
                var config = TSK.FromString(content);
                
                _logger.LogInformation("Successfully loaded configuration from: {FilePath}", filePath);
                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load configuration from: {FilePath}", filePath);
                errors.Add(ex);
            }
        }
        
        throw new AggregateException(
            $"Failed to load configuration '{configName}' from any search path", errors);
    }
    
    public TSK LoadConfigurationWithFallback(string configName, TSK fallbackConfig = null)
    {
        try
        {
            return LoadConfigurationAsync(configName).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Using fallback configuration for {ConfigName}", configName);
            return fallbackConfig ?? new TSK();
        }
    }
}
```

---

## Best Practices

### 1. Configuration Organization

```tsk
# Good: Well-organized configuration
[database]
# Primary database
primary_host = "db1.company.com"
primary_port = 5432

# Read replicas
replica_hosts = ["db2.company.com", "db3.company.com"]
replica_port = 5432

[cache]
# Redis configuration
redis_host = "cache.company.com"
redis_port = 6379
redis_db = 0

[logging]
# Application logging
level = "Info"
file = "/var/log/app.log"
max_size = 50000000
```

### 2. Environment-Specific Configuration

```csharp
public class EnvironmentConfigurationProvider
{
    public TSK GetConfiguration()
    {
        var environment = Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "development";
        var configs = new List<string>
        {
            "appsettings.tsk",           // Base configuration
            $"appsettings.{environment}.tsk", // Environment-specific
            "appsettings.local.tsk"      // Local overrides (gitignored)
        };
        
        var merged = new TSK();
        
        foreach (var configFile in configs)
        {
            if (File.Exists(configFile))
            {
                var config = TSK.FromFile(configFile);
                MergeConfiguration(merged, config);
            }
        }
        
        return merged;
    }
    
    private void MergeConfiguration(TSK target, TSK source)
    {
        // Implementation depends on merge strategy
        // This is simplified - real implementation would handle nested merging
    }
}
```

### 3. Configuration Validation

```csharp
public static class ConfigurationValidator
{
    public static void ValidateRequired(TSK config, params (string section, string key)[] required)
    {
        var missing = new List<string>();
        
        foreach (var (section, key) in required)
        {
            if (config.GetValue(section, key) == null)
            {
                missing.Add($"[{section}].{key}");
            }
        }
        
        if (missing.Any())
        {
            throw new ConfigurationException($"Missing required configuration values: {string.Join(", ", missing)}");
        }
    }
    
    public static void ValidateNumericRange(TSK config, string section, string key, int min, int max)
    {
        var value = config.GetValue(section, key);
        if (value is int intValue)
        {
            if (intValue < min || intValue > max)
            {
                throw new ConfigurationException($"Configuration value [{section}].{key} = {intValue} is outside valid range [{min}, {max}]");
            }
        }
        else
        {
            throw new ConfigurationException($"Configuration value [{section}].{key} is not a valid integer");
        }
    }
}
```

### 4. Performance Monitoring

```csharp
public class ConfigurationPerformanceMonitor
{
    private static readonly Dictionary<string, PerformanceCounter> _counters = new();
    
    public static TSK LoadWithMonitoring(string filePath)
    {
        var stopwatch = Stopwatch.StartNew();
        var initialMemory = GC.GetTotalMemory(false);
        
        try
        {
            var config = TSK.FromFile(filePath);
            
            stopwatch.Stop();
            var finalMemory = GC.GetTotalMemory(false);
            
            LogPerformance(filePath, stopwatch.ElapsedMilliseconds, finalMemory - initialMemory);
            
            return config;
        }
        catch (Exception ex)
        {
            LogError(filePath, ex);
            throw;
        }
    }
    
    private static void LogPerformance(string filePath, long elapsedMs, long memoryUsed)
    {
        Console.WriteLine($"Configuration Load Performance:");
        Console.WriteLine($"  File: {filePath}");
        Console.WriteLine($"  Time: {elapsedMs}ms");
        Console.WriteLine($"  Memory: {memoryUsed / 1024:F1}KB");
    }
    
    private static void LogError(string filePath, Exception ex)
    {
        Console.WriteLine($"Configuration Load Error:");
        Console.WriteLine($"  File: {filePath}");
        Console.WriteLine($"  Error: {ex.Message}");
    }
}
```

---

## Real-World Examples

### Complete Web Application Configuration

```csharp
// Program.cs - ASP.NET Core Integration
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        // Load TuskLang configuration
        var tskConfig = LoadTuskLangConfiguration();
        
        // Configure services using TuskLang config
        ConfigureServicesFromTSK(builder.Services, tskConfig);
        
        var app = builder.Build();
        
        ConfigurePipelineFromTSK(app, tskConfig);
        
        app.Run();
    }
    
    private static TSK LoadTuskLangConfiguration()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        
        var configFiles = new[]
        {
            "appsettings.tsk",
            $"appsettings.{environment}.tsk",
            "appsettings.local.tsk"
        };
        
        var merged = new TSK();
        
        foreach (var file in configFiles)
        {
            if (File.Exists(file))
            {
                var config = TSK.FromFile(file);
                // Merge configurations (implementation specific)
            }
        }
        
        return merged;
    }
    
    private static void ConfigureServicesFromTSK(IServiceCollection services, TSK config)
    {
        // Database configuration
        var dbConfig = DatabaseConfig.FromTSK(config);
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(dbConfig.GetConnectionString()));
        
        // Redis cache configuration
        var cacheHost = (string)config.GetValue("cache", "host");
        var cachePort = (int)config.GetValue("cache", "port");
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = $"{cacheHost}:{cachePort}";
        });
        
        // API client configuration
        var apiConfig = ApiConfig.FromTSK(config);
        services.AddHttpClient<ExternalApiClient>(client =>
        {
            client.BaseAddress = new Uri(apiConfig.BaseUrl);
            client.Timeout = TimeSpan.FromMilliseconds(apiConfig.Timeout);
        });
        
        // Register configuration as singleton
        services.AddSingleton(config);
    }
    
    private static void ConfigurePipelineFromTSK(WebApplication app, TSK config)
    {
        // Enable features based on configuration
        var enableSwagger = (bool)(config.GetValue("features", "enable_swagger") ?? false);
        if (enableSwagger)
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        
        var enableCors = (bool)(config.GetValue("features", "enable_cors") ?? false);
        if (enableCors)
        {
            app.UseCors();
        }
    }
}
```

### Microservice Configuration Example

```tsk
# microservice.tsk - Complete microservice configuration
[service]
name = "user-management-api"
version = "2.1.0"
port = 8080
host = "0.0.0.0"
environment = "production"

[database]
provider = "postgresql"
host = "db.company.com"
port = 5432
database = "users_production"
username = "api_user"
password = "secure_password_here"
ssl_mode = "require"
pool_min = 5
pool_max = 100
connection_timeout = 30
command_timeout = 60

[cache]
provider = "redis"
host = "cache.company.com"
port = 6379
database = 0
password = "redis_password"
connection_timeout = 5000
operation_timeout = 1000
retry_count = 3

[messaging]
provider = "rabbitmq"
host = "mq.company.com"
port = 5672
username = "api_user"
password = "mq_password"
exchange = "user_events"
queue = "user_management_queue"
durable = true
auto_ack = false

[security]
jwt_secret = "your-very-secure-jwt-secret-key-here"
jwt_expiry_hours = 24
enable_https_only = true
cors_origins = ["https://app.company.com", "https://admin.company.com"]
rate_limit_requests_per_minute = 1000

[monitoring]
enable_metrics = true
metrics_port = 9090
health_check_path = "/health"
enable_distributed_tracing = true
jaeger_endpoint = "http://jaeger.company.com:14268/api/traces"
log_level = "Info"
log_file = "/var/log/user-api.log"
log_max_size_mb = 100
log_retention_days = 30

[features]
enable_user_registration = true
enable_password_reset = true
enable_email_verification = true
enable_two_factor_auth = true
max_login_attempts = 5
lockout_duration_minutes = 30

[external_services]
email_service_url = "https://email.company.com/api/v1"
email_service_key = "email_api_key_here"
notification_service_url = "https://notifications.company.com/api/v2"
analytics_service_url = "https://analytics.company.com/api/v1"
```

```csharp
// Complete microservice configuration handler
public class MicroserviceConfiguration
{
    public ServiceConfig Service { get; set; }
    public DatabaseConfig Database { get; set; }
    public CacheConfig Cache { get; set; }
    public MessagingConfig Messaging { get; set; }
    public SecurityConfig Security { get; set; }
    public MonitoringConfig Monitoring { get; set; }
    public FeatureFlags Features { get; set; }
    public ExternalServicesConfig ExternalServices { get; set; }
    
    public static MicroserviceConfiguration FromTSK(TSK tsk)
    {
        return new MicroserviceConfiguration
        {
            Service = ServiceConfig.FromTSK(tsk, "service"),
            Database = DatabaseConfig.FromTSK(tsk, "database"),
            Cache = CacheConfig.FromTSK(tsk, "cache"),
            Messaging = MessagingConfig.FromTSK(tsk, "messaging"),
            Security = SecurityConfig.FromTSK(tsk, "security"),
            Monitoring = MonitoringConfig.FromTSK(tsk, "monitoring"),
            Features = FeatureFlags.FromTSK(tsk, "features"),
            ExternalServices = ExternalServicesConfig.FromTSK(tsk, "external_services")
        };
    }
    
    public void Validate()
    {
        Service?.Validate();
        Database?.Validate();
        Cache?.Validate();
        Messaging?.Validate();
        Security?.Validate();
        Monitoring?.Validate();
        Features?.Validate();
        ExternalServices?.Validate();
    }
}

public class ServiceConfig
{
    public string Name { get; set; }
    public string Version { get; set; }
    public int Port { get; set; }
    public string Host { get; set; }
    public string Environment { get; set; }
    
    public static ServiceConfig FromTSK(TSK tsk, string section)
    {
        return new ServiceConfig
        {
            Name = (string)tsk.GetValue(section, "name") ?? throw new ArgumentNullException("name"),
            Version = (string)tsk.GetValue(section, "version") ?? "1.0.0",
            Port = (int)(tsk.GetValue(section, "port") ?? 8080),
            Host = (string)tsk.GetValue(section, "host") ?? "localhost",
            Environment = (string)tsk.GetValue(section, "environment") ?? "development"
        };
    }
    
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            throw new ConfigurationException("Service name is required");
            
        if (Port < 1 || Port > 65535)
            throw new ConfigurationException("Service port must be between 1 and 65535");
    }
}
```

---

## Troubleshooting

### Common Issues and Solutions

#### Issue: Configuration values are null

**Symptoms:**
- `GetValue()` returns null for existing keys
- Unexpected null reference exceptions

**Solutions:**
```csharp
// Check section exists first
var section = tsk.GetSection("database");
if (section == null)
{
    Console.WriteLine("Database section not found");
    return;
}

// Use safe casting with default values
var port = (int?)(tsk.GetValue("database", "port")) ?? 5432;
var enableSsl = (bool?)(tsk.GetValue("database", "ssl")) ?? false;
```

#### Issue: File parsing errors

**Symptoms:**
- FormatException when loading configuration
- ArgumentException during parsing

**Debugging:**
```csharp
try
{
    var config = TSK.FromFile("config.tsk");
}
catch (Exception ex)
{
    Console.WriteLine($"Configuration parsing error: {ex.Message}");
    
    // Check file contents
    var content = File.ReadAllText("config.tsk");
    Console.WriteLine("File contents:");
    Console.WriteLine(content);
    
    // Try parsing line by line to find the problematic line
    var lines = content.Split('\n');
    for (int i = 0; i < lines.Length; i++)
    {
        var line = lines[i].Trim();
        if (!string.IsNullOrEmpty(line) && !line.StartsWith("#"))
        {
            Console.WriteLine($"Line {i + 1}: {line}");
        }
    }
}
```

#### Issue: Performance problems with large configurations

**Symptoms:**
- Slow configuration loading
- High memory usage
- Frequent garbage collection

**Solutions:**
```csharp
// Use configuration caching
private static readonly ConcurrentDictionary<string, TSK> _configCache = new();

public TSK GetCachedConfiguration(string file)
{
    return _configCache.GetOrAdd(file, _ => 
    {
        Console.WriteLine($"Loading configuration from {file}");
        return TSK.FromFile(file);
    });
}

// Pre-compile frequently accessed values
public class CompiledConfiguration
{
    private readonly Dictionary<string, object> _values = new();
    
    public CompiledConfiguration(TSK tsk)
    {
        // Pre-extract all needed values
        _values["db_host"] = tsk.GetValue("database", "host");
        _values["db_port"] = tsk.GetValue("database", "port");
        _values["cache_host"] = tsk.GetValue("cache", "host");
        // ... etc
    }
    
    public T GetValue<T>(string key) => (T)_values[key];
}
```

#### Issue: Thread safety problems

**Symptoms:**
- Intermittent exceptions in multi-threaded applications
- Configuration values changing unexpectedly

**Solution:**
```csharp
// Use thread-safe wrapper
public class ThreadSafeTSK
{
    private readonly TSK _tsk;
    private readonly ReaderWriterLockSlim _lock = new();
    
    public ThreadSafeTSK(TSK tsk)
    {
        _tsk = tsk;
    }
    
    public object GetValue(string section, string key)
    {
        _lock.EnterReadLock();
        try
        {
            return _tsk.GetValue(section, key);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }
    
    public void SetValue(string section, string key, object value)
    {
        _lock.EnterWriteLock();
        try
        {
            _tsk.SetValue(section, key, value);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
}
```

### Performance Debugging

```csharp
public static class ConfigurationDebugger
{
    public static void ProfileConfigurationLoading(string configFile)
    {
        var stopwatch = Stopwatch.StartNew();
        var initialMemory = GC.GetTotalMemory(true);
        
        // Load configuration
        var config = TSK.FromFile(configFile);
        
        stopwatch.Stop();
        var finalMemory = GC.GetTotalMemory(false);
        var memoryUsed = finalMemory - initialMemory;
        
        Console.WriteLine($"Configuration Loading Profile:");
        Console.WriteLine($"  File: {configFile}");
        Console.WriteLine($"  File Size: {new FileInfo(configFile).Length / 1024:F1} KB");
        Console.WriteLine($"  Load Time: {stopwatch.ElapsedMilliseconds} ms");
        Console.WriteLine($"  Memory Used: {memoryUsed / 1024:F1} KB");
        
        // Analyze configuration structure
        var sections = GetAllSections(config);
        Console.WriteLine($"  Sections: {sections.Count}");
        
        foreach (var section in sections)
        {
            var sectionData = config.GetSection(section);
            Console.WriteLine($"    [{section}]: {sectionData?.Count ?? 0} keys");
        }
    }
    
    private static List<string> GetAllSections(TSK config)
    {
        // This would require access to internal data structure
        // Implementation depends on TSK internal structure
        return new List<string>();
    }
}
```

---

## Version History and Migration Guide

### Version 2.0.1 (Current)
- Enhanced performance optimization
- Improved error handling
- Better thread safety
- Extended API documentation

### Migration from 1.x to 2.x

**Breaking Changes:**
- Some method signatures may have changed
- Error handling improvements may change exception types

**Migration Steps:**
```csharp
// Old way (1.x)
try
{
    var config = TSK.Parse(content); // Method name changed
}
catch (Exception ex)
{
    // Generic exception handling
}

// New way (2.x)
try
{
    var config = TSK.FromString(content); // New method name
}
catch (FormatException ex)
{
    // Specific exception for parsing errors
}
catch (ArgumentException ex)
{
    // Specific exception for argument errors
}
```

---

## Support and Community

### Getting Help

- **GitHub Issues**: [Report bugs and request features](https://github.com/cyber-boost/tusktsk/issues)
- **Documentation**: This comprehensive guide
- **Examples**: Check the `/examples` directory in the repository

### Contributing

We welcome contributions! Please see our contribution guidelines in the repository.

### License

This project is licensed under the BBL License. See LICENSE file for details.

---

*This documentation was generated for TuskTsk C# SDK v2.0.1*  
*Last Updated: January 2025*  
*© 2025 Cyberboost LLC* 