# Configuration Management in C# with TuskLang

## Overview

This guide covers comprehensive configuration management patterns for C# applications using TuskLang, including hierarchical configurations, environment-specific settings, and dynamic configuration updates.

## Table of Contents

1. [Basic Configuration](#basic-configuration)
2. [Hierarchical Configuration](#hierarchical-configuration)
3. [Environment-Specific Configuration](#environment-specific-configuration)
4. [Dynamic Configuration](#dynamic-configuration)
5. [TuskLang Integration](#tusklang-integration)

## Basic Configuration

### Simple Configuration Setup

```csharp
public class AppConfiguration
{
    public string AppName { get; set; } = "MyApp";
    public string Environment { get; set; } = "Development";
    public int Port { get; set; } = 5000;
    public bool EnableLogging { get; set; } = true;
    public DatabaseConfig Database { get; set; } = new();
    public ApiConfig Api { get; set; } = new();
}

public class DatabaseConfig
{
    public string ConnectionString { get; set; } = "";
    public int MaxConnections { get; set; } = 100;
    public TimeSpan CommandTimeout { get; set; } = TimeSpan.FromSeconds(30);
}

public class ApiConfig
{
    public string BaseUrl { get; set; } = "https://api.example.com";
    public string ApiKey { get; set; } = "";
    public int RetryCount { get; set; } = 3;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
```

### Configuration Loading

```csharp
public class ConfigurationLoader
{
    private readonly TuskLang _tuskLang;

    public ConfigurationLoader(TuskLang tuskLang)
    {
        _tuskLang = tuskLang;
    }

    public AppConfiguration LoadConfiguration()
    {
        var config = new AppConfiguration();

        // Load from TuskLang configuration
        config.AppName = _tuskLang.GetValue<string>("app.name", "MyApp");
        config.Environment = _tuskLang.GetValue<string>("app.environment", "Development");
        config.Port = _tuskLang.GetValue<int>("app.port", 5000);
        config.EnableLogging = _tuskLang.GetValue<bool>("app.enableLogging", true);

        // Load nested configurations
        config.Database.ConnectionString = _tuskLang.GetValue<string>("database.connectionString", "");
        config.Database.MaxConnections = _tuskLang.GetValue<int>("database.maxConnections", 100);
        config.Database.CommandTimeout = TimeSpan.FromSeconds(
            _tuskLang.GetValue<int>("database.commandTimeoutSeconds", 30));

        config.Api.BaseUrl = _tuskLang.GetValue<string>("api.baseUrl", "https://api.example.com");
        config.Api.ApiKey = _tuskLang.GetValue<string>("api.apiKey", "");
        config.Api.RetryCount = _tuskLang.GetValue<int>("api.retryCount", 3);
        config.Api.Timeout = TimeSpan.FromSeconds(
            _tuskLang.GetValue<int>("api.timeoutSeconds", 30));

        return config;
    }
}
```

## Hierarchical Configuration

### Configuration Hierarchy

```csharp
public class HierarchicalConfiguration
{
    private readonly Dictionary<string, object> _config;
    private readonly List<string> _configFiles;

    public HierarchicalConfiguration()
    {
        _config = new Dictionary<string, object>();
        _configFiles = new List<string>();
    }

    public void LoadConfigurationHierarchy()
    {
        // Load configuration files in order of precedence
        var configFiles = new[]
        {
            "config/default.tsk",           // Base configuration
            "config/environment.tsk",       // Environment-specific
            "config/local.tsk",            // Local overrides
            "config/secrets.tsk"           // Secrets (if exists)
        };

        foreach (var file in configFiles)
        {
            if (File.Exists(file))
            {
                LoadConfigurationFile(file);
                _configFiles.Add(file);
            }
        }
    }

    private void LoadConfigurationFile(string filePath)
    {
        var tuskLang = new TuskLang(filePath);
        var config = tuskLang.GetAllValues();

        foreach (var (key, value) in config)
        {
            _config[key] = value;
        }
    }

    public T GetValue<T>(string key, T defaultValue = default)
    {
        if (_config.TryGetValue(key, out var value))
        {
            if (value is T typedValue)
                return typedValue;

            // Try to convert the value
            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        return defaultValue;
    }

    public void SetValue<T>(string key, T value)
    {
        _config[key] = value;
    }

    public IEnumerable<string> GetLoadedFiles()
    {
        return _configFiles.AsReadOnly();
    }
}
```

## Environment-Specific Configuration

### Environment Configuration

```csharp
public class EnvironmentConfiguration
{
    private readonly string _environment;
    private readonly TuskLang _tuskLang;

    public EnvironmentConfiguration(string environment)
    {
        _environment = environment;
        _tuskLang = new TuskLang($"config/{environment}.tsk");
    }

    public AppConfiguration LoadEnvironmentConfig()
    {
        var config = new AppConfiguration
        {
            Environment = _environment
        };

        // Load environment-specific settings
        config.AppName = _tuskLang.GetValue<string>("app.name", "MyApp");
        config.Port = _tuskLang.GetValue<int>("app.port", 5000);

        // Database configuration
        config.Database.ConnectionString = _tuskLang.GetValue<string>("database.connectionString", "");
        config.Database.MaxConnections = _tuskLang.GetValue<int>("database.maxConnections", 100);

        // API configuration
        config.Api.BaseUrl = _tuskLang.GetValue<string>("api.baseUrl", "");
        config.Api.ApiKey = _tuskLang.GetValue<string>("api.apiKey", "");

        return config;
    }

    public bool IsDevelopment => _environment.Equals("Development", StringComparison.OrdinalIgnoreCase);
    public bool IsStaging => _environment.Equals("Staging", StringComparison.OrdinalIgnoreCase);
    public bool IsProduction => _environment.Equals("Production", StringComparison.OrdinalIgnoreCase);
}
```

## Dynamic Configuration

### Configuration Change Monitoring

```csharp
public class DynamicConfiguration
{
    private readonly FileSystemWatcher _fileWatcher;
    private readonly TuskLang _tuskLang;
    private readonly Action<AppConfiguration> _onConfigurationChanged;
    private readonly Timer _reloadTimer;

    public DynamicConfiguration(string configPath, Action<AppConfiguration> onConfigurationChanged)
    {
        _tuskLang = new TuskLang(configPath);
        _onConfigurationChanged = onConfigurationChanged;
        _reloadTimer = new Timer(ReloadConfiguration, null, Timeout.Infinite, Timeout.Infinite);

        // Watch for file changes
        _fileWatcher = new FileSystemWatcher(Path.GetDirectoryName(configPath))
        {
            Filter = Path.GetFileName(configPath),
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true
        };

        _fileWatcher.Changed += OnConfigFileChanged;
    }

    private void OnConfigFileChanged(object sender, FileSystemEventArgs e)
    {
        // Debounce rapid file changes
        _reloadTimer.Change(500, Timeout.Infinite);
    }

    private void ReloadConfiguration(object state)
    {
        try
        {
            var newConfig = LoadConfiguration();
            _onConfigurationChanged?.Invoke(newConfig);
        }
        catch (Exception ex)
        {
            // Log error but don't crash
            Console.WriteLine($"Error reloading configuration: {ex.Message}");
        }
    }

    private AppConfiguration LoadConfiguration()
    {
        return new AppConfiguration
        {
            AppName = _tuskLang.GetValue<string>("app.name", "MyApp"),
            Environment = _tuskLang.GetValue<string>("app.environment", "Development"),
            Port = _tuskLang.GetValue<int>("app.port", 5000),
            EnableLogging = _tuskLang.GetValue<bool>("app.enableLogging", true),
            Database = new DatabaseConfig
            {
                ConnectionString = _tuskLang.GetValue<string>("database.connectionString", ""),
                MaxConnections = _tuskLang.GetValue<int>("database.maxConnections", 100),
                CommandTimeout = TimeSpan.FromSeconds(
                    _tuskLang.GetValue<int>("database.commandTimeoutSeconds", 30))
            },
            Api = new ApiConfig
            {
                BaseUrl = _tuskLang.GetValue<string>("api.baseUrl", ""),
                ApiKey = _tuskLang.GetValue<string>("api.apiKey", ""),
                RetryCount = _tuskLang.GetValue<int>("api.retryCount", 3),
                Timeout = TimeSpan.FromSeconds(
                    _tuskLang.GetValue<int>("api.timeoutSeconds", 30))
            }
        };
    }

    public void Dispose()
    {
        _fileWatcher?.Dispose();
        _reloadTimer?.Dispose();
        _tuskLang?.Dispose();
    }
}
```

## TuskLang Integration

### TuskLang Configuration Service

```csharp
public class TuskLangConfigurationService
{
    private readonly TuskLang _tuskLang;
    private readonly ILogger<TuskLangConfigurationService> _logger;

    public TuskLangConfigurationService(TuskLang tuskLang, 
        ILogger<TuskLangConfigurationService> logger)
    {
        _tuskLang = tuskLang;
        _logger = logger;
    }

    public T GetValue<T>(string key, T defaultValue = default)
    {
        try
        {
            return _tuskLang.GetValue(key, defaultValue);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting configuration value for key {Key}", key);
            return defaultValue;
        }
    }

    public void SetValue<T>(string key, T value)
    {
        try
        {
            _tuskLang.SetValue(key, value);
            _logger.LogDebug("Configuration value set for key {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting configuration value for key {Key}", key);
            throw;
        }
    }

    public Dictionary<string, object> GetAllValues()
    {
        try
        {
            return _tuskLang.GetAllValues();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all configuration values");
            return new Dictionary<string, object>();
        }
    }

    public bool HasValue(string key)
    {
        try
        {
            return _tuskLang.HasValue(key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking if configuration key {Key} exists", key);
            return false;
        }
    }

    public void Reload()
    {
        try
        {
            _tuskLang.Reload();
            _logger.LogInformation("Configuration reloaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reloading configuration");
            throw;
        }
    }
}
```

### Configuration Dependency Injection

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTuskLangConfiguration(
        this IServiceCollection services, string configPath)
    {
        // Register TuskLang instance
        services.AddSingleton<TuskLang>(provider => new TuskLang(configPath));

        // Register configuration service
        services.AddSingleton<TuskLangConfigurationService>();

        // Register configuration objects
        services.AddSingleton<AppConfiguration>(provider =>
        {
            var configService = provider.GetRequiredService<TuskLangConfigurationService>();
            
            return new AppConfiguration
            {
                AppName = configService.GetValue<string>("app.name", "MyApp"),
                Environment = configService.GetValue<string>("app.environment", "Development"),
                Port = configService.GetValue<int>("app.port", 5000),
                EnableLogging = configService.GetValue<bool>("app.enableLogging", true),
                Database = new DatabaseConfig
                {
                    ConnectionString = configService.GetValue<string>("database.connectionString", ""),
                    MaxConnections = configService.GetValue<int>("database.maxConnections", 100),
                    CommandTimeout = TimeSpan.FromSeconds(
                        configService.GetValue<int>("database.commandTimeoutSeconds", 30))
                },
                Api = new ApiConfig
                {
                    BaseUrl = configService.GetValue<string>("api.baseUrl", ""),
                    ApiKey = configService.GetValue<string>("api.apiKey", ""),
                    RetryCount = configService.GetValue<int>("api.retryCount", 3),
                    Timeout = TimeSpan.FromSeconds(
                        configService.GetValue<int>("api.timeoutSeconds", 30))
                }
            };
        });

        return services;
    }
}
```

## Summary

This comprehensive configuration management guide covers:

- **Basic Configuration**: Simple configuration setup and loading
- **Hierarchical Configuration**: Multi-level configuration with precedence
- **Environment-Specific Configuration**: Different settings for different environments
- **Dynamic Configuration**: Hot reloading and change monitoring
- **TuskLang Integration**: Deep integration with TuskLang features

The patterns ensure robust, secure, and maintainable configuration management in C# applications using TuskLang. 