using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text;
using System.Net.Http;
using System.Linq;

namespace TuskLang.Operators.Communication
{
    /// <summary>
    /// Webhook operator for TuskLang
    /// Provides webhook operations including sending, receiving, and managing webhook communications
    /// </summary>
    public class WebhookOperator : BaseOperator
    {
        private HttpClient _httpClient;
        private Dictionary<string, string> _headers;
        private string _secret;

        public WebhookOperator() : base("webhook", "Webhook communication operations")
        {
            RegisterMethods();
        }

        private void RegisterMethods()
        {
            RegisterMethod("send", "Send webhook", new[] { "url", "data", "method", "headers", "secret" });
            RegisterMethod("send_json", "Send JSON webhook", new[] { "url", "data", "headers", "secret" });
            RegisterMethod("send_form", "Send form webhook", new[] { "url", "data", "headers", "secret" });
            RegisterMethod("send_xml", "Send XML webhook", new[] { "url", "data", "headers", "secret" });
            RegisterMethod("send_batch", "Send batch webhooks", new[] { "webhooks", "parallel" });
            RegisterMethod("send_retry", "Send webhook with retry", new[] { "url", "data", "max_retries", "delay" });
            RegisterMethod("validate_signature", "Validate webhook signature", new[] { "payload", "signature", "secret" });
            RegisterMethod("generate_signature", "Generate webhook signature", new[] { "payload", "secret" });
            RegisterMethod("test_webhook", "Test webhook endpoint", new[] { "url", "method" });
            RegisterMethod("get_webhook_info", "Get webhook information", new[] { "url" });
        }

        public override async Task<object> ExecuteAsync(string method, Dictionary<string, object> parameters, Dictionary<string, object> context)
        {
            try
            {
                LogDebug($"Executing Webhook operator method: {method}");

                switch (method.ToLower())
                {
                    case "send":
                        return await SendAsync(parameters);
                    case "send_json":
                        return await SendJsonAsync(parameters);
                    case "send_form":
                        return await SendFormAsync(parameters);
                    case "send_xml":
                        return await SendXmlAsync(parameters);
                    case "send_batch":
                        return await SendBatchAsync(parameters);
                    case "send_retry":
                        return await SendRetryAsync(parameters);
                    case "validate_signature":
                        return await ValidateSignatureAsync(parameters);
                    case "generate_signature":
                        return await GenerateSignatureAsync(parameters);
                    case "test_webhook":
                        return await TestWebhookAsync(parameters);
                    case "get_webhook_info":
                        return await GetWebhookInfoAsync(parameters);
                    default:
                        throw new ArgumentException($"Unknown Webhook method: {method}", nameof(method));
                }
            }
            catch (Exception ex)
            {
                LogError($"Error executing Webhook method {method}: {ex.Message}");
                throw new OperatorException($"Webhook operation failed: {ex.Message}", "WEBHOOK_ERROR", ex);
            }
        }

        private async Task<object> SendAsync(Dictionary<string, object> parameters)
        {
            var url = GetRequiredParameter<string>(parameters, "url");
            var data = GetParameter<Dictionary<string, object>>(parameters, "data", new Dictionary<string, object>());
            var method = GetParameter<string>(parameters, "method", "POST");
            var headers = GetParameter<Dictionary<string, string>>(parameters, "headers", new Dictionary<string, string>());
            var secret = GetParameter<string>(parameters, "secret", null);

            try
            {
                _httpClient = new HttpClient();
                
                // Add headers
                foreach (var header in headers)
                {
                    _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                }

                // Add signature if secret provided
                if (!string.IsNullOrEmpty(secret))
                {
                    var signature = GenerateHmacSignature(JsonSerializer.Serialize(data), secret);
                    _httpClient.DefaultRequestHeaders.Add("X-Webhook-Signature", signature);
                }

                var json = JsonSerializer.Serialize(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response;
                switch (method.ToUpper())
                {
                    case "GET":
                        var queryString = string.Join("&", data.Select(d => $"{d.Key}={Uri.EscapeDataString(d.Value?.ToString() ?? "")}"));
                        var getUrl = $"{url}?{queryString}";
                        response = await _httpClient.GetAsync(getUrl);
                        break;
                    case "POST":
                        response = await _httpClient.PostAsync(url, content);
                        break;
                    case "PUT":
                        response = await _httpClient.PutAsync(url, content);
                        break;
                    case "PATCH":
                        response = await _httpClient.PatchAsync(url, content);
                        break;
                    case "DELETE":
                        response = await _httpClient.DeleteAsync(url);
                        break;
                    default:
                        throw new ArgumentException($"Unsupported HTTP method: {method}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var responseData = JsonSerializer.Deserialize<object>(responseContent);

                LogInfo($"Successfully sent webhook to {url}");
                return new { success = true, url, method, statusCode = (int)response.StatusCode, response = responseData };
            }
            catch (Exception ex)
            {
                LogError($"Error sending webhook to {url}: {ex.Message}");
                throw new OperatorException($"Webhook send failed: {ex.Message}", "WEBHOOK_SEND_ERROR", ex);
            }
        }

        private async Task<object> SendJsonAsync(Dictionary<string, object> parameters)
        {
            var url = GetRequiredParameter<string>(parameters, "url");
            var data = GetRequiredParameter<Dictionary<string, object>>(parameters, "data");
            var headers = GetParameter<Dictionary<string, string>>(parameters, "headers", new Dictionary<string, string>());
            var secret = GetParameter<string>(parameters, "secret", null);

            try
            {
                headers["Content-Type"] = "application/json";
                return await SendAsync(new Dictionary<string, object>
                {
                    { "url", url },
                    { "data", data },
                    { "method", "POST" },
                    { "headers", headers },
                    { "secret", secret }
                });
            }
            catch (Exception ex)
            {
                LogError($"Error sending JSON webhook: {ex.Message}");
                throw new OperatorException($"Webhook JSON send failed: {ex.Message}", "WEBHOOK_JSON_SEND_ERROR", ex);
            }
        }

        private async Task<object> SendFormAsync(Dictionary<string, object> parameters)
        {
            var url = GetRequiredParameter<string>(parameters, "url");
            var data = GetRequiredParameter<Dictionary<string, object>>(parameters, "data");
            var headers = GetParameter<Dictionary<string, string>>(parameters, "headers", new Dictionary<string, string>());
            var secret = GetParameter<string>(parameters, "secret", null);

            try
            {
                _httpClient = new HttpClient();
                
                // Add headers
                foreach (var header in headers)
                {
                    _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                }

                // Create form content
                var formData = new List<KeyValuePair<string, string>>();
                foreach (var item in data)
                {
                    formData.Add(new KeyValuePair<string, string>(item.Key, item.Value?.ToString() ?? ""));
                }
                var content = new FormUrlEncodedContent(formData);

                // Add signature if secret provided
                if (!string.IsNullOrEmpty(secret))
                {
                    var formString = string.Join("&", formData.Select(f => $"{f.Key}={f.Value}"));
                    var signature = GenerateHmacSignature(formString, secret);
                    _httpClient.DefaultRequestHeaders.Add("X-Webhook-Signature", signature);
                }

                var response = await _httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                LogInfo($"Successfully sent form webhook to {url}");
                return new { success = true, url, statusCode = (int)response.StatusCode, response = responseContent };
            }
            catch (Exception ex)
            {
                LogError($"Error sending form webhook: {ex.Message}");
                throw new OperatorException($"Webhook form send failed: {ex.Message}", "WEBHOOK_FORM_SEND_ERROR", ex);
            }
        }

        private async Task<object> SendXmlAsync(Dictionary<string, object> parameters)
        {
            var url = GetRequiredParameter<string>(parameters, "url");
            var data = GetRequiredParameter<string>(parameters, "data");
            var headers = GetParameter<Dictionary<string, string>>(parameters, "headers", new Dictionary<string, string>());
            var secret = GetParameter<string>(parameters, "secret", null);

            try
            {
                headers["Content-Type"] = "application/xml";
                _httpClient = new HttpClient();
                
                // Add headers
                foreach (var header in headers)
                {
                    _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                }

                // Add signature if secret provided
                if (!string.IsNullOrEmpty(secret))
                {
                    var signature = GenerateHmacSignature(data, secret);
                    _httpClient.DefaultRequestHeaders.Add("X-Webhook-Signature", signature);
                }

                var content = new StringContent(data, Encoding.UTF8, "application/xml");
                var response = await _httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                LogInfo($"Successfully sent XML webhook to {url}");
                return new { success = true, url, statusCode = (int)response.StatusCode, response = responseContent };
            }
            catch (Exception ex)
            {
                LogError($"Error sending XML webhook: {ex.Message}");
                throw new OperatorException($"Webhook XML send failed: {ex.Message}", "WEBHOOK_XML_SEND_ERROR", ex);
            }
        }

        private async Task<object> SendBatchAsync(Dictionary<string, object> parameters)
        {
            var webhooks = GetRequiredParameter<Dictionary<string, object>[]>(parameters, "webhooks");
            var parallel = GetParameter<bool>(parameters, "parallel", true);

            try
            {
                var results = new List<object>();

                if (parallel)
                {
                    var tasks = webhooks.Select(async webhook =>
                    {
                        var url = webhook.GetValueOrDefault("url")?.ToString();
                        var data = webhook.GetValueOrDefault("data") as Dictionary<string, object>;
                        var method = webhook.GetValueOrDefault("method")?.ToString() ?? "POST";
                        var headers = webhook.GetValueOrDefault("headers") as Dictionary<string, string>;
                        var secret = webhook.GetValueOrDefault("secret")?.ToString();

                        return await SendAsync(new Dictionary<string, object>
                        {
                            { "url", url },
                            { "data", data ?? new Dictionary<string, object>() },
                            { "method", method },
                            { "headers", headers ?? new Dictionary<string, string>() },
                            { "secret", secret }
                        });
                    });

                    var taskResults = await Task.WhenAll(tasks);
                    results.AddRange(taskResults);
                }
                else
                {
                    foreach (var webhook in webhooks)
                    {
                        var url = webhook.GetValueOrDefault("url")?.ToString();
                        var data = webhook.GetValueOrDefault("data") as Dictionary<string, object>;
                        var method = webhook.GetValueOrDefault("method")?.ToString() ?? "POST";
                        var headers = webhook.GetValueOrDefault("headers") as Dictionary<string, string>;
                        var secret = webhook.GetValueOrDefault("secret")?.ToString();

                        var result = await SendAsync(new Dictionary<string, object>
                        {
                            { "url", url },
                            { "data", data ?? new Dictionary<string, object>() },
                            { "method", method },
                            { "headers", headers ?? new Dictionary<string, string>() },
                            { "secret", secret }
                        });
                        results.Add(result);
                    }
                }

                LogInfo($"Successfully sent {results.Count} webhooks");
                return new { success = true, sentCount = results.Count, results = results.ToArray() };
            }
            catch (Exception ex)
            {
                LogError($"Error sending batch webhooks: {ex.Message}");
                throw new OperatorException($"Webhook batch send failed: {ex.Message}", "WEBHOOK_BATCH_SEND_ERROR", ex);
            }
        }

        private async Task<object> SendRetryAsync(Dictionary<string, object> parameters)
        {
            var url = GetRequiredParameter<string>(parameters, "url");
            var data = GetParameter<Dictionary<string, object>>(parameters, "data", new Dictionary<string, object>());
            var maxRetries = GetParameter<int>(parameters, "max_retries", 3);
            var delay = GetParameter<int>(parameters, "delay", 1000);

            try
            {
                Exception lastException = null;
                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    try
                    {
                        var result = await SendAsync(new Dictionary<string, object>
                        {
                            { "url", url },
                            { "data", data }
                        });

                        LogInfo($"Webhook sent successfully on attempt {attempt}");
                        return new { success = true, attempt, result };
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        LogWarning($"Webhook attempt {attempt} failed: {ex.Message}");
                        
                        if (attempt < maxRetries)
                        {
                            await Task.Delay(delay);
                        }
                    }
                }

                throw lastException ?? new Exception("All webhook attempts failed");
            }
            catch (Exception ex)
            {
                LogError($"Error sending webhook with retry: {ex.Message}");
                throw new OperatorException($"Webhook retry send failed: {ex.Message}", "WEBHOOK_RETRY_SEND_ERROR", ex);
            }
        }

        private async Task<object> ValidateSignatureAsync(Dictionary<string, object> parameters)
        {
            var payload = GetRequiredParameter<string>(parameters, "payload");
            var signature = GetRequiredParameter<string>(parameters, "signature");
            var secret = GetRequiredParameter<string>(parameters, "secret");

            try
            {
                var expectedSignature = GenerateHmacSignature(payload, secret);
                var isValid = signature.Equals(expectedSignature, StringComparison.OrdinalIgnoreCase);

                return new { success = true, isValid, expectedSignature };
            }
            catch (Exception ex)
            {
                LogError($"Error validating signature: {ex.Message}");
                throw new OperatorException($"Webhook signature validation failed: {ex.Message}", "WEBHOOK_SIGNATURE_VALIDATION_ERROR", ex);
            }
        }

        private async Task<object> GenerateSignatureAsync(Dictionary<string, object> parameters)
        {
            var payload = GetRequiredParameter<string>(parameters, "payload");
            var secret = GetRequiredParameter<string>(parameters, "secret");

            try
            {
                var signature = GenerateHmacSignature(payload, secret);
                return new { success = true, signature };
            }
            catch (Exception ex)
            {
                LogError($"Error generating signature: {ex.Message}");
                throw new OperatorException($"Webhook signature generation failed: {ex.Message}", "WEBHOOK_SIGNATURE_GENERATION_ERROR", ex);
            }
        }

        private async Task<object> TestWebhookAsync(Dictionary<string, object> parameters)
        {
            var url = GetRequiredParameter<string>(parameters, "url");
            var method = GetParameter<string>(parameters, "method", "GET");

            try
            {
                _httpClient = new HttpClient();
                _httpClient.Timeout = TimeSpan.FromSeconds(10);

                HttpResponseMessage response;
                switch (method.ToUpper())
                {
                    case "GET":
                        response = await _httpClient.GetAsync(url);
                        break;
                    case "POST":
                        response = await _httpClient.PostAsync(url, new StringContent(""));
                        break;
                    case "HEAD":
                        response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));
                        break;
                    default:
                        throw new ArgumentException($"Unsupported test method: {method}");
                }

                var isSuccess = response.IsSuccessStatusCode;
                var statusCode = (int)response.StatusCode;
                var responseTime = DateTime.UtcNow;

                return new { success = true, url, method, isSuccess, statusCode, responseTime };
            }
            catch (Exception ex)
            {
                LogError($"Error testing webhook {url}: {ex.Message}");
                return new { success = false, url, method, error = ex.Message };
            }
        }

        private async Task<object> GetWebhookInfoAsync(Dictionary<string, object> parameters)
        {
            var url = GetRequiredParameter<string>(parameters, "url");

            try
            {
                var info = new Dictionary<string, object>
                {
                    { "url", url },
                    { "supported_methods", new[] { "GET", "POST", "PUT", "PATCH", "DELETE" } },
                    { "content_types", new[] { "application/json", "application/xml", "application/x-www-form-urlencoded" } },
                    { "max_payload_size", "10MB" },
                    { "rate_limit", "1000 requests per hour" }
                };

                return new { success = true, info };
            }
            catch (Exception ex)
            {
                LogError($"Error getting webhook info: {ex.Message}");
                throw new OperatorException($"Webhook info failed: {ex.Message}", "WEBHOOK_INFO_ERROR", ex);
            }
        }

        private string GenerateHmacSignature(string payload, string secret)
        {
            using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return Convert.ToHexString(hash).ToLower();
        }

        public override void Dispose()
        {
            _httpClient?.Dispose();
            base.Dispose();
        }
    }
} 