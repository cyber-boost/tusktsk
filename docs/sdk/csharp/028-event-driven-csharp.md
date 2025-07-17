# 📡 Event-Driven Architecture - TuskLang for C# - "Event Mastery"

**Master event-driven architecture with TuskLang in your C# applications!**

Event-driven architecture enables loose coupling and scalability. This guide covers event sourcing, CQRS, message queues, and real-world event-driven scenarios for TuskLang in C# environments.

## 🎯 Event-Driven Philosophy

### "We Don't Bow to Any King"
- **Loose coupling** - Services communicate via events
- **Event sourcing** - Events as source of truth
- **CQRS** - Separate read and write models
- **Scalability** - Handle high event volumes
- **Replay capability** - Rebuild state from events

## 📨 Event Sourcing

### Example: Event Store with TuskLang
```csharp
// EventStoreService.cs
using System.Text.Json;

public class EventStoreService
{
    private readonly TuskLang _parser;
    private readonly ILogger<EventStoreService> _logger;
    private readonly List<Event> _events;
    
    public EventStoreService(ILogger<EventStoreService> logger)
    {
        _parser = new TuskLang();
        _events = new List<Event>();
        _logger = logger;
    }
    
    public async Task<Event> AppendEventAsync(string aggregateId, string eventType, Dictionary<string, object> data)
    {
        var @event = new Event
        {
            Id = Guid.NewGuid().ToString(),
            AggregateId = aggregateId,
            EventType = eventType,
            Data = data,
            Timestamp = DateTime.UtcNow,
            Version = await GetNextVersionAsync(aggregateId)
        };
        
        _events.Add(@event);
        
        _logger.LogInformation("Appended event {EventType} for aggregate {AggregateId} at version {Version}", 
            eventType, aggregateId, @event.Version);
        
        return @event;
    }
    
    public async Task<List<Event>> GetEventsAsync(string aggregateId, int fromVersion = 0)
    {
        var events = _events
            .Where(e => e.AggregateId == aggregateId && e.Version > fromVersion)
            .OrderBy(e => e.Version)
            .ToList();
        
        _logger.LogInformation("Retrieved {Count} events for aggregate {AggregateId} from version {FromVersion}", 
            events.Count, aggregateId, fromVersion);
        
        return events;
    }
    
    public async Task<Dictionary<string, object>> GetEventStoreConfigurationAsync()
    {
        var config = new Dictionary<string, object>();
        
        // Event store statistics
        config["total_events"] = _events.Count;
        config["unique_aggregates"] = _events.Select(e => e.AggregateId).Distinct().Count();
        config["event_types"] = _events.Select(e => e.EventType).Distinct().ToList();
        
        // Recent events
        var recentEvents = _events
            .OrderByDescending(e => e.Timestamp)
            .Take(10)
            .Select(e => new Dictionary<string, object>
            {
                ["id"] = e.Id,
                ["aggregate_id"] = e.AggregateId,
                ["event_type"] = e.EventType,
                ["timestamp"] = e.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                ["version"] = e.Version
            })
            .ToList();
        
        config["recent_events"] = recentEvents;
        
        return config;
    }
    
    private async Task<int> GetNextVersionAsync(string aggregateId)
    {
        var lastEvent = _events
            .Where(e => e.AggregateId == aggregateId)
            .OrderByDescending(e => e.Version)
            .FirstOrDefault();
        
        return lastEvent?.Version + 1 ?? 1;
    }
}

public class Event
{
    public string Id { get; set; } = string.Empty;
    public string AggregateId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
    public DateTime Timestamp { get; set; }
    public int Version { get; set; }
}
```

## 🔄 CQRS Implementation

### Example: Command and Query Separation
```csharp
// CqrsService.cs
public class CqrsService
{
    private readonly EventStoreService _eventStore;
    private readonly TuskLang _parser;
    private readonly ILogger<CqrsService> _logger;
    private readonly Dictionary<string, object> _readModels;
    
    public CqrsService(EventStoreService eventStore, ILogger<CqrsService> logger)
    {
        _eventStore = eventStore;
        _parser = new TuskLang();
        _readModels = new Dictionary<string, object>();
        _logger = logger;
    }
    
    // Command side - Write operations
    public async Task<CommandResult> ExecuteCommandAsync(ICommand command)
    {
        try
        {
            var result = new CommandResult { Success = true };
            
            switch (command)
            {
                case CreateUserCommand createUser:
                    result = await HandleCreateUserAsync(createUser);
                    break;
                case UpdateUserCommand updateUser:
                    result = await HandleUpdateUserAsync(updateUser);
                    break;
                case DeleteUserCommand deleteUser:
                    result = await HandleDeleteUserAsync(deleteUser);
                    break;
                default:
                    result.Success = false;
                    result.Error = $"Unknown command type: {command.GetType().Name}";
                    break;
            }
            
            if (result.Success)
            {
                // Update read models
                await UpdateReadModelsAsync(command.AggregateId);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute command {CommandType}", command.GetType().Name);
            return new CommandResult { Success = false, Error = ex.Message };
        }
    }
    
    // Query side - Read operations
    public async Task<Dictionary<string, object>> ExecuteQueryAsync(IQuery query)
    {
        try
        {
            switch (query)
            {
                case GetUserQuery getUser:
                    return await HandleGetUserAsync(getUser);
                case GetUsersQuery getUsers:
                    return await HandleGetUsersAsync(getUsers);
                case GetUserHistoryQuery getUserHistory:
                    return await HandleGetUserHistoryAsync(getUserHistory);
                default:
                    return new Dictionary<string, object> { ["error"] = $"Unknown query type: {query.GetType().Name}" };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute query {QueryType}", query.GetType().Name);
            return new Dictionary<string, object> { ["error"] = ex.Message };
        }
    }
    
    private async Task<CommandResult> HandleCreateUserAsync(CreateUserCommand command)
    {
        var eventData = new Dictionary<string, object>
        {
            ["user_id"] = command.UserId,
            ["first_name"] = command.FirstName,
            ["last_name"] = command.LastName,
            ["email"] = command.Email
        };
        
        await _eventStore.AppendEventAsync(command.UserId, "UserCreated", eventData);
        
        return new CommandResult { Success = true };
    }
    
    private async Task<CommandResult> HandleUpdateUserAsync(UpdateUserCommand command)
    {
        var eventData = new Dictionary<string, object>
        {
            ["user_id"] = command.UserId,
            ["first_name"] = command.FirstName,
            ["last_name"] = command.LastName
        };
        
        await _eventStore.AppendEventAsync(command.UserId, "UserUpdated", eventData);
        
        return new CommandResult { Success = true };
    }
    
    private async Task<CommandResult> HandleDeleteUserAsync(DeleteUserCommand command)
    {
        var eventData = new Dictionary<string, object>
        {
            ["user_id"] = command.UserId
        };
        
        await _eventStore.AppendEventAsync(command.UserId, "UserDeleted", eventData);
        
        return new CommandResult { Success = true };
    }
    
    private async Task<Dictionary<string, object>> HandleGetUserAsync(GetUserQuery query)
    {
        if (_readModels.TryGetValue(query.UserId, out var user))
        {
            return new Dictionary<string, object> { ["user"] = user };
        }
        
        return new Dictionary<string, object> { ["error"] = "User not found" };
    }
    
    private async Task<Dictionary<string, object>> HandleGetUsersAsync(GetUsersQuery query)
    {
        var users = _readModels.Values.Where(u => u is Dictionary<string, object>).ToList();
        return new Dictionary<string, object> { ["users"] = users };
    }
    
    private async Task<Dictionary<string, object>> HandleGetUserHistoryAsync(GetUserHistoryQuery query)
    {
        var events = await _eventStore.GetEventsAsync(query.UserId);
        return new Dictionary<string, object> { ["events"] = events };
    }
    
    private async Task UpdateReadModelsAsync(string aggregateId)
    {
        var events = await _eventStore.GetEventsAsync(aggregateId);
        var user = new Dictionary<string, object>();
        
        foreach (var @event in events)
        {
            switch (@event.EventType)
            {
                case "UserCreated":
                    user["id"] = @event.Data["user_id"];
                    user["first_name"] = @event.Data["first_name"];
                    user["last_name"] = @event.Data["last_name"];
                    user["email"] = @event.Data["email"];
                    user["created_at"] = @event.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
                    break;
                case "UserUpdated":
                    user["first_name"] = @event.Data["first_name"];
                    user["last_name"] = @event.Data["last_name"];
                    user["updated_at"] = @event.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
                    break;
                case "UserDeleted":
                    user["deleted"] = true;
                    user["deleted_at"] = @event.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
                    break;
            }
        }
        
        if (user.ContainsKey("deleted") && (bool)user["deleted"])
        {
            _readModels.Remove(aggregateId);
        }
        else
        {
            _readModels[aggregateId] = user;
        }
    }
}

// Command and Query interfaces
public interface ICommand
{
    string AggregateId { get; }
}

public interface IQuery { }

public class CreateUserCommand : ICommand
{
    public string AggregateId => UserId;
    public string UserId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class UpdateUserCommand : ICommand
{
    public string AggregateId => UserId;
    public string UserId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

public class DeleteUserCommand : ICommand
{
    public string AggregateId => UserId;
    public string UserId { get; set; } = string.Empty;
}

public class GetUserQuery : IQuery
{
    public string UserId { get; set; } = string.Empty;
}

public class GetUsersQuery : IQuery { }

public class GetUserHistoryQuery : IQuery
{
    public string UserId { get; set; } = string.Empty;
}

public class CommandResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
}
```

## 📬 Message Queue Integration

### Example: RabbitMQ Integration
```csharp
// MessageQueueService.cs
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

public class MessageQueueService : IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly TuskLang _parser;
    private readonly ILogger<MessageQueueService> _logger;
    
    public MessageQueueService(ILogger<MessageQueueService> logger)
    {
        _parser = new TuskLang();
        _logger = logger;
        
        var factory = new ConnectionFactory
        {
            HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost",
            UserName = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "guest",
            Password = Environment.GetEnvironmentVariable("RABBITMQ_PASS") ?? "guest"
        };
        
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        
        // Declare exchanges and queues
        _channel.ExchangeDeclare("user_events", ExchangeType.Topic, durable: true);
        _channel.QueueDeclare("user_commands", durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind("user_commands", "user_events", "user.*");
    }
    
    public void PublishEvent(string routingKey, Dictionary<string, object> eventData)
    {
        try
        {
            var message = JsonSerializer.Serialize(eventData);
            var body = Encoding.UTF8.GetBytes(message);
            
            _channel.BasicPublish(
                exchange: "user_events",
                routingKey: routingKey,
                basicProperties: null,
                body: body
            );
            
            _logger.LogInformation("Published event with routing key {RoutingKey}", routingKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event with routing key {RoutingKey}", routingKey);
            throw;
        }
    }
    
    public void StartConsuming(string queueName, Action<Dictionary<string, object>> messageHandler)
    {
        var consumer = new EventingBasicConsumer(_channel);
        
        consumer.Received += (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var eventData = JsonSerializer.Deserialize<Dictionary<string, object>>(message);
                
                messageHandler(eventData);
                
                _channel.BasicAck(ea.DeliveryTag, false);
                
                _logger.LogInformation("Processed message from queue {QueueName}", queueName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process message from queue {QueueName}", queueName);
                _channel.BasicNack(ea.DeliveryTag, false, true);
            }
        };
        
        _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
        _logger.LogInformation("Started consuming from queue {QueueName}", queueName);
    }
    
    public async Task<Dictionary<string, object>> GetQueueConfigurationAsync()
    {
        var config = new Dictionary<string, object>();
        
        // Queue statistics
        var queueInfo = _channel.QueueDeclarePassive("user_commands");
        config["queue_name"] = "user_commands";
        config["message_count"] = queueInfo.MessageCount;
        config["consumer_count"] = queueInfo.ConsumerCount;
        
        // Exchange information
        config["exchange_name"] = "user_events";
        config["exchange_type"] = "topic";
        
        return config;
    }
    
    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
```

## 🛠️ Real-World Event-Driven Scenarios
- **E-commerce**: Order events, inventory events, payment events
- **Social media**: Post events, like events, comment events
- **Banking**: Transaction events, account events, fraud events
- **IoT**: Sensor events, device events, alert events

## 🧩 Best Practices
- Use event sourcing for audit trails
- Implement CQRS for complex domains
- Use message queues for reliable event delivery
- Design events for backward compatibility
- Monitor event processing performance

## 🏁 You're Ready!

You can now:
- Implement event sourcing with C# TuskLang
- Use CQRS for complex domains
- Integrate with message queues
- Build scalable event-driven systems

**Next:** [Caching Strategies](029-caching-csharp.md)

---

**"We don't bow to any king" - Your event mastery, your architectural excellence, your scalability power.**

Build event-driven systems. Scale with confidence. 📡 