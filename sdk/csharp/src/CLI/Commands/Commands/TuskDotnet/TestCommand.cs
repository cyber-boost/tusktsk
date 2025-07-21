using System;
using System.CommandLine;
using System.Threading.Tasks;

namespace TuskLang.CLI.Commands.TuskDotnet
{
    public static class TestCommand
    {
        public static Command CreateTestCommand()
        {
            var projectArgument = new Argument<string>("project", "Test project to run") { Arity = ArgumentArity.ZeroOrOne };
            var configurationOption = new Option<string>("--configuration", () => "Debug", "Build configuration");
            var frameworkOption = new Option<string>("--framework", "Target framework");
            var loggerOption = new Option<string>("--logger", "Test logger");

            var testCommand = new Command("test", "Run tests with TuskLang configuration")
            {
                projectArgument,
                configurationOption,
                frameworkOption,
                loggerOption
            };

            testCommand.SetHandler(async (project, configuration, framework, logger) =>
            {
                var command = new TestCommandImplementation();
                Environment.ExitCode = await command.ExecuteAsync(project, configuration, framework, logger);
            }, projectArgument, configurationOption, frameworkOption, loggerOption);

            return testCommand;
        }
    }

    public class TestCommandImplementation : DotnetCommandBase
    {
        public async Task<int> ExecuteAsync(string project, string configuration, string framework, string logger)
        {
            return await ExecuteDotnetCommandAsync(async () =>
            {
                WriteProcessing("🧪 Running tests with TuskLang configuration...");
                
                var projectPath = project ?? ".";
                var args = $"test \"{projectPath}\" --configuration {configuration}";
                
                if (!string.IsNullOrEmpty(framework))
                    args += $" --framework {framework}";
                    
                if (!string.IsNullOrEmpty(logger))
                    args += $" --logger {logger}";
                
                var result = await RunDotnetCommandAsync(args, null, true);
                
                if (result.Success)
                {
                    WriteSuccess("✅ All tests passed");
                }
                else
                {
                    WriteError("❌ Some tests failed");
                }
                
                return result.Success ? 0 : 1;
            }, "Test");
        }
    }
} 