# 💬 TuskLang Bash Comments Guide

**"We don't bow to any king" - Document your configuration with style**

Comments in TuskLang are your way of adding human-readable context to your configuration files. Whether you're documenting complex logic, explaining business rules, or leaving notes for future developers, TuskLang provides flexible commenting that adapts to your preferred style.

## 🎯 Comment Styles

### Single-Line Comments

```bash
#!/bin/bash
source tusk-bash.sh

cat > comments-single.tsk << 'EOF'
# This is a single-line comment
$app_name: "TuskApp"  # Inline comment after a value

[server]
host: "localhost"  # Server hostname
port: 8080         # Server port number
debug: true        # Enable debug mode

# Multiple single-line comments
# for complex explanations
# that span multiple lines
EOF

config=$(tusk_parse comments-single.tsk)
echo "App Name: $(tusk_get "$config" app_name)"
echo "Server: $(tusk_get "$config" server.host):$(tusk_get "$config" server.port)"
```

### Inline Comments

```bash
#!/bin/bash
source tusk-bash.sh

cat > comments-inline.tsk << 'EOF'
[configuration]
# Basic settings with inline comments
api_key: @env("API_KEY")        # Load from environment variable
timeout: 30                     # Request timeout in seconds
retry_count: 3                  # Number of retry attempts
cache_ttl: "5m"                 # Cache time-to-live

# Database configuration
database: {
    host: "localhost",          # Database host
    port: 5432,                 # Database port
    ssl: true                   # Enable SSL connection
}

# Feature flags with explanations
features: {
    new_ui: true,               # Enable new user interface
    beta_api: false,            # Disable beta API endpoints
    analytics: true             # Enable usage analytics
}
EOF

config=$(tusk_parse comments-inline.tsk)
echo "API Key: $(tusk_get "$config" configuration.api_key)"
echo "Timeout: $(tusk_get "$config" configuration.timeout)s"
echo "Database SSL: $(tusk_get "$config" configuration.database.ssl)"
```

## 📝 Documentation Comments

### Section Documentation

```bash
#!/bin/bash
source tusk-bash.sh

cat > comments-documentation.tsk << 'EOF'
# =============================================================================
# TuskApp Configuration File
# =============================================================================
# 
# This configuration file defines the settings for the TuskApp application.
# It supports multiple environments and includes security best practices.
#
# Author: Development Team
# Version: 2.1.0
# Last Updated: 2024-12-19
# =============================================================================

# Global application variables
$app_name: "TuskApp"
$version: "2.1.0"
$environment: @env("APP_ENV", "development")

# =============================================================================
# SERVER CONFIGURATION
# =============================================================================
# 
# Server settings control how the application handles HTTP requests,
# including host binding, port configuration, and worker processes.
#
[server]
host: @if($environment == "production", "0.0.0.0", "localhost")
port: @if($environment == "production", 80, 8080)
workers: @if($environment == "production", 4, 1)
timeout: 30

# =============================================================================
# DATABASE CONFIGURATION
# =============================================================================
# 
# Database settings define connection parameters and pooling behavior.
# Supports multiple database types: SQLite, PostgreSQL, MySQL, MongoDB, Redis.
#
[database]
host: @env("DB_HOST", "localhost")
port: @env("DB_PORT", 5432)
name: @env("DB_NAME", "tuskapp")
ssl: @if($environment == "production", true, false)

# =============================================================================
# SECURITY SETTINGS
# =============================================================================
# 
# Security configuration includes encryption, authentication, and access control.
# All sensitive values should be encrypted or loaded from secure environment variables.
#
[security]
encryption_key: @env.secure("ENCRYPTION_KEY")
session_secret: @env.secure("SESSION_SECRET")
cors_origin: @if($environment == "production", ["https://app.example.com"], ["*"])
EOF

config=$(tusk_parse comments-documentation.tsk)
echo "=== Configuration Documentation ==="
echo "App: $(tusk_get "$config" app_name) v$(tusk_get "$config" version)"
echo "Environment: $(tusk_get "$config" environment)"
echo "Server: $(tusk_get "$config" server.host):$(tusk_get "$config" server.port)"
echo "Database: $(tusk_get "$config" database.host):$(tusk_get "$config" database.port)"
```

## 🔧 Technical Comments

### Code Logic Documentation

```bash
#!/bin/bash
source tusk-bash.sh

cat > comments-technical.tsk << 'EOF'
[technical]
# Complex conditional logic with detailed explanation
# This section determines the optimal configuration based on:
# 1. Current system load (CPU usage)
# 2. Available memory
# 3. Environment type (dev/staging/prod)
# 4. Time of day (business hours vs after hours)

$current_load: @shell("top -bn1 | grep 'Cpu(s)' | awk '{print $2}' | cut -d'%' -f1")
$available_memory: @shell("free | grep Mem | awk '{printf \"%.2f\", $2/1024/1024}'")
$is_business_hours: @if(@date.hour() >= 9 && @date.hour() <= 17, true, false)

# Dynamic configuration based on system conditions
optimal_workers: @if($current_load > 80, 2,
                    @if($current_load > 50, 4, 8))

memory_limit: @if($available_memory < 2.0, "512MB",
                  @if($available_memory < 4.0, "1GB", "2GB"))

# Cache settings based on environment and time
cache_ttl: @if($environment == "production", "1h",
               @if($is_business_hours, "30m", "5m"))

# Logging level based on environment and load
log_level: @if($environment == "production", "error",
               @if($current_load > 70, "warn", "debug"))
EOF

config=$(tusk_parse comments-technical.tsk)
echo "=== Technical Configuration ==="
echo "Current Load: $(tusk_get "$config" technical.current_load)%"
echo "Available Memory: $(tusk_get "$config" technical.available_memory)GB"
echo "Business Hours: $(tusk_get "$config" technical.is_business_hours)"
echo "Optimal Workers: $(tusk_get "$config" technical.optimal_workers)"
echo "Memory Limit: $(tusk_get "$config" technical.memory_limit)"
echo "Cache TTL: $(tusk_get "$config" technical.cache_ttl)"
echo "Log Level: $(tusk_get "$config" technical.log_level)"
```

## 🚨 Warning and Important Comments

### Critical Information

```bash
#!/bin/bash
source tusk-bash.sh

cat > comments-warnings.tsk << 'EOF'
# ⚠️  WARNING: CRITICAL CONFIGURATION
# ===================================
# This section contains sensitive configuration that affects system security.
# DO NOT commit these values to version control without proper encryption.
# Always use environment variables or encrypted values for production.

[critical]
# 🔐 SECURITY: Database credentials
# These must be encrypted or loaded from secure environment variables
database_password: @encrypt(@env("DB_PASSWORD"), "AES-256-GCM")
api_secret: @env.secure("API_SECRET")

# 🚨 PRODUCTION: Performance settings
# These values are optimized for production environments
# Changing them may impact system performance and stability
max_connections: @if($environment == "production", 100, 10)
connection_timeout: @if($environment == "production", 30, 60)

# ⚡ PERFORMANCE: Cache configuration
# Cache settings affect memory usage and response times
# Monitor memory usage when adjusting these values
cache_size: @if($environment == "production", "1GB", "256MB")
cache_ttl: @if($environment == "production", "1h", "5m")

# 🔄 DEPLOYMENT: Update settings
# These control how the application handles updates and restarts
# Be careful when modifying in production environments
auto_restart: @if($environment == "production", false, true)
graceful_shutdown: @if($environment == "production", true, false)
EOF

config=$(tusk_parse comments-warnings.tsk)
echo "=== Critical Configuration ==="
echo "Database Password: [ENCRYPTED]"
echo "API Secret: [SECURE]"
echo "Max Connections: $(tusk_get "$config" critical.max_connections)"
echo "Cache Size: $(tusk_get "$config" critical.cache_size)"
echo "Auto Restart: $(tusk_get "$config" critical.auto_restart)"
```

## 📚 Best Practices

### Comment Organization

```bash
#!/bin/bash
source tusk-bash.sh

cat > comments-best-practices.tsk << 'EOF'
# =============================================================================
# TUSKLANG CONFIGURATION BEST PRACTICES
# =============================================================================
#
# This file demonstrates best practices for commenting TuskLang configurations:
#
# 1. Use clear, descriptive comments
# 2. Group related settings with section headers
# 3. Document complex logic and business rules
# 4. Include warnings for critical settings
# 5. Keep comments up-to-date with code changes
# 6. Use consistent formatting and style
#
# =============================================================================

# GLOBAL VARIABLES
# =============================================================================
# Define application-wide variables that are used throughout the configuration
# Use descriptive names and include default values where appropriate

$app_name: "TuskApp"
$version: "2.1.0"
$environment: @env("APP_ENV", "development")
$debug_mode: @env("DEBUG", "false")

# APPLICATION SETTINGS
# =============================================================================
# Core application configuration including server, database, and feature settings
# Each section should have a clear purpose and documented dependencies

[application]
# Server configuration for handling HTTP requests
server: {
    host: @if($environment == "production", "0.0.0.0", "localhost"),
    port: @if($environment == "production", 80, 8080),
    workers: @if($environment == "production", 4, 1)
}

# Database connection settings
# Supports multiple database types with automatic connection pooling
database: {
    host: @env("DB_HOST", "localhost"),
    port: @env("DB_PORT", 5432),
    ssl: @if($environment == "production", true, false)
}

# Feature flags for controlling application behavior
# Enable/disable features based on environment or user preferences
features: {
    new_ui: @env("FEATURE_NEW_UI", "false"),
    beta_api: @env("FEATURE_BETA_API", "false"),
    analytics: @env("FEATURE_ANALYTICS", "true")
}

# PERFORMANCE OPTIMIZATION
# =============================================================================
# Settings that affect application performance and resource usage
# Monitor system resources when adjusting these values

[performance]
# Cache configuration for improving response times
# Use appropriate TTL values based on data freshness requirements
cache: {
    enabled: true,
    ttl: @if($environment == "production", "1h", "5m"),
    size: @if($environment == "production", "1GB", "256MB")
}

# Connection pooling for database and external services
# Optimize based on expected load and available resources
pooling: {
    min_connections: @if($environment == "production", 5, 1),
    max_connections: @if($environment == "production", 20, 5),
    timeout: 30
}

# SECURITY CONFIGURATION
# =============================================================================
# Security settings for protecting sensitive data and preventing attacks
# Always use encrypted values for production environments

[security]
# Authentication and authorization settings
auth: {
    secret: @env.secure("AUTH_SECRET"),
    token_expiry: "24h",
    max_attempts: 5
}

# Input validation and sanitization
validation: {
    strict_mode: @if($environment == "production", true, false),
    max_input_size: "10MB",
    allowed_extensions: ["jpg", "png", "pdf"]
}
EOF

config=$(tusk_parse comments-best-practices.tsk)
echo "=== Best Practices Configuration ==="
echo "App: $(tusk_get "$config" app_name) v$(tusk_get "$config" version)"
echo "Environment: $(tusk_get "$config" environment)"
echo "Server: $(tusk_get "$config" application.server.host):$(tusk_get "$config" application.server.port)"
echo "Database SSL: $(tusk_get "$config" application.database.ssl)"
echo "Cache TTL: $(tusk_get "$config" performance.cache.ttl)"
echo "Max Connections: $(tusk_get "$config" performance.pooling.max_connections)"
```

## 🔄 Comment Maintenance

### Keeping Comments Current

```bash
#!/bin/bash
source tusk-bash.sh

cat > comments-maintenance.tsk << 'EOF'
# =============================================================================
# COMMENT MAINTENANCE GUIDELINES
# =============================================================================
#
# When updating this configuration file, remember to:
#
# 1. Update comments when changing values or logic
# 2. Remove outdated or incorrect comments
# 3. Add comments for new features or settings
# 4. Review comments during code reviews
# 5. Use consistent formatting and style
#
# Last Updated: 2024-12-19
# Updated By: Development Team
# Changes: Added performance optimization section
# =============================================================================

[maintenance]
# Version tracking for configuration changes
version: "2.1.0"
last_updated: "2024-12-19"
updated_by: @env("USER", "unknown")

# Change log for tracking modifications
changelog: [
    "2024-12-19: Added performance optimization settings",
    "2024-12-18: Updated security configuration",
    "2024-12-17: Initial configuration setup"
]

# Documentation status
documentation: {
    complete: true,
    reviewed: true,
    tested: true
}
EOF

config=$(tusk_parse comments-maintenance.tsk)
echo "=== Maintenance Information ==="
echo "Version: $(tusk_get "$config" maintenance.version)"
echo "Last Updated: $(tusk_get "$config" maintenance.last_updated)"
echo "Updated By: $(tusk_get "$config" maintenance.updated_by)"
echo "Documentation Complete: $(tusk_get "$config" maintenance.documentation.complete)"
```

## 🎯 What You've Learned

In this comments guide, you've mastered:

✅ **Single-line comments** - Basic commenting with # symbol  
✅ **Inline comments** - Comments after values on the same line  
✅ **Documentation comments** - Comprehensive section documentation  
✅ **Technical comments** - Explaining complex logic and business rules  
✅ **Warning comments** - Highlighting critical and important information  
✅ **Best practices** - Organizing and maintaining comments effectively  
✅ **Comment maintenance** - Keeping comments current and accurate  

## 🚀 Next Steps

Ready to explore more TuskLang features?

1. **Key-Value Basics** → [007-key-value-basics-bash.md](007-key-value-basics-bash.md)
2. **Strings** → [008-strings-bash.md](008-strings-bash.md)
3. **Numbers** → [009-numbers-bash.md](009-numbers-bash.md)

## 💡 Pro Tips

- **Be descriptive** - Explain the "why" not just the "what"
- **Keep comments current** - Update comments when changing code
- **Use consistent style** - Follow the same formatting throughout
- **Document complex logic** - Explain business rules and conditions
- **Include warnings** - Highlight critical and security-sensitive settings
- **Group related settings** - Use section headers to organize configuration

---

**Document your configuration with style! 💬** 