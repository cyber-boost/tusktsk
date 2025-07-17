# @sort Operator in TuskLang - Go Guide

## 📊 **Sort Power: @sort Operator Unleashed**

TuskLang's `@sort` operator is your ordering rebellion. We don't bow to any king—especially not to chaotic, unordered data. Here's how to use `@sort` in Go projects to organize, order, and present your data in meaningful sequences.

## 📋 **Table of Contents**
- [What is @sort?](#what-is-sort)
- [Basic Usage](#basic-usage)
- [Sort Strategies](#sort-strategies)
- [Sorting Functions](#sorting-functions)
- [Go Integration](#go-integration)
- [Best Practices](#best-practices)

## 📊 **What is @sort?**

The `@sort` operator orders data according to specified criteria. No more random data—just pure, organized sequences.

## 🛠️ **Basic Usage**

```go
[sort_operations]
users_by_name: @sort.by(@query("SELECT * FROM users"), "name")
orders_by_date: @sort.by(@query("SELECT * FROM orders"), "created_at")
products_by_price: @sort.by(@query("SELECT * FROM products"), "price")
```

## 🔧 **Sort Strategies**

### **Single Field Sorting**
```go
[single_sorting]
by_name: @sort.by(@query("SELECT * FROM users"), "name")
by_date: @sort.by(@query("SELECT * FROM orders"), "created_at")
by_price: @sort.by(@query("SELECT * FROM products"), "price")
```

### **Multiple Field Sorting**
```go
[multi_sorting]
by_dept_then_name: @sort.by(@query("SELECT * FROM employees"), ["department", "name"])
by_status_then_date: @sort.by(@query("SELECT * FROM orders"), ["status", "created_at"])
by_category_then_price: @sort.by(@query("SELECT * FROM products"), ["category", "price"])
```

### **Reverse Sorting**
```go
[reverse_sorting]
newest_first: @sort.by_desc(@query("SELECT * FROM orders"), "created_at")
highest_price: @sort.by_desc(@query("SELECT * FROM products"), "price")
latest_users: @sort.by_desc(@query("SELECT * FROM users"), "created_at")
```

### **Custom Sorting**
```go
[custom_sorting]
by_priority: @sort.custom(@query("SELECT * FROM tasks"), "priority", ["high", "medium", "low"])
by_status_order: @sort.custom(@query("SELECT * FROM orders"), "status", ["pending", "processing", "shipped", "delivered"])
by_importance: @sort.custom(@query("SELECT * FROM notifications"), "importance", ["critical", "high", "normal", "low"])
```

## 📈 **Sorting Functions**

### **Numeric Sorting**
```go
[numeric_sorting]
by_id: @sort.numeric(@query("SELECT * FROM users"), "id")
by_age: @sort.numeric(@query("SELECT * FROM users"), "age")
by_salary: @sort.numeric(@query("SELECT * FROM employees"), "salary")
```

### **String Sorting**
```go
[string_sorting]
by_name: @sort.string(@query("SELECT * FROM users"), "name")
by_email: @sort.string(@query("SELECT * FROM users"), "email")
by_title: @sort.string(@query("SELECT * FROM products"), "title")
```

### **Date Sorting**
```go
[date_sorting]
by_created: @sort.date(@query("SELECT * FROM orders"), "created_at")
by_updated: @sort.date(@query("SELECT * FROM products"), "updated_at")
by_birthday: @sort.date(@query("SELECT * FROM users"), "birth_date")
```

### **Case-Insensitive Sorting**
```go
[case_insensitive]
by_name_ci: @sort.string_ci(@query("SELECT * FROM users"), "name")
by_email_ci: @sort.string_ci(@query("SELECT * FROM users"), "email")
by_title_ci: @sort.string_ci(@query("SELECT * FROM products"), "title")
```

## 🔗 **Go Integration**

```go
// Access sorted data
usersByName := config.GetArray("users_by_name")
ordersByDate := config.GetArray("orders_by_date")
productsByPrice := config.GetArray("products_by_price")

// Process sorted results
for i, user := range usersByName {
    userData := user.(map[string]interface{})
    fmt.Printf("%d. %s\n", i+1, userData["name"])
}
```

### **Manual Sort Implementation**
```go
type DataSorter struct{}

func (s *DataSorter) SortBy(data []map[string]interface{}, field string) []map[string]interface{} {
    result := make([]map[string]interface{}, len(data))
    copy(result, data)
    
    sort.Slice(result, func(i, j int) bool {
        valI := result[i][field]
        valJ := result[j][field]
        
        switch vI := valI.(type) {
        case string:
            if vJ, ok := valJ.(string); ok {
                return vI < vJ
            }
        case int:
            if vJ, ok := valJ.(int); ok {
                return vI < vJ
            }
        case float64:
            if vJ, ok := valJ.(float64); ok {
                return vI < vJ
            }
        }
        
        return fmt.Sprint(valI) < fmt.Sprint(valJ)
    })
    
    return result
}

func (s *DataSorter) SortByMultiple(data []map[string]interface{}, fields []string) []map[string]interface{} {
    result := make([]map[string]interface{}, len(data))
    copy(result, data)
    
    sort.Slice(result, func(i, j int) bool {
        for _, field := range fields {
            valI := result[i][field]
            valJ := result[j][field]
            
            switch vI := valI.(type) {
            case string:
                if vJ, ok := valJ.(string); ok {
                    if vI != vJ {
                        return vI < vJ
                    }
                }
            case int:
                if vJ, ok := valJ.(int); ok {
                    if vI != vJ {
                        return vI < vJ
                    }
                }
            case float64:
                if vJ, ok := valJ.(float64); ok {
                    if vI != vJ {
                        return vI < vJ
                    }
                }
            }
        }
        return false
    })
    
    return result
}

func (s *DataSorter) SortByDesc(data []map[string]interface{}, field string) []map[string]interface{} {
    result := make([]map[string]interface{}, len(data))
    copy(result, data)
    
    sort.Slice(result, func(i, j int) bool {
        valI := result[i][field]
        valJ := result[j][field]
        
        switch vI := valI.(type) {
        case string:
            if vJ, ok := valJ.(string); ok {
                return vI > vJ
            }
        case int:
            if vJ, ok := valJ.(int); ok {
                return vI > vJ
            }
        case float64:
            if vJ, ok := valJ.(float64); ok {
                return vI > vJ
            }
        }
        
        return fmt.Sprint(valI) > fmt.Sprint(valJ)
    })
    
    return result
}

func (s *DataSorter) CustomSort(data []map[string]interface{}, field string, order []interface{}) []map[string]interface{} {
    result := make([]map[string]interface{}, len(data))
    copy(result, data)
    
    // Create order map for quick lookup
    orderMap := make(map[interface{}]int)
    for i, item := range order {
        orderMap[item] = i
    }
    
    sort.Slice(result, func(i, j int) bool {
        valI := result[i][field]
        valJ := result[j][field]
        
        posI, existsI := orderMap[valI]
        posJ, existsJ := orderMap[valJ]
        
        if !existsI && !existsJ {
            return fmt.Sprint(valI) < fmt.Sprint(valJ)
        }
        if !existsI {
            return false
        }
        if !existsJ {
            return true
        }
        
        return posI < posJ
    })
    
    return result
}
```

## 🥇 **Best Practices**
- Use appropriate sort fields for your use case
- Consider performance with large datasets
- Use case-insensitive sorting for user-facing data
- Implement custom sorting for business logic
- Document sort order clearly

---

**TuskLang: Ordered data with @sort.** 