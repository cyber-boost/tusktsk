# API Design in C# TuskLang

## Overview

Well-designed APIs are crucial for building scalable and maintainable applications. This guide covers RESTful API design, versioning, documentation, and API design best practices for C# TuskLang applications.

## 🌐 RESTful API Design

### API Controller Base

```csharp
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[ApiVersion("2.0")]
public abstract class ApiControllerBase : ControllerBase
{
    protected readonly ILogger _logger;
    protected readonly TSKConfig _config;
    protected readonly ICommandQueryDispatcher _dispatcher;
    protected readonly IValidator _validator;
    
    protected ApiControllerBase(
        ILogger logger,
        TSKConfig config,
        ICommandQueryDispatcher dispatcher,
        IValidator validator)
    {
        _logger = logger;
        _config = config;
        _dispatcher = dispatcher;
        _validator = validator;
    }
    
    protected async Task<ActionResult<T>> ExecuteCommandAsync<T>(ICommand command)
    {
        try
        {
            var correlationId = GetCorrelationId();
            
            _logger.LogInformation("Executing command {CommandType} with correlation {CorrelationId}", 
                command.GetType().Name, correlationId);
            
            var result = await _dispatcher.SendAsync<T>(command);
            
            _logger.LogInformation("Command {CommandType} executed successfully", command.GetType().Name);
            
            return Ok(new ApiResponse<T>
            {
                Success = true,
                Data = result,
                CorrelationId = correlationId,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for command {CommandType}", command.GetType().Name);
            
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                ErrorCode = "VALIDATION_ERROR",
                Message = "Validation failed",
                Errors = ex.Errors,
                CorrelationId = GetCorrelationId(),
                Timestamp = DateTime.UtcNow
            });
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found for command {CommandType}", command.GetType().Name);
            
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                ErrorCode = "NOT_FOUND",
                Message = ex.Message,
                CorrelationId = GetCorrelationId(),
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Command {CommandType} failed", command.GetType().Name);
            
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                ErrorCode = "INTERNAL_ERROR",
                Message = "An internal error occurred",
                CorrelationId = GetCorrelationId(),
                Timestamp = DateTime.UtcNow
            });
        }
    }
    
    protected async Task<ActionResult<T>> ExecuteQueryAsync<T>(IQuery<T> query)
    {
        try
        {
            var correlationId = GetCorrelationId();
            
            _logger.LogInformation("Executing query {QueryType} with correlation {CorrelationId}", 
                query.GetType().Name, correlationId);
            
            var result = await _dispatcher.SendAsync<T>(query);
            
            _logger.LogInformation("Query {QueryType} executed successfully", query.GetType().Name);
            
            return Ok(new ApiResponse<T>
            {
                Success = true,
                Data = result,
                CorrelationId = correlationId,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found for query {QueryType}", query.GetType().Name);
            
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                ErrorCode = "NOT_FOUND",
                Message = ex.Message,
                CorrelationId = GetCorrelationId(),
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Query {QueryType} failed", query.GetType().Name);
            
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                ErrorCode = "INTERNAL_ERROR",
                Message = "An internal error occurred",
                CorrelationId = GetCorrelationId(),
                Timestamp = DateTime.UtcNow
            });
        }
    }
    
    protected async Task<ActionResult<T>> ExecuteQueryAsync<T>(IQuery<T> query, bool allowNull)
    {
        var result = await ExecuteQueryAsync<T>(query);
        
        if (result.Result is OkObjectResult okResult && okResult.Value is ApiResponse<T> response)
        {
            if (response.Data == null && !allowNull)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    ErrorCode = "NOT_FOUND",
                    Message = "Resource not found",
                    CorrelationId = GetCorrelationId(),
                    Timestamp = DateTime.UtcNow
                });
            }
        }
        
        return result;
    }
    
    protected string GetCorrelationId()
    {
        return HttpContext.TraceIdentifier;
    }
    
    protected async Task<ValidationResult> ValidateAsync<T>(T model)
    {
        return await _validator.ValidateAsync(model);
    }
    
    protected ActionResult<ApiResponse<object>> ValidationError(ValidationResult validationResult)
    {
        return BadRequest(new ApiResponse<object>
        {
            Success = false,
            ErrorCode = "VALIDATION_ERROR",
            Message = "Validation failed",
            Errors = validationResult.Errors.Select(e => new ValidationError
            {
                Field = e.PropertyName,
                Message = e.ErrorMessage,
                Code = e.ErrorCode
            }).ToList(),
            CorrelationId = GetCorrelationId(),
            Timestamp = DateTime.UtcNow
        });
    }
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? ErrorCode { get; set; }
    public string? Message { get; set; }
    public List<ValidationError>? Errors { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime Timestamp { get; set; }
}

public class ValidationError
{
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}
```

### User API Controller

```csharp
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[ApiVersion("2.0")]
public class UsersController : ApiControllerBase
{
    public UsersController(
        ILogger<UsersController> logger,
        TSKConfig config,
        ICommandQueryDispatcher dispatcher,
        IValidator validator)
        : base(logger, config, dispatcher, validator)
    {
    }
    
    /// <summary>
    /// Creates a new user
    /// </summary>
    /// <param name="request">User creation request</param>
    /// <returns>Created user information</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser([FromBody] CreateUserRequest request)
    {
        // Validate request
        var validationResult = await ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return ValidationError(validationResult);
        }
        
        // Execute command
        var command = new CreateUserCommand
        {
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName
        };
        
        var result = await ExecuteCommandAsync<UserDto>(command);
        
        if (result.Result is OkObjectResult okResult && okResult.Value is ApiResponse<UserDto> response)
        {
            return CreatedAtAction(nameof(GetUser), new { id = response.Data!.Id }, response);
        }
        
        return result;
    }
    
    /// <summary>
    /// Gets a user by ID
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>User information</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(Guid id)
    {
        var query = new GetUserQuery { UserId = id };
        return await ExecuteQueryAsync<UserDto>(query, allowNull: false);
    }
    
    /// <summary>
    /// Gets a list of users with pagination and filtering
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10)</param>
    /// <param name="searchTerm">Search term for email or name</param>
    /// <param name="status">User status filter</param>
    /// <returns>Paginated list of users</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<UserDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<PaginatedResult<UserDto>>>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] UserStatus? status = null)
    {
        var query = new GetUsersQuery
        {
            Page = page,
            PageSize = pageSize,
            SearchTerm = searchTerm,
            Status = status
        };
        
        return await ExecuteQueryAsync<PaginatedResult<UserDto>>(query);
    }
    
    /// <summary>
    /// Updates a user
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="request">User update request</param>
    /// <returns>Updated user information</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(
        Guid id,
        [FromBody] UpdateUserRequest request)
    {
        // Validate request
        var validationResult = await ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return ValidationError(validationResult);
        }
        
        // Execute command
        var command = new UpdateUserCommand
        {
            UserId = id,
            FirstName = request.FirstName,
            LastName = request.LastName
        };
        
        return await ExecuteCommandAsync<UserDto>(command);
    }
    
    /// <summary>
    /// Deletes a user
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>No content</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeleteUser(Guid id)
    {
        var command = new DeleteUserCommand { UserId = id };
        var result = await ExecuteCommandAsync<object>(command);
        
        if (result.Result is OkObjectResult)
        {
            return NoContent();
        }
        
        return result;
    }
    
    /// <summary>
    /// Gets user statistics
    /// </summary>
    /// <returns>User statistics</returns>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(ApiResponse<UserStatistics>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<UserStatistics>>> GetUserStatistics()
    {
        var query = new GetUserStatisticsQuery();
        return await ExecuteQueryAsync<UserStatistics>(query);
    }
}

public class PaginatedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

public class UserStatistics
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int InactiveUsers { get; set; }
    public int NewUsersThisMonth { get; set; }
    public Dictionary<string, int> UsersByStatus { get; set; } = new();
}
```

## 🔄 API Versioning

### API Version Manager

```csharp
public class ApiVersionManager
{
    private readonly ILogger<ApiVersionManager> _logger;
    private readonly TSKConfig _config;
    private readonly Dictionary<string, ApiVersionInfo> _versions;
    
    public ApiVersionManager(ILogger<ApiVersionManager> logger, TSKConfig config)
    {
        _logger = logger;
        _config = config;
        _versions = new Dictionary<string, ApiVersionInfo>();
        
        LoadApiVersions();
    }
    
    public ApiVersionInfo? GetVersionInfo(string version)
    {
        return _versions.GetValueOrDefault(version);
    }
    
    public List<ApiVersionInfo> GetAllVersions()
    {
        return _versions.Values.OrderBy(v => v.Version).ToList();
    }
    
    public bool IsVersionSupported(string version)
    {
        return _versions.ContainsKey(version);
    }
    
    public bool IsVersionDeprecated(string version)
    {
        return _versions.TryGetValue(version, out var versionInfo) && versionInfo.IsDeprecated;
    }
    
    public DateTime? GetDeprecationDate(string version)
    {
        return _versions.TryGetValue(version, out var versionInfo) ? versionInfo.DeprecationDate : null;
    }
    
    public string? GetMigrationGuide(string fromVersion, string toVersion)
    {
        if (_versions.TryGetValue(fromVersion, out var fromInfo) && 
            _versions.TryGetValue(toVersion, out var toInfo))
        {
            return GenerateMigrationGuide(fromInfo, toInfo);
        }
        
        return null;
    }
    
    private void LoadApiVersions()
    {
        var versionsConfig = _config.GetSection("api.versions");
        if (versionsConfig != null)
        {
            foreach (var key in versionsConfig.GetKeys())
            {
                var versionConfig = versionsConfig.GetSection(key);
                var versionInfo = new ApiVersionInfo
                {
                    Version = key,
                    ReleaseDate = DateTime.Parse(versionConfig.Get<string>("release_date", DateTime.UtcNow.ToString())),
                    IsDeprecated = versionConfig.Get<bool>("is_deprecated", false),
                    DeprecationDate = versionConfig.Get<string>("deprecation_date") != null 
                        ? DateTime.Parse(versionConfig.Get<string>("deprecation_date")!) 
                        : null,
                    BreakingChanges = versionConfig.GetSection("breaking_changes").ToDictionary(),
                    NewFeatures = versionConfig.GetSection("new_features").ToDictionary(),
                    DocumentationUrl = versionConfig.Get<string>("documentation_url")
                };
                
                _versions[key] = versionInfo;
            }
        }
        
        // Set default versions if not configured
        if (!_versions.ContainsKey("1.0"))
        {
            _versions["1.0"] = new ApiVersionInfo
            {
                Version = "1.0",
                ReleaseDate = DateTime.UtcNow.AddDays(-30),
                IsDeprecated = false
            };
        }
        
        if (!_versions.ContainsKey("2.0"))
        {
            _versions["2.0"] = new ApiVersionInfo
            {
                Version = "2.0",
                ReleaseDate = DateTime.UtcNow,
                IsDeprecated = false
            };
        }
        
        _logger.LogInformation("Loaded {Count} API versions", _versions.Count);
    }
    
    private string? GenerateMigrationGuide(ApiVersionInfo fromVersion, ApiVersionInfo toVersion)
    {
        var guide = new StringBuilder();
        guide.AppendLine($"# Migration Guide: {fromVersion.Version} to {toVersion.Version}");
        guide.AppendLine();
        
        if (toVersion.BreakingChanges.Any())
        {
            guide.AppendLine("## Breaking Changes");
            guide.AppendLine();
            
            foreach (var change in toVersion.BreakingChanges)
            {
                guide.AppendLine($"### {change.Key}");
                guide.AppendLine(change.Value.ToString());
                guide.AppendLine();
            }
        }
        
        if (toVersion.NewFeatures.Any())
        {
            guide.AppendLine("## New Features");
            guide.AppendLine();
            
            foreach (var feature in toVersion.NewFeatures)
            {
                guide.AppendLine($"### {feature.Key}");
                guide.AppendLine(feature.Value.ToString());
                guide.AppendLine();
            }
        }
        
        return guide.ToString();
    }
}

public class ApiVersionInfo
{
    public string Version { get; set; } = string.Empty;
    public DateTime ReleaseDate { get; set; }
    public bool IsDeprecated { get; set; }
    public DateTime? DeprecationDate { get; set; }
    public Dictionary<string, object> BreakingChanges { get; set; } = new();
    public Dictionary<string, object> NewFeatures { get; set; } = new();
    public string? DocumentationUrl { get; set; }
}

[ApiController]
[Route("api/versions")]
public class ApiVersionsController : ControllerBase
{
    private readonly ApiVersionManager _versionManager;
    private readonly ILogger<ApiVersionsController> _logger;
    
    public ApiVersionsController(ApiVersionManager versionManager, ILogger<ApiVersionsController> logger)
    {
        _versionManager = versionManager;
        _logger = logger;
    }
    
    /// <summary>
    /// Gets all available API versions
    /// </summary>
    /// <returns>List of API versions</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<ApiVersionInfo>), StatusCodes.Status200OK)]
    public ActionResult<List<ApiVersionInfo>> GetVersions()
    {
        return Ok(_versionManager.GetAllVersions());
    }
    
    /// <summary>
    /// Gets information about a specific API version
    /// </summary>
    /// <param name="version">API version</param>
    /// <returns>API version information</returns>
    [HttpGet("{version}")]
    [ProducesResponseType(typeof(ApiVersionInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<ApiVersionInfo> GetVersion(string version)
    {
        var versionInfo = _versionManager.GetVersionInfo(version);
        
        if (versionInfo == null)
        {
            return NotFound();
        }
        
        return Ok(versionInfo);
    }
    
    /// <summary>
    /// Gets migration guide between two API versions
    /// </summary>
    /// <param name="fromVersion">Source version</param>
    /// <param name="toVersion">Target version</param>
    /// <returns>Migration guide</returns>
    [HttpGet("migration/{fromVersion}/{toVersion}")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<string> GetMigrationGuide(string fromVersion, string toVersion)
    {
        var guide = _versionManager.GetMigrationGuide(fromVersion, toVersion);
        
        if (guide == null)
        {
            return NotFound();
        }
        
        return Ok(guide);
    }
}
```

## 📚 API Documentation

### API Documentation Generator

```csharp
public class ApiDocumentationGenerator
{
    private readonly ILogger<ApiDocumentationGenerator> _logger;
    private readonly TSKConfig _config;
    private readonly ApiVersionManager _versionManager;
    
    public ApiDocumentationGenerator(
        ILogger<ApiDocumentationGenerator> logger,
        TSKConfig config,
        ApiVersionManager versionManager)
    {
        _logger = logger;
        _config = config;
        _versionManager = versionManager;
    }
    
    public async Task<string> GenerateOpenApiSpecAsync(string version)
    {
        try
        {
            var versionInfo = _versionManager.GetVersionInfo(version);
            if (versionInfo == null)
            {
                throw new ArgumentException($"Unknown API version: {version}");
            }
            
            var openApiDocument = new OpenApiDocument
            {
                Info = new OpenApiInfo
                {
                    Title = _config.Get<string>("api.title", "TuskLang API"),
                    Version = version,
                    Description = _config.Get<string>("api.description", "TuskLang API Documentation"),
                    Contact = new OpenApiContact
                    {
                        Name = _config.Get<string>("api.contact.name", "TuskLang Team"),
                        Email = _config.Get<string>("api.contact.email", "api@tusklang.org"),
                        Url = new Uri(_config.Get<string>("api.contact.url", "https://tusklang.org"))
                    },
                    License = new OpenApiLicense
                    {
                        Name = _config.Get<string>("api.license.name", "MIT"),
                        Url = new Uri(_config.Get<string>("api.license.url", "https://opensource.org/licenses/MIT"))
                    }
                },
                Servers = new List<OpenApiServer>
                {
                    new OpenApiServer
                    {
                        Url = _config.Get<string>("api.base_url", "https://api.tusklang.org"),
                        Description = "Production server"
                    },
                    new OpenApiServer
                    {
                        Url = _config.Get<string>("api.dev_url", "https://dev-api.tusklang.org"),
                        Description = "Development server"
                    }
                },
                Paths = await GeneratePathsAsync(version),
                Components = await GenerateComponentsAsync(version)
            };
            
            // Add version-specific information
            if (versionInfo.IsDeprecated)
            {
                openApiDocument.Info.Description += $"\n\n**⚠️ This version is deprecated.**";
                if (versionInfo.DeprecationDate.HasValue)
                {
                    openApiDocument.Info.Description += $" Deprecation date: {versionInfo.DeprecationDate.Value:yyyy-MM-dd}";
                }
            }
            
            var json = JsonSerializer.Serialize(openApiDocument, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            _logger.LogInformation("Generated OpenAPI specification for version {Version}", version);
            
            return json;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate OpenAPI specification for version {Version}", version);
            throw;
        }
    }
    
    private async Task<OpenApiPaths> GeneratePathsAsync(string version)
    {
        var paths = new OpenApiPaths();
        
        // Add user endpoints
        paths.Add("/api/v{version}/users", new OpenApiPathItem
        {
            Get = new OpenApiOperation
            {
                Summary = "Get users",
                Description = "Retrieves a paginated list of users with optional filtering",
                OperationId = "getUsers",
                Tags = new List<OpenApiTag> { new OpenApiTag { Name = "Users" } },
                Parameters = new List<OpenApiParameter>
                {
                    new OpenApiParameter
                    {
                        Name = "page",
                        In = ParameterLocation.Query,
                        Description = "Page number",
                        Required = false,
                        Schema = new OpenApiSchema { Type = "integer", Default = new OpenApiInteger(1) }
                    },
                    new OpenApiParameter
                    {
                        Name = "pageSize",
                        In = ParameterLocation.Query,
                        Description = "Page size",
                        Required = false,
                        Schema = new OpenApiSchema { Type = "integer", Default = new OpenApiInteger(10) }
                    },
                    new OpenApiParameter
                    {
                        Name = "searchTerm",
                        In = ParameterLocation.Query,
                        Description = "Search term for email or name",
                        Required = false,
                        Schema = new OpenApiSchema { Type = "string" }
                    },
                    new OpenApiParameter
                    {
                        Name = "status",
                        In = ParameterLocation.Query,
                        Description = "User status filter",
                        Required = false,
                        Schema = new OpenApiSchema { Type = "string", Enum = new List<IOpenApiAny>
                        {
                            new OpenApiString("Active"),
                            new OpenApiString("Inactive"),
                            new OpenApiString("Suspended")
                        }}
                    }
                },
                Responses = new OpenApiResponses
                {
                    ["200"] = new OpenApiResponse
                    {
                        Description = "Successful response",
                        Content = new Dictionary<string, OpenApiMediaType>
                        {
                            ["application/json"] = new OpenApiMediaType
                            {
                                Schema = new OpenApiSchema
                                {
                                    Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = "PaginatedResult_UserDto" }
                                }
                            }
                        }
                    }
                }
            },
            Post = new OpenApiOperation
            {
                Summary = "Create user",
                Description = "Creates a new user",
                OperationId = "createUser",
                Tags = new List<OpenApiTag> { new OpenApiTag { Name = "Users" } },
                RequestBody = new OpenApiRequestBody
                {
                    Required = true,
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new OpenApiMediaType
                        {
                            Schema = new OpenApiSchema
                            {
                                Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = "CreateUserRequest" }
                            }
                        }
                    }
                },
                Responses = new OpenApiResponses
                {
                    ["201"] = new OpenApiResponse
                    {
                        Description = "User created successfully",
                        Content = new Dictionary<string, OpenApiMediaType>
                        {
                            ["application/json"] = new OpenApiMediaType
                            {
                                Schema = new OpenApiSchema
                                {
                                    Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = "ApiResponse_UserDto" }
                                }
                            }
                        }
                    },
                    ["400"] = new OpenApiResponse
                    {
                        Description = "Bad request",
                        Content = new Dictionary<string, OpenApiMediaType>
                        {
                            ["application/json"] = new OpenApiMediaType
                            {
                                Schema = new OpenApiSchema
                                {
                                    Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = "ApiResponse_ValidationError" }
                                }
                            }
                        }
                    }
                }
            }
        });
        
        // Add individual user endpoints
        paths.Add("/api/v{version}/users/{id}", new OpenApiPathItem
        {
            Get = new OpenApiOperation
            {
                Summary = "Get user by ID",
                Description = "Retrieves a user by their ID",
                OperationId = "getUser",
                Tags = new List<OpenApiTag> { new OpenApiTag { Name = "Users" } },
                Parameters = new List<OpenApiParameter>
                {
                    new OpenApiParameter
                    {
                        Name = "id",
                        In = ParameterLocation.Path,
                        Required = true,
                        Schema = new OpenApiSchema { Type = "string", Format = "uuid" }
                    }
                },
                Responses = new OpenApiResponses
                {
                    ["200"] = new OpenApiResponse
                    {
                        Description = "Successful response",
                        Content = new Dictionary<string, OpenApiMediaType>
                        {
                            ["application/json"] = new OpenApiMediaType
                            {
                                Schema = new OpenApiSchema
                                {
                                    Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = "ApiResponse_UserDto" }
                                }
                            }
                        }
                    },
                    ["404"] = new OpenApiResponse
                    {
                        Description = "User not found",
                        Content = new Dictionary<string, OpenApiMediaType>
                        {
                            ["application/json"] = new OpenApiMediaType
                            {
                                Schema = new OpenApiSchema
                                {
                                    Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = "ApiResponse_Error" }
                                }
                            }
                        }
                    }
                }
            },
            Put = new OpenApiOperation
            {
                Summary = "Update user",
                Description = "Updates an existing user",
                OperationId = "updateUser",
                Tags = new List<OpenApiTag> { new OpenApiTag { Name = "Users" } },
                Parameters = new List<OpenApiParameter>
                {
                    new OpenApiParameter
                    {
                        Name = "id",
                        In = ParameterLocation.Path,
                        Required = true,
                        Schema = new OpenApiSchema { Type = "string", Format = "uuid" }
                    }
                },
                RequestBody = new OpenApiRequestBody
                {
                    Required = true,
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new OpenApiMediaType
                        {
                            Schema = new OpenApiSchema
                            {
                                Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = "UpdateUserRequest" }
                            }
                        }
                    }
                },
                Responses = new OpenApiResponses
                {
                    ["200"] = new OpenApiResponse
                    {
                        Description = "User updated successfully",
                        Content = new Dictionary<string, OpenApiMediaType>
                        {
                            ["application/json"] = new OpenApiMediaType
                            {
                                Schema = new OpenApiSchema
                                {
                                    Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = "ApiResponse_UserDto" }
                                }
                            }
                        }
                    }
                }
            },
            Delete = new OpenApiOperation
            {
                Summary = "Delete user",
                Description = "Deletes a user",
                OperationId = "deleteUser",
                Tags = new List<OpenApiTag> { new OpenApiTag { Name = "Users" } },
                Parameters = new List<OpenApiParameter>
                {
                    new OpenApiParameter
                    {
                        Name = "id",
                        In = ParameterLocation.Path,
                        Required = true,
                        Schema = new OpenApiSchema { Type = "string", Format = "uuid" }
                    }
                },
                Responses = new OpenApiResponses
                {
                    ["204"] = new OpenApiResponse
                    {
                        Description = "User deleted successfully"
                    },
                    ["404"] = new OpenApiResponse
                    {
                        Description = "User not found"
                    }
                }
            }
        });
        
        return paths;
    }
    
    private async Task<OpenApiComponents> GenerateComponentsAsync(string version)
    {
        var components = new OpenApiComponents
        {
            Schemas = new Dictionary<string, OpenApiSchema>
            {
                ["UserDto"] = new OpenApiSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, OpenApiSchema>
                    {
                        ["id"] = new OpenApiSchema { Type = "string", Format = "uuid" },
                        ["email"] = new OpenApiSchema { Type = "string", Format = "email" },
                        ["firstName"] = new OpenApiSchema { Type = "string" },
                        ["lastName"] = new OpenApiSchema { Type = "string" },
                        ["status"] = new OpenApiSchema { Type = "string", Enum = new List<IOpenApiAny>
                        {
                            new OpenApiString("Active"),
                            new OpenApiString("Inactive"),
                            new OpenApiString("Suspended")
                        }}
                    },
                    Required = new HashSet<string> { "id", "email", "firstName", "lastName", "status" }
                },
                ["CreateUserRequest"] = new OpenApiSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, OpenApiSchema>
                    {
                        ["email"] = new OpenApiSchema { Type = "string", Format = "email" },
                        ["firstName"] = new OpenApiSchema { Type = "string", MinLength = 1, MaxLength = 50 },
                        ["lastName"] = new OpenApiSchema { Type = "string", MinLength = 1, MaxLength = 50 }
                    },
                    Required = new HashSet<string> { "email", "firstName", "lastName" }
                },
                ["UpdateUserRequest"] = new OpenApiSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, OpenApiSchema>
                    {
                        ["firstName"] = new OpenApiSchema { Type = "string", MinLength = 1, MaxLength = 50 },
                        ["lastName"] = new OpenApiSchema { Type = "string", MinLength = 1, MaxLength = 50 }
                    }
                },
                ["ApiResponse_UserDto"] = new OpenApiSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, OpenApiSchema>
                    {
                        ["success"] = new OpenApiSchema { Type = "boolean" },
                        ["data"] = new OpenApiSchema
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = "UserDto" }
                        },
                        ["correlationId"] = new OpenApiSchema { Type = "string" },
                        ["timestamp"] = new OpenApiSchema { Type = "string", Format = "date-time" }
                    }
                },
                ["PaginatedResult_UserDto"] = new OpenApiSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, OpenApiSchema>
                    {
                        ["items"] = new OpenApiSchema
                        {
                            Type = "array",
                            Items = new OpenApiSchema
                            {
                                Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = "UserDto" }
                            }
                        },
                        ["totalCount"] = new OpenApiSchema { Type = "integer" },
                        ["page"] = new OpenApiSchema { Type = "integer" },
                        ["pageSize"] = new OpenApiSchema { Type = "integer" },
                        ["totalPages"] = new OpenApiSchema { Type = "integer" },
                        ["hasNextPage"] = new OpenApiSchema { Type = "boolean" },
                        ["hasPreviousPage"] = new OpenApiSchema { Type = "boolean" }
                    }
                }
            }
        };
        
        return components;
    }
}

[ApiController]
[Route("api/docs")]
public class ApiDocumentationController : ControllerBase
{
    private readonly ApiDocumentationGenerator _docGenerator;
    private readonly ApiVersionManager _versionManager;
    private readonly ILogger<ApiDocumentationController> _logger;
    
    public ApiDocumentationController(
        ApiDocumentationGenerator docGenerator,
        ApiVersionManager versionManager,
        ILogger<ApiDocumentationController> logger)
    {
        _docGenerator = docGenerator;
        _versionManager = versionManager;
        _logger = logger;
    }
    
    /// <summary>
    /// Gets OpenAPI specification for a specific version
    /// </summary>
    /// <param name="version">API version</param>
    /// <returns>OpenAPI specification</returns>
    [HttpGet("{version}")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<string>> GetOpenApiSpec(string version)
    {
        if (!_versionManager.IsVersionSupported(version))
        {
            return NotFound();
        }
        
        var spec = await _docGenerator.GenerateOpenApiSpecAsync(version);
        return Content(spec, "application/json");
    }
    
    /// <summary>
    /// Gets API documentation index
    /// </summary>
    /// <returns>API documentation index</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiDocumentationIndex), StatusCodes.Status200OK)]
    public ActionResult<ApiDocumentationIndex> GetDocumentationIndex()
    {
        var versions = _versionManager.GetAllVersions();
        
        var index = new ApiDocumentationIndex
        {
            Title = "TuskLang API Documentation",
            Description = "Comprehensive API documentation for TuskLang services",
            Versions = versions.Select(v => new ApiVersionDocumentation
            {
                Version = v.Version,
                ReleaseDate = v.ReleaseDate,
                IsDeprecated = v.IsDeprecated,
                DeprecationDate = v.DeprecationDate,
                DocumentationUrl = $"/api/docs/{v.Version}",
                MigrationGuideUrl = v.Version != "1.0" ? $"/api/versions/migration/1.0/{v.Version}" : null
            }).ToList()
        };
        
        return Ok(index);
    }
}

public class ApiDocumentationIndex
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<ApiVersionDocumentation> Versions { get; set; } = new();
}

public class ApiVersionDocumentation
{
    public string Version { get; set; } = string.Empty;
    public DateTime ReleaseDate { get; set; }
    public bool IsDeprecated { get; set; }
    public DateTime? DeprecationDate { get; set; }
    public string DocumentationUrl { get; set; } = string.Empty;
    public string? MigrationGuideUrl { get; set; }
}
```

## 📝 Summary

This guide covered comprehensive API design strategies for C# TuskLang applications:

- **RESTful API Design**: Controller base classes with standardized responses and error handling
- **API Versioning**: Version management with deprecation and migration guides
- **API Documentation**: OpenAPI specification generation and documentation endpoints
- **API Design Best Practices**: Proper HTTP status codes, validation, and response formats

These API design strategies ensure your C# TuskLang applications have well-designed, versioned, and documented APIs that are easy to use and maintain. 