# 🏷️ TuskLang Bash Typed Values Guide

**"We don't bow to any king" – Type safety in configuration, the TuskLang way.**

Typed values in TuskLang provide type safety, validation, and explicit data type handling. Whether you're ensuring data integrity, preventing runtime errors, or building robust configurations, typed values give you the confidence that your data is exactly what you expect.

## 🎯 What are Typed Values?
Typed values explicitly declare the data type of a value. TuskLang supports:
- **@int()** - Integer values
- **@float()** - Floating-point numbers
- **@bool()** - Boolean values
- **@string()** - String values
- **@array()** - Array values
- **@object()** - Object values

## 📝 Syntax Styles

### Basic Type Declarations
```ini
[typed]
port: @int(8080)
timeout: @float(30.5)
debug: @bool(true)
name: @string("TuskApp")
servers: @array(["web1", "web2"])
config: @object({"host": "localhost", "port": 5432})
```

### JSON-like Typed Values
```json
{
  "server": {
    "port": "@int(8080)",
    "timeout": "@float(30.5)",
    "ssl": "@bool(true)"
  }
}
```

### XML-inspired Typed Values
```xml
<config>
  <server>
    <port>@int(8080)</port>
    <timeout>@float(30.5)</timeout>
    <ssl>@bool(true)</ssl>
  </server>
</config>
```

## 🚀 Quick Start Example

```bash
#!/bin/bash
source tusk-bash.sh

cat > typed-quickstart.tsk << 'EOF'
[app]
name: @string("TuskApp")
version: @string("2.1.0")
port: @int(8080)
timeout: @float(30.5)
debug: @bool(true)
servers: @array(["web1", "web2", "web3"])
database: @object({
    "host": "localhost",
    "port": 5432,
    "ssl": true
})
EOF

config=$(tusk_parse typed-quickstart.tsk)
echo "App: $(tusk_get "$config" app.name) v$(tusk_get "$config" app.version)"
echo "Port: $(tusk_get "$config" app.port)"
echo "Timeout: $(tusk_get "$config" app.timeout)s"
echo "Debug: $(tusk_get "$config" app.debug)"
echo "Servers: $(tusk_get "$config" app.servers)"
echo "Database: $(tusk_get "$config" app.database.host):$(tusk_get "$config" app.database.port)"
```

## 🔗 Real-World Use Cases

### 1. Configuration Validation
```ini
[validation]
# Ensure correct types for critical settings
max_connections: @int(100)
connection_timeout: @float(30.0)
ssl_enabled: @bool(true)
allowed_hosts: @array(["localhost", "127.0.0.1"])
database_config: @object({
    "host": "localhost",
    "port": 5432,
    "ssl": true
})
```

### 2. API Configuration
```ini
[api]
# Type-safe API configuration
endpoint: @string("https://api.example.com")
version: @string("v1")
timeout: @int(30)
retries: @int(3)
headers: @object({
    "Content-Type": "application/json",
    "Authorization": "Bearer ${token}"
})
methods: @array(["GET", "POST", "PUT", "DELETE"])
```

### 3. Database Settings
```ini
[database]
# Type-safe database configuration
host: @string("localhost")
port: @int(5432)
name: @string("myapp")
ssl: @bool(true)
pool_size: @int(10)
timeout: @float(5.0)
```

### 4. Type Validation in Bash
```bash
#!/bin/bash
source tusk-bash.sh

cat > type-validation.tsk << 'EOF'
[config]
port: @int(8080)
timeout: @float(30.5)
debug: @bool(true)
EOF

config=$(tusk_parse type-validation.tsk)

# Type checking in Bash
port=$(tusk_get "$config" config.port)
if [[ "$port" =~ ^[0-9]+$ ]]; then
    echo "Port is a valid integer: $port"
else
    echo "Error: Port is not a valid integer"
    exit 1
fi

timeout=$(tusk_get "$config" config.timeout)
if [[ "$timeout" =~ ^[0-9]+\.?[0-9]*$ ]]; then
    echo "Timeout is a valid float: $timeout"
else
    echo "Error: Timeout is not a valid float"
    exit 1
fi
```

## 🧠 Advanced Typed Patterns

### Dynamic Type Conversion
```ini
[conversion]
# Convert environment variables to proper types
port: @int(@env("PORT", "8080"))
timeout: @float(@env("TIMEOUT", "30.0"))
debug: @bool(@env("DEBUG", "false"))
workers: @int(@env("WORKERS", "4"))
```

### Type-Safe Calculations
```ini
[calculations]
base_port: @int(8000)
offset: @int(80)
final_port: @int($base_port + $offset)

base_timeout: @float(30.0)
multiplier: @float(2.0)
final_timeout: @float($base_timeout * $multiplier)
```

### Complex Typed Objects
```ini
[complex]
server_config: @object({
    "host": @string("localhost"),
    "port": @int(8080),
    "ssl": @bool(true),
    "headers": @object({
        "Content-Type": @string("application/json"),
        "User-Agent": @string("TuskLang/2.1.0")
    }),
    "allowed_methods": @array([@string("GET"), @string("POST")])
})
```

### Type Validation with @validate
```ini
[validated]
# Combine type declarations with validation
port: @int(8080)
@validate.range("port", 1, 65535)

timeout: @float(30.5)
@validate.range("timeout", 1.0, 300.0)

debug: @bool(true)
@validate.required(["debug"])
```

## 🛡️ Security & Performance Notes
- **Type safety:** Typed values prevent type-related runtime errors.
- **Validation:** Combine typed values with @validate for comprehensive checking.
- **Performance:** Type checking adds minimal overhead during parsing.
- **Security:** Typed values help prevent injection attacks by ensuring expected data types.

## 🐞 Troubleshooting
- **Type conversion errors:** Ensure input values can be converted to the specified type.
- **Validation conflicts:** Check that type declarations don't conflict with validation rules.
- **Environment variables:** Ensure environment variables contain valid data for type conversion.
- **Array/object syntax:** Use proper syntax for complex types.

## 💡 Best Practices
- **Use typed values for critical settings:** Ensure important configuration values have correct types.
- **Combine with validation:** Use @validate alongside type declarations for comprehensive checking.
- **Document expected types:** Document the expected types for configuration values.
- **Test type conversions:** Test type conversions with various input values.
- **Handle conversion errors:** Implement error handling for type conversion failures.

## 🔗 Cross-References
- [Numbers](009-numbers-bash.md)
- [Booleans](010-booleans-bash.md)
- [Arrays](012-arrays-bash.md)
- [Objects](013-objects-bash.md)
- [Validation](023-best-practices-bash.md)

---

**Master typed values in TuskLang and build type-safe, robust configurations. 🏷️** 