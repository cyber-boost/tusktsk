# Reserved Keywords in TuskLang

TuskLang reserves certain keywords that have special meaning in the language. These words cannot be used as variable names, function names, or identifiers. This guide covers all reserved keywords and how to work around them when needed.

## Core Reserved Keywords

### Boolean Literals

```tusk
# Reserved boolean values
true   # Boolean true literal
false  # Boolean false literal

# These are reserved and cannot be used as variable names
# true: "some value"   # ERROR
# false: "some value"  # ERROR

# Workarounds
is_true: true
false_value: false
true_flag: true
```

### Null Literal

```tusk
# Reserved null value
null   # Represents absence of value

# Cannot use as variable name
# null: "some value"   # ERROR

# Workarounds
null_value: null
is_null: true
empty: null
```

### Type Keywords

```tusk
# Reserved type names
string    # String type
number    # Number type
boolean   # Boolean type
object    # Object type
array     # Array type
function  # Function type
any       # Any type

# Cannot use as variable names
# string: "text"      # ERROR
# number: 42          # ERROR

# Workarounds
string_value: "text"
number_type: "number"
my_string: "text"
```

## Control Flow Keywords

### Conditional Keywords

```tusk
# Reserved for future control flow
if      # Used with @if
else    # Used with @if
switch  # Used with @switch
case    # Used with @switch
default # Used with @switch

# These may be reserved for future syntax
# if: "condition"     # ERROR
# else: "alternative" # ERROR

# Current usage with @ operator
result = @if(condition, true_value, false_value)
```

### Loop Keywords

```tusk
# Reserved for loops
for     # For loops
while   # While loops
do      # Do-while loops
break   # Break statement
continue # Continue statement

# Cannot use as variables
# for: "iterator"    # ERROR
# while: true        # ERROR

# Current loop usage
@for(i in range(10), {
    # Loop body
})
```

## Function Keywords

### Function Definition

```tusk
# Reserved for functions
function  # Function declaration
return    # Return statement
lambda    # Lambda expression

# Cannot use as variables
# function: "my function"  # ERROR
# return: "value"         # ERROR

# Current function usage
my_function: @lambda(param, {
    return: param * 2
})
```

### Special Function Keywords

```tusk
# Reserved for special behaviors
self     # Self reference
parent   # Parent reference
this     # This context
super    # Super reference

# Usage in objects
object: {
    value: 10
    method: @lambda({
        return: @self.value  # References object.value
    })
}
```

## Declaration Keywords

### Variable Declaration

```tusk
# Reserved for declarations
var    # Variable declaration
let    # Block-scoped variable
const  # Constant declaration
type   # Type declaration

# May be reserved for future syntax
# var: "variable"   # ERROR
# const: "constant" # ERROR

# Current declaration style
my_var: "value"
MY_CONST: "constant value"
```

## Import/Export Keywords

### Module Keywords

```tusk
# Reserved for modules
import   # Import statement
export   # Export statement
from     # Import from
as       # Import/export alias
module   # Module declaration

# Cannot use as variables
# import: "data"    # ERROR
# export: "data"    # ERROR

# Current import syntax
data: @import("./data.tsk")
utils: @import("./utils.tsk") as utilities
```

## Operator Keywords

### Logical Operators

```tusk
# Reserved operators
and      # Logical AND (use &&)
or       # Logical OR (use ||)
not      # Logical NOT (use !)
in       # In operator
of       # Of operator

# Cannot use as variables
# and: true        # ERROR
# or: false        # ERROR

# Use symbols instead
result = a && b  # AND
result = a || b  # OR
result = !a      # NOT
```

### Comparison Keywords

```tusk
# Reserved for comparisons
is       # Identity check
typeof   # Type check
instanceof # Instance check

# May be reserved
# is: "comparison"  # ERROR

# Current usage
type_check = @typeof(value)
```

## Special Keywords

### Async Keywords

```tusk
# Reserved for async operations
async    # Async function
await    # Await expression
promise  # Promise type

# Reserved for future async support
# async: "operation"  # ERROR
# await: "result"     # ERROR
```

### Error Handling

```tusk
# Reserved for error handling
try      # Try block
catch    # Catch block
finally  # Finally block
throw    # Throw statement

# Cannot use as variables
# try: "attempt"     # ERROR
# catch: "error"     # ERROR

# Current error handling
result = @try({
    # Try block
}, {
    # Catch block
})
```

## Class-Related Keywords

### OOP Keywords

```tusk
# Reserved for object-oriented features
class      # Class declaration
extends    # Class inheritance
implements # Interface implementation
interface  # Interface declaration
abstract   # Abstract class/method
static     # Static member
private    # Private member
public     # Public member
protected  # Protected member

# These are reserved
# class: "MyClass"    # ERROR
# private: "data"     # ERROR
```

## Working with Reserved Keywords

### Naming Strategies

```tusk
# When you need similar names, use prefixes/suffixes

# Instead of 'class'
class_name: "User"
user_class: "User"
className: "User"

# Instead of 'type'
type_name: "string"
data_type: "string"
typeOf: "string"

# Instead of 'return'
return_value: 42
returns: 42
returned: 42
```

### Context-Specific Alternatives

```tusk
# For configuration that might clash

# Instead of 'default'
default_value: "none"
defaults: {
    timeout: 30
}
fallback: "default"

# Instead of 'public'
public_key: "xyz123"
is_public: true
visibility: "public"
```

### Escaping (If Supported)

```tusk
# Some languages allow escaping reserved words
# TuskLang may support this in the future
# `class`: "MyClass"  # Hypothetical escaped keyword
# "class": "MyClass"  # Using quotes as keys
```

## Reserved Prefixes

### @ Operator Prefix

```tusk
# The @ symbol is reserved for operators
@if()       # Conditional operator
@env        # Environment access
@time       # Time functions
@lambda()   # Lambda functions

# Cannot create variables starting with @
# @myvar: "value"    # ERROR

# Use without @
myvar: "value"
```

### System Prefixes

```tusk
# Double underscore may be reserved
__proto__    # Prototype chain
__internal__ # Internal use

# Single underscore is fine
_private: "value"
_internal: "data"
```

## Future Reserved Keywords

### Potential Future Keywords

```tusk
# These might be reserved in future versions
namespace   # Namespace declaration
package     # Package declaration
yield       # Generator yield
enum        # Enumeration
sealed      # Sealed class
readonly    # Readonly property
override    # Method override
virtual     # Virtual method

# Avoid using these as variable names
# namespace: "my.namespace"  # Might break in future
```

## Best Practices

### 1. Check Documentation

```tusk
# Always refer to the latest documentation
# Reserved keywords may change between versions

# Safe approach - use descriptive names
configuration: { }      # Instead of 'config'
authentication: { }     # Instead of 'auth'
```

### 2. Use Linters

```tusk
# TuskLang linters will warn about reserved keywords
# Run: tusk lint myfile.tsk

# Linter will catch:
# - Reserved keyword usage
# - Potential conflicts
# - Future deprecations
```

### 3. Naming Conventions

```tusk
# Follow conventions to avoid conflicts

# Use prefixes for clarity
my_class: "User"
my_type: "string"
my_default: "value"

# Use domain-specific terms
user_type: "admin"
default_role: "guest"
return_code: 200
```

### 4. Error Messages

```tusk
# TuskLang provides clear error messages
# Error: Cannot use reserved keyword 'true' as identifier
# Line 10: true: "some value"
#          ^^^^

# The error will suggest alternatives
```

## Common Mistakes

### Using Reserved Words

```tusk
# Common mistakes to avoid

# Wrong
# true: "yes"
# false: "no"
# null: "empty"

# Right
is_true: "yes"
is_false: "no"
is_null: "empty"
```

### Forgetting Context

```tusk
# Keywords might be valid in some contexts

# As object property (if allowed)
config: {
    "default": "value"  # May work with quotes
}

# As string value (always works)
keyword: "class"  # Fine as string value
```

## Quick Reference

### Always Reserved

- `true`, `false`, `null`
- Basic types: `string`, `number`, `boolean`, `object`, `array`, `function`
- May be reserved: `if`, `else`, `for`, `while`, `return`

### Context-Sensitive

- `@` prefix - reserved for operators
- `__` prefix - may be reserved for internals
- Future keywords - check documentation

### Safe Practices

1. Use descriptive names
2. Add prefixes/suffixes when needed
3. Check linter warnings
4. Keep up with language updates

## Next Steps

- Learn about [Syntax Errors](029-syntax-errors.md)
- Review [Best Practices](030-best-practices.md)
- Explore [@ Operators](031-at-operator-intro.md)