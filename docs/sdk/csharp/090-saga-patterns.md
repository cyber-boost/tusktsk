# Saga Patterns in C# with TuskLang

## Overview

Saga patterns manage distributed transactions across multiple services by breaking them into a sequence of local transactions with compensating actions. This guide covers how to implement saga patterns using C# and TuskLang configuration for building reliable, distributed applications.

## Table of Contents

- [Saga Concepts](#saga-concepts)
- [TuskLang Saga Configuration](#tusklang-saga-configuration)
- [Choreography vs Orchestration](#choreography-vs-orchestration)
- [C# Saga Example](#c-saga-example)
- [Compensation Actions](#compensation-actions)
- [Saga State Management](#saga-state-management)
- [Monitoring & Recovery](#monitoring--recovery)
- [Best Practices](#best-practices)

## Saga Concepts

- **Local Transactions**: Individual service operations
- **Compensating Actions**: Rollback operations for failed steps
- **Saga Coordinator**: Manages saga execution and state
- **Event-Driven**: Uses events for communication between services
- **Idempotency**: Ensures operations can be safely retried

## TuskLang Saga Configuration

```ini
# saga.tsk
[saga]
enabled = @env("SAGA_ENABLED", "true")
timeout = @env("SAGA_TIMEOUT_MINUTES", "30")
max_retries = @env("SAGA_MAX_RETRIES", "3")
retry_delay = @env("SAGA_RETRY_DELAY_SECONDS", "5")

[order_saga]
steps = ["validate_order", "reserve_inventory", "process_payment", "confirm_order"]
compensation_steps = ["cancel_order", "release_inventory", "refund_payment"]
timeout = @env("ORDER_SAGA_TIMEOUT_MINUTES", "15")

[payment_saga]
steps = ["authorize_payment", "capture_payment", "settle_payment"]
compensation_steps = ["void_payment", "refund_payment"]
timeout = @env("PAYMENT_SAGA_TIMEOUT_MINUTES", "10")

[monitoring]
saga_tracking_enabled = @env("SAGA_TRACKING_ENABLED", "true")
event_store_connection = @env.secure("SAGA_EVENT_STORE_CONNECTION")
alert_on_failure = @env("SAGA_ALERT_ON_FAILURE", "true")
```

## Choreography vs Orchestration

### Choreography (Event-Driven)
- Services communicate via events
- No central coordinator
- Decoupled architecture
- Complex to track and debug

### Orchestration (Centralized)
- Central saga coordinator
- Explicit workflow definition
- Easier to monitor and debug
- Single point of failure

## C# Saga Example

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using TuskLang;

public interface ISaga
{
    string Id { get; }
    string Name { get; }
    SagaState State { get; }
    Task ExecuteAsync();
    Task CompensateAsync();
}

public enum SagaState
{
    Started,
    InProgress,
    Completed,
    Failed,
    Compensating,
    Compensated
}

public class OrderSaga : ISaga
{
    private readonly IConfiguration _config;
    private readonly ISagaCoordinator _coordinator;
    private readonly IOrderService _orderService;
    private readonly IInventoryService _inventoryService;
    private readonly IPaymentService _paymentService;
    private readonly ISagaStateStore _stateStore;

    public string Id { get; }
    public string Name => "OrderSaga";
    public SagaState State { get; private set; }
    public OrderSagaData Data { get; }

    public OrderSaga(
        IConfiguration config,
        ISagaCoordinator coordinator,
        IOrderService orderService,
        IInventoryService inventoryService,
        IPaymentService paymentService,
        ISagaStateStore stateStore,
        string orderId)
    {
        _config = config;
        _coordinator = coordinator;
        _orderService = orderService;
        _inventoryService = inventoryService;
        _paymentService = paymentService;
        _stateStore = stateStore;
        Id = orderId;
        Data = new OrderSagaData { OrderId = orderId };
        State = SagaState.Started;
    }

    public async Task ExecuteAsync()
    {
        try
        {
            State = SagaState.InProgress;
            await _stateStore.SaveStateAsync(Id, State, Data);

            // Step 1: Validate Order
            await ValidateOrderAsync();

            // Step 2: Reserve Inventory
            await ReserveInventoryAsync();

            // Step 3: Process Payment
            await ProcessPaymentAsync();

            // Step 4: Confirm Order
            await ConfirmOrderAsync();

            State = SagaState.Completed;
            await _stateStore.SaveStateAsync(Id, State, Data);

            _coordinator.CompleteSaga(Id);
        }
        catch (Exception ex)
        {
            State = SagaState.Failed;
            await _stateStore.SaveStateAsync(Id, State, Data);
            await CompensateAsync();
            throw;
        }
    }

    public async Task CompensateAsync()
    {
        try
        {
            State = SagaState.Compensating;
            await _stateStore.SaveStateAsync(Id, State, Data);

            // Execute compensation steps in reverse order
            if (Data.PaymentProcessed)
                await RefundPaymentAsync();

            if (Data.InventoryReserved)
                await ReleaseInventoryAsync();

            if (Data.OrderValidated)
                await CancelOrderAsync();

            State = SagaState.Compensated;
            await _stateStore.SaveStateAsync(Id, State, Data);
        }
        catch (Exception ex)
        {
            // Log compensation failure - manual intervention may be required
            throw;
        }
    }

    private async Task ValidateOrderAsync()
    {
        var order = await _orderService.ValidateOrderAsync(Data.OrderId);
        Data.OrderValidated = true;
        Data.OrderData = order;
        await _stateStore.SaveStateAsync(Id, State, Data);
    }

    private async Task ReserveInventoryAsync()
    {
        var inventoryReservation = await _inventoryService.ReserveInventoryAsync(
            Data.OrderData.ProductId, 
            Data.OrderData.Quantity);
        
        Data.InventoryReserved = true;
        Data.InventoryReservationId = inventoryReservation.Id;
        await _stateStore.SaveStateAsync(Id, State, Data);
    }

    private async Task ProcessPaymentAsync()
    {
        var payment = await _paymentService.ProcessPaymentAsync(
            Data.OrderData.CustomerId,
            Data.OrderData.TotalAmount);
        
        Data.PaymentProcessed = true;
        Data.PaymentId = payment.Id;
        await _stateStore.SaveStateAsync(Id, State, Data);
    }

    private async Task ConfirmOrderAsync()
    {
        await _orderService.ConfirmOrderAsync(Data.OrderId);
        Data.OrderConfirmed = true;
        await _stateStore.SaveStateAsync(Id, State, Data);
    }

    private async Task CancelOrderAsync()
    {
        await _orderService.CancelOrderAsync(Data.OrderId);
    }

    private async Task ReleaseInventoryAsync()
    {
        await _inventoryService.ReleaseInventoryAsync(Data.InventoryReservationId);
    }

    private async Task RefundPaymentAsync()
    {
        await _paymentService.RefundPaymentAsync(Data.PaymentId);
    }
}

public class OrderSagaData
{
    public string OrderId { get; set; }
    public bool OrderValidated { get; set; }
    public bool InventoryReserved { get; set; }
    public bool PaymentProcessed { get; set; }
    public bool OrderConfirmed { get; set; }
    public OrderData OrderData { get; set; }
    public string InventoryReservationId { get; set; }
    public string PaymentId { get; set; }
}
```

## Compensation Actions

```csharp
public interface ICompensationAction
{
    string Name { get; }
    Task ExecuteAsync(SagaData data);
    Task ValidateAsync(SagaData data);
}

public class CancelOrderCompensation : ICompensationAction
{
    public string Name => "CancelOrder";

    public async Task ExecuteAsync(SagaData data)
    {
        var orderData = (OrderSagaData)data;
        // Implementation to cancel order
    }

    public async Task ValidateAsync(SagaData data)
    {
        var orderData = (OrderSagaData)data;
        // Validate that order can be cancelled
    }
}

public class ReleaseInventoryCompensation : ICompensationAction
{
    public string Name => "ReleaseInventory";

    public async Task ExecuteAsync(SagaData data)
    {
        var orderData = (OrderSagaData)data;
        // Implementation to release inventory
    }

    public async Task ValidateAsync(SagaData data)
    {
        var orderData = (OrderSagaData)data;
        // Validate that inventory can be released
    }
}
```

## Saga State Management

```csharp
public interface ISagaStateStore
{
    Task SaveStateAsync(string sagaId, SagaState state, SagaData data);
    Task<(SagaState state, SagaData data)> LoadStateAsync(string sagaId);
    Task<List<SagaState>> GetSagaStatesAsync(string sagaId);
}

public class SagaStateStore : ISagaStateStore
{
    private readonly IConfiguration _config;
    private readonly IDbConnection _connection;

    public SagaStateStore(IConfiguration config, IDbConnection connection)
    {
        _config = config;
        _connection = connection;
    }

    public async Task SaveStateAsync(string sagaId, SagaState state, SagaData data)
    {
        var sql = @"
            INSERT INTO saga_states (saga_id, state, data, created_at)
            VALUES (@SagaId, @State, @Data, @CreatedAt)";

        await _connection.ExecuteAsync(sql, new
        {
            SagaId = sagaId,
            State = state.ToString(),
            Data = JsonSerializer.Serialize(data),
            CreatedAt = DateTime.UtcNow
        });
    }

    public async Task<(SagaState state, SagaData data)> LoadStateAsync(string sagaId)
    {
        var sql = @"
            SELECT state, data 
            FROM saga_states 
            WHERE saga_id = @SagaId 
            ORDER BY created_at DESC 
            LIMIT 1";

        var result = await _connection.QueryFirstOrDefaultAsync(sql, new { SagaId = sagaId });
        
        if (result == null)
            return (SagaState.Started, null);

        var state = Enum.Parse<SagaState>(result.state);
        var data = JsonSerializer.Deserialize<SagaData>(result.data);
        
        return (state, data);
    }

    public async Task<List<SagaState>> GetSagaStatesAsync(string sagaId)
    {
        var sql = @"
            SELECT state 
            FROM saga_states 
            WHERE saga_id = @SagaId 
            ORDER BY created_at";

        var results = await _connection.QueryAsync<string>(sql, new { SagaId = sagaId });
        return results.Select(r => Enum.Parse<SagaState>(r)).ToList();
    }
}
```

## Monitoring & Recovery

```csharp
public class SagaMonitor
{
    private readonly IConfiguration _config;
    private readonly ISagaStateStore _stateStore;
    private readonly ILogger<SagaMonitor> _logger;

    public SagaMonitor(
        IConfiguration config,
        ISagaStateStore stateStore,
        ILogger<SagaMonitor> logger)
    {
        _config = config;
        _stateStore = stateStore;
        _logger = logger;
    }

    public async Task MonitorSagasAsync()
    {
        if (!bool.Parse(_config["monitoring:saga_tracking_enabled"]))
            return;

        // Find stuck sagas
        var stuckSagas = await FindStuckSagasAsync();
        
        foreach (var saga in stuckSagas)
        {
            await HandleStuckSagaAsync(saga);
        }
    }

    private async Task<List<string>> FindStuckSagasAsync()
    {
        // Implementation to find sagas that are stuck
        return new List<string>();
    }

    private async Task HandleStuckSagaAsync(string sagaId)
    {
        var (state, data) = await _stateStore.LoadStateAsync(sagaId);
        
        if (state == SagaState.InProgress)
        {
            // Check if saga has timed out
            var timeout = TimeSpan.FromMinutes(
                int.Parse(_config["saga:timeout"]));
            
            // Implementation to handle timeout
            _logger.LogWarning("Saga {SagaId} has timed out", sagaId);
        }
    }
}
```

## Best Practices

1. **Design compensating actions for all saga steps**
2. **Ensure idempotency of all operations**
3. **Use timeouts to prevent stuck sagas**
4. **Implement proper monitoring and alerting**
5. **Store saga state for recovery**
6. **Handle partial failures gracefully**
7. **Test saga scenarios thoroughly**

## Conclusion

Saga patterns with C# and TuskLang enable building reliable, distributed applications that can handle complex business transactions across multiple services. By leveraging TuskLang for configuration and saga management, you can create systems that maintain data consistency and provide robust error handling. 