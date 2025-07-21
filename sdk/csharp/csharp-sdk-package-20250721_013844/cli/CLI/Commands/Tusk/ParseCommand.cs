using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TuskLang;

namespace TuskLang.CLI.Commands.Tusk
{
    /// <summary>
    /// Parse command implementation - Parse and analyze .tsk files
    /// Provides comprehensive parsing with detailed analysis and validation
    /// </summary>
    public static class ParseCommand
    {
        public static Command CreateParseCommand()
        {
            // Arguments
            var fileArgument = new Argument<string>(
                name: "file",
                description: "Path to the .tsk file to parse");

            // Options
            var outputOption = new Option<string>(
                aliases: new[] { "--output", "-o" },
                description: "Output file for parsed results");

            var formatOption = new Option<string>(
                aliases: new[] { "--format", "-f" },
                getDefaultValue: () => "detailed",
                description: "Output format: detailed, json, summary, tree")
            {
                AllowedValues = { "detailed", "json", "summary", "tree" }
            };

            var validateOption = new Option<bool>(
                aliases: new[] { "--validate" },
                description: "Include validation during parsing");

            var statisticsOption = new Option<bool>(
                aliases: new[] { "--statistics", "-s" },
                description: "Include parsing statistics");

            var commentsOption = new Option<bool>(
                aliases: new[] { "--include-comments" },
                description: "Include comments in the output");

            var metadataOption = new Option<bool>(
                aliases: new[] { "--include-metadata" },
                description: "Include file metadata information");

            // Create command
            var parseCommand = new Command("parse", "Parse and analyze .tsk configuration files")
            {
                fileArgument,
                outputOption,
                formatOption,
                validateOption,
                statisticsOption,
                commentsOption,
                metadataOption
            };

            parseCommand.SetHandler(async (file, output, format, validate, statistics, includeComments, includeMetadata) =>
            {
                var command = new ParseCommandImplementation();
                Environment.ExitCode = await command.ExecuteAsync(
                    file, output, format, validate, statistics, includeComments, includeMetadata);
            }, fileArgument, outputOption, formatOption, validateOption, statisticsOption, commentsOption, metadataOption);

            return parseCommand;
        }
    }

    /// <summary>
    /// Parse command implementation with full parsing and analysis capabilities
    /// </summary>
    public class ParseCommandImplementation : CommandBase
    {
        public async Task<int> ExecuteAsync(
            string file, 
            string output, 
            string format, 
            bool validate, 
            bool statistics, 
            bool includeComments, 
            bool includeMetadata)
        {
            return await ExecuteWithTimingAsync(async () =>
            {
                if (string.IsNullOrEmpty(file))
                {
                    WriteError("No file specified. Use: tusk parse <file>");
                    return 1;
                }

                // Resolve file path
                var filePath = Path.GetFullPath(file);
                if (!ValidateFileExists(filePath, "TSK file"))
                    return 1;

                // Load and parse file
                var tsk = await LoadTskFileAsync(filePath);
                if (tsk == null)
                    return 1;

                // Create parse result
                var parseResult = await CreateParseResultAsync(tsk, filePath, validate, statistics, includeComments, includeMetadata);

                // Output results
                await OutputParseResultAsync(parseResult, output, format);

                return parseResult.HasErrors ? 1 : 0;
            }, "Parse");
        }

        private async Task<ParseResult> CreateParseResultAsync(
            TSK tsk, 
            string filePath, 
            bool validate, 
            bool statistics, 
            bool includeComments, 
            bool includeMetadata)
        {
            var result = new ParseResult
            {
                FilePath = filePath,
                ParsedAt = DateTime.UtcNow,
                Success = true
            };

            try
            {
                // Get basic structure
                var data = tsk.ToDictionary();
                result.Sections = new List<ParsedSection>();

                foreach (var section in data)
                {
                    var parsedSection = new ParsedSection
                    {
                        Name = section.Key,
                        Type = section.Value?.GetType().Name ?? "null",
                        Properties = new List<ParsedProperty>()
                    };

                    if (section.Value is Dictionary<string, object> sectionData)
                    {
                        parsedSection.PropertyCount = sectionData.Count;
                        
                        foreach (var property in sectionData)
                        {
                            var parsedProperty = new ParsedProperty
                            {
                                Key = property.Key,
                                Value = property.Value?.ToString() ?? "null",
                                Type = property.Value?.GetType().Name ?? "null",
                                IsFunction = IsFujsenFunction(property.Value?.ToString()),
                                Size = property.Value?.ToString()?.Length ?? 0
                            };

                            // Analyze complex values
                            if (property.Value is Dictionary<string, object>)
                            {
                                parsedProperty.Type = "Object";
                                parsedProperty.Value = $"[Object with {((Dictionary<string, object>)property.Value).Count} properties]";
                            }
                            else if (property.Value is Array || property.Value is List<object>)
                            {
                                parsedProperty.Type = "Array";
                                parsedProperty.Value = $"[Array with {GetArrayLength(property.Value)} items]";
                            }

                            parsedSection.Properties.Add(parsedProperty);
                        }
                    }
                    else
                    {
                        parsedSection.PropertyCount = 0;
                        parsedSection.Type = "Non-dictionary";
                        parsedSection.Properties.Add(new ParsedProperty
                        {
                            Key = "_value",
                            Value = section.Value?.ToString() ?? "null",
                            Type = section.Value?.GetType().Name ?? "null",
                            Size = section.Value?.ToString()?.Length ?? 0
                        });
                    }

                    result.Sections.Add(parsedSection);
                }

                // Validation if requested
                if (validate)
                {
                    WriteInfo("Performing validation...");
                    var validationResult = await ValidateTskContentAsync(tsk);
                    result.ValidationResult = validationResult;
                    result.HasErrors = !validationResult.IsValid;
                }

                // Statistics if requested
                if (statistics)
                {
                    WriteInfo("Calculating statistics...");
                    result.Statistics = await CalculateStatisticsAsync(tsk, filePath);
                }

                // File metadata if requested
                if (includeMetadata)
                {
                    WriteInfo("Gathering file metadata...");
                    result.FileMetadata = await GatherFileMetadataAsync(filePath);
                }

                // Comments if requested (simplified - would need enhanced parser)
                if (includeComments)
                {
                    WriteInfo("Extracting comments...");
                    result.Comments = ExtractComments(await File.ReadAllTextAsync(filePath));
                }

                WriteSuccess($"Parsed successfully: {result.Sections.Count} sections, {result.Sections.Sum(s => s.PropertyCount)} total properties");

                return result;
            }
            catch (Exception ex)
            {
                WriteError($"Parse failed: {ex.Message}");
                result.Success = false;
                result.HasErrors = true;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        private async Task<ParseStatistics> CalculateStatisticsAsync(TSK tsk, string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            var content = await File.ReadAllTextAsync(filePath);
            var data = tsk.ToDictionary();

            var stats = new ParseStatistics
            {
                FileSizeBytes = fileInfo.Length,
                LineCount = content.Split('\n').Length,
                CharacterCount = content.Length,
                SectionCount = data.Count,
                TotalPropertyCount = 0,
                FunctionCount = 0,
                MaxDepth = 0,
                AveragePropertiesPerSection = 0,
                LargestSectionName = "",
                LargestSectionPropertyCount = 0
            };

            foreach (var section in data)
            {
                if (section.Value is Dictionary<string, object> sectionData)
                {
                    stats.TotalPropertyCount += sectionData.Count;
                    
                    if (sectionData.Count > stats.LargestSectionPropertyCount)
                    {
                        stats.LargestSectionPropertyCount = sectionData.Count;
                        stats.LargestSectionName = section.Key;
                    }

                    // Count functions
                    foreach (var property in sectionData.Values)
                    {
                        if (IsFujsenFunction(property?.ToString()))
                            stats.FunctionCount++;
                    }

                    // Calculate depth (simplified)
                    var depth = CalculateDepth(sectionData);
                    if (depth > stats.MaxDepth)
                        stats.MaxDepth = depth;
                }
                else
                {
                    stats.TotalPropertyCount++;
                }
            }

            stats.AveragePropertiesPerSection = stats.SectionCount > 0 
                ? (double)stats.TotalPropertyCount / stats.SectionCount 
                : 0;

            return stats;
        }

        private async Task<FileMetadata> GatherFileMetadataAsync(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            
            return new FileMetadata
            {
                FullPath = fileInfo.FullName,
                DirectoryPath = fileInfo.DirectoryName,
                FileName = fileInfo.Name,
                Extension = fileInfo.Extension,
                SizeBytes = fileInfo.Length,
                CreatedAt = fileInfo.CreationTimeUtc,
                ModifiedAt = fileInfo.LastWriteTimeUtc,
                AccessedAt = fileInfo.LastAccessTimeUtc,
                IsReadOnly = fileInfo.IsReadOnly,
                Attributes = fileInfo.Attributes.ToString()
            };
        }

        private List<CommentInfo> ExtractComments(string content)
        {
            var comments = new List<CommentInfo>();
            var lines = content.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var commentIndex = line.IndexOf('#');
                
                if (commentIndex >= 0)
                {
                    var comment = line.Substring(commentIndex + 1).Trim();
                    if (!string.IsNullOrEmpty(comment))
                    {
                        comments.Add(new CommentInfo
                        {
                            LineNumber = i + 1,
                            Content = comment,
                            Type = commentIndex == 0 ? "Full Line" : "Inline"
                        });
                    }
                }
            }

            return comments;
        }

        private async Task OutputParseResultAsync(ParseResult result, string outputPath, string format)
        {
            string output;

            switch (format.ToLower())
            {
                case "json":
                    output = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
                    break;
                case "summary":
                    output = CreateSummaryOutput(result);
                    break;
                case "tree":
                    output = CreateTreeOutput(result);
                    break;
                default: // detailed
                    output = CreateDetailedOutput(result);
                    break;
            }

            if (!string.IsNullOrEmpty(outputPath))
            {
                await SaveFileAtomicAsync(outputPath, output, "parse results");
            }
            else if (GlobalOptions.JsonOutput)
            {
                Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
            }
            else
            {
                Console.WriteLine(output);
            }
        }

        private string CreateDetailedOutput(ParseResult result)
        {
            var output = new System.Text.StringBuilder();
            
            output.AppendLine($"📄 Parse Results: {Path.GetFileName(result.FilePath)}");
            output.AppendLine($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            output.AppendLine($"File: {result.FilePath}");
            output.AppendLine($"Parsed: {result.ParsedAt:yyyy-MM-dd HH:mm:ss UTC}");
            output.AppendLine($"Status: {(result.Success ? "✅ Success" : "❌ Failed")}");
            
            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                output.AppendLine($"Error: {result.ErrorMessage}");
            }

            output.AppendLine();
            output.AppendLine($"📊 Structure Overview:");
            output.AppendLine($"  Sections: {result.Sections.Count}");
            output.AppendLine($"  Total Properties: {result.Sections.Sum(s => s.PropertyCount)}");

            if (result.Statistics != null)
            {
                output.AppendLine();
                output.AppendLine("📈 Statistics:");
                output.AppendLine($"  File Size: {result.Statistics.FileSizeBytes:N0} bytes");
                output.AppendLine($"  Lines: {result.Statistics.LineCount:N0}");
                output.AppendLine($"  Characters: {result.Statistics.CharacterCount:N0}");
                output.AppendLine($"  Functions: {result.Statistics.FunctionCount}");
                output.AppendLine($"  Max Depth: {result.Statistics.MaxDepth}");
                output.AppendLine($"  Avg Properties/Section: {result.Statistics.AveragePropertiesPerSection:F1}");
                output.AppendLine($"  Largest Section: {result.Statistics.LargestSectionName} ({result.Statistics.LargestSectionPropertyCount} properties)");
            }

            foreach (var section in result.Sections)
            {
                output.AppendLine();
                output.AppendLine($"[{section.Name}] ({section.Type})");
                output.AppendLine($"  Properties: {section.PropertyCount}");
                
                foreach (var property in section.Properties.Take(10)) // Limit for readability
                {
                    var typeInfo = property.IsFunction ? "🔧 Function" : property.Type;
                    var sizeInfo = property.Size > 100 ? $" ({property.Size} chars)" : "";
                    output.AppendLine($"    {property.Key}: {property.Value.Substring(0, Math.Min(property.Value.Length, 50))}{(property.Value.Length > 50 ? "..." : "")} [{typeInfo}]{sizeInfo}");
                }

                if (section.Properties.Count > 10)
                {
                    output.AppendLine($"    ... and {section.Properties.Count - 10} more properties");
                }
            }

            if (result.ValidationResult != null)
            {
                output.AppendLine();
                output.AppendLine("🔍 Validation Results:");
                output.AppendLine($"  Status: {(result.ValidationResult.IsValid ? "✅ Valid" : "❌ Invalid")}");
                
                if (result.ValidationResult.Issues.Count > 0)
                {
                    output.AppendLine($"  Issues ({result.ValidationResult.Issues.Count}):");
                    foreach (var issue in result.ValidationResult.Issues)
                    {
                        output.AppendLine($"    ❌ {issue}");
                    }
                }

                if (result.ValidationResult.Warnings.Count > 0)
                {
                    output.AppendLine($"  Warnings ({result.ValidationResult.Warnings.Count}):");
                    foreach (var warning in result.ValidationResult.Warnings)
                    {
                        output.AppendLine($"    ⚠️ {warning}");
                    }
                }
            }

            return output.ToString();
        }

        private string CreateSummaryOutput(ParseResult result)
        {
            return $"""
                📄 {Path.GetFileName(result.FilePath)} - Parse Summary
                Status: {(result.Success ? "✅ Success" : "❌ Failed")}
                Sections: {result.Sections.Count} | Properties: {result.Sections.Sum(s => s.PropertyCount)}
                {(result.Statistics != null ? $"Size: {result.Statistics.FileSizeBytes:N0} bytes | Lines: {result.Statistics.LineCount:N0}" : "")}
                {(result.ValidationResult != null ? $"Validation: {(result.ValidationResult.IsValid ? "✅ Valid" : $"❌ {result.ValidationResult.Issues.Count} issues")}" : "")}
                """;
        }

        private string CreateTreeOutput(ParseResult result)
        {
            var output = new System.Text.StringBuilder();
            output.AppendLine($"🌳 {Path.GetFileName(result.FilePath)}");
            
            foreach (var section in result.Sections)
            {
                output.AppendLine($"├── [{section.Name}] ({section.PropertyCount} properties)");
                
                for (int i = 0; i < section.Properties.Count; i++)
                {
                    var property = section.Properties[i];
                    var isLast = i == section.Properties.Count - 1;
                    var prefix = isLast ? "    └── " : "    ├── ";
                    var typeIcon = property.IsFunction ? "🔧" : GetTypeIcon(property.Type);
                    
                    output.AppendLine($"{prefix}{typeIcon} {property.Key}: {property.Type}");
                }
            }

            return output.ToString();
        }

        private string GetTypeIcon(string type)
        {
            return type?.ToLower() switch
            {
                "string" => "📝",
                "int32" or "int64" or "double" or "single" => "🔢",
                "boolean" => "✅",
                "object" => "📦",
                "array" => "📋",
                _ => "📄"
            };
        }

        private bool IsFujsenFunction(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            // Simple heuristic for function detection
            return value.Contains("function") || value.Contains("=>") || value.Contains("return");
        }

        private int GetArrayLength(object value)
        {
            if (value is Array array)
                return array.Length;
            if (value is System.Collections.ICollection collection)
                return collection.Count;
            return 0;
        }

        private int CalculateDepth(Dictionary<string, object> data, int currentDepth = 1)
        {
            int maxDepth = currentDepth;
            
            foreach (var value in data.Values)
            {
                if (value is Dictionary<string, object> nested)
                {
                    int depth = CalculateDepth(nested, currentDepth + 1);
                    if (depth > maxDepth)
                        maxDepth = depth;
                }
            }
            
            return maxDepth;
        }
    }

    #region Result Classes

    public class ParseResult
    {
        public string FilePath { get; set; }
        public DateTime ParsedAt { get; set; }
        public bool Success { get; set; }
        public bool HasErrors { get; set; }
        public string ErrorMessage { get; set; }
        public List<ParsedSection> Sections { get; set; } = new List<ParsedSection>();
        public TskValidationResult ValidationResult { get; set; }
        public ParseStatistics Statistics { get; set; }
        public FileMetadata FileMetadata { get; set; }
        public List<CommentInfo> Comments { get; set; } = new List<CommentInfo>();
    }

    public class ParsedSection
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public int PropertyCount { get; set; }
        public List<ParsedProperty> Properties { get; set; } = new List<ParsedProperty>();
    }

    public class ParsedProperty
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public string Type { get; set; }
        public bool IsFunction { get; set; }
        public int Size { get; set; }
    }

    public class ParseStatistics
    {
        public long FileSizeBytes { get; set; }
        public int LineCount { get; set; }
        public int CharacterCount { get; set; }
        public int SectionCount { get; set; }
        public int TotalPropertyCount { get; set; }
        public int FunctionCount { get; set; }
        public int MaxDepth { get; set; }
        public double AveragePropertiesPerSection { get; set; }
        public string LargestSectionName { get; set; }
        public int LargestSectionPropertyCount { get; set; }
    }

    public class FileMetadata
    {
        public string FullPath { get; set; }
        public string DirectoryPath { get; set; }
        public string FileName { get; set; }
        public string Extension { get; set; }
        public long SizeBytes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
        public DateTime AccessedAt { get; set; }
        public bool IsReadOnly { get; set; }
        public string Attributes { get; set; }
    }

    public class CommentInfo
    {
        public int LineNumber { get; set; }
        public string Content { get; set; }
        public string Type { get; set; }
    }

    #endregion
} 