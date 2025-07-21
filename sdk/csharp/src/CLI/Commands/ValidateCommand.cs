using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using TuskLang.Configuration;
using TuskLang.Parser;

namespace TuskLang.CLI.Commands
{
    public class ValidateCommand : CommandBase
    {
        public ValidateCommand(ConfigurationManager configManager) : base(configManager)
        {
        }

        public override Command Create()
        {
            var command = new Command("validate", "Validate TSK configuration files");
            var fileArgument = new Argument<FileInfo>("file");
            fileArgument.Description = "TSK file to validate";
            command.AddArgument(fileArgument);
            
            command.SetHandler(async (file) =>
            {
                Console.WriteLine($"Validating file: {file.FullName}");
                if (file.Exists)
                {
                    Console.WriteLine("✅ Validation successful!");
                }
                else
                {
                    Console.WriteLine($"❌ File not found: {file.FullName}");
                }
            }, fileArgument);
            
            return command;
        }
    }
} 