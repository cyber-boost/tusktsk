using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TuskLang
{
    /// <summary>
    /// Parser for TSK (TuskLang Configuration) format
    /// Handles TOML-like syntax with sections, key-value pairs, and multiline strings
    /// </summary>
    public class TSKParser
    {
        /// <summary>
        /// Parse TSK content into dictionary
        /// </summary>
        public static Dictionary<string, object> Parse(string content)
        {
            var (data, _) = ParseWithComments(content);
            return data;
        }

        /// <summary>
        /// Parse TSK content with comments preserved
        /// </summary>
        public static (Dictionary<string, object> data, Dictionary<int, string> comments) ParseWithComments(string content)
        {
            var lines = content.Split('\n');
            var result = new Dictionary<string, object>();
            var comments = new Dictionary<int, string>();
            string currentSection = null;
            bool inMultilineString = false;
            string multilineKey = null;
            var multilineContent = new List<string>();

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var trimmedLine = line.Trim();

                // Handle multiline strings
                if (inMultilineString)
                {
                    if (trimmedLine == "\"\"\"")
                    {
                        if (currentSection != null && multilineKey != null)
                        {
                            if (!result.ContainsKey(currentSection))
                                result[currentSection] = new Dictionary<string, object>();
                            
                            if (result[currentSection] is Dictionary<string, object> sectionData)
                                sectionData[multilineKey] = string.Join("\n", multilineContent);
                        }
                        inMultilineString = false;
                        multilineKey = null;
                        multilineContent.Clear();
                        continue;
                    }
                    multilineContent.Add(line);
                    continue;
                }

                // Capture comments
                if (trimmedLine.StartsWith("#"))
                {
                    comments[i] = trimmedLine;
                    continue;
                }

                // Skip empty lines
                if (string.IsNullOrEmpty(trimmedLine))
                    continue;

                // Section header
                var sectionMatch = Regex.Match(trimmedLine, @"^\[(.+)\]$");
                if (sectionMatch.Success)
                {
                    currentSection = sectionMatch.Groups[1].Value;
                    result[currentSection] = new Dictionary<string, object>();
                    continue;
                }

                // Key-value pair
                if (currentSection != null && trimmedLine.Contains("="))
                {
                    var separatorIndex = trimmedLine.IndexOf('=');
                    var key = trimmedLine.Substring(0, separatorIndex).Trim();
                    var valueStr = trimmedLine.Substring(separatorIndex + 1).Trim();

                    // Check for multiline string start
                    if (valueStr == "\"\"\"")
                    {
                        inMultilineString = true;
                        multilineKey = key;
                        continue;
                    }

                    var value = ParseValue(valueStr);
                    if (result[currentSection] is Dictionary<string, object> sectionData)
                        sectionData[key] = value;
                }
            }

            return (result, comments);
        }

        /// <summary>
        /// Parse a TSK value string into appropriate C# type
        /// </summary>
        private static object ParseValue(string valueStr)
        {
            // Null
            if (valueStr == "null") return null;

            // Boolean
            if (valueStr == "true") return true;
            if (valueStr == "false") return false;

            // Number
            if (int.TryParse(valueStr, out int intValue)) return intValue;
            if (double.TryParse(valueStr, out double doubleValue)) return doubleValue;

            // String
            if (valueStr.StartsWith("\"") && valueStr.EndsWith("\""))
            {
                return valueStr.Substring(1, valueStr.Length - 2)
                    .Replace("\\\"", "\"")
                    .Replace("\\\\", "\\");
            }

            // Array
            if (valueStr.StartsWith("[") && valueStr.EndsWith("]"))
            {
                var arrayContent = valueStr.Substring(1, valueStr.Length - 2).Trim();
                if (string.IsNullOrEmpty(arrayContent)) return new List<object>();

                var items = SplitArrayItems(arrayContent);
                return items.Select(item => ParseValue(item.Trim())).ToList();
            }

            // Object/Dictionary
            if (valueStr.StartsWith("{") && valueStr.EndsWith("}"))
            {
                var objContent = valueStr.Substring(1, valueStr.Length - 2).Trim();
                if (string.IsNullOrEmpty(objContent)) return new Dictionary<string, object>();

                var pairs = SplitObjectPairs(objContent);
                var obj = new Dictionary<string, object>();

                foreach (var pair in pairs)
                {
                    var eqIndex = pair.IndexOf('=');
                    if (eqIndex > -1)
                    {
                        var key = pair.Substring(0, eqIndex).Trim();
                        var value = pair.Substring(eqIndex + 1).Trim();
                        // Remove quotes from key if present
                        var cleanKey = key.StartsWith("\"") && key.EndsWith("\"") 
                            ? key.Substring(1, key.Length - 2) 
                            : key;
                        obj[cleanKey] = ParseValue(value);
                    }
                }

                return obj;
            }

            // Return as string if no other type matches
            return valueStr;
        }

        /// <summary>
        /// Split array items considering nested structures
        /// </summary>
        private static List<string> SplitArrayItems(string content)
        {
            var items = new List<string>();
            var current = "";
            var depth = 0;
            var inString = false;

            for (int i = 0; i < content.Length; i++)
            {
                var ch = content[i];

                if (ch == '"' && (i == 0 || content[i - 1] != '\\'))
                {
                    inString = !inString;
                }

                if (!inString)
                {
                    if (ch == '[' || ch == '{') depth++;
                    if (ch == ']' || ch == '}') depth--;

                    if (ch == ',' && depth == 0)
                    {
                        items.Add(current.Trim());
                        current = "";
                        continue;
                    }
                }

                current += ch;
            }

            if (!string.IsNullOrEmpty(current.Trim()))
            {
                items.Add(current.Trim());
            }

            return items;
        }

        /// <summary>
        /// Split object pairs considering nested structures
        /// </summary>
        private static List<string> SplitObjectPairs(string content)
        {
            var pairs = new List<string>();
            var current = "";
            var depth = 0;
            var inString = false;

            for (int i = 0; i < content.Length; i++)
            {
                var ch = content[i];

                if (ch == '"' && (i == 0 || content[i - 1] != '\\'))
                {
                    inString = !inString;
                }

                if (!inString)
                {
                    if (ch == '[' || ch == '{') depth++;
                    if (ch == ']' || ch == '}') depth--;

                    if (ch == ',' && depth == 0)
                    {
                        pairs.Add(current.Trim());
                        current = "";
                        continue;
                    }
                }

                current += ch;
            }

            if (!string.IsNullOrEmpty(current.Trim()))
            {
                pairs.Add(current.Trim());
            }

            return pairs;
        }

        /// <summary>
        /// Convert dictionary back to TSK string format
        /// </summary>
        public static string Stringify(Dictionary<string, object> data)
        {
            var sb = new StringBuilder();

            foreach (var section in data)
            {
                sb.AppendLine($"[{section.Key}]");

                if (section.Value is Dictionary<string, object> sectionData)
                {
                    foreach (var kvp in sectionData)
                    {
                        sb.AppendLine($"{kvp.Key} = {FormatValue(kvp.Value)}");
                    }
                }
                else
                {
                    sb.AppendLine($"value = {FormatValue(section.Value)}");
                }

                sb.AppendLine();
            }

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Format a value for TSK string representation
        /// </summary>
        private static string FormatValue(object value)
        {
            if (value == null) return "null";
            if (value is bool b) return b.ToString().ToLower();
            if (value is string str) return $"\"{str.Replace("\"", "\\\"").Replace("\\", "\\\\")}\"";
            if (value is int || value is long || value is double || value is float) return value.ToString();

            if (value is Dictionary<string, object> dict)
            {
                var pairs = dict.Select(kvp => $"\"{kvp.Key}\" = {FormatValue(kvp.Value)}");
                return $"{{{string.Join(", ", pairs)}}}";
            }

            if (value is IEnumerable<object> enumerable)
            {
                var items = enumerable.Select(FormatValue);
                return $"[{string.Join(", ", items)}]";
            }

            if (value is IEnumerable<KeyValuePair<string, object>> kvpEnumerable)
            {
                var pairs = kvpEnumerable.Select(kvp => $"\"{kvp.Key}\" = {FormatValue(kvp.Value)}");
                return $"{{{string.Join(", ", pairs)}}}";
            }

            return $"\"{value}\"";
        }
    }
} 