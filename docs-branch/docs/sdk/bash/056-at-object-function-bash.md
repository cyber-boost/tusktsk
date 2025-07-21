# 🏗️ TuskLang Bash @object Function Guide

**"We don't bow to any king" – Objects are your configuration's data structures.**

The @object function in TuskLang is your object manipulation powerhouse, enabling dynamic object operations, property management, and data structure manipulation directly within your configuration files. Whether you're working with JSON data, managing complex objects, or transforming data structures, @object provides the flexibility and power to work with objects seamlessly.

## 🎯 What is @object?
The @object function provides object operations in TuskLang. It offers:
- **Object creation** - Create objects from various data sources
- **Property access** - Get, set, and manipulate object properties
- **Object transformation** - Merge, clone, and transform objects
- **Object validation** - Validate object structures and properties
- **Object serialization** - Convert objects to and from different formats

## 📝 Basic @object Syntax

### Object Creation
```ini
[object_creation]
# Create simple objects
user: @object.create({
    "id": 1,
    "name": "John Doe",
    "email": "john@example.com",
    "age": 30
})

config: @object.create({
    "database": {
        "host": "localhost",
        "port": 3306,
        "name": "tusklang"
    },
    "api": {
        "url": "https://api.example.com",
        "timeout": 30
    }
})

# Create objects from JSON
$json_string: '{"name": "Alice", "age": 25, "city": "New York"}'
parsed_object: @object.from_json($json_string)

# Create objects from arrays
$key_value_pairs: [["name", "Bob"], ["age", 35], ["city", "London"]]
object_from_pairs: @object.from_entries($key_value_pairs)
```

### Property Access and Manipulation
```ini
[property_operations]
# Access object properties
$user_data: {
    "id": 123,
    "name": "Alice Johnson",
    "email": "alice@example.com",
    "profile": {
        "age": 28,
        "city": "San Francisco",
        "preferences": {
            "theme": "dark",
            "notifications": true
        }
    }
}

# Get properties
user_id: @object.get($user_data, "id")
user_name: @object.get($user_data, "name")
user_age: @object.get($user_data, "profile.age")
user_theme: @object.get($user_data, "profile.preferences.theme")

# Set properties
updated_user: @object.set($user_data, "profile.age", 29)
new_property: @object.set($user_data, "profile.preferences.language", "en")
nested_property: @object.set($user_data, "profile.preferences.settings.timezone", "UTC")
```

### Object Operations
```ini
[object_operations]
# Object manipulation
$original: {
    "name": "John",
    "age": 30,
    "city": "New York"
}

# Clone objects
cloned_object: @object.clone($original)

# Merge objects
$updates: {
    "age": 31,
    "city": "Boston",
    "country": "USA"
}

merged_object: @object.merge($original, $updates)

# Check object properties
has_name: @object.has($original, "name")
has_country: @object.has($original, "country")
property_count: @object.keys($original).length
```

## 🚀 Quick Start Example

```bash
#!/bin/bash
source tusk-bash.sh

cat > object-quickstart.tsk << 'EOF'
[basic_objects]
# Create and manipulate objects
user: @object.create({
    "id": 1,
    "name": "Alice Johnson",
    "email": "alice@example.com",
    "age": 28
})

config: @object.create({
    "database": {
        "host": "localhost",
        "port": 3306
    },
    "api": {
        "url": "https://api.example.com",
        "timeout": 30
    }
})

[property_access]
# Access object properties
user_name: @object.get($user, "name")
user_age: @object.get($user, "age")
db_host: @object.get($config, "database.host")
api_timeout: @object.get($config, "api.timeout")

[object_manipulation]
# Modify objects
updated_user: @object.set($user, "age", 29)
updated_config: @object.set($config, "database.port", 5432)

# Add new properties
user_with_city: @object.set($user, "city", "San Francisco")
config_with_env: @object.set($config, "environment", "production")

[object_operations]
# Object utilities
user_keys: @object.keys($user)
user_values: @object.values($user)
user_entries: @object.entries($user)

# Check properties
has_email: @object.has($user, "email")
has_phone: @object.has($user, "phone")

[object_transformation]
# Transform objects
$user_data: {
    "first_name": "Alice",
    "last_name": "Johnson",
    "email": "alice@example.com"
}

full_name: @object.get($user_data, "first_name") + " " + @object.get($user_data, "last_name")
user_info: {
    "name": $full_name,
    "email": @object.get($user_data, "email"),
    "created_at": @date("Y-m-d H:i:s")
}
EOF

config=$(tusk_parse object-quickstart.tsk)

echo "=== Basic Objects ==="
echo "User: $(tusk_get "$config" basic_objects.user)"
echo "Config: $(tusk_get "$config" basic_objects.config)"

echo ""
echo "=== Property Access ==="
echo "User Name: $(tusk_get "$config" property_access.user_name)"
echo "User Age: $(tusk_get "$config" property_access.user_age)"
echo "DB Host: $(tusk_get "$config" property_access.db_host)"
echo "API Timeout: $(tusk_get "$config" property_access.api_timeout)"

echo ""
echo "=== Object Manipulation ==="
echo "Updated User: $(tusk_get "$config" object_manipulation.updated_user)"
echo "Updated Config: $(tusk_get "$config" object_manipulation.updated_config)"

echo ""
echo "=== Object Operations ==="
echo "User Keys: $(tusk_get "$config" object_operations.user_keys)"
echo "Has Email: $(tusk_get "$config" object_operations.has_email)"
echo "Has Phone: $(tusk_get "$config" object_operations.has_phone)"

echo ""
echo "=== Object Transformation ==="
echo "Full Name: $(tusk_get "$config" object_transformation.full_name)"
echo "User Info: $(tusk_get "$config" object_transformation.user_info)"
```

## 🔗 Real-World Use Cases

### 1. Configuration Management
```ini
[config_management]
# Manage application configuration
$app_config: {
    "database": {
        "host": @env("DB_HOST", "localhost"),
        "port": @env("DB_PORT", "3306"),
        "name": @env("DB_NAME", "tusklang"),
        "user": @env("DB_USER", "root"),
        "password": @env("DB_PASSWORD", ""),
        "options": {
            "charset": "utf8mb4",
            "timezone": "UTC",
            "ssl": @env("DB_SSL", "false")
        }
    },
    "api": {
        "base_url": @env("API_BASE_URL", "https://api.example.com"),
        "timeout": @env("API_TIMEOUT", "30"),
        "retries": @env("API_RETRIES", "3"),
        "headers": {
            "User-Agent": "TuskLang/1.0",
            "Accept": "application/json"
        }
    },
    "cache": {
        "enabled": @env("CACHE_ENABLED", "true"),
        "driver": @env("CACHE_DRIVER", "redis"),
        "ttl": @env("CACHE_TTL", "3600"),
        "prefix": @env("CACHE_PREFIX", "tusk:")
    }
}

# Environment-specific overrides
$environment_config: {
    "development": {
        "database": {
            "host": "localhost",
            "name": "tusklang_dev"
        },
        "api": {
            "base_url": "http://localhost:3000"
        },
        "cache": {
            "enabled": false
        }
    },
    "production": {
        "database": {
            "host": @env("PROD_DB_HOST"),
            "name": @env("PROD_DB_NAME"),
            "ssl": true
        },
        "api": {
            "timeout": 60,
            "retries": 5
        }
    }
}

# Merge configurations
$final_config: @object.merge($app_config, $environment_config[@env("ENVIRONMENT", "development")])

# Extract specific configurations
$db_config: @object.get($final_config, "database")
$api_config: @object.get($final_config, "api")
$cache_config: @object.get($final_config, "cache")
```

### 2. API Response Processing
```ini
[api_processing]
# Process API response objects
$api_responses: {
    "user_data": {
        "id": 123,
        "name": "Alice Johnson",
        "email": "alice@example.com",
        "profile": {
            "age": 28,
            "city": "San Francisco",
            "preferences": {
                "theme": "dark",
                "notifications": true,
                "language": "en"
            }
        },
        "metadata": {
            "created_at": "2024-01-15T10:30:00Z",
            "updated_at": "2024-01-20T14:45:00Z",
            "version": "1.0"
        }
    },
    "order_data": {
        "id": 456,
        "user_id": 123,
        "items": [
            {"product_id": 1, "quantity": 2, "price": 25.00},
            {"product_id": 3, "quantity": 1, "price": 50.00}
        ],
        "total": 100.00,
        "status": "completed",
        "shipping": {
            "address": "123 Main St, San Francisco, CA",
            "method": "express",
            "cost": 10.00
        }
    }
}

# Extract and transform data
$user_processing: {
    "basic_info": {
        "id": @object.get($api_responses.user_data, "id"),
        "name": @object.get($api_responses.user_data, "name"),
        "email": @object.get($api_responses.user_data, "email")
    },
    "profile_info": @object.get($api_responses.user_data, "profile"),
    "preferences": @object.get($api_responses.user_data, "profile.preferences"),
    "is_adult": @object.get($api_responses.user_data, "profile.age") >= 18,
    "location": @object.get($api_responses.user_data, "profile.city")
}

$order_processing: {
    "order_summary": {
        "id": @object.get($api_responses.order_data, "id"),
        "total": @object.get($api_responses.order_data, "total"),
        "status": @object.get($api_responses.order_data, "status"),
        "item_count": @array.length(@object.get($api_responses.order_data, "items"))
    },
    "shipping_info": @object.get($api_responses.order_data, "shipping"),
    "is_express": @object.get($api_responses.order_data, "shipping.method") == "express",
    "total_with_shipping": @object.get($api_responses.order_data, "total") + @object.get($api_responses.order_data, "shipping.cost")
}
```

### 3. Data Transformation and Validation
```ini
[data_transformation]
# Transform and validate data objects
$raw_data: {
    "user_input": {
        "first_name": "John",
        "last_name": "Doe",
        "email": "john.doe@example.com",
        "age": "30",
        "city": "New York"
    },
    "form_data": {
        "username": "johndoe",
        "password": "secret123",
        "confirm_password": "secret123",
        "terms_accepted": "true"
    }
}

# Validate and transform user input
$validated_user: {
    "name": @object.get($raw_data.user_input, "first_name") + " " + @object.get($raw_data.user_input, "last_name"),
    "email": @object.get($raw_data.user_input, "email"),
    "age": @math.to_number(@object.get($raw_data.user_input, "age")),
    "city": @object.get($raw_data.user_input, "city"),
    "validation": {
        "email_valid": @regex.match(@object.get($raw_data.user_input, "email"), "^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"),
        "age_valid": @math.to_number(@object.get($raw_data.user_input, "age")) >= 18,
        "name_valid": @string.length(@object.get($raw_data.user_input, "first_name")) > 0 && @string.length(@object.get($raw_data.user_input, "last_name")) > 0
    }
}

# Validate form data
$validated_form: {
    "username": @object.get($raw_data.form_data, "username"),
    "password": @object.get($raw_data.form_data, "password"),
    "validation": {
        "passwords_match": @object.get($raw_data.form_data, "password") == @object.get($raw_data.form_data, "confirm_password"),
        "password_strong": @string.length(@object.get($raw_data.form_data, "password")) >= 8,
        "terms_accepted": @object.get($raw_data.form_data, "terms_accepted") == "true",
        "username_available": @sql("SELECT COUNT(*) FROM users WHERE username = ?", @object.get($raw_data.form_data, "username")) == 0
    }
}

# Create final user object
$final_user: @object.merge($validated_user, {
    "username": @object.get($validated_form, "username"),
    "password_hash": @encrypt.hash(@object.get($validated_form, "password"), "bcrypt"),
    "created_at": @date("Y-m-d H:i:s"),
    "status": "active"
})
```

### 4. Object Serialization and Storage
```ini
[object_serialization]
# Serialize objects for storage and transmission
$complex_object: {
    "user": {
        "id": 123,
        "name": "Alice Johnson",
        "preferences": {
            "theme": "dark",
            "notifications": true,
            "language": "en"
        }
    },
    "session": {
        "id": "sess_abc123",
        "created_at": @date("Y-m-d H:i:s"),
        "expires_at": @date.add(@date("Y-m-d H:i:s"), "2h")
    },
    "metadata": {
        "version": "1.0",
        "source": "api",
        "timestamp": @date("Y-m-d H:i:s")
    }
}

# Serialize to different formats
$serialization: {
    "json": @object.to_json($complex_object),
    "yaml": @object.to_yaml($complex_object),
    "xml": @object.to_xml($complex_object),
    "base64": @object.to_base64($complex_object)
}

# Store serialized objects
$storage_operations: {
    "save_json": @file.write("/tmp/user_data.json", $serialization.json),
    "save_yaml": @file.write("/tmp/user_data.yaml", $serialization.yaml),
    "cache_object": @cache("1h", "user_object_123", $complex_object),
    "database_storage": @sql("INSERT INTO user_sessions (user_id, session_data) VALUES (?, ?)", 
        @object.get($complex_object, "user.id"), 
        $serialization.json
    )
}

# Deserialize objects
$deserialization: {
    "from_json": @object.from_json($serialization.json),
    "from_yaml": @object.from_yaml($serialization.yaml),
    "from_xml": @object.from_xml($serialization.xml),
    "from_base64": @object.from_base64($serialization.base64)
}
```

## 🧠 Advanced @object Patterns

### Object Composition and Inheritance
```ini
[object_composition]
# Compose objects from multiple sources
$base_user: {
    "id": 123,
    "name": "Alice Johnson",
    "email": "alice@example.com"
}

$user_profile: {
    "age": 28,
    "city": "San Francisco",
    "preferences": {
        "theme": "dark",
        "notifications": true
    }
}

$user_permissions: {
    "roles": ["user", "editor"],
    "permissions": ["read", "write", "delete"],
    "access_level": "standard"
}

# Compose complete user object
$complete_user: @object.merge($base_user, $user_profile, $user_permissions, {
    "created_at": @date("Y-m-d H:i:s"),
    "status": "active"
})

# Create user templates
$user_templates: {
    "admin": {
        "roles": ["admin"],
        "permissions": ["read", "write", "delete", "admin"],
        "access_level": "admin"
    },
    "moderator": {
        "roles": ["moderator"],
        "permissions": ["read", "write", "moderate"],
        "access_level": "moderator"
    },
    "user": {
        "roles": ["user"],
        "permissions": ["read", "write"],
        "access_level": "standard"
    }
}

# Apply template to user
$templated_user: @object.merge($base_user, $user_profile, $user_templates.user)
```

### Object Validation and Schema
```ini
[object_validation]
# Define object schemas
$user_schema: {
    "required": ["id", "name", "email"],
    "properties": {
        "id": {"type": "number", "min": 1},
        "name": {"type": "string", "min_length": 2},
        "email": {"type": "string", "pattern": "^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$"},
        "age": {"type": "number", "min": 0, "max": 150},
        "city": {"type": "string", "optional": true}
    }
}

$config_schema: {
    "required": ["database", "api"],
    "properties": {
        "database": {
            "type": "object",
            "required": ["host", "port", "name"],
            "properties": {
                "host": {"type": "string"},
                "port": {"type": "number", "min": 1, "max": 65535},
                "name": {"type": "string"}
            }
        },
        "api": {
            "type": "object",
            "required": ["url", "timeout"],
            "properties": {
                "url": {"type": "string", "pattern": "^https?://"},
                "timeout": {"type": "number", "min": 1}
            }
        }
    }
}

# Validate objects against schemas
$validation_results: {
    "user_valid": @object.validate($complete_user, $user_schema),
    "config_valid": @object.validate($final_config, $config_schema),
    "user_errors": @object.validate_errors($complete_user, $user_schema),
    "config_errors": @object.validate_errors($final_config, $config_schema)
}
```

### Performance Optimization
```ini
[performance_optimization]
# Optimize object operations
$optimized_objects: {
    "cached_user": @cache("1h", "user_123", $complete_user),
    "compressed_object": @object.compress($complex_object),
    "minimal_object": @object.pick($complex_object, ["id", "name", "email"]),
    "filtered_object": @object.omit($complex_object, ["metadata", "session"])
}

# Object pooling for performance
$object_pool: {
    "user_template": @object.create({
        "id": 0,
        "name": "",
        "email": "",
        "status": "active"
    }),
    "config_template": @object.create({
        "database": {},
        "api": {},
        "cache": {}
    })
}

# Batch object operations
$batch_operations: {
    "multiple_users": @array.map($user_ids, @object.merge($object_pool.user_template, {
        "id": "item",
        "name": @sql("SELECT name FROM users WHERE id = ?", "item"),
        "email": @sql("SELECT email FROM users WHERE id = ?", "item")
    })),
    "bulk_update": @array.map($users_to_update, @object.set(item, "updated_at", @date("Y-m-d H:i:s")))
}
```

## 🛡️ Security & Performance Notes
- **Object validation:** Always validate object structures and properties
- **Deep cloning:** Use deep cloning for complex nested objects
- **Property access:** Validate property paths to prevent errors
- **Memory management:** Monitor object sizes for large data structures
- **Serialization security:** Validate serialized data before deserialization
- **Object immutability:** Consider immutability for critical objects

## 🐞 Troubleshooting
- **Property not found:** Check object structure and property paths
- **Type errors:** Validate object property types before operations
- **Memory issues:** Monitor object sizes and implement cleanup
- **Serialization errors:** Validate data before serialization
- **Performance problems:** Optimize object operations for large datasets

## 💡 Best Practices
- **Use meaningful property names:** Create descriptive and consistent property names
- **Validate object structures:** Always validate objects against schemas
- **Handle nested properties:** Use dot notation for nested property access
- **Implement error handling:** Handle missing properties gracefully
- **Optimize for performance:** Use appropriate object operations for your use case
- **Document object structures:** Document complex object structures for maintainability

## 🔗 Cross-References
- [@ Operator Introduction](024-at-operator-intro-bash.md)
- [@array Function](055-at-array-function-bash.md)
- [@json Function](058-at-json-function-bash.md)
- [Data Structures](109-data-structures-bash.md)
- [Object Validation](110-object-validation-bash.md)

---

**Master @object in TuskLang and wield the power of data structures in your configurations. 🏗️** 