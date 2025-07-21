using System;
using System.CommandLine;
using System.Threading.Tasks;

namespace TuskLang.CLI.Commands.TuskDotnet
{
    public static class PublishCommand
    {
        public static Command CreatePublishCommand()
        {
            var projectArgument = new Argument<string>("project", "Project to publish") { Arity = ArgumentArity.ZeroOrOne };
            var configurationOption = new Option<string>("--configuration", () => "Release", "Build configuration");
            var environmentOption = new Option<string>("--environment", "Target environment for publishing");
            var outputOption = new Option<string>("--output", "Output directory");
            var runtimeOption = new Option<string>("--runtime", "Target runtime identifier");
            var selfContainedOption = new Option<bool>("--self-contained", "Create self-contained deployment");

            var publishCommand = new Command("publish", "Publish with environment-specific configurations")
            {
                projectArgument,
                configurationOption,
                environmentOption,
                outputOption,
                runtimeOption,
                selfContainedOption
            };

            publishCommand.SetHandler(async (project, configuration, environment, output, runtime, selfContained) =>
            {
                var command = new PublishCommandImplementation();
                Environment.ExitCode = await command.ExecuteAsync(project, configuration, environment, output, runtime, selfContained);
            }, projectArgument, configurationOption, environmentOption, outputOption, runtimeOption, selfContainedOption);

            return publishCommand;
        }
    }

    public class PublishCommandImplementation : DotnetCommandBase
    {
        public async Task<int> ExecuteAsync(string project, string configuration, string environment, string output, string runtime, bool selfContained)
        {
            return await ExecuteDotnetCommandAsync(async () =>
            {
                WriteProcessing($"📦 Publishing .NET application for {environment ?? "default"} environment...");
                
                var projectPath = project ?? ".";
                var args = $"publish \"{projectPath}\" --configuration {configuration}";
                
                if (!string.IsNullOrEmpty(output))
                    args += $" --output \"{output}\"";
                    
                if (!string.IsNullOrEmpty(runtime))
                    args += $" --runtime {runtime}";
                    
                if (selfContained)
                    args += " --self-contained true";
                
                if (!string.IsNullOrEmpty(environment))
                {
                    args += $" /p:PublishEnvironment={environment}";
                    await GenerateEnvironmentConfigAsync(environment, $"{output ?? "publish"}/config");
                }
                
                var result = await RunDotnetCommandAsync(args, null, true);
                
                if (result.Success)
                {
                    WriteSuccess($"✅ Published successfully to {output ?? "default location"}");
                    if (!string.IsNullOrEmpty(environment))
                    {
                        WriteInfo($"Environment-specific configuration included: {environment}");
                    }
                }
                
                return result.Success ? 0 : 1;
            }, "Publish");
        }
    }
} 