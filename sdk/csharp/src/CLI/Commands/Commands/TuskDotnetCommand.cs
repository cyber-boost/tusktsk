using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.IO;
using System.Threading.Tasks;
using TuskLang.CLI.Commands.TuskDotnet;
using System.Collections.Generic; // Added for List

namespace TuskLang.CLI.Commands
{
    /// <summary>
    /// Main tusk-dotnet command implementation - .NET specific tooling and integration
    /// Implements all 8 required .NET commands with full functionality
    /// </summary>
    public static class TuskDotnetCommand
    {
        /// <summary>
        /// Create the complete tusk-dotnet command with all subcommands
        /// </summary>
        public static RootCommand CreateTuskDotnetCommand()
        {
            // Global options for all commands
            var verboseOption = new Option<bool>(
                aliases: new[] { "--verbose", "-v" }, 
                description: "Enable verbose output");
            
            var quietOption = new Option<bool>(
                aliases: new[] { "--quiet", "-q" }, 
                description: "Suppress output except errors");
            
            var jsonOption = new Option<bool>(
                aliases: new[] { "--json", "-j" }, 
                description: "Output results in JSON format");

            var configOption = new Option<string>(
                aliases: new[] { "--config", "-c" }, 
                description: "Path to TuskLang configuration file");

            var dotnetVersionOption = new Option<string>(
                aliases: new[] { "--dotnet-version" }, 
                description: "Target .NET version (6.0, 7.0, 8.0, 9.0)");

            // Root command
            var rootCommand = new RootCommand("tusk-dotnet - .NET Integration Tools for TuskLang")
            {
                verboseOption,
                quietOption,
                jsonOption,
                configOption,
                dotnetVersionOption
            };

            // Set global handler for root command
            rootCommand.SetHandler((verbose, quiet, json, config, dotnetVersion) =>
            {
                SetGlobalOptions(verbose, quiet, json, config, dotnetVersion);
                
                if (!quiet)
                {
                    Console.WriteLine("🚀 TuskLang .NET CLI v2.0.1");
                    Console.WriteLine(".NET Integration Tools for Configuration Management");
                    Console.WriteLine();
                    Console.WriteLine("Available commands:");
                    Console.WriteLine("  new        Create .NET projects with TuskLang integration");
                    Console.WriteLine("  add        Add NuGet packages with TuskLang configuration");
                    Console.WriteLine("  build      Build .NET projects with TuskLang configs");
                    Console.WriteLine("  test       Run tests with TuskLang configuration");
                    Console.WriteLine("  publish    Publish with environment-specific configs");
                    Console.WriteLine("  restore    Restore packages and TuskLang dependencies");
                    Console.WriteLine("  run        Run applications with TuskLang runtime config");
                    Console.WriteLine("  config     Manage .NET-specific TuskLang configurations");
                    Console.WriteLine();
                    Console.WriteLine("Use 'tusk-dotnet <command> --help' for command-specific information");
                    Console.WriteLine("Use 'tusk-dotnet --help' for global options");
                }
            }, verboseOption, quietOption, jsonOption, configOption, dotnetVersionOption);

            // Add all subcommands
            rootCommand.AddCommand(NewCommand.CreateNewCommand());
            rootCommand.AddCommand(AddCommand.CreateAddCommand());
            rootCommand.AddCommand(BuildCommand.CreateBuildCommand());
            rootCommand.AddCommand(TestCommand.CreateTestCommand());
            rootCommand.AddCommand(PublishCommand.CreatePublishCommand());
            rootCommand.AddCommand(RestoreCommand.CreateRestoreCommand());
            rootCommand.AddCommand(RunCommand.CreateRunCommand());
            rootCommand.AddCommand(ConfigCommand.CreateConfigCommand());

            return rootCommand;
        }

        /// <summary>
        /// Create command line parser with enhanced configuration for .NET
        /// </summary>
        public static Parser CreateParser()
        {
            var rootCommand = CreateTuskDotnetCommand();
            
            return new CommandLineBuilder(rootCommand)
                .UseDefaults()
                .UseExceptionHandler((exception, context) =>
                {
                    if (DotnetGlobalOptions.Verbose)
                    {
                        Console.Error.WriteLine($"❌ Unhandled exception: {exception}");
                        Console.Error.WriteLine($"Stack trace: {exception.StackTrace}");
                    }
                    else
                    {
                        Console.Error.WriteLine($"❌ Error: {exception.Message}");
                        Console.Error.WriteLine("Use --verbose for more details");
                    }
                    context.ExitCode = 1;
                })
                .Build();
        }

        /// <summary>
        /// Main entry point for tusk-dotnet command
        /// </summary>
        public static async Task<int> ExecuteAsync(string[] args)
        {
            var parser = CreateParser();
            return await parser.InvokeAsync(args);
        }

        /// <summary>
        /// Set global options for all .NET commands
        /// </summary>
        private static void SetGlobalOptions(bool verbose, bool quiet, bool json, string config, string dotnetVersion)
        {
            DotnetGlobalOptions.Verbose = verbose;
            DotnetGlobalOptions.Quiet = quiet;
            DotnetGlobalOptions.JsonOutput = json;
            DotnetGlobalOptions.ConfigPath = config;
            DotnetGlobalOptions.DotnetVersion = dotnetVersion;

            // Validate mutual exclusivity
            if (verbose && quiet)
            {
                Console.Error.WriteLine("⚠️ Warning: --verbose and --quiet are mutually exclusive. Using verbose mode.");
                DotnetGlobalOptions.Quiet = false;
            }

            // Set configuration file if provided
            if (!string.IsNullOrEmpty(config))
            {
                if (!File.Exists(config))
                {
                    Console.Error.WriteLine($"⚠️ Warning: Configuration file not found: {config}");
                }
                else if (verbose)
                {
                    Console.WriteLine($"📍 Using TuskLang configuration file: {config}");
                }
            }

            // Validate .NET version if provided
            if (!string.IsNullOrEmpty(dotnetVersion))
            {
                if (!IsValidDotnetVersion(dotnetVersion))
                {
                    Console.Error.WriteLine($"⚠️ Warning: Invalid .NET version: {dotnetVersion}. Supported: 6.0, 7.0, 8.0, 9.0");
                }
                else if (verbose)
                {
                    Console.WriteLine($"📍 Target .NET version: {dotnetVersion}");
                }
            }
        }

        /// <summary>
        /// Validate if the specified .NET version is supported
        /// </summary>
        private static bool IsValidDotnetVersion(string version)
        {
            var supportedVersions = new[] { "6.0", "7.0", "8.0", "9.0", "6", "7", "8", "9" };
            return Array.Exists(supportedVersions, v => v == version);
        }
    }

    /// <summary>
    /// Enhanced global options for tusk-dotnet command
    /// </summary>
    public static class DotnetGlobalOptions
    {
        public static bool Verbose { get; set; } = false;
        public static bool Quiet { get; set; } = false;
        public static bool JsonOutput { get; set; } = false;
        public static string ConfigPath { get; set; } = "";
        public static string DotnetVersion { get; set; } = "";

        /// <summary>
        /// Write message respecting quiet mode
        /// </summary>
        public static void WriteLine(string message)
        {
            if (!Quiet)
            {
                Console.WriteLine(message);
            }
        }

        /// <summary>
        /// Write error message (always shown)
        /// </summary>
        public static void WriteError(string message)
        {
            Console.Error.WriteLine($"❌ {message}");
        }

        /// <summary>
        /// Write success message
        /// </summary>
        public static void WriteSuccess(string message)
        {
            if (!Quiet)
            {
                Console.WriteLine($"✅ {message}");
            }
        }

        /// <summary>
        /// Write warning message
        /// </summary>
        public static void WriteWarning(string message)
        {
            if (!Quiet)
            {
                Console.WriteLine($"⚠️ {message}");
            }
        }

        /// <summary>
        /// Write info message (verbose only)
        /// </summary>
        public static void WriteInfo(string message)
        {
            if (Verbose)
            {
                Console.WriteLine($"📍 {message}");
            }
        }

        /// <summary>
        /// Write processing message
        /// </summary>
        public static void WriteProcessing(string message)
        {
            if (!Quiet)
            {
                Console.WriteLine($"🔄 {message}");
            }
        }

        /// <summary>
        /// Write .NET specific message
        /// </summary>
        public static void WriteDotnet(string message)
        {
            if (!Quiet)
            {
                Console.WriteLine($"🔷 {message}");
            }
        }

        /// <summary>
        /// Get current working directory or specified project directory
        /// </summary>
        public static string GetWorkingDirectory()
        {
            if (!string.IsNullOrEmpty(ConfigPath))
            {
                return Path.GetDirectoryName(Path.GetFullPath(ConfigPath)) ?? Directory.GetCurrentDirectory();
            }
            
            return Directory.GetCurrentDirectory();
        }

        /// <summary>
        /// Find .NET project files in current directory hierarchy
        /// </summary>
        public static string[] FindProjectFiles(string startDirectory = null)
        {
            var directory = startDirectory ?? GetWorkingDirectory();
            var projectExtensions = new[] { "*.csproj", "*.vbproj", "*.fsproj" };
            var projectFiles = new List<string>();

            foreach (var extension in projectExtensions)
            {
                var files = Directory.GetFiles(directory, extension, SearchOption.TopDirectoryOnly);
                projectFiles.AddRange(files);
            }

            return projectFiles.ToArray();
        }

        /// <summary>
        /// Find solution files in current directory hierarchy
        /// </summary>
        public static string[] FindSolutionFiles(string startDirectory = null)
        {
            var directory = startDirectory ?? GetWorkingDirectory();
            return Directory.GetFiles(directory, "*.sln", SearchOption.TopDirectoryOnly);
        }

        /// <summary>
        /// Get target .NET version or default
        /// </summary>
        public static string GetTargetFramework()
        {
            if (!string.IsNullOrEmpty(DotnetVersion))
            {
                return DotnetVersion switch
                {
                    "6" or "6.0" => "net6.0",
                    "7" or "7.0" => "net7.0", 
                    "8" or "8.0" => "net8.0",
                    "9" or "9.0" => "net9.0",
                    _ => "net8.0" // Default
                };
            }
            
            return "net8.0"; // Default framework
        }

        /// <summary>
        /// Write timing information if verbose
        /// </summary>
        public static void WriteTimer(string operation, long milliseconds)
        {
            if (Verbose)
            {
                Console.WriteLine($"⏱️ {operation}: {milliseconds}ms");
            }
        }

        /// <summary>
        /// Write .NET SDK information if verbose
        /// </summary>
        public static void WriteSdkInfo(string version, string location)
        {
            if (Verbose)
            {
                Console.WriteLine($"🔷 .NET SDK {version} at {location}");
            }
        }
    }
} 