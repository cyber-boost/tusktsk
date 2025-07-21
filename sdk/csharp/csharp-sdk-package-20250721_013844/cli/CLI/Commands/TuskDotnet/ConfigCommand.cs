using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;

namespace TuskLang.CLI.Commands.TuskDotnet
{
    public static class ConfigCommand
    {
        public static Command CreateConfigCommand()
        {
            var operationArgument = new Argument<string>("operation", "Configuration operation: init, list, get, set, validate, generate")
            {
                AllowedValues = { "init", "list", "get", "set", "validate", "generate" }
            };

            var keyOption = new Option<string>("--key", "Configuration key");
            var valueOption = new Option<string>("--value", "Configuration value");
            var environmentOption = new Option<string>("--environment", "Environment name");
            var outputOption = new Option<string>("--output", "Output file path");

            var configCommand = new Command("config", "Manage .NET-specific TuskLang configurations")
            {
                operationArgument,
                keyOption,
                valueOption,
                environmentOption,
                outputOption
            };

            configCommand.SetHandler(async (operation, key, value, environment, output) =>
            {
                var command = new ConfigCommandImplementation();
                Environment.ExitCode = await command.ExecuteAsync(operation, key, value, environment, output);
            }, operationArgument, keyOption, valueOption, environmentOption, outputOption);

            return configCommand;
        }
    }

    public class ConfigCommandImplementation : DotnetCommandBase
    {
        public async Task<int> ExecuteAsync(string operation, string key, string value, string environment, string output)
        {
            return await ExecuteDotnetCommandAsync(async () =>
            {
                switch (operation.ToLower())
                {
                    case "init":
                        return await InitializeConfigAsync(environment, output);
                    case "list":
                        return await ListConfigurationsAsync(environment);
                    case "get":
                        return await GetConfigurationAsync(key, environment);
                    case "set":
                        return await SetConfigurationAsync(key, value, environment);
                    case "validate":
                        return await ValidateConfigurationAsync(environment);
                    case "generate":
                        return await GenerateConfigurationAsync(environment, output);
                    default:
                        WriteError($"Unknown operation: {operation}");
                        return 1;
                }
            }, $"Config {operation}");
        }

        private async Task<int> InitializeConfigAsync(string environment, string output)
        {
            WriteProcessing("Initializing TuskLang configuration for .NET project...");

            var configPath = output ?? "peanu.tsk";
            
            if (File.Exists(configPath))
            {
                WriteWarning($"Configuration file already exists: {configPath}");
                return 0;
            }

            var defaultConfig = CreateDefaultDotnetConfig();
            await SaveFileAtomicAsync(configPath, defaultConfig, "TuskLang configuration");
            
            WriteSuccess($"✅ Configuration initialized: {configPath}");
            return 0;
        }

        private async Task<int> ListConfigurationsAsync(string environment)
        {
            WriteProcessing("Listing TuskLang configurations...");

            var configFiles = new[]
            {
                "peanu.tsk",
                "config.tsk",
                Path.Combine("config", "development.tsk"),
                Path.Combine("config", "staging.tsk"),
                Path.Combine("config", "production.tsk")
            };

            bool foundAny = false;
            foreach (var configFile in configFiles)
            {
                if (File.Exists(configFile))
                {
                    foundAny = true;
                    var size = new FileInfo(configFile).Length;
                    var modified = File.GetLastWriteTime(configFile);
                    WriteInfo($"📄 {configFile} ({size} bytes, modified {modified:yyyy-MM-dd HH:mm})");
                }
            }

            if (!foundAny)
            {
                WriteWarning("No TuskLang configuration files found");
            }

            return 0;
        }

        private async Task<int> GetConfigurationAsync(string key, string environment)
        {
            if (string.IsNullOrEmpty(key))
            {
                WriteError("Configuration key is required for get operation");
                return 1;
            }

            var configPath = FindConfigFile(environment);
            if (string.IsNullOrEmpty(configPath))
            {
                WriteError("No configuration file found");
                return 1;
            }

            var tsk = await LoadTuskLangConfigAsync(configPath);
            if (tsk == null)
                return 1;

            var config = tsk.ToDictionary();
            var keyParts = key.Split('.');
            
            object currentValue = config;
            foreach (var part in keyParts)
            {
                if (currentValue is Dictionary<string, object> dict && dict.ContainsKey(part))
                {
                    currentValue = dict[part];
                }
                else
                {
                    WriteError($"Configuration key not found: {key}");
                    return 1;
                }
            }

            if (DotnetGlobalOptions.JsonOutput)
            {
                Console.WriteLine(JsonSerializer.Serialize(currentValue, new JsonSerializerOptions { WriteIndented = true }));
            }
            else
            {
                Console.WriteLine($"{key} = {currentValue}");
            }

            return 0;
        }

        private async Task<int> SetConfigurationAsync(string key, string value, string environment)
        {
            WriteError("Set configuration operation not yet implemented");
            return 1;
        }

        private async Task<int> ValidateConfigurationAsync(string environment)
        {
            WriteProcessing("Validating TuskLang configuration...");

            var configPath = FindConfigFile(environment);
            if (string.IsNullOrEmpty(configPath))
            {
                WriteError("No configuration file found");
                return 1;
            }

            var tsk = await LoadTskFileAsync(configPath);
            if (tsk == null)
            {
                WriteError("Invalid configuration file");
                return 1;
            }

            WriteSuccess("✅ Configuration is valid");
            return 0;
        }

        private async Task<int> GenerateConfigurationAsync(string environment, string output)
        {
            WriteProcessing($"Generating configuration for environment: {environment ?? "default"}");

            var outputPath = output ?? $"config/{environment?.ToLower() ?? "default"}.tsk";
            
            await GenerateEnvironmentConfigAsync(environment ?? "Development", outputPath);
            
            WriteSuccess($"✅ Configuration generated: {outputPath}");
            return 0;
        }

        private string FindConfigFile(string environment)
        {
            if (!string.IsNullOrEmpty(environment))
            {
                var envConfig = Path.Combine("config", $"{environment.ToLower()}.tsk");
                if (File.Exists(envConfig))
                    return envConfig;
            }

            var defaultConfigs = new[] { "peanu.tsk", "config.tsk", "tusklang.tsk" };
            return Array.Find(defaultConfigs, File.Exists);
        }

        private string CreateDefaultDotnetConfig()
        {
            return @"# TuskLang Configuration for .NET Project
# Generated by tusk-dotnet config init

project:
  name: ""${PROJECT_NAME}""
  version: ""1.0.0""
  description: "".NET project with TuskLang integration""

application:
  name: ""${PROJECT_NAME}""
  environment: ""${ASPNETCORE_ENVIRONMENT}""
  debug: ""${DEBUG}""

logging:
  level: ""${LOG_LEVEL}""
  console: true
  file: false

# Uncomment for web applications
# server:
#   urls: ""${ASPNETCORE_URLS}""
#   port: ""${PORT}""
#   https_port: ""${HTTPS_PORT}""

# Environment-specific configurations
# Use config/development.tsk, config/staging.tsk, config/production.tsk
";
        }
    }
} 