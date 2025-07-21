# 🌍 @env Function in Rust

The `@env` function in TuskLang provides secure and efficient access to environment variables in Rust applications, with type safety and comprehensive error handling.

## Basic Usage

```rust
// Basic environment variable access
let api_key = @env("API_KEY");
let database_url = @env("DATABASE_URL");
let debug_mode = @env("DEBUG_MODE");

// With default values
let port = @env("PORT", "8080");
let timeout = @env("TIMEOUT", "30");
let log_level = @env("LOG_LEVEL", "info");
```

## Type-Safe Environment Variables

```rust
// Type-safe environment variable parsing
let port: u16 = @env.parse("PORT", 8080)?;
let timeout: u64 = @env.parse("TIMEOUT", 30)?;
let debug: bool = @env.parse("DEBUG", false)?;
let max_connections: usize = @env.parse("MAX_CONNECTIONS", 100)?;

// Parse complex types
let allowed_hosts: Vec<String> = @env.parse("ALLOWED_HOSTS", vec!["localhost".to_string()])?;
let config: serde_json::Value = @env.parse("CONFIG", serde_json::json!({}))?;
```

## Secure Environment Variable Access

```rust
// Secure environment variable access with validation
struct SecureEnvAccess {
    validator: EnvironmentValidator,
    encryption_manager: EncryptionManager,
}

impl SecureEnvAccess {
    fn new() -> Self {
        Self {
            validator: EnvironmentValidator::new(),
            encryption_manager: EncryptionManager::new(),
        }
    }
    
    // Secure access to sensitive environment variables
    fn secure_env(&self, key: &str) -> Result<String, Box<dyn std::error::Error>> {
        let value = @env(key)
            .ok_or_else(|| format!("Environment variable {} not found", key))?;
        
        // Validate the environment variable
        self.validator.validate(key, &value)?;
        
        // Decrypt if necessary
        if self.is_encrypted(key) {
            self.encryption_manager.decrypt(&value)
        } else {
            Ok(value)
        }
    }
    
    // Access with type conversion and validation
    fn secure_env_typed<T>(&self, key: &str, default: T) -> Result<T, Box<dyn std::error::Error>>
    where
        T: std::str::FromStr + Clone,
        T::Err: std::error::Error + 'static,
    {
        let value = self.secure_env(key)?;
        value.parse::<T>().map_err(|e| e.into())
    }
}

// Environment variable validator
struct EnvironmentValidator {
    required_vars: std::collections::HashSet<String>,
    sensitive_vars: std::collections::HashSet<String>,
    validation_rules: std::collections::HashMap<String, Box<dyn Fn(&str) -> Result<(), String>>>,
}

impl EnvironmentValidator {
    fn new() -> Self {
        let mut validator = Self {
            required_vars: std::collections::HashSet::new(),
            sensitive_vars: std::collections::HashSet::new(),
            validation_rules: std::collections::HashMap::new(),
        };
        
        // Set up validation rules
        validator.setup_validation_rules();
        
        validator
    }
    
    fn setup_validation_rules(&mut self) {
        // Required variables
        self.required_vars.insert("DATABASE_URL".to_string());
        self.required_vars.insert("API_KEY".to_string());
        self.required_vars.insert("SECRET_KEY".to_string());
        
        // Sensitive variables
        self.sensitive_vars.insert("API_KEY".to_string());
        self.sensitive_vars.insert("SECRET_KEY".to_string());
        self.sensitive_vars.insert("DATABASE_PASSWORD".to_string());
        
        // Validation rules
        self.validation_rules.insert("PORT".to_string(), Box::new(|value| {
            value.parse::<u16>()
                .map(|_| ())
                .map_err(|_| "Port must be a valid number between 1 and 65535".to_string())
        }));
        
        self.validation_rules.insert("DATABASE_URL".to_string(), Box::new(|value| {
            if value.starts_with("postgresql://") || value.starts_with("mysql://") {
                Ok(())
            } else {
                Err("Database URL must be a valid PostgreSQL or MySQL connection string".to_string())
            }
        }));
        
        self.validation_rules.insert("LOG_LEVEL".to_string(), Box::new(|value| {
            let valid_levels = ["debug", "info", "warn", "error"];
            if valid_levels.contains(&value.to_lowercase().as_str()) {
                Ok(())
            } else {
                Err("Log level must be one of: debug, info, warn, error".to_string())
            }
        }));
    }
    
    fn validate(&self, key: &str, value: &str) -> Result<(), Box<dyn std::error::Error>> {
        // Check if variable is required
        if self.required_vars.contains(key) && value.is_empty() {
            return Err(format!("Required environment variable {} is empty", key).into());
        }
        
        // Apply validation rules
        if let Some(validator) = self.validation_rules.get(key) {
            validator(value).map_err(|e| e.into())?;
        }
        
        Ok(())
    }
    
    fn is_sensitive(&self, key: &str) -> bool {
        self.sensitive_vars.contains(key)
    }
}
```

## Environment Configuration Management

```rust
// Comprehensive environment configuration management
#[derive(Debug, Clone)]
struct EnvironmentConfig {
    pub database: DatabaseConfig,
    pub api: ApiConfig,
    pub security: SecurityConfig,
    pub logging: LoggingConfig,
    pub cache: CacheConfig,
}

#[derive(Debug, Clone)]
struct DatabaseConfig {
    pub url: String,
    pub max_connections: u32,
    pub timeout: u64,
    pub ssl_mode: String,
}

#[derive(Debug, Clone)]
struct ApiConfig {
    pub port: u16,
    pub host: String,
    pub timeout: u64,
    pub rate_limit: u32,
}

#[derive(Debug, Clone)]
struct SecurityConfig {
    pub secret_key: String,
    pub jwt_secret: String,
    pub encryption_key: String,
    pub session_timeout: u64,
}

#[derive(Debug, Clone)]
struct LoggingConfig {
    pub level: String,
    pub format: String,
    pub output: String,
}

#[derive(Debug, Clone)]
struct CacheConfig {
    pub redis_url: String,
    pub ttl: u64,
    pub max_size: usize,
}

impl EnvironmentConfig {
    fn load() -> Result<Self, Box<dyn std::error::Error>> {
        let secure_env = SecureEnvAccess::new();
        
        let database = DatabaseConfig {
            url: secure_env.secure_env("DATABASE_URL")?,
            max_connections: secure_env.secure_env_typed("DB_MAX_CONNECTIONS", 10)?,
            timeout: secure_env.secure_env_typed("DB_TIMEOUT", 30)?,
            ssl_mode: @env("DB_SSL_MODE", "require"),
        };
        
        let api = ApiConfig {
            port: secure_env.secure_env_typed("API_PORT", 8080)?,
            host: @env("API_HOST", "0.0.0.0"),
            timeout: secure_env.secure_env_typed("API_TIMEOUT", 30)?,
            rate_limit: secure_env.secure_env_typed("API_RATE_LIMIT", 1000)?,
        };
        
        let security = SecurityConfig {
            secret_key: secure_env.secure_env("SECRET_KEY")?,
            jwt_secret: secure_env.secure_env("JWT_SECRET")?,
            encryption_key: secure_env.secure_env("ENCRYPTION_KEY")?,
            session_timeout: secure_env.secure_env_typed("SESSION_TIMEOUT", 3600)?,
        };
        
        let logging = LoggingConfig {
            level: @env("LOG_LEVEL", "info"),
            format: @env("LOG_FORMAT", "json"),
            output: @env("LOG_OUTPUT", "stdout"),
        };
        
        let cache = CacheConfig {
            redis_url: @env("REDIS_URL", "redis://localhost:6379"),
            ttl: secure_env.secure_env_typed("CACHE_TTL", 3600)?,
            max_size: secure_env.secure_env_typed("CACHE_MAX_SIZE", 1000)?,
        };
        
        Ok(EnvironmentConfig {
            database,
            api,
            security,
            logging,
            cache,
        })
    }
    
    fn validate(&self) -> Result<(), Vec<String>> {
        let mut errors = Vec::new();
        
        // Validate database configuration
        if self.database.url.is_empty() {
            errors.push("DATABASE_URL is required".to_string());
        }
        
        if self.database.max_connections == 0 {
            errors.push("DB_MAX_CONNECTIONS must be greater than 0".to_string());
        }
        
        // Validate API configuration
        if self.api.port == 0 {
            errors.push("API_PORT must be greater than 0".to_string());
        }
        
        if self.api.host.is_empty() {
            errors.push("API_HOST is required".to_string());
        }
        
        // Validate security configuration
        if self.security.secret_key.len() < 32 {
            errors.push("SECRET_KEY must be at least 32 characters long".to_string());
        }
        
        if self.security.jwt_secret.len() < 32 {
            errors.push("JWT_SECRET must be at least 32 characters long".to_string());
        }
        
        if errors.is_empty() {
            Ok(())
        } else {
            Err(errors)
        }
    }
}
```

## Environment Variable Templates

```rust
// Environment variable templates for different environments
struct EnvironmentTemplate {
    template_vars: std::collections::HashMap<String, String>,
}

impl EnvironmentTemplate {
    fn new() -> Self {
        let mut template_vars = std::collections::HashMap::new();
        
        // Development environment
        template_vars.insert("DEV_DATABASE_URL".to_string(), "postgresql://dev:dev@localhost/dev_db".to_string());
        template_vars.insert("DEV_API_PORT".to_string(), "3000".to_string());
        template_vars.insert("DEV_LOG_LEVEL".to_string(), "debug".to_string());
        
        // Staging environment
        template_vars.insert("STAGING_DATABASE_URL".to_string(), "postgresql://staging:staging@staging-db/staging_db".to_string());
        template_vars.insert("STAGING_API_PORT".to_string(), "8080".to_string());
        template_vars.insert("STAGING_LOG_LEVEL".to_string(), "info".to_string());
        
        // Production environment
        template_vars.insert("PROD_DATABASE_URL".to_string(), "postgresql://prod:prod@prod-db/prod_db".to_string());
        template_vars.insert("PROD_API_PORT".to_string(), "80".to_string());
        template_vars.insert("PROD_LOG_LEVEL".to_string(), "warn".to_string());
        
        Self { template_vars }
    }
    
    fn get_template_value(&self, key: &str) -> Option<&String> {
        self.template_vars.get(key)
    }
    
    fn apply_template(&self, template: &str) -> Result<String, Box<dyn std::error::Error>> {
        let mut result = template.to_string();
        
        for (key, value) in &self.template_vars {
            let placeholder = format!("${{{}}}", key);
            result = result.replace(&placeholder, value);
        }
        
        Ok(result)
    }
}

// Environment-specific configuration
fn load_environment_config() -> Result<EnvironmentConfig, Box<dyn std::error::Error>> {
    let environment = @env("APP_ENV", "development");
    let template = EnvironmentTemplate::new();
    
    match environment.as_str() {
        "development" => {
            std::env::set_var("DATABASE_URL", template.get_template_value("DEV_DATABASE_URL").unwrap());
            std::env::set_var("API_PORT", template.get_template_value("DEV_API_PORT").unwrap());
            std::env::set_var("LOG_LEVEL", template.get_template_value("DEV_LOG_LEVEL").unwrap());
        }
        "staging" => {
            std::env::set_var("DATABASE_URL", template.get_template_value("STAGING_DATABASE_URL").unwrap());
            std::env::set_var("API_PORT", template.get_template_value("STAGING_API_PORT").unwrap());
            std::env::set_var("LOG_LEVEL", template.get_template_value("STAGING_LOG_LEVEL").unwrap());
        }
        "production" => {
            std::env::set_var("DATABASE_URL", template.get_template_value("PROD_DATABASE_URL").unwrap());
            std::env::set_var("API_PORT", template.get_template_value("PROD_API_PORT").unwrap());
            std::env::set_var("LOG_LEVEL", template.get_template_value("PROD_LOG_LEVEL").unwrap());
        }
        _ => return Err("Invalid environment specified".into()),
    }
    
    EnvironmentConfig::load()
}
```

## Environment Variable Monitoring

```rust
// Environment variable monitoring and validation
struct EnvironmentMonitor {
    required_vars: std::collections::HashSet<String>,
    sensitive_vars: std::collections::HashSet<String>,
    validation_history: Vec<ValidationEvent>,
}

#[derive(Debug)]
struct ValidationEvent {
    timestamp: chrono::DateTime<chrono::Utc>,
    variable: String,
    status: ValidationStatus,
    message: String,
}

#[derive(Debug)]
enum ValidationStatus {
    Valid,
    Invalid,
    Missing,
    Sensitive,
}

impl EnvironmentMonitor {
    fn new() -> Self {
        let mut monitor = Self {
            required_vars: std::collections::HashSet::new(),
            sensitive_vars: std::collections::HashSet::new(),
            validation_history: Vec::new(),
        };
        
        monitor.setup_monitoring();
        monitor
    }
    
    fn setup_monitoring(&mut self) {
        // Required variables
        self.required_vars.insert("DATABASE_URL".to_string());
        self.required_vars.insert("API_KEY".to_string());
        self.required_vars.insert("SECRET_KEY".to_string());
        
        // Sensitive variables
        self.sensitive_vars.insert("API_KEY".to_string());
        self.sensitive_vars.insert("SECRET_KEY".to_string());
        self.sensitive_vars.insert("DATABASE_PASSWORD".to_string());
    }
    
    fn validate_environment(&mut self) -> Result<(), Vec<String>> {
        let mut errors = Vec::new();
        
        for var in &self.required_vars {
            match std::env::var(var) {
                Ok(value) => {
                    if value.is_empty() {
                        errors.push(format!("Required environment variable {} is empty", var));
                        self.record_validation(var, ValidationStatus::Invalid, "Variable is empty");
                    } else {
                        self.record_validation(var, ValidationStatus::Valid, "Variable is set");
                    }
                }
                Err(_) => {
                    errors.push(format!("Required environment variable {} is missing", var));
                    self.record_validation(var, ValidationStatus::Missing, "Variable is missing");
                }
            }
        }
        
        // Check for sensitive variables in logs
        for var in &self.sensitive_vars {
            if let Ok(value) = std::env::var(var) {
                if !value.is_empty() {
                    self.record_validation(var, ValidationStatus::Sensitive, "Sensitive variable detected");
                }
            }
        }
        
        if errors.is_empty() {
            Ok(())
        } else {
            Err(errors)
        }
    }
    
    fn record_validation(&mut self, variable: &str, status: ValidationStatus, message: &str) {
        let event = ValidationEvent {
            timestamp: chrono::Utc::now(),
            variable: variable.to_string(),
            status,
            message: message.to_string(),
        };
        
        self.validation_history.push(event);
    }
    
    fn get_validation_report(&self) -> ValidationReport {
        let mut report = ValidationReport::new();
        
        for event in &self.validation_history {
            match event.status {
                ValidationStatus::Valid => report.valid_count += 1,
                ValidationStatus::Invalid => report.invalid_count += 1,
                ValidationStatus::Missing => report.missing_count += 1,
                ValidationStatus::Sensitive => report.sensitive_count += 1,
            }
        }
        
        report
    }
}

#[derive(Debug)]
struct ValidationReport {
    valid_count: usize,
    invalid_count: usize,
    missing_count: usize,
    sensitive_count: usize,
}

impl ValidationReport {
    fn new() -> Self {
        Self {
            valid_count: 0,
            invalid_count: 0,
            missing_count: 0,
            sensitive_count: 0,
        }
    }
    
    fn print_summary(&self) {
        println!("Environment Validation Report:");
        println!("  Valid variables: {}", self.valid_count);
        println!("  Invalid variables: {}", self.invalid_count);
        println!("  Missing variables: {}", self.missing_count);
        println!("  Sensitive variables: {}", self.sensitive_count);
    }
}
```

## Best Practices

### 1. Always Provide Default Values
```rust
// Provide sensible defaults for non-critical environment variables
fn load_config_with_defaults() -> Result<Config, Box<dyn std::error::Error>> {
    let config = Config {
        port: @env.parse("PORT", 8080)?,
        host: @env("HOST", "0.0.0.0"),
        timeout: @env.parse("TIMEOUT", 30)?,
        log_level: @env("LOG_LEVEL", "info"),
    };
    
    Ok(config)
}
```

### 2. Validate Critical Environment Variables
```rust
// Validate critical environment variables at startup
fn validate_critical_env_vars() -> Result<(), Box<dyn std::error::Error>> {
    let critical_vars = vec!["DATABASE_URL", "API_KEY", "SECRET_KEY"];
    
    for var in critical_vars {
        let value = @env(var)
            .ok_or_else(|| format!("Critical environment variable {} is missing", var))?;
        
        if value.is_empty() {
            return Err(format!("Critical environment variable {} is empty", var).into());
        }
    }
    
    Ok(())
}
```

### 3. Use Type-Safe Parsing
```rust
// Use type-safe parsing for environment variables
fn parse_env_vars() -> Result<AppConfig, Box<dyn std::error::Error>> {
    let config = AppConfig {
        port: @env.parse("PORT", 8080)?,
        workers: @env.parse("WORKERS", num_cpus::get())?,
        timeout: @env.parse("TIMEOUT", 30)?,
        debug: @env.parse("DEBUG", false)?,
    };
    
    Ok(config)
}
```

### 4. Secure Sensitive Environment Variables
```rust
// Secure handling of sensitive environment variables
fn secure_env_access() -> Result<String, Box<dyn std::error::Error>> {
    let secure_env = SecureEnvAccess::new();
    
    // Access sensitive variables securely
    let api_key = secure_env.secure_env("API_KEY")?;
    let secret_key = secure_env.secure_env("SECRET_KEY")?;
    
    // Use the values securely
    Ok(format!("API configured with key: {}", &api_key[..8]))
}
```

### 5. Monitor Environment Variable Changes
```rust
// Monitor environment variable changes
fn monitor_env_changes() {
    let mut monitor = EnvironmentMonitor::new();
    
    // Validate environment on startup
    if let Err(errors) = monitor.validate_environment() {
        for error in errors {
            @log("Environment validation error: {}", error);
        }
        std::process::exit(1);
    }
    
    // Print validation report
    let report = monitor.get_validation_report();
    report.print_summary();
}
```

The `@env` function in Rust provides secure, type-safe, and efficient access to environment variables with comprehensive validation and monitoring capabilities. 