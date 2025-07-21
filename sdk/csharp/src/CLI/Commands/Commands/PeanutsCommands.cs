using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;

namespace TuskLang.CLI.Commands
{
    /// <summary>
    /// Peanuts commands for TuskLang CLI
    /// </summary>
    public static class PeanutsCommands
    {
        public static void AddCommands(RootCommand rootCommand)
        {
            var peanutsCommand = new Command("peanuts", "Peanuts operations")
            {
                new Command("compile", "Compile .peanuts to binary .pnt")
                {
                    new Argument<string>("file", "Peanuts file to compile"),
                    new Option<string>("--output", "Output file path"),
                    Handler = CommandHandler.Create<string, string>(CompilePeanuts)
                },
                new Command("auto-compile", "Auto-compile all peanuts files in directory")
                {
                    new Argument<string>("dir", () => ".", "Directory to compile"),
                    Handler = CommandHandler.Create<string>(AutoCompilePeanuts)
                },
                new Command("load", "Load and display binary peanuts file")
                {
                    new Argument<string>("file.pnt", "Binary peanuts file to load"),
                    Handler = CommandHandler.Create<string>(LoadPeanuts)
                }
            };

            rootCommand.AddCommand(peanutsCommand);
        }

        private static async Task<int> CompilePeanuts(string file, string output)
        {
            try
            {
                if (!File.Exists(file))
                {
                    GlobalOptions.WriteError($"File not found: {file}");
                    return 3;
                }

                GlobalOptions.WriteProcessing($"Compiling peanuts file: {file}");

                var config = new PeanutConfig();
                var content = await File.ReadAllTextAsync(file);
                var parsed = config.ParseTextConfig(content);

                var outputFile = output ?? Path.ChangeExtension(file, ".pnt");
                await config.CompileToBinaryAsync(parsed, outputFile);

                GlobalOptions.WriteSuccess($"Peanuts compiled: {file} → {outputFile}");
                return 0;
            }
            catch (Exception ex)
            {
                GlobalOptions.WriteError($"Peanuts compilation failed: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> AutoCompilePeanuts(string dir)
        {
            try
            {
                GlobalOptions.WriteProcessing($"Auto-compiling peanuts files in: {dir}");

                var config = new PeanutConfig();
                var files = Directory.GetFiles(dir, "*.peanuts", SearchOption.AllDirectories);
                var compiledCount = 0;

                foreach (var file in files)
                {
                    try
                    {
                        var content = await File.ReadAllTextAsync(file);
                        var parsed = config.ParseTextConfig(content);
                        var outputFile = Path.ChangeExtension(file, ".pnt");
                        await config.CompileToBinaryAsync(parsed, outputFile);
                        compiledCount++;
                    }
                    catch (Exception ex)
                    {
                        GlobalOptions.WriteError($"Failed to compile {file}: {ex.Message}");
                    }
                }

                GlobalOptions.WriteSuccess($"Auto-compiled {compiledCount} peanuts files");
                return 0;
            }
            catch (Exception ex)
            {
                GlobalOptions.WriteError($"Auto-compilation failed: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> LoadPeanuts(string file)
        {
            try
            {
                if (!File.Exists(file))
                {
                    GlobalOptions.WriteError($"File not found: {file}");
                    return 3;
                }

                GlobalOptions.WriteProcessing($"Loading peanuts file: {file}");

                var config = new PeanutConfig();
                var data = await config.LoadBinaryAsync(file);

                if (GlobalOptions.JsonOutput)
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    GlobalOptions.WriteLine(json);
                }
                else
                {
                    GlobalOptions.WriteLine("Peanuts file contents:");
                    GlobalOptions.WriteLine("=====================");
                    foreach (var kvp in data)
                    {
                        GlobalOptions.WriteLine($"[{kvp.Key}]");
                        if (kvp.Value is Dictionary<string, object> sectionData)
                        {
                            foreach (var sectionKvp in sectionData)
                            {
                                GlobalOptions.WriteLine($"  {sectionKvp.Key} = {sectionKvp.Value}");
                            }
                        }
                        GlobalOptions.WriteLine();
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                GlobalOptions.WriteError($"Failed to load peanuts file: {ex.Message}");
                return 1;
            }
        }
    }
} 