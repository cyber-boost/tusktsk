using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TuskLang.Operators.Security
{
    /// <summary>
    /// OAuth Operator for TuskLang C# SDK
    /// 
    /// Provides OAuth 2.0 authentication capabilities with support for:
    /// - Authorization Code flow
    /// - Client Credentials flow
    /// - Resource Owner Password flow
    /// - Implicit flow
    /// - Token refresh and management
    /// - Multiple OAuth providers (Google, GitHub, Azure AD, etc.)
    /// 
    /// Usage:
    /// ```csharp
    /// // Authorization Code flow
    /// var auth = @oauth({
    ///   provider: "google",
    ///   client_id: "your_client_id",
    ///   client_secret: "your_client_secret",
    ///   redirect_uri: "http://localhost/callback",
    ///   scope: "email profile"
    /// })
    /// 
    /// // Client Credentials flow
    /// var token = @oauth({
    ///   provider: "azure",
    ///   client_id: "your_client_id",
    ///   client_secret: "your_client_secret",
    ///   flow: "client_credentials",
    ///   scope: "https://graph.microsoft.com/.default"
    /// })
    /// ```
    /// </summary>
    public class OAuthOperator : BaseOperator
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        
        public OAuthOperator()
        {
            Version = "2.0.0";
            RequiredFields = new List<string> { "provider" };
            OptionalFields = new List<string> 
            { 
                "client_id", "client_secret", "redirect_uri", "scope", "flow", "code", 
                "refresh_token", "username", "password", "state", "response_type", 
                "grant_type", "token_endpoint", "authorization_endpoint" 
            };
            
            DefaultConfig = new Dictionary<string, object>
            {
                ["flow"] = "authorization_code",
                ["response_type"] = "code",
                ["state"] = Guid.NewGuid().ToString()
            };
        }
        
        public override string GetName() => "oauth";
        
        protected override string GetDescription() => "OAuth 2.0 authentication operator supporting multiple providers and flows";
        
        protected override Dictionary<string, string> GetExamples()
        {
            return new Dictionary<string, string>
            {
                ["auth_code"] = "@oauth({provider: \"google\", client_id: \"id\", client_secret: \"secret\", redirect_uri: \"http://localhost/callback\"})",
                ["client_credentials"] = "@oauth({provider: \"azure\", client_id: \"id\", client_secret: \"secret\", flow: \"client_credentials\"})",
                ["password"] = "@oauth({provider: \"custom\", client_id: \"id\", username: \"user\", password: \"pass\", flow: \"password\"})",
                ["refresh"] = "@oauth({provider: \"google\", refresh_token: \"token\", client_id: \"id\", client_secret: \"secret\"})"
            };
        }
        
        protected override Dictionary<string, string> GetErrorCodes()
        {
            return new Dictionary<string, string>
            {
                ["INVALID_PROVIDER"] = "Unsupported OAuth provider",
                ["INVALID_FLOW"] = "Unsupported OAuth flow",
                ["MISSING_CREDENTIALS"] = "Missing required credentials",
                ["AUTHENTICATION_FAILED"] = "OAuth authentication failed",
                ["TOKEN_EXPIRED"] = "OAuth token has expired",
                ["INVALID_REDIRECT_URI"] = "Invalid redirect URI",
                ["NETWORK_ERROR"] = "Network error during OAuth request"
            };
        }
        
        protected override async Task<object> ExecuteOperatorAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var provider = GetContextValue<string>(config, "provider", "").ToLower();
            var flow = GetContextValue<string>(config, "flow", "authorization_code").ToLower();
            
            switch (flow)
            {
                case "authorization_code":
                    return await HandleAuthorizationCodeFlowAsync(config, context);
                case "client_credentials":
                    return await HandleClientCredentialsFlowAsync(config, context);
                case "password":
                    return await HandlePasswordFlowAsync(config, context);
                case "implicit":
                    return await HandleImplicitFlowAsync(config, context);
                case "refresh":
                    return await HandleRefreshTokenFlowAsync(config, context);
                default:
                    throw new ArgumentException($"Unsupported OAuth flow: {flow}");
            }
        }
        
        /// <summary>
        /// Handle Authorization Code flow
        /// </summary>
        private async Task<object> HandleAuthorizationCodeFlowAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var provider = GetContextValue<string>(config, "provider", "");
            var clientId = GetContextValue<string>(config, "client_id", "");
            var clientSecret = GetContextValue<string>(config, "client_secret", "");
            var redirectUri = GetContextValue<string>(config, "redirect_uri", "");
            var scope = GetContextValue<string>(config, "scope", "");
            var code = GetContextValue<string>(config, "code", "");
            var state = GetContextValue<string>(config, "state", "");
            
            if (string.IsNullOrEmpty(clientId))
                throw new ArgumentException("Client ID is required for OAuth");
            
            if (string.IsNullOrEmpty(code))
            {
                // Generate authorization URL
                var authUrl = GenerateAuthorizationUrl(provider, clientId, redirectUri, scope, state);
                return new Dictionary<string, object>
                {
                    ["authorization_url"] = authUrl,
                    ["state"] = state,
                    ["provider"] = provider,
                    ["flow"] = "authorization_code"
                };
            }
            
            // Exchange code for token
            var tokenEndpoint = GetTokenEndpoint(provider);
            var tokenData = new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["client_id"] = clientId,
                ["code"] = code,
                ["redirect_uri"] = redirectUri
            };
            
            if (!string.IsNullOrEmpty(clientSecret))
                tokenData["client_secret"] = clientSecret;
            
            return await ExchangeCodeForTokenAsync(tokenEndpoint, tokenData);
        }
        
        /// <summary>
        /// Handle Client Credentials flow
        /// </summary>
        private async Task<object> HandleClientCredentialsFlowAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var provider = GetContextValue<string>(config, "provider", "");
            var clientId = GetContextValue<string>(config, "client_id", "");
            var clientSecret = GetContextValue<string>(config, "client_secret", "");
            var scope = GetContextValue<string>(config, "scope", "");
            
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
                throw new ArgumentException("Client ID and Client Secret are required for Client Credentials flow");
            
            var tokenEndpoint = GetTokenEndpoint(provider);
            var tokenData = new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret
            };
            
            if (!string.IsNullOrEmpty(scope))
                tokenData["scope"] = scope;
            
            return await ExchangeCodeForTokenAsync(tokenEndpoint, tokenData);
        }
        
        /// <summary>
        /// Handle Password flow
        /// </summary>
        private async Task<object> HandlePasswordFlowAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var provider = GetContextValue<string>(config, "provider", "");
            var clientId = GetContextValue<string>(config, "client_id", "");
            var clientSecret = GetContextValue<string>(config, "client_secret", "");
            var username = GetContextValue<string>(config, "username", "");
            var password = GetContextValue<string>(config, "password", "");
            var scope = GetContextValue<string>(config, "scope", "");
            
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                throw new ArgumentException("Client ID, Username, and Password are required for Password flow");
            
            var tokenEndpoint = GetTokenEndpoint(provider);
            var tokenData = new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = clientId,
                ["username"] = username,
                ["password"] = password
            };
            
            if (!string.IsNullOrEmpty(clientSecret))
                tokenData["client_secret"] = clientSecret;
            if (!string.IsNullOrEmpty(scope))
                tokenData["scope"] = scope;
            
            return await ExchangeCodeForTokenAsync(tokenEndpoint, tokenData);
        }
        
        /// <summary>
        /// Handle Implicit flow
        /// </summary>
        private async Task<object> HandleImplicitFlowAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var provider = GetContextValue<string>(config, "provider", "");
            var clientId = GetContextValue<string>(config, "client_id", "");
            var redirectUri = GetContextValue<string>(config, "redirect_uri", "");
            var scope = GetContextValue<string>(config, "scope", "");
            var state = GetContextValue<string>(config, "state", "");
            
            if (string.IsNullOrEmpty(clientId))
                throw new ArgumentException("Client ID is required for OAuth");
            
            var authUrl = GenerateAuthorizationUrl(provider, clientId, redirectUri, scope, state, "token");
            return new Dictionary<string, object>
            {
                ["authorization_url"] = authUrl,
                ["state"] = state,
                ["provider"] = provider,
                ["flow"] = "implicit"
            };
        }
        
        /// <summary>
        /// Handle Refresh Token flow
        /// </summary>
        private async Task<object> HandleRefreshTokenFlowAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var provider = GetContextValue<string>(config, "provider", "");
            var clientId = GetContextValue<string>(config, "client_id", "");
            var clientSecret = GetContextValue<string>(config, "client_secret", "");
            var refreshToken = GetContextValue<string>(config, "refresh_token", "");
            
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(refreshToken))
                throw new ArgumentException("Client ID and Refresh Token are required for Refresh Token flow");
            
            var tokenEndpoint = GetTokenEndpoint(provider);
            var tokenData = new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = clientId,
                ["refresh_token"] = refreshToken
            };
            
            if (!string.IsNullOrEmpty(clientSecret))
                tokenData["client_secret"] = clientSecret;
            
            return await ExchangeCodeForTokenAsync(tokenEndpoint, tokenData);
        }
        
        /// <summary>
        /// Exchange authorization code for token
        /// </summary>
        private async Task<object> ExchangeCodeForTokenAsync(string tokenEndpoint, Dictionary<string, string> tokenData)
        {
            try
            {
                var content = new FormUrlEncodedContent(tokenData);
                var response = await _httpClient.PostAsync(tokenEndpoint, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"OAuth token exchange failed: {response.StatusCode} - {errorContent}");
                }
                
                var responseContent = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                
                Log("info", "OAuth token exchange successful", new Dictionary<string, object>
                {
                    ["provider"] = tokenData.GetValueOrDefault("client_id"),
                    ["grant_type"] = tokenData.GetValueOrDefault("grant_type")
                });
                
                return tokenResponse;
            }
            catch (Exception ex)
            {
                Log("error", "OAuth token exchange failed", new Dictionary<string, object>
                {
                    ["error"] = ex.Message,
                    ["endpoint"] = tokenEndpoint
                });
                throw;
            }
        }
        
        /// <summary>
        /// Generate authorization URL
        /// </summary>
        private string GenerateAuthorizationUrl(string provider, string clientId, string redirectUri, string scope, string state, string responseType = "code")
        {
            var authEndpoint = GetAuthorizationEndpoint(provider);
            var queryParams = new List<string>
            {
                $"client_id={Uri.EscapeDataString(clientId)}",
                $"response_type={responseType}",
                $"redirect_uri={Uri.EscapeDataString(redirectUri)}",
                $"state={Uri.EscapeDataString(state)}"
            };
            
            if (!string.IsNullOrEmpty(scope))
                queryParams.Add($"scope={Uri.EscapeDataString(scope)}");
            
            return $"{authEndpoint}?{string.Join("&", queryParams)}";
        }
        
        /// <summary>
        /// Get authorization endpoint for provider
        /// </summary>
        private string GetAuthorizationEndpoint(string provider)
        {
            return provider switch
            {
                "google" => "https://accounts.google.com/o/oauth2/v2/auth",
                "github" => "https://github.com/login/oauth/authorize",
                "azure" => "https://login.microsoftonline.com/common/oauth2/v2.0/authorize",
                "facebook" => "https://www.facebook.com/v12.0/dialog/oauth",
                "linkedin" => "https://www.linkedin.com/oauth/v2/authorization",
                _ => throw new ArgumentException($"Unsupported OAuth provider: {provider}")
            };
        }
        
        /// <summary>
        /// Get token endpoint for provider
        /// </summary>
        private string GetTokenEndpoint(string provider)
        {
            return provider switch
            {
                "google" => "https://oauth2.googleapis.com/token",
                "github" => "https://github.com/login/oauth/access_token",
                "azure" => "https://login.microsoftonline.com/common/oauth2/v2.0/token",
                "facebook" => "https://graph.facebook.com/v12.0/oauth/access_token",
                "linkedin" => "https://www.linkedin.com/oauth/v2/accessToken",
                _ => throw new ArgumentException($"Unsupported OAuth provider: {provider}")
            };
        }
        
        protected override ValidationResult CustomValidate(Dictionary<string, object> config)
        {
            var result = new ValidationResult();
            
            if (config.TryGetValue("provider", out var provider))
            {
                var validProviders = new[] { "google", "github", "azure", "facebook", "linkedin" };
                if (!validProviders.Contains(provider.ToString().ToLower()))
                {
                    result.Errors.Add($"Unsupported provider: {provider}. Supported: {string.Join(", ", validProviders)}");
                }
            }
            
            if (config.TryGetValue("flow", out var flow))
            {
                var validFlows = new[] { "authorization_code", "client_credentials", "password", "implicit", "refresh" };
                if (!validFlows.Contains(flow.ToString().ToLower()))
                {
                    result.Errors.Add($"Unsupported flow: {flow}. Supported: {string.Join(", ", validFlows)}");
                }
            }
            
            return result;
        }
    }
} 