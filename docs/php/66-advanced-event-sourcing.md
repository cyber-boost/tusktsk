# Advanced Event Sourcing

TuskLang enables PHP developers to build robust event-sourced systems with confidence. This guide covers advanced event sourcing patterns, CQRS, and event store management.

## Table of Contents
- [Event Store Management](#event-store-management)
- [Aggregates and Commands](#aggregates-and-commands)
- [CQRS Implementation](#cqrs-implementation)
- [Projections and Read Models](#projections-and-read-models)
- [Event Replay and Snapshots](#event-replay-and-snapshots)
- [Event Versioning](#event-versioning)
- [Event-Driven Integration](#event-driven-integration)
- [Best Practices](#best-practices)

## Event Store Management

```php
// config/event-sourcing.tsk
event_sourcing = {
    event_store = {
        provider = "postgresql"
        table = "events"
        stream_prefix = "stream_"
        metadata = true
    }
    
    aggregates = {
        user = {
            stream_name = "user"
            snapshot_interval = 100
            allowed_events = ["UserCreated", "UserUpdated", "UserDeleted"]
        }
        order = {
            stream_name = "order"
            snapshot_interval = 50
            allowed_events = ["OrderCreated", "OrderItemAdded", "OrderCompleted"]
        }
    }
    
    projections = {
        user_list = {
            source = "user"
            target = "read_models.user_list"
            handlers = ["UserCreated", "UserUpdated", "UserDeleted"]
        }
        order_summary = {
            source = "order"
            target = "read_models.order_summary"
            handlers = ["OrderCreated", "OrderItemAdded", "OrderCompleted"]
        }
    }
}
```

## Aggregates and Commands

```php
<?php
// app/Domain/Aggregates/UserAggregate.php

namespace App\Domain\Aggregates;

use TuskLang\EventSourcing\AggregateRoot;
use TuskLang\EventSourcing\EventStore;

class UserAggregate extends AggregateRoot
{
    private string $id;
    private string $name;
    private string $email;
    private bool $active = true;
    
    public static function create(string $id, string $name, string $email): self
    {
        $aggregate = new self();
        $aggregate->apply(new UserCreated($id, $name, $email));
        return $aggregate;
    }
    
    public function update(string $name, string $email): void
    {
        $this->apply(new UserUpdated($this->id, $name, $email));
    }
    
    public function delete(): void
    {
        $this->apply(new UserDeleted($this->id));
    }
    
    protected function applyUserCreated(UserCreated $event): void
    {
        $this->id = $event->getId();
        $this->name = $event->getName();
        $this->email = $event->getEmail();
    }
    
    protected function applyUserUpdated(UserUpdated $event): void
    {
        $this->name = $event->getName();
        $this->email = $event->getEmail();
    }
    
    protected function applyUserDeleted(UserDeleted $event): void
    {
        $this->active = false;
    }
}
```

## CQRS Implementation

```php
<?php
// app/Application/Commands/CreateUserCommand.php

namespace App\Application\Commands;

use TuskLang\CQRS\Command;

class CreateUserCommand implements Command
{
    public function __construct(
        public readonly string $id,
        public readonly string $name,
        public readonly string $email
    ) {}
}

// app/Application/Commands/CreateUserHandler.php

namespace App\Application\Commands;

use App\Domain\Aggregates\UserAggregate;
use TuskLang\EventSourcing\EventStore;

class CreateUserHandler
{
    public function __construct(private EventStore $eventStore) {}
    
    public function handle(CreateUserCommand $command): void
    {
        $aggregate = UserAggregate::create(
            $command->id,
            $command->name,
            $command->email
        );
        
        $this->eventStore->save($aggregate);
    }
}

// app/Application/Queries/GetUserQuery.php

namespace App\Application\Queries;

use TuskLang\CQRS\Query;

class GetUserQuery implements Query
{
    public function __construct(public readonly string $id) {}
}

// app/Application/Queries/GetUserHandler.php

namespace App\Application\Queries;

use App\Infrastructure\ReadModels\UserReadModel;

class GetUserHandler
{
    public function __construct(private UserReadModel $readModel) {}
    
    public function handle(GetUserQuery $query): ?array
    {
        return $this->readModel->findById($query->id);
    }
}
```

## Projections and Read Models

```php
<?php
// app/Infrastructure/Projections/UserListProjection.php

namespace App\Infrastructure\Projections;

use TuskLang\EventSourcing\Projection;
use TuskLang\Database\Connection;

class UserListProjection implements Projection
{
    public function __construct(private Connection $connection) {}
    
    public function onUserCreated(array $event): void
    {
        $this->connection->insert('read_models.user_list', [
            'id' => $event['id'],
            'name' => $event['name'],
            'email' => $event['email'],
            'created_at' => $event['timestamp']
        ]);
    }
    
    public function onUserUpdated(array $event): void
    {
        $this->connection->update('read_models.user_list', [
            'name' => $event['name'],
            'email' => $event['email'],
            'updated_at' => $event['timestamp']
        ], ['id' => $event['id']]);
    }
    
    public function onUserDeleted(array $event): void
    {
        $this->connection->delete('read_models.user_list', ['id' => $event['id']]);
    }
}

// app/Infrastructure/ReadModels/UserReadModel.php

namespace App\Infrastructure\ReadModels;

use TuskLang\Database\QueryBuilder;

class UserReadModel
{
    public function __construct(private QueryBuilder $queryBuilder) {}
    
    public function findById(string $id): ?array
    {
        return $this->queryBuilder
            ->table('read_models.user_list')
            ->where('id', $id)
            ->first();
    }
    
    public function findAll(int $limit = 20, int $offset = 0): array
    {
        return $this->queryBuilder
            ->table('read_models.user_list')
            ->limit($limit)
            ->offset($offset)
            ->get();
    }
    
    public function findByEmail(string $email): ?array
    {
        return $this->queryBuilder
            ->table('read_models.user_list')
            ->where('email', $email)
            ->first();
    }
}
```

## Event Replay and Snapshots

```php
<?php
// app/Infrastructure/EventStore/EventReplayer.php

namespace App\Infrastructure\EventStore;

use TuskLang\EventSourcing\EventStore;
use TuskLang\EventSourcing\SnapshotStore;

class EventReplayer
{
    public function __construct(
        private EventStore $eventStore,
        private SnapshotStore $snapshotStore
    ) {}
    
    public function replayProjection(string $projectionName, ?string $fromEventId = null): void
    {
        $projection = $this->createProjection($projectionName);
        
        $events = $this->eventStore->getEvents($fromEventId);
        
        foreach ($events as $event) {
            $projection->handle($event);
        }
    }
    
    public function createSnapshot(string $aggregateId, int $version): void
    {
        $aggregate = $this->eventStore->load($aggregateId);
        $this->snapshotStore->save($aggregateId, $aggregate, $version);
    }
    
    public function loadFromSnapshot(string $aggregateId): ?object
    {
        $snapshot = $this->snapshotStore->load($aggregateId);
        
        if ($snapshot) {
            $events = $this->eventStore->getEventsFromVersion($aggregateId, $snapshot['version']);
            return $this->reconstructAggregate($snapshot['state'], $events);
        }
        
        return null;
    }
    
    private function createProjection(string $name): object
    {
        $projectionClass = "App\\Infrastructure\\Projections\\{$name}Projection";
        return new $projectionClass($this->connection);
    }
    
    private function reconstructAggregate(array $state, array $events): object
    {
        // Implementation depends on your aggregate reconstruction logic
        return new UserAggregate($state, $events);
    }
}
```

## Event Versioning

```php
<?php
// app/Infrastructure/EventStore/EventUpgrader.php

namespace App\Infrastructure\EventStore;

class EventUpgrader
{
    private array $upgraders = [];
    
    public function registerUpgrader(string $eventType, callable $upgrader): void
    {
        $this->upgraders[$eventType] = $upgrader;
    }
    
    public function upgradeEvent(array $event): array
    {
        $eventType = $event['type'];
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

// Example event upgrader
$eventUpgrader = new EventUpgrader();
$eventUpgrader->registerUpgrader('UserCreated', function(array $event, int $version) {
    if ($version === 1) {
        // Upgrade from v1 to v2
        $event['data']['status'] = 'active';
        $event['version'] = 2;
    }
    return $event;
});
```

## Event-Driven Integration

```php
<?php
// app/Infrastructure/EventBus/EventBus.php

namespace App\Infrastructure\EventBus;

use TuskLang\Events\EventDispatcher;

class EventBus
{
    private EventDispatcher $dispatcher;
    private array $subscribers = [];
    
    public function __construct(EventDispatcher $dispatcher)
    {
        $this->dispatcher = $dispatcher;
    }
    
    public function publish(array $event): void
    {
        $this->dispatcher->dispatch($event['type'], $event);
        
        // Also publish to external systems if configured
        $this->publishToExternalSystems($event);
    }
    
    public function subscribe(string $eventType, callable $handler): void
    {
        $this->subscribers[$eventType][] = $handler;
        $this->dispatcher->listen($eventType, $handler);
    }
    
    private function publishToExternalSystems(array $event): void
    {
        $config = TuskLang::load('event_sourcing');
        
        if (isset($config['external_publishers'])) {
            foreach ($config['external_publishers'] as $publisher) {
                $this->publishToSystem($event, $publisher);
            }
        }
    }
    
    private function publishToSystem(array $event, array $publisher): void
    {
        $client = new \GuzzleHttp\Client();
        
        try {
            $client->post($publisher['url'], [
                'json' => $event,
                'headers' => $publisher['headers'] ?? []
            ]);
        } catch (\Exception $e) {
            // Log failed external publishing
            error_log("Failed to publish event to {$publisher['url']}: " . $e->getMessage());
        }
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
    }
    
    aggregate_design = {
        keep_aggregates_small = true
        use_commands_for_validation = true
        apply_business_rules = true
        maintain_consistency = true
    }
    
    projection_design = {
        make_projections_fast = true
        use_denormalized_data = true
        handle_concurrent_updates = true
        optimize_for_reads = true
    }
    
    performance = {
        use_snapshots = true
        batch_event_processing = true
        optimize_event_store = true
        use_read_replicas = true
    }
    
    monitoring = {
        track_event_counts = true
        monitor_projection_lag = true
        alert_on_failures = true
        log_important_events = true
    }
}

// Example usage in PHP
class EventSourcingBestPractices
{
    public function implementBestPractices(): void
    {
        // 1. Design events properly
        $event = new UserCreated(
            id: $userId,
            name: $name,
            email: $email,
            timestamp: new DateTime(),
            version: 1
        );
        
        // 2. Validate commands before applying
        $command = new CreateUserCommand($userId, $name, $email);
        $this->validator->validate($command);
        
        // 3. Apply business rules in aggregates
        $aggregate = UserAggregate::create($userId, $name, $email);
        $aggregate->validateBusinessRules();
        
        // 4. Use snapshots for performance
        if ($this->shouldCreateSnapshot($aggregate)) {
            $this->snapshotStore->save($aggregate);
        }
        
        // 5. Monitor and alert
        $this->metrics->increment('events.published');
        $this->logger->info('Event published', ['event' => $event]);
    }
    
    private function shouldCreateSnapshot(object $aggregate): bool
    {
        $config = TuskLang::load('event_sourcing');
        $snapshotInterval = $config['aggregates'][get_class($aggregate)]['snapshot_interval'] ?? 100;
        
        return $aggregate->getVersion() % $snapshotInterval === 0;
    }
}
```

This comprehensive guide covers advanced event sourcing patterns in TuskLang with PHP integration. The event sourcing system is designed to be scalable, maintainable, and performant while maintaining the rebellious spirit of TuskLang development. 