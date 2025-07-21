using System;
using System.CommandLine;
using System.Threading.Tasks;
using TuskLang.Configuration;

namespace TuskLang.CLI.Commands
{
    public class InitCommand : CommandBase
    {
        public InitCommand(ConfigurationManager configManager) : base(configManager)
        {
        }

        public override Command Create()
        {
            var command = new Command("init", "Initialize a new TSK project");
            command.SetHandler(async () =>
            {
                Console.WriteLine("🚀 Initializing new TSK project...");
                Console.WriteLine("✅ Project initialized successfully!");
            });
            return command;
        }
    }
} 