# ✅ TuskLang Bash @validate Function Guide

**"We don't bow to any king" – Validation is your configuration's guardian.**

The @validate function in TuskLang is your data integrity and security guardian, enabling comprehensive validation of configuration values, user inputs, and system data directly within your configuration files. Whether you're validating API responses, ensuring data quality, or enforcing security policies, @validate provides the robust checking and error handling to keep your configurations reliable and secure.

## 🎯 What is @validate?
The @validate function provides comprehensive validation capabilities in TuskLang. It offers:
- **Type validation** - Check data types (string, number, boolean, array, object)
- **Format validation** - Validate emails, URLs, IPs, dates, and custom patterns
- **Range validation** - Check numeric ranges, string lengths, array sizes
- **Required field validation** - Ensure required values are present
- **Custom validation** - Define custom validation rules and functions

## 📝 Basic @validate Syntax

### Simple Validation
```ini
[simple_validation]
# Validate a single value
email_valid: @validate.email("user@example.com")
url_valid: @validate.url("https://example.com")
ip_valid: @validate.ip("192.168.1.1")
number_valid: @validate.number(42)
string_valid: @validate.string("hello")
```

### Required Field Validation
```ini
[required_validation]
# Validate required fields
required_fields: @validate.required(["api_key", "database_url", "port"])
api_key_present: @validate.required(@env("API_KEY"))
config_complete: @validate.required(["host", "port", "user", "password"])
```

### Range and Length Validation
```ini
[range_validation]
# Validate ranges and lengths
port_valid: @validate.range(@env("PORT"), 1, 65535)
age_valid: @validate.range(@env("USER_AGE"), 0, 120)
password_length: @validate.length(@env("PASSWORD"), 8, 128)
username_length: @validate.length(@env("USERNAME"), 3, 50)
```

## 🚀 Quick Start Example

```bash
#!/bin/bash
source tusk-bash.sh

cat > validate-quickstart.tsk << 'EOF'
[validation_demo]
# Basic validation examples
email: "test@example.com"
email_valid: @validate.email($email)

url: "https://tusklang.org"
url_valid: @validate.url($url)

port: @env("PORT", "8080")
port_valid: @validate.range($port, 1, 65535)

password: @env("PASSWORD", "mypassword123")
password_valid: @validate.length($password, 8, 128)

[required_validation]
# Check required fields
required_config: ["api_key", "database_url", "app_name"]
all_required: @validate.required($required_config)

api_key_present: @validate.required(@env("API_KEY"))
db_url_present: @validate.required(@env("DATABASE_URL"))

[complex_validation]
# Complex validation with multiple checks
user_data: {
    "email": "user@example.com",
    "age": 25,
    "username": "john_doe"
}

email_valid: @validate.email($user_data.email)
age_valid: @validate.range($user_data.age, 13, 120)
username_valid: @validate.pattern($user_data.username, "^[a-zA-Z0-9_]{3,20}$")

all_valid: @validate.all([$email_valid, $age_valid, $username_valid])
EOF

config=$(tusk_parse validate-quickstart.tsk)

echo "=== Validation Demo ==="
echo "Email Valid: $(tusk_get "$config" validation_demo.email_valid)"
echo "URL Valid: $(tusk_get "$config" validation_demo.url_valid)"
echo "Port Valid: $(tusk_get "$config" validation_demo.port_valid)"
echo "Password Valid: $(tusk_get "$config" validation_demo.password_valid)"

echo ""
echo "=== Required Validation ==="
echo "All Required: $(tusk_get "$config" required_validation.all_required)"
echo "API Key Present: $(tusk_get "$config" required_validation.api_key_present)"
echo "DB URL Present: $(tusk_get "$config" required_validation.db_url_present)"

echo ""
echo "=== Complex Validation ==="
echo "Email Valid: $(tusk_get "$config" complex_validation.email_valid)"
echo "Age Valid: $(tusk_get "$config" complex_validation.age_valid)"
echo "Username Valid: $(tusk_get "$config" complex_validation.username_valid)"
echo "All Valid: $(tusk_get "$config" complex_validation.all_valid)"
```

## 🔗 Real-World Use Cases

### 1. API Configuration Validation
```ini
[api_validation]
# Validate API configuration
$api_config: {
    "base_url": @env("API_BASE_URL"),
    "api_key": @env("API_KEY"),
    "timeout": @env("API_TIMEOUT", "30"),
    "retries": @env("API_RETRIES", "3")
}

# Validate each field
url_valid: @validate.url($api_config.base_url)
key_present: @validate.required($api_config.api_key)
timeout_valid: @validate.range($api_config.timeout, 1, 300)
retries_valid: @validate.range($api_config.retries, 0, 10)

# Overall validation
api_config_valid: @validate.all([$url_valid, $key_present, $timeout_valid, $retries_valid])

# Conditional configuration based on validation
final_api_config: @if($api_config_valid, $api_config, {
    "error": "Invalid API configuration",
    "missing_fields": @validate.missing_fields($api_config, ["base_url", "api_key", "timeout", "retries"])
})
```

### 2. Database Connection Validation
```ini
[database_validation]
# Validate database connection parameters
$db_config: {
    "host": @env("DB_HOST"),
    "port": @env("DB_PORT", "5432"),
    "database": @env("DB_NAME"),
    "username": @env("DB_USER"),
    "password": @env("DB_PASSWORD")
}

# Validate database configuration
host_valid: @validate.required($db_config.host)
port_valid: @validate.range($db_config.port, 1, 65535)
database_valid: @validate.required($db_config.database)
username_valid: @validate.required($db_config.username)
password_valid: @validate.required($db_config.password)

# Check if host is reachable
host_reachable: @validate.reachable($db_config.host, $db_config.port)

# Overall database validation
db_config_valid: @validate.all([$host_valid, $port_valid, $database_valid, $username_valid, $password_valid, $host_reachable])

# Connection string validation
connection_string: @string.format("postgresql://{user}:{password}@{host}:{port}/{database}", $db_config)
connection_valid: @validate.connection_string($connection_string)
```

### 3. User Input Validation
```ini
[user_input_validation]
# Validate user registration data
$user_input: {
    "email": @env("USER_EMAIL"),
    "username": @env("USER_USERNAME"),
    "password": @env("USER_PASSWORD"),
    "age": @env("USER_AGE"),
    "phone": @env("USER_PHONE")
}

# Email validation
email_valid: @validate.email($user_input.email)

# Username validation (alphanumeric, 3-20 chars)
username_valid: @validate.pattern($user_input.username, "^[a-zA-Z0-9_]{3,20}$")

# Password validation (8+ chars, at least one number and letter)
password_valid: @validate.pattern($user_input.password, "^(?=.*[A-Za-z])(?=.*\\d)[A-Za-z\\d@$!%*#?&]{8,}$")

# Age validation
age_valid: @validate.range($user_input.age, 13, 120)

# Phone validation
phone_valid: @validate.phone($user_input.phone)

# Overall user input validation
user_input_valid: @validate.all([$email_valid, $username_valid, $password_valid, $age_valid, $phone_valid])

# Validation errors
validation_errors: @validate.errors($user_input, {
    "email": "email",
    "username": "pattern:^[a-zA-Z0-9_]{3,20}$",
    "password": "pattern:^(?=.*[A-Za-z])(?=.*\\d)[A-Za-z\\d@$!%*#?&]{8,}$",
    "age": "range:13:120",
    "phone": "phone"
})
```

### 4. System Configuration Validation
```ini
[system_validation]
# Validate system configuration
$system_config: {
    "log_level": @env("LOG_LEVEL", "info"),
    "max_memory": @env("MAX_MEMORY", "1024"),
    "threads": @env("THREADS", "4"),
    "timeout": @env("TIMEOUT", "30")
}

# Validate log level
valid_log_levels: ["debug", "info", "warn", "error"]
log_level_valid: @validate.in($system_config.log_level, $valid_log_levels)

# Validate memory (MB)
memory_valid: @validate.range($system_config.max_memory, 64, 8192)

# Validate thread count
threads_valid: @validate.range($system_config.threads, 1, 32)

# Validate timeout
timeout_valid: @validate.range($system_config.timeout, 1, 300)

# Overall system validation
system_config_valid: @validate.all([$log_level_valid, $memory_valid, $threads_valid, $timeout_valid])

# System resource validation
available_memory: @shell("free -m | awk 'NR==2{print $7}'")
memory_sufficient: @validate.greater_than($available_memory, $system_config.max_memory)

cpu_cores: @shell("nproc")
threads_sufficient: @validate.greater_than_or_equal($cpu_cores, $system_config.threads)
```

## 🧠 Advanced @validate Patterns

### Custom Validation Functions
```ini
[custom_validation]
# Define custom validation rules
$custom_rules: {
    "strong_password": "^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[@$!%*?&])[A-Za-z\\d@$!%*?&]{8,}$",
    "valid_username": "^[a-zA-Z][a-zA-Z0-9_]{2,19}$",
    "valid_domain": "^[a-zA-Z0-9]([a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?\\.[a-zA-Z0-9]([a-zA-Z0-9-]{0,61}[a-zA-Z0-9])*$"
}

# Use custom validation
password: @env("PASSWORD")
password_strong: @validate.pattern($password, $custom_rules.strong_password)

username: @env("USERNAME")
username_valid: @validate.pattern($username, $custom_rules.valid_username)

domain: @env("DOMAIN")
domain_valid: @validate.pattern($domain, $custom_rules.valid_domain)
```

### Conditional Validation
```ini
[conditional_validation]
# Conditional validation based on environment
$environment: @env("APP_ENV", "development")

# Stricter validation in production
$validation_rules: @if($environment == "production", {
    "password_min_length": 12,
    "require_2fa": true,
    "strict_email": true
}, {
    "password_min_length": 8,
    "require_2fa": false,
    "strict_email": false
})

# Apply conditional validation
password: @env("PASSWORD")
password_valid: @validate.length($password, $validation_rules.password_min_length, 128)

email: @env("USER_EMAIL")
email_valid: @if($validation_rules.strict_email, 
    @validate.email($email) && @validate.pattern($email, "^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$"),
    @validate.email($email)
)
```

### Batch Validation with Error Reporting
```ini
[batch_validation]
# Validate multiple configurations at once
$configs: [
    {"name": "api", "url": @env("API_URL"), "key": @env("API_KEY")},
    {"name": "database", "host": @env("DB_HOST"), "port": @env("DB_PORT")},
    {"name": "redis", "host": @env("REDIS_HOST"), "port": @env("REDIS_PORT")}
]

# Validate each configuration
$validation_results: @array.map($configs, {
    "name": item.name,
    "valid": @validate.all([
        @validate.required(item.url || item.host),
        @validate.range(item.port, 1, 65535)
    ]),
    "errors": @validate.errors(item, {
        "url": "url",
        "host": "required",
        "port": "range:1:65535"
    })
})

# Overall validation status
all_configs_valid: @validate.all(@array.map($validation_results, item.valid))

# Failed configurations
failed_configs: @array.filter($validation_results, "!item.valid")
```

## 🛡️ Security & Performance Notes
- **Input sanitization:** Always sanitize inputs before validation to prevent injection
- **Validation order:** Validate required fields first, then format/range validation
- **Performance:** Cache validation results for repeated checks
- **Error handling:** Provide clear, actionable error messages
- **Security:** Validate all external inputs (env vars, user input, API responses)

## 🐞 Troubleshooting
- **Validation failures:** Check validation rules and input data types
- **Pattern mismatches:** Verify regex patterns and test with sample data
- **Performance issues:** Cache expensive validation operations
- **Missing validators:** Ensure all required validators are available
- **Type errors:** Check data types before validation

## 💡 Best Practices
- **Validate early:** Validate inputs as soon as they're received
- **Clear error messages:** Provide specific, actionable validation errors
- **Consistent validation:** Use consistent validation rules across your application
- **Test validation:** Test validation rules with various input types
- **Document rules:** Document validation rules and their purposes
- **Fail fast:** Stop processing when validation fails

## 🔗 Cross-References
- [@ Operator Introduction](024-at-operator-intro-bash.md)
- [@string Function](030-at-string-function-bash.md)
- [Error Handling](062-error-handling-bash.md)
- [Security Best Practices](099-security-best-practices-bash.md)

---

**Master @validate in TuskLang and ensure your configurations are robust and secure. ✅** 