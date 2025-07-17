# API Design in C# with TuskLang

## Overview

This guide covers comprehensive API design patterns for C# applications using TuskLang, including RESTful APIs, GraphQL, and advanced API patterns.

## Table of Contents

1. [RESTful API Design](#restful-api-design)
2. [GraphQL Integration](#graphql-integration)
3. [API Versioning](#api-versioning)
4. [TuskLang Integration](#tusklang-integration)

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
- **API Versioning**: Multiple version support with proper routing
- **TuskLang Integration**: Configuration-driven API settings

The patterns ensure scalable, maintainable, and well-documented APIs that integrate seamlessly with TuskLang's configuration system. 