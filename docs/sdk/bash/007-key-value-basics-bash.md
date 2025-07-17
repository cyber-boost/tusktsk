# 🔑 TuskLang Bash Key-Value Basics Guide

**"We don't bow to any king" - Master the foundation of configuration**

Key-value pairs are the fundamental building blocks of TuskLang configuration. Whether you're setting simple variables, organizing data into sections, or creating complex nested structures, understanding key-value syntax is essential for writing effective TuskLang configurations.

## 🎯 Basic Key-Value Syntax

### Simple Key-Value Pairs

```bash
#!/bin/bash
source tusk-bash.sh

cat > basic-key-value.tsk << 'EOF'
# Simple key-value pairs
app_name: "TuskApp"
version: "2.1.0"
debug: true
port: 8080
timeout: 30

# String values
host: "localhost"
protocol: "https"
path: "/api/v1"

# Numeric values
max_connections: 100
cache_size: 512
retry_attempts: 3

# Boolean values
ssl_enabled: true
auto_restart: false
verbose_logging: yes
EOF

config=$(tusk_parse basic-key-value.tsk)
echo "=== Basic Key-Value Configuration ==="
echo "App: $(tusk_get "$config" app_name) v$(tusk_get "$config" version)"
echo "Host: $(tusk_get "$config" host):$(tusk_get "$config" port)"
echo "Debug: $(tusk_get "$config" debug)"
echo "SSL: $(tusk_get "$config" ssl_enabled)"
echo "Max Connections: $(tusk_get "$config" max_connections)"
```

### Global Variables

```bash
#!/bin/bash
source tusk-bash.sh

cat > global-variables.tsk << 'EOF'
# Global variables (use $ prefix)
$app_name: "TuskApp"
$version: "2.1.0"
$environment: @env("APP_ENV", "development")

# Use global variables in sections
[app]
name: $app_name
version: $version
full_name: "${app_name} v${version}"

[server]
host: @if($environment == "production", "0.0.0.0", "localhost")
port: @if($environment == "production", 80, 8080)
url: "${protocol}://${host}:${port}"

# Variable operations
$base_path: "/var/www"
$app_path: "${base_path}/${app_name}"
$log_path: "${app_path}/logs"
EOF

config=$(tusk_parse global-variables.tsk)
echo "=== Global Variables ==="
echo "App Full Name: $(tusk_get "$config" app.full_name)"
echo "Server URL: $(tusk_get "$config" server.url)"
echo "Log Path: $(tusk_get "$config" log_path)"
```

## 📁 Section Organization

### Basic Sections

```bash
#!/bin/bash
source tusk-bash.sh

cat > sections-basic.tsk << 'EOF'
# Server configuration section
[server]
host: "localhost"
port: 8080
workers: 4
timeout: 30

# Database configuration section
[database]
host: @env("DB_HOST", "localhost")
port: @env("DB_PORT", 5432)
name: @env("DB_NAME", "tuskapp")
ssl: true

# Logging configuration section
[logging]
level: "info"
file: "/var/log/app.log"
max_size: "100MB"
backup_count: 5
EOF

config=$(tusk_parse sections-basic.tsk)
echo "=== Section Organization ==="
echo "Server: $(tusk_get "$config" server.host):$(tusk_get "$config" server.port)"
echo "Database: $(tusk_get "$config" database.host):$(tusk_get "$config" database.port)"
echo "Log Level: $(tusk_get "$config" logging.level)"
echo "Log File: $(tusk_get "$config" logging.file)"
```

### Nested Sections

```bash
#!/bin/bash
source tusk-bash.sh

cat > sections-nested.tsk << 'EOF'
# Application configuration with nested sections
[app]
name: "TuskApp"
version: "2.1.0"

# Nested server configuration
[app.server]
host: "localhost"
port: 8080
ssl: true

# Nested database configuration
[app.database]
host: "localhost"
port: 5432
name: "tuskapp"

# Nested security configuration
[app.security]
encryption: true
session_timeout: "24h"
max_login_attempts: 5
EOF

config=$(tusk_parse sections-nested.tsk)
echo "=== Nested Sections ==="
echo "App: $(tusk_get "$config" app.name) v$(tusk_get "$config" app.version)"
echo "Server: $(tusk_get "$config" app.server.host):$(tusk_get "$config" app.server.port)"
echo "Database: $(tusk_get "$config" app.database.host):$(tusk_get "$config" app.database.port)"
echo "Session Timeout: $(tusk_get "$config" app.security.session_timeout)"
```

## 🔗 Value Types

### String Values

```bash
#!/bin/bash
source tusk-bash.sh

cat > string-values.tsk << 'EOF'
[strings]
# Basic strings
simple: "Hello, World!"
quoted: 'Single quoted string'
unquoted: This is also a string

# String interpolation
$name: "Alice"
$greeting: "Hello, ${name}!"
interpolated: "${greeting} Welcome to TuskLang!"

# Multiline strings
multiline: """
This is a multiline
string that spans
multiple lines
"""

# Escaped characters
escaped: "Line 1\nLine 2\tTabbed"
path: "/var/log/app.log"
EOF

config=$(tusk_parse string-values.tsk)
echo "=== String Values ==="
echo "Simple: $(tusk_get "$config" strings.simple)"
echo "Interpolated: $(tusk_get "$config" strings.interpolated)"
echo "Multiline: $(tusk_get "$config" strings.multiline)"
```

### Numeric Values

```bash
#!/bin/bash
source tusk-bash.sh

cat > numeric-values.tsk << 'EOF'
[numbers]
# Integers
port: 8080
workers: 4
timeout: 30
max_connections: 100

# Floating point
cpu_threshold: 80.5
memory_limit: 512.0
load_average: 2.75

# Scientific notation
large_number: 1.5e6
small_number: 2.3e-4

# Calculations
total_memory: 1024 * 512
cpu_cores: 8
memory_per_core: total_memory / cpu_cores
EOF

config=$(tusk_parse numeric-values.tsk)
echo "=== Numeric Values ==="
echo "Port: $(tusk_get "$config" numbers.port)"
echo "CPU Threshold: $(tusk_get "$config" numbers.cpu_threshold)%"
echo "Memory per Core: $(tusk_get "$config" numbers.memory_per_core)MB"
```

### Boolean Values

```bash
#!/bin/bash
source tusk-bash.sh

cat > boolean-values.tsk << 'EOF'
[booleans]
# True values
debug: true
ssl_enabled: true
verbose: yes
enabled: on

# False values
production: false
maintenance: no
disabled: off

# Conditional booleans
$environment: @env("APP_ENV", "development")
is_production: $environment == "production"
is_debug: $environment != "production"
EOF

config=$(tusk_parse boolean-values.tsk)
echo "=== Boolean Values ==="
echo "Debug: $(tusk_get "$config" booleans.debug)"
echo "Production: $(tusk_get "$config" booleans.is_production)"
echo "Debug Mode: $(tusk_get "$config" booleans.is_debug)"
```

## 🔄 Dynamic Values

### Environment Variables

```bash
#!/bin/bash
source tusk-bash.sh

cat > dynamic-values.tsk << 'EOF'
[dynamic]
# Environment variables with defaults
api_key: @env("API_KEY", "default_key")
database_url: @env("DATABASE_URL", "sqlite:///app.db")
debug_mode: @env("DEBUG", "false")

# Environment variables without defaults
secret_key: @env("SECRET_KEY")
admin_email: @env("ADMIN_EMAIL")

# Conditional values based on environment
$environment: @env("APP_ENV", "development")
server_host: @if($environment == "production", "0.0.0.0", "localhost")
server_port: @if($environment == "production", 80, 8080)
log_level: @if($environment == "production", "error", "debug")
EOF

config=$(tusk_parse dynamic-values.tsk)
echo "=== Dynamic Values ==="
echo "API Key: $(tusk_get "$config" dynamic.api_key)"
echo "Database URL: $(tusk_get "$config" dynamic.database_url)"
echo "Server: $(tusk_get "$config" dynamic.server_host):$(tusk_get "$config" dynamic.server_port)"
echo "Log Level: $(tusk_get "$config" dynamic.log_level)"
```

### Computed Values

```bash
#!/bin/bash
source tusk-bash.sh

cat > computed-values.tsk << 'EOF'
[computed]
# Values computed from other values
$base_url: "https://api.example.com"
$api_version: "v1"
full_api_url: "${base_url}/${api_version}"

# Values computed from system information
$current_time: @date.now()
$system_load: @shell("uptime | awk -F'load average:' '{print $2}' | awk '{print $1}'")
$memory_usage: @shell("free | grep Mem | awk '{printf \"%.2f\", $3/$2 * 100.0}'")

# Values computed from database queries
user_count: @query("SELECT COUNT(*) FROM users")
active_users: @query("SELECT COUNT(*) FROM users WHERE active = 1")

# Values computed from file operations
config_hash: @file.hash("config.tsk", "sha256")
file_size: @file.size("large_file.dat")
EOF

config=$(tusk_parse computed-values.tsk)
echo "=== Computed Values ==="
echo "Full API URL: $(tusk_get "$config" computed.full_api_url)"
echo "Current Time: $(tusk_get "$config" computed.current_time)"
echo "System Load: $(tusk_get "$config" computed.system_load)"
echo "Memory Usage: $(tusk_get "$config" computed.memory_usage)%"
```

## 🎯 Key Naming Conventions

### Best Practices

```bash
#!/bin/bash
source tusk-bash.sh

cat > naming-conventions.tsk << 'EOF'
[naming]
# ✅ Good: Use descriptive names
application_name: "TuskApp"
database_connection_timeout: 30
maximum_retry_attempts: 3

# ✅ Good: Use consistent naming patterns
server_host: "localhost"
server_port: 8080
server_ssl: true

# ✅ Good: Use underscores for readability
log_file_path: "/var/log/app.log"
cache_time_to_live: "5m"
session_timeout_duration: "24h"

# ✅ Good: Use prefixes for related settings
db_host: "localhost"
db_port: 5432
db_name: "tuskapp"

# ✅ Good: Use environment-specific prefixes
prod_server_host: "0.0.0.0"
dev_server_host: "localhost"
staging_server_host: "staging.example.com"
EOF

config=$(tusk_parse naming-conventions.tsk)
echo "=== Naming Conventions ==="
echo "Application: $(tusk_get "$config" naming.application_name)"
echo "Server: $(tusk_get "$config" naming.server_host):$(tusk_get "$config" naming.server_port)"
echo "Database: $(tusk_get "$config" naming.db_host):$(tusk_get "$config" naming.db_port)"
echo "Log File: $(tusk_get "$config" naming.log_file_path)"
```

## 🔧 Advanced Key-Value Patterns

### Conditional Configuration

```bash
#!/bin/bash
source tusk-bash.sh

cat > conditional-config.tsk << 'EOF'
[conditional]
# Environment-based configuration
$environment: @env("APP_ENV", "development")

# Conditional server settings
server: {
    host: @if($environment == "production", "0.0.0.0", "localhost"),
    port: @if($environment == "production", 80, 8080),
    workers: @if($environment == "production", 4, 1),
    ssl: @if($environment == "production", true, false)
}

# Conditional database settings
database: {
    host: @if($environment == "production", "db.production.com", "localhost"),
    port: @if($environment == "production", 5432, 5432),
    ssl: @if($environment == "production", true, false),
    pool_size: @if($environment == "production", 20, 5)
}

# Conditional logging settings
logging: {
    level: @if($environment == "production", "error", "debug"),
    file: @if($environment == "production", "/var/log/app.log", "console"),
    max_size: @if($environment == "production", "100MB", "10MB")
}
EOF

config=$(tusk_parse conditional-config.tsk)
echo "=== Conditional Configuration ==="
echo "Environment: $(tusk_get "$config" conditional.environment)"
echo "Server: $(tusk_get "$config" conditional.server.host):$(tusk_get "$config" conditional.server.port)"
echo "Database: $(tusk_get "$config" conditional.database.host):$(tusk_get "$config" conditional.database.port)"
echo "Log Level: $(tusk_get "$config" conditional.logging.level)"
```

### Validation and Constraints

```bash
#!/bin/bash
source tusk-bash.sh

cat > validation-config.tsk << 'EOF'
[validation]
# Required values
@validate.required(["api_key", "database_url"])

# Type validation
port: @int(8080)
timeout: @float(30.5)
debug: @bool(true)

# Range validation
@validate.range("port", 1, 65535)
@validate.range("timeout", 1, 300)

# Pattern validation
@validate.pattern("email", "^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$")

# Custom validation
@validate.custom("port", "port > 0 && port < 65536")
@validate.custom("timeout", "timeout > 0 && timeout <= 300")

# Values with validation
api_key: @env("API_KEY")
database_url: @env("DATABASE_URL")
admin_email: @env("ADMIN_EMAIL")
EOF

config=$(tusk_parse validation-config.tsk)
echo "=== Validation Configuration ==="
echo "API Key: $(tusk_get "$config" validation.api_key)"
echo "Database URL: $(tusk_get "$config" validation.database_url)"
echo "Admin Email: $(tusk_get "$config" validation.admin_email)"
```

## 🎯 What You've Learned

In this key-value basics guide, you've mastered:

✅ **Basic key-value syntax** - Simple key-value pairs and global variables  
✅ **Section organization** - Basic and nested sections for organization  
✅ **Value types** - Strings, numbers, booleans, and dynamic values  
✅ **Dynamic values** - Environment variables and computed values  
✅ **Naming conventions** - Best practices for key naming  
✅ **Advanced patterns** - Conditional configuration and validation  
✅ **Validation** - Type checking, range validation, and custom validation  

## 🚀 Next Steps

Ready to explore more TuskLang features?

1. **Strings** → [008-strings-bash.md](008-strings-bash.md)
2. **Numbers** → [009-numbers-bash.md](009-numbers-bash.md)
3. **Booleans** → [010-booleans-bash.md](010-booleans-bash.md)

## 💡 Pro Tips

- **Use descriptive names** - Make keys self-documenting
- **Group related settings** - Use sections to organize configuration
- **Use global variables** - Define reusable values with $ prefix
- **Validate early** - Use validation to catch errors early
- **Use environment variables** - Never hardcode sensitive values
- **Keep it simple** - Start simple and add complexity as needed

---

**Master the foundation of TuskLang configuration! 🔑** 