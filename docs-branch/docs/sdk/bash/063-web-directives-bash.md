# Web Directives in TuskLang - Bash Guide

## 🌐 **Revolutionary Web Configuration**

Web directives in TuskLang transform your configuration files into powerful web server orchestrators. No more separate config files for web servers - everything lives in your TuskLang configuration with dynamic behavior and intelligent routing.

> **"We don't bow to any king"** - TuskLang web directives break free from traditional web server constraints and bring modern web capabilities to your Bash applications.

## 🚀 **Core Web Directives**

### **Basic Web Server Setup**
```bash
#web: true                    # Enable web server
#port: 8080                   # Server port
#host: 0.0.0.0               # Bind address
#ssl: true                    # Enable HTTPS
#cors: "*"                    # CORS policy
#timeout: 30                  # Request timeout (seconds)
```

### **Advanced Web Configuration**
```bash
#proxy: true                  # Enable reverse proxy
#load-balancer: round-robin   # Load balancing strategy
#compression: gzip            # Response compression
#cache-control: max-age=3600  # Cache headers
#security-headers: true       # Security headers
#rate-limit: 100/min          # Rate limiting
```

## 🔧 **Bash Web Server Implementation**

### **Basic HTTP Server**
```bash
#!/bin/bash

# Load web configuration
source <(tsk load web.tsk)

# Web server configuration
WEB_PORT="${web_port:-8080}"
WEB_HOST="${web_host:-0.0.0.0}"
SSL_ENABLED="${ssl_enabled:-false}"
CORS_ORIGIN="${cors_origin:-*}"
REQUEST_TIMEOUT="${request_timeout:-30}"

# Start web server
start_web_server() {
    echo "Starting web server on $WEB_HOST:$WEB_PORT"
    
    if [[ "$SSL_ENABLED" == "true" ]]; then
        echo "SSL enabled - starting HTTPS server"
        start_https_server
    else
        echo "Starting HTTP server"
        start_http_server
    fi
}

start_http_server() {
    # Simple HTTP server using netcat
    while true; do
        echo -e "HTTP/1.1 200 OK\r\nContent-Type: text/html\r\n\r\n<html><body><h1>TuskLang Web Server</h1></body></html>" | \
        nc -l -p "$WEB_PORT" -w "$REQUEST_TIMEOUT" 2>/dev/null || true
    done
}

start_https_server() {
    # HTTPS server with SSL certificates
    if [[ -f "$ssl_cert" ]] && [[ -f "$ssl_key" ]]; then
        echo "Using SSL certificates: $ssl_cert, $ssl_key"
        # HTTPS server implementation
    else
        echo "SSL certificates not found, falling back to HTTP"
        start_http_server
    fi
}
```

### **Advanced HTTP Server with Routing**
```bash
#!/bin/bash

# Load web configuration with routes
source <(tsk load web-routes.tsk)

# Route handler
handle_request() {
    local method="$1"
    local path="$2"
    local headers="$3"
    local body="$4"
    
    echo "Handling $method request to $path"
    
    # Apply CORS headers
    add_cors_headers
    
    # Apply security headers
    add_security_headers
    
    # Route to appropriate handler
    case "$path" in
        "/")
            handle_root
            ;;
        "/api/*")
            handle_api "$method" "$path" "$body"
            ;;
        "/health")
            handle_health
            ;;
        "/metrics")
            handle_metrics
            ;;
        *)
            handle_404
            ;;
    esac
}

add_cors_headers() {
    echo "Access-Control-Allow-Origin: $CORS_ORIGIN"
    echo "Access-Control-Allow-Methods: GET, POST, PUT, DELETE, OPTIONS"
    echo "Access-Control-Allow-Headers: Content-Type, Authorization"
}

add_security_headers() {
    echo "X-Content-Type-Options: nosniff"
    echo "X-Frame-Options: DENY"
    echo "X-XSS-Protection: 1; mode=block"
    echo "Strict-Transport-Security: max-age=31536000; includeSubDomains"
}

handle_root() {
    cat << 'EOF'
HTTP/1.1 200 OK
Content-Type: text/html

<!DOCTYPE html>
<html>
<head>
    <title>TuskLang Web Server</title>
</head>
<body>
    <h1>Welcome to TuskLang Web Server</h1>
    <p>Powered by TuskLang configuration</p>
</body>
</html>
EOF
}

handle_api() {
    local method="$1"
    local path="$2"
    local body="$3"
    
    case "$method" in
        "GET")
            handle_api_get "$path"
            ;;
        "POST")
            handle_api_post "$path" "$body"
            ;;
        "PUT")
            handle_api_put "$path" "$body"
            ;;
        "DELETE")
            handle_api_delete "$path"
            ;;
        *)
            handle_405
            ;;
    esac
}

handle_health() {
    cat << 'EOF'
HTTP/1.1 200 OK
Content-Type: application/json

{
    "status": "healthy",
    "timestamp": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
    "uptime": "$(uptime -p)"
}
EOF
}

handle_metrics() {
    cat << 'EOF'
HTTP/1.1 200 OK
Content-Type: text/plain

# HELP http_requests_total Total number of HTTP requests
# TYPE http_requests_total counter
http_requests_total{method="GET",status="200"} $(get_request_count)
EOF
}
```

## 🔒 **SSL/TLS Configuration**

### **SSL Certificate Management**
```bash
#!/bin/bash

# Load SSL configuration
source <(tsk load ssl.tsk)

# SSL setup
setup_ssl() {
    if [[ "$ssl_enabled" == "true" ]]; then
        echo "Setting up SSL/TLS"
        
        # Check for certificates
        if [[ -f "$ssl_cert" ]] && [[ -f "$ssl_key" ]]; then
            echo "Using existing certificates"
            validate_ssl_certificates
        else
            echo "Generating self-signed certificates"
            generate_self_signed_certificates
        fi
        
        # Set SSL parameters
        export SSL_CERT="$ssl_cert"
        export SSL_KEY="$ssl_key"
        export SSL_CIPHERS="${ssl_ciphers:-HIGH:!aNULL:!MD5}"
        export SSL_PROTOCOLS="${ssl_protocols:-TLSv1.2 TLSv1.3}"
    fi
}

validate_ssl_certificates() {
    # Validate certificate
    if openssl x509 -in "$ssl_cert" -text -noout >/dev/null 2>&1; then
        echo "SSL certificate is valid"
        
        # Check expiration
        local expiry=$(openssl x509 -in "$ssl_cert" -enddate -noout | cut -d= -f2)
        echo "Certificate expires: $expiry"
        
        # Check if expired
        if openssl x509 -in "$ssl_cert" -checkend 0 >/dev/null 2>&1; then
            echo "Certificate is not expired"
        else
            echo "WARNING: Certificate has expired"
        fi
    else
        echo "ERROR: Invalid SSL certificate"
        exit 1
    fi
}

generate_self_signed_certificates() {
    local cert_dir=$(dirname "$ssl_cert")
    mkdir -p "$cert_dir"
    
    # Generate private key
    openssl genrsa -out "$ssl_key" 2048
    
    # Generate certificate
    openssl req -new -x509 -key "$ssl_key" -out "$ssl_cert" -days 365 \
        -subj "/C=US/ST=State/L=City/O=Organization/CN=localhost"
    
    echo "Self-signed certificates generated"
}
```

## 🌍 **CORS Configuration**

### **CORS Policy Management**
```bash
#!/bin/bash

# Load CORS configuration
source <(tsk load cors.tsk)

# CORS setup
setup_cors() {
    local cors_origin="${cors_origin:-*}"
    local cors_methods="${cors_methods:-GET, POST, PUT, DELETE, OPTIONS}"
    local cors_headers="${cors_headers:-Content-Type, Authorization}"
    local cors_credentials="${cors_credentials:-false}"
    
    echo "Setting up CORS policy"
    echo "  Origin: $cors_origin"
    echo "  Methods: $cors_methods"
    echo "  Headers: $cors_headers"
    echo "  Credentials: $cors_credentials"
    
    export CORS_ORIGIN="$cors_origin"
    export CORS_METHODS="$cors_methods"
    export CORS_HEADERS="$cors_headers"
    export CORS_CREDENTIALS="$cors_credentials"
}

# Handle CORS preflight requests
handle_cors_preflight() {
    local origin="$1"
    local method="$2"
    local headers="$3"
    
    # Check if origin is allowed
    if [[ "$CORS_ORIGIN" == "*" ]] || [[ "$CORS_ORIGIN" == "$origin" ]]; then
        cat << EOF
HTTP/1.1 200 OK
Access-Control-Allow-Origin: $origin
Access-Control-Allow-Methods: $CORS_METHODS
Access-Control-Allow-Headers: $CORS_HEADERS
Access-Control-Allow-Credentials: $CORS_CREDENTIALS
Access-Control-Max-Age: 86400
Content-Length: 0

EOF
    else
        handle_403 "Origin not allowed: $origin"
    fi
}
```

## ⚡ **Performance Optimization**

### **Compression Setup**
```bash
#!/bin/bash

# Load compression configuration
source <(tsk load compression.tsk)

# Compression setup
setup_compression() {
    local compression_type="${compression_type:-gzip}"
    local compression_level="${compression_level:-6}"
    local compression_min_size="${compression_min_size:-1024}"
    
    echo "Setting up $compression_type compression"
    echo "  Level: $compression_level"
    echo "  Min size: $compression_min_size bytes"
    
    export COMPRESSION_TYPE="$compression_type"
    export COMPRESSION_LEVEL="$compression_level"
    export COMPRESSION_MIN_SIZE="$compression_min_size"
}

# Compress response
compress_response() {
    local content="$1"
    local content_type="$2"
    
    # Check if content should be compressed
    if [[ ${#content} -lt "$COMPRESSION_MIN_SIZE" ]]; then
        echo "$content"
        return
    fi
    
    # Check if content type is compressible
    case "$content_type" in
        text/*|application/json|application/xml|application/javascript)
            # Content is compressible
            ;;
        *)
            echo "$content"
            return
            ;;
    esac
    
    # Apply compression
    case "$COMPRESSION_TYPE" in
        "gzip")
            echo "$content" | gzip -c -"$COMPRESSION_LEVEL"
            ;;
        "deflate")
            echo "$content" | gzip -c -"$COMPRESSION_LEVEL" -n
            ;;
        *)
            echo "$content"
            ;;
    esac
}
```

### **Caching Configuration**
```bash
#!/bin/bash

# Load caching configuration
source <(tsk load cache.tsk)

# Cache setup
setup_cache() {
    local cache_enabled="${cache_enabled:-true}"
    local cache_max_age="${cache_max_age:-3600}"
    local cache_public="${cache_public:-false}"
    local cache_etag="${cache_etag:-true}"
    
    echo "Setting up HTTP caching"
    echo "  Enabled: $cache_enabled"
    echo "  Max age: $cache_max_age seconds"
    echo "  Public: $cache_public"
    echo "  ETag: $cache_etag"
    
    export CACHE_ENABLED="$cache_enabled"
    export CACHE_MAX_AGE="$cache_max_age"
    export CACHE_PUBLIC="$cache_public"
    export CACHE_ETAG="$cache_etag"
}

# Add cache headers
add_cache_headers() {
    local path="$1"
    local content="$2"
    
    if [[ "$CACHE_ENABLED" == "true" ]]; then
        echo "Cache-Control: max-age=$CACHE_MAX_AGE"
        
        if [[ "$CACHE_PUBLIC" == "true" ]]; then
            echo "Cache-Control: public"
        else
            echo "Cache-Control: private"
        fi
        
        if [[ "$CACHE_ETAG" == "true" ]]; then
            local etag=$(echo -n "$content" | md5sum | cut -d' ' -f1)
            echo "ETag: \"$etag\""
        fi
    fi
}
```

## 🔄 **Load Balancing**

### **Load Balancer Configuration**
```bash
#!/bin/bash

# Load load balancer configuration
source <(tsk load load-balancer.tsk)

# Load balancer setup
setup_load_balancer() {
    local strategy="${load_balancer_strategy:-round-robin}"
    local backends=(${load_balancer_backends[@]})
    local health_check="${health_check_enabled:-true}"
    local health_check_interval="${health_check_interval:-30}"
    
    echo "Setting up load balancer"
    echo "  Strategy: $strategy"
    echo "  Backends: ${backends[*]}"
    echo "  Health check: $health_check"
    
    export LB_STRATEGY="$strategy"
    export LB_BACKENDS=("${backends[@]}")
    export HEALTH_CHECK_ENABLED="$health_check"
    export HEALTH_CHECK_INTERVAL="$health_check_interval"
    
    # Initialize load balancer
    case "$strategy" in
        "round-robin")
            setup_round_robin
            ;;
        "least-connections")
            setup_least_connections
            ;;
        "ip-hash")
            setup_ip_hash
            ;;
        *)
            echo "Unknown load balancer strategy: $strategy"
            exit 1
            ;;
    esac
}

setup_round_robin() {
    export LB_CURRENT_INDEX=0
    export LB_BACKEND_COUNT=${#LB_BACKENDS[@]}
    
    echo "Round-robin load balancer initialized with $LB_BACKEND_COUNT backends"
}

get_next_backend() {
    case "$LB_STRATEGY" in
        "round-robin")
            local backend="${LB_BACKENDS[$LB_CURRENT_INDEX]}"
            LB_CURRENT_INDEX=$(((LB_CURRENT_INDEX + 1) % LB_BACKEND_COUNT))
            echo "$backend"
            ;;
        "least-connections")
            get_least_loaded_backend
            ;;
        "ip-hash")
            get_ip_hash_backend
            ;;
    esac
}

get_least_loaded_backend() {
    local least_loaded=""
    local min_connections=999999
    
    for backend in "${LB_BACKENDS[@]}"; do
        local connections=$(get_backend_connections "$backend")
        if [[ "$connections" -lt "$min_connections" ]]; then
            min_connections="$connections"
            least_loaded="$backend"
        fi
    done
    
    echo "$least_loaded"
}

get_ip_hash_backend() {
    local client_ip="$1"
    local hash=$(echo -n "$client_ip" | md5sum | cut -d' ' -f1)
    local index=$((0x${hash:0:8} % LB_BACKEND_COUNT))
    echo "${LB_BACKENDS[$index]}"
}
```

## 🛡️ **Security Features**

### **Security Headers**
```bash
#!/bin/bash

# Load security configuration
source <(tsk load security.tsk)

# Security headers setup
setup_security_headers() {
    local security_headers_enabled="${security_headers_enabled:-true}"
    local hsts_max_age="${hsts_max_age:-31536000}"
    local csp_policy="${csp_policy:-default-src 'self'}"
    local referrer_policy="${referrer_policy:-strict-origin-when-cross-origin}"
    
    echo "Setting up security headers"
    echo "  HSTS max age: $hsts_max_age"
    echo "  CSP policy: $csp_policy"
    echo "  Referrer policy: $referrer_policy"
    
    export SECURITY_HEADERS_ENABLED="$security_headers_enabled"
    export HSTS_MAX_AGE="$hsts_max_age"
    export CSP_POLICY="$csp_policy"
    export REFERRER_POLICY="$referrer_policy"
}

# Add security headers
add_security_headers() {
    if [[ "$SECURITY_HEADERS_ENABLED" == "true" ]]; then
        echo "X-Content-Type-Options: nosniff"
        echo "X-Frame-Options: DENY"
        echo "X-XSS-Protection: 1; mode=block"
        echo "Referrer-Policy: $REFERRER_POLICY"
        echo "Content-Security-Policy: $CSP_POLICY"
        
        if [[ "$ssl_enabled" == "true" ]]; then
            echo "Strict-Transport-Security: max-age=$HSTS_MAX_AGE; includeSubDomains"
        fi
    fi
}
```

## 📊 **Monitoring and Metrics**

### **Request Monitoring**
```bash
#!/bin/bash

# Load monitoring configuration
source <(tsk load monitoring.tsk)

# Monitoring setup
setup_monitoring() {
    local monitoring_enabled="${monitoring_enabled:-true}"
    local metrics_endpoint="${metrics_endpoint:-/metrics}"
    local health_endpoint="${health_endpoint:-/health}"
    
    echo "Setting up monitoring"
    echo "  Metrics endpoint: $metrics_endpoint"
    echo "  Health endpoint: $health_endpoint"
    
    export MONITORING_ENABLED="$monitoring_enabled"
    export METRICS_ENDPOINT="$metrics_endpoint"
    export HEALTH_ENDPOINT="$health_endpoint"
    
    # Initialize metrics
    initialize_metrics
}

initialize_metrics() {
    # Request counters
    declare -gA REQUEST_COUNTERS
    REQUEST_COUNTERS["total"]=0
    REQUEST_COUNTERS["200"]=0
    REQUEST_COUNTERS["404"]=0
    REQUEST_COUNTERS["500"]=0
    
    # Response time tracking
    declare -gA RESPONSE_TIMES
    RESPONSE_TIMES["total"]=0
    RESPONSE_TIMES["count"]=0
    
    echo "Metrics initialized"
}

record_request() {
    local method="$1"
    local path="$2"
    local status="$3"
    local response_time="$4"
    
    # Increment counters
    REQUEST_COUNTERS["total"]=$((REQUEST_COUNTERS["total"] + 1))
    REQUEST_COUNTERS["$status"]=$((REQUEST_COUNTERS["$status"] + 1))
    
    # Track response time
    RESPONSE_TIMES["total"]=$((RESPONSE_TIMES["total"] + response_time))
    RESPONSE_TIMES["count"]=$((RESPONSE_TIMES["count"] + 1))
    
    # Log request
    echo "$(date '+%Y-%m-%d %H:%M:%S') $method $path $status ${response_time}ms"
}

get_metrics() {
    local avg_response_time=0
    if [[ "${RESPONSE_TIMES[count]}" -gt 0 ]]; then
        avg_response_time=$((RESPONSE_TIMES["total"] / RESPONSE_TIMES["count"]))
    fi
    
    cat << EOF
# HELP http_requests_total Total number of HTTP requests
# TYPE http_requests_total counter
http_requests_total{method="all",status="all"} ${REQUEST_COUNTERS["total"]}
http_requests_total{method="all",status="200"} ${REQUEST_COUNTERS["200"]}
http_requests_total{method="all",status="404"} ${REQUEST_COUNTERS["404"]}
http_requests_total{method="all",status="500"} ${REQUEST_COUNTERS["500"]}

# HELP http_response_time_average Average response time in milliseconds
# TYPE http_response_time_average gauge
http_response_time_average $avg_response_time
EOF
}
```

## 🎯 **Real-World Configuration Examples**

### **Complete Web Application**
```bash
# web-app.tsk
app_name: "TuskLang Web App"
version: "1.0.0"

#web: true
#port: 8080
#host: 0.0.0.0
#ssl: true
#cors: "*"
#timeout: 30

#compression: gzip
#compression-level: 6
#compression-min-size: 1024

#cache-control: max-age=3600
#cache-public: true
#cache-etag: true

#security-headers: true
#hsts-max-age: 31536000
#csp-policy: "default-src 'self'"

#monitoring: true
#metrics-endpoint: /metrics
#health-endpoint: /health

#rate-limit: 100/min
#load-balancer: round-robin
#load-balancer-backends: ["http://backend1:3001", "http://backend2:3001"]
```

### **API Gateway Configuration**
```bash
# api-gateway.tsk
gateway_name: "TuskLang API Gateway"
version: "2.0.0"

#web: true
#port: 443
#host: 0.0.0.0
#ssl: true
#cors: "https://app.example.com"

#proxy: true
#load-balancer: least-connections
#load-balancer-backends: ["http://api1:8080", "http://api2:8080", "http://api3:8080"]

#auth: jwt
#rate-limit: 1000/min
#timeout: 60

#compression: gzip
#cache-control: max-age=300

#monitoring: true
#health-check: true
#health-check-interval: 30
```

## 🚨 **Troubleshooting Web Directives**

### **Common Issues and Solutions**

**1. Port Already in Use**
```bash
# Check if port is available
check_port_availability() {
    local port="$1"
    
    if netstat -tuln | grep -q ":$port "; then
        echo "ERROR: Port $port is already in use"
        echo "Available ports:"
        netstat -tuln | grep LISTEN | awk '{print $4}' | cut -d: -f2 | sort -n
        return 1
    fi
    
    return 0
}
```

**2. SSL Certificate Issues**
```bash
# Validate SSL setup
validate_ssl_setup() {
    if [[ "$ssl_enabled" == "true" ]]; then
        if [[ ! -f "$ssl_cert" ]]; then
            echo "ERROR: SSL certificate not found: $ssl_cert"
            return 1
        fi
        
        if [[ ! -f "$ssl_key" ]]; then
            echo "ERROR: SSL private key not found: $ssl_key"
            return 1
        fi
        
        # Check certificate permissions
        if [[ ! -r "$ssl_cert" ]]; then
            echo "ERROR: SSL certificate not readable"
            return 1
        fi
        
        if [[ ! -r "$ssl_key" ]]; then
            echo "ERROR: SSL private key not readable"
            return 1
        fi
    fi
    
    return 0
}
```

**3. CORS Configuration Issues**
```bash
# Validate CORS configuration
validate_cors_config() {
    local cors_origin="$1"
    
    if [[ "$cors_origin" == "*" ]]; then
        echo "WARNING: CORS origin set to '*' - this allows all origins"
        echo "Consider restricting to specific domains for security"
    fi
    
    # Validate origin format
    if [[ "$cors_origin" != "*" ]] && [[ ! "$cors_origin" =~ ^https?:// ]]; then
        echo "ERROR: Invalid CORS origin format: $cors_origin"
        echo "Expected format: https://example.com or *"
        return 1
    fi
    
    return 0
}
```

## 🔒 **Security Best Practices**

### **Web Security Checklist**
```bash
# Security validation
validate_web_security() {
    echo "Validating web security configuration..."
    
    # Check SSL configuration
    if [[ "$ssl_enabled" == "true" ]]; then
        echo "✓ SSL/TLS enabled"
        validate_ssl_setup
    else
        echo "⚠ SSL/TLS not enabled - consider enabling for production"
    fi
    
    # Check security headers
    if [[ "$security_headers_enabled" == "true" ]]; then
        echo "✓ Security headers enabled"
    else
        echo "⚠ Security headers not enabled"
    fi
    
    # Check rate limiting
    if [[ -n "$rate_limit" ]]; then
        echo "✓ Rate limiting configured: $rate_limit"
    else
        echo "⚠ Rate limiting not configured"
    fi
    
    # Check CORS policy
    if [[ "$cors_origin" != "*" ]]; then
        echo "✓ CORS origin restricted: $cors_origin"
    else
        echo "⚠ CORS origin set to '*' - consider restricting"
    fi
}
```

## 📈 **Performance Optimization Tips**

### **Web Performance Checklist**
```bash
# Performance validation
validate_web_performance() {
    echo "Validating web performance configuration..."
    
    # Check compression
    if [[ -n "$compression_type" ]]; then
        echo "✓ Compression enabled: $compression_type"
    else
        echo "⚠ Compression not configured"
    fi
    
    # Check caching
    if [[ "$cache_enabled" == "true" ]]; then
        echo "✓ HTTP caching enabled"
        echo "  Max age: ${cache_max_age:-3600} seconds"
    else
        echo "⚠ HTTP caching not enabled"
    fi
    
    # Check load balancing
    if [[ -n "$load_balancer_strategy" ]]; then
        echo "✓ Load balancing enabled: $load_balancer_strategy"
        echo "  Backends: ${#load_balancer_backends[@]}"
    else
        echo "⚠ Load balancing not configured"
    fi
    
    # Check monitoring
    if [[ "$monitoring_enabled" == "true" ]]; then
        echo "✓ Monitoring enabled"
    else
        echo "⚠ Monitoring not enabled"
    fi
}
```

## 🎯 **Next Steps**

- **API Directives**: Learn about API-specific directives
- **Middleware Integration**: Explore web middleware
- **Plugin System**: Understand web plugins
- **Testing Web Directives**: Test web functionality
- **Performance Tuning**: Optimize web performance

---

**Web directives transform your TuskLang configuration into a powerful web server orchestrator. They bring modern web capabilities to your Bash applications with intelligent routing, security, and performance optimization!** 