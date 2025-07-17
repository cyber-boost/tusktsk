# Security in TuskLang - Go Guide

## 🎯 **The Power of Secure Configuration**

In TuskLang, security isn't just an afterthought—it's built into the core. We don't bow to any king, especially not security vulnerabilities. TuskLang gives you the power to handle sensitive data securely with built-in encryption, secure environment variables, and comprehensive security features.

## 📋 **Table of Contents**
- [Understanding Security in TuskLang](#understanding-security-in-tusklang)
- [Encryption Features](#encryption-features)
- [Secure Environment Variables](#secure-environment-variables)
- [Access Control](#access-control)
- [Go Integration](#go-integration)
- [Security Patterns](#security-patterns)
- [Performance Considerations](#performance-considerations)
- [Real-World Examples](#real-world-examples)
- [Best Practices](#best-practices)

## 🔍 **Understanding Security in TuskLang**

TuskLang provides comprehensive security features for handling sensitive data:

```go
// TuskLang - Security configuration
[security_config]
api_key: @env.secure("API_KEY")
database_password: @encrypt("sensitive_password", "AES-256-GCM")
jwt_secret: @env.secure("JWT_SECRET")
ssl_cert: @file.secure("ssl/cert.pem")
private_key: @file.secure("ssl/private.key")
```

```go
// Go integration
type SecurityConfig struct {
    APIKey           string `tsk:"api_key"`
    DatabasePassword string `tsk:"database_password"`
    JWTSecret        string `tsk:"jwt_secret"`
    SSLCert          string `tsk:"ssl_cert"`
    PrivateKey       string `tsk:"private_key"`
}
```

## 🔐 **Encryption Features**

### **Data Encryption**

```go
// TuskLang - Data encryption
[encryption_config]
sensitive_data: @encrypt("secret_value", "AES-256-GCM")
user_password: @encrypt("user_password", "AES-256-GCM")
api_credentials: @encrypt("api_credentials", "ChaCha20-Poly1305")
config_secret: @encrypt("config_secret", "AES-256-CBC")
```

```go
// Go - Data encryption handling
type EncryptionConfig struct {
    SensitiveData   string `tsk:"sensitive_data"`
    UserPassword    string `tsk:"user_password"`
    APICredentials  string `tsk:"api_credentials"`
    ConfigSecret    string `tsk:"config_secret"`
}

type Encryptor struct {
    key []byte
}

func NewEncryptor(key []byte) *Encryptor {
    return &Encryptor{key: key}
}

func (e *Encryptor) Encrypt(data string, algorithm string) (string, error) {
    switch algorithm {
    case "AES-256-GCM":
        return e.encryptAESGCM(data)
    case "ChaCha20-Poly1305":
        return e.encryptChaCha20(data)
    case "AES-256-CBC":
        return e.encryptAESCBC(data)
    default:
        return "", fmt.Errorf("unsupported encryption algorithm: %s", algorithm)
    }
}

func (e *Encryptor) Decrypt(encryptedData string, algorithm string) (string, error) {
    switch algorithm {
    case "AES-256-GCM":
        return e.decryptAESGCM(encryptedData)
    case "ChaCha20-Poly1305":
        return e.decryptChaCha20(encryptedData)
    case "AES-256-CBC":
        return e.decryptAESCBC(encryptedData)
    default:
        return "", fmt.Errorf("unsupported encryption algorithm: %s", algorithm)
    }
}

func (e *Encryptor) encryptAESGCM(data string) (string, error) {
    block, err := aes.NewCipher(e.key)
    if err != nil {
        return "", fmt.Errorf("failed to create cipher: %w", err)
    }
    
    gcm, err := cipher.NewGCM(block)
    if err != nil {
        return "", fmt.Errorf("failed to create GCM: %w", err)
    }
    
    nonce := make([]byte, gcm.NonceSize())
    if _, err := io.ReadFull(rand.Reader, nonce); err != nil {
        return "", fmt.Errorf("failed to generate nonce: %w", err)
    }
    
    ciphertext := gcm.Seal(nonce, nonce, []byte(data), nil)
    return base64.StdEncoding.EncodeToString(ciphertext), nil
}

func (e *Encryptor) decryptAESGCM(encryptedData string) (string, error) {
    data, err := base64.StdEncoding.DecodeString(encryptedData)
    if err != nil {
        return "", fmt.Errorf("failed to decode base64: %w", err)
    }
    
    block, err := aes.NewCipher(e.key)
    if err != nil {
        return "", fmt.Errorf("failed to create cipher: %w", err)
    }
    
    gcm, err := cipher.NewGCM(block)
    if err != nil {
        return "", fmt.Errorf("failed to create GCM: %w", err)
    }
    
    nonceSize := gcm.NonceSize()
    if len(data) < nonceSize {
        return "", errors.New("ciphertext too short")
    }
    
    nonce, ciphertext := data[:nonceSize], data[nonceSize:]
    plaintext, err := gcm.Open(nil, nonce, ciphertext, nil)
    if err != nil {
        return "", fmt.Errorf("failed to decrypt: %w", err)
    }
    
    return string(plaintext), nil
}

func (e *Encryptor) encryptChaCha20(data string) (string, error) {
    // Simplified ChaCha20-Poly1305 implementation
    // In production, use crypto/aead package
    return e.encryptAESGCM(data) // Fallback for demo
}

func (e *Encryptor) decryptChaCha20(encryptedData string) (string, error) {
    return e.decryptAESGCM(encryptedData) // Fallback for demo
}

func (e *Encryptor) encryptAESCBC(data string) (string, error) {
    block, err := aes.NewCipher(e.key)
    if err != nil {
        return "", fmt.Errorf("failed to create cipher: %w", err)
    }
    
    // Pad data to block size
    paddedData := e.pkcs7Pad([]byte(data), aes.BlockSize)
    
    ciphertext := make([]byte, aes.BlockSize+len(paddedData))
    iv := ciphertext[:aes.BlockSize]
    if _, err := io.ReadFull(rand.Reader, iv); err != nil {
        return "", fmt.Errorf("failed to generate IV: %w", err)
    }
    
    mode := cipher.NewCBCEncrypter(block, iv)
    mode.CryptBlocks(ciphertext[aes.BlockSize:], paddedData)
    
    return base64.StdEncoding.EncodeToString(ciphertext), nil
}

func (e *Encryptor) decryptAESCBC(encryptedData string) (string, error) {
    data, err := base64.StdEncoding.DecodeString(encryptedData)
    if err != nil {
        return "", fmt.Errorf("failed to decode base64: %w", err)
    }
    
    if len(data) < aes.BlockSize {
        return "", errors.New("ciphertext too short")
    }
    
    block, err := aes.NewCipher(e.key)
    if err != nil {
        return "", fmt.Errorf("failed to create cipher: %w", err)
    }
    
    iv := data[:aes.BlockSize]
    ciphertext := data[aes.BlockSize:]
    
    if len(ciphertext)%aes.BlockSize != 0 {
        return "", errors.New("ciphertext is not a multiple of the block size")
    }
    
    mode := cipher.NewCBCDecrypter(block, iv)
    plaintext := make([]byte, len(ciphertext))
    mode.CryptBlocks(plaintext, ciphertext)
    
    // Remove padding
    plaintext, err = e.pkcs7Unpad(plaintext)
    if err != nil {
        return "", fmt.Errorf("failed to remove padding: %w", err)
    }
    
    return string(plaintext), nil
}

func (e *Encryptor) pkcs7Pad(data []byte, blockSize int) []byte {
    padding := blockSize - len(data)%blockSize
    padtext := bytes.Repeat([]byte{byte(padding)}, padding)
    return append(data, padtext...)
}

func (e *Encryptor) pkcs7Unpad(data []byte) ([]byte, error) {
    length := len(data)
    if length == 0 {
        return nil, errors.New("invalid padding")
    }
    
    padding := int(data[length-1])
    if padding > length {
        return nil, errors.New("invalid padding")
    }
    
    return data[:length-padding], nil
}
```

### **Key Management**

```go
// TuskLang - Key management
[key_management]
encryption_key: @key.generate("AES-256")
signing_key: @key.generate("RSA-2048")
hmac_key: @key.generate("HMAC-SHA256")
key_rotation: @key.rotate("encryption_key", "30d")
```

```go
// Go - Key management handling
type KeyManagement struct {
    EncryptionKey string `tsk:"encryption_key"`
    SigningKey    string `tsk:"signing_key"`
    HMACKey       string `tsk:"hmac_key"`
    KeyRotation   string `tsk:"key_rotation"`
}

type KeyManager struct {
    keys map[string]KeyInfo
    mutex sync.RWMutex
}

type KeyInfo struct {
    Key       []byte
    Algorithm string
    Created   time.Time
    Expires   time.Time
}

func NewKeyManager() *KeyManager {
    return &KeyManager{
        keys: make(map[string]KeyInfo),
    }
}

func (k *KeyManager) GenerateKey(algorithm string) ([]byte, error) {
    switch algorithm {
    case "AES-256":
        key := make([]byte, 32)
        if _, err := io.ReadFull(rand.Reader, key); err != nil {
            return nil, fmt.Errorf("failed to generate AES key: %w", err)
        }
        return key, nil
    case "RSA-2048":
        privateKey, err := rsa.GenerateKey(rand.Reader, 2048)
        if err != nil {
            return nil, fmt.Errorf("failed to generate RSA key: %w", err)
        }
        return x509.MarshalPKCS1PrivateKey(privateKey), nil
    case "HMAC-SHA256":
        key := make([]byte, 32)
        if _, err := io.ReadFull(rand.Reader, key); err != nil {
            return nil, fmt.Errorf("failed to generate HMAC key: %w", err)
        }
        return key, nil
    default:
        return nil, fmt.Errorf("unsupported key algorithm: %s", algorithm)
    }
}

func (k *KeyManager) StoreKey(name string, key []byte, algorithm string, expiry time.Duration) {
    k.mutex.Lock()
    defer k.mutex.Unlock()
    
    k.keys[name] = KeyInfo{
        Key:       key,
        Algorithm: algorithm,
        Created:   time.Now(),
        Expires:   time.Now().Add(expiry),
    }
}

func (k *KeyManager) GetKey(name string) (KeyInfo, error) {
    k.mutex.RLock()
    defer k.mutex.RUnlock()
    
    keyInfo, exists := k.keys[name]
    if !exists {
        return KeyInfo{}, fmt.Errorf("key '%s' not found", name)
    }
    
    if time.Now().After(keyInfo.Expires) {
        return KeyInfo{}, fmt.Errorf("key '%s' has expired", name)
    }
    
    return keyInfo, nil
}

func (k *KeyManager) RotateKey(name string, algorithm string, expiry time.Duration) error {
    newKey, err := k.GenerateKey(algorithm)
    if err != nil {
        return fmt.Errorf("failed to generate new key: %w", err)
    }
    
    k.StoreKey(name, newKey, algorithm, expiry)
    return nil
}
```

## 🔒 **Secure Environment Variables**

### **Secure Variable Access**

```go
// TuskLang - Secure environment variables
[secure_env]
api_key: @env.secure("API_KEY")
database_password: @env.secure("DB_PASSWORD")
jwt_secret: @env.secure("JWT_SECRET")
private_key: @env.secure("PRIVATE_KEY")
```

```go
// Go - Secure environment variable handling
type SecureEnv struct {
    APIKey           string `tsk:"api_key"`
    DatabasePassword string `tsk:"database_password"`
    JWTSecret        string `tsk:"jwt_secret"`
    PrivateKey       string `tsk:"private_key"`
}

type SecureEnvManager struct {
    cache map[string]string
    mutex sync.RWMutex
}

func NewSecureEnvManager() *SecureEnvManager {
    return &SecureEnvManager{
        cache: make(map[string]string),
    }
}

func (s *SecureEnvManager) GetSecureEnv(name string) (string, error) {
    s.mutex.RLock()
    if cached, exists := s.cache[name]; exists {
        s.mutex.RUnlock()
        return cached, nil
    }
    s.mutex.RUnlock()
    
    value := os.Getenv(name)
    if value == "" {
        return "", fmt.Errorf("secure environment variable '%s' is not set", name)
    }
    
    // Validate the value is not empty or default
    if value == "default" || value == "placeholder" {
        return "", fmt.Errorf("secure environment variable '%s' has invalid value", name)
    }
    
    s.mutex.Lock()
    s.cache[name] = value
    s.mutex.Unlock()
    
    return value, nil
}

func (s *SecureEnvManager) ValidateSecureEnvs(required []string) error {
    for _, name := range required {
        if _, err := s.GetSecureEnv(name); err != nil {
            return fmt.Errorf("secure environment validation failed: %w", err)
        }
    }
    return nil
}
```

### **Environment Variable Encryption**

```go
// TuskLang - Encrypted environment variables
[encrypted_env]
encrypted_api_key: @env.encrypted("ENCRYPTED_API_KEY", "AES-256-GCM")
encrypted_password: @env.encrypted("ENCRYPTED_PASSWORD", "ChaCha20-Poly1305")
encrypted_secret: @env.encrypted("ENCRYPTED_SECRET", "AES-256-CBC")
```

```go
// Go - Encrypted environment variable handling
type EncryptedEnv struct {
    EncryptedAPIKey    string `tsk:"encrypted_api_key"`
    EncryptedPassword  string `tsk:"encrypted_password"`
    EncryptedSecret    string `tsk:"encrypted_secret"`
}

type EncryptedEnvManager struct {
    encryptor *Encryptor
    cache     map[string]string
    mutex     sync.RWMutex
}

func NewEncryptedEnvManager(key []byte) *EncryptedEnvManager {
    return &EncryptedEnvManager{
        encryptor: NewEncryptor(key),
        cache:     make(map[string]string),
    }
}

func (e *EncryptedEnvManager) GetEncryptedEnv(name string, algorithm string) (string, error) {
    e.mutex.RLock()
    if cached, exists := e.cache[name]; exists {
        e.mutex.RUnlock()
        return cached, nil
    }
    e.mutex.RUnlock()
    
    encryptedValue := os.Getenv(name)
    if encryptedValue == "" {
        return "", fmt.Errorf("encrypted environment variable '%s' is not set", name)
    }
    
    decryptedValue, err := e.encryptor.Decrypt(encryptedValue, algorithm)
    if err != nil {
        return "", fmt.Errorf("failed to decrypt environment variable '%s': %w", name, err)
    }
    
    e.mutex.Lock()
    e.cache[name] = decryptedValue
    e.mutex.Unlock()
    
    return decryptedValue, nil
}
```

## 🛡️ **Access Control**

### **Role-Based Access Control**

```go
// TuskLang - Role-based access control
[access_control]
admin_users: @access.role("admin", ["user1", "user2", "user3"])
read_users: @access.role("read", ["user4", "user5"])
write_users: @access.role("write", ["user6", "user7"])
```

```go
// Go - Role-based access control handling
type AccessControl struct {
    AdminUsers []string `tsk:"admin_users"`
    ReadUsers  []string `tsk:"read_users"`
    WriteUsers []string `tsk:"write_users"`
}

type RBACManager struct {
    roles map[string][]string
    mutex sync.RWMutex
}

func NewRBACManager() *RBACManager {
    return &RBACManager{
        roles: make(map[string][]string),
    }
}

func (r *RBACManager) AddRole(role string, users []string) {
    r.mutex.Lock()
    defer r.mutex.Unlock()
    
    r.roles[role] = users
}

func (r *RBACManager) HasRole(user string, role string) bool {
    r.mutex.RLock()
    defer r.mutex.RUnlock()
    
    users, exists := r.roles[role]
    if !exists {
        return false
    }
    
    for _, u := range users {
        if u == user {
            return true
        }
    }
    
    return false
}

func (r *RBACManager) GetUsersWithRole(role string) []string {
    r.mutex.RLock()
    defer r.mutex.RUnlock()
    
    users, exists := r.roles[role]
    if !exists {
        return []string{}
    }
    
    return append([]string{}, users...)
}
```

### **Permission-Based Access**

```go
// TuskLang - Permission-based access control
[permission_control]
read_config: @permission.allow("read", ["admin", "read"])
write_config: @permission.allow("write", ["admin", "write"])
delete_config: @permission.allow("delete", ["admin"])
```

```go
// Go - Permission-based access control handling
type PermissionControl struct {
    ReadConfig   []string `tsk:"read_config"`
    WriteConfig  []string `tsk:"write_config"`
    DeleteConfig []string `tsk:"delete_config"`
}

type PermissionManager struct {
    permissions map[string][]string
    mutex       sync.RWMutex
}

func NewPermissionManager() *PermissionManager {
    return &PermissionManager{
        permissions: make(map[string][]string),
    }
}

func (p *PermissionManager) AddPermission(action string, roles []string) {
    p.mutex.Lock()
    defer p.mutex.Unlock()
    
    p.permissions[action] = roles
}

func (p *PermissionManager) HasPermission(userRole string, action string) bool {
    p.mutex.RLock()
    defer p.mutex.RUnlock()
    
    allowedRoles, exists := p.permissions[action]
    if !exists {
        return false
    }
    
    for _, role := range allowedRoles {
        if role == userRole {
            return true
        }
    }
    
    return false
}
```

## 🔧 **Go Integration**

### **Security Manager**

```go
// Go - Security manager
type SecurityManager struct {
    encryptor    *Encryptor
    keyManager   *KeyManager
    envManager   *SecureEnvManager
    rbacManager  *RBACManager
    permManager  *PermissionManager
}

func NewSecurityManager(key []byte) *SecurityManager {
    return &SecurityManager{
        encryptor:    NewEncryptor(key),
        keyManager:   NewKeyManager(),
        envManager:   NewSecureEnvManager(),
        rbacManager:  NewRBACManager(),
        permManager:  NewPermissionManager(),
    }
}

func (s *SecurityManager) EncryptData(data string, algorithm string) (string, error) {
    return s.encryptor.Encrypt(data, algorithm)
}

func (s *SecurityManager) DecryptData(encryptedData string, algorithm string) (string, error) {
    return s.encryptor.Decrypt(encryptedData, algorithm)
}

func (s *SecurityManager) GetSecureEnv(name string) (string, error) {
    return s.envManager.GetSecureEnv(name)
}

func (s *SecurityManager) ValidateAccess(user string, action string) error {
    // Check RBAC first
    if s.rbacManager.HasRole(user, "admin") {
        return nil // Admin has all permissions
    }
    
    // Check specific permissions
    if s.permManager.HasPermission(user, action) {
        return nil
    }
    
    return fmt.Errorf("user '%s' does not have permission for action '%s'", user, action)
}
```

### **Security Middleware**

```go
// Go - Security middleware
type SecurityMiddleware struct {
    securityManager *SecurityManager
}

func NewSecurityMiddleware(securityManager *SecurityManager) *SecurityMiddleware {
    return &SecurityMiddleware{
        securityManager: securityManager,
    }
}

func (s *SecurityMiddleware) Authenticate(next http.HandlerFunc) http.HandlerFunc {
    return func(w http.ResponseWriter, r *http.Request) {
        // Extract token from request
        token := r.Header.Get("Authorization")
        if token == "" {
            http.Error(w, "Unauthorized", http.StatusUnauthorized)
            return
        }
        
        // Validate token (simplified)
        if !s.validateToken(token) {
            http.Error(w, "Invalid token", http.StatusUnauthorized)
            return
        }
        
        next.ServeHTTP(w, r)
    }
}

func (s *SecurityMiddleware) Authorize(action string) func(http.HandlerFunc) http.HandlerFunc {
    return func(next http.HandlerFunc) http.HandlerFunc {
        return func(w http.ResponseWriter, r *http.Request) {
            // Extract user from request context
            user := r.Context().Value("user").(string)
            
            if err := s.securityManager.ValidateAccess(user, action); err != nil {
                http.Error(w, "Forbidden", http.StatusForbidden)
                return
            }
            
            next.ServeHTTP(w, r)
        }
    }
}

func (s *SecurityMiddleware) validateToken(token string) bool {
    // Simplified token validation
    // In production, use proper JWT validation
    return token != ""
}
```

## 🚀 **Security Patterns**

### **Configuration Security**

```go
// TuskLang - Configuration security
[config_security]
sensitive_config: @secure.config({
    "api_key": {"encrypted": true, "algorithm": "AES-256-GCM"},
    "database_password": {"encrypted": true, "algorithm": "ChaCha20-Poly1305"},
    "jwt_secret": {"encrypted": true, "algorithm": "AES-256-GCM"},
    "ssl_private_key": {"file": true, "permissions": "600"}
})

access_control: @secure.access({
    "admin": ["user1", "user2"],
    "read": ["user3", "user4"],
    "write": ["user5", "user6"]
})
```

```go
// Go - Configuration security handling
type ConfigSecurity struct {
    SensitiveConfig string `tsk:"sensitive_config"`
    AccessControl   string `tsk:"access_control"`
}

type SecureConfig struct {
    APIKey           string `json:"api_key"`
    DatabasePassword string `json:"database_password"`
    JWTSecret        string `json:"jwt_secret"`
    SSLPrivateKey    string `json:"ssl_private_key"`
}

func (c *ConfigSecurity) LoadSecureConfig(securityManager *SecurityManager) (*SecureConfig, error) {
    config := &SecureConfig{}
    
    // Load encrypted values
    apiKey, err := securityManager.DecryptData(os.Getenv("ENCRYPTED_API_KEY"), "AES-256-GCM")
    if err != nil {
        return nil, fmt.Errorf("failed to decrypt API key: %w", err)
    }
    config.APIKey = apiKey
    
    dbPassword, err := securityManager.DecryptData(os.Getenv("ENCRYPTED_DB_PASSWORD"), "ChaCha20-Poly1305")
    if err != nil {
        return nil, fmt.Errorf("failed to decrypt database password: %w", err)
    }
    config.DatabasePassword = dbPassword
    
    jwtSecret, err := securityManager.DecryptData(os.Getenv("ENCRYPTED_JWT_SECRET"), "AES-256-GCM")
    if err != nil {
        return nil, fmt.Errorf("failed to decrypt JWT secret: %w", err)
    }
    config.JWTSecret = jwtSecret
    
    // Load secure file
    sslKey, err := os.ReadFile("ssl/private.key")
    if err != nil {
        return nil, fmt.Errorf("failed to read SSL private key: %w", err)
    }
    config.SSLPrivateKey = string(sslKey)
    
    return config, nil
}
```

### **API Security**

```go
// TuskLang - API security
[api_security]
rate_limiting: @security.rate_limit({
    "requests_per_minute": 100,
    "burst_size": 20,
    "window_size": "1m"
})

authentication: @security.auth({
    "type": "jwt",
    "algorithm": "HS256",
    "expiry": "24h",
    "refresh_expiry": "7d"
})

cors_policy: @security.cors({
    "allowed_origins": ["https://example.com", "https://api.example.com"],
    "allowed_methods": ["GET", "POST", "PUT", "DELETE"],
    "allowed_headers": ["Authorization", "Content-Type"]
})
```

```go
// Go - API security handling
type APISecurity struct {
    RateLimiting   string `tsk:"rate_limiting"`
    Authentication string `tsk:"authentication"`
    CORSPolicy     string `tsk:"cors_policy"`
}

type RateLimitConfig struct {
    RequestsPerMinute int    `json:"requests_per_minute"`
    BurstSize         int    `json:"burst_size"`
    WindowSize        string `json:"window_size"`
}

type AuthConfig struct {
    Type           string `json:"type"`
    Algorithm      string `json:"algorithm"`
    Expiry         string `json:"expiry"`
    RefreshExpiry  string `json:"refresh_expiry"`
}

type CORSConfig struct {
    AllowedOrigins []string `json:"allowed_origins"`
    AllowedMethods []string `json:"allowed_methods"`
    AllowedHeaders []string `json:"allowed_headers"`
}

func (a *APISecurity) CreateRateLimiter(config RateLimitConfig) *rate.Limiter {
    window, _ := time.ParseDuration(config.WindowSize)
    return rate.NewLimiter(rate.Every(window/time.Duration(config.RequestsPerMinute)), config.BurstSize)
}

func (a *APISecurity) CreateJWTMiddleware(config AuthConfig) func(http.HandlerFunc) http.HandlerFunc {
    return func(next http.HandlerFunc) http.HandlerFunc {
        return func(w http.ResponseWriter, r *http.Request) {
            token := r.Header.Get("Authorization")
            if token == "" {
                http.Error(w, "Missing authorization token", http.StatusUnauthorized)
                return
            }
            
            // Validate JWT token
            if !a.validateJWT(token, config) {
                http.Error(w, "Invalid authorization token", http.StatusUnauthorized)
                return
            }
            
            next.ServeHTTP(w, r)
        }
    }
}

func (a *APISecurity) validateJWT(token string, config AuthConfig) bool {
    // Simplified JWT validation
    // In production, use proper JWT library
    return token != ""
}

func (a *APISecurity) CreateCORSMiddleware(config CORSConfig) func(http.HandlerFunc) http.HandlerFunc {
    return func(next http.HandlerFunc) http.HandlerFunc {
        return func(w http.ResponseWriter, r *http.Request) {
            origin := r.Header.Get("Origin")
            
            // Check if origin is allowed
            allowed := false
            for _, allowedOrigin := range config.AllowedOrigins {
                if origin == allowedOrigin {
                    allowed = true
                    break
                }
            }
            
            if allowed {
                w.Header().Set("Access-Control-Allow-Origin", origin)
            }
            
            w.Header().Set("Access-Control-Allow-Methods", strings.Join(config.AllowedMethods, ", "))
            w.Header().Set("Access-Control-Allow-Headers", strings.Join(config.AllowedHeaders, ", "))
            
            if r.Method == "OPTIONS" {
                w.WriteHeader(http.StatusOK)
                return
            }
            
            next.ServeHTTP(w, r)
        }
    }
}
```

## ⚡ **Performance Considerations**

### **Security Caching**

```go
// Go - Security caching system
type SecurityCache struct {
    cache map[string]interface{}
    mutex sync.RWMutex
    ttl   time.Duration
}

func NewSecurityCache(ttl time.Duration) *SecurityCache {
    return &SecurityCache{
        cache: make(map[string]interface{}),
        ttl:   ttl,
    }
}

func (s *SecurityCache) Get(key string) (interface{}, bool) {
    s.mutex.RLock()
    defer s.mutex.RUnlock()
    
    value, exists := s.cache[key]
    return value, exists
}

func (s *SecurityCache) Set(key string, value interface{}) {
    s.mutex.Lock()
    defer s.mutex.Unlock()
    
    s.cache[key] = value
    
    // Schedule cleanup
    go func() {
        time.Sleep(s.ttl)
        s.mutex.Lock()
        delete(s.cache, key)
        s.mutex.Unlock()
    }()
}

func (s *SecurityCache) Clear() {
    s.mutex.Lock()
    defer s.mutex.Unlock()
    
    s.cache = make(map[string]interface{})
}
```

### **Lazy Security Evaluation**

```go
// Go - Lazy security evaluation
type LazySecurity struct {
    cache *SecurityCache
    manager *SecurityManager
}

func NewLazySecurity(ttl time.Duration, key []byte) *LazySecurity {
    return &LazySecurity{
        cache:   NewSecurityCache(ttl),
        manager: NewSecurityManager(key),
    }
}

func (l *LazySecurity) GetSecureValue(name string, algorithm string) (string, error) {
    // Check cache first
    if cached, exists := l.cache.Get(name); exists {
        return cached.(string), nil
    }
    
    // Get from secure environment
    value, err := l.manager.GetSecureEnv(name)
    if err != nil {
        return "", err
    }
    
    // Cache the result
    l.cache.Set(name, value)
    
    return value, nil
}

func (l *LazySecurity) ValidateAccess(user string, action string) error {
    cacheKey := fmt.Sprintf("access:%s:%s", user, action)
    
    if cached, exists := l.cache.Get(cacheKey); exists {
        if !cached.(bool) {
            return fmt.Errorf("access denied for user '%s' to action '%s'", user, action)
        }
        return nil
    }
    
    err := l.manager.ValidateAccess(user, action)
    l.cache.Set(cacheKey, err == nil)
    
    return err
}
```

## 🌍 **Real-World Examples**

### **Secure API Configuration**

```go
// TuskLang - Secure API configuration
[secure_api]
api_config: @secure.api({
    "base_url": "https://api.example.com",
    "timeout": 30,
    "retries": 3,
    "auth": {
        "type": "bearer",
        "token": @env.secure("API_TOKEN")
    },
    "encryption": {
        "enabled": true,
        "algorithm": "AES-256-GCM"
    }
})

database_config: @secure.database({
    "host": @env("DB_HOST"),
    "port": @env("DB_PORT"),
    "name": @env("DB_NAME"),
    "user": @env("DB_USER"),
    "password": @env.secure("DB_PASSWORD"),
    "ssl": {
        "enabled": true,
        "cert": @file.secure("ssl/cert.pem"),
        "key": @file.secure("ssl/private.key")
    }
})
```

```go
// Go - Secure API configuration handling
type SecureAPI struct {
    APIConfig     string `tsk:"api_config"`
    DatabaseConfig string `tsk:"database_config"`
}

type APIConfig struct {
    BaseURL   string `json:"base_url"`
    Timeout   int    `json:"timeout"`
    Retries   int    `json:"retries"`
    Auth      AuthConfig `json:"auth"`
    Encryption EncryptionConfig `json:"encryption"`
}

type DatabaseConfig struct {
    Host     string `json:"host"`
    Port     int    `json:"port"`
    Name     string `json:"name"`
    User     string `json:"user"`
    Password string `json:"password"`
    SSL      SSLConfig `json:"ssl"`
}

type AuthConfig struct {
    Type  string `json:"type"`
    Token string `json:"token"`
}

type EncryptionConfig struct {
    Enabled   bool   `json:"enabled"`
    Algorithm string `json:"algorithm"`
}

type SSLConfig struct {
    Enabled bool   `json:"enabled"`
    Cert    string `json:"cert"`
    Key     string `json:"key"`
}

func (s *SecureAPI) CreateSecureAPIClient(config APIConfig, securityManager *SecurityManager) (*http.Client, error) {
    client := &http.Client{
        Timeout: time.Duration(config.Timeout) * time.Second,
    }
    
    // Add authentication middleware
    if config.Auth.Type == "bearer" {
        // In production, add bearer token to requests
    }
    
    // Add encryption if enabled
    if config.Encryption.Enabled {
        // In production, add encryption layer
    }
    
    return client, nil
}

func (s *SecureAPI) CreateSecureDatabaseConnection(config DatabaseConfig, securityManager *SecurityManager) (*sql.DB, error) {
    // Build connection string with encrypted password
    connectionString := fmt.Sprintf(
        "host=%s port=%d dbname=%s user=%s password=%s",
        config.Host, config.Port, config.Name, config.User, config.Password,
    )
    
    if config.SSL.Enabled {
        connectionString += " sslmode=require"
    }
    
    db, err := sql.Open("postgres", connectionString)
    if err != nil {
        return nil, fmt.Errorf("failed to open database connection: %w", err)
    }
    
    return db, nil
}
```

### **Secure File Handling**

```go
// TuskLang - Secure file handling
[secure_files]
ssl_certificate: @file.secure("ssl/cert.pem", {
    "permissions": "644",
    "owner": "root",
    "group": "ssl-cert"
})

private_key: @file.secure("ssl/private.key", {
    "permissions": "600",
    "owner": "root",
    "group": "ssl-cert"
})

config_file: @file.secure("config.json", {
    "permissions": "640",
    "owner": "app",
    "group": "app"
})
```

```go
// Go - Secure file handling
type SecureFiles struct {
    SSLCertificate string `tsk:"ssl_certificate"`
    PrivateKey     string `tsk:"private_key"`
    ConfigFile     string `tsk:"config_file"`
}

type FileSecurity struct {
    Path        string `json:"path"`
    Permissions string `json:"permissions"`
    Owner       string `json:"owner"`
    Group       string `json:"group"`
}

func (s *SecureFiles) ValidateFileSecurity(path string, expected FileSecurity) error {
    info, err := os.Stat(path)
    if err != nil {
        return fmt.Errorf("failed to get file info: %w", err)
    }
    
    // Check file permissions
    mode := info.Mode()
    expectedPerms, err := strconv.ParseUint(expected.Permissions, 8, 32)
    if err != nil {
        return fmt.Errorf("invalid permission format: %w", err)
    }
    
    if uint32(mode.Perm()) != uint32(expectedPerms) {
        return fmt.Errorf("file permissions mismatch: expected %s, got %s", 
            expected.Permissions, fmt.Sprintf("%o", mode.Perm()))
    }
    
    // In production, also check owner and group
    // This requires platform-specific code
    
    return nil
}

func (s *SecureFiles) ReadSecureFile(path string) ([]byte, error) {
    // Validate file security before reading
    expected := FileSecurity{
        Path:        path,
        Permissions: "600",
        Owner:       "root",
        Group:       "ssl-cert",
    }
    
    if err := s.ValidateFileSecurity(path, expected); err != nil {
        return nil, fmt.Errorf("file security validation failed: %w", err)
    }
    
    data, err := os.ReadFile(path)
    if err != nil {
        return nil, fmt.Errorf("failed to read secure file: %w", err)
    }
    
    return data, nil
}
```

## 🎯 **Best Practices**

### **1. Use Strong Encryption**

```go
// ✅ Good - Strong encryption
[good_encryption]
sensitive_data: @encrypt("secret_value", "AES-256-GCM")
api_key: @env.secure("API_KEY")
password: @encrypt("user_password", "ChaCha20-Poly1305")

// ❌ Bad - Weak encryption
[bad_encryption]
sensitive_data: @encrypt("secret_value", "AES-128-CBC")  # Weak algorithm
api_key: @env("API_KEY")  # Not secure
password: "plain_text_password"  # No encryption
```

### **2. Validate Security Configurations**

```go
// ✅ Good - Validate security configurations
func (s *SecurityManager) ValidateSecurityConfig() error {
    // Validate encryption keys
    if len(s.key) < 32 {
        return errors.New("encryption key must be at least 32 bytes")
    }
    
    // Validate secure environment variables
    required := []string{"API_KEY", "JWT_SECRET", "DB_PASSWORD"}
    for _, name := range required {
        if _, err := s.GetSecureEnv(name); err != nil {
            return fmt.Errorf("missing required secure environment variable: %s", name)
        }
    }
    
    return nil
}

// ❌ Bad - No validation
func (s *SecurityManager) LoadConfig() error {
    // Load configuration without security validation
    return nil
}
```

### **3. Use Secure File Permissions**

```go
// ✅ Good - Secure file permissions
func (s *SecureFiles) SetSecurePermissions(path string) error {
    // Set restrictive permissions
    if err := os.Chmod(path, 0600); err != nil {
        return fmt.Errorf("failed to set file permissions: %w", err)
    }
    
    return nil
}

// ❌ Bad - Insecure file permissions
func (s *SecureFiles) ReadFile(path string) ([]byte, error) {
    // Read file without checking permissions
    return os.ReadFile(path)
}
```

### **4. Implement Proper Access Control**

```go
// ✅ Good - Proper access control
func (s *SecurityManager) ValidateAccess(user string, action string) error {
    // Check multiple security layers
    if !s.rbacManager.HasRole(user, "admin") {
        if !s.permManager.HasPermission(user, action) {
            return fmt.Errorf("access denied for user '%s' to action '%s'", user, action)
        }
    }
    
    return nil
}

// ❌ Bad - Weak access control
func (s *SecurityManager) CheckAccess(user string) bool {
    // Simple boolean check without proper validation
    return user != ""
}
```

### **5. Log Security Events**

```go
// ✅ Good - Security event logging
func (s *SecurityManager) LogSecurityEvent(event string, user string, details map[string]interface{}) {
    logEntry := map[string]interface{}{
        "timestamp": time.Now().UTC(),
        "event":     event,
        "user":      user,
        "details":   details,
        "ip":        getClientIP(),
    }
    
    // Log to secure audit log
    logData, _ := json.Marshal(logEntry)
    log.Printf("SECURITY: %s", string(logData))
}

// ❌ Bad - No security logging
func (s *SecurityManager) ValidateAccess(user string, action string) error {
    // No logging of access attempts
    return s.checkAccess(user, action)
}
```

---

**🎉 You've mastered security in TuskLang with Go!**

Security in TuskLang ensures your configuration and data are protected at every level. With proper security handling, you can build robust, secure systems that protect sensitive information.

**Next Steps:**
- Explore [025-testing-go.md](025-testing-go.md) for testing strategies
- Master [026-performance-go.md](026-performance-go.md) for optimization
- Dive into [027-deployment-go.md](027-deployment-go.md) for deployment

**Remember:** In TuskLang, security isn't optional—it's essential. Use it wisely to build bulletproof, secure systems. 