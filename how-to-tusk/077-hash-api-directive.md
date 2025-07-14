# #api - API Endpoint Directive

The `#api` directive creates RESTful API endpoints that automatically handle JSON serialization, deserialization, and HTTP response formatting.

## Basic Syntax

```tusk
# Simple API endpoint
#api /hello {
    message: "Hello, World!"
    # Automatically returns: {"message": "Hello, World!"}
}

# With explicit return
#api /users {
    users: @User.all()
    return users  # Returns JSON array of users
}

# Implicit JSON response
#api /status {
    # Last expression is returned as JSON
    {
        status: "online",
        timestamp: @now(),
        version: "1.0.0"
    }
}
```

## Route Parameters

```tusk
# Single parameter
#api /users/{id} {
    user: @User.find(@params.id)
    
    if (!user) {
        @response.status: 404
        return {error: "User not found"}
    }
    
    return user
}

# Multiple parameters
#api /posts/{year}/{month}/{day} {
    date: @params.year + "-" + @params.month + "-" + @params.day
    posts: @Post.whereDate("created_at", date).get()
    
    return {
        date: date,
        count: posts.length,
        posts: posts
    }
}

# Optional parameters
#api /search/{query}/{page?} {
    query: @params.query
    page: @params.page || 1
    
    results: @search(query, page)
    return results
}
```

## HTTP Methods

```tusk
# GET request (default)
#api /users {
    return @User.all()
}

# POST request
#api /users method: POST {
    # Access POST data via @request.post
    user_data: @request.post
    
    # Validate
    errors: @validate(user_data, {
        name: "required|string|max:255",
        email: "required|email|unique:users",
        password: "required|min:8"
    })
    
    if (errors) {
        @response.status: 422
        return {errors: errors}
    }
    
    # Create user
    user: @User.create(user_data)
    @response.status: 201
    
    return {
        message: "User created successfully",
        user: user
    }
}

# Multiple methods
#api /users/{id} method: [GET, PUT, DELETE] {
    switch (@request.method) {
        case "GET":
            return @User.find(@params.id)
            
        case "PUT":
            user: @User.find(@params.id)
            user.update(@request.post)
            return user
            
        case "DELETE":
            @User.destroy(@params.id)
            @response.status: 204
            return null
    }
}
```

## Request Handling

```tusk
# Access request data
#api /process {
    # Query parameters
    sort: @request.query.sort || "created_at"
    order: @request.query.order || "desc"
    
    # Headers
    auth_token: @request.headers.authorization
    content_type: @request.headers.content-type
    
    # POST/PUT data
    data: @request.post
    
    # Raw body
    raw_body: @request.body
    
    # Files
    uploaded_file: @request.files.document
    
    return {
        received: {
            query: @request.query,
            data: data,
            file: uploaded_file ? uploaded_file.name : null
        }
    }
}
```

## Response Control

```tusk
# Set status codes
#api /resource/{id} {
    resource: @Resource.find(@params.id)
    
    if (!resource) {
        @response.status: 404
        return {
            error: "Resource not found",
            id: @params.id
        }
    }
    
    @response.status: 200  # Default
    return resource
}

# Set headers
#api /download/{file} {
    file_path: @storage_path(@params.file)
    
    if (!@file_exists(file_path)) {
        @response.status: 404
        return {error: "File not found"}
    }
    
    @response.headers: {
        "Content-Type": "application/octet-stream",
        "Content-Disposition": `attachment; filename="${@params.file}"`,
        "Cache-Control": "no-cache"
    }
    
    return @file_get_contents(file_path)
}

# Custom response format
#api /custom {
    # Override JSON response
    @response.headers.content-type: "text/plain"
    @response.body: "Plain text response"
    @response.send()  # Bypass automatic JSON encoding
}
```

## Authentication

```tusk
# API authentication middleware
#api /protected middleware: [authenticate] {
    # @user is set by authenticate middleware
    return {
        message: "Welcome, " + @user.name,
        user: @user
    }
}

# Inline authentication
#api /admin/users {
    # Check authentication
    if (!@auth.check()) {
        @response.status: 401
        return {error: "Unauthenticated"}
    }
    
    # Check authorization
    if (!@auth.user.can("manage-users")) {
        @response.status: 403
        return {error: "Unauthorized"}
    }
    
    return @User.all()
}

# Token-based auth
#api /data {
    token: @request.headers.authorization?.replace("Bearer ", "")
    
    if (!token || !@verify_api_token(token)) {
        @response.status: 401
        return {error: "Invalid API token"}
    }
    
    return @get_sensitive_data()
}
```

## Validation

```tusk
# Input validation
#api /register method: POST {
    rules: {
        username: "required|alpha_num|min:3|max:20|unique:users",
        email: "required|email|unique:users",
        password: "required|min:8|confirmed",
        age: "required|integer|min:18",
        terms: "required|accepted"
    }
    
    validation: @validate(@request.post, rules)
    
    if (!validation.passes) {
        @response.status: 422
        return {
            message: "Validation failed",
            errors: validation.errors
        }
    }
    
    # Proceed with registration
    user: @User.create(validation.data)
    
    return {
        message: "Registration successful",
        user: user.only(["id", "username", "email"])
    }
}

# Custom validation
#api /booking method: POST {
    data: @request.post
    
    # Custom validation logic
    errors: {}
    
    if (data.start_date >= data.end_date) {
        errors.date: "End date must be after start date"
    }
    
    if (!@is_available(data.resource_id, data.start_date, data.end_date)) {
        errors.availability: "Resource not available for selected dates"
    }
    
    if (!@is_empty(errors)) {
        @response.status: 422
        return {errors: errors}
    }
    
    booking: @create_booking(data)
    return booking
}
```

## Pagination

```tusk
# Paginated responses
#api /posts {
    page: @request.query.page || 1
    per_page: @request.query.per_page || 20
    
    # Ensure reasonable limits
    per_page: @clamp(per_page, 1, 100)
    
    posts: @Post.paginate(per_page, page)
    
    return {
        data: posts.items,
        pagination: {
            current_page: posts.current_page,
            last_page: posts.last_page,
            per_page: posts.per_page,
            total: posts.total,
            from: posts.from,
            to: posts.to
        },
        links: {
            first: "/api/posts?page=1",
            last: "/api/posts?page=" + posts.last_page,
            prev: posts.prev_page_url,
            next: posts.next_page_url
        }
    }
}

# Cursor-based pagination
#api /feed {
    cursor: @request.query.cursor
    limit: @min(@request.query.limit || 20, 100)
    
    query: @Post.orderBy("created_at", "desc").limit(limit + 1)
    
    if (cursor) {
        query.where("created_at", "<", @decode_cursor(cursor))
    }
    
    posts: query.get()
    has_more: posts.length > limit
    
    if (has_more) {
        posts.pop()  # Remove extra item
    }
    
    return {
        data: posts,
        has_more: has_more,
        next_cursor: has_more ? @encode_cursor(posts.last().created_at) : null
    }
}
```

## Error Handling

```tusk
# Global API error handling
#api /risky {
    try {
        result: @risky_operation()
        return {success: true, data: result}
        
    } catch (ValidationException e) {
        @response.status: 422
        return {
            error: "Validation failed",
            details: e.errors
        }
        
    } catch (NotFoundException e) {
        @response.status: 404
        return {
            error: "Resource not found",
            resource: e.resource,
            id: e.id
        }
        
    } catch (Exception e) {
        @log.error("API error", {
            endpoint: @request.path,
            error: e.message,
            trace: e.trace
        })
        
        @response.status: 500
        return {
            error: "Internal server error",
            message: @env.debug ? e.message : "Something went wrong"
        }
    }
}

# Consistent error format
api_error: (message, code: 400, details: null) => {
    @response.status: code
    
    error: {
        message: message,
        code: code,
        timestamp: @timestamp()
    }
    
    if (details) {
        error.details: details
    }
    
    return error
}
```

## Rate Limiting

```tusk
# API rate limiting
#api /data middleware: [rate_limit:60,100] {
    # 100 requests per 60 minutes
    
    # Rate limit headers are automatically added:
    # X-RateLimit-Limit: 100
    # X-RateLimit-Remaining: 95
    # X-RateLimit-Reset: 1640000000
    
    return @get_data()
}

# Custom rate limiting
#api /expensive {
    key: @auth.user?.id || @request.ip
    limit: @auth.user ? 100 : 20  # Higher limit for authenticated
    
    if (!@rate_limiter.attempt(key, limit)) {
        @response.status: 429
        @response.headers["Retry-After"]: @rate_limiter.available_in(key)
        
        return {
            error: "Too many requests",
            retry_after: @rate_limiter.available_in(key)
        }
    }
    
    return @expensive_operation()
}
```

## API Versioning

```tusk
# Version in URL
#api /v1/users {
    # Version 1 implementation
    return @User.all(["id", "name", "email"])
}

#api /v2/users {
    # Version 2 with more fields
    return @User.all().map(user => {
        ...user,
        avatar_url: @storage_url(user.avatar),
        joined_date: user.created_at
    })
}

# Version in header
#api /users {
    version: @request.headers["api-version"] || "1.0"
    
    switch (version) {
        case "1.0":
            return @v1_response()
        case "2.0":
            return @v2_response()
        default:
            @response.status: 400
            return {error: "Unsupported API version"}
    }
}
```

## CORS Configuration

```tusk
# Enable CORS
#api /public middleware: [cors] {
    # CORS headers are automatically added
    return @public_data()
}

# Custom CORS settings
#api /data middleware: [cors:{origins:["https://app.example.com"],methods:["GET","POST"]}] {
    return @data()
}

# Manual CORS
#api /custom-cors {
    @response.headers: {
        "Access-Control-Allow-Origin": @request.headers.origin || "*",
        "Access-Control-Allow-Methods": "GET, POST, OPTIONS",
        "Access-Control-Allow-Headers": "Content-Type, Authorization",
        "Access-Control-Max-Age": "86400"
    }
    
    if (@request.method == "OPTIONS") {
        @response.status: 204
        return null
    }
    
    return @data()
}
```

## Best Practices

1. **Use consistent response formats** - Standardize success and error responses
2. **Version your APIs** - Plan for backwards compatibility
3. **Implement proper authentication** - Secure sensitive endpoints
4. **Validate all inputs** - Never trust client data
5. **Handle errors gracefully** - Return meaningful error messages
6. **Use appropriate status codes** - Follow HTTP standards
7. **Document your APIs** - Include examples and schemas
8. **Rate limit endpoints** - Prevent abuse

## Related Topics

- `hash-web-directive` - Web page endpoints
- `hash-middleware-directive` - Request middleware
- `validation` - Input validation
- `authentication` - API authentication
- `rate-limiting` - Request throttling