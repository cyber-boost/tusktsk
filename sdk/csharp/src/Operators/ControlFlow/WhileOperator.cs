using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TuskLang.Operators.ControlFlow
{
    /// <summary>
    /// While Operator for TuskLang C# SDK
    /// 
    /// Provides while loop control flow capabilities with support for:
    /// - Conditional iteration
    /// - Break and continue functionality
    /// - Loop variable tracking
    /// - Maximum iteration limits
    /// - Condition evaluation
    /// - Loop state management
    /// 
    /// Usage:
    /// ```csharp
    /// // Basic while loop
    /// var result = @while({
    ///   condition: "i < 10",
    ///   body: "i++"
    /// })
    /// 
    /// // While loop with break condition
    /// var result = @while({
    ///   condition: "true",
    ///   body: "process(item)",
    ///   break_condition: "item == null"
    /// })
    /// ```
    /// </summary>
    public class WhileOperator : BaseOperator
    {
        public WhileOperator()
        {
            Version = "2.0.0";
            RequiredFields = new List<string> { "condition", "body" };
            OptionalFields = new List<string> 
            { 
                "break_condition", "continue_condition", "max_iterations", "initial_state", 
                "loop_variables", "timeout", "parallel", "retry_on_error" 
            };
            
            DefaultConfig = new Dictionary<string, object>
            {
                ["max_iterations"] = 10000,
                ["timeout"] = 300,
                ["parallel"] = false,
                ["retry_on_error"] = false
            };
        }
        
        public override string GetName() => "while";
        
        protected override string GetDescription() => "While loop control flow operator for conditional iteration";
        
        protected override Dictionary<string, string> GetExamples()
        {
            return new Dictionary<string, string>
            {
                ["basic"] = "@while({condition: \"i < 10\", body: \"i++\"})",
                ["break"] = "@while({condition: \"true\", body: \"process()\", break_condition: \"done\"})",
                ["state"] = "@while({condition: \"count > 0\", body: \"count--\", initial_state: {count: 5}})",
                ["timeout"] = "@while({condition: \"!complete\", body: \"check()\", timeout: 60})"
            };
        }
        
        protected override Dictionary<string, string> GetErrorCodes()
        {
            return new Dictionary<string, string>
            {
                ["INVALID_CONDITION"] = "Invalid while loop condition",
                ["MAX_ITERATIONS_EXCEEDED"] = "Maximum iterations exceeded",
                ["TIMEOUT_EXCEEDED"] = "Loop timeout exceeded",
                ["BREAK_CONDITION_ERROR"] = "Error in break condition evaluation",
                ["CONTINUE_CONDITION_ERROR"] = "Error in continue condition evaluation",
                ["BODY_EXECUTION_ERROR"] = "Error executing loop body",
                ["CONDITION_EVALUATION_ERROR"] = "Error evaluating loop condition"
            };
        }
        
        protected override async Task<object> ExecuteOperatorAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var condition = GetContextValue<string>(config, "condition", "");
            var body = GetContextValue<string>(config, "body", "");
            var breakCondition = GetContextValue<string>(config, "break_condition", "");
            var continueCondition = GetContextValue<string>(config, "continue_condition", "");
            var maxIterations = GetContextValue<int>(config, "max_iterations", 10000);
            var timeout = GetContextValue<int>(config, "timeout", 300);
            var parallel = GetContextValue<bool>(config, "parallel", false);
            var retryOnError = GetContextValue<bool>(config, "retry_on_error", false);
            
            if (string.IsNullOrEmpty(condition))
                throw new ArgumentException("Loop condition is required");
            
            if (string.IsNullOrEmpty(body))
                throw new ArgumentException("Loop body is required");
            
            // Initialize loop state
            var loopState = new Dictionary<string, object>(context);
            
            // Add initial state if provided
            if (config.TryGetValue("initial_state", out var initialState) && initialState is Dictionary<string, object> initialStateDict)
            {
                foreach (var kvp in initialStateDict)
                {
                    loopState[kvp.Key] = kvp.Value;
                }
            }
            
            var results = new List<object>();
            var iterations = 0;
            var shouldBreak = false;
            var startTime = DateTime.UtcNow;
            
            while (true)
            {
                // Check timeout
                if ((DateTime.UtcNow - startTime).TotalSeconds > timeout)
                {
                    Log("warning", "While loop timeout exceeded", new Dictionary<string, object>
                    {
                        ["timeout"] = timeout,
                        ["iterations"] = iterations
                    });
                    break;
                }
                
                // Check max iterations
                if (iterations >= maxIterations)
                {
                    Log("warning", "Maximum iterations exceeded", new Dictionary<string, object>
                    {
                        ["iterations"] = iterations,
                        ["max_iterations"] = maxIterations
                    });
                    break;
                }
                
                // Evaluate main condition
                bool conditionResult;
                try
                {
                    conditionResult = EvaluateCondition(condition, loopState);
                }
                catch (Exception ex)
                {
                    Log("error", "Condition evaluation failed", new Dictionary<string, object>
                    {
                        ["condition"] = condition,
                        ["error"] = ex.Message
                    });
                    
                    if (retryOnError)
                    {
                        iterations++;
                        continue;
                    }
                    else
                    {
                        throw new ArgumentException($"Condition evaluation failed: {ex.Message}");
                    }
                }
                
                if (!conditionResult)
                {
                    break;
                }
                
                // Check break condition
                if (!string.IsNullOrEmpty(breakCondition))
                {
                    try
                    {
                        var breakResult = EvaluateCondition(breakCondition, loopState);
                        if (breakResult)
                        {
                            shouldBreak = true;
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log("error", "Break condition evaluation failed", new Dictionary<string, object>
                        {
                            ["condition"] = breakCondition,
                            ["error"] = ex.Message
                        });
                    }
                }
                
                // Check continue condition
                if (!string.IsNullOrEmpty(continueCondition))
                {
                    try
                    {
                        var continueResult = EvaluateCondition(continueCondition, loopState);
                        if (continueResult)
                        {
                            iterations++;
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log("error", "Continue condition evaluation failed", new Dictionary<string, object>
                        {
                            ["condition"] = continueCondition,
                            ["error"] = ex.Message
                        });
                    }
                }
                
                // Execute loop body
                try
                {
                    var bodyResult = await ExecuteBodyAsync(body, loopState);
                    if (bodyResult != null)
                    {
                        results.Add(bodyResult);
                    }
                }
                catch (Exception ex)
                {
                    Log("error", "Loop body execution failed", new Dictionary<string, object>
                    {
                        ["iteration"] = iterations,
                        ["error"] = ex.Message
                    });
                    
                    if (retryOnError)
                    {
                        iterations++;
                        continue;
                    }
                    else
                    {
                        // Check if this is a break exception
                        if (ex.Message.Contains("break"))
                        {
                            shouldBreak = true;
                            break;
                        }
                        else
                        {
                            throw new ArgumentException($"Loop body execution failed: {ex.Message}");
                        }
                    }
                }
                
                iterations++;
            }
            
            var result = new Dictionary<string, object>
            {
                ["results"] = results,
                ["iterations"] = iterations,
                ["condition"] = condition,
                ["broken"] = shouldBreak,
                ["final_state"] = loopState,
                ["execution_time"] = (DateTime.UtcNow - startTime).TotalSeconds
            };
            
            Log("info", "While loop completed", new Dictionary<string, object>
            {
                ["iterations"] = iterations,
                ["condition"] = condition,
                ["broken"] = shouldBreak
            });
            
            return result;
        }
        
        /// <summary>
        /// Evaluate condition expression
        /// </summary>
        private bool EvaluateCondition(string condition, Dictionary<string, object> context)
        {
            // This is a simplified implementation
            // In a real implementation, you would parse and evaluate the condition properly
            
            // Simple variable substitution and basic evaluation
            var evaluatedCondition = condition;
            foreach (var kvp in context)
            {
                var value = kvp.Value?.ToString() ?? "";
                evaluatedCondition = evaluatedCondition.Replace(kvp.Key, value);
            }
            
            // Basic boolean evaluation
            evaluatedCondition = evaluatedCondition.ToLower().Trim();
            
            if (evaluatedCondition == "true" || evaluatedCondition == "1")
                return true;
            
            if (evaluatedCondition == "false" || evaluatedCondition == "0")
                return false;
            
            // Try to evaluate numeric comparisons
            if (evaluatedCondition.Contains("<") || evaluatedCondition.Contains(">") || 
                evaluatedCondition.Contains("==") || evaluatedCondition.Contains("!="))
            {
                try
                {
                    // This is a very basic implementation
                    // In a real system, you'd use a proper expression evaluator
                    return EvaluateNumericCondition(evaluatedCondition);
                }
                catch
                {
                    // If we can't evaluate it as numeric, treat as boolean
                    return !string.IsNullOrEmpty(evaluatedCondition);
                }
            }
            
            // Default to treating as boolean
            return !string.IsNullOrEmpty(evaluatedCondition);
        }
        
        /// <summary>
        /// Evaluate numeric condition
        /// </summary>
        private bool EvaluateNumericCondition(string condition)
        {
            // Very basic numeric condition evaluation
            // This is a simplified implementation
            
            if (condition.Contains("<="))
            {
                var parts = condition.Split("<=");
                if (parts.Length == 2 && double.TryParse(parts[0].Trim(), out var left) && 
                    double.TryParse(parts[1].Trim(), out var right))
                {
                    return left <= right;
                }
            }
            else if (condition.Contains(">="))
            {
                var parts = condition.Split(">=");
                if (parts.Length == 2 && double.TryParse(parts[0].Trim(), out var left) && 
                    double.TryParse(parts[1].Trim(), out var right))
                {
                    return left >= right;
                }
            }
            else if (condition.Contains("<"))
            {
                var parts = condition.Split("<");
                if (parts.Length == 2 && double.TryParse(parts[0].Trim(), out var left) && 
                    double.TryParse(parts[1].Trim(), out var right))
                {
                    return left < right;
                }
            }
            else if (condition.Contains(">"))
            {
                var parts = condition.Split(">");
                if (parts.Length == 2 && double.TryParse(parts[0].Trim(), out var left) && 
                    double.TryParse(parts[1].Trim(), out var right))
                {
                    return left > right;
                }
            }
            else if (condition.Contains("=="))
            {
                var parts = condition.Split("==");
                if (parts.Length == 2)
                {
                    var left = parts[0].Trim();
                    var right = parts[1].Trim();
                    return left == right;
                }
            }
            else if (condition.Contains("!="))
            {
                var parts = condition.Split("!=");
                if (parts.Length == 2)
                {
                    var left = parts[0].Trim();
                    var right = parts[1].Trim();
                    return left != right;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Execute loop body
        /// </summary>
        private async Task<object> ExecuteBodyAsync(string body, Dictionary<string, object> context)
        {
            // This is a simplified implementation
            // In a real implementation, you would parse and execute the body expression
            
            var result = body;
            foreach (var kvp in context)
            {
                result = result.Replace($"{{{kvp.Key}}}", kvp.Value?.ToString() ?? "");
            }
            
            // Simple variable assignment detection
            if (result.Contains("=") && !result.Contains("=="))
            {
                var parts = result.Split('=', 2);
                if (parts.Length == 2)
                {
                    var variable = parts[0].Trim();
                    var value = parts[1].Trim();
                    
                    // Try to parse as number
                    if (int.TryParse(value, out var intValue))
                    {
                        context[variable] = intValue;
                    }
                    else if (double.TryParse(value, out var doubleValue))
                    {
                        context[variable] = doubleValue;
                    }
                    else
                    {
                        context[variable] = value;
                    }
                }
            }
            
            return result;
        }
        
        protected override ValidationResult CustomValidate(Dictionary<string, object> config)
        {
            var result = new ValidationResult();
            
            if (!config.ContainsKey("condition"))
            {
                result.Errors.Add("Loop condition is required");
            }
            
            if (!config.ContainsKey("body"))
            {
                result.Errors.Add("Loop body is required");
            }
            
            if (config.TryGetValue("max_iterations", out var maxIter) && maxIter is int maxValue && maxValue <= 0)
            {
                result.Errors.Add("Max iterations must be positive");
            }
            
            if (config.TryGetValue("timeout", out var timeout) && timeout is int timeoutValue && timeoutValue <= 0)
            {
                result.Errors.Add("Timeout must be positive");
            }
            
            return result;
        }
    }
} 