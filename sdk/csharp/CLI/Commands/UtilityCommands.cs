using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using System.Linq; // Added for .Select()

namespace TuskLang.CLI.Commands
{
    /// <summary>
    /// Utility commands for TuskLang CLI
    /// </summary>
    public static class UtilityCommands
    {
        public static void AddCommands(RootCommand rootCommand)
        {
            var parseCommand = new Command("parse", "Parse and display TSK file contents")
            {
                new Argument<string>("file", "TSK file to parse"),
                new Option<bool>("--json", "Output in JSON format"),
                new Option<bool>("--pretty", "Pretty print output"),
                Handler = CommandHandler.Create<string, bool, bool>(ParseTskFile)
            };

            var validateCommand = new Command("validate", "Validate TSK file syntax")
            {
                new Argument<string>("file", "TSK file to validate"),
                new Option<bool>("--strict", "Enable strict validation"),
                Handler = CommandHandler.Create<string, bool>(ValidateTskFile)
            };

            var convertCommand = new Command("convert", "Convert between formats")
            {
                new Option<string>("-i", "Input file") { IsRequired = true },
                new Option<string>("-o", "Output file") { IsRequired = true },
                new Option<string>("--from", "Input format"),
                new Option<string>("--to", "Output format"),
                Handler = CommandHandler.Create<string, string, string, string>(ConvertFile)
            };

            var getCommand = new Command("get", "Get specific value by key path")
            {
                new Argument<string>("file", "TSK file"),
                new Argument<string>("key.path", "Key path"),
                new Option<bool>("--json", "Output in JSON format"),
                Handler = CommandHandler.Create<string, string, bool>(GetValue)
            };

            var setCommand = new Command("set", "Set value by key path")
            {
                new Argument<string>("file", "TSK file"),
                new Argument<string>("key.path", "Key path"),
                new Argument<string>("value", "Value to set"),
                new Option<bool>("--create", "Create file if it doesn't exist"),
                Handler = CommandHandler.Create<string, string, string, bool>(SetValue)
            };

            var versionCommand = new Command("version", "Show version information")
            {
                Handler = CommandHandler.Create(ShowVersion)
            };

            var helpCommand = new Command("help", "Show help for command")
            {
                new Argument<string>("command", "Command to show help for"),
                Handler = CommandHandler.Create<string>(ShowHelp)
            };

            rootCommand.AddCommand(parseCommand);
            rootCommand.AddCommand(validateCommand);
            rootCommand.AddCommand(convertCommand);
            rootCommand.AddCommand(getCommand);
            rootCommand.AddCommand(setCommand);
            rootCommand.AddCommand(versionCommand);
            rootCommand.AddCommand(helpCommand);
        }

        private static async Task<int> ParseTskFile(string file, bool json, bool pretty)
        {
            try
            {
                if (!File.Exists(file))
                {
                    GlobalOptions.WriteError($"File not found: {file}");
                    return 3;
                }

                GlobalOptions.WriteProcessing($"Parsing TSK file: {file}");

                var content = await File.ReadAllTextAsync(file);
                var tsk = TSK.FromString(content);
                var data = tsk.ToDictionary();

                if (json)
                {
                    var options = new System.Text.Json.JsonSerializerOptions
                    {
                        WriteIndented = pretty
                    };
                    var jsonOutput = System.Text.Json.JsonSerializer.Serialize(data, options);
                    GlobalOptions.WriteLine(jsonOutput);
                }
                else
                {
                    if (pretty)
                    {
                        GlobalOptions.WriteLine(PrettyPrintTsk(data));
                    }
                    else
                    {
                        GlobalOptions.WriteLine(tsk.ToString());
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                GlobalOptions.WriteError($"Failed to parse TSK file: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> ValidateTskFile(string file, bool strict)
        {
            try
            {
                if (!File.Exists(file))
                {
                    GlobalOptions.WriteError($"File not found: {file}");
                    return 3;
                }

                GlobalOptions.WriteProcessing($"Validating TSK file: {file}");

                var content = await File.ReadAllTextAsync(file);
                var validationResult = ValidateTskContent(content, strict);

                if (validationResult.IsValid)
                {
                    GlobalOptions.WriteSuccess("TSK file is valid");
                    if (GlobalOptions.Verbose)
                    {
                        GlobalOptions.WriteLine($"Sections: {validationResult.SectionCount}");
                        GlobalOptions.WriteLine($"Keys: {validationResult.KeyCount}");
                        GlobalOptions.WriteLine($"Lines: {validationResult.LineCount}");
                    }
                    return 0;
                }
                else
                {
                    GlobalOptions.WriteError($"TSK file is invalid: {validationResult.ErrorMessage}");
                    return 1;
                }
            }
            catch (Exception ex)
            {
                GlobalOptions.WriteError($"Validation failed: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> ConvertFile(string input, string output, string from, string to)
        {
            try
            {
                if (!File.Exists(input))
                {
                    GlobalOptions.WriteError($"Input file not found: {input}");
                    return 3;
                }

                GlobalOptions.WriteProcessing($"Converting {input} to {output}");

                var inputContent = await File.ReadAllTextAsync(input);
                var inputFormat = from ?? DetectFormat(input);
                var outputFormat = to ?? DetectFormat(output);

                var convertedContent = await ConvertContent(inputContent, inputFormat, outputFormat);
                await File.WriteAllTextAsync(output, convertedContent);

                GlobalOptions.WriteSuccess($"File converted: {input} → {output}");
                return 0;
            }
            catch (Exception ex)
            {
                GlobalOptions.WriteError($"Conversion failed: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> GetValue(string file, string keyPath, bool json)
        {
            try
            {
                if (!File.Exists(file))
                {
                    GlobalOptions.WriteError($"File not found: {file}");
                    return 3;
                }

                GlobalOptions.WriteProcessing($"Getting value: {keyPath} from {file}");

                var content = await File.ReadAllTextAsync(file);
                var tsk = TSK.FromString(content);
                var data = tsk.ToDictionary();

                var value = GetValueByPath(data, keyPath);
                
                if (value != null)
                {
                    if (json)
                    {
                        var jsonOutput = System.Text.Json.JsonSerializer.Serialize(new { key = keyPath, value = value });
                        GlobalOptions.WriteLine(jsonOutput);
                    }
                    else
                    {
                        GlobalOptions.WriteLine(value.ToString());
                    }
                    return 0;
                }
                else
                {
                    GlobalOptions.WriteError($"Key not found: {keyPath}");
                    return 1;
                }
            }
            catch (Exception ex)
            {
                GlobalOptions.WriteError($"Failed to get value: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> SetValue(string file, string keyPath, string value, bool create)
        {
            try
            {
                GlobalOptions.WriteProcessing($"Setting value: {keyPath} = {value} in {file}");

                Dictionary<string, object> data;
                if (File.Exists(file))
                {
                    var content = await File.ReadAllTextAsync(file);
                    var tsk = TSK.FromString(content);
                    data = tsk.ToDictionary();
                }
                else if (create)
                {
                    data = new Dictionary<string, object>();
                }
                else
                {
                    GlobalOptions.WriteError($"File not found: {file}");
                    return 3;
                }

                SetValueByPath(data, keyPath, ParseValue(value));
                
                var tskNew = new TSK(data);
                var output = tskNew.ToString();
                await File.WriteAllTextAsync(file, output);

                GlobalOptions.WriteSuccess($"Value set successfully: {keyPath} = {value}");
                return 0;
            }
            catch (Exception ex)
            {
                GlobalOptions.WriteError($"Failed to set value: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> ShowVersion()
        {
            GlobalOptions.WriteLine("TuskLang C# CLI v2.0.0");
            GlobalOptions.WriteLine("Strong. Secure. Scalable.");
            GlobalOptions.WriteLine($"Built: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            return 0;
        }

        private static async Task<int> ShowHelp(string command)
        {
            GlobalOptions.WriteLine($"Help for command: {command}");
            GlobalOptions.WriteLine("Use 'tsk --help' for general help");
            return 0;
        }

        // Helper methods
        private static string PrettyPrintTsk(Dictionary<string, object> data)
        {
            var sb = new StringBuilder();
            
            foreach (var section in data)
            {
                sb.AppendLine($"[{section.Key}]");
                
                if (section.Value is Dictionary<string, object> sectionData)
                {
                    foreach (var kvp in sectionData)
                    {
                        var formattedValue = FormatValueForDisplay(kvp.Value);
                        sb.AppendLine($"  {kvp.Key} = {formattedValue}");
                    }
                }
                else
                {
                    var formattedValue = FormatValueForDisplay(section.Value);
                    sb.AppendLine($"  value = {formattedValue}");
                }
                
                sb.AppendLine();
            }
            
            return sb.ToString();
        }

        private static string FormatValueForDisplay(object value)
        {
            if (value == null) return "null";
            if (value is string str) return $"\"{str}\"";
            if (value is bool b) return b.ToString().ToLower();
            if (value is int || value is long || value is double || value is float) return value.ToString();
            
            if (value is Dictionary<string, object> dict)
            {
                var pairs = dict.Select(kvp => $"\"{kvp.Key}\" = {FormatValueForDisplay(kvp.Value)}");
                return $"{{{string.Join(", ", pairs)}}}";
            }
            
            if (value is IEnumerable<object> enumerable)
            {
                var items = enumerable.Select(FormatValueForDisplay);
                return $"[{string.Join(", ", items)}]";
            }
            
            return $"\"{value}\"";
        }

        private static ValidationResult ValidateTskContent(string content, bool strict)
        {
            try
            {
                var tsk = TSK.FromString(content);
                var data = tsk.ToDictionary();
                
                var sectionCount = data.Count;
                var keyCount = 0;
                var lineCount = content.Split('\n').Length;

                foreach (var section in data.Values)
                {
                    if (section is Dictionary<string, object> sectionData)
                        keyCount += sectionData.Count;
                }

                if (strict)
                {
                    // Additional strict validation
                    if (string.IsNullOrWhiteSpace(content))
                    {
                        return new ValidationResult { IsValid = false, ErrorMessage = "File is empty" };
                    }
                }

                return new ValidationResult
                {
                    IsValid = true,
                    SectionCount = sectionCount,
                    KeyCount = keyCount,
                    LineCount = lineCount
                };
            }
            catch (Exception ex)
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private static string DetectFormat(string filename)
        {
            var extension = Path.GetExtension(filename).ToLower();
            return extension switch
            {
                ".tsk" => "tsk",
                ".json" => "json",
                ".yaml" or ".yml" => "yaml",
                ".ini" => "ini",
                ".xml" => "xml",
                _ => "tsk"
            };
        }

        private static async Task<string> ConvertContent(string content, string fromFormat, string toFormat)
        {
            // Parse from source format
            Dictionary<string, object> data;
            
            switch (fromFormat.ToLower())
            {
                case "tsk":
                    var tsk = TSK.FromString(content);
                    data = tsk.ToDictionary();
                    break;
                    
                case "json":
                    data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(content);
                    break;
                    
                default:
                    throw new NotSupportedException($"Input format not supported: {fromFormat}");
            }

            // Convert to target format
            switch (toFormat.ToLower())
            {
                case "tsk":
                    var tskNew = new TSK(data);
                    return tskNew.ToString();
                    
                case "json":
                    return System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    
                default:
                    throw new NotSupportedException($"Output format not supported: {toFormat}");
            }
        }

        private static object GetValueByPath(Dictionary<string, object> data, string keyPath)
        {
            var keys = keyPath.Split('.');
            object current = data;

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

        private static void SetValueByPath(Dictionary<string, object> data, string keyPath, object value)
        {
            var keys = keyPath.Split('.');
            var current = data;

            for (int i = 0; i < keys.Length - 1; i++)
            {
                var key = keys[i];
                if (!current.ContainsKey(key))
                {
                    current[key] = new Dictionary<string, object>();
                }
                
                if (current[key] is Dictionary<string, object> dict)
                {
                    current = dict;
                }
                else
                {
                    throw new InvalidOperationException($"Cannot set value at {keyPath}: {key} is not a section");
                }
            }

            current[keys[^1]] = value;
        }

        private static object ParseValue(string value)
        {
            // Try to parse as different types
            if (value.ToLower() == "true") return true;
            if (value.ToLower() == "false") return false;
            if (value.ToLower() == "null") return null;
            
            if (int.TryParse(value, out var intValue)) return intValue;
            if (double.TryParse(value, out var doubleValue)) return doubleValue;
            
            // Remove quotes if present
            if ((value.StartsWith("\"") && value.EndsWith("\"")) ||
                (value.StartsWith("'") && value.EndsWith("'")))
            {
                return value.Substring(1, value.Length - 2);
            }
            
            return value;
        }
    }

    /// <summary>
    /// Validation result data structure
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
        public int SectionCount { get; set; }
        public int KeyCount { get; set; }
        public int LineCount { get; set; }
    }
} 