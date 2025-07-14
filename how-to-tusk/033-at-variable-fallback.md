# @ Variable Fallback

Variable fallbacks in TuskLang provide elegant ways to handle missing values, undefined variables, and null references. This guide covers fallback patterns and best practices for robust error handling.

## Basic Fallback Syntax

### The || Operator

```tusk
# Basic fallback with ||
port = @env.PORT || 8080
name = @config.app_name || "DefaultApp"
timeout = @settings.timeout || 30000

# The || operator returns the first truthy value
value = null || undefined || "" || 0 || "found"  # "found"
```

### The ?? Operator (Null Coalescing)

```tusk
# Null coalescing - only checks for null/undefined
count = @user.count ?? 0  # Preserves 0 as valid value

# Difference between || and ??
value1 = 0 || 10        # 10 (0 is falsy)
value2 = 0 ?? 10        # 0 (0 is not null)

value3 = "" || "default" # "default" (empty string is falsy)
value4 = "" ?? "default" # "" (empty string is not null)
```

## Chained Fallbacks

### Multiple Fallback Levels

```tusk
# Priority-based fallbacks
database_url = @env.DATABASE_URL ??          # 1. Environment variable
               @config.database.url ??        # 2. Config file
               @secrets.db_url ??             # 3. Secrets manager
               "postgresql://localhost/dev"   # 4. Development default

# With different sources
user_name = @request.body.name ??     # From request
            @request.query.name ??     # From query string
            @session.user.name ??      # From session
            "Anonymous"                # Default
```

### Conditional Fallbacks

```tusk
# Environment-specific fallbacks
api_endpoint = @if(@env.NODE_ENV == "production",
    @env.API_URL ?? "https://api.example.com",
    @env.DEV_API_URL ?? "http://localhost:3000"
)

# Complex fallback logic
cache_ttl = @if(@feature_flags.aggressive_cache,
    @config.cache.long_ttl ?? 3600,
    @config.cache.short_ttl ?? 300
)
```

## Safe Navigation with Fallbacks

### Optional Chaining with Defaults

```tusk
# Combine ?. and ?? for safe access with defaults
user_city = @user?.address?.city ?? "Unknown City"
first_item_name = @items?.[0]?.name ?? "No items"

# Deep navigation with fallbacks
setting_value = @config?.features?.advanced?.timeout ?? 5000

# Method calls with fallbacks
formatted_date = @date?.toLocaleDateString?.() ?? "Invalid Date"
```

### Nested Object Fallbacks

```tusk
# Fallback for entire objects
user_preferences = @user?.preferences ?? {
    theme: "light"
    language: "en"
    notifications: true
}

# Merge with defaults
user_settings = @merge({
    theme: "light"
    language: "en"
    notifications: true
}, @user?.preferences ?? {})
```

## Function Fallbacks

### Fallback Functions

```tusk
# Try primary function, fall back to secondary
get_user_data = @lambda(id, {
    # Try cache first
    cached = @cache.get("user_${id}")
    @if(cached, return: cached)
    
    # Try primary database
    user = @try({
        return: @primary_db.get_user(id)
    }, {
        # Fall back to replica
        return: @replica_db.get_user(id)
    })
    
    return: user ?? null
})
```

### Lazy Evaluation Fallbacks

```tusk
# Only compute fallback if needed
expensive_default = @lambda({ 
    @compute_expensive_default() 
})

# Only calls expensive_default if config value is null
value = @config.setting ?? @expensive_default()

# Pattern for expensive fallbacks
get_or_compute = @lambda(key, compute_fn, {
    cached = @cache.get(key)
    @if(cached != null, return: cached)
    
    computed = @compute_fn()
    @cache.set(key, computed)
    return: computed
})
```

## Type-Safe Fallbacks

### Type Coercion with Fallbacks

```tusk
# Ensure correct types
port = @number(@env.PORT) ?? 8080
debug = @boolean(@env.DEBUG) ?? false
max_items = @int(@env.MAX_ITEMS) ?? 100

# With validation
safe_port = @lambda({
    port = @number(@env.PORT)
    @if(port && port > 0 && port <= 65535,
        return: port,
        return: 8080
    )
})()
```

### Typed Fallback Patterns

```tusk
# Type-specific fallbacks
get_string_config = @lambda(key, default_value: string, {
    value = @config[key]
    return: @isString(value) ? value : default_value
})

get_number_config = @lambda(key, default_value: number, {
    value = @config[key]
    return: @isNumber(value) ? value : default_value
})

# Usage
app_name = @get_string_config("name", "DefaultApp")
port = @get_number_config("port", 8080)
```

## Array and Collection Fallbacks

### Empty Array Fallbacks

```tusk
# Fallback for empty or missing arrays
user_roles = @user?.roles ?? []
active_users = @users?.filter(@lambda(u, u.active)) ?? []

# First item with fallback
primary_server = @servers?.[0] ?? { host: "localhost", port: 8080 }

# Safe array operations
first_three = @items?.slice(0, 3) ?? []
```

### Collection Access Patterns

```tusk
# Safe collection access with defaults
get_from_map = @lambda(map, key, default_value, {
    return: @map?.[key] ?? default_value
})

# Multiple collection fallbacks
find_setting = @lambda(key, {
    return: @user_settings?.[key] ??
            @team_settings?.[key] ??
            @global_settings?.[key] ??
            @default_settings[key]
})
```

## Error Handling with Fallbacks

### Try-Catch Fallbacks

```tusk
# Fallback on errors
safe_parse_json = @lambda(text, fallback = {}, {
    return: @try({
        return: @json.parse(text)
    }, {
        @log.error("JSON parse failed: ${@error.message}")
        return: fallback
    })
})

# Multiple error fallbacks
fetch_data = @lambda(url, {
    return: @try({
        # Try primary URL
        return: @http.get(url)
    }, {
        # Try backup URL
        return: @try({
            return: @http.get(@backup_url)
        }, {
            # Return cached data
            return: @cache.get("last_known_data") ?? {
                error: "All sources failed"
            }
        })
    })
})
```

### Validation with Fallbacks

```tusk
# Validate and provide fallback
validate_email = @lambda(email, fallback = "noreply@example.com", {
    is_valid = @regex.test(email, "^[^@]+@[^@]+\.[^@]+$")
    return: is_valid ? email : fallback
})

# Sanitize with fallback
sanitize_username = @lambda(input, {
    sanitized = @regex.replace(input ?? "", "[^a-zA-Z0-9_]", "")
    return: @len(sanitized) > 0 ? sanitized : "user_${@random_id()}"
})
```

## Configuration Fallback Patterns

### Hierarchical Configuration

```tusk
# Configuration hierarchy with fallbacks
get_config = @lambda(key, {
    # Check in order of precedence
    return: @env[key] ??                    # 1. Environment
            @cli_args[key] ??               # 2. CLI arguments
            @config_file[key] ??            # 3. Config file
            @defaults[key] ??               # 4. Defaults
            null                            # 5. Explicit null
})

# Nested configuration fallbacks
get_nested_config = @lambda(path, default_value, {
    parts = @split(path, ".")
    value = @config
    
    @each(parts, @lambda(part, {
        value = value?.[part]
        @if(value == null, return: default_value)
    }))
    
    return: value ?? default_value
})
```

### Environment-Specific Fallbacks

```tusk
# Development vs Production fallbacks
config:
    api_url = @if(@env.NODE_ENV == "production",
        @env.API_URL ?? @error("API_URL required in production"),
        @env.API_URL ?? "http://localhost:3000"
    )
    
    cache_enabled = @if(@env.NODE_ENV == "production",
        true,  # Always in production
        @env.ENABLE_CACHE ?? false  # Optional in dev
    )
```

## Performance Considerations

### Eager vs Lazy Fallbacks

```tusk
# Eager evaluation (always computes both)
result = @expensive_operation() || @another_expensive_operation()

# Lazy evaluation (only computes if needed)
result = @cached_value ?? @lambda({
    value = @expensive_operation()
    @cache.set("key", value)
    return: value
})()

# Memoized fallbacks
create_memoized = @lambda(fn, {
    cache: null
    return: @lambda({
        @if(cache != null, return: cache)
        cache = @fn()
        return: cache
    })
})
```

### Fallback Chains Performance

```tusk
# Inefficient: Multiple deep property access
value = @obj?.deep?.nested?.value ?? 
        @obj?.deep?.nested?.default ??
        @obj?.deep?.default_value ??
        "fallback"

# Efficient: Cache intermediate values
deep = @obj?.deep
nested = @deep?.nested
value = @nested?.value ?? 
        @nested?.default ?? 
        @deep?.default_value ?? 
        "fallback"
```

## Best Practices

### 1. Order Fallbacks by Likelihood

```tusk
# Most likely to succeed first
user_pref = @user.preferences.theme ??      # User's choice
            @team.settings.theme ??          # Team default
            @organization.theme ??           # Org default
            "light"                          # System default
```

### 2. Use Appropriate Operators

```tusk
# Use ?? for values that might be falsy but valid
items_per_page = @config.pagination.items ?? 10  # 0 would be valid

# Use || for truly falsy checks
message = @error_message || "An error occurred"  # Empty string not valid
```

### 3. Document Fallback Behavior

```tusk
# API timeout configuration
# Priority: Environment > Config > Default (30s)
# Note: 0 means no timeout, not a fallback trigger
api_timeout = @env.API_TIMEOUT ?? @config.api.timeout ?? 30000
```

### 4. Fail Fast in Production

```tusk
# Development: Permissive fallbacks
dev_config:
    api_key = @env.API_KEY ?? "dev-key-12345"
    
# Production: Fail if critical values missing
prod_config:
    api_key = @env.API_KEY ?? @error("API_KEY environment variable required")
```

## Common Patterns

### Settings with Defaults

```tusk
# Application settings with structured defaults
app_settings = @merge({
    # Defaults
    theme: "light"
    language: "en"
    timezone: "UTC"
    features: {
        analytics: false
        notifications: true
    }
}, @user_settings ?? {})
```

### Resource Loading

```tusk
# Load resource with fallbacks
load_template = @lambda(name, {
    # Try user templates
    template = @file.read("./templates/user/${name}.tsk") ??
               # Try theme templates  
               @file.read("./templates/theme/${name}.tsk") ??
               # Try system templates
               @file.read("./templates/system/${name}.tsk") ??
               # Use inline default
               @default_templates[name] ??
               # Error
               @error("Template '${name}' not found")
               
    return: template
})
```

### API Response Handling

```tusk
# Graceful API response handling
handle_api_response = @lambda(response, {
    # Check response validity
    data = @response?.data ?? 
           @response?.body ?? 
           @response
    
    # Parse if needed
    parsed = @if(@isString(data),
        @json.parse(data) ?? {},
        data ?? {}
    )
    
    # Extract with fallbacks
    return: {
        items: @parsed.items ?? @parsed.results ?? []
        total: @parsed.total ?? @parsed.count ?? 0
        page: @parsed.page ?? 1
        success: @parsed.success ?? true
    }
})
```

## Next Steps

- Explore [Request Object](034-at-request-object.md) fallbacks
- Learn about [Error Operators](058-at-operator-errors.md)
- Master [Null Safety](017-null-values.md) patterns