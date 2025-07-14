# Syntax Errors in TuskLang

Understanding and fixing syntax errors is crucial for writing valid TuskLang code. This guide covers common syntax errors, how to identify them, and how to fix them.

## Common Syntax Errors

### Missing Colons or Equals

```tusk
# ERROR: Missing assignment operator
name "John"          # Error: Expected ':' or '='

# CORRECT: Use colon for static values
name: "John"

# ERROR: Wrong operator for dynamic values  
timestamp: @time.now()  # Error: @ operators need '='

# CORRECT: Use equals for dynamic values
timestamp = @time.now()
```

### Incorrect Indentation

```tusk
# ERROR: Inconsistent indentation
server:
    host: "localhost"
  port: 8080        # Error: Inconsistent indentation

# CORRECT: Consistent indentation
server:
    host: "localhost"
    port: 8080

# ERROR: Mixed tabs and spaces
config:
    name: "app"     # Spaces
	version: "1.0"  # Tab - Error!

# CORRECT: Use either spaces OR tabs consistently
config:
    name: "app"
    version: "1.0"
```

### Unterminated Strings

```tusk
# ERROR: Missing closing quote
message: "Hello World    # Error: Unterminated string

# CORRECT: Close the string
message: "Hello World"

# ERROR: Mismatched quotes
greeting: "Hello'        # Error: Started with " but ended with '

# CORRECT: Match quote types
greeting: "Hello"
greeting: 'Hello'

# ERROR: Unterminated heredoc
text: """
This is a heredoc
without closing delimiter

# CORRECT: Close heredoc
text: """
This is a heredoc
with proper closing
"""
```

### Invalid Array Syntax

```tusk
# ERROR: Missing commas
numbers: [1 2 3]        # Error: Missing commas

# CORRECT: Use commas
numbers: [1, 2, 3]

# ERROR: Trailing comma (if not supported)
items: [
    "a",
    "b",
    "c",                # May cause error in some versions
]

# CORRECT: Remove trailing comma or check if supported
items: [
    "a",
    "b",
    "c"
]

# ERROR: Mixed syntax
data: [1, 2, {key: value}]  # Error: 'value' is undefined

# CORRECT: Quote strings or reference properly
data: [1, 2, {key: "value"}]
```

## Object Syntax Errors

### Missing Commas in Objects

```tusk
# ERROR: Missing comma between properties
user: {
    name: "John"
    age: 30         # Error: Missing comma after previous property
}

# CORRECT: Add commas
user: {
    name: "John",
    age: 30
}

# ERROR: Comma after last property (inline)
config: { host: "localhost", port: 8080, }  # May error

# CORRECT: Remove trailing comma in inline objects
config: { host: "localhost", port: 8080 }
```

### Invalid Property Names

```tusk
# ERROR: Unquoted property with special characters
headers: {
    Content-Type: "application/json"  # Error: '-' not allowed
}

# CORRECT: Quote special property names
headers: {
    "Content-Type": "application/json"
}

# ERROR: Starting with number
data: {
    1st: "first"    # Error: Property can't start with number
}

# CORRECT: Quote or rename
data: {
    "1st": "first"  # Or use 'first' as key
}
```

## Function and Expression Errors

### Invalid @ Operator Usage

```tusk
# ERROR: @ operator with colon
result: @compute_value()  # Error: @ requires '='

# CORRECT: Use equals with @ operators
result = @compute_value()

# ERROR: Missing @ for operator calls
current_time = time.now()  # Error: 'time' is not defined

# CORRECT: Use @ prefix
current_time = @time.now()

# ERROR: Space after @
value = @ env.PORT  # Error: No space after @

# CORRECT: No space after @
value = @env.PORT
```

### Lambda Syntax Errors

```tusk
# ERROR: Missing arrow or braces
add = @lambda(a, b, a + b)  # Error in some contexts

# CORRECT: Use proper lambda syntax
add = @lambda(a, b, {
    return: a + b
})

# ERROR: Missing parameter parentheses
greet = @lambda name, {     # Error: Need parentheses
    return: "Hello, ${name}"
})

# CORRECT: Include parentheses
greet = @lambda(name, {
    return: "Hello, ${name}"
})
```

## Reference Errors

### Invalid References

```tusk
# ERROR: Reference to undefined variable
value: @undefined_variable  # Error: undefined_variable not found

# CORRECT: Define before referencing
defined_variable: "value"
value: @defined_variable

# ERROR: Deep reference to non-existent path
data: @config.database.primary.host  # Error if path doesn't exist

# CORRECT: Use optional chaining or check existence
data: @config?.database?.primary?.host ?? "localhost"
```

### Circular References

```tusk
# ERROR: Direct circular reference
a: @b
b: @a  # Error: Circular reference detected

# CORRECT: Break the cycle
a: "value"
b: @a  # Now b references a's value

# ERROR: Indirect circular reference
x: @y.value
y: { value: @z.value }
z: { value: @x }  # Error: Circular through chain

# CORRECT: Use functions for lazy evaluation
x = @lambda({ @y.value })
y: { value: @lambda({ @z.value }) }
z: { value: "actual value" }
```

## Type-Related Errors

### Type Mismatches

```tusk
# ERROR: Assigning wrong type with strict typing
age: string = 30  # Error: Expected string, got number

# CORRECT: Match types or convert
age: string = "30"
# Or
age: number = 30

# ERROR: Array type mismatch
numbers: number[] = [1, "2", 3]  # Error: "2" is not a number

# CORRECT: Ensure all elements match type
numbers: number[] = [1, 2, 3]
# Or convert
numbers: number[] = [1, @number("2"), 3]
```

## Comment Errors

### Incorrect Comment Syntax

```tusk
# CORRECT: Single line comment
name: "John"  # This is a comment

// ERROR: C-style comments not supported
// This won't work as a comment

/* ERROR: Block comments not supported
   This style is not valid */

# CORRECT: Use # for all comments
# This is a comment
# This is another comment

### Documentation comments use ###
# User model documentation
###
```

## String Interpolation Errors

### Invalid Interpolation Syntax

```tusk
# ERROR: Wrong interpolation syntax
message: "Hello $name"       # Error: Missing braces
message: "Hello {name}"      # Error: Missing $
message: "Hello $(name)"     # Error: Wrong syntax

# CORRECT: Use ${} for interpolation
name: "World"
message: "Hello ${name}"

# ERROR: Interpolation in single quotes
text: 'Hello ${name}'  # Won't interpolate

# CORRECT: Use double quotes for interpolation
text: "Hello ${name}"
```

## Error Messages and Debugging

### Understanding Error Messages

```tusk
# Typical error message format:
# Error: Unexpected token '}' at line 15, column 8
# File: config.tsk
# Line 15: server: { host: "localhost" }}
#                                        ^

# The error points to the exact location
```

### Common Error Patterns

```tusk
# "Unexpected token" - Usually syntax error
server: {
    host: "localhost"
    }  # Error: Expected property or '}'

# "Undefined variable" - Reference error
value: @nonexistent  # Error: 'nonexistent' is not defined

# "Type mismatch" - Type error
count: string = 42  # Error: Expected string, got number

# "Invalid indentation" - Formatting error
data:
  item1: "value"
    item2: "value"  # Error: Inconsistent indentation
```

## Prevention Strategies

### Use a Linter

```bash
# Run TuskLang linter
tusk lint myfile.tsk

# Linter catches:
# - Syntax errors
# - Undefined variables
# - Type mismatches
# - Style violations
```

### Format Your Code

```bash
# Auto-format to fix indentation
tusk format myfile.tsk

# Before:
server:{host:"localhost",port:8080}

# After:
server: {
    host: "localhost",
    port: 8080
}
```

### Development Tools

```tusk
# Use IDE/editor with TuskLang support
# Features to look for:
# - Syntax highlighting
# - Real-time error detection
# - Auto-completion
# - Format on save

# VS Code settings
{
    "[tusklang]": {
        "editor.formatOnSave": true,
        "editor.tabSize": 4
    }
}
```

## Best Practices for Error Prevention

### 1. Consistent Style

```tusk
# Pick a style and stick to it
# Good: Consistent throughout
config:
    server:
        host: "localhost"
        port: 8080
    database:
        host: "db.local"
        port: 5432
```

### 2. Validate Early

```tusk
# Validate inputs early
process_user = @lambda(data, {
    # Validate first
    @assert(data != null, "Data cannot be null")
    @assert(data.name, "Name is required")
    @assert(@isNumber(data.age), "Age must be a number")
    
    # Then process
    # ...
})
```

### 3. Use Type Hints

```tusk
# Add type hints for clarity
create_user = @lambda(
    name: string,
    age: number,
    roles: string[]
): User, {
    # Implementation
})
```

### 4. Test Incrementally

```tusk
# Test as you write
# Step 1: Basic structure
config:
    name: "test"

# Step 2: Add nested data
config:
    name: "test"
    server:
        host: "localhost"

# Step 3: Add dynamic values
config:
    name: "test"
    server:
        host: "localhost"
        port = @env.PORT || 8080
```

## Recovery Strategies

### Debugging Steps

1. **Read the error message** - It usually points to the exact issue
2. **Check the line number** - Look at the specific line and surrounding context
3. **Verify syntax** - Ensure proper operators (:, =), quotes, commas
4. **Check indentation** - Use consistent spaces or tabs
5. **Validate references** - Ensure all referenced variables exist
6. **Test in isolation** - Extract problematic code and test separately

### Common Fixes

```tusk
# For "Unexpected token" - Check for missing commas, quotes, or brackets
# For "Undefined variable" - Ensure variable is defined before use
# For "Invalid indentation" - Reformat with consistent spacing
# For "Type mismatch" - Convert values or fix type annotations
# For "Circular reference" - Restructure to break the cycle
```

## Next Steps

- Review [Best Practices](030-best-practices.md)
- Learn about [@ Operators](031-at-operator-intro.md)
- Master [Error Handling](058-at-operator-errors.md)