# Array Operations in TuskLang

Arrays are fundamental data structures in TuskLang, providing ordered collections with powerful manipulation methods and intuitive syntax.

## Array Creation

```tusk
# Array literal
numbers: [1, 2, 3, 4, 5]
empty: []

# Mixed types
mixed: [1, "hello", true, null, {name: "John"}]

# Array constructor
arr1: new Array(5)       # Creates array with 5 empty slots
arr2: new Array(1, 2, 3) # Creates [1, 2, 3]

# Array.from()
from_string: Array.from("Hello")  # ["H", "e", "l", "l", "o"]
from_set: Array.from(new Set([1, 2, 3]))

# Array.of()
array_of: Array.of(7)    # [7] (not empty array of length 7)

# Range creation
range: (start, end) => {
    return Array.from({length: end - start + 1}, (_, i) => start + i)
}
numbers: range(1, 10)    # [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]
```

## Accessing Elements

```tusk
fruits: ["apple", "banana", "orange", "grape"]

# Index access
first: fruits[0]         # "apple"
last: fruits[fruits.length - 1]  # "grape"

# Negative indexing (with helper)
at: (arr, index) => {
    return index < 0 ? arr[arr.length + index] : arr[index]
}
last_item: at(fruits, -1)  # "grape"

# Destructuring
[first, second, ...rest]: fruits
# first = "apple", second = "banana", rest = ["orange", "grape"]

# Slice for sub-arrays
subset: fruits.slice(1, 3)  # ["banana", "orange"]
last_two: fruits.slice(-2)  # ["orange", "grape"]
```

## Adding Elements

```tusk
arr: [1, 2, 3]

# push - add to end
arr.push(4)              # Returns 4 (new length)
arr.push(5, 6, 7)        # Can add multiple

# unshift - add to beginning
arr.unshift(0)           # Returns new length
arr.unshift(-2, -1)      # Can add multiple

# splice - add at specific position
arr.splice(2, 0, "inserted")  # Insert at index 2

# concat - create new array
combined: arr.concat([8, 9, 10])
multiple: [].concat(arr1, arr2, arr3)

# spread operator
merged: [...arr1, ...arr2]
with_elements: [...arr, 4, 5, 6]
```

## Removing Elements

```tusk
arr: [1, 2, 3, 4, 5]

# pop - remove from end
last: arr.pop()          # Returns removed element

# shift - remove from beginning
first: arr.shift()       # Returns removed element

# splice - remove at position
removed: arr.splice(1, 2)  # Remove 2 elements starting at index 1

# filter - create new array without elements
filtered: arr.filter(x => x !== 3)

# Remove by value
remove_value: (arr, value) => {
    index: arr.indexOf(value)
    if (index !== -1) {
        arr.splice(index, 1)
    }
    return arr
}

# Remove all occurrences
remove_all: (arr, value) => {
    return arr.filter(x => x !== value)
}
```

## Array Transformation

```tusk
numbers: [1, 2, 3, 4, 5]

# map - transform each element
doubled: numbers.map(x => x * 2)  # [2, 4, 6, 8, 10]
squared: numbers.map(x => x ** 2)  # [1, 4, 9, 16, 25]

# map with index
indexed: numbers.map((val, idx) => ({index: idx, value: val}))

# filter - select elements
evens: numbers.filter(x => x % 2 === 0)  # [2, 4]
adults: people.filter(p => p.age >= 18)

# reduce - aggregate values
sum: numbers.reduce((acc, val) => acc + val, 0)
product: numbers.reduce((acc, val) => acc * val, 1)

# Complex reduce
grouped: items.reduce((acc, item) => {
    key: item.category
    acc[key]: acc[key] || []
    acc[key].push(item)
    return acc
}, {})

# flatMap - map and flatten
nested: [[1, 2], [3, 4]]
flattened: nested.flatMap(x => x)  # [1, 2, 3, 4]

words: ["Hello World", "from TuskLang"]
all_words: words.flatMap(s => s.split(" "))
# ["Hello", "World", "from", "TuskLang"]
```

## Array Searching

```tusk
arr: [1, 2, 3, 4, 5]
users: [{id: 1, name: "John"}, {id: 2, name: "Jane"}]

# indexOf - find first index
index: arr.indexOf(3)    # 2
not_found: arr.indexOf(10)  # -1

# lastIndexOf - find last index
last_idx: [1, 2, 3, 2, 1].lastIndexOf(2)  # 3

# includes - check existence
has_three: arr.includes(3)  # true

# find - first element matching condition
first_even: arr.find(x => x % 2 === 0)  # 2
user: users.find(u => u.name === "John")

# findIndex - index of first match
even_index: arr.findIndex(x => x % 2 === 0)  # 1

# some - test if any element matches
has_even: arr.some(x => x % 2 === 0)  # true

# every - test if all elements match
all_positive: arr.every(x => x > 0)  # true

# Custom search
binary_search: (arr, target) => {
    left: 0
    right: arr.length - 1
    
    while (left <= right) {
        mid: Math.floor((left + right) / 2)
        if (arr[mid] === target) return mid
        if (arr[mid] < target) left = mid + 1
        else right = mid - 1
    }
    return -1
}
```

## Array Sorting

```tusk
# Default sort (lexicographic)
words: ["banana", "apple", "cherry"]
words.sort()  # ["apple", "banana", "cherry"]

# Numeric sort
numbers: [10, 5, 40, 25, 1000, 1]
numbers.sort((a, b) => a - b)  # [1, 5, 10, 25, 40, 1000]

# Reverse sort
numbers.sort((a, b) => b - a)  # [1000, 40, 25, 10, 5, 1]

# Object sorting
users: [
    {name: "John", age: 30},
    {name: "Jane", age: 25},
    {name: "Bob", age: 35}
]

# Sort by age
users.sort((a, b) => a.age - b.age)

# Sort by name
users.sort((a, b) => a.name.localeCompare(b.name))

# Multi-level sort
sort_by: (arr, ...keys) => {
    return arr.sort((a, b) => {
        for (key of keys) {
            if (a[key] < b[key]) return -1
            if (a[key] > b[key]) return 1
        }
        return 0
    })
}

# Stable sort
stable_sort: (arr, compare) => {
    return arr
        .map((item, index) => ({item, index}))
        .sort((a, b) => compare(a.item, b.item) || a.index - b.index)
        .map(({item}) => item)
}
```

## Array Iteration

```tusk
arr: [1, 2, 3, 4, 5]

# forEach - side effects only
arr.forEach(x => console.log(x))

# forEach with index
arr.forEach((val, idx) => {
    console.log(`${idx}: ${val}`)
})

# for...of loop
for (value of arr) {
    console.log(value)
}

# for...in loop (indices)
for (index in arr) {
    console.log(`${index}: ${arr[index]}`)
}

# Traditional for loop
for (i: 0; i < arr.length; i++) {
    console.log(arr[i])
}

# Reverse iteration
for (i: arr.length - 1; i >= 0; i--) {
    console.log(arr[i])
}

# entries() for index-value pairs
for ([index, value] of arr.entries()) {
    console.log(`${index}: ${value}`)
}
```

## Array Manipulation

```tusk
# reverse - mutates array
arr: [1, 2, 3]
arr.reverse()  # [3, 2, 1]

# Create reversed copy
reversed: [...arr].reverse()

# join - create string
words: ["Hello", "World"]
sentence: words.join(" ")  # "Hello World"
csv: words.join(",")       # "Hello,World"

# flat - flatten nested arrays
nested: [1, [2, 3], [4, [5, 6]]]
flat1: nested.flat()       # [1, 2, 3, 4, [5, 6]]
flat2: nested.flat(2)      # [1, 2, 3, 4, 5, 6]
flat_all: nested.flat(Infinity)  # Flatten all levels

# fill - fill with value
new_arr: new Array(5).fill(0)  # [0, 0, 0, 0, 0]
arr.fill("x", 1, 4)  # Fill indices 1-3 with "x"

# copyWithin
arr: [1, 2, 3, 4, 5]
arr.copyWithin(0, 3, 5)  # [4, 5, 3, 4, 5]
```

## Array Utilities

```tusk
# Check if array
is_array: Array.isArray([1, 2, 3])  # true
is_array: Array.isArray("hello")    # false

# Array equality
arrays_equal: (a, b) => {
    if (a.length !== b.length) return false
    return a.every((val, index) => val === b[index])
}

# Deep equality
deep_equal: (a, b) => {
    if (a === b) return true
    if (!a || !b) return false
    if (a.length !== b.length) return false
    
    return a.every((val, index) => {
        if (Array.isArray(val) && Array.isArray(b[index])) {
            return deep_equal(val, b[index])
        }
        return val === b[index]
    })
}

# Unique values
unique: (arr) => [...new Set(arr)]

# Intersection
intersect: (a, b) => a.filter(x => b.includes(x))

# Difference
diff: (a, b) => a.filter(x => !b.includes(x))

# Shuffle
shuffle: (arr) => {
    copy: [...arr]
    for (i: copy.length - 1; i > 0; i--) {
        j: Math.floor(Math.random() * (i + 1))
        [copy[i], copy[j]]: [copy[j], copy[i]]
    }
    return copy
}
```

## Array Performance

```tusk
# Pre-allocate for known size
# Good
arr: new Array(1000)
for (i: 0; i < 1000; i++) {
    arr[i]: i * 2
}

# Avoid repeated push for large arrays
# Better: collect then concat
chunks: []
while (has_more_data()) {
    chunk: process_chunk()
    chunks.push(chunk)
}
result: [].concat(...chunks)

# Use typed arrays for numeric data
float_array: new Float32Array(1000)
int_array: new Int32Array(1000)

# Avoid sparse arrays
# Bad
sparse: []
sparse[1000]: "value"  # Creates sparse array

# Good
dense: new Array(1001).fill(null)
dense[1000]: "value"
```

## Array Patterns

```tusk
# Chunk array
chunk: (arr, size) => {
    chunks: []
    for (i: 0; i < arr.length; i += size) {
        chunks.push(arr.slice(i, i + size))
    }
    return chunks
}

# Zip arrays
zip: (...arrays) => {
    max_length: Math.max(...arrays.map(a => a.length))
    return Array.from({length: max_length}, (_, i) => {
        return arrays.map(a => a[i])
    })
}

# Rotate array
rotate: (arr, n) => {
    n: n % arr.length
    return arr.slice(n).concat(arr.slice(0, n))
}

# Partition array
partition: (arr, predicate) => {
    return arr.reduce((acc, val) => {
        acc[predicate(val) ? 0 : 1].push(val)
        return acc
    }, [[], []])
}
```

## Best Practices

1. **Use appropriate methods** - map() for transformation, filter() for selection
2. **Avoid mutating when possible** - Create new arrays for immutability
3. **Check array bounds** - Prevent undefined access
4. **Use typed arrays for numbers** - Better performance and memory usage
5. **Be careful with sort()** - It mutates and uses lexicographic order
6. **Prefer declarative methods** - map/filter/reduce over loops
7. **Handle empty arrays** - Many methods return undefined on empty
8. **Consider performance** - Some operations are O(n²)

## Related Topics

- `iterators` - Advanced iteration
- `destructuring` - Array destructuring
- `spread-operator` - Array spreading
- `typed-arrays` - Numeric arrays
- `collection-methods` - Advanced collection operations