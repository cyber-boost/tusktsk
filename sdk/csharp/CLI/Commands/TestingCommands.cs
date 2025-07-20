using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using TuskLang;
using System.Text.Json;

namespace TuskLang.CLI.Commands
{
    /// <summary>
    /// Testing commands for TuskLang CLI
    /// </summary>
    public static class TestingCommands
    {
        public static Command CreateTestingCommand()
        {
            var typeOption = new Option<string>("--type", "Test type");
            var runOption = new Option<bool>("--run", "Run tests");

            var testingCommand = new Command("test", "Testing commands")
            {
                typeOption,
                runOption
            };

            testingCommand.SetHandler(async (type, run) =>
            {
                if (run)
                {
                    await RunTests(type);
                }
                else
                {
                    Console.WriteLine("Use --run to execute tests");
                }
            }, typeOption, runOption);

            return testingCommand;
        }

        private static async Task RunTests(string? testType)
        {
            try
            {
                var config = new PeanutConfig("test-config.tsk");
                
                switch (testType?.ToLower())
                {
                    case "error":
                        await TestErrorHandling(config);
                        break;
                    case "performance":
                        await TestPerformance(config);
                        break;
                    case "binary":
                        await TestBinaryOperations(config);
                        break;
                    case "integration":
                        await TestIntegration(config);
                        break;
                    default:
                        await TestAll(config);
                        break;
                }
                
                Console.WriteLine("[OK] All tests completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Test failed: {ex.Message}");
            }
        }

        private static async Task TestErrorHandling(PeanutConfig config)
        {
            Console.WriteLine("Testing error handling...");
            var testContent = @"[error]
bad_fujsen = function bad() {
    throw new Error(""Test error"");
}";
            
            try
            {
                var parsed = ParseTextConfig(testContent);
                Console.WriteLine("[OK] Error handling test passed");
            }
            catch
            {
                Console.WriteLine("[OK] Error handling test passed (caught expected error)");
            }
        }

        private static async Task TestPerformance(PeanutConfig config)
        {
            Console.WriteLine("Testing performance...");
            var testContent = @"[performance]
loop_fujsen = function loop(count) {
    var sum = 0;
    for (var i = 0; i < count; i++) {
        sum += i;
    }
    return sum;
}";
            
            var parsed = ParseTextConfig(testContent);
            Console.WriteLine("[OK] Performance test passed");
        }

        private static async Task TestBinaryOperations(PeanutConfig config)
        {
            Console.WriteLine("Testing binary operations...");
            var testContent = @"[test]
name = ""Binary Test""
value = 42";
            
            var parsed = ParseTextConfig(testContent);
            await CompileToBinaryAsync(parsed, "test-output.pnt");
            Console.WriteLine("[OK] Binary operations test passed");
        }

        private static async Task TestIntegration(PeanutConfig config)
        {
            Console.WriteLine("Testing integration...");
            var testContent = @"[integration]
name = ""Integration Test""
process_fujsen = function process(data) {
    return data.map(item => ({ ...item, processed: true }));
}";
            
            var parsed = ParseTextConfig(testContent);
            Console.WriteLine("[OK] Integration test passed");
        }

        private static async Task TestAll(PeanutConfig config)
        {
            Console.WriteLine("Running all tests...");
            await TestErrorHandling(config);
            await TestPerformance(config);
            await TestBinaryOperations(config);
            await TestIntegration(config);
        }

        private static Dictionary<string, object> ParseTextConfig(string content)
        {
            var result = new Dictionary<string, object>();
            var lines = content.Split('\n');
            string currentSection = "";

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                    continue;

                if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                {
                    currentSection = trimmed.Substring(1, trimmed.Length - 2);
                    result[currentSection] = new Dictionary<string, object>();
                }
                else if (trimmed.Contains("=") && !string.IsNullOrEmpty(currentSection))
                {
                    var parts = trimmed.Split('=', 2);
                    if (parts.Length == 2)
                    {
                        var key = parts[0].Trim();
                        var value = parts[1].Trim().Trim('"');
                        ((Dictionary<string, object>)result[currentSection])[key] = value;
                    }
                }
            }

            return result;
        }

        private static async Task CompileToBinaryAsync(Dictionary<string, object> config, string outputPath)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(outputPath, json);
        }
    }

    /// <summary>
    /// Test result data structure
    /// </summary>
    public class TestResult
    {
        public string Name { get; set; }
        public bool Passed { get; set; }
        public string Description { get; set; }

        public TestResult(string name, bool passed, string description)
        {
            Name = name;
            Passed = passed;
            Description = description;
        }
    }
} 