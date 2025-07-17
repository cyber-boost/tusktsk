# @group Operator in TuskLang - Go Guide

## 📊 **Group Power: @group Operator Unleashed**

TuskLang's `@group` operator is your aggregation rebellion. We don't bow to any king—especially not to scattered, unorganized data. Here's how to use `@group` in Go projects to organize, aggregate, and analyze your data effectively.

## 📋 **Table of Contents**
- [What is @group?](#what-is-group)
- [Basic Usage](#basic-usage)
- [Grouping Strategies](#grouping-strategies)
- [Aggregation Functions](#aggregation-functions)
- [Go Integration](#go-integration)
- [Best Practices](#best-practices)

## 📊 **What is @group?**

The `@group` operator organizes data into groups and applies aggregation functions. No more scattered data—just pure, organized analysis.

## 🛠️ **Basic Usage**

```go
[group_operations]
users_by_department: @group.by(@query("SELECT * FROM users"), "department")
orders_by_status: @group.by(@query("SELECT * FROM orders"), "status")
sales_by_month: @group.by(@query("SELECT * FROM sales"), "month")
```

## 🔧 **Grouping Strategies**

### **Single Field Grouping**
```go
[single_grouping]
by_department: @group.by(@query("SELECT * FROM employees"), "department")
by_status: @group.by(@query("SELECT * FROM orders"), "status")
by_region: @group.by(@query("SELECT * FROM customers"), "region")
```

### **Multiple Field Grouping**
```go
[multi_grouping]
by_dept_and_role: @group.by(@query("SELECT * FROM employees"), ["department", "role"])
by_status_and_date: @group.by(@query("SELECT * FROM orders"), ["status", "date"])
by_region_and_type: @group.by(@query("SELECT * FROM customers"), ["region", "type"])
```

### **Conditional Grouping**
```go
[conditional_grouping]
active_by_dept: @group.by_condition(@query("SELECT * FROM users"), "department", "status = 'active'")
high_value_by_status: @group.by_condition(@query("SELECT * FROM orders"), "status", "amount > 1000")
recent_by_category: @group.by_condition(@query("SELECT * FROM events"), "category", "created_at > NOW() - INTERVAL '1 day'")
```

## 📈 **Aggregation Functions**

### **Count Aggregations**
```go
[counts]
user_count_by_dept: @group.count(@query("SELECT * FROM users"), "department")
order_count_by_status: @group.count(@query("SELECT * FROM orders"), "status")
product_count_by_category: @group.count(@query("SELECT * FROM products"), "category")
```

### **Sum Aggregations**
```go
[sums]
total_sales_by_dept: @group.sum(@query("SELECT * FROM sales"), "department", "amount")
total_orders_by_status: @group.sum(@query("SELECT * FROM orders"), "status", "quantity")
total_revenue_by_month: @group.sum(@query("SELECT * FROM revenue"), "month", "amount")
```

### **Average Aggregations**
```go
[averages]
avg_salary_by_dept: @group.avg(@query("SELECT * FROM employees"), "department", "salary")
avg_order_value: @group.avg(@query("SELECT * FROM orders"), "status", "amount")
avg_rating_by_product: @group.avg(@query("SELECT * FROM reviews"), "product_id", "rating")
```

### **Min/Max Aggregations**
```go
[min_max]
min_salary_by_dept: @group.min(@query("SELECT * FROM employees"), "department", "salary")
max_order_by_status: @group.max(@query("SELECT * FROM orders"), "status", "amount")
min_price_by_category: @group.min(@query("SELECT * FROM products"), "category", "price")
max_rating_by_product: @group.max(@query("SELECT * FROM reviews"), "product_id", "rating")
```

### **Custom Aggregations**
```go
[custom]
unique_users_by_dept: @group.unique(@query("SELECT * FROM users"), "department", "user_id")
top_products_by_category: @group.top(@query("SELECT * FROM products"), "category", "sales", 5)
bottom_products_by_category: @group.bottom(@query("SELECT * FROM products"), "category", "sales", 5)
```

## 🔗 **Go Integration**

```go
// Access grouped data
usersByDept := config.GetObject("users_by_department")
ordersByStatus := config.GetObject("orders_by_status")
salesByMonth := config.GetObject("sales_by_month")

// Process grouped results
for dept, users := range usersByDept {
    fmt.Printf("Department %s: %d users\n", dept, len(users.([]interface{})))
}
```

### **Manual Group Implementation**
```go
type DataGrouper struct{}

func (g *DataGrouper) GroupBy(data []map[string]interface{}, field string) map[string][]map[string]interface{} {
    result := make(map[string][]map[string]interface{})
    
    for _, item := range data {
        if value, exists := item[field]; exists {
            key := fmt.Sprint(value)
            result[key] = append(result[key], item)
        }
    }
    
    return result
}

func (g *DataGrouper) GroupByMultiple(data []map[string]interface{}, fields []string) map[string][]map[string]interface{} {
    result := make(map[string][]map[string]interface{})
    
    for _, item := range data {
        var keyParts []string
        for _, field := range fields {
            if value, exists := item[field]; exists {
                keyParts = append(keyParts, fmt.Sprint(value))
            }
        }
        key := strings.Join(keyParts, "_")
        result[key] = append(result[key], item)
    }
    
    return result
}

func (g *DataGrouper) Count(data []map[string]interface{}, groupField string) map[string]int {
    result := make(map[string]int)
    
    for _, item := range data {
        if value, exists := item[groupField]; exists {
            key := fmt.Sprint(value)
            result[key]++
        }
    }
    
    return result
}

func (g *DataGrouper) Sum(data []map[string]interface{}, groupField, sumField string) map[string]float64 {
    result := make(map[string]float64)
    
    for _, item := range data {
        if groupValue, exists := item[groupField]; exists {
            if sumValue, exists := item[sumField]; exists {
                key := fmt.Sprint(groupValue)
                if num, ok := sumValue.(float64); ok {
                    result[key] += num
                }
            }
        }
    }
    
    return result
}
```

## 🥇 **Best Practices**
- Use meaningful group names
- Consider performance with large datasets
- Validate group field existence
- Use appropriate aggregation functions
- Document grouping logic clearly

---

**TuskLang: Organized analysis with @group.** 