using System.Threading;
using System.Threading.Tasks;

namespace TuskLang.CLI.Advanced.Interactive
{
    /// <summary>
    /// Interface for wizard steps in the interactive configuration mode
    /// Defines the contract for all wizard steps
    /// </summary>
    public interface IWizardStep
    {
        /// <summary>
        /// Step number in the wizard sequence
        /// </summary>
        int StepNumber { get; set; }

        /// <summary>
        /// Step title displayed to the user
        /// </summary>
        string Title { get; }

        /// <summary>
        /// Step description displayed to the user
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Whether to show help for this step
        /// </summary>
        bool ShowHelp { get; }

        /// <summary>
        /// Whether this step is required
        /// </summary>
        bool IsRequired { get; }

        /// <summary>
        /// Whether this step can be skipped
        /// </summary>
        bool CanSkip { get; }

        /// <summary>
        /// Execute the wizard step
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Step result</returns>
        Task<StepResult> ExecuteAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Validate the step data
        /// </summary>
        /// <param name="data">Step data to validate</param>
        /// <returns>Validation result</returns>
        ValidationResult Validate(Dictionary<string, object> data);

        /// <summary>
        /// Get help text for this step
        /// </summary>
        /// <returns>Help text</returns>
        string GetHelpText();

        /// <summary>
        /// Get default values for this step
        /// </summary>
        /// <returns>Default values</returns>
        Dictionary<string, object> GetDefaultValues();
    }

    /// <summary>
    /// Base class for wizard steps with common functionality
    /// </summary>
    public abstract class WizardStepBase : IWizardStep
    {
        public int StepNumber { get; set; }
        public abstract string Title { get; }
        public abstract string Description { get; }
        public virtual bool ShowHelp => true;
        public virtual bool IsRequired => true;
        public virtual bool CanSkip => false;

        public abstract Task<StepResult> ExecuteAsync(CancellationToken cancellationToken);

        public virtual ValidationResult Validate(Dictionary<string, object> data)
        {
            return new ValidationResult { IsValid = true };
        }

        public virtual string GetHelpText()
        {
            return "No help available for this step.";
        }

        public virtual Dictionary<string, object> GetDefaultValues()
        {
            return new Dictionary<string, object>();
        }

        /// <summary>
        /// Display a prompt to the user
        /// </summary>
        /// <param name="prompt">Prompt text</param>
        /// <param name="defaultValue">Default value</param>
        /// <returns>User input</returns>
        protected string PromptUser(string prompt, string defaultValue = null)
        {
            Console.Write($"{prompt}");
            if (!string.IsNullOrEmpty(defaultValue))
            {
                Console.Write($" [{defaultValue}]");
            }
            Console.Write(": ");

            var input = Console.ReadLine()?.Trim();
            return string.IsNullOrEmpty(input) ? defaultValue : input;
        }

        /// <summary>
        /// Display a yes/no prompt to the user
        /// </summary>
        /// <param name="prompt">Prompt text</param>
        /// <param name="defaultValue">Default value</param>
        /// <returns>User choice</returns>
        protected bool PromptYesNo(string prompt, bool defaultValue = true)
        {
            var defaultText = defaultValue ? "Y/n" : "y/N";
            var input = PromptUser($"{prompt} ({defaultText})", defaultValue ? "Y" : "N");
            
            if (string.IsNullOrEmpty(input))
                return defaultValue;

            return input.ToLowerInvariant().StartsWith("y");
        }

        /// <summary>
        /// Display a selection prompt to the user
        /// </summary>
        /// <param name="prompt">Prompt text</param>
        /// <param name="options">Available options</param>
        /// <param name="defaultIndex">Default selection index</param>
        /// <returns>Selected index</returns>
        protected int PromptSelection(string prompt, string[] options, int defaultIndex = 0)
        {
            Console.WriteLine(prompt);
            Console.WriteLine();

            for (int i = 0; i < options.Length; i++)
            {
                var marker = i == defaultIndex ? ">" : " ";
                Console.WriteLine($"{marker} {i + 1}. {options[i]}");
            }

            Console.WriteLine();
            Console.Write($"Select option (1-{options.Length}) [{defaultIndex + 1}]: ");

            var input = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(input))
                return defaultIndex;

            if (int.TryParse(input, out int selection) && selection >= 1 && selection <= options.Length)
            {
                return selection - 1;
            }

            Console.WriteLine("Invalid selection. Using default.");
            return defaultIndex;
        }

        /// <summary>
        /// Display a multi-line input prompt to the user
        /// </summary>
        /// <param name="prompt">Prompt text</param>
        /// <param name="defaultValue">Default value</param>
        /// <returns>User input</returns>
        protected string PromptMultiLine(string prompt, string defaultValue = null)
        {
            Console.WriteLine(prompt);
            if (!string.IsNullOrEmpty(defaultValue))
            {
                Console.WriteLine($"Default value:\n{defaultValue}");
            }
            Console.WriteLine("Enter your input (press Enter twice to finish):");

            var lines = new List<string>();
            string line;
            while (!string.IsNullOrEmpty(line = Console.ReadLine()))
            {
                lines.Add(line);
            }

            var result = string.Join("\n", lines);
            return string.IsNullOrEmpty(result) ? defaultValue : result;
        }

        /// <summary>
        /// Display a file path prompt to the user
        /// </summary>
        /// <param name="prompt">Prompt text</param>
        /// <param name="defaultValue">Default value</param>
        /// <param name="mustExist">Whether the file must exist</param>
        /// <returns>File path</returns>
        protected string PromptFilePath(string prompt, string defaultValue = null, bool mustExist = false)
        {
            while (true)
            {
                var path = PromptUser(prompt, defaultValue);
                
                if (string.IsNullOrEmpty(path))
                    return defaultValue;

                if (mustExist && !File.Exists(path))
                {
                    Console.WriteLine($"❌ File not found: {path}");
                    continue;
                }

                return path;
            }
        }

        /// <summary>
        /// Display a directory path prompt to the user
        /// </summary>
        /// <param name="prompt">Prompt text</param>
        /// <param name="defaultValue">Default value</param>
        /// <param name="mustExist">Whether the directory must exist</param>
        /// <returns>Directory path</returns>
        protected string PromptDirectoryPath(string prompt, string defaultValue = null, bool mustExist = false)
        {
            while (true)
            {
                var path = PromptUser(prompt, defaultValue);
                
                if (string.IsNullOrEmpty(path))
                    return defaultValue;

                if (mustExist && !Directory.Exists(path))
                {
                    Console.WriteLine($"❌ Directory not found: {path}");
                    continue;
                }

                return path;
            }
        }

        /// <summary>
        /// Display success message
        /// </summary>
        /// <param name="message">Success message</param>
        protected void ShowSuccess(string message)
        {
            Console.WriteLine($"✅ {message}");
        }

        /// <summary>
        /// Display error message
        /// </summary>
        /// <param name="message">Error message</param>
        protected void ShowError(string message)
        {
            Console.WriteLine($"❌ {message}");
        }

        /// <summary>
        /// Display warning message
        /// </summary>
        /// <param name="message">Warning message</param>
        protected void ShowWarning(string message)
        {
            Console.WriteLine($"⚠️ {message}");
        }

        /// <summary>
        /// Display info message
        /// </summary>
        /// <param name="message">Info message</param>
        protected void ShowInfo(string message)
        {
            Console.WriteLine($"ℹ️ {message}");
        }
    }

    #region Result Classes

    /// <summary>
    /// Validation result for wizard steps
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
    }

    #endregion
} 