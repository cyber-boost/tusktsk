using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TuskLang.Operators.ControlFlow
{
    /// <summary>
    /// Map Operator for TuskLang C# SDK
    /// 
    /// Provides collection transformation capabilities with support for:
    /// - Array/collection mapping
    /// - Complex transformation logic
    /// - Async mapping support
    /// - Parallel mapping execution
    /// - Custom mapping functions
    /// - Type conversion and transformation
    /// 
    /// Usage:
    /// ```csharp
    /// // Basic mapping
    /// var result = @map({
    ///   collection: [1, 2, 3, 4, 5],
    ///   transform: "item * 2"
    /// })
    /// 
    /// // Complex mapping
    /// var result = @map({
    ///   collection: users,
    ///   transform: "{name: user.name, age: user.age + 1}"
    /// })
    /// ```
    /// </summary>
    public class MapOperator : BaseOperator
    {
        public MapOperator()
        {
            Version = "2.0.0";
            RequiredFields = new List<string> { "collection", "transform" };
            OptionalFields = new List<string> 
            { 
                "item", "index", "parallel", "max_concurrency", "timeout", 
                "filter", "sort", "sort_direction", "limit", "offset" 
            };
            
            DefaultConfig = new Dictionary<string, object>
            {
                ["item"] = "item",
                ["index"] = "index",
                ["parallel"] = false,
                ["max_concurrency"] = 10,
                ["timeout"] = 300,
                ["sort_direction"] = "asc"
            };
        }
        
        public override string GetName() => "map";
        
        protected override string GetDescription() => "Collection transformation operator for mapping arrays and collections";
        
        protected override Dictionary<string, string> GetExamples()
        {
            return new Dictionary<string, string>
            {
                ["basic"] = "@map({collection: [1,2,3,4,5], transform: \"item * 2\"})",
                ["complex"] = "@map({collection: users, transform: \"{name: user.name, age: user.age + 1}\"})",
                ["parallel"] = "@map({collection: items, transform: \"process(item)\", parallel: true})",
                ["filter"] = "@map({collection: items, transform: \"item.value\", filter: \"item.active\"})"
            };
        }
        
        protected override Dictionary<string, string> GetErrorCodes()
        {
            return new Dictionary<string, string>
            {
                ["INVALID_COLLECTION"] = "Invalid collection provided",
                ["EMPTY_COLLECTION"] = "Collection is empty",
                ["INVALID_TRANSFORM"] = "Invalid transform expression",
                ["TRANSFORM_EVALUATION_ERROR"] = "Error evaluating transform expression",
                ["PARALLEL_EXECUTION_ERROR"] = "Error in parallel mapping",
                ["TIMEOUT_EXCEEDED"] = "Mapping timeout exceeded",
                ["FILTER_ERROR"] = "Error applying filter during mapping"
            };
        }
        
        protected override async Task<object> ExecuteOperatorAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var collection = ResolveVariable(config.GetValueOrDefault("collection"), context);
            var transform = GetContextValue<string>(config, "transform", "");
            var itemVariable = GetContextValue<string>(config, "item", "item");
            var indexVariable = GetContextValue<string>(config, "index", "index");
            var parallel = GetContextValue<bool>(config, "parallel", false);
            var maxConcurrency = GetContextValue<int>(config, "max_concurrency", 10);
            var timeout = GetContextValue<int>(config, "timeout", 300);
            var filter = GetContextValue<string>(config, "filter", "");
            var sort = GetContextValue<string>(config, "sort", "");
            var sortDirection = GetContextValue<string>(config, "sort_direction", "asc");
            var limit = GetContextValue<int>(config, "limit", 0);
            var offset = GetContextValue<int>(config, "offset", 0);
            
            if (collection == null)
                throw new ArgumentException("Collection is required");
            
            if (string.IsNullOrEmpty(transform))
                throw new ArgumentException("Transform expression is required");
            
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
                    ["mapped"] = new List<object>(),
                    ["original_count"] = 0,
                    ["mapped_count"] = 0,
                    ["transform"] = transform
                };
            }
            
            var mappedItems = new List<object>();
            var startTime = DateTime.UtcNow;
            
            if (parallel)
            {
                // Parallel mapping with concurrency limit
                var semaphore = new System.Threading.SemaphoreSlim(maxConcurrency);
                var tasks = new List<Task<KeyValuePair<int, object>>>();
                
                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    var index = i;
                    
                    var task = Task.Run(async () =>
                    {
                        await semaphore.WaitAsync();
                        try
                        {
                            var mapContext = new Dictionary<string, object>(context)
                            {
                                [itemVariable] = item,
                                [indexVariable] = index
                            };
                            
                            // Apply filter if specified
                            if (!string.IsNullOrEmpty(filter))
                            {
                                var shouldInclude = EvaluateCondition(filter, mapContext);
                                if (!shouldInclude)
                                {
                                    return new KeyValuePair<int, object>(index, null);
                                }
                            }
                            
                            var transformed = await ExecuteTransformAsync(transform, mapContext);
                            return new KeyValuePair<int, object>(index, transformed);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    });
                    
                    tasks.Add(task);
                }
                
                var taskResults = await Task.WhenAll(tasks);
                
                // Sort by original index to maintain order
                var sortedResults = new List<KeyValuePair<int, object>>(taskResults);
                sortedResults.Sort((a, b) => a.Key.CompareTo(b.Key));
                
                for (int i = 0; i < sortedResults.Count; i++)
                {
                    if (sortedResults[i].Value != null)
                    {
                        mappedItems.Add(sortedResults[i].Value);
                    }
                }
            }
            else
            {
                // Sequential mapping
                for (int i = 0; i < items.Count; i++)
                {
                    // Check timeout
                    if ((DateTime.UtcNow - startTime).TotalSeconds > timeout)
                    {
                        Log("warning", "Map operation timeout exceeded", new Dictionary<string, object>
                        {
                            ["timeout"] = timeout,
                            ["processed"] = i
                        });
                        break;
                    }
                    
                    var item = items[i];
                    
                    var mapContext = new Dictionary<string, object>(context)
                    {
                        [itemVariable] = item,
                        [indexVariable] = i
                    };
                    
                    try
                    {
                        // Apply filter if specified
                        if (!string.IsNullOrEmpty(filter))
                        {
                            var shouldInclude = EvaluateCondition(filter, mapContext);
                            if (!shouldInclude)
                            {
                                continue;
                            }
                        }
                        
                        var transformed = await ExecuteTransformAsync(transform, mapContext);
                        if (transformed != null)
                        {
                            mappedItems.Add(transformed);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log("error", "Transform evaluation failed", new Dictionary<string, object>
                        {
                            ["transform"] = transform,
                            ["index"] = i,
                            ["error"] = ex.Message
                        });
                    }
                }
            }
            
            // Apply sorting if specified
            if (!string.IsNullOrEmpty(sort))
            {
                try
                {
                    mappedItems = SortItems(mappedItems, sort, sortDirection);
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
            
            // Apply limit and offset
            if (offset > 0 && offset < mappedItems.Count)
            {
                mappedItems = mappedItems.GetRange(offset, mappedItems.Count - offset);
            }
            
            if (limit > 0 && limit < mappedItems.Count)
            {
                mappedItems = mappedItems.GetRange(0, limit);
            }
            
            var result = new Dictionary<string, object>
            {
                ["mapped"] = mappedItems,
                ["original_count"] = items.Count,
                ["mapped_count"] = mappedItems.Count,
                ["transform"] = transform,
                ["parallel"] = parallel,
                ["execution_time"] = (DateTime.UtcNow - startTime).TotalSeconds
            };
            
            if (!string.IsNullOrEmpty(filter))
            {
                result["filter"] = filter;
            }
            
            if (!string.IsNullOrEmpty(sort))
            {
                result["sort"] = sort;
                result["sort_direction"] = sortDirection;
            }
            
            if (limit > 0)
            {
                result["limit"] = limit;
            }
            
            if (offset > 0)
            {
                result["offset"] = offset;
            }
            
            Log("info", "Map operation completed", new Dictionary<string, object>
            {
                ["original_count"] = items.Count,
                ["mapped_count"] = mappedItems.Count,
                ["transform"] = transform,
                ["parallel"] = parallel
            });
            
            return result;
        }
        
        /// <summary>
        /// Execute transform expression
        /// </summary>
        private async Task<object> ExecuteTransformAsync(string transform, Dictionary<string, object> context)
        {
            // This is a simplified implementation
            // In a real implementation, you would parse and execute the transform expression
            
            var result = transform;
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
                if (parts.Length == 2 && double.TryParse(parts[0].Trim(), out var left) && 
                    double.TryParse(parts[1].Trim(), out var right))
                {
                    return left + right;
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
            
            if (!config.ContainsKey("transform"))
            {
                result.Errors.Add("Transform expression is required");
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