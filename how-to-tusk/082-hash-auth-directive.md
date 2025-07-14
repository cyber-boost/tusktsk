# #auth - Authentication Directive

The `#auth` directive provides built-in authentication and authorization controls for routes, making it easy to protect resources and manage user access.

## Basic Syntax

```tusk
# Require authentication
#auth {
    #web /dashboard {
        @render("dashboard.tusk", {user: @auth.user})
    }
}

# Inline auth check
#web /profile {
    #auth required: true
    
    @render("profile.tusk", {user: @auth.user})
}

# With specific permissions
#web /admin {
    #auth role: "admin"
    
    @render("admin/dashboard.tusk")
}
```

## Authentication Guards

```tusk
# Different auth guards
#auth guard: "web" {
    # Session-based authentication (default)
    #web /account {
        @render("account.tusk")
    }
}

#auth guard: "api" {
    # Token-based authentication
    #api /user {
        return @auth.user
    }
}

#auth guard: "admin" {
    # Separate admin authentication
    #web /admin/dashboard {
        @render("admin/dashboard.tusk")
    }
}

# Custom guard
#auth guard: "customer" provider: "customers" {
    #web /customer/portal {
        @render("customer/portal.tusk")
    }
}
```

## Role-Based Access

```tusk
# Single role requirement
#auth role: "editor" {
    #web /posts/create {
        @render("posts/create.tusk")
    }
}

# Multiple roles (any)
#auth roles: ["admin", "moderator"] {
    #web /moderation {
        @render("moderation/dashboard.tusk")
    }
}

# Multiple roles (all required)
#auth roles: ["user", "verified"] require_all: true {
    #web /verified-only {
        @render("verified/content.tusk")
    }
}

# Dynamic role checking
#web /content/{id} {
    #auth can: "view-content" model: @Content.find(@params.id)
    
    @render("content/show.tusk", {
        content: @auth.model
    })
}
```

## Permission-Based Access

```tusk
# Single permission
#auth permission: "manage-users" {
    #web /users {
        users: @User.paginate(20)
        @render("users/index.tusk", {users})
    }
}

# Multiple permissions (any)
#auth permissions: ["create-posts", "edit-posts"] {
    #web /posts/new {
        @render("posts/form.tusk")
    }
}

# Multiple permissions (all required)
#auth permissions: ["view-reports", "export-data"] match: "all" {
    #web /reports/export {
        @export_reports()
    }
}

# Permission with context
#web /posts/{id}/edit {
    post: @Post.find(@params.id)
    
    #auth can: "edit" model: post
    
    @render("posts/edit.tusk", {post})
}
```

## Authentication Methods

```tusk
# Login implementation
#web /login method: POST {
    credentials: {
        email: @request.post.email
        password: @request.post.password
    }
    
    # Attempt login
    if (@auth.attempt(credentials)) {
        # Optional remember me
        if (@request.post.remember) {
            @auth.remember()
        }
        
        # Redirect to intended URL or default
        return @redirect(@auth.intended("/dashboard"))
    }
    
    # Failed login
    @back().with("error", "Invalid credentials")
}

# Logout
#web /logout method: POST {
    #auth required: true
    
    @auth.logout()
    @session.invalidate()
    @redirect("/")
}

# Registration
#web /register method: POST {
    # Validate input
    data: @validate(@request.post, {
        name: "required|string|max:255"
        email: "required|email|unique:users"
        password: "required|min:8|confirmed"
    })
    
    # Create user
    user: @User.create({
        name: data.name
        email: data.email
        password: @hash(data.password)
    })
    
    # Auto login
    @auth.login(user)
    
    # Send verification email
    @event("user.registered", user)
    
    @redirect("/dashboard")
}
```

## OAuth Integration

```tusk
# OAuth routes
#auth provider: "oauth" {
    # Redirect to provider
    #web /auth/{provider} {
        return @auth.driver(@params.provider).redirect()
    }
    
    # Handle callback
    #web /auth/{provider}/callback {
        try {
            user: @auth.driver(@params.provider).user()
            
            # Find or create user
            local_user: @User.firstOrCreate({
                email: user.email
            }, {
                name: user.name
                avatar: user.avatar
                provider: @params.provider
                provider_id: user.id
            })
            
            # Login
            @auth.login(local_user)
            
            @redirect("/dashboard")
            
        } catch (Exception e) {
            @redirect("/login").with("error", "Authentication failed")
        }
    }
}

# Supported providers
#web /login {
    @render("auth/login.tusk", {
        providers: ["google", "github", "facebook"]
    })
}
```

## API Authentication

```tusk
# API token authentication
#auth guard: "api" {
    #api /user {
        # Automatically authenticated via Bearer token
        return @auth.user
    }
    
    #api /user/update method: PUT {
        user: @auth.user
        user.update(@request.post)
        
        return {
            message: "Profile updated"
            user: user
        }
    }
}

# Generate API token
#web /settings/api method: POST {
    #auth required: true
    
    token: @auth.user.createToken("api-access", [
        "read-profile"
        "update-profile"
    ])
    
    @render("settings/token.tusk", {
        token: token.plainTextToken
    })
}

# Stateless token auth
#auth stateless: true {
    #api /data {
        # No session created
        return @sensitive_data()
    }
}
```

## Two-Factor Authentication

```tusk
# 2FA setup
#web /security/2fa {
    #auth required: true
    
    if (@request.method == "POST") {
        # Enable 2FA
        secret: @auth.user.createTwoFactorAuth()
        qr_code: @generate_qr_code(secret.uri)
        
        @render("security/2fa-setup.tusk", {
            secret: secret.secret
            qr_code: qr_code
        })
    } else {
        @render("security/2fa.tusk", {
            enabled: @auth.user.two_factor_enabled
        })
    }
}

# 2FA verification during login
#web /login/2fa method: POST {
    code: @request.post.code
    
    if (@auth.attemptWith2FA(code)) {
        @redirect(@auth.intended("/dashboard"))
    } else {
        @back().with("error", "Invalid authentication code")
    }
}

# Require 2FA for sensitive routes
#auth require_2fa: true {
    #web /settings/security {
        @render("settings/security.tusk")
    }
}
```

## Session Management

```tusk
# Session configuration
#auth session: {
    lifetime: 120  # minutes
    expire_on_close: false
    encrypt: true
    same_site: "lax"
} {
    #web /app/* {
        # All app routes use these session settings
    }
}

# Multiple sessions
#web /admin/* {
    #auth guard: "admin" session: "admin_session"
    
    # Separate session for admin area
}

# Session invalidation
#web /security/sessions {
    #auth required: true
    
    if (@request.method == "POST") {
        # Invalidate other sessions
        @auth.logoutOtherDevices(@request.post.password)
        
        @back().with("success", "Other sessions terminated")
    }
    
    sessions: @auth.user.sessions
    @render("security/sessions.tusk", {sessions})
}
```

## Custom Authentication

```tusk
# Custom auth provider
#auth provider: {
    # Retrieve user by credentials
    retrieve: (credentials) => {
        user: @User.where("email", credentials.email).first()
        
        if (user && @verify_password(credentials.password, user.password)) {
            return user
        }
        
        return null
    }
    
    # Validate credentials
    validate: (user, credentials) => {
        return @verify_password(credentials.password, user.password)
    }
} {
    #web /custom-login method: POST {
        if (@auth.attempt(@request.post)) {
            @redirect("/dashboard")
        }
    }
}

# Custom user provider
#auth user_provider: {
    model: "Customer"
    identifier: "username"
    password_field: "pass_hash"
} {
    #web /customer/* {
        # Uses custom customer authentication
    }
}
```

## Access Control Helpers

```tusk
# In routes
#web /posts/{id} {
    post: @Post.find(@params.id)
    
    # Manual auth check
    if (!@auth.check()) {
        @abort(401)
    }
    
    # Manual permission check
    if (!@auth.user.can("view", post)) {
        @abort(403)
    }
    
    @render("posts/show.tusk", {post})
}

# Policy-based authorization
#auth policy: PostPolicy {
    #web /posts/{id}/edit {
        # Automatically checks PostPolicy@edit
        @render("posts/edit.tusk", {
            post: @auth.model
        })
    }
}

# Gate definitions
gates: {
    "edit-post": (user, post) => {
        return user.id == post.author_id || user.isAdmin()
    }
    
    "delete-post": (user, post) => {
        return user.id == post.author_id && post.created_at > @days_ago(1)
    }
}

# Use gates
#web /posts/{id}/delete method: POST {
    post: @Post.find(@params.id)
    
    #auth can: "delete-post" model: post
    
    post.delete()
    @redirect("/posts")
}
```

## Authentication Events

```tusk
# Listen to auth events
#on auth.login {
    user: @event.user
    
    # Update last login
    user.update({
        last_login_at: @now()
        last_login_ip: @request.ip
    })
    
    # Log activity
    @activity("user.login", user)
}

#on auth.logout {
    # Clean up user data
    @cache.forget("user:" + @event.user.id)
}

#on auth.failed {
    # Log failed attempts
    @log.warning("Failed login attempt", {
        email: @event.credentials.email
        ip: @request.ip
    })
    
    # Check for brute force
    @security.check_failed_attempts(@event.credentials.email)
}

# Custom auth events
#auth events: {
    verified: (user) => {
        @notification.send(user, "EmailVerified")
        @grant_verified_benefits(user)
    }
    
    lockout: (request) => {
        @alert_security_team("Account lockout", request)
    }
}
```

## Best Practices

1. **Use appropriate guards** - Web for sessions, API for tokens
2. **Implement rate limiting** - Prevent brute force attacks
3. **Hash passwords properly** - Use bcrypt or argon2
4. **Validate permissions** - Don't rely on client-side checks
5. **Log auth events** - Track login attempts and changes
6. **Use HTTPS** - Protect credentials in transit
7. **Implement 2FA** - For sensitive applications
8. **Session security** - Regenerate IDs, set appropriate timeouts

## Related Topics

- `hash-middleware-directive` - Authentication middleware
- `session-management` - Session handling
- `password-hashing` - Secure password storage
- `oauth-integration` - Social login
- `api-authentication` - Token-based auth