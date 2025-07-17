# ⚙️ Configuration Management - TuskLang for C# - "Configure with Confidence"

**Master configuration patterns and management for your C# TuskLang applications!**

Configuration management is the foundation of flexible, maintainable applications. This guide covers configuration patterns, validation, hot reloading, and best practices for TuskLang in C# environments.

## 🎛️ Configuration Philosophy

### "We Don't Bow to Any King"
- **Configuration as code** - Version control your configs
- **Environment-specific** - Different configs for different environments
- **Validation first** - Validate configs at startup
- **Hot reloading** - Update configs without restarting
- **Security by design** - Secure sensitive configuration

## 🏗️ Configuration Patterns

### Example: Hierarchical Configuration
```csharp
// ConfigurationService.cs
public class ConfigurationService
{
    private readonly TuskLang _parser;
    private readonly ILogger<ConfigurationService> _logger;
    
    public ConfigurationService(ILogger<ConfigurationService> logger)
    {
        _parser = new TuskLang();
        _logger = logger;
    }
    
    public async Task<Dictionary<string, object>> LoadConfigurationAsync(string environment)
    {
        var config = new Dictionary<string, object>();
        
        // Load base configuration
        config = await LoadBaseConfigAsync();
        
        // Load environment-specific configuration
        var envConfig = await LoadEnvironmentConfigAsync(environment);
        MergeConfigurations(config, envConfig);
        
        // Validate configuration
        await ValidateConfigurationAsync(config);
        
        return config;
    }
    
    private async Task<Dictionary<string, object>> LoadBaseConfigAsync()
    {
        var baseConfigPath = "config/base.tsk";
        return _parser.ParseFile(baseConfigPath);
    }
    
    private async Task<Dictionary<string, object>> LoadEnvironmentConfigAsync(string environment)
    {
        var envConfigPath = $"config/{environment}.tsk";
        return _parser.ParseFile(envConfigPath);
    }
    
    private void MergeConfigurations(Dictionary<string, object> baseConfig, Dictionary<string, object> envConfig)
    {
        foreach (var kvp in envConfig)
        {
            baseConfig[kvp.Key] = kvp.Value;
        }
    }
    
    private async Task ValidateConfigurationAsync(Dictionary<string, object> config)
    {
        var validator = new ConfigurationValidator();
        var validationResult = await validator.ValidateAsync(config);
        
        if (!validationResult.IsValid)
        {
            _logger.LogError("Configuration validation failed: {Errors}", validationResult.Errors);
            throw new ConfigurationValidationException(validationResult.Errors);
        }
    }
}
```

## 🔄 Hot Reloading

### Example: Configuration Hot Reload
```csharp
// HotReloadService.cs
public class HotReloadService
{
    private readonly FileSystemWatcher _watcher;
    private readonly ConfigurationService _configService;
    private readonly ILogger<HotReloadService> _logger;
    
    public HotReloadService(ConfigurationService configService, ILogger<HotReloadService> logger)
    {
        _configService = configService;
        _logger = logger;
        
        _watcher = new FileSystemWatcher("config")
        {
            NotifyFilter = NotifyFilters.LastWrite,
            Filter = "*.tsk",
            EnableRaisingEvents = true
        };
        
        _watcher.Changed += OnConfigurationChanged;
    }
    
    private async void OnConfigurationChanged(object sender, FileSystemEventArgs e)
    {
        _logger.LogInformation("Configuration file changed: {FilePath}", e.FullPath);
        
        try
        {
            // Debounce rapid changes
            await Task.Delay(1000);
            
            // Reload configuration
            var newConfig = await _configService.LoadConfigurationAsync("production");
            
            // Notify subscribers
            OnConfigurationReloaded?.Invoke(newConfig);
            
            _logger.LogInformation("Configuration reloaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reload configuration");
        }
    }
    
    public event Action<Dictionary<string, object>>? OnConfigurationReloaded;
}
```

## ✅ Configuration Validation

### Example: Configuration Validator
```csharp
// ConfigurationValidator.cs
public class ConfigurationValidator
{
    public async Task<ValidationResult> ValidateAsync(Dictionary<string, object> config)
    {
        var result = new ValidationResult();
        
        // Validate required fields
        ValidateRequiredFields(config, result);
        
        // Validate data types
        ValidateDataTypes(config, result);
        
        // Validate business rules
        await ValidateBusinessRulesAsync(config, result);
        
        return result;
    }
    
    private void ValidateRequiredFields(Dictionary<string, object> config, ValidationResult result)
    {
        var requiredFields = new[] { "database_connection", "api_key", "environment" };
        
        foreach (var field in requiredFields)
        {
            if (!config.ContainsKey(field))
            {
                result.Errors.Add($"Required field '{field}' is missing");
            }
        }
    }
    
    private void ValidateDataTypes(Dictionary<string, object> config, ValidationResult result)
    {
        if (config.ContainsKey("port") && !int.TryParse(config["port"].ToString(), out _))
        {
            result.Errors.Add("Port must be a valid integer");
        }
        
        if (config.ContainsKey("timeout") && !int.TryParse(config["timeout"].ToString(), out _))
        {
            result.Errors.Add("Timeout must be a valid integer");
        }
    }
    
    private async Task ValidateBusinessRulesAsync(Dictionary<string, object> config, ValidationResult result)
    {
        // Validate database connection
        if (config.ContainsKey("database_connection"))
        {
            var connectionString = config["database_connection"].ToString();
            if (!await ValidateDatabaseConnectionAsync(connectionString))
            {
                result.Errors.Add("Invalid database connection string");
            }
        }
        
        // Validate API key format
        if (config.ContainsKey("api_key"))
        {
            var apiKey = config["api_key"].ToString();
            if (!IsValidApiKey(apiKey))
            {
                result.Errors.Add("Invalid API key format");
            }
        }
    }
    
    private async Task<bool> ValidateDatabaseConnectionAsync(string connectionString)
    {
        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    private bool IsValidApiKey(string apiKey)
    {
        return !string.IsNullOrEmpty(apiKey) && apiKey.Length >= 32;
    }
}

public class ValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<string> Errors { get; set; } = new List<string>();
}
```

## 🔐 Security Configuration

### Example: Secure Configuration Management
```csharp
// SecureConfigurationService.cs
public class SecureConfigurationService
{
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<SecureConfigurationService> _logger;
    
    public SecureConfigurationService(IEncryptionService encryptionService, ILogger<SecureConfigurationService> logger)
    {
        _encryptionService = encryptionService;
        _logger = logger;
    }
    
    public async Task<Dictionary<string, object>> LoadSecureConfigurationAsync(string filePath)
    {
        var config = TuskLang.ParseFile(filePath);
        
        // Decrypt sensitive values
        await DecryptSensitiveValuesAsync(config);
        
        return config;
    }
    
    private async Task DecryptSensitiveValuesAsync(Dictionary<string, object> config)
    {
        var sensitiveKeys = new[] { "api_key", "database_password", "jwt_secret" };
        
        foreach (var key in sensitiveKeys)
        {
            if (config.ContainsKey(key))
            {
                var encryptedValue = config[key].ToString();
                if (IsEncrypted(encryptedValue))
                {
                    var decryptedValue = await _encryptionService.DecryptAsync(encryptedValue);
                    config[key] = decryptedValue;
                }
            }
        }
    }
    
    private bool IsEncrypted(string value)
    {
        return value.StartsWith("ENC[") && value.EndsWith("]");
    }
}
```

## 🛠️ Real-World Configuration Scenarios
- **Multi-environment deployment**: Different configs for dev/staging/prod
- **Feature flags**: Enable/disable features via configuration
- **Database connections**: Environment-specific database configs
- **API integrations**: External service configurations

## 🧩 Best Practices
- Use hierarchical configuration
- Validate all configurations
- Implement hot reloading for development
- Secure sensitive configuration
- Version control your configs

## 🏁 You're Ready!

You can now:
- Implement robust configuration management
- Validate and secure configurations
- Use hot reloading for rapid development
- Manage multi-environment deployments

**Next:** [Database Integration](024-database-csharp.md)

---

**"We don't bow to any king" - Your configuration mastery, your deployment flexibility, your operational excellence.**

Configure with confidence. Deploy with precision. ⚙️ 