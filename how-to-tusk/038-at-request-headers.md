# @request.headers - HTTP Headers Access

The `@request.headers` operator provides access to HTTP request headers, enabling authentication, content negotiation, and custom header processing.

## Basic Syntax

```tusk
# Access specific header
user_agent: @request.headers.user-agent

# Case-insensitive access
auth_header: @request.headers.authorization

# With fallback
accept_lang: @request.headers.accept-language|"en-US"
```

## Common Headers

```tusk
# Browser information
browser_info: {
    user_agent: @request.headers.user-agent
    accept: @request.headers.accept
    accept_language: @request.headers.accept-language
    accept_encoding: @request.headers.accept-encoding
}

# Request metadata
request_meta: {
    host: @request.headers.host
    referer: @request.headers.referer
    origin: @request.headers.origin
    content_type: @request.headers.content-type
    content_length: @request.headers.content-length
}
```

## Authentication Headers

```tusk
# Bearer token authentication
auth_header: @request.headers.authorization

@if(@auth_header && @starts_with(@auth_header, "Bearer ")) {
    token: @substr(@auth_header, 7)
    user: @verify_jwt_token(@token)
    
    @if(@user) {
        # Authenticated request
        @request.user: @user
    } else {
        @response.status: 401
        error: "Invalid token"
    }
} else {
    @response.status: 401
    error: "Authorization required"
}
```

## API Key Authentication

```tusk
# Check API key in header
api_key: @request.headers.x-api-key

# Validate API key
valid_key: @query("SELECT * FROM api_keys WHERE key = ? AND active = 1", [@api_key])

@if(!@valid_key) {
    @response.status: 403
    error: "Invalid API key"
}

# Track API usage
@query("UPDATE api_keys SET last_used = NOW(), calls = calls + 1 WHERE key = ?", [@api_key])
```

## Custom Headers

```tusk
# Application-specific headers
app_headers: {
    version: @request.headers.x-app-version
    platform: @request.headers.x-platform
    device_id: @request.headers.x-device-id
}

# Webhook signatures
webhook_signature: @request.headers.x-webhook-signature
webhook_timestamp: @request.headers.x-webhook-timestamp

# Validate webhook
expected_signature: @hmac_sha256(@webhook_secret, @request.body + @webhook_timestamp)
is_valid: @webhook_signature == @expected_signature
```

## Content Negotiation

```tusk
# Determine response format
accept_header: @request.headers.accept

@if(@contains(@accept_header, "application/json")) {
    response_format: "json"
} elseif(@contains(@accept_header, "text/html")) {
    response_format: "html"
} else {
    response_format: "text"
}

# Language preference
languages: @explode(",", @request.headers.accept-language)
preferred_language: @trim(@languages[0])
```

## CORS Headers

```tusk
# Handle CORS preflight
@if(@request.method == "OPTIONS") {
    @response.headers.access-control-allow-origin: @request.headers.origin|"*"
    @response.headers.access-control-allow-methods: "GET, POST, PUT, DELETE, OPTIONS"
    @response.headers.access-control-allow-headers: "Content-Type, Authorization"
    @response.headers.access-control-max-age: 86400
    @response.status: 204
}

# Regular CORS headers
@response.headers.access-control-allow-origin: @request.headers.origin|"*"
```

## Security Headers

```tusk
# Check security headers
security_check: {
    has_csrf: @request.headers.x-csrf-token
    has_nonce: @request.headers.x-nonce
    is_secure: @request.headers.x-forwarded-proto == "https"
}

# Validate CSRF token
@if(@request.method == "POST" && !@security_check.has_csrf) {
    @response.status: 403
    error: "CSRF token required"
}

# Check if behind proxy
real_ip: @request.headers.x-forwarded-for|@request.headers.x-real-ip|@request.ip
```

## Rate Limiting

```tusk
# Identify client for rate limiting
client_id: @request.headers.x-api-key|@request.ip

# Check rate limit
rate_limit: @cache.get("rate_limit:" + @client_id)|0

@if(@rate_limit >= 100) {
    @response.status: 429
    @response.headers.x-ratelimit-limit: 100
    @response.headers.x-ratelimit-remaining: 0
    @response.headers.retry-after: 3600
    error: "Rate limit exceeded"
}

# Increment counter
@cache.increment("rate_limit:" + @client_id, 1, 3600)
```

## Debugging Headers

```tusk
# Log all headers for debugging
@if(@debug_mode) {
    all_headers: @request.headers
    @log("Request Headers", @all_headers)
    
    # Add debug response headers
    @response.headers.x-debug-request-id: @generate_uuid()
    @response.headers.x-debug-timestamp: @timestamp
}
```

## Common Patterns

```tusk
# Mobile app API endpoint
#api /app/data {
    # Require app headers
    @if(!@request.headers.x-app-version || !@request.headers.x-platform) {
        @response.status: 400
        error: "Missing required app headers"
    }
    
    # Version compatibility check
    min_version: "2.0.0"
    @if(@version_compare(@request.headers.x-app-version, @min_version) < 0) {
        @response.status: 426
        error: "App version too old, please update"
    }
    
    # Platform-specific response
    @if(@request.headers.x-platform == "ios") {
        data: @get_ios_specific_data()
    } else {
        data: @get_android_specific_data()
    }
}
```

## Best Practices

1. **Header names are case-insensitive** - TuskLang normalizes them
2. **Always provide fallbacks** - Headers might be missing
3. **Validate authentication headers** - Don't trust client claims
4. **Check content types** - Ensure you can process the request
5. **Use custom headers wisely** - Prefix with X- or your app name

## Related Operators

- `@request` - Parent request object
- `@response.headers` - Set response headers
- `@request.ip` - Client IP address
- `@request.method` - HTTP method
- `@contains()` - Check header values