using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using TuskLang;

namespace TuskLang.CLI.Commands
{
    /// <summary>
    /// Base class for all tusk-dotnet commands providing .NET-specific functionality,
    /// SDK integration, project operations, and MSBuild integration
    /// </summary>
    public abstract class DotnetCommandBase : CommandBase
    {
        protected readonly Dictionary<string, object> _dotnetContext;
        protected string _detectedSdkVersion;
        protected string _detectedSdkPath;

        public DotnetCommandBase()
        {
            _dotnetContext = new Dictionary<string, object>();
        }

        /// <summary>
        /// Execute .NET command with SDK detection and timing
        /// </summary>
        protected async Task<int> ExecuteDotnetCommandAsync(Func<Task<int>> commandAction, string commandName)
        {
            return await ExecuteWithTimingAsync(async () =>
            {
                // Detect .NET SDK
                if (!await DetectDotnetSdkAsync())
                {
                    WriteError("No .NET SDK found. Please install .NET SDK 6.0 or later.");
                    return 1;
                }

                WriteProcessing($"Executing {commandName}...");
                var result = await commandAction();
                
                if (result == 0)
                {
                    WriteSuccess($"{commandName} completed successfully");
                }
                else
                {
                    WriteError($"{commandName} failed");
                }

                return result;
            }, commandName);
        }

        /// <summary>
        /// Detect .NET SDK installation and version
        /// </summary>
        protected async Task<bool> DetectDotnetSdkAsync()
        {
            try
            {
                var result = await RunDotnetCommandAsync("--version", null, false);
                if (result.Success)
                {
                    _detectedSdkVersion = result.Output.Trim();
                    DotnetGlobalOptions.WriteSdkInfo(_detectedSdkVersion, "System PATH");
                    
                    // Get SDK location
                    var infoResult = await RunDotnetCommandAsync("--info", null, false);
                    if (infoResult.Success)
                    {
                        var lines = infoResult.Output.Split('\n');
                        var basePath = lines.FirstOrDefault(l => l.Contains("Base Path:"));
                        if (basePath != null)
                        {
                            _detectedSdkPath = basePath.Split(':')[1].Trim();
                        }
                    }
                    
                    return true;
                }
            }
            catch (Exception ex)
            {
                WriteError($"Failed to detect .NET SDK: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Run a dotnet CLI command and capture output
        /// </summary>
        protected async Task<DotnetCommandResult> RunDotnetCommandAsync(
            string arguments, 
            string workingDirectory = null, 
            bool showOutput = true, 
            Dictionary<string, string> environmentVariables = null)
        {
            var result = new DotnetCommandResult();
            
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory ?? Directory.GetCurrentDirectory(),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                // Add environment variables if provided
                if (environmentVariables != null)
                {
                    foreach (var kvp in environmentVariables)
                    {
                        startInfo.EnvironmentVariables[kvp.Key] = kvp.Value;
                    }
                }

                var stopwatch = Stopwatch.StartNew();
                
                using var process = new Process { StartInfo = startInfo };
                
                var outputLines = new List<string>();
                var errorLines = new List<string>();

                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        outputLines.Add(e.Data);
                        if (showOutput && DotnetGlobalOptions.Verbose)
                        {
                            Console.WriteLine($"  {e.Data}");
                        }
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        errorLines.Add(e.Data);
                        if (showOutput)
                        {
                            Console.Error.WriteLine($"  {e.Data}");
                        }
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                
                await process.WaitForExitAsync();
                stopwatch.Stop();

                result.Success = process.ExitCode == 0;
                result.ExitCode = process.ExitCode;
                result.Output = string.Join("\n", outputLines);
                result.Error = string.Join("\n", errorLines);
                result.ExecutionTime = stopwatch.ElapsedMilliseconds;

                DotnetGlobalOptions.WriteTimer($"dotnet {arguments}", result.ExecutionTime);

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex.Message;
                WriteError($"Failed to run dotnet {arguments}: {ex.Message}");
                return result;
            }
        }

        /// <summary>
        /// Find and validate project file
        /// </summary>
        protected string FindProjectFile(string projectPath = null)
        {
            string targetDirectory;

            if (!string.IsNullOrEmpty(projectPath))
            {
                if (File.Exists(projectPath) && Path.GetExtension(projectPath).ToLower().EndsWith("proj"))
                {
                    return projectPath;
                }

                if (Directory.Exists(projectPath))
                {
                    targetDirectory = projectPath;
                }
                else
                {
                    WriteError($"Project path not found: {projectPath}");
                    return null;
                }
            }
            else
            {
                targetDirectory = Directory.GetCurrentDirectory();
            }

            var projectFiles = DotnetGlobalOptions.FindProjectFiles(targetDirectory);
            
            if (projectFiles.Length == 0)
            {
                WriteError($"No project files found in: {targetDirectory}");
                return null;
            }

            if (projectFiles.Length == 1)
            {
                WriteInfo($"Using project: {Path.GetFileName(projectFiles[0])}");
                return projectFiles[0];
            }

            // Multiple projects found - let user choose or use first
            WriteWarning($"Multiple project files found in {targetDirectory}:");
            foreach (var project in projectFiles)
            {
                WriteWarning($"  - {Path.GetFileName(project)}");
            }
            WriteInfo($"Using: {Path.GetFileName(projectFiles[0])}");
            
            return projectFiles[0];
        }

        /// <summary>
        /// Read and parse project file
        /// </summary>
        protected async Task<ProjectInfo> ReadProjectFileAsync(string projectPath)
        {
            try
            {
                if (!File.Exists(projectPath))
                {
                    WriteError($"Project file not found: {projectPath}");
                    return null;
                }

                var content = await File.ReadAllTextAsync(projectPath);
                var doc = new XmlDocument();
                doc.LoadXml(content);

                var projectInfo = new ProjectInfo
                {
                    FilePath = projectPath,
                    FileName = Path.GetFileName(projectPath),
                    Directory = Path.GetDirectoryName(projectPath),
                    ProjectType = DetermineProjectType(doc),
                    TargetFramework = GetTargetFramework(doc),
                    PackageReferences = GetPackageReferences(doc),
                    ProjectReferences = GetProjectReferences(doc),
                    Properties = GetProjectProperties(doc)
                };

                return projectInfo;
            }
            catch (Exception ex)
            {
                WriteError($"Failed to read project file {projectPath}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Write project file with TuskLang integration
        /// </summary>
        protected async Task<bool> WriteProjectFileAsync(ProjectInfo projectInfo, bool addTuskLangIntegration = true)
        {
            try
            {
                var doc = new XmlDocument();
                var project = doc.CreateElement("Project");
                project.SetAttribute("Sdk", GetProjectSdk(projectInfo.ProjectType));
                doc.AppendChild(project);

                // Property group
                var propertyGroup = doc.CreateElement("PropertyGroup");
                project.AppendChild(propertyGroup);

                var targetFramework = doc.CreateElement("TargetFramework");
                targetFramework.InnerText = projectInfo.TargetFramework ?? DotnetGlobalOptions.GetTargetFramework();
                propertyGroup.AppendChild(targetFramework);

                // Add other properties
                foreach (var prop in projectInfo.Properties)
                {
                    var propElement = doc.CreateElement(prop.Key);
                    propElement.InnerText = prop.Value;
                    propertyGroup.AppendChild(propElement);
                }

                // TuskLang integration
                if (addTuskLangIntegration)
                {
                    var tuskLangEnabled = doc.CreateElement("EnableTuskLangIntegration");
                    tuskLangEnabled.InnerText = "true";
                    propertyGroup.AppendChild(tuskLangEnabled);

                    var tuskLangConfig = doc.CreateElement("TuskLangConfigFile");
                    tuskLangConfig.InnerText = DotnetGlobalOptions.ConfigPath ?? "peanu.tsk";
                    propertyGroup.AppendChild(tuskLangConfig);
                }

                // Package references
                if (projectInfo.PackageReferences.Count > 0)
                {
                    var itemGroup = doc.CreateElement("ItemGroup");
                    project.AppendChild(itemGroup);

                    foreach (var package in projectInfo.PackageReferences)
                    {
                        var packageRef = doc.CreateElement("PackageReference");
                        packageRef.SetAttribute("Include", package.Key);
                        packageRef.SetAttribute("Version", package.Value);
                        itemGroup.AppendChild(packageRef);
                    }
                }

                // Project references
                if (projectInfo.ProjectReferences.Count > 0)
                {
                    var itemGroup = doc.CreateElement("ItemGroup");
                    project.AppendChild(itemGroup);

                    foreach (var projRef in projectInfo.ProjectReferences)
                    {
                        var projectRef = doc.CreateElement("ProjectReference");
                        projectRef.SetAttribute("Include", projRef);
                        itemGroup.AppendChild(projectRef);
                    }
                }

                // Save with proper formatting
                using var writer = new StringWriter();
                using var xmlWriter = XmlWriter.Create(writer, new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = "  ",
                    NewLineChars = "\n"
                });
                doc.WriteTo(xmlWriter);

                await File.WriteAllTextAsync(projectInfo.FilePath, writer.ToString());
                WriteSuccess($"Project file updated: {projectInfo.FileName}");
                return true;
            }
            catch (Exception ex)
            {
                WriteError($"Failed to write project file: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Create TuskLang configuration integration for .NET project
        /// </summary>
        protected async Task<bool> AddTuskLangIntegrationAsync(string projectPath, string configPath = null)
        {
            try
            {
                var projectInfo = await ReadProjectFileAsync(projectPath);
                if (projectInfo == null) return false;

                // Add TuskLang NuGet package reference if not exists
                if (!projectInfo.PackageReferences.ContainsKey("TuskLang.Extensions"))
                {
                    projectInfo.PackageReferences["TuskLang.Extensions"] = "2.0.1";
                }

                if (!projectInfo.PackageReferences.ContainsKey("TuskLang.AspNetCore") && 
                    (projectInfo.ProjectType == DotnetProjectType.WebApi || projectInfo.ProjectType == DotnetProjectType.Mvc))
                {
                    projectInfo.PackageReferences["TuskLang.AspNetCore"] = "2.0.1";
                }

                // Add TuskLang properties
                projectInfo.Properties["EnableTuskLangIntegration"] = "true";
                projectInfo.Properties["TuskLangConfigFile"] = configPath ?? "peanu.tsk";

                return await WriteProjectFileAsync(projectInfo, true);
            }
            catch (Exception ex)
            {
                WriteError($"Failed to add TuskLang integration: {ex.Message}");
                return false;
            }
        }

        // Helper methods for project file parsing
        private DotnetProjectType DetermineProjectType(XmlDocument doc)
        {
            var sdk = doc.DocumentElement?.GetAttribute("Sdk");
            var outputType = doc.SelectSingleNode("//OutputType")?.InnerText;

            return sdk switch
            {
                "Microsoft.NET.Sdk.Web" => DotnetProjectType.WebApi,
                "Microsoft.NET.Sdk.Razor" => DotnetProjectType.Mvc,
                "Microsoft.NET.Sdk.BlazorWebAssembly" => DotnetProjectType.Blazor,
                "Microsoft.NET.Sdk.Worker" => DotnetProjectType.WorkerService,
                _ => outputType switch
                {
                    "Exe" => DotnetProjectType.Console,
                    "Library" => DotnetProjectType.ClassLibrary,
                    _ => DotnetProjectType.Console
                }
            };
        }

        private string GetTargetFramework(XmlDocument doc)
        {
            return doc.SelectSingleNode("//TargetFramework")?.InnerText ?? 
                   doc.SelectSingleNode("//TargetFrameworks")?.InnerText?.Split(';')[0] ?? 
                   "net8.0";
        }

        private Dictionary<string, string> GetPackageReferences(XmlDocument doc)
        {
            var packages = new Dictionary<string, string>();
            var packageNodes = doc.SelectNodes("//PackageReference");
            
            foreach (XmlNode node in packageNodes)
            {
                var name = node.Attributes?["Include"]?.Value;
                var version = node.Attributes?["Version"]?.Value;
                if (name != null && version != null)
                {
                    packages[name] = version;
                }
            }
            
            return packages;
        }

        private List<string> GetProjectReferences(XmlDocument doc)
        {
            var references = new List<string>();
            var refNodes = doc.SelectNodes("//ProjectReference");
            
            foreach (XmlNode node in refNodes)
            {
                var include = node.Attributes?["Include"]?.Value;
                if (include != null)
                {
                    references.Add(include);
                }
            }
            
            return references;
        }

        private Dictionary<string, string> GetProjectProperties(XmlDocument doc)
        {
            var properties = new Dictionary<string, string>();
            var propertyGroups = doc.SelectNodes("//PropertyGroup");
            
            foreach (XmlNode group in propertyGroups)
            {
                foreach (XmlNode child in group.ChildNodes)
                {
                    if (child.NodeType == XmlNodeType.Element)
                    {
                        properties[child.Name] = child.InnerText;
                    }
                }
            }
            
            return properties;
        }

        private string GetProjectSdk(DotnetProjectType projectType)
        {
            return projectType switch
            {
                DotnetProjectType.WebApi => "Microsoft.NET.Sdk.Web",
                DotnetProjectType.Mvc => "Microsoft.NET.Sdk.Web", 
                DotnetProjectType.Blazor => "Microsoft.NET.Sdk.BlazorWebAssembly",
                DotnetProjectType.WorkerService => "Microsoft.NET.Sdk.Worker",
                _ => "Microsoft.NET.Sdk"
            };
        }

        /// <summary>
        /// Check if TuskLang configuration exists and is valid
        /// </summary>
        protected async Task<TSK> LoadTuskLangConfigAsync(string configPath = null)
        {
            var tskPath = configPath ?? DotnetGlobalOptions.ConfigPath ?? "peanu.tsk";
            
            if (!File.Exists(tskPath))
            {
                WriteWarning($"TuskLang configuration not found: {tskPath}");
                return null;
            }

            return await LoadTskFileAsync(tskPath);
        }

        /// <summary>
        /// Generate environment-specific configuration for .NET projects
        /// </summary>
        protected async Task<bool> GenerateEnvironmentConfigAsync(string environment, string outputPath)
        {
            var config = new Dictionary<string, object>
            {
                ["environment"] = new Dictionary<string, object>
                {
                    ["name"] = environment,
                    ["aspNetCoreEnvironment"] = environment,
                    ["dotNetEnvironment"] = environment
                },
                ["logging"] = new Dictionary<string, object>
                {
                    ["logLevel"] = new Dictionary<string, object>
                    {
                        ["default"] = environment == "Production" ? "Warning" : "Information",
                        ["microsoft"] = "Warning",
                        ["microsoftHostingLifetime"] = "Information"
                    }
                }
            };

            var tsk = new TSK(config);
            var content = tsk.ToString();
            
            return await SaveFileAtomicAsync(outputPath, content, $"{environment} configuration");
        }
    }

    #region Data Classes

    /// <summary>
    /// Result of a dotnet CLI command execution
    /// </summary>
    public class DotnetCommandResult
    {
        public bool Success { get; set; }
        public int ExitCode { get; set; }
        public string Output { get; set; } = "";
        public string Error { get; set; } = "";
        public long ExecutionTime { get; set; }
    }

    /// <summary>
    /// Information about a .NET project
    /// </summary>
    public class ProjectInfo
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public string Directory { get; set; }
        public DotnetProjectType ProjectType { get; set; }
        public string TargetFramework { get; set; }
        public Dictionary<string, string> PackageReferences { get; set; } = new Dictionary<string, string>();
        public List<string> ProjectReferences { get; set; } = new List<string>();
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// .NET project types supported by TuskLang
    /// </summary>
    public enum DotnetProjectType
    {
        Console,
        WebApi,
        Mvc,
        Blazor,
        WorkerService,
        ClassLibrary,
        TestProject
    }

    #endregion
} 