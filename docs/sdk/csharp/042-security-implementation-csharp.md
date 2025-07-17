# Security Implementation in C# TuskLang

## Overview

Security is paramount in any application. This guide covers authentication, authorization, encryption, secure configuration, and security best practices for C# TuskLang applications.

## 🔐 Authentication

### JWT Authentication Service

```csharp
public class JwtAuthenticationService
{
    private readonly TSKConfig _config;
    private readonly IUserService _userService;
    private readonly ILogger<JwtAuthenticationService> _logger;
    private readonly IPasswordHasher _passwordHasher;
    
    public JwtAuthenticationService(
        TSKConfig config,
        IUserService userService,
        ILogger<JwtAuthenticationService> logger,
        IPasswordHasher passwordHasher)
    {
        _config = config;
        _userService = userService;
        _logger = logger;
        _passwordHasher = passwordHasher;
    }
    
    public async Task<AuthenticationResult> AuthenticateAsync(string email, string password)
    {
        try
        {
            // Get user by email
            var user = await _userService.GetUserByEmailAsync(email);
            if (user == null)
            {
                _logger.LogWarning("Authentication failed: User not found for email {Email}", email);
                return AuthenticationResult.Failure("Invalid credentials");
            }
            
            // Verify password
            if (!_passwordHasher.VerifyPassword(password, user.PasswordHash))
            {
                _logger.LogWarning("Authentication failed: Invalid password for user {UserId}", user.Id);
                return AuthenticationResult.Failure("Invalid credentials");
            }
            
            // Check if user is active
            if (!user.IsActive)
            {
                _logger.LogWarning("Authentication failed: Inactive user {UserId}", user.Id);
                return AuthenticationResult.Failure("Account is deactivated");
            }
            
            // Generate JWT token
            var token = await GenerateJwtTokenAsync(user);
            
            // Record successful login
            await RecordLoginAsync(user.Id, true);
            
            _logger.LogInformation("User {UserId} authenticated successfully", user.Id);
            
            return AuthenticationResult.Success(token, user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication error for email {Email}", email);
            return AuthenticationResult.Failure("Authentication failed");
        }
    }
    
    public async Task<AuthenticationResult> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            var principal = ValidateRefreshToken(refreshToken);
            var userId = int.Parse(principal.FindFirst("user_id")?.Value ?? "0");
            
            if (userId == 0)
            {
                return AuthenticationResult.Failure("Invalid refresh token");
            }
            
            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null || !user.IsActive)
            {
                return AuthenticationResult.Failure("User not found or inactive");
            }
            
            var newToken = await GenerateJwtTokenAsync(user);
            
            return AuthenticationResult.Success(newToken, user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token refresh failed");
            return AuthenticationResult.Failure("Token refresh failed");
        }
    }
    
    private async Task<string> GenerateJwtTokenAsync(UserDto user)
    {
        var jwtSecret = _config.Get<string>("security.jwt_secret");
        var tokenExpiration = _config.Get<int>("security.token_expiration_minutes", 60);
        var refreshTokenExpiration = _config.Get<int>("security.refresh_token_expiration_days", 7);
        
        if (string.IsNullOrEmpty(jwtSecret))
        {
            throw new InvalidOperationException("JWT secret not configured");
        }
        
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(jwtSecret);
        
        var claims = new List<Claim>
        {
            new Claim("user_id", user.Id.ToString()),
            new Claim("email", user.Email),
            new Claim("role", user.Role),
            new Claim("jti", Guid.NewGuid().ToString()),
            new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(tokenExpiration),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature),
            Issuer = _config.Get<string>("security.jwt_issuer", "tusklang-app"),
            Audience = _config.Get<string>("security.jwt_audience", "tusklang-users")
        };
        
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
    
    private ClaimsPrincipal ValidateRefreshToken(string refreshToken)
    {
        var jwtSecret = _config.Get<string>("security.jwt_secret");
        var key = Encoding.ASCII.GetBytes(jwtSecret);
        
        var tokenHandler = new JwtSecurityTokenHandler();
        
        tokenHandler.ValidateToken(refreshToken, new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        }, out SecurityToken validatedToken);
        
        return tokenHandler.ValidateToken(refreshToken, new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        }, out _);
    }
    
    private async Task RecordLoginAsync(int userId, bool success)
    {
        try
        {
            await _userService.RecordLoginAttemptAsync(userId, success, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record login attempt for user {UserId}", userId);
        }
    }
}

public class AuthenticationResult
{
    public bool IsSuccess { get; private set; }
    public string? Token { get; private set; }
    public UserDto? User { get; private set; }
    public string? ErrorMessage { get; private set; }
    
    public static AuthenticationResult Success(string token, UserDto user)
    {
        return new AuthenticationResult
        {
            IsSuccess = true,
            Token = token,
            User = user
        };
    }
    
    public static AuthenticationResult Failure(string errorMessage)
    {
        return new AuthenticationResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}
```

### Password Security

```csharp
public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}

public class BCryptPasswordHasher : IPasswordHasher
{
    private readonly ILogger<BCryptPasswordHasher> _logger;
    
    public BCryptPasswordHasher(ILogger<BCryptPasswordHasher> logger)
    {
        _logger = logger;
    }
    
    public string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            throw new ArgumentException("Password cannot be null or empty", nameof(password));
        }
        
        // Use BCrypt with work factor 12 (good balance of security and performance)
        return BCrypt.Net.BCrypt.HashPassword(password, 12);
    }
    
    public bool VerifyPassword(string password, string hash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash))
        {
            return false;
        }
        
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Password verification failed");
            return false;
        }
    }
}

public class PasswordPolicyValidator
{
    private readonly TSKConfig _config;
    private readonly ILogger<PasswordPolicyValidator> _logger;
    
    public PasswordPolicyValidator(TSKConfig config, ILogger<PasswordPolicyValidator> logger)
    {
        _config = config;
        _logger = logger;
    }
    
    public ValidationResult ValidatePassword(string password)
    {
        var result = new ValidationResult();
        
        var minLength = _config.Get<int>("security.password.min_length", 8);
        var requireUppercase = _config.Get<bool>("security.password.require_uppercase", true);
        var requireLowercase = _config.Get<bool>("security.password.require_lowercase", true);
        var requireDigit = _config.Get<bool>("security.password.require_digit", true);
        var requireSpecialChar = _config.Get<bool>("security.password.require_special_char", true);
        
        if (password.Length < minLength)
        {
            result.AddError($"Password must be at least {minLength} characters long");
        }
        
        if (requireUppercase && !password.Any(char.IsUpper))
        {
            result.AddError("Password must contain at least one uppercase letter");
        }
        
        if (requireLowercase && !password.Any(char.IsLower))
        {
            result.AddError("Password must contain at least one lowercase letter");
        }
        
        if (requireDigit && !password.Any(char.IsDigit))
        {
            result.AddError("Password must contain at least one digit");
        }
        
        if (requireSpecialChar && !password.Any(c => !char.IsLetterOrDigit(c)))
        {
            result.AddError("Password must contain at least one special character");
        }
        
        return result;
    }
}
```

## 🛡️ Authorization

### Role-Based Authorization

```csharp
public class AuthorizationService
{
    private readonly ILogger<AuthorizationService> _logger;
    private readonly TSKConfig _config;
    private readonly IUserService _userService;
    
    public AuthorizationService(ILogger<AuthorizationService> logger, TSKConfig config, IUserService userService)
    {
        _logger = logger;
        _config = config;
        _userService = userService;
    }
    
    public async Task<bool> AuthorizeAsync(int userId, string requiredRole)
    {
        try
        {
            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return false;
            }
            
            return HasRole(user.Role, requiredRole);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authorization failed for user {UserId} with role {RequiredRole}", userId, requiredRole);
            return false;
        }
    }
    
    public async Task<bool> AuthorizeResourceAsync(int userId, string resourceType, int resourceId)
    {
        try
        {
            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return false;
            }
            
            // Admin users can access all resources
            if (HasRole(user.Role, "admin"))
            {
                return true;
            }
            
            // Check resource-specific permissions
            return await CheckResourcePermissionAsync(user, resourceType, resourceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Resource authorization failed for user {UserId} on {ResourceType} {ResourceId}", 
                userId, resourceType, resourceId);
            return false;
        }
    }
    
    private bool HasRole(string userRole, string requiredRole)
    {
        // Role hierarchy: admin > manager > user
        var roleHierarchy = new Dictionary<string, int>
        {
            ["user"] = 1,
            ["manager"] = 2,
            ["admin"] = 3
        };
        
        if (!roleHierarchy.ContainsKey(userRole) || !roleHierarchy.ContainsKey(requiredRole))
        {
            return false;
        }
        
        return roleHierarchy[userRole] >= roleHierarchy[requiredRole];
    }
    
    private async Task<bool> CheckResourcePermissionAsync(UserDto user, string resourceType, int resourceId)
    {
        // Implementation depends on your resource permission model
        // This is a simplified example
        switch (resourceType.ToLower())
        {
            case "user":
                // Users can only access their own data
                return user.Id == resourceId;
                
            case "document":
                // Check document ownership or shared permissions
                return await CheckDocumentPermissionAsync(user.Id, resourceId);
                
            default:
                return false;
        }
    }
    
    private async Task<bool> CheckDocumentPermissionAsync(int userId, int documentId)
    {
        // Implementation would check document ownership or shared permissions
        // This is a placeholder
        await Task.CompletedTask;
        return false;
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class AuthorizeAttribute : Attribute
{
    public string Role { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    
    public AuthorizeAttribute(string role = "")
    {
        Role = role;
    }
    
    public AuthorizeAttribute(string role, string resourceType)
    {
        Role = role;
        ResourceType = resourceType;
    }
}
```

### Permission-Based Authorization

```csharp
public class PermissionService
{
    private readonly ILogger<PermissionService> _logger;
    private readonly TSKConfig _config;
    private readonly IDbConnection _connection;
    
    public PermissionService(ILogger<PermissionService> logger, TSKConfig config, IDbConnection connection)
    {
        _logger = logger;
        _config = config;
        _connection = connection;
    }
    
    public async Task<bool> HasPermissionAsync(int userId, string permission)
    {
        try
        {
            var query = @"
                SELECT COUNT(*) 
                FROM user_permissions up
                JOIN permissions p ON up.permission_id = p.id
                WHERE up.user_id = @UserId AND p.name = @Permission";
            
            var parameters = new { UserId = userId, Permission = permission };
            var count = await _connection.ExecuteScalarAsync<int>(query, parameters);
            
            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Permission check failed for user {UserId} with permission {Permission}", 
                userId, permission);
            return false;
        }
    }
    
    public async Task<List<string>> GetUserPermissionsAsync(int userId)
    {
        try
        {
            var query = @"
                SELECT p.name
                FROM user_permissions up
                JOIN permissions p ON up.permission_id = p.id
                WHERE up.user_id = @UserId";
            
            var parameters = new { UserId = userId };
            var permissions = await _connection.QueryAsync<string>(query, parameters);
            
            return permissions.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get permissions for user {UserId}", userId);
            return new List<string>();
        }
    }
    
    public async Task<bool> GrantPermissionAsync(int userId, string permission)
    {
        try
        {
            // First, ensure the permission exists
            var permissionId = await GetOrCreatePermissionAsync(permission);
            
            // Check if user already has this permission
            var existingQuery = @"
                SELECT COUNT(*) 
                FROM user_permissions 
                WHERE user_id = @UserId AND permission_id = @PermissionId";
            
            var existingParameters = new { UserId = userId, PermissionId = permissionId };
            var existingCount = await _connection.ExecuteScalarAsync<int>(existingQuery, existingParameters);
            
            if (existingCount > 0)
            {
                return true; // Permission already granted
            }
            
            // Grant the permission
            var grantQuery = @"
                INSERT INTO user_permissions (user_id, permission_id, granted_at)
                VALUES (@UserId, @PermissionId, @GrantedAt)";
            
            var grantParameters = new { UserId = userId, PermissionId = permissionId, GrantedAt = DateTime.UtcNow };
            await _connection.ExecuteAsync(grantQuery, grantParameters);
            
            _logger.LogInformation("Permission {Permission} granted to user {UserId}", permission, userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to grant permission {Permission} to user {UserId}", permission, userId);
            return false;
        }
    }
    
    public async Task<bool> RevokePermissionAsync(int userId, string permission)
    {
        try
        {
            var permissionId = await GetPermissionIdAsync(permission);
            if (permissionId == 0)
            {
                return false; // Permission doesn't exist
            }
            
            var query = @"
                DELETE FROM user_permissions 
                WHERE user_id = @UserId AND permission_id = @PermissionId";
            
            var parameters = new { UserId = userId, PermissionId = permissionId };
            var rowsAffected = await _connection.ExecuteAsync(query, parameters);
            
            if (rowsAffected > 0)
            {
                _logger.LogInformation("Permission {Permission} revoked from user {UserId}", permission, userId);
            }
            
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke permission {Permission} from user {UserId}", permission, userId);
            return false;
        }
    }
    
    private async Task<int> GetOrCreatePermissionAsync(string permission)
    {
        var permissionId = await GetPermissionIdAsync(permission);
        
        if (permissionId == 0)
        {
            var createQuery = @"
                INSERT INTO permissions (name, description, created_at)
                VALUES (@Name, @Description, @CreatedAt)
                RETURNING id";
            
            var parameters = new { Name = permission, Description = $"Permission: {permission}", CreatedAt = DateTime.UtcNow };
            permissionId = await _connection.ExecuteScalarAsync<int>(createQuery, parameters);
        }
        
        return permissionId;
    }
    
    private async Task<int> GetPermissionIdAsync(string permission)
    {
        var query = "SELECT id FROM permissions WHERE name = @Name";
        var parameters = new { Name = permission };
        return await _connection.ExecuteScalarAsync<int>(query, parameters);
    }
}
```

## 🔒 Encryption

### Data Encryption Service

```csharp
public class EncryptionService
{
    private readonly ILogger<EncryptionService> _logger;
    private readonly TSKConfig _config;
    private readonly byte[] _key;
    private readonly byte[] _iv;
    
    public EncryptionService(ILogger<EncryptionService> logger, TSKConfig config)
    {
        _logger = logger;
        _config = config;
        
        var encryptionKey = _config.Get<string>("security.encryption_key");
        if (string.IsNullOrEmpty(encryptionKey) || encryptionKey.Length < 32)
        {
            throw new InvalidOperationException("Encryption key must be at least 32 characters long");
        }
        
        _key = Encoding.UTF8.GetBytes(encryptionKey.Substring(0, 32));
        _iv = Encoding.UTF8.GetBytes(encryptionKey.Substring(0, 16));
    }
    
    public async Task<string> EncryptAsync(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
        {
            return plaintext;
        }
        
        try
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            
            using var encryptor = aes.CreateEncryptor();
            using var ms = new MemoryStream();
            using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
            using var writer = new StreamWriter(cs);
            
            await writer.WriteAsync(plaintext);
            await writer.FlushAsync();
            cs.FlushFinalBlock();
            
            var encryptedBytes = ms.ToArray();
            return Convert.ToBase64String(encryptedBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Encryption failed");
            throw new EncryptionException("Failed to encrypt data", ex);
        }
    }
    
    public async Task<string> DecryptAsync(string ciphertext)
    {
        if (string.IsNullOrEmpty(ciphertext))
        {
            return ciphertext;
        }
        
        try
        {
            var encryptedBytes = Convert.FromBase64String(ciphertext);
            
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            
            using var decryptor = aes.CreateDecryptor();
            using var ms = new MemoryStream(encryptedBytes);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var reader = new StreamReader(cs);
            
            return await reader.ReadToEndAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Decryption failed");
            throw new EncryptionException("Failed to decrypt data", ex);
        }
    }
    
    public async Task<byte[]> EncryptBytesAsync(byte[] data)
    {
        if (data == null || data.Length == 0)
        {
            return data;
        }
        
        try
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            
            using var encryptor = aes.CreateEncryptor();
            using var ms = new MemoryStream();
            using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
            
            await cs.WriteAsync(data);
            await cs.FlushAsync();
            cs.FlushFinalBlock();
            
            return ms.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Byte encryption failed");
            throw new EncryptionException("Failed to encrypt bytes", ex);
        }
    }
    
    public async Task<byte[]> DecryptBytesAsync(byte[] encryptedData)
    {
        if (encryptedData == null || encryptedData.Length == 0)
        {
            return encryptedData;
        }
        
        try
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            
            using var decryptor = aes.CreateDecryptor();
            using var ms = new MemoryStream(encryptedData);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var resultMs = new MemoryStream();
            
            await cs.CopyToAsync(resultMs);
            return resultMs.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Byte decryption failed");
            throw new EncryptionException("Failed to decrypt bytes", ex);
        }
    }
}

public class EncryptionException : Exception
{
    public EncryptionException(string message) : base(message)
    {
    }
    
    public EncryptionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
```

### Secure Configuration

```csharp
public class SecureConfigurationService
{
    private readonly ILogger<SecureConfigurationService> _logger;
    private readonly TSKConfig _config;
    private readonly EncryptionService _encryptionService;
    
    public SecureConfigurationService(
        ILogger<SecureConfigurationService> logger,
        TSKConfig config,
        EncryptionService encryptionService)
    {
        _logger = logger;
        _config = config;
        _encryptionService = encryptionService;
    }
    
    public async Task<string> GetSecureValueAsync(string key, string? defaultValue = null)
    {
        try
        {
            var encryptedValue = _config.Get<string>(key);
            if (string.IsNullOrEmpty(encryptedValue))
            {
                return defaultValue ?? string.Empty;
            }
            
            // Check if value is encrypted (starts with ENC:)
            if (encryptedValue.StartsWith("ENC:"))
            {
                var ciphertext = encryptedValue.Substring(4);
                return await _encryptionService.DecryptAsync(ciphertext);
            }
            
            return encryptedValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get secure value for key {Key}", key);
            return defaultValue ?? string.Empty;
        }
    }
    
    public async Task SetSecureValueAsync(string key, string value)
    {
        try
        {
            var encryptedValue = await _encryptionService.EncryptAsync(value);
            _config.Set(key, $"ENC:{encryptedValue}");
            
            _logger.LogInformation("Secure value set for key {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set secure value for key {Key}", key);
            throw;
        }
    }
    
    public async Task<bool> IsEncryptedAsync(string key)
    {
        var value = _config.Get<string>(key);
        return !string.IsNullOrEmpty(value) && value.StartsWith("ENC:");
    }
    
    public async Task<List<string>> GetEncryptedKeysAsync()
    {
        var encryptedKeys = new List<string>();
        var allKeys = _config.GetAllKeys();
        
        foreach (var key in allKeys)
        {
            if (await IsEncryptedAsync(key))
            {
                encryptedKeys.Add(key);
            }
        }
        
        return encryptedKeys;
    }
}
```

## 🚨 Security Monitoring

### Security Event Logger

```csharp
public class SecurityEventLogger
{
    private readonly ILogger<SecurityEventLogger> _logger;
    private readonly TSKConfig _config;
    private readonly IDbConnection _connection;
    
    public SecurityEventLogger(ILogger<SecurityEventLogger> logger, TSKConfig config, IDbConnection connection)
    {
        _logger = logger;
        _config = config;
        _connection = connection;
    }
    
    public async Task LogSecurityEventAsync(SecurityEvent securityEvent)
    {
        try
        {
            // Log to database
            await LogToDatabaseAsync(securityEvent);
            
            // Log to application logs
            LogToApplicationLogs(securityEvent);
            
            // Send alerts for critical events
            if (securityEvent.Severity == SecurityEventSeverity.Critical)
            {
                await SendSecurityAlertAsync(securityEvent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log security event: {EventType}", securityEvent.EventType);
        }
    }
    
    private async Task LogToDatabaseAsync(SecurityEvent securityEvent)
    {
        var query = @"
            INSERT INTO security_events (
                event_type, user_id, ip_address, user_agent, severity, 
                description, metadata, created_at
            ) VALUES (
                @EventType, @UserId, @IpAddress, @UserAgent, @Severity,
                @Description, @Metadata, @CreatedAt
            )";
        
        var parameters = new
        {
            securityEvent.EventType,
            securityEvent.UserId,
            securityEvent.IpAddress,
            securityEvent.UserAgent,
            Severity = securityEvent.Severity.ToString(),
            securityEvent.Description,
            Metadata = JsonSerializer.Serialize(securityEvent.Metadata),
            CreatedAt = DateTime.UtcNow
        };
        
        await _connection.ExecuteAsync(query, parameters);
    }
    
    private void LogToApplicationLogs(SecurityEvent securityEvent)
    {
        var logLevel = securityEvent.Severity switch
        {
            SecurityEventSeverity.Low => LogLevel.Information,
            SecurityEventSeverity.Medium => LogLevel.Warning,
            SecurityEventSeverity.High => LogLevel.Error,
            SecurityEventSeverity.Critical => LogLevel.Critical,
            _ => LogLevel.Information
        };
        
        _logger.Log(logLevel, "Security Event: {EventType} - {Description} - User: {UserId} - IP: {IpAddress}",
            securityEvent.EventType, securityEvent.Description, securityEvent.UserId, securityEvent.IpAddress);
    }
    
    private async Task SendSecurityAlertAsync(SecurityEvent securityEvent)
    {
        var alertUrl = _config.Get<string>("security.alert_webhook_url");
        if (string.IsNullOrEmpty(alertUrl))
        {
            return;
        }
        
        try
        {
            using var client = new HttpClient();
            var alert = new SecurityAlert
            {
                EventType = securityEvent.EventType,
                Severity = securityEvent.Severity.ToString(),
                Description = securityEvent.Description,
                UserId = securityEvent.UserId,
                IpAddress = securityEvent.IpAddress,
                Timestamp = DateTime.UtcNow,
                ServiceName = _config.Get<string>("app.name", "unknown"),
                Environment = _config.Get<string>("app.environment", "unknown")
            };
            
            var json = JsonSerializer.Serialize(alert);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await client.PostAsync(alertUrl, content);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send security alert for event {EventType}", securityEvent.EventType);
        }
    }
}

public class SecurityEvent
{
    public string EventType { get; set; } = string.Empty;
    public int? UserId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public SecurityEventSeverity Severity { get; set; }
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public enum SecurityEventSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public class SecurityAlert
{
    public string EventType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? UserId { get; set; }
    public string? IpAddress { get; set; }
    public DateTime Timestamp { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
}
```

## 📝 Summary

This guide covered comprehensive security implementation strategies for C# TuskLang applications:

- **Authentication**: JWT-based authentication with password security and policy validation
- **Authorization**: Role-based and permission-based authorization systems
- **Encryption**: Data encryption service for sensitive information
- **Secure Configuration**: Encrypted configuration values and secure key management
- **Security Monitoring**: Security event logging and alerting systems

These security implementation strategies ensure your C# TuskLang applications are secure, compliant, and protected against various security threats. 