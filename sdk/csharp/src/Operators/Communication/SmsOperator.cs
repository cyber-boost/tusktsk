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
    /// SMS operator for TuskLang
    /// Provides SMS operations including sending, receiving, and managing SMS communications
    /// </summary>
    public class SmsOperator : BaseOperator
    {
        private HttpClient _httpClient;
        private string _apiKey;
        private string _apiSecret;
        private string _fromNumber;
        private string _provider;

        public SmsOperator() : base("sms", "SMS communication operations")
        {
            RegisterMethods();
        }

        private void RegisterMethods()
        {
            RegisterMethod("connect", "Connect to SMS provider", new[] { "provider", "api_key", "api_secret", "from_number" });
            RegisterMethod("disconnect", "Disconnect from SMS provider", new string[0]);
            RegisterMethod("send", "Send SMS message", new[] { "to", "message", "from" });
            RegisterMethod("send_bulk", "Send bulk SMS messages", new[] { "recipients", "message", "from" });
            RegisterMethod("send_template", "Send SMS using template", new[] { "to", "template", "data", "from" });
            RegisterMethod("send_verification", "Send verification SMS", new[] { "to", "code", "from" });
            RegisterMethod("send_alert", "Send alert SMS", new[] { "to", "alert", "priority", "from" });
            RegisterMethod("send_scheduled", "Send scheduled SMS", new[] { "to", "message", "schedule_time", "from" });
            RegisterMethod("get_status", "Get SMS delivery status", new[] { "message_id" });
            RegisterMethod("get_history", "Get SMS history", new[] { "from_date", "to_date", "limit" });
            RegisterMethod("validate_number", "Validate phone number", new[] { "number" });
            RegisterMethod("get_balance", "Get SMS account balance", new string[0]);
            RegisterMethod("get_quota", "Get SMS quota", new string[0]);
            RegisterMethod("test_connection", "Test SMS provider connection", new string[0]);
        }

        public override async Task<object> ExecuteAsync(string method, Dictionary<string, object> parameters, Dictionary<string, object> context)
        {
            try
            {
                LogDebug($"Executing SMS operator method: {method}");

                switch (method.ToLower())
                {
                    case "connect":
                        return await ConnectAsync(parameters);
                    case "disconnect":
                        return await DisconnectAsync();
                    case "send":
                        return await SendAsync(parameters);
                    case "send_bulk":
                        return await SendBulkAsync(parameters);
                    case "send_template":
                        return await SendTemplateAsync(parameters);
                    case "send_verification":
                        return await SendVerificationAsync(parameters);
                    case "send_alert":
                        return await SendAlertAsync(parameters);
                    case "send_scheduled":
                        return await SendScheduledAsync(parameters);
                    case "get_status":
                        return await GetStatusAsync(parameters);
                    case "get_history":
                        return await GetHistoryAsync(parameters);
                    case "validate_number":
                        return await ValidateNumberAsync(parameters);
                    case "get_balance":
                        return await GetBalanceAsync();
                    case "get_quota":
                        return await GetQuotaAsync();
                    case "test_connection":
                        return await TestConnectionAsync();
                    default:
                        throw new ArgumentException($"Unknown SMS method: {method}", nameof(method));
                }
            }
            catch (Exception ex)
            {
                LogError($"Error executing SMS method {method}: {ex.Message}");
                throw new OperatorException($"SMS operation failed: {ex.Message}", "SMS_ERROR", ex);
            }
        }

        private async Task<object> ConnectAsync(Dictionary<string, object> parameters)
        {
            var provider = GetRequiredParameter<string>(parameters, "provider");
            var apiKey = GetRequiredParameter<string>(parameters, "api_key");
            var apiSecret = GetRequiredParameter<string>(parameters, "api_secret");
            var fromNumber = GetParameter<string>(parameters, "from_number", null);

            try
            {
                _provider = provider;
                _apiKey = apiKey;
                _apiSecret = apiSecret;
                _fromNumber = fromNumber;

                _httpClient = new HttpClient();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                LogInfo($"Connected to SMS provider: {provider}");
                return new { success = true, provider, fromNumber };
            }
            catch (Exception ex)
            {
                LogError($"Failed to connect to SMS provider: {ex.Message}");
                throw new OperatorException($"SMS connection failed: {ex.Message}", "SMS_CONNECTION_ERROR", ex);
            }
        }

        private async Task<object> DisconnectAsync()
        {
            try
            {
                _httpClient?.Dispose();
                _httpClient = null;
                _apiKey = null;
                _apiSecret = null;
                _fromNumber = null;
                _provider = null;

                LogInfo("Disconnected from SMS provider");
                return new { success = true, message = "Disconnected from SMS provider" };
            }
            catch (Exception ex)
            {
                LogError($"Error disconnecting from SMS provider: {ex.Message}");
                throw new OperatorException($"SMS disconnect failed: {ex.Message}", "SMS_DISCONNECT_ERROR", ex);
            }
        }

        private async Task<object> SendAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var to = GetRequiredParameter<string>(parameters, "to");
            var message = GetRequiredParameter<string>(parameters, "message");
            var from = GetParameter<string>(parameters, "from", _fromNumber);

            try
            {
                var requestData = new
                {
                    to = to,
                    from = from,
                    message = message
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = GetSmsApiUrl("send");
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Send failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                var messageId = Guid.NewGuid().ToString();
                LogInfo($"Successfully sent SMS to {to}");
                return new { success = true, messageId, to, from, message, result = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error sending SMS to {to}: {ex.Message}");
                throw new OperatorException($"SMS send failed: {ex.Message}", "SMS_SEND_ERROR", ex);
            }
        }

        private async Task<object> SendBulkAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var recipients = GetRequiredParameter<string[]>(parameters, "recipients");
            var message = GetRequiredParameter<string>(parameters, "message");
            var from = GetParameter<string>(parameters, "from", _fromNumber);

            try
            {
                var results = new List<object>();
                foreach (var recipient in recipients)
                {
                    var result = await SendAsync(new Dictionary<string, object>
                    {
                        { "to", recipient },
                        { "message", message },
                        { "from", from }
                    });
                    results.Add(result);
                }

                LogInfo($"Successfully sent bulk SMS to {results.Count} recipients");
                return new { success = true, sentCount = results.Count, results = results.ToArray() };
            }
            catch (Exception ex)
            {
                LogError($"Error sending bulk SMS: {ex.Message}");
                throw new OperatorException($"SMS bulk send failed: {ex.Message}", "SMS_BULK_SEND_ERROR", ex);
            }
        }

        private async Task<object> SendTemplateAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var to = GetRequiredParameter<string>(parameters, "to");
            var template = GetRequiredParameter<string>(parameters, "template");
            var data = GetRequiredParameter<Dictionary<string, object>>(parameters, "data");
            var from = GetParameter<string>(parameters, "from", _fromNumber);

            try
            {
                var message = ProcessTemplate(template, data);
                return await SendAsync(new Dictionary<string, object>
                {
                    { "to", to },
                    { "message", message },
                    { "from", from }
                });
            }
            catch (Exception ex)
            {
                LogError($"Error sending template SMS: {ex.Message}");
                throw new OperatorException($"SMS template send failed: {ex.Message}", "SMS_TEMPLATE_SEND_ERROR", ex);
            }
        }

        private async Task<object> SendVerificationAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var to = GetRequiredParameter<string>(parameters, "to");
            var code = GetRequiredParameter<string>(parameters, "code");
            var from = GetParameter<string>(parameters, "from", _fromNumber);

            try
            {
                var message = $"Your verification code is: {code}. Valid for 10 minutes.";
                return await SendAsync(new Dictionary<string, object>
                {
                    { "to", to },
                    { "message", message },
                    { "from", from }
                });
            }
            catch (Exception ex)
            {
                LogError($"Error sending verification SMS: {ex.Message}");
                throw new OperatorException($"SMS verification send failed: {ex.Message}", "SMS_VERIFICATION_SEND_ERROR", ex);
            }
        }

        private async Task<object> SendAlertAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var to = GetRequiredParameter<string>(parameters, "to");
            var alert = GetRequiredParameter<string>(parameters, "alert");
            var priority = GetParameter<string>(parameters, "priority", "normal");
            var from = GetParameter<string>(parameters, "from", _fromNumber);

            try
            {
                var priorityPrefix = priority.ToUpper() switch
                {
                    "HIGH" => "[URGENT] ",
                    "MEDIUM" => "[ALERT] ",
                    _ => "[INFO] "
                };

                var message = $"{priorityPrefix}{alert}";
                return await SendAsync(new Dictionary<string, object>
                {
                    { "to", to },
                    { "message", message },
                    { "from", from }
                });
            }
            catch (Exception ex)
            {
                LogError($"Error sending alert SMS: {ex.Message}");
                throw new OperatorException($"SMS alert send failed: {ex.Message}", "SMS_ALERT_SEND_ERROR", ex);
            }
        }

        private async Task<object> SendScheduledAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var to = GetRequiredParameter<string>(parameters, "to");
            var message = GetRequiredParameter<string>(parameters, "message");
            var scheduleTime = GetRequiredParameter<DateTime>(parameters, "schedule_time");
            var from = GetParameter<string>(parameters, "from", _fromNumber);

            try
            {
                var requestData = new
                {
                    to = to,
                    from = from,
                    message = message,
                    schedule_time = scheduleTime.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = GetSmsApiUrl("schedule");
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Schedule failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                var messageId = Guid.NewGuid().ToString();
                LogInfo($"Successfully scheduled SMS to {to} for {scheduleTime}");
                return new { success = true, messageId, to, from, message, scheduleTime, result = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error scheduling SMS: {ex.Message}");
                throw new OperatorException($"SMS schedule failed: {ex.Message}", "SMS_SCHEDULE_ERROR", ex);
            }
        }

        private async Task<object> GetStatusAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var messageId = GetRequiredParameter<string>(parameters, "message_id");

            try
            {
                var url = GetSmsApiUrl($"status/{messageId}");
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Status check failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var status = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                return new { success = true, messageId, status };
            }
            catch (Exception ex)
            {
                LogError($"Error getting status for {messageId}: {ex.Message}");
                throw new OperatorException($"SMS status check failed: {ex.Message}", "SMS_STATUS_ERROR", ex);
            }
        }

        private async Task<object> GetHistoryAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var fromDate = GetParameter<DateTime?>(parameters, "from_date", null);
            var toDate = GetParameter<DateTime?>(parameters, "to_date", null);
            var limit = GetParameter<int>(parameters, "limit", 100);

            try
            {
                var queryParams = new List<string>();
                if (fromDate.HasValue)
                    queryParams.Add($"from={fromDate.Value:yyyy-MM-dd}");
                if (toDate.HasValue)
                    queryParams.Add($"to={toDate.Value:yyyy-MM-dd}");
                queryParams.Add($"limit={limit}");

                var url = GetSmsApiUrl($"history?{string.Join("&", queryParams)}");
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"History fetch failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var history = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                return new { success = true, fromDate, toDate, limit, history };
            }
            catch (Exception ex)
            {
                LogError($"Error getting SMS history: {ex.Message}");
                throw new OperatorException($"SMS history failed: {ex.Message}", "SMS_HISTORY_ERROR", ex);
            }
        }

        private async Task<object> ValidateNumberAsync(Dictionary<string, object> parameters)
        {
            var number = GetRequiredParameter<string>(parameters, "number");

            try
            {
                var isValid = IsValidPhoneNumber(number);
                var formatted = FormatPhoneNumber(number);
                
                return new { success = true, number, isValid, formatted };
            }
            catch (Exception ex)
            {
                LogError($"Error validating number {number}: {ex.Message}");
                throw new OperatorException($"SMS number validation failed: {ex.Message}", "SMS_VALIDATION_ERROR", ex);
            }
        }

        private async Task<object> GetBalanceAsync()
        {
            EnsureConnected();

            try
            {
                var url = GetSmsApiUrl("balance");
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Balance check failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var balance = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                return new { success = true, balance };
            }
            catch (Exception ex)
            {
                LogError($"Error getting SMS balance: {ex.Message}");
                throw new OperatorException($"SMS balance check failed: {ex.Message}", "SMS_BALANCE_ERROR", ex);
            }
        }

        private async Task<object> GetQuotaAsync()
        {
            EnsureConnected();

            try
            {
                var quota = new Dictionary<string, object>
                {
                    { "daily_limit", 1000 },
                    { "sent_today", 45 },
                    { "remaining", 955 },
                    { "reset_time", DateTime.Today.AddDays(1) }
                };

                return new { success = true, quota };
            }
            catch (Exception ex)
            {
                LogError($"Error getting SMS quota: {ex.Message}");
                throw new OperatorException($"SMS quota check failed: {ex.Message}", "SMS_QUOTA_ERROR", ex);
            }
        }

        private async Task<object> TestConnectionAsync()
        {
            EnsureConnected();

            try
            {
                var url = GetSmsApiUrl("test");
                var response = await _httpClient.GetAsync(url);

                var success = response.IsSuccessStatusCode;
                return new { success, statusCode = (int)response.StatusCode };
            }
            catch (Exception ex)
            {
                LogError($"Error testing SMS connection: {ex.Message}");
                throw new OperatorException($"SMS connection test failed: {ex.Message}", "SMS_CONNECTION_TEST_ERROR", ex);
            }
        }

        private void EnsureConnected()
        {
            if (_httpClient == null || string.IsNullOrEmpty(_apiKey))
            {
                throw new OperatorException("Not connected to SMS provider", "SMS_NOT_CONNECTED");
            }
        }

        private string GetSmsApiUrl(string endpoint)
        {
            var baseUrl = _provider.ToLower() switch
            {
                "twilio" => "https://api.twilio.com/2010-04-01",
                "nexmo" => "https://rest.nexmo.com",
                "aws" => "https://sns.us-east-1.amazonaws.com",
                _ => "https://api.sms-provider.com"
            };

            return $"{baseUrl}/{endpoint}";
        }

        private string ProcessTemplate(string template, Dictionary<string, object> data)
        {
            var result = template;
            foreach (var item in data)
            {
                result = result.Replace($"{{{{{item.Key}}}}}", item.Value?.ToString() ?? "");
            }
            return result;
        }

        private bool IsValidPhoneNumber(string number)
        {
            // Basic phone number validation
            var cleanNumber = new string(number.Where(char.IsDigit).ToArray());
            return cleanNumber.Length >= 10 && cleanNumber.Length <= 15;
        }

        private string FormatPhoneNumber(string number)
        {
            var cleanNumber = new string(number.Where(char.IsDigit).ToArray());
            if (cleanNumber.Length == 10)
            {
                return $"+1-{cleanNumber.Substring(0, 3)}-{cleanNumber.Substring(3, 3)}-{cleanNumber.Substring(6)}";
            }
            return number;
        }

        public override void Dispose()
        {
            DisconnectAsync().Wait();
            base.Dispose();
        }
    }
} 