# Inline Objects in TuskLang

Inline objects provide a compact syntax for creating objects within a single line or expression. This guide covers the syntax, use cases, and best practices for inline objects.

## Basic Inline Object Syntax

### Single Line Objects

```tusk
# Basic inline object
point: { x: 10, y: 20 }

# With different value types
person: { name: "John", age: 30, active: true }

# Empty object
empty: {}

# Nested inline
user: { id: 1, address: { city: "NYC", zip: "10001" } }
```

### Inline Objects in Arrays

```tusk
# Array of inline objects
users: [
    { id: 1, name: "Alice", role: "admin" },
    { id: 2, name: "Bob", role: "user" },
    { id: 3, name: "Charlie", role: "user" }
]

# Mixed array
data: [
    "string",
    123,
    { type: "object", value: true },
    ["nested", "array"]
]
```

## Property Syntax

### Key-Value Pairs

```tusk
# Standard property syntax
config: { host: "localhost", port: 8080, ssl: false }

# With quotes (when needed)
special: { "key-with-dash": "value", "123": "numeric key" }

# Computed property names
key: "dynamic"
obj: { [key]: "value", ["computed_" + key]: "another value" }
```

### Property Shorthand

```tusk
# When variable name matches property name
name: "John"
age: 30

# Longhand
person1: { name: name, age: age }

# Shorthand
person2: { name, age }  # Same as above

# Mixed
email: "john@example.com"
user: { name, age, email: email, active: true }
```

## Dynamic Values in Inline Objects

### Expressions as Values

```tusk
# Computed values
stats: {
    total: 100,
    completed: 75,
    percentage: 75 / 100 * 100,
    status: 75 >= 70 ? "passing" : "failing"
}

# Function calls
metadata: {
    timestamp: @time.now(),
    uuid: @uuid.generate(),
    hash: @crypto.hash("data")
}
```

### Conditional Properties

```tusk
# Using ternary operator
user: {
    name: "John",
    role: is_admin ? "admin" : "user",
    permissions: is_admin ? ["read", "write", "delete"] : ["read"]
}

# Spread with conditions
base: { name: "Product", price: 100 }
product = {
    ...base,
    ...(on_sale ? { discount: 20, sale_price: 80 } : {})
}
```

## Spread Operator

### Object Spreading

```tusk
# Spread existing object
defaults: { theme: "light", language: "en" }
settings: { ...defaults, theme: "dark" }
# Result: { theme: "dark", language: "en" }

# Multiple spreads
obj1: { a: 1, b: 2 }
obj2: { b: 3, c: 4 }
combined: { ...obj1, ...obj2, d: 5 }
# Result: { a: 1, b: 3, c: 4, d: 5 }
```

### Conditional Spreading

```tusk
# Conditionally include properties
include_debug: true
config: {
    host: "localhost",
    port: 8080,
    ...(include_debug ? { debug: true, verbose: true } : {})
}

# Pattern for optional properties
create_user = @lambda(name, options = {}, {
    return: {
        id: @uuid.generate(),
        name: name,
        created: @time.now(),
        ...options  # Spread any additional options
    }
})
```

## Inline Objects in Functions

### As Function Arguments

```tusk
# Direct inline object as argument
@http.post("/api/users", { name: "John", email: "john@example.com" })

# With variables
user_data: { name: "Jane", role: "admin" }
@database.insert("users", { ...user_data, created_at: @time.now() })

# Multiple inline objects
@merge(
    { a: 1, b: 2 },
    { b: 3, c: 4 },
    { d: 5 }
)
```

### As Return Values

```tusk
# Return inline object
get_user_summary = @lambda(user, {
    return: {
        id: user.id,
        display_name: "${user.first_name} ${user.last_name}",
        avatar: user.avatar || "/default-avatar.png"
    }
})

# Conditional return
check_status = @lambda(value, {
    return: value > 0 
        ? { success: true, value: value }
        : { success: false, error: "Invalid value" }
})
```

## Method Syntax in Inline Objects

### Methods as Properties

```tusk
# Object with methods
calculator: {
    add: @lambda(a, b, a + b),
    subtract: @lambda(a, b, a - b),
    multiply: @lambda(a, b, a * b),
    divide: @lambda(a, b, {
        @assert(b != 0, "Division by zero")
        return: a / b
    })
}

# Usage
result = calculator.add(5, 3)  # 8
```

### Method Chaining Pattern

```tusk
# Fluent interface
query_builder: {
    _conditions: [],
    
    where: @lambda(condition, {
        @push(_conditions, condition)
        return: @self  # Return self for chaining
    }),
    
    and: @lambda(condition, {
        @push(_conditions, "AND ${condition}")
        return: @self
    }),
    
    build: @lambda({
        return: @join(_conditions, " ")
    })
}

# Usage
query = query_builder
    .where("age > 18")
    .and("status = 'active'")
    .build()
```

## Nested Inline Objects

### Deep Nesting

```tusk
# Complex nested structure
api_response: {
    status: "success",
    data: {
        user: { id: 1, name: "John" },
        posts: [
            { id: 101, title: "First Post", meta: { views: 100 } },
            { id: 102, title: "Second Post", meta: { views: 200 } }
        ]
    },
    metadata: { timestamp: @time.now(), version: "1.0" }
}
```

### Formatting Nested Inline Objects

```tusk
# More readable formatting
config: {
    server: { host: "localhost", port: 8080 },
    database: { 
        host: "db.example.com", 
        port: 5432,
        credentials: { user: "admin", pass: @env.DB_PASS }
    },
    cache: { enabled: true, ttl: 3600 }
}
```

## Type Annotations in Inline Objects

### Inline Type Hints

```tusk
# With type annotations
typed_object: {
    name: string = "John",
    age: number = 30,
    active: boolean = true,
    tags: string[] = ["user", "premium"]
}

# Function with typed inline return
get_point = @lambda(x: number, y: number, {
    return: { x: number = x, y: number = y }
})
```

## Common Patterns

### Options Objects

```tusk
# Function with inline options
fetch_data = @lambda(url, options = {}, {
    defaults: {
        method: "GET",
        headers: {},
        timeout: 30000
    }
    
    config = { ...defaults, ...options }
    return: @http.request(url, config)
})

# Usage
@fetch_data("/api/users", { 
    method: "POST", 
    headers: { "Content-Type": "application/json" } 
})
```

### Factory Functions

```tusk
# Create objects with inline syntax
create_user = @lambda(data, {
    return: {
        id: @uuid.generate(),
        ...data,
        created_at: @time.now(),
        updated_at: @time.now(),
        status: "active"
    }
})

# Usage
new_user = @create_user({ name: "Alice", email: "alice@example.com" })
```

### State Updates

```tusk
# Immutable state update pattern
state: { count: 0, items: [] }

# Update with inline object
new_state = { 
    ...state, 
    count: state.count + 1,
    items: [...state.items, { id: 1, name: "New Item" }]
}
```

## Performance Considerations

### Object Creation Cost

```tusk
# Avoid creating objects in loops
# Inefficient
results = @map(items, @lambda(item, {
    return: { ...defaults, value: item }  # New object each time
}))

# Better - reuse structure
template: { ...defaults }
results = @map(items, @lambda(item, {
    template.value = item
    return: { ...template }  # Clone only when needed
}))
```

### Spread Performance

```tusk
# Multiple spreads can be costly
# Avoid deep spreading in hot paths
result = { ...obj1, ...obj2, ...obj3, ...obj4 }

# Consider Object.assign for many objects
result = @Object.assign({}, obj1, obj2, obj3, obj4)
```

## Best Practices

### 1. Keep Inline Objects Simple

```tusk
# Good - simple and readable
point: { x: 10, y: 20 }

# Too complex for inline
# Better to use multi-line format
complex: { a: { b: { c: { d: { e: "too deep" } } } } }
```

### 2. Use Meaningful Property Names

```tusk
# Bad
data: { a: 1, b: "test", c: true }

# Good
data: { id: 1, name: "test", active: true }
```

### 3. Format for Readability

```tusk
# Single line for simple objects
point: { x: 10, y: 20 }

# Multi-line for clarity when needed
user: {
    id: 1,
    name: "John",
    email: "john@example.com",
    roles: ["user", "editor"]
}
```

### 4. Avoid Side Effects

```tusk
# Bad - side effect in inline object
obj: { 
    value: counter++,  # Modifies external state
    timestamp: @log("Creating object")  # Side effect
}

# Good - pure inline object
obj: {
    value: counter,
    timestamp: @time.now()
}
```

## Common Mistakes

### Missing Commas

```tusk
# Wrong - missing comma
obj: { a: 1 b: 2 }  # Syntax error

# Right
obj: { a: 1, b: 2 }
```

### Trailing Commas

```tusk
# Valid in TuskLang
obj: { 
    a: 1, 
    b: 2,  # Trailing comma OK
}
```

### Quote Confusion

```tusk
# Property names usually don't need quotes
good: { name: "John", age: 30 }

# Only use quotes when necessary
special: { "key-with-dash": "value", "123": "numeric string key" }
```

## Next Steps

- Learn about [Heredoc Strings](021-heredoc-strings.md) for multi-line text
- Explore [References](026-references.md) for object relationships
- Master [Best Practices](030-best-practices.md) for object design