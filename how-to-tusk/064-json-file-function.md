# json() & file.json() - JSON Functions

TuskLang provides powerful JSON handling through the `json()` function for encoding/decoding and `file.json()` for JSON file operations.

## Basic JSON Operations

```tusk
# Encode to JSON
data: {name: "John", age: 30, active: true}
json_string: json(data)  # {"name":"John","age":30,"active":true}

# Pretty print JSON
pretty: json(data, pretty: true)
/* Output:
{
    "name": "John",
    "age": 30,
    "active": true
}
*/

# Decode JSON
parsed: json.decode('{"name":"John","age":30}')
# Returns: {name: "John", age: 30}

# Safe decoding with default
config: json.decode(json_string, default: {})
```

## JSON File Operations

```tusk
# Read JSON file
config: file.json("config.json")

# Write JSON file
file.json("output.json", {
    timestamp: now()
    data: results
})

# Write with formatting
file.json("formatted.json", data, {
    pretty: true
    indent: 4
    sort_keys: true
})

# Update JSON file
file.json.update("settings.json", (current) => {
    current.last_updated: timestamp()
    current.version: (current.version || 0) + 1
    return current
})
```

## Advanced Encoding Options

```tusk
# Custom encoding options
output: json(data, {
    pretty: true              # Pretty print
    indent: 2                 # Indentation spaces
    sort_keys: true          # Sort object keys
    escape_unicode: false    # Don't escape Unicode
    escape_slashes: false    # Don't escape forward slashes
    null_as_empty: true      # null becomes empty string
    stringify_numbers: true  # Large numbers as strings
})

# Handle special values
special_data: {
    infinity: Infinity
    nan: NaN
    undefined: undefined
    function: () => "test"
}

safe_json: json(special_data, {
    handle_special: true  # Converts special values safely
})
# Result: {"infinity":null,"nan":null,"undefined":null}
```

## Error Handling

```tusk
# Safe JSON parsing
parse_json_safe: (string, default: null) => {
    try {
        return json.decode(string)
    } catch (e) {
        log.error("Invalid JSON", {
            error: e.message
            input: string.substring(0, 100) + "..."
        })
        return default
    }
}

# Validate JSON
is_valid_json: (string) => {
    try {
        json.decode(string)
        return true
    } catch {
        return false
    }
}

# Parse with validation
data: json.decode(input, {
    strict: true  # Strict parsing mode
    max_depth: 10  # Prevent deep nesting attacks
    validate: (obj) => {
        # Custom validation
        if (!obj.version || obj.version < 1) {
            throw "Invalid version"
        }
        return true
    }
})
```

## Working with APIs

```tusk
# API response handling
api_response: http.get("https://api.example.com/data")
data: json.decode(api_response.body)

# Send JSON request
response: http.post("https://api.example.com/users", {
    headers: {
        "Content-Type": "application/json"
    }
    body: json({
        name: "John Doe"
        email: "john@example.com"
    })
})

# Parse response
result: json.decode(response.body, {
    default: {error: "Invalid response"}
})
```

## JSON Streaming

```tusk
# Stream large JSON files
file.json.stream("large.json", (item) => {
    # Process each item as it's parsed
    if (item.type == "user") {
        process_user(item)
    }
})

# Write JSON stream
writer: file.json.stream_writer("output.json")
foreach (items as item) {
    writer.write(item)
}
writer.close()

# NDJSON (Newline Delimited JSON)
file.ndjson.write("events.ndjson", events)

file.ndjson.read("events.ndjson", (event) => {
    process_event(event)
})
```

## JSON Schema Validation

```tusk
# Define schema
user_schema: {
    type: "object"
    required: ["name", "email"]
    properties: {
        name: {
            type: "string"
            minLength: 1
            maxLength: 100
        }
        email: {
            type: "string"
            format: "email"
        }
        age: {
            type: "integer"
            minimum: 0
            maximum: 150
        }
    }
}

# Validate against schema
validate_json: (data, schema) => {
    result: json.validate(data, schema)
    
    if (!result.valid) {
        throw {
            message: "Validation failed"
            errors: result.errors
        }
    }
    
    return data
}

# Use validation
user_data: json.decode(input)
valid_user: validate_json(user_data, user_schema)
```

## JSON Transformation

```tusk
# Transform JSON structure
transformer: {
    # Map old keys to new
    mappings: {
        "firstName": "first_name"
        "lastName": "last_name"
        "phoneNumber": "phone"
    }
    
    transform: (data) => {
        result: {}
        
        foreach (data as key => value) {
            new_key: transformer.mappings[key] || key
            
            if (is_object(value)) {
                result[new_key]: transformer.transform(value)
            } else if (is_array(value)) {
                result[new_key]: value.map(v => 
                    is_object(v) ? transformer.transform(v) : v
                )
            } else {
                result[new_key]: value
            }
        }
        
        return result
    }
}

# Apply transformation
original: json.decode('{"firstName":"John","lastName":"Doe"}')
transformed: transformer.transform(original)
# Result: {first_name: "John", last_name: "Doe"}
```

## JSON Patch

```tusk
# Apply JSON Patch (RFC 6902)
original: {
    name: "John"
    age: 30
    hobbies: ["reading", "gaming"]
}

patches: [
    {op: "replace", path: "/name", value: "Jane"}
    {op: "add", path: "/email", value: "jane@example.com"}
    {op: "remove", path: "/age"}
    {op: "add", path: "/hobbies/-", value: "cooking"}
]

patched: json.patch(original, patches)
/* Result:
{
    name: "Jane"
    email: "jane@example.com"
    hobbies: ["reading", "gaming", "cooking"]
}
*/

# Generate patches
diff: json.diff(original, modified)
# Returns array of patch operations
```

## JSON Query

```tusk
# Query JSON with path expressions
data: {
    users: [
        {id: 1, name: "John", active: true}
        {id: 2, name: "Jane", active: false}
        {id: 3, name: "Bob", active: true}
    ]
}

# JSONPath queries
active_users: json.query(data, "$.users[?(@.active==true)]")
names: json.query(data, "$.users[*].name")
first_user: json.query(data, "$.users[0]")

# JMESPath queries
result: json.jmespath(data, "users[?active].{id: id, name: name}")
```

## JSON Merge

```tusk
# Deep merge JSON objects
base: {
    name: "App"
    settings: {
        theme: "light"
        language: "en"
    }
}

override: {
    settings: {
        theme: "dark"
        notifications: true
    }
}

merged: json.merge(base, override, {
    deep: true  # Deep merge
    arrays: "concat"  # How to handle arrays
})
/* Result:
{
    name: "App"
    settings: {
        theme: "dark"
        language: "en"
        notifications: true
    }
}
*/
```

## Configuration Files

```tusk
# Load cascading config files
config: {}

# Load base config
if (file.exists("config/default.json")) {
    config: json.merge(config, file.json("config/default.json"))
}

# Load environment config
env_config: "config/" + env("APP_ENV", "production") + ".json"
if (file.exists(env_config)) {
    config: json.merge(config, file.json(env_config))
}

# Load local config (not in version control)
if (file.exists("config/local.json")) {
    config: json.merge(config, file.json("config/local.json"))
}

# Save resolved config
file.json("config/.resolved.json", config, {pretty: true})
```

## JSON Performance

```tusk
# Fast JSON parsing for large files
large_data: file.read("huge.json")

# Use streaming parser
json.parse_stream(large_data, {
    chunk_size: 1024 * 1024  # 1MB chunks
    on_value: (path, value) => {
        # Process values as they're parsed
        if (path.startsWith("$.items")) {
            process_item(value)
        }
    }
})

# Optimize JSON size
minified: json(data, {
    pretty: false
    remove_null: true
    remove_empty: true
})

# Binary JSON (BSON)
binary: json.to_bson(data)
restored: json.from_bson(binary)
```

## JSON Security

```tusk
# Sanitize JSON before parsing
sanitize_json: (input) => {
    # Remove potentially dangerous content
    cleaned: input
        .replace(/[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]/g, "")  # Control chars
        .replace(/\\x[0-9a-fA-F]{2}/g, "")  # Hex escapes
    
    # Validate structure
    if (!cleaned.match(/^[\s\[\{].*[\]\}]\s*$/)) {
        throw "Invalid JSON structure"
    }
    
    return cleaned
}

# Safe parsing with limits
safe_data: json.decode(sanitized, {
    max_depth: 10  # Prevent deep nesting DoS
    max_length: 1048576  # 1MB max
    allow_comments: false  # Strict JSON only
})
```

## Best Practices

1. **Always handle parse errors** - JSON parsing can fail
2. **Validate untrusted JSON** - Use schemas for validation
3. **Use streaming for large files** - Don't load everything in memory
4. **Set size limits** - Prevent DoS attacks
5. **Escape output properly** - When embedding in HTML/JS
6. **Use pretty print for configs** - Human-readable files
7. **Cache parsed JSON** - Avoid repeated parsing
8. **Consider alternatives** - YAML/TOML for configs

## Related Functions

- `serialize()` - PHP serialization
- `yaml()` - YAML parsing
- `xml()` - XML parsing
- `csv()` - CSV handling
- `parse_str()` - Query string parsing