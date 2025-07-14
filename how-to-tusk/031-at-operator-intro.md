# Introduction to @ Operators

The @ operator is one of TuskLang's most powerful features, providing runtime functionality and dynamic behavior. This guide introduces the @ operator system and its capabilities.

## What is the @ Operator?

The @ operator signifies runtime operations in TuskLang. When you see `@`, you know that something dynamic is happening - whether it's accessing environment variables, calling functions, or performing computations.

```tusk
# Static value (no @)
static_port: 8080

# Dynamic value (with @)
dynamic_port = @env.PORT || 8080

# The @ indicates runtime evaluation
```

## Why @ Operators?

### Visual Distinction

```tusk
# Immediately clear what's static vs dynamic
config:
    name: "MyApp"              # Static - set at parse time
    version: "1.0.0"          # Static
    port = @env.PORT || 3000  # Dynamic - evaluated at runtime
    timestamp = @time.now()    # Dynamic - changes each run
```

### Consistent Syntax

```tusk
# All runtime operations use @ prefix
environment = @env.NODE_ENV
current_time = @time.now()
user_data = @database.query("SELECT * FROM users")
json_output = @json.stringify(data)

# Easy to spot dynamic operations at a glance
```

## Basic @ Operator Usage

### Environment Access

```tusk
# Access environment variables
api_key = @env.API_KEY
database_url = @env.DATABASE_URL || "postgresql://localhost/dev"

# Check if environment variable exists
has_key = @env.API_KEY != null
```

### Function Calls

```tusk
# Built-in functions
random_number = @random()
uuid = @uuid.generate()
timestamp = @time.now()

# User-defined functions
result = @calculate_total(items)
user = @fetch_user(id)
```

### Property Access

```tusk
# Access nested properties dynamically
request_method = @request.method
user_agent = @request.headers.user_agent
query_param = @request.query.page

# Safe navigation
city = @user?.address?.city || "Unknown"
```

## @ Operator Categories

### 1. Variable References

```tusk
# Reference other variables
base_url: "https://api.example.com"
endpoint = "${@base_url}/users"

# Reference with paths
database_host = @config.database.host
```

### 2. System Functions

```tusk
# Time operations
now = @time.now()
formatted = @time.format(now, "YYYY-MM-DD")

# String operations
uppercase = @upper("hello")
trimmed = @trim("  spaced  ")

# Array operations
mapped = @map(array, @lambda(x, x * 2))
filtered = @filter(array, @lambda(x, x > 10))
```

### 3. I/O Operations

```tusk
# File operations
content = @file.read("config.json")
@file.write("output.txt", data)

# HTTP operations
response = @http.get("https://api.example.com/data")
result = @http.post(url, { body: data })

# Database operations
users = @db.query("SELECT * FROM users")
```

### 4. Control Flow

```tusk
# Conditional execution
result = @if(condition, true_value, false_value)

# Error handling
safe_result = @try({
    return: @risky_operation()
}, {
    return: "default value"
})

# Loops
@each(items, @lambda(item, {
    @process(item)
}))
```

## @ Operator Rules

### Assignment Requirements

```tusk
# @ operators require = (not :)
# Wrong:
dynamic: @env.PORT  # Error!

# Correct:
dynamic = @env.PORT

# This is because @ operations happen at runtime
```

### Chaining

```tusk
# @ operators can be chained
result = @trim(@upper(@env.APP_NAME || "default"))

# With property access
user_city = @users[@index].address.city

# With method calls
formatted = @time.format(@time.now(), "HH:mm:ss")
```

### Context Sensitivity

```tusk
# @ operators may behave differently based on context

# In a web server context
client_ip = @request.ip  # Gets current request's IP

# In a CLI context  
args = @argv  # Gets command line arguments

# Context determines available operators
```

## Common @ Operator Patterns

### Default Values

```tusk
# Use || for fallbacks
port = @env.PORT || 8080
name = @config.app_name || "DefaultApp"
timeout = @settings.timeout || 30000
```

### Computed Properties

```tusk
server:
    host: "localhost"
    port = @env.PORT || 3000
    url = "http://${@self.host}:${@self.port}"
    health_check = "${@self.url}/health"
```

### Dynamic Configuration

```tusk
# Load config based on environment
environment = @env.NODE_ENV || "development"
config = @import("./config/${@environment}.tsk")

# Conditional features
features:
    debug = @environment != "production"
    analytics = @env.ANALYTICS_ENABLED == "true"
```

### Template Processing

```tusk
# Process templates with @ operators
email_template: """
Hello ${@user.name},

Your account was created at ${@time.format(@user.created_at, "MMMM DD, YYYY")}.

Best regards,
${@config.company_name}
"""

# Send with processed template
@email.send({
    to: @user.email
    subject: "Welcome!"
    body: @email_template
})
```

## @ Operator Precedence

```tusk
# @ operators follow standard precedence rules
result = @a + @b * @c  # Multiplication first
result = (@a + @b) * @c  # Parentheses override

# Property access is left-to-right
value = @config.database.primary.host

# Function calls have high precedence
result = @calculate(x) + @calculate(y)
```

## Performance Considerations

### Caching Results

```tusk
# @ operators execute each time they're accessed
# This runs the query twice:
users1 = @db.query("SELECT * FROM users")
users2 = @db.query("SELECT * FROM users")  # Runs again!

# Better: Cache the result
users = @db.query("SELECT * FROM users")
users1 = @users
users2 = @users  # Uses cached result
```

### Lazy Evaluation

```tusk
# Use functions for lazy evaluation
get_expensive_data = @lambda({
    return: @db.complex_query()
})

# Only runs when called
@if(need_data, {
    data = @get_expensive_data()
})
```

## Error Handling with @

### Safe Navigation

```tusk
# Use ?. for safe property access
value = @deeply?.nested?.property?.value

# Equivalent to multiple null checks
value = @deeply && @deeply.nested && @deeply.nested.property && @deeply.nested.property.value
```

### Try-Catch Pattern

```tusk
# Wrap risky @ operations
result = @try({
    data: @http.get(url)
    return: @json.parse(data.body)
}, {
    error: @catch
    @log.error("Failed to fetch data: ${error}")
    return: null
})
```

## @ Operator Best Practices

### 1. Use Meaningful Names

```tusk
# Good
user_email = @current_user.email
request_timestamp = @request.timestamp

# Bad
e = @u.e
t = @r.t
```

### 2. Handle Null Values

```tusk
# Always consider null possibilities
username = @user?.name || "Anonymous"
port = @env.PORT || 3000

# Check before using
@if(@database.connected, {
    users = @database.query("SELECT * FROM users")
})
```

### 3. Document Dynamic Behavior

```tusk
# Server port from environment or default to 3000
# Updates on server restart if env changes
port = @env.PORT || 3000

# Cache user permissions for current request
# Refreshed on each new request
permissions = @cache.request("user_perms", {
    @db.get_user_permissions(@user.id)
})
```

## Available @ Operators Overview

Here's a quick reference of major @ operator categories:

- **Variables**: `@variable_name`, `@path.to.value`
- **Environment**: `@env.VARIABLE`
- **Request**: `@request.method`, `@request.body`, `@request.query`
- **Time**: `@time.now()`, `@time.format()`
- **String**: `@upper()`, `@lower()`, `@trim()`
- **Array**: `@map()`, `@filter()`, `@reduce()`
- **Object**: `@keys()`, `@values()`, `@merge()`
- **I/O**: `@file.read()`, `@http.get()`, `@db.query()`
- **JSON**: `@json.parse()`, `@json.stringify()`
- **Control**: `@if()`, `@try()`, `@each()`

## Next Steps

Now that you understand the basics of @ operators, explore specific operators:

- [Variable References](032-at-variable-reference.md) - Using @ to reference values
- [Request Object](034-at-request-object.md) - Web request handling
- [Built-in Functions](041-at-json-function.md) - JSON and data manipulation
- [Database Operations](044-at-query-database.md) - Data persistence

The @ operator system is designed to make dynamic operations clear and consistent throughout your TuskLang code.