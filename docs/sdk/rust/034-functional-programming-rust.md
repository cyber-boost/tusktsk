# Functional Programming in TuskLang with Rust

## 🧮 Functional Foundation

Functional programming with TuskLang and Rust provides a powerful paradigm for building reliable, testable, and maintainable applications. This guide covers pure functions, immutability, higher-order functions, and functional patterns.

## 🏗️ Functional Principles

### Core Principles

```rust
[functional_principles]
pure_functions: true
immutability: true
first_class_functions: true
composition: true

[functional_patterns]
map_reduce: true
monad_pattern: true
functor_pattern: true
applicative: true
```

### Functional Concepts

```rust
[functional_concepts]
referential_transparency: true
side_effect_free: true
higher_order_functions: true
currying: true
```

## 🔧 Pure Functions

### Function Purity

```rust
[function_purity]
pure_functions: true
side_effects: "avoided"
referential_transparency: true

[pure_function_implementation]
// Pure function - same input always produces same output
pub fn add(a: i32, b: i32) -> i32 {
    a + b
}

// Pure function with immutable data
pub fn calculate_total(items: &[Item]) -> f64 {
    items.iter()
        .map(|item| item.price * item.quantity as f64)
        .sum()
}

// Pure function with error handling
pub fn divide(a: f64, b: f64) -> Result<f64, DivisionError> {
    if b == 0.0 {
        Err(DivisionError::DivisionByZero)
    } else {
        Ok(a / b)
    }
}

// Pure function with validation
pub fn validate_email(email: &str) -> Result<Email, ValidationError> {
    if email.contains('@') && email.contains('.') {
        Ok(Email(email.to_string()))
    } else {
        Err(ValidationError::InvalidEmail)
    }
}
```

### Immutable Data Structures

```rust
[immutable_structures]
immutable_collections: true
persistent_data_structures: true
copy_on_write: true

[immutable_implementation]
use std::collections::HashMap;
use std::rc::Rc;

// Immutable user structure
#[derive(Clone, Debug)]
pub struct User {
    pub id: String,
    pub name: String,
    pub email: String,
    pub preferences: HashMap<String, String>,
}

impl User {
    pub fn new(id: String, name: String, email: String) -> Self {
        Self {
            id,
            name,
            email,
            preferences: HashMap::new(),
        }
    }
    
    // Immutable update - returns new instance
    pub fn with_preference(mut self, key: String, value: String) -> Self {
        self.preferences.insert(key, value);
        self
    }
    
    // Immutable update with multiple preferences
    pub fn with_preferences(mut self, prefs: HashMap<String, String>) -> Self {
        self.preferences.extend(prefs);
        self
    }
    
    // Pure function to get preference
    pub fn get_preference(&self, key: &str) -> Option<&String> {
        self.preferences.get(key)
    }
}

// Immutable configuration
#[derive(Clone, Debug)]
pub struct Config {
    pub database_url: String,
    pub api_key: String,
    pub settings: Rc<HashMap<String, String>>,
}

impl Config {
    pub fn new(database_url: String, api_key: String) -> Self {
        Self {
            database_url,
            api_key,
            settings: Rc::new(HashMap::new()),
        }
    }
    
    // Immutable update with new settings
    pub fn with_setting(self, key: String, value: String) -> Self {
        let mut new_settings = HashMap::clone(&self.settings);
        new_settings.insert(key, value);
        Self {
            settings: Rc::new(new_settings),
            ..self
        }
    }
}
```

## 🔄 Higher-Order Functions

### Function Composition

```rust
[function_composition]
composition: true
pipeline: true
currying: true

[composition_implementation]
// Function composition
pub fn compose<A, B, C, F, G>(f: F, g: G) -> impl Fn(A) -> C
where
    F: Fn(B) -> C,
    G: Fn(A) -> B,
{
    move |x| f(g(x))
}

// Pipeline operator simulation
pub trait Pipeline {
    fn pipe<F, R>(self, f: F) -> R
    where
        F: FnOnce(Self) -> R;
}

impl<T> Pipeline for T {
    fn pipe<F, R>(self, f: F) -> R
    where
        F: FnOnce(Self) -> R,
    {
        f(self)
    }
}

// Currying
pub fn curry<A, B, C, F>(f: F) -> impl Fn(A) -> impl Fn(B) -> C
where
    F: Fn(A, B) -> C,
{
    move |a| move |b| f(a, b)
}

// Usage examples
pub fn functional_examples() {
    // Function composition
    let add_one = |x: i32| x + 1;
    let multiply_by_two = |x: i32| x * 2;
    let add_one_then_multiply = compose(multiply_by_two, add_one);
    
    assert_eq!(add_one_then_multiply(5), 12);
    
    // Pipeline
    let result = 5
        .pipe(|x| x + 1)
        .pipe(|x| x * 2)
        .pipe(|x| x.to_string());
    
    assert_eq!(result, "12");
    
    // Currying
    let add = |a: i32, b: i32| a + b;
    let add_five = curry(add)(5);
    
    assert_eq!(add_five(3), 8);
}
```

### Map, Filter, Reduce

```rust
[map_filter_reduce]
map_operations: true
filter_operations: true
reduce_operations: true

[map_filter_reduce_implementation]
// Map operation
pub fn map<T, U, F>(items: &[T], f: F) -> Vec<U>
where
    F: Fn(&T) -> U,
{
    items.iter().map(f).collect()
}

// Filter operation
pub fn filter<T, F>(items: &[T], predicate: F) -> Vec<T>
where
    F: Fn(&T) -> bool,
    T: Clone,
{
    items.iter().filter(|&x| predicate(x)).cloned().collect()
}

// Reduce operation
pub fn reduce<T, U, F>(items: &[T], initial: U, f: F) -> U
where
    F: Fn(U, &T) -> U,
    U: Clone,
{
    items.iter().fold(initial, f)
}

// Fold operation
pub fn fold<T, U, F>(items: &[T], initial: U, f: F) -> U
where
    F: Fn(U, &T) -> U,
    U: Clone,
{
    items.iter().fold(initial, f)
}

// Usage examples
pub fn collection_operations() {
    let numbers = vec![1, 2, 3, 4, 5];
    
    // Map
    let doubled = map(&numbers, |x| x * 2);
    assert_eq!(doubled, vec![2, 4, 6, 8, 10]);
    
    // Filter
    let evens = filter(&numbers, |x| x % 2 == 0);
    assert_eq!(evens, vec![2, 4]);
    
    // Reduce
    let sum = reduce(&numbers, 0, |acc, x| acc + x);
    assert_eq!(sum, 15);
    
    // Fold
    let product = fold(&numbers, 1, |acc, x| acc * x);
    assert_eq!(product, 120);
}
```

## 🔄 Functor Pattern

### Functor Implementation

```rust
[functor_pattern]
functor_trait: true
option_functor: true
result_functor: true

[functor_implementation]
// Functor trait
pub trait Functor<A, B> {
    type Output;
    fn fmap<F>(self, f: F) -> Self::Output
    where
        F: Fn(A) -> B;
}

// Option as Functor
impl<A, B> Functor<A, B> for Option<A> {
    type Output = Option<B>;
    
    fn fmap<F>(self, f: F) -> Self::Output
    where
        F: Fn(A) -> B,
    {
        self.map(f)
    }
}

// Result as Functor
impl<A, B, E> Functor<A, B> for Result<A, E> {
    type Output = Result<B, E>;
    
    fn fmap<F>(self, f: F) -> Self::Output
    where
        F: Fn(A) -> B,
    {
        self.map(f)
    }
}

// Vec as Functor
impl<A, B> Functor<A, B> for Vec<A> {
    type Output = Vec<B>;
    
    fn fmap<F>(self, f: F) -> Self::Output
    where
        F: Fn(A) -> B,
    {
        self.into_iter().map(f).collect()
    }
}

// Usage examples
pub fn functor_examples() {
    // Option functor
    let some_value: Option<i32> = Some(5);
    let doubled = some_value.fmap(|x| x * 2);
    assert_eq!(doubled, Some(10));
    
    // Result functor
    let result: Result<i32, String> = Ok(5);
    let doubled = result.fmap(|x| x * 2);
    assert_eq!(doubled, Ok(10));
    
    // Vec functor
    let numbers = vec![1, 2, 3, 4, 5];
    let doubled = numbers.fmap(|x| x * 2);
    assert_eq!(doubled, vec![2, 4, 6, 8, 10]);
}
```

## 🔄 Monad Pattern

### Monad Implementation

```rust
[monad_pattern]
monad_trait: true
option_monad: true
result_monad: true
list_monad: true

[monad_implementation]
// Monad trait
pub trait Monad<A, B> {
    type Output;
    fn bind<F>(self, f: F) -> Self::Output
    where
        F: Fn(A) -> Self::Output;
}

// Option as Monad
impl<A, B> Monad<A, B> for Option<A> {
    type Output = Option<B>;
    
    fn bind<F>(self, f: F) -> Self::Output
    where
        F: Fn(A) -> Option<B>,
    {
        self.and_then(f)
    }
}

// Result as Monad
impl<A, B, E> Monad<A, B> for Result<A, E> {
    type Output = Result<B, E>;
    
    fn bind<F>(self, f: F) -> Self::Output
    where
        F: Fn(A) -> Result<B, E>,
    {
        self.and_then(f)
    }
}

// Vec as Monad
impl<A, B> Monad<A, B> for Vec<A> {
    type Output = Vec<B>;
    
    fn bind<F>(self, f: F) -> Self::Output
    where
        F: Fn(A) -> Vec<B>,
    {
        self.into_iter().flat_map(f).collect()
    }
}

// Monad comprehension
pub fn monad_comprehension() {
    // Option monad comprehension
    let result = Some(5)
        .bind(|x| Some(x + 1))
        .bind(|x| Some(x * 2))
        .bind(|x| if x > 10 { Some(x) } else { None });
    
    assert_eq!(result, Some(12));
    
    // Result monad comprehension
    let result: Result<i32, String> = Ok(5)
        .bind(|x| Ok(x + 1))
        .bind(|x| Ok(x * 2))
        .bind(|x| if x > 10 { Ok(x) } else { Err("Too small".to_string()) });
    
    assert_eq!(result, Ok(12));
    
    // List monad comprehension
    let result = vec![1, 2, 3]
        .bind(|x| vec![x, x * 2])
        .bind(|x| if x % 2 == 0 { vec![x] } else { vec![] });
    
    assert_eq!(result, vec![2, 4, 6]);
}
```

## 🔄 Applicative Pattern

### Applicative Implementation

```rust
[applicative_pattern]
applicative_trait: true
pure_function: true
apply_function: true

[applicative_implementation]
// Applicative trait
pub trait Applicative<A, B> {
    type Output;
    fn pure(value: A) -> Self;
    fn apply<F>(self, f: Self) -> Self::Output
    where
        F: Fn(A) -> B;
}

// Option as Applicative
impl<A, B> Applicative<A, B> for Option<A> {
    type Output = Option<B>;
    
    fn pure(value: A) -> Self {
        Some(value)
    }
    
    fn apply<F>(self, f: Option<F>) -> Self::Output
    where
        F: Fn(A) -> B,
    {
        match (self, f) {
            (Some(value), Some(func)) => Some(func(value)),
            _ => None,
        }
    }
}

// Result as Applicative
impl<A, B, E> Applicative<A, B> for Result<A, E> {
    type Output = Result<B, E>;
    
    fn pure(value: A) -> Self {
        Ok(value)
    }
    
    fn apply<F>(self, f: Result<F, E>) -> Self::Output
    where
        F: Fn(A) -> B,
    {
        match (self, f) {
            (Ok(value), Ok(func)) => Ok(func(value)),
            (Err(e), _) => Err(e),
            (_, Err(e)) => Err(e),
        }
    }
}

// Usage examples
pub fn applicative_examples() {
    // Option applicative
    let value = Option::pure(5);
    let func = Option::pure(|x: i32| x * 2);
    let result = value.apply(func);
    assert_eq!(result, Some(10));
    
    // Result applicative
    let value = Result::<i32, String>::pure(5);
    let func = Result::<fn(i32) -> i32, String>::pure(|x| x * 2);
    let result = value.apply(func);
    assert_eq!(result, Ok(10));
}
```

## 🔄 Lens Pattern

### Lens Implementation

```rust
[lens_pattern]
lens_trait: true
getter_setter: true
composition: true

[lens_implementation]
// Lens trait
pub trait Lens<S, A> {
    fn get(&self, source: &S) -> A;
    fn set(&self, source: S, value: A) -> S;
    fn modify<F>(&self, source: S, f: F) -> S
    where
        F: Fn(A) -> A,
    {
        let value = self.get(&source);
        let new_value = f(value);
        self.set(source, new_value)
    }
}

// User lens
pub struct UserNameLens;

impl Lens<User, String> for UserNameLens {
    fn get(&self, user: &User) -> String {
        user.name.clone()
    }
    
    fn set(&self, mut user: User, name: String) -> User {
        user.name = name;
        user
    }
}

// Email lens
pub struct UserEmailLens;

impl Lens<User, String> for UserEmailLens {
    fn get(&self, user: &User) -> String {
        user.email.clone()
    }
    
    fn set(&self, mut user: User, email: String) -> User {
        user.email = email;
        user
    }
}

// Lens composition
pub struct ComposeLens<L1, L2> {
    outer: L1,
    inner: L2,
}

impl<S, A, B, L1, L2> Lens<S, B> for ComposeLens<L1, L2>
where
    L1: Lens<S, A>,
    L2: Lens<A, B>,
{
    fn get(&self, source: &S) -> B {
        let intermediate = self.outer.get(source);
        self.inner.get(&intermediate)
    }
    
    fn set(&self, source: S, value: B) -> S {
        let intermediate = self.outer.get(&source);
        let new_intermediate = self.inner.set(intermediate, value);
        self.outer.set(source, new_intermediate)
    }
}

// Usage examples
pub fn lens_examples() {
    let user = User::new(
        "1".to_string(),
        "John Doe".to_string(),
        "john@example.com".to_string(),
    );
    
    let name_lens = UserNameLens;
    let email_lens = UserEmailLens;
    
    // Get value
    let name = name_lens.get(&user);
    assert_eq!(name, "John Doe");
    
    // Set value
    let updated_user = name_lens.set(user, "Jane Doe".to_string());
    assert_eq!(updated_user.name, "Jane Doe");
    
    // Modify value
    let updated_user = name_lens.modify(updated_user, |name| format!("Dr. {}", name));
    assert_eq!(updated_user.name, "Dr. Jane Doe");
}
```

## 🔄 Pattern Matching

### Advanced Pattern Matching

```rust
[pattern_matching]
destructuring: true
guards: true
multiple_patterns: true

[pattern_matching_implementation]
// Pattern matching with destructuring
pub fn process_user(user: User) -> String {
    match user {
        User { name, email, .. } if email.contains("@company.com") => {
            format!("Employee: {}", name)
        }
        User { name, email, .. } if email.contains("@gmail.com") => {
            format!("Gmail user: {}", name)
        }
        User { name, .. } => {
            format!("User: {}", name)
        }
    }
}

// Pattern matching with Result
pub fn process_result(result: Result<i32, String>) -> String {
    match result {
        Ok(value) if value > 0 => format!("Positive: {}", value),
        Ok(0) => "Zero".to_string(),
        Ok(value) => format!("Negative: {}", value),
        Err(error) => format!("Error: {}", error),
    }
}

// Pattern matching with Option
pub fn process_option(option: Option<i32>) -> String {
    match option {
        Some(value) if value > 10 => format!("Large: {}", value),
        Some(value) => format!("Small: {}", value),
        None => "No value".to_string(),
    }
}

// Pattern matching with tuples
pub fn process_tuple(tuple: (i32, String, bool)) -> String {
    match tuple {
        (0, name, true) => format!("Active user: {}", name),
        (id, name, false) => format!("Inactive user {} with id {}", name, id),
        (_, _, _) => "Unknown".to_string(),
    }
}
```

## 🔄 Functional Data Processing

### Data Pipeline

```rust
[data_pipeline]
pipeline_operations: true
stream_processing: true
batch_processing: true

[pipeline_implementation]
// Functional data pipeline
pub struct Pipeline<T> {
    operations: Vec<Box<dyn Fn(Vec<T>) -> Vec<T>>>,
}

impl<T> Pipeline<T> {
    pub fn new() -> Self {
        Self {
            operations: Vec::new(),
        }
    }
    
    pub fn map<F>(mut self, f: F) -> Self
    where
        F: Fn(&T) -> T + 'static,
        T: Clone,
    {
        self.operations.push(Box::new(move |items| {
            items.iter().map(|item| f(item)).collect()
        }));
        self
    }
    
    pub fn filter<F>(mut self, predicate: F) -> Self
    where
        F: Fn(&T) -> bool + 'static,
        T: Clone,
    {
        self.operations.push(Box::new(move |items| {
            items.into_iter().filter(|item| predicate(item)).collect()
        }));
        self
    }
    
    pub fn sort_by<F>(mut self, compare: F) -> Self
    where
        F: Fn(&T, &T) -> std::cmp::Ordering + 'static,
        T: Clone,
    {
        self.operations.push(Box::new(move |mut items| {
            items.sort_by(&compare);
            items
        }));
        self
    }
    
    pub fn execute(self, data: Vec<T>) -> Vec<T> {
        self.operations.into_iter().fold(data, |acc, op| op(acc))
    }
}

// Usage example
pub fn pipeline_example() {
    let numbers = vec![1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
    
    let pipeline = Pipeline::new()
        .filter(|&x| x % 2 == 0)
        .map(|&x| x * 2)
        .sort_by(|a, b| b.cmp(a));
    
    let result = pipeline.execute(numbers);
    assert_eq!(result, vec![20, 16, 12, 8, 4]);
}
```

## 🎯 Best Practices

### 1. **Function Design**
- Write pure functions
- Avoid side effects
- Use immutable data
- Prefer composition over inheritance

### 2. **Data Handling**
- Use immutable data structures
- Implement proper error handling
- Use pattern matching
- Leverage type safety

### 3. **Performance**
- Use lazy evaluation
- Implement efficient algorithms
- Avoid unnecessary allocations
- Profile your code

### 4. **Testing**
- Test pure functions
- Use property-based testing
- Test edge cases
- Verify referential transparency

### 5. **Composition**
- Compose small functions
- Use higher-order functions
- Leverage monads for error handling
- Use applicatives for parallel operations

Functional programming with TuskLang and Rust provides a powerful foundation for building reliable, testable, and maintainable applications that are easier to reason about and debug. 