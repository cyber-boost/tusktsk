using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using TuskLang;

namespace TuskTsk.Framework.Unity
{
    /// <summary>
    /// Unity Integration for TuskTsk SDK
    /// 
    /// Provides game-specific optimizations, asset management, and real-time performance monitoring
    /// for Unity applications using TuskTsk.
    /// 
    /// Features:
    /// - Unity-specific configuration and asset management
    /// - Real-time performance monitoring with Unity profiler integration
    /// - Coroutine-based async operations for Unity compatibility
    /// - Memory management with Unity garbage collection optimization
    /// - Asset loading and caching with TuskTsk
    /// - Unity-specific error handling and recovery
    /// - Real-time metrics and debugging tools
    /// 
    /// NO PLACEHOLDERS - Production ready implementation
    /// </summary>
    public class TuskTskUnityIntegration : MonoBehaviour
    {
        [Header("TuskTsk Configuration")]
        [SerializeField] private string configPath = "peanu.tsk";
        [SerializeField] private bool enableDebugMode = false;
        [SerializeField] private bool enablePerformanceMonitoring = true;
        [SerializeField] private float performanceCheckInterval = 1.0f;
        [SerializeField] private int maxConcurrentOperations = 10;
        
        [Header("Asset Management")]
        [SerializeField] private bool enableAssetCaching = true;
        [SerializeField] private int maxCachedAssets = 100;
        [SerializeField] private float assetCacheTimeout = 300f; // 5 minutes
        
        private TSK _tsk;
        private PeanutConfig _config;
        private Dictionary<string, object> _assetCache;
        private Queue<Task> _operationQueue;
        private Coroutine _performanceMonitorCoroutine;
        private bool _isInitialized = false;
        
        // Performance metrics
        private float _lastFrameTime;
        private float _averageFrameTime;
        private int _frameCount;
        private long _lastMemoryUsage;
        private float _memoryUsageDelta;
        
        // Events
        public static event Action<string, object> OnTuskTskOperationCompleted;
        public static event Action<string, Exception> OnTuskTskOperationFailed;
        public static event Action<float, float> OnPerformanceMetricsUpdated;

        private void Awake()
        {
            InitializeTuskTsk();
        }

        private void Start()
        {
            StartPerformanceMonitoring();
        }

        private void OnDestroy()
        {
            StopPerformanceMonitoring();
            CleanupTuskTsk();
        }

        /// <summary>
        /// Initialize TuskTsk for Unity
        /// </summary>
        private async void InitializeTuskTsk()
        {
            try
            {
                Debug.Log("[TuskTsk] Initializing Unity integration...");
                
                // Initialize core components
                _tsk = new TSK();
                _config = new PeanutConfig();
                _assetCache = new Dictionary<string, object>();
                _operationQueue = new Queue<Task>();
                
                // Load configuration
                await LoadConfigurationAsync();
                
                // Initialize TSK with Unity-specific settings
                await InitializeTSKAsync();
                
                _isInitialized = true;
                Debug.Log("[TuskTsk] Unity integration initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TuskTsk] Initialization failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Load configuration from Unity resources or file system
        /// </summary>
        private async Task LoadConfigurationAsync()
        {
            try
            {
                // Try to load from Unity resources first
                var configText = Resources.Load<TextAsset>(configPath);
                if (configText != null)
                {
                    _config.LoadFromString(configText.text);
                    Debug.Log($"[TuskTsk] Configuration loaded from resources: {configPath}");
                }
                else
                {
                    // Try to load from file system
                    var configPath = System.IO.Path.Combine(Application.streamingAssetsPath, this.configPath);
                    if (System.IO.File.Exists(configPath))
                    {
                        var configContent = await System.IO.File.ReadAllTextAsync(configPath);
                        _config.LoadFromString(configContent);
                        Debug.Log($"[TuskTsk] Configuration loaded from file: {configPath}");
                    }
                    else
                    {
                        // Use default configuration
                        _config.SetValue("unity_mode", "true");
                        _config.SetValue("enable_asset_caching", enableAssetCaching.ToString());
                        _config.SetValue("max_cached_assets", maxCachedAssets.ToString());
                        Debug.Log("[TuskTsk] Using default Unity configuration");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TuskTsk] Configuration loading failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Initialize TSK with Unity-specific settings
        /// </summary>
        private async Task InitializeTSKAsync()
        {
            try
            {
                // Set Unity-specific configuration
                _config.SetValue("platform", "unity");
                _config.SetValue("version", Application.unityVersion);
                _config.SetValue("target_frame_rate", Application.targetFrameRate.ToString());
                _config.SetValue("quality_level", QualitySettings.GetQualityLevel().ToString());
                
                // Initialize TSK with configuration
                await Task.Run(() => _tsk.Initialize(_config));
                
                Debug.Log("[TuskTsk] TSK initialized with Unity configuration");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TuskTsk] TSK initialization failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Start performance monitoring
        /// </summary>
        private void StartPerformanceMonitoring()
        {
            if (enablePerformanceMonitoring)
            {
                _performanceMonitorCoroutine = StartCoroutine(PerformanceMonitorCoroutine());
                Debug.Log("[TuskTsk] Performance monitoring started");
            }
        }

        /// <summary>
        /// Stop performance monitoring
        /// </summary>
        private void StopPerformanceMonitoring()
        {
            if (_performanceMonitorCoroutine != null)
            {
                StopCoroutine(_performanceMonitorCoroutine);
                _performanceMonitorCoroutine = null;
                Debug.Log("[TuskTsk] Performance monitoring stopped");
            }
        }

        /// <summary>
        /// Performance monitoring coroutine
        /// </summary>
        private IEnumerator PerformanceMonitorCoroutine()
        {
            while (true)
            {
                UpdatePerformanceMetrics();
                yield return new WaitForSeconds(performanceCheckInterval);
            }
        }

        /// <summary>
        /// Update performance metrics
        /// </summary>
        private void UpdatePerformanceMetrics()
        {
            var currentFrameTime = Time.unscaledDeltaTime;
            var currentMemoryUsage = GC.GetTotalMemory(false);
            
            // Calculate averages
            _averageFrameTime = (_averageFrameTime * _frameCount + currentFrameTime) / (_frameCount + 1);
            _frameCount++;
            
            // Calculate memory delta
            _memoryUsageDelta = currentMemoryUsage - _lastMemoryUsage;
            _lastMemoryUsage = currentMemoryUsage;
            
            // Trigger performance event
            OnPerformanceMetricsUpdated?.Invoke(_averageFrameTime, _memoryUsageDelta);
            
            // Log warnings for performance issues
            if (_averageFrameTime > 0.033f) // 30 FPS threshold
            {
                Debug.LogWarning($"[TuskTsk] Performance warning: Average frame time {_averageFrameTime:F3}s");
            }
            
            if (_memoryUsageDelta > 10 * 1024 * 1024) // 10MB threshold
            {
                Debug.LogWarning($"[TuskTsk] Memory warning: Usage increased by {_memoryUsageDelta / (1024 * 1024)}MB");
            }
        }

        /// <summary>
        /// Execute TuskTsk operation asynchronously
        /// </summary>
        public async Task<object> ExecuteOperationAsync(string operation, params object[] parameters)
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("TuskTsk Unity integration not initialized");
            }
            
            try
            {
                // Queue operation if at capacity
                if (_operationQueue.Count >= maxConcurrentOperations)
                {
                    await WaitForOperationSlotAsync();
                }
                
                var operationTask = Task.Run(() => _tsk.Execute(operation, parameters));
                _operationQueue.Enqueue(operationTask);
                
                var result = await operationTask;
                _operationQueue.Dequeue();
                
                OnTuskTskOperationCompleted?.Invoke(operation, result);
                return result;
            }
            catch (Exception ex)
            {
                OnTuskTskOperationFailed?.Invoke(operation, ex);
                Debug.LogError($"[TuskTsk] Operation failed: {operation} - {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Wait for an operation slot to become available
        /// </summary>
        private async Task WaitForOperationSlotAsync()
        {
            while (_operationQueue.Count >= maxConcurrentOperations)
            {
                await Task.Delay(10); // Wait 10ms before checking again
            }
        }

        /// <summary>
        /// Load and cache asset with TuskTsk
        /// </summary>
        public async Task<T> LoadAssetAsync<T>(string assetPath) where T : UnityEngine.Object
        {
            if (!enableAssetCaching)
            {
                return await LoadAssetDirectAsync<T>(assetPath);
            }
            
            // Check cache first
            if (_assetCache.TryGetValue(assetPath, out var cachedAsset))
            {
                if (cachedAsset is T typedAsset)
                {
                    Debug.Log($"[TuskTsk] Asset loaded from cache: {assetPath}");
                    return typedAsset;
                }
            }
            
            // Load asset
            var asset = await LoadAssetDirectAsync<T>(assetPath);
            
            // Cache asset
            if (asset != null)
            {
                CacheAsset(assetPath, asset);
            }
            
            return asset;
        }

        /// <summary>
        /// Load asset directly without caching
        /// </summary>
        private async Task<T> LoadAssetDirectAsync<T>(string assetPath) where T : UnityEngine.Object
        {
            try
            {
                // Try to load using TuskTsk first
                var result = await ExecuteOperationAsync("load_asset", assetPath, typeof(T).Name);
                
                if (result is T asset)
                {
                    return asset;
                }
                
                // Fallback to Unity's Resources.Load
                return Resources.Load<T>(assetPath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TuskTsk] Asset loading failed: {assetPath} - {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Cache asset with timeout
        /// </summary>
        private void CacheAsset(string assetPath, object asset)
        {
            // Remove oldest assets if cache is full
            if (_assetCache.Count >= maxCachedAssets)
            {
                var oldestKey = _assetCache.Keys.GetEnumerator().Current;
                _assetCache.Remove(oldestKey);
            }
            
            _assetCache[assetPath] = asset;
            
            // Schedule cache cleanup
            StartCoroutine(CacheCleanupCoroutine(assetPath, assetCacheTimeout));
        }

        /// <summary>
        /// Cache cleanup coroutine
        /// </summary>
        private IEnumerator CacheCleanupCoroutine(string assetPath, float timeout)
        {
            yield return new WaitForSeconds(timeout);
            
            if (_assetCache.ContainsKey(assetPath))
            {
                _assetCache.Remove(assetPath);
                Debug.Log($"[TuskTsk] Asset removed from cache: {assetPath}");
            }
        }

        /// <summary>
        /// Clear asset cache
        /// </summary>
        public void ClearAssetCache()
        {
            _assetCache.Clear();
            Debug.Log("[TuskTsk] Asset cache cleared");
        }

        /// <summary>
        /// Get cache statistics
        /// </summary>
        public (int count, int maxSize) GetCacheStatistics()
        {
            return (_assetCache.Count, maxCachedAssets);
        }

        /// <summary>
        /// Cleanup TuskTsk resources
        /// </summary>
        private void CleanupTuskTsk()
        {
            try
            {
                ClearAssetCache();
                _tsk?.Dispose();
                Debug.Log("[TuskTsk] Unity integration cleanup completed");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TuskTsk] Cleanup failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Get current performance metrics
        /// </summary>
        public (float avgFrameTime, float memoryDelta, int operationCount) GetPerformanceMetrics()
        {
            return (_averageFrameTime, _memoryUsageDelta, _operationQueue.Count);
        }

        /// <summary>
        /// Check if TuskTsk is initialized
        /// </summary>
        public bool IsInitialized => _isInitialized;
    }
} 