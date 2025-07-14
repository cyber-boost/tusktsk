# @ Request Method

The @request.method property provides access to the HTTP method of the current request. This guide covers how to handle different HTTP methods and implement RESTful patterns in TuskLang.

## HTTP Methods Overview

### Accessing the Method

```tusk
# Get the HTTP method
method = @request.method  # "GET", "POST", "PUT", "DELETE", etc.

# Common usage patterns
is_get = @request.method == "GET"
is_post = @request.method == "POST"
is_write_method = @includes(["POST", "PUT", "PATCH"], @request.method)
```

### Standard HTTP Methods

```tusk
# Standard methods and their typical usage
methods:
    GET: "Retrieve resource(s)"
    POST: "Create new resource"
    PUT: "Update entire resource"
    PATCH: "Partial update"
    DELETE: "Remove resource"
    HEAD: "Get headers only"
    OPTIONS: "Get allowed methods"
    CONNECT: "Establish tunnel"
    TRACE: "Loop-back test"
```

## Method-Based Routing

### Simple Method Handling

```tusk
# Basic method routing
handle_request = @lambda({
    @if(@request.method == "GET", {
        return: @handle_get()
    })
    
    @if(@request.method == "POST", {
        return: @handle_post()
    })
    
    # Method not allowed
    return: {
        status: 405
        body: { error: "Method not allowed" }
        headers: { "Allow": "GET, POST" }
    }
})
```

### Switch-Based Routing

```tusk
# Clean method routing with switch
route_handler = @lambda({
    response = @switch(@request.method, {
        "GET": @get_handler(),
        "POST": @post_handler(),
        "PUT": @put_handler(),
        "PATCH": @patch_handler(),
        "DELETE": @delete_handler(),
        "OPTIONS": @options_handler(),
        default: {
            status: 405
            body: { error: "Method ${@request.method} not allowed" }
        }
    })
    
    return: response
})
```

### Method Map Pattern

```tusk
# Define handlers in a map
method_handlers: {
    GET: @lambda({ 
        # List or retrieve resources
        return: @get_resources() 
    }),
    POST: @lambda({ 
        # Create new resource
        return: @create_resource(@request.body) 
    }),
    PUT: @lambda({ 
        # Replace entire resource
        return: @replace_resource(@request.params.id, @request.body) 
    }),
    PATCH: @lambda({ 
        # Update specific fields
        return: @update_resource(@request.params.id, @request.body) 
    }),
    DELETE: @lambda({ 
        # Remove resource
        return: @delete_resource(@request.params.id) 
    })
}

# Route to appropriate handler
handle = @lambda({
    handler = @method_handlers[@request.method]
    
    @if(!handler, {
        return: {
            status: 405
            body: { error: "Method not allowed" }
            headers: { "Allow": @join(@keys(@method_handlers), ", ") }
        }
    })
    
    return: @handler()
})
```

## RESTful Resource Handling

### Complete REST Controller

```tusk
# RESTful user controller
user_controller:
    # GET /users - List all users
    # GET /users/:id - Get specific user
    GET: @lambda({
        @if(@request.params.id, {
            # Get single user
            user = @db.find("users", @request.params.id)
            
            @if(!user, {
                return: { 
                    status: 404, 
                    body: { error: "User not found" } 
                }
            })
            
            return: { body: { user: user } }
        }, {
            # List users with pagination
            page = @int(@request.query.page ?? 1)
            limit = @int(@request.query.limit ?? 20)
            offset = (page - 1) * limit
            
            users = @db.query(
                "SELECT * FROM users LIMIT ? OFFSET ?",
                limit, offset
            )
            
            total = @db.count("users")
            
            return: {
                body: {
                    users: users
                    pagination: {
                        page: page
                        limit: limit
                        total: total
                        pages: @ceil(total / limit)
                    }
                }
            }
        })
    })
    
    # POST /users - Create new user
    POST: @lambda({
        # Validate required fields
        required = ["name", "email", "password"]
        missing = @filter(required, @lambda(field, !@request.body[field]))
        
        @if(@len(missing) > 0, {
            return: {
                status: 400
                body: { 
                    error: "Missing required fields",
                    fields: missing
                }
            }
        })
        
        # Create user
        user_data = {
            name: @request.body.name
            email: @request.body.email
            password_hash: @hash(@request.body.password)
            created_at: @time.now()
        }
        
        user = @db.insert("users", user_data)
        
        return: {
            status: 201
            body: { user: user }
            headers: { "Location": "/users/${user.id}" }
        }
    })
    
    # PUT /users/:id - Replace user
    PUT: @lambda({
        id = @request.params.id
        
        # Check if exists
        existing = @db.find("users", id)
        @if(!existing, {
            return: { status: 404, body: { error: "User not found" } }
        })
        
        # Replace entire record
        user_data = {
            id: id
            name: @request.body.name
            email: @request.body.email
            password_hash: @hash(@request.body.password)
            created_at: existing.created_at
            updated_at: @time.now()
        }
        
        @db.update("users", id, user_data)
        
        return: { body: { user: user_data } }
    })
    
    # PATCH /users/:id - Partial update
    PATCH: @lambda({
        id = @request.params.id
        
        # Check if exists
        user = @db.find("users", id)
        @if(!user, {
            return: { status: 404, body: { error: "User not found" } }
        })
        
        # Update only provided fields
        updates = {}
        
        @if(@request.body.name != null, {
            updates.name = @request.body.name
        })
        
        @if(@request.body.email != null, {
            updates.email = @request.body.email
        })
        
        @if(@request.body.password != null, {
            updates.password_hash = @hash(@request.body.password)
        })
        
        updates.updated_at = @time.now()
        
        @db.update("users", id, updates)
        updated_user = @merge(user, updates)
        
        return: { body: { user: updated_user } }
    })
    
    # DELETE /users/:id - Delete user
    DELETE: @lambda({
        id = @request.params.id
        
        # Check if exists
        user = @db.find("users", id)
        @if(!user, {
            return: { status: 404, body: { error: "User not found" } }
        })
        
        # Delete user
        @db.delete("users", id)
        
        return: { 
            status: 204  # No content
        }
    })
```

## Method-Specific Behaviors

### Safe vs Unsafe Methods

```tusk
# Categorize methods
safe_methods: ["GET", "HEAD", "OPTIONS", "TRACE"]
unsafe_methods: ["POST", "PUT", "PATCH", "DELETE"]

# Check if method is safe (no side effects)
is_safe_method = @includes(@safe_methods, @request.method)

# Apply different logic
handle_with_safety = @lambda({
    @if(@is_safe_method, {
        # Can be cached, retried
        return: @cache_wrapper(@handle_request)
    }, {
        # Should not be cached, needs CSRF protection
        return: @csrf_protection(@handle_request)
    })
})
```

### Idempotent Methods

```tusk
# Idempotent methods (same result for multiple calls)
idempotent_methods: ["GET", "PUT", "DELETE", "HEAD", "OPTIONS"]

# Check idempotency
is_idempotent = @includes(@idempotent_methods, @request.method)

# Retry logic for idempotent methods only
retry_handler = @lambda(handler, {
    @if(@is_idempotent, {
        return: @retry_with_backoff(handler, 3)
    }, {
        # Don't retry non-idempotent methods
        return: @handler()
    })
})
```

## Method Validation

### Allowed Methods Per Route

```tusk
# Define allowed methods for routes
route_methods: {
    "/users": ["GET", "POST"],
    "/users/:id": ["GET", "PUT", "PATCH", "DELETE"],
    "/auth/login": ["POST"],
    "/auth/logout": ["POST"],
    "/public/*": ["GET", "HEAD"]
}

# Validate method for route
validate_method = @lambda(route, method, {
    allowed = @route_methods[route] ?? ["GET"]
    
    @if(!@includes(allowed, method), {
        return: {
            valid: false
            error: "Method ${method} not allowed for ${route}"
            allowed: allowed
        }
    })
    
    return: { valid: true }
})
```

### Method-Specific Validation

```tusk
# Different validation rules per method
method_validators: {
    POST: @lambda({
        # POST must have body
        @if(!@request.body || @isEmpty(@request.body), {
            return: { error: "Request body required for POST" }
        })
        
        # Check content-type
        @if(!@request.headers["content-type"], {
            return: { error: "Content-Type header required" }
        })
        
        return: { valid: true }
    }),
    
    GET: @lambda({
        # GET should not have body
        @if(@request.body, {
            return: { error: "GET requests should not have a body" }
        })
        
        return: { valid: true }
    }),
    
    DELETE: @lambda({
        # DELETE requires resource ID
        @if(!@request.params.id, {
            return: { error: "Resource ID required for DELETE" }
        })
        
        return: { valid: true }
    })
}

# Validate current request method
validate_request = @lambda({
    validator = @method_validators[@request.method]
    
    @if(validator, {
        return: @validator()
    })
    
    return: { valid: true }
})
```

## CORS and OPTIONS

### OPTIONS Method Handler

```tusk
# Handle preflight requests
options_handler = @lambda({
    # Get allowed methods for this route
    allowed_methods = @get_allowed_methods(@request.path)
    
    return: {
        status: 204  # No content
        headers: {
            "Allow": @join(allowed_methods, ", ")
            "Access-Control-Allow-Methods": @join(allowed_methods, ", ")
            "Access-Control-Allow-Headers": "Content-Type, Authorization"
            "Access-Control-Max-Age": "86400"  # 24 hours
        }
    }
})

# Auto-handle OPTIONS
cors_middleware = @lambda(handler, {
    # Handle OPTIONS automatically
    @if(@request.method == "OPTIONS", {
        return: @options_handler()
    })
    
    # Add CORS headers to response
    response = @handler()
    response.headers = @merge(response.headers ?? {}, {
        "Access-Control-Allow-Origin": "*"
    })
    
    return: response
})
```

## Method Override

### Supporting Method Override

```tusk
# Allow method override for clients that only support GET/POST
get_effective_method = @lambda({
    # Check override header
    override_header = @request.headers["x-http-method-override"]
    @if(override_header, {
        return: @upper(override_header)
    })
    
    # Check override parameter
    override_param = @request.query._method
    @if(override_param, {
        return: @upper(override_param)
    })
    
    # Use actual method
    return: @request.method
})

# Use overridden method
effective_method = @get_effective_method()
```

## Security Considerations

### CSRF Protection

```tusk
# CSRF protection for state-changing methods
csrf_protection = @lambda(handler, {
    # Skip for safe methods
    @if(@includes(["GET", "HEAD", "OPTIONS"], @request.method), {
        return: @handler()
    })
    
    # Check CSRF token
    token = @request.headers["x-csrf-token"] ?? @request.body.csrf_token
    
    @if(!@verify_csrf_token(token), {
        return: {
            status: 403
            body: { error: "Invalid CSRF token" }
        }
    })
    
    return: @handler()
})
```

### Method-Based Rate Limiting

```tusk
# Different rate limits per method
method_rate_limits: {
    GET: { requests: 1000, window: 3600 },     # 1000/hour
    POST: { requests: 100, window: 3600 },      # 100/hour
    PUT: { requests: 100, window: 3600 },       # 100/hour
    DELETE: { requests: 50, window: 3600 }      # 50/hour
}

# Apply method-specific rate limit
apply_rate_limit = @lambda({
    limit = @method_rate_limits[@request.method] ?? { 
        requests: 100, 
        window: 3600 
    }
    
    key = "${@request.ip}_${@request.method}"
    current = @cache.increment(key)
    
    @if(current > limit.requests, {
        return: {
            status: 429
            body: { error: "Rate limit exceeded" }
            headers: {
                "X-RateLimit-Limit": limit.requests
                "X-RateLimit-Window": limit.window
                "Retry-After": limit.window
            }
        }
    })
    
    @cache.expire(key, limit.window)
})
```

## Best Practices

1. **Use appropriate methods** - GET for retrieval, POST for creation, etc.
2. **Validate method early** - Check if method is allowed before processing
3. **Return 405 properly** - Include Allow header with permitted methods
4. **Handle OPTIONS** - Support CORS preflight requests
5. **Be idempotent** - PUT and DELETE should be idempotent
6. **Protect unsafe methods** - Add CSRF protection to POST, PUT, DELETE
7. **Document allowed methods** - Make it clear which methods each endpoint supports

## Common Patterns

### Method Middleware Stack

```tusk
# Build middleware stack based on method
create_middleware_stack = @lambda({
    stack: [@logging_middleware]
    
    # Add method-specific middleware
    @if(@includes(["POST", "PUT", "PATCH", "DELETE"], @request.method), {
        @push(stack, @auth_middleware)
        @push(stack, @csrf_middleware)
        @push(stack, @validation_middleware)
    })
    
    @if(@request.method == "GET", {
        @push(stack, @cache_middleware)
    })
    
    return: stack
})
```

### RESTful Collection Pattern

```tusk
# Generic collection handler
create_collection_handler = @lambda(resource_name, {
    handlers: {
        # GET /resources
        "GET:collection": @lambda({
            items = @db.all(resource_name)
            return: { [resource_name]: items }
        }),
        
        # POST /resources  
        "POST:collection": @lambda({
            item = @db.create(resource_name, @request.body)
            return: { 
                status: 201,
                body: { [resource_name]: item }
            }
        }),
        
        # GET /resources/:id
        "GET:item": @lambda({
            item = @db.find(resource_name, @request.params.id)
            return: item ? { [resource_name]: item } : { status: 404 }
        }),
        
        # PUT /resources/:id
        "PUT:item": @lambda({
            @db.replace(resource_name, @request.params.id, @request.body)
            return: { [resource_name]: @request.body }
        }),
        
        # DELETE /resources/:id
        "DELETE:item": @lambda({
            @db.delete(resource_name, @request.params.id)
            return: { status: 204 }
        })
    }
    
    return: @lambda({
        type = @request.params.id ? "item" : "collection"
        key = "${@request.method}:${type}"
        handler = handlers[key]
        
        return: handler ? @handler() : { status: 405 }
    })
})
```

## Next Steps

- Explore [Query Parameters](036-at-request-query.md)
- Learn about [Request Body](037-at-request-body.md)
- Master [Request Headers](038-at-request-headers.md)