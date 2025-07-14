# #rate_limit - Rate Limiting Directive

The `#rate_limit` directive provides request throttling to prevent abuse, protect resources, and ensure fair usage of your application.

## Basic Syntax

```tusk
# Basic rate limiting - 60 requests per minute
#rate_limit 60 {
    #api /search {
        results: @search(@request.query.q)
        return results
    }
}

# Custom window - 100 requests per hour
#rate_limit 100 per: "hour" {
    #api /data {
        return @fetch_data()
    }
}

# With custom key
#rate_limit 30 key: @request.ip {
    #web /download {
        @serve_file()
    }
}
```

## Rate Limit Configuration

```tusk
# Detailed configuration
#rate_limit {
    limit: 100           # Number of requests
    window: 3600         # Time window in seconds
    key: @auth.id || @request.ip  # Rate limit key
    message: "Too many requests"    # Error message
    response_code: 429   # HTTP status code
} {
    #api /endpoint {
        @process_request()
    }
}

# Per-minute limiting
#rate_limit 60 per: "minute" {
    #api /rapid-endpoint {
        return @quick_response()
    }
}

# Multiple windows
#rate_limit {
    limits: [
        {count: 10, window: 60},      # 10 per minute
        {count: 100, window: 3600},    # 100 per hour
        {count: 1000, window: 86400}   # 1000 per day
    ]
} {
    #api /tiered-limit {
        @handle_request()
    }
}
```

## User-Based Rate Limiting

```tusk
# Different limits for different users
#rate_limit {
    limit: () => {
        if (!@auth.check()) return 20  # Guests
        if (@auth.user.isPremium()) return 1000  # Premium users
        return 100  # Regular users
    }
    window: 3600
    key: @auth.id || @request.ip
} {
    #api /user-endpoint {
        @process()
    }
}

# Role-based limits
#rate_limit {
    limits: {
        guest: {count: 10, window: 3600},
        user: {count: 100, window: 3600},
        premium: {count: 1000, window: 3600},
        admin: null  # No limit
    }
    role: @auth.user?.role || "guest"
} {
    #api /role-based {
        @handle()
    }
}
```

## API Key Rate Limiting

```tusk
# Rate limit by API key
#rate_limit {
    key: @request.headers["X-API-Key"]
    limit: () => {
        api_key: @ApiKey.where("key", @request.headers["X-API-Key"]).first()
        return api_key?.rate_limit || 100
    }
    window: 3600
} {
    #api /external-api {
        @serve_api_request()
    }
}

# Tiered API limits
#rate_limit {
    key: @request.api_key
    limits: () => {
        tier: @get_api_tier(@request.api_key)
        
        return match tier {
            "free" => [{count: 100, window: 86400}]
            "basic" => [{count: 1000, window: 3600}]
            "pro" => [{count: 10000, window: 3600}]
            "enterprise" => []  # No limits
        }
    }
} {
    #api /v2/* {
        @handle_api_v2()
    }
}
```

## Route-Specific Limits

```tusk
# Different limits for different routes
#rate_limit {
    limit: match @request.path {
        "/api/search" => 30
        "/api/heavy-operation" => 5
        "/api/lightweight" => 200
        _ => 60  # Default
    }
    window: 3600
} {
    #api /* {
        @route_handler()
    }
}

# Method-based limits
#rate_limit {
    limit: match @request.method {
        "GET" => 100
        "POST" => 50
        "PUT" => 50
        "DELETE" => 20
    }
    window: 3600
} {
    #api /resources {
        @handle_resource()
    }
}
```

## Cost-Based Rate Limiting

```tusk
# Point-based system
#rate_limit {
    points: 1000  # Total points per window
    window: 3600
    
    # Different operations cost different points
    cost: () => {
        return match @request.path {
            "/api/simple" => 1
            "/api/moderate" => 10
            "/api/expensive" => 100
            "/api/ai-generate" => 500
        }
    }
} {
    #api /* {
        @handle_with_cost()
    }
}

# Dynamic cost calculation
#rate_limit {
    points: 10000
    window: 86400  # Daily limit
    
    cost: () => {
        # Calculate based on request complexity
        base_cost: 1
        
        if (@request.query.include_relations) {
            base_cost *= 2
        }
        
        if (@request.query.limit > 100) {
            base_cost *= 3
        }
        
        return base_cost
    }
} {
    #api /flexible-endpoint {
        @process_flexible()
    }
}
```

## Response Headers

```tusk
# Include rate limit headers
#rate_limit 100 per: "hour" headers: true {
    #api /with-headers {
        # Automatically adds:
        # X-RateLimit-Limit: 100
        # X-RateLimit-Remaining: 95
        # X-RateLimit-Reset: 1640995200
        
        return @data()
    }
}

# Custom headers
#rate_limit {
    limit: 60
    window: 3600
    headers: {
        limit: "X-Rate-Limit"
        remaining: "X-Rate-Remaining"
        reset: "X-Rate-Reset"
        retry_after: "Retry-After"  # On 429 response
    }
} {
    #api /custom-headers {
        @process()
    }
}
```

## Bypass and Whitelist

```tusk
# Bypass for certain conditions
#rate_limit {
    limit: 60
    bypass: () => {
        # Skip rate limiting for admins
        if (@auth.user?.isAdmin()) return true
        
        # Skip for internal IPs
        if (@in_array(@request.ip, @config.internal_ips)) return true
        
        # Skip for specific user agents
        if (@request.user_agent == "Internal-Monitor") return true
        
        return false
    }
} {
    #api /conditional {
        @handle()
    }
}

# Whitelist specific keys
#rate_limit {
    limit: 100
    whitelist: [
        "api_key_123abc",  # Specific API keys
        "192.168.1.1",     # Specific IPs
    ]
    key: @request.api_key || @request.ip
} {
    #api /whitelist-example {
        @process()
    }
}
```

## Storage Backends

```tusk
# Redis backend (default)
#rate_limit 100 store: "redis" {
    #api /redis-limited {
        @handle()
    }
}

# Database backend
#rate_limit 100 store: "database" table: "rate_limits" {
    #api /db-limited {
        @handle()
    }
}

# Memory backend (single server only)
#rate_limit 100 store: "memory" {
    #api /memory-limited {
        @handle()
    }
}

# Custom store
#rate_limit {
    limit: 100
    store: {
        get: (key) => @custom_store.get(key)
        increment: (key, window) => @custom_store.increment(key, window)
        reset: (key) => @custom_store.reset(key)
    }
} {
    #api /custom-store {
        @handle()
    }
}
```

## Distributed Rate Limiting

```tusk
# Sliding window with Redis
#rate_limit {
    limit: 1000
    window: 3600
    algorithm: "sliding_window"  # More accurate than fixed window
    store: "redis"
} {
    #api /precise-limit {
        @handle()
    }
}

# Token bucket algorithm
#rate_limit {
    capacity: 100      # Bucket size
    refill_rate: 10    # Tokens per second
    algorithm: "token_bucket"
} {
    #api /smooth-limit {
        @handle()
    }
}

# Distributed rate limiting across servers
#rate_limit {
    limit: 10000
    window: 3600
    store: "redis_cluster"
    sync_interval: 100  # Sync counts every 100ms
} {
    #api /distributed {
        @handle()
    }
}
```

## Error Handling

```tusk
# Custom error response
#rate_limit {
    limit: 60
    on_limit: () => {
        @response.status: 429
        @response.headers["Retry-After"]: @rate_limiter.retry_after()
        
        return @json({
            error: "Rate limit exceeded",
            message: "Please slow down your requests",
            retry_after: @rate_limiter.retry_after(),
            limit: @rate_limiter.limit(),
            window: @rate_limiter.window()
        })
    }
} {
    #api /custom-error {
        @handle()
    }
}

# Different responses for different clients
#rate_limit {
    limit: 100
    on_limit: () => {
        if (@request.wants_json()) {
            @response.status: 429
            return @json({
                error: "Too many requests",
                retry_after: @rate_limiter.retry_after()
            })
        } else {
            @response.status: 429
            return @render("errors/rate_limit.tusk", {
                retry_after: @rate_limiter.retry_after()
            })
        }
    }
} {
    #web /mixed-response {
        @handle()
    }
}
```

## Monitoring and Analytics

```tusk
# Log rate limit hits
#rate_limit {
    limit: 100
    on_limit: () => {
        @log.warning("Rate limit hit", {
            key: @rate_limiter.key(),
            path: @request.path,
            ip: @request.ip,
            user_id: @auth.id
        })
        
        # Track metrics
        @metrics.increment("rate_limit.hit", {
            path: @request.path,
            key_type: @auth.check() ? "user" : "ip"
        })
        
        # Standard 429 response
        @abort(429)
    }
    
    on_success: () => {
        # Track successful requests
        @metrics.increment("rate_limit.allowed", {
            path: @request.path,
            remaining: @rate_limiter.remaining()
        })
    }
} {
    #api /monitored {
        @handle()
    }
}

# Rate limit analytics
#api /admin/rate-limits {
    stats: @rate_limiter.stats({
        paths: ["/api/search", "/api/data"],
        period: "hour",
        group_by: "key"
    })
    
    return {
        top_consumers: stats.top_consumers,
        hit_rate: stats.hit_rate,
        patterns: stats.patterns
    }
}
```

## Testing Rate Limits

```tusk
# Disable in tests
#rate_limit {
    limit: 100
    enabled: @env.APP_ENV != "testing"
} {
    #api /testable {
        @handle()
    }
}

# Test mode with different limits
#rate_limit {
    limit: @env.APP_ENV == "testing" ? 10000 : 100
    window: @env.APP_ENV == "testing" ? 60 : 3600
} {
    #api /test-friendly {
        @handle()
    }
}
```

## Best Practices

1. **Choose appropriate limits** - Balance security and usability
2. **Use meaningful keys** - User ID > IP for authenticated routes
3. **Implement gradual limits** - Multiple windows prevent bursts
4. **Include headers** - Help clients manage their requests
5. **Log violations** - Monitor for abuse patterns
6. **Handle errors gracefully** - Provide clear feedback
7. **Consider costs** - Some operations are more expensive
8. **Test thoroughly** - Ensure limits work as expected

## Related Topics

- `hash-middleware-directive` - Request middleware
- `caching` - Response caching
- `security` - Application security
- `monitoring` - Performance monitoring
- `api-design` - API best practices