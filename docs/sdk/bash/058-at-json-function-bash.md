# 📄 TuskLang Bash @json Function Guide

**"We don't bow to any king" – JSON is your configuration's data format.**

The @json function in TuskLang is your JSON manipulation powerhouse, enabling dynamic JSON operations, data parsing, and format conversion directly within your configuration files. Whether you're parsing API responses, creating JSON documents, or transforming data structures, @json provides the flexibility and power to work with JSON data seamlessly.

## 🎯 What is @json?
The @json function provides JSON operations in TuskLang. It offers:
- **JSON parsing** - Parse JSON strings into data structures
- **JSON generation** - Create JSON from data structures
- **JSON validation** - Validate JSON syntax and structure
- **JSON transformation** - Transform and manipulate JSON data
- **JSON formatting** - Format JSON with proper indentation and structure

## 📝 Basic @json Syntax

### JSON Parsing
```ini
[json_parsing]
# Parse simple JSON strings
$simple_json: '{"name": "John", "age": 30, "city": "New York"}'
parsed_simple: @json.parse($simple_json)

$array_json: '[1, 2, 3, 4, 5]'
parsed_array: @json.parse($array_json)

$nested_json: '{"user": {"name": "Alice", "profile": {"age": 25, "city": "San Francisco"}}}'
parsed_nested: @json.parse($nested_json)

# Parse JSON with validation
$valid_json: '{"valid": true, "data": "test"}'
validated_json: @json.parse($valid_json, true)

# Parse JSON with error handling
$invalid_json: '{"invalid": true, "missing": quote}'
safe_parse: @json.parse($invalid_json, false, {"error": "Invalid JSON"})
```

### JSON Generation
```ini
[json_generation]
# Generate JSON from data structures
$user_data: {
    "id": 123,
    "name": "Alice Johnson",
    "email": "alice@example.com",
    "preferences": {
        "theme": "dark",
        "notifications": true
    }
}

user_json: @json.generate($user_data)
user_json_pretty: @json.generate($user_data, true)

# Generate JSON arrays
$numbers: [1, 2, 3, 4, 5]
numbers_json: @json.generate($numbers)
numbers_json_pretty: @json.generate($numbers, true)

# Generate JSON with custom options
$config_data: {
    "database": {
        "host": "localhost",
        "port": 3306
    },
    "api": {
        "url": "https://api.example.com"
    }
}

config_json: @json.generate($config_data, true, {
    "indent": 4,
    "sort_keys": true
})
```

### JSON Validation
```ini
[json_validation]
# Validate JSON syntax
$valid_json_string: '{"name": "John", "age": 30}'
is_valid: @json.validate($valid_json_string)

$invalid_json_string: '{"name": "John", "age": 30,}'
is_invalid: @json.validate($invalid_json_string)

# Validate JSON structure
$json_schema: {
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

schema_validation: @json.validate_schema($test_data, $json_schema)
```

## 🚀 Quick Start Example

```bash
#!/bin/bash
source tusk-bash.sh

cat > json-quickstart.tsk << 'EOF'
[json_parsing]
# Parse JSON data
$api_response: '{"status": "success", "data": {"users": [{"id": 1, "name": "Alice"}, {"id": 2, "name": "Bob"}]}}'
parsed_response: @json.parse($api_response)

$config_json: '{"database": {"host": "localhost", "port": 3306}, "api": {"timeout": 30}}'
parsed_config: @json.parse($config_json)

[json_generation]
# Generate JSON from data
$user_data: {
    "id": 123,
    "name": "Charlie Brown",
    "email": "charlie@example.com",
    "profile": {
        "age": 30,
        "city": "Chicago",
        "preferences": {
            "theme": "light",
            "notifications": false
        }
    }
}

user_json: @json.generate($user_data)
user_json_pretty: @json.generate($user_data, true)

[json_validation]
# Validate JSON
$test_json: '{"name": "David", "age": 35, "email": "david@example.com"}'
is_valid: @json.validate($test_json)

$invalid_json: '{"name": "Eve", "age": 28, "email": "eve@example.com",}'
is_invalid: @json.validate($invalid_json)

[json_transformation]
# Transform JSON data
$raw_data: {
    "users": [
        {"id": 1, "name": "Alice", "status": "active"},
        {"id": 2, "name": "Bob", "status": "inactive"},
        {"id": 3, "name": "Charlie", "status": "active"}
    ]
}

active_users: @json.filter($raw_data.users, "item.status == 'active'")
user_names: @json.map($raw_data.users, "item.name")
user_count: @json.length($raw_data.users)
EOF

config=$(tusk_parse json-quickstart.tsk)

echo "=== JSON Parsing ==="
echo "Parsed Response: $(tusk_get "$config" json_parsing.parsed_response)"
echo "Parsed Config: $(tusk_get "$config" json_parsing.parsed_config)"

echo ""
echo "=== JSON Generation ==="
echo "User JSON: $(tusk_get "$config" json_generation.user_json)"
echo "Pretty JSON: $(tusk_get "$config" json_generation.user_json_pretty)"

echo ""
echo "=== JSON Validation ==="
echo "Is Valid: $(tusk_get "$config" json_validation.is_valid)"
echo "Is Invalid: $(tusk_get "$config" json_validation.is_invalid)"

echo ""
echo "=== JSON Transformation ==="
echo "Active Users: $(tusk_get "$config" json_transformation.active_users)"
echo "User Names: $(tusk_get "$config" json_transformation.user_names)"
echo "User Count: $(tusk_get "$config" json_transformation.user_count)"
```

## 🔗 Real-World Use Cases

### 1. API Response Processing
```ini
[api_processing]
# Process API responses with JSON
$api_functions: {
    "parse_user_api": @function.create("response", """
        var data = @json.parse(response);
        if (data.status === 'success') {
            return {
                'users': data.data.users,
                'total': data.data.total,
                'page': data.data.page
            };
        } else {
            return {
                'error': true,
                'message': data.message || 'API request failed'
            };
        }
    """),
    
    "parse_weather_api": @function.create("response", """
        var data = @json.parse(response);
        return {
            'location': data.location.name,
            'temperature': data.current.temp_c,
            'condition': data.current.condition.text,
            'humidity': data.current.humidity,
            'wind_speed': data.current.wind_kph
        };
    """),
    
    "parse_stock_api": @function.create("response", """
        var data = @json.parse(response);
        return data.map(function(stock) {
            return {
                'symbol': stock.symbol,
                'price': stock.price,
                'change': stock.change,
                'change_percent': stock.change_percent
            };
        });
    """)
}

# Process different API responses
$api_responses: {
    "user_response": '{"status": "success", "data": {"users": [{"id": 1, "name": "Alice"}, {"id": 2, "name": "Bob"}], "total": 2, "page": 1}}',
    "weather_response": '{"location": {"name": "New York"}, "current": {"temp_c": 22, "condition": {"text": "Partly cloudy"}, "humidity": 65, "wind_kph": 15}}',
    "stock_response": '[{"symbol": "AAPL", "price": 150.25, "change": 2.50, "change_percent": 1.69}, {"symbol": "GOOGL", "price": 2750.00, "change": -15.00, "change_percent": -0.54}]'
}

$processed_responses: {
    "users": @function.call("parse_user_api", $api_responses.user_response),
    "weather": @function.call("parse_weather_api", $api_responses.weather_response),
    "stocks": @function.call("parse_stock_api", $api_responses.stock_response)
}

# Generate JSON responses
$api_responses_generated: {
    "user_list": @json.generate($processed_responses.users, true),
    "weather_data": @json.generate($processed_responses.weather, true),
    "stock_data": @json.generate($processed_responses.stocks, true)
}
```

### 2. Configuration Management
```ini
[config_management]
# Manage configuration with JSON
$config_templates: {
    "database": {
        "mysql": {
            "host": "localhost",
            "port": 3306,
            "database": "tusklang",
            "username": "root",
            "password": "",
            "charset": "utf8mb4",
            "options": {
                "timezone": "UTC",
                "ssl": false
            }
        },
        "postgresql": {
            "host": "localhost",
            "port": 5432,
            "database": "tusklang",
            "username": "postgres",
            "password": "",
            "ssl": true,
            "options": {
                "timezone": "UTC",
                "application_name": "TuskLang"
            }
        }
    },
    "api": {
        "base_url": "https://api.example.com",
        "timeout": 30,
        "retries": 3,
        "headers": {
            "User-Agent": "TuskLang/1.0",
            "Accept": "application/json",
            "Content-Type": "application/json"
        }
    }
}

# Generate configuration JSON
$config_generation: {
    "mysql_config": @json.generate($config_templates.database.mysql, true),
    "postgresql_config": @json.generate($config_templates.database.postgresql, true),
    "api_config": @json.generate($config_templates.api, true),
    "full_config": @json.generate($config_templates, true)
}

# Parse configuration from JSON
$config_json_string: '{"environment": "production", "database": {"host": "prod.example.com", "port": 3306}, "api": {"timeout": 60}}'
parsed_config: @json.parse($config_json_string)

# Merge configurations
$merged_config: @object.merge($config_templates, $parsed_config)
merged_config_json: @json.generate($merged_config, true)
```

### 3. Data Transformation and Validation
```ini
[data_transformation]
# Transform data with JSON operations
$data_transformation: {
    "normalize_user_data": @function.create("users_json", """
        var users = @json.parse(users_json);
        var normalized = [];
        
        for (var i = 0; i < users.length; i++) {
            var user = users[i];
            normalized.push({
                'id': parseInt(user.id) || 0,
                'name': user.name.trim().toLowerCase(),
                'email': user.email.toLowerCase(),
                'age': parseInt(user.age) || 0,
                'status': user.status || 'inactive',
                'created_at': user.created_at || @date('Y-m-d H:i:s')
            });
        }
        
        return @json.generate(normalized, true);
    """),
    
    "validate_json_schema": @function.create("data_json, schema_json", """
        var data = @json.parse(data_json);
        var schema = @json.parse(schema_json);
        
        var errors = [];
        
        // Check required fields
        if (schema.required) {
            for (var i = 0; i < schema.required.length; i++) {
                var field = schema.required[i];
                if (!data.hasOwnProperty(field)) {
                    errors.push('Missing required field: ' + field);
                }
            }
        }
        
        // Check field types
        if (schema.properties) {
            for (var field in schema.properties) {
                if (data.hasOwnProperty(field)) {
                    var expected_type = schema.properties[field].type;
                    var actual_type = typeof data[field];
                    
                    if (expected_type === 'number' && actual_type !== 'number') {
                        errors.push('Field ' + field + ' must be a number');
                    } else if (expected_type === 'string' && actual_type !== 'string') {
                        errors.push('Field ' + field + ' must be a string');
                    }
                }
            }
        }
        
        return @json.generate({
            'valid': errors.length === 0,
            'errors': errors,
            'data': data
        }, true);
    """),
    
    "filter_json_data": @function.create("data_json, filter_criteria", """
        var data = @json.parse(data_json);
        var filtered = [];
        
        for (var i = 0; i < data.length; i++) {
            var item = data[i];
            var matches = true;
            
            for (var field in filter_criteria) {
                if (item[field] !== filter_criteria[field]) {
                    matches = false;
                    break;
                }
            }
            
            if (matches) {
                filtered.push(item);
            }
        }
        
        return @json.generate(filtered, true);
    """)
}

# Transform sample data
$sample_user_data: '[{"id": "1", "name": "  Alice  ", "email": "ALICE@EXAMPLE.COM", "age": "25", "status": "active"}, {"id": "2", "name": "Bob", "email": "bob@example.com", "age": "30", "status": "inactive"}]'

$transformation_results: {
    "normalized_users": @function.call("normalize_user_data", $sample_user_data),
    "active_users": @function.call("filter_json_data", $sample_user_data, '{"status": "active"}'),
    "validation_schema": {
        "type": "object",
        "properties": {
            "id": {"type": "number"},
            "name": {"type": "string"},
            "email": {"type": "string"}
        },
        "required": ["id", "name", "email"]
    }
}
```

### 4. File and Database Operations
```ini
[file_operations]
# JSON file operations
$file_operations: {
    "save_json_file": @function.create("data, filepath", """
        var json_string = @json.generate(data, true);
        return @file.write(filepath, json_string);
    """),
    
    "load_json_file": @function.create("filepath", """
        var json_string = @file.read(filepath);
        return @json.parse(json_string);
    """),
    
    "update_json_file": @function.create("filepath, updates", """
        var data = @function.call('load_json_file', filepath);
        var updated_data = @object.merge(data, updates);
        return @function.call('save_json_file', updated_data, filepath);
    """),
    
    "validate_json_file": @function.create("filepath, schema", """
        var data = @function.call('load_json_file', filepath);
        return @json.validate_schema(data, schema);
    """)
}

# Database JSON operations
$database_operations: {
    "store_json_data": @function.create("table, data", """
        var json_string = @json.generate(data);
        return @sql('INSERT INTO ' + table + ' (data) VALUES (?)', json_string);
    """),
    
    "retrieve_json_data": @function.create("table, id", """
        var result = @sql('SELECT data FROM ' + table + ' WHERE id = ?', id);
        if (result && result.length > 0) {
            return @json.parse(result[0].data);
        }
        return null;
    """),
    
    "update_json_data": @function.create("table, id, data", """
        var json_string = @json.generate(data);
        return @sql('UPDATE ' + table + ' SET data = ? WHERE id = ?', json_string, id);
    """),
    
    "query_json_field": @function.create("table, field_path, value", """
        var query = 'SELECT * FROM ' + table + ' WHERE JSON_EXTRACT(data, ?) = ?';
        return @sql(query, '$.' + field_path, value);
    """)
}

# Use file and database operations
$operation_examples: {
    "save_config": @function.call("save_json_file", $config_templates, "/tmp/config.json"),
    "load_config": @function.call("load_json_file", "/tmp/config.json"),
    "store_user": @function.call("store_json_data", "users", $user_data),
    "retrieve_user": @function.call("retrieve_json_data", "users", 123)
}
```

## 🧠 Advanced @json Patterns

### JSON Schema Validation
```ini
[json_schema]
# Define JSON schemas
$json_schemas: {
    "user_schema": {
        "type": "object",
        "properties": {
            "id": {"type": "number", "minimum": 1},
            "name": {"type": "string", "minLength": 2, "maxLength": 100},
            "email": {"type": "string", "format": "email"},
            "age": {"type": "number", "minimum": 0, "maximum": 150},
            "status": {"type": "string", "enum": ["active", "inactive", "pending"]}
        },
        "required": ["id", "name", "email"],
        "additionalProperties": false
    },
    
    "order_schema": {
        "type": "object",
        "properties": {
            "id": {"type": "number"},
            "customer_id": {"type": "number"},
            "items": {
                "type": "array",
                "items": {
                    "type": "object",
                    "properties": {
                        "product_id": {"type": "number"},
                        "quantity": {"type": "number", "minimum": 1},
                        "price": {"type": "number", "minimum": 0}
                    },
                    "required": ["product_id", "quantity", "price"]
                }
            },
            "total": {"type": "number", "minimum": 0},
            "status": {"type": "string", "enum": ["pending", "processing", "completed", "cancelled"]}
        },
        "required": ["id", "customer_id", "items", "total"]
    }
}

# Validate data against schemas
$validation_examples: {
    "valid_user": {
        "id": 1,
        "name": "Alice Johnson",
        "email": "alice@example.com",
        "age": 25,
        "status": "active"
    },
    
    "invalid_user": {
        "id": "not_a_number",
        "name": "A",
        "email": "invalid_email",
        "age": 200
    }
}

$schema_validation_results: {
    "valid_user_result": @json.validate_schema($validation_examples.valid_user, $json_schemas.user_schema),
    "invalid_user_result": @json.validate_schema($validation_examples.invalid_user, $json_schemas.user_schema)
}
```

### JSON Transformation and Mapping
```ini
[json_transformation]
# Advanced JSON transformation
$transformation_functions: {
    "flatten_json": @function.create("data, prefix", """
        var flattened = {};
        prefix = prefix || '';
        
        for (var key in data) {
            var new_key = prefix ? prefix + '.' + key : key;
            
            if (typeof data[key] === 'object' && data[key] !== null && !Array.isArray(data[key])) {
                var nested = @function.call('flatten_json', data[key], new_key);
                for (var nested_key in nested) {
                    flattened[nested_key] = nested[nested_key];
                }
            } else {
                flattened[new_key] = data[key];
            }
        }
        
        return flattened;
    """),
    
    "unflatten_json": @function.create("data", """
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
    
    "json_diff": @function.create("obj1, obj2", """
        var diff = {};
        
        for (var key in obj1) {
            if (!obj2.hasOwnProperty(key)) {
                diff[key] = {'removed': obj1[key]};
            } else if (JSON.stringify(obj1[key]) !== JSON.stringify(obj2[key])) {
                diff[key] = {
                    'old': obj1[key],
                    'new': obj2[key]
                };
            }
        }
        
        for (var key in obj2) {
            if (!obj1.hasOwnProperty(key)) {
                diff[key] = {'added': obj2[key]};
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
    
    "flattened_data": @function.call("flatten_json", $transformation_examples.nested_data),
    "unflattened_data": @function.call("unflatten_json", $transformation_examples.flattened_data)
}
```

## 🛡️ Security & Performance Notes
- **JSON injection:** Validate and sanitize JSON data to prevent injection attacks
- **Memory usage:** Monitor JSON size for large data structures
- **Parsing performance:** Use efficient JSON parsing for large datasets
- **Schema validation:** Always validate JSON against schemas for critical data
- **Error handling:** Implement proper error handling for JSON parsing failures
- **Data validation:** Validate JSON structure and content before processing

## 🐞 Troubleshooting
- **JSON parsing errors:** Check JSON syntax and validate structure
- **Memory issues:** Monitor JSON size and implement chunking for large data
- **Schema validation failures:** Review schema definitions and data structure
- **Performance problems:** Optimize JSON operations for large datasets
- **Encoding issues:** Ensure proper character encoding for JSON data

## 💡 Best Practices
- **Use schemas:** Always define and validate JSON schemas for critical data
- **Handle errors:** Implement proper error handling for JSON operations
- **Optimize size:** Minimize JSON size by removing unnecessary fields
- **Validate input:** Always validate JSON input before processing
- **Use pretty printing:** Use pretty printing for human-readable JSON
- **Document structure:** Document JSON structure and field meanings

## 🔗 Cross-References
- [@ Operator Introduction](024-at-operator-intro-bash.md)
- [@object Function](056-at-object-function-bash.md)
- [@validate Function](036-at-validate-function-bash.md)
- [Data Formats](113-data-formats-bash.md)
- [API Integration](114-api-integration-bash.md)

---

**Master @json in TuskLang and wield the power of data formats in your configurations. 📄** 