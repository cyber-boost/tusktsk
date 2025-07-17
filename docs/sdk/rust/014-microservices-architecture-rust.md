# 🦀 TuskLang Rust Microservices Architecture

**"We don't bow to any king" - Rust Edition**

Master microservices architecture with TuskLang Rust. From service discovery to inter-service communication, from API gateways to distributed tracing - build scalable, resilient microservices that communicate seamlessly.

## 🏗️ Microservices Foundation

### Service Architecture Design

```rust
use tusklang_rust::{parse, Parser};
use std::collections::HashMap;
use std::sync::Arc;
use tokio::sync::RwLock;

#[derive(Debug, Clone)]
struct Microservice {
    id: String,
    name: String,
    version: String,
    port: u16,
    health_endpoint: String,
    dependencies: Vec<String>,
    config: serde_json::Value,
}

struct ServiceRegistry {
    services: Arc<RwLock<HashMap<String, Microservice>>>,
    parser: Parser,
}

impl ServiceRegistry {
    fn new() -> Self {
        Self {
            services: Arc::new(RwLock::new(HashMap::new())),
            parser: Parser::new(),
        }
    }
    
    async fn register_service(&self, service: Microservice) -> Result<(), Box<dyn std::error::Error>> {
        let mut services = self.services.write().await;
        services.insert(service.id.clone(), service);
        
        // Update service discovery configuration
        self.update_service_discovery().await?;
        
        Ok(())
    }
    
    async fn discover_service(&self, service_name: &str) -> Option<Microservice> {
        let services = self.services.read().await;
        services.values()
            .find(|s| s.name == service_name)
            .cloned()
    }
    
    async fn get_all_services(&self) -> Vec<Microservice> {
        let services = self.services.read().await;
        services.values().cloned().collect()
    }
    
    async fn update_service_discovery(&self) -> Result<(), Box<dyn std::error::Error>> {
        let services = self.get_all_services().await;
        
        let discovery_config = format!(r#"
[service_discovery]
enabled: true
registry_url: "http://localhost:8500"
update_interval: "30s"

[services]
{}
"#, services.iter()
            .map(|s| format!("{}: \"{}:{}\"", s.name, s.id, s.port))
            .collect::<Vec<_>>()
            .join("\n"));
        
        self.parser.parse(&discovery_config).await?;
        Ok(())
    }
}

#[tokio::main]
async fn microservices_foundation() -> Result<(), Box<dyn std::error::Error>> {
    println!("🏗️  Microservices Foundation");
    
    let registry = ServiceRegistry::new();
    
    // Define microservices
    let services = vec![
        Microservice {
            id: "user-service-001".to_string(),
            name: "user-service".to_string(),
            version: "1.0.0".to_string(),
            port: 8081,
            health_endpoint: "/health".to_string(),
            dependencies: vec!["database".to_string()],
            config: serde_json::json!({
                "database": {
                    "host": "localhost",
                    "port": 5432,
                    "database": "users"
                }
            }),
        },
        Microservice {
            id: "order-service-001".to_string(),
            name: "order-service".to_string(),
            version: "1.0.0".to_string(),
            port: 8082,
            health_endpoint: "/health".to_string(),
            dependencies: vec!["user-service".to_string(), "payment-service".to_string()],
            config: serde_json::json!({
                "user_service_url": "http://user-service:8081",
                "payment_service_url": "http://payment-service:8083"
            }),
        },
        Microservice {
            id: "payment-service-001".to_string(),
            name: "payment-service".to_string(),
            version: "1.0.0".to_string(),
            port: 8083,
            health_endpoint: "/health".to_string(),
            dependencies: vec!["database".to_string()],
            config: serde_json::json!({
                "stripe_api_key": "@env('STRIPE_API_KEY')",
                "database": {
                    "host": "localhost",
                    "port": 5432,
                    "database": "payments"
                }
            }),
        },
    ];
    
    // Register services
    for service in services {
        registry.register_service(service).await?;
        println!("✅ Registered service: {}", service.name);
    }
    
    // Service discovery
    if let Some(user_service) = registry.discover_service("user-service").await {
        println!("🔍 Discovered user service: {}:{}", user_service.id, user_service.port);
    }
    
    Ok(())
}
```

### Service Configuration Management

```rust
use tusklang_rust::{parse, Parser};
use std::path::Path;

struct ServiceConfigManager {
    parser: Parser,
    config_dir: String,
}

impl ServiceConfigManager {
    fn new(config_dir: &str) -> Self {
        Self {
            parser: Parser::new(),
            config_dir: config_dir.to_string(),
        }
    }
    
    async fn load_service_config(&mut self, service_name: &str) -> Result<serde_json::Value, Box<dyn std::error::Error>> {
        let config_path = format!("{}/{}.tsk", self.config_dir, service_name);
        
        if Path::new(&config_path).exists() {
            self.parser.parse_file(&config_path).await
        } else {
            Err(format!("Configuration file not found: {}", config_path).into())
        }
    }
    
    async fn load_environment_config(&mut self, environment: &str) -> Result<serde_json::Value, Box<dyn std::error::Error>> {
        let config_path = format!("{}/environment/{}.tsk", self.config_dir, environment);
        self.parser.parse_file(&config_path).await
    }
    
    async fn merge_configurations(&self, base: serde_json::Value, override_config: serde_json::Value) -> serde_json::Value {
        // Deep merge configurations
        let mut merged = base.clone();
        
        if let (Some(base_obj), Some(override_obj)) = (merged.as_object_mut(), override_config.as_object()) {
            for (key, value) in override_obj {
                if let Some(base_value) = base_obj.get_mut(key) {
                    if value.is_object() && base_value.is_object() {
                        // Recursive merge for nested objects
                        let merged_nested = self.merge_configurations(base_value.clone(), value.clone()).await;
                        *base_value = merged_nested;
                    } else {
                        *base_value = value.clone();
                    }
                } else {
                    base_obj.insert(key.clone(), value.clone());
                }
            }
        }
        
        merged
    }
    
    async fn validate_service_config(&self, config: &serde_json::Value) -> Result<(), Box<dyn std::error::Error>> {
        // Validate required fields
        let required_fields = vec![
            ("server", "port"),
            ("database", "host"),
            ("logging", "level"),
        ];
        
        for (section, field) in required_fields {
            if let Some(section_config) = config.get(section) {
                if section_config.get(field).is_none() {
                    return Err(format!("Missing required field: {}.{}", section, field).into());
                }
            } else {
                return Err(format!("Missing required section: {}", section).into());
            }
        }
        
        Ok(())
    }
}

#[tokio::main]
async fn service_configuration_management() -> Result<(), Box<dyn std::error::Error>> {
    println!("⚙️  Service Configuration Management");
    
    let mut config_manager = ServiceConfigManager::new("config/services");
    
    // Load base configuration
    let base_config = config_manager.load_service_config("base").await?;
    println!("✅ Loaded base configuration");
    
    // Load environment-specific configuration
    let environment = std::env::var("APP_ENV").unwrap_or_else(|_| "development".to_string());
    let env_config = config_manager.load_environment_config(&environment).await?;
    println!("✅ Loaded {} environment configuration", environment);
    
    // Merge configurations
    let merged_config = config_manager.merge_configurations(base_config, env_config).await;
    println!("✅ Merged configurations");
    
    // Validate configuration
    config_manager.validate_service_config(&merged_config).await?;
    println!("✅ Configuration validation passed");
    
    // Service-specific configurations
    let services = vec!["user-service", "order-service", "payment-service"];
    
    for service in services {
        match config_manager.load_service_config(service).await {
            Ok(config) => {
                println!("✅ Loaded configuration for {}", service);
                
                // Apply service-specific overrides
                let final_config = config_manager.merge_configurations(merged_config.clone(), config).await;
                
                // Validate service configuration
                config_manager.validate_service_config(&final_config).await?;
                println!("✅ {} configuration validated", service);
            }
            Err(e) => {
                println!("⚠️  No specific configuration for {}: {}", service, e);
            }
        }
    }
    
    Ok(())
}
```

## 🔗 Inter-Service Communication

### gRPC Service Communication

```rust
use tusklang_rust::{parse, Parser};
use tonic::{transport::Server, Request, Response, Status};
use std::sync::Arc;
use tokio::sync::Mutex;

// Define protobuf service
#[derive(Debug)]
struct UserService {
    users: Arc<Mutex<HashMap<String, User>>>,
}

#[derive(Debug, Clone)]
struct User {
    id: String,
    name: String,
    email: String,
    created_at: chrono::DateTime<chrono::Utc>,
}

impl UserService {
    fn new() -> Self {
        Self {
            users: Arc::new(Mutex::new(HashMap::new())),
        }
    }
    
    async fn create_user(&self, name: String, email: String) -> Result<User, Box<dyn std::error::Error>> {
        let user = User {
            id: uuid::Uuid::new_v4().to_string(),
            name,
            email,
            created_at: chrono::Utc::now(),
        };
        
        let mut users = self.users.lock().await;
        users.insert(user.id.clone(), user.clone());
        
        Ok(user)
    }
    
    async fn get_user(&self, id: &str) -> Option<User> {
        let users = self.users.lock().await;
        users.get(id).cloned()
    }
    
    async fn list_users(&self) -> Vec<User> {
        let users = self.users.lock().await;
        users.values().cloned().collect()
    }
}

// gRPC service implementation
#[tonic::async_trait]
impl user_service_server::UserService for UserService {
    async fn create_user(
        &self,
        request: Request<CreateUserRequest>,
    ) -> Result<Response<CreateUserResponse>, Status> {
        let req = request.into_inner();
        
        match self.create_user(req.name, req.email).await {
            Ok(user) => {
                let response = CreateUserResponse {
                    user: Some(User {
                        id: user.id,
                        name: user.name,
                        email: user.email,
                        created_at: user.created_at.to_rfc3339(),
                    }),
                };
                Ok(Response::new(response))
            }
            Err(e) => Err(Status::internal(e.to_string())),
        }
    }
    
    async fn get_user(
        &self,
        request: Request<GetUserRequest>,
    ) -> Result<Response<GetUserResponse>, Status> {
        let req = request.into_inner();
        
        match self.get_user(&req.id).await {
            Some(user) => {
                let response = GetUserResponse {
                    user: Some(User {
                        id: user.id,
                        name: user.name,
                        email: user.email,
                        created_at: user.created_at.to_rfc3339(),
                    }),
                };
                Ok(Response::new(response))
            }
            None => Err(Status::not_found("User not found")),
        }
    }
    
    async fn list_users(
        &self,
        _request: Request<ListUsersRequest>,
    ) -> Result<Response<ListUsersResponse>, Status> {
        let users = self.list_users().await;
        
        let response = ListUsersResponse {
            users: users.into_iter().map(|user| User {
                id: user.id,
                name: user.name,
                email: user.email,
                created_at: user.created_at.to_rfc3339(),
            }).collect(),
        };
        
        Ok(Response::new(response))
    }
}

#[tokio::main]
async fn grpc_service_communication() -> Result<(), Box<dyn std::error::Error>> {
    println!("🔗 gRPC Service Communication");
    
    let mut parser = Parser::new();
    
    // Load gRPC configuration
    let grpc_config = parser.parse(r#"
[grpc]
enabled: true
port: 9090
max_concurrent_streams: 100
max_connection_idle: "30s"
max_connection_age: "5m"
max_connection_age_grace: "10s"
timeout: "30s"
"#).await?;
    
    let user_service = UserService::new();
    let addr = format!("0.0.0.0:{}", grpc_config["grpc"]["port"].as_u64().unwrap_or(9090))
        .parse()?;
    
    println!("🚀 Starting gRPC server on {}", addr);
    
    Server::builder()
        .add_service(user_service_server::UserServiceServer::new(user_service))
        .serve(addr)
        .await?;
    
    Ok(())
}

// Protobuf message definitions (simplified)
#[derive(Debug)]
struct CreateUserRequest {
    name: String,
    email: String,
}

#[derive(Debug)]
struct CreateUserResponse {
    user: Option<User>,
}

#[derive(Debug)]
struct GetUserRequest {
    id: String,
}

#[derive(Debug)]
struct GetUserResponse {
    user: Option<User>,
}

#[derive(Debug)]
struct ListUsersRequest {}

#[derive(Debug)]
struct ListUsersResponse {
    users: Vec<User>,
}

#[derive(Debug)]
struct User {
    id: String,
    name: String,
    email: String,
    created_at: String,
}

// Mock tonic service trait
mod user_service_server {
    use super::*;
    
    #[tonic::async_trait]
    pub trait UserService: Send + Sync + 'static {
        async fn create_user(
            &self,
            request: Request<CreateUserRequest>,
        ) -> Result<Response<CreateUserResponse>, Status>;
        
        async fn get_user(
            &self,
            request: Request<GetUserRequest>,
        ) -> Result<Response<GetUserResponse>, Status>;
        
        async fn list_users(
            &self,
            request: Request<ListUsersRequest>,
        ) -> Result<Response<ListUsersResponse>, Status>;
    }
}
```

### HTTP REST API Communication

```rust
use tusklang_rust::{parse, Parser};
use reqwest::Client;
use serde::{Deserialize, Serialize};
use std::time::Duration;

#[derive(Debug, Serialize, Deserialize)]
struct ApiResponse<T> {
    success: bool,
    data: Option<T>,
    error: Option<String>,
}

#[derive(Debug, Serialize, Deserialize)]
struct User {
    id: String,
    name: String,
    email: String,
    created_at: String,
}

#[derive(Debug, Serialize, Deserialize)]
struct Order {
    id: String,
    user_id: String,
    items: Vec<OrderItem>,
    total: f64,
    status: String,
    created_at: String,
}

#[derive(Debug, Serialize, Deserialize)]
struct OrderItem {
    product_id: String,
    quantity: i32,
    price: f64,
}

struct ServiceClient {
    client: Client,
    base_urls: HashMap<String, String>,
    parser: Parser,
}

impl ServiceClient {
    fn new() -> Self {
        let client = Client::builder()
            .timeout(Duration::from_secs(30))
            .build()
            .expect("Failed to create HTTP client");
        
        Self {
            client,
            base_urls: HashMap::new(),
            parser: Parser::new(),
        }
    }
    
    async fn register_service(&mut self, name: &str, url: &str) {
        self.base_urls.insert(name.to_string(), url.to_string());
    }
    
    async fn call_service<T>(&self, service_name: &str, endpoint: &str) -> Result<T, Box<dyn std::error::Error>>
    where
        T: for<'de> Deserialize<'de>,
    {
        let base_url = self.base_urls.get(service_name)
            .ok_or_else(|| format!("Service not found: {}", service_name))?;
        
        let url = format!("{}{}", base_url, endpoint);
        
        let response = self.client.get(&url)
            .header("Content-Type", "application/json")
            .send()
            .await?;
        
        if response.status().is_success() {
            let api_response: ApiResponse<T> = response.json().await?;
            
            if api_response.success {
                api_response.data.ok_or_else(|| "No data in response".into())
            } else {
                Err(api_response.error.unwrap_or_else(|| "Unknown error".to_string()).into())
            }
        } else {
            Err(format!("HTTP error: {}", response.status()).into())
        }
    }
    
    async fn create_order(&self, user_id: &str, items: Vec<OrderItem>) -> Result<Order, Box<dyn std::error::Error>> {
        let order_request = serde_json::json!({
            "user_id": user_id,
            "items": items
        });
        
        let base_url = self.base_urls.get("order-service")
            .ok_or_else(|| "Order service not found".to_string())?;
        
        let response = self.client.post(&format!("{}/orders", base_url))
            .header("Content-Type", "application/json")
            .json(&order_request)
            .send()
            .await?;
        
        if response.status().is_success() {
            let api_response: ApiResponse<Order> = response.json().await?;
            
            if api_response.success {
                api_response.data.ok_or_else(|| "No data in response".into())
            } else {
                Err(api_response.error.unwrap_or_else(|| "Unknown error".to_string()).into())
            }
        } else {
            Err(format!("HTTP error: {}", response.status()).into())
        }
    }
}

#[tokio::main]
async fn http_rest_api_communication() -> Result<(), Box<dyn std::error::Error>> {
    println!("🌐 HTTP REST API Communication");
    
    let mut service_client = ServiceClient::new();
    
    // Register services
    service_client.register_service("user-service", "http://localhost:8081").await;
    service_client.register_service("order-service", "http://localhost:8082").await;
    service_client.register_service("payment-service", "http://localhost:8083").await;
    
    // Load API configuration
    let api_config = service_client.parser.parse(r#"
[api_communication]
timeout: "30s"
retry_attempts: 3
retry_delay: "1s"
circuit_breaker_enabled: true
circuit_breaker_threshold: 5
circuit_breaker_timeout: "60s"
"#).await?;
    
    // Example: Get user information
    match service_client.call_service::<User>("user-service", "/users/123").await {
        Ok(user) => {
            println!("✅ Retrieved user: {} ({})", user.name, user.email);
            
            // Create order for user
            let order_items = vec![
                OrderItem {
                    product_id: "prod_123".to_string(),
                    quantity: 2,
                    price: 29.99,
                },
                OrderItem {
                    product_id: "prod_456".to_string(),
                    quantity: 1,
                    price: 49.99,
                },
            ];
            
            match service_client.create_order(&user.id, order_items).await {
                Ok(order) => {
                    println!("✅ Created order: {} (Total: ${:.2})", order.id, order.total);
                }
                Err(e) => {
                    println!("❌ Failed to create order: {}", e);
                }
            }
        }
        Err(e) => {
            println!("❌ Failed to get user: {}", e);
        }
    }
    
    Ok(())
}
```

## 🚪 API Gateway Implementation

### Gateway Configuration

```rust
use tusklang_rust::{parse, Parser};
use actix_web::{web, App, HttpServer, HttpResponse, Result};
use std::collections::HashMap;
use std::sync::Arc;
use tokio::sync::RwLock;

#[derive(Debug, Clone)]
struct Route {
    path: String,
    service: String,
    method: String,
    timeout: Duration,
    retries: u32,
}

#[derive(Debug, Clone)]
struct ServiceEndpoint {
    name: String,
    url: String,
    health_check: String,
    circuit_breaker: CircuitBreaker,
}

struct CircuitBreaker {
    failure_count: u32,
    threshold: u32,
    timeout: Duration,
    last_failure: Option<Instant>,
    state: CircuitState,
}

#[derive(Debug, Clone)]
enum CircuitState {
    Closed,
    Open,
    HalfOpen,
}

impl CircuitBreaker {
    fn new(threshold: u32, timeout: Duration) -> Self {
        Self {
            failure_count: 0,
            threshold,
            timeout,
            last_failure: None,
            state: CircuitState::Closed,
        }
    }
    
    fn can_execute(&mut self) -> bool {
        match self.state {
            CircuitState::Open => {
                if let Some(last_failure) = self.last_failure {
                    if Instant::now().duration_since(last_failure) >= self.timeout {
                        self.state = CircuitState::HalfOpen;
                        true
                    } else {
                        false
                    }
                } else {
                    false
                }
            }
            CircuitState::HalfOpen | CircuitState::Closed => true,
        }
    }
    
    fn on_success(&mut self) {
        self.failure_count = 0;
        self.state = CircuitState::Closed;
    }
    
    fn on_failure(&mut self) {
        self.failure_count += 1;
        self.last_failure = Some(Instant::now());
        
        if self.failure_count >= self.threshold {
            self.state = CircuitState::Open;
        }
    }
}

struct ApiGateway {
    routes: Arc<RwLock<HashMap<String, Route>>>,
    services: Arc<RwLock<HashMap<String, ServiceEndpoint>>>,
    parser: Parser,
}

impl ApiGateway {
    fn new() -> Self {
        Self {
            routes: Arc::new(RwLock::new(HashMap::new())),
            services: Arc::new(RwLock::new(HashMap::new())),
            parser: Parser::new(),
        }
    }
    
    async fn add_route(&self, route: Route) {
        let mut routes = self.routes.write().await;
        routes.insert(route.path.clone(), route);
    }
    
    async fn add_service(&self, service: ServiceEndpoint) {
        let mut services = self.services.write().await;
        services.insert(service.name.clone(), service);
    }
    
    async fn route_request(&self, path: &str, method: &str, body: String) -> Result<HttpResponse, Box<dyn std::error::Error>> {
        let routes = self.routes.read().await;
        let services = self.services.read().await;
        
        // Find matching route
        let route = routes.get(path)
            .ok_or_else(|| format!("Route not found: {}", path))?;
        
        if route.method != method {
            return Err(format!("Method not allowed: {} for {}", method, path).into());
        }
        
        // Get service endpoint
        let service = services.get(&route.service)
            .ok_or_else(|| format!("Service not found: {}", route.service))?;
        
        // Check circuit breaker
        let mut circuit_breaker = service.circuit_breaker.clone();
        if !circuit_breaker.can_execute() {
            return Err("Service circuit breaker is open".into());
        }
        
        // Forward request to service
        let client = reqwest::Client::new();
        let url = format!("{}{}", service.url, path);
        
        let response = client.request(
            reqwest::Method::from_bytes(method.as_bytes())?,
            &url
        )
        .body(body)
        .timeout(route.timeout)
        .send()
        .await;
        
        match response {
            Ok(resp) => {
                circuit_breaker.on_success();
                
                let status = resp.status();
                let body = resp.text().await?;
                
                Ok(HttpResponse::build(status)
                    .body(body))
            }
            Err(e) => {
                circuit_breaker.on_failure();
                Err(e.into())
            }
        }
    }
}

#[actix_web::main]
async fn api_gateway_implementation() -> std::io::Result<()> {
    println!("🚪 API Gateway Implementation");
    
    let gateway = Arc::new(ApiGateway::new());
    
    // Load gateway configuration
    let gateway_config = gateway.parser.parse(r#"
[gateway]
port: 8080
timeout: "30s"
max_connections: 1000
rate_limit_enabled: true
rate_limit_requests: 100
rate_limit_window: "1m"

[routes]
"/api/users/*": {
    service: "user-service",
    method: "GET",
    timeout: "10s",
    retries: 3
}
"/api/orders/*": {
    service: "order-service",
    method: "POST",
    timeout: "30s",
    retries: 2
}
"/api/payments/*": {
    service: "payment-service",
    method: "POST",
    timeout: "15s",
    retries: 3
}
"#).await.expect("Failed to parse gateway config");
    
    // Add routes
    let routes = vec![
        Route {
            path: "/api/users/*".to_string(),
            service: "user-service".to_string(),
            method: "GET".to_string(),
            timeout: Duration::from_secs(10),
            retries: 3,
        },
        Route {
            path: "/api/orders/*".to_string(),
            service: "order-service".to_string(),
            method: "POST".to_string(),
            timeout: Duration::from_secs(30),
            retries: 2,
        },
        Route {
            path: "/api/payments/*".to_string(),
            service: "payment-service".to_string(),
            method: "POST".to_string(),
            timeout: Duration::from_secs(15),
            retries: 3,
        },
    ];
    
    for route in routes {
        gateway.add_route(route).await;
    }
    
    // Add services
    let services = vec![
        ServiceEndpoint {
            name: "user-service".to_string(),
            url: "http://localhost:8081".to_string(),
            health_check: "/health".to_string(),
            circuit_breaker: CircuitBreaker::new(5, Duration::from_secs(60)),
        },
        ServiceEndpoint {
            name: "order-service".to_string(),
            url: "http://localhost:8082".to_string(),
            health_check: "/health".to_string(),
            circuit_breaker: CircuitBreaker::new(5, Duration::from_secs(60)),
        },
        ServiceEndpoint {
            name: "payment-service".to_string(),
            url: "http://localhost:8083".to_string(),
            health_check: "/health".to_string(),
            circuit_breaker: CircuitBreaker::new(5, Duration::from_secs(60)),
        },
    ];
    
    for service in services {
        gateway.add_service(service).await;
    }
    
    let gateway_clone = Arc::clone(&gateway);
    
    // Start HTTP server
    let port = gateway_config["gateway"]["port"].as_u64().unwrap_or(8080);
    
    HttpServer::new(move || {
        App::new()
            .app_data(web::Data::new(gateway_clone.clone()))
            .service(web::resource("/{tail:.*}").to(|path: web::Path<String>, req: HttpRequest, body: web::Bytes, gateway: web::Data<Arc<ApiGateway>>| async move {
                let method = req.method().to_string();
                let body_str = String::from_utf8_lossy(&body).to_string();
                
                match gateway.route_request(&path, &method, body_str).await {
                    Ok(response) => response,
                    Err(e) => {
                        HttpResponse::InternalServerError()
                            .json(json!({
                                "error": e.to_string(),
                                "timestamp": chrono::Utc::now().to_rfc3339()
                            }))
                    }
                }
            }))
    })
    .bind(format!("0.0.0.0:{}", port))?
    .run()
    .await
}
```

## 🔍 Distributed Tracing

### Tracing Implementation

```rust
use tusklang_rust::{parse, Parser};
use tracing::{info, warn, error, debug, span, Level};
use tracing_subscriber::{layer::SubscriberExt, util::SubscriberInitExt};
use std::collections::HashMap;
use std::sync::Arc;
use tokio::sync::RwLock;

#[derive(Debug, Clone)]
struct TraceContext {
    trace_id: String,
    span_id: String,
    parent_span_id: Option<String>,
    service_name: String,
    operation_name: String,
    start_time: chrono::DateTime<chrono::Utc>,
    tags: HashMap<String, String>,
}

struct TracingService {
    traces: Arc<RwLock<HashMap<String, Vec<TraceContext>>>>,
    parser: Parser,
}

impl TracingService {
    fn new() -> Self {
        Self {
            traces: Arc::new(RwLock::new(HashMap::new())),
            parser: Parser::new(),
        }
    }
    
    fn generate_trace_id() -> String {
        uuid::Uuid::new_v4().to_string()
    }
    
    fn generate_span_id() -> String {
        uuid::Uuid::new_v4().to_string()
    }
    
    async fn start_trace(&self, service_name: &str, operation_name: &str) -> TraceContext {
        let trace_context = TraceContext {
            trace_id: Self::generate_trace_id(),
            span_id: Self::generate_span_id(),
            parent_span_id: None,
            service_name: service_name.to_string(),
            operation_name: operation_name.to_string(),
            start_time: chrono::Utc::now(),
            tags: HashMap::new(),
        };
        
        let mut traces = self.traces.write().await;
        traces.entry(trace_context.trace_id.clone())
            .or_insert_with(Vec::new)
            .push(trace_context.clone());
        
        trace_context
    }
    
    async fn start_span(&self, trace_id: &str, service_name: &str, operation_name: &str) -> TraceContext {
        let trace_context = TraceContext {
            trace_id: trace_id.to_string(),
            span_id: Self::generate_span_id(),
            parent_span_id: Some(Self::generate_span_id()),
            service_name: service_name.to_string(),
            operation_name: operation_name.to_string(),
            start_time: chrono::Utc::now(),
            tags: HashMap::new(),
        };
        
        let mut traces = self.traces.write().await;
        traces.entry(trace_id.to_string())
            .or_insert_with(Vec::new)
            .push(trace_context.clone());
        
        trace_context
    }
    
    async fn add_tag(&self, trace_id: &str, span_id: &str, key: &str, value: &str) {
        let mut traces = self.traces.write().await;
        
        if let Some(trace_spans) = traces.get_mut(trace_id) {
            for span in trace_spans {
                if span.span_id == span_id {
                    span.tags.insert(key.to_string(), value.to_string());
                    break;
                }
            }
        }
    }
    
    async fn end_span(&self, trace_id: &str, span_id: &str) {
        let mut traces = self.traces.write().await;
        
        if let Some(trace_spans) = traces.get_mut(trace_id) {
            trace_spans.retain(|span| span.span_id != span_id);
        }
    }
    
    async fn get_trace(&self, trace_id: &str) -> Option<Vec<TraceContext>> {
        let traces = self.traces.read().await;
        traces.get(trace_id).cloned()
    }
    
    async fn export_traces(&self) -> Vec<serde_json::Value> {
        let traces = self.traces.read().await;
        
        traces.iter().map(|(trace_id, spans)| {
            serde_json::json!({
                "trace_id": trace_id,
                "spans": spans.iter().map(|span| {
                    serde_json::json!({
                        "span_id": span.span_id,
                        "parent_span_id": span.parent_span_id,
                        "service_name": span.service_name,
                        "operation_name": span.operation_name,
                        "start_time": span.start_time.to_rfc3339(),
                        "tags": span.tags
                    })
                }).collect::<Vec<_>>()
            })
        }).collect()
    }
}

#[tokio::main]
async fn distributed_tracing_implementation() -> Result<(), Box<dyn std::error::Error>> {
    println!("🔍 Distributed Tracing Implementation");
    
    // Initialize tracing
    tracing_subscriber::registry()
        .with(tracing_subscriber::EnvFilter::new(
            std::env::var("RUST_LOG").unwrap_or_else(|_| "info".into()),
        ))
        .with(tracing_subscriber::fmt::layer())
        .init();
    
    let tracing_service = Arc::new(TracingService::new());
    
    // Load tracing configuration
    let tracing_config = tracing_service.parser.parse(r#"
[tracing]
enabled: true
sampling_rate: 1.0
max_trace_duration: "5m"
export_traces: true
jaeger_endpoint: "http://localhost:14268/api/traces"
zipkin_endpoint: "http://localhost:9411/api/v2/spans"
"#).await?;
    
    // Simulate distributed request flow
    let trace_id = tracing_service.start_trace("api-gateway", "handle_request").await.trace_id;
    
    // User service call
    let user_span = tracing_service.start_span(&trace_id, "user-service", "get_user").await;
    tracing_service.add_tag(&trace_id, &user_span.span_id, "user_id", "123").await;
    
    info!(trace_id = %trace_id, span_id = %user_span.span_id, "Calling user service");
    
    // Simulate service call
    tokio::time::sleep(Duration::from_millis(100)).await;
    
    tracing_service.end_span(&trace_id, &user_span.span_id).await;
    
    // Order service call
    let order_span = tracing_service.start_span(&trace_id, "order-service", "create_order").await;
    tracing_service.add_tag(&trace_id, &order_span.span_id, "order_items", "2").await;
    
    info!(trace_id = %trace_id, span_id = %order_span.span_id, "Calling order service");
    
    // Simulate service call
    tokio::time::sleep(Duration::from_millis(200)).await;
    
    tracing_service.end_span(&trace_id, &order_span.span_id).await;
    
    // Payment service call
    let payment_span = tracing_service.start_span(&trace_id, "payment-service", "process_payment").await;
    tracing_service.add_tag(&trace_id, &payment_span.span_id, "amount", "109.97").await;
    
    info!(trace_id = %trace_id, span_id = %payment_span.span_id, "Calling payment service");
    
    // Simulate service call
    tokio::time::sleep(Duration::from_millis(150)).await;
    
    tracing_service.end_span(&trace_id, &payment_span.span_id).await;
    
    // Export traces
    let traces = tracing_service.export_traces().await;
    println!("📊 Exported {} traces", traces.len());
    
    for trace in traces {
        println!("Trace: {}", serde_json::to_string_pretty(&trace)?);
    }
    
    Ok(())
}
```

## 🎯 What You've Learned

1. **Microservices foundation** - Service architecture design and configuration management
2. **Inter-service communication** - gRPC and HTTP REST API communication patterns
3. **API gateway implementation** - Request routing, circuit breakers, and load balancing
4. **Distributed tracing** - Trace context propagation and observability
5. **Service discovery** - Dynamic service registration and discovery
6. **Configuration management** - Environment-specific configuration and validation

## 🚀 Next Steps

1. **Implement service mesh** - Add Istio or Linkerd for advanced traffic management
2. **Add monitoring** - Implement Prometheus metrics and Grafana dashboards
3. **Set up CI/CD** - Create deployment pipelines for microservices
4. **Add security** - Implement mTLS and service-to-service authentication
5. **Performance optimization** - Add caching layers and connection pooling

---

**You now have complete microservices architecture mastery with TuskLang Rust!** From service discovery to distributed tracing, from API gateways to inter-service communication - TuskLang gives you the tools to build scalable, resilient microservices that communicate seamlessly. 