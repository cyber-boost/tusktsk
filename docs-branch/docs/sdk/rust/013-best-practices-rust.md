# 🦀 TuskLang Rust Best Practices

**"We don't bow to any king" - Rust Edition**

Master best practices for TuskLang Rust applications. From code organization to performance optimization, from security practices to deployment strategies - learn how to build maintainable, scalable, and production-ready applications.

## 🏗️ Code Organization

### Project Structure

```rust
// Recommended project structure for TuskLang Rust applications
use tusklang_rust::{parse, Parser};

// src/
// ├── main.rs              // Application entry point
// ├── lib.rs               // Library exports
// ├── config/              // Configuration management
// │   ├── mod.rs
// │   ├── parser.rs        // TSK parser configuration
// │   └── validation.rs    // Configuration validation
// ├── database/            // Database layer
// │   ├── mod.rs
// │   ├── connection.rs    // Connection management
// │   └── migrations.rs    // Database migrations
// ├── security/            // Security layer
// │   ├── mod.rs
// │   ├── auth.rs          // Authentication
// │   ├── encryption.rs    // Encryption utilities
// │   └── validation.rs    // Input validation
// ├── api/                 // API layer
// │   ├── mod.rs
// │   ├── routes.rs        // Route definitions
// │   ├── handlers.rs      // Request handlers
// │   └── middleware.rs    // Middleware
// ├── services/            // Business logic
// │   ├── mod.rs
// │   ├── user_service.rs  // User management
// │   └── data_service.rs  // Data processing
// ├── utils/               // Utility functions
// │   ├── mod.rs
// │   ├── error.rs         // Error handling
// │   └── logging.rs       // Logging utilities
// └── tests/               // Integration tests
//     ├── mod.rs
//     ├── api_tests.rs
//     └── integration_tests.rs

// config/
// ├── development.tsk      // Development configuration
// ├── staging.tsk          // Staging configuration
// ├── production.tsk       // Production configuration
// └── security.tsk         // Security configuration

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    println!("🏗️  Best Practices: Code Organization");
    
    // 1. Modular configuration management
    let mut parser = Parser::new();
    
    // Load environment-specific configuration
    let environment = std::env::var("APP_ENV").unwrap_or_else(|_| "development".to_string());
    let config_file = format!("config/{}.tsk", environment);
    
    let config = parser.parse_file(&config_file).await?;
    
    // 2. Structured configuration validation
    validate_configuration(&config).await?;
    
    // 3. Initialize services with configuration
    let services = initialize_services(&config).await?;
    
    // 4. Start application with proper error handling
    start_application(services).await?;
    
    Ok(())
}

async fn validate_configuration(config: &serde_json::Value) -> Result<(), Box<dyn std::error::Error>> {
    println!("Validating configuration...");
    
    // Validate required sections
    let required_sections = vec!["server", "database", "security", "logging"];
    
    for section in required_sections {
        if !config.get(section).is_some() {
            return Err(format!("Missing required configuration section: {}", section).into());
        }
    }
    
    // Validate server configuration
    let server = &config["server"];
    if server["port"].as_u64().unwrap_or(0) == 0 {
        return Err("Invalid server port configuration".into());
    }
    
    // Validate database configuration
    let database = &config["database"];
    if database["host"].as_str().unwrap_or("").is_empty() {
        return Err("Database host not configured".into());
    }
    
    println!("✅ Configuration validation passed");
    Ok(())
}

async fn initialize_services(config: &serde_json::Value) -> Result<Services, Box<dyn std::error::Error>> {
    println!("Initializing services...");
    
    // Initialize database connection
    let database = initialize_database(&config["database"]).await?;
    
    // Initialize security services
    let security = initialize_security(&config["security"]).await?;
    
    // Initialize logging
    let logging = initialize_logging(&config["logging"]).await?;
    
    Ok(Services {
        database,
        security,
        logging,
    })
}

async fn start_application(services: Services) -> Result<(), Box<dyn std::error::Error>> {
    println!("Starting application...");
    
    // Start HTTP server
    let server_config = services.config["server"].clone();
    let host = server_config["host"].as_str().unwrap_or("0.0.0.0");
    let port = server_config["port"].as_u64().unwrap_or(8080);
    
    println!("🚀 Server starting on {}:{}", host, port);
    
    Ok(())
}

struct Services {
    database: DatabaseService,
    security: SecurityService,
    logging: LoggingService,
}
```

### Configuration Management

```rust
use tusklang_rust::{parse, Parser};
use std::sync::Arc;
use tokio::sync::RwLock;

// Configuration management with type safety
#[derive(Debug, Clone)]
struct AppConfig {
    server: ServerConfig,
    database: DatabaseConfig,
    security: SecurityConfig,
    logging: LoggingConfig,
}

#[derive(Debug, Clone)]
struct ServerConfig {
    host: String,
    port: u16,
    workers: usize,
}

#[derive(Debug, Clone)]
struct DatabaseConfig {
    host: String,
    port: u16,
    database: String,
    username: String,
    password: String,
    max_connections: usize,
}

#[derive(Debug, Clone)]
struct SecurityConfig {
    jwt_secret: String,
    bcrypt_rounds: u32,
    session_timeout: Duration,
}

#[derive(Debug, Clone)]
struct LoggingConfig {
    level: String,
    format: String,
    file: Option<String>,
}

struct ConfigurationManager {
    config: Arc<RwLock<AppConfig>>,
    parser: Parser,
}

impl ConfigurationManager {
    async fn new() -> Result<Self, Box<dyn std::error::Error>> {
        let mut parser = Parser::new();
        
        // Load base configuration
        let base_config = parser.parse_file("config/base.tsk").await?;
        
        // Load environment-specific configuration
        let environment = std::env::var("APP_ENV").unwrap_or_else(|_| "development".to_string());
        let env_config = parser.parse_file(&format!("config/{}.tsk", environment)).await?;
        
        // Merge configurations (environment overrides base)
        let merged_config = merge_configurations(base_config, env_config);
        
        // Convert to strongly-typed configuration
        let app_config = Self::parse_config(merged_config)?;
        
        Ok(Self {
            config: Arc::new(RwLock::new(app_config)),
            parser,
        })
    }
    
    fn parse_config(config: serde_json::Value) -> Result<AppConfig, Box<dyn std::error::Error>> {
        let server = ServerConfig {
            host: config["server"]["host"].as_str().unwrap_or("0.0.0.0").to_string(),
            port: config["server"]["port"].as_u64().unwrap_or(8080) as u16,
            workers: config["server"]["workers"].as_u64().unwrap_or(4) as usize,
        };
        
        let database = DatabaseConfig {
            host: config["database"]["host"].as_str().unwrap_or("localhost").to_string(),
            port: config["database"]["port"].as_u64().unwrap_or(5432) as u16,
            database: config["database"]["database"].as_str().unwrap_or("app").to_string(),
            username: config["database"]["username"].as_str().unwrap_or("postgres").to_string(),
            password: config["database"]["password"].as_str().unwrap_or("").to_string(),
            max_connections: config["database"]["max_connections"].as_u64().unwrap_or(10) as usize,
        };
        
        let security = SecurityConfig {
            jwt_secret: config["security"]["jwt_secret"].as_str().unwrap_or("").to_string(),
            bcrypt_rounds: config["security"]["bcrypt_rounds"].as_u64().unwrap_or(12) as u32,
            session_timeout: Duration::from_secs(
                config["security"]["session_timeout_seconds"].as_u64().unwrap_or(3600)
            ),
        };
        
        let logging = LoggingConfig {
            level: config["logging"]["level"].as_str().unwrap_or("info").to_string(),
            format: config["logging"]["format"].as_str().unwrap_or("json").to_string(),
            file: config["logging"]["file"].as_str().map(|s| s.to_string()),
        };
        
        Ok(AppConfig {
            server,
            database,
            security,
            logging,
        })
    }
    
    async fn get_config(&self) -> AppConfig {
        self.config.read().await.clone()
    }
    
    async fn reload_config(&mut self) -> Result<(), Box<dyn std::error::Error>> {
        let new_config = Self::new().await?;
        *self.config.write().await = new_config.config.read().await.clone();
        Ok(())
    }
}

fn merge_configurations(base: serde_json::Value, env: serde_json::Value) -> serde_json::Value {
    // Simple merge implementation - in practice, use a proper JSON merge library
    let mut merged = base.clone();
    
    if let (Some(base_obj), Some(env_obj)) = (merged.as_object_mut(), env.as_object()) {
        for (key, value) in env_obj {
            base_obj.insert(key.clone(), value.clone());
        }
    }
    
    merged
}
```

## 🔒 Security Best Practices

### Input Validation and Sanitization

```rust
use tusklang_rust::{parse, Parser, validators};
use regex::Regex;
use std::collections::HashMap;

#[tokio::main]
async fn security_best_practices() -> Result<(), Box<dyn std::error::Error>> {
    println!("🔒 Security Best Practices");
    
    let mut parser = Parser::new();
    
    // 1. Comprehensive input validation
    println!("1. Setting up comprehensive input validation...");
    
    // Add custom validators
    parser.add_validator("strong_password", |password: &str| {
        // Password strength requirements
        password.len() >= 8 &&
        password.chars().any(|c| c.is_uppercase()) &&
        password.chars().any(|c| c.is_lowercase()) &&
        password.chars().any(|c| c.is_numeric()) &&
        password.chars().any(|c| "!@#$%^&*".contains(c)) &&
        !password.contains("password") &&
        !password.contains("123") &&
        !password.contains("qwerty")
    });
    
    parser.add_validator("safe_filename", |filename: &str| {
        // Prevent path traversal and unsafe characters
        !filename.contains("..") &&
        !filename.contains("/") &&
        !filename.contains("\\") &&
        !filename.contains(":") &&
        !filename.contains("*") &&
        !filename.contains("?") &&
        !filename.contains("\"") &&
        !filename.contains("<") &&
        !filename.contains(">") &&
        !filename.contains("|")
    });
    
    parser.add_validator("sql_safe", |input: &str| {
        // Basic SQL injection prevention
        let dangerous_patterns = vec![
            "DROP TABLE",
            "DELETE FROM",
            "INSERT INTO",
            "UPDATE",
            "ALTER TABLE",
            "CREATE TABLE",
            "EXEC",
            "EXECUTE",
            "UNION",
            "SELECT *",
        ];
        
        let input_upper = input.to_uppercase();
        !dangerous_patterns.iter().any(|pattern| input_upper.contains(pattern))
    });
    
    // 2. Configuration validation
    let tsk_content = r#"
[security]
input_validation: true
sql_injection_protection: true
xss_protection: true
csrf_protection: true

[validation_rules]
password_min_length: 8
password_require_uppercase: true
password_require_lowercase: true
password_require_numbers: true
password_require_special: true
username_min_length: 3
username_max_length: 50
email_validation: true
"#;
    
    let config = parser.parse(tsk_content).await?;
    
    // 3. Secure configuration handling
    let security_config = SecurityConfig {
        input_validation: config["security"]["input_validation"].as_bool().unwrap_or(true),
        sql_injection_protection: config["security"]["sql_injection_protection"].as_bool().unwrap_or(true),
        xss_protection: config["security"]["xss_protection"].as_bool().unwrap_or(true),
        csrf_protection: config["security"]["csrf_protection"].as_bool().unwrap_or(true),
    };
    
    println!("✅ Security configuration loaded");
    
    // 4. Input sanitization example
    let user_inputs = vec![
        "normal_input",
        "../../../etc/passwd",  // Path traversal attempt
        "'; DROP TABLE users; --",  // SQL injection attempt
        "<script>alert('xss')</script>",  // XSS attempt
        "normal@email.com",
        "invalid-email",
    ];
    
    for input in user_inputs {
        let sanitized = sanitize_input(input, &security_config);
        println!("Input: '{}' -> Sanitized: '{}'", input, sanitized);
    }
    
    Ok(())
}

#[derive(Debug)]
struct SecurityConfig {
    input_validation: bool,
    sql_injection_protection: bool,
    xss_protection: bool,
    csrf_protection: bool,
}

fn sanitize_input(input: &str, config: &SecurityConfig) -> String {
    let mut sanitized = input.to_string();
    
    if config.sql_injection_protection {
        // Remove SQL injection patterns
        let sql_patterns = vec![
            ("'", "''"),
            (";", ""),
            ("--", ""),
            ("/*", ""),
            ("*/", ""),
        ];
        
        for (pattern, replacement) in sql_patterns {
            sanitized = sanitized.replace(pattern, replacement);
        }
    }
    
    if config.xss_protection {
        // Remove XSS patterns
        let xss_patterns = vec![
            ("<script>", ""),
            ("</script>", ""),
            ("javascript:", ""),
            ("onload=", ""),
            ("onerror=", ""),
        ];
        
        for (pattern, replacement) in xss_patterns {
            sanitized = sanitized.replace(pattern, replacement);
        }
    }
    
    sanitized
}
```

### Authentication and Authorization

```rust
use tusklang_rust::{parse, Parser, security::{JWTManager, RBAC}};
use jsonwebtoken::{encode, decode, Header, Validation, EncodingKey, DecodingKey};
use serde::{Deserialize, Serialize};
use bcrypt::{hash, verify, DEFAULT_COST};
use std::collections::HashMap;

#[derive(Debug, Serialize, Deserialize)]
struct Claims {
    sub: String,  // Subject (user ID)
    exp: usize,   // Expiration time
    iat: usize,   // Issued at
    role: String, // User role
    permissions: Vec<String>, // User permissions
}

#[derive(Debug)]
struct User {
    id: String,
    username: String,
    email: String,
    password_hash: String,
    role: String,
    permissions: Vec<String>,
    is_active: bool,
    created_at: chrono::DateTime<chrono::Utc>,
    last_login: Option<chrono::DateTime<chrono::Utc>>,
}

struct AuthService {
    jwt_manager: JWTManager,
    rbac: RBAC,
    users: HashMap<String, User>,
}

impl AuthService {
    fn new(jwt_secret: &str) -> Self {
        let jwt_manager = JWTManager::new();
        let rbac = RBAC::new();
        
        Self {
            jwt_manager,
            rbac,
            users: HashMap::new(),
        }
    }
    
    async fn register_user(&mut self, username: &str, email: &str, password: &str) -> Result<User, Box<dyn std::error::Error>> {
        // Validate input
        if username.len() < 3 || username.len() > 50 {
            return Err("Username must be between 3 and 50 characters".into());
        }
        
        if !email.contains("@") {
            return Err("Invalid email format".into());
        }
        
        if password.len() < 8 {
            return Err("Password must be at least 8 characters".into());
        }
        
        // Check if user already exists
        if self.users.values().any(|u| u.username == username || u.email == email) {
            return Err("User already exists".into());
        }
        
        // Hash password
        let password_hash = hash(password, DEFAULT_COST)?;
        
        // Create user
        let user = User {
            id: uuid::Uuid::new_v4().to_string(),
            username: username.to_string(),
            email: email.to_string(),
            password_hash,
            role: "user".to_string(),
            permissions: vec!["read".to_string()],
            is_active: true,
            created_at: chrono::Utc::now(),
            last_login: None,
        };
        
        self.users.insert(user.id.clone(), user.clone());
        
        Ok(user)
    }
    
    async fn authenticate_user(&mut self, username: &str, password: &str) -> Result<String, Box<dyn std::error::Error>> {
        // Find user
        let user = self.users.values()
            .find(|u| u.username == username && u.is_active)
            .ok_or("Invalid credentials")?;
        
        // Verify password
        if !verify(password, &user.password_hash)? {
            return Err("Invalid credentials".into());
        }
        
        // Update last login
        if let Some(user_mut) = self.users.get_mut(&user.id) {
            user_mut.last_login = Some(chrono::Utc::now());
        }
        
        // Generate JWT token
        let claims = Claims {
            sub: user.id.clone(),
            exp: (chrono::Utc::now() + chrono::Duration::hours(24)).timestamp() as usize,
            iat: chrono::Utc::now().timestamp() as usize,
            role: user.role.clone(),
            permissions: user.permissions.clone(),
        };
        
        let token = encode(&Header::default(), &claims, &EncodingKey::from_secret("your-secret".as_ref()))?;
        
        Ok(token)
    }
    
    async fn verify_token(&self, token: &str) -> Result<Claims, Box<dyn std::error::Error>> {
        let token_data = decode::<Claims>(
            token,
            &DecodingKey::from_secret("your-secret".as_ref()),
            &Validation::default(),
        )?;
        
        Ok(token_data.claims)
    }
    
    async fn has_permission(&self, user_id: &str, permission: &str) -> bool {
        if let Some(user) = self.users.get(user_id) {
            user.permissions.contains(&permission.to_string())
        } else {
            false
        }
    }
    
    async fn require_permission(&self, user_id: &str, permission: &str) -> Result<(), Box<dyn std::error::Error>> {
        if !self.has_permission(user_id, permission).await {
            return Err("Insufficient permissions".into());
        }
        Ok(())
    }
}

#[tokio::main]
async fn authentication_best_practices() -> Result<(), Box<dyn std::error::Error>> {
    println!("🔐 Authentication Best Practices");
    
    let mut auth_service = AuthService::new("your-secret-key");
    
    // 1. User registration with validation
    println!("1. User registration...");
    
    match auth_service.register_user("alice", "alice@example.com", "SecurePass123!").await {
        Ok(user) => println!("✅ User registered: {}", user.username),
        Err(e) => println!("❌ Registration failed: {}", e),
    }
    
    // 2. User authentication
    println!("2. User authentication...");
    
    match auth_service.authenticate_user("alice", "SecurePass123!").await {
        Ok(token) => {
            println!("✅ Authentication successful");
            
            // 3. Token verification
            match auth_service.verify_token(&token).await {
                Ok(claims) => println!("✅ Token verified for user: {}", claims.sub),
                Err(e) => println!("❌ Token verification failed: {}", e),
            }
        }
        Err(e) => println!("❌ Authentication failed: {}", e),
    }
    
    // 4. Permission checking
    println!("4. Permission checking...");
    
    let user_id = "alice";
    let permission = "read";
    
    if auth_service.has_permission(user_id, permission).await {
        println!("✅ User has '{}' permission", permission);
    } else {
        println!("❌ User lacks '{}' permission", permission);
    }
    
    Ok(())
}
```

## ⚡ Performance Best Practices

### Caching Strategies

```rust
use tusklang_rust::{parse, Parser, cache::{MemoryCache, RedisCache, TieredCache}};
use std::collections::HashMap;
use std::sync::Arc;
use tokio::sync::RwLock;
use std::time::{Duration, Instant};

struct CacheManager {
    l1_cache: Arc<RwLock<HashMap<String, (String, Instant)>>>,
    l2_cache: RedisCache,
    cache_config: CacheConfig,
}

#[derive(Debug, Clone)]
struct CacheConfig {
    l1_ttl: Duration,
    l2_ttl: Duration,
    max_l1_size: usize,
    enable_compression: bool,
}

impl CacheManager {
    async fn new(config: CacheConfig) -> Result<Self, Box<dyn std::error::Error>> {
        let l2_cache = RedisCache::new(RedisConfig {
            host: "localhost".to_string(),
            port: 6379,
            db: 0,
        }).await?;
        
        Ok(Self {
            l1_cache: Arc::new(RwLock::new(HashMap::new())),
            l2_cache,
            cache_config: config,
        })
    }
    
    async fn get(&self, key: &str) -> Option<String> {
        // Check L1 cache first
        {
            let l1 = self.l1_cache.read().await;
            if let Some((value, timestamp)) = l1.get(key) {
                if Instant::now().duration_since(*timestamp) < self.cache_config.l1_ttl {
                    return Some(value.clone());
                }
            }
        }
        
        // Check L2 cache
        match self.l2_cache.get(key).await {
            Ok(Some(value)) => {
                // Store in L1 cache
                let mut l1 = self.l1_cache.write().await;
                l1.insert(key.to_string(), (value.clone(), Instant::now()));
                
                // Enforce L1 cache size limit
                if l1.len() > self.cache_config.max_l1_size {
                    let oldest_key = l1.iter()
                        .min_by_key(|(_, (_, timestamp))| timestamp)
                        .map(|(k, _)| k.clone());
                    
                    if let Some(key_to_remove) = oldest_key {
                        l1.remove(&key_to_remove);
                    }
                }
                
                Some(value)
            }
            _ => None,
        }
    }
    
    async fn set(&self, key: &str, value: &str) -> Result<(), Box<dyn std::error::Error>> {
        // Store in L1 cache
        {
            let mut l1 = self.l1_cache.write().await;
            l1.insert(key.to_string(), (value.to_string(), Instant::now()));
        }
        
        // Store in L2 cache
        self.l2_cache.set(key, value, self.cache_config.l2_ttl).await?;
        
        Ok(())
    }
    
    async fn invalidate(&self, key: &str) -> Result<(), Box<dyn std::error::Error>> {
        // Remove from L1 cache
        {
            let mut l1 = self.l1_cache.write().await;
            l1.remove(key);
        }
        
        // Remove from L2 cache
        self.l2_cache.delete(key).await?;
        
        Ok(())
    }
    
    async fn get_stats(&self) -> CacheStats {
        let l1_size = self.l1_cache.read().await.len();
        let l2_stats = self.l2_cache.get_stats().await.unwrap_or_default();
        
        CacheStats {
            l1_size,
            l2_size: l2_stats.keys,
            l1_hits: 0, // Would track in real implementation
            l2_hits: l2_stats.hits,
        }
    }
}

#[derive(Debug)]
struct CacheStats {
    l1_size: usize,
    l2_size: usize,
    l1_hits: usize,
    l2_hits: usize,
}

#[tokio::main]
async fn caching_best_practices() -> Result<(), Box<dyn std::error::Error>> {
    println!("⚡ Caching Best Practices");
    
    let cache_config = CacheConfig {
        l1_ttl: Duration::from_secs(300),  // 5 minutes
        l2_ttl: Duration::from_secs(3600), // 1 hour
        max_l1_size: 1000,
        enable_compression: true,
    };
    
    let cache_manager = CacheManager::new(cache_config).await?;
    
    // 1. Cache frequently accessed data
    println!("1. Caching frequently accessed data...");
    
    let cache_keys = vec![
        "user_profile_123",
        "app_config",
        "database_connection_pool",
        "api_rate_limits",
    ];
    
    for key in cache_keys {
        let value = format!("cached_value_for_{}", key);
        cache_manager.set(key, &value).await?;
        println!("✅ Cached: {}", key);
    }
    
    // 2. Cache with appropriate TTL
    println!("2. Setting appropriate cache TTL...");
    
    let ttl_configs = vec![
        ("user_session", Duration::from_secs(1800)),  // 30 minutes
        ("static_content", Duration::from_secs(86400)), // 24 hours
        ("api_response", Duration::from_secs(300)),   // 5 minutes
    ];
    
    for (key, ttl) in ttl_configs {
        let value = format!("ttl_value_for_{}", key);
        // In real implementation, you'd set different TTLs
        cache_manager.set(key, &value).await?;
        println!("✅ Cached with TTL: {} ({:?})", key, ttl);
    }
    
    // 3. Cache invalidation strategies
    println!("3. Cache invalidation strategies...");
    
    // Invalidate specific keys
    cache_manager.invalidate("user_profile_123").await?;
    println!("✅ Invalidated: user_profile_123");
    
    // 4. Cache statistics
    println!("4. Cache statistics...");
    
    let stats = cache_manager.get_stats().await;
    println!("L1 Cache: {} items", stats.l1_size);
    println!("L2 Cache: {} items", stats.l2_size);
    println!("L2 Hits: {}", stats.l2_hits);
    
    Ok(())
}
```

### Database Optimization

```rust
use tusklang_rust::{parse, Parser, adapters::postgresql::{PostgreSQLAdapter, PostgreSQLConfig, PoolConfig}};
use std::time::Duration;

#[tokio::main]
async fn database_optimization_best_practices() -> Result<(), Box<dyn std::error::Error>> {
    println!("🗄️  Database Optimization Best Practices");
    
    let mut parser = Parser::new();
    
    // 1. Connection pooling configuration
    println!("1. Setting up connection pooling...");
    
    let pool_config = PoolConfig {
        max_open_conns: 50,
        max_idle_conns: 20,
        conn_max_lifetime: Duration::from_secs(300),
        conn_max_idle_time: Duration::from_secs(60),
    };
    
    let db_config = PostgreSQLConfig {
        host: "localhost".to_string(),
        port: 5432,
        database: "optimized_db".to_string(),
        user: "postgres".to_string(),
        password: "secret".to_string(),
        ssl_mode: "disable".to_string(),
    };
    
    let postgres = PostgreSQLAdapter::with_pool(db_config, pool_config).await?;
    parser.set_database_adapter(postgres);
    
    // 2. Query optimization
    println!("2. Query optimization...");
    
    let optimization_tsk = r#"
[database_optimization]
use_indexes: true
prepared_statements: true
query_timeout: "30s"
max_query_time: "60s"
connection_pool_size: 50
statement_cache_size: 1000
"#;
    
    let config = parser.parse(optimization_tsk).await?;
    
    // 3. Index creation
    println!("3. Creating database indexes...");
    
    let index_queries = vec![
        "CREATE INDEX IF NOT EXISTS idx_users_email ON users(email)",
        "CREATE INDEX IF NOT EXISTS idx_users_created_at ON users(created_at)",
        "CREATE INDEX IF NOT EXISTS idx_orders_user_id ON orders(user_id)",
        "CREATE INDEX IF NOT EXISTS idx_orders_status ON orders(status)",
        "CREATE INDEX IF NOT EXISTS idx_logs_timestamp ON logs(timestamp)",
    ];
    
    for query in index_queries {
        match parser.query(query).await {
            Ok(_) => println!("✅ Index created successfully"),
            Err(e) => println!("⚠️  Index creation failed: {}", e),
        }
    }
    
    // 4. Prepared statements
    println!("4. Using prepared statements...");
    
    let prepared_queries = vec![
        ("get_user_by_email", "SELECT * FROM users WHERE email = ?"),
        ("get_user_orders", "SELECT * FROM orders WHERE user_id = ? AND status = ?"),
        ("update_user_status", "UPDATE users SET status = ? WHERE id = ?"),
    ];
    
    for (name, query) in prepared_queries {
        match parser.prepare_statement(name, query).await {
            Ok(_) => println!("✅ Prepared statement: {}", name),
            Err(e) => println!("❌ Failed to prepare statement {}: {}", name, e),
        }
    }
    
    // 5. Query monitoring
    println!("5. Setting up query monitoring...");
    
    let monitoring_tsk = r#"
[query_monitoring]
slow_query_threshold: "100ms"
log_slow_queries: true
log_all_queries: false
query_analytics: true
"#;
    
    let monitoring_config = parser.parse(monitoring_tsk).await?;
    
    let slow_query_threshold = monitoring_config["query_monitoring"]["slow_query_threshold"]
        .as_str()
        .unwrap_or("100ms");
    
    println!("✅ Query monitoring configured with threshold: {}", slow_query_threshold);
    
    Ok(())
}
```

## 🚀 Deployment Best Practices

### Containerization

```rust
use std::process::Command;
use std::path::Path;

#[tokio::main]
async fn containerization_best_practices() -> Result<(), Box<dyn std::error::Error>> {
    println!("🐳 Containerization Best Practices");
    
    // 1. Multi-stage Dockerfile
    println!("1. Creating optimized Dockerfile...");
    
    let dockerfile_content = r#"
# Multi-stage build for optimized production image
FROM rust:1.70-alpine AS builder

# Install build dependencies
RUN apk add --no-cache musl-dev openssl-dev pkgconfig

WORKDIR /app

# Install TuskLang CLI
RUN cargo install tusklang-cli

# Copy dependency files
COPY Cargo.toml Cargo.lock ./

# Create dummy main.rs to build dependencies
RUN mkdir src && echo "fn main() {}" > src/main.rs
RUN cargo build --release
RUN rm -rf src

# Copy source code
COPY src ./src
COPY config.tsk ./

# Build the application
RUN cargo build --release

# Runtime stage
FROM alpine:latest

# Install runtime dependencies
RUN apk add --no-cache ca-certificates tzdata

WORKDIR /app

# Copy binary and TSK configuration
COPY --from=builder /app/target/release/app .
COPY --from=builder /usr/local/cargo/bin/tusk /usr/local/bin/
COPY --from=builder /app/config.tsk .

# Create non-root user
RUN addgroup -g 1001 -S appgroup && \
    adduser -u 1001 -S appuser -G appgroup

# Set ownership
RUN chown -R appuser:appgroup /app

USER appuser

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD tusk health-check config.tsk || exit 1

# Expose port
EXPOSE 8080

# Run application
CMD ["./app"]
"#;
    
    std::fs::write("Dockerfile", dockerfile_content)?;
    println!("✅ Dockerfile created");
    
    // 2. Docker Compose for development
    println!("2. Creating Docker Compose configuration...");
    
    let docker_compose_content = r#"
version: '3.8'

services:
  app:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "8080:8080"
    environment:
      - APP_ENV=development
      - DATABASE_URL=postgresql://postgres:secret@postgres:5432/app
      - REDIS_URL=redis://redis:6379
    volumes:
      - ./config:/app/config:ro
      - app-logs:/app/logs
    depends_on:
      - postgres
      - redis
    networks:
      - app-network
    restart: unless-stopped

  postgres:
    image: postgres:15-alpine
    environment:
      - POSTGRES_DB=app
      - POSTGRES_USER=postgres
      - POSTGRES_PASSWORD=secret
    volumes:
      - postgres-data:/var/lib/postgresql/data
    networks:
      - app-network
    restart: unless-stopped

  redis:
    image: redis:7-alpine
    command: redis-server --appendonly yes
    volumes:
      - redis-data:/data
    networks:
      - app-network
    restart: unless-stopped

volumes:
  postgres-data:
  redis-data:
  app-logs:

networks:
  app-network:
    driver: bridge
"#;
    
    std::fs::write("docker-compose.yml", docker_compose_content)?;
    println!("✅ Docker Compose configuration created");
    
    // 3. Security scanning
    println!("3. Running security scan...");
    
    let security_scan = Command::new("docker")
        .args(&["build", "-t", "tuskapp:latest", "."])
        .output();
    
    match security_scan {
        Ok(_) => {
            println!("✅ Docker image built successfully");
            
            // Run security scan with Trivy
            let trivy_scan = Command::new("trivy")
                .args(&["image", "--severity", "HIGH,CRITICAL", "tuskapp:latest"])
                .output();
            
            match trivy_scan {
                Ok(output) => {
                    let output_str = String::from_utf8_lossy(&output.stdout);
                    if output_str.contains("Total: 0") {
                        println!("✅ No high/critical vulnerabilities found");
                    } else {
                        println!("⚠️  Security vulnerabilities found:");
                        println!("{}", output_str);
                    }
                }
                Err(_) => {
                    println!("⚠️  Trivy not available, skipping security scan");
                }
            }
        }
        Err(e) => println!("❌ Docker build failed: {}", e),
    }
    
    Ok(())
}
```

### Kubernetes Deployment

```rust
#[tokio::main]
async fn kubernetes_best_practices() -> Result<(), Box<dyn std::error::Error>> {
    println!("☸️  Kubernetes Best Practices");
    
    // 1. Resource limits and requests
    println!("1. Setting resource limits and requests...");
    
    let resource_yaml = r#"
apiVersion: apps/v1
kind: Deployment
metadata:
  name: tuskapp
  namespace: default
spec:
  replicas: 3
  selector:
    matchLabels:
      app: tuskapp
  template:
    metadata:
      labels:
        app: tuskapp
    spec:
      containers:
      - name: app
        image: tuskapp:latest
        ports:
        - containerPort: 8080
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /ready
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 5
        env:
        - name: APP_ENV
          value: "production"
        - name: DATABASE_URL
          valueFrom:
            secretKeyRef:
              name: tuskapp-secrets
              key: database-url
        volumeMounts:
        - name: config
          mountPath: /app/config
          readOnly: true
      volumes:
      - name: config
        configMap:
          name: tuskapp-config
"#;
    
    std::fs::write("k8s/deployment.yaml", resource_yaml)?;
    println!("✅ Deployment configuration created");
    
    // 2. Horizontal Pod Autoscaler
    println!("2. Setting up horizontal pod autoscaler...");
    
    let hpa_yaml = r#"
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: tuskapp-hpa
  namespace: default
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: tuskapp
  minReplicas: 2
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 80
"#;
    
    std::fs::write("k8s/hpa.yaml", hpa_yaml)?;
    println!("✅ HPA configuration created");
    
    // 3. Network policies
    println!("3. Setting up network policies...");
    
    let network_policy_yaml = r#"
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: tuskapp-network-policy
  namespace: default
spec:
  podSelector:
    matchLabels:
      app: tuskapp
  policyTypes:
  - Ingress
  - Egress
  ingress:
  - from:
    - namespaceSelector:
        matchLabels:
          name: ingress-nginx
    ports:
    - protocol: TCP
      port: 8080
  egress:
  - to:
    - namespaceSelector:
        matchLabels:
          name: database
    ports:
    - protocol: TCP
      port: 5432
  - to:
    - namespaceSelector:
        matchLabels:
          name: cache
    ports:
    - protocol: TCP
      port: 6379
"#;
    
    std::fs::write("k8s/network-policy.yaml", network_policy_yaml)?;
    println!("✅ Network policy created");
    
    // 4. Pod disruption budget
    println!("4. Setting up pod disruption budget...");
    
    let pdb_yaml = r#"
apiVersion: policy/v1
kind: PodDisruptionBudget
metadata:
  name: tuskapp-pdb
  namespace: default
spec:
  minAvailable: 2
  selector:
    matchLabels:
      app: tuskapp
"#;
    
    std::fs::write("k8s/pdb.yaml", pdb_yaml)?;
    println!("✅ Pod disruption budget created");
    
    Ok(())
}
```

## 🎯 What You've Learned

1. **Code organization** - Modular project structure, configuration management
2. **Security best practices** - Input validation, authentication, authorization
3. **Performance optimization** - Caching strategies, database optimization
4. **Deployment best practices** - Containerization, Kubernetes deployment
5. **Error handling** - Comprehensive error handling and recovery
6. **Monitoring and observability** - Performance monitoring and alerting

## 🚀 Next Steps

1. **Implement best practices** - Apply these practices to your applications
2. **Automate processes** - Set up CI/CD pipelines with best practices
3. **Monitor and optimize** - Continuously monitor and optimize performance
4. **Security audits** - Regular security audits and updates
5. **Team training** - Train your team on best practices

---

**You now have complete best practices mastery with TuskLang Rust!** From code organization to deployment strategies, from security practices to performance optimization - TuskLang gives you the tools to build maintainable, scalable, and production-ready applications. 