# Hash Directives in TuskLang - Bash Guide

## 🎯 **Revolutionary Configuration Directives**

Hash directives (`#`) are TuskLang's powerful way to add behavior, logic, and intelligence to your configuration files. Unlike traditional config files that just store static values, hash directives make your configuration **alive** and **responsive**.

> **"We don't bow to any king"** - TuskLang hash directives break free from static configuration constraints and bring dynamic behavior to your Bash applications.

## 🚀 **What Are Hash Directives?**

Hash directives are special commands that start with `#` and provide dynamic functionality:

```bash
#web: true                    # Enable web server features
#api: /api/v1                 # Define API endpoints
#cli: --help                  # Add CLI commands
#cron: "0 */6 * * *"         # Schedule background tasks
#middleware: auth             # Apply middleware
#auth: jwt                    # Configure authentication
#cache: redis                 # Set caching strategy
#rate-limit: 100/min          # Apply rate limiting
```

## 🎛️ **Core Hash Directive Categories**

### **1. Server & API Directives**
```bash
#web: true                    # Enable web server
#api: /api/v1                 # API base path
#port: 8080                   # Server port
#host: 0.0.0.0               # Bind address
#ssl: true                    # Enable HTTPS
#cors: "*"                    # CORS policy
```

### **2. CLI & Automation Directives**
```bash
#cli: --help                  # CLI command
#cron: "0 */6 * * *"         # Scheduled task
#daemon: true                 # Run as daemon
#pid: /var/run/app.pid       # PID file location
#log: /var/log/app.log       # Log file path
```

### **3. Security & Authentication**
```bash
#auth: jwt                    # Authentication method
#middleware: auth             # Apply middleware
#rate-limit: 100/min          # Rate limiting
#encrypt: AES-256-GCM         # Encryption method
#validate: required           # Input validation
```

### **4. Performance & Caching**
```bash
#cache: redis                 # Caching backend
#cache-ttl: 3600             # Cache TTL in seconds
#compress: gzip               # Compression method
#optimize: true               # Enable optimizations
#monitor: true                # Enable monitoring
```

## 🔧 **Bash Integration Examples**

### **Web Server Configuration**
```bash
#!/bin/bash

# Load TuskLang configuration with web directives
source <(tsk load config.tsk)

# Check if web server is enabled
if [[ "$web_enabled" == "true" ]]; then
    echo "Starting web server on port $server_port"
    
    # Start web server based on configuration
    if [[ "$ssl_enabled" == "true" ]]; then
        echo "Starting HTTPS server with SSL"
        # SSL server startup logic
    else
        echo "Starting HTTP server"
        # HTTP server startup logic
    fi
fi
```

### **API Endpoint Management**
```bash
#!/bin/bash

# Load API configuration
source <(tsk load api.tsk)

# Process API directives
for endpoint in "${api_endpoints[@]}"; do
    echo "Registering API endpoint: $endpoint"
    
    # Extract endpoint details
    path=$(echo "$endpoint" | jq -r '.path')
    method=$(echo "$endpoint" | jq -r '.method')
    handler=$(echo "$endpoint" | jq -r '.handler')
    
    echo "  Path: $path"
    echo "  Method: $method"
    echo "  Handler: $handler"
done
```

### **CLI Command Processing**
```bash
#!/bin/bash

# Load CLI configuration
source <(tsk load cli.tsk)

# Process CLI directives
process_cli_directives() {
    local config_file="$1"
    
    # Extract CLI commands from TuskLang config
    while IFS= read -r line; do
        if [[ "$line" =~ ^#cli: ]]; then
            local command="${line#\#cli: }"
            echo "Registering CLI command: $command"
            
            # Create command handler
            create_command_handler "$command"
        fi
    done < "$config_file"
}

create_command_handler() {
    local command="$1"
    local handler_name="handle_${command//-/_}"
    
    # Generate handler function
    cat << EOF
$handler_name() {
    echo "Executing: $command"
    # Command implementation
}
EOF
}
```

### **Cron Job Management**
```bash
#!/bin/bash

# Load cron configuration
source <(tsk load cron.tsk)

# Process cron directives
setup_cron_jobs() {
    local config_file="$1"
    
    # Extract cron schedules
    while IFS= read -r line; do
        if [[ "$line" =~ ^#cron: ]]; then
            local schedule="${line#\#cron: }"
            local script_path="${line#\#cron: *}"
            
            echo "Setting up cron job: $schedule -> $script_path"
            
            # Add to crontab
            (crontab -l 2>/dev/null; echo "$schedule $script_path") | crontab -
        fi
    done < "$config_file"
}
```

## 🛡️ **Security Directives**

### **Authentication Configuration**
```bash
#!/bin/bash

# Load security configuration
source <(tsk load security.tsk)

# Apply authentication middleware
apply_auth_middleware() {
    case "$auth_method" in
        "jwt")
            echo "Applying JWT authentication"
            setup_jwt_auth
            ;;
        "oauth")
            echo "Applying OAuth authentication"
            setup_oauth_auth
            ;;
        "basic")
            echo "Applying Basic authentication"
            setup_basic_auth
            ;;
        *)
            echo "Unknown auth method: $auth_method"
            exit 1
            ;;
    esac
}

setup_jwt_auth() {
    # JWT setup logic
    export JWT_SECRET="$jwt_secret"
    export JWT_ALGORITHM="$jwt_algorithm"
    echo "JWT authentication configured"
}
```

### **Rate Limiting Implementation**
```bash
#!/bin/bash

# Load rate limiting configuration
source <(tsk load rate-limit.tsk)

# Parse rate limit directive
parse_rate_limit() {
    local rate_limit="$1"
    
    # Extract limit and time window
    if [[ "$rate_limit" =~ ([0-9]+)/([a-z]+) ]]; then
        local limit="${BASH_REMATCH[1]}"
        local window="${BASH_REMATCH[2]}"
        
        echo "Rate limit: $limit requests per $window"
        
        # Convert to seconds
        case "$window" in
            "min") window_seconds=60 ;;
            "hour") window_seconds=3600 ;;
            "day") window_seconds=86400 ;;
            *) window_seconds=60 ;;
        esac
        
        export RATE_LIMIT="$limit"
        export RATE_WINDOW="$window_seconds"
    fi
}

# Apply rate limiting
apply_rate_limiting() {
    local client_ip="$1"
    local request_count=$(get_request_count "$client_ip")
    
    if [[ "$request_count" -gt "$RATE_LIMIT" ]]; then
        echo "Rate limit exceeded for $client_ip"
        return 1
    fi
    
    increment_request_count "$client_ip"
    return 0
}
```

## ⚡ **Performance Directives**

### **Caching Configuration**
```bash
#!/bin/bash

# Load caching configuration
source <(tsk load cache.tsk)

# Setup caching backend
setup_cache() {
    case "$cache_backend" in
        "redis")
            echo "Setting up Redis cache"
            setup_redis_cache
            ;;
        "memcached")
            echo "Setting up Memcached cache"
            setup_memcached_cache
            ;;
        "file")
            echo "Setting up file cache"
            setup_file_cache
            ;;
        *)
            echo "Unknown cache backend: $cache_backend"
            exit 1
            ;;
    esac
}

setup_redis_cache() {
    export REDIS_HOST="$redis_host"
    export REDIS_PORT="$redis_port"
    export REDIS_DB="$redis_db"
    export CACHE_TTL="$cache_ttl"
    
    # Test Redis connection
    if redis-cli -h "$REDIS_HOST" -p "$REDIS_PORT" ping >/dev/null 2>&1; then
        echo "Redis cache connected successfully"
    else
        echo "Failed to connect to Redis cache"
        exit 1
    fi
}
```

### **Compression Setup**
```bash
#!/bin/bash

# Load compression configuration
source <(tsk load compression.tsk)

# Apply compression middleware
apply_compression() {
    case "$compression_method" in
        "gzip")
            echo "Applying Gzip compression"
            setup_gzip_compression
            ;;
        "brotli")
            echo "Applying Brotli compression"
            setup_brotli_compression
            ;;
        "deflate")
            echo "Applying Deflate compression"
            setup_deflate_compression
            ;;
        *)
            echo "No compression applied"
            ;;
    esac
}

setup_gzip_compression() {
    export COMPRESSION_LEVEL="$compression_level"
    export COMPRESSION_MIN_SIZE="$compression_min_size"
    
    # Check if gzip is available
    if command -v gzip >/dev/null 2>&1; then
        echo "Gzip compression ready"
    else
        echo "Gzip not available, skipping compression"
    fi
}
```

## 🔍 **Monitoring & Debugging**

### **Debug Mode Configuration**
```bash
#!/bin/bash

# Load debug configuration
source <(tsk load debug.tsk)

# Enable debug mode
enable_debug_mode() {
    if [[ "$debug_enabled" == "true" ]]; then
        echo "Debug mode enabled"
        export DEBUG_LEVEL="$debug_level"
        export DEBUG_LOG="$debug_log"
        
        # Set Bash debug options
        set -x  # Print commands before execution
        
        # Enable verbose logging
        exec 1> >(tee -a "$DEBUG_LOG")
        exec 2> >(tee -a "$DEBUG_LOG" >&2)
        
        echo "Debug logging to: $DEBUG_LOG"
    fi
}
```

### **Health Check Endpoints**
```bash
#!/bin/bash

# Load health check configuration
source <(tsk load health.tsk)

# Setup health check endpoints
setup_health_checks() {
    if [[ "$health_check_enabled" == "true" ]]; then
        echo "Setting up health check endpoints"
        
        # Create health check script
        cat > /tmp/health-check.sh << 'EOF'
#!/bin/bash
echo "Health check at $(date)"
echo "Status: OK"
echo "Uptime: $(uptime)"
echo "Memory: $(free -h | grep Mem)"
echo "Disk: $(df -h / | tail -1)"
EOF
        
        chmod +x /tmp/health-check.sh
        
        # Schedule health checks
        if [[ -n "$health_check_interval" ]]; then
            echo "Scheduling health checks every $health_check_interval"
            (crontab -l 2>/dev/null; echo "*/$health_check_interval * * * * /tmp/health-check.sh") | crontab -
        fi
    fi
}
```

## 🎨 **Custom Directive Creation**

### **Creating Custom Directives**
```bash
#!/bin/bash

# Custom directive processor
process_custom_directives() {
    local config_file="$1"
    
    while IFS= read -r line; do
        case "$line" in
            \#custom:*)
                local directive="${line#\#custom: }"
                handle_custom_directive "$directive"
                ;;
            \#plugin:*)
                local plugin="${line#\#plugin: }"
                load_plugin "$plugin"
                ;;
            \#hook:*)
                local hook="${line#\#hook: }"
                register_hook "$hook"
                ;;
        esac
    done < "$config_file"
}

handle_custom_directive() {
    local directive="$1"
    echo "Processing custom directive: $directive"
    
    # Parse directive components
    IFS=':' read -r directive_name directive_value <<< "$directive"
    
    case "$directive_name" in
        "backup")
            setup_backup "$directive_value"
            ;;
        "notification")
            setup_notification "$directive_value"
            ;;
        "integration")
            setup_integration "$directive_value"
            ;;
        *)
            echo "Unknown custom directive: $directive_name"
            ;;
    esac
}
```

## 📊 **Real-World Configuration Examples**

### **Complete Web Application**
```bash
# app.tsk
app_name: "My Awesome App"
version: "1.0.0"

#web: true
#api: /api/v1
#port: 8080
#host: 0.0.0.0
#ssl: true
#cors: "*"

#auth: jwt
#middleware: auth
#rate-limit: 100/min

#cache: redis
#cache-ttl: 3600
#compress: gzip

#cron: "0 */6 * * *"
#cli: --help

#monitor: true
#debug: true
```

### **Microservice Configuration**
```bash
# service.tsk
service_name: "user-service"
service_port: 3001

#api: /api/users
#auth: jwt
#rate-limit: 50/min

#cache: redis
#cache-ttl: 1800

#health: /health
#metrics: /metrics

#cron: "*/5 * * * *"
#daemon: true
#pid: /var/run/user-service.pid
#log: /var/log/user-service.log
```

## 🚨 **Troubleshooting Hash Directives**

### **Common Issues and Solutions**

**1. Directive Not Recognized**
```bash
# Problem: Unknown directive error
# Solution: Check directive syntax and spelling
if [[ "$line" =~ ^#[a-z-]+: ]]; then
    echo "Valid directive format: $line"
else
    echo "Invalid directive format: $line"
fi
```

**2. Directive Processing Errors**
```bash
# Problem: Directive processing fails
# Solution: Add error handling and validation
process_directive() {
    local directive="$1"
    
    # Validate directive format
    if [[ ! "$directive" =~ ^#[a-z-]+: ]]; then
        echo "Invalid directive format: $directive" >&2
        return 1
    fi
    
    # Extract directive name and value
    local name="${directive#\#}"
    name="${name%%:*}"
    local value="${directive#*: }"
    
    # Process with error handling
    case "$name" in
        "web"|"api"|"cli"|"cron"|"auth"|"cache"|"rate-limit")
            echo "Processing $name directive: $value"
            ;;
        *)
            echo "Unknown directive: $name" >&2
            return 1
            ;;
    esac
}
```

**3. Configuration Loading Issues**
```bash
# Problem: Directives not loading properly
# Solution: Validate configuration file structure
validate_config_file() {
    local config_file="$1"
    
    if [[ ! -f "$config_file" ]]; then
        echo "Configuration file not found: $config_file" >&2
        return 1
    fi
    
    if [[ ! -r "$config_file" ]]; then
        echo "Configuration file not readable: $config_file" >&2
        return 1
    fi
    
    # Check for basic TuskLang syntax
    if ! grep -q "^[a-zA-Z_][a-zA-Z0-9_]*:" "$config_file"; then
        echo "No valid TuskLang key-value pairs found" >&2
        return 1
    fi
    
    echo "Configuration file validated successfully"
    return 0
}
```

## 🔒 **Security Best Practices**

### **Directive Security**
```bash
# Validate directive values
validate_directive_value() {
    local directive="$1"
    local value="$2"
    
    case "$directive" in
        "port")
            # Validate port number
            if [[ ! "$value" =~ ^[0-9]+$ ]] || [[ "$value" -lt 1 ]] || [[ "$value" -gt 65535 ]]; then
                echo "Invalid port number: $value" >&2
                return 1
            fi
            ;;
        "rate-limit")
            # Validate rate limit format
            if [[ ! "$value" =~ ^[0-9]+/(min|hour|day)$ ]]; then
                echo "Invalid rate limit format: $value" >&2
                return 1
            fi
            ;;
        "cron")
            # Validate cron expression
            if ! echo "$value" | grep -E "^[0-9*/,-\s]+$" >/dev/null; then
                echo "Invalid cron expression: $value" >&2
                return 1
            fi
            ;;
    esac
    
    return 0
}
```

## 📈 **Performance Considerations**

### **Directive Processing Optimization**
```bash
# Optimize directive processing
optimize_directive_processing() {
    local config_file="$1"
    
    # Pre-compile regex patterns
    local directive_pattern='^#[a-z-]+:'
    local comment_pattern='^#.*$'
    
    # Process directives in batches
    local directives=()
    while IFS= read -r line; do
        if [[ "$line" =~ $directive_pattern ]] && [[ ! "$line" =~ $comment_pattern ]]; then
            directives+=("$line")
        fi
    done < "$config_file"
    
    # Process all directives at once
    for directive in "${directives[@]}"; do
        process_directive "$directive"
    done
}
```

## 🎯 **Next Steps**

- **Advanced Directives**: Explore custom directive creation
- **Middleware Integration**: Learn about directive middleware
- **Plugin System**: Understand directive plugins
- **Testing Directives**: Test directive functionality
- **Performance Tuning**: Optimize directive processing

---

**Hash directives transform static configuration into dynamic, intelligent systems. They're the heartbeat that makes TuskLang configuration truly alive!** 