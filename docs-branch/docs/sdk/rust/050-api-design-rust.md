# API Design in TuskLang with Rust

## 🌐 API Foundation

API design with TuskLang and Rust provides powerful tools for building robust and scalable web services. This guide covers REST APIs, GraphQL, gRPC, and advanced API patterns.

## 🏗️ API Architecture

### API Stack

```rust
[api_architecture]
rest_api: true
graphql: true
grpc: true
websockets: true

[api_components]
axum: "web_framework"
tonic: "grpc_framework"
juniper: "graphql_framework"
serde: "serialization"
```

### API Configuration

```rust
[api_configuration]
enable_rest: true
enable_graphql: true
enable_grpc: true
enable_documentation: true

[api_implementation]
use axum::{
    routing::{get, post, put, delete},
    http::StatusCode,
    Json, Router,
    extract::{Path, Query, State},
};
use serde::{Deserialize, Serialize};
use std::sync::Arc;
use tokio::sync::RwLock;

// API manager
pub struct APIManager {
    config: APIConfig,
    routes: Arc<RwLock<Vec<APIRoute>>>,
    middleware: Arc<RwLock<Vec<Box<dyn Middleware>>>>,
    rate_limiters: Arc<RwLock<HashMap<String, RateLimiter>>>,
}

#[derive(Debug, Clone)]
pub struct APIConfig {
    pub host: String,
    pub port: u16,
    pub cors_enabled: bool,
    pub rate_limiting: RateLimitConfig,
    pub authentication: AuthConfig,
    pub documentation: DocumentationConfig,
}

#[derive(Debug, Clone)]
pub struct RateLimitConfig {
    pub enabled: bool,
    pub requests_per_minute: u32,
    pub burst_size: u32,
}

#[derive(Debug, Clone)]
pub struct AuthConfig {
    pub enabled: bool,
    pub jwt_secret: String,
    pub token_expiration: Duration,
}

#[derive(Debug, Clone)]
pub struct DocumentationConfig {
    pub enabled: bool,
    pub swagger_path: String,
    pub openapi_version: String,
}

#[derive(Debug, Clone)]
pub struct APIRoute {
    pub path: String,
    pub method: HTTPMethod,
    pub handler: String,
    pub middleware: Vec<String>,
    pub rate_limit: Option<u32>,
    pub auth_required: bool,
}

#[derive(Debug, Clone)]
pub enum HTTPMethod {
    GET,
    POST,
    PUT,
    DELETE,
    PATCH,
}

#[derive(Debug, Clone)]
pub struct RateLimiter {
    pub requests: Vec<Instant>,
    pub limit: u32,
    pub window: Duration,
}

// Middleware trait
pub trait Middleware: Send + Sync {
    fn process(&self, request: &mut Request) -> Result<(), APIError>;
}

// Request and Response types
#[derive(Debug, Clone)]
pub struct Request {
    pub method: HTTPMethod,
    pub path: String,
    pub headers: HashMap<String, String>,
    pub body: Option<Vec<u8>>,
    pub query_params: HashMap<String, String>,
}

#[derive(Debug, Clone)]
pub struct Response {
    pub status_code: u16,
    pub headers: HashMap<String, String>,
    pub body: Option<Vec<u8>>,
}

impl APIManager {
    pub fn new(config: APIConfig) -> Self {
        Self {
            config,
            routes: Arc::new(RwLock::new(Vec::new())),
            middleware: Arc::new(RwLock::new(Vec::new())),
            rate_limiters: Arc::new(RwLock::new(HashMap::new())),
        }
    }
    
    pub async fn add_route(&self, route: APIRoute) {
        let mut routes = self.routes.write().await;
        routes.push(route);
    }
    
    pub async fn add_middleware(&self, middleware: Box<dyn Middleware>) {
        let mut middleware_list = self.middleware.write().await;
        middleware_list.push(middleware);
    }
    
    pub async fn start_server(&self) -> Result<(), APIError> {
        let app = self.build_router().await?;
        
        let addr = format!("{}:{}", self.config.host, self.config.port)
            .parse()
            .map_err(|e| APIError::ConfigError { message: e.to_string() })?;
        
        println!("API server starting on {}", addr);
        
        axum::Server::bind(&addr)
            .serve(app.into_make_service())
            .await
            .map_err(|e| APIError::ServerError { message: e.to_string() })?;
        
        Ok(())
    }
    
    async fn build_router(&self) -> Result<Router, APIError> {
        let mut router = Router::new();
        
        // Add routes
        let routes = self.routes.read().await;
        for route in routes.iter() {
            let handler = self.create_handler(route).await?;
            
            match route.method {
                HTTPMethod::GET => router = router.route(&route.path, get(handler)),
                HTTPMethod::POST => router = router.route(&route.path, post(handler)),
                HTTPMethod::PUT => router = router.route(&route.path, put(handler)),
                HTTPMethod::DELETE => router = router.route(&route.path, delete(handler)),
                HTTPMethod::PATCH => router = router.route(&route.path, get(handler)), // Simplified
            }
        }
        
        // Add middleware
        let middleware_list = self.middleware.read().await;
        for middleware in middleware_list.iter() {
            // Apply middleware to router
            // This is a simplified implementation
        }
        
        // Add CORS if enabled
        if self.config.cors_enabled {
            router = router.layer(tower_http::cors::CorsLayer::permissive());
        }
        
        Ok(router)
    }
    
    async fn create_handler(&self, route: &APIRoute) -> Result<axum::handler::HandlerFn, APIError> {
        // Create handler function based on route configuration
        // This is a simplified implementation
        Ok(|_| async { "Handler" })
    }
    
    pub async fn check_rate_limit(&self, client_id: &str) -> Result<bool, APIError> {
        if !self.config.rate_limiting.enabled {
            return Ok(true);
        }
        
        let mut limiters = self.rate_limiters.write().await;
        let limiter = limiters.entry(client_id.to_string()).or_insert_with(|| RateLimiter {
            requests: Vec::new(),
            limit: self.config.rate_limiting.requests_per_minute,
            window: Duration::from_secs(60),
        });
        
        let now = Instant::now();
        
        // Remove expired requests
        limiter.requests.retain(|&time| now.duration_since(time) < limiter.window);
        
        // Check if limit exceeded
        if limiter.requests.len() >= limiter.limit as usize {
            return Err(APIError::RateLimitExceeded);
        }
        
        // Add current request
        limiter.requests.push(now);
        
        Ok(true)
    }
}

#[derive(Debug, thiserror::Error)]
pub enum APIError {
    #[error("Configuration error: {message}")]
    ConfigError { message: String },
    #[error("Server error: {message}")]
    ServerError { message: String },
    #[error("Rate limit exceeded")]
    RateLimitExceeded,
    #[error("Authentication failed")]
    AuthenticationFailed,
    #[error("Authorization failed")]
    AuthorizationFailed,
    #[error("Validation error: {message}")]
    ValidationError { message: String },
    #[error("Database error: {message}")]
    DatabaseError { message: String },
}
```

## 🌍 REST API Design

### RESTful API Implementation

```rust
[rest_api_design]
resource_based: true
stateless: true
cacheable: true
layered: true

[rest_implementation]
use axum::{
    routing::{get, post, put, delete},
    http::StatusCode,
    Json, Router,
    extract::{Path, Query, State},
};
use serde::{Deserialize, Serialize};

// User resource
#[derive(Debug, Serialize, Deserialize, Clone)]
pub struct User {
    pub id: u64,
    pub username: String,
    pub email: String,
    pub created_at: chrono::DateTime<chrono::Utc>,
    pub updated_at: chrono::DateTime<chrono::Utc>,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct CreateUserRequest {
    pub username: String,
    pub email: String,
    pub password: String,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct UpdateUserRequest {
    pub username: Option<String>,
    pub email: Option<String>,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct UserListResponse {
    pub users: Vec<User>,
    pub total: u64,
    pub page: u32,
    pub per_page: u32,
}

// User service
pub struct UserService {
    db: Arc<RwLock<HashMap<u64, User>>>,
    next_id: Arc<RwLock<u64>>,
}

impl UserService {
    pub fn new() -> Self {
        Self {
            db: Arc::new(RwLock::new(HashMap::new())),
            next_id: Arc::new(RwLock::new(1)),
        }
    }
    
    pub async fn create_user(&self, request: CreateUserRequest) -> Result<User, APIError> {
        // Validate input
        if request.username.is_empty() || request.email.is_empty() {
            return Err(APIError::ValidationError { 
                message: "Username and email are required".to_string() 
            });
        }
        
        if !request.email.contains('@') {
            return Err(APIError::ValidationError { 
                message: "Invalid email format".to_string() 
            });
        }
        
        let mut db = self.db.write().await;
        let mut next_id = self.next_id.write().await;
        
        // Check if user already exists
        for user in db.values() {
            if user.username == request.username || user.email == request.email {
                return Err(APIError::ValidationError { 
                    message: "User already exists".to_string() 
                });
            }
        }
        
        let now = chrono::Utc::now();
        let user = User {
            id: *next_id,
            username: request.username,
            email: request.email,
            created_at: now,
            updated_at: now,
        };
        
        db.insert(user.id, user.clone());
        *next_id += 1;
        
        Ok(user)
    }
    
    pub async fn get_user(&self, id: u64) -> Result<Option<User>, APIError> {
        let db = self.db.read().await;
        Ok(db.get(&id).cloned())
    }
    
    pub async fn update_user(&self, id: u64, request: UpdateUserRequest) -> Result<Option<User>, APIError> {
        let mut db = self.db.write().await;
        
        if let Some(user) = db.get_mut(&id) {
            if let Some(username) = request.username {
                if !username.is_empty() {
                    user.username = username;
                }
            }
            
            if let Some(email) = request.email {
                if email.contains('@') {
                    user.email = email;
                } else {
                    return Err(APIError::ValidationError { 
                        message: "Invalid email format".to_string() 
                    });
                }
            }
            
            user.updated_at = chrono::Utc::now();
            Ok(Some(user.clone()))
        } else {
            Ok(None)
        }
    }
    
    pub async fn delete_user(&self, id: u64) -> Result<bool, APIError> {
        let mut db = self.db.write().await;
        Ok(db.remove(&id).is_some())
    }
    
    pub async fn list_users(&self, page: u32, per_page: u32) -> Result<UserListResponse, APIError> {
        let db = self.db.read().await;
        let users: Vec<User> = db.values().cloned().collect();
        
        let total = users.len() as u64;
        let start = (page as usize * per_page as usize).min(users.len());
        let end = (start + per_page as usize).min(users.len());
        
        let paginated_users = users[start..end].to_vec();
        
        Ok(UserListResponse {
            users: paginated_users,
            total,
            page,
            per_page,
        })
    }
}

// REST API handlers
pub struct RESTHandlers {
    user_service: Arc<UserService>,
}

impl RESTHandlers {
    pub fn new(user_service: Arc<UserService>) -> Self {
        Self { user_service }
    }
    
    // GET /users
    pub async fn list_users(
        State(state): State<Arc<Self>>,
        Query(params): Query<HashMap<String, String>>,
    ) -> Result<Json<UserListResponse>, (StatusCode, Json<APIErrorResponse>)> {
        let page = params.get("page").and_then(|p| p.parse::<u32>().ok()).unwrap_or(1);
        let per_page = params.get("per_page").and_then(|p| p.parse::<u32>().ok()).unwrap_or(10);
        
        match state.user_service.list_users(page, per_page).await {
            Ok(response) => Ok(Json(response)),
            Err(e) => Err((StatusCode::INTERNAL_SERVER_ERROR, Json(APIErrorResponse {
                error: e.to_string(),
                message: "Failed to list users".to_string(),
            }))),
        }
    }
    
    // POST /users
    pub async fn create_user(
        State(state): State<Arc<Self>>,
        Json(request): Json<CreateUserRequest>,
    ) -> Result<(StatusCode, Json<User>), (StatusCode, Json<APIErrorResponse>)> {
        match state.user_service.create_user(request).await {
            Ok(user) => Ok((StatusCode::CREATED, Json(user))),
            Err(e) => Err((StatusCode::BAD_REQUEST, Json(APIErrorResponse {
                error: e.to_string(),
                message: "Failed to create user".to_string(),
            }))),
        }
    }
    
    // GET /users/{id}
    pub async fn get_user(
        State(state): State<Arc<Self>>,
        Path(id): Path<u64>,
    ) -> Result<Json<User>, (StatusCode, Json<APIErrorResponse>)> {
        match state.user_service.get_user(id).await {
            Ok(Some(user)) => Ok(Json(user)),
            Ok(None) => Err((StatusCode::NOT_FOUND, Json(APIErrorResponse {
                error: "User not found".to_string(),
                message: "The requested user does not exist".to_string(),
            }))),
            Err(e) => Err((StatusCode::INTERNAL_SERVER_ERROR, Json(APIErrorResponse {
                error: e.to_string(),
                message: "Failed to get user".to_string(),
            }))),
        }
    }
    
    // PUT /users/{id}
    pub async fn update_user(
        State(state): State<Arc<Self>>,
        Path(id): Path<u64>,
        Json(request): Json<UpdateUserRequest>,
    ) -> Result<Json<User>, (StatusCode, Json<APIErrorResponse>)> {
        match state.user_service.update_user(id, request).await {
            Ok(Some(user)) => Ok(Json(user)),
            Ok(None) => Err((StatusCode::NOT_FOUND, Json(APIErrorResponse {
                error: "User not found".to_string(),
                message: "The requested user does not exist".to_string(),
            }))),
            Err(e) => Err((StatusCode::BAD_REQUEST, Json(APIErrorResponse {
                error: e.to_string(),
                message: "Failed to update user".to_string(),
            }))),
        }
    }
    
    // DELETE /users/{id}
    pub async fn delete_user(
        State(state): State<Arc<Self>>,
        Path(id): Path<u64>,
    ) -> Result<StatusCode, (StatusCode, Json<APIErrorResponse>)> {
        match state.user_service.delete_user(id).await {
            Ok(true) => Ok(StatusCode::NO_CONTENT),
            Ok(false) => Err((StatusCode::NOT_FOUND, Json(APIErrorResponse {
                error: "User not found".to_string(),
                message: "The requested user does not exist".to_string(),
            }))),
            Err(e) => Err((StatusCode::INTERNAL_SERVER_ERROR, Json(APIErrorResponse {
                error: e.to_string(),
                message: "Failed to delete user".to_string(),
            }))),
        }
    }
}

#[derive(Debug, Serialize)]
pub struct APIErrorResponse {
    pub error: String,
    pub message: String,
}

// REST API router
pub fn create_rest_router(user_service: Arc<UserService>) -> Router {
    let handlers = Arc::new(RESTHandlers::new(user_service));
    
    Router::new()
        .route("/users", get(RESTHandlers::list_users))
        .route("/users", post(RESTHandlers::create_user))
        .route("/users/:id", get(RESTHandlers::get_user))
        .route("/users/:id", put(RESTHandlers::update_user))
        .route("/users/:id", delete(RESTHandlers::delete_user))
        .with_state(handlers)
}
```

## 🔍 GraphQL API Design

### GraphQL Implementation

```rust
[graphql_design]
schema_first: true
type_safety: true
introspection: true
subscriptions: true

[graphql_implementation]
use juniper::{GraphQLObject, GraphQLInputObject, GraphQLEnum, FieldResult};
use juniper::RootNode;

// GraphQL schema
#[derive(GraphQLObject)]
pub struct User {
    pub id: i32,
    pub username: String,
    pub email: String,
    pub created_at: String,
    pub updated_at: String,
}

#[derive(GraphQLInputObject)]
pub struct CreateUserInput {
    pub username: String,
    pub email: String,
    pub password: String,
}

#[derive(GraphQLInputObject)]
pub struct UpdateUserInput {
    pub username: Option<String>,
    pub email: Option<String>,
}

#[derive(GraphQLObject)]
pub struct UserList {
    pub users: Vec<User>,
    pub total: i32,
    pub page: i32,
    pub per_page: i32,
}

#[derive(GraphQLEnum)]
pub enum UserSortBy {
    Username,
    Email,
    CreatedAt,
}

// GraphQL queries
pub struct Query;

#[juniper::graphql_object]
impl Query {
    fn user(&self, id: i32) -> FieldResult<Option<User>> {
        // Implementation would connect to actual service
        Ok(Some(User {
            id,
            username: "test_user".to_string(),
            email: "test@example.com".to_string(),
            created_at: "2024-01-01T00:00:00Z".to_string(),
            updated_at: "2024-01-01T00:00:00Z".to_string(),
        }))
    }
    
    fn users(
        &self,
        page: Option<i32>,
        per_page: Option<i32>,
        sort_by: Option<UserSortBy>,
    ) -> FieldResult<UserList> {
        let page = page.unwrap_or(1);
        let per_page = per_page.unwrap_or(10);
        
        // Implementation would connect to actual service
        Ok(UserList {
            users: vec![
                User {
                    id: 1,
                    username: "user1".to_string(),
                    email: "user1@example.com".to_string(),
                    created_at: "2024-01-01T00:00:00Z".to_string(),
                    updated_at: "2024-01-01T00:00:00Z".to_string(),
                },
                User {
                    id: 2,
                    username: "user2".to_string(),
                    email: "user2@example.com".to_string(),
                    created_at: "2024-01-02T00:00:00Z".to_string(),
                    updated_at: "2024-01-02T00:00:00Z".to_string(),
                },
            ],
            total: 2,
            page,
            per_page,
        })
    }
}

// GraphQL mutations
pub struct Mutation;

#[juniper::graphql_object]
impl Mutation {
    fn create_user(&self, input: CreateUserInput) -> FieldResult<User> {
        // Validate input
        if input.username.is_empty() {
            return Err("Username is required".into());
        }
        
        if !input.email.contains('@') {
            return Err("Invalid email format".into());
        }
        
        // Implementation would connect to actual service
        Ok(User {
            id: 1,
            username: input.username,
            email: input.email,
            created_at: "2024-01-01T00:00:00Z".to_string(),
            updated_at: "2024-01-01T00:00:00Z".to_string(),
        })
    }
    
    fn update_user(&self, id: i32, input: UpdateUserInput) -> FieldResult<Option<User>> {
        // Implementation would connect to actual service
        Ok(Some(User {
            id,
            username: input.username.unwrap_or_else(|| "updated_user".to_string()),
            email: input.email.unwrap_or_else(|| "updated@example.com".to_string()),
            created_at: "2024-01-01T00:00:00Z".to_string(),
            updated_at: "2024-01-01T00:00:00Z".to_string(),
        }))
    }
    
    fn delete_user(&self, id: i32) -> FieldResult<bool> {
        // Implementation would connect to actual service
        Ok(true)
    }
}

// GraphQL schema
pub type Schema = RootNode<'static, Query, Mutation>;

pub fn create_schema() -> Schema {
    Schema::new(Query, Mutation)
}

// GraphQL handler
pub async fn graphql_handler(
    State(schema): State<Arc<Schema>>,
    Json(request): Json<juniper::http::GraphQLRequest>,
) -> Json<juniper::http::GraphQLResponse> {
    let response = request.execute(&schema, &()).await;
    Json(response)
}

// GraphQL playground handler
pub async fn graphql_playground() -> impl axum::response::IntoResponse {
    axum::response::Html(
        r#"
        <!DOCTYPE html>
        <html>
        <head>
            <title>GraphQL Playground</title>
            <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/graphql-playground-react@1.7.42/build/static/css/index.css" />
            <script src="https://cdn.jsdelivr.net/npm/graphql-playground-react@1.7.42/build/static/js/middleware.js"></script>
        </head>
        <body>
            <div id="root"></div>
            <script>
                window.addEventListener('load', function (event) {
                    GraphQLPlayground.init(document.getElementById('root'), {
                        endpoint: '/graphql'
                    })
                })
            </script>
        </body>
        </html>
        "#,
    )
}
```

## 🔌 gRPC API Design

### gRPC Implementation

```rust
[grpc_design]
protocol_buffers: true
streaming: true
bidirectional: true
code_generation: true

[grpc_implementation]
use tonic::{transport::Server, Request, Response, Status};
use tonic::codegen::*;

// Proto file would define:
// service UserService {
//   rpc GetUser(GetUserRequest) returns (User);
//   rpc CreateUser(CreateUserRequest) returns (User);
//   rpc UpdateUser(UpdateUserRequest) returns (User);
//   rpc DeleteUser(DeleteUserRequest) returns (DeleteUserResponse);
//   rpc ListUsers(ListUsersRequest) returns (ListUsersResponse);
//   rpc StreamUsers(StreamUsersRequest) returns (stream User);
// }

// Generated protobuf types (simplified)
#[derive(Debug, Clone)]
pub struct User {
    pub id: u64,
    pub username: String,
    pub email: String,
    pub created_at: String,
    pub updated_at: String,
}

#[derive(Debug, Clone)]
pub struct GetUserRequest {
    pub id: u64,
}

#[derive(Debug, Clone)]
pub struct CreateUserRequest {
    pub username: String,
    pub email: String,
    pub password: String,
}

#[derive(Debug, Clone)]
pub struct UpdateUserRequest {
    pub id: u64,
    pub username: Option<String>,
    pub email: Option<String>,
}

#[derive(Debug, Clone)]
pub struct DeleteUserRequest {
    pub id: u64,
}

#[derive(Debug, Clone)]
pub struct DeleteUserResponse {
    pub success: bool,
}

#[derive(Debug, Clone)]
pub struct ListUsersRequest {
    pub page: u32,
    pub per_page: u32,
}

#[derive(Debug, Clone)]
pub struct ListUsersResponse {
    pub users: Vec<User>,
    pub total: u64,
    pub page: u32,
    pub per_page: u32,
}

#[derive(Debug, Clone)]
pub struct StreamUsersRequest {
    pub filter: Option<String>,
}

// gRPC service implementation
pub struct UserService {
    user_repository: Arc<UserRepository>,
}

impl UserService {
    pub fn new(user_repository: Arc<UserRepository>) -> Self {
        Self { user_repository }
    }
}

#[tonic::async_trait]
impl user_service_server::UserService for UserService {
    async fn get_user(
        &self,
        request: Request<GetUserRequest>,
    ) -> Result<Response<User>, Status> {
        let req = request.into_inner();
        
        match self.user_repository.get_user(req.id).await {
            Ok(Some(user)) => Ok(Response::new(user)),
            Ok(None) => Err(Status::not_found("User not found")),
            Err(e) => Err(Status::internal(e.to_string())),
        }
    }
    
    async fn create_user(
        &self,
        request: Request<CreateUserRequest>,
    ) -> Result<Response<User>, Status> {
        let req = request.into_inner();
        
        // Validate input
        if req.username.is_empty() || req.email.is_empty() {
            return Err(Status::invalid_argument("Username and email are required"));
        }
        
        if !req.email.contains('@') {
            return Err(Status::invalid_argument("Invalid email format"));
        }
        
        match self.user_repository.create_user(req).await {
            Ok(user) => Ok(Response::new(user)),
            Err(e) => Err(Status::internal(e.to_string())),
        }
    }
    
    async fn update_user(
        &self,
        request: Request<UpdateUserRequest>,
    ) -> Result<Response<User>, Status> {
        let req = request.into_inner();
        
        match self.user_repository.update_user(req).await {
            Ok(Some(user)) => Ok(Response::new(user)),
            Ok(None) => Err(Status::not_found("User not found")),
            Err(e) => Err(Status::internal(e.to_string())),
        }
    }
    
    async fn delete_user(
        &self,
        request: Request<DeleteUserRequest>,
    ) -> Result<Response<DeleteUserResponse>, Status> {
        let req = request.into_inner();
        
        match self.user_repository.delete_user(req.id).await {
            Ok(success) => Ok(Response::new(DeleteUserResponse { success })),
            Err(e) => Err(Status::internal(e.to_string())),
        }
    }
    
    async fn list_users(
        &self,
        request: Request<ListUsersRequest>,
    ) -> Result<Response<ListUsersResponse>, Status> {
        let req = request.into_inner();
        
        match self.user_repository.list_users(req.page, req.per_page).await {
            Ok(response) => Ok(Response::new(response)),
            Err(e) => Err(Status::internal(e.to_string())),
        }
    }
    
    type StreamUsersStream = Pin<Box<dyn Stream<Item = Result<User, Status>> + Send + 'static>>;
    
    async fn stream_users(
        &self,
        request: Request<StreamUsersRequest>,
    ) -> Result<Response<Self::StreamUsersStream>, Status> {
        let req = request.into_inner();
        
        let user_repository = self.user_repository.clone();
        let stream = tokio_stream::wrappers::ReceiverStream::new(
            user_repository.stream_users(req.filter).await?
        );
        
        Ok(Response::new(Box::pin(stream) as Self::StreamUsersStream))
    }
}

// User repository for gRPC
pub struct UserRepository {
    db: Arc<RwLock<HashMap<u64, User>>>,
    next_id: Arc<RwLock<u64>>,
}

impl UserRepository {
    pub fn new() -> Self {
        Self {
            db: Arc::new(RwLock::new(HashMap::new())),
            next_id: Arc::new(RwLock::new(1)),
        }
    }
    
    pub async fn get_user(&self, id: u64) -> Result<Option<User>, Box<dyn std::error::Error>> {
        let db = self.db.read().await;
        Ok(db.get(&id).cloned())
    }
    
    pub async fn create_user(&self, request: CreateUserRequest) -> Result<User, Box<dyn std::error::Error>> {
        let mut db = self.db.write().await;
        let mut next_id = self.next_id.write().await;
        
        let now = chrono::Utc::now().to_rfc3339();
        let user = User {
            id: *next_id,
            username: request.username,
            email: request.email,
            created_at: now.clone(),
            updated_at: now,
        };
        
        db.insert(user.id, user.clone());
        *next_id += 1;
        
        Ok(user)
    }
    
    pub async fn update_user(&self, request: UpdateUserRequest) -> Result<Option<User>, Box<dyn std::error::Error>> {
        let mut db = self.db.write().await;
        
        if let Some(user) = db.get_mut(&request.id) {
            if let Some(username) = request.username {
                if !username.is_empty() {
                    user.username = username;
                }
            }
            
            if let Some(email) = request.email {
                if email.contains('@') {
                    user.email = email;
                }
            }
            
            user.updated_at = chrono::Utc::now().to_rfc3339();
            Ok(Some(user.clone()))
        } else {
            Ok(None)
        }
    }
    
    pub async fn delete_user(&self, id: u64) -> Result<bool, Box<dyn std::error::Error>> {
        let mut db = self.db.write().await;
        Ok(db.remove(&id).is_some())
    }
    
    pub async fn list_users(&self, page: u32, per_page: u32) -> Result<ListUsersResponse, Box<dyn std::error::Error>> {
        let db = self.db.read().await;
        let users: Vec<User> = db.values().cloned().collect();
        
        let total = users.len() as u64;
        let start = (page as usize * per_page as usize).min(users.len());
        let end = (start + per_page as usize).min(users.len());
        
        let paginated_users = users[start..end].to_vec();
        
        Ok(ListUsersResponse {
            users: paginated_users,
            total,
            page,
            per_page,
        })
    }
    
    pub async fn stream_users(
        &self,
        filter: Option<String>,
    ) -> Result<tokio::sync::mpsc::Receiver<Result<User, Status>>, Box<dyn std::error::Error>> {
        let (tx, rx) = tokio::sync::mpsc::channel(100);
        let db = self.db.read().await;
        let users: Vec<User> = db.values().cloned().collect();
        
        tokio::spawn(async move {
            for user in users {
                if let Some(ref filter_str) = filter {
                    if !user.username.contains(filter_str) && !user.email.contains(filter_str) {
                        continue;
                    }
                }
                
                if tx.send(Ok(user)).await.is_err() {
                    break;
                }
                
                tokio::time::sleep(Duration::from_millis(100)).await;
            }
        });
        
        Ok(rx)
    }
}

// gRPC server
pub async fn start_grpc_server() -> Result<(), Box<dyn std::error::Error>> {
    let addr = "[::1]:50051".parse()?;
    let user_repository = Arc::new(UserRepository::new());
    let user_service = UserService::new(user_repository);
    
    println!("gRPC server listening on {}", addr);
    
    Server::builder()
        .add_service(user_service_server::UserServiceServer::new(user_service))
        .serve(addr)
        .await?;
    
    Ok(())
}
```

## 🎯 Best Practices

### 1. **API Design Principles**
- Follow RESTful conventions for REST APIs
- Use consistent naming and URL patterns
- Implement proper HTTP status codes
- Design for versioning and backward compatibility

### 2. **Performance and Scalability**
- Implement caching strategies
- Use pagination for large datasets
- Optimize database queries
- Implement rate limiting and throttling

### 3. **Security**
- Implement proper authentication and authorization
- Validate all input data
- Use HTTPS for all communications
- Implement API key management

### 4. **Documentation**
- Provide comprehensive API documentation
- Include code examples and use cases
- Document error codes and responses
- Maintain up-to-date OpenAPI/Swagger specs

### 5. **TuskLang Integration**
- Use TuskLang configuration for API parameters
- Implement API monitoring with TuskLang
- Configure rate limiting through TuskLang
- Use TuskLang for API versioning and routing

API design with TuskLang and Rust provides comprehensive tools for building robust, scalable, and well-documented web services with support for multiple protocols and patterns. 