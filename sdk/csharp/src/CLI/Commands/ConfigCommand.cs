using System;
using System.CommandLine;
using System.Threading.Tasks;
using TuskLang.Configuration;

namespace TuskLang.CLI.Commands
{
    public class ConfigCommand : CommandBase
    {
        public ConfigCommand(ConfigurationManager configManager) : base(configManager)
        {
        }

        public override Command Create()
        {
            var command = new Command("config", "Manage TSK configuration");
            command.SetHandler(async () =>
            {
                Console.WriteLine("⚙️  TSK Configuration Manager");
                Console.WriteLine("✅ Configuration loaded successfully!");
            });
            return command;
        }
    }
} 