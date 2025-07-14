# Math Operations in TuskLang

TuskLang provides comprehensive mathematical operations and functions for numeric computations, from basic arithmetic to advanced mathematical functions.

## Basic Arithmetic Operators

```tusk
# Addition (+)
sum: 10 + 5        # 15
total: price + tax  # Variable addition

# Subtraction (-)
difference: 20 - 8  # 12
remaining: total - paid

# Multiplication (*)
product: 6 * 7      # 42
area: width * height

# Division (/)
quotient: 15 / 3    # 5
average: sum / count

# Modulo (%)
remainder: 17 % 5   # 2
is_even: number % 2 == 0

# Exponentiation (**)
power: 2 ** 3       # 8
square: x ** 2
cube: x ** 3
```

## Assignment Operators

```tusk
# Basic assignment
x: 10

# Addition assignment
x += 5      # x = x + 5

# Subtraction assignment
x -= 3      # x = x - 3

# Multiplication assignment
x *= 2      # x = x * 2

# Division assignment
x /= 4      # x = x / 4

# Modulo assignment
x %= 3      # x = x % 3

# Exponentiation assignment
x **= 2     # x = x ** 2

# Increment/Decrement
count++     # Post-increment
++count     # Pre-increment
count--     # Post-decrement
--count     # Pre-decrement
```

## Math Constants

```tusk
# Mathematical constants
Math.PI         # 3.141592653589793
Math.E          # 2.718281828459045
Math.LN2        # Natural log of 2
Math.LN10       # Natural log of 10
Math.LOG2E      # Base 2 log of E
Math.LOG10E     # Base 10 log of E
Math.SQRT1_2    # Square root of 1/2
Math.SQRT2      # Square root of 2

# Using constants
circle_area: Math.PI * radius ** 2
circle_circumference: 2 * Math.PI * radius
```

## Rounding Functions

```tusk
# Round to nearest integer
Math.round(4.7)     # 5
Math.round(4.4)     # 4
Math.round(-4.5)    # -4

# Round down (floor)
Math.floor(4.9)     # 4
Math.floor(-4.1)    # -5

# Round up (ceiling)
Math.ceil(4.1)      # 5
Math.ceil(-4.9)     # -4

# Truncate decimal part
Math.trunc(4.9)     # 4
Math.trunc(-4.9)    # -4

# Round to decimal places
round_to: (num, places) => {
    multiplier: 10 ** places
    return Math.round(num * multiplier) / multiplier
}

round_to(3.14159, 2)  # 3.14
round_to(3.14159, 4)  # 3.1416
```

## Mathematical Functions

```tusk
# Absolute value
Math.abs(-10)       # 10
Math.abs(5)         # 5

# Square root
Math.sqrt(16)       # 4
Math.sqrt(2)        # 1.4142135623730951

# Cube root
Math.cbrt(27)       # 3
Math.cbrt(8)        # 2

# Power functions
Math.pow(2, 3)      # 8 (same as 2 ** 3)
Math.exp(1)         # e^1 = 2.718281828459045

# Logarithms
Math.log(Math.E)    # 1 (natural log)
Math.log10(100)     # 2 (base 10 log)
Math.log2(8)        # 3 (base 2 log)

# Sign function
Math.sign(5)        # 1
Math.sign(-5)       # -1
Math.sign(0)        # 0
```

## Trigonometric Functions

```tusk
# Basic trig functions (radians)
Math.sin(Math.PI / 2)   # 1
Math.cos(Math.PI)       # -1
Math.tan(Math.PI / 4)   # 1

# Inverse trig functions
Math.asin(1)        # π/2
Math.acos(0)        # π/2
Math.atan(1)        # π/4

# atan2 for angle calculation
angle: Math.atan2(y, x)

# Hyperbolic functions
Math.sinh(1)        # Hyperbolic sine
Math.cosh(1)        # Hyperbolic cosine
Math.tanh(1)        # Hyperbolic tangent

# Degree/Radian conversion
to_radians: (degrees) => degrees * (Math.PI / 180)
to_degrees: (radians) => radians * (180 / Math.PI)

# Using conversions
sin_45_deg: Math.sin(to_radians(45))  # 0.7071067811865476
```

## Min/Max Operations

```tusk
# Minimum value
Math.min(5, 3, 9, 1)    # 1
Math.min(...array)      # Spread array elements

# Maximum value
Math.max(5, 3, 9, 1)    # 9
Math.max(...array)      # Spread array elements

# Clamping values
clamp: (value, min, max) => {
    return Math.max(min, Math.min(max, value))
}

clamp(15, 0, 10)    # 10 (clamped to max)
clamp(-5, 0, 10)    # 0  (clamped to min)
clamp(5, 0, 10)     # 5  (within range)

# Finding min/max in arrays
numbers: [3, 7, 2, 9, 1]
min_value: Math.min(...numbers)  # 1
max_value: Math.max(...numbers)  # 9
```

## Random Numbers

```tusk
# Basic random (0 to 1)
Math.random()       # e.g., 0.7264975303378

# Random integer between min and max (inclusive)
random_int: (min, max) => {
    return Math.floor(Math.random() * (max - min + 1)) + min
}

dice_roll: random_int(1, 6)

# Random float between min and max
random_float: (min, max) => {
    return Math.random() * (max - min) + min
}

temperature: random_float(20.0, 25.0)

# Random boolean
random_bool: () => Math.random() < 0.5

# Random array element
random_element: (array) => {
    return array[Math.floor(Math.random() * array.length)]
}

color: random_element(["red", "green", "blue"])

# Seeded random (using external library)
rng: new SeededRandom(12345)
predictable_random: rng.next()
```

## Number Formatting

```tusk
# Fixed decimal places
(3.14159).toFixed(2)        # "3.14"
(10).toFixed(2)             # "10.00"

# Exponential notation
(12345).toExponential(2)    # "1.23e+4"

# Precision
(3.14159).toPrecision(4)    # "3.142"

# Locale formatting
(1234567.89).toLocaleString()              # "1,234,567.89"
(1234567.89).toLocaleString('de-DE')       # "1.234.567,89"

# Currency formatting
format_currency: (amount, currency: "USD") => {
    return amount.toLocaleString('en-US', {
        style: 'currency',
        currency: currency
    })
}

format_currency(1234.56)    # "$1,234.56"

# Percentage formatting
format_percent: (value, decimals: 0) => {
    return (value * 100).toFixed(decimals) + "%"
}

format_percent(0.1534, 2)   # "15.34%"
```

## Advanced Math Operations

```tusk
# Factorial
factorial: (n) => {
    if (n <= 1) return 1
    return n * factorial(n - 1)
}

# Fibonacci
fibonacci: (n) => {
    if (n <= 1) return n
    return fibonacci(n - 1) + fibonacci(n - 2)
}

# Greatest Common Divisor
gcd: (a, b) => {
    if (b == 0) return a
    return gcd(b, a % b)
}

# Least Common Multiple
lcm: (a, b) => {
    return Math.abs(a * b) / gcd(a, b)
}

# Prime check
is_prime: (n) => {
    if (n <= 1) return false
    if (n <= 3) return true
    if (n % 2 == 0 || n % 3 == 0) return false
    
    i: 5
    while (i * i <= n) {
        if (n % i == 0 || n % (i + 2) == 0) return false
        i += 6
    }
    return true
}
```

## Statistical Functions

```tusk
# Mean (average)
mean: (numbers) => {
    return numbers.reduce((sum, n) => sum + n, 0) / numbers.length
}

# Median
median: (numbers) => {
    sorted: numbers.sort((a, b) => a - b)
    mid: Math.floor(sorted.length / 2)
    
    if (sorted.length % 2 == 0) {
        return (sorted[mid - 1] + sorted[mid]) / 2
    }
    return sorted[mid]
}

# Standard deviation
std_dev: (numbers) => {
    avg: mean(numbers)
    squared_diffs: numbers.map(n => (n - avg) ** 2)
    variance: mean(squared_diffs)
    return Math.sqrt(variance)
}

# Percentile
percentile: (numbers, p) => {
    sorted: numbers.sort((a, b) => a - b)
    index: (p / 100) * (sorted.length - 1)
    lower: Math.floor(index)
    upper: Math.ceil(index)
    weight: index % 1
    
    return sorted[lower] * (1 - weight) + sorted[upper] * weight
}
```

## Bitwise Operations

```tusk
# AND
5 & 3       # 1 (0101 & 0011 = 0001)

# OR
5 | 3       # 7 (0101 | 0011 = 0111)

# XOR
5 ^ 3       # 6 (0101 ^ 0011 = 0110)

# NOT
~5          # -6 (bitwise NOT)

# Left shift
5 << 1      # 10 (0101 << 1 = 1010)

# Right shift
5 >> 1      # 2 (0101 >> 1 = 0010)

# Zero-fill right shift
-5 >>> 1    # 2147483645

# Check if power of 2
is_power_of_two: (n) => n > 0 && (n & (n - 1)) == 0

# Count set bits
count_bits: (n) => {
    count: 0
    while (n) {
        count += n & 1
        n >>= 1
    }
    return count
}
```

## Numeric Validation

```tusk
# Check if number
Number.isFinite(123)        # true
Number.isFinite(Infinity)   # false

# Check if integer
Number.isInteger(5)         # true
Number.isInteger(5.5)       # false

# Check if safe integer
Number.isSafeInteger(9007199254740991)  # true
Number.isSafeInteger(9007199254740992)  # false

# Check for NaN
Number.isNaN(NaN)           # true
Number.isNaN("NaN")         # false (string)

# Parse numbers safely
safe_parse_int: (value, default: 0) => {
    parsed: parseInt(value)
    return Number.isNaN(parsed) ? default : parsed
}

safe_parse_float: (value, default: 0.0) => {
    parsed: parseFloat(value)
    return Number.isNaN(parsed) ? default : parsed
}
```

## Best Practices

1. **Check for division by zero** - Prevent Infinity results
2. **Handle floating-point precision** - Use rounding for display
3. **Validate numeric inputs** - Check for NaN and Infinity
4. **Use appropriate data types** - Integer vs. float
5. **Consider performance** - Cache complex calculations
6. **Use Math constants** - Don't hardcode PI, E, etc.
7. **Handle edge cases** - Empty arrays, negative numbers
8. **Document units** - Specify radians/degrees, currency, etc.

## Related Topics

- `number-formatting` - Formatting numeric output
- `type-conversion` - Converting between types
- `validation` - Numeric validation
- `random-generation` - Random number generation
- `bigint-operations` - Arbitrary precision math