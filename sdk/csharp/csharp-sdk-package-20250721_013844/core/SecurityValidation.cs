using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using System.Collections.Concurrent;

namespace TuskLang
{
    /// <summary>
    /// Advanced security and validation system for TuskLang C# SDK
    /// Implements input validation, security scanning, and threat detection
    /// </summary>
    public class SecurityValidation
    {
        private readonly InputValidator _validator;
        private readonly SecurityScanner _scanner;
        private readonly ThreatDetector _detector;
        private readonly ConcurrentDictionary<string, SecurityMetrics> _metrics;

        public SecurityValidation()
        {
            _validator = new InputValidator();
            _scanner = new SecurityScanner();
            _detector = new ThreatDetector();
            _metrics = new ConcurrentDictionary<string, SecurityMetrics>();
        }

        /// <summary>
        /// Execute operation with security validation
        /// </summary>
        public async Task<T> ExecuteWithSecurity<T>(
            Func<Task<T>> operation,
            Dictionary<string, object> inputs = null,
            string operationName = null,
            SecurityContext context = null)
        {
            var opName = operationName ?? operation.Method.Name;
            var securityContext = context ?? new SecurityContext();

            try
            {
                // Validate inputs
                if (inputs != null)
                {
                    var validationResult = await _validator.ValidateInputs(inputs, securityContext);
                    if (!validationResult.IsValid)
                    {
                        throw new SecurityValidationException($"Input validation failed: {validationResult.Errors}");
                    }
                }

                // Security scan
                var scanResult = await _scanner.ScanOperation(opName, inputs, securityContext);
                if (scanResult.ThreatLevel > ThreatLevel.Medium)
                {
                    throw new SecurityThreatException($"Security threat detected: {scanResult.Description}");
                }

                // Execute operation
                var result = await operation();

                // Validate output
                var outputValidation = await _validator.ValidateOutput(result, securityContext);
                if (!outputValidation.IsValid)
                {
                    throw new SecurityValidationException($"Output validation failed: {outputValidation.Errors}");
                }

                // Record security metrics
                RecordSecurityMetrics(opName, SecurityEvent.Success, scanResult.ThreatLevel);

                return result;
            }
            catch (Exception ex)
            {
                RecordSecurityMetrics(opName, SecurityEvent.Failure, ThreatLevel.High);
                throw;
            }
        }

        /// <summary>
        /// Validate and sanitize input data
        /// </summary>
        public async Task<ValidationResult> ValidateAndSanitize(
            Dictionary<string, object> inputs,
            SecurityContext context = null)
        {
            var securityContext = context ?? new SecurityContext();
            var result = await _validator.ValidateInputs(inputs, securityContext);
            
            if (result.IsValid)
            {
                result.SanitizedData = await _validator.SanitizeInputs(inputs, securityContext);
            }

            return result;
        }

        /// <summary>
        /// Check for security threats in operation
        /// </summary>
        public async Task<SecurityScanResult> ScanForThreats(
            string operationName,
            Dictionary<string, object> inputs = null,
            SecurityContext context = null)
        {
            var securityContext = context ?? new SecurityContext();
            return await _scanner.ScanOperation(operationName, inputs, securityContext);
        }

        /// <summary>
        /// Get security metrics
        /// </summary>
        public SecurityMetrics GetMetrics(string operationName = null)
        {
            if (operationName != null)
            {
                return _metrics.GetValueOrDefault(operationName, new SecurityMetrics());
            }

            // Aggregate all metrics
            var aggregated = new SecurityMetrics();
            foreach (var metric in _metrics.Values)
            {
                aggregated.TotalOperations += metric.TotalOperations;
                aggregated.SuccessfulOperations += metric.SuccessfulOperations;
                aggregated.FailedOperations += metric.FailedOperations;
                aggregated.HighThreatOperations += metric.HighThreatOperations;
                aggregated.MediumThreatOperations += metric.MediumThreatOperations;
                aggregated.LowThreatOperations += metric.LowThreatOperations;
            }

            return aggregated;
        }

        private void RecordSecurityMetrics(string operationName, SecurityEvent securityEvent, ThreatLevel threatLevel)
        {
            var metrics = _metrics.GetOrAdd(operationName, _ => new SecurityMetrics());
            metrics.RecordEvent(securityEvent, threatLevel);
        }
    }

    /// <summary>
    /// Input validation system
    /// </summary>
    public class InputValidator
    {
        private readonly Dictionary<string, ValidationRule> _rules;
        private readonly List<ISanitizer> _sanitizers;

        public InputValidator()
        {
            _rules = new Dictionary<string, ValidationRule>();
            _sanitizers = new List<ISanitizer>
            {
                new SqlInjectionSanitizer(),
                new XssSanitizer(),
                new PathTraversalSanitizer(),
                new CommandInjectionSanitizer()
            };
        }

        public async Task<ValidationResult> ValidateInputs(
            Dictionary<string, object> inputs,
            SecurityContext context)
        {
            var errors = new List<string>();

            foreach (var input in inputs)
            {
                var rule = GetValidationRule(input.Key, context);
                if (rule != null)
                {
                    var validation = await rule.Validate(input.Value);
                    if (!validation.IsValid)
                    {
                        errors.AddRange(validation.Errors);
                    }
                }
            }

            return new ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors
            };
        }

        public async Task<ValidationResult> ValidateOutput(object output, SecurityContext context)
        {
            // Basic output validation
            if (output == null)
            {
                return new ValidationResult { IsValid = true };
            }

            var errors = new List<string>();

            // Check for sensitive data exposure
            if (output.ToString().Contains("password") || output.ToString().Contains("token"))
            {
                errors.Add("Potential sensitive data exposure in output");
            }

            return new ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors
            };
        }

        public async Task<Dictionary<string, object>> SanitizeInputs(
            Dictionary<string, object> inputs,
            SecurityContext context)
        {
            var sanitized = new Dictionary<string, object>();

            foreach (var input in inputs)
            {
                var sanitizedValue = input.Value;
                foreach (var sanitizer in _sanitizers)
                {
                    sanitizedValue = await sanitizer.Sanitize(sanitizedValue);
                }
                sanitized[input.Key] = sanitizedValue;
            }

            return sanitized;
        }

        private ValidationRule GetValidationRule(string key, SecurityContext context)
        {
            if (_rules.ContainsKey(key))
            {
                return _rules[key];
            }

            // Default rules based on key patterns
            if (key.Contains("email"))
            {
                return new EmailValidationRule();
            }
            else if (key.Contains("url"))
            {
                return new UrlValidationRule();
            }
            else if (key.Contains("path"))
            {
                return new PathValidationRule();
            }

            return new StringValidationRule();
        }
    }

    /// <summary>
    /// Security scanning system
    /// </summary>
    public class SecurityScanner
    {
        private readonly List<ISecurityRule> _rules;
        private readonly List<string> _blacklistedPatterns;

        public SecurityScanner()
        {
            _rules = new List<ISecurityRule>
            {
                new SqlInjectionRule(),
                new XssRule(),
                new CommandInjectionRule(),
                new PathTraversalRule(),
                new SensitiveDataRule()
            };

            _blacklistedPatterns = new List<string>
            {
                @"<script[^>]*>.*?</script>",
                @"javascript:",
                @"vbscript:",
                @"on\w+\s*=",
                @"union\s+select",
                @"drop\s+table",
                @"delete\s+from",
                @"exec\s*\(",
                @"system\s*\(",
                @"eval\s*\("
            };
        }

        public async Task<SecurityScanResult> ScanOperation(
            string operationName,
            Dictionary<string, object> inputs,
            SecurityContext context)
        {
            var threats = new List<SecurityThreat>();
            var threatLevel = ThreatLevel.Low;

            // Scan inputs for threats
            if (inputs != null)
            {
                foreach (var input in inputs)
                {
                    var inputThreats = await ScanValue(input.Value, context);
                    threats.AddRange(inputThreats);
                }
            }

            // Apply security rules
            foreach (var rule in _rules)
            {
                var ruleThreats = await rule.Scan(operationName, inputs, context);
                threats.AddRange(ruleThreats);
            }

            // Determine overall threat level
            if (threats.Any(t => t.Level == ThreatLevel.High))
            {
                threatLevel = ThreatLevel.High;
            }
            else if (threats.Any(t => t.Level == ThreatLevel.Medium))
            {
                threatLevel = ThreatLevel.Medium;
            }

            return new SecurityScanResult
            {
                ThreatLevel = threatLevel,
                Threats = threats,
                Description = string.Join("; ", threats.Select(t => t.Description))
            };
        }

        private async Task<List<SecurityThreat>> ScanValue(object value, SecurityContext context)
        {
            var threats = new List<SecurityThreat>();
            var stringValue = value?.ToString() ?? "";

            foreach (var pattern in _blacklistedPatterns)
            {
                if (Regex.IsMatch(stringValue, pattern, RegexOptions.IgnoreCase))
                {
                    threats.Add(new SecurityThreat
                    {
                        Type = ThreatType.MaliciousPattern,
                        Level = ThreatLevel.High,
                        Description = $"Malicious pattern detected: {pattern}"
                    });
                }
            }

            return threats;
        }
    }

    /// <summary>
    /// Threat detection system
    /// </summary>
    public class ThreatDetector
    {
        private readonly Dictionary<string, int> _threatCounts;
        private readonly Dictionary<string, DateTime> _lastThreats;

        public ThreatDetector()
        {
            _threatCounts = new Dictionary<string, int>();
            _lastThreats = new Dictionary<string, DateTime>();
        }

        public bool IsUnderAttack(string operationName, ThreatLevel threatLevel)
        {
            var key = $"{operationName}_{threatLevel}";
            var count = _threatCounts.GetValueOrDefault(key, 0);
            var lastThreat = _lastThreats.GetValueOrDefault(key, DateTime.MinValue);

            // Consider it an attack if more than 10 threats in the last minute
            return count > 10 && DateTime.UtcNow - lastThreat < TimeSpan.FromMinutes(1);
        }

        public void RecordThreat(string operationName, ThreatLevel threatLevel)
        {
            var key = $"{operationName}_{threatLevel}";
            _threatCounts[key] = _threatCounts.GetValueOrDefault(key, 0) + 1;
            _lastThreats[key] = DateTime.UtcNow;
        }
    }

    public class SecurityContext
    {
        public string UserId { get; set; }
        public string SessionId { get; set; }
        public string IpAddress { get; set; }
        public Dictionary<string, object> Permissions { get; set; }
        public bool IsAuthenticated { get; set; }
        public SecurityLevel RequiredLevel { get; set; }

        public SecurityContext()
        {
            Permissions = new Dictionary<string, object>();
            RequiredLevel = SecurityLevel.Standard;
        }
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public Dictionary<string, object> SanitizedData { get; set; }
    }

    public class SecurityScanResult
    {
        public ThreatLevel ThreatLevel { get; set; }
        public List<SecurityThreat> Threats { get; set; } = new List<SecurityThreat>();
        public string Description { get; set; }
    }

    public class SecurityThreat
    {
        public ThreatType Type { get; set; }
        public ThreatLevel Level { get; set; }
        public string Description { get; set; }
    }

    public class SecurityMetrics
    {
        public int TotalOperations { get; set; }
        public int SuccessfulOperations { get; set; }
        public int FailedOperations { get; set; }
        public int HighThreatOperations { get; set; }
        public int MediumThreatOperations { get; set; }
        public int LowThreatOperations { get; set; }

        public void RecordEvent(SecurityEvent securityEvent, ThreatLevel threatLevel)
        {
            TotalOperations++;
            
            if (securityEvent == SecurityEvent.Success)
            {
                SuccessfulOperations++;
            }
            else
            {
                FailedOperations++;
            }

            switch (threatLevel)
            {
                case ThreatLevel.High:
                    HighThreatOperations++;
                    break;
                case ThreatLevel.Medium:
                    MediumThreatOperations++;
                    break;
                case ThreatLevel.Low:
                    LowThreatOperations++;
                    break;
            }
        }
    }

    public enum ThreatLevel
    {
        Low,
        Medium,
        High,
        Critical
    }

    public enum ThreatType
    {
        SqlInjection,
        Xss,
        CommandInjection,
        PathTraversal,
        SensitiveDataExposure,
        MaliciousPattern,
        AuthenticationBypass,
        AuthorizationViolation
    }

    public enum SecurityEvent
    {
        Success,
        Failure,
        ThreatDetected,
        ValidationFailed
    }

    public enum SecurityLevel
    {
        Low,
        Standard,
        High,
        Critical
    }

    // Interfaces and base classes
    public abstract class ValidationRule
    {
        public abstract Task<ValidationResult> Validate(object value);
    }

    public interface ISanitizer
    {
        Task<object> Sanitize(object value);
    }

    public interface ISecurityRule
    {
        Task<List<SecurityThreat>> Scan(string operationName, Dictionary<string, object> inputs, SecurityContext context);
    }

    // Concrete implementations
    public class StringValidationRule : ValidationRule
    {
        public override async Task<ValidationResult> Validate(object value)
        {
            var stringValue = value?.ToString() ?? "";
            
            if (stringValue.Length > 1000)
            {
                return new ValidationResult
                {
                    IsValid = false,
                    Errors = { "String value too long (max 1000 characters)" }
                };
            }

            return new ValidationResult { IsValid = true };
        }
    }

    public class EmailValidationRule : ValidationRule
    {
        public override async Task<ValidationResult> Validate(object value)
        {
            var email = value?.ToString() ?? "";
            var emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            
            if (!Regex.IsMatch(email, emailPattern))
            {
                return new ValidationResult
                {
                    IsValid = false,
                    Errors = { "Invalid email format" }
                };
            }

            return new ValidationResult { IsValid = true };
        }
    }

    public class UrlValidationRule : ValidationRule
    {
        public override async Task<ValidationResult> Validate(object value)
        {
            var url = value?.ToString() ?? "";
            
            if (!Uri.TryCreate(url, UriKind.Absolute, out _))
            {
                return new ValidationResult
                {
                    IsValid = false,
                    Errors = { "Invalid URL format" }
                };
            }

            return new ValidationResult { IsValid = true };
        }
    }

    public class PathValidationRule : ValidationRule
    {
        public override async Task<ValidationResult> Validate(object value)
        {
            var path = value?.ToString() ?? "";
            
            if (path.Contains("..") || path.Contains("//"))
            {
                return new ValidationResult
                {
                    IsValid = false,
                    Errors = { "Path traversal attempt detected" }
                };
            }

            return new ValidationResult { IsValid = true };
        }
    }

    public class SqlInjectionSanitizer : ISanitizer
    {
        public async Task<object> Sanitize(object value)
        {
            var stringValue = value?.ToString() ?? "";
            return stringValue.Replace("'", "''").Replace(";", "");
        }
    }

    public class XssSanitizer : ISanitizer
    {
        public async Task<object> Sanitize(object value)
        {
            var stringValue = value?.ToString() ?? "";
            return stringValue.Replace("<", "&lt;").Replace(">", "&gt;");
        }
    }

    public class PathTraversalSanitizer : ISanitizer
    {
        public async Task<object> Sanitize(object value)
        {
            var stringValue = value?.ToString() ?? "";
            return stringValue.Replace("..", "").Replace("//", "/");
        }
    }

    public class CommandInjectionSanitizer : ISanitizer
    {
        public async Task<object> Sanitize(object value)
        {
            var stringValue = value?.ToString() ?? "";
            return stringValue.Replace(";", "").Replace("|", "").Replace("&", "");
        }
    }

    public class SqlInjectionRule : ISecurityRule
    {
        public async Task<List<SecurityThreat>> Scan(string operationName, Dictionary<string, object> inputs, SecurityContext context)
        {
            var threats = new List<SecurityThreat>();
            var sqlPatterns = new[] { "union select", "drop table", "delete from", "insert into" };

            foreach (var input in inputs?.Values ?? new object[0])
            {
                var stringValue = input?.ToString() ?? "";
                foreach (var pattern in sqlPatterns)
                {
                    if (stringValue.ToLower().Contains(pattern))
                    {
                        threats.Add(new SecurityThreat
                        {
                            Type = ThreatType.SqlInjection,
                            Level = ThreatLevel.High,
                            Description = $"SQL injection attempt detected: {pattern}"
                        });
                    }
                }
            }

            return threats;
        }
    }

    public class XssRule : ISecurityRule
    {
        public async Task<List<SecurityThreat>> Scan(string operationName, Dictionary<string, object> inputs, SecurityContext context)
        {
            var threats = new List<SecurityThreat>();
            var xssPatterns = new[] { "<script", "javascript:", "onclick", "onload" };

            foreach (var input in inputs?.Values ?? new object[0])
            {
                var stringValue = input?.ToString() ?? "";
                foreach (var pattern in xssPatterns)
                {
                    if (stringValue.ToLower().Contains(pattern))
                    {
                        threats.Add(new SecurityThreat
                        {
                            Type = ThreatType.Xss,
                            Level = ThreatLevel.High,
                            Description = $"XSS attempt detected: {pattern}"
                        });
                    }
                }
            }

            return threats;
        }
    }

    public class CommandInjectionRule : ISecurityRule
    {
        public async Task<List<SecurityThreat>> Scan(string operationName, Dictionary<string, object> inputs, SecurityContext context)
        {
            var threats = new List<SecurityThreat>();
            var commandPatterns = new[] { ";", "|", "&", "`", "$(" };

            foreach (var input in inputs?.Values ?? new object[0])
            {
                var stringValue = input?.ToString() ?? "";
                foreach (var pattern in commandPatterns)
                {
                    if (stringValue.Contains(pattern))
                    {
                        threats.Add(new SecurityThreat
                        {
                            Type = ThreatType.CommandInjection,
                            Level = ThreatLevel.High,
                            Description = $"Command injection attempt detected: {pattern}"
                        });
                    }
                }
            }

            return threats;
        }
    }

    public class PathTraversalRule : ISecurityRule
    {
        public async Task<List<SecurityThreat>> Scan(string operationName, Dictionary<string, object> inputs, SecurityContext context)
        {
            var threats = new List<SecurityThreat>();

            foreach (var input in inputs?.Values ?? new object[0])
            {
                var stringValue = input?.ToString() ?? "";
                if (stringValue.Contains("..") || stringValue.Contains("//"))
                {
                    threats.Add(new SecurityThreat
                    {
                        Type = ThreatType.PathTraversal,
                        Level = ThreatLevel.Medium,
                        Description = "Path traversal attempt detected"
                    });
                }
            }

            return threats;
        }
    }

    public class SensitiveDataRule : ISecurityRule
    {
        public async Task<List<SecurityThreat>> Scan(string operationName, Dictionary<string, object> inputs, SecurityContext context)
        {
            var threats = new List<SecurityThreat>();
            var sensitivePatterns = new[] { "password", "token", "secret", "key", "credential" };

            foreach (var input in inputs ?? new Dictionary<string, object>())
            {
                var key = input.Key.ToLower();
                foreach (var pattern in sensitivePatterns)
                {
                    if (key.Contains(pattern))
                    {
                        threats.Add(new SecurityThreat
                        {
                            Type = ThreatType.SensitiveDataExposure,
                            Level = ThreatLevel.Medium,
                            Description = $"Sensitive data detected in key: {input.Key}"
                        });
                    }
                }
            }

            return threats;
        }
    }

    public class SecurityValidationException : Exception
    {
        public SecurityValidationException(string message) : base(message) { }
    }

    public class SecurityThreatException : Exception
    {
        public SecurityThreatException(string message) : base(message) { }
    }
} 