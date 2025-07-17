# @ Operator System in TuskLang - Go Guide

## ⚡️ **The Power of @: Operators Unleashed**

TuskLang’s @ operator system is pure rebellion—dynamic, extensible, and ready for anything. This guide gives Go developers a complete overview of @ operators, usage patterns, and best practices for integrating them into your Go projects.

## 📋 **Table of Contents**
- [What Are @ Operators?](#what-are--operators)
- [Core Operator Types](#core-operator-types)
- [Usage Patterns](#usage-patterns)
- [Go Integration](#go-integration)
- [Best Practices](#best-practices)

## 🤖 **What Are @ Operators?**

@ operators are TuskLang’s superpower: they inject dynamic logic, real-time data, and external integrations directly into your config. No more static files—@ operators make your config come alive.

## 🧩 **Core Operator Types**

- `@env` – Environment variables
- `@date` – Date/time operations
- `@query` – Database queries
- `@cache` – Caching
- `@metrics` – Metrics and monitoring
- `@file` – File operations
- `@http` – HTTP requests
- `@encrypt` – Encryption
- `@validate` – Validation
- `@learn` – Machine learning

## 🛠️ **Usage Patterns**

### **Environment Variables**
```go
[api]
api_key: @env("API_KEY", "default")
```

### **Database Queries**
```go
[stats]
user_count: @query("SELECT COUNT(*) FROM users")
```

### **Caching**
```go
[metrics]
user_count: @cache("5m", @query("SELECT COUNT(*) FROM users"))
```

### **Date/Time**
```go
[build]
build_time: @date.now()
```

### **HTTP Requests**
```go
[external]
status: @http("GET", "https://api.example.com/status")
```

## 🔗 **Go Integration**

### **Accessing Operator Values**
```go
val, err := config.GetString("api_key") // Handles @env
count, err := config.GetInt("user_count") // Handles @query/@cache
```

### **Custom Operator Handling**
```go
type CustomOperator struct {
    Name string
    Eval func(args ...interface{}) (interface{}, error)
}

func RegisterOperator(name string, eval func(args ...interface{}) (interface{}, error)) {
    tusklang.RegisterOperator(name, eval)
}

// Example: Register a custom operator
RegisterOperator("shout", func(args ...interface{}) (interface{}, error) {
    if len(args) == 0 { return "", nil }
    return strings.ToUpper(fmt.Sprint(args[0])), nil
})
```

## 🥇 **Best Practices**

- Use @ operators for all dynamic config needs
- Validate operator results at runtime
- Register custom operators for project-specific logic
- Document all operator usage in your config

---

**TuskLang: With @ operators, your config is alive.** 