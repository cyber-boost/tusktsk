# Hash (#) Directives Introduction

Hash directives are special commands in TuskLang that define how your code should be executed, providing powerful meta-programming capabilities and execution contexts.

## What are Hash Directives?

Hash directives are instructions that start with `#` and tell TuskLang how to handle specific code blocks. They define execution contexts, routing rules, scheduling, and more.

```tusk
# Basic directive syntax
#directive_name optional_parameters {
    # Code block executed in this directive's context
}

# Example: Web endpoint
#web /hello {
    response: "Hello, World!"
}
```

## Core Directives Overview

```tusk
# Web endpoints
#web /api/users {
    # Handle HTTP requests
}

# API endpoints (JSON responses)
#api /api/data {
    # Automatically returns JSON
}

# Command-line interface
#cli process --file {
    # Handle CLI commands
}

# Scheduled tasks
#cron "0 * * * *" {
    # Run every hour
}

# Middleware
#middleware auth {
    # Process requests before handlers
}

# Database migrations
#migration create_users_table {
    # Database schema changes
}

# Tests
#test "user creation" {
    # Unit tests
}
```

## Directive Structure

```tusk
# Full directive syntax
#directive_type route_or_pattern optional_name {
    # Directive body
    
    # Access to special variables
    # @request - incoming request
    # @response - outgoing response
    # @params - route parameters
    # @context - execution context
}

# Nested directives
#web /admin {
    #auth required: true
    #role "admin"
    
    # Admin-only content
}
```

## Common Patterns

```tusk
# RESTful routes
#web /users {
    # GET /users - List users
    users: @User.all()
    @json(users)
}

#web /users/{id} {
    # GET /users/123 - Get specific user
    user: @User.find(@params.id)
    @json(user)
}

#api /users method: POST {
    # POST /users - Create user
    user: @User.create(@request.post)
    @json(user, 201)
}

# Grouped routes
#group /api/v1 {
    #web /users {
        # Routes to /api/v1/users
    }
    
    #web /posts {
        # Routes to /api/v1/posts
    }
}
```

## Directive Modifiers

```tusk
# Method specification
#web /users method: GET {
    # Only handles GET requests
}

#web /users method: [GET, POST] {
    # Handles both GET and POST
}

# Middleware application
#web /secure middleware: [auth, logging] {
    # Applies auth and logging middleware
}

# Conditional directives
#web /feature if: @feature_enabled("new_ui") {
    # Only active if feature is enabled
}

# Named directives
#web /home name: "home_route" {
    # Can be referenced by name
}
```

## Custom Directives

```tusk
# Define custom directive
#define_directive webhook {
    pattern: /^\/webhooks\/(.+)$/
    
    handler: (match, block) => {
        webhook_type: match[1]
        
        return {
            method: "POST"
            middleware: ["verify_webhook"]
            handler: () => {
                payload: @request.post
                signature: @request.headers.x-webhook-signature
                
                if (!@verify_signature(payload, signature)) {
                    @response.status: 401
                    return {error: "Invalid signature"}
                }
                
                return block({
                    type: webhook_type,
                    payload: payload
                })
            }
        }
    }
}

# Use custom directive
#webhook /github {
    # Handle GitHub webhooks
    event: @request.headers.x-github-event
    
    switch (event) {
        case "push":
            @handle_push(@payload)
        case "pull_request":
            @handle_pr(@payload)
    }
}
```

## Directive Context

```tusk
# Each directive provides context
#web /user/profile {
    # Available in web context:
    # @request - HTTP request object
    # @response - HTTP response object
    # @params - Route parameters
    # @session - Session data
    # @cookie - Cookie access
}

#cli backup --database {
    # Available in CLI context:
    # @args - Command line arguments
    # @options - Parsed options
    # @input - STDIN access
    # @output - STDOUT access
}

#cron "*/5 * * * *" {
    # Available in cron context:
    # @schedule - Cron schedule info
    # @last_run - Previous execution time
    # @next_run - Next scheduled time
}
```

## Directive Composition

```tusk
# Combine multiple directives
#authenticated {
    #web /dashboard {
        # Requires authentication
    }
    
    #api /user/data {
        # Also requires authentication
    }
}

# Directive inheritance
#base_api {
    #ratelimit 100/hour
    #cache 300
    
    #api /data {
        # Inherits rate limit and cache
    }
}

# Conditional composition
#if @env.FEATURE_X {
    #web /new-feature {
        # Only defined if feature is enabled
    }
}
```

## Directive Configuration

```tusk
# Global directive settings
#config {
    web: {
        prefix: "/app"
        middleware: ["logging", "cors"]
        error_handler: @handle_http_error
    }
    
    api: {
        version: "v1"
        format: "json"
        authentication: "bearer"
    }
    
    cli: {
        colors: true
        interactive: true
    }
}

# Override per directive
#web /special config: {middleware: []} {
    # No global middleware applied
}
```

## Error Handling in Directives

```tusk
# Directive-specific error handling
#web /risky {
    #on_error {
        @log.error("Web handler failed", @error)
        @response.status: 500
        @json({error: "Internal server error"})
    }
    
    # Risky operation
    result: @risky_operation()
}

# Global error handlers
#error_handler http {
    switch (@error.code) {
        case 404:
            @render("errors/404.tusk")
        case 500:
            @render("errors/500.tusk")
        default:
            @render("errors/generic.tusk", {error: @error})
    }
}
```

## Directive Metadata

```tusk
# Add metadata to directives
#web /api/users {
    #doc {
        description: "List all users"
        parameters: {
            page: "Page number (optional)"
            limit: "Items per page (optional)"
        }
        returns: "Array of user objects"
    }
    
    #deprecated "Use /api/v2/users instead"
    
    # Handler code
}

# Access directive metadata
routes: @get_directives("web")
for (route in routes) {
    if (route.metadata.deprecated) {
        @log.warning("Deprecated route in use", route)
    }
}
```

## Best Practices

1. **Use appropriate directives** - Choose the right directive for the context
2. **Keep handlers focused** - One responsibility per directive
3. **Use middleware** - Share common functionality
4. **Document directives** - Add descriptions and examples
5. **Handle errors** - Always consider failure cases
6. **Test directives** - Unit test your handlers
7. **Organize by feature** - Group related directives
8. **Version APIs** - Use versioning for backwards compatibility

## Common Patterns

```tusk
# RESTful resource
#resource users {
    # Generates all CRUD routes:
    # GET    /users       - index
    # GET    /users/{id}  - show
    # POST   /users       - create
    # PUT    /users/{id}  - update
    # DELETE /users/{id}  - destroy
}

# Nested resources
#resource posts {
    #resource comments {
        # Generates /posts/{post_id}/comments/{id} routes
    }
}

# API versioning
#version 1 {
    #api /users {
        # Version 1 implementation
    }
}

#version 2 {
    #api /users {
        # Version 2 implementation
    }
}
```

## Related Topics

- `hash-web-directive` - Web endpoints
- `hash-api-directive` - API endpoints
- `hash-cli-directive` - CLI commands
- `hash-cron-directive` - Scheduled tasks
- `hash-middleware-directive` - Request processing