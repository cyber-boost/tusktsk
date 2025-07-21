using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TuskLang.CLI.Commands
{
    /// <summary>
    /// CSS Processor for processing and optimizing CSS content
    /// </summary>
    public class CssProcessor
    {
        /// <summary>
        /// Process CSS content with the specified options
        /// </summary>
        public async Task<CssProcessingResult> ProcessAsync(string cssContent, CssProcessingOptions options)
        {
            return await Task.Run(() =>
            {
                var result = new CssProcessingResult();
                
                try
                {
                    var processedCss = cssContent;
                    
                    // Apply minification if requested
                    if (options.Minify)
                    {
                        processedCss = MinifyCss(processedCss);
                    }
                    
                    // Apply autoprefixer if requested
                    if (options.Autoprefix)
                    {
                        processedCss = AddVendorPrefixes(processedCss);
                    }
                    
                    // Apply source maps if requested
                    if (options.GenerateSourceMap)
                    {
                        result.SourceMap = GenerateSourceMap(cssContent, processedCss);
                    }
                    
                    result.IsSuccess = true;
                    result.ProcessedCss = processedCss;
                    result.OriginalSize = cssContent.Length;
                    result.ProcessedSize = processedCss.Length;
                    result.CompressionRatio = CalculateCompressionRatio(cssContent.Length, processedCss.Length);
                }
                catch (Exception ex)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = ex.Message;
                }
                
                return result;
            };
        }
        
        private string MinifyCss(string css)
        {
            // Remove comments
            css = Regex.Replace(css, @"/\*.*?\*/", "", RegexOptions.Singleline);
            
            // Remove unnecessary whitespace
            css = Regex.Replace(css, @"\s+", " ");
            css = Regex.Replace(css, @"\s*{\s*", "{");
            css = Regex.Replace(css, @"\s*}\s*", "}");
            css = Regex.Replace(css, @"\s*:\s*", ":");
            css = Regex.Replace(css, @"\s*;\s*", ";");
            css = Regex.Replace(css, @"\s*,\s*", ",");
            
            // Remove trailing semicolons before closing braces
            css = Regex.Replace(css, @";\s*}", "}");
            
            // Remove empty rules
            css = Regex.Replace(css, @"[^{}]+{\s*}", "");
            
            return css.Trim();
        }
        
        private string AddVendorPrefixes(string css)
        {
            // Add vendor prefixes for common properties
            var prefixMap = new Dictionary<string, string[]>
            {
                ["transform"] = new[] { "-webkit-transform", "-moz-transform", "-ms-transform" },
                ["transition"] = new[] { "-webkit-transition", "-moz-transition", "-o-transition" },
                ["animation"] = new[] { "-webkit-animation", "-moz-animation", "-o-animation" },
                ["border-radius"] = new[] { "-webkit-border-radius", "-moz-border-radius" },
                ["box-shadow"] = new[] { "-webkit-box-shadow", "-moz-box-shadow" },
                ["flex"] = new[] { "-webkit-flex", "-moz-flex", "-ms-flex" },
                ["flex-direction"] = new[] { "-webkit-flex-direction", "-moz-flex-direction", "-ms-flex-direction" },
                ["justify-content"] = new[] { "-webkit-justify-content", "-moz-justify-content", "-ms-justify-content" },
                ["align-items"] = new[] { "-webkit-align-items", "-moz-align-items", "-ms-align-items" }
            };
            
            foreach (var kvp in prefixMap)
            {
                var property = kvp.Key;
                var prefixes = kvp.Value;
                
                var pattern = $@"({property})\s*:\s*([^;]+);";
                var matches = Regex.Matches(css, pattern, RegexOptions.IgnoreCase);
                
                foreach (Match match in matches)
                {
                    var value = match.Groups[2].Value;
                    var replacement = string.Join(";\n  ", prefixes.Select(p => $"{p}: {value}")) + ";\n  " + match.Groups[0].Value;
                    css = css.Replace(match.Groups[0].Value, replacement);
                }
            }
            
            return css;
        }
        
        private string GenerateSourceMap(string originalCss, string processedCss)
        {
            // Simple source map generation
            var sourceMap = new
            {
                version = 3,
                sources = new[] { "input.css" },
                names = new string[0],
                mappings = "AAAA;",
                file = "output.css",
                sourceRoot = ""
            };
            
            return System.Text.Json.JsonSerializer.Serialize(sourceMap);
        }
        
        private double CalculateCompressionRatio(int originalSize, int processedSize)
        {
            if (originalSize == 0) return 0;
            return ((double)(originalSize - processedSize) / originalSize) * 100;
        }
    }
    
    /// <summary>
    /// CSS Processing Options
    /// </summary>
    public class CssProcessingOptions
    {
        public bool Minify { get; set; } = false;
        public bool Autoprefix { get; set; } = false;
        public bool GenerateSourceMap { get; set; } = false;
        public string[] TargetBrowsers { get; set; } = new string[0];
        public bool RemoveUnused { get; set; } = false;
        public bool OptimizeSelectors { get; set; } = false;
    }
    
    /// <summary>
    /// CSS Processing Result
    /// </summary>
    public class CssProcessingResult
    {
        public bool IsSuccess { get; set; }
        public string ProcessedCss { get; set; } = string.Empty;
        public string SourceMap { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public int OriginalSize { get; set; }
        public int ProcessedSize { get; set; }
        public double CompressionRatio { get; set; }
    }
} 