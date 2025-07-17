# @split Operator in TuskLang - Go Guide

## ✂️ **Split Power: @split Operator Unleashed**

TuskLang's `@split` operator is your data separation rebellion. We don't bow to any king—especially not to monolithic, unwieldy configurations. Here's how to use `@split` in Go projects to divide, separate, and organize your configuration data.

## 📋 **Table of Contents**
- [What is @split?](#what-is-split)
- [Basic Usage](#basic-usage)
- [Split Strategies](#split-strategies)
- [Data Organization](#data-organization)
- [Go Integration](#go-integration)
- [Best Practices](#best-practices)

## ✂️ **What is @split?**

The `@split` operator divides data into smaller, manageable pieces. No more monolithic configs—just pure, organized separation.

## 🛠️ **Basic Usage**

```go
[split_operations]
user_data: @split.by_field(@query("SELECT * FROM users"), "department")
config_sections: @split.by_section("monolithic.tsk")
file_chunks: @split.by_size("large_file.txt", 1024)
```

## 🔧 **Split Strategies**

### **Field-Based Splitting**
```go
[field_splits]
by_department: @split.by_field(@query("SELECT * FROM employees"), "department")
by_status: @split.by_field(@query("SELECT * FROM orders"), "status")
by_region: @split.by_field(@query("SELECT * FROM customers"), "region")
```

### **Size-Based Splitting**
```go
[size_splits]
large_files: @split.by_size("big_file.dat", 1048576)  # 1MB chunks
config_chunks: @split.by_size("huge_config.tsk", 4096)  # 4KB chunks
log_chunks: @split.by_size("app.log", 10240)  # 10KB chunks
```

### **Condition-Based Splitting**
```go
[condition_splits]
active_users: @split.by_condition(@query("SELECT * FROM users"), "status = 'active'")
high_value: @split.by_condition(@query("SELECT * FROM orders"), "amount > 1000")
recent_data: @split.by_condition(@query("SELECT * FROM events"), "created_at > NOW() - INTERVAL '1 day'")
```

### **Pattern-Based Splitting**
```go
[pattern_splits]
by_email: @split.by_pattern(@query("SELECT * FROM users"), "email", "@.*\\.com$")
by_phone: @split.by_pattern(@query("SELECT * FROM contacts"), "phone", "^\\+1-")
by_name: @split.by_pattern(@query("SELECT * FROM customers"), "name", "^[A-Z]")
```

## 📊 **Data Organization**

### **Configuration Separation**
```go
[config_splits]
database_config: @split.section("main.tsk", "database")
api_config: @split.section("main.tsk", "api")
cache_config: @split.section("main.tsk", "cache")
```

### **Data Processing Splits**
```go
[processing_splits]
batch_1: @split.range(@query("SELECT * FROM large_table"), 0, 1000)
batch_2: @split.range(@query("SELECT * FROM large_table"), 1000, 2000)
batch_3: @split.range(@query("SELECT * FROM large_table"), 2000, 3000)
```

### **Time-Based Splitting**
```go
[time_splits]
today: @split.by_time(@query("SELECT * FROM events"), "created_at", "today")
this_week: @split.by_time(@query("SELECT * FROM events"), "created_at", "this_week")
this_month: @split.by_time(@query("SELECT * FROM events"), "created_at", "this_month")
```

## 🔗 **Go Integration**

```go
// Access split data
userData := config.GetObject("user_data")
configSections := config.GetObject("config_sections")
fileChunks := config.GetArray("file_chunks")

// Process split results
for department, users := range userData {
    fmt.Printf("Department %s: %d users\n", department, len(users.([]interface{})))
}
```

### **Manual Split Implementation**
```go
type DataSplitter struct{}

func (s *DataSplitter) SplitByField(data []map[string]interface{}, field string) map[string][]map[string]interface{} {
    result := make(map[string][]map[string]interface{})
    
    for _, item := range data {
        if value, exists := item[field]; exists {
            key := fmt.Sprint(value)
            result[key] = append(result[key], item)
        }
    }
    
    return result
}

func (s *DataSplitter) SplitBySize(data []byte, size int) [][]byte {
    var chunks [][]byte
    
    for i := 0; i < len(data); i += size {
        end := i + size
        if end > len(data) {
            end = len(data)
        }
        chunks = append(chunks, data[i:end])
    }
    
    return chunks
}

func (s *DataSplitter) SplitByCondition(data []map[string]interface{}, condition string) ([]map[string]interface{}, []map[string]interface{}) {
    var matching, nonMatching []map[string]interface{}
    
    for _, item := range data {
        if s.evaluateCondition(item, condition) {
            matching = append(matching, item)
        } else {
            nonMatching = append(nonMatching, item)
        }
    }
    
    return matching, nonMatching
}

func (s *DataSplitter) evaluateCondition(item map[string]interface{}, condition string) bool {
    // Implement condition evaluation logic
    return true
}
```

## 🥇 **Best Practices**
- Use meaningful split criteria
- Consider performance implications of splitting
- Validate split results
- Use splitting for data organization and processing
- Document split logic clearly

---

**TuskLang: Organized data with @split.** 