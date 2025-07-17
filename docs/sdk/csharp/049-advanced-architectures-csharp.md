# Advanced Architectures in C# TuskLang

## Overview

Advanced architectural patterns enable scalable, maintainable, and robust applications. This guide covers microservices, event-driven architecture, CQRS, and other advanced patterns for C# TuskLang applications.

## 🏗️ Microservices Architecture

### Microservice Base

```csharp
public abstract class MicroserviceBase
{
    protected readonly ILogger _logger;
    protected readonly TSKConfig _config;
    protected readonly IServiceProvider _serviceProvider;
    protected readonly IHealthCheckService _healthCheckService;
    
    protected MicroserviceBase(
        ILogger logger,
        TSKConfig config,
        IServiceProvider serviceProvider,
        IHealthCheckService healthCheckService)
    {
        _logger = logger;
        _config = config;
        _serviceProvider = serviceProvider;
        _healthCheckService = healthCheckService;
    }
    
    public abstract string ServiceName { get; }
    public abstract string ServiceVersion { get; }
    
    public virtual async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting {ServiceName} v{ServiceVersion}", ServiceName, ServiceVersion);
        
        try
        {
            await InitializeAsync(cancellationToken);
            await StartHealthChecksAsync(cancellationToken);
            await OnStartedAsync(cancellationToken);
            
            _logger.LogInformation("{ServiceName} started successfully", ServiceName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start {ServiceName}", ServiceName);
            throw;
        }
    }
    
    public virtual async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping {ServiceName}", ServiceName);
        
        try
        {
            await OnStoppingAsync(cancellationToken);
            await StopHealthChecksAsync(cancellationToken);
            await CleanupAsync(cancellationToken);
            
            _logger.LogInformation("{ServiceName} stopped successfully", ServiceName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop {ServiceName}", ServiceName);
            throw;
        }
    }
    
    protected virtual async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }
    
    protected virtual async Task StartHealthChecksAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }
    
    protected virtual async Task StopHealthChecksAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }
    
    protected virtual async Task OnStartedAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }
    
    protected virtual async Task OnStoppingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }
    
    protected virtual async Task CleanupAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }
    
    public virtual async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var healthContext = new HealthCheckContext();
            return await _healthCheckService.CheckHealthAsync(healthContext, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed for {ServiceName}", ServiceName);
            return HealthCheckResult.Unhealthy("Health check failed", ex);
        }
    }
}

public class UserService : MicroserviceBase
{
    private readonly IUserRepository _userRepository;
    private readonly IEventBus _eventBus;
    private readonly Timer _cleanupTimer;
    
    public override string ServiceName => "UserService";
    public override string ServiceVersion => "1.0.0";
    
    public UserService(
        ILogger<UserService> logger,
        TSKConfig config,
        IServiceProvider serviceProvider,
        IHealthCheckService healthCheckService,
        IUserRepository userRepository,
        IEventBus eventBus)
        : base(logger, config, serviceProvider, healthCheckService)
    {
        _userRepository = userRepository;
        _eventBus = eventBus;
        
        var cleanupInterval = TimeSpan.FromHours(_config.Get<int>("user_service.cleanup_interval_hours", 24));
        _cleanupTimer = new Timer(CleanupExpiredUsers, null, cleanupInterval, cleanupInterval);
    }
    
    public async Task<UserDto> CreateUserAsync(CreateUserRequest request)
    {
        try
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
            
            // Publish user created event
            var userCreatedEvent = new UserCreatedEvent
            {
                UserId = user.Id,
                Email = user.Email,
                Timestamp = DateTime.UtcNow
            };
            
            await _eventBus.PublishAsync(userCreatedEvent);
            
            _logger.LogInformation("User created: {UserId}", user.Id);
            
            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Status = user.Status
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user: {Email}", request.Email);
            throw;
        }
    }
    
    public async Task<UserDto?> GetUserAsync(Guid userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            
            if (user == null)
            {
                return null;
            }
            
            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Status = user.Status
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user: {UserId}", userId);
            throw;
        }
    }
    
    public async Task UpdateUserAsync(Guid userId, UpdateUserRequest request)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            
            if (user == null)
            {
                throw new NotFoundException($"User {userId} not found");
            }
            
            user.FirstName = request.FirstName ?? user.FirstName;
            user.LastName = request.LastName ?? user.LastName;
            user.UpdatedAt = DateTime.UtcNow;
            
            await _userRepository.UpdateAsync(user);
            
            // Publish user updated event
            var userUpdatedEvent = new UserUpdatedEvent
            {
                UserId = user.Id,
                Timestamp = DateTime.UtcNow
            };
            
            await _eventBus.PublishAsync(userUpdatedEvent);
            
            _logger.LogInformation("User updated: {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user: {UserId}", userId);
            throw;
        }
    }
    
    private async void CleanupExpiredUsers(object? state)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-_config.Get<int>("user_service.expired_user_days", 365));
            var expiredUsers = await _userRepository.GetExpiredUsersAsync(cutoffDate);
            
            foreach (var user in expiredUsers)
            {
                user.Status = UserStatus.Inactive;
                user.UpdatedAt = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);
                
                // Publish user deactivated event
                var userDeactivatedEvent = new UserDeactivatedEvent
                {
                    UserId = user.Id,
                    Reason = "Expired",
                    Timestamp = DateTime.UtcNow
                };
                
                await _eventBus.PublishAsync(userDeactivatedEvent);
            }
            
            if (expiredUsers.Any())
            {
                _logger.LogInformation("Cleaned up {Count} expired users", expiredUsers.Count());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup expired users");
        }
    }
    
    protected override async Task CleanupAsync(CancellationToken cancellationToken)
    {
        _cleanupTimer?.Dispose();
        await base.CleanupAsync(cancellationToken);
    }
}

public class CreateUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

public class UpdateUserRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}

public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public UserStatus Status { get; set; }
}

public enum UserStatus
{
    Active,
    Inactive,
    Suspended
}
```

### Service Discovery

```csharp
public interface IServiceDiscovery
{
    Task<List<ServiceInstance>> GetServiceInstancesAsync(string serviceName);
    Task<ServiceInstance?> GetServiceInstanceAsync(string serviceName);
    Task RegisterServiceAsync(ServiceRegistration registration);
    Task DeregisterServiceAsync(string serviceId);
}

public class ServiceDiscovery : IServiceDiscovery
{
    private readonly ILogger<ServiceDiscovery> _logger;
    private readonly TSKConfig _config;
    private readonly IDbConnection _connection;
    private readonly Timer _heartbeatTimer;
    
    public ServiceDiscovery(ILogger<ServiceDiscovery> logger, TSKConfig config, IDbConnection connection)
    {
        _logger = logger;
        _config = config;
        _connection = connection;
        
        var heartbeatInterval = TimeSpan.FromSeconds(_config.Get<int>("service_discovery.heartbeat_interval_seconds", 30));
        _heartbeatTimer = new Timer(SendHeartbeat, null, heartbeatInterval, heartbeatInterval);
    }
    
    public async Task<List<ServiceInstance>> GetServiceInstancesAsync(string serviceName)
    {
        try
        {
            var query = @"
                SELECT service_id, service_name, host, port, health_url, last_heartbeat, status
                FROM service_registry
                WHERE service_name = @ServiceName AND status = 'healthy'
                ORDER BY last_heartbeat DESC";
            
            var parameters = new { ServiceName = serviceName };
            var instances = await _connection.QueryAsync<ServiceInstance>(query, parameters);
            
            return instances.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get service instances for {ServiceName}", serviceName);
            return new List<ServiceInstance>();
        }
    }
    
    public async Task<ServiceInstance?> GetServiceInstanceAsync(string serviceName)
    {
        var instances = await GetServiceInstancesAsync(serviceName);
        return instances.FirstOrDefault();
    }
    
    public async Task RegisterServiceAsync(ServiceRegistration registration)
    {
        try
        {
            var query = @"
                INSERT INTO service_registry (service_id, service_name, host, port, health_url, version, metadata, last_heartbeat, status)
                VALUES (@ServiceId, @ServiceName, @Host, @Port, @HealthUrl, @Version, @Metadata, @LastHeartbeat, @Status)
                ON DUPLICATE KEY UPDATE
                    host = VALUES(host),
                    port = VALUES(port),
                    health_url = VALUES(health_url),
                    version = VALUES(version),
                    metadata = VALUES(metadata),
                    last_heartbeat = VALUES(last_heartbeat),
                    status = VALUES(status)";
            
            var parameters = new
            {
                registration.ServiceId,
                registration.ServiceName,
                registration.Host,
                registration.Port,
                registration.HealthUrl,
                registration.Version,
                Metadata = JsonSerializer.Serialize(registration.Metadata),
                LastHeartbeat = DateTime.UtcNow,
                Status = "healthy"
            };
            
            await _connection.ExecuteAsync(query, parameters);
            
            _logger.LogInformation("Service registered: {ServiceName} at {Host}:{Port}", 
                registration.ServiceName, registration.Host, registration.Port);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register service {ServiceName}", registration.ServiceName);
            throw;
        }
    }
    
    public async Task DeregisterServiceAsync(string serviceId)
    {
        try
        {
            var query = "DELETE FROM service_registry WHERE service_id = @ServiceId";
            var parameters = new { ServiceId = serviceId };
            
            await _connection.ExecuteAsync(query, parameters);
            
            _logger.LogInformation("Service deregistered: {ServiceId}", serviceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deregister service {ServiceId}", serviceId);
            throw;
        }
    }
    
    private async void SendHeartbeat(object? state)
    {
        try
        {
            var serviceId = _config.Get<string>("app.service_id");
            if (string.IsNullOrEmpty(serviceId))
            {
                return;
            }
            
            var query = "UPDATE service_registry SET last_heartbeat = @LastHeartbeat WHERE service_id = @ServiceId";
            var parameters = new
            {
                ServiceId = serviceId,
                LastHeartbeat = DateTime.UtcNow
            };
            
            await _connection.ExecuteAsync(query, parameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send heartbeat");
        }
    }
    
    public void Dispose()
    {
        _heartbeatTimer?.Dispose();
    }
}

public class ServiceRegistration
{
    public string ServiceId { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string HealthUrl { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class ServiceInstance
{
    public string ServiceId { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string HealthUrl { get; set; } = string.Empty;
    public DateTime LastHeartbeat { get; set; }
    public string Status { get; set; } = string.Empty;
}
```

## 📡 Event-Driven Architecture

### Event Bus

```csharp
public interface IEventBus
{
    Task PublishAsync<T>(T @event) where T : class;
    Task SubscribeAsync<T>(IEventHandler<T> handler) where T : class;
    Task UnsubscribeAsync<T>(IEventHandler<T> handler) where T : class;
}

public class EventBus : IEventBus
{
    private readonly ILogger<EventBus> _logger;
    private readonly TSKConfig _config;
    private readonly IDbConnection _connection;
    private readonly Dictionary<Type, List<object>> _handlers;
    private readonly SemaphoreSlim _handlersLock;
    
    public EventBus(ILogger<EventBus> logger, TSKConfig config, IDbConnection connection)
    {
        _logger = logger;
        _config = config;
        _connection = connection;
        _handlers = new Dictionary<Type, List<object>>();
        _handlersLock = new SemaphoreSlim(1, 1);
    }
    
    public async Task PublishAsync<T>(T @event) where T : class
    {
        try
        {
            // Store event in database for persistence
            await StoreEventAsync(@event);
            
            // Notify handlers
            await NotifyHandlersAsync(@event);
            
            _logger.LogDebug("Event published: {EventType}", typeof(T).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event {EventType}", typeof(T).Name);
            throw;
        }
    }
    
    public async Task SubscribeAsync<T>(IEventHandler<T> handler) where T : class
    {
        await _handlersLock.WaitAsync();
        
        try
        {
            var eventType = typeof(T);
            
            if (!_handlers.ContainsKey(eventType))
            {
                _handlers[eventType] = new List<object>();
            }
            
            _handlers[eventType].Add(handler);
            
            _logger.LogInformation("Handler registered for event: {EventType}", eventType.Name);
        }
        finally
        {
            _handlersLock.Release();
        }
    }
    
    public async Task UnsubscribeAsync<T>(IEventHandler<T> handler) where T : class
    {
        await _handlersLock.WaitAsync();
        
        try
        {
            var eventType = typeof(T);
            
            if (_handlers.ContainsKey(eventType))
            {
                _handlers[eventType].Remove(handler);
                
                if (!_handlers[eventType].Any())
                {
                    _handlers.Remove(eventType);
                }
            }
            
            _logger.LogInformation("Handler unregistered for event: {EventType}", eventType.Name);
        }
        finally
        {
            _handlersLock.Release();
        }
    }
    
    private async Task StoreEventAsync<T>(T @event) where T : class
    {
        try
        {
            var query = @"
                INSERT INTO event_store (event_id, event_type, event_data, created_at, status)
                VALUES (@EventId, @EventType, @EventData, @CreatedAt, @Status)";
            
            var parameters = new
            {
                EventId = Guid.NewGuid(),
                EventType = typeof(T).Name,
                EventData = JsonSerializer.Serialize(@event),
                CreatedAt = DateTime.UtcNow,
                Status = "published"
            };
            
            await _connection.ExecuteAsync(query, parameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store event {EventType}", typeof(T).Name);
            throw;
        }
    }
    
    private async Task NotifyHandlersAsync<T>(T @event) where T : class
    {
        await _handlersLock.WaitAsync();
        
        try
        {
            var eventType = typeof(T);
            
            if (_handlers.ContainsKey(eventType))
            {
                var tasks = _handlers[eventType]
                    .Cast<IEventHandler<T>>()
                    .Select(handler => HandleEventAsync(handler, @event));
                
                await Task.WhenAll(tasks);
            }
        }
        finally
        {
            _handlersLock.Release();
        }
    }
    
    private async Task HandleEventAsync<T>(IEventHandler<T> handler, T @event) where T : class
    {
        try
        {
            await handler.HandleAsync(@event);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Handler failed for event {EventType}", typeof(T).Name);
            
            // Store failed event for retry
            await StoreFailedEventAsync(@event, ex.Message);
        }
    }
    
    private async Task StoreFailedEventAsync<T>(T @event, string errorMessage) where T : class
    {
        try
        {
            var query = @"
                INSERT INTO failed_events (event_id, event_type, event_data, error_message, created_at, retry_count)
                VALUES (@EventId, @EventType, @EventData, @ErrorMessage, @CreatedAt, @RetryCount)";
            
            var parameters = new
            {
                EventId = Guid.NewGuid(),
                EventType = typeof(T).Name,
                EventData = JsonSerializer.Serialize(@event),
                ErrorMessage = errorMessage,
                CreatedAt = DateTime.UtcNow,
                RetryCount = 0
            };
            
            await _connection.ExecuteAsync(query, parameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store failed event {EventType}", typeof(T).Name);
        }
    }
}

public interface IEventHandler<in T> where T : class
{
    Task HandleAsync(T @event);
}

public class UserCreatedEventHandler : IEventHandler<UserCreatedEvent>
{
    private readonly ILogger<UserCreatedEventHandler> _logger;
    private readonly IEmailService _emailService;
    
    public UserCreatedEventHandler(ILogger<UserCreatedEventHandler> logger, IEmailService emailService)
    {
        _logger = logger;
        _emailService = emailService;
    }
    
    public async Task HandleAsync(UserCreatedEvent @event)
    {
        try
        {
            _logger.LogInformation("Handling user created event for user {UserId}", @event.UserId);
            
            // Send welcome email
            await _emailService.SendWelcomeEmailAsync(@event.Email);
            
            _logger.LogInformation("Welcome email sent for user {UserId}", @event.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle user created event for user {UserId}", @event.UserId);
            throw;
        }
    }
}

public class UserCreatedEvent
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class UserUpdatedEvent
{
    public Guid UserId { get; set; }
    public DateTime Timestamp { get; set; }
}

public class UserDeactivatedEvent
{
    public Guid UserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
```

## 📋 CQRS Pattern

### Command and Query Separation

```csharp
public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    Task<CommandResult> HandleAsync(TCommand command);
}

public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query);
}

public interface ICommand
{
    Guid Id { get; }
}

public interface IQuery<TResult>
{
    Guid Id { get; }
}

public class CommandResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public object? Data { get; set; }
    
    public static CommandResult Success(object? data = null)
    {
        return new CommandResult { Success = true, Data = data };
    }
    
    public static CommandResult Failure(string errorMessage)
    {
        return new CommandResult { Success = false, ErrorMessage = errorMessage };
    }
}

public class CreateUserCommand : ICommand
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand>
{
    private readonly ILogger<CreateUserCommandHandler> _logger;
    private readonly IUserRepository _userRepository;
    private readonly IEventBus _eventBus;
    
    public CreateUserCommandHandler(
        ILogger<CreateUserCommandHandler> logger,
        IUserRepository userRepository,
        IEventBus eventBus)
    {
        _logger = logger;
        _userRepository = userRepository;
        _eventBus = eventBus;
    }
    
    public async Task<CommandResult> HandleAsync(CreateUserCommand command)
    {
        try
        {
            // Check if user already exists
            var existingUser = await _userRepository.GetByEmailAsync(command.Email);
            if (existingUser != null)
            {
                return CommandResult.Failure($"User with email {command.Email} already exists");
            }
            
            // Create user
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = command.Email,
                FirstName = command.FirstName,
                LastName = command.LastName,
                CreatedAt = DateTime.UtcNow,
                Status = UserStatus.Active
            };
            
            await _userRepository.CreateAsync(user);
            
            // Publish event
            var userCreatedEvent = new UserCreatedEvent
            {
                UserId = user.Id,
                Email = user.Email,
                Timestamp = DateTime.UtcNow
            };
            
            await _eventBus.PublishAsync(userCreatedEvent);
            
            _logger.LogInformation("User created: {UserId}", user.Id);
            
            return CommandResult.Success(user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user: {Email}", command.Email);
            return CommandResult.Failure(ex.Message);
        }
    }
}

public class GetUserQuery : IQuery<UserDto?>
{
    public Guid Id { get; } = Guid.NewGuid();
    public Guid UserId { get; set; }
}

public class GetUserQueryHandler : IQueryHandler<GetUserQuery, UserDto?>
{
    private readonly ILogger<GetUserQueryHandler> _logger;
    private readonly IUserRepository _userRepository;
    
    public GetUserQueryHandler(ILogger<GetUserQueryHandler> logger, IUserRepository userRepository)
    {
        _logger = logger;
        _userRepository = userRepository;
    }
    
    public async Task<UserDto?> HandleAsync(GetUserQuery query)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(query.UserId);
            
            if (user == null)
            {
                return null;
            }
            
            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Status = user.Status
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user: {UserId}", query.UserId);
            throw;
        }
    }
}

public class GetUsersQuery : IQuery<List<UserDto>>
{
    public Guid Id { get; } = Guid.NewGuid();
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SearchTerm { get; set; }
    public UserStatus? Status { get; set; }
}

public class GetUsersQueryHandler : IQueryHandler<GetUsersQuery, List<UserDto>>
{
    private readonly ILogger<GetUsersQueryHandler> _logger;
    private readonly IUserRepository _userRepository;
    
    public GetUsersQueryHandler(ILogger<GetUsersQueryHandler> logger, IUserRepository userRepository)
    {
        _logger = logger;
        _userRepository = userRepository;
    }
    
    public async Task<List<UserDto>> HandleAsync(GetUsersQuery query)
    {
        try
        {
            var users = await _userRepository.GetUsersAsync(query.Page, query.PageSize, query.SearchTerm, query.Status);
            
            return users.Select(user => new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Status = user.Status
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get users");
            throw;
        }
    }
}

public class CommandQueryDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CommandQueryDispatcher> _logger;
    
    public CommandQueryDispatcher(IServiceProvider serviceProvider, ILogger<CommandQueryDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    
    public async Task<CommandResult> SendAsync<TCommand>(TCommand command) where TCommand : ICommand
    {
        try
        {
            var handlerType = typeof(ICommandHandler<>).MakeGenericType(command.GetType());
            var handler = _serviceProvider.GetService(handlerType);
            
            if (handler == null)
            {
                throw new InvalidOperationException($"No handler found for command {command.GetType().Name}");
            }
            
            var method = handlerType.GetMethod("HandleAsync");
            var result = await (Task<CommandResult>)method!.Invoke(handler, new object[] { command })!;
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send command {CommandType}", command.GetType().Name);
            return CommandResult.Failure(ex.Message);
        }
    }
    
    public async Task<TResult> SendAsync<TQuery, TResult>(TQuery query) where TQuery : IQuery<TResult>
    {
        try
        {
            var handlerType = typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResult));
            var handler = _serviceProvider.GetService(handlerType);
            
            if (handler == null)
            {
                throw new InvalidOperationException($"No handler found for query {query.GetType().Name}");
            }
            
            var method = handlerType.GetMethod("HandleAsync");
            var result = await (Task<TResult>)method!.Invoke(handler, new object[] { query })!;
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send query {QueryType}", query.GetType().Name);
            throw;
        }
    }
}
```

## 📝 Summary

This guide covered advanced architectural patterns for C# TuskLang applications:

- **Microservices Architecture**: Service base classes, service discovery, and inter-service communication
- **Event-Driven Architecture**: Event bus, event handlers, and event persistence
- **CQRS Pattern**: Command and query separation with dedicated handlers and dispatchers
- **Advanced Patterns**: Service registration, health checks, and event sourcing

These architectural patterns enable scalable, maintainable, and robust C# TuskLang applications with clear separation of concerns and excellent testability. 