# API Design in C# with TuskLang

## Overview

This guide covers comprehensive API design patterns for C# applications using TuskLang, including RESTful APIs, GraphQL, gRPC, and advanced API patterns.

## Table of Contents

1. [RESTful API Design](#restful-api-design)
2. [GraphQL Integration](#graphql-integration)
3. [gRPC Services](#grpc-services)
4. [API Versioning](#api-versioning)
5. [API Documentation](#api-documentation)
6. [Rate Limiting](#rate-limiting)
7. [Caching Strategies](#caching-strategies)
8. [TuskLang Integration](#tusklang-integration)

## RESTful API Design

### Basic REST Controller

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;
    private readonly TuskLang _config;

    public UsersController(IUserService userService, ILogger<UsersController> logger, TuskLang config)
    {
        _userService = userService;
        _logger = logger;
        _config = config;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers([FromQuery] UserQueryParams query)
    {
        var maxResults = _config.GetValue<int>("api.maxResults", 100);
        var users = await _userService.GetUsersAsync(query, maxResults);
        
        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        
        if (user == null)
            return NotFound();
            
        return Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserRequest request)
    {
        var user = await _userService.CreateUserAsync(request);
        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
    {
        var success = await _userService.UpdateUserAsync(id, request);
        
        if (!success)
            return NotFound();
            
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var success = await _userService.DeleteUserAsync(id);
        
        if (!success)
            return NotFound();
            
        return NoContent();
    }
}
```

### API Response Patterns

```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public T Data { get; set; }
    public List<string> Errors { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class PaginatedResponse<T>
{
    public IEnumerable<T> Data { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasNext { get; set; }
    public bool HasPrevious { get; set; }
}

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<ProductDto>>> GetProducts(
        [FromQuery] ProductQueryParams query)
    {
        var result = await _productService.GetProductsAsync(query);
        
        return Ok(new PaginatedResponse<ProductDto>
        {
            Data = result.Items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = result.TotalCount,
            TotalPages = (int)Math.Ceiling((double)result.TotalCount / query.PageSize),
            HasNext = query.Page < (int)Math.Ceiling((double)result.TotalCount / query.PageSize),
            HasPrevious = query.Page > 1
        });
    }
}
```

## GraphQL Integration

### GraphQL Schema

```csharp
public class Query
{
    private readonly IUserService _userService;
    private readonly TuskLang _config;

    public Query(IUserService userService, TuskLang config)
    {
        _userService = userService;
        _config = config;
    }

    public async Task<IEnumerable<User>> GetUsers(int? limit = null)
    {
        var maxLimit = _config.GetValue<int>("graphql.maxResults", 100);
        var actualLimit = limit ?? maxLimit;
        
        return await _userService.GetUsersAsync(actualLimit);
    }

    public async Task<User> GetUser(int id)
    {
        return await _userService.GetUserByIdAsync(id);
    }
}

public class Mutation
{
    private readonly IUserService _userService;

    public Mutation(IUserService userService)
    {
        _userService = userService;
    }

    public async Task<User> CreateUser(CreateUserInput input)
    {
        return await _userService.CreateUserAsync(input);
    }

    public async Task<User> UpdateUser(int id, UpdateUserInput input)
    {
        return await _userService.UpdateUserAsync(id, input);
    }

    public async Task<bool> DeleteUser(int id)
    {
        return await _userService.DeleteUserAsync(id);
    }
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public async Task<IEnumerable<Post>> GetPosts([Service] IPostService postService)
    {
        return await postService.GetPostsByUserIdAsync(Id);
    }
}
```

### GraphQL Configuration

```csharp
public static class GraphQLConfiguration
{
    public static IServiceCollection AddGraphQLServices(this IServiceCollection services, TuskLang config)
    {
        services.AddGraphQLServer()
            .AddQueryType<Query>()
            .AddMutationType<Mutation>()
            .AddType<User>()
            .AddType<Post>()
            .AddFiltering()
            .AddSorting()
            .AddProjections()
            .AddInMemorySubscriptions()
            .AddDataLoader<UserDataLoader>()
            .ConfigureSchema(schema =>
            {
                schema.SetDescription("TuskLang API GraphQL Schema");
            });

        // Configure GraphQL settings from TuskLang
        var maxComplexity = config.GetValue<int>("graphql.maxComplexity", 100);
        var maxDepth = config.GetValue<int>("graphql.maxDepth", 10);
        var enableIntrospection = config.GetValue<bool>("graphql.enableIntrospection", true);

        services.Configure<GraphQLServerOptions>(options =>
        {
            options.MaximumComplexity = maxComplexity;
            options.MaximumDepth = maxDepth;
            options.EnableIntrospection = enableIntrospection;
        });

        return services;
    }
}
```

## gRPC Services

### gRPC Service Definition

```protobuf
syntax = "proto3";

package tusklang.api;

service UserService {
  rpc GetUser (GetUserRequest) returns (UserResponse);
  rpc GetUsers (GetUsersRequest) returns (GetUsersResponse);
  rpc CreateUser (CreateUserRequest) returns (UserResponse);
  rpc UpdateUser (UpdateUserRequest) returns (UserResponse);
  rpc DeleteUser (DeleteUserRequest) returns (DeleteUserResponse);
}

message GetUserRequest {
  int32 id = 1;
}

message GetUsersRequest {
  int32 page = 1;
  int32 page_size = 2;
  string search = 3;
}

message UserResponse {
  int32 id = 1;
  string name = 2;
  string email = 3;
  string created_at = 4;
}

message GetUsersResponse {
  repeated UserResponse users = 1;
  int32 total_count = 2;
  int32 page = 3;
  int32 page_size = 4;
}

message CreateUserRequest {
  string name = 1;
  string email = 2;
}

message UpdateUserRequest {
  int32 id = 1;
  string name = 2;
  string email = 3;
}

message DeleteUserRequest {
  int32 id = 1;
}

message DeleteUserResponse {
  bool success = 1;
}
```

### gRPC Service Implementation

```csharp
public class UserGrpcService : UserService.UserServiceBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UserGrpcService> _logger;
    private readonly TuskLang _config;

    public UserGrpcService(IUserService userService, ILogger<UserGrpcService> logger, TuskLang config)
    {
        _userService = userService;
        _logger = logger;
        _config = config;
    }

    public override async Task<UserResponse> GetUser(GetUserRequest request, ServerCallContext context)
    {
        try
        {
            var user = await _userService.GetUserByIdAsync(request.Id);
            
            if (user == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "User not found"));
            }

            return new UserResponse
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                CreatedAt = user.CreatedAt.ToString("O")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId}", request.Id);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<GetUsersResponse> GetUsers(GetUsersRequest request, ServerCallContext context)
    {
        try
        {
            var maxPageSize = _config.GetValue<int>("grpc.maxPageSize", 100);
            var pageSize = Math.Min(request.PageSize, maxPageSize);

            var query = new UserQueryParams
            {
                Page = request.Page,
                PageSize = pageSize,
                Search = request.Search
            };

            var result = await _userService.GetUsersAsync(query);

            var response = new GetUsersResponse
            {
                TotalCount = result.TotalCount,
                Page = request.Page,
                PageSize = pageSize
            };

            response.Users.AddRange(result.Items.Select(u => new UserResponse
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                CreatedAt = u.CreatedAt.ToString("O")
            }));

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<UserResponse> CreateUser(CreateUserRequest request, ServerCallContext context)
    {
        try
        {
            var createRequest = new CreateUserRequest
            {
                Name = request.Name,
                Email = request.Email
            };

            var user = await _userService.CreateUserAsync(createRequest);

            return new UserResponse
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                CreatedAt = user.CreatedAt.ToString("O")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }
}
```

## API Versioning

### Versioned API Controllers

```csharp
[ApiController]
[ApiVersion("1.0")]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly TuskLang _config;

    public UsersController(IUserService userService, TuskLang config)
    {
        _userService = userService;
        _config = config;
    }

    [HttpGet]
    [MapToApiVersion("1.0")]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsersV1([FromQuery] UserQueryParams query)
    {
        var users = await _userService.GetUsersAsync(query);
        return Ok(users);
    }

    [HttpGet]
    [MapToApiVersion("2.0")]
    public async Task<ActionResult<PaginatedResponse<UserDto>>> GetUsersV2([FromQuery] UserQueryParams query)
    {
        var result = await _userService.GetUsersAsync(query);
        
        return Ok(new PaginatedResponse<UserDto>
        {
            Data = result.Items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = result.TotalCount,
            TotalPages = (int)Math.Ceiling((double)result.TotalCount / query.PageSize)
        });
    }

    [HttpGet("{id}")]
    [MapToApiVersion("1.0")]
    public async Task<ActionResult<UserDto>> GetUserV1(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        
        if (user == null)
            return NotFound();
            
        return Ok(user);
    }

    [HttpGet("{id}")]
    [MapToApiVersion("2.0")]
    public async Task<ActionResult<UserDetailDto>> GetUserV2(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        
        if (user == null)
            return NotFound();
            
        var userDetail = new UserDetailDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            IsActive = user.IsActive,
            Profile = user.Profile
        };
            
        return Ok(userDetail);
    }
}
```

### API Version Configuration

```csharp
public static class ApiVersioningConfiguration
{
    public static IServiceCollection AddApiVersioningServices(this IServiceCollection services, TuskLang config)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new HeaderApiVersionReader("X-API-Version"),
                new MediaTypeApiVersionReader("version")
            );
        });

        services.AddVersionedApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        // Configure versioning settings from TuskLang
        var defaultVersion = config.GetValue<string>("api.defaultVersion", "1.0");
        var supportedVersions = config.GetValue<string>("api.supportedVersions", "1.0,2.0")
            .Split(',', StringSplitOptions.RemoveEmptyEntries);

        return services;
    }
}
```

## API Documentation

### Swagger Configuration

```csharp
public static class SwaggerConfiguration
{
    public static IServiceCollection AddSwaggerServices(this IServiceCollection services, TuskLang config)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = config.GetValue<string>("swagger.title", "TuskLang API"),
                Version = "v1",
                Description = config.GetValue<string>("swagger.description", "TuskLang API Documentation"),
                Contact = new OpenApiContact
                {
                    Name = config.GetValue<string>("swagger.contact.name", "API Support"),
                    Email = config.GetValue<string>("swagger.contact.email", "support@example.com")
                }
            });

            options.SwaggerDoc("v2", new OpenApiInfo
            {
                Title = config.GetValue<string>("swagger.title", "TuskLang API"),
                Version = "v2",
                Description = config.GetValue<string>("swagger.description", "TuskLang API Documentation v2")
            });

            // Add API key authentication
            options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Name = "X-API-Key",
                Description = "API Key for authentication"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "ApiKey"
                        }
                    },
                    new string[] {}
                }
            });

            // Include XML comments
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });

        return services;
    }
}
```

## Rate Limiting

### Rate Limiting Implementation

```csharp
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly TuskLang _config;
    private readonly ILogger<RateLimitingMiddleware> _logger;

    public RateLimitingMiddleware(RequestDelegate next, IMemoryCache cache, 
        TuskLang config, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _cache = cache;
        _config = config;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientId = GetClientId(context);
        var endpoint = context.Request.Path;
        
        var maxRequests = _config.GetValue<int>("rateLimit.maxRequests", 100);
        var windowMinutes = _config.GetValue<int>("rateLimit.windowMinutes", 1);
        
        var key = $"rate_limit:{clientId}:{endpoint}";
        var windowStart = DateTime.UtcNow.AddMinutes(-windowMinutes);

        var requests = await _cache.GetOrCreateAsync(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(windowMinutes);
            return Task.FromResult(new List<DateTime>());
        });

        // Remove old requests outside the window
        requests.RemoveAll(r => r < windowStart);

        if (requests.Count >= maxRequests)
        {
            _logger.LogWarning("Rate limit exceeded for client {ClientId} on endpoint {Endpoint}", 
                clientId, endpoint);
            
            context.Response.StatusCode = 429; // Too Many Requests
            context.Response.Headers.Add("Retry-After", windowMinutes.ToString());
            
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Rate limit exceeded",
                retryAfter = windowMinutes
            });
            
            return;
        }

        requests.Add(DateTime.UtcNow);
        await _cache.SetAsync(key, requests, TimeSpan.FromMinutes(windowMinutes));

        await _next(context);
    }

    private string GetClientId(HttpContext context)
    {
        // Try to get client ID from various sources
        var clientId = context.Request.Headers["X-Client-ID"].FirstOrDefault()
            ?? context.Request.Headers["X-API-Key"].FirstOrDefault()
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";

        return clientId;
    }
}
```

## Caching Strategies

### API Caching

```csharp
public class ApiCachingService
{
    private readonly IMemoryCache _cache;
    private readonly TuskLang _config;
    private readonly ILogger<ApiCachingService> _logger;

    public ApiCachingService(IMemoryCache cache, TuskLang config, ILogger<ApiCachingService> logger)
    {
        _cache = cache;
        _config = config;
        _logger = logger;
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
    {
        var defaultExpiration = TimeSpan.FromMinutes(_config.GetValue<int>("cache.defaultExpirationMinutes", 5));
        var actualExpiration = expiration ?? defaultExpiration;

        if (_cache.TryGetValue(key, out T cachedValue))
        {
            _logger.LogDebug("Cache hit for key {Key}", key);
            return cachedValue;
        }

        _logger.LogDebug("Cache miss for key {Key}, fetching data", key);
        
        var value = await factory();
        
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = actualExpiration,
            SlidingExpiration = TimeSpan.FromMinutes(_config.GetValue<int>("cache.slidingExpirationMinutes", 2))
        };

        _cache.Set(key, value, cacheOptions);
        
        return value;
    }

    public void Invalidate(string key)
    {
        _cache.Remove(key);
        _logger.LogDebug("Cache invalidated for key {Key}", key);
    }

    public void InvalidatePattern(string pattern)
    {
        // Note: This is a simplified implementation
        // In production, consider using Redis or a more sophisticated cache
        _logger.LogDebug("Cache pattern invalidation requested for {Pattern}", pattern);
    }
}

[ApiController]
[Route("api/[controller]")]
public class CachedUsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ApiCachingService _cachingService;

    public CachedUsersController(IUserService userService, ApiCachingService cachingService)
    {
        _userService = userService;
        _cachingService = cachingService;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        var cacheKey = $"user:{id}";
        
        var user = await _cachingService.GetOrSetAsync(cacheKey, 
            () => _userService.GetUserByIdAsync(id));

        if (user == null)
            return NotFound();
            
        return Ok(user);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers([FromQuery] UserQueryParams query)
    {
        var cacheKey = $"users:{query.Page}:{query.PageSize}:{query.Search}";
        
        var users = await _cachingService.GetOrSetAsync(cacheKey, 
            () => _userService.GetUsersAsync(query));

        return Ok(users);
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserRequest request)
    {
        var user = await _userService.CreateUserAsync(request);
        
        // Invalidate related caches
        _cachingService.InvalidatePattern("users:*");
        
        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
    }
}
```

## TuskLang Integration

### TuskLang API Configuration

```csharp
public class TuskLangApiConfig
{
    public string BaseUrl { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public int TimeoutSeconds { get; set; } = 30;
    public int RetryCount { get; set; } = 3;
    public bool EnableCaching { get; set; } = true;
    public int CacheExpirationMinutes { get; set; } = 5;
    public bool EnableRateLimiting { get; set; } = true;
    public int MaxRequestsPerMinute { get; set; } = 100;
}

public class TuskLangApiService
{
    private readonly HttpClient _httpClient;
    private readonly TuskLang _config;
    private readonly ILogger<TuskLangApiService> _logger;

    public TuskLangApiService(HttpClient httpClient, TuskLang config, ILogger<TuskLangApiService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
        
        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        var baseUrl = _config.GetValue<string>("api.baseUrl", "");
        var timeout = TimeSpan.FromSeconds(_config.GetValue<int>("api.timeoutSeconds", 30));
        
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.Timeout = timeout;
        
        var apiKey = _config.GetValue<string>("api.apiKey", "");
        if (!string.IsNullOrEmpty(apiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);
        }
    }

    public async Task<T> GetAsync<T>(string endpoint)
    {
        try
        {
            var response = await _httpClient.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling API endpoint {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task<T> PostAsync<T>(string endpoint, object data)
    {
        try
        {
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(endpoint, content);
            response.EnsureSuccessStatusCode();
            
            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling API endpoint {Endpoint}", endpoint);
            throw;
        }
    }
}
```

## Summary

This comprehensive API design guide covers:

- **RESTful API Design**: Standard REST patterns with proper HTTP methods and status codes
- **GraphQL Integration**: Schema definition, resolvers, and configuration
- **gRPC Services**: Protocol buffer definitions and service implementations
- **API Versioning**: Multiple version support with proper routing
- **API Documentation**: Swagger/OpenAPI integration with TuskLang configuration
- **Rate Limiting**: Request throttling with configurable limits
- **Caching Strategies**: Intelligent caching with invalidation patterns
- **TuskLang Integration**: Configuration-driven API settings

The patterns ensure scalable, maintainable, and well-documented APIs that integrate seamlessly with TuskLang's configuration system. 