using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using TuskLang;

namespace TuskLang.CLI.Commands
{
    /// <summary>
    /// Base class for all tusk commands providing common functionality,
    /// error handling, validation, and performance monitoring
    /// </summary>
    public abstract class CommandBase
    {
        protected readonly Stopwatch _stopwatch;
        protected readonly Dictionary<string, object> _context;

        public CommandBase()
        {
            _stopwatch = new Stopwatch();
            _context = new Dictionary<string, object>();
        }

        /// <summary>
        /// Execute the command with timing and error handling
        /// </summary>
        protected async Task<int> ExecuteWithTimingAsync(Func<Task<int>> commandAction, string commandName)
        {
            _stopwatch.Start();

            try
            {
                WriteProcessing($"Executing {commandName}...");
                var result = await commandAction();
                
                _stopwatch.Stop();
                
                if (result == 0)
                {
                    WriteSuccess($"{commandName} completed successfully ({_stopwatch.ElapsedMilliseconds}ms)");
                }
                else
                {
                    WriteError($"{commandName} failed");
                }

                return result;
            }
            catch (Exception ex)
            {
                _stopwatch.Stop();
                WriteError($"{commandName} failed: {ex.Message}");
                
                if (GlobalOptions.Verbose)
                {
                    WriteError($"Stack trace: {ex.StackTrace}");
                }

                return 1;
            }
        }

        /// <summary>
        /// Validate that a file exists and is accessible
        /// </summary>
        protected bool ValidateFileExists(string filePath, string fileDescription = "file")
        {
            if (string.IsNullOrEmpty(filePath))
            {
                WriteError($"No {fileDescription} specified");
                return false;
            }

            if (!File.Exists(filePath))
            {
                WriteError($"{fileDescription} not found: {filePath}");
                return false;
            }

            try
            {
                using var stream = File.OpenRead(filePath);
                return true;
            }
            catch (Exception ex)
            {
                WriteError($"Cannot access {fileDescription}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Validate that a directory exists and is writable
        /// </summary>
        protected bool ValidateDirectoryWritable(string directoryPath, string directoryDescription = "directory")
        {
            if (string.IsNullOrEmpty(directoryPath))
            {
                WriteError($"No {directoryDescription} specified");
                return false;
            }

            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                // Test write permissions
                var testFile = Path.Combine(directoryPath, $".test_write_{Guid.NewGuid():N}.tmp");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);

                return true;
            }
            catch (Exception ex)
            {
                WriteError($"Cannot write to {directoryDescription}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Load and parse a TSK file with comprehensive error handling
        /// </summary>
        protected async Task<TSK> LoadTskFileAsync(string filePath)
        {
            try
            {
                if (!ValidateFileExists(filePath, "TSK file"))
                    return null;

                WriteInfo($"Loading TSK file: {filePath}");
                
                var content = await File.ReadAllTextAsync(filePath);
                if (string.IsNullOrWhiteSpace(content))
                {
                    WriteWarning("TSK file is empty");
                    return new TSK();
                }

                var fileInfo = new FileInfo(filePath);
                WriteInfo($"File size: {fileInfo.Length:N0} bytes");

                if (fileInfo.Length > 100 * 1024 * 1024) // 100MB limit per requirements
                {
                    WriteWarning("Large file detected - this may take some time");
                }

                var tsk = TSK.FromString(content);
                WriteInfo($"TSK file loaded successfully");
                
                return tsk;
            }
            catch (Exception ex)
            {
                WriteError($"Failed to load TSK file: {ex.Message}");
                
                if (GlobalOptions.Verbose)
                {
                    WriteError($"Stack trace: {ex.StackTrace}");
                }
                
                return null;
            }
        }

        /// <summary>
        /// Save content to file with atomic write operations
        /// </summary>
        protected async Task<bool> SaveFileAtomicAsync(string filePath, string content, string fileDescription = "file")
        {
            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !ValidateDirectoryWritable(directory))
                {
                    return false;
                }

                var tempFile = $"{filePath}.tmp_{Guid.NewGuid():N}";
                
                // Write to temporary file first
                await File.WriteAllTextAsync(tempFile, content);
                
                // Atomic move
                if (File.Exists(filePath))
                {
                    var backupFile = $"{filePath}.backup_{DateTime.UtcNow:yyyyMMddHHmmss}";
                    File.Move(filePath, backupFile);
                    
                    try
                    {
                        File.Move(tempFile, filePath);
                        File.Delete(backupFile); // Clean up backup on success
                    }
                    catch
                    {
                        // Restore backup on failure
                        File.Move(backupFile, filePath);
                        throw;
                    }
                }
                else
                {
                    File.Move(tempFile, filePath);
                }

                WriteSuccess($"{fileDescription} saved: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                WriteError($"Failed to save {fileDescription}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Create progress indicator for long-running operations
        /// </summary>
        protected IDisposable CreateProgressIndicator(string operation)
        {
            if (GlobalOptions.Quiet)
                return new DisposableAction(() => { });

            Console.Write($"⏳ {operation}");
            var timer = new System.Threading.Timer(_ => Console.Write("."), null, 500, 500);
            
            return new DisposableAction(() =>
            {
                timer.Dispose();
                Console.WriteLine(" Done");
            });
        }

        /// <summary>
        /// Validate TSK content with comprehensive checks
        /// </summary>
        protected async Task<TskValidationResult> ValidateTskContentAsync(TSK tsk)
        {
            var result = new TskValidationResult();
            var issues = new List<string>();
            var warnings = new List<string>();

            try
            {
                // Basic structure validation
                var data = tsk.ToDictionary();
                result.SectionCount = data.Count;

                int totalKeys = 0;
                foreach (var section in data)
                {
                    if (section.Value is Dictionary<string, object> sectionData)
                    {
                        totalKeys += sectionData.Count;
                        
                        // Check for common issues
                        foreach (var key in sectionData.Keys)
                        {
                            if (string.IsNullOrWhiteSpace(key))
                            {
                                issues.Add($"Empty key found in section '{section.Key}'");
                            }
                            
                            if (key.Contains(" "))
                            {
                                warnings.Add($"Key '{key}' in section '{section.Key}' contains spaces");
                            }
                        }
                    }
                    else
                    {
                        warnings.Add($"Section '{section.Key}' is not a dictionary");
                    }
                }

                result.KeyCount = totalKeys;
                result.Issues = issues;
                result.Warnings = warnings;
                result.IsValid = issues.Count == 0;

                if (result.IsValid)
                {
                    WriteSuccess($"Validation passed: {result.SectionCount} sections, {result.KeyCount} keys");
                }
                else
                {
                    WriteError($"Validation failed with {issues.Count} issues");
                    foreach (var issue in issues)
                    {
                        WriteError($"  • {issue}");
                    }
                }

                if (warnings.Count > 0)
                {
                    WriteWarning($"Validation warnings ({warnings.Count}):");
                    foreach (var warning in warnings)
                    {
                        WriteWarning($"  • {warning}");
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                WriteError($"Validation error: {ex.Message}");
                result.IsValid = false;
                result.Issues = new List<string> { ex.Message };
                return result;
            }
        }

        /// <summary>
        /// Output results in JSON format if requested
        /// </summary>
        protected void OutputResult(object result)
        {
            if (GlobalOptions.JsonOutput)
            {
                var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
                Console.WriteLine(json);
            }
        }

        /// <summary>
        /// Write processing message
        /// </summary>
        protected void WriteProcessing(string message)
        {
            if (!GlobalOptions.Quiet)
            {
                Console.WriteLine($"🔄 {message}");
            }
        }

        /// <summary>
        /// Write success message
        /// </summary>
        protected void WriteSuccess(string message)
        {
            if (!GlobalOptions.Quiet)
            {
                Console.WriteLine($"✅ {message}");
            }
        }

        /// <summary>
        /// Write error message
        /// </summary>
        protected void WriteError(string message)
        {
            Console.Error.WriteLine($"❌ {message}");
        }

        /// <summary>
        /// Write warning message
        /// </summary>
        protected void WriteWarning(string message)
        {
            if (!GlobalOptions.Quiet)
            {
                Console.WriteLine($"⚠️ {message}");
            }
        }

        /// <summary>
        /// Write info message
        /// </summary>
        protected void WriteInfo(string message)
        {
            if (GlobalOptions.Verbose)
            {
                Console.WriteLine($"📍 {message}");
            }
        }
    }

    /// <summary>
    /// TSK validation result with comprehensive information
    /// </summary>
    public class TskValidationResult
    {
        public bool IsValid { get; set; }
        public int SectionCount { get; set; }
        public int KeyCount { get; set; }
        public List<string> Issues { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public TimeSpan ValidationTime { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Disposable action helper
    /// </summary>
    public class DisposableAction : IDisposable
    {
        private readonly Action _action;
        private bool _disposed = false;

        public DisposableAction(Action action)
        {
            _action = action;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _action?.Invoke();
                _disposed = true;
            }
        }
    }
} 