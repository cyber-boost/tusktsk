# API Design in C# TuskLang

## Overview

API design is crucial for building scalable and maintainable applications. This guide covers RESTful APIs, GraphQL, gRPC, authentication, versioning, and best practices for C# TuskLang applications.

## 🌐 RESTful API Design

### Controller Structure

```csharp
[ApiController]
[Route("api/v1/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;
    private readonly TSKConfig _config;
    
    public UsersController(IUserService userService, ILogger<UsersController> logger, TSKConfig config)
    {
        _userService = userService;
        _logger = logger;
        _config = config;
    }
    
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<UserDto>>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null)
    {
        try
        {
            var maxPageSize = _config.Get<int>("api.max_page_size", 100);
            pageSize = Math.Min(pageSize, maxPageSize);
            
            var users = await _userService.GetUsersAsync(page, pageSize, search);
            
            return Ok(new PaginatedResponse<UserDto>
            {
                Data = users.Items,
                Page = page,
                PageSize = pageSize,
                TotalCount = users.TotalCount,
                TotalPages = (int)Math.Ceiling((double)users.TotalCount / pageSize)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get users");
            return StatusCode(500, new ErrorResponse("Internal server error"));
        }
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        try
        {
            var user = await _userService.GetUserByIdAsync(id);
            
            if (user == null)
            {
                return NotFound(new ErrorResponse("User not found"));
            }
            
            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user {UserId}", id);
            return StatusCode(500, new ErrorResponse("Internal server error"));
        }
    }
    
    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            var validationResult = await ValidateCreateUserRequestAsync(request);
            if (!validationResult.IsValid)
            {
                return BadRequest(new ErrorResponse(validationResult.Errors));
            }
            
            var user = await _userService.CreateUserAsync(request);
            
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user");
            return StatusCode(500, new ErrorResponse("Internal server error"));
        }
    }
    
    [HttpPut("{id}")]
    public async Task<ActionResult<UserDto>> UpdateUser(int id, [FromBody] UpdateUserRequest request)
    {
        try
        {
            var validationResult = await ValidateUpdateUserRequestAsync(request);
            if (!validationResult.IsValid)
            {
                return BadRequest(new ErrorResponse(validationResult.Errors));
            }
            
            var user = await _userService.UpdateUserAsync(id, request);
            
            if (user == null)
            {
                return NotFound(new ErrorResponse("User not found"));
            }
            
            return Ok(user);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user {UserId}", id);
            return StatusCode(500, new ErrorResponse("Internal server error"));
        }
    }
    
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteUser(int id)
    {
        try
        {
            var deleted = await _userService.DeleteUserAsync(id);
            
            if (!deleted)
            {
                return NotFound(new ErrorResponse("User not found"));
            }
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete user {UserId}", id);
            return StatusCode(500, new ErrorResponse("Internal server error"));
        }
    }
    
    private async Task<ValidationResult> ValidateCreateUserRequestAsync(CreateUserRequest request)
    {
        var result = new ValidationResult();
        
        if (string.IsNullOrEmpty(request.Email))
        {
            result.AddError("Email is required");
        }
        else if (!IsValidEmail(request.Email))
        {
            result.AddError("Invalid email format");
        }
        
        if (string.IsNullOrEmpty(request.Password))
        {
            result.AddError("Password is required");
        }
        else if (request.Password.Length < 8)
        {
            result.AddError("Password must be at least 8 characters long");
        }
        
        return result;
    }
    
    private async Task<ValidationResult> ValidateUpdateUserRequestAsync(UpdateUserRequest request)
    {
        var result = new ValidationResult();
        
        if (!string.IsNullOrEmpty(request.Email) && !IsValidEmail(request.Email))
        {
            result.AddError("Invalid email format");
        }
        
        return result;
    }
    
    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
```

### Response Models

```csharp
public class PaginatedResponse<T>
{
    public List<T> Data { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}

public class ErrorResponse
{
    public string Message { get; set; }
    public List<string> Errors { get; set; } = new();
    public string? TraceId { get; set; }
    
    public ErrorResponse(string message)
    {
        Message = message;
    }
    
    public ErrorResponse(List<string> errors)
    {
        Message = "Validation failed";
        Errors = errors;
    }
}

public class UserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

public class UpdateUserRequest
{
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}
```

### Service Layer

```csharp
public interface IUserService
{
    Task<PaginatedResult<UserDto>> GetUsersAsync(int page, int pageSize, string? search);
    Task<UserDto?> GetUserByIdAsync(int id);
    Task<UserDto> CreateUserAsync(CreateUserRequest request);
    Task<UserDto?> UpdateUserAsync(int id, UpdateUserRequest request);
    Task<bool> DeleteUserAsync(int id);
}

public class UserService : IUserService
{
    private readonly IDbConnection _connection;
    private readonly ILogger<UserService> _logger;
    private readonly TSKConfig _config;
    private readonly IPasswordHasher _passwordHasher;
    
    public UserService(IDbConnection connection, ILogger<UserService> logger, 
        TSKConfig config, IPasswordHasher passwordHasher)
    {
        _connection = connection;
        _logger = logger;
        _config = config;
        _passwordHasher = passwordHasher;
    }
    
    public async Task<PaginatedResult<UserDto>> GetUsersAsync(int page, int pageSize, string? search)
    {
        var offset = (page - 1) * pageSize;
        
        var whereClause = "";
        var parameters = new DynamicParameters();
        
        if (!string.IsNullOrEmpty(search))
        {
            whereClause = "WHERE email ILIKE @Search OR first_name ILIKE @Search OR last_name ILIKE @Search";
            parameters.Add("@Search", $"%{search}%");
        }
        
        var countQuery = $@"
            SELECT COUNT(*) 
            FROM users 
            {whereClause}";
        
        var totalCount = await _connection.ExecuteScalarAsync<int>(countQuery, parameters);
        
        var query = $@"
            SELECT id, email, first_name, last_name, created_at, updated_at
            FROM users 
            {whereClause}
            ORDER BY created_at DESC
            LIMIT @PageSize OFFSET @Offset";
        
        parameters.Add("@PageSize", pageSize);
        parameters.Add("@Offset", offset);
        
        var users = await _connection.QueryAsync<UserDto>(query, parameters);
        
        return new PaginatedResult<UserDto>
        {
            Items = users.ToList(),
            TotalCount = totalCount
        };
    }
    
    public async Task<UserDto?> GetUserByIdAsync(int id)
    {
        var query = @"
            SELECT id, email, first_name, last_name, created_at, updated_at
            FROM users 
            WHERE id = @Id";
        
        var parameters = new { Id = id };
        
        return await _connection.QueryFirstOrDefaultAsync<UserDto>(query, parameters);
    }
    
    public async Task<UserDto> CreateUserAsync(CreateUserRequest request)
    {
        // Check if user already exists
        var existingUser = await GetUserByEmailAsync(request.Email);
        if (existingUser != null)
        {
            throw new ValidationException("User with this email already exists");
        }
        
        // Hash password
        var hashedPassword = _passwordHasher.HashPassword(request.Password);
        
        var query = @"
            INSERT INTO users (email, password_hash, first_name, last_name, created_at, updated_at)
            VALUES (@Email, @PasswordHash, @FirstName, @LastName, @CreatedAt, @UpdatedAt)
            RETURNING id, email, first_name, last_name, created_at, updated_at";
        
        var parameters = new
        {
            request.Email,
            PasswordHash = hashedPassword,
            request.FirstName,
            request.LastName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        return await _connection.QueryFirstAsync<UserDto>(query, parameters);
    }
    
    public async Task<UserDto?> UpdateUserAsync(int id, UpdateUserRequest request)
    {
        var existingUser = await GetUserByIdAsync(id);
        if (existingUser == null)
        {
            return null;
        }
        
        var updateFields = new List<string>();
        var parameters = new DynamicParameters();
        parameters.Add("@Id", id);
        
        if (!string.IsNullOrEmpty(request.Email))
        {
            updateFields.Add("email = @Email");
            parameters.Add("@Email", request.Email);
        }
        
        if (!string.IsNullOrEmpty(request.FirstName))
        {
            updateFields.Add("first_name = @FirstName");
            parameters.Add("@FirstName", request.FirstName);
        }
        
        if (!string.IsNullOrEmpty(request.LastName))
        {
            updateFields.Add("last_name = @LastName");
            parameters.Add("@LastName", request.LastName);
        }
        
        if (!updateFields.Any())
        {
            return existingUser;
        }
        
        updateFields.Add("updated_at = @UpdatedAt");
        parameters.Add("@UpdatedAt", DateTime.UtcNow);
        
        var query = $@"
            UPDATE users 
            SET {string.Join(", ", updateFields)}
            WHERE id = @Id
            RETURNING id, email, first_name, last_name, created_at, updated_at";
        
        return await _connection.QueryFirstOrDefaultAsync<UserDto>(query, parameters);
    }
    
    public async Task<bool> DeleteUserAsync(int id)
    {
        var query = "DELETE FROM users WHERE id = @Id";
        var parameters = new { Id = id };
        
        var rowsAffected = await _connection.ExecuteAsync(query, parameters);
        return rowsAffected > 0;
    }
    
    private async Task<UserDto?> GetUserByEmailAsync(string email)
    {
        var query = @"
            SELECT id, email, first_name, last_name, created_at, updated_at
            FROM users 
            WHERE email = @Email";
        
        var parameters = new { Email = email };
        
        return await _connection.QueryFirstOrDefaultAsync<UserDto>(query, parameters);
    }
}

public class PaginatedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
}
```

## 🔐 Authentication & Authorization

### JWT Authentication

```csharp
public class JwtAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly TSKConfig _config;
    private readonly IUserService _userService;
    
    public JwtAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock,
        TSKConfig config,
        IUserService userService)
        : base(options, logger, encoder, clock)
    {
        _config = config;
        _userService = userService;
    }
    
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey("Authorization"))
        {
            return AuthenticateResult.Fail("Authorization header not found");
        }
        
        var authHeader = Request.Headers["Authorization"].ToString();
        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.Fail("Bearer token not found");
        }
        
        var token = authHeader.Substring("Bearer ".Length).Trim();
        
        try
        {
            var principal = ValidateToken(token);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            
            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            return AuthenticateResult.Fail($"Token validation failed: {ex.Message}");
        }
    }
    
    private ClaimsPrincipal ValidateToken(string token)
    {
        var jwtSecret = _config.Get<string>("security.jwt_secret");
        if (string.IsNullOrEmpty(jwtSecret))
        {
            throw new InvalidOperationException("JWT secret not configured");
        }
        
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(jwtSecret);
        
        tokenHandler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        }, out SecurityToken validatedToken);
        
        var jwtToken = (JwtSecurityToken)validatedToken;
        var userId = int.Parse(jwtToken.Claims.First(x => x.Type == "user_id").Value);
        
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, jwtToken.Claims.First(x => x.Type == "email").Value),
            new Claim(ClaimTypes.Role, jwtToken.Claims.First(x => x.Type == "role").Value)
        };
        
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        return new ClaimsPrincipal(identity);
    }
}

public class JwtTokenService
{
    private readonly TSKConfig _config;
    private readonly IUserService _userService;
    
    public JwtTokenService(TSKConfig config, IUserService userService)
    {
        _config = config;
        _userService = userService;
    }
    
    public async Task<string> GenerateTokenAsync(int userId)
    {
        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
        {
            throw new ArgumentException("User not found");
        }
        
        var jwtSecret = _config.Get<string>("security.jwt_secret");
        var tokenExpiration = _config.Get<int>("security.token_expiration_minutes", 60);
        
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(jwtSecret);
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("user_id", userId.ToString()),
                new Claim("email", user.Email),
                new Claim("role", "user")
            }),
            Expires = DateTime.UtcNow.AddMinutes(tokenExpiration),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };
        
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
```

### Role-Based Authorization

```csharp
[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/v1/[controller]")]
public class AdminController : ControllerBase
{
    private readonly ILogger<AdminController> _logger;
    private readonly TSKConfig _config;
    
    public AdminController(ILogger<AdminController> logger, TSKConfig config)
    {
        _logger = logger;
        _config = config;
    }
    
    [HttpGet("system-info")]
    public ActionResult<SystemInfoDto> GetSystemInfo()
    {
        var systemInfo = new SystemInfoDto
        {
            Version = _config.Get<string>("app.version", "1.0.0"),
            Environment = _config.Get<string>("app.environment", "development"),
            DatabaseConnection = _config.Get<string>("database.connection_string", ""),
            ApiBaseUrl = _config.Get<string>("api.base_url", ""),
            Timestamp = DateTime.UtcNow
        };
        
        return Ok(systemInfo);
    }
    
    [HttpPost("config-reload")]
    public async Task<ActionResult> ReloadConfiguration()
    {
        try
        {
            // Implementation depends on your configuration reload mechanism
            await Task.Delay(100); // Simulate reload
            
            _logger.LogInformation("Configuration reloaded by admin");
            return Ok(new { message = "Configuration reloaded successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reload configuration");
            return StatusCode(500, new ErrorResponse("Failed to reload configuration"));
        }
    }
}

public class SystemInfoDto
{
    public string Version { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public string DatabaseConnection { get; set; } = string.Empty;
    public string ApiBaseUrl { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
```

## 🔄 API Versioning

### Versioning Strategies

```csharp
[ApiController]
[ApiVersion("1.0")]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;
    
    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }
    
    [HttpGet]
    [MapToApiVersion("1.0")]
    public async Task<ActionResult<List<UserDto>>> GetUsersV1()
    {
        // Version 1.0 implementation
        var users = await _userService.GetUsersAsync(1, 100, null);
        return Ok(users.Items);
    }
    
    [HttpGet]
    [MapToApiVersion("2.0")]
    public async Task<ActionResult<PaginatedResponse<UserDto>>> GetUsersV2(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null)
    {
        // Version 2.0 implementation with pagination
        var users = await _userService.GetUsersAsync(page, pageSize, search);
        
        return Ok(new PaginatedResponse<UserDto>
        {
            Data = users.Items,
            Page = page,
            PageSize = pageSize,
            TotalCount = users.TotalCount,
            TotalPages = (int)Math.Ceiling((double)users.TotalCount / pageSize)
        });
    }
    
    [HttpGet("{id}")]
    [MapToApiVersion("1.0")]
    public async Task<ActionResult<UserDto>> GetUserV1(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        
        if (user == null)
        {
            return NotFound();
        }
        
        return Ok(user);
    }
    
    [HttpGet("{id}")]
    [MapToApiVersion("2.0")]
    public async Task<ActionResult<UserDetailDto>> GetUserV2(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        
        if (user == null)
        {
            return NotFound(new ErrorResponse("User not found"));
        }
        
        var userDetail = new UserDetailDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = $"{user.FirstName} {user.LastName}",
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
        
        return Ok(userDetail);
    }
}

public class UserDetailDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

## 📊 API Documentation

### Swagger/OpenAPI Configuration

```csharp
public class SwaggerConfiguration
{
    public static void ConfigureSwagger(IServiceCollection services, TSKConfig config)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = config.Get<string>("api.title", "My API"),
                Version = "v1",
                Description = config.Get<string>("api.description", "API Documentation"),
                Contact = new OpenApiContact
                {
                    Name = config.Get<string>("api.contact.name", "API Support"),
                    Email = config.Get<string>("api.contact.email", "support@example.com")
                }
            });
            
            c.SwaggerDoc("v2", new OpenApiInfo
            {
                Title = config.Get<string>("api.title", "My API"),
                Version = "v2",
                Description = config.Get<string>("api.description", "API Documentation v2"),
                Contact = new OpenApiContact
                {
                    Name = config.Get<string>("api.contact.name", "API Support"),
                    Email = config.Get<string>("api.contact.email", "support@example.com")
                }
            });
            
            // Add JWT authentication
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });
            
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
            
            // Include XML comments
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }
        });
    }
}
```

## 🚀 Performance Optimization

### Caching Strategies

```csharp
public class CachedUserService : IUserService
{
    private readonly IUserService _userService;
    private readonly IDistributedCache _cache;
    private readonly ILogger<CachedUserService> _logger;
    private readonly TSKConfig _config;
    
    public CachedUserService(IUserService userService, IDistributedCache cache, 
        ILogger<CachedUserService> logger, TSKConfig config)
    {
        _userService = userService;
        _cache = cache;
        _logger = logger;
        _config = config;
    }
    
    public async Task<PaginatedResult<UserDto>> GetUsersAsync(int page, int pageSize, string? search)
    {
        var cacheKey = $"users:page:{page}:size:{pageSize}:search:{search ?? "null"}";
        var cacheExpiration = _config.Get<int>("cache.user_list_expiration_minutes", 5);
        
        var cachedResult = await _cache.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(cachedResult))
        {
            return JsonSerializer.Deserialize<PaginatedResult<UserDto>>(cachedResult)!;
        }
        
        var result = await _userService.GetUsersAsync(page, pageSize, search);
        
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheExpiration)
        };
        
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result), cacheOptions);
        
        return result;
    }
    
    public async Task<UserDto?> GetUserByIdAsync(int id)
    {
        var cacheKey = $"user:{id}";
        var cacheExpiration = _config.Get<int>("cache.user_expiration_minutes", 10);
        
        var cachedUser = await _cache.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(cachedUser))
        {
            return JsonSerializer.Deserialize<UserDto>(cachedUser);
        }
        
        var user = await _userService.GetUserByIdAsync(id);
        
        if (user != null)
        {
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheExpiration)
            };
            
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(user), cacheOptions);
        }
        
        return user;
    }
    
    public async Task<UserDto> CreateUserAsync(CreateUserRequest request)
    {
        var user = await _userService.CreateUserAsync(request);
        
        // Invalidate user list cache
        await InvalidateUserListCacheAsync();
        
        return user;
    }
    
    public async Task<UserDto?> UpdateUserAsync(int id, UpdateUserRequest request)
    {
        var user = await _userService.UpdateUserAsync(id, request);
        
        if (user != null)
        {
            // Invalidate specific user cache
            await _cache.RemoveAsync($"user:{id}");
            
            // Invalidate user list cache
            await InvalidateUserListCacheAsync();
        }
        
        return user;
    }
    
    public async Task<bool> DeleteUserAsync(int id)
    {
        var deleted = await _userService.DeleteUserAsync(id);
        
        if (deleted)
        {
            // Invalidate specific user cache
            await _cache.RemoveAsync($"user:{id}");
            
            // Invalidate user list cache
            await InvalidateUserListCacheAsync();
        }
        
        return deleted;
    }
    
    private async Task InvalidateUserListCacheAsync()
    {
        // This is a simplified implementation
        // In a real application, you might use cache tags or patterns
        var keys = new[] { "users:page:1:size:20:search:null", "users:page:1:size:50:search:null" };
        
        foreach (var key in keys)
        {
            await _cache.RemoveAsync(key);
        }
    }
}
```

## 📝 Summary

This guide covered comprehensive API design patterns for C# TuskLang applications:

- **RESTful API Design**: Controller structure, response models, and service layer implementation
- **Authentication & Authorization**: JWT authentication and role-based authorization
- **API Versioning**: Multiple versioning strategies with backward compatibility
- **API Documentation**: Swagger/OpenAPI configuration with comprehensive documentation
- **Performance Optimization**: Caching strategies for improved API performance

These patterns ensure your C# TuskLang APIs are scalable, secure, well-documented, and performant. 