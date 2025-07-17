# 🦀 Function Composition in TuskLang Rust

**"We don't bow to any king" - Functional Programming Edition**

TuskLang Rust provides powerful function composition capabilities that leverage Rust's type system and zero-cost abstractions. Say goodbye to callback hell and hello to elegant, type-safe functional programming.

## 🚀 Basic Function Composition

```rust
use tusklang_rust::{function_composition, functional};

// Basic function composition
fn add_one(x: i32) -> i32 {
    x + 1
}

fn multiply_by_two(x: i32) -> i32 {
    x * 2
}

fn square(x: i32) -> i32 {
    x * x
}

// Manual composition
let result = square(multiply_by_two(add_one(5))); // ((5 + 1) * 2)^2 = 144

// Using the compose macro
use tusklang_rust::compose;

let composed = compose!(square, multiply_by_two, add_one);
let result = composed(5); // 144

// Function composition with closures
let add_one = |x: i32| x + 1;
let multiply_by_two = |x: i32| x * 2;
let square = |x: i32| x * x;

let composed = |x| square(multiply_by_two(add_one(x)));
let result = composed(5); // 144
```

## 🎯 Advanced Function Composition

```rust
use tusklang_rust::{advanced_composition, traits};

// Generic function composition
fn compose<A, B, C, F, G>(f: F, g: G) -> impl Fn(A) -> C
where
    F: Fn(B) -> C,
    G: Fn(A) -> B,
{
    move |x| f(g(x))
}

// Usage
let add_one = |x: i32| x + 1;
let multiply_by_two = |x: i32| x * 2;
let square = |x: i32| x * x;

let composed = compose(square, compose(multiply_by_two, add_one));
let result = composed(5); // 144

// Function composition with different types
fn parse_int(s: &str) -> Result<i32, std::num::ParseIntError> {
    s.parse()
}

fn validate_positive(x: i32) -> Result<i32, String> {
    if x > 0 {
        Ok(x)
    } else {
        Err("Number must be positive".to_string())
    }
}

fn double(x: i32) -> i32 {
    x * 2
}

// Compose functions that return Results
fn compose_results<A, B, C, E, F, G>(f: F, g: G) -> impl Fn(A) -> Result<C, E>
where
    F: Fn(B) -> Result<C, E>,
    G: Fn(A) -> Result<B, E>,
{
    move |x| g(x).and_then(f)
}

let composed = compose_results(double, parse_int);
let result = composed("42"); // Ok(84)
```

## ⚡ Iterator Function Composition

```rust
use tusklang_rust::{iterator_composition, functional};

// Compose iterator operations
let numbers = vec![1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

// Traditional approach
let result: Vec<i32> = numbers.iter()
    .filter(|&&x| x % 2 == 0)
    .map(|&x| x * 2)
    .filter(|&x| x > 10)
    .collect();

// Using function composition
let is_even = |x: &i32| x % 2 == 0;
let double = |x: &i32| x * 2;
let greater_than_10 = |x: &i32| *x > 10;

let filter_even = |iter| iter.filter(is_even);
let map_double = |iter| iter.map(double);
let filter_gt_10 = |iter| iter.filter(greater_than_10);

let composed_pipeline = compose!(filter_gt_10, map_double, filter_even);
let result: Vec<i32> = composed_pipeline(numbers.iter()).collect();

// Custom iterator composition trait
trait IteratorCompose: Iterator + Sized {
    fn compose<F, B>(self, f: F) -> std::iter::Map<Self, F>
    where
        F: FnMut(Self::Item) -> B,
    {
        self.map(f)
    }
}

impl<I: Iterator> IteratorCompose for I {}

// Usage
let result: Vec<i32> = numbers.iter()
    .compose(|&&x| x)
    .compose(|x| x * 2)
    .compose(|x| x + 1)
    .collect();
```

## 🔧 Function Composition with Error Handling

```rust
use tusklang_rust::{error_composition, Result};

// Compose functions that can fail
#[derive(Debug, thiserror::Error)]
enum ProcessingError {
    #[error("Invalid input: {0}")]
    InvalidInput(String),
    #[error("Processing failed: {0}")]
    ProcessingFailed(String),
    #[error("Validation failed: {0}")]
    ValidationFailed(String),
}

fn validate_input(s: &str) -> Result<&str, ProcessingError> {
    if s.is_empty() {
        Err(ProcessingError::InvalidInput("Input cannot be empty".to_string()))
    } else {
        Ok(s)
    }
}

fn parse_number(s: &str) -> Result<i32, ProcessingError> {
    s.parse::<i32>()
        .map_err(|e| ProcessingError::ProcessingFailed(e.to_string()))
}

fn validate_range(x: i32) -> Result<i32, ProcessingError> {
    if x >= 0 && x <= 100 {
        Ok(x)
    } else {
        Err(ProcessingError::ValidationFailed("Number out of range".to_string()))
    }
}

fn double(x: i32) -> i32 {
    x * 2
}

// Compose with error handling
fn compose_with_error<A, B, C, E, F, G>(f: F, g: G) -> impl Fn(A) -> Result<C, E>
where
    F: Fn(B) -> Result<C, E>,
    G: Fn(A) -> Result<B, E>,
{
    move |x| g(x).and_then(f)
}

// Compose pure functions with error functions
fn compose_pure_with_error<A, B, C, E, F, G>(f: F, g: G) -> impl Fn(A) -> Result<C, E>
where
    F: Fn(B) -> C,
    G: Fn(A) -> Result<B, E>,
{
    move |x| g(x).map(f)
}

let pipeline = compose_pure_with_error(
    double,
    compose_with_error(
        validate_range,
        compose_with_error(parse_number, validate_input)
    )
);

let result = pipeline("42"); // Ok(84)
let error = pipeline("150"); // Err(ValidationFailed)
```

## 🎯 Function Composition with State

```rust
use tusklang_rust::{state_composition, mutable};

// Function composition with mutable state
struct ProcessingState {
    count: usize,
    total: i32,
    errors: Vec<String>,
}

impl ProcessingState {
    fn new() -> Self {
        Self {
            count: 0,
            total: 0,
            errors: Vec::new(),
        }
    }
    
    fn increment_count(&mut self) {
        self.count += 1;
    }
    
    fn add_to_total(&mut self, value: i32) {
        self.total += value;
    }
    
    fn add_error(&mut self, error: String) {
        self.errors.push(error);
    }
}

// Functions that modify state
fn process_number(state: &mut ProcessingState, x: i32) -> i32 {
    state.increment_count();
    state.add_to_total(x);
    x * 2
}

fn validate_and_process(state: &mut ProcessingState, x: i32) -> Result<i32, String> {
    if x < 0 {
        let error = format!("Negative number: {}", x);
        state.add_error(error.clone());
        Err(error)
    } else {
        Ok(process_number(state, x))
    }
}

// Compose stateful functions
fn compose_stateful<A, B, C, F, G>(f: F, g: G) -> impl Fn(&mut ProcessingState, A) -> C
where
    F: Fn(&mut ProcessingState, B) -> C,
    G: Fn(&mut ProcessingState, A) -> B,
{
    move |state, x| f(state, g(state, x))
}

// Usage
let mut state = ProcessingState::new();
let pipeline = compose_stateful(
    |state, x: i32| x + 1,
    compose_stateful(
        |state, x: i32| x * 2,
        |state, x: i32| {
            state.increment_count();
            x
        }
    )
);

let result = pipeline(&mut state, 5); // 11
```

## 🚀 Function Composition with Async

```rust
use tusklang_rust::{async_composition, futures};

// Async function composition
async fn fetch_data(id: i32) -> Result<String, String> {
    // Simulate async operation
    tokio::time::sleep(tokio::time::Duration::from_millis(100)).await;
    Ok(format!("Data for id {}", id))
}

async fn parse_json(data: String) -> Result<serde_json::Value, String> {
    serde_json::from_str(&data)
        .map_err(|e| format!("JSON parsing failed: {}", e))
}

async fn validate_data(data: serde_json::Value) -> Result<serde_json::Value, String> {
    if data.is_object() {
        Ok(data)
    } else {
        Err("Data must be an object".to_string())
    }
}

async fn transform_data(data: serde_json::Value) -> serde_json::Value {
    // Transform the data
    data
}

// Compose async functions
fn compose_async<A, B, C, E, F, G>(f: F, g: G) -> impl Fn(A) -> std::pin::Pin<Box<dyn std::future::Future<Output = Result<C, E>> + Send>>
where
    F: Fn(B) -> std::pin::Pin<Box<dyn std::future::Future<Output = Result<C, E>> + Send>> + Send + Sync + 'static,
    G: Fn(A) -> std::pin::Pin<Box<dyn std::future::Future<Output = Result<B, E>> + Send>> + Send + Sync + 'static,
    A: Send + 'static,
    B: Send + 'static,
    C: Send + 'static,
    E: Send + 'static,
{
    move |x| {
        let future = g(x);
        Box::pin(async move {
            let result = future.await?;
            f(result).await
        })
    }
}

// Usage
let pipeline = compose_async(
    |data| Box::pin(async move { transform_data(data).await }),
    compose_async(
        |data| Box::pin(async move { validate_data(data).await }),
        compose_async(
            |data| Box::pin(async move { parse_json(data).await }),
            |id| Box::pin(async move { fetch_data(id).await })
        )
    )
);

// Run the pipeline
let result = pipeline(42).await;
```

## 🛡️ Function Composition with Type Safety

```rust
use tusklang_rust::{type_safety, generic_composition};

// Type-safe function composition with generics
trait Composable<A, B> {
    type Output;
    fn compose<C, F>(self, f: F) -> impl Fn(A) -> C
    where
        F: Fn(B) -> C;
}

impl<A, B, F> Composable<A, B> for F
where
    F: Fn(A) -> B,
{
    type Output = B;
    
    fn compose<C, G>(self, g: G) -> impl Fn(A) -> C
    where
        G: Fn(B) -> C,
    {
        move |x| g(self(x))
    }
}

// Custom function composition with type constraints
struct FunctionComposer<F> {
    f: F,
}

impl<F> FunctionComposer<F> {
    fn new(f: F) -> Self {
        Self { f }
    }
    
    fn then<G, B, C>(self, g: G) -> FunctionComposer<impl Fn(A) -> C>
    where
        F: Fn(A) -> B,
        G: Fn(B) -> C,
    {
        FunctionComposer {
            f: move |x| g(self.f(x)),
        }
    }
    
    fn call<A>(&self, x: A) -> <F as FnOnce(A)>::Output
    where
        F: FnOnce(A),
    {
        self.f(x)
    }
}

// Usage
let add_one = |x: i32| x + 1;
let multiply_by_two = |x: i32| x * 2;
let square = |x: i32| x * x;

let composer = FunctionComposer::new(add_one)
    .then(multiply_by_two)
    .then(square);

let result = composer.call(5); // 144
```

## ⚡ Performance Optimizations

```rust
use tusklang_rust::{performance, optimization};

// Lazy function composition
struct LazyComposer<F, A> {
    f: F,
    _phantom: std::marker::PhantomData<A>,
}

impl<F, A> LazyComposer<F, A>
where
    F: Fn(A) -> A,
{
    fn new(f: F) -> Self {
        Self {
            f,
            _phantom: std::marker::PhantomData,
        }
    }
    
    fn compose<G>(self, g: G) -> LazyComposer<impl Fn(A) -> A, A>
    where
        G: Fn(A) -> A,
    {
        LazyComposer::new(move |x| g(self.f(x)))
    }
    
    fn evaluate(self, x: A) -> A
    where
        F: FnOnce(A) -> A,
    {
        self.f(x)
    }
}

// Memoized function composition
use std::collections::HashMap;
use std::sync::Mutex;

struct MemoizedComposer<F, A, B> {
    f: F,
    cache: Mutex<HashMap<A, B>>,
}

impl<F, A, B> MemoizedComposer<F, A, B>
where
    F: Fn(A) -> B,
    A: Eq + std::hash::Hash + Clone,
    B: Clone,
{
    fn new(f: F) -> Self {
        Self {
            f,
            cache: Mutex::new(HashMap::new()),
        }
    }
    
    fn call(&self, x: A) -> B {
        if let Some(cached) = self.cache.lock().unwrap().get(&x) {
            return cached.clone();
        }
        
        let result = (self.f)(x.clone());
        self.cache.lock().unwrap().insert(x, result.clone());
        result
    }
}

// Parallel function composition
use rayon::prelude::*;

fn parallel_compose<A, B, C, F, G>(f: F, g: G) -> impl Fn(Vec<A>) -> Vec<C>
where
    F: Fn(B) -> C + Send + Sync,
    G: Fn(A) -> B + Send + Sync,
    A: Send,
    B: Send,
    C: Send,
{
    move |inputs| {
        inputs.into_par_iter()
            .map(g)
            .map(f)
            .collect()
    }
}
```

## 🎯 Function Composition Patterns

```rust
use tusklang_rust::{composition_patterns, design};

// Pipeline pattern
struct Pipeline<A, B> {
    functions: Vec<Box<dyn Fn(A) -> B>>,
}

impl<A, B> Pipeline<A, B> {
    fn new() -> Self {
        Self {
            functions: Vec::new(),
        }
    }
    
    fn add<F>(mut self, f: F) -> Self
    where
        F: Fn(A) -> B + 'static,
    {
        self.functions.push(Box::new(f));
        self
    }
    
    fn execute(&self, input: A) -> Vec<B> {
        self.functions.iter()
            .map(|f| f(input))
            .collect()
    }
}

// Builder pattern for function composition
struct FunctionBuilder<A, B> {
    f: Box<dyn Fn(A) -> B>,
}

impl<A, B> FunctionBuilder<A, B> {
    fn new<F>(f: F) -> Self
    where
        F: Fn(A) -> B + 'static,
    {
        Self {
            f: Box::new(f),
        }
    }
    
    fn then<C, G>(self, g: G) -> FunctionBuilder<A, C>
    where
        G: Fn(B) -> C + 'static,
    {
        let f = self.f;
        FunctionBuilder::new(move |x| g(f(x)))
    }
    
    fn build(self) -> Box<dyn Fn(A) -> B> {
        self.f
    }
}

// Usage
let pipeline = FunctionBuilder::new(|x: i32| x + 1)
    .then(|x| x * 2)
    .then(|x| x * x)
    .build();

let result = pipeline(5); // 144
```

## 🔗 Related Functions

- `compose!()` - Function composition macro
- `pipe!()` - Pipeline macro
- `curry!()` - Function currying macro
- `partial!()` - Partial application macro
- `memoize!()` - Memoization macro
- `lazy!()` - Lazy evaluation macro

## 🎯 Best Practices

```rust
use tusklang_rust::{best_practices, guidelines};

// 1. Use type annotations for clarity
let add_one: fn(i32) -> i32 = |x| x + 1;
let multiply_by_two: fn(i32) -> i32 = |x| x * 2;

// 2. Compose functions with similar types
let pipeline = compose!(square, multiply_by_two, add_one);

// 3. Handle errors properly in composition
let safe_pipeline = compose_with_error(validate, parse);

// 4. Use lazy evaluation for expensive operations
let lazy_pipeline = LazyComposer::new(expensive_function);

// 5. Consider performance implications
let memoized = MemoizedComposer::new(expensive_function);

// 6. Use parallel composition for large datasets
let parallel_pipeline = parallel_compose(process, transform);

// 7. Keep compositions readable
let pipeline = FunctionBuilder::new(step1)
    .then(step2)
    .then(step3)
    .build();

// 8. Test composed functions thoroughly
#[cfg(test)]
mod tests {
    use super::*;
    
    #[test]
    fn test_composition() {
        let composed = compose!(square, multiply_by_two, add_one);
        assert_eq!(composed(5), 144);
    }
}
```

**TuskLang Rust: Where function composition meets type safety. Your functional programming will never be the same.** 