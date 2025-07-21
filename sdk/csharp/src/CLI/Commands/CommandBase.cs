using System.CommandLine;
using TuskLang.Configuration;

namespace TuskLang.CLI.Commands
{
    public abstract class CommandBase
    {
        protected ConfigurationManager ConfigManager { get; }

        protected CommandBase(ConfigurationManager configManager)
        {
            ConfigManager = configManager;
        }

        public abstract Command Create();
    }
} 