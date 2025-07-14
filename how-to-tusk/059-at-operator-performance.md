# @ Operator Performance

Understanding the performance characteristics of @ operators is crucial for building efficient TuskLang applications. This guide covers optimization techniques and best practices.

## Performance Basics

```tusk
# Operator overhead comparison
@benchmark({
    # Direct property access (fastest)
    "direct": () => {
        value: object.property
    }
    
    # @ operator access (minimal overhead)
    "operator": () => {
        value: @object.property
    }
    
    # Dynamic access (slower)
    "dynamic": () => {
        value: @get(@object, "property")
    }
})
```

## Caching Operator Results

```tusk
# Cache expensive operations
expensive_calculation: @cache.remember("calc:" + @id, 3600, () => {
    return @perform_expensive_calculation(@id)
})

# Memoization for pure functions
@memoize
fibonacci: (n) => {
    @if(@n <= 1) return @n
    return @fibonacci(@n - 1) + @fibonacci(@n - 2)
}

# Manual memoization
memoized_function: (() => {
    cache: {}
    return (input) => {
        key: @json_encode(@input)
        @if(@isset(@cache[@key])) {
            return @cache[@key]
        }
        result: @expensive_operation(@input)
        @cache[@key]: @result
        return @result
    }
})()
```

## Lazy Evaluation

```tusk
# Lazy loading with operators
LazyCollection: {
    items: null
    
    # Only load when accessed
    @lazy
    get_items: () => {
        @if(@this.items === null) {
            @this.items: @load_items_from_database()
        }
        return @this.items
    }
    
    # Lazy computed properties
    @lazy
    total: () => {
        return @sum(@this.get_items().map(i => @i.value))
    }
}

# Usage - items only loaded when needed
collection: @LazyCollection.new()
@if(@some_condition) {
    total: @collection.total  # Now items are loaded
}
```

## Operator Batching

```tusk
# Batch multiple operations
BatchProcessor: {
    queue: []
    
    add: (item) => {
        @this.queue[]: @item
        
        # Process when batch is full
        @if(@count(@this.queue) >= 100) {
            @this.process()
        }
    }
    
    process: () => {
        @if(@empty(@this.queue)) return
        
        # Process entire batch at once
        @bulk_insert(@this.queue)
        @this.queue: []
    }
}

# Batch database operations
@foreach(@items as @item) {
    @BatchProcessor.add(@item)  # Batched inserts
}
@BatchProcessor.process()  # Process remaining
```

## Query Optimization

```tusk
# N+1 query problem (bad)
users_bad: @User.all()
@foreach(@users_bad as @user) {
    posts: @Post.where("user_id", @user.id).get()  # N queries
}

# Eager loading (good)
users_good: @User.with("posts").get()  # 2 queries total

# Query result caching
get_user_with_cache: (id) => {
    # Check memory cache first
    @if(@runtime_cache[@id]) {
        return @runtime_cache[@id]
    }
    
    # Then check Redis
    cached: @redis.get("user:" + @id)
    @if(@cached) {
        @runtime_cache[@id]: @json_decode(@cached)
        return @runtime_cache[@id]
    }
    
    # Finally hit database
    user: @User.find(@id)
    @redis.setex("user:" + @id, 3600, @json_encode(@user))
    @runtime_cache[@id]: @user
    return @user
}
```

## String Operation Performance

```tusk
# String concatenation performance
# Bad - multiple concatenations
result_bad: ""
@foreach(@items as @item) {
    result_bad: @result_bad + @item + ", "  # Creates new string each time
}

# Good - use array join
result_good: @join(", ", @items)

# Better - use string builder
builder: @StringBuilder.new()
@foreach(@items as @item) {
    @builder.append(@item).append(", ")
}
result_better: @builder.toString()

# String operations benchmark
@benchmark({
    "concatenation": () => {
        result: ""
        @for(i: 0; @i < 1000; @i++) {
            result: @result + "x"
        }
    }
    
    "array_join": () => {
        parts: []
        @for(i: 0; @i < 1000; @i++) {
            parts[]: "x"
        }
        result: @join("", @parts)
    }
    
    "string_builder": () => {
        builder: @StringBuilder.new()
        @for(i: 0; @i < 1000; @i++) {
            @builder.append("x")
        }
        result: @builder.toString()
    }
})
```

## Array Operation Performance

```tusk
# Array operation optimization
# Bad - multiple passes
result_bad: @array
    .filter(x => @x.active)
    .map(x => @x.value)
    .filter(x => @x > 100)
    .map(x => @x * 1.1)

# Good - single pass
result_good: []
@foreach(@array as @item) {
    @if(@item.active && @item.value > 100) {
        @result_good[]: @item.value * 1.1
    }
}

# Use generators for large datasets
process_large_file: (file) => {
    @foreach(@file.lines() as @line) {  # Generator, doesn't load all lines
        @if(@matches_criteria(@line)) {
            @yield @process_line(@line)
        }
    }
}
```

## Memory Management

```tusk
# Memory-efficient operations
# Bad - loads entire dataset
all_users: @User.all()  # Loads all users into memory
filtered: @all_users.filter(u => @u.active)

# Good - filter at database level
filtered: @User.where("active", true).get()

# Better - use chunks for large datasets
@User.where("active", true).chunk(100, (users) => {
    @foreach(@users as @user) {
        @process_user(@user)
    }
    # Memory is freed after each chunk
})

# Monitor memory usage
memory_before: @memory_get_usage()
@perform_operation()
memory_after: @memory_get_usage()
memory_used: @memory_after - @memory_before

@if(@memory_used > 10485760) {  # 10MB
    @log.warning("High memory usage", {
        used: @format_bytes(@memory_used)
        peak: @format_bytes(@memory_get_peak_usage())
    })
}
```

## Parallel Processing

```tusk
# Parallel operator execution
results: @parallel.map(@items, (item) => {
    return @expensive_operation(@item)
}, {
    workers: 4
    chunk_size: 50
})

# Async parallel processing
async_results: await @Promise.all(
    @items.map(async (item) => {
        return await @async_operation(@item)
    })
)

# Worker pool for CPU-intensive tasks
worker_pool: @WorkerPool.create({
    workers: @cpu_count()
    task: (data) => @heavy_computation(@data)
})

results: await @worker_pool.process(@large_dataset)
```

## JIT Compilation

```tusk
# Mark hot functions for JIT
@jit
hot_function: (data) => {
    # This function will be JIT compiled after threshold
    result: 0
    @foreach(@data as @item) {
        result: @result + @item.value * @item.weight
    }
    return @result
}

# JIT configuration
@jit.config({
    threshold: 1000      # Compile after 1000 calls
    optimize_level: 3    # Maximum optimization
    profile: true        # Profile for better optimization
})

# Check JIT status
status: @jit.status("hot_function")
/* Output:
{
    compiled: true
    calls: 5234
    avg_time_before: 0.0045
    avg_time_after: 0.0008
    speedup: 5.625
}
*/
```

## Profiling Operators

```tusk
# Profile operator performance
@profile.start("operation_name")

# Your code here
result: @complex_operation()

profile: @profile.end("operation_name")
/* Output:
{
    duration: 0.234
    memory_used: 2048576
    memory_peak: 3145728
    operators_called: {
        "@query": 15,
        "@map": 234,
        "@filter": 89
    }
}
*/

# Automatic profiling
@autoprofile(threshold: 0.01)  # Profile operations over 10ms
process_data: (data) => {
    return @transform(@validate(@clean(@data)))
}

# Get profiling report
report: @profile.report()
```

## Optimization Patterns

```tusk
# Early return pattern
process_item: (item) => {
    # Check conditions early
    @if(!@item) return null
    @if(!@item.active) return null
    @if(@item.processed) return @item.result
    
    # Expensive operations only if needed
    return @expensive_process(@item)
}

# Precompute pattern
PrecomputedData: {
    _lookup_table: null
    
    get_lookup: () => {
        @if(!@this._lookup_table) {
            # Build lookup table once
            @this._lookup_table: {}
            data: @load_all_data()
            @foreach(@data as @item) {
                @this._lookup_table[@item.key]: @item
            }
        }
        return @this._lookup_table
    }
    
    find: (key) => {
        lookup: @this.get_lookup()
        return @lookup[@key]  # O(1) lookup
    }
}

# Circuit breaker pattern
CircuitBreaker: {
    failures: 0
    last_failure: null
    threshold: 5
    timeout: 60  # seconds
    
    call: (operation) => {
        # Check if circuit is open
        @if(@this.failures >= @this.threshold) {
            @if(@time() - @this.last_failure < @this.timeout) {
                @throw("Circuit breaker open")
            }
            # Reset after timeout
            @this.failures: 0
        }
        
        @try {
            result: @operation()
            @this.failures: 0  # Reset on success
            return @result
        } catch (e) {
            @this.failures++
            @this.last_failure: @time()
            @throw @e
        }
    }
}
```

## Performance Monitoring

```tusk
# Monitor operator performance
@monitor.operators({
    slow_threshold: 0.1  # 100ms
    memory_threshold: 10485760  # 10MB
    
    on_slow: (operator, duration) => {
        @log.warning("Slow operator", {
            operator: @operator
            duration: @duration
            stack: @get_stack_trace()
        })
    }
    
    on_memory: (operator, memory) => {
        @log.warning("High memory usage", {
            operator: @operator
            memory: @format_bytes(@memory)
        })
    }
})

# Performance budgets
@performance.budget({
    page_load: 1.0      # 1 second
    api_response: 0.2   # 200ms
    query_time: 0.05    # 50ms
})

# Check budget
@performance.check("api_response", () => {
    return @handle_api_request()
})
```

## Best Practices

1. **Profile first** - Don't optimize without measuring
2. **Cache expensive operations** - Use memoization and caching
3. **Batch operations** - Reduce overhead with batching
4. **Use appropriate data structures** - Choose the right tool
5. **Lazy load when possible** - Don't compute until needed
6. **Monitor production performance** - Real-world data matters
7. **Set performance budgets** - Define acceptable thresholds

## Related Features

- `@benchmark()` - Performance testing
- `@profile()` - Code profiling
- `@cache` - Result caching
- `@optimize()` - Automatic optimization
- `@monitor` - Performance monitoring