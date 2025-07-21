using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TuskLang;

namespace TuskLang.CLI.Advanced.Interactive
{
    /// <summary>
    /// Core wizard framework for interactive configuration mode
    /// Provides step management, user interaction, and progress tracking
    /// </summary>
    public class WizardFramework
    {
        private readonly List<IWizardStep> _steps;
        private readonly StepManager _stepManager;
        private readonly HistoryManager _historyManager;
        private readonly HelpSystem _helpSystem;
        private readonly UserPreferences _preferences;
        
        private int _currentStepIndex;
        private bool _isRunning;
        private CancellationTokenSource _cancellationTokenSource;

        public WizardFramework()
        {
            _steps = new List<IWizardStep>();
            _stepManager = new StepManager();
            _historyManager = new HistoryManager();
            _helpSystem = new HelpSystem();
            _preferences = new UserPreferences();
            _currentStepIndex = 0;
            _isRunning = false;
        }

        /// <summary>
        /// Add a step to the wizard
        /// </summary>
        public void AddStep(IWizardStep step)
        {
            if (step == null)
                throw new ArgumentNullException(nameof(step));

            _steps.Add(step);
            step.StepNumber = _steps.Count;
        }

        /// <summary>
        /// Run the complete wizard with user interaction
        /// </summary>
        public async Task<WizardResult> RunAsync(CancellationToken cancellationToken = default)
        {
            if (_steps.Count == 0)
            {
                throw new InvalidOperationException("No steps defined for wizard");
            }

            _isRunning = true;
            _currentStepIndex = 0;
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            try
            {
                Console.Clear();
                DisplayWelcomeMessage();

                var result = new WizardResult
                {
                    StartedAt = DateTime.UtcNow,
                    TotalSteps = _steps.Count
                };

                // Run each step
                for (int i = 0; i < _steps.Count; i++)
                {
                    _currentStepIndex = i;
                    var step = _steps[i];

                    // Check for cancellation
                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        result.Cancelled = true;
                        break;
                    }

                    // Display step header
                    DisplayStepHeader(step);

                    // Execute step
                    var stepResult = await ExecuteStepAsync(step, _cancellationTokenSource.Token);
                    
                    if (!stepResult.Success)
                    {
                        result.FailedStep = step.StepNumber;
                        result.ErrorMessage = stepResult.ErrorMessage;
                        break;
                    }

                    result.CompletedSteps++;
                    _historyManager.AddStepResult(step, stepResult);
                }

                result.CompletedAt = DateTime.UtcNow;
                result.Success = result.CompletedSteps == _steps.Count && !result.Cancelled;

                if (result.Success)
                {
                    DisplaySuccessMessage();
                }
                else if (result.Cancelled)
                {
                    DisplayCancelledMessage();
                }
                else
                {
                    DisplayErrorMessage(result.ErrorMessage);
                }

                return result;
            }
            finally
            {
                _isRunning = false;
                _cancellationTokenSource?.Dispose();
            }
        }

        /// <summary>
        /// Execute a single wizard step
        /// </summary>
        private async Task<StepResult> ExecuteStepAsync(IWizardStep step, CancellationToken cancellationToken)
        {
            var stepResult = new StepResult
            {
                StepNumber = step.StepNumber,
                StartedAt = DateTime.UtcNow
            };

            try
            {
                // Show step description
                DisplayStepDescription(step);

                // Show help if available
                if (step.ShowHelp)
                {
                    DisplayStepHelp(step);
                }

                // Execute the step
                stepResult = await step.ExecuteAsync(cancellationToken);

                // Validate step result
                if (stepResult.Success)
                {
                    DisplayStepSuccess(step);
                }
                else
                {
                    DisplayStepError(step, stepResult.ErrorMessage);
                }

                return stepResult;
            }
            catch (Exception ex)
            {
                stepResult.Success = false;
                stepResult.ErrorMessage = ex.Message;
                DisplayStepError(step, ex.Message);
                return stepResult;
            }
        }

        /// <summary>
        /// Display welcome message
        /// </summary>
        private void DisplayWelcomeMessage()
        {
            Console.WriteLine("🎯 TuskLang Interactive Configuration Wizard");
            Console.WriteLine("=============================================");
            Console.WriteLine();
            Console.WriteLine("Welcome! This wizard will help you create and configure");
            Console.WriteLine("TuskLang configuration files step by step.");
            Console.WriteLine();
            Console.WriteLine($"Total steps: {_steps.Count}");
            Console.WriteLine("Press Ctrl+C at any time to cancel.");
            Console.WriteLine();
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey(true);
            Console.Clear();
        }

        /// <summary>
        /// Display step header with progress
        /// </summary>
        private void DisplayStepHeader(IWizardStep step)
        {
            var progress = ((double)(_currentStepIndex + 1) / _steps.Count) * 100;
            
            Console.WriteLine($"Step {step.StepNumber} of {_steps.Count} ({progress:F0}%)");
            Console.WriteLine($"📋 {step.Title}");
            Console.WriteLine(new string('=', 50));
            Console.WriteLine();
        }

        /// <summary>
        /// Display step description
        /// </summary>
        private void DisplayStepDescription(IWizardStep step)
        {
            if (!string.IsNullOrEmpty(step.Description))
            {
                Console.WriteLine(step.Description);
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Display step help
        /// </summary>
        private void DisplayStepHelp(IWizardStep step)
        {
            var help = _helpSystem.GetHelpForStep(step);
            if (!string.IsNullOrEmpty(help))
            {
                Console.WriteLine("💡 Help:");
                Console.WriteLine(help);
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Display step success message
        /// </summary>
        private void DisplayStepSuccess(IWizardStep step)
        {
            Console.WriteLine($"✅ {step.Title} completed successfully");
            Console.WriteLine();
        }

        /// <summary>
        /// Display step error message
        /// </summary>
        private void DisplayStepError(IWizardStep step, string errorMessage)
        {
            Console.WriteLine($"❌ Error in {step.Title}: {errorMessage}");
            Console.WriteLine();
        }

        /// <summary>
        /// Display success message
        /// </summary>
        private void DisplaySuccessMessage()
        {
            Console.WriteLine("🎉 Configuration wizard completed successfully!");
            Console.WriteLine("Your TuskLang configuration is ready to use.");
            Console.WriteLine();
        }

        /// <summary>
        /// Display cancelled message
        /// </summary>
        private void DisplayCancelledMessage()
        {
            Console.WriteLine("⚠️ Wizard cancelled by user.");
            Console.WriteLine("No changes were saved.");
            Console.WriteLine();
        }

        /// <summary>
        /// Display error message
        /// </summary>
        private void DisplayErrorMessage(string errorMessage)
        {
            Console.WriteLine($"❌ Wizard failed: {errorMessage}");
            Console.WriteLine("Please try again or contact support.");
            Console.WriteLine();
        }

        /// <summary>
        /// Get current step
        /// </summary>
        public IWizardStep GetCurrentStep()
        {
            if (_currentStepIndex >= 0 && _currentStepIndex < _steps.Count)
            {
                return _steps[_currentStepIndex];
            }
            return null;
        }

        /// <summary>
        /// Get total steps
        /// </summary>
        public int TotalSteps => _steps.Count;

        /// <summary>
        /// Get current step index
        /// </summary>
        public int CurrentStepIndex => _currentStepIndex;

        /// <summary>
        /// Check if wizard is running
        /// </summary>
        public bool IsRunning => _isRunning;

        /// <summary>
        /// Cancel the wizard
        /// </summary>
        public void Cancel()
        {
            _cancellationTokenSource?.Cancel();
        }

        /// <summary>
        /// Get wizard progress percentage
        /// </summary>
        public double GetProgressPercentage()
        {
            if (_steps.Count == 0) return 0;
            return ((double)(_currentStepIndex + 1) / _steps.Count) * 100;
        }
    }

    #region Result Classes

    /// <summary>
    /// Result of wizard execution
    /// </summary>
    public class WizardResult
    {
        public DateTime StartedAt { get; set; }
        public DateTime CompletedAt { get; set; }
        public TimeSpan Duration => CompletedAt - StartedAt;
        public int TotalSteps { get; set; }
        public int CompletedSteps { get; set; }
        public bool Success { get; set; }
        public bool Cancelled { get; set; }
        public int? FailedStep { get; set; }
        public string ErrorMessage { get; set; }
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Result of individual step execution
    /// </summary>
    public class StepResult
    {
        public int StepNumber { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime CompletedAt { get; set; }
        public TimeSpan Duration => CompletedAt - StartedAt;
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
    }

    #endregion
} 