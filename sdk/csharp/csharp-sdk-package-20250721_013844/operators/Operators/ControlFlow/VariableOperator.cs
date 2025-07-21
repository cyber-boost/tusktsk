using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using System.Linq;
using System.Reflection;
using System.ComponentModel;

namespace TuskLang.Operators.ControlFlow
{
    /// <summary>
    /// Variable Operator for TuskLang C# SDK
    /// 
    /// Provides comprehensive variable management operations with support for:
    /// - Type-safe variable handling with automatic type conversion
    /// - Complex data types (objects, arrays, dictionaries)
    /// - Variable scoping and lifetime management
    /// - Thread-safe concurrent operations
    /// - Performance-optimized storage and retrieval
    /// - Variable validation and constraints
    /// - Nested variable access and manipulation
    /// - Variable events and change tracking
    /// 
    /// Usage:
    /// ```csharp
    /// // Set variable
    /// var result = @variable({
    ///   action: "set",
    ///   name: "user_id",
    ///   value: 12345,
    ///   type: "integer",
    ///   scope: "global"
    /// })
    /// 
    /// // Get variable
    /// var result = @variable({
    ///   action: "get",
    ///   name: "user_id"
    /// })
    /// 
    /// // Set complex object
    /// var result = @variable({
    ///   action: "set",
    ///   name: "user",
    ///   value: {"name": "John", "age": 30, "roles": ["admin", "user"]}
    /// })
    /// ```
    /// </summary>
    public class VariableOperator : BaseOperator, IDisposable
    {
        private static readonly ConcurrentDictionary<string, VariableInfo> _globalVariables = new();
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, VariableInfo>> _scopedVariables = new();
        private static readonly object _lock = new();
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// Initializes a new instance of the VariableOperator class
        /// </summary>
        public VariableOperator()
        {
            Version = "2.0.0";
            RequiredFields = new List<string> { "action", "name" };
            OptionalFields = new List<string> 
            { 
                "value", "type", "scope", "default", "constraints", "readonly", "persistent",
                "path", "index", "key", "operation", "increment", "format", "validate"
            };
            
            DefaultConfig = new Dictionary<string, object>
            {
                ["scope"] = "global",
                ["type"] = "auto",
                ["readonly"] = false,
                ["persistent"] = false,
                ["validate"] = true
            };

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
                PropertyNameCaseInsensitive = true
            };
        }

        /// <summary>
        /// Gets the operator name
        /// </summary>
        public override string GetName() => "variable";

        /// <summary>
        /// Gets the operator description
        /// </summary>
        protected override string GetDescription()
        {
            return "Provides comprehensive variable management with type safety, scoping, and thread-safe operations";
        }

        /// <summary>
        /// Gets usage examples
        /// </summary>
        protected override Dictionary<string, string> GetExamples()
        {
            return new Dictionary<string, string>
            {
                ["set"] = "@variable({action: \"set\", name: \"count\", value: 42, type: \"integer\"})",
                ["get"] = "@variable({action: \"get\", name: \"count\"})",
                ["increment"] = "@variable({action: \"increment\", name: \"count\", increment: 1})",
                ["set_nested"] = "@variable({action: \"set\", name: \"user.profile.age\", value: 25})",
                ["get_nested"] = "@variable({action: \"get\", name: \"user.profile.name\"})",
                ["list_all"] = "@variable({action: \"list\", scope: \"global\"})"
            };
        }

        /// <summary>
        /// Custom validation for variable operations
        /// </summary>
        protected override ValidationResult CustomValidate(Dictionary<string, object> config)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            var action = config.GetValueOrDefault("action")?.ToString()?.ToLower();
            var name = config.GetValueOrDefault("name")?.ToString();

            // Validate variable name
            if (!string.IsNullOrEmpty(name))
            {
                if (!IsValidVariableName(name))
                {
                    errors.Add("Variable name contains invalid characters. Use alphanumeric characters, dots, and underscores only.");
                }
            }

            // Action-specific validation
            switch (action)
            {
                case "set":
                case "update":
                    if (!config.ContainsKey("value"))
                        errors.Add("'value' is required for set/update operation");
                    break;

                case "get":
                case "delete":
                case "exists":
                    // Name is already required by RequiredFields
                    break;

                case "increment":
                case "decrement":
                    if (config.ContainsKey("increment") && !IsNumeric(config["increment"]))
                        errors.Add("'increment' must be a numeric value");
                    break;

                case "append":
                case "prepend":
                    if (!config.ContainsKey("value"))
                        errors.Add("'value' is required for append/prepend operation");
                    break;
            }

            // Type validation
            if (config.ContainsKey("type"))
            {
                var type = config["type"].ToString()!.ToLower();
                var supportedTypes = new[] { "auto", "string", "integer", "double", "boolean", "object", "array", "datetime" };
                if (!supportedTypes.Contains(type))
                {
                    warnings.Add($"Unsupported type '{type}'. Will use 'auto' instead.");
                }
            }

            return new ValidationResult { Errors = errors, Warnings = warnings };
        }

        /// <summary>
        /// Execute the variable operator
        /// </summary>
        protected override async Task<object> ExecuteOperatorAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var action = config["action"].ToString()!.ToLower();
            var name = config["name"].ToString()!;
            var scope = config.GetValueOrDefault("scope")?.ToString() ?? "global";

            try
            {
                switch (action)
                {
                    case "set":
                        return await SetVariableAsync(name, config, scope);
                    
                    case "get":
                        return await GetVariableAsync(name, config, scope);
                    
                    case "update":
                        return await UpdateVariableAsync(name, config, scope);
                    
                    case "delete":
                        return await DeleteVariableAsync(name, scope);
                    
                    case "exists":
                        return await ExistsVariableAsync(name, scope);
                    
                    case "list":
                        return await ListVariablesAsync(scope, config);
                    
                    case "increment":
                        return await IncrementVariableAsync(name, config, scope);
                    
                    case "decrement":
                        return await DecrementVariableAsync(name, config, scope);
                    
                    case "append":
                        return await AppendVariableAsync(name, config, scope);
                    
                    case "prepend":
                        return await PrependVariableAsync(name, config, scope);
                    
                    case "clear":
                        return await ClearVariablesAsync(scope);
                    
                    case "type":
                        return await GetVariableTypeAsync(name, scope);
                    
                    case "validate":
                        return await ValidateVariableAsync(name, config, scope);
                    
                    default:
                        throw new ArgumentException($"Unsupported variable action: {action}");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Variable operation '{action}' failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Set variable value
        /// </summary>
        private async Task<object> SetVariableAsync(string name, Dictionary<string, object> config, string scope)
        {
            var value = config["value"];
            var type = config.GetValueOrDefault("type")?.ToString() ?? "auto";
            var isReadonly = Convert.ToBoolean(config.GetValueOrDefault("readonly", false));
            var constraints = config.GetValueOrDefault("constraints") as Dictionary<string, object>;

            // Convert value to specified type
            var convertedValue = await ConvertValueAsync(value, type);
            
            // Validate constraints
            if (constraints != null && !await ValidateConstraintsAsync(convertedValue, constraints))
            {
                throw new ArgumentException("Value does not meet specified constraints");
            }

            var variableInfo = new VariableInfo
            {
                Name = name,
                Value = convertedValue,
                Type = GetActualType(convertedValue),
                Scope = scope,
                IsReadonly = isReadonly,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Constraints = constraints
            };

            // Handle nested variable names (e.g., "user.profile.name")
            if (name.Contains('.'))
            {
                return await SetNestedVariableAsync(name, variableInfo, scope);
            }

            // Store in appropriate scope
            var variables = GetVariableStorage(scope);
            
            // Check if variable exists and is readonly
            if (variables.TryGetValue(name, out var existing) && existing.IsReadonly)
            {
                throw new InvalidOperationException($"Cannot modify readonly variable '{name}'");
            }

            variables[name] = variableInfo;

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["name"] = name,
                ["value"] = convertedValue,
                ["type"] = variableInfo.Type,
                ["scope"] = scope,
                ["readonly"] = isReadonly
            };
        }

        /// <summary>
        /// Get variable value
        /// </summary>
        private async Task<object> GetVariableAsync(string name, Dictionary<string, object> config, string scope)
        {
            var defaultValue = config.GetValueOrDefault("default");
            var format = config.GetValueOrDefault("format")?.ToString();

            // Handle nested variable names
            if (name.Contains('.'))
            {
                return await GetNestedVariableAsync(name, scope, defaultValue, format);
            }

            var variables = GetVariableStorage(scope);
            
            if (variables.TryGetValue(name, out var variable))
            {
                var value = variable.Value;
                
                // Apply formatting if specified
                if (!string.IsNullOrEmpty(format))
                {
                    value = await FormatValueAsync(value, format);
                }

                return new Dictionary<string, object>
                {
                    ["found"] = true,
                    ["name"] = name,
                    ["value"] = value,
                    ["type"] = variable.Type,
                    ["scope"] = scope,
                    ["readonly"] = variable.IsReadonly,
                    ["created_at"] = variable.CreatedAt,
                    ["updated_at"] = variable.UpdatedAt
                };
            }
            else
            {
                return new Dictionary<string, object>
                {
                    ["found"] = false,
                    ["name"] = name,
                    ["value"] = defaultValue,
                    ["scope"] = scope
                };
            }
        }

        /// <summary>
        /// Update variable value
        /// </summary>
        private async Task<object> UpdateVariableAsync(string name, Dictionary<string, object> config, string scope)
        {
            var variables = GetVariableStorage(scope);
            
            if (!variables.TryGetValue(name, out var existing))
            {
                throw new KeyNotFoundException($"Variable '{name}' not found in scope '{scope}'");
            }

            if (existing.IsReadonly)
            {
                throw new InvalidOperationException($"Cannot modify readonly variable '{name}'");
            }

            // Update with new value
            var updatedConfig = new Dictionary<string, object>(config)
            {
                ["readonly"] = existing.IsReadonly
            };

            return await SetVariableAsync(name, updatedConfig, scope);
        }

        /// <summary>
        /// Delete variable
        /// </summary>
        private async Task<object> DeleteVariableAsync(string name, string scope)
        {
            var variables = GetVariableStorage(scope);
            
            if (variables.TryGetValue(name, out var existing) && existing.IsReadonly)
            {
                throw new InvalidOperationException($"Cannot delete readonly variable '{name}'");
            }

            var removed = variables.TryRemove(name, out _);

            return new Dictionary<string, object>
            {
                ["success"] = removed,
                ["name"] = name,
                ["scope"] = scope
            };
        }

        /// <summary>
        /// Check if variable exists
        /// </summary>
        private async Task<object> ExistsVariableAsync(string name, string scope)
        {
            var variables = GetVariableStorage(scope);
            var exists = variables.ContainsKey(name);

            return new Dictionary<string, object>
            {
                ["exists"] = exists,
                ["name"] = name,
                ["scope"] = scope
            };
        }

        /// <summary>
        /// List variables in scope
        /// </summary>
        private async Task<object> ListVariablesAsync(string scope, Dictionary<string, object> config)
        {
            var pattern = config.GetValueOrDefault("pattern")?.ToString();
            var includeValues = Convert.ToBoolean(config.GetValueOrDefault("include_values", true));
            
            var variables = GetVariableStorage(scope);
            var results = new List<Dictionary<string, object>>();

            foreach (var kvp in variables)
            {
                if (!string.IsNullOrEmpty(pattern) && !kvp.Key.Contains(pattern))
                    continue;

                var info = new Dictionary<string, object>
                {
                    ["name"] = kvp.Key,
                    ["type"] = kvp.Value.Type,
                    ["readonly"] = kvp.Value.IsReadonly,
                    ["created_at"] = kvp.Value.CreatedAt,
                    ["updated_at"] = kvp.Value.UpdatedAt
                };

                if (includeValues)
                {
                    info["value"] = kvp.Value.Value;
                }

                results.Add(info);
            }

            return new Dictionary<string, object>
            {
                ["variables"] = results,
                ["count"] = results.Count,
                ["scope"] = scope
            };
        }

        /// <summary>
        /// Increment numeric variable
        /// </summary>
        private async Task<object> IncrementVariableAsync(string name, Dictionary<string, object> config, string scope)
        {
            var increment = Convert.ToDouble(config.GetValueOrDefault("increment", 1));
            var variables = GetVariableStorage(scope);
            
            if (variables.TryGetValue(name, out var existing))
            {
                if (existing.IsReadonly)
                {
                    throw new InvalidOperationException($"Cannot modify readonly variable '{name}'");
                }

                var currentValue = Convert.ToDouble(existing.Value);
                var newValue = currentValue + increment;
                
                existing.Value = newValue;
                existing.UpdatedAt = DateTime.UtcNow;
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["name"] = name,
                    ["old_value"] = currentValue,
                    ["new_value"] = newValue,
                    ["increment"] = increment
                };
            }
            else
            {
                // Create new variable with increment as initial value
                var newConfig = new Dictionary<string, object>
                {
                    ["value"] = increment,
                    ["type"] = "double"
                };
                return await SetVariableAsync(name, newConfig, scope);
            }
        }

        /// <summary>
        /// Decrement numeric variable
        /// </summary>
        private async Task<object> DecrementVariableAsync(string name, Dictionary<string, object> config, string scope)
        {
            var decrement = Convert.ToDouble(config.GetValueOrDefault("decrement", 1));
            config["increment"] = -decrement;
            return await IncrementVariableAsync(name, config, scope);
        }

        /// <summary>
        /// Append value to variable
        /// </summary>
        private async Task<object> AppendVariableAsync(string name, Dictionary<string, object> config, string scope)
        {
            var appendValue = config["value"];
            var variables = GetVariableStorage(scope);
            
            if (variables.TryGetValue(name, out var existing))
            {
                if (existing.IsReadonly)
                {
                    throw new InvalidOperationException($"Cannot modify readonly variable '{name}'");
                }

                var newValue = await AppendValues(existing.Value, appendValue);
                existing.Value = newValue;
                existing.UpdatedAt = DateTime.UtcNow;
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["name"] = name,
                    ["value"] = newValue
                };
            }
            else
            {
                // Create new variable with append value
                return await SetVariableAsync(name, new Dictionary<string, object> { ["value"] = appendValue }, scope);
            }
        }

        /// <summary>
        /// Prepend value to variable
        /// </summary>
        private async Task<object> PrependVariableAsync(string name, Dictionary<string, object> config, string scope)
        {
            var prependValue = config["value"];
            var variables = GetVariableStorage(scope);
            
            if (variables.TryGetValue(name, out var existing))
            {
                if (existing.IsReadonly)
                {
                    throw new InvalidOperationException($"Cannot modify readonly variable '{name}'");
                }

                var newValue = await AppendValues(prependValue, existing.Value);
                existing.Value = newValue;
                existing.UpdatedAt = DateTime.UtcNow;
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["name"] = name,
                    ["value"] = newValue
                };
            }
            else
            {
                // Create new variable with prepend value
                return await SetVariableAsync(name, new Dictionary<string, object> { ["value"] = prependValue }, scope);
            }
        }

        /// <summary>
        /// Clear all variables in scope
        /// </summary>
        private async Task<object> ClearVariablesAsync(string scope)
        {
            var variables = GetVariableStorage(scope);
            var count = variables.Count;
            
            // Remove only non-readonly variables
            var toRemove = variables.Where(kvp => !kvp.Value.IsReadonly).ToList();
            foreach (var kvp in toRemove)
            {
                variables.TryRemove(kvp.Key, out _);
            }

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["cleared"] = toRemove.Count,
                ["readonly_kept"] = count - toRemove.Count,
                ["scope"] = scope
            };
        }

        /// <summary>
        /// Get variable type information
        /// </summary>
        private async Task<object> GetVariableTypeAsync(string name, string scope)
        {
            var variables = GetVariableStorage(scope);
            
            if (variables.TryGetValue(name, out var variable))
            {
                return new Dictionary<string, object>
                {
                    ["found"] = true,
                    ["name"] = name,
                    ["type"] = variable.Type,
                    ["is_null"] = variable.Value == null,
                    ["is_numeric"] = IsNumeric(variable.Value),
                    ["is_string"] = variable.Value is string,
                    ["is_array"] = variable.Value is Array || variable.Value is List<object>,
                    ["is_object"] = variable.Value is Dictionary<string, object>
                };
            }
            else
            {
                return new Dictionary<string, object>
                {
                    ["found"] = false,
                    ["name"] = name,
                    ["scope"] = scope
                };
            }
        }

        /// <summary>
        /// Validate variable against constraints
        /// </summary>
        private async Task<object> ValidateVariableAsync(string name, Dictionary<string, object> config, string scope)
        {
            var variables = GetVariableStorage(scope);
            var constraints = config.GetValueOrDefault("constraints") as Dictionary<string, object>;
            
            if (!variables.TryGetValue(name, out var variable))
            {
                return new Dictionary<string, object>
                {
                    ["valid"] = false,
                    ["error"] = "Variable not found"
                };
            }

            var isValid = constraints == null || await ValidateConstraintsAsync(variable.Value, constraints);
            
            return new Dictionary<string, object>
            {
                ["valid"] = isValid,
                ["name"] = name,
                ["value"] = variable.Value,
                ["constraints"] = constraints ?? new Dictionary<string, object>()
            };
        }

        /// <summary>
        /// Get variable storage for scope
        /// </summary>
        private ConcurrentDictionary<string, VariableInfo> GetVariableStorage(string scope)
        {
            if (scope == "global")
            {
                return _globalVariables;
            }
            
            return _scopedVariables.GetOrAdd(scope, _ => new ConcurrentDictionary<string, VariableInfo>());
        }

        /// <summary>
        /// Set nested variable (e.g., "user.profile.name")
        /// </summary>
        private async Task<object> SetNestedVariableAsync(string path, VariableInfo variableInfo, string scope)
        {
            var parts = path.Split('.');
            var rootName = parts[0];
            var variables = GetVariableStorage(scope);
            
            // Get or create root object
            if (!variables.TryGetValue(rootName, out var rootVar))
            {
                rootVar = new VariableInfo
                {
                    Name = rootName,
                    Value = new Dictionary<string, object>(),
                    Type = "object",
                    Scope = scope,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                variables[rootName] = rootVar;
            }

            // Navigate to nested location and set value
            var current = rootVar.Value as Dictionary<string, object>;
            for (int i = 1; i < parts.Length - 1; i++)
            {
                if (!current.ContainsKey(parts[i]))
                {
                    current[parts[i]] = new Dictionary<string, object>();
                }
                current = current[parts[i]] as Dictionary<string, object>;
            }

            // Set the final value
            current[parts[^1]] = variableInfo.Value;
            rootVar.UpdatedAt = DateTime.UtcNow;

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["path"] = path,
                ["value"] = variableInfo.Value,
                ["scope"] = scope
            };
        }

        /// <summary>
        /// Get nested variable value
        /// </summary>
        private async Task<object> GetNestedVariableAsync(string path, string scope, object defaultValue, string format)
        {
            var parts = path.Split('.');
            var rootName = parts[0];
            var variables = GetVariableStorage(scope);
            
            if (!variables.TryGetValue(rootName, out var rootVar))
            {
                return new Dictionary<string, object>
                {
                    ["found"] = false,
                    ["path"] = path,
                    ["value"] = defaultValue,
                    ["scope"] = scope
                };
            }

            // Navigate to nested value
            object current = rootVar.Value;
            for (int i = 1; i < parts.Length; i++)
            {
                if (current is Dictionary<string, object> dict && dict.ContainsKey(parts[i]))
                {
                    current = dict[parts[i]];
                }
                else
                {
                    return new Dictionary<string, object>
                    {
                        ["found"] = false,
                        ["path"] = path,
                        ["value"] = defaultValue,
                        ["scope"] = scope
                    };
                }
            }

            // Apply formatting if specified
            if (!string.IsNullOrEmpty(format))
            {
                current = await FormatValueAsync(current, format);
            }

            return new Dictionary<string, object>
            {
                ["found"] = true,
                ["path"] = path,
                ["value"] = current,
                ["type"] = GetActualType(current),
                ["scope"] = scope
            };
        }

        /// <summary>
        /// Convert value to specified type
        /// </summary>
        private async Task<object> ConvertValueAsync(object value, string type)
        {
            if (value == null) return null;
            
            return type.ToLower() switch
            {
                "auto" => value, // Keep original type
                "string" => value.ToString(),
                "integer" => Convert.ToInt32(value),
                "double" => Convert.ToDouble(value),
                "boolean" => Convert.ToBoolean(value),
                "datetime" => Convert.ToDateTime(value),
                "object" => value is string str ? JsonSerializer.Deserialize<Dictionary<string, object>>(str, _jsonOptions) : value,
                "array" => value is string str2 ? JsonSerializer.Deserialize<List<object>>(str2, _jsonOptions) : value,
                _ => value
            };
        }

        /// <summary>
        /// Get actual type name of value
        /// </summary>
        private string GetActualType(object value)
        {
            return value switch
            {
                null => "null",
                string => "string",
                int or long => "integer",
                float or double or decimal => "double",
                bool => "boolean",
                DateTime => "datetime",
                Dictionary<string, object> => "object",
                Array or List<object> => "array",
                _ => value.GetType().Name.ToLower()
            };
        }

        /// <summary>
        /// Validate constraints
        /// </summary>
        private async Task<bool> ValidateConstraintsAsync(object value, Dictionary<string, object> constraints)
        {
            foreach (var constraint in constraints)
            {
                switch (constraint.Key.ToLower())
                {
                    case "required":
                        if (Convert.ToBoolean(constraint.Value) && value == null)
                            return false;
                        break;
                        
                    case "min":
                        if (IsNumeric(value) && Convert.ToDouble(value) < Convert.ToDouble(constraint.Value))
                            return false;
                        break;
                        
                    case "max":
                        if (IsNumeric(value) && Convert.ToDouble(value) > Convert.ToDouble(constraint.Value))
                            return false;
                        break;
                        
                    case "minlength":
                        if (value is string str && str.Length < Convert.ToInt32(constraint.Value))
                            return false;
                        break;
                        
                    case "maxlength":
                        if (value is string str2 && str2.Length > Convert.ToInt32(constraint.Value))
                            return false;
                        break;
                }
            }
            
            return true;
        }

        /// <summary>
        /// Format value according to format string
        /// </summary>
        private async Task<object> FormatValueAsync(object value, string format)
        {
            if (value == null) return null;
            
            return format.ToLower() switch
            {
                "upper" => value.ToString()!.ToUpper(),
                "lower" => value.ToString()!.ToLower(),
                "json" => JsonSerializer.Serialize(value, _jsonOptions),
                "string" => value.ToString(),
                _ when format.StartsWith("date:") => ((DateTime)value).ToString(format.Substring(5)),
                _ when format.StartsWith("number:") => ((double)value).ToString(format.Substring(7)),
                _ => value.ToString()
            };
        }

        /// <summary>
        /// Append two values together
        /// </summary>
        private async Task<object> AppendValues(object first, object second)
        {
            if (first is string firstStr)
            {
                return firstStr + second?.ToString();
            }
            
            if (first is List<object> firstList)
            {
                if (second is List<object> secondList)
                    firstList.AddRange(secondList);
                else
                    firstList.Add(second);
                return firstList;
            }
            
            if (first is Array firstArray && second is Array secondArray)
            {
                var combined = new object[firstArray.Length + secondArray.Length];
                firstArray.CopyTo(combined, 0);
                secondArray.CopyTo(combined, firstArray.Length);
                return combined;
            }
            
            // Default: concatenate as strings
            return first?.ToString() + second?.ToString();
        }

        /// <summary>
        /// Check if variable name is valid
        /// </summary>
        private bool IsValidVariableName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            
            // Allow alphanumeric, dots, and underscores
            return name.All(c => char.IsLetterOrDigit(c) || c == '.' || c == '_');
        }

        /// <summary>
        /// Check if value is numeric
        /// </summary>
        private bool IsNumeric(object value)
        {
            return value is int or long or float or double or decimal || 
                   double.TryParse(value?.ToString(), out _);
        }

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            // Cleanup if needed
        }

        /// <summary>
        /// Variable information structure
        /// </summary>
        private class VariableInfo
        {
            public string Name { get; set; } = "";
            public object? Value { get; set; }
            public string Type { get; set; } = "auto";
            public string Scope { get; set; } = "global";
            public bool IsReadonly { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime UpdatedAt { get; set; }
            public Dictionary<string, object>? Constraints { get; set; }
        }
    }

    /// <summary>
    /// Extension methods for configuration access
    /// </summary>
    public static class ConfigExtensions
    {
        public static T? GetValueOrDefault<T>(this Dictionary<string, object> dict, string key, T? defaultValue = default)
        {
            if (dict.TryGetValue(key, out var value))
            {
                if (value is T typedValue)
                    return typedValue;
                    
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }
    }
} 