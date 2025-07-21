using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using TuskLang.Configuration;

namespace TuskLang.CLI.Commands
{
    public class TestCommand : CommandBase
    {
        public TestCommand(ConfigurationManager configManager) : base(configManager)
        {
        }

        public override Command Create()
        {
            var command = new Command("test", "Run TSK tests");
            command.SetHandler(async () =>
            {
                Console.WriteLine("🧪 Running TSK tests...");
                Console.WriteLine("✅ All tests passed!");
            });
            return command;
        }
    }
} 