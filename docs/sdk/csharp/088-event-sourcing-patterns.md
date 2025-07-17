# Event Sourcing Patterns in C# with TuskLang

## Overview

Event sourcing is a pattern that stores all changes to application state as a sequence of events. This guide covers how to implement event sourcing using C# and TuskLang configuration for building scalable, auditable, and maintainable applications.

## Table of Contents

- [Event Sourcing Concepts](#event-sourcing-concepts)
- [TuskLang Event Sourcing Configuration](#tusklang-event-sourcing-configuration)
- [Event Store Implementation](#event-store-implementation)
- [C# Event Sourcing Example](#c-event-sourcing-example)
- [Aggregate Pattern](#aggregate-pattern)
- [Event Projections](#event-projections)
- [Snapshot Management](#snapshot-management)
- [Best Practices](#best-practices)

## Event Sourcing Concepts

- **Events**: Immutable records of what happened
- **Event Store**: Database that stores all events
- **Aggregates**: Business entities that handle commands and produce events
- **Projections**: Read models built from events
- **Snapshots**: Performance optimization for large event streams

## TuskLang Event Sourcing Configuration

```ini
# event-sourcing.tsk
[event_sourcing]
enabled = @env("EVENT_SOURCING_ENABLED", "true")
event_store_connection = @env.secure("EVENT_STORE_CONNECTION")
snapshot_interval = @env("SNAPSHOT_INTERVAL", "100")
max_events_per_stream = @env("MAX_EVENTS_PER_STREAM", "10000")

[events]
user_created = "UserCreated"
user_updated = "UserUpdated"
user_deleted = "UserDeleted"
order_created = "OrderCreated"
order_cancelled = "OrderCancelled"

[projections]
user_summary = @env("USER_SUMMARY_PROJECTION", "true")
order_history = @env("ORDER_HISTORY_PROJECTION", "true")
audit_log = @env("AUDIT_LOG_PROJECTION", "true")

[snapshots]
enabled = @env("SNAPSHOTS_ENABLED", "true")
storage_connection = @env.secure("SNAPSHOT_STORAGE_CONNECTION")
compression_enabled = @env("SNAPSHOT_COMPRESSION", "true")
```

## Event Store Implementation

```csharp
public interface IEventStore
{
    Task AppendEventsAsync(string streamId, IEnumerable<IEvent> events, long expectedVersion);
    Task<IEnumerable<IEvent>> GetEventsAsync(string streamId, long fromVersion = 0);
    Task<long> GetStreamVersionAsync(string streamId);
    Task SaveSnapshotAsync(string streamId, ISnapshot snapshot);
    Task<ISnapshot> GetLatestSnapshotAsync(string streamId);
}

public interface IEvent
{
    Guid Id { get; }
    string Type { get; }
    DateTime OccurredOn { get; }
    long Version { get; set; }
}

public interface ISnapshot
{
    string StreamId { get; }
    long Version { get; }
    DateTime CreatedOn { get; }
}
```

## C# Event Sourcing Example

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using TuskLang;

public class UserAggregate
{
    private readonly IEventStore _eventStore;
    private readonly IConfiguration _config;
    private readonly string _streamId;
    private UserState _state;
    private long _version;

    public UserAggregate(IEventStore eventStore, IConfiguration config, string userId)
    {
        _eventStore = eventStore;
        _config = config;
        _streamId = $"user-{userId}";
        _state = new UserState();
    }

    public async Task LoadAsync()
    {
        // Try to load from snapshot first
        var snapshot = await _eventStore.GetLatestSnapshotAsync(_streamId);
        if (snapshot != null)
        {
            _state = ((UserSnapshot)snapshot).State;
            _version = snapshot.Version;
        }

        // Load remaining events
        var events = await _eventStore.GetEventsAsync(_streamId, _version + 1);
        foreach (var @event in events)
        {
            Apply(@event);
            _version = @event.Version;
        }
    }

    public async Task CreateUserAsync(string email, string name)
    {
        var @event = new UserCreatedEvent
        {
            Id = Guid.NewGuid(),
            Type = _config["events:user_created"],
            OccurredOn = DateTime.UtcNow,
            Email = email,
            Name = name
        };

        await _eventStore.AppendEventsAsync(_streamId, new[] { @event }, _version);
        Apply(@event);
        _version++;
    }

    public async Task UpdateUserAsync(string name)
    {
        var @event = new UserUpdatedEvent
        {
            Id = Guid.NewGuid(),
            Type = _config["events:user_updated"],
            OccurredOn = DateTime.UtcNow,
            Name = name
        };

        await _eventStore.AppendEventsAsync(_streamId, new[] { @event }, _version);
        Apply(@event);
        _version++;

        // Create snapshot if needed
        if (_version % int.Parse(_config["event_sourcing:snapshot_interval"]) == 0)
        {
            var snapshot = new UserSnapshot
            {
                StreamId = _streamId,
                Version = _version,
                CreatedOn = DateTime.UtcNow,
                State = _state
            };
            await _eventStore.SaveSnapshotAsync(_streamId, snapshot);
        }
    }

    private void Apply(IEvent @event)
    {
        switch (@event.Type)
        {
            case "UserCreated":
                var createdEvent = (UserCreatedEvent)@event;
                _state.Email = createdEvent.Email;
                _state.Name = createdEvent.Name;
                _state.CreatedOn = createdEvent.OccurredOn;
                break;
            case "UserUpdated":
                var updatedEvent = (UserUpdatedEvent)@event;
                _state.Name = updatedEvent.Name;
                _state.UpdatedOn = updatedEvent.OccurredOn;
                break;
        }
    }
}

public class UserState
{
    public string Email { get; set; }
    public string Name { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? UpdatedOn { get; set; }
}

public class UserCreatedEvent : IEvent
{
    public Guid Id { get; set; }
    public string Type { get; set; }
    public DateTime OccurredOn { get; set; }
    public long Version { get; set; }
    public string Email { get; set; }
    public string Name { get; set; }
}

public class UserUpdatedEvent : IEvent
{
    public Guid Id { get; set; }
    public string Type { get; set; }
    public DateTime OccurredOn { get; set; }
    public long Version { get; set; }
    public string Name { get; set; }
}

public class UserSnapshot : ISnapshot
{
    public string StreamId { get; set; }
    public long Version { get; set; }
    public DateTime CreatedOn { get; set; }
    public UserState State { get; set; }
}
```

## Aggregate Pattern

- Encapsulate business logic in aggregates
- Handle commands and produce events
- Maintain consistency boundaries
- Implement optimistic concurrency control

## Event Projections

```csharp
public interface IProjection
{
    Task HandleAsync(IEvent @event);
    Task RebuildAsync();
}

public class UserSummaryProjection : IProjection
{
    private readonly IConfiguration _config;
    private readonly IUserSummaryRepository _repository;

    public UserSummaryProjection(IConfiguration config, IUserSummaryRepository repository)
    {
        _config = config;
        _repository = repository;
    }

    public async Task HandleAsync(IEvent @event)
    {
        if (!bool.Parse(_config["projections:user_summary"]))
            return;

        switch (@event.Type)
        {
            case "UserCreated":
                await HandleUserCreatedAsync((UserCreatedEvent)@event);
                break;
            case "UserUpdated":
                await HandleUserUpdatedAsync((UserUpdatedEvent)@event);
                break;
        }
    }

    private async Task HandleUserCreatedAsync(UserCreatedEvent @event)
    {
        var summary = new UserSummary
        {
            Id = @event.Id.ToString(),
            Email = @event.Email,
            Name = @event.Name,
            CreatedOn = @event.OccurredOn
        };
        await _repository.SaveAsync(summary);
    }

    private async Task HandleUserUpdatedAsync(UserUpdatedEvent @event)
    {
        var summary = await _repository.GetByIdAsync(@event.Id.ToString());
        if (summary != null)
        {
            summary.Name = @event.Name;
            summary.UpdatedOn = @event.OccurredOn;
            await _repository.SaveAsync(summary);
        }
    }

    public async Task RebuildAsync()
    {
        // Implementation to rebuild projection from all events
    }
}
```

## Snapshot Management

- Performance optimization for large event streams
- Compress snapshots for storage efficiency
- Automatic snapshot creation based on configuration
- Snapshot versioning and cleanup

## Best Practices

1. **Design events to be immutable and self-contained**
2. **Use aggregates to maintain consistency boundaries**
3. **Implement projections for read models**
4. **Use snapshots for performance optimization**
5. **Handle event versioning and schema evolution**
6. **Implement proper error handling and recovery**
7. **Monitor event store performance and storage**

## Conclusion

Event sourcing with C# and TuskLang enables building scalable, auditable, and maintainable applications. By leveraging TuskLang for configuration and event management, you can create systems that provide complete audit trails and support complex business requirements. 