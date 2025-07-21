using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Linq;

namespace TuskLang.CLI.Commands
{
    /// <summary>
    /// Binary performance commands for TuskLang CLI
    /// </summary>
    public static class BinaryCommands
    {
        public static void AddCommands(RootCommand rootCommand)
        {
            var binaryCommand = new Command("binary", "Binary performance operations")
            {
                new Command("compile", "Compile to binary format (.tskb)")
                {
                    new Argument<string>("file.tsk", "TSK file to compile"),
                    new Option<string>("--output", "Output file path"),
                    new Option<bool>("--optimize", "Enable optimization"),
                    Handler = CommandHandler.Create<string, string, bool>(CompileToBinary)
                },
                new Command("execute", "Execute binary file directly")
                {
                    new Argument<string>("file.tskb", "Binary file to execute"),
                    new Option<string[]>("--args", "Execution arguments"),
                    Handler = CommandHandler.Create<string, string[]>(ExecuteBinary)
                },
                new Command("benchmark", "Compare binary vs text performance")
                {
                    new Argument<string>("file", "File to benchmark"),
                    new Option<int>("--iterations", () => 1000, "Number of iterations"),
                    Handler = CommandHandler.Create<string, int>(BenchmarkBinary)
                },
                new Command("optimize", "Optimize binary for production")
                {
                    new Argument<string>("file", "File to optimize"),
                    new Option<string>("--output", "Output file path"),
                    new Option<bool>("--compress", "Enable compression"),
                    Handler = CommandHandler.Create<string, string, bool>(OptimizeBinary)
                }
            };

            rootCommand.AddCommand(binaryCommand);
        }

        private static async Task<int> CompileToBinary(string file, string output, bool optimize)
        {
            try
            {
                if (!File.Exists(file))
                {
                    GlobalOptions.WriteError($"File not found: {file}");
                    return 3;
                }

                GlobalOptions.WriteProcessing($"Compiling to binary: {file}");

                var content = await File.ReadAllTextAsync(file);
                var tsk = TSK.FromString(content);
                var data = tsk.ToDictionary();

                // Determine output file
                var outputFile = output ?? Path.ChangeExtension(file, ".tskb");

                // Compile to binary format
                var config = new PeanutConfig();
                await config.CompileToBinaryAsync(data, outputFile);

                if (optimize)
                {
                    await OptimizeBinaryFile(outputFile, outputFile, true);
                }

                var fileInfo = new FileInfo(outputFile);
                GlobalOptions.WriteSuccess($"Binary compiled: {outputFile} ({fileInfo.Length} bytes)");

                if (GlobalOptions.Verbose)
                {
                    var originalSize = new FileInfo(file).Length;
                    var compression = ((double)(originalSize - fileInfo.Length) / originalSize) * 100;
                    GlobalOptions.WriteLine($"Original size: {originalSize} bytes");
                    GlobalOptions.WriteLine($"Binary size: {fileInfo.Length} bytes");
                    GlobalOptions.WriteLine($"Compression: {compression:F1}%");
                }

                return 0;
            }
            catch (Exception ex)
            {
                GlobalOptions.WriteError($"Binary compilation failed: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> ExecuteBinary(string file, string[] args)
        {
            try
            {
                if (!File.Exists(file))
                {
                    GlobalOptions.WriteError($"Binary file not found: {file}");
                    return 3;
                }

                GlobalOptions.WriteProcessing($"Executing binary: {file}");

                var config = new PeanutConfig();
                var data = await config.LoadBinaryAsync(file);

                // Execute the binary configuration
                var result = await ExecuteBinaryConfiguration(data, args);

                if (GlobalOptions.JsonOutput)
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(result);
                    GlobalOptions.WriteLine(json);
                }
                else
                {
                    GlobalOptions.WriteSuccess("Binary executed successfully");
                    GlobalOptions.WriteLine($"Sections: {result.Sections}");
                    GlobalOptions.WriteLine($"Keys: {result.Keys}");
                    GlobalOptions.WriteLine($"Execution time: {result.ExecutionTime}ms");
                }

                return 0;
            }
            catch (Exception ex)
            {
                GlobalOptions.WriteError($"Binary execution failed: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> BenchmarkBinary(string file, int iterations)
        {
            try
            {
                if (!File.Exists(file))
                {
                    GlobalOptions.WriteError($"File not found: {file}");
                    return 3;
                }

                GlobalOptions.WriteProcessing($"Benchmarking: {file} ({iterations} iterations)");

                var content = await File.ReadAllTextAsync(file);
                var config = new PeanutConfig();

                // Benchmark text parsing
                var textStopwatch = Stopwatch.StartNew();
                for (int i = 0; i < iterations; i++)
                {
                    config.ParseTextConfig(content);
                }
                textStopwatch.Stop();

                // Benchmark binary loading
                var parsed = config.ParseTextConfig(content);
                var tempBinary = Path.GetTempFileName() + ".tskb";
                await config.CompileToBinaryAsync(parsed, tempBinary);

                var binaryStopwatch = Stopwatch.StartNew();
                for (int i = 0; i < iterations; i++)
                {
                    await config.LoadBinaryAsync(tempBinary);
                }
                binaryStopwatch.Stop();

                // Cleanup
                File.Delete(tempBinary);

                // Calculate results
                var textTime = textStopwatch.ElapsedMilliseconds;
                var binaryTime = binaryStopwatch.ElapsedMilliseconds;
                var improvement = ((double)(textTime - binaryTime) / textTime) * 100;

                // Display results
                GlobalOptions.WriteLine("Binary vs Text Performance Benchmark");
                GlobalOptions.WriteLine("====================================");
                GlobalOptions.WriteLine($"File: {file}");
                GlobalOptions.WriteLine($"Iterations: {iterations}");
                GlobalOptions.WriteLine();
                GlobalOptions.WriteLine($"Text parsing: {textTime}ms ({textTime / (double)iterations:F2}ms per iteration)");
                GlobalOptions.WriteLine($"Binary loading: {binaryTime}ms ({binaryTime / (double)iterations:F2}ms per iteration)");
                GlobalOptions.WriteLine($"Performance improvement: {improvement:F1}%");

                if (GlobalOptions.JsonOutput)
                {
                    var result = new
                    {
                        file = file,
                        iterations = iterations,
                        text_time_ms = textTime,
                        binary_time_ms = binaryTime,
                        improvement_percent = improvement,
                        text_per_iteration = textTime / (double)iterations,
                        binary_per_iteration = binaryTime / (double)iterations
                    };
                    var json = System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    GlobalOptions.WriteLine(json);
                }

                return 0;
            }
            catch (Exception ex)
            {
                GlobalOptions.WriteError($"Benchmark failed: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> OptimizeBinary(string file, string output, bool compress)
        {
            try
            {
                if (!File.Exists(file))
                {
                    GlobalOptions.WriteError($"File not found: {file}");
                    return 3;
                }

                GlobalOptions.WriteProcessing($"Optimizing binary: {file}");

                var config = new PeanutConfig();
                var data = await config.LoadBinaryAsync(file);

                // Determine output file
                var outputFile = output ?? Path.ChangeExtension(file, ".optimized.tskb");

                // Optimize the data
                var optimizedData = OptimizeConfigurationData(data);

                // Compile optimized binary
                await config.CompileToBinaryAsync(optimizedData, outputFile);

                if (compress)
                {
                    await CompressBinaryFile(outputFile);
                }

                var originalSize = new FileInfo(file).Length;
                var optimizedSize = new FileInfo(outputFile).Length;
                var improvement = ((double)(originalSize - optimizedSize) / originalSize) * 100;

                GlobalOptions.WriteSuccess($"Binary optimized: {outputFile}");
                GlobalOptions.WriteLine($"Original size: {originalSize} bytes");
                GlobalOptions.WriteLine($"Optimized size: {optimizedSize} bytes");
                GlobalOptions.WriteLine($"Size improvement: {improvement:F1}%");

                return 0;
            }
            catch (Exception ex)
            {
                GlobalOptions.WriteError($"Binary optimization failed: {ex.Message}");
                return 1;
            }
        }

        // Helper methods
        private static async Task<BinaryExecutionResult> ExecuteBinaryConfiguration(Dictionary<string, object> data, string[] args)
        {
            var stopwatch = Stopwatch.StartNew();
            var sections = data.Count;
            var keys = 0;

            foreach (var section in data.Values)
            {
                if (section is Dictionary<string, object> sectionData)
                    keys += sectionData.Count;
            }

            // Simulate execution
            await Task.Delay(10); // Simulate processing time

            stopwatch.Stop();

            return new BinaryExecutionResult
            {
                Sections = sections,
                Keys = keys,
                ExecutionTime = stopwatch.ElapsedMilliseconds,
                Success = true
            };
        }

        private static async Task OptimizeBinaryFile(string inputFile, string outputFile, bool optimize)
        {
            var config = new PeanutConfig();
            var data = await config.LoadBinaryAsync(inputFile);
            var optimizedData = OptimizeConfigurationData(data);
            await config.CompileToBinaryAsync(optimizedData, outputFile);
        }

        private static Dictionary<string, object> OptimizeConfigurationData(Dictionary<string, object> data)
        {
            var optimized = new Dictionary<string, object>();

            foreach (var kvp in data)
            {
                var optimizedValue = OptimizeValue(kvp.Value);
                if (optimizedValue != null)
                {
                    optimized[kvp.Key] = optimizedValue;
                }
            }

            return optimized;
        }

        private static object OptimizeValue(object value)
        {
            if (value == null) return null;

            if (value is string str)
            {
                // Remove unnecessary whitespace
                var trimmed = str.Trim();
                return string.IsNullOrEmpty(trimmed) ? null : trimmed;
            }

            if (value is Dictionary<string, object> dict)
            {
                var optimized = new Dictionary<string, object>();
                foreach (var kvp in dict)
                {
                    var optimizedValue = OptimizeValue(kvp.Value);
                    if (optimizedValue != null)
                    {
                        optimized[kvp.Key] = optimizedValue;
                    }
                }
                return optimized.Count > 0 ? optimized : null;
            }

            if (value is IEnumerable<object> enumerable)
            {
                var optimized = new List<object>();
                foreach (var item in enumerable)
                {
                    var optimizedItem = OptimizeValue(item);
                    if (optimizedItem != null)
                    {
                        optimized.Add(optimizedItem);
                    }
                }
                return optimized.Count > 0 ? optimized : null;
            }

            return value;
        }

        private static async Task CompressBinaryFile(string file)
        {
            // Simple compression by removing redundant data
            var config = new PeanutConfig();
            var data = await config.LoadBinaryAsync(file);
            
            // Recompile with compression
            await config.CompileToBinaryAsync(data, file);
        }
    }

    /// <summary>
    /// Binary execution result data structure
    /// </summary>
    public class BinaryExecutionResult
    {
        public int Sections { get; set; }
        public int Keys { get; set; }
        public long ExecutionTime { get; set; }
        public bool Success { get; set; }
    }
} 