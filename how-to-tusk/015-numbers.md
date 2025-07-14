# Numbers in TuskLang

TuskLang provides comprehensive support for numeric values, including integers, floating-point numbers, and various mathematical operations. This guide covers everything you need to know about working with numbers.

## Number Types

### Integers

```tusk
# Basic integers
count: 42
negative: -17
zero: 0

# Large numbers
population: 7800000000
debt: -1000000

# Binary, octal, and hex literals
binary: 0b1010      # 10 in decimal
octal: 0o755        # 493 in decimal
hexadecimal: 0xFF   # 255 in decimal
```

### Floating-Point Numbers

```tusk
# Basic decimals
price: 19.99
temperature: -40.5
pi: 3.14159265359

# Scientific notation
avogadro: 6.022e23
planck: 6.626e-34
million: 1e6        # 1,000,000
tiny: 1e-9          # 0.000000001
```

### Special Values

```tusk
# Infinity
positive_infinity: Infinity
negative_infinity: -Infinity

# Not a Number
not_a_number: NaN

# Checking special values
is_finite = @isFinite(value)
is_nan = @isNaN(value)
```

## Basic Arithmetic

### Standard Operations

```tusk
a: 10
b: 3

# Basic arithmetic
sum = a + b          # 13
difference = a - b   # 7
product = a * b      # 30
quotient = a / b     # 3.333...
remainder = a % b    # 1

# Power operations
squared = a ** 2     # 100
cubed = a ** 3      # 1000
root = 16 ** 0.5    # 4 (square root)
```

### Order of Operations

```tusk
# Standard mathematical order (PEMDAS)
result1 = 2 + 3 * 4      # 14 (not 20)
result2 = (2 + 3) * 4    # 20
result3 = 10 / 2 * 3     # 15 (left to right)
result4 = 2 ** 3 ** 2    # 512 (right to left for exponents)
```

## Number Functions

### Math Functions

```tusk
# Rounding functions
rounded = @round(3.7)        # 4
floored = @floor(3.7)        # 3
ceiled = @ceil(3.2)          # 4
truncated = @trunc(3.7)      # 3

# Precision rounding
price = @round(19.995, 2)    # 19.99
pi_short = @round(3.14159, 3) # 3.142

# Absolute value
distance = @abs(-42)         # 42

# Min/Max
smallest = @min(5, 3, 8, 1)  # 1
largest = @max(5, 3, 8, 1)   # 8
```

### Advanced Math

```tusk
# Trigonometry (radians)
angle: 45
radians = angle * @PI / 180
sine = @sin(radians)
cosine = @cos(radians)
tangent = @tan(radians)

# Inverse trig
asin_val = @asin(0.5)  # π/6
acos_val = @acos(0.5)  # π/3
atan_val = @atan(1)    # π/4

# Logarithms
natural_log = @ln(10)       # 2.302...
log_base_10 = @log10(100)   # 2
log_base_2 = @log2(8)       # 3

# Exponentials
e_squared = @exp(2)         # e²
sqrt_2 = @sqrt(2)          # 1.414...
```

### Statistical Functions

```tusk
numbers: [1, 2, 3, 4, 5]

# Basic statistics
sum = @sum(numbers)         # 15
average = @avg(numbers)     # 3
median = @median(numbers)   # 3
std_dev = @stddev(numbers)  # 1.58...

# Range and bounds
min_val = @min(numbers)     # 1
max_val = @max(numbers)     # 5
range = max_val - min_val   # 4
```

## Number Conversion

### Type Conversion

```tusk
# String to number
str_num: "42"
num = @number(str_num)      # 42
float = @float("3.14")      # 3.14
int = @int("42.7")          # 42 (truncates)

# With validation
safe_convert = @lambda(str, {
    @if(@isNumeric(str),
        @number(str),
        @error("Invalid number: ${str}")
    )
})

# Number to string
num: 42
str = @string(num)          # "42"
formatted = @format("%.2f", 3.14159)  # "3.14"
```

### Base Conversion

```tusk
decimal: 255

# Convert to different bases
binary = @toBinary(decimal)      # "11111111"
octal = @toOctal(decimal)        # "377"
hex = @toHex(decimal)            # "FF"

# Parse from different bases
from_binary = @parseInt("1010", 2)    # 10
from_octal = @parseInt("755", 8)      # 493
from_hex = @parseInt("FF", 16)        # 255
```

## Number Formatting

### Locale Formatting

```tusk
number: 1234567.89

# Default formatting
formatted = @format.number(number)  # "1,234,567.89"

# Locale-specific
us_format = @format.number(number, "en-US")    # "1,234,567.89"
de_format = @format.number(number, "de-DE")    # "1.234.567,89"
in_format = @format.number(number, "en-IN")    # "12,34,567.89"
```

### Currency Formatting

```tusk
price: 1234.56

# Currency formatting
usd = @format.currency(price, "USD")     # "$1,234.56"
eur = @format.currency(price, "EUR")     # "€1,234.56"
jpy = @format.currency(price, "JPY")     # "¥1,235" (no decimals)

# Custom currency
custom = @format.currency(price, {
    symbol: "₹",
    decimals: 2,
    position: "before"
})
```

### Percentage Formatting

```tusk
ratio: 0.1234

# Basic percentage
percent = @format.percent(ratio)         # "12.34%"
precise = @format.percent(ratio, 1)      # "12.3%"
integer = @format.percent(ratio, 0)      # "12%"
```

## Numeric Ranges

### Creating Ranges

```tusk
# Simple range
numbers = @range(1, 5)      # [1, 2, 3, 4, 5]

# With step
evens = @range(0, 10, 2)    # [0, 2, 4, 6, 8, 10]
odds = @range(1, 10, 2)     # [1, 3, 5, 7, 9]

# Reverse range
countdown = @range(10, 1, -1)  # [10, 9, 8, ..., 1]
```

### Range Operations

```tusk
# Check if in range
age: 25
is_adult = age >= 18 && age <= 65

# Using helper functions
in_range = @inRange(age, 18, 65)

# Clamp to range
clamped = @clamp(value, 0, 100)  # Ensures 0 <= value <= 100
```

## Random Numbers

### Basic Random

```tusk
# Random float between 0 and 1
random_float = @random()

# Random integer in range
dice_roll = @randomInt(1, 6)

# Random from array
colors: ["red", "green", "blue"]
random_color = @randomChoice(colors)
```

### Seeded Random

```tusk
# Create seeded generator
rng = @random.seed(12345)

# Generate consistent sequences
value1 = @rng.next()         # Always same sequence
value2 = @rng.nextInt(1, 100)
```

## Precision and Accuracy

### Floating Point Precision

```tusk
# Floating point issues
result = 0.1 + 0.2          # 0.30000000000000004

# Safe comparison
epsilon: 0.00001
are_equal = @abs(result - 0.3) < epsilon

# Using decimal precision
precise = @decimal("0.1") + @decimal("0.2")  # Exactly 0.3
```

### Working with Money

```tusk
# Don't use floats for money
# price: 19.99  # Can have precision issues

# Use integers (cents) or decimal type
price_cents: 1999
price_dollars = price_cents / 100

# Or use decimal arithmetic
price = @decimal("19.99")
tax = @decimal("0.08")
total = @decimal.multiply(price, @decimal.add(1, tax))
```

## Bitwise Operations

```tusk
a: 0b1010  # 10
b: 0b1100  # 12

# Bitwise operations
and_result = a & b      # 0b1000 (8)
or_result = a | b       # 0b1110 (14)
xor_result = a ^ b      # 0b0110 (6)
not_result = ~a         # -11 (two's complement)

# Bit shifting
left_shift = a << 1     # 0b10100 (20)
right_shift = a >> 1    # 0b0101 (5)
```

## Number Validation

### Type Checking

```tusk
# Check if value is numeric
is_number = @isNumber(value)
is_integer = @isInteger(value)
is_float = @isFloat(value)

# Safe parsing
parse_safe = @lambda(str, {
    @if(@isNumeric(str), {
        value: @number(str)
        error: null
    }, {
        value: null
        error: "Invalid number format"
    })
})
```

### Range Validation

```tusk
# Validate numeric ranges
validate_age = @lambda(age, {
    @assert(@isInteger(age), "Age must be an integer")
    @assert(age >= 0, "Age cannot be negative")
    @assert(age <= 150, "Age seems unrealistic")
    return: true
})

# Validate decimal places
validate_price = @lambda(price, {
    decimals = @string(price).split(".")[1]?.length || 0
    @assert(decimals <= 2, "Price cannot have more than 2 decimal places")
})
```

## Common Patterns

### Financial Calculations

```tusk
# Compound interest
principal: 1000
rate: 0.05  # 5% annual
years: 10
compound_interest = principal * (1 + rate) ** years

# Percentage calculations
original: 100
increase: 20  # 20%
new_value = original * (1 + increase / 100)

# Discount calculation
price: 50
discount_percent: 15
discount_amount = price * (discount_percent / 100)
final_price = price - discount_amount
```

### Statistical Analysis

```tusk
data: [23, 45, 67, 89, 12, 34, 56, 78, 90, 43]

# Calculate statistics
stats: {
    count: @len(data)
    sum: @sum(data)
    mean: @avg(data)
    median: @median(data)
    mode: @mode(data)
    std_dev: @stddev(data)
    variance: @variance(data)
    min: @min(data)
    max: @max(data)
    range: @max(data) - @min(data)
}

# Percentiles
percentile_25 = @percentile(data, 25)
percentile_75 = @percentile(data, 75)
iqr = percentile_75 - percentile_25
```

### Unit Conversion

```tusk
# Temperature conversion
celsius_to_fahrenheit = @lambda(c, c * 9/5 + 32)
fahrenheit_to_celsius = @lambda(f, (f - 32) * 5/9)

# Distance conversion
conversions: {
    km_to_miles: @lambda(km, km * 0.621371)
    miles_to_km: @lambda(mi, mi * 1.60934)
    meters_to_feet: @lambda(m, m * 3.28084)
    feet_to_meters: @lambda(ft, ft * 0.3048)
}

# Usage
distance_km: 10
distance_miles = @conversions.km_to_miles(distance_km)
```

## Performance Tips

### Avoid Repeated Calculations

```tusk
# Inefficient
@each(items, @lambda(item, {
    # Math.PI / 180 calculated each time
    radians = item.degrees * @PI / 180
}))

# Efficient
deg_to_rad = @PI / 180
@each(items, @lambda(item, {
    radians = item.degrees * deg_to_rad
}))
```

### Use Appropriate Types

```tusk
# For whole numbers, use integers
count: 42        # Not 42.0

# For money, use proper precision
price: 1999      # Cents as integer
# Or
price: @decimal("19.99")

# For IDs, consider strings
user_id: "12345"  # Not numeric if not doing math
```

## Best Practices

1. **Use appropriate number types** - integers for counts, decimals for money
2. **Be aware of floating-point precision** - use epsilon for comparisons
3. **Validate numeric inputs** - check ranges and types
4. **Format numbers for display** - use locale-appropriate formatting
5. **Handle special values** - check for NaN and Infinity
6. **Use constants** for mathematical values
7. **Document units** in comments (seconds, bytes, etc.)

## Next Steps

- Learn about [Booleans](016-booleans.md) and logical operations
- Explore [Arrays](018-arrays.md) for number collections
- Master [Type System](025-typed-values.md) for type safety