# ✅ TuskLang Go Booleans Guide

**"We don't bow to any king" - Go Edition**

Master boolean handling in TuskLang and learn how to work with true/false values, logical operations, and conditional logic in Go applications. This guide covers boolean syntax, validation, operations, and best practices.

## 🎯 Boolean Fundamentals

### Basic Boolean Syntax

```go
// config.tsk
[booleans]
enabled: true
disabled: false
debug_mode: true
production: false

[features]
caching: true
logging: true
monitoring: false
analytics: false
ssl: true
compression: true
rate_limiting: false
```

### Go Struct Mapping

```go
// main.go
package main

import (
    "fmt"
    "github.com/tusklang/go"
)

type BooleanConfig struct {
    Enabled bool `tsk:"enabled"` // Feature enabled
    Disabled bool `tsk:"disabled"` // Feature disabled
    DebugMode bool `tsk:"debug_mode"` // Debug mode
    Production bool `tsk:"production"` // Production mode
}

type FeatureConfig struct {
    Caching      bool `tsk:"caching"`       // Enable caching
    Logging      bool `tsk:"logging"`       // Enable logging
    Monitoring   bool `tsk:"monitoring"`    // Enable monitoring
    Analytics    bool `tsk:"analytics"`     // Enable analytics
    SSL          bool `tsk:"ssl"`           // Enable SSL
    Compression  bool `tsk:"compression"`   // Enable compression
    RateLimiting bool `tsk:"rate_limiting"` // Enable rate limiting
}

func main() {
    parser := tusklanggo.NewEnhancedParser()
    
    data, err := parser.ParseFile("config.tsk")
    if err != nil {
        panic(err)
    }
    
    var booleanConfig BooleanConfig
    err = tusklanggo.UnmarshalTSK(data["booleans"].(map[string]interface{}), &booleanConfig)
    if err != nil {
        panic(err)
    }
    
    var featureConfig FeatureConfig
    err = tusklanggo.UnmarshalTSK(data["features"].(map[string]interface{}), &featureConfig)
    if err != nil {
        panic(err)
    }
    
    fmt.Printf("Enabled: %v\n", booleanConfig.Enabled)
    fmt.Printf("Debug Mode: %v\n", booleanConfig.DebugMode)
    fmt.Printf("Caching: %v\n", featureConfig.Caching)
    fmt.Printf("SSL: %v\n", featureConfig.SSL)
}
```

## 🔧 Boolean Operations

### Logical Operations

```go
// config.tsk
[logical]
and_result: @logic.and(true, true, false)
or_result: @logic.or(true, false, false)
not_result: @logic.not(true)
xor_result: @logic.xor(true, false)
nand_result: @logic.nand(true, true)
nor_result: @logic.nor(false, false)
```

### Conditional Logic

```go
// config.tsk
[conditional]
debug_enabled: @env("DEBUG", "false")
production_mode: @env("ENVIRONMENT", "development") == "production"
ssl_required: @env("SSL_REQUIRED", "true")
caching_enabled: @env("CACHE_ENABLED", "false")

[computed]
should_log: @logic.and(@conditional.debug_enabled, @conditional.production_mode)
should_cache: @logic.or(@conditional.caching_enabled, @conditional.production_mode)
should_use_ssl: @logic.and(@conditional.ssl_required, @conditional.production_mode)
```

### Boolean Functions

```go
// config.tsk
[functions]
is_production: """
function isProduction(env) {
    return env === 'production';
}
"""

should_enable_feature: """
function shouldEnableFeature(feature, env, debug) {
    if (feature === 'debug') {
        return debug === true;
    }
    if (feature === 'monitoring') {
        return env === 'production';
    }
    return true;
}
"""

[computed_features]
debug_enabled: @fujsen(is_production, @env("ENVIRONMENT", "development"))
monitoring_enabled: @fujsen(should_enable_feature, "monitoring", @env("ENVIRONMENT", "development"), @env("DEBUG", "false"))
```

## 📊 Boolean Validation

### Type Validation

```go
// main.go
type ValidatedBooleanConfig struct {
    DebugMode   bool `tsk:"debug_mode" validate:"boolean"`
    Production  bool `tsk:"production" validate:"boolean"`
    SSLEnabled  bool `tsk:"ssl_enabled" validate:"boolean"`
    Logging     bool `tsk:"logging" validate:"boolean"`
}

func validateBooleans(config *ValidatedBooleanConfig) error {
    // Boolean validation is typically handled by the parser
    // This function can be used for business logic validation
    
    // Example: Production mode should have SSL enabled
    if config.Production && !config.SSLEnabled {
        return fmt.Errorf("SSL must be enabled in production mode")
    }
    
    // Example: Debug mode should have logging enabled
    if config.DebugMode && !config.Logging {
        return fmt.Errorf("logging must be enabled in debug mode")
    }
    
    return nil
}
```

### String to Boolean Conversion

```go
// main.go
func stringToBool(value string) (bool, error) {
    switch strings.ToLower(strings.TrimSpace(value)) {
    case "true", "1", "yes", "on", "enabled":
        return true, nil
    case "false", "0", "no", "off", "disabled":
        return false, nil
    default:
        return false, fmt.Errorf("cannot convert '%s' to boolean", value)
    }
}

func safeGetBool(data map[string]interface{}, key string) (bool, error) {
    value, exists := data[key]
    if !exists {
        return false, fmt.Errorf("key '%s' not found", key)
    }
    
    switch v := value.(type) {
    case bool:
        return v, nil
    case string:
        return stringToBool(v)
    case int:
        return v != 0, nil
    case float64:
        return v != 0, nil
    default:
        return false, fmt.Errorf("key '%s' cannot be converted to boolean, got %T", key, value)
    }
}
```

## 🔢 Dynamic Booleans

### Environment-Based Booleans

```go
// config.tsk
[environment]
debug_mode: @env("DEBUG", "false")
production: @env("ENVIRONMENT", "development") == "production"
ssl_enabled: @env("SSL_ENABLED", "true")
caching_enabled: @env("CACHE_ENABLED", "false")
monitoring_enabled: @env("MONITORING_ENABLED", "true")
analytics_enabled: @env("ANALYTICS_ENABLED", "false")
```

### Computed Booleans

```go
// config.tsk
[computed]
has_users: @query("SELECT COUNT(*) FROM users") > 0
has_orders: @query("SELECT COUNT(*) FROM orders") > 0
is_weekend: @date.format(@date.now(), "N") >= 6
is_business_hours: @logic.and(@date.format(@date.now(), "N") < 6, @date.format(@date.now(), "G") >= 9, @date.format(@date.now(), "G") <= 17)
```

### Conditional Booleans

```go
// config.tsk
[conditional]
enable_debug: @if(@env("ENVIRONMENT") == "development", true, false)
enable_monitoring: @if(@env("ENVIRONMENT") == "production", true, false)
enable_ssl: @if(@env("ENVIRONMENT") == "production", true, false)
enable_caching: @if(@env("ENVIRONMENT") == "production", true, false)
enable_analytics: @if(@env("ENVIRONMENT") == "production", true, false)
```

```go
// main.go
type EnvironmentConfig struct {
    DebugMode        bool `tsk:"debug_mode"`
    Production       bool `tsk:"production"`
    SSLEnabled       bool `tsk:"ssl_enabled"`
    CachingEnabled   bool `tsk:"caching_enabled"`
    MonitoringEnabled bool `tsk:"monitoring_enabled"`
    AnalyticsEnabled bool `tsk:"analytics_enabled"`
}

type ComputedConfig struct {
    HasUsers        interface{} `tsk:"has_users"`
    HasOrders       interface{} `tsk:"has_orders"`
    IsWeekend       interface{} `tsk:"is_weekend"`
    IsBusinessHours interface{} `tsk:"is_business_hours"`
}

type ConditionalConfig struct {
    EnableDebug     bool `tsk:"enable_debug"`
    EnableMonitoring bool `tsk:"enable_monitoring"`
    EnableSSL       bool `tsk:"enable_ssl"`
    EnableCaching   bool `tsk:"enable_caching"`
    EnableAnalytics bool `tsk:"enable_analytics"`
}
```

## 🎯 Boolean Logic

### Complex Boolean Expressions

```go
// config.tsk
[complex_logic]
should_send_email: @logic.and(@user.has_email, @logic.or(@user.is_active, @user.is_premium))
should_show_ads: @logic.and(@logic.not(@user.is_premium), @logic.not(@user.is_admin))
should_enable_feature: @logic.or(@user.is_admin, @logic.and(@user.is_premium, @feature.is_beta))
should_require_2fa: @logic.or(@user.is_admin, @logic.and(@user.has_sensitive_data, @security.require_2fa))
```

### Boolean Algebra

```go
// config.tsk
[algebra]
a: true
b: false
c: true

[results]
and_ab: @logic.and(@algebra.a, @algebra.b)
or_ab: @logic.or(@algebra.a, @algebra.b)
not_a: @logic.not(@algebra.a)
xor_ab: @logic.xor(@algebra.a, @algebra.b)
complex: @logic.and(@logic.or(@algebra.a, @algebra.b), @logic.not(@algebra.c))
```

### Truth Tables

```go
// config.tsk
[truth_tables]
and_table: {
    "true,true": @logic.and(true, true),
    "true,false": @logic.and(true, false),
    "false,true": @logic.and(false, true),
    "false,false": @logic.and(false, false)
}

or_table: {
    "true,true": @logic.or(true, true),
    "true,false": @logic.or(true, false),
    "false,true": @logic.or(false, true),
    "false,false": @logic.or(false, false)
}

xor_table: {
    "true,true": @logic.xor(true, true),
    "true,false": @logic.xor(true, false),
    "false,true": @logic.xor(false, true),
    "false,false": @logic.xor(false, false)
}
```

## 🔧 Boolean Functions

### Custom Boolean Functions

```go
// config.tsk
[functions]
is_valid_email: """
function isValidEmail(email) {
    return email.includes('@') && email.includes('.');
}
"""

is_valid_phone: """
function isValidPhone(phone) {
    return phone.length >= 10 && phone.length <= 15;
}
"""

is_valid_password: """
function isValidPassword(password) {
    return password.length >= 8 && 
           /[A-Z]/.test(password) && 
           /[a-z]/.test(password) && 
           /[0-9]/.test(password);
}
"""

[validation]
email_valid: @fujsen(is_valid_email, @user.email)
phone_valid: @fujsen(is_valid_phone, @user.phone)
password_valid: @fujsen(is_valid_password, @user.password)
```

### Business Logic Functions

```go
// config.tsk
[functions]
should_show_premium_features: """
function shouldShowPremium(user, features) {
    return user.isPremium || user.isAdmin || features.isBeta;
}
"""

should_require_verification: """
function shouldRequireVerification(user, settings) {
    return user.isNew && settings.requireVerification && !user.isVerified;
}
"""

should_enable_advanced_search: """
function shouldEnableAdvancedSearch(user, settings) {
    return user.isPremium || user.isAdmin || settings.enableForAll;
}
"""

[business_logic]
show_premium: @fujsen(should_show_premium_features, @user, @features)
require_verification: @fujsen(should_require_verification, @user, @settings)
enable_advanced_search: @fujsen(should_enable_advanced_search, @user, @settings)
```

## 🎯 Best Practices

### 1. Boolean Naming

```go
// Good - Clear, descriptive boolean names
[features]
enable_caching: true
enable_logging: true
enable_monitoring: false
enable_ssl: true
enable_compression: true
enable_rate_limiting: false

# Bad - Unclear boolean names
[features]
caching: true
logging: true
monitoring: false
ssl: true
compression: true
rate_limiting: false
```

### 2. Boolean Validation

```go
// Good - Validate boolean logic
func validateBooleanLogic(config *Config) error {
    // Production mode should have SSL enabled
    if config.Production && !config.SSLEnabled {
        return fmt.Errorf("SSL must be enabled in production mode")
    }
    
    // Debug mode should have logging enabled
    if config.DebugMode && !config.Logging {
        return fmt.Errorf("logging must be enabled in debug mode")
    }
    
    // Monitoring should be enabled in production
    if config.Production && !config.Monitoring {
        return fmt.Errorf("monitoring should be enabled in production")
    }
    
    return nil
}

# Bad - No validation
func loadConfig(filename string) (*Config, error) {
    // Load config without validation
    return config, nil
}
```

### 3. Boolean Defaults

```go
// Good - Provide sensible defaults
[app]
debug_mode: @env("DEBUG", "false")
production: @env("ENVIRONMENT", "development") == "production"
ssl_enabled: @env("SSL_ENABLED", "true")
caching_enabled: @env("CACHE_ENABLED", "false")

# Bad - No defaults, will fail if env vars not set
[app]
debug_mode: @env("DEBUG")
production: @env("ENVIRONMENT") == "production"
ssl_enabled: @env("SSL_ENABLED")
caching_enabled: @env("CACHE_ENABLED")
```

### 4. Boolean Documentation

```go
// Good - Document boolean logic
[features]
# Enable response caching for better performance
enable_caching: @env("ENABLE_CACHING", "true")

# Enable detailed logging (may impact performance)
enable_logging: @env("ENABLE_LOGGING", "false")

# Enable system monitoring (required for production)
enable_monitoring: @env("ENABLE_MONITORING", "true")

# Enable SSL/TLS encryption (required for production)
enable_ssl: @env("ENABLE_SSL", "true")

# Bad - No documentation
[features]
enable_caching: true
enable_logging: false
enable_monitoring: true
enable_ssl: true
```

## 📊 Complete Example

### Configuration File

```go
// config.tsk
# ========================================
# BOOLEAN CONFIGURATION
# ========================================
[app]
debug_mode: @env("DEBUG", "false")
production: @env("ENVIRONMENT", "development") == "production"
ssl_enabled: @env("SSL_ENABLED", "true")
caching_enabled: @env("CACHE_ENABLED", "false")

[features]
# Enable response caching for better performance
enable_caching: @env("ENABLE_CACHING", "true")

# Enable detailed logging (may impact performance)
enable_logging: @env("ENABLE_LOGGING", "false")

# Enable system monitoring (required for production)
enable_monitoring: @env("ENABLE_MONITORING", "true")

# Enable SSL/TLS encryption (required for production)
enable_ssl: @env("ENABLE_SSL", "true")

# Enable response compression
enable_compression: @env("ENABLE_COMPRESSION", "true")

# Enable rate limiting
enable_rate_limiting: @env("ENABLE_RATE_LIMITING", "false")

# Enable analytics tracking
enable_analytics: @env("ENABLE_ANALYTICS", "false")

[security]
# Require two-factor authentication for admin users
require_2fa_admin: @env("REQUIRE_2FA_ADMIN", "true")

# Require two-factor authentication for all users
require_2fa_all: @env("REQUIRE_2FA_ALL", "false")

# Enable session timeout
enable_session_timeout: @env("ENABLE_SESSION_TIMEOUT", "true")

# Enable CSRF protection
enable_csrf: @env("ENABLE_CSRF", "true")

[computed]
# Should enable debug features
should_debug: @logic.and(@app.debug_mode, @logic.not(@app.production))

# Should enable monitoring
should_monitor: @logic.or(@app.production, @features.enable_monitoring)

# Should require SSL
should_ssl: @logic.or(@app.production, @app.ssl_enabled)

# Should enable caching
should_cache: @logic.or(@app.production, @features.enable_caching)

# Should require 2FA
should_2fa: @logic.or(@security.require_2fa_all, @logic.and(@security.require_2fa_admin, @user.is_admin))

[functions]
is_valid_user: """
function isValidUser(user) {
    return user.isActive && user.isVerified && !user.isBanned;
}
"""

should_show_premium: """
function shouldShowPremium(user, features) {
    return user.isPremium || user.isAdmin || features.isBeta;
}
"""

should_require_verification: """
function shouldRequireVerification(user, settings) {
    return user.isNew && settings.requireVerification && !user.isVerified;
}
"""

[validation]
user_valid: @fujsen(is_valid_user, @user)
show_premium: @fujsen(should_show_premium, @user, @features)
require_verification: @fujsen(should_require_verification, @user, @settings)
```

### Go Application

```go
// main.go
package main

import (
    "fmt"
    "log"
    "strings"
    "github.com/tusklang/go"
)

// Configuration structures
type AppConfig struct {
    DebugMode      bool `tsk:"debug_mode"`
    Production     bool `tsk:"production"`
    SSLEnabled     bool `tsk:"ssl_enabled"`
    CachingEnabled bool `tsk:"caching_enabled"`
}

type FeatureConfig struct {
    EnableCaching      bool `tsk:"enable_caching"`
    EnableLogging      bool `tsk:"enable_logging"`
    EnableMonitoring   bool `tsk:"enable_monitoring"`
    EnableSSL          bool `tsk:"enable_ssl"`
    EnableCompression  bool `tsk:"enable_compression"`
    EnableRateLimiting bool `tsk:"enable_rate_limiting"`
    EnableAnalytics    bool `tsk:"enable_analytics"`
}

type SecurityConfig struct {
    Require2FAAdmin      bool `tsk:"require_2fa_admin"`
    Require2FAAll        bool `tsk:"require_2fa_all"`
    EnableSessionTimeout bool `tsk:"enable_session_timeout"`
    EnableCSRF           bool `tsk:"enable_csrf"`
}

type ComputedConfig struct {
    ShouldDebug   bool `tsk:"should_debug"`
    ShouldMonitor bool `tsk:"should_monitor"`
    ShouldSSL     bool `tsk:"should_ssl"`
    ShouldCache   bool `tsk:"should_cache"`
    Should2FA     bool `tsk:"should_2fa"`
}

type ValidationConfig struct {
    UserValid           interface{} `tsk:"user_valid"`
    ShowPremium         interface{} `tsk:"show_premium"`
    RequireVerification interface{} `tsk:"require_verification"`
}

type Config struct {
    App        AppConfig        `tsk:"app"`
    Features   FeatureConfig    `tsk:"features"`
    Security   SecurityConfig   `tsk:"security"`
    Computed   ComputedConfig   `tsk:"computed"`
    Validation ValidationConfig `tsk:"validation"`
}

func main() {
    // Load configuration
    config, err := loadConfig("config.tsk")
    if err != nil {
        log.Fatalf("Failed to load configuration: %v", err)
    }
    
    // Validate boolean logic
    if err := validateBooleanLogic(config); err != nil {
        log.Fatalf("Boolean validation failed: %v", err)
    }
    
    // Use configuration
    fmt.Printf("🚀 App Mode: %s\n", getModeString(config.App.Production))
    fmt.Printf("🔧 Debug Mode: %v\n", config.App.DebugMode)
    fmt.Printf("🔒 SSL Enabled: %v\n", config.App.SSLEnabled)
    fmt.Printf("💾 Caching Enabled: %v\n", config.App.CachingEnabled)
    
    fmt.Printf("📊 Features:\n")
    fmt.Printf("  - Caching: %v\n", config.Features.EnableCaching)
    fmt.Printf("  - Logging: %v\n", config.Features.EnableLogging)
    fmt.Printf("  - Monitoring: %v\n", config.Features.EnableMonitoring)
    fmt.Printf("  - SSL: %v\n", config.Features.EnableSSL)
    fmt.Printf("  - Compression: %v\n", config.Features.EnableCompression)
    fmt.Printf("  - Rate Limiting: %v\n", config.Features.EnableRateLimiting)
    fmt.Printf("  - Analytics: %v\n", config.Features.EnableAnalytics)
    
    fmt.Printf("🔐 Security:\n")
    fmt.Printf("  - 2FA Admin: %v\n", config.Security.Require2FAAdmin)
    fmt.Printf("  - 2FA All: %v\n", config.Security.Require2FAAll)
    fmt.Printf("  - Session Timeout: %v\n", config.Security.EnableSessionTimeout)
    fmt.Printf("  - CSRF Protection: %v\n", config.Security.EnableCSRF)
    
    fmt.Printf("🧮 Computed:\n")
    fmt.Printf("  - Should Debug: %v\n", config.Computed.ShouldDebug)
    fmt.Printf("  - Should Monitor: %v\n", config.Computed.ShouldMonitor)
    fmt.Printf("  - Should SSL: %v\n", config.Computed.ShouldSSL)
    fmt.Printf("  - Should Cache: %v\n", config.Computed.ShouldCache)
    fmt.Printf("  - Should 2FA: %v\n", config.Computed.Should2FA)
    
    // Apply configuration
    applyConfiguration(config)
}

func loadConfig(filename string) (*Config, error) {
    parser := tusklanggo.NewEnhancedParser()
    
    data, err := parser.ParseFile(filename)
    if err != nil {
        return nil, fmt.Errorf("failed to parse config file: %w", err)
    }
    
    var config Config
    err = tusklanggo.UnmarshalTSK(data, &config)
    if err != nil {
        return nil, fmt.Errorf("failed to unmarshal config: %w", err)
    }
    
    return &config, nil
}

func validateBooleanLogic(config *Config) error {
    // Production mode should have SSL enabled
    if config.App.Production && !config.App.SSLEnabled {
        return fmt.Errorf("SSL must be enabled in production mode")
    }
    
    // Debug mode should have logging enabled
    if config.App.DebugMode && !config.Features.EnableLogging {
        return fmt.Errorf("logging must be enabled in debug mode")
    }
    
    // Monitoring should be enabled in production
    if config.App.Production && !config.Features.EnableMonitoring {
        return fmt.Errorf("monitoring should be enabled in production")
    }
    
    // CSRF protection should be enabled in production
    if config.App.Production && !config.Security.EnableCSRF {
        return fmt.Errorf("CSRF protection should be enabled in production")
    }
    
    return nil
}

func getModeString(production bool) string {
    if production {
        return "Production"
    }
    return "Development"
}

func applyConfiguration(config *Config) {
    fmt.Printf("\n🔧 Applying configuration...\n")
    
    if config.Computed.ShouldDebug {
        fmt.Printf("  - Enabling debug mode\n")
    }
    
    if config.Computed.ShouldSSL {
        fmt.Printf("  - Enabling SSL/TLS\n")
    }
    
    if config.Computed.ShouldCache {
        fmt.Printf("  - Enabling caching\n")
    }
    
    if config.Computed.ShouldMonitor {
        fmt.Printf("  - Enabling monitoring\n")
    }
    
    if config.Computed.Should2FA {
        fmt.Printf("  - Enabling 2FA requirement\n")
    }
    
    fmt.Printf("✅ Configuration applied successfully!\n")
}
```

## 📚 Summary

You've learned:

1. **Boolean Fundamentals** - Basic syntax and Go struct mapping
2. **Boolean Operations** - Logical operations and conditional logic
3. **Boolean Validation** - Type validation and string conversion
4. **Dynamic Booleans** - Environment-based and computed booleans
5. **Boolean Logic** - Complex expressions and boolean algebra
6. **Boolean Functions** - Custom boolean functions and business logic
7. **Best Practices** - Naming, validation, defaults, and documentation
8. **Complete Examples** - Real-world boolean configuration management

## 🚀 Next Steps

Now that you understand boolean handling:

1. **Implement Validation** - Add boolean logic validation to your applications
2. **Use Logical Operations** - Leverage TuskLang's logical functions
3. **Create Business Logic** - Build complex boolean expressions
4. **Document Boolean Logic** - Clearly document boolean configurations
5. **Test Boolean Logic** - Ensure boolean logic works correctly

---

**"We don't bow to any king"** - You now have the power to handle booleans effectively in your TuskLang Go applications! 