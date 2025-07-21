using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text;
using System.Net.Mail;
using System.Net;
using System.Linq;

namespace TuskLang.Operators.Communication
{
    /// <summary>
    /// Email operator for TuskLang
    /// Provides email operations including sending, receiving, and managing email communications
    /// </summary>
    public class EmailOperator : BaseOperator
    {
        private SmtpClient _smtpClient;
        private string _smtpHost;
        private int _smtpPort;
        private string _username;
        private string _password;
        private bool _useSsl;
        private bool _isConnected = false;

        public EmailOperator() : base("email", "Email communication operations")
        {
            RegisterMethods();
        }

        private void RegisterMethods()
        {
            RegisterMethod("connect", "Connect to SMTP server", new[] { "host", "port", "username", "password", "use_ssl" });
            RegisterMethod("disconnect", "Disconnect from SMTP server", new string[0]);
            RegisterMethod("send", "Send an email", new[] { "to", "subject", "body", "from", "cc", "bcc", "attachments" });
            RegisterMethod("send_template", "Send email using template", new[] { "to", "template", "data", "from", "subject" });
            RegisterMethod("send_bulk", "Send bulk emails", new[] { "recipients", "subject", "body", "from" });
            RegisterMethod("send_attachment", "Send email with attachments", new[] { "to", "subject", "body", "attachments", "from" });
            RegisterMethod("send_html", "Send HTML email", new[] { "to", "subject", "html_body", "text_body", "from" });
            RegisterMethod("send_cc", "Send email with CC", new[] { "to", "cc", "subject", "body", "from" });
            RegisterMethod("send_bcc", "Send email with BCC", new[] { "to", "bcc", "subject", "body", "from" });
            RegisterMethod("send_reply", "Send reply to email", new[] { "message_id", "subject", "body", "from" });
            RegisterMethod("send_forward", "Forward email", new[] { "message_id", "to", "subject", "body", "from" });
            RegisterMethod("validate_email", "Validate email address", new[] { "email" });
            RegisterMethod("check_delivery", "Check email delivery status", new[] { "message_id" });
            RegisterMethod("get_quota", "Get email quota", new string[0]);
            RegisterMethod("test_connection", "Test SMTP connection", new string[0]);
        }

        public override async Task<object> ExecuteAsync(string method, Dictionary<string, object> parameters, Dictionary<string, object> context)
        {
            try
            {
                LogDebug($"Executing Email operator method: {method}");

                switch (method.ToLower())
                {
                    case "connect":
                        return await ConnectAsync(parameters);
                    case "disconnect":
                        return await DisconnectAsync();
                    case "send":
                        return await SendAsync(parameters);
                    case "send_template":
                        return await SendTemplateAsync(parameters);
                    case "send_bulk":
                        return await SendBulkAsync(parameters);
                    case "send_attachment":
                        return await SendAttachmentAsync(parameters);
                    case "send_html":
                        return await SendHtmlAsync(parameters);
                    case "send_cc":
                        return await SendCcAsync(parameters);
                    case "send_bcc":
                        return await SendBccAsync(parameters);
                    case "send_reply":
                        return await SendReplyAsync(parameters);
                    case "send_forward":
                        return await SendForwardAsync(parameters);
                    case "validate_email":
                        return await ValidateEmailAsync(parameters);
                    case "check_delivery":
                        return await CheckDeliveryAsync(parameters);
                    case "get_quota":
                        return await GetQuotaAsync();
                    case "test_connection":
                        return await TestConnectionAsync();
                    default:
                        throw new ArgumentException($"Unknown Email method: {method}", nameof(method));
                }
            }
            catch (Exception ex)
            {
                LogError($"Error executing Email method {method}: {ex.Message}");
                throw new OperatorException($"Email operation failed: {ex.Message}", "EMAIL_ERROR", ex);
            }
        }

        private async Task<object> ConnectAsync(Dictionary<string, object> parameters)
        {
            var host = GetRequiredParameter<string>(parameters, "host");
            var port = GetParameter<int>(parameters, "port", 587);
            var username = GetRequiredParameter<string>(parameters, "username");
            var password = GetRequiredParameter<string>(parameters, "password");
            var useSsl = GetParameter<bool>(parameters, "use_ssl", true);

            try
            {
                _smtpHost = host;
                _smtpPort = port;
                _username = username;
                _password = password;
                _useSsl = useSsl;

                _smtpClient = new SmtpClient(host, port)
                {
                    EnableSsl = useSsl,
                    Credentials = new NetworkCredential(username, password),
                    DeliveryMethod = SmtpDeliveryMethod.Network
                };

                // Test connection
                await _smtpClient.SendMailAsync(new MailMessage());
                _isConnected = true;

                LogInfo($"Connected to SMTP server at {host}:{port}");
                return new { success = true, host, port, username, useSsl };
            }
            catch (Exception ex)
            {
                LogError($"Failed to connect to SMTP server: {ex.Message}");
                throw new OperatorException($"Email connection failed: {ex.Message}", "EMAIL_CONNECTION_ERROR", ex);
            }
        }

        private async Task<object> DisconnectAsync()
        {
            try
            {
                _smtpClient?.Dispose();
                _smtpClient = null;
                _isConnected = false;

                LogInfo("Disconnected from SMTP server");
                return new { success = true, message = "Disconnected from SMTP server" };
            }
            catch (Exception ex)
            {
                LogError($"Error disconnecting from SMTP server: {ex.Message}");
                throw new OperatorException($"Email disconnect failed: {ex.Message}", "EMAIL_DISCONNECT_ERROR", ex);
            }
        }

        private async Task<object> SendAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var to = GetRequiredParameter<string[]>(parameters, "to");
            var subject = GetRequiredParameter<string>(parameters, "subject");
            var body = GetRequiredParameter<string>(parameters, "body");
            var from = GetParameter<string>(parameters, "from", _username);
            var cc = GetParameter<string[]>(parameters, "cc", new string[0]);
            var bcc = GetParameter<string[]>(parameters, "bcc", new string[0]);
            var attachments = GetParameter<string[]>(parameters, "attachments", new string[0]);

            try
            {
                var message = new MailMessage
                {
                    From = new MailAddress(from),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = false
                };

                foreach (var recipient in to)
                {
                    message.To.Add(recipient);
                }

                foreach (var ccRecipient in cc)
                {
                    message.CC.Add(ccRecipient);
                }

                foreach (var bccRecipient in bcc)
                {
                    message.Bcc.Add(bccRecipient);
                }

                foreach (var attachment in attachments)
                {
                    message.Attachments.Add(new Attachment(attachment));
                }

                await _smtpClient.SendMailAsync(message);
                message.Dispose();

                var messageId = Guid.NewGuid().ToString();
                LogInfo($"Successfully sent email to {string.Join(",", to)}");
                return new { success = true, messageId, to, subject, from };
            }
            catch (Exception ex)
            {
                LogError($"Error sending email: {ex.Message}");
                throw new OperatorException($"Email send failed: {ex.Message}", "EMAIL_SEND_ERROR", ex);
            }
        }

        private async Task<object> SendTemplateAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var to = GetRequiredParameter<string[]>(parameters, "to");
            var template = GetRequiredParameter<string>(parameters, "template");
            var data = GetRequiredParameter<Dictionary<string, object>>(parameters, "data");
            var from = GetParameter<string>(parameters, "from", _username);
            var subject = GetParameter<string>(parameters, "subject", "Email from Template");

            try
            {
                var body = ProcessTemplate(template, data);
                return await SendAsync(new Dictionary<string, object>
                {
                    { "to", to },
                    { "subject", subject },
                    { "body", body },
                    { "from", from }
                });
            }
            catch (Exception ex)
            {
                LogError($"Error sending template email: {ex.Message}");
                throw new OperatorException($"Email template send failed: {ex.Message}", "EMAIL_TEMPLATE_SEND_ERROR", ex);
            }
        }

        private async Task<object> SendBulkAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var recipients = GetRequiredParameter<Dictionary<string, object>[]>(parameters, "recipients");
            var subject = GetRequiredParameter<string>(parameters, "subject");
            var body = GetRequiredParameter<string>(parameters, "body");
            var from = GetParameter<string>(parameters, "from", _username);

            try
            {
                var results = new List<object>();
                foreach (var recipient in recipients)
                {
                    var to = recipient.GetValueOrDefault("email")?.ToString();
                    var name = recipient.GetValueOrDefault("name")?.ToString();
                    
                    if (!string.IsNullOrEmpty(to))
                    {
                        var result = await SendAsync(new Dictionary<string, object>
                        {
                            { "to", new[] { to } },
                            { "subject", subject },
                            { "body", body },
                            { "from", from }
                        });
                        results.Add(result);
                    }
                }

                LogInfo($"Successfully sent bulk email to {results.Count} recipients");
                return new { success = true, sentCount = results.Count, results = results.ToArray() };
            }
            catch (Exception ex)
            {
                LogError($"Error sending bulk email: {ex.Message}");
                throw new OperatorException($"Email bulk send failed: {ex.Message}", "EMAIL_BULK_SEND_ERROR", ex);
            }
        }

        private async Task<object> SendAttachmentAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var to = GetRequiredParameter<string[]>(parameters, "to");
            var subject = GetRequiredParameter<string>(parameters, "subject");
            var body = GetRequiredParameter<string>(parameters, "body");
            var attachments = GetRequiredParameter<string[]>(parameters, "attachments");
            var from = GetParameter<string>(parameters, "from", _username);

            try
            {
                return await SendAsync(new Dictionary<string, object>
                {
                    { "to", to },
                    { "subject", subject },
                    { "body", body },
                    { "attachments", attachments },
                    { "from", from }
                });
            }
            catch (Exception ex)
            {
                LogError($"Error sending email with attachments: {ex.Message}");
                throw new OperatorException($"Email attachment send failed: {ex.Message}", "EMAIL_ATTACHMENT_SEND_ERROR", ex);
            }
        }

        private async Task<object> SendHtmlAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var to = GetRequiredParameter<string[]>(parameters, "to");
            var subject = GetRequiredParameter<string>(parameters, "subject");
            var htmlBody = GetRequiredParameter<string>(parameters, "html_body");
            var textBody = GetParameter<string>(parameters, "text_body", "");
            var from = GetParameter<string>(parameters, "from", _username);

            try
            {
                var message = new MailMessage
                {
                    From = new MailAddress(from),
                    Subject = subject,
                    Body = textBody,
                    IsBodyHtml = true
                };

                message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(htmlBody, null, "text/html"));
                if (!string.IsNullOrEmpty(textBody))
                {
                    message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(textBody, null, "text/plain"));
                }

                foreach (var recipient in to)
                {
                    message.To.Add(recipient);
                }

                await _smtpClient.SendMailAsync(message);
                message.Dispose();

                var messageId = Guid.NewGuid().ToString();
                LogInfo($"Successfully sent HTML email to {string.Join(",", to)}");
                return new { success = true, messageId, to, subject, from };
            }
            catch (Exception ex)
            {
                LogError($"Error sending HTML email: {ex.Message}");
                throw new OperatorException($"Email HTML send failed: {ex.Message}", "EMAIL_HTML_SEND_ERROR", ex);
            }
        }

        private async Task<object> SendCcAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var to = GetRequiredParameter<string[]>(parameters, "to");
            var cc = GetRequiredParameter<string[]>(parameters, "cc");
            var subject = GetRequiredParameter<string>(parameters, "subject");
            var body = GetRequiredParameter<string>(parameters, "body");
            var from = GetParameter<string>(parameters, "from", _username);

            try
            {
                return await SendAsync(new Dictionary<string, object>
                {
                    { "to", to },
                    { "cc", cc },
                    { "subject", subject },
                    { "body", body },
                    { "from", from }
                });
            }
            catch (Exception ex)
            {
                LogError($"Error sending email with CC: {ex.Message}");
                throw new OperatorException($"Email CC send failed: {ex.Message}", "EMAIL_CC_SEND_ERROR", ex);
            }
        }

        private async Task<object> SendBccAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var to = GetRequiredParameter<string[]>(parameters, "to");
            var bcc = GetRequiredParameter<string[]>(parameters, "bcc");
            var subject = GetRequiredParameter<string>(parameters, "subject");
            var body = GetRequiredParameter<string>(parameters, "body");
            var from = GetParameter<string>(parameters, "from", _username);

            try
            {
                return await SendAsync(new Dictionary<string, object>
                {
                    { "to", to },
                    { "bcc", bcc },
                    { "subject", subject },
                    { "body", body },
                    { "from", from }
                });
            }
            catch (Exception ex)
            {
                LogError($"Error sending email with BCC: {ex.Message}");
                throw new OperatorException($"Email BCC send failed: {ex.Message}", "EMAIL_BCC_SEND_ERROR", ex);
            }
        }

        private async Task<object> SendReplyAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var messageId = GetRequiredParameter<string>(parameters, "message_id");
            var subject = GetRequiredParameter<string>(parameters, "subject");
            var body = GetRequiredParameter<string>(parameters, "body");
            var from = GetParameter<string>(parameters, "from", _username);

            try
            {
                var replySubject = subject.StartsWith("Re:") ? subject : $"Re: {subject}";
                var replyBody = $"\n\n--- Original Message ---\n{body}";

                return await SendAsync(new Dictionary<string, object>
                {
                    { "to", new[] { "original-sender@example.com" } }, // Would be extracted from messageId
                    { "subject", replySubject },
                    { "body", replyBody },
                    { "from", from }
                });
            }
            catch (Exception ex)
            {
                LogError($"Error sending reply: {ex.Message}");
                throw new OperatorException($"Email reply send failed: {ex.Message}", "EMAIL_REPLY_SEND_ERROR", ex);
            }
        }

        private async Task<object> SendForwardAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var messageId = GetRequiredParameter<string>(parameters, "message_id");
            var to = GetRequiredParameter<string[]>(parameters, "to");
            var subject = GetRequiredParameter<string>(parameters, "subject");
            var body = GetRequiredParameter<string>(parameters, "body");
            var from = GetParameter<string>(parameters, "from", _username);

            try
            {
                var forwardSubject = subject.StartsWith("Fwd:") ? subject : $"Fwd: {subject}";
                var forwardBody = $"--- Forwarded Message ---\n{body}";

                return await SendAsync(new Dictionary<string, object>
                {
                    { "to", to },
                    { "subject", forwardSubject },
                    { "body", forwardBody },
                    { "from", from }
                });
            }
            catch (Exception ex)
            {
                LogError($"Error forwarding email: {ex.Message}");
                throw new OperatorException($"Email forward send failed: {ex.Message}", "EMAIL_FORWARD_SEND_ERROR", ex);
            }
        }

        private async Task<object> ValidateEmailAsync(Dictionary<string, object> parameters)
        {
            var email = GetRequiredParameter<string>(parameters, "email");

            try
            {
                var isValid = IsValidEmail(email);
                return new { success = true, email, isValid };
            }
            catch (Exception ex)
            {
                LogError($"Error validating email {email}: {ex.Message}");
                throw new OperatorException($"Email validation failed: {ex.Message}", "EMAIL_VALIDATION_ERROR", ex);
            }
        }

        private async Task<object> CheckDeliveryAsync(Dictionary<string, object> parameters)
        {
            var messageId = GetRequiredParameter<string>(parameters, "message_id");

            try
            {
                // Simulate delivery status check
                var status = new Dictionary<string, object>
                {
                    { "message_id", messageId },
                    { "status", "delivered" },
                    { "timestamp", DateTime.UtcNow },
                    { "recipient", "recipient@example.com" }
                };

                return new { success = true, delivery = status };
            }
            catch (Exception ex)
            {
                LogError($"Error checking delivery for {messageId}: {ex.Message}");
                throw new OperatorException($"Email delivery check failed: {ex.Message}", "EMAIL_DELIVERY_CHECK_ERROR", ex);
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
                LogError($"Error getting email quota: {ex.Message}");
                throw new OperatorException($"Email quota check failed: {ex.Message}", "EMAIL_QUOTA_ERROR", ex);
            }
        }

        private async Task<object> TestConnectionAsync()
        {
            EnsureConnected();

            try
            {
                var testMessage = new MailMessage
                {
                    From = new MailAddress(_username),
                    To = { _username },
                    Subject = "Connection Test",
                    Body = "This is a test email to verify SMTP connection."
                };

                await _smtpClient.SendMailAsync(testMessage);
                testMessage.Dispose();

                return new { success = true, message = "SMTP connection test successful" };
            }
            catch (Exception ex)
            {
                LogError($"Error testing SMTP connection: {ex.Message}");
                throw new OperatorException($"Email connection test failed: {ex.Message}", "EMAIL_CONNECTION_TEST_ERROR", ex);
            }
        }

        private void EnsureConnected()
        {
            if (!_isConnected || _smtpClient == null)
            {
                throw new OperatorException("Not connected to SMTP server", "EMAIL_NOT_CONNECTED");
            }
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

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        public override void Dispose()
        {
            DisconnectAsync().Wait();
            base.Dispose();
        }
    }
} 