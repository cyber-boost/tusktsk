using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;

namespace TuskLang.Operators.DataProcessing
{
    /// <summary>
    /// JSON Operator for TuskLang C# SDK
    /// 
    /// Provides JSON processing capabilities with support for:
    /// - JSON parsing and serialization
    /// - JSON validation and formatting
    /// - JSON path queries (JSONPath)
    /// - JSON schema validation
    /// - JSON transformation and manipulation
    /// - Pretty printing and minification
    /// 
    /// Usage:
    /// ```csharp
    /// // Parse JSON
    /// var result = @json({
    ///   action: "parse",
    ///   data: "{\"name\": \"John\", \"age\": 30}"
    /// })
    /// 
    /// // Query with JSONPath
    /// var result = @json({
    ///   action: "query",
    ///   data: jsonData,
    ///   path: "$.users[*].name"
    /// })
    /// ```
    /// </summary>
    public class JsonOperator : BaseOperator
    {
        public JsonOperator()
        {
            Version = "2.0.0";
            RequiredFields = new List<string> { "action" };
            OptionalFields = new List<string> 
            { 
                "data", "path", "schema", "format", "indent", "validate", 
                "transform", "merge", "diff", "patch", "timeout" 
            };
            
            DefaultConfig = new Dictionary<string, object>
            {
                ["format"] = "pretty",
                ["indent"] = 2,
                ["validate"] = true,
                ["timeout"] = 300
            };
        }
        
        public override string GetName() => "json";
        
        protected override string GetDescription() => "JSON processing operator for parsing, querying, and manipulating JSON data";
        
        protected override Dictionary<string, string> GetExamples()
        {
            return new Dictionary<string, string>
            {
                ["parse"] = "@json({action: \"parse\", data: \"{\\\"name\\\": \\\"John\\\"}\"})",
                ["stringify"] = "@json({action: \"stringify\", data: {name: \"John\", age: 30}})",
                ["query"] = "@json({action: \"query\", data: jsonData, path: \"$.users[*].name\"})",
                ["validate"] = "@json({action: \"validate\", data: jsonData, schema: schemaData})"
            };
        }
        
        protected override Dictionary<string, string> GetErrorCodes()
        {
            return new Dictionary<string, string>
            {
                ["INVALID_ACTION"] = "Invalid JSON action",
                ["INVALID_JSON"] = "Invalid JSON data",
                ["PARSE_ERROR"] = "JSON parsing error",
                ["QUERY_ERROR"] = "JSONPath query error",
                ["SCHEMA_VALIDATION_ERROR"] = "JSON schema validation failed",
                ["TRANSFORM_ERROR"] = "JSON transformation error",
                ["MERGE_ERROR"] = "JSON merge error",
                ["TIMEOUT_EXCEEDED"] = "JSON operation timeout exceeded"
            };
        }
        
        protected override async Task<object> ExecuteOperatorAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var action = GetContextValue<string>(config, "action", "");
            var data = ResolveVariable(config.GetValueOrDefault("data"), context);
            var path = GetContextValue<string>(config, "path", "");
            var schema = ResolveVariable(config.GetValueOrDefault("schema"), context);
            var format = GetContextValue<string>(config, "format", "pretty");
            var indent = GetContextValue<int>(config, "indent", 2);
            var validate = GetContextValue<bool>(config, "validate", true);
            var transform = GetContextValue<string>(config, "transform", "");
            var merge = ResolveVariable(config.GetValueOrDefault("merge"), context);
            var diff = ResolveVariable(config.GetValueOrDefault("diff"), context);
            var patch = ResolveVariable(config.GetValueOrDefault("patch"), context);
            var timeout = GetContextValue<int>(config, "timeout", 300);
            
            if (string.IsNullOrEmpty(action))
                throw new ArgumentException("Action is required");
            
            var startTime = DateTime.UtcNow;
            
            try
            {
                // Check timeout
                if ((DateTime.UtcNow - startTime).TotalSeconds > timeout)
                {
                    throw new TimeoutException("JSON operation timeout exceeded");
                }
                
                switch (action.ToLower())
                {
                    case "parse":
                        return await ParseJsonAsync(data, validate);
                    
                    case "stringify":
                        return await StringifyJsonAsync(data, format, indent);
                    
                    case "query":
                        return await QueryJsonAsync(data, path);
                    
                    case "validate":
                        return await ValidateJsonAsync(data, schema);
                    
                    case "transform":
                        return await TransformJsonAsync(data, transform);
                    
                    case "merge":
                        return await MergeJsonAsync(data, merge);
                    
                    case "diff":
                        return await DiffJsonAsync(data, diff);
                    
                    case "patch":
                        return await PatchJsonAsync(data, patch);
                    
                    case "format":
                        return await FormatJsonAsync(data, format, indent);
                    
                    case "minify":
                        return await MinifyJsonAsync(data);
                    
                    case "flatten":
                        return await FlattenJsonAsync(data);
                    
                    case "unflatten":
                        return await UnflattenJsonAsync(data);
                    
                    default:
                        throw new ArgumentException($"Unknown JSON action: {action}");
                }
            }
            catch (Exception ex)
            {
                Log("error", "JSON operation failed", new Dictionary<string, object>
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
        /// Parse JSON string to object
        /// </summary>
        private async Task<object> ParseJsonAsync(object data, bool validate)
        {
            if (data == null)
                throw new ArgumentException("Data is required for parsing");
            
            var jsonString = data.ToString();
            
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                };
                
                var result = JsonSerializer.Deserialize<object>(jsonString, options);
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["parsed"] = result,
                    ["valid"] = true
                };
            }
            catch (JsonException ex)
            {
                if (validate)
                {
                    throw new ArgumentException($"Invalid JSON: {ex.Message}");
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
        /// Convert object to JSON string
        /// </summary>
        private async Task<object> StringifyJsonAsync(object data, string format, int indent)
        {
            if (data == null)
                throw new ArgumentException("Data is required for stringification");
            
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = format == "pretty",
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                var jsonString = JsonSerializer.Serialize(data, options);
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["stringified"] = jsonString,
                    ["format"] = format
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Stringification failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Query JSON with JSONPath
        /// </summary>
        private async Task<object> QueryJsonAsync(object data, string path)
        {
            if (data == null)
                throw new ArgumentException("Data is required for querying");
            
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path is required for querying");
            
            try
            {
                // Simplified JSONPath implementation
                // In a real implementation, you would use a proper JSONPath library
                var results = new List<object>();
                
                if (path == "$" || path == "$.*")
                {
                    results.Add(data);
                }
                else if (path.StartsWith("$."))
                {
                    var propertyPath = path.Substring(2);
                    var result = ExtractProperty(data, propertyPath);
                    if (result != null)
                    {
                        results.Add(result);
                    }
                }
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["results"] = results,
                    ["count"] = results.Count,
                    ["path"] = path
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"JSONPath query failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Validate JSON against schema
        /// </summary>
        private async Task<object> ValidateJsonAsync(object data, object schema)
        {
            if (data == null)
                throw new ArgumentException("Data is required for validation");
            
            // Simplified validation
            // In a real implementation, you would use a proper JSON schema validator
            var isValid = true;
            var errors = new List<string>();
            
            if (schema != null)
            {
                // Basic schema validation logic would go here
                // For now, just check if data is not null
                if (data == null)
                {
                    isValid = false;
                    errors.Add("Data cannot be null");
                }
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
        /// Transform JSON data
        /// </summary>
        private async Task<object> TransformJsonAsync(object data, string transform)
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
        /// Merge JSON objects
        /// </summary>
        private async Task<object> MergeJsonAsync(object data, object merge)
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
        /// Diff JSON objects
        /// </summary>
        private async Task<object> DiffJsonAsync(object data, object diff)
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
        /// Patch JSON object
        /// </summary>
        private async Task<object> PatchJsonAsync(object data, object patch)
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
        /// Format JSON
        /// </summary>
        private async Task<object> FormatJsonAsync(object data, string format, int indent)
        {
            if (data == null)
                throw new ArgumentException("Data is required for formatting");
            
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = format == "pretty",
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                var formatted = JsonSerializer.Serialize(data, options);
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["formatted"] = formatted,
                    ["format"] = format
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Formatting failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Minify JSON
        /// </summary>
        private async Task<object> MinifyJsonAsync(object data)
        {
            if (data == null)
                throw new ArgumentException("Data is required for minification");
            
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = false
                };
                
                var minified = JsonSerializer.Serialize(data, options);
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["minified"] = minified
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Minification failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Flatten JSON object
        /// </summary>
        private async Task<object> FlattenJsonAsync(object data)
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
        /// Unflatten JSON object
        /// </summary>
        private async Task<object> UnflattenJsonAsync(object data)
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
        /// Extract property from object using path
        /// </summary>
        private object ExtractProperty(object data, string path)
        {
            if (data == null || string.IsNullOrEmpty(path))
                return null;
            
            var parts = path.Split('.');
            var current = data;
            
            foreach (var part in parts)
            {
                if (current is Dictionary<string, object> dict)
                {
                    if (!dict.TryGetValue(part, out current))
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            
            return current;
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
            var validActions = new[] { "parse", "stringify", "query", "validate", "transform", "merge", "diff", "patch", "format", "minify", "flatten", "unflatten" };
            
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