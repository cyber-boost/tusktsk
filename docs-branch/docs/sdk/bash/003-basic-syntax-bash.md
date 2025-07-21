# 📝 TuskLang Bash Basic Syntax Guide

**"We don't bow to any king" - Master the syntax that adapts to YOU**

TuskLang's revolutionary syntax flexibility means you can write configuration the way YOU want to write it. Whether you prefer traditional INI-style, JSON-like objects, or XML-inspired syntax, TuskLang adapts to your preferences. Let's explore all the syntax styles and data types available in the Bash SDK.

## 🎨 Syntax Flexibility

### Three Syntax Styles

TuskLang supports three distinct syntax styles - choose the one that feels natural to you:

#### 1. Traditional INI-Style (Default)
```bash
#!/bin/bash
source tusk-bash.sh

cat > traditional.tsk << 'EOF'
# Traditional INI-style syntax
$app_name: "TuskApp"
$version: "2.1.0"

[server]
host: "localhost"
port: 8080
debug: true

[database]
host: @env("DB_HOST", "localhost")
port: @env("DB_PORT", 5432)
ssl: false
EOF

config=$(tusk_parse traditional.tsk)
echo "App: $(tusk_get "$config" app_name)"
echo "Server: $(tusk_get "$config" server.host):$(tusk_get "$config" server.port)"
```

#### 2. JSON-Like Objects
```bash
#!/bin/bash
source tusk-bash.sh

cat > json-style.tsk << 'EOF'
{
    "app_name": "TuskApp",
    "version": "2.1.0",
    "server": {
        "host": "localhost",
        "port": 8080,
        "debug": true
    },
    "database": {
        "host": @env("DB_HOST", "localhost"),
        "port": @env("DB_PORT", 5432),
        "ssl": false
    }
}
EOF

config=$(tusk_parse json-style.tsk)
echo "App: $(tusk_get "$config" app_name)"
echo "Server: $(tusk_get "$config" server.host):$(tusk_get "$config" server.port)"
```

#### 3. XML-Inspired Syntax
```bash
#!/bin/bash
source tusk-bash.sh

cat > xml-style.tsk << 'EOF'
<config>
    <app_name>TuskApp</app_name>
    <version>2.1.0</version>
    <server>
        <host>localhost</host>
        <port>8080</port>
        <debug>true</debug>
    </server>
    <database>
        <host>@env("DB_HOST", "localhost")</host>
        <port>@env("DB_PORT", 5432)</port>
        <ssl>false</ssl>
    </database>
</config>
EOF

config=$(tusk_parse xml-style.tsk)
echo "App: $(tusk_get "$config" app_name)"
echo "Server: $(tusk_get "$config" server.host):$(tusk_get "$config" server.port)"
```

## 📊 Data Types

### 1. Strings

```bash
#!/bin/bash
source tusk-bash.sh

cat > strings.tsk << 'EOF'
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

config=$(tusk_parse strings.tsk)
echo "Simple: $(tusk_get "$config" strings.simple)"
echo "Interpolated: $(tusk_get "$config" strings.interpolated)"
echo "Multiline: $(tusk_get "$config" strings.multiline)"
```

### 2. Numbers

```bash
#!/bin/bash
source tusk-bash.sh

cat > numbers.tsk << 'EOF'
[numbers]
# Integers
port: 8080
workers: 4
timeout: 30

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

config=$(tusk_parse numbers.tsk)
echo "Port: $(tusk_get "$config" numbers.port)"
echo "CPU Threshold: $(tusk_get "$config" numbers.cpu_threshold)%"
echo "Memory per Core: $(tusk_get "$config" numbers.memory_per_core)MB"
```

### 3. Booleans

```bash
#!/bin/bash
source tusk-bash.sh

cat > booleans.tsk << 'EOF'
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

config=$(tusk_parse booleans.tsk)
echo "Debug: $(tusk_get "$config" booleans.debug)"
echo "Production: $(tusk_get "$config" booleans.is_production)"
echo "Debug Mode: $(tusk_get "$config" booleans.is_debug)"
```

### 4. Arrays

```bash
#!/bin/bash
source tusk-bash.sh

cat > arrays.tsk << 'EOF'
[arrays]
# Simple arrays
ports: [80, 443, 8080, 3000]
hosts: ["localhost", "127.0.0.1", "0.0.0.0"]
flags: [true, false, true]

# Mixed arrays
mixed: ["string", 42, true, null]

# Array operations
$base_ports: [80, 443]
$dev_ports: [3000, 8080]
all_ports: @array.merge($base_ports, $dev_ports)
unique_ports: @array.unique(all_ports)

# Array access
first_port: all_ports[0]
last_port: all_ports[-1]
port_count: @array.length(all_ports)
EOF

config=$(tusk_parse arrays.tsk)
echo "All Ports: $(tusk_get "$config" arrays.all_ports)"
echo "First Port: $(tusk_get "$config" arrays.first_port)"
echo "Port Count: $(tusk_get "$config" arrays.port_count)"
```

### 5. Objects

```bash
#!/bin/bash
source tusk-bash.sh

cat > objects.tsk << 'EOF'
[objects]
# Simple objects
server: {
    host: "localhost",
    port: 8080,
    ssl: true
}

database: {
    host: @env("DB_HOST", "localhost"),
    port: @env("DB_PORT", 5432),
    name: @env("DB_NAME", "myapp")
}

# Nested objects
config: {
    app: {
        name: "TuskApp",
        version: "2.1.0"
    },
    server: {
        host: "0.0.0.0",
        port: 80
    }
}

# Object operations
$base_config: {
    debug: true,
    log_level: "info"
}

$prod_config: {
    debug: false,
    log_level: "error"
}

merged_config: @object.merge($base_config, $prod_config)
EOF

config=$(tusk_parse objects.tsk)
echo "Server Host: $(tusk_get "$config" objects.server.host)"
echo "Database Name: $(tusk_get "$config" objects.database.name)"
echo "App Name: $(tusk_get "$config" objects.config.app.name)"
echo "Merged Debug: $(tusk_get "$config" objects.merged_config.debug)"
```

### 6. Null Values

```bash
#!/bin/bash
source tusk-bash.sh

cat > nulls.tsk << 'EOF'
[nulls]
# Explicit null
optional_setting: null
missing_value: null

# Conditional null
$environment: @env("APP_ENV", "development")
optional_feature: @if($environment == "production", "enabled", null)

# Null checks
has_optional: optional_setting != null
has_feature: optional_feature != null
EOF

config=$(tusk_parse nulls.tsk)
echo "Optional Setting: $(tusk_get "$config" nulls.optional_setting)"
echo "Has Optional: $(tusk_get "$config" nulls.has_optional)"
echo "Has Feature: $(tusk_get "$config" nulls.has_feature)"
```

## 🔗 Variables and Interpolation

### Global Variables

```bash
#!/bin/bash
source tusk-bash.sh

cat > variables.tsk << 'EOF'
# Global variables (use $ prefix)
$app_name: "TuskApp"
$version: "2.1.0"
$environment: @env("APP_ENV", "development")

# Use variables in sections
[app]
name: $app_name
version: $version
full_name: "${app_name} v${version}"

[server]
host: @if($environment == "production", "0.0.0.0", "localhost")
port: @if($environment == "production", 80, 8080)
url: "http://${host}:${port}"

# Variable operations
$base_path: "/var/www"
$app_path: "${base_path}/${app_name}"
$log_path: "${app_path}/logs"
EOF

config=$(tusk_parse variables.tsk)
echo "App Full Name: $(tusk_get "$config" app.full_name)"
echo "Server URL: $(tusk_get "$config" server.url)"
echo "Log Path: $(tusk_get "$config" log_path)"
```

### String Interpolation

```bash
#!/bin/bash
source tusk-bash.sh

cat > interpolation.tsk << 'EOF'
[interpolation]
# Basic interpolation
$name: "Alice"
$age: 30
message: "Hello, ${name}! You are ${age} years old."

# Complex interpolation
$protocol: "https"
$domain: "example.com"
$path: "/api"
url: "${protocol}://${domain}${path}"

# Nested interpolation
$base_url: "${protocol}://${domain}"
$api_url: "${base_url}${path}"
full_url: "${api_url}/users"

# Conditional interpolation
$environment: @env("APP_ENV", "development")
$port: @if($environment == "production", 443, 3000)
server_url: "${protocol}://${domain}:${port}"
EOF

config=$(tusk_parse interpolation.tsk)
echo "Message: $(tusk_get "$config" interpolation.message)"
echo "Full URL: $(tusk_get "$config" interpolation.full_url)"
echo "Server URL: $(tusk_get "$config" interpolation.server_url)"
```

## 🔄 Comments

### Comment Styles

```bash
#!/bin/bash
source tusk-bash.sh

cat > comments.tsk << 'EOF'
# This is a single-line comment
$app_name: "TuskApp"  # Inline comment

[server]
host: "localhost"  # Server hostname
port: 8080         # Server port

# Multiline comments
# This is a multiline comment
# that spans multiple lines
# for better documentation

[database]
# Database configuration
host: @env("DB_HOST", "localhost")  # Database host
port: @env("DB_PORT", 5432)         # Database port
ssl: true                           # Enable SSL

# Section comments
[logging]
# Logging configuration
level: "info"      # Log level: debug, info, warn, error
file: "/var/log/app.log"  # Log file path
EOF

config=$(tusk_parse comments.tsk)
echo "App Name: $(tusk_get "$config" app_name)"
echo "Server: $(tusk_get "$config" server.host):$(tusk_get "$config" server.port)"
echo "Database: $(tusk_get "$config" database.host):$(tusk_get "$config" database.port)"
```

## 🔧 Type Safety

### Type Validation

```bash
#!/bin/bash
source tusk-bash.sh

cat > type-safety.tsk << 'EOF'
[type_safety]
# Type annotations
port: @int(8080)
cpu_threshold: @float(80.5)
debug: @bool(true)
hosts: @array(["localhost", "127.0.0.1"])
config: @object({host: "localhost", port: 8080})

# Type validation
@validate.type("port", "int")
@validate.type("cpu_threshold", "float")
@validate.type("debug", "bool")
@validate.type("hosts", "array")
@validate.type("config", "object")

# Range validation
@validate.range("port", 1, 65535)
@validate.range("cpu_threshold", 0.0, 100.0)
EOF

config=$(tusk_parse type-safety.tsk)
echo "Port (int): $(tusk_get "$config" type_safety.port)"
echo "CPU Threshold (float): $(tusk_get "$config" type_safety.cpu_threshold)"
echo "Debug (bool): $(tusk_get "$config" type_safety.debug)"
```

## 🎯 Conditional Logic

### If Statements

```bash
#!/bin/bash
source tusk-bash.sh

cat > conditionals.tsk << 'EOF'
$environment: @env("APP_ENV", "development")

[conditionals]
# Simple if statements
debug: @if($environment == "development", true, false)
port: @if($environment == "production", 80, 8080)
host: @if($environment == "production", "0.0.0.0", "localhost")

# Complex conditionals
log_level: @if($environment == "production", "error", 
               @if($environment == "staging", "warn", "debug"))

# Nested conditionals
ssl: @if($environment == "production", true,
         @if($environment == "staging", true, false))

# Multiple conditions
workers: @if($environment == "production", 4,
             @if($environment == "staging", 2, 1))

# Boolean operations
is_production: $environment == "production"
is_development: $environment == "development"
is_staging: $environment == "staging"
EOF

config=$(tusk_parse conditionals.tsk)
echo "Debug: $(tusk_get "$config" conditionals.debug)"
echo "Port: $(tusk_get "$config" conditionals.port)"
echo "Log Level: $(tusk_get "$config" conditionals.log_level)"
echo "SSL: $(tusk_get "$config" conditionals.ssl)"
echo "Workers: $(tusk_get "$config" conditionals.workers)"
```

## 🔗 Cross-File References

### Import and Include

```bash
#!/bin/bash
source tusk-bash.sh

# Create base configuration
cat > base.tsk << 'EOF'
$app_name: "TuskApp"
$version: "2.1.0"

[database]
host: "localhost"
port: 5432
EOF

# Create environment configuration
cat > production.tsk << 'EOF'
$environment: "production"

[database]
host: "db.production.com"
ssl: true

[server]
workers: 4
timeout: 30
EOF

# Create main configuration with imports
cat > main.tsk << 'EOF'
# Import base configuration
@import "base.tsk"

# Import environment-specific configuration
@import "${environment}.tsk"

# Override or add new settings
[app]
full_name: "${app_name} v${version}"
environment: $environment

[logging]
level: @if($environment == "production", "error", "debug")
file: @if($environment == "production", "/var/log/app.log", "console")
EOF

# Parse with environment context
export environment="production"
config=$(tusk_parse main.tsk)

echo "App: $(tusk_get "$config" app.full_name)"
echo "Database: $(tusk_get "$config" database.host)"
echo "SSL: $(tusk_get "$config" database.ssl)"
echo "Workers: $(tusk_get "$config" server.workers)"
echo "Log Level: $(tusk_get "$config" logging.level)"
```

## 🚨 Error Handling

### Syntax Validation

```bash
#!/bin/bash
source tusk-bash.sh

cat > validation.tsk << 'EOF'
[validation]
# Required fields
@validate.required(["api_key", "database_url"])

# Type validation
@validate.type("port", "int")
@validate.type("debug", "bool")

# Range validation
@validate.range("port", 1, 65535)
@validate.range("timeout", 1, 300)

# Custom validation
@validate.custom("port", "port > 0 && port < 65536")
@validate.custom("timeout", "timeout > 0 && timeout <= 300")

# Environment validation
@validate.env(["API_KEY", "DATABASE_URL"])
EOF

# Test validation
validate_config() {
    local file="$1"
    if tusk_validate "$file"; then
        echo "✓ Configuration is valid"
    else
        echo "✗ Configuration has errors"
        return 1
    fi
}

# Test with valid config
validate_config validation.tsk
```

## 🎯 Best Practices

### 1. Use Environment Variables

```bash
#!/bin/bash
source tusk-bash.sh

cat > best-practices.tsk << 'EOF'
# ✅ Good: Use environment variables
[database]
host: @env("DB_HOST", "localhost")
port: @env("DB_PORT", 5432)
password: @env("DB_PASSWORD")

# ❌ Bad: Hardcoded values
# host: "production-db.example.com"
# port: 5432
# password: "secret123"
EOF
```

### 2. Use Global Variables for Reusability

```bash
#!/bin/bash
source tusk-bash.sh

cat > reusable.tsk << 'EOF'
# ✅ Good: Use global variables
$app_name: "TuskApp"
$version: "2.1.0"

[app]
name: $app_name
version: $version
full_name: "${app_name} v${version}"

[paths]
log_file: "/var/log/${app_name}.log"
config_file: "/etc/${app_name}/config.json"

# ❌ Bad: Repeated values
# name: "TuskApp"
# version: "2.1.0"
# full_name: "TuskApp v2.1.0"
# log_file: "/var/log/TuskApp.log"
EOF
```

### 3. Use Conditional Logic for Environment-Specific Settings

```bash
#!/bin/bash
source tusk-bash.sh

cat > environment-aware.tsk << 'EOF'
$environment: @env("APP_ENV", "development")

# ✅ Good: Environment-aware configuration
[server]
host: @if($environment == "production", "0.0.0.0", "localhost")
port: @if($environment == "production", 80, 8080)
workers: @if($environment == "production", 4, 1)

[logging]
level: @if($environment == "production", "error", "debug")
file: @if($environment == "production", "/var/log/app.log", "console")

# ❌ Bad: Separate files for each environment
# production.tsk, development.tsk, staging.tsk
EOF
```

## 🎯 What You've Learned

In this basic syntax guide, you've mastered:

✅ **Three syntax styles** - Traditional, JSON-like, and XML-inspired  
✅ **All data types** - Strings, numbers, booleans, arrays, objects, null  
✅ **Variables and interpolation** - Global variables and string interpolation  
✅ **Comments** - Single-line and inline comments  
✅ **Type safety** - Type validation and range checking  
✅ **Conditional logic** - If statements and boolean operations  
✅ **Cross-file references** - Import and include functionality  
✅ **Error handling** - Validation and error checking  
✅ **Best practices** - Environment variables, reusability, and environment awareness  

## 🚀 Next Steps

Ready to integrate with databases and build advanced features?

1. **Database Integration** → [004-database-integration-bash.md](004-database-integration-bash.md)
2. **Advanced Features** → [005-advanced-features-bash.md](005-advanced-features-bash.md)

## 💡 Pro Tips

- **Choose your syntax style** - Use what feels natural to you
- **Use environment variables** - Never hardcode sensitive values
- **Leverage global variables** - Make your configs DRY (Don't Repeat Yourself)
- **Validate early** - Use type and range validation
- **Use conditional logic** - One config file for all environments

---

**You're now a TuskLang syntax master! 🐚** 