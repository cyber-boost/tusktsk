# Message Queues

TuskLang provides powerful message queue capabilities that enable reliable, scalable, and asynchronous communication between services. This guide covers comprehensive message queue strategies for Go applications.

## Message Queue Philosophy

### Queue-First Communication
```go
// Queue-first communication with TuskLang
type QueueManager struct {
    config *tusk.Config
    producer *MessageProducer
    consumer *MessageConsumer
}

func NewQueueManager(config *tusk.Config) *QueueManager {
    return &QueueManager{
        config:   config,
        producer: NewMessageProducer(config),
        consumer: NewMessageConsumer(config),
    }
}

// SendMessage sends a message to a queue
func (qm *QueueManager) SendMessage(queueName string, message *Message) error {
    // Validate message
    if err := qm.validateMessage(message); err != nil {
        return fmt.Errorf("message validation failed: %w", err)
    }
    
    // Send to queue
    if err := qm.producer.Send(queueName, message); err != nil {
        return fmt.Errorf("failed to send message: %w", err)
    }
    
    return nil
}

type Message struct {
    ID          string
    Type        string
    Data        map[string]interface{}
    Headers     map[string]string
    Timestamp   time.Time
    Priority    int
    RetryCount  int
    CorrelationID string
}
```

### Reliable Message Processing
```go
// Reliable message processing with acknowledgment
type ReliableConsumer struct {
    config *tusk.Config
    consumer *MessageConsumer
    processor MessageProcessor
}

type MessageProcessor interface {
    Process(message *Message) error
}

func (rc *ReliableConsumer) StartConsuming(queueName string) error {
    // Subscribe to queue
    if err := rc.consumer.Subscribe(queueName); err != nil {
        return fmt.Errorf("failed to subscribe to queue: %w", err)
    }
    
    // Start processing messages
    go rc.processMessages()
    
    return nil
}

func (rc *ReliableConsumer) processMessages() {
    for {
        message, err := rc.consumer.Receive()
        if err != nil {
            log.Printf("Error receiving message: %v", err)
            continue
        }
        
        // Process message
        if err := rc.processMessage(message); err != nil {
            log.Printf("Error processing message: %v", err)
            
            // Handle retry logic
            if err := rc.handleRetry(message); err != nil {
                log.Printf("Error handling retry: %v", err)
            }
        } else {
            // Acknowledge message
            if err := rc.consumer.Acknowledge(message); err != nil {
                log.Printf("Error acknowledging message: %v", err)
            }
        }
    }
}

func (rc *ReliableConsumer) processMessage(message *Message) error {
    // Process message with timeout
    done := make(chan error, 1)
    go func() {
        done <- rc.processor.Process(message)
    }()
    
    timeout := rc.config.GetDuration("message_queues.processing.timeout", 30*time.Second)
    select {
    case err := <-done:
        return err
    case <-time.After(timeout):
        return errors.New("message processing timeout")
    }
}
```

## TuskLang Message Queue Configuration

### Queue Configuration
```tsk
# Message queue configuration
message_queues {
    # Queue broker configuration
    broker {
        type = "rabbitmq"
        url = "amqp://user:pass@localhost:5672"
        vhost = "/"
        connection_pool_size = 10
        heartbeat_interval = "30s"
    }
    
    # Queue definitions
    queues {
        # User events queue
        user_events {
            name = "user.events"
            durable = true
            auto_delete = false
            arguments = {
                "x-message-ttl" = "86400000"
                "x-max-length" = "10000"
            }
            bindings = [
                "user.created",
                "user.updated",
                "user.deleted"
            ]
        }
        
        # Order processing queue
        order_processing {
            name = "order.processing"
            durable = true
            auto_delete = false
            arguments = {
                "x-message-ttl" = "3600000"
                "x-max-length" = "5000"
            }
            bindings = [
                "order.created",
                "order.updated"
            ]
        }
        
        # Dead letter queue
        dead_letter {
            name = "dead.letter"
            durable = true
            auto_delete = false
            arguments = {}
        }
    }
    
    # Message processing
    processing {
        prefetch_count = 10
        timeout = "30s"
        retry_policy {
            max_retries = 3
            backoff_delay = "1s"
            max_delay = "30s"
        }
        dead_letter_exchange = "dead.letter"
        dead_letter_routing_key = "dead.letter"
    }
    
    # Message routing
    routing {
        exchange_type = "topic"
        routing_keys = {
            "user.created" = "user.events"
            "user.updated" = "user.events"
            "user.deleted" = "user.events"
            "order.created" = "order.processing"
            "order.updated" = "order.processing"
        }
    }
}
```

### Message Definitions
```tsk
# Message definitions
messages {
    # User messages
    user_created {
        type = "user.created"
        schema = {
            "user_id" = "string"
            "email" = "string"
            "name" = "string"
            "created_at" = "timestamp"
        }
        priority = 1
        ttl = "1h"
        retry_policy = {
            max_retries = 3
            backoff_delay = "5s"
        }
    }
    
    user_updated {
        type = "user.updated"
        schema = {
            "user_id" = "string"
            "email" = "string"
            "name" = "string"
            "updated_at" = "timestamp"
        }
        priority = 2
        ttl = "1h"
        retry_policy = {
            max_retries = 2
            backoff_delay = "10s"
        }
    }
    
    # Order messages
    order_created {
        type = "order.created"
        schema = {
            "order_id" = "string"
            "user_id" = "string"
            "items" = "array"
            "total" = "number"
            "created_at" = "timestamp"
        }
        priority = 5
        ttl = "30m"
        retry_policy = {
            max_retries = 5
            backoff_delay = "2s"
        }
    }
}
```

## Go Message Queue Implementation

### Message Producer Implementation
```go
// Message producer implementation with RabbitMQ
type MessageProducer struct {
    config *tusk.Config
    conn   *amqp.Connection
    channel *amqp.Channel
}

func NewMessageProducer(config *tusk.Config) *MessageProducer {
    // Connect to RabbitMQ
    url := config.GetString("message_queues.broker.url")
    conn, err := amqp.Dial(url)
    if err != nil {
        log.Fatalf("Failed to connect to RabbitMQ: %v", err)
    }
    
    // Create channel
    ch, err := conn.Channel()
    if err != nil {
        log.Fatalf("Failed to open channel: %v", err)
    }
    
    return &MessageProducer{
        config:  config,
        conn:    conn,
        channel: ch,
    }
}

func (mp *MessageProducer) Send(queueName string, message *Message) error {
    // Serialize message
    data, err := json.Marshal(message)
    if err != nil {
        return fmt.Errorf("failed to serialize message: %w", err)
    }
    
    // Get routing key
    routingKey := mp.getRoutingKey(message.Type)
    
    // Publish message
    err = mp.channel.Publish(
        "",        // exchange
        routingKey, // routing key
        false,     // mandatory
        false,     // immediate
        amqp.Publishing{
            ContentType:  "application/json",
            Body:         data,
            DeliveryMode: amqp.Persistent,
            Priority:     uint8(message.Priority),
            MessageId:    message.ID,
            Timestamp:    message.Timestamp,
            Headers:      amqp.Table(message.Headers),
        },
    )
    
    if err != nil {
        return fmt.Errorf("failed to publish message: %w", err)
    }
    
    return nil
}

func (mp *MessageProducer) getRoutingKey(messageType string) string {
    routingKeys := mp.config.GetMap("message_queues.routing.routing_keys")
    if routingKey, exists := routingKeys[messageType]; exists {
        return routingKey.(string)
    }
    return messageType
}
```

### Message Consumer Implementation
```go
// Message consumer implementation with RabbitMQ
type MessageConsumer struct {
    config *tusk.Config
    conn   *amqp.Connection
    channel *amqp.Channel
    queue  string
}

func NewMessageConsumer(config *tusk.Config) *MessageConsumer {
    // Connect to RabbitMQ
    url := config.GetString("message_queues.broker.url")
    conn, err := amqp.Dial(url)
    if err != nil {
        log.Fatalf("Failed to connect to RabbitMQ: %v", err)
    }
    
    // Create channel
    ch, err := conn.Channel()
    if err != nil {
        log.Fatalf("Failed to open channel: %v", err)
    }
    
    // Set QoS
    prefetchCount := config.GetInt("message_queues.processing.prefetch_count", 10)
    err = ch.Qos(prefetchCount, 0, false)
    if err != nil {
        log.Fatalf("Failed to set QoS: %v", err)
    }
    
    return &MessageConsumer{
        config:  config,
        conn:    conn,
        channel: ch,
    }
}

func (mc *MessageConsumer) Subscribe(queueName string) error {
    // Declare queue
    queue, err := mc.channel.QueueDeclare(
        queueName, // name
        true,      // durable
        false,     // delete when unused
        false,     // exclusive
        false,     // no-wait
        nil,       // arguments
    )
    if err != nil {
        return fmt.Errorf("failed to declare queue: %w", err)
    }
    
    mc.queue = queue.Name
    
    return nil
}

func (mc *MessageConsumer) Receive() (*Message, error) {
    // Consume message
    delivery, ok, err := mc.channel.Get(mc.queue, false)
    if err != nil {
        return nil, fmt.Errorf("failed to get message: %w", err)
    }
    
    if !ok {
        return nil, errors.New("no message available")
    }
    
    // Deserialize message
    var message Message
    if err := json.Unmarshal(delivery.Body, &message); err != nil {
        return nil, fmt.Errorf("failed to deserialize message: %w", err)
    }
    
    // Add delivery tag for acknowledgment
    message.DeliveryTag = delivery.DeliveryTag
    
    return &message, nil
}

func (mc *MessageConsumer) Acknowledge(message *Message) error {
    return mc.channel.Ack(message.DeliveryTag, false)
}

func (mc *MessageConsumer) Reject(message *Message, requeue bool) error {
    return mc.channel.Nack(message.DeliveryTag, false, requeue)
}
```

### Message Processor Implementation
```go
// Message processor implementation
type UserMessageProcessor struct {
    config *tusk.Config
    db     *sql.DB
}

func NewUserMessageProcessor(config *tusk.Config) *UserMessageProcessor {
    return &UserMessageProcessor{
        config: config,
    }
}

func (ump *UserMessageProcessor) Process(message *Message) error {
    switch message.Type {
    case "user.created":
        return ump.handleUserCreated(message)
    case "user.updated":
        return ump.handleUserUpdated(message)
    case "user.deleted":
        return ump.handleUserDeleted(message)
    default:
        return fmt.Errorf("unknown message type: %s", message.Type)
    }
}

func (ump *UserMessageProcessor) handleUserCreated(message *Message) error {
    userID := message.Data["user_id"].(string)
    email := message.Data["email"].(string)
    name := message.Data["name"].(string)
    
    // Create user in database
    query := `
        INSERT INTO users (id, email, name, created_at)
        VALUES ($1, $2, $3, $4)
    `
    
    _, err := ump.db.Exec(query, userID, email, name, message.Timestamp)
    if err != nil {
        return fmt.Errorf("failed to create user: %w", err)
    }
    
    return nil
}

func (ump *UserMessageProcessor) handleUserUpdated(message *Message) error {
    userID := message.Data["user_id"].(string)
    email := message.Data["email"].(string)
    name := message.Data["name"].(string)
    
    // Update user in database
    query := `
        UPDATE users
        SET email = $2, name = $3, updated_at = $4
        WHERE id = $1
    `
    
    _, err := ump.db.Exec(query, userID, email, name, message.Timestamp)
    if err != nil {
        return fmt.Errorf("failed to update user: %w", err)
    }
    
    return nil
}

func (ump *UserMessageProcessor) handleUserDeleted(message *Message) error {
    userID := message.Data["user_id"].(string)
    
    // Delete user from database
    query := `DELETE FROM users WHERE id = $1`
    
    _, err := ump.db.Exec(query, userID)
    if err != nil {
        return fmt.Errorf("failed to delete user: %w", err)
    }
    
    return nil
}
```

## Advanced Message Queue Features

### Dead Letter Queue
```go
// Dead letter queue implementation
type DeadLetterQueue struct {
    config *tusk.Config
    producer *MessageProducer
    consumer *MessageConsumer
}

func (dlq *DeadLetterQueue) SetupDeadLetterQueue() error {
    // Declare dead letter exchange
    exchangeName := dlq.config.GetString("message_queues.processing.dead_letter_exchange")
    err := dlq.producer.channel.ExchangeDeclare(
        exchangeName, // name
        "direct",     // type
        true,         // durable
        false,        // auto-deleted
        false,        // internal
        false,        // no-wait
        nil,          // arguments
    )
    if err != nil {
        return fmt.Errorf("failed to declare dead letter exchange: %w", err)
    }
    
    // Declare dead letter queue
    queueName := dlq.config.GetString("message_queues.queues.dead_letter.name")
    _, err = dlq.producer.channel.QueueDeclare(
        queueName, // name
        true,      // durable
        false,     // delete when unused
        false,     // exclusive
        false,     // no-wait
        nil,       // arguments
    )
    if err != nil {
        return fmt.Errorf("failed to declare dead letter queue: %w", err)
    }
    
    // Bind queue to exchange
    routingKey := dlq.config.GetString("message_queues.processing.dead_letter_routing_key")
    err = dlq.producer.channel.QueueBind(
        queueName,    // queue name
        routingKey,   // routing key
        exchangeName, // exchange
        false,        // no-wait
        nil,          // arguments
    )
    if err != nil {
        return fmt.Errorf("failed to bind dead letter queue: %w", err)
    }
    
    return nil
}

func (dlq *DeadLetterQueue) SendToDeadLetter(message *Message, reason string) error {
    // Add dead letter reason to headers
    if message.Headers == nil {
        message.Headers = make(map[string]string)
    }
    message.Headers["dead_letter_reason"] = reason
    message.Headers["original_queue"] = message.Queue
    
    // Send to dead letter queue
    queueName := dlq.config.GetString("message_queues.queues.dead_letter.name")
    return dlq.producer.Send(queueName, message)
}
```

### Message Retry Logic
```go
// Message retry logic implementation
type RetryManager struct {
    config *tusk.Config
    producer *MessageProducer
}

func (rm *RetryManager) HandleRetry(message *Message) error {
    // Get retry policy
    retryPolicy := rm.getRetryPolicy(message.Type)
    
    // Check if max retries exceeded
    if message.RetryCount >= retryPolicy.MaxRetries {
        // Send to dead letter queue
        return rm.sendToDeadLetter(message, "max retries exceeded")
    }
    
    // Calculate delay
    delay := rm.calculateDelay(retryPolicy, message.RetryCount)
    
    // Schedule retry
    go func() {
        time.Sleep(delay)
        message.RetryCount++
        rm.producer.Send(message.Queue, message)
    }()
    
    return nil
}

func (rm *RetryManager) getRetryPolicy(messageType string) *RetryPolicy {
    messages := rm.config.GetMap("messages")
    if messageConfig, exists := messages[messageType]; exists {
        if config, ok := messageConfig.(map[string]interface{}); ok {
            if retryConfig, exists := config["retry_policy"]; exists {
                if retry, ok := retryConfig.(map[string]interface{}); ok {
                    return &RetryPolicy{
                        MaxRetries:  int(retry["max_retries"].(float64)),
                        BackoffDelay: retry["backoff_delay"].(string),
                    }
                }
            }
        }
    }
    
    // Default retry policy
    return &RetryPolicy{
        MaxRetries:  3,
        BackoffDelay: "1s",
    }
}

type RetryPolicy struct {
    MaxRetries  int
    BackoffDelay string
}

func (rm *RetryManager) calculateDelay(policy *RetryPolicy, retryCount int) time.Duration {
    baseDelay, _ := time.ParseDuration(policy.BackoffDelay)
    return baseDelay * time.Duration(1<<retryCount)
}
```

### Message Priority
```go
// Message priority implementation
type PriorityQueue struct {
    config *tusk.Config
    producer *MessageProducer
    consumer *MessageConsumer
}

func (pq *PriorityQueue) SetupPriorityQueue(queueName string) error {
    // Declare priority queue
    _, err := pq.producer.channel.QueueDeclare(
        queueName, // name
        true,      // durable
        false,     // delete when unused
        false,     // exclusive
        false,     // no-wait
        amqp.Table{
            "x-max-priority": 10, // Max priority level
        },
    )
    if err != nil {
        return fmt.Errorf("failed to declare priority queue: %w", err)
    }
    
    return nil
}

func (pq *PriorityQueue) SendWithPriority(queueName string, message *Message, priority int) error {
    // Set message priority
    message.Priority = priority
    
    // Send message
    return pq.producer.Send(queueName, message)
}
```

## Message Queue Tools and Utilities

### Queue Monitoring
```go
// Queue monitoring and metrics
type QueueMonitor struct {
    config *tusk.Config
    metrics map[string]*QueueMetrics
}

type QueueMetrics struct {
    QueueName     string
    MessageCount  int
    ConsumerCount int
    Published     int64
    Consumed      int64
    Failed        int64
    LastUpdated   time.Time
}

func (qm *QueueMonitor) MonitorQueue(queueName string) error {
    // Get queue info from RabbitMQ
    queue, err := qm.getQueueInfo(queueName)
    if err != nil {
        return fmt.Errorf("failed to get queue info: %w", err)
    }
    
    // Update metrics
    metrics := qm.getOrCreateMetrics(queueName)
    metrics.MessageCount = queue.Messages
    metrics.ConsumerCount = queue.Consumers
    metrics.LastUpdated = time.Now()
    
    return nil
}

func (qm *QueueMonitor) getQueueInfo(queueName string) (*amqp.Queue, error) {
    // Get queue info from RabbitMQ management API
    // This is a simplified implementation
    return &amqp.Queue{
        Name:      queueName,
        Messages:  0,
        Consumers: 0,
    }, nil
}

func (qm *QueueMonitor) getOrCreateMetrics(queueName string) *QueueMetrics {
    if metrics, exists := qm.metrics[queueName]; exists {
        return metrics
    }
    
    metrics := &QueueMetrics{QueueName: queueName}
    qm.metrics[queueName] = metrics
    return metrics
}
```

### Message Validation
```go
// Message validation utilities
type MessageValidator struct {
    config *tusk.Config
}

func (mv *MessageValidator) ValidateMessage(message *Message) error {
    // Get message schema
    schema := mv.getMessageSchema(message.Type)
    if schema == nil {
        return fmt.Errorf("no schema found for message type: %s", message.Type)
    }
    
    // Validate message data against schema
    if err := mv.validateData(message.Data, schema); err != nil {
        return fmt.Errorf("message data validation failed: %w", err)
    }
    
    // Validate required fields
    if err := mv.validateRequiredFields(message); err != nil {
        return fmt.Errorf("required fields validation failed: %w", err)
    }
    
    return nil
}

func (mv *MessageValidator) getMessageSchema(messageType string) map[string]interface{} {
    messages := mv.config.GetMap("messages")
    if messageConfig, exists := messages[messageType]; exists {
        if config, ok := messageConfig.(map[string]interface{}); ok {
            if schema, exists := config["schema"]; exists {
                return schema.(map[string]interface{})
            }
        }
    }
    return nil
}

func (mv *MessageValidator) validateData(data map[string]interface{}, schema map[string]interface{}) error {
    // Validate data types against schema
    for field, expectedType := range schema {
        if value, exists := data[field]; exists {
            if err := mv.validateFieldType(value, expectedType.(string)); err != nil {
                return fmt.Errorf("field %s validation failed: %w", field, err)
            }
        }
    }
    
    return nil
}

func (mv *MessageValidator) validateFieldType(value interface{}, expectedType string) error {
    switch expectedType {
    case "string":
        if _, ok := value.(string); !ok {
            return fmt.Errorf("expected string, got %T", value)
        }
    case "number":
        if _, ok := value.(float64); !ok {
            return fmt.Errorf("expected number, got %T", value)
        }
    case "timestamp":
        if _, ok := value.(time.Time); !ok {
            return fmt.Errorf("expected timestamp, got %T", value)
        }
    }
    
    return nil
}
```

## Validation and Error Handling

### Message Queue Configuration Validation
```go
// Validate message queue configuration
func ValidateMessageQueueConfig(config *tusk.Config) error {
    if config == nil {
        return errors.New("message queue config cannot be nil")
    }
    
    // Validate broker configuration
    if !config.Has("message_queues.broker") {
        return errors.New("missing broker configuration")
    }
    
    // Validate queue definitions
    if !config.Has("message_queues.queues") {
        return errors.New("missing queue definitions")
    }
    
    return nil
}
```

### Error Handling
```go
// Handle message queue errors gracefully
func handleMessageQueueError(err error, context string) {
    log.Printf("Message queue error in %s: %v", context, err)
    
    // Log additional context if available
    if mqErr, ok := err.(*MessageQueueError); ok {
        log.Printf("Message queue context: %s", mqErr.Context)
    }
}
```

## Performance Considerations

### Message Queue Performance Optimization
```go
// Optimize message queue performance
type MessageQueueOptimizer struct {
    config *tusk.Config
}

func (mqo *MessageQueueOptimizer) OptimizePerformance() error {
    // Enable connection pooling
    if mqo.config.GetBool("message_queues.performance.connection_pooling") {
        mqo.setupConnectionPooling()
    }
    
    // Enable message batching
    if mqo.config.GetBool("message_queues.performance.batching") {
        mqo.setupMessageBatching()
    }
    
    // Optimize serialization
    if err := mqo.optimizeSerialization(); err != nil {
        return fmt.Errorf("failed to optimize serialization: %w", err)
    }
    
    return nil
}

func (mqo *MessageQueueOptimizer) setupConnectionPooling() {
    // Setup connection pooling for better performance
    // This is a simplified implementation
}

func (mqo *MessageQueueOptimizer) setupMessageBatching() {
    // Setup message batching for better throughput
    // This is a simplified implementation
}
```

## Message Queue Notes

- **Reliability**: Use persistent messages and acknowledgments
- **Dead Letter Queues**: Implement dead letter queues for failed messages
- **Retry Logic**: Implement retry logic with exponential backoff
- **Message Priority**: Use message priority for important messages
- **Queue Monitoring**: Monitor queue health and performance
- **Message Validation**: Validate messages before processing
- **Connection Pooling**: Use connection pooling for better performance
- **Message Batching**: Use message batching for better throughput

## Best Practices

1. **Message Design**: Design messages with clear, descriptive types
2. **Reliability**: Use persistent messages and proper acknowledgments
3. **Dead Letter Queues**: Implement dead letter queues for failed messages
4. **Retry Logic**: Implement retry logic with exponential backoff
5. **Message Priority**: Use message priority for important messages
6. **Queue Monitoring**: Monitor queue health and performance
7. **Message Validation**: Validate all messages before processing
8. **Performance**: Optimize message queue performance

## Integration with TuskLang

```go
// Load message queue configuration from TuskLang
func LoadMessageQueueConfig(configPath string) (*tusk.Config, error) {
    config, err := tusk.LoadConfig(configPath)
    if err != nil {
        return nil, fmt.Errorf("failed to load message queue config: %w", err)
    }
    
    // Validate message queue configuration
    if err := ValidateMessageQueueConfig(config); err != nil {
        return nil, fmt.Errorf("invalid message queue config: %w", err)
    }
    
    return config, nil
}
```

This message queues guide provides comprehensive message queue capabilities for your Go applications using TuskLang. Remember, good message queue design is essential for reliable, scalable communication. 