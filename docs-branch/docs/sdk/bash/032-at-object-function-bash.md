# 🏗️ TuskLang Bash @object Function Guide

**"We don't bow to any king" – Objects are your configuration's structure.**

The @object function in TuskLang is your object manipulation powerhouse, enabling dynamic object operations, property access, transformation, and composition directly within your configuration files. Whether you're building complex data structures, managing configuration hierarchies, or creating dynamic APIs, @object provides the flexibility and power to handle structured data efficiently.

## 🎯 What is @object?
The @object function performs object operations and manipulations in TuskLang. It provides:
- **Object creation** - Create objects from various sources
- **Property access** - Get, set, and manipulate object properties
- **Object transformation** - Merge, clone, transform objects
- **Object analysis** - Keys, values, size, property existence
- **Dynamic objects** - Objects that adapt to data changes

## 📝 Basic @object Syntax

### Object Creation
```ini
[creation]
# Create objects from different sources
static_object: @object({"name": "John", "age": 30, "city": "New York"})
from_query: @object.from_query("SELECT name, age, city FROM users WHERE id = 1")
from_array: @object.from_array(["name", "age", "city"], ["John", 30, "New York"])
merged: @object.merge({"a": 1}, {"b": 2}, {"c": 3})
```

### Property Access
```ini
[access]
# Access object properties
$user: {"name": "Alice", "age": 25, "email": "alice@example.com"}
user_name: @object.get($user, "name")
user_age: @object.get($user, "age")
has_email: @object.has($user, "email")
all_keys: @object.keys($user)
all_values: @object.values($user)
```

### Object Transformation
```ini
[transformation]
# Transform objects
$config: {"debug": true, "log_level": "info", "port": 8080}
transformed: @object.map($config, "key + '_' + value")
filtered: @object.filter($config, "key != 'debug'")
cloned: @object.clone($config)
```

## 🚀 Quick Start Example

```bash
#!/bin/bash
source tusk-bash.sh

cat > object-quickstart.tsk << 'EOF'
[basic_objects]
# Basic object operations
user: @object({"name": "John Doe", "age": 30, "email": "john@example.com"})
server: @object({"hostname": "web1", "ip": "192.168.1.10", "port": 80})
config: @object({"debug": true, "log_level": "info", "timeout": 30})

[object_analysis]
# Analyze objects
user_keys: @object.keys($user)
user_values: @object.values($user)
user_size: @object.size($user)
has_age: @object.has($user, "age")
has_phone: @object.has($user, "phone")

[object_access]
# Access object properties
user_name: @object.get($user, "name")
user_age: @object.get($user, "age")
server_hostname: @object.get($server, "hostname")
config_debug: @object.get($config, "debug")

[object_transformation]
# Transform objects
user_uppercase: @object.map($user, "key + ': ' + value.upper()")
server_info: @object.filter($server, "key != 'port'")
config_safe: @object.clone($config)
EOF

config=$(tusk_parse object-quickstart.tsk)

echo "=== Basic Objects ==="
echo "User: $(tusk_get "$config" basic_objects.user)"
echo "Server: $(tusk_get "$config" basic_objects.server)"
echo "Config: $(tusk_get "$config" basic_objects.config)"

echo ""
echo "=== Object Analysis ==="
echo "User Keys: $(tusk_get "$config" object_analysis.user_keys)"
echo "User Values: $(tusk_get "$config" object_analysis.user_values)"
echo "User Size: $(tusk_get "$config" object_analysis.user_size)"
echo "Has Age: $(tusk_get "$config" object_analysis.has_age)"
echo "Has Phone: $(tusk_get "$config" object_analysis.has_phone)"

echo ""
echo "=== Object Access ==="
echo "User Name: $(tusk_get "$config" object_access.user_name)"
echo "User Age: $(tusk_get "$config" object_access.user_age)"
echo "Server Hostname: $(tusk_get "$config" object_access.server_hostname)"
echo "Config Debug: $(tusk_get "$config" object_access.config_debug)"

echo ""
echo "=== Object Transformation ==="
echo "User Uppercase: $(tusk_get "$config" object_transformation.user_uppercase)"
echo "Server Info: $(tusk_get "$config" object_transformation.server_info)"
echo "Config Safe: $(tusk_get "$config" object_transformation.config_safe)"
```

## 🔗 Real-World Use Cases

### 1. API Configuration Management
```ini
[api_configuration]
# Dynamic API configuration objects
$base_config: @object({
    "timeout": 30,
    "retries": 3,
    "user_agent": "TuskApp/1.0"
})

# Environment-specific configurations
$dev_config: @object({
    "base_url": "https://api-dev.example.com",
    "debug": true,
    "log_level": "debug"
})

$prod_config: @object({
    "base_url": "https://api.example.com",
    "debug": false,
    "log_level": "error"
})

# Merge configurations
environment: @env("APP_ENV", "development")
env_config: @if($environment == "production", $prod_config, $dev_config)
final_config: @object.merge($base_config, $env_config)

# API endpoint configurations
endpoints: @object({
    "users": @object({
        "path": "/api/v1/users",
        "method": "GET",
        "auth_required": true
    }),
    "posts": @object({
        "path": "/api/v1/posts",
        "method": "POST",
        "auth_required": true
    }),
    "health": @object({
        "path": "/health",
        "method": "GET",
        "auth_required": false
    })
})
```

### 2. Database Connection Management
```ini
[database_management]
# Database configuration objects
$db_configs: @object({
    "development": @object({
        "host": "localhost",
        "port": 5432,
        "database": "tuskapp_dev",
        "username": "dev_user",
        "password": @env("DB_PASSWORD_DEV"),
        "pool_size": 5
    }),
    "staging": @object({
        "host": "staging-db.example.com",
        "port": 5432,
        "database": "tuskapp_staging",
        "username": "staging_user",
        "password": @env("DB_PASSWORD_STAGING"),
        "pool_size": 10
    }),
    "production": @object({
        "host": "prod-db.example.com",
        "port": 5432,
        "database": "tuskapp_prod",
        "username": "prod_user",
        "password": @env("DB_PASSWORD_PROD"),
        "pool_size": 20
    })
})

# Get current environment configuration
current_env: @env("APP_ENV", "development")
current_db_config: @object.get($db_configs, $current_env)

# Connection string building
connection_string: @string.format("postgresql://{username}:{password}@{host}:{port}/{database}", $current_db_config)

# Connection pool configuration
pool_config: @object({
    "min_connections": @math($current_db_config.pool_size * 0.5),
    "max_connections": $current_db_config.pool_size,
    "connection_timeout": 30,
    "idle_timeout": 300
})
```

### 3. User Profile and Settings Management
```ini
[user_management]
# User profile objects from database
$user_profiles: @object.from_query("SELECT id, username, email, profile_data FROM users WHERE active = 1")

# Default user settings
$default_settings: @object({
    "theme": "light",
    "language": "en",
    "notifications": @object({
        "email": true,
        "push": false,
        "sms": false
    }),
    "privacy": @object({
        "profile_public": true,
        "show_email": false,
        "show_location": false
    })
})

# User-specific settings
$user_id: @env("USER_ID", "123")
$user_profile: @object.get($user_profiles, $user_id)
$user_settings: @object.merge($default_settings, $user_profile.settings)

# Settings validation
valid_themes: @array(["light", "dark", "auto"])
valid_languages: @array(["en", "es", "fr", "de"])

# Validate user settings
is_valid_theme: @array.contains($valid_themes, $user_settings.theme)
is_valid_language: @array.contains($valid_languages, $user_settings.language)

# Apply validation
final_settings: @object.merge($user_settings, {
    "theme": @if($is_valid_theme, $user_settings.theme, "light"),
    "language": @if($is_valid_language, $user_settings.language, "en")
})
```

### 4. Service Discovery and Load Balancing
```ini
[service_discovery]
# Service registry objects
$service_registry: @object({
    "web": @object({
        "instances": @array.from_query("SELECT hostname, port, health_status FROM servers WHERE service_type = 'web' AND active = 1"),
        "load_balancer": "round_robin",
        "health_check_path": "/health",
        "timeout": 10
    }),
    "api": @object({
        "instances": @array.from_query("SELECT hostname, port, health_status FROM servers WHERE service_type = 'api' AND active = 1"),
        "load_balancer": "least_connections",
        "health_check_path": "/api/health",
        "timeout": 15
    }),
    "database": @object({
        "instances": @array.from_query("SELECT hostname, port, health_status FROM servers WHERE service_type = 'database' AND active = 1"),
        "load_balancer": "primary_backup",
        "health_check_path": "/db/health",
        "timeout": 30
    })
})

# Service health analysis
$service_health: @object.map($service_registry, {
    "service": key,
    "total_instances": @array.length(value.instances),
    "healthy_instances": @array.length(@array.filter(value.instances, "item.health_status == 'healthy'")),
    "unhealthy_instances": @array.length(@array.filter(value.instances, "item.health_status != 'healthy'"))
})

# Load balancer configuration
$lb_config: @object.map($service_registry, {
    "service": key,
    "upstream": key + "_servers",
    "algorithm": value.load_balancer,
    "health_check": value.health_check_path,
    "timeout": value.timeout,
    "servers": @array.map(value.instances, "item.hostname + ':' + item.port")
})
```

## 🧠 Advanced @object Patterns

### Complex Object Operations
```bash
#!/bin/bash
source tusk-bash.sh

cat > complex-objects.tsk << 'EOF'
[complex_operations]
# Complex object operations
$base_config: @object({
    "app_name": "TuskApp",
    "version": "2.1.0",
    "features": @array(["auth", "api", "database", "cache"])
})

$env_configs: @object({
    "development": @object({
        "debug": true,
        "log_level": "debug",
        "database_url": "sqlite:///dev.db"
    }),
    "production": @object({
        "debug": false,
        "log_level": "error",
        "database_url": "postgresql://prod:pass@localhost/prod"
    })
})

# Deep object merging
current_env: @env("APP_ENV", "development")
env_config: @object.get($env_configs, $current_env)
final_config: @object.merge($base_config, $env_config)

# Object transformation with conditions
transformed_config: @object.map($final_config, @if(key == "features", 
    @array.map(value, "item.upper()"), 
    @if(key == "debug", @string.format("DEBUG_{}", value.upper()), value)
))

# Nested object access
database_config: @object({
    "connection": @object({
        "url": $final_config.database_url,
        "pool_size": @if($current_env == "production", 20, 5),
        "timeout": 30
    }),
    "migrations": @object({
        "auto_run": @if($current_env == "production", false, true),
        "backup_before": true
    })
})
EOF

config=$(tusk_parse complex-objects.tsk)
echo "Final Config: $(tusk_get "$config" complex_operations.final_config)"
echo "Transformed Config: $(tusk_get "$config" complex_operations.transformed_config)"
echo "Database Config: $(tusk_get "$config" complex_operations.database_config)"
```

### Object-Based Configuration Templates
```ini
[config_templates]
# Template-based configuration generation
$template_config: @object({
    "web_server": @object({
        "type": "nginx",
        "port": 80,
        "ssl_port": 443,
        "config_template": "nginx.conf.template"
    }),
    "api_server": @object({
        "type": "nodejs",
        "port": 3000,
        "config_template": "api.conf.template"
    }),
    "database": @object({
        "type": "postgresql",
        "port": 5432,
        "config_template": "postgresql.conf.template"
    })
})

# Generate configurations for each service
$services: @array(["web_server", "api_server", "database"])
$generated_configs: @object.map($template_config, {
    "service": key,
    "config": @object.merge(value, {
        "config_file": "/etc/" + key + "/" + value.config_template,
        "log_file": "/var/log/" + key + ".log",
        "pid_file": "/var/run/" + key + ".pid"
    })
})

# Environment-specific overrides
$env_overrides: @object({
    "development": @object({
        "web_server": @object({"port": 8080, "ssl_port": 8443}),
        "api_server": @object({"port": 3001}),
        "database": @object({"port": 5433})
    }),
    "production": @object({
        "web_server": @object({"port": 80, "ssl_port": 443}),
        "api_server": @object({"port": 3000}),
        "database": @object({"port": 5432})
    })
})

# Apply environment overrides
current_env: @env("APP_ENV", "development")
env_override: @object.get($env_overrides, $current_env)
final_service_configs: @object.merge($generated_configs, $env_override)
```

### Dynamic Object Composition
```ini
[dynamic_composition]
# Compose objects dynamically
$base_components: @object({
    "authentication": @object({
        "enabled": true,
        "type": "jwt",
        "secret": @env("JWT_SECRET")
    }),
    "logging": @object({
        "enabled": true,
        "level": "info",
        "format": "json"
    }),
    "caching": @object({
        "enabled": false,
        "type": "redis",
        "ttl": 3600
    })
})

# Feature flags from database
$feature_flags: @object.from_query("SELECT name, enabled, config FROM feature_flags WHERE active = 1")

# Dynamic component composition
$enabled_features: @object.filter($feature_flags, "value.enabled == true")
$dynamic_components: @object.map($enabled_features, {
    "name": key,
    "config": @object.merge($base_components[key], value.config)
})

# Final application configuration
$app_config: @object({
    "components": $dynamic_components,
    "environment": @env("APP_ENV"),
    "version": @env("APP_VERSION"),
    "startup_time": @date("Y-m-d H:i:s")
})
```

## 🛡️ Security & Performance Notes
- **Input validation:** Always validate object inputs to prevent injection attacks
- **Property access:** Use safe property access methods to avoid errors
- **Memory usage:** Be careful with large objects that could consume significant memory
- **Performance:** Cache expensive object operations to improve performance
- **Type safety:** Ensure object properties have consistent types for operations

## 🐞 Troubleshooting
- **Missing properties:** Handle missing object properties gracefully
- **Type mismatches:** Ensure consistent data types in object operations
- **Memory issues:** Monitor memory usage with large objects
- **Performance problems:** Cache expensive object operations
- **Property errors:** Validate object properties before accessing them

## 💡 Best Practices
- **Validate inputs:** Always validate object inputs before processing
- **Handle missing properties:** Account for missing properties in your logic
- **Cache expensive operations:** Cache complex object operations
- **Use appropriate data types:** Ensure consistent types for object properties
- **Document object structures:** Document expected object formats and contents
- **Test thoroughly:** Test object operations with various inputs and edge cases

## 🔗 Cross-References
- [@ Operator Introduction](024-at-operator-intro-bash.md)
- [Objects](013-objects-bash.md)
- [Object Operations](067-object-operations-bash.md)
- [Collections](066-array-operations-bash.md)

---

**Master @object in TuskLang and build structured configurations with power and precision. 🏗️** 