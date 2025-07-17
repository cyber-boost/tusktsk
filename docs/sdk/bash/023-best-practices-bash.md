# 🎯 TuskLang Bash Best Practices Guide

**"We don't bow to any king" – Write TuskLang like a master, not like a novice.**

Best practices in TuskLang are the proven patterns, techniques, and guidelines that separate professional configurations from amateur attempts. Whether you're building simple scripts or complex enterprise systems, following these best practices ensures your TuskLang code is maintainable, secure, and performant.

## 🎯 Why Best Practices Matter
Following best practices provides:
- **Maintainability** - Code that's easy to understand and modify
- **Security** - Protection against vulnerabilities and data breaches
- **Performance** - Efficient, optimized configurations
- **Reliability** - Robust, error-free applications
- **Team collaboration** - Consistent, readable code across projects

## 📝 Configuration Structure

### Logical Organization
```ini
# ✅ Good: Well-organized configuration
[application]
name: "TuskApp"
version: "2.1.0"
environment: @env("APP_ENV", "development")

[server]
host: @env("SERVER_HOST", "localhost")
port: @env("SERVER_PORT", "8080")
ssl_enabled: @env("SSL_ENABLED", "false")

[database]
host: @env("DB_HOST", "localhost")
port: @env("DB_PORT", "5432")
name: @env("DB_NAME", "tuskapp")
ssl: @env("DB_SSL", "true")

[security]
encryption_key: @env.secure("ENCRYPTION_KEY")
session_secret: @env.secure("SESSION_SECRET")
password_min_length: 8
```

### Consistent Naming
```ini
# ✅ Good: Consistent naming conventions
$app_name: "TuskApp"
$app_version: "2.1.0"
$app_environment: @env("APP_ENV", "development")

# Use snake_case for variables
database_connection_timeout: 30
maximum_retry_attempts: 3
ssl_certificate_path: "/etc/ssl/certs/server.crt"

# ❌ Bad: Inconsistent naming
appName: "TuskApp"
APP_VERSION: "2.1.0"
app-env: @env("APP_ENV", "development")
```

## 🚀 Quick Start Example

```bash
#!/bin/bash
source tusk-bash.sh

# Create a well-structured configuration following best practices
cat > best-practices-example.tsk << 'EOF'
# TuskLang Configuration - Best Practices Example
# Author: Your Name
# Date: $(date +%Y-%m-%d)
# Description: Example configuration demonstrating TuskLang best practices

[application]
name: "TuskApp"
version: "2.1.0"
environment: @env("APP_ENV", "development")
debug_mode: @env("DEBUG", "false")

[server]
host: @env("SERVER_HOST", "localhost")
port: @int(@env("SERVER_PORT", "8080"))
ssl_enabled: @bool(@env("SSL_ENABLED", "false"))
workers: @int(@env("WORKERS", "4"))

[database]
host: @env("DB_HOST", "localhost")
port: @int(@env("DB_PORT", "5432"))
name: @env("DB_NAME", "tuskapp")
ssl: @bool(@env("DB_SSL", "true"))
connection_timeout: @int(@env("DB_TIMEOUT", "30"))
max_connections: @int(@env("DB_MAX_CONN", "100"))

[security]
encryption_key: @env.secure("ENCRYPTION_KEY")
session_secret: @env.secure("SESSION_SECRET")
password_min_length: @int(@env("PASSWORD_MIN_LENGTH", "8"))
max_login_attempts: @int(@env("MAX_LOGIN_ATTEMPTS", "5"))

[logging]
level: @env("LOG_LEVEL", "info")
file_path: @env("LOG_FILE", "/var/log/tuskapp.log")
max_file_size: @int(@env("LOG_MAX_SIZE", "100"))
backup_count: @int(@env("LOG_BACKUP_COUNT", "5"))
EOF

# Load and validate configuration
config=$(tusk_parse best-practices-example.tsk)

# Extract values with error handling
app_name=$(tusk_get "$config" application.name 2>/dev/null || echo "Unknown")
app_version=$(tusk_get "$config" application.version 2>/dev/null || echo "Unknown")
server_host=$(tusk_get "$config" server.host 2>/dev/null || echo "localhost")
server_port=$(tusk_get "$config" server.port 2>/dev/null || echo "8080")

echo "Application: $app_name v$app_version"
echo "Server: $server_host:$server_port"
```

## 🔗 Real-World Use Cases

### 1. Environment-Specific Configuration
```ini
# config/base.tsk - Base configuration
[application]
name: "TuskApp"
version: "2.1.0"

[server]
timeout: 30
retries: 3

# config/development.tsk - Development overrides
@include "base.tsk"

[server]
host: "localhost"
port: 8080
debug: true

# config/production.tsk - Production overrides
@include "base.tsk"

[server]
host: "0.0.0.0"
port: 80
debug: false
ssl_enabled: true
```

### 2. Security-First Configuration
```ini
[security]
# Always use secure environment variables for secrets
encryption_key: @env.secure("ENCRYPTION_KEY")
api_key: @env.secure("API_KEY")
database_password: @env.secure("DB_PASSWORD")

# Validate sensitive configuration
@validate.required(["encryption_key", "api_key", "database_password"])

# Use strong defaults for security settings
password_min_length: 8
session_timeout_minutes: 30
max_login_attempts: 5
require_two_factor: false
```

### 3. Performance-Optimized Configuration
```ini
[performance]
# Use appropriate data types for performance
cache_enabled: @bool(@env("CACHE_ENABLED", "true"))
cache_ttl_minutes: @int(@env("CACHE_TTL", "15"))
max_connections: @int(@env("MAX_CONNECTIONS", "100"))
connection_pool_size: @int(@env("POOL_SIZE", "10"))

# Optimize for your use case
database_query_timeout: @int(@env("DB_TIMEOUT", "30"))
api_request_timeout: @int(@env("API_TIMEOUT", "10"))
file_upload_max_size: @int(@env("UPLOAD_MAX_SIZE", "100"))
```

### 4. Monitoring and Observability
```ini
[monitoring]
# Enable comprehensive monitoring
metrics_enabled: @bool(@env("METRICS_ENABLED", "true"))
health_check_interval: @int(@env("HEALTH_CHECK_INTERVAL", "30"))
log_level: @env("LOG_LEVEL", "info")

# Structured logging
log_format: "json"
log_fields: ["timestamp", "level", "message", "user_id", "request_id"]

# Error tracking
error_reporting_enabled: @bool(@env("ERROR_REPORTING", "true"))
error_sampling_rate: @float(@env("ERROR_SAMPLING_RATE", "1.0"))
```

## 🧠 Advanced Best Practices

### Configuration Validation
```ini
[validated_config]
# Use type annotations for validation
port: @int(@env("PORT", "8080"))
timeout: @float(@env("TIMEOUT", "30.0"))
debug: @bool(@env("DEBUG", "false"))

# Validate ranges and constraints
@validate.range("port", 1, 65535)
@validate.range("timeout", 1.0, 300.0)
@validate.required(["port", "timeout"])

# Custom validation rules
@validate.custom("port", "port % 2 == 0", "Port must be even")
```

### Modular Configuration
```ini
# config/database.tsk
[database]
host: @env("DB_HOST", "localhost")
port: @int(@env("DB_PORT", "5432"))
name: @env("DB_NAME", "tuskapp")
ssl: @bool(@env("DB_SSL", "true"))

# config/security.tsk
[security]
encryption_key: @env.secure("ENCRYPTION_KEY")
session_secret: @env.secure("SESSION_SECRET")

# config/main.tsk
@include "database.tsk"
@include "security.tsk"

[application]
name: "TuskApp"
version: "2.1.0"
```

### Error Handling and Fallbacks
```ini
[robust_config]
# Provide fallbacks for all critical values
database_host: @env("DB_HOST", "localhost")
database_port: @int(@env("DB_PORT", "5432"))
api_timeout: @int(@env("API_TIMEOUT", "30"))

# Use conditional logic for environment-specific behavior
debug_mode: @if(@env("APP_ENV") == "development", true, false)
log_level: @if($debug_mode, "debug", "info")
ssl_enabled: @if(@env("APP_ENV") == "production", true, false)
```

## 🛡️ Security Best Practices

### Secret Management
```ini
[secrets]
# Never hardcode secrets
api_key: @env.secure("API_KEY")
database_password: @env.secure("DB_PASSWORD")
encryption_key: @env.secure("ENCRYPTION_KEY")

# Use secure environment variables
session_secret: @env.secure("SESSION_SECRET")
oauth_client_secret: @env.secure("OAUTH_CLIENT_SECRET")
```

### Input Validation
```ini
[validation]
# Validate all inputs
port: @int(@env("PORT", "8080"))
@validate.range("port", 1, 65535)

host: @env("HOST", "localhost")
@validate.pattern("host", "^[a-zA-Z0-9.-]+$", "Invalid hostname")

email: @env("ADMIN_EMAIL", "admin@example.com")
@validate.email("email")
```

## 🐞 Performance Best Practices

### Efficient Configuration Loading
```bash
#!/bin/bash
source tusk-bash.sh

# Cache configuration for performance
CONFIG_CACHE_FILE="/tmp/tusk_config_cache"
CONFIG_CACHE_TTL=300  # 5 minutes

load_cached_config() {
    local config_file="$1"
    
    # Check if cache is valid
    if [[ -f "$CONFIG_CACHE_FILE" ]]; then
        local cache_age=$(($(date +%s) - $(stat -c %Y "$CONFIG_CACHE_FILE")))
        if [[ $cache_age -lt $CONFIG_CACHE_TTL ]]; then
            cat "$CONFIG_CACHE_FILE"
            return 0
        fi
    fi
    
    # Parse and cache configuration
    local config=$(tusk_parse "$config_file")
    echo "$config" > "$CONFIG_CACHE_FILE"
    echo "$config"
}

# Usage
config=$(load_cached_config "app.tsk")
```

## 💡 Best Practices Summary

### Do's
- ✅ Use descriptive, consistent naming conventions
- ✅ Organize configuration logically by functionality
- ✅ Use environment variables for configuration
- ✅ Validate all inputs and configuration values
- ✅ Provide fallbacks for critical values
- ✅ Use type annotations for clarity
- ✅ Document your configuration structure
- ✅ Test your configuration thoroughly

### Don'ts
- ❌ Don't hardcode secrets or sensitive data
- ❌ Don't use inconsistent naming patterns
- ❌ Don't skip validation for critical values
- ❌ Don't mix different syntax styles unnecessarily
- ❌ Don't create deeply nested configurations
- ❌ Don't ignore error handling
- ❌ Don't use unclear or ambiguous variable names

## 🔗 Cross-References
- [Variable Naming](020-variable-naming-bash.md)
- [Error Handling](022-error-handling-bash.md)
- [File Structure](015-file-structure-bash.md)
- [Security](105-security-bash.md)

---

**Follow these best practices and write TuskLang configurations that stand the test of time. 🎯** 