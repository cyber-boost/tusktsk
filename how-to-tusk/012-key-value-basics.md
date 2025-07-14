# Key-Value Basics in TuskLang

At its core, TuskLang is built on key-value pairs, making configuration simple and intuitive. This guide covers the fundamentals of working with keys and values.

## Basic Syntax

The simplest TuskLang statement is a key-value pair:

```tusk
key: value
```

Examples:
```tusk
name: "TuskLang"
version: "1.0.0"
port: 8080
enabled: true
```

## Key Rules

### Valid Key Names

Keys follow these rules:
- Start with a letter or underscore
- Contain letters, numbers, underscores
- Case-sensitive
- No spaces (use underscores)

```tusk
# Valid keys
user_name: "John"
firstName: "Jane"
_private: true
api2_key: "secret"
PORT: 8080

# Invalid keys (won't parse)
# 2factor: true        # Can't start with number
# user-name: "John"    # Hyphens not allowed
# user name: "John"    # Spaces not allowed
```

### Reserved Keywords

Avoid using these reserved words as keys:
- `true`, `false`, `null`
- `if`, `else`, `return`
- Built-in function names

```tusk
# Avoid this
# true: "value"  # Reserved word

# Use this instead
is_true: "value"
true_value: "value"
```

## Value Types

TuskLang automatically infers value types:

```tusk
# String values
name: "TuskLang"
message: 'Hello, World!'
unquoted: Hello    # Also a string

# Numeric values
integer: 42
float: 3.14
negative: -10
scientific: 1.5e10

# Boolean values
enabled: true
disabled: false

# Null value
optional: null
```

## Simple Objects

Create nested structures with indentation:

```tusk
user:
    name: "John Doe"
    age: 30
    active: true
```

## Lists and Arrays

Define arrays using square brackets:

```tusk
# Inline array
colors: ["red", "green", "blue"]

# Multiline array
fruits: [
    "apple",
    "banana",
    "orange"
]

# Array of objects
users: [
    { name: "Alice", age: 30 },
    { name: "Bob", age: 25 }
]
```

## Key Naming Conventions

### Snake Case (Recommended)

```tusk
# Preferred for most keys
user_name: "John"
api_key: "secret"
max_retry_count: 3
database_url: "postgres://..."
```

### Camel Case

```tusk
# Common for JavaScript-style configs
userName: "John"
apiKey: "secret"
maxRetryCount: 3
databaseUrl: "postgres://..."
```

### Pascal Case

```tusk
# Sometimes used for type-like structures
UserModel:
    Name: "string"
    Age: "number"
    Active: "boolean"
```

### Constant Case

```tusk
# For constants and environment mappings
MAX_CONNECTIONS: 100
API_VERSION: "v1"
DEBUG_MODE: false
```

## Accessing Values

### Direct Access

```tusk
# Define values
config:
    app_name: "MyApp"
    version: "1.0.0"

# Access in same file
title = "${config.app_name} ${config.version}"
```

### Nested Access

```tusk
# Deep nesting
database:
    primary:
        host: "db1.example.com"
        port: 5432
    replica:
        host: "db2.example.com"
        port: 5432

# Access nested values
primary_host: @database.primary.host
replica_url = "postgres://${database.replica.host}:${database.replica.port}"
```

## Value Modification

### Overriding Values

```tusk
# Initial value
port: 8080

# Override later in file
port: 3000  # This wins

# Conditional override
port: @env.PORT || 8080
```

### Merging Objects

```tusk
# Base configuration
defaults:
    timeout: 30
    retries: 3
    debug: false

# Environment overrides
overrides:
    debug: true
    retries: 5

# Merged configuration
config = @merge(defaults, overrides)
# Result: { timeout: 30, retries: 5, debug: true }
```

## Special Keys

### Metadata Keys

```tusk
# These keys have special meaning in some contexts
_version: "1.0.0"      # Internal version
_schema: "config/v1"   # Schema identifier
_comment: "Generated file, do not edit"
```

### System Keys

```tusk
# Keys starting with @ are typically computed
@timestamp: @request.timestamp
@computed = someValue + 10
```

## Key Patterns

### Grouping Related Keys

```tusk
# Group by prefix
db_host: "localhost"
db_port: 5432
db_name: "myapp"
db_user: "admin"

# Or use nested object
database:
    host: "localhost"
    port: 5432
    name: "myapp"
    user: "admin"
```

### Feature Flags

```tusk
features:
    new_ui: true
    beta_api: false
    analytics: true
    debug_mode: @env.DEBUG == "true"
```

### Configuration Variants

```tusk
# Environment-specific configs
development:
    api_url: "http://localhost:3000"
    debug: true

staging:
    api_url: "https://staging.example.com"
    debug: true

production:
    api_url: "https://api.example.com"
    debug: false
```

## Dynamic Keys

### Computed Key Names

```tusk
# Using template literals for keys
env_name: "production"
"config_${env_name}":
    api_url: "https://api.example.com"
    
# Results in: config_production
```

### Conditional Keys

```tusk
base_config:
    always_present: true

# Add keys conditionally
@if(@env.ENABLE_CACHE, {
    base_config.cache_enabled: true
    base_config.cache_ttl: 3600
})
```

## Value Validation

### Type Checking

```tusk
# Ensure correct types
port: @assert.number(@env.PORT || 8080)
name: @assert.string(@env.APP_NAME || "MyApp")
debug: @assert.boolean(@env.DEBUG || false)
```

### Range Validation

```tusk
# Validate numeric ranges
port = @env.PORT || 8080
@assert(port >= 1024 && port <= 65535, "Port must be between 1024 and 65535")

# Validate string patterns
email: @env.ADMIN_EMAIL
@assert(@regex.match(email, "^[^@]+@[^@]+$"), "Invalid email format")
```

## Common Patterns

### Configuration with Defaults

```tusk
# Define defaults
defaults:
    host: "localhost"
    port: 8080
    timeout: 30
    retries: 3

# Override with environment
config:
    host: @env.HOST || @defaults.host
    port: @env.PORT || @defaults.port
    timeout: @env.TIMEOUT || @defaults.timeout
    retries: @env.RETRIES || @defaults.retries
```

### Building Connection Strings

```tusk
# Database components
db:
    driver: "postgresql"
    user: "admin"
    password: @env.DB_PASSWORD
    host: "localhost"
    port: 5432
    name: "myapp"

# Build connection string
connection_string = "${db.driver}://${db.user}:${db.password}@${db.host}:${db.port}/${db.name}"
```

### API Endpoints

```tusk
# API configuration
api:
    base_url: "https://api.example.com"
    version: "v1"
    
    endpoints:
        users: "${base_url}/${version}/users"
        posts: "${base_url}/${version}/posts"
        auth: "${base_url}/${version}/auth"
```

## Best Practices

### 1. Use Descriptive Keys

```tusk
# Bad
t: 30
mc: 100

# Good
timeout_seconds: 30
max_connections: 100
```

### 2. Group Related Data

```tusk
# Bad - scattered
user_name: "John"
user_email: "john@example.com"
user_role: "admin"

# Good - grouped
user:
    name: "John"
    email: "john@example.com"
    role: "admin"
```

### 3. Be Consistent

```tusk
# Pick one style and stick to it
database_config:  # If using snake_case
    connection_timeout: 30
    max_pool_size: 10
    # Not: maxPoolSize or ConnectionTimeout
```

### 4. Document Complex Values

```tusk
# Maximum time to wait for a response (in seconds)
# Default: 30s, Min: 1s, Max: 300s
timeout: 30

# Number of worker threads
# Set to 0 for auto-detection based on CPU cores
workers: 0
```

### 5. Use Meaningful Defaults

```tusk
# Provide sensible defaults
config:
    port: @env.PORT || 8080              # Common default port
    host: @env.HOST || "0.0.0.0"        # Listen on all interfaces
    env: @env.NODE_ENV || "development"  # Safe default
```

## Common Mistakes

### Forgetting Quotes

```tusk
# Wrong - might parse incorrectly
message: Hello World  # Might only capture "Hello"

# Right
message: "Hello World"
```

### Inconsistent Nesting

```tusk
# Wrong - mixed nesting
config:
api_key: "secret"
    port: 8080

# Right - consistent nesting
config:
    api_key: "secret"
    port: 8080
```

### Circular References

```tusk
# This creates a circular reference
a: @b
b: @a  # Error: circular reference
```

## Migration Examples

### From JSON

```json
// Before (JSON)
{
  "name": "MyApp",
  "config": {
    "port": 8080,
    "debug": true
  }
}
```

```tusk
# After (TuskLang)
name: "MyApp"
config:
    port: 8080
    debug: true
```

### From YAML

```yaml
# Before (YAML)
database:
  host: localhost
  credentials:
    username: admin
    password: secret
```

```tusk
# After (TuskLang)
database:
    host: "localhost"
    credentials:
        username: "admin"
        password: "secret"
```

## Next Steps

- Understand [Colon vs Equals](013-colon-vs-equals.md) operators
- Learn about [String handling](014-strings.md)
- Explore [Complex data structures](019-nested-objects.md)