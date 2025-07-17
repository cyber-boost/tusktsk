# Error Handling in C# TuskLang

## Overview

Error handling is critical for building robust and reliable TuskLang applications. This guide covers exception handling, custom exceptions, error responses, logging, and recovery strategies for C# applications.

## 🚨 Exception Handling

### Global Exception Handler

```csharp
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly TSKConfig _config;
    
    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, TSKConfig config)
    {
        _logger = logger;
        _config = config;
    }
    
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var errorResponse = new ErrorResponse();
        var statusCode = HttpStatusCode.InternalServerError;
        
        switch (exception)
        {
            case ValidationException validationEx:
                errorResponse.Message = "Validation failed";
                errorResponse.Errors = new List<string> { validationEx.Message };
                statusCode = HttpStatusCode.BadRequest;
                break;
                
            case NotFoundException notFoundEx:
                errorResponse.Message = notFoundEx.Message;
                statusCode = HttpStatusCode.NotFound;
                break;
                
            case UnauthorizedException unauthorizedEx:
                errorResponse.Message = unauthorizedEx.Message;
                statusCode = HttpStatusCode.Unauthorized;
                break;
                
            case ForbiddenException forbiddenEx:
                errorResponse.Message = forbiddenEx.Message;
                statusCode = HttpStatusCode.Forbidden;
                break;
                
            case DatabaseException dbEx:
                errorResponse.Message = "Database operation failed";
                statusCode = HttpStatusCode.InternalServerError;
                _logger.LogError(dbEx, "Database error occurred");
                break;
                
            case ConfigurationException configEx:
                errorResponse.Message = "Configuration error";
                errorResponse.Errors = new List<string> { configEx.Message };
                statusCode = HttpStatusCode.InternalServerError;
                _logger.LogError(configEx, "Configuration error occurred");
                break;
                
            default:
                errorResponse.Message = "An unexpected error occurred";
                statusCode = HttpStatusCode.InternalServerError;
                _logger.LogError(exception, "Unhandled exception occurred");
                break;
        }
        
        // Add trace ID for debugging
        errorResponse.TraceId = httpContext.TraceIdentifier;
        
        // Include detailed error information in development
        if (_config.Get<string>("app.environment") == "development")
        {
            errorResponse.Details = new ErrorDetails
            {
                ExceptionType = exception.GetType().Name,
                StackTrace = exception.StackTrace,
                InnerException = exception.InnerException?.Message
            };
        }
        
        httpContext.Response.StatusCode = (int)statusCode;
        httpContext.Response.ContentType = "application/json";
        
        var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        
        await httpContext.Response.WriteAsync(json, cancellationToken);
        
        return true;
    }
}

public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
    public string? TraceId { get; set; }
    public ErrorDetails? Details { get; set; }
}

public class ErrorDetails
{
    public string? ExceptionType { get; set; }
    public string? StackTrace { get; set; }
    public string? InnerException { get; set; }
}
```

### Custom Exceptions

```csharp
public class ValidationException : Exception
{
    public List<string> Errors { get; }
    
    public ValidationException(string message) : base(message)
    {
        Errors = new List<string> { message };
    }
    
    public ValidationException(List<string> errors) : base("Validation failed")
    {
        Errors = errors;
    }
}

public class NotFoundException : Exception
{
    public string ResourceType { get; }
    public object ResourceId { get; }
    
    public NotFoundException(string resourceType, object resourceId)
        : base($"{resourceType} with id {resourceId} not found")
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
    }
    
    public NotFoundException(string message) : base(message)
    {
        ResourceType = "Resource";
        ResourceId = "unknown";
    }
}

public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message = "Unauthorized access") : base(message)
    {
    }
}

public class ForbiddenException : Exception
{
    public ForbiddenException(string message = "Access forbidden") : base(message)
    {
    }
}

public class DatabaseException : Exception
{
    public string Operation { get; }
    public string? SqlQuery { get; }
    
    public DatabaseException(string operation, string message, Exception? innerException = null)
        : base($"Database operation '{operation}' failed: {message}", innerException)
    {
        Operation = operation;
    }
    
    public DatabaseException(string operation, string message, string sqlQuery, Exception? innerException = null)
        : base($"Database operation '{operation}' failed: {message}", innerException)
    {
        Operation = operation;
        SqlQuery = sqlQuery;
    }
}

public class ConfigurationException : Exception
{
    public string ConfigurationKey { get; }
    
    public ConfigurationException(string configurationKey, string message)
        : base($"Configuration error for key '{configurationKey}': {message}")
    {
        ConfigurationKey = configurationKey;
    }
}

public class TuskLangException : Exception
{
    public string TuskLangCode { get; }
    public string? FilePath { get; }
    
    public TuskLangException(string tuskLangCode, string message, string? filePath = null)
        : base($"TuskLang error in code '{tuskLangCode}': {message}")
    {
        TuskLangCode = tuskLangCode;
        FilePath = filePath;
    }
}
```

## 🔄 Retry Policies

### Circuit Breaker Pattern

```csharp
public class CircuitBreakerPolicy
{
    private readonly ILogger<CircuitBreakerPolicy> _logger;
    private readonly TSKConfig _config;
    private readonly Dictionary<string, CircuitBreakerState> _circuitBreakers = new();
    private readonly object _lock = new();
    
    public CircuitBreakerPolicy(ILogger<CircuitBreakerPolicy> logger, TSKConfig config)
    {
        _logger = logger;
        _config = config;
    }
    
    public async Task<T> ExecuteAsync<T>(string operationName, Func<Task<T>> operation)
    {
        var circuitBreaker = GetOrCreateCircuitBreaker(operationName);
        
        if (circuitBreaker.State == CircuitBreakerState.Open)
        {
            if (DateTime.UtcNow < circuitBreaker.OpenUntil)
            {
                throw new CircuitBreakerOpenException(operationName);
            }
            
            // Try to transition to half-open
            circuitBreaker.State = CircuitBreakerState.HalfOpen;
        }
        
        try
        {
            var result = await operation();
            
            // Success - reset failure count
            circuitBreaker.FailureCount = 0;
            circuitBreaker.State = CircuitBreakerState.Closed;
            
            return result;
        }
        catch (Exception ex)
        {
            circuitBreaker.FailureCount++;
            
            var failureThreshold = _config.Get<int>($"circuit_breaker.{operationName}.failure_threshold", 5);
            var openDuration = _config.Get<int>($"circuit_breaker.{operationName}.open_duration_seconds", 60);
            
            if (circuitBreaker.FailureCount >= failureThreshold)
            {
                circuitBreaker.State = CircuitBreakerState.Open;
                circuitBreaker.OpenUntil = DateTime.UtcNow.AddSeconds(openDuration);
                
                _logger.LogWarning("Circuit breaker opened for operation {OperationName} after {FailureCount} failures",
                    operationName, circuitBreaker.FailureCount);
            }
            
            throw;
        }
    }
    
    private CircuitBreakerState GetOrCreateCircuitBreaker(string operationName)
    {
        lock (_lock)
        {
            if (!_circuitBreakers.ContainsKey(operationName))
            {
                _circuitBreakers[operationName] = new CircuitBreakerState();
            }
            
            return _circuitBreakers[operationName];
        }
    }
}

public class CircuitBreakerState
{
    public CircuitBreakerStateEnum State { get; set; } = CircuitBreakerStateEnum.Closed;
    public int FailureCount { get; set; }
    public DateTime OpenUntil { get; set; }
}

public enum CircuitBreakerStateEnum
{
    Closed,
    Open,
    HalfOpen
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

### Retry Policy with Exponential Backoff

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
        TimeSpan? baseDelay = null)
    {
        var delay = baseDelay ?? TimeSpan.FromSeconds(1);
        var lastException = (Exception?)null;
        
        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (IsRetryableException(ex) && attempt < maxRetries)
            {
                lastException = ex;
                
                var waitTime = delay * Math.Pow(2, attempt); // Exponential backoff
                var jitter = Random.Shared.NextDouble() * 0.1 * waitTime.TotalMilliseconds; // Add jitter
                waitTime = waitTime.Add(TimeSpan.FromMilliseconds(jitter));
                
                _logger.LogWarning(ex, "Attempt {Attempt} failed for operation {OperationName}. Retrying in {WaitTime}ms",
                    attempt + 1, operationName, waitTime.TotalMilliseconds);
                
                await Task.Delay(waitTime);
            }
        }
        
        _logger.LogError(lastException, "Operation {OperationName} failed after {MaxRetries} retries",
            operationName, maxRetries);
        
        throw lastException!;
    }
    
    private bool IsRetryableException(Exception exception)
    {
        // Retry on transient exceptions
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
}
```

## 📝 Error Logging

### Structured Error Logging

```csharp
public class ErrorLogger
{
    private readonly ILogger<ErrorLogger> _logger;
    private readonly TSKConfig _config;
    
    public ErrorLogger(ILogger<ErrorLogger> logger, TSKConfig config)
    {
        _logger = logger;
        _config = config;
    }
    
    public void LogError(Exception exception, string operation, Dictionary<string, object>? context = null)
    {
        var logLevel = GetLogLevel(exception);
        
        var logData = new
        {
            Operation = operation,
            ExceptionType = exception.GetType().Name,
            Message = exception.Message,
            StackTrace = exception.StackTrace,
            InnerException = exception.InnerException?.Message,
            Context = context ?? new Dictionary<string, object>(),
            Timestamp = DateTime.UtcNow,
            Environment = _config.Get<string>("app.environment", "unknown")
        };
        
        _logger.Log(logLevel, exception, "Error in operation {Operation}: {Message}", operation, exception.Message);
        
        // Log additional context if available
        if (context != null && context.Any())
        {
            _logger.Log(logLevel, "Error context: {@Context}", context);
        }
    }
    
    public void LogValidationError(List<string> errors, string operation, Dictionary<string, object>? context = null)
    {
        var logData = new
        {
            Operation = operation,
            ErrorType = "Validation",
            Errors = errors,
            Context = context ?? new Dictionary<string, object>(),
            Timestamp = DateTime.UtcNow
        };
        
        _logger.LogWarning("Validation errors in operation {Operation}: {@Errors}", operation, errors);
        
        if (context != null && context.Any())
        {
            _logger.LogWarning("Validation context: {@Context}", context);
        }
    }
    
    public void LogConfigurationError(string configurationKey, string message, Exception? exception = null)
    {
        var logData = new
        {
            ConfigurationKey = configurationKey,
            Message = message,
            Exception = exception?.Message,
            Timestamp = DateTime.UtcNow
        };
        
        if (exception != null)
        {
            _logger.LogError(exception, "Configuration error for key {ConfigurationKey}: {Message}", 
                configurationKey, message);
        }
        else
        {
            _logger.LogError("Configuration error for key {ConfigurationKey}: {Message}", 
                configurationKey, message);
        }
    }
    
    private LogLevel GetLogLevel(Exception exception)
    {
        return exception switch
        {
            ValidationException => LogLevel.Warning,
            NotFoundException => LogLevel.Information,
            UnauthorizedException => LogLevel.Warning,
            ForbiddenException => LogLevel.Warning,
            DatabaseException => LogLevel.Error,
            ConfigurationException => LogLevel.Critical,
            _ => LogLevel.Error
        };
    }
}
```

## 🔧 Error Recovery

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
    
    public T GetConfigurationWithFallback<T>(string key, T defaultValue)
    {
        try
        {
            return _config.Get<T>(key, defaultValue);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get configuration {Key}, using fallback value", key);
            
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

### Health Checks

```csharp
public class HealthCheckService : IHealthCheck
{
    private readonly IDbConnection _connection;
    private readonly TSKConfig _config;
    private readonly ILogger<HealthCheckService> _logger;
    
    public HealthCheckService(IDbConnection connection, TSKConfig config, ILogger<HealthCheckService> logger)
    {
        _connection = connection;
        _config = config;
        _logger = logger;
    }
    
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var checks = new List<HealthCheckResult>();
        
        // Database health check
        checks.Add(await CheckDatabaseHealthAsync());
        
        // Configuration health check
        checks.Add(await CheckConfigurationHealthAsync());
        
        // External service health check
        checks.Add(await CheckExternalServiceHealthAsync());
        
        var unhealthyChecks = checks.Where(c => c.Status == HealthStatus.Unhealthy).ToList();
        var degradedChecks = checks.Where(c => c.Status == HealthStatus.Degraded).ToList();
        
        if (unhealthyChecks.Any())
        {
            return HealthCheckResult.Unhealthy(
                "Health check failed",
                data: new Dictionary<string, object>
                {
                    ["unhealthy_checks"] = unhealthyChecks.Count,
                    ["degraded_checks"] = degradedChecks.Count
                });
        }
        
        if (degradedChecks.Any())
        {
            return HealthCheckResult.Degraded(
                "Some health checks are degraded",
                data: new Dictionary<string, object>
                {
                    ["degraded_checks"] = degradedChecks.Count
                });
        }
        
        return HealthCheckResult.Healthy("All health checks passed");
    }
    
    private async Task<HealthCheckResult> CheckDatabaseHealthAsync()
    {
        try
        {
            await _connection.OpenAsync();
            await _connection.ExecuteScalarAsync<int>("SELECT 1");
            await _connection.CloseAsync();
            
            return HealthCheckResult.Healthy("Database connection is healthy");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return HealthCheckResult.Unhealthy("Database connection failed", ex);
        }
    }
    
    private async Task<HealthCheckResult> CheckConfigurationHealthAsync()
    {
        try
        {
            var requiredKeys = new[] { "database.connection_string", "api.base_url", "security.jwt_secret" };
            var missingKeys = new List<string>();
            
            foreach (var key in requiredKeys)
            {
                if (!_config.Has(key))
                {
                    missingKeys.Add(key);
                }
            }
            
            if (missingKeys.Any())
            {
                return HealthCheckResult.Unhealthy(
                    "Missing required configuration keys",
                    data: new Dictionary<string, object>
                    {
                        ["missing_keys"] = missingKeys
                    });
            }
            
            return HealthCheckResult.Healthy("Configuration is healthy");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Configuration health check failed");
            return HealthCheckResult.Unhealthy("Configuration check failed", ex);
        }
    }
    
    private async Task<HealthCheckResult> CheckExternalServiceHealthAsync()
    {
        try
        {
            var apiUrl = _config.Get<string>("api.base_url");
            if (string.IsNullOrEmpty(apiUrl))
            {
                return HealthCheckResult.Degraded("API URL not configured");
            }
            
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            
            var response = await client.GetAsync($"{apiUrl}/health");
            
            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy("External service is healthy");
            }
            
            return HealthCheckResult.Degraded(
                "External service returned non-success status",
                data: new Dictionary<string, object>
                {
                    ["status_code"] = (int)response.StatusCode
                });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "External service health check failed");
            return HealthCheckResult.Degraded("External service is unavailable", ex);
        }
    }
}
```

## 📊 Error Monitoring

### Error Metrics Collection

```csharp
public class ErrorMetricsCollector
{
    private readonly ILogger<ErrorMetricsCollector> _logger;
    private readonly TSKConfig _config;
    private readonly Dictionary<string, ErrorMetrics> _metrics = new();
    private readonly object _lock = new();
    
    public ErrorMetricsCollector(ILogger<ErrorMetricsCollector> logger, TSKConfig config)
    {
        _logger = logger;
        _config = config;
    }
    
    public void RecordError(string operation, Exception exception)
    {
        lock (_lock)
        {
            if (!_metrics.ContainsKey(operation))
            {
                _metrics[operation] = new ErrorMetrics();
            }
            
            var metrics = _metrics[operation];
            metrics.TotalErrors++;
            metrics.LastErrorTime = DateTime.UtcNow;
            metrics.LastErrorMessage = exception.Message;
            
            var exceptionType = exception.GetType().Name;
            if (!metrics.ErrorTypes.ContainsKey(exceptionType))
            {
                metrics.ErrorTypes[exceptionType] = 0;
            }
            metrics.ErrorTypes[exceptionType]++;
        }
    }
    
    public void RecordSuccess(string operation)
    {
        lock (_lock)
        {
            if (!_metrics.ContainsKey(operation))
            {
                _metrics[operation] = new ErrorMetrics();
            }
            
            var metrics = _metrics[operation];
            metrics.TotalSuccesses++;
            metrics.LastSuccessTime = DateTime.UtcNow;
        }
    }
    
    public ErrorMetrics GetMetrics(string operation)
    {
        lock (_lock)
        {
            return _metrics.GetValueOrDefault(operation, new ErrorMetrics());
        }
    }
    
    public Dictionary<string, ErrorMetrics> GetAllMetrics()
    {
        lock (_lock)
        {
            return new Dictionary<string, ErrorMetrics>(_metrics);
        }
    }
    
    public async Task<ErrorReport> GenerateErrorReportAsync()
    {
        var allMetrics = GetAllMetrics();
        var report = new ErrorReport
        {
            GeneratedAt = DateTime.UtcNow,
            TotalOperations = allMetrics.Count,
            OperationsWithErrors = allMetrics.Count(kvp => kvp.Value.TotalErrors > 0),
            TotalErrors = allMetrics.Values.Sum(m => m.TotalErrors),
            TotalSuccesses = allMetrics.Values.Sum(m => m.TotalSuccesses),
            Operations = allMetrics.Select(kvp => new OperationMetrics
            {
                OperationName = kvp.Key,
                TotalErrors = kvp.Value.TotalErrors,
                TotalSuccesses = kvp.Value.TotalSuccesses,
                ErrorRate = kvp.Value.TotalErrors + kvp.Value.TotalSuccesses > 0 
                    ? (double)kvp.Value.TotalErrors / (kvp.Value.TotalErrors + kvp.Value.TotalSuccesses) 
                    : 0,
                LastErrorTime = kvp.Value.LastErrorTime,
                LastSuccessTime = kvp.Value.LastSuccessTime,
                ErrorTypes = new Dictionary<string, int>(kvp.Value.ErrorTypes)
            }).ToList()
        };
        
        return report;
    }
}

public class ErrorMetrics
{
    public int TotalErrors { get; set; }
    public int TotalSuccesses { get; set; }
    public DateTime? LastErrorTime { get; set; }
    public DateTime? LastSuccessTime { get; set; }
    public string? LastErrorMessage { get; set; }
    public Dictionary<string, int> ErrorTypes { get; set; } = new();
}

public class ErrorReport
{
    public DateTime GeneratedAt { get; set; }
    public int TotalOperations { get; set; }
    public int OperationsWithErrors { get; set; }
    public int TotalErrors { get; set; }
    public int TotalSuccesses { get; set; }
    public List<OperationMetrics> Operations { get; set; } = new();
}

public class OperationMetrics
{
    public string OperationName { get; set; } = string.Empty;
    public int TotalErrors { get; set; }
    public int TotalSuccesses { get; set; }
    public double ErrorRate { get; set; }
    public DateTime? LastErrorTime { get; set; }
    public DateTime? LastSuccessTime { get; set; }
    public Dictionary<string, int> ErrorTypes { get; set; } = new();
}
```

## 📝 Summary

This guide covered comprehensive error handling strategies for C# TuskLang applications:

- **Exception Handling**: Global exception handler with custom exceptions and structured error responses
- **Retry Policies**: Circuit breaker pattern and exponential backoff retry policies
- **Error Logging**: Structured error logging with context and appropriate log levels
- **Error Recovery**: Graceful degradation and health checks for system resilience
- **Error Monitoring**: Error metrics collection and reporting for operational insights

These error handling strategies ensure your C# TuskLang applications are robust, resilient, and maintainable in production environments. 