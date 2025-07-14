# Custom Directives in TuskLang

TuskLang allows you to create custom directives to extend the language with domain-specific functionality, making your code more expressive and maintainable.

## Defining Custom Directives

```tusk
# Basic custom directive
@directive.define("log_execution") {
    pattern: /^(.*)$/  # Match any content
    
    handler: (match, code, context) => {
        # Add logging before and after
        return """
            @log.debug("Executing: {context.name}")
            start_time: @microtime(true)
            
            {code}
            
            duration: @microtime(true) - start_time
            @log.debug("Completed in {duration}s")
        """
    }
}

# Usage
#log_execution {
    result: @complex_calculation()
    @save_result(result)
}
```

## Directive Structure

```tusk
# Complete directive definition
@directive.define("feature") {
    # Pattern to match directive arguments
    pattern: /^(\w+)(?:\s+if:\s*(.+))?$/
    
    # Parse arguments
    parse: (match) => {
        return {
            name: match[1],
            condition: match[2] || "true"
        }
    }
    
    # Transform code
    handler: (args, code, context) => {
        return """
            if (@feature_enabled("{args.name}") && ({args.condition})) {
                {code}
            }
        """
    }
    
    # Metadata
    meta: {
        description: "Conditionally execute code based on feature flags"
        examples: [
            '#feature new_ui { @render("new_ui.tusk") }',
            '#feature beta if: @user.is_beta { @show_beta_features() }'
        ]
    }
}

# Usage
#feature dark_mode if: @user.preferences.theme == "dark" {
    @apply_dark_theme()
}
```

## Route-Style Directives

```tusk
# Custom route directive
@directive.define("websocket") {
    pattern: /^(\/[^\s]+)(?:\s+(.+))?$/
    
    parse: (match) => {
        path: match[1]
        options: @parse_options(match[2] || "")
        
        return {path, options}
    }
    
    handler: (args, code, context) => {
        # Register WebSocket handler
        @websocket.route(args.path, (ws, request) => {
            # Set up context
            @context.ws: ws
            @context.request: request
            
            # Execute directive code
            eval(code)
        })
        
        return ""  # No inline code generation
    }
}

# Usage
#websocket /chat {
    @ws.on("message", (data) => {
        message: @json.decode(data)
        @broadcast_to_room(message.room, message)
    })
    
    @ws.on("close", () => {
        @remove_from_room(@request.user)
    })
}
```

## Decorator Directives

```tusk
# Method decorator directive
@directive.define("cached") {
    pattern: /^(?:(\d+))?(?:\s+key:\s*(.+))?$/
    
    parse: (match) => {
        return {
            ttl: match[1] ? @int(match[1]) : 3600,
            key: match[2] || null
        }
    }
    
    handler: (args, code, context) => {
        # Extract function name from code
        func_match: code.match(/^(\w+):\s*\(([^)]*)\)\s*=>\s*{/)
        
        if (!func_match) {
            throw "Cached directive requires a function"
        }
        
        func_name: func_match[1]
        func_params: func_match[2]
        
        cache_key: args.key || `"{context.namespace}:{func_name}:" + @md5(@json_encode([{func_params}]))`
        
        return """
            {func_name}: ({func_params}) => {
                _cache_key: {cache_key}
                _cached: @cache.get(_cache_key)
                
                if (_cached !== null) {
                    return _cached
                }
                
                _result: (() => {
                    {code}
                })()
                
                @cache.put(_cache_key, _result, {args.ttl})
                return _result
            }
        """
    }
}

# Usage
#cached 7200 key: "user_stats:{user_id}" {
    get_user_stats: (user_id) => {
        stats: @db.query("SELECT ... expensive query ...")
        return @process_stats(stats)
    }
}
```

## Validation Directives

```tusk
# Input validation directive
@directive.define("validate") {
    pattern: /^(.+)$/
    
    parse: (match) => {
        # Parse validation rules
        rules: @parse_validation_rules(match[1])
        return {rules}
    }
    
    handler: (args, code, context) => {
        validation_code: ""
        
        for (field, rules in args.rules) {
            validation_code += """
                if (!@validate_field(@request.{field}, "{rules}")) {
                    @response.status: 422
                    return @json({
                        error: "Validation failed",
                        field: "{field}",
                        rules: "{rules}"
                    })
                }
            """
        }
        
        return validation_code + "\n" + code
    }
}

# Usage
#validate {
    name: "required|string|max:255"
    email: "required|email"
    age: "required|integer|min:18"
} {
    #api /users method: POST {
        # Validation runs first
        user: @User.create(@request.validated)
        return user
    }
}
```

## Async Directives

```tusk
# Async execution directive
@directive.define("async") {
    pattern: /^(?:pool:\s*(\w+))?(?:\s+timeout:\s*(\d+))?$/
    
    parse: (match) => {
        return {
            pool: match[1] || "default",
            timeout: match[2] ? @int(match[2]) : null
        }
    }
    
    handler: (args, code, context) => {
        return """
            @async.run({
                pool: "{args.pool}",
                timeout: {args.timeout || "null"},
                task: async () => {
                    {code}
                }
            })
        """
    }
}

# Usage
#async pool: heavy_tasks timeout: 30000 {
    result: await @expensive_async_operation()
    await @save_result(result)
}
```

## Testing Directives

```tusk
# Test case directive
@directive.define("test") {
    pattern: /^"([^"]+)"(?:\s+(\w+))?$/
    
    parse: (match) => {
        return {
            description: match[1],
            type: match[2] || "unit"
        }
    }
    
    handler: (args, code, context) => {
        test_name: @slugify(args.description)
        
        # Register test
        @test.register({
            name: test_name,
            description: args.description,
            type: args.type,
            file: context.file,
            line: context.line,
            run: () => {
                eval(code)
            }
        })
        
        return ""  # Tests are registered, not inline
    }
}

# Usage
#test "User can login with valid credentials" integration {
    user: @factory("user").create()
    
    response: @post("/login", {
        email: user.email,
        password: "password"
    })
    
    @assert.equals(response.status, 200)
    @assert.has(response.json(), "token")
}
```

## Monitoring Directives

```tusk
# Performance monitoring directive
@directive.define("monitor") {
    pattern: /^(\w+)(?:\s+alert:\s*(.+))?$/
    
    parse: (match) => {
        return {
            metric: match[1],
            alert_threshold: match[2] ? @float(match[2]) : null
        }
    }
    
    handler: (args, code, context) => {
        return """
            _monitor_start: @microtime(true)
            _monitor_memory: @memory_get_usage()
            
            try {
                _monitor_result: (() => {
                    {code}
                })()
                
                _monitor_duration: @microtime(true) - _monitor_start
                _monitor_memory_used: @memory_get_usage() - _monitor_memory
                
                @metrics.record("{args.metric}", {
                    duration: _monitor_duration,
                    memory: _monitor_memory_used,
                    success: true
                })
                
                if ({args.alert_threshold} && _monitor_duration > {args.alert_threshold}) {
                    @alert("Performance threshold exceeded for {args.metric}", {
                        duration: _monitor_duration,
                        threshold: {args.alert_threshold}
                    })
                }
                
                return _monitor_result
                
            } catch (e) {
                @metrics.record("{args.metric}", {
                    success: false,
                    error: e.message
                })
                throw e
            }
        """
    }
}

# Usage
#monitor database_query alert: 0.5 {
    results: @db.query("SELECT * FROM large_table WHERE complex_conditions")
    @process_results(results)
}
```

## Composition Directives

```tusk
# Compose multiple directives
@directive.define("authenticated_api") {
    pattern: /^(\/[^\s]+)(?:\s+roles:\s*\[([^\]]+)\])?$/
    
    parse: (match) => {
        return {
            path: match[1],
            roles: match[2] ? match[2].split(",").map(r => r.trim()) : []
        }
    }
    
    handler: (args, code, context) => {
        middleware: ["auth", "api"]
        
        if (args.roles.length > 0) {
            middleware.push(`role:${args.roles.join("|")}`)
        }
        
        return """
            #api {args.path} middleware: {middleware} {
                {code}
            }
        """
    }
}

# Usage
#authenticated_api /admin/users roles: [admin, super_admin] {
    users: @User.with("roles").paginate(20)
    return users
}
```

## Registering Directives

```tusk
# Register in bootstrap file
@app.boot(() => {
    # Load custom directives
    @directive.load_from_directory("app/directives")
    
    # Register individual directive
    @directive.register("my_directive", MyDirective)
    
    # Register from configuration
    for (name, config in @config.custom_directives) {
        @directive.define(name, config)
    }
})

# Directive class
class MyDirective {
    pattern: /^(.+)$/
    
    parse(match) {
        return {value: match[1]}
    }
    
    handler(args, code, context) {
        return @transform_code(code, args, context)
    }
    
    validate(args) {
        # Validate directive arguments
        if (!args.value) {
            throw "Value required for my_directive"
        }
    }
}
```

## Best Practices

1. **Keep directives focused** - Single responsibility
2. **Use clear naming** - Directive names should be intuitive
3. **Validate arguments** - Check directive parameters
4. **Document thoroughly** - Include examples and use cases
5. **Handle errors gracefully** - Provide helpful error messages
6. **Test directives** - Unit test transformation logic
7. **Consider performance** - Directives run at compile time
8. **Version carefully** - Changing directives can break code

## Related Topics

- `hash-directive-intro` - Directive basics
- `macro-system` - Code generation
- `metaprogramming` - Advanced techniques
- `compiler-hooks` - Compilation process
- `dsl-creation` - Domain-specific languages