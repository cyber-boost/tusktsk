# @env - Environment Variables

The `@env` operator provides secure access to environment variables, enabling configuration management across different deployment environments.

## Basic Syntax

```tusk
# Access environment variable
database_url: @env.DATABASE_URL

# With fallback value
app_name: @env.APP_NAME|"My App"

# Check if exists
has_api_key: @isset(@env.API_KEY)

# Type casting
debug_mode: @bool(@env.DEBUG|"false")
port: @int(@env.PORT|"3000")
```

## Configuration Management

```tusk
# Application configuration
config: {
    app: {
        name: @env.APP_NAME|"TuskApp"
        env: @env.APP_ENV|"production"
        debug: @bool(@env.APP_DEBUG|"false")
        url: @env.APP_URL|"http://localhost"
        key: @env.APP_KEY  # Required, no default
    }
    
    database: {
        driver: @env.DB_DRIVER|"mysql"
        host: @env.DB_HOST|"localhost"
        port: @int(@env.DB_PORT|"3306")
        name: @env.DB_DATABASE
        user: @env.DB_USERNAME
        pass: @env.DB_PASSWORD
    }
    
    cache: {
        driver: @env.CACHE_DRIVER|"redis"
        host: @env.REDIS_HOST|"localhost"
        port: @int(@env.REDIS_PORT|"6379")
    }
}

# Validate required variables
required_vars: ["APP_KEY", "DB_DATABASE", "DB_USERNAME", "DB_PASSWORD"]
missing: []

@foreach(@required_vars as @var) {
    @if(!@isset(@env[@var])) {
        @missing[]: @var
    }
}

@if(@count(@missing) > 0) {
    @throw("Missing required environment variables: " + @implode(", ", @missing))
}
```

## Environment-Specific Behavior

```tusk
# Development environment
@if(@env.APP_ENV == "development") {
    # Enable debugging
    @error_reporting(E_ALL)
    @ini_set("display_errors", 1)
    
    # Use local services
    @config.api_url: "http://localhost:8080"
    @config.cdn_url: "http://localhost:3000"
    
    # Disable caching
    @cache.disable()
}

# Production environment
@if(@env.APP_ENV == "production") {
    # Error handling
    @error_reporting(0)
    @ini_set("display_errors", 0)
    
    # Use production services
    @config.api_url: @env.API_URL
    @config.cdn_url: @env.CDN_URL
    
    # Enable all optimizations
    @optimize.enable_all()
}

# Staging environment
@if(@env.APP_ENV == "staging") {
    # Use production-like settings but with debugging
    @config.debug: true
    @config.api_url: @env.STAGING_API_URL
}
```

## Service Configuration

```tusk
# Database connections
databases: {
    primary: {
        dsn: @env.DATABASE_URL|@build_dsn({
            driver: @env.DB_DRIVER
            host: @env.DB_HOST
            port: @env.DB_PORT
            dbname: @env.DB_DATABASE
        })
        username: @env.DB_USERNAME
        password: @env.DB_PASSWORD
        options: {
            persistent: @bool(@env.DB_PERSISTENT|"false")
            charset: @env.DB_CHARSET|"utf8mb4"
        }
    }
    
    # Read replica
    @if(@env.DB_READ_HOST) {
        read: {
            host: @env.DB_READ_HOST
            port: @env.DB_READ_PORT|@env.DB_PORT
            username: @env.DB_READ_USERNAME|@env.DB_USERNAME
            password: @env.DB_READ_PASSWORD|@env.DB_PASSWORD
        }
    }
}

# External services
services: {
    stripe: {
        public_key: @env.STRIPE_PUBLIC_KEY
        secret_key: @env.STRIPE_SECRET_KEY
        webhook_secret: @env.STRIPE_WEBHOOK_SECRET
        test_mode: @env.STRIPE_TEST_MODE|(@env.APP_ENV != "production")
    }
    
    aws: {
        key: @env.AWS_ACCESS_KEY_ID
        secret: @env.AWS_SECRET_ACCESS_KEY
        region: @env.AWS_DEFAULT_REGION|"us-east-1"
        bucket: @env.AWS_S3_BUCKET
    }
    
    mail: {
        driver: @env.MAIL_DRIVER|"smtp"
        host: @env.MAIL_HOST
        port: @int(@env.MAIL_PORT|"587")
        username: @env.MAIL_USERNAME
        password: @env.MAIL_PASSWORD
        encryption: @env.MAIL_ENCRYPTION|"tls"
        from_address: @env.MAIL_FROM_ADDRESS
        from_name: @env.MAIL_FROM_NAME|@env.APP_NAME
    }
}
```

## Feature Flags

```tusk
# Feature flag management
features: {
    # Boolean flags
    new_ui: @bool(@env.FEATURE_NEW_UI|"false")
    beta_api: @bool(@env.FEATURE_BETA_API|"false")
    ab_testing: @bool(@env.FEATURE_AB_TESTING|"true")
    
    # Percentage rollouts
    new_checkout: @int(@env.FEATURE_NEW_CHECKOUT_PERCENT|"0")
    
    # User lists
    beta_users: @explode(",", @env.FEATURE_BETA_USERS|"")
}

# Feature check helper
has_feature: (feature, user_id: null) => {
    # Check if feature exists
    @if(!@isset(@features[@feature])) {
        return false
    }
    
    value: @features[@feature]
    
    # Boolean feature
    @if(@is_bool(@value)) {
        return @value
    }
    
    # Percentage rollout
    @if(@is_numeric(@value) && @user_id) {
        user_hash: @crc32(@user_id + @feature)
        return (@user_hash % 100) < @value
    }
    
    # User list
    @if(@is_array(@value) && @user_id) {
        return @in_array(@user_id, @value)
    }
    
    return false
}
```

## Secrets Management

```tusk
# Encrypted secrets
secrets: {
    # Decrypt secrets from environment
    api_key: @decrypt(@env.ENCRYPTED_API_KEY, @env.MASTER_KEY)
    db_password: @decrypt(@env.ENCRYPTED_DB_PASSWORD, @env.MASTER_KEY)
    
    # Rotate secrets periodically
    should_rotate: @days_since(@env.LAST_SECRET_ROTATION) > 90
}

# Secret validation
validate_secrets: {
    # Check format
    @if(!@preg_match("/^[A-Za-z0-9+\/=]+$/", @env.ENCRYPTED_API_KEY)) {
        @throw("Invalid encrypted secret format")
    }
    
    # Check master key
    @if(@strlen(@env.MASTER_KEY) < 32) {
        @throw("Master key too short")
    }
}

# Secure loading
load_secrets: () => {
    # Only load in memory, never log
    @foreach(@secrets as @name => @value) {
        @putenv(@name + "=" + @value)
    }
}
```

## Dynamic Environment

```tusk
# Load environment based on host
dynamic_env: {
    host: @http.host.full_domain
    
    env_map: {
        "example.com": "production"
        "staging.example.com": "staging"
        "dev.example.com": "development"
        "*.test": "testing"
    }
    
    @foreach(@env_map as @pattern => @env) {
        @if(@matches_pattern(@host, @pattern)) {
            @env.APP_ENV: @env
            break
        }
    }
}

# Load .env.{environment} file
env_file: ".env." + @env.APP_ENV
@if(@file_exists(@env_file)) {
    @load_env_file(@env_file)
}
```

## Validation and Defaults

```tusk
# Environment schema
env_schema: {
    APP_ENV: {
        type: "string"
        values: ["development", "staging", "production"]
        default: "production"
    }
    APP_DEBUG: {
        type: "boolean"
        default: false
    }
    APP_PORT: {
        type: "integer"
        min: 1
        max: 65535
        default: 3000
    }
    LOG_LEVEL: {
        type: "string"
        values: ["debug", "info", "warning", "error"]
        default: "info"
    }
    SESSION_LIFETIME: {
        type: "integer"
        min: 1
        max: 525600  # 1 year in minutes
        default: 120
    }
}

# Validate environment
validate_env: () => {
    errors: []
    
    @foreach(@env_schema as @key => @schema) {
        value: @env[@key]|@schema.default
        
        # Type validation
        @switch(@schema.type) {
            case "boolean":
                @if(!@in_array(@strtolower(@value), ["true", "false", "1", "0"])) {
                    @errors[]: @key + " must be boolean"
                }
                
            case "integer":
                @if(!@is_numeric(@value)) {
                    @errors[]: @key + " must be integer"
                } else {
                    num: @int(@value)
                    @if(@isset(@schema.min) && @num < @schema.min) {
                        @errors[]: @key + " must be >= " + @schema.min
                    }
                    @if(@isset(@schema.max) && @num > @schema.max) {
                        @errors[]: @key + " must be <= " + @schema.max
                    }
                }
                
            case "string":
                @if(@isset(@schema.values) && !@in_array(@value, @schema.values)) {
                    @errors[]: @key + " must be one of: " + @implode(", ", @schema.values)
                }
        }
    }
    
    return @errors
}
```

## Environment Debugging

```tusk
# Debug environment (never in production!)
#api /debug/env {
    @if(@env.APP_ENV == "production") {
        @response.status: 403
        error: "Forbidden"
        return
    }
    
    # Sanitize sensitive values
    safe_env: {}
    sensitive_patterns: ["KEY", "SECRET", "PASSWORD", "TOKEN"]
    
    @foreach(@env as @key => @value) {
        is_sensitive: false
        @foreach(@sensitive_patterns as @pattern) {
            @if(@contains(@upper(@key), @pattern)) {
                is_sensitive: true
                break
            }
        }
        
        @if(@is_sensitive) {
            @safe_env[@key]: "***REDACTED***"
        } else {
            @safe_env[@key]: @value
        }
    }
    
    @json({
        environment: @safe_env
        php_ini: @ini_get_all()
        loaded_extensions: @get_loaded_extensions()
    })
}
```

## Environment File Loading

```tusk
# Custom .env loader
load_env_file: (file_path) => {
    @if(!@file_exists(@file_path)) {
        return false
    }
    
    lines: @file_get_lines(@file_path)
    
    @foreach(@lines as @line) {
        # Skip comments and empty lines
        line: @trim(@line)
        @if(@empty(@line) || @starts_with(@line, "#")) {
            continue
        }
        
        # Parse KEY=VALUE
        parts: @explode("=", @line, 2)
        @if(@count(@parts) == 2) {
            key: @trim(@parts[0])
            value: @trim(@parts[1])
            
            # Remove quotes
            @if(@starts_with(@value, '"') && @ends_with(@value, '"')) {
                value: @substr(@value, 1, -1)
            }
            
            # Set environment variable
            @putenv(@key + "=" + @value)
            @env[@key]: @value
        }
    }
    
    return true
}
```

## Best Practices

1. **Never commit .env files** - Use .env.example as template
2. **Validate required variables** - Fail fast if missing
3. **Use appropriate defaults** - But not for secrets
4. **Type cast values** - Environment variables are strings
5. **Separate environments** - Different configs for dev/staging/prod
6. **Encrypt sensitive values** - Don't store plaintext secrets

## Related Features

- `@config` - Configuration management
- `@secret()` - Secret decryption
- `@putenv()` - Set environment variables
- `@file_get_contents()` - Read .env files
- `@validate()` - Schema validation