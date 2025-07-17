# Clean Architecture in C# with TuskLang

## Overview

Clean Architecture is a software design philosophy that emphasizes separation of concerns and dependency inversion. This guide covers how to implement Clean Architecture using C# and TuskLang configuration for building maintainable, testable, and scalable applications.

## Table of Contents

- [Clean Architecture Concepts](#clean-architecture-concepts)
- [TuskLang Clean Architecture Configuration](#tusklang-clean-architecture-configuration)
- [Layers](#layers)
- [C# Clean Architecture Example](#c-clean-architecture-example)
- [Dependency Inversion](#dependency-inversion)
- [Use Cases](#use-cases)
- [Best Practices](#best-practices)

## Clean Architecture Concepts

- **Entities**: Core business objects
- **Use Cases**: Application-specific business rules
- **Interface Adapters**: Controllers, presenters, and gateways
- **Frameworks & Drivers**: External frameworks, databases, and tools
- **Dependency Rule**: Dependencies point inward, toward the core

## TuskLang Clean Architecture Configuration

```ini
# clean-architecture.tsk
[clean_architecture]
enabled = @env("CLEAN_ARCHITECTURE_ENABLED", "true")
project_name = @env("PROJECT_NAME", "OrderManagement")
strict_dependency_rule = @env("STRICT_DEPENDENCY_RULE", "true")

[layers]
entities = @env("ENTITIES_LAYER", "Domain")
use_cases = @env("USE_CASES_LAYER", "Application")
interface_adapters = @env("INTERFACE_ADAPTERS_LAYER", "Infrastructure")
frameworks = @env("FRAMEWORKS_LAYER", "Web")

[entities]
order_entity = @env("ORDER_ENTITY_ENABLED", "true")
user_entity = @env("USER_ENTITY_ENABLED", "true")
product_entity = @env("PRODUCT_ENTITY_ENABLED", "true")

[use_cases]
create_order = @env("CREATE_ORDER_USE_CASE", "true")
get_order = @env("GET_ORDER_USE_CASE", "true")
update_order = @env("UPDATE_ORDER_USE_CASE", "true")
cancel_order = @env("CANCEL_ORDER_USE_CASE", "true")

[repositories]
order_repository = @env("ORDER_REPOSITORY_IMPL", "SqlServer")
user_repository = @env("USER_REPOSITORY_IMPL", "SqlServer")
product_repository = @env("PRODUCT_REPOSITORY_IMPL", "SqlServer")

[external_services]
payment_service = @env("PAYMENT_SERVICE_IMPL", "Stripe")
email_service = @env("EMAIL_SERVICE_IMPL", "SendGrid")
notification_service = @env("NOTIFICATION_SERVICE_IMPL", "SignalR")
```

## Layers

```csharp
// Domain Layer (Entities)
public class Order
{
    public string Id { get; private set; }
    public string CustomerId { get; private set; }
    public List<OrderItem> Items { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTime CreatedOn { get; private set; }
    public decimal TotalAmount { get; private set; }

    public Order(string customerId, List<OrderItem> items)
    {
        Id = Guid.NewGuid().ToString();
        CustomerId = customerId;
        Items = items ?? new List<OrderItem>();
        Status = OrderStatus.Draft;
        CreatedOn = DateTime.UtcNow;
        CalculateTotal();
    }

    public void AddItem(OrderItem item)
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException("Cannot modify confirmed order");

        Items.Add(item);
        CalculateTotal();
    }

    public void Confirm()
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException("Order is not in draft status");

        if (!Items.Any())
            throw new InvalidOperationException("Cannot confirm empty order");

        Status = OrderStatus.Confirmed;
    }

    public void Cancel()
    {
        if (Status == OrderStatus.Cancelled)
            throw new InvalidOperationException("Order is already cancelled");

        Status = OrderStatus.Cancelled;
    }

    private void CalculateTotal()
    {
        TotalAmount = Items.Sum(item => item.TotalPrice);
    }
}

public class OrderItem
{
    public string ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice => Quantity * UnitPrice;
}

public enum OrderStatus
{
    Draft,
    Confirmed,
    Paid,
    Shipped,
    Delivered,
    Cancelled
}

// Application Layer (Use Cases)
public interface ICreateOrderUseCase
{
    Task<CreateOrderResponse> ExecuteAsync(CreateOrderRequest request);
}

public interface IGetOrderUseCase
{
    Task<GetOrderResponse> ExecuteAsync(GetOrderRequest request);
}

public interface IUpdateOrderUseCase
{
    Task<UpdateOrderResponse> ExecuteAsync(UpdateOrderRequest request);
}

public interface ICancelOrderUseCase
{
    Task<CancelOrderResponse> ExecuteAsync(CancelOrderRequest request);
}

public class CreateOrderUseCase : ICreateOrderUseCase
{
    private readonly IOrderRepository _orderRepository;
    private readonly IPaymentService _paymentService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _config;

    public CreateOrderUseCase(
        IOrderRepository orderRepository,
        IPaymentService paymentService,
        IEmailService emailService,
        IConfiguration config)
    {
        _orderRepository = orderRepository;
        _paymentService = paymentService;
        _emailService = emailService;
        _config = config;
    }

    public async Task<CreateOrderResponse> ExecuteAsync(CreateOrderRequest request)
    {
        // Validate request
        ValidateRequest(request);

        // Create order entity
        var orderItems = request.Items.Select(item => new OrderItem
        {
            ProductId = item.ProductId,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice
        }).ToList();

        var order = new Order(request.CustomerId, orderItems);

        // Save order
        await _orderRepository.SaveAsync(order);

        // Process payment if required
        if (request.ProcessPayment)
        {
            var paymentResult = await ProcessPayment(order);
            if (!paymentResult.Success)
            {
                throw new PaymentFailedException(paymentResult.ErrorMessage);
            }
        }

        // Send confirmation email
        if (bool.Parse(_config["use_cases:send_confirmation_email"] ?? "true"))
        {
            await _emailService.SendOrderConfirmationAsync(request.CustomerEmail, order);
        }

        return new CreateOrderResponse
        {
            OrderId = order.Id,
            Status = order.Status.ToString(),
            TotalAmount = order.TotalAmount
        };
    }

    private void ValidateRequest(CreateOrderRequest request)
    {
        if (string.IsNullOrEmpty(request.CustomerId))
            throw new ValidationException("Customer ID is required");

        if (!request.Items.Any())
            throw new ValidationException("Order must contain at least one item");

        foreach (var item in request.Items)
        {
            if (string.IsNullOrEmpty(item.ProductId))
                throw new ValidationException("Product ID is required for all items");

            if (item.Quantity <= 0)
                throw new ValidationException("Quantity must be positive");

            if (item.UnitPrice <= 0)
                throw new ValidationException("Unit price must be positive");
        }
    }

    private async Task<PaymentResult> ProcessPayment(Order order)
    {
        var paymentRequest = new PaymentRequest
        {
            OrderId = order.Id,
            Amount = order.TotalAmount,
            CustomerId = order.CustomerId
        };

        return await _paymentService.ProcessPaymentAsync(paymentRequest);
    }
}

// Infrastructure Layer (Repositories and External Services)
public interface IOrderRepository
{
    Task<Order> GetByIdAsync(string orderId);
    Task<Order> SaveAsync(Order order);
    Task<bool> UpdateAsync(Order order);
    Task<bool> DeleteAsync(string orderId);
}

public interface IPaymentService
{
    Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request);
    Task<bool> RefundPaymentAsync(string paymentId);
}

public interface IEmailService
{
    Task<bool> SendOrderConfirmationAsync(string customerEmail, Order order);
    Task<bool> SendOrderCancellationAsync(string customerEmail, Order order);
}

public class SqlServerOrderRepository : IOrderRepository
{
    private readonly IDbConnection _connection;
    private readonly IConfiguration _config;

    public SqlServerOrderRepository(IDbConnection connection, IConfiguration config)
    {
        _connection = connection;
        _config = config;
    }

    public async Task<Order> GetByIdAsync(string orderId)
    {
        var sql = "SELECT * FROM Orders WHERE Id = @OrderId";
        var orderData = await _connection.QueryFirstOrDefaultAsync<OrderData>(sql, new { OrderId = orderId });
        
        if (orderData == null)
            return null;

        return MapToOrder(orderData);
    }

    public async Task<Order> SaveAsync(Order order)
    {
        var sql = @"
            INSERT INTO Orders (Id, CustomerId, Status, CreatedOn, TotalAmount)
            VALUES (@Id, @CustomerId, @Status, @CreatedOn, @TotalAmount)";

        await _connection.ExecuteAsync(sql, new
        {
            order.Id,
            order.CustomerId,
            Status = order.Status.ToString(),
            order.CreatedOn,
            order.TotalAmount
        });

        return order;
    }

    public async Task<bool> UpdateAsync(Order order)
    {
        var sql = @"
            UPDATE Orders 
            SET Status = @Status, TotalAmount = @TotalAmount
            WHERE Id = @Id";

        var rowsAffected = await _connection.ExecuteAsync(sql, new
        {
            order.Id,
            Status = order.Status.ToString(),
            order.TotalAmount
        });

        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(string orderId)
    {
        var sql = "DELETE FROM Orders WHERE Id = @OrderId";
        var rowsAffected = await _connection.ExecuteAsync(sql, new { OrderId = orderId });
        return rowsAffected > 0;
    }

    private Order MapToOrder(OrderData orderData)
    {
        // Implementation to map database data to domain entity
        return new Order(orderData.CustomerId, new List<OrderItem>())
        {
            Id = orderData.Id,
            Status = Enum.Parse<OrderStatus>(orderData.Status),
            CreatedOn = orderData.CreatedOn,
            TotalAmount = orderData.TotalAmount
        };
    }
}

// Web Layer (Controllers)
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ICreateOrderUseCase _createOrderUseCase;
    private readonly IGetOrderUseCase _getOrderUseCase;
    private readonly IUpdateOrderUseCase _updateOrderUseCase;
    private readonly ICancelOrderUseCase _cancelOrderUseCase;

    public OrdersController(
        ICreateOrderUseCase createOrderUseCase,
        IGetOrderUseCase getOrderUseCase,
        IUpdateOrderUseCase updateOrderUseCase,
        ICancelOrderUseCase cancelOrderUseCase)
    {
        _createOrderUseCase = createOrderUseCase;
        _getOrderUseCase = getOrderUseCase;
        _updateOrderUseCase = updateOrderUseCase;
        _cancelOrderUseCase = cancelOrderUseCase;
    }

    [HttpPost]
    public async Task<ActionResult<CreateOrderResponse>> CreateOrder([FromBody] CreateOrderRequest request)
    {
        try
        {
            var response = await _createOrderUseCase.ExecuteAsync(request);
            return CreatedAtAction(nameof(GetOrder), new { orderId = response.OrderId }, response);
        }
        catch (ValidationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (PaymentFailedException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{orderId}")]
    public async Task<ActionResult<GetOrderResponse>> GetOrder(string orderId)
    {
        try
        {
            var request = new GetOrderRequest { OrderId = orderId };
            var response = await _getOrderUseCase.ExecuteAsync(request);
            return Ok(response);
        }
        catch (NotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPut("{orderId}")]
    public async Task<ActionResult<UpdateOrderResponse>> UpdateOrder(string orderId, [FromBody] UpdateOrderRequest request)
    {
        try
        {
            var updateRequest = new UpdateOrderRequest { OrderId = orderId, Items = request.Items };
            var response = await _updateOrderUseCase.ExecuteAsync(updateRequest);
            return Ok(response);
        }
        catch (ValidationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (NotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpDelete("{orderId}")]
    public async Task<ActionResult<CancelOrderResponse>> CancelOrder(string orderId)
    {
        try
        {
            var request = new CancelOrderRequest { OrderId = orderId };
            var response = await _cancelOrderUseCase.ExecuteAsync(request);
            return Ok(response);
        }
        catch (NotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
}
```

## Dependency Inversion

```csharp
public static class CleanArchitectureServiceCollectionExtensions
{
    public static IServiceCollection AddCleanArchitecture(
        this IServiceCollection services,
        IConfiguration config)
    {
        if (!bool.Parse(config["clean_architecture:enabled"]))
            return services;

        // Register use cases
        services.AddScoped<ICreateOrderUseCase, CreateOrderUseCase>();
        services.AddScoped<IGetOrderUseCase, GetOrderUseCase>();
        services.AddScoped<IUpdateOrderUseCase, UpdateOrderUseCase>();
        services.AddScoped<ICancelOrderUseCase, CancelOrderUseCase>();

        // Register repositories based on configuration
        RegisterRepositories(services, config);

        // Register external services based on configuration
        RegisterExternalServices(services, config);

        return services;
    }

    private static void RegisterRepositories(IServiceCollection services, IConfiguration config)
    {
        var orderRepoImpl = config["repositories:order_repository"];
        var userRepoImpl = config["repositories:user_repository"];
        var productRepoImpl = config["repositories:product_repository"];

        switch (orderRepoImpl.ToLower())
        {
            case "sqlserver":
                services.AddScoped<IOrderRepository, SqlServerOrderRepository>();
                break;
            case "mongodb":
                services.AddScoped<IOrderRepository, MongoDbOrderRepository>();
                break;
            case "inmemory":
                services.AddScoped<IOrderRepository, InMemoryOrderRepository>();
                break;
            default:
                throw new NotSupportedException($"Order repository implementation {orderRepoImpl} is not supported");
        }

        // Similar registration for user and product repositories
    }

    private static void RegisterExternalServices(IServiceCollection services, IConfiguration config)
    {
        var paymentServiceImpl = config["external_services:payment_service"];
        var emailServiceImpl = config["external_services:email_service"];
        var notificationServiceImpl = config["external_services:notification_service"];

        switch (paymentServiceImpl.ToLower())
        {
            case "stripe":
                services.AddScoped<IPaymentService, StripePaymentService>();
                break;
            case "paypal":
                services.AddScoped<IPaymentService, PayPalPaymentService>();
                break;
            case "mock":
                services.AddScoped<IPaymentService, MockPaymentService>();
                break;
            default:
                throw new NotSupportedException($"Payment service implementation {paymentServiceImpl} is not supported");
        }

        // Similar registration for email and notification services
    }
}
```

## Use Cases

```csharp
// Request/Response DTOs
public class CreateOrderRequest
{
    public string CustomerId { get; set; }
    public string CustomerEmail { get; set; }
    public List<OrderItemRequest> Items { get; set; } = new();
    public bool ProcessPayment { get; set; }
}

public class CreateOrderResponse
{
    public string OrderId { get; set; }
    public string Status { get; set; }
    public decimal TotalAmount { get; set; }
}

public class GetOrderRequest
{
    public string OrderId { get; set; }
}

public class GetOrderResponse
{
    public string OrderId { get; set; }
    public string CustomerId { get; set; }
    public List<OrderItemResponse> Items { get; set; } = new();
    public string Status { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedOn { get; set; }
}

public class OrderItemRequest
{
    public string ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class OrderItemResponse
{
    public string ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class PaymentRequest
{
    public string OrderId { get; set; }
    public decimal Amount { get; set; }
    public string CustomerId { get; set; }
}

public class PaymentResult
{
    public bool Success { get; set; }
    public string PaymentId { get; set; }
    public string ErrorMessage { get; set; }
}
```

## Best Practices

1. **Follow the dependency rule strictly**
2. **Keep entities independent of frameworks**
3. **Use interfaces for all external dependencies**
4. **Test use cases in isolation**
5. **Keep controllers thin**
6. **Use DTOs for data transfer**
7. **Implement proper error handling**

## Conclusion

Clean Architecture with C# and TuskLang enables building applications that are independent of frameworks and easily testable. By leveraging TuskLang for configuration and Clean Architecture patterns, you can create systems that are maintainable, extensible, and aligned with business requirements. 