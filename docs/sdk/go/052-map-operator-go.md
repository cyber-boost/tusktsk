# @map Operator in TuskLang - Go Guide

## 🗺️ **Map Power: @map Operator Unleashed**

TuskLang's `@map` operator is your transformation rebellion. We don't bow to any king—especially not to raw, unprocessed data. Here's how to use `@map` in Go projects to transform, convert, and reshape your data into exactly what you need.

## 📋 **Table of Contents**
- [What is @map?](#what-is-map)
- [Basic Usage](#basic-usage)
- [Mapping Strategies](#mapping-strategies)
- [Mapping Functions](#mapping-functions)
- [Go Integration](#go-integration)
- [Best Practices](#best-practices)

## 🗺️ **What is @map?**

The `@map` operator transforms each element in a dataset according to specified rules. No more raw data—just pure, transformed power.

## 🛠️ **Basic Usage**

```go
[map_operations]
user_names: @map.by(@query("SELECT * FROM users"), "name")
user_emails: @map.by(@query("SELECT * FROM users"), "email")
product_prices: @map.by(@query("SELECT * FROM products"), "price")
```

## 🔧 **Mapping Strategies**

### **Field Mapping**
```go
[field_mapping]
names_only: @map.field(@query("SELECT * FROM users"), "name")
emails_only: @map.field(@query("SELECT * FROM users"), "email")
ids_only: @map.field(@query("SELECT * FROM products"), "id")
```

### **Expression Mapping**
```go
[expression_mapping]
full_names: @map.expression(@query("SELECT * FROM users"), "first_name + ' ' + last_name")
price_with_tax: @map.expression(@query("SELECT * FROM products"), "price * 1.1")
age_groups: @map.expression(@query("SELECT * FROM users"), "CASE WHEN age < 18 THEN 'minor' WHEN age < 65 THEN 'adult' ELSE 'senior' END")
```

### **Function Mapping**
```go
[function_mapping]
uppercase_names: @map.function(@query("SELECT * FROM users"), "name", "upper")
lowercase_emails: @map.function(@query("SELECT * FROM users"), "email", "lower")
rounded_prices: @map.function(@query("SELECT * FROM products"), "price", "round")
```

### **Custom Mapping**
```go
[custom_mapping]
formatted_phones: @map.custom(@query("SELECT * FROM contacts"), "phone", "format_phone")
masked_emails: @map.custom(@query("SELECT * FROM users"), "email", "mask_email")
shortened_names: @map.custom(@query("SELECT * FROM users"), "name", "shorten_name")
```

## 📊 **Mapping Functions**

### **String Transformations**
```go
[string_mapping]
upper_names: @map.upper(@query("SELECT * FROM users"), "name")
lower_emails: @map.lower(@query("SELECT * FROM users"), "email")
trimmed_descriptions: @map.trim(@query("SELECT * FROM products"), "description")
capitalized_titles: @map.capitalize(@query("SELECT * FROM products"), "title")
```

### **Numeric Transformations**
```go
[numeric_mapping]
doubled_prices: @map.multiply(@query("SELECT * FROM products"), "price", 2)
halved_quantities: @map.divide(@query("SELECT * FROM orders"), "quantity", 2)
incremented_ids: @map.add(@query("SELECT * FROM users"), "id", 1000)
decremented_scores: @map.subtract(@query("SELECT * FROM scores"), "score", 10)
```

### **Type Conversions**
```go
[type_mapping]
string_ids: @map.to_string(@query("SELECT * FROM users"), "id")
float_prices: @map.to_float(@query("SELECT * FROM products"), "price")
int_quantities: @map.to_int(@query("SELECT * FROM orders"), "quantity")
bool_status: @map.to_bool(@query("SELECT * FROM users"), "active")
```

### **Conditional Mapping**
```go
[conditional_mapping]
status_labels: @map.conditional(@query("SELECT * FROM orders"), "status", {
    "pending": "Awaiting Processing",
    "processing": "In Progress",
    "shipped": "On the Way",
    "delivered": "Completed"
})
priority_colors: @map.conditional(@query("SELECT * FROM tasks"), "priority", {
    "high": "red",
    "medium": "yellow",
    "low": "green"
})
```

## 🔗 **Go Integration**

```go
// Access mapped data
userNames := config.GetArray("user_names")
userEmails := config.GetArray("user_emails")
productPrices := config.GetArray("product_prices")

// Process mapped results
for i, name := range userNames {
    fmt.Printf("User %d: %s\n", i+1, name)
}
```

### **Manual Map Implementation**
```go
type DataMapper struct{}

func (m *DataMapper) MapBy(data []map[string]interface{}, field string) []interface{} {
    var result []interface{}
    
    for _, item := range data {
        if value, exists := item[field]; exists {
            result = append(result, value)
        }
    }
    
    return result
}

func (m *DataMapper) MapByExpression(data []map[string]interface{}, expression string) []interface{} {
    var result []interface{}
    
    for _, item := range data {
        value := m.evaluateExpression(item, expression)
        result = append(result, value)
    }
    
    return result
}

func (m *DataMapper) MapByFunction(data []map[string]interface{}, field, function string) []interface{} {
    var result []interface{}
    
    for _, item := range data {
        if value, exists := item[field]; exists {
            transformed := m.applyFunction(value, function)
            result = append(result, transformed)
        }
    }
    
    return result
}

func (m *DataMapper) MapConditional(data []map[string]interface{}, field string, mapping map[string]interface{}) []interface{} {
    var result []interface{}
    
    for _, item := range data {
        if value, exists := item[field]; exists {
            if mapped, found := mapping[fmt.Sprint(value)]; found {
                result = append(result, mapped)
            } else {
                result = append(result, value)
            }
        }
    }
    
    return result
}

func (m *DataMapper) evaluateExpression(item map[string]interface{}, expression string) interface{} {
    // Implement expression evaluation logic
    // This is a simplified implementation
    return expression
}

func (m *DataMapper) applyFunction(value interface{}, function string) interface{} {
    switch function {
    case "upper":
        if str, ok := value.(string); ok {
            return strings.ToUpper(str)
        }
    case "lower":
        if str, ok := value.(string); ok {
            return strings.ToLower(str)
        }
    case "trim":
        if str, ok := value.(string); ok {
            return strings.TrimSpace(str)
        }
    }
    return value
}
```

## 🥇 **Best Practices**
- Use clear, descriptive mapping rules
- Consider performance with large datasets
- Validate mapped results
- Use appropriate mapping functions for your data types
- Document mapping logic clearly

---

**TuskLang: Transformed data with @map.** 