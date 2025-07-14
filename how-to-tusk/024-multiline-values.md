# Multiline Values in TuskLang

TuskLang provides several ways to work with multiline values, from simple string continuations to complex heredoc syntax. This guide covers all methods for handling multiline content effectively.

## String Continuation

### Basic Line Continuation

```tusk
# Using backslash for continuation
long_string: "This is a very long string that \
continues on the next line without adding \
a newline character to the string content."

# Result: "This is a very long string that continues on the next line without adding a newline character to the string content."
```

### Preserving Line Breaks

```tusk
# Using quotes with actual line breaks
multiline: "First line
Second line
Third line"

# The line breaks are preserved in the string
# Result: "First line\nSecond line\nThird line"
```

## Heredoc Syntax

### Basic Heredoc

```tusk
# Triple quotes for heredoc
message: """
Welcome to our application!

This message spans multiple lines
and preserves all formatting including:
  - Indentation
  - Line breaks
  - Special characters

Thank you for using our service.
"""
```

### Heredoc with Custom Delimiters

```tusk
# SQL query with custom delimiter
query: <<SQL
SELECT 
    u.id,
    u.name,
    u.email,
    COUNT(o.id) as order_count
FROM users u
LEFT JOIN orders o ON u.id = o.user_id
WHERE u.created_at > '2024-01-01'
GROUP BY u.id, u.name, u.email
ORDER BY order_count DESC
SQL

# HTML template
html_template: <<HTML
<!DOCTYPE html>
<html lang="en">
<head>
    <title>${page_title}</title>
    <meta charset="UTF-8">
</head>
<body>
    <h1>${heading}</h1>
    <div class="content">
        ${content}
    </div>
</body>
</html>
HTML
```

### Indented Heredoc

```tusk
# Strip common leading whitespace with <<<
function_code: <<<
    function processData(data) {
        // This indentation will be removed
        const result = data.map(item => {
            return item * 2;
        });
        return result;
    }
>>>

# The function will start at column 0 after processing
```

## Multiline Arrays

### Vertical Arrays

```tusk
# Each element on its own line
fruits: [
    "apple",
    "banana",
    "cherry",
    "date",
    "elderberry"
]

# With trailing comma (recommended)
colors: [
    "red",
    "green", 
    "blue",
]
```

### Complex Array Elements

```tusk
# Array of objects with multiline formatting
users: [
    {
        id: 1
        name: "Alice Johnson"
        email: "alice@example.com"
        roles: ["admin", "user"]
        address: {
            street: "123 Main St"
            city: "Springfield"
            state: "IL"
        }
    },
    {
        id: 2
        name: "Bob Smith"
        email: "bob@example.com"
        roles: ["user"]
        address: {
            street: "456 Oak Ave"
            city: "Portland"
            state: "OR"
        }
    }
]
```

## Multiline Objects

### Nested Object Structure

```tusk
# Well-formatted nested object
application:
    metadata:
        name: "Enterprise App"
        version: "2.1.0"
        description: """
        A comprehensive enterprise application
        that handles multiple business processes
        including inventory, sales, and reporting.
        """
    
    modules:
        inventory:
            enabled: true
            features: [
                "stock_tracking",
                "reorder_alerts",
                "barcode_scanning"
            ]
        
        sales:
            enabled: true
            features: [
                "point_of_sale",
                "online_orders",
                "customer_management"
            ]
```

### Method Definitions

```tusk
# Object with multiline method definitions
string_utils:
    # Multiline method with proper formatting
    word_wrap: @lambda(text, width, {
        words = @split(text, " ")
        lines: []
        current_line: ""
        
        @each(words, @lambda(word, {
            @if(@len(current_line + " " + word) > width, {
                @push(lines, current_line)
                current_line = word
            }, {
                current_line = @if(current_line == "", 
                    word, 
                    current_line + " " + word
                )
            })
        }))
        
        @if(current_line != "", {
            @push(lines, current_line)
        })
        
        return: @join(lines, "\n")
    })
```

## Multiline Function Calls

### Breaking Long Function Calls

```tusk
# Function call with many parameters
result = @database.query(
    "SELECT * FROM users WHERE status = ? AND created_at > ?",
    "active",
    "2024-01-01",
    {
        timeout: 30000,
        cache: true,
        retry_count: 3
    }
)

# Method chaining across lines
processed_data = @data
    .filter(@lambda(item, item.active))
    .map(@lambda(item, {
        id: item.id,
        name: item.name,
        value: item.value * 1.1
    }))
    .sort(@lambda(a, b, b.value - a.value))
    .slice(0, 10)
```

## Multiline Conditionals

### Complex If Statements

```tusk
# Multiline conditional logic
user_status = @if(
    user.active && 
    user.email_verified && 
    user.subscription_status == "active",
    {
        # True branch
        status: "active"
        access_level: "full"
        features: ["all"]
    },
    {
        # False branch
        status: "restricted"
        access_level: "limited"
        features: ["basic"]
    }
)

# Nested conditionals with formatting
category = @if(score >= 90, {
    grade: "A"
    message: "Excellent!"
}, @if(score >= 80, {
    grade: "B" 
    message: "Good job!"
}, @if(score >= 70, {
    grade: "C"
    message: "Satisfactory"
}, {
    grade: "F"
    message: "Needs improvement"
})))
```

## Multiline Comments

### Block Documentation

```tusk
###
# User Management Module
# 
# This module handles all user-related operations including:
# - User registration and authentication
# - Profile management
# - Permission and role assignment
# - Session management
# 
# Dependencies:
# - Database module
# - Encryption module
# - Email module
# 
# @author: Development Team
# @version: 1.0.0
# @since: 2024-01-01
###

# Regular comments can also span
# multiple lines by using multiple
# single-line comments
```

## Multiline String Operations

### String Building

```tusk
# Building complex strings
email_body = @string.build({
    lines: [
        "Dear ${customer_name},",
        "",
        "Thank you for your recent purchase.",
        "Your order details are as follows:",
        "",
        "Order ID: ${order_id}",
        "Date: ${order_date}",
        "Total: $${order_total}",
        "",
        "Best regards,",
        "The Sales Team"
    ]
    separator: "\n"
})

# Or using array join
report = @join([
    "Monthly Sales Report",
    @repeat("=", 50),
    "",
    "Period: ${start_date} to ${end_date}",
    "Total Sales: $${total_sales}",
    "Total Orders: ${order_count}",
    "",
    "Top Products:",
    ...@map(top_products, @lambda(p, "  - ${p.name}: $${p.revenue}"))
], "\n")
```

### Template Strings

```tusk
# Complex template with multiline content
email_template: """
<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; }
        .header { background: #007bff; color: white; padding: 20px; }
        .content { padding: 20px; }
        .footer { background: #f8f9fa; padding: 10px; text-align: center; }
    </style>
</head>
<body>
    <div class="header">
        <h1>${company_name}</h1>
    </div>
    <div class="content">
        ${email_content}
    </div>
    <div class="footer">
        <p>&copy; ${current_year} ${company_name}. All rights reserved.</p>
    </div>
</body>
</html>
"""
```

## Multiline Regular Expressions

### Complex Patterns

```tusk
# Regex with comments and formatting
email_pattern: @regex.compile("""
    ^                      # Start of string
    [a-zA-Z0-9._%+-]+     # Local part
    @                      # At symbol
    [a-zA-Z0-9.-]+        # Domain name
    \.                     # Dot
    [a-zA-Z]{2,}          # Top-level domain
    $                      # End of string
""", "x")  # x flag for extended/verbose mode

# URL pattern with multiline definition
url_pattern: @regex.compile(
    "^(https?://)?" +                    # Protocol (optional)
    "([a-zA-Z0-9-]+\\.)*" +              # Subdomains
    "[a-zA-Z0-9-]+" +                    # Domain name
    "\\.[a-zA-Z]{2,}" +                  # TLD
    "(:[0-9]+)?" +                       # Port (optional)
    "(/[^\\s]*)?" +                      # Path (optional)
    "$",
    "i"  # Case insensitive
)
```

## Best Practices

### 1. Choose the Right Method

```tusk
# Short multiline - use quotes
short_message: "Line 1
Line 2"

# Long multiline - use heredoc
long_message: """
This is a much longer message
that spans many lines and includes
various formatting and structure.
"""

# Code or structured text - use custom delimiter
code_block: <<CODE
function example() {
    return "Use delimiter that matches content type";
}
CODE
```

### 2. Maintain Readability

```tusk
# Good - clear structure
config:
    database:
        primary:
            host: "db1.example.com"
            port: 5432
            connection_string: """
            postgresql://username:password@db1.example.com:5432/mydb
            ?sslmode=require&connect_timeout=10
            """

# Avoid - hard to read
config: { database: { primary: { host: "db1.example.com", port: 5432, connection_string: "postgresql://username:password@db1.example.com:5432/mydb?sslmode=require&connect_timeout=10" } } }
```

### 3. Consider Line Length

```tusk
# Break long lines appropriately
long_condition = 
    user.is_active && 
    user.email_verified && 
    user.age >= 18 && 
    user.country_code == "US" &&
    user.agreed_to_terms == true

# Format for scanning
api_endpoints: [
    "/api/v1/users",
    "/api/v1/products", 
    "/api/v1/orders",
    "/api/v1/customers",
    "/api/v1/reports"
]
```

### 4. Use Consistent Formatting

```tusk
# Consistent multiline style throughout project
# If using heredoc for SQL, use it for all SQL
query1: <<SQL
SELECT * FROM users
SQL

query2: <<SQL  
SELECT * FROM products
SQL

# Not mixed styles
query3: "SELECT * FROM orders"  # Inconsistent!
```

## Common Pitfalls

### Indentation in Heredocs

```tusk
# Remember heredocs preserve indentation
template: """
    This line has 4 spaces at the start
"""

# Use <<< to strip common indentation
template_stripped: <<<
    This line will start at column 0
>>>
```

### Line Ending Differences

```tusk
# Be aware of platform differences
# Unix/Linux/Mac: \n
# Windows: \r\n

# Normalize if needed
normalized = @replace(text, "\r\n", "\n")
```

### Escaping in Multiline

```tusk
# Quotes still need escaping in multiline strings
message: """
He said, "Hello!"
But she replied, \"Goodbye!\"
"""

# Or use different quotes
message2: '''
He said, "Hello!"
No escaping needed here.
'''
```

## Next Steps

- Learn about [Typed Values](025-typed-values.md)
- Explore [References](026-references.md)
- Master [String Handling](014-strings.md)