# 📄 TuskLang Bash @xml Function Guide

**"We don't bow to any king" – XML is your configuration's structure.**

The @xml function in TuskLang is your XML processing powerhouse, enabling dynamic XML parsing, generation, and manipulation directly within your configuration files. Whether you're working with XML APIs, processing XML data, or generating XML documents, @xml provides the flexibility and power to handle structured data seamlessly.

## 🎯 What is @xml?
The @xml function provides XML processing capabilities in TuskLang. It offers:
- **XML parsing** - Parse XML documents and extract data
- **XML generation** - Create XML documents dynamically
- **XML transformation** - Transform XML data between formats
- **XPath queries** - Query XML data using XPath expressions
- **XML validation** - Validate XML against schemas and DTDs

## 📝 Basic @xml Syntax

### Simple XML Parsing
```ini
[simple_xml]
# Parse XML string
$xml_data: "<user><name>John</name><email>john@example.com</email></user>"
parsed_xml: @xml.parse($xml_data)
user_name: @xml.get($parsed_xml, "//name")
user_email: @xml.get($parsed_xml, "//email")
```

### XML File Processing
```ini
[xml_file_processing]
# Parse XML file
config_xml: @xml.parse(@file.read("/etc/app/config.xml"))
api_config: @xml.get($config_xml, "//api")
database_config: @xml.get($config_xml, "//database")
```

### XML Generation
```ini
[xml_generation]
# Generate XML dynamically
$user_data: {"name": "Alice", "email": "alice@example.com", "age": 30}
user_xml: @xml.generate("user", $user_data)

# Generate complex XML
$config_data: {
    "database": {"host": "localhost", "port": 3306, "name": "app_db"},
    "api": {"url": "https://api.example.com", "timeout": 30}
}
config_xml: @xml.generate("configuration", $config_data)
```

## 🚀 Quick Start Example

```bash
#!/bin/bash
source tusk-bash.sh

cat > xml-quickstart.tsk << 'EOF'
[xml_parsing]
# Parse XML data
$xml_string: "<config><app name=\"TuskLang\" version=\"2.1.0\"><database host=\"localhost\" port=\"3306\"/><api url=\"https://api.example.com\"/></app></config>"
parsed_config: @xml.parse($xml_string)

# Extract data using XPath
app_name: @xml.get($parsed_config, "//app/@name")
app_version: @xml.get($parsed_config, "//app/@version")
db_host: @xml.get($parsed_config, "//database/@host")
api_url: @xml.get($parsed_config, "//api/@url")

[xml_generation]
# Generate XML
$user_info: {"name": "John Doe", "email": "john@example.com", "role": "admin"}
user_xml: @xml.generate("user", $user_info)

# Generate nested XML
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
server_xml: @xml.generate("server", $nested_data)

[xml_transformation]
# Transform XML data
$source_xml: "<users><user id=\"1\"><name>Alice</name><email>alice@example.com</email></user><user id=\"2\"><name>Bob</name><email>bob@example.com</email></user></users>"
transformed_xml: @xml.transform($source_xml, "//user", {
    "template": "<person><id>{@id}</id><full_name>{name}</full_name><contact>{email}</contact></person>"
})
EOF

config=$(tusk_parse xml-quickstart.tsk)

echo "=== XML Parsing ==="
echo "App Name: $(tusk_get "$config" xml_parsing.app_name)"
echo "App Version: $(tusk_get "$config" xml_parsing.app_version)"
echo "Database Host: $(tusk_get "$config" xml_parsing.db_host)"
echo "API URL: $(tusk_get "$config" xml_parsing.api_url)"

echo ""
echo "=== XML Generation ==="
echo "User XML: $(tusk_get "$config" xml_generation.user_xml)"
echo "Server XML: $(tusk_get "$config" xml_generation.server_xml)"

echo ""
echo "=== XML Transformation ==="
echo "Transformed XML: $(tusk_get "$config" xml_transformation.transformed_xml)"
```

## 🔗 Real-World Use Cases

### 1. API Integration with XML
```ini
[xml_api_integration]
# Process XML API responses
$api_response: @http("GET", "https://api.example.com/users.xml")
parsed_response: @xml.parse($api_response)

# Extract user data
$users: @xml.get_all($parsed_response, "//user")
user_count: @array.length($users)

# Process each user
$user_data: @array.map($users, {
    "id": @xml.get(item, "@id"),
    "name": @xml.get(item, "name"),
    "email": @xml.get(item, "email"),
    "status": @xml.get(item, "status")
})

# Generate XML response
$response_data: {
    "status": "success",
    "count": $user_count,
    "users": $user_data
}
xml_response: @xml.generate("response", $response_data)
```

### 2. Configuration Management
```ini
[xml_config_management]
# Parse configuration XML
$config_xml: @xml.parse(@file.read("/etc/app/config.xml"))

# Extract configuration sections
$app_config: {
    "name": @xml.get($config_xml, "//app/@name"),
    "version": @xml.get($config_xml, "//app/@version"),
    "environment": @xml.get($config_xml, "//app/environment")
}

$database_config: {
    "host": @xml.get($config_xml, "//database/@host"),
    "port": @xml.get($config_xml, "//database/@port"),
    "name": @xml.get($config_xml, "//database/@name"),
    "credentials": {
        "username": @xml.get($config_xml, "//database/credentials/username"),
        "password": @xml.get($config_xml, "//database/credentials/password")
    }
}

# Generate updated configuration
$updated_config: {
    "app": $app_config,
    "database": $database_config,
    "timestamp": @date("Y-m-d H:i:s")
}

updated_xml: @xml.generate("configuration", $updated_config)
@file.write("/etc/app/updated-config.xml", $updated_xml)
```

### 3. Data Transformation and Migration
```ini
[xml_data_transformation]
# Transform legacy XML data
$legacy_xml: @xml.parse(@file.read("/var/data/legacy-users.xml"))

# Extract and transform user data
$legacy_users: @xml.get_all($legacy_xml, "//user")
$transformed_users: @array.map($legacy_users, {
    "user_id": @xml.get(item, "@id"),
    "first_name": @xml.get(item, "firstName"),
    "last_name": @xml.get(item, "lastName"),
    "email_address": @xml.get(item, "emailAddress"),
    "phone_number": @xml.get(item, "phoneNumber"),
    "created_date": @xml.get(item, "createdDate")
})

# Generate new XML format
$new_format: {
    "users": {
        "version": "2.0",
        "export_date": @date("Y-m-d H:i:s"),
        "user_list": $transformed_users
    }
}

new_xml: @xml.generate("user_export", $new_format)
@file.write("/var/data/migrated-users.xml", $new_xml)
```

### 4. RSS Feed Processing
```ini
[rss_processing]
# Process RSS feed
$rss_feed: @http("GET", "https://blog.example.com/feed.xml")
parsed_rss: @xml.parse($rss_feed)

# Extract feed information
$feed_info: {
    "title": @xml.get($parsed_rss, "//channel/title"),
    "description": @xml.get($parsed_rss, "//channel/description"),
    "link": @xml.get($parsed_rss, "//channel/link"),
    "last_updated": @xml.get($parsed_rss, "//channel/lastBuildDate")
}

# Extract articles
$articles: @xml.get_all($parsed_rss, "//item")
$article_data: @array.map($articles, {
    "title": @xml.get(item, "title"),
    "link": @xml.get(item, "link"),
    "description": @xml.get(item, "description"),
    "pub_date": @xml.get(item, "pubDate"),
    "author": @xml.get(item, "author")
})

# Generate processed feed
$processed_feed: {
    "feed": $feed_info,
    "articles": $article_data,
    "processed_at": @date("Y-m-d H:i:s")
}

processed_xml: @xml.generate("processed_feed", $processed_feed)
```

## 🧠 Advanced @xml Patterns

### XPath Query Optimization
```ini
[xpath_optimization]
# Optimize XPath queries for performance
$large_xml: @xml.parse(@file.read("/var/data/large-dataset.xml"))

# Use efficient XPath queries
$optimized_queries: {
    "users_by_status": @xml.get_all($large_xml, "//user[@status='active']"),
    "recent_orders": @xml.get_all($large_xml, "//order[@date >= '2024-01-01']"),
    "high_value_customers": @xml.get_all($large_xml, "//customer[@total_spent > 1000]")
}

# Cache frequently accessed data
$cached_data: {
    "user_count": @array.length(@xml.get_all($large_xml, "//user")),
    "order_count": @array.length(@xml.get_all($large_xml, "//order")),
    "customer_count": @array.length(@xml.get_all($large_xml, "//customer"))
}
```

### XML Schema Validation
```ini
[xml_validation]
# Validate XML against schema
$xml_document: @file.read("/var/data/user-data.xml")
$schema_file: @file.read("/etc/schemas/user-schema.xsd")

# Validate XML structure
validation_result: @xml.validate($xml_document, $schema_file)

# Handle validation errors
@if($validation_result.valid, {
    "action": "process_xml",
    "data": @xml.parse($xml_document)
}, {
    "action": "handle_validation_errors",
    "errors": $validation_result.errors,
    "timestamp": @date("Y-m-d H:i:s")
})
```

### XML Transformation Pipeline
```ini
[xml_pipeline]
# Multi-step XML transformation pipeline
$source_xml: @xml.parse(@file.read("/var/data/source.xml"))

# Step 1: Extract and filter data
$filtered_data: @xml.transform($source_xml, "//item[@status='active']", {
    "template": "<active_item><id>{@id}</id><name>{name}</name><value>{value}</value></active_item>"
})

# Step 2: Enrich data
$enriched_data: @xml.transform($filtered_data, "//active_item", {
    "template": "<enriched_item><id>{id}</id><name>{name}</name><value>{value}</value><processed_at>{$date('Y-m-d H:i:s')}</processed_at></enriched_item>"
})

# Step 3: Aggregate data
$aggregated_data: @xml.transform($enriched_data, "//enriched_item", {
    "template": "<summary><total_items>{count(//enriched_item)}</total_items><total_value>{sum(//enriched_item/value)}</total_value></summary>"
})

# Final output
final_xml: @xml.generate("processed_data", {
    "pipeline_version": "1.0",
    "processed_at": @date("Y-m-d H:i:s"),
    "data": $aggregated_data
})
```

## 🛡️ Security & Performance Notes
- **XML injection:** Validate and sanitize XML input to prevent injection attacks
- **Memory usage:** Monitor memory consumption when processing large XML files
- **XPath injection:** Sanitize XPath expressions to prevent injection attacks
- **External entities:** Disable external entity processing for security
- **Schema validation:** Always validate XML against schemas when possible
- **Performance optimization:** Use efficient XPath queries and caching strategies

## 🐞 Troubleshooting
- **Parsing errors:** Check XML syntax and structure for validity
- **XPath failures:** Verify XPath expressions and XML structure
- **Memory issues:** Optimize XML processing for large files
- **Schema validation:** Ensure XML conforms to expected schema
- **Performance problems:** Use efficient XPath queries and caching

## 💡 Best Practices
- **Validate XML:** Always validate XML against schemas when possible
- **Use efficient XPath:** Optimize XPath queries for better performance
- **Handle errors:** Implement proper error handling for XML operations
- **Cache results:** Cache frequently accessed XML data
- **Sanitize input:** Validate and sanitize XML input data
- **Document schemas:** Document XML schemas and expected formats

## 🔗 Cross-References
- [@ Operator Introduction](024-at-operator-intro-bash.md)
- [@file Function](038-at-file-function-bash.md)
- [@http Function](037-at-http-function-bash.md)
- [Data Processing](094-data-processing-bash.md)
- [API Integration](077-api-integration-bash.md)

---

**Master @xml in TuskLang and structure your data with precision. 📄** 