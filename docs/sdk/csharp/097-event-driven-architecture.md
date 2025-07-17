# Event-Driven Architecture in C# with TuskLang

## Overview

Event-Driven Architecture (EDA) is a software architecture pattern that promotes the production, detection, consumption, and reaction to events. This guide covers how to implement EDA using C# and TuskLang configuration for building loosely coupled, scalable, and responsive applications.

## Table of Contents

- [Event-Driven Architecture Concepts](#event-driven-architecture-concepts)
- [TuskLang EDA Configuration](#tusklang-eda-configuration)
- [Event Publishing](#event-publishing)
- [C# EDA Example](#c-eda-example)
- [Event Consumers](#event-consumers)
- [Event Sourcing Integration](#event-sourcing-integration)
- [Best Practices](#best-practices)

## Event-Driven Architecture Concepts

- **Events**: Immutable records of something that happened
- **Event Publishers**: Components that produce events
- **Event Consumers**: Components that react to events
- **Event Bus**: Infrastructure that routes events to consumers
- **Event Store**: Database that stores all events
- **Event Sourcing**: Pattern that stores state as a sequence of events

## TuskLang EDA Configuration

```ini
# event-driven.tsk
[event_driven]
enabled = @env("EVENT_DRIVEN_ENABLED", "true")
event_bus_type = @env("EVENT_BUS_TYPE", "rabbitmq")
event_store_enabled = @env("EVENT_STORE_ENABLED", "true")
event_sourcing_enabled = @env("EVENT_SOURCING_ENABLED", "true")

[event_bus]
rabbitmq_connection = @env.secure("RABBITMQ_CONNECTION_STRING")
kafka_bootstrap_servers = @env("KAFKA_BOOTSTRAP_SERVERS", "localhost:9092")
azure_service_bus_connection = @env.secure("AZURE_SERVICE_BUS_CONNECTION")

[event_store]
connection_string = @env.secure("EVENT_STORE_CONNECTION_STRING")
database_name = @env("EVENT_STORE_DATABASE", "EventStore")
snapshot_interval = @env("EVENT_STORE_SNAPSHOT_INTERVAL", "100")

[events]
user_created = @env("USER_CREATED_EVENT", "UserCreated")
user_updated = @env("USER_UPDATED_EVENT", "UserUpdated")
user_deleted = @env("USER_DELETED_EVENT", "UserDeleted")
order_created = @env("ORDER_CREATED_EVENT", "OrderCreated")
order_cancelled = @env("ORDER_CANCELLED_EVENT", "OrderCancelled")
payment_processed = @env("PAYMENT_PROCESSED_EVENT", "PaymentProcessed")

[consumers]
email_service = @env("EMAIL_SERVICE_CONSUMER", "true")
notification_service = @env("NOTIFICATION_SERVICE_CONSUMER", "true")
analytics_service = @env("ANALYTICS_SERVICE_CONSUMER", "true")
audit_service = @env("AUDIT_SERVICE_CONSUMER", "true")

[monitoring]
event_metrics_enabled = @env("EVENT_METRICS_ENABLED", "true")
consumer_health_checks = @env("CONSUMER_HEALTH_CHECKS", "true")
dead_letter_queue_enabled = @env("DEAD_LETTER_QUEUE_ENABLED", "true")
```

## Event Publishing

```csharp
public interface IEvent
{
    string Id { get; }
    string Type { get; }
    DateTime OccurredOn { get; }
    string Source { get; }
    Dictionary<string, object> Metadata { get; }
}

public interface IEventPublisher
{
    Task PublishAsync<T>(T @event) where T : IEvent;
    Task PublishAsync<T>(T @event, string topic) where T : IEvent;
}

public interface IEventBus
{
    Task PublishAsync<T>(T @event) where T : IEvent;
    Task SubscribeAsync<T>(IEventHandler<T> handler) where T : IEvent;
    Task UnsubscribeAsync<T>(IEventHandler<T> handler) where T : IEvent;
}

public interface IEventHandler<in T> where T : IEvent
{
    Task HandleAsync(T @event);
}

public class EventBus : IEventBus
{
    private readonly IConfiguration _config;
    private readonly ILogger<EventBus> _logger;
    private readonly Dictionary<Type, List<object>> _handlers;
    private readonly IEventStore _eventStore;

    public EventBus(IConfiguration config, ILogger<EventBus> logger, IEventStore eventStore)
    {
        _config = config;
        _logger = logger;
        _eventStore = eventStore;
        _handlers = new Dictionary<Type, List<object>>();
    }

    public async Task PublishAsync<T>(T @event) where T : IEvent
    {
        try
        {
            // Store event if event sourcing is enabled
            if (bool.Parse(_config["event_driven:event_sourcing_enabled"]))
            {
                await _eventStore.AppendEventAsync(@event);
            }

            // Notify all handlers
            if (_handlers.TryGetValue(typeof(T), out var handlers))
            {
                var tasks = handlers.Cast<IEventHandler<T>>()
                    .Select(handler => HandleEventAsync(handler, @event));
                
                await Task.WhenAll(tasks);
            }

            _logger.LogInformation("Published event {EventType} with ID {EventId}", 
                @event.Type, @event.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing event {EventType}", @event.Type);
            throw;
        }
    }

    public Task SubscribeAsync<T>(IEventHandler<T> handler) where T : IEvent
    {
        var eventType = typeof(T);
        
        if (!_handlers.ContainsKey(eventType))
        {
            _handlers[eventType] = new List<object>();
        }
        
        _handlers[eventType].Add(handler);
        
        _logger.LogInformation("Subscribed handler {HandlerType} to event {EventType}", 
            handler.GetType().Name, eventType.Name);
        
        return Task.CompletedTask;
    }

    public Task UnsubscribeAsync<T>(IEventHandler<T> handler) where T : IEvent
    {
        var eventType = typeof(T);
        
        if (_handlers.ContainsKey(eventType))
        {
            _handlers[eventType].Remove(handler);
        }
        
        return Task.CompletedTask;
    }

    private async Task HandleEventAsync<T>(IEventHandler<T> handler, T @event) where T : IEvent
    {
        try
        {
            await handler.HandleAsync(@event);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling event {EventType} with handler {HandlerType}", 
                @event.Type, handler.GetType().Name);
            
            // Implement dead letter queue logic here
            if (bool.Parse(_config["monitoring:dead_letter_queue_enabled"]))
            {
                await SendToDeadLetterQueueAsync(@event, ex);
            }
        }
    }

    private async Task SendToDeadLetterQueueAsync<T>(T @event, Exception ex) where T : IEvent
    {
        // Implementation to send failed events to dead letter queue
        _logger.LogWarning("Sending event {EventId} to dead letter queue", @event.Id);
    }
}
```

## C# EDA Example

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using TuskLang;

// Domain Events
public class UserCreatedEvent : IEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = "UserCreated";
    public DateTime OccurredOn { get; set; } = DateTime.UtcNow;
    public string Source { get; set; } = "UserService";
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    public string UserId { get; set; }
    public string Email { get; set; }
    public string Name { get; set; }
}

public class UserUpdatedEvent : IEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = "UserUpdated";
    public DateTime OccurredOn { get; set; } = DateTime.UtcNow;
    public string Source { get; set; } = "UserService";
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    public string UserId { get; set; }
    public string Name { get; set; }
    public Dictionary<string, object> Changes { get; set; } = new();
}

public class OrderCreatedEvent : IEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = "OrderCreated";
    public DateTime OccurredOn { get; set; } = DateTime.UtcNow;
    public string Source { get; set; } = "OrderService";
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    public string OrderId { get; set; }
    public string CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}

// Event Handlers
public class EmailServiceEventHandler : IEventHandler<UserCreatedEvent>, IEventHandler<OrderCreatedEvent>
{
    private readonly IEmailService _emailService;
    private readonly IConfiguration _config;
    private readonly ILogger<EmailServiceEventHandler> _logger;

    public EmailServiceEventHandler(
        IEmailService emailService,
        IConfiguration config,
        ILogger<EmailServiceEventHandler> logger)
    {
        _emailService = emailService;
        _config = config;
        _logger = logger;
    }

    public async Task HandleAsync(UserCreatedEvent @event)
    {
        if (!bool.Parse(_config["consumers:email_service"]))
            return;

        try
        {
            await _emailService.SendWelcomeEmailAsync(@event.Email, @event.Name);
            _logger.LogInformation("Sent welcome email to user {UserId}", @event.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending welcome email to user {UserId}", @event.UserId);
            throw;
        }
    }

    public async Task HandleAsync(OrderCreatedEvent @event)
    {
        if (!bool.Parse(_config["consumers:email_service"]))
            return;

        try
        {
            await _emailService.SendOrderConfirmationAsync(@event.CustomerId, @event.OrderId, @event.TotalAmount);
            _logger.LogInformation("Sent order confirmation email for order {OrderId}", @event.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending order confirmation email for order {OrderId}", @event.OrderId);
            throw;
        }
    }
}

public class NotificationServiceEventHandler : IEventHandler<UserCreatedEvent>, IEventHandler<OrderCreatedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly IConfiguration _config;
    private readonly ILogger<NotificationServiceEventHandler> _logger;

    public NotificationServiceEventHandler(
        INotificationService notificationService,
        IConfiguration config,
        ILogger<NotificationServiceEventHandler> logger)
    {
        _notificationService = notificationService;
        _config = config;
        _logger = logger;
    }

    public async Task HandleAsync(UserCreatedEvent @event)
    {
        if (!bool.Parse(_config["consumers:notification_service"]))
            return;

        try
        {
            await _notificationService.SendUserCreatedNotificationAsync(@event.UserId, @event.Name);
            _logger.LogInformation("Sent user created notification for user {UserId}", @event.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending user created notification for user {UserId}", @event.UserId);
            throw;
        }
    }

    public async Task HandleAsync(OrderCreatedEvent @event)
    {
        if (!bool.Parse(_config["consumers:notification_service"]))
            return;

        try
        {
            await _notificationService.SendOrderCreatedNotificationAsync(@event.CustomerId, @event.OrderId);
            _logger.LogInformation("Sent order created notification for order {OrderId}", @event.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending order created notification for order {OrderId}", @event.OrderId);
            throw;
        }
    }
}

public class AnalyticsServiceEventHandler : IEventHandler<UserCreatedEvent>, IEventHandler<OrderCreatedEvent>
{
    private readonly IAnalyticsService _analyticsService;
    private readonly IConfiguration _config;
    private readonly ILogger<AnalyticsServiceEventHandler> _logger;

    public AnalyticsServiceEventHandler(
        IAnalyticsService analyticsService,
        IConfiguration config,
        ILogger<AnalyticsServiceEventHandler> logger)
    {
        _analyticsService = analyticsService;
        _config = config;
        _logger = logger;
    }

    public async Task HandleAsync(UserCreatedEvent @event)
    {
        if (!bool.Parse(_config["consumers:analytics_service"]))
            return;

        try
        {
            await _analyticsService.TrackUserRegistrationAsync(@event.UserId, @event.OccurredOn);
            _logger.LogInformation("Tracked user registration for user {UserId}", @event.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking user registration for user {UserId}", @event.UserId);
            throw;
        }
    }

    public async Task HandleAsync(OrderCreatedEvent @event)
    {
        if (!bool.Parse(_config["consumers:analytics_service"]))
            return;

        try
        {
            await _analyticsService.TrackOrderCreatedAsync(@event.OrderId, @event.CustomerId, @event.TotalAmount);
            _logger.LogInformation("Tracked order creation for order {OrderId}", @event.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking order creation for order {OrderId}", @event.OrderId);
            throw;
        }
    }
}

// Domain Services that publish events
public class UserService
{
    private readonly IUserRepository _userRepository;
    private readonly IEventBus _eventBus;
    private readonly IConfiguration _config;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository userRepository,
        IEventBus eventBus,
        IConfiguration config,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _eventBus = eventBus;
        _config = config;
        _logger = logger;
    }

    public async Task<User> CreateUserAsync(CreateUserRequest request)
    {
        // Create user
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = request.Email,
            Name = request.Name,
            CreatedOn = DateTime.UtcNow
        };

        await _userRepository.SaveAsync(user);

        // Publish event
        var @event = new UserCreatedEvent
        {
            UserId = user.Id,
            Email = user.Email,
            Name = user.Name
        };

        await _eventBus.PublishAsync(@event);

        _logger.LogInformation("Created user {UserId} and published UserCreated event", user.Id);
        return user;
    }

    public async Task<User> UpdateUserAsync(string userId, UpdateUserRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new NotFoundException($"User {userId} not found");

        var changes = new Dictionary<string, object>();
        
        if (request.Name != null && request.Name != user.Name)
        {
            changes["Name"] = new { Old = user.Name, New = request.Name };
            user.Name = request.Name;
        }

        if (changes.Any())
        {
            user.UpdatedOn = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            // Publish event
            var @event = new UserUpdatedEvent
            {
                UserId = user.Id,
                Name = user.Name,
                Changes = changes
            };

            await _eventBus.PublishAsync(@event);

            _logger.LogInformation("Updated user {UserId} and published UserUpdated event", user.Id);
        }

        return user;
    }
}
```

## Event Consumers

```csharp
public interface IEventConsumer
{
    string ConsumerName { get; }
    Task StartAsync();
    Task StopAsync();
}

public class EventConsumer<T> : IEventConsumer where T : IEvent
{
    private readonly IEventBus _eventBus;
    private readonly IEventHandler<T> _handler;
    private readonly IConfiguration _config;
    private readonly ILogger<EventConsumer<T>> _logger;

    public string ConsumerName { get; }

    public EventConsumer(
        IEventBus eventBus,
        IEventHandler<T> handler,
        IConfiguration config,
        ILogger<EventConsumer<T>> logger)
    {
        _eventBus = eventBus;
        _handler = handler;
        _config = config;
        _logger = logger;
        ConsumerName = $"{typeof(T).Name}Consumer";
    }

    public async Task StartAsync()
    {
        await _eventBus.SubscribeAsync(_handler);
        _logger.LogInformation("Started event consumer {ConsumerName}", ConsumerName);
    }

    public async Task StopAsync()
    {
        await _eventBus.UnsubscribeAsync(_handler);
        _logger.LogInformation("Stopped event consumer {ConsumerName}", ConsumerName);
    }
}

public class EventConsumerManager
{
    private readonly IEnumerable<IEventConsumer> _consumers;
    private readonly IConfiguration _config;
    private readonly ILogger<EventConsumerManager> _logger;

    public EventConsumerManager(
        IEnumerable<IEventConsumer> consumers,
        IConfiguration config,
        ILogger<EventConsumerManager> logger)
    {
        _consumers = consumers;
        _config = config;
        _logger = logger;
    }

    public async Task StartAllConsumersAsync()
    {
        if (!bool.Parse(_config["event_driven:enabled"]))
            return;

        foreach (var consumer in _consumers)
        {
            try
            {
                await consumer.StartAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting consumer {ConsumerName}", consumer.ConsumerName);
            }
        }

        _logger.LogInformation("Started {Count} event consumers", _consumers.Count());
    }

    public async Task StopAllConsumersAsync()
    {
        foreach (var consumer in _consumers)
        {
            try
            {
                await consumer.StopAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping consumer {ConsumerName}", consumer.ConsumerName);
            }
        }

        _logger.LogInformation("Stopped all event consumers");
    }
}
```

## Event Sourcing Integration

```csharp
public interface IEventStore
{
    Task AppendEventAsync<T>(T @event) where T : IEvent;
    Task<IEnumerable<IEvent>> GetEventsAsync(string aggregateId);
    Task<IEnumerable<IEvent>> GetEventsAsync(string aggregateId, long fromVersion);
}

public class EventStore : IEventStore
{
    private readonly IDbConnection _connection;
    private readonly IConfiguration _config;
    private readonly ILogger<EventStore> _logger;

    public EventStore(IDbConnection connection, IConfiguration config, ILogger<EventStore> logger)
    {
        _connection = connection;
        _config = config;
        _logger = logger;
    }

    public async Task AppendEventAsync<T>(T @event) where T : IEvent
    {
        var sql = @"
            INSERT INTO Events (Id, Type, AggregateId, Data, OccurredOn, Source, Metadata)
            VALUES (@Id, @Type, @AggregateId, @Data, @OccurredOn, @Source, @Metadata)";

        await _connection.ExecuteAsync(sql, new
        {
            @event.Id,
            @event.Type,
            AggregateId = GetAggregateId(@event),
            Data = JsonSerializer.Serialize(@event),
            @event.OccurredOn,
            @event.Source,
            Metadata = JsonSerializer.Serialize(@event.Metadata)
        });

        _logger.LogDebug("Stored event {EventType} with ID {EventId}", @event.Type, @event.Id);
    }

    public async Task<IEnumerable<IEvent>> GetEventsAsync(string aggregateId)
    {
        var sql = @"
            SELECT Id, Type, Data, OccurredOn, Source, Metadata
            FROM Events
            WHERE AggregateId = @AggregateId
            ORDER BY OccurredOn";

        var events = await _connection.QueryAsync<EventData>(sql, new { AggregateId = aggregateId });
        return events.Select(DeserializeEvent);
    }

    public async Task<IEnumerable<IEvent>> GetEventsAsync(string aggregateId, long fromVersion)
    {
        var sql = @"
            SELECT Id, Type, Data, OccurredOn, Source, Metadata
            FROM Events
            WHERE AggregateId = @AggregateId AND Version > @FromVersion
            ORDER BY OccurredOn";

        var events = await _connection.QueryAsync<EventData>(sql, new { AggregateId = aggregateId, FromVersion = fromVersion });
        return events.Select(DeserializeEvent);
    }

    private string GetAggregateId(IEvent @event)
    {
        // Extract aggregate ID from event based on event type
        return @event switch
        {
            UserCreatedEvent e => e.UserId,
            UserUpdatedEvent e => e.UserId,
            OrderCreatedEvent e => e.OrderId,
            _ => @event.Id
        };
    }

    private IEvent DeserializeEvent(EventData eventData)
    {
        var eventType = Type.GetType(eventData.Type);
        if (eventType == null)
            throw new InvalidOperationException($"Unknown event type: {eventData.Type}");

        return (IEvent)JsonSerializer.Deserialize(eventData.Data, eventType);
    }
}

public class EventData
{
    public string Id { get; set; }
    public string Type { get; set; }
    public string Data { get; set; }
    public DateTime OccurredOn { get; set; }
    public string Source { get; set; }
    public string Metadata { get; set; }
}
```

## Best Practices

1. **Design events to be immutable and self-contained**
2. **Use event sourcing for audit trails and state reconstruction**
3. **Implement proper error handling and dead letter queues**
4. **Monitor event processing performance**
5. **Use event versioning for schema evolution**
6. **Implement idempotent event handlers**
7. **Test event-driven workflows thoroughly**

## Conclusion

Event-Driven Architecture with C# and TuskLang enables building loosely coupled, scalable, and responsive applications. By leveraging TuskLang for configuration and EDA patterns, you can create systems that react to business events efficiently and maintain clear separation of concerns. 