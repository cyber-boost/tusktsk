# Reactive Programming in C# with TuskLang

## Overview

Reactive programming is a programming paradigm focused on asynchronous data streams and the propagation of change. This guide covers how to implement reactive patterns using C# and TuskLang configuration for building responsive, resilient, and scalable applications.

## Table of Contents

- [Reactive Programming Concepts](#reactive-programming-concepts)
- [TuskLang Reactive Configuration](#tusklang-reactive-configuration)
- [Observable Streams](#observable-streams)
- [C# Reactive Example](#c-reactive-example)
- [Backpressure Handling](#backpressure-handling)
- [Error Handling](#error-handling)
- [Testing Reactive Code](#testing-reactive-code)
- [Best Practices](#best-practices)

## Reactive Programming Concepts

- **Observable**: Data streams that emit values over time
- **Observer**: Subscribers that react to emitted values
- **Operators**: Functions that transform, filter, and combine streams
- **Backpressure**: Handling when producers are faster than consumers
- **Schedulers**: Control where and when operations execute

## TuskLang Reactive Configuration

```ini
# reactive.tsk
[reactive]
enabled = @env("REACTIVE_ENABLED", "true")
buffer_size = @env("REACTIVE_BUFFER_SIZE", "1000")
timeout = @env("REACTIVE_TIMEOUT_MS", "5000")
retry_attempts = @env("REACTIVE_RETRY_ATTEMPTS", "3")

[streams]
user_events = @env("USER_EVENTS_STREAM", "user-events")
order_events = @env("ORDER_EVENTS_STREAM", "order-events")
system_events = @env("SYSTEM_EVENTS_STREAM", "system-events")

[backpressure]
strategy = @env("BACKPRESSURE_STRATEGY", "buffer")
max_buffer_size = @env("MAX_BUFFER_SIZE", "10000")
drop_strategy = @env("DROP_STRATEGY", "oldest")

[error_handling]
retry_on_error = @env("RETRY_ON_ERROR", "true")
circuit_breaker_enabled = @env("CIRCUIT_BREAKER_ENABLED", "true")
error_threshold = @env("ERROR_THRESHOLD", "5")
```

## Observable Streams

```csharp
public interface IObservableStream<T>
{
    IDisposable Subscribe(IObserver<T> observer);
    Task PublishAsync(T value);
}

public interface IObserver<T>
{
    void OnNext(T value);
    void OnError(Exception error);
    void OnCompleted();
}

public class ObservableStream<T> : IObservableStream<T>, IObservable<T>
{
    private readonly IConfiguration _config;
    private readonly ILogger<ObservableStream<T>> _logger;
    private readonly Subject<T> _subject;
    private readonly int _bufferSize;
    private readonly string _streamName;

    public ObservableStream(IConfiguration config, ILogger<ObservableStream<T>> logger, string streamName)
    {
        _config = config;
        _logger = logger;
        _streamName = streamName;
        _subject = new Subject<T>();
        _bufferSize = int.Parse(_config["reactive:buffer_size"]);
    }

    public IDisposable Subscribe(IObserver<T> observer)
    {
        return _subject.Subscribe(observer);
    }

    public async Task PublishAsync(T value)
    {
        try
        {
            _subject.OnNext(value);
            _logger.LogDebug("Published value to stream {StreamName}", _streamName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing to stream {StreamName}", _streamName);
            _subject.OnError(ex);
        }
    }

    public void Complete()
    {
        _subject.OnCompleted();
        _logger.LogInformation("Completed stream {StreamName}", _streamName);
    }

    public void Error(Exception error)
    {
        _subject.OnError(error);
        _logger.LogError(error, "Error in stream {StreamName}", _streamName);
    }
}
```

## C# Reactive Example

```csharp
using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using TuskLang;

public class ReactiveUserService
{
    private readonly IConfiguration _config;
    private readonly ILogger<ReactiveUserService> _logger;
    private readonly ObservableStream<UserEvent> _userEvents;
    private readonly ObservableStream<UserSummary> _userSummaries;

    public ReactiveUserService(
        IConfiguration config,
        ILogger<ReactiveUserService> logger)
    {
        _config = config;
        _logger = logger;
        _userEvents = new ObservableStream<UserEvent>(config, logger, "user-events");
        _userSummaries = new ObservableStream<UserSummary>(config, logger, "user-summaries");
        
        SetupReactivePipeline();
    }

    private void SetupReactivePipeline()
    {
        if (!bool.Parse(_config["reactive:enabled"]))
            return;

        var bufferSize = int.Parse(_config["reactive:buffer_size"]);
        var timeout = TimeSpan.FromMilliseconds(
            int.Parse(_config["reactive:timeout_ms"]));

        // Create reactive pipeline
        _userEvents
            .Buffer(TimeSpan.FromSeconds(5), bufferSize) // Buffer events
            .Where(events => events.Any()) // Filter empty buffers
            .Select(events => ProcessUserEvents(events)) // Transform events
            .Timeout(timeout) // Add timeout
            .Retry(int.Parse(_config["reactive:retry_attempts"])) // Retry on error
            .Subscribe(
                summary => _userSummaries.PublishAsync(summary),
                error => HandleError(error),
                () => _logger.LogInformation("User events pipeline completed")
            );
    }

    public async Task PublishUserEventAsync(UserEvent userEvent)
    {
        await _userEvents.PublishAsync(userEvent);
    }

    public IDisposable SubscribeToUserSummaries(IObserver<UserSummary> observer)
    {
        return _userSummaries.Subscribe(observer);
    }

    private UserSummary ProcessUserEvents(IList<UserEvent> events)
    {
        var summary = new UserSummary
        {
            Timestamp = DateTime.UtcNow,
            EventCount = events.Count,
            UserIds = events.Select(e => e.UserId).Distinct().ToList()
        };

        // Process different event types
        foreach (var userEvent in events)
        {
            switch (userEvent.Type)
            {
                case "UserCreated":
                    summary.CreatedUsers++;
                    break;
                case "UserUpdated":
                    summary.UpdatedUsers++;
                    break;
                case "UserDeleted":
                    summary.DeletedUsers++;
                    break;
            }
        }

        _logger.LogInformation("Processed {Count} user events", events.Count);
        return summary;
    }

    private void HandleError(Exception error)
    {
        _logger.LogError(error, "Error in reactive pipeline");
        
        if (bool.Parse(_config["error_handling:circuit_breaker_enabled"]))
        {
            // Implement circuit breaker logic
        }
    }
}

public class UserEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; }
    public string UserId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Data { get; set; } = new();
}

public class UserSummary
{
    public DateTime Timestamp { get; set; }
    public int EventCount { get; set; }
    public int CreatedUsers { get; set; }
    public int UpdatedUsers { get; set; }
    public int DeletedUsers { get; set; }
    public List<string> UserIds { get; set; } = new();
}
```

## Backpressure Handling

```csharp
public class BackpressureHandler<T>
{
    private readonly IConfiguration _config;
    private readonly ILogger<BackpressureHandler<T>> _logger;
    private readonly string _strategy;
    private readonly int _maxBufferSize;
    private readonly Queue<T> _buffer;

    public BackpressureHandler(IConfiguration config, ILogger<BackpressureHandler<T>> logger)
    {
        _config = config;
        _logger = logger;
        _strategy = _config["backpressure:strategy"];
        _maxBufferSize = int.Parse(_config["backpressure:max_buffer_size"]);
        _buffer = new Queue<T>();
    }

    public async Task HandleAsync(T value, Func<T, Task> processor)
    {
        switch (_strategy.ToLower())
        {
            case "buffer":
                await HandleBufferStrategyAsync(value, processor);
                break;
            case "drop":
                await HandleDropStrategyAsync(value, processor);
                break;
            case "throttle":
                await HandleThrottleStrategyAsync(value, processor);
                break;
            default:
                await processor(value);
                break;
        }
    }

    private async Task HandleBufferStrategyAsync(T value, Func<T, Task> processor)
    {
        if (_buffer.Count >= _maxBufferSize)
        {
            _logger.LogWarning("Buffer full, dropping oldest value");
            _buffer.Dequeue();
        }

        _buffer.Enqueue(value);

        while (_buffer.Count > 0)
        {
            var item = _buffer.Dequeue();
            await processor(item);
        }
    }

    private async Task HandleDropStrategyAsync(T value, Func<T, Task> processor)
    {
        if (_buffer.Count >= _maxBufferSize)
        {
            _logger.LogWarning("Buffer full, dropping value");
            return;
        }

        _buffer.Enqueue(value);
        await processor(value);
    }

    private async Task HandleThrottleStrategyAsync(T value, Func<T, Task> processor)
    {
        // Implement throttling logic
        await processor(value);
    }
}
```

## Error Handling

```csharp
public class ReactiveErrorHandler
{
    private readonly IConfiguration _config;
    private readonly ILogger<ReactiveErrorHandler> _logger;
    private readonly int _errorThreshold;
    private readonly int _errorCount;
    private readonly CircuitBreaker _circuitBreaker;

    public ReactiveErrorHandler(IConfiguration config, ILogger<ReactiveErrorHandler> logger)
    {
        _config = config;
        _logger = logger;
        _errorThreshold = int.Parse(_config["error_handling:error_threshold"]);
        _circuitBreaker = new CircuitBreaker(_errorThreshold);
    }

    public IObservable<T> HandleErrors<T>(IObservable<T> source, Func<Exception, IObservable<T>> fallback = null)
    {
        return source
            .Catch<T, Exception>(exception =>
            {
                _logger.LogError(exception, "Error in reactive stream");
                
                if (bool.Parse(_config["error_handling:retry_on_error"]))
                {
                    return source.Retry(int.Parse(_config["reactive:retry_attempts"]));
                }
                
                return fallback?.Invoke(exception) ?? Observable.Empty<T>();
            })
            .Do(
                value => _logger.LogDebug("Processed value: {Value}", value),
                error => _logger.LogError(error, "Error processing value"),
                () => _logger.LogInformation("Stream completed")
            );
    }
}

public class CircuitBreaker
{
    private readonly int _threshold;
    private int _failureCount;
    private CircuitBreakerState _state;
    private DateTime _lastFailureTime;

    public CircuitBreaker(int threshold)
    {
        _threshold = threshold;
        _state = CircuitBreakerState.Closed;
    }

    public bool IsOpen => _state == CircuitBreakerState.Open;

    public void OnSuccess()
    {
        _state = CircuitBreakerState.Closed;
        _failureCount = 0;
    }

    public void OnFailure()
    {
        _failureCount++;
        _lastFailureTime = DateTime.UtcNow;

        if (_failureCount >= _threshold)
        {
            _state = CircuitBreakerState.Open;
        }
    }

    public bool ShouldAttemptReset()
    {
        return _state == CircuitBreakerState.Open && 
               DateTime.UtcNow - _lastFailureTime > TimeSpan.FromMinutes(1);
    }
}

public enum CircuitBreakerState
{
    Closed,
    Open,
    HalfOpen
}
```

## Testing Reactive Code

```csharp
public class ReactiveTestHelper
{
    public static async Task<List<T>> CollectValuesAsync<T>(IObservable<T> observable, int count)
    {
        var values = new List<T>();
        var tcs = new TaskCompletionSource<bool>();

        observable
            .Take(count)
            .Subscribe(
                value => values.Add(value),
                error => tcs.SetException(error),
                () => tcs.SetResult(true)
            );

        await tcs.Task;
        return values;
    }

    public static async Task<T> GetFirstValueAsync<T>(IObservable<T> observable)
    {
        var values = await CollectValuesAsync(observable, 1);
        return values.FirstOrDefault();
    }

    public static async Task<Exception> GetErrorAsync<T>(IObservable<T> observable)
    {
        var tcs = new TaskCompletionSource<Exception>();

        observable.Subscribe(
            value => { },
            error => tcs.SetResult(error),
            () => tcs.SetResult(null)
        );

        return await tcs.Task;
    }
}

[TestClass]
public class ReactiveUserServiceTests
{
    private IConfiguration _config;
    private ILogger<ReactiveUserService> _logger;
    private ReactiveUserService _service;

    [TestInitialize]
    public void Setup()
    {
        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["reactive:enabled"] = "true",
                ["reactive:buffer_size"] = "10",
                ["reactive:timeout_ms"] = "1000"
            })
            .Build();

        _logger = Mock.Of<ILogger<ReactiveUserService>>();
        _service = new ReactiveUserService(_config, _logger);
    }

    [TestMethod]
    public async Task PublishUserEvent_ShouldProcessEvent()
    {
        // Arrange
        var userEvent = new UserEvent
        {
            Type = "UserCreated",
            UserId = "user-1"
        };

        var summaries = new List<UserSummary>();
        _service.SubscribeToUserSummaries(new TestObserver<UserSummary>(summaries));

        // Act
        await _service.PublishUserEventAsync(userEvent);
        await Task.Delay(1000); // Wait for processing

        // Assert
        Assert.AreEqual(1, summaries.Count);
        Assert.AreEqual(1, summaries[0].CreatedUsers);
    }
}

public class TestObserver<T> : IObserver<T>
{
    private readonly List<T> _values;

    public TestObserver(List<T> values)
    {
        _values = values;
    }

    public void OnNext(T value) => _values.Add(value);
    public void OnError(Exception error) { }
    public void OnCompleted() { }
}
```

## Best Practices

1. **Use appropriate backpressure strategies**
2. **Handle errors gracefully with retry and circuit breaker patterns**
3. **Test reactive code thoroughly**
4. **Monitor stream performance and memory usage**
5. **Use schedulers to control execution context**
6. **Implement proper cleanup and disposal**
7. **Document stream contracts and error scenarios**

## Conclusion

Reactive programming with C# and TuskLang enables building responsive, resilient, and scalable applications. By leveraging TuskLang for configuration and reactive patterns, you can create systems that handle asynchronous data streams efficiently. 