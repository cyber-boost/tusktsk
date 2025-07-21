# 🦀 Logical Operators in TuskLang Rust

**"We don't bow to any king" - Boolean Logic Edition**

TuskLang Rust provides powerful logical operators that leverage Rust's type system and ownership model. Say goodbye to undefined behavior and hello to compile-time safety with zero-cost abstractions.

## 🚀 Basic Logical Operators

```rust
use tusklang_rust::{and, or, not, Boolean};

// AND operator (&&) with short-circuit evaluation
if user.active && user.verified {
    grant_access();
}

// OR operator (||) with type safety
if error.critical || error.count > 10 {
    send_alert();
}

// NOT operator (!) with proper boolean conversion
if !user.banned {
    allow_posting();
}

// Combining operators with parentheses for clarity
if (age >= 18 && age <= 65) || has_permission {
    process_request();
}
```

## 🎯 AND Operator (&&)

```rust
use tusklang_rust::{and_operator, short_circuit};

// Both conditions must be true
let can_purchase = user.logged_in && user.age >= 18;

// Multiple AND conditions with proper error handling
let is_valid = !input.is_empty() && 
               input.len() <= 100 && 
               !contains_special_chars(&input);

// Short-circuit evaluation - second condition only evaluated if first is true
let result = check_permission() && perform_action();

// Chaining method calls with Result types
let success = validate_input(&data)
    .and_then(|_| save_to_database(&data))
    .and_then(|_| send_notification())
    .is_ok();

// Guard pattern with Option chaining
fn process_user(user: Option<&User>) -> Option<()> {
    user?
        .active
        .then(|| ())?
        .permissions
        .as_ref()?
        .contains("write")
        .then(|| ())
}
```

## ⚡ OR Operator (||)

```rust
use tusklang_rust::{or_operator, fallback};

// At least one condition must be true
let has_access = user.is_admin || user.is_owner || user.has_permission;

// Default values with Option unwrapping
let name = user.name.as_deref().unwrap_or("Anonymous");
let port = env::var("PORT")
    .ok()
    .and_then(|p| p.parse::<u16>().ok())
    .unwrap_or(config.port)
    .unwrap_or(3000);

// Multiple OR conditions with pattern matching
let is_weekend = matches!(day.as_str(), "Saturday" | "Sunday");

// Fallback chain with Result and Option
let data = get_from_cache()
    .or_else(|_| get_from_database())
    .unwrap_or_else(|_| get_default_data());

// Validation alternatives with type checking
let is_valid_id = is_uuid(id) || is_numeric(id) || is_legacy_id(id);
```

## 🔧 NOT Operator (!)

```rust
use tusklang_rust::{not_operator, negation};

// Simple negation with proper boolean conversion
if !logged_in {
    redirect("/login");
}

// Boolean conversion with explicit methods
let is_truthy = !value.is_empty();

// Negating complex expressions with parentheses
if !(user.role == "admin" || user.role == "moderator") {
    deny_access();
}

// Common patterns with Rust types
let is_empty = array.is_empty();
let has_no_errors = errors.is_empty();
let is_invalid = !is_valid(&input);

// Negation in iterators
let inactive_users: Vec<&User> = users.iter()
    .filter(|user| !user.active)
    .collect();
```

## 🚀 Combining Logical Operators

```rust
use tusklang_rust::{complex_logic, precedence};

// Complex conditions with proper grouping
if (user.age >= 18 && user.country == "US") || 
   (user.age >= 21 && user.country == "UK") ||
   user.has_override_permission {
    allow_purchase();
}

// Precedence (AND before OR) - use parentheses for clarity
// This is evaluated as: a && (b || c)
if a && (b || c) {
    // ...
}

// Explicit grouping for clarity
if (a && b) || c {
    // Explicit grouping
}

// Multi-level conditions with proper boolean logic
let is_eligible = (user.active && !user.suspended) &&
                  (user.tier == "premium" || user.credits > 100) &&
                  (!region_restricted || user.region == allowed_region);
```

## ⚡ Short-Circuit Evaluation

```rust
use tusklang_rust::{short_circuit, lazy_evaluation};

// AND short-circuits on false
let result = expensive_check() && very_expensive_check();
// very_expensive_check() only runs if expensive_check() is true

// OR short-circuits on true with Result types
let cached = get_from_cache().or_else(|_| fetch_from_api());
// fetch_from_api() only runs if get_from_cache() returns Err

// Practical examples with safe property access
let value = obj.as_ref()
    .and_then(|o| o.property.as_ref())
    .and_then(|p| p.nested.as_ref());

// Conditional execution with lazy evaluation
if debug {
    println!("Debug info: {:?}", data);
}

// Early return pattern with Result
fn process(data: Option<Data>) -> Result<ProcessedData, Error> {
    let data = data.ok_or(Error::NoData)?;
    
    if !data.valid {
        return Err(Error::InvalidData);
    }
    
    // Main processing
    Ok(transform(data))
}
```

## 🎯 Truthy and Falsy Values

```rust
use tusklang_rust::{truthiness, type_safety};

// Rust's truthiness rules - much more explicit than dynamic languages
let falsy_values = vec![
    false,          // Boolean false
    None,           // Option::None
    0,              // Number zero
    "",             // Empty string
    Vec::<i32>::new(), // Empty vector
    HashMap::<String, String>::new(), // Empty map
];

// Everything else is truthy, including:
let truthy_examples = vec![
    true,           // Boolean true
    1,              // Any non-zero number
    "hello",        // Non-empty string
    vec![1, 2, 3],  // Non-empty vector
    HashMap::from([("key".to_string(), "value".to_string())]), // Non-empty map
];

// Testing truthiness with explicit checks
let values = vec![0, 1, "", "hello", vec![], vec![1], None, Some(42)];
let results: Vec<_> = values.iter().map(|v| {
    let truthy = match v {
        0 => false,
        "" => false,
        vec if vec.is_empty() => false,
        None => false,
        _ => true,
    };
    (v, truthy, std::any::type_name_of_val(v))
}).collect();
```

## 🔧 Logical Assignment Operators

```rust
use tusklang_rust::{logical_assignment, mutation};

// Logical AND assignment with Option
let mut user_settings = None;
if let Some(settings) = load_user_settings() {
    user_settings = Some(settings);
}

// Logical OR assignment with fallback
let mut port = 3000;
port = env::var("PORT")
    .ok()
    .and_then(|p| p.parse::<u16>().ok())
    .unwrap_or(port);

// Logical NOT assignment with boolean toggle
let mut debug_mode = false;
debug_mode = !debug_mode;

// Conditional assignment with if-let
let mut cached_data = None;
if cached_data.is_none() {
    cached_data = fetch_data().ok();
}
```

## 🛡️ Type-Safe Logical Operations

```rust
use tusklang_rust::{type_safety, generic_logic};

// Generic logical operations with trait bounds
trait Logical {
    fn and<T>(self, other: T) -> bool;
    fn or<T>(self, other: T) -> bool;
    fn not(self) -> bool;
}

impl Logical for bool {
    fn and<T>(self, other: T) -> bool 
    where T: Into<bool> {
        self && other.into()
    }
    
    fn or<T>(self, other: T) -> bool 
    where T: Into<bool> {
        self || other.into()
    }
    
    fn not(self) -> bool {
        !self
    }
}

// Custom logical types
#[derive(Debug, Clone, PartialEq)]
enum Permission {
    Read,
    Write,
    Admin,
}

impl Permission {
    fn implies(&self, other: &Permission) -> bool {
        match (self, other) {
            (Permission::Admin, _) => true,
            (Permission::Write, Permission::Read) => true,
            (Permission::Read, Permission::Read) => true,
            _ => false,
        }
    }
}
```

## 🚀 Advanced Logical Patterns

```rust
use tusklang_rust::{advanced_patterns, functional};

// Functional logical operations
let conditions = vec![
    user.active,
    user.verified,
    user.age >= 18,
    !user.banned,
];

let all_true = conditions.iter().all(|&condition| condition);
let any_true = conditions.iter().any(|&condition| condition);
let none_true = conditions.iter().all(|&condition| !condition);

// Logical operations with iterators
let valid_users: Vec<&User> = users.iter()
    .filter(|user| user.active && user.verified && !user.banned)
    .collect();

// Custom logical combinators
struct LogicalCombinator<T> {
    conditions: Vec<Box<dyn Fn(&T) -> bool>>,
}

impl<T> LogicalCombinator<T> {
    fn new() -> Self {
        Self { conditions: Vec::new() }
    }
    
    fn and<F>(mut self, condition: F) -> Self 
    where F: Fn(&T) -> bool + 'static {
        self.conditions.push(Box::new(condition));
        self
    }
    
    fn evaluate(&self, item: &T) -> bool {
        self.conditions.iter().all(|condition| condition(item))
    }
}
```

## ⚡ Performance Optimizations

```rust
use tusklang_rust::{performance, optimization};

// Lazy evaluation with closures
let expensive_condition = || {
    // Expensive operation
    std::thread::sleep(Duration::from_millis(100));
    true
};

let result = simple_check() && expensive_condition();

// Memoization of logical results
struct LogicalCache {
    cache: HashMap<String, bool>,
}

impl LogicalCache {
    fn evaluate(&mut self, key: &str, condition: impl FnOnce() -> bool) -> bool {
        if let Some(&cached) = self.cache.get(key) {
            cached
        } else {
            let result = condition();
            self.cache.insert(key.to_string(), result);
            result
        }
    }
}

// Bitwise operations for multiple boolean flags
#[derive(Debug, Clone, Copy)]
struct UserFlags {
    active: bool,
    verified: bool,
    premium: bool,
    admin: bool,
}

impl UserFlags {
    fn to_bits(self) -> u8 {
        let mut bits = 0;
        if self.active { bits |= 1 << 0; }
        if self.verified { bits |= 1 << 1; }
        if self.premium { bits |= 1 << 2; }
        if self.admin { bits |= 1 << 3; }
        bits
    }
    
    fn from_bits(bits: u8) -> Self {
        Self {
            active: bits & (1 << 0) != 0,
            verified: bits & (1 << 1) != 0,
            premium: bits & (1 << 2) != 0,
            admin: bits & (1 << 3) != 0,
        }
    }
}
```

## 🎯 Best Practices

```rust
use tusklang_rust::{best_practices, guidelines};

// 1. Use parentheses for complex expressions
let result = (a && b) || (c && d);

// 2. Leverage short-circuit evaluation
let value = expensive_check() && very_expensive_check();

// 3. Use Option and Result for safe operations
let name = user.name.as_deref().unwrap_or("Anonymous");

// 4. Prefer explicit boolean conversion
let is_valid = !input.is_empty() && input.len() <= 100;

// 5. Use pattern matching for complex conditions
let access_level = match (user.role.as_str(), user.verified) {
    ("admin", _) => "full",
    ("moderator", true) => "limited",
    (_, true) => "read_only",
    _ => "none",
};

// 6. Use functional patterns for collections
let valid_items = items.iter()
    .filter(|item| item.active && !item.deleted)
    .collect::<Vec<_>>();

// 7. Handle errors gracefully
let result = operation1()
    .and_then(|_| operation2())
    .and_then(|_| operation3());

// 8. Use type system to prevent logical errors
#[derive(Debug, Clone, PartialEq)]
enum UserState {
    Active,
    Inactive,
    Suspended,
}

impl UserState {
    fn can_access(&self) -> bool {
        matches!(self, UserState::Active)
    }
}
```

## 🔗 Related Functions

- `and!()` - Logical AND macro
- `or!()` - Logical OR macro
- `not!()` - Logical NOT macro
- `all!()` - All conditions true macro
- `any!()` - Any condition true macro
- `none!()` - No conditions true macro

## 🚀 Advanced Examples

```rust
use tusklang_rust::{advanced_examples, real_world};

// Complex permission system
struct PermissionSystem {
    user: User,
    resource: Resource,
    action: Action,
}

impl PermissionSystem {
    fn can_perform(&self) -> bool {
        let user_permissions = &self.user.permissions;
        let resource_permissions = &self.resource.required_permissions;
        
        // User must be active and not banned
        let user_valid = self.user.active && !self.user.banned;
        
        // User must have required permissions
        let has_permissions = resource_permissions.iter()
            .all(|perm| user_permissions.contains(perm));
        
        // Time-based restrictions
        let time_allowed = self.resource.available_hours.contains(&chrono::Utc::now().hour());
        
        // Geographic restrictions
        let location_allowed = !self.resource.geo_restricted || 
                              self.user.region == self.resource.allowed_region;
        
        user_valid && has_permissions && time_allowed && location_allowed
    }
}

// Async logical operations
async fn async_logical_check(user: &User) -> Result<bool, Error> {
    let (active, verified, premium) = tokio::join!(
        check_user_active(user.id),
        check_user_verified(user.id),
        check_user_premium(user.id),
    );
    
    Ok(active? && verified? && premium?)
}
```

**TuskLang Rust: Where logical operators meet type safety. Your boolean logic will never be the same.** 