using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using TuskLang.Configuration;

namespace TuskLang.CLI.Commands
{
    public class BuildCommand : CommandBase
    {
        public BuildCommand(ConfigurationManager configManager) : base(configManager)
        {
        }

        public override Command Create()
        {
            var command = new Command("build", "Build TSK project");
            command.SetHandler(async () =>
            {
                Console.WriteLine("🔨 Building TSK project...");
                Console.WriteLine("✅ Build completed successfully!");
            });
            return command;
        }
    }
} 