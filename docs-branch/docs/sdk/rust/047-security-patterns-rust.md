# Security Patterns in TuskLang with Rust

## 🔒 Security Foundation

Security patterns with TuskLang and Rust provide robust protection for applications. This guide covers authentication, authorization, encryption, and comprehensive security best practices.

## 🏗️ Security Architecture

### Security Stack

```rust
[security_architecture]
authentication: true
authorization: true
encryption: true
input_validation: true

[security_components]
argon2: "password_hashing"
jwt: "token_management"
bcrypt: "password_verification"
aes: "encryption"
```

### Security Configuration

```rust
[security_configuration]
enable_ssl: true
enable_rate_limiting: true
enable_input_validation: true
enable_audit_logging: true

[security_implementation]
use argon2::{Argon2, PasswordHash, PasswordHasher, PasswordVerifier};
use jsonwebtoken::{decode, encode, DecodingKey, EncodingKey, Header, Validation};
use serde::{Deserialize, Serialize};
use std::collections::HashMap;
use tokio::sync::RwLock;

// Security manager
pub struct SecurityManager {
    config: SecurityConfig,
    rate_limiters: Arc<RwLock<HashMap<String, RateLimiter>>>,
    audit_log: Arc<RwLock<Vec<AuditEvent>>>,
    blacklisted_tokens: Arc<RwLock<HashSet<String>>>,
}

#[derive(Debug, Clone)]
pub struct SecurityConfig {
    pub jwt_secret: String,
    pub jwt_expiration: Duration,
    pub password_min_length: usize,
    pub max_login_attempts: u32,
    pub lockout_duration: Duration,
    pub enable_rate_limiting: bool,
    pub rate_limit_requests: u32,
    pub rate_limit_window: Duration,
}

#[derive(Debug, Clone)]
pub struct RateLimiter {
    pub requests: Vec<Instant>,
    pub limit: u32,
    pub window: Duration,
}

#[derive(Debug, Clone)]
pub struct AuditEvent {
    pub timestamp: Instant,
    pub user_id: Option<String>,
    pub action: String,
    pub resource: String,
    pub ip_address: String,
    pub success: bool,
    pub details: String,
}

impl SecurityManager {
    pub fn new(config: SecurityConfig) -> Self {
        Self {
            config,
            rate_limiters: Arc::new(RwLock::new(HashMap::new())),
            audit_log: Arc::new(RwLock::new(Vec::new())),
            blacklisted_tokens: Arc::new(RwLock::new(HashSet::new())),
        }
    }
    
    pub async fn hash_password(&self, password: &str) -> Result<String, SecurityError> {
        if password.len() < self.config.password_min_length {
            return Err(SecurityError::PasswordTooShort { 
                min_length: self.config.password_min_length 
            });
        }
        
        let argon2 = Argon2::default();
        let salt = rand::random::<[u8; 16]>();
        let hash = argon2.hash_password(password.as_bytes(), &salt)
            .map_err(|e| SecurityError::HashingError { message: e.to_string() })?;
        
        Ok(hash.to_string())
    }
    
    pub async fn verify_password(&self, password: &str, hash: &str) -> Result<bool, SecurityError> {
        let parsed_hash = PasswordHash::new(hash)
            .map_err(|e| SecurityError::HashParsingError { message: e.to_string() })?;
        
        let argon2 = Argon2::default();
        let result = argon2.verify_password(password.as_bytes(), &parsed_hash)
            .map_err(|e| SecurityError::VerificationError { message: e.to_string() })?;
        
        Ok(result.is_ok())
    }
    
    pub async fn generate_jwt(&self, claims: &JwtClaims) -> Result<String, SecurityError> {
        let header = Header::default();
        let encoding_key = EncodingKey::from_secret(self.config.jwt_secret.as_ref());
        
        encode(&header, claims, &encoding_key)
            .map_err(|e| SecurityError::JwtError { message: e.to_string() })
    }
    
    pub async fn verify_jwt(&self, token: &str) -> Result<JwtClaims, SecurityError> {
        // Check if token is blacklisted
        {
            let blacklisted = self.blacklisted_tokens.read().await;
            if blacklisted.contains(token) {
                return Err(SecurityError::TokenBlacklisted);
            }
        }
        
        let decoding_key = DecodingKey::from_secret(self.config.jwt_secret.as_ref());
        let validation = Validation::default();
        
        let token_data = decode::<JwtClaims>(token, &decoding_key, &validation)
            .map_err(|e| SecurityError::JwtError { message: e.to_string() })?;
        
        Ok(token_data.claims)
    }
    
    pub async fn blacklist_token(&self, token: &str) {
        self.blacklisted_tokens.write().await.insert(token.to_string());
    }
    
    pub async fn check_rate_limit(&self, identifier: &str) -> Result<bool, SecurityError> {
        if !self.config.enable_rate_limiting {
            return Ok(true);
        }
        
        let mut limiters = self.rate_limiters.write().await;
        let limiter = limiters.entry(identifier.to_string()).or_insert_with(|| RateLimiter {
            requests: Vec::new(),
            limit: self.config.rate_limit_requests,
            window: self.config.rate_limit_window,
        });
        
        let now = Instant::now();
        
        // Remove expired requests
        limiter.requests.retain(|&time| now.duration_since(time) < limiter.window);
        
        // Check if limit exceeded
        if limiter.requests.len() >= limiter.limit as usize {
            return Err(SecurityError::RateLimitExceeded);
        }
        
        // Add current request
        limiter.requests.push(now);
        
        Ok(true)
    }
    
    pub async fn log_audit_event(&self, event: AuditEvent) {
        let mut audit_log = self.audit_log.write().await;
        audit_log.push(event);
        
        // Keep only last 10000 events
        if audit_log.len() > 10000 {
            audit_log.remove(0);
        }
    }
    
    pub async fn get_audit_events(&self, user_id: Option<&str>) -> Vec<AuditEvent> {
        let audit_log = self.audit_log.read().await;
        
        if let Some(user_id) = user_id {
            audit_log.iter()
                .filter(|event| event.user_id.as_deref() == Some(user_id))
                .cloned()
                .collect()
        } else {
            audit_log.clone()
        }
    }
}

#[derive(Debug, Serialize, Deserialize)]
pub struct JwtClaims {
    pub sub: String, // Subject (user ID)
    pub exp: u64,    // Expiration time
    pub iat: u64,    // Issued at
    pub role: String, // User role
}

#[derive(Debug, thiserror::Error)]
pub enum SecurityError {
    #[error("Password too short. Minimum length: {min_length}")]
    PasswordTooShort { min_length: usize },
    #[error("Hashing error: {message}")]
    HashingError { message: String },
    #[error("Hash parsing error: {message}")]
    HashParsingError { message: String },
    #[error("Verification error: {message}")]
    VerificationError { message: String },
    #[error("JWT error: {message}")]
    JwtError { message: String },
    #[error("Token is blacklisted")]
    TokenBlacklisted,
    #[error("Rate limit exceeded")]
    RateLimitExceeded,
    #[error("Invalid input: {message}")]
    InvalidInput { message: String },
}
```

## 🔐 Authentication Patterns

### Multi-Factor Authentication

```rust
[authentication_patterns]
mfa: true
oauth: true
sso: true
biometric: true

[authentication_implementation]
use totp_rs::{Algorithm, TOTP};

// Multi-factor authentication
pub struct MultiFactorAuth {
    security_manager: Arc<SecurityManager>,
    totp_secrets: Arc<RwLock<HashMap<String, String>>>,
    backup_codes: Arc<RwLock<HashMap<String, Vec<String>>>>,
}

impl MultiFactorAuth {
    pub fn new(security_manager: Arc<SecurityManager>) -> Self {
        Self {
            security_manager,
            totp_secrets: Arc::new(RwLock::new(HashMap::new())),
            backup_codes: Arc::new(RwLock::new(HashMap::new())),
        }
    }
    
    pub async fn setup_totp(&self, user_id: &str) -> Result<String, SecurityError> {
        // Generate TOTP secret
        let secret = totp_rs::Secret::generate_secret();
        let secret_str = secret.to_string();
        
        // Store secret
        {
            let mut secrets = self.totp_secrets.write().await;
            secrets.insert(user_id.to_string(), secret_str.clone());
        }
        
        // Generate backup codes
        let backup_codes = self.generate_backup_codes();
        {
            let mut codes = self.backup_codes.write().await;
            codes.insert(user_id.to_string(), backup_codes);
        }
        
        Ok(secret_str)
    }
    
    pub async fn verify_totp(&self, user_id: &str, code: &str) -> Result<bool, SecurityError> {
        let secret = {
            let secrets = self.totp_secrets.read().await;
            secrets.get(user_id).cloned()
                .ok_or_else(|| SecurityError::InvalidInput { message: "TOTP not setup".to_string() })?
        };
        
        let totp = TOTP::new(
            Algorithm::SHA1,
            6,
            1,
            30,
            secret.into(),
        ).map_err(|e| SecurityError::InvalidInput { message: e.to_string() })?;
        
        let is_valid = totp.verify_current(code)
            .map_err(|e| SecurityError::InvalidInput { message: e.to_string() })?;
        
        Ok(is_valid)
    }
    
    pub async fn verify_backup_code(&self, user_id: &str, code: &str) -> Result<bool, SecurityError> {
        let mut codes = self.backup_codes.write().await;
        
        if let Some(user_codes) = codes.get_mut(user_id) {
            if let Some(index) = user_codes.iter().position(|c| c == code) {
                user_codes.remove(index);
                Ok(true)
            } else {
                Ok(false)
            }
        } else {
            Ok(false)
        }
    }
    
    fn generate_backup_codes(&self) -> Vec<String> {
        (0..10).map(|_| {
            use rand::Rng;
            let mut rng = rand::thread_rng();
            format!("{:08}", rng.gen_range(0..100000000))
        }).collect()
    }
}

// OAuth2 implementation
pub struct OAuth2Provider {
    client_id: String,
    client_secret: String,
    redirect_uri: String,
    auth_url: String,
    token_url: String,
}

impl OAuth2Provider {
    pub fn new(client_id: String, client_secret: String, redirect_uri: String) -> Self {
        Self {
            client_id,
            client_secret,
            redirect_uri,
            auth_url: "https://accounts.google.com/o/oauth2/auth".to_string(),
            token_url: "https://oauth2.googleapis.com/token".to_string(),
        }
    }
    
    pub fn get_auth_url(&self, state: &str) -> String {
        format!(
            "{}?client_id={}&redirect_uri={}&response_type=code&scope=openid%20email%20profile&state={}",
            self.auth_url, self.client_id, self.redirect_uri, state
        )
    }
    
    pub async fn exchange_code_for_token(&self, code: &str) -> Result<OAuthToken, SecurityError> {
        let client = reqwest::Client::new();
        let params = [
            ("client_id", &self.client_id),
            ("client_secret", &self.client_secret),
            ("code", &code.to_string()),
            ("grant_type", &"authorization_code".to_string()),
            ("redirect_uri", &self.redirect_uri),
        ];
        
        let response = client.post(&self.token_url)
            .form(&params)
            .send()
            .await
            .map_err(|e| SecurityError::InvalidInput { message: e.to_string() })?;
        
        let token: OAuthToken = response.json().await
            .map_err(|e| SecurityError::InvalidInput { message: e.to_string() })?;
        
        Ok(token)
    }
}

#[derive(Debug, Deserialize)]
pub struct OAuthToken {
    pub access_token: String,
    pub token_type: String,
    pub expires_in: u64,
    pub refresh_token: Option<String>,
    pub id_token: Option<String>,
}
```

## 🛡️ Authorization Patterns

### Role-Based Access Control

```rust
[authorization_patterns]
rbac: true
abac: true
permission_matrix: true

[authorization_implementation]
use std::collections::{HashMap, HashSet};

// Role-based access control
pub struct RBACManager {
    roles: Arc<RwLock<HashMap<String, Role>>>,
    user_roles: Arc<RwLock<HashMap<String, HashSet<String>>>>,
    permissions: Arc<RwLock<HashMap<String, HashSet<String>>>>,
}

#[derive(Debug, Clone)]
pub struct Role {
    pub name: String,
    pub description: String,
    pub permissions: HashSet<String>,
    pub inherits_from: Vec<String>,
}

impl RBACManager {
    pub fn new() -> Self {
        Self {
            roles: Arc::new(RwLock::new(HashMap::new())),
            user_roles: Arc::new(RwLock::new(HashMap::new())),
            permissions: Arc::new(RwLock::new(HashMap::new())),
        }
    }
    
    pub async fn create_role(&self, name: &str, description: &str, permissions: Vec<String>) -> Result<(), SecurityError> {
        let mut roles = self.roles.write().await;
        
        if roles.contains_key(name) {
            return Err(SecurityError::InvalidInput { message: "Role already exists".to_string() });
        }
        
        let role = Role {
            name: name.to_string(),
            description: description.to_string(),
            permissions: permissions.into_iter().collect(),
            inherits_from: Vec::new(),
        };
        
        roles.insert(name.to_string(), role);
        Ok(())
    }
    
    pub async fn assign_role(&self, user_id: &str, role_name: &str) -> Result<(), SecurityError> {
        // Verify role exists
        {
            let roles = self.roles.read().await;
            if !roles.contains_key(role_name) {
                return Err(SecurityError::InvalidInput { message: "Role does not exist".to_string() });
            }
        }
        
        let mut user_roles = self.user_roles.write().await;
        let user_roles_set = user_roles.entry(user_id.to_string()).or_insert_with(HashSet::new);
        user_roles_set.insert(role_name.to_string());
        
        Ok(())
    }
    
    pub async fn check_permission(&self, user_id: &str, permission: &str) -> Result<bool, SecurityError> {
        let user_roles = {
            let user_roles = self.user_roles.read().await;
            user_roles.get(user_id).cloned().unwrap_or_default()
        };
        
        let roles = self.roles.read().await;
        
        for role_name in user_roles {
            if let Some(role) = roles.get(&role_name) {
                if role.permissions.contains(permission) {
                    return Ok(true);
                }
                
                // Check inherited roles
                for inherited_role in &role.inherits_from {
                    if let Some(inherited) = roles.get(inherited_role) {
                        if inherited.permissions.contains(permission) {
                            return Ok(true);
                        }
                    }
                }
            }
        }
        
        Ok(false)
    }
    
    pub async fn get_user_permissions(&self, user_id: &str) -> HashSet<String> {
        let user_roles = {
            let user_roles = self.user_roles.read().await;
            user_roles.get(user_id).cloned().unwrap_or_default()
        };
        
        let roles = self.roles.read().await;
        let mut permissions = HashSet::new();
        
        for role_name in user_roles {
            if let Some(role) = roles.get(&role_name) {
                permissions.extend(role.permissions.clone());
                
                // Add inherited permissions
                for inherited_role in &role.inherits_from {
                    if let Some(inherited) = roles.get(inherited_role) {
                        permissions.extend(inherited.permissions.clone());
                    }
                }
            }
        }
        
        permissions
    }
}

// Attribute-based access control
pub struct ABACManager {
    policies: Arc<RwLock<Vec<Policy>>>,
}

#[derive(Debug, Clone)]
pub struct Policy {
    pub name: String,
    pub effect: PolicyEffect,
    pub conditions: Vec<Condition>,
    pub resources: Vec<String>,
    pub actions: Vec<String>,
}

#[derive(Debug, Clone)]
pub enum PolicyEffect {
    Allow,
    Deny,
}

#[derive(Debug, Clone)]
pub struct Condition {
    pub attribute: String,
    pub operator: ConditionOperator,
    pub value: String,
}

#[derive(Debug, Clone)]
pub enum ConditionOperator {
    Equals,
    NotEquals,
    GreaterThan,
    LessThan,
    Contains,
    StartsWith,
    EndsWith,
}

impl ABACManager {
    pub fn new() -> Self {
        Self {
            policies: Arc::new(RwLock::new(Vec::new())),
        }
    }
    
    pub async fn add_policy(&self, policy: Policy) {
        let mut policies = self.policies.write().await;
        policies.push(policy);
    }
    
    pub async fn evaluate_access(&self, request: &AccessRequest) -> Result<bool, SecurityError> {
        let policies = self.policies.read().await;
        
        for policy in policies.iter() {
            if self.matches_policy(request, policy).await? {
                return Ok(matches!(policy.effect, PolicyEffect::Allow));
            }
        }
        
        // Default deny
        Ok(false)
    }
    
    async fn matches_policy(&self, request: &AccessRequest, policy: &Policy) -> Result<bool, SecurityError> {
        // Check if resource matches
        if !policy.resources.iter().any(|r| request.resource.starts_with(r)) {
            return Ok(false);
        }
        
        // Check if action matches
        if !policy.actions.contains(&request.action) {
            return Ok(false);
        }
        
        // Check conditions
        for condition in &policy.conditions {
            if !self.evaluate_condition(request, condition).await? {
                return Ok(false);
            }
        }
        
        Ok(true)
    }
    
    async fn evaluate_condition(&self, request: &AccessRequest, condition: &Condition) -> Result<bool, SecurityError> {
        let attribute_value = request.attributes.get(&condition.attribute)
            .ok_or_else(|| SecurityError::InvalidInput { message: "Attribute not found".to_string() })?;
        
        match condition.operator {
            ConditionOperator::Equals => Ok(attribute_value == &condition.value),
            ConditionOperator::NotEquals => Ok(attribute_value != &condition.value),
            ConditionOperator::GreaterThan => {
                let attr_val: f64 = attribute_value.parse().unwrap_or(0.0);
                let cond_val: f64 = condition.value.parse().unwrap_or(0.0);
                Ok(attr_val > cond_val)
            }
            ConditionOperator::LessThan => {
                let attr_val: f64 = attribute_value.parse().unwrap_or(0.0);
                let cond_val: f64 = condition.value.parse().unwrap_or(0.0);
                Ok(attr_val < cond_val)
            }
            ConditionOperator::Contains => Ok(attribute_value.contains(&condition.value)),
            ConditionOperator::StartsWith => Ok(attribute_value.starts_with(&condition.value)),
            ConditionOperator::EndsWith => Ok(attribute_value.ends_with(&condition.value)),
        }
    }
}

#[derive(Debug)]
pub struct AccessRequest {
    pub user_id: String,
    pub resource: String,
    pub action: String,
    pub attributes: HashMap<String, String>,
}
```

## 🔐 Encryption Patterns

### Data Encryption

```rust
[encryption_patterns]
symmetric_encryption: true
asymmetric_encryption: true
key_management: true

[encryption_implementation]
use aes_gcm::{Aes256Gcm, Key, Nonce};
use aes_gcm::aead::{Aead, NewAead};
use rsa::{PublicKey, PrivateKey, RsaPrivateKey, RsaPublicKey};
use rsa::pkcs8::{EncodePublicKey, LineEnding};
use rand::Rng;

// Encryption manager
pub struct EncryptionManager {
    master_key: Vec<u8>,
    key_store: Arc<RwLock<HashMap<String, Vec<u8>>>>,
    config: EncryptionConfig,
}

#[derive(Debug, Clone)]
pub struct EncryptionConfig {
    pub algorithm: EncryptionAlgorithm,
    pub key_size: usize,
    pub enable_key_rotation: bool,
    pub key_rotation_interval: Duration,
}

#[derive(Debug, Clone)]
pub enum EncryptionAlgorithm {
    AES256GCM,
    ChaCha20Poly1305,
    RSA2048,
    RSA4096,
}

impl EncryptionManager {
    pub fn new(config: EncryptionConfig) -> Result<Self, SecurityError> {
        let master_key = Self::generate_master_key(&config)?;
        
        Ok(Self {
            master_key,
            key_store: Arc::new(RwLock::new(HashMap::new())),
            config,
        })
    }
    
    pub async fn encrypt_data(&self, data: &[u8], key_id: Option<&str>) -> Result<EncryptedData, SecurityError> {
        let key = if let Some(key_id) = key_id {
            self.get_key(key_id).await?
        } else {
            self.master_key.clone()
        };
        
        match self.config.algorithm {
            EncryptionAlgorithm::AES256GCM => self.encrypt_aes_gcm(data, &key).await,
            EncryptionAlgorithm::ChaCha20Poly1305 => self.encrypt_chacha20(data, &key).await,
            EncryptionAlgorithm::RSA2048 | EncryptionAlgorithm::RSA4096 => self.encrypt_rsa(data).await,
        }
    }
    
    pub async fn decrypt_data(&self, encrypted_data: &EncryptedData, key_id: Option<&str>) -> Result<Vec<u8>, SecurityError> {
        let key = if let Some(key_id) = key_id {
            self.get_key(key_id).await?
        } else {
            self.master_key.clone()
        };
        
        match self.config.algorithm {
            EncryptionAlgorithm::AES256GCM => self.decrypt_aes_gcm(encrypted_data, &key).await,
            EncryptionAlgorithm::ChaCha20Poly1305 => self.decrypt_chacha20(encrypted_data, &key).await,
            EncryptionAlgorithm::RSA2048 | EncryptionAlgorithm::RSA4096 => self.decrypt_rsa(encrypted_data).await,
        }
    }
    
    async fn encrypt_aes_gcm(&self, data: &[u8], key: &[u8]) -> Result<EncryptedData, SecurityError> {
        let cipher = Aes256Gcm::new_from_slice(key)
            .map_err(|e| SecurityError::InvalidInput { message: e.to_string() })?;
        
        let nonce = Aes256Gcm::generate_nonce(&mut rand::thread_rng());
        let ciphertext = cipher.encrypt(&nonce, data)
            .map_err(|e| SecurityError::InvalidInput { message: e.to_string() })?;
        
        Ok(EncryptedData {
            algorithm: "AES256GCM".to_string(),
            ciphertext,
            nonce: Some(nonce.to_vec()),
            key_id: None,
        })
    }
    
    async fn decrypt_aes_gcm(&self, encrypted_data: &EncryptedData, key: &[u8]) -> Result<Vec<u8>, SecurityError> {
        let cipher = Aes256Gcm::new_from_slice(key)
            .map_err(|e| SecurityError::InvalidInput { message: e.to_string() })?;
        
        let nonce = Nonce::from_slice(&encrypted_data.nonce.as_ref().unwrap());
        let plaintext = cipher.decrypt(nonce, encrypted_data.ciphertext.as_ref())
            .map_err(|e| SecurityError::InvalidInput { message: e.to_string() })?;
        
        Ok(plaintext)
    }
    
    async fn encrypt_chacha20(&self, data: &[u8], key: &[u8]) -> Result<EncryptedData, SecurityError> {
        // Simplified implementation - would use chacha20poly1305 crate in production
        let mut ciphertext = data.to_vec();
        for (i, byte) in ciphertext.iter_mut().enumerate() {
            *byte ^= key[i % key.len()];
        }
        
        Ok(EncryptedData {
            algorithm: "ChaCha20Poly1305".to_string(),
            ciphertext,
            nonce: None,
            key_id: None,
        })
    }
    
    async fn decrypt_chacha20(&self, encrypted_data: &EncryptedData, key: &[u8]) -> Result<Vec<u8>, SecurityError> {
        // Simplified implementation - would use chacha20poly1305 crate in production
        let mut plaintext = encrypted_data.ciphertext.clone();
        for (i, byte) in plaintext.iter_mut().enumerate() {
            *byte ^= key[i % key.len()];
        }
        
        Ok(plaintext)
    }
    
    async fn encrypt_rsa(&self, data: &[u8]) -> Result<EncryptedData, SecurityError> {
        // Simplified RSA implementation
        let mut ciphertext = data.to_vec();
        for (i, byte) in ciphertext.iter_mut().enumerate() {
            *byte = (*byte as u32).pow(3) as u8; // Simplified RSA-like operation
        }
        
        Ok(EncryptedData {
            algorithm: "RSA".to_string(),
            ciphertext,
            nonce: None,
            key_id: None,
        })
    }
    
    async fn decrypt_rsa(&self, encrypted_data: &EncryptedData) -> Result<Vec<u8>, SecurityError> {
        // Simplified RSA implementation
        let mut plaintext = encrypted_data.ciphertext.clone();
        for (i, byte) in plaintext.iter_mut().enumerate() {
            *byte = (*byte as f64).powf(1.0/3.0) as u8; // Simplified RSA-like operation
        }
        
        Ok(plaintext)
    }
    
    fn generate_master_key(config: &EncryptionConfig) -> Result<Vec<u8>, SecurityError> {
        let mut key = vec![0u8; config.key_size];
        rand::thread_rng().fill(&mut key);
        Ok(key)
    }
    
    async fn get_key(&self, key_id: &str) -> Result<Vec<u8>, SecurityError> {
        let key_store = self.key_store.read().await;
        key_store.get(key_id).cloned()
            .ok_or_else(|| SecurityError::InvalidInput { message: "Key not found".to_string() })
    }
    
    pub async fn generate_key_pair(&self) -> Result<(String, String), SecurityError> {
        let mut rng = rand::thread_rng();
        let private_key = RsaPrivateKey::new(&mut rng, 2048)
            .map_err(|e| SecurityError::InvalidInput { message: e.to_string() })?;
        
        let public_key = RsaPublicKey::from(&private_key);
        
        let private_key_pem = private_key.to_pkcs8_pem(LineEnding::LF)
            .map_err(|e| SecurityError::InvalidInput { message: e.to_string() })?;
        
        let public_key_pem = public_key.to_public_key_pem(LineEnding::LF)
            .map_err(|e| SecurityError::InvalidInput { message: e.to_string() })?;
        
        Ok((private_key_pem, public_key_pem))
    }
}

#[derive(Debug, Clone)]
pub struct EncryptedData {
    pub algorithm: String,
    pub ciphertext: Vec<u8>,
    pub nonce: Option<Vec<u8>>,
    pub key_id: Option<String>,
}
```

## 🎯 Best Practices

### 1. **Authentication Security**
- Use strong password policies and multi-factor authentication
- Implement proper session management and token rotation
- Use secure password hashing algorithms (Argon2, bcrypt)
- Implement account lockout mechanisms

### 2. **Authorization Security**
- Follow the principle of least privilege
- Implement role-based and attribute-based access control
- Regularly audit permissions and access patterns
- Use secure token validation and blacklisting

### 3. **Data Protection**
- Encrypt sensitive data at rest and in transit
- Use strong encryption algorithms and key management
- Implement proper key rotation and secure key storage
- Use secure random number generation

### 4. **Input Validation**
- Validate and sanitize all user inputs
- Implement proper SQL injection and XSS protection
- Use parameterized queries and input encoding
- Implement rate limiting and DDoS protection

### 5. **TuskLang Integration**
- Use TuskLang configuration for security parameters
- Implement security monitoring with TuskLang
- Configure authentication and authorization through TuskLang
- Use TuskLang for security policy management

Security patterns with TuskLang and Rust provide comprehensive protection for applications with robust authentication, authorization, and encryption capabilities. 