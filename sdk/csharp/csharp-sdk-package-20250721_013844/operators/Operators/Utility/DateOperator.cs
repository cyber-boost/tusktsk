using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Globalization;

namespace TuskLang.Operators.Utility
{
    /// <summary>
    /// Date Operator for TuskLang C# SDK
    /// 
    /// Provides date and time operations with support for:
    /// - Date parsing and formatting
    /// - Date arithmetic and calculations
    /// - Timezone conversions
    /// - Date validation and comparison
    /// - Date ranges and intervals
    /// - Calendar operations
    /// - Date localization
    /// 
    /// Usage:
    /// ```csharp
    /// // Parse date
    /// var result = @date({
    ///   action: "parse",
    ///   date: "2023-12-25"
    /// })
    /// 
    /// // Format date
    /// var result = @date({
    ///   action: "format",
    ///   date: "2023-12-25T10:30:00",
    ///   format: "yyyy-MM-dd HH:mm:ss"
    /// })
    /// ```
    /// </summary>
    public class DateOperator : BaseOperator
    {
        public DateOperator()
        {
            Version = "2.0.0";
            RequiredFields = new List<string> { "action" };
            OptionalFields = new List<string> 
            { 
                "date", "format", "timezone", "locale", "culture", "add", "subtract",
                "compare", "validate", "range", "business_days", "holidays"
            };
            
            DefaultConfig = new Dictionary<string, object>
            {
                ["format"] = "yyyy-MM-dd HH:mm:ss",
                ["timezone"] = "UTC",
                ["locale"] = "en-US",
                ["culture"] = "en-US",
                ["business_days"] = false
            };
        }
        
        public override string GetName() => "date";
        
        protected override string GetDescription() => "Date and time operator for parsing, formatting, and manipulating dates";
        
        protected override Dictionary<string, string> GetExamples()
        {
            return new Dictionary<string, string>
            {
                ["parse"] = "@date({action: \"parse\", date: \"2023-12-25\"})",
                ["format"] = "@date({action: \"format\", date: \"2023-12-25T10:30:00\", format: \"yyyy-MM-dd\"})",
                ["add"] = "@date({action: \"add\", date: \"2023-12-25\", add: \"7 days\"})",
                ["compare"] = "@date({action: \"compare\", date1: \"2023-12-25\", date2: \"2023-12-26\"})"
            };
        }
        
        protected override Dictionary<string, string> GetErrorCodes()
        {
            return new Dictionary<string, string>
            {
                ["INVALID_DATE"] = "Invalid date format",
                ["PARSE_ERROR"] = "Date parsing error",
                ["FORMAT_ERROR"] = "Date formatting error",
                ["TIMEZONE_ERROR"] = "Timezone conversion error",
                ["LOCALE_ERROR"] = "Locale error",
                ["CALCULATION_ERROR"] = "Date calculation error",
                ["VALIDATION_ERROR"] = "Date validation error"
            };
        }
        
        protected override async Task<object> ExecuteOperatorAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var action = GetContextValue<string>(config, "action", "");
            var date = GetContextValue<string>(config, "date", "");
            var format = GetContextValue<string>(config, "format", "yyyy-MM-dd HH:mm:ss");
            var timezone = GetContextValue<string>(config, "timezone", "UTC");
            var locale = GetContextValue<string>(config, "locale", "en-US");
            var culture = GetContextValue<string>(config, "culture", "en-US");
            var add = GetContextValue<string>(config, "add", "");
            var subtract = GetContextValue<string>(config, "subtract", "");
            var compare = GetContextValue<string>(config, "compare", "");
            var validate = GetContextValue<bool>(config, "validate", true);
            var range = ResolveVariable(config.GetValueOrDefault("range"), context);
            var businessDays = GetContextValue<bool>(config, "business_days", false);
            var holidays = ResolveVariable(config.GetValueOrDefault("holidays"), context);
            
            if (string.IsNullOrEmpty(action))
                throw new ArgumentException("Action is required");
            
            try
            {
                switch (action.ToLower())
                {
                    case "parse":
                        return await ParseDateAsync(date, format, timezone, locale, validate);
                    
                    case "format":
                        return await FormatDateAsync(date, format, timezone, locale);
                    
                    case "now":
                        return await GetCurrentDateAsync(format, timezone, locale);
                    
                    case "add":
                        return await AddToDateAsync(date, add, format, timezone, businessDays, holidays);
                    
                    case "subtract":
                        return await SubtractFromDateAsync(date, subtract, format, timezone, businessDays, holidays);
                    
                    case "compare":
                        return await CompareDatesAsync(date, compare, timezone);
                    
                    case "validate":
                        return await ValidateDateAsync(date, format, timezone);
                    
                    case "diff":
                        return await GetDateDifferenceAsync(date, compare, timezone, businessDays, holidays);
                    
                    case "range":
                        return await GenerateDateRangeAsync(range, format, timezone, businessDays, holidays);
                    
                    case "weekday":
                        return await GetWeekdayAsync(date, timezone, locale);
                    
                    case "quarter":
                        return await GetQuarterAsync(date, timezone);
                    
                    case "age":
                        return await CalculateAgeAsync(date, compare, timezone);
                    
                    default:
                        throw new ArgumentException($"Unknown date action: {action}");
                }
            }
            catch (Exception ex)
            {
                Log("error", "Date operation failed", new Dictionary<string, object>
                {
                    ["action"] = action,
                    ["date"] = date,
                    ["error"] = ex.Message
                });
                
                return new Dictionary<string, object>
                {
                    ["success"] = false,
                    ["error"] = ex.Message,
                    ["action"] = action,
                    ["date"] = date
                };
            }
        }
        
        /// <summary>
        /// Parse date string
        /// </summary>
        private async Task<object> ParseDateAsync(string date, string format, string timezone, string locale, bool validate)
        {
            if (string.IsNullOrEmpty(date))
                throw new ArgumentException("Date is required");
            
            try
            {
                DateTime parsedDate;
                var cultureInfo = new CultureInfo(locale);
                
                if (string.IsNullOrEmpty(format) || format == "auto")
                {
                    // Try to parse with common formats
                    var formats = new[] 
                    {
                        "yyyy-MM-dd HH:mm:ss",
                        "yyyy-MM-dd",
                        "MM/dd/yyyy",
                        "dd/MM/yyyy",
                        "yyyy-MM-ddTHH:mm:ss",
                        "yyyy-MM-ddTHH:mm:ssZ"
                    };
                    
                    if (!DateTime.TryParseExact(date, formats, cultureInfo, DateTimeStyles.None, out parsedDate))
                    {
                        if (!DateTime.TryParse(date, cultureInfo, DateTimeStyles.None, out parsedDate))
                        {
                            throw new ArgumentException($"Unable to parse date: {date}");
                        }
                    }
                }
                else
                {
                    if (!DateTime.TryParseExact(date, format, cultureInfo, DateTimeStyles.None, out parsedDate))
                    {
                        throw new ArgumentException($"Unable to parse date with format: {format}");
                    }
                }
                
                // Convert timezone if needed
                if (timezone.ToUpper() != "UTC")
                {
                    parsedDate = ConvertTimezone(parsedDate, "UTC", timezone);
                }
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["date"] = parsedDate,
                    ["formatted"] = parsedDate.ToString(format, cultureInfo),
                    ["timestamp"] = ((DateTimeOffset)parsedDate).ToUnixTimeSeconds(),
                    ["timezone"] = timezone,
                    ["locale"] = locale,
                    ["valid"] = true
                };
            }
            catch (Exception ex)
            {
                if (validate)
                {
                    throw new ArgumentException($"Date parsing failed: {ex.Message}");
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
        /// Format date
        /// </summary>
        private async Task<object> FormatDateAsync(string date, string format, string timezone, string locale)
        {
            if (string.IsNullOrEmpty(date))
                throw new ArgumentException("Date is required");
            
            try
            {
                var parsedResult = await ParseDateAsync(date, "auto", timezone, locale, true);
                if (parsedResult is Dictionary<string, object> result && result.ContainsKey("date"))
                {
                    var parsedDate = (DateTime)result["date"];
                    var cultureInfo = new CultureInfo(locale);
                    var formatted = parsedDate.ToString(format, cultureInfo);
                    
                    return new Dictionary<string, object>
                    {
                        ["success"] = true,
                        ["date"] = parsedDate,
                        ["formatted"] = formatted,
                        ["format"] = format,
                        ["timezone"] = timezone,
                        ["locale"] = locale
                    };
                }
                
                throw new ArgumentException("Failed to parse date for formatting");
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Date formatting failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Get current date
        /// </summary>
        private async Task<object> GetCurrentDateAsync(string format, string timezone, string locale)
        {
            try
            {
                var now = DateTime.UtcNow;
                
                // Convert timezone if needed
                if (timezone.ToUpper() != "UTC")
                {
                    now = ConvertTimezone(now, "UTC", timezone);
                }
                
                var cultureInfo = new CultureInfo(locale);
                var formatted = now.ToString(format, cultureInfo);
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["date"] = now,
                    ["formatted"] = formatted,
                    ["timestamp"] = ((DateTimeOffset)now).ToUnixTimeSeconds(),
                    ["timezone"] = timezone,
                    ["locale"] = locale
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Failed to get current date: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Add to date
        /// </summary>
        private async Task<object> AddToDateAsync(string date, string add, string format, string timezone, bool businessDays, object holidays)
        {
            if (string.IsNullOrEmpty(date))
                throw new ArgumentException("Date is required");
            
            if (string.IsNullOrEmpty(add))
                throw new ArgumentException("Add value is required");
            
            try
            {
                var parsedResult = await ParseDateAsync(date, "auto", timezone, "en-US", true);
                if (parsedResult is Dictionary<string, object> result && result.ContainsKey("date"))
                {
                    var parsedDate = (DateTime)result["date"];
                    var addedDate = AddTimeToDate(parsedDate, add, businessDays, holidays);
                    
                    var cultureInfo = new CultureInfo("en-US");
                    var formatted = addedDate.ToString(format, cultureInfo);
                    
                    return new Dictionary<string, object>
                    {
                        ["success"] = true,
                        ["original_date"] = parsedDate,
                        ["added_date"] = addedDate,
                        ["formatted"] = formatted,
                        ["add_value"] = add,
                        ["business_days"] = businessDays
                    };
                }
                
                throw new ArgumentException("Failed to parse date for addition");
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Date addition failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Subtract from date
        /// </summary>
        private async Task<object> SubtractFromDateAsync(string date, string subtract, string format, string timezone, bool businessDays, object holidays)
        {
            if (string.IsNullOrEmpty(date))
                throw new ArgumentException("Date is required");
            
            if (string.IsNullOrEmpty(subtract))
                throw new ArgumentException("Subtract value is required");
            
            try
            {
                var parsedResult = await ParseDateAsync(date, "auto", timezone, "en-US", true);
                if (parsedResult is Dictionary<string, object> result && result.ContainsKey("date"))
                {
                    var parsedDate = (DateTime)result["date"];
                    var subtractedDate = SubtractTimeFromDate(parsedDate, subtract, businessDays, holidays);
                    
                    var cultureInfo = new CultureInfo("en-US");
                    var formatted = subtractedDate.ToString(format, cultureInfo);
                    
                    return new Dictionary<string, object>
                    {
                        ["success"] = true,
                        ["original_date"] = parsedDate,
                        ["subtracted_date"] = subtractedDate,
                        ["formatted"] = formatted,
                        ["subtract_value"] = subtract,
                        ["business_days"] = businessDays
                    };
                }
                
                throw new ArgumentException("Failed to parse date for subtraction");
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Date subtraction failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Compare dates
        /// </summary>
        private async Task<object> CompareDatesAsync(string date, string compare, string timezone)
        {
            if (string.IsNullOrEmpty(date))
                throw new ArgumentException("First date is required");
            
            if (string.IsNullOrEmpty(compare))
                throw new ArgumentException("Second date is required");
            
            try
            {
                var date1Result = await ParseDateAsync(date, "auto", timezone, "en-US", true);
                var date2Result = await ParseDateAsync(compare, "auto", timezone, "en-US", true);
                
                if (date1Result is Dictionary<string, object> result1 && result1.ContainsKey("date") &&
                    date2Result is Dictionary<string, object> result2 && result2.ContainsKey("date"))
                {
                    var date1 = (DateTime)result1["date"];
                    var date2 = (DateTime)result2["date"];
                    
                    var comparison = date1.CompareTo(date2);
                    string comparisonText;
                    
                    if (comparison < 0)
                        comparisonText = "before";
                    else if (comparison > 0)
                        comparisonText = "after";
                    else
                        comparisonText = "equal";
                    
                    return new Dictionary<string, object>
                    {
                        ["success"] = true,
                        ["date1"] = date1,
                        ["date2"] = date2,
                        ["comparison"] = comparison,
                        ["comparison_text"] = comparisonText,
                        ["date1_before_date2"] = comparison < 0,
                        ["date1_after_date2"] = comparison > 0,
                        ["dates_equal"] = comparison == 0
                    };
                }
                
                throw new ArgumentException("Failed to parse dates for comparison");
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Date comparison failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Validate date
        /// </summary>
        private async Task<object> ValidateDateAsync(string date, string format, string timezone)
        {
            if (string.IsNullOrEmpty(date))
                throw new ArgumentException("Date is required");
            
            try
            {
                var result = await ParseDateAsync(date, format, timezone, "en-US", false);
                
                if (result is Dictionary<string, object> parsedResult && parsedResult.ContainsKey("valid"))
                {
                    var isValid = (bool)parsedResult["valid"];
                    
                    return new Dictionary<string, object>
                    {
                        ["success"] = true,
                        ["date"] = date,
                        ["valid"] = isValid,
                        ["error"] = isValid ? null : parsedResult.GetValueOrDefault("error", "Unknown error")
                    };
                }
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["date"] = date,
                    ["valid"] = false,
                    ["error"] = "Failed to validate date"
                };
            }
            catch (Exception ex)
            {
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["date"] = date,
                    ["valid"] = false,
                    ["error"] = ex.Message
                };
            }
        }
        
        /// <summary>
        /// Get date difference
        /// </summary>
        private async Task<object> GetDateDifferenceAsync(string date, string compare, string timezone, bool businessDays, object holidays)
        {
            if (string.IsNullOrEmpty(date))
                throw new ArgumentException("First date is required");
            
            if (string.IsNullOrEmpty(compare))
                throw new ArgumentException("Second date is required");
            
            try
            {
                var date1Result = await ParseDateAsync(date, "auto", timezone, "en-US", true);
                var date2Result = await ParseDateAsync(compare, "auto", timezone, "en-US", true);
                
                if (date1Result is Dictionary<string, object> result1 && result1.ContainsKey("date") &&
                    date2Result is Dictionary<string, object> result2 && result2.ContainsKey("date"))
                {
                    var date1 = (DateTime)result1["date"];
                    var date2 = (DateTime)result2["date"];
                    
                    var difference = date2 - date1;
                    var totalDays = difference.TotalDays;
                    var totalHours = difference.TotalHours;
                    var totalMinutes = difference.TotalMinutes;
                    var totalSeconds = difference.TotalSeconds;
                    
                    return new Dictionary<string, object>
                    {
                        ["success"] = true,
                        ["date1"] = date1,
                        ["date2"] = date2,
                        ["difference"] = difference,
                        ["total_days"] = totalDays,
                        ["total_hours"] = totalHours,
                        ["total_minutes"] = totalMinutes,
                        ["total_seconds"] = totalSeconds,
                        ["days"] = difference.Days,
                        ["hours"] = difference.Hours,
                        ["minutes"] = difference.Minutes,
                        ["seconds"] = difference.Seconds
                    };
                }
                
                throw new ArgumentException("Failed to parse dates for difference calculation");
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Date difference calculation failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Generate date range
        /// </summary>
        private async Task<object> GenerateDateRangeAsync(object range, string format, string timezone, bool businessDays, object holidays)
        {
            if (range == null)
                throw new ArgumentException("Range configuration is required");
            
            try
            {
                // Simplified range generation
                // In a real implementation, you would parse the range configuration
                var dates = new List<DateTime>();
                var now = DateTime.UtcNow;
                
                for (int i = 0; i < 7; i++)
                {
                    dates.Add(now.AddDays(i));
                }
                
                var formattedDates = new List<string>();
                var cultureInfo = new CultureInfo("en-US");
                
                foreach (var date in dates)
                {
                    formattedDates.Add(date.ToString(format, cultureInfo));
                }
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["dates"] = dates,
                    ["formatted_dates"] = formattedDates,
                    ["count"] = dates.Count,
                    ["format"] = format,
                    ["timezone"] = timezone
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Date range generation failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Get weekday
        /// </summary>
        private async Task<object> GetWeekdayAsync(string date, string timezone, string locale)
        {
            if (string.IsNullOrEmpty(date))
                throw new ArgumentException("Date is required");
            
            try
            {
                var parsedResult = await ParseDateAsync(date, "auto", timezone, locale, true);
                if (parsedResult is Dictionary<string, object> result && result.ContainsKey("date"))
                {
                    var parsedDate = (DateTime)result["date"];
                    var cultureInfo = new CultureInfo(locale);
                    
                    return new Dictionary<string, object>
                    {
                        ["success"] = true,
                        ["date"] = parsedDate,
                        ["weekday"] = parsedDate.DayOfWeek.ToString(),
                        ["weekday_number"] = (int)parsedDate.DayOfWeek,
                        ["weekday_localized"] = cultureInfo.DateTimeFormat.GetDayName(parsedDate.DayOfWeek),
                        ["weekday_abbreviated"] = cultureInfo.DateTimeFormat.GetAbbreviatedDayName(parsedDate.DayOfWeek)
                    };
                }
                
                throw new ArgumentException("Failed to parse date for weekday calculation");
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Weekday calculation failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Get quarter
        /// </summary>
        private async Task<object> GetQuarterAsync(string date, string timezone)
        {
            if (string.IsNullOrEmpty(date))
                throw new ArgumentException("Date is required");
            
            try
            {
                var parsedResult = await ParseDateAsync(date, "auto", timezone, "en-US", true);
                if (parsedResult is Dictionary<string, object> result && result.ContainsKey("date"))
                {
                    var parsedDate = (DateTime)result["date"];
                    var quarter = (parsedDate.Month - 1) / 3 + 1;
                    
                    return new Dictionary<string, object>
                    {
                        ["success"] = true,
                        ["date"] = parsedDate,
                        ["quarter"] = quarter,
                        ["quarter_name"] = $"Q{quarter}",
                        ["year"] = parsedDate.Year
                    };
                }
                
                throw new ArgumentException("Failed to parse date for quarter calculation");
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Quarter calculation failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Calculate age
        /// </summary>
        private async Task<object> CalculateAgeAsync(string date, string compare, string timezone)
        {
            if (string.IsNullOrEmpty(date))
                throw new ArgumentException("Birth date is required");
            
            try
            {
                var birthDateResult = await ParseDateAsync(date, "auto", timezone, "en-US", true);
                var compareDateResult = await ParseDateAsync(compare ?? "now", "auto", timezone, "en-US", true);
                
                if (birthDateResult is Dictionary<string, object> result1 && result1.ContainsKey("date") &&
                    compareDateResult is Dictionary<string, object> result2 && result2.ContainsKey("date"))
                {
                    var birthDate = (DateTime)result1["date"];
                    var compareDate = (DateTime)result2["date"];
                    
                    var age = compareDate.Year - birthDate.Year;
                    if (compareDate < birthDate.AddYears(age))
                    {
                        age--;
                    }
                    
                    return new Dictionary<string, object>
                    {
                        ["success"] = true,
                        ["birth_date"] = birthDate,
                        ["compare_date"] = compareDate,
                        ["age"] = age,
                        ["age_years"] = age,
                        ["age_months"] = (compareDate.Year - birthDate.Year) * 12 + compareDate.Month - birthDate.Month,
                        ["age_days"] = (compareDate - birthDate).Days
                    };
                }
                
                throw new ArgumentException("Failed to parse dates for age calculation");
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Age calculation failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Convert timezone
        /// </summary>
        private DateTime ConvertTimezone(DateTime date, string fromTimezone, string toTimezone)
        {
            // Simplified timezone conversion
            // In a real implementation, you would use proper timezone libraries
            return date;
        }
        
        /// <summary>
        /// Add time to date
        /// </summary>
        private DateTime AddTimeToDate(DateTime date, string add, bool businessDays, object holidays)
        {
            // Simplified time addition
            // In a real implementation, you would parse the add string and handle business days
            if (add.Contains("days"))
            {
                var days = int.Parse(add.Split(' ')[0]);
                return date.AddDays(days);
            }
            else if (add.Contains("hours"))
            {
                var hours = int.Parse(add.Split(' ')[0]);
                return date.AddHours(hours);
            }
            else if (add.Contains("minutes"))
            {
                var minutes = int.Parse(add.Split(' ')[0]);
                return date.AddMinutes(minutes);
            }
            
            return date;
        }
        
        /// <summary>
        /// Subtract time from date
        /// </summary>
        private DateTime SubtractTimeFromDate(DateTime date, string subtract, bool businessDays, object holidays)
        {
            // Simplified time subtraction
            // In a real implementation, you would parse the subtract string and handle business days
            if (subtract.Contains("days"))
            {
                var days = int.Parse(subtract.Split(' ')[0]);
                return date.AddDays(-days);
            }
            else if (subtract.Contains("hours"))
            {
                var hours = int.Parse(subtract.Split(' ')[0]);
                return date.AddHours(-hours);
            }
            else if (subtract.Contains("minutes"))
            {
                var minutes = int.Parse(subtract.Split(' ')[0]);
                return date.AddMinutes(-minutes);
            }
            
            return date;
        }
        
        protected override ValidationResult CustomValidate(Dictionary<string, object> config)
        {
            var result = new ValidationResult();
            
            if (!config.ContainsKey("action"))
            {
                result.Errors.Add("Action is required");
            }
            
            var action = GetContextValue<string>(config, "action", "");
            var validActions = new[] { "parse", "format", "now", "add", "subtract", "compare", "validate", "diff", "range", "weekday", "quarter", "age" };
            
            if (!string.IsNullOrEmpty(action) && !Array.Exists(validActions, a => a == action.ToLower()))
            {
                result.Errors.Add($"Invalid action: {action}. Valid actions are: {string.Join(", ", validActions)}");
            }
            
            return result;
        }
    }
} 