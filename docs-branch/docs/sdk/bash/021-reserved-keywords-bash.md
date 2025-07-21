# 🚫 TuskLang Bash Reserved Keywords Guide

**"We don't bow to any king" – Know the rules, break the boundaries.**

Reserved keywords in TuskLang are special words that have predefined meanings and cannot be used as variable names or identifiers. Understanding these keywords is essential for writing valid TuskLang configurations and avoiding syntax errors.

## 🎯 What are Reserved Keywords?
Reserved keywords are special words that TuskLang uses for:
- **Operators** - @ symbols and their functions
- **Directives** - Hash directives for special behavior
- **Control structures** - Conditional and flow control
- **Built-in functions** - Predefined functionality
- **Special values** - null, true, false

## 📝 Reserved Keywords List

### @ Operators
```ini
# These are reserved and cannot be used as variable names
@env          # Environment variables
@shell        # Shell command execution
@file         # File operations
@query        # Database queries
@date         # Date/time operations
@math         # Mathematical functions
@string       # String operations
@array        # Array operations
@object       # Object operations
@validate     # Validation functions
@if           # Conditional logic
@cache        # Caching operations
@metrics      # Metrics collection
@learn        # Machine learning
@optimize     # Optimization functions
@encrypt      # Encryption functions
@json         # JSON operations
@render       # Template rendering
@redirect     # Redirect operations
@http         # HTTP operations
@session      # Session variables
@cookie       # Cookie variables
@request      # Request object
@server       # Server variables
@global       # Global variables
@debug        # Debug operations
@peanuts      # Peanuts function
@php          # PHP execution
```

### Hash Directives
```ini
# Hash directives are reserved
#api          # API directive
#web          # Web directive
#cli          # CLI directive
#cron         # Cron directive
#middleware   # Middleware directive
#auth         # Authentication directive
#cache        # Cache directive
#rate-limit   # Rate limiting directive
```

### Control Keywords
```ini
# Control flow keywords
if            # Conditional statements
else          # Alternative conditions
endif         # End of conditional block
for           # Loop control
while         # While loop control
break         # Loop break
continue      # Loop continue
return        # Function return
```

### Special Values
```ini
# Special values that are reserved
null          # Null value
true          # Boolean true
false         # Boolean false
yes            # Boolean true (alternative)
no             # Boolean false (alternative)
on             # Boolean true (alternative)
off            # Boolean false (alternative)
```

## 🚀 Quick Start Example

```bash
#!/bin/bash
source tusk-bash.sh

cat > reserved-keywords-example.tsk << 'EOF'
[valid_config]
# ✅ Valid: Using reserved keywords correctly
api_key: @env("API_KEY")
current_time: @date.now()
user_count: @query("SELECT COUNT(*) FROM users")
debug_mode: @env("DEBUG", false)

# ✅ Valid: Using special values correctly
maintenance_mode: false
ssl_enabled: true
empty_value: null
EOF

config=$(tusk_parse reserved-keywords-example.tsk)
echo "API Key: $(tusk_get "$config" valid_config.api_key)"
echo "Current Time: $(tusk_get "$config" valid_config.current_time)"
echo "User Count: $(tusk_get "$config" valid_config.user_count)"
echo "Debug Mode: $(tusk_get "$config" valid_config.debug_mode)"
```

## 🔗 Real-World Use Cases

### 1. Environment Configuration
```ini
[environment]
# ✅ Correct usage of reserved keywords
database_host: @env("DB_HOST", "localhost")
database_port: @env("DB_PORT", "5432")
debug_enabled: @env("DEBUG", false)
api_timeout: @env("API_TIMEOUT", "30")

# ❌ Invalid: Using reserved keywords as variable names
# @env: "localhost"        # This will cause an error
# @shell: "some_command"   # This will cause an error
```

### 2. Database Operations
```ini
[database]
# ✅ Correct usage
user_count: @query("SELECT COUNT(*) FROM users")
active_users: @query("SELECT COUNT(*) FROM users WHERE active = 1")
last_login: @query("SELECT MAX(last_login) FROM users")

# ❌ Invalid: Using @query as a variable name
# @query: "SELECT * FROM users"  # This will cause an error
```

### 3. File Operations
```ini
[files]
# ✅ Correct usage
config_content: @file.read("config.json")
file_exists: @file.exists("important.txt")
file_size: @file.size("large_file.dat")

# ❌ Invalid: Using @file as a variable name
# @file: "some_file.txt"  # This will cause an error
```

### 4. Conditional Logic
```ini
[logic]
# ✅ Correct usage
environment: @env("APP_ENV", "development")
debug_mode: @if($environment == "development", true, false)
log_level: @if($debug_mode, "debug", "info")

# ❌ Invalid: Using @if as a variable name
# @if: "some_condition"  # This will cause an error
```

## 🧠 Advanced Patterns

### Avoiding Conflicts
```ini
[avoiding_conflicts]
# ✅ Good: Use descriptive names that don't conflict
application_environment: @env("APP_ENV", "development")
database_connection_string: @env("DB_URL")
file_upload_path: @env("UPLOAD_PATH")

# ❌ Bad: Names that might be confused with reserved keywords
env: @env("APP_ENV", "development")        # Could be confusing
query: @env("DB_QUERY")                    # Could be confusing
file: @env("FILE_PATH")                    # Could be confusing
```

### Using Reserved Keywords in Strings
```ini
[strings]
# ✅ Valid: Reserved keywords in strings are fine
description: "This uses @env and @query functions"
message: "The @if statement controls flow"
template: "Use @render to process templates"

# These are just strings, not actual reserved keywords
```

### Hash Directives Usage
```ini
# ✅ Correct usage of hash directives
#api
endpoint: "/api/v1"
methods: ["GET", "POST"]

#web
port: 8080
host: "localhost"

#cli
command: "deploy"
arguments: ["--env", "production"]
```

## 🛡️ Security & Performance Notes
- **Keyword conflicts:** Avoid using reserved keywords as variable names to prevent parsing errors.
- **Security implications:** Some reserved keywords (like @shell) can execute commands; use carefully.
- **Performance:** Reserved keywords are optimized for performance.
- **Validation:** Use @validate to ensure your configuration doesn't conflict with reserved keywords.

## 🐞 Troubleshooting
- **Parsing errors:** Check for reserved keywords used as variable names.
- **Unexpected behavior:** Ensure you're using reserved keywords correctly.
- **Case sensitivity:** Reserved keywords are case-sensitive.
- **Scope issues:** Reserved keywords have specific scopes and contexts.

## 💡 Best Practices
- **Know the keywords:** Familiarize yourself with all reserved keywords.
- **Use descriptive names:** Choose variable names that clearly indicate their purpose.
- **Avoid conflicts:** Don't use names that could be confused with reserved keywords.
- **Document usage:** Document when and how you use reserved keywords.
- **Test thoroughly:** Test configurations to ensure no keyword conflicts.

## 🔗 Cross-References
- [@ Operators](024-at-operator-intro-bash.md)
- [Hash Directives](069-hash-directive-intro-bash.md)
- [Variable Naming](020-variable-naming-bash.md)

---

**Master reserved keywords in TuskLang and write error-free, powerful configurations. 🚫** 