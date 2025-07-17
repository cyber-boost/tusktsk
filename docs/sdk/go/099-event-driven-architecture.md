# Event-Driven Architecture

TuskLang provides powerful event-driven architecture capabilities that enable scalable, decoupled, and responsive applications. This guide covers comprehensive event-driven patterns for Go applications.

## Event-Driven Philosophy

### Event-First Design
```go
// Event-first design with TuskLang
type EventManager struct {
    config *tusk.Config
    bus    *EventBus
    store  *EventStore
}

func NewEventManager(config *tusk.Config) *EventManager {
    return &EventManager{
        config: config,
        bus:    NewEventBus(config),
        store:  NewEventStore(config),
    }
}

// PublishEvent publishes an event to the event bus
func (em *EventManager) PublishEvent(event *Event) error {
    // Validate event
    if err := em.validateEvent(event); err != nil {
        return fmt.Errorf("event validation failed: %w", err)
    }
    
    // Store event
    if err := em.store.StoreEvent(event); err != nil {
        return fmt.Errorf("failed to store event: %w", err)
    }
    
    // Publish to event bus
    if err := em.bus.Publish(event); err != nil {
        return fmt.Errorf("failed to publish event: %w", err)
    }
    
    return nil
}

type Event struct {
    ID          string
    Type        string
    Source      string
    Data        map[string]interface{}
    Timestamp   time.Time
    Version     int
    CorrelationID string
}
```

### Event Sourcing
```go
// Event sourcing implementation
type EventSourcing struct {
    config *tusk.Config
    store  *EventStore
}

func (es *EventSourcing) ApplyEvent(aggregateID string, event *Event) error {
    // Store event
    if err := es.store.StoreEvent(event); err != nil {
        return fmt.Errorf("failed to store event: %w", err)
    }
    
    // Update aggregate state
    if err := es.updateAggregateState(aggregateID, event); err != nil {
        return fmt.Errorf("failed to update aggregate state: %w", err)
    }
    
    return nil
}

func (es *EventSourcing) RebuildAggregate(aggregateID string) (*Aggregate, error) {
    // Get all events for aggregate
    events, err := es.store.GetEvents(aggregateID)
    if err != nil {
        return nil, fmt.Errorf("failed to get events: %w", err)
    }
    
    // Rebuild aggregate from events
    aggregate := &Aggregate{ID: aggregateID}
    for _, event := range events {
        if err := es.applyEventToAggregate(aggregate, event); err != nil {
            return nil, fmt.Errorf("failed to apply event: %w", err)
        }
    }
    
    return aggregate, nil
}

type Aggregate struct {
    ID      string
    Version int
    State   map[string]interface{}
}
```

## TuskLang Event Configuration

### Event Bus Configuration
```tsk
# Event-driven architecture configuration
event_driven_architecture {
    # Event bus configuration
    event_bus {
        type = "kafka"
        brokers = ["localhost:9092"]
        topic_prefix = "events"
        partitions = 3
        replication_factor = 1
        retention_policy = "7d"
    }
    
    # Event store configuration
    event_store {
        type = "postgresql"
        connection_string = "postgresql://user:pass@localhost:5432/events"
        table_name = "events"
        batch_size = 100
        retention_policy = "1y"
    }
    
    # Event processing
    event_processing {
        parallel_processing = true
        max_workers = 10
        batch_size = 50
        retry_policy {
            max_retries = 3
            backoff_delay = "1s"
            max_delay = "30s"
        }
    }
    
    # Event patterns
    event_patterns {
        # Saga pattern
        saga {
            enabled = true
            compensation_enabled = true
            timeout = "5m"
        }
        
        # CQRS pattern
        cqrs {
            enabled = true
            read_model_sync = true
            event_handlers = true
        }
        
        # Event sourcing
        event_sourcing {
            enabled = true
            snapshot_frequency = 100
            projection_rebuild = true
        }
    }
}
```

### Event Definitions
```tsk
# Event definitions
events {
    # User events
    user_created {
        type = "user.created"
        schema = {
            "user_id" = "string"
            "email" = "string"
            "name" = "string"
            "created_at" = "timestamp"
        }
        handlers = ["user_service", "notification_service", "audit_service"]
    }
    
    user_updated {
        type = "user.updated"
        schema = {
            "user_id" = "string"
            "email" = "string"
            "name" = "string"
            "updated_at" = "timestamp"
        }
        handlers = ["user_service", "notification_service"]
    }
    
    # Order events
    order_created {
        type = "order.created"
        schema = {
            "order_id" = "string"
            "user_id" = "string"
            "items" = "array"
            "total" = "number"
            "created_at" = "timestamp"
        }
        handlers = ["order_service", "inventory_service", "payment_service"]
    }
    
    order_processed {
        type = "order.processed"
        schema = {
            "order_id" = "string"
            "status" = "string"
            "processed_at" = "timestamp"
        }
        handlers = ["order_service", "notification_service"]
    }
}
```

## Go Event-Driven Implementation

### Event Bus Implementation
```go
// Event bus implementation with Kafka
type EventBus struct {
    config *tusk.Config
    producer *kafka.Producer
    consumer *kafka.Consumer
}

func NewEventBus(config *tusk.Config) *EventBus {
    // Initialize Kafka producer
    producer, err := kafka.NewProducer(&kafka.ConfigMap{
        "bootstrap.servers": config.GetString("event_driven_architecture.event_bus.brokers.0"),
    })
    if err != nil {
        log.Fatalf("Failed to create producer: %v", err)
    }
    
    // Initialize Kafka consumer
    consumer, err := kafka.NewConsumer(&kafka.ConfigMap{
        "bootstrap.servers": config.GetString("event_driven_architecture.event_bus.brokers.0"),
        "group.id":          "event-handlers",
        "auto.offset.reset": "earliest",
    })
    if err != nil {
        log.Fatalf("Failed to create consumer: %v", err)
    }
    
    return &EventBus{
        config:   config,
        producer: producer,
        consumer: consumer,
    }
}

func (eb *EventBus) Publish(event *Event) error {
    // Serialize event
    data, err := json.Marshal(event)
    if err != nil {
        return fmt.Errorf("failed to serialize event: %w", err)
    }
    
    // Determine topic
    topic := eb.getTopicForEvent(event.Type)
    
    // Publish to Kafka
    err = eb.producer.Produce(&kafka.Message{
        TopicPartition: kafka.TopicPartition{Topic: &topic, Partition: kafka.PartitionAny},
        Value:          data,
        Key:            []byte(event.ID),
    }, nil)
    
    if err != nil {
        return fmt.Errorf("failed to publish event: %w", err)
    }
    
    return nil
}

func (eb *EventBus) Subscribe(eventType string, handler EventHandler) error {
    // Subscribe to topic
    topic := eb.getTopicForEvent(eventType)
    err := eb.consumer.Subscribe(topic, nil)
    if err != nil {
        return fmt.Errorf("failed to subscribe to topic: %w", err)
    }
    
    // Start consuming messages
    go eb.consumeMessages(handler)
    
    return nil
}

func (eb *EventBus) consumeMessages(handler EventHandler) {
    for {
        msg, err := eb.consumer.ReadMessage(-1)
        if err != nil {
            log.Printf("Error reading message: %v", err)
            continue
        }
        
        // Deserialize event
        var event Event
        if err := json.Unmarshal(msg.Value, &event); err != nil {
            log.Printf("Error deserializing event: %v", err)
            continue
        }
        
        // Handle event
        if err := handler.HandleEvent(&event); err != nil {
            log.Printf("Error handling event: %v", err)
        }
        
        // Commit offset
        eb.consumer.CommitMessage(msg)
    }
}

type EventHandler interface {
    HandleEvent(event *Event) error
}
```

### Event Store Implementation
```go
// Event store implementation with PostgreSQL
type EventStore struct {
    config *tusk.Config
    db     *sql.DB
}

func NewEventStore(config *tusk.Config) *EventStore {
    connStr := config.GetString("event_driven_architecture.event_store.connection_string")
    db, err := sql.Open("postgres", connStr)
    if err != nil {
        log.Fatalf("Failed to connect to database: %v", err)
    }
    
    return &EventStore{
        config: config,
        db:     db,
    }
}

func (es *EventStore) StoreEvent(event *Event) error {
    query := `
        INSERT INTO events (id, type, source, data, timestamp, version, correlation_id, aggregate_id)
        VALUES ($1, $2, $3, $4, $5, $6, $7, $8)
    `
    
    data, err := json.Marshal(event.Data)
    if err != nil {
        return fmt.Errorf("failed to serialize event data: %w", err)
    }
    
    _, err = es.db.Exec(query,
        event.ID,
        event.Type,
        event.Source,
        data,
        event.Timestamp,
        event.Version,
        event.CorrelationID,
        event.AggregateID,
    )
    
    if err != nil {
        return fmt.Errorf("failed to store event: %w", err)
    }
    
    return nil
}

func (es *EventStore) GetEvents(aggregateID string) ([]*Event, error) {
    query := `
        SELECT id, type, source, data, timestamp, version, correlation_id
        FROM events
        WHERE aggregate_id = $1
        ORDER BY version ASC
    `
    
    rows, err := es.db.Query(query, aggregateID)
    if err != nil {
        return nil, fmt.Errorf("failed to query events: %w", err)
    }
    defer rows.Close()
    
    var events []*Event
    for rows.Next() {
        var event Event
        var data []byte
        
        err := rows.Scan(
            &event.ID,
            &event.Type,
            &event.Source,
            &data,
            &event.Timestamp,
            &event.Version,
            &event.CorrelationID,
        )
        if err != nil {
            return nil, fmt.Errorf("failed to scan event: %w", err)
        }
        
        if err := json.Unmarshal(data, &event.Data); err != nil {
            return nil, fmt.Errorf("failed to deserialize event data: %w", err)
        }
        
        events = append(events, &event)
    }
    
    return events, nil
}
```

### Event Handlers
```go
// Event handler implementation
type UserEventHandler struct {
    config *tusk.Config
    db     *sql.DB
}

func NewUserEventHandler(config *tusk.Config) *UserEventHandler {
    return &UserEventHandler{
        config: config,
    }
}

func (ueh *UserEventHandler) HandleEvent(event *Event) error {
    switch event.Type {
    case "user.created":
        return ueh.handleUserCreated(event)
    case "user.updated":
        return ueh.handleUserUpdated(event)
    default:
        return fmt.Errorf("unknown event type: %s", event.Type)
    }
}

func (ueh *UserEventHandler) handleUserCreated(event *Event) error {
    userID := event.Data["user_id"].(string)
    email := event.Data["email"].(string)
    name := event.Data["name"].(string)
    
    // Update read model
    query := `
        INSERT INTO users (id, email, name, created_at)
        VALUES ($1, $2, $3, $4)
    `
    
    _, err := ueh.db.Exec(query, userID, email, name, event.Timestamp)
    if err != nil {
        return fmt.Errorf("failed to update read model: %w", err)
    }
    
    return nil
}

func (ueh *UserEventHandler) handleUserUpdated(event *Event) error {
    userID := event.Data["user_id"].(string)
    email := event.Data["email"].(string)
    name := event.Data["name"].(string)
    
    // Update read model
    query := `
        UPDATE users
        SET email = $2, name = $3, updated_at = $4
        WHERE id = $1
    `
    
    _, err := ueh.db.Exec(query, userID, email, name, event.Timestamp)
    if err != nil {
        return fmt.Errorf("failed to update read model: %w", err)
    }
    
    return nil
}
```

## Advanced Event-Driven Features

### Saga Pattern Implementation
```go
// Saga pattern for distributed transactions
type SagaManager struct {
    config *tusk.Config
    bus    *EventBus
    store  *EventStore
}

type Saga struct {
    ID          string
    Steps       []SagaStep
    CurrentStep int
    Status      string
    Compensation []SagaStep
}

type SagaStep struct {
    ID       string
    Action   func() error
    Compensate func() error
    Event    *Event
}

func (sm *SagaManager) ExecuteSaga(saga *Saga) error {
    saga.Status = "running"
    
    for i, step := range saga.Steps {
        saga.CurrentStep = i
        
        // Execute step
        if err := step.Action(); err != nil {
            // Compensate previous steps
            if err := sm.compensate(saga, i-1); err != nil {
                return fmt.Errorf("compensation failed: %w", err)
            }
            
            saga.Status = "failed"
            return fmt.Errorf("saga step failed: %w", err)
        }
        
        // Publish event
        if step.Event != nil {
            if err := sm.bus.Publish(step.Event); err != nil {
                return fmt.Errorf("failed to publish event: %w", err)
            }
        }
    }
    
    saga.Status = "completed"
    return nil
}

func (sm *SagaManager) compensate(saga *Saga, stepIndex int) error {
    for i := stepIndex; i >= 0; i-- {
        step := saga.Steps[i]
        if step.Compensate != nil {
            if err := step.Compensate(); err != nil {
                return fmt.Errorf("compensation step failed: %w", err)
            }
        }
    }
    
    return nil
}
```

### CQRS Implementation
```go
// CQRS (Command Query Responsibility Segregation) implementation
type CQRSManager struct {
    config *tusk.Config
    commandBus *CommandBus
    queryBus   *QueryBus
    eventBus   *EventBus
}

type CommandBus struct {
    handlers map[string]CommandHandler
}

type QueryBus struct {
    handlers map[string]QueryHandler
}

type CommandHandler interface {
    Handle(command interface{}) error
}

type QueryHandler interface {
    Handle(query interface{}) (interface{}, error)
}

func (cqrs *CQRSManager) SendCommand(command interface{}) error {
    commandType := reflect.TypeOf(command).String()
    
    handler, exists := cqrs.commandBus.handlers[commandType]
    if !exists {
        return fmt.Errorf("no handler found for command: %s", commandType)
    }
    
    return handler.Handle(command)
}

func (cqrs *CQRSManager) SendQuery(query interface{}) (interface{}, error) {
    queryType := reflect.TypeOf(query).String()
    
    handler, exists := cqrs.queryBus.handlers[queryType]
    if !exists {
        return nil, fmt.Errorf("no handler found for query: %s", queryType)
    }
    
    return handler.Handle(query)
}

// Command example
type CreateUserCommand struct {
    Email string
    Name  string
}

type CreateUserCommandHandler struct {
    eventBus *EventBus
}

func (h *CreateUserCommandHandler) Handle(command interface{}) error {
    cmd := command.(*CreateUserCommand)
    
    // Create user
    userID := generateUserID()
    
    // Publish event
    event := &Event{
        ID:        generateEventID(),
        Type:      "user.created",
        Source:    "user-service",
        Data:      map[string]interface{}{
            "user_id": userID,
            "email":   cmd.Email,
            "name":    cmd.Name,
        },
        Timestamp: time.Now(),
        Version:   1,
    }
    
    return h.eventBus.Publish(event)
}

// Query example
type GetUserQuery struct {
    UserID string
}

type GetUserQueryHandler struct {
    db *sql.DB
}

func (h *GetUserQueryHandler) Handle(query interface{}) (interface{}, error) {
    q := query.(*GetUserQuery)
    
    var user User
    query := `SELECT id, email, name, created_at FROM users WHERE id = $1`
    
    err := h.db.QueryRow(query, q.UserID).Scan(
        &user.ID,
        &user.Email,
        &user.Name,
        &user.CreatedAt,
    )
    
    if err != nil {
        return nil, fmt.Errorf("failed to get user: %w", err)
    }
    
    return &user, nil
}
```

### Event Projections
```go
// Event projections for read models
type ProjectionManager struct {
    config *tusk.Config
    store  *EventStore
    db     *sql.DB
}

func (pm *ProjectionManager) RebuildProjection(projectionName string) error {
    // Get all events
    events, err := pm.store.GetAllEvents()
    if err != nil {
        return fmt.Errorf("failed to get events: %w", err)
    }
    
    // Clear projection
    if err := pm.clearProjection(projectionName); err != nil {
        return fmt.Errorf("failed to clear projection: %w", err)
    }
    
    // Rebuild projection
    for _, event := range events {
        if err := pm.applyEventToProjection(projectionName, event); err != nil {
            return fmt.Errorf("failed to apply event to projection: %w", err)
        }
    }
    
    return nil
}

func (pm *ProjectionManager) applyEventToProjection(projectionName string, event *Event) error {
    switch projectionName {
    case "user_summary":
        return pm.applyToUserSummary(event)
    case "order_summary":
        return pm.applyToOrderSummary(event)
    default:
        return fmt.Errorf("unknown projection: %s", projectionName)
    }
}

func (pm *ProjectionManager) applyToUserSummary(event *Event) error {
    switch event.Type {
    case "user.created":
        return pm.handleUserCreatedSummary(event)
    case "user.updated":
        return pm.handleUserUpdatedSummary(event)
    }
    
    return nil
}

func (pm *ProjectionManager) handleUserCreatedSummary(event *Event) error {
    userID := event.Data["user_id"].(string)
    email := event.Data["email"].(string)
    name := event.Data["name"].(string)
    
    query := `
        INSERT INTO user_summary (user_id, email, name, created_at, total_orders, total_spent)
        VALUES ($1, $2, $3, $4, 0, 0)
    `
    
    _, err := pm.db.Exec(query, userID, email, name, event.Timestamp)
    return err
}
```

## Event-Driven Tools and Utilities

### Event Monitoring
```go
// Event monitoring and metrics
type EventMonitor struct {
    config *tusk.Config
    metrics map[string]*EventMetrics
}

type EventMetrics struct {
    EventType     string
    Published     int64
    Processed     int64
    Failed        int64
    AverageLatency time.Duration
    LastProcessed time.Time
}

func (em *EventMonitor) RecordEventPublished(eventType string) {
    metrics := em.getOrCreateMetrics(eventType)
    atomic.AddInt64(&metrics.Published, 1)
}

func (em *EventMonitor) RecordEventProcessed(eventType string, latency time.Duration) {
    metrics := em.getOrCreateMetrics(eventType)
    atomic.AddInt64(&metrics.Processed, 1)
    metrics.AverageLatency = latency
    metrics.LastProcessed = time.Now()
}

func (em *EventMonitor) RecordEventFailed(eventType string) {
    metrics := em.getOrCreateMetrics(eventType)
    atomic.AddInt64(&metrics.Failed, 1)
}

func (em *EventMonitor) getOrCreateMetrics(eventType string) *EventMetrics {
    if metrics, exists := em.metrics[eventType]; exists {
        return metrics
    }
    
    metrics := &EventMetrics{EventType: eventType}
    em.metrics[eventType] = metrics
    return metrics
}
```

### Event Replay
```go
// Event replay for debugging and testing
type EventReplayer struct {
    config *tusk.Config
    store  *EventStore
    bus    *EventBus
}

func (er *EventReplayer) ReplayEvents(fromTime, toTime time.Time) error {
    // Get events in time range
    events, err := er.store.GetEventsInRange(fromTime, toTime)
    if err != nil {
        return fmt.Errorf("failed to get events: %w", err)
    }
    
    // Replay events
    for _, event := range events {
        if err := er.bus.Publish(event); err != nil {
            return fmt.Errorf("failed to replay event: %w", err)
        }
    }
    
    return nil
}

func (er *EventReplayer) ReplayEventsByType(eventType string, fromTime, toTime time.Time) error {
    // Get events by type in time range
    events, err := er.store.GetEventsByType(eventType, fromTime, toTime)
    if err != nil {
        return fmt.Errorf("failed to get events: %w", err)
    }
    
    // Replay events
    for _, event := range events {
        if err := er.bus.Publish(event); err != nil {
            return fmt.Errorf("failed to replay event: %w", err)
        }
    }
    
    return nil
}
```

## Validation and Error Handling

### Event Configuration Validation
```go
// Validate event configuration
func ValidateEventConfig(config *tusk.Config) error {
    if config == nil {
        return errors.New("event config cannot be nil")
    }
    
    // Validate event bus configuration
    if !config.Has("event_driven_architecture.event_bus") {
        return errors.New("missing event bus configuration")
    }
    
    // Validate event store configuration
    if !config.Has("event_driven_architecture.event_store") {
        return errors.New("missing event store configuration")
    }
    
    return nil
}
```

### Error Handling
```go
// Handle event errors gracefully
func handleEventError(err error, context string) {
    log.Printf("Event error in %s: %v", context, err)
    
    // Log additional context if available
    if eventErr, ok := err.(*EventError); ok {
        log.Printf("Event context: %s", eventErr.Context)
    }
}
```

## Performance Considerations

### Event Performance Optimization
```go
// Optimize event processing performance
type EventOptimizer struct {
    config *tusk.Config
}

func (eo *EventOptimizer) OptimizeEventProcessing() error {
    // Enable parallel processing
    if eo.config.GetBool("event_driven_architecture.event_processing.parallel_processing") {
        runtime.GOMAXPROCS(runtime.NumCPU())
    }
    
    // Setup batching
    if err := eo.setupBatching(); err != nil {
        return fmt.Errorf("failed to setup batching: %w", err)
    }
    
    // Optimize serialization
    if err := eo.optimizeSerialization(); err != nil {
        return fmt.Errorf("failed to optimize serialization: %w", err)
    }
    
    return nil
}

func (eo *EventOptimizer) setupBatching() error {
    // Setup event batching for better performance
    // This is a simplified implementation
    return nil
}
```

## Event-Driven Notes

- **Event Sourcing**: Use event sourcing for audit trails and state reconstruction
- **CQRS**: Implement CQRS for read/write separation
- **Saga Pattern**: Use saga pattern for distributed transactions
- **Event Projections**: Create projections for optimized read models
- **Event Monitoring**: Monitor event processing and performance
- **Event Replay**: Support event replay for debugging and testing
- **Event Validation**: Validate events before processing
- **Event Versioning**: Handle event schema evolution

## Best Practices

1. **Event Design**: Design events with clear, descriptive names
2. **Event Sourcing**: Use event sourcing for complex domains
3. **CQRS**: Implement CQRS for performance optimization
4. **Saga Pattern**: Use saga pattern for distributed transactions
5. **Event Monitoring**: Monitor event processing comprehensively
6. **Event Replay**: Support event replay for debugging
7. **Event Validation**: Validate all events before processing
8. **Event Versioning**: Handle event schema changes gracefully

## Integration with TuskLang

```go
// Load event configuration from TuskLang
func LoadEventConfig(configPath string) (*tusk.Config, error) {
    config, err := tusk.LoadConfig(configPath)
    if err != nil {
        return nil, fmt.Errorf("failed to load event config: %w", err)
    }
    
    // Validate event configuration
    if err := ValidateEventConfig(config); err != nil {
        return nil, fmt.Errorf("invalid event config: %w", err)
    }
    
    return config, nil
}
```

This event-driven architecture guide provides comprehensive event-driven capabilities for your Go applications using TuskLang. Remember, good event-driven architecture is essential for scalable, decoupled systems. 