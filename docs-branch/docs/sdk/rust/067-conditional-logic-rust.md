# 🦀 Conditional Logic in TuskLang Rust

**"We don't bow to any king" - Control Flow Edition**

TuskLang Rust provides powerful, type-safe conditional logic constructs that leverage Rust's ownership system and pattern matching. Say goodbye to null pointer exceptions and hello to compile-time safety.

## 🚀 If Statements

```rust
use tusklang_rust::{if_expr, match_expr, Condition};

// Basic if statement with type safety
let status = if user.age >= 18 {
    "adult".to_string()
} else {
    "minor".to_string()
};

// If-else statement with automatic type inference
let grade = if score >= 90 {
    "A"
} else if score >= 80 {
    "B"
} else if score >= 70 {
    "C"
} else if score >= 60 {
    "D"
} else {
    "F"
};

// Nested conditions with proper scoping
let access_level = if user.authenticated {
    match user.role.as_str() {
        "admin" => "full",
        "editor" => "limited",
        _ => "read_only",
    }
} else {
    "none"
};

// If with multiple statements
let result = if debug {
    log::debug!("Debug mode active");
    Some("debug_enabled")
} else {
    None
};
```

## 🎯 Inline Conditionals

```rust
use tusklang_rust::{inline_if, guard};

// Single line if with expression
let _ = if debug { log::debug!("Debug mode active") };

// Conditional assignment with type safety
let status = if active { "online" } else { "offline" };

// Multiple statements in one line
let _ = if let Some(error) = error {
    log::error!("Error: {:?}", error);
    return Err(error);
};

// Guard clauses with early returns
fn validate_user(user: &User) -> Result<(), ValidationError> {
    if user.is_none() {
        return Err(ValidationError::UserRequired);
    }
    
    let user = user.as_ref().unwrap();
    
    if user.email.is_empty() {
        return Err(ValidationError::EmailRequired);
    }
    
    if user.age < 18 {
        return Err(ValidationError::AgeRequirement);
    }
    
    Ok(())
}
```

## ⚡ Pattern Matching with Match

```rust
use tusklang_rust::{match_expr, Pattern};

// Basic match with exhaustive checking
let day_type = match day.as_str() {
    "Monday" | "Tuesday" | "Wednesday" | "Thursday" | "Friday" => "weekday",
    "Saturday" | "Sunday" => "weekend",
    _ => "unknown",
};

// Match with expressions and automatic return
let result = match user.role.as_str() {
    "admin" => "Full access",
    "user" => "Limited access",
    _ => "No access",
};

// Pattern matching with ranges
let response_handler = match response.status {
    200..=299 => handle_success,
    301 | 302 => handle_redirect,
    400..=499 => handle_client_error,
    500..=599 => handle_server_error,
    _ => handle_unknown,
};

response_handler(response);

// Match with destructuring
let access_level = match user {
    User { role: "admin", .. } => "full",
    User { role: "editor", verified: true, .. } => "limited",
    User { verified: true, .. } => "read_only",
    _ => "none",
};
```

## 🔧 Boolean Logic

```rust
use tusklang_rust::{and, or, not, Boolean};

// AND operator with short-circuit evaluation
if user.active && user.verified {
    allow_access();
}

// OR operator with type safety
if user.role == "admin" || user.role == "moderator" {
    show_mod_tools();
}

// NOT operator with proper boolean conversion
if !user.banned {
    allow_posting();
}

// Complex boolean expressions with parentheses
let can_purchase = (user.age >= 18 && user.country == "US") || 
                   (user.age >= 21 && user.country == "UK");

if can_purchase {
    allow_purchase();
}

// Short-circuit evaluation with Option
let result = cached_value.or_else(|| expensive_calculation());

// Null coalescing with Option
let name = user.name.as_deref().unwrap_or("Guest");
```

## 🎯 Truthiness and Falsiness

```rust
use tusklang_rust::{is_truthy, is_falsy};

// Rust's truthiness rules
let falsy_values = vec![
    false,      // boolean false
    None,       // Option::None
    0,          // zero
    "",         // empty string
    Vec::new(), // empty vector
    HashMap::new(), // empty map
];

// Truthy check with proper type handling
if !value.is_empty() {
    // Value is truthy
}

// Explicit boolean conversion
let is_valid = !value.is_empty();

// Common patterns with Rust types
if !array.is_empty() {
    // Array has items
}

if !string.is_empty() {
    // String is not empty
}

if let Some(property) = object.get("property") {
    // Property exists and is Some
}
```

## 🚀 Advanced Conditionals

```rust
use tusklang_rust::{Condition, all, any};

// Multiple condition checking with iterators
let conditions = vec![
    user.age >= 18,
    user.verified,
    user.terms_accepted,
    !user.banned,
];

let access_level = if conditions.iter().all(|&condition| condition) {
    "full"
} else if conditions.iter().any(|&condition| condition) {
    "limited"
} else {
    "denied"
};

// Conditional chaining with Option
let theme = user
    .as_ref()
    .and_then(|u| u.profile.as_ref())
    .and_then(|p| p.settings.as_ref())
    .and_then(|s| s.theme.as_ref())
    .unwrap_or("default");

// Conditional method calls with closures
if user.active {
    user.send_notification();
} else {
    user.queue_notification();
}

// Dynamic condition building with Vec
let mut filters = Vec::new();

if let Some(search_term) = search_term {
    filters.push(format!("name LIKE '%{}%'", search_term));
}

if let Some(category) = category {
    filters.push(format!("category_id = {}", category));
}

if let Some(min_price) = min_price {
    filters.push(format!("price >= {}", min_price));
}

let where_clause = if !filters.is_empty() {
    format!("WHERE {}", filters.join(" AND "))
} else {
    String::new()
};
```

## 🎯 When/Unless Helpers

```rust
use tusklang_rust::{when, unless, Conditional};

// When helper (executes if condition is true)
when(user.premium, || {
    enable_premium_features();
    remove_ads();
});

// Unless helper (executes if condition is false)
unless(user.verified, || {
    show_verification_prompt();
    limit_features();
});

// Conditional rendering with templates
let html = format!(
    "{} {}",
    when(user.logged_in, || "<a href=\"/logout\">Logout</a>"),
    unless(user.logged_in, || "<a href=\"/login\">Login</a>")
);

// Chained conditionals with builder pattern
let query = QueryBuilder::new("users")
    .when(has_filter, |q| q.where("status", "active"))
    .when(has_sort, |q| q.order_by(sort_field))
    .unless(include_deleted, |q| q.where_null("deleted_at"));
```

## 🔄 Conditional Loops

```rust
use tusklang_rust::{while_loop, loop_until};

// While loop with condition
let mut counter = 0;
while counter < 10 {
    println!("Counter: {}", counter);
    counter += 1;
}

// Loop with break condition
let mut attempts = 0;
loop {
    attempts += 1;
    
    if attempts > 3 {
        break Err("Max attempts exceeded");
    }
    
    match try_operation() {
        Ok(result) => break Ok(result),
        Err(_) => continue,
    }
};

// For loop with conditional processing
for user in users {
    if user.active {
        process_active_user(user);
    } else {
        skip_inactive_user(user);
    }
}
```

## 🛡️ Error Handling with Conditionals

```rust
use tusklang_rust::{Result, Error};

// Conditional error handling with match
let result = match operation() {
    Ok(value) => {
        if value > 100 {
            Ok(value * 2)
        } else {
            Ok(value)
        }
    },
    Err(e) => {
        if e.is_retryable() {
            retry_operation()
        } else {
            Err(e)
        }
    }
};

// Conditional error recovery
let processed_data = if let Ok(data) = fetch_data() {
    if data.is_valid() {
        data.process()
    } else {
        Data::default()
    }
} else {
    Data::default()
};

// Guard clauses with custom error types
#[derive(Debug, thiserror::Error)]
enum ValidationError {
    #[error("User required")]
    UserRequired,
    #[error("Email required")]
    EmailRequired,
    #[error("Age requirement not met")]
    AgeRequirement,
}

fn validate_user(user: Option<&User>) -> Result<(), ValidationError> {
    let user = user.ok_or(ValidationError::UserRequired)?;
    
    if user.email.is_empty() {
        return Err(ValidationError::EmailRequired);
    }
    
    if user.age < 18 {
        return Err(ValidationError::AgeRequirement);
    }
    
    Ok(())
}
```

## 🎯 Conditional Macros

```rust
use tusklang_rust::{conditional_macro, debug_assert};

// Conditional compilation
#[cfg(debug_assertions)]
fn debug_log(message: &str) {
    println!("DEBUG: {}", message);
}

#[cfg(not(debug_assertions))]
fn debug_log(_message: &str) {
    // No-op in release builds
}

// Conditional feature flags
#[cfg(feature = "premium")]
fn premium_feature() {
    // Premium functionality
}

// Conditional type definitions
#[cfg(target_os = "linux")]
type PlatformSpecific = LinuxType;

#[cfg(target_os = "windows")]
type PlatformSpecific = WindowsType;

// Debug assertions
debug_assert!(value > 0, "Value must be positive");
debug_assert_eq!(actual, expected, "Values should be equal");
```

## ⚡ Performance Optimizations

```rust
use tusklang_rust::{lazy_evaluation, memoization};

// Lazy evaluation with closures
let expensive_calculation = || {
    // Expensive operation
    std::thread::sleep(Duration::from_secs(1));
    42
};

let result = if condition {
    expensive_calculation()
} else {
    0
};

// Memoization with conditional caching
struct MemoizedCalculation {
    cache: HashMap<String, i32>,
}

impl MemoizedCalculation {
    fn calculate(&mut self, input: &str) -> i32 {
        if let Some(&cached) = self.cache.get(input) {
            cached
        } else {
            let result = self.expensive_calculation(input);
            self.cache.insert(input.to_string(), result);
            result
        }
    }
    
    fn expensive_calculation(&self, _input: &str) -> i32 {
        // Expensive operation
        42
    }
}
```

## 🎯 Best Practices

1. **Use pattern matching over if-else chains** - More readable and exhaustive
2. **Leverage Option and Result types** - Avoid null pointer exceptions
3. **Use guard clauses for early returns** - Reduce nesting
4. **Prefer match over switch** - Better type safety and exhaustiveness
5. **Use conditional compilation** - Optimize for different build targets
6. **Handle all cases explicitly** - Let the compiler catch missing cases
7. **Use boolean expressions efficiently** - Leverage short-circuit evaluation
8. **Consider performance implications** - Use lazy evaluation when appropriate

## 🔗 Related Functions

- `match_expr!()` - Pattern matching macro
- `if_expr!()` - Conditional expression macro
- `when!()` - Conditional execution macro
- `unless!()` - Inverse conditional execution macro
- `guard!()` - Guard clause macro

## 🚀 Advanced Patterns

```rust
use tusklang_rust::{pattern_matching, functional};

// Functional conditional programming
let result = users
    .iter()
    .filter(|user| user.active)
    .map(|user| user.process())
    .collect::<Vec<_>>();

// Conditional trait implementations
trait ConditionalTrait {
    fn conditional_method(&self) -> Option<String>;
}

impl ConditionalTrait for User {
    fn conditional_method(&self) -> Option<String> {
        if self.premium {
            Some("premium_feature".to_string())
        } else {
            None
        }
    }
}

// Conditional async operations
async fn conditional_async_operation(user: &User) -> Result<String, Error> {
    if user.has_permission("admin") {
        admin_operation().await
    } else {
        user_operation().await
    }
}
```

**TuskLang Rust: Where conditional logic meets compile-time safety. Your control flow will never be the same.** 