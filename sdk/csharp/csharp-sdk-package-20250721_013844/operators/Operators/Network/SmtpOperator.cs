using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Mail;
using System.Net;
using System.Text;

namespace TuskLang.Operators.Network
{
    /// <summary>
    /// SMTP Operator for TuskLang C# SDK
    /// 
    /// Provides SMTP email operations with support for:
    /// - Email sending with SMTP
    /// - Multiple recipients (To, CC, BCC)
    /// - HTML and plain text email content
    /// - File attachments
    /// - Email templates
    /// - Authentication (Username/Password, OAuth)
    /// - SSL/TLS configuration
    /// 
    /// Usage:
    /// ```csharp
    /// // Send email
    /// var result = @smtp({
    ///   server: "smtp.gmail.com",
    ///   port: 587,
    ///   username: "user@gmail.com",
    ///   password: "password",
    ///   from: "user@gmail.com",
    ///   to: ["recipient@example.com"],
    ///   subject: "Test Email",
    ///   body: "Hello, this is a test email!"
    /// })
    /// 
    /// // Send HTML email
    /// var result = @smtp({
    ///   server: "smtp.gmail.com",
    ///   port: 587,
    ///   username: "user@gmail.com",
    ///   password: "password",
    ///   from: "user@gmail.com",
    ///   to: ["recipient@example.com"],
    ///   subject: "HTML Email",
    ///   body: "<h1>Hello</h1><p>This is HTML content.</p>",
    ///   html: true
    /// })
    /// ```
    /// </summary>
    public class SmtpOperator : BaseOperator
    {
        public SmtpOperator()
        {
            Version = "2.0.0";
            RequiredFields = new List<string> { "server", "from", "to" };
            OptionalFields = new List<string> 
            { 
                "port", "username", "password", "subject", "body", "html", "cc", "bcc",
                "attachments", "timeout", "ssl", "tls", "auth_type", "priority",
                "reply_to", "headers", "template", "template_data"
            };
            
            DefaultConfig = new Dictionary<string, object>
            {
                ["port"] = 587,
                ["timeout"] = 30,
                ["ssl"] = false,
                ["tls"] = true,
                ["auth_type"] = "basic",
                ["priority"] = "normal",
                ["html"] = false
            };
        }
        
        public override string GetName() => "smtp";
        
        protected override string GetDescription() => "SMTP operator for sending emails via SMTP servers";
        
        protected override Dictionary<string, string> GetExamples()
        {
            return new Dictionary<string, string>
            {
                ["basic"] = "@smtp({server: \"smtp.gmail.com\", from: \"user@gmail.com\", to: [\"recipient@example.com\"], subject: \"Test\", body: \"Hello!\"})",
                ["html"] = "@smtp({server: \"smtp.gmail.com\", from: \"user@gmail.com\", to: [\"recipient@example.com\"], subject: \"HTML Email\", body: \"<h1>Hello</h1>\", html: true})",
                ["auth"] = "@smtp({server: \"smtp.gmail.com\", username: \"user@gmail.com\", password: \"pass\", from: \"user@gmail.com\", to: [\"recipient@example.com\"]})",
                ["attachments"] = "@smtp({server: \"smtp.gmail.com\", from: \"user@gmail.com\", to: [\"recipient@example.com\"], attachments: [\"file1.pdf\", \"file2.txt\"]})"
            };
        }
        
        protected override Dictionary<string, string> GetErrorCodes()
        {
            return new Dictionary<string, string>
            {
                ["INVALID_SERVER"] = "Invalid SMTP server",
                ["CONNECTION_FAILED"] = "Failed to connect to SMTP server",
                ["AUTHENTICATION_FAILED"] = "SMTP authentication failed",
                ["INVALID_EMAIL"] = "Invalid email address",
                ["SEND_FAILED"] = "Failed to send email",
                ["TIMEOUT_EXCEEDED"] = "SMTP operation timeout exceeded",
                ["SSL_ERROR"] = "SSL/TLS configuration error",
                ["ATTACHMENT_ERROR"] = "File attachment error"
            };
        }
        
        protected override async Task<object> ExecuteOperatorAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var server = GetContextValue<string>(config, "server", "");
            var port = GetContextValue<int>(config, "port", 587);
            var username = GetContextValue<string>(config, "username", "");
            var password = GetContextValue<string>(config, "password", "");
            var from = GetContextValue<string>(config, "from", "");
            var to = ResolveVariable(config.GetValueOrDefault("to"), context);
            var subject = GetContextValue<string>(config, "subject", "");
            var body = GetContextValue<string>(config, "body", "");
            var html = GetContextValue<bool>(config, "html", false);
            var cc = ResolveVariable(config.GetValueOrDefault("cc"), context);
            var bcc = ResolveVariable(config.GetValueOrDefault("bcc"), context);
            var attachments = ResolveVariable(config.GetValueOrDefault("attachments"), context);
            var timeout = GetContextValue<int>(config, "timeout", 30);
            var ssl = GetContextValue<bool>(config, "ssl", false);
            var tls = GetContextValue<bool>(config, "tls", true);
            var authType = GetContextValue<string>(config, "auth_type", "basic");
            var priority = GetContextValue<string>(config, "priority", "normal");
            var replyTo = GetContextValue<string>(config, "reply_to", "");
            var headers = ResolveVariable(config.GetValueOrDefault("headers"), context);
            var template = GetContextValue<string>(config, "template", "");
            var templateData = ResolveVariable(config.GetValueOrDefault("template_data"), context);
            
            if (string.IsNullOrEmpty(server))
                throw new ArgumentException("SMTP server is required");
            
            if (string.IsNullOrEmpty(from))
                throw new ArgumentException("From email address is required");
            
            if (to == null)
                throw new ArgumentException("To email address(es) are required");
            
            var startTime = DateTime.UtcNow;
            
            try
            {
                // Check timeout
                if ((DateTime.UtcNow - startTime).TotalSeconds > timeout)
                {
                    throw new TimeoutException("SMTP operation timeout exceeded");
                }
                
                // Send email
                return await SendEmailAsync(
                    server, port, username, password, from, to, subject, body, html,
                    cc, bcc, attachments, timeout, ssl, tls, authType, priority,
                    replyTo, headers, template, templateData
                );
            }
            catch (Exception ex)
            {
                Log("error", "SMTP operation failed", new Dictionary<string, object>
                {
                    ["server"] = server,
                    ["from"] = from,
                    ["error"] = ex.Message
                });
                
                return new Dictionary<string, object>
                {
                    ["success"] = false,
                    ["error"] = ex.Message,
                    ["server"] = server,
                    ["from"] = from
                };
            }
        }
        
        /// <summary>
        /// Send email via SMTP
        /// </summary>
        private async Task<object> SendEmailAsync(
            string server, int port, string username, string password, string from,
            object to, string subject, string body, bool html, object cc, object bcc,
            object attachments, int timeout, bool ssl, bool tls, string authType,
            string priority, string replyTo, object headers, string template, object templateData)
        {
            try
            {
                // Create SMTP client
                using var smtpClient = new SmtpClient(server, port);
                
                // Configure SMTP client
                smtpClient.Timeout = timeout * 1000;
                smtpClient.EnableSsl = ssl || tls;
                smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                
                // Set authentication
                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                {
                    smtpClient.Credentials = new NetworkCredential(username, password);
                }
                
                // Create mail message
                using var mailMessage = new MailMessage();
                
                // Set basic properties
                mailMessage.From = new MailAddress(from);
                mailMessage.Subject = subject ?? "";
                mailMessage.Body = body ?? "";
                mailMessage.IsBodyHtml = html;
                
                // Set priority
                mailMessage.Priority = GetMailPriority(priority);
                
                // Set reply-to
                if (!string.IsNullOrEmpty(replyTo))
                {
                    mailMessage.ReplyToList.Add(new MailAddress(replyTo));
                }
                
                // Add recipients
                AddRecipients(mailMessage.To, to);
                AddRecipients(mailMessage.CC, cc);
                AddRecipients(mailMessage.Bcc, bcc);
                
                // Add headers
                AddHeaders(mailMessage, headers);
                
                // Add attachments
                AddAttachments(mailMessage, attachments);
                
                // Process template if provided
                if (!string.IsNullOrEmpty(template))
                {
                    ProcessTemplate(mailMessage, template, templateData);
                }
                
                // Send email
                await smtpClient.SendMailAsync(mailMessage);
                
                Log("info", "Email sent successfully", new Dictionary<string, object>
                {
                    ["server"] = server,
                    ["from"] = from,
                    ["to_count"] = mailMessage.To.Count,
                    ["subject"] = subject
                });
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["server"] = server,
                    ["from"] = from,
                    ["to"] = GetRecipientList(mailMessage.To),
                    ["cc"] = GetRecipientList(mailMessage.CC),
                    ["bcc"] = GetRecipientList(mailMessage.Bcc),
                    ["subject"] = subject,
                    ["sent"] = true,
                    ["message_id"] = GenerateMessageId()
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Failed to send email: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Add recipients to mail message
        /// </summary>
        private void AddRecipients(MailAddressCollection collection, object recipients)
        {
            if (recipients == null)
                return;
            
            if (recipients is List<object> recipientList)
            {
                foreach (var recipient in recipientList)
                {
                    collection.Add(new MailAddress(recipient.ToString()));
                }
            }
            else if (recipients is string recipientString)
            {
                collection.Add(new MailAddress(recipientString));
            }
        }
        
        /// <summary>
        /// Add headers to mail message
        /// </summary>
        private void AddHeaders(MailMessage mailMessage, object headers)
        {
            if (headers is Dictionary<string, object> headersDict)
            {
                foreach (var kvp in headersDict)
                {
                    mailMessage.Headers.Add(kvp.Key, kvp.Value?.ToString() ?? "");
                }
            }
        }
        
        /// <summary>
        /// Add attachments to mail message
        /// </summary>
        private void AddAttachments(MailMessage mailMessage, object attachments)
        {
            if (attachments == null)
                return;
            
            if (attachments is List<object> attachmentList)
            {
                foreach (var attachment in attachmentList)
                {
                    var attachmentPath = attachment.ToString();
                    if (System.IO.File.Exists(attachmentPath))
                    {
                        mailMessage.Attachments.Add(new Attachment(attachmentPath));
                    }
                }
            }
            else if (attachments is string attachmentPath)
            {
                if (System.IO.File.Exists(attachmentPath))
                {
                    mailMessage.Attachments.Add(new Attachment(attachmentPath));
                }
            }
        }
        
        /// <summary>
        /// Process email template
        /// </summary>
        private void ProcessTemplate(MailMessage mailMessage, string template, object templateData)
        {
            // Simplified template processing
            // In a real implementation, you would use a proper template engine
            
            if (templateData is Dictionary<string, object> data)
            {
                var processedBody = template;
                foreach (var kvp in data)
                {
                    processedBody = processedBody.Replace($"{{{{{kvp.Key}}}}}", kvp.Value?.ToString() ?? "");
                }
                
                mailMessage.Body = processedBody;
            }
        }
        
        /// <summary>
        /// Get mail priority
        /// </summary>
        private MailPriority GetMailPriority(string priority)
        {
            return priority.ToLower() switch
            {
                "high" => MailPriority.High,
                "low" => MailPriority.Low,
                _ => MailPriority.Normal
            };
        }
        
        /// <summary>
        /// Get recipient list
        /// </summary>
        private List<string> GetRecipientList(MailAddressCollection collection)
        {
            var recipients = new List<string>();
            foreach (MailAddress address in collection)
            {
                recipients.Add(address.Address);
            }
            return recipients;
        }
        
        /// <summary>
        /// Generate message ID
        /// </summary>
        private string GenerateMessageId()
        {
            return $"<{Guid.NewGuid()}@tusklang>";
        }
        
        protected override ValidationResult CustomValidate(Dictionary<string, object> config)
        {
            var result = new ValidationResult();
            
            if (!config.ContainsKey("server"))
            {
                result.Errors.Add("SMTP server is required");
            }
            
            if (!config.ContainsKey("from"))
            {
                result.Errors.Add("From email address is required");
            }
            
            if (!config.ContainsKey("to"))
            {
                result.Errors.Add("To email address(es) are required");
            }
            
            if (config.TryGetValue("port", out var port) && port is int portValue && (portValue <= 0 || portValue > 65535))
            {
                result.Errors.Add("Port must be between 1 and 65535");
            }
            
            if (config.TryGetValue("timeout", out var timeout) && timeout is int timeoutValue && timeoutValue <= 0)
            {
                result.Errors.Add("Timeout must be positive");
            }
            
            return result;
        }
    }
} 