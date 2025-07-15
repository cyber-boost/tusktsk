using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using TuskLang.CLI.Commands;

namespace TuskLang.CLI
{
    /// <summary>
    /// TuskLang C# CLI - Universal Command Line Interface
    /// Implements all commands from the Universal CLI Command Specification
    /// </summary>
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand("🐘 TuskLang C# CLI - Strong. Secure. Scalable.")
            {
                new Option<bool>(new[] { "--version", "-v" }, "Show version information"),
                new Option<bool>("--verbose", "Enable verbose output"),
                new Option<bool>(new[] { "--quiet", "-q" }, "Suppress non-error output"),
                new Option<string>("--config", "Use alternate config file"),
                new Option<bool>("--json", "Output in JSON format")
            };

            // Add all command categories
            DatabaseCommands.AddCommands(rootCommand);
            DevelopmentCommands.AddCommands(rootCommand);
            TestingCommands.AddCommands(rootCommand);
            ServiceCommands.AddCommands(rootCommand);
            CacheCommands.AddCommands(rootCommand);
            ConfigCommands.AddCommands(rootCommand);
            BinaryCommands.AddCommands(rootCommand);
            AiCommands.AddCommands(rootCommand);
            UtilityCommands.AddCommands(rootCommand);
            PeanutsCommands.AddCommands(rootCommand);
            CssCommands.AddCommands(rootCommand);
            ProjectCommands.AddCommands(rootCommand);
            LicenseCommands.AddCommands(rootCommand);

            // Set up global options
            rootCommand.Handler = CommandHandler.Create<bool, bool, bool, string, bool>(HandleGlobalOptions);

            return await rootCommand.InvokeAsync(args);
        }

        private static async Task<int> HandleGlobalOptions(bool version, bool verbose, bool quiet, string config, bool json)
        {
            if (version)
            {
                Console.WriteLine("TuskLang C# CLI v2.0.0");
                return 0;
            }

            // Set global flags
            GlobalOptions.Verbose = verbose;
            GlobalOptions.Quiet = quiet;
            GlobalOptions.JsonOutput = json;
            GlobalOptions.ConfigPath = config;

            return 0;
        }
    }

    /// <summary>
    /// Global options and utilities for CLI commands
    /// </summary>
    public static class GlobalOptions
    {
        public static bool Verbose { get; set; }
        public static bool Quiet { get; set; }
        public static bool JsonOutput { get; set; }
        public static string ConfigPath { get; set; }

        public static void WriteLine(string message)
        {
            if (!Quiet)
            {
                Console.WriteLine(message);
            }
        }

        public static void WriteError(string message)
        {
            Console.Error.WriteLine($"❌ {message}");
        }

        public static void WriteSuccess(string message)
        {
            if (!Quiet)
            {
                Console.WriteLine($"✅ {message}");
            }
        }

        public static void WriteWarning(string message)
        {
            if (!Quiet)
            {
                Console.WriteLine($"⚠️ {message}");
            }
        }

        public static void WriteInfo(string message)
        {
            if (!Quiet)
            {
                Console.WriteLine($"📍 {message}");
            }
        }

        public static void WriteProcessing(string message)
        {
            if (!Quiet)
            {
                Console.WriteLine($"🔄 {message}");
            }
        }
    }
} 