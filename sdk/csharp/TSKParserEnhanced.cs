using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace TuskLang
{
    /// <summary>
    /// TuskLang Enhanced Parser for C#
    /// "We don't bow to any king" - Support ALL syntax styles
    /// 
    /// Features:
    /// - Multiple grouping: [], {}, <>
    /// - $global vs section-local variables
    /// - Cross-file communication
    /// - Database queries (placeholder adapters)
    /// - All @ operators
    /// - Maximum flexibility
    /// 
    /// DEFAULT CONFIG: peanut.tsk (the bridge of language grace)
    /// </summary>
    public class TSKParserEnhanced
    {
        private Dictionary<string, object> data = new Dictionary<string, object>();
        private Dictionary<string, object> globalVariables = new Dictionary<string, object>();
        private Dictionary<string, object> sectionVariables = new Dictionary<string, object>();
        private Dictionary<string, object> cache = new Dictionary<string, object>();
        private Dictionary<string, object> crossFileCache = new Dictionary<string, object>();
        private string currentSection = "";
        private bool inObject = false;
        private string objectKey = "";
        private bool peanutLoaded = false;
        
        // Standard peanut.tsk locations
        private readonly string[] peanutLocations = {
            "./peanut.tsk",
            "../peanut.tsk",
            "../../peanut.tsk",
            "/etc/tusklang/peanut.tsk",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config/tusklang/peanut.tsk"),
            Environment.GetEnvironmentVariable("TUSKLANG_CONFIG") ?? ""
        };
        
        /// <summary>
        /// Load peanut.tsk if available
        /// </summary>
        public void LoadPeanut()
        {
            if (peanutLoaded) return;
            
            peanutLoaded = true; // Mark first to prevent recursion
            
            foreach (var location in peanutLocations)
            {
                if (string.IsNullOrEmpty(location)) continue;
                
                if (File.Exists(location))
                {
                    Console.WriteLine($"# Loading universal config from: {location}");
                    ParseFile(location);
                    return;
                }
            }
        }
        
        /// <summary>
        /// Parse TuskLang value with all syntax support
        /// </summary>
        public object ParseValue(string value)
        {
            value = value.Trim();
            
            // Remove optional semicolon
            if (value.EndsWith(";"))
            {
                value = value.TrimEnd(';').Trim();
            }
            
            // Basic types
            switch (value)
            {
                case "true": return true;
                case "false": return false;
                case "null": return null;
            }
            
            // Numbers
            if (int.TryParse(value, out int intVal)) return intVal;
            if (double.TryParse(value, out double doubleVal)) return doubleVal;
            
            // $variable references (global)
            var globalVarRegex = new Regex(@"^\$([a-zA-Z_][a-zA-Z0-9_]*)$");
            var globalMatch = globalVarRegex.Match(value);
            if (globalMatch.Success)
            {
                var varName = globalMatch.Groups[1].Value;
                return globalVariables.ContainsKey(varName) ? globalVariables[varName] : "";
            }
            
            // Section-local variable references
            if (!string.IsNullOrEmpty(currentSection))
            {
                var localVarRegex = new Regex(@"^[a-zA-Z_][a-zA-Z0-9_]*$");
                if (localVarRegex.IsMatch(value))
                {
                    var sectionKey = $"{currentSection}.{value}";
                    if (sectionVariables.ContainsKey(sectionKey))
                        return sectionVariables[sectionKey];
                }
            }
            
            // @date function
            var dateRegex = new Regex(@"^@date\([""'](.*)[""]?\)$");
            var dateMatch = dateRegex.Match(value);
            if (dateMatch.Success)
            {
                var formatStr = dateMatch.Groups[1].Value;
                return ExecuteDate(formatStr);
            }
            
            // @env function with default
            var envRegex = new Regex(@"^@env\([""']([^""']*)[""]?(?:,\s*(.+))?\)$");
            var envMatch = envRegex.Match(value);
            if (envMatch.Success)
            {
                var envVar = envMatch.Groups[1].Value;
                var defaultVal = envMatch.Groups.Count > 2 && !string.IsNullOrEmpty(envMatch.Groups[2].Value)
                    ? envMatch.Groups[2].Value.Trim('"', '\'')
                    : "";
                return Environment.GetEnvironmentVariable(envVar) ?? defaultVal;
            }
            
            // Ranges: 8000-9000
            var rangeRegex = new Regex(@"^(\d+)-(\d+)$");
            var rangeMatch = rangeRegex.Match(value);
            if (rangeMatch.Success)
            {
                var min = int.Parse(rangeMatch.Groups[1].Value);
                var max = int.Parse(rangeMatch.Groups[2].Value);
                return new Dictionary<string, object>
                {
                    ["min"] = min,
                    ["max"] = max,
                    ["type"] = "range"
                };
            }
            
            // Arrays
            if (value.StartsWith("[") && value.EndsWith("]"))
            {
                return ParseArray(value);
            }
            
            // Objects
            if (value.StartsWith("{") && value.EndsWith("}"))
            {
                return ParseObject(value);
            }
            
            // Cross-file references: @file.tsk.get('key')
            var crossGetRegex = new Regex(@"^@([a-zA-Z0-9_-]+)\.tsk\.get\([""'](.*)[""]?\)$");
            var crossGetMatch = crossGetRegex.Match(value);
            if (crossGetMatch.Success)
            {
                var fileName = crossGetMatch.Groups[1].Value;
                var key = crossGetMatch.Groups[2].Value;
                return CrossFileGet(fileName, key);
            }
            
            // Cross-file set: @file.tsk.set('key', value)
            var crossSetRegex = new Regex(@"^@([a-zA-Z0-9_-]+)\.tsk\.set\([""']([^""']*)[""]?,\s*(.+)\)$");
            var crossSetMatch = crossSetRegex.Match(value);
            if (crossSetMatch.Success)
            {
                var fileName = crossSetMatch.Groups[1].Value;
                var key = crossSetMatch.Groups[2].Value;
                var val = crossSetMatch.Groups[3].Value;
                return CrossFileSet(fileName, key, val);
            }
            
            // @query function
            var queryRegex = new Regex(@"^@query\([""'](.*)[""]?(.*)\)$");
            var queryMatch = queryRegex.Match(value);
            if (queryMatch.Success)
            {
                var query = queryMatch.Groups[1].Value;
                return ExecuteQuery(query);
            }
            
            // @ operators
            var operatorRegex = new Regex(@"^@([a-zA-Z_][a-zA-Z0-9_]*)\((.+)\)$");
            var operatorMatch = operatorRegex.Match(value);
            if (operatorMatch.Success)
            {
                var operatorName = operatorMatch.Groups[1].Value;
                var parameters = operatorMatch.Groups[2].Value;
                return ExecuteOperator(operatorName, parameters);
            }
            
            // String concatenation
            if (value.Contains(" + "))
            {
                var parts = value.Split(new[] { " + " }, StringSplitOptions.None);
                var result = "";
                foreach (var part in parts)
                {
                    var trimmedPart = part.Trim().Trim('"', '\'');
                    if (!part.Trim().StartsWith("\""))
                    {
                        var parsedPart = ParseValue(trimmedPart);
                        result += parsedPart?.ToString() ?? "";
                    }
                    else
                    {
                        result += trimmedPart;
                    }
                }
                return result;
            }
            
            // Conditional/ternary: condition ? true_val : false_val
            var ternaryRegex = new Regex(@"(.+?)\s*\?\s*(.+?)\s*:\s*(.+)");
            var ternaryMatch = ternaryRegex.Match(value);
            if (ternaryMatch.Success)
            {
                var condition = ternaryMatch.Groups[1].Value.Trim();
                var trueVal = ternaryMatch.Groups[2].Value.Trim();
                var falseVal = ternaryMatch.Groups[3].Value.Trim();
                
                if (EvaluateCondition(condition))
                {
                    return ParseValue(trueVal);
                }
                else
                {
                    return ParseValue(falseVal);
                }
            }
            
            // Remove quotes from strings
            if ((value.StartsWith("\"") && value.EndsWith("\"")) ||
                (value.StartsWith("'") && value.EndsWith("'")))
            {
                return value.Substring(1, value.Length - 2);
            }
            
            // Return as-is
            return value;
        }
        
        /// <summary>
        /// Parse array syntax
        /// </summary>
        private List<object> ParseArray(string value)
        {
            var content = value.Substring(1, value.Length - 2).Trim();
            if (string.IsNullOrEmpty(content))
                return new List<object>();
            
            var items = new List<object>();
            var current = "";
            var depth = 0;
            var inString = false;
            var quoteChar = '\0';
            
            for (int i = 0; i < content.Length; i++)
            {
                var ch = content[i];
                
                if ((ch == '"' || ch == '\'') && !inString)
                {
                    inString = true;
                    quoteChar = ch;
                }
                else if (ch == quoteChar && inString)
                {
                    inString = false;
                    quoteChar = '\0';
                }
                
                if (!inString)
                {
                    if (ch == '[' || ch == '{') depth++;
                    else if (ch == ']' || ch == '}') depth--;
                    else if (ch == ',' && depth == 0)
                    {
                        items.Add(ParseValue(current.Trim()));
                        current = "";
                        continue;
                    }
                }
                
                current += ch;
            }
            
            if (!string.IsNullOrWhiteSpace(current))
            {
                items.Add(ParseValue(current.Trim()));
            }
            
            return items;
        }
        
        /// <summary>
        /// Parse object syntax
        /// </summary>
        private Dictionary<string, object> ParseObject(string value)
        {
            var content = value.Substring(1, value.Length - 2).Trim();
            if (string.IsNullOrEmpty(content))
                return new Dictionary<string, object>();
            
            var pairs = new List<string>();
            var current = "";
            var depth = 0;
            var inString = false;
            var quoteChar = '\0';
            
            for (int i = 0; i < content.Length; i++)
            {
                var ch = content[i];
                
                if ((ch == '"' || ch == '\'') && !inString)
                {
                    inString = true;
                    quoteChar = ch;
                }
                else if (ch == quoteChar && inString)
                {
                    inString = false;
                    quoteChar = '\0';
                }
                
                if (!inString)
                {
                    if (ch == '[' || ch == '{') depth++;
                    else if (ch == ']' || ch == '}') depth--;
                    else if (ch == ',' && depth == 0)
                    {
                        pairs.Add(current.Trim());
                        current = "";
                        continue;
                    }
                }
                
                current += ch;
            }
            
            if (!string.IsNullOrWhiteSpace(current))
            {
                pairs.Add(current.Trim());
            }
            
            var obj = new Dictionary<string, object>();
            foreach (var pair in pairs)
            {
                if (pair.Contains(":"))
                {
                    var colonIndex = pair.IndexOf(':');
                    var key = pair.Substring(0, colonIndex).Trim().Trim('"', '\'');
                    var val = pair.Substring(colonIndex + 1).Trim();
                    obj[key] = ParseValue(val);
                }
                else if (pair.Contains("="))
                {
                    var equalsIndex = pair.IndexOf('=');
                    var key = pair.Substring(0, equalsIndex).Trim().Trim('"', '\'');
                    var val = pair.Substring(equalsIndex + 1).Trim();
                    obj[key] = ParseValue(val);
                }
            }
            
            return obj;
        }
        
        /// <summary>
        /// Evaluate conditions for ternary expressions
        /// </summary>
        private bool EvaluateCondition(string condition)
        {
            condition = condition.Trim();
            
            // Simple equality check
            if (condition.Contains("=="))
            {
                var parts = condition.Split(new[] { "==" }, StringSplitOptions.None);
                var left = ParseValue(parts[0].Trim());
                var right = ParseValue(parts[1].Trim());
                return left?.ToString() == right?.ToString();
            }
            
            // Not equal
            if (condition.Contains("!="))
            {
                var parts = condition.Split(new[] { "!=" }, StringSplitOptions.None);
                var left = ParseValue(parts[0].Trim());
                var right = ParseValue(parts[1].Trim());
                return left?.ToString() != right?.ToString();
            }
            
            // Greater than
            if (condition.Contains(">"))
            {
                var parts = condition.Split('>');
                var left = ParseValue(parts[0].Trim());
                var right = ParseValue(parts[1].Trim());
                
                if (left is IComparable leftComp && right is IComparable rightComp)
                {
                    return leftComp.CompareTo(rightComp) > 0;
                }
                return left?.ToString().CompareTo(right?.ToString()) > 0;
            }
            
            // Default: check if truthy
            var value = ParseValue(condition);
            return value switch
            {
                bool b => b,
                string s => !string.IsNullOrEmpty(s) && s != "false" && s != "null" && s != "0",
                int i => i != 0,
                double d => d != 0.0,
                null => false,
                _ => true
            };
        }
        
        /// <summary>
        /// Get value from another TSK file
        /// </summary>
        private object CrossFileGet(string fileName, string key)
        {
            var cacheKey = $"{fileName}:{key}";
            
            // Check cache
            if (crossFileCache.ContainsKey(cacheKey))
                return crossFileCache[cacheKey];
            
            // Find file
            var directories = new[] { ".", "./config", "..", "../config" };
            string filePath = null;
            
            foreach (var directory in directories)
            {
                var potentialPath = Path.Combine(directory, $"{fileName}.tsk");
                if (File.Exists(potentialPath))
                {
                    filePath = potentialPath;
                    break;
                }
            }
            
            if (filePath == null) return "";
            
            // Parse file and get value
            var tempParser = new TSKParserEnhanced();
            tempParser.ParseFile(filePath);
            
            var value = tempParser.Get(key);
            
            // Cache result
            crossFileCache[cacheKey] = value;
            
            return value;
        }
        
        /// <summary>
        /// Set value in another TSK file (cache only for now)
        /// </summary>
        private object CrossFileSet(string fileName, string key, string value)
        {
            var cacheKey = $"{fileName}:{key}";
            var parsedValue = ParseValue(value);
            crossFileCache[cacheKey] = parsedValue;
            return parsedValue;
        }
        
        /// <summary>
        /// Execute @date function
        /// </summary>
        private string ExecuteDate(string formatStr)
        {
            var now = DateTime.Now;
            
            // Convert PHP-style format to C#
            return formatStr switch
            {
                "Y" => now.ToString("yyyy"),
                "Y-m-d" => now.ToString("yyyy-MM-dd"),
                "Y-m-d H:i:s" => now.ToString("yyyy-MM-dd HH:mm:ss"),
                "c" => now.ToString("yyyy-MM-ddTHH:mm:ssK"),
                _ => now.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }
        
        /// <summary>
        /// Execute database query (placeholder for now)
        /// </summary>
        private string ExecuteQuery(string query)
        {
            LoadPeanut();
            
            // Determine database type
            var dbType = Get("database.default")?.ToString() ?? "sqlite";
            
            // Placeholder implementation
            return $"[Query: {query} on {dbType}]";
        }
        
        /// <summary>
        /// Execute @ operators
        /// </summary>
        private object ExecuteOperator(string operatorName, string parameters)
        {
            switch (operatorName)
            {
                case "cache":
                    // Simple cache implementation
                    var parts = parameters.Split(',', 2);
                    if (parts.Length == 2)
                    {
                        var ttl = parts[0].Trim().Trim('"', '\'');
                        var value = parts[1].Trim();
                        return ParseValue(value);
                    }
                    return "";
                
                case "learn":
                case "optimize":
                case "metrics":
                case "feature":
                    // Placeholders for advanced features
                    return $"@{operatorName}({parameters})";
                
                default:
                    return $"@{operatorName}({parameters})";
            }
        }
        
        /// <summary>
        /// Parse a single line
        /// </summary>
        public void ParseLine(string line)
        {
            var trimmed = line.Trim();
            
            // Skip empty lines and comments
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                return;
            
            // Remove optional semicolon
            if (trimmed.EndsWith(";"))
            {
                trimmed = trimmed.TrimEnd(';').Trim();
            }
            
            // Check for section declaration []
            var sectionRegex = new Regex(@"^\[([a-zA-Z_][a-zA-Z0-9_]*)\]$");
            var sectionMatch = sectionRegex.Match(trimmed);
            if (sectionMatch.Success)
            {
                currentSection = sectionMatch.Groups[1].Value;
                inObject = false;
                return;
            }
            
            // Check for angle bracket object >
            var angleOpenRegex = new Regex(@"^([a-zA-Z_][a-zA-Z0-9_]*)\s*>$");
            var angleOpenMatch = angleOpenRegex.Match(trimmed);
            if (angleOpenMatch.Success)
            {
                inObject = true;
                objectKey = angleOpenMatch.Groups[1].Value;
                return;
            }
            
            // Check for closing angle bracket <
            if (trimmed == "<")
            {
                inObject = false;
                objectKey = "";
                return;
            }
            
            // Check for curly brace object {
            var braceOpenRegex = new Regex(@"^([a-zA-Z_][a-zA-Z0-9_]*)\s*\{$");
            var braceOpenMatch = braceOpenRegex.Match(trimmed);
            if (braceOpenMatch.Success)
            {
                inObject = true;
                objectKey = braceOpenMatch.Groups[1].Value;
                return;
            }
            
            // Check for closing curly brace }
            if (trimmed == "}")
            {
                inObject = false;
                objectKey = "";
                return;
            }
            
            // Parse key-value pairs (both : and = supported)
            var kvRegex = new Regex(@"^([\$]?[a-zA-Z_][a-zA-Z0-9_-]*)\s*[:=]\s*(.+)$");
            var kvMatch = kvRegex.Match(trimmed);
            if (kvMatch.Success)
            {
                var key = kvMatch.Groups[1].Value;
                var value = kvMatch.Groups[2].Value;
                var parsedValue = ParseValue(value);
                
                // Determine storage location
                string storageKey;
                if (inObject && !string.IsNullOrEmpty(objectKey))
                {
                    storageKey = !string.IsNullOrEmpty(currentSection)
                        ? $"{currentSection}.{objectKey}.{key}"
                        : $"{objectKey}.{key}";
                }
                else if (!string.IsNullOrEmpty(currentSection))
                {
                    storageKey = $"{currentSection}.{key}";
                }
                else
                {
                    storageKey = key;
                }
                
                // Store the value
                data[storageKey] = parsedValue;
                
                // Handle global variables
                if (key.StartsWith("$"))
                {
                    var varName = key.Substring(1);
                    globalVariables[varName] = parsedValue;
                }
                else if (!string.IsNullOrEmpty(currentSection) && !key.StartsWith("$"))
                {
                    // Store section-local variable
                    var sectionKey = $"{currentSection}.{key}";
                    sectionVariables[sectionKey] = parsedValue;
                }
            }
        }
        
        /// <summary>
        /// Parse TuskLang content
        /// </summary>
        public Dictionary<string, object> Parse(string content)
        {
            var lines = content.Split('\n');
            
            foreach (var line in lines)
            {
                ParseLine(line);
            }
            
            return data;
        }
        
        /// <summary>
        /// Parse a TSK file
        /// </summary>
        public void ParseFile(string filePath)
        {
            var content = File.ReadAllText(filePath);
            Parse(content);
        }
        
        /// <summary>
        /// Get a value by key
        /// </summary>
        public object Get(string key)
        {
            return data.ContainsKey(key) ? data[key] : null;
        }
        
        /// <summary>
        /// Set a value
        /// </summary>
        public void Set(string key, object value)
        {
            data[key] = value;
        }
        
        /// <summary>
        /// Get all keys
        /// </summary>
        public IEnumerable<string> Keys => data.Keys;
        
        /// <summary>
        /// Get all key-value pairs
        /// </summary>
        public Dictionary<string, object> Items => new Dictionary<string, object>(data);
        
        /// <summary>
        /// Convert to JSON string
        /// </summary>
        public string ToJson()
        {
            return JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        }
        
        /// <summary>
        /// Load configuration from peanut.tsk
        /// </summary>
        public static TSKParserEnhanced LoadFromPeanut()
        {
            var parser = new TSKParserEnhanced();
            parser.LoadPeanut();
            return parser;
        }
    }
}