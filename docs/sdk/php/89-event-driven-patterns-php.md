# ⚡ Event-Driven Patterns with TuskLang & PHP

## Introduction
Event-driven patterns are the foundation of scalable, decoupled systems. TuskLang and PHP let you build sophisticated event-driven architectures with config-driven event sourcing, CQRS, message queues, and saga patterns that handle complex business workflows.

## Key Features
- **Event sourcing and event stores**
- **CQRS (Command Query Responsibility Segregation)**
- **Message queues and event buses**
- **Event replay and projections**
- **Saga patterns for distributed transactions**
- **Event versioning and migration**

## Example: Event-Driven Configuration
```ini
[event_driven]
event_store: @go("events.ConfigureEventStore")
message_queue: @go("events.ConfigureMessageQueue")
projections: @go("events.ConfigureProjections")
sagas: @go("events.ConfigureSagas")
replay: @go("events.ConfigureReplay")
```

## PHP: Event Store Implementation
```php
<?php

namespace App\EventDriven;

use TuskLang\Config;
use TuskLang\Operators\Env;
use TuskLang\Operators\Metrics;
use TuskLang\Operators\Go;

class EventStore
{
    private $config;
    private $pdo;
    private $serializer;
    
    public function __construct()
    {
        $this->config = new Config();
        $this->pdo = $this->createConnection();
        $this->serializer = new EventSerializer();
    }
    
    public function append($aggregateId, $events, $expectedVersion = null)
    {
        $this->pdo->beginTransaction();
        
        try {
            // Check expected version
            if ($expectedVersion !== null) {
                $currentVersion = $this->getCurrentVersion($aggregateId);
                if ($currentVersion !== $expectedVersion) {
                    throw new ConcurrencyException("Expected version {$expectedVersion}, got {$currentVersion}");
                }
            }
            
            $version = $expectedVersion ?? $this->getCurrentVersion($aggregateId);
            
            foreach ($events as $event) {
                $version++;
                
                $stmt = $this->pdo->prepare("
                    INSERT INTO events (aggregate_id, aggregate_type, event_type, event_data, version, occurred_on)
                    VALUES (?, ?, ?, ?, ?, ?)
                ");
                
                $stmt->execute([
                    $aggregateId,
                    $event->getAggregateType(),
                    $event->getEventType(),
                    $this->serializer->serialize($event),
                    $version,
                    $event->getOccurredOn()->format('Y-m-d H:i:s')
                ]);
                
                // Publish event to message queue
                $this->publishEvent($event);
            }
            
            $this->pdo->commit();
            
            // Record metrics
            Metrics::record("events_appended", count($events), [
                "aggregate_type" => $events[0]->getAggregateType()
            ]);
            
            return $version;
            
        } catch (\Exception $e) {
            $this->pdo->rollBack();
            throw $e;
        }
    }
    
    public function getEvents($aggregateId, $fromVersion = 0)
    {
        $stmt = $this->pdo->prepare("
            SELECT event_type, event_data, version, occurred_on
            FROM events
            WHERE aggregate_id = ? AND version > ?
            ORDER BY version ASC
        ");
        
        $stmt->execute([$aggregateId, $fromVersion]);
        
        $events = [];
        while ($row = $stmt->fetch()) {
            $events[] = $this->serializer->deserialize(
                $row['event_type'],
                $row['event_data'],
                new \DateTime($row['occurred_on'])
            );
        }
        
        return $events;
    }
    
    public function getEventsByType($eventType, $limit = 100, $offset = 0)
    {
        $stmt = $this->pdo->prepare("
            SELECT aggregate_id, event_type, event_data, version, occurred_on
            FROM events
            WHERE event_type = ?
            ORDER BY occurred_on ASC
            LIMIT ? OFFSET ?
        ");
        
        $stmt->execute([$eventType, $limit, $offset]);
        
        $events = [];
        while ($row = $stmt->fetch()) {
            $events[] = $this->serializer->deserialize(
                $row['event_type'],
                $row['event_data'],
                new \DateTime($row['occurred_on'])
            );
        }
        
        return $events;
    }
    
    private function getCurrentVersion($aggregateId)
    {
        $stmt = $this->pdo->prepare("
            SELECT MAX(version) as version
            FROM events
            WHERE aggregate_id = ?
        ");
        
        $stmt->execute([$aggregateId]);
        $result = $stmt->fetch();
        
        return $result['version'] ?? 0;
    }
    
    private function createConnection()
    {
        $dsn = Env::get('EVENT_STORE_DSN', 'mysql:host=localhost;dbname=event_store');
        $username = Env::get('EVENT_STORE_USERNAME', 'root');
        $password = Env::secure('EVENT_STORE_PASSWORD');
        
        return new PDO($dsn, $username, $password, [
            PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION,
            PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC
        ]);
    }
    
    private function publishEvent($event)
    {
        $messageQueue = new MessageQueue();
        $messageQueue->publish('events', $event);
    }
}

class EventSerializer
{
    public function serialize($event)
    {
        return json_encode([
            'event_type' => $event->getEventType(),
            'data' => $event->getData(),
            'metadata' => $event->getMetadata()
        ]);
    }
    
    public function deserialize($eventType, $data, $occurredOn)
    {
        $decoded = json_decode($data, true);
        
        $eventClass = $this->getEventClass($eventType);
        $event = new $eventClass($decoded['data'], $occurredOn);
        
        if (isset($decoded['metadata'])) {
            $event->setMetadata($decoded['metadata']);
        }
        
        return $event;
    }
    
    private function getEventClass($eventType)
    {
        $eventMap = [
            'OrderCreated' => OrderCreatedEvent::class,
            'OrderCancelled' => OrderCancelledEvent::class,
            'PaymentProcessed' => PaymentProcessedEvent::class,
            'InventoryReserved' => InventoryReservedEvent::class
        ];
        
        if (!isset($eventMap[$eventType])) {
            throw new \Exception("Unknown event type: {$eventType}");
        }
        
        return $eventMap[$eventType];
    }
}
```

## CQRS Implementation
```php
<?php

namespace App\EventDriven\CQRS;

use TuskLang\Config;

class CommandBus
{
    private $config;
    private $handlers = [];
    
    public function __construct()
    {
        $this->config = new Config();
        $this->loadHandlers();
    }
    
    public function dispatch($command)
    {
        $commandClass = get_class($command);
        
        if (!isset($this->handlers[$commandClass])) {
            throw new \Exception("No handler found for command: {$commandClass}");
        }
        
        $handler = $this->handlers[$commandClass];
        
        return $handler->handle($command);
    }
    
    private function loadHandlers()
    {
        $handlers = $this->config->get('cqrs.command_handlers', []);
        
        foreach ($handlers as $command => $handler) {
            $this->handlers[$command] = new $handler();
        }
    }
}

class QueryBus
{
    private $config;
    private $handlers = [];
    
    public function __construct()
    {
        $this->config = new Config();
        $this->loadHandlers();
    }
    
    public function dispatch($query)
    {
        $queryClass = get_class($query);
        
        if (!isset($this->handlers[$queryClass])) {
            throw new \Exception("No handler found for query: {$queryClass}");
        }
        
        $handler = $this->handlers[$queryClass];
        
        return $handler->handle($query);
    }
    
    private function loadHandlers()
    {
        $handlers = $this->config->get('cqrs.query_handlers', []);
        
        foreach ($handlers as $query => $handler) {
            $this->handlers[$query] = new $handler();
        }
    }
}

class CreateOrderCommand
{
    private $customerId;
    private $items;
    private $total;
    
    public function __construct($customerId, array $items, $total)
    {
        $this->customerId = $customerId;
        $this->items = $items;
        $this->total = $total;
    }
    
    public function getCustomerId() { return $this->customerId; }
    public function getItems() { return $this->items; }
    public function getTotal() { return $this->total; }
}

class CreateOrderHandler
{
    private $eventStore;
    private $orderRepository;
    
    public function __construct(EventStore $eventStore, OrderRepository $orderRepository)
    {
        $this->eventStore = $eventStore;
        $this->orderRepository = $orderRepository;
    }
    
    public function handle(CreateOrderCommand $command)
    {
        $orderId = uniqid('order_');
        
        $event = new OrderCreatedEvent([
            'order_id' => $orderId,
            'customer_id' => $command->getCustomerId(),
            'items' => $command->getItems(),
            'total' => $command->getTotal(),
            'status' => 'pending'
        ]);
        
        $this->eventStore->append($orderId, [$event]);
        
        return $orderId;
    }
}

class GetOrderQuery
{
    private $orderId;
    
    public function __construct($orderId)
    {
        $this->orderId = $orderId;
    }
    
    public function getOrderId() { return $this->orderId; }
}

class GetOrderHandler
{
    private $orderRepository;
    
    public function __construct(OrderRepository $orderRepository)
    {
        $this->orderRepository = $orderRepository;
    }
    
    public function handle(GetOrderQuery $query)
    {
        return $this->orderRepository->findById($query->getOrderId());
    }
}
```

## Message Queue Implementation
```php
<?php

namespace App\EventDriven\MessageQueue;

use TuskLang\Config;

class MessageQueue
{
    private $config;
    private $redis;
    
    public function __construct()
    {
        $this->config = new Config();
        $this->redis = new Redis();
        $this->redis->connect(
            Env::get('REDIS_HOST', 'localhost'),
            Env::get('REDIS_PORT', 6379)
        );
    }
    
    public function publish($topic, $message)
    {
        $payload = [
            'id' => uniqid(),
            'topic' => $topic,
            'message' => $message,
            'timestamp' => date('c'),
            'retry_count' => 0
        ];
        
        $this->redis->lpush("queue:{$topic}", json_encode($payload));
        
        // Notify subscribers
        $this->notifySubscribers($topic, $payload);
    }
    
    public function subscribe($topic, callable $handler)
    {
        $subscriptionId = uniqid();
        
        $this->redis->sadd("subscriptions:{$topic}", $subscriptionId);
        $this->redis->hset("handlers", $subscriptionId, serialize($handler));
        
        return $subscriptionId;
    }
    
    public function unsubscribe($subscriptionId)
    {
        $this->redis->srem("subscriptions:{$topic}", $subscriptionId);
        $this->redis->hdel("handlers", $subscriptionId);
    }
    
    private function notifySubscribers($topic, $payload)
    {
        $subscribers = $this->redis->smembers("subscriptions:{$topic}");
        
        foreach ($subscribers as $subscriptionId) {
            $handler = unserialize($this->redis->hget("handlers", $subscriptionId));
            
            try {
                $handler($payload);
            } catch (\Exception $e) {
                $this->handleError($payload, $e);
            }
        }
    }
    
    private function handleError($payload, $exception)
    {
        $payload['retry_count']++;
        $payload['last_error'] = $exception->getMessage();
        
        if ($payload['retry_count'] < 3) {
            // Retry with exponential backoff
            $delay = pow(2, $payload['retry_count']) * 1000; // milliseconds
            $this->redis->zadd("retry_queue", time() + $delay, json_encode($payload));
        } else {
            // Move to dead letter queue
            $this->redis->lpush("dead_letter_queue", json_encode($payload));
        }
    }
    
    public function processRetryQueue()
    {
        $now = time();
        $retries = $this->redis->zrangebyscore("retry_queue", 0, $now);
        
        foreach ($retries as $payload) {
            $data = json_decode($payload, true);
            $this->redis->zrem("retry_queue", $payload);
            $this->notifySubscribers($data['topic'], $data);
        }
    }
}
```

## Projections and Event Replay
```php
<?php

namespace App\EventDriven\Projections;

use TuskLang\Config;

class ProjectionManager
{
    private $config;
    private $eventStore;
    private $projections = [];
    
    public function __construct(EventStore $eventStore)
    {
        $this->config = new Config();
        $this->eventStore = $eventStore;
        $this->loadProjections();
    }
    
    public function replay($projectionName, $fromEventId = 0)
    {
        if (!isset($this->projections[$projectionName])) {
            throw new \Exception("Projection not found: {$projectionName}");
        }
        
        $projection = $this->projections[$projectionName];
        
        // Get events from event store
        $events = $this->eventStore->getEventsByType($projection->getEventTypes(), 1000, $fromEventId);
        
        foreach ($events as $event) {
            $projection->handle($event);
        }
        
        return count($events);
    }
    
    public function handleEvent($event)
    {
        foreach ($this->projections as $projection) {
            if (in_array($event->getEventType(), $projection->getEventTypes())) {
                $projection->handle($event);
            }
        }
    }
    
    private function loadProjections()
    {
        $projections = $this->config->get('projections', []);
        
        foreach ($projections as $name => $config) {
            $this->projections[$name] = new $config['class']();
        }
    }
}

class OrderProjection
{
    private $pdo;
    
    public function __construct()
    {
        $this->pdo = new PDO(Env::get('READ_DB_DSN'));
    }
    
    public function getEventTypes()
    {
        return ['OrderCreated', 'OrderCancelled', 'PaymentProcessed'];
    }
    
    public function handle($event)
    {
        switch ($event->getEventType()) {
            case 'OrderCreated':
                $this->handleOrderCreated($event);
                break;
                
            case 'OrderCancelled':
                $this->handleOrderCancelled($event);
                break;
                
            case 'PaymentProcessed':
                $this->handlePaymentProcessed($event);
                break;
        }
    }
    
    private function handleOrderCreated($event)
    {
        $data = $event->getData();
        
        $stmt = $this->pdo->prepare("
            INSERT INTO orders (id, customer_id, total, status, created_at)
            VALUES (?, ?, ?, ?, ?)
        ");
        
        $stmt->execute([
            $data['order_id'],
            $data['customer_id'],
            $data['total'],
            $data['status'],
            $event->getOccurredOn()->format('Y-m-d H:i:s')
        ]);
    }
    
    private function handleOrderCancelled($event)
    {
        $data = $event->getData();
        
        $stmt = $this->pdo->prepare("
            UPDATE orders
            SET status = ?, cancelled_at = ?
            WHERE id = ?
        ");
        
        $stmt->execute([
            'cancelled',
            $event->getOccurredOn()->format('Y-m-d H:i:s'),
            $data['order_id']
        ]);
    }
    
    private function handlePaymentProcessed($event)
    {
        $data = $event->getData();
        
        $stmt = $this->pdo->prepare("
            UPDATE orders
            SET status = ?, payment_processed_at = ?
            WHERE id = ?
        ");
        
        $stmt->execute([
            'paid',
            $event->getOccurredOn()->format('Y-m-d H:i:s'),
            $data['order_id']
        ]);
    }
}
```

## Saga Pattern Implementation
```php
<?php

namespace App\EventDriven\Sagas;

use TuskLang\Config;

class SagaManager
{
    private $config;
    private $sagas = [];
    private $messageQueue;
    
    public function __construct(MessageQueue $messageQueue)
    {
        $this->config = new Config();
        $this->messageQueue = $messageQueue;
        $this->loadSagas();
    }
    
    public function startSaga($sagaType, $data)
    {
        if (!isset($this->sagas[$sagaType])) {
            throw new \Exception("Saga not found: {$sagaType}");
        }
        
        $saga = $this->sagas[$sagaType];
        $sagaId = uniqid("saga_");
        
        $saga->start($sagaId, $data);
        
        return $sagaId;
    }
    
    public function handleEvent($event)
    {
        foreach ($this->sagas as $saga) {
            if ($saga->canHandle($event)) {
                $saga->handle($event);
            }
        }
    }
    
    private function loadSagas()
    {
        $sagas = $this->config->get('sagas', []);
        
        foreach ($sagas as $name => $config) {
            $this->sagas[$name] = new $config['class']($this->messageQueue);
        }
    }
}

class OrderSaga
{
    private $messageQueue;
    private $state = [];
    
    public function __construct(MessageQueue $messageQueue)
    {
        $this->messageQueue = $messageQueue;
    }
    
    public function start($sagaId, $data)
    {
        $this->state[$sagaId] = [
            'status' => 'started',
            'data' => $data,
            'compensations' => []
        ];
        
        // Start the saga by reserving inventory
        $this->messageQueue->publish('inventory', new ReserveInventoryCommand($data['items']));
    }
    
    public function canHandle($event)
    {
        return in_array($event->getEventType(), [
            'InventoryReserved',
            'InventoryReservationFailed',
            'PaymentProcessed',
            'PaymentFailed'
        ]);
    }
    
    public function handle($event)
    {
        switch ($event->getEventType()) {
            case 'InventoryReserved':
                $this->handleInventoryReserved($event);
                break;
                
            case 'InventoryReservationFailed':
                $this->handleInventoryReservationFailed($event);
                break;
                
            case 'PaymentProcessed':
                $this->handlePaymentProcessed($event);
                break;
                
            case 'PaymentFailed':
                $this->handlePaymentFailed($event);
                break;
        }
    }
    
    private function handleInventoryReserved($event)
    {
        $sagaId = $event->getSagaId();
        $this->state[$sagaId]['status'] = 'inventory_reserved';
        
        // Add compensation action
        $this->state[$sagaId]['compensations'][] = [
            'action' => 'release_inventory',
            'data' => $event->getData()
        ];
        
        // Proceed to payment
        $this->messageQueue->publish('payment', new ProcessPaymentCommand($event->getData()));
    }
    
    private function handleInventoryReservationFailed($event)
    {
        $sagaId = $event->getSagaId();
        $this->state[$sagaId]['status'] = 'failed';
        
        // Cancel the order
        $this->messageQueue->publish('orders', new CancelOrderCommand($event->getData()));
    }
    
    private function handlePaymentProcessed($event)
    {
        $sagaId = $event->getSagaId();
        $this->state[$sagaId]['status'] = 'completed';
        
        // Confirm the order
        $this->messageQueue->publish('orders', new ConfirmOrderCommand($event->getData()));
    }
    
    private function handlePaymentFailed($event)
    {
        $sagaId = $event->getSagaId();
        $this->state[$sagaId]['status'] = 'failed';
        
        // Compensate by releasing inventory
        $this->compensate($sagaId);
        
        // Cancel the order
        $this->messageQueue->publish('orders', new CancelOrderCommand($event->getData()));
    }
    
    private function compensate($sagaId)
    {
        $compensations = array_reverse($this->state[$sagaId]['compensations']);
        
        foreach ($compensations as $compensation) {
            switch ($compensation['action']) {
                case 'release_inventory':
                    $this->messageQueue->publish('inventory', new ReleaseInventoryCommand($compensation['data']));
                    break;
            }
        }
    }
}
```

## Best Practices
- **Use event sourcing for audit trails**
- **Implement CQRS for read/write separation**
- **Use message queues for decoupling**
- **Implement proper error handling**
- **Use saga patterns for distributed transactions**
- **Version your events**

## Performance Optimization
- **Use event snapshots for large aggregates**
- **Implement event batching**
- **Use efficient projections**
- **Optimize event replay**

## Security Considerations
- **Validate all events**
- **Implement proper access controls**
- **Use encryption for sensitive data**
- **Audit event access**

## Troubleshooting
- **Monitor event processing**
- **Check projection consistency**
- **Verify saga completion**
- **Monitor message queue health**

## Conclusion
TuskLang + PHP = event-driven systems that are scalable, decoupled, and reliable. Build systems that handle complexity with grace. 