# Debugging Tools in TuskLang

TuskLang provides a comprehensive suite of debugging tools to help you identify and fix issues in your code quickly and efficiently.

## Built-in Debugger

```tusk
# Set breakpoints in code
function processUser(user) {
    @debugger()  # Execution stops here
    
    # Step through code
    validated: @validateUser(user)
    
    @debugger.if(user.role === "admin")  # Conditional breakpoint
    
    # Continue execution
    return @saveUser(validated)
}

# Debug with context
@debug({
    user: user,
    validated: validated,
    stack: @debug_backtrace()
})

# Interactive debugging
@debug.interactive()  # Opens REPL at this point

# Debug only in development
if (@env.debug) {
    @debug.dump(complexObject)
    @debug.trace("Processing user: " + user.id)
}
```

## Logging and Output

```tusk
# Console logging with levels
@console.log("Basic message")
@console.info("Information", {context: data})
@console.warn("Warning message")
@console.error("Error occurred", error)
@console.debug("Debug info", {verbose: true})

# Formatted output
@console.table([
    {name: "John", age: 30, role: "admin"},
    {name: "Jane", age: 25, role: "user"}
])

@console.group("User Processing")
@console.log("Starting...")
@console.log("Validating...")
@console.log("Saving...")
@console.groupEnd()

# Timing operations
@console.time("operation")
// ... expensive operation
@console.timeEnd("operation")  # Outputs: operation: 123.45ms

# Pretty printing
@dd(complexObject)  # Dump and die
@dump(complexObject)  # Dump and continue

# Structured logging
logger: @logger("app.users")

logger.info("User created", {
    user_id: user.id,
    email: user.email,
    ip: @request.ip
})

logger.error("Failed to save user", {
    error: exception.message,
    trace: exception.trace,
    user_data: user
})
```

## Stack Traces and Error Information

```tusk
# Get current stack trace
stack: @debug_backtrace()

for (frame in stack) {
    @console.log("File:", frame.file)
    @console.log("Line:", frame.line)
    @console.log("Function:", frame.function)
    @console.log("Class:", frame.class)
    @console.log("Args:", frame.args)
}

# Enhanced error information
try {
    @risky_operation()
} catch (Exception e) {
    # Get detailed error info
    error_info: @debug.exception(e)
    
    @console.error("Exception Details:", {
        message: error_info.message,
        file: error_info.file,
        line: error_info.line,
        trace: error_info.trace,
        previous: error_info.previous,
        context: error_info.context  # Code around error
    })
    
    # Log to error tracking service
    @sentry.captureException(e, {
        user: @auth.user,
        tags: {
            module: "payment",
            severity: "high"
        }
    })
}

# Custom error handler
@error.handler((exception, request) => {
    # Development: Show detailed error page
    if (@env.debug) {
        return @render("errors.debug", {
            exception: exception,
            request: request,
            stack: @format_stack_trace(exception.trace),
            source: @get_source_context(exception.file, exception.line)
        })
    }
    
    # Production: Log and show generic error
    @log.error("Unhandled exception", {
        exception: exception,
        url: request.url,
        user: @auth.id
    })
    
    return @render("errors.500")
})
```

## Variable Inspection

```tusk
# Inspect variable details
user: @User.find(1)

# Get variable info
info: @debug.inspect(user)
@console.log("Type:", info.type)
@console.log("Class:", info.class)
@console.log("Properties:", info.properties)
@console.log("Methods:", info.methods)
@console.log("Memory:", info.memory_usage)

# Watch variables for changes
@debug.watch("user.status", (old_value, new_value) => {
    @console.log("Status changed from", old_value, "to", new_value)
    @debug.trace()
})

# Deep object inspection
@debug.dump(user, {
    depth: 5,  # Max nesting depth
    show_hidden: true,  # Include private properties
    show_methods: true,  # Include method signatures
    circular: "[Circular]"  # Handle circular references
})

# Memory profiling
memory_before: @memory_get_usage()
// ... operation
memory_after: @memory_get_usage()

@console.log("Memory used:", memory_after - memory_before)
@console.log("Peak memory:", @memory_get_peak_usage())
```

## Query Debugging

```tusk
# Enable query logging
@db.enableQueryLog()

// ... run queries
users: @User.where("active", true)->get()

# Get executed queries
queries: @db.getQueryLog()

for (query in queries) {
    @console.log("SQL:", query.sql)
    @console.log("Bindings:", query.bindings)
    @console.log("Time:", query.time + "ms")
}

# Query debugging helper
@db.listen((query) => {
    # Log slow queries
    if (query.time > 100) {
        @logger.warning("Slow query detected", {
            sql: query.sql,
            time: query.time,
            backtrace: @debug_backtrace()
        })
    }
    
    # Debug specific tables
    if (query.sql.includes("users")) {
        @console.debug("User query:", query)
    }
})

# Explain queries
query: @User.where("age", ">", 18)->orderBy("created_at")

# Get query plan
explanation: query.explain()
@console.table(explanation)

# Get raw SQL
@console.log("SQL:", query.toSql())
@console.log("Bindings:", query.getBindings())
```

## HTTP Request/Response Debugging

```tusk
# Debug incoming requests
@middleware.debug_request((request) => {
    @console.group("Incoming Request")
    @console.log("Method:", request.method)
    @console.log("URL:", request.url)
    @console.log("Headers:", request.headers)
    @console.log("Query:", request.query)
    @console.log("Body:", request.post)
    @console.log("Files:", request.files)
    @console.groupEnd()
})

# Debug outgoing responses
@middleware.debug_response((response) => {
    @console.group("Outgoing Response")
    @console.log("Status:", response.status)
    @console.log("Headers:", response.headers)
    @console.log("Body preview:", @str_limit(response.body, 500))
    @console.groupEnd()
})

# HTTP client debugging
client: @http.debug()  # Enable debug mode

response: await client.post("https://api.example.com/users", {
    name: "Test User"
})

# Outputs full request/response details
```

## Performance Profiling

```tusk
# Profile code sections
@profile.start("user-processing")

// ... code to profile
users: @User.all()
for (user in users) {
    @process_user(user)
}

@profile.end("user-processing")

# Get profile results
results: @profile.get("user-processing")
@console.log("Total time:", results.duration)
@console.log("Memory used:", results.memory)
@console.log("Calls:", results.calls)

# Automatic profiling
@profile.auto("UserService::processAll", () => {
    return @UserService.processAll()
})

# Profile with annotations
class UserService {
    @profile
    processUser(user) {
        // Method automatically profiled
    }
    
    @profile("batch-process")
    processBatch(users) {
        // Custom profile name
    }
}

# Flame graph generation
@profile.start_trace()
// ... application code
flame_data: @profile.end_trace()
@file.write("flame.json", flame_data)
```

## Memory Debugging

```tusk
# Track memory leaks
memory_tracker: @debug.memory_tracker()

# Mark checkpoint
memory_tracker.checkpoint("before_operation")

// ... potentially leaky code
for (i in 1..1000) {
    data: @fetch_large_data(i)
    @process_data(data)
}

# Check for leaks
leaks: memory_tracker.check_leaks("before_operation")
if (leaks.found) {
    @console.error("Memory leak detected!", {
        leaked: leaks.bytes,
        objects: leaks.objects,
        types: leaks.types
    })
}

# Object reference tracking
@debug.track_references(suspicious_object)

// ... later
refs: @debug.get_references(suspicious_object)
@console.log("Object referenced by:", refs)

# Garbage collection debugging
@gc.collect()  # Force garbage collection
stats: @gc.stats()
@console.log("GC stats:", stats)
```

## Remote Debugging

```tusk
# Enable remote debugging
@debug.remote.enable({
    host: "0.0.0.0",
    port: 9229,
    wait: false  # Wait for debugger to attach
})

# Debugging webhooks
@debug.webhook("https://debug.example.com/hook", {
    events: ["error", "warning", "slow_query"],
    include: {
        stack_trace: true,
        variables: true,
        system_info: true
    }
})

# Remote REPL
@debug.remote_repl.start({
    port: 9230,
    auth: @env("DEBUG_TOKEN")
})

# Debug API endpoint
#api /debug if: @env.debug {
    return {
        routes: @router.getRoutes(),
        config: @config.all(),
        services: @container.bindings(),
        queries: @db.getQueryLog(),
        cache_stats: @cache.stats(),
        queue_status: @queue.status()
    }
}
```

## Testing and Debug Helpers

```tusk
# Debug test failures
#test "Complex operation" {
    @debug.test.capture()  # Capture all debug info
    
    try {
        result: @complex_operation()
        @assert.equals(result, expected)
    } catch (AssertionError e) {
        # Output captured debug info on failure
        @debug.test.dump()
        throw e
    }
}

# Snapshot debugging
@debug.snapshot("before_refactor", {
    users: @User.all(),
    settings: @config.all(),
    cache: @cache.all()
})

// ... refactored code

@debug.compare_snapshot("before_refactor", {
    users: @User.all(),
    settings: @config.all(),
    cache: @cache.all()
})

# Time travel debugging
recorder: @debug.recorder()
recorder.start()

// ... application flow

# Replay execution
recorder.replay({
    speed: 0.5,  # Half speed
    breakpoints: ["UserService.save"],
    watch: ["user.status"]
})
```

## Debug Configuration

```tusk
# debug.config.tsk
{
    # Debug settings
    debug: {
        enabled: @env("APP_DEBUG", false),
        
        # Error display
        display_errors: true,
        error_detail: "high",  # low, medium, high
        
        # Logging
        log_level: "debug",
        log_queries: true,
        log_requests: true,
        
        # Profiling
        profile: {
            enabled: true,
            sample_rate: 0.1,  # 10% of requests
            slow_threshold: 1000  # ms
        },
        
        # Remote debugging
        remote: {
            enabled: false,
            allowed_ips: ["127.0.0.1"],
            port: 9229
        },
        
        # Debug bar
        debug_bar: {
            enabled: true,
            collectors: [
                "queries",
                "requests",
                "exceptions",
                "timeline",
                "memory",
                "cache"
            ]
        }
    }
}
```

## Debug Toolbar

```tusk
# Enable debug toolbar
@app.use(DebugToolbar, {
    enabled: @env.debug,
    
    # Collectors
    collectors: [
        QueryCollector,
        TimelineCollector,
        MemoryCollector,
        RequestCollector,
        CacheCollector,
        EventCollector
    ],
    
    # Storage
    storage: FileStorage,
    storage_path: "storage/debugbar",
    
    # JavaScript
    include_vendors: true,
    
    # Authorization
    authorize: () => {
        return @env.debug || @auth.user?.isAdmin()
    }
})

# Custom collector
class CustomCollector extends Collector {
    name: "custom"
    
    collect() {
        return {
            data: @my_debug_data(),
            count: @my_metric_count()
        }
    }
    
    getWidgets() {
        return {
            "custom": {
                icon: "bug",
                widget: "PhpDebugBar.Widgets.VariableListWidget",
                map: "custom.data",
                default: "{}"
            },
            "custom:badge": {
                map: "custom.count",
                default: 0
            }
        }
    }
}
```

## Best Practices

1. **Remove debug code in production** - Use environment checks
2. **Use appropriate log levels** - Don't log everything as errors
3. **Structure debug output** - Use groups and tables
4. **Include context** - Add relevant data to debug messages
5. **Use conditional debugging** - Debug specific scenarios
6. **Monitor performance** - Debugging can impact performance
7. **Secure debug endpoints** - Don't expose sensitive data
8. **Clean up debug files** - Remove logs and dumps regularly

## Related Topics

- `logging` - Logging strategies
- `error-handling` - Error management
- `profiling` - Performance profiling
- `monitoring` - Application monitoring
- `testing` - Testing and debugging