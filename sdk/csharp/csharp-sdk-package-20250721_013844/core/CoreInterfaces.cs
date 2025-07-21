using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TuskLang.Core
{
    /// <summary>
    /// Core interfaces and base classes for TuskLang SDK
    /// 
    /// Defines the fundamental contracts and base implementations:
    /// - ISdkComponent interface for all SDK components
    /// - IConfigurationProvider for configuration access
    /// - ILogger for logging and diagnostics
    /// - IValidator for data validation
    /// - Base classes with common functionality
    /// - Extension methods for common operations
    /// - Exception types for SDK-specific errors
    /// - Result types for operation outcomes
    /// 
    /// Provides the foundation for consistent SDK architecture
    /// </summary>
    
    /// <summary>
    /// Base interface for all SDK components
    /// </summary>
    public interface ISdkComponent : IDisposable
    {
        /// <summary>
        /// Component name
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Component version
        /// </summary>
        string Version { get; }
        
        /// <summary>
        /// Component status
        /// </summary>
        ComponentStatus Status { get; }
        
        /// <summary>
        /// Initialize the component
        /// </summary>
        Task<bool> InitializeAsync();
        
        /// <summary>
        /// Shutdown the component
        /// </summary>
        Task ShutdownAsync();
        
        /// <summary>
        /// Get component statistics
        /// </summary>
        ComponentStatistics GetStatistics();
    }
    
    /// <summary>
    /// Configuration provider interface
    /// </summary>
    public interface IConfigurationProvider
    {
        /// <summary>
        /// Get configuration value
        /// </summary>
        T Get<T>(string key, T defaultValue = default);
        
        /// <summary>
        /// Set configuration value
        /// </summary>
        void Set<T>(string key, T value);
        
        /// <summary>
        /// Check if configuration key exists
        /// </summary>
        bool HasKey(string key);
        
        /// <summary>
        /// Get all configuration keys
        /// </summary>
        IEnumerable<string> GetKeys();
        
        /// <summary>
        /// Reload configuration
        /// </summary>
        Task ReloadAsync();
    }
    
    /// <summary>
    /// Logger interface
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Log debug message
        /// </summary>
        void Debug(string message, params object[] args);
        
        /// <summary>
        /// Log information message
        /// </summary>
        void Info(string message, params object[] args);
        
        /// <summary>
        /// Log warning message
        /// </summary>
        void Warning(string message, params object[] args);
        
        /// <summary>
        /// Log error message
        /// </summary>
        void Error(string message, Exception exception = null, params object[] args);
        
        /// <summary>
        /// Log critical message
        /// </summary>
        void Critical(string message, Exception exception = null, params object[] args);
        
        /// <summary>
        /// Check if debug logging is enabled
        /// </summary>
        bool IsDebugEnabled { get; }
        
        /// <summary>
        /// Check if info logging is enabled
        /// </summary>
        bool IsInfoEnabled { get; }
    }
    
    /// <summary>
    /// Validator interface
    /// </summary>
    public interface IValidator<T>
    {
        /// <summary>
        /// Validate object
        /// </summary>
        ValidationResult Validate(T item);
        
        /// <summary>
        /// Validate multiple objects
        /// </summary>
        IEnumerable<ValidationResult> ValidateMany(IEnumerable<T> items);
    }
    
    /// <summary>
    /// Cache interface
    /// </summary>
    public interface ICache<TKey, TValue>
    {
        /// <summary>
        /// Get value from cache
        /// </summary>
        bool TryGet(TKey key, out TValue value);
        
        /// <summary>
        /// Set value in cache
        /// </summary>
        void Set(TKey key, TValue value, TimeSpan? expiry = null);
        
        /// <summary>
        /// Remove value from cache
        /// </summary>
        bool Remove(TKey key);
        
        /// <summary>
        /// Clear all cache entries
        /// </summary>
        void Clear();
        
        /// <summary>
        /// Get cache statistics
        /// </summary>
        CacheStatistics GetStatistics();
    }
    
    /// <summary>
    /// Base SDK component implementation
    /// </summary>
    public abstract class SdkComponentBase : ISdkComponent
    {
        protected readonly ILogger _logger;
        protected readonly IConfigurationProvider _config;
        protected ComponentStatus _status;
        protected DateTime _initializedAt;
        protected DateTime _lastActivity;
        
        protected SdkComponentBase(ILogger logger, IConfigurationProvider config)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _status = ComponentStatus.Created;
            _initializedAt = DateTime.MinValue;
            _lastActivity = DateTime.UtcNow;
        }
        
        public abstract string Name { get; }
        public abstract string Version { get; }
        
        public ComponentStatus Status => _status;
        
        public virtual async Task<bool> InitializeAsync()
        {
            try
            {
                _logger.Info("Initializing component: {ComponentName}", Name);
                
                var result = await OnInitializeAsync();
                
                if (result)
                {
                    _status = ComponentStatus.Initialized;
                    _initializedAt = DateTime.UtcNow;
                    _logger.Info("Component initialized successfully: {ComponentName}", Name);
                }
                else
                {
                    _status = ComponentStatus.Failed;
                    _logger.Error("Component initialization failed: {ComponentName}", Name);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _status = ComponentStatus.Failed;
                _logger.Error("Component initialization error: {ComponentName}", ex, Name);
                return false;
            }
        }
        
        public virtual async Task ShutdownAsync()
        {
            try
            {
                _logger.Info("Shutting down component: {ComponentName}", Name);
                
                await OnShutdownAsync();
                
                _status = ComponentStatus.Shutdown;
                _logger.Info("Component shutdown completed: {ComponentName}", Name);
            }
            catch (Exception ex)
            {
                _logger.Error("Component shutdown error: {ComponentName}", ex, Name);
            }
        }
        
        public virtual ComponentStatistics GetStatistics()
        {
            return new ComponentStatistics
            {
                Name = Name,
                Version = Version,
                Status = Status,
                InitializedAt = _initializedAt,
                LastActivity = _lastActivity,
                Uptime = _initializedAt != DateTime.MinValue ? DateTime.UtcNow - _initializedAt : TimeSpan.Zero
            };
        }
        
        protected virtual void UpdateActivity()
        {
            _lastActivity = DateTime.UtcNow;
        }
        
        protected abstract Task<bool> OnInitializeAsync();
        protected abstract Task OnShutdownAsync();
        
        public virtual void Dispose()
        {
            try
            {
                ShutdownAsync().Wait();
            }
            catch (Exception ex)
            {
                _logger.Error("Error during disposal: {ComponentName}", ex, Name);
            }
        }
    }
    
    /// <summary>
    /// Base configuration provider implementation
    /// </summary>
    public abstract class ConfigurationProviderBase : IConfigurationProvider
    {
        protected readonly Dictionary<string, object> _settings;
        protected readonly object _lock;
        protected readonly ILogger _logger;
        
        protected ConfigurationProviderBase(ILogger logger)
        {
            _settings = new Dictionary<string, object>();
            _lock = new object();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        public virtual T Get<T>(string key, T defaultValue = default)
        {
            if (string.IsNullOrEmpty(key))
                return defaultValue;
            
            lock (_lock)
            {
                if (_settings.TryGetValue(key, out var value))
                {
                    if (value is T typedValue)
                        return typedValue;
                    
                    try
                    {
                        return (T)Convert.ChangeType(value, typeof(T));
                    }
                    catch
                    {
                        _logger.Warning("Failed to convert configuration value for key: {Key}", key);
                        return defaultValue;
                    }
                }
                
                return defaultValue;
            }
        }
        
        public virtual void Set<T>(string key, T value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Configuration key cannot be null or empty", nameof(key));
            
            lock (_lock)
            {
                _settings[key] = value;
                _logger.Debug("Configuration updated: {Key} = {Value}", key, value);
            }
        }
        
        public virtual bool HasKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;
            
            lock (_lock)
            {
                return _settings.ContainsKey(key);
            }
        }
        
        public virtual IEnumerable<string> GetKeys()
        {
            lock (_lock)
            {
                return _settings.Keys.ToArray();
            }
        }
        
        public abstract Task ReloadAsync();
    }
    
    /// <summary>
    /// Base logger implementation
    /// </summary>
    public abstract class LoggerBase : ILogger
    {
        protected readonly LogLevel _minimumLevel;
        protected readonly string _componentName;
        
        protected LoggerBase(string componentName, LogLevel minimumLevel = LogLevel.Info)
        {
            _componentName = componentName ?? "Unknown";
            _minimumLevel = minimumLevel;
        }
        
        public bool IsDebugEnabled => _minimumLevel <= LogLevel.Debug;
        public bool IsInfoEnabled => _minimumLevel <= LogLevel.Info;
        
        public virtual void Debug(string message, params object[] args)
        {
            if (IsDebugEnabled)
            {
                Log(LogLevel.Debug, message, null, args);
            }
        }
        
        public virtual void Info(string message, params object[] args)
        {
            if (IsInfoEnabled)
            {
                Log(LogLevel.Info, message, null, args);
            }
        }
        
        public virtual void Warning(string message, params object[] args)
        {
            Log(LogLevel.Warning, message, null, args);
        }
        
        public virtual void Error(string message, Exception exception = null, params object[] args)
        {
            Log(LogLevel.Error, message, exception, args);
        }
        
        public virtual void Critical(string message, Exception exception = null, params object[] args)
        {
            Log(LogLevel.Critical, message, exception, args);
        }
        
        protected abstract void Log(LogLevel level, string message, Exception exception, object[] args);
        
        protected string FormatMessage(LogLevel level, string message, object[] args)
        {
            try
            {
                var formattedMessage = args?.Length > 0 ? string.Format(message, args) : message;
                return $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [{level}] [{_componentName}] {formattedMessage}";
            }
            catch
            {
                return $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [{level}] [{_componentName}] {message}";
            }
        }
    }
    
    /// <summary>
    /// Base validator implementation
    /// </summary>
    public abstract class ValidatorBase<T> : IValidator<T>
    {
        protected readonly ILogger _logger;
        
        protected ValidatorBase(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        public virtual ValidationResult Validate(T item)
        {
            if (item == null)
            {
                return ValidationResult.Failure("Item cannot be null");
            }
            
            try
            {
                return OnValidate(item);
            }
            catch (Exception ex)
            {
                _logger.Error("Validation error", ex);
                return ValidationResult.Failure($"Validation error: {ex.Message}");
            }
        }
        
        public virtual IEnumerable<ValidationResult> ValidateMany(IEnumerable<T> items)
        {
            if (items == null)
            {
                yield return ValidationResult.Failure("Items collection cannot be null");
                yield break;
            }
            
            foreach (var item in items)
            {
                yield return Validate(item);
            }
        }
        
        protected abstract ValidationResult OnValidate(T item);
    }
    
    /// <summary>
    /// Base cache implementation
    /// </summary>
    public abstract class CacheBase<TKey, TValue> : ICache<TKey, TValue>
    {
        protected readonly Dictionary<TKey, CacheEntry<TValue>> _cache;
        protected readonly object _lock;
        protected readonly ILogger _logger;
        protected readonly int _maxSize;
        
        protected CacheBase(ILogger logger, int maxSize = 1000)
        {
            _cache = new Dictionary<TKey, CacheEntry<TValue>>();
            _lock = new object();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _maxSize = maxSize;
        }
        
        public virtual bool TryGet(TKey key, out TValue value)
        {
            value = default;
            
            if (key == null)
                return false;
            
            lock (_lock)
            {
                if (_cache.TryGetValue(key, out var entry))
                {
                    if (entry.IsExpired)
                    {
                        _cache.Remove(key);
                        return false;
                    }
                    
                    entry.LastAccessed = DateTime.UtcNow;
                    value = entry.Value;
                    return true;
                }
                
                return false;
            }
        }
        
        public virtual void Set(TKey key, TValue value, TimeSpan? expiry = null)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            
            lock (_lock)
            {
                // Remove expired entries
                RemoveExpiredEntries();
                
                // Check if cache is full
                if (_cache.Count >= _maxSize)
                {
                    RemoveLeastRecentlyUsed();
                }
                
                var entry = new CacheEntry<TValue>
                {
                    Value = value,
                    CreatedAt = DateTime.UtcNow,
                    LastAccessed = DateTime.UtcNow,
                    ExpiresAt = expiry.HasValue ? DateTime.UtcNow.Add(expiry.Value) : DateTime.MaxValue
                };
                
                _cache[key] = entry;
                _logger.Debug("Cache entry set: {Key}", key);
            }
        }
        
        public virtual bool Remove(TKey key)
        {
            if (key == null)
                return false;
            
            lock (_lock)
            {
                var removed = _cache.Remove(key);
                if (removed)
                {
                    _logger.Debug("Cache entry removed: {Key}", key);
                }
                return removed;
            }
        }
        
        public virtual void Clear()
        {
            lock (_lock)
            {
                var count = _cache.Count;
                _cache.Clear();
                _logger.Info("Cache cleared: {Count} entries removed", count);
            }
        }
        
        public virtual CacheStatistics GetStatistics()
        {
            lock (_lock)
            {
                RemoveExpiredEntries();
                
                return new CacheStatistics
                {
                    TotalEntries = _cache.Count,
                    MaxSize = _maxSize,
                    HitRatio = CalculateHitRatio(),
                    GeneratedAt = DateTime.UtcNow
                };
            }
        }
        
        protected virtual void RemoveExpiredEntries()
        {
            var expiredKeys = _cache.Where(kvp => kvp.Value.IsExpired).Select(kvp => kvp.Key).ToArray();
            
            foreach (var key in expiredKeys)
            {
                _cache.Remove(key);
            }
            
            if (expiredKeys.Length > 0)
            {
                _logger.Debug("Removed {Count} expired cache entries", expiredKeys.Length);
            }
        }
        
        protected virtual void RemoveLeastRecentlyUsed()
        {
            var lruKey = _cache.OrderBy(kvp => kvp.Value.LastAccessed).First().Key;
            _cache.Remove(lruKey);
            _logger.Debug("Removed least recently used cache entry: {Key}", lruKey);
        }
        
        protected virtual double CalculateHitRatio()
        {
            // In a complete implementation, this would track actual hits/misses
            return _cache.Count > 0 ? 0.8 : 0.0; // Placeholder
        }
    }
    
    /// <summary>
    /// Component status enumeration
    /// </summary>
    public enum ComponentStatus
    {
        Created,
        Initializing,
        Initialized,
        Running,
        Stopping,
        Stopped,
        Failed,
        Shutdown
    }
    
    /// <summary>
    /// Log level enumeration
    /// </summary>
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        Critical = 4
    }
    
    /// <summary>
    /// Validation result
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        
        public static ValidationResult Success()
        {
            return new ValidationResult { IsValid = true };
        }
        
        public static ValidationResult Failure(string error)
        {
            return new ValidationResult
            {
                IsValid = false,
                Errors = { error }
            };
        }
        
        public static ValidationResult Failure(IEnumerable<string> errors)
        {
            return new ValidationResult
            {
                IsValid = false,
                Errors = errors.ToList()
            };
        }
    }
    
    /// <summary>
    /// Component statistics
    /// </summary>
    public class ComponentStatistics
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public ComponentStatus Status { get; set; }
        public DateTime InitializedAt { get; set; }
        public DateTime LastActivity { get; set; }
        public TimeSpan Uptime { get; set; }
    }
    
    /// <summary>
    /// Cache entry
    /// </summary>
    public class CacheEntry<T>
    {
        public T Value { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastAccessed { get; set; }
        public DateTime ExpiresAt { get; set; }
        
        public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    }
    
    /// <summary>
    /// Cache statistics
    /// </summary>
    public class CacheStatistics
    {
        public int TotalEntries { get; set; }
        public int MaxSize { get; set; }
        public double HitRatio { get; set; }
        public DateTime GeneratedAt { get; set; }
    }
    
    /// <summary>
    /// SDK exception base class
    /// </summary>
    public class SdkException : Exception
    {
        public string ComponentName { get; }
        public string ErrorCode { get; }
        
        public SdkException(string message, string componentName = null, string errorCode = null)
            : base(message)
        {
            ComponentName = componentName;
            ErrorCode = errorCode;
        }
        
        public SdkException(string message, Exception innerException, string componentName = null, string errorCode = null)
            : base(message, innerException)
        {
            ComponentName = componentName;
            ErrorCode = errorCode;
        }
    }
    
    /// <summary>
    /// Operation result
    /// </summary>
    public class OperationResult<T>
    {
        public bool Success { get; set; }
        public T Data { get; set; }
        public string Error { get; set; }
        public Exception Exception { get; set; }
        
        public static OperationResult<T> SuccessResult(T data)
        {
            return new OperationResult<T>
            {
                Success = true,
                Data = data
            };
        }
        
        public static OperationResult<T> FailureResult(string error, Exception exception = null)
        {
            return new OperationResult<T>
            {
                Success = false,
                Error = error,
                Exception = exception
            };
        }
    }
} 