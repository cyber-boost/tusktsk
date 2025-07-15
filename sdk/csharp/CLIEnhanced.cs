using System;
using System.IO;
using System.Linq;

namespace TuskLang
{
    /// <summary>
    /// Enhanced CLI for TuskLang C# SDK
    /// </summary>
    public class CLIEnhanced
    {
        public static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                ShowHelp();
                return;
            }
            
            var command = args[0];
            
            try
            {
                switch (command.ToLower())
                {
                    case "parse":
                        if (args.Length < 2)
                        {
                            Console.WriteLine("Error: File path required");
                            Environment.Exit(1);
                        }
                        
                        var parser = new TSKParserEnhanced();
                        parser.ParseFile(args[1]);
                        
                        foreach (var key in parser.Keys)
                        {
                            var value = parser.Get(key);
                            Console.WriteLine($"{key} = {value}");
                        }
                        break;
                        
                    case "get":
                        if (args.Length < 3)
                        {
                            Console.WriteLine("Error: File path and key required");
                            Environment.Exit(1);
                        }
                        
                        var getParser = new TSKParserEnhanced();
                        getParser.ParseFile(args[1]);
                        
                        var getValue = getParser.Get(args[2]);
                        if (getValue != null)
                        {
                            Console.WriteLine(getValue);
                        }
                        break;
                        
                    case "keys":
                        if (args.Length < 2)
                        {
                            Console.WriteLine("Error: File path required");
                            Environment.Exit(1);
                        }
                        
                        var keysParser = new TSKParserEnhanced();
                        keysParser.ParseFile(args[1]);
                        
                        foreach (var key in keysParser.Keys)
                        {
                            Console.WriteLine(key);
                        }
                        break;
                        
                    case "peanut":
                        var peanutParser = TSKParserEnhanced.LoadFromPeanut();
                        Console.WriteLine($"Loaded {peanutParser.Items.Count} configuration items");
                        
                        foreach (var key in peanutParser.Keys)
                        {
                            var value = peanutParser.Get(key);
                            Console.WriteLine($"{key} = {value}");
                        }
                        break;
                        
                    case "json":
                        if (args.Length < 2)
                        {
                            Console.WriteLine("Error: File path required");
                            Environment.Exit(1);
                        }
                        
                        var jsonParser = new TSKParserEnhanced();
                        jsonParser.ParseFile(args[1]);
                        
                        Console.WriteLine(jsonParser.ToJson());
                        break;
                        
                    case "validate":
                        if (args.Length < 2)
                        {
                            Console.WriteLine("Error: File path required");
                            Environment.Exit(1);
                        }
                        
                        try
                        {
                            var validateParser = new TSKParserEnhanced();
                            validateParser.ParseFile(args[1]);
                            Console.WriteLine("✅ File is valid TuskLang syntax");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ Validation failed: {ex.Message}");
                            Environment.Exit(1);
                        }
                        break;
                        
                    default:
                        Console.WriteLine($"Error: Unknown command: {command}");
                        ShowHelp();
                        Environment.Exit(1);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        }
        
        private static void ShowHelp()
        {
            Console.WriteLine(@"
TuskLang Enhanced for C# - The Freedom Parser
============================================

Usage: TuskLang.exe [command] [options]

Commands:
    parse <file>     Parse a .tsk file and show all key-value pairs
    get <file> <key> Get a specific value by key
    keys <file>      List all keys in the file
    json <file>      Convert .tsk file to JSON format
    validate <file>  Validate .tsk file syntax
    peanut           Load configuration from peanut.tsk
    
Examples:
    TuskLang.exe parse appsettings.tsk
    TuskLang.exe get appsettings.tsk database.host
    TuskLang.exe keys appsettings.tsk
    TuskLang.exe json appsettings.tsk
    TuskLang.exe validate appsettings.tsk
    TuskLang.exe peanut

Features:
    - Multiple syntax styles: [], {}, <>
    - Global variables with $
    - Cross-file references: @file.tsk.get()
    - Database queries: @query()
    - Date functions: @date()
    - Environment variables: @env()
    - Conditional expressions (ternary operator)
    - Range syntax: 8000-9000
    - String concatenation with +
    - Optional semicolons
    - Entity Framework integration ready
    - ASP.NET Core configuration support

Default config file: peanut.tsk
""We don't bow to any king"" - Maximum syntax flexibility
");
        }
    }
}