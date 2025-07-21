using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TuskLang.CLI.Commands
{
    /// <summary>
    /// CSS Analyzer for analyzing CSS content and providing insights
    /// </summary>
    public class CssAnalyzer
    {
        /// <summary>
        /// Analyze CSS content and return analysis results
        /// </summary>
        public async Task<CssAnalysisResult> AnalyzeAsync(string cssContent)
        {
            return await Task.Run(() =>
            {
                var result = new CssAnalysisResult();
                
                // Parse CSS rules
                var rules = ParseCssRules(cssContent);
                result.TotalRules = rules.Count;
                
                // Count selectors and properties
                foreach (var rule in rules)
                {
                    result.TotalSelectors += rule.Selectors.Count;
                    result.TotalProperties += rule.Properties.Count;
                    
                    // Check for duplicate properties
                    var duplicates = rule.Properties
                        .GroupBy(p => p.Name)
                        .Where(g => g.Count() > 1)
                        .SelectMany(g => g.Skip(1));
                    
                    result.DuplicateProperties.AddRange(duplicates);
                }
                
                // Calculate average specificity
                if (rules.Count > 0)
                {
                    var totalSpecificity = rules.Sum(r => r.Selectors.Sum(s => CalculateSpecificity(s)));
                    result.AverageSpecificity = (double)totalSpecificity / (rules.Count * rules.Average(r => r.Selectors.Count));
                }
                
                // Calculate gzipped size (rough estimation)
                result.GzippedSize = EstimateGzippedSize(cssContent);
                
                return result;
            };
        }
        
        private List<CssRule> ParseCssRules(string cssContent)
        {
            var rules = new List<CssRule>();
            var rulePattern = @"([^{}]+)\s*\{([^{}]*)\}";
            var matches = Regex.Matches(cssContent, rulePattern, RegexOptions.Singleline);
            
            foreach (Match match in matches)
            {
                var selectorsText = match.Groups[1].Value.Trim();
                var propertiesText = match.Groups[2].Value.Trim();
                
                var selectors = selectorsText.Split(',').Select(s => s.Trim()).ToList();
                var properties = ParseProperties(propertiesText);
                
                rules.Add(new CssRule
                {
                    Selectors = selectors,
                    Properties = properties
                };
            }
            
            return rules;
        }
        
        private List<CssProperty> ParseProperties(string propertiesText)
        {
            var properties = new List<CssProperty>();
            var propertyPattern = @"([^:]+):\s*([^;]+);";
            var matches = Regex.Matches(propertiesText, propertyPattern);
            
            foreach (Match match in matches)
            {
                properties.Add(new CssProperty
                {
                    Name = match.Groups[1].Value.Trim(),
                    Value = match.Groups[2].Value.Trim()
                };
            }
            
            return properties;
        }
        
        private int CalculateSpecificity(string selector)
        {
            // Simple specificity calculation
            var specificity = 0;
            
            // ID selectors
            specificity += Regex.Matches(selector, @"#\w+").Count * 100;
            
            // Class selectors, attribute selectors, pseudo-classes
            specificity += Regex.Matches(selector, @"\.\w+").Count * 10;
            specificity += Regex.Matches(selector, @"\[.*?\]").Count * 10;
            specificity += Regex.Matches(selector, @":\w+").Count * 10;
            
            // Element selectors, pseudo-elements
            specificity += Regex.Matches(selector, @"^[a-zA-Z]").Count;
            specificity += Regex.Matches(selector, @"::\w+").Count;
            
            return specificity;
        }
        
        private int EstimateGzippedSize(string content)
        {
            // Rough estimation: CSS typically compresses to 20-30% of original size
            return (int)(content.Length * 0.25);
        }
    }
    
    /// <summary>
    /// CSS Analysis Result
    /// </summary>
    public class CssAnalysisResult
    {
        public int TotalRules { get; set; }
        public int TotalSelectors { get; set; }
        public int TotalProperties { get; set; }
        public int GzippedSize { get; set; }
        public double AverageSpecificity { get; set; }
        public List<CssProperty> UnusedRules { get; set; } = new List<CssProperty>();
        public List<CssProperty> DuplicateProperties { get; set; } = new List<CssProperty>();
    }
    
    /// <summary>
    /// CSS Rule representation
    /// </summary>
    public class CssRule
    {
        public List<string> Selectors { get; set; } = new List<string>();
        public List<CssProperty> Properties { get; set; } = new List<CssProperty>();
    }
    
    /// <summary>
    /// CSS Property representation
    /// </summary>
    public class CssProperty
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
} 