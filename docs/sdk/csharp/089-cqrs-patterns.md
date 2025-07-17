# CQRS Patterns in C# with TuskLang

## Overview

CQRS (Command Query Responsibility Segregation) separates read and write operations for a data store, allowing you to optimize each side independently. This guide covers how to implement CQRS using C# and TuskLang configuration for building scalable, maintainable applications.

## Table of Contents

- [CQRS Concepts](#cqrs-concepts)
- [TuskLang CQRS Configuration](#tusklang-cqrs-configuration)
- [Command Side](#command-side)
- [Query Side](#query-side)
- [C# CQRS Example](#c-cqrs-example)
- [Event Sourcing Integration](#event-sourcing-integration)
- [Read Models](#read-models)
- [Best Practices](#best-practices)

## CQRS Concepts

- **Commands**: Write operations that change state
- **Queries**: Read operations that retrieve data
- **Command Handlers**: Process commands and update write models
- **Query Handlers**: Retrieve data from optimized read models
- **Event Sourcing**: Often used with CQRS for audit trails

## TuskLang CQRS Configuration

```ini
# cqrs.tsk
[cqrs]
enabled = @env("CQRS_ENABLED", "true")
event_sourcing_enabled = @env("EVENT_SOURCING_ENABLED", "true")
read_model_sync = @env("READ_MODEL_SYNC", "eventual")

[commands]
user_commands = @env("USER_COMMANDS_ENABLED", "true")
order_commands = @env("ORDER_COMMANDS_ENABLED", "true")
product_commands = @env("PRODUCT_COMMANDS_ENABLED", "true")

[queries]
user_queries = @env("USER_QUERIES_ENABLED", "true")
order_queries = @env("ORDER_QUERIES_ENABLED", "true")
product_queries = @env("PRODUCT_QUERIES_ENABLED", "true")

[read_models]
user_summary = @env("USER_SUMMARY_READ_MODEL", "true")
order_history = @env("ORDER_HISTORY_READ_MODEL", "true")
product_catalog = @env("PRODUCT_CATALOG_READ_MODEL", "true")

[databases]
write_database = @env.secure("WRITE_DATABASE_CONNECTION")
read_database = @env.secure("READ_DATABASE_CONNECTION")
```

## Command Side

```csharp
public interface ICommand
{
    Guid Id { get; }
    DateTime Timestamp { get; }
}

public interface ICommandHandler<TCommand> where TCommand : ICommand
{
    Task HandleAsync(TCommand command);
}

public class CreateUserCommand : ICommand
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Email { get; set; }
    public string Name { get; set; }
}

public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IEventStore _eventStore;
    private readonly IConfiguration _config;

    public CreateUserCommandHandler(
        IUserRepository userRepository,
        IEventStore eventStore,
        IConfiguration config)
    {
        _userRepository = userRepository;
        _eventStore = eventStore;
        _config = config;
    }

    public async Task HandleAsync(CreateUserCommand command)
    {
        // Validate command
        if (string.IsNullOrEmpty(command.Email))
            throw new ValidationException("Email is required");

        // Check if user already exists
        var existingUser = await _userRepository.GetByEmailAsync(command.Email);
        if (existingUser != null)
            throw new BusinessRuleException("User with this email already exists");

        // Create user aggregate
        var user = new UserAggregate(_eventStore, _config, command.Id.ToString());
        await user.CreateUserAsync(command.Email, command.Name);

        // Publish domain events if needed
        await PublishDomainEventsAsync(command);
    }

    private async Task PublishDomainEventsAsync(CreateUserCommand command)
    {
        // Implementation to publish domain events
    }
}
```

## Query Side

```csharp
public interface IQuery<TResult>
{
    Guid Id { get; }
    DateTime Timestamp { get; }
}

public interface IQueryHandler<TQuery, TResult> where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query);
}

public class GetUserQuery : IQuery<UserSummary>
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string UserId { get; set; }
}

public class GetUserQueryHandler : IQueryHandler<GetUserQuery, UserSummary>
{
    private readonly IUserSummaryRepository _userSummaryRepository;
    private readonly IConfiguration _config;

    public GetUserQueryHandler(
        IUserSummaryRepository userSummaryRepository,
        IConfiguration config)
    {
        _userSummaryRepository = userSummaryRepository;
        _config = config;
    }

    public async Task<UserSummary> HandleAsync(GetUserQuery query)
    {
        if (!bool.Parse(_config["queries:user_queries"]))
            throw new FeatureDisabledException("User queries are disabled");

        var userSummary = await _userSummaryRepository.GetByIdAsync(query.UserId);
        if (userSummary == null)
            throw new NotFoundException($"User {query.UserId} not found");

        return userSummary;
    }
}
```

## C# CQRS Example

```csharp
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using TuskLang;

public class CQRSMediator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _config;

    public CQRSMediator(IServiceProvider serviceProvider, IConfiguration config)
    {
        _serviceProvider = serviceProvider;
        _config = config;
    }

    public async Task SendAsync<TCommand>(TCommand command) where TCommand : ICommand
    {
        if (!bool.Parse(_config["cqrs:enabled"]))
            throw new FeatureDisabledException("CQRS is disabled");

        var handlerType = typeof(ICommandHandler<>).MakeGenericType(typeof(TCommand));
        var handler = _serviceProvider.GetService(handlerType);

        if (handler == null)
            throw new InvalidOperationException($"No handler found for command {typeof(TCommand).Name}");

        await (Task)handlerType.GetMethod("HandleAsync").Invoke(handler, new object[] { command });
    }

    public async Task<TResult> SendAsync<TQuery, TResult>(TQuery query) where TQuery : IQuery<TResult>
    {
        if (!bool.Parse(_config["cqrs:enabled"]))
            throw new FeatureDisabledException("CQRS is disabled");

        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(typeof(TQuery), typeof(TResult));
        var handler = _serviceProvider.GetService(handlerType);

        if (handler == null)
            throw new InvalidOperationException($"No handler found for query {typeof(TQuery).Name}");

        return await (Task<TResult>)handlerType.GetMethod("HandleAsync").Invoke(handler, new object[] { query });
    }
}

// API Controller Example
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly CQRSMediator _mediator;
    private readonly IConfiguration _config;

    public UsersController(CQRSMediator mediator, IConfiguration config)
    {
        _mediator = mediator;
        _config = config;
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        if (!bool.Parse(_config["commands:user_commands"]))
            return BadRequest("User commands are disabled");

        var command = new CreateUserCommand
        {
            Email = request.Email,
            Name = request.Name
        };

        await _mediator.SendAsync(command);
        return Ok(new { userId = command.Id });
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetUser(string userId)
    {
        var query = new GetUserQuery { UserId = userId };
        var user = await _mediator.SendAsync<GetUserQuery, UserSummary>(query);
        return Ok(user);
    }
}
```

## Event Sourcing Integration

```csharp
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

    public async Task CreateUserAsync(string email, string name)
    {
        var @event = new UserCreatedEvent
        {
            Id = Guid.NewGuid(),
            Type = "UserCreated",
            OccurredOn = DateTime.UtcNow,
            Email = email,
            Name = name
        };

        await _eventStore.AppendEventsAsync(_streamId, new[] { @event }, _version);
        Apply(@event);
        _version++;
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
        }
    }
}
```

## Read Models

```csharp
public class UserSummary
{
    public string Id { get; set; }
    public string Email { get; set; }
    public string Name { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public int OrderCount { get; set; }
    public decimal TotalSpent { get; set; }
}

public interface IUserSummaryRepository
{
    Task<UserSummary> GetByIdAsync(string id);
    Task<List<UserSummary>> GetAllAsync();
    Task SaveAsync(UserSummary userSummary);
    Task UpdateAsync(UserSummary userSummary);
}

public class UserSummaryProjection : IProjection
{
    private readonly IUserSummaryRepository _repository;
    private readonly IConfiguration _config;

    public UserSummaryProjection(IUserSummaryRepository repository, IConfiguration config)
    {
        _repository = repository;
        _config = config;
    }

    public async Task HandleAsync(IEvent @event)
    {
        if (!bool.Parse(_config["read_models:user_summary"]))
            return;

        switch (@event.Type)
        {
            case "UserCreated":
                await HandleUserCreatedAsync((UserCreatedEvent)@event);
                break;
            case "OrderCreated":
                await HandleOrderCreatedAsync((OrderCreatedEvent)@event);
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

    private async Task HandleOrderCreatedAsync(OrderCreatedEvent @event)
    {
        var summary = await _repository.GetByIdAsync(@event.UserId);
        if (summary != null)
        {
            summary.OrderCount++;
            summary.TotalSpent += @event.Amount;
            await _repository.UpdateAsync(summary);
        }
    }
}
```

## Best Practices

1. **Separate read and write models completely**
2. **Use commands for all write operations**
3. **Optimize read models for specific use cases**
4. **Implement eventual consistency for read models**
5. **Use event sourcing for audit trails**
6. **Handle command validation and business rules**
7. **Monitor command and query performance**

## Conclusion

CQRS with C# and TuskLang enables building scalable, maintainable applications with optimized read and write operations. By leveraging TuskLang for configuration and CQRS patterns, you can create systems that handle complex business requirements efficiently. 