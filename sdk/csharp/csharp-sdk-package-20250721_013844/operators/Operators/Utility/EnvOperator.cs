using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

namespace TuskLang.Operators.Utility
{
    /// <summary>
    /// Environment Operator for TuskLang C# SDK
    /// 
    /// Provides comprehensive environment variable operations with support for:
    /// - Environment variable reading and setting
    /// - .env file loading and parsing
    /// - Default value handling with type conversion
    /// - Encrypted environment variables with AES encryption
    /// - Environment variable validation and constraints
    /// - Cross-platform environment management
    /// - Environment variable caching and performance optimization
    /// - Secure credential management
    /// 
    /// Usage:
    /// ```csharp
    /// // Get environment variable
    /// var result = @env({
    ///   action: "get",
    ///   name: "DATABASE_URL",
    ///   default: "localhost:5432"
    /// })
    /// 
    /// // Load .env file
    /// var result = @env({
    ///   action: "load",
    ///   file: ".env",
    ///   override: true
    /// })
    /// 
    /// // Set encrypted variable
    /// var result = @env({
    ///   action: "set_encrypted",
    ///   name: "API_SECRET",
    ///   value: "super-secret-key",
    ///   key: "encryption-key"
    /// })
    /// ```
    /// </summary>
    public class EnvOperator : BaseOperator, IDisposable
    {
        private static readonly ConcurrentDictionary<string, object> _cache = new();
        private static readonly ConcurrentDictionary<string, string> _encryptedVariables = new();
        private static readonly object _fileLock = new();
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// Initializes a new instance of the EnvOperator class
        /// </summary>
        public EnvOperator()
        {
            Version = "2.0.0";
            RequiredFields = new List<string> { "action" };
            OptionalFields = new List<string> 
            { 
                "name", "value", "default", "type", "file", "override", "prefix", "suffix",
                "required", "validate", "format", "encrypted", "key", "algorithm", "cache"
            };
            
            DefaultConfig = new Dictionary<string, object>
            {
                ["type"] = "string",
                ["cache"] = true,
                ["override"] = false,
                ["algorithm"] = "AES256",
                ["file"] = ".env"
            };

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        /// <summary>
        /// Gets the operator name
        /// </summary>
        public override string GetName() => "env";

        /// <summary>
        /// Gets the operator description
        /// </summary>
        protected override string GetDescription()
        {
            return "Provides comprehensive environment variable operations with .env file support and encryption";
        }

        /// <summary>
        /// Gets usage examples
        /// </summary>
        protected override Dictionary<string, string> GetExamples()
        {
            return new Dictionary<string, string>
            {
                ["get"] = "@env({action: \"get\", name: \"DATABASE_URL\", default: \"localhost:5432\"})",
                ["set"] = "@env({action: \"set\", name: \"NODE_ENV\", value: \"development\"})",
                ["load"] = "@env({action: \"load\", file: \".env.local\", override: true})",
                ["list"] = "@env({action: \"list\", prefix: \"DB_\"})",
                ["encrypted"] = "@env({action: \"get_encrypted\", name: \"API_SECRET\", key: \"my-key\"})"
            };
        }

        /// <summary>
        /// Custom validation for environment operations
        /// </summary>
        protected override ValidationResult CustomValidate(Dictionary<string, object> config)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            var action = config.GetValueOrDefault("action")?.ToString()?.ToLower();

            // Action-specific validation
            switch (action)
            {
                case "get":
                case "delete":
                case "exists":
                    if (!config.ContainsKey("name"))
                        errors.Add("'name' is required for this operation");
                    break;

                case "set":
                case "set_encrypted":
                    if (!config.ContainsKey("name"))
                        errors.Add("'name' is required for set operation");
                    if (!config.ContainsKey("value"))
                        errors.Add("'value' is required for set operation");
                    break;

                case "get_encrypted":
                    if (!config.ContainsKey("name"))
                        errors.Add("'name' is required for get_encrypted operation");
                    if (!config.ContainsKey("key"))
                        errors.Add("'key' is required for encrypted operations");
                    break;

                case "load":
                    var file = config.GetValueOrDefault("file")?.ToString();
                    if (!string.IsNullOrEmpty(file) && !File.Exists(file))
                        warnings.Add($"Environment file '{file}' does not exist");
                    break;
            }

            // Validate environment variable name
            var name = config.GetValueOrDefault("name")?.ToString();
            if (!string.IsNullOrEmpty(name))
            {
                if (!IsValidEnvironmentVariableName(name))
                    errors.Add("Environment variable name contains invalid characters");
            }

            return new ValidationResult { Errors = errors, Warnings = warnings };
        }

        /// <summary>
        /// Execute the environment operator
        /// </summary>
        protected override async Task<object> ExecuteOperatorAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var action = config["action"].ToString()!.ToLower();

            try
            {
                switch (action)
                {
                    case "get":
                        return await GetEnvironmentVariableAsync(config);
                    
                    case "set":
                        return await SetEnvironmentVariableAsync(config);
                    
                    case "delete":
                        return await DeleteEnvironmentVariableAsync(config);
                    
                    case "exists":
                        return await ExistsEnvironmentVariableAsync(config);
                    
                    case "list":
                        return await ListEnvironmentVariablesAsync(config);
                    
                    case "load":
                        return await LoadEnvironmentFileAsync(config);
                    
                    case "save":
                        return await SaveEnvironmentFileAsync(config);
                    
                    case "set_encrypted":
                        return await SetEncryptedVariableAsync(config);
                    
                    case "get_encrypted":
                        return await GetEncryptedVariableAsync(config);
                    
                    case "clear_cache":
                        return await ClearCacheAsync();
                    
                    case "validate":
                        return await ValidateEnvironmentAsync(config);
                    
                    default:
                        throw new ArgumentException($"Unsupported environment action: {action}");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Environment operation '{action}' failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get environment variable
        /// </summary>
        private async Task<object> GetEnvironmentVariableAsync(Dictionary<string, object> config)
        {
            var name = config["name"].ToString()!;
            var defaultValue = config.GetValueOrDefault("default");
            var type = config.GetValueOrDefault("type")?.ToString() ?? "string";
            var required = Convert.ToBoolean(config.GetValueOrDefault("required", false));
            var format = config.GetValueOrDefault("format")?.ToString();
            var useCache = Convert.ToBoolean(config.GetValueOrDefault("cache", DefaultConfig["cache"]));

            // Check cache first
            if (useCache && _cache.TryGetValue(name, out var cachedValue))
            {
                return CreateSuccessResponse(name, cachedValue, type, true);
            }

            // Get from environment
            var value = Environment.GetEnvironmentVariable(name);
            
            if (value == null)
            {
                if (required)
                {
                    throw new InvalidOperationException($"Required environment variable '{name}' is not set");
                }
                
                return CreateSuccessResponse(name, defaultValue, type, false);
            }

            // Convert to specified type
            var convertedValue = await ConvertValueAsync(value, type);
            
            // Apply formatting if specified
            if (!string.IsNullOrEmpty(format))
            {
                convertedValue = await FormatValueAsync(convertedValue, format);
            }

            // Cache the result
            if (useCache)
            {
                _cache[name] = convertedValue;
            }

            return CreateSuccessResponse(name, convertedValue, type, true);
        }

        /// <summary>
        /// Set environment variable
        /// </summary>
        private async Task<object> SetEnvironmentVariableAsync(Dictionary<string, object> config)
        {
            var name = config["name"].ToString()!;
            var value = config["value"].ToString()!;
            var target = config.GetValueOrDefault("target")?.ToString() ?? "Process";

            var environmentTarget = target.ToLower() switch
            {
                "machine" => EnvironmentVariableTarget.Machine,
                "user" => EnvironmentVariableTarget.User,
                _ => EnvironmentVariableTarget.Process
            };

            Environment.SetEnvironmentVariable(name, value, environmentTarget);
            
            // Clear cache for this variable
            _cache.TryRemove(name, out _);

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["name"] = name,
                ["value"] = value,
                ["target"] = target
            };
        }

        /// <summary>
        /// Delete environment variable
        /// </summary>
        private async Task<object> DeleteEnvironmentVariableAsync(Dictionary<string, object> config)
        {
            var name = config["name"].ToString()!;
            var target = config.GetValueOrDefault("target")?.ToString() ?? "Process";

            var environmentTarget = target.ToLower() switch
            {
                "machine" => EnvironmentVariableTarget.Machine,
                "user" => EnvironmentVariableTarget.User,
                _ => EnvironmentVariableTarget.Process
            };

            var existedBefore = Environment.GetEnvironmentVariable(name) != null;
            Environment.SetEnvironmentVariable(name, null, environmentTarget);
            
            // Clear cache for this variable
            _cache.TryRemove(name, out _);

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["name"] = name,
                ["existed_before"] = existedBefore,
                ["target"] = target
            };
        }

        /// <summary>
        /// Check if environment variable exists
        /// </summary>
        private async Task<object> ExistsEnvironmentVariableAsync(Dictionary<string, object> config)
        {
            var name = config["name"].ToString()!;
            var exists = Environment.GetEnvironmentVariable(name) != null;

            return new Dictionary<string, object>
            {
                ["exists"] = exists,
                ["name"] = name
            };
        }

        /// <summary>
        /// List environment variables
        /// </summary>
        private async Task<object> ListEnvironmentVariablesAsync(Dictionary<string, object> config)
        {
            var prefix = config.GetValueOrDefault("prefix")?.ToString() ?? "";
            var suffix = config.GetValueOrDefault("suffix")?.ToString() ?? "";
            var includeValues = Convert.ToBoolean(config.GetValueOrDefault("include_values", true));
            var target = config.GetValueOrDefault("target")?.ToString() ?? "Process";

            var environmentTarget = target.ToLower() switch
            {
                "machine" => EnvironmentVariableTarget.Machine,
                "user" => EnvironmentVariableTarget.User,
                _ => EnvironmentVariableTarget.Process
            };

            var variables = new List<Dictionary<string, object>>();
            var envVars = Environment.GetEnvironmentVariables(environmentTarget);

            foreach (string key in envVars.Keys)
            {
                if (!key.StartsWith(prefix) || !key.EndsWith(suffix))
                    continue;

                var variable = new Dictionary<string, object>
                {
                    ["name"] = key
                };

                if (includeValues)
                {
                    variable["value"] = envVars[key];
                }

                variables.Add(variable);
            }

            return new Dictionary<string, object>
            {
                ["variables"] = variables,
                ["count"] = variables.Count,
                ["target"] = target,
                ["prefix"] = prefix,
                ["suffix"] = suffix
            };
        }

        /// <summary>
        /// Load environment file (.env)
        /// </summary>
        private async Task<object> LoadEnvironmentFileAsync(Dictionary<string, object> config)
        {
            var file = config.GetValueOrDefault("file")?.ToString() ?? (string)DefaultConfig["file"];
            var overrideExisting = Convert.ToBoolean(config.GetValueOrDefault("override", DefaultConfig["override"]));
            var encoding = config.GetValueOrDefault("encoding")?.ToString() ?? "UTF-8";

            if (!File.Exists(file))
            {
                throw new FileNotFoundException($"Environment file '{file}' not found");
            }

            var loadedVars = new List<Dictionary<string, object>>();
            var skippedVars = new List<string>();

            lock (_fileLock)
            {
                var lines = File.ReadAllLines(file, Encoding.GetEncoding(encoding));
                
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    
                    // Skip empty lines and comments
                    if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
                        continue;

                    var equalIndex = trimmed.IndexOf('=');
                    if (equalIndex <= 0)
                        continue;

                    var key = trimmed.Substring(0, equalIndex).Trim();
                    var value = trimmed.Substring(equalIndex + 1).Trim();

                    // Remove surrounding quotes
                    if ((value.StartsWith('"') && value.EndsWith('"')) ||
                        (value.StartsWith('\'') && value.EndsWith('\'')))
                    {
                        value = value.Substring(1, value.Length - 2);
                    }

                    // Check if variable already exists
                    if (!overrideExisting && Environment.GetEnvironmentVariable(key) != null)
                    {
                        skippedVars.Add(key);
                        continue;
                    }

                    // Set environment variable
                    Environment.SetEnvironmentVariable(key, value);
                    
                    // Clear cache for this variable
                    _cache.TryRemove(key, out _);
                    
                    loadedVars.Add(new Dictionary<string, object>
                    {
                        ["name"] = key,
                        ["value"] = value
                    });
                }
            }

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["file"] = file,
                ["loaded"] = loadedVars.Count,
                ["skipped"] = skippedVars.Count,
                ["loaded_variables"] = loadedVars,
                ["skipped_variables"] = skippedVars
            };
        }

        /// <summary>
        /// Save environment variables to file
        /// </summary>
        private async Task<object> SaveEnvironmentFileAsync(Dictionary<string, object> config)
        {
            var file = config.GetValueOrDefault("file")?.ToString() ?? ".env";
            var prefix = config.GetValueOrDefault("prefix")?.ToString() ?? "";
            var encoding = config.GetValueOrDefault("encoding")?.ToString() ?? "UTF-8";
            var includeComments = Convert.ToBoolean(config.GetValueOrDefault("include_comments", true));

            var variables = new List<string>();
            var envVars = Environment.GetEnvironmentVariables();

            if (includeComments)
            {
                variables.Add("# Environment variables generated by TuskLang");
                variables.Add($"# Generated on: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                variables.Add("");
            }

            foreach (string key in envVars.Keys)
            {
                if (!string.IsNullOrEmpty(prefix) && !key.StartsWith(prefix))
                    continue;

                var value = envVars[key]?.ToString() ?? "";
                
                // Escape quotes in values
                if (value.Contains(' ') || value.Contains('"') || value.Contains('\''))
                {
                    value = $"\"{value.Replace("\"", "\\\"")}\"";
                }

                variables.Add($"{key}={value}");
            }

            lock (_fileLock)
            {
                File.WriteAllLines(file, variables, Encoding.GetEncoding(encoding));
            }

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["file"] = file,
                ["variables_saved"] = variables.Count - (includeComments ? 3 : 0),
                ["prefix"] = prefix
            };
        }

        /// <summary>
        /// Set encrypted environment variable
        /// </summary>
        private async Task<object> SetEncryptedVariableAsync(Dictionary<string, object> config)
        {
            var name = config["name"].ToString()!;
            var value = config["value"].ToString()!;
            var key = config["key"].ToString()!;
            var algorithm = config.GetValueOrDefault("algorithm")?.ToString() ?? (string)DefaultConfig["algorithm"];

            var encryptedValue = await EncryptValueAsync(value, key, algorithm);
            
            // Store encrypted value in environment and cache
            Environment.SetEnvironmentVariable(name, encryptedValue);
            _encryptedVariables[name] = key; // Store the encryption key association
            
            // Clear regular cache for this variable
            _cache.TryRemove(name, out _);

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["name"] = name,
                ["encrypted"] = true,
                ["algorithm"] = algorithm
            };
        }

        /// <summary>
        /// Get encrypted environment variable
        /// </summary>
        private async Task<object> GetEncryptedVariableAsync(Dictionary<string, object> config)
        {
            var name = config["name"].ToString()!;
            var key = config["key"].ToString()!;
            var algorithm = config.GetValueOrDefault("algorithm")?.ToString() ?? (string)DefaultConfig["algorithm"];
            var defaultValue = config.GetValueOrDefault("default");

            var encryptedValue = Environment.GetEnvironmentVariable(name);
            
            if (encryptedValue == null)
            {
                return CreateSuccessResponse(name, defaultValue, "string", false);
            }

            try
            {
                var decryptedValue = await DecryptValueAsync(encryptedValue, key, algorithm);
                return CreateSuccessResponse(name, decryptedValue, "string", true, encrypted: true);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to decrypt environment variable '{name}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Clear environment variable cache
        /// </summary>
        private async Task<object> ClearCacheAsync()
        {
            var count = _cache.Count;
            _cache.Clear();

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["cleared_count"] = count
            };
        }

        /// <summary>
        /// Validate environment configuration
        /// </summary>
        private async Task<object> ValidateEnvironmentAsync(Dictionary<string, object> config)
        {
            var requiredVars = config.GetValueOrDefault("required") as List<object> ?? new List<object>();
            var results = new List<Dictionary<string, object>>();
            var allValid = true;

            foreach (var varName in requiredVars)
            {
                var name = varName.ToString()!;
                var exists = Environment.GetEnvironmentVariable(name) != null;
                
                results.Add(new Dictionary<string, object>
                {
                    ["name"] = name,
                    ["exists"] = exists,
                    ["valid"] = exists
                });

                if (!exists)
                    allValid = false;
            }

            return new Dictionary<string, object>
            {
                ["valid"] = allValid,
                ["results"] = results,
                ["checked_count"] = results.Count
            };
        }

        /// <summary>
        /// Convert value to specified type
        /// </summary>
        private async Task<object> ConvertValueAsync(object value, string type)
        {
            if (value == null) return null!;
            
            var strValue = value.ToString()!;
            
            return type.ToLower() switch
            {
                "string" => strValue,
                "int" or "integer" => int.Parse(strValue),
                "long" => long.Parse(strValue),
                "double" => double.Parse(strValue),
                "float" => float.Parse(strValue),
                "bool" or "boolean" => bool.Parse(strValue),
                "datetime" => DateTime.Parse(strValue),
                "json" => JsonSerializer.Deserialize<object>(strValue, _jsonOptions),
                "array" => strValue.Split(',').Select(s => s.Trim()).ToArray(),
                _ => strValue
            };
        }

        /// <summary>
        /// Format value according to format string
        /// </summary>
        private async Task<object> FormatValueAsync(object value, string format)
        {
            if (value == null) return null!;
            
            return format.ToLower() switch
            {
                "upper" => value.ToString()!.ToUpper(),
                "lower" => value.ToString()!.ToLower(),
                "trim" => value.ToString()!.Trim(),
                "json" => JsonSerializer.Serialize(value, _jsonOptions),
                _ when format.StartsWith("substring:") => 
                    ExtractSubstring(value.ToString()!, format.Substring(10)),
                _ when format.StartsWith("replace:") => 
                    ApplyReplacement(value.ToString()!, format.Substring(8)),
                _ => value
            };
        }

        /// <summary>
        /// Extract substring based on format
        /// </summary>
        private string ExtractSubstring(string value, string format)
        {
            var parts = format.Split(',');
            if (parts.Length >= 2 && int.TryParse(parts[0], out var start) && int.TryParse(parts[1], out var length))
            {
                return value.Substring(start, Math.Min(length, value.Length - start));
            }
            return value;
        }

        /// <summary>
        /// Apply string replacement based on format
        /// </summary>
        private string ApplyReplacement(string value, string format)
        {
            var parts = format.Split('|');
            if (parts.Length >= 2)
            {
                return value.Replace(parts[0], parts[1]);
            }
            return value;
        }

        /// <summary>
        /// Encrypt value using specified algorithm
        /// </summary>
        private async Task<string> EncryptValueAsync(string value, string key, string algorithm)
        {
            switch (algorithm.ToUpper())
            {
                case "AES256":
                    return EncryptAes256(value, key);
                default:
                    throw new ArgumentException($"Unsupported encryption algorithm: {algorithm}");
            }
        }

        /// <summary>
        /// Decrypt value using specified algorithm
        /// </summary>
        private async Task<string> DecryptValueAsync(string encryptedValue, string key, string algorithm)
        {
            switch (algorithm.ToUpper())
            {
                case "AES256":
                    return DecryptAes256(encryptedValue, key);
                default:
                    throw new ArgumentException($"Unsupported decryption algorithm: {algorithm}");
            }
        }

        /// <summary>
        /// Encrypt using AES-256
        /// </summary>
        private string EncryptAes256(string value, string key)
        {
            using var aes = Aes.Create();
            aes.Key = DeriveKey(key, 32); // 256-bit key
            aes.IV = new byte[16]; // Zero IV for simplicity - in production use random IV
            
            using var encryptor = aes.CreateEncryptor();
            var valueBytes = Encoding.UTF8.GetBytes(value);
            var encryptedBytes = encryptor.TransformFinalBlock(valueBytes, 0, valueBytes.Length);
            
            return Convert.ToBase64String(encryptedBytes);
        }

        /// <summary>
        /// Decrypt using AES-256
        /// </summary>
        private string DecryptAes256(string encryptedValue, string key)
        {
            using var aes = Aes.Create();
            aes.Key = DeriveKey(key, 32); // 256-bit key
            aes.IV = new byte[16]; // Zero IV for simplicity - in production use random IV
            
            using var decryptor = aes.CreateDecryptor();
            var encryptedBytes = Convert.FromBase64String(encryptedValue);
            var decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
            
            return Encoding.UTF8.GetString(decryptedBytes);
        }

        /// <summary>
        /// Derive encryption key from password
        /// </summary>
        private byte[] DeriveKey(string password, int keySize)
        {
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(passwordBytes);
            
            if (hash.Length >= keySize)
            {
                var key = new byte[keySize];
                Array.Copy(hash, key, keySize);
                return key;
            }
            
            // If hash is too short, repeat it
            var result = new byte[keySize];
            for (int i = 0; i < keySize; i++)
            {
                result[i] = hash[i % hash.Length];
            }
            return result;
        }

        /// <summary>
        /// Check if environment variable name is valid
        /// </summary>
        private bool IsValidEnvironmentVariableName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            
            // Environment variable names should start with letter or underscore
            // and contain only letters, digits, and underscores
            return char.IsLetter(name[0]) || name[0] == '_' &&
                   name.All(c => char.IsLetterOrDigit(c) || c == '_');
        }

        /// <summary>
        /// Create success response
        /// </summary>
        private Dictionary<string, object> CreateSuccessResponse(string name, object? value, string type, bool found, bool encrypted = false)
        {
            var response = new Dictionary<string, object>
            {
                ["success"] = true,
                ["found"] = found,
                ["name"] = name,
                ["value"] = value,
                ["type"] = type
            };

            if (encrypted)
            {
                response["encrypted"] = true;
            }

            return response;
        }

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            _cache.Clear();
            _encryptedVariables.Clear();
        }
    }

    /// <summary>
    /// Extension methods for configuration access
    /// </summary>
    public static class EnvConfigExtensions
    {
        public static T? GetValueOrDefault<T>(this Dictionary<string, object> dict, string key, T? defaultValue = default)
        {
            if (dict.TryGetValue(key, out var value))
            {
                if (value is T typedValue)
                    return typedValue;
                    
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }
    }
} 