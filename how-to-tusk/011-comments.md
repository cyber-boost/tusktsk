# Comments in TuskLang

Comments are essential for documenting your code and making it maintainable. TuskLang provides flexible commenting options that prioritize readability and clarity.

## Single-Line Comments

The most common form of commenting uses the `#` symbol:

```tusk
# This is a single-line comment
name: "TuskLang"  # This is an inline comment

# Comments can appear anywhere on their own line
port: 8080
# And they explain the code below or above
```

## Inline Comments

Comments can appear at the end of any line:

```tusk
server:
    host: "localhost"  # Development server
    port: 3000        # Default port for dev
    ssl: false        # SSL disabled in development
```

## Multi-Line Comments

For longer explanations, use multiple single-line comments:

```tusk
# This is a multi-line comment that explains
# the purpose of the following configuration.
# Each line starts with a # symbol.
# 
# You can include blank comment lines for readability.
database:
    host: "localhost"
    port: 5432
```

## Documentation Comments

TuskLang supports special documentation comments using `###`:

```tusk
###
# User Authentication Module
# 
# This module handles user authentication including:
# - Login/logout functionality  
# - Session management
# - Password hashing
# 
# @author John Doe
# @version 1.0.0
# @since 2024-01-01
###

auth:
    session_timeout: 3600  # 1 hour in seconds
    max_attempts: 5        # Maximum login attempts
```

## Function Documentation

Document functions and complex operations:

```tusk
###
# Calculate the total price including tax
# 
# @param price The base price
# @param tax_rate The tax rate as a decimal (e.g., 0.08 for 8%)
# @returns The total price including tax
###
calculate_total = @lambda(price, tax_rate, {
    # Calculate tax amount
    tax: price * tax_rate
    
    # Return total
    return: price + tax
})
```

## Section Headers

Use comments to organize your code into sections:

```tusk
# ===========================================
# Application Configuration
# ===========================================

app:
    name: "MyApp"
    version: "1.0.0"

# ===========================================
# Database Configuration  
# ===========================================

database:
    driver: "postgresql"
    host: "localhost"

# ===========================================
# Server Configuration
# ===========================================

server:
    port: 8080
    workers: 4
```

## TODO and FIXME Comments

Mark areas that need attention:

```tusk
# TODO: Implement user authentication
auth:
    enabled: false  # FIXME: Enable before production
    
# TODO: Add rate limiting
# TODO: Implement CSRF protection
# HACK: Temporary workaround for issue #123
temporary_fix: true

# NOTE: This requires PostgreSQL 14+
database_version: "14.0"

# WARNING: Do not change without updating documentation
magic_number: 42

# DEPRECATED: Use new_feature instead
old_feature: false
```

## Conditional Comments

Comment out code conditionally:

```tusk
# Development-only configuration
# Uncomment for debugging
# debug:
#     verbose: true
#     log_sql: true
#     profile: true

# Production configuration
cache:
    enabled: true
    ttl: 3600
```

## Comment Best Practices

### 1. Explain Why, Not What

```tusk
# Bad: Increment counter by 1
counter = counter + 1

# Good: Track number of retry attempts for rate limiting
counter = counter + 1
```

### 2. Keep Comments Updated

```tusk
# Configuration for AWS S3 bucket
storage:
    # Update this comment if you change providers!
    provider: "s3"
    bucket: "my-app-uploads"
```

### 3. Use Meaningful Section Dividers

```tusk
# ===== CONSTANTS =====
MAX_UPLOAD_SIZE: 10485760  # 10MB in bytes
TIMEOUT_SECONDS: 30

# ===== HELPERS =====
is_valid_email = @lambda(email, {
    # Email validation logic
})

# ===== MAIN LOGIC =====
process_request = @lambda(request, {
    # Request processing
})
```

### 4. Document Complex Logic

```tusk
# Calculate fibonacci number using memoization
# This implementation caches results to improve
# performance for recursive calls
fibonacci = @lambda(n, {
    # Base cases
    @if(n <= 1, return: n)
    
    # Check cache first
    cached: @cache.get("fib_${n}")
    @if(cached, return: cached)
    
    # Calculate and cache result
    result: @fibonacci(n - 1) + @fibonacci(n - 2)
    @cache.set("fib_${n}", result)
    
    return: result
})
```

## API Documentation

Document your APIs and interfaces:

```tusk
###
# User API Endpoints
# 
# All endpoints require authentication token in header:
# Authorization: Bearer <token>
###

api:
    routes:
        ###
        # Get user by ID
        # GET /api/users/:id
        # 
        # Response:
        # {
        #   "id": "123",
        #   "name": "John Doe",
        #   "email": "john@example.com"
        # }
        ###
        "/api/users/:id":
            method: "GET"
            handler: @handlers.get_user
```

## Configuration Documentation

Document configuration options:

```tusk
# Application Settings
# These can be overridden via environment variables
settings:
    # Application name (env: APP_NAME)
    # Used in emails and UI
    name: @env.APP_NAME || "MyApp"
    
    # Debug mode (env: DEBUG)
    # Enables verbose logging and stack traces
    # WARNING: Never enable in production!
    debug: @env.DEBUG == "true"
    
    # Session timeout in seconds (env: SESSION_TIMEOUT)
    # Default: 1 hour (3600 seconds)
    # Minimum: 300 (5 minutes)
    # Maximum: 86400 (24 hours)
    session_timeout: @env.SESSION_TIMEOUT || 3600
```

## Type Annotations in Comments

Document expected types:

```tusk
user:
    # string: User's full name
    name: "John Doe"
    
    # number: Age in years
    age: 30
    
    # boolean: Account active status
    active: true
    
    # string[]: List of user roles
    roles: ["user", "admin"]
    
    # object: Address information
    address:
        # string: Street address
        street: "123 Main St"
        # string: City name
        city: "Springfield"
        # string: 2-letter state code
        state: "IL"
```

## Comment Extraction

TuskLang tools can extract comments for documentation:

```tusk
# @doc-group: Authentication
# @doc-priority: high

###
# @api public
# @param {string} username - User's username
# @param {string} password - User's password  
# @returns {object} Authentication token and user info
# @throws {AuthError} If credentials are invalid
###
login = @lambda(username, password, {
    # Implementation
})
```

## Commenting Out Code

Temporarily disable code:

```tusk
active_features:
    search: true
    # notifications: true  # Disabled pending bug fix
    analytics: true
    # beta_features: true  # Not ready for production
```

## Language-Specific Comments

Support for internationalization:

```tusk
messages:
    # English
    welcome_en: "Welcome to TuskLang!"
    
    # Español  
    welcome_es: "¡Bienvenido a TuskLang!"
    
    # 日本語
    welcome_ja: "TuskLangへようこそ！"
    
    # Emoji comments work too! 🎉
    celebration: "🎊 Success! 🎊"
```

## Performance Notes

Document performance considerations:

```tusk
# PERFORMANCE: This operation is O(n²)
# Consider using a more efficient algorithm
# for large datasets
nested_loop_operation = @lambda(data, {
    # Implementation
})

# OPTIMIZATION: Results are cached for 5 minutes
# Cache key: "expensive_op_${param}"
expensive_operation = @cache("expensive_op_${param}", 300, {
    # Heavy computation
})
```

## Security Comments

Highlight security concerns:

```tusk
# SECURITY: Never log sensitive data
# SECURITY: Always validate user input
# SECURITY: Use prepared statements for SQL

database:
    # SECURITY: Store encrypted in environment
    password: @env.DB_PASSWORD
    
    # SECURITY: Minimum TLS 1.2 required
    ssl:
        enabled: true
        min_version: "TLSv1.2"
```

## Comment Anti-Patterns

Avoid these commenting mistakes:

```tusk
# Don't state the obvious
count: 0  # Set count to 0  (unnecessary)

# Don't leave outdated comments
# Initialize with 100 items
items: []  # Code changed but comment didn't

# Don't comment out code without explanation
# old_method: "deprecated"  # Why is this here?

# Don't use comments for version control
# Changed by John on 2024-01-01  # Use git instead
```

## Best Practices Summary

1. **Write comments for your future self** - Assume you'll forget the context
2. **Update comments when code changes** - Outdated comments are worse than none
3. **Use consistent formatting** - Pick a style and stick to it
4. **Document the why, not the what** - Code shows what, comments explain why
5. **Keep comments concise** - Get to the point quickly
6. **Use documentation comments** for public APIs
7. **Mark todos and issues** clearly
8. **Organize with section headers** for long files

## Next Steps

- Learn about [Key-Value Basics](012-key-value-basics.md)
- Understand [Assignment Operators](013-colon-vs-equals.md)
- Explore [Data Types](014-strings.md)