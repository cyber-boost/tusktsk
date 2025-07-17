# 🏷️ TuskLang Bash Variable Naming Guide

**"We don't bow to any king" – Name your variables with purpose and clarity.**

Variable naming in TuskLang is about creating readable, maintainable, and self-documenting configurations. Whether you're building simple scripts or complex enterprise systems, good variable naming makes your TuskLang configurations powerful, understandable, and professional.

## 🎯 Why Variable Naming Matters
Good variable naming provides:
- **Readability** - Clear, understandable code
- **Maintainability** - Easy to modify and debug
- **Self-documentation** - Variables explain themselves
- **Team collaboration** - Consistent naming across projects
- **Professional quality** - Industry-standard practices

## 📝 Naming Conventions

### Descriptive Names
```ini
[good_naming]
# ✅ Good: Clear and descriptive
database_connection_timeout: 30
maximum_retry_attempts: 3
ssl_certificate_path: "/etc/ssl/certs/server.crt"

# ❌ Bad: Unclear and ambiguous
timeout: 30
retries: 3
cert: "/etc/ssl/certs/server.crt"
```

### Consistent Patterns
```ini
[consistent]
# ✅ Good: Consistent naming patterns
server_host: "localhost"
server_port: 8080
server_ssl: true

# ❌ Bad: Inconsistent patterns
host: "localhost"
serverPort: 8080
ssl_enabled: true
```

### Environment-Specific Naming
```ini
[environment]
# ✅ Good: Environment-specific prefixes
production_server_host: "0.0.0.0"
development_server_host: "localhost"
staging_server_host: "staging.example.com"

# ❌ Bad: Generic names that don't indicate environment
server_host: "0.0.0.0"
```

## 🚀 Quick Start Example

```bash
#!/bin/bash
source tusk-bash.sh

cat > naming-example.tsk << 'EOF'
[application]
$application_name: "TuskApp"
$application_version: "2.1.0"
$application_environment: @env("APP_ENV", "development")

[server_configuration]
$server_host: @if($application_environment == "production", "0.0.0.0", "localhost")
$server_port: @if($application_environment == "production", 80, 8080)
$server_ssl_enabled: @if($application_environment == "production", true, false)

[database_configuration]
$database_host: @env("DB_HOST", "localhost")
$database_port: @env("DB_PORT", "5432")
$database_name: @env("DB_NAME", "tuskapp")
$database_connection_timeout: 30
$database_max_connections: 100
EOF

config=$(tusk_parse naming-example.tsk)
echo "Application: $(tusk_get "$config" application.application_name) v$(tusk_get "$config" application.application_version)"
echo "Server: $(tusk_get "$config" server_configuration.server_host):$(tusk_get "$config" server_configuration.server_port)"
echo "Database: $(tusk_get "$config" database_configuration.database_host):$(tusk_get "$config" database_configuration.database_port)"
```

## 🔗 Real-World Use Cases

### 1. API Configuration
```ini
[api_configuration]
$api_base_url: "https://api.example.com"
$api_version: "v1"
$api_timeout_seconds: 30
$api_max_retry_attempts: 3
$api_authentication_token: @env("API_TOKEN")

# Build derived values
$api_endpoint_url: "${api_base_url}/api/${api_version}"
$api_users_endpoint: "${api_endpoint_url}/users"
$api_posts_endpoint: "${api_endpoint_url}/posts"
```

### 2. Database Configuration
```ini
[database_configuration]
$database_host: @env("DB_HOST", "localhost")
$database_port: @env("DB_PORT", "5432")
$database_name: @env("DB_NAME", "myapp")
$database_username: @env("DB_USER", "postgres")
$database_password: @env("DB_PASSWORD")
$database_ssl_enabled: @env("DB_SSL", "true")
$database_connection_pool_size: 10
$database_query_timeout_seconds: 30
```

### 3. Logging Configuration
```ini
[logging_configuration]
$logging_level: @env("LOG_LEVEL", "info")
$logging_file_path: "/var/log/application.log"
$logging_max_file_size_megabytes: 100
$logging_backup_file_count: 5
$logging_enable_console_output: true
$logging_enable_file_output: true
```

### 4. Security Configuration
```ini
[security_configuration]
$security_encryption_key: @env.secure("ENCRYPTION_KEY")
$security_session_secret: @env.secure("SESSION_SECRET")
$security_password_minimum_length: 8
$security_max_login_attempts: 5
$security_session_timeout_minutes: 30
$security_require_two_factor_authentication: false
```

## 🧠 Advanced Naming Patterns

### Namespace Prefixes
```ini
[namespaced]
# Use prefixes to group related variables
$app_name: "TuskApp"
$app_version: "2.1.0"
$app_environment: @env("APP_ENV", "development")

$db_host: @env("DB_HOST", "localhost")
$db_port: @env("DB_PORT", "5432")
$db_name: @env("DB_NAME", "tuskapp")

$api_base_url: "https://api.example.com"
$api_timeout: 30
$api_retries: 3
```

### Boolean Naming
```ini
[boolean_naming]
# ✅ Good: Clear boolean names
$feature_new_ui_enabled: true
$feature_analytics_enabled: false
$debug_mode_enabled: @env("DEBUG", "false")
$maintenance_mode_active: false

# ❌ Bad: Unclear boolean names
$new_ui: true
$analytics: false
$debug: @env("DEBUG", "false")
$maintenance: false
```

### Numeric Naming
```ini
[numeric_naming]
# ✅ Good: Include units in names
$connection_timeout_seconds: 30
$file_size_limit_megabytes: 100
$cache_ttl_minutes: 15
$rate_limit_requests_per_minute: 1000

# ❌ Bad: No units specified
$timeout: 30
$size_limit: 100
$cache_ttl: 15
$rate_limit: 1000
```

### Array and Object Naming
```ini
[collection_naming]
# ✅ Good: Plural names for collections
$allowed_ip_addresses: ["192.168.1.1", "10.0.0.1"]
$supported_languages: ["en", "es", "fr"]
$database_connection_settings: {
    "host": "localhost",
    "port": 5432,
    "ssl": true
}

# ❌ Bad: Singular names for collections
$allowed_ip: ["192.168.1.1", "10.0.0.1"]
$language: ["en", "es", "fr"]
$database_setting: {
    "host": "localhost",
    "port": 5432,
    "ssl": true
}
```

## 🛡️ Security & Performance Notes
- **Sensitive data:** Never include sensitive information in variable names.
- **Length limits:** Keep variable names reasonably short while remaining descriptive.
- **Special characters:** Avoid special characters that might cause parsing issues.
- **Reserved words:** Avoid using TuskLang reserved words as variable names.

## 🐞 Troubleshooting
- **Parsing errors:** Check for special characters or reserved words in variable names.
- **Scope issues:** Ensure variable names are unique within their scope.
- **Case sensitivity:** Be consistent with case (snake_case recommended).
- **Length issues:** Very long variable names can impact readability.

## 💡 Best Practices
- **Use snake_case:** Use lowercase with underscores for multi-word names.
- **Be descriptive:** Choose names that clearly describe the variable's purpose.
- **Include units:** Include units in names for numeric values.
- **Use prefixes:** Use prefixes to group related variables.
- **Avoid abbreviations:** Use full words instead of abbreviations.
- **Be consistent:** Use consistent naming patterns throughout your project.

## 🔗 Cross-References
- [References](019-references-bash.md)
- [Key-Value Basics](007-key-value-basics-bash.md)
- [Best Practices](023-best-practices-bash.md)

---

**Master variable naming in TuskLang and create professional, maintainable configurations. 🏷️** 