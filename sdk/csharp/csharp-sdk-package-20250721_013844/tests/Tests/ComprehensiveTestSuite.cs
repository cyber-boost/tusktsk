using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Text.Json;
using System.Diagnostics;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TuskLang;
using TuskLang.Tests;

namespace TuskLang.Tests
{
    /// <summary>
    /// Comprehensive Test Suite for TuskTsk C# SDK
    /// CRITICAL REQUIREMENT: 90%+ Code Coverage with Real Tests
    /// 
    /// Coverage Areas:
    /// - TSK Parser and Core Functionality
    /// - Configuration Management
    /// - Error Handling and Edge Cases
    /// - Performance Critical Paths
    /// - Integration Scenarios
    /// - Security Validation
    /// - Concurrent Access Patterns
    /// - Memory Management
    /// 
    /// ZERO TOLERANCE FOR PLACEHOLDER TESTS
    /// </summary>
    [TestClass]
    public class ComprehensiveTestSuite
    {
        private TSK _tsk;
        private string _testConfigContent;
        private string _tempFilePath;
        
        [TestInitialize]
        public void Setup()
        {
            // Real test configuration content
            _testConfigContent = @"
# TuskLang Configuration Test File
[database]
host = ""localhost""
port = 5432
name = ""testdb""
user = ""testuser""
password = ""secretpass""
pool_size = 10
ssl_mode = ""require""

[cache]
provider = ""redis""
host = ""redis.example.com""
port = 6379
ttl = 3600
max_connections = 100

[api]
base_url = ""https://api.example.com""
timeout = 30000
retry_count = 3
rate_limit = 1000

[features]
enable_logging = true
enable_metrics = true
debug_mode = false
experimental_features = [""quantum_processing"", ""ai_integration""]

# Fujsen (function serialization) examples
[functions]
data_transformer = 'x => x.ToString().ToUpper()'
validator = 'input => !string.IsNullOrEmpty(input) && input.Length > 3'
calculator = '(a, b) => a * b + 100'
";
            
            _tempFilePath = Path.GetTempFileName() + ".tsk";
            File.WriteAllText(_tempFilePath, _testConfigContent);
            _tsk = TSK.FromFile(_tempFilePath);
        }
        
        [TestCleanup]
        public void Cleanup()
        {
            if (File.Exists(_tempFilePath))
                File.Delete(_tempFilePath);
        }
        
        #region Core TSK Parser Tests
        
        [TestMethod]
        public void TSK_FromString_ParsesComplexConfiguration()
        {
            // Act
            var tsk = TSK.FromString(_testConfigContent);
            
            // Assert - Database section
            var dbSection = tsk.GetSection("database");
            Assert.IsNotNull(dbSection, "Database section should be parsed");
            Assert.AreEqual("localhost", tsk.GetValue("database", "host"));
            Assert.AreEqual(5432, tsk.GetValue("database", "port"));
            Assert.AreEqual("testdb", tsk.GetValue("database", "name"));
            
            // Assert - Cache section
            var cacheSection = tsk.GetSection("cache");
            Assert.IsNotNull(cacheSection, "Cache section should be parsed");
            Assert.AreEqual("redis", tsk.GetValue("cache", "provider"));
            Assert.AreEqual(6379, tsk.GetValue("cache", "port"));
            
            // Assert - Features array
            var features = tsk.GetValue("features", "experimental_features");
            Assert.IsNotNull(features, "Features array should be parsed");
            Assert.IsInstanceOfType(features, typeof(List<object>), "Features should be a list");
            var featureList = (List<object>)features;
            Assert.IsTrue(featureList.Contains("quantum_processing"));
            Assert.IsTrue(featureList.Contains("ai_integration"));
        }
        
        [TestMethod]
        public void TSK_GetSection_ReturnsCorrectData()
        {
            // Act
            var apiSection = _tsk.GetSection("api");
            
            // Assert
            Assert.IsNotNull(apiSection);
            Assert.AreEqual(4, apiSection.Count);
            Assert.IsTrue(apiSection.ContainsKey("base_url"));
            Assert.IsTrue(apiSection.ContainsKey("timeout"));
            Assert.IsTrue(apiSection.ContainsKey("retry_count"));
            Assert.IsTrue(apiSection.ContainsKey("rate_limit"));
        }
        
        [TestMethod]
        public void TSK_GetValue_HandlesTypedValues()
        {
            // Act & Assert - String values
            Assert.AreEqual("localhost", _tsk.GetValue("database", "host"));
            Assert.AreEqual("require", _tsk.GetValue("database", "ssl_mode"));
            
            // Act & Assert - Integer values
            Assert.AreEqual(5432, _tsk.GetValue("database", "port"));
            Assert.AreEqual(10, _tsk.GetValue("database", "pool_size"));
            Assert.AreEqual(30000, _tsk.GetValue("api", "timeout"));
            
            // Act & Assert - Boolean values
            Assert.AreEqual(true, _tsk.GetValue("features", "enable_logging"));
            Assert.AreEqual(false, _tsk.GetValue("features", "debug_mode"));
        }
        
        [TestMethod]
        public void TSK_SetValue_ModifiesConfiguration()
        {
            // Act
            _tsk.SetValue("database", "host", "newhost.example.com");
            _tsk.SetValue("database", "new_field", "new_value");
            
            // Assert
            Assert.AreEqual("newhost.example.com", _tsk.GetValue("database", "host"));
            Assert.AreEqual("new_value", _tsk.GetValue("database", "new_field"));
        }
        
        [TestMethod]
        public void TSK_SetSection_ReplacesEntireSection()
        {
            // Arrange
            var newSection = new Dictionary<string, object>
            {
                {"provider", "memcached"},
                {"host", "cache.example.com"},
                {"port", 11211}
            };
            
            // Act
            _tsk.SetSection("cache", newSection);
            
            // Assert
            var cacheSection = _tsk.GetSection("cache");
            Assert.AreEqual(3, cacheSection.Count);
            Assert.AreEqual("memcached", cacheSection["provider"]);
            Assert.AreEqual("cache.example.com", cacheSection["host"]);
            Assert.AreEqual(11211, cacheSection["port"]);
        }
        
        #endregion
        
        #region Error Handling and Edge Cases
        
        [TestMethod]
        public void TSK_GetSection_ReturnsNullForNonExistentSection()
        {
            // Act
            var section = _tsk.GetSection("nonexistent");
            
            // Assert
            Assert.IsNull(section);
        }
        
        [TestMethod]
        public void TSK_GetValue_ReturnsNullForNonExistentKey()
        {
            // Act
            var value = _tsk.GetValue("database", "nonexistent_key");
            
            // Assert
            Assert.IsNull(value);
        }
        
        [TestMethod]
        public void TSK_GetValue_ReturnsNullForNonExistentSection()
        {
            // Act
            var value = _tsk.GetValue("nonexistent_section", "key");
            
            // Assert
            Assert.IsNull(value);
        }
        
        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public void TSK_FromFile_ThrowsExceptionForNonExistentFile()
        {
            // Act
            TSK.FromFile("nonexistent_file.tsk");
        }
        
        [TestMethod]
        public void TSK_FromString_HandlesEmptyString()
        {
            // Act
            var tsk = TSK.FromString("");
            
            // Assert
            Assert.IsNotNull(tsk);
            Assert.IsNull(tsk.GetSection("any_section"));
        }
        
        [TestMethod]
        public void TSK_FromString_HandlesOnlyComments()
        {
            // Arrange
            var commentOnlyContent = @"
# This is a comment
# Another comment
";
            
            // Act
            var tsk = TSK.FromString(commentOnlyContent);
            
            // Assert
            Assert.IsNotNull(tsk);
            Assert.IsNull(tsk.GetSection("any_section"));
        }
        
        [TestMethod]
        public void TSK_FromString_HandlesSpecialCharacters()
        {
            // Arrange
            var specialCharContent = @"
[special]
unicode_text = ""Hello 世界 🌍""
escaped_quotes = ""He said \""Hello\""""
newlines = ""Line 1\nLine 2\nLine 3""
tabs = ""Column1\tColumn2\tColumn3""
";
            
            // Act
            var tsk = TSK.FromString(specialCharContent);
            
            // Assert
            Assert.IsNotNull(tsk.GetValue("special", "unicode_text"));
            Assert.IsNotNull(tsk.GetValue("special", "escaped_quotes"));
            Assert.IsNotNull(tsk.GetValue("special", "newlines"));
            Assert.IsNotNull(tsk.GetValue("special", "tabs"));
        }
        
        #endregion
        
        #region Performance Critical Tests
        
        [TestMethod]
        public void TSK_LargeConfiguration_PerformsWithinLimits()
        {
            // Arrange - Create large configuration
            var largeConfigBuilder = new System.Text.StringBuilder();
            largeConfigBuilder.AppendLine("# Large configuration test");
            
            // Add 100 sections with 50 keys each
            for (int section = 0; section < 100; section++)
            {
                largeConfigBuilder.AppendLine($"[section_{section}]");
                for (int key = 0; key < 50; key++)
                {
                    largeConfigBuilder.AppendLine($"key_{key} = \"value_{section}_{key}\"");
                }
                largeConfigBuilder.AppendLine();
            }
            
            var largeConfig = largeConfigBuilder.ToString();
            
            // Act - Measure parsing time
            var stopwatch = Stopwatch.StartNew();
            var tsk = TSK.FromString(largeConfig);
            stopwatch.Stop();
            
            // Assert - Should parse within reasonable time (< 1 second)
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 1000, 
                $"Large configuration parsing took {stopwatch.ElapsedMilliseconds}ms, expected < 1000ms");
            
            // Verify data integrity
            Assert.AreEqual("value_50_25", tsk.GetValue("section_50", "key_25"));
            Assert.AreEqual("value_99_49", tsk.GetValue("section_99", "key_49"));
        }
        
        [TestMethod]
        public void TSK_ConcurrentAccess_ThreadSafe()
        {
            // Arrange
            var tasks = new List<Task<bool>>();
            var random = new Random();
            
            // Act - Create multiple concurrent access tasks
            for (int i = 0; i < 10; i++)
            {
                var taskId = i;
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        // Read operations
                        var value1 = _tsk.GetValue("database", "host");
                        var value2 = _tsk.GetSection("api");
                        
                        // Write operations
                        _tsk.SetValue("concurrent", $"task_{taskId}", $"value_{taskId}");
                        
                        // Verify
                        var verifyValue = _tsk.GetValue("concurrent", $"task_{taskId}");
                        return verifyValue?.ToString() == $"value_{taskId}";
                    }
                    catch
                    {
                        return false;
                    }
                }));
            }
            
            // Wait for all tasks
            Task.WaitAll(tasks.ToArray());
            
            // Assert - All tasks should succeed
            Assert.IsTrue(tasks.All(t => t.Result), "All concurrent operations should succeed");
        }
        
        [TestMethod]
        public void TSK_MemoryUsage_WithinReasonableLimits()
        {
            // Arrange
            var initialMemory = GC.GetTotalMemory(true);
            var tsks = new List<TSK>();
            
            // Act - Create multiple TSK instances
            for (int i = 0; i < 100; i++)
            {
                tsks.Add(TSK.FromString(_testConfigContent));
            }
            
            var finalMemory = GC.GetTotalMemory(true);
            var memoryIncrease = finalMemory - initialMemory;
            
            // Assert - Memory increase should be reasonable (< 50MB for 100 instances)
            Assert.IsTrue(memoryIncrease < 50 * 1024 * 1024, 
                $"Memory increase of {memoryIncrease / 1024 / 1024}MB exceeds 50MB limit");
            
            // Cleanup
            tsks.Clear();
            GC.Collect();
        }
        
        #endregion
        
        #region Integration Tests
        
        [TestMethod]
        public void TSK_RoundTrip_PreservesData()
        {
            // Act - Convert to string and back
            var tskString = _tsk.ToString();
            var roundTripTsk = TSK.FromString(tskString);
            
            // Assert - All original values should be preserved
            Assert.AreEqual(_tsk.GetValue("database", "host"), roundTripTsk.GetValue("database", "host"));
            Assert.AreEqual(_tsk.GetValue("database", "port"), roundTripTsk.GetValue("database", "port"));
            Assert.AreEqual(_tsk.GetValue("cache", "provider"), roundTripTsk.GetValue("cache", "provider"));
            Assert.AreEqual(_tsk.GetValue("api", "timeout"), roundTripTsk.GetValue("api", "timeout"));
            Assert.AreEqual(_tsk.GetValue("features", "debug_mode"), roundTripTsk.GetValue("features", "debug_mode"));
        }
        
        [TestMethod]
        public void TSK_FileOperations_WorkCorrectly()
        {
            // Arrange
            var tempFile = Path.GetTempFileName() + ".tsk";
            var testData = new Dictionary<string, object>
            {
                {"test_key", "test_value"},
                {"number_key", 42}
            };
            
            try
            {
                // Act - Create TSK, modify it, and save
                var tsk = new TSK();
                tsk.SetSection("test_section", testData);
                
                // Save to file (assuming ToString() works)
                File.WriteAllText(tempFile, tsk.ToString());
                
                // Load from file
                var loadedTsk = TSK.FromFile(tempFile);
                
                // Assert
                Assert.AreEqual("test_value", loadedTsk.GetValue("test_section", "test_key"));
                Assert.AreEqual(42, loadedTsk.GetValue("test_section", "number_key"));
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }
        
        #endregion
        
        #region Security and Validation Tests
        
        [TestMethod]
        public void TSK_MaliciousInput_HandledSafely()
        {
            // Arrange - Various potentially malicious inputs
            var maliciousInputs = new[]
            {
                "[section]\nkey = \"../../../etc/passwd\"",
                "[section]\nkey = \"<script>alert('xss')</script>\"",
                "[section]\nkey = \"${env:SECRET_KEY}\"",
                "[section]\nkey = \"" + new string('A', 10000) + "\"", // Very long string
                "[" + new string('X', 1000) + "]\nkey = \"value\"", // Very long section name
            };
            
            // Act & Assert - Should handle all inputs without throwing
            foreach (var input in maliciousInputs)
            {
                try
                {
                    var tsk = TSK.FromString(input);
                    Assert.IsNotNull(tsk, $"TSK should handle input: {input.Substring(0, Math.Min(50, input.Length))}...");
                }
                catch (Exception ex)
                {
                    // If an exception is thrown, it should be a controlled parsing exception, not a security vulnerability
                    Assert.IsTrue(ex is ArgumentException || ex is FormatException || ex is InvalidOperationException,
                        $"Unexpected exception type for malicious input: {ex.GetType().Name}");
                }
            }
        }
        
        [TestMethod]
        public void TSK_InputValidation_RejectsNullInputs()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => TSK.FromString(null));
        }
        
        #endregion
    }
    
    /// <summary>
    /// Advanced Integration Test Suite
    /// Tests real-world scenarios and complex integrations
    /// </summary>
    [TestClass]
    public class AdvancedIntegrationTestSuite
    {
        [TestMethod]
        public async Task TSK_AsyncOperations_WorkCorrectly()
        {
            // Arrange
            var configContent = @"
[async_test]
operation_timeout = 5000
batch_size = 100
retry_count = 3
";
            
            // Act - Simulate async configuration loading and processing
            var tasks = new List<Task<TSK>>();
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(Task.Run(() => TSK.FromString(configContent)));
            }
            
            var results = await Task.WhenAll(tasks);
            
            // Assert - All async operations should succeed
            Assert.AreEqual(10, results.Length);
            foreach (var result in results)
            {
                Assert.IsNotNull(result);
                Assert.AreEqual(5000, result.GetValue("async_test", "operation_timeout"));
                Assert.AreEqual(100, result.GetValue("async_test", "batch_size"));
                Assert.AreEqual(3, result.GetValue("async_test", "retry_count"));
            }
        }
        
        [TestMethod]
        public void TSK_ConfigurationMerging_WorksCorrectly()
        {
            // Arrange
            var baseConfig = @"
[database]
host = ""localhost""
port = 5432
ssl = false

[cache]
ttl = 3600
";
            
            var overrideConfig = @"
[database]
host = ""production.db.com""
ssl = true
pool_size = 20

[api]
endpoint = ""https://api.prod.com""
";
            
            // Act
            var baseTsk = TSK.FromString(baseConfig);
            var overrideTsk = TSK.FromString(overrideConfig);
            
            // Simulate configuration merging
            foreach (var section in new[] {"database", "api"})
            {
                var overrideSection = overrideTsk.GetSection(section);
                if (overrideSection != null)
                {
                    var baseSection = baseTsk.GetSection(section) ?? new Dictionary<string, object>();
                    foreach (var kvp in overrideSection)
                    {
                        baseSection[kvp.Key] = kvp.Value;
                    }
                    baseTsk.SetSection(section, baseSection);
                }
            }
            
            // Assert - Merged configuration should have combined values
            Assert.AreEqual("production.db.com", baseTsk.GetValue("database", "host")); // Overridden
            Assert.AreEqual(5432, baseTsk.GetValue("database", "port")); // Preserved
            Assert.AreEqual(true, baseTsk.GetValue("database", "ssl")); // Overridden
            Assert.AreEqual(20, baseTsk.GetValue("database", "pool_size")); // Added
            Assert.AreEqual(3600, baseTsk.GetValue("cache", "ttl")); // Preserved
            Assert.AreEqual("https://api.prod.com", baseTsk.GetValue("api", "endpoint")); // Added
        }
    }
} 