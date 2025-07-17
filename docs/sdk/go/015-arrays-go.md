# 📚 TuskLang Go Arrays Guide

**"We don't bow to any king" - Go Edition**

Master array handling in TuskLang and learn how to work with lists, slices, and advanced array operations in Go applications. This guide covers array syntax, mapping, manipulation, validation, and best practices.

## 🎯 Array Fundamentals

### Basic Array Syntax

```go
// config.tsk
[arrays]
numbers: [1, 2, 3, 4, 5]
strings: ["apple", "banana", "cherry"]
mixed: [1, "two", true, null]

[features]
enabled: ["database", "caching", "logging"]
disabled: ["monitoring", "analytics"]
```

### Go Struct Mapping

```go
// main.go
package main

import (
    "fmt"
    "github.com/tusklang/go"
)

type ArrayConfig struct {
    Numbers []int         `tsk:"numbers"` // Array of integers
    Strings []string      `tsk:"strings"` // Array of strings
    Mixed   []interface{} `tsk:"mixed"`   // Mixed type array
}

type FeatureListConfig struct {
    Enabled  []string `tsk:"enabled"`  // Enabled features
    Disabled []string `tsk:"disabled"` // Disabled features
}

func main() {
    parser := tusklanggo.NewEnhancedParser()
    
    data, err := parser.ParseFile("config.tsk")
    if err != nil {
        panic(err)
    }
    
    var arrayConfig ArrayConfig
    err = tusklanggo.UnmarshalTSK(data["arrays"].(map[string]interface{}), &arrayConfig)
    if err != nil {
        panic(err)
    }
    
    var featureList FeatureListConfig
    err = tusklanggo.UnmarshalTSK(data["features"].(map[string]interface{}), &featureList)
    if err != nil {
        panic(err)
    }
    
    fmt.Printf("Numbers: %v\n", arrayConfig.Numbers)
    fmt.Printf("Strings: %v\n", arrayConfig.Strings)
    fmt.Printf("Enabled Features: %v\n", featureList.Enabled)
}
```

## 🔧 Array Operations

### Array Manipulation

```go
// config.tsk
[manipulation]
append: @array.append([1, 2, 3], 4)
prepend: @array.prepend([2, 3, 4], 1)
remove: @array.remove([1, 2, 3, 4], 2)
reverse: @array.reverse([1, 2, 3, 4])
sort: @array.sort([4, 2, 1, 3])
unique: @array.unique([1, 2, 2, 3, 4, 4, 5])
```

### Array Filtering and Mapping

```go
// config.tsk
[filtering]
even_numbers: @array.filter([1, 2, 3, 4, 5, 6], "n => n % 2 == 0")
odd_numbers: @array.filter([1, 2, 3, 4, 5, 6], "n => n % 2 != 0")

[mapping]
squared: @array.map([1, 2, 3, 4], "n => n * n")
uppercased: @array.map(["apple", "banana"], "s => s.toUpperCase()")
```

### Array Aggregation

```go
// config.tsk
[aggregation]
sum: @array.sum([1, 2, 3, 4, 5])
average: @array.avg([1, 2, 3, 4, 5])
min: @array.min([1, 2, 3, 4, 5])
max: @array.max([1, 2, 3, 4, 5])
count: @array.count([1, 2, 3, 4, 5])
```

## 📊 Array Validation

### Length Validation

```go
// main.go
type ValidatedArrayConfig struct {
    Numbers []int `tsk:"numbers" validate:"min=1,max=10"`
    Strings []string `tsk:"strings" validate:"min=1,max=5"`
}

func validateArrays(config *ValidatedArrayConfig) error {
    if len(config.Numbers) < 1 || len(config.Numbers) > 10 {
        return fmt.Errorf("numbers array must have 1-10 elements")
    }
    if len(config.Strings) < 1 || len(config.Strings) > 5 {
        return fmt.Errorf("strings array must have 1-5 elements")
    }
    return nil
}
```

### Element Validation

```go
// main.go
func validateArrayElements(numbers []int) error {
    for _, n := range numbers {
        if n < 0 {
            return fmt.Errorf("array element %d is negative", n)
        }
    }
    return nil
}
```

## 🔗 Dynamic Arrays

### Environment-Based Arrays

```go
// config.tsk
[environment]
feature_flags: @env("FEATURE_FLAGS", "[\"beta\",\"dark_mode\"]")
```

### Computed Arrays

```go
// config.tsk
[computed]
user_ids: @query("SELECT id FROM users")
active_sessions: @query("SELECT session_id FROM sessions WHERE active = true")
```

### Conditional Arrays

```go
// config.tsk
[conditional]
admin_emails: @if(@env("ENVIRONMENT") == "production", ["admin@company.com"], ["dev@company.com"]) 
```

## 🎯 Best Practices

### 1. Array Naming

```go
// Good - Descriptive array names
[users]
user_ids: [1, 2, 3, 4, 5]
usernames: ["alice", "bob", "carol"]

# Bad - Vague array names
[users]
ids: [1, 2, 3]
names: ["alice", "bob"]
```

### 2. Array Validation

```go
// Good - Validate array length and elements
func validateArray(config *ArrayConfig) error {
    if len(config.Numbers) == 0 {
        return fmt.Errorf("numbers array cannot be empty")
    }
    for _, n := range config.Numbers {
        if n < 0 {
            return fmt.Errorf("array element %d is negative", n)
        }
    }
    return nil
}

# Bad - No validation
func processArray(config *ArrayConfig) {
    // No validation
}
```

### 3. Array Defaults

```go
// Good - Provide sensible defaults
[features]
enabled: @env("ENABLED_FEATURES", "[\"database\",\"caching\"]")

# Bad - No defaults
[features]
enabled: @env("ENABLED_FEATURES")
```

### 4. Array Documentation

```go
// Good - Document array purpose
[users]
# List of user IDs for admin access
admin_user_ids: [1, 2, 3]

# Bad - No documentation
[users]
admin_user_ids: [1, 2, 3]
```

## 📊 Complete Example

### Configuration File

```go
// config.tsk
# ========================================
# ARRAY CONFIGURATION
# ========================================
[users]
user_ids: @query("SELECT id FROM users")
usernames: @query("SELECT username FROM users")
admin_user_ids: [1, 2, 3]

[features]
enabled: @env("ENABLED_FEATURES", "[\"database\",\"caching\"]")
disabled: ["monitoring", "analytics"]

[settings]
allowed_ips: ["192.168.1.1", "10.0.0.1"]
backup_days: ["Monday", "Wednesday", "Friday"]

[computed]
active_sessions: @query("SELECT session_id FROM sessions WHERE active = true")

[functions]
filter_active: """
function filterActive(users) {
    return users.filter(u => u.active);
}
"""

[filtered]
active_users: @fujsen(filter_active, @users)
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
    UserIDs      []int    `tsk:"user_ids"`
    Usernames    []string `tsk:"usernames"`
    AdminUserIDs []int    `tsk:"admin_user_ids"`
}

type FeaturesConfig struct {
    Enabled  []string `tsk:"enabled"`
    Disabled []string `tsk:"disabled"`
}

type SettingsConfig struct {
    AllowedIPs  []string `tsk:"allowed_ips"`
    BackupDays  []string `tsk:"backup_days"`
}

type ComputedConfig struct {
    ActiveSessions []string `tsk:"active_sessions"`
}

type FilteredConfig struct {
    ActiveUsers []interface{} `tsk:"active_users"`
}

type Config struct {
    Users    UsersConfig    `tsk:"users"`
    Features FeaturesConfig `tsk:"features"`
    Settings SettingsConfig `tsk:"settings"`
    Computed ComputedConfig `tsk:"computed"`
    Filtered FilteredConfig `tsk:"filtered"`
}

func main() {
    config, err := loadConfig("config.tsk")
    if err != nil {
        log.Fatalf("Failed to load configuration: %v", err)
    }
    fmt.Printf("User IDs: %v\n", config.Users.UserIDs)
    fmt.Printf("Enabled Features: %v\n", config.Features.Enabled)
    fmt.Printf("Allowed IPs: %v\n", config.Settings.AllowedIPs)
    fmt.Printf("Active Sessions: %v\n", config.Computed.ActiveSessions)
    fmt.Printf("Active Users: %v\n", config.Filtered.ActiveUsers)
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

1. **Array Fundamentals** - Basic syntax and Go struct mapping
2. **Array Operations** - Manipulation, filtering, mapping, and aggregation
3. **Array Validation** - Length and element validation
4. **Dynamic Arrays** - Environment-based and computed arrays
5. **Best Practices** - Naming, validation, defaults, and documentation
6. **Complete Examples** - Real-world array configuration management

## 🚀 Next Steps

Now that you understand array handling:

1. **Implement Validation** - Add array validation to your applications
2. **Use Array Operations** - Leverage TuskLang's array functions
3. **Create Dynamic Arrays** - Build arrays from queries and environment
4. **Document Arrays** - Clearly document array configurations
5. **Test Array Logic** - Ensure array logic works correctly

---

**"We don't bow to any king"** - You now have the power to handle arrays effectively in your TuskLang Go applications! 