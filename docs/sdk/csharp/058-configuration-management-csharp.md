# Configuration Management in C# with TuskLang

## Overview

Effective configuration management is essential for modern applications. This guide covers comprehensive configuration patterns for C# applications using TuskLang, including hierarchical configurations, environment-specific settings, validation, and dynamic updates.

## Table of Contents

1. [Basic Configuration](#basic-configuration)
2. [Hierarchical Configuration](#hierarchical-configuration)
3. [Environment-Specific Configuration](#environment-specific-configuration)
4. [Configuration Validation](#configuration-validation)
5. [Dynamic Configuration](#dynamic-configuration)
6. [Secret Management](#secret-management)
7. [Configuration Providers](#configuration-providers)
8. [TuskLang Integration](#tusklang-integration)
9. [Best Practices](#best-practices)

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

### Configuration Registration

```csharp
public static class ConfigurationExtensions
{
    public static IServiceCollection AddAppConfiguration(
        this IServiceCollection services, IConfiguration configuration)
    {
        // Bind configuration to strongly-typed objects
        services.Configure<AppConfiguration>(configuration.GetSection("App"));
        services.Configure<DatabaseConfig>(configuration.GetSection("Database"));
        services.Configure<ApiConfig>(configuration.GetSection("Api"));

        // Register configuration as singleton
        services.AddSingleton<AppConfiguration>(provider =>
        {
            var config = new AppConfiguration();
            configuration.GetSection("App").Bind(config);
            configuration.GetSection("Database").Bind(config.Database);
            configuration.GetSection("Api").Bind(config.Api);
            return config;
        });

        return services;
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

### Configuration Override System

```csharp
public class ConfigurationOverride
{
    private readonly Dictionary<string, object> _overrides;
    private readonly HierarchicalConfiguration _baseConfig;

    public ConfigurationOverride(HierarchicalConfiguration baseConfig)
    {
        _baseConfig = baseConfig;
        _overrides = new Dictionary<string, object>();
    }

    public void OverrideValue<T>(string key, T value)
    {
        _overrides[key] = value;
    }

    public T GetValue<T>(string key, T defaultValue = default)
    {
        // Check overrides first
        if (_overrides.TryGetValue(key, out var overrideValue))
        {
            if (overrideValue is T typedValue)
                return typedValue;

            try
            {
                return (T)Convert.ChangeType(overrideValue, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        // Fall back to base configuration
        return _baseConfig.GetValue(key, defaultValue);
    }

    public void ClearOverrides()
    {
        _overrides.Clear();
    }

    public Dictionary<string, object> GetOverrides()
    {
        return new Dictionary<string, object>(_overrides);
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

### Environment-Specific Settings

```csharp
public class EnvironmentSettings
{
    public static AppConfiguration GetConfiguration(string environment)
    {
        var baseConfig = LoadBaseConfiguration();
        var envConfig = LoadEnvironmentConfiguration(environment);
        
        return MergeConfigurations(baseConfig, envConfig);
    }

    private static AppConfiguration LoadBaseConfiguration()
    {
        var tuskLang = new TuskLang("config/base.tsk");
        
        return new AppConfiguration
        {
            AppName = tuskLang.GetValue<string>("app.name", "MyApp"),
            EnableLogging = tuskLang.GetValue<bool>("app.enableLogging", true),
            Database = new DatabaseConfig
            {
                CommandTimeout = TimeSpan.FromSeconds(
                    tuskLang.GetValue<int>("database.commandTimeoutSeconds", 30))
            },
            Api = new ApiConfig
            {
                RetryCount = tuskLang.GetValue<int>("api.retryCount", 3),
                Timeout = TimeSpan.FromSeconds(
                    tuskLang.GetValue<int>("api.timeoutSeconds", 30))
            }
        };
    }

    private static AppConfiguration LoadEnvironmentConfiguration(string environment)
    {
        var tuskLang = new TuskLang($"config/{environment}.tsk");
        
        return new AppConfiguration
        {
            Environment = environment,
            Port = tuskLang.GetValue<int>("app.port", 5000),
            Database = new DatabaseConfig
            {
                ConnectionString = tuskLang.GetValue<string>("database.connectionString", ""),
                MaxConnections = tuskLang.GetValue<int>("database.maxConnections", 100)
            },
            Api = new ApiConfig
            {
                BaseUrl = tuskLang.GetValue<string>("api.baseUrl", ""),
                ApiKey = tuskLang.GetValue<string>("api.apiKey", "")
            }
        };
    }

    private static AppConfiguration MergeConfigurations(
        AppConfiguration baseConfig, AppConfiguration envConfig)
    {
        return new AppConfiguration
        {
            AppName = envConfig.AppName ?? baseConfig.AppName,
            Environment = envConfig.Environment,
            Port = envConfig.Port,
            EnableLogging = baseConfig.EnableLogging,
            Database = new DatabaseConfig
            {
                ConnectionString = envConfig.Database.ConnectionString,
                MaxConnections = envConfig.Database.MaxConnections,
                CommandTimeout = baseConfig.Database.CommandTimeout
            },
            Api = new ApiConfig
            {
                BaseUrl = envConfig.Api.BaseUrl,
                ApiKey = envConfig.Api.ApiKey,
                RetryCount = baseConfig.Api.RetryCount,
                Timeout = baseConfig.Api.Timeout
            }
        };
    }
}
```

## Configuration Validation

### Configuration Validator

```csharp
public class ConfigurationValidator
{
    private readonly List<ValidationError> _errors;

    public ConfigurationValidator()
    {
        _errors = new List<ValidationError>();
    }

    public bool ValidateConfiguration(AppConfiguration config)
    {
        _errors.Clear();

        // Validate required fields
        ValidateRequired(config.AppName, "AppName");
        ValidateRequired(config.Database.ConnectionString, "Database.ConnectionString");
        ValidateRequired(config.Api.BaseUrl, "Api.BaseUrl");
        ValidateRequired(config.Api.ApiKey, "Api.ApiKey");

        // Validate ranges
        ValidateRange(config.Port, 1, 65535, "Port");
        ValidateRange(config.Database.MaxConnections, 1, 1000, "Database.MaxConnections");
        ValidateRange(config.Api.RetryCount, 0, 10, "Api.RetryCount");

        // Validate URLs
        ValidateUrl(config.Api.BaseUrl, "Api.BaseUrl");

        // Validate connection string format
        ValidateConnectionString(config.Database.ConnectionString, "Database.ConnectionString");

        return _errors.Count == 0;
    }

    private void ValidateRequired(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            _errors.Add(new ValidationError(fieldName, "Value is required"));
        }
    }

    private void ValidateRange(int value, int min, int max, string fieldName)
    {
        if (value < min || value > max)
        {
            _errors.Add(new ValidationError(fieldName, 
                $"Value must be between {min} and {max}"));
        }
    }

    private void ValidateUrl(string url, string fieldName)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            _errors.Add(new ValidationError(fieldName, "Invalid URL format"));
        }
    }

    private void ValidateConnectionString(string connectionString, string fieldName)
    {
        if (!string.IsNullOrEmpty(connectionString))
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(connectionString);
                // Basic validation passed
            }
            catch
            {
                _errors.Add(new ValidationError(fieldName, "Invalid connection string format"));
            }
        }
    }

    public IEnumerable<ValidationError> GetErrors()
    {
        return _errors.AsReadOnly();
    }
}

public class ValidationError
{
    public string FieldName { get; }
    public string Message { get; }

    public ValidationError(string fieldName, string message)
    {
        FieldName = fieldName;
        Message = message;
    }

    public override string ToString()
    {
        return $"{FieldName}: {Message}";
    }
}
```

### Fluent Validation

```csharp
public class ConfigurationFluentValidator
{
    public static ValidationResult Validate(AppConfiguration config)
    {
        var validator = new InlineValidator<AppConfiguration>();

        validator.RuleFor(x => x.AppName)
            .NotEmpty()
            .MaximumLength(100);

        validator.RuleFor(x => x.Port)
            .InclusiveBetween(1, 65535);

        validator.RuleFor(x => x.Database.ConnectionString)
            .NotEmpty()
            .Must(BeValidConnectionString)
            .WithMessage("Invalid database connection string");

        validator.RuleFor(x => x.Database.MaxConnections)
            .InclusiveBetween(1, 1000);

        validator.RuleFor(x => x.Api.BaseUrl)
            .NotEmpty()
            .Must(BeValidUrl)
            .WithMessage("Invalid API base URL");

        validator.RuleFor(x => x.Api.ApiKey)
            .NotEmpty()
            .MinimumLength(10);

        validator.RuleFor(x => x.Api.RetryCount)
            .InclusiveBetween(0, 10);

        return validator.Validate(config);
    }

    private static bool BeValidConnectionString(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            return false;

        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool BeValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out _);
    }
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

### Configuration Hot Reload

```csharp
public class ConfigurationHotReload
{
    private readonly IOptionsMonitor<AppConfiguration> _configMonitor;
    private readonly ILogger<ConfigurationHotReload> _logger;

    public ConfigurationHotReload(IOptionsMonitor<AppConfiguration> configMonitor, 
        ILogger<ConfigurationHotReload> logger)
    {
        _configMonitor = configMonitor;
        _logger = logger;

        // Subscribe to configuration changes
        _configMonitor.OnChange(OnConfigurationChanged);
    }

    private void OnConfigurationChanged(AppConfiguration newConfig, string name)
    {
        _logger.LogInformation("Configuration changed for {Name}", name);
        
        // Handle configuration changes
        HandleConfigurationChange(newConfig);
    }

    private void HandleConfigurationChange(AppConfiguration newConfig)
    {
        // Update application settings based on configuration changes
        if (newConfig.EnableLogging)
        {
            _logger.LogInformation("Logging enabled");
        }
        else
        {
            _logger.LogInformation("Logging disabled");
        }

        // Update database connection if changed
        if (!string.IsNullOrEmpty(newConfig.Database.ConnectionString))
        {
            _logger.LogInformation("Database connection string updated");
        }

        // Update API settings if changed
        if (!string.IsNullOrEmpty(newConfig.Api.BaseUrl))
        {
            _logger.LogInformation("API base URL updated to {BaseUrl}", newConfig.Api.BaseUrl);
        }
    }

    public AppConfiguration CurrentConfiguration => _configMonitor.CurrentValue;
}
```

## Secret Management

### Secret Configuration

```csharp
public class SecretConfiguration
{
    private readonly TuskLang _tuskLang;
    private readonly IEncryptionService _encryptionService;

    public SecretConfiguration(TuskLang tuskLang, IEncryptionService encryptionService)
    {
        _tuskLang = tuskLang;
        _encryptionService = encryptionService;
    }

    public string GetSecret(string key, string defaultValue = "")
    {
        var encryptedValue = _tuskLang.GetValue<string>($"secrets.{key}", defaultValue);
        
        if (string.IsNullOrEmpty(encryptedValue) || encryptedValue == defaultValue)
            return defaultValue;

        try
        {
            return _encryptionService.Decrypt(encryptedValue);
        }
        catch
        {
            return defaultValue;
        }
    }

    public void SetSecret(string key, string value)
    {
        var encryptedValue = _encryptionService.Encrypt(value);
        _tuskLang.SetValue($"secrets.{key}", encryptedValue);
    }

    public Dictionary<string, string> GetAllSecrets()
    {
        var secrets = new Dictionary<string, string>();
        var allValues = _tuskLang.GetAllValues();

        foreach (var (key, value) in allValues)
        {
            if (key.StartsWith("secrets."))
            {
                var secretKey = key.Substring("secrets.".Length);
                var decryptedValue = GetSecret(secretKey);
                secrets[secretKey] = decryptedValue;
            }
        }

        return secrets;
    }
}

public interface IEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}

public class AesEncryptionService : IEncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public AesEncryptionService(string key, string iv)
    {
        _key = Convert.FromBase64String(key);
        _iv = Convert.FromBase64String(iv);
    }

    public string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        return Convert.ToBase64String(cipherBytes);
    }

    public string Decrypt(string cipherText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        using var decryptor = aes.CreateDecryptor();
        var cipherBytes = Convert.FromBase64String(cipherText);
        var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

        return Encoding.UTF8.GetString(plainBytes);
    }
}
```

## Configuration Providers

### Custom Configuration Provider

```csharp
public class TuskLangConfigurationProvider : ConfigurationProvider
{
    private readonly string _filePath;
    private readonly FileSystemWatcher _fileWatcher;

    public TuskLangConfigurationProvider(string filePath)
    {
        _filePath = filePath;
        
        if (File.Exists(filePath))
        {
            _fileWatcher = new FileSystemWatcher(Path.GetDirectoryName(filePath))
            {
                Filter = Path.GetFileName(filePath),
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = true
            };

            _fileWatcher.Changed += OnConfigFileChanged;
        }
    }

    public override void Load()
    {
        if (!File.Exists(_filePath))
            return;

        try
        {
            var tuskLang = new TuskLang(_filePath);
            var config = tuskLang.GetAllValues();

            Data.Clear();
            foreach (var (key, value) in config)
            {
                Data[key] = value?.ToString();
            }
        }
        catch (Exception ex)
        {
            // Log error but don't crash
            Console.WriteLine($"Error loading TuskLang configuration: {ex.Message}");
        }
    }

    private void OnConfigFileChanged(object sender, FileSystemEventArgs e)
    {
        // Reload configuration when file changes
        Load();
        OnReload();
    }

    protected override void Dispose(bool disposing)
    {
        _fileWatcher?.Dispose();
        base.Dispose(disposing);
    }
}

public class TuskLangConfigurationSource : IConfigurationSource
{
    private readonly string _filePath;

    public TuskLangConfigurationSource(string filePath)
    {
        _filePath = filePath;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new TuskLangConfigurationProvider(_filePath);
    }
}

public static class ConfigurationBuilderExtensions
{
    public static IConfigurationBuilder AddTuskLangFile(
        this IConfigurationBuilder builder, string path)
    {
        return builder.Add(new TuskLangConfigurationSource(path));
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

## Best Practices

### Configuration Best Practices

```csharp
public class ConfigurationBestPractices
{
    private readonly ILogger<ConfigurationBestPractices> _logger;

    public ConfigurationBestPractices(ILogger<ConfigurationBestPractices> logger)
    {
        _logger = logger;
    }

    // ✅ Good: Use strongly-typed configuration
    public void UseStronglyTypedConfig(AppConfiguration config)
    {
        var port = config.Port; // Type-safe access
        var connectionString = config.Database.ConnectionString;
    }

    // ❌ Bad: Use string-based configuration
    public void UseStringBasedConfig(IConfiguration configuration)
    {
        var port = int.Parse(configuration["Port"]); // Error-prone
        var connectionString = configuration["Database:ConnectionString"];
    }

    // ✅ Good: Validate configuration on startup
    public void ValidateOnStartup(AppConfiguration config)
    {
        var validator = new ConfigurationValidator();
        
        if (!validator.ValidateConfiguration(config))
        {
            var errors = validator.GetErrors();
            foreach (var error in errors)
            {
                _logger.LogError("Configuration error: {Error}", error);
            }
            
            throw new InvalidOperationException("Invalid configuration");
        }
    }

    // ✅ Good: Use environment-specific configuration
    public AppConfiguration LoadEnvironmentConfig(string environment)
    {
        var configPath = $"config/{environment}.tsk";
        
        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException($"Configuration file not found: {configPath}");
        }

        return EnvironmentSettings.GetConfiguration(environment);
    }

    // ✅ Good: Handle configuration changes gracefully
    public void HandleConfigChanges(AppConfiguration newConfig)
    {
        try
        {
            // Validate new configuration
            var validator = new ConfigurationValidator();
            if (!validator.ValidateConfiguration(newConfig))
            {
                _logger.LogWarning("Invalid configuration change, keeping current config");
                return;
            }

            // Apply configuration changes
            ApplyConfigurationChanges(newConfig);
            
            _logger.LogInformation("Configuration updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying configuration changes");
        }
    }

    private void ApplyConfigurationChanges(AppConfiguration newConfig)
    {
        // Apply changes to running application
        // This could involve restarting services, updating connections, etc.
    }
}
```

### Security Best Practices

```csharp
public class ConfigurationSecurity
{
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<ConfigurationSecurity> _logger;

    public ConfigurationSecurity(IEncryptionService encryptionService, 
        ILogger<ConfigurationSecurity> logger)
    {
        _encryptionService = encryptionService;
        _logger = logger;
    }

    // ✅ Good: Encrypt sensitive configuration
    public void StoreSensitiveConfig(string key, string value)
    {
        var encryptedValue = _encryptionService.Encrypt(value);
        // Store encrypted value in configuration
    }

    // ✅ Good: Use secure configuration providers
    public void UseSecureProvider()
    {
        // Use Azure Key Vault, AWS Secrets Manager, or similar
        // for production secrets
    }

    // ✅ Good: Validate configuration permissions
    public void ValidateConfigPermissions(string configPath)
    {
        var fileInfo = new FileInfo(configPath);
        var permissions = fileInfo.Attributes;

        if ((permissions & FileAttributes.ReadOnly) == 0)
        {
            _logger.LogWarning("Configuration file is not read-only");
        }
    }

    // ✅ Good: Audit configuration access
    public void AuditConfigAccess(string key, string user)
    {
        _logger.LogInformation("Configuration accessed - Key: {Key}, User: {User}", key, user);
    }
}
```

## Summary

This comprehensive configuration management guide covers:

- **Basic Configuration**: Simple configuration setup and loading
- **Hierarchical Configuration**: Multi-level configuration with precedence
- **Environment-Specific Configuration**: Different settings for different environments
- **Configuration Validation**: Ensuring configuration correctness
- **Dynamic Configuration**: Hot reloading and change monitoring
- **Secret Management**: Secure handling of sensitive configuration
- **Configuration Providers**: Custom providers for TuskLang integration
- **TuskLang Integration**: Deep integration with TuskLang features
- **Best Practices**: Security, validation, and maintainability guidelines

The patterns ensure robust, secure, and maintainable configuration management in C# applications using TuskLang. 