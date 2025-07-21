using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Text.Json;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace TuskLang
{
    /// <summary>
    /// Advanced configuration management system for TuskLang C# SDK
    /// Provides hierarchical configuration, hot reloading, validation, and environment-specific settings
    /// </summary>
    public class ConfigurationManagement
    {
        private readonly Dictionary<string, object> _configuration;
        private readonly List<IConfigurationProvider> _providers;
        private readonly List<IConfigurationValidator> _validators;
        private readonly List<IConfigurationWatcher> _watchers;
        private readonly ConfigurationMetrics _metrics;
        private readonly object _lock = new object();
        private readonly Timer _reloadTimer;

        public ConfigurationManagement()
        {
            _configuration = new Dictionary<string, object>();
            _providers = new List<IConfigurationProvider>();
            _validators = new List<IConfigurationValidator>();
            _watchers = new List<IConfigurationWatcher>();
            _metrics = new ConfigurationMetrics();

            // Register default providers
            RegisterProvider(new EnvironmentVariableProvider());
            RegisterProvider(new JsonFileProvider());
            RegisterProvider(new TskFileProvider());
            
            // Register default validators
            RegisterValidator(new SchemaValidator());
            RegisterValidator(new TypeValidator());
            
            // Register default watchers
            RegisterWatcher(new FileSystemWatcher());
            
            // Reload configuration every 30 seconds
            _reloadTimer = new Timer(ReloadConfiguration, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        }

        /// <summary>
        /// Get configuration value with type conversion
        /// </summary>
        public T GetValue<T>(string key, T defaultValue = default(T))
        {
            lock (_lock)
            {
                if (_configuration.TryGetValue(key, out var value))
                {
                    try
                    {
                        if (value is T typedValue)
                        {
                            return typedValue;
                        }
                        
                        // Try to convert the value
                        if (typeof(T) == typeof(string))
                        {
                            return (T)(object)value.ToString();
                        }
                        else if (typeof(T) == typeof(int))
                        {
                            return (T)(object)Convert.ToInt32(value);
                        }
                        else if (typeof(T) == typeof(double))
                        {
                            return (T)(object)Convert.ToDouble(value);
                        }
                        else if (typeof(T) == typeof(bool))
                        {
                            return (T)(object)Convert.ToBoolean(value);
                        }
                        else if (typeof(T) == typeof(DateTime))
                        {
                            return (T)(object)Convert.ToDateTime(value);
                        }
                    }
                    catch
                    {
                        // Conversion failed, return default
                    }
                }
                
                return defaultValue;
            }
        }

        /// <summary>
        /// Get configuration section
        /// </summary>
        public Dictionary<string, object> GetSection(string sectionName)
        {
            lock (_lock)
            {
                var section = new Dictionary<string, object>();
                var prefix = $"{sectionName}:";
                
                foreach (var kvp in _configuration)
                {
                    if (kvp.Key.StartsWith(prefix))
                    {
                        var key = kvp.Key.Substring(prefix.Length);
                        section[key] = kvp.Value;
                    }
                }
                
                return section;
            }
        }

        /// <summary>
        /// Set configuration value
        /// </summary>
        public void SetValue<T>(string key, T value)
        {
            lock (_lock)
            {
                _configuration[key] = value;
                _metrics.RecordSet(key);
            }
        }

        /// <summary>
        /// Load configuration from all providers
        /// </summary>
        public async Task LoadConfigurationAsync()
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                var newConfig = new Dictionary<string, object>();
                
                // Load from all providers
                foreach (var provider in _providers)
                {
                    try
                    {
                        var providerConfig = await provider.LoadAsync();
                        foreach (var kvp in providerConfig)
                        {
                            newConfig[kvp.Key] = kvp.Value;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log provider failure but continue
                        Console.WriteLine($"Configuration provider {provider.GetType().Name} failed: {ex.Message}");
                    }
                }
                
                // Validate configuration
                var validationResult = await ValidateConfiguration(newConfig);
                if (!validationResult.IsValid)
                {
                    throw new ConfigurationException($"Configuration validation failed: {string.Join(", ", validationResult.Errors)}");
                }
                
                // Update configuration
                lock (_lock)
                {
                    _configuration.Clear();
                    foreach (var kvp in newConfig)
                    {
                        _configuration[kvp.Key] = kvp.Value;
                    }
                }
                
                // Notify watchers
                await NotifyConfigurationChanged();
                
                _metrics.RecordLoad(DateTime.UtcNow - startTime, true);
            }
            catch (Exception ex)
            {
                _metrics.RecordLoad(DateTime.UtcNow - startTime, false);
                throw new ConfigurationException($"Failed to load configuration: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Register a configuration provider
        /// </summary>
        public void RegisterProvider(IConfigurationProvider provider)
        {
            lock (_lock)
            {
                _providers.Add(provider);
            }
        }

        /// <summary>
        /// Register a configuration validator
        /// </summary>
        public void RegisterValidator(IConfigurationValidator validator)
        {
            lock (_lock)
            {
                _validators.Add(validator);
            }
        }

        /// <summary>
        /// Register a configuration watcher
        /// </summary>
        public void RegisterWatcher(IConfigurationWatcher watcher)
        {
            lock (_lock)
            {
                _watchers.Add(watcher);
            }
        }

        /// <summary>
        /// Get configuration metrics
        /// </summary>
        public ConfigurationMetrics GetMetrics()
        {
            return _metrics;
        }

        /// <summary>
        /// Export configuration to JSON
        /// </summary>
        public string ExportToJson()
        {
            lock (_lock)
            {
                return JsonSerializer.Serialize(_configuration, new JsonSerializerOptions { WriteIndented = true });
            }
        }

        /// <summary>
        /// Import configuration from JSON
        /// </summary>
        public async Task ImportFromJsonAsync(string json)
        {
            try
            {
                var config = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                await LoadConfigurationFromDictionary(config);
            }
            catch (Exception ex)
            {
                throw new ConfigurationException($"Failed to import JSON configuration: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get all configuration keys
        /// </summary>
        public List<string> GetAllKeys()
        {
            lock (_lock)
            {
                return _configuration.Keys.ToList();
            }
        }

        /// <summary>
        /// Check if configuration key exists
        /// </summary>
        public bool HasKey(string key)
        {
            lock (_lock)
            {
                return _configuration.ContainsKey(key);
            }
        }

        private async Task<ValidationResult> ValidateConfiguration(Dictionary<string, object> config)
        {
            var errors = new List<string>();
            
            foreach (var validator in _validators)
            {
                try
                {
                    var result = await validator.ValidateAsync(config);
                    if (!result.IsValid)
                    {
                        errors.AddRange(result.Errors);
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Validator {validator.GetType().Name} failed: {ex.Message}");
                }
            }
            
            return new ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors
            };
        }

        private async Task LoadConfigurationFromDictionary(Dictionary<string, object> config)
        {
            var validationResult = await ValidateConfiguration(config);
            if (!validationResult.IsValid)
            {
                throw new ConfigurationException($"Configuration validation failed: {string.Join(", ", validationResult.Errors)}");
            }
            
            lock (_lock)
            {
                _configuration.Clear();
                foreach (var kvp in config)
                {
                    _configuration[kvp.Key] = kvp.Value;
                }
            }
            
            await NotifyConfigurationChanged();
        }

        private async Task NotifyConfigurationChanged()
        {
            foreach (var watcher in _watchers)
            {
                try
                {
                    await watcher.OnConfigurationChangedAsync(_configuration);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Configuration watcher {watcher.GetType().Name} failed: {ex.Message}");
                }
            }
        }

        private void ReloadConfiguration(object state)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await LoadConfigurationAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Configuration reload failed: {ex.Message}");
                }
            });
        }
    }

    /// <summary>
    /// Configuration provider interface
    /// </summary>
    public interface IConfigurationProvider
    {
        Task<Dictionary<string, object>> LoadAsync();
    }

    /// <summary>
    /// Environment variable configuration provider
    /// </summary>
    public class EnvironmentVariableProvider : IConfigurationProvider
    {
        public async Task<Dictionary<string, object>> LoadAsync()
        {
            var config = new Dictionary<string, object>();
            
            foreach (var envVar in Environment.GetEnvironmentVariables().Cast<System.Collections.DictionaryEntry>())
            {
                var key = envVar.Key.ToString();
                var value = envVar.Value.ToString();
                
                // Only include TuskLang-related environment variables
                if (key.StartsWith("TUSKLANG_") || key.StartsWith("TSK_"))
                {
                    config[key] = value;
                }
            }
            
            return await Task.FromResult(config);
        }
    }

    /// <summary>
    /// JSON file configuration provider
    /// </summary>
    public class JsonFileProvider : IConfigurationProvider
    {
        private readonly string _filePath;

        public JsonFileProvider(string filePath = "config.json")
        {
            _filePath = filePath;
        }

        public async Task<Dictionary<string, object>> LoadAsync()
        {
            if (!File.Exists(_filePath))
            {
                return new Dictionary<string, object>();
            }
            
            var json = await File.ReadAllTextAsync(_filePath);
            var config = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            
            return config ?? new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// TSK file configuration provider
    /// </summary>
    public class TskFileProvider : IConfigurationProvider
    {
        private readonly string _filePath;

        public TskFileProvider(string filePath = "config.tsk")
        {
            _filePath = filePath;
        }

        public async Task<Dictionary<string, object>> LoadAsync()
        {
            if (!File.Exists(_filePath))
            {
                return new Dictionary<string, object>();
            }
            
            var content = await File.ReadAllTextAsync(_filePath);
            var tsk = TSK.FromString(content);
            
            return tsk.ToDictionary();
        }
    }

    /// <summary>
    /// Configuration validator interface
    /// </summary>
    public interface IConfigurationValidator
    {
        Task<ValidationResult> ValidateAsync(Dictionary<string, object> configuration);
    }

    /// <summary>
    /// Schema-based configuration validator
    /// </summary>
    public class SchemaValidator : IConfigurationValidator
    {
        private readonly Dictionary<string, ConfigurationSchema> _schemas;

        public SchemaValidator()
        {
            _schemas = new Dictionary<string, ConfigurationSchema>();
            
            // Define default schemas
            _schemas["database"] = new ConfigurationSchema
            {
                RequiredFields = new[] { "connection_string", "provider" },
                FieldTypes = new Dictionary<string, Type>
                {
                    ["connection_string"] = typeof(string),
                    ["provider"] = typeof(string),
                    ["timeout"] = typeof(int)
                }
            };
            
            _schemas["logging"] = new ConfigurationSchema
            {
                RequiredFields = new[] { "level" },
                FieldTypes = new Dictionary<string, Type>
                {
                    ["level"] = typeof(string),
                    ["file_path"] = typeof(string),
                    ["max_file_size"] = typeof(int)
                }
            };
        }

        public async Task<ValidationResult> ValidateAsync(Dictionary<string, object> configuration)
        {
            var errors = new List<string>();
            
            foreach (var schema in _schemas)
            {
                var sectionName = schema.Key;
                var schemaDef = schema.Value;
                
                // Check required fields
                foreach (var requiredField in schemaDef.RequiredFields)
                {
                    var key = $"{sectionName}:{requiredField}";
                    if (!configuration.ContainsKey(key))
                    {
                        errors.Add($"Required configuration field '{key}' is missing");
                    }
                }
                
                // Check field types
                foreach (var fieldType in schemaDef.FieldTypes)
                {
                    var key = $"{sectionName}:{fieldType.Key}";
                    if (configuration.TryGetValue(key, out var value))
                    {
                        if (value != null && value.GetType() != fieldType.Value)
                        {
                            errors.Add($"Configuration field '{key}' has wrong type. Expected {fieldType.Value.Name}, got {value.GetType().Name}");
                        }
                    }
                }
            }
            
            return await Task.FromResult(new ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors
            });
        }
    }

    /// <summary>
    /// Type-based configuration validator
    /// </summary>
    public class TypeValidator : IConfigurationValidator
    {
        public async Task<ValidationResult> ValidateAsync(Dictionary<string, object> configuration)
        {
            var errors = new List<string>();
            
            foreach (var kvp in configuration)
            {
                var value = kvp.Value;
                if (value != null)
                {
                    // Check for common type issues
                    if (value is string stringValue)
                    {
                        if (stringValue.Length > 10000)
                        {
                            errors.Add($"Configuration value for '{kvp.Key}' is too long (max 10000 characters)");
                        }
                    }
                    else if (value is int intValue)
                    {
                        if (intValue < 0 && kvp.Key.Contains("timeout"))
                        {
                            errors.Add($"Configuration value for '{kvp.Key}' cannot be negative");
                        }
                    }
                }
            }
            
            return await Task.FromResult(new ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors
            });
        }
    }

    /// <summary>
    /// Configuration watcher interface
    /// </summary>
    public interface IConfigurationWatcher
    {
        Task OnConfigurationChangedAsync(Dictionary<string, object> configuration);
    }

    /// <summary>
    /// File system configuration watcher
    /// </summary>
    public class FileSystemWatcher : IConfigurationWatcher
    {
        public async Task OnConfigurationChangedAsync(Dictionary<string, object> configuration)
        {
            // In a real implementation, this would watch for file changes
            // and trigger configuration reload
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// Configuration schema definition
    /// </summary>
    public class ConfigurationSchema
    {
        public string[] RequiredFields { get; set; } = new string[0];
        public Dictionary<string, Type> FieldTypes { get; set; } = new Dictionary<string, Type>();
        public Dictionary<string, object> DefaultValues { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Configuration validation result
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }

    /// <summary>
    /// Configuration metrics collection
    /// </summary>
    public class ConfigurationMetrics
    {
        private readonly Dictionary<string, int> _setCounts = new Dictionary<string, int>();
        private readonly List<TimeSpan> _loadTimes = new List<TimeSpan>();
        private readonly object _lock = new object();

        public void RecordSet(string key)
        {
            lock (_lock)
            {
                _setCounts[key] = _setCounts.GetValueOrDefault(key, 0) + 1;
            }
        }

        public void RecordLoad(TimeSpan loadTime, bool success)
        {
            lock (_lock)
            {
                _loadTimes.Add(loadTime);
                if (_loadTimes.Count > 100)
                {
                    _loadTimes.RemoveAt(0);
                }
            }
        }

        public Dictionary<string, int> GetSetCounts()
        {
            lock (_lock)
            {
                return new Dictionary<string, int>(_setCounts);
            }
        }

        public double AverageLoadTime => _loadTimes.Count > 0 ? _loadTimes.Average(t => t.TotalMilliseconds) : 0;
        public int TotalLoads => _loadTimes.Count;
    }

    public class ConfigurationException : Exception
    {
        public ConfigurationException(string message) : base(message) { }
        public ConfigurationException(string message, Exception innerException) : base(message, innerException) { }
    }
} 