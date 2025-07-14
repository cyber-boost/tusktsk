# @ Operator Security

Security is paramount when using @ operators. This guide covers best practices for preventing vulnerabilities and securing your TuskLang applications.

## Input Validation

```tusk
# Always validate user input
user_input: @request.post.email

# Bad - direct use without validation
@query("SELECT * FROM users WHERE email = '" + @user_input + "'")  # SQL injection!

# Good - parameterized queries
@query("SELECT * FROM users WHERE email = ?", [@user_input])

# Better - validate first
@if(!@validate.email(@user_input)) {
    @throw("Invalid email format")
}
user: @query("SELECT * FROM users WHERE email = ?", [@user_input])

# Best - comprehensive validation
validate_user_input: (input) => {
    rules: {
        email: "required|email|max:255"
        password: "required|min:8|regex:/[A-Z]/|regex:/[0-9]/"
        name: "required|string|max:100|regex:/^[a-zA-Z\s]+$/"
    }
    
    validation: @validate(@input, @rules)
    @if(!@validation.passes) {
        @throw(@validation.errors)
    }
    
    return @validation.data
}
```

## SQL Injection Prevention

```tusk
# Dangerous patterns to avoid
# NEVER do this:
search: @request.get.q
@query("SELECT * FROM products WHERE name LIKE '%" + @search + "%'")  # SQL injection!

# Safe parameterized queries
# Always use placeholders:
@query("SELECT * FROM products WHERE name LIKE ?", ["%" + @search + "%"])

# Query builder safety
products: @Product
    .where("name", "like", "%" + @search + "%")  # Automatically escaped
    .where("price", "<", @max_price)
    .get()

# Multiple parameters
@query("
    SELECT * FROM orders 
    WHERE user_id = ? 
    AND status = ? 
    AND created_at > ?
", [@user_id, @status, @date])

# Safe dynamic queries
build_safe_query: (filters) => {
    query: @db.table("products")
    
    # Whitelist allowed columns
    allowed_columns: ["name", "category", "price", "status"]
    
    @foreach(@filters as @column => @value) {
        @if(@in_array(@column, @allowed_columns)) {
            @query.where(@column, @value)
        }
    }
    
    return @query.get()
}
```

## XSS Prevention

```tusk
# Escape output by default
user_content: @request.post.comment

# Bad - raw output
<div>{@user_content}</div>  # XSS vulnerability!

# Good - escaped output
<div>{@escape(@user_content)}</div>

# Better - context-aware escaping
<div>{@e(@user_content)}</div>                    # HTML context
<script>var data = {@json(@user_content)};</script>  # JavaScript context
<style>body { font-family: {@css(@font)} }</style>   # CSS context

# Rich text with whitelist
safe_html: @purify(@user_content, {
    allowed_tags: ["p", "br", "strong", "em", "a"]
    allowed_attributes: {
        a: ["href", "title"]
    }
    allowed_protocols: ["http", "https", "mailto"]
})

# Template auto-escaping
@template.config({
    auto_escape: true
    escape_function: @escape_html
})
```

## CSRF Protection

```tusk
# CSRF token generation
@middleware csrf_protection {
    # Generate token if not exists
    @if(!@session.csrf_token) {
        @session.csrf_token: @generate_csrf_token()
    }
    
    # Verify token on state-changing requests
    @if(@in_array(@request.method, ["POST", "PUT", "DELETE", "PATCH"])) {
        token: @request.post._token|@request.headers.x-csrf-token
        
        @if(!@csrf_token_valid(@token)) {
            @response.status: 403
            @throw("CSRF token validation failed")
        }
    }
    
    @next()
}

# Include in forms
<form method="POST" action="/update">
    @csrf()  # Generates hidden input with token
    <!-- form fields -->
</form>

# AJAX requests
fetch("/api/update", {
    method: "POST",
    headers: {
        "X-CSRF-Token": @csrf_token(),
        "Content-Type": "application/json"
    },
    body: JSON.stringify(data)
})
```

## Authentication & Authorization

```tusk
# Secure password handling
register_user: (data) => {
    # Validate password strength
    @if(!@validate_password_strength(@data.password)) {
        @throw("Password does not meet security requirements")
    }
    
    # Never store plain passwords
    user: @User.create({
        email: @data.email
        password: @hash_password(@data.password)  # Bcrypt/Argon2
    })
    
    return @user
}

# Secure authentication
authenticate: (email, password) => {
    user: @User.findBy("email", @email)
    
    @if(!@user || !@verify_password(@password, @user.password)) {
        # Generic error message
        @throw("Invalid credentials")
    }
    
    # Regenerate session ID
    @session_regenerate_id()
    
    # Store minimal session data
    @session.user_id: @user.id
    @session.login_time: @time()
    
    return @user
}

# Authorization checks
@middleware require_permission {
    permission: @params.permission
    
    @if(!@can(@permission)) {
        @response.status: 403
        @throw("Insufficient permissions")
    }
    
    @next()
}
```

## File Upload Security

```tusk
# Secure file upload handling
upload_file: (file) => {
    # Validate file type
    allowed_types: ["image/jpeg", "image/png", "image/gif"]
    @if(!@in_array(@file.type, @allowed_types)) {
        @throw("Invalid file type")
    }
    
    # Validate file size
    max_size: 5242880  # 5MB
    @if(@file.size > @max_size) {
        @throw("File too large")
    }
    
    # Generate secure filename
    extension: @get_file_extension(@file.name)
    filename: @generate_uuid() + "." + @extension
    
    # Scan for malware (if available)
    @if(@has_virus_scanner()) {
        @if(!@scan_file(@file.tmp_name)) {
            @throw("File failed security scan")
        }
    }
    
    # Store outside web root
    upload_path: @storage_path("uploads/" + @filename)
    @move_uploaded_file(@file.tmp_name, @upload_path)
    
    # Verify uploaded file
    @if(!@is_valid_image(@upload_path)) {
        @unlink(@upload_path)
        @throw("Invalid image file")
    }
    
    return @filename
}
```

## API Security

```tusk
# API rate limiting
@middleware rate_limit {
    key: @request.ip|@request.api_key
    limit: 100
    window: 3600  # 1 hour
    
    current: @cache.increment("rate:" + @key)
    
    @if(@current > @limit) {
        @response.status: 429
        @response.headers.retry-after: @window
        @throw("Rate limit exceeded")
    }
    
    @response.headers.x-ratelimit-limit: @limit
    @response.headers.x-ratelimit-remaining: @limit - @current
    
    @next()
}

# API key validation
@middleware api_auth {
    key: @request.headers.x-api-key
    
    @if(!@key) {
        @response.status: 401
        @throw("API key required")
    }
    
    # Validate and get app
    app: @validate_api_key(@key)
    @if(!@app) {
        @response.status: 403
        @throw("Invalid API key")
    }
    
    # Check permissions
    @if(!@app.hasPermission(@request.route)) {
        @response.status: 403
        @throw("Insufficient API permissions")
    }
    
    @context.api_app: @app
    @next()
}
```

## Secure Configuration

```tusk
# Environment variable security
get_secure_config: (key) => {
    value: @env[@key]
    
    # Never expose sensitive keys
    sensitive_keys: ["DB_PASSWORD", "API_SECRET", "PRIVATE_KEY"]
    @if(@in_array(@key, @sensitive_keys) && @env.APP_DEBUG) {
        @log.warning("Attempting to access sensitive config in debug mode")
        return "***REDACTED***"
    }
    
    return @value
}

# Secure defaults
config: {
    session: {
        secure: @env.SESSION_SECURE|true      # HTTPS only
        httponly: @env.SESSION_HTTPONLY|true  # No JS access
        samesite: @env.SESSION_SAMESITE|"Lax" # CSRF protection
        lifetime: @env.SESSION_LIFETIME|120    # Minutes
    }
    
    password: {
        min_length: 8
        require_uppercase: true
        require_numbers: true
        require_special: true
        bcrypt_rounds: 12
    }
    
    security: {
        force_https: @env.FORCE_HTTPS|true
        hsts_max_age: 31536000
        csp_policy: "default-src 'self'"
    }
}
```

## Output Encoding

```tusk
# Context-aware output encoding
encode_for_context: (data, context) => {
    @switch(@context) {
        case "html":
            return @htmlspecialchars(@data, ENT_QUOTES, "UTF-8")
            
        case "attribute":
            return @htmlspecialchars(@data, ENT_QUOTES, "UTF-8")
            
        case "javascript":
            return @json_encode(@data)
            
        case "css":
            return @preg_replace("/[^a-zA-Z0-9\s\-_]/", "", @data)
            
        case "url":
            return @urlencode(@data)
            
        default:
            return @htmlspecialchars(@data, ENT_QUOTES, "UTF-8")
    }
}

# Template helpers
{@html(@user_content)}        # HTML context
{@attr(@user_attribute)}      # Attribute context
{@js(@user_data)}            # JavaScript context
{@css(@user_style)}          # CSS context
{@url(@user_param)}          # URL context
```

## Security Headers

```tusk
# Security headers middleware
@middleware security_headers {
    headers: {
        # Prevent XSS
        "X-XSS-Protection": "1; mode=block"
        "X-Content-Type-Options": "nosniff"
        
        # Prevent clickjacking
        "X-Frame-Options": "SAMEORIGIN"
        
        # HTTPS enforcement
        "Strict-Transport-Security": "max-age=31536000; includeSubDomains"
        
        # Content Security Policy
        "Content-Security-Policy": @build_csp_policy({
            default: ["self"]
            script: ["self", "unsafe-inline", "https://trusted-cdn.com"]
            style: ["self", "unsafe-inline"]
            img: ["self", "data:", "https:"]
            font: ["self", "https://fonts.gstatic.com"]
        })
        
        # Referrer Policy
        "Referrer-Policy": "strict-origin-when-cross-origin"
        
        # Permissions Policy
        "Permissions-Policy": "geolocation=(), microphone=(), camera=()"
    }
    
    @foreach(@headers as @name => @value) {
        @response.headers[@name]: @value
    }
    
    @next()
}
```

## Logging and Monitoring

```tusk
# Security event logging
log_security_event: (event_type, details) => {
    @log.security({
        type: @event_type
        timestamp: @timestamp()
        ip: @request.ip
        user_id: @session.user_id
        user_agent: @request.headers.user_agent
        details: @details
        request_id: @request.id
    })
    
    # Alert on critical events
    critical_events: ["multiple_login_failures", "privilege_escalation", "sql_injection_attempt"]
    @if(@in_array(@event_type, @critical_events)) {
        @alert_security_team(@event_type, @details)
    }
}

# Monitor suspicious activity
@on("login_failed", (data) => {
    key: "login_failures:" + @data.ip
    failures: @cache.increment(@key, 1, 3600)
    
    @if(@failures > 5) {
        @log_security_event("multiple_login_failures", {
            ip: @data.ip
            attempts: @failures
        })
        
        # Temporary block
        @cache.set("blocked:" + @data.ip, true, 1800)  # 30 minutes
    }
})
```

## Best Practices

1. **Never trust user input** - Always validate and sanitize
2. **Use parameterized queries** - Prevent SQL injection
3. **Escape output** - Prevent XSS attacks
4. **Implement CSRF protection** - For state-changing operations
5. **Hash passwords properly** - Use bcrypt or Argon2
6. **Set security headers** - Defense in depth
7. **Log security events** - Monitor for attacks
8. **Keep dependencies updated** - Patch vulnerabilities
9. **Use HTTPS everywhere** - Encrypt data in transit
10. **Principle of least privilege** - Limit access rights

## Related Features

- `@validate()` - Input validation
- `@escape()` - Output escaping
- `@hash_password()` - Password hashing
- `@verify_password()` - Password verification
- `@csrf()` - CSRF token management