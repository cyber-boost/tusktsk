# Comparison Operators in TuskLang

Comparison operators are used to compare values and return boolean results, essential for conditional logic and data validation.

## Equality Operators

```tusk
# Equality (==)
# Compares values with type coercion
5 == "5"        # true (number == string)
true == 1       # true (boolean == number)
null == undefined # true

# Strict equality (===)
# Compares values without type coercion
5 === "5"       # false (different types)
true === 1      # false (different types)
null === undefined # false (different types)

# Inequality (!=)
5 != "6"        # true
"hello" != "world" # true

# Strict inequality (!==)
5 !== "5"       # true (different types)
5 !== 5         # false (same value and type)
```

## Relational Operators

```tusk
# Greater than (>)
10 > 5          # true
"b" > "a"       # true (lexicographic)
"10" > "2"      # false (string comparison)

# Less than (<)
3 < 7           # true
"apple" < "banana" # true

# Greater than or equal (>=)
5 >= 5          # true
5 >= 3          # true
5 >= 7          # false

# Less than or equal (<=)
3 <= 3          # true
3 <= 5          # true
7 <= 5          # false

# String comparisons
"abc" < "abd"   # true (character by character)
"ABC" < "abc"   # true (uppercase before lowercase)
"10" < "9"      # true (string comparison, not numeric)
```

## Type Checking Operators

```tusk
# typeof operator
typeof 42           # "number"
typeof "hello"      # "string"
typeof true         # "boolean"
typeof {}           # "object"
typeof []           # "array" (TuskLang enhancement)
typeof null         # "null" (TuskLang enhancement)
typeof undefined    # "undefined"
typeof (() => {})   # "function"

# instanceof operator
date: new Date()
date instanceof Date    # true

array: [1, 2, 3]
array instanceof Array  # true

# Custom type checking
is_string(value)    # true if string
is_number(value)    # true if number
is_array(value)     # true if array
is_object(value)    # true if object (not array)
is_function(value)  # true if function
is_null(value)      # true if null
is_undefined(value) # true if undefined
is_boolean(value)   # true if boolean
```

## Pattern Matching Comparisons

```tusk
# in operator (check property existence)
"name" in user      # true if user has name property
"length" in array   # true (arrays have length)

# Array/String inclusion
"hello" in ["hello", "world"]  # true
"ell" in "hello"               # true (substring)

# Pattern matching with match
result: match value {
    0 => "zero"
    1..10 => "low"
    11..100 => "medium"
    _ => "high"
}

# Range comparisons
age in 18..65      # true if age between 18 and 65
score in [90, 95, 100]  # true if score is one of these values
```

## Comparing Different Types

```tusk
# Number comparisons
42 > 30         # true
3.14 < 5        # true
-10 < 0         # true
Infinity > 1000 # true

# String comparisons (lexicographic)
"apple" < "banana"  # true
"Apple" < "apple"   # true (capitals first)
"100" < "20"        # true (string comparison)

# Boolean comparisons
true > false    # true (true = 1, false = 0)
true == 1       # true
false == 0      # true

# Mixed type comparisons (with coercion)
"5" > 3         # true (string converted to number)
true < 2        # true (true = 1)
"" == 0         # true (empty string = 0)
```

## Object and Array Comparisons

```tusk
# Reference equality
obj1: {name: "John"}
obj2: {name: "John"}
obj3: obj1

obj1 == obj2    # false (different objects)
obj1 === obj2   # false (different objects)
obj1 === obj3   # true (same reference)

# Array comparison
[1, 2] == [1, 2]    # false (different arrays)
arr1: [1, 2]
arr2: arr1
arr1 === arr2       # true (same reference)

# Deep comparison functions
deep_equal(obj1, obj2)  # true if same structure/values
arrays_equal([1, 2], [1, 2])  # true

# Custom comparison
users.sort((a, b) => a.age - b.age)  # Numeric comparison
items.sort((a, b) => a.name.localeCompare(b.name))  # String comparison
```

## Null and Undefined Comparisons

```tusk
# Null comparisons
null == null        # true
null === null       # true
null == undefined   # true (special case)
null === undefined  # false

# Undefined comparisons
undefined == undefined  # true
undefined === undefined # true

# Checking for null or undefined
value == null       # true if null OR undefined
value === null      # true only if null
value === undefined # true only if undefined

# Nullish coalescing
result: value ?? "default"  # Use default if null/undefined

# Optional chaining
length: user?.name?.length  # Safe navigation
```

## Special Value Comparisons

```tusk
# NaN comparisons
NaN == NaN      # false (NaN is never equal to anything)
NaN === NaN     # false
isNaN(NaN)      # true (use this to check for NaN)
Number.isNaN(NaN) # true (stricter check)

# Infinity comparisons
Infinity > 1000000  # true
-Infinity < -1000000 # true
Infinity == Infinity # true

# Zero comparisons
0 == -0         # true
0 === -0        # true
Object.is(0, -0) # false (distinguishes +0 and -0)
```

## Comparison Functions

```tusk
# Numeric comparison function
compare_numbers: (a, b) => {
    if (a < b) return -1
    if (a > b) return 1
    return 0
}

# String comparison (case-insensitive)
compare_strings_ci: (a, b) => {
    return a.toLowerCase().localeCompare(b.toLowerCase())
}

# Date comparison
compare_dates: (d1, d2) => {
    return d1.getTime() - d2.getTime()
}

# Complex object comparison
compare_by: (property) => {
    return (a, b) => {
        if (a[property] < b[property]) return -1
        if (a[property] > b[property]) return 1
        return 0
    }
}

# Usage
users.sort(compare_by("age"))
items.sort(compare_by("price"))
```

## Chained Comparisons

```tusk
# Multiple comparisons
if (0 <= value && value <= 100) {
    # Value is between 0 and 100
}

# Validation chains
is_valid: value != null && 
          value != undefined && 
          value !== "" && 
          value.length > 0

# Range checking
in_range: (value, min, max) => {
    return min <= value && value <= max
}

# Between helper
between: (value, a, b) => {
    return value >= Math.min(a, b) && value <= Math.max(a, b)
}
```

## Performance Tips

```tusk
# Use strict equality when possible
# Faster - no type coercion
if (x === 5) { }

# Slower - requires type coercion
if (x == 5) { }

# Order comparisons efficiently
# Check most likely to fail first
if (expensive_check() && cheap_check()) { }  # Bad
if (cheap_check() && expensive_check()) { }  # Good

# Cache comparison results
is_valid: null
if (is_valid === null) {
    is_valid = complex_validation()
}
```

## Common Patterns

```tusk
# Safe string comparison
safe_compare: (a, b) => {
    return String(a).toLowerCase() === String(b).toLowerCase()
}

# Fuzzy equality (with tolerance)
fuzzy_equal: (a, b, tolerance: 0.0001) => {
    return Math.abs(a - b) < tolerance
}

# Version comparison
compare_versions: (v1, v2) => {
    parts1: v1.split('.').map(Number)
    parts2: v2.split('.').map(Number)
    
    for (i: 0; i < Math.max(parts1.length, parts2.length); i++) {
        p1: parts1[i] || 0
        p2: parts2[i] || 0
        
        if (p1 < p2) return -1
        if (p1 > p2) return 1
    }
    return 0
}

# Truthy/Falsy checks
is_truthy: !!value
is_falsy: !value
```

## Comparison Pitfalls

```tusk
# String number comparison gotcha
"10" < "9"      # true (string comparison)
10 < 9          # false (numeric comparison)

# Object comparison gotcha
{a: 1} == {a: 1}    # false (different objects)

# Array comparison gotcha
[1, 2] == [1, 2]    # false (different arrays)

# NaN comparison gotcha
value == NaN        # always false
value === NaN       # always false
isNaN(value)        # use this instead

# Type coercion surprises
[] == false         # true
[] == 0            # true
"" == false        # true
" " == false       # false (non-empty string)
```

## Best Practices

1. **Use strict equality by default** - Avoid type coercion surprises
2. **Be explicit about type conversions** - Convert before comparing
3. **Use specialized functions for complex comparisons** - Don't rely on operators
4. **Handle null/undefined explicitly** - Avoid unexpected behavior
5. **Test edge cases** - Empty strings, zero, null, undefined
6. **Use descriptive comparison functions** - Make intent clear
7. **Consider locale for string comparisons** - Use localeCompare()
8. **Document non-obvious comparisons** - Explain why

## Related Topics

- `logical-operators` - &&, ||, !
- `ternary-operators` - Conditional expressions
- `type-checking` - Type validation
- `null-handling` - Null/undefined handling
- `equality-algorithms` - Deep equality