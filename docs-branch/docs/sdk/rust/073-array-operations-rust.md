# 🦀 Array Operations in TuskLang Rust

**"We don't bow to any king" - Collection Edition**

TuskLang Rust provides powerful array operations that leverage Rust's ownership system and zero-copy abstractions. Say goodbye to buffer overflows and hello to compile-time safety with efficient collection manipulation.

## 🚀 Basic Array Operations

```rust
use tusklang_rust::{array_ops, Vec, slice};

// Array creation
let empty_array: Vec<i32> = Vec::new();
let with_capacity: Vec<i32> = Vec::with_capacity(10);
let from_elements = vec![1, 2, 3, 4, 5];
let repeated = vec![0; 5]; // [0, 0, 0, 0, 0]

// Array access
let numbers = vec![1, 2, 3, 4, 5];
let first = numbers[0]; // 1
let last = numbers[numbers.len() - 1]; // 5

// Safe array access with Option
let safe_first = numbers.get(0); // Some(&1)
let safe_out_of_bounds = numbers.get(10); // None

// Array length and capacity
let length = numbers.len(); // 5
let capacity = numbers.capacity(); // Current capacity
let is_empty = numbers.is_empty(); // false
```

## 🎯 Array Manipulation

```rust
use tusklang_rust::{array_manipulation, methods};

// Adding elements
let mut numbers = vec![1, 2, 3];
numbers.push(4); // [1, 2, 3, 4]
numbers.insert(1, 10); // [1, 10, 2, 3, 4]

// Removing elements
let removed = numbers.pop(); // Some(4), numbers = [1, 10, 2, 3]
let removed_at = numbers.remove(1); // 10, numbers = [1, 2, 3]

// Clearing and truncating
numbers.clear(); // []
numbers.truncate(2); // Keep only first 2 elements

// Resizing
numbers.resize(5, 0); // [0, 0, 0, 0, 0]
numbers.resize_with(3, || rand::random::<i32>()); // Random values

// Swapping elements
let mut array = vec![1, 2, 3, 4, 5];
array.swap(0, 4); // [5, 2, 3, 4, 1]
```

## ⚡ Array Iteration and Transformation

```rust
use tusklang_rust::{array_iteration, functional};

// Basic iteration
let numbers = vec![1, 2, 3, 4, 5];

// For loop
for number in &numbers {
    println!("Number: {}", number);
}

// Iterator methods
let doubled: Vec<i32> = numbers.iter()
    .map(|&x| x * 2)
    .collect(); // [2, 4, 6, 8, 10]

let evens: Vec<&i32> = numbers.iter()
    .filter(|&&x| x % 2 == 0)
    .collect(); // [2, 4]

let sum: i32 = numbers.iter().sum(); // 15
let product: i32 = numbers.iter().product(); // 120

// Chaining operations
let result: Vec<i32> = numbers.iter()
    .filter(|&&x| x > 2)
    .map(|&x| x * x)
    .collect(); // [9, 16, 25]
```

## 🔧 Array Searching and Filtering

```rust
use tusklang_rust::{array_search, filtering};

// Finding elements
let numbers = vec![1, 2, 3, 4, 5, 2, 6];

// Find first occurrence
let first_two = numbers.iter().find(|&&x| x == 2); // Some(&2)

// Find position
let position = numbers.iter().position(|&x| x == 2); // Some(1)

// Find last occurrence
let last_position = numbers.iter().rposition(|&x| x == 2); // Some(5)

// Check if contains
let contains_three = numbers.contains(&3); // true

// Binary search (requires sorted array)
let mut sorted = vec![1, 2, 3, 4, 5, 6, 7, 8, 9];
let search_result = sorted.binary_search(&5); // Ok(4)

// Custom search with closure
let first_even = numbers.iter().find(|&&x| x % 2 == 0); // Some(&2)
let first_positive = numbers.iter().find(|&&x| x > 0); // Some(&1)
```

## 🎯 Array Sorting and Ordering

```rust
use tusklang_rust::{array_sorting, ordering};

// Basic sorting
let mut numbers = vec![3, 1, 4, 1, 5, 9, 2, 6];
numbers.sort(); // [1, 1, 2, 3, 4, 5, 6, 9]

// Reverse sorting
numbers.sort_by(|a, b| b.cmp(a)); // [9, 6, 5, 4, 3, 2, 1, 1]

// Custom sorting
#[derive(Debug, Clone)]
struct Person {
    name: String,
    age: u32,
}

let mut people = vec![
    Person { name: "Alice".to_string(), age: 30 },
    Person { name: "Bob".to_string(), age: 25 },
    Person { name: "Charlie".to_string(), age: 35 },
];

// Sort by age
people.sort_by(|a, b| a.age.cmp(&b.age));

// Sort by name
people.sort_by(|a, b| a.name.cmp(&b.name));

// Sort by multiple fields
people.sort_by(|a, b| {
    a.age.cmp(&b.age)
        .then(a.name.cmp(&b.name))
});

// Stable sorting (preserves order of equal elements)
numbers.sort_by_key(|&x| x.abs()); // Sort by absolute value
```

## 🚀 Advanced Array Operations

```rust
use tusklang_rust::{advanced_array, algorithms};

// Array deduplication
let mut numbers = vec![1, 2, 2, 3, 3, 3, 4, 5];
numbers.dedup(); // [1, 2, 3, 4, 5]

// Deduplication with custom logic
let mut people = vec![
    Person { name: "Alice".to_string(), age: 30 },
    Person { name: "Alice".to_string(), age: 30 },
    Person { name: "Bob".to_string(), age: 25 },
];

people.dedup_by_key(|p| p.name.clone()); // Remove duplicates by name

// Array partitioning
let numbers = vec![1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
let (evens, odds): (Vec<i32>, Vec<i32>) = numbers.into_iter()
    .partition(|&x| x % 2 == 0);

// Array chunking
let numbers = vec![1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
let chunks: Vec<Vec<i32>> = numbers.chunks(3)
    .map(|chunk| chunk.to_vec())
    .collect(); // [[1, 2, 3], [4, 5, 6], [7, 8, 9], [10]]

// Array windowing
let windows: Vec<&[i32]> = numbers.windows(3).collect();
// [[1, 2, 3], [2, 3, 4], [3, 4, 5], ...]

// Array rotation
let mut numbers = vec![1, 2, 3, 4, 5];
numbers.rotate_left(2); // [3, 4, 5, 1, 2]
numbers.rotate_right(1); // [2, 3, 4, 5, 1]
```

## 🛡️ Safe Array Operations

```rust
use tusklang_rust::{safe_array, error_handling};

// Safe array access with bounds checking
fn safe_get<T>(array: &[T], index: usize) -> Option<&T> {
    array.get(index)
}

// Safe array modification
fn safe_set<T>(array: &mut [T], index: usize, value: T) -> Result<(), String> {
    if index >= array.len() {
        return Err(format!("Index {} out of bounds for array of length {}", index, array.len()));
    }
    array[index] = value;
    Ok(())
}

// Safe array slicing
fn safe_slice<T>(array: &[T], start: usize, end: usize) -> Result<&[T], String> {
    if start > end {
        return Err("Start index greater than end index".to_string());
    }
    if end > array.len() {
        return Err("End index out of bounds".to_string());
    }
    Ok(&array[start..end])
}

// Safe array concatenation
fn safe_concat<T: Clone>(arrays: &[&[T]], max_length: usize) -> Result<Vec<T>, String> {
    let total_length: usize = arrays.iter().map(|arr| arr.len()).sum();
    
    if total_length > max_length {
        return Err("Concatenation would exceed maximum length".to_string());
    }
    
    let mut result = Vec::with_capacity(total_length);
    for array in arrays {
        result.extend_from_slice(array);
    }
    
    Ok(result)
}

// Custom array error types
#[derive(Debug, thiserror::Error)]
enum ArrayError {
    #[error("Index out of bounds: {index} >= {length}")]
    IndexOutOfBounds { index: usize, length: usize },
    #[error("Empty array")]
    EmptyArray,
    #[error("Invalid range: {start}..{end}")]
    InvalidRange { start: usize, end: usize },
}

fn safe_array_operation<T>(array: &[T], index: usize) -> Result<&T, ArrayError> {
    if array.is_empty() {
        return Err(ArrayError::EmptyArray);
    }
    
    if index >= array.len() {
        return Err(ArrayError::IndexOutOfBounds {
            index,
            length: array.len(),
        });
    }
    
    Ok(&array[index])
}
```

## ⚡ Performance Optimizations

```rust
use tusklang_rust::{performance, optimization};

// Pre-allocated array building
fn build_large_array(size: usize) -> Vec<i32> {
    let mut result = Vec::with_capacity(size);
    
    for i in 0..size {
        result.push(i as i32);
    }
    
    result
}

// Efficient array copying
fn efficient_copy<T: Clone>(source: &[T]) -> Vec<T> {
    let mut result = Vec::with_capacity(source.len());
    result.extend_from_slice(source);
    result
}

// Zero-copy array operations
fn zero_copy_operations<T>(array: &[T]) -> (&[T], &[T]) {
    let mid = array.len() / 2;
    (&array[..mid], &array[mid..])
}

// Array pooling for repeated operations
use std::collections::HashMap;

struct ArrayPool {
    pools: HashMap<usize, Vec<Vec<i32>>>,
}

impl ArrayPool {
    fn new() -> Self {
        Self {
            pools: HashMap::new(),
        }
    }
    
    fn get(&mut self, size: usize) -> Vec<i32> {
        if let Some(pool) = self.pools.get_mut(&size) {
            pool.pop().unwrap_or_else(|| vec![0; size])
        } else {
            vec![0; size]
        }
    }
    
    fn return_array(&mut self, mut array: Vec<i32>) {
        array.clear();
        self.pools.entry(array.capacity()).or_insert_with(Vec::new).push(array);
    }
}

// Lazy array evaluation
struct LazyArray<T> {
    generator: Box<dyn Fn() -> Vec<T>>,
    cached: Option<Vec<T>>,
}

impl<T> LazyArray<T> {
    fn new<F>(generator: F) -> Self
    where
        F: Fn() -> Vec<T> + 'static,
    {
        Self {
            generator: Box::new(generator),
            cached: None,
        }
    }
    
    fn get(&mut self) -> &Vec<T> {
        if self.cached.is_none() {
            self.cached = Some((self.generator)());
        }
        self.cached.as_ref().unwrap()
    }
}
```

## 🎯 Array Algorithms

```rust
use tusklang_rust::{array_algorithms, sorting};

// Quick sort implementation
fn quick_sort<T: Ord + Clone>(array: &mut [T]) {
    if array.len() <= 1 {
        return;
    }
    
    let pivot_index = partition(array);
    quick_sort(&mut array[..pivot_index]);
    quick_sort(&mut array[pivot_index + 1..]);
}

fn partition<T: Ord>(array: &mut [T]) -> usize {
    let len = array.len();
    let pivot_index = len - 1;
    let mut store_index = 0;
    
    for i in 0..len - 1 {
        if array[i] <= array[pivot_index] {
            array.swap(i, store_index);
            store_index += 1;
        }
    }
    
    array.swap(pivot_index, store_index);
    store_index
}

// Binary search implementation
fn binary_search<T: Ord>(array: &[T], target: &T) -> Option<usize> {
    let mut left = 0;
    let mut right = array.len();
    
    while left < right {
        let mid = left + (right - left) / 2;
        
        match array[mid].cmp(target) {
            std::cmp::Ordering::Equal => return Some(mid),
            std::cmp::Ordering::Less => left = mid + 1,
            std::cmp::Ordering::Greater => right = mid,
        }
    }
    
    None
}

// Array flattening
fn flatten<T>(arrays: Vec<Vec<T>>) -> Vec<T> {
    let total_size: usize = arrays.iter().map(|arr| arr.len()).sum();
    let mut result = Vec::with_capacity(total_size);
    
    for array in arrays {
        result.extend(array);
    }
    
    result
}

// Array grouping
fn group_by<T, K, F>(array: &[T], key_fn: F) -> HashMap<K, Vec<&T>>
where
    K: Eq + std::hash::Hash,
    F: Fn(&T) -> K,
{
    let mut groups = HashMap::new();
    
    for item in array {
        let key = key_fn(item);
        groups.entry(key).or_insert_with(Vec::new).push(item);
    }
    
    groups
}
```

## 🔧 Array Utilities

```rust
use tusklang_rust::{array_utils, helpers};

// Array statistics
fn array_stats(array: &[f64]) -> (f64, f64, f64) {
    let len = array.len() as f64;
    let sum: f64 = array.iter().sum();
    let mean = sum / len;
    
    let variance: f64 = array.iter()
        .map(|&x| (x - mean).powi(2))
        .sum::<f64>() / len;
    
    let std_dev = variance.sqrt();
    
    (mean, variance, std_dev)
}

// Array shuffling
use rand::seq::SliceRandom;

fn shuffle_array<T>(array: &mut [T]) {
    let mut rng = rand::thread_rng();
    array.shuffle(&mut rng);
}

// Array sampling
fn sample_array<T: Clone>(array: &[T], sample_size: usize) -> Vec<T> {
    let mut rng = rand::thread_rng();
    array.choose_multiple(&mut rng, sample_size).cloned().collect()
}

// Array validation
fn validate_array<T>(array: &[T], validator: impl Fn(&T) -> bool) -> Result<(), String> {
    for (index, item) in array.iter().enumerate() {
        if !validator(item) {
            return Err(format!("Validation failed at index {}", index));
        }
    }
    Ok(())
}

// Array transformation with error handling
fn transform_array<T, U, E>(
    array: &[T],
    transformer: impl Fn(&T) -> Result<U, E>,
) -> Result<Vec<U>, E> {
    array.iter().map(transformer).collect()
}
```

## 🔗 Related Functions

- `array!()` - Array creation macro
- `vec!()` - Vector creation macro
- `slice!()` - Array slicing macro
- `map!()` - Array mapping macro
- `filter!()` - Array filtering macro
- `sort!()` - Array sorting macro
- `reverse!()` - Array reversal macro

## 🎯 Best Practices

```rust
use tusklang_rust::{best_practices, guidelines};

// 1. Use appropriate collection types
let small_fixed = [1, 2, 3, 4, 5]; // Array for small, fixed-size data
let dynamic = vec![1, 2, 3, 4, 5]; // Vec for dynamic data

// 2. Pre-allocate when you know the size
let mut result = Vec::with_capacity(expected_size);

// 3. Use iterators for functional operations
let doubled: Vec<i32> = numbers.iter().map(|&x| x * 2).collect();

// 4. Handle bounds checking properly
let value = array.get(index).unwrap_or(&default_value);

// 5. Use slice operations when possible
fn process_slice(data: &[i32]) -> Vec<i32> {
    // Process without taking ownership
}

// 6. Consider performance for large arrays
let cached_result = expensive_array_operation.cache();

// 7. Use appropriate sorting algorithms
numbers.sort(); // For simple types
numbers.sort_by(|a, b| a.cmp(b)); // For custom ordering

// 8. Handle empty arrays gracefully
if array.is_empty() {
    return Vec::new();
}
```

**TuskLang Rust: Where array operations meet type safety. Your collection manipulation will never be the same.** 