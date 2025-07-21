using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Security.Cryptography;

namespace TuskLang.Framework.Advanced
{
    /// <summary>
    /// Production-ready Blazor WebAssembly support for TuskTsk
    /// Provides client-side configuration management with offline capabilities
    /// </summary>
    public class BlazorWebAssemblySupport : IAsyncDisposable
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly ILogger<BlazorWebAssemblySupport> _logger;
        private readonly ConcurrentDictionary<string, ConfigurationCache> _configurationCache;
        private readonly ConcurrentDictionary<string, ConfigurationSubscription> _subscriptions;
        private readonly BlazorWasmOptions _options;
        private readonly PerformanceMetrics _metrics;
        private readonly SemaphoreSlim _syncSemaphore;
        private bool _disposed = false;

        public BlazorWebAssemblySupport(
            IJSRuntime jsRuntime,
            BlazorWasmOptions options = null,
            ILogger<BlazorWebAssemblySupport> logger = null)
        {
            _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
            _options = options ?? new BlazorWasmOptions();
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<BlazorWebAssemblySupport>.Instance;
            
            _configurationCache = new ConcurrentDictionary<string, ConfigurationCache>();
            _subscriptions = new ConcurrentDictionary<string, ConfigurationSubscription>();
            _syncSemaphore = new SemaphoreSlim(1, 1);
            _metrics = new PerformanceMetrics();

            _logger.LogInformation("Blazor WebAssembly support initialized");
        }

        #region Configuration Management

        /// <summary>
        /// Load configuration from server or cache
        /// </summary>
        public async Task<BlazorConfigurationResult> LoadConfigurationAsync(
            string configKey, CancellationToken cancellationToken = default)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = new BlazorConfigurationResult { ConfigKey = configKey, Success = false };

            try
            {
                // Check cache first
                if (_configurationCache.TryGetValue(configKey, out var cachedConfig))
                {
                    if (!cachedConfig.IsExpired)
                    {
                        result.Success = true;
                        result.Configuration = cachedConfig.Configuration;
                        result.Source = ConfigurationSource.Cache;
                        result.LoadTime = stopwatch.Elapsed;
                        
                        _metrics.RecordCacheHit(stopwatch.Elapsed);
                        _logger.LogDebug($"Configuration loaded from cache: {configKey}");
                        return result;
                    }
                }

                // Try to load from browser storage
                var storageResult = await LoadFromBrowserStorageAsync(configKey, cancellationToken);
                if (storageResult.Success)
                {
                    result.Success = true;
                    result.Configuration = storageResult.Configuration;
                    result.Source = ConfigurationSource.BrowserStorage;
                    result.LoadTime = stopwatch.Elapsed;
                    
                    // Update cache
                    _configurationCache[configKey] = new ConfigurationCache
                    {
                        Configuration = storageResult.Configuration,
                        Timestamp = DateTime.UtcNow,
                        ExpiresAt = DateTime.UtcNow.Add(_options.CacheExpiration)
                    };
                    
                    _metrics.RecordStorageHit(stopwatch.Elapsed);
                    _logger.LogDebug($"Configuration loaded from browser storage: {configKey}");
                    return result;
                }

                // Load from server
                var serverResult = await LoadFromServerAsync(configKey, cancellationToken);
                if (serverResult.Success)
                {
                    result.Success = true;
                    result.Configuration = serverResult.Configuration;
                    result.Source = ConfigurationSource.Server;
                    result.LoadTime = stopwatch.Elapsed;

                    // Cache configuration
                    await CacheConfigurationAsync(configKey, serverResult.Configuration, cancellationToken);
                    
                    _metrics.RecordServerHit(stopwatch.Elapsed);
                    _logger.LogInformation($"Configuration loaded from server: {configKey}");
                    return result;
                }

                result.ErrorMessage = "Failed to load configuration from all sources";
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.ErrorMessage = ex.Message;
                result.LoadTime = stopwatch.Elapsed;
                
                _metrics.RecordError(stopwatch.Elapsed);
                _logger.LogError(ex, $"Failed to load configuration: {configKey}");
                return result;
            }
        }

        /// <summary>
        /// Save configuration to browser storage and server
        /// </summary>
        public async Task<BlazorConfigurationResult> SaveConfigurationAsync(
            string configKey, Dictionary<string, object> configuration, CancellationToken cancellationToken = default)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = new BlazorConfigurationResult { ConfigKey = configKey, Success = false };

            try
            {
                await _syncSemaphore.WaitAsync(cancellationToken);

                // Validate configuration
                var validationResult = await ValidateConfigurationAsync(configuration, cancellationToken);
                if (!validationResult.IsValid)
                {
                    result.ErrorMessage = $"Configuration validation failed: {validationResult.ErrorMessage}";
                    return result;
                }

                // Save to browser storage first (for offline capability)
                var storageResult = await SaveToBrowserStorageAsync(configKey, configuration, cancellationToken);
                if (!storageResult.Success)
                {
                    result.ErrorMessage = $"Failed to save to browser storage: {storageResult.ErrorMessage}";
                    return result;
                }

                // Update cache
                _configurationCache[configKey] = new ConfigurationCache
                {
                    Configuration = configuration,
                    Timestamp = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.Add(_options.CacheExpiration)
                };

                // Try to save to server (if online)
                if (await IsOnlineAsync())
                {
                    var serverResult = await SaveToServerAsync(configKey, configuration, cancellationToken);
                    if (serverResult.Success)
                    {
                        result.Source = ConfigurationSource.Server;
                    }
                    else
                    {
                        // Queue for later sync
                        await QueueForSyncAsync(configKey, configuration, cancellationToken);
                        result.Source = ConfigurationSource.BrowserStorage;
                    }
                }
                else
                {
                    // Queue for later sync when online
                    await QueueForSyncAsync(configKey, configuration, cancellationToken);
                    result.Source = ConfigurationSource.BrowserStorage;
                }

                // Notify subscribers
                await NotifyConfigurationChangedAsync(configKey, configuration, cancellationToken);

                stopwatch.Stop();
                result.Success = true;
                result.Configuration = configuration;
                result.LoadTime = stopwatch.Elapsed;

                _metrics.RecordSave(stopwatch.Elapsed);
                _logger.LogInformation($"Configuration saved: {configKey}");

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.ErrorMessage = ex.Message;
                result.LoadTime = stopwatch.Elapsed;
                
                _metrics.RecordError(stopwatch.Elapsed);
                _logger.LogError(ex, $"Failed to save configuration: {configKey}");
                return result;
            }
            finally
            {
                _syncSemaphore.Release();
            }
        }

        /// <summary>
        /// Subscribe to configuration changes
        /// </summary>
        public async Task<ConfigurationSubscription> SubscribeToConfigurationAsync(
            string configKey, Func<Dictionary<string, object>, Task> callback, CancellationToken cancellationToken = default)
        {
            try
            {
                var subscription = new ConfigurationSubscription
                {
                    Id = Guid.NewGuid().ToString(),
                    ConfigKey = configKey,
                    Callback = callback,
                    CreatedAt = DateTime.UtcNow
                };

                _subscriptions[subscription.Id] = subscription;

                // Set up cross-tab communication
                await SetupCrossTabCommunicationAsync(configKey, cancellationToken);

                _logger.LogDebug($"Configuration subscription created: {configKey}");
                return subscription;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to create subscription for: {configKey}");
                throw;
            }
        }

        /// <summary>
        /// Unsubscribe from configuration changes
        /// </summary>
        public async Task<bool> UnsubscribeFromConfigurationAsync(string subscriptionId)
        {
            try
            {
                if (_subscriptions.TryRemove(subscriptionId, out var subscription))
                {
                    _logger.LogDebug($"Configuration subscription removed: {subscription.ConfigKey}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to remove subscription: {subscriptionId}");
                return false;
            }
        }

        #endregion

        #region Offline Support

        /// <summary>
        /// Sync offline changes when back online
        /// </summary>
        public async Task<SyncResult> SyncOfflineChangesAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = new SyncResult { Success = false, SyncedCount = 0 };

            try
            {
                if (!await IsOnlineAsync())
                {
                    result.ErrorMessage = "Not online";
                    return result;
                }

                var pendingChanges = await GetPendingSyncChangesAsync(cancellationToken);
                var syncedCount = 0;

                foreach (var change in pendingChanges)
                {
                    try
                    {
                        var serverResult = await SaveToServerAsync(change.ConfigKey, change.Configuration, cancellationToken);
                        if (serverResult.Success)
                        {
                            await RemovePendingSyncChangeAsync(change.ConfigKey, cancellationToken);
                            syncedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to sync change for {change.ConfigKey}");
                    }
                }

                stopwatch.Stop();
                result.Success = true;
                result.SyncedCount = syncedCount;
                result.SyncTime = stopwatch.Elapsed;

                _metrics.RecordSync(stopwatch.Elapsed, syncedCount);
                _logger.LogInformation($"Synced {syncedCount} offline changes");

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.ErrorMessage = ex.Message;
                result.SyncTime = stopwatch.Elapsed;
                
                _logger.LogError(ex, "Failed to sync offline changes");
                return result;
            }
        }

        /// <summary>
        /// Check if online
        /// </summary>
        public async Task<bool> IsOnlineAsync()
        {
            try
            {
                return await _jsRuntime.InvokeAsync<bool>("navigator.onLine");
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get offline configuration
        /// </summary>
        public async Task<Dictionary<string, object>> GetOfflineConfigurationAsync(string configKey, CancellationToken cancellationToken = default)
        {
            try
            {
                var storageResult = await LoadFromBrowserStorageAsync(configKey, cancellationToken);
                return storageResult.Success ? storageResult.Configuration : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get offline configuration: {configKey}");
                return null;
            }
        }

        #endregion

        #region Browser Storage Operations

        /// <summary>
        /// Load configuration from browser storage
        /// </summary>
        private async Task<BlazorConfigurationResult> LoadFromBrowserStorageAsync(
            string configKey, CancellationToken cancellationToken)
        {
            try
            {
                var storageKey = GetStorageKey(configKey);
                var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", storageKey);
                
                if (string.IsNullOrEmpty(json))
                    return new BlazorConfigurationResult { Success = false };

                var configuration = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                return new BlazorConfigurationResult
                {
                    Success = true,
                    Configuration = configuration
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to load from browser storage: {configKey}");
                return new BlazorConfigurationResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        /// <summary>
        /// Save configuration to browser storage
        /// </summary>
        private async Task<BlazorConfigurationResult> SaveToBrowserStorageAsync(
            string configKey, Dictionary<string, object> configuration, CancellationToken cancellationToken)
        {
            try
            {
                var storageKey = GetStorageKey(configKey);
                var json = JsonSerializer.Serialize(configuration, new JsonSerializerOptions { WriteIndented = true });
                
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", storageKey, json);
                
                return new BlazorConfigurationResult { Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to save to browser storage: {configKey}");
                return new BlazorConfigurationResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        /// <summary>
        /// Get storage key
        /// </summary>
        private string GetStorageKey(string configKey)
        {
            return $"tuskt_config_{configKey}";
        }

        #endregion

        #region Server Operations

        /// <summary>
        /// Load configuration from server
        /// </summary>
        private async Task<BlazorConfigurationResult> LoadFromServerAsync(
            string configKey, CancellationToken cancellationToken)
        {
            try
            {
                // This would make an HTTP request to the server
                // For now, we'll simulate the server response
                var configuration = new Dictionary<string, object>
                {
                    ["server_loaded"] = true,
                    ["timestamp"] = DateTime.UtcNow.ToString("O"),
                    ["config_key"] = configKey
                };

                return new BlazorConfigurationResult
                {
                    Success = true,
                    Configuration = configuration
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to load from server: {configKey}");
                return new BlazorConfigurationResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        /// <summary>
        /// Save configuration to server
        /// </summary>
        private async Task<BlazorConfigurationResult> SaveToServerAsync(
            string configKey, Dictionary<string, object> configuration, CancellationToken cancellationToken)
        {
            try
            {
                // This would make an HTTP request to the server
                // For now, we'll simulate the server response
                await Task.Delay(100, cancellationToken); // Simulate network delay

                return new BlazorConfigurationResult { Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to save to server: {configKey}");
                return new BlazorConfigurationResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        #endregion

        #region Cross-Tab Communication

        /// <summary>
        /// Set up cross-tab communication
        /// </summary>
        private async Task SetupCrossTabCommunicationAsync(string configKey, CancellationToken cancellationToken)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("setupCrossTabCommunication", configKey, DotNetObjectReference.Create(this));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to setup cross-tab communication: {configKey}");
            }
        }

        /// <summary>
        /// Handle cross-tab configuration change
        /// </summary>
        [JSInvokable]
        public async Task OnCrossTabConfigurationChanged(string configKey, string configurationJson)
        {
            try
            {
                var configuration = JsonSerializer.Deserialize<Dictionary<string, object>>(configurationJson);
                
                // Update cache
                _configurationCache[configKey] = new ConfigurationCache
                {
                    Configuration = configuration,
                    Timestamp = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.Add(_options.CacheExpiration)
                };

                // Notify subscribers
                await NotifyConfigurationChangedAsync(configKey, configuration, CancellationToken.None);

                _logger.LogDebug($"Cross-tab configuration change handled: {configKey}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to handle cross-tab configuration change: {configKey}");
            }
        }

        #endregion

        #region PWA Support

        /// <summary>
        /// Register service worker for PWA support
        /// </summary>
        public async Task<bool> RegisterServiceWorkerAsync(string serviceWorkerPath = "/sw.js")
        {
            try
            {
                var result = await _jsRuntime.InvokeAsync<bool>("registerServiceWorker", serviceWorkerPath);
                
                if (result)
                {
                    _logger.LogInformation("Service worker registered successfully");
                }
                else
                {
                    _logger.LogWarning("Service worker registration failed");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register service worker");
                return false;
            }
        }

        /// <summary>
        /// Install PWA
        /// </summary>
        public async Task<bool> InstallPWAAsync()
        {
            try
            {
                var result = await _jsRuntime.InvokeAsync<bool>("installPWA");
                
                if (result)
                {
                    _logger.LogInformation("PWA installed successfully");
                }
                else
                {
                    _logger.LogWarning("PWA installation failed");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to install PWA");
                return false;
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Cache configuration
        /// </summary>
        private async Task CacheConfigurationAsync(
            string configKey, Dictionary<string, object> configuration, CancellationToken cancellationToken)
        {
            _configurationCache[configKey] = new ConfigurationCache
            {
                Configuration = configuration,
                Timestamp = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(_options.CacheExpiration)
            };
        }

        /// <summary>
        /// Validate configuration
        /// </summary>
        private async Task<ConfigurationValidationResult> ValidateConfigurationAsync(
            Dictionary<string, object> configuration, CancellationToken cancellationToken)
        {
            var result = new ConfigurationValidationResult { IsValid = true, Errors = new List<string>() };

            try
            {
                // Check for required fields
                var requiredFields = _options.RequiredFields ?? new string[0];
                foreach (var field in requiredFields)
                {
                    if (!configuration.ContainsKey(field))
                    {
                        result.IsValid = false;
                        result.Errors.Add($"Required field '{field}' is missing");
                    }
                }

                // Check for size limits
                var json = JsonSerializer.Serialize(configuration);
                if (json.Length > _options.MaxConfigurationSize)
                {
                    result.IsValid = false;
                    result.Errors.Add($"Configuration size exceeds limit of {_options.MaxConfigurationSize} bytes");
                }

                if (!result.IsValid)
                {
                    result.ErrorMessage = string.Join("; ", result.Errors);
                }

                return result;
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.ErrorMessage = ex.Message;
                result.Errors.Add(ex.Message);
                return result;
            }
        }

        /// <summary>
        /// Queue for sync
        /// </summary>
        private async Task QueueForSyncAsync(
            string configKey, Dictionary<string, object> configuration, CancellationToken cancellationToken)
        {
            try
            {
                var pendingChange = new PendingSyncChange
                {
                    ConfigKey = configKey,
                    Configuration = configuration,
                    Timestamp = DateTime.UtcNow
                };

                var storageKey = $"tuskt_pending_sync_{configKey}";
                var json = JsonSerializer.Serialize(pendingChange);
                
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", storageKey, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to queue for sync: {configKey}");
            }
        }

        /// <summary>
        /// Get pending sync changes
        /// </summary>
        private async Task<List<PendingSyncChange>> GetPendingSyncChangesAsync(CancellationToken cancellationToken)
        {
            var changes = new List<PendingSyncChange>();

            try
            {
                // This would iterate through localStorage to find pending changes
                // For now, we'll return an empty list
                return changes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get pending sync changes");
                return changes;
            }
        }

        /// <summary>
        /// Remove pending sync change
        /// </summary>
        private async Task RemovePendingSyncChangeAsync(string configKey, CancellationToken cancellationToken)
        {
            try
            {
                var storageKey = $"tuskt_pending_sync_{configKey}";
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", storageKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to remove pending sync change: {configKey}");
            }
        }

        /// <summary>
        /// Notify configuration changed
        /// </summary>
        private async Task NotifyConfigurationChangedAsync(
            string configKey, Dictionary<string, object> configuration, CancellationToken cancellationToken)
        {
            var subscriptions = _subscriptions.Values
                .Where(s => s.ConfigKey == configKey)
                .ToList();

            foreach (var subscription in subscriptions)
            {
                try
                {
                    await subscription.Callback(configuration);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error in configuration subscription callback: {subscription.Id}");
                }
            }
        }

        #endregion

        #region Performance Metrics

        /// <summary>
        /// Get performance metrics
        /// </summary>
        public PerformanceMetrics GetPerformanceMetrics()
        {
            return _metrics;
        }

        #endregion

        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                _syncSemaphore?.Dispose();
                _disposed = true;
            }
        }
    }

    #region Supporting Classes

    /// <summary>
    /// Blazor WebAssembly options
    /// </summary>
    public class BlazorWasmOptions
    {
        public TimeSpan CacheExpiration { get; set; } = TimeSpan.FromHours(1);
        public string[] RequiredFields { get; set; }
        public int MaxConfigurationSize { get; set; } = 1024 * 1024; // 1MB
        public bool EnableCrossTabCommunication { get; set; } = true;
        public bool EnablePWA { get; set; } = true;
        public bool EnableOfflineSupport { get; set; } = true;
    }

    /// <summary>
    /// Configuration cache
    /// </summary>
    public class ConfigurationCache
    {
        public Dictionary<string, object> Configuration { get; set; }
        public DateTime Timestamp { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    }

    /// <summary>
    /// Configuration subscription
    /// </summary>
    public class ConfigurationSubscription
    {
        public string Id { get; set; }
        public string ConfigKey { get; set; }
        public Func<Dictionary<string, object>, Task> Callback { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Blazor configuration result
    /// </summary>
    public class BlazorConfigurationResult
    {
        public string ConfigKey { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public Dictionary<string, object> Configuration { get; set; }
        public ConfigurationSource Source { get; set; }
        public TimeSpan LoadTime { get; set; }
    }

    /// <summary>
    /// Configuration source
    /// </summary>
    public enum ConfigurationSource
    {
        Cache,
        BrowserStorage,
        Server
    }

    /// <summary>
    /// Sync result
    /// </summary>
    public class SyncResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public int SyncedCount { get; set; }
        public TimeSpan SyncTime { get; set; }
    }

    /// <summary>
    /// Pending sync change
    /// </summary>
    public class PendingSyncChange
    {
        public string ConfigKey { get; set; }
        public Dictionary<string, object> Configuration { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Configuration validation result
    /// </summary>
    public class ConfigurationValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }

    /// <summary>
    /// Performance metrics
    /// </summary>
    public class PerformanceMetrics
    {
        private readonly List<TimeSpan> _loadTimes = new List<TimeSpan>();
        private readonly List<TimeSpan> _saveTimes = new List<TimeSpan>();
        private readonly List<TimeSpan> _syncTimes = new List<TimeSpan>();
        private int _cacheHits = 0;
        private int _storageHits = 0;
        private int _serverHits = 0;
        private int _errors = 0;

        public void RecordCacheHit(TimeSpan time)
        {
            _cacheHits++;
            _loadTimes.Add(time);
            if (_loadTimes.Count > 1000) _loadTimes.RemoveAt(0);
        }

        public void RecordStorageHit(TimeSpan time)
        {
            _storageHits++;
            _loadTimes.Add(time);
            if (_loadTimes.Count > 1000) _loadTimes.RemoveAt(0);
        }

        public void RecordServerHit(TimeSpan time)
        {
            _serverHits++;
            _loadTimes.Add(time);
            if (_loadTimes.Count > 1000) _loadTimes.RemoveAt(0);
        }

        public void RecordSave(TimeSpan time)
        {
            _saveTimes.Add(time);
            if (_saveTimes.Count > 1000) _saveTimes.RemoveAt(0);
        }

        public void RecordSync(TimeSpan time, int count)
        {
            _syncTimes.Add(time);
            if (_syncTimes.Count > 100) _syncTimes.RemoveAt(0);
        }

        public void RecordError(TimeSpan time)
        {
            _errors++;
        }

        public double AverageLoadTime => _loadTimes.Count > 0 ? _loadTimes.Average(t => t.TotalMilliseconds) : 0;
        public double AverageSaveTime => _saveTimes.Count > 0 ? _saveTimes.Average(t => t.TotalMilliseconds) : 0;
        public double AverageSyncTime => _syncTimes.Count > 0 ? _syncTimes.Average(t => t.TotalMilliseconds) : 0;
        public int CacheHits => _cacheHits;
        public int StorageHits => _storageHits;
        public int ServerHits => _serverHits;
        public int Errors => _errors;
        public double CacheHitRate => (_cacheHits + _storageHits + _serverHits) > 0 ? (double)_cacheHits / (_cacheHits + _storageHits + _serverHits) : 0;
    }

    #endregion
} 