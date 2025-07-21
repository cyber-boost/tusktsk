using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace TuskLang.CLI.Advanced
{
    /// <summary>
    /// Production-ready interactive configuration wizard for TuskTsk
    /// Provides wizard-based .tsk creation with real-time validation and guidance
    /// </summary>
    public class InteractiveConfigurationWizard : IDisposable
    {
        private readonly ILogger<InteractiveConfigurationWizard> _logger;
        private readonly ConfigurationTemplateManager _templateManager;
        private readonly ConfigurationValidator _validator;
        private readonly ConfigurationHistory _history;
        private readonly InteractiveWizardOptions _options;
        private readonly PerformanceMetrics _metrics;
        private bool _disposed = false;

        public InteractiveConfigurationWizard(
            InteractiveWizardOptions options = null,
            ILogger<InteractiveConfigurationWizard> logger = null)
        {
            _options = options ?? new InteractiveWizardOptions();
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<InteractiveConfigurationWizard>.Instance;
            
            _templateManager = new ConfigurationTemplateManager(_logger);
            _validator = new ConfigurationValidator(_logger);
            _history = new ConfigurationHistory();
            _metrics = new PerformanceMetrics();

            _logger.LogInformation("Interactive configuration wizard initialized");
        }

        #region Wizard Operations

        /// <summary>
        /// Start interactive configuration wizard
        /// </summary>
        public async Task<WizardResult> StartWizardAsync(string outputPath = null, CancellationToken cancellationToken = default)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = new WizardResult { Success = false };

            try
            {
                _logger.LogInformation("Starting interactive configuration wizard");

                // Initialize wizard state
                var wizardState = new WizardState
                {
                    CurrentStep = 0,
                    Configuration = new Dictionary<string, object>(),
                    OutputPath = outputPath ?? GetDefaultOutputPath(),
                    Templates = await _templateManager.GetAvailableTemplatesAsync(cancellationToken)
                };

                // Display welcome message
                DisplayWelcomeMessage();

                // Run wizard steps
                var stepResult = await RunWizardStepsAsync(wizardState, cancellationToken);
                if (!stepResult.Success)
                {
                    result.ErrorMessage = stepResult.ErrorMessage;
                    return result;
                }

                // Validate final configuration
                var validationResult = await _validator.ValidateConfigurationAsync(wizardState.Configuration, cancellationToken);
                if (!validationResult.IsValid)
                {
                    result.ErrorMessage = $"Configuration validation failed: {validationResult.ErrorMessage}";
                    result.ValidationErrors = validationResult.Errors;
                    return result;
                }

                // Save configuration
                var saveResult = await SaveConfigurationAsync(wizardState, cancellationToken);
                if (!saveResult.Success)
                {
                    result.ErrorMessage = saveResult.ErrorMessage;
                    return result;
                }

                stopwatch.Stop();
                result.Success = true;
                result.Configuration = wizardState.Configuration;
                result.OutputPath = wizardState.OutputPath;
                result.CreationTime = stopwatch.Elapsed;

                _metrics.RecordWizardCompletion(stopwatch.Elapsed);
                _logger.LogInformation($"Configuration wizard completed successfully: {wizardState.OutputPath}");

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.ErrorMessage = ex.Message;
                result.CreationTime = stopwatch.Elapsed;
                
                _metrics.RecordError(stopwatch.Elapsed);
                _logger.LogError(ex, "Configuration wizard failed");
                return result;
            }
        }

        /// <summary>
        /// Edit existing configuration interactively
        /// </summary>
        public async Task<WizardResult> EditConfigurationAsync(string configPath, CancellationToken cancellationToken = default)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = new WizardResult { Success = false };

            try
            {
                if (!File.Exists(configPath))
                {
                    result.ErrorMessage = $"Configuration file not found: {configPath}";
                    return result;
                }

                // Load existing configuration
                var existingConfig = await LoadConfigurationAsync(configPath, cancellationToken);
                if (existingConfig == null)
                {
                    result.ErrorMessage = $"Failed to load configuration: {configPath}";
                    return result;
                }

                // Initialize wizard state with existing configuration
                var wizardState = new WizardState
                {
                    CurrentStep = 0,
                    Configuration = existingConfig,
                    OutputPath = configPath,
                    IsEditMode = true,
                    Templates = await _templateManager.GetAvailableTemplatesAsync(cancellationToken)
                };

                // Display edit mode message
                DisplayEditModeMessage(configPath);

                // Run wizard steps in edit mode
                var stepResult = await RunWizardStepsAsync(wizardState, cancellationToken);
                if (!stepResult.Success)
                {
                    result.ErrorMessage = stepResult.ErrorMessage;
                    return result;
                }

                // Save updated configuration
                var saveResult = await SaveConfigurationAsync(wizardState, cancellationToken);
                if (!saveResult.Success)
                {
                    result.ErrorMessage = saveResult.ErrorMessage;
                    return result;
                }

                stopwatch.Stop();
                result.Success = true;
                result.Configuration = wizardState.Configuration;
                result.OutputPath = wizardState.OutputPath;
                result.CreationTime = stopwatch.Elapsed;

                _metrics.RecordWizardCompletion(stopwatch.Elapsed);
                _logger.LogInformation($"Configuration edit completed: {configPath}");

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.ErrorMessage = ex.Message;
                result.CreationTime = stopwatch.Elapsed;
                
                _metrics.RecordError(stopwatch.Elapsed);
                _logger.LogError(ex, $"Configuration edit failed: {configPath}");
                return result;
            }
        }

        #endregion

        #region Wizard Steps

        /// <summary>
        /// Run wizard steps
        /// </summary>
        private async Task<StepResult> RunWizardStepsAsync(WizardState state, CancellationToken cancellationToken)
        {
            var steps = new List<Func<WizardState, CancellationToken, Task<StepResult>>>
            {
                StepTemplateSelection,
                StepBasicConfiguration,
                StepAdvancedConfiguration,
                StepValidationAndReview
            };

            foreach (var step in steps)
            {
                state.CurrentStep++;
                
                var stepResult = await step(state, cancellationToken);
                if (!stepResult.Success)
                {
                    return stepResult;
                }

                // Check for cancellation
                if (cancellationToken.IsCancellationRequested)
                {
                    return new StepResult { Success = false, ErrorMessage = "Wizard cancelled by user" };
                }
            }

            return new StepResult { Success = true };
        }

        /// <summary>
        /// Step 1: Template selection
        /// </summary>
        private async Task<StepResult> StepTemplateSelection(WizardState state, CancellationToken cancellationToken)
        {
            try
            {
                DisplayStepHeader("Template Selection", "Choose a configuration template to get started");

                // Display available templates
                DisplayTemplates(state.Templates);

                // Get user selection
                var selection = await GetUserSelectionAsync("Select template (or 'custom' for empty): ", cancellationToken);
                
                if (selection.ToLower() == "custom")
                {
                    state.Configuration["template"] = "custom";
                    state.Configuration["name"] = "Custom Configuration";
                }
                else
                {
                    var template = state.Templates.FirstOrDefault(t => t.Id == selection);
                    if (template == null)
                    {
                        return new StepResult { Success = false, ErrorMessage = "Invalid template selection" };
                    }

                    // Apply template configuration
                    state.Configuration["template"] = template.Id;
                    state.Configuration["name"] = template.Name;
                    state.Configuration["description"] = template.Description;
                    
                    // Apply template defaults
                    foreach (var kvp in template.Defaults)
                    {
                        if (!state.Configuration.ContainsKey(kvp.Key))
                        {
                            state.Configuration[kvp.Key] = kvp.Value;
                        }
                    }
                }

                DisplayStepSuccess("Template selected successfully");
                return new StepResult { Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Template selection step failed");
                return new StepResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        /// <summary>
        /// Step 2: Basic configuration
        /// </summary>
        private async Task<StepResult> StepBasicConfiguration(WizardState state, CancellationToken cancellationToken)
        {
            try
            {
                DisplayStepHeader("Basic Configuration", "Configure basic settings for your project");

                // Project name
                var projectName = await GetUserInputAsync("Project name: ", state.Configuration.GetValueOrDefault("name", "").ToString(), cancellationToken);
                state.Configuration["name"] = projectName;

                // Project description
                var description = await GetUserInputAsync("Description: ", state.Configuration.GetValueOrDefault("description", "").ToString(), cancellationToken);
                state.Configuration["description"] = description;

                // Version
                var version = await GetUserInputAsync("Version: ", state.Configuration.GetValueOrDefault("version", "1.0.0").ToString(), cancellationToken);
                state.Configuration["version"] = version;

                // Author
                var author = await GetUserInputAsync("Author: ", state.Configuration.GetValueOrDefault("author", "").ToString(), cancellationToken);
                state.Configuration["author"] = author;

                // License
                var license = await GetUserInputAsync("License: ", state.Configuration.GetValueOrDefault("license", "MIT").ToString(), cancellationToken);
                state.Configuration["license"] = license;

                DisplayStepSuccess("Basic configuration completed");
                return new StepResult { Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Basic configuration step failed");
                return new StepResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        /// <summary>
        /// Step 3: Advanced configuration
        /// </summary>
        private async Task<StepResult> StepAdvancedConfiguration(WizardState state, CancellationToken cancellationToken)
        {
            try
            {
                DisplayStepHeader("Advanced Configuration", "Configure advanced settings and features");

                // Database configuration
                var useDatabase = await GetUserConfirmationAsync("Enable database support? (y/n): ", cancellationToken);
                if (useDatabase)
                {
                    state.Configuration["database_enabled"] = true;
                    
                    var dbType = await GetUserSelectionAsync("Database type (postgresql/mysql/sqlserver): ", cancellationToken);
                    state.Configuration["database_type"] = dbType;
                    
                    var dbHost = await GetUserInputAsync("Database host: ", "localhost", cancellationToken);
                    state.Configuration["database_host"] = dbHost;
                    
                    var dbPort = await GetUserInputAsync("Database port: ", GetDefaultPort(dbType), cancellationToken);
                    state.Configuration["database_port"] = int.Parse(dbPort);
                }
                else
                {
                    state.Configuration["database_enabled"] = false;
                }

                // API configuration
                var enableApi = await GetUserConfirmationAsync("Enable API endpoints? (y/n): ", cancellationToken);
                state.Configuration["api_enabled"] = enableApi;

                if (enableApi)
                {
                    var apiPort = await GetUserInputAsync("API port: ", "5000", cancellationToken);
                    state.Configuration["api_port"] = int.Parse(apiPort);
                    
                    var enableSwagger = await GetUserConfirmationAsync("Enable Swagger documentation? (y/n): ", cancellationToken);
                    state.Configuration["swagger_enabled"] = enableSwagger;
                }

                // Logging configuration
                var logLevel = await GetUserSelectionAsync("Log level (debug/info/warning/error): ", cancellationToken);
                state.Configuration["log_level"] = logLevel;

                // Security configuration
                var enableSecurity = await GetUserConfirmationAsync("Enable security features? (y/n): ", cancellationToken);
                state.Configuration["security_enabled"] = enableSecurity;

                if (enableSecurity)
                {
                    var jwtSecret = await GetUserInputAsync("JWT secret (or press Enter for auto-generation): ", "", cancellationToken);
                    state.Configuration["jwt_secret"] = string.IsNullOrEmpty(jwtSecret) ? GenerateJwtSecret() : jwtSecret;
                }

                DisplayStepSuccess("Advanced configuration completed");
                return new StepResult { Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Advanced configuration step failed");
                return new StepResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        /// <summary>
        /// Step 4: Validation and review
        /// </summary>
        private async Task<StepResult> StepValidationAndReview(WizardState state, CancellationToken cancellationToken)
        {
            try
            {
                DisplayStepHeader("Validation and Review", "Review your configuration before saving");

                // Display configuration summary
                DisplayConfigurationSummary(state.Configuration);

                // Validate configuration
                var validationResult = await _validator.ValidateConfigurationAsync(state.Configuration, cancellationToken);
                if (!validationResult.IsValid)
                {
                    DisplayValidationErrors(validationResult.Errors);
                    
                    var continueAnyway = await GetUserConfirmationAsync("Continue with validation errors? (y/n): ", cancellationToken);
                    if (!continueAnyway)
                    {
                        return new StepResult { Success = false, ErrorMessage = "Configuration validation failed" };
                    }
                }

                // Confirm save
                var confirmSave = await GetUserConfirmationAsync("Save configuration? (y/n): ", cancellationToken);
                if (!confirmSave)
                {
                    return new StepResult { Success = false, ErrorMessage = "Configuration save cancelled by user" };
                }

                DisplayStepSuccess("Configuration validated and ready to save");
                return new StepResult { Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Validation and review step failed");
                return new StepResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        #endregion

        #region User Interaction

        /// <summary>
        /// Get user input with default value
        /// </summary>
        private async Task<string> GetUserInputAsync(string prompt, string defaultValue, CancellationToken cancellationToken)
        {
            var displayPrompt = string.IsNullOrEmpty(defaultValue) ? prompt : $"{prompt}[{defaultValue}] ";
            Console.Write(displayPrompt);
            
            var input = await ReadLineAsync(cancellationToken);
            
            if (string.IsNullOrEmpty(input) && !string.IsNullOrEmpty(defaultValue))
            {
                return defaultValue;
            }
            
            return input;
        }

        /// <summary>
        /// Get user selection from options
        /// </summary>
        private async Task<string> GetUserSelectionAsync(string prompt, CancellationToken cancellationToken)
        {
            Console.Write(prompt);
            return await ReadLineAsync(cancellationToken);
        }

        /// <summary>
        /// Get user confirmation
        /// </summary>
        private async Task<bool> GetUserConfirmationAsync(string prompt, CancellationToken cancellationToken)
        {
            while (true)
            {
                Console.Write(prompt);
                var input = await ReadLineAsync(cancellationToken);
                
                if (string.IsNullOrEmpty(input))
                    continue;
                
                var lower = input.ToLower();
                if (lower == "y" || lower == "yes")
                    return true;
                if (lower == "n" || lower == "no")
                    return false;
                
                Console.WriteLine("Please enter 'y' or 'n'");
            }
        }

        /// <summary>
        /// Read line with cancellation support
        /// </summary>
        private async Task<string> ReadLineAsync(CancellationToken cancellationToken)
        {
            // This is a simplified implementation
            // In a real scenario, you'd want to use a proper async console input library
            return Console.ReadLine() ?? "";
        }

        #endregion

        #region Display Methods

        /// <summary>
        /// Display welcome message
        /// </summary>
        private void DisplayWelcomeMessage()
        {
            Console.WriteLine();
            Console.WriteLine("🚀 Welcome to TuskTsk Configuration Wizard!");
            Console.WriteLine("This wizard will help you create a configuration file for your project.");
            Console.WriteLine("You can press Ctrl+C at any time to cancel.");
            Console.WriteLine();
        }

        /// <summary>
        /// Display edit mode message
        /// </summary>
        private void DisplayEditModeMessage(string configPath)
        {
            Console.WriteLine();
            Console.WriteLine($"📝 Editing configuration: {configPath}");
            Console.WriteLine("You can modify existing settings or add new ones.");
            Console.WriteLine();
        }

        /// <summary>
        /// Display step header
        /// </summary>
        private void DisplayStepHeader(string title, string description)
        {
            Console.WriteLine();
            Console.WriteLine($"=== {title} ===");
            Console.WriteLine(description);
            Console.WriteLine();
        }

        /// <summary>
        /// Display step success
        /// </summary>
        private void DisplayStepSuccess(string message)
        {
            Console.WriteLine($"✅ {message}");
        }

        /// <summary>
        /// Display templates
        /// </summary>
        private void DisplayTemplates(List<ConfigurationTemplate> templates)
        {
            Console.WriteLine("Available templates:");
            foreach (var template in templates)
            {
                Console.WriteLine($"  {template.Id}: {template.Name} - {template.Description}");
            }
            Console.WriteLine();
        }

        /// <summary>
        /// Display configuration summary
        /// </summary>
        private void DisplayConfigurationSummary(Dictionary<string, object> configuration)
        {
            Console.WriteLine("Configuration Summary:");
            Console.WriteLine("=====================");
            
            foreach (var kvp in configuration.OrderBy(k => k.Key))
            {
                var value = kvp.Value?.ToString() ?? "null";
                if (kvp.Key.Contains("secret") || kvp.Key.Contains("password"))
                {
                    value = new string('*', Math.Min(value.Length, 8));
                }
                Console.WriteLine($"  {kvp.Key}: {value}");
            }
            Console.WriteLine();
        }

        /// <summary>
        /// Display validation errors
        /// </summary>
        private void DisplayValidationErrors(List<string> errors)
        {
            Console.WriteLine("Validation Errors:");
            Console.WriteLine("=================");
            foreach (var error in errors)
            {
                Console.WriteLine($"  ❌ {error}");
            }
            Console.WriteLine();
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Get default output path
        /// </summary>
        private string GetDefaultOutputPath()
        {
            return Path.Combine(Directory.GetCurrentDirectory(), "peanu.tsk");
        }

        /// <summary>
        /// Get default port for database type
        /// </summary>
        private string GetDefaultPort(string dbType)
        {
            return dbType.ToLower() switch
            {
                "postgresql" => "5432",
                "mysql" => "3306",
                "sqlserver" => "1433",
                _ => "5432"
            };
        }

        /// <summary>
        /// Generate JWT secret
        /// </summary>
        private string GenerateJwtSecret()
        {
            var random = new Random();
            var bytes = new byte[32];
            random.NextBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Load configuration from file
        /// </summary>
        private async Task<Dictionary<string, object>> LoadConfigurationAsync(string path, CancellationToken cancellationToken)
        {
            try
            {
                var content = await File.ReadAllTextAsync(path, cancellationToken);
                return JsonSerializer.Deserialize<Dictionary<string, object>>(content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to load configuration: {path}");
                return null;
            }
        }

        /// <summary>
        /// Save configuration to file
        /// </summary>
        private async Task<SaveResult> SaveConfigurationAsync(WizardState state, CancellationToken cancellationToken)
        {
            try
            {
                // Create backup if editing existing file
                if (state.IsEditMode && File.Exists(state.OutputPath))
                {
                    var backupPath = $"{state.OutputPath}.backup.{DateTime.Now:yyyyMMddHHmmss}";
                    File.Copy(state.OutputPath, backupPath);
                    _logger.LogInformation($"Created backup: {backupPath}");
                }

                // Save configuration
                var json = JsonSerializer.Serialize(state.Configuration, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(state.OutputPath, json, cancellationToken);

                // Add to history
                _history.AddConfiguration(state.OutputPath, state.Configuration);

                Console.WriteLine($"✅ Configuration saved to: {state.OutputPath}");

                return new SaveResult { Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to save configuration: {state.OutputPath}");
                return new SaveResult { Success = false, ErrorMessage = ex.Message };
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

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
    }

    #region Supporting Classes

    /// <summary>
    /// Interactive wizard options
    /// </summary>
    public class InteractiveWizardOptions
    {
        public bool EnableTemplates { get; set; } = true;
        public bool EnableValidation { get; set; } = true;
        public bool EnableHistory { get; set; } = true;
        public bool EnableBackup { get; set; } = true;
        public TimeSpan StepTimeout { get; set; } = TimeSpan.FromMinutes(5);
    }

    /// <summary>
    /// Wizard state
    /// </summary>
    public class WizardState
    {
        public int CurrentStep { get; set; }
        public Dictionary<string, object> Configuration { get; set; }
        public string OutputPath { get; set; }
        public bool IsEditMode { get; set; }
        public List<ConfigurationTemplate> Templates { get; set; }
    }

    /// <summary>
    /// Wizard result
    /// </summary>
    public class WizardResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public Dictionary<string, object> Configuration { get; set; }
        public string OutputPath { get; set; }
        public TimeSpan CreationTime { get; set; }
        public List<string> ValidationErrors { get; set; } = new List<string>();
    }

    /// <summary>
    /// Step result
    /// </summary>
    public class StepResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Save result
    /// </summary>
    public class SaveResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Configuration template
    /// </summary>
    public class ConfigurationTemplate
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Dictionary<string, object> Defaults { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Configuration template manager
    /// </summary>
    public class ConfigurationTemplateManager
    {
        private readonly ILogger _logger;

        public ConfigurationTemplateManager(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<List<ConfigurationTemplate>> GetAvailableTemplatesAsync(CancellationToken cancellationToken)
        {
            return new List<ConfigurationTemplate>
            {
                new ConfigurationTemplate
                {
                    Id = "webapi",
                    Name = "Web API",
                    Description = "ASP.NET Core Web API with database support",
                    Defaults = new Dictionary<string, object>
                    {
                        ["api_enabled"] = true,
                        ["api_port"] = 5000,
                        ["swagger_enabled"] = true,
                        ["database_enabled"] = true,
                        ["database_type"] = "postgresql",
                        ["log_level"] = "info"
                    }
                },
                new ConfigurationTemplate
                {
                    Id = "console",
                    Name = "Console Application",
                    Description = "Console application with basic configuration",
                    Defaults = new Dictionary<string, object>
                    {
                        ["api_enabled"] = false,
                        ["database_enabled"] = false,
                        ["log_level"] = "info"
                    }
                },
                new ConfigurationTemplate
                {
                    Id = "library",
                    Name = "Class Library",
                    Description = "Class library with minimal configuration",
                    Defaults = new Dictionary<string, object>
                    {
                        ["api_enabled"] = false,
                        ["database_enabled"] = false,
                        ["log_level"] = "warning"
                    }
                }
            };
        }
    }

    /// <summary>
    /// Configuration validator
    /// </summary>
    public class ConfigurationValidator
    {
        private readonly ILogger _logger;

        public ConfigurationValidator(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<ValidationResult> ValidateConfigurationAsync(Dictionary<string, object> configuration, CancellationToken cancellationToken)
        {
            var result = new ValidationResult { IsValid = true, Errors = new List<string>() };

            try
            {
                // Required fields
                if (!configuration.ContainsKey("name") || string.IsNullOrEmpty(configuration["name"]?.ToString()))
                {
                    result.IsValid = false;
                    result.Errors.Add("Project name is required");
                }

                if (!configuration.ContainsKey("version") || string.IsNullOrEmpty(configuration["version"]?.ToString()))
                {
                    result.IsValid = false;
                    result.Errors.Add("Project version is required");
                }

                // Version format validation
                if (configuration.ContainsKey("version"))
                {
                    var version = configuration["version"].ToString();
                    if (!IsValidVersion(version))
                    {
                        result.IsValid = false;
                        result.Errors.Add("Invalid version format. Use semantic versioning (e.g., 1.0.0)");
                    }
                }

                // Database configuration validation
                if (configuration.GetValueOrDefault("database_enabled", false))
                {
                    if (!configuration.ContainsKey("database_type"))
                    {
                        result.IsValid = false;
                        result.Errors.Add("Database type is required when database is enabled");
                    }
                    else
                    {
                        var dbType = configuration["database_type"].ToString();
                        if (!IsValidDatabaseType(dbType))
                        {
                            result.IsValid = false;
                            result.Errors.Add($"Unsupported database type: {dbType}");
                        }
                    }
                }

                // API configuration validation
                if (configuration.GetValueOrDefault("api_enabled", false))
                {
                    if (!configuration.ContainsKey("api_port"))
                    {
                        result.IsValid = false;
                        result.Errors.Add("API port is required when API is enabled");
                    }
                    else
                    {
                        var port = configuration["api_port"];
                        if (port is int portInt && (portInt < 1 || portInt > 65535))
                        {
                            result.IsValid = false;
                            result.Errors.Add("API port must be between 1 and 65535");
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

        private bool IsValidVersion(string version)
        {
            return Regex.IsMatch(version, @"^\d+\.\d+\.\d+(-[a-zA-Z0-9.-]+)?(\+[a-zA-Z0-9.-]+)?$");
        }

        private bool IsValidDatabaseType(string dbType)
        {
            return new[] { "postgresql", "mysql", "sqlserver" }.Contains(dbType.ToLower());
        }
    }

    /// <summary>
    /// Configuration history
    /// </summary>
    public class ConfigurationHistory
    {
        private readonly List<HistoryEntry> _history = new List<HistoryEntry>();

        public void AddConfiguration(string path, Dictionary<string, object> configuration)
        {
            _history.Add(new HistoryEntry
            {
                Path = path,
                Configuration = new Dictionary<string, object>(configuration),
                Timestamp = DateTime.UtcNow
            });

            // Keep only last 10 entries
            if (_history.Count > 10)
            {
                _history.RemoveAt(0);
            }
        }

        public List<HistoryEntry> GetHistory()
        {
            return new List<HistoryEntry>(_history);
        }
    }

    /// <summary>
    /// History entry
    /// </summary>
    public class HistoryEntry
    {
        public string Path { get; set; }
        public Dictionary<string, object> Configuration { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Validation result
    /// </summary>
    public class ValidationResult
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
        private readonly List<TimeSpan> _wizardCompletionTimes = new List<TimeSpan>();
        private readonly List<TimeSpan> _errorTimes = new List<TimeSpan>();

        public void RecordWizardCompletion(TimeSpan time)
        {
            _wizardCompletionTimes.Add(time);
            if (_wizardCompletionTimes.Count > 100) _wizardCompletionTimes.RemoveAt(0);
        }

        public void RecordError(TimeSpan time)
        {
            _errorTimes.Add(time);
            if (_errorTimes.Count > 100) _errorTimes.RemoveAt(0);
        }

        public double AverageWizardCompletionTime => _wizardCompletionTimes.Count > 0 ? _wizardCompletionTimes.Average(t => t.TotalMilliseconds) : 0;
        public int ErrorCount => _errorTimes.Count;
        public int TotalWizards => _wizardCompletionTimes.Count;
    }

    #endregion
} 