using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Diagnostics;
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
            var fileOption = new Option<string>("--file", "File to process");
            var benchmarkOption = new Option<bool>("--benchmark", "Run benchmarks");
            var infoOption = new Option<bool>("--info", "Show system info");

            var utilityCommand = new Command("utility", "Utility commands")
            {
                fileOption,
                benchmarkOption,
                infoOption
            };

            utilityCommand.SetHandler(async (file, benchmark, info) =>
            {
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
            }, fileOption, benchmarkOption, infoOption);

            return utilityCommand;
        }

        private static async Task RunBenchmarks(string? filePath)
        {
            try
            {
                var config = new PeanutConfig(filePath ?? "peanu.tsk");
                await BenchmarkConfigAsync(config);
                Console.WriteLine("✅ Benchmarks completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error running benchmarks: {ex.Message}");
            }
        }

        private static async Task BenchmarkConfigAsync(PeanutConfig config)
        {
            var stopwatch = Stopwatch.StartNew();
            
            // Benchmark loading
            stopwatch.Restart();
            var content = await config.LoadAsync();
            var loadTime = stopwatch.ElapsedMilliseconds;
            
            // Benchmark parsing
            stopwatch.Restart();
            var lines = content.Split('\n');
            var sections = lines.Count(l => l.Trim().StartsWith("[") && l.Trim().EndsWith("]"));
            var keys = lines.Count(l => l.Trim().Contains("=") && !l.Trim().StartsWith("#"));
            var parseTime = stopwatch.ElapsedMilliseconds;
            
            // Benchmark saving
            stopwatch.Restart();
            await File.WriteAllTextAsync(config.FilePath + ".benchmark", content);
            var saveTime = stopwatch.ElapsedMilliseconds;
            
            Console.WriteLine("Benchmark Results:");
            Console.WriteLine($"  Load Time: {loadTime}ms");
            Console.WriteLine($"  Parse Time: {parseTime}ms");
            Console.WriteLine($"  Save Time: {saveTime}ms");
            Console.WriteLine($"  Total Time: {loadTime + parseTime + saveTime}ms");
            Console.WriteLine($"  Sections: {sections}");
            Console.WriteLine($"  Keys: {keys}");
            Console.WriteLine($"  Lines: {lines.Length}");
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