using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace TuskLang.Operators
{
    /// <summary>
    /// Base Operator Class for TuskLang C# SDK
    /// 
    /// Provides common functionality for all @ operators including
    /// validation, error handling, and context management.
    /// 
    /// Follows the same pattern as the PHP SDK BaseOperator.
    /// </summary>
    public abstract class BaseOperator
    {
        protected Dictionary<string, object> DefaultConfig { get; set; } = new Dictionary<string, object>();
        protected List<string> RequiredFields { get; set; } = new List<string>();
        protected List<string> OptionalFields { get; set; } = new List<string>();
        protected string Version { get; set; } = "1.0.0";
        protected bool DebugMode { get; set; } = false;
        
        /// <summary>
        /// Get operator name
        /// </summary>
        public abstract string GetName();
        
        /// <summary>
        /// Get operator version
        /// </summary>
        public virtual string GetVersion() => Version;
        
        /// <summary>
        /// Get operator description
        /// </summary>
        protected virtual string GetDescription() => "Base operator description - override in subclasses";
        
        /// <summary>
        /// Get usage examples
        /// </summary>
        protected virtual Dictionary<string, string> GetExamples()
        {
            return new Dictionary<string, string>
            {
                ["basic"] = $"@{GetName()}(\"example\")",
                ["advanced"] = $"@{GetName()}({{param: \"value\"}})"
            };
        }
        
        /// <summary>
        /// Get error codes
        /// </summary>
        protected virtual Dictionary<string, string> GetErrorCodes()
        {
            return new Dictionary<string, string>
            {
                ["INVALID_CONFIG"] = "Invalid configuration provided",
                ["EXECUTION_FAILED"] = "Operator execution failed",
                ["TIMEOUT"] = "Operation timed out",
                ["CONNECTION_FAILED"] = "Connection to external service failed"
            };
        }
        
        /// <summary>
        /// Get operator schema
        /// </summary>
        public virtual Dictionary<string, object> GetSchema()
        {
            return new Dictionary<string, object>
            {
                ["name"] = GetName(),
                ["version"] = GetVersion(),
                ["description"] = GetDescription(),
                ["required_fields"] = RequiredFields,
                ["optional_fields"] = OptionalFields,
                ["default_config"] = DefaultConfig,
                ["examples"] = GetExamples(),
                ["error_codes"] = GetErrorCodes()
            };
        }
        
        /// <summary>
        /// Validate configuration
        /// </summary>
        public virtual ValidationResult Validate(Dictionary<string, object> config)
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            
            // Check required fields
            foreach (var field in RequiredFields)
            {
                if (!config.ContainsKey(field))
                {
                    errors.Add($"Required field '{field}' is missing");
                }
            }
            
            // Validate field types and values
            var validation = ValidateFields(config);
            errors.AddRange(validation.Errors);
            warnings.AddRange(validation.Warnings);
            
            // Custom validation
            var customValidation = CustomValidate(config);
            errors.AddRange(customValidation.Errors);
            warnings.AddRange(customValidation.Warnings);
            
            return new ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors,
                Warnings = warnings
            };
        }
        
        /// <summary>
        /// Validate individual fields
        /// </summary>
        protected virtual ValidationResult ValidateFields(Dictionary<string, object> config)
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            
            foreach (var kvp in config)
            {
                var validation = ValidateField(kvp.Key, kvp.Value);
                errors.AddRange(validation.Errors);
                warnings.AddRange(validation.Warnings);
            }
            
            return new ValidationResult { Errors = errors, Warnings = warnings };
        }
        
        /// <summary>
        /// Validate a single field
        /// </summary>
        protected virtual ValidationResult ValidateField(string field, object value)
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            
            // Check if field is known
            if (!RequiredFields.Contains(field) && !OptionalFields.Contains(field))
            {
                warnings.Add($"Unknown field '{field}'");
            }
            
            // Type validation
            var typeValidation = ValidateFieldType(field, value);
            errors.AddRange(typeValidation.Errors);
            
            return new ValidationResult { Errors = errors, Warnings = warnings };
        }
        
        /// <summary>
        /// Validate field type
        /// </summary>
        protected virtual ValidationResult ValidateFieldType(string field, object value)
        {
            // Override in subclasses for specific type validation
            return new ValidationResult();
        }
        
        /// <summary>
        /// Custom validation logic
        /// </summary>
        protected virtual ValidationResult CustomValidate(Dictionary<string, object> config)
        {
            // Override in subclasses for custom validation
            return new ValidationResult();
        }
        
        /// <summary>
        /// Execute operator
        /// </summary>
        public virtual async Task<object> ExecuteAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            try
            {
                // Merge with default configuration
                var mergedConfig = new Dictionary<string, object>(DefaultConfig);
                foreach (var kvp in config)
                {
                    mergedConfig[kvp.Key] = kvp.Value;
                }
                
                // Pre-execution hooks
                BeforeExecute(mergedConfig, context);
                
                // Execute the operator
                var result = await ExecuteOperatorAsync(mergedConfig, context);
                
                // Post-execution hooks
                AfterExecute(mergedConfig, context, result);
                
                return result;
            }
            catch (Exception ex)
            {
                HandleError(ex, config, context);
                throw;
            }
        }
        
        /// <summary>
        /// Execute operator (synchronous version)
        /// </summary>
        public virtual object Execute(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            return ExecuteAsync(config, context).GetAwaiter().GetResult();
        }
        
        /// <summary>
        /// Execute the actual operator logic
        /// </summary>
        protected abstract Task<object> ExecuteOperatorAsync(Dictionary<string, object> config, Dictionary<string, object> context);
        
        /// <summary>
        /// Pre-execution hook
        /// </summary>
        protected virtual void BeforeExecute(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            if (DebugMode)
            {
                Log("debug", $"Executing {GetName()} operator", new Dictionary<string, object>
                {
                    ["config"] = config,
                    ["context"] = context
                });
            }
        }
        
        /// <summary>
        /// Post-execution hook
        /// </summary>
        protected virtual void AfterExecute(Dictionary<string, object> config, Dictionary<string, object> context, object result)
        {
            if (DebugMode)
            {
                Log("info", $"{GetName()} operator completed", new Dictionary<string, object>
                {
                    ["result_type"] = result?.GetType().Name
                });
            }
        }
        
        /// <summary>
        /// Handle errors
        /// </summary>
        protected virtual void HandleError(Exception ex, Dictionary<string, object> config, Dictionary<string, object> context)
        {
            Log("error", $"Operator {GetName()} failed", new Dictionary<string, object>
            {
                ["error"] = ex.Message,
                ["stack_trace"] = ex.StackTrace,
                ["config"] = config
            });
            
            EmitErrorMetric(ex);
        }
        
        /// <summary>
        /// Emit error metric
        /// </summary>
        protected virtual void EmitErrorMetric(Exception ex)
        {
            // Override in subclasses to emit metrics
        }
        
        /// <summary>
        /// Get context value
        /// </summary>
        protected virtual T GetContextValue<T>(Dictionary<string, object> context, string key, T defaultValue = default)
        {
            if (context.TryGetValue(key, out var value))
            {
                if (value is T typedValue)
                {
                    return typedValue;
                }
                
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
        
        /// <summary>
        /// Resolve variable references
        /// </summary>
        protected virtual object ResolveVariable(object value, Dictionary<string, object> context)
        {
            if (value is string strValue)
            {
                // Handle @variable references
                if (strValue.StartsWith("@variable(") && strValue.EndsWith(")"))
                {
                    var varName = strValue.Substring(10, strValue.Length - 11).Trim('"', '\'');
                    if (context.TryGetValue(varName, out var varValue))
                    {
                        return varValue;
                    }
                }
                
                // Handle @env references
                if (strValue.StartsWith("@env(") && strValue.EndsWith(")"))
                {
                    var envVar = strValue.Substring(5, strValue.Length - 6).Trim('"', '\'');
                    return Environment.GetEnvironmentVariable(envVar) ?? "";
                }
            }
            
            return value;
        }
        
        /// <summary>
        /// Set debug mode
        /// </summary>
        public virtual void SetDebugMode(bool enabled)
        {
            DebugMode = enabled;
        }
        
        /// <summary>
        /// Cleanup resources
        /// </summary>
        public virtual void Cleanup()
        {
            // Override in subclasses to cleanup resources
        }
        
        /// <summary>
        /// Create error response
        /// </summary>
        protected virtual Dictionary<string, object> CreateErrorResponse(string code, string message, Dictionary<string, object> context = null)
        {
            return new Dictionary<string, object>
            {
                ["error"] = true,
                ["code"] = code,
                ["message"] = message,
                ["timestamp"] = DateTime.UtcNow,
                ["trace_id"] = GenerateTraceId(),
                ["context"] = context ?? new Dictionary<string, object>()
            };
        }
        
        /// <summary>
        /// Get error suggestions
        /// </summary>
        protected virtual List<string> GetErrorSuggestions(string code)
        {
            var suggestions = new List<string>();
            
            switch (code)
            {
                case "INVALID_CONFIG":
                    suggestions.Add("Check that all required fields are provided");
                    suggestions.Add("Verify field types match expected values");
                    suggestions.Add("Review operator documentation for correct usage");
                    break;
                case "EXECUTION_FAILED":
                    suggestions.Add("Check external service connectivity");
                    suggestions.Add("Verify authentication credentials");
                    suggestions.Add("Review error logs for specific details");
                    break;
                case "TIMEOUT":
                    suggestions.Add("Increase timeout value in configuration");
                    suggestions.Add("Check network connectivity");
                    suggestions.Add("Verify external service is responding");
                    break;
                case "CONNECTION_FAILED":
                    suggestions.Add("Verify connection parameters (host, port, etc.)");
                    suggestions.Add("Check firewall and network settings");
                    suggestions.Add("Ensure external service is running");
                    break;
            }
            
            return suggestions;
        }
        
        /// <summary>
        /// Generate trace ID
        /// </summary>
        protected virtual string GenerateTraceId()
        {
            return Guid.NewGuid().ToString("N");
        }
        
        /// <summary>
        /// Log message
        /// </summary>
        protected virtual void Log(string level, string message, Dictionary<string, object> context = null)
        {
            var logEntry = new Dictionary<string, object>
            {
                ["timestamp"] = DateTime.UtcNow,
                ["level"] = level,
                ["operator"] = GetName(),
                ["message"] = message,
                ["trace_id"] = GenerateTraceId()
            };
            
            if (context != null)
            {
                foreach (var kvp in context)
                {
                    logEntry[kvp.Key] = kvp.Value;
                }
            }
            
            // In a real implementation, this would go to a proper logging system
            if (DebugMode || level == "error")
            {
                Console.WriteLine($"[{level.ToUpper()}] {GetName()}: {message}");
            }
        }
    }
    
    /// <summary>
    /// Validation result
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; } = true;
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
    }
} 