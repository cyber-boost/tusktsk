# Validation in TuskLang - Go Guide

## 🎯 **The Power of Configuration Validation**

In TuskLang, validation isn't just about checking values—it's about ensuring your configuration is bulletproof. We don't bow to any king, especially not invalid or unsafe configurations. TuskLang gives you the power to validate your configuration at multiple levels with comprehensive, type-safe validation rules.

## 📋 **Table of Contents**
- [Understanding Validation in TuskLang](#understanding-validation-in-tusklang)
- [Basic Validation Syntax](#basic-validation-syntax)
- [Schema Validation](#schema-validation)
- [Custom Validation Rules](#custom-validation-rules)
- [Go Integration](#go-integration)
- [Validation Patterns](#validation-patterns)
- [Performance Considerations](#performance-considerations)
- [Real-World Examples](#real-world-examples)
- [Best Practices](#best-practices)

## 🔍 **Understanding Validation in TuskLang**

TuskLang provides comprehensive validation at configuration time:

```go
// TuskLang - Validation configuration
[validation_config]
required_fields: @validate.required(["api_key", "database_url"])
email_validation: @validate.email(@env("ADMIN_EMAIL"))
url_validation: @validate.url(@env("API_BASE_URL"))
numeric_validation: @validate.numeric(@env("PORT"))
range_validation: @validate.range(@env("TIMEOUT"), 1, 300)
```

```go
// Go integration
type ValidationConfig struct {
    RequiredFields   []string `tsk:"required_fields"`
    EmailValidation  bool     `tsk:"email_validation"`
    URLValidation    bool     `tsk:"url_validation"`
    NumericValidation bool    `tsk:"numeric_validation"`
    RangeValidation  bool     `tsk:"range_validation"`
}
```

## 🎨 **Basic Validation Syntax**

### **Required Field Validation**

```go
// TuskLang - Required field validation
[required_validation]
api_key: @validate.required(["API_KEY", "DATABASE_URL"])
user_fields: @validate.required(["USER_ID", "USER_NAME", "USER_EMAIL"])
config_fields: @validate.required(["APP_NAME", "ENVIRONMENT"])
```

```go
// Go - Required field validation handling
type RequiredValidation struct {
    APIKey     []string `tsk:"api_key"`
    UserFields []string `tsk:"user_fields"`
    ConfigFields []string `tsk:"config_fields"`
}

func (r *RequiredValidation) ValidateRequired(fields []string) error {
    for _, field := range fields {
        if os.Getenv(field) == "" {
            return fmt.Errorf("required field %s is missing", field)
        }
    }
    return nil
}

func (r *RequiredValidation) ValidateAll() error {
    if err := r.ValidateRequired(r.APIKey); err != nil {
        return fmt.Errorf("API validation failed: %w", err)
    }
    
    if err := r.ValidateRequired(r.UserFields); err != nil {
        return fmt.Errorf("user validation failed: %w", err)
    }
    
    if err := r.ValidateRequired(r.ConfigFields); err != nil {
        return fmt.Errorf("config validation failed: %w", err)
    }
    
    return nil
}
```

### **Type Validation**

```go
// TuskLang - Type validation
[type_validation]
string_fields: @validate.string(["APP_NAME", "ENVIRONMENT"])
numeric_fields: @validate.numeric(["PORT", "TIMEOUT", "MAX_CONNECTIONS"])
boolean_fields: @validate.boolean(["DEBUG", "ENABLE_CACHE"])
```

```go
// Go - Type validation handling
type TypeValidation struct {
    StringFields  []string `tsk:"string_fields"`
    NumericFields []string `tsk:"numeric_fields"`
    BooleanFields []string `tsk:"boolean_fields"`
}

func (t *TypeValidation) ValidateString(fields []string) error {
    for _, field := range fields {
        value := os.Getenv(field)
        if value == "" {
            return fmt.Errorf("string field %s is empty", field)
        }
    }
    return nil
}

func (t *TypeValidation) ValidateNumeric(fields []string) error {
    for _, field := range fields {
        value := os.Getenv(field)
        if value == "" {
            return fmt.Errorf("numeric field %s is empty", field)
        }
        
        if _, err := strconv.Atoi(value); err != nil {
            return fmt.Errorf("field %s must be numeric, got: %s", field, value)
        }
    }
    return nil
}

func (t *TypeValidation) ValidateBoolean(fields []string) error {
    for _, field := range fields {
        value := os.Getenv(field)
        if value == "" {
            return fmt.Errorf("boolean field %s is empty", field)
        }
        
        if value != "true" && value != "false" {
            return fmt.Errorf("field %s must be boolean (true/false), got: %s", field, value)
        }
    }
    return nil
}
```

### **Format Validation**

```go
// TuskLang - Format validation
[format_validation]
email_validation: @validate.email(@env("ADMIN_EMAIL"))
url_validation: @validate.url(@env("API_BASE_URL"))
ip_validation: @validate.ip(@env("SERVER_IP"))
uuid_validation: @validate.uuid(@env("SESSION_ID"))
```

```go
// Go - Format validation handling
type FormatValidation struct {
    EmailValidation bool `tsk:"email_validation"`
    URLValidation   bool `tsk:"url_validation"`
    IPValidation    bool `tsk:"ip_validation"`
    UUIDValidation  bool `tsk:"uuid_validation"`
}

func (f *FormatValidation) ValidateEmail(email string) bool {
    emailRegex := regexp.MustCompile(`^[^\s@]+@[^\s@]+\.[^\s@]+$`)
    return emailRegex.MatchString(email)
}

func (f *FormatValidation) ValidateURL(url string) bool {
    _, err := url.Parse(url)
    return err == nil
}

func (f *FormatValidation) ValidateIP(ip string) bool {
    return net.ParseIP(ip) != nil
}

func (f *FormatValidation) ValidateUUID(uuid string) bool {
    uuidRegex := regexp.MustCompile(`^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$`)
    return uuidRegex.MatchString(strings.ToLower(uuid))
}
```

## 📋 **Schema Validation**

### **JSON Schema Validation**

```go
// TuskLang - JSON schema validation
[schema_validation]
user_schema: @validate.schema({
    "type": "object",
    "properties": {
        "name": {"type": "string", "minLength": 1},
        "email": {"type": "string", "format": "email"},
        "age": {"type": "integer", "minimum": 18, "maximum": 100}
    },
    "required": ["name", "email"]
})

config_schema: @validate.schema({
    "type": "object",
    "properties": {
        "app_name": {"type": "string"},
        "port": {"type": "integer", "minimum": 1, "maximum": 65535},
        "debug": {"type": "boolean"}
    },
    "required": ["app_name", "port"]
})
```

```go
// Go - JSON schema validation handling
type SchemaValidation struct {
    UserSchema   string `tsk:"user_schema"`
    ConfigSchema string `tsk:"config_schema"`
}

type User struct {
    Name  string `json:"name"`
    Email string `json:"email"`
    Age   int    `json:"age,omitempty"`
}

type Config struct {
    AppName string `json:"app_name"`
    Port    int    `json:"port"`
    Debug   bool   `json:"debug,omitempty"`
}

func (s *SchemaValidation) ValidateUser(user User) error {
    // Validate required fields
    if user.Name == "" {
        return errors.New("name is required")
    }
    
    if user.Email == "" {
        return errors.New("email is required")
    }
    
    // Validate email format
    emailRegex := regexp.MustCompile(`^[^\s@]+@[^\s@]+\.[^\s@]+$`)
    if !emailRegex.MatchString(user.Email) {
        return errors.New("invalid email format")
    }
    
    // Validate age range
    if user.Age > 0 && (user.Age < 18 || user.Age > 100) {
        return errors.New("age must be between 18 and 100")
    }
    
    return nil
}

func (s *SchemaValidation) ValidateConfig(config Config) error {
    // Validate required fields
    if config.AppName == "" {
        return errors.New("app_name is required")
    }
    
    if config.Port <= 0 {
        return errors.New("port is required")
    }
    
    // Validate port range
    if config.Port < 1 || config.Port > 65535 {
        return errors.New("port must be between 1 and 65535")
    }
    
    return nil
}
```

### **Custom Schema Validation**

```go
// TuskLang - Custom schema validation
[custom_schema]
database_schema: @validate.custom_schema({
    "host": {"type": "string", "required": true},
    "port": {"type": "integer", "min": 1, "max": 65535},
    "name": {"type": "string", "required": true},
    "ssl": {"type": "boolean", "default": false}
})

api_schema: @validate.custom_schema({
    "base_url": {"type": "string", "format": "url", "required": true},
    "timeout": {"type": "integer", "min": 1, "max": 300},
    "retries": {"type": "integer", "min": 0, "max": 10}
})
```

```go
// Go - Custom schema validation handling
type CustomSchema struct {
    DatabaseSchema string `tsk:"database_schema"`
    APISchema      string `tsk:"api_schema"`
}

type DatabaseConfig struct {
    Host string `json:"host"`
    Port int    `json:"port"`
    Name string `json:"name"`
    SSL  bool   `json:"ssl"`
}

type APIConfig struct {
    BaseURL string `json:"base_url"`
    Timeout int    `json:"timeout"`
    Retries int    `json:"retries"`
}

func (c *CustomSchema) ValidateDatabase(config DatabaseConfig) error {
    if config.Host == "" {
        return errors.New("database host is required")
    }
    
    if config.Port < 1 || config.Port > 65535 {
        return errors.New("database port must be between 1 and 65535")
    }
    
    if config.Name == "" {
        return errors.New("database name is required")
    }
    
    return nil
}

func (c *CustomSchema) ValidateAPI(config APIConfig) error {
    if config.BaseURL == "" {
        return errors.New("API base URL is required")
    }
    
    if _, err := url.Parse(config.BaseURL); err != nil {
        return fmt.Errorf("invalid API base URL: %w", err)
    }
    
    if config.Timeout < 1 || config.Timeout > 300 {
        return errors.New("API timeout must be between 1 and 300 seconds")
    }
    
    if config.Retries < 0 || config.Retries > 10 {
        return errors.New("API retries must be between 0 and 10")
    }
    
    return nil
}
```

## 🔧 **Custom Validation Rules**

### **Business Logic Validation**

```go
// TuskLang - Business logic validation
[business_validation]
password_strength: @validate.custom("password", "min_length:8,uppercase:true,number:true")
age_validation: @validate.range(@env("AGE"), 18, 100)
file_size: @validate.max_size("upload.txt", "10MB")
domain_validation: @validate.custom("domain", "allowed_domains:example.com,test.com")
```

```go
// Go - Business logic validation handling
type BusinessValidation struct {
    PasswordStrength bool `tsk:"password_strength"`
    AgeValidation    bool `tsk:"age_validation"`
    FileSize         bool `tsk:"file_size"`
    DomainValidation bool `tsk:"domain_validation"`
}

func (b *BusinessValidation) ValidatePassword(password string) error {
    if len(password) < 8 {
        return errors.New("password must be at least 8 characters long")
    }
    
    hasUpper := regexp.MustCompile(`[A-Z]`).MatchString(password)
    if !hasUpper {
        return errors.New("password must contain at least one uppercase letter")
    }
    
    hasNumber := regexp.MustCompile(`[0-9]`).MatchString(password)
    if !hasNumber {
        return errors.New("password must contain at least one number")
    }
    
    return nil
}

func (b *BusinessValidation) ValidateAge(age int) error {
    if age < 18 || age > 100 {
        return errors.New("age must be between 18 and 100")
    }
    return nil
}

func (b *BusinessValidation) ValidateFileSize(path string, maxSize string) error {
    info, err := os.Stat(path)
    if err != nil {
        return fmt.Errorf("failed to get file info: %w", err)
    }
    
    size := info.Size()
    maxBytes := parseSize(maxSize)
    
    if size > maxBytes {
        return fmt.Errorf("file size %d bytes exceeds maximum %d bytes", size, maxBytes)
    }
    
    return nil
}

func (b *BusinessValidation) ValidateDomain(domain string, allowedDomains []string) error {
    for _, allowed := range allowedDomains {
        if domain == allowed {
            return nil
        }
    }
    
    return fmt.Errorf("domain %s is not in allowed list: %v", domain, allowedDomains)
}

func parseSize(sizeStr string) int64 {
    if strings.HasSuffix(sizeStr, "MB") {
        size, _ := strconv.Atoi(strings.TrimSuffix(sizeStr, "MB"))
        return int64(size * 1024 * 1024)
    }
    if strings.HasSuffix(sizeStr, "KB") {
        size, _ := strconv.Atoi(strings.TrimSuffix(sizeStr, "KB"))
        return int64(size * 1024)
    }
    size, _ := strconv.Atoi(sizeStr)
    return int64(size)
}
```

### **Cross-Field Validation**

```go
// TuskLang - Cross-field validation
[cross_field_validation]
password_match: @validate.match(@env("PASSWORD"), @env("PASSWORD_CONFIRM"))
date_range: @validate.date_range(@env("START_DATE"), @env("END_DATE"))
numeric_range: @validate.numeric_range(@env("MIN_VALUE"), @env("MAX_VALUE"))
```

```go
// Go - Cross-field validation handling
type CrossFieldValidation struct {
    PasswordMatch bool `tsk:"password_match"`
    DateRange     bool `tsk:"date_range"`
    NumericRange  bool `tsk:"numeric_range"`
}

func (c *CrossFieldValidation) ValidatePasswordMatch(password, confirm string) error {
    if password != confirm {
        return errors.New("passwords do not match")
    }
    return nil
}

func (c *CrossFieldValidation) ValidateDateRange(startDate, endDate string) error {
    start, err := time.Parse("2006-01-02", startDate)
    if err != nil {
        return fmt.Errorf("invalid start date: %w", err)
    }
    
    end, err := time.Parse("2006-01-02", endDate)
    if err != nil {
        return fmt.Errorf("invalid end date: %w", err)
    }
    
    if start.After(end) {
        return errors.New("start date must be before end date")
    }
    
    return nil
}

func (c *CrossFieldValidation) ValidateNumericRange(min, max string) error {
    minVal, err := strconv.Atoi(min)
    if err != nil {
        return fmt.Errorf("invalid minimum value: %w", err)
    }
    
    maxVal, err := strconv.Atoi(max)
    if err != nil {
        return fmt.Errorf("invalid maximum value: %w", err)
    }
    
    if minVal >= maxVal {
        return errors.New("minimum value must be less than maximum value")
    }
    
    return nil
}
```

## 🔧 **Go Integration**

### **Validation Engine**

```go
// Go - Validation engine
type ValidationEngine struct {
    validators map[string]ValidatorFunc
}

type ValidatorFunc func(value interface{}) error

func NewValidationEngine() *ValidationEngine {
    return &ValidationEngine{
        validators: make(map[string]ValidatorFunc),
    }
}

func (v *ValidationEngine) RegisterValidator(name string, fn ValidatorFunc) {
    v.validators[name] = fn
}

func (v *ValidationEngine) Validate(name string, value interface{}) error {
    fn, exists := v.validators[name]
    if !exists {
        return fmt.Errorf("validator '%s' not found", name)
    }
    
    return fn(value)
}

// Register common validators
func (v *ValidationEngine) RegisterCommon() {
    v.RegisterValidator("required", v.requiredValidator)
    v.RegisterValidator("email", v.emailValidator)
    v.RegisterValidator("url", v.urlValidator)
    v.RegisterValidator("numeric", v.numericValidator)
    v.RegisterValidator("range", v.rangeValidator)
}

func (v *ValidationEngine) requiredValidator(value interface{}) error {
    if value == nil {
        return errors.New("value is required")
    }
    
    if str, ok := value.(string); ok && str == "" {
        return errors.New("value is required")
    }
    
    return nil
}

func (v *ValidationEngine) emailValidator(value interface{}) error {
    str, ok := value.(string)
    if !ok {
        return errors.New("value must be a string")
    }
    
    emailRegex := regexp.MustCompile(`^[^\s@]+@[^\s@]+\.[^\s@]+$`)
    if !emailRegex.MatchString(str) {
        return errors.New("invalid email format")
    }
    
    return nil
}

func (v *ValidationEngine) urlValidator(value interface{}) error {
    str, ok := value.(string)
    if !ok {
        return errors.New("value must be a string")
    }
    
    _, err := url.Parse(str)
    if err != nil {
        return fmt.Errorf("invalid URL format: %w", err)
    }
    
    return nil
}

func (v *ValidationEngine) numericValidator(value interface{}) error {
    switch v := value.(type) {
    case int, int64, float64:
        return nil
    case string:
        if _, err := strconv.Atoi(v); err != nil {
            return errors.New("value must be numeric")
        }
        return nil
    default:
        return errors.New("value must be numeric")
    }
}

func (v *ValidationEngine) rangeValidator(value interface{}) error {
    // This is a simplified implementation
    // In practice, you'd want to pass min/max as additional parameters
    return nil
}
```

### **Validation Chain**

```go
// Go - Validation chain
type ValidationChain struct {
    engine *ValidationEngine
    rules  []ValidationRule
}

type ValidationRule struct {
    Name  string
    Value interface{}
    Args  []interface{}
}

func NewValidationChain(engine *ValidationEngine) *ValidationChain {
    return &ValidationChain{
        engine: engine,
        rules:  make([]ValidationRule, 0),
    }
}

func (v *ValidationChain) AddRule(name string, value interface{}, args ...interface{}) *ValidationChain {
    v.rules = append(v.rules, ValidationRule{
        Name:  name,
        Value: value,
        Args:  args,
    })
    return v
}

func (v *ValidationChain) Validate() error {
    for _, rule := range v.rules {
        if err := v.engine.Validate(rule.Name, rule.Value); err != nil {
            return fmt.Errorf("validation failed for rule '%s': %w", rule.Name, err)
        }
    }
    return nil
}

// Fluent interface example
func (v *ValidationChain) Required(value interface{}) *ValidationChain {
    return v.AddRule("required", value)
}

func (v *ValidationChain) Email(value interface{}) *ValidationChain {
    return v.AddRule("email", value)
}

func (v *ValidationChain) URL(value interface{}) *ValidationChain {
    return v.AddRule("url", value)
}

func (v *ValidationChain) Numeric(value interface{}) *ValidationChain {
    return v.AddRule("numeric", value)
}
```

## 🚀 **Validation Patterns**

### **Configuration Validation**

```go
// TuskLang - Configuration validation
[config_validation]
app_config: @validate.config({
    "required": ["APP_NAME", "ENVIRONMENT", "PORT"],
    "numeric": ["PORT", "TIMEOUT"],
    "boolean": ["DEBUG", "ENABLE_CACHE"],
    "email": ["ADMIN_EMAIL"],
    "url": ["API_BASE_URL"]
})

database_config: @validate.config({
    "required": ["DB_HOST", "DB_NAME", "DB_USER"],
    "numeric": ["DB_PORT"],
    "boolean": ["DB_SSL"]
})
```

```go
// Go - Configuration validation handling
type ConfigValidation struct {
    AppConfig     string `tsk:"app_config"`
    DatabaseConfig string `tsk:"database_config"`
}

type AppConfig struct {
    AppName     string `json:"app_name"`
    Environment string `json:"environment"`
    Port        int    `json:"port"`
    Timeout     int    `json:"timeout"`
    Debug       bool   `json:"debug"`
    EnableCache bool   `json:"enable_cache"`
    AdminEmail  string `json:"admin_email"`
    APIBaseURL  string `json:"api_base_url"`
}

type DatabaseConfig struct {
    Host string `json:"host"`
    Port int    `json:"port"`
    Name string `json:"name"`
    User string `json:"user"`
    SSL  bool   `json:"ssl"`
}

func (c *ConfigValidation) ValidateAppConfig(config AppConfig) error {
    engine := NewValidationEngine()
    engine.RegisterCommon()
    
    chain := NewValidationChain(engine)
    
    chain.Required(config.AppName).
        Required(config.Environment).
        Numeric(config.Port).
        Numeric(config.Timeout)
    
    if config.AdminEmail != "" {
        chain.Email(config.AdminEmail)
    }
    
    if config.APIBaseURL != "" {
        chain.URL(config.APIBaseURL)
    }
    
    return chain.Validate()
}

func (c *ConfigValidation) ValidateDatabaseConfig(config DatabaseConfig) error {
    engine := NewValidationEngine()
    engine.RegisterCommon()
    
    chain := NewValidationChain(engine)
    
    chain.Required(config.Host).
        Required(config.Name).
        Required(config.User).
        Numeric(config.Port)
    
    return chain.Validate()
}
```

### **Data Validation**

```go
// TuskLang - Data validation
[data_validation]
user_data: @validate.data({
    "name": {"type": "string", "required": true, "min_length": 1},
    "email": {"type": "string", "required": true, "format": "email"},
    "age": {"type": "integer", "min": 18, "max": 100}
})

order_data: @validate.data({
    "items": {"type": "array", "min_items": 1},
    "total": {"type": "number", "min": 0},
    "customer_id": {"type": "string", "required": true}
})
```

```go
// Go - Data validation handling
type DataValidation struct {
    UserData  string `tsk:"user_data"`
    OrderData string `tsk:"order_data"`
}

type User struct {
    Name  string `json:"name"`
    Email string `json:"email"`
    Age   int    `json:"age"`
}

type Order struct {
    Items      []string  `json:"items"`
    Total      float64   `json:"total"`
    CustomerID string    `json:"customer_id"`
}

func (d *DataValidation) ValidateUser(user User) error {
    if user.Name == "" {
        return errors.New("name is required")
    }
    
    if len(user.Name) < 1 {
        return errors.New("name must be at least 1 character")
    }
    
    if user.Email == "" {
        return errors.New("email is required")
    }
    
    emailRegex := regexp.MustCompile(`^[^\s@]+@[^\s@]+\.[^\s@]+$`)
    if !emailRegex.MatchString(user.Email) {
        return errors.New("invalid email format")
    }
    
    if user.Age < 18 || user.Age > 100 {
        return errors.New("age must be between 18 and 100")
    }
    
    return nil
}

func (d *DataValidation) ValidateOrder(order Order) error {
    if len(order.Items) < 1 {
        return errors.New("order must have at least one item")
    }
    
    if order.Total < 0 {
        return errors.New("order total cannot be negative")
    }
    
    if order.CustomerID == "" {
        return errors.New("customer ID is required")
    }
    
    return nil
}
```

## ⚡ **Performance Considerations**

### **Validation Caching**

```go
// Go - Validation caching system
type ValidationCache struct {
    cache map[string]bool
    mutex sync.RWMutex
    ttl   time.Duration
}

func NewValidationCache(ttl time.Duration) *ValidationCache {
    return &ValidationCache{
        cache: make(map[string]bool),
        ttl:   ttl,
    }
}

func (v *ValidationCache) Get(key string) (bool, bool) {
    v.mutex.RLock()
    defer v.mutex.RUnlock()
    
    value, exists := v.cache[key]
    return value, exists
}

func (v *ValidationCache) Set(key string, value bool) {
    v.mutex.Lock()
    defer v.mutex.Unlock()
    
    v.cache[key] = value
    
    // Schedule cleanup
    go func() {
        time.Sleep(v.ttl)
        v.mutex.Lock()
        delete(v.cache, key)
        v.mutex.Unlock()
    }()
}

func (v *ValidationCache) Clear() {
    v.mutex.Lock()
    defer v.mutex.Unlock()
    
    v.cache = make(map[string]bool)
}
```

### **Lazy Validation**

```go
// Go - Lazy validation system
type LazyValidator struct {
    cache *ValidationCache
    engine *ValidationEngine
}

func NewLazyValidator(ttl time.Duration) *LazyValidator {
    engine := NewValidationEngine()
    engine.RegisterCommon()
    
    return &LazyValidator{
        cache:  NewValidationCache(ttl),
        engine: engine,
    }
}

func (l *LazyValidator) Validate(name string, value interface{}) error {
    // Generate cache key
    cacheKey := l.generateCacheKey(name, value)
    
    // Check cache first
    if valid, exists := l.cache.Get(cacheKey); exists {
        if !valid {
            return errors.New("validation failed")
        }
        return nil
    }
    
    // Perform validation
    err := l.engine.Validate(name, value)
    isValid := err == nil
    
    // Cache the result
    l.cache.Set(cacheKey, isValid)
    
    return err
}

func (l *LazyValidator) generateCacheKey(name string, value interface{}) string {
    jsonData, _ := json.Marshal(value)
    return fmt.Sprintf("%s:%s", name, string(jsonData))
}
```

## 🌍 **Real-World Examples**

### **API Configuration Validation**

```go
// TuskLang - API configuration validation
[api_validation]
api_config: @validate.api_config({
    "base_url": {"required": true, "format": "url"},
    "timeout": {"required": true, "type": "integer", "min": 1, "max": 300},
    "retries": {"type": "integer", "min": 0, "max": 10},
    "rate_limit": {"type": "integer", "min": 1},
    "auth_token": {"required": true, "min_length": 32}
})

endpoint_config: @validate.endpoint_config({
    "path": {"required": true, "pattern": "^/[a-zA-Z0-9/_-]+$"},
    "method": {"required": true, "enum": ["GET", "POST", "PUT", "DELETE"]},
    "timeout": {"type": "integer", "min": 1, "max": 60}
})
```

```go
// Go - API configuration validation handling
type APIValidation struct {
    APIConfig     string `tsk:"api_config"`
    EndpointConfig string `tsk:"endpoint_config"`
}

type APIConfig struct {
    BaseURL   string `json:"base_url"`
    Timeout   int    `json:"timeout"`
    Retries   int    `json:"retries"`
    RateLimit int    `json:"rate_limit"`
    AuthToken string `json:"auth_token"`
}

type EndpointConfig struct {
    Path    string `json:"path"`
    Method  string `json:"method"`
    Timeout int    `json:"timeout"`
}

func (a *APIValidation) ValidateAPIConfig(config APIConfig) error {
    // Validate base URL
    if config.BaseURL == "" {
        return errors.New("base URL is required")
    }
    
    if _, err := url.Parse(config.BaseURL); err != nil {
        return fmt.Errorf("invalid base URL: %w", err)
    }
    
    // Validate timeout
    if config.Timeout < 1 || config.Timeout > 300 {
        return errors.New("timeout must be between 1 and 300 seconds")
    }
    
    // Validate retries
    if config.Retries < 0 || config.Retries > 10 {
        return errors.New("retries must be between 0 and 10")
    }
    
    // Validate rate limit
    if config.RateLimit < 1 {
        return errors.New("rate limit must be at least 1")
    }
    
    // Validate auth token
    if config.AuthToken == "" {
        return errors.New("auth token is required")
    }
    
    if len(config.AuthToken) < 32 {
        return errors.New("auth token must be at least 32 characters")
    }
    
    return nil
}

func (a *APIValidation) ValidateEndpointConfig(config EndpointConfig) error {
    // Validate path
    if config.Path == "" {
        return errors.New("path is required")
    }
    
    pathRegex := regexp.MustCompile(`^/[a-zA-Z0-9/_-]+$`)
    if !pathRegex.MatchString(config.Path) {
        return errors.New("invalid path format")
    }
    
    // Validate method
    validMethods := []string{"GET", "POST", "PUT", "DELETE"}
    methodValid := false
    for _, method := range validMethods {
        if config.Method == method {
            methodValid = true
            break
        }
    }
    
    if !methodValid {
        return errors.New("invalid HTTP method")
    }
    
    // Validate timeout
    if config.Timeout < 1 || config.Timeout > 60 {
        return errors.New("timeout must be between 1 and 60 seconds")
    }
    
    return nil
}
```

### **Database Configuration Validation**

```go
// TuskLang - Database configuration validation
[database_validation]
postgres_config: @validate.database_config({
    "host": {"required": true, "type": "string"},
    "port": {"required": true, "type": "integer", "min": 1, "max": 65535},
    "name": {"required": true, "type": "string"},
    "user": {"required": true, "type": "string"},
    "password": {"type": "string"},
    "ssl_mode": {"type": "string", "enum": ["disable", "require", "verify-ca", "verify-full"]},
    "pool_size": {"type": "integer", "min": 1, "max": 100}
})

redis_config: @validate.database_config({
    "host": {"required": true, "type": "string"},
    "port": {"type": "integer", "min": 1, "max": 65535, "default": 6379},
    "password": {"type": "string"},
    "db": {"type": "integer", "min": 0, "max": 15, "default": 0},
    "timeout": {"type": "integer", "min": 1, "max": 300, "default": 30}
})
```

```go
// Go - Database configuration validation handling
type DatabaseValidation struct {
    PostgresConfig string `tsk:"postgres_config"`
    RedisConfig    string `tsk:"redis_config"`
}

type PostgresConfig struct {
    Host     string `json:"host"`
    Port     int    `json:"port"`
    Name     string `json:"name"`
    User     string `json:"user"`
    Password string `json:"password"`
    SSLMode  string `json:"ssl_mode"`
    PoolSize int    `json:"pool_size"`
}

type RedisConfig struct {
    Host     string `json:"host"`
    Port     int    `json:"port"`
    Password string `json:"password"`
    DB       int    `json:"db"`
    Timeout  int    `json:"timeout"`
}

func (d *DatabaseValidation) ValidatePostgresConfig(config PostgresConfig) error {
    // Validate host
    if config.Host == "" {
        return errors.New("host is required")
    }
    
    // Validate port
    if config.Port < 1 || config.Port > 65535 {
        return errors.New("port must be between 1 and 65535")
    }
    
    // Validate database name
    if config.Name == "" {
        return errors.New("database name is required")
    }
    
    // Validate user
    if config.User == "" {
        return errors.New("user is required")
    }
    
    // Validate SSL mode
    validSSLModes := []string{"disable", "require", "verify-ca", "verify-full"}
    sslModeValid := false
    for _, mode := range validSSLModes {
        if config.SSLMode == mode {
            sslModeValid = true
            break
        }
    }
    
    if config.SSLMode != "" && !sslModeValid {
        return errors.New("invalid SSL mode")
    }
    
    // Validate pool size
    if config.PoolSize < 1 || config.PoolSize > 100 {
        return errors.New("pool size must be between 1 and 100")
    }
    
    return nil
}

func (d *DatabaseValidation) ValidateRedisConfig(config RedisConfig) error {
    // Validate host
    if config.Host == "" {
        return errors.New("host is required")
    }
    
    // Set defaults
    if config.Port == 0 {
        config.Port = 6379
    }
    if config.DB == 0 {
        config.DB = 0
    }
    if config.Timeout == 0 {
        config.Timeout = 30
    }
    
    // Validate port
    if config.Port < 1 || config.Port > 65535 {
        return errors.New("port must be between 1 and 65535")
    }
    
    // Validate database number
    if config.DB < 0 || config.DB > 15 {
        return errors.New("database number must be between 0 and 15")
    }
    
    // Validate timeout
    if config.Timeout < 1 || config.Timeout > 300 {
        return errors.New("timeout must be between 1 and 300 seconds")
    }
    
    return nil
}
```

## 🎯 **Best Practices**

### **1. Validate Early and Often**

```go
// ✅ Good - Early validation
func (c *Config) LoadAndValidate() error {
    // Load configuration
    if err := c.Load(); err != nil {
        return fmt.Errorf("failed to load config: %w", err)
    }
    
    // Validate immediately
    if err := c.Validate(); err != nil {
        return fmt.Errorf("configuration validation failed: %w", err)
    }
    
    return nil
}

// ❌ Bad - Late validation
func (c *Config) Load() error {
    // Load configuration without validation
    // Validation happens much later, potentially causing issues
    return nil
}
```

### **2. Use Specific Error Messages**

```go
// ✅ Good - Specific error messages
func (v *Validator) ValidateEmail(email string) error {
    if email == "" {
        return errors.New("email address is required")
    }
    
    emailRegex := regexp.MustCompile(`^[^\s@]+@[^\s@]+\.[^\s@]+$`)
    if !emailRegex.MatchString(email) {
        return fmt.Errorf("invalid email format: %s", email)
    }
    
    return nil
}

// ❌ Bad - Generic error messages
func (v *Validator) ValidateEmail(email string) error {
    if email == "" || !strings.Contains(email, "@") {
        return errors.New("validation failed")
    }
    return nil
}
```

### **3. Cache Validation Results**

```go
// ✅ Good - Cached validation
func (l *LazyValidator) Validate(name string, value interface{}) error {
    cacheKey := l.generateCacheKey(name, value)
    
    if valid, exists := l.cache.Get(cacheKey); exists {
        if !valid {
            return errors.New("validation failed")
        }
        return nil
    }
    
    err := l.engine.Validate(name, value)
    l.cache.Set(cacheKey, err == nil)
    
    return err
}

// ❌ Bad - No caching
func (v *Validator) Validate(name string, value interface{}) error {
    // Always perform validation, even for same values
    return v.performValidation(name, value)
}
```

### **4. Use Type-Safe Validation**

```go
// ✅ Good - Type-safe validation
type UserValidator struct {
    user User
}

func (v *UserValidator) Validate() error {
    if v.user.Name == "" {
        return errors.New("name is required")
    }
    
    if v.user.Age < 18 {
        return errors.New("age must be at least 18")
    }
    
    return nil
}

// ❌ Bad - Untyped validation
func ValidateUser(data map[string]interface{}) error {
    name, ok := data["name"].(string)
    if !ok || name == "" {
        return errors.New("name is required")
    }
    
    age, ok := data["age"].(int)
    if !ok || age < 18 {
        return errors.New("age must be at least 18")
    }
    
    return nil
}
```

### **5. Document Validation Rules**

```go
// ✅ Good - Documented validation rules
type ValidationRules struct {
    // User validation rules
    // - Name: required, min length 1
    // - Email: required, valid email format
    // - Age: optional, between 18-100
    UserRules map[string]interface{} `json:"user_rules"`
    
    // Configuration validation rules
    // - AppName: required, string
    // - Port: required, integer 1-65535
    // - Debug: optional, boolean
    ConfigRules map[string]interface{} `json:"config_rules"`
}

// ❌ Bad - Undocumented validation rules
type ValidationRules struct {
    UserRules   map[string]interface{} `json:"user_rules"`
    ConfigRules map[string]interface{} `json:"config_rules"`
}
```

---

**🎉 You've mastered validation in TuskLang with Go!**

Validation in TuskLang ensures your configuration is bulletproof and reliable. With proper validation handling, you can build robust systems that catch errors early and maintain data integrity.

**Next Steps:**
- Explore [024-security-go.md](024-security-go.md) for security features
- Master [025-testing-go.md](025-testing-go.md) for testing strategies
- Dive into [026-performance-go.md](026-performance-go.md) for optimization

**Remember:** In TuskLang, validation isn't just checking—it's ensuring your configuration is bulletproof. Use it wisely to build reliable, secure systems. 