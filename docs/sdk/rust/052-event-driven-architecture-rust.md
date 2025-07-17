# Event-Driven Architecture in TuskLang with Rust

## 📡 Event-Driven Foundation

Event-driven architecture with TuskLang and Rust provides scalable, loosely-coupled, and reactive systems. This guide covers event sourcing, CQRS, message queues, and advanced event-driven patterns.

## 🏗️ Architecture Overview

### Event-Driven Stack

```rust
[event_driven_architecture]
event_sourcing: true
cqrs: true
message_queues: true
reactive_streams: true

[event_components]
tokio: "async_runtime"
serde: "serialization"
tracing: "observability"
uuid: "event_identification"
```

### Event Configuration

```rust
[event_configuration]
enable_event_sourcing: true
enable_cqrs: true
enable_message_queues: true
enable_streaming: true

[event_implementation]
use serde::{Deserialize, Serialize};
use tokio::sync::mpsc;
use tracing::{info, error, instrument};
use std::sync::Arc;
use tokio::sync::RwLock;
use uuid::Uuid;

// Event store
pub struct EventStore {
    events: Arc<RwLock<Vec<StoredEvent>>>,
    snapshots: Arc<RwLock<HashMap<String, Snapshot>>>,
    projections: Arc<RwLock<HashMap<String, Projection>>>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct StoredEvent {
    pub id: String,
    pub aggregate_id: String,
    pub event_type: String,
    pub data: serde_json::Value,
    pub metadata: EventMetadata,
    pub version: u64,
    pub timestamp: chrono::DateTime<chrono::Utc>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct EventMetadata {
    pub correlation_id: String,
    pub causation_id: String,
    pub user_id: Option<String>,
    pub source: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Snapshot {
    pub aggregate_id: String,
    pub data: serde_json::Value,
    pub version: u64,
    pub timestamp: chrono::DateTime<chrono::Utc>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Projection {
    pub name: String,
    pub data: serde_json::Value,
    pub last_event_id: String,
    pub updated_at: chrono::DateTime<chrono::Utc>,
}

impl EventStore {
    pub fn new() -> Self {
        Self {
            events: Arc::new(RwLock::new(Vec::new())),
            snapshots: Arc::new(RwLock::new(HashMap::new())),
            projections: Arc::new(RwLock::new(HashMap::new())),
        }
    }
    
    pub async fn append_events(&self, aggregate_id: &str, events: Vec<Event>) -> Result<(), EventStoreError> {
        let mut stored_events = self.events.write().await;
        let current_version = stored_events.iter()
            .filter(|e| e.aggregate_id == aggregate_id)
            .map(|e| e.version)
            .max()
            .unwrap_or(0);
        
        for (index, event) in events.iter().enumerate() {
            let stored_event = StoredEvent {
                id: Uuid::new_v4().to_string(),
                aggregate_id: aggregate_id.to_string(),
                event_type: event.event_type.clone(),
                data: serde_json::to_value(event)
                    .map_err(|e| EventStoreError::SerializationError { message: e.to_string() })?,
                metadata: event.metadata.clone(),
                version: current_version + index as u64 + 1,
                timestamp: chrono::Utc::now(),
            };
            
            stored_events.push(stored_event);
        }
        
        info!("Appended {} events for aggregate: {}", events.len(), aggregate_id);
        Ok(())
    }
    
    pub async fn get_events(&self, aggregate_id: &str, from_version: Option<u64>) -> Result<Vec<StoredEvent>, EventStoreError> {
        let events = self.events.read().await;
        let filtered_events: Vec<StoredEvent> = events.iter()
            .filter(|e| e.aggregate_id == aggregate_id)
            .filter(|e| from_version.map_or(true, |v| e.version > v))
            .cloned()
            .collect();
        
        Ok(filtered_events)
    }
    
    pub async fn save_snapshot(&self, snapshot: Snapshot) -> Result<(), EventStoreError> {
        let mut snapshots = self.snapshots.write().await;
        snapshots.insert(snapshot.aggregate_id.clone(), snapshot);
        Ok(())
    }
    
    pub async fn get_snapshot(&self, aggregate_id: &str) -> Option<Snapshot> {
        let snapshots = self.snapshots.read().await;
        snapshots.get(aggregate_id).cloned()
    }
    
    pub async fn update_projection(&self, projection: Projection) -> Result<(), EventStoreError> {
        let mut projections = self.projections.write().await;
        projections.insert(projection.name.clone(), projection);
        Ok(())
    }
    
    pub async fn get_projection(&self, name: &str) -> Option<Projection> {
        let projections = self.projections.read().await;
        projections.get(name).cloned()
    }
}

#[derive(Debug, thiserror::Error)]
pub enum EventStoreError {
    #[error("Serialization error: {message}")]
    SerializationError { message: String },
    #[error("Aggregate not found: {aggregate_id}")]
    AggregateNotFound { aggregate_id: String },
    #[error("Concurrency error: {message}")]
    ConcurrencyError { message: String },
}
```

## 📝 Event Sourcing

### Event Sourcing Implementation

```rust
[event_sourcing]
aggregate_pattern: true
event_replay: true
snapshot_pattern: true
projection_pattern: true

[event_sourcing_implementation]
// Base event trait
pub trait Event: Serialize + DeserializeOwned + Send + Sync {
    fn event_type(&self) -> &str;
    fn aggregate_id(&self) -> &str;
}

// Base aggregate trait
pub trait Aggregate: Send + Sync {
    type Event: Event;
    type Error: std::error::Error + Send + Sync;
    
    fn aggregate_id(&self) -> &str;
    fn version(&self) -> u64;
    fn apply_event(&mut self, event: &Self::Event) -> Result<(), Self::Error>;
    fn snapshot(&self) -> serde_json::Value;
    fn load_from_snapshot(&mut self, snapshot: serde_json::Value) -> Result<(), Self::Error>;
}

// User aggregate
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct User {
    pub id: String,
    pub username: String,
    pub email: String,
    pub status: UserStatus,
    pub version: u64,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub enum UserStatus {
    Active,
    Inactive,
    Suspended,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub enum UserEvent {
    UserCreated {
        id: String,
        username: String,
        email: String,
        metadata: EventMetadata,
    },
    UserUpdated {
        username: Option<String>,
        email: Option<String>,
        metadata: EventMetadata,
    },
    UserStatusChanged {
        status: UserStatus,
        metadata: EventMetadata,
    },
}

impl Event for UserEvent {
    fn event_type(&self) -> &str {
        match self {
            UserEvent::UserCreated { .. } => "user.created",
            UserEvent::UserUpdated { .. } => "user.updated",
            UserEvent::UserStatusChanged { .. } => "user.status_changed",
        }
    }
    
    fn aggregate_id(&self) -> &str {
        match self {
            UserEvent::UserCreated { id, .. } => id,
            UserEvent::UserUpdated { .. } => &self.aggregate_id(),
            UserEvent::UserStatusChanged { .. } => &self.aggregate_id(),
        }
    }
}

impl Aggregate for User {
    type Event = UserEvent;
    type Error = UserError;
    
    fn aggregate_id(&self) -> &str {
        &self.id
    }
    
    fn version(&self) -> u64 {
        self.version
    }
    
    fn apply_event(&mut self, event: &Self::Event) -> Result<(), Self::Error> {
        match event {
            UserEvent::UserCreated { id, username, email, .. } => {
                self.id = id.clone();
                self.username = username.clone();
                self.email = email.clone();
                self.status = UserStatus::Active;
                self.version += 1;
            }
            UserEvent::UserUpdated { username, email, .. } => {
                if let Some(username) = username {
                    self.username = username.clone();
                }
                if let Some(email) = email {
                    self.email = email.clone();
                }
                self.version += 1;
            }
            UserEvent::UserStatusChanged { status, .. } => {
                self.status = status.clone();
                self.version += 1;
            }
        }
        Ok(())
    }
    
    fn snapshot(&self) -> serde_json::Value {
        serde_json::to_value(self).unwrap()
    }
    
    fn load_from_snapshot(&mut self, snapshot: serde_json::Value) -> Result<(), Self::Error> {
        let user: User = serde_json::from_value(snapshot)
            .map_err(|e| UserError::DeserializationError { message: e.to_string() })?;
        *self = user;
        Ok(())
    }
}

// User repository with event sourcing
pub struct UserRepository {
    event_store: Arc<EventStore>,
}

impl UserRepository {
    pub fn new(event_store: Arc<EventStore>) -> Self {
        Self { event_store }
    }
    
    pub async fn save(&self, user: &User, events: Vec<UserEvent>) -> Result<(), UserError> {
        // Check for concurrency conflicts
        let stored_events = self.event_store.get_events(&user.id, None).await
            .map_err(|e| UserError::EventStoreError { message: e.to_string() })?;
        
        let expected_version = stored_events.len() as u64;
        if user.version() != expected_version {
            return Err(UserError::ConcurrencyError { 
                message: format!("Expected version {}, got {}", expected_version, user.version()) 
            });
        }
        
        // Append events
        self.event_store.append_events(&user.id, events).await
            .map_err(|e| UserError::EventStoreError { message: e.to_string() })?;
        
        // Save snapshot every 10 events
        if (user.version() + 1) % 10 == 0 {
            let snapshot = Snapshot {
                aggregate_id: user.id.clone(),
                data: user.snapshot(),
                version: user.version(),
                timestamp: chrono::Utc::now(),
            };
            
            self.event_store.save_snapshot(snapshot).await
                .map_err(|e| UserError::EventStoreError { message: e.to_string() })?;
        }
        
        Ok(())
    }
    
    pub async fn load(&self, id: &str) -> Result<Option<User>, UserError> {
        // Try to load from snapshot first
        if let Some(snapshot) = self.event_store.get_snapshot(id).await {
            let mut user = User {
                id: id.to_string(),
                username: String::new(),
                email: String::new(),
                status: UserStatus::Inactive,
                version: 0,
            };
            
            user.load_from_snapshot(snapshot.data)?;
            
            // Load events after snapshot
            let events = self.event_store.get_events(id, Some(snapshot.version)).await
                .map_err(|e| UserError::EventStoreError { message: e.to_string() })?;
            
            for stored_event in events {
                let event: UserEvent = serde_json::from_value(stored_event.data)
                    .map_err(|e| UserError::DeserializationError { message: e.to_string() })?;
                user.apply_event(&event)?;
            }
            
            Ok(Some(user))
        } else {
            // Load all events
            let events = self.event_store.get_events(id, None).await
                .map_err(|e| UserError::EventStoreError { message: e.to_string() })?;
            
            if events.is_empty() {
                Ok(None)
            } else {
                let mut user = User {
                    id: id.to_string(),
                    username: String::new(),
                    email: String::new(),
                    status: UserStatus::Inactive,
                    version: 0,
                };
                
                for stored_event in events {
                    let event: UserEvent = serde_json::from_value(stored_event.data)
                        .map_err(|e| UserError::DeserializationError { message: e.to_string() })?;
                    user.apply_event(&event)?;
                }
                
                Ok(Some(user))
            }
        }
    }
}

#[derive(Debug, thiserror::Error)]
pub enum UserError {
    #[error("Event store error: {message}")]
    EventStoreError { message: String },
    #[error("Deserialization error: {message}")]
    DeserializationError { message: String },
    #[error("Concurrency error: {message}")]
    ConcurrencyError { message: String },
    #[error("Validation error: {message}")]
    ValidationError { message: String },
}
```

## 🔄 CQRS (Command Query Responsibility Segregation)

### CQRS Implementation

```rust
[cqrs_architecture]
command_handlers: true
query_handlers: true
read_models: true
write_models: true

[cqrs_implementation]
// Command trait
pub trait Command: Send + Sync {
    type Aggregate: Aggregate;
    type Error: std::error::Error + Send + Sync;
    
    fn aggregate_id(&self) -> &str;
    fn execute(&self, aggregate: &mut Self::Aggregate) -> Result<Vec<<Self::Aggregate as Aggregate>::Event>, Self::Error>;
}

// Query trait
pub trait Query: Send + Sync {
    type Result;
    type Error: std::error::Error + Send + Sync;
}

// Command handler
pub struct CommandHandler<C: Command> {
    repository: Arc<UserRepository>,
    event_publisher: Arc<EventPublisher>,
}

impl<C: Command<Aggregate = User>> CommandHandler<C> {
    pub fn new(repository: Arc<UserRepository>, event_publisher: Arc<EventPublisher>) -> Self {
        Self {
            repository,
            event_publisher,
        }
    }
    
    pub async fn handle(&self, command: C) -> Result<(), C::Error> {
        // Load aggregate
        let mut aggregate = self.repository.load(command.aggregate_id()).await
            .map_err(|e| C::Error::from(e))?;
        
        let aggregate = aggregate.get_or_insert_with(|| User {
            id: command.aggregate_id().to_string(),
            username: String::new(),
            email: String::new(),
            status: UserStatus::Inactive,
            version: 0,
        });
        
        // Execute command
        let events = command.execute(aggregate)?;
        
        // Save aggregate
        self.repository.save(aggregate, events.clone()).await
            .map_err(|e| C::Error::from(e))?;
        
        // Publish events
        for event in events {
            self.event_publisher.publish(&event).await
                .map_err(|e| C::Error::from(e))?;
        }
        
        Ok(())
    }
}

// User commands
#[derive(Debug, Clone)]
pub struct CreateUserCommand {
    pub id: String,
    pub username: String,
    pub email: String,
    pub metadata: EventMetadata,
}

#[derive(Debug, Clone)]
pub struct UpdateUserCommand {
    pub id: String,
    pub username: Option<String>,
    pub email: Option<String>,
    pub metadata: EventMetadata,
}

#[derive(Debug, Clone)]
pub struct ChangeUserStatusCommand {
    pub id: String,
    pub status: UserStatus,
    pub metadata: EventMetadata,
}

impl Command for CreateUserCommand {
    type Aggregate = User;
    type Error = UserError;
    
    fn aggregate_id(&self) -> &str {
        &self.id
    }
    
    fn execute(&self, _aggregate: &mut Self::Aggregate) -> Result<Vec<UserEvent>, Self::Error> {
        let event = UserEvent::UserCreated {
            id: self.id.clone(),
            username: self.username.clone(),
            email: self.email.clone(),
            metadata: self.metadata.clone(),
        };
        
        Ok(vec![event])
    }
}

impl Command for UpdateUserCommand {
    type Aggregate = User;
    type Error = UserError;
    
    fn aggregate_id(&self) -> &str {
        &self.id
    }
    
    fn execute(&self, aggregate: &mut Self::Aggregate) -> Result<Vec<UserEvent>, Self::Error> {
        let event = UserEvent::UserUpdated {
            username: self.username.clone(),
            email: self.email.clone(),
            metadata: self.metadata.clone(),
        };
        
        Ok(vec![event])
    }
}

impl Command for ChangeUserStatusCommand {
    type Aggregate = User;
    type Error = UserError;
    
    fn aggregate_id(&self) -> &str {
        &self.id
    }
    
    fn execute(&self, _aggregate: &mut Self::Aggregate) -> Result<Vec<UserEvent>, Self::Error> {
        let event = UserEvent::UserStatusChanged {
            status: self.status.clone(),
            metadata: self.metadata.clone(),
        };
        
        Ok(vec![event])
    }
}

// Query handlers
pub struct QueryHandler {
    read_model: Arc<UserReadModel>,
}

impl QueryHandler {
    pub fn new(read_model: Arc<UserReadModel>) -> Self {
        Self { read_model }
    }
    
    pub async fn handle<Q: Query>(&self, query: Q) -> Result<Q::Result, Q::Error> {
        // Route query to appropriate handler
        // This is a simplified implementation
        todo!("Implement query routing")
    }
}

// User queries
#[derive(Debug, Clone)]
pub struct GetUserQuery {
    pub id: String,
}

#[derive(Debug, Clone)]
pub struct ListUsersQuery {
    pub page: u32,
    pub per_page: u32,
    pub status: Option<UserStatus>,
}

impl Query for GetUserQuery {
    type Result = Option<UserReadModel>;
    type Error = UserError;
}

impl Query for ListUsersQuery {
    type Result = Vec<UserReadModel>;
    type Error = UserError;
}

// Read model
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct UserReadModel {
    pub id: String,
    pub username: String,
    pub email: String,
    pub status: UserStatus,
    pub created_at: chrono::DateTime<chrono::Utc>,
    pub updated_at: chrono::DateTime<chrono::Utc>,
}

pub struct UserReadModel {
    users: Arc<RwLock<HashMap<String, UserReadModel>>>,
}

impl UserReadModel {
    pub fn new() -> Self {
        Self {
            users: Arc::new(RwLock::new(HashMap::new())),
        }
    }
    
    pub async fn get_user(&self, id: &str) -> Option<UserReadModel> {
        let users = self.users.read().await;
        users.get(id).cloned()
    }
    
    pub async fn list_users(&self, page: u32, per_page: u32, status: Option<UserStatus>) -> Vec<UserReadModel> {
        let users = self.users.read().await;
        let mut filtered_users: Vec<UserReadModel> = users.values()
            .filter(|user| status.as_ref().map_or(true, |s| user.status == *s))
            .cloned()
            .collect();
        
        // Sort by created_at
        filtered_users.sort_by(|a, b| b.created_at.cmp(&a.created_at));
        
        // Paginate
        let start = (page as usize * per_page as usize).min(filtered_users.len());
        let end = (start + per_page as usize).min(filtered_users.len());
        
        filtered_users[start..end].to_vec()
    }
    
    pub async fn update_user(&self, user: UserReadModel) {
        let mut users = self.users.write().await;
        users.insert(user.id.clone(), user);
    }
}
```

## 📨 Message Queues

### Message Queue Implementation

```rust
[message_queues]
publish_subscribe: true
point_to_point: true
dead_letter_queues: true
message_persistence: true

[message_queue_implementation]
// Message queue
pub struct MessageQueue {
    topics: Arc<RwLock<HashMap<String, Topic>>>,
    subscriptions: Arc<RwLock<HashMap<String, Vec<Subscription>>>>,
    dead_letter_queue: Arc<RwLock<Vec<DeadLetterMessage>>>,
}

#[derive(Debug, Clone)]
pub struct Topic {
    pub name: String,
    pub messages: Vec<Message>,
    pub max_retries: u32,
    pub retention_period: Duration,
}

#[derive(Debug, Clone)]
pub struct Message {
    pub id: String,
    pub topic: String,
    pub data: serde_json::Value,
    pub metadata: MessageMetadata,
    pub created_at: chrono::DateTime<chrono::Utc>,
    pub delivery_count: u32,
}

#[derive(Debug, Clone)]
pub struct MessageMetadata {
    pub correlation_id: String,
    pub causation_id: String,
    pub user_id: Option<String>,
    pub priority: MessagePriority,
    pub ttl: Option<Duration>,
}

#[derive(Debug, Clone)]
pub enum MessagePriority {
    Low,
    Normal,
    High,
    Critical,
}

#[derive(Debug, Clone)]
pub struct Subscription {
    pub id: String,
    pub topic: String,
    pub handler: Box<dyn MessageHandler>,
    pub filter: Option<MessageFilter>,
    pub max_retries: u32,
    pub retry_delay: Duration,
}

#[derive(Debug, Clone)]
pub struct MessageFilter {
    pub field: String,
    pub operator: FilterOperator,
    pub value: serde_json::Value,
}

#[derive(Debug, Clone)]
pub enum FilterOperator {
    Equals,
    NotEquals,
    Contains,
    GreaterThan,
    LessThan,
}

#[derive(Debug, Clone)]
pub struct DeadLetterMessage {
    pub message: Message,
    pub error: String,
    pub failed_at: chrono::DateTime<chrono::Utc>,
}

impl MessageQueue {
    pub fn new() -> Self {
        Self {
            topics: Arc::new(RwLock::new(HashMap::new())),
            subscriptions: Arc::new(RwLock::new(HashMap::new())),
            dead_letter_queue: Arc::new(RwLock::new(Vec::new())),
        }
    }
    
    pub async fn create_topic(&self, name: &str, max_retries: u32, retention_period: Duration) -> Result<(), MessageQueueError> {
        let mut topics = self.topics.write().await;
        
        if topics.contains_key(name) {
            return Err(MessageQueueError::TopicAlreadyExists { name: name.to_string() });
        }
        
        let topic = Topic {
            name: name.to_string(),
            messages: Vec::new(),
            max_retries,
            retention_period,
        };
        
        topics.insert(name.to_string(), topic);
        info!("Topic created: {}", name);
        Ok(())
    }
    
    pub async fn publish<T: Serialize>(&self, topic: &str, data: &T, metadata: MessageMetadata) -> Result<(), MessageQueueError> {
        let message = Message {
            id: Uuid::new_v4().to_string(),
            topic: topic.to_string(),
            data: serde_json::to_value(data)
                .map_err(|e| MessageQueueError::SerializationError { message: e.to_string() })?,
            metadata,
            created_at: chrono::Utc::now(),
            delivery_count: 0,
        };
        
        // Add to topic
        {
            let mut topics = self.topics.write().await;
            if let Some(topic_data) = topics.get_mut(topic) {
                topic_data.messages.push(message.clone());
            } else {
                return Err(MessageQueueError::TopicNotFound { name: topic.to_string() });
            }
        }
        
        // Deliver to subscribers
        self.deliver_message(&message).await?;
        
        info!("Message published to topic {}: {}", topic, message.id);
        Ok(())
    }
    
    pub async fn subscribe(
        &self,
        topic: &str,
        handler: Box<dyn MessageHandler>,
        filter: Option<MessageFilter>,
    ) -> Result<String, MessageQueueError> {
        let subscription_id = Uuid::new_v4().to_string();
        
        let subscription = Subscription {
            id: subscription_id.clone(),
            topic: topic.to_string(),
            handler,
            filter,
            max_retries: 3,
            retry_delay: Duration::from_secs(5),
        };
        
        let mut subscriptions = self.subscriptions.write().await;
        subscriptions.entry(topic.to_string())
            .or_insert_with(Vec::new)
            .push(subscription);
        
        info!("Subscription created for topic {}: {}", topic, subscription_id);
        Ok(subscription_id)
    }
    
    async fn deliver_message(&self, message: &Message) -> Result<(), MessageQueueError> {
        let subscriptions = self.subscriptions.read().await;
        
        if let Some(topic_subscriptions) = subscriptions.get(&message.topic) {
            for subscription in topic_subscriptions {
                // Check filter
                if let Some(filter) = &subscription.filter {
                    if !self.matches_filter(&message.data, filter) {
                        continue;
                    }
                }
                
                // Deliver message
                let message_clone = message.clone();
                let subscription_clone = subscription.clone();
                let dead_letter_queue = self.dead_letter_queue.clone();
                
                tokio::spawn(async move {
                    let mut retry_count = 0;
                    
                    while retry_count < subscription_clone.max_retries {
                        match subscription_clone.handler.handle(&message_clone).await {
                            Ok(_) => {
                                info!("Message delivered successfully: {}", message_clone.id);
                                break;
                            }
                            Err(e) => {
                                retry_count += 1;
                                error!("Message delivery failed (attempt {}): {}", retry_count, e);
                                
                                if retry_count >= subscription_clone.max_retries {
                                    // Send to dead letter queue
                                    let dead_letter = DeadLetterMessage {
                                        message: message_clone.clone(),
                                        error: e.to_string(),
                                        failed_at: chrono::Utc::now(),
                                    };
                                    
                                    let mut dlq = dead_letter_queue.write().await;
                                    dlq.push(dead_letter);
                                    break;
                                }
                                
                                tokio::time::sleep(subscription_clone.retry_delay).await;
                            }
                        }
                    }
                });
            }
        }
        
        Ok(())
    }
    
    fn matches_filter(&self, data: &serde_json::Value, filter: &MessageFilter) -> bool {
        if let Some(value) = data.get(&filter.field) {
            match filter.operator {
                FilterOperator::Equals => value == &filter.value,
                FilterOperator::NotEquals => value != &filter.value,
                FilterOperator::Contains => {
                    if let Some(str_value) = value.as_str() {
                        if let Some(filter_str) = filter.value.as_str() {
                            return str_value.contains(filter_str);
                        }
                    }
                    false
                }
                FilterOperator::GreaterThan => value > &filter.value,
                FilterOperator::LessThan => value < &filter.value,
            }
        } else {
            false
        }
    }
    
    pub async fn get_dead_letter_messages(&self) -> Vec<DeadLetterMessage> {
        let dlq = self.dead_letter_queue.read().await;
        dlq.clone()
    }
}

// Message handler trait
#[async_trait::async_trait]
pub trait MessageHandler: Send + Sync {
    async fn handle(&self, message: &Message) -> Result<(), MessageHandlerError>;
}

#[derive(Debug, thiserror::Error)]
pub enum MessageHandlerError {
    #[error("Processing error: {message}")]
    ProcessingError { message: String },
    #[error("Validation error: {message}")]
    ValidationError { message: String },
}

#[derive(Debug, thiserror::Error)]
pub enum MessageQueueError {
    #[error("Topic not found: {name}")]
    TopicNotFound { name: String },
    #[error("Topic already exists: {name}")]
    TopicAlreadyExists { name: String },
    #[error("Serialization error: {message}")]
    SerializationError { message: String },
    #[error("Delivery error: {message}")]
    DeliveryError { message: String },
}
```

## 🎯 Best Practices

### 1. **Event Design**
- Design events to be immutable and self-contained
- Use descriptive event names and clear data structures
- Include correlation and causation IDs for tracing
- Version events for backward compatibility

### 2. **Event Sourcing**
- Use snapshots for performance optimization
- Implement proper concurrency control
- Design aggregates with clear boundaries
- Use event replay for debugging and testing

### 3. **CQRS Implementation**
- Separate read and write models completely
- Use projections for complex queries
- Implement eventual consistency patterns
- Optimize read models for specific use cases

### 4. **Message Queues**
- Implement proper error handling and retries
- Use dead letter queues for failed messages
- Implement message filtering and routing
- Monitor queue performance and health

### 5. **TuskLang Integration**
- Use TuskLang configuration for event store settings
- Configure message queue parameters through TuskLang
- Implement event routing with TuskLang
- Use TuskLang for CQRS configuration

Event-driven architecture with TuskLang and Rust provides scalable, loosely-coupled, and reactive systems with comprehensive patterns for event sourcing, CQRS, and message queuing. 