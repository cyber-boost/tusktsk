# @metrics - Performance and Business Metrics

The `@metrics` operator provides comprehensive application monitoring, tracking both technical performance and business KPIs in real-time.

## Basic Syntax

```tusk
# Record a metric
@metrics.record("page_views", 1)

# Record with tags
@metrics.record("api_calls", 1, {
    endpoint: "/users"
    method: "GET"
    status: 200
})

# Timing metrics
@metrics.time("database_query", () => {
    return @query("SELECT * FROM users")
})

# Gauge (current value)
@metrics.gauge("active_users", @count_active_users())

# Histogram (distribution)
@metrics.histogram("response_time", @request_duration)
```

## Performance Tracking

```tusk
# Request timing middleware
#middleware performance_tracking {
    start_time: @microtime(true)
    start_memory: @memory_get_usage()
    
    # Process request
    @next()
    
    # Record metrics
    duration: @microtime(true) - @start_time
    memory_used: @memory_get_usage() - @start_memory
    
    @metrics.histogram("request_duration", @duration, {
        method: @request.method
        endpoint: @request.route
        status: @response.status
    })
    
    @metrics.histogram("memory_usage", @memory_used, {
        endpoint: @request.route
    })
    
    # Alert on slow requests
    @if(@duration > 1.0) {
        @metrics.increment("slow_requests", {
            endpoint: @request.route
        })
        @log.warning("Slow request", {
            duration: @duration
            endpoint: @request.route
        })
    }
}
```

## Business Metrics

```tusk
# E-commerce metrics
track_purchase: (order) => {
    # Revenue metric
    @metrics.increment("revenue", @order.total, {
        currency: @order.currency
        payment_method: @order.payment_method
        customer_type: @order.is_new_customer ? "new" : "returning"
    })
    
    # Order metrics
    @metrics.increment("orders", 1, {
        status: "completed"
        source: @order.source
    })
    
    # Average order value
    @metrics.gauge("average_order_value", 
        @calculate_average_order_value())
    
    # Items per order
    @metrics.histogram("items_per_order", 
        @count(@order.items))
}

# User engagement
track_user_action: (action, user_id) => {
    @metrics.increment("user_actions", 1, {
        action: @action
        user_segment: @get_user_segment(@user_id)
    })
    
    # Daily active users
    @metrics.set("daily_active_users", @user_id)
    
    # Session duration
    @if(@action == "logout") {
        duration: @time() - @session.start_time
        @metrics.histogram("session_duration", @duration)
    }
}
```

## Real-time Dashboards

```tusk
# Dashboard metrics endpoint
#api /metrics/dashboard {
    period: @request.get.period|"hour"
    
    metrics: {
        # Current values
        current: {
            active_users: @metrics.gauge_value("active_users")
            requests_per_minute: @metrics.rate("requests", "minute")
            error_rate: @metrics.error_rate()
            response_time_p95: @metrics.percentile("response_time", 95)
        }
        
        # Time series data
        timeseries: {
            requests: @metrics.timeseries("requests", @period, 24)
            errors: @metrics.timeseries("errors", @period, 24)
            response_times: @metrics.timeseries("response_time", @period, 24)
        }
        
        # Top endpoints
        top_endpoints: @metrics.top("requests", "endpoint", 10)
        slowest_endpoints: @metrics.top("response_time", "endpoint", 10)
        
        # Business metrics
        business: {
            revenue_today: @metrics.sum("revenue", "today")
            orders_today: @metrics.count("orders", "today")
            conversion_rate: @calculate_conversion_rate()
            cart_abandonment_rate: @calculate_abandonment_rate()
        }
    }
    
    @json(@metrics)
}
```

## Custom Metrics

```tusk
# Define custom metric types
Metrics: {
    # Counter - only goes up
    counter: (name, increment: 1, tags: {}) => {
        @metrics.increment(@name, @increment, @tags)
    }
    
    # Timer - tracks durations
    timer: (name, start_time, tags: {}) => {
        duration: @microtime(true) - @start_time
        @metrics.histogram(@name, @duration, @tags)
        return @duration
    }
    
    # Rate - tracks frequency
    rate: (name, tags: {}) => {
        @metrics.increment(@name + "_total", 1, @tags)
        @metrics.gauge(@name + "_rate", 
            @calculate_rate(@name), @tags)
    }
}

# Usage
@Metrics.counter("user_registrations", 1, {source: "web"})
@Metrics.timer("api_call_duration", @start_time, {endpoint: "/users"})
@Metrics.rate("uploads", {type: "image"})
```

## Error Tracking

```tusk
# Error metrics
track_error: (error, context: {}) => {
    @metrics.increment("errors", 1, {
        type: @error.type|"unknown"
        code: @error.code|500
        file: @error.file
        line: @error.line
        endpoint: @context.endpoint
    })
    
    # Error rate by endpoint
    @metrics.rate("error_rate", {
        endpoint: @context.endpoint
    })
    
    # Alert on error spike
    error_rate: @metrics.get_rate("errors", "minute")
    @if(@error_rate > 10) {
        @alert("High error rate detected", {
            rate: @error_rate
            endpoint: @context.endpoint
        })
    }
}

# Exception handler
#middleware error_tracking {
    @try {
        @next()
    } catch (error) {
        @track_error(@error, {
            endpoint: @request.route
            user_id: @session.user_id
        })
        @throw @error  # Re-throw
    }
}
```

## Database Metrics

```tusk
# Query performance tracking
db_metrics: {
    # Before query
    before_query: (sql) => {
        query_id: @generate_id()
        @metrics.queries[@query_id]: {
            sql: @sql
            start: @microtime(true)
        }
        return @query_id
    }
    
    # After query
    after_query: (query_id, result) => {
        query: @metrics.queries[@query_id]
        duration: @microtime(true) - @query.start
        
        @metrics.histogram("db_query_time", @duration, {
            table: @extract_table_name(@query.sql)
            operation: @extract_operation(@query.sql)
        })
        
        # Slow query logging
        @if(@duration > 0.1) {
            @metrics.increment("slow_queries")
            @log.warning("Slow query", {
                sql: @query.sql
                duration: @duration
            })
        }
        
        delete @metrics.queries[@query_id]
    }
}
```

## API Metrics

```tusk
# API usage tracking
#middleware api_metrics {
    # API key metrics
    api_key: @request.headers.x-api-key
    
    @if(@api_key) {
        @metrics.increment("api_usage", 1, {
            api_key: @hash_api_key(@api_key)
            endpoint: @request.route
            method: @request.method
        })
        
        # Rate limiting metrics
        rate_limit: @get_rate_limit(@api_key)
        usage: @get_api_usage(@api_key)
        
        @metrics.gauge("api_rate_limit_usage", 
            @usage / @rate_limit * 100, {
                api_key: @hash_api_key(@api_key)
            })
    }
    
    @next()
    
    # Response metrics
    @metrics.increment("api_responses", 1, {
        status: @response.status
        endpoint: @request.route
    })
}
```

## Cache Metrics

```tusk
# Cache performance tracking
cache_metrics: {
    on_hit: (key) => {
        @metrics.increment("cache_hits", 1, {
            cache: "redis"
            key_prefix: @get_key_prefix(@key)
        })
    }
    
    on_miss: (key) => {
        @metrics.increment("cache_misses", 1, {
            cache: "redis"
            key_prefix: @get_key_prefix(@key)
        })
    }
    
    on_set: (key, size) => {
        @metrics.histogram("cache_item_size", @size, {
            key_prefix: @get_key_prefix(@key)
        })
    }
    
    # Calculate hit rate
    hit_rate: () => {
        hits: @metrics.sum("cache_hits", "hour")
        misses: @metrics.sum("cache_misses", "hour")
        return @hits / (@hits + @misses) * 100
    }
}
```

## Alerting

```tusk
# Define alert rules
alert_rules: [
    {
        name: "High Error Rate"
        condition: () => @metrics.rate("errors", "minute") > 10
        action: (data) => @send_alert("error_rate", @data)
    },
    {
        name: "Low Conversion Rate"
        condition: () => @metrics.gauge_value("conversion_rate") < 1.0
        action: (data) => @send_alert("conversion_rate", @data)
    },
    {
        name: "High Response Time"
        condition: () => @metrics.percentile("response_time", 95) > 2.0
        action: (data) => @send_alert("response_time", @data)
    }
]

# Check alerts
#cron "* * * * *" {
    @foreach(@alert_rules as @rule) {
        @if(@rule.condition()) {
            @rule.action({
                rule: @rule.name
                timestamp: @timestamp
                value: @rule.condition()
            })
        }
    }
}
```

## Exporting Metrics

```tusk
# Prometheus format export
#api /metrics/export {
    format: @request.get.format|"prometheus"
    
    @if(@format == "prometheus") {
        output: @metrics.export_prometheus()
        @response.headers.content-type: "text/plain"
        @output(@output)
    } elseif(@format == "json") {
        @json(@metrics.export_json())
    } else {
        @response.status: 400
        error: "Unsupported format"
    }
}

# StatsD integration
statsd_export: {
    host: @env.STATSD_HOST|"localhost"
    port: @env.STATSD_PORT|8125
    
    @foreach(@metrics.get_all() as @metric) {
        @statsd.send(@metric.name, @metric.value, @metric.type)
    }
}
```

## Performance Optimization

```tusk
# Batch metrics recording
metrics_buffer: []

# Add to buffer instead of immediate send
buffer_metric: (type, name, value, tags) => {
    @metrics_buffer[]: {
        type: @type
        name: @name
        value: @value
        tags: @tags
        timestamp: @time()
    }
    
    # Flush if buffer is full
    @if(@count(@metrics_buffer) >= 100) {
        @flush_metrics()
    }
}

# Flush metrics periodically
#cron "* * * * *" {
    @flush_metrics()
}

flush_metrics: () => {
    @if(@count(@metrics_buffer) > 0) {
        @metrics.batch(@metrics_buffer)
        @metrics_buffer: []
    }
}
```

## Best Practices

1. **Use consistent naming** - Follow naming conventions for metrics
2. **Add relevant tags** - Enable filtering and grouping
3. **Avoid high cardinality** - Don't use unique IDs as tags
4. **Set up alerts** - Monitor critical metrics
5. **Regular cleanup** - Archive old metrics data
6. **Performance impact** - Minimize metrics overhead

## Related Features

- `@monitor` - Application monitoring
- `@log` - Logging system
- `@alert` - Alert management
- `@analytics` - User analytics
- `@performance` - Performance profiling