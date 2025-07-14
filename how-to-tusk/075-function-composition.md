# Function Composition in TuskLang

Function composition is a powerful paradigm in TuskLang that allows you to build complex operations by combining simple functions, promoting code reuse and clarity.

## Basic Composition

```tusk
# Simple function composition
add_one: (x) => x + 1
double: (x) => x * 2
square: (x) => x ** 2

# Manual composition
result: square(double(add_one(5)))  # ((5 + 1) * 2)² = 144

# Compose function
compose: (...fns) => {
    return (x) => fns.reduceRight((acc, fn) => fn(acc), x)
}

# Using compose (right to left)
calculate: compose(square, double, add_one)
result: calculate(5)  # 144

# Pipe function (left to right)
pipe: (...fns) => {
    return (x) => fns.reduce((acc, fn) => fn(acc), x)
}

# Using pipe
process: pipe(add_one, double, square)
result: process(5)  # 144
```

## Practical Examples

```tusk
# String processing pipeline
trim: (s) => s.trim()
lowercase: (s) => s.toLowerCase()
replace_spaces: (s) => s.replace(/\s+/g, '-')
remove_special: (s) => s.replace(/[^a-z0-9-]/g, '')

slugify: pipe(
    trim,
    lowercase,
    replace_spaces,
    remove_special
)

slug: slugify("  Hello World!  ")  # "hello-world"

# Data transformation
parse_int: (x) => parseInt(x)
validate_positive: (x) => x > 0 ? x : 0
add_tax: (x) => x * 1.08
format_currency: (x) => "$" + x.toFixed(2)

process_price: pipe(
    parse_int,
    validate_positive,
    add_tax,
    format_currency
)

price: process_price("100")  # "$108.00"
```

## Partial Application

```tusk
# Partial application for composition
multiply: (a) => (b) => a * b
add: (a) => (b) => a + b
divide: (a) => (b) => b / a

# Create specialized functions
double: multiply(2)
triple: multiply(3)
add_ten: add(10)
half: divide(2)

# Compose with partial functions
calculate: pipe(
    add_ten,      # x + 10
    double,       # (x + 10) * 2
    half          # ((x + 10) * 2) / 2
)

result: calculate(5)  # 15

# More complex example
filter_by: (predicate) => (array) => array.filter(predicate)
map_by: (fn) => (array) => array.map(fn)
reduce_by: (fn, initial) => (array) => array.reduce(fn, initial)

# Create pipeline
process_numbers: pipe(
    filter_by(x => x > 0),              # Keep positive
    map_by(x => x * 2),                 # Double them
    reduce_by((sum, x) => sum + x, 0)   # Sum them
)

total: process_numbers([1, -2, 3, -4, 5])  # 18
```

## Async Composition

```tusk
# Async pipe
pipe_async: (...fns) => {
    return async (x) => {
        result: x
        for (fn of fns) {
            result: await fn(result)
        }
        return result
    }
}

# Async functions
fetch_user: async (id) => {
    response: await fetch(`/api/users/${id}`)
    return response.json()
}

enrich_user: async (user) => {
    posts: await fetch(`/api/users/${user.id}/posts`).then(r => r.json())
    return {...user, posts}
}

format_user: async (user) => {
    return {
        name: user.name,
        email: user.email,
        post_count: user.posts.length
    }
}

# Compose async operations
get_user_summary: pipe_async(
    fetch_user,
    enrich_user,
    format_user
)

summary: await get_user_summary(123)
```

## Function Decorators

```tusk
# Logging decorator
with_logging: (fn) => {
    return (...args) => {
        console.log(`Calling ${fn.name} with`, args)
        result: fn(...args)
        console.log(`Result:`, result)
        return result
    }
}

# Timing decorator
with_timing: (fn) => {
    return (...args) => {
        start: performance.now()
        result: fn(...args)
        end: performance.now()
        console.log(`${fn.name} took ${end - start}ms`)
        return result
    }
}

# Memoization decorator
memoize: (fn) => {
    cache: new Map()
    
    return (...args) => {
        key: JSON.stringify(args)
        
        if (cache.has(key)) {
            return cache.get(key)
        }
        
        result: fn(...args)
        cache.set(key, result)
        return result
    }
}

# Compose decorators
enhance: compose(
    with_logging,
    with_timing,
    memoize
)

# Enhanced function
fibonacci: enhance((n) => {
    if (n <= 1) return n
    return fibonacci(n - 1) + fibonacci(n - 2)
})
```

## Monadic Composition

```tusk
# Maybe monad for null safety
Maybe: {
    of: (value) => ({
        value,
        map: (fn) => value != null ? Maybe.of(fn(value)) : Maybe.of(null),
        chain: (fn) => value != null ? fn(value) : Maybe.of(null),
        or_else: (default) => value != null ? value : default
    })
}

# Safe property access
safe_prop: (prop) => (obj) => Maybe.of(obj?.[prop])

# Compose with Maybe
get_city: pipe(
    Maybe.of,
    (m) => m.chain(safe_prop('address')),
    (m) => m.chain(safe_prop('city')),
    (m) => m.or_else('Unknown')
)

city: get_city(user)  # Safe even if address is null

# Result monad for error handling
Result: {
    ok: (value) => ({
        is_ok: true,
        value,
        map: (fn) => Result.ok(fn(value)),
        chain: (fn) => fn(value),
        or_else: () => value
    }),
    
    err: (error) => ({
        is_ok: false,
        error,
        map: () => Result.err(error),
        chain: () => Result.err(error),
        or_else: (fn) => fn(error)
    })
}

# Safe division
safe_divide: (a) => (b) => {
    if (b === 0) return Result.err("Division by zero")
    return Result.ok(a / b)
}

# Compose with Result
calculate_percentage: pipe(
    (x) => Result.ok(x),
    (r) => r.chain(x => safe_divide(x)(100)),
    (r) => r.map(x => x * 100),
    (r) => r.map(x => x.toFixed(2) + "%"),
    (r) => r.or_else(err => "Error: " + err)
)
```

## Transducers

```tusk
# Transducer pattern
mapping: (fn) => (reducer) => {
    return (acc, val) => reducer(acc, fn(val))
}

filtering: (predicate) => (reducer) => {
    return (acc, val) => predicate(val) ? reducer(acc, val) : acc
}

# Compose transducers
compose_transducers: compose

# Create transducer
xform: compose_transducers(
    filtering(x => x > 0),
    mapping(x => x * 2),
    mapping(x => x + 1)
)

# Use with reduce
result: [1, -2, 3, -4, 5].reduce(
    xform((acc, val) => [...acc, val]),
    []
)  # [3, 7, 11]

# Reuse with different reducers
sum: [1, -2, 3, -4, 5].reduce(
    xform((acc, val) => acc + val),
    0
)  # 21
```

## Functional Utilities

```tusk
# Identity function
identity: (x) => x

# Constant function
constant: (x) => () => x

# Flip arguments
flip: (fn) => (a) => (b) => fn(b)(a)

# Curry function
curry: (fn) => {
    return (...args) => {
        if (args.length >= fn.length) {
            return fn(...args)
        }
        return (...more) => curry(fn)(...args, ...more)
    }
}

# Uncurry function
uncurry: (fn) => (...args) => {
    result: fn
    for (arg of args) {
        result: result(arg)
    }
    return result
}

# Tap for debugging
tap: (fn) => (x) => {
    fn(x)
    return x
}

# Usage in pipeline
process: pipe(
    add_one,
    tap(x => console.log("After add:", x)),
    double,
    tap(x => console.log("After double:", x)),
    square
)
```

## Advanced Patterns

```tusk
# Kleisli composition for monads
kleisli_compose: (f, g) => (x) => f(x).chain(g)

# Contravariant composition
contramap: (fn) => (transform) => {
    return (x) => fn(transform(x))
}

# Bifunctor mapping
bimap: (f, g) => (either) => {
    return either.is_left 
        ? {...either, value: f(either.value)}
        : {...either, value: g(either.value)}
}

# Lens composition
lens: (getter, setter) => ({
    get: getter,
    set: setter,
    over: (fn) => (obj) => setter(fn(getter(obj)))(obj)
})

compose_lenses: (lens1, lens2) => lens(
    (obj) => lens2.get(lens1.get(obj)),
    (val) => (obj) => lens1.set(lens2.set(val)(lens1.get(obj)))(obj)
)

# Category theory inspired
class Category {
    constructor(compose, id) {
        this.compose: compose
        this.id: id
    }
    
    // Laws
    left_identity: (f) => this.compose(this.id, f) === f
    right_identity: (f) => this.compose(f, this.id) === f
    associativity: (f, g, h) => 
        this.compose(f, this.compose(g, h)) === 
        this.compose(this.compose(f, g), h)
}
```

## Performance Optimization

```tusk
# Lazy composition
lazy_compose: (...fns) => {
    return {
        add: (fn) => lazy_compose(...fns, fn),
        run: (x) => fns.reduceRight((acc, fn) => fn(acc), x)
    }
}

# Build pipeline incrementally
pipeline: lazy_compose()
    .add(validate)
    .add(transform)
    .add(format)

# Run when ready
result: pipeline.run(data)

# Compile composed function
compile_composition: (...fns) => {
    # Generate optimized code
    code: "return function(x) { return "
    
    for (i: fns.length - 1; i >= 0; i--) {
        code += `fns[${i}](`
    }
    
    code += "x"
    code += ")".repeat(fns.length)
    code += " }"
    
    return new Function("fns", code)(fns)
}

# Parallel composition for independent operations
parallel_compose: (...fns) => {
    return async (x) => {
        results: await Promise.all(fns.map(fn => fn(x)))
        return results
    }
}
```

## Best Practices

1. **Keep functions pure** - No side effects for predictability
2. **Single responsibility** - Each function does one thing
3. **Type consistency** - Ensure compatible input/output types
4. **Name meaningfully** - Composed functions should have clear names
5. **Test components** - Test individual functions and compositions
6. **Document pipelines** - Explain what the composition does
7. **Consider performance** - Deep composition can impact performance
8. **Use type hints** - Help with composition compatibility

## Related Topics

- `higher-order-functions` - Functions that operate on functions
- `partial-application` - Creating specialized functions
- `currying` - Function transformation
- `monads` - Compositional error handling
- `functional-programming` - FP concepts in TuskLang