using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TuskLang;

namespace TuskLang.CLI.Commands.Tusk
{
    /// <summary>
    /// Watch command implementation - Watch files for changes and auto-recompile
    /// Provides file system monitoring with fast change detection and automatic rebuild
    /// </summary>
    public static class WatchCommand
    {
        public static Command CreateWatchCommand()
        {
            // Arguments
            var fileArgument = new Argument<string>(
                name: "file",
                getDefaultValue: () => "peanu.tsk",
                description: "Path to the .tsk file to watch");

            // Options
            var outputOption = new Option<string>(
                aliases: new[] { "--output", "-o" },
                getDefaultValue: () => "build",
                description: "Output directory for build artifacts");

            var intervalOption = new Option<int>(
                aliases: new[] { "--interval", "-i" },
                getDefaultValue: () => 500,
                description: "Watch polling interval in milliseconds");

            var compileOption = new Option<bool>(
                aliases: new[] { "--compile" },
                getDefaultValue: () => true,
                description: "Auto-compile on changes");

            var validateOption = new Option<bool>(
                aliases: new[] { "--validate" },
                getDefaultValue: () => true,
                description: "Auto-validate on changes");

            var patternOption = new Option<string>(
                aliases: new[] { "--pattern" },
                getDefaultValue: () => "*.tsk",
                description: "File pattern to watch");

            var recursiveOption = new Option<bool>(
                aliases: new[] { "--recursive", "-r" },
                getDefaultValue: () => true,
                description: "Watch subdirectories recursively");

            var debounceOption = new Option<int>(
                aliases: new[] { "--debounce" },
                getDefaultValue: () => 100,
                description: "Debounce delay in milliseconds");

            // Create command
            var watchCommand = new Command("watch", "Watch files for changes and auto-recompile with fast change detection")
            {
                fileArgument,
                outputOption,
                intervalOption,
                compileOption,
                validateOption,
                patternOption,
                recursiveOption,
                debounceOption
            };

            watchCommand.SetHandler(async (file, output, interval, compile, validate, pattern, recursive, debounce) =>
            {
                var command = new WatchCommandImplementation();
                Environment.ExitCode = await command.ExecuteAsync(
                    file, output, interval, compile, validate, pattern, recursive, debounce);
            }, fileArgument, outputOption, intervalOption, compileOption, validateOption, patternOption, recursiveOption, debounceOption);

            return watchCommand;
        }
    }

    /// <summary>
    /// Watch command implementation with file system monitoring and auto-rebuild capabilities
    /// </summary>
    public class WatchCommandImplementation : CommandBase
    {
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Dictionary<string, DateTime> _lastProcessed;
        private FileSystemWatcher _fileWatcher;
        private Timer _debounceTimer;
        private readonly object _lockObject = new object();

        public WatchCommandImplementation()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _lastProcessed = new Dictionary<string, DateTime>();
        }

        public async Task<int> ExecuteAsync(
            string file,
            string output,
            int interval,
            bool compile,
            bool validate,
            string pattern,
            bool recursive,
            int debounce)
        {
            try
            {
                var filePath = Path.GetFullPath(file);
                if (!ValidateFileExists(filePath, "TSK file"))
                    return 1;

                var watchDirectory = Path.GetDirectoryName(filePath);
                
                WriteProcessing($"🔍 Starting file watcher...");
                WriteInfo($"Watching: {filePath}");
                WriteInfo($"Pattern: {pattern}");
                WriteInfo($"Output: {output}");
                WriteInfo($"Interval: {interval}ms");
                WriteInfo($"Debounce: {debounce}ms");
                WriteInfo($"Recursive: {recursive}");
                WriteInfo($"Auto-compile: {compile}");
                WriteInfo($"Auto-validate: {validate}");
                WriteInfo("");
                WriteInfo("Press Ctrl+C to stop watching...");
                WriteInfo("");

                // Initial build
                WriteProcessing("Performing initial build...");
                await ProcessFileChangeAsync(filePath, output, compile, validate);

                // Setup file system watcher
                SetupFileWatcher(watchDirectory, pattern, recursive, debounce, async () =>
                {
                    await ProcessFileChangeAsync(filePath, output, compile, validate);
                });

                // Setup console cancellation
                Console.CancelKeyPress += (sender, e) =>
                {
                    e.Cancel = true;
                    _cancellationTokenSource.Cancel();
                };

                // Wait for cancellation
                try
                {
                    await Task.Delay(Timeout.Infinite, _cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    WriteInfo("");
                    WriteProcessing("Shutting down file watcher...");
                }

                return 0;
            }
            catch (Exception ex)
            {
                WriteError($"Watch failed: {ex.Message}");
                return 1;
            }
            finally
            {
                Cleanup();
            }
        }

        private void SetupFileWatcher(string directory, string pattern, bool recursive, int debounce, Func<Task> onChangeCallback)
        {
            _fileWatcher = new FileSystemWatcher(directory, pattern)
            {
                IncludeSubdirectories = recursive,
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size
            };

            // Debounce changes to avoid multiple rapid fires
            _fileWatcher.Changed += (sender, e) => DebouncedFileChange(e.FullPath, debounce, onChangeCallback);
            _fileWatcher.Created += (sender, e) => DebouncedFileChange(e.FullPath, debounce, onChangeCallback);
            _fileWatcher.Deleted += (sender, e) => DebouncedFileChange(e.FullPath, debounce, onChangeCallback);
            _fileWatcher.Renamed += (sender, e) => DebouncedFileChange(e.FullPath, debounce, onChangeCallback);

            _fileWatcher.Error += (sender, e) =>
            {
                WriteError($"File watcher error: {e.GetException()?.Message}");
            };

            WriteSuccess("File watcher initialized");
        }

        private void DebouncedFileChange(string filePath, int debounce, Func<Task> onChangeCallback)
        {
            lock (_lockObject)
            {
                // Dispose existing timer
                _debounceTimer?.Dispose();

                // Create new debounce timer
                _debounceTimer = new Timer(async _ =>
                {
                    try
                    {
                        var now = DateTime.UtcNow;
                        var key = filePath.ToLowerInvariant();

                        // Check if we processed this file recently
                        if (_lastProcessed.TryGetValue(key, out var lastProcessed) &&
                            (now - lastProcessed).TotalMilliseconds < debounce * 2)
                        {
                            return; // Skip duplicate change
                        }

                        _lastProcessed[key] = now;
                        
                        WriteInfo($"📝 File changed: {Path.GetFileName(filePath)} at {now:HH:mm:ss.fff}");
                        await onChangeCallback();
                    }
                    catch (Exception ex)
                    {
                        WriteError($"Error processing file change: {ex.Message}");
                    }
                }, null, debounce, Timeout.Infinite);
            }
        }

        private async Task ProcessFileChangeAsync(string filePath, string outputDir, bool compile, bool validate)
        {
            var changeId = Guid.NewGuid().ToString("N")[..8];
            
            try
            {
                WriteProcessing($"🔄 Processing change [{changeId}]...");

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                // Load the file
                var tsk = await LoadTskFileAsync(filePath);
                if (tsk == null)
                {
                    WriteError($"❌ [{changeId}] Failed to load file");
                    return;
                }

                var tasks = new List<Task<(string operation, bool success, string message)>>();

                // Validation task
                if (validate)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            var validationResult = await ValidateTskContentAsync(tsk);
                            if (validationResult.IsValid)
                            {
                                return ("validate", true, $"Validated ({validationResult.SectionCount} sections, {validationResult.KeyCount} keys)");
                            }
                            else
                            {
                                var issues = string.Join(", ", validationResult.Issues.Take(3));
                                if (validationResult.Issues.Count > 3) issues += "...";
                                return ("validate", false, $"Validation failed: {issues}");
                            }
                        }
                        catch (Exception ex)
                        {
                            return ("validate", false, $"Validation error: {ex.Message}");
                        }
                    }));
                }

                // Compilation task
                if (compile)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            var outputPath = Path.Combine(outputDir, Path.ChangeExtension(Path.GetFileName(filePath), ".pnt"));
                            Directory.CreateDirectory(outputDir);

                            // Use the CompileCommand implementation
                            var compileCommand = new CompileCommandImplementation();
                            
                            // Create temporary file for compilation
                            var tempFile = Path.GetTempFileName();
                            await File.WriteAllTextAsync(tempFile, tsk.ToString());

                            try
                            {
                                var result = await compileCommand.ExecuteAsync(
                                    tempFile, outputPath, true, "gzip", false, true, false, true);
                                
                                if (result == 0)
                                {
                                    var fileInfo = new FileInfo(outputPath);
                                    return ("compile", true, $"Compiled to {Path.GetFileName(outputPath)} ({fileInfo.Length:N0} bytes)");
                                }
                                else
                                {
                                    return ("compile", false, "Compilation failed");
                                }
                            }
                            finally
                            {
                                File.Delete(tempFile);
                            }
                        }
                        catch (Exception ex)
                        {
                            return ("compile", false, $"Compilation error: {ex.Message}");
                        }
                    }));
                }

                // Wait for all tasks to complete
                if (tasks.Count > 0)
                {
                    var results = await Task.WhenAll(tasks);
                    
                    stopwatch.Stop();

                    // Report results
                    var successCount = 0;
                    var messages = new List<string>();

                    foreach (var (operation, success, message) in results)
                    {
                        if (success)
                        {
                            successCount++;
                            messages.Add($"✅ {message}");
                        }
                        else
                        {
                            messages.Add($"❌ {message}");
                        }
                    }

                    var status = successCount == results.Length ? "✅" : "⚠️";
                    WriteInfo($"{status} [{changeId}] Completed {successCount}/{results.Length} operations in {stopwatch.ElapsedMilliseconds}ms");
                    
                    foreach (var message in messages)
                    {
                        WriteInfo($"   {message}");
                    }
                }
                else
                {
                    stopwatch.Stop();
                    WriteInfo($"✅ [{changeId}] File processed in {stopwatch.ElapsedMilliseconds}ms (no operations configured)");
                }

                WriteInfo(""); // Empty line for readability
            }
            catch (Exception ex)
            {
                WriteError($"❌ [{changeId}] Processing failed: {ex.Message}");
            }
        }

        private void Cleanup()
        {
            _cancellationTokenSource?.Cancel();
            _fileWatcher?.Dispose();
            _debounceTimer?.Dispose();
            _cancellationTokenSource?.Dispose();
        }
    }
} 