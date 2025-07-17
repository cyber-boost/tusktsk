using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Dynamic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;

namespace TuskLang
{
    /// <summary>
    /// TSK (TuskLang Configuration) Parser and Generator for C#
    /// Handles the TOML-like format with full fujsen (function serialization) support
    /// </summary>
    public class TSK
    {
        private Dictionary<string, object> _data;
        private ConcurrentDictionary<string, Delegate> _fujsenCache;
        private Dictionary<int, string> _comments;
        private Dictionary<string, object> _metadata;

        public TSK(Dictionary<string, object> data = null)
        {
            _data = data ?? new Dictionary<string, object>();
            _fujsenCache = new ConcurrentDictionary<string, Delegate>();
            _comments = new Dictionary<int, string>();
            _metadata = new Dictionary<string, object>();
        }

        /// <summary>
        /// Create TSK instance from string content
        /// </summary>
        public static TSK FromString(string content)
        {
            var parser = new TSKParser();
            var (data, comments) = parser.ParseWithComments(content);
            var tsk = new TSK(data);
            tsk._comments = comments;
            return tsk;
        }

        /// <summary>
        /// Create TSK instance from file
        /// </summary>
        public static TSK FromFile(string filepath)
        {
            var content = File.ReadAllText(filepath);
            return FromString(content);
        }

        /// <summary>
        /// Get a section by name
        /// </summary>
        public Dictionary<string, object> GetSection(string name)
        {
            return _data.ContainsKey(name) && _data[name] is Dictionary<string, object> section 
                ? section 
                : null;
        }

        /// <summary>
        /// Get a value from a section
        /// </summary>
        public object GetValue(string section, string key)
        {
            var sectionData = GetSection(section);
            return sectionData?.ContainsKey(key) == true ? sectionData[key] : null;
        }

        /// <summary>
        /// Set a section with values
        /// </summary>
        public void SetSection(string name, Dictionary<string, object> values)
        {
            _data[name] = values;
        }

        /// <summary>
        /// Set a value in a section
        /// </summary>
        public void SetValue(string section, string key, object value)
        {
            if (!_data.ContainsKey(section))
                _data[section] = new Dictionary<string, object>();
            
            if (_data[section] is Dictionary<string, object> sectionData)
                sectionData[key] = value;
        }

        /// <summary>
        /// Convert TSK to string representation
        /// </summary>
        public override string ToString()
        {
            return TSKParser.Stringify(_data);
        }

        /// <summary>
        /// Convert TSK to dictionary
        /// </summary>
        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>(_data);
        }

        /// <summary>
        /// Execute a FUJSEN function from a section
        /// </summary>
        public object ExecuteFujsen(string section, string key = "fujsen", params object[] args)
        {
            var sectionData = GetSection(section);
            if (sectionData == null || !sectionData.ContainsKey(key))
                throw new ArgumentException($"FUJSEN function '{key}' not found in section '{section}'");

            var code = sectionData[key]?.ToString();
            if (string.IsNullOrEmpty(code))
                throw new ArgumentException($"FUJSEN code is empty for '{section}.{key}'");

            var cacheKey = $"{section}.{key}";
            if (!_fujsenCache.TryGetValue(cacheKey, out var compiledFunction))
            {
                compiledFunction = CompileFujsen(code);
                _fujsenCache[cacheKey] = compiledFunction;
            }

            return compiledFunction.DynamicInvoke(args);
        }

        /// <summary>
        /// Execute FUJSEN with context injection
        /// </summary>
        public object ExecuteFujsenWithContext(string section, string key, Dictionary<string, object> context, params object[] args)
        {
            var sectionData = GetSection(section);
            if (sectionData == null || !sectionData.ContainsKey(key))
                throw new ArgumentException($"FUJSEN function '{key}' not found in section '{section}'");

            var code = sectionData[key]?.ToString();
            if (string.IsNullOrEmpty(code))
                throw new ArgumentException($"FUJSEN code is empty for '{section}.{key}'");

            // Inject context into the function
            var contextCode = InjectContextIntoCode(code, context);
            
            var cacheKey = $"{section}.{key}.{GetContextHash(context)}";
            if (!_fujsenCache.TryGetValue(cacheKey, out var compiledFunction))
            {
                compiledFunction = CompileFujsen(contextCode);
                _fujsenCache[cacheKey] = compiledFunction;
            }

            return compiledFunction.DynamicInvoke(args);
        }

        /// <summary>
        /// Compile JavaScript FUJSEN code to C# delegate
        /// </summary>
        private Delegate CompileFujsen(string code)
        {
            // Convert JavaScript to C#-like code
            var csharpCode = ConvertJavaScriptToCSharp(code);
            
            // Create dynamic assembly for compilation
            using (var provider = new CSharpCodeProvider())
            {
                var parameters = new CompilerParameters
                {
                    GenerateInMemory = true,
                    GenerateExecutable = false,
                    ReferencedAssemblies = 
                    {
                        "System.dll",
                        "System.Core.dll",
                        "System.Linq.dll",
                        "System.Collections.dll"
                    }
                };

                var source = $@"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Dynamic;

public class FujsenFunction
{{
    public static object Execute(params object[] args)
    {{
        {csharpCode}
    }}
}}";

                var results = provider.CompileAssemblyFromSource(parameters, source);
                
                if (results.Errors.HasErrors)
                {
                    var errors = string.Join("\n", results.Errors.Cast<CompilerError>());
                    throw new InvalidOperationException($"FUJSEN compilation failed:\n{errors}");
                }

                var type = results.CompiledAssembly.GetType("FujsenFunction");
                var method = type.GetMethod("Execute");
                return Delegate.CreateDelegate(typeof(Func<object[], object>), method);
            }
        }

        /// <summary>
        /// Convert JavaScript code to C#-like code
        /// </summary>
        private string ConvertJavaScriptToCSharp(string jsCode)
        {
            // Basic JavaScript to C# conversion
            var csharpCode = jsCode
                .Replace("function", "// function")
                .Replace("var ", "var ")
                .Replace("let ", "var ")
                .Replace("const ", "var ")
                .Replace("=>", "=>")
                .Replace("throw new Error", "throw new Exception")
                .Replace("Date.now()", "DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()")
                .Replace("console.log", "Console.WriteLine");

            // Handle function declarations
            var functionMatch = Regex.Match(csharpCode, @"function\s+(\w+)\s*\(([^)]*)\)\s*\{");
            if (functionMatch.Success)
            {
                var funcName = functionMatch.Groups[1].Value;
                var parameters = functionMatch.Groups[2].Value;
                var paramArray = parameters.Split(',').Select(p => p.Trim()).ToArray();
                
                csharpCode = $@"
var {funcName} = new Func<{string.Join(", ", paramArray.Select(_ => "object"))}, object>(({parameters}) => {{
    {csharpCode.Substring(functionMatch.Index + functionMatch.Length)}
}});
return {funcName}(args);";
            }

            return csharpCode;
        }

        /// <summary>
        /// Inject context variables into FUJSEN code
        /// </summary>
        private string InjectContextIntoCode(string code, Dictionary<string, object> context)
        {
            var contextVars = string.Join("\n", 
                context.Select(kvp => $"var {kvp.Key} = {SerializeValue(kvp.Value)};"));
            
            return $"{contextVars}\n{code}";
        }

        /// <summary>
        /// Serialize value for JavaScript context
        /// </summary>
        private string SerializeValue(object value)
        {
            if (value == null) return "null";
            if (value is string str) return $"\"{str.Replace("\"", "\\\"")}\"";
            if (value is bool b) return b.ToString().ToLower();
            if (value is int || value is long || value is double || value is float) return value.ToString();
            if (value is Dictionary<string, object> dict)
            {
                var pairs = dict.Select(kvp => $"\"{kvp.Key}\": {SerializeValue(kvp.Value)}");
                return $"{{{string.Join(", ", pairs)}}}";
            }
            if (value is IEnumerable<object> enumerable)
            {
                var items = enumerable.Select(SerializeValue);
                return $"[{string.Join(", ", items)}]";
            }
            return $"\"{value}\"";
        }

        /// <summary>
        /// Get hash for context caching
        /// </summary>
        private string GetContextHash(Dictionary<string, object> context)
        {
            var contextStr = string.Join("|", context.OrderBy(kvp => kvp.Key).Select(kvp => $"{kvp.Key}={kvp.Value}"));
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(contextStr));
        }

        /// <summary>
        /// Get all FUJSEN functions from a section
        /// </summary>
        public Dictionary<string, Delegate> GetFujsenMap(string section)
        {
            var sectionData = GetSection(section);
            if (sectionData == null) return new Dictionary<string, Delegate>();

            var fujsenMap = new Dictionary<string, Delegate>();
            foreach (var kvp in sectionData)
            {
                if (kvp.Value is string code && !string.IsNullOrEmpty(code))
                {
                    try
                    {
                        var compiledFunction = CompileFujsen(code);
                        fujsenMap[kvp.Key] = compiledFunction;
                    }
                    catch
                    {
                        // Skip non-FUJSEN strings
                    }
                }
            }
            return fujsenMap;
        }

        /// <summary>
        /// Set a FUJSEN function in a section
        /// </summary>
        public void SetFujsen(string section, string key, Delegate function)
        {
            // Convert delegate to string representation (simplified)
            var code = $"// Function: {function.Method.Name}\n// Parameters: {string.Join(", ", function.Method.GetParameters().Select(p => p.Name))}";
            SetValue(section, key, code);
        }

        /// <summary>
        /// Execute FUJSEN @ operators
        /// </summary>
        public async Task<object> ExecuteOperators(object value, Dictionary<string, object> context = null)
        {
            if (value is string strValue && strValue.Contains("@"))
            {
                return await ExecuteOperator(strValue, context ?? new Dictionary<string, object>());
            }
            return value;
        }

        /// <summary>
        /// Execute a single @ operator
        /// </summary>
        private async Task<object> ExecuteOperator(string expression, Dictionary<string, object> context)
        {
            var operatorMatch = Regex.Match(expression, @"@(\w+)\s*\(([^)]*)\)");
            if (!operatorMatch.Success) return expression;

            var operatorName = operatorMatch.Groups[1].Value;
            var operatorArgs = operatorMatch.Groups[2].Value;

            return operatorName.ToLower() switch
            {
                "query" => await ExecuteQuery(operatorArgs, context),
                "cache" => await ExecuteCache(operatorArgs, context),
                "metrics" => ExecuteMetrics(operatorArgs, context),
                "if" => ExecuteIf(operatorArgs, context),
                "date" => ExecuteDate(operatorArgs, context),
                "env" => ExecuteEnv(operatorArgs, context),
                "optimize" => ExecuteOptimize(operatorArgs, context),
                "learn" => ExecuteLearn(operatorArgs, context),
                "feature" => ExecuteFeature(operatorArgs, context),
                "json" => ExecuteJson(operatorArgs, context),
                "request" => await ExecuteRequest(operatorArgs, context),
                "file" => await ExecuteFile(operatorArgs, context),
                "flex" => await ExecuteFlex(operatorArgs, context),
                _ => expression
            };
        }

        // @ Operator implementations
        private async Task<object> ExecuteQuery(string expression, Dictionary<string, object> context)
        {
            // Mock database query - in real implementation, connect to actual database
            return new Dictionary<string, object>
            {
                ["results"] = new List<Dictionary<string, object>>(),
                ["count"] = 0,
                ["query"] = expression
            };
        }

        private async Task<object> ExecuteCache(string expression, Dictionary<string, object> context)
        {
            // Mock cache implementation
            var parts = expression.Split(',');
            var ttl = parts.Length > 0 ? ParseTtl(parts[0].Trim()) : 300.0;
            var key = parts.Length > 1 ? parts[1].Trim().Trim('"') : "default";
            
            return new Dictionary<string, object>
            {
                ["cached"] = true,
                ["ttl"] = ttl,
                ["key"] = key,
                ["value"] = context.GetValueOrDefault("cache_value", "default")
            };
        }

        private object ExecuteMetrics(string expression, Dictionary<string, object> context)
        {
            var parts = expression.Split(',');
            var metricName = parts[0].Trim().Trim('"');
            var value = parts.Length > 1 ? double.Parse(parts[1].Trim()) : 0.0;
            
            return new Dictionary<string, object>
            {
                ["metric"] = metricName,
                ["value"] = value,
                ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
        }

        private object ExecuteIf(string expression, Dictionary<string, object> context)
        {
            var parts = expression.Split(',');
            if (parts.Length < 3) return false;
            
            var condition = parts[0].Trim();
            var trueValue = parts[1].Trim().Trim('"');
            var falseValue = parts[2].Trim().Trim('"');
            
            // Simple condition evaluation
            var result = EvaluateCondition(condition, context);
            return result ? trueValue : falseValue;
        }

        private object ExecuteDate(string expression, Dictionary<string, object> context)
        {
            var format = string.IsNullOrEmpty(expression) ? "yyyy-MM-dd HH:mm:ss" : expression.Trim().Trim('"');
            return DateTime.Now.ToString(format);
        }

        private object ExecuteEnv(string expression, Dictionary<string, object> context)
        {
            var varName = expression.Trim().Trim('"');
            return Environment.GetEnvironmentVariable(varName);
        }

        private object ExecuteOptimize(string expression, Dictionary<string, object> context)
        {
            return new Dictionary<string, object>
            {
                ["optimized"] = true,
                ["parameter"] = expression.Trim().Trim('"'),
                ["value"] = context.GetValueOrDefault("optimize_value", "default")
            };
        }

        private object ExecuteLearn(string expression, Dictionary<string, object> context)
        {
            return new Dictionary<string, object>
            {
                ["learned"] = true,
                ["parameter"] = expression.Trim().Trim('"'),
                ["value"] = context.GetValueOrDefault("learn_value", "default")
            };
        }

        private object ExecuteFeature(string expression, Dictionary<string, object> context)
        {
            var featureName = expression.Trim().Trim('"');
            // Mock feature detection
            return featureName switch
            {
                "redis" => true,
                "postgresql" => true,
                "sqlite" => true,
                "azure" => true,
                "unity" => true,
                _ => false
            };
        }

        private object ExecuteJson(string expression, Dictionary<string, object> context)
        {
            try
            {
                return System.Text.Json.JsonSerializer.Serialize(context);
            }
            catch
            {
                return "{}";
            }
        }

        private async Task<object> ExecuteRequest(string expression, Dictionary<string, object> context)
        {
            // Mock HTTP request - in real implementation, use HttpClient
            return new Dictionary<string, object>
            {
                ["status"] = 200,
                ["data"] = new Dictionary<string, object>(),
                ["url"] = expression.Trim().Trim('"')
            };
        }

        private async Task<object> ExecuteFile(string expression, Dictionary<string, object> context)
        {
            var filepath = expression.Trim().Trim('"');
            try
            {
                return await File.ReadAllTextAsync(filepath);
            }
            catch
            {
                return null;
            }
        }

        private async Task<object> ExecuteFlex(string expression, Dictionary<string, object> context)
        {
            // FlexChain integration
            var parts = expression.Split(',');
            var operation = parts[0].Trim().Trim('"');
            
            return operation switch
            {
                "balance" => await GetFlexBalance(parts.Length > 1 ? parts[1].Trim().Trim('"') : ""),
                "transfer" => await ExecuteFlexTransfer(parts.Skip(1).ToArray()),
                "stake" => await ExecuteFlexStake(parts.Skip(1).ToArray()),
                "unstake" => await ExecuteFlexUnstake(parts.Skip(1).ToArray()),
                "reward" => await GetFlexReward(parts.Length > 1 ? parts[1].Trim().Trim('"') : ""),
                "status" => await GetFlexStatus(),
                "delegate" => await ExecuteFlexDelegate(parts.Skip(1).ToArray()),
                "vote" => await ExecuteFlexVote(parts.Skip(1).ToArray()),
                _ => new Dictionary<string, object> { ["error"] = $"Unknown Flex operation: {operation}" }
            };
        }

        // FlexChain operation implementations
        private async Task<object> GetFlexBalance(string address)
        {
            return new Dictionary<string, object>
            {
                ["address"] = address,
                ["balance"] = 1000.0,
                ["currency"] = "FLEX"
            };
        }

        private async Task<object> ExecuteFlexTransfer(string[] args)
        {
            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["transaction_id"] = $"tx_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                ["amount"] = args.Length > 0 ? double.Parse(args[0]) : 0.0,
                ["to"] = args.Length > 1 ? args[1].Trim('"') : "",
                ["from"] = args.Length > 2 ? args[2].Trim('"') : ""
            };
        }

        private async Task<object> ExecuteFlexStake(string[] args)
        {
            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["staked_amount"] = args.Length > 0 ? double.Parse(args[0]) : 0.0,
                ["validator"] = args.Length > 1 ? args[1].Trim('"') : ""
            };
        }

        private async Task<object> ExecuteFlexUnstake(string[] args)
        {
            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["unstaked_amount"] = args.Length > 0 ? double.Parse(args[0]) : 0.0,
                ["validator"] = args.Length > 1 ? args[1].Trim('"') : ""
            };
        }

        private async Task<object> GetFlexReward(string address)
        {
            return new Dictionary<string, object>
            {
                ["address"] = address,
                ["reward"] = 50.0,
                ["currency"] = "FLEX"
            };
        }

        private async Task<object> GetFlexStatus()
        {
            return new Dictionary<string, object>
            {
                ["network"] = "FlexChain",
                ["status"] = "active",
                ["block_height"] = 12345,
                ["validators"] = 10,
                ["total_staked"] = 1000000.0
            };
        }

        private async Task<object> ExecuteFlexDelegate(string[] args)
        {
            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["delegated_amount"] = args.Length > 0 ? double.Parse(args[0]) : 0.0,
                ["validator"] = args.Length > 1 ? args[1].Trim('"') : "",
                ["delegator"] = args.Length > 2 ? args[2].Trim('"') : ""
            };
        }

        private async Task<object> ExecuteFlexVote(string[] args)
        {
            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["proposal"] = args.Length > 0 ? args[0].Trim('"') : "",
                ["vote"] = args.Length > 1 ? args[1].Trim('"') : "",
                ["voter"] = args.Length > 2 ? args[2].Trim('"') : ""
            };
        }

        // Helper methods
        private double ParseTtl(string ttl)
        {
            if (double.TryParse(ttl, out var seconds)) return seconds;
            
            var match = Regex.Match(ttl, @"(\d+)([smhd])");
            if (!match.Success) return 300.0;
            
            var value = double.Parse(match.Groups[1].Value);
            var unit = match.Groups[2].Value;
            
            return unit switch
            {
                "s" => value,
                "m" => value * 60,
                "h" => value * 3600,
                "d" => value * 86400,
                _ => 300.0
            };
        }

        private bool EvaluateCondition(string condition, Dictionary<string, object> context)
        {
            // Simple condition evaluation
            if (condition.Contains("=="))
            {
                var parts = condition.Split("==");
                var left = parts[0].Trim().Trim('"');
                var right = parts[1].Trim().Trim('"');
                return context.GetValueOrDefault(left, "").ToString() == right;
            }
            
            if (condition.Contains("!="))
            {
                var parts = condition.Split("!=");
                var left = parts[0].Trim().Trim('"');
                var right = parts[1].Trim().Trim('"');
                return context.GetValueOrDefault(left, "").ToString() != right;
            }
            
            return false;
        }
    }
} 