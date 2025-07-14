# @session - Session Variables

The `@session` operator provides server-side session storage for maintaining state across HTTP requests, perfect for user authentication and shopping carts.

## Basic Syntax

```tusk
# Set session variable
@session.username: "john_doe"

# Read session variable
current_user: @session.username

# Delete session variable
@session.cart: null

# Check if exists
has_user: @isset(@session.user_id)
```

## Session Lifecycle

```tusk
# Start session (automatic in web context)
@session_start()

# Get session ID
session_id: @session_id()

# Regenerate session ID (security)
@session_regenerate_id()

# Destroy session
@session_destroy()
```

## User Authentication

```tusk
# Login endpoint
#api /login {
    username: @request.post.username
    password: @request.post.password
    
    # Verify credentials
    user: @query("SELECT * FROM users WHERE username = ?", [@username])
    
    @if(@verify_password(@password, @user.password_hash)) {
        # Set session variables
        @session.user_id: @user.id
        @session.username: @user.username
        @session.role: @user.role
        @session.login_time: @timestamp
        
        # Regenerate session ID for security
        @session_regenerate_id()
        
        @json({
            success: true
            message: "Login successful"
        })
    } else {
        @json({
            success: false
            error: "Invalid credentials"
        })
    }
}

# Logout endpoint
#api /logout {
    # Clear session
    @session_destroy()
    
    @json({
        success: true
        message: "Logged out successfully"
    })
}
```

## Shopping Cart

```tusk
# Initialize cart if not exists
@if(!@isset(@session.cart)) {
    @session.cart: []
}

# Add to cart
#api /cart/add {
    product_id: @request.post.product_id
    quantity: @request.post.quantity|1
    
    # Get current cart
    cart: @session.cart
    
    # Check if product already in cart
    existing_index: @array_search(@cart, "product_id", @product_id)
    
    @if(@existing_index !== false) {
        # Update quantity
        cart[@existing_index].quantity: @cart[@existing_index].quantity + @quantity
    } else {
        # Add new item
        product: @query("SELECT * FROM products WHERE id = ?", [@product_id])
        cart[]: {
            product_id: @product_id
            name: @product.name
            price: @product.price
            quantity: @quantity
        }
    }
    
    # Save cart
    @session.cart: @cart
    
    @json({
        success: true
        cart_count: @count(@cart)
        cart_total: @array_sum(@cart, "price * quantity")
    })
}
```

## Session Security

```tusk
# Session timeout check
last_activity: @session.last_activity|0
timeout_minutes: 30

@if(@time() - @last_activity > @timeout_minutes * 60) {
    # Session expired
    @session_destroy()
    @redirect("/login?expired=1")
}

# Update last activity
@session.last_activity: @time()

# IP validation
@if(@session.ip && @session.ip != @request.ip) {
    # Possible session hijacking
    @session_destroy()
    @log_security_event("Session IP mismatch", {
        session_ip: @session.ip
        request_ip: @request.ip
    })
    @redirect("/login?security=1")
}
```

## Flash Messages

```tusk
# Set flash message
@session.flash: {
    type: "success"
    message: "Profile updated successfully"
}

# Display flash message
@if(@session.flash) {
    flash: @session.flash
    # Clear after displaying
    @session.flash: null
    
    # In template
    <div class="alert alert-{@flash.type}">
        {@flash.message}
    </div>
}
```

## Multi-step Forms

```tusk
# Wizard step tracking
#api /wizard/step/{step} {
    current_step: @step
    
    # Store form data
    @session.wizard[@current_step]: @request.post
    
    # Validate current step
    is_valid: @validate_step(@current_step, @request.post)
    
    @if(@is_valid) {
        @if(@current_step < 3) {
            # Go to next step
            @redirect("/wizard/step/" + (@current_step + 1))
        } else {
            # Process complete form
            all_data: @session.wizard
            result: @process_wizard(@all_data)
            
            # Clear wizard data
            @session.wizard: null
            
            @redirect("/wizard/complete")
        }
    } else {
        # Show errors
        @session.flash: {
            type: "error"
            message: "Please correct the errors"
        }
        @redirect("/wizard/step/" + @current_step)
    }
}
```

## Preferences Storage

```tusk
# User preferences
@session.preferences: {
    theme: @request.post.theme|"light"
    language: @request.post.language|"en"
    timezone: @request.post.timezone|"UTC"
    notifications: @request.post.notifications|true
}

# Apply preferences
current_theme: @session.preferences.theme|"light"
current_language: @session.preferences.language|"en"

# Theme switcher
#api /preferences/theme {
    new_theme: @request.post.theme
    
    # Update preference
    prefs: @session.preferences|{}
    prefs.theme: @new_theme
    @session.preferences: @prefs
    
    # Also save to database if logged in
    @if(@session.user_id) {
        @query("UPDATE users SET theme = ? WHERE id = ?", 
               [@new_theme, @session.user_id])
    }
    
    @json({
        success: true
        theme: @new_theme
    })
}
```

## Session Arrays

```tusk
# Recent searches
@if(!@isset(@session.recent_searches)) {
    @session.recent_searches: []
}

# Add search term
search_term: @request.get.q
@if(@search_term) {
    # Add to beginning, limit to 10
    @session.recent_searches: @array_unique(
        @array_merge([@search_term], @session.recent_searches)
    )
    @session.recent_searches: @array_slice(@session.recent_searches, 0, 10)
}

# Recently viewed products
@session.viewed_products[]: @product_id
@session.viewed_products: @array_slice(@array_unique(@session.viewed_products), -5)
```

## Session Debugging

```tusk
# Debug session data
#api /debug/session {
    # Only in development
    @if(@env.APP_ENV != "development") {
        @response.status: 403
        error: "Forbidden"
    }
    
    session_data: {
        id: @session_id()
        data: @session
        save_path: @session_save_path()
        cookie_params: @session_get_cookie_params()
    }
    
    @json(@session_data)
}
```

## Best Practices

1. **Always regenerate session ID after login** - Prevents fixation attacks
2. **Set session timeout** - Don't keep sessions forever
3. **Validate session data** - Don't trust that it hasn't been tampered
4. **Use HTTPS** - Session cookies should be secure
5. **Clean up old sessions** - Implement garbage collection

## Related Operators

- `@cookie` - Client-side storage
- `@cache` - Server-side caching
- `@user` - Current user shortcuts
- `@session_start()` - Manual session control
- `@session_destroy()` - End session