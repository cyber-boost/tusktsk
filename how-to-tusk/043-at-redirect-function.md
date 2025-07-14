# @redirect() - HTTP Redirects

The `@redirect()` function sends HTTP redirect responses to navigate users to different URLs, with support for various redirect types and data passing.

## Basic Syntax

```tusk
# Simple redirect
@redirect("/dashboard")

# External redirect
@redirect("https://example.com")

# Redirect with status code
@redirect("/new-location", 301)  # Permanent redirect

# Redirect with flash data
@redirect("/login", {
    flash: "Please login first"
})
```

## Redirect Types

```tusk
# Temporary redirect (302 - default)
@redirect("/temporary-page")

# Permanent redirect (301)
@redirect("/new-url", 301)

# See Other (303 - after POST)
@redirect("/success", 303)

# Temporary Redirect (307 - preserve method)
@redirect("/process", 307)

# Permanent Redirect (308 - preserve method)
@redirect("/new-endpoint", 308)
```

## After Form Submission

```tusk
#api /contact {
    # Process form
    name: @request.post.name
    email: @request.post.email
    message: @request.post.message
    
    # Validate
    @if(!@name || !@email || !@message) {
        @redirect("/contact", {
            flash: {
                type: "error"
                message: "All fields are required"
            },
            old: @request.post  # Preserve form data
        })
    }
    
    # Save to database
    @query("INSERT INTO messages (name, email, message) VALUES (?, ?, ?)",
           [@name, @email, @message])
    
    # Redirect with success message
    @redirect("/contact", {
        flash: {
            type: "success"
            message: "Thank you! Your message has been sent."
        }
    }, 303)  # Use 303 after POST
}
```

## Authentication Redirects

```tusk
# Middleware to check authentication
check_auth: {
    @if(!@session.user_id) {
        # Store intended destination
        @session.redirect_after_login: @request.uri
        
        @redirect("/login", {
            flash: "Please login to continue"
        })
    }
}

# After successful login
#api /login {
    # ... authentication logic ...
    
    @if(@authenticated) {
        # Check for intended destination
        redirect_to: @session.redirect_after_login|"/dashboard"
        @session.redirect_after_login: null
        
        @redirect(@redirect_to)
    }
}
```

## Conditional Redirects

```tusk
#web /profile {
    # Redirect based on user type
    @if(!@session.user_id) {
        @redirect("/login")
    } elseif(@session.user.role == "admin") {
        @redirect("/admin/profile")
    } elseif(@session.user.role == "vendor") {
        @redirect("/vendor/profile")
    } else {
        @redirect("/user/profile")
    }
}

# Feature flags
#web /new-feature {
    @if(!@feature_enabled("new_ui")) {
        @redirect("/old-feature", {
            flash: "This feature is coming soon!"
        })
    }
    
    # Show new feature...
}
```

## Redirect with Query Parameters

```tusk
# Build redirect URL with parameters
#api /search {
    query: @request.post.q
    category: @request.post.category
    sort: @request.post.sort|"relevance"
    
    # Build query string
    params: @http_build_query({
        q: @query
        category: @category
        sort: @sort
        page: 1
    })
    
    @redirect("/search/results?" + @params)
}

# Preserve existing parameters
#web /filter {
    # Get current parameters
    current_params: @request.query
    
    # Add/update filter
    current_params.filter: @request.post.filter
    
    # Redirect with updated parameters
    @redirect(@request.path + "?" + @http_build_query(@current_params))
}
```

## Back/Referrer Redirects

```tusk
# Redirect back with fallback
#api /action {
    # Perform action...
    
    # Redirect to referrer or fallback
    referrer: @request.headers.referer
    
    @if(@referrer && @is_safe_url(@referrer)) {
        @redirect(@referrer)
    } else {
        @redirect("/dashboard")
    }
}

# Helper function for safe URLs
is_safe_url: (url) => {
    # Check if URL is from same domain
    parsed: @parse_url(@url)
    current_host: @request.headers.host
    
    return !@parsed.host || @parsed.host == @current_host
}
```

## Named Routes

```tusk
# Define routes with names
routes: {
    home: "/"
    dashboard: "/dashboard"
    profile: "/user/profile"
    post: "/blog/post/{id}"
}

# Redirect to named route
@redirect(@route("dashboard"))

# With parameters
@redirect(@route("post", {id: 123}))

# Route helper
route: (name, params: {}) => {
    path: @routes[@name]
    
    # Replace parameters
    @foreach(@params as @key => @value) {
        path: @str_replace("{" + @key + "}", @value, @path)
    }
    
    return @path
}
```

## Flash Messages

```tusk
# Set flash message and redirect
#api /update-profile {
    # Update user profile
    success: @update_user_profile(@request.post)
    
    @if(@success) {
        @session.flash: {
            type: "success"
            message: "Profile updated successfully!"
        }
        @redirect("/profile")
    } else {
        @session.flash: {
            type: "error"
            message: "Failed to update profile"
        }
        @redirect("/profile/edit")
    }
}

# Display flash messages (in template)
@if(@session.flash) {
    <div class="alert alert-{@session.flash.type}">
        {@session.flash.message}
    </div>
    @session.flash: null  # Clear after display
}
```

## Mobile App Redirects

```tusk
# Detect mobile and redirect
#web / {
    is_mobile: @is_mobile_device()
    has_app: @cookie.has_mobile_app
    
    @if(@is_mobile && !@has_app) {
        # Redirect to app store
        @if(@is_ios()) {
            @redirect("https://apps.apple.com/app/id123456")
        } elseif(@is_android()) {
            @redirect("https://play.google.com/store/apps/details?id=com.example")
        } else {
            @redirect("/mobile")
        }
    }
}

# Deep linking
#web /product/{id} {
    @if(@is_mobile_app()) {
        # Redirect to app deep link
        @redirect("myapp://product/" + @id)
    }
    
    # Regular web view...
}
```

## Maintenance Mode

```tusk
# Global maintenance redirect
maintenance_check: {
    is_maintenance: @env.MAINTENANCE_MODE == "true"
    allowed_ips: ["192.168.1.1", "10.0.0.1"]
    
    @if(@is_maintenance && !@in_array(@request.ip, @allowed_ips)) {
        @redirect("/maintenance", 503)
    }
}

# Maintenance page
#web /maintenance {
    @response.status: 503
    @response.headers.retry-after: 3600  # 1 hour
    
    @render("templates/maintenance.tusk", {
        message: "We'll be back soon!"
    })
}
```

## Redirect Loops Prevention

```tusk
# Prevent redirect loops
#web /setup {
    # Check if already redirected
    redirect_count: @session.redirect_count|0
    
    @if(@redirect_count > 3) {
        # Break the loop
        @session.redirect_count: 0
        @response.status: 500
        error: "Redirect loop detected"
        return
    }
    
    # Check setup status
    @if(!@is_setup_complete()) {
        @session.redirect_count: @redirect_count + 1
        @redirect("/setup/step1")
    }
    
    @session.redirect_count: 0
}
```

## AJAX Redirects

```tusk
# Handle AJAX redirect responses
#api /ajax-action {
    # Check if AJAX request
    is_ajax: @request.headers.x-requested-with == "XMLHttpRequest"
    
    # Perform action...
    
    @if(@is_ajax) {
        # Return JSON with redirect URL
        @json({
            redirect: "/success",
            message: "Action completed"
        })
    } else {
        # Regular redirect
        @redirect("/success")
    }
}

# Client-side handling:
# if (response.redirect) {
#     window.location.href = response.redirect;
# }
```

## Best Practices

1. **Use appropriate status codes** - 301 for permanent, 302 for temporary
2. **Prevent redirect loops** - Add loop detection
3. **Validate redirect URLs** - Prevent open redirects
4. **Use 303 after POST** - Prevent form resubmission
5. **Include flash messages** - Provide user feedback
6. **Handle AJAX differently** - Return JSON for AJAX requests

## Related Functions

- `@response.status` - Set response status
- `@response.headers` - Set custom headers
- `@route()` - Generate URLs
- `@session.flash` - Flash messages
- `@request.headers.referer` - Get referrer URL