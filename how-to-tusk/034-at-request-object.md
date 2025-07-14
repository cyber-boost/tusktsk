# @ Request Object

The @request object provides access to HTTP request data in TuskLang web applications. This guide covers how to use @request to handle incoming HTTP requests, access headers, parse bodies, and work with query parameters.

## Overview

The @request object is available in web server contexts and contains all information about the current HTTP request:

```tusk
# Basic request information
method = @request.method        # "GET", "POST", etc.
url = @request.url             # Full URL
path = @request.path           # URL path
ip = @request.ip               # Client IP address
```

## Request Properties

### Core Properties

```tusk
# Essential request properties
request_info:
    method: @request.method           # HTTP method
    url: @request.url                # Complete URL
    path: @request.path              # Path without query string
    protocol: @request.protocol      # "http" or "https"
    hostname: @request.hostname      # Host header value
    ip: @request.ip                  # Client IP address
    timestamp: @request.timestamp    # Request timestamp
```

### URL Components

```tusk
# For URL: https://api.example.com:8080/users/123?filter=active&page=2

url_parts:
    protocol: @request.protocol      # "https"
    hostname: @request.hostname      # "api.example.com"
    port: @request.port             # 8080
    path: @request.path             # "/users/123"
    pathname: @request.pathname      # Same as path
    search: @request.search         # "?filter=active&page=2"
    href: @request.href             # Full URL
```

## Request Headers

### Accessing Headers

```tusk
# Access specific headers
auth_token = @request.headers.authorization
content_type = @request.headers["content-type"]
user_agent = @request.headers.user_agent

# Common headers
headers:
    auth: @request.headers.authorization
    content_type: @request.headers["content-type"]
    accept: @request.headers.accept
    user_agent: @request.headers["user-agent"]
    referer: @request.headers.referer
    host: @request.headers.host
```

### Header Normalization

```tusk
# Headers are normalized to lowercase
# These all access the same header:
auth1 = @request.headers.authorization
auth2 = @request.headers.Authorization
auth3 = @request.headers.AUTHORIZATION
auth4 = @request.headers["authorization"]

# Custom headers
api_key = @request.headers["x-api-key"]
request_id = @request.headers["x-request-id"]
```

## Query Parameters

### Basic Query Access

```tusk
# URL: /search?q=tusklang&page=1&limit=10

# Access individual parameters
search_query = @request.query.q      # "tusklang"
page = @request.query.page          # "1" (string)
limit = @request.query.limit        # "10" (string)

# With type conversion
page_num = @int(@request.query.page ?? 1)
limit_num = @int(@request.query.limit ?? 10)
```

### Query Parameter Patterns

```tusk
# Handle missing parameters
search_params:
    query: @request.query.q ?? ""
    page: @int(@request.query.page ?? 1)
    limit: @int(@request.query.limit ?? 20)
    sort: @request.query.sort ?? "relevance"
    order: @request.query.order ?? "desc"

# Array parameters (?tags[]=js&tags[]=web)
tags = @request.query.tags ?? []

# Boolean parameters
include_deleted = @request.query.deleted == "true"
is_active = @request.query.active != "false"
```

## Request Body

### Accessing Body Data

```tusk
# Raw body
raw_body = @request.body

# Parsed JSON body (automatic for application/json)
json_data = @request.body  # Already parsed if JSON

# Form data (application/x-www-form-urlencoded)
form_data = @request.body

# Access specific fields
user_name = @request.body.name
user_email = @request.body.email
```

### Body Parsing Patterns

```tusk
# Safe body access
create_user = @lambda({
    # Validate content type
    @if(@request.headers["content-type"] != "application/json", {
        return: { error: "Content-Type must be application/json" }
    })
    
    # Safe field access
    name = @request.body?.name
    email = @request.body?.email
    
    # Validation
    @if(!name || !email, {
        return: { error: "Name and email required" }
    })
    
    # Process...
})
```

### Multipart/Form-Data

```tusk
# Handle file uploads
upload_handler = @lambda({
    # Check if multipart
    is_multipart = @includes(@request.headers["content-type"], "multipart/form-data")
    
    @if(is_multipart, {
        # Access files
        uploaded_file = @request.files.upload
        
        file_info: {
            filename: @uploaded_file.filename
            mimetype: @uploaded_file.mimetype
            size: @uploaded_file.size
            data: @uploaded_file.data
        }
        
        # Access form fields
        title = @request.body.title
        description = @request.body.description
    })
})
```

## HTTP Methods

### Method-Based Routing

```tusk
# Route based on HTTP method
handle_request = @lambda({
    @switch(@request.method, {
        "GET": @handle_get(),
        "POST": @handle_post(),
        "PUT": @handle_put(),
        "DELETE": @handle_delete(),
        "PATCH": @handle_patch(),
        default: {
            status: 405
            body: { error: "Method not allowed" }
        }
    })
})

# RESTful patterns
user_routes:
    # GET /users - List users
    GET: @lambda({
        users = @db.query("SELECT * FROM users")
        return: { users: users }
    })
    
    # POST /users - Create user
    POST: @lambda({
        user = @create_user(@request.body)
        return: { user: user }
    })
```

## Path Parameters

### Extract Path Parameters

```tusk
# Route: /users/:id/posts/:postId

# Manual extraction
extract_params = @lambda(pattern, path, {
    # Implementation to extract :id and :postId
})

# With routing library
route_params = @request.params  # { id: "123", postId: "456" }

# Access parameters
user_id = @request.params.id
post_id = @request.params.postId
```

### Dynamic Routing

```tusk
# Route patterns
routes:
    "/users/:id": @lambda({
        user_id = @request.params.id
        user = @db.find("users", user_id)
        return: { user: user }
    })
    
    "/users/:id/posts/:postId": @lambda({
        user_id = @request.params.id
        post_id = @request.params.postId
        post = @db.query(
            "SELECT * FROM posts WHERE user_id = ? AND id = ?",
            user_id, post_id
        )
        return: { post: post }
    })
```

## Request Context

### User and Session

```tusk
# Access authenticated user
current_user = @request.user
is_authenticated = @request.user != null

# Session data
session_id = @request.session.id
session_data = @request.session.data
user_preferences = @request.session.preferences

# Authentication check
require_auth = @lambda(handler, {
    return: @lambda({
        @if(!@request.user, {
            return: {
                status: 401
                body: { error: "Authentication required" }
            }
        })
        
        return: @handler()
    })
})
```

### Request Metadata

```tusk
# Additional request context
context:
    request_id: @request.id ?? @uuid()
    timestamp: @request.timestamp
    processing_time: @time.now() - @request.timestamp
    
    # Geo information (if available)
    geo: @request.geo ?? {
        country: "unknown"
        region: "unknown"
        city: "unknown"
    }
    
    # Device information
    is_mobile: @includes(@request.headers["user-agent"], "Mobile")
    is_bot: @regex.test(@request.headers["user-agent"], "bot|crawler|spider")
```

## Request Validation

### Input Validation

```tusk
# Comprehensive request validation
validate_request = @lambda(rules, {
    errors: []
    
    # Validate method
    @if(rules.method && @request.method != rules.method, {
        @push(errors, "Method must be ${rules.method}")
    })
    
    # Validate headers
    @each(rules.required_headers ?? [], @lambda(header, {
        @if(!@request.headers[header], {
            @push(errors, "Missing required header: ${header}")
        })
    }))
    
    # Validate body
    @each(rules.required_fields ?? [], @lambda(field, {
        @if(!@request.body?.[field], {
            @push(errors, "Missing required field: ${field}")
        })
    }))
    
    # Validate query params
    @each(rules.required_params ?? [], @lambda(param, {
        @if(!@request.query[param], {
            @push(errors, "Missing required parameter: ${param}")
        })
    }))
    
    return: {
        valid: @len(errors) == 0
        errors: errors
    }
})
```

### Schema Validation

```tusk
# Define request schema
user_create_schema:
    method: "POST"
    headers: {
        "content-type": "application/json"
    }
    body: {
        name: { type: "string", required: true, min: 2, max: 100 }
        email: { type: "string", required: true, pattern: "^[^@]+@[^@]+$" }
        age: { type: "number", required: false, min: 13, max: 120 }
    }

# Validate against schema
validate_create_user = @lambda({
    validation = @validate_request_schema(@request, @user_create_schema)
    
    @if(!validation.valid, {
        return: {
            status: 400
            body: { errors: validation.errors }
        }
    })
    
    # Process valid request...
})
```

## Security Considerations

### CORS Headers

```tusk
# Handle CORS
cors_middleware = @lambda(handler, {
    return: @lambda({
        # Set CORS headers
        headers: {
            "Access-Control-Allow-Origin": @env.CORS_ORIGIN ?? "*"
            "Access-Control-Allow-Methods": "GET, POST, PUT, DELETE, OPTIONS"
            "Access-Control-Allow-Headers": "Content-Type, Authorization"
        }
        
        # Handle preflight
        @if(@request.method == "OPTIONS", {
            return: { status: 204, headers: headers }
        })
        
        # Process request
        response = @handler()
        response.headers = @merge(response.headers ?? {}, headers)
        return: response
    })
})
```

### Rate Limiting

```tusk
# Simple rate limiting
rate_limit = @lambda(max_requests, window_seconds, {
    key: "${@request.ip}_${@request.path}"
    count = @cache.get(key) ?? 0
    
    @if(count >= max_requests, {
        return: {
            status: 429
            body: { error: "Too many requests" }
            headers: {
                "X-RateLimit-Limit": max_requests
                "X-RateLimit-Remaining": 0
                "Retry-After": window_seconds
            }
        }
    })
    
    @cache.set(key, count + 1, window_seconds)
    return: null  # Continue processing
})
```

## Common Patterns

### RESTful API Handler

```tusk
# Generic REST handler
create_rest_handler = @lambda(resource_name, {
    return: {
        # GET /resources
        list: @lambda({
            page = @int(@request.query.page ?? 1)
            limit = @int(@request.query.limit ?? 20)
            
            items = @db.query(
                "SELECT * FROM ${resource_name} LIMIT ? OFFSET ?",
                limit, (page - 1) * limit
            )
            
            return: { 
                [resource_name]: items,
                page: page,
                limit: limit
            }
        })
        
        # GET /resources/:id
        get: @lambda({
            id = @request.params.id
            item = @db.find(resource_name, id)
            
            @if(!item, {
                return: { status: 404, body: { error: "Not found" }}
            })
            
            return: { [resource_name]: item }
        })
        
        # POST /resources
        create: @lambda({
            data = @request.body
            item = @db.insert(resource_name, data)
            return: { status: 201, body: { [resource_name]: item }}
        })
    }
})
```

### Request Logging

```tusk
# Log all requests
request_logger = @lambda(handler, {
    return: @lambda({
        start_time = @time.now()
        
        # Log request
        @log.info({
            method: @request.method
            path: @request.path
            ip: @request.ip
            user_agent: @request.headers["user-agent"]
            timestamp: start_time
        })
        
        # Process request
        response = @handler()
        
        # Log response
        @log.info({
            method: @request.method
            path: @request.path
            status: response.status ?? 200
            duration: @time.now() - start_time
        })
        
        return: response
    })
})
```

## Best Practices

1. **Always validate input** - Never trust request data
2. **Use type conversion** - Convert query/body values to expected types
3. **Handle missing data** - Use fallbacks for optional parameters
4. **Set security headers** - CORS, CSP, etc.
5. **Log requests** - For debugging and monitoring
6. **Rate limit** - Protect against abuse
7. **Sanitize output** - Prevent XSS when rendering user input

## Next Steps

- Learn about [Request Methods](035-at-request-method.md)
- Explore [Query Parameters](036-at-request-query.md)
- Master [Request Body Handling](037-at-request-body.md)