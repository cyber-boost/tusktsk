# 📝 TuskLang Bash Strings Guide

**"We don't bow to any king" - Master string manipulation in TuskLang**

Strings are the most fundamental data type in TuskLang, used for everything from simple text values to complex configuration data. TuskLang provides powerful string capabilities including interpolation, multiline support, and dynamic string operations.

## 🎯 String Types

### Basic Strings

```bash
#!/bin/bash
source tusk-bash.sh

cat > basic-strings.tsk << 'EOF'
[strings]
# Simple quoted strings
simple: "Hello, World!"
single_quoted: 'Single quoted string'
unquoted: This is also a string

# Strings with special characters
path: "/var/log/app.log"
url: "https://api.example.com/v1"
email: "user@example.com"

# Empty strings
empty: ""
null_string: null
EOF

config=$(tusk_parse basic-strings.tsk)
echo "=== Basic Strings ==="
echo "Simple: $(tusk_get "$config" strings.simple)"
echo "Path: $(tusk_get "$config" strings.path)"
echo "URL: $(tusk_get "$config" strings.url)"
echo "Empty: '$(tusk_get "$config" strings.empty)'"
```

### String Interpolation

```bash
#!/bin/bash
source tusk-bash.sh

cat > string-interpolation.tsk << 'EOF'
[interpolation]
# Basic interpolation
$name: "Alice"
$age: 30
message: "Hello, ${name}! You are ${age} years old."

# Complex interpolation
$protocol: "https"
$domain: "example.com"
$path: "/api"
url: "${protocol}://${domain}${path}"

# Nested interpolation
$base_url: "${protocol}://${domain}"
$api_url: "${base_url}${path}"
full_url: "${api_url}/users"

# Conditional interpolation
$environment: @env("APP_ENV", "development")
$port: @if($environment == "production", 443, 3000)
server_url: "${protocol}://${domain}:${port}"
EOF

config=$(tusk_parse string-interpolation.tsk)
echo "=== String Interpolation ==="
echo "Message: $(tusk_get "$config" interpolation.message)"
echo "Full URL: $(tusk_get "$config" interpolation.full_url)"
echo "Server URL: $(tusk_get "$config" interpolation.server_url)"
```

### Multiline Strings

```bash
#!/bin/bash
source tusk-bash.sh

cat > multiline-strings.tsk << 'EOF'
[multiline]
# Basic multiline string
basic_multiline: """
This is a multiline
string that spans
multiple lines
"""

# Multiline with interpolation
$app_name: "TuskApp"
$version: "2.1.0"
welcome_message: """
Welcome to ${app_name} v${version}!

This is a powerful configuration language
that adapts to your preferred syntax.

Features:
- Dynamic configuration
- Database integration
- @ operator system
"""

# Multiline with special formatting
html_template: """
<!DOCTYPE html>
<html>
<head>
    <title>${app_name}</title>
</head>
<body>
    <h1>Welcome to ${app_name}</h1>
    <p>Version: ${version}</p>
</body>
</html>
"""
EOF

config=$(tusk_parse multiline-strings.tsk)
echo "=== Multiline Strings ==="
echo "Basic: $(tusk_get "$config" multiline.basic_multiline)"
echo "Welcome: $(tusk_get "$config" multiline.welcome_message)"
echo "HTML: $(tusk_get "$config" multiline.html_template)"
```

## 🔧 String Operations

### String Concatenation

```bash
#!/bin/bash
source tusk-bash.sh

cat > string-concatenation.tsk << 'EOF'
[concatenation]
# Basic concatenation
$first: "Hello"
$second: "World"
concatenated: "${first}, ${second}!"

# Path concatenation
$base_path: "/var/www"
$app_name: "tuskapp"
$log_dir: "logs"
full_path: "${base_path}/${app_name}/${log_dir}"

# URL construction
$protocol: "https"
$host: "api.example.com"
$version: "v1"
$endpoint: "users"
api_url: "${protocol}://${host}/api/${version}/${endpoint}"

# File path construction
$config_dir: "/etc"
$app_name: "tuskapp"
$config_file: "config.tsk"
config_path: "${config_dir}/${app_name}/${config_file}"
EOF

config=$(tusk_parse string-concatenation.tsk)
echo "=== String Concatenation ==="
echo "Concatenated: $(tusk_get "$config" concatenation.concatenated)"
echo "Full Path: $(tusk_get "$config" concatenation.full_path)"
echo "API URL: $(tusk_get "$config" concatenation.api_url)"
echo "Config Path: $(tusk_get "$config" concatenation.config_path)"
```

### String Functions

```bash
#!/bin/bash
source tusk-bash.sh

cat > string-functions.tsk << 'EOF'
[functions]
# String length
$text: "Hello, TuskLang!"
text_length: @string.length($text)

# String case conversion
$mixed_case: "Hello World"
uppercase: @string.upper($mixed_case)
lowercase: @string.lower($mixed_case)
title_case: @string.title($mixed_case)

# String trimming
$padded: "   Hello World   "
trimmed: @string.trim($padded)
left_trimmed: @string.ltrim($padded)
right_trimmed: @string.rtrim($padded)

# String replacement
$original: "Hello World"
replaced: @string.replace($original, "World", "TuskLang")

# String splitting
$csv_line: "apple,banana,cherry"
split_result: @string.split($csv_line, ",")

# String joining
$words: ["Hello", "TuskLang", "World"]
joined: @string.join($words, " ")
EOF

config=$(tusk_parse string-functions.tsk)
echo "=== String Functions ==="
echo "Length: $(tusk_get "$config" functions.text_length)"
echo "Uppercase: $(tusk_get "$config" functions.uppercase)"
echo "Lowercase: $(tusk_get "$config" functions.lowercase)"
echo "Trimmed: '$(tusk_get "$config" functions.trimmed)'"
echo "Replaced: $(tusk_get "$config" functions.replaced)"
echo "Joined: $(tusk_get "$config" functions.joined)"
```

## 🔍 String Validation

### Pattern Matching

```bash
#!/bin/bash
source tusk-bash.sh

cat > string-validation.tsk << 'EOF'
[validation]
# Email validation
$email: @env("USER_EMAIL", "user@example.com")
is_valid_email: @string.match($email, "^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$")

# URL validation
$url: @env("API_URL", "https://api.example.com")
is_valid_url: @string.match($url, "^https?://[^\\s/$.?#].[^\\s]*$")

# Phone number validation
$phone: @env("PHONE", "+1-555-123-4567")
is_valid_phone: @string.match($phone, "^\\+?[1-9]\\d{1,14}$")

# IP address validation
$ip: @env("SERVER_IP", "192.168.1.1")
is_valid_ip: @string.match($ip, "^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$")

# Custom validation
$username: @env("USERNAME", "alice123")
is_valid_username: @string.match($username, "^[a-zA-Z0-9_]{3,20}$")
EOF

config=$(tusk_parse string-validation.tsk)
echo "=== String Validation ==="
echo "Valid Email: $(tusk_get "$config" validation.is_valid_email)"
echo "Valid URL: $(tusk_get "$config" validation.is_valid_url)"
echo "Valid Phone: $(tusk_get "$config" validation.is_valid_phone)"
echo "Valid IP: $(tusk_get "$config" validation.is_valid_ip)"
echo "Valid Username: $(tusk_get "$config" validation.is_valid_username)"
```

## 🔄 Dynamic String Generation

### Environment-Based Strings

```bash
#!/bin/bash
source tusk-bash.sh

cat > dynamic-strings.tsk << 'EOF'
[dynamic]
# Environment-based configuration
$environment: @env("APP_ENV", "development")
$app_name: @env("APP_NAME", "TuskApp")

# Dynamic log file path
log_file: @if($environment == "production", 
    "/var/log/${app_name}.log", 
    "./logs/${app_name}.log")

# Dynamic database URL
$db_host: @env("DB_HOST", "localhost")
$db_port: @env("DB_PORT", "5432")
$db_name: @env("DB_NAME", "tuskapp")
database_url: "postgresql://${db_host}:${db_port}/${db_name}"

# Dynamic API endpoint
$api_version: @env("API_VERSION", "v1")
$api_base: @env("API_BASE", "https://api.example.com")
api_endpoint: "${api_base}/api/${api_version}"

# Dynamic configuration file path
config_file: @if($environment == "production",
    "/etc/${app_name}/config.tsk",
    "./config/${environment}.tsk")
EOF

config=$(tusk_parse dynamic-strings.tsk)
echo "=== Dynamic Strings ==="
echo "Log File: $(tusk_get "$config" dynamic.log_file)"
echo "Database URL: $(tusk_get "$config" dynamic.database_url)"
echo "API Endpoint: $(tusk_get "$config" dynamic.api_endpoint)"
echo "Config File: $(tusk_get "$config" dynamic.config_file)"
```

### System Information Strings

```bash
#!/bin/bash
source tusk-bash.sh

cat > system-strings.tsk << 'EOF'
[system]
# System information strings
$hostname: @shell("hostname")
$current_user: @env("USER")
$current_time: @date.now()
$system_load: @shell("uptime | awk -F'load average:' '{print $2}' | awk '{print $1}'")

# System status message
status_message: "System ${hostname} is running with load ${system_load} at ${current_time}"

# User-specific paths
$home_dir: @env("HOME")
user_config_path: "${home_dir}/.config/tuskapp/config.tsk"
user_log_path: "${home_dir}/.local/share/tuskapp/logs"

# Process information
$process_id: @shell("echo $$")
process_info: "Process ${process_id} running as ${current_user}"

# Network information
$local_ip: @shell("hostname -I | awk '{print $1}'")
network_info: "Local IP: ${local_ip}, Hostname: ${hostname}"
EOF

config=$(tusk_parse system-strings.tsk)
echo "=== System Strings ==="
echo "Status: $(tusk_get "$config" system.status_message)"
echo "User Config: $(tusk_get "$config" system.user_config_path)"
echo "Process Info: $(tusk_get "$config" system.process_info)"
echo "Network Info: $(tusk_get "$config" system.network_info)"
```

## 🎯 What You've Learned

In this strings guide, you've mastered:

✅ **String types** - Basic, interpolated, and multiline strings  
✅ **String operations** - Concatenation, functions, and manipulation  
✅ **String validation** - Pattern matching and validation  
✅ **Dynamic strings** - Environment-based and system-generated strings  
✅ **Best practices** - String handling and formatting  

## 🚀 Next Steps

Ready to explore more TuskLang features?

1. **Numbers** → [009-numbers-bash.md](009-numbers-bash.md)
2. **Booleans** → [010-booleans-bash.md](010-booleans-bash.md)
3. **Arrays** → [012-arrays-bash.md](012-arrays-bash.md)

## 💡 Pro Tips

- **Use interpolation** - Leverage `${variable}` syntax for dynamic strings
- **Validate inputs** - Use pattern matching for data validation
- **Escape properly** - Use appropriate escaping for special characters
- **Use multiline strings** - For complex text content and templates
- **Keep it readable** - Use descriptive variable names in interpolations

---

**Master string manipulation in TuskLang! 📝** 