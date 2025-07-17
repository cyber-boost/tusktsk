# 📋 TuskLang Bash @json Function Guide

**"We don't bow to any king" – JSON is your configuration's language.**

The @json function in TuskLang is your JSON processing powerhouse, enabling dynamic JSON parsing, generation, and manipulation directly within your configuration files. Whether you're working with REST APIs, processing JSON data, or generating JSON responses, @json provides the flexibility and power to handle structured data seamlessly.

## 🎯 What is @json?
The @json function provides JSON processing capabilities in TuskLang. It offers:
- **JSON parsing** - Parse JSON strings and extract data
- **JSON generation** - Create JSON documents dynamically
- **JSON transformation** - Transform JSON data between formats
- **JSON validation** - Validate JSON against schemas
- **JSON querying** - Query JSON data using JSONPath expressions

## 📝 Basic @json Syntax

### Simple JSON Parsing
```ini
[simple_json]
# Parse JSON string
$json_data: '{"name": "John", "email": "john@example.com", "age": 30}'
parsed_json: @json.parse($json_data)
user_name: @json.get($parsed_json, "$.name")
user_email: @json.get($parsed_json, "$.email")
user_age: @json.get($parsed_json, "$.age")
```

### JSON File Processing
```ini
[json_file_processing]
# Parse JSON file
config_json: @json.parse(@file.read("/etc/app/config.json"))
api_config: @json.get($config_json, "$.api")
database_config: @json.get($config_json, "$.database")
```

### JSON Generation
```ini
[json_generation]
# Generate JSON dynamically
$user_data: {"name": "Alice", "email": "alice@example.com", "age": 30}
user_json: @json.generate($user_data)

# Generate complex JSON
$config_data: {
    "database": {"host": "localhost", "port": 3306, "name": "app_db"},
    "api": {"url": "https://api.example.com", "timeout": 30}
}
config_json: @json.generate($config_data)
```

## 🚀 Quick Start Example

```bash
#!/bin/bash
source tusk-bash.sh

cat > json-quickstart.tsk << 'EOF'
[json_parsing]
# Parse JSON data
$json_string: '{"app": {"name": "TuskLang", "version": "2.1.0"}, "database": {"host": "localhost", "port": 3306}, "api": {"url": "https://api.example.com"}}'
parsed_config: @json.parse($json_string)

# Extract data using JSONPath
app_name: @json.get($parsed_config, "$.app.name")
app_version: @json.get($parsed_config, "$.app.version")
db_host: @json.get($parsed_config, "$.database.host")
api_url: @json.get($parsed_config, "$.api.url")

[json_generation]
# Generate JSON
$user_info: {"name": "John Doe", "email": "john@example.com", "role": "admin"}
user_json: @json.generate($user_info)

# Generate nested JSON
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
server_json: @json.generate($nested_data)

[json_transformation]
# Transform JSON data
$source_json: '{"users": [{"id": 1, "name": "Alice", "email": "alice@example.com"}, {"id": 2, "name": "Bob", "email": "bob@example.com"}]}'
transformed_json: @json.transform($source_json, "$.users[*]", {
    "template": {"person_id": "$.id", "full_name": "$.name", "contact": "$.email"}
})
EOF

config=$(tusk_parse json-quickstart.tsk)

echo "=== JSON Parsing ==="
echo "App Name: $(tusk_get "$config" json_parsing.app_name)"
echo "App Version: $(tusk_get "$config" json_parsing.app_version)"
echo "Database Host: $(tusk_get "$config" json_parsing.db_host)"
echo "API URL: $(tusk_get "$config" json_parsing.api_url)"

echo ""
echo "=== JSON Generation ==="
echo "User JSON: $(tusk_get "$config" json_generation.user_json)"
echo "Server JSON: $(tusk_get "$config" json_generation.server_json)"

echo ""
echo "=== JSON Transformation ==="
echo "Transformed JSON: $(tusk_get "$config" json_transformation.transformed_json)"
```

## 🔗 Real-World Use Cases

### 1. REST API Integration
```ini
[rest_api_integration]
# Process JSON API responses
$api_response: @http("GET", "https://api.example.com/users")
parsed_response: @json.parse($api_response)

# Extract user data
$users: @json.get_all($parsed_response, "$.users")
user_count: @array.length($users)

# Process each user
$user_data: @array.map($users, {
    "id": @json.get(item, "$.id"),
    "name": @json.get(item, "$.name"),
    "email": @json.get(item, "$.email"),
    "status": @json.get(item, "$.status")
})

# Generate JSON response
$response_data: {
    "status": "success",
    "count": $user_count,
    "users": $user_data,
    "timestamp": @date("Y-m-d H:i:s")
}
json_response: @json.generate($response_data)
```

### 2. Configuration Management
```ini
[json_config_management]
# Parse configuration JSON
$config_json: @json.parse(@file.read("/etc/app/config.json"))

# Extract configuration sections
$app_config: {
    "name": @json.get($config_json, "$.app.name"),
    "version": @json.get($config_json, "$.app.version"),
    "environment": @json.get($config_json, "$.app.environment")
}

$database_config: {
    "host": @json.get($config_json, "$.database.host"),
    "port": @json.get($config_json, "$.database.port"),
    "name": @json.get($config_json, "$.database.name"),
    "credentials": {
        "username": @json.get($config_json, "$.database.credentials.username"),
        "password": @json.get($config_json, "$.database.credentials.password")
    }
}

# Generate updated configuration
$updated_config: {
    "app": $app_config,
    "database": $database_config,
    "timestamp": @date("Y-m-d H:i:s")
}

updated_json: @json.generate($updated_config)
@file.write("/etc/app/updated-config.json", $updated_json)
```

### 3. Data Transformation and Migration
```ini
[json_data_transformation]
# Transform legacy JSON data
$legacy_json: @json.parse(@file.read("/var/data/legacy-users.json"))

# Extract and transform user data
$legacy_users: @json.get_all($legacy_json, "$.users")
$transformed_users: @array.map($legacy_users, {
    "user_id": @json.get(item, "$.id"),
    "first_name": @json.get(item, "$.firstName"),
    "last_name": @json.get(item, "$.lastName"),
    "email_address": @json.get(item, "$.emailAddress"),
    "phone_number": @json.get(item, "$.phoneNumber"),
    "created_date": @json.get(item, "$.createdDate")
})

# Generate new JSON format
$new_format: {
    "users": {
        "version": "2.0",
        "export_date": @date("Y-m-d H:i:s"),
        "user_list": $transformed_users
    }
}

new_json: @json.generate($new_format)
@file.write("/var/data/migrated-users.json", $new_json)
```

### 4. Webhook Processing
```ini
[webhook_processing]
# Process webhook JSON payload
$webhook_payload: @http("POST", "https://webhook.example.com/events", {
    "headers": {"Content-Type": "application/json"},
    "body": $event_data
})

parsed_webhook: @json.parse($webhook_payload)

# Extract webhook data
$webhook_info: {
    "event_type": @json.get($parsed_webhook, "$.event"),
    "timestamp": @json.get($parsed_webhook, "$.timestamp"),
    "user_id": @json.get($parsed_webhook, "$.data.user_id"),
    "action": @json.get($parsed_webhook, "$.data.action")
}

# Process webhook based on event type
@if($webhook_info.event_type == "user.created", {
    "action": "create_user",
    "data": @json.get($parsed_webhook, "$.data")
}, @if($webhook_info.event_type == "user.updated", {
    "action": "update_user",
    "data": @json.get($parsed_webhook, "$.data")
}, {
    "action": "unknown_event",
    "event_type": $webhook_info.event_type
}))
```

## 🧠 Advanced @json Patterns

### JSONPath Query Optimization
```ini
[jsonpath_optimization]
# Optimize JSONPath queries for performance
$large_json: @json.parse(@file.read("/var/data/large-dataset.json"))

# Use efficient JSONPath queries
$optimized_queries: {
    "users_by_status": @json.get_all($large_json, "$.users[?(@.status=='active')]"),
    "recent_orders": @json.get_all($large_json, "$.orders[?(@.date>='2024-01-01')]"),
    "high_value_customers": @json.get_all($large_json, "$.customers[?(@.total_spent>1000)]")
}

# Cache frequently accessed data
$cached_data: {
    "user_count": @array.length(@json.get_all($large_json, "$.users")),
    "order_count": @array.length(@json.get_all($large_json, "$.orders")),
    "customer_count": @array.length(@json.get_all($large_json, "$.customers"))
}
```

### JSON Schema Validation
```ini
[json_validation]
# Validate JSON against schema
$json_document: @file.read("/var/data/user-data.json")
$schema_file: @file.read("/etc/schemas/user-schema.json")

# Validate JSON structure
validation_result: @json.validate($json_document, $schema_file)

# Handle validation errors
@if($validation_result.valid, {
    "action": "process_json",
    "data": @json.parse($json_document)
}, {
    "action": "handle_validation_errors",
    "errors": $validation_result.errors,
    "timestamp": @date("Y-m-d H:i:s")
})
```

### JSON Transformation Pipeline
```ini
[json_pipeline]
# Multi-step JSON transformation pipeline
$source_json: @json.parse(@file.read("/var/data/source.json"))

# Step 1: Extract and filter data
$filtered_data: @json.transform($source_json, "$.items[?(@.status=='active')]", {
    "template": {"id": "$.id", "name": "$.name", "value": "$.value"}
})

# Step 2: Enrich data
$enriched_data: @json.transform($filtered_data, "$[*]", {
    "template": {"id": "$.id", "name": "$.name", "value": "$.value", "processed_at": @date("Y-m-d H:i:s")}
})

# Step 3: Aggregate data
$aggregated_data: {
    "total_items": @array.length($enriched_data),
    "total_value": @array.sum(@array.map($enriched_data, "$.value"))
}

# Final output
final_json: @json.generate({
    "pipeline_version": "1.0",
    "processed_at": @date("Y-m-d H:i:s"),
    "data": $aggregated_data
})
```

### JSON Caching and Performance
```ini
[json_caching]
# Implement JSON caching for performance
$cache_key: "user_data_" + @env("USER_ID")
cached_user_data: @cache.get($cache_key)

# Parse cached data or fetch fresh
$user_data: @if($cached_user_data != "null", 
    @json.parse($cached_user_data), 
    @json.parse(@http("GET", "https://api.example.com/users/" + @env("USER_ID")))
)

# Cache parsed data
@cache.set($cache_key, @json.generate($user_data), "1h")

# Process user data
$processed_data: {
    "user_id": @json.get($user_data, "$.id"),
    "name": @json.get($user_data, "$.name"),
    "email": @json.get($user_data, "$.email"),
    "last_login": @json.get($user_data, "$.last_login")
}
```

## 🛡️ Security & Performance Notes
- **JSON injection:** Validate and sanitize JSON input to prevent injection attacks
- **Memory usage:** Monitor memory consumption when processing large JSON files
- **JSONPath injection:** Sanitize JSONPath expressions to prevent injection attacks
- **Schema validation:** Always validate JSON against schemas when possible
- **Performance optimization:** Use efficient JSONPath queries and caching strategies
- **Data privacy:** Ensure sensitive data is properly handled in JSON operations

## 🐞 Troubleshooting
- **Parsing errors:** Check JSON syntax and structure for validity
- **JSONPath failures:** Verify JSONPath expressions and JSON structure
- **Memory issues:** Optimize JSON processing for large files
- **Schema validation:** Ensure JSON conforms to expected schema
- **Performance problems:** Use efficient JSONPath queries and caching

## 💡 Best Practices
- **Validate JSON:** Always validate JSON against schemas when possible
- **Use efficient JSONPath:** Optimize JSONPath queries for better performance
- **Handle errors:** Implement proper error handling for JSON operations
- **Cache results:** Cache frequently accessed JSON data
- **Sanitize input:** Validate and sanitize JSON input data
- **Document schemas:** Document JSON schemas and expected formats

## 🔗 Cross-References
- [@ Operator Introduction](024-at-operator-intro-bash.md)
- [@file Function](038-at-file-function-bash.md)
- [@http Function](037-at-http-function-bash.md)
- [@cache Function](033-at-cache-function-bash.md)
- [API Integration](077-api-integration-bash.md)

---

**Master @json in TuskLang and structure your data with flexibility. 📋** 