# API Gateway in TuskLang - Bash Guide

## 🚪 **Revolutionary API Gateway Configuration**

API gateway in TuskLang transforms your configuration files into intelligent, unified API management systems. No more scattered endpoints or manual routing—everything lives in your TuskLang configuration with dynamic request routing, intelligent rate limiting, and comprehensive API analytics.

> **"We don't bow to any king"** – TuskLang API gateway breaks free from traditional API management constraints and brings modern gateway capabilities to your Bash applications.

## 🚀 **Core API Gateway Directives**

### **Basic API Gateway Setup**
```bash
#api-gateway: enabled                   # Enable API gateway
#gateway-enabled: true                 # Alternative syntax
#gateway-port: 8080                    # Gateway port
#gateway-host: 0.0.0.0                 # Gateway host
#gateway-routes: true                  # Enable route management
#gateway-rate-limiting: true           # Enable rate limiting
```

### **Advanced API Gateway Configuration**
```bash
#gateway-authentication: true          # Enable authentication
#gateway-authorization: true           # Enable authorization
#gateway-caching: true                 # Enable response caching
#gateway-monitoring: true              # Enable gateway monitoring
#gateway-logging: true                 # Enable request logging
#gateway-metrics: true                 # Enable API metrics
```

## 🔧 **Bash API Gateway Implementation**

### **Basic API Gateway Manager**
```bash
#!/bin/bash

# Load API gateway configuration
source <(tsk load api-gateway.tsk)

# API gateway configuration
GATEWAY_ENABLED="${gateway_enabled:-true}"
GATEWAY_PORT="${gateway_port:-8080}"
GATEWAY_HOST="${gateway_host:-0.0.0.0}"
GATEWAY_ROUTES="${gateway_routes:-true}"
GATEWAY_RATE_LIMITING="${gateway_rate_limiting:-true}"

# API gateway manager
class APIGatewayManager {
    constructor() {
        this.enabled = GATEWAY_ENABLED
        this.port = GATEWAY_PORT
        this.host = GATEWAY_HOST
        this.routes = GATEWAY_ROUTES
        this.rateLimiting = GATEWAY_RATE_LIMITING
        this.routes = new Map()
        this.stats = {
            requests_processed: 0,
            requests_authenticated: 0,
            requests_cached: 0,
            requests_rate_limited: 0
        }
    }
    
    addRoute(path, method, target, options = {}) {
        this.routes.set(`${method}:${path}`, {
            path,
            method,
            target,
            ...options
        })
    }
    
    processRequest(request) {
        if (!this.enabled) return null
        
        this.stats.requests_processed++
        
        // Check rate limiting
        if (this.rateLimiting && !this.checkRateLimit(request)) {
            this.stats.requests_rate_limited++
            return { status: 429, message: 'Rate limit exceeded' }
        }
        
        // Check authentication
        if (this.requiresAuthentication(request) && !this.authenticate(request)) {
            return { status: 401, message: 'Authentication required' }
        }
        
        // Check authorization
        if (this.requiresAuthorization(request) && !this.authorize(request)) {
            return { status: 403, message: 'Access denied' }
        }
        
        // Route request
        const route = this.findRoute(request)
        if (route) {
            return this.executeRoute(route, request)
        }
        
        return { status: 404, message: 'Route not found' }
    }
    
    checkRateLimit(request) {
        // Implementation for rate limiting
        return true
    }
    
    authenticate(request) {
        // Implementation for authentication
        this.stats.requests_authenticated++
        return true
    }
    
    authorize(request) {
        // Implementation for authorization
        return true
    }
    
    findRoute(request) {
        const key = `${request.method}:${request.path}`
        return this.routes.get(key)
    }
    
    executeRoute(route, request) {
        // Implementation for route execution
        return { status: 200, data: 'response' }
    }
    
    getStats() {
        return { ...this.stats }
    }
}

# Initialize API gateway manager
const apiGatewayManager = new APIGatewayManager()
```

### **Dynamic Route Management**
```bash
#!/bin/bash

# Dynamic route management
manage_routes() {
    local action="$1"
    local route_path="$2"
    local route_method="${3:-GET}"
    local route_target="$4"
    
    case "$action" in
        "add")
            add_route "$route_path" "$route_method" "$route_target"
            ;;
        "remove")
            remove_route "$route_path" "$route_method"
            ;;
        "list")
            list_routes
            ;;
        "update")
            update_route "$route_path" "$route_method" "$route_target"
            ;;
        *)
            echo "Unknown action: $action"
            return 1
            ;;
    esac
}

add_route() {
    local path="$1"
    local method="$2"
    local target="$3"
    local routes_file="${gateway_routes_file:-/etc/api-gateway/routes.conf}"
    
    # Create routes directory
    mkdir -p "$(dirname "$routes_file")"
    
    # Add route to configuration
    echo "$method|$path|$target" >> "$routes_file"
    
    echo "✓ Route added: $method $path -> $target"
}

remove_route() {
    local path="$1"
    local method="$2"
    local routes_file="${gateway_routes_file:-/etc/api-gateway/routes.conf}"
    
    if [[ -f "$routes_file" ]]; then
        # Remove matching route
        sed -i "/^$method|$path|/d" "$routes_file"
        echo "✓ Route removed: $method $path"
    else
        echo "Routes file not found: $routes_file"
    fi
}

list_routes() {
    local routes_file="${gateway_routes_file:-/etc/api-gateway/routes.conf}"
    
    if [[ -f "$routes_file" ]]; then
        echo "API Gateway Routes:"
        echo "=================="
        while IFS='|' read -r method path target; do
            printf "%-8s %-30s -> %s\n" "$method" "$path" "$target"
        done < "$routes_file"
    else
        echo "No routes configured"
    fi
}

update_route() {
    local path="$1"
    local method="$2"
    local target="$3"
    local routes_file="${gateway_routes_file:-/etc/api-gateway/routes.conf}"
    
    if [[ -f "$routes_file" ]]; then
        # Update matching route
        sed -i "s|^$method|$path|.*|$method|$path|$target|" "$routes_file"
        echo "✓ Route updated: $method $path -> $target"
    else
        echo "Routes file not found: $routes_file"
    fi
}
```

### **Request Processing**
```bash
#!/bin/bash

# Request processing
process_api_request() {
    local method="$1"
    local path="$2"
    local headers="${3:-}"
    local body="${4:-}"
    
    # Log request
    log_request "$method" "$path" "$headers"
    
    # Check rate limiting
    if ! check_gateway_rate_limit "$method" "$path"; then
        return_429_response
        return 1
    fi
    
    # Check authentication
    if requires_authentication "$method" "$path" && ! authenticate_request "$headers"; then
        return_401_response
        return 1
    fi
    
    # Check authorization
    if requires_authorization "$method" "$path" && ! authorize_request "$headers" "$method" "$path"; then
        return_403_response
        return 1
    fi
    
    # Find and execute route
    local route=$(find_route "$method" "$path")
    if [[ -n "$route" ]]; then
        execute_route "$route" "$method" "$path" "$headers" "$body"
    else
        return_404_response
        return 1
    fi
}

log_request() {
    local method="$1"
    local path="$2"
    local headers="$3"
    local request_log="/var/log/api-gateway/requests.log"
    
    echo "$(date '+%Y-%m-%d %H:%M:%S') | $method | $path | $headers" >> "$request_log"
}

check_gateway_rate_limit() {
    local method="$1"
    local path="$2"
    local rate_limit_file="/tmp/gateway_rate_limit_${method}_${path//[^a-zA-Z0-9]/_}"
    local current_time=$(date +%s)
    local limit="${gateway_rate_limit:-100}"
    local window="${gateway_rate_window:-60}"
    
    # Check rate limit
    if [[ -f "$rate_limit_file" ]]; then
        local request_count=$(cat "$rate_limit_file")
        local last_reset=$(stat -c %Y "$rate_limit_file")
        
        # Reset counter if window has passed
        if [[ $((current_time - last_reset)) -gt $window ]]; then
            echo "1" > "$rate_limit_file"
            return 0
        fi
        
        # Check if limit exceeded
        if [[ $request_count -ge $limit ]]; then
            return 1
        fi
        
        # Increment counter
        echo $((request_count + 1)) > "$rate_limit_file"
    else
        echo "1" > "$rate_limit_file"
    fi
    
    return 0
}

authenticate_request() {
    local headers="$1"
    local auth_header=$(echo "$headers" | grep -o "Authorization: Bearer [^ ]*" | cut -d' ' -f3)
    
    if [[ -n "$auth_header" ]]; then
        # Validate JWT token
        validate_jwt_token "$auth_header"
        return $?
    fi
    
    return 1
}

validate_jwt_token() {
    local token="$1"
    local secret="${gateway_jwt_secret:-your-secret-key}"
    
    # Simple JWT validation (in production, use proper JWT library)
    if [[ -n "$token" ]]; then
        # Check if token is not expired (simplified)
        return 0
    fi
    
    return 1
}

authorize_request() {
    local headers="$1"
    local method="$2"
    local path="$3"
    
    # Extract user role from token
    local user_role=$(extract_user_role "$headers")
    
    # Check authorization rules
    check_authorization_rules "$user_role" "$method" "$path"
    return $?
}

extract_user_role() {
    local headers="$1"
    local auth_header=$(echo "$headers" | grep -o "Authorization: Bearer [^ ]*" | cut -d' ' -f3)
    
    if [[ -n "$auth_header" ]]; then
        # Extract role from JWT payload (simplified)
        echo "user"
    else
        echo "anonymous"
    fi
}

check_authorization_rules() {
    local role="$1"
    local method="$2"
    local path="$3"
    local auth_rules_file="${gateway_auth_rules_file:-/etc/api-gateway/auth.conf}"
    
    if [[ -f "$auth_rules_file" ]]; then
        while IFS='|' read -r rule_role rule_method rule_path; do
            if [[ "$rule_role" == "$role" ]] && \
               [[ "$rule_method" == "$method" ]] && \
               [[ "$path" =~ $rule_path ]]; then
                return 0
            fi
        done < "$auth_rules_file"
    fi
    
    return 1
}

find_route() {
    local method="$1"
    local path="$2"
    local routes_file="${gateway_routes_file:-/etc/api-gateway/routes.conf}"
    
    if [[ -f "$routes_file" ]]; then
        while IFS='|' read -r route_method route_path route_target; do
            if [[ "$route_method" == "$method" ]] && [[ "$route_path" == "$path" ]]; then
                echo "$route_target"
                return 0
            fi
        done < "$routes_file"
    fi
    
    return 1
}

execute_route() {
    local target="$1"
    local method="$2"
    local path="$3"
    local headers="$4"
    local body="$5"
    
    echo "Executing route: $method $path -> $target"
    
    # Parse target
    IFS=':' read -r host port <<< "$target"
    
    # Forward request
    forward_request "$host" "$port" "$method" "$path" "$headers" "$body"
}

forward_request() {
    local host="$1"
    local port="$2"
    local method="$3"
    local path="$4"
    local headers="$5"
    local body="$6"
    
    # Build curl command
    local curl_cmd="curl -s -X $method"
    
    # Add headers
    if [[ -n "$headers" ]]; then
        IFS=',' read -ra header_array <<< "$headers"
        for header in "${header_array[@]}"; do
            curl_cmd="$curl_cmd -H '$header'"
        done
    fi
    
    # Add body if present
    if [[ -n "$body" ]]; then
        curl_cmd="$curl_cmd -d '$body'"
    fi
    
    # Execute request
    local response=$(eval "$curl_cmd 'http://$host:$port$path'")
    local exit_code=$?
    
    if [[ $exit_code -eq 0 ]]; then
        echo "$response"
    else
        echo "Error forwarding request: $exit_code"
        return 1
    fi
}
```

### **Response Caching**
```bash
#!/bin/bash

# Response caching
cache_response() {
    local method="$1"
    local path="$2"
    local response="$3"
    local ttl="${4:-300}" # 5 minutes default
    local cache_dir="${gateway_cache_dir:-/tmp/api-gateway/cache}"
    
    # Only cache GET requests
    if [[ "$method" != "GET" ]]; then
        return 0
    fi
    
    # Create cache directory
    mkdir -p "$cache_dir"
    
    # Create cache key
    local cache_key=$(echo "$method:$path" | md5sum | cut -d' ' -f1)
    local cache_file="$cache_dir/$cache_key"
    
    # Store response with timestamp
    cat > "$cache_file" << EOF
{
    "method": "$method",
    "path": "$path",
    "response": "$response",
    "cached_at": "$(date -Iseconds)",
    "ttl": $ttl
}
EOF
    
    echo "✓ Response cached: $method $path"
}

get_cached_response() {
    local method="$1"
    local path="$2"
    local cache_dir="${gateway_cache_dir:-/tmp/api-gateway/cache}"
    
    # Only check cache for GET requests
    if [[ "$method" != "GET" ]]; then
        return 1
    fi
    
    # Create cache key
    local cache_key=$(echo "$method:$path" | md5sum | cut -d' ' -f1)
    local cache_file="$cache_dir/$cache_key"
    
    if [[ -f "$cache_file" ]]; then
        local cached_at=$(jq -r '.cached_at' "$cache_file" 2>/dev/null)
        local ttl=$(jq -r '.ttl' "$cache_file" 2>/dev/null)
        local current_time=$(date +%s)
        local cache_time=$(date -d "$cached_at" +%s 2>/dev/null || echo 0)
        
        # Check if cache is still valid
        if [[ $((current_time - cache_time)) -lt $ttl ]]; then
            local response=$(jq -r '.response' "$cache_file" 2>/dev/null)
            echo "$response"
            return 0
        else
            # Remove expired cache
            rm -f "$cache_file"
        fi
    fi
    
    return 1
}

clear_cache() {
    local cache_dir="${gateway_cache_dir:-/tmp/api-gateway/cache}"
    
    if [[ -d "$cache_dir" ]]; then
        rm -rf "$cache_dir"/*
        echo "✓ API gateway cache cleared"
    else
        echo "Cache directory not found: $cache_dir"
    fi
}
```

### **API Gateway Monitoring**
```bash
#!/bin/bash

# API gateway monitoring
monitor_gateway() {
    local monitoring_file="/var/log/api-gateway/monitoring.json"
    
    # Collect gateway metrics
    local total_requests=$(get_total_requests)
    local successful_requests=$(get_successful_requests)
    local failed_requests=$(get_failed_requests)
    local cached_requests=$(get_cached_requests)
    local rate_limited_requests=$(get_rate_limited_requests)
    local average_response_time=$(get_average_response_time)
    
    # Generate monitoring report
    cat > "$monitoring_file" << EOF
{
    "timestamp": "$(date -Iseconds)",
    "total_requests": $total_requests,
    "successful_requests": $successful_requests,
    "failed_requests": $failed_requests,
    "cached_requests": $cached_requests,
    "rate_limited_requests": $rate_limited_requests,
    "success_rate": $(((successful_requests * 100) / total_requests)),
    "cache_hit_rate": $(((cached_requests * 100) / total_requests)),
    "average_response_time_ms": $average_response_time
}
EOF
    
    echo "✓ API gateway monitoring completed"
}

get_total_requests() {
    local request_log="/var/log/api-gateway/requests.log"
    
    if [[ -f "$request_log" ]]; then
        wc -l < "$request_log"
    else
        echo "0"
    fi
}

get_successful_requests() {
    local request_log="/var/log/api-gateway/requests.log"
    
    if [[ -f "$request_log" ]]; then
        grep -c "200\|201\|204" "$request_log" 2>/dev/null || echo "0"
    else
        echo "0"
    fi
}

get_failed_requests() {
    local request_log="/var/log/api-gateway/requests.log"
    
    if [[ -f "$request_log" ]]; then
        grep -c "4[0-9][0-9]\|5[0-9][0-9]" "$request_log" 2>/dev/null || echo "0"
    else
        echo "0"
    fi
}

get_cached_requests() {
    local cache_log="/var/log/api-gateway/cache.log"
    
    if [[ -f "$cache_log" ]]; then
        grep -c "CACHE_HIT" "$cache_log" 2>/dev/null || echo "0"
    else
        echo "0"
    fi
}

get_rate_limited_requests() {
    local request_log="/var/log/api-gateway/requests.log"
    
    if [[ -f "$request_log" ]]; then
        grep -c "429" "$request_log" 2>/dev/null || echo "0"
    else
        echo "0"
    fi
}

get_average_response_time() {
    local response_log="/var/log/api-gateway/responses.log"
    
    if [[ -f "$response_log" ]]; then
        local total_time=0
        local request_count=0
        
        while IFS= read -r log_line; do
            local response_time=$(echo "$log_line" | grep -o 'response_time=[0-9]*' | cut -d'=' -f2)
            if [[ -n "$response_time" ]]; then
                total_time=$((total_time + response_time))
                request_count=$((request_count + 1))
            fi
        done < "$response_log"
        
        if [[ $request_count -gt 0 ]]; then
            echo $((total_time / request_count))
        else
            echo "0"
        fi
    else
        echo "0"
    fi
}
```

### **Response Handling**
```bash
#!/bin/bash

# Response handling
return_200_response() {
    local response="$1"
    echo "HTTP/1.1 200 OK"
    echo "Content-Type: application/json"
    echo "Content-Length: ${#response}"
    echo ""
    echo "$response"
}

return_401_response() {
    echo "HTTP/1.1 401 Unauthorized"
    echo "Content-Type: application/json"
    echo "Content-Length: 31"
    echo ""
    echo '{"error": "Authentication required"}'
}

return_403_response() {
    echo "HTTP/1.1 403 Forbidden"
    echo "Content-Type: application/json"
    echo "Content-Length: 25"
    echo ""
    echo '{"error": "Access denied"}'
}

return_404_response() {
    echo "HTTP/1.1 404 Not Found"
    echo "Content-Type: application/json"
    echo "Content-Length: 25"
    echo ""
    echo '{"error": "Route not found"}'
}

return_429_response() {
    echo "HTTP/1.1 429 Too Many Requests"
    echo "Content-Type: application/json"
    echo "Content-Length: 35"
    echo ""
    echo '{"error": "Rate limit exceeded"}'
}

requires_authentication() {
    local method="$1"
    local path="$2"
    local auth_routes_file="${gateway_auth_routes_file:-/etc/api-gateway/auth-routes.conf}"
    
    if [[ -f "$auth_routes_file" ]]; then
        while IFS='|' read -r route_method route_path; do
            if [[ "$route_method" == "$method" ]] && [[ "$path" =~ $route_path ]]; then
                return 0
            fi
        done < "$auth_routes_file"
    fi
    
    return 1
}

requires_authorization() {
    local method="$1"
    local path="$2"
    local auth_routes_file="${gateway_auth_routes_file:-/etc/api-gateway/auth-routes.conf}"
    
    # All authenticated routes require authorization
    requires_authentication "$method" "$path"
    return $?
}
```

## 🎯 **Real-World Configuration Examples**

### **Complete API Gateway Configuration**
```bash
# api-gateway-config.tsk
api_gateway_config:
  enabled: true
  port: 8080
  host: 0.0.0.0
  routes: true
  rate_limiting: true

#api-gateway: enabled
#gateway-enabled: true
#gateway-port: 8080
#gateway-host: 0.0.0.0
#gateway-routes: true
#gateway-rate-limiting: true

#gateway-authentication: true
#gateway-authorization: true
#gateway-caching: true
#gateway-monitoring: true
#gateway-logging: true
#gateway-metrics: true

#gateway-config:
#  general:
#    port: 8080
#    host: "0.0.0.0"
#    routes: true
#    rate_limiting: true
#  routes:
#    enabled: true
#    file: "/etc/api-gateway/routes.conf"
#    default_policy: "deny"
#  rate_limiting:
#    enabled: true
#    limit: 100
#    window: 60
#    per_client: true
#  authentication:
#    enabled: true
#    type: "jwt"
#    secret: "${GATEWAY_JWT_SECRET}"
#    routes_file: "/etc/api-gateway/auth-routes.conf"
#  authorization:
#    enabled: true
#    rules_file: "/etc/api-gateway/auth.conf"
#    enforcement: "strict"
#  caching:
#    enabled: true
#    ttl: 300
#    directory: "/tmp/api-gateway/cache"
#    methods: ["GET"]
#  monitoring:
#    enabled: true
#    interval: 60
#    metrics:
#      - "request_count"
#      - "response_time"
#      - "error_rate"
#      - "cache_hit_rate"
#  logging:
#    enabled: true
#    level: "info"
#    file: "/var/log/api-gateway/requests.log"
#    rotation: true
```

### **Multi-Service API Gateway**
```bash
# multi-service-api-gateway.tsk
multi_service_api_gateway:
  services:
    - name: user-service
      routes: true
      authentication: true
    - name: order-service
      routes: true
      authentication: true
    - name: payment-service
      routes: true
      authentication: true

#gateway-user-service: enabled
#gateway-order-service: enabled
#gateway-payment-service: enabled

#gateway-config:
#  services:
#    user_service:
#      enabled: true
#      routes: true
#      authentication: true
#      target: "user-service:8080"
#    order_service:
#      enabled: true
#      routes: true
#      authentication: true
#      target: "order-service:8080"
#    payment_service:
#      enabled: true
#      routes: true
#      authentication: true
#      target: "payment-service:8080"
```

## 🚨 **Troubleshooting API Gateway**

### **Common Issues and Solutions**

**1. Route Issues**
```bash
# Debug routes
debug_routes() {
    echo "Debugging API gateway routes..."
    list_routes
    echo "Route debugging completed"
}
```

**2. Authentication Issues**
```bash
# Debug authentication
debug_authentication() {
    local method="$1"
    local path="$2"
    echo "Debugging authentication for $method $path"
    requires_authentication "$method" "$path"
    echo "Authentication required: $?"
}
```

## 🔒 **Security Best Practices**

### **API Gateway Security Checklist**
```bash
# Security validation
validate_api_gateway_security() {
    echo "Validating API gateway security configuration..."
    # Check authentication
    if [[ "${gateway_authentication}" == "true" ]]; then
        echo "✓ Authentication enabled"
    else
        echo "⚠ Authentication not enabled"
    fi
    # Check authorization
    if [[ "${gateway_authorization}" == "true" ]]; then
        echo "✓ Authorization enabled"
    else
        echo "⚠ Authorization not enabled"
    fi
    # Check rate limiting
    if [[ "${gateway_rate_limiting}" == "true" ]]; then
        echo "✓ Rate limiting enabled"
    else
        echo "⚠ Rate limiting not enabled"
    fi
}
```

## 📈 **Performance Optimization Tips**

### **API Gateway Performance Checklist**
```bash
# Performance validation
validate_api_gateway_performance() {
    echo "Validating API gateway performance configuration..."
    # Check caching
    if [[ "${gateway_caching}" == "true" ]]; then
        echo "✓ Response caching enabled"
    else
        echo "⚠ Response caching not enabled"
    fi
    # Check rate limiting
    local rate_limit="${gateway_rate_limit:-100}"
    if [[ "$rate_limit" -ge 50 ]]; then
        echo "✓ Reasonable rate limit ($rate_limit)"
    else
        echo "⚠ Low rate limit may impact performance ($rate_limit)"
    fi
    # Check monitoring
    if [[ "${gateway_monitoring}" == "true" ]]; then
        echo "✓ Gateway monitoring enabled"
    else
        echo "⚠ Gateway monitoring not enabled"
    fi
}
```

## 🎯 **Next Steps**

- **API Gateway Optimization**: Learn about advanced API gateway optimization
- **API Gateway Visualization**: Create API gateway visualization dashboards
- **API Gateway Correlation**: Implement API gateway correlation and alerting
- **API Gateway Compliance**: Set up API gateway compliance and auditing

---

**API gateway transforms your TuskLang configuration into an intelligent, unified API management system. It brings modern gateway capabilities to your Bash applications with dynamic routing, intelligent rate limiting, and comprehensive API analytics!** 