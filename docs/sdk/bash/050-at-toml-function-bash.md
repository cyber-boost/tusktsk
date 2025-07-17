# ⚙️ TuskLang Bash @toml Function Guide

**"We don't bow to any king" – TOML is your configuration's clarity.**

The @toml function in TuskLang is your TOML processing powerhouse, enabling dynamic TOML parsing, generation, and manipulation directly within your configuration files. Whether you're working with Rust projects, Python configurations, or application settings, @toml provides the flexibility and power to handle structured data with clarity and precision.

## 🎯 What is @toml?
The @toml function provides TOML processing capabilities in TuskLang. It offers:
- **TOML parsing** - Parse TOML strings and files
- **TOML generation** - Create TOML documents dynamically
- **TOML transformation** - Transform TOML data between formats
- **Configuration management** - Manage complex configuration structures
- **Project configuration** - Handle project-specific TOML files

## 📝 Basic @toml Syntax

### Simple TOML Parsing
```ini
[simple_toml]
# Parse TOML string
$toml_data: |
  name = "John"
  email = "john@example.com"
  age = 30
  active = true
parsed_toml: @toml.parse($toml_data)
user_name: @toml.get($parsed_toml, "name")
user_email: @toml.get($parsed_toml, "email")
user_age: @toml.get($parsed_toml, "age")
```

### TOML File Processing
```ini
[toml_file_processing]
# Parse TOML file
config_toml: @toml.parse(@file.read("/etc/app/config.toml"))
api_config: @toml.get($config_toml, "api")
database_config: @toml.get($config_toml, "database")
```

### TOML Generation
```ini
[toml_generation]
# Generate TOML dynamically
$user_data: {
    "name": "Alice",
    "email": "alice@example.com",
    "age": 30,
    "roles": ["admin", "user"]
}
user_toml: @toml.generate($user_data)

# Generate complex TOML
$config_data: {
    "database": {
        "host": "localhost",
        "port": 3306,
        "name": "app_db"
    },
    "api": {
        "url": "https://api.example.com",
        "timeout": 30
    }
}
config_toml: @toml.generate($config_data)
```

## 🚀 Quick Start Example

```bash
#!/bin/bash
source tusk-bash.sh

cat > toml-quickstart.tsk << 'EOF'
[toml_parsing]
# Parse TOML data
$toml_string: |
  [app]
  name = "TuskLang"
  version = "2.1.0"
  
  [database]
  host = "localhost"
  port = 3306
  
  [api]
  url = "https://api.example.com"
  timeout = 30
parsed_config: @toml.parse($toml_string)

# Extract data
app_name: @toml.get($parsed_config, "app.name")
app_version: @toml.get($parsed_config, "app.version")
db_host: @toml.get($parsed_config, "database.host")
api_url: @toml.get($parsed_config, "api.url")

[toml_generation]
# Generate TOML
$user_info: {
    "name": "John Doe",
    "email": "john@example.com",
    "role": "admin",
    "permissions": ["read", "write", "delete"]
}
user_toml: @toml.generate($user_info)

# Generate nested TOML
$nested_data: {
    "server": {
        "name": "web-server-01",
        "ip": "192.168.1.100",
        "services": {
            "web": {"port": 80, "enabled": true},
            "ssl": {"port": 443, "enabled": true}
        }
    }
}
server_toml: @toml.generate($nested_data)

[toml_transformation]
# Transform TOML data
$source_toml: |
  [[users]]
  id = 1
  name = "Alice"
  email = "alice@example.com"
  
  [[users]]
  id = 2
  name = "Bob"
  email = "bob@example.com"
transformed_toml: @toml.transform($source_toml, "users[*]", {
    "template": {
        "person_id": "$.id",
        "full_name": "$.name",
        "contact": "$.email"
    }
})
EOF

config=$(tusk_parse toml-quickstart.tsk)

echo "=== TOML Parsing ==="
echo "App Name: $(tusk_get "$config" toml_parsing.app_name)"
echo "App Version: $(tusk_get "$config" toml_parsing.app_version)"
echo "Database Host: $(tusk_get "$config" toml_parsing.db_host)"
echo "API URL: $(tusk_get "$config" toml_parsing.api_url)"

echo ""
echo "=== TOML Generation ==="
echo "User TOML: $(tusk_get "$config" toml_generation.user_toml)"
echo "Server TOML: $(tusk_get "$config" toml_generation.server_toml)"

echo ""
echo "=== TOML Transformation ==="
echo "Transformed TOML: $(tusk_get "$config" toml_transformation.transformed_toml)"
```

## 🔗 Real-World Use Cases

### 1. Rust Project Configuration
```ini
[rust_config]
# Generate Cargo.toml for Rust project
$cargo_config: {
    "package": {
        "name": "tusklang-app",
        "version": "0.1.0",
        "edition": "2021",
        "authors": ["TuskLang Team <team@tusklang.com>"],
        "description": "A powerful configuration language"
    },
    "dependencies": {
        "serde": "1.0",
        "serde_json": "1.0",
        "tokio": {version = "1.0", features = ["full"]},
        "reqwest": {version = "0.11", features = ["json"]}
    },
    "dev-dependencies": {
        "tokio-test": "0.4"
    },
    "features": {
        "default": ["std"],
        "std": [],
        "no_std": []
    }
}

cargo_toml: @toml.generate($cargo_config)
@file.write("Cargo.toml", $cargo_toml)

# Generate .cargo/config.toml
$cargo_config_toml: {
    "build": {
        "rustflags": ["-C", "target-cpu=native"],
        "target": "x86_64-unknown-linux-gnu"
    },
    "net": {
        "git-fetch-with-cli": true
    }
}

cargo_config: @toml.generate($cargo_config_toml)
@file.write(".cargo/config.toml", $cargo_config)
```

### 2. Python Project Configuration
```ini
[python_config]
# Generate pyproject.toml for Python project
$pyproject_config: {
    "build-system": {
        "requires": ["setuptools>=45", "wheel"],
        "build-backend": "setuptools.build_meta"
    },
    "project": {
        "name": "tusklang-python",
        "version": "0.1.0",
        "description": "Python bindings for TuskLang",
        "authors": [{"name": "TuskLang Team", "email": "team@tusklang.com"}],
        "dependencies": [
            "requests>=2.25.0",
            "pyyaml>=5.4",
            "toml>=0.10.0"
        ],
        "requires-python": ">=3.8"
    },
    "tool": {
        "pytest": {
            "testpaths": ["tests"],
            "python_files": ["test_*.py"]
        },
        "black": {
            "line-length": 88,
            "target-version": ["py38"]
        }
    }
}

pyproject_toml: @toml.generate($pyproject_config)
@file.write("pyproject.toml", $pyproject_toml)
```

### 3. Application Configuration Management
```ini
[app_configuration]
# Parse application configuration
$app_toml: @toml.parse(@file.read("/etc/app/config.toml"))

# Extract configuration sections
$app_config: {
    "name": @toml.get($app_toml, "app.name"),
    "version": @toml.get($app_toml, "app.version"),
    "environment": @toml.get($app_toml, "app.environment")
}

$database_config: {
    "host": @toml.get($app_toml, "database.host"),
    "port": @toml.get($app_toml, "database.port"),
    "name": @toml.get($app_toml, "database.name"),
    "credentials": {
        "username": @toml.get($app_toml, "database.credentials.username"),
        "password": @toml.get($app_toml, "database.credentials.password")
    }
}

# Generate updated configuration
$updated_config: {
    "app": $app_config,
    "database": $database_config,
    "timestamp": @date("Y-m-d H:i:s")
}

updated_toml: @toml.generate($updated_config)
@file.write("/etc/app/updated-config.toml", $updated_toml)
```

### 4. Development Environment Configuration
```ini
[dev_environment]
# Generate development environment configuration
$dev_config: {
    "development": {
        "debug": true,
        "log_level": "debug",
        "port": 3000
    },
    "database": {
        "host": "localhost",
        "port": 5432,
        "name": "tusklang_dev",
        "pool_size": 10
    },
    "redis": {
        "host": "localhost",
        "port": 6379,
        "db": 0
    },
    "features": {
        "hot_reload": true,
        "auto_migrate": true,
        "seed_data": true
    }
}

dev_toml: @toml.generate($dev_config)
@file.write("config/development.toml", $dev_toml)

# Generate production configuration
$prod_config: {
    "production": {
        "debug": false,
        "log_level": "info",
        "port": 80
    },
    "database": {
        "host": @env("DB_HOST"),
        "port": @env("DB_PORT", "5432"),
        "name": @env("DB_NAME"),
        "pool_size": 50
    },
    "redis": {
        "host": @env("REDIS_HOST"),
        "port": @env("REDIS_PORT", "6379"),
        "db": 0
    },
    "features": {
        "hot_reload": false,
        "auto_migrate": false,
        "seed_data": false
    }
}

prod_toml: @toml.generate($prod_config)
@file.write("config/production.toml", $prod_toml)
```

## 🧠 Advanced @toml Patterns

### TOML Template Processing
```ini
[toml_templates]
# Process TOML templates with dynamic data
$template_toml: |
  [app]
  name = "{{ .AppName }}"
  version = "{{ .Version }}"
  environment = "{{ .Environment }}"
  
  [database]
  host = "{{ .DatabaseHost }}"
  port = {{ .DatabasePort }}
  name = "{{ .DatabaseName }}"
  
  [api]
  url = "{{ .APIURL }}"
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
processed_toml: @toml.process_template($template_toml, $template_vars)
@file.write("/etc/app/config.toml", $processed_toml)
```

### TOML Data Validation
```ini
[toml_validation]
# Validate TOML against schema
$toml_document: @file.read("/var/config/app-config.toml")
$schema_file: @file.read("/etc/schemas/app-schema.toml")

# Validate TOML structure
validation_result: @toml.validate($toml_document, $schema_file)

# Handle validation errors
@if($validation_result.valid, {
    "action": "process_toml",
    "data": @toml.parse($toml_document)
}, {
    "action": "handle_validation_errors",
    "errors": $validation_result.errors,
    "timestamp": @date("Y-m-d H:i:s")
})
```

### TOML Configuration Inheritance
```ini
[toml_inheritance]
# Base configuration
$base_config: {
    "app": {
        "name": "tusklang",
        "version": "2.1.0"
    },
    "database": {
        "host": "localhost",
        "port": 3306
    }
}

# Environment-specific overrides
$dev_config: {
    "database": {
        "host": "dev-db.example.com",
        "port": 3306
    },
    "logging": {
        "level": "debug"
    }
}

$prod_config: {
    "database": {
        "host": "prod-db.example.com",
        "port": 3306
    },
    "logging": {
        "level": "info"
    }
}

# Merge configurations
$environment: @env("ENVIRONMENT", "development")
$final_config: @if($environment == "production", 
    @toml.merge($base_config, $prod_config),
    @toml.merge($base_config, $dev_config)
)

final_toml: @toml.generate($final_config)
@file.write("/etc/app/config-" + $environment + ".toml", $final_toml)
```

### TOML Array and Table Processing
```ini
[toml_arrays_tables]
# Process TOML arrays and tables
$complex_toml: |
  [[servers]]
  name = "web-01"
  ip = "192.168.1.10"
  ports = [80, 443]
  
  [[servers]]
  name = "web-02"
  ip = "192.168.1.11"
  ports = [80, 443]
  
  [database]
  [database.primary]
  host = "db-01.example.com"
  port = 3306
  
  [database.replica]
  host = "db-02.example.com"
  port = 3306

parsed_complex: @toml.parse($complex_toml)

# Extract server information
$servers: @toml.get_all($parsed_complex, "servers")
$server_data: @array.map($servers, {
    "name": item.name,
    "ip": item.ip,
    "ports": item.ports
})

# Extract database configuration
$db_config: {
    "primary": @toml.get($parsed_complex, "database.primary"),
    "replica": @toml.get($parsed_complex, "database.replica")
}

# Generate processed configuration
$processed_config: {
    "servers": $server_data,
    "database": $db_config,
    "processed_at": @date("Y-m-d H:i:s")
}

processed_toml: @toml.generate($processed_config)
```

## 🛡️ Security & Performance Notes
- **TOML injection:** Validate and sanitize TOML input to prevent injection attacks
- **Memory usage:** Monitor memory consumption when processing large TOML files
- **Schema validation:** Always validate TOML against schemas when possible
- **Template security:** Sanitize template variables to prevent code injection
- **File permissions:** Ensure proper file permissions for TOML operations
- **Performance optimization:** Use efficient TOML processing for large files

## 🐞 Troubleshooting
- **Parsing errors:** Check TOML syntax and structure
- **Schema validation:** Ensure TOML conforms to expected schema
- **Template errors:** Verify template syntax and variable substitution
- **Memory issues:** Optimize TOML processing for large files
- **Encoding problems:** Ensure proper character encoding (UTF-8)

## 💡 Best Practices
- **Use schemas:** Always validate TOML against schemas when possible
- **Proper structure:** Maintain consistent TOML structure and naming
- **Template security:** Sanitize template variables
- **Error handling:** Implement proper error handling for TOML operations
- **Document structure:** Document TOML structure and expected format
- **Version control:** Use version control for TOML configurations

## 🔗 Cross-References
- [@ Operator Introduction](024-at-operator-intro-bash.md)
- [@file Function](038-at-file-function-bash.md)
- [@env Function](026-at-env-function-bash.md)
- [Configuration Management](093-configuration-management-bash.md)
- [Project Configuration](104-project-configuration-bash.md)

---

**Master @toml in TuskLang and structure your configurations with clarity. ⚙️** 