# Escaping Characters in TuskLang

Understanding character escaping is essential for handling special characters, preventing syntax errors, and ensuring your strings contain exactly what you intend. This guide covers all aspects of character escaping in TuskLang.

## Basic Escape Sequences

### Common Escape Characters

```tusk
# Newline
line_break: "First line\nSecond line"

# Tab
tabbed: "Column1\tColumn2\tColumn3"

# Carriage return
cr_text: "Overwrite\rText"

# Backslash
path: "C:\\Users\\Documents"

# Quotes in strings
single_in_double: "It's working"
double_in_double: "She said, \"Hello!\""
single_in_single: 'It\'s working'
double_in_single: 'She said, "Hello!"'
```

### Complete Escape Reference

```tusk
escapes:
    newline: "\n"          # Line feed (LF)
    carriage: "\r"         # Carriage return (CR)
    tab: "\t"              # Horizontal tab
    backslash: "\\"        # Backslash
    double_quote: "\""     # Double quote
    single_quote: "\'"     # Single quote
    null_char: "\0"        # Null character
    bell: "\a"             # Alert/Bell
    backspace: "\b"        # Backspace
    form_feed: "\f"        # Form feed
    vertical_tab: "\v"     # Vertical tab
```

## Unicode Escapes

### Unicode Code Points

```tusk
# Unicode escape sequences
unicode_chars:
    # Basic unicode escape (4 digits)
    heart: "\u2764"        # ❤
    star: "\u2605"         # ★
    check: "\u2713"        # ✓
    
    # Extended unicode (with braces)
    emoji_smile: "\u{1F600}"    # 😀
    emoji_rocket: "\u{1F680}"   # 🚀
    emoji_wave: "\u{1F44B}"     # 👋
    
    # Combining characters
    accented: "e\u0301"    # é (e + combining acute accent)
```

### Hexadecimal Escapes

```tusk
# Hex escape sequences
hex_chars:
    space: "\x20"          # Space character
    uppercase_a: "\x41"    # 'A'
    delete: "\x7F"         # DEL character
    
# Building strings with hex
ascii_art: "\x2A\x2A\x2A\x20\x48\x65\x6C\x6C\x6F\x20\x2A\x2A\x2A"
# Result: "*** Hello ***"
```

## Escaping in Different Contexts

### String Interpolation

```tusk
name: "World"

# Escape the dollar sign to prevent interpolation
literal_dollar: "The price is \$100"           # Result: "The price is $100"
interpolated: "Hello, ${name}!"                # Result: "Hello, World!"
escaped_interp: "The syntax is \${variable}"   # Result: "The syntax is ${variable}"

# Complex escaping
template: "Use \${var} for interpolation, \\n for newline"
```

### Regular Expressions

```tusk
# Regex patterns need proper escaping
patterns:
    # Escape regex special characters
    literal_dot: "\\."              # Matches literal period
    literal_star: "\\*"             # Matches literal asterisk
    backslash: "\\\\"               # Matches single backslash
    
    # Common regex patterns
    email: "^[^@]+@[^@]+\\.[^@]+$"
    phone: "\\+?\\d{1,3}[-.\\s]?\\d{3,4}[-.\\s]?\\d{4}"
    
    # Character classes
    whitespace: "\\s+"              # One or more whitespace
    word_boundary: "\\bword\\b"     # Word boundaries
    digit: "\\d{3}"                 # Three digits
```

### JSON Strings

```tusk
# Escaping for JSON
json_data: """
{
    "message": "Hello \"World\"",
    "path": "C:\\\\Users\\\\file.txt",
    "special": "Line 1\\nLine 2\\tTabbed"
}
"""

# Or use a function to auto-escape
safe_json = @json.stringify({
    message: 'Hello "World"'
    path: "C:\\Users\\file.txt"
    special: "Line 1\nLine 2\tTabbed"
})
```

### SQL Queries

```tusk
# Escape single quotes in SQL
user_input: "O'Brien"
safe_input = @replace(user_input, "'", "''")

query: "SELECT * FROM users WHERE name = '${safe_input}'"

# Better: Use parameterized queries
@db.query("SELECT * FROM users WHERE name = ?", user_input)
```

## Special String Syntax

### Raw Strings

```tusk
# Use single quotes to reduce escaping needs
raw_regex: '\\d{3}-\\d{3}-\\d{4}'  # Phone pattern
raw_path: 'C:\Users\Documents'      # Windows path

# Triple single quotes for raw heredoc
raw_heredoc: '''
This text contains ${variables} and \n escape sequences
that are NOT processed. What you see is what you get.
'''
```

### Verbatim Strings

```tusk
# For paths and patterns
windows_path: @raw("C:\Users\John\Documents\file.txt")
regex_pattern: @raw("\d{3}-\d{3}-\d{4}")

# Alternative syntax with backticks (if supported)
verbatim: `This is a \raw\ string with no \n processing`
```

## Escaping in URLs and URIs

### URL Encoding

```tusk
# Manual URL encoding
search_term: "hello world & more"
encoded: @replace_all(search_term, {
    " ": "%20",
    "&": "%26",
    "=": "%3D",
    "?": "%3F",
    "#": "%23"
})

# Better: Use built-in encoding
safe_url = "https://example.com/search?q=${@url.encode(search_term)}"

# Decode URL parameters
encoded_param: "hello%20world%20%26%20more"
decoded = @url.decode(encoded_param)  # "hello world & more"
```

### Path Escaping

```tusk
# File system paths
filename: "my file (2023).txt"
safe_filename = @replace_all(filename, {
    " ": "_",
    "(": "",
    ")": "",
    "&": "and"
})

# URL path segments
segment: "user/profile"
escaped_segment = @url.encode_path(segment)  # "user%2Fprofile"
```

## HTML and XML Escaping

### HTML Entities

```tusk
# HTML escape mapping
html_escapes:
    "&": "&amp;"
    "<": "&lt;"
    ">": "&gt;"
    '"': "&quot;"
    "'": "&#39;"

# Manual HTML escaping
user_content: "<script>alert('XSS')</script>"
safe_html = @replace_all(user_content, html_escapes)

# Built-in HTML escaping
escaped = @html.escape(user_content)
# Result: "&lt;script&gt;alert(&#39;XSS&#39;)&lt;/script&gt;"

# Unescape HTML
unescaped = @html.unescape("&lt;div&gt;Hello&lt;/div&gt;")
# Result: "<div>Hello</div>"
```

### XML CDATA

```tusk
# When you need to include raw content in XML
xml_content: """
<data>
    <![CDATA[
    This can contain <tags> and & ampersands
    without escaping!
    ]]>
</data>
"""
```

## Shell Command Escaping

### Command Line Arguments

```tusk
# Escape shell arguments
user_file: "my file.txt"
safe_file = @shell.quote(user_file)  # 'my file.txt'

command = "cat ${safe_file}"

# Or use array form (safer)
@exec(["cat", user_file])  # No escaping needed

# Windows command escaping
windows_arg: 'path with "quotes"'
escaped_arg = @replace_all(windows_arg, {
    '"': '""',
    "^": "^^",
    "&": "^&",
    "|": "^|"
})
```

## Common Escaping Patterns

### Building Safe Strings

```tusk
# Function to escape for different contexts
escape_for_context = @lambda(text, context, {
    @switch(context, {
        "html": @html.escape(text),
        "url": @url.encode(text),
        "sql": @sql.escape(text),
        "regex": @regex.escape(text),
        "shell": @shell.quote(text),
        default: text
    })
})

# Usage
user_input: "dangerous <script> & 'quotes'"
safe_for_html = @escape_for_context(user_input, "html")
safe_for_sql = @escape_for_context(user_input, "sql")
```

### Template Literal Escaping

```tusk
# Escape template literals
template: "Hello, \${name}! Your balance is \$${balance}."
# Result: "Hello, ${name}! Your balance is $100."

# Function to escape template syntax
escape_template = @lambda(str, {
    @replace_all(str, {
        "$": "\\$",
        "{": "\\{",
        "}": "\\}"
    })
})
```

### CSV Field Escaping

```tusk
# CSV field escaping rules
escape_csv_field = @lambda(field, {
    # Fields with comma, quote, or newline need quoting
    @if(@contains(field, ",") || @contains(field, '"') || @contains(field, "\n"), {
        # Escape quotes by doubling them
        escaped = @replace(field, '"', '""')
        return: '"${escaped}"'
    }, field)
})

# Example
fields: ["Normal", 'With "quotes"', "With, comma", "With\nnewline"]
csv_line = @join(@map(fields, escape_csv_field), ",")
```

## Error Prevention

### Validation Before Escaping

```tusk
# Validate input before processing
safe_process = @lambda(input, {
    # Check for null/undefined
    @if(!input, return: "")
    
    # Convert to string if needed
    str_input = @string(input)
    
    # Apply appropriate escaping
    return: @html.escape(str_input)
})
```

### Context-Aware Escaping

```tusk
# Different contexts need different escaping
render_user_content = @lambda(content, context, {
    @switch(context, {
        "html_body": @html.escape(content),
        "html_attribute": @html.escape(content, { quotes: true }),
        "javascript": @json.stringify(content),
        "url_param": @url.encode(content),
        "css": @css.escape(content),
        default: @error("Unknown context: ${context}")
    })
})
```

## Best Practices

### 1. Know Your Context

```tusk
# Always escape based on where the string will be used
user_name: "Alice <admin>"

# Different contexts need different escaping
html_context: @html.escape(user_name)         # "Alice &lt;admin&gt;"
url_context: @url.encode(user_name)           # "Alice%20%3Cadmin%3E"
js_context: @json.stringify(user_name)        # "\"Alice <admin>\""
```

### 2. Use Built-in Functions

```tusk
# Prefer built-in escaping functions
# Good
safe_html = @html.escape(user_input)
safe_url = @url.encode(user_input)

# Avoid manual escaping when possible
# Not ideal
manual_html = @replace_all(user_input, {"<": "&lt;", ">": "&gt;"})
```

### 3. Escape Late

```tusk
# Store raw data, escape on output
user:
    name: "O'Brien"  # Store raw
    bio: "I <3 coding"  # Store raw

# Escape when rendering
html_output: """
<div>
    <h1>${@html.escape(user.name)}</h1>
    <p>${@html.escape(user.bio)}</p>
</div>
"""
```

### 4. Document Escaping Requirements

```tusk
###
# Renders user comment
# @param comment Raw user input (unescaped)
# @param format Output format: "html", "text", "markdown"
# @returns Properly escaped string for the target format
###
render_comment = @lambda(comment, format, {
    # Implementation
})
```

## Common Mistakes

### Double Escaping

```tusk
# Wrong - double escaping
user_input: "<script>"
escaped_once = @html.escape(user_input)      # "&lt;script&gt;"
escaped_twice = @html.escape(escaped_once)   # "&amp;lt;script&amp;gt;"

# Right - escape once at output
raw_data: "<script>"
output = @html.escape(raw_data)
```

### Wrong Context Escaping

```tusk
# Wrong - HTML escaping for URLs
url = "https://example.com/search?q=${@html.escape(search_term)}"

# Right - URL encoding for URLs
url = "https://example.com/search?q=${@url.encode(search_term)}"
```

### Forgetting to Escape

```tusk
# Dangerous - no escaping
html = "<div>${user_input}</div>"  # XSS vulnerability!

# Safe - proper escaping
html = "<div>${@html.escape(user_input)}</div>"
```

## Next Steps

- Master [String Interpolation](014-strings.md)
- Learn about [Regular Expressions](014-strings.md#regular-expressions)
- Understand [Security Best Practices](060-at-operator-security.md)