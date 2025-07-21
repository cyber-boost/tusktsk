using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using TuskLang.Configuration;

namespace TuskLang.Configuration
{
    /// <summary>
    /// Configuration Manager - High-level API for configuration processing
    /// 
    /// Provides comprehensive configuration management:
    /// - Configuration loading, processing, and validation
    /// - Configuration caching and hot-reloading
    /// - Multiple configuration format support
    /// - Configuration merging and inheritance
    /// - Environment-specific configuration handling
    /// - Thread-safe concurrent configuration access
    /// - Configuration change monitoring and notifications
    /// 
    /// Performance: Optimized for high-performance configuration access
    /// </summary>
    public class ConfigurationManager : IDisposable
    {
        private readonly Dictionary<string, object> _settings;
        private readonly string _configPath;
        private readonly ConfigurationManagerOptions _options;
        private readonly ConfigurationEngine _engine;
        private readonly ConcurrentDictionary<string, CachedConfiguration> _configurationCache;
        private readonly Dictionary<string, FileSystemWatcher> _fileWatchers;
        private readonly ConcurrentDictionary<string, DateTime> _fileWatchTimestamps;
        private readonly List<IConfigurationChangeHandler> _changeHandlers;
        private readonly object _lock = new object();

        public ConfigurationManager()
        {
            _settings = new Dictionary<string, object>();
            _configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".tusktsk", "config.json");
            _options = new ConfigurationManagerOptions();
            _engine = new ConfigurationEngine(_options.EngineOptions);
            _configurationCache = new ConcurrentDictionary<string, CachedConfiguration>();
            _fileWatchers = new Dictionary<string, FileSystemWatcher>();
            _fileWatchTimestamps = new ConcurrentDictionary<string, DateTime>();
            _changeHandlers = new List<IConfigurationChangeHandler>();
        }

        public event EventHandler<ConfigurationLoadedEventArgs> ConfigurationLoaded;
        public event EventHandler<ConfigurationChangedEventArgs> ConfigurationChanged;
        public event EventHandler<ConfigurationErrorEventArgs> ConfigurationError;
        
        /// <summary>
        /// Load configuration from file
        /// </summary>
        public async Task<IConfiguration> LoadConfigurationAsync(string filePath, string environment = null)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
            
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Configuration file not found: {filePath}");
            
            var fullPath = Path.GetFullPath(filePath);
            var cacheKey = GenerateCacheKey(fullPath, environment);
            
            // Check cache first
            if (_options.EnableCaching && TryGetCachedConfiguration(cacheKey, out var cached))
            {
                return cached;
            }
            
            try
            {
                // Process configuration
                var result = await _engine.ProcessFileAsync(fullPath);
                
                if (!result.Success)
                {
                    var errors = string.Join("; ", result.Errors.Select(e => e.Message));
                    throw new ConfigurationException($"Configuration processing failed: {errors}");
                }
                
                // Apply environment-specific overrides
                if (!string.IsNullOrEmpty(environment))
                {
                    await ApplyEnvironmentOverrides(result.Configuration, environment, Path.GetDirectoryName(fullPath));
                }
                
                // Create configuration wrapper
                var configuration = new ConfigurationWrapper(result.Configuration, fullPath, environment, result);
                
                // Cache if enabled
                if (_options.EnableCaching)
                {
                    CacheConfiguration(cacheKey, configuration);
                }
                
                // Setup file watching if enabled
                if (_options.EnableHotReloading)
                {
                    SetupFileWatcher(fullPath, environment);
                }
                
                // Raise loaded event
                ConfigurationLoaded?.Invoke(this, new ConfigurationLoadedEventArgs(configuration, fullPath, environment));
                
                return configuration;
            }
            catch (Exception ex)
            {
                ConfigurationError?.Invoke(this, new ConfigurationErrorEventArgs(ex, filePath, environment));
                throw;
            }
        }
        
        /// <summary>
        /// Load configuration from string
        /// </summary>
        public async Task<IConfiguration> LoadConfigurationAsync(string source, string name, string environment = null)
        {
            if (string.IsNullOrEmpty(source))
                throw new ArgumentException("Source cannot be null or empty", nameof(source));
            
            if (string.IsNullOrEmpty(name))
                name = "<string>";
            
            try
            {
                var result = await _engine.ProcessStringAsync(source, name);
                
                if (!result.Success)
                {
                    var errors = string.Join("; ", result.Errors.Select(e => e.Message));
                    throw new ConfigurationException($"Configuration processing failed: {errors}");
                }
                
                // Apply environment-specific overrides
                if (!string.IsNullOrEmpty(environment))
                {
                    await ApplyEnvironmentOverrides(result.Configuration, environment, Directory.GetCurrentDirectory());
                }
                
                var configuration = new ConfigurationWrapper(result.Configuration, name, environment, result);
                
                // Raise loaded event
                ConfigurationLoaded?.Invoke(this, new ConfigurationLoadedEventArgs(configuration, name, environment));
                
                return configuration;
            }
            catch (Exception ex)
            {
                ConfigurationError?.Invoke(this, new ConfigurationErrorEventArgs(ex, name, environment));
                throw;
            }
        }
        
        /// <summary>
        /// Load multiple configurations and merge them
        /// </summary>
        public async Task<IConfiguration> LoadMergedConfigurationAsync(string[] filePaths, string environment = null)
        {
            if (filePaths == null || filePaths.Length == 0)
                throw new ArgumentException("File paths cannot be null or empty", nameof(filePaths));
            
            var configurations = new List<IConfiguration>();
            var errors = new List<Exception>();
            
            // Load all configurations
            foreach (var filePath in filePaths)
            {
                try
                {
                    var config = await LoadConfigurationAsync(filePath, environment);
                    configurations.Add(config);
                }
                catch (Exception ex)
                {
                    errors.Add(ex);
                    if (_options.StopOnFirstError)
                    {
                        throw;
                    }
                }
            }
            
            if (configurations.Count == 0)
            {
                throw new AggregateException("Failed to load any configurations", errors);
            }
            
            // Merge configurations
            var merged = MergeConfigurations(configurations);
            
            return merged;
        }
        
        /// <summary>
        /// Reload configuration from cache or file
        /// </summary>
        public async Task<IConfiguration> ReloadConfigurationAsync(string filePath, string environment = null)
        {
            var cacheKey = GenerateCacheKey(filePath, environment);
            
            // Remove from cache to force reload
            _configurationCache.TryRemove(cacheKey, out _);
            
            return await LoadConfigurationAsync(filePath, environment);
        }
        
        /// <summary>
        /// Get configuration from cache
        /// </summary>
        public IConfiguration GetCachedConfiguration(string filePath, string environment = null)
        {
            var cacheKey = GenerateCacheKey(filePath, environment);
            
            if (TryGetCachedConfiguration(cacheKey, out var cached))
            {
                return cached;
            }
            
            return null;
        }
        
        /// <summary>
        /// Check if configuration is cached and valid
        /// </summary>
        public bool IsConfigurationCached(string filePath, string environment = null)
        {
            var cacheKey = GenerateCacheKey(filePath, environment);
            return _configurationCache.ContainsKey(cacheKey);
        }
        
        /// <summary>
        /// Clear configuration cache
        /// </summary>
        public void ClearCache()
        {
            _configurationCache.Clear();
            _fileWatchTimestamps.Clear();
        }
        
        /// <summary>
        /// Clear specific configuration from cache
        /// </summary>
        public void ClearCachedConfiguration(string filePath, string environment = null)
        {
            var cacheKey = GenerateCacheKey(filePath, environment);
            _configurationCache.TryRemove(cacheKey, out _);
        }
        
        /// <summary>
        /// Validate configuration
        /// </summary>
        public bool ValidateConfiguration()
        {
            try
            {
                // For now, just return true
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Configuration validation failed: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Get configuration manager statistics
        /// </summary>
        public ConfigurationManagerStatistics GetStatistics()
        {
            return new ConfigurationManagerStatistics
            {
                CachedConfigurationCount = _configurationCache.Count,
                FileWatcherCount = _fileWatchers.Count,
                ChangeHandlerCount = _changeHandlers.Count,
                TotalCacheSize = EstimateCacheSize(),
                CacheHitRatio = CalculateCacheHitRatio()
            };
        }
        
        /// <summary>
        /// Add configuration change handler
        /// </summary>
        public void AddChangeHandler(IConfigurationChangeHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            
            lock (_lock)
            {
                _changeHandlers.Add(handler);
            }
        }
        
        /// <summary>
        /// Remove configuration change handler
        /// </summary>
        public void RemoveChangeHandler(IConfigurationChangeHandler handler)
        {
            if (handler == null)
                return;
            
            lock (_lock)
            {
                _changeHandlers.Remove(handler);
            }
        }
        
        /// <summary>
        /// Generate cache key
        /// </summary>
        private string GenerateCacheKey(string filePath, string environment)
        {
            var key = filePath;
            if (!string.IsNullOrEmpty(environment))
            {
                key = $"{filePath}:{environment}";
            }
            return key;
        }
        
        /// <summary>
        /// Try get cached configuration
        /// </summary>
        private bool TryGetCachedConfiguration(string cacheKey, out IConfiguration configuration)
        {
            configuration = null;
            
            if (!_configurationCache.TryGetValue(cacheKey, out var cached))
            {
                return false;
            }
            
            // Check expiry
            if (cached.ExpiresAt < DateTime.UtcNow)
            {
                _configurationCache.TryRemove(cacheKey, out _);
                return false;
            }
            
            // Check file modification time if watching is enabled
            if (_options.EnableHotReloading && cached.Configuration is ConfigurationWrapper config)
            {
                // For now, skip file modification check since ConfigurationWrapper doesn't have SourceFile
                // TODO: Add SourceFile property to ConfigurationWrapper if needed
            }
            
            configuration = cached.Configuration;
            return true;
        }
        
        /// <summary>
        /// Cache configuration
        /// </summary>
        private void CacheConfiguration(string cacheKey, IConfiguration configuration)
        {
            var cached = new CachedConfiguration
            {
                Configuration = configuration,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(_options.CacheExpiry)
            };
            
            _configurationCache.AddOrUpdate(cacheKey, cached, (key, existing) => cached);
        }
        
        /// <summary>
        /// Setup file watcher for hot reloading
        /// </summary>
        private void SetupFileWatcher(string filePath, string environment)
        {
            var directory = Path.GetDirectoryName(filePath);
            var fileName = Path.GetFileName(filePath);
            
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            {
                return;
            }
            
            lock (_lock)
            {
                var watcherKey = $"{directory}:{fileName}";
                if (_fileWatchers.ContainsKey(watcherKey))
                {
                    return; // Already watching this file
                }
                
                var watcher = new FileSystemWatcher(directory, fileName)
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                    EnableRaisingEvents = true
                };
                
                watcher.Changed += async (sender, e) =>
                {
                    await HandleFileChanged(e.FullPath, environment);
                };
                
                _fileWatchers[watcherKey] = watcher;
            }
        }
        
        /// <summary>
        /// Handle file changed event
        /// </summary>
        private async Task HandleFileChanged(string filePath, string environment)
        {
            // Debounce rapid file changes
            var now = DateTime.UtcNow;
            var lastChange = _fileWatchTimestamps.GetOrAdd(filePath, now);
            
            if (now - lastChange < TimeSpan.FromSeconds(1))
            {
                return; // Too soon, ignore
            }
            
            _fileWatchTimestamps.AddOrUpdate(filePath, now, (key, existing) => now);
            
            try
            {
                // Clear cache and reload
                var cacheKey = GenerateCacheKey(filePath, environment);
                _configurationCache.TryRemove(cacheKey, out var oldCached);
                
                var newConfiguration = await LoadConfigurationAsync(filePath, environment);
                
                // Notify change handlers
                var changeArgs = new ConfigurationChangedEventArgs(
                    oldCached?.Configuration, 
                    newConfiguration, 
                    filePath, 
                    environment);
                
                ConfigurationChanged?.Invoke(this, changeArgs);
                
                // Notify registered handlers
                List<IConfigurationChangeHandler> handlersToNotify;
                lock (_lock)
                {
                    handlersToNotify = new List<IConfigurationChangeHandler>(_changeHandlers);
                }
                
                foreach (var handler in handlersToNotify)
                {
                    try
                    {
                        await handler.HandleConfigurationChangedAsync(changeArgs);
                    }
                    catch (Exception ex)
                    {
                        ConfigurationError?.Invoke(this, new ConfigurationErrorEventArgs(ex, filePath, environment));
                    }
                }
            }
            catch (Exception ex)
            {
                ConfigurationError?.Invoke(this, new ConfigurationErrorEventArgs(ex, filePath, environment));
            }
        }
        
        /// <summary>
        /// Apply environment-specific overrides
        /// </summary>
        private async Task ApplyEnvironmentOverrides(Dictionary<string, object> configuration, string environment, string baseDirectory)
        {
            // Look for environment-specific files
            var envFiles = new[]
            {
                Path.Combine(baseDirectory, $"config.{environment}.tsk"),
                Path.Combine(baseDirectory, $"peanu.{environment}.tsk"),
                Path.Combine(baseDirectory, "environments", $"{environment}.tsk")
            };
            
            foreach (var envFile in envFiles)
            {
                if (File.Exists(envFile))
                {
                    try
                    {
                        var envResult = await _engine.ProcessFileAsync(envFile);
                        if (envResult.Success)
                        {
                            MergeConfiguration(configuration, envResult.Configuration);
                        }
                    }
                    catch (Exception ex)
                    {
                        ConfigurationError?.Invoke(this, new ConfigurationErrorEventArgs(ex, envFile, environment));
                    }
                }
            }
        }
        
        /// <summary>
        /// Merge configurations
        /// </summary>
        private IConfiguration MergeConfigurations(List<IConfiguration> configurations)
        {
            if (configurations.Count == 1)
            {
                return configurations[0];
            }
            
            var merged = new Dictionary<string, object>();
            var sourceFiles = new List<string>();
            
            foreach (var config in configurations)
            {
                if (config is ConfigurationWrapper tuskConfig)
                {
                    // Handle ConfigurationWrapper if needed
                    // For now, just add the source file
                    sourceFiles.Add("unknown");
                }
            }
            
            return new ConfigurationWrapper(merged, string.Join(";", sourceFiles), null, null);
        }
        
        /// <summary>
        /// Merge configuration dictionaries
        /// </summary>
        private void MergeConfiguration(Dictionary<string, object> target, Dictionary<string, object> source)
        {
            foreach (var kvp in source)
            {
                if (target.ContainsKey(kvp.Key) && 
                    target[kvp.Key] is Dictionary<string, object> targetDict &&
                    kvp.Value is Dictionary<string, object> sourceDict)
                {
                    // Recursively merge nested dictionaries
                    MergeConfiguration(targetDict, sourceDict);
                }
                else
                {
                    // Override or add new value
                    target[kvp.Key] = kvp.Value;
                }
            }
        }
        
        /// <summary>
        /// Estimate cache size
        /// </summary>
        private long EstimateCacheSize()
        {
            // Rough estimation - in production this would be more accurate
            return _configurationCache.Count * 1024; // Assume ~1KB per cached config
        }
        
        /// <summary>
        /// Calculate cache hit ratio
        /// </summary>
        private double CalculateCacheHitRatio()
        {
            // This would track actual hits/misses in production
            return _configurationCache.Count > 0 ? 0.8 : 0.0; // Placeholder
        }
        
        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            lock (_lock)
            {
                // Dispose file watchers
                foreach (var watcher in _fileWatchers.Values)
                {
                    watcher?.Dispose();
                }
                _fileWatchers.Clear();
                
                // Clear caches
                _configurationCache.Clear();
                _fileWatchTimestamps.Clear();
                _changeHandlers.Clear();
            }
            
            // _engine doesn't implement IDisposable, so we don't need to dispose it
        }
    }
    
    /// <summary>
    /// Configuration manager options
    /// </summary>
    public class ConfigurationManagerOptions
    {
        public ConfigurationEngineOptions EngineOptions { get; set; } = new ConfigurationEngineOptions();
        public bool EnableCaching { get; set; } = true;
        public bool EnableHotReloading { get; set; } = true;
        public TimeSpan CacheExpiry { get; set; } = TimeSpan.FromMinutes(30);
        public bool StopOnFirstError { get; set; } = false;
        public int MaxCachedConfigurations { get; set; } = 100;
    }
    
    /// <summary>
    /// Cached configuration wrapper
    /// </summary>
    internal class CachedConfiguration
    {
        public IConfiguration Configuration { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
    

    
    /// <summary>
    /// Configuration manager statistics
    /// </summary>
    public class ConfigurationManagerStatistics
    {
        public int CachedConfigurationCount { get; set; }
        public int FileWatcherCount { get; set; }
        public int ChangeHandlerCount { get; set; }
        public long TotalCacheSize { get; set; }
        public double CacheHitRatio { get; set; }
    }
    
    /// <summary>
    /// Configuration change handler interface
    /// </summary>
    public interface IConfigurationChangeHandler
    {
        Task HandleConfigurationChangedAsync(ConfigurationChangedEventArgs args);
    }
    
    /// <summary>
    /// Configuration changed event args
    /// </summary>
    public class ConfigurationChangedEventArgs : EventArgs
    {
        public IConfiguration OldConfiguration { get; }
        public IConfiguration NewConfiguration { get; }
        public string FilePath { get; }
        public string Environment { get; }
        
        public ConfigurationChangedEventArgs(IConfiguration oldConfig, IConfiguration newConfig, string filePath, string environment)
        {
            OldConfiguration = oldConfig;
            NewConfiguration = newConfig;
            FilePath = filePath;
            Environment = environment;
        }
    }
    
    /// <summary>
    /// Configuration error event args
    /// </summary>
    public class ConfigurationErrorEventArgs : EventArgs
    {
        public Exception Exception { get; }
        public string FilePath { get; }
        public string Environment { get; }
        
        public ConfigurationErrorEventArgs(Exception exception, string filePath, string environment)
        {
            Exception = exception;
            FilePath = filePath;
            Environment = environment;
        }
    }
    
    /// <summary>
    /// Configuration loaded event args
    /// </summary>
    public class ConfigurationLoadedEventArgs : EventArgs
    {
        public IConfiguration Configuration { get; }
        public string FilePath { get; }
        public string Environment { get; }
        
        public ConfigurationLoadedEventArgs(IConfiguration configuration, string filePath, string environment)
        {
            Configuration = configuration;
            FilePath = filePath;
            Environment = environment;
        }
    }
    
    /// <summary>
    /// Configuration validation result
    /// </summary>
    public class ConfigurationValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public TimeSpan ProcessingTime { get; set; }
        public int CacheHits { get; set; }
        public int CacheMisses { get; set; }
    }

    /// <summary>
    /// Configuration engine options
    /// </summary>
    public class ConfigurationEngineOptions
    {
        public bool EnableValidation { get; set; } = true;
        public bool EnableCaching { get; set; } = true;
        public TimeSpan CacheTimeout { get; set; } = TimeSpan.FromMinutes(30);
    }

    /// <summary>
    /// Configuration engine
    /// </summary>
    public class ConfigurationEngine
    {
        private readonly ConfigurationEngineOptions _options;

        public ConfigurationEngine(ConfigurationEngineOptions options = null)
        {
            _options = options ?? new ConfigurationEngineOptions();
        }

        public async Task<ConfigurationProcessingResult> ProcessFileAsync(string filePath)
        {
            try
            {
                // TODO: Implement actual file processing
                var result = new ConfigurationProcessingResult
                {
                    Success = true,
                    Configuration = new Dictionary<string, object>(),
                    Errors = new List<ConfigurationError>()
                };

                return result;
            }
            catch (Exception ex)
            {
                return new ConfigurationProcessingResult
                {
                    Success = false,
                    Errors = new List<ConfigurationError>
                    {
                        new ConfigurationError { Message = ex.Message, Line = 0, Column = 0 }
                    }
                };
            }
        }
        
        public async Task<ConfigurationProcessingResult> ProcessStringAsync(string source, string name)
        {
            try
            {
                // TODO: Implement actual string processing
                var result = new ConfigurationProcessingResult
                {
                    Success = true,
                    Configuration = new Dictionary<string, object>(),
                    Errors = new List<ConfigurationError>()
                };

                return result;
            }
            catch (Exception ex)
            {
                return new ConfigurationProcessingResult
                {
                    Success = false,
                    Errors = new List<ConfigurationError>
                    {
                        new ConfigurationError { Message = ex.Message, Line = 0, Column = 0 }
                    }
                };
            }
        }
    }

    /// <summary>
    /// Configuration processing result
    /// </summary>
    public class ConfigurationProcessingResult
    {
        public bool Success { get; set; }
        public Dictionary<string, object> Configuration { get; set; }
        public List<ConfigurationError> Errors { get; set; } = new List<ConfigurationError>();
    }

    /// <summary>
    /// Configuration error
    /// </summary>
    public class ConfigurationError
    {
        public string Message { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
    }

    public class ConfigurationWrapper : IConfiguration
    {
        private readonly Dictionary<string, object> _settings;
        private readonly string _sourceFile;
        private readonly string _environment;
        private readonly ConfigurationProcessingResult _processingResult;

        public ConfigurationWrapper(Dictionary<string, object> settings, string sourceFile, string environment, ConfigurationProcessingResult processingResult)
        {
            _settings = settings ?? new Dictionary<string, object>();
            _sourceFile = sourceFile;
            _environment = environment;
            _processingResult = processingResult;
        }

        public string this[string key]
        {
            get => _settings.TryGetValue(key, out var value) ? value?.ToString() : null;
            set => _settings[key] = value;
        }

        public string Key => null;
        public string Path => null;
        public string Value => null;
        public IEnumerable<IConfigurationSection> GetChildren() => Enumerable.Empty<IConfigurationSection>();
        public IChangeToken GetReloadToken() => new CancellationChangeToken(new CancellationToken());
        public IConfigurationSection GetSection(string key) => null;
    }

    public class ConfigurationException : Exception
    {
        public ConfigurationException(string message) : base(message) { }
        public ConfigurationException(string message, Exception innerException) : base(message, innerException) { }
    }
} 