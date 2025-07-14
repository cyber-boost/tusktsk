# @ Operator Errors

Understanding and handling errors with @ operators is crucial for building robust TuskLang applications. This guide covers error types, handling strategies, and best practices for @ operator error management.

## Common @ Operator Errors

### Reference Errors

```tusk
# Undefined variable reference
value = @undefined_variable  # Error: undefined_variable is not defined

# Non-existent property
user: { name: "John" }
email = @user.email  # Returns null, not error
age = @user.profile.age  # Error: Cannot read property 'age' of undefined

# Safe property access
email = @user?.email ?? "no-email"
age = @user?.profile?.age ?? 0
```

### Type Errors

```tusk
# Wrong operator usage
static_value: @env.PORT  # Error: @ operators require = not :

# Invalid operation on null
value: null
result = @value.toUpperCase()  # Error: Cannot read property of null

# Type mismatch in operations
text: "hello"
result = @text * 2  # Error: Cannot multiply string

# Safe type handling
result = @if(@isString(text), @text.toUpperCase(), text)
```

### Syntax Errors

```tusk
# Missing parentheses
result = @map items, @lambda(x, x * 2)  # Error: Missing parentheses

# Correct syntax
result = @map(items, @lambda(x, x * 2))

# Invalid @ operator syntax
value = @ env.PORT  # Error: Space after @
value = @@double_at  # Error: Invalid syntax

# Nested @ operator issues
value = @{@dynamic_key}  # Error: Invalid syntax
# Correct: Use brackets
value = @parent[@dynamic_key]
```

## Error Handling Strategies

### Try-Catch Pattern

```tusk
# Basic try-catch with @ operators
safe_operation = @try({
    data = @json.parse(@file.read("config.json"))
    port = @data.server.port
    return: port
}, {
    error = @catch
    @log.error("Failed to load config: ${error.message}")
    return: 8080  # Default value
})

# Catching specific errors
result = @try({
    user = @db.query("SELECT * FROM users WHERE id = ?", user_id)[0]
    return: @user.email
}, {
    error = @catch
    
    @if(@includes(error.message, "connection"), {
        @log.error("Database connection error")
        return: null
    })
    
    @if(@includes(error.message, "undefined"), {
        @log.warn("User not found")
        return: "not-found"
    })
    
    # Re-throw unknown errors
    @throw(error)
})
```

### Error Object Structure

```tusk
# Standard error object
handle_error = @lambda(operation, {
    result = @try({
        data: @operation()
        return: { success: true, data: data }
    }, {
        error: @catch
        
        return: {
            success: false
            error: {
                message: error.message
                code: error.code ?? "UNKNOWN_ERROR"
                stack: error.stack
                timestamp: @time.now()
                operation: @operation.name
            }
        }
    })
})
```

### Custom Error Types

```tusk
# Define custom error types
ValidationError = @lambda(message, field, {
    return: {
        name: "ValidationError"
        message: message
        field: field
        code: "VALIDATION_ERROR"
    }
})

NotFoundError = @lambda(resource, id, {
    return: {
        name: "NotFoundError"
        message: "${resource} with id ${id} not found"
        resource: resource
        id: id
        code: "NOT_FOUND"
    }
})

# Throw custom errors
validate_email = @lambda(email, {
    @if(!@regex.test(email, "^[^@]+@[^@]+$"), {
        @throw(@ValidationError("Invalid email format", "email"))
    })
})
```

## @ Operator-Specific Error Handling

### Environment Variable Errors

```tusk
# Missing environment variables
api_key = @env.API_KEY  # Might be null

# Error if required
require_env = @lambda(name, {
    value = @env[name]
    @if(!value, {
        @throw("Environment variable ${name} is required")
    })
    return: value
})

api_key = @require_env("API_KEY")

# With fallback and warning
get_env = @lambda(name, default_value = null, required = false, {
    value = @env[name]
    
    @if(!value, {
        @if(required, {
            @throw("Required environment variable ${name} is not set")
        })
        
        @if(default_value == null, {
            @log.warn("Environment variable ${name} not set")
        })
        
        return: default_value
    })
    
    return: value
})
```

### File Operation Errors

```tusk
# File reading errors
read_config = @lambda(path, {
    return: @try({
        content: @file.read(path)
        data: @json.parse(content)
        return: data
    }, {
        error: @catch
        
        @if(@includes(error.message, "ENOENT"), {
            @log.error("File not found: ${path}")
            return: null
        })
        
        @if(@includes(error.message, "EACCES"), {
            @log.error("Permission denied: ${path}")
            return: null
        })
        
        @if(@includes(error.message, "JSON"), {
            @log.error("Invalid JSON in file: ${path}")
            return: null
        })
        
        @throw(error)
    })
})
```

### HTTP Request Errors

```tusk
# Comprehensive HTTP error handling
http_request = @lambda(url, options = {}, {
    max_retries = options.retries ?? 3
    retry_delay = options.retry_delay ?? 1000
    
    attempt = @lambda(retry_count, {
        return: @try({
            response = @http.request(url, options)
            
            # Check HTTP status
            @if(response.status >= 400, {
                @throw({
                    name: "HTTPError"
                    message: "HTTP ${response.status}: ${response.statusText}"
                    status: response.status
                    response: response
                })
            })
            
            return: response
        }, {
            error = @catch
            
            # Network errors - retry
            @if(@includes(error.message, "ECONNREFUSED") || 
                @includes(error.message, "ETIMEDOUT"), {
                
                @if(retry_count < max_retries, {
                    @log.warn("Network error, retrying... (${retry_count + 1}/${max_retries})")
                    @time.sleep(retry_delay * (retry_count + 1))
                    return: @attempt(retry_count + 1)
                })
            })
            
            # HTTP errors - check if should retry
            @if(error.name == "HTTPError", {
                # Retry on 5xx errors
                @if(error.status >= 500 && retry_count < max_retries, {
                    @log.warn("Server error ${error.status}, retrying...")
                    @time.sleep(retry_delay * (retry_count + 1))
                    return: @attempt(retry_count + 1)
                })
                
                # Don't retry client errors (4xx)
                @throw(error)
            })
            
            @throw(error)
        })
    })
    
    return: @attempt(0)
})
```

### Database Query Errors

```tusk
# Database error handling
safe_query = @lambda(sql, params = [], {
    return: @try({
        results: @query(sql, ...params)
        return: { success: true, data: results }
    }, {
        error: @catch
        
        # Connection errors
        @if(@includes(error.message, "ECONNREFUSED"), {
            return: {
                success: false
                error: "Database connection refused"
                code: "DB_CONNECTION_ERROR"
                retry: true
            }
        })
        
        # Constraint violations
        @if(@includes(error.message, "UNIQUE constraint"), {
            return: {
                success: false
                error: "Duplicate entry"
                code: "DUPLICATE_ERROR"
                retry: false
            }
        })
        
        # Syntax errors
        @if(@includes(error.message, "syntax error"), {
            @log.error("SQL syntax error: ${sql}")
            return: {
                success: false
                error: "Query syntax error"
                code: "SYNTAX_ERROR"
                retry: false
            }
        })
        
        # Unknown database errors
        return: {
            success: false
            error: error.message
            code: "DB_ERROR"
            retry: false
        }
    })
})
```

## Error Recovery Patterns

### Fallback Chain

```tusk
# Multiple fallback sources
get_config_value = @lambda(key, {
    # Try primary source
    value = @try({
        return: @cache.get(key)
    })
    
    # Try secondary source
    @if(!value, {
        value = @try({
            return: @db.query("SELECT value FROM config WHERE key = ?", key)[0]?.value
        })
    })
    
    # Try file source
    @if(!value, {
        value = @try({
            config = @json.parse(@file.read("config.json"))
            return: @config[key]
        })
    })
    
    # Final fallback
    return: value ?? @defaults[key] ?? null
})
```

### Circuit Breaker Pattern

```tusk
# Prevent cascading failures
create_circuit_breaker = @lambda(operation, options = {}, {
    threshold: options.threshold ?? 5
    timeout: options.timeout ?? 60000  # 1 minute
    
    state: "closed"  # closed, open, half-open
    failures: 0
    last_failure: null
    
    return: @lambda(...args, {
        # Check if circuit is open
        @if(state == "open", {
            # Check if timeout has passed
            @if(@time.now() - last_failure > timeout, {
                state = "half-open"
                failures = 0
            }, {
                @throw("Circuit breaker is open")
            })
        })
        
        # Try operation
        result = @try({
            result: @operation(...args)
            
            # Success - reset circuit
            @if(state == "half-open", {
                state = "closed"
            })
            failures = 0
            
            return: result
        }, {
            error: @catch
            failures = failures + 1
            last_failure = @time.now()
            
            # Open circuit if threshold reached
            @if(failures >= threshold, {
                state = "open"
                @log.error("Circuit breaker opened after ${failures} failures")
            })
            
            @throw(error)
        })
        
        return: result
    })
})
```

## Error Logging and Monitoring

### Structured Error Logging

```tusk
# Comprehensive error logger
log_error = @lambda(error, context = {}, {
    error_data: {
        timestamp: @time.iso()
        level: "ERROR"
        message: error.message
        code: error.code
        stack: error.stack
        
        # Context information
        user_id: @request?.user?.id
        request_id: @request?.id
        url: @request?.url
        method: @request?.method
        
        # Custom context
        ...context
        
        # System information
        hostname: @system.hostname
        process_id: @process.pid
        memory_usage: @process.memory_usage
    }
    
    # Log to multiple destinations
    @console.error(@json.stringify(error_data))
    @file.append("errors.log", @json.stringify(error_data) + "\n")
    
    # Send to monitoring service
    @try({
        @http.post("https://monitoring.example.com/errors", {
            body: error_data
        })
    })
    
    return: error_data
})
```

### Error Aggregation

```tusk
# Collect and group errors
error_aggregator:
    errors: []
    max_errors: 100
    
    add: @lambda(error, {
        @push(@errors, {
            error: error
            timestamp: @time.now()
            count: 1
        })
        
        # Limit array size
        @if(@len(@errors) > @max_errors, {
            @shift(@errors)
        })
        
        # Group similar errors
        @aggregate()
    })
    
    aggregate: @lambda({
        grouped = @group_by(@errors, @lambda(e, {
            # Group by error message and code
            return: "${e.error.code}_${e.error.message}"
        }))
        
        return: @map(@entries(grouped), @lambda(entry, {
            [key, errors] = entry
            return: {
                key: key
                count: @len(errors)
                first_seen: @min(@map(errors, @lambda(e, e.timestamp)))
                last_seen: @max(@map(errors, @lambda(e, e.timestamp)))
                sample: errors[0].error
            }
        }))
    })
```

## Best Practices

### 1. Fail Fast with Clear Messages

```tusk
# Bad: Silent failure
value = @env.API_KEY ?? ""

# Good: Clear error for required values
value = @env.API_KEY ?? @throw("API_KEY environment variable is required")
```

### 2. Use Specific Error Types

```tusk
# Bad: Generic error
@throw("Error occurred")

# Good: Specific error with context
@throw({
    name: "ConfigurationError"
    message: "Invalid port number: must be between 1 and 65535"
    field: "port"
    value: port
    code: "INVALID_PORT"
})
```

### 3. Provide Recovery Options

```tusk
# Bad: Just throw error
data = @file.read(path)

# Good: Provide fallback
data = @try({
    return: @file.read(path)
}, {
    @log.warn("Could not read ${path}, using default")
    return: @default_data
})
```

### 4. Log Errors with Context

```tusk
# Bad: No context
@catch(error, {
    @log.error(error.message)
})

# Good: Rich context
@catch(error, {
    @log_error(error, {
        operation: "user_registration"
        user_email: email
        step: "email_validation"
    })
})
```

### 5. Handle Async Errors

```tusk
# Handle Promise rejections
async_operation = @lambda({
    return: @promise(@lambda(resolve, reject, {
        @try({
            result = @http.get(url)
            @resolve(result)
        }, {
            error = @catch
            @log_error(error)
            @reject(error)
        })
    }))
})
```

## Common Error Patterns

### Validation Errors

```tusk
# Collect multiple validation errors
validate_user = @lambda(data, {
    errors: []
    
    # Validate required fields
    @if(!data.name, {
        @push(errors, @ValidationError("Name is required", "name"))
    })
    
    @if(!data.email, {
        @push(errors, @ValidationError("Email is required", "email"))
    }, @if(!@validate.email(data.email), {
        @push(errors, @ValidationError("Invalid email format", "email"))
    }))
    
    @if(data.age != null && (data.age < 0 || data.age > 150), {
        @push(errors, @ValidationError("Age must be between 0 and 150", "age"))
    })
    
    @if(@len(errors) > 0, {
        @throw({
            name: "ValidationError"
            message: "Validation failed"
            errors: errors
        })
    })
    
    return: true
})
```

### Retry with Backoff

```tusk
# Exponential backoff retry
retry_with_backoff = @lambda(operation, max_attempts = 3, {
    attempt = @lambda(count, {
        return: @try({
            return: @operation()
        }, {
            error = @catch
            
            @if(count >= max_attempts, {
                @throw(error)
            })
            
            # Exponential backoff: 1s, 2s, 4s...
            delay = 1000 * (2 ** (count - 1))
            @log.warn("Attempt ${count} failed, retrying in ${delay}ms...")
            
            @time.sleep(delay)
            return: @attempt(count + 1)
        })
    })
    
    return: @attempt(1)
})
```

## Next Steps

- Learn about [Performance Optimization](059-at-operator-performance.md)
- Explore [Security Best Practices](060-at-operator-security.md)
- Master [Error Recovery Patterns](033-at-variable-fallback.md)