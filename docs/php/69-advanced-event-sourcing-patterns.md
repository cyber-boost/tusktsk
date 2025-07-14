# Advanced Event Sourcing Patterns

TuskLang enables PHP developers to build sophisticated event-sourced systems with confidence. This guide covers advanced event sourcing patterns, CQRS implementation, and event store management.

## Table of Contents
- [Event Store Architecture](#event-store-architecture)
- [Aggregate Design](#aggregate-design)
- [Event Versioning](#event-versioning)
- [Snapshot Management](#snapshot-management)
- [Projection Patterns](#projection-patterns)
- [Saga Patterns](#saga-patterns)
- [Best Practices](#best-practices)

## Event Store Architecture

```php
// config/event-store.tsk
event_store = {
    provider = "postgresql"
    connection = {
        host = "@env('DB_HOST')"
        port = "@env('DB_PORT', 5432)"
        database = "@env('DB_NAME')"
        username = "@env('DB_USER')"
        password = "@env('DB_PASSWORD')"
    }
    
    tables = {
        events = {
            name = "domain_events"
            columns = ["id", "aggregate_id", "aggregate_type", "event_type", "event_data", "version", "created_at"]
            indexes = ["aggregate_id_version", "event_type", "created_at"]
        }
        snapshots = {
            name = "aggregate_snapshots"
            columns = ["aggregate_id", "aggregate_type", "snapshot_data", "version", "created_at"]
            indexes = ["aggregate_id"]
        }
    }
    
    serialization = {
        format = "json"
        compression = true
        encryption = false
    }
    
    retention = {
        events_days = 365
        snapshots_days = 90
        archive_enabled = true
    }
}
```

## Aggregate Design

```php
<?php
// app/Domain/Aggregates/OrderAggregate.php

namespace App\Domain\Aggregates;

use TuskLang\EventSourcing\AggregateRoot;
use App\Domain\Events\OrderCreated;
use App\Domain\Events\OrderItemAdded;
use App\Domain\Events\OrderCompleted;
use App\Domain\Events\OrderCancelled;
use App\Domain\ValueObjects\OrderId;
use App\Domain\ValueObjects\Money;
use App\Domain\ValueObjects\Quantity;

class OrderAggregate extends AggregateRoot
{
    private OrderId $id;
    private string $customerId;
    private array $items = [];
    private Money $totalAmount;
    private string $status = 'pending';
    private \DateTimeImmutable $createdAt;
    
    public static function create(OrderId $id, string $customerId): self
    {
        $aggregate = new self();
        $aggregate->apply(new OrderCreated(
            $id,
            $customerId,
            new \DateTimeImmutable()
        ));
        return $aggregate;
    }
    
    public function addItem(string $productId, Quantity $quantity, Money $unitPrice): void
    {
        if ($this->status !== 'pending') {
            throw new \DomainException('Cannot add items to non-pending order');
        }
        
        $this->apply(new OrderItemAdded(
            $this->id,
            $productId,
            $quantity,
            $unitPrice,
            new \DateTimeImmutable()
        ));
    }
    
    public function complete(): void
    {
        if ($this->status !== 'pending') {
            throw new \DomainException('Order is not in pending status');
        }
        
        if (empty($this->items)) {
            throw new \DomainException('Cannot complete empty order');
        }
        
        $this->apply(new OrderCompleted(
            $this->id,
            $this->totalAmount,
            new \DateTimeImmutable()
        ));
    }
    
    public function cancel(string $reason): void
    {
        if ($this->status === 'completed') {
            throw new \DomainException('Cannot cancel completed order');
        }
        
        $this->apply(new OrderCancelled(
            $this->id,
            $reason,
            new \DateTimeImmutable()
        ));
    }
    
    protected function applyOrderCreated(OrderCreated $event): void
    {
        $this->id = $event->getOrderId();
        $this->customerId = $event->getCustomerId();
        $this->createdAt = $event->getCreatedAt();
        $this->totalAmount = new Money(0);
    }
    
    protected function applyOrderItemAdded(OrderItemAdded $event): void
    {
        $this->items[] = [
            'product_id' => $event->getProductId(),
            'quantity' => $event->getQuantity(),
            'unit_price' => $event->getUnitPrice()
        ];
        
        $this->totalAmount = $this->totalAmount->add(
            $event->getUnitPrice()->multiply($event->getQuantity())
        );
    }
    
    protected function applyOrderCompleted(OrderCompleted $event): void
    {
        $this->status = 'completed';
    }
    
    protected function applyOrderCancelled(OrderCancelled $event): void
    {
        $this->status = 'cancelled';
    }
    
    public function getId(): OrderId
    {
        return $this->id;
    }
    
    public function getCustomerId(): string
    {
        return $this->customerId;
    }
    
    public function getItems(): array
    {
        return $this->items;
    }
    
    public function getTotalAmount(): Money
    {
        return $this->totalAmount;
    }
    
    public function getStatus(): string
    {
        return $this->status;
    }
    
    public function getCreatedAt(): \DateTimeImmutable
    {
        return $this->createdAt;
    }
}
```

## Event Versioning

```php
<?php
// app/Infrastructure/EventStore/EventUpgrader.php

namespace App\Infrastructure\EventStore;

use TuskLang\EventSourcing\EventUpgraderInterface;

class EventUpgrader implements EventUpgraderInterface
{
    private array $upgraders = [];
    
    public function registerUpgrader(string $eventType, callable $upgrader): void
    {
        $this->upgraders[$eventType] = $upgrader;
    }
    
    public function upgradeEvent(array $event): array
    {
        $eventType = $event['event_type'];
        $version = $event['version'] ?? 1;
        
        if (isset($this->upgraders[$eventType])) {
            $upgrader = $this->upgraders[$eventType];
            return $upgrader($event, $version);
        }
        
        return $event;
    }
    
    public function upgradeEvents(array $events): array
    {
        return array_map([$this, 'upgradeEvent'], $events);
    }
}

// Example event upgrader registration
$upgrader = new EventUpgrader();

$upgrader->registerUpgrader('OrderCreated', function(array $event, int $version) {
    if ($version === 1) {
        // Upgrade from v1 to v2 - add shipping address
        $event['event_data']['shipping_address'] = null;
        $event['version'] = 2;
    }
    return $event;
});

$upgrader->registerUpgrader('OrderItemAdded', function(array $event, int $version) {
    if ($version === 1) {
        // Upgrade from v1 to v2 - add tax information
        $event['event_data']['tax_amount'] = 0;
        $event['version'] = 2;
    }
    return $event;
});
```

## Snapshot Management

```php
<?php
// app/Infrastructure/EventStore/SnapshotStore.php

namespace App\Infrastructure\EventStore;

use TuskLang\EventSourcing\SnapshotStoreInterface;
use TuskLang\Database\Connection;

class SnapshotStore implements SnapshotStoreInterface
{
    private Connection $connection;
    private array $config;
    
    public function __construct(Connection $connection, array $config)
    {
        $this->connection = $connection;
        $this->config = $config;
    }
    
    public function save(string $aggregateId, object $aggregate, int $version): void
    {
        $snapshotData = $this->serializeAggregate($aggregate);
        
        $this->connection->insert('aggregate_snapshots', [
            'aggregate_id' => $aggregateId,
            'aggregate_type' => get_class($aggregate),
            'snapshot_data' => $snapshotData,
            'version' => $version,
            'created_at' => new \DateTime()
        ]);
    }
    
    public function load(string $aggregateId): ?array
    {
        $result = $this->connection->select(
            "SELECT * FROM aggregate_snapshots WHERE aggregate_id = ? ORDER BY version DESC LIMIT 1",
            [$aggregateId]
        );
        
        if (empty($result)) {
            return null;
        }
        
        return [
            'state' => $this->deserializeAggregate($result[0]['snapshot_data']),
            'version' => $result[0]['version']
        ];
    }
    
    public function delete(string $aggregateId): void
    {
        $this->connection->delete('aggregate_snapshots', ['aggregate_id' => $aggregateId]);
    }
    
    public function cleanup(int $maxAge): void
    {
        $cutoffDate = new \DateTime("-{$maxAge} days");
        
        $this->connection->delete(
            'aggregate_snapshots',
            ['created_at < ?' => $cutoffDate]
        );
    }
    
    private function serializeAggregate(object $aggregate): string
    {
        return json_encode($aggregate);
    }
    
    private function deserializeAggregate(string $data): array
    {
        return json_decode($data, true);
    }
}
```

## Projection Patterns

```php
<?php
// app/Infrastructure/Projections/OrderProjection.php

namespace App\Infrastructure\Projections;

use TuskLang\EventSourcing\ProjectionInterface;
use TuskLang\Database\Connection;

class OrderProjection implements ProjectionInterface
{
    private Connection $connection;
    private array $config;
    
    public function __construct(Connection $connection, array $config)
    {
        $this->connection = $connection;
        $this->config = $config;
    }
    
    public function onOrderCreated(array $event): void
    {
        $this->connection->insert('read_models.orders', [
            'id' => $event['aggregate_id'],
            'customer_id' => $event['event_data']['customer_id'],
            'status' => 'pending',
            'total_amount' => 0,
            'item_count' => 0,
            'created_at' => $event['event_data']['created_at'],
            'updated_at' => $event['event_data']['created_at']
        ]);
    }
    
    public function onOrderItemAdded(array $event): void
    {
        $orderId = $event['aggregate_id'];
        $eventData = $event['event_data'];
        
        // Update order total
        $this->connection->update('read_models.orders', [
            'total_amount' => $this->connection->raw("total_amount + ?", [
                $eventData['unit_price'] * $eventData['quantity']
            ]),
            'item_count' => $this->connection->raw("item_count + ?", [$eventData['quantity']]),
            'updated_at' => $eventData['created_at']
        ], ['id' => $orderId]);
        
        // Add order item
        $this->connection->insert('read_models.order_items', [
            'order_id' => $orderId,
            'product_id' => $eventData['product_id'],
            'quantity' => $eventData['quantity'],
            'unit_price' => $eventData['unit_price'],
            'total_price' => $eventData['unit_price'] * $eventData['quantity'],
            'created_at' => $eventData['created_at']
        ]);
    }
    
    public function onOrderCompleted(array $event): void
    {
        $this->connection->update('read_models.orders', [
            'status' => 'completed',
            'completed_at' => $event['event_data']['created_at'],
            'updated_at' => $event['event_data']['created_at']
        ], ['id' => $event['aggregate_id']]);
    }
    
    public function onOrderCancelled(array $event): void
    {
        $this->connection->update('read_models.orders', [
            'status' => 'cancelled',
            'cancellation_reason' => $event['event_data']['reason'],
            'cancelled_at' => $event['event_data']['created_at'],
            'updated_at' => $event['event_data']['created_at']
        ], ['id' => $event['aggregate_id']]);
    }
}

// app/Infrastructure/ReadModels/OrderReadModel.php

namespace App\Infrastructure\ReadModels;

use TuskLang\Database\QueryBuilder;

class OrderReadModel
{
    private QueryBuilder $queryBuilder;
    
    public function __construct(QueryBuilder $queryBuilder)
    {
        $this->queryBuilder = $queryBuilder;
    }
    
    public function findById(string $orderId): ?array
    {
        return $this->queryBuilder
            ->table('read_models.orders')
            ->where('id', $orderId)
            ->first();
    }
    
    public function findByCustomerId(string $customerId, int $limit = 20, int $offset = 0): array
    {
        return $this->queryBuilder
            ->table('read_models.orders')
            ->where('customer_id', $customerId)
            ->orderBy('created_at', 'desc')
            ->limit($limit)
            ->offset($offset)
            ->get();
    }
    
    public function findByStatus(string $status, int $limit = 20, int $offset = 0): array
    {
        return $this->queryBuilder
            ->table('read_models.orders')
            ->where('status', $status)
            ->orderBy('created_at', 'desc')
            ->limit($limit)
            ->offset($offset)
            ->get();
    }
    
    public function getOrderItems(string $orderId): array
    {
        return $this->queryBuilder
            ->table('read_models.order_items')
            ->where('order_id', $orderId)
            ->orderBy('created_at', 'asc')
            ->get();
    }
    
    public function getOrderStatistics(): array
    {
        return $this->queryBuilder
            ->table('read_models.orders')
            ->select([
                'status',
                $this->queryBuilder->raw('COUNT(*) as count'),
                $this->queryBuilder->raw('SUM(total_amount) as total_revenue')
            ])
            ->groupBy('status')
            ->get();
    }
}
```

## Saga Patterns

```php
<?php
// app/Infrastructure/Sagas/OrderSaga.php

namespace App\Infrastructure\Sagas;

use TuskLang\Sagas\SagaInterface;
use TuskLang\EventSourcing\EventStore;
use TuskLang\CommandBus\CommandBus;

class OrderSaga implements SagaInterface
{
    private string $sagaId;
    private string $orderId;
    private string $state = 'started';
    private array $compensations = [];
    
    public function __construct(
        private EventStore $eventStore,
        private CommandBus $commandBus
    ) {}
    
    public function start(string $orderId): void
    {
        $this->sagaId = uniqid('saga_');
        $this->orderId = $orderId;
        
        // Step 1: Reserve inventory
        $this->reserveInventory();
    }
    
    public function onInventoryReserved(array $event): void
    {
        $this->state = 'inventory_reserved';
        
        // Step 2: Process payment
        $this->processPayment();
    }
    
    public function onPaymentProcessed(array $event): void
    {
        $this->state = 'payment_processed';
        
        // Step 3: Confirm order
        $this->confirmOrder();
    }
    
    public function onOrderConfirmed(array $event): void
    {
        $this->state = 'completed';
    }
    
    public function onInventoryReservationFailed(array $event): void
    {
        $this->state = 'failed';
        $this->compensate();
    }
    
    public function onPaymentFailed(array $event): void
    {
        $this->state = 'failed';
        $this->compensate();
    }
    
    private function reserveInventory(): void
    {
        $command = new ReserveInventoryCommand($this->orderId);
        $this->commandBus->dispatch($command);
        
        $this->compensations[] = new ReleaseInventoryCommand($this->orderId);
    }
    
    private function processPayment(): void
    {
        $command = new ProcessPaymentCommand($this->orderId);
        $this->commandBus->dispatch($command);
        
        $this->compensations[] = new RefundPaymentCommand($this->orderId);
    }
    
    private function confirmOrder(): void
    {
        $command = new ConfirmOrderCommand($this->orderId);
        $this->commandBus->dispatch($command);
    }
    
    private function compensate(): void
    {
        // Execute compensations in reverse order
        $compensations = array_reverse($this->compensations);
        
        foreach ($compensations as $compensation) {
            try {
                $this->commandBus->dispatch($compensation);
            } catch (\Exception $e) {
                // Log compensation failure
                error_log("Compensation failed: " . $e->getMessage());
            }
        }
    }
    
    public function getSagaId(): string
    {
        return $this->sagaId;
    }
    
    public function getState(): string
    {
        return $this->state;
    }
}
```

## Best Practices

```php
// config/event-sourcing-best-practices.tsk

event_sourcing_best_practices = {
    event_design = {
        use_past_tense = true
        include_aggregate_id = true
        version_events = true
        keep_events_immutable = true
        use_value_objects = true
    }
    
    aggregate_design = {
        keep_aggregates_small = true
        use_commands_for_validation = true
        apply_business_rules = true
        maintain_consistency = true
        use_snapshots = true
    }
    
    projection_design = {
        make_projections_fast = true
        use_denormalized_data = true
        handle_concurrent_updates = true
        optimize_for_reads = true
        use_materialized_views = true
    }
    
    saga_design = {
        keep_sagas_simple = true
        implement_compensations = true
        handle_failures_gracefully = true
        use_timeouts = true
        monitor_saga_progress = true
    }
    
    performance = {
        use_snapshots = true
        batch_event_processing = true
        optimize_event_store = true
        use_read_replicas = true
        implement_caching = true
    }
    
    monitoring = {
        track_event_counts = true
        monitor_projection_lag = true
        alert_on_failures = true
        log_important_events = true
        measure_performance = true
    }
}

// Example usage in PHP
class EventSourcingBestPractices
{
    public function implementBestPractices(): void
    {
        // 1. Design events properly
        $event = new OrderCreated(
            orderId: $orderId,
            customerId: $customerId,
            createdAt: new \DateTimeImmutable(),
            version: 1
        );
        
        // 2. Validate commands before applying
        $command = new CreateOrderCommand($orderId, $customerId);
        $this->validator->validate($command);
        
        // 3. Apply business rules in aggregates
        $aggregate = OrderAggregate::create($orderId, $customerId);
        $aggregate->validateBusinessRules();
        
        // 4. Use snapshots for performance
        if ($this->shouldCreateSnapshot($aggregate)) {
            $this->snapshotStore->save($aggregate->getId(), $aggregate, $aggregate->getVersion());
        }
        
        // 5. Handle saga compensations
        $saga = new OrderSaga($this->eventStore, $this->commandBus);
        $saga->start($orderId);
        
        // 6. Monitor and alert
        $this->metrics->increment('events.published');
        $this->logger->info('Event published', ['event' => $event]);
    }
    
    private function shouldCreateSnapshot(object $aggregate): bool
    {
        $config = TuskLang::load('event_store');
        $snapshotInterval = $config['aggregates'][get_class($aggregate)]['snapshot_interval'] ?? 100;
        
        return $aggregate->getVersion() % $snapshotInterval === 0;
    }
}
```

This comprehensive guide covers advanced event sourcing patterns in TuskLang with PHP integration. The event sourcing system is designed to be scalable, maintainable, and performant while maintaining the rebellious spirit of TuskLang development. 