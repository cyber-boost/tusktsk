using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;

namespace TuskLang.Framework.Advanced
{
    /// <summary>
    /// Production-ready hot reload configuration support for TuskTsk
    /// Provides live configuration updates without application restart
    /// </summary>
    public class HotReloadConfigurationSupport : IDisposable
    {
        private readonly ILogger<HotReloadConfigurationSupport> _logger;
        private readonly IConfigurationRoot _configuration;
        private readonly ConcurrentDictionary<string, ConfigurationChangeToken> _changeTokens;
        private readonly ConcurrentDictionary<string, ConfigurationSnapshot> _snapshots;
        private readonly FileSystemWatcher _fileWatcher;
        private readonly Timer _healthCheckTimer;
        private readonly SemaphoreSlim _reloadSemaphore;
        private readonly string _configDirectory;
        private readonly HotReloadOptions _options;
        private readonly PerformanceMetrics _metrics;
        private bool _disposed = false;

        public HotReloadConfigurationSupport(
            IConfigurationRoot configuration,
            string configDirectory,
            HotReloadOptions options = null,
            ILogger<HotReloadConfigurationSupport> logger = null)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _configDirectory = configDirectory ?? throw new ArgumentNullException(nameof(configDirectory));
            _options = options ?? new HotReloadOptions();
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<HotReloadConfigurationSupport>.Instance;
            
            _changeTokens = new ConcurrentDictionary<string, ConfigurationChangeToken>();
            _snapshots = new ConcurrentDictionary<string, ConfigurationSnapshot>();
            _reloadSemaphore = new SemaphoreSlim(1, 1);
            _metrics = new PerformanceMetrics();

            // Initialize file watcher
            _fileWatcher = new FileSystemWatcher(_configDirectory)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                Filter = "*.tsk",
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };

            _fileWatcher.Changed += OnConfigurationFileChanged;
            _fileWatcher.Created += OnConfigurationFileChanged;
            _fileWatcher.Deleted += OnConfigurationFileChanged;
            _fileWatcher.Renamed += OnConfigurationFileRenamed;

            // Start health check timer
            _healthCheckTimer = new Timer(PerformHealthCheck, null, 
                TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

            _logger.LogInformation($"Hot reload configuration support initialized for directory: {_configDirectory}");
        }

        #region Hot Reload Operations

        /// <summary>
        /// Enable hot reload for a specific configuration file
        /// </summary>
        public async Task<bool> EnableHotReloadAsync(string filePath, CancellationToken cancellationToken = default)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning($"Configuration file not found: {filePath}");
                    return false;
                }

                // Create change token for the file
                var changeToken = new ConfigurationChangeToken(filePath);
                _changeTokens[filePath] = changeToken;

                // Take initial snapshot
                var snapshot = await CreateSnapshotAsync(filePath, cancellationToken);
                _snapshots[filePath] = snapshot;

                // Register with configuration system
                var changeTokenSource = new CancellationTokenSource();
                var reloadToken = new ConfigurationReloadToken();
                
                _configuration.GetReloadToken().RegisterChangeCallback(_ => 
                {
                    _ = Task.Run(async () => await ReloadConfigurationAsync(filePath, cancellationToken));
                }, null);

                stopwatch.Stop();
                _metrics.RecordHotReloadEnable(stopwatch.Elapsed);
                
                _logger.LogInformation($"Hot reload enabled for {filePath} in {stopwatch.ElapsedMilliseconds}ms");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to enable hot reload for {filePath}");
                return false;
            }
        }

        /// <summary>
        /// Disable hot reload for a specific configuration file
        /// </summary>
        public async Task<bool> DisableHotReloadAsync(string filePath)
        {
            try
            {
                _changeTokens.TryRemove(filePath, out _);
                _snapshots.TryRemove(filePath, out _);
                
                _logger.LogInformation($"Hot reload disabled for {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to disable hot reload for {filePath}");
                return false;
            }
        }

        /// <summary>
        /// Perform hot reload of configuration
        /// </summary>
        public async Task<HotReloadResult> ReloadConfigurationAsync(string filePath, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new HotReloadResult { FilePath = filePath, Success = false };

            try
            {
                await _reloadSemaphore.WaitAsync(cancellationToken);

                // Validate file exists
                if (!File.Exists(filePath))
                {
                    result.ErrorMessage = "Configuration file not found";
                    return result;
                }

                // Create new snapshot
                var newSnapshot = await CreateSnapshotAsync(filePath, cancellationToken);
                
                // Validate configuration
                var validationResult = await ValidateConfigurationAsync(newSnapshot, cancellationToken);
                if (!validationResult.IsValid)
                {
                    result.ErrorMessage = $"Configuration validation failed: {validationResult.ErrorMessage}";
                    result.ValidationErrors = validationResult.Errors;
                    return result;
                }

                // Check for conflicts with existing configuration
                var conflictResult = await CheckConfigurationConflictsAsync(newSnapshot, cancellationToken);
                if (conflictResult.HasConflicts)
                {
                    result.ErrorMessage = $"Configuration conflicts detected: {conflictResult.ConflictMessage}";
                    result.Conflicts = conflictResult.Conflicts;
                    return result;
                }

                // Take backup of current configuration
                var backupSnapshot = _snapshots.GetValueOrDefault(filePath);
                if (backupSnapshot != null)
                {
                    await CreateBackupAsync(filePath, backupSnapshot, cancellationToken);
                }

                // Apply new configuration
                var applyResult = await ApplyConfigurationAsync(newSnapshot, cancellationToken);
                if (!applyResult.Success)
                {
                    // Rollback if apply failed
                    if (backupSnapshot != null)
                    {
                        await RollbackConfigurationAsync(filePath, backupSnapshot, cancellationToken);
                    }
                    
                    result.ErrorMessage = $"Failed to apply configuration: {applyResult.ErrorMessage}";
                    return result;
                }

                // Update snapshot
                _snapshots[filePath] = newSnapshot;

                // Notify change listeners
                await NotifyConfigurationChangedAsync(filePath, newSnapshot, cancellationToken);

                stopwatch.Stop();
                result.Success = true;
                result.ReloadTime = stopwatch.Elapsed;
                result.ChangesApplied = newSnapshot.Changes.Count;

                _metrics.RecordHotReload(stopwatch.Elapsed, true);
                _logger.LogInformation($"Configuration hot reload completed for {filePath} in {stopwatch.ElapsedMilliseconds}ms");

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.ErrorMessage = ex.Message;
                result.ReloadTime = stopwatch.Elapsed;
                
                _metrics.RecordHotReload(stopwatch.Elapsed, false);
                _logger.LogError(ex, $"Hot reload failed for {filePath}");
                
                return result;
            }
            finally
            {
                _reloadSemaphore.Release();
            }
        }

        /// <summary>
        /// Rollback configuration to previous state
        /// </summary>
        public async Task<RollbackResult> RollbackConfigurationAsync(string filePath, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new RollbackResult { FilePath = filePath, Success = false };

            try
            {
                await _reloadSemaphore.WaitAsync(cancellationToken);

                // Find backup snapshot
                var backupPath = GetBackupPath(filePath);
                if (!File.Exists(backupPath))
                {
                    result.ErrorMessage = "No backup found for rollback";
                    return result;
                }

                // Load backup configuration
                var backupSnapshot = await LoadBackupAsync(backupPath, cancellationToken);
                if (backupSnapshot == null)
                {
                    result.ErrorMessage = "Failed to load backup configuration";
                    return result;
                }

                // Apply backup configuration
                var applyResult = await ApplyConfigurationAsync(backupSnapshot, cancellationToken);
                if (!applyResult.Success)
                {
                    result.ErrorMessage = $"Failed to apply backup: {applyResult.ErrorMessage}";
                    return result;
                }

                // Update current snapshot
                _snapshots[filePath] = backupSnapshot;

                // Notify rollback
                await NotifyConfigurationRollbackAsync(filePath, backupSnapshot, cancellationToken);

                stopwatch.Stop();
                result.Success = true;
                result.RollbackTime = stopwatch.Elapsed;

                _logger.LogInformation($"Configuration rollback completed for {filePath} in {stopwatch.ElapsedMilliseconds}ms");
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.ErrorMessage = ex.Message;
                result.RollbackTime = stopwatch.Elapsed;
                
                _logger.LogError(ex, $"Rollback failed for {filePath}");
                return result;
            }
            finally
            {
                _reloadSemaphore.Release();
            }
        }

        #endregion

        #region Configuration Validation

        /// <summary>
        /// Validate configuration before applying
        /// </summary>
        private async Task<ConfigurationValidationResult> ValidateConfigurationAsync(
            ConfigurationSnapshot snapshot, CancellationToken cancellationToken)
        {
            var result = new ConfigurationValidationResult { IsValid = true, Errors = new List<string>() };

            try
            {
                // Validate JSON syntax
                if (!IsValidJson(snapshot.Content))
                {
                    result.IsValid = false;
                    result.Errors.Add("Invalid JSON syntax");
                    return result;
                }

                // Validate required fields
                var requiredFields = _options.RequiredFields ?? new string[0];
                foreach (var field in requiredFields)
                {
                    if (!snapshot.Configuration.ContainsKey(field))
                    {
                        result.IsValid = false;
                        result.Errors.Add($"Required field '{field}' is missing");
                    }
                }

                // Validate field types
                var typeValidations = _options.TypeValidations ?? new Dictionary<string, Type>();
                foreach (var validation in typeValidations)
                {
                    if (snapshot.Configuration.TryGetValue(validation.Key, out var value))
                    {
                        if (value?.GetType() != validation.Value)
                        {
                            result.IsValid = false;
                            result.Errors.Add($"Field '{validation.Key}' must be of type {validation.Value.Name}");
                        }
                    }
                }

                // Custom validation callbacks
                if (_options.CustomValidators != null)
                {
                    foreach (var validator in _options.CustomValidators)
                    {
                        var validationResult = await validator(snapshot.Configuration, cancellationToken);
                        if (!validationResult.IsValid)
                        {
                            result.IsValid = false;
                            result.Errors.AddRange(validationResult.Errors);
                        }
                    }
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
        /// Check for configuration conflicts
        /// </summary>
        private async Task<ConfigurationConflictResult> CheckConfigurationConflictsAsync(
            ConfigurationSnapshot newSnapshot, CancellationToken cancellationToken)
        {
            var result = new ConfigurationConflictResult { HasConflicts = false, Conflicts = new List<ConfigurationConflict>() };

            try
            {
                // Check for duplicate keys
                var duplicateKeys = newSnapshot.Configuration
                    .GroupBy(kvp => kvp.Key)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key);

                foreach (var key in duplicateKeys)
                {
                    result.HasConflicts = true;
                    result.Conflicts.Add(new ConfigurationConflict
                    {
                        Type = ConflictType.DuplicateKey,
                        Key = key,
                        Message = $"Duplicate key found: {key}"
                    });
                }

                // Check for circular references
                var circularRefs = DetectCircularReferences(newSnapshot.Configuration);
                foreach (var circularRef in circularRefs)
                {
                    result.HasConflicts = true;
                    result.Conflicts.Add(new ConfigurationConflict
                    {
                        Type = ConflictType.CircularReference,
                        Key = circularRef,
                        Message = $"Circular reference detected: {circularRef}"
                    });
                }

                // Check for invalid references
                var invalidRefs = DetectInvalidReferences(newSnapshot.Configuration);
                foreach (var invalidRef in invalidRefs)
                {
                    result.HasConflicts = true;
                    result.Conflicts.Add(new ConfigurationConflict
                    {
                        Type = ConflictType.InvalidReference,
                        Key = invalidRef.Key,
                        Message = $"Invalid reference: {invalidRef.Value}"
                    });
                }

                if (result.HasConflicts)
                {
                    result.ConflictMessage = string.Join("; ", result.Conflicts.Select(c => c.Message));
                }

                return result;
            }
            catch (Exception ex)
            {
                result.HasConflicts = true;
                result.ConflictMessage = ex.Message;
                return result;
            }
        }

        #endregion

        #region File System Events

        /// <summary>
        /// Handle configuration file changes
        /// </summary>
        private async void OnConfigurationFileChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                // Debounce rapid file changes
                await Task.Delay(_options.FileChangeDebounceMs);
                
                if (_changeTokens.TryGetValue(e.FullPath, out var changeToken))
                {
                    changeToken.OnReload();
                    
                    _logger.LogDebug($"Configuration file changed: {e.FullPath}");
                    
                    // Trigger hot reload
                    _ = Task.Run(async () => 
                    {
                        await ReloadConfigurationAsync(e.FullPath, CancellationToken.None);
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error handling file change for {e.FullPath}");
            }
        }

        /// <summary>
        /// Handle configuration file renames
        /// </summary>
        private async void OnConfigurationFileRenamed(object sender, RenamedEventArgs e)
        {
            try
            {
                // Remove old file from tracking
                _changeTokens.TryRemove(e.OldFullPath, out _);
                _snapshots.TryRemove(e.OldFullPath, out _);

                // Add new file to tracking
                if (File.Exists(e.FullPath))
                {
                    await EnableHotReloadAsync(e.FullPath, CancellationToken.None);
                }

                _logger.LogInformation($"Configuration file renamed: {e.OldFullPath} -> {e.FullPath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error handling file rename: {e.OldFullPath} -> {e.FullPath}");
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Create configuration snapshot
        /// </summary>
        private async Task<ConfigurationSnapshot> CreateSnapshotAsync(string filePath, CancellationToken cancellationToken)
        {
            var content = await File.ReadAllTextAsync(filePath, cancellationToken);
            var configuration = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
            var checksum = ComputeChecksum(content);
            var changes = await DetectChangesAsync(filePath, configuration, cancellationToken);

            return new ConfigurationSnapshot
            {
                FilePath = filePath,
                Content = content,
                Configuration = configuration,
                Checksum = checksum,
                Timestamp = DateTime.UtcNow,
                Changes = changes
            };
        }

        /// <summary>
        /// Apply configuration changes
        /// </summary>
        private async Task<ConfigurationApplyResult> ApplyConfigurationAsync(
            ConfigurationSnapshot snapshot, CancellationToken cancellationToken)
        {
            var result = new ConfigurationApplyResult { Success = false };

            try
            {
                // Update configuration providers
                var providers = _configuration.Providers.ToList();
                foreach (var provider in providers)
                {
                    if (provider is FileConfigurationProvider fileProvider)
                    {
                        if (fileProvider.Source.Path == snapshot.FilePath)
                        {
                            await fileProvider.LoadAsync();
                            break;
                        }
                    }
                }

                // Trigger configuration reload
                _configuration.Reload();

                result.Success = true;
                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        /// <summary>
        /// Detect configuration changes
        /// </summary>
        private async Task<List<ConfigurationChange>> DetectChangesAsync(
            string filePath, Dictionary<string, object> newConfig, CancellationToken cancellationToken)
        {
            var changes = new List<ConfigurationChange>();

            if (_snapshots.TryGetValue(filePath, out var oldSnapshot))
            {
                var oldConfig = oldSnapshot.Configuration;

                // Find added keys
                var addedKeys = newConfig.Keys.Except(oldConfig.Keys);
                foreach (var key in addedKeys)
                {
                    changes.Add(new ConfigurationChange
                    {
                        Type = ChangeType.Added,
                        Key = key,
                        OldValue = null,
                        NewValue = newConfig[key]
                    });
                }

                // Find removed keys
                var removedKeys = oldConfig.Keys.Except(newConfig.Keys);
                foreach (var key in removedKeys)
                {
                    changes.Add(new ConfigurationChange
                    {
                        Type = ChangeType.Removed,
                        Key = key,
                        OldValue = oldConfig[key],
                        NewValue = null
                    });
                }

                // Find modified keys
                var commonKeys = newConfig.Keys.Intersect(oldConfig.Keys);
                foreach (var key in commonKeys)
                {
                    if (!Equals(oldConfig[key], newConfig[key]))
                    {
                        changes.Add(new ConfigurationChange
                        {
                            Type = ChangeType.Modified,
                            Key = key,
                            OldValue = oldConfig[key],
                            NewValue = newConfig[key]
                        });
                    }
                }
            }

            return changes;
        }

        /// <summary>
        /// Compute file checksum
        /// </summary>
        private string ComputeChecksum(string content)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
            return Convert.ToBase64String(hash);
        }

        /// <summary>
        /// Validate JSON syntax
        /// </summary>
        private bool IsValidJson(string content)
        {
            try
            {
                JsonDocument.Parse(content);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Detect circular references
        /// </summary>
        private List<string> DetectCircularReferences(Dictionary<string, object> configuration)
        {
            var visited = new HashSet<string>();
            var recursionStack = new HashSet<string>();
            var circularRefs = new List<string>();

            foreach (var key in configuration.Keys)
            {
                if (!visited.Contains(key))
                {
                    DetectCircularReferencesRecursive(key, configuration, visited, recursionStack, circularRefs);
                }
            }

            return circularRefs;
        }

        /// <summary>
        /// Recursive circular reference detection
        /// </summary>
        private void DetectCircularReferencesRecursive(
            string key, Dictionary<string, object> configuration, 
            HashSet<string> visited, HashSet<string> recursionStack, List<string> circularRefs)
        {
            visited.Add(key);
            recursionStack.Add(key);

            if (configuration.TryGetValue(key, out var value) && value is string strValue)
            {
                // Check if value references another key
                if (configuration.ContainsKey(strValue))
                {
                    if (recursionStack.Contains(strValue))
                    {
                        circularRefs.Add(key);
                    }
                    else if (!visited.Contains(strValue))
                    {
                        DetectCircularReferencesRecursive(strValue, configuration, visited, recursionStack, circularRefs);
                    }
                }
            }

            recursionStack.Remove(key);
        }

        /// <summary>
        /// Detect invalid references
        /// </summary>
        private List<KeyValuePair<string, string>> DetectInvalidReferences(Dictionary<string, object> configuration)
        {
            var invalidRefs = new List<KeyValuePair<string, string>>();

            foreach (var kvp in configuration)
            {
                if (kvp.Value is string strValue)
                {
                    // Check if value references another key that doesn't exist
                    if (strValue.StartsWith("${") && strValue.EndsWith("}"))
                    {
                        var referencedKey = strValue.Substring(2, strValue.Length - 3);
                        if (!configuration.ContainsKey(referencedKey))
                        {
                            invalidRefs.Add(new KeyValuePair<string, string>(kvp.Key, strValue));
                        }
                    }
                }
            }

            return invalidRefs;
        }

        #endregion

        #region Health Monitoring

        /// <summary>
        /// Perform health check
        /// </summary>
        private async void PerformHealthCheck(object state)
        {
            try
            {
                foreach (var kvp in _snapshots)
                {
                    var filePath = kvp.Key;
                    var snapshot = kvp.Value;

                    // Check if file still exists
                    if (!File.Exists(filePath))
                    {
                        _logger.LogWarning($"Configuration file no longer exists: {filePath}");
                        continue;
                    }

                    // Check if file has been modified
                    var currentChecksum = ComputeChecksum(await File.ReadAllTextAsync(filePath));
                    if (currentChecksum != snapshot.Checksum)
                    {
                        _logger.LogInformation($"Configuration file modified outside of hot reload: {filePath}");
                        await ReloadConfigurationAsync(filePath, CancellationToken.None);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during health check");
            }
        }

        #endregion

        #region Notification Methods

        /// <summary>
        /// Notify configuration changed
        /// </summary>
        private async Task NotifyConfigurationChangedAsync(string filePath, ConfigurationSnapshot snapshot, CancellationToken cancellationToken)
        {
            // Implementation would notify registered listeners
            await Task.CompletedTask;
        }

        /// <summary>
        /// Notify configuration rollback
        /// </summary>
        private async Task NotifyConfigurationRollbackAsync(string filePath, ConfigurationSnapshot snapshot, CancellationToken cancellationToken)
        {
            // Implementation would notify registered listeners
            await Task.CompletedTask;
        }

        #endregion

        #region Backup Methods

        /// <summary>
        /// Create backup
        /// </summary>
        private async Task CreateBackupAsync(string filePath, ConfigurationSnapshot snapshot, CancellationToken cancellationToken)
        {
            var backupPath = GetBackupPath(filePath);
            var backupData = new BackupData
            {
                FilePath = filePath,
                Content = snapshot.Content,
                Timestamp = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(backupData, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(backupPath, json, cancellationToken);
        }

        /// <summary>
        /// Load backup
        /// </summary>
        private async Task<ConfigurationSnapshot> LoadBackupAsync(string backupPath, CancellationToken cancellationToken)
        {
            var json = await File.ReadAllTextAsync(backupPath, cancellationToken);
            var backupData = JsonSerializer.Deserialize<BackupData>(json);
            
            return new ConfigurationSnapshot
            {
                FilePath = backupData.FilePath,
                Content = backupData.Content,
                Configuration = JsonSerializer.Deserialize<Dictionary<string, object>>(backupData.Content),
                Timestamp = backupData.Timestamp
            };
        }

        /// <summary>
        /// Get backup path
        /// </summary>
        private string GetBackupPath(string filePath)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var extension = Path.GetExtension(filePath);
            var backupDir = Path.Combine(_configDirectory, ".backups");
            
            Directory.CreateDirectory(backupDir);
            
            return Path.Combine(backupDir, $"{fileName}.backup{extension}");
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
                _fileWatcher?.Dispose();
                _healthCheckTimer?.Dispose();
                _reloadSemaphore?.Dispose();
                _disposed = true;
            }
        }
    }

    #region Supporting Classes

    /// <summary>
    /// Hot reload options
    /// </summary>
    public class HotReloadOptions
    {
        public int FileChangeDebounceMs { get; set; } = 500;
        public string[] RequiredFields { get; set; }
        public Dictionary<string, Type> TypeValidations { get; set; }
        public List<Func<Dictionary<string, object>, CancellationToken, Task<ConfigurationValidationResult>>> CustomValidators { get; set; }
        public bool EnableBackup { get; set; } = true;
        public bool EnableValidation { get; set; } = true;
        public bool EnableConflictDetection { get; set; } = true;
    }

    /// <summary>
    /// Configuration snapshot
    /// </summary>
    public class ConfigurationSnapshot
    {
        public string FilePath { get; set; }
        public string Content { get; set; }
        public Dictionary<string, object> Configuration { get; set; }
        public string Checksum { get; set; }
        public DateTime Timestamp { get; set; }
        public List<ConfigurationChange> Changes { get; set; } = new List<ConfigurationChange>();
    }

    /// <summary>
    /// Configuration change
    /// </summary>
    public class ConfigurationChange
    {
        public ChangeType Type { get; set; }
        public string Key { get; set; }
        public object OldValue { get; set; }
        public object NewValue { get; set; }
    }

    /// <summary>
    /// Change types
    /// </summary>
    public enum ChangeType
    {
        Added,
        Removed,
        Modified
    }

    /// <summary>
    /// Hot reload result
    /// </summary>
    public class HotReloadResult
    {
        public string FilePath { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public TimeSpan ReloadTime { get; set; }
        public int ChangesApplied { get; set; }
        public List<string> ValidationErrors { get; set; } = new List<string>();
        public List<ConfigurationConflict> Conflicts { get; set; } = new List<ConfigurationConflict>();
    }

    /// <summary>
    /// Rollback result
    /// </summary>
    public class RollbackResult
    {
        public string FilePath { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public TimeSpan RollbackTime { get; set; }
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
    /// Configuration conflict result
    /// </summary>
    public class ConfigurationConflictResult
    {
        public bool HasConflicts { get; set; }
        public string ConflictMessage { get; set; }
        public List<ConfigurationConflict> Conflicts { get; set; } = new List<ConfigurationConflict>();
    }

    /// <summary>
    /// Configuration conflict
    /// </summary>
    public class ConfigurationConflict
    {
        public ConflictType Type { get; set; }
        public string Key { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// Conflict types
    /// </summary>
    public enum ConflictType
    {
        DuplicateKey,
        CircularReference,
        InvalidReference
    }

    /// <summary>
    /// Configuration apply result
    /// </summary>
    public class ConfigurationApplyResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Backup data
    /// </summary>
    public class BackupData
    {
        public string FilePath { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Performance metrics
    /// </summary>
    public class PerformanceMetrics
    {
        private readonly List<TimeSpan> _hotReloadTimes = new List<TimeSpan>();
        private readonly List<TimeSpan> _enableTimes = new List<TimeSpan>();
        private int _totalHotReloads = 0;
        private int _successfulHotReloads = 0;
        private int _failedHotReloads = 0;

        public void RecordHotReload(TimeSpan time, bool success)
        {
            _hotReloadTimes.Add(time);
            _totalHotReloads++;
            
            if (success)
                _successfulHotReloads++;
            else
                _failedHotReloads++;

            if (_hotReloadTimes.Count > 1000)
                _hotReloadTimes.RemoveAt(0);
        }

        public void RecordHotReloadEnable(TimeSpan time)
        {
            _enableTimes.Add(time);
            if (_enableTimes.Count > 100)
                _enableTimes.RemoveAt(0);
        }

        public double AverageHotReloadTime => _hotReloadTimes.Count > 0 ? _hotReloadTimes.Average(t => t.TotalMilliseconds) : 0;
        public double AverageEnableTime => _enableTimes.Count > 0 ? _enableTimes.Average(t => t.TotalMilliseconds) : 0;
        public int TotalHotReloads => _totalHotReloads;
        public int SuccessfulHotReloads => _successfulHotReloads;
        public int FailedHotReloads => _failedHotReloads;
        public double SuccessRate => _totalHotReloads > 0 ? (double)_successfulHotReloads / _totalHotReloads : 0;
    }

    /// <summary>
    /// Configuration change token
    /// </summary>
    public class ConfigurationChangeToken : IChangeToken
    {
        private readonly string _filePath;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public ConfigurationChangeToken(string filePath)
        {
            _filePath = filePath;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public bool HasChanged => _cancellationTokenSource.Token.IsCancellationRequested;
        public bool ActiveChangeCallbacks => true;

        public IDisposable RegisterChangeCallback(Action<object> callback, object state)
        {
            return _cancellationTokenSource.Token.Register(() => callback(state));
        }

        public void OnReload()
        {
            _cancellationTokenSource.Cancel();
        }
    }

    #endregion
} 