# 📝 TuskLang Bash @string Function Guide

**"We don't bow to any king" – Strings are your configuration's voice.**

The @string function in TuskLang is your text manipulation powerhouse, enabling dynamic string operations, formatting, and transformation directly within your configuration files. Whether you're building dynamic URLs, formatting messages, or processing text data, @string provides the flexibility and power to make your configurations communicate effectively.

## 🎯 What is @string?
The @string function performs string operations and manipulations in TuskLang. It provides:
- **String concatenation** - Combine multiple strings
- **String formatting** - Format strings with variables
- **String transformation** - Case changes, trimming, replacement
- **String validation** - Check string properties and patterns
- **Dynamic text** - Create dynamic text based on variables

## 📝 Basic @string Syntax

### String Concatenation
```ini
[concatenation]
# Basic string concatenation
full_name: @string.concat("John", " ", "Doe")
url: @string.concat("https://", "api.example.com", "/v1")
file_path: @string.concat("/var/log/", "app-", @date("Y-m-d"), ".log")
```

### String Formatting
```ini
[formatting]
# String formatting with variables
$user_name: "Alice"
$user_id: 12345
$timestamp: @date("Y-m-d H:i:s")

formatted_message: @string.format("User {name} (ID: {id}) logged in at {time}", {
    "name": $user_name,
    "id": $user_id,
    "time": $timestamp
})
```

### String Transformation
```ini
[transformation]
# String case operations
original: "Hello World"
uppercase: @string.upper($original)
lowercase: @string.lower($original)
title_case: @string.title($original)

# String trimming and cleaning
dirty_string: "  Hello World  "
trimmed: @string.trim($dirty_string)
cleaned: @string.clean($dirty_string)
```

## 🚀 Quick Start Example

```bash
#!/bin/bash
source tusk-bash.sh

cat > string-quickstart.tsk << 'EOF'
[basic_strings]
# Basic string operations
greeting: @string.concat("Hello", ", ", "World", "!")
file_name: @string.concat("config-", @date("Y-m-d"), ".tsk")
api_endpoint: @string.concat("https://api.example.com/v1/users")

[string_formatting]
# Format strings with variables
$app_name: "TuskApp"
$version: "2.1.0"
$environment: @env("APP_ENV", "development")

app_title: @string.format("{name} v{version} ({env})", {
    "name": $app_name,
    "version": $version,
    "env": $environment
})

[string_transformation]
# Transform strings
original_text: "  Hello World  "
uppercase_text: @string.upper($original_text)
lowercase_text: @string.lower($original_text)
trimmed_text: @string.trim($original_text)
title_case_text: @string.title($original_text)

[url_building]
# Build dynamic URLs
$base_url: "https://api.example.com"
$api_version: "v1"
$endpoint: "users"

full_url: @string.concat($base_url, "/api/", $api_version, "/", $endpoint)
health_check_url: @string.concat($base_url, "/health")
EOF

config=$(tusk_parse string-quickstart.tsk)

echo "=== Basic Strings ==="
echo "Greeting: $(tusk_get "$config" basic_strings.greeting)"
echo "File Name: $(tusk_get "$config" basic_strings.file_name)"
echo "API Endpoint: $(tusk_get "$config" basic_strings.api_endpoint)"

echo ""
echo "=== String Formatting ==="
echo "App Title: $(tusk_get "$config" string_formatting.app_title)"

echo ""
echo "=== String Transformation ==="
echo "Original: '$(tusk_get "$config" string_transformation.original_text)'"
echo "Uppercase: $(tusk_get "$config" string_transformation.uppercase_text)"
echo "Lowercase: $(tusk_get "$config" string_transformation.lowercase_text)"
echo "Trimmed: '$(tusk_get "$config" string_transformation.trimmed_text)'"
echo "Title Case: $(tusk_get "$config" string_transformation.title_case_text)"

echo ""
echo "=== URL Building ==="
echo "Full URL: $(tusk_get "$config" url_building.full_url)"
echo "Health Check URL: $(tusk_get "$config" url_building.health_check_url)"
```

## 🔗 Real-World Use Cases

### 1. Dynamic URL and API Endpoint Building
```ini
[api_configuration]
# Dynamic API configuration
$base_url: @env("API_BASE_URL", "https://api.example.com")
$api_version: @env("API_VERSION", "v1")
$api_key: @env("API_KEY")

# Build API endpoints
users_endpoint: @string.concat($base_url, "/api/", $api_version, "/users")
posts_endpoint: @string.concat($base_url, "/api/", $api_version, "/posts")
health_endpoint: @string.concat($base_url, "/health")

# Authentication headers
auth_header: @string.format("Bearer {key}", {"key": $api_key})
content_type_header: "application/json"

# Query string building
$user_id: @env("USER_ID", "123")
user_query: @string.format("?user_id={id}&include=profile", {"id": $user_id})
full_user_url: @string.concat($users_endpoint, "/", $user_id)
```

### 2. File Path and Log Management
```ini
[file_management]
# Dynamic file path construction
$app_name: @env("APP_NAME", "tuskapp")
$log_directory: @env("LOG_DIR", "/var/log")
$config_directory: @env("CONFIG_DIR", "/etc")

# Log file paths with timestamps
current_date: @date("Y-m-d")
current_month: @date("Y-m")
current_year: @date("Y")

daily_log: @string.concat($log_directory, "/", $app_name, "-", $current_date, ".log")
monthly_log: @string.concat($log_directory, "/", $app_name, "-", $current_month, ".log")
yearly_log: @string.concat($log_directory, "/", $app_name, "-", $current_year, ".log")

# Configuration file paths
main_config: @string.concat($config_directory, "/", $app_name, "/config.tsk")
env_config: @string.concat($config_directory, "/", $app_name, "/", @env("APP_ENV"), ".tsk")

# Backup file naming
backup_timestamp: @date("Y-m-d-H-i-s")
backup_file: @string.concat("/var/backups/", $app_name, "-", $backup_timestamp, ".tar.gz")
```

### 3. Email and Notification Templates
```ini
[notification_templates]
# Dynamic email templates
$user_name: @env("USER_NAME", "User")
$app_name: @env("APP_NAME", "TuskApp")
$support_email: @env("SUPPORT_EMAIL", "support@example.com")

# Welcome email template
welcome_subject: @string.format("Welcome to {app}, {name}!", {
    "app": $app_name,
    "name": $user_name
})

welcome_body: @string.format("""
Dear {name},

Welcome to {app}! Your account has been successfully created.

If you have any questions, please contact us at {support}.

Best regards,
The {app} Team
""", {
    "name": $user_name,
    "app": $app_name,
    "support": $support_email
})

# Error notification template
$error_type: "Database Connection"
$error_message: "Connection timeout"
$timestamp: @date("Y-m-d H:i:s")

error_subject: @string.format("[{app}] {type} Error", {
    "app": $app_name,
    "type": $error_type
})

error_body: @string.format("""
Error Type: {type}
Error Message: {message}
Timestamp: {time}
Environment: {env}

Please investigate this issue immediately.
""", {
    "type": $error_type,
    "message": $error_message,
    "time": $timestamp,
    "env": @env("APP_ENV")
})
```

### 4. Database Query Building
```ini
[database_queries]
# Dynamic SQL query construction
$table_name: "users"
$user_id: @env("USER_ID", "123")
$limit: @env("QUERY_LIMIT", "100")

# Basic query building
select_users: @string.format("SELECT * FROM {table} LIMIT {limit}", {
    "table": $table_name,
    "limit": $limit
})

select_user_by_id: @string.format("SELECT * FROM {table} WHERE id = {id}", {
    "table": $table_name,
    "id": $user_id
})

# Conditional query building
$include_profile: @env("INCLUDE_PROFILE", "true")
$include_settings: @env("INCLUDE_SETTINGS", "false")

# Build dynamic SELECT clause
select_fields: @if($include_profile == "true" && $include_settings == "true", 
    "id, username, email, profile_data, settings_data",
    @if($include_profile == "true", 
        "id, username, email, profile_data",
        @if($include_settings == "true",
            "id, username, email, settings_data",
            "id, username, email"
        )
    )
)

dynamic_query: @string.format("SELECT {fields} FROM {table} WHERE id = {id}", {
    "fields": $select_fields,
    "table": $table_name,
    "id": $user_id
})
```

## 🧠 Advanced @string Patterns

### Template Engine Simulation
```bash
#!/bin/bash
source tusk-bash.sh

cat > template-engine.tsk << 'EOF'
[template_engine]
# Simple template engine using @string
$template: """
Hello {name},

Your account balance is ${balance}.
Your last login was on {last_login}.

Thank you for using {app_name}!

Best regards,
{app_name} Team
"""

$user_data: {
    "name": "Alice Johnson",
    "balance": "1,234.56",
    "last_login": @date("F j, Y"),
    "app_name": "TuskApp"
}

# Process template with data
processed_template: @string.format($template, $user_data)

# Alternative with direct variable substitution
direct_template: @string.format("""
Hello {name},

Your account balance is ${balance}.
Your last login was on {last_login}.

Thank you for using {app_name}!

Best regards,
{app_name} Team
""", $user_data)
EOF

config=$(tusk_parse template-engine.tsk)
echo "Processed Template:"
echo "$(tusk_get "$config" template_engine.processed_template)"
```

### String Validation and Sanitization
```ini
[string_validation]
# String validation and sanitization
$raw_input: @env("USER_INPUT", "Hello World 123!")

# Validation checks
is_empty: @string.empty($raw_input)
length: @string.length($raw_input)
contains_numbers: @string.contains($raw_input, "123")
starts_with: @string.starts_with($raw_input, "Hello")
ends_with: @string.ends_with($raw_input, "!")

# Sanitization
sanitized: @string.sanitize($raw_input)
alphanumeric_only: @string.replace($raw_input, "[^a-zA-Z0-9]", "")
lowercase_sanitized: @string.lower(@string.sanitize($raw_input))

# Pattern matching
is_email: @string.matches($raw_input, "^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$")
is_url: @string.matches($raw_input, "^https?://[^\s/$.?#].[^\s]*$")
is_phone: @string.matches($raw_input, "^\+?[\d\s\-\(\)]{10,}$")
```

### Internationalization Support
```ini
[internationalization]
# Multi-language string support
$language: @env("LANGUAGE", "en")
$user_name: @env("USER_NAME", "User")

# Language-specific greetings
greetings: @if($language == "es", 
    @string.format("¡Hola {name}!", {"name": $user_name}),
    @if($language == "fr",
        @string.format("Bonjour {name}!", {"name": $user_name}),
        @string.format("Hello {name}!", {"name": $user_name})
    )
)

# Language-specific date formats
$current_date: @date("Y-m-d")
formatted_date: @if($language == "es",
    @string.format("{date} (formato español)", {"date": $current_date}),
    @if($language == "fr",
        @string.format("{date} (format français)", {"date": $current_date}),
        @string.format("{date} (English format)", {"date": $current_date})
    )
)

# Language-specific error messages
$error_code: "404"
error_message: @if($language == "es",
    @string.format("Error {code}: Página no encontrada", {"code": $error_code}),
    @if($language == "fr",
        @string.format("Erreur {code}: Page non trouvée", {"code": $error_code}),
        @string.format("Error {code}: Page not found", {"code": $error_code})
    )
)
```

## 🛡️ Security & Performance Notes
- **Input validation:** Always validate string inputs to prevent injection attacks
- **String escaping:** Properly escape special characters in dynamic content
- **Memory usage:** Be careful with large string operations that could consume memory
- **Performance:** Cache expensive string operations to improve performance
- **Encoding:** Ensure proper character encoding for international content

## 🐞 Troubleshooting
- **Encoding issues:** Check character encoding for international strings
- **Memory problems:** Monitor memory usage with large string operations
- **Format errors:** Validate format strings and variable placeholders
- **Performance issues:** Cache expensive string operations
- **Special characters:** Handle special characters and escape sequences properly

## 💡 Best Practices
- **Validate inputs:** Always validate string inputs before processing
- **Use appropriate encoding:** Use UTF-8 for international content
- **Cache expensive operations:** Cache complex string operations
- **Escape special characters:** Properly escape special characters in dynamic content
- **Document templates:** Document string templates and expected variables
- **Test thoroughly:** Test string operations with various inputs and edge cases

## 🔗 Cross-References
- [@ Operator Introduction](024-at-operator-intro-bash.md)
- [Strings](008-strings-bash.md)
- [Multiline Values](017-multiline-values-bash.md)
- [Templates](035-at-render-function-bash.md)

---

**Master @string in TuskLang and give your configurations the power of dynamic text. 📝** 