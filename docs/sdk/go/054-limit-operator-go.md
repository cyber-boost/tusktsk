# @limit Operator in TuskLang - Go Guide

## 📏 **Limit Power: @limit Operator Unleashed**

TuskLang's `@limit` operator is your data control rebellion. We don't bow to any king—especially not to overwhelming, unlimited data. Here's how to use `@limit` in Go projects to control, paginate, and manage your data output.

## 📋 **Table of Contents**
- [What is @limit?](#what-is-limit)
- [Basic Usage](#basic-usage)
- [Limit Strategies](#limit-strategies)
- [Pagination](#pagination)
- [Go Integration](#go-integration)
- [Best Practices](#best-practices)

## 📏 **What is @limit?**

The `@limit` operator restricts the number of results returned from a dataset. No more data overload—just pure, controlled output.

## 🛠️ **Basic Usage**

```go
[limit_operations]
recent_users: @limit.by(@query("SELECT * FROM users ORDER BY created_at DESC"), 10)
top_products: @limit.by(@query("SELECT * FROM products ORDER BY sales DESC"), 5)
latest_orders: @limit.by(@query("SELECT * FROM orders ORDER BY created_at DESC"), 20)
```

## 🔧 **Limit Strategies**

### **Simple Limiting**
```go
[simple_limits]
first_10: @limit.by(@query("SELECT * FROM users"), 10)
first_5: @limit.by(@query("SELECT * FROM products"), 5)
first_100: @limit.by(@query("SELECT * FROM orders"), 100)
```

### **Offset Limiting**
```go
[offset_limits]
page_1: @limit.offset(@query("SELECT * FROM users"), 0, 10)
page_2: @limit.offset(@query("SELECT * FROM users"), 10, 10)
page_3: @limit.offset(@query("SELECT * FROM users"), 20, 10)
```

### **Conditional Limiting**
```go
[conditional_limits]
active_users: @limit.by(@query("SELECT * FROM users WHERE status = 'active'"), 50)
high_value_orders: @limit.by(@query("SELECT * FROM orders WHERE amount > 1000"), 25)
recent_events: @limit.by(@query("SELECT * FROM events WHERE created_at > NOW() - INTERVAL '1 day'"), 100)
```

### **Dynamic Limiting**
```go
[dynamic_limits]
configurable_limit: @limit.by(@query("SELECT * FROM users"), @env("USER_LIMIT", 10))
environment_based: @limit.by(@query("SELECT * FROM logs"), @env("LOG_LIMIT", 1000))
```

## 📄 **Pagination**

### **Page-Based Pagination**
```go
[page_pagination]
page_1: @limit.page(@query("SELECT * FROM users"), 1, 10)
page_2: @limit.page(@query("SELECT * FROM users"), 2, 10)
page_3: @limit.page(@query("SELECT * FROM users"), 3, 10)
```

### **Cursor-Based Pagination**
```go
[cursor_pagination]
after_id_100: @limit.after(@query("SELECT * FROM users"), "id", 100, 10)
before_id_200: @limit.before(@query("SELECT * FROM users"), "id", 200, 10)
```

### **Time-Based Pagination**
```go
[time_pagination]
after_timestamp: @limit.after_time(@query("SELECT * FROM events"), "created_at", "2024-01-01", 50)
before_timestamp: @limit.before_time(@query("SELECT * FROM events"), "created_at", "2024-12-31", 50)
```

## 🔗 **Go Integration**

```go
// Access limited data
recentUsers := config.GetArray("recent_users")
topProducts := config.GetArray("top_products")
latestOrders := config.GetArray("latest_orders")

// Process limited results
for i, user := range recentUsers {
    userData := user.(map[string]interface{})
    fmt.Printf("User %d: %s\n", i+1, userData["name"])
}
```

### **Manual Limit Implementation**
```go
type DataLimiter struct{}

func (l *DataLimiter) LimitBy(data []map[string]interface{}, limit int) []map[string]interface{} {
    if limit <= 0 || limit >= len(data) {
        return data
    }
    
    return data[:limit]
}

func (l *DataLimiter) LimitOffset(data []map[string]interface{}, offset, limit int) []map[string]interface{} {
    if offset >= len(data) {
        return []map[string]interface{}{}
    }
    
    end := offset + limit
    if end > len(data) {
        end = len(data)
    }
    
    return data[offset:end]
}

func (l *DataLimiter) LimitPage(data []map[string]interface{}, page, pageSize int) []map[string]interface{} {
    if page <= 0 || pageSize <= 0 {
        return []map[string]interface{}{}
    }
    
    offset := (page - 1) * pageSize
    return l.LimitOffset(data, offset, pageSize)
}

func (l *DataLimiter) LimitAfter(data []map[string]interface{}, field string, value interface{}, limit int) []map[string]interface{} {
    var result []map[string]interface{}
    found := false
    
    for _, item := range data {
        if !found {
            if itemValue, exists := item[field]; exists && l.compareValues(itemValue, value) > 0 {
                found = true
            }
        }
        
        if found {
            result = append(result, item)
            if len(result) >= limit {
                break
            }
        }
    }
    
    return result
}

func (l *DataLimiter) LimitBefore(data []map[string]interface{}, field string, value interface{}, limit int) []map[string]interface{} {
    var result []map[string]interface{}
    
    for _, item := range data {
        if itemValue, exists := item[field]; exists && l.compareValues(itemValue, value) < 0 {
            result = append(result, item)
            if len(result) >= limit {
                break
            }
        }
    }
    
    return result
}

func (l *DataLimiter) compareValues(a, b interface{}) int {
    // Implement value comparison logic
    // This is a simplified implementation
    return 0
}
```

## 🥇 **Best Practices**
- Use appropriate limit sizes for your use case
- Implement pagination for large datasets
- Consider performance with offset-based pagination
- Use cursor-based pagination for real-time data
- Document limit logic clearly

---

**TuskLang: Controlled data with @limit.** 