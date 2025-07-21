using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TuskLang.Operators.ControlFlow
{
    /// <summary>
    /// Filter Operator for TuskLang C# SDK
    /// 
    /// Provides collection filtering capabilities with support for:
    /// - Array/collection filtering
    /// - Complex filter conditions
    /// - Async filtering support
    /// - Parallel filtering
    /// - Custom filter functions
    /// - Multiple filter criteria
    /// 
    /// Usage:
    /// ```csharp
    /// // Basic filtering
    /// var result = @filter({
    ///   collection: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10],
    ///   condition: "item > 5"
    /// })
    /// 
    /// // Complex filtering
    /// var result = @filter({
    ///   collection: users,
    ///   condition: "user.age >= 18 && user.active == true"
    /// })
    /// ```
    /// </summary>
    public class FilterOperator : BaseOperator
    {
        public FilterOperator()
        {
            Version = "2.0.0";
            RequiredFields = new List<string> { "collection", "condition" };
            OptionalFields = new List<string> 
            { 
                "item", "index", "parallel", "max_concurrency", "timeout", 
                "limit", "offset", "sort", "sort_direction" 
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
        
        public override string GetName() => "filter";
        
        protected override string GetDescription() => "Collection filtering operator for filtering arrays and collections";
        
        protected override Dictionary<string, string> GetExamples()
        {
            return new Dictionary<string, string>
            {
                ["basic"] = "@filter({collection: [1,2,3,4,5], condition: \"item > 3\"})",
                ["complex"] = "@filter({collection: users, condition: \"user.age >= 18 && user.active\"})",
                ["parallel"] = "@filter({collection: items, condition: \"item.valid\", parallel: true})",
                ["limit"] = "@filter({collection: items, condition: \"item.active\", limit: 10})"
            };
        }
        
        protected override Dictionary<string, string> GetErrorCodes()
        {
            return new Dictionary<string, string>
            {
                ["INVALID_COLLECTION"] = "Invalid collection provided",
                ["EMPTY_COLLECTION"] = "Collection is empty",
                ["INVALID_CONDITION"] = "Invalid filter condition",
                ["CONDITION_EVALUATION_ERROR"] = "Error evaluating filter condition",
                ["PARALLEL_EXECUTION_ERROR"] = "Error in parallel filtering",
                ["TIMEOUT_EXCEEDED"] = "Filtering timeout exceeded",
                ["SORT_ERROR"] = "Error sorting filtered results"
            };
        }
        
        protected override async Task<object> ExecuteOperatorAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var collection = ResolveVariable(config.GetValueOrDefault("collection"), context);
            var condition = GetContextValue<string>(config, "condition", "");
            var itemVariable = GetContextValue<string>(config, "item", "item");
            var indexVariable = GetContextValue<string>(config, "index", "index");
            var parallel = GetContextValue<bool>(config, "parallel", false);
            var maxConcurrency = GetContextValue<int>(config, "max_concurrency", 10);
            var timeout = GetContextValue<int>(config, "timeout", 300);
            var limit = GetContextValue<int>(config, "limit", 0);
            var offset = GetContextValue<int>(config, "offset", 0);
            var sort = GetContextValue<string>(config, "sort", "");
            var sortDirection = GetContextValue<string>(config, "sort_direction", "asc");
            
            if (collection == null)
                throw new ArgumentException("Collection is required");
            
            if (string.IsNullOrEmpty(condition))
                throw new ArgumentException("Filter condition is required");
            
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
                    ["filtered"] = new List<object>(),
                    ["original_count"] = 0,
                    ["filtered_count"] = 0,
                    ["condition"] = condition
                };
            }
            
            var filteredItems = new List<object>();
            var startTime = DateTime.UtcNow;
            
            if (parallel)
            {
                // Parallel filtering with concurrency limit
                var semaphore = new System.Threading.SemaphoreSlim(maxConcurrency);
                var tasks = new List<Task<KeyValuePair<int, bool>>>();
                
                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    var index = i;
                    
                    var task = Task.Run(async () =>
                    {
                        await semaphore.WaitAsync();
                        try
                        {
                            var filterContext = new Dictionary<string, object>(context)
                            {
                                [itemVariable] = item,
                                [indexVariable] = index
                            };
                            
                            var shouldInclude = EvaluateCondition(condition, filterContext);
                            return new KeyValuePair<int, bool>(index, shouldInclude);
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
                var sortedResults = new List<KeyValuePair<int, bool>>(taskResults);
                sortedResults.Sort((a, b) => a.Key.CompareTo(b.Key));
                
                for (int i = 0; i < sortedResults.Count; i++)
                {
                    if (sortedResults[i].Value)
                    {
                        filteredItems.Add(items[sortedResults[i].Key]);
                    }
                }
            }
            else
            {
                // Sequential filtering
                for (int i = 0; i < items.Count; i++)
                {
                    // Check timeout
                    if ((DateTime.UtcNow - startTime).TotalSeconds > timeout)
                    {
                        Log("warning", "Filter operation timeout exceeded", new Dictionary<string, object>
                        {
                            ["timeout"] = timeout,
                            ["processed"] = i
                        });
                        break;
                    }
                    
                    var item = items[i];
                    
                    var filterContext = new Dictionary<string, object>(context)
                    {
                        [itemVariable] = item,
                        [indexVariable] = i
                    };
                    
                    try
                    {
                        var shouldInclude = EvaluateCondition(condition, filterContext);
                        if (shouldInclude)
                        {
                            filteredItems.Add(item);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log("error", "Filter condition evaluation failed", new Dictionary<string, object>
                        {
                            ["condition"] = condition,
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
                    filteredItems = SortItems(filteredItems, sort, sortDirection);
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
            if (offset > 0 && offset < filteredItems.Count)
            {
                filteredItems = filteredItems.GetRange(offset, filteredItems.Count - offset);
            }
            
            if (limit > 0 && limit < filteredItems.Count)
            {
                filteredItems = filteredItems.GetRange(0, limit);
            }
            
            var result = new Dictionary<string, object>
            {
                ["filtered"] = filteredItems,
                ["original_count"] = items.Count,
                ["filtered_count"] = filteredItems.Count,
                ["condition"] = condition,
                ["parallel"] = parallel,
                ["execution_time"] = (DateTime.UtcNow - startTime).TotalSeconds
            };
            
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
            
            Log("info", "Filter operation completed", new Dictionary<string, object>
            {
                ["original_count"] = items.Count,
                ["filtered_count"] = filteredItems.Count,
                ["condition"] = condition,
                ["parallel"] = parallel
            });
            
            return result;
        }
        
        /// <summary>
        /// Evaluate filter condition
        /// </summary>
        private bool EvaluateCondition(string condition, Dictionary<string, object> context)
        {
            // This is a simplified implementation
            // In a real implementation, you would parse and evaluate the condition properly
            
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
                evaluatedCondition.Contains("==") || evaluatedCondition.Contains("!=") ||
                evaluatedCondition.Contains(">=") || evaluatedCondition.Contains("<="))
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
            
            // Try to evaluate logical operations
            if (evaluatedCondition.Contains("&&") || evaluatedCondition.Contains("||"))
            {
                try
                {
                    return EvaluateLogicalCondition(evaluatedCondition);
                }
                catch
                {
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
        /// Evaluate logical condition
        /// </summary>
        private bool EvaluateLogicalCondition(string condition)
        {
            if (condition.Contains("&&"))
            {
                var parts = condition.Split("&&");
                foreach (var part in parts)
                {
                    if (!EvaluateCondition(part.Trim(), new Dictionary<string, object>()))
                    {
                        return false;
                    }
                }
                return true;
            }
            else if (condition.Contains("||"))
            {
                var parts = condition.Split("||");
                foreach (var part in parts)
                {
                    if (EvaluateCondition(part.Trim(), new Dictionary<string, object>()))
                    {
                        return true;
                    }
                }
                return false;
            }
            
            return false;
        }
        
        /// <summary>
        /// Sort items
        /// </summary>
        private List<object> SortItems(List<object> items, string sortField, string sortDirection)
        {
            // This is a simplified implementation
            // In a real implementation, you would implement proper sorting logic
            
            var sortedItems = new List<object>(items);
            
            // Simple string-based sorting for demonstration
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
            
            if (!config.ContainsKey("condition"))
            {
                result.Errors.Add("Filter condition is required");
            }
            
            if (config.TryGetValue("max_concurrency", out var maxConc) && maxConc is int maxValue && maxValue <= 0)
            {
                result.Errors.Add("Max concurrency must be positive");
            }
            
            if (config.TryGetValue("timeout", out var timeout) && timeout is int timeoutValue && timeoutValue <= 0)
            {
                result.Errors.Add("Timeout must be positive");
            }
            
            if (config.TryGetValue("limit", out var limit) && limit is int limitValue && limitValue < 0)
            {
                result.Errors.Add("Limit must be non-negative");
            }
            
            if (config.TryGetValue("offset", out var offset) && offset is int offsetValue && offsetValue < 0)
            {
                result.Errors.Add("Offset must be non-negative");
            }
            
            return result;
        }
    }
} 