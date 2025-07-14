# Typed Values in TuskLang

While TuskLang is dynamically typed by default, it supports optional type annotations and runtime type checking for better code reliability and documentation. This guide covers TuskLang's type system.

## Basic Type Annotations

### Simple Type Hints

```tusk
# Basic type annotations using : type syntax
name: string = "John Doe"
age: number = 30
is_active: boolean = true
data: object = { key: "value" }
items: array = [1, 2, 3]

# Type annotations without initial values
username: string
user_id: number
settings: object
```

### Type Inference

```tusk
# TuskLang can infer types from values
message: "Hello"        # Inferred as string
count: 42              # Inferred as number
enabled: true          # Inferred as boolean

# Explicit typing overrides inference
id: string = 123       # Will convert to "123"
```

## Built-in Types

### Primitive Types

```tusk
# String type
first_name: string = "Jane"
last_name: string = "Doe"
email: string       # Declared but not initialized

# Number type (integers and floats)
age: number = 25
price: number = 19.99
quantity: number

# Boolean type
is_verified: boolean = true
has_subscription: boolean = false
is_admin: boolean

# Null type
optional_value: null = null
empty_field: null
```

### Composite Types

```tusk
# Object type
user: object = {
    name: "John"
    age: 30
}

# Array type
numbers: array = [1, 2, 3, 4, 5]
mixed: array = ["string", 123, true]

# Function type
calculate: function = @lambda(a, b, a + b)
```

## Advanced Type Syntax

### Union Types

```tusk
# Value can be one of multiple types
status: string | number = "active"    # Can be string
status = 1                            # Or number

# Common union patterns
result: object | null = @database.find(id)
port: string | number = @env.PORT || 3000

# Multiple unions
value: string | number | boolean | null
```

### Array Type Syntax

```tusk
# Array of specific type
names: string[] = ["Alice", "Bob", "Charlie"]
scores: number[] = [95, 87, 92]

# Nested array types
matrix: number[][] = [
    [1, 2, 3],
    [4, 5, 6]
]

# Mixed array (any type)
mixed: any[] = ["text", 123, true, { key: "value" }]
```

### Object Type Definitions

```tusk
# Inline object type
person: { name: string, age: number } = {
    name: "John"
    age: 30
}

# Type definition
type User = {
    id: number
    name: string
    email: string
    roles: string[]
}

# Using type definition
current_user: User = {
    id: 1
    name: "Admin"
    email: "admin@example.com"
    roles: ["admin", "user"]
}
```

## Optional and Nullable Types

### Optional Properties

```tusk
# Optional properties with ?
type Profile = {
    name: string
    bio?: string          # Optional
    avatar?: string       # Optional
    website?: string      # Optional
}

user_profile: Profile = {
    name: "John"          # Required
    # bio, avatar, website are optional
}
```

### Nullable Types

```tusk
# Explicitly nullable
middle_name: string? = null      # Can be string or null
phone: string? = "+1234567890"   # Currently string

# Nullable in objects
type Contact = {
    email: string
    phone: string?        # Can be null
    fax: string?         # Can be null
}
```

## Type Validation

### Runtime Type Checking

```tusk
# Validate types at runtime
validate_user = @lambda(data: any, {
    # Check required types
    @assert(@isString(data.name), "Name must be a string")
    @assert(@isNumber(data.age), "Age must be a number")
    @assert(data.age >= 0, "Age must be positive")
    
    # Check optional types
    @if(data.email != null, {
        @assert(@isString(data.email), "Email must be a string")
        @assert(@includes(data.email, "@"), "Invalid email format")
    })
    
    return: true
})
```

### Type Guards

```tusk
# Type guard functions
is_valid_user = @lambda(obj: any): boolean, {
    return: @isObject(obj) && 
            @isString(obj.name) && 
            @isNumber(obj.age) &&
            @isArray(obj.roles)
})

# Using type guards
process_data = @lambda(data: any, {
    @if(@is_valid_user(data), {
        # Safe to use as User type
        welcome_message: "Hello, ${data.name}!"
    }, {
        @error("Invalid user data")
    })
})
```

## Generic Types

### Generic Functions

```tusk
# Generic type parameters
map_array = @lambda<T, U>(
    arr: T[], 
    fn: function(T): U
): U[], {
    result: U[] = []
    @each(arr, @lambda(item, {
        @push(result, fn(item))
    }))
    return: result
})

# Using generics
numbers: number[] = [1, 2, 3]
strings: string[] = @map_array(numbers, @lambda(n, @string(n)))
```

### Generic Type Definitions

```tusk
# Generic container type
type Container<T> = {
    value: T
    timestamp: number
}

# Using generic type
string_container: Container<string> = {
    value: "Hello"
    timestamp: @time.now()
}

number_container: Container<number> = {
    value: 42
    timestamp: @time.now()
}
```

## Type Conversion

### Explicit Casting

```tusk
# String to number
str_value: string = "123"
num_value: number = @number(str_value)  # 123

# Number to string
num: number = 456
str: string = @string(num)  # "456"

# To boolean
bool_from_string: boolean = @boolean("true")  # true
bool_from_number: boolean = @boolean(1)        # true

# Safe casting with validation
safe_to_number = @lambda(value: any): number?, {
    @if(@isNumeric(value), {
        return: @number(value)
    }, {
        return: null
    })
})
```

### Type Coercion

```tusk
# Automatic coercion in operations
result1: string = "Number: " + 123     # "Number: 123"
result2: number = "5" * 2              # 10 (string coerced to number)

# Prevent coercion with strict typing
strict_add = @lambda(a: number, b: number): number, {
    @assert(@isNumber(a) && @isNumber(b), "Both arguments must be numbers")
    return: a + b
})
```

## Custom Types

### Type Aliases

```tusk
# Create type aliases
type ID = string | number
type Email = string
type Timestamp = number

# Using aliases
user_id: ID = "user_123"
user_email: Email = "user@example.com"
created_at: Timestamp = @time.now()
```

### Complex Type Definitions

```tusk
# Enum-like types
type Status = "active" | "inactive" | "pending" | "deleted"

# Structured types
type Address = {
    street: string
    city: string
    state: string
    zip: string
    country?: string
}

type User = {
    id: ID
    name: string
    email: Email
    status: Status
    address?: Address
    metadata?: object
}

# Nested type usage
new_user: User = {
    id: 123
    name: "John Doe"
    email: "john@example.com"
    status: "active"
    address: {
        street: "123 Main St"
        city: "Anytown"
        state: "CA"
        zip: "12345"
    }
}
```

## Type Inference Patterns

### Const Assertions

```tusk
# Literal types
STATUS_ACTIVE: "active" = "active"      # Type is literal "active", not string
PORT: 8080 = 8080                       # Type is literal 8080, not number

# Object const
CONFIG: const = {
    host: "localhost"
    port: 3000
    ssl: false
}
# Properties are readonly
```

### Type Narrowing

```tusk
# Type narrowing in conditionals
process_value = @lambda(value: string | number | null, {
    @if(value == null, {
        return: "No value"
    })
    
    # value is string | number here
    @if(@isString(value), {
        # value is string here
        return: "String: ${value.toUpperCase()}"
    })
    
    # value is number here
    return: "Number: ${value * 2}"
})
```

## Type Documentation

### JSDoc-style Comments

```tusk
###
# Calculate the total price including tax
# @param {number} price - Base price
# @param {number} tax_rate - Tax rate as decimal
# @returns {number} Total price with tax
###
calculate_total = @lambda(price: number, tax_rate: number): number, {
    return: price * (1 + tax_rate)
})

###
# User data structure
# @typedef {Object} UserData
# @property {string} name - User's full name
# @property {number} age - User's age
# @property {string[]} roles - User roles
###
```

## Type System Configuration

### Strict Mode

```tusk
# Enable strict type checking
@pragma strict_types true

# Now all variables must have types
name = "John"         # Error: missing type annotation
name: string = "John" # OK

# Function parameters must be typed
add = @lambda(a, b, a + b)                    # Error
add = @lambda(a: number, b: number, a + b)    # OK
```

### Type Checking Options

```tusk
# Configure type system behavior
@pragma {
    strict_types: true           # Require type annotations
    implicit_any: false          # Disallow implicit any type
    null_checks: true           # Strict null checking
    type_coercion: "warn"       # Warn on implicit coercion
}
```

## Best Practices

### 1. Use Types for Public APIs

```tusk
# Type public interfaces clearly
export create_user = @lambda(
    name: string, 
    email: string, 
    age?: number
): User, {
    # Implementation
})
```

### 2. Progressive Typing

```tusk
# Start loose, add types gradually
# Phase 1: No types
data = fetch_data()

# Phase 2: Basic types
data: object = fetch_data()

# Phase 3: Specific types
data: UserData[] = fetch_data()
```

### 3. Type Critical Paths

```tusk
# Type error-prone or critical code
payment_processor = @lambda(
    amount: number,
    currency: "USD" | "EUR" | "GBP",
    card: CardDetails
): PaymentResult, {
    # Typed for safety
})
```

### 4. Document Complex Types

```tusk
###
# Represents a node in the tree structure
# @type TreeNode<T>
###
type TreeNode<T> = {
    value: T
    children: TreeNode<T>[]
    parent?: TreeNode<T>
}
```

## Common Patterns

### Result Types

```tusk
# Success/Error pattern
type Result<T, E> = 
    | { success: true, data: T }
    | { success: false, error: E }

fetch_user = @lambda(id: number): Result<User, string>, {
    user = @db.find("users", id)
    
    @if(user == null, {
        return: { success: false, error: "User not found" }
    })
    
    return: { success: true, data: user }
})
```

### Builder Pattern with Types

```tusk
type QueryBuilder = {
    select: (fields: string[]) => QueryBuilder
    where: (condition: string) => QueryBuilder
    limit: (count: number) => QueryBuilder
    build: () => string
}

create_query_builder = @lambda(): QueryBuilder, {
    _parts: { select: [], where: [], limit: null }
    
    return: {
        select: @lambda(fields, {
            _parts.select = fields
            return: @self
        }),
        where: @lambda(condition, {
            @push(_parts.where, condition)
            return: @self
        }),
        limit: @lambda(count, {
            _parts.limit = count
            return: @self
        }),
        build: @lambda({
            # Build SQL query
        })
    }
})
```

## Next Steps

- Explore [References](026-references.md) for object relationships
- Learn about [Variable Naming](027-variable-naming.md)
- Master [Best Practices](030-best-practices.md) for type usage