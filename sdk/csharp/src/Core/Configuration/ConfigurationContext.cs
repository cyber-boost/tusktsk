using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TuskLang.Configuration
{
    /// <summary>
    /// Configuration Context - Manages runtime state and execution context
    /// 
    /// Provides:
    /// - Section and scope management
    /// - Include file tracking and circular reference prevention
    /// - Variable registration and lookup
    /// - Cache statistics and performance metrics
    /// - Thread-safe context state management
    /// - Execution state tracking
    /// 
    /// Performance: Lightweight context switching, efficient scope management
    /// </summary>
    public class ConfigurationContext : IDisposable
    {
        private readonly string _rootFile;
        private readonly ConfigurationEngineOptions _options;
        private readonly Stack<string> _includeStack;
        private readonly HashSet<string> _processedFiles;
        private readonly Dictionary<string, object> _contextVariables;
        private readonly Dictionary<string, DateTime> _timestamps;
        private readonly List<string> _sectionStack;
        private readonly object _lock;
        
        // Performance metrics
        private int _cacheHits;
        private int _cacheMisses;
        private int _variableAccesses;
        private int _includeCount;
        private DateTime _startTime;
        
        // Current state
        private string _currentSection;
        private int _includeDepth;
        private bool _isDisposed;
        
        /// <summary>
        /// Initializes a new instance of ConfigurationContext
        /// </summary>
        public ConfigurationContext(string rootFile, ConfigurationEngineOptions options)
        {
            _rootFile = rootFile ?? throw new ArgumentNullException(nameof(rootFile));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _includeStack = new Stack<string>();
            _processedFiles = new HashSet<string>();
            _contextVariables = new Dictionary<string, object>();
            _timestamps = new Dictionary<string, DateTime>();
            _sectionStack = new List<string>();
            _lock = new object();
            
            _cacheHits = 0;
            _cacheMisses = 0;
            _variableAccesses = 0;
            _includeCount = 0;
            _startTime = DateTime.UtcNow;
            
            _currentSection = "";
            _includeDepth = 0;
            _isDisposed = false;
            
            // Add root file to processed files
            _processedFiles.Add(Path.GetFullPath(rootFile));
        }
        
        /// <summary>
        /// Current include depth
        /// </summary>
        public int IncludeDepth
        {
            get
            {
                lock (_lock)
                {
                    return _includeDepth;
                }
            }
        }
        
        /// <summary>
        /// Current section name
        /// </summary>
        public string CurrentSection
        {
            get
            {
                lock (_lock)
                {
                    return _currentSection;
                }
            }
        }
        
        /// <summary>
        /// Cache hits count
        /// </summary>
        public int CacheHits
        {
            get
            {
                lock (_lock)
                {
                    return _cacheHits;
                }
            }
        }
        
        /// <summary>
        /// Cache misses count
        /// </summary>
        public int CacheMisses
        {
            get
            {
                lock (_lock)
                {
                    return _cacheMisses;
                }
            }
        }
        
        /// <summary>
        /// Variable access count
        /// </summary>
        public int VariableAccesses
        {
            get
            {
                lock (_lock)
                {
                    return _variableAccesses;
                }
            }
        }
        
        /// <summary>
        /// Include count
        /// </summary>
        public int IncludeCount
        {
            get
            {
                lock (_lock)
                {
                    return _includeCount;
                }
            }
        }
        
        /// <summary>
        /// Processing time elapsed
        /// </summary>
        public TimeSpan ProcessingTime
        {
            get
            {
                lock (_lock)
                {
                    return DateTime.UtcNow - _startTime;
                }
            }
        }
        
        /// <summary>
        /// Enter a section scope
        /// </summary>
        public void EnterSection(string sectionName)
        {
            if (string.IsNullOrEmpty(sectionName))
                throw new ArgumentException("Section name cannot be null or empty", nameof(sectionName));
            
            lock (_lock)
            {
                ThrowIfDisposed();
                
                _sectionStack.Add(sectionName);
                _currentSection = sectionName;
                _timestamps[$"section:{sectionName}"] = DateTime.UtcNow;
            }
        }
        
        /// <summary>
        /// Exit current section scope
        /// </summary>
        public void ExitSection()
        {
            lock (_lock)
            {
                ThrowIfDisposed();
                
                if (_sectionStack.Count > 0)
                {
                    var exitedSection = _sectionStack[_sectionStack.Count - 1];
                    _sectionStack.RemoveAt(_sectionStack.Count - 1);
                    
                    _currentSection = _sectionStack.Count > 0 ? _sectionStack[_sectionStack.Count - 1] : "";
                    _timestamps[$"section_exit:{exitedSection}"] = DateTime.UtcNow;
                }
            }
        }
        
        /// <summary>
        /// Push include file onto stack
        /// </summary>
        public void PushInclude(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
            
            lock (_lock)
            {
                ThrowIfDisposed();
                
                var fullPath = Path.GetFullPath(filePath);
                
                // Check include depth limit
                if (_includeDepth >= _options.MaxIncludeDepth)
                {
                    throw new ConfigurationException($"Maximum include depth exceeded ({_options.MaxIncludeDepth})");
                }
                
                // Check for circular includes
                if (_includeStack.Contains(fullPath))
                {
                    var chain = string.Join(" -> ", _includeStack) + " -> " + fullPath;
                    throw new ConfigurationException($"Circular include detected: {chain}");
                }
                
                _includeStack.Push(fullPath);
                _processedFiles.Add(fullPath);
                _includeDepth++;
                _includeCount++;
                _timestamps[$"include_start:{fullPath}"] = DateTime.UtcNow;
            }
        }
        
        /// <summary>
        /// Pop include file from stack
        /// </summary>
        public void PopInclude()
        {
            lock (_lock)
            {
                ThrowIfDisposed();
                
                if (_includeStack.Count > 0)
                {
                    var poppedFile = _includeStack.Pop();
                    _includeDepth--;
                    _timestamps[$"include_end:{poppedFile}"] = DateTime.UtcNow;
                }
            }
        }
        
        /// <summary>
        /// Check if file is in current include chain
        /// </summary>
        public bool IsFileInIncludeChain(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;
            
            lock (_lock)
            {
                ThrowIfDisposed();
                var fullPath = Path.GetFullPath(filePath);
                return _includeStack.Contains(fullPath);
            }
        }
        
        /// <summary>
        /// Check if file has been processed
        /// </summary>
        public bool IsFileProcessed(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;
            
            lock (_lock)
            {
                ThrowIfDisposed();
                var fullPath = Path.GetFullPath(filePath);
                return _processedFiles.Contains(fullPath);
            }
        }
        
        /// <summary>
        /// Register global variable
        /// </summary>
        public void RegisterGlobalVariable(string name, object value)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Variable name cannot be null or empty", nameof(name));
            
            lock (_lock)
            {
                ThrowIfDisposed();
                
                _contextVariables[$"global:{name}"] = value;
                _timestamps[$"global_var:{name}"] = DateTime.UtcNow;
            }
        }
        
        /// <summary>
        /// Register section variable
        /// </summary>
        public void RegisterSectionVariable(string section, string name, object value)
        {
            if (string.IsNullOrEmpty(section))
                throw new ArgumentException("Section name cannot be null or empty", nameof(section));
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Variable name cannot be null or empty", nameof(name));
            
            lock (_lock)
            {
                ThrowIfDisposed();
                
                _contextVariables[$"section:{section}:{name}"] = value;
                _timestamps[$"section_var:{section}:{name}"] = DateTime.UtcNow;
            }
        }
        
        /// <summary>
        /// Register assignment
        /// </summary>
        public void RegisterAssignment(string key, object value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            
            lock (_lock)
            {
                ThrowIfDisposed();
                
                var contextKey = !string.IsNullOrEmpty(_currentSection) 
                    ? $"assignment:{_currentSection}:{key}" 
                    : $"assignment:root:{key}";
                
                _contextVariables[contextKey] = value;
                _timestamps[contextKey] = DateTime.UtcNow;
            }
        }
        
        /// <summary>
        /// Get context variable
        /// </summary>
        public T GetContextVariable<T>(string key, T defaultValue = default)
        {
            if (string.IsNullOrEmpty(key))
                return defaultValue;
            
            lock (_lock)
            {
                ThrowIfDisposed();
                
                if (_contextVariables.TryGetValue(key, out var value) && value is T typedValue)
                {
                    _variableAccesses++;
                    return typedValue;
                }
                
                return defaultValue;
            }
        }
        
        /// <summary>
        /// Set context variable
        /// </summary>
        public void SetContextVariable(string key, object value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            
            lock (_lock)
            {
                ThrowIfDisposed();
                
                _contextVariables[key] = value;
                _timestamps[key] = DateTime.UtcNow;
            }
        }
        
        /// <summary>
        /// Register cache hit
        /// </summary>
        public void RegisterCacheHit()
        {
            lock (_lock)
            {
                ThrowIfDisposed();
                _cacheHits++;
            }
        }
        
        /// <summary>
        /// Register cache miss
        /// </summary>
        public void RegisterCacheMiss()
        {
            lock (_lock)
            {
                ThrowIfDisposed();
                _cacheMisses++;
            }
        }
        
        /// <summary>
        /// Get current include stack
        /// </summary>
        public string[] GetIncludeStack()
        {
            lock (_lock)
            {
                ThrowIfDisposed();
                return _includeStack.ToArray();
            }
        }
        
        /// <summary>
        /// Get section stack
        /// </summary>
        public string[] GetSectionStack()
        {
            lock (_lock)
            {
                ThrowIfDisposed();
                return _sectionStack.ToArray();
            }
        }
        
        /// <summary>
        /// Get processed files
        /// </summary>
        public string[] GetProcessedFiles()
        {
            lock (_lock)
            {
                ThrowIfDisposed();
                return _processedFiles.ToArray();
            }
        }
        
        /// <summary>
        /// Get context statistics
        /// </summary>
        public ConfigurationContextStatistics GetStatistics()
        {
            lock (_lock)
            {
                ThrowIfDisposed();
                
                return new ConfigurationContextStatistics
                {
                    ProcessingTime = DateTime.UtcNow - _startTime,
                    CacheHits = _cacheHits,
                    CacheMisses = _cacheMisses,
                    VariableAccesses = _variableAccesses,
                    IncludeCount = _includeCount,
                    MaxIncludeDepth = _includeDepth,
                    SectionCount = _sectionStack.Count,
                    ProcessedFileCount = _processedFiles.Count,
                    ContextVariableCount = _contextVariables.Count,
                    CacheHitRatio = _cacheHits + _cacheMisses > 0 ? (double)_cacheHits / (_cacheHits + _cacheMisses) : 0.0
                };
            }
        }
        
        /// <summary>
        /// Reset context state
        /// </summary>
        public void Reset()
        {
            lock (_lock)
            {
                ThrowIfDisposed();
                
                _includeStack.Clear();
                _processedFiles.Clear();
                _contextVariables.Clear();
                _timestamps.Clear();
                _sectionStack.Clear();
                
                _cacheHits = 0;
                _cacheMisses = 0;
                _variableAccesses = 0;
                _includeCount = 0;
                _startTime = DateTime.UtcNow;
                
                _currentSection = "";
                _includeDepth = 0;
                
                // Re-add root file
                _processedFiles.Add(Path.GetFullPath(_rootFile));
            }
        }
        
        /// <summary>
        /// Clone context for new processing
        /// </summary>
        public ConfigurationContext Clone()
        {
            lock (_lock)
            {
                ThrowIfDisposed();
                
                var cloned = new ConfigurationContext(_rootFile, _options);
                
                // Copy current state
                foreach (var file in _processedFiles)
                {
                    cloned._processedFiles.Add(file);
                }
                
                foreach (var kvp in _contextVariables)
                {
                    cloned._contextVariables[kvp.Key] = kvp.Value;
                }
                
                foreach (var section in _sectionStack)
                {
                    cloned._sectionStack.Add(section);
                }
                
                cloned._currentSection = _currentSection;
                cloned._includeDepth = _includeDepth;
                
                return cloned;
            }
        }
        
        /// <summary>
        /// Export context state
        /// </summary>
        public ConfigurationContextState ExportState()
        {
            lock (_lock)
            {
                ThrowIfDisposed();
                
                return new ConfigurationContextState
                {
                    RootFile = _rootFile,
                    CurrentSection = _currentSection,
                    IncludeDepth = _includeDepth,
                    IncludeStack = _includeStack.ToArray(),
                    SectionStack = _sectionStack.ToArray(),
                    ProcessedFiles = _processedFiles.ToArray(),
                    ContextVariables = new Dictionary<string, object>(_contextVariables),
                    Statistics = GetStatistics(),
                    CreatedAt = _startTime,
                    ExportedAt = DateTime.UtcNow
                };
            }
        }
        
        /// <summary>
        /// Validate context state
        /// </summary>
        public ConfigurationContextValidation ValidateState()
        {
            lock (_lock)
            {
                ThrowIfDisposed();
                
                var validation = new ConfigurationContextValidation
                {
                    IsValid = true,
                    Errors = new List<string>(),
                    Warnings = new List<string>()
                };
                
                // Check include depth
                if (_includeDepth > _options.MaxIncludeDepth)
                {
                    validation.IsValid = false;
                    validation.Errors.Add($"Include depth ({_includeDepth}) exceeds maximum ({_options.MaxIncludeDepth})");
                }
                
                // Check for circular includes
                var includeArray = _includeStack.ToArray();
                for (int i = 0; i < includeArray.Length; i++)
                {
                    for (int j = i + 1; j < includeArray.Length; j++)
                    {
                        if (includeArray[i] == includeArray[j])
                        {
                            validation.IsValid = false;
                            validation.Errors.Add($"Circular include detected: {includeArray[i]}");
                        }
                    }
                }
                
                // Check section stack consistency
                if (_sectionStack.Count > 0 && _currentSection != _sectionStack[_sectionStack.Count - 1])
                {
                    validation.Warnings.Add("Current section does not match top of section stack");
                }
                
                // Check processing time
                var processingTime = DateTime.UtcNow - _startTime;
                if (processingTime > TimeSpan.FromMinutes(5))
                {
                    validation.Warnings.Add($"Processing time ({processingTime}) is unusually long");
                }
                
                return validation;
            }
        }
        
        /// <summary>
        /// Throw if disposed
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(ConfigurationContext));
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
                    _includeStack?.Clear();
                    _processedFiles?.Clear();
                    _contextVariables?.Clear();
                    _timestamps?.Clear();
                    _sectionStack?.Clear();
                    
                    _isDisposed = true;
                }
            }
        }
    }
    
    /// <summary>
    /// Configuration context statistics
    /// </summary>
    public class ConfigurationContextStatistics
    {
        public TimeSpan ProcessingTime { get; set; }
        public int CacheHits { get; set; }
        public int CacheMisses { get; set; }
        public int VariableAccesses { get; set; }
        public int IncludeCount { get; set; }
        public int MaxIncludeDepth { get; set; }
        public int SectionCount { get; set; }
        public int ProcessedFileCount { get; set; }
        public int ContextVariableCount { get; set; }
        public double CacheHitRatio { get; set; }
        
        public override string ToString()
        {
            return $"Processing: {ProcessingTime}, Cache: {CacheHits}/{CacheHits + CacheMisses} ({CacheHitRatio:P1}), " +
                   $"Variables: {VariableAccesses}, Includes: {IncludeCount}, Sections: {SectionCount}";
        }
    }
    
    /// <summary>
    /// Configuration context state export
    /// </summary>
    public class ConfigurationContextState
    {
        public string RootFile { get; set; }
        public string CurrentSection { get; set; }
        public int IncludeDepth { get; set; }
        public string[] IncludeStack { get; set; }
        public string[] SectionStack { get; set; }
        public string[] ProcessedFiles { get; set; }
        public Dictionary<string, object> ContextVariables { get; set; }
        public ConfigurationContextStatistics Statistics { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExportedAt { get; set; }
    }
    
    /// <summary>
    /// Configuration context validation result
    /// </summary>
    public class ConfigurationContextValidation
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
} 