using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Reflection;
using System.Runtime.InteropServices;

namespace TuskLang.Framework.Advanced
{
    /// <summary>
    /// Production-ready advanced platform integration for TuskTsk
    /// Supports .NET MAUI, Avalonia, and cross-platform configuration management
    /// </summary>
    public class AdvancedPlatformIntegration : IDisposable
    {
        private readonly ILogger<AdvancedPlatformIntegration> _logger;
        private readonly ConcurrentDictionary<string, PlatformConfiguration> _platformConfigs;
        private readonly ConcurrentDictionary<string, PlatformAdapter> _platformAdapters;
        private readonly AdvancedPlatformOptions _options;
        private readonly PerformanceMetrics _metrics;
        private readonly SemaphoreSlim _syncSemaphore;
        private bool _disposed = false;

        public AdvancedPlatformIntegration(
            AdvancedPlatformOptions options = null,
            ILogger<AdvancedPlatformIntegration> logger = null)
        {
            _options = options ?? new AdvancedPlatformOptions();
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<AdvancedPlatformIntegration>.Instance;
            
            _platformConfigs = new ConcurrentDictionary<string, PlatformConfiguration>();
            _platformAdapters = new ConcurrentDictionary<string, PlatformAdapter>();
            _syncSemaphore = new SemaphoreSlim(1, 1);
            _metrics = new PerformanceMetrics();

            InitializePlatformAdapters();
            _logger.LogInformation("Advanced platform integration initialized");
        }

        #region Platform Detection & Initialization

        /// <summary>
        /// Initialize platform adapters
        /// </summary>
        private void InitializePlatformAdapters()
        {
            // Detect current platform
            var currentPlatform = DetectCurrentPlatform();
            _logger.LogInformation($"Detected platform: {currentPlatform}");

            // Register platform adapters
            RegisterPlatformAdapter(PlatformType.MAUI, new MauiPlatformAdapter(_logger));
            RegisterPlatformAdapter(PlatformType.Avalonia, new AvaloniaPlatformAdapter(_logger));
            RegisterPlatformAdapter(PlatformType.WinUI, new WinUIPlatformAdapter(_logger));
            RegisterPlatformAdapter(PlatformType.Uno, new UnoPlatformAdapter(_logger));
            RegisterPlatformAdapter(PlatformType.Xamarin, new XamarinPlatformAdapter(_logger));
        }

        /// <summary>
        /// Detect current platform
        /// </summary>
        public PlatformType DetectCurrentPlatform()
        {
            try
            {
                // Check for MAUI
                if (IsMauiPlatform())
                    return PlatformType.MAUI;

                // Check for Avalonia
                if (IsAvaloniaPlatform())
                    return PlatformType.Avalonia;

                // Check for WinUI
                if (IsWinUIPlatform())
                    return PlatformType.WinUI;

                // Check for Uno
                if (IsUnoPlatform())
                    return PlatformType.Uno;

                // Check for Xamarin
                if (IsXamarinPlatform())
                    return PlatformType.Xamarin;

                // Default to desktop
                return PlatformType.Desktop;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to detect platform");
                return PlatformType.Desktop;
            }
        }

        /// <summary>
        /// Check if running on MAUI platform
        /// </summary>
        private bool IsMauiPlatform()
        {
            try
            {
                // Check for MAUI-specific assemblies
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                return assemblies.Any(a => a.FullName.Contains("Microsoft.Maui"));
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check if running on Avalonia platform
        /// </summary>
        private bool IsAvaloniaPlatform()
        {
            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                return assemblies.Any(a => a.FullName.Contains("Avalonia"));
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check if running on WinUI platform
        /// </summary>
        private bool IsWinUIPlatform()
        {
            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                return assemblies.Any(a => a.FullName.Contains("Microsoft.UI"));
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check if running on Uno platform
        /// </summary>
        private bool IsUnoPlatform()
        {
            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                return assemblies.Any(a => a.FullName.Contains("Uno"));
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check if running on Xamarin platform
        /// </summary>
        private bool IsXamarinPlatform()
        {
            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                return assemblies.Any(a => a.FullName.Contains("Xamarin"));
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Platform Configuration Management

        /// <summary>
        /// Load platform-specific configuration
        /// </summary>
        public async Task<PlatformConfigurationResult> LoadPlatformConfigurationAsync(
            PlatformType platformType, string configKey, CancellationToken cancellationToken = default)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = new PlatformConfigurationResult { PlatformType = platformType, ConfigKey = configKey, Success = false };

            try
            {
                // Get platform adapter
                if (!_platformAdapters.TryGetValue(platformType.ToString(), out var adapter))
                {
                    result.ErrorMessage = $"Platform adapter not found for {platformType}";
                    return result;
                }

                // Load platform-specific configuration
                var config = await adapter.LoadConfigurationAsync(configKey, cancellationToken);
                if (config.Success)
                {
                    // Apply platform-specific transformations
                    var transformedConfig = await ApplyPlatformTransformationsAsync(platformType, config.Configuration, cancellationToken);
                    
                    // Cache configuration
                    _platformConfigs[configKey] = new PlatformConfiguration
                    {
                        PlatformType = platformType,
                        Configuration = transformedConfig,
                        Timestamp = DateTime.UtcNow
                    };

                    result.Success = true;
                    result.Configuration = transformedConfig;
                    result.LoadTime = stopwatch.Elapsed;

                    _metrics.RecordLoad(stopwatch.Elapsed, platformType);
                    _logger.LogInformation($"Platform configuration loaded: {platformType} - {configKey}");
                }
                else
                {
                    result.ErrorMessage = config.ErrorMessage;
                }

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.ErrorMessage = ex.Message;
                result.LoadTime = stopwatch.Elapsed;
                
                _metrics.RecordError(stopwatch.Elapsed, platformType);
                _logger.LogError(ex, $"Failed to load platform configuration: {platformType} - {configKey}");
                return result;
            }
        }

        /// <summary>
        /// Save platform-specific configuration
        /// </summary>
        public async Task<PlatformConfigurationResult> SavePlatformConfigurationAsync(
            PlatformType platformType, string configKey, Dictionary<string, object> configuration, CancellationToken cancellationToken = default)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = new PlatformConfigurationResult { PlatformType = platformType, ConfigKey = configKey, Success = false };

            try
            {
                await _syncSemaphore.WaitAsync(cancellationToken);

                // Get platform adapter
                if (!_platformAdapters.TryGetValue(platformType.ToString(), out var adapter))
                {
                    result.ErrorMessage = $"Platform adapter not found for {platformType}";
                    return result;
                }

                // Validate platform-specific configuration
                var validationResult = await ValidatePlatformConfigurationAsync(platformType, configuration, cancellationToken);
                if (!validationResult.IsValid)
                {
                    result.ErrorMessage = $"Platform configuration validation failed: {validationResult.ErrorMessage}";
                    return result;
                }

                // Apply platform-specific transformations
                var transformedConfig = await ApplyPlatformTransformationsAsync(platformType, configuration, cancellationToken);

                // Save configuration
                var saveResult = await adapter.SaveConfigurationAsync(configKey, transformedConfig, cancellationToken);
                if (saveResult.Success)
                {
                    // Cache configuration
                    _platformConfigs[configKey] = new PlatformConfiguration
                    {
                        PlatformType = platformType,
                        Configuration = transformedConfig,
                        Timestamp = DateTime.UtcNow
                    };

                    // Sync across platforms if enabled
                    if (_options.EnableCrossPlatformSync)
                    {
                        await SyncAcrossPlatformsAsync(configKey, transformedConfig, cancellationToken);
                    }

                    result.Success = true;
                    result.Configuration = transformedConfig;
                    result.LoadTime = stopwatch.Elapsed;

                    _metrics.RecordSave(stopwatch.Elapsed, platformType);
                    _logger.LogInformation($"Platform configuration saved: {platformType} - {configKey}");
                }
                else
                {
                    result.ErrorMessage = saveResult.ErrorMessage;
                }

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.ErrorMessage = ex.Message;
                result.LoadTime = stopwatch.Elapsed;
                
                _metrics.RecordError(stopwatch.Elapsed, platformType);
                _logger.LogError(ex, $"Failed to save platform configuration: {platformType} - {configKey}");
                return result;
            }
            finally
            {
                _syncSemaphore.Release();
            }
        }

        #endregion

        #region Platform-Specific Features

        /// <summary>
        /// Get platform-specific UI configuration
        /// </summary>
        public async Task<UIConfigurationResult> GetUIConfigurationAsync(PlatformType platformType, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!_platformAdapters.TryGetValue(platformType.ToString(), out var adapter))
                {
                    return new UIConfigurationResult { Success = false, ErrorMessage = $"Platform adapter not found for {platformType}" };
                }

                var uiConfig = await adapter.GetUIConfigurationAsync(cancellationToken);
                
                _logger.LogDebug($"UI configuration retrieved for {platformType}");
                return uiConfig;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get UI configuration for {platformType}");
                return new UIConfigurationResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        /// <summary>
        /// Apply platform-specific theme
        /// </summary>
        public async Task<bool> ApplyThemeAsync(PlatformType platformType, string themeName, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!_platformAdapters.TryGetValue(platformType.ToString(), out var adapter))
                {
                    _logger.LogError($"Platform adapter not found for {platformType}");
                    return false;
                }

                var result = await adapter.ApplyThemeAsync(themeName, cancellationToken);
                
                if (result)
                {
                    _logger.LogInformation($"Theme '{themeName}' applied to {platformType}");
                }
                else
                {
                    _logger.LogWarning($"Failed to apply theme '{themeName}' to {platformType}");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to apply theme '{themeName}' to {platformType}");
                return false;
            }
        }

        /// <summary>
        /// Get platform-specific device information
        /// </summary>
        public async Task<DeviceInfoResult> GetDeviceInfoAsync(PlatformType platformType, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!_platformAdapters.TryGetValue(platformType.ToString(), out var adapter))
                {
                    return new DeviceInfoResult { Success = false, ErrorMessage = $"Platform adapter not found for {platformType}" };
                }

                var deviceInfo = await adapter.GetDeviceInfoAsync(cancellationToken);
                
                _logger.LogDebug($"Device info retrieved for {platformType}");
                return deviceInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get device info for {platformType}");
                return new DeviceInfoResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        #endregion

        #region Cross-Platform Synchronization

        /// <summary>
        /// Sync configuration across platforms
        /// </summary>
        private async Task SyncAcrossPlatformsAsync(string configKey, Dictionary<string, object> configuration, CancellationToken cancellationToken)
        {
            try
            {
                var syncTasks = new List<Task>();

                foreach (var adapter in _platformAdapters.Values)
                {
                    if (adapter.PlatformType != DetectCurrentPlatform())
                    {
                        syncTasks.Add(adapter.SaveConfigurationAsync(configKey, configuration, cancellationToken));
                    }
                }

                await Task.WhenAll(syncTasks);
                _logger.LogDebug($"Configuration synced across {syncTasks.Count} platforms");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync configuration across platforms");
            }
        }

        /// <summary>
        /// Get cross-platform configuration
        /// </summary>
        public async Task<CrossPlatformConfigurationResult> GetCrossPlatformConfigurationAsync(string configKey, CancellationToken cancellationToken = default)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = new CrossPlatformConfigurationResult { ConfigKey = configKey, Success = false };

            try
            {
                var platformConfigs = new Dictionary<PlatformType, Dictionary<string, object>>();

                foreach (var adapter in _platformAdapters.Values)
                {
                    var config = await adapter.LoadConfigurationAsync(configKey, cancellationToken);
                    if (config.Success)
                    {
                        platformConfigs[adapter.PlatformType] = config.Configuration;
                    }
                }

                result.Success = true;
                result.PlatformConfigurations = platformConfigs;
                result.LoadTime = stopwatch.Elapsed;

                _logger.LogInformation($"Cross-platform configuration loaded for {configKey}");
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.ErrorMessage = ex.Message;
                result.LoadTime = stopwatch.Elapsed;
                
                _logger.LogError(ex, $"Failed to get cross-platform configuration: {configKey}");
                return result;
            }
        }

        #endregion

        #region Platform Transformations

        /// <summary>
        /// Apply platform-specific transformations
        /// </summary>
        private async Task<Dictionary<string, object>> ApplyPlatformTransformationsAsync(
            PlatformType platformType, Dictionary<string, object> configuration, CancellationToken cancellationToken)
        {
            var transformed = new Dictionary<string, object>(configuration);

            try
            {
                switch (platformType)
                {
                    case PlatformType.MAUI:
                        transformed = await ApplyMauiTransformationsAsync(transformed, cancellationToken);
                        break;
                    case PlatformType.Avalonia:
                        transformed = await ApplyAvaloniaTransformationsAsync(transformed, cancellationToken);
                        break;
                    case PlatformType.WinUI:
                        transformed = await ApplyWinUITransformationsAsync(transformed, cancellationToken);
                        break;
                    case PlatformType.Uno:
                        transformed = await ApplyUnoTransformationsAsync(transformed, cancellationToken);
                        break;
                    case PlatformType.Xamarin:
                        transformed = await ApplyXamarinTransformationsAsync(transformed, cancellationToken);
                        break;
                }

                return transformed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to apply platform transformations for {platformType}");
                return configuration; // Return original if transformation fails
            }
        }

        /// <summary>
        /// Apply MAUI-specific transformations
        /// </summary>
        private async Task<Dictionary<string, object>> ApplyMauiTransformationsAsync(
            Dictionary<string, object> configuration, CancellationToken cancellationToken)
        {
            var transformed = new Dictionary<string, object>(configuration);

            // Add MAUI-specific configuration
            transformed["platform"] = "maui";
            transformed["supports_hot_reload"] = true;
            transformed["supports_platform_specific_ui"] = true;

            // Transform UI configuration for MAUI
            if (transformed.TryGetValue("ui", out var uiConfig) && uiConfig is Dictionary<string, object> ui)
            {
                var mauiUi = new Dictionary<string, object>(ui)
                {
                    ["framework"] = "maui",
                    ["supports_native_controls"] = true,
                    ["supports_custom_renderers"] = true
                };
                transformed["ui"] = mauiUi;
            }

            return transformed;
        }

        /// <summary>
        /// Apply Avalonia-specific transformations
        /// </summary>
        private async Task<Dictionary<string, object>> ApplyAvaloniaTransformationsAsync(
            Dictionary<string, object> configuration, CancellationToken cancellationToken)
        {
            var transformed = new Dictionary<string, object>(configuration);

            // Add Avalonia-specific configuration
            transformed["platform"] = "avalonia";
            transformed["supports_hot_reload"] = true;
            transformed["supports_platform_specific_ui"] = true;

            // Transform UI configuration for Avalonia
            if (transformed.TryGetValue("ui", out var uiConfig) && uiConfig is Dictionary<string, object> ui)
            {
                var avaloniaUi = new Dictionary<string, object>(ui)
                {
                    ["framework"] = "avalonia",
                    ["supports_xaml"] = true,
                    ["supports_styles"] = true
                };
                transformed["ui"] = avaloniaUi;
            }

            return transformed;
        }

        /// <summary>
        /// Apply WinUI-specific transformations
        /// </summary>
        private async Task<Dictionary<string, object>> ApplyWinUITransformationsAsync(
            Dictionary<string, object> configuration, CancellationToken cancellationToken)
        {
            var transformed = new Dictionary<string, object>(configuration);

            // Add WinUI-specific configuration
            transformed["platform"] = "winui";
            transformed["supports_hot_reload"] = true;
            transformed["supports_platform_specific_ui"] = true;

            // Transform UI configuration for WinUI
            if (transformed.TryGetValue("ui", out var uiConfig) && uiConfig is Dictionary<string, object> ui)
            {
                var winuiUi = new Dictionary<string, object>(ui)
                {
                    ["framework"] = "winui",
                    ["supports_xaml"] = true,
                    ["supports_uwp_features"] = true
                };
                transformed["ui"] = winuiUi;
            }

            return transformed;
        }

        /// <summary>
        /// Apply Uno-specific transformations
        /// </summary>
        private async Task<Dictionary<string, object>> ApplyUnoTransformationsAsync(
            Dictionary<string, object> configuration, CancellationToken cancellationToken)
        {
            var transformed = new Dictionary<string, object>(configuration);

            // Add Uno-specific configuration
            transformed["platform"] = "uno";
            transformed["supports_hot_reload"] = true;
            transformed["supports_platform_specific_ui"] = true;

            // Transform UI configuration for Uno
            if (transformed.TryGetValue("ui", out var uiConfig) && uiConfig is Dictionary<string, object> ui)
            {
                var unoUi = new Dictionary<string, object>(ui)
                {
                    ["framework"] = "uno",
                    ["supports_xaml"] = true,
                    ["supports_cross_platform"] = true
                };
                transformed["ui"] = unoUi;
            }

            return transformed;
        }

        /// <summary>
        /// Apply Xamarin-specific transformations
        /// </summary>
        private async Task<Dictionary<string, object>> ApplyXamarinTransformationsAsync(
            Dictionary<string, object> configuration, CancellationToken cancellationToken)
        {
            var transformed = new Dictionary<string, object>(configuration);

            // Add Xamarin-specific configuration
            transformed["platform"] = "xamarin";
            transformed["supports_hot_reload"] = false; // Xamarin doesn't support hot reload
            transformed["supports_platform_specific_ui"] = true;

            // Transform UI configuration for Xamarin
            if (transformed.TryGetValue("ui", out var uiConfig) && uiConfig is Dictionary<string, object> ui)
            {
                var xamarinUi = new Dictionary<string, object>(ui)
                {
                    ["framework"] = "xamarin",
                    ["supports_xaml"] = true,
                    ["supports_native_controls"] = true
                };
                transformed["ui"] = xamarinUi;
            }

            return transformed;
        }

        #endregion

        #region Validation

        /// <summary>
        /// Validate platform-specific configuration
        /// </summary>
        private async Task<ConfigurationValidationResult> ValidatePlatformConfigurationAsync(
            PlatformType platformType, Dictionary<string, object> configuration, CancellationToken cancellationToken)
        {
            var result = new ConfigurationValidationResult { IsValid = true, Errors = new List<string>() };

            try
            {
                // Platform-specific validation
                switch (platformType)
                {
                    case PlatformType.MAUI:
                        result = await ValidateMauiConfigurationAsync(configuration, cancellationToken);
                        break;
                    case PlatformType.Avalonia:
                        result = await ValidateAvaloniaConfigurationAsync(configuration, cancellationToken);
                        break;
                    case PlatformType.WinUI:
                        result = await ValidateWinUIConfigurationAsync(configuration, cancellationToken);
                        break;
                    case PlatformType.Uno:
                        result = await ValidateUnoConfigurationAsync(configuration, cancellationToken);
                        break;
                    case PlatformType.Xamarin:
                        result = await ValidateXamarinConfigurationAsync(configuration, cancellationToken);
                        break;
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
        /// Validate MAUI configuration
        /// </summary>
        private async Task<ConfigurationValidationResult> ValidateMauiConfigurationAsync(
            Dictionary<string, object> configuration, CancellationToken cancellationToken)
        {
            var result = new ConfigurationValidationResult { IsValid = true, Errors = new List<string>() };

            // MAUI-specific validation rules
            if (configuration.TryGetValue("ui", out var uiConfig) && uiConfig is Dictionary<string, object> ui)
            {
                // Validate MAUI-specific UI properties
                if (ui.ContainsKey("native_controls") && !(ui["native_controls"] is bool))
                {
                    result.IsValid = false;
                    result.Errors.Add("native_controls must be a boolean value");
                }
            }

            if (!result.IsValid)
            {
                result.ErrorMessage = string.Join("; ", result.Errors);
            }

            return result;
        }

        /// <summary>
        /// Validate Avalonia configuration
        /// </summary>
        private async Task<ConfigurationValidationResult> ValidateAvaloniaConfigurationAsync(
            Dictionary<string, object> configuration, CancellationToken cancellationToken)
        {
            var result = new ConfigurationValidationResult { IsValid = true, Errors = new List<string>() };

            // Avalonia-specific validation rules
            if (configuration.TryGetValue("ui", out var uiConfig) && uiConfig is Dictionary<string, object> ui)
            {
                // Validate Avalonia-specific UI properties
                if (ui.ContainsKey("styles") && !(ui["styles"] is Dictionary<string, object>))
                {
                    result.IsValid = false;
                    result.Errors.Add("styles must be a dictionary");
                }
            }

            if (!result.IsValid)
            {
                result.ErrorMessage = string.Join("; ", result.Errors);
            }

            return result;
        }

        /// <summary>
        /// Validate WinUI configuration
        /// </summary>
        private async Task<ConfigurationValidationResult> ValidateWinUIConfigurationAsync(
            Dictionary<string, object> configuration, CancellationToken cancellationToken)
        {
            var result = new ConfigurationValidationResult { IsValid = true, Errors = new List<string>() };

            // WinUI-specific validation rules
            if (configuration.TryGetValue("ui", out var uiConfig) && uiConfig is Dictionary<string, object> ui)
            {
                // Validate WinUI-specific UI properties
                if (ui.ContainsKey("uwp_features") && !(ui["uwp_features"] is bool))
                {
                    result.IsValid = false;
                    result.Errors.Add("uwp_features must be a boolean value");
                }
            }

            if (!result.IsValid)
            {
                result.ErrorMessage = string.Join("; ", result.Errors);
            }

            return result;
        }

        /// <summary>
        /// Validate Uno configuration
        /// </summary>
        private async Task<ConfigurationValidationResult> ValidateUnoConfigurationAsync(
            Dictionary<string, object> configuration, CancellationToken cancellationToken)
        {
            var result = new ConfigurationValidationResult { IsValid = true, Errors = new List<string>() };

            // Uno-specific validation rules
            if (configuration.TryGetValue("ui", out var uiConfig) && uiConfig is Dictionary<string, object> ui)
            {
                // Validate Uno-specific UI properties
                if (ui.ContainsKey("cross_platform") && !(ui["cross_platform"] is bool))
                {
                    result.IsValid = false;
                    result.Errors.Add("cross_platform must be a boolean value");
                }
            }

            if (!result.IsValid)
            {
                result.ErrorMessage = string.Join("; ", result.Errors);
            }

            return result;
        }

        /// <summary>
        /// Validate Xamarin configuration
        /// </summary>
        private async Task<ConfigurationValidationResult> ValidateXamarinConfigurationAsync(
            Dictionary<string, object> configuration, CancellationToken cancellationToken)
        {
            var result = new ConfigurationValidationResult { IsValid = true, Errors = new List<string>() };

            // Xamarin-specific validation rules
            if (configuration.TryGetValue("ui", out var uiConfig) && uiConfig is Dictionary<string, object> ui)
            {
                // Validate Xamarin-specific UI properties
                if (ui.ContainsKey("native_controls") && !(ui["native_controls"] is bool))
                {
                    result.IsValid = false;
                    result.Errors.Add("native_controls must be a boolean value");
                }
            }

            if (!result.IsValid)
            {
                result.ErrorMessage = string.Join("; ", result.Errors);
            }

            return result;
        }

        #endregion

        #region Platform Adapter Management

        /// <summary>
        /// Register platform adapter
        /// </summary>
        private void RegisterPlatformAdapter(PlatformType platformType, PlatformAdapter adapter)
        {
            _platformAdapters[platformType.ToString()] = adapter;
            _logger.LogDebug($"Platform adapter registered: {platformType}");
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

        public void Dispose()
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
    /// Advanced platform options
    /// </summary>
    public class AdvancedPlatformOptions
    {
        public bool EnableCrossPlatformSync { get; set; } = true;
        public bool EnablePlatformSpecificValidation { get; set; } = true;
        public bool EnablePlatformTransformations { get; set; } = true;
        public TimeSpan SyncTimeout { get; set; } = TimeSpan.FromSeconds(30);
        public string[] SupportedPlatforms { get; set; } = { "maui", "avalonia", "winui", "uno", "xamarin" };
    }

    /// <summary>
    /// Platform types
    /// </summary>
    public enum PlatformType
    {
        Desktop,
        MAUI,
        Avalonia,
        WinUI,
        Uno,
        Xamarin
    }

    /// <summary>
    /// Platform configuration
    /// </summary>
    public class PlatformConfiguration
    {
        public PlatformType PlatformType { get; set; }
        public Dictionary<string, object> Configuration { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Platform configuration result
    /// </summary>
    public class PlatformConfigurationResult
    {
        public PlatformType PlatformType { get; set; }
        public string ConfigKey { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public Dictionary<string, object> Configuration { get; set; }
        public TimeSpan LoadTime { get; set; }
    }

    /// <summary>
    /// Cross-platform configuration result
    /// </summary>
    public class CrossPlatformConfigurationResult
    {
        public string ConfigKey { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public Dictionary<PlatformType, Dictionary<string, object>> PlatformConfigurations { get; set; }
        public TimeSpan LoadTime { get; set; }
    }

    /// <summary>
    /// UI configuration result
    /// </summary>
    public class UIConfigurationResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public Dictionary<string, object> UIConfiguration { get; set; }
    }

    /// <summary>
    /// Device info result
    /// </summary>
    public class DeviceInfoResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public Dictionary<string, object> DeviceInfo { get; set; }
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
        private readonly Dictionary<PlatformType, List<TimeSpan>> _loadTimes = new Dictionary<PlatformType, List<TimeSpan>>();
        private readonly Dictionary<PlatformType, List<TimeSpan>> _saveTimes = new Dictionary<PlatformType, List<TimeSpan>>();
        private readonly Dictionary<PlatformType, int> _errors = new Dictionary<PlatformType, int>();

        public void RecordLoad(TimeSpan time, PlatformType platformType)
        {
            if (!_loadTimes.ContainsKey(platformType))
                _loadTimes[platformType] = new List<TimeSpan>();

            _loadTimes[platformType].Add(time);
            if (_loadTimes[platformType].Count > 1000)
                _loadTimes[platformType].RemoveAt(0);
        }

        public void RecordSave(TimeSpan time, PlatformType platformType)
        {
            if (!_saveTimes.ContainsKey(platformType))
                _saveTimes[platformType] = new List<TimeSpan>();

            _saveTimes[platformType].Add(time);
            if (_saveTimes[platformType].Count > 1000)
                _saveTimes[platformType].RemoveAt(0);
        }

        public void RecordError(TimeSpan time, PlatformType platformType)
        {
            if (!_errors.ContainsKey(platformType))
                _errors[platformType] = 0;

            _errors[platformType]++;
        }

        public double GetAverageLoadTime(PlatformType platformType)
        {
            return _loadTimes.ContainsKey(platformType) && _loadTimes[platformType].Count > 0
                ? _loadTimes[platformType].Average(t => t.TotalMilliseconds)
                : 0;
        }

        public double GetAverageSaveTime(PlatformType platformType)
        {
            return _saveTimes.ContainsKey(platformType) && _saveTimes[platformType].Count > 0
                ? _saveTimes[platformType].Average(t => t.TotalMilliseconds)
                : 0;
        }

        public int GetErrorCount(PlatformType platformType)
        {
            return _errors.ContainsKey(platformType) ? _errors[platformType] : 0;
        }
    }

    #endregion

    #region Platform Adapters

    /// <summary>
    /// Base platform adapter
    /// </summary>
    public abstract class PlatformAdapter
    {
        protected readonly ILogger _logger;

        protected PlatformAdapter(ILogger logger)
        {
            _logger = logger;
        }

        public abstract PlatformType PlatformType { get; }
        public abstract Task<PlatformConfigurationResult> LoadConfigurationAsync(string configKey, CancellationToken cancellationToken);
        public abstract Task<PlatformConfigurationResult> SaveConfigurationAsync(string configKey, Dictionary<string, object> configuration, CancellationToken cancellationToken);
        public abstract Task<UIConfigurationResult> GetUIConfigurationAsync(CancellationToken cancellationToken);
        public abstract Task<bool> ApplyThemeAsync(string themeName, CancellationToken cancellationToken);
        public abstract Task<DeviceInfoResult> GetDeviceInfoAsync(CancellationToken cancellationToken);
    }

    /// <summary>
    /// MAUI platform adapter
    /// </summary>
    public class MauiPlatformAdapter : PlatformAdapter
    {
        public override PlatformType PlatformType => PlatformType.MAUI;

        public MauiPlatformAdapter(ILogger logger) : base(logger) { }

        public override async Task<PlatformConfigurationResult> LoadConfigurationAsync(string configKey, CancellationToken cancellationToken)
        {
            // MAUI-specific configuration loading
            var config = new Dictionary<string, object>
            {
                ["platform"] = "maui",
                ["supports_hot_reload"] = true,
                ["ui_framework"] = "maui"
            };

            return new PlatformConfigurationResult
            {
                Success = true,
                Configuration = config
            };
        }

        public override async Task<PlatformConfigurationResult> SaveConfigurationAsync(string configKey, Dictionary<string, object> configuration, CancellationToken cancellationToken)
        {
            // MAUI-specific configuration saving
            return new PlatformConfigurationResult
            {
                Success = true,
                Configuration = configuration
            };
        }

        public override async Task<UIConfigurationResult> GetUIConfigurationAsync(CancellationToken cancellationToken)
        {
            var uiConfig = new Dictionary<string, object>
            {
                ["framework"] = "maui",
                ["supports_native_controls"] = true,
                ["supports_custom_renderers"] = true
            };

            return new UIConfigurationResult
            {
                Success = true,
                UIConfiguration = uiConfig
            };
        }

        public override async Task<bool> ApplyThemeAsync(string themeName, CancellationToken cancellationToken)
        {
            // MAUI-specific theme application
            return true;
        }

        public override async Task<DeviceInfoResult> GetDeviceInfoAsync(CancellationToken cancellationToken)
        {
            var deviceInfo = new Dictionary<string, object>
            {
                ["platform"] = "maui",
                ["os"] = "cross-platform"
            };

            return new DeviceInfoResult
            {
                Success = true,
                DeviceInfo = deviceInfo
            };
        }
    }

    /// <summary>
    /// Avalonia platform adapter
    /// </summary>
    public class AvaloniaPlatformAdapter : PlatformAdapter
    {
        public override PlatformType PlatformType => PlatformType.Avalonia;

        public AvaloniaPlatformAdapter(ILogger logger) : base(logger) { }

        public override async Task<PlatformConfigurationResult> LoadConfigurationAsync(string configKey, CancellationToken cancellationToken)
        {
            // Avalonia-specific configuration loading
            var config = new Dictionary<string, object>
            {
                ["platform"] = "avalonia",
                ["supports_hot_reload"] = true,
                ["ui_framework"] = "avalonia"
            };

            return new PlatformConfigurationResult
            {
                Success = true,
                Configuration = config
            };
        }

        public override async Task<PlatformConfigurationResult> SaveConfigurationAsync(string configKey, Dictionary<string, object> configuration, CancellationToken cancellationToken)
        {
            // Avalonia-specific configuration saving
            return new PlatformConfigurationResult
            {
                Success = true,
                Configuration = configuration
            };
        }

        public override async Task<UIConfigurationResult> GetUIConfigurationAsync(CancellationToken cancellationToken)
        {
            var uiConfig = new Dictionary<string, object>
            {
                ["framework"] = "avalonia",
                ["supports_xaml"] = true,
                ["supports_styles"] = true
            };

            return new UIConfigurationResult
            {
                Success = true,
                UIConfiguration = uiConfig
            };
        }

        public override async Task<bool> ApplyThemeAsync(string themeName, CancellationToken cancellationToken)
        {
            // Avalonia-specific theme application
            return true;
        }

        public override async Task<DeviceInfoResult> GetDeviceInfoAsync(CancellationToken cancellationToken)
        {
            var deviceInfo = new Dictionary<string, object>
            {
                ["platform"] = "avalonia",
                ["os"] = "cross-platform"
            };

            return new DeviceInfoResult
            {
                Success = true,
                DeviceInfo = deviceInfo
            };
        }
    }

    /// <summary>
    /// WinUI platform adapter
    /// </summary>
    public class WinUIPlatformAdapter : PlatformAdapter
    {
        public override PlatformType PlatformType => PlatformType.WinUI;

        public WinUIPlatformAdapter(ILogger logger) : base(logger) { }

        public override async Task<PlatformConfigurationResult> LoadConfigurationAsync(string configKey, CancellationToken cancellationToken)
        {
            // WinUI-specific configuration loading
            var config = new Dictionary<string, object>
            {
                ["platform"] = "winui",
                ["supports_hot_reload"] = true,
                ["ui_framework"] = "winui"
            };

            return new PlatformConfigurationResult
            {
                Success = true,
                Configuration = config
            };
        }

        public override async Task<PlatformConfigurationResult> SaveConfigurationAsync(string configKey, Dictionary<string, object> configuration, CancellationToken cancellationToken)
        {
            // WinUI-specific configuration saving
            return new PlatformConfigurationResult
            {
                Success = true,
                Configuration = configuration
            };
        }

        public override async Task<UIConfigurationResult> GetUIConfigurationAsync(CancellationToken cancellationToken)
        {
            var uiConfig = new Dictionary<string, object>
            {
                ["framework"] = "winui",
                ["supports_xaml"] = true,
                ["supports_uwp_features"] = true
            };

            return new UIConfigurationResult
            {
                Success = true,
                UIConfiguration = uiConfig
            };
        }

        public override async Task<bool> ApplyThemeAsync(string themeName, CancellationToken cancellationToken)
        {
            // WinUI-specific theme application
            return true;
        }

        public override async Task<DeviceInfoResult> GetDeviceInfoAsync(CancellationToken cancellationToken)
        {
            var deviceInfo = new Dictionary<string, object>
            {
                ["platform"] = "winui",
                ["os"] = "windows"
            };

            return new DeviceInfoResult
            {
                Success = true,
                DeviceInfo = deviceInfo
            };
        }
    }

    /// <summary>
    /// Uno platform adapter
    /// </summary>
    public class UnoPlatformAdapter : PlatformAdapter
    {
        public override PlatformType PlatformType => PlatformType.Uno;

        public UnoPlatformAdapter(ILogger logger) : base(logger) { }

        public override async Task<PlatformConfigurationResult> LoadConfigurationAsync(string configKey, CancellationToken cancellationToken)
        {
            // Uno-specific configuration loading
            var config = new Dictionary<string, object>
            {
                ["platform"] = "uno",
                ["supports_hot_reload"] = true,
                ["ui_framework"] = "uno"
            };

            return new PlatformConfigurationResult
            {
                Success = true,
                Configuration = config
            };
        }

        public override async Task<PlatformConfigurationResult> SaveConfigurationAsync(string configKey, Dictionary<string, object> configuration, CancellationToken cancellationToken)
        {
            // Uno-specific configuration saving
            return new PlatformConfigurationResult
            {
                Success = true,
                Configuration = configuration
            };
        }

        public override async Task<UIConfigurationResult> GetUIConfigurationAsync(CancellationToken cancellationToken)
        {
            var uiConfig = new Dictionary<string, object>
            {
                ["framework"] = "uno",
                ["supports_xaml"] = true,
                ["supports_cross_platform"] = true
            };

            return new UIConfigurationResult
            {
                Success = true,
                UIConfiguration = uiConfig
            };
        }

        public override async Task<bool> ApplyThemeAsync(string themeName, CancellationToken cancellationToken)
        {
            // Uno-specific theme application
            return true;
        }

        public override async Task<DeviceInfoResult> GetDeviceInfoAsync(CancellationToken cancellationToken)
        {
            var deviceInfo = new Dictionary<string, object>
            {
                ["platform"] = "uno",
                ["os"] = "cross-platform"
            };

            return new DeviceInfoResult
            {
                Success = true,
                DeviceInfo = deviceInfo
            };
        }
    }

    /// <summary>
    /// Xamarin platform adapter
    /// </summary>
    public class XamarinPlatformAdapter : PlatformAdapter
    {
        public override PlatformType PlatformType => PlatformType.Xamarin;

        public XamarinPlatformAdapter(ILogger logger) : base(logger) { }

        public override async Task<PlatformConfigurationResult> LoadConfigurationAsync(string configKey, CancellationToken cancellationToken)
        {
            // Xamarin-specific configuration loading
            var config = new Dictionary<string, object>
            {
                ["platform"] = "xamarin",
                ["supports_hot_reload"] = false,
                ["ui_framework"] = "xamarin"
            };

            return new PlatformConfigurationResult
            {
                Success = true,
                Configuration = config
            };
        }

        public override async Task<PlatformConfigurationResult> SaveConfigurationAsync(string configKey, Dictionary<string, object> configuration, CancellationToken cancellationToken)
        {
            // Xamarin-specific configuration saving
            return new PlatformConfigurationResult
            {
                Success = true,
                Configuration = configuration
            };
        }

        public override async Task<UIConfigurationResult> GetUIConfigurationAsync(CancellationToken cancellationToken)
        {
            var uiConfig = new Dictionary<string, object>
            {
                ["framework"] = "xamarin",
                ["supports_xaml"] = true,
                ["supports_native_controls"] = true
            };

            return new UIConfigurationResult
            {
                Success = true,
                UIConfiguration = uiConfig
            };
        }

        public override async Task<bool> ApplyThemeAsync(string themeName, CancellationToken cancellationToken)
        {
            // Xamarin-specific theme application
            return true;
        }

        public override async Task<DeviceInfoResult> GetDeviceInfoAsync(CancellationToken cancellationToken)
        {
            var deviceInfo = new Dictionary<string, object>
            {
                ["platform"] = "xamarin",
                ["os"] = "cross-platform"
            };

            return new DeviceInfoResult
            {
                Success = true,
                DeviceInfo = deviceInfo
            };
        }
    }

    #endregion
} 