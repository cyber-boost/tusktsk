# String Operations in TuskLang

TuskLang provides powerful string manipulation capabilities with intuitive syntax and comprehensive built-in functions for all your text processing needs.

## String Creation and Literals

```tusk
# Single quotes
single: 'Hello, World!'

# Double quotes
double: "Hello, World!"

# Template literals (backticks)
name: "TuskLang"
greeting: `Welcome to ${name}!`

# Multiline strings
multiline: """
This is a
multiline string
in TuskLang
"""

# Escaped characters
escaped: "Line 1\nLine 2\tTabbed"
quotes: "She said \"Hello\""
path: 'C:\\Users\\Documents'

# Unicode
emoji: "Hello 👋 World 🌍"
unicode: "\u0048\u0065\u006C\u006C\u006F"  # "Hello"
```

## String Concatenation

```tusk
# Plus operator
full_name: first_name + " " + last_name

# Template literals
message: `${greeting}, ${name}!`

# Join array
words: ["Hello", "World"]
sentence: words.join(" ")  # "Hello World"

# Concat method
str1: "Hello"
str2: str1.concat(" ", "World", "!")  # "Hello World!"

# String builder pattern
StringBuilder: {
    parts: []
    
    append: (str) => {
        this.parts.push(str)
        return this
    }
    
    toString: () => this.parts.join("")
}

builder: new StringBuilder()
result: builder.append("Hello")
               .append(" ")
               .append("World")
               .toString()
```

## String Length and Access

```tusk
# Length property
text: "Hello"
length: text.length  # 5

# Character access
first: text[0]       # "H"
last: text[text.length - 1]  # "o"

# charAt method
char: text.charAt(1)  # "e"

# charCodeAt for Unicode
code: text.charCodeAt(0)  # 72 (Unicode for 'H')

# Iterate over characters
for (char of text) {
    console.log(char)
}

# Convert to array
chars: Array.from(text)  # ["H", "e", "l", "l", "o"]
chars: [...text]         # Same result
```

## String Search Operations

```tusk
# indexOf - first occurrence
text: "Hello, World!"
index: text.indexOf("World")     # 7
not_found: text.indexOf("xyz")    # -1

# lastIndexOf - last occurrence
repeated: "Hello Hello"
last: repeated.lastIndexOf("Hello")  # 6

# includes - check presence
has_world: text.includes("World")    # true

# startsWith
is_greeting: text.startsWith("Hello") # true

# endsWith
is_exclaim: text.endsWith("!")        # true

# search with regex
pattern: /world/i
match_index: text.search(pattern)     # 7

# Custom search
find_all: (str, substr) => {
    indices: []
    index: str.indexOf(substr)
    
    while (index !== -1) {
        indices.push(index)
        index: str.indexOf(substr, index + 1)
    }
    
    return indices
}
```

## String Extraction

```tusk
text: "Hello, World!"

# substring(start, end)
sub1: text.substring(0, 5)    # "Hello"
sub2: text.substring(7)       # "World!"

# substr(start, length) - deprecated but common
sub3: text.substr(7, 5)       # "World"

# slice(start, end) - supports negative indices
slice1: text.slice(0, 5)      # "Hello"
slice2: text.slice(-6)        # "World!"
slice3: text.slice(-6, -1)    # "World"

# Extract between delimiters
extract_between: (str, start, end) => {
    start_index: str.indexOf(start)
    if (start_index === -1) return ""
    
    start_index += start.length
    end_index: str.indexOf(end, start_index)
    if (end_index === -1) return ""
    
    return str.substring(start_index, end_index)
}

# Example: extract_between("Hello [World]!", "[", "]") => "World"
```

## String Transformation

```tusk
text: "Hello, World!"

# Case conversion
upper: text.toUpperCase()      # "HELLO, WORLD!"
lower: text.toLowerCase()      # "hello, world!"

# Trim whitespace
padded: "  Hello  "
trimmed: padded.trim()         # "Hello"
trim_start: padded.trimStart() # "Hello  "
trim_end: padded.trimEnd()     # "  Hello"

# Replace
replaced: text.replace("World", "TuskLang")  # "Hello, TuskLang!"
replace_all: text.replaceAll("l", "L")       # "HeLLo, WorLd!"

# Replace with regex
no_vowels: text.replace(/[aeiou]/gi, "")     # "Hll, Wrld!"

# Repeat
repeated: "Ha".repeat(3)       # "HaHaHa"

# Pad
padded_start: "5".padStart(3, "0")   # "005"
padded_end: "Hello".padEnd(10, ".")  # "Hello....."
```

## String Splitting and Joining

```tusk
# Split string
csv: "apple,banana,orange"
fruits: csv.split(",")         # ["apple", "banana", "orange"]

# Split with limit
limited: csv.split(",", 2)     # ["apple", "banana,orange"]

# Split by regex
words: "Hello   World".split(/\s+/)  # ["Hello", "World"]

# Split into characters
chars: "Hello".split("")       # ["H", "e", "l", "l", "o"]

# Join array
array: ["Hello", "World"]
joined: array.join(" ")        # "Hello World"
custom: array.join(" - ")      # "Hello - World"

# Advanced splitting
smart_split: (str, delimiter, options: {}) => {
    { trim: true, remove_empty: true }: options
    
    parts: str.split(delimiter)
    
    if (trim) {
        parts: parts.map(p => p.trim())
    }
    
    if (remove_empty) {
        parts: parts.filter(p => p.length > 0)
    }
    
    return parts
}
```

## Regular Expressions

```tusk
# Create regex
pattern: /hello/i              # Case insensitive
global: /hello/gi              # Global + case insensitive

# Test pattern
is_email: /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test("user@example.com")

# Match
text: "Hello 123 World 456"
numbers: text.match(/\d+/g)    # ["123", "456"]

# Match with groups
email: "john@example.com"
parts: email.match(/^([^@]+)@(.+)$/)
username: parts[1]             # "john"
domain: parts[2]               # "example.com"

# Replace with function
capitalized: text.replace(/\b\w/g, (char) => char.toUpperCase())

# Split with regex
words: text.split(/\s+/)

# Advanced regex operations
extract_urls: (text) => {
    url_pattern: /(https?:\/\/[^\s]+)/g
    return text.match(url_pattern) || []
}
```

## String Comparison

```tusk
# Basic comparison
"apple" < "banana"             # true (lexicographic)
"Apple" < "apple"              # true (capitals first)

# Case-insensitive comparison
compare_ci: (a, b) => {
    return a.toLowerCase() === b.toLowerCase()
}

# localeCompare for proper sorting
"ä".localeCompare("z", "en")   # -1 (ä before z)
"ä".localeCompare("z", "sv")   # 1  (ä after z in Swedish)

# Natural sort comparison
natural_compare: (a, b) => {
    return a.localeCompare(b, undefined, {
        numeric: true,
        sensitivity: 'base'
    })
}

# Examples
["item2", "item10", "item1"].sort(natural_compare)
# Result: ["item1", "item2", "item10"]
```

## String Formatting

```tusk
# String interpolation
name: "John"
age: 30
formatted: `${name} is ${age} years old`

# Printf-style formatting (with helper)
sprintf: (format, ...args) => {
    i: 0
    return format.replace(/%[sdif]/g, (match) => {
        arg: args[i++]
        switch (match) {
            case '%s': return String(arg)
            case '%d': case '%i': return parseInt(arg)
            case '%f': return parseFloat(arg)
            default: return match
        }
    })
}

message: sprintf("Hello %s, you have %d points", "John", 100)

# Custom formatting
format_template: (template, data) => {
    return template.replace(/\{(\w+)\}/g, (match, key) => {
        return data[key] ?? match
    })
}

result: format_template("Hello {name}, welcome to {site}!", {
    name: "John",
    site: "TuskLang"
})
```

## String Validation

```tusk
# Empty check
is_empty: (str) => !str || str.length === 0

# Whitespace only
is_blank: (str) => !str || str.trim().length === 0

# Email validation
is_email: (email) => {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)
}

# URL validation
is_url: (url) => {
    try {
        new URL(url)
        return true
    } catch {
        return false
    }
}

# Phone validation
is_phone: (phone) => {
    # Remove non-digits
    digits: phone.replace(/\D/g, "")
    return digits.length >= 10 && digits.length <= 15
}

# Custom validators
validators: {
    alpha: /^[a-zA-Z]+$/,
    alphanumeric: /^[a-zA-Z0-9]+$/,
    numeric: /^\d+$/,
    hex_color: /^#[0-9A-Fa-f]{6}$/
}

validate: (str, type) => validators[type]?.test(str) ?? false
```

## String Encoding

```tusk
# Base64 encoding
base64_encode: (str) => btoa(str)
base64_decode: (encoded) => atob(encoded)

# URL encoding
url_encode: (str) => encodeURIComponent(str)
url_decode: (encoded) => decodeURIComponent(encoded)

# HTML encoding
html_encode: (str) => {
    return str.replace(/[&<>"']/g, (char) => {
        entities: {
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#39;'
        }
        return entities[char]
    })
}

# Custom encoding
rot13: (str) => {
    return str.replace(/[a-zA-Z]/g, (char) => {
        code: char.charCodeAt(0)
        base: code < 97 ? 65 : 97
        return String.fromCharCode((code - base + 13) % 26 + base)
    })
}
```

## Performance Tips

```tusk
# Use string builder for many concatenations
# Bad
result: ""
for (i: 0; i < 1000; i++) {
    result += "text"  # Creates new string each time
}

# Good
parts: []
for (i: 0; i < 1000; i++) {
    parts.push("text")
}
result: parts.join("")

# Cache regex patterns
email_pattern: /^[^\s@]+@[^\s@]+\.[^\s@]+$/
# Reuse pattern instead of creating new regex each time

# Use indexOf for simple searches
# Faster than regex for simple substring search
if (text.indexOf("search") !== -1) {
    # Found
}
```

## Best Practices

1. **Use template literals for interpolation** - Cleaner than concatenation
2. **Choose the right string method** - slice() vs substring() vs substr()
3. **Be careful with regex** - Can be slow on large strings
4. **Handle Unicode properly** - Consider multi-byte characters
5. **Validate and sanitize input** - Prevent injection attacks
6. **Use localeCompare for sorting** - Handles international characters
7. **Cache compiled regex** - Don't recreate patterns
8. **Consider memory with large strings** - Use streaming when possible

## Related Topics

- `regex-patterns` - Regular expression patterns
- `template-literals` - Advanced templating
- `unicode-handling` - Working with Unicode
- `string-encoding` - Character encodings
- `text-processing` - Advanced text manipulation