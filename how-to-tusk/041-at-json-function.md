# @ JSON Function

The @json functions provide powerful JSON parsing and stringification capabilities in TuskLang. This guide covers working with JSON data, including parsing, stringifying, and manipulating JSON structures.

## JSON Parsing

### Basic Parsing

```tusk
# Parse JSON string to object
json_string: '{"name": "John", "age": 30, "active": true}'
data = @json.parse(json_string)

# Access parsed data
name = data.name        # "John"
age = data.age         # 30
is_active = data.active # true

# Parse with error handling
safe_parse = @lambda(json_text, {
    return: @try({
        return: @json.parse(json_text)
    }, {
        @log.error("Invalid JSON: ${@error.message}")
        return: null
    })
})
```

### Parsing Complex Structures

```tusk
# Parse nested JSON
complex_json: '''
{
    "users": [
        {"id": 1, "name": "Alice", "roles": ["admin", "user"]},
        {"id": 2, "name": "Bob", "roles": ["user"]}
    ],
    "metadata": {
        "total": 2,
        "page": 1,
        "timestamp": "2024-01-01T00:00:00Z"
    }
}
'''

response = @json.parse(complex_json)

# Access nested data
first_user = response.users[0]
total_users = response.metadata.total
admin_users = @filter(response.users, @lambda(u, @includes(u.roles, "admin")))
```

### Parse Options

```tusk
# Parse with custom reviver function
json_with_dates: '{"created": "2024-01-01T00:00:00Z", "count": 42}'

parsed = @json.parse(json_with_dates, @lambda(key, value, {
    # Convert date strings to timestamps
    @if(key == "created" && @isString(value), {
        return: @date.parse(value)
    })
    return: value
}))

# Safe parsing with defaults
parse_with_defaults = @lambda(json_text, defaults = {}, {
    parsed = @try({
        return: @json.parse(json_text)
    }, {
        return: {}
    })
    
    return: @merge(defaults, parsed)
})
```

## JSON Stringification

### Basic Stringification

```tusk
# Convert object to JSON string
data: {
    name: "John Doe"
    age: 30
    active: true
    tags: ["developer", "designer"]
}

json_string = @json.stringify(data)
# Result: '{"name":"John Doe","age":30,"active":true,"tags":["developer","designer"]}'

# Pretty printing
pretty_json = @json.stringify(data, null, 2)
# Result:
# {
#   "name": "John Doe",
#   "age": 30,
#   "active": true,
#   "tags": [
#     "developer",
#     "designer"
#   ]
# }
```

### Stringify Options

```tusk
# Custom replacer function
data: {
    name: "John"
    password: "secret123"
    email: "john@example.com"
    internal_id: "usr_123"
}

# Filter sensitive data
safe_json = @json.stringify(data, @lambda(key, value, {
    # Exclude sensitive fields
    @if(@includes(["password", "internal_id"], key), {
        return: undefined  # Exclude from output
    })
    
    # Mask email
    @if(key == "email", {
        return: @regex.replace(value, "(?<=.{3}).(?=.*@)", "*")
    })
    
    return: value
}))

# Result: '{"name":"John","email":"joh***@example.com"}'
```

### Circular Reference Handling

```tusk
# Handle circular references
create_safe_stringify = @lambda({
    seen: []
    
    return: @lambda(obj, space = 0, {
        return: @json.stringify(obj, @lambda(key, value, {
            @if(@isObject(value), {
                @if(@includes(@seen, value), {
                    return: "[Circular Reference]"
                })
                @push(@seen, value)
            })
            return: value
        }), space)
    })
})

safe_stringify = @create_safe_stringify()
```

## JSON Manipulation

### Deep Merge JSON

```tusk
# Merge JSON objects deeply
deep_merge_json = @lambda(json1, json2, {
    obj1 = @if(@isString(json1), @json.parse(json1), json1)
    obj2 = @if(@isString(json2), @json.parse(json2), json2)
    
    merged = @deep_merge(obj1, obj2)
    
    return: @json.stringify(merged)
})

# Example usage
base_config: '{"app": {"name": "MyApp", "version": "1.0"}, "debug": false}'
overrides: '{"app": {"version": "2.0"}, "debug": true, "new_feature": true}'

final_config = @deep_merge_json(base_config, overrides)
# Result: {"app":{"name":"MyApp","version":"2.0"},"debug":true,"new_feature":true}
```

### JSON Path Access

```tusk
# Access JSON data using path notation
json_path = @lambda(data, path, {
    # Parse if string
    obj = @if(@isString(data), @json.parse(data), data)
    
    # Split path and traverse
    parts = @split(path, ".")
    result = obj
    
    @each(parts, @lambda(part, {
        # Handle array notation
        @if(@regex.test(part, "\\[\\d+\\]"), {
            [key, index] = @regex.match(part, "(.+)\\[(\\d+)\\]")
            result = result?.[key]?.[parseInt(index)]
        }, {
            result = result?.[part]
        })
    }))
    
    return: result
})

# Usage
data: '{"users": [{"name": "John", "address": {"city": "NYC"}}]}'
city = @json_path(data, "users[0].address.city")  # "NYC"
```

### JSON Schema Validation

```tusk
# Validate JSON against schema
validate_json_schema = @lambda(data, schema, {
    errors: []
    
    # Parse if needed
    obj = @if(@isString(data), @json.parse(data), data)
    
    # Check required fields
    @each(schema.required ?? [], @lambda(field, {
        @if(!obj[field], {
            @push(errors, "Missing required field: ${field}")
        })
    }))
    
    # Validate types
    @each(@keys(schema.properties ?? {}), @lambda(key, {
        @if(obj[key] != null, {
            expected_type = schema.properties[key].type
            actual_type = @typeof(obj[key])
            
            @if(expected_type != actual_type, {
                @push(errors, "Field '${key}' should be ${expected_type}, got ${actual_type}")
            })
        })
    }))
    
    return: {
        valid: @len(errors) == 0
        errors: errors
    }
})

# Example schema
user_schema: {
    required: ["name", "email"]
    properties: {
        name: { type: "string" }
        email: { type: "string" }
        age: { type: "number" }
    }
}
```

## Working with JSON APIs

### API Response Handling

```tusk
# Fetch and parse JSON API response
fetch_json = @lambda(url, options = {}, {
    response = @http.request(url, @merge({
        headers: {
            "Accept": "application/json"
            "Content-Type": "application/json"
        }
    }, options))
    
    # Check response
    @if(response.status >= 400, {
        @throw("HTTP Error ${response.status}: ${response.statusText}")
    })
    
    # Parse JSON response
    return: @json.parse(response.body)
})

# Usage with error handling
get_user_data = @lambda(user_id, {
    return: @try({
        data: @fetch_json("https://api.example.com/users/${user_id}")
        return: { success: true, data: data }
    }, {
        error: @catch
        return: { success: false, error: error.message }
    })
})
```

### JSON-RPC Implementation

```tusk
# Simple JSON-RPC client
json_rpc_call = @lambda(url, method, params = null, {
    request_data: {
        jsonrpc: "2.0"
        method: method
        params: params
        id: @uuid()
    }
    
    response = @http.post(url, {
        headers: { "Content-Type": "application/json" }
        body: @json.stringify(request_data)
    })
    
    result = @json.parse(response.body)
    
    # Check for JSON-RPC error
    @if(result.error, {
        @throw("JSON-RPC Error ${result.error.code}: ${result.error.message}")
    })
    
    return: result.result
})

# Usage
user = @json_rpc_call("https://api.example.com/rpc", "getUser", { id: 123 })
```

## JSON Transformation

### Transform Keys

```tusk
# Convert between naming conventions
transform_keys = @lambda(data, transformer, {
    obj = @if(@isString(data), @json.parse(data), data)
    
    transform_object = @lambda(o, {
        @if(@isArray(o), {
            return: @map(o, transform_object)
        })
        
        @if(@isObject(o), {
            result: {}
            @each(@keys(o), @lambda(key, {
                new_key = @transformer(key)
                result[new_key] = @transform_object(o[key])
            }))
            return: result
        })
        
        return: o
    })
    
    return: @transform_object(obj)
})

# Snake case to camel case
snake_to_camel = @lambda(str, {
    @regex.replace(str, "_([a-z])", @lambda(match, letter, {
        return: @upper(letter)
    }))
})

# Usage
snake_data: { user_name: "John", user_age: 30 }
camel_data = @transform_keys(snake_data, @snake_to_camel)
# Result: { userName: "John", userAge: 30 }
```

### Filter JSON Data

```tusk
# Filter JSON array based on criteria
filter_json_array = @lambda(json_data, filter_fn, {
    data = @if(@isString(json_data), @json.parse(json_data), json_data)
    
    @if(!@isArray(data), {
        @throw("Input must be an array")
    })
    
    filtered = @filter(data, filter_fn)
    return: @json.stringify(filtered)
})

# Example: Filter active users
users_json: '[{"name":"John","active":true},{"name":"Jane","active":false}]'
active_users = @filter_json_array(users_json, @lambda(user, user.active))
```

## JSON Diff and Patch

### Compare JSON Objects

```tusk
# Compare two JSON objects
json_diff = @lambda(obj1, obj2, path = "", {
    diffs: []
    
    # Compare all keys from both objects
    all_keys = @unique(@concat(@keys(obj1), @keys(obj2)))
    
    @each(all_keys, @lambda(key, {
        current_path = path ? "${path}.${key}" : key
        val1 = obj1[key]
        val2 = obj2[key]
        
        @if(val1 === undefined, {
            @push(diffs, { type: "added", path: current_path, value: val2 })
        }, @if(val2 === undefined, {
            @push(diffs, { type: "removed", path: current_path, value: val1 })
        }, @if(@isObject(val1) && @isObject(val2), {
            # Recursive diff
            nested_diffs = @json_diff(val1, val2, current_path)
            diffs = @concat(diffs, nested_diffs)
        }, @if(val1 !== val2, {
            @push(diffs, { 
                type: "changed", 
                path: current_path, 
                oldValue: val1, 
                newValue: val2 
            })
        }))))
    }))
    
    return: diffs
})
```

## Performance Optimization

### Streaming JSON Parser

```tusk
# Parse large JSON files in chunks
stream_json_parse = @lambda(file_path, callback, {
    buffer: ""
    depth: 0
    in_string: false
    escape_next: false
    
    # Process chunks
    @file.stream(file_path, @lambda(chunk, {
        buffer = buffer + chunk
        
        # Simple JSON object detection
        @each(@split(chunk, ""), @lambda(char, {
            @if(escape_next, {
                escape_next = false
                return
            })
            
            @if(char == "\\" && in_string, {
                escape_next = true
                return
            })
            
            @if(char == '"' && !escape_next, {
                in_string = !in_string
                return
            })
            
            @if(!in_string, {
                @if(char == "{", depth = depth + 1)
                @if(char == "}", {
                    depth = depth - 1
                    @if(depth == 0, {
                        # Complete object found
                        obj = @json.parse(buffer)
                        @callback(obj)
                        buffer = ""
                    })
                })
            })
        }))
    }))
})
```

### JSON Memory Optimization

```tusk
# Optimize memory for large JSON operations
optimize_json_memory = @lambda({
    # Use streaming for large files
    large_file_threshold: 10 * 1024 * 1024  # 10MB
    
    # Clear parsed objects after use
    process_json_file = @lambda(file_path, processor, {
        size = @file.size(file_path)
        
        @if(size > large_file_threshold, {
            # Stream process
            @stream_json_parse(file_path, processor)
        }, {
            # Load all at once
            data = @json.parse(@file.read(file_path))
            result = @processor(data)
            data = null  # Clear reference
            return: result
        })
    })
})
```

## Error Handling

### Safe JSON Operations

```tusk
# Comprehensive JSON error handling
safe_json_operation = @lambda(operation, fallback = null, {
    return: @try({
        return: @operation()
    }, {
        error: @catch
        
        @if(@includes(error.message, "JSON"), {
            @log.error("JSON Error: ${error.message}")
            return: fallback
        })
        
        # Re-throw non-JSON errors
        @throw(error)
    })
})

# Usage
result = @safe_json_operation(@lambda({
    data = @json.parse(potentially_invalid_json)
    return: data.some.deep.property
}), { default: "value" })
```

## Best Practices

1. **Always handle parse errors** - JSON.parse can throw
2. **Use pretty printing** for debugging and logs
3. **Filter sensitive data** when stringifying
4. **Validate JSON structure** before using
5. **Consider memory usage** for large JSON files
6. **Use streaming** for very large datasets
7. **Cache parsed JSON** when used multiple times

## Common Patterns

### JSON Configuration Loader

```tusk
# Load and merge JSON configuration files
load_json_config = @lambda(base_path, env = "development", {
    # Load base config
    base_config = @json.parse(@file.read("${base_path}/config.base.json"))
    
    # Load environment config
    env_config = @try({
        return: @json.parse(@file.read("${base_path}/config.${env}.json"))
    }, {
        return: {}
    })
    
    # Load local overrides
    local_config = @try({
        return: @json.parse(@file.read("${base_path}/config.local.json"))
    }, {
        return: {}
    })
    
    # Merge all configs
    return: @deep_merge(base_config, env_config, local_config)
})
```

### JSON Logger

```tusk
# Structured JSON logging
json_logger = @lambda(level, message, context = {}, {
    log_entry: {
        timestamp: @time.iso()
        level: level
        message: message
        context: context
        hostname: @system.hostname
        pid: @process.pid
    }
    
    # Output as JSON line
    @console.log(@json.stringify(log_entry))
    
    # Also write to file if configured
    @if(@config.log_file, {
        @file.append(@config.log_file, @json.stringify(log_entry) + "\n")
    })
})

# Usage
@json_logger("info", "User logged in", { user_id: 123, ip: @request.ip })
```

## Next Steps

- Learn about [Render Function](042-at-render-function.md)
- Explore [HTTP Operations](050-at-http-host.md)
- Master [Database Queries](044-at-query-database.md)