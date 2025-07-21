# Event Sourcing with TuskLang

TuskLang revolutionizes event sourcing by providing configuration-driven event stores, CQRS patterns, and event-driven architectures with seamless integration and powerful querying capabilities.

## Overview

TuskLang's event sourcing capabilities combine the power of event-driven architecture with the simplicity of configuration-driven development, enabling robust, scalable, and auditable systems.

```php
// Event Sourcing Configuration
event_sourcing = {
    event_store = {
        type = "database"
        connection = @env(DATABASE_URL)
        table_prefix = "events_"
        partitioning = {
            strategy = "by_date"
            partition_size = "monthly"
        }
        
        serialization = {
            format = "json"
            compression = "gzip"
            versioning = true
        }
        
        indexing = {
            event_type = true
            aggregate_id = true
            timestamp = true
            metadata = true
        }
    }
    
    aggregates = {
        user = {
            event_types = ["UserCreated", "UserUpdated", "UserDeleted"]
            snapshot_frequency = 100
            projection_tables = ["users", "user_profiles"]
        }
        
        order = {
            event_types = ["OrderCreated", "OrderItemAdded", "OrderShipped", "OrderCancelled"]
            snapshot_frequency = 50
            projection_tables = ["orders", "order_items", "order_status"]
        }
        
        product = {
            event_types = ["ProductCreated", "PriceChanged", "StockUpdated"]
            snapshot_frequency = 200
            projection_tables = ["products", "product_prices", "inventory"]
        }
    }
    
    projections = {
        real_time = true
        batch_processing = true
        materialized_views = true
        read_models = {
            optimized_queries = true
            denormalization = true
        }
    }
}
```

## Core Event Sourcing Features

### 1. Event Store Management

```php
// Event Store Configuration
event_store_config = {
    storage = {
        primary = {
            type = "postgresql"
            connection = @env(EVENT_STORE_DB_URL)
            schema = "events"
        }
        
        archive = {
            type = "s3"
            bucket = "event-archive"
            retention_policy = "7 years"
        }
        
        cache = {
            type = "redis"
            ttl = "1 hour"
            max_memory = "2GB"
        }
    }
    
    event_handling = {
        validation = true
        deduplication = true
        ordering = "strict"
        concurrency_control = "optimistic"
    }
    
    performance = {
        batch_size = 1000
        parallel_processing = true
        connection_pooling = true
    }
}

// Event Store Implementation
class EventStore {
    private $config;
    private $connection;
    private $cache;
    
    public function __construct($configPath) {
        $this->config = $this->loadConfig($configPath);
        $this->connection = new DatabaseConnection($this->config->event_store->connection);
        $this->cache = new RedisCache($this->config->event_store_config->storage->cache);
    }
    
    public function appendEvents($aggregateId, $events, $expectedVersion = null) {
        $this->connection->beginTransaction();
        
        try {
            // Check concurrency
            if ($expectedVersion !== null) {
                $currentVersion = $this->getCurrentVersion($aggregateId);
                if ($currentVersion !== $expectedVersion) {
                    throw new ConcurrencyException("Version mismatch");
                }
            }
            
            // Store events
            foreach ($events as $event) {
                $this->storeEvent($aggregateId, $event);
            }
            
            // Update projections
            $this->updateProjections($aggregateId, $events);
            
            $this->connection->commit();
            
            // Publish events
            $this->publishEvents($events);
            
        } catch (Exception $e) {
            $this->connection->rollback();
            throw $e;
        }
    }
    
    public function getEvents($aggregateId, $fromVersion = 0, $toVersion = null) {
        $cacheKey = "events:{$aggregateId}:{$fromVersion}:{$toVersion}";
        
        // Try cache first
        $cached = $this->cache->get($cacheKey);
        if ($cached !== null) {
            return $cached;
        }
        
        // Query database
        $events = $this->queryEvents($aggregateId, $fromVersion, $toVersion);
        
        // Cache results
        $this->cache->set($cacheKey, $events, $this->config->event_store_config->storage->cache->ttl);
        
        return $events;
    }
    
    private function storeEvent($aggregateId, $event) {
        $sql = "
            INSERT INTO events (
                aggregate_id, 
                event_type, 
                event_data, 
                metadata, 
                version, 
                timestamp
            ) VALUES (?, ?, ?, ?, ?, ?)
        ";
        
        $this->connection->execute($sql, [
            $aggregateId,
            $event->getType(),
            json_encode($event->getData()),
            json_encode($event->getMetadata()),
            $event->getVersion(),
            $event->getTimestamp()
        ]);
    }
}
```

### 2. Aggregate Management

```php
// Aggregate Configuration
aggregate_config = {
    user_aggregate = {
        event_handlers = {
            UserCreated = "handleUserCreated"
            UserUpdated = "handleUserUpdated"
            UserDeleted = "handleUserDeleted"
        }
        
        business_rules = {
            email_uniqueness = true
            age_validation = "18+"
            status_transitions = ["active", "inactive", "suspended"]
        }
        
        invariants = {
            email_required = true
            username_unique = true
            status_valid = true
        }
    }
    
    order_aggregate = {
        event_handlers = {
            OrderCreated = "handleOrderCreated"
            OrderItemAdded = "handleOrderItemAdded"
            OrderShipped = "handleOrderShipped"
            OrderCancelled = "handleOrderCancelled"
        }
        
        business_rules = {
            minimum_order_amount = 10.00
            max_items_per_order = 100
            valid_status_transitions = {
                "created" = ["confirmed", "cancelled"]
                "confirmed" = ["shipped", "cancelled"]
                "shipped" = ["delivered", "returned"]
            }
        }
    }
}

// Aggregate Base Class
abstract class Aggregate {
    protected $id;
    protected $version = 0;
    protected $uncommittedEvents = [];
    protected $config;
    
    public function __construct($id, $configPath) {
        $this->id = $id;
        $this->config = $this->loadConfig($configPath);
    }
    
    public function apply($event) {
        $eventType = $event->getType();
        $handler = $this->config->aggregate_config->{$this->getAggregateName()}->event_handlers->$eventType;
        
        if (method_exists($this, $handler)) {
            $this->$handler($event);
        }
        
        $this->version++;
    }
    
    public function raiseEvent($eventType, $data, $metadata = []) {
        $event = new Event(
            $this->id,
            $eventType,
            $data,
            $metadata,
            $this->version + 1,
            new DateTime()
        );
        
        $this->apply($event);
        $this->uncommittedEvents[] = $event;
        
        return $event;
    }
    
    public function getUncommittedEvents() {
        return $this->uncommittedEvents;
    }
    
    public function markEventsAsCommitted() {
        $this->uncommittedEvents = [];
    }
    
    abstract protected function getAggregateName();
}

// User Aggregate Implementation
class UserAggregate extends Aggregate {
    private $email;
    private $username;
    private $status;
    private $profile;
    
    public function createUser($email, $username, $profile) {
        // Validate business rules
        $this->validateEmailUniqueness($email);
        $this->validateUsernameUniqueness($username);
        $this->validateProfile($profile);
        
        $this->raiseEvent('UserCreated', [
            'email' => $email,
            'username' => $username,
            'profile' => $profile
        ]);
    }
    
    public function updateUser($updates) {
        if ($this->status === 'deleted') {
            throw new DomainException("Cannot update deleted user");
        }
        
        $this->raiseEvent('UserUpdated', $updates);
    }
    
    public function deleteUser() {
        if ($this->status === 'deleted') {
            throw new DomainException("User already deleted");
        }
        
        $this->raiseEvent('UserDeleted', [
            'deleted_at' => new DateTime()
        ]);
    }
    
    private function handleUserCreated($event) {
        $data = $event->getData();
        $this->email = $data['email'];
        $this->username = $data['username'];
        $this->profile = $data['profile'];
        $this->status = 'active';
    }
    
    private function handleUserUpdated($event) {
        $data = $event->getData();
        
        if (isset($data['email'])) {
            $this->validateEmailUniqueness($data['email']);
            $this->email = $data['email'];
        }
        
        if (isset($data['username'])) {
            $this->validateUsernameUniqueness($data['username']);
            $this->username = $data['username'];
        }
        
        if (isset($data['profile'])) {
            $this->profile = array_merge($this->profile, $data['profile']);
        }
    }
    
    private function handleUserDeleted($event) {
        $this->status = 'deleted';
    }
    
    protected function getAggregateName() {
        return 'user_aggregate';
    }
}
```

### 3. CQRS Implementation

```php
// CQRS Configuration
cqrs_config = {
    commands = {
        handlers = {
            CreateUser = "CreateUserHandler"
            UpdateUser = "UpdateUserHandler"
            DeleteUser = "DeleteUserHandler"
        }
        
        validation = {
            input_validation = true
            business_rule_validation = true
            authorization = true
        }
    }
    
    queries = {
        handlers = {
            GetUser = "GetUserHandler"
            GetUsers = "GetUsersHandler"
            GetUserProfile = "GetUserProfileHandler"
        }
        
        optimization = {
            read_models = true
            caching = true
            indexing = true
        }
    }
    
    projections = {
        real_time = true
        eventual_consistency = true
        read_model_updates = "async"
    }
}

// Command Bus Implementation
class CommandBus {
    private $config;
    private $handlers = [];
    
    public function __construct($configPath) {
        $this->config = $this->loadConfig($configPath);
        $this->registerHandlers();
    }
    
    public function dispatch($command) {
        $commandType = get_class($command);
        $handlerClass = $this->config->cqrs_config->commands->handlers->$commandType;
        
        if (!isset($this->handlers[$handlerClass])) {
            throw new HandlerNotFoundException("Handler not found for command: {$commandType}");
        }
        
        $handler = $this->handlers[$handlerClass];
        
        // Validate command
        if ($this->config->cqrs_config->commands->validation->input_validation) {
            $this->validateCommand($command);
        }
        
        // Execute command
        return $handler->handle($command);
    }
    
    private function registerHandlers() {
        foreach ($this->config->cqrs_config->commands->handlers as $command => $handler) {
            $this->handlers[$handler] = new $handler();
        }
    }
}

// Query Bus Implementation
class QueryBus {
    private $config;
    private $handlers = [];
    private $cache;
    
    public function __construct($configPath) {
        $this->config = $this->loadConfig($configPath);
        $this->cache = new RedisCache();
        $this->registerHandlers();
    }
    
    public function dispatch($query) {
        $queryType = get_class($query);
        $handlerClass = $this->config->cqrs_config->queries->handlers->$queryType;
        
        if (!isset($this->handlers[$handlerClass])) {
            throw new HandlerNotFoundException("Handler not found for query: {$queryType}");
        }
        
        $handler = $this->handlers[$handlerClass];
        
        // Check cache first
        if ($this->config->cqrs_config->queries->optimization->caching) {
            $cacheKey = $this->generateCacheKey($query);
            $cached = $this->cache->get($cacheKey);
            if ($cached !== null) {
                return $cached;
            }
        }
        
        // Execute query
        $result = $handler->handle($query);
        
        // Cache result
        if ($this->config->cqrs_config->queries->optimization->caching) {
            $this->cache->set($cacheKey, $result, 3600);
        }
        
        return $result;
    }
}

// Command Handler Example
class CreateUserHandler {
    private $eventStore;
    private $userRepository;
    
    public function __construct() {
        $this->eventStore = new EventStore('config/event-sourcing.tsk');
        $this->userRepository = new UserRepository();
    }
    
    public function handle(CreateUserCommand $command) {
        // Validate business rules
        $this->validateBusinessRules($command);
        
        // Create aggregate
        $user = new UserAggregate($command->getUserId(), 'config/aggregates.tsk');
        $user->createUser($command->getEmail(), $command->getUsername(), $command->getProfile());
        
        // Store events
        $this->eventStore->appendEvents($user->getId(), $user->getUncommittedEvents());
        
        // Update read model
        $this->updateReadModel($user);
        
        return new UserCreatedEvent($user->getId());
    }
    
    private function validateBusinessRules($command) {
        // Check email uniqueness
        if ($this->userRepository->emailExists($command->getEmail())) {
            throw new DomainException("Email already exists");
        }
        
        // Check username uniqueness
        if ($this->userRepository->usernameExists($command->getUsername())) {
            throw new DomainException("Username already exists");
        }
    }
}
```

## Advanced Event Sourcing Features

### 1. Event Projections

```php
// Projection Configuration
projection_config = {
    user_projection = {
        table = "users"
        events = ["UserCreated", "UserUpdated", "UserDeleted"]
        mapping = {
            UserCreated = {
                action = "insert"
                fields = {
                    id = "aggregate_id"
                    email = "data.email"
                    username = "data.username"
                    status = "'active'"
                    created_at = "timestamp"
                }
            }
            
            UserUpdated = {
                action = "update"
                where = "id = aggregate_id"
                fields = {
                    email = "COALESCE(data.email, email)"
                    username = "COALESCE(data.username, username)"
                    updated_at = "timestamp"
                }
            }
            
            UserDeleted = {
                action = "update"
                where = "id = aggregate_id"
                fields = {
                    status = "'deleted'"
                    deleted_at = "timestamp"
                }
            }
        }
    }
    
    order_projection = {
        table = "orders"
        events = ["OrderCreated", "OrderShipped", "OrderCancelled"]
        joins = {
            order_items = {
                table = "order_items"
                foreign_key = "order_id"
                events = ["OrderItemAdded"]
            }
        }
    }
}

// Projection Engine
class ProjectionEngine {
    private $config;
    private $connection;
    
    public function __construct($configPath) {
        $this->config = $this->loadConfig($configPath);
        $this->connection = new DatabaseConnection();
    }
    
    public function projectEvent($event) {
        foreach ($this->config->projection_config as $projectionName => $config) {
            if (in_array($event->getType(), $config->events)) {
                $this->applyProjection($projectionName, $event, $config);
            }
        }
    }
    
    private function applyProjection($projectionName, $event, $config) {
        $mapping = $config->mapping->{$event->getType()};
        
        switch ($mapping->action) {
            case 'insert':
                $this->insertRecord($config->table, $this->mapFields($mapping->fields, $event));
                break;
                
            case 'update':
                $this->updateRecord($config->table, $this->mapFields($mapping->fields, $event), $mapping->where);
                break;
                
            case 'delete':
                $this->deleteRecord($config->table, $mapping->where);
                break;
        }
    }
    
    private function mapFields($fieldMapping, $event) {
        $mapped = [];
        
        foreach ($fieldMapping as $dbField => $eventPath) {
            if (strpos($eventPath, 'data.') === 0) {
                $dataField = substr($eventPath, 5);
                $mapped[$dbField] = $event->getData()[$dataField] ?? null;
            } elseif ($eventPath === 'aggregate_id') {
                $mapped[$dbField] = $event->getAggregateId();
            } elseif ($eventPath === 'timestamp') {
                $mapped[$dbField] = $event->getTimestamp();
            } else {
                $mapped[$dbField] = $eventPath;
            }
        }
        
        return $mapped;
    }
}
```

### 2. Event Replay and Snapshots

```php
// Snapshot Configuration
snapshot_config = {
    user_snapshots = {
        aggregate_type = "user"
        frequency = 100
        storage = {
            type = "database"
            table = "user_snapshots"
        }
        
        serialization = {
            format = "json"
            compression = "gzip"
        }
    }
    
    replay_config = {
        batch_size = 1000
        parallel_processing = true
        progress_tracking = true
        error_handling = "continue"
    }
}

// Snapshot Manager
class SnapshotManager {
    private $config;
    private $eventStore;
    
    public function __construct($configPath) {
        $this->config = $this->loadConfig($configPath);
        $this->eventStore = new EventStore($configPath);
    }
    
    public function createSnapshot($aggregateId, $aggregate) {
        $snapshotConfig = $this->config->snapshot_config->user_snapshots;
        
        $snapshot = [
            'aggregate_id' => $aggregateId,
            'version' => $aggregate->getVersion(),
            'state' => $this->serializeState($aggregate),
            'created_at' => new DateTime()
        ];
        
        $this->storeSnapshot($snapshot, $snapshotConfig);
    }
    
    public function loadAggregate($aggregateId, $aggregateClass) {
        // Try to load from snapshot first
        $snapshot = $this->loadLatestSnapshot($aggregateId);
        
        if ($snapshot) {
            $aggregate = $this->deserializeState($snapshot['state'], $aggregateClass);
            $aggregate->setVersion($snapshot['version']);
            
            // Load remaining events
            $remainingEvents = $this->eventStore->getEvents($aggregateId, $snapshot['version'] + 1);
            foreach ($remainingEvents as $event) {
                $aggregate->apply($event);
            }
        } else {
            // Load all events
            $events = $this->eventStore->getEvents($aggregateId);
            $aggregate = new $aggregateClass($aggregateId);
            foreach ($events as $event) {
                $aggregate->apply($event);
            }
        }
        
        return $aggregate;
    }
    
    public function replayEvents($fromDate = null, $toDate = null) {
        $replayConfig = $this->config->snapshot_config->replay_config;
        
        // Get all events in date range
        $events = $this->eventStore->getEventsInRange($fromDate, $toDate);
        
        // Process in batches
        $batches = array_chunk($events, $replayConfig->batch_size);
        
        foreach ($batches as $batch) {
            try {
                $this->processEventBatch($batch);
            } catch (Exception $e) {
                if ($replayConfig->error_handling === 'continue') {
                    error_log("Error processing batch: " . $e->getMessage());
                    continue;
                } else {
                    throw $e;
                }
            }
        }
    }
}
```

### 3. Event Versioning and Migration

```php
// Event Versioning Configuration
event_versioning = {
    version_strategy = "semantic"
    migration_enabled = true
    
    migrations = {
        UserCreated_v1_to_v2 = {
            from_version = 1
            to_version = 2
            changes = {
                add_fields = ["profile"]
                remove_fields = ["age"]
                transform_fields = {
                    "name" = "split_name_to_first_last"
                }
            }
        }
        
        OrderCreated_v2_to_v3 = {
            from_version = 2
            to_version = 3
            changes = {
                add_fields = ["shipping_address"]
                transform_fields = {
                    "items" = "normalize_order_items"
                }
            }
        }
    }
    
    backward_compatibility = {
        enabled = true
        max_versions_back = 3
    }
}

// Event Migration Engine
class EventMigrationEngine {
    private $config;
    
    public function __construct($configPath) {
        $this->config = $this->loadConfig($configPath);
    }
    
    public function migrateEvent($event) {
        $currentVersion = $event->getVersion();
        $targetVersion = $this->getLatestVersion($event->getType());
        
        if ($currentVersion === $targetVersion) {
            return $event;
        }
        
        $migratedEvent = $event;
        
        for ($version = $currentVersion; $version < $targetVersion; $version++) {
            $migrationKey = "{$event->getType()}_v{$version}_to_v" . ($version + 1);
            
            if (isset($this->config->event_versioning->migrations->$migrationKey)) {
                $migratedEvent = $this->applyMigration($migratedEvent, $migrationKey);
            }
        }
        
        return $migratedEvent;
    }
    
    private function applyMigration($event, $migrationKey) {
        $migration = $this->config->event_versioning->migrations->$migrationKey;
        $data = $event->getData();
        
        // Add new fields
        if (isset($migration->changes->add_fields)) {
            foreach ($migration->changes->add_fields as $field) {
                $data[$field] = null;
            }
        }
        
        // Remove fields
        if (isset($migration->changes->remove_fields)) {
            foreach ($migration->changes->remove_fields as $field) {
                unset($data[$field]);
            }
        }
        
        // Transform fields
        if (isset($migration->changes->transform_fields)) {
            foreach ($migration->changes->transform_fields as $field => $transformation) {
                if (isset($data[$field])) {
                    $data[$field] = $this->applyTransformation($data[$field], $transformation);
                }
            }
        }
        
        return new Event(
            $event->getAggregateId(),
            $event->getType(),
            $data,
            $event->getMetadata(),
            $event->getVersion() + 1,
            $event->getTimestamp()
        );
    }
}
```

## Integration Patterns

### 1. Database-Driven Event Sourcing

```php
// Live Database Queries in Event Sourcing Config
event_sourcing_data = {
    event_streams = @query("
        SELECT 
            aggregate_id,
            event_type,
            event_data,
            metadata,
            version,
            timestamp
        FROM events 
        WHERE timestamp >= NOW() - INTERVAL 24 HOUR
        ORDER BY aggregate_id, version
    ")
    
    aggregate_snapshots = @query("
        SELECT 
            aggregate_id,
            aggregate_type,
            state,
            version,
            created_at
        FROM snapshots 
        WHERE created_at >= NOW() - INTERVAL 7 DAY
        ORDER BY aggregate_id, version DESC
    ")
    
    projection_tables = @query("
        SELECT 
            table_name,
            event_types,
            last_updated
        FROM projection_metadata 
        WHERE is_active = true
    ")
    
    event_statistics = @query("
        SELECT 
            event_type,
            COUNT(*) as event_count,
            COUNT(DISTINCT aggregate_id) as aggregate_count,
            MIN(timestamp) as first_occurrence,
            MAX(timestamp) as last_occurrence
        FROM events 
        WHERE timestamp >= NOW() - INTERVAL 30 DAY
        GROUP BY event_type
        ORDER BY event_count DESC
    ")
}
```

### 2. Event-Driven Integration

```php
// Event-Driven Integration Configuration
event_driven_integration = {
    event_publishers = {
        kafka = {
            brokers = ["localhost:9092"]
            topic_prefix = "events."
            partitioning = "hash"
        }
        
        rabbitmq = {
            host = "localhost"
            exchange = "events"
            routing_key = "event.type"
        }
        
        webhooks = {
            endpoints = ["https://api.external.com/events"]
            retry_policy = {
                max_retries = 3
                backoff_multiplier = 2
            }
        }
    }
    
    event_subscribers = {
        notification_service = {
            events = ["UserCreated", "OrderShipped"]
            handler = "NotificationEventHandler"
        }
        
        analytics_service = {
            events = ["*"]
            handler = "AnalyticsEventHandler"
        }
        
        audit_service = {
            events = ["*"]
            handler = "AuditEventHandler"
        }
    }
}

// Event Publisher
class EventPublisher {
    private $config;
    private $publishers = [];
    
    public function __construct($configPath) {
        $this->config = $this->loadConfig($configPath);
        $this->initializePublishers();
    }
    
    public function publish($event) {
        foreach ($this->publishers as $publisher) {
            try {
                $publisher->publish($event);
            } catch (Exception $e) {
                error_log("Failed to publish event: " . $e->getMessage());
            }
        }
    }
    
    private function initializePublishers() {
        foreach ($this->config->event_driven_integration->event_publishers as $type => $config) {
            switch ($type) {
                case 'kafka':
                    $this->publishers[] = new KafkaPublisher($config);
                    break;
                case 'rabbitmq':
                    $this->publishers[] = new RabbitMQPublisher($config);
                    break;
                case 'webhooks':
                    $this->publishers[] = new WebhookPublisher($config);
                    break;
            }
        }
    }
}
```

### 3. Saga Pattern Implementation

```php
// Saga Configuration
saga_config = {
    order_processing_saga = {
        steps = [
            {
                name = "validate_order"
                command = "ValidateOrderCommand"
                compensation = "CancelOrderCommand"
            },
            {
                name = "reserve_inventory"
                command = "ReserveInventoryCommand"
                compensation = "ReleaseInventoryCommand"
            },
            {
                name = "process_payment"
                command = "ProcessPaymentCommand"
                compensation = "RefundPaymentCommand"
            },
            {
                name = "ship_order"
                command = "ShipOrderCommand"
                compensation = "CancelShipmentCommand"
            }
        ]
        
        timeout = "30 minutes"
        retry_policy = {
            max_retries = 3
            backoff_multiplier = 2
        }
    }
}

// Saga Orchestrator
class SagaOrchestrator {
    private $config;
    private $commandBus;
    private $eventStore;
    
    public function __construct($configPath) {
        $this->config = $this->loadConfig($configPath);
        $this->commandBus = new CommandBus($configPath);
        $this->eventStore = new EventStore($configPath);
    }
    
    public function executeSaga($sagaName, $data) {
        $sagaConfig = $this->config->saga_config->$sagaName;
        $sagaId = uniqid("saga_");
        
        $saga = new Saga($sagaId, $sagaName, $data);
        
        try {
            foreach ($sagaConfig->steps as $step) {
                $command = $this->createCommand($step->command, $data);
                $result = $this->commandBus->dispatch($command);
                
                $saga->addCompletedStep($step->name, $result);
            }
            
            $saga->markAsCompleted();
            
        } catch (Exception $e) {
            $saga->markAsFailed($e->getMessage());
            $this->compensateSaga($saga, $sagaConfig);
        }
        
        $this->eventStore->appendEvents($sagaId, $saga->getUncommittedEvents());
        
        return $saga;
    }
    
    private function compensateSaga($saga, $sagaConfig) {
        $completedSteps = array_reverse($saga->getCompletedSteps());
        
        foreach ($completedSteps as $step) {
            $stepConfig = $this->findStepConfig($sagaConfig->steps, $step->name);
            
            if (isset($stepConfig->compensation)) {
                $compensationCommand = $this->createCommand($stepConfig->compensation, $step->data);
                $this->commandBus->dispatch($compensationCommand);
            }
        }
    }
}
```

## Best Practices

### 1. Event Design

```php
// Event Design Guidelines
event_design = {
    naming_conventions = {
        event_types = "PascalCase"
        aggregate_events = "AggregateName + Action"
        domain_events = "Domain + Event"
    }
    
    event_structure = {
        required_fields = ["aggregate_id", "event_type", "timestamp", "version"]
        optional_fields = ["metadata", "correlation_id", "causation_id"]
        data_serialization = "json"
    }
    
    event_sizing = {
        max_event_size = "1MB"
        compression_threshold = "10KB"
        chunking_enabled = true
    }
    
    event_ordering = {
        strict_ordering = true
        causal_ordering = true
        global_ordering = false
    }
}
```

### 2. Performance Optimization

```php
// Performance Configuration
performance_config = {
    event_store = {
        indexing = {
            composite_indexes = true
            covering_indexes = true
            partition_pruning = true
        }
        
        caching = {
            event_cache = true
            snapshot_cache = true
            projection_cache = true
        }
        
        batching = {
            write_batching = true
            batch_size = 1000
            flush_interval = "1 second"
        }
    }
    
    projections = {
        parallel_processing = true
        incremental_updates = true
        materialized_views = true
    }
    
    queries = {
        read_models = true
        query_optimization = true
        connection_pooling = true
    }
}
```

### 3. Monitoring and Observability

```php
// Monitoring Configuration
monitoring_config = {
    metrics = {
        event_count = true
        event_size = true
        processing_latency = true
        error_rate = true
    }
    
    tracing = {
        distributed_tracing = true
        event_correlation = true
        performance_tracking = true
    }
    
    alerting = {
        event_processing_delays = true
        error_thresholds = true
        performance_degradation = true
    }
    
    logging = {
        event_logging = true
        audit_logging = true
        debug_logging = false
    }
}
```

This comprehensive event sourcing documentation demonstrates how TuskLang revolutionizes event-driven architecture by providing configuration-driven event stores, CQRS patterns, and powerful event processing capabilities while maintaining the rebellious spirit and technical excellence that defines the TuskLang ecosystem. 