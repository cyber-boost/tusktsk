using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.IO;
using System.Threading.Tasks;
using TuskLang.CLI.Commands.Tusk;

namespace TuskLang.CLI.Commands
{
    /// <summary>
    /// Main tusk command implementation - Complete CLI tool for TuskLang
    /// Implements all 8 required commands with full functionality
    /// </summary>
    public static class TuskCommand
    {
        /// <summary>
        /// Create the complete tusk command with all subcommands
        /// </summary>
        public static RootCommand CreateTuskCommand()
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
                description: "Path to configuration file");

            // Root command
            var rootCommand = new RootCommand("tusk - TuskLang Configuration Management Tool")
            {
                verboseOption,
                quietOption,
                jsonOption,
                configOption
            };

            // Set global handler for root command
            rootCommand.SetHandler((verbose, quiet, json, config) =>
            {
                SetGlobalOptions(verbose, quiet, json, config);
                
                if (!quiet)
                {
                    Console.WriteLine("🚀 TuskLang CLI v2.0.1");
                    Console.WriteLine("Configuration Management Tool for Modern Applications");
                    Console.WriteLine();
                    Console.WriteLine("Available commands:");
                    Console.WriteLine("  parse      Parse and analyze .tsk files");
                    Console.WriteLine("  compile    Compile .tsk to binary .pnt format");
                    Console.WriteLine("  validate   Comprehensive validation with detailed reports");
                    Console.WriteLine("  init       Create new TuskLang project");
                    Console.WriteLine("  build      Build current project");
                    Console.WriteLine("  watch      Watch files for changes and auto-recompile");
                    Console.WriteLine("  version    Show version and build information");
                    Console.WriteLine("  help       Show help for commands");
                    Console.WriteLine();
                    Console.WriteLine("Use 'tusk <command> --help' for command-specific information");
                    Console.WriteLine("Use 'tusk --help' for global options");
                }
            }, verboseOption, quietOption, jsonOption, configOption);

            // Add all subcommands
            rootCommand.AddCommand(ParseCommand.CreateParseCommand());
            rootCommand.AddCommand(CompileCommand.CreateCompileCommand());
            rootCommand.AddCommand(ValidateCommand.CreateValidateCommand());
            rootCommand.AddCommand(InitCommand.CreateInitCommand());
            rootCommand.AddCommand(BuildCommand.CreateBuildCommand());
            rootCommand.AddCommand(WatchCommand.CreateWatchCommand());
            rootCommand.AddCommand(VersionCommand.CreateVersionCommand());
            rootCommand.AddCommand(HelpCommand.CreateHelpCommand());

            return rootCommand;
        }

        /// <summary>
        /// Create command line parser with enhanced configuration
        /// </summary>
        public static Parser CreateParser()
        {
            var rootCommand = CreateTuskCommand();
            
            return new CommandLineBuilder(rootCommand)
                .UseDefaults()
                .UseExceptionHandler((exception, context) =>
                {
                    if (GlobalOptions.Verbose)
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
        /// Main entry point for tusk command
        /// </summary>
        public static async Task<int> ExecuteAsync(string[] args)
        {
            var parser = CreateParser();
            return await parser.InvokeAsync(args);
        }

        /// <summary>
        /// Set global options for all commands
        /// </summary>
        private static void SetGlobalOptions(bool verbose, bool quiet, bool json, string config)
        {
            GlobalOptions.Verbose = verbose;
            GlobalOptions.Quiet = quiet;
            GlobalOptions.JsonOutput = json;
            GlobalOptions.ConfigPath = config;

            // Validate mutual exclusivity
            if (verbose && quiet)
            {
                Console.Error.WriteLine("⚠️ Warning: --verbose and --quiet are mutually exclusive. Using verbose mode.");
                GlobalOptions.Quiet = false;
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
                    Console.WriteLine($"📍 Using configuration file: {config}");
                }
            }
        }
    }

    /// <summary>
    /// Enhanced global options for tusk command
    /// </summary>
    public static class GlobalOptions
    {
        public static bool Verbose { get; set; } = false;
        public static bool Quiet { get; set; } = false;
        public static bool JsonOutput { get; set; } = false;
        public static string ConfigPath { get; set; } = "";

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
        /// Get current working directory or specified config directory
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
        /// Find configuration file in current directory hierarchy
        /// </summary>
        public static string FindConfigurationFile(string preferredName = "peanu.tsk")
        {
            if (!string.IsNullOrEmpty(ConfigPath) && File.Exists(ConfigPath))
            {
                return ConfigPath;
            }

            var directory = new DirectoryInfo(GetWorkingDirectory());
            
            while (directory != null)
            {
                var configFile = Path.Combine(directory.FullName, preferredName);
                if (File.Exists(configFile))
                {
                    return configFile;
                }

                // Also check for .tsk files
                var tskFiles = directory.GetFiles("*.tsk");
                if (tskFiles.Length == 1)
                {
                    return tskFiles[0].FullName;
                }

                directory = directory.Parent;
            }

            return null;
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
        /// Write file size information if verbose
        /// </summary>
        public static void WriteFileInfo(string filePath)
        {
            if (Verbose && File.Exists(filePath))
            {
                var fileInfo = new FileInfo(filePath);
                Console.WriteLine($"📁 {Path.GetFileName(filePath)}: {fileInfo.Length:N0} bytes, Modified: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
            }
        }
    }
} 