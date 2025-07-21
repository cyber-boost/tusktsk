using System;
using System.CommandLine;
using System.Threading.Tasks;

namespace TuskLang.CLI.Commands.TuskDotnet
{
    /// <summary>
    /// Add command implementation - Add NuGet packages with TuskLang configuration
    /// </summary>
    public static class AddCommand
    {
        public static Command CreateAddCommand()
        {
            var packageArgument = new Argument<string>("package", "NuGet package to add");
            var versionOption = new Option<string>("--version", "Package version");
            var projectOption = new Option<string>("--project", "Project to add package to");

            var addCommand = new Command("add", "Add NuGet packages with TuskLang configuration")
            {
                packageArgument,
                versionOption,
                projectOption
            };

            addCommand.SetHandler(async (package, version, project) =>
            {
                var command = new AddCommandImplementation();
                Environment.ExitCode = await command.ExecuteAsync(package, version, project);
            }, packageArgument, versionOption, projectOption);

            return addCommand;
        }
    }

    public class AddCommandImplementation : DotnetCommandBase
    {
        public async Task<int> ExecuteAsync(string package, string version, string project)
        {
            return await ExecuteDotnetCommandAsync(async () =>
            {
                WriteProcessing($"Adding NuGet package: {package}");
                
                var projectPath = FindProjectFile(project);
                if (projectPath == null) return 1;

                var versionArg = !string.IsNullOrEmpty(version) ? $"--version {version}" : "";
                var args = $"add \"{projectPath}\" package {package} {versionArg}";
                
                var result = await RunDotnetCommandAsync(args);
                
                if (result.Success)
                {
                    WriteSuccess($"✅ Package {package} added successfully");
                    await AddTuskLangIntegrationAsync(projectPath, "");
                }
                
                return result.Success ? 0 : 1;
            }, "Add Package");
        }
    }
} 