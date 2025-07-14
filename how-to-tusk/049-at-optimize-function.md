# @optimize() - Performance Optimization

The `@optimize()` function provides intelligent performance optimization, automatically improving code execution, database queries, and resource utilization.

## Basic Syntax

```tusk
# Optimize a function
optimized_fn: @optimize(@my_function)

# Optimize with hints
optimized: @optimize(@slow_function, {
    cache: true
    memoize: true
    parallel: true
})

# Query optimization
optimized_query: @optimize.query(@sql_query)

# Auto-optimize block
@optimize.block {
    # Code here is automatically optimized
    results: @expensive_operation()
}
```

## Function Optimization

```tusk
# Automatic memoization
fibonacci: @optimize.memoize((n) => {
    @if(@n <= 1) return @n
    return @fibonacci(@n - 1) + @fibonacci(@n - 2)
})

# Parallel execution
process_items: @optimize.parallel((items) => {
    return @items.map(item => {
        # Heavy processing per item
        result: @complex_calculation(@item)
        return @result
    })
}, {
    workers: 4
    chunk_size: 100
})

# JIT compilation
hot_function: @optimize.jit((data) => {
    # Frequently called function
    # TuskLang JIT compiles after threshold
    total: 0
    @foreach(@data as @item) {
        total: @total + @item.value * @item.weight
    }
    return @total
})
```

## Query Optimization

```tusk
# Automatic query optimization
get_user_orders: (user_id) => {
    # Original query
    query: "
        SELECT o.*, p.name as product_name, p.price
        FROM orders o
        JOIN products p ON o.product_id = p.id
        WHERE o.user_id = ?
        ORDER BY o.created_at DESC
    "
    
    # Optimize query
    optimized: @optimize.query(@query, {
        analyze: true
        add_indexes: true
        cache_plan: true
    })
    
    return @query(@optimized, [@user_id])
}

# Index recommendations
#api /admin/optimize-database {
    recommendations: @optimize.analyze_database({
        tables: ["orders", "products", "users"]
        workload: @get_recent_queries()
    })
    
    @json({
        missing_indexes: @recommendations.indexes
        slow_queries: @recommendations.slow_queries
        optimization_suggestions: @recommendations.suggestions
        estimated_improvement: @recommendations.impact_percent + "%"
    })
}
```

## Caching Optimization

```tusk
# Intelligent caching
get_product_details: @optimize.cache((product_id) => {
    product: @query("SELECT * FROM products WHERE id = ?", [@product_id])
    reviews: @query("SELECT * FROM reviews WHERE product_id = ?", [@product_id])
    related: @get_related_products(@product_id)
    
    return {
        product: @product
        reviews: @reviews
        related: @related
        computed_score: @calculate_product_score(@product, @reviews)
    }
}, {
    key_pattern: "product:{id}"
    ttl: "adaptive"  # TuskLang adjusts based on access patterns
    invalidate_on: ["product_update", "review_added"]
})

# Cache warming
@optimize.warm_cache({
    patterns: [
        "product:*",
        "category:*",
        "user:preferences:*"
    ],
    strategy: "most_accessed"
    schedule: "0 */4 * * *"  # Every 4 hours
})
```

## Memory Optimization

```tusk
# Memory-efficient data processing
process_large_file: @optimize.memory((file_path) => {
    # Automatically chunks large operations
    results: []
    
    @optimize.stream(@file_path, (chunk) => {
        # Process chunk without loading entire file
        processed: @process_chunk(@chunk)
        @results.push(@processed)
        
        # TuskLang manages memory automatically
    })
    
    return @aggregate_results(@results)
})

# Object pooling
connection_pool: @optimize.pool({
    create: () => @create_database_connection()
    destroy: (conn) => @conn.close()
    validate: (conn) => @conn.is_alive()
    size: 10
    max: 50
})

# Use pooled connection
@connection_pool.use((conn) => {
    return @conn.query("SELECT * FROM users")
})
```

## Algorithm Optimization

```tusk
# Automatic algorithm selection
sort_data: @optimize.algorithm((data) => {
    # TuskLang chooses best sorting algorithm
    # based on data size and characteristics
    return @sort(@data)
}, {
    algorithms: ["quicksort", "mergesort", "radixsort"]
    benchmark: true
})

# Dynamic programming optimization
knapsack: @optimize.dynamic((items, capacity) => {
    # Automatically applies DP optimization
    @if(@capacity == 0 || @items.length == 0) {
        return 0
    }
    
    item: @items[0]
    remaining: @items.slice(1)
    
    @if(@item.weight > @capacity) {
        return @knapsack(@remaining, @capacity)
    }
    
    with_item: @item.value + @knapsack(@remaining, @capacity - @item.weight)
    without_item: @knapsack(@remaining, @capacity)
    
    return @max(@with_item, @without_item)
})
```

## Request Optimization

```tusk
# Batch API requests
batch_fetcher: @optimize.batch((ids) => {
    # Instead of N individual requests
    # TuskLang batches them automatically
    return @fetch_multiple(@ids)
}, {
    max_batch_size: 100
    delay: 10  # ms to wait for more requests
})

# Usage - these get batched
user1: @batch_fetcher(123)
user2: @batch_fetcher(456)
user3: @batch_fetcher(789)

# Request deduplication
get_user: @optimize.dedupe((user_id) => {
    # Multiple simultaneous requests for same ID
    # only result in one actual fetch
    return @query("SELECT * FROM users WHERE id = ?", [@user_id])
})
```

## Lazy Loading

```tusk
# Lazy evaluation
Report: @optimize.lazy({
    # These are only computed when accessed
    summary: () => @calculate_summary()
    details: () => @fetch_detailed_data()
    charts: () => @generate_charts()
    
    # Always computed
    title: "Sales Report"
    date: @today()
})

# Only summary is calculated
@if(@Report.summary.total > 1000) {
    # Now details are fetched
    @send_email(@Report.details)
}
```

## Compilation Optimization

```tusk
# Pre-compile templates
templates: @optimize.precompile({
    "email/welcome": @load_template("email/welcome.tusk")
    "email/reset": @load_template("email/reset.tusk")
    "pages/home": @load_template("pages/home.tusk")
})

# Pre-compile regex patterns
patterns: @optimize.regex({
    email: /^[^\s@]+@[^\s@]+\.[^\s@]+$/
    phone: /^\+?[\d\s-()]+$/
    url: /^https?:\/\/.+$/
})

# Use pre-compiled
is_valid_email: @patterns.email.test(@email)
```

## Network Optimization

```tusk
# HTTP/2 multiplexing
api_client: @optimize.http({
    base_url: "https://api.example.com"
    connection_pooling: true
    http2: true
    compression: "gzip"
    keepalive: true
})

# Automatic retry with backoff
fetch_with_retry: @optimize.retry((url) => {
    return @http.get(@url)
}, {
    max_attempts: 3
    backoff: "exponential"
    retry_on: [408, 429, 500, 502, 503, 504]
})

# Circuit breaker pattern
external_api: @optimize.circuit_breaker((endpoint) => {
    return @fetch(@endpoint)
}, {
    failure_threshold: 5
    reset_timeout: 60000  # 1 minute
    half_open_requests: 3
})
```

## Database Connection Optimization

```tusk
# Connection pooling with optimization
db: @optimize.database({
    host: @env.DB_HOST
    pool: {
        min: 2
        max: 10
        acquire_timeout: 30000
        idle_timeout: 10000
    }
    query_timeout: 5000
    statement_cache_size: 100
})

# Prepared statement caching
get_user_by_email: @optimize.prepare("
    SELECT * FROM users WHERE email = ?
")

# Use prepared statement (faster)
user: @get_user_by_email(@email)
```

## Profiling and Analysis

```tusk
# Profile code block
@optimize.profile("expensive_operation") {
    data: @load_large_dataset()
    processed: @process_data(@data)
    results: @analyze_results(@processed)
}

# Get profiling results
profile: @optimize.get_profile("expensive_operation")
/*
{
    total_time: 1.234
    memory_peak: 134217728
    breakdown: {
        load_large_dataset: 0.456
        process_data: 0.678  
        analyze_results: 0.100
    }
    suggestions: [
        "Consider caching load_large_dataset results",
        "process_data could benefit from parallelization"
    ]
}
*/

# Continuous profiling
@optimize.watch({
    functions: ["api_handler", "database_query", "render_template"]
    threshold: 100  # ms
    action: (profile) => {
        @if(@profile.duration > @threshold) {
            @log.warning("Slow operation detected", @profile)
            @metrics.record("slow_operation", @profile.duration, {
                function: @profile.function
            })
        }
    }
})
```

## Auto-Scaling

```tusk
# Dynamic resource allocation
worker_pool: @optimize.auto_scale({
    min_workers: 2
    max_workers: 20
    scale_up_threshold: 0.8  # 80% CPU
    scale_down_threshold: 0.2  # 20% CPU
    
    task: (job) => {
        # Process job
        return @process_job(@job)
    }
})

# Submit work - pool scales automatically
@foreach(@jobs as @job) {
    @worker_pool.submit(@job)
}
```

## Best Practices

1. **Measure first** - Profile before optimizing
2. **Set budgets** - Define performance targets
3. **Cache wisely** - Not everything benefits from caching
4. **Monitor impact** - Track optimization effectiveness
5. **Iterate** - Optimization is an ongoing process
6. **Document changes** - Note what was optimized and why

## Related Features

- `@profile()` - Performance profiling
- `@cache` - Caching system
- `@parallel()` - Parallel execution
- `@benchmark()` - Performance testing
- `@metrics` - Performance monitoring