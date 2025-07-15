using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace TuskLang.CLI.Commands
{
    /// <summary>
    /// Testing commands for TuskLang CLI
    /// </summary>
    public static class TestingCommands
    {
        public static void AddCommands(RootCommand rootCommand)
        {
            var testCommand = new Command("test", "Run test suites")
            {
                new Argument<string>("suite", () => "all", "Test suite to run"),
                new Option<bool>("--verbose", "Enable verbose test output"),
                new Option<string>("--output", "Test output file"),
                new Option<bool>("--coverage", "Generate coverage report"),
                Handler = CommandHandler.Create<string, bool, string, bool>(RunTests)
            };

            rootCommand.AddCommand(testCommand);
        }

        public static async Task<int> RunTests(string suite, bool verbose, string output, bool coverage)
        {
            try
            {
                GlobalOptions.WriteProcessing($"Running test suite: {suite}");

                var testResults = new List<TestResult>();
                var stopwatch = Stopwatch.StartNew();

                switch (suite.ToLower())
                {
                    case "all":
                        testResults.AddRange(await RunParserTests());
                        testResults.AddRange(await RunFujsenTests());
                        testResults.AddRange(await RunSdkTests());
                        testResults.AddRange(await RunPerformanceTests());
                        break;

                    case "parser":
                        testResults.AddRange(await RunParserTests());
                        break;

                    case "fujsen":
                        testResults.AddRange(await RunFujsenTests());
                        break;

                    case "sdk":
                        testResults.AddRange(await RunSdkTests());
                        break;

                    case "performance":
                        testResults.AddRange(await RunPerformanceTests());
                        break;

                    default:
                        GlobalOptions.WriteError($"Unknown test suite: {suite}");
                        return 2;
                }

                stopwatch.Stop();

                // Generate test report
                var report = GenerateTestReport(testResults, stopwatch.Elapsed, suite);
                
                if (!string.IsNullOrEmpty(output))
                {
                    await File.WriteAllTextAsync(output, report);
                    GlobalOptions.WriteSuccess($"Test report saved to: {output}");
                }
                else
                {
                    GlobalOptions.WriteLine(report);
                }

                // Return appropriate exit code
                var failedTests = testResults.Count(r => !r.Passed);
                return failedTests > 0 ? 1 : 0;
            }
            catch (Exception ex)
            {
                GlobalOptions.WriteError($"Test execution failed: {ex.Message}");
                return 1;
            }
        }

        private static async Task<List<TestResult>> RunParserTests()
        {
            var results = new List<TestResult>();
            GlobalOptions.WriteProcessing("Running parser tests...");

            // Test basic TSK parsing
            results.Add(await TestBasicParsing());
            results.Add(await TestComplexParsing());
            results.Add(await TestErrorHandling());
            results.Add(await TestPerformanceParsing());

            return results;
        }

        private static async Task<List<TestResult>> RunFujsenTests()
        {
            var results = new List<TestResult>();
            GlobalOptions.WriteProcessing("Running FUJSEN tests...");

            // Test FUJSEN function execution
            results.Add(await TestFujsenBasic());
            results.Add(await TestFujsenComplex());
            results.Add(await TestFujsenErrorHandling());
            results.Add(await TestFujsenPerformance());

            return results;
        }

        private static async Task<List<TestResult>> RunSdkTests()
        {
            var results = new List<TestResult>();
            GlobalOptions.WriteProcessing("Running SDK tests...");

            // Test SDK-specific features
            results.Add(await TestConfigurationLoading());
            results.Add(await TestBinaryCompilation());
            results.Add(await TestShellStorage());
            results.Add(await TestIntegration());

            return results;
        }

        private static async Task<List<TestResult>> RunPerformanceTests()
        {
            var results = new List<TestResult>();
            GlobalOptions.WriteProcessing("Running performance tests...");

            // Test performance benchmarks
            results.Add(await TestParsingPerformance());
            results.Add(await TestFujsenPerformance());
            results.Add(await TestBinaryPerformance());
            results.Add(await TestMemoryUsage());

            return results;
        }

        // Individual test implementations
        private static async Task<TestResult> TestBasicParsing()
        {
            try
            {
                var testContent = @"
[test]
name = ""Basic Test""
value = 42
enabled = true
";

                var tsk = TSK.FromString(testContent);
                var name = tsk.GetValue("test", "name");
                var value = tsk.GetValue("test", "value");
                var enabled = tsk.GetValue("test", "enabled");

                var passed = name?.ToString() == "Basic Test" && 
                           value?.ToString() == "42" && 
                           enabled?.ToString() == "True";

                return new TestResult("Basic Parsing", passed, "Basic TSK parsing functionality");
            }
            catch (Exception ex)
            {
                return new TestResult("Basic Parsing", false, $"Failed: {ex.Message}");
            }
        }

        private static async Task<TestResult> TestComplexParsing()
        {
            try
            {
                var testContent = @"
[complex]
array = [1, 2, 3, ""test""]
object = {""key1"" = ""value1"", ""key2"" = 42}
nested = {""level1"" = {""level2"" = ""deep""}}
";

                var tsk = TSK.FromString(testContent);
                var array = tsk.GetValue("complex", "array");
                var obj = tsk.GetValue("complex", "object");
                var nested = tsk.GetValue("complex", "nested");

                var passed = array != null && obj != null && nested != null;

                return new TestResult("Complex Parsing", passed, "Complex data structure parsing");
            }
            catch (Exception ex)
            {
                return new TestResult("Complex Parsing", false, $"Failed: {ex.Message}");
            }
        }

        private static async Task<TestResult> TestErrorHandling()
        {
            try
            {
                var invalidContent = @"
[test]
name = ""Test""
invalid_syntax = [1, 2, 3,  # Missing closing bracket
";

                TSK.FromString(invalidContent);
                return new TestResult("Error Handling", false, "Should have thrown exception for invalid syntax");
            }
            catch
            {
                return new TestResult("Error Handling", true, "Properly handled invalid syntax");
            }
        }

        private static async Task<TestResult> TestPerformanceParsing()
        {
            try
            {
                var largeContent = GenerateLargeTskContent(1000);
                var stopwatch = Stopwatch.StartNew();
                
                var tsk = TSK.FromString(largeContent);
                stopwatch.Stop();

                var passed = stopwatch.ElapsedMilliseconds < 1000; // Should parse in under 1 second
                return new TestResult("Performance Parsing", passed, 
                    $"Parsed 1000 sections in {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                return new TestResult("Performance Parsing", false, $"Failed: {ex.Message}");
            }
        }

        private static async Task<TestResult> TestFujsenBasic()
        {
            try
            {
                var testContent = @"
[calculator]
add_fujsen = """
function add(a, b) {
    return a + b;
}
"""
";

                var tsk = TSK.FromString(testContent);
                var result = tsk.ExecuteFujsen("calculator", "add", 5, 3);

                var passed = result?.ToString() == "8";
                return new TestResult("FUJSEN Basic", passed, "Basic FUJSEN function execution");
            }
            catch (Exception ex)
            {
                return new TestResult("FUJSEN Basic", false, $"Failed: {ex.Message}");
            }
        }

        private static async Task<TestResult> TestFujsenComplex()
        {
            try
            {
                var testContent = @"
[processor]
transform_fujsen = """
function transform(data) {
    return {
        processed: true,
        count: data.length,
        items: data.map(item => ({ ...item, processed: true }))
    };
}
"""
";

                var tsk = TSK.FromString(testContent);
                var testData = new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object> { ["id"] = 1, ["value"] = "test" }
                };

                var result = tsk.ExecuteFujsen("processor", "transform", testData);

                var passed = result != null;
                return new TestResult("FUJSEN Complex", passed, "Complex FUJSEN function execution");
            }
            catch (Exception ex)
            {
                return new TestResult("FUJSEN Complex", false, $"Failed: {ex.Message}");
            }
        }

        private static async Task<TestResult> TestFujsenErrorHandling()
        {
            try
            {
                var testContent = @"
[error]
bad_fujsen = """
function bad() {
    throw new Error(""Test error"");
}
"""
";

                var tsk = TSK.FromString(testContent);
                tsk.ExecuteFujsen("error", "bad");
                return new TestResult("FUJSEN Error Handling", false, "Should have thrown exception");
            }
            catch
            {
                return new TestResult("FUJSEN Error Handling", true, "Properly handled FUJSEN error");
            }
        }

        private static async Task<TestResult> TestFujsenPerformance()
        {
            try
            {
                var testContent = @"
[performance]
loop_fujsen = """
function loop(count) {
    var sum = 0;
    for (var i = 0; i < count; i++) {
        sum += i;
    }
    return sum;
}
"""
";

                var tsk = TSK.FromString(testContent);
                var stopwatch = Stopwatch.StartNew();
                
                var result = tsk.ExecuteFujsen("performance", "loop", 10000);
                stopwatch.Stop();

                var passed = stopwatch.ElapsedMilliseconds < 1000; // Should execute in under 1 second
                return new TestResult("FUJSEN Performance", passed, 
                    $"Executed loop in {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                return new TestResult("FUJSEN Performance", false, $"Failed: {ex.Message}");
            }
        }

        private static async Task<TestResult> TestConfigurationLoading()
        {
            try
            {
                var config = new PeanutConfig();
                var result = await config.LoadAsync();

                var passed = result != null && result.Count > 0;
                return new TestResult("Configuration Loading", passed, "Configuration loading functionality");
            }
            catch (Exception ex)
            {
                return new TestResult("Configuration Loading", false, $"Failed: {ex.Message}");
            }
        }

        private static async Task<TestResult> TestBinaryCompilation()
        {
            try
            {
                var testContent = @"
[test]
name = ""Binary Test""
value = 42
";

                var config = new PeanutConfig();
                var parsed = config.ParseTextConfig(testContent);
                var tempFile = Path.GetTempFileName() + ".pnt";
                
                await config.CompileToBinaryAsync(parsed, tempFile);
                var loaded = await config.LoadBinaryAsync(tempFile);

                File.Delete(tempFile);

                var passed = loaded.ContainsKey("test") && 
                           loaded["test"] is Dictionary<string, object> testSection &&
                           testSection.ContainsKey("name") && 
                           testSection["name"]?.ToString() == "Binary Test";

                return new TestResult("Binary Compilation", passed, "Binary compilation and loading");
            }
            catch (Exception ex)
            {
                return new TestResult("Binary Compilation", false, $"Failed: {ex.Message}");
            }
        }

        private static async Task<TestResult> TestShellStorage()
        {
            try
            {
                var testData = "Hello, TuskLang!";
                var shellData = ShellStorage.CreateShellData(testData, "test");
                var binary = ShellStorage.Pack(shellData);
                var retrieved = ShellStorage.Unpack(binary);

                var passed = retrieved.Data?.ToString() == testData;
                return new TestResult("Shell Storage", passed, "Binary shell storage functionality");
            }
            catch (Exception ex)
            {
                return new TestResult("Shell Storage", false, $"Failed: {ex.Message}");
            }
        }

        private static async Task<TestResult> TestIntegration()
        {
            try
            {
                // Test integration between components
                var testContent = @"
[integration]
name = ""Integration Test""
process_fujsen = """
function process(data) {
    return data.map(item => ({ ...item, processed: true }));
}
"""
";

                var tsk = TSK.FromString(testContent);
                var config = new PeanutConfig();
                var parsed = config.ParseTextConfig(testContent);

                var passed = tsk.GetValue("integration", "name")?.ToString() == "Integration Test" &&
                           parsed.ContainsKey("integration");

                return new TestResult("Integration", passed, "Component integration testing");
            }
            catch (Exception ex)
            {
                return new TestResult("Integration", false, $"Failed: {ex.Message}");
            }
        }

        private static async Task<TestResult> TestParsingPerformance()
        {
            try
            {
                var largeContent = GenerateLargeTskContent(5000);
                var stopwatch = Stopwatch.StartNew();
                
                var tsk = TSK.FromString(largeContent);
                stopwatch.Stop();

                var passed = stopwatch.ElapsedMilliseconds < 2000; // Should parse in under 2 seconds
                return new TestResult("Parsing Performance", passed, 
                    $"Parsed 5000 sections in {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                return new TestResult("Parsing Performance", false, $"Failed: {ex.Message}");
            }
        }

        private static async Task<TestResult> TestMemoryUsage()
        {
            try
            {
                var initialMemory = GC.GetTotalMemory(true);
                
                for (int i = 0; i < 100; i++)
                {
                    var content = GenerateLargeTskContent(100);
                    var tsk = TSK.FromString(content);
                }

                GC.Collect();
                var finalMemory = GC.GetTotalMemory(true);
                var memoryIncrease = finalMemory - initialMemory;

                var passed = memoryIncrease < 50 * 1024 * 1024; // Less than 50MB increase
                return new TestResult("Memory Usage", passed, 
                    $"Memory increase: {memoryIncrease / 1024 / 1024}MB");
            }
            catch (Exception ex)
            {
                return new TestResult("Memory Usage", false, $"Failed: {ex.Message}");
            }
        }

        private static string GenerateLargeTskContent(int sections)
        {
            var sb = new StringBuilder();
            
            for (int i = 0; i < sections; i++)
            {
                sb.AppendLine($"[section_{i}]");
                sb.AppendLine($"name = \"Section {i}\"");
                sb.AppendLine($"value = {i}");
                sb.AppendLine($"enabled = {(i % 2 == 0).ToString().ToLower()}");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static string GenerateTestReport(List<TestResult> results, TimeSpan duration, string suite)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("🐘 TuskLang C# Test Report");
            sb.AppendLine("==========================");
            sb.AppendLine($"Suite: {suite}");
            sb.AppendLine($"Duration: {duration.TotalMilliseconds:F0}ms");
            sb.AppendLine($"Total Tests: {results.Count}");
            sb.AppendLine($"Passed: {results.Count(r => r.Passed)}");
            sb.AppendLine($"Failed: {results.Count(r => !r.Passed)}");
            sb.AppendLine();

            foreach (var result in results)
            {
                var status = result.Passed ? "✅" : "❌";
                sb.AppendLine($"{status} {result.Name}: {result.Description}");
            }

            return sb.ToString();
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