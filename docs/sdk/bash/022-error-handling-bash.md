# ⚠️ TuskLang Bash Error Handling Guide

**"We don't bow to any king" – Handle errors like a pro, not like a peasant.**

Error handling in TuskLang is about building robust, resilient configurations that gracefully handle failures, provide meaningful feedback, and maintain system stability. Whether you're dealing with missing files, invalid data, or system failures, proper error handling ensures your TuskLang applications remain reliable and user-friendly.

## 🎯 Why Error Handling Matters
Proper error handling provides:
- **Reliability** - Systems that don't crash on errors
- **Debugging** - Clear error messages for troubleshooting
- **User experience** - Graceful degradation when things go wrong
- **Security** - Prevention of information leakage through errors
- **Maintainability** - Easier to identify and fix issues

## 📝 Error Types in TuskLang

### Configuration Errors
```ini
[error_examples]
# Missing required values
api_key: @env("API_KEY")  # Will fail if API_KEY not set

# Invalid syntax
invalid_array: [1, 2, 3,  # Missing closing bracket

# Type mismatches
port: @int("not_a_number")  # Will fail conversion
```

### Runtime Errors
```ini
[runtime_errors]
# File not found
config_content: @file.read("nonexistent.txt")

# Database connection failure
user_count: @query("SELECT COUNT(*) FROM users")

# Shell command failure
system_info: @shell("invalid_command")
```

## 🚀 Quick Start Example

```bash
#!/bin/bash
source tusk-bash.sh

# Robust configuration loading with error handling
load_config() {
    local config_file="$1"
    
    # Check if file exists
    if [[ ! -f "$config_file" ]]; then
        echo "Error: Configuration file $config_file not found" >&2
        return 1
    fi
    
    # Validate configuration syntax
    if ! tusk_validate "$config_file" 2>/dev/null; then
        echo "Error: Invalid configuration syntax in $config_file" >&2
        return 1
    fi
    
    # Parse configuration with error handling
    local config
    if ! config=$(tusk_parse "$config_file" 2>/dev/null); then
        echo "Error: Failed to parse configuration file $config_file" >&2
        return 1
    fi
    
    echo "$config"
}

# Usage with error handling
if config=$(load_config "app.tsk"); then
    echo "Configuration loaded successfully"
    app_name=$(tusk_get "$config" app.name 2>/dev/null || echo "Unknown")
    echo "App: $app_name"
else
    echo "Failed to load configuration, using defaults"
    # Fallback to default configuration
    app_name="DefaultApp"
fi
```

## 🔗 Real-World Use Cases

### 1. Environment Variable Validation
```bash
#!/bin/bash
source tusk-bash.sh

cat > env-validation.tsk << 'EOF'
[app]
api_key: @env("API_KEY")
database_url: @env("DATABASE_URL")
debug_mode: @env("DEBUG", "false")
EOF

config=$(tusk_parse env-validation.tsk)

# Validate required environment variables
validate_required_env() {
    local api_key=$(tusk_get "$config" app.api_key 2>/dev/null)
    local db_url=$(tusk_get "$config" app.database_url 2>/dev/null)
    
    if [[ -z "$api_key" ]]; then
        echo "Error: API_KEY environment variable is required" >&2
        return 1
    fi
    
    if [[ -z "$db_url" ]]; then
        echo "Error: DATABASE_URL environment variable is required" >&2
        return 1
    fi
    
    echo "All required environment variables are set"
}

validate_required_env
```

### 2. Database Connection Error Handling
```bash
#!/bin/bash
source tusk-bash.sh

cat > db-config.tsk << 'EOF'
[database]
host: @env("DB_HOST", "localhost")
port: @env("DB_PORT", "5432")
name: @env("DB_NAME", "myapp")
EOF

config=$(tusk_parse db-config.tsk)

# Test database connection with error handling
test_database_connection() {
    local host=$(tusk_get "$config" database.host)
    local port=$(tusk_get "$config" database.port)
    local name=$(tusk_get "$config" database.name)
    
    echo "Testing database connection to $host:$port/$name..."
    
    # Test connection with timeout
    if timeout 5 bash -c "echo 'SELECT 1;' | psql -h $host -p $port -d $name" >/dev/null 2>&1; then
        echo "Database connection successful"
        return 0
    else
        echo "Error: Database connection failed" >&2
        return 1
    fi
}

test_database_connection
```

### 3. File Operation Error Handling
```bash
#!/bin/bash
source tusk-bash.sh

cat > file-operations.tsk << 'EOF'
[files]
config_path: @env("CONFIG_PATH", "/etc/app/config.tsk")
log_path: @env("LOG_PATH", "/var/log/app.log")
EOF

config=$(tusk_parse file-operations.tsk)

# Safe file operations with error handling
safe_file_operations() {
    local config_path=$(tusk_get "$config" files.config_path)
    local log_path=$(tusk_get "$config" files.log_path)
    
    # Check if config file exists and is readable
    if [[ ! -f "$config_path" ]]; then
        echo "Error: Configuration file $config_path not found" >&2
        return 1
    fi
    
    if [[ ! -r "$config_path" ]]; then
        echo "Error: Configuration file $config_path is not readable" >&2
        return 1
    fi
    
    # Check if log directory is writable
    local log_dir=$(dirname "$log_path")
    if [[ ! -w "$log_dir" ]]; then
        echo "Error: Log directory $log_dir is not writable" >&2
        return 1
    fi
    
    echo "File operations validated successfully"
}

safe_file_operations
```

### 4. API Error Handling
```bash
#!/bin/bash
source tusk-bash.sh

cat > api-config.tsk << 'EOF'
[api]
base_url: @env("API_BASE_URL", "https://api.example.com")
timeout: @env("API_TIMEOUT", "30")
retries: @env("API_RETRIES", "3")
EOF

config=$(tusk_parse api-config.tsk)

# API request with error handling
api_request() {
    local endpoint="$1"
    local base_url=$(tusk_get "$config" api.base_url)
    local timeout=$(tusk_get "$config" api.timeout)
    local retries=$(tusk_get "$config" api.retries)
    
    local url="${base_url}${endpoint}"
    local attempt=1
    
    while [[ $attempt -le $retries ]]; do
        echo "API request attempt $attempt/$retries to $url"
        
        if response=$(curl -s -w "%{http_code}" --max-time "$timeout" "$url" 2>/dev/null); then
            local http_code="${response: -3}"
            local body="${response%???}"
            
            if [[ "$http_code" == "200" ]]; then
                echo "API request successful: $body"
                return 0
            else
                echo "API request failed with HTTP $http_code" >&2
            fi
        else
            echo "API request failed (attempt $attempt/$retries)" >&2
        fi
        
        ((attempt++))
        [[ $attempt -le $retries ]] && sleep 2
    done
    
    echo "Error: All API request attempts failed" >&2
    return 1
}

api_request "/health"
```

## 🧠 Advanced Error Handling Patterns

### Graceful Degradation
```bash
#!/bin/bash
source tusk-bash.sh

# Load configuration with fallbacks
load_config_with_fallbacks() {
    local primary_config="$1"
    local fallback_config="$2"
    
    # Try primary configuration first
    if [[ -f "$primary_config" ]] && config=$(tusk_parse "$primary_config" 2>/dev/null); then
        echo "Using primary configuration: $primary_config"
        echo "$config"
        return 0
    fi
    
    # Fall back to secondary configuration
    if [[ -f "$fallback_config" ]] && config=$(tusk_parse "$fallback_config" 2>/dev/null); then
        echo "Using fallback configuration: $fallback_config" >&2
        echo "$config"
        return 0
    fi
    
    # Use default configuration
    echo "Using default configuration" >&2
    cat > /tmp/default.tsk << 'EOF'
[app]
name: "DefaultApp"
port: 8080
debug: false
EOF
    
    tusk_parse /tmp/default.tsk
}

config=$(load_config_with_fallbacks "app.tsk" "app.backup.tsk")
```

### Error Logging and Monitoring
```bash
#!/bin/bash
source tusk-bash.sh

# Error logging function
log_error() {
    local message="$1"
    local severity="${2:-ERROR}"
    local timestamp=$(date '+%Y-%m-%d %H:%M:%S')
    
    echo "[$timestamp] [$severity] $message" >> /var/log/tusk-errors.log
    
    # Send to monitoring system if available
    if command -v curl >/dev/null 2>&1; then
        curl -s -X POST "https://monitoring.example.com/errors" \
             -H "Content-Type: application/json" \
             -d "{\"message\":\"$message\",\"severity\":\"$severity\",\"timestamp\":\"$timestamp\"}" \
             >/dev/null 2>&1 &
    fi
}

# Wrapper for TuskLang operations with error logging
safe_tusk_operation() {
    local operation="$1"
    local args="$2"
    
    if result=$($operation $args 2>&1); then
        echo "$result"
        return 0
    else
        log_error "TuskLang operation failed: $operation $args - $result"
        return 1
    fi
}

# Usage
if ! config=$(safe_tusk_operation "tusk_parse" "config.tsk"); then
    echo "Configuration loading failed, check error logs"
    exit 1
fi
```

## 🛡️ Security & Performance Notes
- **Error message security:** Don't expose sensitive information in error messages.
- **Logging security:** Ensure error logs don't contain sensitive data.
- **Performance impact:** Error handling adds minimal overhead but improves reliability.
- **Resource cleanup:** Always clean up resources even when errors occur.

## 🐞 Troubleshooting
- **Silent failures:** Check for commands that fail silently and add proper error checking.
- **Error propagation:** Ensure errors are properly propagated up the call stack.
- **Log analysis:** Regularly review error logs to identify patterns and issues.
- **Testing:** Test error conditions to ensure error handling works correctly.

## 💡 Best Practices
- **Always check return codes:** Check the return status of all operations.
- **Provide meaningful messages:** Give users clear, actionable error messages.
- **Log errors appropriately:** Log errors with sufficient detail for debugging.
- **Use fallbacks:** Provide fallback options when primary operations fail.
- **Test error conditions:** Regularly test your error handling code.

## 🔗 Cross-References
- [CLI Overview](016-cli-overview-bash.md)
- [Validation](023-best-practices-bash.md)
- [Debugging](104-debugging-bash.md)

---

**Master error handling in TuskLang and build robust, reliable applications. ⚠️** 