# @server - Server Variables

The `@server` operator provides access to server information and PHP's $_SERVER superglobal, offering details about the server environment and request execution.

## Basic Syntax

```tusk
# Access server variables
server_name: @server.SERVER_NAME
document_root: @server.DOCUMENT_ROOT

# Common server info
method: @server.REQUEST_METHOD
script: @server.SCRIPT_NAME
```

## Request Information

```tusk
# Request details
request_info: {
    # HTTP method
    method: @server.REQUEST_METHOD
    
    # Request URI and query
    uri: @server.REQUEST_URI
    query_string: @server.QUERY_STRING
    
    # Script information
    script_name: @server.SCRIPT_NAME
    script_filename: @server.SCRIPT_FILENAME
    php_self: @server.PHP_SELF
    
    # Request time
    time: @server.REQUEST_TIME
    time_float: @server.REQUEST_TIME_FLOAT
    
    # Protocol
    protocol: @server.SERVER_PROTOCOL
    https: @server.HTTPS == "on"
}

# Client information
client_info: {
    # IP addresses
    remote_addr: @server.REMOTE_ADDR
    remote_host: @server.REMOTE_HOST
    remote_port: @server.REMOTE_PORT
    
    # Forwarded IPs (behind proxy)
    forwarded_for: @server.HTTP_X_FORWARDED_FOR
    real_ip: @server.HTTP_X_REAL_IP
    
    # User agent
    user_agent: @server.HTTP_USER_AGENT
    
    # Accept headers
    accept: @server.HTTP_ACCEPT
    accept_language: @server.HTTP_ACCEPT_LANGUAGE
    accept_encoding: @server.HTTP_ACCEPT_ENCODING
}
```

## Server Environment

```tusk
# Server details
server_details: {
    # Software
    software: @server.SERVER_SOFTWARE
    gateway: @server.GATEWAY_INTERFACE
    
    # Server identification
    name: @server.SERVER_NAME
    addr: @server.SERVER_ADDR
    port: @server.SERVER_PORT
    admin: @server.SERVER_ADMIN
    
    # Paths
    document_root: @server.DOCUMENT_ROOT
    context_prefix: @server.CONTEXT_PREFIX
    context_document_root: @server.CONTEXT_DOCUMENT_ROOT
    
    # PHP information
    php_version: PHP_VERSION
    php_os: PHP_OS
    php_sapi: PHP_SAPI
}
```

## Authentication Headers

```tusk
# HTTP authentication
auth_info: {
    # Basic auth
    auth_user: @server.PHP_AUTH_USER
    auth_pass: @server.PHP_AUTH_PW
    auth_type: @server.AUTH_TYPE
    
    # Digest auth
    auth_digest: @server.PHP_AUTH_DIGEST
    
    # Remote user (from web server)
    remote_user: @server.REMOTE_USER
}

# Check authentication
is_authenticated: @isset(@server.PHP_AUTH_USER)

# Validate basic auth
validate_basic_auth: () => {
    @if(!@is_authenticated) {
        @response.status: 401
        @response.headers.www-authenticate: 'Basic realm="Restricted Area"'
        return false
    }
    
    # Verify credentials
    user: @server.PHP_AUTH_USER
    pass: @server.PHP_AUTH_PW
    
    valid: @check_credentials(@user, @pass)
    @if(!@valid) {
        @response.status: 403
        return false
    }
    
    return true
}
```

## Path Information

```tusk
# Path details
paths: {
    # Original request
    request_uri: @server.REQUEST_URI
    
    # Path components
    path_info: @server.PATH_INFO
    orig_path_info: @server.ORIG_PATH_INFO
    path_translated: @server.PATH_TRANSLATED
    
    # Script paths
    script_name: @server.SCRIPT_NAME
    script_filename: @server.SCRIPT_FILENAME
    
    # Parse path
    parsed: @parse_url(@server.REQUEST_URI)
    path: @parsed.path
    query: @parsed.query
}

# Get clean path
clean_path: () => {
    path: @server.PATH_INFO|@server.REQUEST_URI
    
    # Remove query string
    @if(@contains(@path, "?")) {
        path: @explode("?", @path)[0]
    }
    
    # Remove trailing slash
    path: @rtrim(@path, "/")
    
    return @path ?: "/"
}
```

## CLI Detection

```tusk
# Check if running in CLI
is_cli: @server.argc > 0 || PHP_SAPI == "cli"

# CLI arguments
@if(@is_cli) {
    cli_args: @server.argv
    script_name: @cli_args[0]
    arguments: @array_slice(@cli_args, 1)
    
    # Parse CLI options
    options: @getopt("hv:f:", ["help", "verbose:", "file:"])
}

# Different behavior for CLI vs Web
@if(@is_cli) {
    # CLI mode
    @output.format: "text"
    @log.destination: "stdout"
} else {
    # Web mode
    @output.format: "html"
    @log.destination: "file"
}
```

## Request Timing

```tusk
# Performance monitoring
request_timing: {
    # Start time
    start_time: @server.REQUEST_TIME_FLOAT
    
    # Current time
    current_time: @microtime(true)
    
    # Elapsed time
    elapsed: @current_time - @start_time
    
    # Memory usage
    memory_current: @memory_get_usage()
    memory_peak: @memory_get_peak_usage()
}

# Log slow requests
@if(@request_timing.elapsed > 1.0) {
    @log.warning("Slow request", {
        uri: @server.REQUEST_URI
        elapsed: @request_timing.elapsed
        memory: @request_timing.memory_peak
    })
}
```

## Custom Headers

```tusk
# Access all HTTP headers
headers: {}
@foreach(@server as @key => @value) {
    @if(@starts_with(@key, "HTTP_")) {
        # Convert HTTP_HEADER_NAME to Header-Name
        header_name: @substr(@key, 5)
        header_name: @str_replace("_", "-", @header_name)
        header_name: @ucwords(@strtolower(@header_name), "-")
        
        @headers[@header_name]: @value
    }
}

# Custom application headers
app_headers: {
    request_id: @server.HTTP_X_REQUEST_ID
    api_version: @server.HTTP_X_API_VERSION
    client_version: @server.HTTP_X_CLIENT_VERSION
    device_id: @server.HTTP_X_DEVICE_ID
}
```

## Security Checks

```tusk
# Security validation
security_checks: {
    # Check for proxy headers
    is_proxied: @isset(@server.HTTP_X_FORWARDED_FOR) || 
                @isset(@server.HTTP_X_REAL_IP)
    
    # Get real IP
    real_ip: @get_real_ip()
    
    # SSL/TLS check
    is_https: @server.HTTPS == "on" || 
              @server.HTTP_X_FORWARDED_PROTO == "https" ||
              @server.SERVER_PORT == 443
    
    # Validate host header
    valid_host: @in_array(@server.HTTP_HOST, @allowed_hosts)
}

# Get real IP helper
get_real_ip: () => {
    # Check various headers in order
    headers: [
        "HTTP_CF_CONNECTING_IP",     # Cloudflare
        "HTTP_X_REAL_IP",            # Nginx
        "HTTP_X_FORWARDED_FOR",      # Standard proxy
        "REMOTE_ADDR"                # Direct connection
    ]
    
    @foreach(@headers as @header) {
        @if(@isset(@server[@header])) {
            ip: @server[@header]
            
            # Handle comma-separated list
            @if(@contains(@ip, ",")) {
                ip: @trim(@explode(",", @ip)[0])
            }
            
            # Validate IP
            @if(@filter_var(@ip, FILTER_VALIDATE_IP)) {
                return @ip
            }
        }
    }
    
    return @server.REMOTE_ADDR
}
```

## Environment Detection

```tusk
# Detect execution environment
detect_environment: {
    # Check various indicators
    is_docker: @file_exists("/.dockerenv")
    is_kubernetes: @isset(@env.KUBERNETES_SERVICE_HOST)
    is_aws: @isset(@env.AWS_EXECUTION_ENV)
    is_heroku: @isset(@env.DYNO)
    
    # Web server detection
    server_software: @server.SERVER_SOFTWARE
    is_apache: @contains(@server_software, "Apache")
    is_nginx: @contains(@server_software, "nginx")
    is_iis: @contains(@server_software, "IIS")
    
    # PHP SAPI
    sapi: PHP_SAPI
    is_fpm: @sapi == "fpm-fcgi"
    is_cgi: @contains(@sapi, "cgi")
    is_builtin: @sapi == "cli-server"
}
```

## Development Server

```tusk
# Built-in PHP server detection
is_dev_server: PHP_SAPI == "cli-server"

@if(@is_dev_server) {
    # Special handling for PHP built-in server
    # Static file serving
    @if(@file_exists(@server.DOCUMENT_ROOT + @server.REQUEST_URI)) {
        return false  # Let PHP serve the file
    }
    
    # Enable development features
    @ini_set("display_errors", 1)
    @error_reporting(E_ALL)
}
```

## Server Metrics

```tusk
# Collect server metrics
server_metrics: {
    # System load
    load_average: @sys_getloadavg()
    
    # Disk usage
    disk_free: @disk_free_space("/")
    disk_total: @disk_total_space("/")
    disk_usage_percent: ((@disk_total - @disk_free) / @disk_total) * 100
    
    # Memory info
    memory_limit: @ini_get("memory_limit")
    memory_usage: @memory_get_usage(true)
    memory_peak: @memory_get_peak_usage(true)
    
    # Connection info
    connection_status: @connection_status()
    connection_aborted: @connection_aborted()
}

# Health check endpoint
#api /health {
    @json({
        status: "healthy"
        server: {
            software: @server.SERVER_SOFTWARE
            php_version: PHP_VERSION
            uptime: @server.REQUEST_TIME - @server.SERVER_START_TIME
        }
        metrics: @server_metrics
        timestamp: @time()
    })
}
```

## Best Practices

1. **Validate server variables** - May not always be set
2. **Use real IP detection** - Handle proxies properly
3. **Check HTTPS properly** - Consider proxy headers
4. **Sanitize user input** - Even from server vars
5. **Cache server checks** - Don't repeat expensive operations
6. **Handle CLI mode** - Different variables available

## Related Features

- `@request` - Request object
- `@env` - Environment variables
- `@headers` - HTTP headers
- `@cli` - CLI arguments
- `@system` - System information