# @merge Operator in TuskLang - Go Guide

## 🔗 **Merge Power: @merge Operator Unleashed**

TuskLang's `@merge` operator is your data fusion rebellion. We don't bow to any king—especially not to fragmented, scattered data. Here's how to use `@merge` in Go projects to combine, inherit, and unify configuration data from multiple sources.

## 📋 **Table of Contents**
- [What is @merge?](#what-is-merge)
- [Basic Usage](#basic-usage)
- [Merge Strategies](#merge-strategies)
- [Configuration Inheritance](#configuration-inheritance)
- [Go Integration](#go-integration)
- [Best Practices](#best-practices)

## 🔗 **What is @merge?**

The `@merge` operator combines data from multiple sources into a unified configuration. No more scattered configs—just pure, unified power.

## 🛠️ **Basic Usage**

```go
[merged_config]
base: @merge("base.tsk", "override.tsk")
environment: @merge("common.tsk", "prod.tsk")
user_config: @merge("default.tsk", "user.tsk", "local.tsk")
```

## 🔧 **Merge Strategies**

### **Deep Merge**
```go
[deep_merge]
config: @merge.deep("base.tsk", "override.tsk")
settings: @merge.deep("default.tsk", "environment.tsk", "local.tsk")
```

### **Shallow Merge**
```go
[shallow_merge]
simple: @merge.shallow("config1.tsk", "config2.tsk")
basic: @merge.shallow("default.tsk", "override.tsk")
```

### **Selective Merge**
```go
[selective]
database: @merge.select("base.tsk", "override.tsk", ["host", "port", "name"])
api: @merge.select("base.tsk", "override.tsk", ["timeout", "retries"])
```

### **Conditional Merge**
```go
[conditional]
config: @merge.if(@env("ENVIRONMENT") == "production", "prod.tsk", "dev.tsk")
settings: @merge.if(@env("DEBUG") == "true", "debug.tsk", "release.tsk")
```

## 🏗️ **Configuration Inheritance**

### **Environment-Based Inheritance**
```go
[inheritance]
base_config: @merge("common.tsk", @format.template("{env}.tsk", {"env": @env("ENVIRONMENT")}))
user_config: @merge("default.tsk", @format.template("users/{user}.tsk", {"user": @env("USER")}))
```

### **Feature-Based Inheritance**
```go
[features]
config: @merge("base.tsk", @format.template("features/{feature}.tsk", {"feature": @env("FEATURE")}))
modules: @merge("core.tsk", @format.template("modules/{module}.tsk", {"module": @env("MODULE")}))
```

### **Region-Based Inheritance**
```go
[regions]
config: @merge("global.tsk", @format.template("regions/{region}.tsk", {"region": @env("REGION")}))
settings: @merge("default.tsk", @format.template("regions/{region}/settings.tsk", {"region": @env("REGION")}))
```

## 🔗 **Go Integration**

```go
// Load merged configuration
config, err := tusklang.LoadConfig("merged_config.tsk")
if err != nil {
    log.Fatal(err)
}

// Access merged values
databaseHost := config.GetString("database.host")
apiTimeout := config.GetInt("api.timeout")
debugMode := config.GetBool("debug.enabled")
```

### **Manual Merge Implementation**
```go
type ConfigMerger struct{}

func (m *ConfigMerger) Merge(configs ...map[string]interface{}) map[string]interface{} {
    result := make(map[string]interface{})
    
    for _, config := range configs {
        for key, value := range config {
            result[key] = value
        }
    }
    
    return result
}

func (m *ConfigMerger) DeepMerge(configs ...map[string]interface{}) map[string]interface{} {
    result := make(map[string]interface{})
    
    for _, config := range configs {
        for key, value := range config {
            if existing, exists := result[key]; exists {
                if existingMap, ok := existing.(map[string]interface{}); ok {
                    if valueMap, ok := value.(map[string]interface{}); ok {
                        result[key] = m.DeepMerge(existingMap, valueMap)
                        continue
                    }
                }
            }
            result[key] = value
        }
    }
    
    return result
}

func (m *ConfigMerger) SelectiveMerge(base, override map[string]interface{}, keys []string) map[string]interface{} {
    result := make(map[string]interface{})
    
    // Copy base config
    for key, value := range base {
        result[key] = value
    }
    
    // Override selected keys
    for _, key := range keys {
        if value, exists := override[key]; exists {
            result[key] = value
        }
    }
    
    return result
}
```

## 🥇 **Best Practices**
- Use clear naming conventions for merged configs
- Document merge order and precedence
- Test merged configurations thoroughly
- Use conditional merging for environment-specific configs
- Validate merged configurations

---

**TuskLang: Unified configuration with @merge.** 