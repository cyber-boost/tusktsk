using System;
using System.CommandLine;
using System.Threading.Tasks;

namespace TuskLang.CLI.Commands.TuskDotnet
{
    public static class RestoreCommand
    {
        public static Command CreateRestoreCommand()
        {
            var projectArgument = new Argument<string>("project", "Project to restore") { Arity = ArgumentArity.ZeroOrOne };
            var verbosityOption = new Option<string>("--verbosity", () => "minimal", "Verbosity level");

            var restoreCommand = new Command("restore", "Restore packages and TuskLang dependencies")
            {
                projectArgument,
                verbosityOption
            };

            restoreCommand.SetHandler(async (project, verbosity) =>
            {
                var command = new RestoreCommandImplementation();
                Environment.ExitCode = await command.ExecuteAsync(project, verbosity);
            }, projectArgument, verbosityOption);

            return restoreCommand;
        }
    }

    public class RestoreCommandImplementation : DotnetCommandBase
    {
        public async Task<int> ExecuteAsync(string project, string verbosity)
        {
            return await ExecuteDotnetCommandAsync(async () =>
            {
                WriteProcessing("🔄 Restoring NuGet packages and TuskLang dependencies...");
                
                var projectPath = project ?? ".";
                var args = $"restore \"{projectPath}\" --verbosity {verbosity}";
                
                var result = await RunDotnetCommandAsync(args);
                
                if (result.Success)
                {
                    WriteSuccess("✅ Packages restored successfully");
                }
                
                return result.Success ? 0 : 1;
            }, "Restore");
        }
    }
} 