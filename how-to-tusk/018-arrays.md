# Arrays in TuskLang

Arrays are ordered collections of values in TuskLang. They can contain any type of data, including other arrays and objects. This guide covers everything you need to know about working with arrays.

## Creating Arrays

### Array Literals

```tusk
# Empty array
empty: []

# Array of strings
fruits: ["apple", "banana", "orange"]

# Array of numbers
numbers: [1, 2, 3, 4, 5]

# Mixed types
mixed: ["string", 42, true, null, { key: "value" }]

# Nested arrays
matrix: [
    [1, 2, 3],
    [4, 5, 6],
    [7, 8, 9]
]
```

### Multiline Arrays

```tusk
# Cleaner for long arrays
users: [
    "Alice",
    "Bob",
    "Charlie",
    "David",
    "Eve"
]

# Array of objects
products: [
    {
        id: 1
        name: "Laptop"
        price: 999.99
    },
    {
        id: 2
        name: "Mouse"
        price: 29.99
    }
]
```

## Accessing Array Elements

### Index Access

```tusk
colors: ["red", "green", "blue", "yellow"]

# Access by index (0-based)
first = colors[0]        # "red"
second = colors[1]       # "green"
last = colors[3]         # "yellow"

# Negative indices (from end)
last_item = colors[-1]   # "yellow"
second_last = colors[-2] # "blue"

# Out of bounds returns null
invalid = colors[10]     # null
```

### Safe Access

```tusk
# Optional chaining for safe access
data: null
safe_access = data?.[0]  # null (no error)

# With nested arrays
matrix: [[1, 2], [3, 4]]
value = matrix?.[0]?.[1]  # 2

# With default values
item = array?.[index] ?? "default"
```

## Array Properties

### Length

```tusk
fruits: ["apple", "banana", "orange"]

# Get array length
count = @len(fruits)     # 3
count2 = fruits.length   # 3

# Check if empty
is_empty = @len(fruits) == 0
has_items = @len(fruits) > 0
```

### Type Checking

```tusk
# Check if value is array
data1: [1, 2, 3]
data2: "not an array"

is_array1 = @isArray(data1)  # true
is_array2 = @isArray(data2)  # false

# Check array contents
all_numbers = @all(data1, @isNumber)
has_string = @any(data1, @isString)
```

## Array Methods

### Adding Elements

```tusk
numbers: [1, 2, 3]

# Push (add to end)
@push(numbers, 4)         # [1, 2, 3, 4]

# Unshift (add to beginning)
@unshift(numbers, 0)      # [0, 1, 2, 3, 4]

# Insert at index
@insert(numbers, 2, 1.5)  # [0, 1, 1.5, 2, 3, 4]

# Concatenate arrays
more_numbers: [5, 6, 7]
combined = @concat(numbers, more_numbers)
```

### Removing Elements

```tusk
letters: ["a", "b", "c", "d", "e"]

# Pop (remove from end)
last = @pop(letters)      # "e", array is now ["a", "b", "c", "d"]

# Shift (remove from beginning)
first = @shift(letters)   # "a", array is now ["b", "c", "d"]

# Remove at index
@removeAt(letters, 1)     # Removes "c", array is now ["b", "d"]

# Remove by value
@remove(letters, "b")     # Array is now ["d"]
```

### Slicing and Splicing

```tusk
numbers: [0, 1, 2, 3, 4, 5, 6, 7, 8, 9]

# Slice (extract portion)
subset = @slice(numbers, 2, 5)    # [2, 3, 4]
from_index = @slice(numbers, 5)   # [5, 6, 7, 8, 9]
last_three = @slice(numbers, -3)  # [7, 8, 9]

# Splice (modify array)
removed = @splice(numbers, 2, 3)  # Removes 3 items at index 2
# numbers is now [0, 1, 5, 6, 7, 8, 9]

# Splice with replacement
@splice(numbers, 1, 2, ["a", "b", "c"])
# Replaces 2 items at index 1 with new items
```

## Array Transformation

### Map

```tusk
numbers: [1, 2, 3, 4, 5]

# Transform each element
doubled = @map(numbers, @lambda(n, n * 2))
# Result: [2, 4, 6, 8, 10]

# With index
indexed = @map(numbers, @lambda(n, i, "${i}: ${n}"))
# Result: ["0: 1", "1: 2", "2: 3", "3: 4", "4: 5"]

# Object transformation
users: [
    { name: "Alice", age: 30 },
    { name: "Bob", age: 25 }
]
names = @map(users, @lambda(user, user.name))
# Result: ["Alice", "Bob"]
```

### Filter

```tusk
numbers: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]

# Filter by condition
evens = @filter(numbers, @lambda(n, n % 2 == 0))
# Result: [2, 4, 6, 8, 10]

# Complex filtering
products: [
    { name: "Laptop", price: 999, inStock: true },
    { name: "Mouse", price: 29, inStock: false },
    { name: "Keyboard", price: 79, inStock: true }
]

available = @filter(products, @lambda(p, p.inStock && p.price < 100))
# Result: [{ name: "Keyboard", price: 79, inStock: true }]
```

### Reduce

```tusk
numbers: [1, 2, 3, 4, 5]

# Sum all numbers
sum = @reduce(numbers, @lambda(acc, n, acc + n), 0)
# Result: 15

# Find maximum
max = @reduce(numbers, @lambda(acc, n, @max(acc, n)), -Infinity)
# Result: 5

# Build object from array
pairs: [["a", 1], ["b", 2], ["c", 3]]
object = @reduce(pairs, @lambda(acc, pair, {
    acc[pair[0]] = pair[1]
    return: acc
}), {})
# Result: { a: 1, b: 2, c: 3 }
```

## Array Search

### Finding Elements

```tusk
fruits: ["apple", "banana", "orange", "banana"]

# Find index
index = @indexOf(fruits, "banana")      # 1
last_index = @lastIndexOf(fruits, "banana")  # 3
not_found = @indexOf(fruits, "grape")   # -1

# Check if includes
has_apple = @includes(fruits, "apple")  # true
has_grape = @includes(fruits, "grape")  # false

# Find with predicate
numbers: [1, 5, 10, 15, 20]
first_large = @find(numbers, @lambda(n, n > 10))      # 15
index_large = @findIndex(numbers, @lambda(n, n > 10)) # 3
```

### Array Testing

```tusk
numbers: [2, 4, 6, 8, 10]

# Test if all match condition
all_even = @all(numbers, @lambda(n, n % 2 == 0))  # true

# Test if any match condition
has_large = @any(numbers, @lambda(n, n > 7))      # true

# Count matching elements
even_count = @count(numbers, @lambda(n, n % 2 == 0))  # 5
```

## Array Sorting

### Basic Sorting

```tusk
# Sort numbers
numbers: [3, 1, 4, 1, 5, 9]
sorted = @sort(numbers)           # [1, 1, 3, 4, 5, 9]
descending = @sort(numbers, "desc")  # [9, 5, 4, 3, 1, 1]

# Sort strings
words: ["banana", "apple", "cherry"]
alphabetical = @sort(words)       # ["apple", "banana", "cherry"]

# Natural sort (handles numbers in strings)
files: ["file10.txt", "file2.txt", "file1.txt"]
natural = @sort.natural(files)    # ["file1.txt", "file2.txt", "file10.txt"]
```

### Custom Sorting

```tusk
# Sort objects
users: [
    { name: "Alice", age: 30 },
    { name: "Bob", age: 25 },
    { name: "Charlie", age: 35 }
]

# Sort by age
by_age = @sort(users, @lambda(a, b, a.age - b.age))

# Sort by multiple criteria
sorted_users = @sort(users, @lambda(a, b, {
    # First by age, then by name
    @if(a.age != b.age,
        a.age - b.age,
        @compare(a.name, b.name)
    )
}))
```

## Array Manipulation

### Reverse

```tusk
original: [1, 2, 3, 4, 5]
reversed = @reverse(original)  # [5, 4, 3, 2, 1]

# Original unchanged
# Use @reverse! for in-place reversal
```

### Unique Values

```tusk
numbers: [1, 2, 2, 3, 3, 3, 4]
unique = @unique(numbers)      # [1, 2, 3, 4]

# Unique by property
users: [
    { id: 1, name: "Alice" },
    { id: 2, name: "Bob" },
    { id: 1, name: "Alice Duplicate" }
]
unique_users = @uniqueBy(users, @lambda(u, u.id))
```

### Flatten

```tusk
nested: [[1, 2], [3, 4], [5, 6]]
flat = @flatten(nested)        # [1, 2, 3, 4, 5, 6]

# Deep flatten
deep_nested: [1, [2, [3, [4, 5]]]]
deep_flat = @flatten(deep_nested, Infinity)  # [1, 2, 3, 4, 5]

# Flatten one level
one_level = @flatten(deep_nested, 1)  # [1, 2, [3, [4, 5]]]
```

## Array Grouping

### Group By

```tusk
people: [
    { name: "Alice", department: "Engineering" },
    { name: "Bob", department: "Sales" },
    { name: "Charlie", department: "Engineering" },
    { name: "David", department: "Sales" }
]

# Group by department
by_dept = @groupBy(people, @lambda(p, p.department))
# Result: {
#   "Engineering": [Alice, Charlie],
#   "Sales": [Bob, David]
# }

# Group with custom key
by_name_length = @groupBy(people, @lambda(p, @len(p.name)))
```

### Partition

```tusk
numbers: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]

# Split into two groups
[evens, odds] = @partition(numbers, @lambda(n, n % 2 == 0))
# evens: [2, 4, 6, 8, 10]
# odds: [1, 3, 5, 7, 9]

# Chunk into groups of size
chunks = @chunk(numbers, 3)
# Result: [[1, 2, 3], [4, 5, 6], [7, 8, 9], [10]]
```

## Array Operations

### Set Operations

```tusk
set1: [1, 2, 3, 4, 5]
set2: [4, 5, 6, 7, 8]

# Union (all unique elements)
union = @union(set1, set2)           # [1, 2, 3, 4, 5, 6, 7, 8]

# Intersection (common elements)
intersection = @intersection(set1, set2)  # [4, 5]

# Difference (in first but not second)
difference = @difference(set1, set2)      # [1, 2, 3]

# Symmetric difference (in either but not both)
sym_diff = @symmetricDifference(set1, set2)  # [1, 2, 3, 6, 7, 8]
```

### Zip and Unzip

```tusk
names: ["Alice", "Bob", "Charlie"]
ages: [30, 25, 35]
cities: ["NYC", "LA", "Chicago"]

# Zip arrays together
zipped = @zip(names, ages, cities)
# Result: [
#   ["Alice", 30, "NYC"],
#   ["Bob", 25, "LA"],
#   ["Charlie", 35, "Chicago"]
# ]

# Unzip
[names2, ages2, cities2] = @unzip(zipped)
```

## Array Patterns

### Array Destructuring

```tusk
# Basic destructuring
point: [10, 20]
[x, y] = point  # x: 10, y: 20

# With rest
numbers: [1, 2, 3, 4, 5]
[first, second, ...rest] = numbers
# first: 1, second: 2, rest: [3, 4, 5]

# Skip elements
[a, , c] = ["A", "B", "C"]  # a: "A", c: "C"
```

### Array Builders

```tusk
# Range function
range = @range(1, 10)        # [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]
evens = @range(0, 10, 2)     # [0, 2, 4, 6, 8, 10]

# Array from repeated value
repeated = @repeat("x", 5)    # ["x", "x", "x", "x", "x"]

# Array from function
generated = @generate(5, @lambda(i, i * i))  # [0, 1, 4, 9, 16]
```

## Performance Considerations

### Efficient Operations

```tusk
# Avoid multiple passes
# Inefficient
filtered = @filter(array, condition)
mapped = @map(filtered, transform)

# Efficient - single pass
result = @reduce(array, @lambda(acc, item, {
    @if(condition(item), {
        @push(acc, transform(item))
    })
    return: acc
}), [])
```

### Array vs Object

```tusk
# Use arrays for ordered collections
items: ["first", "second", "third"]

# Use objects for keyed access
lookup: {
    "key1": "value1"
    "key2": "value2"
}

# Don't use arrays as maps
# Bad
user_array: [["id", 123], ["name", "John"]]

# Good
user_object: { id: 123, name: "John" }
```

## Common Mistakes

### Modifying During Iteration

```tusk
# Wrong - modifying array while iterating
@each(array, @lambda(item, i, {
    @if(condition, {
        @removeAt(array, i)  # Dangerous!
    })
}))

# Right - filter instead
filtered = @filter(array, @lambda(item, !condition))
```

### Reference vs Copy

```tusk
# Arrays are passed by reference
original: [1, 2, 3]
reference = original
@push(reference, 4)  # Modifies original!

# Create a copy
copy = [...original]  # Spread operator
copy2 = @slice(original)  # Full slice
copy3 = @concat([], original)  # Concatenate with empty
```

## Best Practices

1. **Use descriptive array names** - plural nouns for collections
2. **Check bounds** before accessing indices
3. **Prefer immutable operations** - don't modify original arrays
4. **Use appropriate methods** - filter instead of manual loops
5. **Handle empty arrays** gracefully
6. **Document array structure** for complex data
7. **Consider performance** for large arrays

## Next Steps

- Learn about [Nested Objects](019-nested-objects.md) in arrays
- Explore [Array Methods](031-at-operator-intro.md) with @ operators
- Master [Best Practices](030-best-practices.md) for data structures