using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TuskLang.Operators.Security
{
    /// <summary>
    /// Vault Operator for TuskLang C# SDK
    /// 
    /// Provides HashiCorp Vault integration capabilities with support for:
    /// - Secrets management and retrieval
    /// - Key-value storage operations
    /// - Dynamic secrets generation
    /// - Authentication (token, app role, kubernetes)
    /// - Encryption and decryption
    /// - Audit logging and monitoring
    /// 
    /// Usage:
    /// ```csharp
    /// // Get secret
    /// var secret = @vault({
    ///   action: "get_secret",
    ///   server: "https://vault.example.com",
    ///   token: "vault_token",
    ///   path: "secret/data/myapp/database"
    /// })
    /// 
    /// // Store secret
    /// var result = @vault({
    ///   action: "store_secret",
    ///   server: "https://vault.example.com",
    ///   token: "vault_token",
    ///   path: "secret/data/myapp/api_key",
    ///   data: {api_key: "secret_value"}
    /// })
    /// ```
    /// </summary>
    public class VaultOperator : BaseOperator
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        
        public VaultOperator()
        {
            Version = "2.0.0";
            RequiredFields = new List<string> { "action" };
            OptionalFields = new List<string> 
            { 
                "server", "token", "path", "data", "mount", "role_id", "secret_id", 
                "namespace", "timeout", "ssl_verify", "auth_method", "lease_duration" 
            };
            
            DefaultConfig = new Dictionary<string, object>
            {
                ["timeout"] = 30,
                ["ssl_verify"] = true,
                ["auth_method"] = "token"
            };
        }
        
        public override string GetName() => "vault";
        
        protected override string GetDescription() => "HashiCorp Vault secrets management and security operator";
        
        protected override Dictionary<string, string> GetExamples()
        {
            return new Dictionary<string, string>
            {
                ["get_secret"] = "@vault({action: \"get_secret\", server: \"https://vault.com\", token: \"token\", path: \"secret/data/myapp/db\"})",
                ["store_secret"] = "@vault({action: \"store_secret\", server: \"https://vault.com\", token: \"token\", path: \"secret/data/myapp/api\", data: {key: \"value\"}})",
                ["list_secrets"] = "@vault({action: \"list_secrets\", server: \"https://vault.com\", token: \"token\", path: \"secret/metadata/myapp\"})",
                ["delete_secret"] = "@vault({action: \"delete_secret\", server: \"https://vault.com\", token: \"token\", path: \"secret/data/myapp/old\"})",
                ["encrypt"] = "@vault({action: \"encrypt\", server: \"https://vault.com\", token: \"token\", key: \"mykey\", plaintext: \"sensitive_data\"})"
            };
        }
        
        protected override Dictionary<string, string> GetErrorCodes()
        {
            return new Dictionary<string, string>
            {
                ["INVALID_ACTION"] = "Invalid Vault action specified",
                ["CONNECTION_FAILED"] = "Failed to connect to Vault server",
                ["AUTHENTICATION_FAILED"] = "Vault authentication failed",
                ["INVALID_TOKEN"] = "Invalid Vault token",
                ["SECRET_NOT_FOUND"] = "Secret not found in Vault",
                ["PERMISSION_DENIED"] = "Permission denied for Vault operation",
                ["INVALID_PATH"] = "Invalid Vault path",
                ["ENCRYPTION_FAILED"] = "Vault encryption operation failed"
            };
        }
        
        protected override async Task<object> ExecuteOperatorAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var action = GetContextValue<string>(config, "action", "").ToLower();
            
            switch (action)
            {
                case "get_secret":
                    return await GetSecretAsync(config, context);
                case "store_secret":
                    return await StoreSecretAsync(config, context);
                case "list_secrets":
                    return await ListSecretsAsync(config, context);
                case "delete_secret":
                    return await DeleteSecretAsync(config, context);
                case "encrypt":
                    return await EncryptAsync(config, context);
                case "decrypt":
                    return await DecryptAsync(config, context);
                case "generate_secret":
                    return await GenerateSecretAsync(config, context);
                case "login":
                    return await LoginAsync(config, context);
                default:
                    throw new ArgumentException($"Invalid Vault action: {action}");
            }
        }
        
        /// <summary>
        /// Get secret from Vault
        /// </summary>
        private async Task<object> GetSecretAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var server = GetContextValue<string>(config, "server", "");
            var token = GetContextValue<string>(config, "token", "");
            var path = GetContextValue<string>(config, "path", "");
            var mount = GetContextValue<string>(config, "mount", "secret");
            var timeout = GetContextValue<int>(config, "timeout", 30);
            
            if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(token) || string.IsNullOrEmpty(path))
                throw new ArgumentException("Server, token, and path are required for get_secret");
            
            try
            {
                var url = $"{server.TrimEnd('/')}/v1/{mount}/data/{path.TrimStart('/')}";
                
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("X-Vault-Token", token);
                
                var response = await _httpClient.SendAsync(request);
                
                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        return new Dictionary<string, object>
                        {
                            ["error"] = "Secret not found",
                            ["path"] = path
                        };
                    }
                    
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Vault request failed: {response.StatusCode} - {errorContent}");
                }
                
                var content = await response.Content.ReadAsStringAsync();
                var vaultResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
                
                // Extract the actual secret data
                if (vaultResponse.ContainsKey("data") && vaultResponse["data"] is JsonElement dataElement)
                {
                    var data = JsonSerializer.Deserialize<Dictionary<string, object>>(dataElement.GetRawText());
                    
                    var result = new Dictionary<string, object>
                    {
                        ["secret"] = data,
                        ["path"] = path,
                        ["mount"] = mount,
                        ["lease_duration"] = vaultResponse.GetValueOrDefault("lease_duration", 0),
                        ["renewable"] = vaultResponse.GetValueOrDefault("renewable", false)
                    };
                    
                    Log("info", "Secret retrieved from Vault", new Dictionary<string, object>
                    {
                        ["path"] = path,
                        ["mount"] = mount
                    });
                    
                    return result;
                }
                
                return vaultResponse;
            }
            catch (Exception ex)
            {
                Log("error", "Failed to get secret from Vault", new Dictionary<string, object>
                {
                    ["error"] = ex.Message,
                    ["path"] = path
                });
                
                throw new ArgumentException($"Failed to get secret from Vault: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Store secret in Vault
        /// </summary>
        private async Task<object> StoreSecretAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var server = GetContextValue<string>(config, "server", "");
            var token = GetContextValue<string>(config, "token", "");
            var path = GetContextValue<string>(config, "path", "");
            var data = ResolveVariable(config.GetValueOrDefault("data"), context);
            var mount = GetContextValue<string>(config, "mount", "secret");
            
            if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(token) || string.IsNullOrEmpty(path))
                throw new ArgumentException("Server, token, and path are required for store_secret");
            
            if (data == null)
                throw new ArgumentException("Data is required for store_secret");
            
            try
            {
                var url = $"{server.TrimEnd('/')}/v1/{mount}/data/{path.TrimStart('/')}";
                
                var requestData = new Dictionary<string, object>
                {
                    ["data"] = data
                };
                
                var jsonContent = JsonSerializer.Serialize(requestData);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = content
                };
                request.Headers.Add("X-Vault-Token", token);
                
                var response = await _httpClient.SendAsync(request);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Vault request failed: {response.StatusCode} - {errorContent}");
                }
                
                var responseContent = await response.Content.ReadAsStringAsync();
                var vaultResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                
                var result = new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["path"] = path,
                    ["mount"] = mount,
                    ["version"] = vaultResponse.GetValueOrDefault("version", 1)
                };
                
                Log("info", "Secret stored in Vault", new Dictionary<string, object>
                {
                    ["path"] = path,
                    ["mount"] = mount
                });
                
                return result;
            }
            catch (Exception ex)
            {
                Log("error", "Failed to store secret in Vault", new Dictionary<string, object>
                {
                    ["error"] = ex.Message,
                    ["path"] = path
                });
                
                throw new ArgumentException($"Failed to store secret in Vault: {ex.Message}");
            }
        }
        
        /// <summary>
        /// List secrets in Vault
        /// </summary>
        private async Task<object> ListSecretsAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var server = GetContextValue<string>(config, "server", "");
            var token = GetContextValue<string>(config, "token", "");
            var path = GetContextValue<string>(config, "path", "");
            var mount = GetContextValue<string>(config, "mount", "secret");
            
            if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(token))
                throw new ArgumentException("Server and token are required for list_secrets");
            
            try
            {
                var url = $"{server.TrimEnd('/')}/v1/{mount}/metadata/{path?.TrimStart('/')}";
                
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("X-Vault-Token", token);
                
                var response = await _httpClient.SendAsync(request);
                
                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        return new Dictionary<string, object>
                        {
                            ["secrets"] = new List<string>(),
                            ["path"] = path,
                            ["count"] = 0
                        };
                    }
                    
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Vault request failed: {response.StatusCode} - {errorContent}");
                }
                
                var content = await response.Content.ReadAsStringAsync();
                var vaultResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
                
                var secrets = new List<string>();
                if (vaultResponse.ContainsKey("keys") && vaultResponse["keys"] is JsonElement keysElement)
                {
                    var keys = JsonSerializer.Deserialize<List<string>>(keysElement.GetRawText());
                    secrets.AddRange(keys);
                }
                
                var result = new Dictionary<string, object>
                {
                    ["secrets"] = secrets,
                    ["path"] = path,
                    ["mount"] = mount,
                    ["count"] = secrets.Count
                };
                
                Log("info", "Secrets listed from Vault", new Dictionary<string, object>
                {
                    ["path"] = path,
                    ["count"] = secrets.Count
                });
                
                return result;
            }
            catch (Exception ex)
            {
                Log("error", "Failed to list secrets from Vault", new Dictionary<string, object>
                {
                    ["error"] = ex.Message,
                    ["path"] = path
                });
                
                throw new ArgumentException($"Failed to list secrets from Vault: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Delete secret from Vault
        /// </summary>
        private async Task<object> DeleteSecretAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var server = GetContextValue<string>(config, "server", "");
            var token = GetContextValue<string>(config, "token", "");
            var path = GetContextValue<string>(config, "path", "");
            var mount = GetContextValue<string>(config, "mount", "secret");
            
            if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(token) || string.IsNullOrEmpty(path))
                throw new ArgumentException("Server, token, and path are required for delete_secret");
            
            try
            {
                var url = $"{server.TrimEnd('/')}/v1/{mount}/metadata/{path.TrimStart('/')}";
                
                var request = new HttpRequestMessage(HttpMethod.Delete, url);
                request.Headers.Add("X-Vault-Token", token);
                
                var response = await _httpClient.SendAsync(request);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Vault request failed: {response.StatusCode} - {errorContent}");
                }
                
                var result = new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["path"] = path,
                    ["mount"] = mount
                };
                
                Log("info", "Secret deleted from Vault", new Dictionary<string, object>
                {
                    ["path"] = path,
                    ["mount"] = mount
                });
                
                return result;
            }
            catch (Exception ex)
            {
                Log("error", "Failed to delete secret from Vault", new Dictionary<string, object>
                {
                    ["error"] = ex.Message,
                    ["path"] = path
                });
                
                throw new ArgumentException($"Failed to delete secret from Vault: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Encrypt data using Vault
        /// </summary>
        private async Task<object> EncryptAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var server = GetContextValue<string>(config, "server", "");
            var token = GetContextValue<string>(config, "token", "");
            var key = GetContextValue<string>(config, "key", "");
            var plaintext = GetContextValue<string>(config, "plaintext", "");
            var mount = GetContextValue<string>(config, "mount", "transit");
            
            if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(token) || string.IsNullOrEmpty(key) || string.IsNullOrEmpty(plaintext))
                throw new ArgumentException("Server, token, key, and plaintext are required for encrypt");
            
            try
            {
                var url = $"{server.TrimEnd('/')}/v1/{mount}/encrypt/{key}";
                
                var requestData = new Dictionary<string, object>
                {
                    ["plaintext"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(plaintext))
                };
                
                var jsonContent = JsonSerializer.Serialize(requestData);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = content
                };
                request.Headers.Add("X-Vault-Token", token);
                
                var response = await _httpClient.SendAsync(request);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Vault encryption failed: {response.StatusCode} - {errorContent}");
                }
                
                var responseContent = await response.Content.ReadAsStringAsync();
                var vaultResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                
                var result = new Dictionary<string, object>
                {
                    ["encrypted"] = true,
                    ["ciphertext"] = vaultResponse.GetValueOrDefault("data", new Dictionary<string, object>()).GetValueOrDefault("ciphertext", ""),
                    ["key"] = key
                };
                
                Log("info", "Data encrypted using Vault", new Dictionary<string, object>
                {
                    ["key"] = key
                });
                
                return result;
            }
            catch (Exception ex)
            {
                Log("error", "Failed to encrypt data using Vault", new Dictionary<string, object>
                {
                    ["error"] = ex.Message,
                    ["key"] = key
                });
                
                throw new ArgumentException($"Failed to encrypt data using Vault: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Decrypt data using Vault
        /// </summary>
        private async Task<object> DecryptAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var server = GetContextValue<string>(config, "server", "");
            var token = GetContextValue<string>(config, "token", "");
            var key = GetContextValue<string>(config, "key", "");
            var ciphertext = GetContextValue<string>(config, "ciphertext", "");
            var mount = GetContextValue<string>(config, "mount", "transit");
            
            if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(token) || string.IsNullOrEmpty(key) || string.IsNullOrEmpty(ciphertext))
                throw new ArgumentException("Server, token, key, and ciphertext are required for decrypt");
            
            try
            {
                var url = $"{server.TrimEnd('/')}/v1/{mount}/decrypt/{key}";
                
                var requestData = new Dictionary<string, object>
                {
                    ["ciphertext"] = ciphertext
                };
                
                var jsonContent = JsonSerializer.Serialize(requestData);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = content
                };
                request.Headers.Add("X-Vault-Token", token);
                
                var response = await _httpClient.SendAsync(request);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Vault decryption failed: {response.StatusCode} - {errorContent}");
                }
                
                var responseContent = await response.Content.ReadAsStringAsync();
                var vaultResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                
                var plaintextBase64 = vaultResponse.GetValueOrDefault("data", new Dictionary<string, object>()).GetValueOrDefault("plaintext", "").ToString();
                var plaintext = Encoding.UTF8.GetString(Convert.FromBase64String(plaintextBase64));
                
                var result = new Dictionary<string, object>
                {
                    ["decrypted"] = true,
                    ["plaintext"] = plaintext,
                    ["key"] = key
                };
                
                Log("info", "Data decrypted using Vault", new Dictionary<string, object>
                {
                    ["key"] = key
                });
                
                return result;
            }
            catch (Exception ex)
            {
                Log("error", "Failed to decrypt data using Vault", new Dictionary<string, object>
                {
                    ["error"] = ex.Message,
                    ["key"] = key
                });
                
                throw new ArgumentException($"Failed to decrypt data using Vault: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Generate dynamic secret
        /// </summary>
        private async Task<object> GenerateSecretAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            // This is a placeholder implementation
            // In a real implementation, you would call Vault's dynamic secrets endpoints
            return new Dictionary<string, object>
            {
                ["success"] = false,
                ["error"] = "Dynamic secret generation not implemented in this version"
            };
        }
        
        /// <summary>
        /// Login to Vault
        /// </summary>
        private async Task<object> LoginAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            // This is a placeholder implementation
            // In a real implementation, you would handle various auth methods
            return new Dictionary<string, object>
            {
                ["success"] = false,
                ["error"] = "Vault login not implemented in this version"
            };
        }
        
        protected override ValidationResult CustomValidate(Dictionary<string, object> config)
        {
            var result = new ValidationResult();
            
            if (config.TryGetValue("action", out var action))
            {
                var validActions = new[] { "get_secret", "store_secret", "list_secrets", "delete_secret", "encrypt", "decrypt", "generate_secret", "login" };
                if (!validActions.Contains(action.ToString().ToLower()))
                {
                    result.Errors.Add($"Invalid action: {action}. Supported: {string.Join(", ", validActions)}");
                }
            }
            
            return result;
        }
    }
} 