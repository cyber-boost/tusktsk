# Logical Operators in TuskLang

Logical operators are used to combine or modify boolean expressions, enabling complex conditional logic in your TuskLang applications.

## Basic Logical Operators

```tusk
# AND operator (&&)
if (user.active && user.verified) {
    grant_access()
}

# OR operator (||)
if (error.critical || error.count > 10) {
    send_alert()
}

# NOT operator (!)
if (!user.banned) {
    allow_posting()
}

# Combining operators
if ((age >= 18 && age <= 65) || has_permission) {
    process_request()
}
```

## AND Operator (&&)

```tusk
# Both conditions must be true
can_purchase: user.logged_in && user.age >= 18

# Multiple AND conditions
is_valid: input.length > 0 && 
          input.length <= 100 && 
          !contains_special_chars(input)

# Short-circuit evaluation
# Second condition only evaluated if first is true
result: check_permission() && perform_action()

# Chaining method calls
success: validate_input(data) && 
         save_to_database(data) && 
         send_notification()

# Guard pattern
process_user: (user) => {
    return user && 
           user.active && 
           user.permissions && 
           user.permissions.includes("write")
}
```

## OR Operator (||)

```tusk
# At least one condition must be true
has_access: user.is_admin || user.is_owner || user.has_permission

# Default values (short-circuit)
name: user.name || "Anonymous"
port: env.PORT || config.port || 3000

# Multiple OR conditions
is_weekend: day == "Saturday" || day == "Sunday"

# Fallback chain
data: get_from_cache() || 
      get_from_database() || 
      get_default_data()

# Validation alternatives
is_valid_id: is_uuid(id) || is_numeric(id) || is_legacy_id(id)
```

## NOT Operator (!)

```tusk
# Simple negation
if (!logged_in) {
    redirect("/login")
}

# Double negation for boolean conversion
is_truthy: !!value

# Negating complex expressions
if (!(user.role == "admin" || user.role == "moderator")) {
    deny_access()
}

# Common patterns
is_empty: !array.length
has_no_errors: !errors.length
is_invalid: !is_valid(input)

# Negation in filters
inactive_users: users.filter(user => !user.active)
```

## Combining Logical Operators

```tusk
# Complex conditions
if ((user.age >= 18 && user.country == "US") || 
    (user.age >= 21 && user.country == "UK") ||
    user.has_override_permission) {
    allow_purchase()
}

# Precedence (AND before OR)
# This is evaluated as: a && (b || c)
if (a && b || c) {
    // ...
}

# Use parentheses for clarity
if ((a && b) || c) {
    // Explicit grouping
}

# Multi-level conditions
is_eligible: (user.active && !user.suspended) &&
             (user.tier == "premium" || user.credits > 100) &&
             (!region_restricted || user.region == allowed_region)
```

## Short-Circuit Evaluation

```tusk
# AND short-circuits on false
result: expensive_check() && very_expensive_check()
# very_expensive_check() only runs if expensive_check() is true

# OR short-circuits on true
cached: get_from_cache() || fetch_from_api()
# fetch_from_api() only runs if get_from_cache() returns falsy

# Practical examples
# Safe property access
value: obj && obj.property && obj.property.nested

# Conditional execution
debug && console.log("Debug info:", data)

# Early return pattern
function process(data) {
    data || return null
    data.valid || return {error: "Invalid data"}
    
    # Main processing
    return transform(data)
}
```

## Truthy and Falsy Values

```tusk
# Falsy values in TuskLang
falsy_values: [
    false,          # Boolean false
    null,           # Null value
    undefined,      # Undefined
    0,              # Number zero
    -0,             # Negative zero
    0n,             # BigInt zero
    "",             # Empty string
    NaN             # Not a Number
]

# Everything else is truthy, including:
truthy_examples: [
    true,           # Boolean true
    1,              # Any non-zero number
    "hello",        # Non-empty string
    [],             # Empty array (!)
    {},             # Empty object (!)
    () => {},       # Functions
]

# Testing truthiness
values: [0, 1, "", "hello", [], {}, null, undefined]
results: values.map(v => ({
    value: v,
    truthy: !!v,
    type: typeof v
}))
```

## Logical Assignment Operators

```tusk
# Logical AND assignment (&&=)
# Assigns only if variable is truthy
user.settings &&= load_user_settings()

# Logical OR assignment (||=)
# Assigns only if variable is falsy
config.timeout ||= 5000

# Logical nullish assignment (??=)
# Assigns only if variable is null or undefined
user.name ??= "Guest"

# Practical examples
# Initialize if needed
cache.users ||= {}
cache.users[id] ||= fetch_user(id)

# Conditional update
user.active &&= check_subscription_status()

# Set defaults
options.retries ??= 3
options.timeout ??= 10000
options.headers ||= {}
```

## Advanced Patterns

```tusk
# All/Any patterns
conditions: [
    user.age >= 18,
    user.email_verified,
    user.terms_accepted
]

all_true: conditions.every(c => c)
any_true: conditions.some(c => c)

# Conditional chaining with logical operators
result: step1() && step2() && step3() || handle_failure()

# State machine logic
can_transition: (from, to) => {
    return (from == "pending" && to == "active") ||
           (from == "active" && to == "completed") ||
           (from == "active" && to == "cancelled") ||
           (from == "any" && user.is_admin)
}

# Feature flags with fallbacks
feature_enabled: (flag) => {
    return env.features[flag] ||
           user.beta_features[flag] ||
           default_features[flag] ||
           false
}
```

## Logical Operators in Functions

```tusk
# Parameter validation
create_user: (name, email, age) => {
    # Validate all parameters
    if (!name || !email || !age) {
        throw "All parameters required"
    }
    
    # Validate individual parameters
    if (!is_valid_email(email) || !is_valid_name(name)) {
        throw "Invalid input"
    }
    
    # Create user
    return new User(name, email, age)
}

# Conditional function execution
execute_if_valid: (condition, action, fallback) => {
    return condition && action() || fallback()
}

# Pipeline with error checking
process_pipeline: (data) => {
    return validate(data) &&
           transform(data) &&
           save(data) &&
           notify_success() ||
           handle_error()
}
```

## Performance Considerations

```tusk
# Order matters for short-circuit
# Put cheaper operations first
if (simple_check() && expensive_check()) {
    # expensive_check only runs if needed
}

# Cache boolean results
is_valid: null
get_validation_status: () => {
    # Cache expensive validation
    is_valid ??= perform_expensive_validation()
    return is_valid
}

# Avoid redundant checks
# Bad
if (user.active && user.verified && user.active) {
    # Checking active twice
}

# Good
if (user.active && user.verified) {
    # Each check only once
}
```

## Common Patterns and Idioms

```tusk
# Toggle boolean
is_visible: !is_visible

# Ensure boolean type
is_active: !!status

# Default object pattern
options: user_options || {}

# Safe navigation
value: a && a.b && a.b.c && a.b.c.d

# Existence check
has_property: obj && "property" in obj

# Array/String emptiness
if (array && array.length) {
    # Array exists and has items
}

# Multiple fallbacks
result: try_primary() || 
        try_secondary() || 
        try_tertiary() || 
        use_default()

# Conditional logging
verbose && log.debug("Detailed information")
```

## Best Practices

1. **Use parentheses for clarity** - Make precedence explicit
2. **Order for short-circuit** - Put cheaper/likely-to-fail conditions first
3. **Avoid deep nesting** - Extract complex logic to functions
4. **Be careful with falsy values** - 0 and "" are falsy
5. **Use descriptive names** - For boolean variables and functions
6. **Consider readability** - Sometimes if-else is clearer
7. **Leverage short-circuit** - For performance and control flow
8. **Test edge cases** - Especially with type coercion

## Related Topics

- `comparison-operators` - ==, !=, <, >, etc.
- `conditional-logic` - if, else, switch
- `ternary-operators` - ? : operator
- `nullish-coalescing` - ?? operator
- `optional-chaining` - ?. operator