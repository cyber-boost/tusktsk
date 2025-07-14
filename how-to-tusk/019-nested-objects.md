# Nested Objects in TuskLang

Nested objects allow you to create complex, hierarchical data structures in TuskLang. This guide covers creating, accessing, and manipulating nested objects effectively.

## Creating Nested Objects

### Basic Nesting

```tusk
# Simple nested object
user:
    name: "John Doe"
    age: 30
    address:
        street: "123 Main St"
        city: "Springfield"
        country: "USA"
```

### Deep Nesting

```tusk
# Multiple levels of nesting
company:
    name: "Tech Corp"
    founded: 2010
    headquarters:
        address:
            street: "456 Tech Ave"
            city: "San Francisco"
            state: "CA"
            coordinates:
                lat: 37.7749
                lng: -122.4194
        building:
            floors: 10
            parking: true
    departments:
        engineering:
            head: "Alice Johnson"
            employees: 50
            teams:
                frontend:
                    lead: "Bob Smith"
                    members: 12
                backend:
                    lead: "Carol White"
                    members: 15
```

## Accessing Nested Values

### Dot Notation

```tusk
company:
    name: "Tech Corp"
    location:
        city: "San Francisco"
        state: "CA"

# Access nested values
company_name: company.name
city: company.location.city
state: company.location.state

# Deep access
building_floors: company.headquarters.building.floors
```

### Bracket Notation

```tusk
# Useful for dynamic keys
key: "location"
location = company[key]

# Accessing with variables
property: "city"
city = company.location[property]

# Mixed notation
value = company["location"].city
```

### Optional Chaining

```tusk
# Safe access to nested properties
user:
    profile:
        settings: null

# Without optional chaining (might error)
# theme = user.profile.settings.theme  # Error!

# With optional chaining
theme = user?.profile?.settings?.theme ?? "default"

# Check deeply nested values
has_premium = user?.subscription?.type == "premium"
```

## Modifying Nested Objects

### Direct Assignment

```tusk
user:
    name: "John"
    preferences:
        theme: "light"
        language: "en"

# Modify nested value
user.preferences.theme = "dark"

# Add new nested property
user.preferences.notifications = true

# Create nested structure on the fly
user.social = {
    twitter: "@john"
    github: "john-doe"
}
```

### Dynamic Path Assignment

```tusk
# Set value at dynamic path
set_nested = @lambda(obj, path, value, {
    keys = @split(path, ".")
    current = obj
    
    @each(keys, @lambda(key, index, {
        @if(index == @len(keys) - 1, {
            current[key] = value
        }, {
            current[key] = current[key] || {}
            current = current[key]
        })
    }))
})

# Usage
config: {}
@set_nested(config, "database.host", "localhost")
@set_nested(config, "database.port", 5432)
# Result: { database: { host: "localhost", port: 5432 } }
```

## Merging Nested Objects

### Shallow Merge

```tusk
defaults:
    theme: "light"
    language: "en"
    features:
        autoSave: true
        spellCheck: true

user_prefs:
    theme: "dark"
    features:
        autoSave: false

# Shallow merge (features object replaced entirely)
merged = @merge(defaults, user_prefs)
# Result: features only has autoSave: false
```

### Deep Merge

```tusk
# Deep merge function
deep_merge = @lambda(target, source, {
    @each(@keys(source), @lambda(key, {
        @if(@isObject(source[key]) && @isObject(target[key]), {
            target[key] = @deep_merge(target[key], source[key])
        }, {
            target[key] = source[key]
        })
    }))
    return: target
})

# Usage
result = @deep_merge(@clone(defaults), user_prefs)
# Result: features has both autoSave: false and spellCheck: true
```

## Working with Complex Structures

### Mixed Arrays and Objects

```tusk
data:
    users: [
        {
            id: 1
            name: "Alice"
            roles: ["admin", "user"]
            permissions:
                read: true
                write: true
                delete: true
        },
        {
            id: 2
            name: "Bob"
            roles: ["user"]
            permissions:
                read: true
                write: false
                delete: false
        }
    ]
    settings:
        max_users: 100
        features: ["chat", "video", "screen-share"]

# Access mixed structures
first_user_name: data.users[0].name
admin_can_delete: data.users[0].permissions.delete
available_features: data.settings.features
```

### Recursive Structures

```tusk
# Tree structure
file_system:
    name: "root"
    type: "directory"
    children: [
        {
            name: "home"
            type: "directory"
            children: [
                {
                    name: "user"
                    type: "directory"
                    children: [
                        {
                            name: "document.txt"
                            type: "file"
                            size: 1024
                        }
                    ]
                }
            ]
        },
        {
            name: "etc"
            type: "directory"
            children: []
        }
    ]

# Recursive function to find files
find_files = @lambda(node, files = [], {
    @if(node.type == "file", {
        @push(files, node)
    }, {
        @each(node.children || [], @lambda(child, {
            @find_files(child, files)
        }))
    })
    return: files
})
```

## Validation and Type Checking

### Schema Validation

```tusk
# Define schema
user_schema:
    name: { type: "string", required: true }
    age: { type: "number", min: 0, max: 150 }
    email: { type: "string", pattern: "^[^@]+@[^@]+$" }
    address: {
        type: "object"
        properties: {
            street: { type: "string" }
            city: { type: "string", required: true }
            zip: { type: "string", pattern: "^\\d{5}$" }
        }
    }

# Validation function
validate_object = @lambda(obj, schema, path = "", {
    errors: []
    
    @each(@keys(schema), @lambda(key, {
        field_schema = schema[key]
        value = obj[key]
        field_path = path ? "${path}.${key}" : key
        
        # Check required
        @if(field_schema.required && value == null, {
            @push(errors, "${field_path} is required")
            return
        })
        
        # Type checking
        @if(value != null && field_schema.type, {
            @if(field_schema.type == "object" && field_schema.properties, {
                # Recursive validation
                nested_errors = @validate_object(value, field_schema.properties, field_path)
                errors = @concat(errors, nested_errors.errors)
            }, {
                # Simple type check
                @if(!@checkType(value, field_schema.type), {
                    @push(errors, "${field_path} must be ${field_schema.type}")
                })
            })
        })
    }))
    
    return: { valid: @len(errors) == 0, errors: errors }
})
```

## Transformation Patterns

### Object Mapping

```tusk
# Transform nested structure
transform_user = @lambda(user, {
    return: {
        id: user.id
        displayName: "${user.firstName} ${user.lastName}"
        contact: {
            email: user.emailAddress
            phone: user.phoneNumber
        }
        location: "${user.address.city}, ${user.address.state}"
        isActive: user.status == "active"
    }
})

# Batch transformation
users: [/* array of users */]
transformed = @map(users, transform_user)
```

### Flattening Objects

```tusk
# Flatten nested object to dot notation
flatten_object = @lambda(obj, prefix = "", result = {}, {
    @each(@keys(obj), @lambda(key, {
        new_key = prefix ? "${prefix}.${key}" : key
        
        @if(@isObject(obj[key]) && !@isArray(obj[key]), {
            @flatten_object(obj[key], new_key, result)
        }, {
            result[new_key] = obj[key]
        })
    }))
    
    return: result
})

# Example
nested:
    user:
        name: "John"
        address:
            city: "NYC"
            zip: "10001"

flat = @flatten_object(nested)
# Result: {
#   "user.name": "John",
#   "user.address.city": "NYC",
#   "user.address.zip": "10001"
# }
```

### Unflattening Objects

```tusk
# Convert dot notation back to nested
unflatten_object = @lambda(obj, {
    result: {}
    
    @each(@keys(obj), @lambda(key, {
        @set_nested(result, key, obj[key])
    }))
    
    return: result
})
```

## Query and Filter Patterns

### Deep Search

```tusk
# Search nested objects
find_in_object = @lambda(obj, predicate, results = [], path = "", {
    @if(@predicate(obj, path), {
        @push(results, { value: obj, path: path })
    })
    
    @if(@isObject(obj), {
        @each(@keys(obj), @lambda(key, {
            new_path = path ? "${path}.${key}" : key
            @find_in_object(obj[key], predicate, results, new_path)
        }))
    })
    
    return: results
})

# Find all email fields
emails = @find_in_object(data, @lambda(value, path, {
    @isString(value) && @includes(path, "email")
}))
```

### Path-based Access

```tusk
# Get value by path
get_by_path = @lambda(obj, path, {
    keys = @split(path, ".")
    return: @reduce(keys, @lambda(current, key, current?.[key]), obj)
})

# Set value by path
set_by_path = @lambda(obj, path, value, {
    keys = @split(path, ".")
    last_key = @pop(keys)
    target = @reduce(keys, @lambda(current, key, {
        current[key] = current[key] || {}
        return: current[key]
    }), obj)
    target[last_key] = value
})

# Usage
value = @get_by_path(company, "departments.engineering.head")
@set_by_path(company, "departments.engineering.budget", 1000000)
```

## Performance Considerations

### Avoid Deep Cloning

```tusk
# Expensive for large objects
deep_clone = @lambda(obj, {
    @JSON.parse(@JSON.stringify(obj))
})

# Better - clone only what you need
partial_clone = {
    id: original.id
    name: original.name
    settings: { ...original.settings }  # Shallow clone settings
}
```

### Lazy Evaluation

```tusk
# Don't compute all properties upfront
user_view:
    basic: user.basic_info
    
    # Compute expensive properties only when accessed
    detailed = @lazy({
        posts_count: @db.count("posts", { user_id: user.id })
        followers: @db.count("followers", { user_id: user.id })
        last_activity: @db.last_activity(user.id)
    })
```

## Best Practices

### 1. Keep Nesting Reasonable

```tusk
# Too deep (hard to work with)
data.users[0].profile.settings.preferences.ui.theme.color.primary

# Better - flatten where sensible
data.users[0].theme_primary_color
```

### 2. Use Consistent Structure

```tusk
# Consistent nested structure
api_response:
    success: true
    data: { /* actual data */ }
    error: null
    metadata: {
        timestamp: @time.now()
        version: "1.0"
    }
```

### 3. Document Complex Structures

```tusk
###
# User object structure:
# {
#   id: string
#   profile: {
#     name: string
#     email: string
#     preferences: {
#       theme: "light" | "dark"
#       language: string
#     }
#   }
#   permissions: string[]
# }
###
```

### 4. Handle Missing Properties

```tusk
# Defensive programming
safe_access = user?.profile?.email ?? "no-email@example.com"

# With defaults
user_settings = @merge(default_settings, user?.settings ?? {})
```

## Common Patterns

### Configuration Objects

```tusk
app_config:
    server:
        host: @env.HOST || "localhost"
        port: @env.PORT || 3000
        ssl:
            enabled: @env.NODE_ENV == "production"
            cert: "/path/to/cert"
            key: "/path/to/key"
    
    database:
        primary:
            host: @env.DB_HOST
            port: 5432
            name: @env.DB_NAME
        
        replica:
            host: @env.DB_REPLICA_HOST
            port: 5432
            name: @env.DB_NAME
    
    features:
        cache:
            enabled: true
            ttl: 3600
            driver: "redis"
```

### State Management

```tusk
app_state:
    user: {
        id: null
        authenticated: false
        profile: null
    }
    
    ui: {
        theme: "light"
        sidebar: {
            visible: true
            width: 250
        }
        modals: {
            login: false
            settings: false
        }
    }
    
    data: {
        posts: []
        comments: {}
        loading: {
            posts: false
            comments: false
        }
    }
```

## Next Steps

- Learn about [Inline Objects](020-inline-objects.md) for compact syntax
- Explore [References](026-references.md) for object relationships
- Master [Best Practices](030-best-practices.md) for data modeling