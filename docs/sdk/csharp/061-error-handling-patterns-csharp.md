# Error Handling Patterns in C# with TuskLang

## Overview

This guide covers comprehensive error handling patterns for C# applications using TuskLang, including exception handling, error recovery, and fault tolerance strategies.

## Table of Contents

1. [Exception Handling](#exception-handling)
2. [Error Recovery](#error-recovery)
3. [Fault Tolerance](#fault-tolerance)
4. [Error Logging](#error-logging)
5. [TuskLang Integration](#tusklang-integration)

## Exception Handling

### Custom Exceptions

```csharp
public class TuskLangException : Exception
{
    public string ErrorCode { get; }
    public Dictionary<string, object> Context { get; }

    public TuskLangException(string message, string errorCode = null, 
        Dictionary<string, object> context = null, Exception innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode ?? "TUSKLANG_ERROR";
        Context = context ?? new Dictionary<string, object>();
    }
}

public class ConfigurationException : TuskLangException
{
    public string ConfigKey { get; }

    public ConfigurationException(string configKey, string message, 
        Exception innerException = null)
        : base(message, "CONFIG_ERROR", new Dictionary<string, object> { ["configKey"] = configKey }, innerException)
    {
        ConfigKey = configKey;
    }
}

public class ValidationException : TuskLangException
{
    public List<ValidationError> Errors { get; }

    public ValidationException(List<ValidationError> errors)
        : base("Validation failed", "VALIDATION_ERROR", 
            new Dictionary<string, object> { ["errorCount"] = errors.Count })
    {
        Errors = errors;
    }
}

public class ValidationError
{
    public string Field { get; set; }
    public string Message { get; set; }
    public string Code { get; set; }
}
```

### Exception Handling Middleware

```csharp
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly TuskLang _config;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, 
        ILogger<GlobalExceptionHandlerMiddleware> logger, TuskLang config)
    {
        _next = next;
        _logger = logger;
        _config = config;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var enableDetailedErrors = _config.GetValue<bool>("errorHandling.enableDetailedErrors", false);
        var logLevel = _config.GetValue<string>("errorHandling.logLevel", "Error");

        // Log the exception
        LogException(exception, logLevel);

        // Create error response
        var errorResponse = CreateErrorResponse(exception, enableDetailedErrors);

        // Set response
        context.Response.StatusCode = GetStatusCode(exception);
        context.Response.ContentType = "application/json";

        var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }

    private void LogException(Exception exception, string logLevel)
    {
        var logLevelEnum = Enum.Parse<LogLevel>(logLevel, true);

        _logger.Log(logLevelEnum, exception, "Unhandled exception: {Message}", exception.Message);
    }

    private object CreateErrorResponse(Exception exception, bool includeDetails)
    {
        var response = new
        {
            error = new
            {
                message = includeDetails ? exception.Message : "An error occurred",
                type = exception.GetType().Name,
                timestamp = DateTime.UtcNow
            }
        };

        if (exception is TuskLangException tuskException)
        {
            response = new
            {
                error = new
                {
                    message = includeDetails ? exception.Message : "A configuration error occurred",
                    type = exception.GetType().Name,
                    code = tuskException.ErrorCode,
                    context = includeDetails ? tuskException.Context : null,
                    timestamp = DateTime.UtcNow
                }
            };
        }

        if (includeDetails && exception.StackTrace != null)
        {
            response = new
            {
                error = new
                {
                    message = exception.Message,
                    type = exception.GetType().Name,
                    stackTrace = exception.StackTrace,
                    timestamp = DateTime.UtcNow
                }
            };
        }

        return response;
    }

    private int GetStatusCode(Exception exception)
    {
        return exception switch
        {
            ValidationException => 400,
            ConfigurationException => 500,
            UnauthorizedAccessException => 401,
            NotFoundException => 404,
            _ => 500
        };
    }
}
```

### Service-Level Error Handling

```csharp
public class UserService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserService> _logger;
    private readonly TuskLang _config;

    public UserService(IUserRepository userRepository, ILogger<UserService> logger, TuskLang config)
    {
        _userRepository = userRepository;
        _logger = logger;
        _config = config;
    }

    public async Task<User> GetUserByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
            {
                throw new ValidationException(new List<ValidationError>
                {
                    new() { Field = "id", Message = "User ID must be positive", Code = "INVALID_ID" }
                });
            }

            var user = await _userRepository.GetByIdAsync(id);
            
            if (user == null)
            {
                throw new NotFoundException($"User with ID {id} not found");
            }

            return user;
        }
        catch (ValidationException)
        {
            throw; // Re-throw validation exceptions
        }
        catch (NotFoundException)
        {
            throw; // Re-throw not found exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user with ID {UserId}", id);
            throw new TuskLangException($"Failed to retrieve user with ID {id}", 
                "USER_RETRIEVAL_ERROR", new Dictionary<string, object> { ["userId"] = id }, ex);
        }
    }

    public async Task<User> CreateUserAsync(CreateUserRequest request)
    {
        try
        {
            // Validate request
            var validationErrors = ValidateCreateUserRequest(request);
            if (validationErrors.Any())
            {
                throw new ValidationException(validationErrors);
            }

            // Check for duplicate email
            var existingUser = await _userRepository.GetByEmailAsync(request.Email);
            if (existingUser != null)
            {
                throw new ValidationException(new List<ValidationError>
                {
                    new() { Field = "email", Message = "Email already exists", Code = "DUPLICATE_EMAIL" }
                });
            }

            var user = new User
            {
                Name = request.Name,
                Email = request.Email,
                CreatedAt = DateTime.UtcNow
            };

            var createdUser = await _userRepository.CreateAsync(user);
            
            _logger.LogInformation("User created successfully. ID: {UserId}, Email: {Email}", 
                createdUser.Id, createdUser.Email);

            return createdUser;
        }
        catch (ValidationException)
        {
            throw; // Re-throw validation exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user with email {Email}", request.Email);
            throw new TuskLangException($"Failed to create user with email {request.Email}", 
                "USER_CREATION_ERROR", new Dictionary<string, object> { ["email"] = request.Email }, ex);
        }
    }

    private List<ValidationError> ValidateCreateUserRequest(CreateUserRequest request)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors.Add(new ValidationError
            {
                Field = "name",
                Message = "Name is required",
                Code = "REQUIRED_FIELD"
            });
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            errors.Add(new ValidationError
            {
                Field = "email",
                Message = "Email is required",
                Code = "REQUIRED_FIELD"
            });
        }
        else if (!IsValidEmail(request.Email))
        {
            errors.Add(new ValidationError
            {
                Field = "email",
                Message = "Invalid email format",
                Code = "INVALID_FORMAT"
            });
        }

        return errors;
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

## Error Recovery

### Retry Pattern

```csharp
public class RetryPolicy
{
    private readonly int _maxRetries;
    private readonly TimeSpan _baseDelay;
    private readonly double _backoffMultiplier;
    private readonly ILogger<RetryPolicy> _logger;

    public RetryPolicy(int maxRetries, TimeSpan baseDelay, double backoffMultiplier, 
        ILogger<RetryPolicy> logger)
    {
        _maxRetries = maxRetries;
        _baseDelay = baseDelay;
        _backoffMultiplier = backoffMultiplier;
        _logger = logger;
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string operationName)
    {
        var lastException = default(Exception);

        for (int attempt = 0; attempt <= _maxRetries; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (IsRetryableException(ex) && attempt < _maxRetries)
            {
                lastException = ex;
                var delay = CalculateDelay(attempt);
                
                _logger.LogWarning(ex, "Attempt {Attempt} failed for operation {Operation}. " +
                    "Retrying in {Delay}ms", attempt + 1, operationName, delay.TotalMilliseconds);

                await Task.Delay(delay);
            }
        }

        _logger.LogError(lastException, "Operation {Operation} failed after {Attempts} attempts", 
            operationName, _maxRetries + 1);
        
        throw new TuskLangException($"Operation {operationName} failed after {_maxRetries + 1} attempts", 
            "RETRY_EXHAUSTED", new Dictionary<string, object> { ["operation"] = operationName }, lastException);
    }

    private bool IsRetryableException(Exception exception)
    {
        return exception switch
        {
            TimeoutException => true,
            HttpRequestException => true,
            SqlException sqlEx => IsRetryableSqlException(sqlEx),
            _ => false
        };
    }

    private bool IsRetryableSqlException(SqlException sqlException)
    {
        // SQL Server error codes that are retryable
        var retryableCodes = new[] { 1205, 1222, 8645, 8651, -2, 53, 64, 233, 10053, 10054, 10060, 40197, 40501, 40613, 49918, 49919, 49920 };
        return retryableCodes.Contains(sqlException.Number);
    }

    private TimeSpan CalculateDelay(int attempt)
    {
        var delay = _baseDelay * Math.Pow(_backoffMultiplier, attempt);
        return TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds, 30000)); // Max 30 seconds
    }
}

public class ResilientUserService
{
    private readonly IUserRepository _userRepository;
    private readonly RetryPolicy _retryPolicy;
    private readonly ILogger<ResilientUserService> _logger;

    public ResilientUserService(IUserRepository userRepository, RetryPolicy retryPolicy, 
        ILogger<ResilientUserService> logger)
    {
        _userRepository = userRepository;
        _retryPolicy = retryPolicy;
        _logger = logger;
    }

    public async Task<User> GetUserByIdAsync(int id)
    {
        return await _retryPolicy.ExecuteAsync(
            () => _userRepository.GetByIdAsync(id),
            $"GetUserById_{id}");
    }

    public async Task<User> CreateUserAsync(CreateUserRequest request)
    {
        return await _retryPolicy.ExecuteAsync(
            () => _userRepository.CreateAsync(new User
            {
                Name = request.Name,
                Email = request.Email,
                CreatedAt = DateTime.UtcNow
            }),
            "CreateUser");
    }
}
```

### Circuit Breaker Pattern

```csharp
public class CircuitBreaker
{
    private readonly int _failureThreshold;
    private readonly TimeSpan _resetTimeout;
    private readonly ILogger<CircuitBreaker> _logger;

    private CircuitState _state = CircuitState.Closed;
    private int _failureCount = 0;
    private DateTime _lastFailureTime = DateTime.MinValue;

    public CircuitBreaker(int failureThreshold, TimeSpan resetTimeout, ILogger<CircuitBreaker> logger)
    {
        _failureThreshold = failureThreshold;
        _resetTimeout = resetTimeout;
        _logger = logger;
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string operationName)
    {
        if (_state == CircuitState.Open)
        {
            if (DateTime.UtcNow - _lastFailureTime >= _resetTimeout)
            {
                _logger.LogInformation("Circuit breaker for {Operation} transitioning to half-open", operationName);
                _state = CircuitState.HalfOpen;
            }
            else
            {
                throw new CircuitBreakerOpenException($"Circuit breaker is open for operation {operationName}");
            }
        }

        try
        {
            var result = await operation();
            
            if (_state == CircuitState.HalfOpen)
            {
                _logger.LogInformation("Circuit breaker for {Operation} transitioning to closed", operationName);
                _state = CircuitState.Closed;
                _failureCount = 0;
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _failureCount++;
            _lastFailureTime = DateTime.UtcNow;

            if (_failureCount >= _failureThreshold)
            {
                _logger.LogWarning("Circuit breaker for {Operation} transitioning to open after {Failures} failures", 
                    operationName, _failureCount);
                _state = CircuitState.Open;
            }

            throw;
        }
    }

    private enum CircuitState
    {
        Closed,
        Open,
        HalfOpen
    }
}

public class CircuitBreakerOpenException : Exception
{
    public CircuitBreakerOpenException(string message) : base(message) { }
}
```

## Fault Tolerance

### Fallback Pattern

```csharp
public class FallbackService
{
    private readonly IUserRepository _primaryRepository;
    private readonly IUserRepository _fallbackRepository;
    private readonly ILogger<FallbackService> _logger;
    private readonly TuskLang _config;

    public FallbackService(IUserRepository primaryRepository, IUserRepository fallbackRepository,
        ILogger<FallbackService> logger, TuskLang config)
    {
        _primaryRepository = primaryRepository;
        _fallbackRepository = fallbackRepository;
        _logger = logger;
        _config = config;
    }

    public async Task<User> GetUserByIdAsync(int id)
    {
        try
        {
            return await _primaryRepository.GetByIdAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Primary repository failed for user {UserId}, trying fallback", id);
            
            try
            {
                var user = await _fallbackRepository.GetByIdAsync(id);
                _logger.LogInformation("Fallback repository succeeded for user {UserId}", id);
                return user;
            }
            catch (Exception fallbackEx)
            {
                _logger.LogError(fallbackEx, "Both primary and fallback repositories failed for user {UserId}", id);
                throw new TuskLangException($"Failed to retrieve user {id} from both repositories", 
                    "REPOSITORY_FAILURE", new Dictionary<string, object> { ["userId"] = id }, fallbackEx);
            }
        }
    }

    public async Task<User> CreateUserAsync(CreateUserRequest request)
    {
        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            var createdUser = await _primaryRepository.CreateAsync(user);
            
            // Try to sync to fallback repository
            try
            {
                await _fallbackRepository.CreateAsync(user);
            }
            catch (Exception syncEx)
            {
                _logger.LogWarning(syncEx, "Failed to sync user {UserId} to fallback repository", createdUser.Id);
            }

            return createdUser;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Primary repository failed for user creation, trying fallback");
            
            try
            {
                var createdUser = await _fallbackRepository.CreateAsync(user);
                _logger.LogInformation("Fallback repository succeeded for user creation");
                return createdUser;
            }
            catch (Exception fallbackEx)
            {
                _logger.LogError(fallbackEx, "Both primary and fallback repositories failed for user creation");
                throw new TuskLangException("Failed to create user in both repositories", 
                    "REPOSITORY_FAILURE", new Dictionary<string, object> { ["email"] = request.Email }, fallbackEx);
            }
        }
    }
}
```

### Bulkhead Pattern

```csharp
public class BulkheadPolicy
{
    private readonly SemaphoreSlim _semaphore;
    private readonly ILogger<BulkheadPolicy> _logger;

    public BulkheadPolicy(int maxConcurrency, ILogger<BulkheadPolicy> logger)
    {
        _semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
        _logger = logger;
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string operationName)
    {
        if (!await _semaphore.WaitAsync(TimeSpan.FromSeconds(30)))
        {
            throw new BulkheadFullException($"Bulkhead is full for operation {operationName}");
        }

        try
        {
            return await operation();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Dispose()
    {
        _semaphore?.Dispose();
    }
}

public class BulkheadFullException : Exception
{
    public BulkheadFullException(string message) : base(message) { }
}

public class BulkheadUserService
{
    private readonly IUserRepository _userRepository;
    private readonly BulkheadPolicy _bulkheadPolicy;
    private readonly ILogger<BulkheadUserService> _logger;

    public BulkheadUserService(IUserRepository userRepository, BulkheadPolicy bulkheadPolicy,
        ILogger<BulkheadUserService> logger)
    {
        _userRepository = userRepository;
        _bulkheadPolicy = bulkheadPolicy;
        _logger = logger;
    }

    public async Task<User> GetUserByIdAsync(int id)
    {
        return await _bulkheadPolicy.ExecuteAsync(
            () => _userRepository.GetByIdAsync(id),
            $"GetUserById_{id}");
    }

    public async Task<User> CreateUserAsync(CreateUserRequest request)
    {
        return await _bulkheadPolicy.ExecuteAsync(
            () => _userRepository.CreateAsync(new User
            {
                Name = request.Name,
                Email = request.Email,
                CreatedAt = DateTime.UtcNow
            }),
            "CreateUser");
    }
}
```

## Error Logging

### Structured Error Logging

```csharp
public class ErrorLogger
{
    private readonly ILogger _logger;
    private readonly TuskLang _config;

    public ErrorLogger(ILogger logger, TuskLang config)
    {
        _logger = logger;
        _config = config;
    }

    public void LogError(Exception exception, string operation, Dictionary<string, object> context = null)
    {
        var logLevel = GetLogLevel(exception);
        var includeStackTrace = _config.GetValue<bool>("logging.includeStackTrace", false);
        var includeContext = _config.GetValue<bool>("logging.includeContext", true);

        var logData = new Dictionary<string, object>
        {
            ["operation"] = operation,
            ["exceptionType"] = exception.GetType().Name,
            ["message"] = exception.Message,
            ["timestamp"] = DateTime.UtcNow
        };

        if (includeStackTrace && exception.StackTrace != null)
        {
            logData["stackTrace"] = exception.StackTrace;
        }

        if (includeContext && context != null)
        {
            foreach (var (key, value) in context)
            {
                logData[$"context_{key}"] = value;
            }
        }

        if (exception is TuskLangException tuskException)
        {
            logData["errorCode"] = tuskException.ErrorCode;
            
            if (includeContext && tuskException.Context != null)
            {
                foreach (var (key, value) in tuskException.Context)
                {
                    logData[$"tuskContext_{key}"] = value;
                }
            }
        }

        _logger.Log(logLevel, exception, "Error in operation {Operation}: {Message}", 
            operation, exception.Message);
    }

    private LogLevel GetLogLevel(Exception exception)
    {
        return exception switch
        {
            ValidationException => LogLevel.Warning,
            NotFoundException => LogLevel.Information,
            UnauthorizedAccessException => LogLevel.Warning,
            TuskLangException => LogLevel.Error,
            _ => LogLevel.Error
        };
    }
}
```

## TuskLang Integration

### TuskLang Error Handling

```csharp
public class TuskLangErrorHandler
{
    private readonly TuskLang _config;
    private readonly ILogger<TuskLangErrorHandler> _logger;

    public TuskLangErrorHandler(TuskLang config, ILogger<TuskLangErrorHandler> logger)
    {
        _config = config;
        _logger = logger;
    }

    public T GetValueWithFallback<T>(string key, T defaultValue, string errorCode = null)
    {
        try
        {
            return _config.GetValue(key, defaultValue);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get configuration value for key {Key}, using default", key);
            
            if (!string.IsNullOrEmpty(errorCode))
            {
                throw new ConfigurationException(key, $"Failed to get configuration value for key {key}", ex);
            }
            
            return defaultValue;
        }
    }

    public void SetValueWithValidation<T>(string key, T value, Func<T, bool> validator = null)
    {
        try
        {
            if (validator != null && !validator(value))
            {
                throw new ValidationException(new List<ValidationError>
                {
                    new() { Field = key, Message = $"Invalid value for configuration key {key}", Code = "INVALID_VALUE" }
                });
            }

            _config.SetValue(key, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set configuration value for key {Key}", key);
            throw new ConfigurationException(key, $"Failed to set configuration value for key {key}", ex);
        }
    }

    public bool TryGetValue<T>(string key, out T value)
    {
        try
        {
            value = _config.GetValue<T>(key);
            return true;
        }
        catch
        {
            value = default;
            return false;
        }
    }
}
```

## Summary

This comprehensive error handling patterns guide covers:

- **Exception Handling**: Custom exceptions, middleware, and service-level error handling
- **Error Recovery**: Retry patterns and circuit breaker implementations
- **Fault Tolerance**: Fallback patterns and bulkhead isolation
- **Error Logging**: Structured logging with configurable detail levels
- **TuskLang Integration**: Configuration-driven error handling and validation

The patterns ensure robust error handling, graceful degradation, and comprehensive logging in C# applications using TuskLang. 