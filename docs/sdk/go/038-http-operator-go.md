# @http Operator in TuskLang - Go Guide

## 🌐 **Web Power: @http Operator Unleashed**

TuskLang's `@http` operator is your internet rebellion. We don't bow to any king—not even the web. Here's how to use `@http` in Go projects to make HTTP requests, consume APIs, and fetch external data directly from your configuration.

## 📋 **Table of Contents**
- [What is @http?](#what-is-http)
- [Basic Usage](#basic-usage)
- [HTTP Methods](#http-methods)
- [Authentication](#authentication)
- [Go Integration](#go-integration)
- [Best Practices](#best-practices)

## 🌍 **What is @http?**

The `@http` operator makes HTTP requests and injects the responses directly into your config. No more external API calls in your code—just pure, config-driven web power.

## 🛠️ **Basic Usage**

```go
[external]
api_status: @http("GET", "https://api.example.com/status")
weather: @http("GET", "https://api.weather.com/current")
user_data: @http("POST", "https://api.example.com/users", {"name": "John"})
```

## 🔧 **HTTP Methods**

### **GET Requests**
```go
[api]
status: @http("GET", "https://api.example.com/health")
users: @http("GET", "https://api.example.com/users")
```

### **POST Requests**
```go
[create]
new_user: @http("POST", "https://api.example.com/users", {"name": "Alice", "email": "alice@example.com"})
```

### **PUT Requests**
```go
[update]
update_user: @http("PUT", "https://api.example.com/users/123", {"name": "Bob"})
```

### **DELETE Requests**
```go
[delete]
remove_user: @http("DELETE", "https://api.example.com/users/123")
```

## 🔐 **Authentication**

```go
[authenticated]
user_profile: @http("GET", "https://api.example.com/profile", {}, {"Authorization": "Bearer token123"})
```

## 🔗 **Go Integration**

```go
apiStatus := config.GetString("api_status")
weather := config.GetString("weather")
```

### **Manual HTTP Client**
```go
client := &http.Client{Timeout: 10 * time.Second}
resp, err := client.Get("https://api.example.com/status")
if err != nil {
    log.Fatal(err)
}
defer resp.Body.Close()

body, err := io.ReadAll(resp.Body)
if err != nil {
    log.Fatal(err)
}
```

## 🥇 **Best Practices**
- Use HTTPS for all external requests
- Set appropriate timeouts
- Handle HTTP errors gracefully
- Cache external API responses with @cache
- Validate response data

---

**TuskLang: Web power in your config with @http.** 