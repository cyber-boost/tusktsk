using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using System.Linq; // Added for .Select()
using TuskLang;

namespace TuskLang.CLI.Commands
{
    /// <summary>
    /// Utility commands for TuskLang CLI
    /// </summary>
    public static class UtilityCommands
    {
        public static Command CreateUtilityCommand()
        {
            var utilityCommand = new Command("utility", "Utility commands")
            {
                new Option<string>("--file", "File to process") { IsRequired = false },
                new Option<bool>("--benchmark", "Run benchmarks") { IsRequired = false },
                new Option<bool>("--info", "Show system info") { IsRequired = false }
            };

            utilityCommand.SetHandler(async (context) =>
            {
                var file = context.ParseResult.GetValueForOption<string>("--file");
                var benchmark = context.ParseResult.GetValueForOption<bool>("--benchmark");
                var info = context.ParseResult.GetValueForOption<bool>("--info");

                if (benchmark)
                {
                    await RunBenchmarks(file);
                }
                else if (info)
                {
                    ShowSystemInfo();
                }
                else
                {
                    Console.WriteLine("Use --benchmark or --info options");
                }
            });

            return utilityCommand;
        }

        private static async Task RunBenchmarks(string? filePath)
        {
            try
            {
                var config = new PeanutConfig(filePath ?? "peanu.tsk");
                await config.BenchmarkAsync();
                Console.WriteLine("✅ Benchmarks completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error running benchmarks: {ex.Message}");
            }
        }

        private static void ShowSystemInfo()
        {
            Console.WriteLine("System Information:");
            Console.WriteLine($"OS: {Environment.OSVersion}");
            Console.WriteLine($".NET Version: {Environment.Version}");
            Console.WriteLine($"Machine Name: {Environment.MachineName}");
            Console.WriteLine($"Processor Count: {Environment.ProcessorCount}");
            Console.WriteLine($"Working Directory: {Environment.CurrentDirectory}");
        }
    }
} 