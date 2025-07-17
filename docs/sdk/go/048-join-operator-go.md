# @join Operator in TuskLang - Go Guide

## 🔗 **Join Power: @join Operator Unleashed**

TuskLang's `@join` operator is your relationship rebellion. We don't bow to any king—especially not to disconnected, isolated data. Here's how to use `@join` in Go projects to connect, relate, and unify data from multiple sources.

## 📋 **Table of Contents**
- [What is @join?](#what-is-join)
- [Basic Usage](#basic-usage)
- [Join Types](#join-types)
- [Relationship Mapping](#relationship-mapping)
- [Go Integration](#go-integration)
- [Best Practices](#best-practices)

## 🔗 **What is @join?**

The `@join` operator connects data from multiple sources based on relationships. No more isolated datasets—just pure, connected power.

## 🛠️ **Basic Usage**

```go
[join_operations]
user_orders: @join.inner("users", "orders", "user_id")
user_profiles: @join.left("users", "profiles", "user_id")
product_categories: @join.right("products", "categories", "category_id")
```

## 🔧 **Join Types**

### **Inner Join**
```go
[inner_joins]
user_orders: @join.inner("users", "orders", "user_id")
product_reviews: @join.inner("products", "reviews", "product_id")
customer_orders: @join.inner("customers", "orders", "customer_id")
```

### **Left Join**
```go
[left_joins]
users_profiles: @join.left("users", "profiles", "user_id")
products_categories: @join.left("products", "categories", "category_id")
orders_shipping: @join.left("orders", "shipping", "order_id")
```

### **Right Join**
```go
[right_joins]
categories_products: @join.right("categories", "products", "category_id")
profiles_users: @join.right("profiles", "users", "user_id")
shipping_orders: @join.right("shipping", "orders", "order_id")
```

### **Full Join**
```go
[full_joins]
all_data: @join.full("table1", "table2", "id")
complete_info: @join.full("users", "profiles", "user_id")
```

## 🗺️ **Relationship Mapping**

### **One-to-One Relationships**
```go
[one_to_one]
user_profile: @join.inner("users", "profiles", "user_id")
product_details: @join.inner("products", "details", "product_id")
```

### **One-to-Many Relationships**
```go
[one_to_many]
user_orders: @join.inner("users", "orders", "user_id")
category_products: @join.inner("categories", "products", "category_id")
```

### **Many-to-Many Relationships**
```go
[many_to_many]
user_roles: @join.inner("users", "user_roles", "user_id")
@join.inner("user_roles", "roles", "role_id")
product_tags: @join.inner("products", "product_tags", "product_id")
@join.inner("product_tags", "tags", "tag_id")
```

### **Complex Joins**
```go
[complex_joins]
user_order_details: @join.inner("users", "orders", "user_id")
@join.inner("orders", "order_items", "order_id")
@join.inner("order_items", "products", "product_id")
```

## 🔗 **Go Integration**

```go
// Access joined data
userOrders := config.GetArray("user_orders")
userProfiles := config.GetArray("user_profiles")
productCategories := config.GetArray("product_categories")

// Process joined results
for _, order := range userOrders {
    user := order.(map[string]interface{})
    fmt.Printf("User %s has order %s\n", user["name"], user["order_id"])
}
```

### **Manual Join Implementation**
```go
type DataJoiner struct{}

func (j *DataJoiner) InnerJoin(left, right []map[string]interface{}, key string) []map[string]interface{} {
    var result []map[string]interface{}
    
    // Create index for right table
    rightIndex := make(map[interface{}]map[string]interface{})
    for _, item := range right {
        if value, exists := item[key]; exists {
            rightIndex[value] = item
        }
    }
    
    // Join with left table
    for _, leftItem := range left {
        if leftValue, exists := leftItem[key]; exists {
            if rightItem, found := rightIndex[leftValue]; found {
                joined := make(map[string]interface{})
                
                // Copy left item
                for k, v := range leftItem {
                    joined[k] = v
                }
                
                // Copy right item (with prefix to avoid conflicts)
                for k, v := range rightItem {
                    joined["right_"+k] = v
                }
                
                result = append(result, joined)
            }
        }
    }
    
    return result
}

func (j *DataJoiner) LeftJoin(left, right []map[string]interface{}, key string) []map[string]interface{} {
    var result []map[string]interface{}
    
    // Create index for right table
    rightIndex := make(map[interface{}]map[string]interface{})
    for _, item := range right {
        if value, exists := item[key]; exists {
            rightIndex[value] = item
        }
    }
    
    // Join with left table
    for _, leftItem := range left {
        joined := make(map[string]interface{})
        
        // Copy left item
        for k, v := range leftItem {
            joined[k] = v
        }
        
        // Try to find matching right item
        if leftValue, exists := leftItem[key]; exists {
            if rightItem, found := rightIndex[leftValue]; found {
                for k, v := range rightItem {
                    joined["right_"+k] = v
                }
            }
        }
        
        result = append(result, joined)
    }
    
    return result
}
```

## 🥇 **Best Practices**
- Use appropriate join types for your use case
- Index join keys for performance
- Handle null values in outer joins
- Validate join relationships
- Consider join performance with large datasets

---

**TuskLang: Connected data with @join.** 