# @ Variable Reference

The @ operator allows you to reference variables and values dynamically in TuskLang. This guide covers how to use @ for variable references, property access, and value resolution.

## Basic Variable References

### Simple References

```tusk
# Define a variable
app_name: "MyApplication"

# Reference it with @
title = "@{app_name} Dashboard"  # Direct reference
welcome = "Welcome to ${@app_name}"  # In string interpolation

# Reference equals the value
name_copy = @app_name  # "MyApplication"
```

### Reference vs Value

```tusk
# Understanding the difference
original: "Hello"

# These are different:
copy_value: "Hello"     # Literal string
copy_reference = @original  # Reference to original

# If original changes (in dynamic contexts):
original = "Goodbye"
# copy_value is still "Hello"
# copy_reference reflects the new value
```

## Property Access

### Dot Notation

```tusk
# Access nested properties
config:
    app:
        name: "MyApp"
        version: "1.0.0"
    server:
        host: "localhost"
        port: 8080

# Reference nested values
app_name = @config.app.name
server_url = "http://${@config.server.host}:${@config.server.port}"

# Deep nesting
deep_value = @config.app.metadata.author.email
```

### Bracket Notation

```tusk
# Dynamic property access
users:
    alice: { age: 30, role: "admin" }
    bob: { age: 25, role: "user" }

username: "alice"
user_data = @users[@username]  # Dynamic key

# With expressions
index: 0
first_item = @array[@index]

# Computed property names
property: "age"
alice_age = @users.alice[@property]  # 30
```

## Array References

### Index Access

```tusk
# Array references
colors: ["red", "green", "blue", "yellow"]

# By index
primary_color = @colors[0]    # "red"
secondary_color = @colors[1]  # "green"

# Negative indices
last_color = @colors[-1]      # "yellow"
second_last = @colors[-2]     # "blue"

# Dynamic index
index: 2
selected_color = @colors[@index]  # "blue"
```

### Array Methods with @

```tusk
numbers: [1, 2, 3, 4, 5]

# Reference in operations
doubled = @map(@numbers, @lambda(n, n * 2))
sum = @reduce(@numbers, @lambda(a, b, a + b), 0)

# Length reference
count = @numbers.length
last_index = @numbers.length - 1
```

## Self and Parent References

### Using @self

```tusk
# Reference current object
server:
    host: "api.example.com"
    port: 443
    protocol: "https"
    url = "${@self.protocol}://${@self.host}:${@self.port}"
    health_endpoint = "${@self.url}/health"

# In nested contexts
user:
    first_name: "John"
    last_name: "Doe"
    profile:
        full_name = "${@parent.first_name} ${@parent.last_name}"
        display_name = @self.full_name
```

### Parent References

```tusk
# Access parent scope
app:
    name: "MyApp"
    modules:
        auth:
            module_name: "Authentication"
            app_name = @parent.parent.name  # "MyApp"
            full_name = "${@parent.parent.name} - ${@self.module_name}"
```

## Scope Resolution

### Local vs Global

```tusk
# Global scope
global_value: "I'm global"

function_example = @lambda({
    # Local scope
    local_value: "I'm local"
    global_value: "I shadow global"  # Shadows global
    
    # Access local (shadowed)
    local_ref = @global_value  # "I shadow global"
    
    # Access global explicitly
    global_ref = @root.global_value  # "I'm global"
})
```

### Module Scope

```tusk
# In module.tsk
module_data:
    version: "1.0.0"
    api_key: "secret"

export:
    get_version: @lambda({ return: @module_data.version })
    get_key: @lambda({ return: @module_data.api_key })

# In main.tsk
mod = @import("module.tsk")
version = @mod.get_version()  # Accesses module's scope
```

## Optional Chaining

### Safe Property Access

```tusk
# Potentially null/undefined values
user: {
    name: "John"
    # address might be missing
}

# Without optional chaining (risky)
# city = @user.address.city  # Error if address is null!

# With optional chaining (safe)
city = @user?.address?.city  # Returns null if any part is missing

# With default value
city = @user?.address?.city ?? "Unknown City"
```

### Array Optional Chaining

```tusk
# Safe array access
data: {
    users: null  # Might be null
}

# Safe access
first_user = @data?.users?.[0]
first_user_name = @data?.users?.[0]?.name ?? "No user"

# Chaining method calls
uppercase_name = @data?.users?.[0]?.name?.toUpperCase?.()
```

## Dynamic References

### Building Reference Paths

```tusk
# Dynamic path construction
get_config_value = @lambda(section, key, {
    # Build path dynamically
    path: "${section}.${key}"
    
    # Resolve path to value
    return: @resolve_path(@config, path)
})

# Usage
db_host = @get_config_value("database", "host")
api_key = @get_config_value("api", "key")
```

### Reference Resolution

```tusk
# Resolve string paths to values
resolve_path = @lambda(obj, path, {
    parts = @split(path, ".")
    result = obj
    
    @each(parts, @lambda(part, {
        result = result?.[part]
    }))
    
    return: result
})

# Example usage
config: {
    database: {
        primary: {
            host: "db1.example.com"
        }
    }
}

host = @resolve_path(@config, "database.primary.host")
```

## Reference Patterns

### Configuration References

```tusk
# Base configuration with references
defaults:
    timeout: 30
    retries: 3
    base_url: "https://api.example.com"

# Service configurations reference defaults
service_a:
    timeout = @defaults.timeout
    retries = @defaults.retries
    url = "${@defaults.base_url}/service-a"

service_b:
    timeout = @defaults.timeout * 2  # Custom timeout
    retries = @defaults.retries
    url = "${@defaults.base_url}/service-b"
```

### Computed References

```tusk
# References in computed properties
pricing:
    base_price: 100
    tax_rate: 0.08
    
    # Computed using references
    tax_amount = @self.base_price * @self.tax_rate
    total_price = @self.base_price + @self.tax_amount
    
    # Format for display
    display_price = "$${@self.total_price.toFixed(2)}"
```

### Circular Reference Prevention

```tusk
# This would cause an error:
# a = @b
# b = @a  # Circular reference!

# Safe patterns:

# 1. Use functions for lazy evaluation
node_a:
    value: "A"
    get_next: @lambda({ return: @node_b })

node_b:
    value: "B"
    get_next: @lambda({ return: @node_a })

# 2. Use conditional references
config:
    use_default: true
    default_value: 100
    custom_value: 200
    active_value = @self.use_default ? @self.default_value : @self.custom_value
```

## Performance Optimization

### Reference Caching

```tusk
# References are resolved when accessed
expensive_data = @compute_expensive_operation()

# Multiple references don't recompute
ref1 = @expensive_data  # Uses cached value
ref2 = @expensive_data  # Uses same cached value

# But function calls re-execute
data1 = @compute_expensive_operation()  # Runs
data2 = @compute_expensive_operation()  # Runs again!
```

### Efficient Reference Patterns

```tusk
# Inefficient: Multiple deep references
user_name = @app.data.users[0].profile.name
user_email = @app.data.users[0].profile.email
user_phone = @app.data.users[0].profile.phone

# Efficient: Store intermediate reference
user_profile = @app.data.users[0].profile
user_name = @user_profile.name
user_email = @user_profile.email
user_phone = @user_profile.phone
```

## Error Handling

### Reference Validation

```tusk
# Check if reference exists
safe_reference = @lambda(path, default_value, {
    try_result = @try({
        return: @resolve_path(@root, path)
    }, {
        return: default_value
    })
    
    return: try_result
})

# Usage
value = @safe_reference("config.missing.path", "default")
```

### Debugging References

```tusk
# Debug helper for references
debug_reference = @lambda(ref_path, {
    @console.log("Attempting to resolve: ${ref_path}")
    
    result = @try({
        value: @resolve_path(@root, ref_path)
        @console.log("Success: ${@json.stringify(value)}")
        return: value
    }, {
        error: @catch
        @console.error("Failed: ${error}")
        return: null
    })
})
```

## Best Practices

### 1. Use Clear Reference Names

```tusk
# Good: Clear what's being referenced
user_email = @current_user.email
api_endpoint = @config.api.endpoint

# Bad: Ambiguous references
e = @u.e
url = @c.a.e
```

### 2. Handle Missing References

```tusk
# Always consider null/undefined
name = @user?.name ?? "Anonymous"
settings = @config?.user_settings ?? @default_settings

# Check existence before use
@if(@config?.database?.host, {
    connect_to_database(@config.database.host)
})
```

### 3. Document Complex References

```tusk
# Retrieves user permissions from cache or database
# Falls back to empty array if user not found
permissions = @cache.get("perms_${@user.id}") ?? 
              @db.get_permissions(@user.id) ?? 
              []
```

### 4. Avoid Deep Nesting

```tusk
# Hard to read and maintain
value = @app.modules.user.services.auth.providers.oauth.google.client_id

# Better: Break it down
auth_providers = @app.modules.user.services.auth.providers
google_client_id = @auth_providers.oauth.google.client_id
```

## Common Patterns

### Default Values Chain

```tusk
# Priority-based configuration
port = @env.PORT ??              # 1. Environment variable
       @config.server.port ??     # 2. Config file
       @defaults.port ??          # 3. Default config
       8080                       # 4. Hardcoded fallback
```

### Reference Factories

```tusk
# Create references dynamically
create_service_ref = @lambda(service_name, {
    return: {
        config: @config.services[@service_name]
        status: @status.services[@service_name]
        metrics: @metrics.services[@service_name]
    }
})

# Usage
auth_service = @create_service_ref("auth")
auth_config = @auth_service.config
```

## Next Steps

- Learn about [Variable Fallbacks](033-at-variable-fallback.md)
- Explore [Request Object](034-at-request-object.md) references
- Master [Object Navigation](045-at-tusk-object.md)