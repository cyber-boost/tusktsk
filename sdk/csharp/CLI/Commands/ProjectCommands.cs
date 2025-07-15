using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using TuskLang.CLI;
using System.Collections.Generic;
using System.Collections;

namespace TuskLang.CLI.Commands
{
    /// <summary>
    /// Project management commands for TuskLang CLI
    /// Implements: tsk init, tsk migrate
    /// </summary>
    public static class ProjectCommands
    {
        public static void AddCommands(RootCommand rootCommand)
        {
            // tsk init [project-name]
            var initCommand = new Command("init", "Initialize new TuskLang project")
            {
                new Argument<string>("project-name", () => "tusklang-project", "Name of the project to create")
            };
            initCommand.Handler = CommandHandler.Create<string>(InitProject);
            rootCommand.AddCommand(initCommand);

            // tsk migrate --from=<format>
            var migrateCommand = new Command("migrate", "Migrate from other configuration formats")
            {
                new Option<string>("--from", "Source format (json|yaml|ini|env)", ArgumentArity.ExactlyOne)
            };
            migrateCommand.Handler = CommandHandler.Create<string>(MigrateProject);
            rootCommand.AddCommand(migrateCommand);
        }

        private static async Task<int> InitProject(string projectName)
        {
            try
            {
                GlobalOptions.WriteProcessing($"Initializing TuskLang project: {projectName}");

                // Create project directory
                var projectDir = Path.Combine(Directory.GetCurrentDirectory(), projectName);
                if (Directory.Exists(projectDir))
                {
                    GlobalOptions.WriteError($"Project directory '{projectName}' already exists");
                    return 1;
                }

                Directory.CreateDirectory(projectDir);

                // Create basic project structure
                var srcDir = Path.Combine(projectDir, "src");
                var testsDir = Path.Combine(projectDir, "tests");
                var configDir = Path.Combine(projectDir, "config");

                Directory.CreateDirectory(srcDir);
                Directory.CreateDirectory(testsDir);
                Directory.CreateDirectory(configDir);

                // Create default configuration file
                var configContent = @"[app]
name: """ + projectName + @"""
version: ""1.0.0""
description: ""TuskLang project""

[server]
host: ""localhost""
port: 8080
debug: true

[database]
driver: ""sqlite""
path: ""app.db""

[logging]
level: ""info""
format: ""json""
";

                await File.WriteAllTextAsync(Path.Combine(projectDir, "peanu.peanuts"), configContent);

                // Create .gitignore
                var gitignoreContent = @"
# TuskLang
.tsk/
*.pnt
*.shell

# .NET
bin/
obj/
*.user
*.suo
*.cache

# IDE
.vs/
.vscode/
*.swp
*.swo

# OS
.DS_Store
Thumbs.db

# Logs
*.log
logs/

# Database
*.db
*.sqlite
*.sqlite3

# Environment
.env
.env.local
";

                await File.WriteAllTextAsync(Path.Combine(projectDir, ".gitignore"), gitignoreContent);

                // Create README
                var readmeContent = @"# " + projectName + @"

A TuskLang project.

## Quick Start

```bash
# Initialize the project
tsk init

# Start development server
tsk serve

# Run tests
tsk test all

# Build for production
tsk compile src/main.tsk
```

## Configuration

This project uses TuskLang's hierarchical configuration system. See `peanu.peanuts` for the main configuration file.

## Development

- `src/` - Source code
- `tests/` - Test files
- `config/` - Configuration files
- `peanu.peanuts` - Main configuration

## Commands

- `tsk serve` - Start development server
- `tsk test all` - Run all tests
- `tsk config get <key>` - Get configuration value
- `tsk db init` - Initialize database
- `tsk db migrate` - Run database migrations
";

                await File.WriteAllTextAsync(Path.Combine(projectDir, "README.md"), readmeContent);

                // Create sample TSK file
                var sampleTskContent = @"// Sample TuskLang configuration
app {
    name: ""Sample App""
    version: ""1.0.0""
}

server {
    host: ""localhost""
    port: 3000
    ssl {
        enabled: false
    }
}

database {
    driver: ""sqlite""
    path: ""app.db""
    pool_size: 10
}
";

                await File.WriteAllTextAsync(Path.Combine(projectDir, "src", "main.tsk"), sampleTskContent);

                GlobalOptions.WriteSuccess($"Project '{projectName}' initialized successfully!");
                GlobalOptions.WriteLine($"Project structure created in: {projectDir}");
                GlobalOptions.WriteLine("");
                GlobalOptions.WriteLine("Next steps:");
                GlobalOptions.WriteLine("  cd " + projectName);
                GlobalOptions.WriteLine("  tsk serve");
                GlobalOptions.WriteLine("  tsk test all");

                return 0;
            }
            catch (Exception ex)
            {
                GlobalOptions.WriteError($"Failed to initialize project: {ex.Message}");
                if (GlobalOptions.Verbose)
                {
                    Console.Error.WriteLine(ex.StackTrace);
                }
                return 1;
            }
        }

        private static async Task<int> MigrateProject(string fromFormat)
        {
            try
            {
                GlobalOptions.WriteProcessing($"Migrating from {fromFormat} format");

                if (string.IsNullOrEmpty(fromFormat))
                {
                    GlobalOptions.WriteError("Please specify source format with --from option");
                    GlobalOptions.WriteLine("Supported formats: json, yaml, ini, env");
                    return 2;
                }

                var supportedFormats = new[] { "json", "yaml", "ini", "env" };
                if (!Array.Exists(supportedFormats, f => f.Equals(fromFormat, StringComparison.OrdinalIgnoreCase)))
                {
                    GlobalOptions.WriteError($"Unsupported format: {fromFormat}");
                    GlobalOptions.WriteLine("Supported formats: json, yaml, ini, env");
                    return 2;
                }

                // Look for source files
                var sourceFiles = new List<string>();
                var extensions = fromFormat.ToLower() switch
                {
                    "json" => new[] { "*.json", "appsettings.json", "config.json" },
                    "yaml" => new[] { "*.yml", "*.yaml", "config.yml", "config.yaml" },
                    "ini" => new[] { "*.ini", "config.ini" },
                    "env" => new[] { ".env", ".env.local", ".env.production" },
                    _ => new[] { $"*.{fromFormat}" }
                };

                foreach (var pattern in extensions)
                {
                    sourceFiles.AddRange(Directory.GetFiles(Directory.GetCurrentDirectory(), pattern));
                }

                if (sourceFiles.Count == 0)
                {
                    GlobalOptions.WriteError($"No {fromFormat} files found in current directory");
                    GlobalOptions.WriteLine("Please ensure source files are in the current directory");
                    return 3;
                }

                GlobalOptions.WriteLine($"Found {sourceFiles.Count} source file(s):");
                foreach (var file in sourceFiles)
                {
                    GlobalOptions.WriteLine($"  - {Path.GetFileName(file)}");
                }

                // Convert each file
                foreach (var sourceFile in sourceFiles)
                {
                    await ConvertFile(sourceFile, fromFormat);
                }

                GlobalOptions.WriteSuccess("Migration completed successfully!");
                GlobalOptions.WriteLine("Generated files:");
                GlobalOptions.WriteLine("  - peanu.peanuts (main configuration)");
                GlobalOptions.WriteLine("  - peanu.tsk (TuskLang format)");

                return 0;
            }
            catch (Exception ex)
            {
                GlobalOptions.WriteError($"Migration failed: {ex.Message}");
                if (GlobalOptions.Verbose)
                {
                    Console.Error.WriteLine(ex.StackTrace);
                }
                return 1;
            }
        }

        private static async Task ConvertFile(string sourceFile, string fromFormat)
        {
            GlobalOptions.WriteProcessing($"Converting {Path.GetFileName(sourceFile)}");

            try
            {
                var content = await File.ReadAllTextAsync(sourceFile);
                var config = fromFormat.ToLower() switch
                {
                    "json" => ConvertFromJson(content),
                    "yaml" => ConvertFromYaml(content),
                    "ini" => ConvertFromIni(content),
                    "env" => ConvertFromEnv(content),
                    _ => throw new NotSupportedException($"Format {fromFormat} not supported")
                };

                // Write as peanuts format
                var peanutsFile = "peanu.peanuts";
                await File.WriteAllTextAsync(peanutsFile, config);

                // Write as TSK format
                var tskFile = "peanu.tsk";
                var tskContent = ConvertToTsk(config);
                await File.WriteAllTextAsync(tskFile, tskContent);

                GlobalOptions.WriteSuccess($"Converted {Path.GetFileName(sourceFile)} to {peanutsFile} and {tskFile}");
            }
            catch (Exception ex)
            {
                GlobalOptions.WriteError($"Failed to convert {Path.GetFileName(sourceFile)}: {ex.Message}");
            }
        }

        private static string ConvertFromJson(string jsonContent)
        {
            // Simple JSON to peanuts conversion
            var config = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent);
            return ConvertDictionaryToPeanuts(config);
        }

        private static string ConvertFromYaml(string yamlContent)
        {
            // Simple YAML to peanuts conversion (basic implementation)
            var lines = yamlContent.Split('\n');
            var config = new Dictionary<string, object>();
            var currentSection = "";
            var indentStack = new Stack<int>();

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
                    continue;

                var indent = line.Length - line.TrimStart().Length;
                
                if (indent == 0 && trimmed.EndsWith(':'))
                {
                    currentSection = trimmed.TrimEnd(':');
                    config[currentSection] = new Dictionary<string, object>();
                }
                else if (indent > 0 && trimmed.Contains(':'))
                {
                    var parts = trimmed.Split(':', 2);
                    var key = parts[0].Trim();
                    var value = parts[1].Trim();
                    
                    if (config.ContainsKey(currentSection) && config[currentSection] is Dictionary<string, object> section)
                    {
                        section[key] = ParseValue(value);
                    }
                }
            }

            return ConvertDictionaryToPeanuts(config);
        }

        private static string ConvertFromIni(string iniContent)
        {
            var lines = iniContent.Split('\n');
            var config = new Dictionary<string, object>();
            var currentSection = "";

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(';') || trimmed.StartsWith('#'))
                    continue;

                if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
                {
                    currentSection = trimmed.Substring(1, trimmed.Length - 2);
                    config[currentSection] = new Dictionary<string, object>();
                }
                else if (trimmed.Contains('='))
                {
                    var parts = trimmed.Split('=', 2);
                    var key = parts[0].Trim();
                    var value = parts[1].Trim();
                    
                    if (config.ContainsKey(currentSection) && config[currentSection] is Dictionary<string, object> section)
                    {
                        section[key] = ParseValue(value);
                    }
                }
            }

            return ConvertDictionaryToPeanuts(config);
        }

        private static string ConvertFromEnv(string envContent)
        {
            var lines = envContent.Split('\n');
            var config = new Dictionary<string, object>();
            var appSection = new Dictionary<string, object>();

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
                    continue;

                if (trimmed.Contains('='))
                {
                    var parts = trimmed.Split('=', 2);
                    var key = parts[0].Trim();
                    var value = parts[1].Trim();
                    
                    // Remove quotes if present
                    if ((value.StartsWith('"') && value.EndsWith('"')) || 
                        (value.StartsWith('\'') && value.EndsWith('\'')))
                    {
                        value = value.Substring(1, value.Length - 2);
                    }

                    appSection[key] = ParseValue(value);
                }
            }

            config["app"] = appSection;
            return ConvertDictionaryToPeanuts(config);
        }

        private static object ParseValue(string value)
        {
            // Try to parse as different types
            if (int.TryParse(value, out var intValue))
                return intValue;
            if (double.TryParse(value, out var doubleValue))
                return doubleValue;
            if (bool.TryParse(value, out var boolValue))
                return boolValue;
            if (value.Equals("null", StringComparison.OrdinalIgnoreCase))
                return null;
            
            return value;
        }

        private static string ConvertDictionaryToPeanuts(Dictionary<string, object> config)
        {
            var result = new System.Text.StringBuilder();
            
            foreach (var kvp in config)
            {
                result.AppendLine($"[{kvp.Key}]");
                
                if (kvp.Value is Dictionary<string, object> section)
                {
                    foreach (var item in section)
                    {
                        var value = item.Value?.ToString() ?? "null";
                        if (value is string strValue && !strValue.StartsWith('"'))
                        {
                            value = $"\"{value}\"";
                        }
                        result.AppendLine($"{item.Key}: {value}");
                    }
                }
                else
                {
                    var value = kvp.Value?.ToString() ?? "null";
                    if (value is string strValue && !strValue.StartsWith('"'))
                    {
                        value = $"\"{value}\"";
                    }
                    result.AppendLine($"{kvp.Key}: {value}");
                }
                
                result.AppendLine();
            }
            
            return result.ToString().TrimEnd();
        }

        private static string ConvertToTsk(string peanutsContent)
        {
            // Convert peanuts format to TSK format
            var lines = peanutsContent.Split('\n');
            var result = new System.Text.StringBuilder();
            var currentSection = "";

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed))
                    continue;

                if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
                {
                    currentSection = trimmed.Substring(1, trimmed.Length - 2);
                    result.AppendLine($"{currentSection} {{");
                }
                else if (trimmed.Contains(':'))
                {
                    var parts = trimmed.Split(':', 2);
                    var key = parts[0].Trim();
                    var value = parts[1].Trim();
                    result.AppendLine($"    {key}: {value}");
                }
                else if (trimmed == "}")
                {
                    result.AppendLine("}");
                }
            }

            return result.ToString().TrimEnd();
        }
    }
} 