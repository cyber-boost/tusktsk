# ⚙️ TuskLang Bash @ini Function Guide

**"We don't bow to any king" – INI is your configuration's foundation.**

The @ini function in TuskLang is your INI processing powerhouse, enabling dynamic INI parsing, generation, and manipulation directly within your configuration files. Whether you're working with legacy systems, Windows configurations, or application settings, @ini provides the flexibility and power to handle structured data with simplicity and reliability.

## 🎯 What is @ini?
The @ini function provides INI processing capabilities in TuskLang. It offers:
- **INI parsing** - Parse INI strings and files
- **INI generation** - Create INI documents dynamically
- **INI transformation** - Transform INI data between formats
- **Configuration management** - Manage complex configuration structures
- **Legacy system support** - Handle traditional INI file formats

## 📝 Basic @ini Syntax

### Simple INI Parsing
```ini
[simple_ini]
# Parse INI string
$ini_data: |
  [user]
  name = John
  email = john@example.com
  age = 30
  active = true
parsed_ini: @ini.parse($ini_data)
user_name: @ini.get($parsed_ini, "user.name")
user_email: @ini.get($parsed_ini, "user.email")
user_age: @ini.get($parsed_ini, "user.age")
```

### INI File Processing
```ini
[ini_file_processing]
# Parse INI file
config_ini: @ini.parse(@file.read("/etc/app/config.ini"))
api_config: @ini.get($config_ini, "api")
database_config: @ini.get($config_ini, "database")
```

### INI Generation
```ini
[ini_generation]
# Generate INI dynamically
$user_data: {
    "user": {
        "name": "Alice",
        "email": "alice@example.com",
        "age": 30,
        "roles": "admin,user"
    }
}
user_ini: @ini.generate($user_data)

# Generate complex INI
$config_data: {
    "database": {
        "host": "localhost",
        "port": "3306",
        "name": "app_db"
    },
    "api": {
        "url": "https://api.example.com",
        "timeout": "30"
    }
}
config_ini: @ini.generate($config_data)
```

## 🚀 Quick Start Example

```bash
#!/bin/bash
source tusk-bash.sh

cat > ini-quickstart.tsk << 'EOF'
[ini_parsing]
# Parse INI data
$ini_string: |
  [app]
  name = TuskLang
  version = 2.1.0
  
  [database]
  host = localhost
  port = 3306
  
  [api]
  url = https://api.example.com
  timeout = 30
parsed_config: @ini.parse($ini_string)

# Extract data
app_name: @ini.get($parsed_config, "app.name")
app_version: @ini.get($parsed_config, "app.version")
db_host: @ini.get($parsed_config, "database.host")
api_url: @ini.get($parsed_config, "api.url")

[ini_generation]
# Generate INI
$user_info: {
    "user": {
        "name": "John Doe",
        "email": "john@example.com",
        "role": "admin",
        "permissions": "read,write,delete"
    }
}
user_ini: @ini.generate($user_info)

# Generate nested INI
$nested_data: {
    "server": {
        "name": "web-server-01",
        "ip": "192.168.1.100",
        "services": "web,ssl"
    }
}
server_ini: @ini.generate($nested_data)

[ini_transformation]
# Transform INI data
$source_ini: |
  [users]
  user1 = Alice
  user2 = Bob
  user3 = Charlie
transformed_ini: @ini.transform($source_ini, "users", {
    "template": {
        "person_name": "$.value",
        "user_id": "$.key"
    }
})
EOF

config=$(tusk_parse ini-quickstart.tsk)

echo "=== INI Parsing ==="
echo "App Name: $(tusk_get "$config" ini_parsing.app_name)"
echo "App Version: $(tusk_get "$config" ini_parsing.app_version)"
echo "Database Host: $(tusk_get "$config" ini_parsing.db_host)"
echo "API URL: $(tusk_get "$config" ini_parsing.api_url)"

echo ""
echo "=== INI Generation ==="
echo "User INI: $(tusk_get "$config" ini_generation.user_ini)"
echo "Server INI: $(tusk_get "$config" ini_generation.server_ini)"

echo ""
echo "=== INI Transformation ==="
echo "Transformed INI: $(tusk_get "$config" ini_transformation.transformed_ini)"
```

## 🔗 Real-World Use Cases

### 1. Windows Configuration Management
```ini
[windows_config]
# Generate Windows INI configuration
$windows_config: {
    "system": {
        "computer_name": "TUSKLANG-SERVER",
        "domain": "tusklang.local",
        "timezone": "UTC"
    },
    "network": {
        "ip_address": "192.168.1.100",
        "subnet_mask": "255.255.255.0",
        "gateway": "192.168.1.1",
        "dns_servers": "8.8.8.8,8.8.4.4"
    },
    "services": {
        "web_server": "enabled",
        "database": "enabled",
        "backup": "disabled"
    }
}

windows_ini: @ini.generate($windows_config)
@file.write("C:/Windows/System32/config.ini", $windows_ini)

# Generate application-specific INI
$app_config: {
    "application": {
        "name": "TuskLang App",
        "version": "2.1.0",
        "install_path": "C:/Program Files/TuskLang"
    },
    "database": {
        "connection_string": "Server=localhost;Database=tusklang;Trusted_Connection=true",
        "timeout": "30"
    },
    "logging": {
        "level": "info",
        "file_path": "C:/Logs/tusklang.log",
        "max_size": "10MB"
    }
}

app_ini: @ini.generate($app_config)
@file.write("C:/Program Files/TuskLang/config.ini", $app_ini)
```

### 2. Legacy System Configuration
```ini
[legacy_system]
# Parse legacy system configuration
$legacy_ini: @ini.parse(@file.read("/etc/legacy-system/config.ini"))

# Extract legacy configuration
$system_config: {
    "hostname": @ini.get($legacy_ini, "system.hostname"),
    "ip_address": @ini.get($legacy_ini, "network.ip_address"),
    "port": @ini.get($legacy_ini, "network.port")
}

# Modernize legacy configuration
$modern_config: {
    "system": {
        "hostname": $system_config.hostname,
        "environment": "production"
    },
    "network": {
        "ip_address": $system_config.ip_address,
        "port": $system_config.port,
        "protocol": "tcp"
    },
    "monitoring": {
        "enabled": "true",
        "interval": "60"
    }
}

modern_ini: @ini.generate($modern_config)
@file.write("/etc/modern-system/config.ini", $modern_ini)
```

### 3. Application Configuration Management
```ini
[app_configuration]
# Parse application configuration
$app_ini: @ini.parse(@file.read("/etc/app/config.ini"))

# Extract configuration sections
$app_config: {
    "name": @ini.get($app_ini, "app.name"),
    "version": @ini.get($app_ini, "app.version"),
    "environment": @ini.get($app_ini, "app.environment")
}

$database_config: {
    "host": @ini.get($app_ini, "database.host"),
    "port": @ini.get($app_ini, "database.port"),
    "name": @ini.get($app_ini, "database.name"),
    "username": @ini.get($app_ini, "database.username"),
    "password": @ini.get($app_ini, "database.password")
}

# Generate updated configuration
$updated_config: {
    "app": $app_config,
    "database": $database_config,
    "timestamp": @date("Y-m-d H:i:s")
}

updated_ini: @ini.generate($updated_config)
@file.write("/etc/app/updated-config.ini", $updated_ini)
```

### 4. Development Environment Configuration
```ini
[dev_environment]
# Generate development environment configuration
$dev_config: {
    "development": {
        "debug": "true",
        "log_level": "debug",
        "port": "3000"
    },
    "database": {
        "host": "localhost",
        "port": "5432",
        "name": "tusklang_dev",
        "pool_size": "10"
    },
    "redis": {
        "host": "localhost",
        "port": "6379",
        "db": "0"
    },
    "features": {
        "hot_reload": "true",
        "auto_migrate": "true",
        "seed_data": "true"
    }
}

dev_ini: @ini.generate($dev_config)
@file.write("config/development.ini", $dev_ini)

# Generate production configuration
$prod_config: {
    "production": {
        "debug": "false",
        "log_level": "info",
        "port": "80"
    },
    "database": {
        "host": @env("DB_HOST"),
        "port": @env("DB_PORT", "5432"),
        "name": @env("DB_NAME"),
        "pool_size": "50"
    },
    "redis": {
        "host": @env("REDIS_HOST"),
        "port": @env("REDIS_PORT", "6379"),
        "db": "0"
    },
    "features": {
        "hot_reload": "false",
        "auto_migrate": "false",
        "seed_data": "false"
    }
}

prod_ini: @ini.generate($prod_config)
@file.write("config/production.ini", $prod_ini)
```

## 🧠 Advanced @ini Patterns

### INI Template Processing
```ini
[ini_templates]
# Process INI templates with dynamic data
$template_ini: |
  [app]
  name = {{ .AppName }}
  version = {{ .Version }}
  environment = {{ .Environment }}
  
  [database]
  host = {{ .DatabaseHost }}
  port = {{ .DatabasePort }}
  name = {{ .DatabaseName }}
  
  [api]
  url = {{ .APIURL }}
  timeout = {{ .APITimeout }}

# Template variables
$template_vars: {
    "AppName": "tusklang",
    "Version": "2.1.0",
    "Environment": @env("ENVIRONMENT", "development"),
    "DatabaseHost": @env("DB_HOST", "localhost"),
    "DatabasePort": @env("DB_PORT", "3306"),
    "DatabaseName": @env("DB_NAME", "tusklang"),
    "APIURL": @env("API_URL", "https://api.example.com"),
    "APITimeout": @env("API_TIMEOUT", "30")
}

# Process template
processed_ini: @ini.process_template($template_ini, $template_vars)
@file.write("/etc/app/config.ini", $processed_ini)
```

### INI Data Validation
```ini
[ini_validation]
# Validate INI against schema
$ini_document: @file.read("/var/config/app-config.ini")
$schema_file: @file.read("/etc/schemas/app-schema.ini")

# Validate INI structure
validation_result: @ini.validate($ini_document, $schema_file)

# Handle validation errors
@if($validation_result.valid, {
    "action": "process_ini",
    "data": @ini.parse($ini_document)
}, {
    "action": "handle_validation_errors",
    "errors": $validation_result.errors,
    "timestamp": @date("Y-m-d H:i:s")
})
```

### INI Configuration Inheritance
```ini
[ini_inheritance]
# Base configuration
$base_config: {
    "app": {
        "name": "tusklang",
        "version": "2.1.0"
    },
    "database": {
        "host": "localhost",
        "port": "3306"
    }
}

# Environment-specific overrides
$dev_config: {
    "database": {
        "host": "dev-db.example.com",
        "port": "3306"
    },
    "logging": {
        "level": "debug"
    }
}

$prod_config: {
    "database": {
        "host": "prod-db.example.com",
        "port": "3306"
    },
    "logging": {
        "level": "info"
    }
}

# Merge configurations
$environment: @env("ENVIRONMENT", "development")
$final_config: @if($environment == "production", 
    @ini.merge($base_config, $prod_config),
    @ini.merge($base_config, $dev_config)
)

final_ini: @ini.generate($final_config)
@file.write("/etc/app/config-" + $environment + ".ini", $final_ini)
```

### INI Section and Key Processing
```ini
[ini_sections_keys]
# Process INI sections and keys
$complex_ini: |
  [database]
  host = db-01.example.com
  port = 3306
  name = tusklang
  
  [database.replica]
  host = db-02.example.com
  port = 3306
  
  [services]
  web = enabled
  ssl = enabled
  backup = disabled
  
  [users]
  admin = John Doe
  user1 = Alice Smith
  user2 = Bob Johnson

parsed_complex: @ini.parse($complex_ini)

# Extract database configuration
$db_config: {
    "primary": {
        "host": @ini.get($parsed_complex, "database.host"),
        "port": @ini.get($parsed_complex, "database.port"),
        "name": @ini.get($parsed_complex, "database.name")
    },
    "replica": {
        "host": @ini.get($parsed_complex, "database.replica.host"),
        "port": @ini.get($parsed_complex, "database.replica.port")
    }
}

# Extract service status
$services: @ini.get_all($parsed_complex, "services")
$service_status: @array.map($services, {
    "service": item.key,
    "status": item.value
})

# Extract user information
$users: @ini.get_all($parsed_complex, "users")
$user_data: @array.map($users, {
    "username": item.key,
    "full_name": item.value
})

# Generate processed configuration
$processed_config: {
    "database": $db_config,
    "services": $service_status,
    "users": $user_data,
    "processed_at": @date("Y-m-d H:i:s")
}

processed_ini: @ini.generate($processed_config)
```

## 🛡️ Security & Performance Notes
- **INI injection:** Validate and sanitize INI input to prevent injection attacks
- **Memory usage:** Monitor memory consumption when processing large INI files
- **Schema validation:** Always validate INI against schemas when possible
- **Template security:** Sanitize template variables to prevent code injection
- **File permissions:** Ensure proper file permissions for INI operations
- **Performance optimization:** Use efficient INI processing for large files

## 🐞 Troubleshooting
- **Parsing errors:** Check INI syntax and structure
- **Schema validation:** Ensure INI conforms to expected schema
- **Template errors:** Verify template syntax and variable substitution
- **Memory issues:** Optimize INI processing for large files
- **Encoding problems:** Ensure proper character encoding (UTF-8)

## 💡 Best Practices
- **Use schemas:** Always validate INI against schemas when possible
- **Proper structure:** Maintain consistent INI structure and naming
- **Template security:** Sanitize template variables
- **Error handling:** Implement proper error handling for INI operations
- **Document structure:** Document INI structure and expected format
- **Version control:** Use version control for INI configurations

## 🔗 Cross-References
- [@ Operator Introduction](024-at-operator-intro-bash.md)
- [@file Function](038-at-file-function-bash.md)
- [@env Function](026-at-env-function-bash.md)
- [Configuration Management](093-configuration-management-bash.md)
- [Legacy System Integration](105-legacy-system-integration-bash.md)

---

**Master @ini in TuskLang and structure your configurations with foundation. ⚙️** 