# Rust Cheat Sheet

## Basic Syntax and Data Types

### Primitive Types
```rust
// Integer types
let integer: i32 = 42;                       // 32-bit signed integer
let unsigned: u32 = 42;                      // 32-bit unsigned integer
let long: i64 = 1234567890;                  // 64-bit signed integer
let ulong: u64 = 1234567890;                 // 64-bit unsigned integer
let small: i8 = 127;                         // 8-bit signed integer
let usmall: u8 = 255;                        // 8-bit unsigned integer

// Floating point types
let float: f32 = 3.14;                       // 32-bit floating point
let double: f64 = 3.14;                      // 64-bit floating point

// Boolean type
let flag: bool = true;                       // true or false

// Character type
let character: char = 'A';                   // Unicode scalar value

// String types
let string: String = String::from("Hello");  // Owned string
let str_slice: &str = "World";               // String slice
let string_literal: &'static str = "Static"; // Static string

// Unit type
let unit: () = ();                           // Unit type (empty tuple)
```

### Compound Types
```rust
// Tuples
let tuple: (i32, f64, &str) = (1, 2.0, "three"); // Fixed-size tuple
let (x, y, z) = tuple;                       // Destructuring
let first = tuple.0;                         // Access by index

// Arrays
let array: [i32; 5] = [1, 2, 3, 4, 5];      // Fixed-size array
let array_slice: &[i32] = &array[1..4];      // Array slice
let repeated = [0; 10];                      // Array with repeated values

// Vectors
let mut vector: Vec<i32> = Vec::new();       // Dynamic array
let vector_literal = vec![1, 2, 3, 4, 5];   // Vector literal
vector.push(6);                              // Add element
vector.pop();                                // Remove last element
```

## Variables and Operators

### Variable Declaration
```rust
// Immutable variable (default)
let x = 5;                                   // Type inference
let y: i32 = 10;                             // Explicit type

// Mutable variable
let mut z = 15;                              // Mutable binding
z = 20;                                      // Can be reassigned

// Constants
const MAX_POINTS: u32 = 100_000;             // Compile-time constant
static GLOBAL: &str = "Global";              // Static variable

// Shadowing
let x = 5;                                   // Original x
let x = x + 1;                               // Shadow x with new value
let x = x * 2;                               // Shadow again
```

### Operators
```rust
// Arithmetic operators
let sum = a + b;                             // Addition
let difference = a - b;                      // Subtraction
let product = a * b;                         // Multiplication
let quotient = a / b;                        // Division
let remainder = a % b;                       // Modulo

// Comparison operators
let equal = a == b;                          // Equality
let not_equal = a != b;                      // Inequality
let greater = a > b;                         // Greater than
let less = a < b;                            // Less than
let greater_equal = a >= b;                  // Greater or equal
let less_equal = a <= b;                     // Less or equal

// Logical operators
let and = a && b;                            // Logical AND
let or = a || b;                             // Logical OR
let not = !a;                                // Logical NOT

// Bitwise operators
let bit_and = a & b;                         // Bitwise AND
let bit_or = a | b;                          // Bitwise OR
let bit_xor = a ^ b;                         // Bitwise XOR
let bit_not = !a;                            // Bitwise NOT
let left_shift = a << 2;                     // Left shift
let right_shift = a >> 2;                    // Right shift

// Assignment operators
let mut value = 10;
value += 5;                                  // value = value + 5
value -= 3;                                  // value = value - 3
value *= 2;                                  // value = value * 2
value /= 4;                                  // value = value / 4
value %= 3;                                  // value = value % 3
```

## Control Structures

### Conditional Statements
```rust
// If-else statement
if number < 5 {
    println!("number is less than 5");
} else if number < 10 {
    println!("number is less than 10");
} else {
    println!("number is 10 or greater");
}

// If expression (returns a value)
let number = if condition { 5 } else { 6 };

// Match expression
let value = match number {
    1 => "one",
    2 => "two",
    3 => "three",
    _ => "something else",                   // Default case
};

// Match with patterns
let message = match value {
    Some(x) if x < 5 => "small",
    Some(x) if x < 10 => "medium",
    Some(_) => "large",
    None => "no value",
};
```

### Loops
```rust
// Loop (infinite)
loop {
    println!("infinite loop");
    break;                                   // Exit loop
}

// While loop
while condition {
    println!("while loop");
}

// For loop
for i in 0..10 {                             // Range 0 to 9
    println!("{}", i);
}

// For loop with iterator
for item in vec![1, 2, 3, 4, 5] {
    println!("{}", item);
}

// For loop with enumerate
for (index, value) in vec![1, 2, 3].iter().enumerate() {
    println!("{}: {}", index, value);
}

// Loop labels (for nested loops)
'outer: loop {
    'inner: loop {
        break 'outer;                        // Break outer loop
    }
}

// Continue and break
for i in 0..10 {
    if i == 5 {
        continue;                            // Skip iteration
    }
    if i == 8 {
        break;                               // Exit loop
    }
    println!("{}", i);
}
```

## Functions/Methods

### Function Declaration
```rust
// Basic function
fn greet() {
    println!("Hello, world!");
}

// Function with parameters
fn add(a: i32, b: i32) -> i32 {
    a + b                                    // Return value (no semicolon)
}

// Function with multiple return values
fn divide(a: f64, b: f64) -> (f64, bool) {
    if b == 0.0 {
        (0.0, false)                         // Division by zero
    } else {
        (a / b, true)                        // Successful division
    }
}

// Function with default parameters (using Option)
fn greet_with_name(name: Option<&str>) {
    match name {
        Some(n) => println!("Hello, {}!", n),
        None => println!("Hello, stranger!"),
    }
}

// Generic function
fn find_max<T: PartialOrd>(list: &[T]) -> Option<&T> {
    list.iter().max()
}

// Closure (anonymous function)
let add_one = |x: i32| x + 1;
let multiply = |x, y| x * y;                 // Type inference
let complex = |x| {
    let y = x * 2;
    y + 1
};
```

### Method Implementation
```rust
struct Rectangle {
    width: u32,
    height: u32,
}

impl Rectangle {
    // Associated function (like static method)
    fn new(width: u32, height: u32) -> Rectangle {
        Rectangle { width, height }
    }

    // Method (takes &self)
    fn area(&self) -> u32 {
        self.width * self.height
    }

    // Mutable method (takes &mut self)
    fn resize(&mut self, width: u32, height: u32) {
        self.width = width;
        self.height = height;
    }

    // Consuming method (takes self)
    fn into_tuple(self) -> (u32, u32) {
        (self.width, self.height)
    }
}
```

## Data Structures

### Structs
```rust
// Tuple struct
struct Point(i32, i32);

// Named struct
struct Person {
    name: String,
    age: u32,
    email: Option<String>,
}

// Unit struct
struct UnitStruct;

// Creating instances
let point = Point(10, 20);
let person = Person {
    name: String::from("Alice"),
    age: 30,
    email: Some(String::from("alice@example.com")),
};

// Field access
let x = point.0;
let name = person.name.clone();
```

### Enums
```rust
// Basic enum
enum Direction {
    North,
    South,
    East,
    West,
}

// Enum with data
enum Message {
    Quit,                                    // No data
    Move { x: i32, y: i32 },                // Named fields
    Write(String),                          // Single value
    ChangeColor(i32, i32, i32),             // Multiple values
}

// Using enums
let direction = Direction::North;
let message = Message::Write(String::from("hello"));

// Pattern matching with enums
match message {
    Message::Quit => println!("Quit"),
    Message::Move { x, y } => println!("Move to ({}, {})", x, y),
    Message::Write(text) => println!("Write: {}", text),
    Message::ChangeColor(r, g, b) => println!("Color: ({}, {}, {})", r, g, b),
}
```

### Collections
```rust
// Vector operations
let mut vec = Vec::new();
vec.push(1);                                // Add element
vec.pop();                                  // Remove last element
vec.insert(0, 10);                          // Insert at index
vec.remove(0);                              // Remove at index
let len = vec.len();                        // Get length
let is_empty = vec.is_empty();              // Check if empty

// HashMap operations
use std::collections::HashMap;
let mut map = HashMap::new();
map.insert("key", "value");                 // Insert key-value
map.get("key");                             // Get value
map.remove("key");                          // Remove key-value
map.contains_key("key");                    // Check if key exists

// HashSet operations
use std::collections::HashSet;
let mut set = HashSet::new();
set.insert(1);                              // Add element
set.remove(&1);                             // Remove element
set.contains(&1);                           // Check if contains
```

## Common Built-in Functions

### String Operations
```rust
let text = String::from("Hello, World!");

// String methods
let len = text.len();                       // Get length
let is_empty = text.is_empty();             // Check if empty
let upper = text.to_uppercase();            // Convert to uppercase
let lower = text.to_lowercase();            // Convert to lowercase
let trimmed = text.trim();                  // Remove whitespace
let replaced = text.replace("World", "Rust"); // Replace substring
let starts = text.starts_with("Hello");     // Check prefix
let ends = text.ends_with("!");             // Check suffix
let contains = text.contains("World");      // Check substring

// String slicing
let slice = &text[0..5];                    // Get slice (bytes)
let chars: Vec<char> = text.chars().collect(); // Get characters
let words: Vec<&str> = text.split(' ').collect(); // Split by delimiter

// String formatting
let formatted = format!("Hello, {}!", "Rust"); // Format string
let interpolated = format!("x = {}, y = {}", 10, 20); // Multiple values
```

### Iterator Methods
```rust
let numbers = vec![1, 2, 3, 4, 5];

// Common iterator methods
let doubled: Vec<i32> = numbers.iter().map(|x| x * 2).collect(); // Transform
let filtered: Vec<i32> = numbers.iter().filter(|x| x > &2).cloned().collect(); // Filter
let sum: i32 = numbers.iter().sum();        // Sum all elements
let product: i32 = numbers.iter().product(); // Product of all elements
let max = numbers.iter().max();             // Find maximum
let min = numbers.iter().min();             // Find minimum
let any = numbers.iter().any(|x| x > &3);   // Check if any element matches
let all = numbers.iter().all(|x| x > &0);   // Check if all elements match
let find = numbers.iter().find(|x| x > &3); // Find first matching element
let position = numbers.iter().position(|x| x > &3); // Find position
```

### Math Operations
```rust
// Standard math functions
let abs = (-5).abs();                       // Absolute value
let max = 5.max(10);                        // Maximum of two values
let min = 5.min(10);                        // Minimum of two values
let pow = 2i32.pow(3);                      // Exponentiation
let sqrt = (16.0_f64).sqrt();               // Square root
let round = 3.7_f64.round();                // Round to nearest
let ceil = 3.2_f64.ceil();                  // Round up
let floor = 3.8_f64.floor();                // Round down

// Random numbers
use rand::Rng;
let mut rng = rand::thread_rng();
let random_int: i32 = rng.gen_range(1..101); // Random integer
let random_float: f64 = rng.gen();          // Random float
```

## File I/O Operations

### File Operations
```rust
use std::fs;
use std::io::{self, Read, Write, BufRead, BufReader, BufWriter};

// Read file
let content = fs::read_to_string("file.txt")?; // Read as string
let bytes = fs::read("file.bin")?;          // Read as bytes
let lines: Vec<String> = fs::read_to_string("file.txt")?
    .lines()
    .map(String::from)
    .collect();                             // Read lines

// Write file
fs::write("file.txt", "content")?;          // Write string
fs::write("file.bin", bytes)?;              // Write bytes

// Append to file
use std::fs::OpenOptions;
let mut file = OpenOptions::new()
    .append(true)
    .open("file.txt")?;
writeln!(file, "new line")?;                // Append line

// File metadata
let metadata = fs::metadata("file.txt")?;
let size = metadata.len();                  // File size
let is_file = metadata.is_file();           // Check if file
let is_dir = metadata.is_dir();             // Check if directory
let modified = metadata.modified()?;        // Last modified time
```

### Stream Operations
```rust
// Read with buffered reader
let file = fs::File::open("file.txt")?;
let reader = BufReader::new(file);
for line in reader.lines() {
    println!("{}", line?);
}

// Write with buffered writer
let file = fs::File::create("output.txt")?;
let mut writer = BufWriter::new(file);
writeln!(writer, "Line 1")?;
writeln!(writer, "Line 2")?;
writer.flush()?;                            // Ensure all data is written

// Binary operations
let mut file = fs::File::open("file.bin")?;
let mut buffer = [0; 1024];
let bytes_read = file.read(&mut buffer)?;   // Read into buffer
```

### Directory Operations
```rust
// Directory operations
fs::create_dir("new_dir")?;                 // Create directory
fs::create_dir_all("path/to/dir")?;         // Create nested directories
fs::remove_dir("dir")?;                     // Remove empty directory
fs::remove_dir_all("dir")?;                 // Remove directory and contents

// List directory contents
for entry in fs::read_dir(".")? {
    let entry = entry?;
    let path = entry.path();
    let name = entry.file_name();
    println!("{:?}", name);
}

// File operations
fs::copy("source.txt", "dest.txt")?;        // Copy file
fs::rename("old.txt", "new.txt")?;          // Rename/move file
fs::remove_file("file.txt")?;               // Delete file
```

## Error Handling

### Result and Option
```rust
// Result type
fn divide(a: f64, b: f64) -> Result<f64, String> {
    if b == 0.0 {
        Err("Division by zero".to_string())
    } else {
        Ok(a / b)
    }
}

// Using Result
match divide(10.0, 2.0) {
    Ok(result) => println!("Result: {}", result),
    Err(e) => println!("Error: {}", e),
}

// Using ? operator
fn process_file() -> Result<String, io::Error> {
    let content = fs::read_to_string("file.txt")?; // Propagate error
    Ok(content)
}

// Option type
fn find_user(id: u32) -> Option<String> {
    if id == 1 {
        Some("Alice".to_string())
    } else {
        None
    }
}

// Using Option
match find_user(1) {
    Some(name) => println!("User: {}", name),
    None => println!("User not found"),
}

// Option methods
let user = find_user(1);
let name = user.unwrap_or("Unknown".to_string()); // Default value
let name = user.expect("User should exist");      // Panic with message
let name = user.unwrap();                         // Panic if None
```

### Custom Error Types
```rust
use thiserror::Error;

#[derive(Error, Debug)]
enum MyError {
    #[error("IO error: {0}")]
    Io(#[from] io::Error),
    
    #[error("Parse error: {0}")]
    Parse(String),
    
    #[error("Validation error: {field}")]
    Validation { field: String },
}

// Using custom error
fn process_data() -> Result<String, MyError> {
    let content = fs::read_to_string("file.txt")?; // Convert io::Error
    if content.is_empty() {
        return Err(MyError::Validation { 
            field: "content".to_string() 
        });
    }
    Ok(content)
}
```

### Panic and Assertions
```rust
// Panic
panic!("Something went wrong!");             // Panic with message
panic!("Value: {}", 42);                     // Panic with formatted message

// Assertions
assert!(condition, "Condition failed");      // Assert condition
assert_eq!(a, b, "Values not equal");        // Assert equality
assert_ne!(a, b, "Values should not be equal"); // Assert inequality

// Debug assertions (only in debug builds)
debug_assert!(condition);                    // Debug assertion
debug_assert_eq!(a, b);                     // Debug equality assertion

// Unreachable code
unreachable!("This should never happen");    // Mark as unreachable
unimplemented!("Not implemented yet");       // Mark as unimplemented
```

## Key Libraries/Modules

### Collections
```rust
use std::collections::{HashMap, HashSet, BTreeMap, BTreeSet, VecDeque, BinaryHeap};

// HashMap (unordered key-value pairs)
let mut map = HashMap::new();
map.insert("key", "value");
map.get("key");                             // Returns Option<&V>

// BTreeMap (ordered key-value pairs)
let mut btree = BTreeMap::new();
btree.insert("key", "value");

// HashSet (unordered unique values)
let mut set = HashSet::new();
set.insert("value");
set.contains("value");

// BTreeSet (ordered unique values)
let mut btree_set = BTreeSet::new();
btree_set.insert("value");

// VecDeque (double-ended queue)
let mut deque = VecDeque::new();
deque.push_front(1);
deque.push_back(2);
deque.pop_front();
deque.pop_back();

// BinaryHeap (priority queue)
let mut heap = BinaryHeap::new();
heap.push(3);
heap.push(1);
heap.push(4);
heap.pop();                                 // Returns largest value
```

### Iterators
```rust
// Creating iterators
let vec = vec![1, 2, 3, 4, 5];
let iter = vec.iter();                      // Iterator over references
let iter_mut = vec.iter_mut();              // Iterator over mutable references
let into_iter = vec.into_iter();            // Iterator that takes ownership

// Iterator adaptors
let doubled: Vec<i32> = vec.iter()
    .map(|x| x * 2)
    .collect();                             // Transform elements

let filtered: Vec<i32> = vec.iter()
    .filter(|x| x > &&2)
    .cloned()
    .collect();                             // Filter elements

let chained: Vec<i32> = vec.iter()
    .chain([6, 7, 8].iter())
    .cloned()
    .collect();                             // Chain iterators

let zipped: Vec<(i32, char)> = vec.iter()
    .zip(['a', 'b', 'c'].iter())
    .map(|(a, b)| (*a, *b))
    .collect();                             // Zip iterators

// Consumer methods
let sum: i32 = vec.iter().sum();            // Sum all elements
let product: i32 = vec.iter().product();    // Product of all elements
let max = vec.iter().max();                 // Find maximum
let min = vec.iter().min();                 // Find minimum
let count = vec.iter().count();             // Count elements
let any = vec.iter().any(|x| x > &3);       // Check if any matches
let all = vec.iter().all(|x| x > &0);       // Check if all match
let find = vec.iter().find(|x| x > &3);     // Find first match
let position = vec.iter().position(|x| x > &3); // Find position
```

### Async/Await
```rust
use tokio;
use std::future::Future;

// Async function
async fn fetch_data() -> Result<String, Box<dyn std::error::Error>> {
    // Simulate async operation
    tokio::time::sleep(tokio::time::Duration::from_secs(1)).await;
    Ok("Data".to_string())
}

// Using async functions
#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    let data = fetch_data().await?;
    println!("{}", data);
    Ok(())
}

// Multiple async operations
async fn process_multiple() -> Result<Vec<String>, Box<dyn std::error::Error>> {
    let futures = vec![
        fetch_data(),
        fetch_data(),
        fetch_data(),
    ];
    
    let results = futures::future::join_all(futures).await;
    let mut data = Vec::new();
    for result in results {
        data.push(result?);
    }
    Ok(data)
}

// Async with timeout
use tokio::time::{timeout, Duration};

async fn fetch_with_timeout() -> Result<String, Box<dyn std::error::Error>> {
    let result = timeout(Duration::from_secs(5), fetch_data()).await?;
    Ok(result?)
}
```

### Serde (Serialization)
```rust
use serde::{Deserialize, Serialize};

#[derive(Serialize, Deserialize, Debug)]
struct Person {
    name: String,
    age: u32,
    email: Option<String>,
}

// JSON serialization
let person = Person {
    name: "Alice".to_string(),
    age: 30,
    email: Some("alice@example.com".to_string()),
};

let json = serde_json::to_string(&person)?;  // Serialize to JSON
let deserialized: Person = serde_json::from_str(&json)?; // Deserialize from JSON

// YAML serialization
let yaml = serde_yaml::to_string(&person)?;  // Serialize to YAML
let deserialized: Person = serde_yaml::from_str(&yaml)?; // Deserialize from YAML

// TOML serialization
let toml = toml::to_string(&person)?;        // Serialize to TOML
let deserialized: Person = toml::from_str(&toml)?; // Deserialize from TOML
```

### Logging
```rust
use log::{info, warn, error, debug, trace};

// Initialize logger
env_logger::init();

// Logging levels
trace!("Trace message");                     // Most verbose
debug!("Debug message");                     // Debug information
info!("Info message");                       // General information
warn!("Warning message");                    // Warning
error!("Error message");                     // Error

// Structured logging
info!(target: "app", "User logged in: {}", username);
warn!(target: "database", "Connection slow: {}ms", duration);

// Log with context
log::info!("Processing request", {
    user_id: user_id,
    request_id: request_id,
});
```

This cheat sheet covers the essential Rust syntax and features. For more advanced topics, refer to the official Rust documentation and "The Rust Programming Language" book. 