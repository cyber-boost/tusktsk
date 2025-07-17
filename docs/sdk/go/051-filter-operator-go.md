# @filter Operator in TuskLang - Go Guide

## 🔍 **Filter Power: @filter Operator Unleashed**

TuskLang's `@filter` operator is your data selection rebellion. We don't bow to any king—especially not to irrelevant, unwanted data. Here's how to use `@filter` in Go projects to select, refine, and focus on the data that matters.

## 📋 **Table of Contents**
- [What is @filter?](#what-is-filter)
- [Basic Usage](#basic-usage)
- [Filter Conditions](#filter-conditions)
- [Filter Functions](#filter-functions)
- [Go Integration](#go-integration)
- [Best Practices](#best-practices)

## 🔍 **What is @filter?**

The `@filter` operator selects data based on specified conditions. No more irrelevant data—just pure, focused selection.

## 🛠️ **Basic Usage**

```go
[filter_operations]
active_users: @filter.by(@query("SELECT * FROM users"), "status = 'active'")
high_value_orders: @filter.by(@query("SELECT * FROM orders"), "amount > 1000")
recent_events: @filter.by(@query("SELECT * FROM events"), "created_at > NOW() - INTERVAL '1 day'")
```

## 🔧 **Filter Conditions**

### **Simple Conditions**
```go
[simple_filters]
active_only: @filter.by(@query("SELECT * FROM users"), "status = 'active'")
admin_users: @filter.by(@query("SELECT * FROM users"), "role = 'admin'")
premium_customers: @filter.by(@query("SELECT * FROM customers"), "tier = 'premium'")
```

### **Complex Conditions**
```go
[complex_filters]
active_admins: @filter.by(@query("SELECT * FROM users"), "status = 'active' AND role = 'admin'")
high_value_recent: @filter.by(@query("SELECT * FROM orders"), "amount > 1000 AND created_at > NOW() - INTERVAL '7 days'")
premium_active: @filter.by(@query("SELECT * FROM customers"), "tier = 'premium' AND last_login > NOW() - INTERVAL '30 days'")
```

### **Range Filters**
```go
[range_filters]
price_range: @filter.by(@query("SELECT * FROM products"), "price BETWEEN 10 AND 100")
date_range: @filter.by(@query("SELECT * FROM orders"), "created_at BETWEEN '2024-01-01' AND '2024-12-31'")
age_range: @filter.by(@query("SELECT * FROM users"), "age BETWEEN 18 AND 65")
```

### **Pattern Filters**
```go
[pattern_filters]
email_domain: @filter.by(@query("SELECT * FROM users"), "email LIKE '%@example.com'")
phone_format: @filter.by(@query("SELECT * FROM contacts"), "phone LIKE '+1-%'")
name_starts_with: @filter.by(@query("SELECT * FROM users"), "name LIKE 'A%'")
```

## 📊 **Filter Functions**

### **Field-Based Filtering**
```go
[field_filters]
by_status: @filter.field(@query("SELECT * FROM users"), "status", ["active", "verified"])
by_role: @filter.field(@query("SELECT * FROM users"), "role", ["admin", "manager"])
by_category: @filter.field(@query("SELECT * FROM products"), "category", ["electronics", "books"])
```

### **Value-Based Filtering**
```go
[value_filters]
non_null: @filter.not_null(@query("SELECT * FROM users"), "email")
non_empty: @filter.not_empty(@query("SELECT * FROM products"), "description")
positive_values: @filter.positive(@query("SELECT * FROM orders"), "amount")
```

### **Custom Filtering**
```go
[custom_filters]
valid_emails: @filter.custom(@query("SELECT * FROM users"), "email", "is_valid_email")
strong_passwords: @filter.custom(@query("SELECT * FROM users"), "password", "is_strong_password")
valid_phones: @filter.custom(@query("SELECT * FROM contacts"), "phone", "is_valid_phone")
```

## 🔗 **Go Integration**

```go
// Access filtered data
activeUsers := config.GetArray("active_users")
highValueOrders := config.GetArray("high_value_orders")
recentEvents := config.GetArray("recent_events")

// Process filtered results
for _, user := range activeUsers {
    userData := user.(map[string]interface{})
    fmt.Printf("Active user: %s\n", userData["name"])
}
```

### **Manual Filter Implementation**
```go
type DataFilter struct{}

func (f *DataFilter) FilterBy(data []map[string]interface{}, condition string) []map[string]interface{} {
    var result []map[string]interface{}
    
    for _, item := range data {
        if f.evaluateCondition(item, condition) {
            result = append(result, item)
        }
    }
    
    return result
}

func (f *DataFilter) FilterByField(data []map[string]interface{}, field string, values []interface{}) []map[string]interface{} {
    var result []map[string]interface{}
    
    valueSet := make(map[interface{}]bool)
    for _, value := range values {
        valueSet[value] = true
    }
    
    for _, item := range data {
        if value, exists := item[field]; exists {
            if valueSet[value] {
                result = append(result, item)
            }
        }
    }
    
    return result
}

func (f *DataFilter) FilterNotNull(data []map[string]interface{}, field string) []map[string]interface{} {
    var result []map[string]interface{}
    
    for _, item := range data {
        if value, exists := item[field]; exists && value != nil && value != "" {
            result = append(result, item)
        }
    }
    
    return result
}

func (f *DataFilter) FilterByRange(data []map[string]interface{}, field string, min, max interface{}) []map[string]interface{} {
    var result []map[string]interface{}
    
    for _, item := range data {
        if value, exists := item[field]; exists {
            if f.isInRange(value, min, max) {
                result = append(result, item)
            }
        }
    }
    
    return result
}

func (f *DataFilter) evaluateCondition(item map[string]interface{}, condition string) bool {
    // Implement condition evaluation logic
    // This is a simplified implementation
    return true
}

func (f *DataFilter) isInRange(value, min, max interface{}) bool {
    // Implement range checking logic
    // This is a simplified implementation
    return true
}
```

## 🥇 **Best Practices**
- Use clear, descriptive filter conditions
- Consider performance with large datasets
- Validate filter conditions before applying
- Use appropriate filter types for your data
- Document filter logic clearly

---

**TuskLang: Focused data with @filter.** 