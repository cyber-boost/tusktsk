# @global - Global Variables

The `@global` operator provides access to globally scoped variables that persist across different parts of your application during a single request.

## Basic Syntax

```tusk
# Set global variable
@global.user_context: {id: 123, name: "John"}

# Access global variable
current_user: @global.user_context

# Check if exists
has_user: @isset(@global.user_context)

# Delete global variable
@global.temp_data: null
```

## Application State

```tusk
# Initialize application globals
@global.app: {
    name: @env.APP_NAME|"TuskApp"
    version: "2.0.0"
    started_at: @microtime(true)
    request_id: @generate_uuid()
}

# Configuration globals
@global.config: {
    timezone: @env.TZ|"UTC"
    locale: @env.LOCALE|"en_US"
    currency: @env.CURRENCY|"USD"
    date_format: @env.DATE_FORMAT|"Y-m-d"
}

# Runtime settings
@global.runtime: {
    debug: @bool(@env.DEBUG|"false")
    cache_enabled: @bool(@env.CACHE_ENABLED|"true")
    maintenance_mode: @bool(@env.MAINTENANCE|"false")
}
```

## Request Context

```tusk
# Set up request context
#middleware request_context {
    # Create request context
    @global.request_context: {
        id: @generate_request_id()
        started_at: @microtime(true)
        ip: @get_real_ip()
        user_agent: @request.headers.user-agent
        method: @request.method
        path: @request.path
    }
    
    # Add to response headers
    @response.headers.x-request-id: @global.request_context.id
    
    @next()
    
    # Log request completion
    @global.request_context.ended_at: @microtime(true)
    @global.request_context.duration: @global.request_context.ended_at - 
                                      @global.request_context.started_at
    
    @log.info("Request completed", @global.request_context)
}
```

## User Context

```tusk
# Authentication middleware
#middleware auth {
    token: @request.headers.authorization
    
    @if(@token) {
        user: @verify_token(@token)
        
        @if(@user) {
            # Set global user context
            @global.user: {
                id: @user.id
                name: @user.name
                email: @user.email
                roles: @user.roles
                permissions: @load_permissions(@user.id)
                timezone: @user.timezone|@global.config.timezone
            }
            
            # Set user-specific settings
            @global.user_settings: @load_user_settings(@user.id)
        }
    }
    
    @next()
}

# Access user anywhere
@if(@global.user) {
    welcome_message: "Hello, " + @global.user.name
}

# Permission check helper
can: (permission) => {
    @if(!@global.user) return false
    return @in_array(@permission, @global.user.permissions)
}
```

## Feature Flags

```tusk
# Load feature flags globally
@global.features: {
    new_ui: @bool(@env.FEATURE_NEW_UI|"false")
    beta_api: @bool(@env.FEATURE_BETA_API|"false")
    dark_mode: @bool(@env.FEATURE_DARK_MODE|"true")
    
    # User-specific features
    @if(@global.user) {
        user_features: @get_user_features(@global.user.id)
        @merge(@global.features, @user_features)
    }
}

# Feature check helper
feature_enabled: (feature) => {
    return @global.features[@feature]|false
}

# Use in templates
@if(@feature_enabled("new_ui")) {
    @render("templates/new_ui.tusk")
} else {
    @render("templates/classic_ui.tusk")
}
```

## Database Connections

```tusk
# Global database connections
@global.db: {
    # Primary connection
    primary: @create_connection({
        host: @env.DB_HOST
        database: @env.DB_NAME
        user: @env.DB_USER
        password: @env.DB_PASS
    })
    
    # Read replica
    read: @create_connection({
        host: @env.DB_READ_HOST|@env.DB_HOST
        database: @env.DB_NAME
        user: @env.DB_READ_USER|@env.DB_USER
        password: @env.DB_READ_PASS|@env.DB_PASS
    })
    
    # Analytics database
    analytics: @create_connection({
        host: @env.ANALYTICS_DB_HOST
        database: @env.ANALYTICS_DB_NAME
    })
}

# Query helpers using global connections
query: (sql, params: [], connection: "primary") => {
    return @global.db[@connection].query(@sql, @params)
}

read_query: (sql, params: []) => {
    return @query(@sql, @params, "read")
}
```

## Cache Instances

```tusk
# Global cache instances
@global.cache: {
    # Default cache
    default: @cache.store(@env.CACHE_DRIVER|"redis")
    
    # Session cache
    sessions: @cache.store("redis").namespace("sessions")
    
    # API cache
    api: @cache.store("redis").namespace("api").ttl(300)
    
    # View cache
    views: @cache.store("file").path("/tmp/cache/views")
}

# Helper to use specific cache
cache_get: (key, store: "default") => {
    return @global.cache[@store].get(@key)
}

cache_set: (key, value, ttl: 3600, store: "default") => {
    return @global.cache[@store].set(@key, @value, @ttl)
}
```

## Service Container

```tusk
# Global service container
@global.services: {}

# Register services
register_service: (name, factory) => {
    @global.services[@name]: @factory
}

# Register common services
@register_service("mailer", () => {
    return @create_mailer({
        driver: @env.MAIL_DRIVER
        host: @env.MAIL_HOST
        port: @env.MAIL_PORT
        username: @env.MAIL_USERNAME
        password: @env.MAIL_PASSWORD
    })
})

@register_service("logger", () => {
    return @create_logger({
        level: @env.LOG_LEVEL|"info"
        file: @env.LOG_FILE|"/var/log/app.log"
    })
})

# Get service
service: (name) => {
    @if(!@isset(@global.services[@name])) {
        @throw("Service not found: " + @name)
    }
    
    service: @global.services[@name]
    
    # Lazy instantiation
    @if(@is_callable(@service)) {
        @global.services[@name]: @service()
        return @global.services[@name]
    }
    
    return @service
}

# Use services
@service("mailer").send({
    to: "user@example.com"
    subject: "Welcome"
    body: "Welcome to our app!"
})
```

## Event System

```tusk
# Global event listeners
@global.events: {}

# Register event listener
on: (event, callback) => {
    @if(!@isset(@global.events[@event])) {
        @global.events[@event]: []
    }
    @global.events[@event][]: @callback
}

# Trigger event
emit: (event, data: {}) => {
    @if(@isset(@global.events[@event])) {
        @foreach(@global.events[@event] as @callback) {
            @callback(@data)
        }
    }
}

# Register listeners
@on("user.login", (data) => {
    @log.info("User logged in", {user_id: @data.user_id})
    @update_last_login(@data.user_id)
})

@on("order.placed", (data) => {
    @send_order_confirmation(@data.order_id)
    @update_inventory(@data.items)
    @track_analytics("order_placed", @data)
})

# Emit events
@emit("user.login", {user_id: @user.id, ip: @request.ip})
```

## Shared State

```tusk
# Share state between components
@global.shared: {
    # Shopping cart
    cart: @session.cart|[]
    
    # Current theme
    theme: @cookie.theme|@user.theme|"light"
    
    # Breadcrumbs
    breadcrumbs: []
    
    # Flash messages
    messages: []
}

# Breadcrumb management
add_breadcrumb: (title, url: null) => {
    @global.shared.breadcrumbs[]: {
        title: @title
        url: @url
    }
}

# Flash message management
flash: (type, message) => {
    @global.shared.messages[]: {
        type: @type
        message: @message
        timestamp: @time()
    }
}

# Use in templates
@foreach(@global.shared.breadcrumbs as @crumb) {
    @if(@crumb.url) {
        <a href="{@crumb.url}">{@crumb.title}</a> /
    } else {
        {@crumb.title}
    }
}
```

## Performance Monitoring

```tusk
# Global performance tracking
@global.performance: {
    timers: {}
    counters: {}
    memory_start: @memory_get_usage()
}

# Start timer
start_timer: (name) => {
    @global.performance.timers[@name]: @microtime(true)
}

# End timer
end_timer: (name) => {
    @if(@isset(@global.performance.timers[@name])) {
        duration: @microtime(true) - @global.performance.timers[@name]
        @global.performance.timers[@name]: @duration
        return @duration
    }
    return 0
}

# Increment counter
increment_counter: (name, value: 1) => {
    @if(!@isset(@global.performance.counters[@name])) {
        @global.performance.counters[@name]: 0
    }
    @global.performance.counters[@name]: @global.performance.counters[@name] + @value
}

# Get performance summary
get_performance_summary: () => {
    return {
        timers: @global.performance.timers
        counters: @global.performance.counters
        memory_used: @memory_get_usage() - @global.performance.memory_start
        memory_peak: @memory_get_peak_usage()
    }
}
```

## Cleanup

```tusk
# Register shutdown handler
@register_shutdown(() => {
    # Close database connections
    @foreach(@global.db as @name => @connection) {
        @connection.close()
    }
    
    # Flush cache writes
    @foreach(@global.cache as @name => @cache) {
        @cache.flush_pending()
    }
    
    # Log performance metrics
    @log.debug("Request performance", @get_performance_summary())
    
    # Clear sensitive data
    @global.user: null
    @global.services: {}
})
```

## Best Practices

1. **Use namespacing** - Organize globals by purpose
2. **Initialize early** - Set up globals in middleware
3. **Clean up after use** - Clear sensitive data
4. **Avoid overuse** - Prefer dependency injection
5. **Document globals** - Make their purpose clear
6. **Thread safety** - Be careful in async contexts

## Related Features

- `@session` - Session storage
- `@context` - Request context
- `@store` - Persistent storage
- `@registry` - Service registry
- `@container` - Dependency injection