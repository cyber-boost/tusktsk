# Configuration Management in C# TuskLang

## Overview

Effective configuration management is essential for building flexible and maintainable applications. This guide covers configuration loading, validation, hot reloading, and configuration best practices for C# TuskLang applications.

## ⚙️ Configuration Loading

### Advanced Configuration Manager

```csharp
public class AdvancedConfigurationManager
{
    private readonly ILogger<AdvancedConfigurationManager> _logger;
    private readonly Dictionary<string, object> _configuration;
    private readonly Dictionary<string, ConfigurationValidator> _validators;
    private readonly List<IConfigurationWatcher> _watchers;
    private readonly object _lock = new();
    private readonly FileSystemWatcher? _fileWatcher;
    
    public AdvancedConfigurationManager(ILogger<AdvancedConfigurationManager> logger)
    {
        _logger = logger;
        _configuration = new Dictionary<string, object>();
        _validators = new Dictionary<string, ConfigurationValidator>();
        _watchers = new List<IConfigurationWatcher>();
        
        // Set up file watcher for hot reloading
        var configPath = Environment.GetEnvironmentVariable("TUSK_CONFIG_PATH") ?? "config.tsk";
        if (File.Exists(configPath))
        {
            _fileWatcher = new FileSystemWatcher(Path.GetDirectoryName(configPath)!, Path.GetFileName(configPath));
            _fileWatcher.Changed += OnConfigurationFileChanged;
            _fileWatcher.EnableRaisingEvents = true;
        }
    }
    
    public async Task LoadConfigurationAsync(string configPath)
    {
        try
        {
            _logger.LogInformation("Loading configuration from {ConfigPath}", configPath);
            
            if (!File.Exists(configPath))
            {
                throw new FileNotFoundException($"Configuration file not found: {configPath}");
            }
            
            var configContent = await File.ReadAllTextAsync(configPath);
            var newConfiguration = ParseConfiguration(configContent);
            
            lock (_lock)
            {
                _configuration.Clear();
                foreach (var kvp in newConfiguration)
                {
                    _configuration[kvp.Key] = kvp.Value;
                }
            }
            
            // Validate configuration
            await ValidateConfigurationAsync();
            
            // Notify watchers
            await NotifyConfigurationChangedAsync();
            
            _logger.LogInformation("Configuration loaded successfully with {Count} settings", _configuration.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load configuration from {ConfigPath}", configPath);
            throw;
        }
    }
    
    public async Task LoadConfigurationFromEnvironmentAsync()
    {
        try
        {
            _logger.LogInformation("Loading configuration from environment variables");
            
            var environmentConfig = new Dictionary<string, object>();
            
            foreach (var envVar in Environment.GetEnvironmentVariables().Cast<DictionaryEntry>())
            {
                var key = envVar.Key.ToString()!;
                var value = envVar.Value?.ToString() ?? string.Empty;
                
                if (key.StartsWith("TUSK_"))
                {
                    var configKey = key.Substring(5).ToLower().Replace('_', '.');
                    environmentConfig[configKey] = value;
                }
            }
            
            lock (_lock)
            {
                foreach (var kvp in environmentConfig)
                {
                    _configuration[kvp.Key] = kvp.Value;
                }
            }
            
            await ValidateConfigurationAsync();
            await NotifyConfigurationChangedAsync();
            
            _logger.LogInformation("Environment configuration loaded with {Count} settings", environmentConfig.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load configuration from environment");
            throw;
        }
    }
    
    public async Task LoadConfigurationFromDatabaseAsync(string connectionString, string tableName = "configuration")
    {
        try
        {
            _logger.LogInformation("Loading configuration from database table {TableName}", tableName);
            
            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();
            
            var query = $"SELECT config_key, config_value, config_type FROM {tableName} WHERE is_active = 1";
            var results = await connection.QueryAsync<ConfigurationRecord>(query);
            
            var databaseConfig = new Dictionary<string, object>();
            
            foreach (var record in results)
            {
                var value = ConvertValue(record.ConfigValue, record.ConfigType);
                databaseConfig[record.ConfigKey] = value;
            }
            
            lock (_lock)
            {
                foreach (var kvp in databaseConfig)
                {
                    _configuration[kvp.Key] = kvp.Value;
                }
            }
            
            await ValidateConfigurationAsync();
            await NotifyConfigurationChangedAsync();
            
            _logger.LogInformation("Database configuration loaded with {Count} settings", databaseConfig.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load configuration from database");
            throw;
        }
    }
    
    private Dictionary<string, object> ParseConfiguration(string content)
    {
        var configuration = new Dictionary<string, object>();
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            
            // Skip comments and empty lines
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith('#'))
            {
                continue;
            }
            
            var equalIndex = trimmedLine.IndexOf('=');
            if (equalIndex > 0)
            {
                var key = trimmedLine.Substring(0, equalIndex).Trim();
                var value = trimmedLine.Substring(equalIndex + 1).Trim();
                
                // Remove quotes if present
                if ((value.StartsWith('"') && value.EndsWith('"')) ||
                    (value.StartsWith('\'') && value.EndsWith('\'')))
                {
                    value = value.Substring(1, value.Length - 2);
                }
                
                configuration[key] = value;
            }
        }
        
        return configuration;
    }
    
    private object ConvertValue(string value, string type)
    {
        return type.ToLower() switch
        {
            "int" => int.Parse(value),
            "long" => long.Parse(value),
            "double" => double.Parse(value),
            "bool" => bool.Parse(value),
            "datetime" => DateTime.Parse(value),
            "guid" => Guid.Parse(value),
            _ => value
        };
    }
    
    public T Get<T>(string key, T defaultValue = default!)
    {
        lock (_lock)
        {
            if (_configuration.TryGetValue(key, out var value))
            {
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
    }
    
    public bool Has(string key)
    {
        lock (_lock)
        {
            return _configuration.ContainsKey(key);
        }
    }
    
    public void Set<T>(string key, T value)
    {
        lock (_lock)
        {
            _configuration[key] = value!;
        }
        
        _logger.LogDebug("Configuration updated: {Key} = {Value}", key, value);
    }
    
    public Dictionary<string, object> GetAll()
    {
        lock (_lock)
        {
            return new Dictionary<string, object>(_configuration);
        }
    }
    
    public void RegisterValidator(string key, ConfigurationValidator validator)
    {
        _validators[key] = validator;
        _logger.LogDebug("Registered validator for configuration key: {Key}", key);
    }
    
    public void RegisterWatcher(IConfigurationWatcher watcher)
    {
        _watchers.Add(watcher);
        _logger.LogDebug("Registered configuration watcher: {WatcherType}", watcher.GetType().Name);
    }
    
    private async Task ValidateConfigurationAsync()
    {
        foreach (var kvp in _validators)
        {
            var key = kvp.Key;
            var validator = kvp.Value;
            
            if (_configuration.TryGetValue(key, out var value))
            {
                var validationResult = await validator.ValidateAsync(key, value);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("Configuration validation failed for {Key}: {Error}", 
                        key, validationResult.ErrorMessage);
                }
            }
        }
    }
    
    private async Task NotifyConfigurationChangedAsync()
    {
        foreach (var watcher in _watchers)
        {
            try
            {
                await watcher.OnConfigurationChangedAsync(_configuration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Configuration watcher {WatcherType} failed", watcher.GetType().Name);
            }
        }
    }
    
    private async void OnConfigurationFileChanged(object sender, FileSystemEventArgs e)
    {
        try
        {
            _logger.LogInformation("Configuration file changed: {FilePath}", e.FullPath);
            
            // Wait a bit to ensure file is fully written
            await Task.Delay(100);
            
            await LoadConfigurationAsync(e.FullPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reload configuration from changed file");
        }
    }
    
    public void Dispose()
    {
        _fileWatcher?.Dispose();
    }
}

public class ConfigurationRecord
{
    public string ConfigKey { get; set; } = string.Empty;
    public string ConfigValue { get; set; } = string.Empty;
    public string ConfigType { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public abstract class ConfigurationValidator
{
    public abstract Task<ValidationResult> ValidateAsync(string key, object value);
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    
    public static ValidationResult Success()
    {
        return new ValidationResult { IsValid = true };
    }
    
    public static ValidationResult Failure(string errorMessage)
    {
        return new ValidationResult { IsValid = false, ErrorMessage = errorMessage };
    }
}

public interface IConfigurationWatcher
{
    Task OnConfigurationChangedAsync(Dictionary<string, object> configuration);
}
```

### Configuration Validators

```csharp
public class RequiredConfigurationValidator : ConfigurationValidator
{
    public override Task<ValidationResult> ValidateAsync(string key, object value)
    {
        if (value == null || (value is string str && string.IsNullOrEmpty(str)))
        {
            return Task.FromResult(ValidationResult.Failure($"Configuration key '{key}' is required"));
        }
        
        return Task.FromResult(ValidationResult.Success());
    }
}

public class StringLengthValidator : ConfigurationValidator
{
    private readonly int _minLength;
    private readonly int _maxLength;
    
    public StringLengthValidator(int minLength = 0, int maxLength = int.MaxValue)
    {
        _minLength = minLength;
        _maxLength = maxLength;
    }
    
    public override Task<ValidationResult> ValidateAsync(string key, object value)
    {
        if (value is string str)
        {
            if (str.Length < _minLength)
            {
                return Task.FromResult(ValidationResult.Failure(
                    $"Configuration key '{key}' must be at least {_minLength} characters long"));
            }
            
            if (str.Length > _maxLength)
            {
                return Task.FromResult(ValidationResult.Failure(
                    $"Configuration key '{key}' must be no more than {_maxLength} characters long"));
            }
        }
        
        return Task.FromResult(ValidationResult.Success());
    }
}

public class NumericRangeValidator : ConfigurationValidator
{
    private readonly double _minValue;
    private readonly double _maxValue;
    
    public NumericRangeValidator(double minValue = double.MinValue, double maxValue = double.MaxValue)
    {
        _minValue = minValue;
        _maxValue = maxValue;
    }
    
    public override Task<ValidationResult> ValidateAsync(string key, object value)
    {
        if (value is IConvertible convertible)
        {
            try
            {
                var numericValue = Convert.ToDouble(convertible);
                
                if (numericValue < _minValue)
                {
                    return Task.FromResult(ValidationResult.Failure(
                        $"Configuration key '{key}' must be at least {_minValue}"));
                }
                
                if (numericValue > _maxValue)
                {
                    return Task.FromResult(ValidationResult.Failure(
                        $"Configuration key '{key}' must be no more than {_maxValue}"));
                }
            }
            catch
            {
                return Task.FromResult(ValidationResult.Failure(
                    $"Configuration key '{key}' must be a numeric value"));
            }
        }
        
        return Task.FromResult(ValidationResult.Success());
    }
}

public class RegexValidator : ConfigurationValidator
{
    private readonly string _pattern;
    private readonly string _description;
    
    public RegexValidator(string pattern, string description = "")
    {
        _pattern = pattern;
        _description = description;
    }
    
    public override Task<ValidationResult> ValidateAsync(string key, object value)
    {
        if (value is string str)
        {
            if (!Regex.IsMatch(str, _pattern))
            {
                var errorMessage = string.IsNullOrEmpty(_description)
                    ? $"Configuration key '{key}' does not match required pattern"
                    : $"Configuration key '{key}' {_description}";
                
                return Task.FromResult(ValidationResult.Failure(errorMessage));
            }
        }
        
        return Task.FromResult(ValidationResult.Success());
    }
}

public class UrlValidator : ConfigurationValidator
{
    public override Task<ValidationResult> ValidateAsync(string key, object value)
    {
        if (value is string str)
        {
            if (!Uri.TryCreate(str, UriKind.Absolute, out _))
            {
                return Task.FromResult(ValidationResult.Failure(
                    $"Configuration key '{key}' must be a valid URL"));
            }
        }
        
        return Task.FromResult(ValidationResult.Success());
    }
}

public class EmailValidator : ConfigurationValidator
{
    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    
    public override Task<ValidationResult> ValidateAsync(string key, object value)
    {
        if (value is string str)
        {
            if (!EmailRegex.IsMatch(str))
            {
                return Task.FromResult(ValidationResult.Failure(
                    $"Configuration key '{key}' must be a valid email address"));
            }
        }
        
        return Task.FromResult(ValidationResult.Success());
    }
}
```

## 🔄 Hot Reloading

### Configuration Hot Reloader

```csharp
public class ConfigurationHotReloader
{
    private readonly ILogger<ConfigurationHotReloader> _logger;
    private readonly AdvancedConfigurationManager _configManager;
    private readonly List<IConfigurationReloadHandler> _reloadHandlers;
    private readonly Dictionary<string, DateTime> _lastReloadTimes;
    private readonly object _lock = new();
    
    public ConfigurationHotReloader(
        ILogger<ConfigurationHotReloader> logger,
        AdvancedConfigurationManager configManager)
    {
        _logger = logger;
        _configManager = configManager;
        _reloadHandlers = new List<IConfigurationReloadHandler>();
        _lastReloadTimes = new Dictionary<string, DateTime>();
    }
    
    public void RegisterReloadHandler(IConfigurationReloadHandler handler)
    {
        _reloadHandlers.Add(handler);
        _logger.LogDebug("Registered configuration reload handler: {HandlerType}", handler.GetType().Name);
    }
    
    public async Task ReloadConfigurationAsync(string source = "file")
    {
        try
        {
            _logger.LogInformation("Starting configuration reload from {Source}", source);
            
            var reloadStartTime = DateTime.UtcNow;
            
            // Store old configuration for comparison
            var oldConfiguration = _configManager.GetAll();
            
            // Reload configuration
            switch (source.ToLower())
            {
                case "file":
                    var configPath = Environment.GetEnvironmentVariable("TUSK_CONFIG_PATH") ?? "config.tsk";
                    await _configManager.LoadConfigurationAsync(configPath);
                    break;
                    
                case "environment":
                    await _configManager.LoadConfigurationFromEnvironmentAsync();
                    break;
                    
                case "database":
                    var connectionString = Environment.GetEnvironmentVariable("TUSK_DB_CONNECTION");
                    if (!string.IsNullOrEmpty(connectionString))
                    {
                        await _configManager.LoadConfigurationFromDatabaseAsync(connectionString);
                    }
                    break;
                    
                default:
                    throw new ArgumentException($"Unknown configuration source: {source}");
            }
            
            var newConfiguration = _configManager.GetAll();
            
            // Find changed keys
            var changedKeys = FindChangedKeys(oldConfiguration, newConfiguration);
            
            if (changedKeys.Any())
            {
                _logger.LogInformation("Configuration reloaded with {Count} changed keys: {Keys}", 
                    changedKeys.Count, string.Join(", ", changedKeys));
                
                // Notify reload handlers
                await NotifyReloadHandlersAsync(changedKeys, oldConfiguration, newConfiguration);
            }
            else
            {
                _logger.LogInformation("Configuration reloaded with no changes");
            }
            
            // Update last reload time
            lock (_lock)
            {
                _lastReloadTimes[source] = reloadStartTime;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Configuration reload failed from {Source}", source);
            throw;
        }
    }
    
    private List<string> FindChangedKeys(Dictionary<string, object> oldConfig, Dictionary<string, object> newConfig)
    {
        var changedKeys = new List<string>();
        
        // Check for modified or new keys
        foreach (var kvp in newConfig)
        {
            if (!oldConfig.TryGetValue(kvp.Key, out var oldValue) || !Equals(oldValue, kvp.Value))
            {
                changedKeys.Add(kvp.Key);
            }
        }
        
        // Check for removed keys
        foreach (var key in oldConfig.Keys)
        {
            if (!newConfig.ContainsKey(key))
            {
                changedKeys.Add(key);
            }
        }
        
        return changedKeys;
    }
    
    private async Task NotifyReloadHandlersAsync(
        List<string> changedKeys,
        Dictionary<string, object> oldConfiguration,
        Dictionary<string, object> newConfiguration)
    {
        foreach (var handler in _reloadHandlers)
        {
            try
            {
                await handler.OnConfigurationReloadedAsync(changedKeys, oldConfiguration, newConfiguration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Configuration reload handler {HandlerType} failed", handler.GetType().Name);
            }
        }
    }
    
    public DateTime GetLastReloadTime(string source)
    {
        lock (_lock)
        {
            return _lastReloadTimes.GetValueOrDefault(source, DateTime.MinValue);
        }
    }
    
    public List<string> GetRegisteredHandlers()
    {
        return _reloadHandlers.Select(h => h.GetType().Name).ToList();
    }
}

public interface IConfigurationReloadHandler
{
    Task OnConfigurationReloadedAsync(
        List<string> changedKeys,
        Dictionary<string, object> oldConfiguration,
        Dictionary<string, object> newConfiguration);
}

public class DatabaseConnectionReloadHandler : IConfigurationReloadHandler
{
    private readonly ILogger<DatabaseConnectionReloadHandler> _logger;
    private readonly IDbConnectionFactory _connectionFactory;
    
    public DatabaseConnectionReloadHandler(
        ILogger<DatabaseConnectionReloadHandler> logger,
        IDbConnectionFactory connectionFactory)
    {
        _logger = logger;
        _connectionFactory = connectionFactory;
    }
    
    public async Task OnConfigurationReloadedAsync(
        List<string> changedKeys,
        Dictionary<string, object> oldConfiguration,
        Dictionary<string, object> newConfiguration)
    {
        var databaseKeys = changedKeys.Where(k => k.StartsWith("database.")).ToList();
        
        if (databaseKeys.Any())
        {
            _logger.LogInformation("Database configuration changed, updating connection factory");
            
            try
            {
                await _connectionFactory.UpdateConfigurationAsync(newConfiguration);
                _logger.LogInformation("Database connection factory updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update database connection factory");
            }
        }
    }
}

public class CacheConfigurationReloadHandler : IConfigurationReloadHandler
{
    private readonly ILogger<CacheConfigurationReloadHandler> _logger;
    private readonly ICacheManager _cacheManager;
    
    public CacheConfigurationReloadHandler(
        ILogger<CacheConfigurationReloadHandler> logger,
        ICacheManager cacheManager)
    {
        _logger = logger;
        _cacheManager = cacheManager;
    }
    
    public async Task OnConfigurationReloadedAsync(
        List<string> changedKeys,
        Dictionary<string, object> oldConfiguration,
        Dictionary<string, object> newConfiguration)
    {
        var cacheKeys = changedKeys.Where(k => k.StartsWith("cache.")).ToList();
        
        if (cacheKeys.Any())
        {
            _logger.LogInformation("Cache configuration changed, updating cache manager");
            
            try
            {
                await _cacheManager.UpdateConfigurationAsync(newConfiguration);
                _logger.LogInformation("Cache manager updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update cache manager");
            }
        }
    }
}
```

## 📝 Summary

This guide covered comprehensive configuration management strategies for C# TuskLang applications:

- **Configuration Loading**: Advanced configuration manager with multiple sources
- **Configuration Validators**: Built-in validators for common validation scenarios
- **Hot Reloading**: Configuration hot reloader with change detection and handlers
- **Configuration Best Practices**: Proper validation, reloading, and management patterns

These configuration management strategies ensure your C# TuskLang applications have flexible, validated, and dynamically updatable configurations. 