# 🦀 Comparison Operators in TuskLang Rust

**"We don't bow to any king" - Comparison Edition**

TuskLang Rust provides powerful comparison operators that leverage Rust's type system and trait system. Say goodbye to implicit type coercion and hello to compile-time safety with explicit comparisons.

## 🚀 Basic Comparison Operators

```rust
use tusklang_rust::{compare, PartialEq, PartialOrd};

// Equality operators with type safety
let is_equal = a == b;
let is_not_equal = a != b;

// Relational operators with proper ordering
let is_less = a < b;
let is_less_equal = a <= b;
let is_greater = a > b;
let is_greater_equal = a >= b;

// Type-safe comparisons
let numbers = vec![1, 2, 3, 4, 5];
let strings = vec!["apple", "banana", "cherry"];

// Compare numbers
let max_number = numbers.iter().max().unwrap();
let min_number = numbers.iter().min().unwrap();

// Compare strings
let max_string = strings.iter().max().unwrap();
let min_string = strings.iter().min().unwrap();
```

## 🎯 Equality Operators (==, !=)

```rust
use tusklang_rust::{equality, PartialEq};

// Basic equality with primitive types
let a = 42;
let b = 42;
let is_equal = a == b; // true

// String equality
let name1 = "John".to_string();
let name2 = "John".to_string();
let names_equal = name1 == name2; // true

// Custom type equality
#[derive(Debug, Clone, PartialEq)]
struct User {
    id: i32,
    name: String,
    email: String,
}

let user1 = User {
    id: 1,
    name: "John".to_string(),
    email: "john@example.com".to_string(),
};

let user2 = User {
    id: 1,
    name: "John".to_string(),
    email: "john@example.com".to_string(),
};

let users_equal = user1 == user2; // true

// Inequality with Option types
let some_value = Some(42);
let none_value = None;
let not_equal = some_value != none_value; // true

// Floating point comparison (beware of precision)
let float1 = 0.1 + 0.2;
let float2 = 0.3;
let float_equal = (float1 - float2).abs() < f64::EPSILON; // Use epsilon for floats
```

## ⚡ Relational Operators (<, <=, >, >=)

```rust
use tusklang_rust::{relational, PartialOrd};

// Numeric comparisons
let age = 25;
let is_adult = age >= 18; // true
let is_teenager = age >= 13 && age <= 19; // true

// String comparisons (lexicographic)
let name1 = "Alice";
let name2 = "Bob";
let name1_comes_first = name1 < name2; // true

// Custom type ordering
#[derive(Debug, Clone, PartialEq, PartialOrd)]
struct Product {
    name: String,
    price: f64,
    priority: i32,
}

let product1 = Product {
    name: "Laptop".to_string(),
    price: 999.99,
    priority: 1,
};

let product2 = Product {
    name: "Mouse".to_string(),
    price: 29.99,
    priority: 2,
};

// Compare by priority first, then by price
let product1_higher_priority = product1.priority > product2.priority; // false

// Vector comparisons
let vec1 = vec![1, 2, 3];
let vec2 = vec![1, 2, 4];
let vec1_less = vec1 < vec2; // true (lexicographic comparison)

// Option comparisons
let some_5 = Some(5);
let some_10 = Some(10);
let some_5_less = some_5 < some_10; // true
let none_less = None < some_5; // true (None is less than Some)
```

## 🔧 Custom Comparison Implementations

```rust
use tusklang_rust::{custom_comparison, traits};

// Custom comparison for complex types
#[derive(Debug, Clone)]
struct ComplexNumber {
    real: f64,
    imaginary: f64,
}

impl PartialEq for ComplexNumber {
    fn eq(&self, other: &Self) -> bool {
        (self.real - other.real).abs() < f64::EPSILON &&
        (self.imaginary - other.imaginary).abs() < f64::EPSILON
    }
}

impl PartialOrd for ComplexNumber {
    fn partial_cmp(&self, other: &Self) -> Option<std::cmp::Ordering> {
        // Compare by magnitude
        let magnitude1 = (self.real * self.real + self.imaginary * self.imaginary).sqrt();
        let magnitude2 = (other.real * other.real + other.imaginary * other.imaginary).sqrt();
        magnitude1.partial_cmp(&magnitude2)
    }
}

// Custom comparison with multiple fields
#[derive(Debug, Clone)]
struct Person {
    name: String,
    age: u32,
    height: f64,
}

impl PartialEq for Person {
    fn eq(&self, other: &Self) -> bool {
        self.name == other.name && self.age == other.age
    }
}

impl PartialOrd for Person {
    fn partial_cmp(&self, other: &Self) -> Option<std::cmp::Ordering> {
        // Primary sort by age, secondary by name
        match self.age.cmp(&other.age) {
            std::cmp::Ordering::Equal => Some(self.name.cmp(&other.name)),
            other => Some(other),
        }
    }
}
```

## 🎯 Comparison with Collections

```rust
use tusklang_rust::{collection_comparison, iterators};

// Vector comparisons
let numbers1 = vec![1, 2, 3];
let numbers2 = vec![1, 2, 3];
let numbers3 = vec![1, 2, 4];

let equal_vectors = numbers1 == numbers2; // true
let different_vectors = numbers1 != numbers3; // true
let first_less = numbers1 < numbers3; // true

// HashMap comparisons
use std::collections::HashMap;

let mut map1 = HashMap::new();
map1.insert("a", 1);
map1.insert("b", 2);

let mut map2 = HashMap::new();
map2.insert("a", 1);
map2.insert("b", 2);

let maps_equal = map1 == map2; // true

// Custom collection comparison
#[derive(Debug, Clone)]
struct CustomSet {
    elements: Vec<i32>,
}

impl PartialEq for CustomSet {
    fn eq(&self, other: &Self) -> bool {
        if self.elements.len() != other.elements.len() {
            return false;
        }
        
        let mut sorted_self = self.elements.clone();
        let mut sorted_other = other.elements.clone();
        sorted_self.sort();
        sorted_other.sort();
        
        sorted_self == sorted_other
    }
}

// Set-like behavior
let set1 = CustomSet { elements: vec![3, 1, 2] };
let set2 = CustomSet { elements: vec![1, 2, 3] };
let sets_equal = set1 == set2; // true (order doesn't matter)
```

## 🛡️ Safe Comparison Patterns

```rust
use tusklang_rust::{safe_comparison, error_handling};

// Safe floating point comparison
fn safe_float_compare(a: f64, b: f64, epsilon: f64) -> bool {
    (a - b).abs() < epsilon
}

// Safe string comparison (case-insensitive)
fn case_insensitive_compare(a: &str, b: &str) -> bool {
    a.to_lowercase() == b.to_lowercase()
}

// Safe Option comparison
fn safe_option_compare<T: PartialEq>(a: Option<T>, b: Option<T>) -> bool {
    match (a, b) {
        (Some(a_val), Some(b_val)) => a_val == b_val,
        (None, None) => true,
        _ => false,
    }
}

// Safe Result comparison
fn safe_result_compare<T: PartialEq, E: PartialEq>(
    a: Result<T, E>,
    b: Result<T, E>
) -> bool {
    match (a, b) {
        (Ok(a_val), Ok(b_val)) => a_val == b_val,
        (Err(a_err), Err(b_err)) => a_err == b_err,
        _ => false,
    }
}

// Null-safe comparison
fn null_safe_compare<T: PartialEq>(a: Option<&T>, b: Option<&T>) -> bool {
    match (a, b) {
        (Some(a_val), Some(b_val)) => a_val == b_val,
        (None, None) => true,
        _ => false,
    }
}
```

## 🚀 Advanced Comparison Patterns

```rust
use tusklang_rust::{advanced_comparison, functional};

// Functional comparison with closures
let numbers = vec![1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

// Find all numbers greater than 5
let greater_than_5: Vec<&i32> = numbers.iter()
    .filter(|&&n| n > 5)
    .collect();

// Find the first number that satisfies a condition
let first_even = numbers.iter()
    .find(|&&n| n % 2 == 0);

// Custom comparison function
fn compare_by_criteria<T, F>(a: &T, b: &T, criteria: F) -> std::cmp::Ordering
where
    F: Fn(&T) -> i32,
{
    criteria(a).cmp(&criteria(b))
}

// Use custom comparison
let people = vec![
    Person { name: "Alice".to_string(), age: 30, height: 1.75 },
    Person { name: "Bob".to_string(), age: 25, height: 1.80 },
    Person { name: "Charlie".to_string(), age: 35, height: 1.70 },
];

// Sort by age
let sorted_by_age: Vec<Person> = {
    let mut sorted = people.clone();
    sorted.sort_by(|a, b| a.age.cmp(&b.age));
    sorted
};

// Sort by height
let sorted_by_height: Vec<Person> = {
    let mut sorted = people.clone();
    sorted.sort_by(|a, b| a.height.partial_cmp(&b.height).unwrap());
    sorted
};
```

## ⚡ Performance Optimizations

```rust
use tusklang_rust::{performance, optimization};

// Efficient comparison with early exit
fn efficient_string_compare(a: &str, b: &str) -> bool {
    if a.len() != b.len() {
        return false;
    }
    
    a.bytes().zip(b.bytes()).all(|(a_byte, b_byte)| a_byte == b_byte)
}

// Cached comparison results
use std::collections::HashMap;

struct ComparisonCache {
    cache: HashMap<(String, String), bool>,
}

impl ComparisonCache {
    fn new() -> Self {
        Self { cache: HashMap::new() }
    }
    
    fn compare(&mut self, a: &str, b: &str) -> bool {
        let key = if a < b {
            (a.to_string(), b.to_string())
        } else {
            (b.to_string(), a.to_string())
        };
        
        if let Some(&cached) = self.cache.get(&key) {
            cached
        } else {
            let result = a == b;
            self.cache.insert(key, result);
            result
        }
    }
}

// Lazy comparison with memoization
struct LazyComparator<T> {
    compare_fn: Box<dyn Fn(&T, &T) -> bool>,
    cache: HashMap<(usize, usize), bool>,
}

impl<T> LazyComparator<T> {
    fn new<F>(compare_fn: F) -> Self
    where
        F: Fn(&T, &T) -> bool + 'static,
    {
        Self {
            compare_fn: Box::new(compare_fn),
            cache: HashMap::new(),
        }
    }
    
    fn compare(&mut self, items: &[T], i: usize, j: usize) -> bool {
        let key = if i < j { (i, j) } else { (j, i) };
        
        if let Some(&cached) = self.cache.get(&key) {
            cached
        } else {
            let result = (self.compare_fn)(&items[i], &items[j]);
            self.cache.insert(key, result);
            result
        }
    }
}
```

## 🎯 Comparison with Error Handling

```rust
use tusklang_rust::{error_handling, Result};

// Comparison that can fail
fn safe_division_compare(a: f64, b: f64) -> Result<bool, &'static str> {
    if b == 0.0 {
        return Err("Division by zero");
    }
    
    Ok((a / b) > 1.0)
}

// Comparison with custom error types
#[derive(Debug, thiserror::Error)]
enum ComparisonError {
    #[error("Invalid comparison: {0}")]
    InvalidComparison(String),
    #[error("Type mismatch: expected {expected}, got {actual}")]
    TypeMismatch { expected: String, actual: String },
}

fn typed_compare<T: PartialEq + std::fmt::Debug>(
    a: &T,
    b: &T,
    expected_type: &str,
) -> Result<bool, ComparisonError> {
    let actual_type = std::any::type_name::<T>();
    
    if actual_type != expected_type {
        return Err(ComparisonError::TypeMismatch {
            expected: expected_type.to_string(),
            actual: actual_type.to_string(),
        });
    }
    
    Ok(a == b)
}

// Comparison with validation
fn validate_and_compare<T: PartialEq>(
    a: T,
    b: T,
    validator: impl Fn(&T) -> Result<(), String>,
) -> Result<bool, String> {
    validator(&a)?;
    validator(&b)?;
    Ok(a == b)
}
```

## 🔧 Best Practices

```rust
use tusklang_rust::{best_practices, guidelines};

// 1. Use appropriate comparison for floating point
let float_equal = (a - b).abs() < f64::EPSILON;

// 2. Implement PartialEq and PartialOrd for custom types
#[derive(Debug, Clone, PartialEq, PartialOrd)]
struct MyType {
    field1: i32,
    field2: String,
}

// 3. Use pattern matching for complex comparisons
let comparison_result = match (a, b) {
    (Some(a_val), Some(b_val)) => a_val == b_val,
    (None, None) => true,
    _ => false,
};

// 4. Leverage iterator methods for collection comparisons
let all_equal = numbers.iter().all(|&n| n == numbers[0]);

// 5. Use custom comparison functions for complex logic
fn compare_users(a: &User, b: &User) -> std::cmp::Ordering {
    a.age.cmp(&b.age)
        .then(a.name.cmp(&b.name))
        .then(a.email.cmp(&b.email))
}

// 6. Handle edge cases explicitly
fn safe_compare(a: Option<i32>, b: Option<i32>) -> bool {
    match (a, b) {
        (Some(a_val), Some(b_val)) => a_val == b_val,
        (None, None) => true,
        _ => false,
    }
}

// 7. Use type system to prevent comparison errors
#[derive(Debug, Clone, PartialEq, PartialOrd)]
enum UserRole {
    Guest,
    User,
    Moderator,
    Admin,
}

// 8. Consider performance implications
fn efficient_compare(a: &str, b: &str) -> bool {
    if a.len() != b.len() {
        return false;
    }
    a == b
}
```

## 🔗 Related Functions

- `compare!()` - Comparison macro
- `eq!()` - Equality macro
- `lt!()` - Less than macro
- `gt!()` - Greater than macro
- `partial_cmp!()` - Partial comparison macro
- `safe_compare!()` - Safe comparison macro

## 🚀 Advanced Examples

```rust
use tusklang_rust::{advanced_examples, real_world};

// Complex comparison system
struct ComparisonSystem<T> {
    comparators: Vec<Box<dyn Fn(&T, &T) -> std::cmp::Ordering>>,
}

impl<T> ComparisonSystem<T> {
    fn new() -> Self {
        Self { comparators: Vec::new() }
    }
    
    fn add_comparator<F>(mut self, comparator: F) -> Self
    where
        F: Fn(&T, &T) -> std::cmp::Ordering + 'static,
    {
        self.comparators.push(Box::new(comparator));
        self
    }
    
    fn compare(&self, a: &T, b: &T) -> std::cmp::Ordering {
        for comparator in &self.comparators {
            match comparator(a, b) {
                std::cmp::Ordering::Equal => continue,
                other => return other,
            }
        }
        std::cmp::Ordering::Equal
    }
}

// Usage
let comparison_system = ComparisonSystem::new()
    .add_comparator(|a: &Person, b: &Person| a.age.cmp(&b.age))
    .add_comparator(|a: &Person, b: &Person| a.name.cmp(&b.name));

// Async comparison
async fn async_compare<T: PartialEq + Send + Sync>(
    a: T,
    b: T,
) -> Result<bool, Box<dyn std::error::Error + Send + Sync>> {
    // Simulate async comparison
    tokio::time::sleep(Duration::from_millis(10)).await;
    Ok(a == b)
}
```

**TuskLang Rust: Where comparison operators meet type safety. Your comparisons will never be the same.** 