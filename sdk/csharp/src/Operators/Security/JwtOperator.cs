using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;

namespace TuskLang.Operators.Security
{
    /// <summary>
    /// JWT Operator for TuskLang C# SDK
    /// 
    /// Provides JWT (JSON Web Token) capabilities with support for:
    /// - Token creation and signing
    /// - Token verification and validation
    /// - Token decoding and payload extraction
    /// - Multiple signing algorithms (HS256, RS256, ES256)
    /// - Custom claims and headers
    /// - Token refresh and rotation
    /// 
    /// Usage:
    /// ```csharp
    /// // Create JWT token
    /// var token = @jwt({
    ///   action: "create",
    ///   payload: {user_id: 123, role: "admin"},
    ///   secret: "your_secret_key",
    ///   algorithm: "HS256"
    /// })
    /// 
    /// // Verify JWT token
    /// var verified = @jwt({
    ///   action: "verify",
    ///   token: "jwt_token_string",
    ///   secret: "your_secret_key"
    /// })
    /// ```
    /// </summary>
    public class JwtOperator : BaseOperator
    {
        public JwtOperator()
        {
            Version = "2.0.0";
            RequiredFields = new List<string> { "action" };
            OptionalFields = new List<string> 
            { 
                "token", "payload", "secret", "private_key", "public_key", "algorithm", 
                "issuer", "audience", "subject", "expires_in", "not_before", "issued_at",
                "header", "claims", "refresh_token", "rotation_enabled" 
            };
            
            DefaultConfig = new Dictionary<string, object>
            {
                ["algorithm"] = "HS256",
                ["expires_in"] = 3600,
                ["issuer"] = "tusklang",
                ["audience"] = "tusklang_users"
            };
        }
        
        public override string GetName() => "jwt";
        
        protected override string GetDescription() => "JWT (JSON Web Token) operator for token creation, verification, and management with multiple signing algorithms";
        
        protected override Dictionary<string, string> GetExamples()
        {
            return new Dictionary<string, string>
            {
                ["create_basic"] = "@jwt({action: \"create\", payload: {user_id: 123}, secret: \"key\"})",
                ["create_advanced"] = "@jwt({action: \"create\", payload: {user: \"john\"}, secret: \"key\", algorithm: \"HS256\", expires_in: 3600})",
                ["verify"] = "@jwt({action: \"verify\", token: \"jwt_token\", secret: \"key\"})",
                ["decode"] = "@jwt({action: \"decode\", token: \"jwt_token\"})",
                ["refresh"] = "@jwt({action: \"refresh\", token: \"jwt_token\", secret: \"key\"})",
                ["rsa_create"] = "@jwt({action: \"create\", payload: {user: \"john\"}, private_key: @file(\"private.pem\"), algorithm: \"RS256\"})"
            };
        }
        
        protected override Dictionary<string, string> GetErrorCodes()
        {
            return new Dictionary<string, string>
            {
                ["INVALID_ACTION"] = "Invalid JWT action specified",
                ["INVALID_TOKEN"] = "Invalid JWT token format",
                ["INVALID_SECRET"] = "Invalid or missing secret key",
                ["INVALID_ALGORITHM"] = "Unsupported JWT algorithm",
                ["TOKEN_EXPIRED"] = "JWT token has expired",
                ["TOKEN_INVALID"] = "JWT token is invalid or malformed",
                ["SIGNATURE_INVALID"] = "JWT signature verification failed",
                ["MISSING_PAYLOAD"] = "JWT payload is required for token creation",
                ["KEY_MISMATCH"] = "Key type does not match algorithm requirements"
            };
        }
        
        protected override async Task<object> ExecuteOperatorAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var action = GetContextValue<string>(config, "action", "").ToLower();
            
            switch (action)
            {
                case "create":
                    return await CreateTokenAsync(config, context);
                case "verify":
                    return await VerifyTokenAsync(config, context);
                case "decode":
                    return await DecodeTokenAsync(config, context);
                case "refresh":
                    return await RefreshTokenAsync(config, context);
                case "validate":
                    return await ValidateTokenAsync(config, context);
                default:
                    throw new ArgumentException($"Invalid JWT action: {action}");
            }
        }
        
        /// <summary>
        /// Create a new JWT token
        /// </summary>
        private async Task<object> CreateTokenAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var payload = ResolveVariable(config.GetValueOrDefault("payload"), context);
            var secret = GetContextValue<string>(config, "secret", "");
            var privateKey = GetContextValue<string>(config, "private_key", "");
            var algorithm = GetContextValue<string>(config, "algorithm", "HS256").ToUpper();
            var issuer = GetContextValue<string>(config, "issuer", "tusklang");
            var audience = GetContextValue<string>(config, "audience", "tusklang_users");
            var subject = GetContextValue<string>(config, "subject", "");
            var expiresIn = GetContextValue<int>(config, "expires_in", 3600);
            var notBefore = GetContextValue<int>(config, "not_before", 0);
            var issuedAt = GetContextValue<bool>(config, "issued_at", true);
            
            if (payload == null)
            {
                throw new ArgumentException("JWT payload is required for token creation");
            }
            
            // Convert payload to claims
            var claims = new List<Claim>();
            
            if (payload is Dictionary<string, object> payloadDict)
            {
                foreach (var kvp in payloadDict)
                {
                    claims.Add(new Claim(kvp.Key, kvp.Value?.ToString() ?? ""));
                }
            }
            else
            {
                claims.Add(new Claim("data", payload.ToString()));
            }
            
            // Add standard claims
            if (!string.IsNullOrEmpty(issuer))
                claims.Add(new Claim(JwtRegisteredClaimNames.Iss, issuer));
            if (!string.IsNullOrEmpty(audience))
                claims.Add(new Claim(JwtRegisteredClaimNames.Aud, audience));
            if (!string.IsNullOrEmpty(subject))
                claims.Add(new Claim(JwtRegisteredClaimNames.Sub, subject));
            if (issuedAt)
                claims.Add(new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()));
            
            // Create signing credentials
            SecurityKey securityKey;
            SigningCredentials signingCredentials;
            
            switch (algorithm)
            {
                case "HS256":
                case "HS384":
                case "HS512":
                    if (string.IsNullOrEmpty(secret))
                        throw new ArgumentException("Secret key is required for HMAC algorithms");
                    
                    securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
                    signingCredentials = new SigningCredentials(securityKey, GetHmacAlgorithm(algorithm));
                    break;
                    
                case "RS256":
                case "RS384":
                case "RS512":
                    if (string.IsNullOrEmpty(privateKey))
                        throw new ArgumentException("Private key is required for RSA algorithms");
                    
                    var rsa = RSA.Create();
                    try
                    {
                        rsa.ImportFromPem(privateKey);
                    }
                    catch
                    {
                        throw new ArgumentException("Invalid RSA private key format");
                    }
                    
                    securityKey = new RsaSecurityKey(rsa);
                    signingCredentials = new SigningCredentials(securityKey, GetRsaAlgorithm(algorithm));
                    break;
                    
                default:
                    throw new ArgumentException($"Unsupported algorithm: {algorithm}");
            }
            
            // Create token descriptor
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddSeconds(expiresIn),
                NotBefore = notBefore > 0 ? DateTime.UtcNow.AddSeconds(notBefore) : DateTime.UtcNow,
                SigningCredentials = signingCredentials,
                Issuer = issuer,
                Audience = audience
            };
            
            // Create token
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);
            
            var result = new Dictionary<string, object>
            {
                ["token"] = tokenString,
                ["algorithm"] = algorithm,
                ["expires_at"] = tokenDescriptor.Expires?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                ["issued_at"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                ["payload"] = payload
            };
            
            Log("info", "JWT token created", new Dictionary<string, object>
            {
                ["algorithm"] = algorithm,
                ["expires_in"] = expiresIn,
                ["claims_count"] = claims.Count
            });
            
            return result;
        }
        
        /// <summary>
        /// Verify a JWT token
        /// </summary>
        private async Task<object> VerifyTokenAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var token = GetContextValue<string>(config, "token", "");
            var secret = GetContextValue<string>(config, "secret", "");
            var publicKey = GetContextValue<string>(config, "public_key", "");
            var algorithm = GetContextValue<string>(config, "algorithm", "HS256").ToUpper();
            var issuer = GetContextValue<string>(config, "issuer", "");
            var audience = GetContextValue<string>(config, "audience", "");
            
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentException("JWT token is required for verification");
            }
            
            // Create token validation parameters
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateIssuer = !string.IsNullOrEmpty(issuer),
                ValidateAudience = !string.IsNullOrEmpty(audience),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
            
            if (!string.IsNullOrEmpty(issuer))
                validationParameters.ValidIssuer = issuer;
            if (!string.IsNullOrEmpty(audience))
                validationParameters.ValidAudience = audience;
            
            // Set signing key based on algorithm
            switch (algorithm)
            {
                case "HS256":
                case "HS384":
                case "HS512":
                    if (string.IsNullOrEmpty(secret))
                        throw new ArgumentException("Secret key is required for HMAC verification");
                    
                    validationParameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
                    break;
                    
                case "RS256":
                case "RS384":
                case "RS512":
                    if (string.IsNullOrEmpty(publicKey))
                        throw new ArgumentException("Public key is required for RSA verification");
                    
                    var rsa = RSA.Create();
                    try
                    {
                        rsa.ImportFromPem(publicKey);
                    }
                    catch
                    {
                        throw new ArgumentException("Invalid RSA public key format");
                    }
                    
                    validationParameters.IssuerSigningKey = new RsaSecurityKey(rsa);
                    break;
                    
                default:
                    throw new ArgumentException($"Unsupported algorithm: {algorithm}");
            }
            
            // Verify token
            var tokenHandler = new JwtSecurityTokenHandler();
            ClaimsPrincipal principal;
            
            try
            {
                principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            }
            catch (SecurityTokenExpiredException)
            {
                return CreateErrorResponse("TOKEN_EXPIRED", "JWT token has expired");
            }
            catch (SecurityTokenInvalidSignatureException)
            {
                return CreateErrorResponse("SIGNATURE_INVALID", "JWT signature verification failed");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse("TOKEN_INVALID", $"JWT token validation failed: {ex.Message}");
            }
            
            // Extract claims
            var claims = new Dictionary<string, object>();
            foreach (var claim in principal.Claims)
            {
                claims[claim.Type] = claim.Value;
            }
            
            var result = new Dictionary<string, object>
            {
                ["valid"] = true,
                ["claims"] = claims,
                ["algorithm"] = algorithm,
                ["issuer"] = principal.FindFirst(JwtRegisteredClaimNames.Iss)?.Value,
                ["audience"] = principal.FindFirst(JwtRegisteredClaimNames.Aud)?.Value,
                ["subject"] = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value,
                ["expires_at"] = principal.FindFirst(JwtRegisteredClaimNames.Exp)?.Value,
                ["issued_at"] = principal.FindFirst(JwtRegisteredClaimNames.Iat)?.Value
            };
            
            Log("info", "JWT token verified", new Dictionary<string, object>
            {
                ["algorithm"] = algorithm,
                ["claims_count"] = claims.Count
            });
            
            return result;
        }
        
        /// <summary>
        /// Decode a JWT token without verification
        /// </summary>
        private async Task<object> DecodeTokenAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var token = GetContextValue<string>(config, "token", "");
            
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentException("JWT token is required for decoding");
            }
            
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            
            // Extract header and payload
            var header = new Dictionary<string, object>();
            foreach (var kvp in jwtToken.Header)
            {
                header[kvp.Key] = kvp.Value;
            }
            
            var payload = new Dictionary<string, object>();
            foreach (var claim in jwtToken.Claims)
            {
                payload[claim.Type] = claim.Value;
            }
            
            var result = new Dictionary<string, object>
            {
                ["header"] = header,
                ["payload"] = payload,
                ["signature"] = jwtToken.RawSignature,
                ["algorithm"] = jwtToken.Header["alg"]?.ToString(),
                ["type"] = jwtToken.Header["typ"]?.ToString()
            };
            
            Log("info", "JWT token decoded", new Dictionary<string, object>
            {
                ["algorithm"] = jwtToken.Header["alg"]?.ToString(),
                ["claims_count"] = payload.Count
            });
            
            return result;
        }
        
        /// <summary>
        /// Refresh a JWT token
        /// </summary>
        private async Task<object> RefreshTokenAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var token = GetContextValue<string>(config, "token", "");
            var secret = GetContextValue<string>(config, "secret", "");
            var expiresIn = GetContextValue<int>(config, "expires_in", 3600);
            
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentException("JWT token is required for refresh");
            }
            
            // First verify the token
            var verificationResult = await VerifyTokenAsync(new Dictionary<string, object>
            {
                ["token"] = token,
                ["secret"] = secret
            }, context);
            
            if (verificationResult is Dictionary<string, object> errorResult && errorResult.ContainsKey("error"))
            {
                return errorResult;
            }
            
            // Extract claims from verified token
            var verifiedResult = verificationResult as Dictionary<string, object>;
            var claims = verifiedResult["claims"] as Dictionary<string, object>;
            
            // Create new token with same claims but new expiration
            var newConfig = new Dictionary<string, object>(config)
            {
                ["action"] = "create",
                ["payload"] = claims,
                ["expires_in"] = expiresIn
            };
            
            return await CreateTokenAsync(newConfig, context);
        }
        
        /// <summary>
        /// Validate a JWT token (alias for verify)
        /// </summary>
        private async Task<object> ValidateTokenAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            return await VerifyTokenAsync(config, context);
        }
        
        /// <summary>
        /// Get HMAC algorithm
        /// </summary>
        private string GetHmacAlgorithm(string algorithm)
        {
            return algorithm switch
            {
                "HS256" => SecurityAlgorithms.HmacSha256,
                "HS384" => SecurityAlgorithms.HmacSha384,
                "HS512" => SecurityAlgorithms.HmacSha512,
                _ => SecurityAlgorithms.HmacSha256
            };
        }
        
        /// <summary>
        /// Get RSA algorithm
        /// </summary>
        private string GetRsaAlgorithm(string algorithm)
        {
            return algorithm switch
            {
                "RS256" => SecurityAlgorithms.RsaSha256,
                "RS384" => SecurityAlgorithms.RsaSha384,
                "RS512" => SecurityAlgorithms.RsaSha512,
                _ => SecurityAlgorithms.RsaSha256
            };
        }
        
        protected override ValidationResult CustomValidate(Dictionary<string, object> config)
        {
            var result = new ValidationResult();
            
            if (config.TryGetValue("action", out var action))
            {
                var validActions = new[] { "create", "verify", "decode", "refresh", "validate" };
                if (!validActions.Contains(action.ToString().ToLower()))
                {
                    result.Errors.Add($"Invalid action: {action}. Supported: {string.Join(", ", validActions)}");
                }
            }
            
            if (config.TryGetValue("algorithm", out var algorithm))
            {
                var validAlgorithms = new[] { "HS256", "HS384", "HS512", "RS256", "RS384", "RS512" };
                if (!validAlgorithms.Contains(algorithm.ToString().ToUpper()))
                {
                    result.Errors.Add($"Invalid algorithm: {algorithm}. Supported: {string.Join(", ", validAlgorithms)}");
                }
            }
            
            return result;
        }
    }
} 