# 🦀 TuskLang Rust Event-Driven Architecture

**"We don't bow to any king" - Rust Edition**

Master event-driven architecture with TuskLang Rust. From event sourcing to CQRS, from message queues to event streaming - build reactive, scalable systems that respond to events in real-time.

## 🎯 Event Sourcing Foundation

### Event Store Implementation

```rust
use tusklang_rust::{parse, Parser};
use serde::{Deserialize, Serialize};
use std::collections::HashMap;
use std::sync::Arc;
use tokio::sync::RwLock;
use chrono::{DateTime, Utc};

#[derive(Debug, Clone, Serialize, Deserialize)]
struct Event {
    id: String,
    aggregate_id: String,
    event_type: String,
    data: serde_json::Value,
    metadata: EventMetadata,
    version: u64,
    timestamp: DateTime<Utc>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
struct EventMetadata {
    user_id: Option<String>,
    correlation_id: Option<String>,
    causation_id: Option<String>,
    source: String,
}

struct EventStore {
    events: Arc<RwLock<HashMap<String, Vec<Event>>>>,
    parser: Parser,
}

impl EventStore {
    fn new() -> Self {
        Self {
            events: Arc::new(RwLock::new(HashMap::new())),
            parser: Parser::new(),
        }
    }
    
    async fn append_events(&self, aggregate_id: &str, events: Vec<Event>) -> Result<(), Box<dyn std::error::Error>> {
        let mut event_store = self.events.write().await;
        
        let aggregate_events = event_store.entry(aggregate_id.to_string())
            .or_insert_with(Vec::new);
        
        // Check version consistency
        let expected_version = aggregate_events.len() as u64;
        for event in &events {
            if event.version != expected_version {
                return Err("Event version mismatch".into());
            }
        }
        
        // Append events
        aggregate_events.extend(events);
        
        Ok(())
    }
    
    async fn get_events(&self, aggregate_id: &str, from_version: u64) -> Vec<Event> {
        let event_store = self.events.read().await;
        
        if let Some(events) = event_store.get(aggregate_id) {
            events.iter()
                .filter(|event| event.version >= from_version)
                .cloned()
                .collect()
        } else {
            Vec::new()
        }
    }
    
    async fn get_all_events(&self, aggregate_id: &str) -> Vec<Event> {
        let event_store = self.events.read().await;
        event_store.get(aggregate_id).cloned().unwrap_or_default()
    }
    
    async fn get_events_by_type(&self, event_type: &str) -> Vec<Event> {
        let event_store = self.events.read().await;
        
        event_store.values()
            .flatten()
            .filter(|event| event.event_type == event_type)
            .cloned()
            .collect()
    }
}

#[tokio::main]
async fn event_sourcing_foundation() -> Result<(), Box<dyn std::error::Error>> {
    println!("🎯 Event Sourcing Foundation");
    
    let event_store = EventStore::new();
    
    // Load event sourcing configuration
    let config = event_store.parser.parse(r#"
[event_sourcing]
enabled: true
snapshot_interval: 100
event_store_type: "in_memory"
persistence_enabled: true
event_serialization: "json"
"#).await?;
    
    // Create sample events
    let user_created = Event {
        id: uuid::Uuid::new_v4().to_string(),
        aggregate_id: "user_123".to_string(),
        event_type: "UserCreated".to_string(),
        data: serde_json::json!({
            "name": "Alice Johnson",
            "email": "alice@example.com"
        }),
        metadata: EventMetadata {
            user_id: Some("system".to_string()),
            correlation_id: Some("corr_123".to_string()),
            causation_id: None,
            source: "user-service".to_string(),
        },
        version: 0,
        timestamp: Utc::now(),
    };
    
    let user_updated = Event {
        id: uuid::Uuid::new_v4().to_string(),
        aggregate_id: "user_123".to_string(),
        event_type: "UserUpdated".to_string(),
        data: serde_json::json!({
            "name": "Alice Smith",
            "email": "alice.smith@example.com"
        }),
        metadata: EventMetadata {
            user_id: Some("user_123".to_string()),
            correlation_id: Some("corr_123".to_string()),
            causation_id: Some(user_created.id.clone()),
            source: "user-service".to_string(),
        },
        version: 1,
        timestamp: Utc::now(),
    };
    
    // Append events
    event_store.append_events("user_123", vec![user_created, user_updated]).await?;
    println!("✅ Events appended to event store");
    
    // Retrieve events
    let events = event_store.get_all_events("user_123").await;
    println!("📊 Retrieved {} events for user_123", events.len());
    
    for event in events {
        println!("  Event: {} (v{}) - {}", 
            event.event_type, event.version, event.timestamp);
    }
    
    Ok(())
}
```

### Aggregate Pattern

```rust
use std::collections::HashMap;

#[derive(Debug, Clone)]
struct User {
    id: String,
    name: String,
    email: String,
    is_active: bool,
    created_at: DateTime<Utc>,
    updated_at: DateTime<Utc>,
    version: u64,
}

impl User {
    fn new(id: String, name: String, email: String) -> Self {
        Self {
            id,
            name,
            email,
            is_active: true,
            created_at: Utc::now(),
            updated_at: Utc::now(),
            version: 0,
        }
    }
    
    fn apply_event(&mut self, event: &Event) -> Result<(), Box<dyn std::error::Error>> {
        match event.event_type.as_str() {
            "UserCreated" => {
                let data = event.data.as_object().unwrap();
                self.name = data["name"].as_str().unwrap().to_string();
                self.email = data["email"].as_str().unwrap().to_string();
                self.created_at = event.timestamp;
            }
            "UserUpdated" => {
                let data = event.data.as_object().unwrap();
                self.name = data["name"].as_str().unwrap().to_string();
                self.email = data["email"].as_str().unwrap().to_string();
                self.updated_at = event.timestamp;
            }
            "UserDeactivated" => {
                self.is_active = false;
                self.updated_at = event.timestamp;
            }
            "UserActivated" => {
                self.is_active = true;
                self.updated_at = event.timestamp;
            }
            _ => return Err(format!("Unknown event type: {}", event.event_type).into()),
        }
        
        self.version = event.version + 1;
        Ok(())
    }
    
    fn create_user(id: String, name: String, email: String) -> (Self, Vec<Event>) {
        let user = User::new(id.clone(), name.clone(), email.clone());
        
        let event = Event {
            id: uuid::Uuid::new_v4().to_string(),
            aggregate_id: id,
            event_type: "UserCreated".to_string(),
            data: serde_json::json!({
                "name": name,
                "email": email
            }),
            metadata: EventMetadata {
                user_id: Some("system".to_string()),
                correlation_id: Some(uuid::Uuid::new_v4().to_string()),
                causation_id: None,
                source: "user-service".to_string(),
            },
            version: 0,
            timestamp: Utc::now(),
        };
        
        (user, vec![event])
    }
    
    fn update_user(&mut self, name: String, email: String) -> Vec<Event> {
        let event = Event {
            id: uuid::Uuid::new_v4().to_string(),
            aggregate_id: self.id.clone(),
            event_type: "UserUpdated".to_string(),
            data: serde_json::json!({
                "name": name,
                "email": email
            }),
            metadata: EventMetadata {
                user_id: Some(self.id.clone()),
                correlation_id: Some(uuid::Uuid::new_v4().to_string()),
                causation_id: None,
                source: "user-service".to_string(),
            },
            version: self.version,
            timestamp: Utc::now(),
        };
        
        vec![event]
    }
    
    fn deactivate(&mut self) -> Vec<Event> {
        let event = Event {
            id: uuid::Uuid::new_v4().to_string(),
            aggregate_id: self.id.clone(),
            event_type: "UserDeactivated".to_string(),
            data: serde_json::json!({}),
            metadata: EventMetadata {
                user_id: Some(self.id.clone()),
                correlation_id: Some(uuid::Uuid::new_v4().to_string()),
                causation_id: None,
                source: "user-service".to_string(),
            },
            version: self.version,
            timestamp: Utc::now(),
        };
        
        vec![event]
    }
}

struct UserRepository {
    event_store: Arc<EventStore>,
}

impl UserRepository {
    fn new(event_store: Arc<EventStore>) -> Self {
        Self { event_store }
    }
    
    async fn save(&self, user: &User, events: Vec<Event>) -> Result<(), Box<dyn std::error::Error>> {
        self.event_store.append_events(&user.id, events).await
    }
    
    async fn load(&self, user_id: &str) -> Result<Option<User>, Box<dyn std::error::Error>> {
        let events = self.event_store.get_all_events(user_id).await;
        
        if events.is_empty() {
            return Ok(None);
        }
        
        let mut user = User::new(
            user_id.to_string(),
            "".to_string(),
            "".to_string(),
        );
        
        for event in events {
            user.apply_event(&event)?;
        }
        
        Ok(Some(user))
    }
}
```

## 📨 Message Queue Integration

### RabbitMQ Integration

```rust
use lapin::{Connection, Channel, BasicProperties};
use serde_json::json;

struct MessageQueue {
    channel: Channel,
    parser: Parser,
}

impl MessageQueue {
    async fn new(amqp_url: &str) -> Result<Self, Box<dyn std::error::Error>> {
        let conn = Connection::connect(amqp_url, Default::default()).await?;
        let channel = conn.create_channel().await?;
        
        Ok(Self {
            channel,
            parser: Parser::new(),
        })
    }
    
    async fn publish_event(&self, exchange: &str, routing_key: &str, event: &Event) -> Result<(), Box<dyn std::error::Error>> {
        let message = json!({
            "event_id": event.id,
            "aggregate_id": event.aggregate_id,
            "event_type": event.event_type,
            "data": event.data,
            "metadata": event.metadata,
            "version": event.version,
            "timestamp": event.timestamp.to_rfc3339(),
        });
        
        let payload = serde_json::to_vec(&message)?;
        
        self.channel
            .basic_publish(
                exchange,
                routing_key,
                Default::default(),
                &payload,
                BasicProperties::default(),
            )
            .await?;
        
        Ok(())
    }
    
    async fn consume_events<F>(&self, queue: &str, handler: F) -> Result<(), Box<dyn std::error::Error>>
    where
        F: Fn(Event) -> Result<(), Box<dyn std::error::Error>> + Send + 'static,
    {
        let consumer = self.channel
            .basic_consume(
                queue,
                "event_consumer",
                Default::default(),
                Default::default(),
            )
            .await?;
        
        for delivery in consumer {
            let (_, delivery) = delivery?;
            
            let event: Event = serde_json::from_slice(&delivery.data)?;
            
            match handler(event) {
                Ok(_) => {
                    delivery.ack(Default::default()).await?;
                }
                Err(e) => {
                    println!("Error processing event: {}", e);
                    delivery.nack(Default::default()).await?;
                }
            }
        }
        
        Ok(())
    }
}

#[tokio::main]
async fn message_queue_integration() -> Result<(), Box<dyn std::error::Error>> {
    println!("📨 Message Queue Integration");
    
    let message_queue = MessageQueue::new("amqp://localhost:5672").await?;
    
    // Load message queue configuration
    let config = message_queue.parser.parse(r#"
[message_queue]
type: "rabbitmq"
host: "localhost"
port: 5672
username: "guest"
password: "guest"
virtual_host: "/"

[exchanges]
user_events: {
    name: "user.events",
    type: "topic",
    durable: true
}
order_events: {
    name: "order.events", 
    type: "topic",
    durable: true
}
"#).await?;
    
    // Publish user created event
    let user_created = Event {
        id: uuid::Uuid::new_v4().to_string(),
        aggregate_id: "user_456".to_string(),
        event_type: "UserCreated".to_string(),
        data: json!({
            "name": "Bob Wilson",
            "email": "bob@example.com"
        }),
        metadata: EventMetadata {
            user_id: Some("system".to_string()),
            correlation_id: Some("corr_456".to_string()),
            causation_id: None,
            source: "user-service".to_string(),
        },
        version: 0,
        timestamp: Utc::now(),
    };
    
    message_queue.publish_event("user.events", "user.created", &user_created).await?;
    println!("✅ Published user created event");
    
    // Publish order created event
    let order_created = Event {
        id: uuid::Uuid::new_v4().to_string(),
        aggregate_id: "order_789".to_string(),
        event_type: "OrderCreated".to_string(),
        data: json!({
            "user_id": "user_456",
            "items": [
                {"product_id": "prod_123", "quantity": 2, "price": 29.99}
            ],
            "total": 59.98
        }),
        metadata: EventMetadata {
            user_id: Some("user_456".to_string()),
            correlation_id: Some("corr_456".to_string()),
            causation_id: Some(user_created.id.clone()),
            source: "order-service".to_string(),
        },
        version: 0,
        timestamp: Utc::now(),
    };
    
    message_queue.publish_event("order.events", "order.created", &order_created).await?;
    println!("✅ Published order created event");
    
    Ok(())
}
```

## 🔄 CQRS Implementation

### Command and Query Separation

```rust
use std::collections::HashMap;

// Commands
#[derive(Debug, Clone)]
struct CreateUserCommand {
    name: String,
    email: String,
}

#[derive(Debug, Clone)]
struct UpdateUserCommand {
    user_id: String,
    name: String,
    email: String,
}

#[derive(Debug, Clone)]
struct DeactivateUserCommand {
    user_id: String,
}

// Queries
#[derive(Debug, Clone)]
struct GetUserQuery {
    user_id: String,
}

#[derive(Debug, Clone)]
struct GetUsersQuery {
    limit: usize,
    offset: usize,
}

#[derive(Debug, Clone)]
struct SearchUsersQuery {
    search_term: String,
}

// Query Models
#[derive(Debug, Clone)]
struct UserView {
    id: String,
    name: String,
    email: String,
    is_active: bool,
    created_at: DateTime<Utc>,
    updated_at: DateTime<Utc>,
}

struct CommandHandler {
    event_store: Arc<EventStore>,
    message_queue: Arc<MessageQueue>,
}

impl CommandHandler {
    fn new(event_store: Arc<EventStore>, message_queue: Arc<MessageQueue>) -> Self {
        Self {
            event_store,
            message_queue,
        }
    }
    
    async fn handle_create_user(&self, command: CreateUserCommand) -> Result<String, Box<dyn std::error::Error>> {
        let user_id = uuid::Uuid::new_v4().to_string();
        let (user, events) = User::create_user(user_id.clone(), command.name, command.email);
        
        // Save to event store
        self.event_store.append_events(&user_id, events.clone()).await?;
        
        // Publish events
        for event in events {
            self.message_queue.publish_event("user.events", "user.created", &event).await?;
        }
        
        Ok(user_id)
    }
    
    async fn handle_update_user(&self, command: UpdateUserCommand) -> Result<(), Box<dyn std::error::Error>> {
        let mut user = self.load_user(&command.user_id).await?;
        let events = user.update_user(command.name, command.email);
        
        // Save to event store
        self.event_store.append_events(&command.user_id, events.clone()).await?;
        
        // Publish events
        for event in events {
            self.message_queue.publish_event("user.events", "user.updated", &event).await?;
        }
        
        Ok(())
    }
    
    async fn handle_deactivate_user(&self, command: DeactivateUserCommand) -> Result<(), Box<dyn std::error::Error>> {
        let mut user = self.load_user(&command.user_id).await?;
        let events = user.deactivate();
        
        // Save to event store
        self.event_store.append_events(&command.user_id, events.clone()).await?;
        
        // Publish events
        for event in events {
            self.message_queue.publish_event("user.events", "user.deactivated", &event).await?;
        }
        
        Ok(())
    }
    
    async fn load_user(&self, user_id: &str) -> Result<User, Box<dyn std::error::Error>> {
        let events = self.event_store.get_all_events(user_id).await;
        
        if events.is_empty() {
            return Err("User not found".into());
        }
        
        let mut user = User::new(
            user_id.to_string(),
            "".to_string(),
            "".to_string(),
        );
        
        for event in events {
            user.apply_event(&event)?;
        }
        
        Ok(user)
    }
}

struct QueryHandler {
    read_model: Arc<RwLock<HashMap<String, UserView>>>,
}

impl QueryHandler {
    fn new() -> Self {
        Self {
            read_model: Arc::new(RwLock::new(HashMap::new())),
        }
    }
    
    async fn handle_get_user(&self, query: GetUserQuery) -> Result<Option<UserView>, Box<dyn std::error::Error>> {
        let read_model = self.read_model.read().await;
        Ok(read_model.get(&query.user_id).cloned())
    }
    
    async fn handle_get_users(&self, query: GetUsersQuery) -> Result<Vec<UserView>, Box<dyn std::error::Error>> {
        let read_model = self.read_model.read().await;
        
        let users: Vec<UserView> = read_model.values()
            .skip(query.offset)
            .take(query.limit)
            .cloned()
            .collect();
        
        Ok(users)
    }
    
    async fn handle_search_users(&self, query: SearchUsersQuery) -> Result<Vec<UserView>, Box<dyn std::error::Error>> {
        let read_model = self.read_model.read().await;
        
        let users: Vec<UserView> = read_model.values()
            .filter(|user| {
                user.name.to_lowercase().contains(&query.search_term.to_lowercase()) ||
                user.email.to_lowercase().contains(&query.search_term.to_lowercase())
            })
            .cloned()
            .collect();
        
        Ok(users)
    }
    
    async fn update_read_model(&self, event: &Event) -> Result<(), Box<dyn std::error::Error>> {
        let mut read_model = self.read_model.write().await;
        
        match event.event_type.as_str() {
            "UserCreated" => {
                let data = event.data.as_object().unwrap();
                let user_view = UserView {
                    id: event.aggregate_id.clone(),
                    name: data["name"].as_str().unwrap().to_string(),
                    email: data["email"].as_str().unwrap().to_string(),
                    is_active: true,
                    created_at: event.timestamp,
                    updated_at: event.timestamp,
                };
                read_model.insert(event.aggregate_id.clone(), user_view);
            }
            "UserUpdated" => {
                if let Some(user_view) = read_model.get_mut(&event.aggregate_id) {
                    let data = event.data.as_object().unwrap();
                    user_view.name = data["name"].as_str().unwrap().to_string();
                    user_view.email = data["email"].as_str().unwrap().to_string();
                    user_view.updated_at = event.timestamp;
                }
            }
            "UserDeactivated" => {
                if let Some(user_view) = read_model.get_mut(&event.aggregate_id) {
                    user_view.is_active = false;
                    user_view.updated_at = event.timestamp;
                }
            }
            _ => {}
        }
        
        Ok(())
    }
}

#[tokio::main]
async fn cqrs_implementation() -> Result<(), Box<dyn std::error::Error>> {
    println!("🔄 CQRS Implementation");
    
    let event_store = Arc::new(EventStore::new());
    let message_queue = Arc::new(MessageQueue::new("amqp://localhost:5672").await?);
    let command_handler = CommandHandler::new(event_store.clone(), message_queue.clone());
    let query_handler = QueryHandler::new();
    
    // Handle commands
    let create_command = CreateUserCommand {
        name: "Charlie Brown".to_string(),
        email: "charlie@example.com".to_string(),
    };
    
    let user_id = command_handler.handle_create_user(create_command).await?;
    println!("✅ Created user: {}", user_id);
    
    let update_command = UpdateUserCommand {
        user_id: user_id.clone(),
        name: "Charlie Wilson".to_string(),
        email: "charlie.wilson@example.com".to_string(),
    };
    
    command_handler.handle_update_user(update_command).await?;
    println!("✅ Updated user: {}", user_id);
    
    // Handle queries
    let get_user_query = GetUserQuery {
        user_id: user_id.clone(),
    };
    
    if let Some(user_view) = query_handler.handle_get_user(get_user_query).await? {
        println!("📊 User view: {} ({})", user_view.name, user_view.email);
    }
    
    let search_query = SearchUsersQuery {
        search_term: "Charlie".to_string(),
    };
    
    let search_results = query_handler.handle_search_users(search_query).await?;
    println!("🔍 Found {} users matching 'Charlie'", search_results.len());
    
    Ok(())
}
```

## 🎯 What You've Learned

1. **Event sourcing foundation** - Event store implementation and aggregate pattern
2. **Message queue integration** - RabbitMQ integration for event publishing
3. **CQRS implementation** - Command and query responsibility separation
4. **Event-driven patterns** - Event handling and read model updates
5. **Aggregate design** - Domain-driven design with event sourcing
6. **Event store operations** - Appending, retrieving, and querying events

## 🚀 Next Steps

1. **Add event projections** - Implement read model projections
2. **Add event replay** - Implement event replay for rebuilding read models
3. **Add snapshots** - Implement snapshot storage for performance
4. **Add event versioning** - Implement event schema versioning
5. **Add event validation** - Implement event validation and business rules

---

**You now have complete event-driven architecture mastery with TuskLang Rust!** From event sourcing to CQRS, from message queues to event streaming - TuskLang gives you the tools to build reactive, scalable systems that respond to events in real-time. 