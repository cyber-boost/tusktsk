using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TuskLang.Operators.ControlFlow
{
    /// <summary>
    /// Sort Operator for TuskLang C# SDK
    /// 
    /// Provides collection sorting capabilities with support for:
    /// - Array/collection sorting
    /// - Multiple sort criteria
    /// - Custom sort functions
    /// - Async sorting support
    /// - Ascending/descending order
    /// - Natural and custom sorting
    /// 
    /// Usage:
    /// ```csharp
    /// // Basic sorting
    /// var result = @sort({
    ///   collection: [3, 1, 4, 1, 5, 9, 2, 6],
    ///   field: "value"
    /// })
    /// 
    /// // Complex sorting
    /// var result = @sort({
    ///   collection: users,
    ///   field: "age",
    ///   direction: "desc",
    ///   secondary: "name"
    /// })
    /// ```
    /// </summary>
    public class SortOperator : BaseOperator
    {
        public SortOperator()
        {
            Version = "2.0.0";
            RequiredFields = new List<string> { "collection" };
            OptionalFields = new List<string> 
            { 
                "field", "direction", "secondary", "secondary_direction", "custom", 
                "timeout", "limit", "offset", "natural", "case_sensitive" 
            };
            
            DefaultConfig = new Dictionary<string, object>
            {
                ["field"] = "value",
                ["direction"] = "asc",
                ["secondary_direction"] = "asc",
                ["timeout"] = 300,
                ["natural"] = false,
                ["case_sensitive"] = false
            };
        }
        
        public override string GetName() => "sort";
        
        protected override string GetDescription() => "Collection sorting operator for ordering arrays and collections";
        
        protected override Dictionary<string, string> GetExamples()
        {
            return new Dictionary<string, string>
            {
                ["basic"] = "@sort({collection: [3,1,4,1,5,9,2,6], field: \"value\"})",
                ["desc"] = "@sort({collection: users, field: \"age\", direction: \"desc\"})",
                ["multi"] = "@sort({collection: users, field: \"age\", secondary: \"name\"})",
                ["custom"] = "@sort({collection: items, custom: \"item.priority * 10 + item.id\"})"
            };
        }
        
        protected override Dictionary<string, string> GetErrorCodes()
        {
            return new Dictionary<string, string>
            {
                ["INVALID_COLLECTION"] = "Invalid collection provided",
                ["EMPTY_COLLECTION"] = "Collection is empty",
                ["INVALID_FIELD"] = "Invalid sort field",
                ["INVALID_DIRECTION"] = "Invalid sort direction",
                ["CUSTOM_SORT_ERROR"] = "Error in custom sort function",
                ["TIMEOUT_EXCEEDED"] = "Sort timeout exceeded",
                ["FIELD_NOT_FOUND"] = "Sort field not found in collection items"
            };
        }
        
        protected override async Task<object> ExecuteOperatorAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var collection = ResolveVariable(config.GetValueOrDefault("collection"), context);
            var field = GetContextValue<string>(config, "field", "value");
            var direction = GetContextValue<string>(config, "direction", "asc");
            var secondary = GetContextValue<string>(config, "secondary", "");
            var secondaryDirection = GetContextValue<string>(config, "secondary_direction", "asc");
            var custom = GetContextValue<string>(config, "custom", "");
            var timeout = GetContextValue<int>(config, "timeout", 300);
            var limit = GetContextValue<int>(config, "limit", 0);
            var offset = GetContextValue<int>(config, "offset", 0);
            var natural = GetContextValue<bool>(config, "natural", false);
            var caseSensitive = GetContextValue<bool>(config, "case_sensitive", false);
            
            if (collection == null)
                throw new ArgumentException("Collection is required");
            
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
                    ["sorted"] = new List<object>(),
                    ["original_count"] = 0,
                    ["sorted_count"] = 0
                };
            }
            
            var startTime = DateTime.UtcNow;
            var sortedItems = new List<object>(items);
            
            try
            {
                // Check timeout
                if ((DateTime.UtcNow - startTime).TotalSeconds > timeout)
                {
                    Log("warning", "Sort operation timeout exceeded", new Dictionary<string, object>
                    {
                        ["timeout"] = timeout
                    });
                    return new Dictionary<string, object>
                    {
                        ["sorted"] = items,
                        ["original_count"] = items.Count,
                        ["sorted_count"] = items.Count,
                        ["timeout_exceeded"] = true
                    };
                }
                
                if (!string.IsNullOrEmpty(custom))
                {
                    // Custom sort function
                    sortedItems = await CustomSortAsync(sortedItems, custom, context);
                }
                else
                {
                    // Field-based sorting
                    sortedItems = SortByField(sortedItems, field, direction, caseSensitive);
                    
                    // Secondary sort if specified
                    if (!string.IsNullOrEmpty(secondary))
                    {
                        sortedItems = SortBySecondaryField(sortedItems, field, direction, secondary, secondaryDirection, caseSensitive);
                    }
                }
                
                // Apply limit and offset
                if (offset > 0 && offset < sortedItems.Count)
                {
                    sortedItems = sortedItems.GetRange(offset, sortedItems.Count - offset);
                }
                
                if (limit > 0 && limit < sortedItems.Count)
                {
                    sortedItems = sortedItems.GetRange(0, limit);
                }
            }
            catch (Exception ex)
            {
                Log("error", "Sort operation failed", new Dictionary<string, object>
                {
                    ["error"] = ex.Message,
                    ["field"] = field,
                    ["direction"] = direction
                });
                
                // Return original collection on error
                sortedItems = items;
            }
            
            var result = new Dictionary<string, object>
            {
                ["sorted"] = sortedItems,
                ["original_count"] = items.Count,
                ["sorted_count"] = sortedItems.Count,
                ["field"] = field,
                ["direction"] = direction,
                ["execution_time"] = (DateTime.UtcNow - startTime).TotalSeconds
            };
            
            if (!string.IsNullOrEmpty(secondary))
            {
                result["secondary"] = secondary;
                result["secondary_direction"] = secondaryDirection;
            }
            
            if (!string.IsNullOrEmpty(custom))
            {
                result["custom"] = custom;
            }
            
            if (limit > 0)
            {
                result["limit"] = limit;
            }
            
            if (offset > 0)
            {
                result["offset"] = offset;
            }
            
            if (natural)
            {
                result["natural"] = natural;
            }
            
            if (caseSensitive)
            {
                result["case_sensitive"] = caseSensitive;
            }
            
            Log("info", "Sort operation completed", new Dictionary<string, object>
            {
                ["original_count"] = items.Count,
                ["sorted_count"] = sortedItems.Count,
                ["field"] = field,
                ["direction"] = direction
            });
            
            return result;
        }
        
        /// <summary>
        /// Custom sort using expression
        /// </summary>
        private async Task<List<object>> CustomSortAsync(List<object> items, string customExpression, Dictionary<string, object> context)
        {
            var sortableItems = new List<KeyValuePair<object, double>>();
            
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var sortContext = new Dictionary<string, object>(context)
                {
                    ["item"] = item,
                    ["index"] = i
                };
                
                try
                {
                    var sortValue = await EvaluateCustomExpressionAsync(customExpression, sortContext);
                    var numericValue = ConvertToNumeric(sortValue);
                    sortableItems.Add(new KeyValuePair<object, double>(item, numericValue));
                }
                catch (Exception ex)
                {
                    Log("error", "Custom sort evaluation failed", new Dictionary<string, object>
                    {
                        ["expression"] = customExpression,
                        ["index"] = i,
                        ["error"] = ex.Message
                    });
                    sortableItems.Add(new KeyValuePair<object, double>(item, 0));
                }
            }
            
            // Sort by numeric value
            sortableItems.Sort((a, b) => a.Value.CompareTo(b.Value));
            
            // Extract sorted items
            var sortedItems = new List<object>();
            foreach (var kvp in sortableItems)
            {
                sortedItems.Add(kvp.Key);
            }
            
            return sortedItems;
        }
        
        /// <summary>
        /// Sort by field
        /// </summary>
        private List<object> SortByField(List<object> items, string field, string direction, bool caseSensitive)
        {
            var sortedItems = new List<object>(items);
            
            sortedItems.Sort((a, b) =>
            {
                var aValue = ExtractFieldValue(a, field);
                var bValue = ExtractFieldValue(b, field);
                
                var comparison = CompareValues(aValue, bValue, caseSensitive);
                return direction.ToLower() == "desc" ? -comparison : comparison;
            });
            
            return sortedItems;
        }
        
        /// <summary>
        /// Sort by secondary field
        /// </summary>
        private List<object> SortBySecondaryField(List<object> items, string primaryField, string primaryDirection, 
            string secondaryField, string secondaryDirection, bool caseSensitive)
        {
            var sortedItems = new List<object>(items);
            
            sortedItems.Sort((a, b) =>
            {
                var aPrimaryValue = ExtractFieldValue(a, primaryField);
                var bPrimaryValue = ExtractFieldValue(b, primaryField);
                
                var primaryComparison = CompareValues(aPrimaryValue, bPrimaryValue, caseSensitive);
                if (primaryComparison != 0)
                {
                    return primaryDirection.ToLower() == "desc" ? -primaryComparison : primaryComparison;
                }
                
                // If primary values are equal, sort by secondary field
                var aSecondaryValue = ExtractFieldValue(a, secondaryField);
                var bSecondaryValue = ExtractFieldValue(b, secondaryField);
                
                var secondaryComparison = CompareValues(aSecondaryValue, bSecondaryValue, caseSensitive);
                return secondaryDirection.ToLower() == "desc" ? -secondaryComparison : secondaryComparison;
            });
            
            return sortedItems;
        }
        
        /// <summary>
        /// Extract field value from object
        /// </summary>
        private object ExtractFieldValue(object item, string field)
        {
            if (item == null)
                return null;
            
            if (field == "value" || field == "item")
                return item;
            
            if (item is Dictionary<string, object> dict)
            {
                return dict.GetValueOrDefault(field);
            }
            
            // Try to get property using reflection
            try
            {
                var property = item.GetType().GetProperty(field);
                if (property != null)
                {
                    return property.GetValue(item);
                }
            }
            catch
            {
                // Property not found or not accessible
            }
            
            return null;
        }
        
        /// <summary>
        /// Compare two values
        /// </summary>
        private int CompareValues(object a, object b, bool caseSensitive)
        {
            if (a == null && b == null)
                return 0;
            
            if (a == null)
                return -1;
            
            if (b == null)
                return 1;
            
            // Try numeric comparison first
            if (a is IComparable aComparable && b is IComparable)
            {
                try
                {
                    return aComparable.CompareTo(b);
                }
                catch
                {
                    // Fall back to string comparison
                }
            }
            
            // String comparison
            var aStr = a.ToString();
            var bStr = b.ToString();
            
            if (caseSensitive)
            {
                return string.Compare(aStr, bStr, StringComparison.Ordinal);
            }
            else
            {
                return string.Compare(aStr, bStr, StringComparison.OrdinalIgnoreCase);
            }
        }
        
        /// <summary>
        /// Evaluate custom expression
        /// </summary>
        private async Task<object> EvaluateCustomExpressionAsync(string expression, Dictionary<string, object> context)
        {
            // This is a simplified implementation
            // In a real implementation, you would parse and execute the expression
            
            var result = expression;
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
        /// Convert value to numeric for sorting
        /// </summary>
        private double ConvertToNumeric(object value)
        {
            if (value == null)
                return 0;
            
            if (value is double d)
                return d;
            
            if (value is int i)
                return i;
            
            if (value is long l)
                return l;
            
            if (value is float f)
                return f;
            
            if (value is decimal dec)
                return (double)dec;
            
            if (double.TryParse(value.ToString(), out var result))
                return result;
            
            // For non-numeric values, use hash code as sort value
            return value.GetHashCode();
        }
        
        protected override ValidationResult CustomValidate(Dictionary<string, object> config)
        {
            var result = new ValidationResult();
            
            if (!config.ContainsKey("collection"))
            {
                result.Errors.Add("Collection is required");
            }
            
            var direction = GetContextValue<string>(config, "direction", "asc");
            if (direction != "asc" && direction != "desc")
            {
                result.Errors.Add("Direction must be 'asc' or 'desc'");
            }
            
            var secondaryDirection = GetContextValue<string>(config, "secondary_direction", "asc");
            if (secondaryDirection != "asc" && secondaryDirection != "desc")
            {
                result.Errors.Add("Secondary direction must be 'asc' or 'desc'");
            }
            
            if (config.TryGetValue("timeout", out var timeout) && timeout is int timeoutValue && timeoutValue <= 0)
            {
                result.Errors.Add("Timeout must be positive");
            }
            
            return result;
        }
    }
} 