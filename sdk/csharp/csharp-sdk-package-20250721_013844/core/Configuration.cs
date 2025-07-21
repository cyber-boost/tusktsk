using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace TuskLang.Configuration
{
    /// <summary>
    /// Configuration interface for accessing processed configuration data
    /// </summary>
    public interface IConfiguration
    {
        /// <summary>
        /// Get configuration value by key
        /// </summary>
        T Get<T>(string key, T defaultValue = default);
        
        /// <summary>
        /// Get configuration value by key with conversion
        /// </summary>
        object Get(string key, Type type, object defaultValue = null);
        
        /// <summary>
        /// Get string value
        /// </summary>
        string GetString(string key, string defaultValue = null);
        
        /// <summary>
        /// Get integer value
        /// </summary>
        int GetInt(string key, int defaultValue = 0);
        
        /// <summary>
        /// Get boolean value
        /// </summary>
        bool GetBool(string key, bool defaultValue = false);
        
        /// <summary>
        /// Get double value
        /// </summary>
        double GetDouble(string key, double defaultValue = 0.0);
        
        /// <summary>
        /// Get array value
        /// </summary>
        T[] GetArray<T>(string key, T[] defaultValue = null);
        
        /// <summary>
        /// Get section as configuration
        /// </summary>
        IConfiguration GetSection(string sectionName);
        
        /// <summary>
        /// Check if key exists
        /// </summary>
        bool HasKey(string key);
        
        /// <summary>
        /// Get all keys
        /// </summary>
        string[] GetKeys();
        
        /// <summary>
        /// Get all settings as dictionary
        /// </summary>
        Dictionary<string, object> GetAllSettings();
        
        /// <summary>
        /// Source file path
        /// </summary>
        string SourceFile { get; }
        
        /// <summary>
        /// Environment name
        /// </summary>
        string Environment { get; }
    }
    
    /// <summary>
    /// Configuration implementation providing access to processed configuration data
    /// 
    /// Features:
    /// - Type-safe configuration value access
    /// - Nested section support with dot notation
    /// - Automatic type conversion and validation
    /// - Default value handling
    /// - Environment and source tracking
    /// - High-performance key lookup and caching
    /// 
    /// Performance: Optimized dictionary access, cached conversions
    /// </summary>
    public class Configuration : IConfiguration, IDisposable
    {
        private readonly Dictionary<string, object> _settings;
        private readonly Dictionary<string, object> _cache;
        private readonly object _lock;
        private readonly ConfigurationResult _processingResult;
        private bool _isDisposed;
        
        /// <summary>
        /// Source file path
        /// </summary>
        public string SourceFile { get; }
        
        /// <summary>
        /// Environment name
        /// </summary>
        public string Environment { get; }
        
        /// <summary>
        /// Processing result information
        /// </summary>
        public ConfigurationResult ProcessingResult => _processingResult;
        
        /// <summary>
        /// Initializes a new instance of Configuration
        /// </summary>
        public Configuration(Dictionary<string, object> settings, string sourceFile, string environment, ConfigurationResult processingResult)
        {
            _settings = settings ?? new Dictionary<string, object>();
            _cache = new Dictionary<string, object>();
            _lock = new object();
            _processingResult = processingResult;
            SourceFile = sourceFile;
            Environment = environment;
            _isDisposed = false;
        }
        
        /// <summary>
        /// Get configuration value by key
        /// </summary>
        public T Get<T>(string key, T defaultValue = default)
        {
            if (string.IsNullOrEmpty(key))
                return defaultValue;
            
            try
            {
                var value = GetValue(key);
                if (value == null)
                    return defaultValue;
                
                return ConvertValue<T>(value);
            }
            catch
            {
                return defaultValue;
            }
        }
        
        /// <summary>
        /// Get configuration value by key with type conversion
        /// </summary>
        public object Get(string key, Type type, object defaultValue = null)
        {
            if (string.IsNullOrEmpty(key) || type == null)
                return defaultValue;
            
            try
            {
                var value = GetValue(key);
                if (value == null)
                    return defaultValue;
                
                return ConvertValue(value, type);
            }
            catch
            {
                return defaultValue;
            }
        }
        
        /// <summary>
        /// Get string value
        /// </summary>
        public string GetString(string key, string defaultValue = null)
        {
            return Get(key, defaultValue);
        }
        
        /// <summary>
        /// Get integer value
        /// </summary>
        public int GetInt(string key, int defaultValue = 0)
        {
            return Get(key, defaultValue);
        }
        
        /// <summary>
        /// Get boolean value
        /// </summary>
        public bool GetBool(string key, bool defaultValue = false)
        {
            return Get(key, defaultValue);
        }
        
        /// <summary>
        /// Get double value
        /// </summary>
        public double GetDouble(string key, double defaultValue = 0.0)
        {
            return Get(key, defaultValue);
        }
        
        /// <summary>
        /// Get array value
        /// </summary>
        public T[] GetArray<T>(string key, T[] defaultValue = null)
        {
            try
            {
                var value = GetValue(key);
                if (value == null)
                    return defaultValue;
                
                if (value is T[] directArray)
                    return directArray;
                
                if (value is Array array)
                {
                    var result = new T[array.Length];
                    for (int i = 0; i < array.Length; i++)
                    {
                        result[i] = ConvertValue<T>(array.GetValue(i));
                    }
                    return result;
                }
                
                if (value is IEnumerable<object> enumerable)
                {
                    return enumerable.Select(ConvertValue<T>).ToArray();
                }
                
                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }
        
        /// <summary>
        /// Get section as configuration
        /// </summary>
        public IConfiguration GetSection(string sectionName)
        {
            if (string.IsNullOrEmpty(sectionName))
                return new Configuration(new Dictionary<string, object>(), SourceFile, Environment, _processingResult);
            
            lock (_lock)
            {
                ThrowIfDisposed();
                
                if (_settings.TryGetValue(sectionName, out var sectionValue) &&
                    sectionValue is Dictionary<string, object> sectionDict)
                {
                    return new Configuration(sectionDict, SourceFile, Environment, _processingResult);
                }
                
                // Try dot notation for nested sections
                var nestedSection = GetNestedSection(sectionName);
                if (nestedSection != null)
                {
                    return new Configuration(nestedSection, SourceFile, Environment, _processingResult);
                }
                
                return new Configuration(new Dictionary<string, object>(), SourceFile, Environment, _processingResult);
            }
        }
        
        /// <summary>
        /// Check if key exists
        /// </summary>
        public bool HasKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;
            
            lock (_lock)
            {
                ThrowIfDisposed();
                return GetValue(key) != null;
            }
        }
        
        /// <summary>
        /// Get all keys
        /// </summary>
        public string[] GetKeys()
        {
            lock (_lock)
            {
                ThrowIfDisposed();
                return GetAllKeysRecursive("", _settings).ToArray();
            }
        }
        
        /// <summary>
        /// Get all settings as dictionary
        /// </summary>
        public Dictionary<string, object> GetAllSettings()
        {
            lock (_lock)
            {
                ThrowIfDisposed();
                return new Dictionary<string, object>(_settings);
            }
        }
        
        /// <summary>
        /// Get configuration statistics
        /// </summary>
        public ConfigurationStatistics GetStatistics()
        {
            lock (_lock)
            {
                ThrowIfDisposed();
                
                return new ConfigurationStatistics
                {
                    TotalKeys = CountKeysRecursive(_settings),
                    TotalSections = CountSections(_settings),
                    CachedValues = _cache.Count,
                    ProcessingTime = _processingResult?.ProcessingTime ?? TimeSpan.Zero,
                    SourceFile = SourceFile,
                    Environment = Environment,
                    HasProcessingResult = _processingResult != null
                };
            }
        }
        
        /// <summary>
        /// Export configuration to dictionary with flattened keys
        /// </summary>
        public Dictionary<string, object> ExportFlattened(string separator = ":")
        {
            lock (_lock)
            {
                ThrowIfDisposed();
                return FlattenDictionary(_settings, "", separator);
            }
        }
        
        /// <summary>
        /// Merge another configuration into this one
        /// </summary>
        public void Merge(IConfiguration other)
        {
            if (other == null)
                return;
            
            lock (_lock)
            {
                ThrowIfDisposed();
                
                var otherSettings = other.GetAllSettings();
                MergeDictionaries(_settings, otherSettings);
                _cache.Clear(); // Clear cache after merge
            }
        }
        
        /// <summary>
        /// Clone configuration
        /// </summary>
        public Configuration Clone()
        {
            lock (_lock)
            {
                ThrowIfDisposed();
                
                var clonedSettings = CloneDictionary(_settings);
                return new Configuration(clonedSettings, SourceFile, Environment, _processingResult);
            }
        }
        
        /// <summary>
        /// Validate configuration against schema
        /// </summary>
        public ConfigurationValidationResult Validate(ConfigurationSchema schema = null)
        {
            var result = new ConfigurationValidationResult
            {
                IsValid = true,
                Errors = new List<string>(),
                Warnings = new List<string>()
            };
            
            if (schema == null)
            {
                // Basic validation without schema
                ValidateBasicStructure(result);
            }
            else
            {
                // Schema-based validation would be implemented here
                result.Warnings.Add("Schema-based validation not implemented in this version");
            }
            
            return result;
        }
        
        /// <summary>
        /// Get value with dot notation support
        /// </summary>
        private object GetValue(string key)
        {
            lock (_lock)
            {
                ThrowIfDisposed();
                
                // Check cache first
                if (_cache.TryGetValue(key, out var cachedValue))
                {
                    return cachedValue;
                }
                
                var value = GetValueRecursive(key, _settings);
                
                // Cache the result
                _cache[key] = value;
                
                return value;
            }
        }
        
        /// <summary>
        /// Get value recursively with dot notation
        /// </summary>
        private object GetValueRecursive(string key, Dictionary<string, object> dict)
        {
            if (string.IsNullOrEmpty(key))
                return null;
            
            var parts = key.Split('.');
            var current = dict;
            
            for (int i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                
                if (!current.TryGetValue(part, out var value))
                {
                    return null;
                }
                
                if (i == parts.Length - 1)
                {
                    return value; // Last part, return the value
                }
                
                if (value is Dictionary<string, object> nestedDict)
                {
                    current = nestedDict;
                }
                else
                {
                    return null; // Can't traverse further
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Get nested section by dot notation
        /// </summary>
        private Dictionary<string, object> GetNestedSection(string sectionName)
        {
            var parts = sectionName.Split('.');
            var current = _settings;
            
            foreach (var part in parts)
            {
                if (!current.TryGetValue(part, out var value))
                {
                    return null;
                }
                
                if (value is Dictionary<string, object> nestedDict)
                {
                    current = nestedDict;
                }
                else
                {
                    return null;
                }
            }
            
            return current;
        }
        
        /// <summary>
        /// Convert value to specified type
        /// </summary>
        private T ConvertValue<T>(object value)
        {
            return (T)ConvertValue(value, typeof(T));
        }
        
        /// <summary>
        /// Convert value to specified type
        /// </summary>
        private object ConvertValue(object value, Type targetType)
        {
            if (value == null)
                return GetDefaultValue(targetType);
            
            if (targetType.IsAssignableFrom(value.GetType()))
                return value;
            
            // Handle nullable types
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                if (value == null)
                    return null;
                    
                targetType = Nullable.GetUnderlyingType(targetType);
            }
            
            // String conversion
            if (targetType == typeof(string))
                return value.ToString();
            
            // Boolean conversion
            if (targetType == typeof(bool))
                return ConvertToBoolean(value);
            
            // Numeric conversions
            if (targetType == typeof(int))
                return Convert.ToInt32(value, CultureInfo.InvariantCulture);
            if (targetType == typeof(long))
                return Convert.ToInt64(value, CultureInfo.InvariantCulture);
            if (targetType == typeof(double))
                return Convert.ToDouble(value, CultureInfo.InvariantCulture);
            if (targetType == typeof(decimal))
                return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
            if (targetType == typeof(float))
                return Convert.ToSingle(value, CultureInfo.InvariantCulture);
            
            // DateTime conversion
            if (targetType == typeof(DateTime))
                return DateTime.Parse(value.ToString(), CultureInfo.InvariantCulture);
            
            // TimeSpan conversion
            if (targetType == typeof(TimeSpan))
                return TimeSpan.Parse(value.ToString(), CultureInfo.InvariantCulture);
            
            // Enum conversion
            if (targetType.IsEnum)
                return Enum.Parse(targetType, value.ToString(), true);
            
            // Array conversion
            if (targetType.IsArray && value is Array sourceArray)
            {
                var elementType = targetType.GetElementType();
                var result = Array.CreateInstance(elementType, sourceArray.Length);
                
                for (int i = 0; i < sourceArray.Length; i++)
                {
                    result.SetValue(ConvertValue(sourceArray.GetValue(i), elementType), i);
                }
                
                return result;
            }
            
            // Try using Convert.ChangeType as fallback
            try
            {
                return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
            }
            catch
            {
                return GetDefaultValue(targetType);
            }
        }
        
        /// <summary>
        /// Convert value to boolean
        /// </summary>
        private bool ConvertToBoolean(object value)
        {
            if (value is bool boolValue)
                return boolValue;
            
            if (value is string stringValue)
            {
                return stringValue.ToLower() switch
                {
                    "true" or "yes" or "1" or "on" or "enabled" => true,
                    "false" or "no" or "0" or "off" or "disabled" => false,
                    _ => !string.IsNullOrEmpty(stringValue)
                };
            }
            
            if (value is int intValue)
                return intValue != 0;
            
            if (value is double doubleValue)
                return doubleValue != 0.0;
            
            return value != null;
        }
        
        /// <summary>
        /// Get default value for type
        /// </summary>
        private object GetDefaultValue(Type type)
        {
            if (type.IsValueType)
                return Activator.CreateInstance(type);
            
            return null;
        }
        
        /// <summary>
        /// Get all keys recursively
        /// </summary>
        private List<string> GetAllKeysRecursive(string prefix, Dictionary<string, object> dict)
        {
            var keys = new List<string>();
            
            foreach (var kvp in dict)
            {
                var key = string.IsNullOrEmpty(prefix) ? kvp.Key : $"{prefix}.{kvp.Key}";
                keys.Add(key);
                
                if (kvp.Value is Dictionary<string, object> nestedDict)
                {
                    keys.AddRange(GetAllKeysRecursive(key, nestedDict));
                }
            }
            
            return keys;
        }
        
        /// <summary>
        /// Count keys recursively
        /// </summary>
        private int CountKeysRecursive(Dictionary<string, object> dict)
        {
            int count = dict.Count;
            
            foreach (var value in dict.Values)
            {
                if (value is Dictionary<string, object> nestedDict)
                {
                    count += CountKeysRecursive(nestedDict);
                }
            }
            
            return count;
        }
        
        /// <summary>
        /// Count sections in dictionary
        /// </summary>
        private int CountSections(Dictionary<string, object> dict)
        {
            int count = 0;
            
            foreach (var value in dict.Values)
            {
                if (value is Dictionary<string, object> nestedDict)
                {
                    count += 1 + CountSections(nestedDict);
                }
            }
            
            return count;
        }
        
        /// <summary>
        /// Flatten dictionary with specified separator
        /// </summary>
        private Dictionary<string, object> FlattenDictionary(Dictionary<string, object> dict, string prefix, string separator)
        {
            var result = new Dictionary<string, object>();
            
            foreach (var kvp in dict)
            {
                var key = string.IsNullOrEmpty(prefix) ? kvp.Key : $"{prefix}{separator}{kvp.Key}";
                
                if (kvp.Value is Dictionary<string, object> nestedDict)
                {
                    var nestedFlattened = FlattenDictionary(nestedDict, key, separator);
                    foreach (var nestedKvp in nestedFlattened)
                    {
                        result[nestedKvp.Key] = nestedKvp.Value;
                    }
                }
                else
                {
                    result[key] = kvp.Value;
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Merge dictionaries
        /// </summary>
        private void MergeDictionaries(Dictionary<string, object> target, Dictionary<string, object> source)
        {
            foreach (var kvp in source)
            {
                if (target.TryGetValue(kvp.Key, out var existingValue) &&
                    existingValue is Dictionary<string, object> existingDict &&
                    kvp.Value is Dictionary<string, object> sourceDict)
                {
                    // Recursively merge nested dictionaries
                    MergeDictionaries(existingDict, sourceDict);
                }
                else
                {
                    // Override or add new value
                    target[kvp.Key] = kvp.Value;
                }
            }
        }
        
        /// <summary>
        /// Clone dictionary deeply
        /// </summary>
        private Dictionary<string, object> CloneDictionary(Dictionary<string, object> original)
        {
            var clone = new Dictionary<string, object>();
            
            foreach (var kvp in original)
            {
                if (kvp.Value is Dictionary<string, object> nestedDict)
                {
                    clone[kvp.Key] = CloneDictionary(nestedDict);
                }
                else if (kvp.Value is Array array)
                {
                    var clonedArray = Array.CreateInstance(array.GetType().GetElementType(), array.Length);
                    Array.Copy(array, clonedArray, array.Length);
                    clone[kvp.Key] = clonedArray;
                }
                else
                {
                    clone[kvp.Key] = kvp.Value;
                }
            }
            
            return clone;
        }
        
        /// <summary>
        /// Validate basic structure
        /// </summary>
        private void ValidateBasicStructure(ConfigurationValidationResult result)
        {
            // Check for null values that might be problematic
            var nullKeys = GetAllKeysRecursive("", _settings)
                .Where(key => GetValue(key) == null)
                .ToList();
            
            if (nullKeys.Any())
            {
                result.Warnings.Add($"Found {nullKeys.Count} null values: {string.Join(", ", nullKeys.Take(5))}");
            }
            
            // Check for empty strings
            var emptyStringKeys = GetAllKeysRecursive("", _settings)
                .Where(key => GetValue(key) is string str && string.IsNullOrEmpty(str))
                .ToList();
            
            if (emptyStringKeys.Any())
            {
                result.Warnings.Add($"Found {emptyStringKeys.Count} empty string values");
            }
        }
        
        /// <summary>
        /// Throw if disposed
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(Configuration));
            }
        }
        
        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            lock (_lock)
            {
                if (!_isDisposed)
                {
                    _settings?.Clear();
                    _cache?.Clear();
                    _isDisposed = true;
                }
            }
        }
    }
    
    /// <summary>
    /// Configuration statistics
    /// </summary>
    public class ConfigurationStatistics
    {
        public int TotalKeys { get; set; }
        public int TotalSections { get; set; }
        public int CachedValues { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        public string SourceFile { get; set; }
        public string Environment { get; set; }
        public bool HasProcessingResult { get; set; }
        
        public override string ToString()
        {
            return $"Keys: {TotalKeys}, Sections: {TotalSections}, Cached: {CachedValues}, " +
                   $"Processing: {ProcessingTime}, Source: {SourceFile}";
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
        
        public bool HasErrors => Errors?.Count > 0;
        public bool HasWarnings => Warnings?.Count > 0;
        
        public override string ToString()
        {
            var status = IsValid ? "Valid" : "Invalid";
            return $"{status} - Errors: {Errors?.Count ?? 0}, Warnings: {Warnings?.Count ?? 0}";
        }
    }
    
    /// <summary>
    /// Configuration schema for validation
    /// </summary>
    public class ConfigurationSchema
    {
        public Dictionary<string, ConfigurationFieldSchema> Fields { get; set; } = new Dictionary<string, ConfigurationFieldSchema>();
        public bool AllowAdditionalFields { get; set; } = true;
    }
    
    /// <summary>
    /// Configuration field schema
    /// </summary>
    public class ConfigurationFieldSchema
    {
        public Type Type { get; set; }
        public bool Required { get; set; }
        public object DefaultValue { get; set; }
        public string[] AllowedValues { get; set; }
        public object MinValue { get; set; }
        public object MaxValue { get; set; }
        public string Pattern { get; set; }
    }
} 