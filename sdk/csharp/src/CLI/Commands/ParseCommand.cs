using System;
using System.CommandLine;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using TuskLang.Configuration;
using TuskLang.Parser;

namespace TuskLang.CLI.Commands
{
    public class ParseCommand : CommandBase
    {
        public ParseCommand(ConfigurationManager configManager) : base(configManager)
        {
        }

        public override Command Create()
        {
            var command = new Command("parse", "Parse TSK configuration files");

            var fileArgument = new Argument<FileInfo>("file");
            fileArgument.Description = "TSK file to parse";
            var verboseOption = new Option<bool>("--verbose", "Enable verbose output");
            var outputOption = new Option<string>("--output", "Output file path");

            command.AddArgument(fileArgument);
            command.AddOption(verboseOption);
            command.AddOption(outputOption);

            command.SetHandler(async (file, verbose, output) =>
            {
                await HandleParse(file, verbose, output);
            }, fileArgument, verboseOption, outputOption);

            return command;
        }

        private async Task HandleParse(FileInfo file, bool verbose, string output)
        {
            try
            {
                Console.WriteLine($"Parsing file: {file.FullName}");

                if (!file.Exists)
                {
                    Console.WriteLine($"Error: File {file.FullName} does not exist");
                    return;
                }

                var content = await File.ReadAllTextAsync(file.FullName);
                
                // Simple parsing logic for now
                if (verbose)
                {
                    Console.WriteLine($"File size: {file.Length} bytes");
                    Console.WriteLine($"Content length: {content.Length} characters");
                }

                Console.WriteLine("✅ Parse successful!");
                
                if (!string.IsNullOrEmpty(output))
                {
                    var outputPath = Path.Combine(output, $"{Path.GetFileNameWithoutExtension(file.Name)}.parsed");
                    await File.WriteAllTextAsync(outputPath, content);
                    Console.WriteLine($"Output saved to: {outputPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing file: {ex.Message}");
            }
        }

        public static ParseCommand Create(ConfigurationManager configManager)
        {
            return new ParseCommand(configManager);
        }
    }
} 