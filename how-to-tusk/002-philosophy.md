# TuskLang Philosophy

TuskLang is built on the principle of "digital grace" - technology that transcends mere functionality to become an instrument of deeper purpose. This philosophy shapes every aspect of the language.

## Core Principles

### 1. Grace Over Complexity
Configuration should be a blessing, not a burden. TuskLang makes the simple things simple and the complex things possible.

```tusk
# Simple stays simple
name: "MyApp"

# Complex becomes manageable
dynamic_config = @optimize({
    cache_ttl: 3600
    strategy: "adaptive"
})
```

### 2. Human-First Design
Code is read far more often than it's written. TuskLang prioritizes human comprehension:

```tusk
# Self-documenting syntax
user_settings:
    theme: "dark"              # Visual preference
    notifications: enabled     # Push notifications
    language: "en-US"         # Locale setting
```

### 3. Forgiveness by Default
Errors should guide, not punish. TuskLang provides graceful fallbacks:

```tusk
# Fallback values prevent crashes
api_key: @env.API_KEY || "development-key"
timeout: @env.TIMEOUT || 30
```

### 4. Power Through Simplicity
Complex operations shouldn't require complex syntax:

```tusk
# One line does what would take many in other languages
user_data: @cache("user_${id}", @query("SELECT * FROM users WHERE id = ?", @request.query.id))
```

## Design Decisions

### Why Not JSON?
JSON lacks:
- Comments for documentation
- Dynamic values
- Readable multiline strings
- Type flexibility
- Runtime operations

### Why Not YAML?
YAML suffers from:
- Whitespace sensitivity
- Complex specification
- Security vulnerabilities
- Ambiguous syntax

### Why Not TOML?
TOML limitations:
- Verbose table syntax
- Limited nesting
- No dynamic operations
- Rigid structure

## The @ Operator Philosophy

The @ symbol represents "at runtime" - operations that happen when needed, not when defined:

```tusk
# Static definition
static_port: 8080

# Dynamic resolution
dynamic_port = @env.PORT || 8080

# The @ signifies "reach out and get this when needed"
current_time: @request.timestamp
user_locale: @request.headers.accept-language
```

## Configuration as Code

TuskLang treats configuration as first-class code:

```tusk
# Not just data, but logic
rate_limit:
    enabled: @env.RATE_LIMIT_ENABLED
    max_requests = @if(@env.APP_ENV == "production", 100, 1000)
    window_seconds: 60
    
    # Embedded business logic
    check = @learn({
        pattern: "rate_limit_${ip}"
        threshold: @self.max_requests
        window: @self.window_seconds
    })
```

## Principle of Least Surprise

TuskLang behaves as developers expect:

```tusk
# Strings don't need quotes (but can have them)
name: MyApplication
name: "MyApplication"  # Both work

# Numbers are numbers
port: 8080
pi: 3.14159

# Booleans are obvious
enabled: true
disabled: false

# Objects nest naturally
database:
    primary:
        host: "db1.example.com"
    replica:
        host: "db2.example.com"
```

## Progressive Enhancement

Start simple, add complexity only when needed:

```tusk
# Level 1: Static configuration
app_name: "MyApp"

# Level 2: Environment variables
app_name: @env.APP_NAME || "MyApp"

# Level 3: Conditional logic
app_name = @if(@env.DEPLOY_ENV == "staging", 
    "MyApp (Staging)", 
    @env.APP_NAME || "MyApp"
)

# Level 4: Dynamic computation
app_name = @optimize({
    base: @env.APP_NAME || "MyApp"
    suffix: @if(@env.DEPLOY_ENV != "production", " (${@env.DEPLOY_ENV})", "")
    cache: true
})
```

## Error Philosophy

Errors in TuskLang are:
1. **Descriptive**: Clear about what went wrong
2. **Helpful**: Suggest fixes
3. **Contextual**: Show where the error occurred
4. **Graceful**: Provide fallback behavior when possible

```tusk
# This error:
port: @env.PORT

# Produces: "Warning: @env.PORT is undefined. Consider adding a fallback: @env.PORT || 8080"

# Better:
port: @env.PORT || 8080
```

## Community Values

TuskLang development follows these values:

1. **Inclusivity**: Welcome all skill levels
2. **Clarity**: Prefer explicit over implicit
3. **Pragmatism**: Real-world use cases drive features
4. **Stability**: Breaking changes are rare and well-communicated
5. **Joy**: Using TuskLang should spark joy, not frustration

## The Future

TuskLang evolves based on:
- Real-world usage patterns
- Community feedback
- Performance requirements
- Security considerations

But always maintaining the core philosophy: Configuration with grace.

## Summary

TuskLang isn't just another configuration language - it's a philosophy of making software configuration a source of clarity rather than confusion, power rather than problems, and ultimately, grace rather than grief.