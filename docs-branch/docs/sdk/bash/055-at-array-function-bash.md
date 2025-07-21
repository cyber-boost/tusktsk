# 📊 TuskLang Bash @array Function Guide

**"We don't bow to any king" – Arrays are your configuration's data structures.**

The @array function in TuskLang is your data manipulation powerhouse, enabling dynamic array operations, list processing, and collection management directly within your configuration files. Whether you're processing lists, filtering data, or transforming collections, @array provides the flexibility and power to work with data structures seamlessly.

## 🎯 What is @array?
The @array function provides array operations in TuskLang. It offers:
- **Array creation** - Create arrays from various data sources
- **Array manipulation** - Add, remove, and modify array elements
- **Array iteration** - Process arrays with map, filter, and reduce operations
- **Array searching** - Find elements and check array contents
- **Array transformation** - Convert and transform array data

## 📝 Basic @array Syntax

### Array Creation
```ini
[array_creation]
# Create simple arrays
numbers: @array.create([1, 2, 3, 4, 5])
names: @array.create(["Alice", "Bob", "Charlie", "Diana"])
mixed: @array.create([1, "hello", true, 3.14])

# Create arrays from strings
$csv_data: "apple,banana,cherry,date"
fruits: @array.from_string($csv_data, ",")

$space_separated: "red green blue yellow"
colors: @array.from_string($space_separated, " ")

# Create arrays from ranges
range_1_to_10: @array.range(1, 10)
range_0_to_100_step_5: @array.range(0, 100, 5)
```

### Array Access and Manipulation
```ini
[array_manipulation]
# Access array elements
$numbers: [1, 2, 3, 4, 5]
first_element: @array.get($numbers, 0)
last_element: @array.get($numbers, -1)
middle_element: @array.get($numbers, 2)

# Add elements to arrays
$original: [1, 2, 3]
with_push: @array.push($original, 4, 5)
with_unshift: @array.unshift($original, 0, -1)

# Remove elements from arrays
$full_array: [1, 2, 3, 4, 5]
without_last: @array.pop($full_array)
without_first: @array.shift($full_array)
sliced: @array.slice($full_array, 1, 3)
```

### Array Operations
```ini
[array_operations]
# Array iteration and transformation
$data: [1, 2, 3, 4, 5]
doubled: @array.map($data, "item * 2")
filtered: @array.filter($data, "item > 2")
sum: @array.reduce($data, "sum + item", 0)

# Array searching
$names: ["Alice", "Bob", "Charlie", "Diana", "Eve"]
contains_alice: @array.includes($names, "Alice")
alice_index: @array.index_of($names, "Alice")
find_long_names: @array.find($names, "string.length(item) > 4")
```

## 🚀 Quick Start Example

```bash
#!/bin/bash
source tusk-bash.sh

cat > array-quickstart.tsk << 'EOF'
[basic_arrays]
# Create and manipulate arrays
numbers: @array.create([1, 2, 3, 4, 5])
names: @array.create(["Alice", "Bob", "Charlie"])

# Array operations
doubled_numbers: @array.map($numbers, "item * 2")
filtered_numbers: @array.filter($numbers, "item > 2")
sum_numbers: @array.reduce($numbers, "sum + item", 0)

[array_manipulation]
# Add and remove elements
$original: [1, 2, 3]
with_new_elements: @array.push($original, 4, 5)
without_last: @array.pop($original)

# Array slicing and access
$full_array: [10, 20, 30, 40, 50]
first_three: @array.slice($full_array, 0, 3)
last_three: @array.slice($full_array, -3)
middle_elements: @array.slice($full_array, 1, 4)

[array_searching]
# Search and find elements
$search_array: ["apple", "banana", "cherry", "date", "elderberry"]
contains_banana: @array.includes($search_array, "banana")
banana_index: @array.index_of($search_array, "banana")
long_fruits: @array.filter($search_array, "string.length(item) > 5")

[array_transformation]
# Transform array data
$user_data: [
    {"name": "Alice", "age": 25},
    {"name": "Bob", "age": 30},
    {"name": "Charlie", "age": 35}
]

names_only: @array.map($user_data, "item.name")
adults_only: @array.filter($user_data, "item.age >= 18")
average_age: @array.reduce($user_data, "sum + item.age", 0) / @array.length($user_data)
EOF

config=$(tusk_parse array-quickstart.tsk)

echo "=== Basic Arrays ==="
echo "Numbers: $(tusk_get "$config" basic_arrays.numbers)"
echo "Names: $(tusk_get "$config" basic_arrays.names)"
echo "Doubled: $(tusk_get "$config" basic_arrays.doubled_numbers)"
echo "Filtered: $(tusk_get "$config" basic_arrays.filtered_numbers)"
echo "Sum: $(tusk_get "$config" basic_arrays.sum_numbers)"

echo ""
echo "=== Array Manipulation ==="
echo "With New Elements: $(tusk_get "$config" array_manipulation.with_new_elements)"
echo "First Three: $(tusk_get "$config" array_manipulation.first_three)"
echo "Last Three: $(tusk_get "$config" array_manipulation.last_three)"

echo ""
echo "=== Array Searching ==="
echo "Contains Banana: $(tusk_get "$config" array_searching.contains_banana)"
echo "Banana Index: $(tusk_get "$config" array_searching.banana_index)"
echo "Long Fruits: $(tusk_get "$config" array_searching.long_fruits)"

echo ""
echo "=== Array Transformation ==="
echo "Names Only: $(tusk_get "$config" array_transformation.names_only)"
echo "Adults Only: $(tusk_get "$config" array_transformation.adults_only)"
echo "Average Age: $(tusk_get "$config" array_transformation.average_age)"
```

## 🔗 Real-World Use Cases

### 1. Data Processing and Analytics
```ini
[data_processing]
# Process user data arrays
$user_analytics: {
    "user_ids": @array.create([1, 2, 3, 4, 5, 6, 7, 8, 9, 10]),
    "user_names": @array.create(["Alice", "Bob", "Charlie", "Diana", "Eve", "Frank", "Grace", "Henry", "Ivy", "Jack"]),
    "user_ages": @array.create([25, 30, 35, 28, 22, 40, 29, 33, 26, 31]),
    "user_scores": @array.create([85, 92, 78, 95, 88, 76, 91, 83, 89, 94])
}

# Calculate analytics
$analytics_results: {
    "total_users": @array.length($user_analytics.user_ids),
    "average_age": @array.reduce($user_analytics.user_ages, "sum + item", 0) / @array.length($user_analytics.user_ages),
    "average_score": @array.reduce($user_analytics.user_scores, "sum + item", 0) / @array.length($user_analytics.user_scores),
    "high_scorers": @array.filter($user_analytics.user_scores, "item >= 90"),
    "young_users": @array.filter($user_analytics.user_ages, "item < 30"),
    "top_performers": @array.map(@array.filter($user_analytics.user_scores, "item >= 90"), "item")
}

# Create user profiles
$user_profiles: @array.map($user_analytics.user_ids, {
    "id": "item",
    "name": @array.get($user_analytics.user_names, "index"),
    "age": @array.get($user_analytics.user_ages, "index"),
    "score": @array.get($user_analytics.user_scores, "index"),
    "category": @if(@array.get($user_analytics.user_scores, "index") >= 90, "excellent", 
        @if(@array.get($user_analytics.user_scores, "index") >= 80, "good", "average"))
})

# Find top users
$top_users: @array.filter($user_profiles, "item.score >= 90")
$young_excellent: @array.filter($user_profiles, "item.age < 30 && item.score >= 90")
```

### 2. Configuration Management
```ini
[config_management]
# Manage configuration arrays
$server_configs: {
    "production": {
        "hosts": @array.create(["prod1.example.com", "prod2.example.com", "prod3.example.com"]),
        "ports": @array.create([80, 443, 8080]),
        "databases": @array.create(["prod_db_1", "prod_db_2"])
    },
    "staging": {
        "hosts": @array.create(["staging1.example.com", "staging2.example.com"]),
        "ports": @array.create([8080, 8443]),
        "databases": @array.create(["staging_db"])
    },
    "development": {
        "hosts": @array.create(["localhost"]),
        "ports": @array.create([3000, 3001]),
        "databases": @array.create(["dev_db"])
    }
}

# Process configuration data
$config_processing: {
    "all_hosts": @array.concat($server_configs.production.hosts, $server_configs.staging.hosts, $server_configs.development.hosts),
    "all_ports": @array.concat($server_configs.production.ports, $server_configs.staging.ports, $server_configs.development.ports),
    "unique_ports": @array.unique(@array.concat($server_configs.production.ports, $server_configs.staging.ports, $server_configs.development.ports)),
    "production_servers": @array.map($server_configs.production.hosts, {
        "host": "item",
        "environment": "production",
        "ports": $server_configs.production.ports
    })
}

# Feature flags management
$feature_flags: @array.create([
    {"name": "new_ui", "enabled": true, "environments": ["development", "staging"]},
    {"name": "beta_features", "enabled": false, "environments": ["development"]},
    {"name": "analytics", "enabled": true, "environments": ["production", "staging"]},
    {"name": "debug_mode", "enabled": true, "environments": ["development"]}
])

$active_features: @array.filter($feature_flags, "item.enabled")
$dev_features: @array.filter($feature_flags, "@array.includes(item.environments, 'development')")
```

### 3. API Response Processing
```ini
[api_processing]
# Process API response arrays
$api_responses: {
    "users": @array.create([
        {"id": 1, "name": "Alice", "email": "alice@example.com", "status": "active"},
        {"id": 2, "name": "Bob", "email": "bob@example.com", "status": "inactive"},
        {"id": 3, "name": "Charlie", "email": "charlie@example.com", "status": "active"},
        {"id": 4, "name": "Diana", "email": "diana@example.com", "status": "pending"}
    ]),
    "orders": @array.create([
        {"id": 1, "user_id": 1, "amount": 150.00, "status": "completed"},
        {"id": 2, "user_id": 1, "amount": 75.50, "status": "completed"},
        {"id": 3, "user_id": 2, "amount": 200.00, "status": "pending"},
        {"id": 4, "user_id": 3, "amount": 125.25, "status": "completed"}
    ])
}

# Process user data
$user_processing: {
    "active_users": @array.filter($api_responses.users, "item.status == 'active'"),
    "user_emails": @array.map($api_responses.users, "item.email"),
    "user_names": @array.map($api_responses.users, "item.name"),
    "user_count_by_status": @array.reduce($api_responses.users, {
        "active": @if(item.status == "active", sum.active + 1, sum.active),
        "inactive": @if(item.status == "inactive", sum.inactive + 1, sum.inactive),
        "pending": @if(item.status == "pending", sum.pending + 1, sum.pending)
    }, {"active": 0, "inactive": 0, "pending": 0})
}

# Process order data
$order_processing: {
    "completed_orders": @array.filter($api_responses.orders, "item.status == 'completed'"),
    "total_revenue": @array.reduce($api_responses.orders, "sum + item.amount", 0),
    "completed_revenue": @array.reduce(@array.filter($api_responses.orders, "item.status == 'completed'"), "sum + item.amount", 0),
    "orders_by_user": @array.reduce($api_responses.orders, {
        "user_" + item.user_id: @array.push(sum["user_" + item.user_id] || [], item)
    }, {})
}
```

### 4. File and Directory Processing
```ini
[file_processing]
# Process file and directory arrays
$file_operations: {
    "log_files": @array.create(@file.glob("/var/log/*.log")),
    "config_files": @array.create(@file.glob("/etc/*.conf")),
    "backup_files": @array.create(@file.glob("/backups/*.tar.gz")),
    "temp_files": @array.create(@file.glob("/tmp/*.tmp"))
}

# Process file data
$file_processing: {
    "log_file_sizes": @array.map($file_operations.log_files, @file.size("item")),
    "large_log_files": @array.filter($file_operations.log_files, "@file.size(item) > 10485760"),  # > 10MB
    "recent_configs": @array.filter($file_operations.config_files, "@file.modified(item) > @date.sub(@date.now(), '7d')"),
    "old_backups": @array.filter($file_operations.backup_files, "@file.modified(item) < @date.sub(@date.now(), '30d')")
}

# Directory structure processing
$directory_structure: {
    "web_dirs": @array.create(@file.glob("/var/www/*/")),
    "app_dirs": @array.create(@file.glob("/apps/*/")),
    "data_dirs": @array.create(@file.glob("/data/*/"))
}

$dir_processing: {
    "web_apps": @array.map($directory_structure.web_dirs, {
        "path": "item",
        "name": @string.basename("item"),
        "size": @file.size("item"),
        "files": @array.length(@file.glob("item/*"))
    }),
    "large_apps": @array.filter($dir_processing.web_apps, "item.size > 1073741824"),  # > 1GB
    "app_names": @array.map($dir_processing.web_apps, "item.name")
}
```

## 🧠 Advanced @array Patterns

### Complex Array Operations
```ini
[complex_operations]
# Advanced array manipulation
$advanced_arrays: {
    "nested_data": @array.create([
        {"id": 1, "data": [1, 2, 3]},
        {"id": 2, "data": [4, 5, 6]},
        {"id": 3, "data": [7, 8, 9]}
    ]),
    "matrix": @array.create([
        [1, 2, 3],
        [4, 5, 6],
        [7, 8, 9]
    ])
}

# Flatten nested arrays
$flattened_data: @array.flatten(@array.map($advanced_arrays.nested_data, "item.data"))
$flattened_matrix: @array.flatten($advanced_arrays.matrix)

# Matrix operations
$matrix_operations: {
    "transpose": @array.transpose($advanced_arrays.matrix),
    "diagonal": @array.map($advanced_arrays.matrix, "item[index]"),
    "sum_rows": @array.map($advanced_arrays.matrix, "@array.reduce(item, 'sum + element', 0)")
}

# Group by operations
$grouping_data: @array.create([
    {"category": "fruit", "name": "apple", "price": 1.00},
    {"category": "fruit", "name": "banana", "price": 0.50},
    {"category": "vegetable", "name": "carrot", "price": 0.75},
    {"category": "fruit", "name": "cherry", "price": 2.00},
    {"category": "vegetable", "name": "lettuce", "price": 1.25}
])

$grouped_by_category: @array.group_by($grouping_data, "item.category")
$category_totals: @array.map($grouped_by_category, {
    "category": "key",
    "items": "value",
    "total_price": "@array.reduce(value, 'sum + item.price', 0)",
    "item_count": "@array.length(value)"
})
```

### Performance Optimization
```ini
[performance_optimization]
# Optimize array operations
$optimized_arrays: {
    "large_dataset": @array.create(@range(1, 10000)),
    "filtered_data": @array.filter($optimized_arrays.large_dataset, "item % 2 == 0"),
    "mapped_data": @array.map($optimized_arrays.large_dataset, "item * 2"),
    "reduced_data": @array.reduce($optimized_arrays.large_dataset, "sum + item", 0)
}

# Chunk large arrays for processing
$chunked_processing: {
    "chunks": @array.chunk($optimized_arrays.large_dataset, 1000),
    "processed_chunks": @array.map($chunked_processing.chunks, "@array.reduce(item, 'sum + element', 0)"),
    "total_sum": @array.reduce($processed_chunks, "sum + item", 0)
}

# Lazy evaluation for large datasets
$lazy_processing: {
    "stream": @array.stream($optimized_arrays.large_dataset),
    "filtered_stream": @array.filter($lazy_processing.stream, "item > 5000"),
    "limited_stream": @array.limit($lazy_processing.filtered_stream, 100)
}
```

### Array Validation and Error Handling
```ini
[array_validation]
# Validate array operations
$validation_arrays: {
    "valid_array": @array.create([1, 2, 3, 4, 5]),
    "empty_array": @array.create([]),
    "null_array": null,
    "mixed_array": @array.create([1, "string", true, null, 3.14])
}

# Safe array operations
$safe_operations: {
    "safe_length": @array.safe_length($validation_arrays.valid_array),
    "safe_get": @array.safe_get($validation_arrays.valid_array, 10, "default"),
    "safe_map": @array.safe_map($validation_arrays.mixed_array, "item"),
    "safe_filter": @array.safe_filter($validation_arrays.mixed_array, "item != null")
}

# Array validation
$array_validation: {
    "is_array": @array.is_array($validation_arrays.valid_array),
    "is_empty": @array.is_empty($validation_arrays.empty_array),
    "has_elements": @array.has_elements($validation_arrays.valid_array),
    "all_numbers": @array.every($validation_arrays.valid_array, "typeof(item) == 'number'")
}
```

## 🛡️ Security & Performance Notes
- **Array bounds checking:** Always validate array indices before access
- **Memory management:** Monitor array sizes for large datasets
- **Type safety:** Validate array element types when processing
- **Performance optimization:** Use appropriate array methods for large datasets
- **Error handling:** Implement proper error handling for array operations
- **Data validation:** Validate array data before processing

## 🐞 Troubleshooting
- **Index out of bounds:** Check array length before accessing elements
- **Memory issues:** Monitor array sizes and implement chunking for large datasets
- **Type errors:** Validate array element types before operations
- **Performance problems:** Optimize array operations for large datasets
- **Data corruption:** Validate array data integrity

## 💡 Best Practices
- **Use appropriate methods:** Choose the right array method for your use case
- **Validate inputs:** Always validate array data before processing
- **Handle edge cases:** Consider empty arrays and null values
- **Optimize performance:** Use efficient array operations for large datasets
- **Document operations:** Document complex array operations for maintainability
- **Test thoroughly:** Test array operations with various data types and sizes

## 🔗 Cross-References
- [@ Operator Introduction](024-at-operator-intro-bash.md)
- [@string Function](030-at-string-function-bash.md)
- [@math Function](032-at-math-function-bash.md)
- [Data Processing](108-data-processing-bash.md)
- [Performance Optimization](095-performance-optimization-bash.md)

---

**Master @array in TuskLang and wield the power of data structures in your configurations. 📊** 