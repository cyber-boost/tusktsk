# TuskLang Best Practices

This guide compiles the best practices for writing clean, maintainable, and efficient TuskLang code. Following these guidelines will help you create robust applications and make your code easier to understand and maintain.

## Code Organization

### File Structure

```tusk
# Organize files logically
project/
├── config/
│   ├── app.tsk         # Application configuration
│   ├── database.tsk    # Database settings
│   └── security.tsk    # Security settings
├── src/
│   ├── models/         # Data models
│   ├── services/       # Business logic
│   └── utils/          # Helper functions
├── tests/              # Test files
└── main.tsk           # Entry point

# Each file should have a single responsibility
# Good: auth.tsk handles only authentication
# Bad: utils.tsk with mixed concerns
```

### Module Design

```tusk
# Good: Cohesive module with clear purpose
# auth.tsk
module:
    name: "Authentication"
    version: "1.0.0"
    description: "Handles user authentication and sessions"

# Public interface
export:
    login: @login_user
    logout: @logout_user  
    verify: @verify_token
    refresh: @refresh_token

# Private implementation
_hash_password = @lambda(password, { })
_generate_token = @lambda(user, { })

# Bad: Module doing too many things
# everything.tsk - Avoid this!
```

## Naming Conventions

### Variable Naming

```tusk
# Good: Descriptive and consistent
user_count: 42
is_authenticated: true
can_edit_posts: false
max_retry_attempts: 3

# Bad: Unclear or inconsistent
n: 42
flag: true
editOK: false  # Mixed naming style
MAX: 3
```

### Function Naming

```tusk
# Good: Verb-based, clear intent
get_user_by_id = @lambda(id, { })
calculate_total_price = @lambda(items, { })
is_valid_email = @lambda(email, { })
send_notification = @lambda(user, message, { })

# Bad: Unclear or noun-based
user = @lambda(id, { })      # Should be get_user
total = @lambda(items, { })   # Should be calculate_total
email = @lambda(addr, { })    # Unclear purpose
```

## Type Safety

### Use Type Annotations

```tusk
# Good: Clear types for public interfaces
create_user = @lambda(
    name: string,
    email: string,
    age: number,
    roles: string[] = ["user"]
): User, {
    # Implementation
})

# Bad: No type information
create_user = @lambda(name, email, age, roles, {
    # What types are expected?
})
```

### Validate Input Types

```tusk
# Good: Validate early
process_payment = @lambda(amount: any, currency: any, {
    # Validate inputs
    @assert(@isNumber(amount) && amount > 0, "Invalid amount")
    @assert(@includes(["USD", "EUR", "GBP"], currency), "Invalid currency")
    
    # Process payment
})

# Bad: Assume inputs are correct
process_payment = @lambda(amount, currency, {
    total = amount * exchange_rate  # May fail if amount isn't a number
})
```

## Error Handling

### Graceful Error Handling

```tusk
# Good: Handle errors explicitly
fetch_user_data = @lambda(user_id: number, {
    result = @try({
        user: @database.find_user(user_id)
        
        @if(!user, {
            @throw("User not found")
        })
        
        return: { success: true, data: user }
    }, {
        error: @catch_error
        @log.error("Failed to fetch user ${user_id}: ${error}")
        return: { success: false, error: error.message }
    })
    
    return: result
})

# Bad: Let errors propagate unchecked
fetch_user_data = @lambda(user_id, {
    return: @database.find_user(user_id)  # What if this fails?
})
```

### Use Result Types

```tusk
# Good: Explicit success/failure states
type Result<T, E> = 
    | { ok: true, value: T }
    | { ok: false, error: E }

divide = @lambda(a: number, b: number): Result<number, string>, {
    @if(b == 0, {
        return: { ok: false, error: "Division by zero" }
    })
    
    return: { ok: true, value: a / b }
})

# Usage
result = @divide(10, 2)
@if(result.ok, {
    @print("Result: ${result.value}")
}, {
    @print("Error: ${result.error}")
})
```

## Configuration Management

### Environment-Based Config

```tusk
# Good: Separate config by environment
base_config:
    app_name: "MyApp"
    version: "1.0.0"
    features:
        logging: true
        metrics: true

development_config:
    ...@base_config
    debug: true
    database_url: "postgresql://localhost/myapp_dev"
    
production_config:
    ...@base_config
    debug: false
    database_url: @env.DATABASE_URL
    ssl_required: true

# Load based on environment
config = @env.NODE_ENV == "production" 
    ? @production_config 
    : @development_config
```

### Secure Secrets

```tusk
# Good: Never hardcode secrets
api_config:
    key: @env.API_KEY || @error("API_KEY not set")
    secret: @env.API_SECRET || @error("API_SECRET not set")
    
# Bad: Hardcoded secrets
api_config:
    key: "sk_live_abcd1234"  # NEVER DO THIS
    secret: "my-secret-key"  # SECURITY RISK
```

## Performance Optimization

### Avoid Unnecessary Computation

```tusk
# Good: Cache expensive operations
_user_permissions_cache: {}

get_user_permissions = @lambda(user_id: number, {
    # Check cache first
    cached = @_user_permissions_cache[user_id]
    @if(cached, return: cached)
    
    # Compute if not cached
    permissions = @database.query(
        "SELECT * FROM permissions WHERE user_id = ?", 
        user_id
    )
    
    # Cache for future use
    _user_permissions_cache[user_id] = permissions
    return: permissions
})

# Bad: Recompute every time
get_user_permissions = @lambda(user_id, {
    return: @database.query(
        "SELECT * FROM permissions WHERE user_id = ?",
        user_id
    )
})
```

### Use Appropriate Data Structures

```tusk
# Good: Use object for key-value lookups
user_lookup: {
    "user_123": { name: "Alice", role: "admin" },
    "user_456": { name: "Bob", role: "user" }
}
user = @user_lookup[user_id]  # O(1) lookup

# Bad: Use array for lookups
users: [
    { id: "user_123", name: "Alice", role: "admin" },
    { id: "user_456", name: "Bob", role: "user" }
]
user = @find(users, @lambda(u, u.id == user_id))  # O(n) lookup
```

## Code Clarity

### Single Responsibility

```tusk
# Good: Each function does one thing
validate_email = @lambda(email: string): boolean, {
    return: @regex.test(email, "^[^@]+@[^@]+\.[^@]+$")
})

send_email = @lambda(to: string, subject: string, body: string, {
    # Just sends email
    @email_service.send({ to, subject, body })
})

# Bad: Function doing multiple things
process_user_email = @lambda(email, {
    # Validates
    @if(!@regex.test(email, "^[^@]+@[^@]+$"), {
        return: false
    })
    
    # Normalizes
    email = @lower(@trim(email))
    
    # Saves to database
    @database.save_email(email)
    
    # Sends welcome email
    @email_service.send({
        to: email,
        subject: "Welcome",
        body: "..."
    })
    
    return: true
})
```

### Clear Control Flow

```tusk
# Good: Easy to follow logic
process_order = @lambda(order: Order, {
    # Validate
    validation = @validate_order(order)
    @if(!validation.valid, {
        return: { error: validation.errors }
    })
    
    # Check inventory
    available = @check_inventory(order.items)
    @if(!available, {
        return: { error: "Insufficient inventory" }
    })
    
    # Process payment
    payment = @process_payment(order.payment)
    @if(!payment.success, {
        return: { error: payment.error }
    })
    
    # Fulfill order
    return: @fulfill_order(order)
})

# Bad: Nested and hard to follow
process_order = @lambda(order, {
    @if(@validate_order(order), {
        @if(@check_inventory(order.items), {
            payment = @process_payment(order.payment)
            @if(payment.success, {
                return: @fulfill_order(order)
            }, {
                return: { error: payment.error }
            })
        }, {
            return: { error: "Insufficient inventory" }
        })
    }, {
        return: { error: "Invalid order" }
    })
})
```

## Documentation

### Document Public APIs

```tusk
###
# Creates a new user account
# 
# @param {string} email - User's email address
# @param {string} password - User's password (will be hashed)
# @param {object} profile - Optional profile data
# @returns {Result<User, string>} Success with user or error
# @throws {ValidationError} If input validation fails
# 
# @example
# result = @create_user("john@example.com", "secure123", {
#     name: "John Doe",
#     timezone: "UTC"
# })
###
create_user = @lambda(
    email: string,
    password: string, 
    profile: object = {}
): Result<User, string>, {
    # Implementation
})
```

### Inline Comments

```tusk
# Good: Explain why, not what
# Use exponential backoff to avoid overwhelming the server
retry_delay = @min(base_delay * (2 ** attempt), max_delay)

# Bad: Explain what (obvious from code)
# Multiply base_delay by 2 to the power of attempt
retry_delay = base_delay * (2 ** attempt)
```

## Testing

### Write Testable Code

```tusk
# Good: Pure function, easy to test
calculate_discount = @lambda(
    price: number,
    discount_percent: number
): number, {
    @assert(price >= 0, "Price must be non-negative")
    @assert(discount_percent >= 0 && discount_percent <= 100, 
            "Discount must be between 0 and 100")
    
    return: price * (1 - discount_percent / 100)
})

# Bad: Side effects, hard to test
apply_discount = @lambda(order_id, discount_percent, {
    order = @database.get_order(order_id)  # External dependency
    order.total = order.total * (1 - discount_percent / 100)
    @database.save_order(order)  # Side effect
    @email.send_discount_applied(order)  # Another side effect
})
```

### Structure Tests

```tusk
# Good: Organized test structure
tests:
    "User Authentication":
        "should hash passwords": {
            hashed = @hash_password("password123")
            assert: hashed != "password123"
            assert: @verify_password("password123", hashed)
        }
        
        "should reject weak passwords": {
            result = @validate_password("123")
            assert: !result.valid
            assert: @includes(result.errors, "Too short")
        }
```

## Security

### Input Validation

```tusk
# Good: Validate and sanitize all inputs
handle_user_input = @lambda(input: string, {
    # Validate length
    @if(@len(input) > 1000, {
        @error("Input too long")
    })
    
    # Sanitize for different contexts
    html_safe = @html.escape(input)
    sql_safe = @sql.escape(input)
    
    # Use appropriately based on context
})

# Bad: Trust user input
handle_user_input = @lambda(input, {
    @database.query("SELECT * FROM users WHERE name = '${input}'")  # SQL injection risk!
})
```

### Principle of Least Privilege

```tusk
# Good: Expose only what's necessary
auth_module:
    # Private implementation details
    _secret_key: @env.JWT_SECRET
    _hash_algorithm: "SHA256"
    
    # Public interface only exposes safe operations
    export:
        login: @public_login
        verify_token: @public_verify
        # _secret_key is NOT exported
```

## Common Patterns

### Builder Pattern

```tusk
# Good: Fluent interface for complex objects
create_email_builder = @lambda({
    _email: {
        to: [],
        cc: [],
        subject: "",
        body: "",
        attachments: []
    }
    
    return: {
        to: @lambda(address, {
            @push(_email.to, address)
            return: @self
        }),
        cc: @lambda(address, {
            @push(_email.cc, address)
            return: @self
        }),
        subject: @lambda(text, {
            _email.subject = text
            return: @self
        }),
        body: @lambda(content, {
            _email.body = content
            return: @self
        }),
        attach: @lambda(file, {
            @push(_email.attachments, file)
            return: @self
        }),
        build: @lambda({ return: _email })
    }
})

# Usage
email = @create_email_builder()
    .to("user@example.com")
    .cc("manager@example.com")
    .subject("Monthly Report")
    .body("Please find attached...")
    .attach("report.pdf")
    .build()
```

### Repository Pattern

```tusk
# Good: Abstraction over data access
user_repository:
    find_by_id: @lambda(id: number, {
        @database.query_one("SELECT * FROM users WHERE id = ?", id)
    })
    
    find_by_email: @lambda(email: string, {
        @database.query_one("SELECT * FROM users WHERE email = ?", email)
    })
    
    save: @lambda(user: User, {
        @if(user.id, {
            # Update existing
            @database.execute(
                "UPDATE users SET name = ?, email = ? WHERE id = ?",
                user.name, user.email, user.id
            )
        }, {
            # Insert new
            user.id = @database.insert(
                "INSERT INTO users (name, email) VALUES (?, ?)",
                user.name, user.email
            )
        })
        return: user
    })
```

## Summary Checklist

- ✅ Use descriptive names
- ✅ Add type annotations for public APIs
- ✅ Handle errors gracefully
- ✅ Validate inputs early
- ✅ Keep functions focused (single responsibility)
- ✅ Document complex logic
- ✅ Use consistent formatting
- ✅ Avoid hardcoded secrets
- ✅ Write testable code
- ✅ Follow security best practices
- ✅ Optimize for readability over cleverness
- ✅ Use appropriate data structures
- ✅ Cache expensive operations
- ✅ Keep modules cohesive
- ✅ Review and refactor regularly

## Next Steps

- Explore [@ Operators](031-at-operator-intro.md) for advanced features
- Learn about [Performance Optimization](059-at-operator-performance.md)
- Master [Security Practices](060-at-operator-security.md)