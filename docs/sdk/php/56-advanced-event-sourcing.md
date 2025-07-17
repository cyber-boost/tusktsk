# Advanced Event Sourcing in PHP with TuskLang

## Overview

TuskLang revolutionizes event sourcing by making it configuration-driven, intelligent, and adaptive. This guide covers advanced event sourcing patterns that leverage TuskLang's dynamic capabilities for comprehensive event management and state reconstruction.

## 🎯 Event Sourcing Architecture

### Event Sourcing Configuration

```ini
# event-sourcing.tsk
[event_sourcing]
enabled = true
store = "eventstore"
snapshots = true
projections = true

[event_sourcing.store]
type = "eventstore"
connection = {
    host = "localhost",
    port = 2113,
    username = "admin",
    password = "changeit"
}

[event_sourcing.events]
versioning = true
serialization = "json"
compression = true
encryption = false

[event_sourcing.snapshots]
enabled = true
frequency = 100
storage = "separate"
compression = true

[event_sourcing.projections]
enabled = true
real_time = true
batch_processing = true
error_handling = true

[event_sourcing.replay]
enabled = true
parallel_processing = true
checkpointing = true
```

### Event Sourcing Manager Implementation

```php
<?php
// EventSourcingManager.php
class EventSourcingManager
{
    private $config;
    private $eventStore;
    private $snapshotStore;
    private $projectionManager;
    private $eventBus;
    
    public function __construct()
    {
        $this->config = new TuskConfig('event-sourcing.tsk');
        $this->eventStore = new EventStore();
        $this->snapshotStore = new SnapshotStore();
        $this->projectionManager = new ProjectionManager();
        $this->eventBus = new EventBus();
        $this->initializeEventSourcing();
    }
    
    private function initializeEventSourcing()
    {
        $storeConfig = $this->config->get('event_sourcing.store');
        
        // Initialize event store
        $this->eventStore->connect($storeConfig['connection']);
        
        // Initialize snapshot store
        if ($this->config->get('event_sourcing.snapshots.enabled')) {
            $this->snapshotStore->initialize($storeConfig['connection']);
        }
        
        // Initialize projections
        if ($this->config->get('event_sourcing.projections.enabled')) {
            $this->projectionManager->initialize();
        }
    }
    
    public function saveEvents($aggregateId, $events, $expectedVersion = null)
    {
        $startTime = microtime(true);
        
        try {
            // Validate events
            $this->validateEvents($events);
            
            // Save events to store
            $savedEvents = $this->eventStore->saveEvents($aggregateId, $events, $expectedVersion);
            
            // Create snapshot if needed
            if ($this->shouldCreateSnapshot($aggregateId)) {
                $this->createSnapshot($aggregateId);
            }
            
            // Publish events
            $this->publishEvents($savedEvents);
            
            // Update projections
            $this->updateProjections($savedEvents);
            
            $duration = (microtime(true) - $startTime) * 1000;
            $this->logEventSourcingOperation('save_events', $aggregateId, $duration);
            
            return $savedEvents;
            
        } catch (Exception $e) {
            $this->handleEventSourcingError('save_events', $aggregateId, $e);
            throw $e;
        }
    }
    
    public function getEvents($aggregateId, $fromVersion = 0, $toVersion = null)
    {
        $startTime = microtime(true);
        
        try {
            // Try to get from snapshot first
            $snapshot = $this->getSnapshot($aggregateId);
            
            if ($snapshot && $snapshot['version'] >= $fromVersion) {
                $events = $this->eventStore->getEvents($aggregateId, $snapshot['version'] + 1, $toVersion);
                $events = array_merge($snapshot['state'], $events);
            } else {
                $events = $this->eventStore->getEvents($aggregateId, $fromVersion, $toVersion);
            }
            
            $duration = (microtime(true) - $startTime) * 1000;
            $this->logEventSourcingOperation('get_events', $aggregateId, $duration);
            
            return $events;
            
        } catch (Exception $e) {
            $this->handleEventSourcingError('get_events', $aggregateId, $e);
            throw $e;
        }
    }
    
    public function replayEvents($aggregateId, $handler)
    {
        $startTime = microtime(true);
        
        try {
            $events = $this->getEvents($aggregateId);
            $state = null;
            
            foreach ($events as $event) {
                $state = $handler->handle($event, $state);
            }
            
            $duration = (microtime(true) - $startTime) * 1000;
            $this->logEventSourcingOperation('replay_events', $aggregateId, $duration);
            
            return $state;
            
        } catch (Exception $e) {
            $this->handleEventSourcingError('replay_events', $aggregateId, $e);
            throw $e;
        }
    }
    
    public function createProjection($name, $handler, $options = [])
    {
        if (!$this->config->get('event_sourcing.projections.enabled')) {
            throw new EventSourcingException("Projections not enabled");
        }
        
        return $this->projectionManager->createProjection($name, $handler, $options);
    }
    
    public function getProjection($name)
    {
        return $this->projectionManager->getProjection($name);
    }
    
    public function rebuildProjection($name)
    {
        return $this->projectionManager->rebuildProjection($name);
    }
    
    private function validateEvents($events)
    {
        foreach ($events as $event) {
            if (!$event instanceof DomainEvent) {
                throw new InvalidEventException("Event must implement DomainEvent interface");
            }
            
            if (!$event->getAggregateId()) {
                throw new InvalidEventException("Event must have aggregate ID");
            }
        }
    }
    
    private function shouldCreateSnapshot($aggregateId)
    {
        if (!$this->config->get('event_sourcing.snapshots.enabled')) {
            return false;
        }
        
        $eventCount = $this->eventStore->getEventCount($aggregateId);
        $frequency = $this->config->get('event_sourcing.snapshots.frequency');
        
        return $eventCount % $frequency === 0;
    }
    
    private function createSnapshot($aggregateId)
    {
        $events = $this->eventStore->getEvents($aggregateId);
        $state = $this->reconstructState($aggregateId, $events);
        
        $snapshot = [
            'aggregate_id' => $aggregateId,
            'version' => count($events),
            'state' => $state,
            'created_at' => time()
        ];
        
        $this->snapshotStore->saveSnapshot($snapshot);
    }
    
    private function getSnapshot($aggregateId)
    {
        return $this->snapshotStore->getSnapshot($aggregateId);
    }
    
    private function reconstructState($aggregateId, $events)
    {
        // This would typically use an aggregate factory or handler
        $aggregate = $this->createAggregate($aggregateId);
        
        foreach ($events as $event) {
            $aggregate->apply($event);
        }
        
        return $aggregate->getState();
    }
    
    private function createAggregate($aggregateId)
    {
        // Factory method to create appropriate aggregate
        return new UserAggregate($aggregateId);
    }
    
    private function publishEvents($events)
    {
        foreach ($events as $event) {
            $this->eventBus->publish($event);
        }
    }
    
    private function updateProjections($events)
    {
        if (!$this->config->get('event_sourcing.projections.enabled')) {
            return;
        }
        
        foreach ($events as $event) {
            $this->projectionManager->handleEvent($event);
        }
    }
    
    private function logEventSourcingOperation($operation, $aggregateId, $duration)
    {
        $logEntry = [
            'operation' => $operation,
            'aggregate_id' => $aggregateId,
            'duration' => $duration,
            'timestamp' => time()
        ];
        
        error_log(json_encode($logEntry));
    }
    
    private function handleEventSourcingError($operation, $aggregateId, $exception)
    {
        $errorEntry = [
            'operation' => $operation,
            'aggregate_id' => $aggregateId,
            'error' => $exception->getMessage(),
            'timestamp' => time()
        ];
        
        error_log(json_encode($errorEntry));
    }
}
```

## 📦 Event Store

### Event Store Configuration

```ini
# event-store.tsk
[event_store]
type = "eventstore"
connection = {
    host = "localhost",
    port = 2113,
    username = "admin",
    password = "changeit"
}

[event_store.streams]
naming = "category-aggregateid"
partitioning = true
retention = "unlimited"

[event_store.events]
serialization = "json"
compression = true
encryption = false
metadata = true

[event_store.performance]
batch_size = 100
connection_pool = 10
timeout = 30
```

### Event Store Implementation

```php
class EventStore
{
    private $config;
    private $connection;
    private $serializer;
    
    public function __construct()
    {
        $this->config = new TuskConfig('event-store.tsk');
        $this->serializer = new EventSerializer();
    }
    
    public function connect($connectionConfig)
    {
        $this->connection = new EventStoreConnection($connectionConfig);
        $this->connection->connect();
    }
    
    public function saveEvents($aggregateId, $events, $expectedVersion = null)
    {
        $streamName = $this->getStreamName($aggregateId);
        $savedEvents = [];
        
        foreach ($events as $event) {
            $eventData = $this->serializeEvent($event);
            $eventNumber = $this->connection->appendToStream($streamName, $eventData, $expectedVersion);
            
            $savedEvent = [
                'id' => $event->getId(),
                'aggregate_id' => $aggregateId,
                'event_number' => $eventNumber,
                'event_type' => get_class($event),
                'data' => $eventData,
                'metadata' => $event->getMetadata(),
                'created_at' => time()
            ];
            
            $savedEvents[] = $savedEvent;
            
            if ($expectedVersion !== null) {
                $expectedVersion++;
            }
        }
        
        return $savedEvents;
    }
    
    public function getEvents($aggregateId, $fromVersion = 0, $toVersion = null)
    {
        $streamName = $this->getStreamName($aggregateId);
        $events = $this->connection->readStreamEvents($streamName, $fromVersion, $toVersion);
        
        return array_map(function($event) {
            return $this->deserializeEvent($event);
        }, $events);
    }
    
    public function getEventCount($aggregateId)
    {
        $streamName = $this->getStreamName($aggregateId);
        return $this->connection->getStreamEventCount($streamName);
    }
    
    public function deleteStream($aggregateId)
    {
        $streamName = $this->getStreamName($aggregateId);
        return $this->connection->deleteStream($streamName);
    }
    
    public function subscribeToStream($aggregateId, $callback)
    {
        $streamName = $this->getStreamName($aggregateId);
        return $this->connection->subscribeToStream($streamName, $callback);
    }
    
    public function subscribeToAll($callback)
    {
        return $this->connection->subscribeToAll($callback);
    }
    
    private function getStreamName($aggregateId)
    {
        $naming = $this->config->get('event_store.streams.naming');
        
        if ($naming === 'category-aggregateid') {
            $category = $this->getCategory($aggregateId);
            return "{$category}-{$aggregateId}";
        }
        
        return $aggregateId;
    }
    
    private function getCategory($aggregateId)
    {
        // Extract category from aggregate ID or use default
        if (strpos($aggregateId, '-') !== false) {
            return explode('-', $aggregateId)[0];
        }
        
        return 'default';
    }
    
    private function serializeEvent($event)
    {
        $serialization = $this->config->get('event_store.events.serialization');
        
        switch ($serialization) {
            case 'json':
                return json_encode($event);
            case 'php':
                return serialize($event);
            default:
                throw new InvalidArgumentException("Unknown serialization format: {$serialization}");
        }
    }
    
    private function deserializeEvent($eventData)
    {
        $serialization = $this->config->get('event_store.events.serialization');
        
        switch ($serialization) {
            case 'json':
                return json_decode($eventData, true);
            case 'php':
                return unserialize($eventData);
            default:
                throw new InvalidArgumentException("Unknown serialization format: {$serialization}");
        }
    }
}

class EventStoreConnection
{
    private $connection;
    private $config;
    
    public function __construct($config)
    {
        $this->config = $config;
    }
    
    public function connect()
    {
        $this->connection = new \EventStore\EventStoreConnection([
            'host' => $this->config['host'],
            'port' => $this->config['port'],
            'username' => $this->config['username'],
            'password' => $this->config['password']
        ]);
    }
    
    public function appendToStream($streamName, $eventData, $expectedVersion = null)
    {
        $event = new \EventStore\EventData(
            uniqid(),
            'event',
            true,
            $eventData,
            []
        );
        
        $result = $this->connection->appendToStream(
            $streamName,
            $expectedVersion ?? \EventStore\ExpectedVersion::ANY,
            [$event]
        );
        
        return $result->nextExpectedVersion();
    }
    
    public function readStreamEvents($streamName, $fromVersion, $toVersion)
    {
        $result = $this->connection->readStreamEventsForward(
            $streamName,
            $fromVersion,
            $toVersion ?? 1000,
            false
        );
        
        $events = [];
        foreach ($result->events() as $event) {
            $events[] = [
                'id' => $event->eventId(),
                'event_number' => $event->eventNumber(),
                'event_type' => $event->eventType(),
                'data' => $event->data(),
                'metadata' => $event->metadata(),
                'created' => $event->created()
            ];
        }
        
        return $events;
    }
    
    public function getStreamEventCount($streamName)
    {
        $result = $this->connection->readStreamEventsBackward(
            $streamName,
            \EventStore\StreamPosition::END,
            1,
            false
        );
        
        if (empty($result->events())) {
            return 0;
        }
        
        return $result->events()[0]->eventNumber() + 1;
    }
    
    public function deleteStream($streamName)
    {
        return $this->connection->deleteStream($streamName, \EventStore\ExpectedVersion::ANY);
    }
    
    public function subscribeToStream($streamName, $callback)
    {
        return $this->connection->subscribeToStream($streamName, $callback);
    }
    
    public function subscribeToAll($callback)
    {
        return $this->connection->subscribeToAll($callback);
    }
}
```

## 📸 Snapshot Store

### Snapshot Store Configuration

```ini
# snapshot-store.tsk
[snapshot_store]
enabled = true
storage = "separate"
compression = true

[snapshot_store.storage]
type = "redis"
connection = {
    host = "localhost",
    port = 6379,
    database = 1
}

[snapshot_store.snapshots]
retention = 10
compression = true
encryption = false
```

### Snapshot Store Implementation

```php
class SnapshotStore
{
    private $config;
    private $storage;
    private $serializer;
    
    public function __construct()
    {
        $this->config = new TuskConfig('snapshot-store.tsk');
        $this->serializer = new SnapshotSerializer();
    }
    
    public function initialize($connectionConfig)
    {
        $storageType = $this->config->get('snapshot_store.storage.type');
        
        switch ($storageType) {
            case 'redis':
                $this->storage = new RedisStorage($connectionConfig);
                break;
            case 'database':
                $this->storage = new DatabaseStorage($connectionConfig);
                break;
            case 'file':
                $this->storage = new FileStorage($connectionConfig);
                break;
            default:
                throw new InvalidArgumentException("Unknown storage type: {$storageType}");
        }
    }
    
    public function saveSnapshot($snapshot)
    {
        $key = $this->getSnapshotKey($snapshot['aggregate_id']);
        $data = $this->serializeSnapshot($snapshot);
        
        return $this->storage->set($key, $data);
    }
    
    public function getSnapshot($aggregateId)
    {
        $key = $this->getSnapshotKey($aggregateId);
        $data = $this->storage->get($key);
        
        if (!$data) {
            return null;
        }
        
        return $this->deserializeSnapshot($data);
    }
    
    public function deleteSnapshot($aggregateId)
    {
        $key = $this->getSnapshotKey($aggregateId);
        return $this->storage->delete($key);
    }
    
    public function cleanupSnapshots()
    {
        $retention = $this->config->get('snapshot_store.snapshots.retention');
        $snapshots = $this->storage->getAllSnapshots();
        
        foreach ($snapshots as $snapshot) {
            $snapshotData = $this->deserializeSnapshot($snapshot);
            
            if ($this->shouldDeleteSnapshot($snapshotData, $retention)) {
                $this->deleteSnapshot($snapshotData['aggregate_id']);
            }
        }
    }
    
    private function getSnapshotKey($aggregateId)
    {
        return "snapshot:{$aggregateId}";
    }
    
    private function serializeSnapshot($snapshot)
    {
        $data = json_encode($snapshot);
        
        if ($this->config->get('snapshot_store.snapshots.compression')) {
            $data = gzcompress($data);
        }
        
        return $data;
    }
    
    private function deserializeSnapshot($data)
    {
        if ($this->config->get('snapshot_store.snapshots.compression')) {
            $data = gzuncompress($data);
        }
        
        return json_decode($data, true);
    }
    
    private function shouldDeleteSnapshot($snapshot, $retention)
    {
        $age = time() - $snapshot['created_at'];
        $maxAge = $retention * 24 * 60 * 60; // Convert days to seconds
        
        return $age > $maxAge;
    }
}

class RedisStorage
{
    private $redis;
    
    public function __construct($config)
    {
        $this->redis = new Redis();
        $this->redis->connect($config['host'], $config['port']);
        $this->redis->select($config['database']);
    }
    
    public function set($key, $value)
    {
        return $this->redis->set($key, $value);
    }
    
    public function get($key)
    {
        return $this->redis->get($key);
    }
    
    public function delete($key)
    {
        return $this->redis->del($key);
    }
    
    public function getAllSnapshots()
    {
        $keys = $this->redis->keys('snapshot:*');
        $snapshots = [];
        
        foreach ($keys as $key) {
            $snapshots[] = $this->redis->get($key);
        }
        
        return $snapshots;
    }
}
```

## 📊 Projections

### Projections Configuration

```ini
# projections.tsk
[projections]
enabled = true
real_time = true
batch_processing = true

[projections.storage]
type = "redis"
connection = {
    host = "localhost",
    port = 6379,
    database = 2
}

[projections.processing]
parallel = true
batch_size = 100
error_handling = true
```

### Projections Implementation

```php
class ProjectionManager
{
    private $config;
    private $storage;
    private $projections = [];
    private $eventHandlers = [];
    
    public function __construct()
    {
        $this->config = new TuskConfig('projections.tsk');
        $this->storage = new ProjectionStorage();
    }
    
    public function initialize()
    {
        $storageType = $this->config->get('projections.storage.type');
        $connection = $this->config->get('projections.storage.connection');
        
        $this->storage->connect($storageType, $connection);
    }
    
    public function createProjection($name, $handler, $options = [])
    {
        $projection = [
            'name' => $name,
            'handler' => $handler,
            'options' => $options,
            'state' => [],
            'last_event_number' => 0,
            'created_at' => time()
        ];
        
        $this->projections[$name] = $projection;
        $this->storage->saveProjection($projection);
        
        return $projection;
    }
    
    public function getProjection($name)
    {
        if (!isset($this->projections[$name])) {
            $projection = $this->storage->getProjection($name);
            if ($projection) {
                $this->projections[$name] = $projection;
            }
        }
        
        return $this->projections[$name] ?? null;
    }
    
    public function handleEvent($event)
    {
        if (!$this->config->get('projections.real_time')) {
            return;
        }
        
        foreach ($this->projections as $name => $projection) {
            try {
                $this->processEventForProjection($projection, $event);
            } catch (Exception $e) {
                $this->handleProjectionError($name, $event, $e);
            }
        }
    }
    
    public function rebuildProjection($name)
    {
        $projection = $this->getProjection($name);
        
        if (!$projection) {
            throw new ProjectionNotFoundException("Projection not found: {$name}");
        }
        
        // Reset projection state
        $projection['state'] = [];
        $projection['last_event_number'] = 0;
        
        // Replay all events
        $this->replayEventsForProjection($projection);
        
        // Save updated projection
        $this->storage->saveProjection($projection);
        
        return $projection;
    }
    
    private function processEventForProjection($projection, $event)
    {
        $handler = $projection['handler'];
        
        if (is_callable($handler)) {
            $projection['state'] = $handler($event, $projection['state']);
        } elseif (is_object($handler) && method_exists($handler, 'handle')) {
            $projection['state'] = $handler->handle($event, $projection['state']);
        }
        
        $projection['last_event_number'] = $event['event_number'];
        
        $this->storage->saveProjection($projection);
    }
    
    private function replayEventsForProjection($projection)
    {
        // Get all events from event store
        $events = $this->getAllEvents();
        
        foreach ($events as $event) {
            $this->processEventForProjection($projection, $event);
        }
    }
    
    private function getAllEvents()
    {
        // This would typically get events from the event store
        // For now, return empty array
        return [];
    }
    
    private function handleProjectionError($projectionName, $event, $exception)
    {
        if (!$this->config->get('projections.processing.error_handling')) {
            throw $exception;
        }
        
        $errorEntry = [
            'projection' => $projectionName,
            'event' => $event,
            'error' => $exception->getMessage(),
            'timestamp' => time()
        ];
        
        error_log(json_encode($errorEntry));
    }
}

// Example Projection Handlers
class UserProjectionHandler
{
    public function handle($event, $state)
    {
        switch ($event['event_type']) {
            case 'UserCreated':
                return $this->handleUserCreated($event, $state);
            case 'UserUpdated':
                return $this->handleUserUpdated($event, $state);
            case 'UserDeleted':
                return $this->handleUserDeleted($event, $state);
            default:
                return $state;
        }
    }
    
    private function handleUserCreated($event, $state)
    {
        $userId = $event['data']['user_id'];
        $state[$userId] = [
            'id' => $userId,
            'email' => $event['data']['email'],
            'name' => $event['data']['name'],
            'created_at' => $event['created_at'],
            'status' => 'active'
        ];
        
        return $state;
    }
    
    private function handleUserUpdated($event, $state)
    {
        $userId = $event['data']['user_id'];
        
        if (isset($state[$userId])) {
            $state[$userId] = array_merge($state[$userId], $event['data']);
            $state[$userId]['updated_at'] = $event['created_at'];
        }
        
        return $state;
    }
    
    private function handleUserDeleted($event, $state)
    {
        $userId = $event['data']['user_id'];
        
        if (isset($state[$userId])) {
            $state[$userId]['status'] = 'deleted';
            $state[$userId]['deleted_at'] = $event['created_at'];
        }
        
        return $state;
    }
}

class OrderProjectionHandler
{
    public function handle($event, $state)
    {
        switch ($event['event_type']) {
            case 'OrderCreated':
                return $this->handleOrderCreated($event, $state);
            case 'OrderPaid':
                return $this->handleOrderPaid($event, $state);
            case 'OrderShipped':
                return $this->handleOrderShipped($event, $state);
            default:
                return $state;
        }
    }
    
    private function handleOrderCreated($event, $state)
    {
        $orderId = $event['data']['order_id'];
        $state[$orderId] = [
            'id' => $orderId,
            'user_id' => $event['data']['user_id'],
            'amount' => $event['data']['amount'],
            'status' => 'created',
            'created_at' => $event['created_at']
        ];
        
        return $state;
    }
    
    private function handleOrderPaid($event, $state)
    {
        $orderId = $event['data']['order_id'];
        
        if (isset($state[$orderId])) {
            $state[$orderId]['status'] = 'paid';
            $state[$orderId]['paid_at'] = $event['created_at'];
        }
        
        return $state;
    }
    
    private function handleOrderShipped($event, $state)
    {
        $orderId = $event['data']['order_id'];
        
        if (isset($state[$orderId])) {
            $state[$orderId]['status'] = 'shipped';
            $state[$orderId]['shipped_at'] = $event['created_at'];
        }
        
        return $state;
    }
}
```

## 📋 Best Practices

### Event Sourcing Best Practices

1. **Event Design**: Design events to be immutable and descriptive
2. **Aggregate Boundaries**: Define clear aggregate boundaries
3. **Event Versioning**: Implement event versioning for schema evolution
4. **Snapshots**: Use snapshots for performance optimization
5. **Projections**: Create read-optimized projections
6. **Event Replay**: Ensure events can be replayed correctly
7. **Error Handling**: Implement robust error handling
8. **Monitoring**: Monitor event store performance and health

### Integration Examples

```php
// Event Sourcing Integration
class EventSourcingIntegration
{
    private $eventSourcingManager;
    private $userRepository;
    private $orderRepository;
    
    public function __construct()
    {
        $this->eventSourcingManager = new EventSourcingManager();
        $this->userRepository = new UserRepository($this->eventSourcingManager);
        $this->orderRepository = new OrderRepository($this->eventSourcingManager);
        
        $this->initializeProjections();
    }
    
    private function initializeProjections()
    {
        // Create user projection
        $this->eventSourcingManager->createProjection(
            'users',
            new UserProjectionHandler()
        );
        
        // Create order projection
        $this->eventSourcingManager->createProjection(
            'orders',
            new OrderProjectionHandler()
        );
    }
    
    public function createUser($userData)
    {
        $user = new UserAggregate(uniqid());
        $user->create($userData['email'], $userData['name']);
        
        $events = $user->getUncommittedEvents();
        $this->eventSourcingManager->saveEvents($user->getId(), $events);
        
        return $user->getId();
    }
    
    public function getUser($userId)
    {
        $projection = $this->eventSourcingManager->getProjection('users');
        return $projection['state'][$userId] ?? null;
    }
    
    public function createOrder($orderData)
    {
        $order = new OrderAggregate(uniqid());
        $order->create($orderData['user_id'], $orderData['items']);
        
        $events = $order->getUncommittedEvents();
        $this->eventSourcingManager->saveEvents($order->getId(), $events);
        
        return $order->getId();
    }
    
    public function getOrder($orderId)
    {
        $projection = $this->eventSourcingManager->getProjection('orders');
        return $projection['state'][$orderId] ?? null;
    }
    
    public function replayEvents($aggregateId)
    {
        return $this->eventSourcingManager->replayEvents($aggregateId, new EventHandler());
    }
}
```

## 🔧 Troubleshooting

### Common Issues

1. **Event Store Performance**: Use snapshots and optimize projections
2. **Memory Usage**: Implement pagination and streaming
3. **Event Replay**: Ensure events are idempotent and deterministic
4. **Schema Evolution**: Use event versioning and migration strategies
5. **Projection Consistency**: Implement error handling and retry mechanisms

### Debug Configuration

```ini
# debug-event-sourcing.tsk
[debug]
enabled = true
log_level = "verbose"
trace_events = true

[debug.output]
console = true
file = "/var/log/tusk-event-sourcing-debug.log"
```

This comprehensive event sourcing system leverages TuskLang's configuration-driven approach to create intelligent, adaptive event management solutions that ensure reliable, scalable, and maintainable event-driven architectures. 