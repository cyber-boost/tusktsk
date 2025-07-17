# Configuration Management in C# TuskLang

## Overview

Configuration management is the backbone of any TuskLang application. This guide covers advanced configuration patterns, hierarchical structures, hot reloading, validation, and best practices for C# applications.

## 🏗️ Hierarchical Configuration

### Multi-Level Configuration Structure

```csharp
// Configuration hierarchy from most specific to least specific
public class ConfigurationHierarchy
{
    public string Environment { get; set; } = "development";
    public string ConfigPath { get; set; } = "config";
    
    public async Task<TSKConfig> LoadHierarchicalConfig()
    {
        var config = new TSKConfig();
        
        // Load in order of specificity
        await config.LoadFileAsync("config/default.tsk");
        await config.LoadFileAsync($"config/{Environment}.tsk");
        await config.LoadFileAsync("config/local.tsk"); // Override for local development
        
        return config;
    }
}
```

### Environment-Specific Overrides

```csharp
// peanu.tsk - Base configuration
[database]
host = "localhost"
port = 5432
name = "myapp"

[api]
base_url = "https://api.example.com"
timeout = 30

// peanu.production.tsk - Production overrides
[database]
host = @env("DB_HOST", "prod-db.example.com")
port = @env("DB_PORT", "5432")

[api]
base_url = @env("API_BASE_URL", "https://api.production.com")
timeout = 60

// peanu.staging.tsk - Staging overrides
[database]
host = @env("DB_HOST", "staging-db.example.com")

[api]
base_url = @env("API_BASE_URL", "https://api.staging.com")
```

### Configuration Inheritance

```csharp
public class InheritedConfiguration
{
    public async Task<TSKConfig> LoadWithInheritance()
    {
        var config = new TSKConfig();
        
        // Load base configuration
        await config.LoadFileAsync("config/base.tsk");
        
        // Load environment-specific overrides
        var environment = Environment.GetEnvironmentVariable("APP_ENV") ?? "development";
        await config.LoadFileAsync($"config/{environment}.tsk");
        
        // Load feature-specific configurations
        await config.LoadFileAsync("config/features/database.tsk");
        await config.LoadFileAsync("config/features/api.tsk");
        await config.LoadFileAsync("config/features/cache.tsk");
        
        return config;
    }
}
```

## 🔄 Hot Reloading

### File System Watcher

```csharp
public class HotReloadConfiguration
{
    private readonly TSKConfig _config;
    private readonly FileSystemWatcher _watcher;
    private readonly ILogger<HotReloadConfiguration> _logger;
    
    public HotReloadConfiguration(TSKConfig config, ILogger<HotReloadConfiguration> logger)
    {
        _config = config;
        _logger = logger;
        
        _watcher = new FileSystemWatcher("config")
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
            Filter = "*.tsk",
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };
        
        _watcher.Changed += OnConfigFileChanged;
        _watcher.Created += OnConfigFileChanged;
        _watcher.Deleted += OnConfigFileChanged;
    }
    
    private async void OnConfigFileChanged(object sender, FileSystemEventArgs e)
    {
        try
        {
            _logger.LogInformation("Configuration file changed: {FilePath}", e.FullPath);
            
            // Debounce rapid changes
            await Task.Delay(100);
            
            // Reload configuration
            await _config.ReloadAsync();
            
            _logger.LogInformation("Configuration reloaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reload configuration");
        }
    }
    
    public void Dispose()
    {
        _watcher?.Dispose();
    }
}
```

### Configuration Change Notifications

```csharp
public class ConfigurationChangeNotifier
{
    private readonly ILogger<ConfigurationChangeNotifier> _logger;
    private readonly List<IConfigurationChangeHandler> _handlers = new();
    
    public event EventHandler<ConfigurationChangedEventArgs> ConfigurationChanged;
    
    public ConfigurationChangeNotifier(ILogger<ConfigurationChangeNotifier> logger)
    {
        _logger = logger;
    }
    
    public void RegisterHandler(IConfigurationChangeHandler handler)
    {
        _handlers.Add(handler);
    }
    
    public async Task NotifyConfigurationChanged(TSKConfig oldConfig, TSKConfig newConfig)
    {
        var args = new ConfigurationChangedEventArgs(oldConfig, newConfig);
        
        // Notify event subscribers
        ConfigurationChanged?.Invoke(this, args);
        
        // Notify registered handlers
        foreach (var handler in _handlers)
        {
            try
            {
                await handler.OnConfigurationChangedAsync(args);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Handler {HandlerType} failed to process configuration change", 
                    handler.GetType().Name);
            }
        }
    }
}

public interface IConfigurationChangeHandler
{
    Task OnConfigurationChangedAsync(ConfigurationChangedEventArgs args);
}

public class ConfigurationChangedEventArgs : EventArgs
{
    public TSKConfig OldConfig { get; }
    public TSKConfig NewConfig { get; }
    
    public ConfigurationChangedEventArgs(TSKConfig oldConfig, TSKConfig newConfig)
    {
        OldConfig = oldConfig;
        NewConfig = newConfig;
    }
}
```

### Database-Driven Configuration

```csharp
public class DatabaseConfigurationProvider
{
    private readonly IDbConnection _connection;
    private readonly ILogger<DatabaseConfigurationProvider> _logger;
    private readonly Timer _refreshTimer;
    
    public DatabaseConfigurationProvider(IDbConnection connection, ILogger<DatabaseConfigurationProvider> logger)
    {
        _connection = connection;
        _logger = logger;
        
        // Refresh configuration every 5 minutes
        _refreshTimer = new Timer(RefreshConfiguration, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
    }
    
    private async void RefreshConfiguration(object state)
    {
        try
        {
            var query = @"
                SELECT key, value, environment 
                FROM configuration 
                WHERE environment = @environment OR environment = 'global'
                ORDER BY environment DESC, updated_at DESC";
            
            var parameters = new { environment = Environment.GetEnvironmentVariable("APP_ENV") };
            
            var configData = await _connection.QueryAsync<ConfigEntry>(query, parameters);
            
            // Update configuration
            await UpdateConfigurationFromDatabase(configData);
            
            _logger.LogInformation("Configuration refreshed from database");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh configuration from database");
        }
    }
    
    private async Task UpdateConfigurationFromDatabase(IEnumerable<ConfigEntry> configData)
    {
        var config = new TSKConfig();
        
        foreach (var entry in configData)
        {
            config.Set(entry.Key, entry.Value);
        }
        
        // Notify configuration change
        // Implementation depends on your notification system
    }
}

public class ConfigEntry
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}
```

## ✅ Configuration Validation

### Schema Validation

```csharp
public class ConfigurationValidator
{
    private readonly ILogger<ConfigurationValidator> _logger;
    
    public ConfigurationValidator(ILogger<ConfigurationValidator> logger)
    {
        _logger = logger;
    }
    
    public async Task<ValidationResult> ValidateConfigurationAsync(TSKConfig config)
    {
        var result = new ValidationResult();
        
        // Validate required sections
        await ValidateRequiredSectionsAsync(config, result);
        
        // Validate database configuration
        await ValidateDatabaseConfigAsync(config, result);
        
        // Validate API configuration
        await ValidateApiConfigAsync(config, result);
        
        // Validate security configuration
        await ValidateSecurityConfigAsync(config, result);
        
        return result;
    }
    
    private async Task ValidateRequiredSectionsAsync(TSKConfig config, ValidationResult result)
    {
        var requiredSections = new[] { "database", "api", "security", "logging" };
        
        foreach (var section in requiredSections)
        {
            if (!config.HasSection(section))
            {
                result.AddError($"Missing required section: {section}");
            }
        }
    }
    
    private async Task ValidateDatabaseConfigAsync(TSKConfig config, ValidationResult result)
    {
        var dbConfig = config.GetSection("database");
        
        if (dbConfig == null) return;
        
        // Validate connection string
        var connectionString = dbConfig.Get<string>("connection_string");
        if (string.IsNullOrEmpty(connectionString))
        {
            result.AddError("Database connection string is required");
        }
        else
        {
            // Test database connection
            try
            {
                using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();
                await connection.CloseAsync();
            }
            catch (Exception ex)
            {
                result.AddError($"Database connection failed: {ex.Message}");
            }
        }
        
        // Validate timeout values
        var timeout = dbConfig.Get<int>("timeout", 30);
        if (timeout <= 0 || timeout > 300)
        {
            result.AddError("Database timeout must be between 1 and 300 seconds");
        }
    }
    
    private async Task ValidateApiConfigAsync(TSKConfig config, ValidationResult result)
    {
        var apiConfig = config.GetSection("api");
        
        if (apiConfig == null) return;
        
        // Validate base URL
        var baseUrl = apiConfig.Get<string>("base_url");
        if (string.IsNullOrEmpty(baseUrl))
        {
            result.AddError("API base URL is required");
        }
        else if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out _))
        {
            result.AddError("API base URL must be a valid absolute URL");
        }
        
        // Validate timeout
        var timeout = apiConfig.Get<int>("timeout", 30);
        if (timeout <= 0 || timeout > 300)
        {
            result.AddError("API timeout must be between 1 and 300 seconds");
        }
    }
    
    private async Task ValidateSecurityConfigAsync(TSKConfig config, ValidationResult result)
    {
        var securityConfig = config.GetSection("security");
        
        if (securityConfig == null) return;
        
        // Validate JWT secret
        var jwtSecret = securityConfig.Get<string>("jwt_secret");
        if (string.IsNullOrEmpty(jwtSecret))
        {
            result.AddError("JWT secret is required");
        }
        else if (jwtSecret.Length < 32)
        {
            result.AddError("JWT secret must be at least 32 characters long");
        }
        
        // Validate encryption key
        var encryptionKey = securityConfig.Get<string>("encryption_key");
        if (string.IsNullOrEmpty(encryptionKey))
        {
            result.AddError("Encryption key is required");
        }
        else if (encryptionKey.Length < 32)
        {
            result.AddError("Encryption key must be at least 32 characters long");
        }
    }
}

public class ValidationResult
{
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();
    
    public bool IsValid => Errors.Count == 0;
    
    public void AddError(string error)
    {
        Errors.Add(error);
    }
    
    public void AddWarning(string warning)
    {
        Warnings.Add(warning);
    }
}
```

### Custom Validation Attributes

```csharp
[AttributeUsage(AttributeTargets.Property)]
public class ConfigurationValidationAttribute : Attribute
{
    public string ValidationRule { get; }
    public string ErrorMessage { get; }
    
    public ConfigurationValidationAttribute(string validationRule, string errorMessage)
    {
        ValidationRule = validationRule;
        ErrorMessage = errorMessage;
    }
}

public class DatabaseConfiguration
{
    [ConfigurationValidation("required", "Connection string is required")]
    public string ConnectionString { get; set; } = string.Empty;
    
    [ConfigurationValidation("range:1-300", "Timeout must be between 1 and 300 seconds")]
    public int Timeout { get; set; } = 30;
    
    [ConfigurationValidation("range:1-100", "Max connections must be between 1 and 100")]
    public int MaxConnections { get; set; } = 20;
}

public class ApiConfiguration
{
    [ConfigurationValidation("url", "Base URL must be a valid URL")]
    public string BaseUrl { get; set; } = string.Empty;
    
    [ConfigurationValidation("range:1-300", "Timeout must be between 1 and 300 seconds")]
    public int Timeout { get; set; } = 30;
    
    [ConfigurationValidation("range:1-10", "Retry count must be between 1 and 10")]
    public int RetryCount { get; set; } = 3;
}
```

## 🔐 Secure Configuration

### Environment Variable Integration

```csharp
public class SecureConfigurationProvider
{
    private readonly ILogger<SecureConfigurationProvider> _logger;
    
    public SecureConfigurationProvider(ILogger<SecureConfigurationProvider> logger)
    {
        _logger = logger;
    }
    
    public async Task<TSKConfig> LoadSecureConfigurationAsync()
    {
        var config = new TSKConfig();
        
        // Load base configuration
        await config.LoadFileAsync("config/secure.tsk");
        
        // Replace sensitive values with environment variables
        await ReplaceSensitiveValuesAsync(config);
        
        return config;
    }
    
    private async Task ReplaceSensitiveValuesAsync(TSKConfig config)
    {
        // Database credentials
        var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");
        if (!string.IsNullOrEmpty(dbPassword))
        {
            config.Set("database.password", dbPassword);
        }
        
        // API keys
        var apiKey = Environment.GetEnvironmentVariable("API_KEY");
        if (!string.IsNullOrEmpty(apiKey))
        {
            config.Set("api.key", apiKey);
        }
        
        // JWT secret
        var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET");
        if (!string.IsNullOrEmpty(jwtSecret))
        {
            config.Set("security.jwt_secret", jwtSecret);
        }
        
        // Encryption key
        var encryptionKey = Environment.GetEnvironmentVariable("ENCRYPTION_KEY");
        if (!string.IsNullOrEmpty(encryptionKey))
        {
            config.Set("security.encryption_key", encryptionKey);
        }
    }
}
```

### Encrypted Configuration Values

```csharp
public class EncryptedConfigurationProvider
{
    private readonly string _encryptionKey;
    private readonly ILogger<EncryptedConfigurationProvider> _logger;
    
    public EncryptedConfigurationProvider(string encryptionKey, ILogger<EncryptedConfigurationProvider> logger)
    {
        _encryptionKey = encryptionKey;
        _logger = logger;
    }
    
    public async Task<TSKConfig> LoadEncryptedConfigurationAsync()
    {
        var config = new TSKConfig();
        
        // Load encrypted configuration
        await config.LoadFileAsync("config/encrypted.tsk");
        
        // Decrypt sensitive values
        await DecryptSensitiveValuesAsync(config);
        
        return config;
    }
    
    private async Task DecryptSensitiveValuesAsync(TSKConfig config)
    {
        var sensitiveKeys = new[] { "database.password", "api.key", "security.jwt_secret" };
        
        foreach (var key in sensitiveKeys)
        {
            var encryptedValue = config.Get<string>(key);
            if (!string.IsNullOrEmpty(encryptedValue) && encryptedValue.StartsWith("ENC:"))
            {
                try
                {
                    var decryptedValue = await DecryptValueAsync(encryptedValue.Substring(4));
                    config.Set(key, decryptedValue);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to decrypt configuration value for key: {Key}", key);
                }
            }
        }
    }
    
    private async Task<string> DecryptValueAsync(string encryptedValue)
    {
        // Implementation depends on your encryption library
        // Example using AES encryption
        using var aes = Aes.Create();
        aes.Key = Convert.FromBase64String(_encryptionKey);
        
        var encryptedBytes = Convert.FromBase64String(encryptedValue);
        var iv = new byte[16];
        var ciphertext = new byte[encryptedBytes.Length - 16];
        
        Array.Copy(encryptedBytes, 0, iv, 0, 16);
        Array.Copy(encryptedBytes, 16, ciphertext, 0, ciphertext.Length);
        
        aes.IV = iv;
        
        using var decryptor = aes.CreateDecryptor();
        using var ms = new MemoryStream(ciphertext);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var reader = new StreamReader(cs);
        
        return await reader.ReadToEndAsync();
    }
}
```

## 📊 Configuration Analytics

### Usage Tracking

```csharp
public class ConfigurationAnalytics
{
    private readonly ILogger<ConfigurationAnalytics> _logger;
    private readonly Dictionary<string, int> _accessCounts = new();
    private readonly Dictionary<string, DateTime> _lastAccess = new();
    
    public ConfigurationAnalytics(ILogger<ConfigurationAnalytics> logger)
    {
        _logger = logger;
    }
    
    public void TrackAccess(string key)
    {
        lock (_accessCounts)
        {
            _accessCounts[key] = _accessCounts.GetValueOrDefault(key, 0) + 1;
            _lastAccess[key] = DateTime.UtcNow;
        }
    }
    
    public async Task<ConfigurationUsageReport> GenerateReportAsync()
    {
        lock (_accessCounts)
        {
            var report = new ConfigurationUsageReport
            {
                TotalAccesses = _accessCounts.Values.Sum(),
                MostAccessedKeys = _accessCounts
                    .OrderByDescending(x => x.Value)
                    .Take(10)
                    .ToDictionary(x => x.Key, x => x.Value),
                LastAccessTimes = new Dictionary<string, DateTime>(_lastAccess)
            };
            
            return report;
        }
    }
    
    public async Task LogUnusedConfigurationAsync(TSKConfig config)
    {
        var allKeys = config.GetAllKeys();
        var unusedKeys = allKeys.Where(key => !_accessCounts.ContainsKey(key)).ToList();
        
        if (unusedKeys.Any())
        {
            _logger.LogWarning("Unused configuration keys detected: {UnusedKeys}", 
                string.Join(", ", unusedKeys));
        }
    }
}

public class ConfigurationUsageReport
{
    public int TotalAccesses { get; set; }
    public Dictionary<string, int> MostAccessedKeys { get; set; } = new();
    public Dictionary<string, DateTime> LastAccessTimes { get; set; } = new();
}
```

## 🚀 Best Practices

### Configuration Organization

```csharp
// Recommended configuration structure
config/
├── base.tsk                 # Base configuration
├── development.tsk          # Development overrides
├── staging.tsk             # Staging overrides
├── production.tsk          # Production overrides
├── local.tsk               # Local development overrides (git ignored)
├── features/
│   ├── database.tsk        # Database-specific configuration
│   ├── api.tsk            # API-specific configuration
│   ├── cache.tsk          # Cache-specific configuration
│   └── security.tsk       # Security-specific configuration
└── secrets/
    └── encrypted.tsk       # Encrypted sensitive values
```

### Performance Optimization

```csharp
public class OptimizedConfigurationProvider
{
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());
    private readonly ILogger<OptimizedConfigurationProvider> _logger;
    
    public OptimizedConfigurationProvider(ILogger<OptimizedConfigurationProvider> logger)
    {
        _logger = logger;
    }
    
    public async Task<TSKConfig> LoadOptimizedConfigurationAsync()
    {
        var cacheKey = "configuration_cache";
        
        if (_cache.TryGetValue(cacheKey, out TSKConfig cachedConfig))
        {
            return cachedConfig;
        }
        
        var config = await LoadConfigurationAsync();
        
        // Cache for 5 minutes
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(5))
            .SetSlidingExpiration(TimeSpan.FromMinutes(2));
        
        _cache.Set(cacheKey, config, cacheOptions);
        
        return config;
    }
    
    private async Task<TSKConfig> LoadConfigurationAsync()
    {
        var config = new TSKConfig();
        
        // Load configuration files in parallel
        var tasks = new[]
        {
            config.LoadFileAsync("config/base.tsk"),
            config.LoadFileAsync("config/features/database.tsk"),
            config.LoadFileAsync("config/features/api.tsk"),
            config.LoadFileAsync("config/features/cache.tsk")
        };
        
        await Task.WhenAll(tasks);
        
        return config;
    }
}
```

### Error Handling

```csharp
public class ResilientConfigurationProvider
{
    private readonly ILogger<ResilientConfigurationProvider> _logger;
    private TSKConfig _fallbackConfig;
    
    public ResilientConfigurationProvider(ILogger<ResilientConfigurationProvider> logger)
    {
        _logger = logger;
        _fallbackConfig = CreateFallbackConfiguration();
    }
    
    public async Task<TSKConfig> LoadResilientConfigurationAsync()
    {
        try
        {
            var config = new TSKConfig();
            
            // Try to load configuration files
            await LoadConfigurationFilesAsync(config);
            
            // Validate configuration
            var validator = new ConfigurationValidator(_logger);
            var validationResult = await validator.ValidateConfigurationAsync(config);
            
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Configuration validation failed, using fallback: {Errors}", 
                    string.Join(", ", validationResult.Errors));
                return _fallbackConfig;
            }
            
            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load configuration, using fallback");
            return _fallbackConfig;
        }
    }
    
    private async Task LoadConfigurationFilesAsync(TSKConfig config)
    {
        var configFiles = new[]
        {
            "config/base.tsk",
            "config/features/database.tsk",
            "config/features/api.tsk"
        };
        
        foreach (var file in configFiles)
        {
            try
            {
                await config.LoadFileAsync(file);
            }
            catch (FileNotFoundException)
            {
                _logger.LogWarning("Configuration file not found: {File}", file);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load configuration file: {File}", file);
            }
        }
    }
    
    private TSKConfig CreateFallbackConfiguration()
    {
        var config = new TSKConfig();
        
        // Set safe default values
        config.Set("database.connection_string", "Server=localhost;Database=myapp;");
        config.Set("database.timeout", 30);
        config.Set("api.base_url", "https://api.example.com");
        config.Set("api.timeout", 30);
        config.Set("logging.level", "Warning");
        
        return config;
    }
}
```

## 📝 Summary

This guide covered advanced configuration management patterns for C# TuskLang applications:

- **Hierarchical Configuration**: Multi-level configuration with environment-specific overrides
- **Hot Reloading**: Real-time configuration updates with file system watchers
- **Configuration Validation**: Comprehensive validation with custom rules and schemas
- **Secure Configuration**: Environment variable integration and encrypted values
- **Configuration Analytics**: Usage tracking and performance monitoring
- **Best Practices**: Organization, optimization, and error handling strategies

These patterns ensure your C# TuskLang applications have robust, secure, and maintainable configuration management systems. 