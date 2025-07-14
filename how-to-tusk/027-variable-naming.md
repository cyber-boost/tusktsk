# Variable Naming in TuskLang

Choosing good variable names is crucial for writing maintainable TuskLang code. This guide covers naming conventions, rules, and best practices for variables in TuskLang.

## Naming Rules

### Valid Variable Names

```tusk
# Letters, numbers, and underscores
username: "john_doe"
user_id: 12345
firstName: "John"
_private_var: "hidden"
var123: "numeric suffix"
user2: "second user"

# Unicode letters are allowed
用户名: "张三"
prénom: "Jean"
```

### Invalid Variable Names

```tusk
# Cannot start with number
# 123var: "invalid"  # Error

# Cannot contain spaces
# user name: "invalid"  # Error

# Cannot contain special characters (except underscore)
# user-name: "invalid"  # Error
# user.name: "invalid"  # Error (dot notation)
# user@email: "invalid" # Error

# Cannot use reserved words
# true: "invalid"  # Error
# null: "invalid"  # Error
```

### Case Sensitivity

```tusk
# Variable names are case-sensitive
userName: "John"
username: "jane"  # Different variable
USERNAME: "ADMIN" # Different variable

# All three are distinct variables
```

## Naming Conventions

### Snake Case (Recommended)

```tusk
# Lowercase with underscores - Python/Ruby style
user_name: "John Doe"
first_name: "John"
last_name: "Doe"
is_active: true
created_at: @time.now()
max_retry_count: 3

# Good for most variables
database_connection: @db.connect()
api_endpoint: "/api/v1/users"
```

### Camel Case

```tusk
# First letter lowercase - JavaScript style
userName: "John Doe"
firstName: "John"
lastName: "Doe"
isActive: true
createdAt: @time.now()
maxRetryCount: 3

# Common in web development contexts
apiEndpoint: "/api/v1/users"
requestHandler: @lambda(req, res, { })
```

### Pascal Case

```tusk
# First letter uppercase - C#/Java class style
# Typically used for types or constructors
UserModel: {
    Name: "string"
    Age: "number"
    IsActive: "boolean"
}

# Type definitions
DatabaseConfig: {
    Host: "string"
    Port: "number"
}
```

### Constant Case

```tusk
# All uppercase with underscores - constant values
MAX_CONNECTIONS: 100
DEFAULT_TIMEOUT: 30000
API_VERSION: "v1"
DATABASE_URL: "postgresql://localhost/myapp"
ITEMS_PER_PAGE: 20

# Environment variable mappings
NODE_ENV: @env.NODE_ENV
API_KEY: @env.API_KEY
```

## Semantic Naming

### Descriptive Names

```tusk
# Bad - too short, unclear
d: @db.query("SELECT * FROM users")
n: 42
s: "active"

# Good - descriptive and clear
user_data: @db.query("SELECT * FROM users")
retry_count: 42
account_status: "active"

# Clear intent
is_authenticated: true
has_permission: false
can_edit: @user.role == "admin"
```

### Purpose-Driven Names

```tusk
# Name by purpose, not type
# Bad
string1: "john@example.com"
bool1: true
obj1: { }

# Good
user_email: "john@example.com"
is_email_verified: true
user_preferences: { }
```

### Context-Aware Names

```tusk
# Include context in names
# Instead of generic 'id'
user_id: 123
product_id: 456
order_id: 789

# Instead of generic 'name'
customer_name: "John Doe"
company_name: "Tech Corp"
product_name: "Widget Pro"
```

## Common Patterns

### Boolean Variables

```tusk
# Use is_, has_, can_, should_ prefixes
is_active: true
is_verified: false
is_loading: false

has_permission: true
has_children: false
has_been_processed: true

can_edit: true
can_delete: false
can_view: true

should_retry: true
should_cache: false
```

### Collection Names

```tusk
# Use plural for arrays/lists
users: ["Alice", "Bob", "Charlie"]
products: []
order_items: []

# Single item from collection
user: @users[0]
product: @find(@products, { id: 123 })

# Count/length variables
user_count: @len(@users)
total_products: @len(@products)
```

### Function/Method Names

```tusk
# Use verb or verb phrases
get_user: @lambda(id, { })
create_order: @lambda(data, { })
validate_email: @lambda(email, { })
calculate_total: @lambda(items, { })
send_notification: @lambda(user, message, { })

# Boolean returning functions
is_valid_email: @lambda(email, { })
has_sufficient_funds: @lambda(account, amount, { })
can_process_payment: @lambda(payment, { })
```

### Temporal Variables

```tusk
# Time-related variables
created_at: @time.now()
updated_at: @time.now()
deleted_at: null

start_date: "2024-01-01"
end_date: "2024-12-31"
duration_seconds: 3600

last_login: @user.last_login_timestamp
next_retry_at: @time.now() + @retry_delay
```

## Avoiding Ambiguity

### Clear Distinctions

```tusk
# Ambiguous
data: { }
info: { }
temp: 0

# Clear
user_profile_data: { }
system_info: { }
temperature_celsius: 0

# Distinguish similar concepts
raw_input: "user input"
sanitized_input: @sanitize(@raw_input)
validated_input: @validate(@sanitized_input)
```

### Units in Names

```tusk
# Include units for clarity
timeout: 30           # Unclear unit
timeout_seconds: 30   # Clear
timeout_ms: 30000    # Clear

file_size: 1024        # Unclear unit
file_size_bytes: 1024  # Clear
file_size_kb: 1       # Clear

distance: 100          # Unclear unit
distance_meters: 100   # Clear
distance_km: 0.1      # Clear
```

## Scoping and Namespacing

### Module Prefixes

```tusk
# Prefix with module/component name
auth_user: { }
auth_token: "xyz"
auth_validate: @lambda(token, { })

db_connection: { }
db_query: @lambda(sql, { })
db_transaction: @lambda(operations, { })

ui_theme: "dark"
ui_language: "en"
```

### Nested Scopes

```tusk
# Use clear scope indicators
global_config: { }

module:
    module_config: { }
    
    function: @lambda({
        local_var: "scoped"
        # Clear what scope we're referencing
        use_global: @global_config.setting
        use_module: @module_config.option
    })
```

## Special Naming Patterns

### Private Variables

```tusk
# Leading underscore for private/internal
_internal_cache: { }
_private_key: "secret"
_helper_function: @lambda({ })

public_api: {
    # Public interface
    get_data: @lambda({ })
    
    # Private implementation detail
    _fetch_from_cache: @lambda({ })
}
```

### Temporary Variables

```tusk
# Clear temporary nature
temp_result: @process_data()
tmp_file_path: "/tmp/upload_${@uuid()}"

# Or use meaningful names even for temps
intermediate_calculation: @step1()
final_result: @step2(@intermediate_calculation)
```

### Loop Variables

```tusk
# Traditional single letters for simple loops
@map(items, @lambda(i, i * 2))

# But prefer descriptive names
@map(users, @lambda(user, {
    name: user.name
    email: user.email
}))

# Index variables
@each(products, @lambda(product, index, {
    rank: index + 1
    name: product.name
}))
```

## Anti-Patterns to Avoid

### Overly Abbreviated

```tusk
# Bad - too abbreviated
usrNm: "John"
prdCt: 5
dbConn: @connect()

# Good - clear and readable
user_name: "John"
product_count: 5
database_connection: @connect()
```

### Hungarian Notation

```tusk
# Avoid type prefixes
strName: "John"      # Don't do this
intAge: 30          # Don't do this
boolActive: true    # Don't do this

# Prefer clear names without type prefixes
name: "John"
age: 30
is_active: true
```

### Generic Names

```tusk
# Avoid meaningless names
data: { }
obj: { }
val: 42
thing: "something"
stuff: []

# Use specific, meaningful names
user_profile: { }
configuration: { }
retry_count: 42
error_message: "something"
menu_items: []
```

## Refactoring Names

### Progressive Improvement

```tusk
# Initial version
x: @db.query("SELECT * FROM users WHERE active = true")

# Better
users: @db.query("SELECT * FROM users WHERE active = true")

# Even better
active_users: @db.query("SELECT * FROM users WHERE active = true")

# Best - extracted to function
get_active_users: @lambda({
    @db.query("SELECT * FROM users WHERE active = true")
})
active_users: @get_active_users()
```

## Best Practices Summary

1. **Be descriptive** - Names should convey purpose
2. **Be consistent** - Use the same convention throughout
3. **Be specific** - Avoid generic names
4. **Include context** - Add prefixes/suffixes for clarity
5. **Consider scope** - Name based on usage scope
6. **Think maintenance** - Will others understand?
7. **Avoid ambiguity** - Make distinctions clear

## Style Guide Template

```tusk
# Project naming conventions
style_guide:
    variables: "snake_case"
    constants: "UPPER_SNAKE_CASE"
    functions: "snake_case with verb"
    types: "PascalCase"
    private: "_leading_underscore"
    
    prefixes:
        booleans: ["is_", "has_", "can_", "should_"]
        getters: "get_"
        setters: "set_"
        handlers: "handle_"
        
    suffixes:
        time: ["_at", "_date", "_timestamp"]
        counts: ["_count", "_total", "_num"]
        ids: "_id"
```

## Next Steps

- Learn about [Reserved Keywords](028-reserved-keywords.md)
- Understand [Syntax Errors](029-syntax-errors.md)
- Review [Best Practices](030-best-practices.md)