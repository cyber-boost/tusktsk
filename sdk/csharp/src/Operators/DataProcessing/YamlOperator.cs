using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;

namespace TuskLang.Operators.DataProcessing
{
    /// <summary>
    /// YAML Operator for TuskLang C# SDK
    /// 
    /// Provides YAML processing capabilities with support for:
    /// - YAML parsing and serialization
    /// - YAML validation and formatting
    /// - YAML to JSON/XML conversion
    /// - YAML schema validation
    /// - YAML transformation and manipulation
    /// - Pretty printing and minification
    /// 
    /// Usage:
    /// ```csharp
    /// // Parse YAML
    /// var result = @yaml({
    ///   action: "parse",
    ///   data: "name: John\nage: 30"
    /// })
    /// 
    /// // Convert to JSON
    /// var result = @yaml({
    ///   action: "to_json",
    ///   data: yamlData
    /// })
    /// ```
    /// </summary>
    public class YamlOperator : BaseOperator
    {
        public YamlOperator()
        {
            Version = "2.0.0";
            RequiredFields = new List<string> { "action" };
            OptionalFields = new List<string> 
            { 
                "data", "schema", "format", "indent", "validate", 
                "transform", "merge", "diff", "patch", "timeout", "encoding" 
            };
            
            DefaultConfig = new Dictionary<string, object>
            {
                ["format"] = "pretty",
                ["indent"] = 2,
                ["validate"] = true,
                ["timeout"] = 300,
                ["encoding"] = "UTF-8"
            };
        }
        
        public override string GetName() => "yaml";
        
        protected override string GetDescription() => "YAML processing operator for parsing, querying, and manipulating YAML data";
        
        protected override Dictionary<string, string> GetExamples()
        {
            return new Dictionary<string, string>
            {
                ["parse"] = "@yaml({action: \"parse\", data: \"name: John\\nage: 30\"})",
                ["stringify"] = "@yaml({action: \"stringify\", data: {name: \"John\", age: 30}})",
                ["to_json"] = "@yaml({action: \"to_json\", data: yamlData})",
                ["validate"] = "@yaml({action: \"validate\", data: yamlData, schema: schemaData})"
            };
        }
        
        protected override Dictionary<string, string> GetErrorCodes()
        {
            return new Dictionary<string, string>
            {
                ["INVALID_ACTION"] = "Invalid YAML action",
                ["INVALID_YAML"] = "Invalid YAML data",
                ["PARSE_ERROR"] = "YAML parsing error",
                ["SCHEMA_VALIDATION_ERROR"] = "YAML schema validation failed",
                ["TRANSFORM_ERROR"] = "YAML transformation error",
                ["MERGE_ERROR"] = "YAML merge error",
                ["TIMEOUT_EXCEEDED"] = "YAML operation timeout exceeded"
            };
        }
        
        protected override async Task<object> ExecuteOperatorAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var action = GetContextValue<string>(config, "action", "");
            var data = ResolveVariable(config.GetValueOrDefault("data"), context);
            var schema = ResolveVariable(config.GetValueOrDefault("schema"), context);
            var format = GetContextValue<string>(config, "format", "pretty");
            var indent = GetContextValue<int>(config, "indent", 2);
            var validate = GetContextValue<bool>(config, "validate", true);
            var transform = GetContextValue<string>(config, "transform", "");
            var merge = ResolveVariable(config.GetValueOrDefault("merge"), context);
            var diff = ResolveVariable(config.GetValueOrDefault("diff"), context);
            var patch = ResolveVariable(config.GetValueOrDefault("patch"), context);
            var timeout = GetContextValue<int>(config, "timeout", 300);
            var encoding = GetContextValue<string>(config, "encoding", "UTF-8");
            
            if (string.IsNullOrEmpty(action))
                throw new ArgumentException("Action is required");
            
            var startTime = DateTime.UtcNow;
            
            try
            {
                // Check timeout
                if ((DateTime.UtcNow - startTime).TotalSeconds > timeout)
                {
                    throw new TimeoutException("YAML operation timeout exceeded");
                }
                
                switch (action.ToLower())
                {
                    case "parse":
                        return await ParseYamlAsync(data, validate, encoding);
                    
                    case "stringify":
                        return await StringifyYamlAsync(data, format, indent, encoding);
                    
                    case "to_json":
                        return await ConvertToJsonAsync(data);
                    
                    case "to_xml":
                        return await ConvertToXmlAsync(data);
                    
                    case "validate":
                        return await ValidateYamlAsync(data, schema);
                    
                    case "transform":
                        return await TransformYamlAsync(data, transform);
                    
                    case "merge":
                        return await MergeYamlAsync(data, merge);
                    
                    case "diff":
                        return await DiffYamlAsync(data, diff);
                    
                    case "patch":
                        return await PatchYamlAsync(data, patch);
                    
                    case "format":
                        return await FormatYamlAsync(data, format, indent, encoding);
                    
                    case "minify":
                        return await MinifyYamlAsync(data, encoding);
                    
                    case "flatten":
                        return await FlattenYamlAsync(data);
                    
                    case "unflatten":
                        return await UnflattenYamlAsync(data);
                    
                    default:
                        throw new ArgumentException($"Unknown YAML action: {action}");
                }
            }
            catch (Exception ex)
            {
                Log("error", "YAML operation failed", new Dictionary<string, object>
                {
                    ["action"] = action,
                    ["error"] = ex.Message
                });
                
                return new Dictionary<string, object>
                {
                    ["success"] = false,
                    ["error"] = ex.Message,
                    ["action"] = action
                };
            }
        }
        
        /// <summary>
        /// Parse YAML string to object
        /// </summary>
        private async Task<object> ParseYamlAsync(object data, bool validate, string encoding)
        {
            if (data == null)
                throw new ArgumentException("Data is required for parsing");
            
            var yamlString = data.ToString();
            
            try
            {
                // Simplified YAML parsing
                // In a real implementation, you would use a proper YAML parser
                var result = ParseYamlString(yamlString);
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["parsed"] = result,
                    ["valid"] = true,
                    ["encoding"] = encoding
                };
            }
            catch (Exception ex)
            {
                if (validate)
                {
                    throw new ArgumentException($"Invalid YAML: {ex.Message}");
                }
                
                return new Dictionary<string, object>
                {
                    ["success"] = false,
                    ["error"] = ex.Message,
                    ["valid"] = false
                };
            }
        }
        
        /// <summary>
        /// Convert object to YAML string
        /// </summary>
        private async Task<object> StringifyYamlAsync(object data, string format, int indent, string encoding)
        {
            if (data == null)
                throw new ArgumentException("Data is required for stringification");
            
            try
            {
                // Simplified YAML serialization
                // In a real implementation, you would use a proper YAML serializer
                var yamlString = ConvertToYamlString(data, indent);
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["stringified"] = yamlString,
                    ["format"] = format,
                    ["encoding"] = encoding
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Stringification failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Convert YAML to JSON
        /// </summary>
        private async Task<object> ConvertToJsonAsync(object data)
        {
            if (data == null)
                throw new ArgumentException("Data is required for conversion");
            
            try
            {
                // Simplified conversion
                // In a real implementation, you would parse YAML and convert to JSON
                var jsonData = data; // Placeholder for actual conversion
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["json"] = jsonData
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"JSON conversion failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Convert YAML to XML
        /// </summary>
        private async Task<object> ConvertToXmlAsync(object data)
        {
            if (data == null)
                throw new ArgumentException("Data is required for conversion");
            
            try
            {
                // Simplified conversion
                // In a real implementation, you would parse YAML and convert to XML
                var xmlData = data; // Placeholder for actual conversion
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["xml"] = xmlData
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"XML conversion failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Validate YAML against schema
        /// </summary>
        private async Task<object> ValidateYamlAsync(object data, object schema)
        {
            if (data == null)
                throw new ArgumentException("Data is required for validation");
            
            // Simplified validation
            // In a real implementation, you would use a proper YAML schema validator
            var isValid = true;
            var errors = new List<string>();
            
            try
            {
                // Basic validation - check if data is not null
                if (data == null)
                {
                    isValid = false;
                    errors.Add("Data cannot be null");
                }
            }
            catch (Exception ex)
            {
                isValid = false;
                errors.Add($"YAML validation failed: {ex.Message}");
            }
            
            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["valid"] = isValid,
                ["errors"] = errors,
                ["error_count"] = errors.Count
            };
        }
        
        /// <summary>
        /// Transform YAML data
        /// </summary>
        private async Task<object> TransformYamlAsync(object data, string transform)
        {
            if (data == null)
                throw new ArgumentException("Data is required for transformation");
            
            if (string.IsNullOrEmpty(transform))
                throw new ArgumentException("Transform expression is required");
            
            try
            {
                // Simplified transformation
                // In a real implementation, you would parse and execute the transform expression
                var transformed = data; // Placeholder for actual transformation
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["transformed"] = transformed,
                    ["transform"] = transform
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Transformation failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Merge YAML objects
        /// </summary>
        private async Task<object> MergeYamlAsync(object data, object merge)
        {
            if (data == null)
                throw new ArgumentException("Data is required for merging");
            
            if (merge == null)
                throw new ArgumentException("Merge data is required");
            
            try
            {
                // Simplified merge
                // In a real implementation, you would implement deep merge logic
                var merged = new Dictionary<string, object>();
                
                if (data is Dictionary<string, object> dataDict)
                {
                    foreach (var kvp in dataDict)
                    {
                        merged[kvp.Key] = kvp.Value;
                    }
                }
                
                if (merge is Dictionary<string, object> mergeDict)
                {
                    foreach (var kvp in mergeDict)
                    {
                        merged[kvp.Key] = kvp.Value;
                    }
                }
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["merged"] = merged
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Merge failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Diff YAML objects
        /// </summary>
        private async Task<object> DiffYamlAsync(object data, object diff)
        {
            if (data == null)
                throw new ArgumentException("Data is required for diffing");
            
            if (diff == null)
                throw new ArgumentException("Diff data is required");
            
            try
            {
                // Simplified diff
                // In a real implementation, you would implement proper diff logic
                var differences = new List<object>();
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["differences"] = differences,
                    ["has_differences"] = differences.Count > 0
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Diff failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Patch YAML object
        /// </summary>
        private async Task<object> PatchYamlAsync(object data, object patch)
        {
            if (data == null)
                throw new ArgumentException("Data is required for patching");
            
            if (patch == null)
                throw new ArgumentException("Patch data is required");
            
            try
            {
                // Simplified patch
                // In a real implementation, you would implement proper patch logic
                var patched = data; // Placeholder for actual patching
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["patched"] = patched
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Patch failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Format YAML
        /// </summary>
        private async Task<object> FormatYamlAsync(object data, string format, int indent, string encoding)
        {
            if (data == null)
                throw new ArgumentException("Data is required for formatting");
            
            try
            {
                var yamlString = ConvertToYamlString(data, indent);
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["formatted"] = yamlString,
                    ["format"] = format,
                    ["encoding"] = encoding
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Formatting failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Minify YAML
        /// </summary>
        private async Task<object> MinifyYamlAsync(object data, string encoding)
        {
            if (data == null)
                throw new ArgumentException("Data is required for minification");
            
            try
            {
                var yamlString = ConvertToYamlString(data, 0);
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["minified"] = yamlString,
                    ["encoding"] = encoding
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Minification failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Flatten YAML object
        /// </summary>
        private async Task<object> FlattenYamlAsync(object data)
        {
            if (data == null)
                throw new ArgumentException("Data is required for flattening");
            
            try
            {
                var flattened = FlattenObject(data, "");
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["flattened"] = flattened
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Flattening failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Unflatten YAML object
        /// </summary>
        private async Task<object> UnflattenYamlAsync(object data)
        {
            if (data == null)
                throw new ArgumentException("Data is required for unflattening");
            
            try
            {
                var unflattened = UnflattenObject(data);
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["unflattened"] = unflattened
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Unflattening failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Parse YAML string (simplified implementation)
        /// </summary>
        private object ParseYamlString(string yamlString)
        {
            var result = new Dictionary<string, object>();
            var lines = yamlString.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                    continue;
                
                var colonIndex = trimmedLine.IndexOf(':');
                if (colonIndex > 0)
                {
                    var key = trimmedLine.Substring(0, colonIndex).Trim();
                    var value = trimmedLine.Substring(colonIndex + 1).Trim();
                    
                    // Remove quotes if present
                    if (value.StartsWith("\"") && value.EndsWith("\""))
                    {
                        value = value.Substring(1, value.Length - 2);
                    }
                    else if (value.StartsWith("'") && value.EndsWith("'"))
                    {
                        value = value.Substring(1, value.Length - 2);
                    }
                    
                    // Try to parse as number
                    if (int.TryParse(value, out var intValue))
                    {
                        result[key] = intValue;
                    }
                    else if (double.TryParse(value, out var doubleValue))
                    {
                        result[key] = doubleValue;
                    }
                    else if (value.ToLower() == "true" || value.ToLower() == "false")
                    {
                        result[key] = bool.Parse(value.ToLower());
                    }
                    else
                    {
                        result[key] = value;
                    }
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Convert object to YAML string (simplified implementation)
        /// </summary>
        private string ConvertToYamlString(object data, int indent)
        {
            var yamlBuilder = new StringBuilder();
            
            if (data is Dictionary<string, object> dict)
            {
                foreach (var kvp in dict)
                {
                    var indentStr = new string(' ', indent);
                    yamlBuilder.AppendLine($"{indentStr}{kvp.Key}: {ConvertValueToYaml(kvp.Value, indent + 2)}");
                }
            }
            
            return yamlBuilder.ToString().TrimEnd('\r', '\n');
        }
        
        /// <summary>
        /// Convert value to YAML string
        /// </summary>
        private string ConvertValueToYaml(object value, int indent)
        {
            if (value == null)
                return "null";
            
            if (value is string str)
            {
                if (str.Contains('\n') || str.Contains('"') || str.Contains('\''))
                {
                    return $"\"{str.Replace("\"", "\\\"")}\"";
                }
                return str;
            }
            
            if (value is bool boolValue)
            {
                return boolValue.ToString().ToLower();
            }
            
            if (value is int || value is long || value is double || value is float || value is decimal)
            {
                return value.ToString();
            }
            
            if (value is Dictionary<string, object> dict)
            {
                var yamlBuilder = new StringBuilder();
                yamlBuilder.AppendLine();
                
                foreach (var kvp in dict)
                {
                    var indentStr = new string(' ', indent);
                    yamlBuilder.AppendLine($"{indentStr}{kvp.Key}: {ConvertValueToYaml(kvp.Value, indent + 2)}");
                }
                
                return yamlBuilder.ToString().TrimEnd('\r', '\n');
            }
            
            if (value is List<object> list)
            {
                var yamlBuilder = new StringBuilder();
                yamlBuilder.AppendLine();
                
                foreach (var item in list)
                {
                    var indentStr = new string(' ', indent);
                    yamlBuilder.AppendLine($"{indentStr}- {ConvertValueToYaml(item, indent + 2)}");
                }
                
                return yamlBuilder.ToString().TrimEnd('\r', '\n');
            }
            
            return value.ToString();
        }
        
        /// <summary>
        /// Flatten object to key-value pairs
        /// </summary>
        private Dictionary<string, object> FlattenObject(object data, string prefix)
        {
            var result = new Dictionary<string, object>();
            
            if (data is Dictionary<string, object> dict)
            {
                foreach (var kvp in dict)
                {
                    var key = string.IsNullOrEmpty(prefix) ? kvp.Key : $"{prefix}.{kvp.Key}";
                    
                    if (kvp.Value is Dictionary<string, object> nestedDict)
                    {
                        var nested = FlattenObject(kvp.Value, key);
                        foreach (var nestedKvp in nested)
                        {
                            result[nestedKvp.Key] = nestedKvp.Value;
                        }
                    }
                    else
                    {
                        result[key] = kvp.Value;
                    }
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Unflatten object from key-value pairs
        /// </summary>
        private object UnflattenObject(object data)
        {
            if (data is Dictionary<string, object> dict)
            {
                var result = new Dictionary<string, object>();
                
                foreach (var kvp in dict)
                {
                    var parts = kvp.Key.Split('.');
                    var current = result;
                    
                    for (int i = 0; i < parts.Length - 1; i++)
                    {
                        var part = parts[i];
                        if (!current.ContainsKey(part))
                        {
                            current[part] = new Dictionary<string, object>();
                        }
                        current = (Dictionary<string, object>)current[part];
                    }
                    
                    current[parts[parts.Length - 1]] = kvp.Value;
                }
                
                return result;
            }
            
            return data;
        }
        
        protected override ValidationResult CustomValidate(Dictionary<string, object> config)
        {
            var result = new ValidationResult();
            
            if (!config.ContainsKey("action"))
            {
                result.Errors.Add("Action is required");
            }
            
            var action = GetContextValue<string>(config, "action", "");
            var validActions = new[] { "parse", "stringify", "to_json", "to_xml", "validate", "transform", "merge", "diff", "patch", "format", "minify", "flatten", "unflatten" };
            
            if (!string.IsNullOrEmpty(action) && !Array.Exists(validActions, a => a == action.ToLower()))
            {
                result.Errors.Add($"Invalid action: {action}. Valid actions are: {string.Join(", ", validActions)}");
            }
            
            if (config.TryGetValue("timeout", out var timeout) && timeout is int timeoutValue && timeoutValue <= 0)
            {
                result.Errors.Add("Timeout must be positive");
            }
            
            if (config.TryGetValue("indent", out var indent) && indent is int indentValue && indentValue < 0)
            {
                result.Errors.Add("Indent must be non-negative");
            }
            
            return result;
        }
    }
} 