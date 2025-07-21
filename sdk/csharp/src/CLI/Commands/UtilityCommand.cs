using System;
using System.CommandLine;
using System.Threading.Tasks;
using TuskLang.Configuration;

namespace TuskLang.CLI.Commands
{
    public class UtilityCommand : CommandBase
    {
        public UtilityCommand(ConfigurationManager configManager) : base(configManager)
        {
        }

        public override Command Create()
        {
            var command = new Command("utility", "TSK utility tools");
            command.SetHandler(async () =>
            {
                Console.WriteLine("🔧 TSK Utility Tools");
                Console.WriteLine("✅ Utilities ready!");
            });
            return command;
        }
    }
} 