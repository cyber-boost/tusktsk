# 🦀 TuskLang Rust Security Implementation

**"We don't bow to any king" - Rust Edition**

Master security implementation for TuskLang Rust applications. From encryption and hashing to access control and input validation - learn how to build secure, production-ready applications that protect your data and users.

## 🔐 Encryption and Hashing

### AES-256-GCM Encryption

```rust
use tusklang_rust::{parse, Parser, security::{Encryption, KeyManager}};
use aes_gcm::{Aes256Gcm, Key, Nonce};
use aes_gcm::aead::{Aead, NewAead};
use rand::Rng;

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    let mut parser = Parser::new();
    
    let encryption = Encryption::new();
    let key_manager = KeyManager::new();
    
    parser.set_encryption(encryption);
    parser.set_key_manager(key_manager);
    
    let tsk_content = r#"
[security]
encryption_enabled: true
encryption_algorithm: "AES-256-GCM"
key_rotation_interval: "30d"
encrypted_password: @encrypt("mysecretpassword", "AES-256-GCM")
encrypted_api_key: @encrypt("abc123def456", "AES-256-GCM")
encrypted_database_url: @encrypt("postgresql://user:pass@localhost/db", "AES-256-GCM")
"#;
    
    let data = parser.parse(tsk_content).await?;
    
    println!("Encryption configured:");
    println!("  Algorithm: {}", data["security"]["encryption_algorithm"]);
    println!("  Key rotation: {}", data["security"]["key_rotation_interval"]);
    println!("  Encrypted password: {}", data["security"]["encrypted_password"]);
    println!("  Encrypted API key: {}", data["security"]["encrypted_api_key"]);
    
    Ok(())
}
```

### Advanced Encryption with Key Rotation

```rust
use tusklang_rust::{parse, Parser, security::{Encryption, KeyManager}};
use std::collections::HashMap;
use std::time::{Duration, Instant};

struct RotatingKeyManager {
    keys: HashMap<String, (Vec<u8>, Instant)>,
    rotation_interval: Duration,
}

impl RotatingKeyManager {
    fn new(rotation_interval: Duration) -> Self {
        Self {
            keys: HashMap::new(),
            rotation_interval,
        }
    }
    
    fn get_key(&mut self, key_id: &str) -> Vec<u8> {
        let now = Instant::now();
        
        if let Some((key, created)) = self.keys.get(key_id) {
            if now.duration_since(*created) < self.rotation_interval {
                return key.clone();
            }
        }
        
        // Generate new key
        let new_key = self.generate_key();
        self.keys.insert(key_id.to_string(), (new_key.clone(), now));
        new_key
    }
    
    fn generate_key(&self) -> Vec<u8> {
        let mut key = vec![0u8; 32]; // 256-bit key
        rand::thread_rng().fill(&mut key);
        key
    }
}

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    let mut parser = Parser::new();
    
    let key_manager = RotatingKeyManager::new(Duration::from_secs(30 * 24 * 60 * 60)); // 30 days
    parser.set_key_manager(Arc::new(Mutex::new(key_manager)));
    
    let tsk_content = r#"
[encryption]
master_key: @key.generate("master", "AES-256")
session_key: @key.rotate("session", "30m")
api_key: @key.rotate("api", "7d")
database_key: @key.rotate("database", "90d")
"#;
    
    let data = parser.parse(tsk_content).await?;
    
    println!("Key rotation configured:");
    println!("  Master key: {}", data["encryption"]["master_key"]);
    println!("  Session key: {}", data["encryption"]["session_key"]);
    println!("  API key: {}", data["encryption"]["api_key"]);
    println!("  Database key: {}", data["encryption"]["database_key"]);
    
    Ok(())
}
```

### Password Hashing with bcrypt

```rust
use tusklang_rust::{parse, Parser, security::Hashing};
use bcrypt::{hash, verify, DEFAULT_COST};

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    let mut parser = Parser::new();
    
    let hashing = Hashing::new();
    parser.set_hashing(hashing);
    
    let tsk_content = r#"
[password_security]
bcrypt_rounds: 12
password_hash: @hash("mysecretpassword", "bcrypt")
sha256_hash: @hash("data", "sha256")
sha512_hash: @hash("data", "sha512")
argon2_hash: @hash("password", "argon2")
"#;
    
    let data = parser.parse(tsk_content).await?;
    
    println!("Password security configured:");
    println!("  BCrypt rounds: {}", data["password_security"]["bcrypt_rounds"]);
    println!("  Password hash: {}", data["password_security"]["password_hash"]);
    println!("  SHA-256 hash: {}", data["password_security"]["sha256_hash"]);
    println!("  SHA-512 hash: {}", data["password_security"]["sha512_hash"]);
    println!("  Argon2 hash: {}", data["password_security"]["argon2_hash"]);
    
    // Verify password
    let password = "mysecretpassword";
    let hash = data["password_security"]["password_hash"].as_str().unwrap();
    
    let is_valid = bcrypt::verify(password, hash)?;
    println!("Password verification: {}", is_valid);
    
    Ok(())
}
```

## 🛡️ Input Validation

### Comprehensive Validation System

```rust
use tusklang_rust::{parse, Parser, validators};
use regex::Regex;
use url::Url;

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    let mut parser = Parser::new();
    
    let tsk_content = r#"
[validation]
email: @validate.email("user@example.com")
url: @validate.url("https://example.com")
ip_address: @validate.ip("192.168.1.1")
port: @validate.range(8080, 1, 65535)
password: @validate.password("StrongPass123!")
phone: @validate.phone("+1-555-123-4567")
credit_card: @validate.credit_card("4111111111111111")
uuid: @validate.uuid("550e8400-e29b-41d4-a716-446655440000")
"#;
    
    // Add custom validators
    parser.add_validator("strong_password", |password: &str| {
        password.len() >= 8 && 
        password.chars().any(|c| c.is_uppercase()) &&
        password.chars().any(|c| c.is_lowercase()) &&
        password.chars().any(|c| c.is_numeric()) &&
        password.chars().any(|c| "!@#$%^&*".contains(c))
    });
    
    parser.add_validator("phone", |phone: &str| {
        let phone_regex = Regex::new(r"^\+?[\d\s\-\(\)]+$").unwrap();
        phone_regex.is_match(phone) && phone.len() >= 10
    });
    
    parser.add_validator("credit_card", |card: &str| {
        // Luhn algorithm for credit card validation
        let digits: Vec<u32> = card.chars()
            .filter(|c| c.is_digit(10))
            .map(|c| c.to_digit(10).unwrap())
            .collect();
        
        if digits.len() < 13 || digits.len() > 19 {
            return false;
        }
        
        let sum: u32 = digits.iter().enumerate()
            .map(|(i, &digit)| {
                if i % 2 == digits.len() % 2 {
                    let doubled = digit * 2;
                    if doubled > 9 { doubled - 9 } else { doubled }
                } else {
                    digit
                }
            })
            .sum();
        
        sum % 10 == 0
    });
    
    parser.add_validator("uuid", |uuid: &str| {
        let uuid_regex = Regex::new(r"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$").unwrap();
        uuid_regex.is_match(uuid.to_lowercase())
    });
    
    let data = parser.parse(tsk_content).await?;
    
    println!("Validation results:");
    println!("  Email: {}", data["validation"]["email"]);
    println!("  URL: {}", data["validation"]["url"]);
    println!("  IP address: {}", data["validation"]["ip_address"]);
    println!("  Port: {}", data["validation"]["port"]);
    println!("  Password: {}", data["validation"]["password"]);
    println!("  Phone: {}", data["validation"]["phone"]);
    println!("  Credit card: {}", data["validation"]["credit_card"]);
    println!("  UUID: {}", data["validation"]["uuid"]);
    
    Ok(())
}
```

### SQL Injection Prevention

```rust
use tusklang_rust::{parse, Parser, adapters::sqlite::SQLiteAdapter};

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    let mut parser = Parser::new();
    
    // Setup test database
    let db = SQLiteAdapter::new(":memory:").await?;
    db.execute(r#"
        CREATE TABLE users (
            id INTEGER PRIMARY KEY,
            name TEXT NOT NULL,
            email TEXT UNIQUE,
            role TEXT DEFAULT 'user'
        );
        
        INSERT INTO users (name, email, role) VALUES 
            ('Alice', 'alice@example.com', 'admin'),
            ('Bob', 'bob@example.com', 'user'),
            ('Charlie', 'charlie@example.com', 'user');
    "#).await?;
    
    parser.set_database_adapter(db);
    
    // Test malicious input
    let malicious_inputs = vec![
        "'; DROP TABLE users; --",
        "' OR '1'='1",
        "'; INSERT INTO users (name, email) VALUES ('hacker', 'hacker@evil.com'); --",
        "admin'--",
        "' UNION SELECT * FROM users--",
    ];
    
    for malicious_input in malicious_inputs {
        let tsk_content = format!(r#"
[users]
user_data: @query("SELECT * FROM users WHERE name = ?", "{}")
"#, malicious_input);
        
        let data = parser.parse(tsk_content).await?;
        
        // Verify table still exists
        let table_check = parser.query("SELECT name FROM sqlite_master WHERE type='table' AND name='users'").await?;
        assert!(!table_check.is_empty(), "Table should still exist after malicious input: {}", malicious_input);
        
        // Verify no unauthorized data was returned
        let user_data: Vec<serde_json::Value> = serde_json::from_value(data["users"]["user_data"].clone()).unwrap();
        assert!(user_data.is_empty(), "No data should be returned for malicious input: {}", malicious_input);
        
        println!("✅ Protected against SQL injection: {}", malicious_input);
    }
    
    Ok(())
}
```

## 🔒 Access Control

### Role-Based Access Control (RBAC)

```rust
use tusklang_rust::{parse, Parser, security::{AccessControl, RBAC}};
use std::collections::HashMap;

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    let mut parser = Parser::new();
    
    let rbac = RBAC::new();
    parser.set_rbac(rbac);
    
    // Setup RBAC rules
    let tsk_content = r#"
[rbac]
roles {
    admin {
        permissions: ["users:read", "users:write", "users:delete", "system:admin"]
        can_manage_users: true
        can_access_admin_panel: true
    }
    
    user {
        permissions: ["users:read", "profile:write"]
        can_manage_users: false
        can_access_admin_panel: false
    }
    
    moderator {
        permissions: ["users:read", "users:write", "content:moderate"]
        can_manage_users: true
        can_access_admin_panel: true
    }
}

[access_control]
user_permissions: @rbac.get_permissions(@request.user_role)
can_access_resource: @rbac.can_access(@request.user_id, @request.resource, @request.action)
can_manage_users: @rbac.has_permission(@request.user_role, "users:write")
can_access_admin: @rbac.has_permission(@request.user_role, "system:admin")
"#;
    
    let data = parser.parse(tsk_content).await?;
    
    // Test different user roles
    let test_cases = vec![
        ("admin", "users:read", true),
        ("admin", "system:admin", true),
        ("user", "users:read", true),
        ("user", "users:write", false),
        ("moderator", "content:moderate", true),
        ("moderator", "system:admin", false),
    ];
    
    for (role, permission, expected) in test_cases {
        let has_permission = parser.execute_fujsen("rbac", "has_permission", &[&role, &permission]).await?;
        assert_eq!(has_permission, expected, "Role {} should {} have permission {}", 
            role, if expected { "" } else { "not" }, permission);
        
        println!("✅ Role '{}' {} permission '{}'", 
            role, if has_permission { "has" } else { "doesn't have" }, permission);
    }
    
    Ok(())
}
```

### Attribute-Based Access Control (ABAC)

```rust
use tusklang_rust::{parse, Parser, security::ABAC};
use serde_json::json;

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    let mut parser = Parser::new();
    
    let abac = ABAC::new();
    parser.set_abac(abac);
    
    let tsk_content = r#"
[abac]
policies {
    time_based_access {
        condition: @request.time >= "09:00" AND @request.time <= "17:00"
        action: "allow"
        resource: "office_resources"
    }
    
    location_based_access {
        condition: @request.location == "office" OR @request.ip_range == "192.168.1.0/24"
        action: "allow"
        resource: "internal_resources"
    }
    
    department_access {
        condition: @request.user_department == @resource.department
        action: "allow"
        resource: "department_data"
    }
}

[access_decisions]
time_restricted: @abac.evaluate(@request.user_attributes, @request.resource_attributes, @request.action)
location_restricted: @abac.evaluate(@request.user_attributes, @request.resource_attributes, @request.action)
department_restricted: @abac.evaluate(@request.user_attributes, @request.resource_attributes, @request.action)
"#;
    
    let data = parser.parse(tsk_content).await?;
    
    // Test ABAC policies
    let test_cases = vec![
        // Time-based access
        (
            json!({"time": "14:30", "location": "office"}),
            json!({"type": "office_resources"}),
            "read",
            true
        ),
        (
            json!({"time": "23:00", "location": "office"}),
            json!({"type": "office_resources"}),
            "read",
            false
        ),
        // Location-based access
        (
            json!({"ip": "192.168.1.100"}),
            json!({"type": "internal_resources"}),
            "read",
            true
        ),
        (
            json!({"ip": "203.0.113.1"}),
            json!({"type": "internal_resources"}),
            "read",
            false
        ),
    ];
    
    for (user_attrs, resource_attrs, action, expected) in test_cases {
        let decision = parser.execute_fujsen("abac", "evaluate", &[&user_attrs, &resource_attrs, &action]).await?;
        assert_eq!(decision, expected, "ABAC decision should be {} for user {:?} accessing resource {:?}", 
            expected, user_attrs, resource_attrs);
        
        println!("✅ ABAC decision: {} for user {:?} accessing resource {:?}", 
            decision, user_attrs, resource_attrs);
    }
    
    Ok(())
}
```

## 🔐 Authentication and Session Management

### JWT Token Management

```rust
use tusklang_rust::{parse, Parser, security::{JWTManager, SessionManager}};
use jsonwebtoken::{encode, decode, Header, Validation, EncodingKey, DecodingKey};
use serde::{Deserialize, Serialize};
use std::time::{Duration, SystemTime, UNIX_EPOCH};

#[derive(Debug, Serialize, Deserialize)]
struct Claims {
    sub: String, // Subject (user ID)
    exp: usize,  // Expiration time
    iat: usize,  // Issued at
    role: String, // User role
}

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    let mut parser = Parser::new();
    
    let jwt_manager = JWTManager::new();
    let session_manager = SessionManager::new();
    
    parser.set_jwt_manager(jwt_manager);
    parser.set_session_manager(session_manager);
    
    let tsk_content = r#"
[authentication]
jwt_secret: @env("JWT_SECRET", "your-secret-key")
jwt_expiration: "24h"
refresh_token_expiration: "7d"
session_timeout: "30m"

[token_management]
access_token: @jwt.generate(@request.user_id, @request.user_role, "24h")
refresh_token: @jwt.generate_refresh(@request.user_id, "7d")
token_validation: @jwt.validate(@request.token)
token_refresh: @jwt.refresh(@request.refresh_token)
"#;
    
    let data = parser.parse(tsk_content).await?;
    
    println!("Authentication configured:");
    println!("  JWT expiration: {}", data["authentication"]["jwt_expiration"]);
    println!("  Refresh token expiration: {}", data["authentication"]["refresh_token_expiration"]);
    println!("  Session timeout: {}", data["authentication"]["session_timeout"]);
    
    // Test JWT token generation and validation
    let user_id = "user123";
    let user_role = "admin";
    
    let access_token = parser.execute_fujsen("token_management", "generate_token", &[&user_id, &user_role]).await?;
    println!("Generated access token: {}", access_token);
    
    let token_validation = parser.execute_fujsen("token_management", "validate_token", &[&access_token]).await?;
    println!("Token validation: {}", token_validation);
    
    Ok(())
}
```

### Session Management

```rust
use tusklang_rust::{parse, Parser, security::SessionManager};
use std::collections::HashMap;
use std::time::{Duration, Instant};
use uuid::Uuid;

struct SecureSessionManager {
    sessions: HashMap<String, (SessionData, Instant)>,
    session_timeout: Duration,
}

#[derive(Clone)]
struct SessionData {
    user_id: String,
    role: String,
    permissions: Vec<String>,
    last_activity: Instant,
}

impl SecureSessionManager {
    fn new(session_timeout: Duration) -> Self {
        Self {
            sessions: HashMap::new(),
            session_timeout,
        }
    }
    
    fn create_session(&mut self, user_id: String, role: String, permissions: Vec<String>) -> String {
        let session_id = Uuid::new_v4().to_string();
        let session_data = SessionData {
            user_id,
            role,
            permissions,
            last_activity: Instant::now(),
        };
        
        self.sessions.insert(session_id.clone(), (session_data, Instant::now()));
        session_id
    }
    
    fn validate_session(&mut self, session_id: &str) -> Option<SessionData> {
        if let Some((session_data, created)) = self.sessions.get(session_id) {
            if Instant::now().duration_since(*created) < self.session_timeout {
                // Update last activity
                let mut updated_session = session_data.clone();
                updated_session.last_activity = Instant::now();
                self.sessions.insert(session_id.to_string(), (updated_session.clone(), *created));
                return Some(updated_session);
            } else {
                // Session expired
                self.sessions.remove(session_id);
            }
        }
        None
    }
    
    fn cleanup_expired_sessions(&mut self) {
        let now = Instant::now();
        self.sessions.retain(|_, (_, created)| {
            now.duration_since(*created) < self.session_timeout
        });
    }
}

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    let mut parser = Parser::new();
    
    let session_manager = SecureSessionManager::new(Duration::from_secs(30 * 60)); // 30 minutes
    parser.set_session_manager(Arc::new(Mutex::new(session_manager)));
    
    let tsk_content = r#"
[session_management]
session_timeout: "30m"
max_sessions_per_user: 5
session_cleanup_interval: "5m"
secure_session_cookies: true
http_only_cookies: true
same_site_policy: "strict"

[session_operations]
create_session: @session.create(@request.user_id, @request.user_role, @request.permissions)
validate_session: @session.validate(@request.session_id)
destroy_session: @session.destroy(@request.session_id)
refresh_session: @session.refresh(@request.session_id)
"#;
    
    let data = parser.parse(tsk_content).await?;
    
    println!("Session management configured:");
    println!("  Session timeout: {}", data["session_management"]["session_timeout"]);
    println!("  Max sessions per user: {}", data["session_management"]["max_sessions_per_user"]);
    println!("  Cleanup interval: {}", data["session_management"]["session_cleanup_interval"]);
    
    Ok(())
}
```

## 🛡️ Security Headers and HTTPS

### Security Headers Implementation

```rust
use actix_web::{get, HttpResponse, Result, middleware};
use tusklang_rust::{parse, Parser};

#[get("/secure")]
async fn secure_endpoint(parser: web::Data<Arc<Parser>>) -> Result<HttpResponse> {
    let security_config = parser.get("security.headers").expect("Security config not found");
    
    let mut response = HttpResponse::Ok()
        .json(json!({
            "message": "Secure endpoint",
            "timestamp": chrono::Utc::now().to_rfc3339()
        }));
    
    // Add security headers
    response.headers_mut().insert(
        "X-Content-Type-Options",
        "nosniff".parse().unwrap()
    );
    
    response.headers_mut().insert(
        "X-Frame-Options",
        "DENY".parse().unwrap()
    );
    
    response.headers_mut().insert(
        "X-XSS-Protection",
        "1; mode=block".parse().unwrap()
    );
    
    response.headers_mut().insert(
        "Strict-Transport-Security",
        "max-age=31536000; includeSubDomains".parse().unwrap()
    );
    
    response.headers_mut().insert(
        "Content-Security-Policy",
        "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'".parse().unwrap()
    );
    
    response.headers_mut().insert(
        "Referrer-Policy",
        "strict-origin-when-cross-origin".parse().unwrap()
    );
    
    Ok(response)
}

// Security middleware
pub fn security_middleware() -> impl Transform<T, Service = S> {
    middleware::DefaultHeaders::new()
        .add(("X-Content-Type-Options", "nosniff"))
        .add(("X-Frame-Options", "DENY"))
        .add(("X-XSS-Protection", "1; mode=block"))
        .add(("Strict-Transport-Security", "max-age=31536000; includeSubDomains"))
        .add(("Content-Security-Policy", "default-src 'self'"))
        .add(("Referrer-Policy", "strict-origin-when-cross-origin"))
}

#[actix_web::main]
async fn main() -> std::io::Result<()> {
    let mut parser = Parser::new();
    let config = parser.parse_file("security.tsk").expect("Failed to parse config");
    
    HttpServer::new(move || {
        App::new()
            .app_data(web::Data::new(Arc::new(parser.clone())))
            .wrap(security_middleware())
            .service(secure_endpoint)
    })
    .bind("0.0.0.0:8080")?
    .run()
    .await
}
```

### HTTPS Configuration

```rust
use actix_web::{get, HttpResponse, Result};
use rustls::{ServerConfig, PrivateKey, Certificate};
use std::fs::File;
use std::io::BufReader;

#[actix_web::main]
async fn main() -> std::io::Result<()> {
    let mut parser = Parser::new();
    let config = parser.parse_file("https.tsk").expect("Failed to parse config");
    
    // Load SSL certificate and private key
    let cert_file = File::open("cert.pem").expect("Failed to open certificate");
    let key_file = File::open("key.pem").expect("Failed to open private key");
    
    let cert_chain = rustls_pemfile::certs(&mut BufReader::new(cert_file))
        .expect("Failed to parse certificate");
    let key_der = rustls_pemfile::pkcs8_private_keys(&mut BufReader::new(key_file))
        .expect("Failed to parse private key");
    
    let cert = Certificate(cert_chain[0].clone());
    let key = PrivateKey(key_der[0].clone());
    
    let tls_config = ServerConfig::builder()
        .with_safe_defaults()
        .with_no_client_auth()
        .with_single_cert(vec![cert], key)
        .expect("Failed to build TLS config");
    
    let server_config = &config["server"];
    let host = server_config["host"].as_str().unwrap();
    let port = server_config["port"].as_u64().unwrap();
    
    HttpServer::new(move || {
        App::new()
            .app_data(web::Data::new(Arc::new(parser.clone())))
            .wrap(security_middleware())
            .service(secure_endpoint)
    })
    .bind_rustls(format!("{}:{}", host, port), tls_config)?
    .run()
    .await
}
```

## 🔍 Security Monitoring and Logging

### Security Event Logging

```rust
use tusklang_rust::{parse, Parser, security::{SecurityLogger, ThreatDetector}};
use serde_json::json;
use std::sync::Arc;
use tokio::sync::mpsc;

struct SecurityEvent {
    timestamp: chrono::DateTime<chrono::Utc>,
    event_type: String,
    user_id: Option<String>,
    ip_address: String,
    action: String,
    resource: String,
    success: bool,
    details: serde_json::Value,
}

struct SecurityLogger {
    tx: mpsc::Sender<SecurityEvent>,
}

impl SecurityLogger {
    fn new() -> (Self, mpsc::Receiver<SecurityEvent>) {
        let (tx, rx) = mpsc::channel(1000);
        (Self { tx }, rx)
    }
    
    async fn log_event(&self, event: SecurityEvent) {
        let _ = self.tx.send(event).await;
    }
    
    async fn log_failed_login(&self, user_id: &str, ip_address: &str, reason: &str) {
        let event = SecurityEvent {
            timestamp: chrono::Utc::now(),
            event_type: "failed_login".to_string(),
            user_id: Some(user_id.to_string()),
            ip_address: ip_address.to_string(),
            action: "login".to_string(),
            resource: "authentication".to_string(),
            success: false,
            details: json!({
                "reason": reason,
                "severity": "medium"
            }),
        };
        
        self.log_event(event).await;
    }
    
    async fn log_suspicious_activity(&self, ip_address: &str, action: &str, details: serde_json::Value) {
        let event = SecurityEvent {
            timestamp: chrono::Utc::now(),
            event_type: "suspicious_activity".to_string(),
            user_id: None,
            ip_address: ip_address.to_string(),
            action: action.to_string(),
            resource: "system".to_string(),
            success: false,
            details,
        };
        
        self.log_event(event).await;
    }
}

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    let mut parser = Parser::new();
    
    let (security_logger, mut rx) = SecurityLogger::new();
    parser.set_security_logger(Arc::new(security_logger));
    
    let tsk_content = r#"
[security_monitoring]
enabled: true
log_level: "info"
alert_threshold: 5
block_duration: "1h"
monitored_events: ["failed_login", "suspicious_activity", "sql_injection", "xss_attempt"]

[threat_detection]
rate_limiting_enabled: true
max_requests_per_minute: 100
ip_blacklist_enabled: true
user_blacklist_enabled: true
"#;
    
    let data = parser.parse(tsk_content).await?;
    
    println!("Security monitoring configured:");
    println!("  Enabled: {}", data["security_monitoring"]["enabled"]);
    println!("  Log level: {}", data["security_monitoring"]["log_level"]);
    println!("  Alert threshold: {}", data["security_monitoring"]["alert_threshold"]);
    
    // Start security event processing
    tokio::spawn(async move {
        while let Some(event) = rx.recv().await {
            println!("Security Event: {:?}", event);
            
            // Process security events
            match event.event_type.as_str() {
                "failed_login" => {
                    println!("Failed login attempt from {} for user {:?}", 
                        event.ip_address, event.user_id);
                },
                "suspicious_activity" => {
                    println!("Suspicious activity detected from {}: {}", 
                        event.ip_address, event.action);
                },
                _ => {
                    println!("Other security event: {}", event.event_type);
                }
            }
        }
    });
    
    Ok(())
}
```

## 🎯 What You've Learned

1. **Encryption and hashing** - AES-256-GCM encryption, bcrypt password hashing, key rotation
2. **Input validation** - Comprehensive validation system with custom validators
3. **SQL injection prevention** - Parameterized queries and input sanitization
4. **Access control** - RBAC and ABAC implementation
5. **Authentication and session management** - JWT tokens and secure session handling
6. **Security headers and HTTPS** - Security headers implementation and HTTPS configuration
7. **Security monitoring and logging** - Security event logging and threat detection

## 🚀 Next Steps

1. **Implement security measures** - Apply the security techniques to your applications
2. **Set up monitoring** - Configure security monitoring and alerting
3. **Regular security audits** - Conduct regular security assessments
4. **Stay updated** - Keep up with the latest security best practices
5. **Penetration testing** - Perform regular penetration testing

---

**You now have complete security implementation mastery with TuskLang Rust!** From encryption and hashing to access control and monitoring - TuskLang gives you the tools to build secure, production-ready applications that protect your data and users. 