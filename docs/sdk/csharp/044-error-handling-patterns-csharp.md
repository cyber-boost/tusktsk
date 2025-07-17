# Error Handling Patterns in C# TuskLang

## Overview

Effective error handling is crucial for building resilient applications. This guide covers exception handling patterns, circuit breakers, retry mechanisms, and error recovery strategies for C# TuskLang applications.

## 🚨 Exception Handling Patterns

### Global Exception Handler

```csharp
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly TSKConfig _config;
    private readonly IErrorReportingService _errorReportingService;
    
    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        TSKConfig config,
        IErrorReportingService errorReportingService)
    {
        _logger = logger;
        _config = config;
        _errorReportingService = errorReportingService;
    }
    
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var errorId = Guid.NewGuid().ToString();
        var correlationId = httpContext.TraceIdentifier;
        
        // Log the exception
        _logger.LogError(exception, "Unhandled exception occurred. ErrorId: {ErrorId}, CorrelationId: {CorrelationId}",
            errorId, correlationId);
        
        // Report to error tracking service
        await _errorReportingService.ReportErrorAsync(exception, errorId, correlationId);
        
        // Determine response based on exception type
        var (statusCode, message) = GetErrorResponse(exception);
        
        // Create error response
        var errorResponse = new ErrorResponse
        {
            ErrorId = errorId,
            Message = message,
            CorrelationId = correlationId,
            Timestamp = DateTime.UtcNow
        };
        
        // Add details in development environment
        if (_config.Get<string>("app.environment") == "development")
        {
            errorResponse.Details = new ErrorDetails
            {
                ExceptionType = exception.GetType().Name,
                StackTrace = exception.StackTrace,
                InnerException = exception.InnerException?.Message
            };
        }
        
        // Set response
        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";
        
        var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        
        await httpContext.Response.WriteAsync(json, cancellationToken);
        
        return true;
    }
    
    private (int statusCode, string message) GetErrorResponse(Exception exception)
    {
        return exception switch
        {
            ValidationException => (400, "Validation failed"),
            NotFoundException => (404, "Resource not found"),
            UnauthorizedException => (401, "Unauthorized access"),
            ForbiddenException => (403, "Access forbidden"),
            TimeoutException => (408, "Request timeout"),
            DatabaseException => (503, "Database service unavailable"),
            ConfigurationException => (500, "Configuration error"),
            _ => (500, "An unexpected error occurred")
        };
    }
}

public class ErrorResponse
{
    public string ErrorId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public DateTime Timestamp { get; set; }
    public ErrorDetails? Details { get; set; }
}

public class ErrorDetails
{
    public string? ExceptionType { get; set; }
    public string? StackTrace { get; set; }
    public string? InnerException { get; set; }
}
```

### Exception Filters

```csharp
public class ExceptionFilter : IExceptionFilter
{
    private readonly ILogger<ExceptionFilter> _logger;
    private readonly TSKConfig _config;
    
    public ExceptionFilter(ILogger<ExceptionFilter> logger, TSKConfig config)
    {
        _logger = logger;
        _config = config;
    }
    
    public void OnException(ExceptionContext context)
    {
        var exception = context.Exception;
        var actionDescriptor = context.ActionDescriptor;
        var controllerName = actionDescriptor.ControllerName;
        var actionName = actionDescriptor.ActionName;
        
        // Log the exception
        _logger.LogError(exception, "Exception in {Controller}.{Action}: {Message}",
            controllerName, actionName, exception.Message);
        
        // Create error response
        var errorResponse = new ErrorResponse
        {
            ErrorId = Guid.NewGuid().ToString(),
            Message = GetUserFriendlyMessage(exception),
            CorrelationId = context.HttpContext.TraceIdentifier,
            Timestamp = DateTime.UtcNow
        };
        
        // Add details in development
        if (_config.Get<string>("app.environment") == "development")
        {
            errorResponse.Details = new ErrorDetails
            {
                ExceptionType = exception.GetType().Name,
                StackTrace = exception.StackTrace,
                InnerException = exception.InnerException?.Message
            };
        }
        
        // Set result
        context.Result = new JsonResult(errorResponse)
        {
            StatusCode = GetStatusCode(exception)
        };
        
        context.ExceptionHandled = true;
    }
    
    private string GetUserFriendlyMessage(Exception exception)
    {
        return exception switch
        {
            ValidationException => "The provided data is invalid",
            NotFoundException => "The requested resource was not found",
            UnauthorizedException => "You are not authorized to perform this action",
            ForbiddenException => "Access to this resource is forbidden",
            TimeoutException => "The operation timed out",
            DatabaseException => "A database error occurred",
            ConfigurationException => "A configuration error occurred",
            _ => "An unexpected error occurred"
        };
    }
    
    private int GetStatusCode(Exception exception)
    {
        return exception switch
        {
            ValidationException => 400,
            NotFoundException => 404,
            UnauthorizedException => 401,
            ForbiddenException => 403,
            TimeoutException => 408,
            DatabaseException => 503,
            ConfigurationException => 500,
            _ => 500
        };
    }
}
```

## 🔄 Circuit Breaker Pattern

### Circuit Breaker Implementation

```csharp
public class CircuitBreaker<T>
{
    private readonly ILogger<CircuitBreaker<T>> _logger;
    private readonly TSKConfig _config;
    private readonly string _operationName;
    private readonly object _lock = new();
    
    private CircuitBreakerState _state = CircuitBreakerState.Closed;
    private int _failureCount = 0;
    private DateTime _lastFailureTime = DateTime.MinValue;
    private DateTime _openUntil = DateTime.MinValue;
    
    public CircuitBreaker(ILogger<CircuitBreaker<T>> logger, TSKConfig config, string operationName)
    {
        _logger = logger;
        _config = config;
        _operationName = operationName;
    }
    
    public async Task<T> ExecuteAsync(Func<Task<T>> operation)
    {
        if (ShouldAllowExecution())
        {
            try
            {
                var result = await operation();
                OnSuccess();
                return result;
            }
            catch (Exception ex)
            {
                OnFailure(ex);
                throw;
            }
        }
        
        throw new CircuitBreakerOpenException(_operationName);
    }
    
    private bool ShouldAllowExecution()
    {
        lock (_lock)
        {
            switch (_state)
            {
                case CircuitBreakerState.Closed:
                    return true;
                    
                case CircuitBreakerState.Open:
                    if (DateTime.UtcNow >= _openUntil)
                    {
                        _state = CircuitBreakerState.HalfOpen;
                        _logger.LogInformation("Circuit breaker {OperationName} transitioning to half-open state", _operationName);
                        return true;
                    }
                    return false;
                    
                case CircuitBreakerState.HalfOpen:
                    return true;
                    
                default:
                    return false;
            }
        }
    }
    
    private void OnSuccess()
    {
        lock (_lock)
        {
            if (_state == CircuitBreakerState.HalfOpen)
            {
                _state = CircuitBreakerState.Closed;
                _failureCount = 0;
                _logger.LogInformation("Circuit breaker {OperationName} closed after successful operation", _operationName);
            }
        }
    }
    
    private void OnFailure(Exception exception)
    {
        lock (_lock)
        {
            _failureCount++;
            _lastFailureTime = DateTime.UtcNow;
            
            var failureThreshold = _config.Get<int>($"circuit_breaker.{_operationName}.failure_threshold", 5);
            var openDuration = _config.Get<int>($"circuit_breaker.{_operationName}.open_duration_seconds", 60);
            
            if (_failureCount >= failureThreshold)
            {
                _state = CircuitBreakerState.Open;
                _openUntil = DateTime.UtcNow.AddSeconds(openDuration);
                
                _logger.LogWarning("Circuit breaker {OperationName} opened after {FailureCount} failures. Will remain open until {OpenUntil}",
                    _operationName, _failureCount, _openUntil);
            }
        }
    }
    
    public CircuitBreakerStatus GetStatus()
    {
        lock (_lock)
        {
            return new CircuitBreakerStatus
            {
                State = _state,
                FailureCount = _failureCount,
                LastFailureTime = _lastFailureTime,
                OpenUntil = _openUntil
            };
        }
    }
    
    public void Reset()
    {
        lock (_lock)
        {
            _state = CircuitBreakerState.Closed;
            _failureCount = 0;
            _lastFailureTime = DateTime.MinValue;
            _openUntil = DateTime.MinValue;
            
            _logger.LogInformation("Circuit breaker {OperationName} manually reset", _operationName);
        }
    }
}

public enum CircuitBreakerState
{
    Closed,
    Open,
    HalfOpen
}

public class CircuitBreakerStatus
{
    public CircuitBreakerState State { get; set; }
    public int FailureCount { get; set; }
    public DateTime LastFailureTime { get; set; }
    public DateTime OpenUntil { get; set; }
}

public class CircuitBreakerOpenException : Exception
{
    public string OperationName { get; }
    
    public CircuitBreakerOpenException(string operationName)
        : base($"Circuit breaker is open for operation '{operationName}'")
    {
        OperationName = operationName;
    }
}
```

### Circuit Breaker Service

```csharp
public class CircuitBreakerService
{
    private readonly ILogger<CircuitBreakerService> _logger;
    private readonly TSKConfig _config;
    private readonly Dictionary<string, object> _circuitBreakers = new();
    private readonly object _lock = new();
    
    public CircuitBreakerService(ILogger<CircuitBreakerService> logger, TSKConfig config)
    {
        _logger = logger;
        _config = config;
    }
    
    public CircuitBreaker<T> GetCircuitBreaker<T>(string operationName)
    {
        lock (_lock)
        {
            var key = $"{typeof(T).Name}_{operationName}";
            
            if (!_circuitBreakers.ContainsKey(key))
            {
                _circuitBreakers[key] = new CircuitBreaker<T>(_logger, _config, operationName);
            }
            
            return (CircuitBreaker<T>)_circuitBreakers[key];
        }
    }
    
    public async Task<T> ExecuteWithCircuitBreakerAsync<T>(
        string operationName,
        Func<Task<T>> operation,
        Func<Task<T>>? fallback = null)
    {
        var circuitBreaker = GetCircuitBreaker<T>(operationName);
        
        try
        {
            return await circuitBreaker.ExecuteAsync(operation);
        }
        catch (CircuitBreakerOpenException)
        {
            if (fallback != null)
            {
                _logger.LogWarning("Circuit breaker open for {OperationName}, using fallback", operationName);
                return await fallback();
            }
            throw;
        }
    }
    
    public Dictionary<string, CircuitBreakerStatus> GetAllStatuses()
    {
        lock (_lock)
        {
            var statuses = new Dictionary<string, CircuitBreakerStatus>();
            
            foreach (var kvp in _circuitBreakers)
            {
                var circuitBreaker = kvp.Value;
                var statusMethod = circuitBreaker.GetType().GetMethod("GetStatus");
                if (statusMethod != null)
                {
                    var status = statusMethod.Invoke(circuitBreaker, null) as CircuitBreakerStatus;
                    if (status != null)
                    {
                        statuses[kvp.Key] = status;
                    }
                }
            }
            
            return statuses;
        }
    }
    
    public void ResetCircuitBreaker(string operationName)
    {
        lock (_lock)
        {
            var circuitBreaker = _circuitBreakers.Values.FirstOrDefault(cb => 
                cb.GetType().GetGenericArguments()[0].Name + "_" + operationName == 
                cb.GetType().GetProperty("OperationName")?.GetValue(cb)?.ToString());
            
            if (circuitBreaker != null)
            {
                var resetMethod = circuitBreaker.GetType().GetMethod("Reset");
                resetMethod?.Invoke(circuitBreaker, null);
            }
        }
    }
}
```

## 🔁 Retry Patterns

### Retry Policy

```csharp
public class RetryPolicy
{
    private readonly ILogger<RetryPolicy> _logger;
    private readonly TSKConfig _config;
    
    public RetryPolicy(ILogger<RetryPolicy> logger, TSKConfig config)
    {
        _logger = logger;
        _config = config;
    }
    
    public async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        string operationName,
        int maxRetries = 3,
        TimeSpan? baseDelay = null,
        Func<Exception, bool>? shouldRetry = null)
    {
        var delay = baseDelay ?? TimeSpan.FromSeconds(1);
        var lastException = (Exception?)null;
        
        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (ShouldRetry(ex, shouldRetry) && attempt < maxRetries)
            {
                lastException = ex;
                
                var waitTime = CalculateWaitTime(delay, attempt);
                
                _logger.LogWarning(ex, "Attempt {Attempt} failed for operation {OperationName}. Retrying in {WaitTime}ms",
                    attempt + 1, operationName, waitTime.TotalMilliseconds);
                
                await Task.Delay(waitTime);
            }
        }
        
        _logger.LogError(lastException, "Operation {OperationName} failed after {MaxRetries} retries",
            operationName, maxRetries);
        
        throw lastException!;
    }
    
    public async Task ExecuteWithRetryAsync(
        Func<Task> operation,
        string operationName,
        int maxRetries = 3,
        TimeSpan? baseDelay = null,
        Func<Exception, bool>? shouldRetry = null)
    {
        var delay = baseDelay ?? TimeSpan.FromSeconds(1);
        var lastException = (Exception?)null;
        
        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                await operation();
                return;
            }
            catch (Exception ex) when (ShouldRetry(ex, shouldRetry) && attempt < maxRetries)
            {
                lastException = ex;
                
                var waitTime = CalculateWaitTime(delay, attempt);
                
                _logger.LogWarning(ex, "Attempt {Attempt} failed for operation {OperationName}. Retrying in {WaitTime}ms",
                    attempt + 1, operationName, waitTime.TotalMilliseconds);
                
                await Task.Delay(waitTime);
            }
        }
        
        _logger.LogError(lastException, "Operation {OperationName} failed after {MaxRetries} retries",
            operationName, maxRetries);
        
        throw lastException!;
    }
    
    private bool ShouldRetry(Exception exception, Func<Exception, bool>? shouldRetry)
    {
        if (shouldRetry != null)
        {
            return shouldRetry(exception);
        }
        
        // Default retry conditions
        return exception is TimeoutException ||
               exception is HttpRequestException ||
               exception is TaskCanceledException ||
               (exception is SqlException sqlEx && IsRetryableSqlException(sqlEx));
    }
    
    private bool IsRetryableSqlException(SqlException sqlException)
    {
        // SQL Server error codes that indicate transient failures
        var retryableErrorCodes = new[] { 2, 53, 64, 233, 10053, 10054, 10060, 40197, 40501, 40613 };
        return retryableErrorCodes.Contains(sqlException.Number);
    }
    
    private TimeSpan CalculateWaitTime(TimeSpan baseDelay, int attempt)
    {
        // Exponential backoff with jitter
        var exponentialDelay = baseDelay * Math.Pow(2, attempt);
        var jitter = Random.Shared.NextDouble() * 0.1 * exponentialDelay.TotalMilliseconds;
        return exponentialDelay.Add(TimeSpan.FromMilliseconds(jitter));
    }
}
```

### Retry with Exponential Backoff

```csharp
public class ExponentialBackoffRetryPolicy
{
    private readonly ILogger<ExponentialBackoffRetryPolicy> _logger;
    private readonly TSKConfig _config;
    
    public ExponentialBackoffRetryPolicy(ILogger<ExponentialBackoffRetryPolicy> logger, TSKConfig config)
    {
        _logger = logger;
        _config = config;
    }
    
    public async Task<T> ExecuteAsync<T>(
        Func<Task<T>> operation,
        string operationName,
        RetryOptions? options = null)
    {
        options ??= new RetryOptions();
        
        var attempt = 0;
        var lastException = (Exception?)null;
        
        while (attempt <= options.MaxRetries)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (ShouldRetry(ex, options) && attempt < options.MaxRetries)
            {
                lastException = ex;
                attempt++;
                
                var waitTime = CalculateWaitTime(options, attempt);
                
                _logger.LogWarning(ex, "Attempt {Attempt} failed for operation {OperationName}. Retrying in {WaitTime}ms",
                    attempt, operationName, waitTime.TotalMilliseconds);
                
                await Task.Delay(waitTime);
            }
        }
        
        _logger.LogError(lastException, "Operation {OperationName} failed after {MaxRetries} retries",
            operationName, options.MaxRetries);
        
        throw lastException!;
    }
    
    private bool ShouldRetry(Exception exception, RetryOptions options)
    {
        // Check if exception is in the retryable exceptions list
        if (options.RetryableExceptions.Any(type => type.IsInstanceOfType(exception)))
        {
            return true;
        }
        
        // Check custom retry condition
        if (options.ShouldRetry != null)
        {
            return options.ShouldRetry(exception);
        }
        
        return false;
    }
    
    private TimeSpan CalculateWaitTime(RetryOptions options, int attempt)
    {
        var baseDelay = options.BaseDelay;
        var maxDelay = options.MaxDelay;
        
        // Calculate exponential delay
        var delay = baseDelay * Math.Pow(options.BackoffMultiplier, attempt - 1);
        
        // Apply jitter if enabled
        if (options.UseJitter)
        {
            var jitter = Random.Shared.NextDouble() * options.JitterFactor;
            delay = delay * (1 + jitter);
        }
        
        // Cap at maximum delay
        return TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds, maxDelay.TotalMilliseconds));
    }
}

public class RetryOptions
{
    public int MaxRetries { get; set; } = 3;
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromMinutes(1);
    public double BackoffMultiplier { get; set; } = 2.0;
    public bool UseJitter { get; set; } = true;
    public double JitterFactor { get; set; } = 0.1;
    public List<Type> RetryableExceptions { get; set; } = new();
    public Func<Exception, bool>? ShouldRetry { get; set; }
}
```

## 🔧 Error Recovery Strategies

### Graceful Degradation

```csharp
public class GracefulDegradationService
{
    private readonly ILogger<GracefulDegradationService> _logger;
    private readonly TSKConfig _config;
    private readonly Dictionary<string, object> _fallbackValues = new();
    
    public GracefulDegradationService(ILogger<GracefulDegradationService> logger, TSKConfig config)
    {
        _logger = logger;
        _config = config;
        InitializeFallbackValues();
    }
    
    public async Task<T> ExecuteWithFallbackAsync<T>(
        Func<Task<T>> primaryOperation,
        Func<Task<T>> fallbackOperation,
        string operationName)
    {
        try
        {
            return await primaryOperation();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Primary operation {OperationName} failed, using fallback", operationName);
            
            try
            {
                return await fallbackOperation();
            }
            catch (Exception fallbackEx)
            {
                _logger.LogError(fallbackEx, "Fallback operation {OperationName} also failed", operationName);
                throw;
            }
        }
    }
    
    public T GetValueWithFallback<T>(string key, T defaultValue)
    {
        try
        {
            var value = _config.Get<T>(key);
            return value ?? defaultValue;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get configuration value for key {Key}, using fallback", key);
            
            if (_fallbackValues.TryGetValue(key, out var fallbackValue))
            {
                return (T)fallbackValue;
            }
            
            return defaultValue;
        }
    }
    
    public async Task<T> GetCachedValueWithFallbackAsync<T>(
        string cacheKey,
        Func<Task<T>> dataProvider,
        Func<Task<T>> fallbackProvider,
        TimeSpan cacheDuration)
    {
        try
        {
            // Try to get from cache first
            var cachedValue = await GetFromCacheAsync<T>(cacheKey);
            if (cachedValue != null)
            {
                return cachedValue;
            }
            
            // Try primary data provider
            var value = await dataProvider();
            await SetCacheAsync(cacheKey, value, cacheDuration);
            return value;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Primary data provider failed for {CacheKey}, using fallback", cacheKey);
            
            try
            {
                var fallbackValue = await fallbackProvider();
                await SetCacheAsync(cacheKey, fallbackValue, cacheDuration);
                return fallbackValue;
            }
            catch (Exception fallbackEx)
            {
                _logger.LogError(fallbackEx, "Fallback data provider also failed for {CacheKey}", cacheKey);
                throw;
            }
        }
    }
    
    private void InitializeFallbackValues()
    {
        // Initialize fallback values for critical configuration
        _fallbackValues["database.connection_string"] = "Server=localhost;Database=fallback;";
        _fallbackValues["api.base_url"] = "https://fallback-api.example.com";
        _fallbackValues["cache.timeout"] = 30;
        _fallbackValues["logging.level"] = "Warning";
    }
    
    private async Task<T?> GetFromCacheAsync<T>(string key)
    {
        // Implementation depends on your caching mechanism
        await Task.CompletedTask;
        return default;
    }
    
    private async Task SetCacheAsync<T>(string key, T value, TimeSpan duration)
    {
        // Implementation depends on your caching mechanism
        await Task.CompletedTask;
    }
}
```

### Error Recovery Middleware

```csharp
public class ErrorRecoveryMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorRecoveryMiddleware> _logger;
    private readonly TSKConfig _config;
    
    public ErrorRecoveryMiddleware(
        RequestDelegate next,
        ILogger<ErrorRecoveryMiddleware> logger,
        TSKConfig config)
    {
        _next = next;
        _logger = logger;
        _config = config;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        var maxRetries = _config.Get<int>("error_recovery.max_retries", 3);
        var retryDelay = TimeSpan.FromMilliseconds(_config.Get<int>("error_recovery.retry_delay_ms", 100));
        
        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                await _next(context);
                return;
            }
            catch (Exception ex) when (IsRecoverableError(ex) && attempt < maxRetries)
            {
                _logger.LogWarning(ex, "Recoverable error on attempt {Attempt} for {Path}", 
                    attempt + 1, context.Request.Path);
                
                if (attempt < maxRetries - 1)
                {
                    await Task.Delay(retryDelay * (attempt + 1)); // Exponential backoff
                }
            }
        }
        
        // If we get here, all retries failed
        throw new InvalidOperationException("All recovery attempts failed");
    }
    
    private bool IsRecoverableError(Exception exception)
    {
        return exception is TimeoutException ||
               exception is HttpRequestException ||
               exception is TaskCanceledException ||
               exception is SqlException ||
               exception is IOException;
    }
}
```

## 📝 Summary

This guide covered comprehensive error handling patterns for C# TuskLang applications:

- **Exception Handling**: Global exception handlers and filters for consistent error responses
- **Circuit Breaker Pattern**: Circuit breaker implementation for fault tolerance
- **Retry Patterns**: Retry policies with exponential backoff and jitter
- **Error Recovery**: Graceful degradation and recovery middleware

These error handling patterns ensure your C# TuskLang applications are resilient, fault-tolerant, and provide excellent user experience even when errors occur. 