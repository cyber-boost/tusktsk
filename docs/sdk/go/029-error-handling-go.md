# Error Handling in TuskLang - Go Guide

## 🛑 **No Excuses: Bulletproof Error Handling**

TuskLang doesn’t bow to any king—especially not to silent failures or cryptic stack traces. This guide shows you how to handle errors in TuskLang-powered Go projects with clarity, confidence, and zero tolerance for ambiguity.

## 📋 **Table of Contents**
- [Types of Errors](#types-of-errors)
- [Config Parsing Errors](#config-parsing-errors)
- [Runtime Errors](#runtime-errors)
- [Go Integration](#go-integration)
- [Error Patterns](#error-patterns)
- [Best Practices](#best-practices)

## ⚠️ **Types of Errors**

- **Syntax Errors**: Malformed config, missing brackets, invalid keys
- **Validation Errors**: Required fields missing, type mismatches
- **Runtime Errors**: Failed queries, missing files, permission denied
- **Operator Errors**: @query, @env, @file failures

## 📝 **Config Parsing Errors**

### **TuskLang Example**

```go
// TuskLang - Syntax error
[database
url: "postgres://localhost"  # Missing closing bracket
```

```go
// Go - Handling parse errors
config, err := tusklang.LoadConfig("broken.tsk")
if err != nil {
    log.Fatalf("Config parse error: %v", err)
}
```

### **Validation Error Example**

```go
// TuskLang - Validation error
[api]
api_key: 12345  # Should be a string, not an int
```

```go
// Go - Handling validation errors
err = config.Validate()
if err != nil {
    log.Printf("Validation failed: %v", err)
}
```

## 💥 **Runtime Errors**

### **Operator Failures**

```go
// TuskLang - Operator error
[metrics]
user_count: @query("SELECT COUNT(*) FROM missing_table")  # Table does not exist
```

```go
// Go - Handling operator errors
userCount, err := config.GetInt("user_count")
if err != nil {
    log.Printf("Query failed: %v", err)
    // Fallback or alert
}
```

### **File/Env Errors**

```go
// TuskLang - File error
[ssl]
private_key: @file.read("/missing/key.pem")
```

```go
// Go - Handling file errors
key, err := config.GetString("private_key")
if err != nil {
    log.Printf("File read error: %v", err)
}
```

## 🔗 **Go Integration**

### **Idiomatic Error Handling**

```go
// Go - Idiomatic error handling
val, err := config.GetString("api_key")
if err != nil {
    log.Printf("Missing api_key: %v", err)
    // Provide default or exit
}
```

### **Custom Error Types**

```go
type ConfigError struct {
    Section string
    Key     string
    Err     error
}

func (e *ConfigError) Error() string {
    return fmt.Sprintf("[%s] %s: %v", e.Section, e.Key, e.Err)
}

// Usage
return &ConfigError{Section: "database", Key: "url", Err: err}
```

### **Error Wrapping**

```go
// Go - Error wrapping
if err != nil {
    return fmt.Errorf("failed to load config: %w", err)
}
```

## 🧩 **Error Patterns**

- Always check errors from config accessors
- Use descriptive error messages
- Wrap errors with context
- Log and alert on critical failures
- Provide fallbacks for non-fatal errors

## 🥇 **Best Practices**

- Never ignore errors—handle or escalate
- Use custom error types for clarity
- Log all config and runtime errors
- Fail fast on critical config issues
- Document error cases in your code

---

**TuskLang: No silent failures. No surprises. Just bulletproof error handling.** 