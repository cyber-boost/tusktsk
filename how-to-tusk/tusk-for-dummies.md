# TuskLang for Dummies - SIMPLIFIED

## The Core Idea

TuskLang is **ANTI-COMPLEXITY**. We hate:
- JSON's `{}` everywhere
- YAML's whitespace hell  
- ENV files scattered everywhere

## Basic Syntax - IT'S JUST KEY: VALUE!

```tsk
# Static values
name: "MyApp"
port: 8080
debug: true

# Dynamic values with @
database_host: @env("DB_HOST", "localhost")
current_time: @php(time())
user_count: @query("SELECT COUNT(*) FROM users")
```

That's it. **NO CURLY BRACES** for basic config.

## Multiple Ways to Group (YOUR CHOICE!)

```tsk
# Top level - ALWAYS FLAT
app_name: "TuskPHP"
version: "1.0.0"

# Method 1: Square brackets (TOML-style)
[database]
host: @env("DB_HOST", "localhost")
port: 5432
name: "myapp"

# Method 2: Angle brackets (NEW!)
database>
    host: @env("DB_HOST", "localhost")
    port: 5432
    name: "myapp"
<

# Method 3: Curly braces (if you must)
database {
    host: @env("DB_HOST", "localhost")
    port: 5432
    name: "myapp"
}
```

## The @ Symbol = DYNAMIC POWER

The `@` makes things **dynamic** instead of static:

```tsk
# Static (boring)
timeout: 30

# Dynamic (powerful!)
timeout: @env("TIMEOUT", 30)
timeout: @query("SELECT timeout FROM config LIMIT 1")
timeout: @optimize("timeout", 30)
timeout: @cache("5m", expensive_calculation)
```

## Core @ Functions (Available Now in PHP)

1. **@env(var, default)** - Environment variables
   ```tsk
   api_key: @env("API_KEY", "dev-key-123")
   ```

2. **@query(sql)** - Database queries (THE KILLER FEATURE!)
   ```tsk
   user_limit: @query("SELECT max_users FROM plans WHERE id = 1")
   ```

3. **@php(code)** - PHP expressions
   ```tsk
   year: @php(date('Y'))
   ```

4. **@file(path)** - Include files
   ```tsk
   secrets: @file("/etc/myapp/secrets.tsk")
   ```

## FUJSEN @ Operators (Advanced)

These are the **intelligent** operators:

```tsk
# Cache expensive operations
product_count: @cache("1h", @query("SELECT COUNT(*) FROM products"))

# Learn optimal values
worker_count: @learn("optimal_workers", 4)

# Track metrics
response_time: @metrics("api_response_ms", 0)

# Auto-optimize
batch_size: @optimize("batch_size", 100)
```

## Real-World Example

```tsk
# config.tsk - SIMPLE AND POWERFUL
app_name: "MyStore"
environment: @env("APP_ENV", "development")

# Dynamic limits based on plan
rate_limit: @query("SELECT rate_limit FROM plans WHERE user_id = ?", @user.id)

# Smart caching
cache_ttl: @learn("optimal_cache_time", "5m")

# Only group when necessary
database {
    host: @env("DB_HOST", "localhost")
    connections: @optimize("db_connections", 10)
}
```

## What Makes TuskLang Different?

1. **Flat by default** - No unnecessary nesting
2. **@ = Dynamic** - Easy to spot what's dynamic
3. **Database queries in config** - No other config language does this!
4. **Self-optimizing** - @learn and @optimize adapt over time

## Variables and Cross-File Communication

```tsk
# Variables with $
$global_var: "I'm available everywhere"
port: $global_var

# Section-specific variables
[api]
var: "I'm only in [api] section"
endpoint: var  # no $ means local to section

# Cross-file references
api_key: @api.tsk.get('secret_key')
db_config: @database.tsk.get('connection')
shared_port: @config.tsk.get('port')

# Set values in other files
@config.tsk.set('port', 8080)

# Ranges and optional semicolons
port-range: 8888-9999
port-range: 8888-9999;  # semicolon optional

# Date formatting
year: @date('Y')
timestamp: @date('Y-m-d H:i:s')
```

## The Philosophy

> "If JSON and YAML had a baby, and that baby went to MIT" - That's TuskLang

- **Static values**: Just write them
- **Dynamic values**: Add @
- **Groups**: Use [], >, or {} - YOUR CHOICE!
- **Variables**: $var (global), var (section-local)
- **Cross-file**: @file.tsk.get() and @file.tsk.set()
- **Intelligence**: Let @ operators do the heavy lifting

## Common Mistakes

❌ **WRONG** - Too much nesting:
```tsk
app {
    settings {
        database {
            primary {
                host: "localhost"
            }
        }
    }
}
```

✅ **RIGHT** - Flat when possible:
```tsk
database_host: "localhost"
# OR if you must group
database {
    host: "localhost"
}
```

❌ **WRONG** - Static when it should be dynamic:
```tsk
max_connections: 100
```

✅ **RIGHT** - Let it adapt:
```tsk
max_connections: @optimize("connections", 100)
```

## The Endgame

Imagine configs that:
- Update themselves based on database state
- Learn optimal values over time
- Pull settings from multiple sources
- Cache expensive lookups
- All in a readable, flat format

That's TuskLang. **Simple syntax, powerful results.**