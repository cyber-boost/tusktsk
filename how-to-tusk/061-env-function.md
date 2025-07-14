# env() - Environment Variables Function

The `env()` function provides a convenient way to access environment variables with optional default values, ensuring your application can handle missing configuration gracefully.

## Basic Syntax

```tusk
# Get environment variable
database_host: env("DB_HOST")

# With default value
app_name: env("APP_NAME", "My TuskLang App")

# Type casting
debug_mode: env("DEBUG", false)
port: env("PORT", 3000)
```

## Common Usage Patterns

```tusk
# Database configuration
database: {
    driver: env("DB_DRIVER", "mysql")
    host: env("DB_HOST", "localhost")
    port: env("DB_PORT", 3306)
    database: env("DB_DATABASE", "myapp")
    username: env("DB_USERNAME", "root")
    password: env("DB_PASSWORD", "")
    charset: env("DB_CHARSET", "utf8mb4")
}

# Application settings
app: {
    name: env("APP_NAME", "TuskLang Application")
    env: env("APP_ENV", "production")
    debug: env("APP_DEBUG", false)
    url: env("APP_URL", "http://localhost")
    timezone: env("APP_TIMEZONE", "UTC")
}

# Third-party services
services: {
    mail: {
        driver: env("MAIL_DRIVER", "smtp")
        host: env("MAIL_HOST")
        port: env("MAIL_PORT", 587)
        username: env("MAIL_USERNAME")
        password: env("MAIL_PASSWORD")
        encryption: env("MAIL_ENCRYPTION", "tls")
    }
    
    stripe: {
        key: env("STRIPE_KEY")
        secret: env("STRIPE_SECRET")
        webhook_secret: env("STRIPE_WEBHOOK_SECRET")
    }
}
```

## Type Coercion

```tusk
# Boolean values
is_production: env("PRODUCTION", false)  # "true" -> true, "false" -> false
debug_enabled: env("DEBUG") == "true"    # Manual comparison

# Numeric values
port: env("PORT", 3000)                  # Automatically converted to integer
timeout: env("TIMEOUT", 30.5)            # Float values supported
max_connections: env("MAX_CONNECTIONS", 100)

# String values
app_name: env("APP_NAME", "Default")     # Strings remain strings
api_key: env("API_KEY", "")              # Empty string default

# Arrays (comma-separated)
allowed_hosts: env("ALLOWED_HOSTS", "localhost").split(",")
trusted_proxies: env("TRUSTED_PROXIES", "127.0.0.1").split(",").map(ip => ip.trim())
```

## Required Variables

```tusk
# Ensure required variables exist
require_env: (key, message: null) => {
    value: env(key)
    
    if (!value) {
        error_message: message || "Required environment variable '" + key + "' is not set"
        throw new Error(error_message)
    }
    
    return value
}

# Usage
database_url: require_env("DATABASE_URL", "Database connection URL is required")
api_secret: require_env("API_SECRET")

# Multiple required variables
required_vars: ["DATABASE_URL", "API_KEY", "SECRET_KEY"]
missing: []

foreach (required_vars as var_name) {
    if (!env(var_name)) {
        missing[] = var_name
    }
}

if (count(missing) > 0) {
    throw new Error("Missing required environment variables: " + join(", ", missing))
}
```

## Environment-Specific Logic

```tusk
# Different behavior based on environment
if (env("APP_ENV") == "production") {
    # Production settings
    cache_driver: env("CACHE_DRIVER", "redis")
    session_driver: env("SESSION_DRIVER", "redis")
    queue_driver: env("QUEUE_DRIVER", "redis")
    
    # Enable all optimizations
    config.optimize: true
    config.cache_views: true
    config.minify_assets: true
} else if (env("APP_ENV") == "development") {
    # Development settings
    cache_driver: env("CACHE_DRIVER", "file")
    session_driver: env("SESSION_DRIVER", "file")
    queue_driver: env("QUEUE_DRIVER", "sync")
    
    # Disable optimizations for debugging
    config.optimize: false
    config.cache_views: false
    config.minify_assets: false
}

# Feature flags
features: {
    new_dashboard: env("FEATURE_NEW_DASHBOARD", false)
    beta_api: env("FEATURE_BETA_API", false)
    experimental_cache: env("FEATURE_EXPERIMENTAL_CACHE", false)
}

if (features.new_dashboard) {
    include("dashboard/new.tusk")
} else {
    include("dashboard/legacy.tusk")
}
```

## Complex Configurations

```tusk
# Parse JSON from environment
redis_config: env("REDIS_CONFIG") ? 
    json_decode(env("REDIS_CONFIG")) : 
    {
        host: env("REDIS_HOST", "localhost"),
        port: env("REDIS_PORT", 6379),
        password: env("REDIS_PASSWORD")
    }

# Parse URLs
database_url: env("DATABASE_URL")
if (database_url) {
    parsed: parse_url(database_url)
    database: {
        driver: parsed.scheme
        host: parsed.host
        port: parsed.port || 3306
        database: substr(parsed.path, 1)
        username: parsed.user
        password: parsed.pass
    }
}

# Multiple environment support
get_service_url: (service) => {
    # Check environment-specific URL first
    env_key: strtoupper(env("APP_ENV")) + "_" + strtoupper(service) + "_URL"
    url: env(env_key)
    
    # Fallback to general URL
    if (!url) {
        url: env(strtoupper(service) + "_URL")
    }
    
    # Final fallback to localhost
    if (!url) {
        ports: {
            api: 8080,
            admin: 8081,
            websocket: 8082
        }
        url: "http://localhost:" + (ports[service] || 8080)
    }
    
    return url
}
```

## Security Considerations

```tusk
# Sanitize sensitive values in logs
safe_env: (key, default: null) => {
    value: env(key, default)
    sensitive_keys: ["PASSWORD", "SECRET", "KEY", "TOKEN"]
    
    # Check if key contains sensitive words
    is_sensitive: false
    foreach (sensitive_keys as sensitive) {
        if (str_contains(strtoupper(key), sensitive)) {
            is_sensitive: true
            break
        }
    }
    
    # Log access to sensitive variables
    if (is_sensitive && env("LOG_ENV_ACCESS")) {
        log.info("Accessed sensitive environment variable", {
            key: key,
            timestamp: now()
        })
    }
    
    return value
}

# Validate environment values
validated_env: (key, validator, default: null) => {
    value: env(key, default)
    
    if (value && !validator(value)) {
        throw new Error("Invalid value for environment variable '" + key + "': " + value)
    }
    
    return value
}

# Usage
email: validated_env("ADMIN_EMAIL", (v) => filter_var(v, FILTER_VALIDATE_EMAIL))
port: validated_env("PORT", (v) => v > 0 && v < 65536, 3000)
```

## Loading .env Files

```tusk
# Load .env file (usually done by framework)
load_env_file: (file: ".env") => {
    if (!file_exists(file)) {
        return false
    }
    
    lines: file(file, FILE_IGNORE_NEW_LINES | FILE_SKIP_EMPTY_LINES)
    
    foreach (lines as line) {
        # Skip comments
        if (str_starts_with(trim(line), "#")) {
            continue
        }
        
        # Parse KEY=VALUE
        parts: explode("=", line, 2)
        if (count(parts) == 2) {
            key: trim(parts[0])
            value: trim(parts[1])
            
            # Remove quotes if present
            if (
                (str_starts_with(value, '"') && str_ends_with(value, '"')) ||
                (str_starts_with(value, "'") && str_ends_with(value, "'"))
            ) {
                value: substr(value, 1, -1)
            }
            
            # Set environment variable if not already set
            if (!getenv(key)) {
                putenv(key + "=" + value)
            }
        }
    }
    
    return true
}

# Load environment-specific files
env_file: ".env." + env("APP_ENV", "local")
if (file_exists(env_file)) {
    load_env_file(env_file)
}
```

## Caching Environment Values

```tusk
# Cache parsed environment values
env_cache: {}

cached_env: (key, default: null, parser: null) => {
    # Check cache first
    if (isset(env_cache[key])) {
        return env_cache[key]
    }
    
    # Get and parse value
    value: env(key, default)
    if (parser && value !== default) {
        value: parser(value)
    }
    
    # Cache the result
    env_cache[key]: value
    
    return value
}

# Usage with expensive parsing
allowed_origins: cached_env("CORS_ALLOWED_ORIGINS", [], (value) => {
    origins: explode(",", value)
    return array_map((origin) => trim(origin), origins)
})

feature_flags: cached_env("FEATURE_FLAGS", {}, (value) => {
    return json_decode(value, true)
})
```

## Testing with Environment Variables

```tusk
# Helper for testing with different env values
with_env: (vars, callback) => {
    # Save current values
    original: {}
    foreach (vars as key => value) {
        original[key]: env(key)
        putenv(key + "=" + value)
    }
    
    try {
        # Execute callback with new env
        result: callback()
    } finally {
        # Restore original values
        foreach (original as key => value) {
            if (value === null) {
                putenv(key)  # Unset
            } else {
                putenv(key + "=" + value)
            }
        }
    }
    
    return result
}

# Test example
test_with_production_env: () => {
    return with_env({
        APP_ENV: "production",
        DEBUG: "false",
        CACHE_DRIVER: "redis"
    }, () => {
        # Test code runs with production env
        assert(env("APP_ENV") == "production")
        assert(env("DEBUG") == "false")
        return run_tests()
    })
}
```

## Best Practices

1. **Always provide defaults** - Avoid missing variable errors
2. **Use descriptive names** - Make configuration self-documenting
3. **Don't commit .env files** - Keep secrets out of version control
4. **Validate critical values** - Ensure configuration is valid
5. **Type cast appropriately** - Convert strings to expected types
6. **Cache parsed values** - Avoid repeated parsing
7. **Log sensitive access** - Monitor security-critical variables
8. **Document required variables** - In .env.example file

## Related Functions

- `getenv()` - PHP's native environment function
- `putenv()` - Set environment variables
- `$_ENV` - Environment superglobal
- `config()` - Configuration management
- `setting()` - Application settings