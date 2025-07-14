# @cache - Caching System

The `@cache` operator provides a powerful caching layer for improving application performance by storing frequently accessed data in memory.

## Basic Syntax

```tusk
# Store value in cache
@cache.set("key", "value")

# Get value from cache
value: @cache.get("key")

# Store with expiration (seconds)
@cache.set("user:123", @user_data, 3600)  # 1 hour

# Get with default fallback
value: @cache.get("key", "default value")

# Delete from cache
@cache.delete("key")

# Clear entire cache
@cache.flush()
```

## Cache Stores

```tusk
# Different cache stores
memory_cache: @cache.store("memory")     # In-memory (default)
redis_cache: @cache.store("redis")       # Redis backend
file_cache: @cache.store("file")         # File-based
memcached: @cache.store("memcached")     # Memcached

# Use specific store
@redis_cache.set("key", "value", 3600)
value: @redis_cache.get("key")
```

## Caching Patterns

```tusk
# Cache-aside pattern
get_user: (id) => {
    cache_key: "user:" + @id
    
    # Try cache first
    user: @cache.get(@cache_key)
    
    @if(!@user) {
        # Cache miss - load from database
        user: @query("SELECT * FROM users WHERE id = ?", [@id])
        
        @if(@user) {
            # Store in cache for 1 hour
            @cache.set(@cache_key, @user, 3600)
        }
    }
    
    return @user
}

# Remember pattern
products: @cache.remember("products:active", 3600, () => {
    # This closure runs only on cache miss
    return @query("SELECT * FROM products WHERE active = 1")
})
```

## Cache Tags

```tusk
# Tag-based caching
@cache.tags(["products", "homepage"]).set("featured_products", @products, 3600)

# Get tagged cache
products: @cache.tags(["products"]).get("featured_products")

# Flush by tag
@cache.tags(["products"]).flush()  # Clears all product caches

# Multiple tags
@cache.tags(["user:123", "posts"]).set("user_posts", @posts)

# Clear specific user's caches
@cache.tags(["user:123"]).flush()
```

## Cache Invalidation

```tusk
# Update product and clear related caches
update_product: (id, data) => {
    # Update database
    @query("UPDATE products SET ? WHERE id = ?", [@data, @id])
    
    # Clear specific caches
    @cache.delete("product:" + @id)
    @cache.delete("products:all")
    @cache.tags(["products", "category:" + @data.category_id]).flush()
    
    # Clear page caches
    @cache.delete("page:home")
    @cache.delete("page:category:" + @data.category_id)
}

# Time-based invalidation
@cache.set("stats:daily", @daily_stats, @seconds_until_midnight())

# Event-based invalidation
on_order_placed: (order) => {
    # Clear product stock cache
    @cache.delete("product:stock:" + @order.product_id)
    
    # Clear user order cache
    @cache.delete("user:orders:" + @order.user_id)
    
    # Update counters
    @cache.increment("stats:orders:today")
}
```

## Atomic Operations

```tusk
# Increment/decrement
@cache.increment("page_views")
@cache.increment("product:views:123", 1)
@cache.decrement("stock:product:456", 1)

# Atomic add (only if doesn't exist)
@if(@cache.add("lock:process", 1, 60)) {
    # Got the lock, process...
    @do_exclusive_operation()
    @cache.delete("lock:process")
} else {
    # Another process has the lock
    error: "Process already running"
}

# Compare and swap
current: @cache.get("counter")
@cache.cas("counter", @current, @current + 1)
```

## Cache Warming

```tusk
# Warm cache on startup
warm_cache: {
    # Pre-load frequently accessed data
    categories: @query("SELECT * FROM categories WHERE active = 1")
    @cache.set("categories:all", @categories, 86400)  # 24 hours
    
    # Pre-load configuration
    config: @load_config()
    @cache.set("app:config", @config, 3600)
    
    # Pre-generate expensive computations
    @foreach(@categories as @category) {
        products: @query("SELECT * FROM products WHERE category_id = ?", [@category.id])
        @cache.set("products:category:" + @category.id, @products, 3600)
    }
}

# Scheduled cache warming
#cron "0 * * * *" {
    # Refresh cache every hour
    @warm_popular_products_cache()
    @warm_user_stats_cache()
}
```

## Distributed Caching

```tusk
# Configure distributed cache
cache_config: {
    default: "redis"
    stores: {
        redis: {
            driver: "redis"
            host: @env.REDIS_HOST|"localhost"
            port: @env.REDIS_PORT|6379
            database: 0
            prefix: @env.APP_NAME + ":"
        }
    }
}

# Cache with namespacing
app_cache: @cache.namespace(@env.APP_NAME)
@app_cache.set("settings", @settings)

# Cache across servers
session_cache: @cache.store("redis").namespace("sessions")
@session_cache.set(@session_id, @session_data, 1800)  # 30 minutes
```

## Query Result Caching

```tusk
# Cache database queries
get_products: (category_id, page: 1) => {
    cache_key: "products:cat:" + @category_id + ":page:" + @page
    
    return @cache.remember(@cache_key, 3600, () => {
        return @query("
            SELECT * FROM products 
            WHERE category_id = ? 
            ORDER BY created_at DESC 
            LIMIT ?, 20
        ", [@category_id, (@page - 1) * 20])
    })
}

# Cache with dependencies
get_product_with_reviews: (id) => {
    product: @cache.remember("product:" + @id, 3600, () => {
        return @query("SELECT * FROM products WHERE id = ?", [@id])
    })
    
    reviews: @cache.remember("reviews:product:" + @id, 1800, () => {
        return @query("SELECT * FROM reviews WHERE product_id = ?", [@id])
    })
    
    return {
        product: @product
        reviews: @reviews
    }
}
```

## Page Caching

```tusk
# Full page caching
#web /page/{slug} {
    cache_key: "page:" + @slug + ":" + @request.query_string
    
    # Check cache
    cached_html: @cache.get(@cache_key)
    
    @if(@cached_html) {
        @response.headers.x-cache: "HIT"
        @output(@cached_html)
        return
    }
    
    # Generate page
    page: @query("SELECT * FROM pages WHERE slug = ?", [@slug])
    html: @render("page.tusk", {page: @page})
    
    # Cache for 1 hour
    @cache.set(@cache_key, @html, 3600)
    
    @response.headers.x-cache: "MISS"
    @output(@html)
}
```

## Fragment Caching

```tusk
# In templates
<div class="sidebar">
    {@cache.remember("sidebar:user:" + @user.id, 1800, () => {
        return @render("partials/user_sidebar.tusk", {user: @user})
    })}
</div>

<div class="products">
    @foreach(@categories as @category) {
        {@cache.remember("category:box:" + @category.id, 3600, () => {
            return @render("partials/category_box.tusk", {
                category: @category,
                products: @get_category_products(@category.id, 5)
            })
        })}
    }
</div>
```

## Cache Monitoring

```tusk
# Cache statistics
cache_stats: {
    # Get cache statistics
    stats: @cache.stats()
    
    # Monitor hit rate
    hits: @stats.hits
    misses: @stats.misses
    hit_rate: @hits / (@hits + @misses) * 100
    
    # Memory usage
    memory_used: @stats.memory_used
    memory_limit: @stats.memory_limit
    memory_percent: @memory_used / @memory_limit * 100
}

# Log cache performance
#middleware cache_monitor {
    start: @microtime(true)
    
    # Process request...
    
    duration: @microtime(true) - @start
    
    @if(@duration > 0.1) {  # Log slow cache operations
        @log.warning("Slow cache operation", {
            duration: @duration,
            key: @cache_key,
            operation: @cache_operation
        })
    }
}
```

## Cache Configuration

```tusk
# Application cache configuration
cache: {
    # Default TTLs by type
    ttl: {
        default: 3600        # 1 hour
        user: 1800          # 30 minutes  
        product: 7200       # 2 hours
        static: 86400       # 24 hours
        session: 1800       # 30 minutes
    }
    
    # Size limits
    limits: {
        max_key_length: 250
        max_value_size: 1048576  # 1MB
        max_entries: 10000
    }
    
    # Eviction policy
    eviction: "lru"  # Least Recently Used
}

# Helper for consistent TTLs
cache_ttl: (type: "default") => {
    return @cache.ttl[@type]|@cache.ttl.default
}

# Usage
@cache.set("user:" + @id, @user, @cache_ttl("user"))
```

## Best Practices

1. **Use consistent key naming** - Establish naming conventions
2. **Set appropriate TTLs** - Balance freshness vs performance
3. **Handle cache misses gracefully** - Always have fallback logic
4. **Invalidate related caches** - Keep data consistent
5. **Monitor cache performance** - Track hit rates and memory usage
6. **Use cache tags** - Easier bulk invalidation

## Related Features

- `@remember()` - Cache helper function
- `@cache.tags()` - Tagged caching
- `@cache.lock()` - Cache-based locking
- `@redis` - Redis client
- `@memcached` - Memcached client