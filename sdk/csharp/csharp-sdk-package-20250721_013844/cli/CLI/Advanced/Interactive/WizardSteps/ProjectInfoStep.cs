using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TuskLang.CLI.Advanced.Interactive.WizardSteps
{
    /// <summary>
    /// First wizard step for collecting project information
    /// Gathers basic project details like name, version, description
    /// </summary>
    public class ProjectInfoStep : WizardStepBase
    {
        private readonly Dictionary<string, object> _existingConfig;

        public override string Title => "Project Information";
        public override string Description => "Let's start by gathering basic information about your project.";
        public override bool ShowHelp => true;
        public override bool IsRequired => true;
        public override bool CanSkip => false;

        public ProjectInfoStep(Dictionary<string, object> existingConfig = null)
        {
            _existingConfig = existingConfig;
        }

        public override async Task<StepResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            var result = new StepResult
            {
                StepNumber = StepNumber,
                StartedAt = DateTime.UtcNow
            };

            try
            {
                ShowInfo("We'll collect basic project information to set up your TuskLang configuration.");

                // Get project name
                var projectName = GetProjectName();
                if (string.IsNullOrEmpty(projectName))
                {
                    result.Success = false;
                    result.ErrorMessage = "Project name is required.";
                    return result;
                }

                // Get project version
                var projectVersion = GetProjectVersion();

                // Get project description
                var projectDescription = GetProjectDescription();

                // Get project type
                var projectType = GetProjectType();

                // Get author information
                var authorName = GetAuthorName();
                var authorEmail = GetAuthorEmail();

                // Store collected data
                result.Data["projectName"] = projectName;
                result.Data["projectVersion"] = projectVersion;
                result.Data["projectDescription"] = projectDescription;
                result.Data["projectType"] = projectType;
                result.Data["authorName"] = authorName;
                result.Data["authorEmail"] = authorEmail;

                result.Success = true;
                result.CompletedAt = DateTime.UtcNow;

                ShowSuccess($"Project information collected: {projectName} v{projectVersion}");

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.CompletedAt = DateTime.UtcNow;
                return result;
            }
        }

        public override ValidationResult Validate(Dictionary<string, object> data)
        {
            var validation = new ValidationResult();

            if (!data.ContainsKey("projectName") || string.IsNullOrEmpty(data["projectName"]?.ToString()))
            {
                validation.IsValid = false;
                validation.Errors.Add("Project name is required");
            }

            if (!data.ContainsKey("projectVersion") || string.IsNullOrEmpty(data["projectVersion"]?.ToString()))
            {
                validation.IsValid = false;
                validation.Errors.Add("Project version is required");
            }

            return validation;
        }

        public override string GetHelpText()
        {
            return @"Project Information Help:

• Project Name: A unique identifier for your project (e.g., 'my-awesome-app')
• Project Version: Semantic versioning format (e.g., '1.0.0', '2.1.3-beta')
• Project Description: Brief description of what your project does
• Project Type: The type of project (web, console, library, etc.)
• Author Name: Your name or organization name
• Author Email: Contact email for the project

All fields marked with * are required.";
        }

        public override Dictionary<string, object> GetDefaultValues()
        {
            var defaults = new Dictionary<string, object>
            {
                ["projectName"] = "my-project",
                ["projectVersion"] = "1.0.0",
                ["projectDescription"] = "A TuskLang-powered application",
                ["projectType"] = "console",
                ["authorName"] = Environment.UserName,
                ["authorEmail"] = ""
            };

            // Override with existing config if available
            if (_existingConfig != null)
            {
                if (_existingConfig.TryGetValue("project", out var projectObj) && projectObj is Dictionary<string, object> project)
                {
                    foreach (var kvp in project)
                    {
                        defaults[$"project{kvp.Key}"] = kvp.Value;
                    }
                }
            }

            return defaults;
        }

        private string GetProjectName()
        {
            var defaultValue = GetDefaultValues()["projectName"].ToString();
            
            while (true)
            {
                var projectName = PromptUser("Project name *", defaultValue);
                
                if (string.IsNullOrEmpty(projectName))
                {
                    ShowError("Project name is required.");
                    continue;
                }

                // Validate project name format
                if (!IsValidProjectName(projectName))
                {
                    ShowError("Project name must be alphanumeric with hyphens or underscores only.");
                    continue;
                }

                return projectName;
            }
        }

        private string GetProjectVersion()
        {
            var defaultValue = GetDefaultValues()["projectVersion"].ToString();
            
            while (true)
            {
                var version = PromptUser("Project version *", defaultValue);
                
                if (string.IsNullOrEmpty(version))
                {
                    ShowError("Project version is required.");
                    continue;
                }

                // Validate version format
                if (!IsValidVersion(version))
                {
                    ShowError("Version must be in semantic versioning format (e.g., 1.0.0, 2.1.3-beta).");
                    continue;
                }

                return version;
            }
        }

        private string GetProjectDescription()
        {
            var defaultValue = GetDefaultValues()["projectDescription"].ToString();
            return PromptUser("Project description", defaultValue);
        }

        private string GetProjectType()
        {
            var options = new[]
            {
                "console - Console application",
                "web - Web application",
                "api - API service",
                "library - Class library",
                "worker - Background worker",
                "desktop - Desktop application",
                "mobile - Mobile application",
                "other - Other type"
            };

            var defaultIndex = 0; // console
            var selectedIndex = PromptSelection("Select project type:", options, defaultIndex);
            return options[selectedIndex].Split(' ')[0]; // Extract just the type
        }

        private string GetAuthorName()
        {
            var defaultValue = GetDefaultValues()["authorName"].ToString();
            return PromptUser("Author name", defaultValue);
        }

        private string GetAuthorEmail()
        {
            var defaultValue = GetDefaultValues()["authorEmail"].ToString();
            
            while (true)
            {
                var email = PromptUser("Author email", defaultValue);
                
                if (string.IsNullOrEmpty(email))
                {
                    return email; // Email is optional
                }

                // Validate email format
                if (!IsValidEmail(email))
                {
                    ShowWarning("Email format appears invalid. Continue anyway? (y/N)");
                    if (PromptYesNo("Continue with this email?", false))
                    {
                        return email;
                    }
                    continue;
                }

                return email;
            }
        }

        private bool IsValidProjectName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            
            // Allow alphanumeric, hyphens, underscores
            foreach (char c in name)
            {
                if (!char.IsLetterOrDigit(c) && c != '-' && c != '_')
                {
                    return false;
                }
            }
            
            return true;
        }

        private bool IsValidVersion(string version)
        {
            if (string.IsNullOrEmpty(version)) return false;
            
            // Basic semantic versioning validation
            var parts = version.Split('.');
            if (parts.Length < 2 || parts.Length > 3) return false;
            
            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part)) return false;
                
                // Check if it's a number or contains valid pre-release/build metadata
                if (!int.TryParse(part.Split('-')[0].Split('+')[0], out _))
                {
                    return false;
                }
            }
            
            return true;
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrEmpty(email)) return false;
            
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
} 