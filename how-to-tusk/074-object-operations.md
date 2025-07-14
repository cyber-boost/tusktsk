# Object Operations in TuskLang

Objects are key-value collections in TuskLang, providing flexible data structures with powerful manipulation methods and intuitive syntax.

## Object Creation

```tusk
# Object literal
person: {
    name: "John Doe"
    age: 30
    active: true
}

# Empty object
empty: {}

# Computed property names
key: "dynamicKey"
obj: {
    [key]: "value"
    ["prop_" + 42]: "computed"
}

# Object constructor
obj1: new Object()
obj2: Object.create(null)  # No prototype

# From entries
entries: [["name", "John"], ["age", 30]]
from_entries: Object.fromEntries(entries)

# Shorthand properties
name: "John"
age: 30
person: {name, age}  # Same as {name: name, age: age}

# Methods
user: {
    name: "John"
    greet() {
        return "Hello, " + this.name
    }
}
```

## Property Access

```tusk
person: {name: "John", age: 30, "special-key": "value"}

# Dot notation
name: person.name

# Bracket notation
age: person["age"]
special: person["special-key"]

# Dynamic property access
prop: "name"
value: person[prop]

# Optional chaining
city: person.address?.city  # undefined if address doesn't exist

# Nullish coalescing
display_name: person.nickname ?? person.name ?? "Anonymous"

# Destructuring
{name, age}: person
{name: userName, age: userAge}: person  # Rename

# Nested destructuring
user: {profile: {name: "John", settings: {theme: "dark"}}}
{profile: {name, settings: {theme}}}: user
```

## Adding and Modifying Properties

```tusk
obj: {a: 1}

# Add/modify properties
obj.b: 2
obj["c"]: 3

# Object.assign() - shallow merge
target: {a: 1}
source1: {b: 2}
source2: {c: 3, a: 10}
merged: Object.assign(target, source1, source2)
# target is modified: {a: 10, b: 2, c: 3}

# Spread operator - create new object
combined: {...obj1, ...obj2}
with_override: {...defaults, ...userConfig}

# Deep merge function
deep_merge: (target, source) => {
    for (key in source) {
        if (is_object(source[key]) && is_object(target[key])) {
            target[key]: deep_merge(target[key], source[key])
        } else {
            target[key]: source[key]
        }
    }
    return target
}

# Property descriptors
Object.defineProperty(obj, 'readonly', {
    value: 42,
    writable: false,
    enumerable: true,
    configurable: false
})
```

## Removing Properties

```tusk
obj: {a: 1, b: 2, c: 3}

# delete operator
delete obj.b
delete obj["c"]

# Create new object without properties
{b, ...rest}: obj  # rest contains all except b

# Remove multiple properties
omit: (obj, ...keys) => {
    result: {...obj}
    keys.forEach(key => delete result[key])
    return result
}

cleaned: omit(obj, "temp", "internal", "_private")

# Filter properties
pick: (obj, ...keys) => {
    result: {}
    keys.forEach(key => {
        if (key in obj) result[key]: obj[key]
    })
    return result
}

subset: pick(user, "id", "name", "email")
```

## Object Iteration

```tusk
obj: {name: "John", age: 30, city: "NYC"}

# for...in loop
for (key in obj) {
    console.log(key + ": " + obj[key])
}

# Object.keys()
keys: Object.keys(obj)  # ["name", "age", "city"]
keys.forEach(key => {
    console.log(key + ": " + obj[key])
})

# Object.values()
values: Object.values(obj)  # ["John", 30, "NYC"]

# Object.entries()
entries: Object.entries(obj)  # [["name", "John"], ["age", 30], ["city", "NYC"]]
for ([key, value] of entries) {
    console.log(key + ": " + value)
}

# hasOwnProperty check
for (key in obj) {
    if (obj.hasOwnProperty(key)) {
        # Own property, not inherited
    }
}

# Get own property names (including non-enumerable)
all_props: Object.getOwnPropertyNames(obj)
symbols: Object.getOwnPropertySymbols(obj)
```

## Object Transformation

```tusk
# Map object values
map_values: (obj, fn) => {
    result: {}
    for (key in obj) {
        result[key]: fn(obj[key], key)
    }
    return result
}

prices: {apple: 1.5, banana: 0.5}
with_tax: map_values(prices, price => price * 1.08)

# Map object keys
map_keys: (obj, fn) => {
    result: {}
    for (key in obj) {
        new_key: fn(key, obj[key])
        result[new_key]: obj[key]
    }
    return result
}

snake_case: map_keys(obj, key => key.replace(/([A-Z])/g, "_$1").toLowerCase())

# Filter object
filter_object: (obj, predicate) => {
    result: {}
    for (key in obj) {
        if (predicate(obj[key], key)) {
            result[key]: obj[key]
        }
    }
    return result
}

positive_only: filter_object(numbers, val => val > 0)

# Reduce object
reduce_object: (obj, fn, initial) => {
    result: initial
    for (key in obj) {
        result: fn(result, obj[key], key)
    }
    return result
}

sum: reduce_object(scores, (sum, score) => sum + score, 0)
```

## Object Comparison

```tusk
# Reference equality
obj1: {a: 1}
obj2: {a: 1}
obj3: obj1

obj1 === obj2  # false (different objects)
obj1 === obj3  # true (same reference)

# Shallow equality
shallow_equal: (a, b) => {
    keys_a: Object.keys(a)
    keys_b: Object.keys(b)
    
    if (keys_a.length !== keys_b.length) return false
    
    return keys_a.every(key => a[key] === b[key])
}

# Deep equality
deep_equal: (a, b) => {
    if (a === b) return true
    
    if (!a || !b) return false
    if (typeof a !== typeof b) return false
    
    if (typeof a !== 'object') return a === b
    
    keys_a: Object.keys(a)
    keys_b: Object.keys(b)
    
    if (keys_a.length !== keys_b.length) return false
    
    return keys_a.every(key => deep_equal(a[key], b[key]))
}

# Object diff
diff: (old_obj, new_obj) => {
    changes: {}
    
    # Check modified and removed
    for (key in old_obj) {
        if (!(key in new_obj)) {
            changes[key]: {type: 'removed', old: old_obj[key]}
        } else if (old_obj[key] !== new_obj[key]) {
            changes[key]: {type: 'modified', old: old_obj[key], new: new_obj[key]}
        }
    }
    
    # Check added
    for (key in new_obj) {
        if (!(key in old_obj)) {
            changes[key]: {type: 'added', new: new_obj[key]}
        }
    }
    
    return changes
}
```

## Object Cloning

```tusk
# Shallow clone
original: {a: 1, b: {c: 2}}

# Spread operator
clone1: {...original}

# Object.assign
clone2: Object.assign({}, original)

# Deep clone
deep_clone: (obj) => {
    if (obj === null || typeof obj !== 'object') return obj
    if (obj instanceof Date) return new Date(obj)
    if (obj instanceof Array) return obj.map(item => deep_clone(item))
    
    cloned: {}
    for (key in obj) {
        cloned[key]: deep_clone(obj[key])
    }
    return cloned
}

# JSON method (limited)
json_clone: JSON.parse(JSON.stringify(obj))
# Note: Loses functions, undefined, symbols

# Structured clone (if available)
structured: structuredClone(obj)
```

## Object Freezing and Sealing

```tusk
# Freeze - no modifications
obj: {a: 1, b: 2}
Object.freeze(obj)
obj.a: 3  # Silently fails (error in strict mode)
obj.c: 3  # Silently fails

# Check if frozen
is_frozen: Object.isFrozen(obj)

# Seal - no adding/deleting, but can modify
obj2: {x: 1, y: 2}
Object.seal(obj2)
obj2.x: 3  # Works
obj2.z: 3  # Fails
delete obj2.y  # Fails

# Prevent extensions only
obj3: {p: 1}
Object.preventExtensions(obj3)
obj3.p: 2  # Works
obj3.q: 2  # Fails

# Deep freeze
deep_freeze: (obj) => {
    Object.freeze(obj)
    
    Object.values(obj).forEach(value => {
        if (typeof value === 'object' && value !== null) {
            deep_freeze(value)
        }
    })
    
    return obj
}
```

## Object Patterns

```tusk
# Factory pattern
create_user: (name, email) => {
    return {
        id: generate_id(),
        name,
        email,
        created_at: new Date(),
        
        update(data) {
            Object.assign(this, data)
            this.updated_at: new Date()
        },
        
        to_json() {
            return JSON.stringify(this)
        }
    }
}

# Object pooling
object_pool: {
    pool: [],
    
    get() {
        return this.pool.pop() || this.create()
    },
    
    release(obj) {
        this.reset(obj)
        this.pool.push(obj)
    },
    
    create() {
        return {}
    },
    
    reset(obj) {
        for (key in obj) {
            delete obj[key]
        }
    }
}

# Proxy for validation
validated_object: (schema) => {
    return new Proxy({}, {
        set(target, prop, value) {
            if (schema[prop]) {
                if (!schema[prop](value)) {
                    throw new Error(`Invalid value for ${prop}`)
                }
            }
            target[prop]: value
            return true
        }
    })
}

user_schema: {
    age: val => typeof val === 'number' && val >= 0,
    email: val => /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(val)
}

user: validated_object(user_schema)
```

## Object Utilities

```tusk
# Check if object is empty
is_empty: (obj) => Object.keys(obj).length === 0

# Get nested value safely
get: (obj, path, default_value) => {
    keys: path.split('.')
    result: obj
    
    for (key of keys) {
        result: result?.[key]
        if (result === undefined) return default_value
    }
    
    return result
}

# Set nested value
set: (obj, path, value) => {
    keys: path.split('.')
    last: keys.pop()
    
    current: obj
    for (key of keys) {
        if (!(key in current) || typeof current[key] !== 'object') {
            current[key]: {}
        }
        current: current[key]
    }
    
    current[last]: value
    return obj
}

# Invert object
invert: (obj) => {
    result: {}
    for (key in obj) {
        result[obj[key]]: key
    }
    return result
}

# Group by property
group_by: (array, key) => {
    return array.reduce((acc, item) => {
        group_key: item[key]
        acc[group_key]: acc[group_key] || []
        acc[group_key].push(item)
        return acc
    }, {})
}
```

## Performance Considerations

```tusk
# Object vs Map for dynamic keys
# Use Map for frequent additions/deletions
map: new Map()
map.set(key, value)
map.delete(key)

# Use object for static structure
config: {
    api_url: "...",
    timeout: 5000
}

# Avoid delete in hot paths
# Instead of delete, set to undefined
obj.prop: undefined

# Or create new object without property
{unwanted, ...kept}: obj

# Pre-define object shape
# V8 optimizes objects with stable shape
user: {
    id: null,
    name: null,
    email: null
}
# Then fill values
```

## Best Practices

1. **Use const for objects** - Prevents reassignment, not mutation
2. **Prefer immutability** - Create new objects instead of mutating
3. **Use descriptive keys** - Make objects self-documenting
4. **Avoid delete in loops** - Performance impact
5. **Consider Map for dynamic keys** - Better performance
6. **Validate object shapes** - Use schemas or TypeScript
7. **Handle missing properties** - Use optional chaining
8. **Be careful with prototypes** - Can cause unexpected behavior

## Related Topics

- `destructuring` - Object destructuring
- `spread-operator` - Object spreading
- `json-operations` - JSON serialization
- `class-syntax` - Object-oriented patterns
- `prototypes` - Prototype chain