# 🦀 Math Operations in TuskLang Rust

**"We don't bow to any king" - Mathematics Edition**

TuskLang Rust provides powerful mathematical operations that leverage Rust's type system and zero-cost abstractions. Say goodbye to floating-point precision errors and hello to compile-time safety with mathematical guarantees.

## 🚀 Basic Arithmetic Operators

```rust
use tusklang_rust::{math, arithmetic};

// Addition with type safety
let sum = a + b;
let result: i32 = 10 + 20; // 30

// Subtraction
let difference = a - b;
let negative: i32 = 5 - 10; // -5

// Multiplication
let product = a * b;
let area: f64 = 5.5 * 3.2; // 17.6

// Division
let quotient = a / b;
let division: f64 = 10.0 / 3.0; // 3.333...

// Modulo (remainder)
let remainder = a % b;
let modulo: i32 = 17 % 5; // 2

// Power (using standard library)
let power = a.pow(b);
let squared: i32 = 5_i32.pow(2); // 25
```

## 🎯 Advanced Mathematical Operations

```rust
use tusklang_rust::{advanced_math, scientific};

// Square root
let sqrt = (a as f64).sqrt();
let root: f64 = 16.0.sqrt(); // 4.0

// Power with floating point
let power_float = (a as f64).powf(b as f64);
let cube: f64 = 3.0.powf(3.0); // 27.0

// Natural logarithm
let ln = (a as f64).ln();
let natural_log: f64 = std::f64::consts::E.ln(); // 1.0

// Base-10 logarithm
let log10 = (a as f64).log10();
let log_100: f64 = 100.0.log10(); // 2.0

// Exponential
let exp = (a as f64).exp();
let e_power: f64 = 1.0.exp(); // 2.718...

// Absolute value
let abs = a.abs();
let absolute: i32 = (-42).abs(); // 42

// Ceiling and floor
let ceiling = (a as f64).ceil();
let floor = (a as f64).floor();
let round = (a as f64).round();
```

## ⚡ Mathematical Constants

```rust
use tusklang_rust::{constants, mathematical};

// Pi constant
let pi = std::f64::consts::PI; // 3.141592653589793

// Euler's number
let e = std::f64::consts::E; // 2.718281828459045

// Golden ratio
let phi = (1.0 + 5.0_f64.sqrt()) / 2.0; // 1.618033988749895

// Square root of 2
let sqrt2 = 2.0_f64.sqrt(); // 1.4142135623730951

// Custom mathematical constants
const GRAVITY: f64 = 9.81;
const SPEED_OF_LIGHT: f64 = 299_792_458.0;
const AVOGADRO: f64 = 6.02214076e23;

// Using constants in calculations
let potential_energy = mass * GRAVITY * height;
let time_dilation = time / (1.0 - (velocity / SPEED_OF_LIGHT).powi(2)).sqrt();
```

## 🔧 Type-Safe Mathematical Operations

```rust
use tusklang_rust::{type_safety, mathematical_types};

// Integer arithmetic with overflow checking
let checked_sum = a.checked_add(b);
let checked_product = a.checked_mul(b);

// Wrapping arithmetic (modular arithmetic)
let wrapping_sum = a.wrapping_add(b);
let wrapping_product = a.wrapping_mul(b);

// Saturating arithmetic (clamps to min/max)
let saturating_sum = a.saturating_add(b);
let saturating_product = a.saturating_mul(b);

// Overflow arithmetic (panics on overflow in debug)
let overflowing_sum = a.overflowing_add(b);
let (result, overflowed) = overflowing_sum;

// Example usage
let max_u32 = u32::MAX;
let overflow_result = max_u32.checked_add(1); // None
let wrapping_result = max_u32.wrapping_add(1); // 0
let saturating_result = max_u32.saturating_add(1); // u32::MAX
```

## 🎯 Mathematical Functions

```rust
use tusklang_rust::{math_functions, trigonometry};

// Trigonometric functions (in radians)
let sine = angle.sin();
let cosine = angle.cos();
let tangent = angle.tan();

// Inverse trigonometric functions
let arcsin = value.asin();
let arccos = value.acos();
let arctan = value.atan();

// Hyperbolic functions
let sinh = value.sinh();
let cosh = value.cosh();
let tanh = value.tanh();

// Utility functions
let min = a.min(b);
let max = a.max(b);
let clamp = value.clamp(min_val, max_val);

// Example: Calculate distance between two points
fn distance(x1: f64, y1: f64, x2: f64, y2: f64) -> f64 {
    let dx = x2 - x1;
    let dy = y2 - y1;
    (dx * dx + dy * dy).sqrt()
}
```

## 🚀 Vector and Matrix Operations

```rust
use tusklang_rust::{vectors, matrices};

// Vector operations
#[derive(Debug, Clone)]
struct Vector2D {
    x: f64,
    y: f64,
}

impl Vector2D {
    fn new(x: f64, y: f64) -> Self {
        Self { x, y }
    }
    
    fn magnitude(&self) -> f64 {
        (self.x * self.x + self.y * self.y).sqrt()
    }
    
    fn normalize(&self) -> Self {
        let mag = self.magnitude();
        Self {
            x: self.x / mag,
            y: self.y / mag,
        }
    }
    
    fn dot(&self, other: &Self) -> f64 {
        self.x * other.x + self.y * other.y
    }
}

// Matrix operations
#[derive(Debug, Clone)]
struct Matrix2x2 {
    data: [[f64; 2]; 2],
}

impl Matrix2x2 {
    fn new(a: f64, b: f64, c: f64, d: f64) -> Self {
        Self {
            data: [[a, b], [c, d]],
        }
    }
    
    fn determinant(&self) -> f64 {
        self.data[0][0] * self.data[1][1] - self.data[0][1] * self.data[1][0]
    }
    
    fn multiply(&self, other: &Self) -> Self {
        let mut result = [[0.0; 2]; 2];
        for i in 0..2 {
            for j in 0..2 {
                for k in 0..2 {
                    result[i][j] += self.data[i][k] * other.data[k][j];
                }
            }
        }
        Self { data: result }
    }
}
```

## 🛡️ Safe Mathematical Operations

```rust
use tusklang_rust::{safe_math, error_handling};

// Safe division with zero checking
fn safe_divide(a: f64, b: f64) -> Result<f64, &'static str> {
    if b == 0.0 {
        Err("Division by zero")
    } else {
        Ok(a / b)
    }
}

// Safe square root with negative checking
fn safe_sqrt(value: f64) -> Result<f64, &'static str> {
    if value < 0.0 {
        Err("Cannot take square root of negative number")
    } else {
        Ok(value.sqrt())
    }
}

// Safe logarithm with domain checking
fn safe_ln(value: f64) -> Result<f64, &'static str> {
    if value <= 0.0 {
        Err("Cannot take logarithm of non-positive number")
    } else {
        Ok(value.ln())
    }
}

// Safe power with overflow checking
fn safe_pow(base: f64, exponent: f64) -> Result<f64, &'static str> {
    let result = base.powf(exponent);
    if result.is_finite() {
        Ok(result)
    } else {
        Err("Power operation resulted in overflow or NaN")
    }
}
```

## ⚡ Performance Optimizations

```rust
use tusklang_rust::{performance, optimization};

// Fast integer power using bit manipulation
fn fast_pow(mut base: i32, mut exponent: u32) -> i32 {
    let mut result = 1;
    while exponent > 0 {
        if exponent & 1 == 1 {
            result *= base;
        }
        base *= base;
        exponent >>= 1;
    }
    result
}

// Fast square root approximation
fn fast_sqrt(value: f64) -> f64 {
    let mut x = value;
    let mut y = 1.0;
    let epsilon = 0.000001;
    
    while (x - y).abs() > epsilon {
        x = (x + y) / 2.0;
        y = value / x;
    }
    x
}

// Lookup table for common calculations
struct MathLookupTable {
    sin_table: Vec<f64>,
    cos_table: Vec<f64>,
}

impl MathLookupTable {
    fn new(resolution: usize) -> Self {
        let mut sin_table = Vec::with_capacity(resolution);
        let mut cos_table = Vec::with_capacity(resolution);
        
        for i in 0..resolution {
            let angle = 2.0 * std::f64::consts::PI * i as f64 / resolution as f64;
            sin_table.push(angle.sin());
            cos_table.push(angle.cos());
        }
        
        Self { sin_table, cos_table }
    }
    
    fn fast_sin(&self, angle: f64) -> f64 {
        let index = ((angle / (2.0 * std::f64::consts::PI)) * self.sin_table.len() as f64) as usize;
        self.sin_table[index % self.sin_table.len()]
    }
}
```

## 🎯 Mathematical Error Handling

```rust
use tusklang_rust::{math_errors, Result};

// Custom mathematical error types
#[derive(Debug, thiserror::Error)]
enum MathError {
    #[error("Division by zero")]
    DivisionByZero,
    #[error("Square root of negative number: {0}")]
    NegativeSquareRoot(f64),
    #[error("Logarithm of non-positive number: {0}")]
    InvalidLogarithm(f64),
    #[error("Overflow in operation: {operation}")]
    Overflow { operation: String },
    #[error("Invalid input: {0}")]
    InvalidInput(String),
}

// Mathematical operations with proper error handling
fn mathematical_operation(a: f64, b: f64, operation: &str) -> Result<f64, MathError> {
    match operation {
        "add" => {
            let result = a + b;
            if result.is_finite() {
                Ok(result)
            } else {
                Err(MathError::Overflow { operation: "addition".to_string() })
            }
        }
        "divide" => {
            if b == 0.0 {
                Err(MathError::DivisionByZero)
            } else {
                Ok(a / b)
            }
        }
        "sqrt" => {
            if a < 0.0 {
                Err(MathError::NegativeSquareRoot(a))
            } else {
                Ok(a.sqrt())
            }
        }
        "log" => {
            if a <= 0.0 {
                Err(MathError::InvalidLogarithm(a))
            } else {
                Ok(a.ln())
            }
        }
        _ => Err(MathError::InvalidInput(operation.to_string())),
    }
}
```

## 🔧 Mathematical Utilities

```rust
use tusklang_rust::{math_utils, helpers};

// Greatest common divisor
fn gcd(mut a: u64, mut b: u64) -> u64 {
    while b != 0 {
        let temp = b;
        b = a % b;
        a = temp;
    }
    a
}

// Least common multiple
fn lcm(a: u64, b: u64) -> u64 {
    a * b / gcd(a, b)
}

// Factorial
fn factorial(n: u64) -> u64 {
    if n <= 1 {
        1
    } else {
        n * factorial(n - 1)
    }
}

// Fibonacci sequence
fn fibonacci(n: u64) -> u64 {
    if n <= 1 {
        n
    } else {
        fibonacci(n - 1) + fibonacci(n - 2)
    }
}

// Prime number checking
fn is_prime(n: u64) -> bool {
    if n < 2 {
        return false;
    }
    if n == 2 {
        return true;
    }
    if n % 2 == 0 {
        return false;
    }
    
    let sqrt_n = (n as f64).sqrt() as u64;
    for i in (3..=sqrt_n).step_by(2) {
        if n % i == 0 {
            return false;
        }
    }
    true
}

// Random number generation
use rand::Rng;

fn random_range(min: f64, max: f64) -> f64 {
    let mut rng = rand::thread_rng();
    rng.gen_range(min..max)
}
```

## 🚀 Advanced Mathematical Patterns

```rust
use tusklang_rust::{advanced_patterns, mathematical};

// Complex number arithmetic
#[derive(Debug, Clone)]
struct Complex {
    real: f64,
    imaginary: f64,
}

impl Complex {
    fn new(real: f64, imaginary: f64) -> Self {
        Self { real, imaginary }
    }
    
    fn magnitude(&self) -> f64 {
        (self.real * self.real + self.imaginary * self.imaginary).sqrt()
    }
    
    fn conjugate(&self) -> Self {
        Self {
            real: self.real,
            imaginary: -self.imaginary,
        }
    }
}

impl std::ops::Add for Complex {
    type Output = Self;
    
    fn add(self, other: Self) -> Self {
        Self {
            real: self.real + other.real,
            imaginary: self.imaginary + other.imaginary,
        }
    }
}

impl std::ops::Mul for Complex {
    type Output = Self;
    
    fn mul(self, other: Self) -> Self {
        Self {
            real: self.real * other.real - self.imaginary * other.imaginary,
            imaginary: self.real * other.imaginary + self.imaginary * other.real,
        }
    }
}

// Polynomial evaluation
fn evaluate_polynomial(coefficients: &[f64], x: f64) -> f64 {
    coefficients.iter()
        .enumerate()
        .map(|(i, &coeff)| coeff * x.powi(i as i32))
        .sum()
}

// Numerical integration (trapezoidal rule)
fn integrate<F>(f: F, a: f64, b: f64, n: usize) -> f64
where
    F: Fn(f64) -> f64,
{
    let h = (b - a) / n as f64;
    let mut sum = (f(a) + f(b)) / 2.0;
    
    for i in 1..n {
        sum += f(a + i as f64 * h);
    }
    
    h * sum
}
```

## 🔗 Related Functions

- `add!()` - Addition macro
- `multiply!()` - Multiplication macro
- `divide!()` - Division macro
- `power!()` - Power macro
- `sqrt!()` - Square root macro
- `sin!()` - Sine macro
- `cos!()` - Cosine macro
- `log!()` - Logarithm macro

## 🎯 Best Practices

```rust
use tusklang_rust::{best_practices, guidelines};

// 1. Use appropriate numeric types
let integer_math = 42_i32 + 58_i32;
let float_math = 3.14_f64 * 2.0_f64;

// 2. Handle overflow and underflow
let checked_result = a.checked_add(b).unwrap_or(0);

// 3. Use constants for mathematical values
const PI: f64 = std::f64::consts::PI;
const E: f64 = std::f64::consts::E;

// 4. Validate inputs for mathematical operations
fn safe_math_operation(a: f64, b: f64) -> Result<f64, MathError> {
    if !a.is_finite() || !b.is_finite() {
        return Err(MathError::InvalidInput("Non-finite input".to_string()));
    }
    Ok(a + b)
}

// 5. Use appropriate precision for calculations
let high_precision = 3.141592653589793_f64;
let low_precision = 3.14_f32;

// 6. Consider performance for repeated calculations
let cached_result = expensive_calculation.cache();

// 7. Use mathematical libraries for complex operations
use num_traits::Float;
let result = value.sqrt().unwrap_or(0.0);

// 8. Handle edge cases explicitly
fn robust_math_operation(a: f64, b: f64) -> f64 {
    match (a, b) {
        (a, b) if a.is_nan() || b.is_nan() => f64::NAN,
        (a, b) if a.is_infinite() || b.is_infinite() => f64::INFINITY,
        _ => a + b,
    }
}
```

**TuskLang Rust: Where mathematical operations meet type safety. Your calculations will never be the same.** 