using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace TuskLang.CLI.Commands
{
    /// <summary>
    /// Configuration commands for TuskLang CLI
    /// </summary>
    public static class ConfigCommands
    {
        public static void AddCommands(RootCommand rootCommand)
        {
            var configCommand = new Command("config", "Configuration operations")
            {
                new Command("get", "Get configuration value by path")
                {
                    new Argument<string>("key.path", "Configuration key path"),
                    new Argument<string>("dir", () => ".", "Directory to search"),
                    Handler = CommandHandler.Create<string, string>(GetConfigValue)
                },
                new Command("check", "Check configuration hierarchy")
                {
                    new Argument<string>("path", () => ".", "Path to check"),
                    Handler = CommandHandler.Create<string>(CheckConfigHierarchy)
                },
                new Command("validate", "Validate entire configuration chain")
                {
                    new Argument<string>("path", () => ".", "Path to validate"),
                    Handler = CommandHandler.Create<string>(ValidateConfig)
                },
                new Command("compile", "Auto-compile all peanu.tsk files")
                {
                    new Argument<string>("path", () => ".", "Path to compile"),
                    Handler = CommandHandler.Create<string>(CompileConfigs)
                },
                new Command("docs", "Generate configuration documentation")
                {
                    new Argument<string>("path", () => ".", "Path to document"),
                    Handler = CommandHandler.Create<string>(GenerateConfigDocs)
                },
                new Command("clear-cache", "Clear configuration cache")
                {
                    new Argument<string>("path", () => ".", "Path to clear cache for"),
                    Handler = CommandHandler.Create<string>(ClearConfigCache)
                },
                new Command("stats", "Show configuration performance statistics")
                {
                    Handler = CommandHandler.Create(ShowConfigStats)
                }
            };

            rootCommand.AddCommand(configCommand);
        }

        private static async Task<int> GetConfigValue(string keyPath, string dir)
        {
            try
            {
                GlobalOptions.WriteProcessing($"Getting config value: {keyPath}");

                var config = new PeanutConfig();
                var result = await config.LoadAsync(dir);

                var value = GetValueByPath(result, keyPath);
                
                if (value != null)
                {
                    if (GlobalOptions.JsonOutput)
                    {
                        var json = JsonSerializer.Serialize(new { key = keyPath, value = value });
                        GlobalOptions.WriteLine(json);
                    }
                    else
                    {
                        GlobalOptions.WriteLine(value.ToString());
                    }
                    return 0;
                }
                else
                {
                    GlobalOptions.WriteError($"Configuration key not found: {keyPath}");
                    return 1;
                }
            }
            catch (Exception ex)
            {
                GlobalOptions.WriteError($"Failed to get config value: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> CheckConfigHierarchy(string path)
        {
            try
            {
                GlobalOptions.WriteProcessing($"Checking configuration hierarchy: {path}");

                var config = new PeanutConfig();
                var hierarchy = await config.FindConfigHierarchyAsync(path);

                GlobalOptions.WriteLine("Configuration Hierarchy:");
                GlobalOptions.WriteLine("=======================");

                for (int i = 0; i < hierarchy.Count; i++)
                {
                    var configFile = hierarchy[i];
                    var prefix = i == hierarchy.Count - 1 ? "└── " : "├── ";
                    var status = File.Exists(configFile.Path) ? "✅" : "❌";
                    
                    GlobalOptions.WriteLine($"{prefix}{status} {configFile.Path} ({configFile.Type})");
                    
                    if (File.Exists(configFile.Path))
                    {
                        var fileInfo = new FileInfo(configFile.Path);
                        GlobalOptions.WriteLine($"    Size: {fileInfo.Length} bytes");
                        GlobalOptions.WriteLine($"    Modified: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                GlobalOptions.WriteError($"Failed to check config hierarchy: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> ValidateConfig(string path)
        {
            try
            {
                GlobalOptions.WriteProcessing($"Validating configuration: {path}");

                var config = new PeanutConfig();
                var hierarchy = await config.FindConfigHierarchyAsync(path);
                var validationResults = new List<ValidationResult>();

                foreach (var configFile in hierarchy)
                {
                    if (File.Exists(configFile.Path))
                    {
                        var result = await ValidateConfigFile(configFile);
                        validationResults.Add(result);
                    }
                }

                // Display validation results
                GlobalOptions.WriteLine("Configuration Validation Results:");
                GlobalOptions.WriteLine("=================================");

                var allValid = true;
                foreach (var result in validationResults)
                {
                    var status = result.IsValid ? "✅" : "❌";
                    GlobalOptions.WriteLine($"{status} {result.FilePath}");
                    
                    if (!result.IsValid)
                    {
                        GlobalOptions.WriteError($"  Error: {result.ErrorMessage}");
                        allValid = false;
                    }
                    else
                    {
                        GlobalOptions.WriteLine($"  Sections: {result.SectionCount}");
                        GlobalOptions.WriteLine($"  Keys: {result.KeyCount}");
                    }
                }

                return allValid ? 0 : 1;
            }
            catch (Exception ex)
            {
                GlobalOptions.WriteError($"Failed to validate configuration: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> CompileConfigs(string path)
        {
            try
            {
                GlobalOptions.WriteProcessing($"Compiling configurations: {path}");

                var config = new PeanutConfig();
                var hierarchy = await config.FindConfigHierarchyAsync(path);
                var compiledCount = 0;

                foreach (var configFile in hierarchy)
                {
                    if (configFile.Type == PeanutConfig.ConfigType.Text || 
                        configFile.Type == PeanutConfig.ConfigType.Tsk)
                    {
                        try
                        {
                            var content = await File.ReadAllTextAsync(configFile.Path);
                            var parsed = config.ParseTextConfig(content);
                            
                            var binaryPath = configFile.Path.Replace(".peanuts", ".pnt").Replace(".tsk", ".pnt");
                            await config.CompileToBinaryAsync(parsed, binaryPath);
                            
                            GlobalOptions.WriteSuccess($"Compiled: {configFile.Path} → {binaryPath}");
                            compiledCount++;
                        }
                        catch (Exception ex)
                        {
                            GlobalOptions.WriteError($"Failed to compile {configFile.Path}: {ex.Message}");
                        }
                    }
                }

                GlobalOptions.WriteSuccess($"Compiled {compiledCount} configuration files");
                return 0;
            }
            catch (Exception ex)
            {
                GlobalOptions.WriteError($"Failed to compile configurations: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> GenerateConfigDocs(string path)
        {
            try
            {
                GlobalOptions.WriteProcessing($"Generating configuration documentation: {path}");

                var config = new PeanutConfig();
                var hierarchy = await config.FindConfigHierarchyAsync(path);
                var docs = new StringBuilder();

                docs.AppendLine("# TuskLang Configuration Documentation");
                docs.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                docs.AppendLine($"Path: {Path.GetFullPath(path)}");
                docs.AppendLine();

                foreach (var configFile in hierarchy)
                {
                    if (File.Exists(configFile.Path))
                    {
                        docs.AppendLine($"## {Path.GetFileName(configFile.Path)}");
                        docs.AppendLine($"**Path:** {configFile.Path}");
                        docs.AppendLine($"**Type:** {configFile.Type}");
                        docs.AppendLine();

                        try
                        {
                            var content = await File.ReadAllTextAsync(configFile.Path);
                            var parsed = config.ParseTextConfig(content);

                            foreach (var section in parsed)
                            {
                                docs.AppendLine($"### [{section.Key}]");
                                
                                if (section.Value is Dictionary<string, object> sectionData)
                                {
                                    foreach (var kvp in sectionData)
                                    {
                                        var valueType = GetValueType(kvp.Value);
                                        var defaultValue = GetDefaultValue(kvp.Value);
                                        
                                        docs.AppendLine($"- **{kvp.Key}** (`{valueType}`)");
                                        docs.AppendLine($"  - Default: `{defaultValue}`");
                                        
                                        // Add description if available in comments
                                        var description = GetKeyDescription(content, section.Key, kvp.Key);
                                        if (!string.IsNullOrEmpty(description))
                                        {
                                            docs.AppendLine($"  - Description: {description}");
                                        }
                                        docs.AppendLine();
                                    }
                                }
                                docs.AppendLine();
                            }
                        }
                        catch (Exception ex)
                        {
                            docs.AppendLine($"**Error:** {ex.Message}");
                            docs.AppendLine();
                        }
                    }
                }

                var docsPath = Path.Combine(path, "CONFIG_DOCS.md");
                await File.WriteAllTextAsync(docsPath, docs.ToString());

                GlobalOptions.WriteSuccess($"Configuration documentation generated: {docsPath}");
                return 0;
            }
            catch (Exception ex)
            {
                GlobalOptions.WriteError($"Failed to generate configuration documentation: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> ClearConfigCache(string path)
        {
            try
            {
                GlobalOptions.WriteProcessing($"Clearing configuration cache: {path}");

                var config = new PeanutConfig();
                
                // Clear the cache by disposing and recreating
                config.Dispose();
                
                // Also clear any binary cache files
                var binaryFiles = Directory.GetFiles(path, "*.pnt", SearchOption.AllDirectories);
                var clearedCount = 0;

                foreach (var binaryFile in binaryFiles)
                {
                    try
                    {
                        File.Delete(binaryFile);
                        GlobalOptions.WriteLine($"Cleared: {binaryFile}");
                        clearedCount++;
                    }
                    catch (Exception ex)
                    {
                        GlobalOptions.WriteWarning($"Failed to clear {binaryFile}: {ex.Message}");
                    }
                }

                GlobalOptions.WriteSuccess($"Configuration cache cleared. Removed {clearedCount} binary files.");
                return 0;
            }
            catch (Exception ex)
            {
                GlobalOptions.WriteError($"Failed to clear configuration cache: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> ShowConfigStats()
        {
            try
            {
                GlobalOptions.WriteProcessing("Gathering configuration statistics...");

                var config = new PeanutConfig();
                var hierarchy = await config.FindConfigHierarchyAsync(".");
                var stats = new Dictionary<string, object>();

                var totalFiles = hierarchy.Count;
                var totalSize = 0L;
                var sectionCount = 0;
                var keyCount = 0;
                var binaryFiles = 0;
                var textFiles = 0;

                foreach (var configFile in hierarchy)
                {
                    if (File.Exists(configFile.Path))
                    {
                        var fileInfo = new FileInfo(configFile.Path);
                        totalSize += fileInfo.Length;

                        if (configFile.Type == PeanutConfig.ConfigType.Binary)
                            binaryFiles++;
                        else
                            textFiles++;

                        try
                        {
                            var content = await File.ReadAllTextAsync(configFile.Path);
                            var parsed = config.ParseTextConfig(content);
                            
                            sectionCount += parsed.Count;
                            foreach (var section in parsed.Values)
                            {
                                if (section is Dictionary<string, object> sectionData)
                                    keyCount += sectionData.Count;
                            }
                        }
                        catch
                        {
                            // Skip files that can't be parsed
                        }
                    }
                }

                stats["total_files"] = totalFiles;
                stats["total_size_bytes"] = totalSize;
                stats["total_size_mb"] = Math.Round(totalSize / 1024.0 / 1024.0, 2);
                stats["section_count"] = sectionCount;
                stats["key_count"] = keyCount;
                stats["binary_files"] = binaryFiles;
                stats["text_files"] = textFiles;
                stats["average_keys_per_section"] = sectionCount > 0 ? Math.Round((double)keyCount / sectionCount, 2) : 0;

                if (GlobalOptions.JsonOutput)
                {
                    var json = JsonSerializer.Serialize(stats, new JsonSerializerOptions { WriteIndented = true });
                    GlobalOptions.WriteLine(json);
                }
                else
                {
                    GlobalOptions.WriteLine("Configuration Statistics:");
                    GlobalOptions.WriteLine("========================");
                    GlobalOptions.WriteLine($"Total Files: {stats["total_files"]}");
                    GlobalOptions.WriteLine($"Total Size: {stats["total_size_mb"]} MB");
                    GlobalOptions.WriteLine($"Sections: {stats["section_count"]}");
                    GlobalOptions.WriteLine($"Keys: {stats["key_count"]}");
                    GlobalOptions.WriteLine($"Binary Files: {stats["binary_files"]}");
                    GlobalOptions.WriteLine($"Text Files: {stats["text_files"]}");
                    GlobalOptions.WriteLine($"Avg Keys/Section: {stats["average_keys_per_section"]}");
                }

                return 0;
            }
            catch (Exception ex)
            {
                GlobalOptions.WriteError($"Failed to show configuration statistics: {ex.Message}");
                return 1;
            }
        }

        // Helper methods
        private static object GetValueByPath(Dictionary<string, object> config, string keyPath)
        {
            var keys = keyPath.Split('.');
            object current = config;

            foreach (var key in keys)
            {
                if (current is Dictionary<string, object> dict && dict.TryGetValue(key, out var value))
                {
                    current = value;
                }
                else
                {
                    return null;
                }
            }

            return current;
        }

        private static async Task<ValidationResult> ValidateConfigFile(PeanutConfig.ConfigFile configFile)
        {
            try
            {
                var config = new PeanutConfig();
                var content = await File.ReadAllTextAsync(configFile.Path);
                var parsed = config.ParseTextConfig(content);

                var sectionCount = parsed.Count;
                var keyCount = 0;

                foreach (var section in parsed.Values)
                {
                    if (section is Dictionary<string, object> sectionData)
                        keyCount += sectionData.Count;
                }

                return new ValidationResult
                {
                    FilePath = configFile.Path,
                    IsValid = true,
                    SectionCount = sectionCount,
                    KeyCount = keyCount
                };
            }
            catch (Exception ex)
            {
                return new ValidationResult
                {
                    FilePath = configFile.Path,
                    IsValid = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private static string GetValueType(object value)
        {
            return value switch
            {
                string => "string",
                int => "integer",
                long => "long",
                double => "double",
                float => "float",
                bool => "boolean",
                null => "null",
                _ => value.GetType().Name.ToLower()
            };
        }

        private static string GetDefaultValue(object value)
        {
            if (value == null) return "null";
            if (value is string str) return $"\"{str}\"";
            if (value is bool b) return b.ToString().ToLower();
            return value.ToString();
        }

        private static string GetKeyDescription(string content, string section, string key)
        {
            // Simple comment extraction - look for comments above the key
            var lines = content.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (line.StartsWith($"[{section}]"))
                {
                    // Look for the key in subsequent lines
                    for (int j = i + 1; j < lines.Length; j++)
                    {
                        var nextLine = lines[j].Trim();
                        if (nextLine.StartsWith($"{key} =") || nextLine.StartsWith($"{key}:"))
                        {
                            // Check if there's a comment above this line
                            if (j > 0 && lines[j - 1].Trim().StartsWith("#"))
                            {
                                return lines[j - 1].Trim().Substring(1).Trim();
                            }
                            break;
                        }
                        if (nextLine.StartsWith("[") && nextLine.EndsWith("]"))
                            break;
                    }
                }
            }
            return "";
        }
    }

    /// <summary>
    /// Validation result data structure
    /// </summary>
    public class ValidationResult
    {
        public string FilePath { get; set; }
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
        public int SectionCount { get; set; }
        public int KeyCount { get; set; }
    }
} 