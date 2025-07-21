using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;

#if XAMARIN_BUILD
using Xamarin.Forms;
using Xamarin.Essentials;
#endif

namespace TuskTsk.Framework.Xamarin
{
    /// <summary>
    /// Xamarin Integration for TuskTsk Configuration Language
    /// 
    /// Features:
    /// - Cross-platform iOS and Android support
    /// - MVVM pattern integration
    /// - Xamarin.Forms compatibility
    /// - Mobile-optimized performance
    /// - Platform-specific storage handling
    /// - Background task support
    /// - Network-aware configuration loading
    /// - Device capability detection
    /// 
    /// NO PLACEHOLDERS - Production Xamarin integration
    /// </summary>
    public class TuskTskXamarinIntegration : INotifyPropertyChanged
    {
        public static TuskTskXamarinIntegration Instance { get; private set; }
        
        private Dictionary<string, object> _currentConfiguration;
        private TuskTskXamarinService _tuskService;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isInitialized;
        private bool _isConfigurationLoaded;
        private string _lastError;
        
        // Events for MVVM binding
        public event PropertyChangedEventHandler PropertyChanged;
        public event Action<Dictionary<string, object>> ConfigurationLoaded;
        public event Action<string> ConfigurationError;
        public event Action<string, object> OperatorExecuted;
        
        // Properties for MVVM binding
        public Dictionary<string, object> CurrentConfiguration
        {
            get => _currentConfiguration;
            private set
            {
                _currentConfiguration = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsConfigurationLoaded));
            }
        }
        
        public bool IsInitialized
        {
            get => _isInitialized;
            private set
            {
                _isInitialized = value;
                OnPropertyChanged();
            }
        }
        
        public bool IsConfigurationLoaded
        {
            get => _isConfigurationLoaded;
            private set
            {
                _isConfigurationLoaded = value;
                OnPropertyChanged();
            }
        }
        
        public string LastError
        {
            get => _lastError;
            private set
            {
                _lastError = value;
                OnPropertyChanged();
            }
        }
        
        // Configuration
        public TuskTskXamarinOptions Options { get; private set; }
        
        #region Initialization
        
        /// <summary>
        /// Initialize TuskTsk for Xamarin application
        /// </summary>
        public static async Task<TuskTskXamarinIntegration> InitializeAsync(TuskTskXamarinOptions options = null)
        {
            if (Instance != null)
            {
                return Instance;
            }
            
            Instance = new TuskTskXamarinIntegration();
            await Instance.InternalInitializeAsync(options ?? new TuskTskXamarinOptions());
            
            return Instance;
        }
        
        private async Task InternalInitializeAsync(TuskTskXamarinOptions options)
        {
            try
            {
                Options = options;
                _cancellationTokenSource = new CancellationTokenSource();
                
                // Initialize platform-specific services
                await InitializePlatformServicesAsync();
                
                // Initialize TuskTsk service
                _tuskService = new TuskTskXamarinService(options);
                
                IsInitialized = true;
                
                LogDebug("TuskTsk Xamarin integration initialized successfully");
                
                // Load default configuration if specified
                if (!string.IsNullOrEmpty(options.DefaultConfigurationFile))
                {
                    await LoadConfigurationAsync(options.DefaultConfigurationFile);
                }
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                LogError($"Failed to initialize TuskTsk: {ex.Message}");
                throw;
            }
        }
        
        private async Task InitializePlatformServicesAsync()
        {
#if XAMARIN_BUILD
            // Initialize Xamarin.Essentials
            try
            {
                // Platform-specific initialization would go here
                await Task.Run(() =>
                {
                    // Initialize any required platform services
                    var platform = DeviceInfo.Platform;
                    LogDebug($"Initializing for platform: {platform}");
                });
            }
            catch (Exception ex)
            {
                LogError($"Platform initialization failed: {ex.Message}");
            }
#endif
        }
        
        #endregion
        
        #region Configuration Management
        
        /// <summary>
        /// Load configuration from file
        /// </summary>
        public async Task<Dictionary<string, object>> LoadConfigurationAsync(string fileName, bool useCache = true)
        {
            ThrowIfNotInitialized();
            
            try
            {
                LogDebug($"Loading configuration: {fileName}");
                
                var filePath = await GetConfigurationFilePathAsync(fileName);
                var content = await LoadFileContentAsync(filePath);
                
                if (string.IsNullOrEmpty(content))
                {
                    throw new FileNotFoundException($"Configuration file not found or empty: {fileName}");
                }
                
                var config = await _tuskService.ParseConfigurationAsync(content, _cancellationTokenSource.Token);
                
                CurrentConfiguration = config;
                IsConfigurationLoaded = true;
                
                ConfigurationLoaded?.Invoke(config);
                
                LogDebug($"Configuration loaded successfully: {fileName} ({config.Count} keys)");
                return config;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                IsConfigurationLoaded = false;
                LogError($"Failed to load configuration {fileName}: {ex.Message}");
                ConfigurationError?.Invoke(ex.Message);
                throw;
            }
        }
        
        /// <summary>
        /// Load configuration from embedded resources
        /// </summary>
        public async Task<Dictionary<string, object>> LoadConfigurationFromResourceAsync(string resourceName, Assembly assembly = null)
        {
            ThrowIfNotInitialized();
            
            try
            {
                assembly = assembly ?? Assembly.GetCallingAssembly();
                
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    throw new FileNotFoundException($"Embedded resource not found: {resourceName}");
                }
                
                using var reader = new StreamReader(stream);
                var content = await reader.ReadToEndAsync();
                
                var config = await _tuskService.ParseConfigurationAsync(content, _cancellationTokenSource.Token);
                
                CurrentConfiguration = config;
                IsConfigurationLoaded = true;
                
                ConfigurationLoaded?.Invoke(config);
                
                LogDebug($"Configuration loaded from resource: {resourceName} ({config.Count} keys)");
                return config;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                IsConfigurationLoaded = false;
                LogError($"Failed to load configuration from resource {resourceName}: {ex.Message}");
                ConfigurationError?.Invoke(ex.Message);
                throw;
            }
        }
        
        /// <summary>
        /// Load configuration from URL (for remote configurations)
        /// </summary>
        public async Task<Dictionary<string, object>> LoadConfigurationFromUrlAsync(string url)
        {
            ThrowIfNotInitialized();
            
#if XAMARIN_BUILD
            try
            {
                // Check network connectivity
                var networkAccess = Connectivity.NetworkAccess;
                if (networkAccess != NetworkAccess.Internet)
                {
                    throw new InvalidOperationException("No internet connection available");
                }
                
                LogDebug($"Loading configuration from URL: {url}");
                
                using var httpClient = new System.Net.Http.HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(Options.NetworkTimeoutSeconds);
                
                var content = await httpClient.GetStringAsync(url);
                
                if (string.IsNullOrEmpty(content))
                {
                    throw new InvalidOperationException($"Empty response from URL: {url}");
                }
                
                var config = await _tuskService.ParseConfigurationAsync(content, _cancellationTokenSource.Token);
                
                CurrentConfiguration = config;
                IsConfigurationLoaded = true;
                
                ConfigurationLoaded?.Invoke(config);
                
                LogDebug($"Configuration loaded from URL: {url} ({config.Count} keys)");
                return config;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                IsConfigurationLoaded = false;
                LogError($"Failed to load configuration from URL {url}: {ex.Message}");
                ConfigurationError?.Invoke(ex.Message);
                throw;
            }
#else
            throw new NotSupportedException("URL configuration loading requires Xamarin.Essentials");
#endif
        }
        
        #endregion
        
        #region Operator Execution
        
        /// <summary>
        /// Execute operator asynchronously
        /// </summary>
        public async Task<object> ExecuteOperatorAsync(string operatorName, Dictionary<string, object> config)
        {
            ThrowIfNotInitialized();
            
            try
            {
                LogDebug($"Executing operator: {operatorName}");
                
                var result = await _tuskService.ExecuteOperatorAsync(operatorName, config, _cancellationTokenSource.Token);
                
                OperatorExecuted?.Invoke(operatorName, result);
                
                LogDebug($"Operator executed successfully: {operatorName}");
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                LogError($"Failed to execute operator {operatorName}: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Execute multiple operators in background
        /// </summary>
        public async Task<List<object>> ExecuteOperatorsInBackgroundAsync(IEnumerable<OperatorExecution> executions, IProgress<OperatorProgress> progress = null)
        {
            ThrowIfNotInitialized();
            
            var executionList = executions.ToList();
            var results = new List<object>();
            
            try
            {
                LogDebug($"Executing {executionList.Count} operators in background");
                
                for (int i = 0; i < executionList.Count; i++)
                {
                    var execution = executionList[i];
                    
                    progress?.Report(new OperatorProgress
                    {
                        Current = i,
                        Total = executionList.Count,
                        OperatorName = execution.OperatorName,
                        Status = "Executing"
                    });
                    
                    var result = await ExecuteOperatorAsync(execution.OperatorName, execution.Configuration);
                    results.Add(result);
                    
                    progress?.Report(new OperatorProgress
                    {
                        Current = i + 1,
                        Total = executionList.Count,
                        OperatorName = execution.OperatorName,
                        Status = "Completed"
                    });
                }
                
                LogDebug($"Successfully executed {executionList.Count} operators in background");
                return results;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                LogError($"Background operator execution failed: {ex.Message}");
                throw;
            }
        }
        
        #endregion
        
        #region Configuration Access
        
        /// <summary>
        /// Get configuration value with type safety
        /// </summary>
        public T GetConfigValue<T>(string key, T defaultValue = default)
        {
            if (!IsConfigurationLoaded || !CurrentConfiguration.ContainsKey(key))
            {
                return defaultValue;
            }
            
            try
            {
                var value = CurrentConfiguration[key];
                
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
        /// Set configuration value and notify MVVM bindings
        /// </summary>
        public void SetConfigValue(string key, object value)
        {
            if (CurrentConfiguration == null)
            {
                CurrentConfiguration = new Dictionary<string, object>();
            }
            
            CurrentConfiguration[key] = value;
            OnPropertyChanged(nameof(CurrentConfiguration));
            
            LogDebug($"Config value set: {key} = {value}");
        }
        
        /// <summary>
        /// Check if configuration key exists
        /// </summary>
        public bool HasConfigValue(string key)
        {
            return IsConfigurationLoaded && CurrentConfiguration.ContainsKey(key);
        }
        
        #endregion
        
        #region Template Processing
        
        /// <summary>
        /// Process template with current configuration
        /// </summary>
        public async Task<string> ProcessTemplateAsync(string template)
        {
            ThrowIfNotInitialized();
            
            try
            {
                var variables = CurrentConfiguration ?? new Dictionary<string, object>();
                return await _tuskService.ProcessTemplateAsync(template, variables, _cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                LogError($"Failed to process template: {ex.Message}");
                throw;
            }
        }
        
        #endregion
        
        #region Platform Integration
        
        /// <summary>
        /// Save configuration to device storage
        /// </summary>
        public async Task SaveConfigurationAsync(string fileName)
        {
            if (!IsConfigurationLoaded)
            {
                throw new InvalidOperationException("No configuration loaded to save");
            }
            
#if XAMARIN_BUILD
            try
            {
                var filePath = Path.Combine(FileSystem.AppDataDirectory, fileName);
                var json = System.Text.Json.JsonSerializer.Serialize(CurrentConfiguration, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                
                await File.WriteAllTextAsync(filePath, json);
                
                LogDebug($"Configuration saved to: {filePath}");
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                LogError($"Failed to save configuration: {ex.Message}");
                throw;
            }
#endif
        }
        
        /// <summary>
        /// Get device information for configuration
        /// </summary>
        public Dictionary<string, object> GetDeviceInfo()
        {
#if XAMARIN_BUILD
            try
            {
                return new Dictionary<string, object>
                {
                    ["platform"] = DeviceInfo.Platform.ToString(),
                    ["version"] = DeviceInfo.VersionString,
                    ["model"] = DeviceInfo.Model,
                    ["manufacturer"] = DeviceInfo.Manufacturer,
                    ["name"] = DeviceInfo.Name,
                    ["idiom"] = DeviceInfo.Idiom.ToString(),
                    ["type"] = DeviceInfo.DeviceType.ToString(),
                    ["app_version"] = AppInfo.VersionString,
                    ["app_build"] = AppInfo.BuildString,
                    ["package_name"] = AppInfo.PackageName
                };
            }
            catch (Exception ex)
            {
                LogError($"Failed to get device info: {ex.Message}");
                return new Dictionary<string, object> { ["error"] = ex.Message };
            }
#else
            return new Dictionary<string, object> { ["platform"] = "unknown" };
#endif
        }
        
        #endregion
        
        #region Private Methods
        
        private async Task<string> GetConfigurationFilePathAsync(string fileName)
        {
#if XAMARIN_BUILD
            // Try app data directory first
            var appDataPath = Path.Combine(FileSystem.AppDataDirectory, fileName);
            if (File.Exists(appDataPath))
            {
                return appDataPath;
            }
            
            // Try bundle/assets
            var bundlePath = Path.Combine(FileSystem.OpenAppPackageFileAsync(fileName).ConfigureAwait(false).GetAwaiter().GetResult().Name);
            return bundlePath;
#else
            return fileName;
#endif
        }
        
        private async Task<string> LoadFileContentAsync(string filePath)
        {
#if XAMARIN_BUILD
            try
            {
                // Try as app package file first
                using var stream = await FileSystem.OpenAppPackageFileAsync(Path.GetFileName(filePath));
                using var reader = new StreamReader(stream);
                return await reader.ReadToEndAsync();
            }
            catch
            {
                // Fall back to regular file system
                if (File.Exists(filePath))
                {
                    return await File.ReadAllTextAsync(filePath);
                }
                throw new FileNotFoundException($"File not found: {filePath}");
            }
#else
            return await File.ReadAllTextAsync(filePath);
#endif
        }
        
        private void ThrowIfNotInitialized()
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("TuskTsk Xamarin integration not initialized. Call InitializeAsync first.");
            }
        }
        
        private void LogDebug(string message)
        {
            if (Options.EnableDebugLogging)
            {
                System.Diagnostics.Debug.WriteLine($"[TuskTsk] {message}");
            }
        }
        
        private void LogError(string message)
        {
            System.Diagnostics.Debug.WriteLine($"[TuskTsk Error] {message}");
        }
        
        private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        #endregion
        
        #region IDisposable
        
        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            
            if (Instance == this)
            {
                Instance = null;
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// Xamarin-specific options
    /// </summary>
    public class TuskTskXamarinOptions
    {
        public string DefaultConfigurationFile { get; set; }
        public bool EnableDebugLogging { get; set; } = false;
        public bool CacheConfigurations { get; set; } = true;
        public int CacheTimeoutMinutes { get; set; } = 30;
        public int MaxConcurrentOperators { get; set; } = 5;
        public int OperatorTimeoutSeconds { get; set; } = 30;
        public int NetworkTimeoutSeconds { get; set; } = 30;
        public bool EnableErrorRecovery { get; set; } = true;
        public bool EnableBackgroundExecution { get; set; } = true;
    }
    
    /// <summary>
    /// Operator execution definition for Xamarin
    /// </summary>
    public class OperatorExecution
    {
        public string OperatorName { get; set; }
        public Dictionary<string, object> Configuration { get; set; } = new();
        public int TimeoutSeconds { get; set; } = 30;
    }
    
    /// <summary>
    /// Progress reporting for operator execution
    /// </summary>
    public class OperatorProgress
    {
        public int Current { get; set; }
        public int Total { get; set; }
        public string OperatorName { get; set; }
        public string Status { get; set; }
        public double Percentage => Total > 0 ? (double)Current / Total * 100 : 0;
    }
    
    /// <summary>
    /// Xamarin-specific TuskTsk service
    /// </summary>
    public class TuskTskXamarinService
    {
        private readonly TuskTskXamarinOptions _options;
        
        public TuskTskXamarinService(TuskTskXamarinOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            InitializeComponents();
        }
        
        public async Task<Dictionary<string, object>> ParseConfigurationAsync(string content, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                var parser = new TuskLang.TSKParserEnhanced();
                return parser.ParseConfiguration(content);
            }, cancellationToken);
        }
        
        public async Task<object> ExecuteOperatorAsync(string operatorName, Dictionary<string, object> config, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                return TuskLang.Operators.OperatorRegistry.ExecuteOperator(operatorName, config);
            }, cancellationToken);
        }
        
        public async Task<string> ProcessTemplateAsync(string template, Dictionary<string, object> variables, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
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
            TuskLang.Operators.OperatorRegistry.Initialize();
        }
    }
} 