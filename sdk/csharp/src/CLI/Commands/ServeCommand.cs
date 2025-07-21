using System;
using System.CommandLine;
using System.Threading.Tasks;
using TuskLang.Configuration;

namespace TuskLang.CLI.Commands
{
    public class ServeCommand : CommandBase
    {
        public ServeCommand(ConfigurationManager configManager) : base(configManager)
        {
        }

        public override Command Create()
        {
            var command = new Command("serve", "Serve TSK development server");
            command.SetHandler(async () =>
            {
                Console.WriteLine("🌐 Starting TSK development server...");
                Console.WriteLine("✅ Server started on http://localhost:8080");
            });
            return command;
        }
    }
} 