# Imports in TuskLang - Go Guide

## 🎯 **The Power of Modular Configuration**

In TuskLang, imports aren't just about including files—they're about building modular, reusable configuration systems. We don't bow to any king, especially not monolithic configuration files. TuskLang gives you the power to compose complex configurations from simple, focused modules.

## 📋 **Table of Contents**
- [Understanding Imports in TuskLang](#understanding-imports-in-tusklang)
- [Import Syntax](#import-syntax)
- [Go Integration](#go-integration)
- [Modular Configuration Patterns](#modular-configuration-patterns)
- [Cross-File Communication](#cross-file-communication)
- [Performance Considerations](#performance-considerations)
- [Real-World Examples](#real-world-examples)
- [Best Practices](#best-practices)

## 🔍 **Understanding Imports in TuskLang**

TuskLang's import system allows you to compose configurations from multiple files:

```go
// TuskLang - Main configuration with imports
[main_config]
import: [
    "database.tsk",
    "api.tsk",
    "logging.tsk"
]

app_name: "My Application"
environment: @env("ENVIRONMENT", "development")
```

```go
// database.tsk - Database configuration module
[database]
host: @env("DB_HOST", "localhost")
port: @env("DB_PORT", 5432)
name: @env("DB_NAME", "app")
user: @env("DB_USER", "postgres")
pass: @env("DB_PASS", "")
```

```go
// Go integration
type MainConfig struct {
    AppName     string `tsk:"app_name"`
    Environment string `tsk:"environment"`
    Database    DatabaseConfig
    API         APIConfig
    Logging     LoggingConfig
}

type DatabaseConfig struct {
    Host string `tsk:"host"`
    Port int    `tsk:"port"`
    Name string `tsk:"name"`
    User string `tsk:"user"`
    Pass string `tsk:"pass"`
}
```

## 🎨 **Import Syntax**

### **Basic Import Declaration**

```go
// TuskLang - Basic import syntax
[config]
import: "database.tsk"

app_name: "My App"
```

```go
// Go - Handling basic imports
type Config struct {
    AppName  string         `tsk:"app_name"`
    Database DatabaseConfig
}

func LoadConfig() (*Config, error) {
    config := &Config{}
    err := tusk.ParseFile("config.tsk", config)
    if err != nil {
        return nil, fmt.Errorf("failed to load config: %w", err)
    }
    return config, nil
}
```

### **Multiple Imports**

```go
// TuskLang - Multiple imports
[config]
import: [
    "database.tsk",
    "api.tsk",
    "logging.tsk",
    "security.tsk"
]

app_name: "My Application"
```

```go
// Go - Handling multiple imports
type Config struct {
    AppName  string         `tsk:"app_name"`
    Database DatabaseConfig
    API      APIConfig
    Logging  LoggingConfig
    Security SecurityConfig
}

func LoadConfig() (*Config, error) {
    config := &Config{}
    err := tusk.ParseFile("config.tsk", config)
    if err != nil {
        return nil, fmt.Errorf("failed to load config: %w", err)
    }
    return config, nil
}
```

### **Conditional Imports**

```go
// TuskLang - Conditional imports
[config]
import: @if(@env("ENVIRONMENT") == "production",
    ["database.prod.tsk", "api.prod.tsk"],
    ["database.dev.tsk", "api.dev.tsk"]
)

app_name: "My Application"
```

```go
// Go - Handling conditional imports
type Config struct {
    AppName  string         `tsk:"app_name"`
    Database DatabaseConfig
    API      APIConfig
}

func LoadConfig() (*Config, error) {
    config := &Config{}
    err := tusk.ParseFile("config.tsk", config)
    if err != nil {
        return nil, fmt.Errorf("failed to load config: %w", err)
    }
    return config, nil
}
```

## 🔧 **Go Integration**

### **Import Resolution**

```go
// Go - Import resolution system
type ImportResolver struct {
    BasePath string
    Imports  map[string]interface{}
}

func NewImportResolver(basePath string) *ImportResolver {
    return &ImportResolver{
        BasePath: basePath,
        Imports:  make(map[string]interface{}),
    }
}

func (i *ImportResolver) ResolveImports(configPath string) ([]string, error) {
    // Read the main config file
    data, err := os.ReadFile(configPath)
    if err != nil {
        return nil, fmt.Errorf("failed to read config file: %w", err)
    }
    
    // Parse imports from the config
    imports, err := i.parseImports(data)
    if err != nil {
        return nil, fmt.Errorf("failed to parse imports: %w", err)
    }
    
    // Resolve relative paths
    resolved := make([]string, len(imports))
    for j, importPath := range imports {
        resolved[j] = i.resolvePath(importPath)
    }
    
    return resolved, nil
}

func (i *ImportResolver) parseImports(data []byte) ([]string, error) {
    // Parse TuskLang file to extract imports
    // This is a simplified implementation
    lines := strings.Split(string(data), "\n")
    var imports []string
    
    for _, line := range lines {
        if strings.Contains(line, "import:") {
            // Extract import paths
            importStr := strings.TrimSpace(strings.Split(line, "import:")[1])
            if strings.HasPrefix(importStr, "[") && strings.HasSuffix(importStr, "]") {
                // Array import
                importStr = strings.Trim(importStr, "[]")
                paths := strings.Split(importStr, ",")
                for _, path := range paths {
                    imports = append(imports, strings.Trim(path, " \""))
                }
            } else {
                // Single import
                imports = append(imports, strings.Trim(importStr, " \""))
            }
        }
    }
    
    return imports, nil
}

func (i *ImportResolver) resolvePath(importPath string) string {
    if filepath.IsAbs(importPath) {
        return importPath
    }
    return filepath.Join(i.BasePath, importPath)
}
```

### **Modular Configuration Loading**

```go
// Go - Modular configuration loading
type ModularLoader struct {
    Resolver *ImportResolver
    Cache    map[string]interface{}
}

func NewModularLoader(basePath string) *ModularLoader {
    return &ModularLoader{
        Resolver: NewImportResolver(basePath),
        Cache:    make(map[string]interface{}),
    }
}

func (m *ModularLoader) LoadConfig(configPath string) (map[string]interface{}, error) {
    // Check cache first
    if cached, exists := m.Cache[configPath]; exists {
        return cached.(map[string]interface{}), nil
    }
    
    // Load main config
    config, err := m.loadFile(configPath)
    if err != nil {
        return nil, fmt.Errorf("failed to load main config: %w", err)
    }
    
    // Resolve and load imports
    imports, err := m.Resolver.ResolveImports(configPath)
    if err != nil {
        return nil, fmt.Errorf("failed to resolve imports: %w", err)
    }
    
    // Load imported configurations
    for _, importPath := range imports {
        imported, err := m.LoadConfig(importPath)
        if err != nil {
            return nil, fmt.Errorf("failed to load import %s: %w", importPath, err)
        }
        
        // Merge imported config into main config
        config = m.mergeConfigs(config, imported)
    }
    
    // Cache the result
    m.Cache[configPath] = config
    
    return config, nil
}

func (m *ModularLoader) loadFile(path string) (map[string]interface{}, error) {
    data, err := os.ReadFile(path)
    if err != nil {
        return nil, fmt.Errorf("failed to read file %s: %w", path, err)
    }
    
    config := make(map[string]interface{})
    err = tusk.ParseBytes(data, &config)
    if err != nil {
        return nil, fmt.Errorf("failed to parse file %s: %w", path, err)
    }
    
    return config, nil
}

func (m *ModularLoader) mergeConfigs(main, imported map[string]interface{}) map[string]interface{} {
    result := make(map[string]interface{})
    
    // Copy main config
    for key, value := range main {
        result[key] = value
    }
    
    // Merge imported config (imported values override main values)
    for key, value := range imported {
        result[key] = value
    }
    
    return result
}
```

### **Type-Safe Import Handling**

```go
// Go - Type-safe import handling
type TypeSafeImports struct {
    Loader *ModularLoader
}

func NewTypeSafeImports(basePath string) *TypeSafeImports {
    return &TypeSafeImports{
        Loader: NewModularLoader(basePath),
    }
}

func (t *TypeSafeImports) LoadTypedConfig(configPath string, config interface{}) error {
    // Load raw configuration
    rawConfig, err := t.Loader.LoadConfig(configPath)
    if err != nil {
        return fmt.Errorf("failed to load configuration: %w", err)
    }
    
    // Convert to JSON for type-safe parsing
    jsonData, err := json.Marshal(rawConfig)
    if err != nil {
        return fmt.Errorf("failed to marshal configuration: %w", err)
    }
    
    // Parse into typed struct
    err = json.Unmarshal(jsonData, config)
    if err != nil {
        return fmt.Errorf("failed to unmarshal configuration: %w", err)
    }
    
    return nil
}

func (t *TypeSafeImports) LoadAppConfig(configPath string) (*AppConfig, error) {
    config := &AppConfig{}
    err := t.LoadTypedConfig(configPath, config)
    if err != nil {
        return nil, err
    }
    return config, nil
}
```

## 🚀 **Modular Configuration Patterns**

### **Environment-Specific Modules**

```go
// TuskLang - Environment-specific modules
// config.tsk
[main_config]
import: @if(@env("ENVIRONMENT") == "production",
    ["config.production.tsk"],
    @if(@env("ENVIRONMENT") == "staging",
        ["config.staging.tsk"],
        ["config.development.tsk"]
    )
)

app_name: "My Application"
```

```go
// config.production.tsk
[production_config]
database_host: "prod-db.example.com"
database_port: 5432
log_level: "error"
debug_mode: false
```

```go
// config.development.tsk
[development_config]
database_host: "localhost"
database_port: 5432
log_level: "debug"
debug_mode: true
```

```go
// Go - Environment-specific configuration handling
type EnvironmentConfig struct {
    AppName      string `tsk:"app_name"`
    DatabaseHost string `tsk:"database_host"`
    DatabasePort int    `tsk:"database_port"`
    LogLevel     string `tsk:"log_level"`
    DebugMode    bool   `tsk:"debug_mode"`
}

func LoadEnvironmentConfig() (*EnvironmentConfig, error) {
    config := &EnvironmentConfig{}
    err := tusk.ParseFile("config.tsk", config)
    if err != nil {
        return nil, fmt.Errorf("failed to load environment config: %w", err)
    }
    return config, nil
}
```

### **Feature-Specific Modules**

```go
// TuskLang - Feature-specific modules
// config.tsk
[main_config]
import: [
    "database.tsk",
    "api.tsk",
    "logging.tsk",
    "security.tsk"
]

app_name: "My Application"
```

```go
// database.tsk
[database]
host: @env("DB_HOST", "localhost")
port: @env("DB_PORT", 5432)
name: @env("DB_NAME", "app")
user: @env("DB_USER", "postgres")
pass: @env("DB_PASS", "")
ssl_mode: @env("DB_SSL", "disable")
```

```go
// api.tsk
[api]
base_url: @env("API_BASE_URL", "http://localhost:3000")
timeout: @env("API_TIMEOUT", 30)
retry_count: @env("API_RETRY_COUNT", 3)
rate_limit: @env("API_RATE_LIMIT", 1000)
```

```go
// Go - Feature-specific configuration handling
type FeatureConfig struct {
    AppName  string         `tsk:"app_name"`
    Database DatabaseConfig `tsk:"database"`
    API      APIConfig      `tsk:"api"`
    Logging  LoggingConfig  `tsk:"logging"`
    Security SecurityConfig `tsk:"security"`
}

type DatabaseConfig struct {
    Host    string `tsk:"host"`
    Port    int    `tsk:"port"`
    Name    string `tsk:"name"`
    User    string `tsk:"user"`
    Pass    string `tsk:"pass"`
    SSLMode string `tsk:"ssl_mode"`
}

type APIConfig struct {
    BaseURL   string `tsk:"base_url"`
    Timeout   int    `tsk:"timeout"`
    RetryCount int   `tsk:"retry_count"`
    RateLimit int    `tsk:"rate_limit"`
}

func LoadFeatureConfig() (*FeatureConfig, error) {
    config := &FeatureConfig{}
    err := tusk.ParseFile("config.tsk", config)
    if err != nil {
        return nil, fmt.Errorf("failed to load feature config: %w", err)
    }
    return config, nil
}
```

### **Layered Configuration**

```go
// TuskLang - Layered configuration
// base.tsk
[base_config]
app_name: "My Application"
version: "1.0.0"
environment: @env("ENVIRONMENT", "development")
```

```go
// config.tsk
[main_config]
import: ["base.tsk"]

# Override base configuration
environment: @env("ENVIRONMENT", "production")
debug_mode: @env("DEBUG", false)
```

```go
// Go - Layered configuration handling
type LayeredConfig struct {
    AppName     string `tsk:"app_name"`
    Version     string `tsk:"version"`
    Environment string `tsk:"environment"`
    DebugMode   bool   `tsk:"debug_mode"`
}

func LoadLayeredConfig() (*LayeredConfig, error) {
    config := &LayeredConfig{}
    err := tusk.ParseFile("config.tsk", config)
    if err != nil {
        return nil, fmt.Errorf("failed to load layered config: %w", err)
    }
    return config, nil
}
```

## 🔗 **Cross-File Communication**

### **Shared Configuration**

```go
// TuskLang - Shared configuration
// shared.tsk
[shared]
company_name: "Acme Corp"
api_version: "v1"
default_timeout: 30
```

```go
// config.tsk
[main_config]
import: ["shared.tsk"]

app_name: "My Application"
timeout: @shared.default_timeout
```

```go
// Go - Shared configuration handling
type SharedConfig struct {
    CompanyName    string `tsk:"company_name"`
    APIVersion     string `tsk:"api_version"`
    DefaultTimeout int    `tsk:"default_timeout"`
}

type MainConfig struct {
    AppName string `tsk:"app_name"`
    Timeout int    `tsk:"timeout"`
    Shared  SharedConfig
}

func LoadSharedConfig() (*MainConfig, error) {
    config := &MainConfig{}
    err := tusk.ParseFile("config.tsk", config)
    if err != nil {
        return nil, fmt.Errorf("failed to load shared config: %w", err)
    }
    return config, nil
}
```

### **Configuration Inheritance**

```go
// TuskLang - Configuration inheritance
// parent.tsk
[parent_config]
base_url: "https://api.example.com"
timeout: 30
retry_count: 3
```

```go
// child.tsk
[child_config]
import: ["parent.tsk"]

# Inherit from parent and override specific values
timeout: 60
custom_setting: "value"
```

```go
// Go - Configuration inheritance handling
type ParentConfig struct {
    BaseURL    string `tsk:"base_url"`
    Timeout    int    `tsk:"timeout"`
    RetryCount int    `tsk:"retry_count"`
}

type ChildConfig struct {
    BaseURL       string `tsk:"base_url"`
    Timeout       int    `tsk:"timeout"`
    RetryCount    int    `tsk:"retry_count"`
    CustomSetting string `tsk:"custom_setting"`
}

func LoadChildConfig() (*ChildConfig, error) {
    config := &ChildConfig{}
    err := tusk.ParseFile("child.tsk", config)
    if err != nil {
        return nil, fmt.Errorf("failed to load child config: %w", err)
    }
    return config, nil
}
```

### **Dynamic Imports**

```go
// TuskLang - Dynamic imports
[config]
import: @if(@env("FEATURES"),
    @split(@env("FEATURES"), ","),
    ["default.tsk"]
)

app_name: "My Application"
```

```go
// Go - Dynamic import handling
type DynamicConfig struct {
    AppName string `tsk:"app_name"`
    Features map[string]interface{}
}

func LoadDynamicConfig() (*DynamicConfig, error) {
    config := &DynamicConfig{}
    err := tusk.ParseFile("config.tsk", config)
    if err != nil {
        return nil, fmt.Errorf("failed to load dynamic config: %w", err)
    }
    return config, nil
}
```

## ⚡ **Performance Considerations**

### **Import Caching**

```go
// Go - Import caching system
type ImportCache struct {
    cache map[string]interface{}
    mutex sync.RWMutex
    ttl   time.Duration
}

func NewImportCache(ttl time.Duration) *ImportCache {
    return &ImportCache{
        cache: make(map[string]interface{}),
        ttl:   ttl,
    }
}

func (i *ImportCache) Get(key string) (interface{}, bool) {
    i.mutex.RLock()
    defer i.mutex.RUnlock()
    
    value, exists := i.cache[key]
    return value, exists
}

func (i *ImportCache) Set(key string, value interface{}) {
    i.mutex.Lock()
    defer i.mutex.Unlock()
    
    i.cache[key] = value
    
    // Schedule cleanup
    go func() {
        time.Sleep(i.ttl)
        i.mutex.Lock()
        delete(i.cache, key)
        i.mutex.Unlock()
    }()
}

func (i *ImportCache) Clear() {
    i.mutex.Lock()
    defer i.mutex.Unlock()
    
    i.cache = make(map[string]interface{})
}
```

### **Lazy Loading**

```go
// Go - Lazy loading of imports
type LazyImports struct {
    cache *ImportCache
    loader *ModularLoader
}

func NewLazyImports(basePath string, cacheTTL time.Duration) *LazyImports {
    return &LazyImports{
        cache:  NewImportCache(cacheTTL),
        loader: NewModularLoader(basePath),
    }
}

func (l *LazyImports) LoadConfig(configPath string) (map[string]interface{}, error) {
    // Check cache first
    if cached, exists := l.cache.Get(configPath); exists {
        return cached.(map[string]interface{}), nil
    }
    
    // Load configuration
    config, err := l.loader.LoadConfig(configPath)
    if err != nil {
        return nil, err
    }
    
    // Cache the result
    l.cache.Set(configPath, config)
    
    return config, nil
}

func (l *LazyImports) LoadTypedConfig(configPath string, config interface{}) error {
    // Load raw configuration
    rawConfig, err := l.LoadConfig(configPath)
    if err != nil {
        return err
    }
    
    // Convert to typed struct
    jsonData, err := json.Marshal(rawConfig)
    if err != nil {
        return fmt.Errorf("failed to marshal configuration: %w", err)
    }
    
    err = json.Unmarshal(jsonData, config)
    if err != nil {
        return fmt.Errorf("failed to unmarshal configuration: %w", err)
    }
    
    return nil
}
```

## 🌍 **Real-World Examples**

### **Microservices Configuration**

```go
// TuskLang - Microservices configuration
// services/config.tsk
[services_config]
import: [
    "database.tsk",
    "redis.tsk",
    "api.tsk",
    "monitoring.tsk"
]

service_name: "user-service"
service_version: "1.0.0"
```

```go
// services/database.tsk
[database]
host: @env("DB_HOST", "localhost")
port: @env("DB_PORT", 5432)
name: @env("DB_NAME", "users")
user: @env("DB_USER", "postgres")
pass: @env("DB_PASS", "")
pool_size: @env("DB_POOL_SIZE", 10)
```

```go
// services/api.tsk
[api]
port: @env("API_PORT", 8080)
host: @env("API_HOST", "0.0.0.0")
timeout: @env("API_TIMEOUT", 30)
rate_limit: @env("API_RATE_LIMIT", 1000)
```

```go
// Go - Microservices configuration handling
type ServicesConfig struct {
    ServiceName    string         `tsk:"service_name"`
    ServiceVersion string         `tsk:"service_version"`
    Database       DatabaseConfig `tsk:"database"`
    Redis          RedisConfig    `tsk:"redis"`
    API            APIConfig      `tsk:"api"`
    Monitoring     MonitoringConfig `tsk:"monitoring"`
}

type DatabaseConfig struct {
    Host     string `tsk:"host"`
    Port     int    `tsk:"port"`
    Name     string `tsk:"name"`
    User     string `tsk:"user"`
    Pass     string `tsk:"pass"`
    PoolSize int    `tsk:"pool_size"`
}

type APIConfig struct {
    Port      int `tsk:"port"`
    Host      string `tsk:"host"`
    Timeout   int `tsk:"timeout"`
    RateLimit int `tsk:"rate_limit"`
}

func LoadServicesConfig() (*ServicesConfig, error) {
    config := &ServicesConfig{}
    err := tusk.ParseFile("services/config.tsk", config)
    if err != nil {
        return nil, fmt.Errorf("failed to load services config: %w", err)
    }
    return config, nil
}
```

### **Multi-Environment Application**

```go
// TuskLang - Multi-environment application
// app/config.tsk
[app_config]
import: @if(@env("ENVIRONMENT") == "production",
    ["config.production.tsk"],
    @if(@env("ENVIRONMENT") == "staging",
        ["config.staging.tsk"],
        ["config.development.tsk"]
    )
)

app_name: "My Application"
```

```go
// app/config.production.tsk
[production_config]
database_host: "prod-db.example.com"
database_port: 5432
api_base_url: "https://api.production.com"
log_level: "error"
debug_mode: false
monitoring_enabled: true
```

```go
// app/config.development.tsk
[development_config]
database_host: "localhost"
database_port: 5432
api_base_url: "http://localhost:3000"
log_level: "debug"
debug_mode: true
monitoring_enabled: false
```

```go
// Go - Multi-environment application handling
type AppConfig struct {
    AppName            string `tsk:"app_name"`
    DatabaseHost       string `tsk:"database_host"`
    DatabasePort       int    `tsk:"database_port"`
    APIBaseURL         string `tsk:"api_base_url"`
    LogLevel           string `tsk:"log_level"`
    DebugMode          bool   `tsk:"debug_mode"`
    MonitoringEnabled  bool   `tsk:"monitoring_enabled"`
}

func LoadAppConfig() (*AppConfig, error) {
    config := &AppConfig{}
    err := tusk.ParseFile("app/config.tsk", config)
    if err != nil {
        return nil, fmt.Errorf("failed to load app config: %w", err)
    }
    return config, nil
}

func (a *AppConfig) IsProduction() bool {
    return a.LogLevel == "error" && !a.DebugMode
}

func (a *AppConfig) IsDevelopment() bool {
    return a.LogLevel == "debug" && a.DebugMode
}
```

### **Plugin System Configuration**

```go
// TuskLang - Plugin system configuration
// plugins/config.tsk
[plugins_config]
import: @if(@env("PLUGINS"),
    @split(@env("PLUGINS"), ","),
    ["default.tsk"]
)

plugin_directory: "./plugins"
auto_load: true
```

```go
// plugins/auth.tsk
[auth_plugin]
enabled: true
provider: @env("AUTH_PROVIDER", "jwt")
secret_key: @env("AUTH_SECRET", "")
token_expiry: @env("AUTH_EXPIRY", 3600)
```

```go
// plugins/cache.tsk
[cache_plugin]
enabled: true
provider: @env("CACHE_PROVIDER", "redis")
host: @env("CACHE_HOST", "localhost")
port: @env("CACHE_PORT", 6379)
ttl: @env("CACHE_TTL", 300)
```

```go
// Go - Plugin system configuration handling
type PluginsConfig struct {
    PluginDirectory string                 `tsk:"plugin_directory"`
    AutoLoad        bool                   `tsk:"auto_load"`
    Plugins         map[string]interface{} `tsk:"plugins"`
}

type AuthPlugin struct {
    Enabled     bool   `tsk:"enabled"`
    Provider    string `tsk:"provider"`
    SecretKey   string `tsk:"secret_key"`
    TokenExpiry int    `tsk:"token_expiry"`
}

type CachePlugin struct {
    Enabled bool   `tsk:"enabled"`
    Provider string `tsk:"provider"`
    Host     string `tsk:"host"`
    Port     int    `tsk:"port"`
    TTL      int    `tsk:"ttl"`
}

func LoadPluginsConfig() (*PluginsConfig, error) {
    config := &PluginsConfig{}
    err := tusk.ParseFile("plugins/config.tsk", config)
    if err != nil {
        return nil, fmt.Errorf("failed to load plugins config: %w", err)
    }
    return config, nil
}

func (p *PluginsConfig) GetEnabledPlugins() []string {
    var enabled []string
    for name, plugin := range p.Plugins {
        if pluginMap, ok := plugin.(map[string]interface{}); ok {
            if enabled, ok := pluginMap["enabled"].(bool); ok && enabled {
                enabled = append(enabled, name)
            }
        }
    }
    return enabled
}
```

## 🎯 **Best Practices**

### **1. Use Descriptive File Names**

```go
// ✅ Good - Descriptive file names
import: [
    "database.production.tsk",
    "api.v1.tsk",
    "logging.structured.tsk"
]

// ❌ Bad - Unclear file names
import: [
    "db.tsk",
    "api.tsk",
    "log.tsk"
]
```

### **2. Organize by Feature**

```go
// ✅ Good - Organized by feature
config/
├── database/
│   ├── postgresql.tsk
│   └── mysql.tsk
├── api/
│   ├── v1.tsk
│   └── v2.tsk
└── logging/
    ├── structured.tsk
    └── simple.tsk

// ❌ Bad - Flat structure
config/
├── db.tsk
├── api.tsk
└── log.tsk
```

### **3. Use Environment-Specific Imports**

```go
// ✅ Good - Environment-specific imports
[config]
import: @if(@env("ENVIRONMENT") == "production",
    ["config.production.tsk"],
    ["config.development.tsk"]
)

// ❌ Bad - Single file with conditionals
[config]
database_host: @if(@env("ENVIRONMENT") == "production", "prod-db", "localhost")
log_level: @if(@env("ENVIRONMENT") == "production", "error", "debug")
```

### **4. Validate Import Dependencies**

```go
// ✅ Good - Validate import dependencies
func (l *ModularLoader) ValidateImports(configPath string) error {
    imports, err := l.Resolver.ResolveImports(configPath)
    if err != nil {
        return fmt.Errorf("failed to resolve imports: %w", err)
    }
    
    for _, importPath := range imports {
        if _, err := os.Stat(importPath); os.IsNotExist(err) {
            return fmt.Errorf("import file not found: %s", importPath)
        }
    }
    
    return nil
}

// ❌ Bad - No validation
func (l *ModularLoader) LoadConfig(configPath string) (map[string]interface{}, error) {
    // Load without checking if imports exist
    return l.loadFile(configPath)
}
```

### **5. Cache Import Results**

```go
// ✅ Good - Cache import results
func (l *LazyImports) LoadConfig(configPath string) (map[string]interface{}, error) {
    // Check cache first
    if cached, exists := l.cache.Get(configPath); exists {
        return cached.(map[string]interface{}), nil
    }
    
    // Load and cache
    config, err := l.loader.LoadConfig(configPath)
    if err != nil {
        return nil, err
    }
    
    l.cache.Set(configPath, config)
    return config, nil
}

// ❌ Bad - No caching
func (l *ModularLoader) LoadConfig(configPath string) (map[string]interface{}, error) {
    // Always reload from disk
    return l.loadFile(configPath)
}
```

---

**🎉 You've mastered imports in TuskLang with Go!**

Imports in TuskLang transform monolithic configuration into modular, reusable systems. With proper import handling, you can build complex configurations from simple, focused modules.

**Next Steps:**
- Explore [021-operators-go.md](021-operators-go.md) for advanced operators
- Master [022-templates-go.md](022-templates-go.md) for dynamic templates
- Dive into [023-validation-go.md](023-validation-go.md) for configuration validation

**Remember:** In TuskLang, imports aren't just includes—they're the building blocks of modular configuration. Use them wisely to create maintainable, scalable systems. 