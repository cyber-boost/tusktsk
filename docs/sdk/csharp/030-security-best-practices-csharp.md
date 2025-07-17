# 🔒 Security Best Practices - TuskLang for C# - "Security Mastery"

**Master security best practices with TuskLang in your C# applications!**

Security is paramount in modern applications. This guide covers authentication, authorization, encryption, secure configuration, and real-world security scenarios for TuskLang in C# environments.

## 🛡️ Security Philosophy

### "We Don't Bow to Any King"
- **Security first** - Security is not an afterthought
- **Defense in depth** - Multiple layers of protection
- **Least privilege** - Grant minimum necessary access
- **Secure by default** - Secure configurations out of the box
- **Continuous monitoring** - Monitor for security threats

## 🔐 Authentication

### Example: JWT Authentication Service
```csharp
// JwtAuthenticationService.cs
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

public class JwtAuthenticationService
{
    private readonly TuskLang _parser;
    private readonly ILogger<JwtAuthenticationService> _logger;
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    
    public JwtAuthenticationService(ILogger<JwtAuthenticationService> logger)
    {
        _parser = new TuskLang();
        _logger = logger;
        
        // Load security configuration
        var config = _parser.ParseFile("config/security.tsk");
        _secretKey = config["jwt_secret"].ToString();
        _issuer = config["jwt_issuer"].ToString();
        _audience = config["jwt_audience"].ToString();
    }
    
    public string GenerateToken(string userId, string email, List<string> roles)
    {
        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Email, email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            
            // Add role claims
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
            
            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: credentials
            );
            
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenString = tokenHandler.WriteToken(token);
            
            _logger.LogInformation("Generated JWT token for user {UserId}", userId);
            return tokenString;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate JWT token for user {UserId}", userId);
            throw;
        }
    }
    
    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secretKey);
            
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
            
            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            
            _logger.LogDebug("JWT token validated successfully");
            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "JWT token validation failed");
            return null;
        }
    }
}
```

## 🔑 Authorization

### Example: Role-Based Authorization Service
```csharp
// AuthorizationService.cs
using System.Security.Claims;

public class AuthorizationService
{
    private readonly TuskLang _parser;
    private readonly ILogger<AuthorizationService> _logger;
    private readonly Dictionary<string, List<string>> _rolePermissions;
    
    public AuthorizationService(ILogger<AuthorizationService> logger)
    {
        _parser = new TuskLang();
        _logger = logger;
        _rolePermissions = new Dictionary<string, List<string>>();
        
        LoadRolePermissions();
    }
    
    private void LoadRolePermissions()
    {
        var config = _parser.ParseFile("config/authorization.tsk");
        var roles = config["roles"] as Dictionary<string, object>;
        
        foreach (var role in roles ?? new Dictionary<string, object>())
        {
            var roleName = role.Key;
            var permissions = role.Value as List<object>;
            
            _rolePermissions[roleName] = permissions?.Select(p => p.ToString()).ToList() ?? new List<string>();
        }
        
        _logger.LogInformation("Loaded authorization configuration for {Count} roles", _rolePermissions.Count);
    }
    
    public bool HasPermission(ClaimsPrincipal user, string permission)
    {
        try
        {
            var roles = user.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();
            
            foreach (var role in roles)
            {
                if (_rolePermissions.ContainsKey(role) && 
                    _rolePermissions[role].Contains(permission))
                {
                    _logger.LogDebug("User {UserId} has permission {Permission} through role {Role}", 
                        user.Identity?.Name, permission, role);
                    return true;
                }
            }
            
            _logger.LogWarning("User {UserId} denied permission {Permission}", 
                user.Identity?.Name, permission);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission {Permission} for user {UserId}", 
                permission, user.Identity?.Name);
            return false;
        }
    }
    
    public List<string> GetUserPermissions(ClaimsPrincipal user)
    {
        var permissions = new List<string>();
        var roles = user.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();
        
        foreach (var role in roles)
        {
            if (_rolePermissions.ContainsKey(role))
            {
                permissions.AddRange(_rolePermissions[role]);
            }
        }
        
        return permissions.Distinct().ToList();
    }
}
```

## 🔐 Encryption

### Example: Encryption Service
```csharp
// EncryptionService.cs
using System.Security.Cryptography;
using System.Text;

public class EncryptionService
{
    private readonly TuskLang _parser;
    private readonly ILogger<EncryptionService> _logger;
    private readonly string _encryptionKey;
    
    public EncryptionService(ILogger<EncryptionService> logger)
    {
        _parser = new TuskLang();
        _logger = logger;
        
        var config = _parser.ParseFile("config/security.tsk");
        _encryptionKey = config["encryption_key"].ToString();
    }
    
    public string Encrypt(string plainText)
    {
        try
        {
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(_encryptionKey.PadRight(32).Substring(0, 32));
            aes.GenerateIV();
            
            using var encryptor = aes.CreateEncryptor();
            using var msEncrypt = new MemoryStream();
            using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
            using var swEncrypt = new StreamWriter(csEncrypt);
            
            swEncrypt.Write(plainText);
            swEncrypt.Flush();
            csEncrypt.FlushFinalBlock();
            
            var encrypted = msEncrypt.ToArray();
            var result = Convert.ToBase64String(aes.IV.Concat(encrypted).ToArray());
            
            _logger.LogDebug("Successfully encrypted data");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt data");
            throw;
        }
    }
    
    public string Decrypt(string cipherText)
    {
        try
        {
            var cipherBytes = Convert.FromBase64String(cipherText);
            
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(_encryptionKey.PadRight(32).Substring(0, 32));
            
            var iv = new byte[16];
            var encrypted = new byte[cipherBytes.Length - 16];
            
            Array.Copy(cipherBytes, 0, iv, 0, 16);
            Array.Copy(cipherBytes, 16, encrypted, 0, encrypted.Length);
            
            aes.IV = iv;
            
            using var decryptor = aes.CreateDecryptor();
            using var msDecrypt = new MemoryStream(encrypted);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);
            
            var decrypted = srDecrypt.ReadToEnd();
            
            _logger.LogDebug("Successfully decrypted data");
            return decrypted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt data");
            throw;
        }
    }
    
    public string HashPassword(string password)
    {
        try
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            var hash = Convert.ToBase64String(hashedBytes);
            
            _logger.LogDebug("Successfully hashed password");
            return hash;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to hash password");
            throw;
        }
    }
    
    public bool VerifyPassword(string password, string hash)
    {
        try
        {
            var passwordHash = HashPassword(password);
            var isValid = passwordHash == hash;
            
            _logger.LogDebug("Password verification result: {IsValid}", isValid);
            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify password");
            return false;
        }
    }
}
```

## 🔒 Secure Configuration

### Example: Secure Configuration Service
```csharp
// SecureConfigurationService.cs
public class SecureConfigurationService
{
    private readonly TuskLang _parser;
    private readonly EncryptionService _encryptionService;
    private readonly ILogger<SecureConfigurationService> _logger;
    
    public SecureConfigurationService(
        EncryptionService encryptionService,
        ILogger<SecureConfigurationService> logger)
    {
        _parser = new TuskLang();
        _encryptionService = encryptionService;
        _logger = logger;
    }
    
    public async Task<Dictionary<string, object>> LoadSecureConfigurationAsync(string filePath)
    {
        try
        {
            var config = _parser.ParseFile(filePath);
            
            // Decrypt sensitive values
            await DecryptSensitiveValuesAsync(config);
            
            _logger.LogInformation("Loaded secure configuration from {FilePath}", filePath);
            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load secure configuration from {FilePath}", filePath);
            throw;
        }
    }
    
    private async Task DecryptSensitiveValuesAsync(Dictionary<string, object> config)
    {
        var sensitiveKeys = new[] { "database_password", "api_key", "jwt_secret", "encryption_key" };
        
        foreach (var key in sensitiveKeys)
        {
            if (config.ContainsKey(key))
            {
                var value = config[key].ToString();
                if (IsEncrypted(value))
                {
                    var decryptedValue = _encryptionService.Decrypt(ExtractEncryptedValue(value));
                    config[key] = decryptedValue;
                    
                    _logger.LogDebug("Decrypted sensitive configuration key: {Key}", key);
                }
            }
        }
    }
    
    private bool IsEncrypted(string value)
    {
        return value.StartsWith("ENC[") && value.EndsWith("]");
    }
    
    private string ExtractEncryptedValue(string encryptedString)
    {
        return encryptedString.Substring(4, encryptedString.Length - 5);
    }
    
    public async Task<Dictionary<string, object>> GetSecurityAuditReportAsync()
    {
        var report = new Dictionary<string, object>();
        
        // Configuration security check
        var configSecurity = await CheckConfigurationSecurityAsync();
        report["configuration_security"] = configSecurity;
        
        // Encryption status
        var encryptionStatus = await CheckEncryptionStatusAsync();
        report["encryption_status"] = encryptionStatus;
        
        // Security recommendations
        var recommendations = await GenerateSecurityRecommendationsAsync();
        report["recommendations"] = recommendations;
        
        return report;
    }
    
    private async Task<Dictionary<string, object>> CheckConfigurationSecurityAsync()
    {
        var security = new Dictionary<string, object>();
        
        // Check for hardcoded secrets
        var config = _parser.ParseFile("config/app.tsk");
        var hasHardcodedSecrets = config.Values.Any(v => 
            v.ToString().Contains("password") || 
            v.ToString().Contains("secret") || 
            v.ToString().Contains("key"));
        
        security["has_hardcoded_secrets"] = hasHardcodedSecrets;
        security["recommendation"] = hasHardcodedSecrets ? 
            "Use encrypted configuration values" : "Configuration security is good";
        
        return security;
    }
    
    private async Task<Dictionary<string, object>> CheckEncryptionStatusAsync()
    {
        var status = new Dictionary<string, object>();
        
        status["encryption_enabled"] = true;
        status["algorithm"] = "AES-256";
        status["key_rotation_enabled"] = false;
        status["recommendation"] = "Enable key rotation for enhanced security";
        
        return status;
    }
    
    private async Task<List<string>> GenerateSecurityRecommendationsAsync()
    {
        var recommendations = new List<string>
        {
            "Enable HTTPS for all communications",
            "Implement rate limiting",
            "Use secure session management",
            "Enable audit logging",
            "Regular security updates",
            "Implement input validation",
            "Use parameterized queries",
            "Enable CORS properly"
        };
        
        return recommendations;
    }
}
```

## 🛠️ Real-World Security Scenarios
- **API security**: Secure REST APIs with JWT authentication
- **Database security**: Encrypt sensitive data and use secure connections
- **Configuration security**: Encrypt configuration files and secrets
- **Session security**: Secure session management and CSRF protection

## 🧩 Best Practices
- Use HTTPS for all communications
- Implement proper authentication and authorization
- Encrypt sensitive data at rest and in transit
- Use secure configuration management
- Implement audit logging
- Regular security updates and patches

## 🏁 You're Ready!

You can now:
- Implement secure authentication and authorization
- Use encryption for sensitive data
- Manage secure configurations
- Follow security best practices

**Next:** [Data Validation](031-data-validation-csharp.md)

---

**"We don't bow to any king" - Your security mastery, your protection excellence, your defense strength.**

Secure by design. Protected by default. 🔒 