# Null Values in TuskLang

Null represents the absence of a value in TuskLang. Understanding how to work with null is crucial for writing robust applications that handle missing data gracefully.

## The Null Literal

### Basic Null Assignment

```tusk
# Explicit null assignment
value: null
missing_data: null
not_yet_loaded: null

# Null is a reserved keyword (lowercase only)
# These are NOT null:
# Null     # Undefined variable
# NULL     # Undefined variable
# "null"   # String
```

### When Null Appears

```tusk
# Uninitialized optional values
user:
    name: "John"
    email: "john@example.com"
    phone: null  # Optional field

# Failed operations
result = @database.find_user(999)  # Returns null if not found

# Cleared values
cache_value = null  # Explicitly cleared
```

## Null Checking

### Direct Comparison

```tusk
value: null

# Check for null
is_null = value == null        # true
is_not_null = value != null    # false

# Null is only equal to null
null == null          # true
null == false         # false
null == 0            # false
null == ""           # false
null == undefined    # false
```

### Null Testing Functions

```tusk
# Built-in null checking
@isNull(null)        # true
@isNull(0)           # false
@isNull("")          # false
@isNull(false)       # false

# Check for null or undefined
@isNullOrUndefined(null)      # true
@isNullOrUndefined(undefined) # true
@isNullOrUndefined(0)         # false

# Check if value exists (not null/undefined)
@exists(null)        # false
@exists(0)           # true
@exists("")          # true
```

## Null Coalescing

### Using OR Operator

```tusk
# Fallback values with ||
name = user.name || "Anonymous"
port = @env.PORT || 8080

# Be careful with falsy values
count = user.count || 0  # Problem if count could be 0

# Null coalescing operator (only checks null/undefined)
count = user.count ?? 0  # Preserves 0 as valid value
```

### Null Coalescing Patterns

```tusk
# Chain multiple fallbacks
display_name = user.nickname ?? user.fullname ?? user.email ?? "User"

# With functions
get_config = @lambda(key, default_value, {
    value: @env[key] ?? @config[key] ?? default_value
    return: value
})

# Safe navigation
city = user?.address?.city ?? "Unknown"
```

## Optional Chaining

### Safe Property Access

```tusk
# Traditional null checking
city = null
@if(user != null && user.address != null, {
    city = user.address.city
})

# Optional chaining
city = user?.address?.city

# With array access
first_phone = user?.phones?.[0]

# With function calls
result = api?.getData?.()
```

### Combining with Null Coalescing

```tusk
# Safe access with default
user_city = user?.address?.city ?? "No city specified"

# Multiple levels
company_name = employee?.department?.company?.name ?? "Unknown Company"

# With array methods
first_tag = post?.tags?.[0]?.name ?? "Untagged"
```

## Null in Conditionals

### Truthiness of Null

```tusk
# Null is falsy
value: null

@if(value, {
    # This won't execute
    @print("Value exists")
}, {
    # This executes
    @print("Value is null or falsy")
})

# Explicit null check
@if(value == null, {
    @print("Value is specifically null")
})
```

### Guard Clauses

```tusk
process_user = @lambda(user, {
    # Early return for null
    @if(user == null, {
        return: { error: "User is null" }
    })
    
    # Process valid user
    return: {
        name: user.name
        email: user.email
    }
})
```

## Null Handling Patterns

### Default Object Pattern

```tusk
# Define defaults
default_user: {
    name: "Guest"
    email: ""
    role: "visitor"
    preferences: {}
}

# Merge with possible null
safe_user = @merge(default_user, user ?? {})
```

### Null Object Pattern

```tusk
# Instead of returning null, return a null object
null_user: {
    id: null
    name: "Unknown User"
    email: "no-email@example.com"
    is_null: true
    
    # Methods that do nothing
    save: @lambda({ return: false })
    delete: @lambda({ return: false })
}

find_user = @lambda(id, {
    user = @database.get_user(id)
    return: user ?? @null_user
})
```

### Maybe/Option Pattern

```tusk
# Wrap nullable values
maybe = @lambda(value, {
    is_null: value == null
    value: value
    
    map: @lambda(fn, {
        @if(@is_null, @maybe(null), @maybe(fn(@value)))
    })
    
    or_else: @lambda(default, {
        @if(@is_null, default, @value)
    })
})

# Usage
user_maybe = @maybe(@find_user(id))
user_name = @user_maybe.map(@lambda(u, u.name)).or_else("Unknown")
```

## Null in Data Structures

### Arrays with Null

```tusk
# Arrays can contain null
data: [1, null, 3, null, 5]

# Filter out nulls
clean_data = @filter(data, @lambda(x, x != null))
# Result: [1, 3, 5]

# Count non-null values
non_null_count = @filter(data, @exists).length

# Replace nulls
filled_data = @map(data, @lambda(x, x ?? 0))
# Result: [1, 0, 3, 0, 5]
```

### Objects with Null Values

```tusk
user: {
    name: "John"
    email: null
    phone: null
    address: {
        street: "123 Main St"
        city: "Springfield"
        zip: null
    }
}

# Remove null properties
clean_user = @filter_object(user, @lambda(key, value, value != null))

# Get all null fields
null_fields = @filter(@entries(user), @lambda(entry, entry[1] == null))
```

## Database and Null

### Query Results

```tusk
# Database queries often return null
user = @db.query_one("SELECT * FROM users WHERE id = ?", user_id)

@if(user == null, {
    @response.status(404)
    @response.json({ error: "User not found" })
}, {
    @response.json(user)
})
```

### Null in SQL

```tusk
# Handle SQL NULL values
users_with_email = @db.query("""
    SELECT * FROM users 
    WHERE email IS NOT NULL
""")

# Insert with null
@db.execute("""
    INSERT INTO users (name, email, phone) 
    VALUES (?, ?, ?)
""", name, email ?? null, phone ?? null)
```

## JSON and Null

### JSON Serialization

```tusk
data: {
    name: "Product"
    description: null
    price: 19.99
}

# Null is preserved in JSON
json = @json.stringify(data)
# {"name":"Product","description":null,"price":19.99}

# Option to remove nulls
json_clean = @json.stringify(data, { exclude_null: true })
# {"name":"Product","price":19.99}
```

### JSON Parsing

```tusk
json_string: '{"name":"John","age":null}'
parsed = @json.parse(json_string)

# Check parsed values
has_age = parsed.age != null  # false
age = parsed.age ?? 0          # 0
```

## Null Safety Best Practices

### 1. Explicit Null Handling

```tusk
# Bad - assumes value exists
username = user.name.toUpperCase()

# Good - handles null
username = user?.name?.toUpperCase() ?? "ANONYMOUS"
```

### 2. Document Nullable Fields

```tusk
# User type definition
user_schema: {
    id: "string"          # Required
    name: "string"        # Required
    email: "string"       # Required
    phone: "string?"      # Optional (nullable)
    bio: "string?"        # Optional (nullable)
}
```

### 3. Validate Input

```tusk
validate_user = @lambda(data, {
    errors: []
    
    # Required fields cannot be null
    @if(data.name == null, {
        errors.push("Name is required")
    })
    
    # Optional fields can be null but validate if present
    @if(data.phone != null && !@validate.phone(data.phone), {
        errors.push("Invalid phone number")
    })
    
    return: {
        valid: @len(errors) == 0
        errors: errors
    }
})
```

### 4. Use Type Guards

```tusk
# Type guard function
is_valid_user = @lambda(obj, {
    return: obj != null && 
            obj.id != null && 
            obj.name != null &&
            obj.email != null
})

# Usage
@if(@is_valid_user(data), {
    # Safe to use data.id, data.name, data.email
    @process_user(data)
})
```

## Error Handling with Null

### Try-Catch Pattern

```tusk
safe_divide = @lambda(a, b, {
    @if(b == 0, {
        return: null  # Return null for invalid operation
    })
    return: a / b
})

result = @safe_divide(10, 0)
@if(result == null, {
    @print("Division by zero")
})
```

### Result Type Pattern

```tusk
# Return structured results instead of null
find_user_safe = @lambda(id, {
    user = @db.find_user(id)
    
    @if(user == null, {
        return: { success: false, error: "User not found" }
    })
    
    return: { success: true, data: user }
})

# Usage
result = @find_user_safe(123)
@if(result.success, {
    @print("Found user: ${result.data.name}")
}, {
    @print("Error: ${result.error}")
})
```

## Performance Considerations

### Null Check Ordering

```tusk
# Check null first (fast)
@if(value != null && @expensive_validation(value), {
    # Process
})

# Not this (expensive_validation might run on null)
@if(@expensive_validation(value) && value != null, {
    # Process
})
```

### Caching Null Results

```tusk
# Cache null results to avoid repeated lookups
user_cache: {}

get_user = @lambda(id, {
    # Check cache (including null values)
    @if(@has_key(user_cache, id), {
        return: user_cache[id]
    })
    
    # Fetch and cache (even if null)
    user = @db.find_user(id)
    user_cache[id] = user
    return: user
})
```

## Common Mistakes

### Comparing with Undefined

```tusk
# Wrong - undefined is not null
value = undefined
is_null = value == null  # false

# Right - check both
is_null_or_undefined = value == null || value == undefined
```

### Forgetting Null in Chains

```tusk
# Dangerous
length = user.name.length  # Error if user or name is null

# Safe
length = user?.name?.length ?? 0
```

### Null vs Empty

```tusk
# Don't confuse null with empty values
@if(user.bio == null, {
    # Bio was never set
})

@if(user.bio == "", {
    # Bio was set to empty string
})

# Check both if needed
@if(user.bio == null || user.bio == "", {
    # Bio is missing or empty
})
```

## Next Steps

- Learn about [Arrays](018-arrays.md) and null handling in collections
- Explore [Nested Objects](019-nested-objects.md) with nullable fields
- Master [Error Handling](029-syntax-errors.md) for null safety