using System;
using System.CommandLine;
using System.Threading.Tasks;
using TuskLang.Configuration;

namespace TuskTsk.CLI.Commands
{
    public class LicenseCommand : CommandBase
    {
        public LicenseCommand(ConfigurationManager configManager) : base(configManager)
        {
        }

        public override Command Create()
        {
            var command = new Command("license", "License management");

            var actionOption = new Option<string>("--action");
            var verboseOption = new Option<bool>("--verbose");

            command.Add(actionOption);
            command.Add(verboseOption);

            // For beta version, we'll use a simple approach without SetHandler
            return command;
        }
    }
} 