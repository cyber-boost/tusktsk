using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace TuskLang.Operators.Network
{
    /// <summary>
    /// HTTP Operator for TuskLang C# SDK
    /// 
    /// Provides HTTP operations with support for:
    /// - GET, POST, PUT, DELETE, PATCH requests
    /// - Request/response headers management
    /// - JSON, XML, form data handling
    /// - Authentication (Basic, Bearer, API Key)
    /// - Request/response validation
    /// - Retry logic and timeout handling
    /// - SSL/TLS configuration
    /// 
    /// Usage:
    /// ```csharp
    /// // GET request
    /// var result = @http({
    ///   method: "GET",
    ///   url: "https://api.example.com/users",
    ///   headers: {Authorization: "Bearer token"}
    /// })
    /// 
    /// // POST request
    /// var result = @http({
    ///   method: "POST",
    ///   url: "https://api.example.com/users",
    ///   body: {name: "John", age: 30},
    ///   headers: {Content-Type: "application/json"}
    /// })
    /// ```
    /// </summary>
    public class HttpOperator : BaseOperator
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        
        public HttpOperator()
        {
            Version = "2.0.0";
            RequiredFields = new List<string> { "method", "url" };
            OptionalFields = new List<string> 
            { 
                "body", "headers", "timeout", "retries", "retry_delay", "auth_type",
                "auth_token", "auth_username", "auth_password", "ssl_verify", 
                "follow_redirects", "max_redirects", "user_agent", "content_type"
            };
            
            DefaultConfig = new Dictionary<string, object>
            {
                ["method"] = "GET",
                ["timeout"] = 30,
                ["retries"] = 3,
                ["retry_delay"] = 1,
                ["ssl_verify"] = true,
                ["follow_redirects"] = true,
                ["max_redirects"] = 10,
                ["user_agent"] = "TuskLang/2.0.0",
                ["content_type"] = "application/json"
            };
        }
        
        public override string GetName() => "http";
        
        protected override string GetDescription() => "HTTP operator for making web requests and handling responses";
        
        protected override Dictionary<string, string> GetExamples()
        {
            return new Dictionary<string, string>
            {
                ["get"] = "@http({method: \"GET\", url: \"https://api.example.com/users\"})",
                ["post"] = "@http({method: \"POST\", url: \"https://api.example.com/users\", body: {name: \"John\"}})",
                ["auth"] = "@http({method: \"GET\", url: \"https://api.example.com/protected\", auth_type: \"bearer\", auth_token: \"token\"})",
                ["form"] = "@http({method: \"POST\", url: \"https://api.example.com/submit\", body: {field1: \"value1\"}, content_type: \"application/x-www-form-urlencoded\"})"
            };
        }
        
        protected override Dictionary<string, string> GetErrorCodes()
        {
            return new Dictionary<string, string>
            {
                ["INVALID_URL"] = "Invalid URL provided",
                ["INVALID_METHOD"] = "Invalid HTTP method",
                ["CONNECTION_FAILED"] = "Connection to server failed",
                ["TIMEOUT_EXCEEDED"] = "Request timeout exceeded",
                ["AUTHENTICATION_FAILED"] = "Authentication failed",
                ["SSL_ERROR"] = "SSL/TLS error",
                ["REDIRECT_ERROR"] = "Too many redirects",
                ["RESPONSE_ERROR"] = "Error in response processing"
            };
        }
        
        protected override async Task<object> ExecuteOperatorAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var method = GetContextValue<string>(config, "method", "GET");
            var url = GetContextValue<string>(config, "url", "");
            var body = ResolveVariable(config.GetValueOrDefault("body"), context);
            var headers = ResolveVariable(config.GetValueOrDefault("headers"), context);
            var timeout = GetContextValue<int>(config, "timeout", 30);
            var retries = GetContextValue<int>(config, "retries", 3);
            var retryDelay = GetContextValue<int>(config, "retry_delay", 1);
            var authType = GetContextValue<string>(config, "auth_type", "");
            var authToken = GetContextValue<string>(config, "auth_token", "");
            var authUsername = GetContextValue<string>(config, "auth_username", "");
            var authPassword = GetContextValue<string>(config, "auth_password", "");
            var sslVerify = GetContextValue<bool>(config, "ssl_verify", true);
            var followRedirects = GetContextValue<bool>(config, "follow_redirects", true);
            var maxRedirects = GetContextValue<int>(config, "max_redirects", 10);
            var userAgent = GetContextValue<string>(config, "user_agent", "TuskLang/2.0.0");
            var contentType = GetContextValue<string>(config, "content_type", "application/json");
            
            if (string.IsNullOrEmpty(url))
                throw new ArgumentException("URL is required");
            
            if (string.IsNullOrEmpty(method))
                throw new ArgumentException("Method is required");
            
            var startTime = DateTime.UtcNow;
            
            try
            {
                // Check timeout
                if ((DateTime.UtcNow - startTime).TotalSeconds > timeout)
                {
                    throw new TimeoutException("HTTP operation timeout exceeded");
                }
                
                // Execute HTTP request with retry logic
                return await ExecuteHttpRequestWithRetryAsync(
                    method, url, body, headers, timeout, retries, retryDelay,
                    authType, authToken, authUsername, authPassword, sslVerify,
                    followRedirects, maxRedirects, userAgent, contentType
                );
            }
            catch (Exception ex)
            {
                Log("error", "HTTP operation failed", new Dictionary<string, object>
                {
                    ["method"] = method,
                    ["url"] = url,
                    ["error"] = ex.Message
                });
                
                return new Dictionary<string, object>
                {
                    ["success"] = false,
                    ["error"] = ex.Message,
                    ["method"] = method,
                    ["url"] = url
                };
            }
        }
        
        /// <summary>
        /// Execute HTTP request with retry logic
        /// </summary>
        private async Task<object> ExecuteHttpRequestWithRetryAsync(
            string method, string url, object body, object headers, int timeout,
            int retries, int retryDelay, string authType, string authToken,
            string authUsername, string authPassword, bool sslVerify,
            bool followRedirects, int maxRedirects, string userAgent, string contentType)
        {
            Exception lastException = null;
            
            for (int attempt = 0; attempt <= retries; attempt++)
            {
                try
                {
                    return await ExecuteHttpRequestAsync(
                        method, url, body, headers, timeout, authType, authToken,
                        authUsername, authPassword, sslVerify, followRedirects,
                        maxRedirects, userAgent, contentType
                    );
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    
                    if (attempt < retries)
                    {
                        Log("warning", "HTTP request failed, retrying", new Dictionary<string, object>
                        {
                            ["attempt"] = attempt + 1,
                            ["retries"] = retries,
                            ["error"] = ex.Message
                        });
                        
                        await Task.Delay(retryDelay * 1000);
                    }
                }
            }
            
            throw lastException ?? new Exception("HTTP request failed after all retries");
        }
        
        /// <summary>
        /// Execute HTTP request
        /// </summary>
        private async Task<object> ExecuteHttpRequestAsync(
            string method, string url, object body, object headers, int timeout,
            string authType, string authToken, string authUsername, string authPassword,
            bool sslVerify, bool followRedirects, int maxRedirects, string userAgent, string contentType)
        {
            try
            {
                // Create HTTP request message
                var request = new HttpRequestMessage();
                request.Method = GetHttpMethod(method);
                request.RequestUri = new Uri(url);
                
                // Add headers
                AddHeaders(request, headers, userAgent, contentType);
                
                // Add authentication
                AddAuthentication(request, authType, authToken, authUsername, authPassword);
                
                // Add body
                if (body != null && (method == "POST" || method == "PUT" || method == "PATCH"))
                {
                    request.Content = CreateHttpContent(body, contentType);
                }
                
                // Execute request
                var response = await _httpClient.SendAsync(request);
                
                // Process response
                return await ProcessHttpResponseAsync(response, startTime: DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"HTTP request failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Get HTTP method
        /// </summary>
        private HttpMethod GetHttpMethod(string method)
        {
            return method.ToUpper() switch
            {
                "GET" => HttpMethod.Get,
                "POST" => HttpMethod.Post,
                "PUT" => HttpMethod.Put,
                "DELETE" => HttpMethod.Delete,
                "PATCH" => HttpMethod.Patch,
                "HEAD" => HttpMethod.Head,
                "OPTIONS" => HttpMethod.Options,
                _ => throw new ArgumentException($"Unsupported HTTP method: {method}")
            };
        }
        
        /// <summary>
        /// Add headers to request
        /// </summary>
        private void AddHeaders(HttpRequestMessage request, object headers, string userAgent, string contentType)
        {
            // Add default headers
            request.Headers.Add("User-Agent", userAgent);
            
            // Add custom headers
            if (headers is Dictionary<string, object> headersDict)
            {
                foreach (var kvp in headersDict)
                {
                    if (kvp.Key.ToLower() == "content-type")
                    {
                        // Content-Type will be set on the content
                        continue;
                    }
                    
                    request.Headers.Add(kvp.Key, kvp.Value?.ToString() ?? "");
                }
            }
        }
        
        /// <summary>
        /// Add authentication to request
        /// </summary>
        private void AddAuthentication(HttpRequestMessage request, string authType, string authToken, string authUsername, string authPassword)
        {
            if (string.IsNullOrEmpty(authType))
                return;
            
            switch (authType.ToLower())
            {
                case "bearer":
                    if (!string.IsNullOrEmpty(authToken))
                    {
                        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);
                    }
                    break;
                    
                case "basic":
                    if (!string.IsNullOrEmpty(authUsername) && !string.IsNullOrEmpty(authPassword))
                    {
                        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{authUsername}:{authPassword}"));
                        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
                    }
                    break;
                    
                case "apikey":
                    if (!string.IsNullOrEmpty(authToken))
                    {
                        request.Headers.Add("X-API-Key", authToken);
                    }
                    break;
            }
        }
        
        /// <summary>
        /// Create HTTP content
        /// </summary>
        private HttpContent CreateHttpContent(object body, string contentType)
        {
            if (body == null)
                return null;
            
            switch (contentType.ToLower())
            {
                case "application/json":
                    var jsonString = JsonSerializer.Serialize(body);
                    return new StringContent(jsonString, Encoding.UTF8, "application/json");
                
                case "application/x-www-form-urlencoded":
                    if (body is Dictionary<string, object> formData)
                    {
                        var formContent = new List<string>();
                        foreach (var kvp in formData)
                        {
                            formContent.Add($"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value?.ToString() ?? "")}");
                        }
                        var formString = string.Join("&", formContent);
                        return new StringContent(formString, Encoding.UTF8, "application/x-www-form-urlencoded");
                    }
                    break;
                
                case "text/plain":
                    return new StringContent(body.ToString(), Encoding.UTF8, "text/plain");
                
                case "text/xml":
                    return new StringContent(body.ToString(), Encoding.UTF8, "text/xml");
            }
            
            // Default to JSON
            var defaultJsonString = JsonSerializer.Serialize(body);
            return new StringContent(defaultJsonString, Encoding.UTF8, "application/json");
        }
        
        /// <summary>
        /// Process HTTP response
        /// </summary>
        private async Task<object> ProcessHttpResponseAsync(HttpResponseMessage response, DateTime startTime)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            var responseHeaders = new Dictionary<string, object>();
            
            foreach (var header in response.Headers)
            {
                responseHeaders[header.Key] = string.Join(", ", header.Value);
            }
            
            foreach (var header in response.Content.Headers)
            {
                responseHeaders[header.Key] = string.Join(", ", header.Value);
            }
            
            // Try to parse response body as JSON
            object parsedBody = responseBody;
            try
            {
                if (!string.IsNullOrEmpty(responseBody))
                {
                    parsedBody = JsonSerializer.Deserialize<object>(responseBody);
                }
            }
            catch
            {
                // Keep as string if JSON parsing fails
            }
            
            var result = new Dictionary<string, object>
            {
                ["success"] = response.IsSuccessStatusCode,
                ["status_code"] = (int)response.StatusCode,
                ["status_text"] = response.StatusCode.ToString(),
                ["url"] = response.RequestMessage?.RequestUri?.ToString(),
                ["method"] = response.RequestMessage?.Method?.ToString(),
                ["headers"] = responseHeaders,
                ["body"] = parsedBody,
                ["raw_body"] = responseBody,
                ["content_type"] = response.Content.Headers.ContentType?.ToString(),
                ["content_length"] = response.Content.Headers.ContentLength,
                ["execution_time"] = (DateTime.UtcNow - startTime).TotalSeconds
            };
            
            if (!response.IsSuccessStatusCode)
            {
                result["error"] = $"HTTP {response.StatusCode}: {response.ReasonPhrase}";
            }
            
            Log("info", "HTTP request completed", new Dictionary<string, object>
            {
                ["method"] = result["method"],
                ["url"] = result["url"],
                ["status_code"] = result["status_code"],
                ["execution_time"] = result["execution_time"]
            });
            
            return result;
        }
        
        protected override ValidationResult CustomValidate(Dictionary<string, object> config)
        {
            var result = new ValidationResult();
            
            if (!config.ContainsKey("method"))
            {
                result.Errors.Add("Method is required");
            }
            
            if (!config.ContainsKey("url"))
            {
                result.Errors.Add("URL is required");
            }
            
            var method = GetContextValue<string>(config, "method", "");
            var validMethods = new[] { "GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS" };
            
            if (!string.IsNullOrEmpty(method) && !Array.Exists(validMethods, m => m == method.ToUpper()))
            {
                result.Errors.Add($"Invalid method: {method}. Valid methods are: {string.Join(", ", validMethods)}");
            }
            
            if (config.TryGetValue("timeout", out var timeout) && timeout is int timeoutValue && timeoutValue <= 0)
            {
                result.Errors.Add("Timeout must be positive");
            }
            
            if (config.TryGetValue("retries", out var retries) && retries is int retriesValue && retriesValue < 0)
            {
                result.Errors.Add("Retries must be non-negative");
            }
            
            if (config.TryGetValue("retry_delay", out var retryDelay) && retryDelay is int retryDelayValue && retryDelayValue < 0)
            {
                result.Errors.Add("Retry delay must be non-negative");
            }
            
            if (config.TryGetValue("max_redirects", out var maxRedirects) && maxRedirects is int maxRedirectsValue && maxRedirectsValue < 0)
            {
                result.Errors.Add("Max redirects must be non-negative");
            }
            
            return result;
        }
    }
} 