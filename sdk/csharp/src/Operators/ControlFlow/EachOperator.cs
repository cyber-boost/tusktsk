using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TuskLang.Operators.ControlFlow
{
    /// <summary>
    /// Each Operator for TuskLang C# SDK
    /// 
    /// Provides collection iteration capabilities with support for:
    /// - Array/collection iteration
    /// - Key-value pair iteration
    /// - Async iteration support
    /// - Break and continue functionality
    /// - Loop variable tracking
    /// - Parallel execution
    /// 
    /// Usage:
    /// ```csharp
    /// // Array iteration
    /// var result = @each({
    ///   collection: [1, 2, 3, 4, 5],
    ///   body: "console.log('Item: ' + item)"
    /// })
    /// 
    /// // Dictionary iteration
    /// var result = @each({
    ///   collection: {name: "John", age: 30},
    ///   body: "console.log(key + ': ' + value)"
    /// })
    /// ```
    /// </summary>
    public class EachOperator : BaseOperator
    {
        public EachOperator()
        {
            Version = "2.0.0";
            RequiredFields = new List<string> { "collection", "body" };
            OptionalFields = new List<string> 
            { 
                "item", "key", "index", "break_condition", "continue_condition", 
                "parallel", "max_concurrency", "timeout", "retry_on_error" 
            };
            
            DefaultConfig = new Dictionary<string, object>
            {
                ["item"] = "item",
                ["key"] = "key",
                ["index"] = "index",
                ["parallel"] = false,
                ["max_concurrency"] = 10,
                ["timeout"] = 300,
                ["retry_on_error"] = false
            };
        }
        
        public override string GetName() => "each";
        
        protected override string GetDescription() => "Collection iteration operator for processing arrays and dictionaries";
        
        protected override Dictionary<string, string> GetExamples()
        {
            return new Dictionary<string, string>
            {
                ["array"] = "@each({collection: [1,2,3], body: \"console.log(item)\"})",
                ["dictionary"] = "@each({collection: {a: 1, b: 2}, body: \"console.log(key + ': ' + value)\"})",
                ["parallel"] = "@each({collection: items, body: \"process(item)\", parallel: true})",
                ["break"] = "@each({collection: items, body: \"process(item)\", break_condition: \"item == null\"})"
            };
        }
        
        protected override Dictionary<string, string> GetErrorCodes()
        {
            return new Dictionary<string, string>
            {
                ["INVALID_COLLECTION"] = "Invalid collection provided",
                ["EMPTY_COLLECTION"] = "Collection is empty",
                ["BREAK_CONDITION_ERROR"] = "Error in break condition evaluation",
                ["CONTINUE_CONDITION_ERROR"] = "Error in continue condition evaluation",
                ["BODY_EXECUTION_ERROR"] = "Error executing iteration body",
                ["PARALLEL_EXECUTION_ERROR"] = "Error in parallel execution",
                ["TIMEOUT_EXCEEDED"] = "Iteration timeout exceeded"
            };
        }
        
        protected override async Task<object> ExecuteOperatorAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var collection = ResolveVariable(config.GetValueOrDefault("collection"), context);
            var body = GetContextValue<string>(config, "body", "");
            var itemVariable = GetContextValue<string>(config, "item", "item");
            var keyVariable = GetContextValue<string>(config, "key", "key");
            var indexVariable = GetContextValue<string>(config, "index", "index");
            var breakCondition = GetContextValue<string>(config, "break_condition", "");
            var continueCondition = GetContextValue<string>(config, "continue_condition", "");
            var parallel = GetContextValue<bool>(config, "parallel", false);
            var maxConcurrency = GetContextValue<int>(config, "max_concurrency", 10);
            var timeout = GetContextValue<int>(config, "timeout", 300);
            var retryOnError = GetContextValue<bool>(config, "retry_on_error", false);
            
            if (collection == null)
                throw new ArgumentException("Collection is required");
            
            if (string.IsNullOrEmpty(body))
                throw new ArgumentException("Body is required");
            
            var items = new List<KeyValuePair<string, object>>();
            
            // Convert collection to key-value pairs
            if (collection is Dictionary<string, object> dict)
            {
                foreach (var kvp in dict)
                {
                    items.Add(new KeyValuePair<string, object>(kvp.Key, kvp.Value));
                }
            }
            else if (collection is System.Collections.IDictionary idict)
            {
                foreach (System.Collections.DictionaryEntry entry in idict)
                {
                    items.Add(new KeyValuePair<string, object>(entry.Key?.ToString() ?? "", entry.Value));
                }
            }
            else if (collection is System.Collections.IEnumerable enumerable)
            {
                int index = 0;
                foreach (var item in enumerable)
                {
                    items.Add(new KeyValuePair<string, object>(index.ToString(), item));
                    index++;
                }
            }
            else
            {
                items.Add(new KeyValuePair<string, object>("0", collection));
            }
            
            if (items.Count == 0)
            {
                return new Dictionary<string, object>
                {
                    ["results"] = new List<object>(),
                    ["iterations"] = 0,
                    ["items_count"] = 0
                };
            }
            
            var results = new List<object>();
            var shouldBreak = false;
            var startTime = DateTime.UtcNow;
            
            if (parallel)
            {
                // Parallel execution with concurrency limit
                var semaphore = new System.Threading.SemaphoreSlim(maxConcurrency);
                var tasks = new List<Task<object>>();
                
                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    var task = Task.Run(async () =>
                    {
                        await semaphore.WaitAsync();
                        try
                        {
                            var loopContext = new Dictionary<string, object>(context)
                            {
                                [itemVariable] = item.Value,
                                [keyVariable] = item.Key,
                                [indexVariable] = i,
                                ["first"] = i == 0,
                                ["last"] = i == items.Count - 1
                            };
                            
                            return await ExecuteBodyAsync(body, loopContext);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    });
                    
                    tasks.Add(task);
                }
                
                var taskResults = await Task.WhenAll(tasks);
                results.AddRange(taskResults);
            }
            else
            {
                // Sequential execution
                for (int i = 0; i < items.Count; i++)
                {
                    // Check timeout
                    if ((DateTime.UtcNow - startTime).TotalSeconds > timeout)
                    {
                        Log("warning", "Each iteration timeout exceeded", new Dictionary<string, object>
                        {
                            ["timeout"] = timeout,
                            ["iterations"] = i
                        });
                        break;
                    }
                    
                    var item = items[i];
                    
                    // Create iteration context
                    var loopContext = new Dictionary<string, object>(context)
                    {
                        [itemVariable] = item.Value,
                        [keyVariable] = item.Key,
                        [indexVariable] = i,
                        ["first"] = i == 0,
                        ["last"] = i == items.Count - 1
                    };
                    
                    // Check break condition
                    if (!string.IsNullOrEmpty(breakCondition))
                    {
                        try
                        {
                            var breakResult = EvaluateCondition(breakCondition, loopContext);
                            if (breakResult is bool shouldBreakNow && shouldBreakNow)
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
                            var continueResult = EvaluateCondition(continueCondition, loopContext);
                            if (continueResult is bool shouldContinue && shouldContinue)
                            {
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
                    
                    // Execute iteration body
                    try
                    {
                        var bodyResult = await ExecuteBodyAsync(body, loopContext);
                        if (bodyResult != null)
                        {
                            results.Add(bodyResult);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log("error", "Iteration body execution failed", new Dictionary<string, object>
                        {
                            ["iteration"] = i,
                            ["error"] = ex.Message
                        });
                        
                        if (retryOnError)
                        {
                            i--; // Retry this iteration
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
                                throw new ArgumentException($"Iteration body execution failed: {ex.Message}");
                            }
                        }
                    }
                }
            }
            
            var result = new Dictionary<string, object>
            {
                ["results"] = results,
                ["iterations"] = items.Count,
                ["items_count"] = items.Count,
                ["broken"] = shouldBreak,
                ["parallel"] = parallel,
                ["execution_time"] = (DateTime.UtcNow - startTime).TotalSeconds
            };
            
            Log("info", "Each iteration completed", new Dictionary<string, object>
            {
                ["iterations"] = items.Count,
                ["parallel"] = parallel,
                ["broken"] = shouldBreak
            });
            
            return result;
        }
        
        /// <summary>
        /// Execute iteration body
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
            
            return result;
        }
        
        /// <summary>
        /// Evaluate condition expression
        /// </summary>
        private object EvaluateCondition(string condition, Dictionary<string, object> context)
        {
            // This is a simplified implementation
            // In a real implementation, you would parse and evaluate the condition
            
            var evaluatedCondition = condition;
            foreach (var kvp in context)
            {
                var value = kvp.Value?.ToString() ?? "";
                evaluatedCondition = evaluatedCondition.Replace(kvp.Key, value);
            }
            
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
                    return EvaluateNumericCondition(evaluatedCondition);
                }
                catch
                {
                    return !string.IsNullOrEmpty(evaluatedCondition);
                }
            }
            
            return !string.IsNullOrEmpty(evaluatedCondition);
        }
        
        /// <summary>
        /// Evaluate numeric condition
        /// </summary>
        private bool EvaluateNumericCondition(string condition)
        {
            // Very basic numeric condition evaluation
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
        
        protected override ValidationResult CustomValidate(Dictionary<string, object> config)
        {
            var result = new ValidationResult();
            
            if (!config.ContainsKey("collection"))
            {
                result.Errors.Add("Collection is required");
            }
            
            if (!config.ContainsKey("body"))
            {
                result.Errors.Add("Body is required");
            }
            
            if (config.TryGetValue("max_concurrency", out var maxConc) && maxConc is int maxValue && maxValue <= 0)
            {
                result.Errors.Add("Max concurrency must be positive");
            }
            
            if (config.TryGetValue("timeout", out var timeout) && timeout is int timeoutValue && timeoutValue <= 0)
            {
                result.Errors.Add("Timeout must be positive");
            }
            
            return result;
        }
    }
} 