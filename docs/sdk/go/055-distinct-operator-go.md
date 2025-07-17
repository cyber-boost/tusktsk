# @distinct Operator in TuskLang - Go Guide

## 🎯 **Distinct Power: @distinct Operator Unleashed**

TuskLang's `@distinct` operator is your uniqueness rebellion. We don't bow to any king—especially not to duplicate, redundant data. Here's how to use `@distinct` in Go projects to eliminate duplicates, find unique values, and maintain data integrity.

## 📋 **Table of Contents**
- [What is @distinct?](#what-is-distinct)
- [Basic Usage](#basic-usage)
- [Distinct Strategies](#distinct-strategies)
- [Distinct Functions](#distinct-functions)
- [Go Integration](#go-integration)
- [Best Practices](#best-practices)

## 🎯 **What is @distinct?**

The `@distinct` operator removes duplicate values from a dataset. No more redundant data—just pure, unique values.

## 🛠️ **Basic Usage**

```go
[distinct_operations]
unique_users: @distinct.by(@query("SELECT * FROM users"), "email")
unique_products: @distinct.by(@query("SELECT * FROM products"), "sku")
unique_departments: @distinct.by(@query("SELECT * FROM employees"), "department")
```

## 🔧 **Distinct Strategies**

### **Single Field Distinct**
```go
[single_distinct]
unique_emails: @distinct.by(@query("SELECT * FROM users"), "email")
unique_names: @distinct.by(@query("SELECT * FROM users"), "name")
unique_ids: @distinct.by(@query("SELECT * FROM products"), "id")
```

### **Multiple Field Distinct**
```go
[multi_distinct]
unique_combinations: @distinct.by(@query("SELECT * FROM orders"), ["user_id", "product_id"])
unique_locations: @distinct.by(@query("SELECT * FROM events"), ["city", "state"])
unique_sessions: @distinct.by(@query("SELECT * FROM sessions"), ["user_id", "session_id"])
```

### **Conditional Distinct**
```go
[conditional_distinct]
active_unique_users: @distinct.by_condition(@query("SELECT * FROM users"), "email", "status = 'active'")
verified_unique_emails: @distinct.by_condition(@query("SELECT * FROM users"), "email", "verified = true")
recent_unique_orders: @distinct.by_condition(@query("SELECT * FROM orders"), "order_id", "created_at > NOW() - INTERVAL '1 day'")
```

### **Case-Insensitive Distinct**
```go
[case_insensitive_distinct]
unique_emails_ci: @distinct.by_ci(@query("SELECT * FROM users"), "email")
unique_names_ci: @distinct.by_ci(@query("SELECT * FROM users"), "name")
unique_domains_ci: @distinct.by_ci(@query("SELECT * FROM users"), "domain")
```

## 📊 **Distinct Functions**

### **Value-Based Distinct**
```go
[value_distinct]
unique_values: @distinct.values(@query("SELECT * FROM users"), "status")
unique_categories: @distinct.values(@query("SELECT * FROM products"), "category")
unique_priorities: @distinct.values(@query("SELECT * FROM tasks"), "priority")
```

### **Count Distinct**
```go
[count_distinct]
unique_user_count: @distinct.count(@query("SELECT * FROM users"), "email")
unique_product_count: @distinct.count(@query("SELECT * FROM products"), "category")
unique_order_count: @distinct.count(@query("SELECT * FROM orders"), "user_id")
```

### **Group Distinct**
```go
[group_distinct]
unique_by_department: @distinct.group(@query("SELECT * FROM employees"), "department", "name")
unique_by_category: @distinct.group(@query("SELECT * FROM products"), "category", "brand")
unique_by_status: @distinct.group(@query("SELECT * FROM orders"), "status", "user_id")
```

### **Custom Distinct**
```go
[custom_distinct]
normalized_emails: @distinct.custom(@query("SELECT * FROM users"), "email", "normalize_email")
formatted_phones: @distinct.custom(@query("SELECT * FROM contacts"), "phone", "format_phone")
canonical_names: @distinct.custom(@query("SELECT * FROM users"), "name", "canonicalize_name")
```

## 🔗 **Go Integration**

```go
// Access distinct data
uniqueUsers := config.GetArray("unique_users")
uniqueProducts := config.GetArray("unique_products")
uniqueDepartments := config.GetArray("unique_departments")

// Process distinct results
for i, user := range uniqueUsers {
    userData := user.(map[string]interface{})
    fmt.Printf("Unique user %d: %s\n", i+1, userData["email"])
}
```

### **Manual Distinct Implementation**
```go
type DataDistinct struct{}

func (d *DataDistinct) DistinctBy(data []map[string]interface{}, field string) []map[string]interface{} {
    seen := make(map[interface{}]bool)
    var result []map[string]interface{}
    
    for _, item := range data {
        if value, exists := item[field]; exists {
            if !seen[value] {
                seen[value] = true
                result = append(result, item)
            }
        }
    }
    
    return result
}

func (d *DataDistinct) DistinctByMultiple(data []map[string]interface{}, fields []string) []map[string]interface{} {
    seen := make(map[string]bool)
    var result []map[string]interface{}
    
    for _, item := range data {
        var keyParts []string
        for _, field := range fields {
            if value, exists := item[field]; exists {
                keyParts = append(keyParts, fmt.Sprint(value))
            }
        }
        key := strings.Join(keyParts, "_")
        
        if !seen[key] {
            seen[key] = true
            result = append(result, item)
        }
    }
    
    return result
}

func (d *DataDistinct) DistinctByCondition(data []map[string]interface{}, field, condition string) []map[string]interface{} {
    seen := make(map[interface{}]bool)
    var result []map[string]interface{}
    
    for _, item := range data {
        if d.evaluateCondition(item, condition) {
            if value, exists := item[field]; exists {
                if !seen[value] {
                    seen[value] = true
                    result = append(result, item)
                }
            }
        }
    }
    
    return result
}

func (d *DataDistinct) DistinctByCaseInsensitive(data []map[string]interface{}, field string) []map[string]interface{} {
    seen := make(map[string]bool)
    var result []map[string]interface{}
    
    for _, item := range data {
        if value, exists := item[field]; exists {
            if str, ok := value.(string); ok {
                lowerStr := strings.ToLower(str)
                if !seen[lowerStr] {
                    seen[lowerStr] = true
                    result = append(result, item)
                }
            }
        }
    }
    
    return result
}

func (d *DataDistinct) DistinctValues(data []map[string]interface{}, field string) []interface{} {
    seen := make(map[interface{}]bool)
    var result []interface{}
    
    for _, item := range data {
        if value, exists := item[field]; exists {
            if !seen[value] {
                seen[value] = true
                result = append(result, value)
            }
        }
    }
    
    return result
}

func (d *DataDistinct) DistinctCount(data []map[string]interface{}, field string) int {
    seen := make(map[interface{}]bool)
    
    for _, item := range data {
        if value, exists := item[field]; exists {
            seen[value] = true
        }
    }
    
    return len(seen)
}

func (d *DataDistinct) evaluateCondition(item map[string]interface{}, condition string) bool {
    // Implement condition evaluation logic
    // This is a simplified implementation
    return true
}
```

## 🥇 **Best Practices**
- Use appropriate distinct fields for your use case
- Consider performance with large datasets
- Use case-insensitive distinct for user-facing data
- Validate distinct results
- Document distinct logic clearly

---

**TuskLang: Unique data with @distinct.** 