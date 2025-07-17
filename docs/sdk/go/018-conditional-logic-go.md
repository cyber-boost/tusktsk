# Conditional Logic in TuskLang - Go Guide

## 🎯 **The Power of Dynamic Configuration**

In TuskLang, conditional logic isn't just about if/else—it's about making your configuration alive and responsive. We don't bow to any king, especially not static configuration files. TuskLang gives you the power to make decisions at configuration time, not just runtime.

## 📋 **Table of Contents**
- [Understanding Conditional Logic](#understanding-conditional-logic)
- [Basic Conditional Syntax](#basic-conditional-syntax)
- [Go Integration](#go-integration)
- [Advanced Conditional Patterns](#advanced-conditional-patterns)
- [Environment-Based Logic](#environment-based-logic)
- [Performance Considerations](#performance-considerations)
- [Real-World Examples](#real-world-examples)
- [Best Practices](#best-practices)

## 🔍 **Understanding Conditional Logic**

TuskLang's conditional logic operates at configuration time, allowing dynamic decision-making:

```go
// TuskLang configuration with conditionals
[environment_config]
debug_mode: @if(@env("DEBUG"), true, false)
log_level: @if(@env("PRODUCTION"), "error", "debug")
cache_enabled: @if(@env("CACHE"), true, false)

[feature_flags]
beta_features: @if(@env("BETA"), true, false)
experimental: @if(@env("EXPERIMENTAL"), true, false)
```

```go
// Go integration
type EnvironmentConfig struct {
    DebugMode     bool   `tsk:"debug_mode"`
    LogLevel      string `tsk:"log_level"`
    CacheEnabled  bool   `tsk:"cache_enabled"`
}

type FeatureFlags struct {
    BetaFeatures  bool `tsk:"beta_features"`
    Experimental  bool `tsk:"experimental"`
}
```

## 🎨 **Basic Conditional Syntax**

### **Simple If Statements**

```go
// TuskLang - Basic conditional syntax
[basic_conditionals]
value1: @if(condition, true_value, false_value)
value2: @if(@env("DEBUG"), "debug", "production")
value3: @if(@env("FEATURE_FLAG"), "enabled", "disabled")
```

```go
// Go - Handling basic conditionals
type BasicConditionals struct {
    Value1 string `tsk:"value1"`
    Value2 string `tsk:"value2"`
    Value3 string `tsk:"value3"`
}

func (b *BasicConditionals) IsDebugMode() bool {
    return b.Value2 == "debug"
}

func (b *BasicConditionals) IsFeatureEnabled() bool {
    return b.Value3 == "enabled"
}
```

### **Nested Conditionals**

```go
// TuskLang - Nested conditional logic
[nested_conditionals]
log_level: @if(@env("PRODUCTION"), 
    "error", 
    @if(@env("STAGING"), "warn", "debug")
)

cache_strategy: @if(@env("MEMORY_LIMIT") > 1024,
    "redis",
    @if(@env("DISK_SPACE") > 1000, "file", "memory")
)
```

```go
// Go - Handling nested conditionals
type NestedConditionals struct {
    LogLevel      string `tsk:"log_level"`
    CacheStrategy string `tsk:"cache_strategy"`
}

func (n *NestedConditionals) GetLogLevel() string {
    return n.LogLevel
}

func (n *NestedConditionals) GetCacheStrategy() string {
    return n.CacheStrategy
}

func (n *NestedConditionals) IsProduction() bool {
    return n.LogLevel == "error"
}

func (n *NestedConditionals) IsStaging() bool {
    return n.LogLevel == "warn"
}

func (n *NestedConditionals) IsDevelopment() bool {
    return n.LogLevel == "debug"
}
```

### **Complex Conditional Logic**

```go
// TuskLang - Complex conditional expressions
[complex_conditionals]
database_url: @if(@env("DATABASE_URL"),
    @env("DATABASE_URL"),
    @if(@env("DB_HOST") && @env("DB_NAME"),
        @concat("postgresql://", @env("DB_USER"), ":", @env("DB_PASS"), "@", @env("DB_HOST"), ":", @env("DB_PORT"), "/", @env("DB_NAME")),
        "sqlite://local.db"
    )
)

api_timeout: @if(@env("API_TIMEOUT"),
    @env("API_TIMEOUT"),
    @if(@env("PRODUCTION"), 30, 60)
)
```

```go
// Go - Handling complex conditionals
type ComplexConditionals struct {
    DatabaseURL string `tsk:"database_url"`
    APITimeout  int    `tsk:"api_timeout"`
}

func (c *ComplexConditionals) GetDatabaseURL() string {
    return c.DatabaseURL
}

func (c *ComplexConditionals) GetAPITimeout() time.Duration {
    return time.Duration(c.APITimeout) * time.Second
}

func (c *ComplexConditionals) IsUsingExternalDatabase() bool {
    return strings.Contains(c.DatabaseURL, "postgresql://")
}

func (c *ComplexConditionals) IsUsingLocalDatabase() bool {
    return strings.Contains(c.DatabaseURL, "sqlite://")
}
```

## 🔧 **Go Integration**

### **Conditional Configuration Loading**

```go
// Go - Dynamic configuration loading based on conditions
type ConditionalLoader struct {
    Config *AppConfig
}

func (c *ConditionalLoader) LoadConfig() error {
    // Determine environment
    env := os.Getenv("ENVIRONMENT")
    if env == "" {
        env = "development"
    }
    
    // Load appropriate config file
    configFile := fmt.Sprintf("config.%s.tsk", env)
    
    config := &AppConfig{}
    err := tusk.ParseFile(configFile, config)
    if err != nil {
        return fmt.Errorf("failed to load config: %w", err)
    }
    
    c.Config = config
    return nil
}

func (c *ConditionalLoader) ValidateConfig() error {
    if c.Config == nil {
        return errors.New("configuration not loaded")
    }
    
    // Environment-specific validation
    if c.Config.Environment == "production" {
        if c.Config.DebugMode {
            return errors.New("debug mode should be disabled in production")
        }
        
        if c.Config.LogLevel == "debug" {
            return errors.New("debug logging should be disabled in production")
        }
    }
    
    return nil
}
```

### **Conditional Feature Flags**

```go
// Go - Feature flag implementation
type FeatureFlags struct {
    BetaFeatures    bool `tsk:"beta_features"`
    Experimental    bool `tsk:"experimental"`
    DarkMode        bool `tsk:"dark_mode"`
    Analytics       bool `tsk:"analytics"`
    Notifications   bool `tsk:"notifications"`
}

func (f *FeatureFlags) IsFeatureEnabled(feature string) bool {
    switch feature {
    case "beta":
        return f.BetaFeatures
    case "experimental":
        return f.Experimental
    case "dark_mode":
        return f.DarkMode
    case "analytics":
        return f.Analytics
    case "notifications":
        return f.Notifications
    default:
        return false
    }
}

func (f *FeatureFlags) GetEnabledFeatures() []string {
    var features []string
    
    if f.BetaFeatures {
        features = append(features, "beta")
    }
    
    if f.Experimental {
        features = append(features, "experimental")
    }
    
    if f.DarkMode {
        features = append(features, "dark_mode")
    }
    
    if f.Analytics {
        features = append(features, "analytics")
    }
    
    if f.Notifications {
        features = append(features, "notifications")
    }
    
    return features
}
```

### **Conditional Validation**

```go
// Go - Conditional validation rules
type ConditionalValidation struct {
    Environment string `tsk:"environment"`
    DebugMode   bool   `tsk:"debug_mode"`
    LogLevel    string `tsk:"log_level"`
    APIKey      string `tsk:"api_key"`
    DatabaseURL string `tsk:"database_url"`
}

func (c *ConditionalValidation) Validate() error {
    var errors []error
    
    // Environment-specific validation
    if c.Environment == "production" {
        if c.DebugMode {
            errors = append(errors, errors.New("debug mode cannot be enabled in production"))
        }
        
        if c.LogLevel == "debug" {
            errors = append(errors, errors.New("debug logging cannot be enabled in production"))
        }
        
        if c.APIKey == "" {
            errors = append(errors, errors.New("API key is required in production"))
        }
        
        if !strings.Contains(c.DatabaseURL, "postgresql://") {
            errors = append(errors, errors.New("production must use PostgreSQL"))
        }
    }
    
    // Development-specific validation
    if c.Environment == "development" {
        if c.APIKey != "" {
            errors = append(errors, errors.New("API key should not be set in development"))
        }
    }
    
    if len(errors) > 0 {
        return fmt.Errorf("validation failed: %v", errors)
    }
    
    return nil
}
```

## 🚀 **Advanced Conditional Patterns**

### **Switch-Like Conditionals**

```go
// TuskLang - Switch-like conditional logic
[switch_conditionals]
environment: @env("ENVIRONMENT")

log_level: @switch(@env("ENVIRONMENT"),
    "production", "error",
    "staging", "warn",
    "development", "debug",
    "debug"
)

database_type: @switch(@env("DB_TYPE"),
    "postgresql", "postgresql://localhost:5432/app",
    "mysql", "mysql://localhost:3306/app",
    "sqlite", "sqlite://local.db",
    "sqlite://local.db"
)
```

```go
// Go - Handling switch-like conditionals
type SwitchConditionals struct {
    Environment   string `tsk:"environment"`
    LogLevel      string `tsk:"log_level"`
    DatabaseType  string `tsk:"database_type"`
}

func (s *SwitchConditionals) GetLogLevel() string {
    return s.LogLevel
}

func (s *SwitchConditionals) GetDatabaseType() string {
    return s.DatabaseType
}

func (s *SwitchConditionals) IsProduction() bool {
    return s.Environment == "production"
}

func (s *SwitchConditionals) IsStaging() bool {
    return s.Environment == "staging"
}

func (s *SwitchConditionals) IsDevelopment() bool {
    return s.Environment == "development"
}
```

### **Conditional Arrays and Maps**

```go
// TuskLang - Conditional arrays and maps
[conditional_arrays]
features: @if(@env("BETA"),
    ["feature1", "feature2", "feature3"],
    ["feature1"]
)

plugins: @if(@env("EXPERIMENTAL"),
    {
        "plugin1": "enabled",
        "plugin2": "enabled",
        "plugin3": "enabled"
    },
    {
        "plugin1": "enabled"
    }
)
```

```go
// Go - Handling conditional arrays and maps
type ConditionalArrays struct {
    Features []string            `tsk:"features"`
    Plugins  map[string]string   `tsk:"plugins"`
}

func (c *ConditionalArrays) HasFeature(feature string) bool {
    for _, f := range c.Features {
        if f == feature {
            return true
        }
    }
    return false
}

func (c *ConditionalArrays) IsPluginEnabled(plugin string) bool {
    status, exists := c.Plugins[plugin]
    return exists && status == "enabled"
}

func (c *ConditionalArrays) GetEnabledPlugins() []string {
    var enabled []string
    for plugin, status := range c.Plugins {
        if status == "enabled" {
            enabled = append(enabled, plugin)
        }
    }
    return enabled
}
```

### **Conditional Functions**

```go
// TuskLang - Conditional function execution
[conditional_functions]
process_data: @if(@env("ENABLE_PROCESSING"),
    """function process(data) { return data.map(x => x * 2); }""",
    """function process(data) { return data; }"""
)

validate_input: @if(@env("STRICT_VALIDATION"),
    """function validate(input) { return input.length > 0 && input.length < 100; }""",
    """function validate(input) { return true; }"""
)
```

```go
// Go - Handling conditional functions
type ConditionalFunctions struct {
    ProcessData   string `tsk:"process_data"`
    ValidateInput string `tsk:"validate_input"`
}

func (c *ConditionalFunctions) ExecuteProcessData(data []int) ([]int, error) {
    // Execute the conditional function
    result, err := tusk.ExecuteFunction(c.ProcessData, "process", data)
    if err != nil {
        return nil, fmt.Errorf("failed to execute process function: %w", err)
    }
    
    // Convert result to []int
    if processed, ok := result.([]int); ok {
        return processed, nil
    }
    
    return nil, errors.New("invalid result type from process function")
}

func (c *ConditionalFunctions) ExecuteValidateInput(input string) (bool, error) {
    // Execute the conditional function
    result, err := tusk.ExecuteFunction(c.ValidateInput, "validate", input)
    if err != nil {
        return false, fmt.Errorf("failed to execute validate function: %w", err)
    }
    
    // Convert result to bool
    if valid, ok := result.(bool); ok {
        return valid, nil
    }
    
    return false, errors.New("invalid result type from validate function")
}
```

## 🌍 **Environment-Based Logic**

### **Environment-Specific Configuration**

```go
// TuskLang - Environment-specific configuration
[environment_specific]
database_url: @if(@env("ENVIRONMENT") == "production",
    @env("DATABASE_URL"),
    @if(@env("ENVIRONMENT") == "staging",
        "postgresql://staging:5432/app",
        "sqlite://local.db"
    )
)

api_endpoint: @if(@env("ENVIRONMENT") == "production",
    "https://api.production.com",
    @if(@env("ENVIRONMENT") == "staging",
        "https://api.staging.com",
        "http://localhost:3000"
    )
)

log_level: @if(@env("ENVIRONMENT") == "production",
    "error",
    @if(@env("ENVIRONMENT") == "staging",
        "warn",
        "debug"
    )
)
```

```go
// Go - Environment-specific configuration handling
type EnvironmentSpecific struct {
    DatabaseURL string `tsk:"database_url"`
    APIEndpoint string `tsk:"api_endpoint"`
    LogLevel    string `tsk:"log_level"`
}

func (e *EnvironmentSpecific) GetEnvironment() string {
    env := os.Getenv("ENVIRONMENT")
    if env == "" {
        env = "development"
    }
    return env
}

func (e *EnvironmentSpecific) IsProduction() bool {
    return e.GetEnvironment() == "production"
}

func (e *EnvironmentSpecific) IsStaging() bool {
    return e.GetEnvironment() == "staging"
}

func (e *EnvironmentSpecific) IsDevelopment() bool {
    return e.GetEnvironment() == "development"
}

func (e *EnvironmentSpecific) GetDatabaseURL() string {
    return e.DatabaseURL
}

func (e *EnvironmentSpecific) GetAPIEndpoint() string {
    return e.APIEndpoint
}

func (e *EnvironmentSpecific) GetLogLevel() string {
    return e.LogLevel
}
```

### **Feature Toggle Logic**

```go
// TuskLang - Feature toggle logic
[feature_toggles]
dark_mode: @if(@env("DARK_MODE") == "true", true, false)
analytics: @if(@env("ANALYTICS") == "true", true, false)
notifications: @if(@env("NOTIFICATIONS") == "true", true, false)
beta_features: @if(@env("BETA_FEATURES") == "true", true, false)
experimental: @if(@env("EXPERIMENTAL") == "true", true, false)
```

```go
// Go - Feature toggle handling
type FeatureToggles struct {
    DarkMode       bool `tsk:"dark_mode"`
    Analytics      bool `tsk:"analytics"`
    Notifications  bool `tsk:"notifications"`
    BetaFeatures   bool `tsk:"beta_features"`
    Experimental   bool `tsk:"experimental"`
}

func (f *FeatureToggles) IsFeatureEnabled(feature string) bool {
    switch feature {
    case "dark_mode":
        return f.DarkMode
    case "analytics":
        return f.Analytics
    case "notifications":
        return f.Notifications
    case "beta_features":
        return f.BetaFeatures
    case "experimental":
        return f.Experimental
    default:
        return false
    }
}

func (f *FeatureToggles) GetEnabledFeatures() []string {
    var features []string
    
    if f.DarkMode {
        features = append(features, "dark_mode")
    }
    
    if f.Analytics {
        features = append(features, "analytics")
    }
    
    if f.Notifications {
        features = append(features, "notifications")
    }
    
    if f.BetaFeatures {
        features = append(features, "beta_features")
    }
    
    if f.Experimental {
        features = append(features, "experimental")
    }
    
    return features
}

func (f *FeatureToggles) ValidateToggles() error {
    // Validate toggle combinations
    if f.BetaFeatures && !f.Experimental {
        return errors.New("beta features require experimental mode")
    }
    
    if f.Analytics && f.Experimental {
        return errors.New("analytics should not be enabled with experimental features")
    }
    
    return nil
}
```

## ⚡ **Performance Considerations**

### **Conditional Evaluation Optimization**

```go
// Go - Optimized conditional evaluation
type OptimizedConditionals struct {
    Environment string `tsk:"environment"`
    DebugMode   bool   `tsk:"debug_mode"`
    LogLevel    string `tsk:"log_level"`
}

func (o *OptimizedConditionals) GetLogLevel() string {
    // Cache the result to avoid repeated string comparisons
    if o.LogLevel != "" {
        return o.LogLevel
    }
    
    // Fallback logic
    if o.Environment == "production" {
        return "error"
    } else if o.Environment == "staging" {
        return "warn"
    }
    return "debug"
}

func (o *OptimizedConditionals) ShouldLog(level string) bool {
    logLevels := map[string]int{
        "debug": 0,
        "info":  1,
        "warn":  2,
        "error": 3,
    }
    
    currentLevel := logLevels[o.GetLogLevel()]
    messageLevel := logLevels[level]
    
    return messageLevel >= currentLevel
}
```

### **Lazy Evaluation**

```go
// Go - Lazy evaluation of conditional logic
type LazyConditionals struct {
    Config *AppConfig
}

func (l *LazyConditionals) GetExpensiveValue() (string, error) {
    // Only compute expensive value when needed
    if l.Config.Environment == "production" {
        return l.computeProductionValue()
    } else if l.Config.Environment == "staging" {
        return l.computeStagingValue()
    }
    return l.computeDevelopmentValue()
}

func (l *LazyConditionals) computeProductionValue() (string, error) {
    // Expensive computation for production
    time.Sleep(100 * time.Millisecond) // Simulate expensive operation
    return "production_value", nil
}

func (l *LazyConditionals) computeStagingValue() (string, error) {
    // Moderate computation for staging
    time.Sleep(50 * time.Millisecond) // Simulate moderate operation
    return "staging_value", nil
}

func (l *LazyConditionals) computeDevelopmentValue() (string, error) {
    // Simple computation for development
    return "development_value", nil
}
```

## 🌍 **Real-World Examples**

### **Multi-Environment Application Configuration**

```go
// TuskLang - Multi-environment configuration
[app_config]
environment: @env("ENVIRONMENT")

database: @if(@env("ENVIRONMENT") == "production",
    {
        "host": @env("DB_HOST"),
        "port": @env("DB_PORT"),
        "name": @env("DB_NAME"),
        "user": @env("DB_USER"),
        "pass": @env("DB_PASS"),
        "ssl": true
    },
    @if(@env("ENVIRONMENT") == "staging",
        {
            "host": "staging-db.example.com",
            "port": 5432,
            "name": "app_staging",
            "user": "staging_user",
            "pass": "staging_pass",
            "ssl": true
        },
        {
            "host": "localhost",
            "port": 5432,
            "name": "app_dev",
            "user": "dev_user",
            "pass": "dev_pass",
            "ssl": false
        }
    )
)

api: @if(@env("ENVIRONMENT") == "production",
    {
        "base_url": "https://api.production.com",
        "timeout": 30,
        "retries": 3,
        "rate_limit": 1000
    },
    @if(@env("ENVIRONMENT") == "staging",
        {
            "base_url": "https://api.staging.com",
            "timeout": 60,
            "retries": 5,
            "rate_limit": 100
        },
        {
            "base_url": "http://localhost:3000",
            "timeout": 120,
            "retries": 10,
            "rate_limit": 10
        }
    )
)
```

```go
// Go - Multi-environment configuration handling
type DatabaseConfig struct {
    Host string `tsk:"host"`
    Port int    `tsk:"port"`
    Name string `tsk:"name"`
    User string `tsk:"user"`
    Pass string `tsk:"pass"`
    SSL  bool   `tsk:"ssl"`
}

type APIConfig struct {
    BaseURL   string `tsk:"base_url"`
    Timeout   int    `tsk:"timeout"`
    Retries   int    `tsk:"retries"`
    RateLimit int    `tsk:"rate_limit"`
}

type AppConfig struct {
    Environment string         `tsk:"environment"`
    Database    DatabaseConfig `tsk:"database"`
    API         APIConfig      `tsk:"api"`
}

func (a *AppConfig) GetDatabaseURL() string {
    sslMode := "disable"
    if a.Database.SSL {
        sslMode = "require"
    }
    
    return fmt.Sprintf(
        "postgresql://%s:%s@%s:%d/%s?sslmode=%s",
        a.Database.User,
        a.Database.Pass,
        a.Database.Host,
        a.Database.Port,
        a.Database.Name,
        sslMode,
    )
}

func (a *AppConfig) GetAPITimeout() time.Duration {
    return time.Duration(a.API.Timeout) * time.Second
}

func (a *AppConfig) IsProduction() bool {
    return a.Environment == "production"
}

func (a *AppConfig) IsStaging() bool {
    return a.Environment == "staging"
}

func (a *AppConfig) IsDevelopment() bool {
    return a.Environment == "development"
}
```

### **Feature Flag Management System**

```go
// TuskLang - Feature flag management
[feature_flags]
environment: @env("ENVIRONMENT")

flags: @if(@env("ENVIRONMENT") == "production",
    {
        "dark_mode": false,
        "analytics": true,
        "notifications": true,
        "beta_features": false,
        "experimental": false,
        "chat": true,
        "video": true,
        "ai_assistant": false
    },
    @if(@env("ENVIRONMENT") == "staging",
        {
            "dark_mode": true,
            "analytics": true,
            "notifications": true,
            "beta_features": true,
            "experimental": true,
            "chat": true,
            "video": true,
            "ai_assistant": true
        },
        {
            "dark_mode": true,
            "analytics": false,
            "notifications": false,
            "beta_features": true,
            "experimental": true,
            "chat": true,
            "video": false,
            "ai_assistant": true
        }
    )
)
```

```go
// Go - Feature flag management system
type FeatureFlags struct {
    Environment string            `tsk:"environment"`
    Flags       map[string]bool   `tsk:"flags"`
}

func (f *FeatureFlags) IsFeatureEnabled(feature string) bool {
    enabled, exists := f.Flags[feature]
    return exists && enabled
}

func (f *FeatureFlags) GetEnabledFeatures() []string {
    var enabled []string
    for feature, isEnabled := range f.Flags {
        if isEnabled {
            enabled = append(enabled, feature)
        }
    }
    return enabled
}

func (f *FeatureFlags) GetDisabledFeatures() []string {
    var disabled []string
    for feature, isEnabled := range f.Flags {
        if !isEnabled {
            disabled = append(disabled, feature)
        }
    }
    return disabled
}

func (f *FeatureFlags) ValidateFlags() error {
    var errors []error
    
    // Production validation
    if f.Environment == "production" {
        if f.IsFeatureEnabled("experimental") {
            errors = append(errors, errors.New("experimental features cannot be enabled in production"))
        }
        
        if f.IsFeatureEnabled("beta_features") {
            errors = append(errors, errors.New("beta features cannot be enabled in production"))
        }
        
        if !f.IsFeatureEnabled("analytics") {
            errors = append(errors, errors.New("analytics must be enabled in production"))
        }
    }
    
    // Development validation
    if f.Environment == "development" {
        if f.IsFeatureEnabled("analytics") {
            errors = append(errors, errors.New("analytics should not be enabled in development"))
        }
    }
    
    if len(errors) > 0 {
        return fmt.Errorf("feature flag validation failed: %v", errors)
    }
    
    return nil
}
```

## 🎯 **Best Practices**

### **1. Use Clear Conditional Logic**

```go
// ✅ Good - Clear conditional logic
[good_conditionals]
debug_mode: @if(@env("DEBUG") == "true", true, false)
log_level: @if(@env("ENVIRONMENT") == "production", "error", "debug")

// ❌ Bad - Unclear conditional logic
[bad_conditionals]
debug_mode: @if(@env("DEBUG"), true, false)  // What if DEBUG is "false"?
log_level: @if(@env("ENVIRONMENT"), "error", "debug")  // What if ENVIRONMENT is "staging"?
```

### **2. Provide Meaningful Defaults**

```go
// ✅ Good - Meaningful defaults
[good_defaults]
api_timeout: @if(@env("API_TIMEOUT"), @env("API_TIMEOUT"), 30)
retry_count: @if(@env("RETRY_COUNT"), @env("RETRY_COUNT"), 3)

// ❌ Bad - No defaults
[bad_defaults]
api_timeout: @env("API_TIMEOUT")  // Could be empty
retry_count: @env("RETRY_COUNT")  // Could be empty
```

### **3. Validate Conditional Results**

```go
// ✅ Good - Validate conditional results
func (c *Config) Validate() error {
    if c.Environment == "production" && c.DebugMode {
        return errors.New("debug mode cannot be enabled in production")
    }
    
    if c.Environment == "development" && c.LogLevel == "error" {
        return errors.New("development should use debug logging")
    }
    
    return nil
}

// ❌ Bad - No validation
func (c *Config) Validate() error {
    return nil // No validation of conditional logic
}
```

### **4. Use Environment-Specific Configuration Files**

```go
// ✅ Good - Environment-specific files
config.production.tsk
config.staging.tsk
config.development.tsk

// ❌ Bad - Single file with complex conditionals
config.tsk  // Too many conditionals make it hard to read
```

### **5. Document Conditional Logic**

```go
// ✅ Good - Documented conditional logic
[documented_conditionals]
# Production uses external database, others use local
database_url: @if(@env("ENVIRONMENT") == "production",
    @env("DATABASE_URL"),  # External database
    "sqlite://local.db"    # Local database
)

# Production disables debug features
debug_mode: @if(@env("ENVIRONMENT") == "production",
    false,  # Disabled in production
    true    # Enabled in other environments
)

// ❌ Bad - Undocumented conditionals
[undocumented_conditionals]
database_url: @if(@env("ENVIRONMENT") == "production", @env("DATABASE_URL"), "sqlite://local.db")
debug_mode: @if(@env("ENVIRONMENT") == "production", false, true)
```

---

**🎉 You've mastered conditional logic in TuskLang with Go!**

Conditional logic in TuskLang transforms static configuration into dynamic, responsive systems. With proper conditional handling, you can build applications that adapt to their environment and user preferences.

**Next Steps:**
- Explore [019-functions-go.md](019-functions-go.md) for executable configuration
- Master [020-imports-go.md](020-imports-go.md) for modular configuration
- Dive into [021-operators-go.md](021-operators-go.md) for advanced operators

**Remember:** In TuskLang, conditionals aren't just logic—they're the heartbeat of your configuration. Use them wisely to create responsive, adaptive systems. 