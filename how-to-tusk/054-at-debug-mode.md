# @debug - Debug Mode and Tools

The `@debug` operator provides comprehensive debugging capabilities, helping developers identify issues, trace execution, and optimize performance.

## Basic Syntax

```tusk
# Check if debug mode is enabled
@if(@debug) {
    @log("Debug mode active")
}

# Debug output
@debug.log("Variable value", @my_var)

# Dump and die
@debug.dd(@complex_object)

# Conditional debugging
@debug.when(@condition, "This only logs when condition is true")
```

## Debug Configuration

```tusk
# Enable/disable debug mode
@debug.enable()
@debug.disable()

# Configure debug settings
@debug.config({
    # Output settings
    output: "console"      # console, file, browser
    format: "pretty"       # pretty, json, raw
    
    # What to include
    show_types: true
    show_memory: true
    show_time: true
    show_backtrace: false
    max_depth: 10
    
    # Performance
    profile: true
    slow_query_threshold: 0.1
    
    # Error handling
    display_errors: true
    error_level: E_ALL
})
```

## Variable Inspection

```tusk
# Debug dump
@debug.dump(@variable)
/* Output:
string(5) "hello"
Memory: 1.2MB
Time: 0.0023s
*/

# Dump with label
@debug.dump(@user, "Current User")
/* Output:
=== Current User ===
object(User) {
    id: 123
    name: "John Doe"
    email: "john@example.com"
}
*/

# Dump and die
@debug.dd(@data)  # Stops execution

# Dump multiple variables
@debug.dump_all({
    user: @user
    request: @request
    session: @session
})

# Pretty print
@debug.pretty(@complex_array)
```

## Execution Tracing

```tusk
# Trace function calls
@debug.trace("Starting user authentication")

# Trace with data
@debug.trace("Processing order", {
    order_id: @order.id
    items: @count(@order.items)
    total: @order.total
})

# Function execution trace
calculate_total: @debug.trace_function((items) => {
    total: 0
    @foreach(@items as @item) {
        total: @total + @item.price * @item.quantity
    }
    return @total
})

# Backtrace
@debug.backtrace()
/* Output:
#0 /app/controllers/UserController.tusk:45 process_request()
#1 /app/middleware/Auth.tusk:23 authenticate()
#2 /app/routes.tusk:12 handle()
*/

# Get call stack
stack: @debug.get_stack()
@foreach(@stack as @frame) {
    @log(@frame.file + ":" + @frame.line + " " + @frame.function)
}
```

## Performance Profiling

```tusk
# Profile code block
@debug.profile("database_operations") {
    users: @query("SELECT * FROM users")
    @foreach(@users as @user) {
        @update_user_stats(@user)
    }
}

# Get profile results
profile: @debug.get_profile("database_operations")
/* Output:
{
    total_time: 0.234
    memory_used: 2048576
    memory_peak: 3145728
    queries: 101
    query_time: 0.189
}
*/

# Benchmark comparison
@debug.benchmark({
    "method1": () => @process_with_method1(),
    "method2": () => @process_with_method2(),
    "method3": () => @process_with_method3()
}, 1000)  # Run 1000 iterations
/* Output:
method1: 0.0023s average
method2: 0.0019s average (fastest)
method3: 0.0045s average
*/
```

## Query Debugging

```tusk
# Enable query logging
@debug.enable_query_log()

# Execute queries
users: @query("SELECT * FROM users WHERE active = ?", [1])
orders: @query("SELECT * FROM orders WHERE user_id = ?", [@user.id])

# Get query log
queries: @debug.get_queries()
/* Output:
[
    {
        sql: "SELECT * FROM users WHERE active = ?"
        params: [1]
        time: 0.0023
        rows: 150
        file: "/app/models/User.tusk:34"
    },
    {
        sql: "SELECT * FROM orders WHERE user_id = ?"
        params: [123]
        time: 0.0156
        rows: 12
        file: "/app/models/Order.tusk:78"
    }
]
*/

# Explain query
@debug.explain("SELECT * FROM users WHERE email = ?", ["test@example.com"])
/* Output:
+----+-------------+-------+------+---------------+-------+---------+-------+------+-------------+
| id | select_type | table | type | possible_keys | key   | key_len | ref   | rows | Extra       |
+----+-------------+-------+------+---------------+-------+---------+-------+------+-------------+
| 1  | SIMPLE      | users | ref  | email_idx     | email | 767     | const | 1    | Using where |
+----+-------------+-------+------+---------------+-------+---------+-------+------+-------------+
*/
```

## Memory Debugging

```tusk
# Memory snapshot
@debug.memory_snapshot("before_operation")

# Do memory-intensive operation
large_data: @load_large_dataset()
processed: @process_data(@large_data)

# Compare memory
@debug.memory_snapshot("after_operation")
diff: @debug.memory_diff("before_operation", "after_operation")
/* Output:
Memory increased by 45.3MB
Peak memory: 128.5MB
Objects created: 15,234
*/

# Memory usage tracking
@debug.track_memory({
    @load_users()
    @process_users()
    @save_results()
})
/* Output:
Step 1: +12.3MB (12.3MB total)
Step 2: +34.2MB (46.5MB total)
Step 3: -15.1MB (31.4MB total)
*/

# Find memory leaks
@debug.find_leaks({
    iterations: 1000
    threshold: 1024  # 1KB
})
```

## Error Debugging

```tusk
# Enhanced error reporting
@debug.on_error((error) => {
    @debug.log("Error occurred", {
        message: @error.message
        file: @error.file
        line: @error.line
        trace: @error.trace
        context: @debug.get_context(@error.file, @error.line, 5)
    })
})

# Try with detailed debugging
@debug.try({
    result: @risky_operation()
} catch (e) {
    @debug.exception(@e)
    /* Output:
    Exception: Database connection failed
    File: /app/db/Connection.tusk:45
    
    Code context:
    43:     @if(!@connection) {
    44:         @retry_count++
    45: >       @throw("Database connection failed")
    46:     }
    47: 
    
    Variables in scope:
    - connection: null
    - retry_count: 3
    - config: {host: "localhost", port: 3306}
    */
})
```

## Interactive Debugging

```tusk
# Set breakpoint
@debug.breakpoint()  # Pauses execution if debugger attached

# Conditional breakpoint
@debug.breakpoint_if(@user.id == 123)

# Watch variables
@debug.watch("user.status", (old_value, new_value) => {
    @log("User status changed from " + @old_value + " to " + @new_value)
})

# Interactive console
@debug.console({
    user: @user
    request: @request
    # Available in console for inspection
})
```

## Network Debugging

```tusk
# Debug HTTP requests
@debug.http_request("GET", "https://api.example.com/users", {
    headers: @headers
    show_request: true
    show_response: true
    show_timing: true
})
/* Output:
=== REQUEST ===
GET https://api.example.com/users
Headers:
  Authorization: Bearer xxx...
  Accept: application/json

=== RESPONSE ===
Status: 200 OK
Time: 234ms
Headers:
  Content-Type: application/json
  X-RateLimit-Remaining: 99
Body:
  {"users": [...]}
*/

# Debug webhook
@debug.webhook(@request, @response)
```

## Template Debugging

```tusk
# Enable template debugging
@debug.templates({
    show_variables: true
    show_includes: true
    show_render_time: true
})

# Debug in template
{@debug.vars()}  # Shows all available variables

{@debug.template_stack()}  # Shows template inheritance

{@debug.partial_time("header.tusk")}  # Time specific partial
```

## Production Debugging

```tusk
# Safe debugging in production
@debug.production({
    # Only enable for specific users
    allowed_users: [@env.DEBUG_USER_ID]
    
    # Or by IP
    allowed_ips: ["192.168.1.100", "10.0.0.5"]
    
    # Log to file instead of output
    log_file: "/var/log/debug.log"
    
    # Mask sensitive data
    mask_fields: ["password", "token", "secret"]
})

# Conditional debug output
@debug.if_allowed("Sensitive operation", @data)

# Debug sampling (1% of requests)
@debug.sample(0.01, "Checking performance", @metrics)
```

## Debug Toolbar

```tusk
# Enable debug toolbar
@debug.toolbar({
    position: "bottom"
    panels: [
        "request",
        "database", 
        "templates",
        "cache",
        "profile",
        "logs",
        "mail"
    ]
})

# Add custom panel
@debug.toolbar.add_panel("custom", {
    title: "My Panel"
    content: @render("debug/custom_panel.tusk")
    badge: @count(@items)
})
```

## Logging Integration

```tusk
# Debug logger
@debug.logger({
    level: "debug"
    file: "/var/log/app_debug.log"
    format: "[{timestamp}] {level}: {message} {context}"
})

# Log with context
@debug.log_context({
    user_id: @user.id
    request_id: @request.id
    session_id: @session.id
})

# All subsequent logs include context
@debug.log("Processing order")  # Includes user_id, request_id, etc.
```

## Best Practices

1. **Disable in production** - Use environment checks
2. **Use descriptive labels** - Make debugging easier
3. **Clean up debug code** - Remove before committing
4. **Log strategically** - Don't over-log
5. **Mask sensitive data** - Never log passwords/tokens
6. **Use appropriate tools** - Profiler for performance, trace for flow

## Related Features

- `@log` - Logging system
- `@profile()` - Performance profiling
- `@trace()` - Execution tracing
- `@monitor` - Application monitoring
- `@test` - Testing utilities