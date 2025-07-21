using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace TuskLang.CLI.Commands
{
    /// <summary>
    /// Development commands for TuskLang CLI
    /// </summary>
    public static class DevelopmentCommands
    {
        public static void AddCommands(RootCommand rootCommand)
        {
            var serveCommand = new Command("serve", "Start development server")
            {
                new Argument<int>("port", () => 8080, "Port number"),
                new Option<string>("--host", () => "localhost", "Host address"),
                new Option<bool>("--https", "Enable HTTPS"),
                Handler = CommandHandler.Create<int, string, bool>(StartDevelopmentServer)
            };

            var compileCommand = new Command("compile", "Compile .tsk file")
            {
                new Argument<string>("file", "TSK file to compile"),
                new Option<string>("--output", "Output file path"),
                new Option<bool>("--optimize", "Enable optimization"),
                Handler = CommandHandler.Create<string, string, bool>(CompileTskFile)
            };

            var optimizeCommand = new Command("optimize", "Optimize .tsk file for production")
            {
                new Argument<string>("file", "TSK file to optimize"),
                new Option<string>("--output", "Output file path"),
                new Option<bool>("--minify", "Minify output"),
                Handler = CommandHandler.Create<string, string, bool>(OptimizeTskFile)
            };

            rootCommand.AddCommand(serveCommand);
            rootCommand.AddCommand(compileCommand);
            rootCommand.AddCommand(optimizeCommand);
        }

        private static async Task<int> StartDevelopmentServer(int port, string host, bool https)
        {
            try
            {
                GlobalOptions.WriteProcessing($"Starting development server on {host}:{port}");

                var config = await LoadConfiguration();
                var serverConfig = GetServerConfiguration(config);

                var listener = new HttpListener();
                var prefix = $"http{(https ? "s" : "")}://{host}:{port}/";
                listener.Prefixes.Add(prefix);
                listener.Start();

                GlobalOptions.WriteSuccess($"Development server started at {prefix}");
                GlobalOptions.WriteLine("Press Ctrl+C to stop the server");

                // Handle requests
                while (listener.IsListening)
                {
                    try
                    {
                        var context = await listener.GetContextAsync();
                        _ = Task.Run(() => HandleRequest(context, serverConfig));
                    }
                    catch (Exception ex)
                    {
                        if (listener.IsListening)
                        {
                            GlobalOptions.WriteError($"Request handling error: {ex.Message}");
                        }
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                GlobalOptions.WriteError($"Failed to start development server: {ex.Message}");
                return 1;
            }
        }

        private static async Task HandleRequest(HttpListenerContext context, Dictionary<string, object> serverConfig)
        {
            try
            {
                var request = context.Request;
                var response = context.Response;

                GlobalOptions.WriteLine($"{request.HttpMethod} {request.Url.PathAndQuery}");

                var path = request.Url.AbsolutePath;
                var responseContent = "";

                // Route requests
                switch (path)
                {
                    case "/":
                        responseContent = GenerateHomePage(serverConfig);
                        response.ContentType = "text/html";
                        break;

                    case "/api/status":
                        responseContent = GenerateStatusResponse(serverConfig);
                        response.ContentType = "application/json";
                        break;

                    case "/api/config":
                        responseContent = GenerateConfigResponse(serverConfig);
                        response.ContentType = "application/json";
                        break;

                    default:
                        response.StatusCode = 404;
                        responseContent = Generate404Page();
                        response.ContentType = "text/html";
                        break;
                }

                var buffer = Encoding.UTF8.GetBytes(responseContent);
                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                response.Close();
            }
            catch (Exception ex)
            {
                GlobalOptions.WriteError($"Request handling error: {ex.Message}");
            }
        }

        private static string GenerateHomePage(Dictionary<string, object> serverConfig)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <title>TuskLang Development Server</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 40px; }}
        .container {{ max-width: 800px; margin: 0 auto; }}
        .header {{ background: #f0f0f0; padding: 20px; border-radius: 5px; }}
        .endpoint {{ background: #f9f9f9; padding: 10px; margin: 10px 0; border-left: 4px solid #007acc; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🐘 TuskLang Development Server</h1>
            <p>Strong. Secure. Scalable.</p>
        </div>
        
        <h2>Available Endpoints</h2>
        <div class='endpoint'>
            <strong>GET /api/status</strong> - Server status and health check
        </div>
        <div class='endpoint'>
            <strong>GET /api/config</strong> - Current configuration
        </div>
        
        <h2>Server Information</h2>
        <p><strong>Version:</strong> {serverConfig.GetValueOrDefault("version", "2.0.0")}</p>
        <p><strong>Environment:</strong> {serverConfig.GetValueOrDefault("environment", "development")}</p>
        <p><strong>Started:</strong> {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>
    </div>
</body>
</html>";
        }

        private static string GenerateStatusResponse(Dictionary<string, object> serverConfig)
        {
            return System.Text.Json.JsonSerializer.Serialize(new
            {
                status = "running",
                version = serverConfig.GetValueOrDefault("version", "2.0.0"),
                environment = serverConfig.GetValueOrDefault("environment", "development"),
                uptime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
        }

        private static string GenerateConfigResponse(Dictionary<string, object> serverConfig)
        {
            return System.Text.Json.JsonSerializer.Serialize(serverConfig);
        }

        private static string Generate404Page()
        {
            return @"
<!DOCTYPE html>
<html>
<head>
    <title>404 - Not Found</title>
    <style>
        body { font-family: Arial, sans-serif; text-align: center; margin-top: 100px; }
    </style>
</head>
<body>
    <h1>404 - Page Not Found</h1>
    <p>The requested resource was not found on this server.</p>
    <a href='/'>Return to Home</a>
</body>
</html>";
        }

        private static async Task<int> CompileTskFile(string file, string output, bool optimize)
        {
            try
            {
                if (!File.Exists(file))
                {
                    GlobalOptions.WriteError($"File not found: {file}");
                    return 3;
                }

                GlobalOptions.WriteProcessing($"Compiling TSK file: {file}");

                var content = await File.ReadAllTextAsync(file);
                var tsk = TSK.FromString(content);

                // Compile to optimized format
                var compiledContent = CompileTskContent(tsk, optimize);

                // Determine output file
                var outputFile = output ?? Path.ChangeExtension(file, ".tskc");
                await File.WriteAllTextAsync(outputFile, compiledContent);

                GlobalOptions.WriteSuccess($"TSK file compiled to: {outputFile}");

                if (GlobalOptions.Verbose)
                {
                    var originalSize = new FileInfo(file).Length;
                    var compiledSize = new FileInfo(outputFile).Length;
                    var compression = ((double)(originalSize - compiledSize) / originalSize) * 100;
                    GlobalOptions.WriteLine($"Original size: {originalSize} bytes");
                    GlobalOptions.WriteLine($"Compiled size: {compiledSize} bytes");
                    GlobalOptions.WriteLine($"Compression: {compression:F1}%");
                }

                return 0;
            }
            catch (Exception ex)
            {
                GlobalOptions.WriteError($"Compilation failed: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> OptimizeTskFile(string file, string output, bool minify)
        {
            try
            {
                if (!File.Exists(file))
                {
                    GlobalOptions.WriteError($"File not found: {file}");
                    return 3;
                }

                GlobalOptions.WriteProcessing($"Optimizing TSK file: {file}");

                var content = await File.ReadAllTextAsync(file);
                var tsk = TSK.FromString(content);

                // Optimize the content
                var optimizedContent = OptimizeTskContent(tsk, minify);

                // Determine output file
                var outputFile = output ?? Path.ChangeExtension(file, ".optimized.tsk");
                await File.WriteAllTextAsync(outputFile, optimizedContent);

                GlobalOptions.WriteSuccess($"TSK file optimized to: {outputFile}");

                if (GlobalOptions.Verbose)
                {
                    var originalSize = new FileInfo(file).Length;
                    var optimizedSize = new FileInfo(outputFile).Length;
                    var improvement = ((double)(originalSize - optimizedSize) / originalSize) * 100;
                    GlobalOptions.WriteLine($"Original size: {originalSize} bytes");
                    GlobalOptions.WriteLine($"Optimized size: {optimizedSize} bytes");
                    GlobalOptions.WriteLine($"Size improvement: {improvement:F1}%");
                }

                return 0;
            }
            catch (Exception ex)
            {
                GlobalOptions.WriteError($"Optimization failed: {ex.Message}");
                return 1;
            }
        }

        private static string CompileTskContent(TSK tsk, bool optimize)
        {
            var data = tsk.ToDictionary();
            var compiled = new StringBuilder();

            compiled.AppendLine("# TuskLang Compiled Configuration");
            compiled.AppendLine($"# Compiled: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            compiled.AppendLine($"# Optimized: {optimize}");
            compiled.AppendLine();

            foreach (var section in data)
            {
                compiled.AppendLine($"[{section.Key}]");

                if (section.Value is Dictionary<string, object> sectionData)
                {
                    foreach (var kvp in sectionData)
                    {
                        var value = FormatValueForCompilation(kvp.Value, optimize);
                        compiled.AppendLine($"{kvp.Key} = {value}");
                    }
                }
                else
                {
                    compiled.AppendLine($"value = {FormatValueForCompilation(section.Value, optimize)}");
                }

                compiled.AppendLine();
            }

            return compiled.ToString();
        }

        private static string OptimizeTskContent(TSK tsk, bool minify)
        {
            var data = tsk.ToDictionary();
            var optimized = new StringBuilder();

            if (!minify)
            {
                optimized.AppendLine("# TuskLang Optimized Configuration");
                optimized.AppendLine($"# Optimized: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                optimized.AppendLine();
            }

            foreach (var section in data)
            {
                if (!minify) optimized.AppendLine($"[{section.Key}]");
                else optimized.Append($"[{section.Key}]");

                if (section.Value is Dictionary<string, object> sectionData)
                {
                    foreach (var kvp in sectionData)
                    {
                        var value = FormatValueForOptimization(kvp.Value, minify);
                        if (minify)
                            optimized.Append($"{kvp.Key}={value}");
                        else
                            optimized.AppendLine($"{kvp.Key} = {value}");
                    }
                }
                else
                {
                    var value = FormatValueForOptimization(section.Value, minify);
                    if (minify)
                        optimized.Append($"value={value}");
                    else
                        optimized.AppendLine($"value = {value}");
                }

                if (!minify) optimized.AppendLine();
            }

            return optimized.ToString();
        }

        private static string FormatValueForCompilation(object value, bool optimize)
        {
            if (value == null) return "null";
            if (value is bool b) return b.ToString().ToLower();
            if (value is string str)
            {
                if (optimize && str.Length > 100)
                {
                    // Truncate long strings in optimized mode
                    return $"\"{str.Substring(0, 100)}...\"";
                }
                return $"\"{str.Replace("\"", "\\\"")}\"";
            }
            if (value is int || value is long || value is double || value is float) return value.ToString();
            if (value is Dictionary<string, object> dict)
            {
                var pairs = dict.Select(kvp => $"\"{kvp.Key}\" = {FormatValueForCompilation(kvp.Value, optimize)}");
                return $"{{{string.Join(", ", pairs)}}}";
            }
            if (value is IEnumerable<object> enumerable)
            {
                var items = enumerable.Select(item => FormatValueForCompilation(item, optimize));
                return $"[{string.Join(", ", items)}]";
            }
            return $"\"{value}\"";
        }

        private static string FormatValueForOptimization(object value, bool minify)
        {
            if (value == null) return "null";
            if (value is bool b) return b.ToString().ToLower();
            if (value is string str)
            {
                if (minify)
                {
                    // Remove unnecessary quotes for simple strings
                    if (str.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-'))
                        return str;
                }
                return $"\"{str.Replace("\"", "\\\"")}\"";
            }
            if (value is int || value is long || value is double || value is float) return value.ToString();
            if (value is Dictionary<string, object> dict)
            {
                var pairs = dict.Select(kvp => $"\"{kvp.Key}\"={FormatValueForOptimization(kvp.Value, minify)}");
                return $"{{{string.Join(",", pairs)}}}";
            }
            if (value is IEnumerable<object> enumerable)
            {
                var items = enumerable.Select(item => FormatValueForOptimization(item, minify));
                return $"[{string.Join(",", items)}]";
            }
            return $"\"{value}\"";
        }

        private static async Task<Dictionary<string, object>> LoadConfiguration()
        {
            var config = new PeanutConfig();
            return await config.LoadAsync();
        }

        private static Dictionary<string, object> GetServerConfiguration(Dictionary<string, object> config)
        {
            return new Dictionary<string, object>
            {
                ["version"] = config.GetValueOrDefault("version", "2.0.0"),
                ["environment"] = config.GetValueOrDefault("environment", "development"),
                ["server"] = config.GetValueOrDefault("server", new Dictionary<string, object>()),
                ["features"] = config.GetValueOrDefault("features", new Dictionary<string, object>())
            };
        }
    }
} 