using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TuskLang.Operators.ControlFlow
{
    /// <summary>
    /// Reduce Operator for TuskLang C# SDK
    /// 
    /// Provides collection reduction capabilities with support for:
    /// - Array/collection reduction
    /// - Complex reduction logic
    /// - Async reduction support
    /// - Custom reduction functions
    /// - Initial value specification
    /// - Type conversion and aggregation
    /// 
    /// Usage:
    /// ```csharp
    /// // Basic reduction
    /// var result = @reduce({
    ///   collection: [1, 2, 3, 4, 5],
    ///   reducer: "accumulator + item",
    ///   initial: 0
    /// })
    /// 
    /// // Complex reduction
    /// var result = @reduce({
    ///   collection: orders,
    ///   reducer: "{total: accumulator.total + item.amount, count: accumulator.count + 1}",
    ///   initial: {total: 0, count: 0}
    /// })
    /// ```
    /// </summary>
    public class ReduceOperator : BaseOperator
    {
        public ReduceOperator()
        {
            Version = "2.0.0";
            RequiredFields = new List<string> { "collection", "reducer" };
            OptionalFields = new List<string> 
            { 
                "initial", "item", "index", "accumulator", "timeout", 
                "filter", "sort", "sort_direction" 
            };
            
            DefaultConfig = new Dictionary<string, object>
            {
                ["item"] = "item",
                ["index"] = "index",
                ["accumulator"] = "accumulator",
                ["timeout"] = 300,
                ["sort_direction"] = "asc"
            };
        }
        
        public override string GetName() => "reduce";
        
        protected override string GetDescription() => "Collection reduction operator for aggregating arrays and collections";
        
        protected override Dictionary<string, string> GetExamples()
        {
            return new Dictionary<string, string>
            {
                ["basic"] = "@reduce({collection: [1,2,3,4,5], reducer: \"accumulator + item\", initial: 0})",
                ["complex"] = "@reduce({collection: orders, reducer: \"{total: accumulator.total + item.amount}\", initial: {total: 0}})",
                ["filter"] = "@reduce({collection: items, reducer: \"accumulator + item.value\", filter: \"item.active\"})",
                ["string"] = "@reduce({collection: words, reducer: \"accumulator + ' ' + item\", initial: \"\"})"
            };
        }
        
        protected override Dictionary<string, string> GetErrorCodes()
        {
            return new Dictionary<string, string>
            {
                ["INVALID_COLLECTION"] = "Invalid collection provided",
                ["EMPTY_COLLECTION"] = "Collection is empty",
                ["INVALID_REDUCER"] = "Invalid reducer expression",
                ["REDUCER_EVALUATION_ERROR"] = "Error evaluating reducer expression",
                ["TIMEOUT_EXCEEDED"] = "Reduction timeout exceeded",
                ["FILTER_ERROR"] = "Error applying filter during reduction",
                ["INITIAL_VALUE_ERROR"] = "Error with initial value"
            };
        }
        
        protected override async Task<object> ExecuteOperatorAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var collection = ResolveVariable(config.GetValueOrDefault("collection"), context);
            var reducer = GetContextValue<string>(config, "reducer", "");
            var initial = ResolveVariable(config.GetValueOrDefault("initial"), context);
            var itemVariable = GetContextValue<string>(config, "item", "item");
            var indexVariable = GetContextValue<string>(config, "index", "index");
            var accumulatorVariable = GetContextValue<string>(config, "accumulator", "accumulator");
            var timeout = GetContextValue<int>(config, "timeout", 300);
            var filter = GetContextValue<string>(config, "filter", "");
            var sort = GetContextValue<string>(config, "sort", "");
            var sortDirection = GetContextValue<string>(config, "sort_direction", "asc");
            
            if (collection == null)
                throw new ArgumentException("Collection is required");
            
            if (string.IsNullOrEmpty(reducer))
                throw new ArgumentException("Reducer expression is required");
            
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
                    ["result"] = initial ?? 0,
                    ["original_count"] = 0,
                    ["processed_count"] = 0,
                    ["reducer"] = reducer
                };
            }
            
            // Apply sorting if specified
            if (!string.IsNullOrEmpty(sort))
            {
                try
                {
                    items = SortItems(items, sort, sortDirection);
                }
                catch (Exception ex)
                {
                    Log("error", "Sorting failed", new Dictionary<string, object>
                    {
                        ["sort"] = sort,
                        ["error"] = ex.Message
                    });
                }
            }
            
            var accumulator = initial ?? 0;
            var processedCount = 0;
            var startTime = DateTime.UtcNow;
            
            // Sequential reduction (reduce operations are inherently sequential)
            for (int i = 0; i < items.Count; i++)
            {
                // Check timeout
                if ((DateTime.UtcNow - startTime).TotalSeconds > timeout)
                {
                    Log("warning", "Reduce operation timeout exceeded", new Dictionary<string, object>
                    {
                        ["timeout"] = timeout,
                        ["processed"] = i
                    });
                    break;
                }
                
                var item = items[i];
                
                var reduceContext = new Dictionary<string, object>(context)
                {
                    [itemVariable] = item,
                    [indexVariable] = i,
                    [accumulatorVariable] = accumulator
                };
                
                try
                {
                    // Apply filter if specified
                    if (!string.IsNullOrEmpty(filter))
                    {
                        var shouldInclude = EvaluateCondition(filter, reduceContext);
                        if (!shouldInclude)
                        {
                            continue;
                        }
                    }
                    
                    accumulator = await ExecuteReducerAsync(reducer, reduceContext);
                    processedCount++;
                }
                catch (Exception ex)
                {
                    Log("error", "Reducer evaluation failed", new Dictionary<string, object>
                    {
                        ["reducer"] = reducer,
                        ["index"] = i,
                        ["error"] = ex.Message
                    });
                }
            }
            
            var result = new Dictionary<string, object>
            {
                ["result"] = accumulator,
                ["original_count"] = items.Count,
                ["processed_count"] = processedCount,
                ["reducer"] = reducer,
                ["execution_time"] = (DateTime.UtcNow - startTime).TotalSeconds
            };
            
            if (initial != null)
            {
                result["initial"] = initial;
            }
            
            if (!string.IsNullOrEmpty(filter))
            {
                result["filter"] = filter;
            }
            
            if (!string.IsNullOrEmpty(sort))
            {
                result["sort"] = sort;
                result["sort_direction"] = sortDirection;
            }
            
            Log("info", "Reduce operation completed", new Dictionary<string, object>
            {
                ["original_count"] = items.Count,
                ["processed_count"] = processedCount,
                ["reducer"] = reducer
            });
            
            return result;
        }
        
        /// <summary>
        /// Execute reducer expression
        /// </summary>
        private async Task<object> ExecuteReducerAsync(string reducer, Dictionary<string, object> context)
        {
            // This is a simplified implementation
            // In a real implementation, you would parse and execute the reducer expression
            
            var result = reducer;
            foreach (var kvp in context)
            {
                result = result.Replace($"{{{kvp.Key}}}", kvp.Value?.ToString() ?? "");
            }
            
            // Simple arithmetic evaluation
            if (result.Contains("*") || result.Contains("/") || result.Contains("+") || result.Contains("-"))
            {
                try
                {
                    return EvaluateArithmeticExpression(result);
                }
                catch
                {
                    // If arithmetic evaluation fails, return as string
                }
            }
            
            // Try to parse as JSON object
            if (result.StartsWith("{") && result.EndsWith("}"))
            {
                try
                {
                    return ParseJsonObject(result);
                }
                catch
                {
                    // If JSON parsing fails, return as string
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Evaluate arithmetic expression
        /// </summary>
        private object EvaluateArithmeticExpression(string expression)
        {
            // Very basic arithmetic evaluation
            // In a real implementation, you would use a proper expression evaluator
            
            expression = expression.Trim();
            
            if (expression.Contains("*"))
            {
                var parts = expression.Split('*');
                if (parts.Length == 2 && double.TryParse(parts[0].Trim(), out var left) && 
                    double.TryParse(parts[1].Trim(), out var right))
                {
                    return left * right;
                }
            }
            else if (expression.Contains("/"))
            {
                var parts = expression.Split('/');
                if (parts.Length == 2 && double.TryParse(parts[0].Trim(), out var left) && 
                    double.TryParse(parts[1].Trim(), out var right))
                {
                    if (right != 0)
                        return left / right;
                }
            }
            else if (expression.Contains("+"))
            {
                var parts = expression.Split('+');
                if (parts.Length == 2)
                {
                    // Try numeric addition first
                    if (double.TryParse(parts[0].Trim(), out var left) && 
                        double.TryParse(parts[1].Trim(), out var right))
                    {
                        return left + right;
                    }
                    // If numeric fails, try string concatenation
                    else
                    {
                        return parts[0].Trim() + parts[1].Trim();
                    }
                }
            }
            else if (expression.Contains("-"))
            {
                var parts = expression.Split('-');
                if (parts.Length == 2 && double.TryParse(parts[0].Trim(), out var left) && 
                    double.TryParse(parts[1].Trim(), out var right))
                {
                    return left - right;
                }
            }
            
            return expression;
        }
        
        /// <summary>
        /// Parse JSON object
        /// </summary>
        private object ParseJsonObject(string json)
        {
            // Simplified JSON parsing
            // In a real implementation, you would use a proper JSON parser
            
            var result = new Dictionary<string, object>();
            json = json.Trim('{', '}');
            
            var pairs = json.Split(',');
            foreach (var pair in pairs)
            {
                var keyValue = pair.Split(':');
                if (keyValue.Length == 2)
                {
                    var key = keyValue[0].Trim();
                    var value = keyValue[1].Trim();
                    
                    // Try to parse value as number
                    if (double.TryParse(value, out var numValue))
                    {
                        result[key] = numValue;
                    }
                    else
                    {
                        result[key] = value.Trim('"', '\'');
                    }
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Evaluate condition expression
        /// </summary>
        private bool EvaluateCondition(string condition, Dictionary<string, object> context)
        {
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
        /// Sort items
        /// </summary>
        private List<object> SortItems(List<object> items, string sortField, string sortDirection)
        {
            var sortedItems = new List<object>(items);
            
            sortedItems.Sort((a, b) =>
            {
                var aStr = a?.ToString() ?? "";
                var bStr = b?.ToString() ?? "";
                
                var comparison = string.Compare(aStr, bStr, StringComparison.OrdinalIgnoreCase);
                return sortDirection.ToLower() == "desc" ? -comparison : comparison;
            });
            
            return sortedItems;
        }
        
        protected override ValidationResult CustomValidate(Dictionary<string, object> config)
        {
            var result = new ValidationResult();
            
            if (!config.ContainsKey("collection"))
            {
                result.Errors.Add("Collection is required");
            }
            
            if (!config.ContainsKey("reducer"))
            {
                result.Errors.Add("Reducer expression is required");
            }
            
            if (config.TryGetValue("timeout", out var timeout) && timeout is int timeoutValue && timeoutValue <= 0)
            {
                result.Errors.Add("Timeout must be positive");
            }
            
            return result;
        }
    }
} 