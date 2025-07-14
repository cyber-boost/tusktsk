# Ternary Operators in TuskLang

Ternary operators provide concise conditional expressions for simple if-else logic, making code more readable and compact.

## Basic Ternary Syntax

```tusk
# Basic ternary operator
result: condition ? true_value : false_value

# Simple example
age: 25
status: age >= 18 ? "adult" : "minor"

# With variables
is_logged_in: true
message: is_logged_in ? "Welcome back!" : "Please log in"

# With expressions
score: 85
grade: score >= 90 ? "A" : score >= 80 ? "B" : "C"
```

## Nested Ternary Operators

```tusk
# Multiple conditions
score: 75
grade: score >= 90 ? "A" :
       score >= 80 ? "B" :
       score >= 70 ? "C" :
       score >= 60 ? "D" : "F"

# Formatted for readability
user_type: user.is_admin ? "Administrator" :
           user.is_moderator ? "Moderator" :
           user.is_premium ? "Premium User" :
           "Standard User"

# Complex nested example
discount: customer.years > 5 ? 0.20 :
          customer.years > 2 ? 0.10 :
          customer.premium ? 0.05 : 0
```

## Ternary with Functions

```tusk
# Function calls in ternary
result: is_valid(input) ? process(input) : handle_error()

# Method calls
output: user.active ? user.get_full_profile() : user.get_basic_info()

# Lazy evaluation
data: use_cache ? get_cached_data() : fetch_fresh_data()

# With closures
handler: is_async ? 
    () => handle_async(request) : 
    () => handle_sync(request)
```

## Ternary in Assignments

```tusk
# Variable assignment
username: user?.name ? user.name : "Guest"

# Property assignment
config: {
    theme: user_preference ? user_preference : "default"
    language: detected_lang ? detected_lang : "en"
    timezone: user.timezone ? user.timezone : "UTC"
}

# Array elements
statuses: [
    order.shipped ? "Shipped" : "Pending",
    payment.confirmed ? "Paid" : "Awaiting Payment",
    stock > 0 ? "In Stock" : "Out of Stock"
]

# Dynamic keys
response: {
    (success ? "data" : "error"): result
}
```

## Ternary with Null Coalescing

```tusk
# Null coalescing operator (??)
name: user.name ?? "Unknown"

# Combined with ternary
display_name: user.nickname ? user.nickname : (user.name ?? "User")

# Multiple fallbacks
theme: user.theme ?? settings.default_theme ?? "light"

# With type checking
value: is_string(input) ? input : (input?.toString() ?? "")
```

## Ternary in Templates

```tusk
# In string templates
greeting: `Hello, ${user.name ? user.name : "Guest"}!`

# HTML generation
html: """
<div class="${active ? 'active' : 'inactive'}">
    ${user.premium ? '<span class="premium-badge">PRO</span>' : ''}
    <h3>${title ? title : 'Untitled'}</h3>
</div>
"""

# Template literals
message: `You have ${count} ${count == 1 ? 'item' : 'items'} in your cart`

# CSS classes
class_name: `
    btn
    ${primary ? 'btn-primary' : 'btn-secondary'}
    ${large ? 'btn-lg' : ''}
    ${disabled ? 'disabled' : ''}
`.trim()
```

## Ternary in Function Returns

```tusk
# Simple return
get_price: (user) => {
    return user.premium ? price * 0.8 : price
}

# Arrow function with ternary
calculate_tax: (amount, location) => 
    location == "NY" ? amount * 0.08 : amount * 0.05

# Multiple return paths
validate: (value) => {
    return !value ? {error: "Required"} :
           value.length < 3 ? {error: "Too short"} :
           value.length > 50 ? {error: "Too long"} :
           {valid: true}
}

# Conditional return types
fetch_data: (use_mock) => 
    use_mock ? 
        Promise.resolve(mock_data) : 
        http.get("/api/data")
```

## Ternary in Loops

```tusk
# In array methods
numbers: [1, 2, 3, 4, 5]
labels: numbers.map(n => n % 2 == 0 ? "even" : "odd")

# Filtering with ternary
items: products.map(p => 
    p.in_stock ? 
        {id: p.id, name: p.name, status: "available"} : 
        null
).filter(Boolean)

# Reduce with ternary
total: items.reduce((sum, item) => 
    sum + (item.taxable ? item.price * 1.08 : item.price), 
    0
)

# Conditional accumulation
grouped: data.reduce((acc, item) => {
    key: item.type == "A" ? "group1" : "group2"
    acc[key]: (acc[key] ?? []).concat(item)
    return acc
}, {})
```

## Ternary with Type Checking

```tusk
# Type-based behavior
process_value: (val) => 
    is_array(val) ? val.join(",") :
    is_object(val) ? JSON.stringify(val) :
    is_number(val) ? val.toFixed(2) :
    String(val)

# Safe type conversion
to_number: (val) => 
    is_number(val) ? val :
    is_string(val) ? parseFloat(val) :
    is_boolean(val) ? (val ? 1 : 0) :
    0

# Type guards
safe_length: (val) => 
    is_string(val) ? val.length :
    is_array(val) ? val.length :
    has_property(val, 'length') ? val.length :
    0
```

## Performance Considerations

```tusk
# Avoid expensive operations in ternary
# Bad - calculates both values
result: condition ? expensive_operation1() : expensive_operation2()

# Good - lazy evaluation
result: condition ? 
    (() => expensive_operation1())() : 
    (() => expensive_operation2())()

# Better - use if statement for complex logic
if (condition) {
    result: expensive_operation1()
} else {
    result: expensive_operation2()
}

# Cache repeated checks
is_valid: validate_input(input)
message: is_valid ? "Success" : "Failed"
class: is_valid ? "success" : "error"
```

## Ternary Best Practices

```tusk
# Keep it simple
# Good
status: active ? "on" : "off"

# Bad - too complex
result: a > b ? (c > d ? (e > f ? "x" : "y") : "z") : (g > h ? "i" : "j")

# Use parentheses for clarity
priority: (user.premium ? 10 : 5) + bonus

# Align for readability
config: {
    timeout:   dev_mode ? 5000  : 30000,
    retries:   dev_mode ? 1     : 3,
    cache:     dev_mode ? false : true,
    compress:  dev_mode ? false : true
}

# Consider extracting to functions
get_user_level: (user) => {
    return user.admin ? 3 :
           user.moderator ? 2 :
           user.premium ? 1 : 0
}

level: get_user_level(user)
```

## Alternative Patterns

```tusk
# Object lookup instead of nested ternary
grades: {
    A: [90, 100],
    B: [80, 89],
    C: [70, 79],
    D: [60, 69],
    F: [0, 59]
}

get_grade: (score) => {
    for (letter, range in grades) {
        if (score >= range[0] && score <= range[1]) {
            return letter
        }
    }
}

# Switch expression
result: switch (true) {
    case score >= 90: "A"
    case score >= 80: "B"
    case score >= 70: "C"
    case score >= 60: "D"
    default: "F"
}

# Guard pattern
get_discount: (user) => {
    if (!user) return 0
    if (user.vip) return 0.25
    if (user.premium) return 0.15
    if (user.member) return 0.10
    return 0.05
}
```

## Common Pitfalls

```tusk
# Falsy value confusion
# Careful with 0, empty string, etc.
count: items.length ? items.length : 1  # 0 becomes 1!
count: items.length ?? 1  # Better for this case

# Assignment vs comparison
# Wrong
result: x = 5 ? "yes" : "no"  # Assignment!

# Right
result: x == 5 ? "yes" : "no"  # Comparison

# Side effects in ternary
# Avoid
counter: validate() ? counter++ : counter--  # Side effects

# Better
if (validate()) {
    counter++
} else {
    counter--
}
```

## Best Practices

1. **Keep ternaries simple** - Use if-else for complex logic
2. **Avoid deep nesting** - Max 2-3 levels
3. **Use parentheses** - Clarify precedence
4. **Consider readability** - Sometimes if-else is clearer
5. **Don't abuse ternaries** - Not everything needs to be compact
6. **Watch for side effects** - Ternaries should be expressions
7. **Use consistent formatting** - Align for readability
8. **Extract complex logic** - Into named functions

## Related Topics

- `conditional-logic` - If-else statements
- `null-coalescing` - ?? operator
- `optional-chaining` - ?. operator
- `logical-operators` - && and || operators
- `switch-expressions` - Pattern matching