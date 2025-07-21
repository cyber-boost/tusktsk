using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TuskLang;

namespace TuskLang.CLI.Commands.Tusk
{
    /// <summary>
    /// Validate command implementation - Comprehensive validation with detailed reports
    /// Provides deep analysis, best practices checking, and detailed reporting
    /// </summary>
    public static class ValidateCommand
    {
        public static Command CreateValidateCommand()
        {
            // Arguments
            var fileArgument = new Argument<string>(
                name: "file",
                description: "Path to the .tsk file to validate");

            // Options
            var outputOption = new Option<string>(
                aliases: new[] { "--output", "-o" },
                description: "Output file for validation report");

            var formatOption = new Option<string>(
                aliases: new[] { "--format", "-f" },
                getDefaultValue: () => "detailed",
                description: "Report format: detailed, json, summary, checklist")
            {
                AllowedValues = { "detailed", "json", "summary", "checklist" }
            };

            var strictOption = new Option<bool>(
                aliases: new[] { "--strict" },
                description: "Enable strict validation mode");

            var bestPracticesOption = new Option<bool>(
                aliases: new[] { "--best-practices" },
                getDefaultValue: () => true,
                description: "Check against best practices");

            var performanceOption = new Option<bool>(
                aliases: new[] { "--performance" },
                description: "Include performance analysis");

            var securityOption = new Option<bool>(
                aliases: new[] { "--security" },
                description: "Include security analysis");

            var compatibilityOption = new Option<bool>(
                aliases: new[] { "--compatibility" },
                description: "Check compatibility with different versions");

            // Create command
            var validateCommand = new Command("validate", "Comprehensive validation with detailed analysis and reporting")
            {
                fileArgument,
                outputOption,
                formatOption,
                strictOption,
                bestPracticesOption,
                performanceOption,
                securityOption,
                compatibilityOption
            };

            validateCommand.SetHandler(async (file, output, format, strict, bestPractices, performance, security, compatibility) =>
            {
                var command = new ValidateCommandImplementation();
                Environment.ExitCode = await command.ExecuteAsync(
                    file, output, format, strict, bestPractices, performance, security, compatibility);
            }, fileArgument, outputOption, formatOption, strictOption, bestPracticesOption, performanceOption, securityOption, compatibilityOption);

            return validateCommand;
        }
    }

    /// <summary>
    /// Validate command implementation with comprehensive analysis capabilities
    /// </summary>
    public class ValidateCommandImplementation : CommandBase
    {
        public async Task<int> ExecuteAsync(
            string file,
            string output,
            string format,
            bool strict,
            bool bestPractices,
            bool performance,
            bool security,
            bool compatibility)
        {
            return await ExecuteWithTimingAsync(async () =>
            {
                if (string.IsNullOrEmpty(file))
                {
                    WriteError("No file specified. Use: tusk validate <file>");
                    return 1;
                }

                // Resolve file path
                var filePath = Path.GetFullPath(file);
                if (!ValidateFileExists(filePath, "TSK file"))
                    return 1;

                WriteProcessing($"Validating TSK file: {Path.GetFileName(filePath)}");

                // Load TSK file
                var tsk = await LoadTskFileAsync(filePath);
                if (tsk == null)
                    return 1;

                // Perform comprehensive validation
                var validationReport = await PerformComprehensiveValidationAsync(
                    tsk, filePath, strict, bestPractices, performance, security, compatibility);

                // Output results
                await OutputValidationReportAsync(validationReport, output, format);

                // Return appropriate exit code
                if (strict && validationReport.HasWarnings)
                    return 1;
                
                return validationReport.HasErrors ? 1 : 0;
            }, "Validate");
        }

        private async Task<ValidationReport> PerformComprehensiveValidationAsync(
            TSK tsk,
            string filePath,
            bool strict,
            bool bestPractices,
            bool performance,
            bool security,
            bool compatibility)
        {
            var report = new ValidationReport
            {
                FilePath = filePath,
                ValidatedAt = DateTime.UtcNow,
                StrictMode = strict,
                Sections = new List<ValidationSection>()
            };

            try
            {
                // Basic structure validation
                WriteProcessing("Performing basic validation...");
                var basicValidation = await ValidateTskContentAsync(tsk);
                AddValidationSection(report, "Basic Structure", basicValidation.IsValid, basicValidation.Issues, basicValidation.Warnings);

                // Syntax and format validation
                WriteProcessing("Validating syntax and format...");
                await ValidateSyntaxAndFormatAsync(report, filePath);

                // Schema validation
                WriteProcessing("Validating schema compliance...");
                await ValidateSchemaComplianceAsync(report, tsk);

                // Reference validation
                WriteProcessing("Validating references and dependencies...");
                await ValidateReferencesAsync(report, tsk);

                // Best practices check
                if (bestPractices)
                {
                    WriteProcessing("Checking best practices...");
                    await ValidateBestPracticesAsync(report, tsk);
                }

                // Performance analysis
                if (performance)
                {
                    WriteProcessing("Analyzing performance characteristics...");
                    await ValidatePerformanceAsync(report, tsk, filePath);
                }

                // Security analysis
                if (security)
                {
                    WriteProcessing("Performing security analysis...");
                    await ValidateSecurityAsync(report, tsk);
                }

                // Compatibility check
                if (compatibility)
                {
                    WriteProcessing("Checking version compatibility...");
                    await ValidateCompatibilityAsync(report, tsk);
                }

                // Calculate overall results
                report.HasErrors = report.Sections.Any(s => s.Issues.Count > 0);
                report.HasWarnings = report.Sections.Any(s => s.Warnings.Count > 0);
                report.TotalIssues = report.Sections.Sum(s => s.Issues.Count);
                report.TotalWarnings = report.Sections.Sum(s => s.Warnings.Count);

                var statusMessage = report.HasErrors 
                    ? $"❌ Validation failed: {report.TotalIssues} errors, {report.TotalWarnings} warnings"
                    : report.HasWarnings 
                        ? $"⚠️ Validation completed with warnings: {report.TotalWarnings} warnings"
                        : "✅ Validation passed successfully";

                WriteInfo(statusMessage);

                return report;
            }
            catch (Exception ex)
            {
                WriteError($"Validation failed: {ex.Message}");
                AddValidationSection(report, "Validation Error", false, new List<string> { ex.Message }, new List<string>());
                report.HasErrors = true;
                return report;
            }
        }

        private async Task ValidateSyntaxAndFormatAsync(ValidationReport report, string filePath)
        {
            var issues = new List<string>();
            var warnings = new List<string>();

            try
            {
                var content = await File.ReadAllTextAsync(filePath);
                var lines = content.Split('\n');

                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    var lineNumber = i + 1;

                    // Check for common syntax issues
                    if (line.TrimStart().StartsWith("[") && !line.TrimEnd().EndsWith("]"))
                    {
                        issues.Add($"Line {lineNumber}: Unclosed section header");
                    }

                    // Check for invalid characters in keys
                    var keyMatch = Regex.Match(line, @"^\s*([^=\[\]#]+)\s*=");
                    if (keyMatch.Success)
                    {
                        var key = keyMatch.Groups[1].Value.Trim();
                        if (string.IsNullOrWhiteSpace(key))
                        {
                            issues.Add($"Line {lineNumber}: Empty key");
                        }
                        else if (key.Contains("  "))
                        {
                            warnings.Add($"Line {lineNumber}: Key '{key}' contains multiple spaces");
                        }
                        else if (key.StartsWith(" ") || key.EndsWith(" "))
                        {
                            warnings.Add($"Line {lineNumber}: Key '{key}' has leading/trailing whitespace");
                        }
                    }

                    // Check for unmatched quotes
                    var singleQuotes = line.Count(c => c == '\'');
                    var doubleQuotes = line.Count(c => c == '"');
                    
                    if (singleQuotes % 2 != 0)
                    {
                        issues.Add($"Line {lineNumber}: Unmatched single quotes");
                    }
                    
                    if (doubleQuotes % 2 != 0)
                    {
                        issues.Add($"Line {lineNumber}: Unmatched double quotes");
                    }

                    // Check for very long lines
                    if (line.Length > 500)
                    {
                        warnings.Add($"Line {lineNumber}: Very long line ({line.Length} characters)");
                    }
                }

                // Check file encoding
                if (content.Contains('\0'))
                {
                    issues.Add("File contains null bytes - invalid text encoding");
                }

                // Check for BOM
                if (content.StartsWith("\uFEFF"))
                {
                    warnings.Add("File contains UTF-8 BOM - may cause parsing issues");
                }
            }
            catch (Exception ex)
            {
                issues.Add($"Syntax validation failed: {ex.Message}");
            }

            AddValidationSection(report, "Syntax & Format", issues.Count == 0, issues, warnings);
        }

        private async Task ValidateSchemaComplianceAsync(ValidationReport report, TSK tsk)
        {
            var issues = new List<string>();
            var warnings = new List<string>();

            try
            {
                var data = tsk.ToDictionary();

                // Check for required sections (if any defined in schema)
                var knownSectionTypes = new HashSet<string> { "database", "server", "application", "logging", "security" };

                foreach (var section in data)
                {
                    if (section.Value is Dictionary<string, object> sectionData)
                    {
                        // Check for empty sections
                        if (sectionData.Count == 0)
                        {
                            warnings.Add($"Section '{section.Key}' is empty");
                        }

                        // Check for reserved keywords
                        var reservedKeywords = new HashSet<string> { "version", "schema", "metadata" };
                        if (reservedKeywords.Contains(section.Key.ToLower()))
                        {
                            warnings.Add($"Section '{section.Key}' uses reserved keyword");
                        }

                        // Validate property naming conventions
                        foreach (var property in sectionData)
                        {
                            if (!IsValidPropertyName(property.Key))
                            {
                                warnings.Add($"Property '{section.Key}.{property.Key}' doesn't follow naming conventions");
                            }

                            // Check for SQL injection patterns in values
                            if (property.Value is string strValue && ContainsSuspiciousPatterns(strValue))
                            {
                                warnings.Add($"Property '{section.Key}.{property.Key}' contains potentially suspicious patterns");
                            }
                        }
                    }
                    else
                    {
                        warnings.Add($"Section '{section.Key}' is not a dictionary structure");
                    }
                }
            }
            catch (Exception ex)
            {
                issues.Add($"Schema validation failed: {ex.Message}");
            }

            AddValidationSection(report, "Schema Compliance", issues.Count == 0, issues, warnings);
        }

        private async Task ValidateReferencesAsync(ValidationReport report, TSK tsk)
        {
            var issues = new List<string>();
            var warnings = new List<string>();

            try
            {
                var data = tsk.ToDictionary();
                var allKeys = new HashSet<string>();
                var references = new List<(string source, string target)>();

                // Collect all keys
                foreach (var section in data)
                {
                    if (section.Value is Dictionary<string, object> sectionData)
                    {
                        foreach (var property in sectionData)
                        {
                            allKeys.Add($"{section.Key}.{property.Key}");
                        }
                    }
                }

                // Find references
                foreach (var section in data)
                {
                    if (section.Value is Dictionary<string, object> sectionData)
                    {
                        foreach (var property in sectionData)
                        {
                            if (property.Value is string strValue)
                            {
                                // Look for reference patterns like ${section.key} or @{section.key}
                                var referenceMatches = Regex.Matches(strValue, @"\$\{([^}]+)\}|@\{([^}]+)\}");
                                
                                foreach (Match match in referenceMatches)
                                {
                                    var reference = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
                                    references.Add(($"{section.Key}.{property.Key}", reference));
                                    
                                    if (!allKeys.Contains(reference))
                                    {
                                        issues.Add($"Reference '{reference}' in '{section.Key}.{property.Key}' points to non-existent key");
                                    }
                                }
                            }
                        }
                    }
                }

                // Check for circular references
                var circularReferences = DetectCircularReferences(references);
                foreach (var circular in circularReferences)
                {
                    issues.Add($"Circular reference detected: {string.Join(" -> ", circular)}");
                }
            }
            catch (Exception ex)
            {
                issues.Add($"Reference validation failed: {ex.Message}");
            }

            AddValidationSection(report, "References & Dependencies", issues.Count == 0, issues, warnings);
        }

        private async Task ValidateBestPracticesAsync(ValidationReport report, TSK tsk)
        {
            var issues = new List<string>();
            var warnings = new List<string>();

            try
            {
                var data = tsk.ToDictionary();

                // Check naming conventions
                foreach (var section in data)
                {
                    // Section names should be lowercase with underscores
                    if (!Regex.IsMatch(section.Key, @"^[a-z][a-z0-9_]*$"))
                    {
                        warnings.Add($"Section '{section.Key}' doesn't follow naming convention (lowercase_with_underscores)");
                    }

                    if (section.Value is Dictionary<string, object> sectionData)
                    {
                        // Check for sensitive data
                        foreach (var property in sectionData)
                        {
                            var key = property.Key.ToLower();
                            var value = property.Value?.ToString() ?? "";

                            // Check for passwords, keys, secrets
                            if (key.Contains("password") || key.Contains("secret") || key.Contains("key"))
                            {
                                if (value.Length < 8)
                                {
                                    warnings.Add($"Property '{section.Key}.{property.Key}' appears to be sensitive but has weak value");
                                }
                                
                                if (!value.StartsWith("${") && !value.StartsWith("@{"))
                                {
                                    warnings.Add($"Property '{section.Key}.{property.Key}' contains sensitive data without reference");
                                }
                            }

                            // Check for hardcoded URLs
                            if (value.StartsWith("http://"))
                            {
                                warnings.Add($"Property '{section.Key}.{property.Key}' uses insecure HTTP URL");
                            }

                            // Check for development/test patterns
                            if (value.ToLower().Contains("localhost") || value.ToLower().Contains("127.0.0.1"))
                            {
                                warnings.Add($"Property '{section.Key}.{property.Key}' contains localhost reference");
                            }
                        }

                        // Check section organization
                        if (sectionData.Count > 20)
                        {
                            warnings.Add($"Section '{section.Key}' has many properties ({sectionData.Count}) - consider breaking it down");
                        }
                    }
                }

                // Check for documentation
                var hasCommentedSections = data.Keys.Any(k => k.StartsWith("#") || k.Contains("_doc"));
                if (!hasCommentedSections)
                {
                    warnings.Add("Configuration lacks documentation comments");
                }
            }
            catch (Exception ex)
            {
                issues.Add($"Best practices validation failed: {ex.Message}");
            }

            AddValidationSection(report, "Best Practices", issues.Count == 0, issues, warnings);
        }

        private async Task ValidatePerformanceAsync(ValidationReport report, TSK tsk, string filePath)
        {
            var issues = new List<string>();
            var warnings = new List<string>();

            try
            {
                var fileInfo = new FileInfo(filePath);
                var data = tsk.ToDictionary();

                // File size analysis
                if (fileInfo.Length > 10 * 1024 * 1024) // 10MB
                {
                    warnings.Add($"Large configuration file ({fileInfo.Length:N0} bytes) may impact performance");
                }

                // Complexity analysis
                var totalProperties = data.Values.OfType<Dictionary<string, object>>().Sum(s => s.Count);
                if (totalProperties > 1000)
                {
                    warnings.Add($"High property count ({totalProperties}) may impact parsing performance");
                }

                // Deep nesting analysis
                var maxDepth = CalculateMaxDepth(data);
                if (maxDepth > 10)
                {
                    warnings.Add($"Deep nesting detected (depth: {maxDepth}) may impact performance");
                }

                // Large value analysis
                foreach (var section in data)
                {
                    if (section.Value is Dictionary<string, object> sectionData)
                    {
                        foreach (var property in sectionData)
                        {
                            if (property.Value is string strValue && strValue.Length > 100000) // 100KB
                            {
                                warnings.Add($"Large value in '{section.Key}.{property.Key}' ({strValue.Length} chars) may impact performance");
                            }
                        }
                    }
                }

                // Function complexity analysis
                var functionCount = CountComplexFunctions(data);
                if (functionCount > 50)
                {
                    warnings.Add($"High function count ({functionCount}) may impact execution performance");
                }
            }
            catch (Exception ex)
            {
                issues.Add($"Performance validation failed: {ex.Message}");
            }

            AddValidationSection(report, "Performance Analysis", issues.Count == 0, issues, warnings);
        }

        private async Task ValidateSecurityAsync(ValidationReport report, TSK tsk)
        {
            var issues = new List<string>();
            var warnings = new List<string>();

            try
            {
                var data = tsk.ToDictionary();

                foreach (var section in data)
                {
                    if (section.Value is Dictionary<string, object> sectionData)
                    {
                        foreach (var property in sectionData)
                        {
                            var key = property.Key.ToLower();
                            var value = property.Value?.ToString() ?? "";

                            // Check for plaintext passwords
                            if ((key.Contains("password") || key.Contains("secret")) && 
                                !value.StartsWith("${") && !value.StartsWith("@{") &&
                                !string.IsNullOrEmpty(value))
                            {
                                issues.Add($"Plaintext credential detected in '{section.Key}.{property.Key}'");
                            }

                            // Check for SQL injection patterns
                            if (ContainsSqlInjectionPatterns(value))
                            {
                                warnings.Add($"Potential SQL injection pattern in '{section.Key}.{property.Key}'");
                            }

                            // Check for path traversal
                            if (value.Contains("../") || value.Contains("..\\"))
                            {
                                warnings.Add($"Path traversal pattern detected in '{section.Key}.{property.Key}'");
                            }

                            // Check for script injection
                            if (ContainsScriptInjectionPatterns(value))
                            {
                                warnings.Add($"Potential script injection pattern in '{section.Key}.{property.Key}'");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                issues.Add($"Security validation failed: {ex.Message}");
            }

            AddValidationSection(report, "Security Analysis", issues.Count == 0, issues, warnings);
        }

        private async Task ValidateCompatibilityAsync(ValidationReport report, TSK tsk)
        {
            var issues = new List<string>();
            var warnings = new List<string>();

            try
            {
                var data = tsk.ToDictionary();

                // Check for version-specific features
                foreach (var section in data)
                {
                    if (section.Value is Dictionary<string, object> sectionData)
                    {
                        foreach (var property in sectionData)
                        {
                            // Check for features that might not be compatible with older versions
                            if (property.Key.Contains("@") && !property.Key.StartsWith("@"))
                            {
                                warnings.Add($"Property '{section.Key}.{property.Key}' uses @ symbol which may not be compatible with all parsers");
                            }

                            // Check for Unicode characters
                            if (ContainsUnicodeCharacters(property.Key) || ContainsUnicodeCharacters(property.Value?.ToString()))
                            {
                                warnings.Add($"Property '{section.Key}.{property.Key}' contains Unicode characters which may cause compatibility issues");
                            }
                        }
                    }
                }

                // Check for overall structure compatibility
                if (data.Count == 0)
                {
                    warnings.Add("Empty configuration may not be supported by all parsers");
                }
            }
            catch (Exception ex)
            {
                issues.Add($"Compatibility validation failed: {ex.Message}");
            }

            AddValidationSection(report, "Version Compatibility", issues.Count == 0, issues, warnings);
        }

        private async Task OutputValidationReportAsync(ValidationReport report, string outputPath, string format)
        {
            string output;

            switch (format.ToLower())
            {
                case "json":
                    output = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
                    break;
                case "summary":
                    output = CreateSummaryReport(report);
                    break;
                case "checklist":
                    output = CreateChecklistReport(report);
                    break;
                default: // detailed
                    output = CreateDetailedReport(report);
                    break;
            }

            if (!string.IsNullOrEmpty(outputPath))
            {
                await SaveFileAtomicAsync(outputPath, output, "validation report");
            }
            else if (GlobalOptions.JsonOutput)
            {
                Console.WriteLine(JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
            }
            else
            {
                Console.WriteLine(output);
            }
        }

        private string CreateDetailedReport(ValidationReport report)
        {
            var output = new System.Text.StringBuilder();
            
            output.AppendLine($"🔍 Validation Report: {Path.GetFileName(report.FilePath)}");
            output.AppendLine($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            output.AppendLine($"File: {report.FilePath}");
            output.AppendLine($"Validated: {report.ValidatedAt:yyyy-MM-dd HH:mm:ss UTC}");
            output.AppendLine($"Strict Mode: {(report.StrictMode ? "✅ Enabled" : "⚪ Disabled")}");
            output.AppendLine();

            var overallStatus = report.HasErrors ? "❌ FAILED" : 
                               report.HasWarnings ? "⚠️ WARNINGS" : "✅ PASSED";
            output.AppendLine($"Overall Status: {overallStatus}");
            output.AppendLine($"Total Issues: {report.TotalIssues} errors, {report.TotalWarnings} warnings");
            output.AppendLine();

            foreach (var section in report.Sections)
            {
                var sectionStatus = section.Issues.Count == 0 ? "✅" : "❌";
                output.AppendLine($"{sectionStatus} {section.Name}");
                
                if (section.Issues.Count > 0)
                {
                    output.AppendLine("  Issues:");
                    foreach (var issue in section.Issues)
                    {
                        output.AppendLine($"    ❌ {issue}");
                    }
                }

                if (section.Warnings.Count > 0)
                {
                    output.AppendLine("  Warnings:");
                    foreach (var warning in section.Warnings)
                    {
                        output.AppendLine($"    ⚠️ {warning}");
                    }
                }

                if (section.Issues.Count == 0 && section.Warnings.Count == 0)
                {
                    output.AppendLine("  No issues found");
                }

                output.AppendLine();
            }

            return output.ToString();
        }

        private string CreateSummaryReport(ValidationReport report)
        {
            var status = report.HasErrors ? "❌ FAILED" : 
                        report.HasWarnings ? "⚠️ WARNINGS" : "✅ PASSED";
            
            return $"""
                🔍 {Path.GetFileName(report.FilePath)} - Validation Summary
                Status: {status}
                Issues: {report.TotalIssues} errors, {report.TotalWarnings} warnings
                Sections Checked: {report.Sections.Count}
                Validated: {report.ValidatedAt:yyyy-MM-dd HH:mm:ss}
                """;
        }

        private string CreateChecklistReport(ValidationReport report)
        {
            var output = new System.Text.StringBuilder();
            output.AppendLine($"📋 Validation Checklist: {Path.GetFileName(report.FilePath)}");
            output.AppendLine();

            foreach (var section in report.Sections)
            {
                var status = section.Issues.Count == 0 ? "✅" : "❌";
                output.AppendLine($"{status} {section.Name}");
            }

            return output.ToString();
        }

        // Helper methods
        private void AddValidationSection(ValidationReport report, string name, bool passed, List<string> issues, List<string> warnings)
        {
            report.Sections.Add(new ValidationSection
            {
                Name = name,
                Passed = passed,
                Issues = issues,
                Warnings = warnings
            });
        }

        private bool IsValidPropertyName(string name)
        {
            return Regex.IsMatch(name, @"^[a-zA-Z_][a-zA-Z0-9_]*$");
        }

        private bool ContainsSuspiciousPatterns(string value)
        {
            var suspiciousPatterns = new[]
            {
                @"\bSELECT\b.*\bFROM\b", @"\bINSERT\b.*\bINTO\b", @"\bUPDATE\b.*\bSET\b",
                @"\bDELETE\b.*\bFROM\b", @"\bDROP\b.*\bTABLE\b", @"<script\b", @"javascript:",
                @"eval\s*\(", @"setTimeout\s*\(", @"setInterval\s*\("
            };

            return suspiciousPatterns.Any(pattern => 
                Regex.IsMatch(value, pattern, RegexOptions.IgnoreCase));
        }

        private bool ContainsSqlInjectionPatterns(string value)
        {
            var sqlPatterns = new[]
            {
                @"'.*(\bOR\b|\bAND\b).*'", @";\s*DROP\s+", @"UNION\s+SELECT",
                @"'.*--", @"/\*.*\*/"
            };

            return sqlPatterns.Any(pattern => 
                Regex.IsMatch(value, pattern, RegexOptions.IgnoreCase));
        }

        private bool ContainsScriptInjectionPatterns(string value)
        {
            var scriptPatterns = new[]
            {
                @"<script\b.*>", @"javascript:", @"vbscript:", @"on\w+\s*=",
                @"eval\s*\(", @"Function\s*\("
            };

            return scriptPatterns.Any(pattern => 
                Regex.IsMatch(value, pattern, RegexOptions.IgnoreCase));
        }

        private bool ContainsUnicodeCharacters(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            return text.Any(c => c > 127);
        }

        private int CalculateMaxDepth(Dictionary<string, object> data, int currentDepth = 1)
        {
            int maxDepth = currentDepth;
            
            foreach (var value in data.Values)
            {
                if (value is Dictionary<string, object> nested)
                {
                    int depth = CalculateMaxDepth(nested, currentDepth + 1);
                    if (depth > maxDepth)
                        maxDepth = depth;
                }
            }
            
            return maxDepth;
        }

        private int CountComplexFunctions(Dictionary<string, object> data)
        {
            int count = 0;
            foreach (var section in data.Values)
            {
                if (section is Dictionary<string, object> sectionData)
                {
                    foreach (var value in sectionData.Values)
                    {
                        if (value is string strValue && 
                            (strValue.Length > 500 || strValue.Split('\n').Length > 20))
                        {
                            count++;
                        }
                    }
                }
            }
            return count;
        }

        private List<List<string>> DetectCircularReferences(List<(string source, string target)> references)
        {
            var circular = new List<List<string>>();
            var graph = references.GroupBy(r => r.source)
                                 .ToDictionary(g => g.Key, g => g.Select(r => r.target).ToList());

            var visited = new HashSet<string>();
            var recursionStack = new HashSet<string>();

            foreach (var node in graph.Keys)
            {
                if (!visited.Contains(node))
                {
                    var path = new List<string>();
                    if (HasCycle(node, graph, visited, recursionStack, path))
                    {
                        circular.Add(new List<string>(path));
                    }
                }
            }

            return circular;
        }

        private bool HasCycle(string node, Dictionary<string, List<string>> graph, 
                             HashSet<string> visited, HashSet<string> recursionStack, List<string> path)
        {
            visited.Add(node);
            recursionStack.Add(node);
            path.Add(node);

            if (graph.ContainsKey(node))
            {
                foreach (var neighbor in graph[node])
                {
                    if (!visited.Contains(neighbor))
                    {
                        if (HasCycle(neighbor, graph, visited, recursionStack, path))
                            return true;
                    }
                    else if (recursionStack.Contains(neighbor))
                    {
                        return true;
                    }
                }
            }

            recursionStack.Remove(node);
            path.RemoveAt(path.Count - 1);
            return false;
        }
    }

    #region Result Classes

    public class ValidationReport
    {
        public string FilePath { get; set; }
        public DateTime ValidatedAt { get; set; }
        public bool StrictMode { get; set; }
        public bool HasErrors { get; set; }
        public bool HasWarnings { get; set; }
        public int TotalIssues { get; set; }
        public int TotalWarnings { get; set; }
        public List<ValidationSection> Sections { get; set; } = new List<ValidationSection>();
    }

    public class ValidationSection
    {
        public string Name { get; set; }
        public bool Passed { get; set; }
        public List<string> Issues { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
    }

    #endregion
} 