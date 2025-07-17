# Null Values in TuskLang - Go Guide

## 🎯 **The Power of Nothingness**

In TuskLang, null values aren't just absence—they're a conscious choice. We don't bow to any king, including the tyranny of undefined states. TuskLang gives you explicit control over null values with powerful validation and safety features.

## 📋 **Table of Contents**
- [Understanding Null in TuskLang](#understanding-null-in-tusklang)
- [Null Value Syntax](#null-value-syntax)
- [Go Integration](#go-integration)
- [Null Safety Patterns](#null-safety-patterns)
- [Validation and Error Handling](#validation-and-error-handling)
- [Performance Considerations](#performance-considerations)
- [Real-World Examples](#real-world-examples)
- [Best Practices](#best-practices)

## 🔍 **Understanding Null in TuskLang**

TuskLang treats null values as first-class citizens with explicit handling:

```go
// TuskLang configuration
[user_preferences]
theme: null
notifications: null
timezone: null

[system_config]
debug_mode: null
log_level: null
cache_enabled: null
```

```go
// Go integration
type UserPreferences struct {
    Theme          *string `tsk:"theme"`
    Notifications  *bool   `tsk:"notifications"`
    Timezone       *string `tsk:"timezone"`
}

type SystemConfig struct {
    DebugMode     *bool `tsk:"debug_mode"`
    LogLevel      *string `tsk:"log_level"`
    CacheEnabled  *bool `tsk:"cache_enabled"`
}
```

## 🎨 **Null Value Syntax**

### **Explicit Null Declaration**

```go
// TuskLang - Multiple syntax styles
[traditional]
value1: null

[object_style]
{
    "value2": null,
    "value3": null
}

[array_style]
[
    null,
    "actual_value",
    null
]
```

```go
// Go - Handling explicit nulls
type Config struct {
    Value1 *string `tsk:"value1"`
    Value2 *int    `tsk:"value2"`
    Value3 *bool   `tsk:"value3"`
    Array  []*string `tsk:"array"`
}

// Parse with null awareness
config := &Config{}
err := tusk.ParseFile("config.tsk", config)
if err != nil {
    log.Fatal(err)
}

// Check for null values
if config.Value1 == nil {
    fmt.Println("Value1 is explicitly null")
}
```

### **Conditional Null Assignment**

```go
// TuskLang - Dynamic null assignment
[conditional_config]
user_id: @env("USER_ID", null)
api_key: @env("API_KEY", null)
debug_mode: @env("DEBUG", null)

[smart_defaults]
cache_ttl: @if(@env("CACHE_TTL"), @env("CACHE_TTL"), null)
retry_count: @if(@env("RETRY_COUNT"), @env("RETRY_COUNT"), null)
```

```go
// Go - Handling conditional nulls
type ConditionalConfig struct {
    UserID    *string `tsk:"user_id"`
    APIKey    *string `tsk:"api_key"`
    DebugMode *bool   `tsk:"debug_mode"`
    CacheTTL  *int    `tsk:"cache_ttl"`
    RetryCount *int   `tsk:"retry_count"`
}

func (c *ConditionalConfig) IsComplete() bool {
    return c.UserID != nil && c.APIKey != nil
}

func (c *ConditionalConfig) GetCacheTTL() int {
    if c.CacheTTL != nil {
        return *c.CacheTTL
    }
    return 300 // Default 5 minutes
}
```

## 🔧 **Go Integration**

### **Pointer Types for Null Safety**

```go
// Go - Using pointers for null values
type DatabaseConfig struct {
    Host     *string `tsk:"host"`
    Port     *int    `tsk:"port"`
    Username *string `tsk:"username"`
    Password *string `tsk:"password"`
    Database *string `tsk:"database"`
}

func (db *DatabaseConfig) GetConnectionString() (string, error) {
    if db.Host == nil {
        return "", errors.New("database host is required")
    }
    
    port := 5432 // Default PostgreSQL port
    if db.Port != nil {
        port = *db.Port
    }
    
    return fmt.Sprintf("host=%s port=%d", *db.Host, port), nil
}
```

### **Null-Aware Validation**

```go
// Go - Custom validation for null values
type ValidationConfig struct {
    RequiredField *string `tsk:"required_field" validate:"required"`
    OptionalField *string `tsk:"optional_field"`
    NullableField *int    `tsk:"nullable_field"`
}

func (v *ValidationConfig) Validate() error {
    if v.RequiredField == nil {
        return errors.New("required_field cannot be null")
    }
    
    if v.NullableField != nil && *v.NullableField < 0 {
        return errors.New("nullable_field must be positive if not null")
    }
    
    return nil
}
```

### **Null Coalescing**

```go
// Go - Null coalescing patterns
type AppConfig struct {
    Theme      *string `tsk:"theme"`
    Language   *string `tsk:"language"`
    Timezone   *string `tsk:"timezone"`
}

func (a *AppConfig) GetTheme() string {
    if a.Theme != nil {
        return *a.Theme
    }
    return "default"
}

func (a *AppConfig) GetLanguage() string {
    if a.Language != nil {
        return *a.Language
    }
    return "en"
}

func (a *AppConfig) GetTimezone() string {
    if a.Timezone != nil {
        return *a.Timezone
    }
    return "UTC"
}
```

## 🛡️ **Null Safety Patterns**

### **Option Pattern Implementation**

```go
// Go - Option pattern for null safety
type Option[T any] struct {
    value *T
}

func Some[T any](value T) Option[T] {
    return Option[T]{value: &value}
}

func None[T any]() Option[T] {
    return Option[T]{value: nil}
}

func (o Option[T]) IsSome() bool {
    return o.value != nil
}

func (o Option[T]) IsNone() bool {
    return o.value == nil
}

func (o Option[T]) Unwrap() T {
    if o.value == nil {
        panic("attempted to unwrap None value")
    }
    return *o.value
}

func (o Option[T]) UnwrapOr(defaultValue T) T {
    if o.value == nil {
        return defaultValue
    }
    return *o.value
}

// Usage with TuskLang
type SafeConfig struct {
    APIKey Option[string] `tsk:"api_key"`
    Debug  Option[bool]   `tsk:"debug"`
    Port   Option[int]    `tsk:"port"`
}
```

### **Result Pattern for Error Handling**

```go
// Go - Result pattern for null with error context
type Result[T any] struct {
    value *T
    err   error
}

func Ok[T any](value T) Result[T] {
    return Result[T]{value: &value, err: nil}
}

func Err[T any](err error) Result[T] {
    return Result[T]{value: nil, err: err}
}

func (r Result[T]) IsOk() bool {
    return r.err == nil
}

func (r Result[T]) IsErr() bool {
    return r.err != nil
}

func (r Result[T]) Unwrap() T {
    if r.err != nil {
        panic(fmt.Sprintf("attempted to unwrap error: %v", r.err))
    }
    return *r.value
}

func (r Result[T]) UnwrapOr(defaultValue T) T {
    if r.err != nil {
        return defaultValue
    }
    return *r.value
}

// Usage with TuskLang configuration
func ParseConfig(path string) Result[AppConfig] {
    config := &AppConfig{}
    err := tusk.ParseFile(path, config)
    if err != nil {
        return Err[AppConfig](err)
    }
    return Ok(*config)
}
```

## ✅ **Validation and Error Handling**

### **Null-Specific Validation**

```go
// TuskLang - Null validation rules
[validation_rules]
required_field: @validate.required(["user_id", "api_key"])
nullable_field: @validate.nullable(["theme", "language"])
conditional_null: @validate.conditional(["debug_mode", "log_level"])
```

```go
// Go - Null validation implementation
type ValidationRules struct {
    RequiredField   *string `tsk:"required_field"`
    NullableField   *string `tsk:"nullable_field"`
    ConditionalNull *bool   `tsk:"conditional_null"`
}

func (v *ValidationRules) Validate() []error {
    var errors []error
    
    // Required field validation
    if v.RequiredField == nil {
        errors = append(errors, errors.New("required_field cannot be null"))
    }
    
    // Nullable field validation (always passes)
    // This is just for documentation purposes
    
    // Conditional null validation
    if v.ConditionalNull != nil && *v.ConditionalNull {
        // Additional validation when not null
    }
    
    return errors
}
```

### **Null-Aware Error Handling**

```go
// Go - Comprehensive null error handling
type ErrorHandler struct {
    Config *AppConfig
}

func (e *ErrorHandler) HandleNullValues() error {
    if e.Config == nil {
        return errors.New("configuration is null")
    }
    
    var missingFields []string
    
    if e.Config.Theme == nil {
        missingFields = append(missingFields, "theme")
    }
    
    if e.Config.Language == nil {
        missingFields = append(missingFields, "language")
    }
    
    if len(missingFields) > 0 {
        return fmt.Errorf("missing required fields: %v", missingFields)
    }
    
    return nil
}

func (e *ErrorHandler) HandlePartialNulls() map[string]interface{} {
    result := make(map[string]interface{})
    
    if e.Config.Theme != nil {
        result["theme"] = *e.Config.Theme
    } else {
        result["theme"] = "default"
    }
    
    if e.Config.Language != nil {
        result["language"] = *e.Config.Language
    } else {
        result["language"] = "en"
    }
    
    return result
}
```

## ⚡ **Performance Considerations**

### **Memory Efficiency**

```go
// Go - Memory-efficient null handling
type EfficientConfig struct {
    // Use pointers only when null is meaningful
    RequiredString string  `tsk:"required_string"`  // Never null
    OptionalString *string `tsk:"optional_string"`  // Can be null
    RequiredInt    int     `tsk:"required_int"`     // Never null
    OptionalInt    *int    `tsk:"optional_int"`     // Can be null
}

// Memory usage comparison
type MemoryHeavyConfig struct {
    // All fields as pointers (wasteful)
    String1 *string `tsk:"string1"`
    String2 *string `tsk:"string2"`
    Int1    *int    `tsk:"int1"`
    Int2    *int    `tsk:"int2"`
}

type MemoryEfficientConfig struct {
    // Only optional fields as pointers
    String1 string  `tsk:"string1"`  // Required
    String2 *string `tsk:"string2"`  // Optional
    Int1    int     `tsk:"int1"`     // Required
    Int2    *int    `tsk:"int2"`     // Optional
}
```

### **Null Check Optimization**

```go
// Go - Optimized null checking
type OptimizedConfig struct {
    Theme    *string `tsk:"theme"`
    Language *string `tsk:"language"`
    Debug    *bool   `tsk:"debug"`
}

func (o *OptimizedConfig) GetValues() map[string]interface{} {
    // Single pass through fields
    result := make(map[string]interface{})
    
    if o.Theme != nil {
        result["theme"] = *o.Theme
    }
    
    if o.Language != nil {
        result["language"] = *o.Language
    }
    
    if o.Debug != nil {
        result["debug"] = *o.Debug
    }
    
    return result
}

// Avoid repeated null checks
func (o *OptimizedConfig) GetThemeOrDefault() string {
    if o.Theme == nil {
        return "default"
    }
    return *o.Theme
}
```

## 🌍 **Real-World Examples**

### **Database Configuration with Null Safety**

```go
// TuskLang - Database configuration
[database]
host: @env("DB_HOST", null)
port: @env("DB_PORT", null)
username: @env("DB_USER", null)
password: @env("DB_PASS", null)
database: @env("DB_NAME", null)
ssl_mode: @env("DB_SSL", null)
```

```go
// Go - Database configuration handling
type DatabaseConfig struct {
    Host     *string `tsk:"host"`
    Port     *int    `tsk:"port"`
    Username *string `tsk:"username"`
    Password *string `tsk:"password"`
    Database *string `tsk:"database"`
    SSLMode  *string `tsk:"ssl_mode"`
}

func (db *DatabaseConfig) Validate() error {
    required := []struct {
        name  string
        value *string
    }{
        {"host", db.Host},
        {"username", db.Username},
        {"database", db.Database},
    }
    
    for _, field := range required {
        if field.value == nil {
            return fmt.Errorf("database %s is required", field.name)
        }
    }
    
    return nil
}

func (db *DatabaseConfig) GetConnectionString() string {
    port := 5432
    if db.Port != nil {
        port = *db.Port
    }
    
    sslMode := "disable"
    if db.SSLMode != nil {
        sslMode = *db.SSLMode
    }
    
    return fmt.Sprintf(
        "host=%s port=%d user=%s password=%s dbname=%s sslmode=%s",
        *db.Host, port, *db.Username, *db.Password, *db.Database, sslMode,
    )
}
```

### **API Configuration with Optional Fields**

```go
// TuskLang - API configuration
[api]
base_url: @env("API_BASE_URL", null)
timeout: @env("API_TIMEOUT", null)
retry_count: @env("API_RETRY_COUNT", null)
rate_limit: @env("API_RATE_LIMIT", null)
auth_token: @env("API_AUTH_TOKEN", null)
```

```go
// Go - API configuration handling
type APIConfig struct {
    BaseURL    *string `tsk:"base_url"`
    Timeout    *int    `tsk:"timeout"`
    RetryCount *int    `tsk:"retry_count"`
    RateLimit  *int    `tsk:"rate_limit"`
    AuthToken  *string `tsk:"auth_token"`
}

func (api *APIConfig) GetTimeout() time.Duration {
    if api.Timeout != nil {
        return time.Duration(*api.Timeout) * time.Second
    }
    return 30 * time.Second // Default 30 seconds
}

func (api *APIConfig) GetRetryCount() int {
    if api.RetryCount != nil {
        return *api.RetryCount
    }
    return 3 // Default 3 retries
}

func (api *APIConfig) IsAuthenticated() bool {
    return api.AuthToken != nil && *api.AuthToken != ""
}
```

### **User Preferences with Null Handling**

```go
// TuskLang - User preferences
[user_preferences]
theme: @env("USER_THEME", null)
language: @env("USER_LANGUAGE", null)
timezone: @env("USER_TIMEZONE", null)
notifications: @env("USER_NOTIFICATIONS", null)
privacy_level: @env("USER_PRIVACY", null)
```

```go
// Go - User preferences handling
type UserPreferences struct {
    Theme         *string `tsk:"theme"`
    Language      *string `tsk:"language"`
    Timezone      *string `tsk:"timezone"`
    Notifications *bool   `tsk:"notifications"`
    PrivacyLevel  *int    `tsk:"privacy_level"`
}

func (u *UserPreferences) GetEffectiveSettings() map[string]interface{} {
    settings := make(map[string]interface{})
    
    // Apply defaults for null values
    settings["theme"] = u.GetTheme()
    settings["language"] = u.GetLanguage()
    settings["timezone"] = u.GetTimezone()
    settings["notifications"] = u.GetNotifications()
    settings["privacy_level"] = u.GetPrivacyLevel()
    
    return settings
}

func (u *UserPreferences) GetTheme() string {
    if u.Theme != nil {
        return *u.Theme
    }
    return "light"
}

func (u *UserPreferences) GetLanguage() string {
    if u.Language != nil {
        return *u.Language
    }
    return "en"
}

func (u *UserPreferences) GetTimezone() string {
    if u.Timezone != nil {
        return *u.Timezone
    }
    return "UTC"
}

func (u *UserPreferences) GetNotifications() bool {
    if u.Notifications != nil {
        return *u.Notifications
    }
    return true
}

func (u *UserPreferences) GetPrivacyLevel() int {
    if u.PrivacyLevel != nil {
        return *u.PrivacyLevel
    }
    return 1
}
```

## 🎯 **Best Practices**

### **1. Use Pointers Only When Necessary**

```go
// ✅ Good - Only optional fields as pointers
type GoodConfig struct {
    RequiredField string  `tsk:"required_field"`  // Never null
    OptionalField *string `tsk:"optional_field"`  // Can be null
}

// ❌ Bad - All fields as pointers
type BadConfig struct {
    RequiredField *string `tsk:"required_field"`  // Unnecessary pointer
    OptionalField *string `tsk:"optional_field"`  // Necessary pointer
}
```

### **2. Provide Meaningful Defaults**

```go
// ✅ Good - Clear default handling
func (c *Config) GetValueOrDefault() string {
    if c.Value != nil {
        return *c.Value
    }
    return "meaningful_default"
}

// ❌ Bad - No default handling
func (c *Config) GetValue() string {
    return *c.Value // Will panic if nil
}
```

### **3. Validate Early and Often**

```go
// ✅ Good - Early validation
func (c *Config) Validate() error {
    if c.RequiredField == nil {
        return errors.New("required_field cannot be null")
    }
    return nil
}

// ❌ Bad - Late validation
func (c *Config) Process() error {
    // Process for a while...
    if c.RequiredField == nil {
        return errors.New("required_field cannot be null") // Too late!
    }
    return nil
}
```

### **4. Use Type-Safe Null Handling**

```go
// ✅ Good - Type-safe null handling
type SafeConfig struct {
    StringField *string `tsk:"string_field"`
    IntField    *int    `tsk:"int_field"`
    BoolField   *bool   `tsk:"bool_field"`
}

// ❌ Bad - Interface-based null handling
type UnsafeConfig struct {
    StringField interface{} `tsk:"string_field"`
    IntField    interface{} `tsk:"int_field"`
    BoolField   interface{} `tsk:"bool_field"`
}
```

### **5. Document Null Semantics**

```go
// ✅ Good - Clear documentation
type DocumentedConfig struct {
    // RequiredField is required and cannot be null
    RequiredField string `tsk:"required_field"`
    
    // OptionalField can be null and will use default "value"
    OptionalField *string `tsk:"optional_field"`
    
    // ConditionalField can be null but requires validation if present
    ConditionalField *int `tsk:"conditional_field"`
}

// ❌ Bad - No documentation
type UndocumentedConfig struct {
    Field1 string  `tsk:"field1"`
    Field2 *string `tsk:"field2"`
    Field3 *int    `tsk:"field3"`
}
```

---

**🎉 You've mastered null values in TuskLang with Go!**

Null values in TuskLang aren't just absence—they're a conscious choice that gives you explicit control over your configuration. With proper null handling, you can build robust, type-safe applications that gracefully handle missing or optional values.

**Next Steps:**
- Explore [018-conditional-logic-go.md](018-conditional-logic-go.md) for dynamic configuration
- Master [019-functions-go.md](019-functions-go.md) for executable configuration
- Dive into [020-imports-go.md](020-imports-go.md) for modular configuration

**Remember:** In TuskLang, null is not a bug—it's a feature. Use it wisely, handle it safely, and build configurations that adapt to your needs. 