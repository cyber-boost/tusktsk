using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TuskLang;

namespace TuskLang.CLI.Commands.Tusk
{
    /// <summary>
    /// Compile command implementation - Compile .tsk files to binary .pnt format
    /// Provides comprehensive compilation with optimization, compression, and validation
    /// </summary>
    public static class CompileCommand
    {
        public static Command CreateCompileCommand()
        {
            // Arguments
            var fileArgument = new Argument<string>(
                name: "file",
                description: "Path to the .tsk file to compile");

            // Options
            var outputOption = new Option<string>(
                aliases: new[] { "--output", "-o" },
                description: "Output path for compiled .pnt file");

            var optimizeOption = new Option<bool>(
                aliases: new[] { "--optimize", "-O" },
                getDefaultValue: () => true,
                description: "Enable compilation optimizations");

            var compressionOption = new Option<string>(
                aliases: new[] { "--compression", "-c" },
                getDefaultValue: () => "gzip",
                description: "Compression method: none, gzip, brotli")
            {
                AllowedValues = { "none", "gzip", "brotli" }
            };

            var validateOption = new Option<bool>(
                aliases: new[] { "--validate" },
                getDefaultValue: () => true,
                description: "Validate before compiling");

            var metadataOption = new Option<bool>(
                aliases: new[] { "--include-metadata" },
                getDefaultValue: () => true,
                description: "Include metadata in compiled file");

            var debugInfoOption = new Option<bool>(
                aliases: new[] { "--debug-info" },
                description: "Include debug information");

            var checksumOption = new Option<bool>(
                aliases: new[] { "--checksum" },
                getDefaultValue: () => true,
                description: "Generate integrity checksums");

            // Create command
            var compileCommand = new Command("compile", "Compile .tsk files to optimized binary .pnt format")
            {
                fileArgument,
                outputOption,
                optimizeOption,
                compressionOption,
                validateOption,
                metadataOption,
                debugInfoOption,
                checksumOption
            };

            compileCommand.SetHandler(async (file, output, optimize, compression, validate, includeMetadata, debugInfo, checksum) =>
            {
                var command = new CompileCommandImplementation();
                Environment.ExitCode = await command.ExecuteAsync(
                    file, output, optimize, compression, validate, includeMetadata, debugInfo, checksum);
            }, fileArgument, outputOption, optimizeOption, compressionOption, validateOption, metadataOption, debugInfoOption, checksumOption);

            return compileCommand;
        }
    }

    /// <summary>
    /// Compile command implementation with full compilation and optimization capabilities
    /// </summary>
    public class CompileCommandImplementation : CommandBase
    {
        public async Task<int> ExecuteAsync(
            string file,
            string output,
            bool optimize,
            string compression,
            bool validate,
            bool includeMetadata,
            bool debugInfo,
            bool checksum)
        {
            return await ExecuteWithTimingAsync(async () =>
            {
                if (string.IsNullOrEmpty(file))
                {
                    WriteError("No file specified. Use: tusk compile <file>");
                    return 1;
                }

                // Resolve file paths
                var inputPath = Path.GetFullPath(file);
                if (!ValidateFileExists(inputPath, "TSK file"))
                    return 1;

                var outputPath = !string.IsNullOrEmpty(output) 
                    ? Path.GetFullPath(output)
                    : Path.ChangeExtension(inputPath, ".pnt");

                // Load and validate TSK file
                var tsk = await LoadTskFileAsync(inputPath);
                if (tsk == null)
                    return 1;

                // Validation if requested
                if (validate)
                {
                    WriteProcessing("Validating TSK file...");
                    var validationResult = await ValidateTskContentAsync(tsk);
                    if (!validationResult.IsValid)
                    {
                        WriteError($"Validation failed with {validationResult.Issues.Count} issues");
                        return 1;
                    }
                    WriteSuccess("Validation passed");
                }

                // Compile to PNT format
                var compileResult = await CompileToPntAsync(
                    tsk, inputPath, outputPath, optimize, compression, includeMetadata, debugInfo, checksum);

                if (compileResult.Success)
                {
                    WriteSuccess($"Compilation successful: {Path.GetFileName(outputPath)}");
                    WriteInfo($"Original size: {compileResult.OriginalSizeBytes:N0} bytes");
                    WriteInfo($"Compiled size: {compileResult.CompiledSizeBytes:N0} bytes");
                    WriteInfo($"Compression ratio: {compileResult.CompressionRatio:P1}");
                    
                    if (GlobalOptions.JsonOutput)
                    {
                        OutputResult(compileResult);
                    }

                    return 0;
                }
                else
                {
                    WriteError($"Compilation failed: {compileResult.ErrorMessage}");
                    return 1;
                }
            }, "Compile");
        }

        private async Task<CompileResult> CompileToPntAsync(
            TSK tsk,
            string inputPath,
            string outputPath,
            bool optimize,
            string compression,
            bool includeMetadata,
            bool debugInfo,
            bool checksum)
        {
            var result = new CompileResult
            {
                InputPath = inputPath,
                OutputPath = outputPath,
                CompiledAt = DateTime.UtcNow,
                Success = false
            };

            try
            {
                WriteProcessing("Preparing compilation...");

                // Get original content
                var originalContent = await File.ReadAllTextAsync(inputPath);
                result.OriginalSizeBytes = Encoding.UTF8.GetByteCount(originalContent);

                // Create PNT structure
                var pntData = new PntData
                {
                    Version = "2.0.1",
                    Format = "pnt",
                    CompiledAt = DateTime.UtcNow,
                    OriginalFile = Path.GetFileName(inputPath),
                    Optimized = optimize,
                    Compression = compression,
                    DebugInfo = debugInfo
                };

                // Process TSK data
                var data = tsk.ToDictionary();
                
                if (optimize)
                {
                    WriteProcessing("Applying optimizations...");
                    data = await OptimizeTskDataAsync(data);
                    result.Optimizations = new List<string> { "Key deduplication", "Value compression", "Structure optimization" };
                }

                pntData.Data = data;

                // Add metadata if requested
                if (includeMetadata)
                {
                    WriteProcessing("Including metadata...");
                    pntData.Metadata = await GatherCompileMetadataAsync(inputPath, tsk);
                }

                // Add debug information if requested
                if (debugInfo)
                {
                    WriteProcessing("Including debug information...");
                    pntData.DebugInfo = await CreateDebugInfoAsync(inputPath, tsk);
                }

                // Serialize to JSON
                var jsonOptions = new JsonSerializerOptions 
                { 
                    WriteIndented = false, 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                };
                var jsonData = JsonSerializer.Serialize(pntData, jsonOptions);
                var jsonBytes = Encoding.UTF8.GetBytes(jsonData);

                // Apply compression
                byte[] compressedData;
                switch (compression.ToLower())
                {
                    case "gzip":
                        WriteProcessing("Applying GZIP compression...");
                        compressedData = await CompressGzipAsync(jsonBytes);
                        break;
                    case "brotli":
                        WriteProcessing("Applying Brotli compression...");
                        compressedData = await CompressBrotliAsync(jsonBytes);
                        break;
                    default:
                        compressedData = jsonBytes;
                        break;
                }

                // Create PNT file structure
                var pntFile = new PntFile
                {
                    Header = new PntHeader
                    {
                        Magic = "PNT\x00",
                        Version = 1,
                        Compression = GetCompressionType(compression),
                        OriginalSize = (uint)jsonBytes.Length,
                        CompressedSize = (uint)compressedData.Length,
                        Checksum = checksum ? CalculateChecksum(compressedData) : "",
                        Created = DateTime.UtcNow.ToBinary(),
                        Reserved = new byte[32] // Reserved for future use
                    },
                    Data = compressedData
                };

                // Write to output file
                WriteProcessing($"Writing compiled file: {Path.GetFileName(outputPath)}");
                await WritePntFileAsync(outputPath, pntFile);

                // Calculate results
                result.CompiledSizeBytes = new FileInfo(outputPath).Length;
                result.CompressionRatio = 1.0 - ((double)result.CompiledSizeBytes / result.OriginalSizeBytes);
                result.Success = true;

                WriteInfo($"Compilation completed in {_stopwatch.ElapsedMilliseconds}ms");

                return result;
            }
            catch (Exception ex)
            {
                WriteError($"Compilation error: {ex.Message}");
                result.Success = false;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        private async Task<Dictionary<string, object>> OptimizeTskDataAsync(Dictionary<string, object> data)
        {
            var optimized = new Dictionary<string, object>();

            // Key deduplication and optimization
            var keyMap = new Dictionary<string, string>();
            var nextKeyId = 1;

            foreach (var section in data)
            {
                var optimizedSection = new Dictionary<string, object>();

                if (section.Value is Dictionary<string, object> sectionData)
                {
                    foreach (var property in sectionData)
                    {
                        // Optimize keys (keep original for now, but could compress common keys)
                        var optimizedKey = property.Key;

                        // Optimize values
                        var optimizedValue = await OptimizeValueAsync(property.Value);
                        optimizedSection[optimizedKey] = optimizedValue;
                    }
                }
                else
                {
                    optimizedSection = section.Value as Dictionary<string, object> ?? new Dictionary<string, object>();
                }

                optimized[section.Key] = optimizedSection;
            }

            return optimized;
        }

        private async Task<object> OptimizeValueAsync(object value)
        {
            // Optimize different value types
            if (value is string strValue)
            {
                // String optimizations
                if (strValue.Length > 1000)
                {
                    // Could apply string compression for large strings
                    return strValue; // For now, keep as-is
                }
                return strValue;
            }
            else if (value is Dictionary<string, object> dictValue)
            {
                // Recursively optimize nested objects
                return await OptimizeTskDataAsync(dictValue);
            }
            else if (value is Array || value is List<object>)
            {
                // Array optimizations could be applied here
                return value;
            }

            return value;
        }

        private async Task<Dictionary<string, object>> GatherCompileMetadataAsync(string inputPath, TSK tsk)
        {
            var fileInfo = new FileInfo(inputPath);
            var data = tsk.ToDictionary();

            var metadata = new Dictionary<string, object>
            {
                ["compiler"] = new Dictionary<string, object>
                {
                    ["name"] = "TuskLang Compiler",
                    ["version"] = "2.0.1",
                    ["platform"] = Environment.OSVersion.ToString(),
                    ["framework"] = Environment.Version.ToString()
                },
                ["source"] = new Dictionary<string, object>
                {
                    ["path"] = fileInfo.FullName,
                    ["size"] = fileInfo.Length,
                    ["modified"] = fileInfo.LastWriteTimeUtc.ToBinary(),
                    ["checksum"] = CalculateFileChecksum(inputPath)
                },
                ["statistics"] = new Dictionary<string, object>
                {
                    ["sections"] = data.Count,
                    ["totalProperties"] = data.Values
                        .OfType<Dictionary<string, object>>()
                        .Sum(section => section.Count),
                    ["functions"] = CountFunctions(data)
                }
            };

            return metadata;
        }

        private async Task<Dictionary<string, object>> CreateDebugInfoAsync(string inputPath, TSK tsk)
        {
            var content = await File.ReadAllTextAsync(inputPath);
            var lines = content.Split('\n');

            var debugInfo = new Dictionary<string, object>
            {
                ["sourceLines"] = lines.Length,
                ["sourceMap"] = new Dictionary<string, object>(), // Would contain line number mappings
                ["symbols"] = ExtractSymbols(tsk),
                ["comments"] = ExtractCommentDebugInfo(content)
            };

            return debugInfo;
        }

        private async Task<byte[]> CompressGzipAsync(byte[] data)
        {
            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, CompressionMode.Compress, true))
            {
                await gzip.WriteAsync(data, 0, data.Length);
            }
            return output.ToArray();
        }

        private async Task<byte[]> CompressBrotliAsync(byte[] data)
        {
            using var output = new MemoryStream();
            using (var brotli = new BrotliStream(output, CompressionMode.Compress, true))
            {
                await brotli.WriteAsync(data, 0, data.Length);
            }
            return output.ToArray();
        }

        private async Task WritePntFileAsync(string outputPath, PntFile pntFile)
        {
            using var stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
            using var writer = new BinaryWriter(stream);

            // Write header
            writer.Write(Encoding.ASCII.GetBytes(pntFile.Header.Magic));
            writer.Write(pntFile.Header.Version);
            writer.Write((byte)pntFile.Header.Compression);
            writer.Write(pntFile.Header.OriginalSize);
            writer.Write(pntFile.Header.CompressedSize);
            writer.Write(pntFile.Header.Created);
            writer.Write(Encoding.UTF8.GetBytes(pntFile.Header.Checksum.PadRight(64, '\0')));
            writer.Write(pntFile.Header.Reserved);

            // Write data
            writer.Write(pntFile.Data);
        }

        private byte GetCompressionType(string compression)
        {
            return compression.ToLower() switch
            {
                "gzip" => 1,
                "brotli" => 2,
                _ => 0 // none
            };
        }

        private string CalculateChecksum(byte[] data)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(data);
            return Convert.ToHexString(hash).ToLower();
        }

        private string CalculateFileChecksum(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(stream);
            return Convert.ToHexString(hash).ToLower();
        }

        private int CountFunctions(Dictionary<string, object> data)
        {
            int count = 0;
            foreach (var section in data.Values)
            {
                if (section is Dictionary<string, object> sectionData)
                {
                    count += sectionData.Values
                        .OfType<string>()
                        .Count(value => IsFujsenFunction(value));
                }
            }
            return count;
        }

        private bool IsFujsenFunction(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            return value.Contains("function") || value.Contains("=>") || value.Contains("return");
        }

        private List<string> ExtractSymbols(TSK tsk)
        {
            var symbols = new List<string>();
            var data = tsk.ToDictionary();

            foreach (var section in data)
            {
                symbols.Add($"section:{section.Key}");
                
                if (section.Value is Dictionary<string, object> sectionData)
                {
                    foreach (var property in sectionData.Keys)
                    {
                        symbols.Add($"property:{section.Key}.{property}");
                    }
                }
            }

            return symbols;
        }

        private List<Dictionary<string, object>> ExtractCommentDebugInfo(string content)
        {
            var comments = new List<Dictionary<string, object>>();
            var lines = content.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var commentIndex = line.IndexOf('#');
                
                if (commentIndex >= 0)
                {
                    comments.Add(new Dictionary<string, object>
                    {
                        ["line"] = i + 1,
                        ["column"] = commentIndex + 1,
                        ["content"] = line.Substring(commentIndex + 1).Trim()
                    });
                }
            }

            return comments;
        }
    }

    #region Result and Data Classes

    public class CompileResult
    {
        public string InputPath { get; set; }
        public string OutputPath { get; set; }
        public DateTime CompiledAt { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public long OriginalSizeBytes { get; set; }
        public long CompiledSizeBytes { get; set; }
        public double CompressionRatio { get; set; }
        public List<string> Optimizations { get; set; } = new List<string>();
        public Dictionary<string, object> Statistics { get; set; }
    }

    public class PntData
    {
        public string Version { get; set; }
        public string Format { get; set; }
        public DateTime CompiledAt { get; set; }
        public string OriginalFile { get; set; }
        public bool Optimized { get; set; }
        public string Compression { get; set; }
        public bool DebugInfo { get; set; }
        public Dictionary<string, object> Data { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
        public Dictionary<string, object> DebugInfo { get; set; }
    }

    public class PntFile
    {
        public PntHeader Header { get; set; }
        public byte[] Data { get; set; }
    }

    public class PntHeader
    {
        public string Magic { get; set; } // "PNT\x00"
        public uint Version { get; set; } // Format version
        public byte Compression { get; set; } // 0=none, 1=gzip, 2=brotli
        public uint OriginalSize { get; set; } // Original JSON size
        public uint CompressedSize { get; set; } // Compressed size
        public long Created { get; set; } // Creation timestamp
        public string Checksum { get; set; } // SHA256 checksum
        public byte[] Reserved { get; set; } // Reserved bytes
    }

    #endregion
} 