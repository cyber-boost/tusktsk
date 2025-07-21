using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using TuskLang;
using TuskLang.CLI.Advanced.Interactive.WizardSteps;

namespace TuskLang.CLI.Advanced.Interactive
{
    /// <summary>
    /// Main configuration wizard for creating and editing TuskLang .tsk files
    /// Provides comprehensive step-by-step guidance with real-time validation
    /// </summary>
    public class ConfigurationWizard
    {
        private readonly WizardFramework _framework;
        private readonly TemplateManager _templateManager;
        private readonly RealTimeValidator _validator;
        private readonly FileManager _fileManager;
        private readonly UserPreferences _preferences;

        public ConfigurationWizard()
        {
            _framework = new WizardFramework();
            _templateManager = new TemplateManager();
            _validator = new RealTimeValidator();
            _fileManager = new FileManager();
            _preferences = new UserPreferences();
        }

        /// <summary>
        /// Create a new configuration file with wizard
        /// </summary>
        public async Task<WizardResult> CreateNewConfigurationAsync(string outputPath = null, CancellationToken cancellationToken = default)
        {
            try
            {
                SetupWizardSteps();
                
                var result = await _framework.RunAsync(cancellationToken);
                
                if (result.Success)
                {
                    await SaveConfigurationAsync(result, outputPath);
                }

                return result;
            }
            catch (Exception ex)
            {
                return new WizardResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    StartedAt = DateTime.UtcNow,
                    CompletedAt = DateTime.UtcNow
                };
            }
        }

        /// <summary>
        /// Edit an existing configuration file with wizard
        /// </summary>
        public async Task<WizardResult> EditConfigurationAsync(string filePath, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"Configuration file not found: {filePath}");
                }

                // Load existing configuration
                var existingConfig = await LoadExistingConfigurationAsync(filePath);
                
                SetupWizardSteps(existingConfig);
                
                var result = await _framework.RunAsync(cancellationToken);
                
                if (result.Success)
                {
                    await SaveConfigurationAsync(result, filePath);
                }

                return result;
            }
            catch (Exception ex)
            {
                return new WizardResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    StartedAt = DateTime.UtcNow,
                    CompletedAt = DateTime.UtcNow
                };
            }
        }

        /// <summary>
        /// Setup wizard steps for configuration creation/editing
        /// </summary>
        private void SetupWizardSteps(Dictionary<string, object> existingConfig = null)
        {
            _framework.AddStep(new ProjectInfoStep(existingConfig));
            _framework.AddStep(new TemplateSelectionStep(_templateManager, existingConfig));
            _framework.AddStep(new ConfigurationBuilderStep(_validator, existingConfig));
            _framework.AddStep(new ValidationStep(_validator));
            _framework.AddStep(new FileSaveStep(_fileManager, _preferences));
        }

        /// <summary>
        /// Load existing configuration from file
        /// </summary>
        private async Task<Dictionary<string, object>> LoadExistingConfigurationAsync(string filePath)
        {
            try
            {
                var content = await File.ReadAllTextAsync(filePath);
                var tsk = new TSK(content);
                return tsk.ToDictionary();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load existing configuration: {ex.Message}");
            }
        }

        /// <summary>
        /// Save configuration to file
        /// </summary>
        private async Task SaveConfigurationAsync(WizardResult result, string outputPath)
        {
            try
            {
                if (result.Data.TryGetValue("configuration", out var configObj) && configObj is Dictionary<string, object> config)
                {
                    var tsk = new TSK(config);
                    var content = tsk.ToString();

                    if (string.IsNullOrEmpty(outputPath))
                    {
                        outputPath = result.Data.TryGetValue("outputPath", out var pathObj) 
                            ? pathObj.ToString() 
                            : "peanu.tsk";
                    }

                    await _fileManager.SaveConfigurationAsync(outputPath, content);
                    
                    result.Data["savedPath"] = outputPath;
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Failed to save configuration: {ex.Message}";
            }
        }

        /// <summary>
        /// Get available templates
        /// </summary>
        public List<TemplateInfo> GetAvailableTemplates()
        {
            return _templateManager.GetAvailableTemplates();
        }

        /// <summary>
        /// Validate configuration content
        /// </summary>
        public ValidationResult ValidateConfiguration(string content)
        {
            return _validator.ValidateConfiguration(content);
        }

        /// <summary>
        /// Get user preferences
        /// </summary>
        public UserPreferences GetUserPreferences()
        {
            return _preferences;
        }

        /// <summary>
        /// Cancel the wizard
        /// </summary>
        public void Cancel()
        {
            _framework.Cancel();
        }

        /// <summary>
        /// Check if wizard is running
        /// </summary>
        public bool IsRunning => _framework.IsRunning;

        /// <summary>
        /// Get wizard progress
        /// </summary>
        public double GetProgressPercentage()
        {
            return _framework.GetProgressPercentage();
        }
    }

    #region Data Classes

    /// <summary>
    /// Template information
    /// </summary>
    public class TemplateInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public Dictionary<string, object> DefaultValues { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
    }

    #endregion
} 