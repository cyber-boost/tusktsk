# #cache - Caching Directive

The `#cache` directive provides declarative caching for routes and code blocks, improving performance by storing computed results and responses.

## Basic Syntax

```tusk
# Cache route response for 1 hour
#cache 3600 {
    #web /expensive-page {
        data: @perform_expensive_computation()
        @render("page.tusk", {data})
    }
}

# Cache with custom key
#cache key: "homepage" duration: 7200 {
    #web / {
        @render("home.tusk", @get_homepage_data())
    }
}

# Cache forever
#cache forever: true key: "static-content" {
    content: @load_static_content()
    return content
}
```

## Cache Keys

```tusk
# Automatic cache key generation
#cache 3600 {
    #web /products {
        # Key auto-generated from: method + path + query string
        # e.g., "GET:/products?page=2&sort=price"
        products: @Product.paginate(20)
        @render("products.tusk", {products})
    }
}

# Custom cache key
#cache key: "products:{page}:{sort}" duration: 1800 {
    #web /products {
        page: @request.query.page || 1
        sort: @request.query.sort || "name"
        
        products: @Product.orderBy(sort).paginate(20, page)
        @render("products.tusk", {products})
    }
}

# Dynamic cache key
#cache key: () => "user:" + @auth.id + ":dashboard" duration: 600 {
    #web /dashboard {
        # Cached per user
        @render("dashboard.tusk", @get_user_dashboard_data())
    }
}

# Multiple key segments
#cache key: ["products", @request.query.category, @request.query.page] {
    # Creates key like: "products:electronics:2"
    @render_product_list()
}
```

## Cache Tags

```tusk
# Tag-based cache invalidation
#cache tags: ["products"] duration: 3600 {
    #web /products {
        @render("products.tusk", {
            products: @Product.all()
        })
    }
}

#cache tags: ["products", "homepage"] duration: 7200 {
    #web / {
        @render("home.tusk", {
            featured: @Product.featured()
        })
    }
}

# Invalidate by tag
#web /admin/products/update method: POST {
    @update_product(@request.post)
    
    # Clear all caches tagged with "products"
    @cache.tags("products").flush()
    
    @redirect("/admin/products")
}

# Multiple tags with specific items
#cache tags: () => ["user:" + @auth.id, "posts"] {
    #web /my-posts {
        posts: @auth.user.posts
        @render("posts/mine.tusk", {posts})
    }
}
```

## Conditional Caching

```tusk
# Cache only for guests
#cache when: !@auth.check() duration: 3600 {
    #web /public-content {
        @render("public.tusk")
    }
}

# Cache based on conditions
#cache when: @request.query.nocache != "1" {
    #web /data {
        @expensive_operation()
    }
}

# Dynamic cache duration
#cache duration: @auth.check() ? 300 : 3600 {
    #web /content {
        # 5 minutes for logged in users, 1 hour for guests
        @render("content.tusk")
    }
}

# Skip cache for certain conditions
#cache unless: @request.query.preview == "1" {
    #web /article/{slug} {
        article: @Article.findBySlug(@params.slug)
        @render("article.tusk", {article})
    }
}
```

## Cache Stores

```tusk
# Use specific cache store
#cache store: "redis" duration: 3600 {
    #web /api-data {
        @json(@fetch_external_api_data())
    }
}

# File-based cache
#cache store: "file" path: "pages" {
    #web /static-page {
        @render("static.tusk")
    }
}

# Memory cache (request-level)
#cache store: "array" {
    # Cached only for current request
    @expensive_calculation()
}

# Multiple stores (cache hierarchy)
#cache stores: ["array", "redis"] {
    # Checks array first, then redis
    # Stores in both when cache miss
    @complex_operation()
}
```

## Response Caching

```tusk
# Full response caching
#cache response: true duration: 3600 {
    #web /full-page {
        # Entire response including headers is cached
        @response.headers["X-Custom"]: "Value"
        @render("page.tusk")
    }
}

# Partial response caching
#cache response: {
    headers: ["Content-Type", "X-Custom"]
    status: true
} {
    #web /partial {
        @response.headers["X-Dynamic"]: @time()  # Not cached
        @response.headers["X-Custom"]: "Static"  # Cached
        @render("partial.tusk")
    }
}

# Cache with vary headers
#cache vary: ["Accept-Language", "Accept"] {
    #web /content {
        # Different cache entries for different Accept-Language headers
        lang: @request.headers["Accept-Language"]
        @render("content/" + lang + ".tusk")
    }
}
```

## Fragment Caching

```tusk
# Cache fragments within routes
#web /complex-page {
    # Cache expensive parts
    sidebar: #cache key: "sidebar" duration: 7200 {
        @render_sidebar()
    }
    
    # Cache with tags
    products: #cache tags: ["products"] duration: 3600 {
        @Product.featured().limit(10).get()
    }
    
    # Don't cache user-specific content
    user_data: @get_user_specific_data()
    
    @render("complex.tusk", {
        sidebar: sidebar
        products: products
        user_data: user_data
    })
}

# Nested caching
#cache key: "page:home" duration: 3600 {
    header: #cache key: "header" duration: 7200 {
        @render("partials/header.tusk")
    }
    
    content: #cache key: "home:content" duration: 1800 {
        @render("home/content.tusk")
    }
    
    @render("layout.tusk", {header, content})
}
```

## Cache Warming

```tusk
# Pre-warm cache
#cache warm: true key: "expensive-data" {
    @calculate_expensive_metrics()
}

# Warm cache on schedule
#cron "0 * * * *" {
    # Warm critical caches every hour
    #cache warm: true key: "homepage" {
        @render_homepage()
    }
    
    #cache warm: true key: "products:featured" {
        @Product.featured().get()
    }
}

# Warm cache after events
#on product.updated {
    product: @event.product
    
    # Re-warm related caches
    #cache warm: true key: "product:" + product.id {
        @render_product_page(product)
    }
    
    #cache warm: true tags: ["category:" + product.category_id] {
        @render_category_page(product.category)
    }
}
```

## Cache Locking

```tusk
# Prevent cache stampede
#cache lock: true lock_timeout: 30 {
    #web /popular-endpoint {
        # Only one process regenerates cache
        # Others wait or use stale data
        @expensive_operation()
    }
}

# Custom lock behavior
#cache lock: {
    timeout: 60
    wait: true  # Wait for lock
    stale: true  # Serve stale data while waiting
} {
    @heavy_computation()
}

# Async cache regeneration
#cache lock: false async: true {
    #web /data {
        # Returns stale data immediately
        # Regenerates cache in background
        @slow_data_fetch()
    }
}
```

## Cache Configuration

```tusk
# Cache with options
#cache {
    key: "custom-key"
    duration: 3600
    tags: ["api", "v2"]
    store: "redis"
    compress: true  # Compress large values
    serialize: "json"  # How to serialize data
} {
    @complex_data_structure()
}

# TTL with jitter (prevent simultaneous expiry)
#cache duration: 3600 jitter: 300 {
    # Expires between 3300-3900 seconds
    @api_call()
}

# Sliding expiration
#cache duration: 1800 sliding: true {
    # Extends TTL on each access
    @frequently_accessed_data()
}
```

## Debugging Cache

```tusk
# Debug mode
#cache debug: true {
    #web /test {
        # Logs cache hits/misses
        # Adds debug headers
        @render("test.tusk")
    }
}

# Skip cache in development
#cache unless: @env.debug {
    #web /page {
        @render("page.tusk")
    }
}

# Cache statistics
#cache track: true key: "tracked-operation" {
    @operation()
}

# Later: View cache stats
stats: @cache.stats("tracked-operation")
// {hits: 150, misses: 23, hit_rate: 0.867}
```

## Cache Invalidation

```tusk
# Manual invalidation
#web /admin/clear-cache {
    # Clear specific keys
    @cache.forget("homepage")
    @cache.forget(["products:1", "products:2"])
    
    # Clear by pattern
    @cache.forgetPattern("products:*")
    
    # Clear by tags
    @cache.tags(["products", "homepage"]).flush()
    
    # Clear everything
    @cache.flush()
}

# Automatic invalidation
#cache invalidate_on: ["product.created", "product.updated"] {
    #web /products {
        @render_product_list()
    }
}

# Time-based invalidation
#cache until: "tomorrow" {
    # Cache until midnight
    @daily_report()
}

#cache until: "2024-12-31 23:59:59" {
    # Cache until specific time
    @yearly_summary()
}
```

## Performance Optimization

```tusk
# Batch cache operations
#cache batch: true {
    // Multiple cache operations are batched
    data1: @cache.get("key1")
    data2: @cache.get("key2")
    data3: @cache.get("key3")
}

# Pipeline cache
#cache pipeline: true {
    @cache.put("key1", value1)
    @cache.put("key2", value2)
    @cache.increment("counter")
}

# Lazy cache loading
#cache lazy: true key: "expensive" {
    # Only loads if actually accessed
    @very_expensive_operation()
}
```

## Best Practices

1. **Choose appropriate TTLs** - Balance freshness vs performance
2. **Use cache tags** - Easier invalidation of related data
3. **Key naming conventions** - Use consistent, descriptive keys
4. **Monitor hit rates** - Track cache effectiveness
5. **Handle cache misses** - Always have fallback logic
6. **Compress large values** - Save memory and bandwidth
7. **Avoid caching user data** - Or use user-specific keys
8. **Test cache invalidation** - Ensure data consistency

## Related Topics

- `cache-operations` - Cache manipulation functions
- `performance-optimization` - Performance strategies
- `cache-drivers` - Different cache backends
- `cache-tags` - Tag-based invalidation
- `cache-warming` - Pre-loading cache