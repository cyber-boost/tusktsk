# What is TuskLang?

TuskLang is a revolutionary configuration and scripting language that unifies the simplicity of key-value pairs with the power of dynamic runtime operations. Designed as an alternative to JSON and environment files, TuskLang brings grace to configuration management.

## Core Concept

TuskLang treats configuration as code, allowing you to:
- Define static configuration with clean key-value syntax
- Execute dynamic operations using the @ operator
- Seamlessly integrate with web servers and applications
- Handle complex data structures without verbose syntax

## Why TuskLang?

### Simplicity First
```tusk
# Instead of complex JSON
name: "MyApp"
version: "1.0.0"
debug: true
```

### Dynamic When Needed
```tusk
# Runtime values with @ operator
hostname: @http.host
timestamp: @request.timestamp
user_data: @query("SELECT * FROM users WHERE id = ?", @request.query.id)
```

### Human-Readable
Unlike JSON's strict syntax or YAML's whitespace sensitivity, TuskLang prioritizes readability:
```tusk
# Comments are first-class citizens
server_config:
    host: "localhost"  # Development server
    port: 8080        # Default port
    ssl: false        # Enable in production
```

## Key Features

1. **Dual Assignment Operators**: Use `:` for static values, `=` for dynamic expressions
2. **Native @ Operations**: Built-in functions for common tasks
3. **Type Safety**: Optional type hints for better validation
4. **Heredoc Support**: Clean multiline strings
5. **Reference System**: Link values without duplication
6. **Extensible**: Custom operators and functions

## Use Cases

- **Web Applications**: Dynamic configuration based on requests
- **API Servers**: Route definitions with embedded logic
- **Microservices**: Service discovery and configuration
- **Build Tools**: Project configuration files
- **Container Orchestration**: Dynamic environment setup

## Design Philosophy

TuskLang embodies "digital grace" - making the complex simple without sacrificing power. It believes configuration should be:
- Readable by humans
- Writable without documentation
- Powerful when complexity is needed
- Forgiving of common mistakes

## Example: Complete Application Config

```tusk
# Application metadata
app:
    name: "TuskApp"
    version: "2.0.0"
    environment: @env.APP_ENV || "development"

# Database configuration
database:
    driver: "postgresql"
    host: @env.DB_HOST || "localhost"
    port: @env.DB_PORT || 5432
    name: @env.DB_NAME || "tuskapp_dev"
    
    # Dynamic connection string
    connection_string = "postgresql://${username}:${password}@${host}:${port}/${name}"

# Server configuration
server:
    host: "0.0.0.0"
    port: @env.PORT || 3000
    
    # SSL configuration based on environment
    ssl: @if(@env.APP_ENV == "production", {
        enabled: true
        cert: "/etc/ssl/certs/server.crt"
        key: "/etc/ssl/private/server.key"
    }, {
        enabled: false
    })

# Feature flags
features:
    new_ui: @cache("feature_new_ui", @query("SELECT enabled FROM features WHERE name = 'new_ui'"))
    analytics: true
    debug_mode: @env.APP_ENV != "production"
```

## Getting Started

TuskLang files use the `.tsk` extension and can be:
- Parsed as configuration files
- Executed as scripts
- Embedded in web applications
- Used as templates

Continue to the next section to understand TuskLang's philosophy and design principles.