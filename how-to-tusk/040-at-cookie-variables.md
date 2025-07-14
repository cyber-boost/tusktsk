# @cookie - Cookie Variables

The `@cookie` operator provides easy access to HTTP cookies for client-side data persistence, remember-me functionality, and user preferences.

## Basic Syntax

```tusk
# Read cookie
username: @cookie.username

# Set cookie (simple)
@cookie.theme: "dark"

# Set cookie with options
@cookie.remember_token: {
    value: @generate_token()
    expires: @time() + 2592000  # 30 days
    path: "/"
    domain: ".example.com"
    secure: true
    httponly: true
    samesite: "Lax"
}

# Delete cookie
@cookie.old_data: null
```

## Cookie Options

```tusk
# Comprehensive cookie setting
@cookie.user_preferences: {
    value: @json_encode({
        theme: "dark"
        language: "en"
        timezone: "UTC"
    })
    expires: @time() + 31536000     # 1 year
    path: "/"                       # Available site-wide
    domain: @request.host           # Current domain
    secure: @is_https()             # HTTPS only
    httponly: true                  # No JavaScript access
    samesite: "Strict"              # CSRF protection
}

# Session cookie (expires on browser close)
@cookie.session_data: {
    value: @session_id
    path: "/"
    httponly: true
}
```

## Remember Me Implementation

```tusk
# Login with remember me
#api /login {
    username: @request.post.username
    password: @request.post.password
    remember: @request.post.remember|false
    
    user: @authenticate(@username, @password)
    
    @if(@user) {
        # Set session
        @session.user_id: @user.id
        
        @if(@remember) {
            # Generate secure token
            token: @generate_secure_token()
            token_hash: @hash("sha256", @token)
            
            # Store in database
            @query("INSERT INTO remember_tokens (user_id, token_hash, expires) 
                    VALUES (?, ?, DATE_ADD(NOW(), INTERVAL 30 DAY))", 
                   [@user.id, @token_hash])
            
            # Set cookie
            @cookie.remember_me: {
                value: @user.id + ":" + @token
                expires: @time() + 2592000  # 30 days
                httponly: true
                secure: true
                samesite: "Lax"
            }
        }
        
        @json({success: true})
    }
}

# Auto-login check
auto_login: {
    # Check remember cookie
    remember: @cookie.remember_me
    
    @if(@remember && !@session.user_id) {
        parts: @explode(":", @remember)
        user_id: @parts[0]
        token: @parts[1]
        token_hash: @hash("sha256", @token)
        
        # Verify token
        valid: @query("SELECT * FROM remember_tokens 
                      WHERE user_id = ? AND token_hash = ? 
                      AND expires > NOW()", 
                     [@user_id, @token_hash])
        
        @if(@valid) {
            # Auto login
            @session.user_id: @user_id
            @session.auto_login: true
            
            # Refresh token
            @query("UPDATE remember_tokens 
                   SET expires = DATE_ADD(NOW(), INTERVAL 30 DAY) 
                   WHERE token_hash = ?", [@token_hash])
        }
    }
}
```

## User Preferences

```tusk
# Theme switcher
#api /preferences/theme/{theme} {
    # Validate theme
    valid_themes: ["light", "dark", "auto"]
    
    @if(@in_array(@theme, @valid_themes)) {
        # Set cookie
        @cookie.theme: {
            value: @theme
            expires: @time() + 31536000  # 1 year
            path: "/"
        }
        
        # Update session too
        @session.theme: @theme
        
        @json({
            success: true
            theme: @theme
        })
    } else {
        @response.status: 400
        @json({error: "Invalid theme"})
    }
}

# Read preference with fallback
current_theme: @cookie.theme|@session.theme|"light"
```

## Shopping Cart Backup

```tusk
# Save cart to cookie (for non-logged users)
save_cart_to_cookie: {
    cart: @session.cart|[]
    
    @if(@count(@cart) > 0 && !@session.user_id) {
        # Compress cart data
        cart_data: @json_encode(@cart)
        compressed: @gzcompress(@cart_data)
        encoded: @base64_encode(@compressed)
        
        @cookie.cart_backup: {
            value: @encoded
            expires: @time() + 604800  # 7 days
            path: "/"
        }
    }
}

# Restore cart from cookie
restore_cart: {
    @if(@cookie.cart_backup && !@session.cart) {
        decoded: @base64_decode(@cookie.cart_backup)
        decompressed: @gzuncompress(@decoded)
        cart_data: @json_decode(@decompressed)
        
        @session.cart: @cart_data
        
        # Clear backup
        @cookie.cart_backup: null
    }
}
```

## Analytics and Tracking

```tusk
# First visit tracking
@if(!@cookie.first_visit) {
    @cookie.first_visit: {
        value: @timestamp
        expires: @time() + 63072000  # 2 years
        path: "/"
    }
    
    # Track new visitor
    @track_event("new_visitor", {
        referrer: @request.headers.referer
        landing_page: @request.uri
    })
}

# Visit counter
visits: @int(@cookie.visit_count|0) + 1
@cookie.visit_count: {
    value: @visits
    expires: @time() + 31536000
    path: "/"
}

# A/B testing
@if(!@cookie.ab_variant) {
    variant: @rand(0, 1) ? "A" : "B"
    @cookie.ab_variant: {
        value: @variant
        expires: @time() + 2592000  # 30 days
        path: "/"
    }
}
```

## Security Cookies

```tusk
# CSRF token
@if(!@cookie.csrf_token) {
    @cookie.csrf_token: {
        value: @generate_csrf_token()
        expires: @time() + 7200  # 2 hours
        path: "/"
        secure: true
        samesite: "Strict"
    }
}

# Verify CSRF
verify_csrf: {
    cookie_token: @cookie.csrf_token
    header_token: @request.headers.x-csrf-token
    
    is_valid: @cookie_token && @cookie_token == @header_token
    
    @if(!@is_valid && @request.method != "GET") {
        @response.status: 403
        error: "Invalid CSRF token"
    }
}
```

## Cookie Consent

```tusk
# Check consent
has_consent: @cookie.cookie_consent == "accepted"

# Set analytics cookies only with consent
@if(@has_consent) {
    @cookie.analytics_id: {
        value: @generate_analytics_id()
        expires: @time() + 63072000  # 2 years
        path: "/"
    }
}

# Consent banner endpoint
#api /cookies/consent {
    action: @request.post.action
    
    @if(@action == "accept") {
        @cookie.cookie_consent: {
            value: "accepted"
            expires: @time() + 31536000
            path: "/"
        }
    } elseif(@action == "reject") {
        # Clear non-essential cookies
        @cookie.analytics_id: null
        @cookie.marketing_id: null
        
        @cookie.cookie_consent: {
            value: "rejected"
            expires: @time() + 31536000
            path: "/"
        }
    }
    
    @json({success: true})
}
```

## Language Selection

```tusk
# Detect language preference
user_language: @cookie.language|@detect_browser_language()|"en"

# Language switcher
#api /language/{lang} {
    # Validate language code
    supported: ["en", "es", "fr", "de", "ja"]
    
    @if(@in_array(@lang, @supported)) {
        @cookie.language: {
            value: @lang
            expires: @time() + 157680000  # 5 years
            path: "/"
            domain: ".example.com"  # All subdomains
        }
        
        # Redirect to localized version
        @redirect("/" + @lang + @request.uri)
    }
}
```

## Cookie Debugging

```tusk
# List all cookies
#api /debug/cookies {
    @if(@env.DEBUG != "true") {
        @response.status: 403
        error: "Forbidden"
    }
    
    all_cookies: @cookie
    
    # Parse cookie options
    cookie_info: {}
    @foreach(@all_cookies as @name => @value) {
        cookie_info[@name]: {
            value: @value
            size: @strlen(@value)
            type: @get_type(@value)
        }
    }
    
    @json({
        cookies: @cookie_info
        total: @count(@all_cookies)
        header: @request.headers.cookie
    })
}
```

## Best Practices

1. **Use secure flag on HTTPS** - Prevent cookie theft
2. **Set httponly for sensitive data** - Prevent XSS attacks
3. **Use samesite attribute** - CSRF protection
4. **Set appropriate expiration** - Don't keep cookies forever
5. **Minimize cookie size** - Sent with every request
6. **Encrypt sensitive data** - Don't store plaintext passwords

## Related Operators

- `@session` - Server-side storage
- `@request.headers.cookie` - Raw cookie header
- `@response.headers.set-cookie` - Manual cookie setting
- `@time()` - Current timestamp for expiration
- `@is_https()` - Check secure connection