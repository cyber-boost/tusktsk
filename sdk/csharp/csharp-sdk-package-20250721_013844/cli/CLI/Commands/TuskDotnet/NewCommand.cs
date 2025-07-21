using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using TuskLang;

namespace TuskLang.CLI.Commands.TuskDotnet
{
    /// <summary>
    /// New command implementation - Create .NET projects with TuskLang integration
    /// Provides comprehensive project scaffolding with multiple templates
    /// </summary>
    public static class NewCommand
    {
        public static Command CreateNewCommand()
        {
            // Arguments
            var templateArgument = new Argument<string>(
                name: "template",
                description: "Project template: console, webapi, mvc, blazor, worker, classlib, xunit, nunit")
            {
                AllowedValues = { "console", "webapi", "mvc", "blazor", "worker", "classlib", "xunit", "nunit" }
            };

            // Options
            var nameOption = new Option<string>(
                aliases: new[] { "--name", "-n" },
                description: "Name of the project");

            var outputOption = new Option<string>(
                aliases: new[] { "--output", "-o" },
                description: "Output directory for the project");

            var frameworkOption = new Option<string>(
                aliases: new[] { "--framework", "-f" },
                description: "Target framework (net6.0, net7.0, net8.0, net9.0)");

            var langVersionOption = new Option<string>(
                aliases: new[] { "--langVersion" },
                getDefaultValue: () => "latest",
                description: "C# language version");

            var includeTestsOption = new Option<bool>(
                aliases: new[] { "--include-tests" },
                description: "Include test project");

            var dockerOption = new Option<bool>(
                aliases: new[] { "--docker" },
                description: "Add Docker support");

            var tuskConfigOption = new Option<string>(
                aliases: new[] { "--tusk-config" },
                getDefaultValue: () => "peanu.tsk",
                description: "TuskLang configuration file name");

            // Create command
            var newCommand = new Command("new", "Create .NET projects with TuskLang integration")
            {
                templateArgument,
                nameOption,
                outputOption,
                frameworkOption,
                langVersionOption,
                includeTestsOption,
                dockerOption,
                tuskConfigOption
            };

            newCommand.SetHandler(async (template, name, output, framework, langVersion, includeTests, docker, tuskConfig) =>
            {
                var command = new NewCommandImplementation();
                Environment.ExitCode = await command.ExecuteAsync(
                    template, name, output, framework, langVersion, includeTests, docker, tuskConfig);
            }, templateArgument, nameOption, outputOption, frameworkOption, langVersionOption, includeTestsOption, dockerOption, tuskConfigOption);

            return newCommand;
        }
    }

    /// <summary>
    /// New command implementation with comprehensive .NET project creation
    /// </summary>
    public class NewCommandImplementation : DotnetCommandBase
    {
        public async Task<int> ExecuteAsync(
            string template,
            string name,
            string output,
            string framework,
            string langVersion,
            bool includeTests,
            bool docker,
            string tuskConfig)
        {
            return await ExecuteDotnetCommandAsync(async () =>
            {
                if (string.IsNullOrEmpty(name))
                {
                    name = Path.GetFileName(Directory.GetCurrentDirectory());
                    WriteInfo($"Using current directory name: {name}");
                }

                var projectDir = !string.IsNullOrEmpty(output) 
                    ? Path.GetFullPath(output)
                    : Path.Combine(Directory.GetCurrentDirectory(), name);

                WriteProcessing($"Creating {template} project '{name}' with TuskLang integration...");

                // Create project directory
                Directory.CreateDirectory(projectDir);

                // Create the .NET project
                var projectResult = await CreateDotnetProjectAsync(template, name, projectDir, framework, langVersion);
                if (!projectResult)
                    return 1;

                // Add TuskLang integration
                var integrationResult = await AddTuskLangIntegrationToProjectAsync(projectDir, name, tuskConfig);
                if (!integrationResult)
                    return 1;

                // Create TuskLang configuration
                await CreateTuskLangConfigurationAsync(projectDir, template, tuskConfig);

                // Add additional features
                if (includeTests)
                {
                    await CreateTestProjectAsync(projectDir, name, framework);
                }

                if (docker)
                {
                    await CreateDockerFilesAsync(projectDir, template);
                }

                // Create development environment files
                await CreateDevelopmentFilesAsync(projectDir, template);

                WriteSuccess($"✅ Project '{name}' created successfully!");
                WriteInfo($"📁 Location: {projectDir}");
                WriteInfo("Next steps:");
                WriteInfo("  1. cd " + Path.GetFileName(projectDir));
                WriteInfo("  2. tusk-dotnet restore");
                WriteInfo("  3. tusk-dotnet build");
                WriteInfo("  4. tusk-dotnet run");

                return 0;
            }, "New Project");
        }

        private async Task<bool> CreateDotnetProjectAsync(string template, string name, string projectDir, string framework, string langVersion)
        {
            try
            {
                var dotnetTemplate = GetDotnetTemplateName(template);
                var targetFramework = framework ?? DotnetGlobalOptions.GetTargetFramework();

                var arguments = $"new {dotnetTemplate} -n {name} -o \"{projectDir}\" --framework {targetFramework} --langVersion {langVersion}";

                DotnetGlobalOptions.WriteInfo($"Running: dotnet {arguments}");
                var result = await RunDotnetCommandAsync(arguments, null, true);

                if (!result.Success)
                {
                    WriteError($"Failed to create .NET project: {result.Error}");
                    return false;
                }

                WriteSuccess($".NET {template} project created");
                return true;
            }
            catch (Exception ex)
            {
                WriteError($"Error creating .NET project: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> AddTuskLangIntegrationToProjectAsync(string projectDir, string projectName, string configFile)
        {
            try
            {
                var projectFiles = Directory.GetFiles(projectDir, "*.csproj");
                if (projectFiles.Length == 0)
                {
                    WriteError("No project file found");
                    return false;
                }

                var projectFile = projectFiles[0];
                WriteInfo($"Adding TuskLang integration to: {Path.GetFileName(projectFile)}");

                // Read and modify project file
                var projectInfo = await ReadProjectFileAsync(projectFile);
                if (projectInfo == null)
                    return false;

                // Add TuskLang packages
                projectInfo.PackageReferences["TuskLang"] = "2.0.1";
                projectInfo.PackageReferences["TuskLang.Extensions"] = "2.0.1";

                // Add ASP.NET Core integration for web projects
                if (projectInfo.ProjectType == DotnetProjectType.WebApi || projectInfo.ProjectType == DotnetProjectType.Mvc)
                {
                    projectInfo.PackageReferences["TuskLang.AspNetCore"] = "2.0.1";
                }

                // Add project properties
                projectInfo.Properties["EnableTuskLangIntegration"] = "true";
                projectInfo.Properties["TuskLangConfigFile"] = configFile;
                projectInfo.Properties["GenerateAssemblyConfigurationAttribute"] = "false";

                return await WriteProjectFileAsync(projectInfo, true);
            }
            catch (Exception ex)
            {
                WriteError($"Failed to add TuskLang integration: {ex.Message}");
                return false;
            }
        }

        private async Task CreateTuskLangConfigurationAsync(string projectDir, string template, string configFile)
        {
            try
            {
                var configPath = Path.Combine(projectDir, configFile);
                var config = CreateTemplateConfiguration(template);

                var tsk = new TSK(config);
                var content = tsk.ToString();

                await SaveFileAtomicAsync(configPath, content, "TuskLang configuration");
                
                // Also create environment-specific configs
                var configDir = Path.Combine(projectDir, "config");
                Directory.CreateDirectory(configDir);

                var environments = new[] { "Development", "Staging", "Production" };
                foreach (var env in environments)
                {
                    var envConfig = CreateEnvironmentConfiguration(template, env);
                    var envTsk = new TSK(envConfig);
                    var envConfigPath = Path.Combine(configDir, $"{env.ToLower()}.tsk");
                    await SaveFileAtomicAsync(envConfigPath, envTsk.ToString(), $"{env} configuration");
                }
            }
            catch (Exception ex)
            {
                WriteWarning($"Failed to create TuskLang configuration: {ex.Message}");
            }
        }

        private async Task CreateTestProjectAsync(string baseDir, string projectName, string framework)
        {
            try
            {
                var testProjectName = $"{projectName}.Tests";
                var testDir = Path.Combine(Path.GetDirectoryName(baseDir), testProjectName);

                WriteInfo("Creating test project...");
                
                var targetFramework = framework ?? DotnetGlobalOptions.GetTargetFramework();
                var arguments = $"new xunit -n {testProjectName} -o \"{testDir}\" --framework {targetFramework}";
                
                var result = await RunDotnetCommandAsync(arguments, null, false);
                if (!result.Success)
                {
                    WriteWarning("Failed to create test project");
                    return;
                }

                // Add reference to main project
                var mainProjectPath = Directory.GetFiles(baseDir, "*.csproj")[0];
                var addRefArgs = $"add \"{testDir}\" reference \"{mainProjectPath}\"";
                await RunDotnetCommandAsync(addRefArgs, null, false);

                WriteSuccess("Test project created");
            }
            catch (Exception ex)
            {
                WriteWarning($"Failed to create test project: {ex.Message}");
            }
        }

        private async Task CreateDockerFilesAsync(string projectDir, string template)
        {
            try
            {
                WriteInfo("Creating Docker files...");

                var dockerfile = CreateDockerfileContent(template);
                await SaveFileAtomicAsync(Path.Combine(projectDir, "Dockerfile"), dockerfile, "Dockerfile");

                var dockerIgnore = CreateDockerIgnoreContent();
                await SaveFileAtomicAsync(Path.Combine(projectDir, ".dockerignore"), dockerIgnore, ".dockerignore");

                var dockerCompose = CreateDockerComposeContent(Path.GetFileName(projectDir), template);
                await SaveFileAtomicAsync(Path.Combine(projectDir, "docker-compose.yml"), dockerCompose, "docker-compose");

                WriteSuccess("Docker files created");
            }
            catch (Exception ex)
            {
                WriteWarning($"Failed to create Docker files: {ex.Message}");
            }
        }

        private async Task CreateDevelopmentFilesAsync(string projectDir, string template)
        {
            try
            {
                // Create .gitignore
                var gitignore = CreateGitIgnoreContent();
                await SaveFileAtomicAsync(Path.Combine(projectDir, ".gitignore"), gitignore, ".gitignore");

                // Create README
                var readme = CreateReadmeContent(Path.GetFileName(projectDir), template);
                await SaveFileAtomicAsync(Path.Combine(projectDir, "README.md"), readme, "README");

                // Create VS Code settings
                var vscodeDir = Path.Combine(projectDir, ".vscode");
                Directory.CreateDirectory(vscodeDir);

                var launchJson = CreateLaunchJsonContent(template);
                await SaveFileAtomicAsync(Path.Combine(vscodeDir, "launch.json"), launchJson, "VS Code launch.json");

                var tasksJson = CreateTasksJsonContent();
                await SaveFileAtomicAsync(Path.Combine(vscodeDir, "tasks.json"), tasksJson, "VS Code tasks.json");

                WriteSuccess("Development files created");
            }
            catch (Exception ex)
            {
                WriteWarning($"Failed to create development files: {ex.Message}");
            }
        }

        // Template configuration generators
        private Dictionary<string, object> CreateTemplateConfiguration(string template)
        {
            var config = new Dictionary<string, object>
            {
                ["project"] = new Dictionary<string, object>
                {
                    ["name"] = "${PROJECT_NAME}",
                    ["version"] = "1.0.0",
                    ["description"] = $".NET {template} project with TuskLang integration",
                    ["template"] = template
                },
                ["application"] = new Dictionary<string, object>
                {
                    ["name"] = "${PROJECT_NAME}",
                    ["environment"] = "${ASPNETCORE_ENVIRONMENT}",
                    ["debug"] = "${DEBUG}"
                }
            };

            // Template-specific configuration
            switch (template.ToLower())
            {
                case "webapi":
                case "mvc":
                    config["server"] = new Dictionary<string, object>
                    {
                        ["urls"] = "${ASPNETCORE_URLS}",
                        ["port"] = "${PORT}",
                        ["https_port"] = "${HTTPS_PORT}"
                    };
                    config["cors"] = new Dictionary<string, object>
                    {
                        ["enabled"] = true,
                        ["origins"] = new[] { "http://localhost:3000", "https://localhost:3001" }
                    };
                    break;

                case "worker":
                    config["worker"] = new Dictionary<string, object>
                    {
                        ["interval"] = "${WORKER_INTERVAL}",
                        ["enabled"] = "${WORKER_ENABLED}"
                    };
                    break;
            }

            config["logging"] = new Dictionary<string, object>
            {
                ["level"] = "${LOG_LEVEL}",
                ["console"] = true,
                ["file"] = "${LOG_TO_FILE}"
            };

            return config;
        }

        private Dictionary<string, object> CreateEnvironmentConfiguration(string template, string environment)
        {
            var config = new Dictionary<string, object>
            {
                ["project"] = new Dictionary<string, object>
                {
                    ["environment"] = environment
                }
            };

            switch (environment.ToLower())
            {
                case "development":
                    config["application"] = new Dictionary<string, object>
                    {
                        ["debug"] = true
                    };
                    config["logging"] = new Dictionary<string, object>
                    {
                        ["level"] = "Debug"
                    };
                    if (template == "webapi" || template == "mvc")
                    {
                        config["server"] = new Dictionary<string, object>
                        {
                            ["urls"] = "https://localhost:7001;http://localhost:5001"
                        };
                    }
                    break;

                case "production":
                    config["application"] = new Dictionary<string, object>
                    {
                        ["debug"] = false
                    };
                    config["logging"] = new Dictionary<string, object>
                    {
                        ["level"] = "Warning"
                    };
                    if (template == "webapi" || template == "mvc")
                    {
                        config["server"] = new Dictionary<string, object>
                        {
                            ["urls"] = "https://*:443;http://*:80"
                        };
                    }
                    break;
            }

            return config;
        }

        private string GetDotnetTemplateName(string template)
        {
            return template.ToLower() switch
            {
                "console" => "console",
                "webapi" => "webapi",
                "mvc" => "mvc",
                "blazor" => "blazorwasm",
                "worker" => "worker",
                "classlib" => "classlib",
                "xunit" => "xunit",
                "nunit" => "nunit",
                _ => "console"
            };
        }

        // Content generators for various files
        private string CreateDockerfileContent(string template)
        {
            var baseImage = template == "blazor" ? "nginx:alpine" : "mcr.microsoft.com/dotnet/aspnet:8.0";
            var sdkImage = "mcr.microsoft.com/dotnet/sdk:8.0";
            
            if (template == "blazor")
            {
                return @"FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY [""*.csproj"", "".""]
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM nginx:alpine
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
COPY nginx.conf /etc/nginx/nginx.conf
";
            }

            return $@"FROM {sdkImage} AS build
WORKDIR /src
COPY [""*.csproj"", "".""]
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM {baseImage} AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT [""dotnet"", ""{{assemblyname}}.dll""]
";
        }

        private string CreateDockerIgnoreContent()
        {
            return @"**/.dockerignore
**/.env
**/.git
**/.gitignore
**/.vs
**/.vscode
**/*.*proj.user
**/bin
**/obj
**/out
**/TestResults
";
        }

        private string CreateDockerComposeContent(string projectName, string template)
        {
            var port = template switch
            {
                "webapi" or "mvc" => "8080:80",
                "blazor" => "8080:80",
                _ => "8080:80"
            };

            return $@"version: '3.8'

services:
  {projectName.ToLower()}:
    image: {projectName.ToLower()}
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - ""{port}""
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:80
    volumes:
      - ./peanu.tsk:/app/peanu.tsk:ro
      - ./config:/app/config:ro
";
        }

        private string CreateGitIgnoreContent()
        {
            return @"## Ignore Visual Studio temporary files, build results, and
## files generated by popular Visual Studio add-ons.
##
## Get latest from https://github.com/github/gitignore/blob/master/VisualStudio.gitignore

# User-specific files
*.rsuser
*.suo
*.user
*.userosscache
*.sln.docstates

# User-specific files (MonoDevelop/Xamarin Studio)
*.userprefs

# Mono auto generated files
mono_crash.*

# Build results
[Dd]ebug/
[Dd]ebugPublic/
[Rr]elease/
[Rr]eleases/
x64/
x86/
[Ww][Ii][Nn]32/
[Aa][Rr][Mm]/
[Aa][Rr][Mm]64/
bld/
[Bb]in/
[Oo]bj/
[Ll]og/
[Ll]ogs/

# Visual Studio 2015/2017 cache/options directory
.vs/

# TuskLang compiled files
*.pnt

# Environment files
.env
.env.local
.env.development
.env.staging
.env.production
";
        }

        private string CreateReadmeContent(string projectName, string template)
        {
            return $@"# {projectName}

{GetProjectDescription(template)}

## Quick Start

1. Restore dependencies:
   ```bash
   tusk-dotnet restore
   ```

2. Build the project:
   ```bash
   tusk-dotnet build
   ```

3. Run the application:
   ```bash
   tusk-dotnet run
   ```

## TuskLang Integration

This project uses TuskLang for configuration management:

- `peanu.tsk` - Main configuration file
- `config/` - Environment-specific configurations
  - `development.tsk` - Development settings
  - `staging.tsk` - Staging settings
  - `production.tsk` - Production settings

## Configuration

The application automatically loads configuration from TuskLang files based on the current environment:

```bash
# Development
ASPNETCORE_ENVIRONMENT=Development tusk-dotnet run

# Production
ASPNETCORE_ENVIRONMENT=Production tusk-dotnet run
```

## Docker Support

Build and run with Docker:

```bash
docker-compose up --build
```

## Development

- **IDE**: Visual Studio, Visual Studio Code, or JetBrains Rider
- **Framework**: .NET 8.0
- **Configuration**: TuskLang (.tsk files)
- **Testing**: xUnit (if test project included)

## Project Structure

```
{projectName}/
├── peanu.tsk              # Main TuskLang configuration
├── config/                # Environment configurations
│   ├── development.tsk
│   ├── staging.tsk
│   └── production.tsk
├── {GetMainFile(template)}
└── {projectName}.csproj   # Project file with TuskLang integration
```

## Commands

All standard .NET CLI commands work, plus TuskLang integration:

- `tusk-dotnet build` - Build with TuskLang processing
- `tusk-dotnet run` - Run with TuskLang configuration
- `tusk-dotnet test` - Run tests with TuskLang config
- `tusk-dotnet publish` - Publish with environment configs

## License

This project is licensed under [Your License].
";
        }

        private string CreateLaunchJsonContent(string template)
        {
            var program = template switch
            {
                "webapi" or "mvc" => "${workspaceFolder}/bin/Debug/net8.0/${workspaceFolderBasename}.dll",
                "console" => "${workspaceFolder}/bin/Debug/net8.0/${workspaceFolderBasename}.dll", 
                "worker" => "${workspaceFolder}/bin/Debug/net8.0/${workspaceFolderBasename}.dll",
                _ => "${workspaceFolder}/bin/Debug/net8.0/${workspaceFolderBasename}.dll"
            };

            return $@"{{
    ""version"": ""0.2.0"",
    ""configurations"": [
        {{
            ""name"": "".NET Core Launch (console)"",
            ""type"": ""coreclr"",
            ""request"": ""launch"",
            ""preLaunchTask"": ""build"",
            ""program"": ""{program}"",
            ""args"": [],
            ""cwd"": ""${{workspaceFolder}}"",
            ""console"": ""internalConsole"",
            ""stopAtEntry"": false,
            ""env"": {{
                ""ASPNETCORE_ENVIRONMENT"": ""Development"",
                ""TUSKLANG_CONFIG"": ""peanu.tsk""
            }}
        }},
        {{
            ""name"": "".NET Core Attach"",
            ""type"": ""coreclr"",
            ""request"": ""attach""
        }}
    ]
}}";
        }

        private string CreateTasksJsonContent()
        {
            return @"{
    ""version"": ""2.0.0"",
    ""tasks"": [
        {
            ""label"": ""build"",
            ""command"": ""dotnet"",
            ""type"": ""process"",
            ""args"": [
                ""build"",
                ""${workspaceFolder}""
            ],
            ""problemMatcher"": ""$msCompile""
        },
        {
            ""label"": ""publish"",
            ""command"": ""dotnet"",
            ""type"": ""process"",
            ""args"": [
                ""publish"",
                ""${workspaceFolder}"",
                ""/property:GenerateFullPaths=true"",
                ""/consoleloggerparameters:NoSummary""
            ],
            ""problemMatcher"": ""$msCompile""
        },
        {
            ""label"": ""watch"",
            ""command"": ""dotnet"",
            ""type"": ""process"",
            ""args"": [
                ""watch"",
                ""run"",
                ""${workspaceFolder}""
            ],
            ""problemMatcher"": ""$msCompile""
        },
        {
            ""label"": ""tusk-build"",
            ""command"": ""tusk-dotnet"",
            ""type"": ""shell"",
            ""args"": [
                ""build""
            ],
            ""group"": ""build"",
            ""presentation"": {
                ""echo"": true,
                ""reveal"": ""always"",
                ""focus"": false,
                ""panel"": ""shared""
            },
            ""problemMatcher"": ""$msCompile""
        }
    ]
}";
        }

        private string GetProjectDescription(string template)
        {
            return template.ToLower() switch
            {
                "console" => "A console application with TuskLang configuration integration.",
                "webapi" => "A Web API application with TuskLang configuration and ASP.NET Core integration.", 
                "mvc" => "An MVC web application with TuskLang configuration and ASP.NET Core integration.",
                "blazor" => "A Blazor WebAssembly application with TuskLang configuration integration.",
                "worker" => "A background worker service with TuskLang configuration integration.",
                "classlib" => "A class library with TuskLang integration for configuration management.",
                _ => "A .NET application with TuskLang configuration integration."
            };
        }

        private string GetMainFile(string template)
        {
            return template.ToLower() switch
            {
                "webapi" => "Controllers/",
                "mvc" => "Controllers/ and Views/",
                "blazor" => "wwwroot/ and Pages/",
                "worker" => "Worker.cs",
                _ => "Program.cs"
            };
        }
    }
} 