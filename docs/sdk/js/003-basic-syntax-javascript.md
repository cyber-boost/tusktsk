# 🟨 TuskLang JavaScript Basic Syntax Guide

**"We don't bow to any king" - JavaScript Edition**

Master TuskLang syntax with JavaScript examples. Learn how to write configuration files that adapt to YOUR preferred syntax style - whether you love INI, JSON, or XML-inspired syntax.

## 🎨 Syntax Flexibility

TuskLang supports multiple syntax styles. Choose what feels natural to you:

### Style 1: Traditional INI (Sections)

```javascript
const TuskLang = require('tusklang');

const config = TuskLang.parse(`
[app]
name: "MyApp"
version: "1.0.0"
debug: true

[server]
host: "0.0.0.0"
port: 3000
workers: 4

[database]
host: "localhost"
port: 5432
name: "myapp"
user: "postgres"
password: @env("DB_PASSWORD")
`);

console.log(config.app.name);     // "MyApp"
console.log(config.server.port);  // 3000
console.log(config.database.host); // "localhost"
```

### Style 2: Curly Brace Objects

```javascript
const config = TuskLang.parse(`
app {
    name: "MyApp"
    version: "1.0.0"
    debug: true
}

server {
    host: "0.0.0.0"
    port: 3000
    workers: 4
}

database {
    host: "localhost"
    port: 5432
    name: "myapp"
    user: "postgres"
    password: @env("DB_PASSWORD")
}
`);

console.log(config.app.name);     // "MyApp"
console.log(config.server.port);  // 3000
```

### Style 3: Angle Bracket Objects

```javascript
const config = TuskLang.parse(`
app >
    name: "MyApp"
    version: "1.0.0"
    debug: true
<

server >
    host: "0.0.0.0"
    port: 3000
    workers: 4
<

database >
    host: "localhost"
    port: 5432
    name: "myapp"
    user: "postgres"
    password: @env("DB_PASSWORD")
<
`);

console.log(config.app.name);     // "MyApp"
console.log(config.server.port);  // 3000
```

## 📝 Data Types

### Strings

```javascript
const config = TuskLang.parse(`
app {
    # Single quotes
    name: 'MyApp'
    
    # Double quotes
    version: "1.0.0"
    
    # No quotes (simple strings)
    environment: production
    
    # Multiline strings
    description: """
    This is a multi-line
    string that can span
    multiple lines
    """
    
    # Escaped quotes
    message: "He said \"Hello World!\""
    
    # Template literals with variables
    log_file: "/var/log/${app_name}.log"
}
`);

console.log(config.app.name);        // "MyApp"
console.log(config.app.environment); // "production"
console.log(config.app.description); // Multi-line string
```

### Numbers

```javascript
const config = TuskLang.parse(`
server {
    # Integers
    port: 3000
    workers: 4
    
    # Floats
    timeout: 5.5
    ratio: 0.75
    
    # Scientific notation
    memory_limit: 1.5e6
    
    # Negative numbers
    retry_delay: -1000
}

database {
    # Large numbers
    max_connections: 1000000
    
    # Decimal precision
    precision: 3.14159
}
`);

console.log(typeof config.server.port);        // "number"
console.log(typeof config.server.timeout);     // "number"
console.log(config.database.precision);        // 3.14159
```

### Booleans

```javascript
const config = TuskLang.parse(`
app {
    # True values
    debug: true
    enabled: yes
    active: on
    verbose: 1
    
    # False values
    production: false
    maintenance: no
    beta: off
    silent: 0
}

features {
    # Conditional features
    cache_enabled: true
    metrics_enabled: false
    ssl_required: yes
    debug_mode: no
}
`);

console.log(config.app.debug);        // true
console.log(config.app.enabled);      // true
console.log(config.app.production);   // false
console.log(config.app.maintenance);  // false
```

### Null Values

```javascript
const config = TuskLang.parse(`
database {
    # Explicit null
    password: null
    
    # Undefined (will be null)
    optional_setting: undefined
    
    # Empty string (different from null)
    empty_value: ""
}

cache {
    # Null for optional settings
    redis_password: null
    custom_config: null
}
`);

console.log(config.database.password);      // null
console.log(config.database.optional_setting); // null
console.log(config.database.empty_value);   // ""
```

## 🔗 Variables and Interpolation

### Global Variables

```javascript
const config = TuskLang.parse(`
# Global variables (start with $)
$app_name: "MyAwesomeApp"
$version: "1.0.0"
$environment: @env("NODE_ENV", "development")

app {
    name: $app_name
    version: $version
    debug: @if($environment != "production", true, false)
}

server {
    host: "0.0.0.0"
    port: @if($environment == "production", 80, 3000)
    workers: @if($environment == "production", 4, 1)
}

paths {
    log_file: "/var/log/${app_name}.log"
    data_dir: "/var/lib/${app_name}"
    config_file: "/etc/${app_name}/config.json"
}
`);

console.log(config.app.name);        // "MyAwesomeApp"
console.log(config.paths.log_file);  // "/var/log/MyAwesomeApp.log"
```

### Variable Interpolation

```javascript
const config = TuskLang.parse(`
$base_url: "https://api.example.com"
$version: "v1"

api {
    # String interpolation
    users_endpoint: "${base_url}/${version}/users"
    posts_endpoint: "${base_url}/${version}/posts"
    
    # With @ operators
    auth_endpoint: "${base_url}/${version}/auth"
    timeout: 5000
}

# Complex interpolation
urls {
    api_base: $base_url
    api_version: $version
    full_url: "${base_url}/${version}"
    
    # Nested interpolation
    user_profile: "${base_url}/${version}/users/${user_id}"
}
`);

console.log(config.api.users_endpoint);  // "https://api.example.com/v1/users"
console.log(config.urls.full_url);       // "https://api.example.com/v1"
```

## 📚 Arrays and Objects

### Arrays

```javascript
const config = TuskLang.parse(`
app {
    # Simple arrays
    allowed_hosts: ["localhost", "127.0.0.1", "0.0.0.0"]
    ports: [3000, 3001, 3002, 3003]
    
    # Mixed type arrays
    settings: ["debug", true, 42, "production"]
    
    # Arrays with @ operators
    environments: ["development", "staging", "production"]
    current_env: @env("NODE_ENV", "development")
}

database {
    # Database connection pools
    read_replicas: [
        "db-read-1.example.com",
        "db-read-2.example.com",
        "db-read-3.example.com"
    ]
    
    # Port configurations
    replica_ports: [5432, 5433, 5434]
}

# Multi-line arrays
features {
    enabled_modules: [
        "authentication",
        "authorization", 
        "caching",
        "metrics",
        "logging"
    ]
    
    # Arrays with variables
    required_services: [
        $database_service,
        $cache_service,
        $queue_service
    ]
}
`);

console.log(config.app.allowed_hosts[0]);     // "localhost"
console.log(config.app.ports.length);         // 4
console.log(config.features.enabled_modules); // Array of 5 strings
```

### Objects

```javascript
const config = TuskLang.parse(`
app {
    # Simple objects
    metadata {
        name: "MyApp"
        version: "1.0.0"
        author: "John Doe"
    }
    
    # Nested objects
    settings {
        debug {
            enabled: true
            level: "verbose"
            output: "console"
        }
        
        production {
            enabled: false
            level: "error"
            output: "file"
        }
    }
}

database {
    # Connection configuration
    postgresql {
        host: "localhost"
        port: 5432
        database: "myapp"
        user: "postgres"
        password: @env("DB_PASSWORD")
        
        # Connection pool
        pool {
            min: 2
            max: 10
            idle_timeout: "30s"
        }
    }
    
    # Redis configuration
    redis {
        host: "localhost"
        port: 6379
        password: @env("REDIS_PASSWORD")
        
        # Redis options
        options {
            retry_delay: 1000
            max_attempts: 3
            connect_timeout: 5000
        }
    }
}

# Complex nested objects
security {
    authentication {
        jwt {
            secret: @env("JWT_SECRET")
            expires_in: "24h"
            algorithm: "HS256"
        }
        
        oauth {
            providers {
                google {
                    client_id: @env("GOOGLE_CLIENT_ID")
                    client_secret: @env("GOOGLE_CLIENT_SECRET")
                    redirect_uri: "https://myapp.com/auth/google/callback"
                }
                
                github {
                    client_id: @env("GITHUB_CLIENT_ID")
                    client_secret: @env("GITHUB_CLIENT_SECRET")
                    redirect_uri: "https://myapp.com/auth/github/callback"
                }
            }
        }
    }
}
`);

console.log(config.app.metadata.name);                    // "MyApp"
console.log(config.database.postgresql.pool.max);         // 10
console.log(config.security.authentication.jwt.algorithm); // "HS256"
```

## 🔄 Cross-File References

### File Linking

```javascript
const TuskLang = require('tusklang');

// Create enhanced instance for cross-file communication
const tsk = new TuskLang.Enhanced();

// Global configuration (peanu.tsk)
const globalConfig = TuskLang.parse(`
$app_name: "MyAwesomeApp"
$version: "1.0.0"
$environment: @env("NODE_ENV", "development")

[paths]
log_dir: "/var/log/${app_name}"
data_dir: "/var/lib/${app_name}"
config_dir: "/etc/${app_name}"

[features]
debug: @if($environment != "production", true, false)
logging: @if($environment == "production", "json", "text")
cache_ttl: @if($environment == "production", "1h", "5m")
`);

// Link global configuration
tsk.linkFile('peanu.tsk', globalConfig);

// Application configuration that references global config
const appConfig = TuskLang.parse(`
# Reference global variables
$app_name: @peanu.tsk.get("app_name")
$environment: @peanu.tsk.get("environment")

app {
    name: $app_name
    version: @peanu.tsk.get("version")
    debug: @peanu.tsk.get("features.debug")
}

server {
    host: "0.0.0.0"
    port: @if($environment == "production", 80, 3000)
    workers: @if($environment == "production", 4, 1)
}

logging {
    level: @if($environment == "production", "error", "debug")
    format: @peanu.tsk.get("features.logging")
    file: @peanu.tsk.get("paths.log_dir") + "/app.log"
}

cache {
    ttl: @peanu.tsk.get("features.cache_ttl")
    driver: "redis"
    host: "localhost"
    port: 6379
}
`);

// Parse with cross-file references
async function loadConfig() {
    try {
        const result = await tsk.parse(appConfig);
        
        console.log(`App: ${result.app.name} v${result.app.version}`);
        console.log(`Environment: ${process.env.NODE_ENV || 'development'}`);
        console.log(`Debug: ${result.app.debug}`);
        console.log(`Log file: ${result.logging.file}`);
        console.log(`Cache TTL: ${result.cache.ttl}`);
        
    } catch (error) {
        console.error('Error loading config:', error.message);
    }
}

loadConfig();
```

## 🎯 Conditional Logic

### If Statements

```javascript
const config = TuskLang.parse(`
$environment: @env("NODE_ENV", "development")

app {
    name: "MyApp"
    version: "1.0.0"
    debug: @if($environment != "production", true, false)
}

server {
    host: "0.0.0.0"
    port: @if($environment == "production", 80, 3000)
    workers: @if($environment == "production", 4, 1)
}

database {
    host: @if($environment == "production", "prod-db.example.com", "localhost")
    port: 5432
    name: "myapp"
    user: "postgres"
    password: @env("DB_PASSWORD")
    
    # Conditional connection settings
    ssl: @if($environment == "production", true, false)
    pool_size: @if($environment == "production", 20, 5)
}

logging {
    level: @if($environment == "production", "error", "debug")
    format: @if($environment == "production", "json", "text")
    file: @if($environment == "production", "/var/log/app.log", "console")
}

security {
    cors: @if($environment == "production", {
        origin: ["https://myapp.com"],
        credentials: true
    }, {
        origin: "*",
        credentials: false
    })
    
    rate_limit: @if($environment == "production", 100, 1000)
}
`);

console.log(config.app.debug);        // true/false based on NODE_ENV
console.log(config.server.port);      // 80 for production, 3000 for development
console.log(config.database.host);    // Different hosts per environment
```

### Ternary Operators

```javascript
const config = TuskLang.parse(`
$environment: @env("NODE_ENV", "development")

# Simple ternary
debug_mode: $environment == "development" ? true : false

# Complex ternary with objects
database_config: $environment == "production" ? {
    host: "prod-db.example.com",
    ssl: true,
    pool_size: 20
} : {
    host: "localhost",
    ssl: false,
    pool_size: 5
}

# Nested ternary
log_level: $environment == "production" ? "error" : 
           $environment == "staging" ? "warn" : "debug"

# Ternary with @ operators
cache_ttl: $environment == "production" ? "1h" : "5m"
api_timeout: $environment == "production" ? 10000 : 5000
`);
```

## 📝 Comments and Documentation

### Comments

```javascript
const config = TuskLang.parse(`
# This is a single-line comment
app {
    name: "MyApp"  # Inline comment
    version: "1.0.0"
    
    # Multi-line comment block
    # This section contains application settings
    # that are used throughout the system
    debug: true
}

# Database configuration
# Supports PostgreSQL, MySQL, SQLite
database {
    host: "localhost"  # Database host
    port: 5432         # Database port
    name: "myapp"      # Database name
    user: "postgres"   # Database user
    password: @env("DB_PASSWORD")  # Secure password from env
}

# API configuration
# External service endpoints
api {
    endpoint: @env("API_ENDPOINT", "https://api.example.com")
    timeout: 5000      # Request timeout in milliseconds
    retries: 3         # Number of retry attempts
}
`);
```

## 🔧 Type Safety with TypeScript

### TypeScript Integration

```typescript
import { TuskLang, Config } from 'tusklang';

// Define configuration interface
interface AppConfig {
    app: {
        name: string;
        version: string;
        debug: boolean;
    };
    server: {
        host: string;
        port: number;
        workers: number;
    };
    database: {
        host: string;
        port: number;
        name: string;
        user: string;
        password: string;
    };
    api: {
        endpoint: string;
        timeout: number;
        retries: number;
    };
}

// Parse with type safety
const config: AppConfig = TuskLang.parse<AppConfig>(`
app {
    name: "MyApp"
    version: "1.0.0"
    debug: true
}

server {
    host: "0.0.0.0"
    port: 3000
    workers: 4
}

database {
    host: "localhost"
    port: 5432
    name: "myapp"
    user: "postgres"
    password: @env("DB_PASSWORD")
}

api {
    endpoint: @env("API_ENDPOINT", "https://api.example.com")
    timeout: 5000
    retries: 3
}
`);

// Full TypeScript support
console.log(config.app.name);     // TypeScript knows this is a string
console.log(config.server.port);  // TypeScript knows this is a number
console.log(config.database.host); // TypeScript knows this is a string
```

## 🎯 Best Practices

### 1. Use Consistent Syntax Style

```javascript
// Choose one style and stick to it
const config = TuskLang.parse(`
# Use sections for simple configurations
[app]
name: "MyApp"
version: "1.0.0"

# Use objects for complex nested structures
database {
    postgresql {
        host: "localhost"
        port: 5432
    }
    
    redis {
        host: "localhost"
        port: 6379
    }
}
`);
```

### 2. Use Global Variables for Reusability

```javascript
const config = TuskLang.parse(`
# Define global variables at the top
$app_name: "MyApp"
$version: "1.0.0"
$environment: @env("NODE_ENV", "development")

# Use variables throughout the config
app {
    name: $app_name
    version: $version
    debug: @if($environment != "production", true, false)
}

paths {
    log_file: "/var/log/${app_name}.log"
    data_dir: "/var/lib/${app_name}"
}
`);
```

### 3. Use @ Operators for Dynamic Values

```javascript
const config = TuskLang.parse(`
app {
    name: "MyApp"
    version: "1.0.0"
    
    # Use @ operators for environment-specific values
    debug: @if(@env("NODE_ENV") != "production", true, false)
    timestamp: @date.now()
    random_id: @uuid.generate()
}

database {
    host: @env("DB_HOST", "localhost")
    port: @env("DB_PORT", "5432")
    password: @env.secure("DB_PASSWORD")
    
    # Use @ operators for dynamic queries
    user_count: @query("SELECT COUNT(*) FROM users")
}
`);
```

## 📚 Next Steps

1. **[Database Integration](004-database-integration-javascript.md)** - Connect to databases with @ operators
2. **[Advanced Features](005-advanced-features-javascript.md)** - Master @ operators and FUJSEN
3. **[Framework Integration](006-framework-integration-javascript.md)** - Use with Express.js, Next.js, and more
4. **[Production Deployment](007-production-deployment-javascript.md)** - Deploy with confidence

## 🎉 You've Mastered TuskLang Syntax!

You now understand:
- ✅ **Multiple syntax styles** - Choose what feels natural
- ✅ **Data types** - Strings, numbers, booleans, null, arrays, objects
- ✅ **Variables and interpolation** - Share values across your config
- ✅ **Cross-file communication** - Link multiple `.tsk` files
- ✅ **Conditional logic** - Environment-specific configurations
- ✅ **TypeScript support** - Full type safety and IntelliSense
- ✅ **Best practices** - Write maintainable configurations

**Ready to connect to databases and use @ operators? Let's dive deeper!** 