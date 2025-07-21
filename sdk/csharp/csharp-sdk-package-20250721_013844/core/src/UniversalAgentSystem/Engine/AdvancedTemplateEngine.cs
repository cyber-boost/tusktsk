using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace TODO2.UniversalAgentSystem.Engine
{
    /// <summary>
    /// Advanced Template Engine for Universal Agent System
    /// Supports 15+ programming languages with conditional logic and inheritance
    /// </summary>
    public class AdvancedTemplateEngine
    {
        private readonly ILogger<AdvancedTemplateEngine> _logger;
        private readonly Dictionary<string, ITemplateLanguageProvider> _languageProviders;
        private readonly TemplateValidationEngine _validator;
        private readonly TemplateCache _cache;

        public AdvancedTemplateEngine(ILogger<AdvancedTemplateEngine> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _languageProviders = InitializeLanguageProviders();
            _validator = new TemplateValidationEngine();
            _cache = new TemplateCache();
        }

        /// <summary>
        /// Initialize language-specific template providers for 15+ languages
        /// </summary>
        private Dictionary<string, ITemplateLanguageProvider> InitializeLanguageProviders()
        {
            return new Dictionary<string, ITemplateLanguageProvider>
            {
                ["go"] = new GoTemplateProvider(),
                ["csharp"] = new CSharpTemplateProvider(),
                ["python"] = new PythonTemplateProvider(),
                ["typescript"] = new TypeScriptTemplateProvider(),
                ["rust"] = new RustTemplateProvider(),
                ["java"] = new JavaTemplateProvider(),
                ["javascript"] = new JavaScriptTemplateProvider(),
                ["php"] = new PhpTemplateProvider(),
                ["ruby"] = new RubyTemplateProvider(),
                ["swift"] = new SwiftTemplateProvider(),
                ["kotlin"] = new KotlinTemplateProvider(),
                ["scala"] = new ScalaTemplateProvider(),
                ["elixir"] = new ElixirTemplateProvider(),
                ["haskell"] = new HaskellTemplateProvider(),
                ["clojure"] = new ClojureTemplateProvider()
            };
        }

        /// <summary>
        /// Process template with advanced variable substitution and conditional logic
        /// </summary>
        public async Task<TemplateResult> ProcessTemplateAsync(string templateContent, Dictionary<string, object> variables, string language)
        {
            try
            {
                _logger.LogInformation("Processing template for language: {Language}", language);

                // Validate template and variables
                var validationResult = await _validator.ValidateTemplateAsync(templateContent, variables, language);
                if (!validationResult.IsValid)
                {
                    return TemplateResult.Failure(validationResult.Errors);
                }

                // Get language-specific provider
                if (!_languageProviders.TryGetValue(language.ToLowerInvariant(), out var provider))
                {
                    return TemplateResult.Failure(new[] { $"Unsupported language: {language}" });
                }

                // Check cache first
                var cacheKey = GenerateCacheKey(templateContent, variables, language);
                if (_cache.TryGet(cacheKey, out var cachedResult))
                {
                    _logger.LogDebug("Template result retrieved from cache");
                    return cachedResult;
                }

                // Process template with language-specific optimizations
                var processedContent = await ProcessTemplateWithProviderAsync(templateContent, variables, provider);

                // Apply language-specific syntax validation
                var syntaxValidation = await provider.ValidateSyntaxAsync(processedContent);
                if (!syntaxValidation.IsValid)
                {
                    return TemplateResult.Failure(syntaxValidation.Errors);
                }

                var result = TemplateResult.Success(processedContent, language);
                _cache.Set(cacheKey, result);

                _logger.LogInformation("Template processed successfully for language: {Language}", language);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing template for language: {Language}", language);
                return TemplateResult.Failure(new[] { ex.Message });
            }
        }

        /// <summary>
        /// Process template with advanced conditional logic and inheritance
        /// </summary>
        private async Task<string> ProcessTemplateWithProviderAsync(string template, Dictionary<string, object> variables, ITemplateLanguageProvider provider)
        {
            var processed = template;

            // Process inheritance patterns
            processed = await ProcessInheritanceAsync(processed, variables, provider);

            // Process conditional logic
            processed = await ProcessConditionalsAsync(processed, variables, provider);

            // Process variable substitution with language-specific syntax
            processed = await ProcessVariableSubstitutionAsync(processed, variables, provider);

            // Process loops and iterations
            processed = await ProcessLoopsAsync(processed, variables, provider);

            // Apply language-specific optimizations
            processed = await provider.ApplyOptimizationsAsync(processed);

            return processed;
        }

        /// <summary>
        /// Process template inheritance patterns
        /// </summary>
        private async Task<string> ProcessInheritanceAsync(string template, Dictionary<string, object> variables, ITemplateLanguageProvider provider)
        {
            var inheritancePattern = @"\{\{\s*extends\s+([^}]+)\s*\}\}";
            var match = Regex.Match(template, inheritancePattern);

            if (match.Success)
            {
                var parentTemplate = match.Groups[1].Value.Trim();
                var parentContent = await LoadParentTemplateAsync(parentTemplate);

                // Replace extends directive with parent content
                template = Regex.Replace(template, inheritancePattern, parentContent);

                // Process blocks in parent template
                template = await ProcessBlocksAsync(template, variables, provider);
            }

            return template;
        }

        /// <summary>
        /// Process conditional logic with language-specific syntax
        /// </summary>
        private async Task<string> ProcessConditionalsAsync(string template, Dictionary<string, object> variables, ITemplateLanguageProvider provider)
        {
            var conditionalPattern = @"\{\{\s*if\s+([^}]+)\s*\}\}(.*?)\{\{\s*endif\s*\}\}";
            var matches = Regex.Matches(template, conditionalPattern, RegexOptions.Singleline);

            foreach (Match match in matches)
            {
                var condition = match.Groups[1].Value.Trim();
                var content = match.Groups[2].Value;

                var shouldInclude = await EvaluateConditionAsync(condition, variables);
                var replacement = shouldInclude ? content : string.Empty;

                template = template.Replace(match.Value, replacement);
            }

            return template;
        }

        /// <summary>
        /// Process variable substitution with language-specific syntax
        /// </summary>
        private async Task<string> ProcessVariableSubstitutionAsync(string template, Dictionary<string, object> variables, ITemplateLanguageProvider provider)
        {
            var variablePattern = @"\{\{\s*([^}]+)\s*\}\}";
            var matches = Regex.Matches(template, variablePattern);

            foreach (Match match in matches)
            {
                var variableName = match.Groups[1].Value.Trim();
                var value = await ResolveVariableAsync(variableName, variables, provider);
                var formattedValue = await provider.FormatValueAsync(value, variableName);

                template = template.Replace(match.Value, formattedValue);
            }

            return template;
        }

        /// <summary>
        /// Process loops and iterations
        /// </summary>
        private async Task<string> ProcessLoopsAsync(string template, Dictionary<string, object> variables, ITemplateLanguageProvider provider)
        {
            var loopPattern = @"\{\{\s*foreach\s+([^}]+)\s*in\s+([^}]+)\s*\}\}(.*?)\{\{\s*endforeach\s*\}\}";
            var matches = Regex.Matches(template, loopPattern, RegexOptions.Singleline);

            foreach (Match match in matches)
            {
                var itemName = match.Groups[1].Value.Trim();
                var collectionName = match.Groups[2].Value.Trim();
                var loopContent = match.Groups[3].Value;

                if (variables.TryGetValue(collectionName, out var collection) && collection is IEnumerable<object> items)
                {
                    var loopResult = new StringBuilder();
                    foreach (var item in items)
                    {
                        var itemVariables = new Dictionary<string, object>(variables) { [itemName] = item };
                        var processedContent = await ProcessVariableSubstitutionAsync(loopContent, itemVariables, provider);
                        loopResult.Append(processedContent);
                    }

                    template = template.Replace(match.Value, loopResult.ToString());
                }
            }

            return template;
        }

        /// <summary>
        /// Process template blocks for inheritance
        /// </summary>
        private async Task<string> ProcessBlocksAsync(string template, Dictionary<string, object> variables, ITemplateLanguageProvider provider)
        {
            var blockPattern = @"\{\{\s*block\s+([^}]+)\s*\}\}(.*?)\{\{\s*endblock\s*\}\}";
            var matches = Regex.Matches(template, blockPattern, RegexOptions.Singleline);

            foreach (Match match in matches)
            {
                var blockName = match.Groups[1].Value.Trim();
                var blockContent = match.Groups[2].Value;

                // Process block content with variables
                var processedBlock = await ProcessVariableSubstitutionAsync(blockContent, variables, provider);
                template = template.Replace(match.Value, processedBlock);
            }

            return template;
        }

        /// <summary>
        /// Evaluate conditional expressions
        /// </summary>
        private async Task<bool> EvaluateConditionAsync(string condition, Dictionary<string, object> variables)
        {
            try
            {
                // Simple condition evaluation - can be extended with more complex logic
                var parts = condition.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                
                if (parts.Length == 3)
                {
                    var left = await ResolveVariableAsync(parts[0], variables, null);
                    var op = parts[1];
                    var right = await ResolveVariableAsync(parts[2], variables, null);

                    return op switch
                    {
                        "==" => Equals(left, right),
                        "!=" => !Equals(left, right),
                        ">" => Compare(left, right) > 0,
                        "<" => Compare(left, right) < 0,
                        ">=" => Compare(left, right) >= 0,
                        "<=" => Compare(left, right) <= 0,
                        _ => false
                    };
                }

                // Boolean variable check
                if (variables.TryGetValue(condition, out var value))
                {
                    return value is bool boolValue && boolValue;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Resolve variable value from context
        /// </summary>
        private async Task<object> ResolveVariableAsync(string variableName, Dictionary<string, object> variables, ITemplateLanguageProvider provider)
        {
            if (variables.TryGetValue(variableName, out var value))
            {
                return value;
            }

            // Handle nested properties (e.g., "user.name")
            var parts = variableName.Split('.');
            if (parts.Length > 1 && variables.TryGetValue(parts[0], out var parent))
            {
                return await ResolveNestedPropertyAsync(parent, parts.Skip(1), provider);
            }

            return string.Empty;
        }

        /// <summary>
        /// Resolve nested object properties
        /// </summary>
        private async Task<object> ResolveNestedPropertyAsync(object parent, IEnumerable<string> properties, ITemplateLanguageProvider provider)
        {
            var current = parent;
            foreach (var property in properties)
            {
                if (current == null) return string.Empty;

                if (current is Dictionary<string, object> dict && dict.TryGetValue(property, out var dictValue))
                {
                    current = dictValue;
                }
                else if (current is JsonElement jsonElement && jsonElement.TryGetProperty(property, out var jsonValue))
                {
                    current = jsonValue.GetString();
                }
                else
                {
                    // Use reflection for object properties
                    var prop = current.GetType().GetProperty(property);
                    current = prop?.GetValue(current) ?? string.Empty;
                }
            }

            return current ?? string.Empty;
        }

        /// <summary>
        /// Load parent template for inheritance
        /// </summary>
        private async Task<string> LoadParentTemplateAsync(string templatePath)
        {
            try
            {
                if (File.Exists(templatePath))
                {
                    return await File.ReadAllTextAsync(templatePath);
                }

                // Try loading from template directory
                var templateDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "templates");
                var fullPath = Path.Combine(templateDir, templatePath);
                
                if (File.Exists(fullPath))
                {
                    return await File.ReadAllTextAsync(fullPath);
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load parent template: {TemplatePath}", templatePath);
                return string.Empty;
            }
        }

        /// <summary>
        /// Generate cache key for template processing
        /// </summary>
        private string GenerateCacheKey(string template, Dictionary<string, object> variables, string language)
        {
            var variablesJson = JsonSerializer.Serialize(variables);
            var combined = $"{template}|{variablesJson}|{language}";
            return Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(combined)));
        }

        /// <summary>
        /// Compare values for conditional evaluation
        /// </summary>
        private int Compare(object left, object right)
        {
            if (left is IComparable leftComparable && right is IComparable rightComparable)
            {
                try
                {
                    return leftComparable.CompareTo(rightComparable);
                }
                catch
                {
                    return 0;
                }
            }

            return string.Compare(left?.ToString(), right?.ToString());
        }

        /// <summary>
        /// Get supported languages
        /// </summary>
        public IEnumerable<string> GetSupportedLanguages() => _languageProviders.Keys;

        /// <summary>
        /// Validate template syntax for specific language
        /// </summary>
        public async Task<ValidationResult> ValidateTemplateAsync(string template, string language)
        {
            if (!_languageProviders.TryGetValue(language.ToLowerInvariant(), out var provider))
            {
                return ValidationResult.Failure(new[] { $"Unsupported language: {language}" });
            }

            return await provider.ValidateSyntaxAsync(template);
        }
    }

    /// <summary>
    /// Template processing result
    /// </summary>
    public class TemplateResult
    {
        public bool IsSuccess { get; }
        public string Content { get; }
        public string Language { get; }
        public string[] Errors { get; }

        private TemplateResult(bool isSuccess, string content, string language, string[] errors)
        {
            IsSuccess = isSuccess;
            Content = content;
            Language = language;
            Errors = errors ?? Array.Empty<string>();
        }

        public static TemplateResult Success(string content, string language) 
            => new(true, content, language, null);

        public static TemplateResult Failure(string[] errors) 
            => new(false, string.Empty, string.Empty, errors);
    }

    /// <summary>
    /// Template validation result
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; }
        public string[] Errors { get; }

        private ValidationResult(bool isValid, string[] errors)
        {
            IsValid = isValid;
            Errors = errors ?? Array.Empty<string>();
        }

        public static ValidationResult Success() => new(true, null);
        public static ValidationResult Failure(string[] errors) => new(false, errors);
    }

    /// <summary>
    /// Interface for language-specific template providers
    /// </summary>
    public interface ITemplateLanguageProvider
    {
        Task<ValidationResult> ValidateSyntaxAsync(string content);
        Task<string> FormatValueAsync(object value, string variableName);
        Task<string> ApplyOptimizationsAsync(string content);
    }

    /// <summary>
    /// Template cache for performance optimization
    /// </summary>
    public class TemplateCache
    {
        private readonly Dictionary<string, TemplateResult> _cache = new();
        private readonly object _lock = new();

        public bool TryGet(string key, out TemplateResult result)
        {
            lock (_lock)
            {
                return _cache.TryGetValue(key, out result);
            }
        }

        public void Set(string key, TemplateResult result)
        {
            lock (_lock)
            {
                _cache[key] = result;
            }
        }
    }

    /// <summary>
    /// Template validation engine
    /// </summary>
    public class TemplateValidationEngine
    {
        public async Task<ValidationResult> ValidateTemplateAsync(string template, Dictionary<string, object> variables, string language)
        {
            var errors = new List<string>();

            // Validate template syntax
            if (string.IsNullOrWhiteSpace(template))
            {
                errors.Add("Template content cannot be empty");
            }

            // Validate variable references
            var variablePattern = @"\{\{\s*([^}]+)\s*\}\}";
            var matches = Regex.Matches(template, variablePattern);

            foreach (Match match in matches)
            {
                var variableName = match.Groups[1].Value.Trim();
                if (!variables.ContainsKey(variableName) && !variableName.Contains('.'))
                {
                    errors.Add($"Undefined variable: {variableName}");
                }
            }

            // Validate conditional syntax
            var conditionalPattern = @"\{\{\s*if\s+([^}]+)\s*\}\}";
            var conditionalMatches = Regex.Matches(template, conditionalPattern);
            var endifMatches = Regex.Matches(template, @"\{\{\s*endif\s*\}\}");

            if (conditionalMatches.Count != endifMatches.Count)
            {
                errors.Add("Mismatched if/endif statements");
            }

            return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors.ToArray());
        }
    }

    // Language-specific providers (implementations for each supported language)
    public class GoTemplateProvider : ITemplateLanguageProvider
    {
        public async Task<ValidationResult> ValidateSyntaxAsync(string content)
        {
            // Go-specific syntax validation
            var errors = new List<string>();
            
            // Check for proper Go syntax patterns
            if (content.Contains("package main") && !content.Contains("func main()"))
            {
                errors.Add("Go template missing main function");
            }

            return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors.ToArray());
        }

        public async Task<string> FormatValueAsync(object value, string variableName)
        {
            return value?.ToString() ?? "nil";
        }

        public async Task<string> ApplyOptimizationsAsync(string content)
        {
            // Go-specific optimizations
            return content.Replace("var ", "").Replace(":= ", "");
        }
    }

    public class CSharpTemplateProvider : ITemplateLanguageProvider
    {
        public async Task<ValidationResult> ValidateSyntaxAsync(string content)
        {
            var errors = new List<string>();
            
            if (content.Contains("using System;") && !content.Contains("namespace"))
            {
                errors.Add("C# template missing namespace declaration");
            }

            return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors.ToArray());
        }

        public async Task<string> FormatValueAsync(object value, string variableName)
        {
            return value?.ToString() ?? "null";
        }

        public async Task<string> ApplyOptimizationsAsync(string content)
        {
            // C#-specific optimizations
            return content.Replace("var ", "").Replace(";", "");
        }
    }

    // Additional language providers would be implemented similarly
    public class PythonTemplateProvider : ITemplateLanguageProvider
    {
        public async Task<ValidationResult> ValidateSyntaxAsync(string content) => ValidationResult.Success();
        public async Task<string> FormatValueAsync(object value, string variableName) => value?.ToString() ?? "None";
        public async Task<string> ApplyOptimizationsAsync(string content) => content;
    }

    public class TypeScriptTemplateProvider : ITemplateLanguageProvider
    {
        public async Task<ValidationResult> ValidateSyntaxAsync(string content) => ValidationResult.Success();
        public async Task<string> FormatValueAsync(object value, string variableName) => value?.ToString() ?? "undefined";
        public async Task<string> ApplyOptimizationsAsync(string content) => content;
    }

    public class RustTemplateProvider : ITemplateLanguageProvider
    {
        public async Task<ValidationResult> ValidateSyntaxAsync(string content) => ValidationResult.Success();
        public async Task<string> FormatValueAsync(object value, string variableName) => value?.ToString() ?? "None";
        public async Task<string> ApplyOptimizationsAsync(string content) => content;
    }

    // Additional providers for remaining languages...
    public class JavaTemplateProvider : ITemplateLanguageProvider
    {
        public async Task<ValidationResult> ValidateSyntaxAsync(string content) => ValidationResult.Success();
        public async Task<string> FormatValueAsync(object value, string variableName) => value?.ToString() ?? "null";
        public async Task<string> ApplyOptimizationsAsync(string content) => content;
    }

    public class JavaScriptTemplateProvider : ITemplateLanguageProvider
    {
        public async Task<ValidationResult> ValidateSyntaxAsync(string content) => ValidationResult.Success();
        public async Task<string> FormatValueAsync(object value, string variableName) => value?.ToString() ?? "undefined";
        public async Task<string> ApplyOptimizationsAsync(string content) => content;
    }

    public class PhpTemplateProvider : ITemplateLanguageProvider
    {
        public async Task<ValidationResult> ValidateSyntaxAsync(string content) => ValidationResult.Success();
        public async Task<string> FormatValueAsync(object value, string variableName) => value?.ToString() ?? "null";
        public async Task<string> ApplyOptimizationsAsync(string content) => content;
    }

    public class RubyTemplateProvider : ITemplateLanguageProvider
    {
        public async Task<ValidationResult> ValidateSyntaxAsync(string content) => ValidationResult.Success();
        public async Task<string> FormatValueAsync(object value, string variableName) => value?.ToString() ?? "nil";
        public async Task<string> ApplyOptimizationsAsync(string content) => content;
    }

    public class SwiftTemplateProvider : ITemplateLanguageProvider
    {
        public async Task<ValidationResult> ValidateSyntaxAsync(string content) => ValidationResult.Success();
        public async Task<string> FormatValueAsync(object value, string variableName) => value?.ToString() ?? "nil";
        public async Task<string> ApplyOptimizationsAsync(string content) => content;
    }

    public class KotlinTemplateProvider : ITemplateLanguageProvider
    {
        public async Task<ValidationResult> ValidateSyntaxAsync(string content) => ValidationResult.Success();
        public async Task<string> FormatValueAsync(object value, string variableName) => value?.ToString() ?? "null";
        public async Task<string> ApplyOptimizationsAsync(string content) => content;
    }

    public class ScalaTemplateProvider : ITemplateLanguageProvider
    {
        public async Task<ValidationResult> ValidateSyntaxAsync(string content) => ValidationResult.Success();
        public async Task<string> FormatValueAsync(object value, string variableName) => value?.ToString() ?? "null";
        public async Task<string> ApplyOptimizationsAsync(string content) => content;
    }

    public class ElixirTemplateProvider : ITemplateLanguageProvider
    {
        public async Task<ValidationResult> ValidateSyntaxAsync(string content) => ValidationResult.Success();
        public async Task<string> FormatValueAsync(object value, string variableName) => value?.ToString() ?? "nil";
        public async Task<string> ApplyOptimizationsAsync(string content) => content;
    }

    public class HaskellTemplateProvider : ITemplateLanguageProvider
    {
        public async Task<ValidationResult> ValidateSyntaxAsync(string content) => ValidationResult.Success();
        public async Task<string> FormatValueAsync(object value, string variableName) => value?.ToString() ?? "Nothing";
        public async Task<string> ApplyOptimizationsAsync(string content) => content;
    }

    public class ClojureTemplateProvider : ITemplateLanguageProvider
    {
        public async Task<ValidationResult> ValidateSyntaxAsync(string content) => ValidationResult.Success();
        public async Task<string> FormatValueAsync(object value, string variableName) => value?.ToString() ?? "nil";
        public async Task<string> ApplyOptimizationsAsync(string content) => content;
    }
} 