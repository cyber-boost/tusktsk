# 🗺️ TuskLang Go Maps Guide

**"We don't bow to any king" - Go Edition**

Master map/object handling in TuskLang and learn how to work with key-value objects, nested maps, and advanced map operations in Go applications. This guide covers map syntax, mapping, manipulation, validation, and best practices.

## 🎯 Map Fundamentals

### Basic Map/Object Syntax

```go
// config.tsk
[objects]
user: {
    name: "John Doe"
    email: "john@example.com"
    age: 30
    active: true
}

settings: {
    theme: "dark"
    language: "en"
    timezone: "UTC"
    notifications: true
}
```

### Go Struct Mapping

```go
// main.go
package main

import (
    "fmt"
    "github.com/tusklang/go"
)

type User struct {
    Name   string `tsk:"name"`   // User name
    Email  string `tsk:"email"`  // User email
    Age    int    `tsk:"age"`    // User age
    Active bool   `tsk:"active"` // User active status
}

type Settings struct {
    Theme         string `tsk:"theme"`         // UI theme
    Language      string `tsk:"language"`      // Language
    Timezone      string `tsk:"timezone"`      // Timezone
    Notifications bool   `tsk:"notifications"` // Enable notifications
}

type ObjectConfig struct {
    User     User     `tsk:"user"`     // User object
    Settings Settings `tsk:"settings"` // Settings object
}

func main() {
    parser := tusklanggo.NewEnhancedParser()
    
    data, err := parser.ParseFile("config.tsk")
    if err != nil {
        panic(err)
    }
    
    var config ObjectConfig
    err = tusklanggo.UnmarshalTSK(data["objects"].(map[string]interface{}), &config)
    if err != nil {
        panic(err)
    }
    
    fmt.Printf("User: %+v\n", config.User)
    fmt.Printf("Settings: %+v\n", config.Settings)
}
```

## 🔧 Map Operations

### Map Manipulation

```go
// config.tsk
[manipulation]
set: @map.set({"a": 1, "b": 2}, "c", 3)
get: @map.get({"a": 1, "b": 2}, "a")
delete: @map.delete({"a": 1, "b": 2}, "b")
keys: @map.keys({"a": 1, "b": 2, "c": 3})
values: @map.values({"a": 1, "b": 2, "c": 3})
merge: @map.merge({"a": 1}, {"b": 2, "c": 3})
```

### Nested Maps

```go
// config.tsk
[nested]
user: {
    name: "Alice"
    profile: {
        email: "alice@example.com"
        age: 28
        address: {
            street: "123 Main St"
            city: "Metropolis"
            zip: "12345"
        }
    }
}
```

```go
// main.go
type Address struct {
    Street string `tsk:"street"`
    City   string `tsk:"city"`
    Zip    string `tsk:"zip"`
}

type Profile struct {
    Email   string  `tsk:"email"`
    Age     int     `tsk:"age"`
    Address Address `tsk:"address"`
}

type NestedUser struct {
    Name    string  `tsk:"name"`
    Profile Profile `tsk:"profile"`
}

type NestedConfig struct {
    User NestedUser `tsk:"user"`
}
```

## 📊 Map Validation

### Key Validation

```go
// main.go
func validateMapKeys(m map[string]interface{}, requiredKeys []string) error {
    for _, key := range requiredKeys {
        if _, ok := m[key]; !ok {
            return fmt.Errorf("missing required key: %s", key)
        }
    }
    return nil
}
```

### Value Validation

```go
// main.go
func validateMapValues(m map[string]interface{}) error {
    for k, v := range m {
        if v == nil {
            return fmt.Errorf("key '%s' has nil value", k)
        }
    }
    return nil
}
```

## 🔗 Dynamic Maps

### Environment-Based Maps

```go
// config.tsk
[environment]
settings: @env("SETTINGS_JSON", '{"theme":"dark","language":"en"}')
```

### Computed Maps

```go
// config.tsk
[computed]
user_stats: @query("SELECT username, login_count FROM users")
settings_map: @file.read("settings.json")
```

### Conditional Maps

```go
// config.tsk
[conditional]
admin_settings: @if(@env("ENVIRONMENT") == "production", {"theme":"dark"}, {"theme":"light"})
```

## 🎯 Best Practices

### 1. Map Naming

```go
// Good - Descriptive map names
[users]
user_profiles: {
    "alice": {"email": "alice@example.com", "age": 28},
    "bob": {"email": "bob@example.com", "age": 32}
}

# Bad - Vague map names
[users]
profiles: {
    "alice": {"email": "alice@example.com"},
    "bob": {"email": "bob@example.com"}
}
```

### 2. Map Validation

```go
// Good - Validate map keys and values
func validateUserProfiles(profiles map[string]interface{}) error {
    for username, profile := range profiles {
        if profile == nil {
            return fmt.Errorf("profile for user '%s' is nil", username)
        }
    }
    return nil
}

# Bad - No validation
func processProfiles(profiles map[string]interface{}) {
    // No validation
}
```

### 3. Map Defaults

```go
// Good - Provide sensible defaults
[settings]
theme: @env("THEME", "dark")
language: @env("LANGUAGE", "en")

# Bad - No defaults
[settings]
theme: @env("THEME")
language: @env("LANGUAGE")
```

### 4. Map Documentation

```go
// Good - Document map purpose
[settings]
# Application settings for user preferences
user_preferences: {
    "theme": "dark",
    "language": "en"
}

# Bad - No documentation
[settings]
user_preferences: {
    "theme": "dark",
    "language": "en"
}
```

## 📊 Complete Example

### Configuration File

```go
// config.tsk
# ========================================
# MAP CONFIGURATION
# ========================================
[users]
user_profiles: {
    "alice": {"email": "alice@example.com", "age": 28},
    "bob": {"email": "bob@example.com", "age": 32}
}

[settings]
theme: @env("THEME", "dark")
language: @env("LANGUAGE", "en")

[computed]
user_stats: @query("SELECT username, login_count FROM users")
settings_map: @file.read("settings.json")

[functions]
merge_settings: """
function mergeSettings(defaults, overrides) {
    return {...defaults, ...overrides};
}
"""

[merged]
final_settings: @fujsen(merge_settings, @settings, @computed.settings_map)
```

### Go Application

```go
// main.go
package main

import (
    "fmt"
    "log"
    "github.com/tusklang/go"
)

type UsersConfig struct {
    UserProfiles map[string]interface{} `tsk:"user_profiles"`
}

type SettingsConfig struct {
    Theme    string `tsk:"theme"`
    Language string `tsk:"language"`
}

type ComputedConfig struct {
    UserStats   map[string]interface{} `tsk:"user_stats"`
    SettingsMap map[string]interface{} `tsk:"settings_map"`
}

type MergedConfig struct {
    FinalSettings map[string]interface{} `tsk:"final_settings"`
}

type Config struct {
    Users    UsersConfig    `tsk:"users"`
    Settings SettingsConfig `tsk:"settings"`
    Computed ComputedConfig `tsk:"computed"`
    Merged   MergedConfig   `tsk:"merged"`
}

func main() {
    config, err := loadConfig("config.tsk")
    if err != nil {
        log.Fatalf("Failed to load configuration: %v", err)
    }
    fmt.Printf("User Profiles: %v\n", config.Users.UserProfiles)
    fmt.Printf("Theme: %s\n", config.Settings.Theme)
    fmt.Printf("User Stats: %v\n", config.Computed.UserStats)
    fmt.Printf("Final Settings: %v\n", config.Merged.FinalSettings)
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
```

## 📚 Summary

You've learned:

1. **Map Fundamentals** - Basic syntax and Go struct mapping
2. **Map Operations** - Manipulation, merging, and nested maps
3. **Map Validation** - Key and value validation
4. **Dynamic Maps** - Environment-based and computed maps
5. **Best Practices** - Naming, validation, defaults, and documentation
6. **Complete Examples** - Real-world map configuration management

## 🚀 Next Steps

Now that you understand map handling:

1. **Implement Validation** - Add map validation to your applications
2. **Use Map Operations** - Leverage TuskLang's map functions
3. **Create Dynamic Maps** - Build maps from queries and environment
4. **Document Maps** - Clearly document map configurations
5. **Test Map Logic** - Ensure map logic works correctly

---

**"We don't bow to any king"** - You now have the power to handle maps effectively in your TuskLang Go applications! 