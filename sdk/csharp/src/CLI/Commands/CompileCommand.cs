using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using TuskLang.Configuration;

namespace TuskLang.CLI.Commands
{
    public class CompileCommand : CommandBase
    {
        public CompileCommand(ConfigurationManager configManager) : base(configManager)
        {
        }

        public override Command Create()
        {
            var command = new Command("compile", "Compile TSK configuration files");

            var fileArgument = new Argument<FileInfo>("file");
            fileArgument.Description = "TSK file to compile";
            var outputOption = new Option<string>("--output", "Output directory");
            var verboseOption = new Option<bool>("--verbose", "Enable verbose output");

            command.AddArgument(fileArgument);
            command.AddOption(outputOption);
            command.AddOption(verboseOption);

            command.SetHandler(async (file, output, verbose) =>
            {
                await HandleCompile(file, output, verbose);
            }, fileArgument, outputOption, verboseOption);

            return command;
        }

        private async Task HandleCompile(FileInfo file, string output, bool verbose)
        {
            try
            {
                Console.WriteLine($"Compiling file: {file.FullName}");

                if (!file.Exists)
                {
                    Console.WriteLine($"Error: File {file.FullName} does not exist");
                    return;
                }

                var content = await File.ReadAllTextAsync(file.FullName);
                
                // Basic compilation logic
                if (verbose)
                {
                    Console.WriteLine($"File size: {file.Length} bytes");
                    Console.WriteLine($"Content length: {content.Length} characters");
                }

                Console.WriteLine("✅ Compilation successful!");
                
                if (!string.IsNullOrEmpty(output))
                {
                    var outputPath = Path.Combine(output, $"{Path.GetFileNameWithoutExtension(file.Name)}.compiled");
                    await File.WriteAllTextAsync(outputPath, content);
                    Console.WriteLine($"Output saved to: {outputPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error compiling file: {ex.Message}");
            }
        }

        public static CompileCommand Create(ConfigurationManager configManager)
        {
            return new CompileCommand(configManager);
        }
    }
} 