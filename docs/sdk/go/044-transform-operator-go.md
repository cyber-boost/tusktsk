# @transform Operator in TuskLang - Go Guide

## 🔄 **Transform Power: @transform Operator Unleashed**

TuskLang's `@transform` operator is your data manipulation rebellion. We don't bow to any king—especially not to raw, unprocessed data. Here's how to use `@transform` in Go projects to transform, map, filter, and process data directly in your configuration.

## 📋 **Table of Contents**
- [What is @transform?](#what-is-transform)
- [Basic Usage](#basic-usage)
- [Transformation Types](#transformation-types)
- [Data Processing](#data-processing)
- [Go Integration](#go-integration)
- [Best Practices](#best-practices)

## 🔄 **What is @transform?**

The `@transform` operator processes and transforms data directly in your config. No more external processing—just pure, inline data manipulation.

## 🛠️ **Basic Usage**

```go
[data_processing]
uppercase_names: @transform.upper(@query("SELECT name FROM users"))
lowercase_emails: @transform.lower(@query("SELECT email FROM users"))
trimmed_values: @transform.trim(@query("SELECT description FROM products"))
```

## 🔧 **Transformation Types**

### **String Transformations**
```go
[strings]
uppercase: @transform.upper("hello world")
lowercase: @transform.lower("HELLO WORLD")
capitalized: @transform.capitalize("hello world")
trimmed: @transform.trim("  hello world  ")
replaced: @transform.replace("hello world", "hello", "hi")
```

### **Numeric Transformations**
```go
[numbers]
rounded: @transform.round(3.14159, 2)
ceiling: @transform.ceil(3.14159)
floor: @transform.floor(3.14159)
absolute: @transform.abs(-42)
percentage: @transform.percent(0.75)
```

### **Array Transformations**
```go
[arrays]
filtered: @transform.filter(@query("SELECT * FROM users"), "active = true")
mapped: @transform.map(@query("SELECT name FROM users"), "upper(name)")
sorted: @transform.sort(@query("SELECT name FROM users"), "name")
limited: @transform.limit(@query("SELECT * FROM users"), 10)
```

### **Object Transformations**
```go
[objects]
flattened: @transform.flatten({"user": {"name": "John", "age": 30}})
picked: @transform.pick(@query("SELECT * FROM users"), ["name", "email"])
omitted: @transform.omit(@query("SELECT * FROM users"), ["password", "ssn"])
```

## 📊 **Data Processing**

### **Database Result Processing**
```go
[db_processing]
active_users: @transform.filter(@query("SELECT * FROM users"), "status = 'active'")
user_names: @transform.map(@query("SELECT * FROM users"), "name")
sorted_orders: @transform.sort(@query("SELECT * FROM orders"), "created_at DESC")
recent_orders: @transform.limit(@query("SELECT * FROM orders"), 100)
```

### **API Response Processing**
```go
[api_processing]
weather_data: @transform.pick(@http("GET", "https://api.weather.com/current"), ["temperature", "humidity"])
user_profiles: @transform.map(@http("GET", "https://api.users.com/profiles"), "name")
filtered_products: @transform.filter(@http("GET", "https://api.products.com/list"), "price < 100")
```

## 🔗 **Go Integration**

```go
// Access transformed data
uppercaseNames := config.GetArray("uppercase_names")
activeUsers := config.GetArray("active_users")
weatherData := config.GetObject("weather_data")

// Process transformed results
for _, name := range uppercaseNames {
    fmt.Printf("Name: %s\n", name)
}
```

### **Manual Data Transformation**
```go
type DataTransformer struct{}

func (t *DataTransformer) Transform(data interface{}, operation string, args ...interface{}) (interface{}, error) {
    switch operation {
    case "upper":
        if str, ok := data.(string); ok {
            return strings.ToUpper(str), nil
        }
    case "lower":
        if str, ok := data.(string); ok {
            return strings.ToLower(str), nil
        }
    case "trim":
        if str, ok := data.(string); ok {
            return strings.TrimSpace(str), nil
        }
    }
    return data, nil
}

func (t *DataTransformer) Filter(data []interface{}, condition string) ([]interface{}, error) {
    // Implement filtering logic
    return data, nil
}

func (t *DataTransformer) Map(data []interface{}, expression string) ([]interface{}, error) {
    // Implement mapping logic
    return data, nil
}
```

## 🥇 **Best Practices**
- Use transformations for data cleaning and formatting
- Chain transformations for complex processing
- Validate transformed data before using
- Cache expensive transformations with @cache
- Document transformation logic clearly

---

**TuskLang: Data transformation power with @transform.** 