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
            var rootCommand = new RootCommand("TuskLang CLI - Configuration Management Tool")
            {
                new Option<string>("--config", "Configuration file path") { IsRequired = false },
                new Option<bool>("--verbose", "Enable verbose output") { IsRequired = false }
            };

            // Add subcommands
            rootCommand.AddCommand(ConfigCommands.CreateConfigCommand());
            rootCommand.AddCommand(UtilityCommands.CreateUtilityCommand());
            rootCommand.AddCommand(TestingCommands.CreateTestingCommand());

            rootCommand.SetHandler(async (context) =>
            {
                var configPath = context.ParseResult.GetValueForOption<string>("--config");
                var verbose = context.ParseResult.GetValueForOption<bool>("--verbose");
                
                if (verbose)
                {
                    Console.WriteLine("Verbose mode enabled");
                }
                
                if (!string.IsNullOrEmpty(configPath))
                {
                    Console.WriteLine($"Using config: {configPath}");
                }
                
                Console.WriteLine("TuskLang CLI - Use --help for available commands");
            });

            return await rootCommand.InvokeAsync(args);
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