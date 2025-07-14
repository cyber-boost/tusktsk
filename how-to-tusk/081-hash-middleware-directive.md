# #middleware - Request Middleware Directive

The `#middleware` directive creates reusable request processing logic that runs before your main handlers, perfect for authentication, logging, validation, and request transformation.

## Basic Syntax

```tusk
# Define middleware
#middleware auth {
    if (!@session.user_id) {
        @redirect("/login")
        return false  # Stop processing
    }
    
    # Set user context
    @request.user: @User.find(@session.user_id)
    return true  # Continue to next middleware/handler
}

# Use middleware in routes
#web /dashboard middleware: [auth] {
    # This only runs if auth middleware passes
    @render("dashboard.tusk", {user: @request.user})
}
```

## Middleware Flow

```tusk
# Middleware execution order
#middleware first {
    @log("First middleware - before")
    
    # Call next middleware/handler
    result: @next()
    
    @log("First middleware - after")
    return result
}

#middleware second {
    @log("Second middleware - before")
    result: @next()
    @log("Second middleware - after")
    return result
}

#web /test middleware: [first, second] {
    @log("Main handler")
    return "Done"
}

# Output order:
# First middleware - before
# Second middleware - before  
# Main handler
# Second middleware - after
# First middleware - after
```

## Request Modification

```tusk
# Add data to request
#middleware add_metadata {
    @request.request_id: @generate_uuid()
    @request.start_time: @microtime(true)
    @request.ip: @get_real_ip()
    
    # Add to all views
    @view.share({
        app_name: @config.app.name,
        user: @request.user,
        csrf_token: @csrf_token()
    })
    
    @next()
}

# Transform request data
#middleware sanitize_input {
    # Sanitize all input
    if (@request.method == "POST" || @request.method == "PUT") {
        @request.post: @sanitize_data(@request.post)
    }
    
    @request.query: @sanitize_data(@request.query)
    
    @next()
}

sanitize_data: (data) => {
    if (is_array(data)) {
        return data.map(item => @sanitize_data(item))
    } else if (is_object(data)) {
        result: {}
        for (key, value in data) {
            result[key]: @sanitize_data(value)
        }
        return result
    } else if (is_string(data)) {
        return @strip_tags(@trim(data))
    }
    return data
}
```

## Response Modification

```tusk
# Modify response headers
#middleware security_headers {
    # Process request
    @next()
    
    # Add security headers to response
    @response.headers["X-Content-Type-Options"]: "nosniff"
    @response.headers["X-Frame-Options"]: "SAMEORIGIN"
    @response.headers["X-XSS-Protection"]: "1; mode=block"
    @response.headers["Referrer-Policy"]: "strict-origin-when-cross-origin"
    
    if (@request.secure) {
        @response.headers["Strict-Transport-Security"]: 
            "max-age=31536000; includeSubDomains"
    }
}

# Compress response
#middleware compression {
    # Check if client accepts gzip
    accept_encoding: @request.headers["accept-encoding"] || ""
    
    if (!accept_encoding.includes("gzip")) {
        return @next()
    }
    
    # Process request
    @next()
    
    # Compress response body
    if (@response.body && @strlen(@response.body) > 1024) {
        compressed: @gzencode(@response.body, 6)
        @response.body: compressed
        @response.headers["Content-Encoding"]: "gzip"
        @response.headers["Vary"]: "Accept-Encoding"
    }
}
```

## Authentication Middleware

```tusk
# Basic authentication
#middleware auth {
    # Check session
    if (!@session.user_id) {
        # Check remember me cookie
        remember_token: @cookie.remember_me
        
        if (remember_token) {
            user: @validate_remember_token(remember_token)
            if (user) {
                @session.user_id: user.id
                @request.user: user
                return @next()
            }
        }
        
        # Not authenticated
        @session.intended_url: @request.url
        @redirect("/login")
        return false
    }
    
    # Load user
    @request.user: @User.find(@session.user_id)
    
    if (!@request.user) {
        @session.forget("user_id")
        @redirect("/login")
        return false
    }
    
    @next()
}

# Role-based access
#middleware role {
    required_role: @params.role || "user"
    
    if (!@request.user) {
        @response.status: 401
        return @json({error: "Unauthenticated"})
    }
    
    if (!@request.user.hasRole(required_role)) {
        @response.status: 403
        return @json({error: "Insufficient permissions"})
    }
    
    @next()
}

# API token authentication
#middleware api_auth {
    token: @request.headers.authorization?.replace("Bearer ", "")
    
    if (!token) {
        @response.status: 401
        return @json({error: "No token provided"})
    }
    
    # Validate token
    payload: @validate_jwt(token)
    
    if (!payload) {
        @response.status: 401
        return @json({error: "Invalid token"})
    }
    
    # Set user context
    @request.user: @User.find(payload.user_id)
    @request.token: payload
    
    @next()
}
```

## Rate Limiting

```tusk
# Rate limiting middleware
#middleware rate_limit {
    # Get limits from params or defaults
    max_attempts: @params.attempts || 60
    decay_minutes: @params.decay || 1
    
    # Generate key
    key: "rate_limit:" + @request.ip + ":" + @request.path
    
    # Check attempts
    attempts: @cache.get(key, 0)
    
    if (attempts >= max_attempts) {
        @response.status: 429
        @response.headers["Retry-After"]: decay_minutes * 60
        @response.headers["X-RateLimit-Limit"]: max_attempts
        @response.headers["X-RateLimit-Remaining"]: 0
        
        return @json({
            error: "Too many requests",
            retry_after: decay_minutes * 60
        })
    }
    
    # Increment attempts
    @cache.increment(key, 1, decay_minutes * 60)
    
    # Add headers
    @response.headers["X-RateLimit-Limit"]: max_attempts
    @response.headers["X-RateLimit-Remaining"]: max_attempts - attempts - 1
    
    @next()
}

# Per-user rate limiting
#middleware user_rate_limit {
    if (!@request.user) {
        return @next()  # Skip for non-authenticated
    }
    
    key: "user_rate:" + @request.user.id + ":" + @request.path
    limit: @request.user.isPremium() ? 1000 : 100
    
    attempts: @cache.get(key, 0)
    
    if (attempts >= limit) {
        @response.status: 429
        return @json({error: "Rate limit exceeded for user"})
    }
    
    @cache.increment(key, 1, 3600)  # 1 hour window
    @next()
}
```

## CORS Middleware

```tusk
# CORS handling
#middleware cors {
    origin: @request.headers.origin
    
    # Configure allowed origins
    allowed_origins: @params.origins || ["*"]
    allowed_methods: @params.methods || ["GET", "POST", "PUT", "DELETE", "OPTIONS"]
    allowed_headers: @params.headers || ["Content-Type", "Authorization"]
    max_age: @params.max_age || 86400
    
    # Check origin
    if (allowed_origins[0] != "*" && !allowed_origins.includes(origin)) {
        return @next()  # Don't add CORS headers
    }
    
    # Set CORS headers
    @response.headers["Access-Control-Allow-Origin"]: 
        allowed_origins[0] == "*" ? "*" : origin
    @response.headers["Access-Control-Allow-Methods"]: 
        allowed_methods.join(", ")
    @response.headers["Access-Control-Allow-Headers"]: 
        allowed_headers.join(", ")
    @response.headers["Access-Control-Max-Age"]: max_age
    @response.headers["Access-Control-Allow-Credentials"]: "true"
    
    # Handle preflight
    if (@request.method == "OPTIONS") {
        @response.status: 204
        return  # Don't continue to handler
    }
    
    @next()
}
```

## Logging Middleware

```tusk
# Request logging
#middleware logging {
    start_time: @microtime(true)
    request_id: @generate_uuid()
    
    # Log request
    @log.info("Request started", {
        id: request_id,
        method: @request.method,
        path: @request.path,
        ip: @request.ip,
        user_id: @request.user?.id
    })
    
    # Process request
    @next()
    
    # Log response
    duration: @microtime(true) - start_time
    
    @log.info("Request completed", {
        id: request_id,
        status: @response.status,
        duration: duration,
        memory: @memory_get_peak_usage(true)
    })
    
    # Add timing header
    @response.headers["X-Response-Time"]: duration + "s"
    @response.headers["X-Request-ID"]: request_id
}

# Detailed logging for debugging
#middleware debug_logging if: @env.debug {
    @log.debug("Request details", {
        headers: @request.headers,
        query: @request.query,
        post: @request.post,
        cookies: @request.cookies
    })
    
    @next()
    
    @log.debug("Response details", {
        status: @response.status,
        headers: @response.headers,
        body_size: @strlen(@response.body)
    })
}
```

## Validation Middleware

```tusk
# Input validation
#middleware validate {
    rules: @params.rules || {}
    
    # Combine all input
    input: {...@request.query, ...@request.post}
    
    # Validate
    validation: @validator.make(input, rules)
    
    if (!validation.passes()) {
        @response.status: 422
        return @json({
            message: "Validation failed",
            errors: validation.errors()
        })
    }
    
    # Add validated data to request
    @request.validated: validation.validated()
    
    @next()
}

# Custom validation middleware
#middleware validate_json {
    if (@request.headers["content-type"] != "application/json") {
        @response.status: 400
        return @json({error: "Content-Type must be application/json"})
    }
    
    # Try to parse JSON body
    try {
        @request.json: @json_decode(@request.body)
    } catch {
        @response.status: 400
        return @json({error: "Invalid JSON in request body"})
    }
    
    @next()
}
```

## Cache Middleware

```tusk
# Page caching
#middleware cache {
    duration: @params.duration || 3600
    
    # Generate cache key
    key: "page:" + @request.method + ":" + @request.url
    
    # Check cache
    cached: @cache.get(key)
    
    if (cached) {
        @response.body: cached.body
        @response.headers: cached.headers
        @response.headers["X-Cache"]: "HIT"
        return  # Don't continue to handler
    }
    
    # Process request
    @next()
    
    # Cache successful responses
    if (@response.status == 200) {
        @cache.put(key, {
            body: @response.body,
            headers: @response.headers
        }, duration)
    }
    
    @response.headers["X-Cache"]: "MISS"
}

# Conditional caching
#middleware smart_cache {
    # Don't cache for authenticated users
    if (@request.user) {
        return @next()
    }
    
    # Don't cache POST requests
    if (@request.method != "GET") {
        return @next()
    }
    
    # Use standard cache middleware
    @apply_middleware("cache", {duration: 1800})
}
```

## Maintenance Mode

```tusk
# Maintenance mode middleware
#middleware maintenance {
    if (!@env.maintenance_mode) {
        return @next()
    }
    
    # Allow certain IPs
    allowed_ips: @config.maintenance.allowed_ips || []
    if (allowed_ips.includes(@request.ip)) {
        return @next()
    }
    
    # Allow certain routes
    allowed_routes: ["/maintenance", "/api/health"]
    if (allowed_routes.includes(@request.path)) {
        return @next()
    }
    
    @response.status: 503
    @response.headers["Retry-After"]: "3600"
    
    if (@request.wants_json()) {
        return @json({
            error: "Maintenance mode",
            message: "We'll be back soon!"
        })
    }
    
    @render("maintenance.tusk")
}
```

## Middleware Groups

```tusk
# Define middleware groups
middleware_groups: {
    web: [
        "session",
        "csrf",
        "security_headers"
    ],
    
    api: [
        "api_auth",
        "rate_limit:100,1",
        "cors"
    ],
    
    admin: [
        "auth",
        "role:admin",
        "logging"
    ]
}

# Apply group to routes
#web /admin/* middleware: @middleware_groups.admin {
    # All admin routes use admin middleware group
}

# Custom middleware pipeline
#middleware pipeline {
    middlewares: @params.middlewares || []
    
    # Execute each middleware
    for (mw in middlewares) {
        if (!@apply_middleware(mw)) {
            return false  # Stop if middleware fails
        }
    }
    
    @next()
}
```

## Testing Middleware

```tusk
# Test mode middleware
#middleware test_mode if: @env.testing {
    # Add test helpers
    @request.is_test: true
    
    # Allow test authentication
    if (@request.headers["x-test-user"]) {
        user_id: @request.headers["x-test-user"]
        @request.user: @User.find(user_id)
        @session.user_id: user_id
    }
    
    # Disable rate limiting
    @request.skip_rate_limit: true
    
    @next()
}

# Feature flag middleware
#middleware feature {
    feature: @params.feature
    
    if (!feature || !@features.enabled(feature)) {
        @response.status: 404
        return @json({error: "Feature not available"})
    }
    
    @next()
}
```

## Best Practices

1. **Keep middleware focused** - Single responsibility per middleware
2. **Order matters** - Place middleware in correct sequence
3. **Handle errors gracefully** - Don't break the request flow
4. **Use parameters** - Make middleware configurable
5. **Cache when possible** - Avoid repeated operations
6. **Test thoroughly** - Middleware affects many routes
7. **Document behavior** - Explain what middleware does
8. **Monitor performance** - Middleware runs on every request

## Related Topics

- `hash-web-directive` - Web endpoints
- `hash-api-directive` - API endpoints  
- `authentication` - Auth systems
- `rate-limiting` - Request throttling
- `cors` - Cross-origin requests