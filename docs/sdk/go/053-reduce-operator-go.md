# @reduce Operator in TuskLang - Go Guide

## 🔄 **Reduce Power: @reduce Operator Unleashed**

TuskLang's `@reduce` operator is your aggregation rebellion. We don't bow to any king—especially not to scattered, unsummarized data. Here's how to use `@reduce` in Go projects to combine, aggregate, and distill your data into meaningful summaries.

## 📋 **Table of Contents**
- [What is @reduce?](#what-is-reduce)
- [Basic Usage](#basic-usage)
- [Reduce Strategies](#reduce-strategies)
- [Reduce Functions](#reduce-functions)
- [Go Integration](#go-integration)
- [Best Practices](#best-practices)

## 🔄 **What is @reduce?**

The `@reduce` operator combines all elements in a dataset into a single result. No more scattered data—just pure, aggregated power.

## 🛠️ **Basic Usage**

```go
[reduce_operations]
total_users: @reduce.sum(@query("SELECT * FROM users"), "id")
total_sales: @reduce.sum(@query("SELECT * FROM orders"), "amount")
average_price: @reduce.avg(@query("SELECT * FROM products"), "price")
```

## 🔧 **Reduce Strategies**

### **Numeric Aggregation**
```go
[numeric_reduce]
total_amount: @reduce.sum(@query("SELECT * FROM orders"), "amount")
average_salary: @reduce.avg(@query("SELECT * FROM employees"), "salary")
max_price: @reduce.max(@query("SELECT * FROM products"), "price")
min_age: @reduce.min(@query("SELECT * FROM users"), "age")
```

### **String Aggregation**
```go
[string_reduce]
all_names: @reduce.concat(@query("SELECT * FROM users"), "name", ", ")
all_emails: @reduce.concat(@query("SELECT * FROM users"), "email", "; ")
longest_name: @reduce.max_length(@query("SELECT * FROM users"), "name")
shortest_name: @reduce.min_length(@query("SELECT * FROM users"), "name")
```

### **Boolean Aggregation**
```go
[boolean_reduce]
all_active: @reduce.all(@query("SELECT * FROM users"), "active")
any_admin: @reduce.any(@query("SELECT * FROM users"), "is_admin")
none_deleted: @reduce.none(@query("SELECT * FROM users"), "deleted")
```

### **Custom Aggregation**
```go
[custom_reduce]
unique_departments: @reduce.unique(@query("SELECT * FROM employees"), "department")
most_common_status: @reduce.mode(@query("SELECT * FROM orders"), "status")
median_price: @reduce.median(@query("SELECT * FROM products"), "price")
```

## 📊 **Reduce Functions**

### **Sum Functions**
```go
[sum_functions]
total_revenue: @reduce.sum(@query("SELECT * FROM sales"), "revenue")
total_quantity: @reduce.sum(@query("SELECT * FROM orders"), "quantity")
total_score: @reduce.sum(@query("SELECT * FROM scores"), "score")
```

### **Average Functions**
```go
[average_functions]
avg_rating: @reduce.avg(@query("SELECT * FROM reviews"), "rating")
avg_response_time: @reduce.avg(@query("SELECT * FROM metrics"), "response_time")
avg_order_value: @reduce.avg(@query("SELECT * FROM orders"), "amount")
```

### **Min/Max Functions**
```go
[min_max_functions]
highest_score: @reduce.max(@query("SELECT * FROM scores"), "score")
lowest_price: @reduce.min(@query("SELECT * FROM products"), "price")
oldest_user: @reduce.min(@query("SELECT * FROM users"), "created_at")
newest_order: @reduce.max(@query("SELECT * FROM orders"), "created_at")
```

### **Count Functions**
```go
[count_functions]
total_records: @reduce.count(@query("SELECT * FROM users"))
active_count: @reduce.count_where(@query("SELECT * FROM users"), "status = 'active'")
unique_count: @reduce.count_unique(@query("SELECT * FROM users"), "department")
```

### **Statistical Functions**
```go
[statistical_functions]
standard_deviation: @reduce.stddev(@query("SELECT * FROM scores"), "score")
variance: @reduce.variance(@query("SELECT * FROM prices"), "price")
percentile_95: @reduce.percentile(@query("SELECT * FROM response_times"), "time", 95)
```

## 🔗 **Go Integration**

```go
// Access reduced data
totalUsers := config.GetInt("total_users")
totalSales := config.GetFloat("total_sales")
averagePrice := config.GetFloat("average_price")

// Process reduced results
fmt.Printf("Total users: %d\n", totalUsers)
fmt.Printf("Total sales: $%.2f\n", totalSales)
fmt.Printf("Average price: $%.2f\n", averagePrice)
```

### **Manual Reduce Implementation**
```go
type DataReducer struct{}

func (r *DataReducer) Sum(data []map[string]interface{}, field string) float64 {
    var sum float64
    
    for _, item := range data {
        if value, exists := item[field]; exists {
            switch v := value.(type) {
            case int:
                sum += float64(v)
            case float64:
                sum += v
            case float32:
                sum += float64(v)
            }
        }
    }
    
    return sum
}

func (r *DataReducer) Average(data []map[string]interface{}, field string) float64 {
    sum := r.Sum(data, field)
    return sum / float64(len(data))
}

func (r *DataReducer) Max(data []map[string]interface{}, field string) interface{} {
    if len(data) == 0 {
        return nil
    }
    
    var max interface{}
    var maxValue float64
    
    for _, item := range data {
        if value, exists := item[field]; exists {
            var currentValue float64
            switch v := value.(type) {
            case int:
                currentValue = float64(v)
            case float64:
                currentValue = v
            case float32:
                currentValue = float64(v)
            default:
                continue
            }
            
            if max == nil || currentValue > maxValue {
                max = value
                maxValue = currentValue
            }
        }
    }
    
    return max
}

func (r *DataReducer) Min(data []map[string]interface{}, field string) interface{} {
    if len(data) == 0 {
        return nil
    }
    
    var min interface{}
    var minValue float64
    
    for _, item := range data {
        if value, exists := item[field]; exists {
            var currentValue float64
            switch v := value.(type) {
            case int:
                currentValue = float64(v)
            case float64:
                currentValue = v
            case float32:
                currentValue = float64(v)
            default:
                continue
            }
            
            if min == nil || currentValue < minValue {
                min = value
                minValue = currentValue
            }
        }
    }
    
    return min
}

func (r *DataReducer) Count(data []map[string]interface{}) int {
    return len(data)
}

func (r *DataReducer) CountWhere(data []map[string]interface{}, condition string) int {
    count := 0
    
    for _, item := range data {
        if r.evaluateCondition(item, condition) {
            count++
        }
    }
    
    return count
}

func (r *DataReducer) Concat(data []map[string]interface{}, field, separator string) string {
    var parts []string
    
    for _, item := range data {
        if value, exists := item[field]; exists {
            parts = append(parts, fmt.Sprint(value))
        }
    }
    
    return strings.Join(parts, separator)
}

func (r *DataReducer) evaluateCondition(item map[string]interface{}, condition string) bool {
    // Implement condition evaluation logic
    // This is a simplified implementation
    return true
}
```

## 🥇 **Best Practices**
- Use appropriate reduce functions for your data types
- Consider performance with large datasets
- Validate reduce results
- Use meaningful field names for aggregation
- Document reduce logic clearly

---

**TuskLang: Aggregated data with @reduce.** 