# References in TuskLang

References in TuskLang allow you to link to other values, create aliases, and avoid duplication in your configuration. This guide covers how to use references effectively.

## Basic References

### Using the @ Symbol

```tusk
# Define a value
database_host: "localhost"

# Reference it elsewhere
connection_string: "postgresql://user@${@database_host}:5432/mydb"

# Direct reference
primary_host: @database_host

# Reference in objects
database:
    primary:
        host: @database_host
        port: 5432
```

### Reference Syntax Types

```tusk
# Direct reference
value1: @other_value

# Reference in interpolation
message: "Host is ${@server.host}"

# Reference with path
nested_value: @config.database.host

# Reference with brackets
dynamic_key: "host"
value: @config.database[dynamic_key]
```

## Object References

### Referencing Nested Values

```tusk
config:
    app:
        name: "MyApp"
        version: "1.0.0"
    server:
        host: "localhost"
        port: 8080

# Reference nested values
app_name: @config.app.name
server_url: "http://${@config.server.host}:${@config.server.port}"

# Deep nesting
deep_reference: @config.app.metadata.author.name
```

### Self References

```tusk
server:
    host: "localhost"
    port: 8080
    # Reference sibling properties
    url: "http://${@self.host}:${@self.port}"
    
    # Reference parent
    config_name: @parent.name
```

## Array References

### Referencing Array Elements

```tusk
servers: ["server1", "server2", "server3"]

# Reference by index
primary_server: @servers[0]
backup_server: @servers[1]

# Reference in loops
server_configs: @map(@servers, @lambda(server, {
    host: server
    port: 8080
    url: "http://${server}:8080"
}))
```

### Dynamic Array Access

```tusk
users: [
    { id: 1, name: "Alice" },
    { id: 2, name: "Bob" },
    { id: 3, name: "Charlie" }
]

# Find and reference
active_user_id: 2
active_user: @find(@users, @lambda(u, u.id == @active_user_id))
```

## Reference Patterns

### Configuration Inheritance

```tusk
# Base configuration
base_config:
    timeout: 30
    retries: 3
    log_level: "info"

# Environment-specific configs reference base
dev_config:
    ...@base_config     # Spread reference
    log_level: "debug"  # Override
    host: "localhost"

prod_config:
    ...@base_config
    host: "api.example.com"
    ssl: true
```

### Avoiding Duplication

```tusk
# Define once
api_version: "v1"
api_base: "/api/${@api_version}"

# Reference multiple times
endpoints:
    users: "${@api_base}/users"
    products: "${@api_base}/products"
    orders: "${@api_base}/orders"
    
    # Version-specific endpoints
    legacy:
        users: "/api/v0/users"
    current:
        users: @endpoints.users
```

### Computed References

```tusk
# Dynamic reference based on condition
environment: "production"

database:
    development:
        host: "localhost"
        name: "app_dev"
    production:
        host: "db.example.com"
        name: "app_prod"

# Computed reference
current_db: @database[@environment]
connection: "postgresql://${@current_db.host}/${@current_db.name}"
```

## Reference Scoping

### Local Scope

```tusk
global_value: "I'm global"

function_example: @lambda({
    local_value: "I'm local"
    
    # Can reference both
    message: "${@global_value} and ${local_value}"
    
    # Local shadows global if same name
    global_value: "I shadow the global"
    shadowed: @global_value  # References local version
})
```

### Module Scope

```tusk
# In user_module.tsk
module:
    name: "User Module"
    version: "1.0.0"
    
    api:
        get_user: @lambda(id, { /* ... */ })
        create_user: @lambda(data, { /* ... */ })

# In main.tsk
user_module: @import("user_module.tsk")

# Reference module contents
get_user_fn: @user_module.api.get_user
module_version: @user_module.version
```

## Circular Reference Prevention

### Detecting Circular References

```tusk
# This creates a circular reference - will error
a: @b
b: @c  
c: @a  # Error: Circular reference detected

# Safe pattern - use functions for lazy evaluation
get_a: @lambda({ @b() })
get_b: @lambda({ @c() })
get_c: @lambda({ "final value" })
```

### Breaking Reference Cycles

```tusk
# Use intermediate values
step1: "initial"
step2: "${@step1} -> second"
step3: "${@step2} -> third"
# No cycle, each depends only on previous

# Or use conditional references
node:
    value: "data"
    next: @if(@has_next, @nodes[@index + 1], null)
```

## Advanced Reference Techniques

### Reference Guards

```tusk
# Safe reference with fallback
value: @some.deep.path ?? "default"

# Check if reference exists
has_config: @exists(@config.optional_section)

# Conditional reference
db_host: @if(@env.DB_HOST, @env.DB_HOST, @config.database.host)
```

### Reference Transformation

```tusk
# Transform referenced value
raw_port: "8080"
numeric_port: @number(@raw_port)

# Chain transformations
user_input: "  HELLO WORLD  "
cleaned: @trim(@lower(@user_input))  # "hello world"

# Reference with method call
items: ["apple", "banana", "orange"]
items_string: @join(@items, ", ")
```

### Dynamic Reference Paths

```tusk
# Build reference path dynamically
get_config_value = @lambda(section, key, {
    path: "${section}.${key}"
    return: @resolve(path)  # Resolve string as reference path
})

# Usage
db_host: @get_config_value("database", "host")
```

## Reference Resolution

### Order of Resolution

```tusk
# TuskLang resolves references in order:
# 1. Local scope
# 2. Parent scopes (upward)
# 3. Global scope
# 4. Imported modules

value: "global"

section:
    value: "section"
    
    subsection:
        # References section-level value, not global
        referenced: @value  # "section"
```

### Lazy vs Eager Resolution

```tusk
# Eager resolution (immediate)
static_ref: @config.value

# Lazy resolution (on demand)
lazy_ref = @lambda({ @config.value })

# Useful for circular dependencies
a:
    value: 1
    get_b: @lambda({ @b.value })

b:
    value: 2
    get_a: @lambda({ @a.value })
```

## Common Reference Patterns

### Configuration Templates

```tusk
# Template with references
server_template:
    host: @hostname
    port: @port
    url: "http://${@self.host}:${@self.port}"
    health_check: "${@self.url}/health"

# Create instances
web_server:
    hostname: "web.example.com"
    port: 80
    ...@server_template

api_server:
    hostname: "api.example.com"  
    port: 8080
    ...@server_template
```

### Shared Constants

```tusk
# Define constants once
constants:
    MAX_RETRIES: 3
    TIMEOUT_MS: 30000
    API_VERSION: "v2"
    
# Reference throughout
http_client:
    retries: @constants.MAX_RETRIES
    timeout: @constants.TIMEOUT_MS
    base_url: "/api/${@constants.API_VERSION}"
```

### Reference Factories

```tusk
# Factory pattern with references
create_service = @lambda(name, port, {
    return: {
        name: name
        port: port
        host: @config.default_host
        url: "http://${@config.default_host}:${port}"
        health: "${@self.url}/health"
    }
})

# Create services
auth_service: @create_service("auth", 9001)
user_service: @create_service("users", 9002)
```

## Performance Considerations

### Reference Caching

```tusk
# References are resolved once and cached
expensive_value: @compute_expensive_operation()

# Multiple references don't recompute
ref1: @expensive_value  # Uses cached value
ref2: @expensive_value  # Uses cached value

# Force recomputation with function
get_fresh = @lambda({ @compute_expensive_operation() })
```

### Avoiding Deep References

```tusk
# Deep reference chains can be slow
deep: @a.b.c.d.e.f.g.h.i.j

# Better - store intermediate reference
intermediate: @a.b.c.d.e
shallow: @intermediate.f.g.h.i.j

# Or destructure
{ f: { g: { h: { i: { j: target }}}}} = @a.b.c.d.e
result: @target
```

## Best Practices

### 1. Use Meaningful Names

```tusk
# Bad - unclear references
x: @y.z

# Good - descriptive references  
user_email: @current_user.contact.email
```

### 2. Document Complex References

```tusk
# Reference to dynamically selected database based on environment
# @see database configuration section
active_db: @databases[@env.APP_ENV || "development"]
```

### 3. Validate References

```tusk
# Check references exist before use
safe_reference = @if(@exists(@optional.path), {
    value: @optional.path
}, {
    value: "default"
    warning: "Optional path not found, using default"
})
```

### 4. Group Related References

```tusk
# Group configuration references
refs:
    db_host: @config.database.host
    db_port: @config.database.port
    api_key: @secrets.api_key
    
# Use grouped references
connection: "postgresql://${@refs.db_host}:${@refs.db_port}"
```

## Common Mistakes

### Missing @ Symbol

```tusk
# Wrong - treats as literal string
value: other_value

# Right - creates reference
value: @other_value
```

### Reference to Non-Existent Path

```tusk
# This will error if path doesn't exist
value: @non.existent.path

# Safe approach
value: @non.existent.path ?? "fallback"
```

### Circular References

```tusk
# Avoid circular dependencies
# Wrong
a: @b
b: @a

# Right - break the cycle
a: @b.value
b: { value: "actual data" }
```

## Next Steps

- Learn about [Variable Naming](027-variable-naming.md)
- Explore [Reserved Keywords](028-reserved-keywords.md)  
- Master [Best Practices](030-best-practices.md) for references