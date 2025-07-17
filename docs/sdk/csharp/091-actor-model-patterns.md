# Actor Model Patterns in C# with TuskLang

## Overview

The Actor Model is a concurrency model where actors are the universal primitives of computation. This guide covers how to implement actor patterns using C# and TuskLang configuration for building highly concurrent, scalable, and fault-tolerant applications.

## Table of Contents

- [Actor Model Concepts](#actor-model-concepts)
- [TuskLang Actor Configuration](#tusklang-actor-configuration)
- [Actor Implementation](#actor-implementation)
- [C# Actor Example](#c-actor-example)
- [Actor Supervision](#actor-supervision)
- [Message Passing](#message-passing)
- [Actor Clustering](#actor-clustering)
- [Best Practices](#best-practices)

## Actor Model Concepts

- **Actors**: Independent units of computation with private state
- **Messages**: Immutable communication between actors
- **Mailboxes**: Queues that hold messages for actors
- **Supervision**: Error handling and recovery strategies
- **Isolation**: Actors don't share state, communicate only via messages

## TuskLang Actor Configuration

```ini
# actor.tsk
[actor_system]
name = @env("ACTOR_SYSTEM_NAME", "MyActorSystem")
max_actors = @env("MAX_ACTORS", "10000")
mailbox_size = @env("MAILBOX_SIZE", "1000")
dispatcher_threads = @env("DISPATCHER_THREADS", "4")

[supervision]
strategy = @env("SUPERVISION_STRATEGY", "restart")
max_restarts = @env("MAX_RESTARTS", "3")
restart_window = @env("RESTART_WINDOW_SECONDS", "60")

[clustering]
enabled = @env("ACTOR_CLUSTERING_ENABLED", "true")
cluster_name = @env("ACTOR_CLUSTER_NAME", "MyCluster")
seed_nodes = @env("ACTOR_SEED_NODES", "localhost:8080,localhost:8081")

[monitoring]
actor_metrics_enabled = @env("ACTOR_METRICS_ENABLED", "true")
message_tracking_enabled = @env("MESSAGE_TRACKING_ENABLED", "true")
dead_letter_monitoring = @env("DEAD_LETTER_MONITORING", "true")
```

## Actor Implementation

```csharp
public interface IActor
{
    string Id { get; }
    Task ReceiveAsync(object message);
    Task StartAsync();
    Task StopAsync();
}

public abstract class Actor : IActor
{
    private readonly IConfiguration _config;
    private readonly ILogger<Actor> _logger;
    private readonly Mailbox<object> _mailbox;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private Task _processingTask;

    public string Id { get; }
    protected bool IsRunning { get; private set; }

    protected Actor(IConfiguration config, ILogger<Actor> logger, string id)
    {
        _config = config;
        _logger = logger;
        Id = id;
        _mailbox = new Mailbox<object>(int.Parse(_config["actor_system:mailbox_size"]));
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public async Task StartAsync()
    {
        IsRunning = true;
        _processingTask = ProcessMessagesAsync();
        _logger.LogInformation("Actor {ActorId} started", Id);
    }

    public async Task StopAsync()
    {
        IsRunning = false;
        _cancellationTokenSource.Cancel();
        
        if (_processingTask != null)
            await _processingTask;
            
        _logger.LogInformation("Actor {ActorId} stopped", Id);
    }

    public async Task ReceiveAsync(object message)
    {
        if (!IsRunning)
            throw new InvalidOperationException($"Actor {Id} is not running");

        await _mailbox.PostAsync(message);
    }

    private async Task ProcessMessagesAsync()
    {
        while (IsRunning && !_cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                var message = await _mailbox.ReceiveAsync(_cancellationTokenSource.Token);
                await HandleMessageAsync(message);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message in actor {ActorId}", Id);
                await HandleErrorAsync(ex);
            }
        }
    }

    protected abstract Task HandleMessageAsync(object message);
    protected virtual Task HandleErrorAsync(Exception ex) => Task.CompletedTask;
}
```

## C# Actor Example

```csharp
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using TuskLang;

public class UserActor : Actor
{
    private readonly IUserRepository _userRepository;
    private UserState _state;

    public UserActor(
        IConfiguration config,
        ILogger<UserActor> logger,
        IUserRepository userRepository,
        string userId) 
        : base(config, logger, $"user-{userId}")
    {
        _userRepository = userRepository;
        _state = new UserState { Id = userId };
    }

    protected override async Task HandleMessageAsync(object message)
    {
        switch (message)
        {
            case CreateUserCommand cmd:
                await HandleCreateUserAsync(cmd);
                break;
            case UpdateUserCommand cmd:
                await HandleUpdateUserAsync(cmd);
                break;
            case GetUserQuery query:
                await HandleGetUserAsync(query);
                break;
            default:
                Logger.LogWarning("Unknown message type: {MessageType}", message.GetType().Name);
                break;
        }
    }

    private async Task HandleCreateUserAsync(CreateUserCommand command)
    {
        try
        {
            var user = new User
            {
                Id = _state.Id,
                Email = command.Email,
                Name = command.Name,
                CreatedOn = DateTime.UtcNow
            };

            await _userRepository.CreateAsync(user);
            _state = new UserState
            {
                Id = user.Id,
                Email = user.Email,
                Name = user.Name,
                CreatedOn = user.CreatedOn
            };

            Logger.LogInformation("User {UserId} created", _state.Id);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating user {UserId}", _state.Id);
            throw;
        }
    }

    private async Task HandleUpdateUserAsync(UpdateUserCommand command)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(_state.Id);
            if (user == null)
                throw new NotFoundException($"User {_state.Id} not found");

            user.Name = command.Name;
            user.UpdatedOn = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            _state.Name = user.Name;
            _state.UpdatedOn = user.UpdatedOn;

            Logger.LogInformation("User {UserId} updated", _state.Id);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating user {UserId}", _state.Id);
            throw;
        }
    }

    private async Task HandleGetUserAsync(GetUserQuery query)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(_state.Id);
            if (user == null)
                throw new NotFoundException($"User {_state.Id} not found");

            // Send response back to sender
            var response = new GetUserResponse { User = user };
            // Implementation to send response
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting user {UserId}", _state.Id);
            throw;
        }
    }
}

public class UserState
{
    public string Id { get; set; }
    public string Email { get; set; }
    public string Name { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? UpdatedOn { get; set; }
}

public class CreateUserCommand
{
    public string Email { get; set; }
    public string Name { get; set; }
}

public class UpdateUserCommand
{
    public string Name { get; set; }
}

public class GetUserQuery
{
    public string UserId { get; set; }
}

public class GetUserResponse
{
    public User User { get; set; }
}
```

## Actor Supervision

```csharp
public interface ISupervisor
{
    Task HandleFailureAsync(IActor actor, Exception exception);
    Task RestartActorAsync(IActor actor);
    Task StopActorAsync(IActor actor);
}

public class Supervisor : ISupervisor
{
    private readonly IConfiguration _config;
    private readonly ILogger<Supervisor> _logger;
    private readonly Dictionary<string, int> _restartCounts;
    private readonly Dictionary<string, DateTime> _lastRestartTimes;

    public Supervisor(IConfiguration config, ILogger<Supervisor> logger)
    {
        _config = config;
        _logger = logger;
        _restartCounts = new Dictionary<string, int>();
        _lastRestartTimes = new Dictionary<string, DateTime>();
    }

    public async Task HandleFailureAsync(IActor actor, Exception exception)
    {
        var strategy = _config["supervision:strategy"];
        var maxRestarts = int.Parse(_config["supervision:max_restarts"]);
        var restartWindow = TimeSpan.FromSeconds(
            int.Parse(_config["supervision:restart_window_seconds"]));

        _logger.LogError(exception, "Actor {ActorId} failed", actor.Id);

        switch (strategy.ToLower())
        {
            case "restart":
                await HandleRestartStrategyAsync(actor, maxRestarts, restartWindow);
                break;
            case "stop":
                await StopActorAsync(actor);
                break;
            case "escalate":
                await EscalateFailureAsync(actor, exception);
                break;
            default:
                _logger.LogWarning("Unknown supervision strategy: {Strategy}", strategy);
                break;
        }
    }

    private async Task HandleRestartStrategyAsync(IActor actor, int maxRestarts, TimeSpan restartWindow)
    {
        var now = DateTime.UtcNow;
        var lastRestart = _lastRestartTimes.GetValueOrDefault(actor.Id, DateTime.MinValue);
        var restartCount = _restartCounts.GetValueOrDefault(actor.Id, 0);

        // Reset restart count if outside window
        if (now - lastRestart > restartWindow)
        {
            restartCount = 0;
        }

        if (restartCount < maxRestarts)
        {
            restartCount++;
            _restartCounts[actor.Id] = restartCount;
            _lastRestartTimes[actor.Id] = now;

            _logger.LogInformation("Restarting actor {ActorId} (attempt {Attempt}/{Max})", 
                actor.Id, restartCount, maxRestarts);

            await RestartActorAsync(actor);
        }
        else
        {
            _logger.LogError("Actor {ActorId} exceeded max restarts, stopping", actor.Id);
            await StopActorAsync(actor);
        }
    }

    public async Task RestartActorAsync(IActor actor)
    {
        try
        {
            await actor.StopAsync();
            await Task.Delay(1000); // Brief delay before restart
            await actor.StartAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restarting actor {ActorId}", actor.Id);
            throw;
        }
    }

    public async Task StopActorAsync(IActor actor)
    {
        try
        {
            await actor.StopAsync();
            _logger.LogInformation("Actor {ActorId} stopped by supervisor", actor.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping actor {ActorId}", actor.Id);
            throw;
        }
    }

    private async Task EscalateFailureAsync(IActor actor, Exception exception)
    {
        // Implementation to escalate failure to parent supervisor
        _logger.LogError("Escalating failure for actor {ActorId}", actor.Id);
    }
}
```

## Message Passing

```csharp
public class Mailbox<T>
{
    private readonly Channel<T> _channel;
    private readonly int _capacity;

    public Mailbox(int capacity)
    {
        _capacity = capacity;
        _channel = Channel.CreateBounded<T>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        });
    }

    public async Task PostAsync(T message)
    {
        await _channel.Writer.WriteAsync(message);
    }

    public async Task<T> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        return await _channel.Reader.ReadAsync(cancellationToken);
    }

    public bool TryReceive(out T message)
    {
        return _channel.Reader.TryRead(out message);
    }

    public int Count => _channel.Reader.Count;
}

public class ActorSystem
{
    private readonly IConfiguration _config;
    private readonly ILogger<ActorSystem> _logger;
    private readonly ISupervisor _supervisor;
    private readonly ConcurrentDictionary<string, IActor> _actors;
    private readonly SemaphoreSlim _actorLimit;

    public ActorSystem(
        IConfiguration config,
        ILogger<ActorSystem> logger,
        ISupervisor supervisor)
    {
        _config = config;
        _logger = logger;
        _supervisor = supervisor;
        _actors = new ConcurrentDictionary<string, IActor>();
        var maxActors = int.Parse(_config["actor_system:max_actors"]);
        _actorLimit = new SemaphoreSlim(maxActors, maxActors);
    }

    public async Task<IActor> CreateActorAsync<T>(string id, params object[] constructorArgs) 
        where T : IActor
    {
        await _actorLimit.WaitAsync();

        try
        {
            if (_actors.TryGetValue(id, out var existingActor))
                return existingActor;

            var actor = (T)Activator.CreateInstance(typeof(T), constructorArgs);
            await actor.StartAsync();

            _actors[id] = actor;
            _logger.LogInformation("Created actor {ActorId} of type {ActorType}", id, typeof(T).Name);

            return actor;
        }
        catch (Exception ex)
        {
            _actorLimit.Release();
            _logger.LogError(ex, "Error creating actor {ActorId}", id);
            throw;
        }
    }

    public async Task SendAsync(string actorId, object message)
    {
        if (_actors.TryGetValue(actorId, out var actor))
        {
            await actor.ReceiveAsync(message);
        }
        else
        {
            throw new ActorNotFoundException($"Actor {actorId} not found");
        }
    }

    public async Task StopActorAsync(string actorId)
    {
        if (_actors.TryRemove(actorId, out var actor))
        {
            await actor.StopAsync();
            _actorLimit.Release();
            _logger.LogInformation("Stopped actor {ActorId}", actorId);
        }
    }

    public async Task StopAllAsync()
    {
        var stopTasks = _actors.Values.Select(actor => actor.StopAsync());
        await Task.WhenAll(stopTasks);
        _actors.Clear();
        _logger.LogInformation("Stopped all actors");
    }
}
```

## Actor Clustering

```csharp
public interface IActorCluster
{
    Task<IActor> GetActorAsync(string actorId);
    Task SendAsync(string actorId, object message);
    Task RegisterNodeAsync(string nodeId, string address);
    Task UnregisterNodeAsync(string nodeId);
}

public class ActorCluster : IActorCluster
{
    private readonly IConfiguration _config;
    private readonly ILogger<ActorCluster> _logger;
    private readonly Dictionary<string, string> _nodes;
    private readonly ActorSystem _localActorSystem;

    public ActorCluster(
        IConfiguration config,
        ILogger<ActorCluster> logger,
        ActorSystem localActorSystem)
    {
        _config = config;
        _logger = logger;
        _localActorSystem = localActorSystem;
        _nodes = new Dictionary<string, string>();
    }

    public async Task<IActor> GetActorAsync(string actorId)
    {
        // Check if actor exists locally
        // If not, route to appropriate node
        var nodeId = DetermineNodeForActor(actorId);
        
        if (nodeId == GetLocalNodeId())
        {
            return await _localActorSystem.CreateActorAsync<UserActor>(actorId);
        }
        else
        {
            return await GetRemoteActorAsync(actorId, nodeId);
        }
    }

    public async Task SendAsync(string actorId, object message)
    {
        var nodeId = DetermineNodeForActor(actorId);
        
        if (nodeId == GetLocalNodeId())
        {
            await _localActorSystem.SendAsync(actorId, message);
        }
        else
        {
            await SendRemoteMessageAsync(actorId, message, nodeId);
        }
    }

    private string DetermineNodeForActor(string actorId)
    {
        // Simple hash-based routing
        var hash = actorId.GetHashCode();
        var nodeCount = _nodes.Count;
        var nodeIndex = Math.Abs(hash) % nodeCount;
        return _nodes.Keys.ElementAt(nodeIndex);
    }

    private string GetLocalNodeId()
    {
        return Environment.MachineName;
    }

    private async Task<IActor> GetRemoteActorAsync(string actorId, string nodeId)
    {
        // Implementation to get remote actor reference
        throw new NotImplementedException();
    }

    private async Task SendRemoteMessageAsync(string actorId, object message, string nodeId)
    {
        // Implementation to send message to remote node
        throw new NotImplementedException();
    }

    public async Task RegisterNodeAsync(string nodeId, string address)
    {
        _nodes[nodeId] = address;
        _logger.LogInformation("Registered node {NodeId} at {Address}", nodeId, address);
    }

    public async Task UnregisterNodeAsync(string nodeId)
    {
        if (_nodes.Remove(nodeId))
        {
            _logger.LogInformation("Unregistered node {NodeId}", nodeId);
        }
    }
}
```

## Best Practices

1. **Keep actors small and focused on a single responsibility**
2. **Use immutable messages for communication**
3. **Implement proper supervision strategies**
4. **Monitor actor performance and mailbox sizes**
5. **Handle actor failures gracefully**
6. **Use clustering for scalability**
7. **Test actor interactions thoroughly**

## Conclusion

Actor model patterns with C# and TuskLang enable building highly concurrent, scalable, and fault-tolerant applications. By leveraging TuskLang for configuration and actor management, you can create systems that handle complex concurrency requirements efficiently. 