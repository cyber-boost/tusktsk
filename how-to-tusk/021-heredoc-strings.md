# Heredoc Strings in TuskLang

Heredoc strings provide a clean way to define multi-line text content in TuskLang. They're perfect for templates, documentation, SQL queries, and any other multi-line string content.

## Basic Heredoc Syntax

### Triple Quotes

```tusk
# Basic heredoc with """
message: """
Hello World!
This is a multi-line string.
It preserves line breaks and formatting.
"""

# The string includes the line breaks
# Result: "Hello World!\nThis is a multi-line string.\nIt preserves line breaks and formatting."
```

### Indentation Handling

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

# Leading spaces are preserved in the output
```

## Heredoc with Interpolation

### Variable Substitution

```tusk
name: "John"
age: 30

# Heredoc with interpolation
profile: """
Name: ${name}
Age: ${age}
Status: Active
"""

# Complex interpolation
user_data: {
    email: "john@example.com"
    role: "admin"
}

details: """
User Profile
============
Name: ${name}
Email: ${user_data.email}
Role: ${user_data.role}
Account Created: ${@time.now()}
"""
```

### Expressions in Heredoc

```tusk
items: ["apple", "banana", "orange"]
tax_rate: 0.08

receipt: """
Shopping Receipt
================
Items: ${@join(items, ", ")}
Subtotal: $${subtotal}
Tax (${tax_rate * 100}%): $${subtotal * tax_rate}
Total: $${subtotal * (1 + tax_rate)}

Thank you for shopping!
"""
```

## Special Heredoc Markers

### Custom Delimiters

```tusk
# Using custom end markers
sql_query: <<SQL
SELECT u.id, u.name, u.email
FROM users u
WHERE u.active = true
  AND u.created_at > '2024-01-01'
ORDER BY u.name
SQL

# HTML content
html_template: <<HTML
<!DOCTYPE html>
<html>
    <head>
        <title>${page_title}</title>
    </head>
    <body>
        <h1>${heading}</h1>
        <p>${content}</p>
    </body>
</html>
HTML
```

### Indented Heredoc Markers

```tusk
# Strip common leading whitespace with <<<
formatted_text: <<<
    This text has
    consistent indentation
    that will be stripped
>>>

# Result has no leading spaces:
# "This text has\nconsistent indentation\nthat will be stripped"
```

## Practical Use Cases

### Configuration Files

```tusk
# Generate configuration files
nginx_config: """
server {
    listen ${port};
    server_name ${server_name};
    
    location / {
        proxy_pass http://${backend_host}:${backend_port};
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
    
    location /static {
        alias ${static_path};
        expires 30d;
    }
}
"""

# Write to file
@file.write("/etc/nginx/sites-available/${site_name}", nginx_config)
```

### Email Templates

```tusk
# Email template with heredoc
welcome_email: """
Subject: Welcome to ${app_name}!

Dear ${user_name},

Welcome to ${app_name}! We're excited to have you on board.

Your account has been created with the following details:
- Username: ${username}
- Email: ${email}
- Account Type: ${account_type}

To get started:
1. Log in at ${login_url}
2. Complete your profile
3. Explore our features

If you have any questions, feel free to contact us at ${support_email}.

Best regards,
The ${app_name} Team

--
This is an automated message. Please do not reply to this email.
"""
```

### SQL Queries

```tusk
# Complex SQL with heredoc
report_query: """
WITH monthly_sales AS (
    SELECT 
        DATE_TRUNC('month', order_date) as month,
        SUM(total_amount) as revenue,
        COUNT(DISTINCT customer_id) as customers
    FROM orders
    WHERE order_date >= '${start_date}'
      AND order_date <= '${end_date}'
      AND status = 'completed'
    GROUP BY DATE_TRUNC('month', order_date)
)
SELECT 
    TO_CHAR(month, 'YYYY-MM') as period,
    revenue,
    customers,
    revenue / NULLIF(customers, 0) as avg_order_value
FROM monthly_sales
ORDER BY month DESC
"""

# Execute query
results = @db.query(report_query)
```

### Code Generation

```tusk
# Generate code with heredoc
generate_class = @lambda(class_name, properties, {
    code: """
class ${class_name} {
    constructor(${@join(@map(properties, @lambda(p, p.name)), ", ")}) {
${@join(@map(properties, @lambda(p, "        this.${p.name} = ${p.name};")), "\n")}
    }
    
${@join(@map(properties, @lambda(p, """
    get${@capitalize(p.name)}() {
        return this.${p.name};
    }
    
    set${@capitalize(p.name)}(value) {
        this.${p.name} = value;
    }""")), "\n\n")}
}

module.exports = ${class_name};
"""
    
    return: code
})
```

## Heredoc Best Practices

### 1. Choose Appropriate Delimiters

```tusk
# Use meaningful delimiters for content type
json_data: <<JSON
{
    "name": "${name}",
    "version": "${version}"
}
JSON

xml_data: <<XML
<config>
    <name>${name}</name>
    <version>${version}</version>
</config>
XML

shell_script: <<BASH
#!/bin/bash
echo "Installing ${app_name}..."
mkdir -p ${install_dir}
cp -r ./files/* ${install_dir}/
BASH
```

### 2. Handle Escaping

```tusk
# Escape special characters when needed
regex_pattern: """
^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$
"""

# Or use raw heredoc (no interpolation)
raw_text: '''
This ${variable} won't be interpolated.
Special characters like \ and " are preserved as-is.
'''
```

### 3. Format for Readability

```tusk
# Well-formatted heredoc
markdown: """
# ${title}

## Introduction

${introduction_text}

## Features

${@join(@map(features, @lambda(f, "- ${f}")), "\n")}

## Installation

\`\`\`bash
${installation_commands}
\`\`\`

## Usage

${usage_instructions}

---

*Generated on ${@time.format(@time.now(), "YYYY-MM-DD")}*
"""
```

## Advanced Heredoc Patterns

### Conditional Content

```tusk
# Include content conditionally
email_body: """
Hi ${name},

${@if(is_new_user, """
Welcome to our platform! As a new user, here are some tips to get started:
- Complete your profile
- Explore our tutorials
- Join our community forum
""", """
Welcome back! Here's what's new since your last visit:
- New features added
- Performance improvements
- Bug fixes
""")}

Best regards,
The Team
"""
```

### Dynamic Indentation

```tusk
# Generate indented code
generate_nested = @lambda(items, level = 0, {
    indent = @repeat("  ", level)
    
    content: """
${@join(@map(items, @lambda(item, {
    @if(item.children, """
${indent}${item.name}:
${@generate_nested(item.children, level + 1)}""", 
    "${indent}${item.name}: ${item.value}")
})), "\n")}
"""
    
    return: @trim(content)
})
```

### Template Composition

```tusk
# Compose templates from parts
header_template: """
<!DOCTYPE html>
<html>
<head>
    <title>${title}</title>
    <meta charset="UTF-8">
</head>
<body>
"""

footer_template: """
    <footer>
        <p>&copy; ${year} ${company_name}</p>
    </footer>
</body>
</html>
"""

# Combine templates
full_page = @lambda(title, content, {
    return: """
${header_template}
    <main>
        ${content}
    </main>
${footer_template}
"""
})
```

## Heredoc with Functions

### Processing Heredoc Content

```tusk
# Process heredoc content
csv_data: """
name,age,city
John,30,New York
Jane,25,Los Angeles
Bob,35,Chicago
"""

# Parse CSV from heredoc
rows = @csv.parse(csv_data)

# Minify heredoc content
minified_js: @minify("""
    function hello(name) {
        console.log('Hello, ' + name);
        return true;
    }
""")
```

### Heredoc in Lambdas

```tusk
# Return heredoc from function
generate_report = @lambda(data, {
    return: """
    Sales Report
    ============
    
    Period: ${data.start_date} to ${data.end_date}
    
    Summary:
    - Total Sales: $${data.total_sales}
    - Number of Orders: ${data.order_count}
    - Average Order Value: $${data.avg_order_value}
    
    Top Products:
    ${@join(@map(data.top_products, @lambda(p, "- ${p.name}: $${p.revenue}")), "\n")}
    """
})
```

## Common Pitfalls

### Unintended Whitespace

```tusk
# Be careful with trailing spaces
text: """
Line with trailing spaces    
Another line
"""  # The spaces after "spaces" are preserved

# Use trim if needed
clean_text = @trim(text)
```

### Quote Confusion

```tusk
# Triple quotes must be on their own line
# Wrong:
# text: """Hello
# World"""

# Right:
text: """
Hello
World
"""
```

### Interpolation Issues

```tusk
# Escape $ if you don't want interpolation
price_template: """
The price is \$${price}
"""  # Results in "The price is $19.99"

# Or use single-quote heredoc
literal: '''
The ${variable} syntax is preserved literally
'''
```

## Performance Considerations

### Large Heredocs

```tusk
# For very large static content, consider external files
# Instead of:
huge_template: """
[10,000 lines of content]
"""

# Better:
huge_template: @file.read("templates/huge_template.txt")
```

### Repeated Interpolation

```tusk
# Cache interpolated results if used multiple times
email_template: """
Dear ${customer_name},
[rest of template]
"""

# If sending many emails, cache the base template
base_template: @template.compile(email_template)
final_email = @base_template.render({ customer_name: "John" })
```

## Best Practices Summary

1. **Use heredoc for multi-line strings** - Much cleaner than concatenation
2. **Choose meaningful delimiters** - SQL, HTML, JSON, etc.
3. **Be mindful of whitespace** - Heredocs preserve formatting
4. **Use interpolation wisely** - Escape when needed
5. **Consider external files** for very large content
6. **Strip common indentation** with <<< when appropriate
7. **Document heredoc purpose** with comments

## Next Steps

- Learn about [Escaping Characters](022-escaping-characters.md)
- Master [String Interpolation](014-strings.md)
- Explore [Template Patterns](030-best-practices.md)