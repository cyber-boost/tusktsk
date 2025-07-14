# Booleans in TuskLang

Booleans represent true/false values and are fundamental for conditional logic in TuskLang. This guide covers boolean values, operations, and best practices.

## Boolean Literals

### Basic Boolean Values

```tusk
# Boolean literals
is_active: true
is_disabled: false

# Case-sensitive - only lowercase works
# TRUE, True, FALSE, False are NOT valid boolean literals
```

### Boolean-like Values

```tusk
# These are strings, not booleans
not_bool1: "true"   # String
not_bool2: 'false'  # String
not_bool3: True     # Undefined variable
not_bool4: 1        # Number

# Convert strings to booleans
bool1 = @boolean("true")   # true
bool2 = @boolean("false")  # false
bool3 = @boolean("yes")    # true
bool4 = @boolean("no")     # false
```

## Boolean Operations

### Logical Operators

```tusk
a: true
b: false

# AND operator (&&)
result1 = a && b        # false
result2 = true && true  # true

# OR operator (||)
result3 = a || b        # true
result4 = false || false # false

# NOT operator (!)
result5 = !a            # false
result6 = !b            # true

# Combining operators
complex = (a || b) && !c
```

### Comparison Operators

```tusk
x: 5
y: 10

# Equality
equal = x == y          # false
not_equal = x != y      # true

# Comparison
less_than = x < y       # true
greater_than = x > y    # false
less_equal = x <= y     # true
greater_equal = x >= y  # false

# String comparison
str1: "apple"
str2: "banana"
alphabetical = str1 < str2  # true
```

## Truthiness and Falsiness

### Truthy Values

```tusk
# These evaluate to true in boolean context
truthy_values:
    non_empty_string: "hello"   # Any non-empty string
    positive_number: 42         # Any non-zero number
    negative_number: -1         # Including negative
    array: [1, 2, 3]           # Non-empty arrays
    object: { key: "value" }   # Non-empty objects

# Testing truthiness
@if("hello", "truthy", "falsy")     # "truthy"
@if(42, "truthy", "falsy")           # "truthy"
@if([1], "truthy", "falsy")          # "truthy"
```

### Falsy Values

```tusk
# These evaluate to false in boolean context
falsy_values:
    boolean_false: false
    null_value: null
    zero: 0
    empty_string: ""
    empty_array: []
    empty_object: {}

# Testing falsiness
@if(false, "truthy", "falsy")       # "falsy"
@if(null, "truthy", "falsy")        # "falsy"
@if(0, "truthy", "falsy")           # "falsy"
@if("", "truthy", "falsy")          # "falsy"
```

## Boolean Conversion

### Explicit Conversion

```tusk
# Convert various types to boolean
bool_from_string = @boolean("yes")     # true
bool_from_number = @boolean(1)          # true
bool_from_null = @boolean(null)         # false

# String conversion rules
string_bools:
    from_true: @boolean("true")         # true
    from_yes: @boolean("yes")           # true
    from_1: @boolean("1")               # true
    from_on: @boolean("on")             # true
    
    from_false: @boolean("false")       # false
    from_no: @boolean("no")             # false
    from_0: @boolean("0")               # false
    from_off: @boolean("off")           # false
    
    from_other: @boolean("hello")       # true (non-empty)
```

### Implicit Conversion

```tusk
# Automatic conversion in boolean context
value: "hello"

# If statement
@if(value, {
    # Executes because "hello" is truthy
    result: "Value is truthy"
})

# Logical operations force boolean context
has_value = value && true    # true
is_empty = !value           # false
```

## Conditional Logic

### If Expressions

```tusk
# Basic if expression
age: 18
can_vote = @if(age >= 18, true, false)

# Without explicit boolean
status = @if(user.active, "Active", "Inactive")

# Nested conditions
category = @if(age < 13, "child",
           @if(age < 20, "teen",
           @if(age < 60, "adult", "senior")))
```

### Boolean Guards

```tusk
# Early return pattern
validate_user = @lambda(user, {
    # Guard clauses
    @if(!user, return: false)
    @if(!user.email, return: false)
    @if(!user.active, return: false)
    
    # All checks passed
    return: true
})

# Chained validations
is_valid = user && user.email && user.active
```

## Short-Circuit Evaluation

### AND Short-Circuit

```tusk
# AND stops at first false
result = false && @expensive_operation()  # expensive_operation never runs

# Practical use
user && user.profile && user.profile.settings

# Safe property access
has_permission = user && user.roles && user.roles.includes("admin")
```

### OR Short-Circuit

```tusk
# OR stops at first true
result = true || @expensive_operation()  # expensive_operation never runs

# Default values using OR
port = @env.PORT || 8080
name = user.name || "Anonymous"

# First truthy value wins
display_name = user.nickname || user.fullname || user.email || "User"
```

## Boolean Arrays

### Array of Booleans

```tusk
# Define boolean array
permissions: [true, false, true, true, false]

# Check if all true
all_granted = @all(permissions)        # false

# Check if any true
some_granted = @any(permissions)       # true

# Count true values
granted_count = @filter(permissions, @lambda(p, p)).length
```

### Boolean Mapping

```tusk
# Transform to booleans
ages: [15, 18, 21, 16, 25]
can_vote = @map(ages, @lambda(age, age >= 18))
# Result: [false, true, true, false, true]

# Filter based on boolean condition
adults = @filter(ages, @lambda(age, age >= 18))
# Result: [18, 21, 25]
```

## Complex Boolean Logic

### Boolean Algebra

```tusk
# De Morgan's Laws
a: true
b: false

# !(a && b) == !a || !b
law1_left = !(a && b)
law1_right = !a || !b
law1_valid = law1_left == law1_right  # true

# !(a || b) == !a && !b
law2_left = !(a || b)
law2_right = !a && !b
law2_valid = law2_left == law2_right  # true
```

### Boolean Functions

```tusk
# XOR (exclusive OR)
xor = @lambda(a, b, (a || b) && !(a && b))

# NAND
nand = @lambda(a, b, !(a && b))

# NOR
nor = @lambda(a, b, !(a || b))

# Usage
result1 = @xor(true, false)   # true
result2 = @xor(true, true)    # false
```

## Boolean Flags and Configuration

### Feature Flags

```tusk
features:
    new_ui: @env.FEATURE_NEW_UI == "true"
    beta_api: @env.FEATURE_BETA_API == "true"
    analytics: true
    debug_mode: @env.NODE_ENV != "production"

# Conditional feature loading
@if(features.analytics, {
    @import("analytics.tsk")
})
```

### Configuration Validation

```tusk
config:
    ssl_enabled: true
    ssl_cert: "/path/to/cert"
    ssl_key: "/path/to/key"

# Validate boolean dependencies
validate_ssl = @lambda({
    @if(config.ssl_enabled, {
        @assert(config.ssl_cert, "SSL cert required when SSL enabled")
        @assert(config.ssl_key, "SSL key required when SSL enabled")
    })
})
```

## Boolean Patterns

### Toggle Pattern

```tusk
# Simple toggle
is_visible: true
toggle_visibility = @lambda({
    is_visible = !is_visible
})

# State-based toggle
state:
    dark_mode: false
    
toggle_dark_mode = @lambda({
    state.dark_mode = !state.dark_mode
    @emit("theme_changed", state.dark_mode)
})
```

### Boolean Accumulator

```tusk
# Check multiple conditions
validations: {
    has_name: user.name != ""
    has_email: user.email != ""
    valid_email: @regex.match(user.email, "^[^@]+@[^@]+$")
    has_password: user.password.length >= 8
}

# All must be true
is_valid = @all(@values(validations))

# Get failed validations
failed = @filter(@entries(validations), @lambda(entry, !entry[1]))
```

### Permission Checking

```tusk
# Role-based permissions
user:
    roles: ["user", "editor"]

permissions:
    read: true
    write: @user.roles.includes("editor") || @user.roles.includes("admin")
    delete: @user.roles.includes("admin")
    
can_perform = @lambda(action, @permissions[action] || false)
```

## Error Handling with Booleans

### Success/Failure Pattern

```tusk
# Operation result
result: {
    success: false
    data: null
    error: "Connection failed"
}

# Check and handle
@if(result.success, {
    # Handle success
    @process(result.data)
}, {
    # Handle failure
    @log.error(result.error)
})
```

### Validation Results

```tusk
validate_input = @lambda(input, {
    errors: []
    
    @if(!input.name, {
        errors.push("Name is required")
    })
    
    @if(!input.email || !@validate.email(input.email), {
        errors.push("Valid email is required")
    })
    
    return: {
        valid: @len(errors) == 0
        errors: errors
    }
})
```

## Performance Considerations

### Lazy Evaluation

```tusk
# Expensive operations with booleans
should_process: false

# This won't execute expensive_check if should_process is false
result = should_process && @expensive_check()

# Better than
@if(should_process, {
    result = @expensive_check()
})
```

### Boolean Caching

```tusk
# Cache expensive boolean calculations
_is_premium_cached: null

is_premium_user = @lambda({
    @if(_is_premium_cached != null, {
        return: _is_premium_cached
    })
    
    # Expensive check
    _is_premium_cached = @database.check_premium_status(user.id)
    return: _is_premium_cached
})
```

## Best Practices

1. **Use explicit boolean values** - Avoid relying on truthiness when clarity matters
2. **Name boolean variables clearly** - Use `is_`, `has_`, `can_` prefixes
3. **Leverage short-circuit evaluation** for performance
4. **Validate boolean conversions** from user input
5. **Document complex boolean logic** with comments
6. **Use consistent patterns** for similar checks
7. **Avoid double negatives** - `!is_not_valid` is confusing

## Common Mistakes

### String vs Boolean

```tusk
# Wrong
is_active: "true"  # This is a string
@if(is_active == true, ...)  # Will be false!

# Right
is_active: true
# Or
is_active = @boolean("true")
```

### Unnecessary Boolean Conversion

```tusk
# Redundant
is_valid = @if(value > 0, true, false)

# Better
is_valid = value > 0
```

### Forgetting Short-Circuit

```tusk
# Might cause error if user is null
has_name = user.name != ""  # Error if user is null

# Safe version
has_name = user && user.name != ""
```

## Next Steps

- Learn about [Null Values](017-null-values.md) and handling absence
- Explore [Arrays](018-arrays.md) for collections
- Master [Conditional Logic](031-at-operator-intro.md) with @ operators