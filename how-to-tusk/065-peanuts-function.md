# peanuts() - Lightweight Template Function

The `peanuts()` function provides a simple, lightweight templating system in TuskLang, perfect for quick string interpolation and basic template needs without the overhead of full template engines.

## Basic Syntax

```tusk
# Simple interpolation
template: "Hello, {name}!"
output: peanuts(template, {name: "World"})  # "Hello, World!"

# Multiple variables
greeting: peanuts("Welcome {user}, you have {count} new messages", {
    user: "John"
    count: 5
})

# With default values
result: peanuts("Hello, {name|Guest}!", {})  # "Hello, Guest!"
```

## Variable Interpolation

```tusk
# Basic variables
data: {
    title: "TuskLang"
    version: "2.0"
    author: "John Doe"
}

readme: peanuts("""
# {title} v{version}

Created by {author}
""", data)

# Nested properties
user: {
    name: "Jane"
    profile: {
        bio: "Developer"
        location: "NYC"
    }
}

bio: peanuts("{name} is a {profile.bio} from {profile.location}", user)

# Array access
items: ["first", "second", "third"]
text: peanuts("The {0} item and the {2} item", items)
```

## Filters and Modifiers

```tusk
# Built-in filters
template: """
Name: {name|upper}
Email: {email|lower}
Price: ${price|number:2}
Date: {date|date:Y-m-d}
Status: {active|yesno:Active:Inactive}
"""

output: peanuts(template, {
    name: "John Doe"
    email: "JOHN@EXAMPLE.COM"
    price: 99.999
    date: "2024-01-15"
    active: true
})

# Chain filters
text: peanuts("{message|trim|upper|truncate:20}", {
    message: "  This is a long message that needs truncation  "
})
```

## Conditional Output

```tusk
# Simple conditionals
template: "{?logged_in}Welcome back!{/logged_in}{?!logged_in}Please login{/logged_in}"

logged_out: peanuts(template, {logged_in: false})  # "Please login"
logged_in: peanuts(template, {logged_in: true})    # "Welcome back!"

# Conditional with variables
user_template: """
{?user}
    Hello, {user.name}!
    {?user.premium}You are a premium member{/user.premium}
{/user}
{?!user}
    Welcome, guest!
{/user}
"""

output: peanuts(user_template, {
    user: {
        name: "Alice"
        premium: true
    }
})
```

## Loops

```tusk
# Simple loops
template: """
<ul>
{#items}
    <li>{.}</li>
{/items}
</ul>
"""

html: peanuts(template, {
    items: ["Apple", "Banana", "Orange"]
})

# Loop with objects
user_list: """
{#users}
<div class="user">
    <h3>{name}</h3>
    <p>{email}</p>
</div>
{/users}
"""

output: peanuts(user_list, {
    users: [
        {name: "John", email: "john@example.com"}
        {name: "Jane", email: "jane@example.com"}
    ]
})

# Loop with index
indexed: """
{#items}
    {_index}. {name} - ${price}
{/items}
"""

result: peanuts(indexed, {
    items: [
        {name: "Item 1", price: 10}
        {name: "Item 2", price: 20}
    ]
})
```

## Custom Filters

```tusk
# Register custom filters
peanuts.filter("currency", (value, symbol: "$") => {
    return symbol + number_format(value, 2)
})

peanuts.filter("ago", (date) => {
    seconds: time() - strtotime(date)
    
    if (seconds < 60) return "just now"
    if (seconds < 3600) return floor(seconds / 60) + " minutes ago"
    if (seconds < 86400) return floor(seconds / 3600) + " hours ago"
    return floor(seconds / 86400) + " days ago"
})

# Use custom filters
template: """
Price: {amount|currency}
Posted: {created_at|ago}
"""

output: peanuts(template, {
    amount: 99.99
    created_at: "2024-01-15 10:00:00"
})
```

## Template Includes

```tusk
# Include other templates
main_template: """
<!DOCTYPE html>
<html>
<head>
    <title>{title}</title>
</head>
<body>
    {>header}
    
    <main>
        {content}
    </main>
    
    {>footer}
</body>
</html>
"""

# Register partials
peanuts.partial("header", '<header><h1>{site_name}</h1></header>')
peanuts.partial("footer", '<footer>&copy; {year} {site_name}</footer>')

# Render with includes
html: peanuts(main_template, {
    title: "My Page"
    site_name: "TuskLang Site"
    year: date("Y")
    content: "Page content here"
})
```

## HTML Escaping

```tusk
# Auto-escape HTML
template: """
<h1>{title}</h1>
<p>{content}</p>
<div>{raw_html|raw}</div>
"""

output: peanuts(template, {
    title: "News & Updates"  # Escaped: "News &amp; Updates"
    content: '<script>alert("XSS")</script>'  # Escaped
    raw_html: '<strong>Bold text</strong>'  # Not escaped due to |raw
}, {
    auto_escape: true  # Default
})

# Disable auto-escaping
unsafe: peanuts("{content}", {content: "<b>Bold</b>"}, {
    auto_escape: false
})
```

## Email Templates

```tusk
# Email template
email_template: """
Subject: {subject}

Dear {name},

{?order}
Thank you for your order #{order.id}.

Items:
{#order.items}
- {name}: ${price} x {quantity} = ${total}
{/order.items}

Total: ${order.total}
{/order}

{?!order}
Welcome to our service!
{/order}

Best regards,
{company}
"""

# Send email
email_content: peanuts(email_template, {
    subject: "Order Confirmation"
    name: "John Doe"
    order: {
        id: "12345"
        items: [
            {name: "Widget", price: 10, quantity: 2, total: 20}
            {name: "Gadget", price: 15, quantity: 1, total: 15}
        ]
        total: 35
    }
    company: "TuskLang Store"
})

send_email(to: "john@example.com", body: email_content)
```

## Configuration Templates

```tusk
# Generate config files
nginx_template: """
server {
    listen {port|80};
    server_name {domains|_};
    root {root|/var/www/html};
    
    {?ssl}
    listen 443 ssl;
    ssl_certificate {ssl.cert};
    ssl_certificate_key {ssl.key};
    {/ssl}
    
    {#locations}
    location {path} {
        {?proxy}proxy_pass {proxy};{/proxy}
        {?root}root {root};{/root}
    }
    {/locations}
}
"""

config: peanuts(nginx_template, {
    port: 8080
    domains: "example.com www.example.com"
    root: "/var/www/myapp"
    ssl: {
        cert: "/etc/ssl/cert.pem"
        key: "/etc/ssl/key.pem"
    }
    locations: [
        {path: "/api", proxy: "http://localhost:3000"}
        {path: "/static", root: "/var/www/static"}
    ]
})
```

## SQL Query Templates

```tusk
# Safe SQL templates (still use parameterized queries!)
query_template: """
SELECT {fields|*}
FROM {table}
{?where}WHERE {where}{/where}
{?order}ORDER BY {order}{/order}
{?limit}LIMIT {limit}{/limit}
"""

query: peanuts(query_template, {
    fields: "id, name, email"
    table: "users"
    where: "active = 1"
    order: "created_at DESC"
    limit: 10
})

# Execute with parameters
results: db.query(query)
```

## Markdown Templates

```tusk
# Generate markdown
changelog_template: """
# Changelog

## [{version}] - {date}

### {type}
{#changes}
- {description} {?issue}(#{issue}){/issue}
{/changes}

{?breaking}
### Breaking Changes
{#breaking}
- {.}
{/breaking}
{/breaking}
"""

changelog: peanuts(changelog_template, {
    version: "2.0.0"
    date: date("Y-m-d")
    type: "Added"
    changes: [
        {description: "New feature X", issue: 123}
        {description: "Support for Y"}
    ]
    breaking: [
        "Removed deprecated method Z"
    ]
})
```

## Caching Templates

```tusk
# Cache compiled templates
cached_peanuts: (template, data, cache_key: null) => {
    if (cache_key) {
        cached: cache.get("peanuts:" + cache_key)
        if (cached) return cached
    }
    
    # Compile template once
    compiled: peanuts.compile(template)
    
    # Render with data
    result: compiled(data)
    
    if (cache_key) {
        cache.set("peanuts:" + cache_key, result, 3600)
    }
    
    return result
}

# Pre-compile templates
templates: {
    email: peanuts.compile(file.read("templates/email.peanuts"))
    invoice: peanuts.compile(file.read("templates/invoice.peanuts"))
}

# Use pre-compiled
email_html: templates.email({user: user, message: message})
```

## Error Handling

```tusk
# Safe rendering
safe_peanuts: (template, data, default: "") => {
    try {
        return peanuts(template, data)
    } catch (e) {
        log.error("Template error", {
            error: e.message
            template: template.substring(0, 100)
        })
        return default
    }
}

# Debug mode
debug_output: peanuts(template, data, {
    debug: true  # Shows undefined variables
    strict: false  # Don't throw on missing vars
})
```

## Best Practices

1. **Keep templates simple** - Use full template engine for complex needs
2. **Escape by default** - Prevent XSS vulnerabilities
3. **Cache compiled templates** - Improve performance
4. **Use meaningful names** - Make templates self-documenting
5. **Validate data** - Don't assume variables exist
6. **Separate logic from templates** - Keep templates clean
7. **Use filters for formatting** - Reusable transformations
8. **Test with edge cases** - Empty arrays, null values

## Related Functions

- `render()` - Full template rendering
- `sprintf()` - Simple string formatting
- `str_replace()` - String replacement
- `view()` - View rendering
- `blade()` - Blade template engine