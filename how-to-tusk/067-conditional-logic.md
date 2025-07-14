# Conditional Logic in TuskLang

TuskLang provides powerful conditional logic constructs for controlling program flow based on conditions.

## If Statements

```tusk
# Basic if statement
if (user.age >= 18) {
    status: "adult"
}

# If-else statement
if (score >= 90) {
    grade: "A"
} else {
    grade: "B"
}

# If-elseif-else chain
if (score >= 90) {
    grade: "A"
} elseif (score >= 80) {
    grade: "B"
} elseif (score >= 70) {
    grade: "C"
} elseif (score >= 60) {
    grade: "D"
} else {
    grade: "F"
}

# Nested conditions
if (user.authenticated) {
    if (user.role == "admin") {
        access_level: "full"
    } elseif (user.role == "editor") {
        access_level: "limited"
    } else {
        access_level: "read_only"
    }
} else {
    access_level: "none"
}
```

## Inline Conditionals

```tusk
# Single line if
if (debug) log("Debug mode active")

# Conditional assignment
status: if (active) "online" else "offline"

# Multiple statements in one line
if (error) { log.error(error); return null }

# Guard clauses
validate_user: (user) => {
    if (!user) return {error: "User required"}
    if (!user.email) return {error: "Email required"}
    if (!user.age >= 18) return {error: "Must be 18+"}
    return {success: true}
}
```

## Switch Statements

```tusk
# Basic switch
switch (day) {
    case "Monday":
    case "Tuesday":
    case "Wednesday":
    case "Thursday":
    case "Friday":
        type: "weekday"
        break
        
    case "Saturday":
    case "Sunday":
        type: "weekend"
        break
        
    default:
        type: "unknown"
}

# Switch with expressions
result: switch (user.role) {
    case "admin": 
        "Full access"
    case "user": 
        "Limited access"
    default: 
        "No access"
}

# Pattern matching switch
switch (response.status) {
    case 200..299:
        handle_success(response)
        
    case 301, 302:
        handle_redirect(response)
        
    case 400..499:
        handle_client_error(response)
        
    case 500..599:
        handle_server_error(response)
        
    default:
        handle_unknown(response)
}
```

## Boolean Logic

```tusk
# AND operator (&&)
if (user.active && user.verified) {
    allow_access()
}

# OR operator (||)
if (user.role == "admin" || user.role == "moderator") {
    show_mod_tools()
}

# NOT operator (!)
if (!user.banned) {
    allow_posting()
}

# Complex boolean expressions
if ((user.age >= 18 && user.country == "US") || 
    (user.age >= 21 && user.country == "UK")) {
    allow_purchase()
}

# Short-circuit evaluation
result: cached_value || expensive_calculation()

# Null coalescing
name: user.name ?? "Guest"
```

## Truthiness and Falsiness

```tusk
# Falsy values in TuskLang
falsy_values: [
    false,      # boolean false
    null,       # null
    0,          # zero
    "",         # empty string
    [],         # empty array
    {}          # empty object (in some contexts)
]

# Truthy check
if (value) {
    # Executes if value is truthy
}

# Explicit boolean conversion
is_valid: !!value

# Common patterns
if (array.length) {
    # Array has items
}

if (string) {
    # String is not empty
}

if (object.property) {
    # Property exists and is truthy
}
```

## Advanced Conditionals

```tusk
# Multiple condition checking
conditions: [
    user.age >= 18,
    user.verified,
    user.terms_accepted,
    !user.banned
]

if (conditions.all()) {
    grant_full_access()
} elseif (conditions.some()) {
    grant_limited_access()
} else {
    deny_access()
}

# Conditional chaining
result: user?.profile?.settings?.theme ?? "default"

# Conditional method calls
user.active ? user.send_notification() : user.queue_notification()

# Dynamic condition building
filters: []
if (search_term) filters[] = "name LIKE '%" + search_term + "%'"
if (category) filters[] = "category_id = " + category
if (min_price) filters[] = "price >= " + min_price

where_clause: filters.length ? "WHERE " + filters.join(" AND ") : ""
```

## When/Unless Helpers

```tusk
# When helper (executes if condition is true)
when(user.premium, () => {
    enable_premium_features()
    remove_ads()
})

# Unless helper (executes if condition is false)
unless(user.verified, () => {
    show_verification_prompt()
    limit_features()
})

# Conditional rendering
html: """
    {when(user.logged_in, '<a href="/logout">Logout</a>')}
    {unless(user.logged_in, '<a href="/login">Login</a>')}
"""

# Chained conditionals
query.when(has_filter, (q) => q.where("status", "active"))
     .when(has_sort, (q) => q.orderBy(sort_field))
     .unless(include_deleted, (q) => q.whereNull("deleted_at"))
```

## Conditional Loops

```tusk
# While loop
counter: 0
while (counter < 10) {
    process(counter)
    counter++
}

# Do-while loop
do {
    result: attempt_operation()
} while (!result.success && retries++ < 3)

# Break on condition
foreach (items as item) {
    if (item.stop) break
    if (item.skip) continue
    
    process(item)
}

# Conditional loop continuation
items.each((item) => {
    if (!should_process(item)) return true  # Continue
    if (reached_limit()) return false       # Break
    
    process(item)
})
```

## Error Handling Conditionals

```tusk
# Try-catch conditionals
try {
    result: risky_operation()
    if (result.success) {
        handle_success(result.data)
    } else {
        handle_failure(result.error)
    }
} catch (NetworkError e) {
    if (e.code == 'TIMEOUT') {
        retry_operation()
    } else {
        log_error(e)
    }
} catch (e) {
    handle_generic_error(e)
}

# Conditional error recovery
result: attempt_operation()
    .catch_if(
        (e) => e.code == 'RETRY', 
        () => retry_with_backoff()
    )
    .catch_if(
        (e) => e.code == 'AUTH',
        () => refresh_auth_and_retry()
    )
    .catch(() => default_error_handler())
```

## Performance Considerations

```tusk
# Order conditions by likelihood
if (common_case) {
    # Most likely path first
} elseif (less_common) {
    # Less likely
} else {
    # Rare case
}

# Avoid expensive operations in conditions
# Bad
if (expensive_calculation() > threshold) {
    # ...
}

# Good
calc_result: expensive_calculation()
if (calc_result > threshold) {
    # ...
}

# Use early returns
process_user: (user) => {
    if (!user) return null
    if (!user.active) return null
    if (user.banned) return null
    
    # Main logic here
    return process(user)
}
```

## Conditional Compilation

```tusk
# Compile-time conditionals
#if DEBUG
    log.level: "debug"
    enable_profiling: true
#else
    log.level: "error"
    enable_profiling: false
#endif

# Environment-based code inclusion
#if ENVIRONMENT == "production"
    include("optimized_functions.tusk")
#else
    include("debug_functions.tusk")
#endif

# Feature flags
#if FEATURE_NEW_UI
    render_new_ui()
#else
    render_legacy_ui()
#endif
```

## Best Practices

1. **Use meaningful condition names** - Make code self-documenting
2. **Prefer early returns** - Reduce nesting depth
3. **Order by probability** - Put likely cases first
4. **Avoid deep nesting** - Extract complex logic to functions
5. **Use guard clauses** - Handle edge cases early
6. **Leverage short-circuit evaluation** - For performance
7. **Be explicit about truthiness** - When intent isn't clear
8. **Consider readability** - Sometimes verbose is better

## Related Topics

- `ternary-operators` - Conditional expressions
- `logical-operators` - Boolean operations
- `comparison-operators` - Comparison operations
- `switch-expressions` - Pattern matching
- `null-coalescing` - Null handling