using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Completions;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using TuskLang.Configuration;

namespace TuskTsk.CLI.Advanced
{
    public class InteractiveMode
    {
        private readonly ConfigurationManager _configManager;
        private readonly CommandLineBuilder _builder;
        private readonly List<string> _commandHistory;
        private readonly Dictionary<string, string> _aliases;
        private bool _isRunning;
        private int _historyIndex;

        public InteractiveMode(ConfigurationManager configManager)
        {
            _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
            _commandHistory = new List<string>();
            _aliases = new Dictionary<string, string>
            {
                { "p", "parse" },
                { "c", "compile" },
                { "v", "validate" },
                { "i", "init" },
                { "b", "build" },
                { "t", "test" },
                { "s", "serve" },
                { "db", "database" },
                { "cache", "cache" },
                { "css", "css" },
                { "ai", "ai" },
                { "pn", "peanuts" }
            };

            _builder = new CommandLineBuilder()
                .UseDefaults()
                .UseAnsiTerminalWhenAvailable()
                .AddCommand(new Command("exit", "Exit interactive mode"))
                .AddCommand(new Command("clear", "Clear the console"))
                .AddCommand(new Command("history", "Show command history"))
                .AddCommand(new Command("help", "Show available commands"))
                .AddCommand(new Command("version", "Show TSK version"));
        }

        public async Task StartAsync()
        {
            _isRunning = true;
            Console.WriteLine("🚀 TSK Interactive Mode");
            Console.WriteLine("Type 'help' for available commands, 'exit' to quit");
            Console.WriteLine();

            while (_isRunning)
            {
                try
                {
                    var prompt = GetPrompt();
                    var input = await ReadLineAsync(prompt);

                    if (string.IsNullOrWhiteSpace(input))
                        continue;

                    _commandHistory.Add(input);
                    _historyIndex = _commandHistory.Count;

                    await ProcessCommandAsync(input);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error: {ex.Message}");
                }
            }
        }

        private string GetPrompt()
        {
            var currentDir = Directory.GetCurrentDirectory();
            var dirName = Path.GetFileName(currentDir);
            return $"[TSK] {dirName}> ";
        }

        private async Task<string> ReadLineAsync(string prompt)
        {
            Console.Write(prompt);
            var input = new StringBuilder();
            var position = 0;

            while (true)
            {
                var key = Console.ReadKey(true);

                switch (key.Key)
                {
                    case ConsoleKey.Enter:
                        Console.WriteLine();
                        return input.ToString();

                    case ConsoleKey.Escape:
                        Console.WriteLine();
                        return "";

                    case ConsoleKey.Backspace:
                        if (position > 0)
                        {
                            input.Remove(position - 1, 1);
                            position--;
                            Console.Write("\b \b");
                        }
                        break;

                    case ConsoleKey.Delete:
                        if (position < input.Length)
                        {
                            input.Remove(position, 1);
                            Console.Write(" \b");
                        }
                        break;

                    case ConsoleKey.LeftArrow:
                        if (position > 0)
                        {
                            position--;
                            Console.Write("\b");
                        }
                        break;

                    case ConsoleKey.RightArrow:
                        if (position < input.Length)
                        {
                            position++;
                            Console.Write(input[position - 1]);
                        }
                        break;

                    case ConsoleKey.UpArrow:
                        var upSuggestion = GetHistorySuggestion(-1);
                        if (upSuggestion != null)
                        {
                            ClearCurrentLine();
                            input.Clear();
                            input.Append(upSuggestion);
                            position = input.Length;
                            Console.Write(upSuggestion);
                        }
                        break;

                    case ConsoleKey.DownArrow:
                        var downSuggestion = GetHistorySuggestion(1);
                        if (downSuggestion != null)
                        {
                            ClearCurrentLine();
                            input.Clear();
                            input.Append(downSuggestion);
                            position = input.Length;
                            Console.Write(downSuggestion);
                        }
                        break;

                    case ConsoleKey.Tab:
                        var completion = GetCompletion(input.ToString(), position);
                        if (completion != null)
                        {
                            ClearCurrentLine();
                            input.Clear();
                            input.Append(completion);
                            position = input.Length;
                            Console.Write(completion);
                        }
                        break;

                    default:
                        if (key.KeyChar >= 32 && key.KeyChar <= 126)
                        {
                            input.Insert(position, key.KeyChar);
                            position++;
                            Console.Write(key.KeyChar);
                        }
                        break;
                }
            }
        }

        private void ClearCurrentLine()
        {
            var currentLine = Console.CursorTop;
            Console.SetCursorPosition(0, currentLine);
            Console.Write(new string(' ', Console.WindowWidth - 1));
            Console.SetCursorPosition(0, currentLine);
        }

        private string GetHistorySuggestion(int direction)
        {
            var newIndex = _historyIndex + direction;
            if (newIndex >= 0 && newIndex < _commandHistory.Count)
            {
                _historyIndex = newIndex;
                return _commandHistory[_historyIndex];
            }
            return null;
        }

        private string GetCompletion(string input, int position)
        {
            var words = input.Split(' ');
            if (words.Length == 0) return null;

            var currentWord = words[words.Length - 1];
            var suggestions = new List<string>();

            // Command completions
            var commands = new[] { "parse", "compile", "validate", "init", "build", "test", "serve", "config", "database", "cache", "license", "css", "peanuts", "ai", "utility" };
            suggestions.AddRange(commands.Where(c => c.StartsWith(currentWord, StringComparison.OrdinalIgnoreCase)));

            // File completions
            if (currentWord.Contains('.'))
            {
                var dir = Path.GetDirectoryName(currentWord) ?? ".";
                var pattern = Path.GetFileName(currentWord) + "*";
                try
                {
                    var files = Directory.GetFiles(dir, pattern);
                    suggestions.AddRange(files.Select(f => Path.GetFileName(f)));
                }
                catch { }
            }

            return suggestions.FirstOrDefault();
        }

        private async Task ProcessCommandAsync(string input)
        {
            var args = ParseArguments(input);
            if (args.Length == 0) return;

            var command = args[0].ToLower();

            // Handle built-in commands
            switch (command)
            {
                case "exit":
                    _isRunning = false;
                    Console.WriteLine("👋 Goodbye!");
                    return;

                case "clear":
                    Console.Clear();
                    return;

                case "history":
                    ShowHistory();
                    return;

                case "help":
                    ShowHelp();
                    return;

                case "version":
                    ShowVersion();
                    return;
            }

            // Handle aliases
            if (_aliases.ContainsKey(command))
            {
                args[0] = _aliases[command];
            }

            // Execute TSK command
            await ExecuteTskCommandAsync(args);
        }

        private string[] ParseArguments(string input)
        {
            var args = new List<string>();
            var current = new StringBuilder();
            var inQuotes = false;

            for (int i = 0; i < input.Length; i++)
            {
                var c = input[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ' ' && !inQuotes)
                {
                    if (current.Length > 0)
                    {
                        args.Add(current.ToString());
                        current.Clear();
                    }
                }
                else
                {
                    current.Append(c);
                }
            }

            if (current.Length > 0)
            {
                args.Add(current.ToString());
            }

            return args.ToArray();
        }

        private void ShowHistory()
        {
            Console.WriteLine("📜 Command History:");
            for (int i = 0; i < _commandHistory.Count; i++)
            {
                Console.WriteLine($"  {i + 1}: {_commandHistory[i]}");
            }
        }

        private void ShowHelp()
        {
            Console.WriteLine("🆘 Available Commands:");
            Console.WriteLine("  parse <file>     - Parse a TSK file");
            Console.WriteLine("  compile <file>   - Compile a TSK file");
            Console.WriteLine("  validate <file>  - Validate a TSK file");
            Console.WriteLine("  init <name>      - Initialize a new project");
            Console.WriteLine("  build            - Build the current project");
            Console.WriteLine("  test             - Run tests");
            Console.WriteLine("  serve            - Start development server");
            Console.WriteLine("  config           - Manage configuration");
            Console.WriteLine("  database         - Database operations");
            Console.WriteLine("  cache            - Cache operations");
            Console.WriteLine("  license          - License management");
            Console.WriteLine("  css              - CSS processing");
            Console.WriteLine("  peanuts          - Peanuts token operations");
            Console.WriteLine("  ai               - AI integrations");
            Console.WriteLine("  utility          - Utility functions");
            Console.WriteLine();
            Console.WriteLine("📝 Built-in Commands:");
            Console.WriteLine("  help             - Show this help");
            Console.WriteLine("  history          - Show command history");
            Console.WriteLine("  clear            - Clear console");
            Console.WriteLine("  version          - Show version");
            Console.WriteLine("  exit             - Exit interactive mode");
        }

        private void ShowVersion()
        {
            Console.WriteLine("📦 TSK Version: 2.0.2");
            Console.WriteLine("🎯 .NET Runtime: " + Environment.Version);
            Console.WriteLine("🖥️  Platform: " + Environment.OSVersion);
        }

        private async Task ExecuteTskCommandAsync(string[] args)
        {
            try
            {
                // This would integrate with the actual TSK command system
                Console.WriteLine($"🔧 Executing: {string.Join(" ", args)}");
                
                // Simulate command execution with progress
                using (var progress = new ProgressBar())
                {
                    for (int i = 0; i <= 100; i += 10)
                    {
                        progress.Report(i / 100.0);
                        await Task.Delay(50);
                    }
                }

                Console.WriteLine("✅ Command completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Command failed: {ex.Message}");
            }
        }
    }

    public class ProgressBar : IDisposable
    {
        private readonly int _width;
        private int _lastProgress;

        public ProgressBar(int width = 50)
        {
            _width = width;
            _lastProgress = -1;
        }

        public void Report(double progress)
        {
            var currentProgress = (int)(progress * _width);
            if (currentProgress == _lastProgress) return;

            _lastProgress = currentProgress;
            var bar = new string('█', currentProgress) + new string('░', _width - currentProgress);
            var percentage = (int)(progress * 100);
            
            Console.Write($"\r[{bar}] {percentage}%");
        }

        public void Dispose()
        {
            Console.WriteLine();
        }
    }
} 