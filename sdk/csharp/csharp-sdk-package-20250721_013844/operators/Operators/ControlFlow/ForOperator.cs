using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TuskLang.Operators.ControlFlow
{
    /// <summary>
    /// For Operator for TuskLang C# SDK
    /// 
    /// Provides for loop control flow capabilities with support for:
    /// - Numeric range iteration
    /// - Array/collection iteration
    /// - Custom step values
    /// - Break and continue functionality
    /// - Loop variable tracking
    /// - Nested loop support
    /// 
    /// Usage:
    /// ```csharp
    /// // Numeric range loop
    /// var result = @for({
    ///   start: 1,
    ///   end: 10,
    ///   step: 2,
    ///   body: "console.log('Iteration: ' + i)"
    /// })
    /// 
    /// // Array iteration
    /// var result = @for({
    ///   array: [1, 2, 3, 4, 5],
    ///   body: "sum += item"
    /// })
    /// ```
    /// </summary>
    public class ForOperator : BaseOperator
    {
        public ForOperator()
        {
            Version = "2.0.0";
            RequiredFields = new List<string> { "body" };
            OptionalFields = new List<string> 
            { 
                "start", "end", "step", "array", "collection", "variable", "index", 
                "break_condition", "continue_condition", "max_iterations", "parallel" 
            };
            
            DefaultConfig = new Dictionary<string, object>
            {
                ["start"] = 0,
                ["step"] = 1,
                ["variable"] = "i",
                ["index"] = "index",
                ["max_iterations"] = 10000,
                ["parallel"] = false
            };
        }
        
        public override string GetName() => "for";
        
        protected override string GetDescription() => "For loop control flow operator for iteration and repetition";
        
        protected override Dictionary<string, string> GetExamples()
        {
            return new Dictionary<string, string>
            {
                ["numeric"] = "@for({start: 1, end: 10, body: \"console.log(i)\"})",
                ["array"] = "@for({array: [1,2,3], body: \"sum += item\"})",
                ["step"] = "@for({start: 0, end: 100, step: 5, body: \"evens.push(i)\"})",
                ["break"] = "@for({start: 1, end: 100, break_condition: \"i > 50\", body: \"process(i)\"})"
            };
        }
        
        protected override Dictionary<string, string> GetErrorCodes()
        {
            return new Dictionary<string, string>
            {
                ["INVALID_RANGE"] = "Invalid loop range specified",
                ["MAX_ITERATIONS_EXCEEDED"] = "Maximum iterations exceeded",
                ["INVALID_ARRAY"] = "Invalid array or collection provided",
                ["BREAK_CONDITION_ERROR"] = "Error in break condition evaluation",
                ["CONTINUE_CONDITION_ERROR"] = "Error in continue condition evaluation",
                ["BODY_EXECUTION_ERROR"] = "Error executing loop body",
                ["PARALLEL_EXECUTION_ERROR"] = "Error in parallel execution"
            };
        }
        
        protected override async Task<object> ExecuteOperatorAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var body = GetContextValue<string>(config, "body", "");
            var array = ResolveVariable(config.GetValueOrDefault("array"), context);
            var collection = ResolveVariable(config.GetValueOrDefault("collection"), context);
            var parallel = GetContextValue<bool>(config, "parallel", false);
            
            if (array != null || collection != null)
            {
                return await ExecuteArrayLoopAsync(config, context, array ?? collection);
            }
            else
            {
                return await ExecuteNumericLoopAsync(config, context);
            }
        }
        
        /// <summary>
        /// Execute numeric range loop
        /// </summary>
        private async Task<object> ExecuteNumericLoopAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var start = GetContextValue<int>(config, "start", 0);
            var end = GetContextValue<int>(config, "end", 0);
            var step = GetContextValue<int>(config, "step", 1);
            var body = GetContextValue<string>(config, "body", "");
            var variable = GetContextValue<string>(config, "variable", "i");
            var breakCondition = GetContextValue<string>(config, "break_condition", "");
            var continueCondition = GetContextValue<string>(config, "continue_condition", "");
            var maxIterations = GetContextValue<int>(config, "max_iterations", 10000);
            
            if (string.IsNullOrEmpty(body))
                throw new ArgumentException("Loop body is required");
            
            if (step == 0)
                throw new ArgumentException("Step cannot be zero");
            
            var results = new List<object>();
            var iterations = 0;
            var shouldBreak = false;
            
            for (int i = start; (step > 0 && i <= end) || (step < 0 && i >= end); i += step)
            {
                if (iterations >= maxIterations)
                {
                    Log("warning", "Maximum iterations exceeded", new Dictionary<string, object>
                    {
                        ["iterations"] = iterations,
                        ["max_iterations"] = maxIterations
                    });
                    break;
                }
                
                // Create loop context
                var loopContext = new Dictionary<string, object>(context)
                {
                    [variable] = i,
                    ["index"] = iterations,
                    ["first"] = iterations == 0,
                    ["last"] = (step > 0 && i + step > end) || (step < 0 && i + step < end)
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
                    var bodyResult = await ExecuteBodyAsync(body, loopContext);
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
                    
                    // Check if this is a break exception
                    if (ex.Message.Contains("break"))
                    {
                        shouldBreak = true;
                        break;
                    }
                }
                
                iterations++;
            }
            
            var result = new Dictionary<string, object>
            {
                ["results"] = results,
                ["iterations"] = iterations,
                ["start"] = start,
                ["end"] = end,
                ["step"] = step,
                ["broken"] = shouldBreak
            };
            
            Log("info", "Numeric loop completed", new Dictionary<string, object>
            {
                ["iterations"] = iterations,
                ["start"] = start,
                ["end"] = end
            });
            
            return result;
        }
        
        /// <summary>
        /// Execute array/collection loop
        /// </summary>
        private async Task<object> ExecuteArrayLoopAsync(Dictionary<string, object> config, Dictionary<string, object> context, object collection)
        {
            var body = GetContextValue<string>(config, "body", "");
            var variable = GetContextValue<string>(config, "variable", "item");
            var indexVariable = GetContextValue<string>(config, "index", "index");
            var breakCondition = GetContextValue<string>(config, "break_condition", "");
            var continueCondition = GetContextValue<string>(config, "continue_condition", "");
            var parallel = GetContextValue<bool>(config, "parallel", false);
            
            if (string.IsNullOrEmpty(body))
                throw new ArgumentException("Loop body is required");
            
            var items = new List<object>();
            
            // Convert collection to list
            if (collection is System.Collections.IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    items.Add(item);
                }
            }
            else if (collection is List<object> list)
            {
                items = list;
            }
            else
            {
                items.Add(collection);
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
            
            if (parallel)
            {
                // Parallel execution
                var tasks = new List<Task<object>>();
                
                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    var loopContext = new Dictionary<string, object>(context)
                    {
                        [variable] = item,
                        [indexVariable] = i,
                        ["first"] = i == 0,
                        ["last"] = i == items.Count - 1
                    };
                    
                    tasks.Add(ExecuteBodyAsync(body, loopContext));
                }
                
                var taskResults = await Task.WhenAll(tasks);
                results.AddRange(taskResults);
            }
            else
            {
                // Sequential execution
                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    
                    // Create loop context
                    var loopContext = new Dictionary<string, object>(context)
                    {
                        [variable] = item,
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
                    
                    // Execute loop body
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
                        Log("error", "Loop body execution failed", new Dictionary<string, object>
                        {
                            ["iteration"] = i,
                            ["error"] = ex.Message
                        });
                        
                        // Check if this is a break exception
                        if (ex.Message.Contains("break"))
                        {
                            shouldBreak = true;
                            break;
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
                ["parallel"] = parallel
            };
            
            Log("info", "Array loop completed", new Dictionary<string, object>
            {
                ["iterations"] = items.Count,
                ["parallel"] = parallel
            });
            
            return result;
        }
        
        /// <summary>
        /// Execute loop body
        /// </summary>
        private async Task<object> ExecuteBodyAsync(string body, Dictionary<string, object> context)
        {
            // This is a simplified implementation
            // In a real implementation, you would parse and execute the body expression
            // For now, we'll just return the body as a string with context variables substituted
            
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
            // For now, we'll just check if the condition contains any context variables
            
            foreach (var kvp in context)
            {
                if (condition.Contains(kvp.Key))
                {
                    // Simple evaluation - if condition contains a variable, return true
                    return true;
                }
            }
            
            return false;
        }
        
        protected override ValidationResult CustomValidate(Dictionary<string, object> config)
        {
            var result = new ValidationResult();
            
            if (!config.ContainsKey("body"))
            {
                result.Errors.Add("Loop body is required");
            }
            
            if (config.TryGetValue("step", out var step) && step is int stepValue && stepValue == 0)
            {
                result.Errors.Add("Step cannot be zero");
            }
            
            if (config.TryGetValue("max_iterations", out var maxIter) && maxIter is int maxValue && maxValue <= 0)
            {
                result.Errors.Add("Max iterations must be positive");
            }
            
            return result;
        }
    }
} 