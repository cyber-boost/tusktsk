# API Directives in TuskLang - Bash Guide

## 🔌 **Revolutionary API Configuration**

API directives in TuskLang transform your configuration files into intelligent API gateways. No more separate API documentation or complex routing configurations - everything lives in your TuskLang configuration with dynamic endpoints, automatic validation, and intelligent response handling.

> **"We don't bow to any king"** - TuskLang API directives break free from traditional API framework constraints and bring modern API capabilities to your Bash applications.

## 🚀 **Core API Directives**

### **Basic API Setup**
```bash
#api: /api/v1                 # API base path
#api-version: 1.0             # API version
#api-docs: /docs              # API documentation endpoint
#api-spec: openapi            # API specification format
#api-auth: jwt                # API authentication method
#api-rate-limit: 100/min      # API rate limiting
```

### **Advanced API Configuration**
```bash
#api-cors: "*"                # API CORS policy
#api-timeout: 30              # API request timeout
#api-cache: redis             # API response caching
#api-logging: true            # API request logging
#api-metrics: true            # API metrics collection
#api-validation: true         # Request/response validation
```

## 🔧 **Bash API Gateway Implementation**

### **Basic API Router**
```bash
#!/bin/bash

# Load API configuration
source <(tsk load api.tsk)

# API configuration
API_BASE_PATH="${api_base_path:-/api/v1}"
API_VERSION="${api_version:-1.0}"
API_AUTH_METHOD="${api_auth_method:-jwt}"
API_RATE_LIMIT="${api_rate_limit:-100/min}"
API_TIMEOUT="${api_timeout:-30}"

# API router
route_api_request() {
    local method="$1"
    local path="$2"
    local headers="$3"
    local body="$4"
    
    echo "Routing API request: $method $path"
    
    # Remove API base path from request path
    local api_path="${path#$API_BASE_PATH}"
    
    # Apply API middleware
    apply_api_middleware "$method" "$api_path" "$headers" "$body"
    
    # Route to appropriate handler
    case "$api_path" in
        "/users"|"/users/")
            handle_users_api "$method" "$api_path" "$body"
            ;;
        "/users/*")
            handle_user_api "$method" "$api_path" "$body"
            ;;
        "/posts"|"/posts/")
            handle_posts_api "$method" "$api_path" "$body"
            ;;
        "/posts/*")
            handle_post_api "$method" "$api_path" "$body"
            ;;
        "/health")
            handle_health_api
            ;;
        "/metrics")
            handle_metrics_api
            ;;
        "/docs")
            handle_docs_api
            ;;
        *)
            handle_404_api
            ;;
    esac
}

# API middleware
apply_api_middleware() {
    local method="$1"
    local path="$2"
    local headers="$3"
    local body="$4"
    
    # Apply authentication
    if ! authenticate_api_request "$headers"; then
        handle_401_api "Authentication required"
        return 1
    fi
    
    # Apply rate limiting
    if ! check_api_rate_limit "$headers"; then
        handle_429_api "Rate limit exceeded"
        return 1
    fi
    
    # Apply request validation
    if ! validate_api_request "$method" "$path" "$body"; then
        handle_400_api "Invalid request"
        return 1
    fi
    
    # Log API request
    log_api_request "$method" "$path" "$headers"
    
    return 0
}
```

### **Authentication Middleware**
```bash
#!/bin/bash

# Load authentication configuration
source <(tsk load auth.tsk)

# API authentication
authenticate_api_request() {
    local headers="$1"
    
    case "$API_AUTH_METHOD" in
        "jwt")
            authenticate_jwt "$headers"
            ;;
        "api-key")
            authenticate_api_key "$headers"
            ;;
        "oauth")
            authenticate_oauth "$headers"
            ;;
        "none")
            return 0  # No authentication required
            ;;
        *)
            echo "Unknown authentication method: $API_AUTH_METHOD" >&2
            return 1
            ;;
    esac
}

authenticate_jwt() {
    local headers="$1"
    
    # Extract Authorization header
    local auth_header=$(echo "$headers" | grep -i "authorization:" | cut -d' ' -f2-)
    
    if [[ -z "$auth_header" ]]; then
        echo "No Authorization header found" >&2
        return 1
    fi
    
    # Remove "Bearer " prefix
    local token="${auth_header#Bearer }"
    
    if [[ -z "$token" ]]; then
        echo "No JWT token found" >&2
        return 1
    fi
    
    # Validate JWT token
    if validate_jwt_token "$token"; then
        echo "JWT authentication successful"
        return 0
    else
        echo "JWT authentication failed" >&2
        return 1
    fi
}

validate_jwt_token() {
    local token="$1"
    
    # Decode JWT token (header.payload.signature)
    IFS='.' read -r header payload signature <<< "$token"
    
    if [[ -z "$header" ]] || [[ -z "$payload" ]] || [[ -z "$signature" ]]; then
        echo "Invalid JWT format" >&2
        return 1
    fi
    
    # Decode payload (base64url)
    local decoded_payload=$(echo "$payload" | tr '_-' '/+' | base64 -d 2>/dev/null)
    
    if [[ -z "$decoded_payload" ]]; then
        echo "Invalid JWT payload" >&2
        return 1
    fi
    
    # Check expiration
    local exp=$(echo "$decoded_payload" | jq -r '.exp // empty')
    if [[ -n "$exp" ]]; then
        local current_time=$(date +%s)
        if [[ "$current_time" -gt "$exp" ]]; then
            echo "JWT token expired" >&2
            return 1
        fi
    fi
    
    # Extract user information
    export API_USER_ID=$(echo "$decoded_payload" | jq -r '.sub // empty')
    export API_USER_ROLE=$(echo "$decoded_payload" | jq -r '.role // empty')
    
    echo "JWT validation successful for user: $API_USER_ID"
    return 0
}

authenticate_api_key() {
    local headers="$1"
    
    # Extract API key from headers
    local api_key=$(echo "$headers" | grep -i "x-api-key:" | cut -d' ' -f2-)
    
    if [[ -z "$api_key" ]]; then
        echo "No API key found" >&2
        return 1
    fi
    
    # Validate API key against database or configuration
    if validate_api_key "$api_key"; then
        echo "API key authentication successful"
        return 0
    else
        echo "API key authentication failed" >&2
        return 1
    fi
}

validate_api_key() {
    local api_key="$1"
    
    # Check against configured API keys
    local valid_keys=(${valid_api_keys[@]})
    
    for key in "${valid_keys[@]}"; do
        if [[ "$api_key" == "$key" ]]; then
            return 0
        fi
    done
    
    echo "Invalid API key: $api_key" >&2
    return 1
}
```

### **Rate Limiting Implementation**
```bash
#!/bin/bash

# Load rate limiting configuration
source <(tsk load rate-limit.tsk)

# API rate limiting
check_api_rate_limit() {
    local headers="$1"
    
    # Parse rate limit configuration
    parse_rate_limit_config "$API_RATE_LIMIT"
    
    # Get client identifier
    local client_id=$(get_client_id "$headers")
    
    # Check current rate
    local current_count=$(get_api_request_count "$client_id")
    
    if [[ "$current_count" -ge "$RATE_LIMIT" ]]; then
        echo "Rate limit exceeded for client: $client_id" >&2
        return 1
    fi
    
    # Increment request count
    increment_api_request_count "$client_id"
    
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
        
        echo "Rate limit: $RATE_LIMIT requests per $window ($RATE_WINDOW seconds)"
    else
        echo "Invalid rate limit format: $rate_limit" >&2
        return 1
    fi
}

get_client_id() {
    local headers="$1"
    
    # Try to get client IP
    local client_ip=$(echo "$headers" | grep -i "x-forwarded-for:" | cut -d' ' -f2- | cut -d',' -f1)
    
    if [[ -z "$client_ip" ]]; then
        client_ip=$(echo "$headers" | grep -i "x-real-ip:" | cut -d' ' -f2-)
    fi
    
    if [[ -z "$client_ip" ]]; then
        # Fallback to user ID if authenticated
        client_ip="${API_USER_ID:-unknown}"
    fi
    
    echo "$client_ip"
}

get_api_request_count() {
    local client_id="$1"
    local cache_key="api_rate_limit:$client_id"
    
    # Get count from cache (Redis, file, or memory)
    local count=$(get_cache_value "$cache_key")
    echo "${count:-0}"
}

increment_api_request_count() {
    local client_id="$1"
    local cache_key="api_rate_limit:$client_id"
    
    # Increment count in cache
    local current_count=$(get_api_request_count "$client_id")
    local new_count=$((current_count + 1))
    
    # Set with expiration
    set_cache_value "$cache_key" "$new_count" "$RATE_WINDOW"
}
```

### **Request Validation**
```bash
#!/bin/bash

# Load validation configuration
source <(tsk load validation.tsk)

# API request validation
validate_api_request() {
    local method="$1"
    local path="$2"
    local body="$3"
    
    # Validate HTTP method
    if ! validate_http_method "$method"; then
        echo "Invalid HTTP method: $method" >&2
        return 1
    fi
    
    # Validate path format
    if ! validate_api_path "$path"; then
        echo "Invalid API path: $path" >&2
        return 1
    fi
    
    # Validate request body
    if ! validate_request_body "$method" "$path" "$body"; then
        echo "Invalid request body" >&2
        return 1
    fi
    
    return 0
}

validate_http_method() {
    local method="$1"
    
    case "$method" in
        GET|POST|PUT|DELETE|PATCH|OPTIONS)
            return 0
            ;;
        *)
            return 1
            ;;
    esac
}

validate_api_path() {
    local path="$1"
    
    # Check for valid characters
    if [[ ! "$path" =~ ^/[a-zA-Z0-9/_-]*$ ]]; then
        return 1
    fi
    
    # Check for double slashes
    if [[ "$path" =~ // ]]; then
        return 1
    fi
    
    return 0
}

validate_request_body() {
    local method="$1"
    local path="$2"
    local body="$3"
    
    # Only validate body for methods that typically have one
    case "$method" in
        POST|PUT|PATCH)
            if [[ -n "$body" ]]; then
                # Validate JSON format
                if ! echo "$body" | jq . >/dev/null 2>&1; then
                    echo "Invalid JSON format in request body" >&2
                    return 1
                fi
                
                # Validate against schema if available
                validate_against_schema "$path" "$body"
            fi
            ;;
    esac
    
    return 0
}

validate_against_schema() {
    local path="$1"
    local body="$2"
    
    # Get schema for this endpoint
    local schema=$(get_api_schema "$path")
    
    if [[ -n "$schema" ]]; then
        # Validate JSON against schema
        if ! echo "$body" | jq -e "$schema" >/dev/null 2>&1; then
            echo "Request body does not match schema" >&2
            return 1
        fi
    fi
    
    return 0
}
```

## 📊 **API Response Handling**

### **Standardized API Responses**
```bash
#!/bin/bash

# API response handlers
handle_api_success() {
    local data="$1"
    local message="${2:-Success}"
    local status_code="${3:-200}"
    
    local response=$(cat << EOF
{
    "success": true,
    "message": "$message",
    "data": $data,
    "timestamp": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
    "api_version": "$API_VERSION"
}
EOF
)
    
    send_api_response "$status_code" "$response"
}

handle_api_error() {
    local message="$1"
    local status_code="${2:-500}"
    local error_code="${3:-INTERNAL_ERROR}"
    
    local response=$(cat << EOF
{
    "success": false,
    "error": {
        "code": "$error_code",
        "message": "$message"
    },
    "timestamp": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
    "api_version": "$API_VERSION"
}
EOF
)
    
    send_api_response "$status_code" "$response"
}

send_api_response() {
    local status_code="$1"
    local response="$2"
    
    # Get status text
    local status_text=$(get_status_text "$status_code")
    
    # Add response headers
    echo "HTTP/1.1 $status_code $status_text"
    echo "Content-Type: application/json"
    echo "Content-Length: ${#response}"
    echo "X-API-Version: $API_VERSION"
    echo "X-Request-ID: $(generate_request_id)"
    echo ""
    echo "$response"
}

get_status_text() {
    local status_code="$1"
    
    case "$status_code" in
        200) echo "OK" ;;
        201) echo "Created" ;;
        400) echo "Bad Request" ;;
        401) echo "Unauthorized" ;;
        403) echo "Forbidden" ;;
        404) echo "Not Found" ;;
        429) echo "Too Many Requests" ;;
        500) echo "Internal Server Error" ;;
        *) echo "Unknown" ;;
    esac
}

generate_request_id() {
    echo "$(date +%s)-$(openssl rand -hex 8)"
}
```

### **Specific API Endpoint Handlers**
```bash
#!/bin/bash

# Users API handlers
handle_users_api() {
    local method="$1"
    local path="$2"
    local body="$3"
    
    case "$method" in
        "GET")
            get_users_list
            ;;
        "POST")
            create_user "$body"
            ;;
        *)
            handle_405_api "Method not allowed"
            ;;
    esac
}

get_users_list() {
    # Get users from database
    local users=$(query_database "SELECT id, name, email, created_at FROM users ORDER BY created_at DESC")
    
    if [[ $? -eq 0 ]]; then
        handle_api_success "$users" "Users retrieved successfully"
    else
        handle_api_error "Failed to retrieve users" 500 "DATABASE_ERROR"
    fi
}

create_user() {
    local body="$1"
    
    # Extract user data
    local name=$(echo "$body" | jq -r '.name // empty')
    local email=$(echo "$body" | jq -r '.email // empty')
    local password=$(echo "$body" | jq -r '.password // empty')
    
    # Validate required fields
    if [[ -z "$name" ]] || [[ -z "$email" ]] || [[ -z "$password" ]]; then
        handle_api_error "Missing required fields: name, email, password" 400 "VALIDATION_ERROR"
        return
    fi
    
    # Hash password
    local hashed_password=$(echo -n "$password" | sha256sum | cut -d' ' -f1)
    
    # Insert user into database
    local user_id=$(query_database "INSERT INTO users (name, email, password_hash) VALUES ('$name', '$email', '$hashed_password') RETURNING id")
    
    if [[ $? -eq 0 ]] && [[ -n "$user_id" ]]; then
        local new_user=$(query_database "SELECT id, name, email, created_at FROM users WHERE id = $user_id")
        handle_api_success "$new_user" "User created successfully" 201
    else
        handle_api_error "Failed to create user" 500 "DATABASE_ERROR"
    fi
}

# Posts API handlers
handle_posts_api() {
    local method="$1"
    local path="$2"
    local body="$3"
    
    case "$method" in
        "GET")
            get_posts_list
            ;;
        "POST")
            create_post "$body"
            ;;
        *)
            handle_405_api "Method not allowed"
            ;;
    esac
}

get_posts_list() {
    # Get posts from database
    local posts=$(query_database "SELECT id, title, content, author_id, created_at FROM posts ORDER BY created_at DESC")
    
    if [[ $? -eq 0 ]]; then
        handle_api_success "$posts" "Posts retrieved successfully"
    else
        handle_api_error "Failed to retrieve posts" 500 "DATABASE_ERROR"
    fi
}

create_post() {
    local body="$1"
    
    # Extract post data
    local title=$(echo "$body" | jq -r '.title // empty')
    local content=$(echo "$body" | jq -r '.content // empty')
    
    # Validate required fields
    if [[ -z "$title" ]] || [[ -z "$content" ]]; then
        handle_api_error "Missing required fields: title, content" 400 "VALIDATION_ERROR"
        return
    fi
    
    # Insert post into database
    local post_id=$(query_database "INSERT INTO posts (title, content, author_id) VALUES ('$title', '$content', $API_USER_ID) RETURNING id")
    
    if [[ $? -eq 0 ]] && [[ -n "$post_id" ]]; then
        local new_post=$(query_database "SELECT id, title, content, author_id, created_at FROM posts WHERE id = $post_id")
        handle_api_success "$new_post" "Post created successfully" 201
    else
        handle_api_error "Failed to create post" 500 "DATABASE_ERROR"
    fi
}
```

## 📈 **API Metrics and Monitoring**

### **API Metrics Collection**
```bash
#!/bin/bash

# Load metrics configuration
source <(tsk load metrics.tsk)

# API metrics
initialize_api_metrics() {
    # Request counters
    declare -gA API_REQUEST_COUNTERS
    API_REQUEST_COUNTERS["total"]=0
    API_REQUEST_COUNTERS["200"]=0
    API_REQUEST_COUNTERS["400"]=0
    API_REQUEST_COUNTERS["401"]=0
    API_REQUEST_COUNTERS["404"]=0
    API_REQUEST_COUNTERS["429"]=0
    API_REQUEST_COUNTERS["500"]=0
    
    # Response time tracking
    declare -gA API_RESPONSE_TIMES
    API_RESPONSE_TIMES["total"]=0
    API_RESPONSE_TIMES["count"]=0
    
    # Endpoint usage tracking
    declare -gA API_ENDPOINT_USAGE
    
    echo "API metrics initialized"
}

record_api_metric() {
    local endpoint="$1"
    local method="$2"
    local status_code="$3"
    local response_time="$4"
    
    # Increment counters
    API_REQUEST_COUNTERS["total"]=$((API_REQUEST_COUNTERS["total"] + 1))
    API_REQUEST_COUNTERS["$status_code"]=$((API_REQUEST_COUNTERS["$status_code"] + 1))
    
    # Track response time
    API_RESPONSE_TIMES["total"]=$((API_RESPONSE_TIMES["total"] + response_time))
    API_RESPONSE_TIMES["count"]=$((API_RESPONSE_TIMES["count"] + 1))
    
    # Track endpoint usage
    local endpoint_key="${method}:${endpoint}"
    API_ENDPOINT_USAGE["$endpoint_key"]=$((API_ENDPOINT_USAGE["$endpoint_key"] + 1))
    
    # Log API request
    echo "$(date '+%Y-%m-%d %H:%M:%S') API $method $endpoint $status_code ${response_time}ms"
}

get_api_metrics() {
    local avg_response_time=0
    if [[ "${API_RESPONSE_TIMES[count]}" -gt 0 ]]; then
        avg_response_time=$((API_RESPONSE_TIMES["total"] / API_RESPONSE_TIMES["count"]))
    fi
    
    cat << EOF
# HELP api_requests_total Total number of API requests
# TYPE api_requests_total counter
api_requests_total{status="all"} ${API_REQUEST_COUNTERS["total"]}
api_requests_total{status="200"} ${API_REQUEST_COUNTERS["200"]}
api_requests_total{status="400"} ${API_REQUEST_COUNTERS["400"]}
api_requests_total{status="401"} ${API_REQUEST_COUNTERS["401"]}
api_requests_total{status="404"} ${API_REQUEST_COUNTERS["404"]}
api_requests_total{status="429"} ${API_REQUEST_COUNTERS["429"]}
api_requests_total{status="500"} ${API_REQUEST_COUNTERS["500"]}

# HELP api_response_time_average Average API response time in milliseconds
# TYPE api_response_time_average gauge
api_response_time_average $avg_response_time

# HELP api_endpoint_usage_total Total usage by endpoint
# TYPE api_endpoint_usage_total counter
EOF
    
    # Add endpoint usage metrics
    for endpoint in "${!API_ENDPOINT_USAGE[@]}"; do
        local count="${API_ENDPOINT_USAGE[$endpoint]}"
        echo "api_endpoint_usage_total{endpoint=\"$endpoint\"} $count"
    done
}
```

## 🎯 **Real-World Configuration Examples**

### **Complete API Configuration**
```bash
# api-config.tsk
api_name: "TuskLang API"
api_version: "1.0.0"
api_description: "RESTful API for TuskLang applications"

#api: /api/v1
#api-version: 1.0
#api-docs: /docs
#api-spec: openapi
#api-auth: jwt
#api-rate-limit: 100/min

#api-cors: "https://app.example.com"
#api-timeout: 30
#api-cache: redis
#api-logging: true
#api-metrics: true
#api-validation: true

#api-endpoints:
#  - path: /users
#    methods: [GET, POST]
#    auth: required
#    rate-limit: 50/min
#  - path: /users/{id}
#    methods: [GET, PUT, DELETE]
#    auth: required
#    rate-limit: 100/min
#  - path: /posts
#    methods: [GET, POST]
#    auth: required
#    rate-limit: 200/min
#  - path: /posts/{id}
#    methods: [GET, PUT, DELETE]
#    auth: required
#    rate-limit: 100/min
#  - path: /health
#    methods: [GET]
#    auth: none
#    rate-limit: 1000/min
#  - path: /metrics
#    methods: [GET]
#    auth: api-key
#    rate-limit: 10/min
```

### **Microservice API Configuration**
```bash
# user-service-api.tsk
service_name: "User Service API"
service_version: "2.0.0"

#api: /api/users
#api-version: 2.0
#api-auth: jwt
#api-rate-limit: 200/min

#api-cors: "*"
#api-timeout: 60
#api-cache: redis
#api-logging: true
#api-metrics: true

#api-endpoints:
#  - path: /
#    methods: [GET, POST]
#    auth: required
#    validation: user-schema
#  - path: /{id}
#    methods: [GET, PUT, DELETE]
#    auth: required
#    validation: user-update-schema
#  - path: /{id}/profile
#    methods: [GET, PUT]
#    auth: required
#    validation: profile-schema
#  - path: /search
#    methods: [GET]
#    auth: required
#    validation: search-schema
```

## 🚨 **Troubleshooting API Directives**

### **Common Issues and Solutions**

**1. Authentication Failures**
```bash
# Debug authentication issues
debug_authentication() {
    local headers="$1"
    
    echo "Debugging authentication..."
    echo "Headers: $headers"
    echo "Auth method: $API_AUTH_METHOD"
    
    case "$API_AUTH_METHOD" in
        "jwt")
            local auth_header=$(echo "$headers" | grep -i "authorization:")
            echo "Auth header: $auth_header"
            
            if [[ -n "$auth_header" ]]; then
                local token="${auth_header#Bearer }"
                echo "Token: ${token:0:20}..."
                
                # Decode JWT header
                IFS='.' read -r header payload signature <<< "$token"
                local decoded_header=$(echo "$header" | tr '_-' '/+' | base64 -d 2>/dev/null)
                echo "JWT header: $decoded_header"
            fi
            ;;
        "api-key")
            local api_key=$(echo "$headers" | grep -i "x-api-key:" | cut -d' ' -f2-)
            echo "API key: ${api_key:0:10}..."
            ;;
    esac
}
```

**2. Rate Limiting Issues**
```bash
# Debug rate limiting
debug_rate_limiting() {
    local client_id="$1"
    
    echo "Debugging rate limiting..."
    echo "Client ID: $client_id"
    echo "Rate limit: $API_RATE_LIMIT"
    
    local current_count=$(get_api_request_count "$client_id")
    echo "Current count: $current_count"
    
    local cache_key="api_rate_limit:$client_id"
    echo "Cache key: $cache_key"
    
    # Check cache directly
    local cached_value=$(get_cache_value "$cache_key")
    echo "Cached value: $cached_value"
}
```

**3. Validation Errors**
```bash
# Debug validation issues
debug_validation() {
    local method="$1"
    local path="$2"
    local body="$3"
    
    echo "Debugging validation..."
    echo "Method: $method"
    echo "Path: $path"
    echo "Body: $body"
    
    # Validate JSON format
    if [[ -n "$body" ]]; then
        if echo "$body" | jq . >/dev/null 2>&1; then
            echo "JSON format: Valid"
        else
            echo "JSON format: Invalid"
            echo "JSON error: $(echo "$body" | jq . 2>&1)"
        fi
    fi
    
    # Check path format
    if [[ "$path" =~ ^/[a-zA-Z0-9/_-]*$ ]]; then
        echo "Path format: Valid"
    else
        echo "Path format: Invalid"
    fi
}
```

## 🔒 **Security Best Practices**

### **API Security Checklist**
```bash
# Security validation
validate_api_security() {
    echo "Validating API security configuration..."
    
    # Check authentication
    if [[ "$API_AUTH_METHOD" != "none" ]]; then
        echo "✓ Authentication enabled: $API_AUTH_METHOD"
    else
        echo "⚠ No authentication configured"
    fi
    
    # Check rate limiting
    if [[ -n "$API_RATE_LIMIT" ]]; then
        echo "✓ Rate limiting configured: $API_RATE_LIMIT"
    else
        echo "⚠ Rate limiting not configured"
    fi
    
    # Check CORS policy
    if [[ "$api_cors" != "*" ]]; then
        echo "✓ CORS restricted: $api_cors"
    else
        echo "⚠ CORS set to '*' - consider restricting"
    fi
    
    # Check request validation
    if [[ "$api_validation" == "true" ]]; then
        echo "✓ Request validation enabled"
    else
        echo "⚠ Request validation not enabled"
    fi
    
    # Check HTTPS
    if [[ "$ssl_enabled" == "true" ]]; then
        echo "✓ HTTPS enabled"
    else
        echo "⚠ HTTPS not enabled - consider for production"
    fi
}
```

## 📈 **Performance Optimization Tips**

### **API Performance Checklist**
```bash
# Performance validation
validate_api_performance() {
    echo "Validating API performance configuration..."
    
    # Check caching
    if [[ -n "$api_cache" ]]; then
        echo "✓ Response caching enabled: $api_cache"
    else
        echo "⚠ Response caching not configured"
    fi
    
    # Check timeout
    if [[ -n "$api_timeout" ]]; then
        echo "✓ Request timeout configured: ${api_timeout}s"
    else
        echo "⚠ Request timeout not configured"
    fi
    
    # Check metrics
    if [[ "$api_metrics" == "true" ]]; then
        echo "✓ Metrics collection enabled"
    else
        echo "⚠ Metrics collection not enabled"
    fi
    
    # Check logging
    if [[ "$api_logging" == "true" ]]; then
        echo "✓ Request logging enabled"
    else
        echo "⚠ Request logging not enabled"
    fi
}
```

## 🎯 **Next Steps**

- **CLI Directives**: Learn about CLI-specific directives
- **Middleware Integration**: Explore API middleware
- **Plugin System**: Understand API plugins
- **Testing API Directives**: Test API functionality
- **Performance Tuning**: Optimize API performance

---

**API directives transform your TuskLang configuration into a powerful API gateway. They bring modern API capabilities to your Bash applications with intelligent routing, authentication, validation, and monitoring!** 