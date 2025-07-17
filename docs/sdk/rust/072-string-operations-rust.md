# 🦀 String Operations in TuskLang Rust

**"We don't bow to any king" - String Manipulation Edition**

TuskLang Rust provides powerful string operations that leverage Rust's ownership system and zero-copy abstractions. Say goodbye to string buffer overflows and hello to compile-time safety with efficient string manipulation.

## 🚀 Basic String Operations

```rust
use tusklang_rust::{string_ops, String, str};

// String creation
let empty_string = String::new();
let from_literal = "Hello, World!".to_string();
let from_str = String::from("Hello, World!");

// String concatenation
let mut greeting = "Hello".to_string();
greeting.push_str(", World!");
greeting.push('!');

// String interpolation with format!
let name = "Alice";
let age = 30;
let message = format!("Hello, {}! You are {} years old.", name, age);

// String length and capacity
let text = "Hello, World!";
let length = text.len(); // 13
let capacity = text.capacity(); // Current capacity

// String slicing
let hello = &text[0..5]; // "Hello"
let world = &text[7..12]; // "World"
```

## 🎯 String Manipulation

```rust
use tusklang_rust::{string_manipulation, methods};

// Case conversion
let text = "Hello, World!";
let uppercase = text.to_uppercase(); // "HELLO, WORLD!"
let lowercase = text.to_lowercase(); // "hello, world!"

// Trimming whitespace
let padded = "   Hello, World!   ";
let trimmed = padded.trim(); // "Hello, World!"
let trimmed_start = padded.trim_start(); // "Hello, World!   "
let trimmed_end = padded.trim_end(); // "   Hello, World!"

// String replacement
let original = "Hello, World!";
let replaced = original.replace("World", "Rust"); // "Hello, Rust!"
let replaced_all = original.replace("l", "L"); // "HeLLo, WorLd!"

// String splitting
let csv = "apple,banana,cherry";
let fruits: Vec<&str> = csv.split(',').collect();
let lines: Vec<&str> = "line1\nline2\nline3".lines().collect();

// String joining
let words = vec!["Hello", "World", "Rust"];
let joined = words.join(" "); // "Hello World Rust"
let concatenated = words.concat(); // "HelloWorldRust"
```

## ⚡ String Searching and Matching

```rust
use tusklang_rust::{string_search, pattern_matching};

// Contains check
let text = "Hello, World!";
let contains_hello = text.contains("Hello"); // true
let contains_rust = text.contains("Rust"); // false

// Starts with and ends with
let starts_with_hello = text.starts_with("Hello"); // true
let ends_with_world = text.ends_with("World!"); // true

// Find substring position
let position = text.find("World"); // Some(7)
let rposition = text.rfind("l"); // Some(10)

// Pattern matching with regex
use regex::Regex;

let re = Regex::new(r"\d+").unwrap();
let text_with_numbers = "Hello 123 World 456";
let numbers: Vec<&str> = re.find_iter(text_with_numbers)
    .map(|m| m.as_str())
    .collect(); // ["123", "456"]

// Custom pattern matching
let text = "Hello123World456";
let mut current_number = String::new();
let mut numbers = Vec::new();

for ch in text.chars() {
    if ch.is_ascii_digit() {
        current_number.push(ch);
    } else if !current_number.is_empty() {
        numbers.push(current_number.clone());
        current_number.clear();
    }
}
if !current_number.is_empty() {
    numbers.push(current_number);
}
```

## 🔧 String Parsing and Conversion

```rust
use tusklang_rust::{string_parsing, conversion};

// Parse numbers from strings
let number_str = "42";
let number: i32 = number_str.parse().unwrap(); // 42

// Safe parsing with Result
let parse_result: Result<i32, _> = "42".parse();
let parse_error: Result<i32, _> = "not_a_number".parse();

// Parse with default values
let number = "42".parse::<i32>().unwrap_or(0);
let float = "3.14".parse::<f64>().unwrap_or(0.0);

// String to number conversion with validation
fn safe_parse_int(s: &str) -> Result<i32, String> {
    s.trim().parse::<i32>()
        .map_err(|e| format!("Failed to parse '{}': {}", s, e))
}

// Number to string conversion
let number = 42;
let string_number = number.to_string();
let formatted_number = format!("{:05}", number); // "00042"

// Custom formatting
let price = 19.99;
let formatted_price = format!("${:.2}", price); // "$19.99"
let percentage = 0.85;
let formatted_percentage = format!("{:.1}%", percentage * 100.0); // "85.0%"
```

## 🎯 String Validation and Sanitization

```rust
use tusklang_rust::{string_validation, sanitization};

// Email validation
fn is_valid_email(email: &str) -> bool {
    let email_regex = Regex::new(r"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$").unwrap();
    email_regex.is_match(email)
}

// URL validation
fn is_valid_url(url: &str) -> bool {
    url.starts_with("http://") || url.starts_with("https://")
}

// String sanitization
fn sanitize_html(input: &str) -> String {
    input
        .replace("&", "&amp;")
        .replace("<", "&lt;")
        .replace(">", "&gt;")
        .replace("\"", "&quot;")
        .replace("'", "&#x27;")
}

// Remove special characters
fn remove_special_chars(input: &str) -> String {
    input.chars()
        .filter(|c| c.is_alphanumeric() || c.is_whitespace())
        .collect()
}

// Normalize whitespace
fn normalize_whitespace(input: &str) -> String {
    input.split_whitespace().collect::<Vec<&str>>().join(" ")
}

// Validate string length
fn validate_string_length(s: &str, min: usize, max: usize) -> Result<(), String> {
    let len = s.len();
    if len < min {
        Err(format!("String too short: {} < {}", len, min))
    } else if len > max {
        Err(format!("String too long: {} > {}", len, max))
    } else {
        Ok(())
    }
}
```

## 🚀 Advanced String Operations

```rust
use tusklang_rust::{advanced_string, algorithms};

// String similarity (Levenshtein distance)
fn levenshtein_distance(s1: &str, s2: &str) -> usize {
    let len1 = s1.chars().count();
    let len2 = s2.chars().count();
    
    if len1 == 0 { return len2; }
    if len2 == 0 { return len1; }
    
    let mut matrix = vec![vec![0; len2 + 1]; len1 + 1];
    
    for i in 0..=len1 {
        matrix[i][0] = i;
    }
    for j in 0..=len2 {
        matrix[0][j] = j;
    }
    
    for (i, c1) in s1.chars().enumerate() {
        for (j, c2) in s2.chars().enumerate() {
            let cost = if c1 == c2 { 0 } else { 1 };
            matrix[i + 1][j + 1] = (matrix[i][j + 1] + 1)
                .min(matrix[i + 1][j] + 1)
                .min(matrix[i][j] + cost);
        }
    }
    
    matrix[len1][len2]
}

// String tokenization
fn tokenize(text: &str) -> Vec<String> {
    text.split_whitespace()
        .map(|s| s.to_lowercase())
        .filter(|s| !s.is_empty())
        .collect()
}

// String compression (simple RLE)
fn compress_rle(input: &str) -> String {
    if input.is_empty() {
        return String::new();
    }
    
    let mut result = String::new();
    let mut current_char = input.chars().next().unwrap();
    let mut count = 1;
    
    for ch in input.chars().skip(1) {
        if ch == current_char {
            count += 1;
        } else {
            if count > 1 {
                result.push_str(&count.to_string());
            }
            result.push(current_char);
            current_char = ch;
            count = 1;
        }
    }
    
    if count > 1 {
        result.push_str(&count.to_string());
    }
    result.push(current_char);
    
    result
}

// String decompression (simple RLE)
fn decompress_rle(input: &str) -> String {
    let mut result = String::new();
    let mut chars = input.chars();
    
    while let Some(ch) = chars.next() {
        if ch.is_ascii_digit() {
            let mut count_str = ch.to_string();
            while let Some(next_ch) = chars.next() {
                if next_ch.is_ascii_digit() {
                    count_str.push(next_ch);
                } else {
                    let count: usize = count_str.parse().unwrap_or(1);
                    result.push_str(&next_ch.to_string().repeat(count));
                    break;
                }
            }
        } else {
            result.push(ch);
        }
    }
    
    result
}
```

## 🛡️ Safe String Operations

```rust
use tusklang_rust::{safe_string, error_handling};

// Safe string slicing with bounds checking
fn safe_slice(s: &str, start: usize, end: usize) -> Result<&str, String> {
    if start > end {
        return Err("Start index greater than end index".to_string());
    }
    if end > s.len() {
        return Err("End index out of bounds".to_string());
    }
    Ok(&s[start..end])
}

// Safe string replacement
fn safe_replace(s: &str, from: &str, to: &str) -> String {
    if from.is_empty() {
        return s.to_string();
    }
    s.replace(from, to)
}

// Safe string concatenation with size limits
fn safe_concat(strings: &[&str], max_length: usize) -> Result<String, String> {
    let mut result = String::new();
    
    for s in strings {
        if result.len() + s.len() > max_length {
            return Err("Concatenation would exceed maximum length".to_string());
        }
        result.push_str(s);
    }
    
    Ok(result)
}

// Safe string parsing with custom error types
#[derive(Debug, thiserror::Error)]
enum StringParseError {
    #[error("Empty string")]
    Empty,
    #[error("Invalid format: {0}")]
    InvalidFormat(String),
    #[error("Value out of range: {0}")]
    OutOfRange(String),
}

fn safe_parse_positive_int(s: &str) -> Result<i32, StringParseError> {
    if s.is_empty() {
        return Err(StringParseError::Empty);
    }
    
    let number: i32 = s.parse()
        .map_err(|_| StringParseError::InvalidFormat(s.to_string()))?;
    
    if number < 0 {
        return Err(StringParseError::OutOfRange(s.to_string()));
    }
    
    Ok(number)
}
```

## ⚡ Performance Optimizations

```rust
use tusklang_rust::{performance, optimization};

// Pre-allocated string building
fn build_large_string(parts: &[&str]) -> String {
    let total_length: usize = parts.iter().map(|s| s.len()).sum();
    let mut result = String::with_capacity(total_length);
    
    for part in parts {
        result.push_str(part);
    }
    
    result
}

// String interning for repeated strings
use std::collections::HashMap;
use std::sync::Mutex;
use once_cell::sync::Lazy;

static STRING_CACHE: Lazy<Mutex<HashMap<String, String>>> = Lazy::new(|| {
    Mutex::new(HashMap::new())
});

fn intern_string(s: &str) -> String {
    let mut cache = STRING_CACHE.lock().unwrap();
    if let Some(cached) = cache.get(s) {
        cached.clone()
    } else {
        let owned = s.to_string();
        cache.insert(owned.clone(), owned.clone());
        owned
    }
}

// Efficient string comparison
fn efficient_string_compare(a: &str, b: &str) -> bool {
    if a.len() != b.len() {
        return false;
    }
    
    a.bytes().zip(b.bytes()).all(|(a_byte, b_byte)| a_byte == b_byte)
}

// Lazy string evaluation
struct LazyString {
    parts: Vec<String>,
    cached: Option<String>,
}

impl LazyString {
    fn new() -> Self {
        Self {
            parts: Vec::new(),
            cached: None,
        }
    }
    
    fn add_part(&mut self, part: String) {
        self.parts.push(part);
        self.cached = None; // Invalidate cache
    }
    
    fn to_string(&mut self) -> String {
        if let Some(ref cached) = self.cached {
            cached.clone()
        } else {
            let result = self.parts.join("");
            self.cached = Some(result.clone());
            result
        }
    }
}
```

## 🎯 String Templates and Formatting

```rust
use tusklang_rust::{templates, formatting};

// Template engine
struct StringTemplate {
    template: String,
    variables: HashMap<String, String>,
}

impl StringTemplate {
    fn new(template: String) -> Self {
        Self {
            template,
            variables: HashMap::new(),
        }
    }
    
    fn set_variable(&mut self, name: &str, value: &str) {
        self.variables.insert(name.to_string(), value.to_string());
    }
    
    fn render(&self) -> String {
        let mut result = self.template.clone();
        
        for (name, value) in &self.variables {
            let placeholder = format!("{{{{{}}}}}", name);
            result = result.replace(&placeholder, value);
        }
        
        result
    }
}

// Usage
let mut template = StringTemplate::new(
    "Hello, {name}! You are {age} years old.".to_string()
);
template.set_variable("name", "Alice");
template.set_variable("age", "30");
let rendered = template.render(); // "Hello, Alice! You are 30 years old."

// Advanced formatting with custom types
#[derive(Debug)]
struct Person {
    name: String,
    age: u32,
}

impl std::fmt::Display for Person {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        write!(f, "{} ({})", self.name, self.age)
    }
}

let person = Person {
    name: "Alice".to_string(),
    age: 30,
};
let formatted = format!("Person: {}", person); // "Person: Alice (30)"
```

## 🔗 Related Functions

- `concat!()` - String concatenation macro
- `format!()` - String formatting macro
- `replace!()` - String replacement macro
- `split!()` - String splitting macro
- `join!()` - String joining macro
- `trim!()` - String trimming macro
- `case!()` - Case conversion macro

## 🎯 Best Practices

```rust
use tusklang_rust::{best_practices, guidelines};

// 1. Use appropriate string types
let static_str = "Hello"; // &'static str
let owned_string = "Hello".to_string(); // String
let borrowed_str = &owned_string; // &str

// 2. Pre-allocate strings when building large ones
let mut result = String::with_capacity(expected_length);

// 3. Use string slices when possible
fn process_text(text: &str) -> String {
    // Process without taking ownership
}

// 4. Handle UTF-8 properly
let unicode_text = "Hello, 世界!";
let char_count = unicode_text.chars().count(); // 9, not 13

// 5. Use iterators for string processing
let words: Vec<&str> = text.split_whitespace().collect();

// 6. Validate input strings
fn process_user_input(input: &str) -> Result<String, String> {
    if input.is_empty() {
        return Err("Input cannot be empty".to_string());
    }
    Ok(input.to_string())
}

// 7. Use const for static strings
const GREETING: &str = "Hello, World!";

// 8. Consider performance for repeated operations
let cached_result = expensive_string_operation.cache();
```

**TuskLang Rust: Where string operations meet type safety. Your text processing will never be the same.** 