using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TuskLang.Parser
{
    /// <summary>
    /// Parse Error Handler for TuskTsk Language Parser
    /// 
    /// Provides comprehensive error reporting:
    /// - Detailed error messages with line/column information
    /// - Error recovery suggestions and fix recommendations
    /// - Error categorization and severity levels
    /// - Context-aware error descriptions
    /// - Multiple error format outputs (text, JSON, structured)
    /// 
    /// Performance: Thread-safe error collection and reporting
    /// </summary>
    public class ParseErrorHandler : IDisposable
    {
        private readonly List<TuskParseError> _errors;
        private readonly List<TuskParseWarning> _warnings;
        private readonly Dictionary<int, string> _sourceLines;
        private readonly ErrorReportingOptions _options;
        private readonly object _lock;
        
        /// <summary>
        /// Initializes a new instance of the ParseErrorHandler
        /// </summary>
        public ParseErrorHandler(ErrorReportingOptions options = null)
        {
            _errors = new List<TuskParseError>();
            _warnings = new List<TuskParseWarning>();
            _sourceLines = new Dictionary<int, string>();
            _options = options ?? new ErrorReportingOptions();
            _lock = new object();
        }
        
        /// <summary>
        /// Set source code for context in error messages
        /// </summary>
        public void SetSource(string source)
        {
            lock (_lock)
            {
                _sourceLines.Clear();
                
                if (!string.IsNullOrEmpty(source))
                {
                    var lines = source.Split('\n');
                    for (int i = 0; i < lines.Length; i++)
                    {
                        _sourceLines[i + 1] = lines[i].TrimEnd('\r');
                    }
                }
            }
        }
        
        /// <summary>
        /// Report lexer error
        /// </summary>
        public void ReportLexerError(string message, int line, int column, char? character = null)
        {
            var error = new TuskParseError
            {
                Type = ParseErrorType.LexicalError,
                Message = message,
                Line = line,
                Column = column,
                Severity = ErrorSeverity.Error,
                Context = CreateErrorContext(line, column),
                Suggestions = GetLexerErrorSuggestions(message, character),
                ErrorCode = "LEX001"
            };
            
            lock (_lock)
            {
                _errors.Add(error);
            }
        }
        
        /// <summary>
        /// Report syntax error
        /// </summary>
        public void ReportSyntaxError(string message, int line, int column, Token? token = null)
        {
            var error = new TuskParseError
            {
                Type = ParseErrorType.SyntaxError,
                Message = message,
                Line = line,
                Column = column,
                Severity = ErrorSeverity.Error,
                Context = CreateErrorContext(line, column),
                Suggestions = GetSyntaxErrorSuggestions(message, token),
                ErrorCode = "SYN001",
                Token = token
            };
            
            lock (_lock)
            {
                _errors.Add(error);
            }
        }
        
        /// <summary>
        /// Report semantic error
        /// </summary>
        public void ReportSemanticError(string message, int line, int column, SemanticErrorType errorType)
        {
            var error = new TuskParseError
            {
                Type = ParseErrorType.SemanticError,
                Message = message,
                Line = line,
                Column = column,
                Severity = GetSemanticErrorSeverity(errorType),
                Context = CreateErrorContext(line, column),
                Suggestions = GetSemanticErrorSuggestions(message, errorType),
                ErrorCode = GetSemanticErrorCode(errorType)
            };
            
            lock (_lock)
            {
                _errors.Add(error);
            }
        }
        
        /// <summary>
        /// Report warning
        /// </summary>
        public void ReportWarning(string message, int line, int column, WarningType warningType)
        {
            var warning = new TuskParseWarning
            {
                Type = warningType,
                Message = message,
                Line = line,
                Column = column,
                Context = CreateErrorContext(line, column),
                Suggestions = GetWarningSuggestions(message, warningType),
                WarningCode = GetWarningCode(warningType)
            };
            
            lock (_lock)
            {
                _warnings.Add(warning);
            }
        }
        
        /// <summary>
        /// Get all errors
        /// </summary>
        public List<TuskParseError> GetErrors()
        {
            lock (_lock)
            {
                return new List<TuskParseError>(_errors);
            }
        }
        
        /// <summary>
        /// Get all warnings
        /// </summary>
        public List<TuskParseWarning> GetWarnings()
        {
            lock (_lock)
            {
                return new List<TuskParseWarning>(_warnings);
            }
        }
        
        /// <summary>
        /// Check if there are any errors
        /// </summary>
        public bool HasErrors()
        {
            lock (_lock)
            {
                return _errors.Count > 0;
            }
        }
        
        /// <summary>
        /// Check if there are any warnings
        /// </summary>
        public bool HasWarnings()
        {
            lock (_lock)
            {
                return _warnings.Count > 0;
            }
        }
        
        /// <summary>
        /// Get error count
        /// </summary>
        public int ErrorCount
        {
            get
            {
                lock (_lock)
                {
                    return _errors.Count;
                }
            }
        }
        
        /// <summary>
        /// Get warning count
        /// </summary>
        public int WarningCount
        {
            get
            {
                lock (_lock)
                {
                    return _warnings.Count;
                }
            }
        }
        
        /// <summary>
        /// Clear all errors and warnings
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _errors.Clear();
                _warnings.Clear();
            }
        }
        
        /// <summary>
        /// Generate formatted error report
        /// </summary>
        public string GenerateReport(ReportFormat format = ReportFormat.Text)
        {
            lock (_lock)
            {
                return format switch
                {
                    ReportFormat.Text => GenerateTextReport(),
                    ReportFormat.Json => GenerateJsonReport(),
                    ReportFormat.Xml => GenerateXmlReport(),
                    ReportFormat.Markdown => GenerateMarkdownReport(),
                    _ => GenerateTextReport()
                };
            }
        }
        
        /// <summary>
        /// Generate text format report
        /// </summary>
        private string GenerateTextReport()
        {
            var report = new StringBuilder();
            
            if (_errors.Count > 0)
            {
                report.AppendLine($"=== ERRORS ({_errors.Count}) ===");
                report.AppendLine();
                
                foreach (var error in _errors.OrderBy(e => e.Line).ThenBy(e => e.Column))
                {
                    report.AppendLine($"Error {error.ErrorCode} at line {error.Line}, column {error.Column}:");
                    report.AppendLine($"  {error.Message}");
                    
                    if (error.Context?.SourceLine != null)
                    {
                        report.AppendLine($"  | {error.Context.SourceLine}");
                        if (error.Column > 0)
                        {
                            var pointer = new string(' ', error.Column + 2) + "^";
                            report.AppendLine($"  | {pointer}");
                        }
                    }
                    
                    if (error.Suggestions?.Count > 0)
                    {
                        report.AppendLine("  Suggestions:");
                        foreach (var suggestion in error.Suggestions)
                        {
                            report.AppendLine($"    - {suggestion}");
                        }
                    }
                    
                    report.AppendLine();
                }
            }
            
            if (_warnings.Count > 0)
            {
                report.AppendLine($"=== WARNINGS ({_warnings.Count}) ===");
                report.AppendLine();
                
                foreach (var warning in _warnings.OrderBy(w => w.Line).ThenBy(w => w.Column))
                {
                    report.AppendLine($"Warning {warning.WarningCode} at line {warning.Line}, column {warning.Column}:");
                    report.AppendLine($"  {warning.Message}");
                    
                    if (warning.Context?.SourceLine != null)
                    {
                        report.AppendLine($"  | {warning.Context.SourceLine}");
                        if (warning.Column > 0)
                        {
                            var pointer = new string(' ', warning.Column + 2) + "^";
                            report.AppendLine($"  | {pointer}");
                        }
                    }
                    
                    if (warning.Suggestions?.Count > 0)
                    {
                        report.AppendLine("  Suggestions:");
                        foreach (var suggestion in warning.Suggestions)
                        {
                            report.AppendLine($"    - {suggestion}");
                        }
                    }
                    
                    report.AppendLine();
                }
            }
            
            if (_errors.Count == 0 && _warnings.Count == 0)
            {
                report.AppendLine("No errors or warnings found.");
            }
            else
            {
                report.AppendLine($"Total: {_errors.Count} error(s), {_warnings.Count} warning(s)");
            }
            
            return report.ToString();
        }
        
        /// <summary>
        /// Generate JSON format report
        /// </summary>
        private string GenerateJsonReport()
        {
            var report = new
            {
                errors = _errors.Select(e => new
                {
                    type = e.Type.ToString(),
                    code = e.ErrorCode,
                    message = e.Message,
                    line = e.Line,
                    column = e.Column,
                    severity = e.Severity.ToString(),
                    suggestions = e.Suggestions,
                    context = e.Context != null ? new { sourceLine = e.Context.SourceLine } : null
                }),
                warnings = _warnings.Select(w => new
                {
                    type = w.Type.ToString(),
                    code = w.WarningCode,
                    message = w.Message,
                    line = w.Line,
                    column = w.Column,
                    suggestions = w.Suggestions,
                    context = w.Context != null ? new { sourceLine = w.Context.SourceLine } : null
                }),
                summary = new
                {
                    errorCount = _errors.Count,
                    warningCount = _warnings.Count,
                    hasErrors = _errors.Count > 0
                }
            };
            
            return System.Text.Json.JsonSerializer.Serialize(report, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });
        }
        
        /// <summary>
        /// Generate XML format report
        /// </summary>
        private string GenerateXmlReport()
        {
            var report = new StringBuilder();
            report.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            report.AppendLine("<parseReport>");
            
            report.AppendLine("  <errors>");
            foreach (var error in _errors)
            {
                report.AppendLine("    <error>");
                report.AppendLine($"      <type>{error.Type}</type>");
                report.AppendLine($"      <code>{error.ErrorCode}</code>");
                report.AppendLine($"      <message>{System.Security.SecurityElement.Escape(error.Message)}</message>");
                report.AppendLine($"      <line>{error.Line}</line>");
                report.AppendLine($"      <column>{error.Column}</column>");
                report.AppendLine($"      <severity>{error.Severity}</severity>");
                
                if (error.Suggestions?.Count > 0)
                {
                    report.AppendLine("      <suggestions>");
                    foreach (var suggestion in error.Suggestions)
                    {
                        report.AppendLine($"        <suggestion>{System.Security.SecurityElement.Escape(suggestion)}</suggestion>");
                    }
                    report.AppendLine("      </suggestions>");
                }
                
                report.AppendLine("    </error>");
            }
            report.AppendLine("  </errors>");
            
            report.AppendLine("  <warnings>");
            foreach (var warning in _warnings)
            {
                report.AppendLine("    <warning>");
                report.AppendLine($"      <type>{warning.Type}</type>");
                report.AppendLine($"      <code>{warning.WarningCode}</code>");
                report.AppendLine($"      <message>{System.Security.SecurityElement.Escape(warning.Message)}</message>");
                report.AppendLine($"      <line>{warning.Line}</line>");
                report.AppendLine($"      <column>{warning.Column}</column>");
                
                if (warning.Suggestions?.Count > 0)
                {
                    report.AppendLine("      <suggestions>");
                    foreach (var suggestion in warning.Suggestions)
                    {
                        report.AppendLine($"        <suggestion>{System.Security.SecurityElement.Escape(suggestion)}</suggestion>");
                    }
                    report.AppendLine("      </suggestions>");
                }
                
                report.AppendLine("    </warning>");
            }
            report.AppendLine("  </warnings>");
            
            report.AppendLine("  <summary>");
            report.AppendLine($"    <errorCount>{_errors.Count}</errorCount>");
            report.AppendLine($"    <warningCount>{_warnings.Count}</warningCount>");
            report.AppendLine($"    <hasErrors>{_errors.Count > 0}</hasErrors>");
            report.AppendLine("  </summary>");
            
            report.AppendLine("</parseReport>");
            
            return report.ToString();
        }
        
        /// <summary>
        /// Generate Markdown format report
        /// </summary>
        private string GenerateMarkdownReport()
        {
            var report = new StringBuilder();
            report.AppendLine("# TuskTsk Parse Report");
            report.AppendLine();
            
            if (_errors.Count > 0)
            {
                report.AppendLine($"## Errors ({_errors.Count})");
                report.AppendLine();
                
                foreach (var error in _errors.OrderBy(e => e.Line))
                {
                    report.AppendLine($"### {error.ErrorCode} - Line {error.Line}:{error.Column}");
                    report.AppendLine($"**{error.Message}**");
                    report.AppendLine();
                    
                    if (error.Context?.SourceLine != null)
                    {
                        report.AppendLine("```");
                        report.AppendLine(error.Context.SourceLine);
                        if (error.Column > 0)
                        {
                            report.AppendLine(new string(' ', error.Column - 1) + "^");
                        }
                        report.AppendLine("```");
                        report.AppendLine();
                    }
                    
                    if (error.Suggestions?.Count > 0)
                    {
                        report.AppendLine("**Suggestions:**");
                        foreach (var suggestion in error.Suggestions)
                        {
                            report.AppendLine($"- {suggestion}");
                        }
                        report.AppendLine();
                    }
                }
            }
            
            if (_warnings.Count > 0)
            {
                report.AppendLine($"## Warnings ({_warnings.Count})");
                report.AppendLine();
                
                foreach (var warning in _warnings.OrderBy(w => w.Line))
                {
                    report.AppendLine($"### {warning.WarningCode} - Line {warning.Line}:{warning.Column}");
                    report.AppendLine($"**{warning.Message}**");
                    report.AppendLine();
                    
                    if (warning.Suggestions?.Count > 0)
                    {
                        report.AppendLine("**Suggestions:**");
                        foreach (var suggestion in warning.Suggestions)
                        {
                            report.AppendLine($"- {suggestion}");
                        }
                        report.AppendLine();
                    }
                }
            }
            
            report.AppendLine("## Summary");
            report.AppendLine($"- **Errors:** {_errors.Count}");
            report.AppendLine($"- **Warnings:** {_warnings.Count}");
            report.AppendLine($"- **Status:** {(_errors.Count == 0 ? "✅ Success" : "❌ Failed")}");
            
            return report.ToString();
        }
        
        /// <summary>
        /// Create error context with source line information
        /// </summary>
        private ErrorContext CreateErrorContext(int line, int column)
        {
            var context = new ErrorContext { Line = line, Column = column };
            
            if (_sourceLines.TryGetValue(line, out var sourceLine))
            {
                context.SourceLine = sourceLine;
                
                // Add surrounding lines for context
                if (_options.IncludeContextLines && _options.ContextLineCount > 0)
                {
                    context.ContextLines = new Dictionary<int, string>();
                    
                    int start = Math.Max(1, line - _options.ContextLineCount);
                    int end = Math.Min(_sourceLines.Keys.Max(), line + _options.ContextLineCount);
                    
                    for (int i = start; i <= end; i++)
                    {
                        if (_sourceLines.TryGetValue(i, out var contextLine))
                        {
                            context.ContextLines[i] = contextLine;
                        }
                    }
                }
            }
            
            return context;
        }
        
        /// <summary>
        /// Get lexer error suggestions
        /// </summary>
        private List<string> GetLexerErrorSuggestions(string message, char? character)
        {
            var suggestions = new List<string>();
            
            if (message.Contains("Unterminated string"))
            {
                suggestions.Add("Add closing quote to terminate the string");
                suggestions.Add("Check for escaped quotes within the string");
            }
            else if (message.Contains("Unexpected character"))
            {
                if (character.HasValue)
                {
                    var ch = character.Value;
                    suggestions.Add($"Remove or escape the character '{ch}'");
                    
                    if (char.IsControl(ch))
                    {
                        suggestions.Add("Check for invisible control characters");
                    }
                    
                    switch (ch)
                    {
                        case '\t':
                            suggestions.Add("Use spaces instead of tabs for indentation");
                            break;
                        case '\r':
                            suggestions.Add("Use Unix line endings (LF) instead of Windows (CRLF)");
                            break;
                    }
                }
                suggestions.Add("Check the TuskTsk syntax reference for valid characters");
            }
            else if (message.Contains("Invalid number"))
            {
                suggestions.Add("Check number format (e.g., 123, 123.45, 1.23e-4)");
                suggestions.Add("Remove any invalid characters from the number");
            }
            
            return suggestions;
        }
        
        /// <summary>
        /// Get syntax error suggestions
        /// </summary>
        private List<string> GetSyntaxErrorSuggestions(string message, Token? token)
        {
            var suggestions = new List<string>();
            
            if (message.Contains("Expected"))
            {
                if (message.Contains("':'"))
                {
                    suggestions.Add("Add colon ':' after key name in assignment");
                    suggestions.Add("Check for missing colon in object or assignment");
                }
                else if (message.Contains("']'"))
                {
                    suggestions.Add("Add closing bracket ']' to close array or section");
                    suggestions.Add("Check for unmatched opening bracket '['");
                }
                else if (message.Contains("'}'"))
                {
                    suggestions.Add("Add closing brace '}' to close object");
                    suggestions.Add("Check for unmatched opening brace '{'");
                }
                else if (message.Contains("')'"))
                {
                    suggestions.Add("Add closing parenthesis ')' to close expression");
                    suggestions.Add("Check for unmatched opening parenthesis '('");
                }
                else if (message.Contains("'<'"))
                {
                    suggestions.Add("Add closing angle bracket '<' to close named object");
                    suggestions.Add("Check for unmatched opening angle bracket '>'");
                }
            }
            else if (message.Contains("Unexpected token"))
            {
                if (token != null)
                {
                    suggestions.Add($"Remove or replace the unexpected '{token.Lexeme}' token");
                    
                    switch (token.Type)
                    {
                        case TokenType.Semicolon:
                            suggestions.Add("Semicolon is optional in TuskTsk - consider removing it");
                            break;
                        case TokenType.Comma:
                            suggestions.Add("Check if comma is needed here (arrays, object properties)");
                            break;
                    }
                }
                
                suggestions.Add("Check the TuskTsk syntax reference for correct usage");
                suggestions.Add("Verify that all brackets, braces, and parentheses are properly matched");
            }
            
            return suggestions;
        }
        
        /// <summary>
        /// Get semantic error suggestions
        /// </summary>
        private List<string> GetSemanticErrorSuggestions(string message, SemanticErrorType errorType)
        {
            var suggestions = new List<string>();
            
            switch (errorType)
            {
                case SemanticErrorType.UndefinedVariable:
                    suggestions.Add("Check variable name spelling");
                    suggestions.Add("Ensure variable is defined before use");
                    suggestions.Add("Check if you meant to use a global variable with '$' prefix");
                    break;
                    
                case SemanticErrorType.TypeMismatch:
                    suggestions.Add("Check data types in the expression");
                    suggestions.Add("Consider explicit type conversion if needed");
                    suggestions.Add("Verify operator compatibility with operand types");
                    break;
                    
                case SemanticErrorType.InvalidIdentifier:
                    suggestions.Add("Use only letters, numbers, and underscores in identifiers");
                    suggestions.Add("Start identifiers with a letter or underscore");
                    suggestions.Add("Avoid reserved keywords as identifiers");
                    break;
                    
                case SemanticErrorType.WrongArgumentCount:
                    suggestions.Add("Check the function or operator documentation for correct argument count");
                    suggestions.Add("Add missing arguments or remove extra ones");
                    break;
                    
                case SemanticErrorType.InvalidPropertyAccess:
                    suggestions.Add("Ensure you're accessing properties on object types");
                    suggestions.Add("Check property name spelling");
                    break;
                    
                case SemanticErrorType.InvalidIndexAccess:
                    suggestions.Add("Use integer indices for arrays");
                    suggestions.Add("Use string keys for object property access");
                    suggestions.Add("Ensure the variable is an array or object");
                    break;
            }
            
            return suggestions;
        }
        
        /// <summary>
        /// Get warning suggestions
        /// </summary>
        private List<string> GetWarningSuggestions(string message, WarningType warningType)
        {
            var suggestions = new List<string>();
            
            switch (warningType)
            {
                case WarningType.UnusedVariable:
                    suggestions.Add("Remove unused variable declaration");
                    suggestions.Add("Use the variable somewhere in your configuration");
                    break;
                    
                case WarningType.DuplicateDefinition:
                    suggestions.Add("Remove duplicate definition");
                    suggestions.Add("Rename one of the conflicting items");
                    break;
                    
                case WarningType.ImplicitConversion:
                    suggestions.Add("Consider explicit type conversion for clarity");
                    suggestions.Add("Verify that implicit conversion produces expected results");
                    break;
                    
                case WarningType.DeprecatedSyntax:
                    suggestions.Add("Update to use modern TuskTsk syntax");
                    suggestions.Add("Check the latest documentation for recommended patterns");
                    break;
            }
            
            return suggestions;
        }
        
        /// <summary>
        /// Get semantic error severity
        /// </summary>
        private ErrorSeverity GetSemanticErrorSeverity(SemanticErrorType errorType)
        {
            return errorType switch
            {
                SemanticErrorType.UndefinedVariable => ErrorSeverity.Error,
                SemanticErrorType.TypeMismatch => ErrorSeverity.Error,
                SemanticErrorType.InvalidIdentifier => ErrorSeverity.Error,
                SemanticErrorType.WrongArgumentCount => ErrorSeverity.Error,
                SemanticErrorType.InvalidPropertyAccess => ErrorSeverity.Warning,
                SemanticErrorType.InvalidIndexAccess => ErrorSeverity.Error,
                SemanticErrorType.InternalError => ErrorSeverity.Fatal,
                _ => ErrorSeverity.Error
            };
        }
        
        /// <summary>
        /// Get semantic error code
        /// </summary>
        private string GetSemanticErrorCode(SemanticErrorType errorType)
        {
            return errorType switch
            {
                SemanticErrorType.UndefinedVariable => "SEM001",
                SemanticErrorType.TypeMismatch => "SEM002",
                SemanticErrorType.InvalidIdentifier => "SEM003",
                SemanticErrorType.WrongArgumentCount => "SEM004",
                SemanticErrorType.InvalidPropertyAccess => "SEM005",
                SemanticErrorType.InvalidIndexAccess => "SEM006",
                SemanticErrorType.InternalError => "SEM999",
                _ => "SEM000"
            };
        }
        
        /// <summary>
        /// Get warning code
        /// </summary>
        private string GetWarningCode(WarningType warningType)
        {
            return warningType switch
            {
                WarningType.UnusedVariable => "WARN001",
                WarningType.DuplicateDefinition => "WARN002",
                WarningType.ImplicitConversion => "WARN003",
                WarningType.DeprecatedSyntax => "WARN004",
                WarningType.PossibleTypo => "WARN005",
                WarningType.PerformanceIssue => "WARN006",
                _ => "WARN000"
            };
        }
        
        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            lock (_lock)
            {
                _errors?.Clear();
                _warnings?.Clear();
                _sourceLines?.Clear();
            }
        }
    }
    
    /// <summary>
    /// Parse error information
    /// </summary>
    public class TuskParseError
    {
        public ParseErrorType Type { get; set; }
        public string Message { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
        public ErrorSeverity Severity { get; set; }
        public string ErrorCode { get; set; }
        public ErrorContext Context { get; set; }
        public List<string> Suggestions { get; set; }
        public Token? Token { get; set; }
        
        public override string ToString()
        {
            return $"{Severity} {ErrorCode} at line {Line}, column {Column}: {Message}";
        }
    }
    
    /// <summary>
    /// Parse warning information
    /// </summary>
    public class TuskParseWarning
    {
        public WarningType Type { get; set; }
        public string Message { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
        public string WarningCode { get; set; }
        public ErrorContext Context { get; set; }
        public List<string> Suggestions { get; set; }
        
        public override string ToString()
        {
            return $"Warning {WarningCode} at line {Line}, column {Column}: {Message}";
        }
    }
    
    /// <summary>
    /// Error context information
    /// </summary>
    public class ErrorContext
    {
        public int Line { get; set; }
        public int Column { get; set; }
        public string SourceLine { get; set; }
        public Dictionary<int, string> ContextLines { get; set; }
    }
    
    /// <summary>
    /// Error reporting options
    /// </summary>
    public class ErrorReportingOptions
    {
        public bool IncludeContextLines { get; set; } = true;
        public int ContextLineCount { get; set; } = 2;
        public bool IncludeSuggestions { get; set; } = true;
        public bool GroupByType { get; set; } = false;
        public int MaxErrorsToReport { get; set; } = 100;
    }
    
    /// <summary>
    /// Parse error types
    /// </summary>
    public enum ParseErrorType
    {
        LexicalError,
        SyntaxError,
        SemanticError
    }
    
    /// <summary>
    /// Error severity levels
    /// </summary>
    public enum ErrorSeverity
    {
        Info,
        Warning,
        Error,
        Fatal
    }
    
    /// <summary>
    /// Warning types
    /// </summary>
    public enum WarningType
    {
        UnusedVariable,
        DuplicateDefinition,
        ImplicitConversion,
        DeprecatedSyntax,
        PossibleTypo,
        PerformanceIssue
    }
    
    /// <summary>
    /// Report output formats
    /// </summary>
    public enum ReportFormat
    {
        Text,
        Json,
        Xml,
        Markdown
    }
} 