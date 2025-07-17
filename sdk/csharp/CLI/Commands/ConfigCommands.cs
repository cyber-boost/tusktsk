using System;
using System.CommandLine;
using System.CommandLine.Invocation;
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
            var configCommand = new Command("config", "Configuration management commands")
            {
                new Option<string>("--file", "Configuration file path") { IsRequired = false },
                new Option<bool>("--validate", "Validate configuration") { IsRequired = false },
                new Option<bool>("--format", "Format configuration file") { IsRequired = false }
            };

            configCommand.SetHandler(async (context) =>
            {
                var file = context.ParseResult.GetValueForOption<string>("--file");
                var validate = context.ParseResult.GetValueForOption<bool>("--validate");
                var format = context.ParseResult.GetValueForOption<bool>("--format");

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
            });

            return configCommand;
        }

        private static async Task ValidateConfig(string? filePath)
        {
            try
            {
                var config = new PeanutConfig(filePath ?? "peanu.tsk");
                var result = await config.ValidateAsync();
                
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
                await config.FormatAsync();
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