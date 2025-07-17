# Microservices Architecture in TuskLang with Rust

## 🏗️ Microservices Foundation

Microservices architecture with TuskLang and Rust provides scalable, maintainable, and resilient distributed systems. This guide covers service decomposition, inter-service communication, and advanced microservices patterns.

## 🏗️ Architecture Overview

### Microservices Stack

```rust
[microservices_architecture]
service_mesh: true
api_gateway: true
event_driven: true
distributed_tracing: true

[microservices_components]
tonic: "grpc_framework"
tokio: "async_runtime"
serde: "serialization"
tracing: "observability"
```

### Service Configuration

```rust
[microservices_configuration]
enable_service_mesh: true
enable_circuit_breaker: true
enable_distributed_tracing: true
enable_metrics: true

[microservices_implementation]
use tonic::{transport::Server, Request, Response, Status};
use tokio::sync::mpsc;
use serde::{Deserialize, Serialize};
use tracing::{info, error, instrument};
use std::sync::Arc;
use tokio::sync::RwLock;

// Service registry
pub struct ServiceRegistry {
    services: Arc<RwLock<HashMap<String, ServiceInfo>>>,
    health_checks: Arc<RwLock<HashMap<String, HealthCheck>>>,
}

#[derive(Debug, Clone)]
pub struct ServiceInfo {
    pub name: String,
    pub version: String,
    pub host: String,
    pub port: u16,
    pub health_endpoint: String,
    pub status: ServiceStatus,
    pub last_heartbeat: chrono::DateTime<chrono::Utc>,
}

#[derive(Debug, Clone)]
pub enum ServiceStatus {
    Healthy,
    Unhealthy,
    Unknown,
}

#[derive(Debug, Clone)]
pub struct HealthCheck {
    pub service_name: String,
    pub interval: Duration,
    pub timeout: Duration,
    pub retries: u32,
}

impl ServiceRegistry {
    pub fn new() -> Self {
        Self {
            services: Arc::new(RwLock::new(HashMap::new())),
            health_checks: Arc::new(RwLock::new(HashMap::new())),
        }
    }
    
    pub async fn register_service(&self, service: ServiceInfo) -> Result<(), RegistryError> {
        let mut services = self.services.write().await;
        services.insert(service.name.clone(), service);
        info!("Service registered: {}", service.name);
        Ok(())
    }
    
    pub async fn deregister_service(&self, service_name: &str) -> Result<(), RegistryError> {
        let mut services = self.services.write().await;
        services.remove(service_name);
        info!("Service deregistered: {}", service_name);
        Ok(())
    }
    
    pub async fn get_service(&self, service_name: &str) -> Option<ServiceInfo> {
        let services = self.services.read().await;
        services.get(service_name).cloned()
    }
    
    pub async fn list_services(&self) -> Vec<ServiceInfo> {
        let services = self.services.read().await;
        services.values().cloned().collect()
    }
    
    pub async fn update_service_status(&self, service_name: &str, status: ServiceStatus) -> Result<(), RegistryError> {
        let mut services = self.services.write().await;
        if let Some(service) = services.get_mut(service_name) {
            service.status = status;
            service.last_heartbeat = chrono::Utc::now();
        }
        Ok(())
    }
    
    pub async fn add_health_check(&self, health_check: HealthCheck) {
        let mut health_checks = self.health_checks.write().await;
        health_checks.insert(health_check.service_name.clone(), health_check);
    }
    
    pub async fn start_health_monitoring(&self) {
        let health_checks = self.health_checks.read().await;
        let services = self.services.clone();
        
        for health_check in health_checks.values() {
            let health_check = health_check.clone();
            let services = services.clone();
            
            tokio::spawn(async move {
                loop {
                    if let Some(service) = services.read().await.get(&health_check.service_name) {
                        let status = Self::perform_health_check(service).await;
                        let _ = Self::update_service_status(&services, &health_check.service_name, status).await;
                    }
                    
                    tokio::time::sleep(health_check.interval).await;
                }
            });
        }
    }
    
    async fn perform_health_check(service: &ServiceInfo) -> ServiceStatus {
        let client = reqwest::Client::new();
        let url = format!("http://{}:{}{}", service.host, service.port, service.health_endpoint);
        
        match client.get(&url).timeout(Duration::from_secs(5)).send().await {
            Ok(response) => {
                if response.status().is_success() {
                    ServiceStatus::Healthy
                } else {
                    ServiceStatus::Unhealthy
                }
            }
            Err(_) => ServiceStatus::Unhealthy,
        }
    }
    
    async fn update_service_status(
        services: &Arc<RwLock<HashMap<String, ServiceInfo>>>,
        service_name: &str,
        status: ServiceStatus,
    ) -> Result<(), RegistryError> {
        let mut services = services.write().await;
        if let Some(service) = services.get_mut(service_name) {
            service.status = status;
            service.last_heartbeat = chrono::Utc::now();
        }
        Ok(())
    }
}

#[derive(Debug, thiserror::Error)]
pub enum RegistryError {
    #[error("Service not found: {service_name}")]
    ServiceNotFound { service_name: String },
    #[error("Service already exists: {service_name}")]
    ServiceAlreadyExists { service_name: String },
    #[error("Health check failed: {message}")]
    HealthCheckFailed { message: String },
}
```

## 🔌 Service Communication

### gRPC Service Communication

```rust
[service_communication]
grpc: true
http: true
message_queue: true
event_streaming: true

[communication_implementation]
// User service
pub struct UserService {
    db: Arc<RwLock<HashMap<u64, User>>>,
    event_publisher: Arc<EventPublisher>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct User {
    pub id: u64,
    pub username: String,
    pub email: String,
    pub created_at: chrono::DateTime<chrono::Utc>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct CreateUserRequest {
    pub username: String,
    pub email: String,
    pub password: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct UserCreatedEvent {
    pub user_id: u64,
    pub username: String,
    pub email: String,
    pub timestamp: chrono::DateTime<chrono::Utc>,
}

impl UserService {
    pub fn new(event_publisher: Arc<EventPublisher>) -> Self {
        Self {
            db: Arc::new(RwLock::new(HashMap::new())),
            event_publisher,
        }
    }
    
    #[instrument(skip(self))]
    pub async fn create_user(&self, request: CreateUserRequest) -> Result<User, ServiceError> {
        info!("Creating user: {}", request.username);
        
        // Validate input
        if request.username.is_empty() || request.email.is_empty() {
            return Err(ServiceError::ValidationError { 
                message: "Username and email are required".to_string() 
            });
        }
        
        let mut db = self.db.write().await;
        let user_id = db.len() as u64 + 1;
        
        let user = User {
            id: user_id,
            username: request.username.clone(),
            email: request.email.clone(),
            created_at: chrono::Utc::now(),
        };
        
        db.insert(user.id, user.clone());
        
        // Publish event
        let event = UserCreatedEvent {
            user_id: user.id,
            username: user.username.clone(),
            email: user.email.clone(),
            timestamp: user.created_at,
        };
        
        self.event_publisher.publish("user.created", &event).await?;
        
        info!("User created successfully: {}", user.id);
        Ok(user)
    }
    
    #[instrument(skip(self))]
    pub async fn get_user(&self, id: u64) -> Result<Option<User>, ServiceError> {
        let db = self.db.read().await;
        Ok(db.get(&id).cloned())
    }
}

// Order service
pub struct OrderService {
    db: Arc<RwLock<HashMap<u64, Order>>>,
    user_service_client: Arc<UserServiceClient>,
    event_publisher: Arc<EventPublisher>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Order {
    pub id: u64,
    pub user_id: u64,
    pub items: Vec<OrderItem>,
    pub total_amount: f64,
    pub status: OrderStatus,
    pub created_at: chrono::DateTime<chrono::Utc>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct OrderItem {
    pub product_id: u64,
    pub quantity: u32,
    pub price: f64,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub enum OrderStatus {
    Pending,
    Confirmed,
    Shipped,
    Delivered,
    Cancelled,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct CreateOrderRequest {
    pub user_id: u64,
    pub items: Vec<OrderItem>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct OrderCreatedEvent {
    pub order_id: u64,
    pub user_id: u64,
    pub total_amount: f64,
    pub timestamp: chrono::DateTime<chrono::Utc>,
}

impl OrderService {
    pub fn new(
        user_service_client: Arc<UserServiceClient>,
        event_publisher: Arc<EventPublisher>,
    ) -> Self {
        Self {
            db: Arc::new(RwLock::new(HashMap::new())),
            user_service_client,
            event_publisher,
        }
    }
    
    #[instrument(skip(self))]
    pub async fn create_order(&self, request: CreateOrderRequest) -> Result<Order, ServiceError> {
        info!("Creating order for user: {}", request.user_id);
        
        // Verify user exists
        let user = self.user_service_client.get_user(request.user_id).await?;
        if user.is_none() {
            return Err(ServiceError::ValidationError { 
                message: "User not found".to_string() 
            });
        }
        
        // Calculate total
        let total_amount: f64 = request.items.iter().map(|item| item.price * item.quantity as f64).sum();
        
        let mut db = self.db.write().await;
        let order_id = db.len() as u64 + 1;
        
        let order = Order {
            id: order_id,
            user_id: request.user_id,
            items: request.items,
            total_amount,
            status: OrderStatus::Pending,
            created_at: chrono::Utc::now(),
        };
        
        db.insert(order.id, order.clone());
        
        // Publish event
        let event = OrderCreatedEvent {
            order_id: order.id,
            user_id: order.user_id,
            total_amount: order.total_amount,
            timestamp: order.created_at,
        };
        
        self.event_publisher.publish("order.created", &event).await?;
        
        info!("Order created successfully: {}", order.id);
        Ok(order)
    }
    
    #[instrument(skip(self))]
    pub async fn get_order(&self, id: u64) -> Result<Option<Order>, ServiceError> {
        let db = self.db.read().await;
        Ok(db.get(&id).cloned())
    }
    
    #[instrument(skip(self))]
    pub async fn update_order_status(&self, id: u64, status: OrderStatus) -> Result<Order, ServiceError> {
        let mut db = self.db.write().await;
        
        if let Some(order) = db.get_mut(&id) {
            order.status = status;
            Ok(order.clone())
        } else {
            Err(ServiceError::NotFound { message: "Order not found".to_string() })
        }
    }
}

// User service client
pub struct UserServiceClient {
    client: reqwest::Client,
    base_url: String,
}

impl UserServiceClient {
    pub fn new(base_url: String) -> Self {
        Self {
            client: reqwest::Client::new(),
            base_url,
        }
    }
    
    pub async fn get_user(&self, id: u64) -> Result<Option<User>, ServiceError> {
        let url = format!("{}/users/{}", self.base_url, id);
        
        match self.client.get(&url).send().await {
            Ok(response) => {
                if response.status().is_success() {
                    let user: User = response.json().await
                        .map_err(|e| ServiceError::CommunicationError { message: e.to_string() })?;
                    Ok(Some(user))
                } else if response.status() == reqwest::StatusCode::NOT_FOUND {
                    Ok(None)
                } else {
                    Err(ServiceError::CommunicationError { 
                        message: format!("HTTP {}: {}", response.status(), response.text().await.unwrap_or_default())
                    })
                }
            }
            Err(e) => Err(ServiceError::CommunicationError { message: e.to_string() }),
        }
    }
}
```

## 📡 Event-Driven Architecture

### Event Publishing and Consumption

```rust
[event_driven_architecture]
event_publishing: true
event_consumption: true
event_sourcing: true
cqrs: true

[event_implementation]
// Event publisher
pub struct EventPublisher {
    event_store: Arc<EventStore>,
    message_queue: Arc<MessageQueue>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Event {
    pub id: String,
    pub event_type: String,
    pub aggregate_id: String,
    pub data: serde_json::Value,
    pub timestamp: chrono::DateTime<chrono::Utc>,
    pub version: u64,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct EventMetadata {
    pub correlation_id: String,
    pub causation_id: String,
    pub user_id: Option<String>,
}

impl EventPublisher {
    pub fn new(event_store: Arc<EventStore>, message_queue: Arc<MessageQueue>) -> Self {
        Self {
            event_store,
            message_queue,
        }
    }
    
    pub async fn publish<T: Serialize>(&self, event_type: &str, data: &T) -> Result<(), EventError> {
        let event = Event {
            id: uuid::Uuid::new_v4().to_string(),
            event_type: event_type.to_string(),
            aggregate_id: "".to_string(), // Would be set based on context
            data: serde_json::to_value(data)
                .map_err(|e| EventError::SerializationError { message: e.to_string() })?,
            timestamp: chrono::Utc::now(),
            version: 1,
        };
        
        // Store event
        self.event_store.store_event(&event).await?;
        
        // Publish to message queue
        self.message_queue.publish(event_type, &event).await?;
        
        info!("Event published: {} - {}", event_type, event.id);
        Ok(())
    }
}

// Event store
pub struct EventStore {
    events: Arc<RwLock<Vec<Event>>>,
}

impl EventStore {
    pub fn new() -> Self {
        Self {
            events: Arc::new(RwLock::new(Vec::new())),
        }
    }
    
    pub async fn store_event(&self, event: &Event) -> Result<(), EventError> {
        let mut events = self.events.write().await;
        events.push(event.clone());
        Ok(())
    }
    
    pub async fn get_events(&self, aggregate_id: &str) -> Result<Vec<Event>, EventError> {
        let events = self.events.read().await;
        Ok(events.iter()
            .filter(|e| e.aggregate_id == aggregate_id)
            .cloned()
            .collect())
    }
    
    pub async fn get_events_by_type(&self, event_type: &str) -> Result<Vec<Event>, EventError> {
        let events = self.events.read().await;
        Ok(events.iter()
            .filter(|e| e.event_type == event_type)
            .cloned()
            .collect())
    }
}

// Message queue
pub struct MessageQueue {
    subscribers: Arc<RwLock<HashMap<String, Vec<Box<dyn EventHandler>>>>>,
}

impl MessageQueue {
    pub fn new() -> Self {
        Self {
            subscribers: Arc::new(RwLock::new(HashMap::new())),
        }
    }
    
    pub async fn subscribe(&self, event_type: &str, handler: Box<dyn EventHandler>) {
        let mut subscribers = self.subscribers.write().await;
        subscribers.entry(event_type.to_string())
            .or_insert_with(Vec::new)
            .push(handler);
    }
    
    pub async fn publish(&self, event_type: &str, event: &Event) -> Result<(), EventError> {
        let subscribers = self.subscribers.read().await;
        
        if let Some(handlers) = subscribers.get(event_type) {
            for handler in handlers {
                if let Err(e) = handler.handle(event).await {
                    error!("Event handler error: {}", e);
                }
            }
        }
        
        Ok(())
    }
}

// Event handler trait
#[async_trait::async_trait]
pub trait EventHandler: Send + Sync {
    async fn handle(&self, event: &Event) -> Result<(), EventError>;
}

// Notification service event handler
pub struct NotificationEventHandler {
    notification_service: Arc<NotificationService>,
}

#[async_trait::async_trait]
impl EventHandler for NotificationEventHandler {
    async fn handle(&self, event: &Event) -> Result<(), EventError> {
        match event.event_type.as_str() {
            "user.created" => {
                if let Ok(user_data) = serde_json::from_value::<UserCreatedEvent>(event.data.clone()) {
                    self.notification_service.send_welcome_email(&user_data).await?;
                }
            }
            "order.created" => {
                if let Ok(order_data) = serde_json::from_value::<OrderCreatedEvent>(event.data.clone()) {
                    self.notification_service.send_order_confirmation(&order_data).await?;
                }
            }
            _ => {}
        }
        Ok(())
    }
}

// Notification service
pub struct NotificationService {
    email_client: Arc<EmailClient>,
}

impl NotificationService {
    pub fn new(email_client: Arc<EmailClient>) -> Self {
        Self { email_client }
    }
    
    pub async fn send_welcome_email(&self, event: &UserCreatedEvent) -> Result<(), ServiceError> {
        let email = Email {
            to: event.email.clone(),
            subject: "Welcome to our platform!".to_string(),
            body: format!("Hello {}, welcome to our platform!", event.username),
        };
        
        self.email_client.send_email(&email).await?;
        info!("Welcome email sent to: {}", event.email);
        Ok(())
    }
    
    pub async fn send_order_confirmation(&self, event: &OrderCreatedEvent) -> Result<(), ServiceError> {
        // Implementation would send order confirmation email
        info!("Order confirmation sent for order: {}", event.order_id);
        Ok(())
    }
}

#[derive(Debug, Clone)]
pub struct Email {
    pub to: String,
    pub subject: String,
    pub body: String,
}

pub struct EmailClient;

impl EmailClient {
    pub async fn send_email(&self, email: &Email) -> Result<(), ServiceError> {
        // Implementation would send actual email
        info!("Email sent to {}: {}", email.to, email.subject);
        Ok(())
    }
}
```

## 🔄 Circuit Breaker Pattern

### Circuit Breaker Implementation

```rust
[circuit_breaker]
failure_threshold: true
recovery_timeout: true
half_open_state: true

[circuit_breaker_implementation]
use std::sync::atomic::{AtomicU32, Ordering};
use std::time::{Duration, Instant};

// Circuit breaker
pub struct CircuitBreaker {
    failure_threshold: u32,
    recovery_timeout: Duration,
    state: Arc<RwLock<CircuitState>>,
    failure_count: Arc<AtomicU32>,
    last_failure_time: Arc<RwLock<Option<Instant>>>,
}

#[derive(Debug, Clone)]
pub enum CircuitState {
    Closed,
    Open,
    HalfOpen,
}

impl CircuitBreaker {
    pub fn new(failure_threshold: u32, recovery_timeout: Duration) -> Self {
        Self {
            failure_threshold,
            recovery_timeout,
            state: Arc::new(RwLock::new(CircuitState::Closed)),
            failure_count: Arc::new(AtomicU32::new(0)),
            last_failure_time: Arc::new(RwLock::new(None)),
        }
    }
    
    pub async fn call<F, Fut, T, E>(&self, f: F) -> Result<T, CircuitBreakerError<E>>
    where
        F: FnOnce() -> Fut,
        Fut: Future<Output = Result<T, E>>,
        E: std::fmt::Debug,
    {
        let state = self.state.read().await;
        
        match *state {
            CircuitState::Open => {
                let last_failure = self.last_failure_time.read().await;
                if let Some(failure_time) = *last_failure {
                    if failure_time.elapsed() >= self.recovery_timeout {
                        // Transition to half-open
                        drop(state);
                        let mut state = self.state.write().await;
                        *state = CircuitState::HalfOpen;
                        drop(state);
                        
                        // Try the call
                        self.try_call(f).await
                    } else {
                        Err(CircuitBreakerError::CircuitOpen)
                    }
                } else {
                    Err(CircuitBreakerError::CircuitOpen)
                }
            }
            CircuitState::HalfOpen | CircuitState::Closed => {
                drop(state);
                self.try_call(f).await
            }
        }
    }
    
    async fn try_call<F, Fut, T, E>(&self, f: F) -> Result<T, CircuitBreakerError<E>>
    where
        F: FnOnce() -> Fut,
        Fut: Future<Output = Result<T, E>>,
        E: std::fmt::Debug,
    {
        match f().await {
            Ok(result) => {
                // Success - reset failure count and close circuit
                self.failure_count.store(0, Ordering::SeqCst);
                let mut state = self.state.write().await;
                *state = CircuitState::Closed;
                Ok(result)
            }
            Err(error) => {
                // Failure - increment count and potentially open circuit
                let failure_count = self.failure_count.fetch_add(1, Ordering::SeqCst) + 1;
                let mut last_failure_time = self.last_failure_time.write().await;
                *last_failure_time = Some(Instant::now());
                
                if failure_count >= self.failure_threshold {
                    let mut state = self.state.write().await;
                    *state = CircuitState::Open;
                    Err(CircuitBreakerError::CircuitOpen)
                } else {
                    Err(CircuitBreakerError::ServiceError(error))
                }
            }
        }
    }
    
    pub async fn get_state(&self) -> CircuitState {
        self.state.read().await.clone()
    }
}

#[derive(Debug, thiserror::Error)]
pub enum CircuitBreakerError<E> {
    #[error("Circuit breaker is open")]
    CircuitOpen,
    #[error("Service error: {0:?}")]
    ServiceError(E),
}

// Service with circuit breaker
pub struct ResilientUserServiceClient {
    client: UserServiceClient,
    circuit_breaker: CircuitBreaker,
}

impl ResilientUserServiceClient {
    pub fn new(client: UserServiceClient) -> Self {
        Self {
            circuit_breaker: CircuitBreaker::new(5, Duration::from_secs(30)),
            client,
        }
    }
    
    pub async fn get_user(&self, id: u64) -> Result<Option<User>, ServiceError> {
        self.circuit_breaker.call(|| self.client.get_user(id)).await
            .map_err(|e| match e {
                CircuitBreakerError::CircuitOpen => ServiceError::ServiceUnavailable { 
                    message: "Service temporarily unavailable".to_string() 
                },
                CircuitBreakerError::ServiceError(e) => e,
            })
    }
}
```

## 🎯 Best Practices

### 1. **Service Decomposition**
- Decompose by business capability
- Keep services focused and cohesive
- Use bounded contexts for domain modeling
- Implement proper service boundaries

### 2. **Inter-Service Communication**
- Use asynchronous communication when possible
- Implement circuit breakers for resilience
- Use event-driven patterns for loose coupling
- Implement proper error handling and retries

### 3. **Data Management**
- Use database per service pattern
- Implement eventual consistency
- Use event sourcing for audit trails
- Implement proper data versioning

### 4. **Observability**
- Implement distributed tracing
- Use structured logging
- Monitor service health and metrics
- Implement proper alerting

### 5. **TuskLang Integration**
- Use TuskLang configuration for service parameters
- Implement service discovery with TuskLang
- Configure circuit breakers through TuskLang
- Use TuskLang for event routing and filtering

Microservices architecture with TuskLang and Rust provides scalable, maintainable, and resilient distributed systems with comprehensive patterns for service communication and coordination. 