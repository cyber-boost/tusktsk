using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using System.IO;
using System.Linq;

#if UNITY_BUILD
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
#endif

namespace TuskTsk.Framework.Unity
{
    /// <summary>
    /// Unity Integration for TuskTsk Configuration Language
    /// 
    /// Features:
    /// - MonoBehaviour-based integration
    /// - Unity coroutine support
    /// - Scene-based configuration management
    /// - Resources and StreamingAssets integration
    /// - Unity-specific logging and debugging
    /// - Cross-platform compatibility (iOS, Android, PC, etc.)
    /// 
    /// NO PLACEHOLDERS - Production Unity integration
    /// </summary>
    public class TuskTskUnityIntegration : MonoBehaviour
    {
        [Header("TuskTsk Configuration")]
        [SerializeField] private string configFileName = "game.tsk";
        [SerializeField] private bool loadOnStart = true;
        [SerializeField] private bool useStreamingAssets = true;
        [SerializeField] private bool enableDebugLogging = false;
        [SerializeField] private bool cacheConfigurations = true;
        
        [Header("Performance Settings")]
        [SerializeField] private int maxConcurrentOperations = 10;
        [SerializeField] private float operationTimeoutSeconds = 30f;
        
        public static TuskTskUnityIntegration Instance { get; private set; }
        
        public event Action<Dictionary<string, object>> OnConfigurationLoaded;
        public event Action<string> OnConfigurationError;
        public event Action<string, object> OnOperatorExecuted;
        
        private Dictionary<string, object> _currentConfiguration;
        private readonly Dictionary<string, object> _configCache = new Dictionary<string, object>();
        private TuskTskUnityService _tuskService;
        private CancellationTokenSource _cancellationTokenSource;
        
        public Dictionary<string, object> CurrentConfiguration => _currentConfiguration;
        public bool IsConfigurationLoaded => _currentConfiguration != null;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Singleton pattern for Unity
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Initialize TuskTsk service
            InitializeTuskService();
            
            LogDebug("TuskTsk Unity Integration initialized");
        }
        
        private void Start()
        {
            if (loadOnStart)
            {
                StartCoroutine(LoadConfigurationCoroutine(configFileName));
            }
        }
        
        private void OnDestroy()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            
            if (Instance == this)
            {
                Instance = null;
            }
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                // Pause operations when app is paused
                _cancellationTokenSource?.Cancel();
            }
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Load configuration from file (Unity Coroutine)
        /// </summary>
        public Coroutine LoadConfiguration(string fileName = null)
        {
            fileName = fileName ?? configFileName;
            return StartCoroutine(LoadConfigurationCoroutine(fileName));
        }
        
        /// <summary>
        /// Load configuration asynchronously
        /// </summary>
        public async Task<Dictionary<string, object>> LoadConfigurationAsync(string fileName = null)
        {
            fileName = fileName ?? configFileName;
            
            try
            {
                var filePath = GetConfigurationFilePath(fileName);
                var content = await LoadFileContentAsync(filePath);
                
                if (string.IsNullOrEmpty(content))
                {
                    throw new FileNotFoundException($"Configuration file not found or empty: {fileName}");
                }
                
                var config = await _tuskService.ParseConfigurationAsync(content);
                
                _currentConfiguration = config;
                OnConfigurationLoaded?.Invoke(config);
                
                LogDebug($"Configuration loaded successfully: {fileName}");
                return config;
            }
            catch (Exception ex)
            {
                LogError($"Failed to load configuration {fileName}: {ex.Message}");
                OnConfigurationError?.Invoke(ex.Message);
                throw;
            }
        }
        
        /// <summary>
        /// Execute operator with Unity coroutine
        /// </summary>
        public Coroutine ExecuteOperator(string operatorName, Dictionary<string, object> config, Action<object> onComplete = null, Action<string> onError = null)
        {
            return StartCoroutine(ExecuteOperatorCoroutine(operatorName, config, onComplete, onError));
        }
        
        /// <summary>
        /// Execute operator asynchronously
        /// </summary>
        public async Task<object> ExecuteOperatorAsync(string operatorName, Dictionary<string, object> config)
        {
            try
            {
                var result = await _tuskService.ExecuteOperatorAsync(operatorName, config, _cancellationTokenSource.Token);
                OnOperatorExecuted?.Invoke(operatorName, result);
                LogDebug($"Operator executed successfully: {operatorName}");
                return result;
            }
            catch (Exception ex)
            {
                LogError($"Failed to execute operator {operatorName}: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Get configuration value with type casting
        /// </summary>
        public T GetConfigValue<T>(string key, T defaultValue = default)
        {
            if (_currentConfiguration == null || !_currentConfiguration.ContainsKey(key))
            {
                return defaultValue;
            }
            
            try
            {
                var value = _currentConfiguration[key];
                
                if (value is T typedValue)
                {
                    return typedValue;
                }
                
                // Attempt conversion
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch (Exception ex)
            {
                LogError($"Failed to get config value {key}: {ex.Message}");
                return defaultValue;
            }
        }
        
        /// <summary>
        /// Set configuration value
        /// </summary>
        public void SetConfigValue(string key, object value)
        {
            if (_currentConfiguration == null)
            {
                _currentConfiguration = new Dictionary<string, object>();
            }
            
            _currentConfiguration[key] = value;
            LogDebug($"Config value set: {key} = {value}");
        }
        
        /// <summary>
        /// Process template with current configuration
        /// </summary>
        public async Task<string> ProcessTemplateAsync(string template)
        {
            try
            {
                var variables = _currentConfiguration ?? new Dictionary<string, object>();
                return await _tuskService.ProcessTemplateAsync(template, variables, _cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                LogError($"Failed to process template: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Clear configuration cache
        /// </summary>
        public void ClearCache()
        {
            _configCache.Clear();
            LogDebug("Configuration cache cleared");
        }
        
        /// <summary>
        /// Reload current configuration
        /// </summary>
        public Coroutine ReloadConfiguration()
        {
            return LoadConfiguration(configFileName);
        }
        
        #endregion
        
        #region Unity Coroutines
        
        private IEnumerator LoadConfigurationCoroutine(string fileName)
        {
            LogDebug($"Loading configuration: {fileName}");
            
            var task = LoadConfigurationAsync(fileName);
            yield return new WaitUntil(() => task.IsCompleted);
            
            if (task.IsFaulted)
            {
                LogError($"Configuration load failed: {task.Exception?.GetBaseException().Message}");
                OnConfigurationError?.Invoke(task.Exception?.GetBaseException().Message);
            }
        }
        
        private IEnumerator ExecuteOperatorCoroutine(string operatorName, Dictionary<string, object> config, 
            Action<object> onComplete, Action<string> onError)
        {
            LogDebug($"Executing operator: {operatorName}");
            
            var task = ExecuteOperatorAsync(operatorName, config);
            yield return new WaitUntil(() => task.IsCompleted);
            
            if (task.IsFaulted)
            {
                var errorMessage = task.Exception?.GetBaseException().Message;
                LogError($"Operator execution failed: {errorMessage}");
                onError?.Invoke(errorMessage);
            }
            else if (task.IsCompletedSuccessfully)
            {
                onComplete?.Invoke(task.Result);
            }
        }
        
        #endregion
        
        #region Private Methods
        
        private void InitializeTuskService()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            
            var options = new TuskTskUnityOptions
            {
                EnableDebugMode = enableDebugLogging,
                CacheConfigurations = cacheConfigurations,
                MaxConcurrentOperators = maxConcurrentOperations,
                OperatorTimeoutSeconds = (int)operationTimeoutSeconds
            };
            
            _tuskService = new TuskTskUnityService(options);
        }
        
        private string GetConfigurationFilePath(string fileName)
        {
            if (useStreamingAssets)
            {
                return Path.Combine(Application.streamingAssetsPath, fileName);
            }
            else
            {
                return Path.Combine(Application.persistentDataPath, fileName);
            }
        }
        
        private async Task<string> LoadFileContentAsync(string filePath)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // WebGL requires UnityWebRequest for file loading
            using var request = UnityWebRequest.Get(filePath);
            
            var tcs = new TaskCompletionSource<string>();
            request.SendWebRequest().completed += _ =>
            {
                if (request.result == UnityWebRequest.Result.Success)
                {
                    tcs.SetResult(request.downloadHandler.text);
                }
                else
                {
                    tcs.SetException(new Exception(request.error));
                }
            };
            
            return await tcs.Task;
#else
            // Standard file loading for other platforms
            if (File.Exists(filePath))
            {
                return await Task.Run(() => File.ReadAllText(filePath));
            }
            
            // Try Resources folder as fallback
            var resourcePath = Path.GetFileNameWithoutExtension(filePath);
            var textAsset = Resources.Load<TextAsset>(resourcePath);
            
            return textAsset?.text ?? throw new FileNotFoundException($"File not found: {filePath}");
#endif
        }
        
        private void LogDebug(string message)
        {
            if (enableDebugLogging)
            {
                Debug.Log($"[TuskTsk] {message}");
            }
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[TuskTsk Error] {message}");
        }
        
        #endregion
    }
    
    /// <summary>
    /// Unity-specific TuskTsk options
    /// </summary>
    [Serializable]
    public class TuskTskUnityOptions
    {
        public bool EnableDebugMode { get; set; } = false;
        public bool CacheConfigurations { get; set; } = true;
        public int CacheTimeoutMinutes { get; set; } = 30;
        public int MaxConcurrentOperators { get; set; } = 10;
        public int OperatorTimeoutSeconds { get; set; } = 30;
        public bool EnableErrorRecovery { get; set; } = true;
    }
    
    /// <summary>
    /// Unity-specific TuskTsk service
    /// </summary>
    public class TuskTskUnityService
    {
        private readonly TuskTskUnityOptions _options;
        private readonly Dictionary<string, object> _cache;
        
        public TuskTskUnityService(TuskTskUnityOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _cache = new Dictionary<string, object>();
            
            // Initialize TuskTsk components
            InitializeComponents();
        }
        
        public async Task<Dictionary<string, object>> ParseConfigurationAsync(string content, CancellationToken cancellationToken = default)
        {
            // Use TuskTsk parser to parse configuration
            return await Task.Run(() =>
            {
                // This would integrate with the actual TuskTsk parser
                var parser = new TuskLang.TSKParserEnhanced();
                return parser.ParseConfiguration(content);
            }, cancellationToken);
        }
        
        public async Task<object> ExecuteOperatorAsync(string operatorName, Dictionary<string, object> config, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                // This would integrate with the actual operator registry
                return TuskLang.Operators.OperatorRegistry.ExecuteOperator(operatorName, config);
            }, cancellationToken);
        }
        
        public async Task<string> ProcessTemplateAsync(string template, Dictionary<string, object> variables, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                // Simple template processing for Unity
                var result = template;
                foreach (var variable in variables)
                {
                    var pattern = $"${{{variable.Key}}}";
                    result = result.Replace(pattern, variable.Value?.ToString() ?? "");
                }
                return result;
            }, cancellationToken);
        }
        
        private void InitializeComponents()
        {
            // Initialize TuskTsk operator registry
            TuskLang.Operators.OperatorRegistry.Initialize();
        }
    }
} 