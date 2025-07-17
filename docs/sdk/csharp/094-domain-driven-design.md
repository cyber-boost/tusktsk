# Domain-Driven Design in C# with TuskLang

## Overview

Domain-Driven Design (DDD) is an approach to software development that focuses on modeling software to match a domain according to input from domain experts. This guide covers how to implement DDD patterns using C# and TuskLang configuration for building complex, domain-rich applications.

## Table of Contents

- [DDD Concepts](#ddd-concepts)
- [TuskLang DDD Configuration](#tusklang-ddd-configuration)
- [Bounded Contexts](#bounded-contexts)
- [C# DDD Example](#c-ddd-example)
- [Aggregates](#aggregates)
- [Domain Services](#domain-services)
- [Value Objects](#value-objects)
- [Best Practices](#best-practices)

## DDD Concepts

- **Bounded Context**: A boundary within which a particular model is defined and applicable
- **Aggregate**: A cluster of domain objects that can be treated as a single unit
- **Entity**: Objects with identity that persist over time
- **Value Object**: Objects defined by their attributes rather than identity
- **Domain Service**: Services that implement domain logic that doesn't belong to entities or value objects
- **Repository**: Abstraction for storing and retrieving aggregates

## TuskLang DDD Configuration

```ini
# ddd.tsk
[ddd]
enabled = @env("DDD_ENABLED", "true")
bounded_contexts = @env("BOUNDED_CONTEXTS", "order,user,product")
aggregate_consistency = @env("AGGREGATE_CONSISTENCY", "strong")
event_sourcing_enabled = @env("EVENT_SOURCING_ENABLED", "true")

[bounded_contexts.order]
name = "Order Management"
aggregates = ["Order", "OrderLine"]
domain_services = ["OrderCalculationService", "OrderValidationService"]
repositories = ["IOrderRepository"]

[bounded_contexts.user]
name = "User Management"
aggregates = ["User", "UserProfile"]
domain_services = ["UserRegistrationService", "UserAuthenticationService"]
repositories = ["IUserRepository"]

[bounded_contexts.product]
name = "Product Catalog"
aggregates = ["Product", "Category"]
domain_services = ["ProductPricingService", "InventoryService"]
repositories = ["IProductRepository"]

[domain_events]
enabled = @env("DOMAIN_EVENTS_ENABLED", "true")
event_store_connection = @env.secure("DOMAIN_EVENT_STORE_CONNECTION")
```

## Bounded Contexts

```csharp
public interface IBoundedContext
{
    string Name { get; }
    IEnumerable<Type> Aggregates { get; }
    IEnumerable<Type> DomainServices { get; }
    IEnumerable<Type> Repositories { get; }
}

public class OrderBoundedContext : IBoundedContext
{
    public string Name => "Order Management";
    
    public IEnumerable<Type> Aggregates => new[]
    {
        typeof(Order),
        typeof(OrderLine)
    };
    
    public IEnumerable<Type> DomainServices => new[]
    {
        typeof(OrderCalculationService),
        typeof(OrderValidationService)
    };
    
    public IEnumerable<Type> Repositories => new[]
    {
        typeof(IOrderRepository)
    };
}
```

## C# DDD Example

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using TuskLang;

// Value Objects
public record Money(decimal Amount, string Currency)
{
    public static Money Zero(string currency = "USD") => new(0, currency);
    
    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot add different currencies");
        return new(Amount + other.Amount, Currency);
    }
    
    public Money Multiply(decimal factor) => new(Amount * factor, Currency);
}

public record Address(string Street, string City, string State, string ZipCode, string Country)
{
    public bool IsValid => !string.IsNullOrEmpty(Street) && 
                          !string.IsNullOrEmpty(City) && 
                          !string.IsNullOrEmpty(Country);
}

// Entities
public class Order : AggregateRoot
{
    private readonly List<OrderLine> _orderLines;
    private readonly List<IDomainEvent> _domainEvents;

    public string Id { get; private set; }
    public string CustomerId { get; private set; }
    public Address ShippingAddress { get; private set; }
    public OrderStatus Status { get; private set; }
    public Money TotalAmount { get; private set; }
    public DateTime CreatedOn { get; private set; }
    public DateTime? UpdatedOn { get; private set; }

    public IReadOnlyCollection<OrderLine> OrderLines => _orderLines.AsReadOnly();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private Order()
    {
        _orderLines = new List<OrderLine>();
        _domainEvents = new List<IDomainEvent>();
    }

    public static Order Create(string customerId, Address shippingAddress)
    {
        var order = new Order
        {
            Id = Guid.NewGuid().ToString(),
            CustomerId = customerId,
            ShippingAddress = shippingAddress,
            Status = OrderStatus.Draft,
            TotalAmount = Money.Zero(),
            CreatedOn = DateTime.UtcNow
        };

        order.AddDomainEvent(new OrderCreatedEvent(order.Id, customerId));
        return order;
    }

    public void AddOrderLine(string productId, int quantity, Money unitPrice)
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException("Cannot modify confirmed order");

        var orderLine = OrderLine.Create(productId, quantity, unitPrice);
        _orderLines.Add(orderLine);

        RecalculateTotal();
        AddDomainEvent(new OrderLineAddedEvent(Id, productId, quantity, unitPrice));
    }

    public void RemoveOrderLine(string productId)
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException("Cannot modify confirmed order");

        var orderLine = _orderLines.FirstOrDefault(ol => ol.ProductId == productId);
        if (orderLine != null)
        {
            _orderLines.Remove(orderLine);
            RecalculateTotal();
            AddDomainEvent(new OrderLineRemovedEvent(Id, productId));
        }
    }

    public void Confirm()
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException("Order is not in draft status");

        if (!_orderLines.Any())
            throw new InvalidOperationException("Cannot confirm empty order");

        if (!ShippingAddress.IsValid)
            throw new InvalidOperationException("Invalid shipping address");

        Status = OrderStatus.Confirmed;
        UpdatedOn = DateTime.UtcNow;
        AddDomainEvent(new OrderConfirmedEvent(Id, CustomerId, TotalAmount));
    }

    public void Cancel()
    {
        if (Status == OrderStatus.Cancelled)
            throw new InvalidOperationException("Order is already cancelled");

        Status = OrderStatus.Cancelled;
        UpdatedOn = DateTime.UtcNow;
        AddDomainEvent(new OrderCancelledEvent(Id, CustomerId));
    }

    private void RecalculateTotal()
    {
        TotalAmount = _orderLines.Aggregate(Money.Zero(), (total, line) => total.Add(line.TotalPrice));
    }

    private void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

public class OrderLine : Entity
{
    public string ProductId { get; private set; }
    public int Quantity { get; private set; }
    public Money UnitPrice { get; private set; }
    public Money TotalPrice { get; private set; }

    private OrderLine() { }

    public static OrderLine Create(string productId, int quantity, Money unitPrice)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive", nameof(quantity));

        if (unitPrice.Amount <= 0)
            throw new ArgumentException("Unit price must be positive", nameof(unitPrice));

        return new OrderLine
        {
            ProductId = productId,
            Quantity = quantity,
            UnitPrice = unitPrice,
            TotalPrice = unitPrice.Multiply(quantity)
        };
    }

    public void UpdateQuantity(int newQuantity)
    {
        if (newQuantity <= 0)
            throw new ArgumentException("Quantity must be positive", nameof(newQuantity));

        Quantity = newQuantity;
        TotalPrice = UnitPrice.Multiply(newQuantity);
    }
}

public enum OrderStatus
{
    Draft,
    Confirmed,
    Shipped,
    Delivered,
    Cancelled
}
```

## Aggregates

```csharp
public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents;

    protected AggregateRoot()
    {
        _domainEvents = new List<IDomainEvent>();
    }

    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

public abstract class Entity
{
    public string Id { get; protected set; }

    protected Entity()
    {
        Id = Guid.NewGuid().ToString();
    }

    public override bool Equals(object obj)
    {
        if (obj is not Entity other)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return Id == other.Id;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public static bool operator ==(Entity left, Entity right)
    {
        return left?.Equals(right) ?? right is null;
    }

    public static bool operator !=(Entity left, Entity right)
    {
        return !(left == right);
    }
}

public interface IDomainEvent
{
    string Id { get; }
    DateTime OccurredOn { get; }
    string AggregateId { get; }
}

public class OrderCreatedEvent : IDomainEvent
{
    public string Id { get; }
    public DateTime OccurredOn { get; }
    public string AggregateId { get; }
    public string CustomerId { get; }

    public OrderCreatedEvent(string orderId, string customerId)
    {
        Id = Guid.NewGuid().ToString();
        OccurredOn = DateTime.UtcNow;
        AggregateId = orderId;
        CustomerId = customerId;
    }
}
```

## Domain Services

```csharp
public interface IDomainService
{
    string Name { get; }
}

public class OrderCalculationService : IDomainService
{
    public string Name => "OrderCalculationService";

    public Money CalculateOrderTotal(Order order)
    {
        return order.OrderLines.Aggregate(Money.Zero(), (total, line) => total.Add(line.TotalPrice));
    }

    public Money CalculateTax(Order order, decimal taxRate)
    {
        var subtotal = CalculateOrderTotal(order);
        return subtotal.Multiply(taxRate / 100m);
    }

    public Money CalculateShippingCost(Order order, Money baseShippingCost)
    {
        var totalWeight = order.OrderLines.Sum(line => line.Quantity); // Assuming weight = quantity for simplicity
        var weightMultiplier = totalWeight > 10 ? 1.5m : 1.0m;
        return baseShippingCost.Multiply(weightMultiplier);
    }
}

public class OrderValidationService : IDomainService
{
    public string Name => "OrderValidationService";

    public ValidationResult ValidateOrder(Order order)
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(order.CustomerId))
            errors.Add("Customer ID is required");

        if (!order.ShippingAddress.IsValid)
            errors.Add("Valid shipping address is required");

        if (!order.OrderLines.Any())
            errors.Add("Order must contain at least one item");

        foreach (var orderLine in order.OrderLines)
        {
            if (orderLine.Quantity <= 0)
                errors.Add($"Invalid quantity for product {orderLine.ProductId}");

            if (orderLine.UnitPrice.Amount <= 0)
                errors.Add($"Invalid unit price for product {orderLine.ProductId}");
        }

        return errors.Any() 
            ? ValidationResult.Failure(errors)
            : ValidationResult.Success();
    }
}

public class ValidationResult
{
    public bool IsSuccess { get; }
    public List<string> Errors { get; }

    private ValidationResult(bool isSuccess, List<string> errors)
    {
        IsSuccess = isSuccess;
        Errors = errors ?? new List<string>();
    }

    public static ValidationResult Success() => new(true, null);
    public static ValidationResult Failure(List<string> errors) => new(false, errors);
}
```

## Value Objects

```csharp
public abstract class ValueObject<T> where T : ValueObject<T>
{
    protected abstract IEnumerable<object> GetEqualityComponents();

    public override bool Equals(object obj)
    {
        if (obj is not T other)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Select(x => x?.GetHashCode() ?? 0)
            .Aggregate((x, y) => x ^ y);
    }

    public static bool operator ==(ValueObject<T> left, ValueObject<T> right)
    {
        return left?.Equals(right) ?? right is null;
    }

    public static bool operator !=(ValueObject<T> left, ValueObject<T> right)
    {
        return !(left == right);
    }
}

public class EmailAddress : ValueObject<EmailAddress>
{
    public string Value { get; }

    public EmailAddress(string value)
    {
        if (string.IsNullOrEmpty(value))
            throw new ArgumentException("Email address cannot be empty", nameof(value));

        if (!IsValidEmail(value))
            throw new ArgumentException("Invalid email address format", nameof(value));

        Value = value.ToLowerInvariant();
    }

    private static bool IsValidEmail(string email)
    {
        return email.Contains("@") && email.Contains(".") && email.Length > 5;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}

public class PhoneNumber : ValueObject<PhoneNumber>
{
    public string Value { get; }

    public PhoneNumber(string value)
    {
        if (string.IsNullOrEmpty(value))
            throw new ArgumentException("Phone number cannot be empty", nameof(value));

        Value = NormalizePhoneNumber(value);
    }

    private static string NormalizePhoneNumber(string phoneNumber)
    {
        return new string(phoneNumber.Where(char.IsDigit).ToArray());
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
```

## Best Practices

1. **Define clear bounded contexts**
2. **Use aggregates to maintain consistency boundaries**
3. **Implement value objects for immutable concepts**
4. **Use domain services for complex business logic**
5. **Publish domain events for side effects**
6. **Keep aggregates small and focused**
7. **Use repositories for aggregate persistence**

## Conclusion

Domain-Driven Design with C# and TuskLang enables building complex, domain-rich applications that accurately model business requirements. By leveraging TuskLang for configuration and DDD patterns, you can create systems that are maintainable, extensible, and aligned with business needs. 