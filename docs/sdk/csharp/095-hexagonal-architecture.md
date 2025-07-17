# Hexagonal Architecture in C# with TuskLang

## Overview

Hexagonal Architecture (also known as Ports and Adapters) is an architectural pattern that allows an application to be driven by users, programs, automated tests, or batch scripts, and to be developed and tested in isolation from its eventual run-time devices and databases. This guide covers how to implement hexagonal architecture using C# and TuskLang configuration.

## Table of Contents

- [Hexagonal Architecture Concepts](#hexagonal-architecture-concepts)
- [TuskLang Hexagonal Configuration](#tusklang-hexagonal-configuration)
- [Ports and Adapters](#ports-and-adapters)
- [C# Hexagonal Example](#c-hexagonal-example)
- [Dependency Injection](#dependency-injection)
- [Testing Strategy](#testing-strategy)
- [Best Practices](#best-practices)

## Hexagonal Architecture Concepts

- **Core Domain**: The business logic at the center of the application
- **Ports**: Interfaces that define how the core communicates with the outside world
- **Adapters**: Implementations that connect the core to external systems
- **Inbound Ports**: Interfaces for incoming requests (use cases)
- **Outbound Ports**: Interfaces for outgoing requests (repositories, external services)

## TuskLang Hexagonal Configuration

```ini
# hexagonal.tsk
[hexagonal]
enabled = @env("HEXAGONAL_ENABLED", "true")
core_domain_name = @env("CORE_DOMAIN_NAME", "OrderManagement")
inbound_ports = @env("INBOUND_PORTS", "OrderService,UserService")
outbound_ports = @env("OUTBOUND_PORTS", "OrderRepository,PaymentService,EmailService")

[inbound_ports.order_service]
name = "OrderService"
use_cases = ["CreateOrder", "GetOrder", "UpdateOrder", "CancelOrder"]
validation_enabled = @env("ORDER_VALIDATION_ENABLED", "true")

[outbound_ports.order_repository]
name = "OrderRepository"
implementation = @env("ORDER_REPOSITORY_IMPL", "SqlServer")
connection_string = @env.secure("ORDER_DB_CONNECTION")

[outbound_ports.payment_service]
name = "PaymentService"
implementation = @env("PAYMENT_SERVICE_IMPL", "Stripe")
api_key = @env.secure("STRIPE_API_KEY")

[outbound_ports.email_service]
name = "EmailService"
implementation = @env("EMAIL_SERVICE_IMPL", "SendGrid")
api_key = @env.secure("SENDGRID_API_KEY")

[testing]
mock_adapters_enabled = @env("MOCK_ADAPTERS_ENABLED", "true")
test_database_enabled = @env("TEST_DATABASE_ENABLED", "true")
```

## Ports and Adapters

```csharp
// Core Domain - Entities
public class Order
{
    public string Id { get; set; }
    public string CustomerId { get; set; }
    public List<OrderItem> Items { get; set; } = new();
    public OrderStatus Status { get; set; }
    public DateTime CreatedOn { get; set; }
    public decimal TotalAmount { get; set; }
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

// Inbound Ports (Use Cases)
public interface ICreateOrderUseCase
{
    Task<Order> ExecuteAsync(CreateOrderRequest request);
}

public interface IGetOrderUseCase
{
    Task<Order> ExecuteAsync(string orderId);
}

public interface IUpdateOrderUseCase
{
    Task<Order> ExecuteAsync(string orderId, UpdateOrderRequest request);
}

public interface ICancelOrderUseCase
{
    Task<bool> ExecuteAsync(string orderId);
}

// Outbound Ports (Repositories and External Services)
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
```

## C# Hexagonal Example

```csharp
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using TuskLang;

// Core Domain - Use Case Implementations
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

    public async Task<Order> ExecuteAsync(CreateOrderRequest request)
    {
        // Validate request
        if (!bool.Parse(_config["inbound_ports:order_service:validation_enabled"]))
        {
            ValidateCreateOrderRequest(request);
        }

        // Create order
        var order = new Order
        {
            Id = Guid.NewGuid().ToString(),
            CustomerId = request.CustomerId,
            Items = request.Items.Select(item => new OrderItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            }).ToList(),
            Status = OrderStatus.Draft,
            CreatedOn = DateTime.UtcNow
        };

        // Calculate total
        order.TotalAmount = order.Items.Sum(item => item.TotalPrice);

        // Save order
        await _orderRepository.SaveAsync(order);

        // Process payment if required
        if (request.ProcessPayment)
        {
            var paymentRequest = new PaymentRequest
            {
                OrderId = order.Id,
                Amount = order.TotalAmount,
                CustomerId = order.CustomerId
            };

            var paymentResult = await _paymentService.ProcessPaymentAsync(paymentRequest);
            
            if (paymentResult.Success)
            {
                order.Status = OrderStatus.Paid;
                await _orderRepository.UpdateAsync(order);
                
                // Send confirmation email
                await _emailService.SendOrderConfirmationAsync(request.CustomerEmail, order);
            }
            else
            {
                throw new PaymentFailedException(paymentResult.ErrorMessage);
            }
        }

        return order;
    }

    private void ValidateCreateOrderRequest(CreateOrderRequest request)
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
}

public class GetOrderUseCase : IGetOrderUseCase
{
    private readonly IOrderRepository _orderRepository;

    public GetOrderUseCase(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<Order> ExecuteAsync(string orderId)
    {
        if (string.IsNullOrEmpty(orderId))
            throw new ValidationException("Order ID is required");

        var order = await _orderRepository.GetByIdAsync(orderId);
        
        if (order == null)
            throw new NotFoundException($"Order {orderId} not found");

        return order;
    }
}

// Request/Response DTOs
public class CreateOrderRequest
{
    public string CustomerId { get; set; }
    public string CustomerEmail { get; set; }
    public List<OrderItemRequest> Items { get; set; } = new();
    public bool ProcessPayment { get; set; }
}

public class OrderItemRequest
{
    public string ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class UpdateOrderRequest
{
    public List<OrderItemRequest> Items { get; set; } = new();
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

## Dependency Injection

```csharp
public static class HexagonalServiceCollectionExtensions
{
    public static IServiceCollection AddHexagonalArchitecture(
        this IServiceCollection services, 
        IConfiguration config)
    {
        if (!bool.Parse(config["hexagonal:enabled"]))
            return services;

        // Register use cases (inbound ports)
        services.AddScoped<ICreateOrderUseCase, CreateOrderUseCase>();
        services.AddScoped<IGetOrderUseCase, GetOrderUseCase>();
        services.AddScoped<IUpdateOrderUseCase, UpdateOrderUseCase>();
        services.AddScoped<ICancelOrderUseCase, CancelOrderUseCase>();

        // Register adapters based on configuration
        RegisterOrderRepository(services, config);
        RegisterPaymentService(services, config);
        RegisterEmailService(services, config);

        return services;
    }

    private static void RegisterOrderRepository(IServiceCollection services, IConfiguration config)
    {
        var implementation = config["outbound_ports:order_repository:implementation"];
        
        switch (implementation.ToLower())
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
                throw new NotSupportedException($"Order repository implementation {implementation} is not supported");
        }
    }

    private static void RegisterPaymentService(IServiceCollection services, IConfiguration config)
    {
        var implementation = config["outbound_ports:payment_service:implementation"];
        
        switch (implementation.ToLower())
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
                throw new NotSupportedException($"Payment service implementation {implementation} is not supported");
        }
    }

    private static void RegisterEmailService(IServiceCollection services, IConfiguration config)
    {
        var implementation = config["outbound_ports:email_service:implementation"];
        
        switch (implementation.ToLower())
        {
            case "sendgrid":
                services.AddScoped<IEmailService, SendGridEmailService>();
                break;
            case "smtp":
                services.AddScoped<IEmailService, SmtpEmailService>();
                break;
            case "mock":
                services.AddScoped<IEmailService, MockEmailService>();
                break;
            default:
                throw new NotSupportedException($"Email service implementation {implementation} is not supported");
        }
    }
}
```

## Testing Strategy

```csharp
[TestClass]
public class CreateOrderUseCaseTests
{
    private ICreateOrderUseCase _useCase;
    private Mock<IOrderRepository> _orderRepository;
    private Mock<IPaymentService> _paymentService;
    private Mock<IEmailService> _emailService;
    private IConfiguration _config;

    [TestInitialize]
    public void Setup()
    {
        _orderRepository = new Mock<IOrderRepository>();
        _paymentService = new Mock<IPaymentService>();
        _emailService = new Mock<IEmailService>();
        
        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["hexagonal:enabled"] = "true",
                ["inbound_ports:order_service:validation_enabled"] = "true"
            })
            .Build();

        _useCase = new CreateOrderUseCase(
            _orderRepository.Object,
            _paymentService.Object,
            _emailService.Object,
            _config);
    }

    [TestMethod]
    public async Task ExecuteAsync_ValidRequest_ReturnsOrder()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            CustomerId = "customer-1",
            CustomerEmail = "test@example.com",
            Items = new List<OrderItemRequest>
            {
                new() { ProductId = "product-1", Quantity = 2, UnitPrice = 10.00m }
            },
            ProcessPayment = false
        };

        _orderRepository.Setup(r => r.SaveAsync(It.IsAny<Order>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("customer-1", result.CustomerId);
        Assert.AreEqual(OrderStatus.Draft, result.Status);
        Assert.AreEqual(20.00m, result.TotalAmount);
        
        _orderRepository.Verify(r => r.SaveAsync(It.IsAny<Order>()), Times.Once);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithPayment_ProcessesPaymentAndSendsEmail()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            CustomerId = "customer-1",
            CustomerEmail = "test@example.com",
            Items = new List<OrderItemRequest>
            {
                new() { ProductId = "product-1", Quantity = 1, UnitPrice = 10.00m }
            },
            ProcessPayment = true
        };

        _orderRepository.Setup(r => r.SaveAsync(It.IsAny<Order>()))
            .Returns(Task.CompletedTask);
        _orderRepository.Setup(r => r.UpdateAsync(It.IsAny<Order>()))
            .ReturnsAsync(true);

        _paymentService.Setup(p => p.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
            .ReturnsAsync(new PaymentResult { Success = true, PaymentId = "payment-1" });

        _emailService.Setup(e => e.SendOrderConfirmationAsync(It.IsAny<string>(), It.IsAny<Order>()))
            .ReturnsAsync(true);

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        Assert.AreEqual(OrderStatus.Paid, result.Status);
        
        _paymentService.Verify(p => p.ProcessPaymentAsync(It.IsAny<PaymentRequest>()), Times.Once);
        _emailService.Verify(e => e.SendOrderConfirmationAsync(It.IsAny<string>(), It.IsAny<Order>()), Times.Once);
    }
}
```

## Best Practices

1. **Keep the core domain independent of external concerns**
2. **Use interfaces for all external dependencies**
3. **Implement adapters for different external systems**
4. **Test the core domain in isolation**
5. **Use dependency injection for loose coupling**
6. **Keep ports focused and cohesive**
7. **Document the architecture and port contracts**

## Conclusion

Hexagonal Architecture with C# and TuskLang enables building applications that are independent of external concerns and easily testable. By leveraging TuskLang for configuration and hexagonal patterns, you can create systems that are maintainable, extensible, and aligned with business requirements. 