# ⚡ @ Operators for Python

**"We don't bow to any king" - Dynamic Configuration Power**

@ operators are TuskLang's revolutionary feature that brings dynamic, intelligent behavior to configuration files. These operators allow you to execute functions, access external data, and create truly responsive configuration.

## 🎯 What are @ Operators?

@ operators are special functions that execute at configuration parse time, providing dynamic values based on:
- Environment variables
- Date and time operations
- File system access
- HTTP requests
- Database queries
- Caching mechanisms
- Machine learning predictions
- And much more!

## 🚀 Basic @ Operator Syntax

```python
from tsk import TSK

config = TSK.from_string("""
[basic]
# Environment variable with fallback
api_key: @env("API_KEY", "default_key")
debug_mode: @env("DEBUG", "false")

# Current timestamp
current_time: @date.now()
formatted_date: @date("Y-m-d H:i:s")

# File operations
config_content: @file.read("config.json")
file_exists: @file.exists("important.txt")

# HTTP requests
weather_data: @http("GET", "https://api.weatherapi.com/v1/current.json?key=YOUR_KEY&q=London")
""")

result = config.parse()
print(f"API Key: {result['basic']['api_key']}")
print(f"Current time: {result['basic']['current_time']}")
print(f"File exists: {result['basic']['file_exists']}")
```

## 🌍 Environment Variables

### Basic Environment Access

```python
config = TSK.from_string("""
[environment]
# Simple environment variable
database_url: @env("DATABASE_URL")

# With default fallback
api_endpoint: @env("API_ENDPOINT", "https://api.example.com")
debug_mode: @env("DEBUG", "false")
log_level: @env("LOG_LEVEL", "info")

# Multiple fallbacks
database_host: @env("DB_HOST", "DB_HOST_ALT", "localhost")
""")
```

### Secure Environment Variables

```python
config = TSK.from_string("""
[secrets]
# Secure environment variables (encrypted in memory)
api_key: @env.secure("API_KEY")
database_password: @env.secure("DB_PASSWORD")
jwt_secret: @env.secure("JWT_SECRET")

# Validate required environment variables
required_vars: @env.required(["API_KEY", "DB_PASSWORD", "JWT_SECRET"])
""")
```

### Environment Variable Validation

```python
config = TSK.from_string("""
[validation]
# Validate email format
admin_email: @env.validate("ADMIN_EMAIL", "email")

# Validate URL format
api_url: @env.validate("API_URL", "url")

# Validate numeric range
port_number: @env.validate("PORT", "int", min=1, max=65535)

# Validate boolean
debug_enabled: @env.validate("DEBUG", "bool")
""")
```

## 📅 Date and Time Operations

### Current Date/Time

```python
config = TSK.from_string("""
[timestamps]
# Current timestamp
now: @date.now()
current_time: @date("Y-m-d H:i:s")
iso_format: @date("c")
unix_timestamp: @date("U")

# Formatted dates
date_only: @date("Y-m-d")
time_only: @date("H:i:s")
day_name: @date("l")
month_name: @date("F")
""")
```

### Date Arithmetic

```python
config = TSK.from_string("""
[date_math]
# Add time periods
tomorrow: @date.add("1d")
next_week: @date.add("7d")
next_month: @date.add("1m")
next_year: @date.add("1y")

# Subtract time periods
yesterday: @date.subtract("1d")
last_week: @date.subtract("7d")
last_month: @date.subtract("1m")
last_year: @date.subtract("1y")

# Complex periods
three_days_ago: @date.subtract("3d")
two_weeks_from_now: @date.add("2w")
six_months_ago: @date.subtract("6m")
""")
```

### Date Formatting and Parsing

```python
config = TSK.from_string("""
[date_formatting]
# Format specific dates
christmas: @date.format("2024-12-25", "l, F j, Y")
new_year: @date.format("2025-01-01", "Y-m-d H:i:s")

# Parse and format
parsed_date: @date.parse("2024-06-15", "Y-m-d", "F j, Y")

# Timezone operations
utc_time: @date.timezone("UTC")
local_time: @date.timezone("America/New_York")
tokyo_time: @date.timezone("Asia/Tokyo")
""")
```

## 📁 File Operations

### File Reading

```python
config = TSK.from_string("""
[files]
# Read text files
config_json: @file.read("config.json")
log_content: @file.read("app.log")
readme: @file.read("README.md")

# Read with encoding
utf8_file: @file.read("data.txt", "utf-8")
binary_file: @file.read("image.png", "binary")

# Read lines
config_lines: @file.lines("config.txt")
log_lines: @file.lines("app.log", max_lines=100)
""")
```

### File Information

```python
config = TSK.from_string("""
[file_info]
# Check file existence
exists: @file.exists("important.txt")
not_exists: @file.exists("missing.txt")

# File properties
size: @file.size("large_file.dat")
modified: @file.modified("config.json")
created: @file.created("data.txt")

# File type detection
is_text: @file.is_text("config.json")
is_binary: @file.is_binary("image.jpg")
is_executable: @file.is_executable("script.sh")
""")
```

### File Writing and Manipulation

```python
config = TSK.from_string("""
[file_operations]
# Write content
write_result: @file.write("output.txt", "Hello, TuskLang!")
append_result: @file.append("log.txt", "New log entry")

# Copy and move
copy_result: @file.copy("source.txt", "backup.txt")
move_result: @file.move("temp.txt", "final.txt")

# Delete files
delete_result: @file.delete("temp.txt")
""")
```

## 🌐 HTTP Operations

### GET Requests

```python
config = TSK.from_string("""
[http]
# Simple GET request
weather: @http("GET", "https://api.weatherapi.com/v1/current.json?key=YOUR_KEY&q=London")

# GET with headers
api_data: @http("GET", "https://api.example.com/data", {
    "Authorization": "Bearer @env('API_KEY')",
    "Content-Type": "application/json"
})

# GET with timeout
slow_api: @http("GET", "https://slow-api.example.com", timeout=30)
""")
```

### POST Requests

```python
config = TSK.from_string("""
[api]
# POST with JSON data
create_user: @http("POST", "https://api.example.com/users", {
    "name": "Alice",
    "email": "alice@example.com",
    "age": 25
})

# POST with form data
upload_file: @http("POST", "https://api.example.com/upload", {
    "file": "@file.read('data.csv')",
    "type": "csv"
}, headers={"Authorization": "Bearer @env('API_KEY')"})
""")
```

### Advanced HTTP Operations

```python
config = TSK.from_string("""
[advanced_http]
# PUT request
update_user: @http("PUT", "https://api.example.com/users/123", {
    "name": "Alice Updated",
    "email": "alice.updated@example.com"
})

# DELETE request
delete_user: @http("DELETE", "https://api.example.com/users/123")

# PATCH request
patch_user: @http("PATCH", "https://api.example.com/users/123", {
    "active": false
})

# Custom headers and options
custom_request: @http("GET", "https://api.example.com/data", {
    "headers": {
        "Authorization": "Bearer @env('API_KEY')",
        "User-Agent": "TuskLang/1.0"
    },
    "timeout": 60,
    "verify_ssl": false
})
""")
```

## 🗄️ Database Queries

### Basic Queries

```python
config = TSK.from_string("""
[database]
# Simple queries
user_count: @query("SELECT COUNT(*) FROM users")
active_users: @query("SELECT COUNT(*) FROM users WHERE active = 1")

# Parameterized queries
user_by_id: @query("SELECT * FROM users WHERE id = ?", @request.user_id)
users_by_status: @query("SELECT * FROM users WHERE active = ?", @request.active)
""")
```

### Complex Queries

```python
config = TSK.from_string("""
[analytics]
# Join queries
user_orders: @query("""
    SELECT u.name, COUNT(o.id) as order_count, SUM(o.amount) as total_amount
    FROM users u
    LEFT JOIN orders o ON u.id = o.user_id
    WHERE u.active = 1
    GROUP BY u.id, u.name
    ORDER BY total_amount DESC
""")

# Date-based queries
recent_orders: @query("""
    SELECT * FROM orders 
    WHERE created_at > ? 
    ORDER BY created_at DESC
    LIMIT 10
""", @date.subtract("7d"))

# Aggregation queries
revenue_stats: @query("""
    SELECT 
        SUM(amount) as total_revenue,
        AVG(amount) as avg_order,
        COUNT(*) as order_count
    FROM orders 
    WHERE status = 'completed'
""")
""")
```

## 💾 Caching Operations

### Basic Caching

```python
config = TSK.from_string("""
[cache]
# Cache expensive operations
user_count: @cache("5m", @query("SELECT COUNT(*) FROM users"))
weather_data: @cache("1h", @http("GET", "https://api.weatherapi.com/v1/current.json?key=YOUR_KEY&q=London"))

# Cache with parameters
user_profile: @cache("10m", @query("SELECT * FROM users WHERE id = ?", @request.user_id))
user_orders: @cache("2m", @query("SELECT * FROM orders WHERE user_id = ?", @request.user_id))
""")
```

### Advanced Caching

```python
config = TSK.from_string("""
[advanced_cache]
# Cache with custom key
custom_key: @cache("1h", "custom_key", @expensive_operation())

# Cache with conditions
conditional_cache: @cache.if("5m", @request.user_id > 100, @query("SELECT * FROM users WHERE id = ?", @request.user_id))

# Cache invalidation
cache_with_invalidation: @cache.invalidate("user_cache", @query("SELECT * FROM users WHERE id = ?", @request.user_id))
""")
```

## 🤖 Machine Learning Operations

### Learning and Prediction

```python
config = TSK.from_string("""
[ml]
# Learn from data
optimal_setting: @learn("optimal_setting", "default_value", @historical_data())

# Predict values
predicted_load: @predict("server_load", @current_metrics())
optimal_cache_size: @predict("cache_size", @usage_patterns())

# Optimize parameters
optimized_timeout: @optimize("timeout", 30, @performance_data())
optimal_workers: @optimize("workers", 4, @load_data())
""")
```

### Advanced ML Operations

```python
config = TSK.from_string("""
[advanced_ml]
# Train models
model_accuracy: @train("user_classifier", @training_data(), @validation_data())

# Feature importance
feature_importance: @analyze("user_behavior", @user_data())

# Anomaly detection
anomaly_score: @detect_anomaly("system_metrics", @current_metrics())
""")
```

## 📊 Metrics and Monitoring

### Basic Metrics

```python
config = TSK.from_string("""
[metrics]
# Record metrics
response_time: @metrics("response_time_ms", 150)
error_rate: @metrics("error_rate", 0.02)
user_count: @metrics("active_users", @query("SELECT COUNT(*) FROM users WHERE active = 1"))

# Increment counters
request_count: @metrics.increment("requests_total")
error_count: @metrics.increment("errors_total")
""")
```

### Advanced Metrics

```python
config = TSK.from_string("""
[advanced_metrics]
# Histograms
response_time_histogram: @metrics.histogram("response_time", 150, buckets=[10, 50, 100, 200, 500])

# Gauges
memory_usage: @metrics.gauge("memory_bytes", @memory_usage())
cpu_usage: @metrics.gauge("cpu_percent", @cpu_usage())

# Custom metrics
business_metric: @metrics.custom("orders_per_minute", @calculate_orders_per_minute())
""")
```

## 🔧 Conditional Logic

### Basic Conditionals

```python
config = TSK.from_string("""
[conditionals]
# Simple if/else
debug_mode: @if(@env("DEBUG") == "true", true, false)
port_number: @if(@env("ENVIRONMENT") == "production", 80, 8080)

# Multiple conditions
log_level: @if(@env("ENVIRONMENT") == "production", "error", 
               @if(@env("ENVIRONMENT") == "staging", "warn", "debug"))

# Boolean operations
feature_enabled: @and(@env("FEATURE_FLAG") == "true", @env("ENVIRONMENT") == "production")
can_access: @or(@user_role == "admin", @user_role == "moderator")
""")
```

### Advanced Conditionals

```python
config = TSK.from_string("""
[advanced_conditionals]
# Complex conditions
cache_duration: @if(@and(@env("ENVIRONMENT") == "production", @query("SELECT COUNT(*) FROM users") > 1000), "1h", "5m")

# Nested conditions
security_level: @if(@env("ENVIRONMENT") == "production",
                    @if(@query("SELECT COUNT(*) FROM failed_logins") > 10, "high", "medium"),
                    "low")

# Switch-like conditions
database_type: @switch(@env("DB_TYPE"),
                       "postgres", "postgresql",
                       "mysql", "mysql",
                       "sqlite", "sqlite",
                       "default")
""")
```

## 🔄 String Operations

### Basic String Operations

```python
config = TSK.from_string("""
[strings]
# String manipulation
uppercase_name: @string.upper(@env("USER_NAME"))
lowercase_email: @string.lower(@env("EMAIL"))
capitalized_title: @string.capitalize(@env("PAGE_TITLE"))

# String formatting
formatted_url: @string.format("https://api.{}.com/v1/{}", @env("DOMAIN"), @env("ENDPOINT"))
interpolated_path: @string.interpolate("/users/{}/profile", @request.user_id)
""")
```

### Advanced String Operations

```python
config = TSK.from_string("""
[advanced_strings]
# String validation
is_email: @string.is_email(@env("EMAIL"))
is_url: @string.is_url(@env("WEBSITE_URL"))
is_numeric: @string.is_numeric(@env("PORT"))

# String transformation
slugified_title: @string.slugify(@env("PAGE_TITLE"))
truncated_description: @string.truncate(@env("DESCRIPTION"), 100)
sanitized_input: @string.sanitize(@request.user_input)
""")
```

## 🔢 Mathematical Operations

### Basic Math

```python
config = TSK.from_string("""
[math]
# Basic arithmetic
total: @math.add(@query("SELECT amount FROM orders"), @query("SELECT amount FROM refunds"))
average: @math.divide(@query("SELECT SUM(amount) FROM orders"), @query("SELECT COUNT(*) FROM orders"))
percentage: @math.multiply(@math.divide(@active_users, @total_users), 100)

# Mathematical functions
rounded_value: @math.round(@query("SELECT AVG(amount) FROM orders"), 2)
ceiling_value: @math.ceil(@query("SELECT AVG(amount) FROM orders"))
floor_value: @math.floor(@query("SELECT AVG(amount) FROM orders"))
""")
```

### Advanced Math

```python
config = TSK.from_string("""
[advanced_math]
# Statistical functions
mean: @math.mean(@query("SELECT amount FROM orders"))
median: @math.median(@query("SELECT amount FROM orders"))
standard_deviation: @math.std(@query("SELECT amount FROM orders"))

# Random numbers
random_id: @math.random(1000, 9999)
random_choice: @math.choice(["option1", "option2", "option3"])
""")
```

## 🔐 Security Operations

### Encryption and Hashing

```python
config = TSK.from_string("""
[security]
# Encryption
encrypted_data: @encrypt(@sensitive_data, "AES-256-GCM")
decrypted_data: @decrypt(@encrypted_data, "AES-256-GCM")

# Hashing
password_hash: @hash(@password, "bcrypt")
data_hash: @hash(@data, "sha256")

# JWT operations
jwt_token: @jwt.encode(@payload, @env("JWT_SECRET"))
jwt_payload: @jwt.decode(@token, @env("JWT_SECRET"))
""")
```

### Validation and Sanitization

```python
config = TSK.from_string("""
[validation]
# Input validation
valid_email: @validate.email(@request.email)
valid_url: @validate.url(@request.website)
valid_age: @validate.range(@request.age, 0, 150)

# Data sanitization
sanitized_input: @sanitize.html(@request.user_input)
sanitized_sql: @sanitize.sql(@request.query)
""")
```

## 🚀 Custom @ Operators

### Creating Custom Operators

```python
from tsk import TSK

# Define custom operator
def custom_operator(value, *args):
    # Custom logic here
    return f"Custom: {value}"

# Register custom operator
TSK.register_operator("custom", custom_operator)

# Use custom operator
config = TSK.from_string("""
[test]
result: @custom("Hello, TuskLang!")
""")

result = config.parse()
print(result['test']['result'])  # Output: Custom: Hello, TuskLang!
```

### Advanced Custom Operators

```python
# Operator with multiple parameters
def advanced_operator(base_value, operation, *args):
    if operation == "multiply":
        return base_value * args[0]
    elif operation == "add":
        return base_value + args[0]
    elif operation == "format":
        return args[0].format(base_value)
    return base_value

TSK.register_operator("advanced", advanced_operator)

config = TSK.from_string("""
[test]
multiplied: @advanced(10, "multiply", 5)
added: @advanced(10, "add", 3)
formatted: @advanced("Hello", "format", "Message: {}")
""")
```

## 🛠️ Error Handling

### Operator Error Handling

```python
config = TSK.from_string("""
[error_handling]
# Safe environment variable access
api_key: @env("API_KEY", "default_key")

# Safe file operations
config_content: @file.read("config.json", fallback="{}")

# Safe HTTP requests
weather_data: @http("GET", "https://api.example.com/weather", fallback={"error": "Service unavailable"})

# Safe database queries
user_count: @query("SELECT COUNT(*) FROM users", fallback=0)
""")
```

### Custom Error Handling

```python
config = TSK.from_string("""
[advanced_error_handling]
# Try-catch style operations
safe_operation: @try(@expensive_operation(), @fallback_value)

# Conditional error handling
robust_query: @if(@query.exists("SELECT * FROM users"), 
                  @query("SELECT * FROM users"), 
                  [])
""")
```

## 🚀 Next Steps

Now that you understand @ operators:

1. **Explore Advanced Features** - [007-advanced-features-python.md](007-advanced-features-python.md)
2. **Learn FUJSEN Functions** - [004-fujsen-python.md](004-fujsen-python.md)
3. **Integrate Databases** - [005-database-integration-python.md](005-database-integration-python.md)

## 💡 Best Practices

1. **Use fallbacks** - Always provide default values for critical operations
2. **Cache expensive operations** - Use `@cache` for frequently accessed data
3. **Handle errors gracefully** - Use error handling operators
4. **Validate inputs** - Use validation operators for user input
5. **Optimize performance** - Cache and optimize expensive operations
6. **Secure sensitive data** - Use secure operators for sensitive information
7. **Monitor usage** - Use metrics operators to track performance

---

**"We don't bow to any king"** - @ operators give you the power to create truly dynamic, intelligent configuration that adapts and responds to your environment! 