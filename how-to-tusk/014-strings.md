# Strings in TuskLang

Strings are one of the most common data types in TuskLang. This guide covers everything you need to know about working with text data.

## Basic String Syntax

### Quoted Strings

TuskLang supports both single and double quotes:

```tusk
# Double quotes (preferred)
message: "Hello, World!"

# Single quotes (also valid)
greeting: 'Welcome to TuskLang'

# They're functionally identical
name1: "Alice"
name2: 'Alice'  # Same as above
```

### Unquoted Strings

Simple strings without spaces can be unquoted:

```tusk
# Valid unquoted strings
environment: production
protocol: https
status: active

# Must use quotes for:
message: "Hello World"      # Contains space
path: "C:\\Program Files"   # Contains backslash
url: "https://example.com"  # Contains special characters
```

## String Interpolation

### Basic Interpolation

Use `${}` for variable substitution in double-quoted strings:

```tusk
name: "TuskLang"
version: "1.0.0"

# String interpolation
message: "Welcome to ${name} version ${version}!"
# Result: "Welcome to TuskLang version 1.0.0!"

# Single quotes don't interpolate
literal: '${name} is literal'
# Result: "${name} is literal"
```

### Expression Interpolation

```tusk
count: 42
price: 19.99

# Expressions in interpolation
summary: "You have ${count} items totaling $${price * count}"

# Function calls
greeting: "Hello, ${@user.name.toUpperCase()}!"

# Conditional interpolation
status: "Server is ${@if(is_running, 'up', 'down')}"
```

### Nested Interpolation

```tusk
user:
    first_name: "John"
    last_name: "Doe"
    
# Accessing nested values
full_name: "${user.first_name} ${user.last_name}"

# Complex expressions
profile_url: "/users/${user.id}/profile?v=${@time.now()}"
```

## Escape Sequences

### Common Escapes

```tusk
# Newline
multiline: "Line 1\nLine 2"

# Tab
tabbed: "Name:\tJohn"

# Quotes
quoted: "She said, \"Hello!\""
apostrophe: 'It\'s working'

# Backslash
path: "C:\\Users\\John"

# Unicode
emoji: "Hello \u{1F44B}"  # 👋
```

### Escape Reference

```tusk
escapes:
    newline: "First\nSecond"          # Line break
    tab: "Col1\tCol2"                 # Tab character
    carriage: "Text\rOverwrite"       # Carriage return
    backslash: "Path\\to\\file"       # Literal backslash
    quote: "Say \"Hi\""               # Double quote
    single: 'Don\'t'                  # Single quote
    unicode: "\u{1F600}"              # 😀
    hex: "\x48\x65\x6C\x6C\x6F"      # Hello
```

## Multiline Strings

### Using Quotes

```tusk
# Multiline with quotes
description: "This is a long description that
spans multiple lines. The line breaks
are preserved in the string."

# With explicit line breaks
formatted: "Line 1\n\
            Line 2\n\
            Line 3"
```

### Heredoc Syntax

For large text blocks, use heredoc:

```tusk
# Basic heredoc
html: """
<!DOCTYPE html>
<html>
    <head>
        <title>Welcome</title>
    </head>
    <body>
        <h1>Hello, World!</h1>
    </body>
</html>
"""

# Heredoc with interpolation
email_template: """
Dear ${customer_name},

Thank you for your order #${order_id}.

Your items will be shipped to:
${shipping_address}

Best regards,
${company_name}
"""
```

### Indented Heredoc

```tusk
# Heredoc preserves indentation
yaml_content: """
    version: '3'
    services:
      web:
        image: nginx
        ports:
          - "80:80"
"""

# Strip leading whitespace with <<<
stripped: <<<
    This text has
    no leading spaces
    despite indentation
>>>
```

## String Operations

### Concatenation

```tusk
# Using interpolation
first: "Hello"
second: "World"
combined: "${first}, ${second}!"

# Using the concat operator
full_path = @concat("/home/", username, "/documents")

# Building strings dynamically
parts: ["Hello", "World"]
message = @join(parts, " ")  # "Hello World"
```

### String Methods

```tusk
text: "Hello, World!"

# Length
length = @len(text)  # 13

# Case conversion
upper = @upper(text)      # "HELLO, WORLD!"
lower = @lower(text)      # "hello, world!"
title = @title(text)      # "Hello, World!"

# Trimming
padded: "  Hello  "
trimmed = @trim(padded)   # "Hello"
```

### Searching and Replacing

```tusk
text: "The quick brown fox"

# Check if contains
has_quick = @contains(text, "quick")  # true

# Find position
position = @index(text, "brown")      # 10

# Replace
updated = @replace(text, "brown", "red")  # "The quick red fox"

# Replace all occurrences
template: "Hello, {name}. Welcome, {name}!"
filled = @replace_all(template, "{name}", "Alice")
```

### Splitting and Joining

```tusk
# Split string
csv: "apple,banana,orange"
fruits = @split(csv, ",")  # ["apple", "banana", "orange"]

# Split with limit
limited = @split("a-b-c-d", "-", 2)  # ["a", "b-c-d"]

# Join array
words: ["Hello", "World"]
sentence = @join(words, " ")  # "Hello World"

# Join with custom separator
path_parts: ["home", "user", "documents"]
path = @join(path_parts, "/")  # "home/user/documents"
```

## Regular Expressions

### Pattern Matching

```tusk
email: "user@example.com"

# Basic match
is_email = @regex.match(email, "^[^@]+@[^@]+\\.[^@]+$")

# Extract matches
text: "Order #12345 confirmed"
order_match = @regex.find(text, "#(\\d+)")
order_number = @order_match[1]  # "12345"

# Find all matches
urls: "Visit https://example.com and https://test.com"
all_urls = @regex.find_all(urls, "https://[^\\s]+")
```

### Pattern Replacement

```tusk
# Simple regex replace
phone: "123-456-7890"
clean_phone = @regex.replace(phone, "[^0-9]", "")  # "1234567890"

# Complex replacements
markdown: "**bold** and *italic*"
html = @regex.replace_all(markdown, {
    "\\*\\*(.+?)\\*\\*": "<strong>$1</strong>",
    "\\*(.+?)\\*": "<em>$1</em>"
})
```

## String Formatting

### Printf-style Formatting

```tusk
# Basic formatting
formatted = @format("Hello, %s!", "World")

# Multiple values
message = @format("%s has %d items", username, item_count)

# Number formatting
price_text = @format("Price: $%.2f", 19.99)  # "Price: $19.99"
```

### Template Strings

```tusk
# Define template
template: "Dear {{name}}, your order {{order_id}} is {{status}}."

# Fill template
message = @template.fill(template, {
    name: "John",
    order_id: "12345",
    status: "shipped"
})
```

## String Validation

### Built-in Validators

```tusk
# Email validation
email: "user@example.com"
is_valid_email = @validate.email(email)

# URL validation
url: "https://example.com"
is_valid_url = @validate.url(url)

# Custom validation
username: "john_doe123"
is_valid_username = @regex.match(username, "^[a-zA-Z0-9_]{3,20}$")
```

### Length Validation

```tusk
password: "secretpass"

# Check length
is_valid_length = @len(password) >= 8 && @len(password) <= 128

# Validate with message
validate_password = @lambda(pwd, {
    @if(@len(pwd) < 8, 
        @error("Password must be at least 8 characters"),
        true
    )
})
```

## Unicode and Encoding

### Unicode Support

```tusk
# Full Unicode support
greeting_english: "Hello"
greeting_japanese: "こんにちは"
greeting_arabic: "مرحبا"
greeting_emoji: "👋 Hello!"

# Character counting
emoji_text: "Hello 👋 World 🌍"
char_count = @len(emoji_text)  # Counts properly
```

### Encoding Operations

```tusk
# Base64 encoding
original: "Hello, World!"
encoded = @base64.encode(original)
decoded = @base64.decode(encoded)

# URL encoding
url_param: "hello world & more"
safe_param = @url.encode(url_param)  # "hello%20world%20%26%20more"
```

## String Comparison

### Basic Comparison

```tusk
str1: "Hello"
str2: "hello"

# Case-sensitive comparison
equal = str1 == str2  # false

# Case-insensitive comparison
equal_ignore_case = @lower(str1) == @lower(str2)  # true

# Comparison operators
result = "apple" < "banana"  # true (alphabetical)
```

### Advanced Comparison

```tusk
# Natural sort comparison
files: ["file10.txt", "file2.txt", "file1.txt"]
sorted = @sort.natural(files)  # ["file1.txt", "file2.txt", "file10.txt"]

# Locale-aware comparison
names: ["Åke", "Zebra", "Adam"]
sorted_locale = @sort.locale(names, "sv-SE")  # Swedish sorting
```

## Performance Tips

### String Building

```tusk
# Inefficient - multiple concatenations
result = ""
@each(items, @lambda(item, {
    result = result + item + ", "  # Creates new string each time
}))

# Efficient - use join
result = @join(items, ", ")

# Or use string builder
builder = @string.builder()
@each(items, @lambda(item, {
    @builder.append(item)
    @builder.append(", ")
}))
result = @builder.toString()
```

### String Pooling

```tusk
# Reuse common strings
status_active: "active"
status_inactive: "inactive"

users: [
    { name: "John", status: @status_active },
    { name: "Jane", status: @status_active },
    { name: "Bob", status: @status_inactive }
]
```

## Common Patterns

### SQL Query Building

```tusk
# Safe query building
table: "users"
conditions: ["active = true", "age > 18"]
query = "SELECT * FROM ${table} WHERE ${@join(conditions, ' AND ')}"

# With parameters
user_id: 123
safe_query = @sql.prepare("SELECT * FROM users WHERE id = ?", user_id)
```

### Path Manipulation

```tusk
# Building file paths
base_dir: "/home/user"
filename: "document.txt"
full_path = @path.join(base_dir, "documents", filename)

# Extract components
path: "/home/user/file.txt"
dir = @path.dirname(path)     # "/home/user"
base = @path.basename(path)   # "file.txt"
ext = @path.extension(path)   # ".txt"
```

### String Sanitization

```tusk
# HTML escaping
user_input: "<script>alert('XSS')</script>"
safe_html = @html.escape(user_input)

# SQL escaping
search_term: "O'Brien"
safe_sql = @sql.escape(search_term)

# General sanitization
filename: "my file (2).txt"
safe_filename = @regex.replace(filename, "[^a-zA-Z0-9.-]", "_")
```

## Best Practices

1. **Use double quotes** for strings that might need interpolation
2. **Use heredoc** for multi-line strings
3. **Escape special characters** properly
4. **Validate user input** before using in strings
5. **Use appropriate encoding** for different contexts
6. **Consider performance** when building large strings
7. **Be mindful of Unicode** in string operations

## Next Steps

- Learn about [Numbers](015-numbers.md) in TuskLang
- Explore [Boolean values](016-booleans.md)
- Master [Arrays](018-arrays.md) for string collections