# Error Handling Patterns in C# TuskLang

## Overview

Effective error handling patterns are crucial for building robust and resilient applications. This guide covers exception patterns, error recovery strategies, and error handling best practices for C# TuskLang applications.

## 🚨 Exception Patterns

### Result Pattern

```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? ErrorMessage { get; }
    public Exception? Exception { get; }
    public List<ValidationError>? ValidationErrors { get; }
    
    private Result(bool isSuccess, T? value = default, string? errorMessage = null, Exception? exception = null, List<ValidationError>? validationErrors = null)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorMessage = errorMessage;
        Exception = exception;
        ValidationErrors = validationErrors;
    }
    
    public static Result<T> Success(T value)
    {
        return new Result<T>(true, value);
    }
    
    public static Result<T> Failure(string errorMessage, Exception? exception = null)
    {
        return new Result<T>(false, errorMessage: errorMessage, exception: exception);
    }
    
    public static Result<T> ValidationFailure(List<ValidationError> validationErrors)
    {
        return new Result<T>(false, validationErrors: validationErrors);
    }
    
    public Result<TNew> Map<TNew>(Func<T, TNew> mapper)
    {
        if (!IsSuccess)
        {
            return Result<TNew>.Failure(ErrorMessage!, Exception);
        }
        
        try
        {
            var newValue = mapper(Value!);
            return Result<TNew>.Success(newValue);
        }
        catch (Exception ex)
        {
            return Result<TNew>.Failure("Mapping failed", ex);
        }
    }
    
    public async Task<Result<TNew>> MapAsync<TNew>(Func<T, Task<TNew>> mapper)
    {
        if (!IsSuccess)
        {
            return Result<TNew>.Failure(ErrorMessage!, Exception);
        }
        
        try
        {
            var newValue = await mapper(Value!);
            return Result<TNew>.Success(newValue);
        }
        catch (Exception ex)
        {
            return Result<TNew>.Failure("Mapping failed", ex);
        }
    }
    
    public Result<T> OnSuccess(Action<T> action)
    {
        if (IsSuccess)
        {
            action(Value!);
        }
        
        return this;
    }
    
    public Result<T> OnFailure(Action<string, Exception?> action)
    {
        if (!IsSuccess)
        {
            action(ErrorMessage!, Exception);
        }
        
        return this;
    }
    
    public T GetValueOrThrow()
    {
        if (!IsSuccess)
        {
            throw Exception ?? new InvalidOperationException(ErrorMessage ?? "Operation failed");
        }
        
        return Value!;
    }
    
    public T GetValueOrDefault(T defaultValue)
    {
        return IsSuccess ? Value! : defaultValue;
    }
}

public class ValidationError
{
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

// Example usage with Result pattern
public class UserServiceWithResult
{
    private readonly IUserRepository _userRepository;
    private readonly IValidator _validator;
    private readonly ILogger<UserServiceWithResult> _logger;
    
    public UserServiceWithResult(
        IUserRepository userRepository,
        IValidator validator,
        ILogger<UserServiceWithResult> logger)
    {
        _userRepository = userRepository;
        _validator = validator;
        _logger = logger;
    }
    
    public async Task<Result<UserDto>> CreateUserAsync(CreateUserRequest request)
    {
        try
        {
            // Validate request
            var validationResult = await _validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var validationErrors = validationResult.Errors.Select(e => new ValidationError
                {
                    Field = e.PropertyName,
                    Message = e.ErrorMessage,
                    Code = e.ErrorCode
                }).ToList();
                
                return Result<UserDto>.ValidationFailure(validationErrors);
            }
            
            // Check if user already exists
            var existingUser = await _userRepository.GetByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return Result<UserDto>.Failure($"User with email {request.Email} already exists");
            }
            
            // Create user
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                CreatedAt = DateTime.UtcNow,
                Status = UserStatus.Active
            };
            
            await _userRepository.CreateAsync(user);
            
            var userDto = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Status = user.Status
            };
            
            _logger.LogInformation("User created successfully: {UserId}", user.Id);
            
            return Result<UserDto>.Success(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user: {Email}", request.Email);
            return Result<UserDto>.Failure("Failed to create user", ex);
        }
    }
    
    public async Task<Result<UserDto?>> GetUserAsync(Guid userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            
            if (user == null)
            {
                return Result<UserDto?>.Success(null);
            }
            
            var userDto = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Status = user.Status
            };
            
            return Result<UserDto?>.Success(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user: {UserId}", userId);
            return Result<UserDto?>.Failure("Failed to get user", ex);
        }
    }
}
```

### Either Pattern

```csharp
public class Either<TLeft, TRight>
{
    private readonly TLeft? _left;
    private readonly TRight? _right;
    private readonly bool _isRight;
    
    private Either(TLeft? left, TRight? right, bool isRight)
    {
        _left = left;
        _right = right;
        _isRight = isRight;
    }
    
    public static Either<TLeft, TRight> Left(TLeft left)
    {
        return new Either<TLeft, TRight>(left, default, false);
    }
    
    public static Either<TLeft, TRight> Right(TRight right)
    {
        return new Either<TLeft, TRight>(default, right, true);
    }
    
    public bool IsRight => _isRight;
    public bool IsLeft => !_isRight;
    
    public TLeft LeftValue => _isRight ? throw new InvalidOperationException("Cannot access left value of right") : _left!;
    public TRight RightValue => _isRight ? _right! : throw new InvalidOperationException("Cannot access right value of left");
    
    public Either<TLeft, TNewRight> Map<TNewRight>(Func<TRight, TNewRight> mapper)
    {
        return _isRight ? Either<TLeft, TNewRight>.Right(mapper(_right!)) : Either<TLeft, TNewRight>.Left(_left!);
    }
    
    public async Task<Either<TLeft, TNewRight>> MapAsync<TNewRight>(Func<TRight, Task<TNewRight>> mapper)
    {
        if (_isRight)
        {
            var newRight = await mapper(_right!);
            return Either<TLeft, TNewRight>.Right(newRight);
        }
        
        return Either<TLeft, TNewRight>.Left(_left!);
    }
    
    public Either<TNewLeft, TRight> MapLeft<TNewLeft>(Func<TLeft, TNewLeft> mapper)
    {
        return _isRight ? Either<TNewLeft, TRight>.Right(_right!) : Either<TNewLeft, TRight>.Left(mapper(_left!));
    }
    
    public T Match<T>(Func<TLeft, T> leftFunc, Func<TRight, T> rightFunc)
    {
        return _isRight ? rightFunc(_right!) : leftFunc(_left!);
    }
    
    public async Task<T> MatchAsync<T>(Func<TLeft, Task<T>> leftFunc, Func<TRight, Task<T>> rightFunc)
    {
        return _isRight ? await rightFunc(_right!) : await leftFunc(_left!);
    }
    
    public void Match(Action<TLeft> leftAction, Action<TRight> rightAction)
    {
        if (_isRight)
        {
            rightAction(_right!);
        }
        else
        {
            leftAction(_left!);
        }
    }
    
    public Either<TLeft, TRight> OnRight(Action<TRight> action)
    {
        if (_isRight)
        {
            action(_right!);
        }
        
        return this;
    }
    
    public Either<TLeft, TRight> OnLeft(Action<TLeft> action)
    {
        if (!_isRight)
        {
            action(_left!);
        }
        
        return this;
    }
}

// Example usage with Either pattern
public class UserServiceWithEither
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserServiceWithEither> _logger;
    
    public UserServiceWithEither(IUserRepository userRepository, ILogger<UserServiceWithEither> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }
    
    public async Task<Either<string, UserDto>> CreateUserAsync(CreateUserRequest request)
    {
        try
        {
            // Check if user already exists
            var existingUser = await _userRepository.GetByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return Either<string, UserDto>.Left($"User with email {request.Email} already exists");
            }
            
            // Create user
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                CreatedAt = DateTime.UtcNow,
                Status = UserStatus.Active
            };
            
            await _userRepository.CreateAsync(user);
            
            var userDto = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Status = user.Status
            };
            
            _logger.LogInformation("User created successfully: {UserId}", user.Id);
            
            return Either<string, UserDto>.Right(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user: {Email}", request.Email);
            return Either<string, UserDto>.Left("Failed to create user");
        }
    }
    
    public async Task<Either<string, UserDto?>> GetUserAsync(Guid userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            
            if (user == null)
            {
                return Either<string, UserDto?>.Right(null);
            }
            
            var userDto = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Status = user.Status
            };
            
            return Either<string, UserDto?>.Right(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user: {UserId}", userId);
            return Either<string, UserDto?>.Left("Failed to get user");
        }
    }
}
```

## 🔄 Error Recovery Strategies

### Retry Pattern

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
        RetryOptions options,
        CancellationToken cancellationToken = default)
    {
        var attempt = 0;
        var lastException = (Exception?)null;
        
        while (attempt < options.MaxRetries)
        {
            try
            {
                attempt++;
                
                _logger.LogDebug("Executing operation (attempt {Attempt}/{MaxRetries})", 
                    attempt, options.MaxRetries);
                
                var result = await operation();
                
                if (attempt > 1)
                {
                    _logger.LogInformation("Operation succeeded on attempt {Attempt}", attempt);
                }
                
                return result;
            }
            catch (Exception ex) when (ShouldRetry(ex, options) && attempt < options.MaxRetries)
            {
                lastException = ex;
                
                var delay = CalculateDelay(attempt, options);
                
                _logger.LogWarning(ex, "Operation failed on attempt {Attempt}/{MaxRetries}. Retrying in {Delay}ms", 
                    attempt, options.MaxRetries, delay.TotalMilliseconds);
                
                await Task.Delay(delay, cancellationToken);
            }
        }
        
        _logger.LogError(lastException, "Operation failed after {MaxRetries} attempts", options.MaxRetries);
        throw lastException!;
    }
    
    public async Task ExecuteWithRetryAsync(
        Func<Task> operation,
        RetryOptions options,
        CancellationToken cancellationToken = default)
    {
        await ExecuteWithRetryAsync(async () =>
        {
            await operation();
            return true;
        }, options, cancellationToken);
    }
    
    private bool ShouldRetry(Exception exception, RetryOptions options)
    {
        if (options.RetryableExceptions.Contains(exception.GetType()))
        {
            return true;
        }
        
        if (exception is HttpRequestException httpEx)
        {
            return options.RetryableHttpStatusCodes.Contains(httpEx.StatusCode);
        }
        
        return false;
    }
    
    private TimeSpan CalculateDelay(int attempt, RetryOptions options)
    {
        return options.BackoffStrategy switch
        {
            BackoffStrategy.Fixed => options.BaseDelay,
            BackoffStrategy.Exponential => TimeSpan.FromMilliseconds(
                options.BaseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1)),
            BackoffStrategy.Linear => TimeSpan.FromMilliseconds(
                options.BaseDelay.TotalMilliseconds * attempt),
            _ => options.BaseDelay
        };
    }
}

public class RetryOptions
{
    public int MaxRetries { get; set; } = 3;
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromSeconds(1);
    public BackoffStrategy BackoffStrategy { get; set; } = BackoffStrategy.Exponential;
    public List<Type> RetryableExceptions { get; set; } = new()
    {
        typeof(TimeoutException),
        typeof(HttpRequestException),
        typeof(DatabaseException)
    };
    public List<HttpStatusCode> RetryableHttpStatusCodes { get; set; } = new()
    {
        HttpStatusCode.RequestTimeout,
        HttpStatusCode.InternalServerError,
        HttpStatusCode.BadGateway,
        HttpStatusCode.ServiceUnavailable,
        HttpStatusCode.GatewayTimeout
    };
}

public enum BackoffStrategy
{
    Fixed,
    Exponential,
    Linear
}

// Example usage with retry pattern
public class UserServiceWithRetry
{
    private readonly IUserRepository _userRepository;
    private readonly RetryPolicy _retryPolicy;
    private readonly ILogger<UserServiceWithRetry> _logger;
    
    public UserServiceWithRetry(
        IUserRepository userRepository,
        RetryPolicy retryPolicy,
        ILogger<UserServiceWithRetry> logger)
    {
        _userRepository = userRepository;
        _retryPolicy = retryPolicy;
        _logger = logger;
    }
    
    public async Task<UserDto> CreateUserAsync(CreateUserRequest request)
    {
        var retryOptions = new RetryOptions
        {
            MaxRetries = 3,
            BaseDelay = TimeSpan.FromSeconds(1),
            BackoffStrategy = BackoffStrategy.Exponential
        };
        
        return await _retryPolicy.ExecuteWithRetryAsync(async () =>
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                CreatedAt = DateTime.UtcNow,
                Status = UserStatus.Active
            };
            
            await _userRepository.CreateAsync(user);
            
            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Status = user.Status
            };
        }, retryOptions);
    }
}
```

### Circuit Breaker Pattern

```csharp
public class CircuitBreaker
{
    private readonly ILogger<CircuitBreaker> _logger;
    private readonly TSKConfig _config;
    private readonly string _name;
    private readonly object _lock = new();
    
    private CircuitBreakerState _state = CircuitBreakerState.Closed;
    private int _failureCount = 0;
    private DateTime _lastFailureTime = DateTime.MinValue;
    private DateTime _lastStateChangeTime = DateTime.UtcNow;
    
    public CircuitBreaker(ILogger<CircuitBreaker> logger, TSKConfig config, string name)
    {
        _logger = logger;
        _config = config;
        _name = name;
    }
    
    public async Task<T> ExecuteAsync<T>(
        Func<Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        if (!await CanExecuteAsync())
        {
            throw new CircuitBreakerOpenException(_name);
        }
        
        try
        {
            var result = await operation();
            await OnSuccessAsync();
            return result;
        }
        catch (Exception ex)
        {
            await OnFailureAsync(ex);
            throw;
        }
    }
    
    public async Task ExecuteAsync(
        Func<Task> operation,
        CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(async () =>
        {
            await operation();
            return true;
        }, cancellationToken);
    }
    
    private async Task<bool> CanExecuteAsync()
    {
        lock (_lock)
        {
            switch (_state)
            {
                case CircuitBreakerState.Closed:
                    return true;
                
                case CircuitBreakerState.Open:
                    var timeout = TimeSpan.FromSeconds(_config.Get<int>($"circuit_breaker.{_name}.timeout_seconds", 60));
                    if (DateTime.UtcNow - _lastFailureTime >= timeout)
                    {
                        _state = CircuitBreakerState.HalfOpen;
                        _lastStateChangeTime = DateTime.UtcNow;
                        _logger.LogInformation("Circuit breaker {Name} moved to half-open state", _name);
                    }
                    return false;
                
                case CircuitBreakerState.HalfOpen:
                    return true;
                
                default:
                    return false;
            }
        }
    }
    
    private async Task OnSuccessAsync()
    {
        lock (_lock)
        {
            if (_state == CircuitBreakerState.HalfOpen)
            {
                _state = CircuitBreakerState.Closed;
                _failureCount = 0;
                _lastStateChangeTime = DateTime.UtcNow;
                _logger.LogInformation("Circuit breaker {Name} moved to closed state", _name);
            }
        }
    }
    
    private async Task OnFailureAsync(Exception exception)
    {
        lock (_lock)
        {
            _failureCount++;
            _lastFailureTime = DateTime.UtcNow;
            
            var failureThreshold = _config.Get<int>($"circuit_breaker.{_name}.failure_threshold", 5);
            
            if (_state == CircuitBreakerState.Closed && _failureCount >= failureThreshold)
            {
                _state = CircuitBreakerState.Open;
                _lastStateChangeTime = DateTime.UtcNow;
                _logger.LogWarning("Circuit breaker {Name} moved to open state after {FailureCount} failures", 
                    _name, _failureCount);
            }
            else if (_state == CircuitBreakerState.HalfOpen)
            {
                _state = CircuitBreakerState.Open;
                _lastStateChangeTime = DateTime.UtcNow;
                _logger.LogWarning("Circuit breaker {Name} moved back to open state", _name);
            }
        }
    }
    
    public CircuitBreakerStatus GetStatus()
    {
        lock (_lock)
        {
            return new CircuitBreakerStatus
            {
                Name = _name,
                State = _state,
                FailureCount = _failureCount,
                LastFailureTime = _lastFailureTime,
                LastStateChangeTime = _lastStateChangeTime
            };
        }
    }
    
    public void Reset()
    {
        lock (_lock)
        {
            _state = CircuitBreakerState.Closed;
            _failureCount = 0;
            _lastStateChangeTime = DateTime.UtcNow;
            _logger.LogInformation("Circuit breaker {Name} manually reset", _name);
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
    public string Name { get; set; } = string.Empty;
    public CircuitBreakerState State { get; set; }
    public int FailureCount { get; set; }
    public DateTime LastFailureTime { get; set; }
    public DateTime LastStateChangeTime { get; set; }
}

public class CircuitBreakerOpenException : Exception
{
    public string CircuitBreakerName { get; }
    
    public CircuitBreakerOpenException(string circuitBreakerName)
        : base($"Circuit breaker '{circuitBreakerName}' is open")
    {
        CircuitBreakerName = circuitBreakerName;
    }
}

// Example usage with circuit breaker pattern
public class UserServiceWithCircuitBreaker
{
    private readonly IUserRepository _userRepository;
    private readonly CircuitBreaker _circuitBreaker;
    private readonly ILogger<UserServiceWithCircuitBreaker> _logger;
    
    public UserServiceWithCircuitBreaker(
        IUserRepository userRepository,
        ILogger<UserServiceWithCircuitBreaker> logger,
        TSKConfig config)
    {
        _userRepository = userRepository;
        _logger = logger;
        _circuitBreaker = new CircuitBreaker(logger, config, "user-service");
    }
    
    public async Task<UserDto> CreateUserAsync(CreateUserRequest request)
    {
        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                CreatedAt = DateTime.UtcNow,
                Status = UserStatus.Active
            };
            
            await _userRepository.CreateAsync(user);
            
            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Status = user.Status
            };
        });
    }
}
```

## 📝 Summary

This guide covered comprehensive error handling patterns for C# TuskLang applications:

- **Result Pattern**: Functional approach to error handling with success/failure states
- **Either Pattern**: Functional programming pattern for handling two possible outcomes
- **Retry Pattern**: Automatic retry logic with configurable backoff strategies
- **Circuit Breaker Pattern**: Fault tolerance pattern for preventing cascading failures

These error handling patterns ensure your C# TuskLang applications are robust, resilient, and handle errors gracefully. 