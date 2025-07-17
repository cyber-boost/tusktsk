# 📋 TuskLang Bash @array Function Guide

**"We don't bow to any king" – Arrays are your configuration's collections.**

The @array function in TuskLang is your collection manipulation powerhouse, enabling dynamic array operations, filtering, mapping, and transformation directly within your configuration files. Whether you're processing lists of servers, managing user groups, or building dynamic configurations, @array provides the flexibility and power to handle collections efficiently.

## 🎯 What is @array?
The @array function performs array operations and manipulations in TuskLang. It provides:
- **Array creation** - Create arrays from various sources
- **Array manipulation** - Add, remove, sort, filter elements
- **Array transformation** - Map, reduce, transform arrays
- **Array analysis** - Length, sum, min, max, average
- **Dynamic arrays** - Arrays that adapt to data changes

## 📝 Basic @array Syntax

### Array Creation
```ini
[creation]
# Create arrays from different sources
static_array: @array(["apple", "banana", "cherry"])
from_string: @array.split("server1,server2,server3", ",")
from_range: @array.range(1, 10)
from_query: @array.from_query("SELECT name FROM servers WHERE active = 1")
```

### Array Operations
```ini
[operations]
# Basic array operations
$fruits: ["apple", "banana", "cherry", "date"]
array_length: @array.length($fruits)
first_element: @array.first($fruits)
last_element: @array.last($fruits)
sorted_array: @array.sort($fruits)
reversed_array: @array.reverse($fruits)
```

### Array Transformation
```ini
[transformation]
# Transform arrays
$numbers: [1, 2, 3, 4, 5]
doubled: @array.map($numbers, "item * 2")
filtered: @array.filter($numbers, "item > 2")
sum: @array.sum($numbers)
average: @array.average($numbers)
```

## 🚀 Quick Start Example

```bash
#!/bin/bash
source tusk-bash.sh

cat > array-quickstart.tsk << 'EOF'
[basic_arrays]
# Basic array operations
fruits: @array(["apple", "banana", "cherry", "date"])
numbers: @array([1, 2, 3, 4, 5, 6, 7, 8, 9, 10])
servers: @array(["web1", "web2", "db1", "cache1"])

[array_analysis]
# Analyze arrays
fruits_count: @array.length($fruits)
numbers_sum: @array.sum($numbers)
numbers_average: @array.average($numbers)
numbers_max: @array.max($numbers)
numbers_min: @array.min($numbers)

[array_transformation]
# Transform arrays
doubled_numbers: @array.map($numbers, "item * 2")
even_numbers: @array.filter($numbers, "item % 2 == 0")
web_servers: @array.filter($servers, "item.starts_with('web')")
uppercase_fruits: @array.map($fruits, "item.upper()")

[array_operations]
# Array operations
sorted_fruits: @array.sort($fruits)
reversed_numbers: @array.reverse($numbers)
unique_servers: @array.unique($servers)
fruits_joined: @array.join($fruits, ", ")
EOF

config=$(tusk_parse array-quickstart.tsk)

echo "=== Basic Arrays ==="
echo "Fruits: $(tusk_get "$config" basic_arrays.fruits)"
echo "Numbers: $(tusk_get "$config" basic_arrays.numbers)"
echo "Servers: $(tusk_get "$config" basic_arrays.servers)"

echo ""
echo "=== Array Analysis ==="
echo "Fruits Count: $(tusk_get "$config" array_analysis.fruits_count)"
echo "Numbers Sum: $(tusk_get "$config" array_analysis.numbers_sum)"
echo "Numbers Average: $(tusk_get "$config" array_analysis.numbers_average)"
echo "Numbers Max: $(tusk_get "$config" array_analysis.numbers_max)"
echo "Numbers Min: $(tusk_get "$config" array_analysis.numbers_min)"

echo ""
echo "=== Array Transformation ==="
echo "Doubled Numbers: $(tusk_get "$config" array_transformation.doubled_numbers)"
echo "Even Numbers: $(tusk_get "$config" array_transformation.even_numbers)"
echo "Web Servers: $(tusk_get "$config" array_transformation.web_servers)"
echo "Uppercase Fruits: $(tusk_get "$config" array_transformation.uppercase_fruits)"

echo ""
echo "=== Array Operations ==="
echo "Sorted Fruits: $(tusk_get "$config" array_operations.sorted_fruits)"
echo "Reversed Numbers: $(tusk_get "$config" array_operations.reversed_numbers)"
echo "Unique Servers: $(tusk_get "$config" array_operations.unique_servers)"
echo "Fruits Joined: $(tusk_get "$config" array_operations.fruits_joined)"
```

## 🔗 Real-World Use Cases

### 1. Server Management and Load Balancing
```ini
[server_management]
# Dynamic server arrays from database
all_servers: @array.from_query("SELECT hostname FROM servers WHERE active = 1")
web_servers: @array.from_query("SELECT hostname FROM servers WHERE type = 'web' AND active = 1")
db_servers: @array.from_query("SELECT hostname FROM servers WHERE type = 'database' AND active = 1")
cache_servers: @array.from_query("SELECT hostname FROM servers WHERE type = 'cache' AND active = 1")

# Server analysis
total_servers: @array.length($all_servers)
web_server_count: @array.length($web_servers)
db_server_count: @array.length($db_servers)
cache_server_count: @array.length($cache_servers)

# Load balancing configuration
load_balancer_pools: {
    "web": $web_servers,
    "database": $db_servers,
    "cache": $cache_servers
}

# Health check URLs
health_check_urls: @array.map($all_servers, "item + '/health'")
ping_urls: @array.map($all_servers, "item + '/ping'")
```

### 2. User Management and Permissions
```ini
[user_management]
# User arrays from database
all_users: @array.from_query("SELECT username FROM users WHERE active = 1")
admin_users: @array.from_query("SELECT username FROM users WHERE role = 'admin' AND active = 1")
premium_users: @array.from_query("SELECT username FROM users WHERE subscription_type = 'premium' AND active = 1")
new_users: @array.from_query("SELECT username FROM users WHERE created_at >= DATE_SUB(NOW(), INTERVAL 7 DAY)")

# User statistics
total_users: @array.length($all_users)
admin_count: @array.length($admin_users)
premium_count: @array.length($premium_users)
new_user_count: @array.length($new_users)

# Permission groups
admin_permissions: @array(["read", "write", "delete", "admin"])
user_permissions: @array(["read", "write"])
guest_permissions: @array(["read"])

# Dynamic permission assignment
user_permission_map: {
    "admin": $admin_permissions,
    "premium": $user_permissions,
    "basic": $user_permissions,
    "guest": $guest_permissions
}
```

### 3. Configuration Management
```ini
[configuration_management]
# Environment-specific configurations
environments: @array(["development", "staging", "production"])
feature_flags: @array.from_query("SELECT name FROM feature_flags WHERE active = 1")
config_files: @array(["database.tsk", "api.tsk", "security.tsk", "logging.tsk"])

# Dynamic configuration building
env_configs: @array.map($environments, "item + '.tsk'")
feature_flag_names: @array.map($feature_flags, "item")
config_paths: @array.map($config_files, "/etc/app/" + item)

# Configuration validation
required_configs: @array(["database", "api", "security"])
optional_configs: @array(["logging", "monitoring", "analytics"])

# Environment-specific settings
dev_settings: @array(["debug=true", "log_level=debug", "cache=false"])
prod_settings: @array(["debug=false", "log_level=error", "cache=true"])
```

### 4. Data Processing and Analytics
```ini
[data_processing]
# Data arrays from queries
user_ids: @array.from_query("SELECT id FROM users WHERE last_login >= DATE_SUB(NOW(), INTERVAL 30 DAY)")
order_amounts: @array.from_query("SELECT amount FROM orders WHERE status = 'completed' AND created_at >= DATE_SUB(NOW(), INTERVAL 7 DAY)")
response_times: @array.from_query("SELECT response_time FROM api_logs WHERE created_at >= DATE_SUB(NOW(), INTERVAL 1 HOUR)")

# Statistical analysis
total_orders: @array.length($order_amounts)
total_revenue: @array.sum($order_amounts)
average_order: @array.average($order_amounts)
max_order: @array.max($order_amounts)
min_order: @array.min($order_amounts)

# Performance metrics
avg_response_time: @array.average($response_times)
max_response_time: @array.max($response_times)
min_response_time: @array.min($response_times)
slow_requests: @array.filter($response_times, "item > 1000")

# Data categorization
high_value_orders: @array.filter($order_amounts, "item > 100")
medium_value_orders: @array.filter($order_amounts, "item > 50 && item <= 100")
low_value_orders: @array.filter($order_amounts, "item <= 50")
```

## 🧠 Advanced @array Patterns

### Complex Array Operations
```bash
#!/bin/bash
source tusk-bash.sh

cat > complex-arrays.tsk << 'EOF'
[complex_operations]
# Complex array operations
$base_numbers: @array([1, 2, 3, 4, 5, 6, 7, 8, 9, 10])
$server_data: @array.from_query("SELECT hostname, cpu_usage, memory_usage FROM servers WHERE active = 1")

# Multi-step transformations
even_numbers: @array.filter($base_numbers, "item % 2 == 0")
squared_even: @array.map($even_numbers, "item * item")
sum_of_squares: @array.sum($squared_even)

# Complex filtering
high_cpu_servers: @array.filter($server_data, "item.cpu_usage > 80")
high_memory_servers: @array.filter($server_data, "item.memory_usage > 85")
overloaded_servers: @array.filter($server_data, "item.cpu_usage > 80 || item.memory_usage > 85")

# Array composition
$primary_servers: @array(["web1", "web2", "db1"])
$backup_servers: @array(["web3", "web4", "db2"])
all_servers: @array.concat($primary_servers, $backup_servers)
unique_servers: @array.unique($all_servers)
EOF

config=$(tusk_parse complex-arrays.tsk)
echo "Even Numbers: $(tusk_get "$config" complex_operations.even_numbers)"
echo "Squared Even: $(tusk_get "$config" complex_operations.squared_even)"
echo "Sum of Squares: $(tusk_get "$config" complex_operations.sum_of_squares)"
echo "All Servers: $(tusk_get "$config" complex_operations.all_servers)"
echo "Unique Servers: $(tusk_get "$config" complex_operations.unique_servers)"
```

### Array-Based Configuration Generation
```ini
[config_generation]
# Generate configurations from arrays
$environments: @array(["development", "staging", "production"])
$services: @array(["web", "api", "database", "cache"])

# Generate environment-specific configs
env_configs: @array.map($environments, {
    "environment": item,
    "config_file": item + ".tsk",
    "log_level": @if(item == "production", "error", @if(item == "staging", "warn", "debug")),
    "debug": @if(item == "production", false, true)
})

# Generate service configurations
service_configs: @array.map($services, {
    "service": item,
    "port": @if(item == "web", 80, @if(item == "api", 8080, @if(item == "database", 5432, 6379))),
    "health_check": "/" + item + "/health",
    "timeout": @if(item == "database", 30, 10)
})

# Generate load balancer configs
load_balancer_configs: @array.map($services, {
    "service": item,
    "upstream": item + "_servers",
    "port": @if(item == "web", 80, @if(item == "api", 8080, @if(item == "database", 5432, 6379))),
    "health_check_interval": 30
})
```

### Dynamic Array Processing
```ini
[dynamic_processing]
# Process arrays dynamically
$raw_data: @array.from_query("SELECT * FROM raw_data WHERE processed = 0")
$processing_rules: @array([
    {"field": "name", "transform": "upper"},
    {"field": "email", "transform": "lower"},
    {"field": "age", "transform": "int"},
    {"field": "score", "transform": "float"}
])

# Apply processing rules
processed_data: @array.map($raw_data, "apply_rules(item, $processing_rules)")

# Batch processing
batch_size: 100
total_items: @array.length($raw_data)
total_batches: @math(ceil($total_items / $batch_size))
batches: @array.range(0, $total_batches - 1)

# Process in batches
batch_configs: @array.map($batches, {
    "batch_number": item,
    "start_index": item * $batch_size,
    "end_index": min((item + 1) * $batch_size, $total_items),
    "items": @array.slice($raw_data, item * $batch_size, min((item + 1) * $batch_size, $total_items))
})
```

## 🛡️ Security & Performance Notes
- **Input validation:** Always validate array inputs to prevent injection attacks
- **Memory usage:** Be careful with large arrays that could consume significant memory
- **Performance:** Cache expensive array operations to improve performance
- **Type safety:** Ensure array elements have consistent types for operations
- **Overflow protection:** Monitor array sizes to prevent memory issues

## 🐞 Troubleshooting
- **Empty arrays:** Handle empty arrays gracefully in operations
- **Type mismatches:** Ensure consistent data types in array operations
- **Memory issues:** Monitor memory usage with large arrays
- **Performance problems:** Cache expensive array operations
- **Index errors:** Validate array indices before accessing elements

## 💡 Best Practices
- **Validate inputs:** Always validate array inputs before processing
- **Handle empty arrays:** Account for empty arrays in your logic
- **Cache expensive operations:** Cache complex array operations
- **Use appropriate data types:** Ensure consistent types for array elements
- **Document array structures:** Document expected array formats and contents
- **Test thoroughly:** Test array operations with various inputs and edge cases

## 🔗 Cross-References
- [@ Operator Introduction](024-at-operator-intro-bash.md)
- [Arrays](012-arrays-bash.md)
- [Array Operations](066-array-operations-bash.md)
- [Collections](067-object-operations-bash.md)

---

**Master @array in TuskLang and handle collections with power and precision. 📋** 