using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Threading.Tasks;

namespace TuskLang.CLI.Commands.Tusk
{
    /// <summary>
    /// Help command implementation - Comprehensive help system
    /// Provides detailed help for all commands with examples and usage
    /// </summary>
    public static class HelpCommand
    {
        public static Command CreateHelpCommand()
        {
            // Arguments
            var commandArgument = new Argument<string>(
                name: "command",
                description: "Command to get help for")
            {
                Arity = ArgumentArity.ZeroOrOne
            };

            // Options
            var listOption = new Option<bool>(
                aliases: new[] { "--list", "-l" },
                description: "List all available commands");

            var examplesOption = new Option<bool>(
                aliases: new[] { "--examples", "-e" },
                description: "Show usage examples");

            var verboseOption = new Option<bool>(
                aliases: new[] { "--verbose", "-v" },
                description: "Show detailed help");

            // Create command
            var helpCommand = new Command("help", "Show help information for commands")
            {
                commandArgument,
                listOption,
                examplesOption,
                verboseOption
            };

            helpCommand.SetHandler(async (command, list, examples, verbose) =>
            {
                var helpCommandImpl = new HelpCommandImplementation();
                Environment.ExitCode = await helpCommandImpl.ExecuteAsync(command, list, examples, verbose);
            }, commandArgument, listOption, examplesOption, verboseOption);

            return helpCommand;
        }
    }

    /// <summary>
    /// Help command implementation with comprehensive documentation
    /// </summary>
    public class HelpCommandImplementation
    {
        private readonly Dictionary<string, CommandHelp> _commandHelp;

        public HelpCommandImplementation()
        {
            _commandHelp = BuildCommandHelpDatabase();
        }

        public async Task<int> ExecuteAsync(string command, bool list, bool examples, bool verbose)
        {
            try
            {
                if (list)
                {
                    ShowCommandList();
                }
                else if (string.IsNullOrEmpty(command))
                {
                    ShowGeneralHelp(examples, verbose);
                }
                else
                {
                    ShowCommandHelp(command, examples, verbose);
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"❌ Help system error: {ex.Message}");
                return 1;
            }
        }

        private void ShowGeneralHelp(bool examples, bool verbose)
        {
            Console.WriteLine("🚀 TuskLang CLI v2.0.1 - Configuration Management Tool");
            Console.WriteLine();
            Console.WriteLine("USAGE:");
            Console.WriteLine("  tusk <command> [options] [arguments]");
            Console.WriteLine();
            Console.WriteLine("DESCRIPTION:");
            Console.WriteLine("  TuskLang CLI provides comprehensive tools for managing configuration files");
            Console.WriteLine("  in the TuskLang format (.tsk). It supports parsing, validation, compilation,");
            Console.WriteLine("  project scaffolding, building, and automatic change detection.");
            Console.WriteLine();
            Console.WriteLine("COMMANDS:");

            foreach (var help in _commandHelp.Values.OrderBy(h => h.Name))
            {
                Console.WriteLine($"  {help.Name,-12} {help.Description}");
            }

            Console.WriteLine();
            Console.WriteLine("GLOBAL OPTIONS:");
            Console.WriteLine("  -v, --verbose     Enable verbose output");
            Console.WriteLine("  -q, --quiet       Suppress output except errors");
            Console.WriteLine("  -j, --json        Output results in JSON format");
            Console.WriteLine("  -c, --config      Path to configuration file");
            Console.WriteLine("  -h, --help        Show help information");
            Console.WriteLine();
            Console.WriteLine("EXAMPLES:");
            Console.WriteLine("  tusk parse config.tsk                    # Parse and analyze configuration");
            Console.WriteLine("  tusk validate config.tsk --strict       # Strict validation");
            Console.WriteLine("  tusk compile config.tsk -o output.pnt   # Compile to binary format");
            Console.WriteLine("  tusk init my-project --template web     # Create web project");
            Console.WriteLine("  tusk build --environment production     # Build for production");
            Console.WriteLine("  tusk watch config.tsk                   # Watch for changes");
            Console.WriteLine();
            Console.WriteLine("For detailed help on a command, use:");
            Console.WriteLine("  tusk help <command>");
            Console.WriteLine("  tusk <command> --help");

            if (verbose)
            {
                ShowVerboseGeneralHelp();
            }
        }

        private void ShowVerboseGeneralHelp()
        {
            Console.WriteLine();
            Console.WriteLine("CONFIGURATION FILES:");
            Console.WriteLine("  TuskLang uses .tsk files with TOML-like syntax supporting:");
            Console.WriteLine("  • Section-based configuration: [section]");
            Console.WriteLine("  • Key-value pairs: key = \"value\"");
            Console.WriteLine("  • Environment variables: ${VARIABLE}");
            Console.WriteLine("  • Functions (FUJSEN): function() { ... }");
            Console.WriteLine("  • Comments: # This is a comment");
            Console.WriteLine();
            Console.WriteLine("PROJECT STRUCTURE:");
            Console.WriteLine("  peanu.tsk            # Main configuration file");
            Console.WriteLine("  config/              # Environment-specific configurations");
            Console.WriteLine("    development.tsk    # Development settings");
            Console.WriteLine("    staging.tsk        # Staging settings");
            Console.WriteLine("    production.tsk     # Production settings");
            Console.WriteLine("  templates/           # Configuration templates");
            Console.WriteLine("  build/               # Build output directory");
            Console.WriteLine();
            Console.WriteLine("ENVIRONMENT VARIABLES:");
            Console.WriteLine("  TUSKLANG_CONFIG      # Default configuration file path");
            Console.WriteLine("  TUSKLANG_ENV         # Environment name (development, staging, production)");
            Console.WriteLine("  TUSKLANG_DEBUG       # Enable debug mode");
            Console.WriteLine("  TUSKLANG_VERBOSE     # Enable verbose output");
        }

        private void ShowCommandList()
        {
            Console.WriteLine("📋 Available Commands:");
            Console.WriteLine();

            var categories = new Dictionary<string, List<CommandHelp>>
            {
                ["Core Operations"] = new List<CommandHelp>(),
                ["Project Management"] = new List<CommandHelp>(),
                ["Development Tools"] = new List<CommandHelp>(),
                ["Information"] = new List<CommandHelp>()
            };

            foreach (var help in _commandHelp.Values)
            {
                var category = help.Category ?? "Core Operations";
                if (categories.ContainsKey(category))
                {
                    categories[category].Add(help);
                }
                else
                {
                    categories["Core Operations"].Add(help);
                }
            }

            foreach (var category in categories)
            {
                if (category.Value.Count == 0) continue;

                Console.WriteLine($"{category.Key}:");
                foreach (var help in category.Value.OrderBy(h => h.Name))
                {
                    Console.WriteLine($"  {help.Name,-12} {help.Description}");
                }
                Console.WriteLine();
            }
        }

        private void ShowCommandHelp(string commandName, bool examples, bool verbose)
        {
            if (!_commandHelp.TryGetValue(commandName.ToLower(), out var help))
            {
                Console.Error.WriteLine($"❌ Unknown command: {commandName}");
                Console.Error.WriteLine();
                Console.Error.WriteLine("Available commands:");
                foreach (var cmd in _commandHelp.Keys.OrderBy(k => k))
                {
                    Console.Error.WriteLine($"  {cmd}");
                }
                return;
            }

            Console.WriteLine($"🔧 {help.Name} - {help.Description}");
            Console.WriteLine();
            Console.WriteLine("USAGE:");
            Console.WriteLine($"  {help.Usage}");
            
            if (!string.IsNullOrEmpty(help.DetailedDescription))
            {
                Console.WriteLine();
                Console.WriteLine("DESCRIPTION:");
                Console.WriteLine($"  {help.DetailedDescription}");
            }

            if (help.Arguments.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("ARGUMENTS:");
                foreach (var arg in help.Arguments)
                {
                    Console.WriteLine($"  {arg.Name,-15} {arg.Description}");
                    if (verbose && !string.IsNullOrEmpty(arg.Details))
                    {
                        Console.WriteLine($"                  {arg.Details}");
                    }
                }
            }

            if (help.Options.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("OPTIONS:");
                foreach (var opt in help.Options)
                {
                    var aliases = string.Join(", ", opt.Aliases);
                    Console.WriteLine($"  {aliases,-15} {opt.Description}");
                    if (verbose && !string.IsNullOrEmpty(opt.Details))
                    {
                        Console.WriteLine($"                  {opt.Details}");
                    }
                }
            }

            if ((examples || verbose) && help.Examples.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("EXAMPLES:");
                foreach (var example in help.Examples)
                {
                    Console.WriteLine($"  {example.Command}");
                    if (!string.IsNullOrEmpty(example.Description))
                    {
                        Console.WriteLine($"    # {example.Description}");
                    }
                    Console.WriteLine();
                }
            }

            if (verbose)
            {
                ShowVerboseCommandHelp(help);
            }

            Console.WriteLine("For global options, use: tusk help");
        }

        private void ShowVerboseCommandHelp(CommandHelp help)
        {
            if (!string.IsNullOrEmpty(help.Notes))
            {
                Console.WriteLine("NOTES:");
                Console.WriteLine($"  {help.Notes}");
                Console.WriteLine();
            }

            if (help.RelatedCommands.Count > 0)
            {
                Console.WriteLine("RELATED COMMANDS:");
                foreach (var related in help.RelatedCommands)
                {
                    Console.WriteLine($"  tusk {related}");
                }
                Console.WriteLine();
            }

            if (help.ExitCodes.Count > 0)
            {
                Console.WriteLine("EXIT CODES:");
                foreach (var exit in help.ExitCodes)
                {
                    Console.WriteLine($"  {exit.Code,-3} {exit.Description}");
                }
                Console.WriteLine();
            }
        }

        private Dictionary<string, CommandHelp> BuildCommandHelpDatabase()
        {
            return new Dictionary<string, CommandHelp>
            {
                ["parse"] = new CommandHelp
                {
                    Name = "parse",
                    Category = "Core Operations",
                    Description = "Parse and analyze .tsk configuration files",
                    DetailedDescription = "Parses TuskLang configuration files, analyzes their structure, and provides detailed information about sections, properties, and potential issues.",
                    Usage = "tusk parse <file> [options]",
                    Arguments = new List<ArgumentHelp>
                    {
                        new ArgumentHelp { Name = "file", Description = "Path to the .tsk file to parse", Details = "Must be a valid TuskLang configuration file" }
                    },
                    Options = new List<OptionHelp>
                    {
                        new OptionHelp { Aliases = new[] { "-o", "--output" }, Description = "Output file for parsed results" },
                        new OptionHelp { Aliases = new[] { "-f", "--format" }, Description = "Output format: detailed, json, summary, tree" },
                        new OptionHelp { Aliases = new[] { "--validate" }, Description = "Include validation during parsing" },
                        new OptionHelp { Aliases = new[] { "-s", "--statistics" }, Description = "Include parsing statistics" },
                        new OptionHelp { Aliases = new[] { "--include-comments" }, Description = "Include comments in output" },
                        new OptionHelp { Aliases = new[] { "--include-metadata" }, Description = "Include file metadata" }
                    },
                    Examples = new List<ExampleHelp>
                    {
                        new ExampleHelp { Command = "tusk parse config.tsk", Description = "Parse configuration file" },
                        new ExampleHelp { Command = "tusk parse config.tsk --format json --statistics", Description = "Parse with JSON output and statistics" },
                        new ExampleHelp { Command = "tusk parse config.tsk -o analysis.txt --validate", Description = "Parse, validate, and save results" }
                    },
                    RelatedCommands = new List<string> { "validate", "compile" },
                    ExitCodes = new List<ExitCodeHelp>
                    {
                        new ExitCodeHelp { Code = 0, Description = "Success" },
                        new ExitCodeHelp { Code = 1, Description = "Parse error or file not found" }
                    }
                },

                ["compile"] = new CommandHelp
                {
                    Name = "compile",
                    Category = "Core Operations",
                    Description = "Compile .tsk files to optimized binary .pnt format",
                    DetailedDescription = "Compiles TuskLang configuration files to binary .pnt format with optimization, compression, and metadata inclusion for production use.",
                    Usage = "tusk compile <file> [options]",
                    Arguments = new List<ArgumentHelp>
                    {
                        new ArgumentHelp { Name = "file", Description = "Path to the .tsk file to compile" }
                    },
                    Options = new List<OptionHelp>
                    {
                        new OptionHelp { Aliases = new[] { "-o", "--output" }, Description = "Output path for compiled .pnt file" },
                        new OptionHelp { Aliases = new[] { "-O", "--optimize" }, Description = "Enable compilation optimizations" },
                        new OptionHelp { Aliases = new[] { "-c", "--compression" }, Description = "Compression: none, gzip, brotli" },
                        new OptionHelp { Aliases = new[] { "--validate" }, Description = "Validate before compiling" },
                        new OptionHelp { Aliases = new[] { "--include-metadata" }, Description = "Include metadata in compiled file" },
                        new OptionHelp { Aliases = new[] { "--debug-info" }, Description = "Include debug information" },
                        new OptionHelp { Aliases = new[] { "--checksum" }, Description = "Generate integrity checksums" }
                    },
                    Examples = new List<ExampleHelp>
                    {
                        new ExampleHelp { Command = "tusk compile config.tsk", Description = "Compile with default settings" },
                        new ExampleHelp { Command = "tusk compile config.tsk --compression brotli -o prod.pnt", Description = "Compile with Brotli compression" },
                        new ExampleHelp { Command = "tusk compile config.tsk --debug-info --no-optimize", Description = "Debug build with symbols" }
                    },
                    Notes = "Compiled .pnt files are significantly smaller and load faster than .tsk files",
                    RelatedCommands = new List<string> { "parse", "validate" }
                },

                ["validate"] = new CommandHelp
                {
                    Name = "validate",
                    Category = "Core Operations", 
                    Description = "Comprehensive validation with detailed analysis and reporting",
                    DetailedDescription = "Performs thorough validation of TuskLang files including syntax, schema compliance, references, best practices, performance, and security analysis.",
                    Usage = "tusk validate <file> [options]",
                    Arguments = new List<ArgumentHelp>
                    {
                        new ArgumentHelp { Name = "file", Description = "Path to the .tsk file to validate" }
                    },
                    Options = new List<OptionHelp>
                    {
                        new OptionHelp { Aliases = new[] { "-o", "--output" }, Description = "Output file for validation report" },
                        new OptionHelp { Aliases = new[] { "-f", "--format" }, Description = "Report format: detailed, json, summary, checklist" },
                        new OptionHelp { Aliases = new[] { "--strict" }, Description = "Enable strict validation mode" },
                        new OptionHelp { Aliases = new[] { "--best-practices" }, Description = "Check against best practices" },
                        new OptionHelp { Aliases = new[] { "--performance" }, Description = "Include performance analysis" },
                        new OptionHelp { Aliases = new[] { "--security" }, Description = "Include security analysis" },
                        new OptionHelp { Aliases = new[] { "--compatibility" }, Description = "Check version compatibility" }
                    },
                    Examples = new List<ExampleHelp>
                    {
                        new ExampleHelp { Command = "tusk validate config.tsk", Description = "Basic validation" },
                        new ExampleHelp { Command = "tusk validate config.tsk --strict --security", Description = "Strict validation with security checks" },
                        new ExampleHelp { Command = "tusk validate config.tsk --format checklist", Description = "Checklist format output" }
                    },
                    RelatedCommands = new List<string> { "parse", "compile" }
                },

                ["init"] = new CommandHelp
                {
                    Name = "init",
                    Category = "Project Management",
                    Description = "Create new TuskLang project with scaffolding and templates",
                    DetailedDescription = "Initializes new TuskLang projects with proper directory structure, configuration templates, and documentation based on the selected template type.",
                    Usage = "tusk init [name] [options]",
                    Arguments = new List<ArgumentHelp>
                    {
                        new ArgumentHelp { Name = "name", Description = "Name of the project to create (optional)" }
                    },
                    Options = new List<OptionHelp>
                    {
                        new OptionHelp { Aliases = new[] { "-t", "--template" }, Description = "Project template: basic, web, api, database, microservice" },
                        new OptionHelp { Aliases = new[] { "-d", "--directory" }, Description = "Directory to create project in" },
                        new OptionHelp { Aliases = new[] { "-f", "--force" }, Description = "Overwrite existing files" },
                        new OptionHelp { Aliases = new[] { "-i", "--interactive" }, Description = "Interactive project setup" },
                        new OptionHelp { Aliases = new[] { "--git" }, Description = "Initialize git repository" }
                    },
                    Examples = new List<ExampleHelp>
                    {
                        new ExampleHelp { Command = "tusk init my-project", Description = "Create basic project" },
                        new ExampleHelp { Command = "tusk init api-service --template api", Description = "Create API service project" },
                        new ExampleHelp { Command = "tusk init --interactive", Description = "Interactive setup" }
                    },
                    RelatedCommands = new List<string> { "build" }
                },

                ["build"] = new CommandHelp
                {
                    Name = "build",
                    Category = "Project Management",
                    Description = "Build current project with all configurations and environments",
                    DetailedDescription = "Builds TuskLang projects by processing all configuration files, validating, compiling, and generating build artifacts for deployment.",
                    Usage = "tusk build [options]",
                    Options = new List<OptionHelp>
                    {
                        new OptionHelp { Aliases = new[] { "-c", "--config" }, Description = "Main configuration file to build" },
                        new OptionHelp { Aliases = new[] { "-e", "--environment" }, Description = "Build for specific environment" },
                        new OptionHelp { Aliases = new[] { "-o", "--output" }, Description = "Output directory for build artifacts" },
                        new OptionHelp { Aliases = new[] { "--clean" }, Description = "Clean build directory first" },
                        new OptionHelp { Aliases = new[] { "--validate" }, Description = "Validate configurations before building" },
                        new OptionHelp { Aliases = new[] { "--compile" }, Description = "Compile configurations to binary format" },
                        new OptionHelp { Aliases = new[] { "-p", "--parallel" }, Description = "Enable parallel processing" },
                        new OptionHelp { Aliases = new[] { "-v", "--verbose" }, Description = "Enable verbose build output" }
                    },
                    Examples = new List<ExampleHelp>
                    {
                        new ExampleHelp { Command = "tusk build", Description = "Build with default settings" },
                        new ExampleHelp { Command = "tusk build --environment production --clean", Description = "Clean production build" },
                        new ExampleHelp { Command = "tusk build --parallel --verbose", Description = "Parallel build with verbose output" }
                    },
                    RelatedCommands = new List<string> { "init", "watch" }
                },

                ["watch"] = new CommandHelp
                {
                    Name = "watch",
                    Category = "Development Tools",
                    Description = "Watch files for changes and auto-recompile with fast change detection",
                    DetailedDescription = "Monitors TuskLang configuration files for changes and automatically recompiles, validates, or rebuilds when modifications are detected.",
                    Usage = "tusk watch [file] [options]",
                    Arguments = new List<ArgumentHelp>
                    {
                        new ArgumentHelp { Name = "file", Description = "Path to the .tsk file to watch (default: peanu.tsk)" }
                    },
                    Options = new List<OptionHelp>
                    {
                        new OptionHelp { Aliases = new[] { "-o", "--output" }, Description = "Output directory for build artifacts" },
                        new OptionHelp { Aliases = new[] { "-i", "--interval" }, Description = "Watch polling interval in milliseconds" },
                        new OptionHelp { Aliases = new[] { "--compile" }, Description = "Auto-compile on changes" },
                        new OptionHelp { Aliases = new[] { "--validate" }, Description = "Auto-validate on changes" },
                        new OptionHelp { Aliases = new[] { "--pattern" }, Description = "File pattern to watch" },
                        new OptionHelp { Aliases = new[] { "-r", "--recursive" }, Description = "Watch subdirectories recursively" },
                        new OptionHelp { Aliases = new[] { "--debounce" }, Description = "Debounce delay in milliseconds" }
                    },
                    Examples = new List<ExampleHelp>
                    {
                        new ExampleHelp { Command = "tusk watch", Description = "Watch default configuration" },
                        new ExampleHelp { Command = "tusk watch config.tsk --compile", Description = "Watch and auto-compile" },
                        new ExampleHelp { Command = "tusk watch --pattern \"*.tsk\" --recursive", Description = "Watch all .tsk files recursively" }
                    },
                    Notes = "Press Ctrl+C to stop watching. Use --debounce to control rapid change detection.",
                    RelatedCommands = new List<string> { "build", "compile" }
                },

                ["version"] = new CommandHelp
                {
                    Name = "version",
                    Category = "Information",
                    Description = "Show version and build information",
                    DetailedDescription = "Displays comprehensive version information including build details, system information, and dependency versions.",
                    Usage = "tusk version [options]",
                    Options = new List<OptionHelp>
                    {
                        new OptionHelp { Aliases = new[] { "-d", "--details" }, Description = "Show detailed version information" },
                        new OptionHelp { Aliases = new[] { "-s", "--system" }, Description = "Include system information" },
                        new OptionHelp { Aliases = new[] { "--dependencies" }, Description = "Show dependency versions" }
                    },
                    Examples = new List<ExampleHelp>
                    {
                        new ExampleHelp { Command = "tusk version", Description = "Show basic version" },
                        new ExampleHelp { Command = "tusk version --details --system", Description = "Show detailed system information" },
                        new ExampleHelp { Command = "tusk version --dependencies", Description = "Show all dependencies" }
                    },
                    RelatedCommands = new List<string> { "help" }
                },

                ["help"] = new CommandHelp
                {
                    Name = "help",
                    Category = "Information",
                    Description = "Show help information for commands",
                    DetailedDescription = "Provides comprehensive help and usage information for all TuskLang CLI commands with examples and detailed explanations.",
                    Usage = "tusk help [command] [options]",
                    Arguments = new List<ArgumentHelp>
                    {
                        new ArgumentHelp { Name = "command", Description = "Command to get help for (optional)" }
                    },
                    Options = new List<OptionHelp>
                    {
                        new OptionHelp { Aliases = new[] { "-l", "--list" }, Description = "List all available commands" },
                        new OptionHelp { Aliases = new[] { "-e", "--examples" }, Description = "Show usage examples" },
                        new OptionHelp { Aliases = new[] { "-v", "--verbose" }, Description = "Show detailed help" }
                    },
                    Examples = new List<ExampleHelp>
                    {
                        new ExampleHelp { Command = "tusk help", Description = "Show general help" },
                        new ExampleHelp { Command = "tusk help parse", Description = "Show help for parse command" },
                        new ExampleHelp { Command = "tusk help --list", Description = "List all commands" }
                    },
                    RelatedCommands = new List<string> { "version" }
                }
            };
        }
    }

    #region Help Data Classes

    public class CommandHelp
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public string DetailedDescription { get; set; }
        public string Usage { get; set; }
        public string Notes { get; set; }
        public List<ArgumentHelp> Arguments { get; set; } = new List<ArgumentHelp>();
        public List<OptionHelp> Options { get; set; } = new List<OptionHelp>();
        public List<ExampleHelp> Examples { get; set; } = new List<ExampleHelp>();
        public List<string> RelatedCommands { get; set; } = new List<string>();
        public List<ExitCodeHelp> ExitCodes { get; set; } = new List<ExitCodeHelp>();
    }

    public class ArgumentHelp
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Details { get; set; }
    }

    public class OptionHelp
    {
        public string[] Aliases { get; set; }
        public string Description { get; set; }
        public string Details { get; set; }
    }

    public class ExampleHelp
    {
        public string Command { get; set; }
        public string Description { get; set; }
    }

    public class ExitCodeHelp
    {
        public int Code { get; set; }
        public string Description { get; set; }
    }

    #endregion
} 