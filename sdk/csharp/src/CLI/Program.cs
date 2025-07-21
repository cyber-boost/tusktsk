using System;
using System.CommandLine;
using System.Threading.Tasks;
using TuskLang.Configuration;
using TuskLang.Parser;
using TuskLang.CLI.Commands;
using System.Linq;

namespace TuskLang.CLI
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            try
            {
                var rootCommand = new RootCommand("TuskLang C# SDK - Minimal Viable Build");

                var helpOption = new Option<bool>("--help", "Show help information");
                helpOption.AddAlias("-h");
                rootCommand.Add(helpOption);

                var configManager = new ConfigurationManager();
                
                // Create and add commands properly
                rootCommand.Add(new ParseCommand(configManager).Create());
                rootCommand.Add(new CompileCommand(configManager).Create());
                rootCommand.Add(new ValidateCommand(configManager).Create());
                rootCommand.Add(new InitCommand(configManager).Create());
                rootCommand.Add(new BuildCommand(configManager).Create());
                rootCommand.Add(new TestCommand(configManager).Create());
                rootCommand.Add(new ServeCommand(configManager).Create());
                rootCommand.Add(new ConfigCommand(configManager).Create());
                rootCommand.Add(new ProjectCommand(configManager).Create());
                rootCommand.Add(new AiCommand(configManager).Create());
                rootCommand.Add(new UtilityCommand(configManager).Create());

                // Use Invoke for compatibility
                var result = rootCommand.Invoke(args);
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }
    }
} 