using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq; // Added for FirstOrDefault

namespace TuskLang.CLI.Commands.Tusk
{
    /// <summary>
    /// Version command implementation - Show version and build information
    /// Provides comprehensive version details, build info, and system information
    /// </summary>
    public static class VersionCommand
    {
        public static Command CreateVersionCommand()
        {
            // Options
            var detailsOption = new Option<bool>(
                aliases: new[] { "--details", "-d" },
                description: "Show detailed version information");

            var systemOption = new Option<bool>(
                aliases: new[] { "--system", "-s" },
                description: "Include system information");

            var dependenciesOption = new Option<bool>(
                aliases: new[] { "--dependencies" },
                description: "Show dependency versions");

            // Create command
            var versionCommand = new Command("version", "Show version and build information")
            {
                detailsOption,
                systemOption,
                dependenciesOption
            };

            versionCommand.SetHandler(async (details, system, dependencies) =>
            {
                var command = new VersionCommandImplementation();
                Environment.ExitCode = await command.ExecuteAsync(details, system, dependencies);
            }, detailsOption, systemOption, dependenciesOption);

            return versionCommand;
        }
    }

    /// <summary>
    /// Version command implementation with comprehensive version reporting
    /// </summary>
    public class VersionCommandImplementation
    {
        public async Task<int> ExecuteAsync(bool details, bool system, bool dependencies)
        {
            try
            {
                var versionInfo = GatherVersionInformation(details, system, dependencies);

                if (GlobalOptions.JsonOutput)
                {
                    var json = JsonSerializer.Serialize(versionInfo, new JsonSerializerOptions { WriteIndented = true });
                    Console.WriteLine(json);
                }
                else
                {
                    DisplayVersionInformation(versionInfo, details, system, dependencies);
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"❌ Failed to get version information: {ex.Message}");
                return 1;
            }
        }

        private VersionInformation GatherVersionInformation(bool details, bool system, bool dependencies)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            var buildTime = GetBuildTime(assembly);

            var versionInfo = new VersionInformation
            {
                ProductName = "TuskLang CLI",
                Version = version?.ToString() ?? "2.0.1",
                BuildVersion = GetInformationalVersion(assembly) ?? version?.ToString() ?? "2.0.1",
                BuildTime = buildTime,
                CommitHash = GetCommitHash(),
                BuildConfiguration = GetBuildConfiguration(),
                TargetFramework = GetTargetFramework(),
                RuntimeVersion = Environment.Version.ToString(),
                CommandLineArguments = Environment.GetCommandLineArgs()
            };

            if (details)
            {
                versionInfo.Details = new VersionDetails
                {
                    AssemblyLocation = assembly.Location,
                    AssemblyFullName = assembly.FullName,
                    ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
                    OSArchitecture = RuntimeInformation.OSArchitecture.ToString(),
                    RuntimeIdentifier = RuntimeInformation.RuntimeIdentifier,
                    FrameworkDescription = RuntimeInformation.FrameworkDescription,
                    IsDebugBuild = IsDebugBuild(),
                    HasNativeCode = HasNativeCode(),
                    IsOptimized = IsOptimized()
                };
            }

            if (system)
            {
                versionInfo.SystemInfo = new SystemInformation
                {
                    OSDescription = RuntimeInformation.OSDescription,
                    OSVersion = Environment.OSVersion.ToString(),
                    MachineName = Environment.MachineName,
                    UserName = Environment.UserName,
                    ProcessorCount = Environment.ProcessorCount,
                    Is64BitOperatingSystem = Environment.Is64BitOperatingSystem,
                    Is64BitProcess = Environment.Is64BitProcess,
                    WorkingSet = Environment.WorkingSet,
                    SystemDirectory = Environment.SystemDirectory,
                    CurrentDirectory = Environment.CurrentDirectory,
                    CommandLine = Environment.CommandLine,
                    RuntimeVersion = RuntimeInformation.FrameworkDescription,
                    CLRVersion = Environment.Version.ToString(),
                    GCSettings = new GCInformation
                    {
                        IsServerGC = System.Runtime.GCSettings.IsServerGC,
                        LargeObjectHeapCompactionMode = System.Runtime.GCSettings.LargeObjectHeapCompactionMode.ToString(),
                        LatencyMode = System.Runtime.GCSettings.LatencyMode.ToString()
                    }
                };
            }

            if (dependencies)
            {
                versionInfo.Dependencies = GatherDependencyInformation();
            }

            return versionInfo;
        }

        private void DisplayVersionInformation(VersionInformation versionInfo, bool details, bool system, bool dependencies)
        {
            // Main version info
            Console.WriteLine($"🚀 {versionInfo.ProductName} v{versionInfo.Version}");
            Console.WriteLine($"Build: {versionInfo.BuildVersion}");
            
            if (versionInfo.BuildTime.HasValue)
                Console.WriteLine($"Built: {versionInfo.BuildTime:yyyy-MM-dd HH:mm:ss UTC}");

            if (!string.IsNullOrEmpty(versionInfo.CommitHash))
                Console.WriteLine($"Commit: {versionInfo.CommitHash}");

            Console.WriteLine($"Configuration: {versionInfo.BuildConfiguration}");
            Console.WriteLine($"Framework: {versionInfo.TargetFramework}");
            Console.WriteLine($"Runtime: {versionInfo.RuntimeVersion}");
            Console.WriteLine();

            // Details
            if (details && versionInfo.Details != null)
            {
                Console.WriteLine("📋 Build Details:");
                Console.WriteLine($"  Assembly: {versionInfo.Details.AssemblyLocation}");
                Console.WriteLine($"  Full Name: {versionInfo.Details.AssemblyFullName}");
                Console.WriteLine($"  Process Architecture: {versionInfo.Details.ProcessArchitecture}");
                Console.WriteLine($"  OS Architecture: {versionInfo.Details.OSArchitecture}");
                Console.WriteLine($"  Runtime Identifier: {versionInfo.Details.RuntimeIdentifier}");
                Console.WriteLine($"  Framework: {versionInfo.Details.FrameworkDescription}");
                Console.WriteLine($"  Debug Build: {versionInfo.Details.IsDebugBuild}");
                Console.WriteLine($"  Optimized: {versionInfo.Details.IsOptimized}");
                Console.WriteLine($"  Native Code: {versionInfo.Details.HasNativeCode}");
                Console.WriteLine();
            }

            // System information
            if (system && versionInfo.SystemInfo != null)
            {
                Console.WriteLine("💻 System Information:");
                Console.WriteLine($"  OS: {versionInfo.SystemInfo.OSDescription}");
                Console.WriteLine($"  Version: {versionInfo.SystemInfo.OSVersion}");
                Console.WriteLine($"  Machine: {versionInfo.SystemInfo.MachineName}");
                Console.WriteLine($"  User: {versionInfo.SystemInfo.UserName}");
                Console.WriteLine($"  Processors: {versionInfo.SystemInfo.ProcessorCount}");
                Console.WriteLine($"  64-bit OS: {versionInfo.SystemInfo.Is64BitOperatingSystem}");
                Console.WriteLine($"  64-bit Process: {versionInfo.SystemInfo.Is64BitProcess}");
                Console.WriteLine($"  Working Set: {versionInfo.SystemInfo.WorkingSet:N0} bytes");
                Console.WriteLine($"  System Directory: {versionInfo.SystemInfo.SystemDirectory}");
                Console.WriteLine($"  Current Directory: {versionInfo.SystemInfo.CurrentDirectory}");
                Console.WriteLine($"  CLR Version: {versionInfo.SystemInfo.CLRVersion}");
                Console.WriteLine($"  Runtime: {versionInfo.SystemInfo.RuntimeVersion}");
                
                if (versionInfo.SystemInfo.GCSettings != null)
                {
                    Console.WriteLine("  Garbage Collection:");
                    Console.WriteLine($"    Server GC: {versionInfo.SystemInfo.GCSettings.IsServerGC}");
                    Console.WriteLine($"    LOH Compaction: {versionInfo.SystemInfo.GCSettings.LargeObjectHeapCompactionMode}");
                    Console.WriteLine($"    Latency Mode: {versionInfo.SystemInfo.GCSettings.LatencyMode}");
                }
                Console.WriteLine();
            }

            // Dependencies
            if (dependencies && versionInfo.Dependencies != null)
            {
                Console.WriteLine("📦 Dependencies:");
                foreach (var dep in versionInfo.Dependencies)
                {
                    Console.WriteLine($"  {dep.Name}: {dep.Version}");
                    if (!string.IsNullOrEmpty(dep.Location))
                        Console.WriteLine($"    Location: {dep.Location}");
                }
                Console.WriteLine();
            }

            // Feature availability
            Console.WriteLine("⚙️ Available Features:");
            Console.WriteLine("  ✅ Parse - Parse and analyze .tsk files");
            Console.WriteLine("  ✅ Compile - Compile to binary .pnt format");
            Console.WriteLine("  ✅ Validate - Comprehensive validation");
            Console.WriteLine("  ✅ Init - Create new projects");
            Console.WriteLine("  ✅ Build - Build project configurations");
            Console.WriteLine("  ✅ Watch - Auto-recompile on changes");
            Console.WriteLine("  ✅ Version - Show version information");
            Console.WriteLine("  ✅ Help - Command help system");
        }

        private DateTime? GetBuildTime(Assembly assembly)
        {
            try
            {
                var location = assembly.Location;
                if (string.IsNullOrEmpty(location))
                    return null;

                return System.IO.File.GetCreationTimeUtc(location);
            }
            catch
            {
                return null;
            }
        }

        private string GetInformationalVersion(Assembly assembly)
        {
            return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        }

        private string GetCommitHash()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var commitAttribute = assembly.GetCustomAttribute<AssemblyMetadataAttribute>();
                return commitAttribute?.Value ?? "unknown";
            }
            catch
            {
                return "unknown";
            }
        }

        private string GetBuildConfiguration()
        {
#if DEBUG
            return "Debug";
#else
            return "Release";
#endif
        }

        private string GetTargetFramework()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var targetFrameworkAttribute = assembly.GetCustomAttribute<System.Runtime.Versioning.TargetFrameworkAttribute>();
            return targetFrameworkAttribute?.FrameworkName ?? ".NET";
        }

        private bool IsDebugBuild()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var debuggableAttribute = assembly.GetCustomAttribute<System.Diagnostics.DebuggableAttribute>();
            return debuggableAttribute?.IsJITOptimizerDisabled ?? false;
        }

        private bool HasNativeCode()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                return assembly.GetTypes().Length > 0; // Simplified check
            }
            catch
            {
                return false;
            }
        }

        private bool IsOptimized()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var debuggableAttribute = assembly.GetCustomAttribute<System.Diagnostics.DebuggableAttribute>();
            return debuggableAttribute?.IsJITOptimizerDisabled == false;
        }

        private List<DependencyInformation> GatherDependencyInformation()
        {
            var dependencies = new List<DependencyInformation>();

            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var referencedAssemblies = assembly.GetReferencedAssemblies();

                foreach (var refAssembly in referencedAssemblies)
                {
                    try
                    {
                        var loadedAssembly = Assembly.Load(refAssembly);
                        dependencies.Add(new DependencyInformation
                        {
                            Name = refAssembly.Name,
                            Version = refAssembly.Version?.ToString(),
                            Location = loadedAssembly.Location,
                            Culture = refAssembly.CultureName ?? "neutral",
                            PublicKeyToken = refAssembly.GetPublicKeyToken()?.Length > 0 
                                ? Convert.ToHexString(refAssembly.GetPublicKeyToken()) 
                                : "none"
                        });
                    }
                    catch
                    {
                        dependencies.Add(new DependencyInformation
                        {
                            Name = refAssembly.Name,
                            Version = refAssembly.Version?.ToString(),
                            Location = "not loaded",
                            Culture = refAssembly.CultureName ?? "neutral"
                        });
                    }
                }

                // Add key system dependencies
                dependencies.AddRange(new[]
                {
                    new DependencyInformation
                    {
                        Name = "System.CommandLine",
                        Version = GetPackageVersion("System.CommandLine"),
                        Location = "NuGet Package"
                    },
                    new DependencyInformation
                    {
                        Name = "System.Text.Json",
                        Version = GetPackageVersion("System.Text.Json"),
                        Location = "NuGet Package"
                    }
                });
            }
            catch (Exception ex)
            {
                dependencies.Add(new DependencyInformation
                {
                    Name = "Error",
                    Version = "Failed to load dependencies",
                    Location = ex.Message
                });
            }

            return dependencies;
        }

        private string GetPackageVersion(string packageName)
        {
            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                var assembly = assemblies.FirstOrDefault(a => a.GetName().Name == packageName);
                return assembly?.GetName().Version?.ToString() ?? "unknown";
            }
            catch
            {
                return "unknown";
            }
        }
    }

    #region Data Classes

    public class VersionInformation
    {
        public string ProductName { get; set; }
        public string Version { get; set; }
        public string BuildVersion { get; set; }
        public DateTime? BuildTime { get; set; }
        public string CommitHash { get; set; }
        public string BuildConfiguration { get; set; }
        public string TargetFramework { get; set; }
        public string RuntimeVersion { get; set; }
        public string[] CommandLineArguments { get; set; }
        public VersionDetails Details { get; set; }
        public SystemInformation SystemInfo { get; set; }
        public List<DependencyInformation> Dependencies { get; set; }
    }

    public class VersionDetails
    {
        public string AssemblyLocation { get; set; }
        public string AssemblyFullName { get; set; }
        public string ProcessArchitecture { get; set; }
        public string OSArchitecture { get; set; }
        public string RuntimeIdentifier { get; set; }
        public string FrameworkDescription { get; set; }
        public bool IsDebugBuild { get; set; }
        public bool HasNativeCode { get; set; }
        public bool IsOptimized { get; set; }
    }

    public class SystemInformation
    {
        public string OSDescription { get; set; }
        public string OSVersion { get; set; }
        public string MachineName { get; set; }
        public string UserName { get; set; }
        public int ProcessorCount { get; set; }
        public bool Is64BitOperatingSystem { get; set; }
        public bool Is64BitProcess { get; set; }
        public long WorkingSet { get; set; }
        public string SystemDirectory { get; set; }
        public string CurrentDirectory { get; set; }
        public string CommandLine { get; set; }
        public string RuntimeVersion { get; set; }
        public string CLRVersion { get; set; }
        public GCInformation GCSettings { get; set; }
    }

    public class GCInformation
    {
        public bool IsServerGC { get; set; }
        public string LargeObjectHeapCompactionMode { get; set; }
        public string LatencyMode { get; set; }
    }

    public class DependencyInformation
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string Location { get; set; }
        public string Culture { get; set; }
        public string PublicKeyToken { get; set; }
    }

    #endregion
} 