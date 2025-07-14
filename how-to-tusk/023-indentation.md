# Indentation in TuskLang

Proper indentation is crucial in TuskLang for creating nested structures and maintaining readable code. This guide covers indentation rules, best practices, and common patterns.

## Indentation Basics

### Spaces vs Tabs

```tusk
# TuskLang accepts both spaces and tabs, but consistency is key
# Recommended: 4 spaces per level

# Good - consistent spaces
server:
    host: "localhost"
    port: 8080
    ssl:
        enabled: true
        cert: "/path/to/cert"

# Also valid - consistent tabs
database:
	host: "localhost"
	port: 5432
	credentials:
		username: "admin"
		password: "secret"
```

### Indentation Levels

```tusk
# Each nested level should have consistent indentation
app:                      # Level 0
    name: "MyApp"        # Level 1 (4 spaces)
    version: "1.0.0"     # Level 1
    
    server:              # Level 1
        host: "0.0.0.0"  # Level 2 (8 spaces)
        port: 8080       # Level 2
        
        ssl:             # Level 2
            enabled: true     # Level 3 (12 spaces)
            cert: "cert.pem"  # Level 3
```

## Nested Objects

### Creating Hierarchy

```tusk
# Indentation creates object hierarchy
company:
    name: "Tech Corp"
    employees:
        engineering:
            frontend:
                count: 10
                lead: "Alice"
            backend:
                count: 15
                lead: "Bob"
        sales:
            count: 5
            lead: "Charlie"

# Accessing nested values
frontend_lead: company.employees.engineering.frontend.lead
```

### Mixed Content

```tusk
# Objects can contain various types
user:
    id: 123
    name: "John Doe"
    
    # Nested object
    profile:
        bio: "Software developer"
        avatar: "/images/john.jpg"
    
    # Array with proper indentation
    skills: [
        "JavaScript",
        "Python",
        "TuskLang"
    ]
    
    # Inline object (no indentation needed)
    location: { city: "NYC", country: "USA" }
```

## Arrays and Indentation

### Multiline Arrays

```tusk
# Arrays can be formatted with indentation
colors: [
    "red",
    "green",
    "blue"
]

# Array of objects with indentation
users: [
    {
        id: 1
        name: "Alice"
        role: "admin"
    },
    {
        id: 2
        name: "Bob"
        role: "user"
    }
]

# Deeply nested arrays
matrix: [
    [1, 2, 3],
    [4, 5, 6],
    [7, 8, 9]
]
```

### Complex Array Structures

```tusk
# API endpoints with proper indentation
api:
    endpoints: [
        {
            path: "/users"
            method: "GET"
            auth: true
            params: {
                page: "number"
                limit: "number"
            }
        },
        {
            path: "/users/:id"
            method: "GET"
            auth: true
            params: {
                id: "required"
            }
        }
    ]
```

## Indentation in Different Contexts

### Configuration Files

```tusk
# Well-indented configuration
config:
    development:
        database:
            host: "localhost"
            port: 5432
            name: "dev_db"
        
        cache:
            enabled: false
        
        logging:
            level: "debug"
            output: "console"
    
    production:
        database:
            host: @env.DB_HOST
            port: @env.DB_PORT
            name: @env.DB_NAME
        
        cache:
            enabled: true
            ttl: 3600
        
        logging:
            level: "error"
            output: "file"
```

### Function Definitions

```tusk
# Indentation in lambda functions
api:
    routes:
        get_user: @lambda(id, {
            # Function body is indented
            user = @db.find("users", id)
            
            @if(user == null, {
                # Nested blocks maintain indentation
                return: {
                    error: "User not found"
                    status: 404
                }
            })
            
            # Return statement
            return: {
                data: user
                status: 200
            }
        })
```

## Indentation Patterns

### Consistent Nesting

```tusk
# Maintain consistent indentation throughout
application:
    metadata:
        name: "MyApp"
        version: "1.0.0"
        author:
            name: "John Doe"
            email: "john@example.com"
    
    features:
        authentication:
            enabled: true
            providers:
                local: true
                oauth:
                    google: true
                    github: true
        
        analytics:
            enabled: false
```

### Alignment for Readability

```tusk
# Align values for better readability
database_config:
    host:       "localhost"
    port:       5432
    username:   "admin"
    password:   @env.DB_PASSWORD
    database:   "myapp"
    
    # Or without alignment (also valid)
    pool:
        min: 5
        max: 20
        idle_timeout: 30000
```

### Conditional Indentation

```tusk
# Indentation with conditional content
server:
    base_config:
        host: "0.0.0.0"
        port: 8080
    
    # Conditional nested content
    @if(@env.NODE_ENV == "production", {
        ssl:
            enabled: true
            cert: @env.SSL_CERT
            key: @env.SSL_KEY
    })
```

## Common Indentation Errors

### Inconsistent Indentation

```tusk
# Wrong - mixed indentation
server:
    host: "localhost"  # 4 spaces
  port: 8080          # 2 spaces - WRONG!
        ssl:          # 8 spaces - WRONG!
    enabled: true     # 4 spaces

# Right - consistent indentation
server:
    host: "localhost"
    port: 8080
    ssl:
        enabled: true
```

### Tab/Space Mixing

```tusk
# Wrong - mixing tabs and spaces
config:
    name: "app"      # spaces
	version: "1.0"   # tab - WRONG!
    
# Configure your editor to show whitespace characters
# to catch these issues
```

### Incorrect Hierarchy

```tusk
# Wrong - indentation doesn't match structure
app:
name: "MyApp"        # Should be indented
    version: "1.0"   # Correct
settings:            # Should not be indented
    debug: true      # Correct

# Right
app:
    name: "MyApp"
    version: "1.0"
settings:
    debug: true
```

## Editor Configuration

### VS Code Settings

```json
// .vscode/settings.json
{
    "editor.tabSize": 4,
    "editor.insertSpaces": true,
    "editor.detectIndentation": false,
    "editor.renderWhitespace": "boundary",
    "[tusklang]": {
        "editor.tabSize": 4,
        "editor.insertSpaces": true
    }
}
```

### EditorConfig

```ini
# .editorconfig
[*.tsk]
indent_style = space
indent_size = 4
end_of_line = lf
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true
```

## Indentation Best Practices

### 1. Be Consistent

```tusk
# Pick one style and stick to it throughout your project
# Good - all 4 spaces
config:
    server:
        host: "localhost"
        port: 8080
    database:
        host: "localhost"
        port: 5432
```

### 2. Use Clear Hierarchy

```tusk
# Clear parent-child relationships through indentation
organization:
    departments:
        engineering:
            teams:
                frontend:
                    members: 5
                backend:
                    members: 8
        marketing:
            teams:
                digital:
                    members: 3
```

### 3. Avoid Deep Nesting

```tusk
# Too deep - hard to read and maintain
config:
    app:
        server:
            http:
                routes:
                    api:
                        v1:
                            users:
                                endpoints:
                                    get: "/users"

# Better - flatten where possible
config:
    server_host: "localhost"
    server_port: 8080
    
api_routes:
    v1_users_get: "/api/v1/users"
    v1_users_post: "/api/v1/users"
```

### 4. Format Long Lines

```tusk
# Break long lines with proper indentation
error_message: @format(
    "Failed to process request: %s at %s with params: %s",
    error_type,
    timestamp,
    @json.stringify(params)
)

# Long array with indentation
supported_formats: [
    "application/json",
    "application/xml",
    "text/plain",
    "text/html",
    "application/x-www-form-urlencoded"
]
```

## Special Cases

### Heredoc Indentation

```tusk
# Heredoc preserves internal indentation
template: """
    <div class="container">
        <h1>${title}</h1>
        <p>${content}</p>
    </div>
"""

# Strip common leading whitespace
stripped: <<<
    This text will have
    leading whitespace removed
    but internal indentation preserved
>>>
```

### Inline Comments

```tusk
# Comments should follow indentation
server:
    # Main server configuration
    host: "localhost"
    port: 8080
    
    # SSL configuration
    ssl:
        # Enable HTTPS
        enabled: true
        # Certificate paths
        cert: "/etc/ssl/cert.pem"
        key: "/etc/ssl/key.pem"
```

## Indentation Tools

### Auto-formatting

```tusk
# Use TuskLang formatter
# Command line:
# tusk format myfile.tsk

# Before formatting
server:
  host:"localhost"
    port:8080

# After formatting
server:
    host: "localhost"
    port: 8080
```

### Validation

```tusk
# TuskLang can validate indentation
# tusk check --strict myfile.tsk

# Will report:
# - Inconsistent indentation
# - Mixed tabs/spaces
# - Incorrect nesting
```

## Performance Notes

```tusk
# Indentation has no runtime performance impact
# It's purely for organization and readability

# These are equivalent at runtime:
compact: {a: 1, b: {c: 2, d: 3}}

expanded:
    a: 1
    b:
        c: 2
        d: 3
```

## Summary

1. **Use consistent indentation** (recommend 4 spaces)
2. **Don't mix tabs and spaces**
3. **Align child elements** under their parents
4. **Keep nesting depth** reasonable
5. **Configure your editor** for TuskLang
6. **Use formatting tools** to maintain consistency
7. **Follow project conventions** if they exist

## Next Steps

- Learn about [Multiline Values](024-multiline-values.md)
- Explore [Nested Objects](019-nested-objects.md)
- Master [Best Practices](030-best-practices.md)