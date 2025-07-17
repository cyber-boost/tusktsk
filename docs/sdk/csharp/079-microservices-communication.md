# Microservices Communication Patterns in C# with TuskLang

## Overview

Microservices communication is a critical aspect of distributed systems architecture. This guide covers various communication patterns, protocols, and best practices for building robust microservices using C# and TuskLang configuration.

## Table of Contents

- [Communication Patterns](#communication-patterns)
- [Synchronous Communication](#synchronous-communication)
- [Asynchronous Communication](#asynchronous-communication)
- [Event-Driven Architecture](#event-driven-architecture)
- [Service Discovery](#service-discovery)
- [Load Balancing](#load-balancing)
- [Circuit Breakers](#circuit-breakers)
- [Message Queues](#message-queues)
- [API Gateway Integration](#api-gateway-integration)
- [Monitoring & Observability](#monitoring--observability)

## Communication Patterns

### Pattern Configuration

```csharp
// Program.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using TuskLang;

var builder = WebApplication.CreateBuilder(args);

// Load TuskLang configuration
var tuskConfig = TuskConfig.Load("microservices.tsk");

builder.Services.AddMicroservicesCommunication(tuskConfig);
builder.Services.AddServiceDiscovery(tuskConfig);
builder.Services.AddMessageQueue(tuskConfig);
builder.Services.AddCircuitBreaker(tuskConfig);

var app = builder.Build();
app.Run();
```

### TuskLang Microservices Configuration

```ini
# microservices.tsk
[microservices]
environment = @env("ENVIRONMENT", "development")
service_name = @env("SERVICE_NAME", "user-service")
version = "1.0.0"

[communication]
default_timeout = @env("DEFAULT_TIMEOUT_MS", "5000")
retry_attempts = @env("RETRY_ATTEMPTS", "3")
retry_delay = @env("RETRY_DELAY_MS", "1000")

[synchronous]
enabled = true
protocol = @env("SYNC_PROTOCOL", "http")
serialization = @env("SERIALIZATION", "json")

[asynchronous]
enabled = true
message_queue = @env("MESSAGE_QUEUE", "rabbitmq")
event_bus = @env("EVENT_BUS", "redis")

[service_discovery]
enabled = true
provider = @env("DISCOVERY_PROVIDER", "consul")
refresh_interval = @env("DISCOVERY_REFRESH_MS", "30000")

[load_balancing]
strategy = @env("LB_STRATEGY", "round-robin")
health_check_interval = @env("HEALTH_CHECK_MS", "10000")

[circuit_breaker]
enabled = true
failure_threshold = @env("CB_FAILURE_THRESHOLD", "5")
timeout = @env("CB_TIMEOUT_MS", "60000")

[monitoring]
metrics_enabled = true
tracing_enabled = true
log_level = @env("LOG_LEVEL", "Information")
```

## Synchronous Communication

### HTTP Client Factory

```csharp
// HttpClientFactory.cs
public class HttpClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<HttpClientFactory> _logger;

    public HttpClientFactory(
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        ILogger<HttpClientFactory> logger)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
    }

    public HttpClient CreateClient(string serviceName)
    {
        var client = _httpClientFactory.CreateClient(serviceName);
        
        // Configure default headers
        var defaultHeaders = _config.GetSection($"services:{serviceName}:headers")
            .Get<Dictionary<string, string>>();
        
        if (defaultHeaders != null)
        {
            foreach (var header in defaultHeaders)
            {
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }

        // Configure timeout
        var timeout = _config.GetValue<int>($"services:{serviceName}:timeout", 
            _config.GetValue<int>("communication:default_timeout", 5000));
        client.Timeout = TimeSpan.FromMilliseconds(timeout);

        return client;
    }
}
```

### Service Client Base

```csharp
// ServiceClientBase.cs
public abstract class ServiceClientBase
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger _logger;
    private readonly CircuitBreaker _circuitBreaker;

    protected ServiceClientBase(
        HttpClient httpClient,
        IConfiguration config,
        ILogger logger,
        CircuitBreaker circuitBreaker)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
        _circuitBreaker = circuitBreaker;
    }

    protected async Task<T> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default)
    {
        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        });
    }

    protected async Task<T> PostAsync<T>(string endpoint, object data, CancellationToken cancellationToken = default)
    {
        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<T>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        });
    }

    protected async Task PutAsync(string endpoint, object data, CancellationToken cancellationToken = default)
    {
        await _circuitBreaker.ExecuteAsync(async () =>
        {
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PutAsync(endpoint, content, cancellationToken);
            response.EnsureSuccessStatusCode();
        });
    }

    protected async Task DeleteAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        await _circuitBreaker.ExecuteAsync(async () =>
        {
            var response = await _httpClient.DeleteAsync(endpoint, cancellationToken);
            response.EnsureSuccessStatusCode();
        });
    }
}
```

### User Service Client

```csharp
// UserServiceClient.cs
public class UserServiceClient : ServiceClientBase
{
    public UserServiceClient(
        HttpClient httpClient,
        IConfiguration config,
        ILogger<UserServiceClient> logger,
        CircuitBreaker circuitBreaker)
        : base(httpClient, config, logger, circuitBreaker)
    {
    }

    public async Task<User> GetUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching user {UserId}", userId);
        return await GetAsync<User>($"/api/users/{userId}", cancellationToken);
    }

    public async Task<List<User>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching all users");
        return await GetAsync<List<User>>("/api/users", cancellationToken);
    }

    public async Task<User> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating user {Email}", request.Email);
        return await PostAsync<User>("/api/users", request, cancellationToken);
    }

    public async Task UpdateUserAsync(int userId, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating user {UserId}", userId);
        await PutAsync($"/api/users/{userId}", request, cancellationToken);
    }

    public async Task DeleteUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting user {UserId}", userId);
        await DeleteAsync($"/api/users/{userId}", cancellationToken);
    }
}
```

## Asynchronous Communication

### Message Queue Interface

```csharp
// IMessageQueue.cs
public interface IMessageQueue
{
    Task PublishAsync<T>(string topic, T message, CancellationToken cancellationToken = default);
    Task SubscribeAsync<T>(string topic, Func<T, Task> handler, CancellationToken cancellationToken = default);
    Task UnsubscribeAsync(string topic, CancellationToken cancellationToken = default);
}

// RabbitMQ Implementation
public class RabbitMQMessageQueue : IMessageQueue, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly IConfiguration _config;
    private readonly ILogger<RabbitMQMessageQueue> _logger;
    private readonly Dictionary<string, IModel> _consumerChannels;

    public RabbitMQMessageQueue(IConfiguration config, ILogger<RabbitMQMessageQueue> logger)
    {
        _config = config;
        _logger = logger;
        _consumerChannels = new Dictionary<string, IModel>();

        var factory = new ConnectionFactory
        {
            HostName = _config.GetValue<string>("message_queue:rabbitmq:host"),
            Port = _config.GetValue<int>("message_queue:rabbitmq:port", 5672),
            UserName = _config.GetValue<string>("message_queue:rabbitmq:username"),
            Password = _config.GetValue<string>("message_queue:rabbitmq:password")
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        
        _logger.LogInformation("Connected to RabbitMQ at {Host}:{Port}", 
            factory.HostName, factory.Port);
    }

    public async Task PublishAsync<T>(string topic, T message, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        _channel.ExchangeDeclare(topic, ExchangeType.Topic, durable: true);
        _channel.BasicPublish(
            exchange: topic,
            routingKey: "",
            basicProperties: null,
            body: body);

        _logger.LogInformation("Published message to topic {Topic}", topic);
    }

    public async Task SubscribeAsync<T>(string topic, Func<T, Task> handler, CancellationToken cancellationToken = default)
    {
        var queueName = $"{topic}_{Guid.NewGuid()}";
        _channel.ExchangeDeclare(topic, ExchangeType.Topic, durable: true);
        _channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(queueName, topic, "");

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = JsonSerializer.Deserialize<T>(Encoding.UTF8.GetString(body));
                
                await handler(message);
                
                _channel.BasicAck(ea.DeliveryTag, false);
                _logger.LogInformation("Processed message from topic {Topic}", topic);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message from topic {Topic}", topic);
                _channel.BasicNack(ea.DeliveryTag, false, true);
            }
        };

        _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
        _consumerChannels[topic] = _channel;
        
        _logger.LogInformation("Subscribed to topic {Topic}", topic);
    }

    public async Task UnsubscribeAsync(string topic, CancellationToken cancellationToken = default)
    {
        if (_consumerChannels.TryGetValue(topic, out var channel))
        {
            channel.Close();
            _consumerChannels.Remove(topic);
            _logger.LogInformation("Unsubscribed from topic {Topic}", topic);
        }
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}
```

## Event-Driven Architecture

### Event Bus

```csharp
// IEventBus.cs
public interface IEventBus
{
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : IEvent;
    Task SubscribeAsync<T>(IEventHandler<T> handler, CancellationToken cancellationToken = default) where T : IEvent;
}

// IEvent.cs
public interface IEvent
{
    Guid Id { get; }
    DateTime OccurredOn { get; }
    string EventType { get; }
}

// IEventHandler.cs
public interface IEventHandler<in T> where T : IEvent
{
    Task HandleAsync(T @event, CancellationToken cancellationToken = default);
}

// EventBus Implementation
public class EventBus : IEventBus
{
    private readonly IMessageQueue _messageQueue;
    private readonly IConfiguration _config;
    private readonly ILogger<EventBus> _logger;
    private readonly Dictionary<Type, List<object>> _handlers;

    public EventBus(IMessageQueue messageQueue, IConfiguration config, ILogger<EventBus> logger)
    {
        _messageQueue = messageQueue;
        _config = config;
        _logger = logger;
        _handlers = new Dictionary<Type, List<object>>();
    }

    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : IEvent
    {
        var topic = $"events.{@event.EventType}";
        await _messageQueue.PublishAsync(topic, @event, cancellationToken);
        
        _logger.LogInformation("Published event {EventType} with ID {EventId}", 
            @event.EventType, @event.Id);
    }

    public async Task SubscribeAsync<T>(IEventHandler<T> handler, CancellationToken cancellationToken = default) where T : IEvent
    {
        var eventType = typeof(T);
        if (!_handlers.ContainsKey(eventType))
        {
            _handlers[eventType] = new List<object>();
        }
        
        _handlers[eventType].Add(handler);

        var topic = $"events.{eventType.Name}";
        await _messageQueue.SubscribeAsync<T>(async (@event) =>
        {
            await handler.HandleAsync(@event, cancellationToken);
        }, cancellationToken);

        _logger.LogInformation("Subscribed to event {EventType}", eventType.Name);
    }
}
```

### Event Examples

```csharp
// UserCreatedEvent.cs
public class UserCreatedEvent : IEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime OccurredOn { get; set; } = DateTime.UtcNow;
    public string EventType => "UserCreated";
    
    public int UserId { get; set; }
    public string Email { get; set; }
    public string Name { get; set; }
}

// UserCreatedEventHandler.cs
public class UserCreatedEventHandler : IEventHandler<UserCreatedEvent>
{
    private readonly ILogger<UserCreatedEventHandler> _logger;
    private readonly IEmailService _emailService;

    public UserCreatedEventHandler(ILogger<UserCreatedEventHandler> logger, IEmailService emailService)
    {
        _logger = logger;
        _emailService = emailService;
    }

    public async Task HandleAsync(UserCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling UserCreated event for user {UserId}", @event.UserId);
        
        // Send welcome email
        await _emailService.SendWelcomeEmailAsync(@event.Email, @event.Name, cancellationToken);
        
        // Update analytics
        await UpdateUserAnalyticsAsync(@event.UserId, cancellationToken);
    }

    private async Task UpdateUserAnalyticsAsync(int userId, CancellationToken cancellationToken)
    {
        // Implementation for updating user analytics
        await Task.Delay(100, cancellationToken); // Simulate work
    }
}
```

## Service Discovery

### Service Discovery Client

```csharp
// IServiceDiscovery.cs
public interface IServiceDiscovery
{
    Task<List<ServiceInstance>> GetServiceInstancesAsync(string serviceName, CancellationToken cancellationToken = default);
    Task RegisterServiceAsync(ServiceRegistration registration, CancellationToken cancellationToken = default);
    Task DeregisterServiceAsync(string serviceId, CancellationToken cancellationToken = default);
}

// ServiceInstance.cs
public class ServiceInstance
{
    public string Id { get; set; }
    public string ServiceName { get; set; }
    public string Host { get; set; }
    public int Port { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
    public bool IsHealthy { get; set; }
}

// ServiceRegistration.cs
public class ServiceRegistration
{
    public string ServiceName { get; set; }
    public string Host { get; set; }
    public int Port { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
    public string HealthCheckEndpoint { get; set; }
}

// Consul Service Discovery
public class ConsulServiceDiscovery : IServiceDiscovery
{
    private readonly IConsulClient _consulClient;
    private readonly IConfiguration _config;
    private readonly ILogger<ConsulServiceDiscovery> _logger;

    public ConsulServiceDiscovery(IConsulClient consulClient, IConfiguration config, ILogger<ConsulServiceDiscovery> logger)
    {
        _consulClient = consulClient;
        _config = config;
        _logger = logger;
    }

    public async Task<List<ServiceInstance>> GetServiceInstancesAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        var response = await _consulClient.Catalog.Service(serviceName, cancellationToken);
        
        var instances = response.Response.Select(s => new ServiceInstance
        {
            Id = s.ServiceID,
            ServiceName = s.ServiceName,
            Host = s.ServiceAddress,
            Port = s.ServicePort,
            Metadata = s.ServiceMeta,
            IsHealthy = s.Checks.All(c => c.Status == HealthStatus.Passing)
        }).ToList();

        _logger.LogInformation("Found {Count} instances for service {ServiceName}", 
            instances.Count, serviceName);

        return instances;
    }

    public async Task RegisterServiceAsync(ServiceRegistration registration, CancellationToken cancellationToken = default)
    {
        var serviceId = $"{registration.ServiceName}-{Guid.NewGuid()}";
        
        var serviceRegistration = new AgentServiceRegistration
        {
            ID = serviceId,
            Name = registration.ServiceName,
            Address = registration.Host,
            Port = registration.Port,
            Meta = registration.Metadata,
            Check = new AgentServiceCheck
            {
                HTTP = $"http://{registration.Host}:{registration.Port}{registration.HealthCheckEndpoint}",
                Interval = TimeSpan.FromSeconds(10),
                Timeout = TimeSpan.FromSeconds(5)
            }
        };

        await _consulClient.Agent.ServiceRegister(serviceRegistration, cancellationToken);
        
        _logger.LogInformation("Registered service {ServiceName} with ID {ServiceId}", 
            registration.ServiceName, serviceId);
    }

    public async Task DeregisterServiceAsync(string serviceId, CancellationToken cancellationToken = default)
    {
        await _consulClient.Agent.ServiceDeregister(serviceId, cancellationToken);
        
        _logger.LogInformation("Deregistered service {ServiceId}", serviceId);
    }
}
```

## Load Balancing

### Load Balancer Strategies

```csharp
// ILoadBalancer.cs
public interface ILoadBalancer
{
    ServiceInstance SelectInstance(List<ServiceInstance> instances);
}

// RoundRobinLoadBalancer.cs
public class RoundRobinLoadBalancer : ILoadBalancer
{
    private int _currentIndex = 0;
    private readonly object _lock = new object();

    public ServiceInstance SelectInstance(List<ServiceInstance> instances)
    {
        if (instances == null || !instances.Any())
        {
            throw new InvalidOperationException("No service instances available");
        }

        lock (_lock)
        {
            var instance = instances[_currentIndex];
            _currentIndex = (_currentIndex + 1) % instances.Count;
            return instance;
        }
    }
}

// LeastConnectionsLoadBalancer.cs
public class LeastConnectionsLoadBalancer : ILoadBalancer
{
    private readonly ConcurrentDictionary<string, int> _connectionCounts;

    public LeastConnectionsLoadBalancer()
    {
        _connectionCounts = new ConcurrentDictionary<string, int>();
    }

    public ServiceInstance SelectInstance(List<ServiceInstance> instances)
    {
        if (instances == null || !instances.Any())
        {
            throw new InvalidOperationException("No service instances available");
        }

        var instance = instances
            .Where(i => i.IsHealthy)
            .OrderBy(i => _connectionCounts.GetOrAdd(i.Id, 0))
            .First();

        _connectionCounts.AddOrUpdate(instance.Id, 1, (key, oldValue) => oldValue + 1);
        
        return instance;
    }

    public void ReleaseConnection(string instanceId)
    {
        _connectionCounts.AddOrUpdate(instanceId, 0, (key, oldValue) => Math.Max(0, oldValue - 1));
    }
}
```

## Circuit Breakers

### Circuit Breaker Implementation

```csharp
// CircuitBreaker.cs
public class CircuitBreaker
{
    private readonly IConfiguration _config;
    private readonly ILogger<CircuitBreaker> _logger;
    private readonly string _serviceName;
    private readonly int _failureThreshold;
    private readonly TimeSpan _timeout;
    
    private CircuitBreakerState _state = CircuitBreakerState.Closed;
    private int _failureCount = 0;
    private DateTime _lastFailureTime;
    private readonly object _lock = new object();

    public CircuitBreaker(string serviceName, IConfiguration config, ILogger<CircuitBreaker> logger)
    {
        _serviceName = serviceName;
        _config = config;
        _logger = logger;
        _failureThreshold = _config.GetValue<int>($"circuit_breaker:{serviceName}:failure_threshold", 5);
        _timeout = TimeSpan.FromMilliseconds(_config.GetValue<int>($"circuit_breaker:{serviceName}:timeout", 60000));
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
    {
        if (_state == CircuitBreakerState.Open)
        {
            if (DateTime.UtcNow - _lastFailureTime >= _timeout)
            {
                _state = CircuitBreakerState.HalfOpen;
                _logger.LogInformation("Circuit breaker for {ServiceName} moved to HALF-OPEN", _serviceName);
            }
            else
            {
                throw new CircuitBreakerOpenException($"Circuit breaker is open for {_serviceName}");
            }
        }

        try
        {
            var result = await action();
            OnSuccess();
            return result;
        }
        catch (Exception ex)
        {
            OnFailure();
            throw;
        }
    }

    private void OnSuccess()
    {
        lock (_lock)
        {
            _state = CircuitBreakerState.Closed;
            _failureCount = 0;
            _logger.LogInformation("Circuit breaker for {ServiceName} moved to CLOSED", _serviceName);
        }
    }

    private void OnFailure()
    {
        lock (_lock)
        {
            _failureCount++;
            _lastFailureTime = DateTime.UtcNow;

            if (_failureCount >= _failureThreshold)
            {
                _state = CircuitBreakerState.Open;
                _logger.LogWarning("Circuit breaker for {ServiceName} moved to OPEN after {FailureCount} failures", 
                    _serviceName, _failureCount);
            }
        }
    }
}

public enum CircuitBreakerState
{
    Closed,
    Open,
    HalfOpen
}

public class CircuitBreakerOpenException : Exception
{
    public CircuitBreakerOpenException(string message) : base(message) { }
}
```

## Message Queues

### Message Queue Configuration

```csharp
// MessageQueueConfiguration.cs
public static class MessageQueueConfiguration
{
    public static IServiceCollection AddMessageQueue(this IServiceCollection services, IConfiguration config)
    {
        var queueType = config.GetValue<string>("asynchronous:message_queue", "rabbitmq");
        
        switch (queueType.ToLower())
        {
            case "rabbitmq":
                services.AddSingleton<IMessageQueue, RabbitMQMessageQueue>();
                break;
            case "redis":
                services.AddSingleton<IMessageQueue, RedisMessageQueue>();
                break;
            case "kafka":
                services.AddSingleton<IMessageQueue, KafkaMessageQueue>();
                break;
            default:
                throw new NotSupportedException($"Message queue type {queueType} is not supported");
        }

        services.AddSingleton<IEventBus, EventBus>();
        
        return services;
    }
}
```

## API Gateway Integration

### Gateway Service Client

```csharp
// GatewayServiceClient.cs
public class GatewayServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<GatewayServiceClient> _logger;
    private readonly CircuitBreaker _circuitBreaker;

    public GatewayServiceClient(
        HttpClient httpClient,
        IConfiguration config,
        ILogger<GatewayServiceClient> logger,
        CircuitBreaker circuitBreaker)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
        _circuitBreaker = circuitBreaker;
    }

    public async Task<T> CallServiceAsync<T>(string serviceName, string endpoint, CancellationToken cancellationToken = default)
    {
        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            var serviceUrl = _config.GetValue<string>($"services:{serviceName}:url");
            var url = $"{serviceUrl}{endpoint}";
            
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        });
    }
}
```

## Monitoring & Observability

### Distributed Tracing

```csharp
// TracingMiddleware.cs
public class TracingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _config;
    private readonly ILogger<TracingMiddleware> _logger;

    public TracingMiddleware(RequestDelegate next, IConfiguration config, ILogger<TracingMiddleware> logger)
    {
        _next = next;
        _config = config;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() 
            ?? Guid.NewGuid().ToString();
        
        context.Response.Headers.Add("X-Correlation-ID", correlationId);

        using var activity = ActivitySource.StartActivity("Microservice.Request");
        activity?.SetTag("correlation.id", correlationId);
        activity?.SetTag("service.name", _config.GetValue<string>("microservices:service_name"));
        activity?.SetTag("http.method", context.Request.Method);
        activity?.SetTag("http.url", context.Request.Path);

        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            activity?.SetTag("http.status_code", context.Response.StatusCode);
            activity?.SetTag("duration_ms", stopwatch.ElapsedMilliseconds);
        }
    }
}
```

### Health Checks

```csharp
// HealthCheckService.cs
public class HealthCheckService : BackgroundService
{
    private readonly IServiceDiscovery _serviceDiscovery;
    private readonly IConfiguration _config;
    private readonly ILogger<HealthCheckService> _logger;
    private readonly HttpClient _httpClient;

    public HealthCheckService(
        IServiceDiscovery serviceDiscovery,
        IConfiguration config,
        ILogger<HealthCheckService> logger,
        HttpClient httpClient)
    {
        _serviceDiscovery = serviceDiscovery;
        _config = config;
        _logger = logger;
        _httpClient = httpClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMilliseconds(
            _config.GetValue<int>("load_balancing:health_check_interval", 10000));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformHealthChecksAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing health checks");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task PerformHealthChecksAsync(CancellationToken cancellationToken)
    {
        var services = _config.GetSection("services").GetChildren();
        
        foreach (var service in services)
        {
            var serviceName = service.Key;
            var instances = await _serviceDiscovery.GetServiceInstancesAsync(serviceName, cancellationToken);
            
            foreach (var instance in instances)
            {
                await CheckInstanceHealthAsync(instance, cancellationToken);
            }
        }
    }

    private async Task CheckInstanceHealthAsync(ServiceInstance instance, CancellationToken cancellationToken)
    {
        try
        {
            var healthUrl = $"http://{instance.Host}:{instance.Port}/health";
            var response = await _httpClient.GetAsync(healthUrl, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Health check failed for instance {InstanceId} of service {ServiceName}", 
                    instance.Id, instance.ServiceName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check error for instance {InstanceId} of service {ServiceName}", 
                instance.Id, instance.ServiceName);
        }
    }
}
```

## Best Practices

### Communication Best Practices

1. **Service Boundaries**: Define clear service boundaries and responsibilities
2. **API Design**: Use consistent API design patterns across services
3. **Error Handling**: Implement proper error handling and retry mechanisms
4. **Monitoring**: Use distributed tracing and metrics for observability
5. **Security**: Implement proper authentication and authorization
6. **Resilience**: Use circuit breakers and fallback mechanisms

### Performance Optimization

1. **Connection Pooling**: Reuse HTTP connections where possible
2. **Caching**: Implement appropriate caching strategies
3. **Compression**: Use compression for large payloads
4. **Async/Await**: Use async/await patterns consistently
5. **Resource Management**: Properly dispose of resources

### Security Considerations

1. **Authentication**: Use strong authentication mechanisms
2. **Authorization**: Implement fine-grained authorization
3. **Encryption**: Encrypt sensitive data in transit and at rest
4. **Input Validation**: Validate all inputs to prevent attacks
5. **Rate Limiting**: Implement rate limiting to prevent abuse

## Conclusion

Microservices communication is a complex topic that requires careful consideration of patterns, protocols, and best practices. By implementing these patterns with C# and TuskLang, you can build robust, scalable, and maintainable microservices architectures.

The combination of synchronous and asynchronous communication patterns, along with proper service discovery, load balancing, and circuit breakers, provides a solid foundation for building distributed systems that can handle the challenges of modern application development. 