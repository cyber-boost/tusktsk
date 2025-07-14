# Security Best Practices in TuskLang

Security is paramount in modern applications. TuskLang provides comprehensive security features and follows industry best practices to protect your applications from common vulnerabilities.

## Input Validation and Sanitization

```tusk
# Input validation
class InputValidator {
    static validate(data, rules) {
        errors: {}
        
        for (field, rule in rules) {
            value: data[field]
            
            # Required validation
            if (rule.includes("required") && (!value || value === "")) {
                errors[field]: "Field is required"
                continue
            }
            
            # Type validation
            if (rule.includes("email") && !@is_valid_email(value)) {
                errors[field]: "Invalid email format"
            }
            
            # Length validation
            if (rule.includes("min:")) {
                min_length: @extract_rule_value(rule, "min")
                if (value.length < min_length) {
                    errors[field]: `Minimum length is ${min_length}`
                }
            }
            
            # Pattern validation
            if (rule.includes("pattern:")) {
                pattern: @extract_rule_pattern(rule)
                if (!pattern.test(value)) {
                    errors[field]: "Invalid format"
                }
            }
        }
        
        return {valid: Object.keys(errors).length === 0, errors}
    }
    
    static sanitize(input, type = "string") {
        match type {
            "string" => @strip_tags(@trim(input))
            "html" => @purify_html(input)  # Allow safe HTML only
            "sql" => @escape_sql(input)
            "url" => @filter_url(input)
            "int" => @int(input)
            "float" => @float(input)
            "email" => @filter_email(input)
            "filename" => @sanitize_filename(input)
            _ => input
        }
    }
}

# Secure request handling
#web /user/update method: POST {
    # Validate CSRF token
    @csrf.verify(@request.post._token)
    
    # Validate input
    validation: InputValidator.validate(@request.post, {
        name: "required|min:2|max:100",
        email: "required|email",
        bio: "max:500|pattern:^[a-zA-Z0-9\s.,!?-]*$"
    })
    
    if (!validation.valid) {
        return @json({errors: validation.errors}, 422)
    }
    
    # Sanitize input
    clean_data: {
        name: InputValidator.sanitize(@request.post.name, "string"),
        email: InputValidator.sanitize(@request.post.email, "email"),
        bio: InputValidator.sanitize(@request.post.bio, "html")
    }
    
    # Update user
    @auth.user.update(clean_data)
    
    return @json({message: "Profile updated"})
}
```

## SQL Injection Prevention

```tusk
# Always use parameterized queries
class UserRepository {
    # Good: Parameterized query
    findByEmail(email) {
        return @db.select(
            "SELECT * FROM users WHERE email = ? AND active = ?",
            [email, true]
        ).first()
    }
    
    # Good: Query builder (automatically escaped)
    searchUsers(term) {
        return @User.where("name", "like", "%" + term + "%")
            .where("active", true)
            .get()
    }
    
    # When using raw SQL, validate inputs
    customQuery(user_id, status) {
        # Validate inputs
        if (!@is_numeric(user_id)) {
            throw "Invalid user ID"
        }
        
        if (!@in_array(status, ["active", "inactive", "pending"])) {
            throw "Invalid status"
        }
        
        return @db.select(
            "SELECT * FROM users WHERE id = ? AND status = ?",
            [user_id, status]
        )
    }
    
    # Safe dynamic queries
    buildDynamicQuery(filters) {
        query: @User.query()
        
        # Whitelist allowed filters
        allowed_filters: ["name", "email", "status", "created_at"]
        
        for (field, value in filters) {
            if (!allowed_filters.includes(field)) {
                continue  # Skip unauthorized filters
            }
            
            query.where(field, value)
        }
        
        return query.get()
    }
}
```

## Cross-Site Scripting (XSS) Prevention

```tusk
# Template escaping
# In TuskLang templates, output is escaped by default
#template user_profile {
    <h1>{user.name}</h1>  <!-- Automatically escaped -->
    <p>Bio: {user.bio}</p>  <!-- Automatically escaped -->
    
    <!-- Raw output (use with caution) -->
    <div class="content">{! trusted_content !}</div>
    
    <!-- Specific escaping functions -->
    <script>
        var userName = {@js(user.name)};  // JavaScript escaping
        var userBio = {@attr(user.bio)};  // Attribute escaping
    </script>
}

# Content Security Policy
class SecurityHeaders {
    static apply(response) {
        response.headers["Content-Security-Policy"]: 
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' cdn.example.com; " +
            "style-src 'self' 'unsafe-inline' fonts.googleapis.com; " +
            "img-src 'self' data: cdn.example.com; " +
            "connect-src 'self' api.example.com; " +
            "font-src 'self' fonts.gstatic.com; " +
            "object-src 'none'; " +
            "base-uri 'self';"
        
        response.headers["X-Content-Type-Options"]: "nosniff"
        response.headers["X-Frame-Options"]: "DENY"
        response.headers["X-XSS-Protection"]: "1; mode=block"
        response.headers["Referrer-Policy"]: "strict-origin-when-cross-origin"
        
        return response
    }
}

# Apply security headers middleware
@middleware.global(SecurityHeaders::apply)

# Safe HTML processing
class HtmlSanitizer {
    static clean(html, options = {}) {
        allowed_tags: options.tags || [
            "p", "br", "strong", "em", "ul", "ol", "li", "h1", "h2", "h3"
        ]
        
        allowed_attributes: options.attributes || {
            "a": ["href", "title"],
            "img": ["src", "alt", "title"]
        }
        
        return @html_purifier(html, {
            allowed_tags: allowed_tags,
            allowed_attributes: allowed_attributes,
            remove_blank: true,
            encode_entities: true
        })
    }
}
```

## Cross-Site Request Forgery (CSRF) Protection

```tusk
# CSRF token generation and validation
class CsrfProtection {
    static generate_token() {
        token: @random_bytes(32)
        @session.put("_csrf_token", token)
        return @base64_encode(token)
    }
    
    static verify_token(token) {
        if (!token) {
            throw "CSRF token missing"
        }
        
        session_token: @session.get("_csrf_token")
        if (!session_token) {
            throw "No CSRF token in session"
        }
        
        if (!@hash_equals(session_token, @base64_decode(token))) {
            throw "CSRF token mismatch"
        }
        
        return true
    }
    
    static middleware() {
        return (request, next) => {
            # Skip verification for safe methods
            if (@in_array(request.method, ["GET", "HEAD", "OPTIONS"])) {
                return next()
            }
            
            # Verify token from header or form
            token: request.headers["X-CSRF-TOKEN"] || request.post._token
            
            try {
                this.verify_token(token)
                return next()
            } catch (error) {
                return @response.status(419).json({
                    message: "CSRF token mismatch"
                })
            }
        }
    }
}

# Apply CSRF protection to forms
@middleware.web(CsrfProtection.middleware())

# In templates
#template form {
    <form method="POST" action="/user/update">
        <input type="hidden" name="_token" value="{@csrf_token()}">
        <!-- form fields -->
    </form>
}
```

## Authentication and Authorization

```tusk
# Secure password handling
class PasswordSecurity {
    static hash(password) {
        # Use strong hashing algorithm
        return @password_hash(password, PASSWORD_ARGON2ID, {
            memory_cost: 65536,  # 64 MB
            time_cost: 4,        # 4 iterations
            threads: 3           # 3 threads
        })
    }
    
    static verify(password, hash) {
        return @password_verify(password, hash)
    }
    
    static validate_strength(password) {
        errors: []
        
        if (password.length < 12) {
            errors.push("Password must be at least 12 characters")
        }
        
        if (!/[a-z]/.test(password)) {
            errors.push("Password must contain lowercase letters")
        }
        
        if (!/[A-Z]/.test(password)) {
            errors.push("Password must contain uppercase letters")
        }
        
        if (!/\d/.test(password)) {
            errors.push("Password must contain numbers")
        }
        
        if (!/[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/.test(password)) {
            errors.push("Password must contain special characters")
        }
        
        # Check against common passwords
        if (@is_common_password(password)) {
            errors.push("Password is too common")
        }
        
        return {valid: errors.length === 0, errors}
    }
}

# Secure session management
class SessionSecurity {
    static configure() {
        @session.configure({
            # Use secure session ID generation
            entropy_length: 64,
            
            # Regenerate session ID on login
            regenerate_on_login: true,
            
            # Session timeout
            lifetime: 120,  # 2 hours
            
            # Secure cookies
            cookie: {
                secure: @env.app_env === "production",
                httponly: true,
                samesite: "strict"
            },
            
            # IP validation
            validate_ip: true,
            
            # User agent validation
            validate_user_agent: true
        })
    }
    
    static validate_session() {
        # Check session fingerprint
        expected_fingerprint: @hash([
            @request.ip,
            @request.user_agent,
            @env.app_key
        ])
        
        session_fingerprint: @session.get("_fingerprint")
        
        if (!@hash_equals(expected_fingerprint, session_fingerprint)) {
            @session.invalidate()
            throw "Session validation failed"
        }
    }
}

# Role-based access control
class RoleBasedAccess {
    static check_permission(user, resource, action) {
        # Check direct permissions
        permission: user.permissions.find(p => 
            p.resource === resource && p.action === action
        )
        
        if (permission) {
            return permission.granted
        }
        
        # Check role permissions
        for (role in user.roles) {
            permission: role.permissions.find(p => 
                p.resource === resource && p.action === action
            )
            
            if (permission && permission.granted) {
                return true
            }
        }
        
        return false
    }
    
    static middleware(resource, action) {
        return (request, next) => {
            if (!@auth.check()) {
                return @response.status(401).json({
                    message: "Authentication required"
                })
            }
            
            if (!this.check_permission(@auth.user, resource, action)) {
                return @response.status(403).json({
                    message: "Insufficient permissions"
                })
            }
            
            return next()
        }
    }
}
```

## API Security

```tusk
# Rate limiting
class ApiRateLimit {
    static middleware(requests = 60, window = 60) {
        return (request, next) => {
            key: "rate_limit:" + (request.user?.id || request.ip)
            
            current: @cache.get(key, 0)
            
            if (current >= requests) {
                return @response.status(429).json({
                    message: "Rate limit exceeded",
                    retry_after: window
                })
            }
            
            @cache.increment(key, 1, window)
            
            # Add rate limit headers
            @response.headers["X-RateLimit-Limit"]: requests
            @response.headers["X-RateLimit-Remaining"]: requests - current - 1
            @response.headers["X-RateLimit-Reset"]: Date.now() + window * 1000
            
            return next()
        }
    }
}

# API key authentication
class ApiKeyAuth {
    static middleware() {
        return async (request, next) => {
            api_key: request.headers["x-api-key"]
            
            if (!api_key) {
                return @response.status(401).json({
                    message: "API key required"
                })
            }
            
            # Validate API key
            key_data: await @ApiKey.where("key", api_key)
                .where("active", true)
                .where("expires_at", ">", @now())
                .first()
            
            if (!key_data) {
                return @response.status(401).json({
                    message: "Invalid API key"
                })
            }
            
            # Check rate limits for this key
            if (key_data.rate_limit) {
                rate_key: "api_key_rate:" + key_data.id
                usage: @cache.get(rate_key, 0)
                
                if (usage >= key_data.rate_limit) {
                    return @response.status(429).json({
                        message: "API key rate limit exceeded"
                    })
                }
                
                @cache.increment(rate_key, 1, 3600)
            }
            
            # Log API usage
            @log.info("API key used", {
                key_id: key_data.id,
                endpoint: request.path,
                ip: request.ip
            })
            
            request.api_key: key_data
            return next()
        }
    }
}

# JWT token security
class JwtSecurity {
    static generate(payload, expires_in = 3600) {
        return @jwt.sign(payload, @env.jwt_secret, {
            algorithm: "HS256",
            expiresIn: expires_in,
            issuer: @env.app_name,
            audience: @env.app_url
        })
    }
    
    static verify(token) {
        try {
            return @jwt.verify(token, @env.jwt_secret, {
                algorithms: ["HS256"],
                issuer: @env.app_name,
                audience: @env.app_url
            })
        } catch (error) {
            throw "Invalid token: " + error.message
        }
    }
    
    static blacklist(token) {
        # Add token to blacklist
        decoded: @jwt.decode(token, {complete: true})
        jti: decoded.payload.jti
        exp: decoded.payload.exp
        
        @cache.put("blacklist:" + jti, true, exp - Date.now())
    }
    
    static is_blacklisted(token) {
        decoded: @jwt.decode(token, {complete: true})
        jti: decoded.payload.jti
        
        return @cache.has("blacklist:" + jti)
    }
}
```

## File Upload Security

```tusk
# Secure file upload handling
class SecureFileUpload {
    static validate(file, options = {}) {
        errors: []
        
        # Check file size
        max_size: options.max_size || 10 * 1024 * 1024  # 10MB
        if (file.size > max_size) {
            errors.push("File too large")
        }
        
        # Check file type
        allowed_types: options.allowed_types || ["image/jpeg", "image/png", "image/gif"]
        if (!allowed_types.includes(file.type)) {
            errors.push("File type not allowed")
        }
        
        # Check file extension
        allowed_extensions: options.allowed_extensions || [".jpg", ".jpeg", ".png", ".gif"]
        extension: @path.extname(file.name).toLowerCase()
        if (!allowed_extensions.includes(extension)) {
            errors.push("File extension not allowed")
        }
        
        # Verify MIME type matches extension
        if (!@verify_mime_type(file.type, extension)) {
            errors.push("File type mismatch")
        }
        
        return {valid: errors.length === 0, errors}
    }
    
    static sanitize_filename(filename) {
        # Remove dangerous characters
        clean: filename.replace(/[^a-zA-Z0-9._-]/g, "")
        
        # Prevent directory traversal
        clean: clean.replace(/\.\.+/g, ".")
        
        # Ensure extension is safe
        extension: @path.extname(clean)
        name: @path.basename(clean, extension)
        
        # Limit length
        if (name.length > 100) {
            name: name.substring(0, 100)
        }
        
        return name + extension
    }
    
    static scan_for_malware(file_path) {
        # Integrate with antivirus scanner
        return @clamav.scan(file_path)
    }
    
    static process_upload(file, destination) {
        # Validate file
        validation: this.validate(file)
        if (!validation.valid) {
            throw "File validation failed: " + validation.errors.join(", ")
        }
        
        # Generate safe filename
        safe_name: this.sanitize_filename(file.name)
        unique_name: @uuid() + "_" + safe_name
        
        # Store outside web root
        secure_path: @path.join(destination, unique_name)
        
        # Move file
        @move_uploaded_file(file.tmp_name, secure_path)
        
        # Scan for malware
        if (!this.scan_for_malware(secure_path)) {
            @unlink(secure_path)
            throw "File failed security scan"
        }
        
        # Store file metadata securely
        return {
            original_name: file.name,
            stored_name: unique_name,
            path: secure_path,
            size: file.size,
            type: file.type
        }
    }
}
```

## Security Monitoring and Logging

```tusk
# Security event logging
class SecurityLogger {
    static log_security_event(event_type, details) {
        @log.security(event_type, {
            timestamp: @now(),
            ip: @request.ip,
            user_id: @auth.id,
            user_agent: @request.user_agent,
            session_id: @session.id,
            ...details
        })
        
        # Send to SIEM if critical
        if (@is_critical_event(event_type)) {
            @siem.send(event_type, details)
        }
    }
    
    static log_failed_login(email, reason) {
        this.log_security_event("failed_login", {
            email: email,
            reason: reason
        })
        
        # Track failed attempts
        key: "failed_login:" + @request.ip
        attempts: @cache.increment(key, 1, 900)  # 15 minutes
        
        # Block IP after multiple failures
        if (attempts >= 5) {
            @cache.put("blocked_ip:" + @request.ip, true, 3600)
            this.log_security_event("ip_blocked", {
                reason: "Multiple failed login attempts"
            })
        }
    }
    
    static log_privilege_escalation(user, attempted_action) {
        this.log_security_event("privilege_escalation", {
            user_id: user.id,
            current_roles: user.roles.pluck("name"),
            attempted_action: attempted_action
        })
    }
}

# Intrusion detection
class IntrusionDetection {
    static monitor_requests() {
        return (request, next) => {
            # Check for SQL injection patterns
            if (@detect_sql_injection(request)) {
                SecurityLogger.log_security_event("sql_injection_attempt", {
                    url: request.url,
                    payload: request.all()
                })
                
                return @response.status(400).json({
                    message: "Malicious request detected"
                })
            }
            
            # Check for XSS patterns
            if (@detect_xss_attempt(request)) {
                SecurityLogger.log_security_event("xss_attempt", {
                    url: request.url,
                    payload: request.all()
                })
            }
            
            # Check for path traversal
            if (@detect_path_traversal(request)) {
                SecurityLogger.log_security_event("path_traversal_attempt", {
                    url: request.url
                })
                
                return @response.status(400).json({
                    message: "Invalid request path"
                })
            }
            
            return next()
        }
    }
}
```

## Best Practices Summary

1. **Input Validation** - Validate and sanitize all user input
2. **SQL Injection Prevention** - Use parameterized queries
3. **XSS Protection** - Escape output and use CSP headers
4. **CSRF Protection** - Use tokens for state-changing operations
5. **Authentication Security** - Strong passwords and secure sessions
6. **Authorization** - Implement proper access controls
7. **HTTPS Everywhere** - Use encryption for all communications
8. **Security Headers** - Implement comprehensive security headers
9. **File Upload Security** - Validate and scan uploaded files
10. **Security Monitoring** - Log and monitor security events

## Related Topics

- `authentication` - User authentication systems
- `authorization` - Access control patterns
- `encryption` - Data encryption techniques
- `security-headers` - HTTP security headers
- `penetration-testing` - Security testing