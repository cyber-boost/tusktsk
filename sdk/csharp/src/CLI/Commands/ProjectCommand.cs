using System;
using System.CommandLine;
using System.Threading.Tasks;
using TuskLang.Configuration;

namespace TuskLang.CLI.Commands
{
    public class ProjectCommand : CommandBase
    {
        public ProjectCommand(ConfigurationManager configManager) : base(configManager)
        {
        }

        public override Command Create()
        {
            var command = new Command("project", "Manage TSK projects");
            command.SetHandler(async () =>
            {
                Console.WriteLine("📁 TSK Project Manager");
                Console.WriteLine("✅ Project management ready!");
            });
            return command;
        }
    }
} 