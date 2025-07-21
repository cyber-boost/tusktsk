using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TuskLang;

namespace TuskLang.CLI.Commands.Tusk
{
    /// <summary>
    /// Build command implementation - Build current project with all configurations
    /// Provides comprehensive building with environment support, validation, and compilation
    /// </summary>
    public static class BuildCommand
    {
        public static Command CreateBuildCommand()
        {
            // Options
            var configOption = new Option<string>(
                aliases: new[] { "--config", "-c" },
                getDefaultValue: () => "peanu.tsk",
                description: "Main configuration file to build");

            var environmentOption = new Option<string>(
                aliases: new[] { "--environment", "-e" },
                description: "Build for specific environment (development, staging, production)");

            var outputOption = new Option<string>(
                aliases: new[] { "--output", "-o" },
                getDefaultValue: () => "build",
                description: "Output directory for build artifacts");

            var cleanOption = new Option<bool>(
                aliases: new[] { "--clean" },
                description: "Clean build directory before building");

            var validateOption = new Option<bool>(
                aliases: new[] { "--validate" },
                getDefaultValue: () => true,
                description: "Validate configurations before building");

            var compileOption = new Option<bool>(
                aliases: new[] { "--compile" },
                getDefaultValue: () => true,
                description: "Compile configurations to binary format");

            var parallelOption = new Option<bool>(
                aliases: new[] { "--parallel", "-p" },
                getDefaultValue: () => true,
                description: "Enable parallel processing");

            var verboseOption = new Option<bool>(
                aliases: new[] { "--verbose", "-v" },
                description: "Enable verbose build output");

            // Create command
            var buildCommand = new Command("build", "Build current project with all configurations and environments")
            {
                configOption,
                environmentOption,
                outputOption,
                cleanOption,
                validateOption,
                compileOption,
                parallelOption,
                verboseOption
            };

            buildCommand.SetHandler(async (config, environment, output, clean, validate, compile, parallel, verbose) =>
            {
                var command = new BuildCommandImplementation();
                Environment.ExitCode = await command.ExecuteAsync(
                    config, environment, output, clean, validate, compile, parallel, verbose);
            }, configOption, environmentOption, outputOption, cleanOption, validateOption, compileOption, parallelOption, verboseOption);

            return buildCommand;
        }
    }

    /// <summary>
    /// Build command implementation with comprehensive build capabilities
    /// </summary>
    public class BuildCommandImplementation : CommandBase
    {
        public async Task<int> ExecuteAsync(
            string config,
            string environment,
            string output,
            bool clean,
            bool validate,
            bool compile,
            bool parallel,
            bool verbose)
        {
            return await ExecuteWithTimingAsync(async () =>
            {
                var buildResult = new BuildResult
                {
                    StartedAt = DateTime.UtcNow,
                    Success = false,
                    BuildSteps = new List<BuildStep>()
                };

                try
                {
                    // Setup verbose logging
                    if (verbose) GlobalOptions.Verbose = true;

                    WriteProcessing($"🔨 Starting build process...");
                    WriteInfo($"Configuration: {config}");
                    WriteInfo($"Environment: {environment ?? "all"}");
                    WriteInfo($"Output: {output}");

                    // Step 1: Clean build directory
                    if (clean)
                    {
                        await ExecuteBuildStep(buildResult, "Clean", async () => await CleanBuildDirectoryAsync(output));
                    }

                    // Step 2: Discover configuration files
                    var configFiles = await ExecuteBuildStep(buildResult, "Discovery", async () => 
                        await DiscoverConfigurationFilesAsync(config, environment));

                    // Step 3: Validate configurations
                    if (validate)
                    {
                        await ExecuteBuildStep(buildResult, "Validation", async () => 
                            await ValidateConfigurationsAsync(configFiles));
                    }

                    // Step 4: Process configurations
                    var processedConfigs = await ExecuteBuildStep(buildResult, "Processing", async () => 
                        await ProcessConfigurationsAsync(configFiles, parallel));

                    // Step 5: Compile configurations
                    if (compile)
                    {
                        await ExecuteBuildStep(buildResult, "Compilation", async () => 
                            await CompileConfigurationsAsync(processedConfigs, output, parallel));
                    }

                    // Step 6: Generate build manifest
                    await ExecuteBuildStep(buildResult, "Manifest", async () => 
                        await GenerateBuildManifestAsync(buildResult, output));

                    // Step 7: Package artifacts
                    await ExecuteBuildStep(buildResult, "Packaging", async () => 
                        await PackageBuildArtifactsAsync(output, processedConfigs));

                    buildResult.CompletedAt = DateTime.UtcNow;
                    buildResult.Success = true;
                    buildResult.BuildDuration = buildResult.CompletedAt - buildResult.StartedAt;

                    WriteSuccess($"✅ Build completed successfully in {buildResult.BuildDuration.TotalMilliseconds:F0}ms");
                    WriteInfo($"📦 Build artifacts: {output}");
                    WriteInfo($"📄 Processed {configFiles.Count} configuration files");

                    if (GlobalOptions.JsonOutput)
                    {
                        OutputResult(buildResult);
                    }

                    return 0;
                }
                catch (Exception ex)
                {
                    buildResult.CompletedAt = DateTime.UtcNow;
                    buildResult.Success = false;
                    buildResult.ErrorMessage = ex.Message;
                    buildResult.BuildDuration = buildResult.CompletedAt - buildResult.StartedAt;

                    WriteError($"❌ Build failed: {ex.Message}");
                    return 1;
                }
            }, "Build");
        }

        private async Task<T> ExecuteBuildStep<T>(BuildResult buildResult, string stepName, Func<Task<T>> stepAction)
        {
            var step = new BuildStep
            {
                Name = stepName,
                StartedAt = DateTime.UtcNow
            };

            try
            {
                WriteProcessing($"🔄 {stepName}...");
                var result = await stepAction();
                
                step.CompletedAt = DateTime.UtcNow;
                step.Duration = step.CompletedAt - step.StartedAt;
                step.Success = true;
                
                WriteInfo($"✅ {stepName} completed ({step.Duration.TotalMilliseconds:F0}ms)");
                
                buildResult.BuildSteps.Add(step);
                return result;
            }
            catch (Exception ex)
            {
                step.CompletedAt = DateTime.UtcNow;
                step.Duration = step.CompletedAt - step.StartedAt;
                step.Success = false;
                step.ErrorMessage = ex.Message;
                
                WriteError($"❌ {stepName} failed: {ex.Message}");
                
                buildResult.BuildSteps.Add(step);
                throw;
            }
        }

        private async Task CleanBuildDirectoryAsync(string outputDir)
        {
            if (Directory.Exists(outputDir))
            {
                WriteInfo($"Cleaning build directory: {outputDir}");
                Directory.Delete(outputDir, true);
            }
            Directory.CreateDirectory(outputDir);
        }

        private async Task<List<ConfigurationFile>> DiscoverConfigurationFilesAsync(string mainConfig, string environment)
        {
            var configFiles = new List<ConfigurationFile>();

            // Add main configuration file
            if (File.Exists(mainConfig))
            {
                configFiles.Add(new ConfigurationFile
                {
                    Path = Path.GetFullPath(mainConfig),
                    Type = "main",
                    Environment = "all",
                    Priority = 1
                });
            }
            else
            {
                throw new FileNotFoundException($"Main configuration file not found: {mainConfig}");
            }

            // Discover environment-specific configurations
            var configDir = Path.Combine(Directory.GetCurrentDirectory(), "config");
            if (Directory.Exists(configDir))
            {
                var envFiles = Directory.GetFiles(configDir, "*.tsk");
                
                foreach (var envFile in envFiles)
                {
                    var envName = Path.GetFileNameWithoutExtension(envFile);
                    
                    // If specific environment requested, only include that one
                    if (!string.IsNullOrEmpty(environment) && envName != environment)
                        continue;

                    configFiles.Add(new ConfigurationFile
                    {
                        Path = Path.GetFullPath(envFile),
                        Type = "environment",
                        Environment = envName,
                        Priority = GetEnvironmentPriority(envName)
                    });
                }
            }

            // Discover template configurations
            var templatesDir = Path.Combine(Directory.GetCurrentDirectory(), "templates");
            if (Directory.Exists(templatesDir))
            {
                var templateFiles = Directory.GetFiles(templatesDir, "*.tsk");
                
                foreach (var templateFile in templateFiles)
                {
                    configFiles.Add(new ConfigurationFile
                    {
                        Path = Path.GetFullPath(templateFile),
                        Type = "template",
                        Environment = "template",
                        Priority = 99
                    });
                }
            }

            WriteInfo($"Discovered {configFiles.Count} configuration files");
            return configFiles.OrderBy(f => f.Priority).ToList();
        }

        private int GetEnvironmentPriority(string environment)
        {
            return environment switch
            {
                "development" => 10,
                "staging" => 20,
                "production" => 30,
                _ => 50
            };
        }

        private async Task ValidateConfigurationsAsync(List<ConfigurationFile> configFiles)
        {
            var validationTasks = configFiles.Select(async file =>
            {
                try
                {
                    WriteInfo($"Validating: {Path.GetFileName(file.Path)}");
                    var tsk = await LoadTskFileAsync(file.Path);
                    if (tsk == null)
                        throw new Exception($"Failed to load configuration: {file.Path}");

                    var validationResult = await ValidateTskContentAsync(tsk);
                    if (!validationResult.IsValid)
                    {
                        var issues = string.Join(", ", validationResult.Issues);
                        throw new Exception($"Validation failed for {file.Path}: {issues}");
                    }

                    file.Valid = true;
                }
                catch (Exception ex)
                {
                    file.Valid = false;
                    file.ValidationError = ex.Message;
                    throw;
                }
            });

            await Task.WhenAll(validationTasks);
        }

        private async Task<List<ProcessedConfiguration>> ProcessConfigurationsAsync(List<ConfigurationFile> configFiles, bool parallel)
        {
            var processedConfigs = new List<ProcessedConfiguration>();

            if (parallel)
            {
                var processingTasks = configFiles.Select(async file =>
                {
                    return await ProcessSingleConfigurationAsync(file);
                });

                var results = await Task.WhenAll(processingTasks);
                processedConfigs.AddRange(results);
            }
            else
            {
                foreach (var file in configFiles)
                {
                    var processed = await ProcessSingleConfigurationAsync(file);
                    processedConfigs.Add(processed);
                }
            }

            WriteInfo($"Processed {processedConfigs.Count} configurations");
            return processedConfigs;
        }

        private async Task<ProcessedConfiguration> ProcessSingleConfigurationAsync(ConfigurationFile file)
        {
            WriteInfo($"Processing: {Path.GetFileName(file.Path)}");

            var tsk = await LoadTskFileAsync(file.Path);
            if (tsk == null)
                throw new Exception($"Failed to load configuration: {file.Path}");

            var processed = new ProcessedConfiguration
            {
                SourceFile = file,
                Data = tsk.ToDictionary(),
                ProcessedAt = DateTime.UtcNow,
                OutputFileName = GenerateOutputFileName(file)
            };

            // Perform configuration processing (merge, resolve references, etc.)
            processed.Data = await ResolveConfigurationReferencesAsync(processed.Data);
            processed.Statistics = CalculateConfigurationStatistics(processed.Data);

            return processed;
        }

        private string GenerateOutputFileName(ConfigurationFile file)
        {
            var baseName = Path.GetFileNameWithoutExtension(file.Path);
            var suffix = file.Environment != "all" ? $".{file.Environment}" : "";
            return $"{baseName}{suffix}";
        }

        private async Task<Dictionary<string, object>> ResolveConfigurationReferencesAsync(Dictionary<string, object> data)
        {
            // Simple reference resolution - in production would be more sophisticated
            var resolved = new Dictionary<string, object>();

            foreach (var section in data)
            {
                if (section.Value is Dictionary<string, object> sectionData)
                {
                    var resolvedSection = new Dictionary<string, object>();
                    
                    foreach (var property in sectionData)
                    {
                        var value = property.Value;
                        
                        // Resolve environment variable references
                        if (value is string strValue && strValue.Contains("${"))
                        {
                            value = ResolveEnvironmentVariables(strValue);
                        }
                        
                        resolvedSection[property.Key] = value;
                    }
                    
                    resolved[section.Key] = resolvedSection;
                }
                else
                {
                    resolved[section.Key] = section.Value;
                }
            }

            return resolved;
        }

        private string ResolveEnvironmentVariables(string input)
        {
            // Simple environment variable resolution
            var result = input;
            var envVarPattern = @"\$\{([^}]+)\}";
            var matches = System.Text.RegularExpressions.Regex.Matches(input, envVarPattern);
            
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                var envVar = match.Groups[1].Value;
                var envValue = Environment.GetEnvironmentVariable(envVar) ?? $"${{{envVar}}}";
                result = result.Replace(match.Value, envValue);
            }
            
            return result;
        }

        private ConfigurationStatistics CalculateConfigurationStatistics(Dictionary<string, object> data)
        {
            var stats = new ConfigurationStatistics
            {
                SectionCount = data.Count,
                PropertyCount = 0,
                EnvironmentReferences = 0,
                FunctionCount = 0
            };

            foreach (var section in data.Values)
            {
                if (section is Dictionary<string, object> sectionData)
                {
                    stats.PropertyCount += sectionData.Count;
                    
                    foreach (var value in sectionData.Values)
                    {
                        if (value is string strValue)
                        {
                            if (strValue.Contains("${"))
                                stats.EnvironmentReferences++;
                            if (strValue.Contains("function") || strValue.Contains("=>"))
                                stats.FunctionCount++;
                        }
                    }
                }
            }

            return stats;
        }

        private async Task CompileConfigurationsAsync(List<ProcessedConfiguration> configs, string outputDir, bool parallel)
        {
            var compiledDir = Path.Combine(outputDir, "compiled");
            Directory.CreateDirectory(compiledDir);

            if (parallel)
            {
                var compilationTasks = configs.Select(async config =>
                {
                    await CompileSingleConfigurationAsync(config, compiledDir);
                });

                await Task.WhenAll(compilationTasks);
            }
            else
            {
                foreach (var config in configs)
                {
                    await CompileSingleConfigurationAsync(config, compiledDir);
                }
            }

            WriteInfo($"Compiled {configs.Count} configurations to {compiledDir}");
        }

        private async Task CompileSingleConfigurationAsync(ProcessedConfiguration config, string outputDir)
        {
            WriteInfo($"Compiling: {config.OutputFileName}");

            // Create temporary TSK for compilation
            var tempTsk = new TSK(config.Data);
            var outputPath = Path.Combine(outputDir, $"{config.OutputFileName}.pnt");

            // Use the CompileCommand implementation
            var compileCommand = new CompileCommandImplementation();
            
            // Create temporary file for compilation
            var tempFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempFile, tempTsk.ToString());

            try
            {
                var result = await compileCommand.ExecuteAsync(
                    tempFile, outputPath, true, "gzip", false, true, false, true);
                
                if (result != 0)
                    throw new Exception($"Compilation failed for {config.OutputFileName}");
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        private async Task GenerateBuildManifestAsync(BuildResult buildResult, string outputDir)
        {
            var manifest = new BuildManifest
            {
                BuildId = Guid.NewGuid().ToString(),
                BuildVersion = "1.0.0",
                BuildTimestamp = buildResult.StartedAt,
                BuildDuration = buildResult.BuildDuration,
                Success = buildResult.Success,
                Environment = Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "unknown",
                BuildSteps = buildResult.BuildSteps,
                Artifacts = Directory.GetFiles(outputDir, "*", SearchOption.AllDirectories)
                    .Select(f => Path.GetRelativePath(outputDir, f))
                    .ToList()
            };

            var manifestJson = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
            await SaveFileAtomicAsync(Path.Combine(outputDir, "build-manifest.json"), manifestJson, "build manifest");
        }

        private async Task PackageBuildArtifactsAsync(string outputDir, List<ProcessedConfiguration> configs)
        {
            // Create summary file
            var summary = new
            {
                build_completed = DateTime.UtcNow,
                configurations_processed = configs.Count,
                environments = configs.Select(c => c.SourceFile.Environment).Distinct().ToList(),
                artifacts = Directory.GetFiles(outputDir, "*", SearchOption.AllDirectories)
                    .Select(f => new
                    {
                        path = Path.GetRelativePath(outputDir, f),
                        size = new FileInfo(f).Length,
                        modified = new FileInfo(f).LastWriteTimeUtc
                    })
                    .ToList()
            };

            var summaryJson = JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true });
            await SaveFileAtomicAsync(Path.Combine(outputDir, "build-summary.json"), summaryJson, "build summary");

            WriteInfo($"Created build package with {summary.artifacts.Count} artifacts");
        }
    }

    #region Result Classes

    public class BuildResult
    {
        public DateTime StartedAt { get; set; }
        public DateTime CompletedAt { get; set; }
        public TimeSpan BuildDuration { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public List<BuildStep> BuildSteps { get; set; } = new List<BuildStep>();
    }

    public class BuildStep
    {
        public string Name { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime CompletedAt { get; set; }
        public TimeSpan Duration { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class ConfigurationFile
    {
        public string Path { get; set; }
        public string Type { get; set; } // main, environment, template
        public string Environment { get; set; }
        public int Priority { get; set; }
        public bool Valid { get; set; }
        public string ValidationError { get; set; }
    }

    public class ProcessedConfiguration
    {
        public ConfigurationFile SourceFile { get; set; }
        public Dictionary<string, object> Data { get; set; }
        public DateTime ProcessedAt { get; set; }
        public string OutputFileName { get; set; }
        public ConfigurationStatistics Statistics { get; set; }
    }

    public class ConfigurationStatistics
    {
        public int SectionCount { get; set; }
        public int PropertyCount { get; set; }
        public int EnvironmentReferences { get; set; }
        public int FunctionCount { get; set; }
    }

    public class BuildManifest
    {
        public string BuildId { get; set; }
        public string BuildVersion { get; set; }
        public DateTime BuildTimestamp { get; set; }
        public TimeSpan BuildDuration { get; set; }
        public bool Success { get; set; }
        public string Environment { get; set; }
        public List<BuildStep> BuildSteps { get; set; }
        public List<string> Artifacts { get; set; }
    }

    #endregion
} 