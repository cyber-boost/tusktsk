# Middleware Directives in TuskLang - Bash Guide

## 🔗 **Revolutionary Middleware Configuration**

Middleware directives in TuskLang transform your configuration files into intelligent request processing pipelines. No more separate middleware files or complex integration code - everything lives in your TuskLang configuration with dynamic middleware chains, automatic request processing, and intelligent response handling.

> **"We don't bow to any king"** - TuskLang middleware directives break free from traditional middleware framework constraints and bring modern request processing capabilities to your Bash applications.

## 🚀 **Core Middleware Directives**

### **Basic Middleware Setup**
```bash
#middleware: auth               # Enable authentication middleware
#middleware-order: ["auth", "cors", "logging", "rate-limit"] # Middleware execution order
#middleware-config: auth.jwt.secret=mysecret # Middleware configuration
#middleware-enabled: true       # Enable middleware processing
#middleware-timeout: 30         # Middleware timeout in seconds
#middleware-logging: true       # Enable middleware logging
```

### **Advanced Middleware Configuration**
```bash
#middleware-chain: ["auth", "cors", "logging", "rate-limit", "cache", "compress"]
#middleware-custom: my-custom-middleware # Custom middleware function
#middleware-error-handling: true # Enable error handling middleware
#middleware-metrics: true       # Enable middleware metrics collection
#middleware-debug: false        # Enable middleware debug mode
#middleware-cache: redis        # Middleware caching backend
```

## 🔧 **Bash Middleware Implementation**

### **Basic Middleware Manager**
```bash
#!/bin/bash

# Load middleware configuration
source <(tsk load middleware.tsk)

# Middleware configuration
MIDDLEWARE_ENABLED="${middleware_enabled:-true}"
MIDDLEWARE_TIMEOUT="${middleware_timeout:-30}"
MIDDLEWARE_LOGGING="${middleware_logging:-true}"
MIDDLEWARE_DEBUG="${middleware_debug:-false}"

# Middleware manager
class MiddlewareManager {
    constructor() {
        this.middleware_chain = []
        this.middleware_config = {}
        this.request_context = {}
        this.response_context = {}
    }
    
    addMiddleware(name, handler, options = {}) {
        this.middleware_chain.push({
            name: name,
            handler: handler,
            enabled: options.enabled !== false,
            order: options.order || this.middleware_chain.length,
            config: options.config || {}
        })
    }
    
    processRequest(request) {
        this.request_context = request
        this.response_context = {}
        
        console.log("Processing request through middleware chain...")
        
        for (const middleware of this.middleware_chain) {
            if (!middleware.enabled) {
                console.log(`Skipping disabled middleware: ${middleware.name}`)
                continue
            }
            
            console.log(`Executing middleware: ${middleware.name}`)
            
            try {
                const result = this.executeMiddleware(middleware, request)
                if (result === false) {
                    console.log(`Middleware ${middleware.name} blocked request`)
                    return this.response_context
                }
            } catch (error) {
                console.error(`Middleware ${middleware.name} failed: ${error.message}`)
                if (this.middleware_config.error_handling) {
                    this.handleMiddlewareError(middleware, error)
                } else {
                    throw error
                }
            }
        }
        
        return this.response_context
    }
    
    executeMiddleware(middleware, request) {
        const start_time = Date.now()
        
        // Set timeout for middleware execution
        const timeout_promise = new Promise((_, reject) => {
            setTimeout(() => reject(new Error('Middleware timeout')), this.middleware_config.timeout * 1000)
        })
        
        const middleware_promise = middleware.handler(request, this.response_context, middleware.config)
        
        return Promise.race([middleware_promise, timeout_promise])
            .then(result => {
                const execution_time = Date.now() - start_time
                this.logMiddlewareExecution(middleware.name, execution_time, 'success')
                return result
            })
            .catch(error => {
                const execution_time = Date.now() - start_time
                this.logMiddlewareExecution(middleware.name, execution_time, 'error', error.message)
                throw error
            })
    }
    
    logMiddlewareExecution(name, execution_time, status, error = null) {
        if (this.middleware_config.logging) {
            const log_entry = {
                timestamp: new Date().toISOString(),
                middleware: name,
                execution_time: execution_time,
                status: status,
                error: error
            }
            
            console.log(`Middleware Log: ${JSON.stringify(log_entry)}`)
        }
    }
    
    handleMiddlewareError(middleware, error) {
        console.error(`Error in middleware ${middleware.name}: ${error.message}`)
        
        // Create error response
        this.response_context.status_code = 500
        this.response_context.error = {
            middleware: middleware.name,
            message: error.message,
            timestamp: new Date().toISOString()
        }
    }
}

# Initialize middleware manager
const middlewareManager = new MiddlewareManager()

# Configure middleware
middlewareManager.middleware_config = {
    timeout: MIDDLEWARE_TIMEOUT,
    logging: MIDDLEWARE_LOGGING,
    debug: MIDDLEWARE_DEBUG,
    error_handling: true
}
```

### **Authentication Middleware**
```bash
#!/bin/bash

# Authentication middleware
auth_middleware() {
    local request="$1"
    local response="$2"
    local config="$3"
    
    echo "Executing authentication middleware..."
    
    # Extract authentication method
    local auth_method="${config[auth_method]:-jwt}"
    local auth_header=$(echo "$request" | grep -i "authorization:" | cut -d' ' -f2-)
    
    if [[ -z "$auth_header" ]]; then
        echo "No authorization header found"
        response["status_code"]=401
        response["error"]="Authentication required"
        return 1
    fi
    
    case "$auth_method" in
        "jwt")
            authenticate_jwt "$auth_header" "$config" "$response"
            ;;
        "api-key")
            authenticate_api_key "$auth_header" "$config" "$response"
            ;;
        "basic")
            authenticate_basic "$auth_header" "$config" "$response"
            ;;
        "oauth")
            authenticate_oauth "$auth_header" "$config" "$response"
            ;;
        *)
            echo "Unknown authentication method: $auth_method"
            response["status_code"]=401
            response["error"]="Unsupported authentication method"
            return 1
            ;;
    esac
}

authenticate_jwt() {
    local token="$1"
    local config="$2"
    local response="$3"
    
    # Remove "Bearer " prefix
    token="${token#Bearer }"
    
    if [[ -z "$token" ]]; then
        echo "No JWT token found"
        response["status_code"]=401
        response["error"]="Invalid token format"
        return 1
    fi
    
    # Validate JWT token
    if validate_jwt_token "$token" "$config"; then
        echo "JWT authentication successful"
        
        # Extract user information
        local user_info=$(extract_jwt_payload "$token")
        response["user"]="$user_info"
        response["authenticated"]=true
        
        return 0
    else
        echo "JWT authentication failed"
        response["status_code"]=401
        response["error"]="Invalid token"
        return 1
    fi
}

validate_jwt_token() {
    local token="$1"
    local config="$2"
    
    # Decode JWT token
    IFS='.' read -r header payload signature <<< "$token"
    
    if [[ -z "$header" ]] || [[ -z "$payload" ]] || [[ -z "$signature" ]]; then
        return 1
    fi
    
    # Decode payload
    local decoded_payload=$(echo "$payload" | tr '_-' '/+' | base64 -d 2>/dev/null)
    
    if [[ -z "$decoded_payload" ]]; then
        return 1
    fi
    
    # Check expiration
    local exp=$(echo "$decoded_payload" | jq -r '.exp // empty')
    if [[ -n "$exp" ]]; then
        local current_time=$(date +%s)
        if [[ "$current_time" -gt "$exp" ]]; then
            return 1
        fi
    fi
    
    # Verify signature if secret is provided
    local secret="${config[jwt_secret]}"
    if [[ -n "$secret" ]]; then
        # This is a simplified verification - in production, use proper JWT libraries
        local expected_signature=$(echo -n "$header.$payload" | openssl dgst -sha256 -hmac "$secret" | cut -d' ' -f2)
        if [[ "$signature" != "$expected_signature" ]]; then
            return 1
        fi
    fi
    
    return 0
}

extract_jwt_payload() {
    local token="$1"
    
    # Extract payload part
    local payload=$(echo "$token" | cut -d'.' -f2)
    
    # Decode and return
    echo "$payload" | tr '_-' '/+' | base64 -d 2>/dev/null
}

authenticate_api_key() {
    local api_key="$1"
    local config="$2"
    local response="$3"
    
    # Validate API key
    local valid_keys=(${config[valid_api_keys]})
    
    for key in "${valid_keys[@]}"; do
        if [[ "$api_key" == "$key" ]]; then
            echo "API key authentication successful"
            response["authenticated"]=true
            response["auth_method"]="api-key"
            return 0
        fi
    done
    
    echo "API key authentication failed"
    response["status_code"]=401
    response["error"]="Invalid API key"
    return 1
}

authenticate_basic() {
    local credentials="$1"
    local config="$2"
    local response="$3"
    
    # Decode base64 credentials
    local decoded_credentials=$(echo "$credentials" | base64 -d 2>/dev/null)
    
    if [[ -z "$decoded_credentials" ]]; then
        response["status_code"]=401
        response["error"]="Invalid credentials format"
        return 1
    fi
    
    # Extract username and password
    IFS=':' read -r username password <<< "$decoded_credentials"
    
    # Validate credentials
    if validate_basic_credentials "$username" "$password" "$config"; then
        echo "Basic authentication successful"
        response["authenticated"]=true
        response["user"]="$username"
        response["auth_method"]="basic"
        return 0
    else
        echo "Basic authentication failed"
        response["status_code"]=401
        response["error"]="Invalid credentials"
        return 1
    fi
}

validate_basic_credentials() {
    local username="$1"
    local password="$2"
    local config="$3"
    
    # This is a simplified validation - in production, use proper authentication
    local valid_users=(${config[valid_users]})
    
    for user in "${valid_users[@]}"; do
        IFS=':' read -r valid_username valid_password <<< "$user"
        if [[ "$username" == "$valid_username" ]] && [[ "$password" == "$valid_password" ]]; then
            return 0
        fi
    done
    
    return 1
}
```

### **CORS Middleware**
```bash
#!/bin/bash

# CORS middleware
cors_middleware() {
    local request="$1"
    local response="$2"
    local config="$3"
    
    echo "Executing CORS middleware..."
    
    # Extract request method and origin
    local method=$(echo "$request" | grep -i "^[A-Z]\+" | head -1 | awk '{print $1}')
    local origin=$(echo "$request" | grep -i "origin:" | cut -d' ' -f2-)
    
    # Handle preflight requests
    if [[ "$method" == "OPTIONS" ]]; then
        handle_cors_preflight "$origin" "$config" "$response"
        return 0
    fi
    
    # Add CORS headers to response
    add_cors_headers "$origin" "$config" "$response"
    
    return 0
}

handle_cors_preflight() {
    local origin="$1"
    local config="$2"
    local response="$3"
    
    # Check if origin is allowed
    if is_origin_allowed "$origin" "$config"; then
        response["status_code"]=200
        response["headers"]="Access-Control-Allow-Origin: $origin"
        response["headers"]+="\nAccess-Control-Allow-Methods: ${config[allowed_methods]:-GET, POST, PUT, DELETE, OPTIONS}"
        response["headers"]+="\nAccess-Control-Allow-Headers: ${config[allowed_headers]:-Content-Type, Authorization}"
        response["headers"]+="\nAccess-Control-Allow-Credentials: ${config[allow_credentials]:-true}"
        response["headers"]+="\nAccess-Control-Max-Age: ${config[max_age]:-86400}"
    else
        response["status_code"]=403
        response["error"]="Origin not allowed"
    fi
}

add_cors_headers() {
    local origin="$1"
    local config="$2"
    local response="$3"
    
    if is_origin_allowed "$origin" "$config"; then
        response["headers"]="Access-Control-Allow-Origin: $origin"
        response["headers"]+="\nAccess-Control-Allow-Credentials: ${config[allow_credentials]:-true}"
    fi
}

is_origin_allowed() {
    local origin="$1"
    local config="$2"
    
    local allowed_origins=(${config[allowed_origins]})
    
    # Check for wildcard
    if [[ " ${allowed_origins[*]} " =~ " * " ]]; then
        return 0
    fi
    
    # Check specific origins
    for allowed_origin in "${allowed_origins[@]}"; do
        if [[ "$origin" == "$allowed_origin" ]]; then
            return 0
        fi
    done
    
    return 1
}
```

### **Rate Limiting Middleware**
```bash
#!/bin/bash

# Rate limiting middleware
rate_limit_middleware() {
    local request="$1"
    local response="$2"
    local config="$3"
    
    echo "Executing rate limiting middleware..."
    
    # Get client identifier
    local client_id=$(get_client_id "$request")
    
    # Check rate limit
    if check_rate_limit "$client_id" "$config"; then
        echo "Rate limit check passed"
        return 0
    else
        echo "Rate limit exceeded"
        response["status_code"]=429
        response["error"]="Rate limit exceeded"
        response["headers"]="Retry-After: ${config[retry_after]:-60}"
        return 1
    fi
}

get_client_id() {
    local request="$1"
    
    # Try to get client IP
    local client_ip=$(echo "$request" | grep -i "x-forwarded-for:" | cut -d' ' -f2- | cut -d',' -f1)
    
    if [[ -z "$client_ip" ]]; then
        client_ip=$(echo "$request" | grep -i "x-real-ip:" | cut -d' ' -f2)
    fi
    
    if [[ -z "$client_ip" ]]; then
        # Fallback to user ID if authenticated
        client_ip="${response[user_id]:-unknown}"
    fi
    
    echo "$client_ip"
}

check_rate_limit() {
    local client_id="$1"
    local config="$2"
    
    # Parse rate limit configuration
    local limit="${config[rate_limit]:-100/min}"
    parse_rate_limit_config "$limit"
    
    # Get current request count
    local current_count=$(get_request_count "$client_id")
    
    if [[ "$current_count" -ge "$RATE_LIMIT" ]]; then
        return 1
    fi
    
    # Increment request count
    increment_request_count "$client_id"
    
    return 0
}

parse_rate_limit_config() {
    local rate_limit="$1"
    
    # Parse format: "100/min", "1000/hour", "10000/day"
    if [[ "$rate_limit" =~ ([0-9]+)/([a-z]+) ]]; then
        RATE_LIMIT="${BASH_REMATCH[1]}"
        local window="${BASH_REMATCH[2]}"
        
        case "$window" in
            "min") RATE_WINDOW=60 ;;
            "hour") RATE_WINDOW=3600 ;;
            "day") RATE_WINDOW=86400 ;;
            *) RATE_WINDOW=60 ;;
        esac
    else
        RATE_LIMIT=100
        RATE_WINDOW=60
    fi
}

get_request_count() {
    local client_id="$1"
    local cache_key="rate_limit:$client_id"
    
    # Get count from cache
    local count=$(get_cache_value "$cache_key")
    echo "${count:-0}"
}

increment_request_count() {
    local client_id="$1"
    local cache_key="rate_limit:$client_id"
    
    # Increment count in cache
    local current_count=$(get_request_count "$client_id")
    local new_count=$((current_count + 1))
    
    # Set with expiration
    set_cache_value "$cache_key" "$new_count" "$RATE_WINDOW"
}
```

### **Logging Middleware**
```bash
#!/bin/bash

# Logging middleware
logging_middleware() {
    local request="$1"
    local response="$2"
    local config="$3"
    
    echo "Executing logging middleware..."
    
    # Extract request information
    local method=$(echo "$request" | grep -i "^[A-Z]\+" | head -1 | awk '{print $1}')
    local path=$(echo "$request" | grep -i "^[A-Z]\+" | head -1 | awk '{print $2}')
    local user_agent=$(echo "$request" | grep -i "user-agent:" | cut -d' ' -f2-)
    local client_ip=$(get_client_ip "$request")
    
    # Create log entry
    local log_entry=$(create_log_entry "$method" "$path" "$user_agent" "$client_ip" "$response")
    
    # Write to log file
    write_log_entry "$log_entry" "$config"
    
    return 0
}

create_log_entry() {
    local method="$1"
    local path="$2"
    local user_agent="$3"
    local client_ip="$4"
    local response="$5"
    
    local timestamp=$(date '+%Y-%m-%d %H:%M:%S')
    local status_code="${response[status_code]:-200}"
    local response_time="${response[response_time]:-0}"
    
    # Create log line in common log format
    echo "$client_ip - - [$timestamp] \"$method $path HTTP/1.1\" $status_code $response_time \"$user_agent\""
}

write_log_entry() {
    local log_entry="$1"
    local config="$2"
    
    local log_file="${config[log_file]:-/var/log/tusk-middleware.log}"
    local log_format="${config[log_format]:-common}"
    
    case "$log_format" in
        "common")
            echo "$log_entry" >> "$log_file"
            ;;
        "json")
            echo "$(convert_to_json "$log_entry")" >> "$log_file"
            ;;
        "custom")
            echo "$(format_custom_log "$log_entry" "$config")" >> "$log_file"
            ;;
    esac
}

convert_to_json() {
    local log_entry="$1"
    
    # Parse common log format and convert to JSON
    # This is a simplified conversion
    echo "{\"log_entry\": \"$log_entry\", \"timestamp\": \"$(date -u +%Y-%m-%dT%H:%M:%SZ)\"}"
}

format_custom_log() {
    local log_entry="$1"
    local config="$2"
    
    local template="${config[log_template]:-\$timestamp \$method \$path \$status_code}"
    
    # Replace placeholders in template
    echo "$template" | sed "s/\$timestamp/$(date '+%Y-%m-%d %H:%M:%S')/g" \
                      | sed "s/\$method/$(echo "$log_entry" | awk '{print $6}' | tr -d '"')/g" \
                      | sed "s/\$path/$(echo "$log_entry" | awk '{print $7}')/g" \
                      | sed "s/\$status_code/$(echo "$log_entry" | awk '{print $9}')/g"
}
```

### **Caching Middleware**
```bash
#!/bin/bash

# Caching middleware
cache_middleware() {
    local request="$1"
    local response="$2"
    local config="$3"
    
    echo "Executing caching middleware..."
    
    # Check if request is cacheable
    if ! is_cacheable_request "$request" "$config"; then
        echo "Request not cacheable"
        return 0
    fi
    
    # Generate cache key
    local cache_key=$(generate_cache_key "$request" "$config")
    
    # Check if response is in cache
    local cached_response=$(get_cached_response "$cache_key" "$config")
    
    if [[ -n "$cached_response" ]]; then
        echo "Cache hit for key: $cache_key"
        response["cached"]=true
        response["cache_key"]="$cache_key"
        
        # Return cached response
        parse_cached_response "$cached_response" "$response"
        return 0
    else
        echo "Cache miss for key: $cache_key"
        response["cache_key"]="$cache_key"
        return 0
    fi
}

is_cacheable_request() {
    local request="$1"
    local config="$2"
    
    # Check HTTP method
    local method=$(echo "$request" | grep -i "^[A-Z]\+" | head -1 | awk '{print $1}')
    
    case "$method" in
        GET|HEAD)
            # Check for cache control headers
            local cache_control=$(echo "$request" | grep -i "cache-control:" | cut -d' ' -f2-)
            if [[ "$cache_control" =~ no-cache ]]; then
                return 1
            fi
            return 0
            ;;
        *)
            return 1
            ;;
    esac
}

generate_cache_key() {
    local request="$1"
    local config="$2"
    
    # Extract method and path
    local method=$(echo "$request" | grep -i "^[A-Z]\+" | head -1 | awk '{print $1}')
    local path=$(echo "$request" | grep -i "^[A-Z]\+" | head -1 | awk '{print $2}')
    
    # Include query parameters if present
    local query=$(echo "$path" | grep -o "?.*" || echo "")
    
    # Generate hash
    local key="$method:$path$query"
    echo "$key" | md5sum | cut -d' ' -f1
}

get_cached_response() {
    local cache_key="$1"
    local config="$2"
    
    local cache_backend="${config[cache_backend]:-file}"
    
    case "$cache_backend" in
        "redis")
            get_redis_cache "$cache_key"
            ;;
        "memcached")
            get_memcached_cache "$cache_key"
            ;;
        "file")
            get_file_cache "$cache_key" "$config"
            ;;
        *)
            return 1
            ;;
    esac
}

get_file_cache() {
    local cache_key="$1"
    local config="$2"
    
    local cache_dir="${config[cache_dir]:-/tmp/tusk-cache}"
    local cache_file="$cache_dir/$cache_key"
    
    if [[ -f "$cache_file" ]]; then
        # Check if cache is expired
        local expiry=$(head -1 "$cache_file")
        local current_time=$(date +%s)
        
        if [[ "$current_time" -lt "$expiry" ]]; then
            # Return cached content
            tail -n +2 "$cache_file"
        else
            # Remove expired cache
            rm -f "$cache_file"
        fi
    fi
}

set_cached_response() {
    local cache_key="$1"
    local response="$2"
    local config="$3"
    
    local cache_backend="${config[cache_backend]:-file}"
    local ttl="${config[cache_ttl]:-3600}"
    
    case "$cache_backend" in
        "redis")
            set_redis_cache "$cache_key" "$response" "$ttl"
            ;;
        "memcached")
            set_memcached_cache "$cache_key" "$response" "$ttl"
            ;;
        "file")
            set_file_cache "$cache_key" "$response" "$ttl" "$config"
            ;;
    esac
}

set_file_cache() {
    local cache_key="$1"
    local response="$2"
    local ttl="$3"
    local config="$4"
    
    local cache_dir="${config[cache_dir]:-/tmp/tusk-cache}"
    local cache_file="$cache_dir/$cache_key"
    
    # Create cache directory
    mkdir -p "$cache_dir"
    
    # Calculate expiry time
    local expiry=$(( $(date +%s) + ttl ))
    
    # Write cache file
    echo "$expiry" > "$cache_file"
    echo "$response" >> "$cache_file"
}
```

## 🎯 **Real-World Configuration Examples**

### **Complete Middleware Configuration**
```bash
# middleware-config.tsk
middleware_config:
  enabled: true
  timeout: 30
  logging: true
  debug: false
  error_handling: true
  metrics: true

#middleware: auth
#middleware-order: ["auth", "cors", "logging", "rate-limit", "cache"]
#middleware-enabled: true
#middleware-timeout: 30
#middleware-logging: true

#middleware-config:
#  auth:
#    method: jwt
#    jwt_secret: mysecretkey
#    jwt_algorithm: HS256
#  cors:
#    allowed_origins: ["https://app.example.com", "https://api.example.com"]
#    allowed_methods: "GET, POST, PUT, DELETE, OPTIONS"
#    allowed_headers: "Content-Type, Authorization"
#    allow_credentials: true
#    max_age: 86400
#  rate_limit:
#    rate_limit: "100/min"
#    retry_after: 60
#    burst_limit: 200
#  logging:
#    log_file: "/var/log/tusk-middleware.log"
#    log_format: "json"
#    log_level: "info"
#  cache:
#    cache_backend: "redis"
#    cache_ttl: 3600
#    cache_prefix: "tusk:"
```

### **API Gateway Middleware**
```bash
# api-gateway-middleware.tsk
gateway_config:
  name: "API Gateway"
  version: "1.0.0"
  environment: "production"

#middleware: auth
#middleware-chain: ["auth", "cors", "logging", "rate-limit", "cache", "compress"]
#middleware-enabled: true
#middleware-timeout: 60

#middleware-config:
#  auth:
#    method: jwt
#    jwt_secret: "${JWT_SECRET}"
#    jwt_algorithm: HS256
#    jwt_issuer: "api-gateway"
#    jwt_audience: "api-clients"
#  cors:
#    allowed_origins: ["https://app.example.com"]
#    allowed_methods: "GET, POST, PUT, DELETE, PATCH, OPTIONS"
#    allowed_headers: "Content-Type, Authorization, X-API-Key"
#    allow_credentials: true
#  rate_limit:
#    rate_limit: "1000/hour"
#    retry_after: 300
#    burst_limit: 100
#  logging:
#    log_file: "/var/log/api-gateway.log"
#    log_format: "json"
#    log_level: "info"
#    include_headers: true
#    include_body: false
#  cache:
#    cache_backend: "redis"
#    cache_ttl: 1800
#    cache_prefix: "api:"
#    cache_headers: ["ETag", "Last-Modified"]
#  compress:
#    enabled: true
#    min_size: 1024
#    algorithms: ["gzip", "deflate"]
```

### **Web Application Middleware**
```bash
# web-app-middleware.tsk
web_config:
  name: "Web Application"
  version: "2.0.0"
  environment: "development"

#middleware: session
#middleware-chain: ["session", "cors", "logging", "security", "compress"]
#middleware-enabled: true
#middleware-timeout: 30

#middleware-config:
#  session:
#    store: "redis"
#    secret: "${SESSION_SECRET}"
#    ttl: 3600
#    cookie_name: "session_id"
#    cookie_secure: true
#    cookie_http_only: true
#  cors:
#    allowed_origins: ["http://localhost:3000", "https://app.example.com"]
#    allowed_methods: "GET, POST, PUT, DELETE"
#    allowed_headers: "Content-Type, Authorization"
#    allow_credentials: true
#  logging:
#    log_file: "/var/log/web-app.log"
#    log_format: "common"
#    log_level: "debug"
#  security:
#    security_headers: true
#    hsts_max_age: 31536000
#    csp_policy: "default-src 'self'"
#    x_frame_options: "DENY"
#    x_content_type_options: "nosniff"
#  compress:
#    enabled: true
#    min_size: 512
#    algorithms: ["gzip"]
```

## 🚨 **Troubleshooting Middleware Directives**

### **Common Issues and Solutions**

**1. Middleware Not Executing**
```bash
# Debug middleware execution
debug_middleware_execution() {
    local request="$1"
    
    echo "Debugging middleware execution..."
    echo "Request: $request"
    echo "Middleware enabled: $MIDDLEWARE_ENABLED"
    echo "Middleware chain: ${middleware_chain[*]}"
    
    # Check middleware configuration
    if [[ "$MIDDLEWARE_ENABLED" != "true" ]]; then
        echo "Middleware is disabled"
        return 1
    fi
    
    # Check middleware chain
    if [[ ${#middleware_chain[@]} -eq 0 ]]; then
        echo "No middleware in chain"
        return 1
    fi
    
    # Test each middleware
    for middleware in "${middleware_chain[@]}"; do
        echo "Testing middleware: $middleware"
        test_middleware "$middleware" "$request"
    done
}

test_middleware() {
    local middleware="$1"
    local request="$2"
    
    case "$middleware" in
        "auth")
            test_auth_middleware "$request"
            ;;
        "cors")
            test_cors_middleware "$request"
            ;;
        "rate-limit")
            test_rate_limit_middleware "$request"
            ;;
        "logging")
            test_logging_middleware "$request"
            ;;
        "cache")
            test_cache_middleware "$request"
            ;;
        *)
            echo "Unknown middleware: $middleware"
            ;;
    esac
}
```

**2. Authentication Failures**
```bash
# Debug authentication middleware
debug_auth_middleware() {
    local request="$1"
    
    echo "Debugging authentication middleware..."
    
    # Check for authorization header
    local auth_header=$(echo "$request" | grep -i "authorization:")
    if [[ -n "$auth_header" ]]; then
        echo "Authorization header found: ${auth_header:0:50}..."
    else
        echo "No authorization header found"
    fi
    
    # Check JWT token if present
    if [[ "$auth_header" =~ Bearer ]]; then
        local token="${auth_header#Bearer }"
        echo "JWT token: ${token:0:20}..."
        
        # Decode JWT header
        IFS='.' read -r header payload signature <<< "$token"
        local decoded_header=$(echo "$header" | tr '_-' '/+' | base64 -d 2>/dev/null)
        echo "JWT header: $decoded_header"
    fi
    
    # Check configuration
    echo "Auth method: ${middleware_config[auth_method]}"
    echo "JWT secret configured: $([[ -n "${middleware_config[jwt_secret]}" ]] && echo "Yes" || echo "No")"
}
```

**3. CORS Issues**
```bash
# Debug CORS middleware
debug_cors_middleware() {
    local request="$1"
    
    echo "Debugging CORS middleware..."
    
    # Check origin
    local origin=$(echo "$request" | grep -i "origin:" | cut -d' ' -f2-)
    if [[ -n "$origin" ]]; then
        echo "Request origin: $origin"
    else
        echo "No origin header found"
    fi
    
    # Check allowed origins
    echo "Allowed origins: ${middleware_config[allowed_origins]}"
    
    # Check if origin is allowed
    if is_origin_allowed "$origin" "$middleware_config"; then
        echo "✓ Origin is allowed"
    else
        echo "✗ Origin is not allowed"
    fi
    
    # Check method
    local method=$(echo "$request" | grep -i "^[A-Z]\+" | head -1 | awk '{print $1}')
    echo "Request method: $method"
    echo "Allowed methods: ${middleware_config[allowed_methods]}"
}
```

## 🔒 **Security Best Practices**

### **Middleware Security Checklist**
```bash
# Security validation
validate_middleware_security() {
    echo "Validating middleware security configuration..."
    
    # Check authentication configuration
    if [[ -n "${middleware_config[auth_method]}" ]]; then
        echo "✓ Authentication middleware configured"
        
        case "${middleware_config[auth_method]}" in
            "jwt")
                if [[ -n "${middleware_config[jwt_secret]}" ]]; then
                    echo "✓ JWT secret configured"
                else
                    echo "⚠ JWT secret not configured"
                fi
                ;;
            "api-key")
                if [[ -n "${middleware_config[valid_api_keys]}" ]]; then
                    echo "✓ API keys configured"
                else
                    echo "⚠ API keys not configured"
                fi
                ;;
        esac
    else
        echo "⚠ No authentication middleware configured"
    fi
    
    # Check CORS configuration
    if [[ -n "${middleware_config[allowed_origins]}" ]]; then
        echo "✓ CORS origins configured"
        
        # Check for wildcard
        if [[ " ${middleware_config[allowed_origins]} " =~ " * " ]]; then
            echo "⚠ CORS allows all origins (*) - consider restricting"
        fi
    else
        echo "⚠ No CORS configuration"
    fi
    
    # Check rate limiting
    if [[ -n "${middleware_config[rate_limit]}" ]]; then
        echo "✓ Rate limiting configured: ${middleware_config[rate_limit]}"
    else
        echo "⚠ No rate limiting configured"
    fi
    
    # Check security headers
    if [[ "${middleware_config[security_headers]}" == "true" ]]; then
        echo "✓ Security headers enabled"
    else
        echo "⚠ Security headers not enabled"
    fi
}
```

## 📈 **Performance Optimization Tips**

### **Middleware Performance Checklist**
```bash
# Performance validation
validate_middleware_performance() {
    echo "Validating middleware performance configuration..."
    
    # Check caching
    if [[ -n "${middleware_config[cache_backend]}" ]]; then
        echo "✓ Caching enabled: ${middleware_config[cache_backend]}"
        echo "  TTL: ${middleware_config[cache_ttl]:-3600}s"
    else
        echo "⚠ Caching not configured"
    fi
    
    # Check compression
    if [[ "${middleware_config[compress_enabled]}" == "true" ]]; then
        echo "✓ Compression enabled"
        echo "  Min size: ${middleware_config[compress_min_size]:-1024} bytes"
    else
        echo "⚠ Compression not enabled"
    fi
    
    # Check timeout
    if [[ -n "${middleware_config[timeout]}" ]]; then
        echo "✓ Timeout configured: ${middleware_config[timeout]}s"
    else
        echo "⚠ No timeout configured"
    fi
    
    # Check logging level
    if [[ "${middleware_config[log_level]}" == "debug" ]]; then
        echo "⚠ Debug logging enabled - may impact performance"
    else
        echo "✓ Appropriate logging level: ${middleware_config[log_level]:-info}"
    fi
}
```

## 🎯 **Next Steps**

- **Auth Directives**: Learn about authentication-specific directives
- **Plugin Integration**: Explore middleware plugins
- **Advanced Patterns**: Understand complex middleware patterns
- **Testing Middleware**: Test middleware functionality
- **Performance Tuning**: Optimize middleware performance

---

**Middleware directives transform your TuskLang configuration into a powerful request processing pipeline. They bring modern middleware capabilities to your Bash applications with intelligent request handling, security, and performance optimization!** 