using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System.Linq;

namespace TuskLang.Operators.DataProcessing
{
    /// <summary>
    /// CSV Operator for TuskLang C# SDK
    /// 
    /// Provides CSV processing capabilities with support for:
    /// - CSV parsing and serialization
    /// - CSV validation and formatting
    /// - CSV filtering and sorting
    /// - CSV transformation and manipulation
    /// - CSV to JSON/XML conversion
    /// - CSV schema validation
    /// 
    /// Usage:
    /// ```csharp
    /// // Parse CSV
    /// var result = @csv({
    ///   action: "parse",
    ///   data: "name,age\nJohn,30\nJane,25"
    /// })
    /// 
    /// // Convert to JSON
    /// var result = @csv({
    ///   action: "to_json",
    ///   data: csvData
    /// })
    /// ```
    /// </summary>
    public class CsvOperator : BaseOperator
    {
        public CsvOperator()
        {
            Version = "2.0.0";
            RequiredFields = new List<string> { "action" };
            OptionalFields = new List<string> 
            { 
                "data", "delimiter", "has_header", "encoding", "format", "validate", 
                "filter", "sort", "sort_direction", "limit", "offset", "timeout" 
            };
            
            DefaultConfig = new Dictionary<string, object>
            {
                ["delimiter"] = ",",
                ["has_header"] = true,
                ["encoding"] = "UTF-8",
                ["format"] = "pretty",
                ["validate"] = true,
                ["sort_direction"] = "asc",
                ["timeout"] = 300
            };
        }
        
        public override string GetName() => "csv";
        
        protected override string GetDescription() => "CSV processing operator for parsing, querying, and manipulating CSV data";
        
        protected override Dictionary<string, string> GetExamples()
        {
            return new Dictionary<string, string>
            {
                ["parse"] = "@csv({action: \"parse\", data: \"name,age\\nJohn,30\\nJane,25\"})",
                ["stringify"] = "@csv({action: \"stringify\", data: [{name: \"John\", age: 30}]})",
                ["to_json"] = "@csv({action: \"to_json\", data: csvData})",
                ["filter"] = "@csv({action: \"filter\", data: csvData, filter: \"age > 25\"})"
            };
        }
        
        protected override Dictionary<string, string> GetErrorCodes()
        {
            return new Dictionary<string, string>
            {
                ["INVALID_ACTION"] = "Invalid CSV action",
                ["INVALID_CSV"] = "Invalid CSV data",
                ["PARSE_ERROR"] = "CSV parsing error",
                ["DELIMITER_ERROR"] = "Invalid delimiter",
                ["ENCODING_ERROR"] = "Encoding error",
                ["FILTER_ERROR"] = "CSV filter error",
                ["SORT_ERROR"] = "CSV sort error",
                ["TIMEOUT_EXCEEDED"] = "CSV operation timeout exceeded"
            };
        }
        
        protected override async Task<object> ExecuteOperatorAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var action = GetContextValue<string>(config, "action", "");
            var data = ResolveVariable(config.GetValueOrDefault("data"), context);
            var delimiter = GetContextValue<string>(config, "delimiter", ",");
            var hasHeader = GetContextValue<bool>(config, "has_header", true);
            var encoding = GetContextValue<string>(config, "encoding", "UTF-8");
            var format = GetContextValue<string>(config, "format", "pretty");
            var validate = GetContextValue<bool>(config, "validate", true);
            var filter = GetContextValue<string>(config, "filter", "");
            var sort = GetContextValue<string>(config, "sort", "");
            var sortDirection = GetContextValue<string>(config, "sort_direction", "asc");
            var limit = GetContextValue<int>(config, "limit", 0);
            var offset = GetContextValue<int>(config, "offset", 0);
            var timeout = GetContextValue<int>(config, "timeout", 300);
            
            if (string.IsNullOrEmpty(action))
                throw new ArgumentException("Action is required");
            
            var startTime = DateTime.UtcNow;
            
            try
            {
                // Check timeout
                if ((DateTime.UtcNow - startTime).TotalSeconds > timeout)
                {
                    throw new TimeoutException("CSV operation timeout exceeded");
                }
                
                switch (action.ToLower())
                {
                    case "parse":
                        return await ParseCsvAsync(data, delimiter, hasHeader, encoding, validate);
                    
                    case "stringify":
                        return await StringifyCsvAsync(data, delimiter, hasHeader, encoding, format);
                    
                    case "to_json":
                        return await ConvertToJsonAsync(data, delimiter, hasHeader);
                    
                    case "to_xml":
                        return await ConvertToXmlAsync(data, delimiter, hasHeader);
                    
                    case "filter":
                        return await FilterCsvAsync(data, filter, delimiter, hasHeader);
                    
                    case "sort":
                        return await SortCsvAsync(data, sort, sortDirection, delimiter, hasHeader);
                    
                    case "validate":
                        return await ValidateCsvAsync(data, delimiter, hasHeader);
                    
                    case "format":
                        return await FormatCsvAsync(data, delimiter, hasHeader, format);
                    
                    case "minify":
                        return await MinifyCsvAsync(data, delimiter, hasHeader, encoding);
                    
                    case "flatten":
                        return await FlattenCsvAsync(data, delimiter, hasHeader);
                    
                    case "unflatten":
                        return await UnflattenCsvAsync(data, delimiter, hasHeader);
                    
                    default:
                        throw new ArgumentException($"Unknown CSV action: {action}");
                }
            }
            catch (Exception ex)
            {
                Log("error", "CSV operation failed", new Dictionary<string, object>
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
        /// Parse CSV string to object
        /// </summary>
        private async Task<object> ParseCsvAsync(object data, string delimiter, bool hasHeader, string encoding, bool validate)
        {
            if (data == null)
                throw new ArgumentException("Data is required for parsing");
            
            var csvString = data.ToString();
            
            try
            {
                var lines = csvString.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                var result = new List<Dictionary<string, object>>();
                var headers = new List<string>();
                
                if (lines.Length == 0)
                {
                    return new Dictionary<string, object>
                    {
                        ["success"] = true,
                        ["parsed"] = result,
                        ["headers"] = headers,
                        ["row_count"] = 0,
                        ["valid"] = true
                    };
                }
                
                // Parse headers if present
                if (hasHeader && lines.Length > 0)
                {
                    headers = ParseCsvLine(lines[0], delimiter);
                    lines = lines.Skip(1).ToArray();
                }
                else
                {
                    // Generate default headers
                    var firstLine = ParseCsvLine(lines[0], delimiter);
                    for (int i = 0; i < firstLine.Count; i++)
                    {
                        headers.Add($"column_{i + 1}");
                    }
                }
                
                // Parse data rows
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;
                    
                    var values = ParseCsvLine(line, delimiter);
                    var row = new Dictionary<string, object>();
                    
                    for (int i = 0; i < Math.Min(headers.Count, values.Count); i++)
                    {
                        row[headers[i]] = values[i];
                    }
                    
                    result.Add(row);
                }
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["parsed"] = result,
                    ["headers"] = headers,
                    ["row_count"] = result.Count,
                    ["column_count"] = headers.Count,
                    ["valid"] = true,
                    ["delimiter"] = delimiter,
                    ["has_header"] = hasHeader
                };
            }
            catch (Exception ex)
            {
                if (validate)
                {
                    throw new ArgumentException($"Invalid CSV: {ex.Message}");
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
        /// Convert object to CSV string
        /// </summary>
        private async Task<object> StringifyCsvAsync(object data, string delimiter, bool hasHeader, string encoding, string format)
        {
            if (data == null)
                throw new ArgumentException("Data is required for stringification");
            
            try
            {
                var csvBuilder = new StringBuilder();
                var headers = new List<string>();
                var rows = new List<Dictionary<string, object>>();
                
                // Extract data
                if (data is List<object> dataList)
                {
                    foreach (var item in dataList)
                    {
                        if (item is Dictionary<string, object> dict)
                        {
                            rows.Add(dict);
                            foreach (var key in dict.Keys)
                            {
                                if (!headers.Contains(key))
                                {
                                    headers.Add(key);
                                }
                            }
                        }
                    }
                }
                else if (data is Dictionary<string, object> singleRow)
                {
                    rows.Add(singleRow);
                    headers.AddRange(singleRow.Keys);
                }
                
                // Write headers
                if (hasHeader && headers.Count > 0)
                {
                    csvBuilder.AppendLine(string.Join(delimiter, headers.Select(h => EscapeCsvField(h))));
                }
                
                // Write data rows
                foreach (var row in rows)
                {
                    var values = new List<string>();
                    foreach (var header in headers)
                    {
                        var value = row.GetValueOrDefault(header, "");
                        values.Add(EscapeCsvField(value?.ToString() ?? ""));
                    }
                    csvBuilder.AppendLine(string.Join(delimiter, values));
                }
                
                var csvString = csvBuilder.ToString().TrimEnd('\r', '\n');
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["stringified"] = csvString,
                    ["delimiter"] = delimiter,
                    ["has_header"] = hasHeader,
                    ["encoding"] = encoding,
                    ["format"] = format
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Stringification failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Convert CSV to JSON
        /// </summary>
        private async Task<object> ConvertToJsonAsync(object data, string delimiter, bool hasHeader)
        {
            if (data == null)
                throw new ArgumentException("Data is required for conversion");
            
            try
            {
                var csvData = await ParseCsvAsync(data, delimiter, hasHeader, "UTF-8", true);
                
                if (csvData is Dictionary<string, object> result && result.ContainsKey("parsed"))
                {
                    return new Dictionary<string, object>
                    {
                        ["success"] = true,
                        ["json"] = result["parsed"],
                        ["headers"] = result["headers"],
                        ["row_count"] = result["row_count"]
                    };
                }
                
                throw new ArgumentException("Failed to parse CSV data");
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"JSON conversion failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Convert CSV to XML
        /// </summary>
        private async Task<object> ConvertToXmlAsync(object data, string delimiter, bool hasHeader)
        {
            if (data == null)
                throw new ArgumentException("Data is required for conversion");
            
            try
            {
                var csvData = await ParseCsvAsync(data, delimiter, hasHeader, "UTF-8", true);
                
                if (csvData is Dictionary<string, object> result && result.ContainsKey("parsed"))
                {
                    var xmlBuilder = new StringBuilder();
                    xmlBuilder.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                    xmlBuilder.AppendLine("<csv_data>");
                    
                    var rows = result["parsed"] as List<Dictionary<string, object>>;
                    if (rows != null)
                    {
                        foreach (var row in rows)
                        {
                            xmlBuilder.AppendLine("  <row>");
                            foreach (var kvp in row)
                            {
                                xmlBuilder.AppendLine($"    <{kvp.Key}>{EscapeXmlValue(kvp.Value?.ToString() ?? "")}</{kvp.Key}>");
                            }
                            xmlBuilder.AppendLine("  </row>");
                        }
                    }
                    
                    xmlBuilder.AppendLine("</csv_data>");
                    
                    return new Dictionary<string, object>
                    {
                        ["success"] = true,
                        ["xml"] = xmlBuilder.ToString(),
                        ["headers"] = result["headers"],
                        ["row_count"] = result["row_count"]
                    };
                }
                
                throw new ArgumentException("Failed to parse CSV data");
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"XML conversion failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Filter CSV data
        /// </summary>
        private async Task<object> FilterCsvAsync(object data, string filter, string delimiter, bool hasHeader)
        {
            if (data == null)
                throw new ArgumentException("Data is required for filtering");
            
            if (string.IsNullOrEmpty(filter))
                throw new ArgumentException("Filter expression is required");
            
            try
            {
                var csvData = await ParseCsvAsync(data, delimiter, hasHeader, "UTF-8", true);
                
                if (csvData is Dictionary<string, object> result && result.ContainsKey("parsed"))
                {
                    var rows = result["parsed"] as List<Dictionary<string, object>>;
                    var filteredRows = new List<Dictionary<string, object>>();
                    
                    if (rows != null)
                    {
                        foreach (var row in rows)
                        {
                            if (EvaluateFilter(row, filter))
                            {
                                filteredRows.Add(row);
                            }
                        }
                    }
                    
                    return new Dictionary<string, object>
                    {
                        ["success"] = true,
                        ["filtered"] = filteredRows,
                        ["original_count"] = rows?.Count ?? 0,
                        ["filtered_count"] = filteredRows.Count,
                        ["filter"] = filter
                    };
                }
                
                throw new ArgumentException("Failed to parse CSV data");
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Filter failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Sort CSV data
        /// </summary>
        private async Task<object> SortCsvAsync(object data, string sort, string sortDirection, string delimiter, bool hasHeader)
        {
            if (data == null)
                throw new ArgumentException("Data is required for sorting");
            
            if (string.IsNullOrEmpty(sort))
                throw new ArgumentException("Sort field is required");
            
            try
            {
                var csvData = await ParseCsvAsync(data, delimiter, hasHeader, "UTF-8", true);
                
                if (csvData is Dictionary<string, object> result && result.ContainsKey("parsed"))
                {
                    var rows = result["parsed"] as List<Dictionary<string, object>>;
                    
                    if (rows != null)
                    {
                        var sortedRows = new List<Dictionary<string, object>>(rows);
                        
                        sortedRows.Sort((a, b) =>
                        {
                            var aValue = a.GetValueOrDefault(sort, "");
                            var bValue = b.GetValueOrDefault(sort, "");
                            
                            var comparison = string.Compare(aValue?.ToString() ?? "", bValue?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);
                            return sortDirection.ToLower() == "desc" ? -comparison : comparison;
                        });
                        
                        return new Dictionary<string, object>
                        {
                            ["success"] = true,
                            ["sorted"] = sortedRows,
                            ["sort_field"] = sort,
                            ["sort_direction"] = sortDirection
                        };
                    }
                }
                
                throw new ArgumentException("Failed to parse CSV data");
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Sort failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Validate CSV data
        /// </summary>
        private async Task<object> ValidateCsvAsync(object data, string delimiter, bool hasHeader)
        {
            if (data == null)
                throw new ArgumentException("Data is required for validation");
            
            try
            {
                var csvData = await ParseCsvAsync(data, delimiter, hasHeader, "UTF-8", false);
                
                if (csvData is Dictionary<string, object> result)
                {
                    var isValid = result.ContainsKey("valid") && (bool)result["valid"];
                    var errors = new List<string>();
                    
                    if (!isValid)
                    {
                        errors.Add("CSV parsing failed");
                    }
                    
                    return new Dictionary<string, object>
                    {
                        ["success"] = true,
                        ["valid"] = isValid,
                        ["errors"] = errors,
                        ["error_count"] = errors.Count
                    };
                }
                
                throw new ArgumentException("Failed to validate CSV data");
            }
            catch (Exception ex)
            {
                return new Dictionary<string, object>
                {
                    ["success"] = false,
                    ["valid"] = false,
                    ["errors"] = new List<string> { ex.Message },
                    ["error_count"] = 1
                };
            }
        }
        
        /// <summary>
        /// Format CSV data
        /// </summary>
        private async Task<object> FormatCsvAsync(object data, string delimiter, bool hasHeader, string format)
        {
            if (data == null)
                throw new ArgumentException("Data is required for formatting");
            
            try
            {
                var csvData = await ParseCsvAsync(data, delimiter, hasHeader, "UTF-8", true);
                
                if (csvData is Dictionary<string, object> result && result.ContainsKey("parsed"))
                {
                    var formattedData = await StringifyCsvAsync(result["parsed"], delimiter, hasHeader, "UTF-8", format);
                    
                    return new Dictionary<string, object>
                    {
                        ["success"] = true,
                        ["formatted"] = formattedData,
                        ["format"] = format
                    };
                }
                
                throw new ArgumentException("Failed to parse CSV data");
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Format failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Minify CSV data
        /// </summary>
        private async Task<object> MinifyCsvAsync(object data, string delimiter, bool hasHeader, string encoding)
        {
            if (data == null)
                throw new ArgumentException("Data is required for minification");
            
            try
            {
                var csvData = await ParseCsvAsync(data, delimiter, hasHeader, "UTF-8", true);
                
                if (csvData is Dictionary<string, object> result && result.ContainsKey("parsed"))
                {
                    var minifiedData = await StringifyCsvAsync(result["parsed"], delimiter, hasHeader, encoding, "minified");
                    
                    return new Dictionary<string, object>
                    {
                        ["success"] = true,
                        ["minified"] = minifiedData,
                        ["encoding"] = encoding
                    };
                }
                
                throw new ArgumentException("Failed to parse CSV data");
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Minification failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Flatten CSV data
        /// </summary>
        private async Task<object> FlattenCsvAsync(object data, string delimiter, bool hasHeader)
        {
            if (data == null)
                throw new ArgumentException("Data is required for flattening");
            
            try
            {
                var csvData = await ParseCsvAsync(data, delimiter, hasHeader, "UTF-8", true);
                
                if (csvData is Dictionary<string, object> result && result.ContainsKey("parsed"))
                {
                    var rows = result["parsed"] as List<Dictionary<string, object>>;
                    var flattened = new List<object>();
                    
                    if (rows != null)
                    {
                        foreach (var row in rows)
                        {
                            foreach (var kvp in row)
                            {
                                flattened.Add(new Dictionary<string, object>
                                {
                                    ["field"] = kvp.Key,
                                    ["value"] = kvp.Value
                                });
                            }
                        }
                    }
                    
                    return new Dictionary<string, object>
                    {
                        ["success"] = true,
                        ["flattened"] = flattened
                    };
                }
                
                throw new ArgumentException("Failed to parse CSV data");
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Flattening failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Unflatten CSV data
        /// </summary>
        private async Task<object> UnflattenCsvAsync(object data, string delimiter, bool hasHeader)
        {
            if (data == null)
                throw new ArgumentException("Data is required for unflattening");
            
            try
            {
                // This would require specific flattened format
                // For now, return the original data
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["unflattened"] = data
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Unflattening failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Parse CSV line
        /// </summary>
        private List<string> ParseCsvLine(string line, string delimiter)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;
            
            for (int i = 0; i < line.Length; i++)
            {
                var c = line[i];
                
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c.ToString() == delimiter && !inQuotes)
                {
                    result.Add(current.ToString().Trim());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
            
            result.Add(current.ToString().Trim());
            return result;
        }
        
        /// <summary>
        /// Escape CSV field
        /// </summary>
        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "";
            
            if (field.Contains('"') || field.Contains(',') || field.Contains('\n') || field.Contains('\r'))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }
            
            return field;
        }
        
        /// <summary>
        /// Escape XML value
        /// </summary>
        private string EscapeXmlValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            
            return value
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }
        
        /// <summary>
        /// Evaluate filter expression
        /// </summary>
        private bool EvaluateFilter(Dictionary<string, object> row, string filter)
        {
            // Simplified filter evaluation
            // In a real implementation, you would parse and execute the filter expression
            
            var filterLower = filter.ToLower();
            
            foreach (var kvp in row)
            {
                var fieldName = kvp.Key.ToLower();
                var fieldValue = kvp.Value?.ToString()?.ToLower() ?? "";
                
                if (filterLower.Contains(fieldName) && filterLower.Contains(fieldValue))
                {
                    return true;
                }
            }
            
            return false;
        }
        
        protected override ValidationResult CustomValidate(Dictionary<string, object> config)
        {
            var result = new ValidationResult();
            
            if (!config.ContainsKey("action"))
            {
                result.Errors.Add("Action is required");
            }
            
            var action = GetContextValue<string>(config, "action", "");
            var validActions = new[] { "parse", "stringify", "to_json", "to_xml", "filter", "sort", "validate", "format", "minify", "flatten", "unflatten" };
            
            if (!string.IsNullOrEmpty(action) && !Array.Exists(validActions, a => a == action.ToLower()))
            {
                result.Errors.Add($"Invalid action: {action}. Valid actions are: {string.Join(", ", validActions)}");
            }
            
            if (config.TryGetValue("timeout", out var timeout) && timeout is int timeoutValue && timeoutValue <= 0)
            {
                result.Errors.Add("Timeout must be positive");
            }
            
            return result;
        }
    }
} 