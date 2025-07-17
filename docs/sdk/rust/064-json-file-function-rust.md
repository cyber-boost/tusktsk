# 📄 @json_file Function in Rust

The `@json_file` function in TuskLang provides specialized JSON file operations in Rust, with schema validation, type safety, and high-performance JSON processing capabilities.

## Basic Usage

```rust
// Basic JSON file operations
let data = @json_file.read("config.json")?;
@json_file.write("data.json", @json_data)?;
let exists = @json_file.exists("settings.json")?;

// JSON file operations with validation
let validated_data = @json_file.read_validate("config.json", @schema)?;
@json_file.write_pretty("output.json", @formatted_data)?;
let parsed_data = @json_file.parse("data.json")?;
```

## JSON File Operations

```rust
// Comprehensive JSON file operations
struct JsonFileManager {
    base_path: std::path::PathBuf,
    schema_registry: std::collections::HashMap<String, serde_json::Value>,
    cache: std::collections::HashMap<String, (serde_json::Value, std::time::Instant)>,
}

impl JsonFileManager {
    fn new(base_path: &str) -> Result<Self, Box<dyn std::error::Error>> {
        let base_path = std::path::PathBuf::from(base_path);
        
        if !base_path.exists() {
            std::fs::create_dir_all(&base_path)?;
        }
        
        Ok(Self {
            base_path,
            schema_registry: std::collections::HashMap::new(),
            cache: std::collections::HashMap::new(),
        })
    }
    
    // Read JSON file with caching
    fn read_json(&mut self, path: &str) -> Result<serde_json::Value, Box<dyn std::error::Error>> {
        let full_path = self.base_path.join(path);
        
        // Validate path
        self.validate_path(&full_path)?;
        
        // Check cache first
        let cache_key = full_path.to_string_lossy().to_string();
        if let Some((cached_data, timestamp)) = self.cache.get(&cache_key) {
            if timestamp.elapsed() < std::time::Duration::from_secs(300) { // 5 minutes cache
                return Ok(cached_data.clone());
            }
        }
        
        // Read and parse JSON
        let content = std::fs::read_to_string(&full_path)?;
        let json_data = serde_json::from_str(&content)?;
        
        // Cache result
        self.cache.insert(cache_key, (json_data.clone(), std::time::Instant::now()));
        
        Ok(json_data)
    }
    
    // Write JSON file with atomic operation
    fn write_json(&self, path: &str, data: &serde_json::Value) -> Result<(), Box<dyn std::error::Error>> {
        let full_path = self.base_path.join(path);
        
        // Validate path
        self.validate_path(&full_path)?;
        
        // Ensure directory exists
        if let Some(parent) = full_path.parent() {
            std::fs::create_dir_all(parent)?;
        }
        
        // Write atomically using temporary file
        let temp_path = full_path.with_extension("tmp");
        let json_string = serde_json::to_string_pretty(data)?;
        std::fs::write(&temp_path, json_string)?;
        std::fs::rename(&temp_path, &full_path)?;
        
        Ok(())
    }
    
    // Read JSON with schema validation
    fn read_json_validate(&mut self, path: &str, schema: &serde_json::Value) -> Result<serde_json::Value, Box<dyn std::error::Error>> {
        let json_data = self.read_json(path)?;
        
        // Validate against schema
        self.validate_against_schema(&json_data, schema)?;
        
        Ok(json_data)
    }
    
    // Write JSON with pretty formatting
    fn write_json_pretty(&self, path: &str, data: &serde_json::Value) -> Result<(), Box<dyn std::error::Error>> {
        let full_path = self.base_path.join(path);
        
        // Validate path
        self.validate_path(&full_path)?;
        
        // Ensure directory exists
        if let Some(parent) = full_path.parent() {
            std::fs::create_dir_all(parent)?;
        }
        
        // Write with pretty formatting
        let json_string = serde_json::to_string_pretty(data)?;
        std::fs::write(&full_path, json_string)?;
        
        Ok(())
    }
    
    // Parse JSON string
    fn parse_json(&self, content: &str) -> Result<serde_json::Value, Box<dyn std::error::Error>> {
        let json_data = serde_json::from_str(content)?;
        Ok(json_data)
    }
    
    // Validate path security
    fn validate_path(&self, path: &std::path::Path) -> Result<(), Box<dyn std::error::Error>> {
        // Check for path traversal attempts
        if path.to_string_lossy().contains("..") {
            return Err("Path traversal attempt detected".into());
        }
        
        // Ensure path is within base directory
        if !path.starts_with(&self.base_path) {
            return Err("Path outside allowed directory".into());
        }
        
        // Check file extension
        if let Some(extension) = path.extension() {
            if extension.to_string_lossy().to_lowercase() != "json" {
                return Err("File must have .json extension".into());
            }
        }
        
        Ok(())
    }
    
    // Validate JSON against schema
    fn validate_against_schema(&self, data: &serde_json::Value, schema: &serde_json::Value) -> Result<(), Box<dyn std::error::Error>> {
        // Use jsonschema crate for validation
        let compiled_schema = jsonschema::JSONSchema::compile(schema)?;
        
        if let Err(errors) = compiled_schema.validate(data) {
            let error_messages: Vec<String> = errors.map(|e| e.to_string()).collect();
            return Err(format!("JSON validation failed: {}", error_messages.join(", ")).into());
        }
        
        Ok(())
    }
}
```

## JSON Schema Management

```rust
// JSON schema management
struct JsonSchemaManager {
    schemas: std::collections::HashMap<String, serde_json::Value>,
    validators: std::collections::HashMap<String, jsonschema::JSONSchema>,
}

impl JsonSchemaManager {
    fn new() -> Self {
        Self {
            schemas: std::collections::HashMap::new(),
            validators: std::collections::HashMap::new(),
        }
    }
    
    // Register schema
    fn register_schema(&mut self, name: &str, schema: serde_json::Value) -> Result<(), Box<dyn std::error::Error>> {
        // Compile schema for validation
        let compiled_schema = jsonschema::JSONSchema::compile(&schema)?;
        
        self.schemas.insert(name.to_string(), schema);
        self.validators.insert(name.to_string(), compiled_schema);
        
        Ok(())
    }
    
    // Get schema
    fn get_schema(&self, name: &str) -> Option<&serde_json::Value> {
        self.schemas.get(name)
    }
    
    // Validate data against schema
    fn validate_data(&self, name: &str, data: &serde_json::Value) -> Result<(), Box<dyn std::error::Error>> {
        if let Some(validator) = self.validators.get(name) {
            if let Err(errors) = validator.validate(data) {
                let error_messages: Vec<String> = errors.map(|e| e.to_string()).collect();
                return Err(format!("Schema validation failed: {}", error_messages.join(", ")).into());
            }
        } else {
            return Err(format!("Schema '{}' not found", name).into());
        }
        
        Ok(())
    }
    
    // Load schema from file
    fn load_schema_from_file(&mut self, name: &str, path: &str) -> Result<(), Box<dyn std::error::Error>> {
        let schema_content = std::fs::read_to_string(path)?;
        let schema = serde_json::from_str(&schema_content)?;
        self.register_schema(name, schema)
    }
}

// Common JSON schemas
fn get_common_schemas() -> std::collections::HashMap<String, serde_json::Value> {
    let mut schemas = std::collections::HashMap::new();
    
    // User schema
    let user_schema = serde_json::json!({
        "type": "object",
        "properties": {
            "id": { "type": "integer" },
            "name": { "type": "string", "minLength": 1 },
            "email": { "type": "string", "format": "email" },
            "age": { "type": "integer", "minimum": 0, "maximum": 150 }
        },
        "required": ["id", "name", "email"]
    });
    schemas.insert("user".to_string(), user_schema);
    
    // Configuration schema
    let config_schema = serde_json::json!({
        "type": "object",
        "properties": {
            "app_name": { "type": "string" },
            "version": { "type": "string" },
            "debug": { "type": "boolean" },
            "port": { "type": "integer", "minimum": 1, "maximum": 65535 },
            "database": {
                "type": "object",
                "properties": {
                    "url": { "type": "string" },
                    "max_connections": { "type": "integer", "minimum": 1 }
                },
                "required": ["url"]
            }
        },
        "required": ["app_name", "version", "port"]
    });
    schemas.insert("config".to_string(), config_schema);
    
    schemas
}
```

## Type-Safe JSON Operations

```rust
// Type-safe JSON operations
struct TypedJsonManager {
    type_registry: std::collections::HashMap<String, Box<dyn JsonTypeHandler>>,
}

trait JsonTypeHandler {
    fn validate(&self, data: &serde_json::Value) -> Result<(), Box<dyn std::error::Error>>;
    fn convert(&self, data: &serde_json::Value) -> Result<Value, Box<dyn std::error::Error>>;
}

impl TypedJsonManager {
    fn new() -> Self {
        let mut manager = Self {
            type_registry: std::collections::HashMap::new(),
        };
        
        // Register type handlers
        manager.register_type_handler("User", Box::new(UserTypeHandler));
        manager.register_type_handler("Config", Box::new(ConfigTypeHandler));
        manager.register_type_handler("LogEntry", Box::new(LogEntryTypeHandler));
        
        manager
    }
    
    fn register_type_handler(&mut self, type_name: &str, handler: Box<dyn JsonTypeHandler>) {
        self.type_registry.insert(type_name.to_string(), handler);
    }
    
    fn read_typed<T>(&self, path: &str) -> Result<T, Box<dyn std::error::Error>>
    where
        T: serde::de::DeserializeOwned,
    {
        let content = std::fs::read_to_string(path)?;
        let data: T = serde_json::from_str(&content)?;
        Ok(data)
    }
    
    fn write_typed<T>(&self, path: &str, data: &T) -> Result<(), Box<dyn std::error::Error>>
    where
        T: serde::Serialize,
    {
        let json_string = serde_json::to_string_pretty(data)?;
        std::fs::write(path, json_string)?;
        Ok(())
    }
}

// User type handler
struct UserTypeHandler;

impl JsonTypeHandler for UserTypeHandler {
    fn validate(&self, data: &serde_json::Value) -> Result<(), Box<dyn std::error::Error>> {
        if let Some(obj) = data.as_object() {
            if !obj.contains_key("id") || !obj.contains_key("name") || !obj.contains_key("email") {
                return Err("User must have id, name, and email fields".into());
            }
            
            if let Some(email) = obj.get("email").and_then(|e| e.as_str()) {
                if !email.contains('@') {
                    return Err("Invalid email format".into());
                }
            }
        } else {
            return Err("User data must be an object".into());
        }
        
        Ok(())
    }
    
    fn convert(&self, data: &serde_json::Value) -> Result<Value, Box<dyn std::error::Error>> {
        // Convert to TuskLang Value
        Ok(serde_json::from_value(data.clone())?)
    }
}

// Configuration type handler
struct ConfigTypeHandler;

impl JsonTypeHandler for ConfigTypeHandler {
    fn validate(&self, data: &serde_json::Value) -> Result<(), Box<dyn std::error::Error>> {
        if let Some(obj) = data.as_object() {
            if !obj.contains_key("app_name") || !obj.contains_key("version") || !obj.contains_key("port") {
                return Err("Config must have app_name, version, and port fields".into());
            }
            
            if let Some(port) = obj.get("port").and_then(|p| p.as_u64()) {
                if port == 0 || port > 65535 {
                    return Err("Port must be between 1 and 65535".into());
                }
            }
        } else {
            return Err("Config data must be an object".into());
        }
        
        Ok(())
    }
    
    fn convert(&self, data: &serde_json::Value) -> Result<Value, Box<dyn std::error::Error>> {
        Ok(serde_json::from_value(data.clone())?)
    }
}

// Log entry type handler
struct LogEntryTypeHandler;

impl JsonTypeHandler for LogEntryTypeHandler {
    fn validate(&self, data: &serde_json::Value) -> Result<(), Box<dyn std::error::Error>> {
        if let Some(obj) = data.as_object() {
            if !obj.contains_key("timestamp") || !obj.contains_key("level") || !obj.contains_key("message") {
                return Err("Log entry must have timestamp, level, and message fields".into());
            }
            
            if let Some(level) = obj.get("level").and_then(|l| l.as_str()) {
                let valid_levels = ["debug", "info", "warn", "error"];
                if !valid_levels.contains(&level) {
                    return Err("Invalid log level".into());
                }
            }
        } else {
            return Err("Log entry data must be an object".into());
        }
        
        Ok(())
    }
    
    fn convert(&self, data: &serde_json::Value) -> Result<Value, Box<dyn std::error::Error>> {
        Ok(serde_json::from_value(data.clone())?)
    }
}
```

## JSON Data Structures

```rust
// JSON data structures with serde
#[derive(Debug, serde::Serialize, serde::Deserialize)]
struct User {
    id: u32,
    name: String,
    email: String,
    age: Option<u32>,
    preferences: Option<UserPreferences>,
}

#[derive(Debug, serde::Serialize, serde::Deserialize)]
struct UserPreferences {
    theme: String,
    notifications: bool,
    language: String,
}

#[derive(Debug, serde::Serialize, serde::Deserialize)]
struct AppConfig {
    app_name: String,
    version: String,
    debug: bool,
    port: u16,
    database: DatabaseConfig,
    features: Vec<String>,
}

#[derive(Debug, serde::Serialize, serde::Deserialize)]
struct DatabaseConfig {
    url: String,
    max_connections: u32,
    timeout: u64,
}

#[derive(Debug, serde::Serialize, serde::Deserialize)]
struct LogEntry {
    timestamp: chrono::DateTime<chrono::Utc>,
    level: LogLevel,
    message: String,
    metadata: std::collections::HashMap<String, serde_json::Value>,
}

#[derive(Debug, serde::Serialize, serde::Deserialize)]
enum LogLevel {
    Debug,
    Info,
    Warn,
    Error,
}

// JSON operations with typed structures
fn typed_json_operations() -> Result<(), Box<dyn std::error::Error>> {
    let typed_manager = TypedJsonManager::new();
    
    // Read typed configuration
    let config: AppConfig = typed_manager.read_typed("config.json")?;
    println!("App: {} v{}", config.app_name, config.version);
    
    // Write typed user data
    let user = User {
        id: 1,
        name: "John Doe".to_string(),
        email: "john@example.com".to_string(),
        age: Some(30),
        preferences: Some(UserPreferences {
            theme: "dark".to_string(),
            notifications: true,
            language: "en".to_string(),
        }),
    };
    
    typed_manager.write_typed("user.json", &user)?;
    
    // Read and write log entries
    let log_entry = LogEntry {
        timestamp: chrono::Utc::now(),
        level: LogLevel::Info,
        message: "Application started".to_string(),
        metadata: {
            let mut meta = std::collections::HashMap::new();
            meta.insert("user_id".to_string(), serde_json::Value::Number(1.into()));
            meta
        },
    };
    
    typed_manager.write_typed("log.json", &log_entry)?;
    
    Ok(())
}
```

## JSON Validation and Transformation

```rust
// JSON validation and transformation
struct JsonValidator {
    rules: std::collections::HashMap<String, Box<dyn ValidationRule>>,
}

trait ValidationRule {
    fn validate(&self, data: &serde_json::Value) -> Result<(), String>;
    fn transform(&self, data: &serde_json::Value) -> Result<serde_json::Value, String>;
}

impl JsonValidator {
    fn new() -> Self {
        let mut validator = Self {
            rules: std::collections::HashMap::new(),
        };
        
        // Register validation rules
        validator.register_rule("email", Box::new(EmailValidationRule));
        validator.register_rule("url", Box::new(UrlValidationRule));
        validator.register_rule("date", Box::new(DateValidationRule));
        
        validator
    }
    
    fn register_rule(&mut self, name: &str, rule: Box<dyn ValidationRule>) {
        self.rules.insert(name.to_string(), rule);
    }
    
    fn validate_json(&self, data: &serde_json::Value, rules: &[String]) -> Result<(), Vec<String>> {
        let mut errors = Vec::new();
        
        for rule_name in rules {
            if let Some(rule) = self.rules.get(rule_name) {
                if let Err(error) = rule.validate(data) {
                    errors.push(format!("{}: {}", rule_name, error));
                }
            }
        }
        
        if errors.is_empty() {
            Ok(())
        } else {
            Err(errors)
        }
    }
    
    fn transform_json(&self, data: &serde_json::Value, rules: &[String]) -> Result<serde_json::Value, Box<dyn std::error::Error>> {
        let mut transformed = data.clone();
        
        for rule_name in rules {
            if let Some(rule) = self.rules.get(rule_name) {
                transformed = rule.transform(&transformed)?;
            }
        }
        
        Ok(transformed)
    }
}

// Email validation rule
struct EmailValidationRule;

impl ValidationRule for EmailValidationRule {
    fn validate(&self, data: &serde_json::Value) -> Result<(), String> {
        if let Some(email) = data.as_str() {
            if !email.contains('@') || !email.contains('.') {
                return Err("Invalid email format".to_string());
            }
        } else {
            return Err("Email must be a string".to_string());
        }
        
        Ok(())
    }
    
    fn transform(&self, data: &serde_json::Value) -> Result<serde_json::Value, String> {
        if let Some(email) = data.as_str() {
            Ok(serde_json::Value::String(email.to_lowercase()))
        } else {
            Err("Email must be a string".to_string())
        }
    }
}

// URL validation rule
struct UrlValidationRule;

impl ValidationRule for UrlValidationRule {
    fn validate(&self, data: &serde_json::Value) -> Result<(), String> {
        if let Some(url) = data.as_str() {
            if !url.starts_with("http://") && !url.starts_with("https://") {
                return Err("URL must start with http:// or https://".to_string());
            }
        } else {
            return Err("URL must be a string".to_string());
        }
        
        Ok(())
    }
    
    fn transform(&self, data: &serde_json::Value) -> Result<serde_json::Value, String> {
        if let Some(url) = data.as_str() {
            // Normalize URL
            let normalized = if !url.starts_with("http://") && !url.starts_with("https://") {
                format!("https://{}", url)
            } else {
                url.to_string()
            };
            
            Ok(serde_json::Value::String(normalized))
        } else {
            Err("URL must be a string".to_string())
        }
    }
}

// Date validation rule
struct DateValidationRule;

impl ValidationRule for DateValidationRule {
    fn validate(&self, data: &serde_json::Value) -> Result<(), String> {
        if let Some(date_str) = data.as_str() {
            if chrono::DateTime::parse_from_rfc3339(date_str).is_err() {
                return Err("Invalid date format. Expected RFC3339".to_string());
            }
        } else {
            return Err("Date must be a string".to_string());
        }
        
        Ok(())
    }
    
    fn transform(&self, data: &serde_json::Value) -> Result<serde_json::Value, String> {
        if let Some(date_str) = data.as_str() {
            if let Ok(datetime) = chrono::DateTime::parse_from_rfc3339(date_str) {
                Ok(serde_json::Value::String(datetime.to_rfc3339()))
            } else {
                Err("Invalid date format".to_string())
            }
        } else {
            Err("Date must be a string".to_string())
        }
    }
}
```

## Best Practices

### 1. Always Validate JSON Data
```rust
// Validate JSON data before processing
fn safe_json_operation(path: &str) -> Result<(), Box<dyn std::error::Error>> {
    let mut json_manager = JsonFileManager::new("/safe/path")?;
    let schema_manager = JsonSchemaManager::new();
    
    // Load and validate schema
    let schema = schema_manager.get_schema("config").unwrap();
    
    // Read and validate JSON
    let data = json_manager.read_json_validate(path, schema)?;
    
    // Process validated data
    process_config_data(data)?;
    
    Ok(())
}
```

### 2. Use Type-Safe Operations
```rust
// Use type-safe JSON operations
fn typed_json_handling() -> Result<(), Box<dyn std::error::Error>> {
    let typed_manager = TypedJsonManager::new();
    
    // Read with type safety
    let config: AppConfig = typed_manager.read_typed("config.json")?;
    
    // Modify and write back
    let mut updated_config = config;
    updated_config.debug = true;
    typed_manager.write_typed("config.json", &updated_config)?;
    
    Ok(())
}
```

### 3. Implement JSON Caching
```rust
// Cache JSON data for performance
fn cached_json_read(path: &str) -> Result<serde_json::Value, Box<dyn std::error::Error>> {
    let cache_key = format!("json:{}", path);
    
    // Check cache first
    if let Some(cached) = @cache.get(&cache_key) {
        return Ok(cached);
    }
    
    // Read JSON file
    let mut json_manager = JsonFileManager::new("/base/path")?;
    let data = json_manager.read_json(path)?;
    
    // Cache for 10 minutes
    @cache.put(&cache_key, &data, std::time::Duration::from_secs(600))?;
    
    Ok(data)
}
```

### 4. Validate JSON Schema
```rust
// Validate JSON against schema
fn validate_json_schema(data: &serde_json::Value, schema_name: &str) -> Result<(), Box<dyn std::error::Error>> {
    let schema_manager = JsonSchemaManager::new();
    
    // Load common schemas
    let schemas = get_common_schemas();
    for (name, schema) in schemas {
        schema_manager.register_schema(&name, schema)?;
    }
    
    // Validate data
    schema_manager.validate_data(schema_name, data)?;
    
    Ok(())
}
```

### 5. Use Pretty Formatting for Human-Readable Files
```rust
// Use pretty formatting for configuration files
fn write_config_file(config: &AppConfig, path: &str) -> Result<(), Box<dyn std::error::Error>> {
    let json_manager = JsonFileManager::new("/config")?;
    
    // Convert to JSON value
    let json_data = serde_json::to_value(config)?;
    
    // Write with pretty formatting
    json_manager.write_json_pretty(path, &json_data)?;
    
    Ok(())
}
```

The `@json_file` function in Rust provides comprehensive JSON file operations with schema validation, type safety, and high performance. 