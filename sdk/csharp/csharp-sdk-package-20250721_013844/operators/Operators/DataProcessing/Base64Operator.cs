using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;

namespace TuskLang.Operators.DataProcessing
{
    /// <summary>
    /// Base64 Operator for TuskLang C# SDK
    /// 
    /// Provides Base64 encoding and decoding capabilities with support for:
    /// - Base64 encoding of strings and binary data
    /// - Base64 decoding to strings and binary data
    /// - URL-safe Base64 encoding/decoding
    /// - Base64 validation and formatting
    /// - Base64 to binary conversion
    /// - Binary to Base64 conversion
    /// 
    /// Usage:
    /// ```csharp
    /// // Encode string
    /// var result = @base64({
    ///   action: "encode",
    ///   data: "Hello, World!"
    /// })
    /// 
    /// // Decode Base64
    /// var result = @base64({
    ///   action: "decode",
    ///   data: "SGVsbG8sIFdvcmxkIQ=="
    /// })
    /// ```
    /// </summary>
    public class Base64Operator : BaseOperator
    {
        public Base64Operator()
        {
            Version = "2.0.0";
            RequiredFields = new List<string> { "action", "data" };
            OptionalFields = new List<string> 
            { 
                "encoding", "url_safe", "padding", "validate", 
                "format", "timeout", "binary" 
            };
            
            DefaultConfig = new Dictionary<string, object>
            {
                ["encoding"] = "UTF-8",
                ["url_safe"] = false,
                ["padding"] = true,
                ["validate"] = true,
                ["format"] = "string",
                ["timeout"] = 300,
                ["binary"] = false
            };
        }
        
        public override string GetName() => "base64";
        
        protected override string GetDescription() => "Base64 encoding and decoding operator for data transformation";
        
        protected override Dictionary<string, string> GetExamples()
        {
            return new Dictionary<string, string>
            {
                ["encode"] = "@base64({action: \"encode\", data: \"Hello, World!\"})",
                ["decode"] = "@base64({action: \"decode\", data: \"SGVsbG8sIFdvcmxkIQ==\"})",
                ["url_safe"] = "@base64({action: \"encode\", data: \"Hello, World!\", url_safe: true})",
                ["binary"] = "@base64({action: \"encode\", data: binaryData, binary: true})"
            };
        }
        
        protected override Dictionary<string, string> GetErrorCodes()
        {
            return new Dictionary<string, string>
            {
                ["INVALID_ACTION"] = "Invalid Base64 action",
                ["INVALID_DATA"] = "Invalid data provided",
                ["ENCODE_ERROR"] = "Base64 encoding error",
                ["DECODE_ERROR"] = "Base64 decoding error",
                ["INVALID_BASE64"] = "Invalid Base64 string",
                ["ENCODING_ERROR"] = "Character encoding error",
                ["BINARY_ERROR"] = "Binary data processing error",
                ["TIMEOUT_EXCEEDED"] = "Base64 operation timeout exceeded"
            };
        }
        
        protected override async Task<object> ExecuteOperatorAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var action = GetContextValue<string>(config, "action", "");
            var data = ResolveVariable(config.GetValueOrDefault("data"), context);
            var encoding = GetContextValue<string>(config, "encoding", "UTF-8");
            var urlSafe = GetContextValue<bool>(config, "url_safe", false);
            var padding = GetContextValue<bool>(config, "padding", true);
            var validate = GetContextValue<bool>(config, "validate", true);
            var format = GetContextValue<string>(config, "format", "string");
            var timeout = GetContextValue<int>(config, "timeout", 300);
            var binary = GetContextValue<bool>(config, "binary", false);
            
            if (string.IsNullOrEmpty(action))
                throw new ArgumentException("Action is required");
            
            if (data == null)
                throw new ArgumentException("Data is required");
            
            var startTime = DateTime.UtcNow;
            
            try
            {
                // Check timeout
                if ((DateTime.UtcNow - startTime).TotalSeconds > timeout)
                {
                    throw new TimeoutException("Base64 operation timeout exceeded");
                }
                
                switch (action.ToLower())
                {
                    case "encode":
                        return await EncodeBase64Async(data, encoding, urlSafe, padding, binary);
                    
                    case "decode":
                        return await DecodeBase64Async(data, encoding, urlSafe, binary);
                    
                    case "validate":
                        return await ValidateBase64Async(data, urlSafe);
                    
                    case "format":
                        return await FormatBase64Async(data, format, padding);
                    
                    case "to_binary":
                        return await Base64ToBinaryAsync(data, urlSafe);
                    
                    case "from_binary":
                        return await BinaryToBase64Async(data, urlSafe, padding);
                    
                    case "url_encode":
                        return await UrlEncodeBase64Async(data, padding);
                    
                    case "url_decode":
                        return await UrlDecodeBase64Async(data);
                    
                    default:
                        throw new ArgumentException($"Unknown Base64 action: {action}");
                }
            }
            catch (Exception ex)
            {
                Log("error", "Base64 operation failed", new Dictionary<string, object>
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
        /// Encode data to Base64
        /// </summary>
        private async Task<object> EncodeBase64Async(object data, string encoding, bool urlSafe, bool padding, bool binary)
        {
            try
            {
                byte[] bytes;
                
                if (binary && data is byte[] binaryData)
                {
                    bytes = binaryData;
                }
                else
                {
                    var dataString = data.ToString();
                    bytes = GetEncoding(encoding).GetBytes(dataString);
                }
                
                var base64String = Convert.ToBase64String(bytes);
                
                if (urlSafe)
                {
                    base64String = base64String.Replace('+', '-').Replace('/', '_');
                }
                
                if (!padding)
                {
                    base64String = base64String.TrimEnd('=');
                }
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["encoded"] = base64String,
                    ["encoding"] = encoding,
                    ["url_safe"] = urlSafe,
                    ["padding"] = padding,
                    ["binary"] = binary,
                    ["length"] = base64String.Length
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Encoding failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Decode Base64 data
        /// </summary>
        private async Task<object> DecodeBase64Async(object data, string encoding, bool urlSafe, bool binary)
        {
            try
            {
                var base64String = data.ToString();
                
                if (urlSafe)
                {
                    base64String = base64String.Replace('-', '+').Replace('_', '/');
                }
                
                // Add padding if needed
                var padding = 4 - (base64String.Length % 4);
                if (padding != 4)
                {
                    base64String += new string('=', padding);
                }
                
                var bytes = Convert.FromBase64String(base64String);
                
                if (binary)
                {
                    return new Dictionary<string, object>
                    {
                        ["success"] = true,
                        ["decoded"] = bytes,
                        ["encoding"] = encoding,
                        ["url_safe"] = urlSafe,
                        ["binary"] = true,
                        ["length"] = bytes.Length
                    };
                }
                else
                {
                    var decodedString = GetEncoding(encoding).GetString(bytes);
                    
                    return new Dictionary<string, object>
                    {
                        ["success"] = true,
                        ["decoded"] = decodedString,
                        ["encoding"] = encoding,
                        ["url_safe"] = urlSafe,
                        ["binary"] = false,
                        ["length"] = decodedString.Length
                    };
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Decoding failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Validate Base64 string
        /// </summary>
        private async Task<object> ValidateBase64Async(object data, bool urlSafe)
        {
            try
            {
                var base64String = data.ToString();
                var isValid = true;
                var errors = new List<string>();
                
                // Check for valid characters
                var validChars = urlSafe ? 
                    "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_" :
                    "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
                
                foreach (var c in base64String)
                {
                    if (c != '=' && !validChars.Contains(c))
                    {
                        isValid = false;
                        errors.Add($"Invalid character: {c}");
                        break;
                    }
                }
                
                // Check length
                if (base64String.Length % 4 != 0)
                {
                    isValid = false;
                    errors.Add("Length must be multiple of 4");
                }
                
                // Try to decode
                try
                {
                    var tempString = base64String;
                    if (urlSafe)
                    {
                        tempString = tempString.Replace('-', '+').Replace('_', '/');
                    }
                    
                    var padding = 4 - (tempString.Length % 4);
                    if (padding != 4)
                    {
                        tempString += new string('=', padding);
                    }
                    
                    Convert.FromBase64String(tempString);
                }
                catch (Exception ex)
                {
                    isValid = false;
                    errors.Add($"Decoding failed: {ex.Message}");
                }
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["valid"] = isValid,
                    ["errors"] = errors,
                    ["error_count"] = errors.Count,
                    ["url_safe"] = urlSafe
                };
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
        /// Format Base64 string
        /// </summary>
        private async Task<object> FormatBase64Async(object data, string format, bool padding)
        {
            try
            {
                var base64String = data.ToString();
                
                if (format == "pretty")
                {
                    // Add line breaks every 76 characters
                    var formatted = new StringBuilder();
                    for (int i = 0; i < base64String.Length; i += 76)
                    {
                        var chunk = base64String.Substring(i, Math.Min(76, base64String.Length - i));
                        formatted.AppendLine(chunk);
                    }
                    base64String = formatted.ToString().TrimEnd('\r', '\n');
                }
                
                if (!padding)
                {
                    base64String = base64String.TrimEnd('=');
                }
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["formatted"] = base64String,
                    ["format"] = format,
                    ["padding"] = padding
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Formatting failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Convert Base64 to binary
        /// </summary>
        private async Task<object> Base64ToBinaryAsync(object data, bool urlSafe)
        {
            try
            {
                var base64String = data.ToString();
                
                if (urlSafe)
                {
                    base64String = base64String.Replace('-', '+').Replace('_', '/');
                }
                
                // Add padding if needed
                var padding = 4 - (base64String.Length % 4);
                if (padding != 4)
                {
                    base64String += new string('=', padding);
                }
                
                var bytes = Convert.FromBase64String(base64String);
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["binary"] = bytes,
                    ["length"] = bytes.Length,
                    ["url_safe"] = urlSafe
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Binary conversion failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Convert binary to Base64
        /// </summary>
        private async Task<object> BinaryToBase64Async(object data, bool urlSafe, bool padding)
        {
            try
            {
                byte[] bytes;
                
                if (data is byte[] binaryData)
                {
                    bytes = binaryData;
                }
                else
                {
                    throw new ArgumentException("Data must be binary array");
                }
                
                var base64String = Convert.ToBase64String(bytes);
                
                if (urlSafe)
                {
                    base64String = base64String.Replace('+', '-').Replace('/', '_');
                }
                
                if (!padding)
                {
                    base64String = base64String.TrimEnd('=');
                }
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["base64"] = base64String,
                    ["url_safe"] = urlSafe,
                    ["padding"] = padding,
                    ["length"] = base64String.Length
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Base64 conversion failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// URL-safe Base64 encoding
        /// </summary>
        private async Task<object> UrlEncodeBase64Async(object data, bool padding)
        {
            try
            {
                var result = await EncodeBase64Async(data, "UTF-8", true, padding, false);
                
                if (result is Dictionary<string, object> dict)
                {
                    dict["action"] = "url_encode";
                }
                
                return result;
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"URL encoding failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// URL-safe Base64 decoding
        /// </summary>
        private async Task<object> UrlDecodeBase64Async(object data)
        {
            try
            {
                var result = await DecodeBase64Async(data, "UTF-8", true, false);
                
                if (result is Dictionary<string, object> dict)
                {
                    dict["action"] = "url_decode";
                }
                
                return result;
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"URL decoding failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Get encoding by name
        /// </summary>
        private Encoding GetEncoding(string encodingName)
        {
            return encodingName.ToUpper() switch
            {
                "UTF-8" => Encoding.UTF8,
                "UTF-16" => Encoding.Unicode,
                "UTF-32" => Encoding.UTF32,
                "ASCII" => Encoding.ASCII,
                "ISO-8859-1" => Encoding.GetEncoding("ISO-8859-1"),
                _ => Encoding.UTF8
            };
        }
        
        protected override ValidationResult CustomValidate(Dictionary<string, object> config)
        {
            var result = new ValidationResult();
            
            if (!config.ContainsKey("action"))
            {
                result.Errors.Add("Action is required");
            }
            
            if (!config.ContainsKey("data"))
            {
                result.Errors.Add("Data is required");
            }
            
            var action = GetContextValue<string>(config, "action", "");
            var validActions = new[] { "encode", "decode", "validate", "format", "to_binary", "from_binary", "url_encode", "url_decode" };
            
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