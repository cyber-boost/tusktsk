# Colon vs Equals: Understanding TuskLang Assignment Operators

TuskLang features two assignment operators that serve different purposes: `:` for static assignment and `=` for dynamic assignment. Understanding when to use each is crucial for writing effective TuskLang code.

## The Colon (:) Operator - Static Assignment

The colon operator assigns static, literal values that are determined at parse time.

### Basic Usage

```tusk
# Static string assignment
name: "TuskLang"

# Static number assignment  
port: 8080

# Static boolean assignment
debug: true

# Static object assignment
config:
    host: "localhost"
    timeout: 30
```

### Characteristics of Colon Assignment

1. **Parse-time evaluation** - Values are set when the file is parsed
2. **Literal values only** - Cannot contain expressions or function calls
3. **Immutable intent** - Suggests the value won't change
4. **Configuration-friendly** - Perfect for static settings

### When to Use Colon

```tusk
# Application metadata - won't change at runtime
app:
    name: "MyApplication"
    version: "1.0.0"
    author: "Jane Doe"

# Static configuration
database:
    driver: "postgresql"
    charset: "utf8"
    
# Constant values
constants:
    MAX_UPLOAD_SIZE: 10485760  # 10MB
    API_VERSION: "v1"
    DEFAULT_TIMEOUT: 30
```

## The Equals (=) Operator - Dynamic Assignment

The equals operator assigns values that are computed at runtime, including expressions and function calls.

### Basic Usage

```tusk
# Dynamic expression
total = price + tax

# Function call result
timestamp = @time.now()

# Conditional assignment
port = @env.PORT || 8080

# Computed value
greeting = "Hello, ${user_name}!"
```

### Characteristics of Equals Assignment

1. **Runtime evaluation** - Values computed when accessed
2. **Expressions allowed** - Can include calculations and function calls
3. **Dynamic nature** - Values can depend on runtime state
4. **Re-evaluation** - May produce different results each time

### When to Use Equals

```tusk
# Environment-dependent values
api_url = @env.API_URL || "http://localhost:3000"

# Computed values
full_name = "${first_name} ${last_name}"

# Conditional logic
theme = @if(user_preference == "dark", "dark-theme", "light-theme")

# Function results
current_user = @auth.get_user()
```

## Side-by-Side Comparison

```tusk
# Static (colon) - Always "Hello, World!"
static_greeting: "Hello, World!"

# Dynamic (equals) - Changes based on time
dynamic_greeting = @if(@time.hour < 12, "Good morning!", "Good afternoon!")

# Static object
server_config:
    host: "localhost"
    port: 8080

# Dynamic object
runtime_config = {
    host: @env.HOST || "localhost"
    port: @env.PORT || 8080
    ssl: @env.NODE_ENV == "production"
}
```

## Common Patterns

### Configuration with Overrides

```tusk
# Static defaults
defaults:
    host: "localhost"
    port: 8080
    timeout: 30

# Dynamic overrides
config = @merge(defaults, {
    host: @env.HOST || @defaults.host
    port: @env.PORT || @defaults.port
})
```

### Building Complex Values

```tusk
# Static components
protocol: "https"
domain: "api.example.com"
version: "v1"

# Dynamic composition
base_url = "${protocol}://${domain}/${version}"
```

### Conditional Configuration

```tusk
# Static environment name
environment: "production"

# Dynamic configuration based on environment
debug_mode = @environment != "production"
log_level = @if(@environment == "production", "error", "debug")
```

## Mixed Usage in Objects

You can mix both operators within objects:

```tusk
user:
    # Static properties
    id: "user123"
    type: "standard"
    
    # Dynamic properties
    created_at = @time.now()
    session_token = @uuid.generate()
    is_admin = @roles.includes("admin")
```

## Function Definitions

Both operators work with function definitions:

```tusk
# Static function assignment (preferred for lambdas)
calculate_tax: @lambda(price, rate, {
    return: price * rate
})

# Dynamic function assignment (when needed)
get_handler = @if(@env.USE_CACHE, @cached_handler, @direct_handler)
```

## Arrays and Dynamic Values

```tusk
# Static array
colors: ["red", "green", "blue"]

# Dynamic array
active_users = @users.filter(@lambda(user, user.active))

# Mixed approach
menu_items: [
    { name: "Home", url: "/" },
    { name: "Dashboard", url = @auth.dashboard_url() },
    { name: "Profile", url = "/users/${@auth.user_id}" }
]
```

## Re-evaluation Behavior

Understanding when values are re-evaluated is crucial:

```tusk
# Static - evaluated once at parse time
static_time: "2024-01-01 00:00:00"

# Dynamic - MAY be re-evaluated
current_time = @time.now()

# Force single evaluation with caching
cached_time = @cache.get_or_set("app_start_time", @time.now())
```

## Performance Considerations

### Static is Faster

```tusk
# Faster - no runtime computation
config:
    max_connections: 100
    timeout: 30

# Slower - requires runtime evaluation
config = {
    max_connections: @env.MAX_CONN || 100
    timeout: @env.TIMEOUT || 30
}
```

### Use Static When Possible

```tusk
# Unnecessary dynamic assignment
message = "Welcome to TuskLang"  # Could be static

# Better
message: "Welcome to TuskLang"

# Dynamic when needed
personalized_message = "Welcome, ${user_name}!"
```

## Error Handling

### Static Errors (Parse Time)

```tusk
# This fails at parse time
# invalid: "unterminated string

# This is caught immediately
port: "not a number"  # Type mismatch warning
```

### Dynamic Errors (Runtime)

```tusk
# This might fail at runtime
result = @database.query("SELECT * FROM users")

# Better with error handling
result = @try({
    data: @database.query("SELECT * FROM users")
}, {
    data: []
    error: "Database query failed"
})
```

## Best Practices

### 1. Default to Static

Use `:` unless you specifically need dynamic behavior:

```tusk
# Good - static for fixed values
app_name: "MyApp"
max_retries: 3

# Only use = when necessary
current_time = @time.now()
```

### 2. Be Consistent

Don't mix operators arbitrarily:

```tusk
# Confusing
server:
    host: "localhost"
    port = 8080  # Why dynamic here?

# Clear
server:
    host: "localhost"
    port: 8080
```

### 3. Document Dynamic Behavior

```tusk
# This value changes based on server load
worker_count = @system.cpu_count * 2

# Cached for 5 minutes
user_count = @cache("user_count", 300, @database.count("users"))
```

### 4. Group by Type

```tusk
# Static configuration
constants:
    APP_NAME: "MyApp"
    VERSION: "1.0.0"
    MAX_FILE_SIZE: 10485760

# Dynamic runtime values
runtime = {
    start_time: @time.now()
    memory_usage: @system.memory.used
    active_connections: @server.connections.length
}
```

## Common Mistakes

### Using = for Static Values

```tusk
# Wrong - unnecessary dynamic assignment
name = "John Doe"

# Right - use static assignment
name: "John Doe"
```

### Using : for Dynamic Values

```tusk
# Wrong - won't work
timestamp: @time.now()  # Error: @ not allowed with :

# Right - use dynamic assignment
timestamp = @time.now()
```

### Forgetting Runtime Context

```tusk
# This might not work as expected
config:
    # This is evaluated at parse time, not when accessed
    token: @env.API_TOKEN  # Error if @ operators not allowed

# Better
config:
    token = @env.API_TOKEN  # Evaluated when accessed
```

## Advanced Patterns

### Lazy Evaluation

```tusk
# Define expensive operations with =
expensive_data = @database.fetch_all_records()

# Better - use a function for lazy evaluation
get_expensive_data = @lambda({
    @cache.get_or_set("expensive_data", 3600, {
        @database.fetch_all_records()
    })
})
```

### Configuration Builders

```tusk
# Static base
base_config:
    app: "MyApp"
    version: "1.0.0"

# Dynamic builder
build_config = @lambda(env, {
    config = @merge(@base_config, {
        environment: env
        debug: env != "production"
        api_url: @if(env == "production",
            "https://api.example.com",
            "http://localhost:3000"
        )
    })
    return: config
})

# Use the builder
dev_config = @build_config("development")
prod_config = @build_config("production")
```

## Summary

- Use `:` for static, literal values that won't change
- Use `=` for dynamic values, expressions, and function calls
- Static assignment is faster and clearer for configuration
- Dynamic assignment enables runtime flexibility
- Choose based on whether the value needs runtime evaluation

## Next Steps

- Learn about [String handling](014-strings.md)
- Explore [Numbers](015-numbers.md) in TuskLang
- Understand [Boolean values](016-booleans.md)