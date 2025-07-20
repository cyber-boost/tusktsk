using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using TuskLang;

namespace TuskLang.CLI.Commands
{
    /// <summary>
    /// Configuration commands for TuskLang CLI
    /// </summary>
    public static class ConfigCommands
    {
        public static Command CreateConfigCommand()
        {
            var fileOption = new Option<string>("--file", "Configuration file path");
            var validateOption = new Option<bool>("--validate", "Validate configuration");
            var formatOption = new Option<bool>("--format", "Format configuration file");

            var configCommand = new Command("config", "Configuration management commands")
            {
                fileOption,
                validateOption,
                formatOption
            };

            configCommand.SetHandler(async (file, validate, format) =>
            {
                if (validate)
                {
                    await ValidateConfig(file);
                }
                else if (format)
                {
                    await FormatConfig(file);
                }
                else
                {
                    await ShowConfig(file);
                }
            }, fileOption, validateOption, formatOption);

            return configCommand;
        }

        private static async Task ValidateConfig(string? filePath)
        {
            try
            {
                var config = new PeanutConfig(filePath ?? "peanu.tsk");
                var result = await ValidateConfigAsync(config);
                
                if (result.IsValid)
                {
                    Console.WriteLine("✅ Configuration is valid");
                    Console.WriteLine($"Sections: {result.SectionCount}");
                    Console.WriteLine($"Keys: {result.KeyCount}");
                    Console.WriteLine($"Lines: {result.LineCount}");
                }
                else
                {
                    Console.WriteLine($"❌ Configuration validation failed: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error validating config: {ex.Message}");
            }
        }

        private static async Task FormatConfig(string? filePath)
        {
            try
            {
                var config = new PeanutConfig(filePath ?? "peanu.tsk");
                await FormatConfigAsync(config);
                Console.WriteLine("✅ Configuration formatted successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error formatting config: {ex.Message}");
            }
        }

        private static async Task ShowConfig(string? filePath)
        {
            try
            {
                var config = new PeanutConfig(filePath ?? "peanu.tsk");
                var content = await config.LoadAsync();
                Console.WriteLine(content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading config: {ex.Message}");
            }
        }

        private static async Task<ValidationResult> ValidateConfigAsync(PeanutConfig config)
        {
            try
            {
                var content = await config.LoadAsync();
                var lines = content.Split('\n');
                var sections = 0;
                var keys = 0;

                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                    {
                        sections++;
                    }
                    else if (trimmed.Contains("=") && !trimmed.StartsWith("#"))
                    {
                        keys++;
                    }
                }

                return new ValidationResult
                {
                    IsValid = true,
                    SectionCount = sections,
                    KeyCount = keys,
                    LineCount = lines.Length
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

        private static async Task FormatConfigAsync(PeanutConfig config)
        {
            var content = await config.LoadAsync();
            var lines = content.Split('\n');
            var formattedLines = new List<string>();
            var inSection = false;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed))
                {
                    if (inSection)
                    {
                        formattedLines.Add("");
                    }
                    continue;
                }

                if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                {
                    if (inSection)
                    {
                        formattedLines.Add("");
                    }
                    formattedLines.Add(trimmed);
                    inSection = true;
                }
                else if (trimmed.StartsWith("#"))
                {
                    formattedLines.Add(trimmed);
                }
                else if (trimmed.Contains("="))
                {
                    var parts = trimmed.Split('=', 2);
                    if (parts.Length == 2)
                    {
                        var key = parts[0].Trim();
                        var value = parts[1].Trim();
                        formattedLines.Add($"  {key} = {value}");
                    }
                }
            }

            var formattedContent = string.Join("\n", formattedLines);
            await File.WriteAllTextAsync(config.FilePath, formattedContent);
        }

        /// <summary>
        /// Validation result data structure
        /// </summary>
        public class ValidationResult
        {
            public bool IsValid { get; set; }
            public string ErrorMessage { get; set; } = "";
            public int SectionCount { get; set; }
            public int KeyCount { get; set; }
            public int LineCount { get; set; }
        }
    }
} 