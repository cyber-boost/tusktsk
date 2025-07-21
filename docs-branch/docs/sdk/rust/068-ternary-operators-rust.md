# 🦀 Ternary Operators in TuskLang Rust

**"We don't bow to any king" - Expression Edition**

TuskLang Rust provides powerful ternary operators that leverage Rust's expression-based syntax and type system. Say goodbye to verbose if-else blocks and hello to concise, type-safe conditional expressions.

## 🚀 Basic Ternary Syntax

```rust
use tusklang_rust::{ternary, if_expr};

// Basic ternary operator with type safety
let result = if condition { true_value } else { false_value };

// Simple example with automatic type inference
let age = 25;
let status = if age >= 18 { "adult" } else { "minor" };

// With variables and proper ownership
let is_logged_in = true;
let message = if is_logged_in { "Welcome back!" } else { "Please log in" };

// With expressions and type coercion
let score = 85;
let grade = if score >= 90 { "A" } else if score >= 80 { "B" } else { "C" };
```

## 🎯 Nested Ternary Operators

```rust
use tusklang_rust::{nested_ternary, match_expr};

// Multiple conditions with match (more idiomatic than nested if-else)
let score = 75;
let grade = match score {
    90..=100 => "A",
    80..=89 => "B",
    70..=79 => "C",
    60..=69 => "D",
    _ => "F",
};

// Formatted for readability with if-else chain
let user_type = if user.is_admin {
    "Administrator"
} else if user.is_moderator {
    "Moderator"
} else if user.is_premium {
    "Premium User"
} else {
    "Standard User"
};

// Complex nested example with proper scoping
let discount = if customer.years > 5 {
    0.20
} else if customer.years > 2 {
    0.10
} else if customer.premium {
    0.05
} else {
    0.0
};
```

## ⚡ Ternary with Functions

```rust
use tusklang_rust::{function_ternary, lazy_evaluation};

// Function calls in ternary with proper error handling
let result = if is_valid(&input) {
    process(input)?
} else {
    handle_error()?
};

// Method calls with ownership considerations
let output = if user.active {
    user.get_full_profile()?
} else {
    user.get_basic_info()?
};

// Lazy evaluation with closures
let data = if use_cache {
    get_cached_data()?
} else {
    fetch_fresh_data()?
};

// With closures and async support
let handler = if is_async {
    Box::new(move || handle_async(request))
} else {
    Box::new(move || handle_sync(request))
};
```

## 🔧 Ternary in Assignments

```rust
use tusklang_rust::{assignment_ternary, Option};

// Variable assignment with Option handling
let username = user.as_ref()
    .and_then(|u| u.name.as_ref())
    .map(|n| n.as_str())
    .unwrap_or("Guest");

// Property assignment with struct construction
let config = Config {
    theme: user_preference.unwrap_or_else(|| "default".to_string()),
    language: detected_lang.unwrap_or_else(|| "en".to_string()),
    timezone: user.timezone.unwrap_or_else(|| "UTC".to_string()),
};

// Array elements with conditional logic
let statuses = vec![
    if order.shipped { "Shipped" } else { "Pending" },
    if payment.confirmed { "Paid" } else { "Awaiting Payment" },
    if stock > 0 { "In Stock" } else { "Out of Stock" },
];

// Dynamic keys with HashMap
let mut response = HashMap::new();
if success {
    response.insert("data", result);
} else {
    response.insert("error", result);
}
```

## 🎯 Ternary with Option Coalescing

```rust
use tusklang_rust::{option_coalescing, unwrap_or};

// Option coalescing operator (unwrap_or)
let name = user.name.unwrap_or_else(|| "Unknown".to_string());

// Combined with ternary
let display_name = user.nickname
    .as_ref()
    .map(|n| n.as_str())
    .or(user.name.as_deref())
    .unwrap_or("User");

// Multiple fallbacks with chaining
let theme = user.theme
    .or_else(|| settings.default_theme.clone())
    .unwrap_or_else(|| "light".to_string());

// With type checking and conversion
let value = if let Some(input) = input.as_str() {
    input.to_string()
} else if let Some(input) = input.as_string() {
    input
} else {
    String::new()
};
```

## 🎨 Ternary in Templates

```rust
use tusklang_rust::{template_ternary, format_macro};

// In string templates with format!
let greeting = format!("Hello, {}!", 
    user.name.as_deref().unwrap_or("Guest")
);

// HTML generation with conditional elements
let html = format!(r#"
<div class="{}">
    {}
    <h3>{}</h3>
</div>
"#,
    if active { "active" } else { "inactive" },
    if user.premium { "<span class=\"premium-badge\">PRO</span>" } else { "" },
    title.as_deref().unwrap_or("Untitled")
);

// Template literals with pluralization
let message = format!("You have {} {} in your cart",
    count,
    if count == 1 { "item" } else { "items" }
);

// CSS classes with conditional concatenation
let class_name = format!("btn {} {} {}",
    if primary { "btn-primary" } else { "btn-secondary" },
    if large { "btn-lg" } else { "" },
    if disabled { "disabled" } else { "" }
).trim();
```

## 🔄 Ternary in Function Returns

```rust
use tusklang_rust::{return_ternary, Result};

// Simple return with type safety
fn get_price(user: &User, base_price: f64) -> f64 {
    if user.premium { base_price * 0.8 } else { base_price }
}

// Arrow function equivalent with closure
let calculate_tax = |amount: f64, location: &str| {
    if location == "NY" { amount * 0.08 } else { amount * 0.05 }
};

// Multiple return paths with Result
fn validate(value: &str) -> Result<(), ValidationError> {
    if value.is_empty() {
        Err(ValidationError::Required)
    } else if value.len() < 3 {
        Err(ValidationError::TooShort)
    } else if value.len() > 50 {
        Err(ValidationError::TooLong)
    } else {
        Ok(())
    }
}

// Conditional return types with async
async fn fetch_data(use_mock: bool) -> Result<Data, Error> {
    if use_mock {
        Ok(mock_data())
    } else {
        http::get("/api/data").await
    }
}
```

## 🔄 Ternary in Iterators

```rust
use tusklang_rust::{iterator_ternary, functional};

// In iterator methods with map
let numbers = vec![1, 2, 3, 4, 5];
let labels: Vec<&str> = numbers.iter()
    .map(|&n| if n % 2 == 0 { "even" } else { "odd" })
    .collect();

// Filtering with ternary and Option
let items: Vec<Product> = products.into_iter()
    .filter_map(|p| {
        if p.in_stock {
            Some(Product {
                id: p.id,
                name: p.name,
                status: "available".to_string(),
            })
        } else {
            None
        }
    })
    .collect();

// Reduce with ternary
let total: f64 = items.iter()
    .fold(0.0, |sum, item| {
        sum + if item.taxable { item.price * 1.08 } else { item.price }
    });

// Conditional accumulation with HashMap
let grouped: HashMap<String, Vec<Item>> = data.into_iter()
    .fold(HashMap::new(), |mut acc, item| {
        let key = if item.item_type == "A" { "group1" } else { "group2" };
        acc.entry(key.to_string()).or_insert_with(Vec::new).push(item);
        acc
    });
```

## 🛡️ Ternary with Type Checking

```rust
use tusklang_rust::{type_checking, Any};

// Type-based behavior with pattern matching
fn process_value(val: &dyn std::any::Any) -> String {
    if let Some(s) = val.downcast_ref::<String>() {
        s.clone()
    } else if let Some(s) = val.downcast_ref::<&str>() {
        s.to_string()
    } else if let Some(arr) = val.downcast_ref::<Vec<String>>() {
        arr.join(",")
    } else if let Some(obj) = val.downcast_ref::<HashMap<String, String>>() {
        serde_json::to_string(obj).unwrap_or_default()
    } else if let Some(num) = val.downcast_ref::<f64>() {
        format!("{:.2}", num)
    } else {
        format!("{:?}", val)
    }
}

// Safe type conversion with Result
fn to_number(val: &str) -> Result<f64, ParseError> {
    if let Ok(num) = val.parse::<f64>() {
        Ok(num)
    } else if val == "true" {
        Ok(1.0)
    } else if val == "false" {
        Ok(0.0)
    } else {
        Err(ParseError::InvalidNumber)
    }
}

// Type guards with trait bounds
fn safe_length<T>(val: &T) -> usize 
where
    T: AsRef<str> + ?Sized,
{
    val.as_ref().len()
}

fn safe_length_vec<T>(val: &[T]) -> usize {
    val.len()
}
```

## ⚡ Performance Considerations

```rust
use tusklang_rust::{performance, lazy_evaluation};

// Avoid expensive operations in ternary
// Bad - calculates both values
let result = if condition { 
    expensive_operation1() 
} else { 
    expensive_operation2() 
};

// Good - lazy evaluation with closures
let result = if condition {
    expensive_operation1()
} else {
    expensive_operation2()
};

// Better - use match for complex logic
let result = match condition {
    true => expensive_operation1(),
    false => expensive_operation2(),
};

// Cache repeated checks
let is_valid = validate_input(&input);
let message = if is_valid { "Success" } else { "Failed" };
let class = if is_valid { "success" } else { "error" };
```

## 🎯 Ternary Best Practices

```rust
use tusklang_rust::{best_practices, readability};

// Keep it simple and readable
// Good
let status = if active { "on" } else { "off" };

// Bad - too complex, use match instead
let result = match (a > b, c > d, e > f, g > h) {
    (true, true, true, _) => "x",
    (true, true, false, _) => "y",
    (true, false, _, _) => "z",
    (false, _, _, true) => "i",
    (false, _, _, false) => "j",
};

// Use parentheses for clarity
let priority = (if user.premium { 10 } else { 5 }) + bonus;

// Align for readability
let user_type = if user.is_admin {
    "Administrator"
} else if user.is_moderator {
    "Moderator"
} else if user.is_premium {
    "Premium User"
} else {
    "Standard User"
};

// Use match for exhaustive patterns
let grade = match score {
    90..=100 => "A",
    80..=89 => "B",
    70..=79 => "C",
    60..=69 => "D",
    _ => "F",
};
```

## 🚀 Advanced Patterns

```rust
use tusklang_rust::{advanced_patterns, functional};

// Functional ternary with Option
let result = Some(42)
    .filter(|&x| x > 0)
    .map(|x| x * 2)
    .unwrap_or(0);

// Ternary with custom types
#[derive(Debug, Clone)]
enum UserStatus {
    Active,
    Inactive,
    Suspended,
}

impl UserStatus {
    fn display_name(&self) -> &'static str {
        match self {
            UserStatus::Active => "Active User",
            UserStatus::Inactive => "Inactive User",
            UserStatus::Suspended => "Suspended User",
        }
    }
}

// Ternary with async/await
async fn conditional_async_operation(user: &User) -> Result<String, Error> {
    let result = if user.premium {
        premium_operation().await?
    } else {
        standard_operation().await?
    };
    
    Ok(result)
}

// Ternary with generics
fn conditional_transform<T, U>(
    value: T,
    condition: bool,
    transform_a: impl FnOnce(T) -> U,
    transform_b: impl FnOnce(T) -> U,
) -> U {
    if condition {
        transform_a(value)
    } else {
        transform_b(value)
    }
}
```

## 🔗 Related Functions

- `if_expr!()` - Conditional expression macro
- `match_expr!()` - Pattern matching macro
- `ternary!()` - Ternary operator macro
- `option_coalesce!()` - Option coalescing macro
- `lazy_eval!()` - Lazy evaluation macro

## 🎯 Best Practices Summary

1. **Prefer match over nested if-else** - More readable and exhaustive
2. **Use Option methods for null coalescing** - `unwrap_or`, `or_else`
3. **Leverage type system** - Let compiler catch type mismatches
4. **Keep expressions simple** - Avoid complex nested ternaries
5. **Use proper ownership** - Consider borrowing vs ownership
6. **Handle errors gracefully** - Use Result types appropriately
7. **Consider performance** - Avoid expensive operations in ternaries
8. **Use functional patterns** - Leverage iterator methods

**TuskLang Rust: Where ternary operators meet type safety. Your conditional expressions will never be the same.** 