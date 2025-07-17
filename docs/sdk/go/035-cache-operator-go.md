# @cache Operator in TuskLang - Go Guide

## ⚡ **Speed Demon: @cache Operator Mastery**

TuskLang's `@cache` operator is your performance superweapon. We don't bow to any king—especially not slow, repeated operations. Here's how to use `@cache` in Go projects to turbocharge your configuration performance.

## 📋 **Table of Contents**
- [What is @cache?](#what-is-cache)
- [Basic Usage](#basic-usage)
- [TTL Configuration](#ttl-configuration)
- [Cache Strategies](#cache-strategies)
- [Go Integration](#go-integration)
- [Best Practices](#best-practices)

## 🧠 **What is @cache?**

The `@cache` operator caches expensive operations and their results. No more repeated database queries or HTTP calls—just pure, cached performance.

## 🛠️ **Basic Usage**

```go
[metrics]
user_count: @cache("5m", @query("SELECT COUNT(*) FROM users"))
api_status: @cache("1m", @http("GET", "https://api.example.com/status"))
```

## ⏱️ **TTL Configuration**

```go
[performance]
short_cache: @cache("30s", expensive_operation)
medium_cache: @cache("5m", medium_operation)
long_cache: @cache("1h", long_operation)
```

## 🎯 **Cache Strategies**

### **Database Query Caching**
```go
[stats]
user_count: @cache("5m", @query("SELECT COUNT(*) FROM users"))
order_count: @cache("2m", @query("SELECT COUNT(*) FROM orders WHERE created_at > NOW() - INTERVAL '1 day'"))
```

### **HTTP Request Caching**
```go
[external]
weather: @cache("15m", @http("GET", "https://api.weather.com/current"))
```

### **Computed Value Caching**
```go
[calculations]
total_revenue: @cache("10m", @calculate("SELECT SUM(amount) FROM transactions"))
```

## 🔗 **Go Integration**

```go
userCount := config.GetInt("user_count") // Automatically cached
apiStatus := config.GetString("api_status") // Automatically cached
```

### **Manual Cache Management**
```go
cache := tusklang.NewCache(5 * time.Minute)
cache.Set("key", "value")
val, found := cache.Get("key")
```

## 🥇 **Best Practices**
- Cache expensive operations (queries, HTTP calls)
- Use appropriate TTL for your data
- Invalidate cache when data changes
- Monitor cache hit rates

---

**TuskLang: Speed is power with @cache.** 