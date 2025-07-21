using System;
using System.Threading.Tasks;
using TuskLang.CLI.Commands;

namespace TuskLang.CLI
{
    /// <summary>
    /// TuskLang C# CLI - Universal Command Line Interface
    /// Complete implementation with both tusk and tusk-dotnet commands
    /// </summary>
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            try
            {
                // Determine which command to execute based on first argument
                if (args.Length > 0 && args[0] == "tusk-dotnet")
                {
                    // Execute tusk-dotnet command
                    var dotnetArgs = new string[args.Length - 1];
                    Array.Copy(args, 1, dotnetArgs, 0, dotnetArgs.Length);
                    return await TuskDotnetCommand.ExecuteAsync(dotnetArgs);
                }
                else
                {
                    // Execute regular tusk command system
                    return await TuskCommand.ExecuteAsync(args);
                }
            }
            catch (Exception ex)
            {
                // Global exception handler
                Console.Error.WriteLine($"❌ Fatal error: {ex.Message}");
                
                if (args.Length > 0 && (args[0] == "--verbose" || args[0] == "-v"))
                {
                    Console.Error.WriteLine($"Stack trace: {ex.StackTrace}");
                }
                
                return 1;
            }
        }
    }
} 