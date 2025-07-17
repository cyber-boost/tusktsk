# 🌍 TuskLang Bash @env Function Guide

**"We don't bow to any king" – Environment variables are your configuration's lifeblood.**

The @env function in TuskLang is your gateway to dynamic, environment-aware configuration. Whether you're managing different deployment environments, handling sensitive data, or building portable applications, @env provides the flexibility to adapt your configuration to any environment without code changes.

## 🎯 What is @env?
The @env function accesses environment variables with optional fallback values. It provides:
- **Environment awareness** - Different values for different environments
- **Security** - Access to sensitive configuration data
- **Portability** - Configuration that works across systems
- **Flexibility** - Optional fallbacks for missing variables
- **Type safety** - Automatic type conversion for common types

## 📝 Basic @env Syntax

### Simple Environment Access
```ini
[basic]
# Access environment variables
api_key: @env("API_KEY")
database_url: @env("DATABASE_URL")
debug_mode: @env("DEBUG")
```

### With Fallback Values
```ini
[with_fallbacks]
# Provide default values when environment variables are not set
host: @env("HOST", "localhost")
port: @env("PORT", "8080")
timeout: @env("TIMEOUT", "30")
```

### Type-Specific Access
```ini
[typed]
# Convert to specific types
port: @int(@env("PORT", "8080"))
timeout: @float(@env("TIMEOUT", "30.0"))
debug: @bool(@env("DEBUG", "false"))
workers: @int(@env("WORKERS", "4"))
```

## 🚀 Quick Start Example

```bash
#!/bin/bash
source tusk-bash.sh

# Set some environment variables for testing
export APP_ENV="development"
export API_KEY="test_api_key_12345"
export DB_HOST="localhost"
export DEBUG="true"

cat > env-quickstart.tsk << 'EOF'
[application]
name: "TuskApp"
version: "2.1.0"
environment: @env("APP_ENV", "production")
debug_mode: @bool(@env("DEBUG", "false"))

[api]
key: @env("API_KEY")
base_url: @env("API_BASE_URL", "https://api.example.com")
timeout: @int(@env("API_TIMEOUT", "30"))

[database]
host: @env("DB_HOST", "localhost")
port: @int(@env("DB_PORT", "5432"))
name: @env("DB_NAME", "tuskapp")
user: @env("DB_USER", "postgres")
password: @env("DB_PASSWORD", "")

[server]
host: @env("SERVER_HOST", "0.0.0.0")
port: @int(@env("SERVER_PORT", "8080"))
workers: @int(@env("WORKERS", "4"))
EOF

config=$(tusk_parse env-quickstart.tsk)

echo "Application: $(tusk_get "$config" application.name) v$(tusk_get "$config" application.version)"
echo "Environment: $(tusk_get "$config" application.environment)"
echo "Debug Mode: $(tusk_get "$config" application.debug_mode)"
echo "API Key: $(tusk_get "$config" api.key)"
echo "Database: $(tusk_get "$config" database.host):$(tusk_get "$config" database.port)"
echo "Server: $(tusk_get "$config" server.host):$(tusk_get "$config" server.port)"
```

## 🔗 Real-World Use Cases

### 1. Environment-Specific Configuration
```ini
[environment_config]
# Different settings for different environments
environment: @env("APP_ENV", "development")

# Environment-specific database settings
database_host: @if($environment == "production", 
    @env("PROD_DB_HOST", "prod-db.example.com"),
    @env("DEV_DB_HOST", "localhost")
)

database_port: @if($environment == "production", 
    @int(@env("PROD_DB_PORT", "5432")),
    @int(@env("DEV_DB_PORT", "5432"))
)

# Environment-specific logging
log_level: @if($environment == "production", "error", "debug")
log_file: @if($environment == "production", "/var/log/app.log", "/tmp/app.log")
```

### 2. Secure Configuration Management
```ini
[secure_config]
# Use secure environment variables for sensitive data
encryption_key: @env.secure("ENCRYPTION_KEY")
api_secret: @env.secure("API_SECRET")
database_password: @env.secure("DB_PASSWORD")
session_secret: @env.secure("SESSION_SECRET")

# Regular environment variables for non-sensitive data
app_name: @env("APP_NAME", "TuskApp")
app_version: @env("APP_VERSION", "2.1.0")
debug_mode: @bool(@env("DEBUG", "false"))
```

### 3. Docker and Container Configuration
```ini
[container_config]
# Container-aware configuration
container_id: @env("HOSTNAME", "unknown")
pod_name: @env("POD_NAME", "unknown")
namespace: @env("NAMESPACE", "default")

# Resource limits from environment
memory_limit: @env("MEMORY_LIMIT", "512Mi")
cpu_limit: @env("CPU_LIMIT", "500m")
max_workers: @int(@env("MAX_WORKERS", "4"))

# Service discovery
database_host: @env("DB_HOST", "localhost")
redis_host: @env("REDIS_HOST", "localhost")
api_gateway: @env("API_GATEWAY", "http://localhost:8080")
```

### 4. Feature Flags and A/B Testing
```ini
[feature_flags]
# Feature flags from environment variables
feature_new_ui: @bool(@env("FEATURE_NEW_UI", "false"))
feature_analytics: @bool(@env("FEATURE_ANALYTICS", "true"))
feature_beta: @bool(@env("FEATURE_BETA", "false"))

# A/B testing configuration
ab_test_group: @env("AB_TEST_GROUP", "control")
ab_test_percentage: @int(@env("AB_TEST_PERCENTAGE", "50"))

# Dynamic feature configuration
max_upload_size: @int(@env("MAX_UPLOAD_SIZE", "100"))
cache_ttl: @int(@env("CACHE_TTL", "300"))
rate_limit: @int(@env("RATE_LIMIT", "1000"))
```

## 🧠 Advanced @env Patterns

### Conditional Configuration
```bash
#!/bin/bash
source tusk-bash.sh

cat > conditional-env.tsk << 'EOF'
[conditional]
environment: @env("APP_ENV", "development")

# Conditional database configuration
database_config: @if($environment == "production", {
    "host": @env("PROD_DB_HOST", "prod-db.example.com"),
    "port": @int(@env("PROD_DB_PORT", "5432")),
    "ssl": true
}, {
    "host": @env("DEV_DB_HOST", "localhost"),
    "port": @int(@env("DEV_DB_PORT", "5432")),
    "ssl": false
})

# Conditional API configuration
api_config: @if($environment == "production", {
    "base_url": @env("PROD_API_URL", "https://api.example.com"),
    "timeout": @int(@env("PROD_API_TIMEOUT", "30")),
    "retries": @int(@env("PROD_API_RETRIES", "3"))
}, {
    "base_url": @env("DEV_API_URL", "http://localhost:3000"),
    "timeout": @int(@env("DEV_API_TIMEOUT", "10")),
    "retries": @int(@env("DEV_API_RETRIES", "1"))
})
EOF

config=$(tusk_parse conditional-env.tsk)
echo "Environment: $(tusk_get "$config" conditional.environment)"
echo "Database Host: $(tusk_get "$config" conditional.database_config.host)"
echo "API Base URL: $(tusk_get "$config" conditional.api_config.base_url)"
```

### Environment Variable Validation
```bash
#!/bin/bash
source tusk-bash.sh

cat > env-validation.tsk << 'EOF'
[validation]
# Required environment variables
api_key: @env("API_KEY")
database_url: @env("DATABASE_URL")
encryption_key: @env.secure("ENCRYPTION_KEY")

# Validate required variables
@validate.required(["api_key", "database_url", "encryption_key"])

# Optional variables with validation
port: @int(@env("PORT", "8080"))
@validate.range("port", 1, 65535)

timeout: @float(@env("TIMEOUT", "30.0"))
@validate.range("timeout", 1.0, 300.0)

email: @env("ADMIN_EMAIL", "admin@example.com")
@validate.email("email")
EOF

config=$(tusk_parse env-validation.tsk)
echo "API Key: $(tusk_get "$config" validation.api_key)"
echo "Port: $(tusk_get "$config" validation.port)"
echo "Timeout: $(tusk_get "$config" validation.timeout)"
```

### Dynamic Configuration Loading
```bash
#!/bin/bash
source tusk-bash.sh

# Function to load environment-specific configuration
load_env_config() {
    local env=${1:-development}
    local config_file="config/${env}.tsk"
    
    if [[ -f "$config_file" ]]; then
        echo "Loading configuration for environment: $env"
        tusk_parse "$config_file"
    else
        echo "Configuration file $config_file not found, using defaults" >&2
        # Return default configuration
        cat > /tmp/default.tsk << 'EOF'
[app]
name: "TuskApp"
environment: "default"
debug: false
EOF
        tusk_parse /tmp/default.tsk
    fi
}

# Usage
config=$(load_env_config "$APP_ENV")
echo "Loaded configuration for: $(tusk_get "$config" app.environment)"
```

## 🛡️ Security & Performance Notes
- **Secure environment variables:** Use @env.secure for sensitive data like passwords and API keys
- **Validation:** Always validate environment variables before using them
- **Fallbacks:** Provide sensible default values for all environment variables
- **Type safety:** Use type conversion functions (@int, @bool, @float) for proper type handling
- **Performance:** Environment variable access is fast and cached

## 🐞 Troubleshooting
- **Missing variables:** Always provide fallback values to prevent configuration errors
- **Type mismatches:** Use type conversion functions to ensure proper data types
- **Permission issues:** Ensure proper permissions for accessing environment variables
- **Scope issues:** Environment variables are process-scoped; child processes inherit them

## 💡 Best Practices
- **Use descriptive names:** Choose clear, descriptive environment variable names
- **Provide fallbacks:** Always provide default values for optional environment variables
- **Validate inputs:** Use @validate functions to ensure environment variables meet requirements
- **Document variables:** Document all required and optional environment variables
- **Use secure variants:** Use @env.secure for sensitive data
- **Type conversion:** Use appropriate type conversion functions for data types

## 🔗 Cross-References
- [@ Operator Introduction](024-at-operator-intro-bash.md)
- [@shell Operator](026-at-shell-function-bash.md)
- [Validation](023-best-practices-bash.md)
- [Security](105-security-bash.md)

---

**Master @env in TuskLang and build environment-aware, secure configurations. 🌍** 