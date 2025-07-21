using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace TuskLang.CLI.Commands.TuskDotnet
{
    /// <summary>
    /// Build command implementation - Build .NET projects with TuskLang integration
    /// Provides comprehensive building with configuration processing and optimization
    /// </summary>
    public static class BuildCommand
    {
        public static Command CreateBuildCommand()
        {
            // Arguments
            var projectArgument = new Argument<string>(
                name: "project",
                description: "Project file or directory to build (optional)")
            {
                Arity = ArgumentArity.ZeroOrOne
            };

            // Options
            var configurationOption = new Option<string>(
                aliases: new[] { "--configuration", "-c" },
                getDefaultValue: () => "Debug",
                description: "Build configuration: Debug, Release")
            {
                AllowedValues = { "Debug", "Release" }
            };

            var environmentOption = new Option<string>(
                aliases: new[] { "--environment", "-e" },
                description: "Target environment for TuskLang configuration");

            var frameworkOption = new Option<string>(
                aliases: new[] { "--framework", "-f" },
                description: "Target framework to build for");

            var outputOption = new Option<string>(
                aliases: new[] { "--output", "-o" },
                description: "Output directory for build artifacts");

            var verbosityOption = new Option<string>(
                aliases: new[] { "--verbosity", "-v" },
                getDefaultValue: () => "minimal",
                description: "MSBuild verbosity: quiet, minimal, normal, detailed, diagnostic")
            {
                AllowedValues = { "quiet", "minimal", "normal", "detailed", "diagnostic" }
            };

            var noRestoreOption = new Option<bool>(
                aliases: new[] { "--no-restore" },
                description: "Skip package restore");

            var noDependenciesOption = new Option<bool>(
                aliases: new[] { "--no-dependencies" },
                description: "Don't build project dependencies");

            var parallelOption = new Option<bool>(
                aliases: new[] { "--parallel", "-p" },
                getDefaultValue: () => true,
                description: "Enable parallel build");

            var tuskProcessingOption = new Option<bool>(
                aliases: new[] { "--tusk-processing" },
                getDefaultValue: () => true,
                description: "Enable TuskLang configuration processing");

            // Create command
            var buildCommand = new Command("build", "Build .NET projects with TuskLang configuration integration")
            {
                projectArgument,
                configurationOption,
                environmentOption,
                frameworkOption,
                outputOption,
                verbosityOption,
                noRestoreOption,
                noDependenciesOption,
                parallelOption,
                tuskProcessingOption
            };

            buildCommand.SetHandler(async (project, configuration, environment, framework, output, verbosity, noRestore, noDependencies, parallel, tuskProcessing) =>
            {
                var command = new BuildCommandImplementation();
                Environment.ExitCode = await command.ExecuteAsync(
                    project, configuration, environment, framework, output, verbosity, noRestore, noDependencies, parallel, tuskProcessing);
            }, projectArgument, configurationOption, environmentOption, frameworkOption, outputOption, verbosityOption, noRestoreOption, noDependenciesOption, parallelOption, tuskProcessingOption);

            return buildCommand;
        }
    }

    /// <summary>
    /// Build command implementation with comprehensive .NET building and TuskLang integration
    /// </summary>
    public class BuildCommandImplementation : DotnetCommandBase
    {
        public async Task<int> ExecuteAsync(
            string project,
            string configuration,
            string environment,
            string framework,
            string output,
            string verbosity,
            bool noRestore,
            bool noDependencies,
            bool parallel,
            bool tuskProcessing)
        {
            return await ExecuteDotnetCommandAsync(async () =>
            {
                var buildResult = new BuildResult
                {
                    StartedAt = DateTime.UtcNow,
                    Configuration = configuration,
                    Environment = environment,
                    Success = false
                };

                try
                {
                    // Find project file
                    var projectPath = FindProjectFile(project);
                    if (projectPath == null)
                        return 1;

                    buildResult.ProjectPath = projectPath;
                    
                    WriteProcessing($"🔨 Building .NET project with TuskLang integration...");
                    WriteInfo($"Project: {Path.GetFileName(projectPath)}");
                    WriteInfo($"Configuration: {configuration}");
                    WriteInfo($"Environment: {environment ?? "default"}");

                    // Process TuskLang configuration if enabled
                    if (tuskProcessing)
                    {
                        var configProcessed = await ProcessTuskLangConfigurationAsync(projectPath, environment);
                        if (!configProcessed)
                        {
                            WriteWarning("TuskLang configuration processing failed, continuing with build...");
                        }
                    }

                    // Restore packages if not skipped
                    if (!noRestore)
                    {
                        WriteProcessing("Restoring NuGet packages...");
                        var restoreResult = await RestorePackagesAsync(projectPath);
                        if (!restoreResult.Success)
                        {
                            WriteError($"Package restore failed: {restoreResult.Error}");
                            return 1;
                        }
                        WriteSuccess("Packages restored");
                    }

                    // Build the project
                    var buildSuccess = await BuildProjectAsync(
                        projectPath, configuration, framework, output, verbosity, noDependencies, parallel);

                    if (!buildSuccess)
                        return 1;

                    // Generate build artifacts info
                    await GenerateBuildManifestAsync(buildResult, projectPath, output);

                    buildResult.CompletedAt = DateTime.UtcNow;
                    buildResult.Success = true;
                    buildResult.BuildDuration = buildResult.CompletedAt - buildResult.StartedAt;

                    WriteSuccess($"✅ Build completed successfully in {buildResult.BuildDuration.TotalMilliseconds:F0}ms");
                    
                    if (DotnetGlobalOptions.JsonOutput)
                    {
                        var json = JsonSerializer.Serialize(buildResult, new JsonSerializerOptions { WriteIndented = true });
                        Console.WriteLine(json);
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

        private async Task<bool> ProcessTuskLangConfigurationAsync(string projectPath, string environment)
        {
            try
            {
                WriteInfo("Processing TuskLang configuration...");

                // Find TuskLang configuration
                var projectDir = Path.GetDirectoryName(projectPath);
                var configPath = FindTuskLangConfigFile(projectDir);
                
                if (string.IsNullOrEmpty(configPath))
                {
                    WriteWarning("No TuskLang configuration found");
                    return true; // Not an error - project might not use TuskLang
                }

                // Load main configuration
                var tsk = await LoadTuskLangConfigAsync(configPath);
                if (tsk == null)
                    return false;

                WriteInfo($"Loaded TuskLang configuration: {Path.GetFileName(configPath)}");

                // Load environment-specific configuration if specified
                if (!string.IsNullOrEmpty(environment))
                {
                    var envConfigPath = Path.Combine(projectDir, "config", $"{environment.ToLower()}.tsk");
                    if (File.Exists(envConfigPath))
                    {
                        var envTsk = await LoadTuskLangConfigAsync(envConfigPath);
                        if (envTsk != null)
                        {
                            WriteInfo($"Loaded environment configuration: {environment}");
                        }
                    }
                }

                // Generate MSBuild properties from TuskLang configuration
                await GenerateMSBuildPropsAsync(projectDir, tsk, environment);

                WriteSuccess("TuskLang configuration processed");
                return true;
            }
            catch (Exception ex)
            {
                WriteWarning($"Failed to process TuskLang configuration: {ex.Message}");
                return false;
            }
        }

        private async Task<DotnetCommandResult> RestorePackagesAsync(string projectPath)
        {
            var arguments = $"restore \"{projectPath}\"";
            return await RunDotnetCommandAsync(arguments, Path.GetDirectoryName(projectPath), false);
        }

        private async Task<bool> BuildProjectAsync(
            string projectPath,
            string configuration,
            string framework,
            string output,
            string verbosity,
            bool noDependencies,
            bool parallel)
        {
            try
            {
                var arguments = new List<string>
                {
                    "build",
                    $"\"{projectPath}\"",
                    $"--configuration {configuration}",
                    $"--verbosity {verbosity}",
                    "--no-restore" // We already restored
                };

                if (!string.IsNullOrEmpty(framework))
                {
                    arguments.Add($"--framework {framework}");
                }

                if (!string.IsNullOrEmpty(output))
                {
                    arguments.Add($"--output \"{output}\"");
                }

                if (noDependencies)
                {
                    arguments.Add("--no-dependencies");
                }

                if (parallel)
                {
                    arguments.Add($"--maxcpucount:{Environment.ProcessorCount}");
                }

                // Add TuskLang-specific MSBuild properties
                arguments.Add("/p:EnableTuskLangIntegration=true");
                
                if (!string.IsNullOrEmpty(DotnetGlobalOptions.ConfigPath))
                {
                    arguments.Add($"/p:TuskLangConfigFile={DotnetGlobalOptions.ConfigPath}");
                }

                var buildArgs = string.Join(" ", arguments);
                WriteInfo($"Running: dotnet {buildArgs}");

                var result = await RunDotnetCommandAsync(buildArgs, Path.GetDirectoryName(projectPath), true);

                if (!result.Success)
                {
                    WriteError($"Build failed with exit code {result.ExitCode}");
                    if (!string.IsNullOrEmpty(result.Error))
                    {
                        WriteError($"Build errors:\n{result.Error}");
                    }
                    return false;
                }

                WriteSuccess($"Project built successfully");
                return true;
            }
            catch (Exception ex)
            {
                WriteError($"Build error: {ex.Message}");
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

        private async Task GenerateMSBuildPropsAsync(string projectDir, TSK tsk, string environment)
        {
            try
            {
                var propsPath = Path.Combine(projectDir, "obj", "TuskLang.props");
                Directory.CreateDirectory(Path.GetDirectoryName(propsPath));

                var props = new List<string>
                {
                    "<?xml version=\"1.0\" encoding=\"utf-8\"?>",
                    "<Project>",
                    "  <PropertyGroup>",
                    "    <TuskLangEnabled>true</TuskLangEnabled>"
                };

                if (!string.IsNullOrEmpty(environment))
                {
                    props.Add($"    <TuskLangEnvironment>{environment}</TuskLangEnvironment>");
                }

                // Extract relevant configuration values for MSBuild
                var data = tsk.ToDictionary();
                foreach (var section in data)
                {
                    if (section.Value is Dictionary<string, object> sectionData)
                    {
                        foreach (var property in sectionData)
                        {
                            if (property.Value is string strValue && IsMSBuildCompatible(strValue))
                            {
                                var propName = $"TuskLang_{section.Key}_{property.Key}";
                                props.Add($"    <{propName}>{strValue}</{propName}>");
                            }
                        }
                    }
                }

                props.AddRange(new[]
                {
                    "  </PropertyGroup>",
                    "</Project>"
                });

                await File.WriteAllTextAsync(propsPath, string.Join("\n", props));
                WriteInfo($"Generated MSBuild properties: {Path.GetFileName(propsPath)}");
            }
            catch (Exception ex)
            {
                WriteWarning($"Failed to generate MSBuild props: {ex.Message}");
            }
        }

        private bool IsMSBuildCompatible(string value)
        {
            // Check if value is suitable for MSBuild property
            return !string.IsNullOrWhiteSpace(value) && 
                   value.Length < 1000 && 
                   !value.Contains('<') && 
                   !value.Contains('>') &&
                   !value.Contains('\n');
        }

        private async Task GenerateBuildManifestAsync(BuildResult buildResult, string projectPath, string outputPath)
        {
            try
            {
                var projectInfo = await ReadProjectFileAsync(projectPath);
                if (projectInfo == null)
                    return;

                var outputDir = !string.IsNullOrEmpty(outputPath) 
                    ? outputPath 
                    : Path.Combine(Path.GetDirectoryName(projectPath), "bin", buildResult.Configuration);

                var manifest = new
                {
                    buildId = Guid.NewGuid().ToString(),
                    timestamp = buildResult.StartedAt,
                    duration = buildResult.BuildDuration,
                    project = new
                    {
                        name = projectInfo.FileName,
                        path = projectInfo.FilePath,
                        type = projectInfo.ProjectType.ToString(),
                        framework = projectInfo.TargetFramework
                    },
                    build = new
                    {
                        configuration = buildResult.Configuration,
                        environment = buildResult.Environment,
                        success = buildResult.Success,
                        outputPath = outputDir
                    },
                    artifacts = Directory.Exists(outputDir) 
                        ? Directory.GetFiles(outputDir, "*", SearchOption.AllDirectories)
                            .Select(f => new
                            {
                                path = Path.GetRelativePath(outputDir, f),
                                size = new FileInfo(f).Length,
                                modified = new FileInfo(f).LastWriteTimeUtc
                            })
                            .ToArray()
                        : Array.Empty<object>(),
                    tuskLang = new
                    {
                        enabled = true,
                        configFile = DotnetGlobalOptions.ConfigPath ?? "peanu.tsk",
                        environment = buildResult.Environment
                    }
                };

                var manifestPath = Path.Combine(outputDir, "build-manifest.json");
                Directory.CreateDirectory(Path.GetDirectoryName(manifestPath));
                
                var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(manifestPath, json);

                WriteInfo($"Build manifest created: {Path.GetFileName(manifestPath)}");
            }
            catch (Exception ex)
            {
                WriteWarning($"Failed to create build manifest: {ex.Message}");
            }
        }
    }

    #region Result Classes

    public class BuildResult
    {
        public DateTime StartedAt { get; set; }
        public DateTime CompletedAt { get; set; }
        public TimeSpan BuildDuration { get; set; }
        public string ProjectPath { get; set; }
        public string Configuration { get; set; }
        public string Environment { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public List<string> Artifacts { get; set; } = new List<string>();
        public Dictionary<string, object> TuskLangConfig { get; set; } = new Dictionary<string, object>();
    }

    #endregion
} 