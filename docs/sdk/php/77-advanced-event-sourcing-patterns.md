# Advanced Event Sourcing Patterns

TuskLang enables PHP developers to implement sophisticated event sourcing patterns with confidence. This guide covers advanced event sourcing, CQRS implementation, and event-driven architecture strategies.

## Table of Contents
- [Event Store Configuration](#event-store-configuration)
- [Aggregate Design](#aggregate-design)
- [Event Projections](#event-projections)
- [Snapshot Management](#snapshot-management)
- [Event Replay](#event-replay)
- [Best Practices](#best-practices)

## Event Store Configuration

```php
// config/event-sourcing.tsk
event_sourcing = {
    event_store = {
        type = "database"
        provider = "mysql"
        table = "event_store"
        connection = "@env('EVENT_STORE_DB')"
        
        columns = {
            id = "event_id"
            aggregate_id = "aggregate_id"
            aggregate_type = "aggregate_type"
            event_type = "event_type"
            event_data = "event_data"
            event_metadata = "event_metadata"
            version = "version"
            occurred_at = "occurred_at"
            stream_id = "stream_id"
        }
        
        indexing = {
            aggregate_id_index = true
            event_type_index = true
            occurred_at_index = true
            stream_id_index = true
        }
    }
    
    projections = {
        enabled = true
        real_time = true
        batch_processing = true
        error_handling = "retry"
        
        types = {
            read_model = true
            analytics = true
            audit = true
            notification = true
        }
    }
    
    snapshots = {
        enabled = true
        frequency = 100
        storage = "database"
        compression = true
        retention = "30d"
    }
    
    event_handlers = {
        async_processing = true
        retry_policy = {
            max_attempts = 3
            backoff_strategy = "exponential"
            initial_delay = 1000
        }
        
        dead_letter_queue = {
            enabled = true
            storage = "redis"
            ttl = "7d"
        }
    }
    
    serialization = {
        format = "json"
        compression = true
        versioning = true
        schema_evolution = true
    }
}
```

## Aggregate Design

```php
<?php
// app/Domain/Aggregates/UserAggregate.php

namespace App\Domain\Aggregates;

use TuskLang\EventSourcing\AggregateRoot;
use TuskLang\EventSourcing\DomainEvent;
use App\Domain\Events\UserCreated;
use App\Domain\Events\UserActivated;
use App\Domain\Events\UserDeactivated;
use App\Domain\Events\UserProfileUpdated;
use App\Domain\ValueObjects\UserId;
use App\Domain\ValueObjects\Email;
use App\Domain\ValueObjects\UserStatus;

class UserAggregate extends AggregateRoot
{
    private UserId $id;
    private Email $email;
    private UserStatus $status;
    private string $name;
    private \DateTimeImmutable $createdAt;
    private ?\DateTimeImmutable $activatedAt = null;
    
    public static function create(UserId $id, Email $email, string $name): self
    {
        $aggregate = new self();
        $aggregate->apply(new UserCreated($id, $email, $name));
        return $aggregate;
    }
    
    public function activate(): void
    {
        if ($this->status->equals(UserStatus::ACTIVE())) {
            throw new \DomainException('User is already active');
        }
        
        $this->apply(new UserActivated($this->id, new \DateTimeImmutable()));
    }
    
    public function deactivate(): void
    {
        if ($this->status->equals(UserStatus::INACTIVE())) {
            throw new \DomainException('User is already inactive');
        }
        
        $this->apply(new UserDeactivated($this->id, new \DateTimeImmutable()));
    }
    
    public function updateProfile(string $name): void
    {
        if ($name !== $this->name) {
            $this->apply(new UserProfileUpdated($this->id, $name, $this->name));
        }
    }
    
    protected function applyUserCreated(UserCreated $event): void
    {
        $this->id = $event->userId;
        $this->email = $event->email;
        $this->name = $event->name;
        $this->status = UserStatus::PENDING();
        $this->createdAt = $event->occurredAt;
    }
    
    protected function applyUserActivated(UserActivated $event): void
    {
        $this->status = UserStatus::ACTIVE();
        $this->activatedAt = $event->activatedAt;
    }
    
    protected function applyUserDeactivated(UserDeactivated $event): void
    {
        $this->status = UserStatus::INACTIVE();
    }
    
    protected function applyUserProfileUpdated(UserProfileUpdated $event): void
    {
        $this->name = $event->newName;
    }
    
    public function getId(): UserId
    {
        return $this->id;
    }
    
    public function getEmail(): Email
    {
        return $this->email;
    }
    
    public function getStatus(): UserStatus
    {
        return $this->status;
    }
    
    public function getName(): string
    {
        return $this->name;
    }
    
    public function getCreatedAt(): \DateTimeImmutable
    {
        return $this->createdAt;
    }
    
    public function getActivatedAt(): ?\DateTimeImmutable
    {
        return $this->activatedAt;
    }
}

// app/Domain/Aggregates/OrderAggregate.php

namespace App\Domain\Aggregates;

use TuskLang\EventSourcing\AggregateRoot;
use App\Domain\Events\OrderCreated;
use App\Domain\Events\OrderItemAdded;
use App\Domain\Events\OrderItemRemoved;
use App\Domain\Events\OrderConfirmed;
use App\Domain\Events\OrderCancelled;
use App\Domain\ValueObjects\OrderId;
use App\Domain\ValueObjects\UserId;
use App\Domain\ValueObjects\OrderStatus;
use App\Domain\ValueObjects\Money;

class OrderAggregate extends AggregateRoot
{
    private OrderId $id;
    private UserId $userId;
    private OrderStatus $status;
    private array $items = [];
    private Money $totalAmount;
    private \DateTimeImmutable $createdAt;
    private ?\DateTimeImmutable $confirmedAt = null;
    
    public static function create(OrderId $id, UserId $userId): self
    {
        $aggregate = new self();
        $aggregate->apply(new OrderCreated($id, $userId));
        return $aggregate;
    }
    
    public function addItem(string $productId, int $quantity, Money $unitPrice): void
    {
        if (!$this->status->equals(OrderStatus::DRAFT())) {
            throw new \DomainException('Cannot add items to non-draft order');
        }
        
        $this->apply(new OrderItemAdded($this->id, $productId, $quantity, $unitPrice));
    }
    
    public function removeItem(string $productId): void
    {
        if (!$this->status->equals(OrderStatus::DRAFT())) {
            throw new \DomainException('Cannot remove items from non-draft order');
        }
        
        if (!isset($this->items[$productId])) {
            throw new \DomainException('Item not found in order');
        }
        
        $this->apply(new OrderItemRemoved($this->id, $productId));
    }
    
    public function confirm(): void
    {
        if (!$this->status->equals(OrderStatus::DRAFT())) {
            throw new \DomainException('Only draft orders can be confirmed');
        }
        
        if (empty($this->items)) {
            throw new \DomainException('Cannot confirm empty order');
        }
        
        $this->apply(new OrderConfirmed($this->id, new \DateTimeImmutable()));
    }
    
    public function cancel(): void
    {
        if ($this->status->equals(OrderStatus::CANCELLED())) {
            throw new \DomainException('Order is already cancelled');
        }
        
        $this->apply(new OrderCancelled($this->id, new \DateTimeImmutable()));
    }
    
    protected function applyOrderCreated(OrderCreated $event): void
    {
        $this->id = $event->orderId;
        $this->userId = $event->userId;
        $this->status = OrderStatus::DRAFT();
        $this->totalAmount = Money::zero();
        $this->createdAt = $event->occurredAt;
    }
    
    protected function applyOrderItemAdded(OrderItemAdded $event): void
    {
        $itemKey = $event->productId;
        
        if (isset($this->items[$itemKey])) {
            $this->items[$itemKey]['quantity'] += $event->quantity;
        } else {
            $this->items[$itemKey] = [
                'productId' => $event->productId,
                'quantity' => $event->quantity,
                'unitPrice' => $event->unitPrice
            ];
        }
        
        $this->recalculateTotal();
    }
    
    protected function applyOrderItemRemoved(OrderItemRemoved $event): void
    {
        unset($this->items[$event->productId]);
        $this->recalculateTotal();
    }
    
    protected function applyOrderConfirmed(OrderConfirmed $event): void
    {
        $this->status = OrderStatus::CONFIRMED();
        $this->confirmedAt = $event->confirmedAt;
    }
    
    protected function applyOrderCancelled(OrderCancelled $event): void
    {
        $this->status = OrderStatus::CANCELLED();
    }
    
    private function recalculateTotal(): void
    {
        $total = Money::zero();
        
        foreach ($this->items as $item) {
            $itemTotal = $item['unitPrice']->multiply($item['quantity']);
            $total = $total->add($itemTotal);
        }
        
        $this->totalAmount = $total;
    }
    
    public function getId(): OrderId
    {
        return $this->id;
    }
    
    public function getUserId(): UserId
    {
        return $this->userId;
    }
    
    public function getStatus(): OrderStatus
    {
        return $this->status;
    }
    
    public function getItems(): array
    {
        return $this->items;
    }
    
    public function getTotalAmount(): Money
    {
        return $this->totalAmount;
    }
    
    public function getCreatedAt(): \DateTimeImmutable
    {
        return $this->createdAt;
    }
    
    public function getConfirmedAt(): ?\DateTimeImmutable
    {
        return $this->confirmedAt;
    }
}
```

## Event Projections

```php
<?php
// app/Infrastructure/Projections/UserProjection.php

namespace App\Infrastructure\Projections;

use TuskLang\EventSourcing\Projection;
use TuskLang\EventSourcing\EventStore;
use App\Domain\Events\UserCreated;
use App\Domain\Events\UserActivated;
use App\Domain\Events\UserDeactivated;
use App\Domain\Events\UserProfileUpdated;

class UserProjection implements Projection
{
    private EventStore $eventStore;
    private \PDO $connection;
    
    public function __construct(EventStore $eventStore, \PDO $connection)
    {
        $this->eventStore = $eventStore;
        $this->connection = $connection;
    }
    
    public function project(string $streamId): void
    {
        $events = $this->eventStore->getEvents($streamId);
        
        foreach ($events as $event) {
            $this->handleEvent($event);
        }
    }
    
    public function handleEvent(DomainEvent $event): void
    {
        match(get_class($event)) {
            UserCreated::class => $this->handleUserCreated($event),
            UserActivated::class => $this->handleUserActivated($event),
            UserDeactivated::class => $this->handleUserDeactivated($event),
            UserProfileUpdated::class => $this->handleUserProfileUpdated($event),
            default => null
        };
    }
    
    private function handleUserCreated(UserCreated $event): void
    {
        $stmt = $this->connection->prepare("
            INSERT INTO users (id, email, name, status, created_at)
            VALUES (?, ?, ?, ?, ?)
        ");
        
        $stmt->execute([
            $event->userId->getValue(),
            $event->email->getValue(),
            $event->name,
            'pending',
            $event->occurredAt->format('Y-m-d H:i:s')
        ]);
    }
    
    private function handleUserActivated(UserActivated $event): void
    {
        $stmt = $this->connection->prepare("
            UPDATE users 
            SET status = ?, activated_at = ?
            WHERE id = ?
        ");
        
        $stmt->execute([
            'active',
            $event->activatedAt->format('Y-m-d H:i:s'),
            $event->userId->getValue()
        ]);
    }
    
    private function handleUserDeactivated(UserDeactivated $event): void
    {
        $stmt = $this->connection->prepare("
            UPDATE users 
            SET status = ?
            WHERE id = ?
        ");
        
        $stmt->execute([
            'inactive',
            $event->userId->getValue()
        ]);
    }
    
    private function handleUserProfileUpdated(UserProfileUpdated $event): void
    {
        $stmt = $this->connection->prepare("
            UPDATE users 
            SET name = ?
            WHERE id = ?
        ");
        
        $stmt->execute([
            $event->newName,
            $event->userId->getValue()
        ]);
    }
}

// app/Infrastructure/Projections/OrderProjection.php

namespace App\Infrastructure\Projections;

use TuskLang\EventSourcing\Projection;
use App\Domain\Events\OrderCreated;
use App\Domain\Events\OrderItemAdded;
use App\Domain\Events\OrderItemRemoved;
use App\Domain\Events\OrderConfirmed;
use App\Domain\Events\OrderCancelled;

class OrderProjection implements Projection
{
    private EventStore $eventStore;
    private \PDO $connection;
    
    public function __construct(EventStore $eventStore, \PDO $connection)
    {
        $this->eventStore = $eventStore;
        $this->connection = $connection;
    }
    
    public function project(string $streamId): void
    {
        $events = $this->eventStore->getEvents($streamId);
        
        foreach ($events as $event) {
            $this->handleEvent($event);
        }
    }
    
    public function handleEvent(DomainEvent $event): void
    {
        match(get_class($event)) {
            OrderCreated::class => $this->handleOrderCreated($event),
            OrderItemAdded::class => $this->handleOrderItemAdded($event),
            OrderItemRemoved::class => $this->handleOrderItemRemoved($event),
            OrderConfirmed::class => $this->handleOrderConfirmed($event),
            OrderCancelled::class => $this->handleOrderCancelled($event),
            default => null
        };
    }
    
    private function handleOrderCreated(OrderCreated $event): void
    {
        $stmt = $this->connection->prepare("
            INSERT INTO orders (id, user_id, status, total_amount, created_at)
            VALUES (?, ?, ?, ?, ?)
        ");
        
        $stmt->execute([
            $event->orderId->getValue(),
            $event->userId->getValue(),
            'draft',
            0.00,
            $event->occurredAt->format('Y-m-d H:i:s')
        ]);
    }
    
    private function handleOrderItemAdded(OrderItemAdded $event): void
    {
        $stmt = $this->connection->prepare("
            INSERT INTO order_items (order_id, product_id, quantity, unit_price)
            VALUES (?, ?, ?, ?)
            ON DUPLICATE KEY UPDATE
            quantity = quantity + VALUES(quantity)
        ");
        
        $stmt->execute([
            $event->orderId->getValue(),
            $event->productId,
            $event->quantity,
            $event->unitPrice->getAmount()
        ]);
        
        $this->updateOrderTotal($event->orderId->getValue());
    }
    
    private function handleOrderItemRemoved(OrderItemRemoved $event): void
    {
        $stmt = $this->connection->prepare("
            DELETE FROM order_items 
            WHERE order_id = ? AND product_id = ?
        ");
        
        $stmt->execute([
            $event->orderId->getValue(),
            $event->productId
        ]);
        
        $this->updateOrderTotal($event->orderId->getValue());
    }
    
    private function handleOrderConfirmed(OrderConfirmed $event): void
    {
        $stmt = $this->connection->prepare("
            UPDATE orders 
            SET status = ?, confirmed_at = ?
            WHERE id = ?
        ");
        
        $stmt->execute([
            'confirmed',
            $event->confirmedAt->format('Y-m-d H:i:s'),
            $event->orderId->getValue()
        ]);
    }
    
    private function handleOrderCancelled(OrderCancelled $event): void
    {
        $stmt = $this->connection->prepare("
            UPDATE orders 
            SET status = ?
            WHERE id = ?
        ");
        
        $stmt->execute([
            'cancelled',
            $event->orderId->getValue()
        ]);
    }
    
    private function updateOrderTotal(string $orderId): void
    {
        $stmt = $this->connection->prepare("
            UPDATE orders o
            SET total_amount = (
                SELECT COALESCE(SUM(quantity * unit_price), 0)
                FROM order_items oi
                WHERE oi.order_id = o.id
            )
            WHERE o.id = ?
        ");
        
        $stmt->execute([$orderId]);
    }
}
```

## Snapshot Management

```php
<?php
// app/Infrastructure/Snapshots/SnapshotManager.php

namespace App\Infrastructure\Snapshots;

use TuskLang\EventSourcing\SnapshotManagerInterface;
use TuskLang\EventSourcing\AggregateRoot;

class SnapshotManager implements SnapshotManagerInterface
{
    private \PDO $connection;
    private array $config;
    
    public function __construct(\PDO $connection, array $config)
    {
        $this->connection = $connection;
        $this->config = $config;
    }
    
    public function saveSnapshot(string $aggregateId, AggregateRoot $aggregate, int $version): void
    {
        $snapshot = [
            'aggregate_id' => $aggregateId,
            'aggregate_type' => get_class($aggregate),
            'version' => $version,
            'state' => json_encode($aggregate->getState()),
            'created_at' => date('Y-m-d H:i:s')
        ];
        
        $stmt = $this->connection->prepare("
            INSERT INTO snapshots (aggregate_id, aggregate_type, version, state, created_at)
            VALUES (?, ?, ?, ?, ?)
            ON DUPLICATE KEY UPDATE
            version = VALUES(version),
            state = VALUES(state),
            created_at = VALUES(created_at)
        ");
        
        $stmt->execute([
            $snapshot['aggregate_id'],
            $snapshot['aggregate_type'],
            $snapshot['version'],
            $snapshot['state'],
            $snapshot['created_at']
        ]);
    }
    
    public function getSnapshot(string $aggregateId): ?array
    {
        $stmt = $this->connection->prepare("
            SELECT * FROM snapshots 
            WHERE aggregate_id = ?
            ORDER BY version DESC 
            LIMIT 1
        ");
        
        $stmt->execute([$aggregateId]);
        $result = $stmt->fetch(\PDO::FETCH_ASSOC);
        
        if (!$result) {
            return null;
        }
        
        return [
            'aggregate_id' => $result['aggregate_id'],
            'aggregate_type' => $result['aggregate_type'],
            'version' => (int) $result['version'],
            'state' => json_decode($result['state'], true),
            'created_at' => new \DateTimeImmutable($result['created_at'])
        ];
    }
    
    public function shouldCreateSnapshot(string $aggregateId, int $version): bool
    {
        $frequency = $this->config['snapshots']['frequency'] ?? 100;
        return $version % $frequency === 0;
    }
    
    public function cleanupOldSnapshots(): void
    {
        $retention = $this->config['snapshots']['retention'] ?? '30d';
        $cutoffDate = date('Y-m-d H:i:s', strtotime("-{$retention}"));
        
        $stmt = $this->connection->prepare("
            DELETE FROM snapshots 
            WHERE created_at < ?
        ");
        
        $stmt->execute([$cutoffDate]);
    }
}
```

## Event Replay

```php
<?php
// app/Infrastructure/EventReplay/EventReplayManager.php

namespace App\Infrastructure\EventReplay;

use TuskLang\EventSourcing\EventStore;
use TuskLang\EventSourcing\Projection;

class EventReplayManager
{
    private EventStore $eventStore;
    private array $projections = [];
    private array $config;
    
    public function __construct(EventStore $eventStore, array $config)
    {
        $this->eventStore = $eventStore;
        $this->config = $config;
    }
    
    public function addProjection(string $name, Projection $projection): void
    {
        $this->projections[$name] = $projection;
    }
    
    public function replayAll(string $projectionName, ?\DateTimeImmutable $from = null): void
    {
        if (!isset($this->projections[$projectionName])) {
            throw new \RuntimeException("Projection '{$projectionName}' not found");
        }
        
        $projection = $this->projections[$projectionName];
        $events = $this->eventStore->getAllEvents($from);
        
        foreach ($events as $event) {
            $projection->handleEvent($event);
        }
    }
    
    public function replayStream(string $projectionName, string $streamId): void
    {
        if (!isset($this->projections[$projectionName])) {
            throw new \RuntimeException("Projection '{$projectionName}' not found");
        }
        
        $projection = $this->projections[$projectionName];
        $projection->project($streamId);
    }
    
    public function replayFromVersion(string $projectionName, string $streamId, int $fromVersion): void
    {
        if (!isset($this->projections[$projectionName])) {
            throw new \RuntimeException("Projection '{$projectionName}' not found");
        }
        
        $projection = $this->projections[$projectionName];
        $events = $this->eventStore->getEventsFromVersion($streamId, $fromVersion);
        
        foreach ($events as $event) {
            $projection->handleEvent($event);
        }
    }
    
    public function getReplayStatus(string $projectionName): array
    {
        if (!isset($this->projections[$projectionName])) {
            throw new \RuntimeException("Projection '{$projectionName}' not found");
        }
        
        $lastEvent = $this->eventStore->getLastEvent();
        $lastProcessedEvent = $this->getLastProcessedEvent($projectionName);
        
        return [
            'projection' => $projectionName,
            'last_event_id' => $lastEvent ? $lastEvent->getId() : null,
            'last_processed_event_id' => $lastProcessedEvent ? $lastProcessedEvent->getId() : null,
            'is_up_to_date' => $lastEvent && $lastProcessedEvent && 
                              $lastEvent->getId() === $lastProcessedEvent->getId()
        ];
    }
    
    private function getLastProcessedEvent(string $projectionName): ?DomainEvent
    {
        // Implementation depends on how you track processed events
        // This is a simplified version
        return null;
    }
}
```

## Best Practices

```php
// config/event-sourcing-best-practices.tsk
event_sourcing_best_practices = {
    aggregate_design = {
        keep_aggregates_small = true
        use_value_objects = true
        implement_business_rules = true
        maintain_consistency = true
    }
    
    event_design = {
        make_events_immutable = true
        use_descriptive_names = true
        include_necessary_data = true
        version_events = true
    }
    
    projection_design = {
        keep_projections_simple = true
        handle_errors_gracefully = true
        use_idempotent_operations = true
        optimize_for_reads = true
    }
    
    performance = {
        use_snapshots = true
        optimize_event_store = true
        implement_caching = true
        use_batch_processing = true
    }
    
    testing = {
        test_aggregate_behavior = true
        test_event_projections = true
        test_event_replay = true
        use_event_sourcing_testing = true
    }
    
    monitoring = {
        track_event_counts = true
        monitor_projection_lag = true
        alert_on_failures = true
        measure_performance = true
    }
}

// Example usage in PHP
class EventSourcingBestPractices
{
    public function implementBestPractices(): void
    {
        // 1. Configure event store
        $config = TuskLang::load('event_sourcing');
        $eventStore = new DatabaseEventStore($config['event_store']);
        
        // 2. Set up projections
        $userProjection = new UserProjection($eventStore, $this->connection);
        $orderProjection = new OrderProjection($eventStore, $this->connection);
        
        // 3. Configure snapshot manager
        $snapshotManager = new SnapshotManager($this->connection, $config['snapshots']);
        
        // 4. Set up event replay manager
        $replayManager = new EventReplayManager($eventStore, $config);
        $replayManager->addProjection('user_projection', $userProjection);
        $replayManager->addProjection('order_projection', $orderProjection);
        
        // 5. Create and save aggregate
        $user = UserAggregate::create(
            new UserId(),
            new Email('user@example.com'),
            'John Doe'
        );
        
        $eventStore->save($user->getId()->getValue(), $user->getUncommittedEvents());
        
        // 6. Create snapshot if needed
        if ($snapshotManager->shouldCreateSnapshot($user->getId()->getValue(), $user->getVersion())) {
            $snapshotManager->saveSnapshot($user->getId()->getValue(), $user, $user->getVersion());
        }
        
        // 7. Project events
        $userProjection->project($user->getId()->getValue());
        
        // 8. Replay events if needed
        $replayManager->replayAll('user_projection');
        
        // 9. Monitor and log
        $this->logger->info('Event sourcing implemented', [
            'aggregates' => ['User', 'Order'],
            'projections' => ['user_projection', 'order_projection'],
            'snapshots_enabled' => $config['snapshots']['enabled'],
            'event_count' => $eventStore->getEventCount()
        ]);
    }
}
```

This comprehensive guide covers advanced event sourcing patterns in TuskLang with PHP integration. The event sourcing system is designed to be robust, scalable, and maintainable while maintaining the rebellious spirit of TuskLang development. 