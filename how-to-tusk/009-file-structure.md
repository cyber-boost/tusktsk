# TuskLang File Structure

Understanding how to organize TuskLang files is crucial for building maintainable applications. This guide covers file organization, naming conventions, and project structures.

## File Extensions

### Primary Extension: .tsk

All TuskLang files use the `.tsk` extension:

```
config.tsk          # Configuration file
server.tsk          # Server definition
routes.tsk          # Route definitions
database.tsk        # Database configuration
```

### Special Files

```
.tuskignore         # Files to ignore in TuskLang operations
tusk.config.tsk     # Project configuration
tusk.lock           # Dependency lock file
```

## Basic File Structure

### Single File Application

For simple applications, one file is sufficient:

```tusk
# app.tsk - Complete application
metadata:
    name: "Simple App"
    version: "1.0.0"

config:
    port: 8080
    debug: true

routes:
    "/": "Welcome!"
    "/api": @import("api.tsk")

# Run with: tusk serve app.tsk
```

### Multi-File Structure

Larger applications benefit from separation:

```
myapp/
├── tusk.config.tsk    # Project configuration
├── main.tsk           # Entry point
├── config/
│   ├── app.tsk        # Application config
│   ├── database.tsk   # Database config
│   └── cache.tsk      # Cache config
├── routes/
│   ├── index.tsk      # Route definitions
│   ├── api.tsk        # API routes
│   └── admin.tsk      # Admin routes
├── models/
│   ├── user.tsk       # User model
│   └── product.tsk    # Product model
├── lib/
│   ├── auth.tsk       # Authentication
│   └── utils.tsk      # Utilities
└── tests/
    ├── unit.tsk       # Unit tests
    └── integration.tsk # Integration tests
```

## Project Configuration

### tusk.config.tsk

The main project configuration file:

```tusk
# tusk.config.tsk
project:
    name: "MyTuskApp"
    version: "1.0.0"
    author: "Your Name"
    license: "MIT"

# Entry point
main: "main.tsk"

# Dependencies
dependencies:
    "tusklang/http": "^1.0.0"
    "tusklang/database": "^2.1.0"
    "community/auth": "^0.5.0"

# Scripts
scripts:
    start: "tusk serve main.tsk"
    test: "tusk test tests/"
    build: "tusk compile main.tsk -o dist/app"
    dev: "tusk serve main.tsk --watch"

# Environment-specific configs
environments:
    development:
        debug: true
        port: 3000
    
    production:
        debug: false
        port: 80
        ssl: true
```

## Import System

### Basic Imports

```tusk
# Import entire file
config: @import("config/app.tsk")

# Import specific values
database_config: @import("config/database.tsk").connection

# Import with alias
db: @import("lib/database.tsk") as database
```

### Relative vs Absolute Imports

```tusk
# Relative imports (from current file location)
local_config: @import("./config.tsk")
parent_config: @import("../shared/config.tsk")

# Absolute imports (from project root)
root_config: @import("/config/main.tsk")

# Package imports
http: @import("tusklang/http")
```

### Circular Import Prevention

TuskLang automatically detects and prevents circular imports:

```tusk
# file1.tsk
data: @import("file2.tsk").value  # OK first time

# file2.tsk
other: @import("file1.tsk").data  # Error: Circular import detected
```

## Organization Patterns

### MVC Pattern

```
mvc-app/
├── main.tsk
├── models/
│   ├── index.tsk
│   ├── user.tsk
│   └── product.tsk
├── views/
│   ├── layouts/
│   │   └── main.tsk
│   ├── users/
│   │   ├── list.tsk
│   │   └── detail.tsk
│   └── products/
│       ├── list.tsk
│       └── detail.tsk
└── controllers/
    ├── user_controller.tsk
    └── product_controller.tsk
```

### Microservices Pattern

```
microservices/
├── gateway/
│   ├── main.tsk
│   ├── routes.tsk
│   └── middleware.tsk
├── auth-service/
│   ├── main.tsk
│   ├── handlers.tsk
│   └── tokens.tsk
├── user-service/
│   ├── main.tsk
│   ├── models.tsk
│   └── handlers.tsk
└── shared/
    ├── config.tsk
    ├── database.tsk
    └── utils.tsk
```

### API-First Pattern

```
api-project/
├── main.tsk
├── api/
│   ├── v1/
│   │   ├── routes.tsk
│   │   ├── users.tsk
│   │   └── products.tsk
│   └── v2/
│       ├── routes.tsk
│       └── users.tsk
├── middleware/
│   ├── auth.tsk
│   ├── cors.tsk
│   └── ratelimit.tsk
└── schemas/
    ├── user.tsk
    └── product.tsk
```

## Naming Conventions

### File Names

```
# Use lowercase with underscores
user_controller.tsk     # Good
UserController.tsk      # Avoid
user-controller.tsk     # Avoid

# Be descriptive
authentication.tsk      # Good
auth.tsk               # OK for common abbreviations
a.tsk                  # Too vague

# Group related files
database_config.tsk
database_connection.tsk
database_migrations.tsk
```

### Directory Names

```
# Use lowercase, plural for collections
models/            # Good
Model/            # Avoid
model/            # Avoid for collections

# Be consistent
controllers/
views/
routes/
```

## Configuration Management

### Environment-Based Files

```
config/
├── base.tsk           # Shared configuration
├── development.tsk    # Development overrides
├── staging.tsk        # Staging overrides
├── production.tsk     # Production overrides
└── test.tsk          # Test configuration
```

Loading configuration:

```tusk
# main.tsk
base_config: @import("config/base.tsk")
env: @env.APP_ENV || "development"
env_config: @import("config/${env}.tsk")

# Merge configurations
config = @merge(base_config, env_config)
```

### Secrets Management

```
# secrets.tsk (DO NOT commit to version control)
secrets:
    api_key: "your-secret-key"
    database_password: "secure-password"
    jwt_secret: "jwt-secret-key"

# .tuskignore
secrets.tsk
*.key
*.pem
```

Loading secrets:

```tusk
# Load secrets safely
secrets: @import("secrets.tsk") || {
    api_key: @env.API_KEY
    database_password: @env.DB_PASSWORD
    jwt_secret: @env.JWT_SECRET
}
```

## Module System

### Creating Modules

```tusk
# lib/email.tsk - Email module
module:
    name: "email"
    version: "1.0.0"

# Private variables (not exported)
_smtp_config:
    host: @env.SMTP_HOST || "localhost"
    port: @env.SMTP_PORT || 587

# Public interface
send = @lambda(to, subject, body, {
    # Implementation
    @smtp.send(_smtp_config, {
        to: to
        subject: subject
        body: body
    })
})

# Export public API
export:
    send: @send
```

### Using Modules

```tusk
# Import and use module
email: @import("lib/email.tsk")

# Send email
@email.send("user@example.com", "Welcome!", "Thanks for signing up!")
```

## Build and Distribution

### Build Configuration

```tusk
# build.tsk
build:
    input: "main.tsk"
    output: "dist/app"
    target: @env.BUILD_TARGET || "linux-amd64"
    
    # Optimization settings
    optimize: true
    minify: true
    tree_shake: true
    
    # Include assets
    assets:
        - "public/**/*"
        - "templates/**/*.tsk"
    
    # Exclude files
    exclude:
        - "tests/**/*"
        - "*.test.tsk"
        - ".tuskignore"
```

### Package Structure

```
dist/
├── app                 # Compiled binary
├── config/
│   └── default.tsk    # Default configuration
├── assets/            # Static assets
│   ├── css/
│   ├── js/
│   └── images/
└── README.md          # Distribution readme
```

## Testing Structure

### Test Files

```
tests/
├── unit/
│   ├── models/
│   │   ├── user.test.tsk
│   │   └── product.test.tsk
│   └── utils/
│       └── helpers.test.tsk
├── integration/
│   ├── api.test.tsk
│   └── database.test.tsk
├── e2e/
│   └── flows.test.tsk
└── fixtures/
    ├── users.tsk
    └── products.tsk
```

### Test File Format

```tusk
# user.test.tsk
tests:
    "User model":
        "should create user":
            user: @import("../../models/user.tsk").create({
                name: "Test User"
                email: "test@example.com"
            })
            assert: @user.id != null
        
        "should validate email":
            result: @import("../../models/user.tsk").validateEmail("invalid")
            assert: @result == false
```

## Documentation Structure

### Inline Documentation

```tusk
# models/user.tsk

###
# User Model
# Handles user data and authentication
# @version 1.0.0
# @author Your Name
###

# Create a new user
# @param data Object containing user data
# @returns User object with generated ID
create = @lambda(data, {
    # Validate required fields
    @assert(data.email, "Email is required")
    @assert(data.name, "Name is required")
    
    # Create user
    user: @merge(data, {
        id: @uuid()
        created_at: @timestamp()
    })
    
    return: user
})
```

### Documentation Files

```
docs/
├── README.tsk         # Project documentation
├── API.tsk           # API documentation
├── SETUP.tsk         # Setup instructions
└── examples/         # Example usage
    ├── basic.tsk
    └── advanced.tsk
```

## Best Practices

### 1. Consistent Structure

- Use the same structure across projects
- Follow team conventions
- Document non-standard structures

### 2. Logical Organization

- Group related files together
- Separate concerns clearly
- Keep files focused and small

### 3. Clear Naming

- Use descriptive file names
- Avoid abbreviations unless common
- Be consistent with naming patterns

### 4. Import Management

- Use relative imports within modules
- Use absolute imports for cross-module
- Avoid deep nesting of imports

### 5. Configuration Hierarchy

- Base → Environment → Local
- Never commit secrets
- Use environment variables for deployment

## Common Patterns

### Factory Pattern

```tusk
# factories/user_factory.tsk
create = @lambda(type, data, {
    types:
        admin: @import("../models/admin.tsk")
        customer: @import("../models/customer.tsk")
        guest: @import("../models/guest.tsk")
    
    model: @types[type]
    return: @model.create(data)
})
```

### Repository Pattern

```tusk
# repositories/user_repository.tsk
_db: @import("../lib/database.tsk")

find = @lambda(id, {
    @_db.query("SELECT * FROM users WHERE id = ?", id)
})

create = @lambda(data, {
    @_db.insert("users", data)
})

update = @lambda(id, data, {
    @_db.update("users", data, "id = ?", id)
})
```

## Migration Guide

### From JSON/YAML

```
# Before (config.json)
{
  "app": {
    "name": "MyApp",
    "port": 8080
  }
}

# After (config.tsk)
app:
    name: "MyApp"
    port: 8080
```

### From Environment Files

```
# Before (.env)
APP_NAME=MyApp
APP_PORT=8080
DEBUG=true

# After (config.tsk)
app:
    name: @env.APP_NAME || "MyApp"
    port: @env.APP_PORT || 8080
    debug: @env.DEBUG == "true"
```

## Next Steps

- Learn the [CLI Overview](010-cli-overview.md) for project management
- Understand [Comments](011-comments.md) for documentation
- Master [Import System](026-references.md) for modular code