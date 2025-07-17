# 🛑 Error Handling Patterns - TuskLang for C# - "Resilience Mastery"

**Master error handling patterns with TuskLang in your C# applications!**

Error handling is crucial for building resilient applications. This guide covers exception handling, error recovery, circuit breakers, and real-world error handling scenarios for TuskLang in C# environments.

## 🛡️ Error Handling Philosophy

### "We Don't Bow to Any King"
- **Fail gracefully** - Never crash, always recover
- **Fail fast** - Detect errors early
- **Retry intelligently** - Use exponential backoff
- **Circuit breakers** - Prevent cascading failures
- **Monitor everything** - Track error patterns

## 🔄 Exception Handling

### Example: Exception Handling Service
```csharp
// ExceptionHandlingService.cs
public class ExceptionHandlingService
{
    private readonly TuskLang _parser;
    private readonly ILogger<ExceptionHandlingService> _logger;
    private readonly Dictionary<string, ExceptionHandler> _exceptionHandlers;
    
    public ExceptionHandlingService(ILogger<ExceptionHandlingService> logger)
    {
        _parser = new TuskLang();
        _logger = logger;
        _exceptionHandlers = new Dictionary<string, ExceptionHandler>();
        
        RegisterExceptionHandlers();
    }
    
    private void RegisterExceptionHandlers()
    {
        // Register database exception handler
        _exceptionHandlers["DatabaseException"] = new ExceptionHandler
        {
            ExceptionType = typeof(SqlException),
            RetryCount = 3,
            RetryDelay = TimeSpan.FromSeconds(1),
            FallbackAction = () => new Dictionary<string, object> { ["error"] = "Database temporarily unavailable" }
        };
        
        // Register network exception handler
        _exceptionHandlers["NetworkException"] = new ExceptionHandler
        {
            ExceptionType = typeof(HttpRequestException),
            RetryCount = 2,
            RetryDelay = TimeSpan.FromSeconds(2),
            FallbackAction = () => new Dictionary<string, object> { ["error"] = "Network temporarily unavailable" }
        };
        
        // Register validation exception handler
        _exceptionHandlers["ValidationException"] = new ExceptionHandler
        {
            ExceptionType = typeof(ValidationException),
            RetryCount = 0,
            RetryDelay = TimeSpan.Zero,
            FallbackAction = () => new Dictionary<string, object> { ["error"] = "Invalid input data" }
        };
        
        _logger.LogInformation("Registered {Count} exception handlers", _exceptionHandlers.Count);
    }
    
    public async Task<T> ExecuteWithErrorHandlingAsync<T>(Func<Task<T>> operation, string operationName)
    {
        try
        {
            _logger.LogDebug("Executing operation: {OperationName}", operationName);
            var result = await operation();
            _logger.LogDebug("Operation {OperationName} completed successfully", operationName);
            return result;
        }
        catch (Exception ex)
        {
            var handler = GetExceptionHandler(ex);
            if (handler != null)
            {
                return await HandleExceptionWithRetryAsync(operation, handler, operationName);
            }
            
            _logger.LogError(ex, "Unhandled exception in operation {OperationName}", operationName);
            throw;
        }
    }
    
    private ExceptionHandler? GetExceptionHandler(Exception exception)
    {
        foreach (var handler in _exceptionHandlers.Values)
        {
            if (handler.ExceptionType.IsInstanceOfType(exception))
            {
                return handler;
            }
        }
        return null;
    }
    
    private async Task<T> HandleExceptionWithRetryAsync<T>(Func<Task<T>> operation, ExceptionHandler handler, string operationName)
    {
        for (int attempt = 1; attempt <= handler.RetryCount; attempt++)
        {
            try
            {
                _logger.LogWarning("Retry attempt {Attempt} for operation {OperationName}", attempt, operationName);
                await Task.Delay(handler.RetryDelay * attempt); // Exponential backoff
                
                var result = await operation();
                _logger.LogInformation("Operation {OperationName} succeeded on retry attempt {Attempt}", operationName, attempt);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Retry attempt {Attempt} failed for operation {OperationName}", attempt, operationName);
                
                if (attempt == handler.RetryCount)
                {
                    _logger.LogError(ex, "All retry attempts failed for operation {OperationName}, using fallback", operationName);
                    return (T)handler.FallbackAction();
                }
            }
        }
        
        return (T)handler.FallbackAction();
    }
}

public class ExceptionHandler
{
    public Type ExceptionType { get; set; } = typeof(Exception);
    public int RetryCount { get; set; }
    public TimeSpan RetryDelay { get; set; }
    public Func<object> FallbackAction { get; set; } = () => new { error = "Operation failed" };
}

public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}
```

## 🔌 Circuit Breaker Pattern

### Example: Circuit Breaker Service
```csharp
// CircuitBreakerService.cs
public class CircuitBreakerService
{
    private readonly TuskLang _parser;
    private readonly ILogger<CircuitBreakerService> _logger;
    private readonly Dictionary<string, CircuitBreaker> _circuitBreakers;
    
    public CircuitBreakerService(ILogger<CircuitBreakerService> logger)
    {
        _parser = new TuskLang();
        _logger = logger;
        _circuitBreakers = new Dictionary<string, CircuitBreaker>();
        
        LoadCircuitBreakerConfiguration();
    }
    
    private void LoadCircuitBreakerConfiguration()
    {
        var config = _parser.ParseFile("config/circuit-breakers.tsk");
        var breakers = config["circuit_breakers"] as Dictionary<string, object>;
        
        foreach (var breaker in breakers ?? new Dictionary<string, object>())
        {
            var breakerName = breaker.Key;
            var breakerConfig = breaker.Value as Dictionary<string, object>;
            
            if (breakerConfig != null)
            {
                _circuitBreakers[breakerName] = new CircuitBreaker
                {
                    Name = breakerName,
                    FailureThreshold = int.Parse(breakerConfig["failure_threshold"].ToString()),
                    RecoveryTimeout = TimeSpan.FromSeconds(int.Parse(breakerConfig["recovery_timeout_seconds"].ToString())),
                    MonitoringWindow = TimeSpan.FromSeconds(int.Parse(breakerConfig["monitoring_window_seconds"].ToString()))
                };
            }
        }
        
        _logger.LogInformation("Loaded {Count} circuit breaker configurations", _circuitBreakers.Count);
    }
    
    public async Task<T> ExecuteWithCircuitBreakerAsync<T>(string breakerName, Func<Task<T>> operation)
    {
        if (!_circuitBreakers.ContainsKey(breakerName))
        {
            _logger.LogWarning("Circuit breaker {BreakerName} not found, executing without circuit breaker", breakerName);
            return await operation();
        }
        
        var circuitBreaker = _circuitBreakers[breakerName];
        
        switch (circuitBreaker.State)
        {
            case CircuitBreakerState.Closed:
                return await ExecuteInClosedStateAsync(circuitBreaker, operation);
                
            case CircuitBreakerState.Open:
                return await ExecuteInOpenStateAsync(circuitBreaker, operation);
                
            case CircuitBreakerState.HalfOpen:
                return await ExecuteInHalfOpenStateAsync(circuitBreaker, operation);
                
            default:
                throw new InvalidOperationException($"Unknown circuit breaker state: {circuitBreaker.State}");
        }
    }
    
    private async Task<T> ExecuteInClosedStateAsync<T>(CircuitBreaker circuitBreaker, Func<Task<T>> operation)
    {
        try
        {
            var result = await operation();
            circuitBreaker.RecordSuccess();
            return result;
        }
        catch (Exception ex)
        {
            circuitBreaker.RecordFailure();
            _logger.LogWarning(ex, "Operation failed in closed state for circuit breaker {BreakerName}", circuitBreaker.Name);
            throw;
        }
    }
    
    private async Task<T> ExecuteInOpenStateAsync<T>(CircuitBreaker circuitBreaker, Func<Task<T>> operation)
    {
        if (circuitBreaker.ShouldAttemptReset())
        {
            _logger.LogInformation("Attempting to reset circuit breaker {BreakerName}", circuitBreaker.Name);
            circuitBreaker.SetHalfOpen();
            return await ExecuteInHalfOpenStateAsync(circuitBreaker, operation);
        }
        
        _logger.LogWarning("Circuit breaker {BreakerName} is open, returning fallback", circuitBreaker.Name);
        return (T)circuitBreaker.FallbackAction();
    }
    
    private async Task<T> ExecuteInHalfOpenStateAsync<T>(CircuitBreaker circuitBreaker, Func<Task<T>> operation)
    {
        try
        {
            var result = await operation();
            circuitBreaker.SetClosed();
            _logger.LogInformation("Circuit breaker {BreakerName} reset to closed state", circuitBreaker.Name);
            return result;
        }
        catch (Exception ex)
        {
            circuitBreaker.SetOpen();
            _logger.LogWarning(ex, "Operation failed in half-open state for circuit breaker {BreakerName}", circuitBreaker.Name);
            return (T)circuitBreaker.FallbackAction();
        }
    }
    
    public async Task<Dictionary<string, object>> GetCircuitBreakerStatusAsync()
    {
        var status = new Dictionary<string, object>();
        
        foreach (var breaker in _circuitBreakers)
        {
            status[breaker.Key] = new Dictionary<string, object>
            {
                ["state"] = breaker.Value.State.ToString(),
                ["failure_count"] = breaker.Value.FailureCount,
                ["last_failure_time"] = breaker.Value.LastFailureTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Never",
                ["next_reset_time"] = breaker.Value.NextResetTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A"
            };
        }
        
        return status;
    }
}

public class CircuitBreaker
{
    public string Name { get; set; } = string.Empty;
    public CircuitBreakerState State { get; private set; } = CircuitBreakerState.Closed;
    public int FailureCount { get; private set; }
    public int FailureThreshold { get; set; }
    public TimeSpan RecoveryTimeout { get; set; }
    public TimeSpan MonitoringWindow { get; set; }
    public DateTime? LastFailureTime { get; private set; }
    public DateTime? NextResetTime { get; private set; }
    
    public void RecordSuccess()
    {
        if (State == CircuitBreakerState.Closed)
        {
            FailureCount = 0;
            LastFailureTime = null;
        }
    }
    
    public void RecordFailure()
    {
        FailureCount++;
        LastFailureTime = DateTime.UtcNow;
        
        if (FailureCount >= FailureThreshold)
        {
            SetOpen();
        }
    }
    
    public void SetOpen()
    {
        State = CircuitBreakerState.Open;
        NextResetTime = DateTime.UtcNow.Add(RecoveryTimeout);
        _logger.LogWarning("Circuit breaker {Name} opened after {FailureCount} failures", Name, FailureCount);
    }
    
    public void SetHalfOpen()
    {
        State = CircuitBreakerState.HalfOpen;
        NextResetTime = null;
    }
    
    public void SetClosed()
    {
        State = CircuitBreakerState.Closed;
        FailureCount = 0;
        LastFailureTime = null;
        NextResetTime = null;
    }
    
    public bool ShouldAttemptReset()
    {
        return State == CircuitBreakerState.Open && 
               NextResetTime.HasValue && 
               DateTime.UtcNow >= NextResetTime.Value;
    }
    
    public object FallbackAction()
    {
        return new Dictionary<string, object> { ["error"] = $"Service {Name} temporarily unavailable" };
    }
}

public enum CircuitBreakerState
{
    Closed,
    Open,
    HalfOpen
}
```

## 🔄 Retry Pattern

### Example: Retry Service
```csharp
// RetryService.cs
public class RetryService
{
    private readonly TuskLang _parser;
    private readonly ILogger<RetryService> _logger;
    private readonly Dictionary<string, RetryPolicy> _retryPolicies;
    
    public RetryService(ILogger<RetryService> logger)
    {
        _parser = new TuskLang();
        _logger = logger;
        _retryPolicies = new Dictionary<string, RetryPolicy>();
        
        LoadRetryPolicies();
    }
    
    private void LoadRetryPolicies()
    {
        var config = _parser.ParseFile("config/retry-policies.tsk");
        var policies = config["retry_policies"] as Dictionary<string, object>;
        
        foreach (var policy in policies ?? new Dictionary<string, object>())
        {
            var policyName = policy.Key;
            var policyData = policy.Value as Dictionary<string, object>;
            
            if (policyData != null)
            {
                _retryPolicies[policyName] = new RetryPolicy
                {
                    Name = policyName,
                    MaxRetries = int.Parse(policyData["max_retries"].ToString()),
                    BaseDelay = TimeSpan.FromMilliseconds(int.Parse(policyData["base_delay_ms"].ToString())),
                    MaxDelay = TimeSpan.FromSeconds(int.Parse(policyData["max_delay_seconds"].ToString())),
                    BackoffMultiplier = double.Parse(policyData["backoff_multiplier"].ToString())
                };
            }
        }
        
        _logger.LogInformation("Loaded {Count} retry policies", _retryPolicies.Count);
    }
    
    public async Task<T> ExecuteWithRetryAsync<T>(string policyName, Func<Task<T>> operation)
    {
        if (!_retryPolicies.ContainsKey(policyName))
        {
            _logger.LogWarning("Retry policy {PolicyName} not found, executing without retry", policyName);
            return await operation();
        }
        
        var policy = _retryPolicies[policyName];
        var delay = policy.BaseDelay;
        
        for (int attempt = 0; attempt <= policy.MaxRetries; attempt++)
        {
            try
            {
                if (attempt > 0)
                {
                    _logger.LogInformation("Retry attempt {Attempt} for policy {PolicyName}", attempt, policyName);
                    await Task.Delay(delay);
                }
                
                var result = await operation();
                
                if (attempt > 0)
                {
                    _logger.LogInformation("Operation succeeded on retry attempt {Attempt} for policy {PolicyName}", attempt, policyName);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Operation failed on attempt {Attempt} for policy {PolicyName}", attempt, policyName);
                
                if (attempt == policy.MaxRetries)
                {
                    _logger.LogError(ex, "All retry attempts failed for policy {PolicyName}", policyName);
                    throw;
                }
                
                // Calculate next delay with exponential backoff
                delay = TimeSpan.FromMilliseconds(Math.Min(
                    delay.TotalMilliseconds * policy.BackoffMultiplier,
                    policy.MaxDelay.TotalMilliseconds
                ));
            }
        }
        
        throw new InvalidOperationException($"Retry policy {policyName} failed unexpectedly");
    }
}

public class RetryPolicy
{
    public string Name { get; set; } = string.Empty;
    public int MaxRetries { get; set; }
    public TimeSpan BaseDelay { get; set; }
    public TimeSpan MaxDelay { get; set; }
    public double BackoffMultiplier { get; set; }
}
```

## 🛠️ Real-World Error Handling Scenarios
- **Database operations**: Handle connection failures and timeouts
- **API calls**: Handle network failures and service unavailability
- **File operations**: Handle file system errors and permissions
- **External services**: Handle third-party service failures

## 🧩 Best Practices
- Use appropriate retry strategies
- Implement circuit breakers for external dependencies
- Log errors with sufficient context
- Provide meaningful error messages
- Monitor error patterns and trends

## 🏁 You're Ready!

You can now:
- Implement comprehensive error handling
- Use circuit breakers for resilience
- Apply retry patterns with exponential backoff
- Build robust, fault-tolerant applications

**Next:** [Logging Patterns](033-logging-patterns-csharp.md)

---

**"We don't bow to any king" - Your resilience mastery, your fault tolerance, your error excellence.**

Handle errors gracefully. Recover confidently. 🛑 