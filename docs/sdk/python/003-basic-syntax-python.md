# 📝 TuskLang Basic Syntax for Python

**"We don't bow to any king" - Master the Syntax**

TuskLang's revolutionary syntax gives you the flexibility to write configuration in your preferred style while maintaining the power of executable configuration with a heartbeat.

## 🎨 Multiple Syntax Styles

TuskLang supports three distinct syntax styles. Choose the one that feels natural to you:

### 1. Traditional INI Style (Default)

```python
from tsk import TSK

config = TSK.from_string("""
# Global variables
$app_name: "MyApp"
$version: "1.0.0"

[server]
host: "0.0.0.0"
port: 8080
debug: true

[database]
host: "localhost"
port: 5432
name: "myapp"
user: "postgres"
password: @env("DB_PASSWORD")

[api]
endpoint: "https://api.example.com"
timeout: 30
retries: 3
headers: {
    "Content-Type": "application/json",
    "Authorization": "Bearer @env('API_KEY')"
}
""")
```

### 2. JSON-Like Style

```python
config = TSK.from_string("""
{
    "app_name": "MyApp",
    "version": "1.0.0",
    "server": {
        "host": "0.0.0.0",
        "port": 8080,
        "debug": true
    },
    "database": {
        "host": "localhost",
        "port": 5432,
        "name": "myapp",
        "user": "postgres",
        "password": "@env('DB_PASSWORD')"
    },
    "api": {
        "endpoint": "https://api.example.com",
        "timeout": 30,
        "retries": 3
    }
}
""")
```

### 3. XML-Inspired Style

```python
config = TSK.from_string("""
<app>
    <app_name>MyApp</app_name>
    <version>1.0.0</version>
    <server>
        <host>0.0.0.0</host>
        <port>8080</port>
        <debug>true</debug>
    </server>
    <database>
        <host>localhost</host>
        <port>5432</port>
        <name>myapp</name>
        <user>postgres</user>
        <password>@env('DB_PASSWORD')</password>
    </database>
    <api>
        <endpoint>https://api.example.com</endpoint>
        <timeout>30</timeout>
        <retries>3</retries>
    </api>
</app>
""")
```

## 📊 Data Types

TuskLang supports all Python data types with automatic type inference:

### Strings

```python
config = TSK.from_string("""
[strings]
simple: "Hello, World!"
quoted: 'Single quotes work too'
multiline: """
    This is a multiline
    string that spans
    multiple lines
"""
escaped: "Line 1\nLine 2\tTabbed"
unicode: "Hello, 世界!"
""")
```

### Numbers

```python
config = TSK.from_string("""
[numbers]
integer: 42
float: 3.14159
negative: -10
scientific: 1.23e-4
hex: 0xFF
binary: 0b1010
octal: 0o755
""")
```

### Booleans

```python
config = TSK.from_string("""
[booleans]
true_value: true
false_value: false
yes_value: yes
no_value: no
on_value: on
off_value: off
""")
```

### Null Values

```python
config = TSK.from_string("""
[nulls]
null_value: null
none_value: none
empty_string: ""
undefined: undefined
""")
```

### Arrays/Lists

```python
config = TSK.from_string("""
[arrays]
simple: [1, 2, 3, 4, 5]
mixed: ["hello", 42, true, null]
nested: [[1, 2], [3, 4], [5, 6]]
strings: ["apple", "banana", "cherry"]
numbers: [1.1, 2.2, 3.3, 4.4]
booleans: [true, false, true]
""")
```

### Objects/Dictionaries

```python
config = TSK.from_string("""
[objects]
simple: {
    "name": "John",
    "age": 30,
    "active": true
}
nested: {
    "user": {
        "id": 123,
        "profile": {
            "name": "Alice",
            "email": "alice@example.com"
        }
    }
}
mixed: {
    "string": "value",
    "number": 42,
    "boolean": true,
    "array": [1, 2, 3],
    "object": {
        "nested": "value"
    }
}
""")
```

## 🔗 Global Variables

Global variables (prefixed with `$`) are accessible throughout the configuration:

```python
config = TSK.from_string("""
# Global variables
$app_name: "MyApp"
$version: "1.0.0"
$environment: @env("APP_ENV", "development")

[server]
host: "0.0.0.0"
port: @if($environment == "production", 80, 8080)
debug: @if($environment != "production", true, false)

[paths]
log_file: "/var/log/${app_name}.log"
config_file: "/etc/${app_name}/config.json"
data_dir: "/var/lib/${app_name}/v${version}"
""")

result = config.parse()
print(f"Log file: {result['paths']['log_file']}")
# Output: Log file: /var/log/MyApp.log
```

## 📁 Sections and Nesting

### Basic Sections

```python
config = TSK.from_string("""
[server]
host: "0.0.0.0"
port: 8080

[database]
host: "localhost"
port: 5432

[api]
endpoint: "https://api.example.com"
""")
```

### Nested Sections

```python
config = TSK.from_string("""
[server]
host: "0.0.0.0"
port: 8080

[server.security]
ssl: true
cert_file: "/etc/ssl/cert.pem"
key_file: "/etc/ssl/key.pem"

[server.logging]
level: "info"
file: "/var/log/app.log"
max_size: "100MB"
""")
```

### Deep Nesting

```python
config = TSK.from_string("""
[app]
name: "MyApp"

[app.server]
host: "0.0.0.0"
port: 8080

[app.server.security]
ssl: true
cert_file: "/etc/ssl/cert.pem"

[app.server.security.headers]
X-Frame-Options: "DENY"
X-Content-Type-Options: "nosniff"
Content-Security-Policy: "default-src 'self'"
""")
```

## 💬 Comments

TuskLang supports multiple comment styles:

```python
config = TSK.from_string("""
# This is a single-line comment
$app_name: "MyApp"  # Inline comment

[server]
# Server configuration
host: "0.0.0.0"     # Listen on all interfaces
port: 8080          # Default port

[database]
# Database settings
host: "localhost"   # Local database
port: 5432          # PostgreSQL default

/*
This is a multi-line comment
that spans multiple lines
and can contain any text
*/

[api]
endpoint: "https://api.example.com"  # API endpoint
""")
```

## 🔄 String Interpolation

TuskLang supports powerful string interpolation:

```python
config = TSK.from_string("""
$app_name: "MyApp"
$version: "1.0.0"
$environment: "production"

[paths]
log_file: "/var/log/${app_name}.log"
config_file: "/etc/${app_name}/config.json"
data_dir: "/var/lib/${app_name}/v${version}"

[urls]
api_url: "https://api.${app_name}.com"
docs_url: "https://docs.${app_name}.com"
status_url: "https://status.${app_name}.com"

[files]
backup_file: "/backups/${app_name}_${environment}_${version}.tar.gz"
""")

result = config.parse()
print(f"Log file: {result['paths']['log_file']}")
print(f"Backup file: {result['files']['backup_file']}")
# Output:
# Log file: /var/log/MyApp.log
# Backup file: /backups/MyApp_production_1.0.0.tar.gz
```

## ⚡ @ Operator Integration

@ operators work seamlessly with all syntax styles:

```python
config = TSK.from_string("""
[environment]
current_env: @env("APP_ENV", "development")
debug_mode: @env("DEBUG", "false")

[timestamps]
current_time: @date.now()
formatted_date: @date("Y-m-d H:i:s")
yesterday: @date.subtract("1d")

[files]
config_content: @file.read("config.json")
file_exists: @file.exists("important.txt")

[external]
weather: @http("GET", "https://api.weatherapi.com/v1/current.json?key=YOUR_KEY&q=London")
""")
```

## 🔗 Cross-File References

Reference values from other TSK files:

```python
# main.tsk
main_config = TSK.from_string("""
$app_name: "MyApp"

[database]
host: @config.tsk.get("db_host")
port: @config.tsk.get("db_port")
name: @config.tsk.get("db_name")
""")

# config.tsk
db_config = TSK.from_string("""
db_host: "localhost"
db_port: 5432
db_name: "myapp"
db_user: "postgres"
db_password: @env("DB_PASSWORD")
""")

# Link files
main_config.link_file('config.tsk', db_config)
result = main_config.parse()
```

## 🎯 Conditional Logic

Use conditional expressions for dynamic configuration:

```python
config = TSK.from_string("""
$environment: @env("APP_ENV", "development")

[server]
host: "0.0.0.0"
port: @if($environment == "production", 80, 8080)
workers: @if($environment == "production", 4, 1)
debug: @if($environment != "production", true, false)

[logging]
level: @if($environment == "production", "error", "debug")
format: @if($environment == "production", "json", "text")
file: @if($environment == "production", "/var/log/app.log", "app.log")

[security]
ssl: @if($environment == "production", true, false)
cert_file: @if($environment == "production", "/etc/ssl/cert.pem", null)
""")
```

## 🔧 Type Annotations

TuskLang supports type annotations for better IDE support:

```python
config = TSK.from_string("""
[types]
string_value: "hello" # str
integer_value: 42 # int
float_value: 3.14 # float
boolean_value: true # bool
array_value: [1, 2, 3] # list
object_value: {"key": "value"} # dict
null_value: null # None
""")
```

## 🚀 Advanced Syntax Features

### Multiline Values

```python
config = TSK.from_string("""
[multiline]
sql_query: """
    SELECT u.name, u.email, p.title
    FROM users u
    JOIN posts p ON u.id = p.user_id
    WHERE u.active = true
    ORDER BY p.created_at DESC
"""

python_code: """
    def process_data(data):
        result = []
        for item in data:
            if item.get('active'):
                result.append({
                    'id': item['id'],
                    'name': item['name'].upper()
                })
        return result
"""

json_data: """
    {
        "users": [
            {"id": 1, "name": "Alice"},
            {"id": 2, "name": "Bob"}
        ],
        "total": 2
    }
"""
""")
```

### Array and Object Operations

```python
config = TSK.from_string("""
[arrays]
numbers: [1, 2, 3, 4, 5]
first: @arrays.numbers[0]
last: @arrays.numbers[-1]
slice: @arrays.numbers[1:3]
length: @arrays.numbers.length()

[objects]
user: {
    "id": 123,
    "name": "Alice",
    "email": "alice@example.com",
    "roles": ["admin", "user"]
}
user_id: @objects.user.id
user_name: @objects.user.name
user_roles: @objects.user.roles
is_admin: @objects.user.roles.includes("admin")
""")
```

## 🛠️ Syntax Validation

Validate your TSK syntax:

```python
from tsk import TSK

# Valid syntax
try:
    config = TSK.from_string("""
    $app_name: "MyApp"
    [server]
    host: "0.0.0.0"
    port: 8080
    """)
    print("Syntax is valid!")
except Exception as e:
    print(f"Syntax error: {e}")

# Invalid syntax
try:
    config = TSK.from_string("""
    $app_name: "MyApp"
    [server
    host: "0.0.0.0"  # Missing closing bracket
    """)
except Exception as e:
    print(f"Syntax error: {e}")
```

## 📋 Best Practices

### 1. Use Descriptive Names

```python
# Good
config = TSK.from_string("""
$application_name: "MyWebApplication"
$application_version: "2.1.0"

[web_server]
host_address: "0.0.0.0"
port_number: 8080
debug_mode: true
""")

# Avoid
config = TSK.from_string("""
$a: "MyApp"
$v: "2.1.0"

[s]
h: "0.0.0.0"
p: 8080
d: true
""")
```

### 2. Group Related Settings

```python
config = TSK.from_string("""
[server]
host: "0.0.0.0"
port: 8080

[server.security]
ssl: true
cert_file: "/etc/ssl/cert.pem"

[server.logging]
level: "info"
file: "/var/log/app.log"

[database]
host: "localhost"
port: 5432
name: "myapp"
""")
```

### 3. Use Global Variables for Common Values

```python
config = TSK.from_string("""
$app_name: "MyApp"
$environment: @env("APP_ENV", "development")

[paths]
log_file: "/var/log/${app_name}.log"
config_file: "/etc/${app_name}/config.json"
data_dir: "/var/lib/${app_name}"

[urls]
api_url: "https://api.${app_name}.com"
docs_url: "https://docs.${app_name}.com"
""")
```

### 4. Comment Complex Logic

```python
config = TSK.from_string("""
# Application configuration
$app_name: "MyApp"
$version: "1.0.0"

[server]
# Server configuration
host: "0.0.0.0"  # Listen on all interfaces
port: 8080       # Default development port

[database]
# Database connection settings
host: "localhost"
port: 5432       # PostgreSQL default port
name: "myapp"
user: "postgres"
password: @env("DB_PASSWORD")  # Secure password from environment
""")
```

## 🚀 Next Steps

Now that you understand the basic syntax:

1. **Learn FUJSEN Functions** - [004-fujsen-python.md](004-fujsen-python.md)
2. **Master @ Operators** - [006-at-operators-python.md](006-at-operators-python.md)
3. **Integrate Databases** - [005-database-integration-python.md](005-database-integration-python.md)
4. **Explore Advanced Features** - [007-advanced-features-python.md](007-advanced-features-python.md)

---

**"We don't bow to any king"** - TuskLang's flexible syntax gives you the power to write configuration in your preferred style while maintaining the revolutionary capabilities of executable configuration with a heartbeat! 