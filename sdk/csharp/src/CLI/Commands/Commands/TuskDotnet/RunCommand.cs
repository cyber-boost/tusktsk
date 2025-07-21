using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using TuskLang;
using System.Linq;

namespace TuskLang.CLI.Commands.TuskDotnet
{
    /// <summary>
    /// Run command implementation - Run .NET applications with TuskLang runtime configuration
    /// Provides runtime configuration injection and environment management
    /// </summary>
    public static class RunCommand
    {
        public static Command CreateRunCommand()
        {
            // Arguments  
            var projectArgument = new Argument<string>(
                name: "project",
                description: "Project file or directory to run (optional)")
            {
                Arity = ArgumentArity.ZeroOrOne
            };

            // Options
            var configurationOption = new Option<string>(
                aliases: new[] { "--configuration", "-c" },
                getDefaultValue: () => "Debug",
                description: "Build configuration: Debug, Release");

            var environmentOption = new Option<string>(
                aliases: new[] { "--environment", "-e" },
                description: "Runtime environment (Development, Staging, Production)");

            var frameworkOption = new Option<string>(
                aliases: new[] { "--framework", "-f" },
                description: "Target framework to run");

            var noBuildOption = new Option<bool>(
                aliases: new[] { "--no-build" },
                description: "Don't build before running");

            var noRestoreOption = new Option<bool>(
                aliases: new[] { "--no-restore" },
                description: "Don't restore before running");

            var launchProfileOption = new Option<string>(
                aliases: new[] { "--launch-profile" },
                description: "Launch profile to use");

            var verbosityOption = new Option<string>(
                aliases: new[] { "--verbosity", "-v" },
                getDefaultValue: () => "minimal",
                description: "Verbosity level");

            // Runtime arguments
            var argumentsOption = new Option<string[]>(
                aliases: new[] { "--", "--args" },
                description: "Arguments to pass to the application")
            {
                AllowMultipleArgumentsPerToken = true
            };

            // Create command
            var runCommand = new Command("run", "Run .NET applications with TuskLang runtime configuration")
            {
                projectArgument,
                configurationOption,
                environmentOption,
                frameworkOption,
                noBuildOption,
                noRestoreOption,
                launchProfileOption,
                verbosityOption,
                argumentsOption
            };

            runCommand.SetHandler(async (project, configuration, environment, framework, noBuild, noRestore, launchProfile, verbosity, args) =>
            {
                var command = new RunCommandImplementation();
                Environment.ExitCode = await command.ExecuteAsync(
                    project, configuration, environment, framework, noBuild, noRestore, launchProfile, verbosity, args);
            }, projectArgument, configurationOption, environmentOption, frameworkOption, noBuildOption, noRestoreOption, launchProfileOption, verbosityOption, argumentsOption);

            return runCommand;
        }
    }

    /// <summary>
    /// Run command implementation with TuskLang runtime configuration integration
    /// </summary>
    public class RunCommandImplementation : DotnetCommandBase
    {
        public async Task<int> ExecuteAsync(
            string project,
            string configuration,
            string environment,
            string framework,
            bool noBuild,
            bool noRestore,
            string launchProfile,
            string verbosity,
            string[] args)
        {
            return await ExecuteDotnetCommandAsync(async () =>
            {
                // Find project file
                var projectPath = FindProjectFile(project);
                if (projectPath == null)
                    return 1;

                WriteProcessing($"🚀 Running .NET application with TuskLang configuration...");
                WriteInfo($"Project: {Path.GetFileName(projectPath)}");
                WriteInfo($"Configuration: {configuration}");
                WriteInfo($"Environment: {environment ?? "default"}");

                // Setup runtime environment
                var runtimeEnvironment = await SetupRuntimeEnvironmentAsync(projectPath, environment);
                if (runtimeEnvironment == null)
                {
                    WriteWarning("Failed to setup runtime environment, continuing...");
                    runtimeEnvironment = new Dictionary<string, string>();
                }

                // Build project unless skipped
                if (!noBuild)
                {
                    WriteProcessing("Building project...");
                    var buildSuccess = await BuildProjectForRunAsync(projectPath, configuration, framework, noRestore);
                    if (!buildSuccess)
                        return 1;
                    WriteSuccess("Project built successfully");
                }

                // Run the application
                var runSuccess = await RunApplicationAsync(
                    projectPath, configuration, framework, launchProfile, verbosity, args, runtimeEnvironment);

                return runSuccess ? 0 : 1;
            }, "Run");
        }

        private async Task<Dictionary<string, string>> SetupRuntimeEnvironmentAsync(string projectPath, string environment)
        {
            try
            {
                var envVars = new Dictionary<string, string>();
                var projectDir = Path.GetDirectoryName(projectPath);

                // Load TuskLang configuration
                var configPath = FindTuskLangConfigFile(projectDir);
                if (!string.IsNullOrEmpty(configPath))
                {
                    var tsk = await LoadTuskLangConfigAsync(configPath);
                    if (tsk != null)
                    {
                        WriteInfo($"Loading runtime configuration from: {Path.GetFileName(configPath)}");

                        // Convert TuskLang config to environment variables
                        var config = tsk.ToDictionary();
                        ConvertConfigToEnvironmentVariables(config, envVars, "TUSKLANG");

                        // Load environment-specific configuration
                        if (!string.IsNullOrEmpty(environment))
                        {
                            var envConfigPath = Path.Combine(projectDir, "config", $"{environment.ToLower()}.tsk");
                            if (File.Exists(envConfigPath))
                            {
                                var envTsk = await LoadTuskLangConfigAsync(envConfigPath);
                                if (envTsk != null)
                                {
                                    WriteInfo($"Loading environment configuration: {environment}");
                                    var envConfig = envTsk.ToDictionary();
                                    ConvertConfigToEnvironmentVariables(envConfig, envVars, "TUSKLANG");
                                }
                            }
                        }
                    }
                }

                // Set standard ASP.NET Core environment variables
                if (!string.IsNullOrEmpty(environment))
                {
                    envVars["ASPNETCORE_ENVIRONMENT"] = environment;
                    envVars["DOTNET_ENVIRONMENT"] = environment;
                }

                // Add TuskLang-specific environment variables
                envVars["TUSKLANG_CONFIG_FILE"] = configPath ?? "peanu.tsk";
                envVars["TUSKLANG_RUNTIME_MODE"] = "true";

                WriteInfo($"Runtime environment configured with {envVars.Count} variables");
                return envVars;
            }
            catch (Exception ex)
            {
                WriteWarning($"Failed to setup runtime environment: {ex.Message}");
                return new Dictionary<string, string>();
            }
        }

        private void ConvertConfigToEnvironmentVariables(
            Dictionary<string, object> config,
            Dictionary<string, string> envVars,
            string prefix)
        {
            foreach (var section in config)
            {
                if (section.Value is Dictionary<string, object> sectionData)
                {
                    foreach (var property in sectionData)
                    {
                        if (property.Value is string strValue)
                        {
                            var envVarName = $"{prefix}_{section.Key}_{property.Key}".ToUpper();
                            envVars[envVarName] = strValue;
                        }
                    }
                }
                else if (section.Value is string strValue)
                {
                    var envVarName = $"{prefix}_{section.Key}".ToUpper();
                    envVars[envVarName] = strValue;
                }
            }
        }

        private async Task<bool> BuildProjectForRunAsync(
            string projectPath,
            string configuration,
            string framework,
            bool noRestore)
        {
            try
            {
                var arguments = new List<string> { "build", $"\"{projectPath}\"", $"--configuration {configuration}" };

                if (!string.IsNullOrEmpty(framework))
                {
                    arguments.Add($"--framework {framework}");
                }

                if (noRestore)
                {
                    arguments.Add("--no-restore");
                }

                var buildArgs = string.Join(" ", arguments);
                var result = await RunDotnetCommandAsync(buildArgs, Path.GetDirectoryName(projectPath), false);

                return result.Success;
            }
            catch (Exception ex)
            {
                WriteError($"Build failed: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> RunApplicationAsync(
            string projectPath,
            string configuration,
            string framework,
            string launchProfile,
            string verbosity,
            string[] args,
            Dictionary<string, string> environmentVariables)
        {
            try
            {
                var runArguments = new List<string> { "run", $"--project \"{projectPath}\"" };

                if (!string.IsNullOrEmpty(configuration))
                {
                    runArguments.Add($"--configuration {configuration}");
                }

                if (!string.IsNullOrEmpty(framework))
                {
                    runArguments.Add($"--framework {framework}");
                }

                if (!string.IsNullOrEmpty(launchProfile))
                {
                    runArguments.Add($"--launch-profile {launchProfile}");
                }

                if (!string.IsNullOrEmpty(verbosity))
                {
                    runArguments.Add($"--verbosity {verbosity}");
                }

                runArguments.Add("--no-build"); // We already built if needed

                // Add application arguments
                if (args != null && args.Length > 0)
                {
                    runArguments.Add("--");
                    runArguments.AddRange(args);
                }

                var runArgs = string.Join(" ", runArguments);
                WriteInfo($"Running: dotnet {runArgs}");

                // Show environment variables in verbose mode
                if (DotnetGlobalOptions.Verbose && environmentVariables.Count > 0)
                {
                    WriteInfo("Runtime environment variables:");
                    foreach (var envVar in environmentVariables.Take(10)) // Show first 10
                    {
                        WriteInfo($"  {envVar.Key}={envVar.Value}");
                    }
                    if (environmentVariables.Count > 10)
                    {
                        WriteInfo($"  ... and {environmentVariables.Count - 10} more");
                    }
                }

                WriteSuccess("🎯 Starting application...");
                WriteInfo("Press Ctrl+C to stop the application");

                var result = await RunDotnetCommandAsync(
                    runArgs, 
                    Path.GetDirectoryName(projectPath), 
                    true, 
                    environmentVariables);

                if (result.Success)
                {
                    WriteSuccess("Application completed successfully");
                    return true;
                }
                else
                {
                    WriteError($"Application failed with exit code {result.ExitCode}");
                    if (!string.IsNullOrEmpty(result.Error))
                    {
                        WriteError($"Error: {result.Error}");
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                WriteError($"Failed to run application: {ex.Message}");
                return false;
            }
        }

        private string FindTuskLangConfigFile(string projectDir)
        {
            var configFiles = new[]
            {
                DotnetGlobalOptions.ConfigPath,
                Path.Combine(projectDir, "peanu.tsk"),
                Path.Combine(projectDir, "config.tsk"),
                Path.Combine(projectDir, "tusklang.tsk")
            };

            return configFiles.FirstOrDefault(f => !string.IsNullOrEmpty(f) && File.Exists(f));
        }
    }
} 