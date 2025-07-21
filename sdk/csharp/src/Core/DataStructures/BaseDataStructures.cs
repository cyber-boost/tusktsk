using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace TuskLang.Core
{
    /// <summary>
    /// Base Data Structures & APIs - Fundamental SDK Infrastructure
    /// 
    /// Provides the complete foundation for the TuskLang C# SDK:
    /// - Core interfaces and base classes for all SDK components
    /// - Memory management and resource pooling for high-performance scenarios
    /// - Thread-safe collections and concurrent data structures
    /// - Configuration and settings management infrastructure
    /// - Logging, monitoring, and diagnostics framework
    /// - Extension methods and utility functions
    /// - Plugin and extension system architecture
    /// - Performance optimization and caching infrastructure
    /// 
    /// Performance: Optimized for high-throughput, low-latency operations
    /// </summary>
    public static class BaseDataStructures
    {
        // Global configuration and settings
        private static readonly ConcurrentDictionary<string, object> _globalSettings;
        private static readonly ConcurrentDictionary<string, IDisposable> _resources;
        private static readonly object _initializationLock;
        private static bool _isInitialized;
        
        // Performance monitoring
        private static readonly PerformanceMonitor _performanceMonitor;
        private static readonly ResourcePool _resourcePool;
        private static readonly ExtensionManager _extensionManager;
        
        static BaseDataStructures()
        {
            _globalSettings = new ConcurrentDictionary<string, object>();
            _resources = new ConcurrentDictionary<string, IDisposable>();
            _initializationLock = new object();
            _isInitialized = false;
            
            _performanceMonitor = new PerformanceMonitor();
            _resourcePool = new ResourcePool();
            _extensionManager = new ExtensionManager();
        }
        
        /// <summary>
        /// Initialize the base data structures system
        /// </summary>
        public static void Initialize(BaseDataStructuresOptions options = null)
        {
            lock (_initializationLock)
            {
                if (_isInitialized) return;
                
                var opts = options ?? new BaseDataStructuresOptions();
                
                // Initialize core systems
                _performanceMonitor.Initialize(opts.PerformanceOptions);
                _resourcePool.Initialize(opts.ResourceOptions);
                _extensionManager.Initialize(opts.ExtensionOptions);
                
                // Set default settings
                SetGlobalSetting("sdk.version", "1.0.0");
                SetGlobalSetting("sdk.initialized", DateTime.UtcNow);
                SetGlobalSetting("performance.monitoring.enabled", opts.EnablePerformanceMonitoring);
                SetGlobalSetting("resource.pooling.enabled", opts.EnableResourcePooling);
                SetGlobalSetting("extension.system.enabled", opts.EnableExtensionSystem);
                
                _isInitialized = true;
            }
        }
        
        /// <summary>
        /// Get global setting
        /// </summary>
        public static T GetGlobalSetting<T>(string key, T defaultValue = default)
        {
            if (_globalSettings.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }
        
        /// <summary>
        /// Set global setting
        /// </summary>
        public static void SetGlobalSetting<T>(string key, T value)
        {
            _globalSettings.AddOrUpdate(key, value, (k, v) => value);
        }
        
        /// <summary>
        /// Register disposable resource
        /// </summary>
        public static void RegisterResource(string name, IDisposable resource)
        {
            _resources.AddOrUpdate(name, resource, (k, v) => 
            {
                v?.Dispose();
                return resource;
            });
        }
        
        /// <summary>
        /// Get registered resource
        /// </summary>
        public static T GetResource<T>(string name) where T : class, IDisposable
        {
            return _resources.TryGetValue(name, out var resource) ? resource as T : null;
        }
        
        /// <summary>
        /// Get performance monitor
        /// </summary>
        public static PerformanceMonitor PerformanceMonitor => _performanceMonitor;
        
        /// <summary>
        /// Get resource pool
        /// </summary>
        public static ResourcePool ResourcePool => _resourcePool;
        
        /// <summary>
        /// Get extension manager
        /// </summary>
        public static ExtensionManager ExtensionManager => _extensionManager;
        
        /// <summary>
        /// Shutdown and cleanup all resources
        /// </summary>
        public static void Shutdown()
        {
            lock (_initializationLock)
            {
                if (!_isInitialized) return;
                
                // Dispose all registered resources
                foreach (var resource in _resources.Values)
                {
                    try
                    {
                        resource?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue cleanup
                        System.Diagnostics.Debug.WriteLine($"Error disposing resource: {ex.Message}");
                    }
                }
                _resources.Clear();
                
                // Shutdown core systems
                _performanceMonitor?.Shutdown();
                _resourcePool?.Shutdown();
                _extensionManager?.Shutdown();
                
                _globalSettings.Clear();
                _isInitialized = false;
            }
        }
    }
    
    /// <summary>
    /// Performance monitoring system
    /// </summary>
    public class PerformanceMonitor : IDisposable
    {
        private readonly ConcurrentDictionary<string, PerformanceMetric> _metrics;
        private readonly ConcurrentDictionary<string, PerformanceCounter> _counters;
        private readonly List<IPerformanceObserver> _observers;
        private readonly object _lock;
        private bool _isInitialized;
        
        public PerformanceMonitor()
        {
            _metrics = new ConcurrentDictionary<string, PerformanceMetric>();
            _counters = new ConcurrentDictionary<string, PerformanceCounter>();
            _observers = new List<IPerformanceObserver>();
            _lock = new object();
            _isInitialized = false;
        }
        
        public void Initialize(PerformanceMonitorOptions options = null)
        {
            lock (_lock)
            {
                if (_isInitialized) return;
                
                var opts = options ?? new PerformanceMonitorOptions();
                
                // Initialize default metrics
                RegisterMetric("sdk.operations.total", "Total SDK operations");
                RegisterMetric("sdk.operations.success", "Successful operations");
                RegisterMetric("sdk.operations.failed", "Failed operations");
                RegisterMetric("sdk.memory.usage", "Memory usage in bytes");
                RegisterMetric("sdk.cpu.usage", "CPU usage percentage");
                
                // Initialize default counters
                RegisterCounter("sdk.requests", "Total requests processed");
                RegisterCounter("sdk.errors", "Total errors encountered");
                RegisterCounter("sdk.cache.hits", "Cache hits");
                RegisterCounter("sdk.cache.misses", "Cache misses");
                
                _isInitialized = true;
            }
        }
        
        public void RegisterMetric(string name, string description)
        {
            _metrics.TryAdd(name, new PerformanceMetric(name, description));
        }
        
        public void RegisterCounter(string name, string description)
        {
            _counters.TryAdd(name, new PerformanceCounter(name, description));
        }
        
        public void RecordMetric(string name, double value)
        {
            if (_metrics.TryGetValue(name, out var metric))
            {
                metric.RecordValue(value);
                NotifyObservers(name, value);
            }
        }
        
        public void IncrementCounter(string name, long increment = 1)
        {
            if (_counters.TryGetValue(name, out var counter))
            {
                counter.Increment(increment);
                NotifyObservers(name, increment);
            }
        }
        
        public PerformanceMetric GetMetric(string name)
        {
            return _metrics.TryGetValue(name, out var metric) ? metric : null;
        }
        
        public PerformanceCounter GetCounter(string name)
        {
            return _counters.TryGetValue(name, out var counter) ? counter : null;
        }
        
        public PerformanceReport GenerateReport()
        {
            return new PerformanceReport
            {
                Metrics = _metrics.Values.ToList(),
                Counters = _counters.Values.ToList(),
                GeneratedAt = DateTime.UtcNow
            };
        }
        
        public void AddObserver(IPerformanceObserver observer)
        {
            lock (_lock)
            {
                _observers.Add(observer);
            }
        }
        
        public void RemoveObserver(IPerformanceObserver observer)
        {
            lock (_lock)
            {
                _observers.Remove(observer);
            }
        }
        
        private void NotifyObservers(string name, double value)
        {
            lock (_lock)
            {
                foreach (var observer in _observers)
                {
                    try
                    {
                        observer.OnMetricRecorded(name, value);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error notifying observer: {ex.Message}");
                    }
                }
            }
        }
        
        public void Shutdown()
        {
            lock (_lock)
            {
                _observers.Clear();
                _metrics.Clear();
                _counters.Clear();
                _isInitialized = false;
            }
        }
        
        public void Dispose()
        {
            Shutdown();
        }
    }
    
    /// <summary>
    /// Resource pooling system for high-performance scenarios
    /// </summary>
    public class ResourcePool : IDisposable
    {
        private readonly ConcurrentDictionary<Type, object> _pools;
        private readonly ConcurrentDictionary<string, object> _namedPools;
        private readonly object _lock;
        private bool _isInitialized;
        
        public ResourcePool()
        {
            _pools = new ConcurrentDictionary<Type, object>();
            _namedPools = new ConcurrentDictionary<string, object>();
            _lock = new object();
            _isInitialized = false;
        }
        
        public void Initialize(ResourcePoolOptions options = null)
        {
            lock (_lock)
            {
                if (_isInitialized) return;
                
                var opts = options ?? new ResourcePoolOptions();
                
                // Initialize default pools
                RegisterPool<MemoryStream>(() => new MemoryStream(), opts.DefaultPoolSize);
                RegisterPool<System.Text.StringBuilder>(() => new System.Text.StringBuilder(), opts.DefaultPoolSize);
                
                _isInitialized = true;
            }
        }
        
        public void RegisterPool<T>(Func<T> factory, int maxSize = 100) where T : class
        {
            var pool = new ObjectPool<T>(factory, maxSize);
            _pools.AddOrUpdate(typeof(T), pool, (k, v) => pool);
        }
        
        public void RegisterNamedPool<T>(string name, Func<T> factory, int maxSize = 100) where T : class
        {
            var pool = new ObjectPool<T>(factory, maxSize);
            _namedPools.AddOrUpdate(name, pool, (k, v) => pool);
        }
        
        public T Get<T>() where T : class
        {
            if (_pools.TryGetValue(typeof(T), out var pool) && pool is ObjectPool<T> typedPool)
            {
                return typedPool.Get();
            }
            return null;
        }
        
        public T GetNamed<T>(string name) where T : class
        {
            if (_namedPools.TryGetValue(name, out var pool) && pool is ObjectPool<T> typedPool)
            {
                return typedPool.Get();
            }
            return null;
        }
        
        public void Return<T>(T item) where T : class
        {
            if (_pools.TryGetValue(typeof(T), out var pool) && pool is ObjectPool<T> typedPool)
            {
                typedPool.Return(item);
            }
        }
        
        public void ReturnNamed<T>(string name, T item) where T : class
        {
            if (_namedPools.TryGetValue(name, out var pool) && pool is ObjectPool<T> typedPool)
            {
                typedPool.Return(item);
            }
        }
        
        public ResourcePoolStatistics GetStatistics()
        {
            return new ResourcePoolStatistics
            {
                TotalPools = _pools.Count + _namedPools.Count,
                TypePools = _pools.Count,
                NamedPools = _namedPools.Count,
                GeneratedAt = DateTime.UtcNow
            };
        }
        
        public void Shutdown()
        {
            lock (_lock)
            {
                _pools.Clear();
                _namedPools.Clear();
                _isInitialized = false;
            }
        }
        
        public void Dispose()
        {
            Shutdown();
        }
    }
    
    /// <summary>
    /// Extension management system
    /// </summary>
    public class ExtensionManager : IDisposable
    {
        private readonly ConcurrentDictionary<string, IExtension> _extensions;
        private readonly ConcurrentDictionary<string, ExtensionMetadata> _metadata;
        private readonly List<IExtensionObserver> _observers;
        private readonly object _lock;
        private bool _isInitialized;
        
        public ExtensionManager()
        {
            _extensions = new ConcurrentDictionary<string, IExtension>();
            _metadata = new ConcurrentDictionary<string, ExtensionMetadata>();
            _observers = new List<IExtensionObserver>();
            _lock = new object();
            _isInitialized = false;
        }
        
        public void Initialize(ExtensionManagerOptions options = null)
        {
            lock (_lock)
            {
                if (_isInitialized) return;
                
                var opts = options ?? new ExtensionManagerOptions();
                
                // Load extensions from configured directories
                if (opts.AutoLoadExtensions)
                {
                    LoadExtensionsFromDirectory(opts.ExtensionsDirectory);
                }
                
                _isInitialized = true;
            }
        }
        
        public void RegisterExtension(string name, IExtension extension, ExtensionMetadata metadata = null)
        {
            _extensions.AddOrUpdate(name, extension, (k, v) => extension);
            
            if (metadata != null)
            {
                _metadata.AddOrUpdate(name, metadata, (k, v) => metadata);
            }
            
            NotifyObservers(name, ExtensionEvent.Registered);
        }
        
        public void UnregisterExtension(string name)
        {
            if (_extensions.TryRemove(name, out var extension))
            {
                try
                {
                    extension?.Dispose();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error disposing extension {name}: {ex.Message}");
                }
                
                _metadata.TryRemove(name, out _);
                NotifyObservers(name, ExtensionEvent.Unregistered);
            }
        }
        
        public T GetExtension<T>(string name) where T : class, IExtension
        {
            return _extensions.TryGetValue(name, out var extension) ? extension as T : null;
        }
        
        public IEnumerable<IExtension> GetAllExtensions()
        {
            return _extensions.Values;
        }
        
        public ExtensionMetadata GetExtensionMetadata(string name)
        {
            return _metadata.TryGetValue(name, out var metadata) ? metadata : null;
        }
        
        public void AddObserver(IExtensionObserver observer)
        {
            lock (_lock)
            {
                _observers.Add(observer);
            }
        }
        
        public void RemoveObserver(IExtensionObserver observer)
        {
            lock (_lock)
            {
                _observers.Remove(observer);
            }
        }
        
        private void LoadExtensionsFromDirectory(string directory)
        {
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                return;
            
            // In a complete implementation, this would load extensions from DLL files
            // For now, this is a placeholder for the extension loading mechanism
        }
        
        private void NotifyObservers(string extensionName, ExtensionEvent evt)
        {
            lock (_lock)
            {
                foreach (var observer in _observers)
                {
                    try
                    {
                        observer.OnExtensionEvent(extensionName, evt);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error notifying extension observer: {ex.Message}");
                    }
                }
            }
        }
        
        public void Shutdown()
        {
            lock (_lock)
            {
                // Dispose all extensions
                foreach (var extension in _extensions.Values)
                {
                    try
                    {
                        extension?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error disposing extension: {ex.Message}");
                    }
                }
                
                _extensions.Clear();
                _metadata.Clear();
                _observers.Clear();
                _isInitialized = false;
            }
        }
        
        public void Dispose()
        {
            Shutdown();
        }
    }
    
    /// <summary>
    /// Object pool for efficient resource management
    /// </summary>
    public class ObjectPool<T> where T : class
    {
        private readonly ConcurrentQueue<T> _pool;
        private readonly Func<T> _factory;
        private readonly int _maxSize;
        private int _currentSize;
        
        public ObjectPool(Func<T> factory, int maxSize = 100)
        {
            _pool = new ConcurrentQueue<T>();
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _maxSize = maxSize;
            _currentSize = 0;
        }
        
        public T Get()
        {
            if (_pool.TryDequeue(out var item))
            {
                return item;
            }
            
            return _factory();
        }
        
        public void Return(T item)
        {
            if (item == null) return;
            
            if (_currentSize < _maxSize)
            {
                _pool.Enqueue(item);
                _currentSize++;
            }
        }
        
        public int CurrentSize => _currentSize;
        public int MaxSize => _maxSize;
    }
    
    /// <summary>
    /// Performance metric tracking
    /// </summary>
    public class PerformanceMetric
    {
        private readonly string _name;
        private readonly string _description;
        private readonly List<double> _values;
        private readonly object _lock;
        
        public PerformanceMetric(string name, string description)
        {
            _name = name;
            _description = description;
            _values = new List<double>();
            _lock = new object();
        }
        
        public string Name => _name;
        public string Description => _description;
        
        public void RecordValue(double value)
        {
            lock (_lock)
            {
                _values.Add(value);
                
                // Keep only recent values to prevent memory bloat
                if (_values.Count > 1000)
                {
                    _values.RemoveRange(0, _values.Count - 1000);
                }
            }
        }
        
        public PerformanceMetricSnapshot GetSnapshot()
        {
            lock (_lock)
            {
                if (_values.Count == 0)
                {
                    return new PerformanceMetricSnapshot
                    {
                        Name = _name,
                        Count = 0,
                        Min = 0,
                        Max = 0,
                        Average = 0,
                        Timestamp = DateTime.UtcNow
                    };
                }
                
                return new PerformanceMetricSnapshot
                {
                    Name = _name,
                    Count = _values.Count,
                    Min = _values.Min(),
                    Max = _values.Max(),
                    Average = _values.Average(),
                    Timestamp = DateTime.UtcNow
                };
            }
        }
    }
    
    /// <summary>
    /// Performance counter
    /// </summary>
    public class PerformanceCounter
    {
        private readonly string _name;
        private readonly string _description;
        private long _value;
        
        public PerformanceCounter(string name, string description)
        {
            _name = name;
            _description = description;
            _value = 0;
        }
        
        public string Name => _name;
        public string Description => _description;
        public long Value => _value;
        
        public void Increment(long increment = 1)
        {
            Interlocked.Add(ref _value, increment);
        }
        
        public void Reset()
        {
            Interlocked.Exchange(ref _value, 0);
        }
    }
    
    /// <summary>
    /// Performance metric snapshot
    /// </summary>
    public class PerformanceMetricSnapshot
    {
        public string Name { get; set; }
        public int Count { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public double Average { get; set; }
        public DateTime Timestamp { get; set; }
    }
    
    /// <summary>
    /// Performance report
    /// </summary>
    public class PerformanceReport
    {
        public List<PerformanceMetric> Metrics { get; set; }
        public List<PerformanceCounter> Counters { get; set; }
        public DateTime GeneratedAt { get; set; }
    }
    
    /// <summary>
    /// Resource pool statistics
    /// </summary>
    public class ResourcePoolStatistics
    {
        public int TotalPools { get; set; }
        public int TypePools { get; set; }
        public int NamedPools { get; set; }
        public DateTime GeneratedAt { get; set; }
    }
    
    /// <summary>
    /// Extension metadata
    /// </summary>
    public class ExtensionMetadata
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        public string Author { get; set; }
        public DateTime LoadedAt { get; set; }
    }
    
    /// <summary>
    /// Extension event types
    /// </summary>
    public enum ExtensionEvent
    {
        Registered,
        Unregistered,
        Enabled,
        Disabled
    }
    
    /// <summary>
    /// Base data structures options
    /// </summary>
    public class BaseDataStructuresOptions
    {
        public PerformanceMonitorOptions PerformanceOptions { get; set; } = new PerformanceMonitorOptions();
        public ResourcePoolOptions ResourceOptions { get; set; } = new ResourcePoolOptions();
        public ExtensionManagerOptions ExtensionOptions { get; set; } = new ExtensionManagerOptions();
        public bool EnablePerformanceMonitoring { get; set; } = true;
        public bool EnableResourcePooling { get; set; } = true;
        public bool EnableExtensionSystem { get; set; } = true;
    }
    
    /// <summary>
    /// Performance monitor options
    /// </summary>
    public class PerformanceMonitorOptions
    {
        public bool EnableMetrics { get; set; } = true;
        public bool EnableCounters { get; set; } = true;
        public int MaxMetricHistory { get; set; } = 1000;
    }
    
    /// <summary>
    /// Resource pool options
    /// </summary>
    public class ResourcePoolOptions
    {
        public int DefaultPoolSize { get; set; } = 100;
        public bool EnableAutoCleanup { get; set; } = true;
        public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(5);
    }
    
    /// <summary>
    /// Extension manager options
    /// </summary>
    public class ExtensionManagerOptions
    {
        public bool AutoLoadExtensions { get; set; } = false;
        public string ExtensionsDirectory { get; set; } = "extensions";
        public bool EnableHotReloading { get; set; } = false;
    }
    
    /// <summary>
    /// Performance observer interface
    /// </summary>
    public interface IPerformanceObserver
    {
        void OnMetricRecorded(string name, double value);
    }
    
    /// <summary>
    /// Extension interface
    /// </summary>
    public interface IExtension : IDisposable
    {
        string Name { get; }
        string Version { get; }
        void Initialize();
        void Shutdown();
    }
    
    /// <summary>
    /// Extension observer interface
    /// </summary>
    public interface IExtensionObserver
    {
        void OnExtensionEvent(string extensionName, ExtensionEvent evt);
    }
} 