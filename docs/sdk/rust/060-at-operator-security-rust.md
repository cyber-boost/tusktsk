# 🔒 @ Operator Security in Rust

TuskLang provides comprehensive security features for @ operators in Rust, ensuring secure data handling, input validation, and protection against common vulnerabilities.

## Security Principles

```rust
// Security-focused @ operator implementation
#[derive(Debug)]
struct SecurityContext {
    pub user_id: Option<u32>,
    pub permissions: Vec<String>,
    pub session_token: Option<String>,
    pub ip_address: String,
    pub user_agent: String,
}

// Security validation trait
trait SecurityValidator {
    fn validate_input(&self, input: &Value) -> Result<(), SecurityError>;
    fn validate_permissions(&self, required_permissions: &[String]) -> Result<(), SecurityError>;
    fn sanitize_output(&self, output: Value) -> Result<Value, SecurityError>;
    fn audit_operation(&self, operation: &str, context: &SecurityContext) -> Result<(), SecurityError>;
}

// Security error types
#[derive(Debug, thiserror::Error)]
pub enum SecurityError {
    #[error("Input validation failed: {message}")]
    InputValidationError { message: String },
    
    #[error("Insufficient permissions: required {required}, have {actual}")]
    InsufficientPermissions { required: Vec<String>, actual: Vec<String> },
    
    #[error("SQL injection attempt detected")]
    SqlInjectionAttempt,
    
    #[error("XSS attempt detected")]
    XssAttempt,
    
    #[error("Path traversal attempt detected")]
    PathTraversalAttempt,
    
    #[error("Rate limit exceeded")]
    RateLimitExceeded,
    
    #[error("Authentication required")]
    AuthenticationRequired,
    
    #[error("Session expired")]
    SessionExpired,
}
```

## Input Validation and Sanitization

```rust
// Secure input validation
struct SecureInputValidator {
    sql_keywords: std::collections::HashSet<String>,
    xss_patterns: Vec<regex::Regex>,
    path_traversal_patterns: Vec<regex::Regex>,
}

impl SecureInputValidator {
    fn new() -> Self {
        let sql_keywords = vec![
            "SELECT", "INSERT", "UPDATE", "DELETE", "DROP", "CREATE", "ALTER",
            "UNION", "OR", "AND", "WHERE", "FROM", "JOIN", "HAVING", "GROUP BY"
        ].into_iter().map(|s| s.to_string()).collect();
        
        let xss_patterns = vec![
            regex::Regex::new(r"<script[^>]*>.*?</script>").unwrap(),
            regex::Regex::new(r"javascript:").unwrap(),
            regex::Regex::new(r"on\w+\s*=").unwrap(),
        ];
        
        let path_traversal_patterns = vec![
            regex::Regex::new(r"\.\.").unwrap(),
            regex::Regex::new(r"//").unwrap(),
            regex::Regex::new(r"\\").unwrap(),
        ];
        
        Self {
            sql_keywords,
            xss_patterns,
            path_traversal_patterns,
        }
    }
    
    fn validate_string_input(&self, input: &str, context: &str) -> Result<String, SecurityError> {
        let sanitized = input.trim();
        
        // Check for SQL injection
        if self.detect_sql_injection(sanitized) {
            return Err(SecurityError::SqlInjectionAttempt);
        }
        
        // Check for XSS
        if self.detect_xss(sanitized) {
            return Err(SecurityError::XssAttempt);
        }
        
        // Check for path traversal
        if self.detect_path_traversal(sanitized) {
            return Err(SecurityError::PathTraversalAttempt);
        }
        
        // Apply context-specific validation
        match context {
            "email" => self.validate_email(sanitized),
            "username" => self.validate_username(sanitized),
            "filename" => self.validate_filename(sanitized),
            _ => Ok(sanitized.to_string()),
        }
    }
    
    fn detect_sql_injection(&self, input: &str) -> bool {
        let upper_input = input.to_uppercase();
        self.sql_keywords.iter().any(|keyword| upper_input.contains(keyword))
    }
    
    fn detect_xss(&self, input: &str) -> bool {
        self.xss_patterns.iter().any(|pattern| pattern.is_match(input))
    }
    
    fn detect_path_traversal(&self, input: &str) -> bool {
        self.path_traversal_patterns.iter().any(|pattern| pattern.is_match(input))
    }
    
    fn validate_email(&self, email: &str) -> Result<String, SecurityError> {
        let email_regex = regex::Regex::new(r"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$").unwrap();
        
        if email_regex.is_match(email) {
            Ok(email.to_string())
        } else {
            Err(SecurityError::InputValidationError {
                message: "Invalid email format".to_string(),
            })
        }
    }
    
    fn validate_username(&self, username: &str) -> Result<String, SecurityError> {
        if username.len() < 3 || username.len() > 20 {
            return Err(SecurityError::InputValidationError {
                message: "Username must be between 3 and 20 characters".to_string(),
            });
        }
        
        let username_regex = regex::Regex::new(r"^[a-zA-Z0-9_-]+$").unwrap();
        if username_regex.is_match(username) {
            Ok(username.to_string())
        } else {
            Err(SecurityError::InputValidationError {
                message: "Username contains invalid characters".to_string(),
            })
        }
    }
    
    fn validate_filename(&self, filename: &str) -> Result<String, SecurityError> {
        let filename_regex = regex::Regex::new(r"^[a-zA-Z0-9._-]+$").unwrap();
        if filename_regex.is_match(filename) {
            Ok(filename.to_string())
        } else {
            Err(SecurityError::InputValidationError {
                message: "Invalid filename".to_string(),
            })
        }
    }
}
```

## Authentication and Authorization

```rust
// Secure authentication and authorization
struct SecurityManager {
    validator: SecureInputValidator,
    rate_limiter: RateLimiter,
    session_manager: SessionManager,
}

impl SecurityManager {
    fn new() -> Self {
        Self {
            validator: SecureInputValidator::new(),
            rate_limiter: RateLimiter::new(),
            session_manager: SessionManager::new(),
        }
    }
    
    fn authenticate_user(&self, credentials: &Value) -> Result<SecurityContext, SecurityError> {
        // Validate input
        let username = credentials.get("username")
            .and_then(|u| u.as_str())
            .ok_or(SecurityError::InputValidationError {
                message: "Username required".to_string(),
            })?;
        
        let password = credentials.get("password")
            .and_then(|p| p.as_str())
            .ok_or(SecurityError::InputValidationError {
                message: "Password required".to_string(),
            })?;
        
        // Sanitize inputs
        let sanitized_username = self.validator.validate_string_input(username, "username")?;
        
        // Check rate limiting
        if !self.rate_limiter.check_limit(&sanitized_username, "login") {
            return Err(SecurityError::RateLimitExceeded);
        }
        
        // Verify credentials
        let user = @verify_credentials(&sanitized_username, password)?;
        
        // Create security context
        let context = SecurityContext {
            user_id: Some(user.id),
            permissions: user.permissions,
            session_token: Some(@generate_session_token(user.id)?),
            ip_address: @get_client_ip(),
            user_agent: @get_user_agent(),
        };
        
        // Audit login
        self.audit_operation("user_login", &context)?;
        
        Ok(context)
    }
    
    fn authorize_operation(&self, operation: &str, context: &SecurityContext) -> Result<(), SecurityError> {
        // Check if user is authenticated
        let user_id = context.user_id.ok_or(SecurityError::AuthenticationRequired)?;
        
        // Check session validity
        if let Some(token) = &context.session_token {
            if !@validate_session_token(token)? {
                return Err(SecurityError::SessionExpired);
            }
        }
        
        // Get required permissions for operation
        let required_permissions = @get_required_permissions(operation)?;
        
        // Check if user has required permissions
        let has_permission = required_permissions.iter().all(|permission| {
            context.permissions.contains(permission)
        });
        
        if !has_permission {
            return Err(SecurityError::InsufficientPermissions {
                required: required_permissions,
                actual: context.permissions.clone(),
            });
        }
        
        // Audit operation
        self.audit_operation(operation, context)?;
        
        Ok(())
    }
    
    fn audit_operation(&self, operation: &str, context: &SecurityContext) -> Result<(), SecurityError> {
        let audit_log = AuditLog {
            timestamp: chrono::Utc::now(),
            user_id: context.user_id,
            operation: operation.to_string(),
            ip_address: context.ip_address.clone(),
            user_agent: context.user_agent.clone(),
            success: true,
        };
        
        @save_audit_log(audit_log)?;
        Ok(())
    }
}

#[derive(Debug)]
struct AuditLog {
    timestamp: chrono::DateTime<chrono::Utc>,
    user_id: Option<u32>,
    operation: String,
    ip_address: String,
    user_agent: String,
    success: bool,
}
```

## Secure Database Operations

```rust
// Secure database operations with parameterized queries
struct SecureDatabaseOperator {
    validator: SecureInputValidator,
    connection_pool: sqlx::PgPool,
}

impl SecureDatabaseOperator {
    fn new(connection_pool: sqlx::PgPool) -> Self {
        Self {
            validator: SecureInputValidator::new(),
            connection_pool,
        }
    }
    
    // Secure query execution with parameterized statements
    async fn secure_query(&self, sql: &str, params: Vec<Value>, context: &SecurityContext) -> Result<Vec<Value>, SecurityError> {
        // Validate SQL template (not user input)
        self.validate_sql_template(sql)?;
        
        // Sanitize parameters
        let sanitized_params = self.sanitize_parameters(params)?;
        
        // Execute with prepared statement
        let statement = self.connection_pool.prepare(sql).await
            .map_err(|e| SecurityError::InputValidationError {
                message: format!("Invalid SQL template: {}", e)
            })?;
        
        let result = statement.query_with(sanitized_params)
            .fetch_all(&self.connection_pool)
            .await
            .map_err(|e| SecurityError::InputValidationError {
                message: format!("Database error: {}", e)
            })?;
        
        Ok(result.into_iter().map(|row| row.into()).collect())
    }
    
    fn validate_sql_template(&self, sql: &str) -> Result<(), SecurityError> {
        // Check for dangerous SQL patterns
        let dangerous_patterns = vec![
            "DROP TABLE",
            "DELETE FROM",
            "TRUNCATE",
            "ALTER TABLE",
            "CREATE TABLE",
        ];
        
        let upper_sql = sql.to_uppercase();
        for pattern in dangerous_patterns {
            if upper_sql.contains(pattern) {
                return Err(SecurityError::InputValidationError {
                    message: format!("Dangerous SQL pattern detected: {}", pattern)
                });
            }
        }
        
        Ok(())
    }
    
    fn sanitize_parameters(&self, params: Vec<Value>) -> Result<Vec<Value>, SecurityError> {
        let mut sanitized = Vec::new();
        
        for param in params {
            let sanitized_param = match param {
                Value::String(s) => {
                    let sanitized = self.validator.validate_string_input(&s, "general")?;
                    Value::String(sanitized)
                }
                Value::Number(n) => Value::Number(n),
                Value::Bool(b) => Value::Bool(b),
                Value::Array(arr) => {
                    let sanitized_arr = self.sanitize_parameters(arr)?;
                    Value::Array(sanitized_arr)
                }
                Value::Object(obj) => {
                    let mut sanitized_obj = std::collections::HashMap::new();
                    for (key, value) in obj {
                        let sanitized_key = self.validator.validate_string_input(&key, "key")?;
                        let sanitized_value = self.sanitize_single_parameter(value)?;
                        sanitized_obj.insert(sanitized_key, sanitized_value);
                    }
                    Value::Object(sanitized_obj)
                }
                Value::Null => Value::Null,
            };
            
            sanitized.push(sanitized_param);
        }
        
        Ok(sanitized)
    }
    
    fn sanitize_single_parameter(&self, param: Value) -> Result<Value, SecurityError> {
        match param {
            Value::String(s) => {
                let sanitized = self.validator.validate_string_input(&s, "general")?;
                Ok(Value::String(sanitized))
            }
            _ => Ok(param),
        }
    }
}
```

## Rate Limiting and DDoS Protection

```rust
// Rate limiting implementation
struct RateLimiter {
    limits: std::collections::HashMap<String, (u32, std::time::Duration)>,
    counters: std::collections::HashMap<String, (u32, std::time::Instant)>,
}

impl RateLimiter {
    fn new() -> Self {
        let mut limits = std::collections::HashMap::new();
        limits.insert("login".to_string(), (5, std::time::Duration::from_secs(300))); // 5 attempts per 5 minutes
        limits.insert("api_call".to_string(), (100, std::time::Duration::from_secs(60))); // 100 calls per minute
        limits.insert("file_upload".to_string(), (10, std::time::Duration::from_secs(3600))); // 10 uploads per hour
        
        Self {
            limits,
            counters: std::collections::HashMap::new(),
        }
    }
    
    fn check_limit(&mut self, identifier: &str, operation: &str) -> bool {
        let key = format!("{}:{}", identifier, operation);
        let now = std::time::Instant::now();
        
        if let Some((max_attempts, window)) = self.limits.get(operation) {
            if let Some((count, start_time)) = self.counters.get(&key) {
                if now.duration_since(*start_time) < *window {
                    if *count >= *max_attempts {
                        return false; // Rate limit exceeded
                    }
                    // Increment counter
                    self.counters.insert(key, (*count + 1, *start_time));
                } else {
                    // Reset counter for new window
                    self.counters.insert(key, (1, now));
                }
            } else {
                // First attempt
                self.counters.insert(key, (1, now));
            }
        }
        
        true
    }
    
    fn reset_limit(&mut self, identifier: &str, operation: &str) {
        let key = format!("{}:{}", identifier, operation);
        self.counters.remove(&key);
    }
}

// DDoS protection
struct DdosProtector {
    ip_blacklist: std::collections::HashSet<String>,
    suspicious_patterns: Vec<regex::Regex>,
    request_history: std::collections::HashMap<String, Vec<std::time::Instant>>,
}

impl DdosProtector {
    fn new() -> Self {
        let suspicious_patterns = vec![
            regex::Regex::new(r"\.\./").unwrap(), // Path traversal
            regex::Regex::new(r"<script").unwrap(), // XSS
            regex::Regex::new(r"UNION.*SELECT").unwrap(), // SQL injection
        ];
        
        Self {
            ip_blacklist: std::collections::HashSet::new(),
            suspicious_patterns,
            request_history: std::collections::HashMap::new(),
        }
    }
    
    fn check_request(&mut self, ip: &str, user_agent: &str, path: &str) -> Result<(), SecurityError> {
        // Check if IP is blacklisted
        if self.ip_blacklist.contains(ip) {
            return Err(SecurityError::RateLimitExceeded);
        }
        
        // Check for suspicious patterns
        for pattern in &self.suspicious_patterns {
            if pattern.is_match(path) || pattern.is_match(user_agent) {
                self.ip_blacklist.insert(ip.to_string());
                return Err(SecurityError::InputValidationError {
                    message: "Suspicious request pattern detected".to_string(),
                });
            }
        }
        
        // Check request frequency
        let now = std::time::Instant::now();
        let window = std::time::Duration::from_secs(60);
        let max_requests = 1000;
        
        let history = self.request_history.entry(ip.to_string()).or_insert_with(Vec::new);
        
        // Remove old requests
        history.retain(|&timestamp| now.duration_since(timestamp) < window);
        
        // Check if too many requests
        if history.len() >= max_requests {
            self.ip_blacklist.insert(ip.to_string());
            return Err(SecurityError::RateLimitExceeded);
        }
        
        // Add current request
        history.push(now);
        
        Ok(())
    }
}
```

## Secure File Operations

```rust
// Secure file operations
struct SecureFileOperator {
    validator: SecureInputValidator,
    allowed_extensions: std::collections::HashSet<String>,
    max_file_size: usize,
    upload_directory: std::path::PathBuf,
}

impl SecureFileOperator {
    fn new() -> Self {
        let allowed_extensions = vec![
            "jpg", "jpeg", "png", "gif", "pdf", "txt", "doc", "docx"
        ].into_iter().map(|s| s.to_string()).collect();
        
        Self {
            validator: SecureInputValidator::new(),
            allowed_extensions,
            max_file_size: 10 * 1024 * 1024, // 10MB
            upload_directory: std::path::PathBuf::from("/secure/uploads"),
        }
    }
    
    fn secure_file_upload(&self, filename: &str, content: &[u8], context: &SecurityContext) -> Result<String, SecurityError> {
        // Validate filename
        let sanitized_filename = self.validator.validate_string_input(filename, "filename")?;
        
        // Check file extension
        if let Some(extension) = std::path::Path::new(&sanitized_filename).extension() {
            let ext = extension.to_string_lossy().to_lowercase();
            if !self.allowed_extensions.contains(&ext) {
                return Err(SecurityError::InputValidationError {
                    message: format!("File extension '{}' not allowed", ext)
                });
            }
        }
        
        // Check file size
        if content.len() > self.max_file_size {
            return Err(SecurityError::InputValidationError {
                message: "File too large".to_string(),
            });
        }
        
        // Generate secure filename
        let secure_filename = self.generate_secure_filename(&sanitized_filename)?;
        let file_path = self.upload_directory.join(&secure_filename);
        
        // Ensure path is within upload directory
        if !file_path.starts_with(&self.upload_directory) {
            return Err(SecurityError::PathTraversalAttempt);
        }
        
        // Write file
        std::fs::write(&file_path, content)
            .map_err(|e| SecurityError::InputValidationError {
                message: format!("Failed to write file: {}", e)
            })?;
        
        // Audit file upload
        @audit_file_upload(&secure_filename, content.len(), context)?;
        
        Ok(secure_filename)
    }
    
    fn generate_secure_filename(&self, original_filename: &str) -> Result<String, SecurityError> {
        use rand::Rng;
        
        let mut rng = rand::thread_rng();
        let random_suffix: String = (0..16)
            .map(|_| rng.sample(rand::distributions::Alphanumeric) as char)
            .collect();
        
        let extension = std::path::Path::new(original_filename)
            .extension()
            .and_then(|ext| ext.to_str())
            .unwrap_or("");
        
        let secure_filename = if extension.is_empty() {
            random_suffix
        } else {
            format!("{}.{}", random_suffix, extension)
        };
        
        Ok(secure_filename)
    }
    
    fn secure_file_read(&self, filename: &str, context: &SecurityContext) -> Result<Vec<u8>, SecurityError> {
        // Validate filename
        let sanitized_filename = self.validator.validate_string_input(filename, "filename")?;
        
        // Construct file path
        let file_path = self.upload_directory.join(&sanitized_filename);
        
        // Ensure path is within upload directory
        if !file_path.starts_with(&self.upload_directory) {
            return Err(SecurityError::PathTraversalAttempt);
        }
        
        // Check if file exists
        if !file_path.exists() {
            return Err(SecurityError::InputValidationError {
                message: "File not found".to_string(),
            });
        }
        
        // Read file
        let content = std::fs::read(&file_path)
            .map_err(|e| SecurityError::InputValidationError {
                message: format!("Failed to read file: {}", e)
            })?;
        
        // Audit file access
        @audit_file_access(&sanitized_filename, context)?;
        
        Ok(content)
    }
}
```

## Encryption and Data Protection

```rust
// Encryption utilities
struct EncryptionManager {
    encryption_key: [u8; 32],
    algorithm: String,
}

impl EncryptionManager {
    fn new(key: [u8; 32]) -> Self {
        Self {
            encryption_key: key,
            algorithm: "AES-256-GCM".to_string(),
        }
    }
    
    fn encrypt_sensitive_data(&self, data: &str) -> Result<String, SecurityError> {
        use aes_gcm::{Aes256Gcm, Key, Nonce};
        use aes_gcm::aead::{Aead, NewAead};
        
        let key = Key::from_slice(&self.encryption_key);
        let cipher = Aes256Gcm::new(key);
        
        let nonce = Nonce::from_slice(b"unique nonce"); // In production, use random nonce
        
        let encrypted = cipher.encrypt(nonce, data.as_bytes())
            .map_err(|e| SecurityError::InputValidationError {
                message: format!("Encryption failed: {}", e)
            })?;
        
        Ok(base64::encode(encrypted))
    }
    
    fn decrypt_sensitive_data(&self, encrypted_data: &str) -> Result<String, SecurityError> {
        use aes_gcm::{Aes256Gcm, Key, Nonce};
        use aes_gcm::aead::{Aead, NewAead};
        
        let key = Key::from_slice(&self.encryption_key);
        let cipher = Aes256Gcm::new(key);
        
        let nonce = Nonce::from_slice(b"unique nonce");
        
        let encrypted_bytes = base64::decode(encrypted_data)
            .map_err(|e| SecurityError::InputValidationError {
                message: format!("Invalid encrypted data: {}", e)
            })?;
        
        let decrypted = cipher.decrypt(nonce, encrypted_bytes.as_ref())
            .map_err(|e| SecurityError::InputValidationError {
                message: format!("Decryption failed: {}", e)
            })?;
        
        String::from_utf8(decrypted)
            .map_err(|e| SecurityError::InputValidationError {
                message: format!("Invalid decrypted data: {}", e)
            })
    }
    
    fn hash_password(&self, password: &str) -> Result<String, SecurityError> {
        use argon2::{Argon2, PasswordHash, PasswordHasher, PasswordVerifier, SaltString};
        
        let salt = SaltString::generate(&mut rand::thread_rng());
        let argon2 = Argon2::default();
        
        let password_hash = argon2.hash_password(password.as_bytes(), &salt)
            .map_err(|e| SecurityError::InputValidationError {
                message: format!("Password hashing failed: {}", e)
            })?;
        
        Ok(password_hash.to_string())
    }
    
    fn verify_password(&self, password: &str, hash: &str) -> Result<bool, SecurityError> {
        use argon2::{Argon2, PasswordHash, PasswordVerifier};
        
        let parsed_hash = PasswordHash::new(hash)
            .map_err(|e| SecurityError::InputValidationError {
                message: format!("Invalid password hash: {}", e)
            })?;
        
        Ok(Argon2::default().verify_password(password.as_bytes(), &parsed_hash).is_ok())
    }
}
```

## Secure @ Operator Implementation

```rust
// Secure @ operator wrapper
struct SecureOperatorWrapper {
    security_manager: SecurityManager,
    database_operator: SecureDatabaseOperator,
    file_operator: SecureFileOperator,
    encryption_manager: EncryptionManager,
    ddos_protector: DdosProtector,
}

impl SecureOperatorWrapper {
    fn new() -> Self {
        let encryption_key = [0u8; 32]; // In production, load from secure environment
        let connection_pool = sqlx::PgPool::connect("postgresql://user:pass@localhost/db").await.unwrap();
        
        Self {
            security_manager: SecurityManager::new(),
            database_operator: SecureDatabaseOperator::new(connection_pool),
            file_operator: SecureFileOperator::new(),
            encryption_manager: EncryptionManager::new(encryption_key),
            ddos_protector: DdosProtector::new(),
        }
    }
    
    // Secure @query operator
    async fn secure_query(&self, sql: &str, params: Vec<Value>, context: &SecurityContext) -> Result<Vec<Value>, SecurityError> {
        // Check DDoS protection
        self.ddos_protector.check_request(&context.ip_address, &context.user_agent, sql)?;
        
        // Authorize operation
        self.security_manager.authorize_operation("database_query", context)?;
        
        // Execute secure query
        self.database_operator.secure_query(sql, params, context).await
    }
    
    // Secure @file operator
    async fn secure_file(&self, operation: &str, path: &str, content: Option<Vec<u8>>, context: &SecurityContext) -> Result<Value, SecurityError> {
        // Check DDoS protection
        self.ddos_protector.check_request(&context.ip_address, &context.user_agent, path)?;
        
        // Authorize operation
        self.security_manager.authorize_operation("file_operation", context)?;
        
        match operation {
            "read" => {
                let content = self.file_operator.secure_file_read(path, context)?;
                Ok(Value::String(base64::encode(content)))
            }
            "write" => {
                let content = content.ok_or(SecurityError::InputValidationError {
                    message: "Content required for write operation".to_string(),
                })?;
                let filename = self.file_operator.secure_file_upload(path, &content, context)?;
                Ok(Value::String(filename))
            }
            _ => Err(SecurityError::InputValidationError {
                message: format!("Unknown file operation: {}", operation)
            })
        }
    }
    
    // Secure @encrypt operator
    fn secure_encrypt(&self, data: &str, context: &SecurityContext) -> Result<Value, SecurityError> {
        // Authorize operation
        self.security_manager.authorize_operation("encryption", context)?;
        
        let encrypted = self.encryption_manager.encrypt_sensitive_data(data)?;
        Ok(Value::String(encrypted))
    }
    
    // Secure @decrypt operator
    fn secure_decrypt(&self, encrypted_data: &str, context: &SecurityContext) -> Result<Value, SecurityError> {
        // Authorize operation
        self.security_manager.authorize_operation("decryption", context)?;
        
        let decrypted = self.encryption_manager.decrypt_sensitive_data(encrypted_data)?;
        Ok(Value::String(decrypted))
    }
}
```

## Best Practices

### 1. Always Validate Input
```rust
// Validate all user input
fn secure_operator_call(operator: &str, args: Vec<Value>, context: &SecurityContext) -> Result<Value, SecurityError> {
    let validator = SecureInputValidator::new();
    
    // Validate operator name
    let sanitized_operator = validator.validate_string_input(operator, "operator")?;
    
    // Validate arguments
    for (i, arg) in args.iter().enumerate() {
        if let Value::String(s) = arg {
            validator.validate_string_input(s, "argument")?;
        }
    }
    
    // Execute operator with validated inputs
    execute_operator(&sanitized_operator, args, context)
}
```

### 2. Use Parameterized Queries
```rust
// Always use parameterized queries
async fn secure_database_operation(sql: &str, params: Vec<Value>) -> Result<Vec<Value>, SecurityError> {
    // Use prepared statements
    let statement = connection.prepare(sql).await?;
    let result = statement.query_with(params).fetch_all(&connection).await?;
    
    Ok(result.into_iter().map(|row| row.into()).collect())
}
```

### 3. Implement Proper Authentication
```rust
// Always check authentication
fn secure_operation(operation: &str, context: &SecurityContext) -> Result<Value, SecurityError> {
    // Verify user is authenticated
    if context.user_id.is_none() {
        return Err(SecurityError::AuthenticationRequired);
    }
    
    // Check session validity
    if let Some(token) = &context.session_token {
        if !@validate_session_token(token)? {
            return Err(SecurityError::SessionExpired);
        }
    }
    
    // Execute operation
    execute_operation(operation, context)
}
```

### 4. Audit All Operations
```rust
// Audit all security-sensitive operations
fn audited_operation(operation: &str, context: &SecurityContext) -> Result<Value, SecurityError> {
    let start_time = std::time::Instant::now();
    
    let result = execute_operation(operation, context);
    
    // Log audit entry
    let audit_entry = AuditLog {
        timestamp: chrono::Utc::now(),
        user_id: context.user_id,
        operation: operation.to_string(),
        ip_address: context.ip_address.clone(),
        user_agent: context.user_agent.clone(),
        success: result.is_ok(),
        duration: start_time.elapsed(),
    };
    
    @save_audit_log(audit_entry)?;
    
    result
}
```

### 5. Use Encryption for Sensitive Data
```rust
// Encrypt sensitive data
fn secure_data_storage(data: &str, context: &SecurityContext) -> Result<Value, SecurityError> {
    let encryption_manager = EncryptionManager::new([0u8; 32]);
    
    // Encrypt sensitive data before storage
    let encrypted = encryption_manager.encrypt_sensitive_data(data)?;
    
    // Store encrypted data
    @store_encrypted_data(encrypted, context)?;
    
    Ok(Value::String("Data stored securely".to_string()))
}
```

The @ operator security in Rust provides comprehensive protection against common vulnerabilities while maintaining high performance and usability. 