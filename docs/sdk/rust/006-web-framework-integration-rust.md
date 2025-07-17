# 🦀 TuskLang Rust Web Framework Integration

**"We don't bow to any king" - Rust Edition**

Master web framework integration with TuskLang Rust. From Actix-web to Axum, Rocket to Warp - learn how to build high-performance web applications with zero-copy configuration parsing and real-time database integration.

## 🚀 Actix-web Integration

### Basic Setup

```rust
use actix_web::{web, App, HttpServer, HttpResponse, Result};
use serde::{Deserialize, Serialize};
use tusklang_rust::{parse, Parser};
use std::sync::Arc;

#[derive(Deserialize)]
struct PaymentRequest {
    amount: f64,
    recipient: String,
}

#[derive(Serialize)]
struct PaymentResponse {
    success: bool,
    transaction_id: String,
    amount: f64,
    recipient: String,
}

async fn process_payment(
    req: web::Json<PaymentRequest>,
    parser: web::Data<Arc<Parser>>,
) -> Result<HttpResponse> {
    let result = parser.execute_fujsen(
        "payment",
        "process",
        &[&req.amount, &req.recipient]
    ).await?;
    
    Ok(HttpResponse::Ok().json(result))
}

async fn get_users(parser: web::Data<Arc<Parser>>) -> Result<HttpResponse> {
    let users = parser.query("SELECT * FROM users WHERE active = 1").await?;
    Ok(HttpResponse::Ok().json(users))
}

async fn get_config(parser: web::Data<Arc<Parser>>) -> Result<HttpResponse> {
    let config = parser.get_config()?;
    Ok(HttpResponse::Ok().json(config))
}

#[actix_web::main]
async fn main() -> std::io::Result<()> {
    let mut parser = Parser::new();
    let config = parser.parse_file("app.tsk").expect("Failed to parse config");
    
    let server_config = &config["server"];
    let host = server_config["host"].as_str().unwrap();
    let port = server_config["port"].as_u64().unwrap();
    
    HttpServer::new(move || {
        App::new()
            .app_data(web::Data::new(Arc::new(parser.clone())))
            .route("/api/users", web::get().to(get_users))
            .route("/api/payment", web::post().to(process_payment))
            .route("/api/config", web::get().to(get_config))
    })
    .bind(format!("{}:{}", host, port))?
    .run()
    .await
}
```

### Advanced Actix-web Features

```rust
use actix_web::{web, App, HttpServer, HttpResponse, Result, middleware};
use actix_cors::Cors;
use actix_web::dev::ServiceRequest;
use tusklang_rust::{parse, Parser};
use std::sync::Arc;

// Custom middleware for TuskLang integration
async fn tusk_middleware(
    req: ServiceRequest,
    parser: web::Data<Arc<Parser>>,
) -> Result<ServiceRequest, actix_web::Error> {
    // Add TuskLang context to request
    let mut req_data = req.app_data::<web::Data<Arc<Parser>>>().unwrap();
    
    // Execute pre-request logic
    let user_id = req.headers().get("X-User-ID")
        .and_then(|h| h.to_str().ok())
        .unwrap_or("anonymous");
    
    let user_permissions = parser.execute_fujsen(
        "auth",
        "get_permissions",
        &[&user_id]
    ).await.unwrap_or_default();
    
    req.extensions_mut().insert(user_permissions);
    Ok(req)
}

async fn protected_route(
    req: HttpRequest,
    parser: web::Data<Arc<Parser>>,
) -> Result<HttpResponse> {
    let user_permissions = req.extensions().get::<serde_json::Value>()
        .unwrap_or(&serde_json::Value::Null);
    
    if user_permissions["can_access"].as_bool().unwrap_or(false) {
        let data = parser.query("SELECT * FROM protected_data").await?;
        Ok(HttpResponse::Ok().json(data))
    } else {
        Ok(HttpResponse::Forbidden().json(json!({
            "error": "Access denied"
        })))
    }
}

#[actix_web::main]
async fn main() -> std::io::Result<()> {
    let mut parser = Parser::new();
    let config = parser.parse_file("app.tsk").expect("Failed to parse config");
    
    let server_config = &config["server"];
    let host = server_config["host"].as_str().unwrap();
    let port = server_config["port"].as_u64().unwrap();
    
    let cors_config = &config["cors"];
    let allowed_origins = cors_config["allowed_origins"].as_array().unwrap();
    
    HttpServer::new(move || {
        let cors = Cors::default()
            .allowed_origins(allowed_origins.iter().map(|s| s.as_str().unwrap()).collect::<Vec<_>>())
            .allowed_methods(vec!["GET", "POST", "PUT", "DELETE"])
            .allowed_headers(vec![http::header::AUTHORIZATION, http::header::ACCEPT])
            .max_age(3600);
        
        App::new()
            .app_data(web::Data::new(Arc::new(parser.clone())))
            .wrap(cors)
            .wrap(middleware::Logger::default())
            .service(
                web::scope("/api")
                    .route("/protected", web::get().to(protected_route))
                    .route("/users", web::get().to(get_users))
                    .route("/payment", web::post().to(process_payment))
            )
    })
    .bind(format!("{}:{}", host, port))?
    .run()
    .await
}
```

## ⚡ Axum Integration

### Basic Setup

```rust
use axum::{
    extract::Json,
    http::StatusCode,
    response::Json as JsonResponse,
    routing::{get, post},
    Router,
};
use serde::{Deserialize, Serialize};
use tusklang_rust::{parse, Parser};
use std::sync::Arc;

#[derive(Deserialize)]
struct PaymentRequest {
    amount: f64,
    recipient: String,
}

#[derive(Serialize)]
struct PaymentResponse {
    success: bool,
    transaction_id: String,
    amount: f64,
    recipient: String,
}

async fn process_payment(
    Json(req): Json<PaymentRequest>,
    axum::extract::State(parser): axum::extract::State<Arc<Parser>>,
) -> Result<JsonResponse<PaymentResponse>, StatusCode> {
    let result = parser.execute_fujsen(
        "payment",
        "process",
        &[&req.amount, &req.recipient]
    ).await
    .map_err(|_| StatusCode::INTERNAL_SERVER_ERROR)?;
    
    Ok(JsonResponse(result))
}

async fn get_users(
    axum::extract::State(parser): axum::extract::State<Arc<Parser>>,
) -> Result<JsonResponse<Vec<serde_json::Value>>, StatusCode> {
    let users = parser.query("SELECT * FROM users WHERE active = 1").await
        .map_err(|_| StatusCode::INTERNAL_SERVER_ERROR)?;
    
    Ok(JsonResponse(users))
}

async fn get_config(
    axum::extract::State(parser): axum::extract::State<Arc<Parser>>,
) -> Result<JsonResponse<serde_json::Value>, StatusCode> {
    let config = parser.get_config()
        .map_err(|_| StatusCode::INTERNAL_SERVER_ERROR)?;
    
    Ok(JsonResponse(config))
}

#[tokio::main]
async fn main() {
    let mut parser = Parser::new();
    let config = parser.parse_file("app.tsk").expect("Failed to parse config");
    
    let app = Router::new()
        .route("/api/users", get(get_users))
        .route("/api/payment", post(process_payment))
        .route("/api/config", get(get_config))
        .with_state(Arc::new(parser));
    
    let server_config = &config["server"];
    let host = server_config["host"].as_str().unwrap();
    let port = server_config["port"].as_u64().unwrap();
    
    axum::Server::bind(&format!("{}:{}", host, port).parse().unwrap())
        .serve(app.into_make_service())
        .await
        .unwrap();
}
```

### Advanced Axum Features

```rust
use axum::{
    extract::{Json, Path, Query, State},
    http::{HeaderMap, StatusCode},
    response::Json as JsonResponse,
    routing::{get, post, put, delete},
    Router,
};
use serde::{Deserialize, Serialize};
use tusklang_rust::{parse, Parser};
use std::sync::Arc;
use tower_http::cors::{CorsLayer, Any};

#[derive(Deserialize)]
struct UserQuery {
    page: Option<u32>,
    limit: Option<u32>,
    search: Option<String>,
}

#[derive(Deserialize)]
struct CreateUserRequest {
    name: String,
    email: String,
    role: String,
}

#[derive(Serialize)]
struct UserResponse {
    id: u32,
    name: String,
    email: String,
    role: String,
    created_at: String,
}

async fn get_users_paginated(
    Query(query): Query<UserQuery>,
    State(parser): State<Arc<Parser>>,
) -> Result<JsonResponse<Vec<UserResponse>>, StatusCode> {
    let page = query.page.unwrap_or(1);
    let limit = query.limit.unwrap_or(10);
    let offset = (page - 1) * limit;
    
    let where_clause = if let Some(search) = query.search {
        format!("WHERE name LIKE '%{}%' OR email LIKE '%{}%'", search, search)
    } else {
        String::new()
    };
    
    let sql = format!(
        "SELECT id, name, email, role, created_at FROM users {} ORDER BY created_at DESC LIMIT {} OFFSET {}",
        where_clause, limit, offset
    );
    
    let users = parser.query(&sql).await
        .map_err(|_| StatusCode::INTERNAL_SERVER_ERROR)?;
    
    let user_responses: Vec<UserResponse> = users.into_iter()
        .map(|user| UserResponse {
            id: user["id"].as_u64().unwrap() as u32,
            name: user["name"].as_str().unwrap().to_string(),
            email: user["email"].as_str().unwrap().to_string(),
            role: user["role"].as_str().unwrap().to_string(),
            created_at: user["created_at"].as_str().unwrap().to_string(),
        })
        .collect();
    
    Ok(JsonResponse(user_responses))
}

async fn create_user(
    Json(req): Json<CreateUserRequest>,
    State(parser): State<Arc<Parser>>,
) -> Result<JsonResponse<UserResponse>, StatusCode> {
    // Validate user data using TuskLang
    let validation_result = parser.execute_fujsen(
        "validation",
        "validate_user",
        &[&req.name, &req.email, &req.role]
    ).await
    .map_err(|_| StatusCode::BAD_REQUEST)?;
    
    if !validation_result["valid"].as_bool().unwrap_or(false) {
        return Err(StatusCode::BAD_REQUEST);
    }
    
    // Insert user into database
    let sql = "INSERT INTO users (name, email, role) VALUES (?, ?, ?) RETURNING id, name, email, role, created_at";
    let result = parser.query(sql, &[&req.name, &req.email, &req.role]).await
        .map_err(|_| StatusCode::INTERNAL_SERVER_ERROR)?;
    
    let user = &result[0];
    let user_response = UserResponse {
        id: user["id"].as_u64().unwrap() as u32,
        name: user["name"].as_str().unwrap().to_string(),
        email: user["email"].as_str().unwrap().to_string(),
        role: user["role"].as_str().unwrap().to_string(),
        created_at: user["created_at"].as_str().unwrap().to_string(),
    };
    
    Ok(JsonResponse(user_response))
}

async fn update_user(
    Path(user_id): Path<u32>,
    Json(req): Json<CreateUserRequest>,
    State(parser): State<Arc<Parser>>,
) -> Result<JsonResponse<UserResponse>, StatusCode> {
    let sql = "UPDATE users SET name = ?, email = ?, role = ? WHERE id = ? RETURNING id, name, email, role, created_at";
    let result = parser.query(sql, &[&req.name, &req.email, &req.role, &user_id]).await
        .map_err(|_| StatusCode::INTERNAL_SERVER_ERROR)?;
    
    if result.is_empty() {
        return Err(StatusCode::NOT_FOUND);
    }
    
    let user = &result[0];
    let user_response = UserResponse {
        id: user["id"].as_u64().unwrap() as u32,
        name: user["name"].as_str().unwrap().to_string(),
        email: user["email"].as_str().unwrap().to_string(),
        role: user["role"].as_str().unwrap().to_string(),
        created_at: user["created_at"].as_str().unwrap().to_string(),
    };
    
    Ok(JsonResponse(user_response))
}

async fn delete_user(
    Path(user_id): Path<u32>,
    State(parser): State<Arc<Parser>>,
) -> Result<StatusCode, StatusCode> {
    let sql = "DELETE FROM users WHERE id = ?";
    let result = parser.execute(sql, &[&user_id]).await
        .map_err(|_| StatusCode::INTERNAL_SERVER_ERROR)?;
    
    if result.rows_affected() == 0 {
        return Err(StatusCode::NOT_FOUND);
    }
    
    Ok(StatusCode::NO_CONTENT)
}

#[tokio::main]
async fn main() {
    let mut parser = Parser::new();
    let config = parser.parse_file("app.tsk").expect("Failed to parse config");
    
    let cors = CorsLayer::new()
        .allow_origin(Any)
        .allow_methods(Any)
        .allow_headers(Any);
    
    let app = Router::new()
        .route("/api/users", get(get_users_paginated).post(create_user))
        .route("/api/users/:id", put(update_user).delete(delete_user))
        .layer(cors)
        .with_state(Arc::new(parser));
    
    let server_config = &config["server"];
    let host = server_config["host"].as_str().unwrap();
    let port = server_config["port"].as_u64().unwrap();
    
    axum::Server::bind(&format!("{}:{}", host, port).parse().unwrap())
        .serve(app.into_make_service())
        .await
        .unwrap();
}
```

## 🚀 Rocket Integration

### Basic Setup

```rust
use rocket::{post, get, serde::{json::Json, Deserialize, Serialize}};
use tusklang_rust::{parse, Parser};

#[derive(Deserialize)]
struct PaymentRequest {
    amount: f64,
    recipient: String,
}

#[derive(Serialize)]
struct PaymentResponse {
    success: bool,
    transaction_id: String,
    amount: f64,
    recipient: String,
}

#[get("/api/users")]
async fn get_users(parser: &rocket::State<Parser>) -> Json<Vec<serde_json::Value>> {
    let users = parser.query("SELECT * FROM users WHERE active = 1").await
        .expect("Failed to query users");
    Json(users)
}

#[post("/api/payment", data = "<req>")]
async fn process_payment(
    req: Json<PaymentRequest>,
    parser: &rocket::State<Parser>,
) -> Json<PaymentResponse> {
    let result = parser.execute_fujsen(
        "payment",
        "process",
        &[&req.amount, &req.recipient]
    ).await
    .expect("Failed to process payment");
    
    Json(result)
}

#[get("/api/config")]
async fn get_config(parser: &rocket::State<Parser>) -> Json<serde_json::Value> {
    let config = parser.get_config().expect("Failed to get config");
    Json(config)
}

#[launch]
fn rocket() -> _ {
    let mut parser = Parser::new();
    let config = parser.parse_file("rocket.tsk").expect("Failed to parse config");
    
    rocket::build()
        .manage(parser)
        .mount("/", routes![get_users, process_payment, get_config])
}
```

### Advanced Rocket Features

```rust
use rocket::{
    post, get, put, delete,
    serde::{json::Json, Deserialize, Serialize},
    http::Status,
    request::{FromRequest, Outcome},
    State,
};
use tusklang_rust::{parse, Parser};
use std::collections::HashMap;

#[derive(Deserialize)]
struct UserRequest {
    name: String,
    email: String,
    role: String,
}

#[derive(Serialize)]
struct UserResponse {
    id: u32,
    name: String,
    email: String,
    role: String,
    created_at: String,
}

// Custom request guard for authentication
struct AuthenticatedUser {
    user_id: u32,
    permissions: Vec<String>,
}

#[rocket::async_trait]
impl<'r> FromRequest<'r> for AuthenticatedUser {
    type Error = ();

    async fn from_request(request: &'r rocket::Request<'_>) -> Outcome<Self, Self::Error> {
        let parser = request.guard::<&State<Parser>>().await.succeeded()?;
        
        let token = request.headers().get_one("Authorization")
            .ok_or(())?;
        
        let auth_result = parser.execute_fujsen(
            "auth",
            "validate_token",
            &[&token]
        ).await
        .map_err(|_| ())?;
        
        if !auth_result["valid"].as_bool().unwrap_or(false) {
            return Outcome::Error((Status::Unauthorized, ()));
        }
        
        Outcome::Success(AuthenticatedUser {
            user_id: auth_result["user_id"].as_u64().unwrap() as u32,
            permissions: auth_result["permissions"].as_array().unwrap()
                .iter()
                .map(|p| p.as_str().unwrap().to_string())
                .collect(),
        })
    }
}

#[get("/api/users?<page>&<limit>&<search>")]
async fn get_users_paginated(
    page: Option<u32>,
    limit: Option<u32>,
    search: Option<String>,
    parser: &State<Parser>,
) -> Json<Vec<UserResponse>> {
    let page = page.unwrap_or(1);
    let limit = limit.unwrap_or(10);
    let offset = (page - 1) * limit;
    
    let where_clause = if let Some(search) = search {
        format!("WHERE name LIKE '%{}%' OR email LIKE '%{}%'", search, search)
    } else {
        String::new()
    };
    
    let sql = format!(
        "SELECT id, name, email, role, created_at FROM users {} ORDER BY created_at DESC LIMIT {} OFFSET {}",
        where_clause, limit, offset
    );
    
    let users = parser.query(&sql).await.expect("Failed to query users");
    
    let user_responses: Vec<UserResponse> = users.into_iter()
        .map(|user| UserResponse {
            id: user["id"].as_u64().unwrap() as u32,
            name: user["name"].as_str().unwrap().to_string(),
            email: user["email"].as_str().unwrap().to_string(),
            role: user["role"].as_str().unwrap().to_string(),
            created_at: user["created_at"].as_str().unwrap().to_string(),
        })
        .collect();
    
    Json(user_responses)
}

#[post("/api/users", data = "<req>")]
async fn create_user(
    req: Json<UserRequest>,
    _auth: AuthenticatedUser,
    parser: &State<Parser>,
) -> Result<Json<UserResponse>, Status> {
    if !_auth.permissions.contains(&"users:create".to_string()) {
        return Err(Status::Forbidden);
    }
    
    let sql = "INSERT INTO users (name, email, role) VALUES (?, ?, ?) RETURNING id, name, email, role, created_at";
    let result = parser.query(sql, &[&req.name, &req.email, &req.role]).await
        .map_err(|_| Status::InternalServerError)?;
    
    let user = &result[0];
    let user_response = UserResponse {
        id: user["id"].as_u64().unwrap() as u32,
        name: user["name"].as_str().unwrap().to_string(),
        email: user["email"].as_str().unwrap().to_string(),
        role: user["role"].as_str().unwrap().to_string(),
        created_at: user["created_at"].as_str().unwrap().to_string(),
    };
    
    Ok(Json(user_response))
}

#[put("/api/users/<id>", data = "<req>")]
async fn update_user(
    id: u32,
    req: Json<UserRequest>,
    _auth: AuthenticatedUser,
    parser: &State<Parser>,
) -> Result<Json<UserResponse>, Status> {
    if !_auth.permissions.contains(&"users:update".to_string()) {
        return Err(Status::Forbidden);
    }
    
    let sql = "UPDATE users SET name = ?, email = ?, role = ? WHERE id = ? RETURNING id, name, email, role, created_at";
    let result = parser.query(sql, &[&req.name, &req.email, &req.role, &id]).await
        .map_err(|_| Status::InternalServerError)?;
    
    if result.is_empty() {
        return Err(Status::NotFound);
    }
    
    let user = &result[0];
    let user_response = UserResponse {
        id: user["id"].as_u64().unwrap() as u32,
        name: user["name"].as_str().unwrap().to_string(),
        email: user["email"].as_str().unwrap().to_string(),
        role: user["role"].as_str().unwrap().to_string(),
        created_at: user["created_at"].as_str().unwrap().to_string(),
    };
    
    Ok(Json(user_response))
}

#[delete("/api/users/<id>")]
async fn delete_user(
    id: u32,
    _auth: AuthenticatedUser,
    parser: &State<Parser>,
) -> Result<Status, Status> {
    if !_auth.permissions.contains(&"users:delete".to_string()) {
        return Err(Status::Forbidden);
    }
    
    let sql = "DELETE FROM users WHERE id = ?";
    let result = parser.execute(sql, &[&id]).await
        .map_err(|_| Status::InternalServerError)?;
    
    if result.rows_affected() == 0 {
        return Err(Status::NotFound);
    }
    
    Ok(Status::NoContent)
}

#[launch]
fn rocket() -> _ {
    let mut parser = Parser::new();
    let config = parser.parse_file("rocket.tsk").expect("Failed to parse config");
    
    rocket::build()
        .manage(parser)
        .mount("/", routes![
            get_users_paginated,
            create_user,
            update_user,
            delete_user
        ])
}
```

## 🌊 Warp Integration

### Basic Setup

```rust
use warp::{Filter, Reply};
use serde::{Deserialize, Serialize};
use tusklang_rust::{parse, Parser};
use std::sync::Arc;

#[derive(Deserialize)]
struct PaymentRequest {
    amount: f64,
    recipient: String,
}

#[derive(Serialize)]
struct PaymentResponse {
    success: bool,
    transaction_id: String,
    amount: f64,
    recipient: String,
}

async fn get_users(parser: Arc<Parser>) -> Result<impl Reply, warp::Rejection> {
    let users = parser.query("SELECT * FROM users WHERE active = 1").await
        .map_err(|_| warp::reject::not_found())?;
    
    Ok(warp::reply::json(&users))
}

async fn process_payment(
    req: PaymentRequest,
    parser: Arc<Parser>,
) -> Result<impl Reply, warp::Rejection> {
    let result = parser.execute_fujsen(
        "payment",
        "process",
        &[&req.amount, &req.recipient]
    ).await
    .map_err(|_| warp::reject::not_found())?;
    
    Ok(warp::reply::json(&result))
}

async fn get_config(parser: Arc<Parser>) -> Result<impl Reply, warp::Rejection> {
    let config = parser.get_config()
        .map_err(|_| warp::reject::not_found())?;
    
    Ok(warp::reply::json(&config))
}

#[tokio::main]
async fn main() {
    let mut parser = Parser::new();
    let config = parser.parse_file("warp.tsk").expect("Failed to parse config");
    let parser = Arc::new(parser);
    
    let parser_filter = warp::any().map(move || Arc::clone(&parser));
    
    let users_route = warp::path("api")
        .and(warp::path("users"))
        .and(warp::get())
        .and(parser_filter.clone())
        .and_then(get_users);
    
    let payment_route = warp::path("api")
        .and(warp::path("payment"))
        .and(warp::post())
        .and(warp::body::json())
        .and(parser_filter.clone())
        .and_then(process_payment);
    
    let config_route = warp::path("api")
        .and(warp::path("config"))
        .and(warp::get())
        .and(parser_filter)
        .and_then(get_config);
    
    let routes = users_route
        .or(payment_route)
        .or(config_route)
        .with(warp::cors().allow_any_origin());
    
    let server_config = &config["server"];
    let host = server_config["host"].as_str().unwrap();
    let port = server_config["port"].as_u64().unwrap();
    
    warp::serve(routes)
        .run((host.parse::<std::net::IpAddr>().unwrap(), port as u16))
        .await;
}
```

## 🔧 Configuration Files

### Actix-web Configuration (`actix.tsk`)

```tsk
app_name: "ActixTuskApp"
version: "1.0.0"
debug: true

[server]
host: "0.0.0.0"
port: 8080
workers: 4

[database]
host: "localhost"
port: 5432
name: "actix_app"
user: "postgres"
password: @env("DB_PASSWORD", "default")

[cors]
allowed_origins: ["http://localhost:3000", "https://myapp.com"]
allowed_methods: ["GET", "POST", "PUT", "DELETE"]
allowed_headers: ["Authorization", "Content-Type"]
max_age: 3600

[security]
jwt_secret: @env("JWT_SECRET")
bcrypt_rounds: 12
session_timeout: "24h"

[logging]
level: @if(@env("APP_ENV") == "production", "info", "debug")
format: @if(@env("APP_ENV") == "production", "json", "text")
file: @if(@env("APP_ENV") == "production", "/var/log/actix.log", "console")

[payment]
processor: "stripe"
webhook_secret: @env("STRIPE_WEBHOOK_SECRET")
currency: "usd"
```

### Axum Configuration (`axum.tsk`)

```tsk
app_name: "AxumTuskApp"
version: "1.0.0"
debug: true

[server]
host: "0.0.0.0"
port: 8080

[database]
host: "localhost"
port: 5432
name: "axum_app"
user: "postgres"
password: @env("DB_PASSWORD", "default")

[cache]
redis_host: "localhost"
redis_port: 6379
redis_db: 0
ttl: "5m"

[auth]
jwt_secret: @env("JWT_SECRET")
token_expiry: "24h"
refresh_token_expiry: "7d"

[api]
rate_limit: 1000
rate_limit_window: "1m"
max_request_size: "10mb"

[monitoring]
metrics_port: 9090
health_check_interval: "30s"
```

### Rocket Configuration (`rocket.tsk`)

```tsk
app_name: "RocketTuskApp"
version: "1.0.0"
debug: true

[server]
host: "0.0.0.0"
port: 8080

[database]
host: "localhost"
port: 5432
name: "rocket_app"
user: "postgres"
password: @env("DB_PASSWORD", "default")

[security]
jwt_secret: @env("JWT_SECRET")
bcrypt_rounds: 12

[logging]
level: "info"
format: "json"

[features]
websockets: true
file_upload: true
max_file_size: "10mb"
```

## 🧪 Testing Web Framework Integration

### Actix-web Tests

```rust
use actix_web::{test, web, App};
use tusklang_rust::{parse, Parser};
use std::sync::Arc;

#[tokio::test]
async fn test_actix_integration() {
    let mut parser = Parser::new();
    let config = parser.parse_file("test_actix.tsk").expect("Failed to parse config");
    let parser = Arc::new(parser);
    
    let app = test::init_service(
        App::new()
            .app_data(web::Data::new(Arc::clone(&parser)))
            .route("/api/users", web::get().to(get_users))
            .route("/api/payment", web::post().to(process_payment))
    ).await;
    
    // Test users endpoint
    let req = test::TestRequest::get()
        .uri("/api/users")
        .to_request();
    let resp = test::call_service(&app, req).await;
    assert!(resp.status().is_success());
    
    // Test payment endpoint
    let payment_req = PaymentRequest {
        amount: 100.0,
        recipient: "test@example.com".to_string(),
    };
    let req = test::TestRequest::post()
        .uri("/api/payment")
        .set_json(&payment_req)
        .to_request();
    let resp = test::call_service(&app, req).await;
    assert!(resp.status().is_success());
}
```

### Axum Tests

```rust
use axum::{
    body::Body,
    http::{Request, StatusCode},
    routing::{get, post},
    Router,
};
use serde_json::json;
use tusklang_rust::{parse, Parser};
use std::sync::Arc;
use tower::ServiceExt;

#[tokio::test]
async fn test_axum_integration() {
    let mut parser = Parser::new();
    let config = parser.parse_file("test_axum.tsk").expect("Failed to parse config");
    let parser = Arc::new(parser);
    
    let app = Router::new()
        .route("/api/users", get(get_users))
        .route("/api/payment", post(process_payment))
        .with_state(Arc::clone(&parser));
    
    // Test users endpoint
    let req = Request::builder()
        .uri("/api/users")
        .body(Body::empty())
        .unwrap();
    let resp = app.clone().oneshot(req).await.unwrap();
    assert_eq!(resp.status(), StatusCode::OK);
    
    // Test payment endpoint
    let payment_req = json!({
        "amount": 100.0,
        "recipient": "test@example.com"
    });
    let req = Request::builder()
        .method("POST")
        .uri("/api/payment")
        .header("content-type", "application/json")
        .body(Body::from(serde_json::to_string(&payment_req).unwrap()))
        .unwrap();
    let resp = app.oneshot(req).await.unwrap();
    assert_eq!(resp.status(), StatusCode::OK);
}
```

## 🚀 Performance Optimization

### Connection Pooling

```rust
use tusklang_rust::adapters::postgresql::{PostgreSQLAdapter, PostgreSQLConfig, PoolConfig};
use std::time::Duration;

async fn setup_optimized_database() -> Result<PostgreSQLAdapter, Box<dyn std::error::Error>> {
    let postgres = PostgreSQLAdapter::with_pool(PostgreSQLConfig {
        host: "localhost".to_string(),
        port: 5432,
        database: "webapp".to_string(),
        user: "postgres".to_string(),
        password: "secret".to_string(),
        ssl_mode: "disable".to_string(),
    }, PoolConfig {
        max_open_conns: 50,
        max_idle_conns: 20,
        conn_max_lifetime: Duration::from_secs(300),
        conn_max_idle_time: Duration::from_secs(60),
    }).await?;
    
    Ok(postgres)
}
```

### Caching Integration

```rust
use tusklang_rust::{parse, Parser, cache::{MemoryCache, RedisCache}};

async fn setup_caching(parser: &mut Parser) -> Result<(), Box<dyn std::error::Error>> {
    // Memory cache for frequently accessed data
    let memory_cache = MemoryCache::new();
    parser.set_cache(memory_cache);
    
    // Redis cache for distributed caching
    let redis_cache = RedisCache::new(RedisConfig {
        host: "localhost".to_string(),
        port: 6379,
        db: 0,
    }).await?;
    
    // Use Redis for expensive operations
    let tsk_content = r#"
[expensive_operations]
user_profile: @cache("1h", "user_profile", @request.user_id)
api_response: @cache("5m", "api_call", @request.endpoint)
"#;
    
    let data = parser.parse(tsk_content).await?;
    println!("Cached operations: {:?}", data["expensive_operations"]);
    
    Ok(())
}
```

## 🔒 Security Integration

### Authentication Middleware

```rust
use actix_web::{dev::ServiceRequest, Error};
use tusklang_rust::{parse, Parser};
use jsonwebtoken::{decode, DecodingKey, Validation};

async fn auth_middleware(
    req: ServiceRequest,
    parser: web::Data<Arc<Parser>>,
) -> Result<ServiceRequest, Error> {
    let token = req.headers().get("Authorization")
        .and_then(|h| h.to_str().ok())
        .and_then(|s| s.strip_prefix("Bearer "));
    
    if let Some(token) = token {
        let jwt_secret = parser.get("security.jwt_secret")
            .expect("JWT secret not configured");
        
        let token_data = decode::<serde_json::Value>(
            token,
            &DecodingKey::from_secret(jwt_secret.as_str().unwrap().as_ref()),
            &Validation::default()
        );
        
        if let Ok(token_data) = token_data {
            req.extensions_mut().insert(token_data.claims);
        }
    }
    
    Ok(req)
}
```

### Rate Limiting

```rust
use actix_web::{dev::ServiceRequest, Error};
use std::collections::HashMap;
use std::sync::Mutex;
use std::time::{Duration, Instant};

struct RateLimiter {
    requests: Mutex<HashMap<String, Vec<Instant>>>,
    limit: usize,
    window: Duration,
}

impl RateLimiter {
    fn new(limit: usize, window: Duration) -> Self {
        Self {
            requests: Mutex::new(HashMap::new()),
            limit,
            window,
        }
    }
    
    fn is_allowed(&self, key: &str) -> bool {
        let mut requests = self.requests.lock().unwrap();
        let now = Instant::now();
        
        // Clean old requests
        if let Some(timestamps) = requests.get_mut(key) {
            timestamps.retain(|&timestamp| now.duration_since(timestamp) < self.window);
            
            if timestamps.len() < self.limit {
                timestamps.push(now);
                true
            } else {
                false
            }
        } else {
            requests.insert(key.to_string(), vec![now]);
            true
        }
    }
}

async fn rate_limit_middleware(
    req: ServiceRequest,
    rate_limiter: web::Data<Arc<RateLimiter>>,
) -> Result<ServiceRequest, Error> {
    let client_ip = req.connection_info().peer_addr()
        .unwrap_or("unknown")
        .to_string();
    
    if !rate_limiter.is_allowed(&client_ip) {
        return Err(actix_web::error::ErrorTooManyRequests("Rate limit exceeded"));
    }
    
    Ok(req)
}
```

## 📊 Monitoring and Metrics

### Prometheus Integration

```rust
use actix_web::{web, App, HttpServer, HttpResponse};
use prometheus::{Registry, Counter, Histogram, Opts};
use tusklang_rust::{parse, Parser};
use std::sync::Arc;

lazy_static::lazy_static! {
    static ref HTTP_REQUESTS_TOTAL: Counter = Counter::new(
        "http_requests_total",
        "Total number of HTTP requests"
    ).unwrap();
    
    static ref HTTP_REQUEST_DURATION: Histogram = Histogram::new(
        "http_request_duration_seconds",
        "HTTP request duration in seconds"
    ).unwrap();
}

async fn metrics_handler() -> HttpResponse {
    use prometheus::Encoder;
    let encoder = prometheus::TextEncoder::new();
    let mut buffer = Vec::new();
    encoder.encode(&prometheus::gather(), &mut buffer).unwrap();
    
    HttpResponse::Ok()
        .content_type("text/plain")
        .body(String::from_utf8(buffer).unwrap())
}

async fn monitored_handler(
    parser: web::Data<Arc<Parser>>,
) -> HttpResponse {
    let start = std::time::Instant::now();
    
    // Your handler logic here
    let result = parser.query("SELECT COUNT(*) FROM users").await;
    
    // Record metrics
    HTTP_REQUESTS_TOTAL.inc();
    HTTP_REQUEST_DURATION.observe(start.elapsed().as_secs_f64());
    
    match result {
        Ok(data) => HttpResponse::Ok().json(data),
        Err(_) => HttpResponse::InternalServerError().finish(),
    }
}

#[actix_web::main]
async fn main() -> std::io::Result<()> {
    let mut parser = Parser::new();
    let config = parser.parse_file("monitoring.tsk").expect("Failed to parse config");
    
    HttpServer::new(move || {
        App::new()
            .app_data(web::Data::new(Arc::new(parser.clone())))
            .route("/metrics", web::get().to(metrics_handler))
            .route("/api/monitored", web::get().to(monitored_handler))
    })
    .bind("0.0.0.0:8080")?
    .run()
    .await
}
```

## 🎯 What You've Learned

1. **Actix-web integration** with middleware and advanced features
2. **Axum integration** with state management and routing
3. **Rocket integration** with request guards and authentication
4. **Warp integration** with filters and handlers
5. **Configuration management** for each framework
6. **Testing strategies** for web framework integration
7. **Performance optimization** with connection pooling and caching
8. **Security integration** with authentication and rate limiting
9. **Monitoring and metrics** with Prometheus integration

## 🚀 Next Steps

1. **Choose your framework** - Select the web framework that best fits your needs
2. **Implement authentication** - Add JWT-based authentication to your routes
3. **Add monitoring** - Integrate Prometheus metrics for production monitoring
4. **Optimize performance** - Implement caching and connection pooling
5. **Deploy to production** - Use the deployment examples for your chosen framework

---

**You now have complete web framework integration mastery with TuskLang Rust!** From Actix-web to Axum, Rocket to Warp - TuskLang gives you the power to build high-performance web applications with zero-copy configuration parsing and real-time database integration. 