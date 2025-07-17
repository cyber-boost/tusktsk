# ⚙️ TuskLang Bash @ini Function Guide

**"We don't bow to any king" – INI is your configuration's classic format.**

The @ini function in TuskLang is your INI manipulation powerhouse, enabling dynamic INI operations, configuration parsing, and format conversion directly within your configuration files. Whether you're parsing Windows INI files, PHP configuration files, or legacy application configurations, @ini provides the flexibility and power to work with INI data seamlessly.

## 🎯 What is @ini?
The @ini function provides INI operations in TuskLang. It offers:
- **INI parsing** - Parse INI strings into data structures
- **INI generation** - Create INI from data structures
- **INI validation** - Validate INI syntax and structure
- **INI transformation** - Transform and manipulate INI data
- **INI formatting** - Format INI with proper sections and comments

## 📝 Basic @ini Syntax

### INI Parsing
```ini
[ini_parsing]
# Parse simple INI strings
$simple_ini: |
  name=John Doe
  age=30
  city=New York
parsed_simple: @ini.parse($simple_ini)

$sectioned_ini: |
  [database]
  host=localhost
  port=3306
  name=tusklang
  
  [api]
  url=https://api.example.com
  timeout=30
parsed_sectioned: @ini.parse($sectioned_ini)

$complex_ini: |
  ; Database configuration
  [database]
  host=localhost
  port=3306
  name=tusklang
  charset=utf8mb4
  
  ; API configuration
  [api]
  base_url=https://api.example.com
  timeout=30
  retries=3
  
  ; Feature flags
  [features]
  authentication=true
  caching=true
  logging=false
parsed_complex: @ini.parse($complex_ini)

# Parse INI with validation
$valid_ini: |
  valid=true
  data=test
validated_ini: @ini.parse($valid_ini, true)

# Parse INI with error handling
$invalid_ini: |
  invalid=true
  missing=quote
safe_parse: @ini.parse($invalid_ini, false, {"error": "Invalid INI"})
```

### INI Generation
```ini
[ini_generation]
# Generate INI from data structures
$user_data: {
    "id": 123,
    "name": "Alice Johnson",
    "email": "alice@example.com",
    "preferences": {
        "theme": "dark",
        "notifications": true
    }
}

user_ini: @ini.generate($user_data)
user_ini_pretty: @ini.generate($user_data, true)

# Generate INI with sections
$config_data: {
    "database": {
        "host": "localhost",
        "port": 3306,
        "name": "tusklang"
    },
    "api": {
        "url": "https://api.example.com",
        "timeout": 30
    }
}

config_ini: @ini.generate($config_data, true)

# Generate INI with custom options
$app_config: {
    "app": {
        "name": "MyApp",
        "version": "1.0.0",
        "debug": false
    },
    "features": ["auth", "caching", "logging"]
}

app_ini: @ini.generate($app_config, true, {
    "indent": 4,
    "sort_keys": true,
    "add_comments": true
})
```

### INI Validation
```ini
[ini_validation]
# Validate INI syntax
$valid_ini_string: |
  name=John
  age=30
is_valid: @ini.validate($valid_ini_string)

$invalid_ini_string: |
  name=John
  age=30
  city=New York
is_invalid: @ini.validate($invalid_ini_string)

# Validate INI structure
$ini_schema: {
    "type": "object",
    "properties": {
        "name": {"type": "string"},
        "age": {"type": "number"},
        "email": {"type": "string", "format": "email"}
    },
    "required": ["name", "age"]
}

$test_data: {
    "name": "Alice",
    "age": 25,
    "email": "alice@example.com"
}

schema_validation: @ini.validate_schema($test_data, $ini_schema)
```

## 🚀 Quick Start Example

```bash
#!/bin/bash
source tusk-bash.sh

cat > ini-quickstart.tsk << 'EOF'
[ini_parsing]
# Parse INI data
$php_ini: |
  [PHP]
  memory_limit=256M
  max_execution_time=30
  upload_max_filesize=10M
  
  [MySQL]
  host=localhost
  port=3306
  database=myapp
parsed_php: @ini.parse($php_ini)

$apache_ini: |
  [Server]
  DocumentRoot=/var/www/html
  ServerName=example.com
  Port=80
  
  [Security]
  AllowOverride=All
  Require=all granted
parsed_apache: @ini.parse($apache_ini)

[ini_generation]
# Generate INI from data
$app_config: {
    "app": {
        "name": "MyApp",
        "version": "1.0.0",
        "environment": "production"
    },
    "database": {
        "host": "localhost",
        "port": 5432,
        "name": "myapp_db"
    },
    "features": ["authentication", "caching", "logging"]
}

app_ini: @ini.generate($app_config)
app_ini_pretty: @ini.generate($app_config, true)

[ini_validation]
# Validate INI
$test_ini: |
  name=Charlie
  age=30
  email=charlie@example.com
is_valid: @ini.validate($test_ini)

$invalid_ini: |
  name=David
  age=35
  email=david@example.com
is_invalid: @ini.validate($invalid_ini)

[ini_transformation]
# Transform INI data
$config_data: {
    "services": [
        {"name": "web", "port": 80, "enabled": true},
        {"name": "api", "port": 3000, "enabled": true},
        {"name": "db", "port": 5432, "enabled": false}
    ]
}

enabled_services: @ini.filter($config_data.services, "item.enabled == true")
service_names: @ini.map($config_data.services, "item.name")
service_count: @ini.length($config_data.services)
EOF

config=$(tusk_parse ini-quickstart.tsk)

echo "=== INI Parsing ==="
echo "Parsed PHP: $(tusk_get "$config" ini_parsing.parsed_php)"
echo "Parsed Apache: $(tusk_get "$config" ini_parsing.parsed_apache)"

echo ""
echo "=== INI Generation ==="
echo "App INI: $(tusk_get "$config" ini_generation.app_ini)"
echo "Pretty INI: $(tusk_get "$config" ini_generation.app_ini_pretty)"

echo ""
echo "=== INI Validation ==="
echo "Is Valid: $(tusk_get "$config" ini_validation.is_valid)"
echo "Is Invalid: $(tusk_get "$config" ini_validation.is_invalid)"

echo ""
echo "=== INI Transformation ==="
echo "Enabled Services: $(tusk_get "$config" ini_transformation.enabled_services)"
echo "Service Names: $(tusk_get "$config" ini_transformation.service_names)"
echo "Service Count: $(tusk_get "$config" ini_transformation.service_count)"
```

## 🔗 Real-World Use Cases

### 1. PHP Configuration Management
```ini
[php_config]
# Manage PHP configurations
$php_templates: {
    "development": {
        "PHP": {
            "memory_limit": "512M",
            "max_execution_time": "60",
            "upload_max_filesize": "20M",
            "post_max_size": "20M",
            "display_errors": "On",
            "log_errors": "On",
            "error_reporting": "E_ALL"
        },
        "MySQL": {
            "host": "localhost",
            "port": "3306",
            "database": "myapp_dev",
            "username": "root",
            "password": ""
        },
        "Redis": {
            "host": "localhost",
            "port": "6379",
            "database": "0"
        }
    },
    "production": {
        "PHP": {
            "memory_limit": "256M",
            "max_execution_time": "30",
            "upload_max_filesize": "10M",
            "post_max_size": "10M",
            "display_errors": "Off",
            "log_errors": "On",
            "error_reporting": "E_ERROR | E_WARNING | E_PARSE"
        },
        "MySQL": {
            "host": "@env('DB_HOST')",
            "port": "@env('DB_PORT', '3306')",
            "database": "@env('DB_NAME')",
            "username": "@env('DB_USER')",
            "password": "@env('DB_PASSWORD')"
        },
        "Redis": {
            "host": "@env('REDIS_HOST')",
            "port": "@env('REDIS_PORT', '6379')",
            "database": "0"
        }
    }
}

# Generate PHP INI files
$php_generation: {
    "dev_ini": @ini.generate($php_templates.development, true),
    "prod_ini": @ini.generate($php_templates.production, true)
}

# Parse existing PHP INI
$existing_php_ini: |
  [PHP]
  memory_limit=128M
  max_execution_time=30
  upload_max_filesize=2M
  
  [MySQL]
  host=localhost
  database=myapp

parsed_php_ini: @ini.parse($existing_php_ini)

# Update PHP INI
$updated_php_ini: @ini.merge($existing_php_ini, {
    "PHP": {
        "memory_limit": "256M",
        "max_execution_time": "60"
    },
    "Redis": {
        "host": "localhost",
        "port": "6379"
    }
})
updated_php_ini: @ini.generate($updated_php_ini, true)
```

### 2. Apache Configuration Management
```ini
[apache_config]
# Manage Apache configurations
$apache_templates: {
    "development": {
        "Server": {
            "DocumentRoot": "/var/www/html",
            "ServerName": "localhost",
            "Port": "80",
            "ServerAdmin": "admin@localhost"
        },
        "Directory": {
            "AllowOverride": "All",
            "Require": "all granted",
            "Options": "Indexes FollowSymLinks"
        },
        "Security": {
            "ServerTokens": "Prod",
            "ServerSignature": "Off",
            "TraceEnable": "Off"
        }
    },
    "production": {
        "Server": {
            "DocumentRoot": "/var/www/html",
            "ServerName": "@env('SERVER_NAME')",
            "Port": "80",
            "ServerAdmin": "@env('SERVER_ADMIN')"
        },
        "Directory": {
            "AllowOverride": "None",
            "Require": "all denied",
            "Options": "FollowSymLinks"
        },
        "Security": {
            "ServerTokens": "Prod",
            "ServerSignature": "Off",
            "TraceEnable": "Off",
            "Header": "always set X-Content-Type-Options nosniff"
        }
    }
}

# Generate Apache INI files
$apache_generation: {
    "dev_ini": @ini.generate($apache_templates.development, true),
    "prod_ini": @ini.generate($apache_templates.production, true)
}

# Parse existing Apache configuration
$existing_apache: |
  [Server]
  DocumentRoot=/var/www/html
  ServerName=example.com
  
  [Directory]
  AllowOverride=All

parsed_apache: @ini.parse($existing_apache)

# Merge Apache configurations
$merged_apache: @ini.merge($existing_apache, {
    "Security": {
        "ServerTokens": "Prod",
        "ServerSignature": "Off"
    }
})
merged_apache_ini: @ini.generate($merged_apache, true)
```

### 3. Application Configuration Management
```ini
[app_config]
# Manage application configurations with INI
$app_config_templates: {
    "development": {
        "app": {
            "name": "MyApp",
            "version": "1.0.0",
            "debug": "true",
            "log_level": "debug"
        },
        "database": {
            "host": "localhost",
            "port": "5432",
            "name": "myapp_dev",
            "pool_size": "5"
        },
        "redis": {
            "host": "localhost",
            "port": "6379",
            "db": "0"
        },
        "features": {
            "authentication": "true",
            "caching": "true",
            "logging": "true"
        }
    },
    "production": {
        "app": {
            "name": "MyApp",
            "version": "1.0.0",
            "debug": "false",
            "log_level": "info"
        },
        "database": {
            "host": "@env('DB_HOST')",
            "port": "@env('DB_PORT', '5432')",
            "name": "@env('DB_NAME')",
            "pool_size": "20"
        },
        "redis": {
            "host": "@env('REDIS_HOST')",
            "port": "@env('REDIS_PORT', '6379')",
            "db": "0"
        },
        "features": {
            "authentication": "true",
            "caching": "true",
            "logging": "true",
            "monitoring": "true"
        }
    }
}

# Generate application configuration INI
$app_config_generation: {
    "dev_ini": @ini.generate($app_config_templates.development, true),
    "prod_ini": @ini.generate($app_config_templates.production, true)
}

# Parse configuration from INI
$config_ini_string: |
  [app]
  name=MyApp
  version=1.0.0
  debug=false
  
  [database]
  host=prod.example.com
  port=5432
  name=myapp_prod
  
  [features]
  authentication=true
  caching=true

parsed_config: @ini.parse($config_ini_string)

# Validate configuration structure
$config_schema: {
    "type": "object",
    "properties": {
        "app": {
            "type": "object",
            "properties": {
                "name": {"type": "string"},
                "version": {"type": "string"},
                "debug": {"type": "boolean"}
            },
            "required": ["name", "version"]
        },
        "database": {
            "type": "object",
            "properties": {
                "host": {"type": "string"},
                "port": {"type": "number"},
                "name": {"type": "string"}
            },
            "required": ["host", "name"]
        }
    },
    "required": ["app", "database"]
}

config_validation: @ini.validate_schema($parsed_config, $config_schema)
```

### 4. Legacy System Integration
```ini
[legacy_integration]
# Manage legacy system configurations
$legacy_templates: {
    "windows_ini": {
        "System": {
            "ComputerName": "WORKSTATION-01",
            "Domain": "CORP.LOCAL",
            "TimeZone": "Eastern Standard Time"
        },
        "Network": {
            "DHCP": "true",
            "IPAddress": "192.168.1.100",
            "SubnetMask": "255.255.255.0",
            "Gateway": "192.168.1.1"
        },
        "Services": {
            "Spooler": "running",
            "RpcSs": "running",
            "Themes": "stopped"
        }
    },
    "dos_config": {
        "FILES": "40",
        "BUFFERS": "20",
        "DEVICE": "C:\\DOS\\HIMEM.SYS",
        "DOS": "HIGH,UMB"
    }
}

# Generate legacy INI files
$legacy_generation: {
    "windows_ini": @ini.generate($legacy_templates.windows_ini, true),
    "dos_config": @ini.generate($legacy_templates.dos_config, true)
}

# Parse existing legacy configurations
$existing_legacy: |
  [System]
  ComputerName=OLD-WORKSTATION
  Domain=LEGACY.LOCAL
  
  [Network]
  DHCP=false
  IPAddress=10.0.0.100

parsed_legacy: @ini.parse($existing_legacy)

# Update legacy configurations
$updated_legacy: @ini.merge($existing_legacy, {
    "System": {
        "ComputerName": "NEW-WORKSTATION",
        "Domain": "MODERN.LOCAL"
    },
    "Security": {
        "Firewall": "enabled",
        "Antivirus": "enabled"
    }
})
updated_legacy_ini: @ini.generate($updated_legacy, true)
```

## 🧠 Advanced @ini Patterns

### INI Schema Validation
```ini
[ini_schema]
# Define INI schemas
$ini_schemas: {
    "php_config": {
        "type": "object",
        "properties": {
            "PHP": {
                "type": "object",
                "properties": {
                    "memory_limit": {"type": "string", "pattern": "^\\d+M$"},
                    "max_execution_time": {"type": "number", "minimum": 1},
                    "upload_max_filesize": {"type": "string", "pattern": "^\\d+M$"}
                },
                "required": ["memory_limit", "max_execution_time"]
            },
            "MySQL": {
                "type": "object",
                "properties": {
                    "host": {"type": "string"},
                    "port": {"type": "number", "minimum": 1, "maximum": 65535},
                    "database": {"type": "string"}
                },
                "required": ["host", "database"]
            }
        },
        "required": ["PHP"]
    },
    "apache_config": {
        "type": "object",
        "properties": {
            "Server": {
                "type": "object",
                "properties": {
                    "DocumentRoot": {"type": "string"},
                    "ServerName": {"type": "string"},
                    "Port": {"type": "number", "minimum": 1, "maximum": 65535}
                },
                "required": ["DocumentRoot", "ServerName"]
            }
        },
        "required": ["Server"]
    }
}

# Validate INI against schemas
$validation_examples: {
    "valid_php": {
        "PHP": {
            "memory_limit": "256M",
            "max_execution_time": 30,
            "upload_max_filesize": "10M"
        },
        "MySQL": {
            "host": "localhost",
            "port": 3306,
            "database": "myapp"
        }
    },
    "invalid_php": {
        "PHP": {
            "memory_limit": "invalid",
            "max_execution_time": -1
        }
    }
}

$schema_validation_results: {
    "valid_php_result": @ini.validate_schema($validation_examples.valid_php, $ini_schemas.php_config),
    "invalid_php_result": @ini.validate_schema($validation_examples.invalid_php, $ini_schemas.php_config)
}
```

### INI Transformation and Mapping
```ini
[ini_transformation]
# Advanced INI transformation
$transformation_functions: {
    "flatten_ini": @function.create("data, prefix", """
        var flattened = {};
        prefix = prefix || '';
        
        for (var key in data) {
            var new_key = prefix ? prefix + '.' + key : key;
            
            if (typeof data[key] === 'object' && data[key] !== null && !Array.isArray(data[key])) {
                var nested = @function.call('flatten_ini', data[key], new_key);
                for (var nested_key in nested) {
                    flattened[nested_key] = nested[nested_key];
                }
            } else {
                flattened[new_key] = data[key];
            }
        }
        
        return flattened;
    """),
    
    "unflatten_ini": @function.create("data", """
        var unflattened = {};
        
        for (var key in data) {
            var keys = key.split('.');
            var current = unflattened;
            
            for (var i = 0; i < keys.length - 1; i++) {
                if (!current[keys[i]]) {
                    current[keys[i]] = {};
                }
                current = current[keys[i]];
            }
            
            current[keys[keys.length - 1]] = data[key];
        }
        
        return unflattened;
    """),
    
    "ini_diff": @function.create("ini1, ini2", """
        var data1 = @ini.parse(ini1);
        var data2 = @ini.parse(ini2);
        
        var diff = {};
        
        for (var key in data1) {
            if (!data2.hasOwnProperty(key)) {
                diff[key] = {'removed': data1[key]};
            } else if (@ini.generate(data1[key]) !== @ini.generate(data2[key])) {
                diff[key] = {
                    'old': data1[key],
                    'new': data2[key]
                };
            }
        }
        
        for (var key in data2) {
            if (!data1.hasOwnProperty(key)) {
                diff[key] = {'added': data2[key]};
            }
        }
        
        return diff;
    """)
}

# Use transformation functions
$transformation_examples: {
    "nested_data": {
        "user": {
            "profile": {
                "name": "Alice",
                "age": 25
            },
            "settings": {
                "theme": "dark"
            }
        }
    },
    
    "flattened_data": @function.call("flatten_ini", $transformation_examples.nested_data),
    "unflattened_data": @function.call("unflatten_ini", $transformation_examples.flattened_data)
}
```

## 🛡️ Security & Performance Notes
- **INI injection:** Validate and sanitize INI data to prevent injection attacks
- **Memory usage:** Monitor INI size for large data structures
- **Parsing performance:** Use efficient INI parsing for large datasets
- **Schema validation:** Always validate INI against schemas for critical data
- **Error handling:** Implement proper error handling for INI parsing failures
- **Data validation:** Validate INI structure and content before processing

## 🐞 Troubleshooting
- **INI parsing errors:** Check INI syntax and validate structure
- **Memory issues:** Monitor INI size and implement chunking for large data
- **Schema validation failures:** Review schema definitions and data structure
- **Performance problems:** Optimize INI operations for large datasets
- **Encoding issues:** Ensure proper character encoding for INI data

## 💡 Best Practices
- **Use schemas:** Always define and validate INI schemas for critical data
- **Handle errors:** Implement proper error handling for INI operations
- **Optimize size:** Minimize INI size by removing unnecessary fields
- **Validate input:** Always validate INI input before processing
- **Use proper formatting:** Use consistent formatting for readability
- **Document structure:** Document INI structure and field meanings

## 🔗 Cross-References
- [@ Operator Introduction](024-at-operator-intro-bash.md)
- [@yaml Function](059-at-yaml-function-bash.md)
- [@toml Function](060-at-toml-function-bash.md)
- [Configuration Management](115-configuration-management-bash.md)
- [Legacy System Integration](118-legacy-system-integration-bash.md)

---

**Master @ini in TuskLang and wield the power of classic configuration in your systems. ⚙️** 