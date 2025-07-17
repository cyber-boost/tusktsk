# ☕ TuskLang Java Basic Syntax Guide

**"We don't bow to any king" - Java Edition**

Master TuskLang syntax in Java with comprehensive examples covering all syntax styles, type mapping, validation, and best practices. TuskLang adapts to YOUR preferred syntax style.

## 🎯 Syntax Flexibility

TuskLang supports three syntax styles - choose the one that feels natural to you:

### 1. Traditional INI-Style (Sections)

```tsk
# Traditional INI-style configuration
[app]
name: "My Java App"
version: "1.0.0"
debug: true
port: 8080

[database]
host: "localhost"
port: 5432
name: "myapp"
user: "postgres"
password: "secret"

[server]
host: "0.0.0.0"
port: 8080
ssl: false
```

### 2. JSON-Like Objects (Curly Braces)

```tsk
# JSON-like object syntax
app {
    name: "My Java App"
    version: "1.0.0"
    debug: true
    port: 8080
}

database {
    host: "localhost"
    port: 5432
    name: "myapp"
    user: "postgres"
    password: "secret"
}

server {
    host: "0.0.0.0"
    port: 8080
    ssl: false
}
```

### 3. XML-Inspired (Angle Brackets)

```tsk
# XML-inspired syntax
app >
    name: "My Java App"
    version: "1.0.0"
    debug: true
    port: 8080
<

database >
    host: "localhost"
    port: 5432
    name: "myapp"
    user: "postgres"
    password: "secret"
<

server >
    host: "0.0.0.0"
    port: 8080
    ssl: false
<
```

## 🔧 Java Integration

### Basic Parsing

```java
import org.tusklang.java.TuskLang;
import java.util.Map;

public class BasicSyntaxExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        
        // Parse any syntax style
        Map<String, Object> config = parser.parseFile("config.tsk");
        
        // Access values
        System.out.println("App name: " + config.get("app"));
        System.out.println("Database host: " + config.get("database"));
        System.out.println("Server port: " + config.get("server"));
    }
}
```

### Type-Safe Configuration Classes

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.config.TuskConfig;

@TuskConfig
public class AppConfig {
    public String appName;
    public String version;
    public boolean debug;
    public int port;
    
    public DatabaseConfig database;
    public ServerConfig server;
}

@TuskConfig
public class DatabaseConfig {
    public String host;
    public int port;
    public String name;
    public String user;
    public String password;
}

@TuskConfig
public class ServerConfig {
    public String host;
    public int port;
    public boolean ssl;
}

public class TypeSafeExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        
        // Parse into strongly-typed class
        AppConfig config = parser.parseFile("config.tsk", AppConfig.class);
        
        // Type-safe access
        System.out.println("App: " + config.appName + " v" + config.version);
        System.out.println("Database: " + config.database.host + ":" + config.database.port);
        System.out.println("Server: " + config.server.host + ":" + config.server.port);
    }
}
```

## 📊 Data Types

### Primitive Types

```tsk
# String values
app_name: "My Java App"
description: "A powerful application"

# Numeric values
port: 8080
timeout: 30.5
max_connections: 100

# Boolean values
debug: true
production: false
ssl_enabled: true

# Null values
optional_setting: null
```

### Complex Types

```tsk
# Arrays
[features]
enabled: ["user_management", "payment_processing", "analytics"]
ports: [8080, 8081, 8082]
timeouts: [30, 60, 120]

# Nested objects
[database]
connection {
    host: "localhost"
    port: 5432
    pool {
        min_size: 5
        max_size: 20
        timeout: 30
    }
}

# Mixed types
[settings]
strings: ["one", "two", "three"]
numbers: [1, 2, 3, 4, 5]
booleans: [true, false, true]
mixed: ["text", 42, true, null]
```

### Java Type Mapping

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.config.TuskConfig;
import java.util.List;
import java.util.Map;

@TuskConfig
public class ComplexConfig {
    // Primitive types
    public String appName;
    public int port;
    public double timeout;
    public boolean debug;
    
    // Arrays
    public List<String> features;
    public List<Integer> ports;
    public List<Double> timeouts;
    
    // Nested objects
    public DatabaseConfig database;
    public Map<String, Object> settings;
}

@TuskConfig
public class DatabaseConfig {
    public ConnectionConfig connection;
}

@TuskConfig
public class ConnectionConfig {
    public String host;
    public int port;
    public PoolConfig pool;
}

@TuskConfig
public class PoolConfig {
    public int minSize;
    public int maxSize;
    public int timeout;
}

public class DataTypesExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        ComplexConfig config = parser.parseFile("config.tsk", ComplexConfig.class);
        
        // Access typed data
        System.out.println("Features: " + config.features);
        System.out.println("Ports: " + config.ports);
        System.out.println("Database pool: " + config.database.connection.pool.minSize);
    }
}
```

## 🔗 Cross-File References

### Import and Reference

Create `database.tsk`:

```tsk
[connection]
host: "localhost"
port: 5432
name: "myapp"
user: "postgres"
password: "secret"

[pool]
min_size: 5
max_size: 20
timeout: 30
```

Create `app.tsk`:

```tsk
# Import database configuration
@import("database.tsk")

[app]
name: "My App"
version: "1.0.0"

# Reference imported values
[database]
connection: @ref("database.connection")
pool_settings: @ref("database.pool")
```

### Java Implementation

```java
import org.tusklang.java.TuskLang;
import java.util.Map;

public class CrossFileExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        
        // Parse main file (imports are handled automatically)
        Map<String, Object> config = parser.parseFile("app.tsk");
        
        // Access imported values
        Map<String, Object> database = (Map<String, Object>) config.get("database");
        Map<String, Object> connection = (Map<String, Object>) database.get("connection");
        
        System.out.println("Database host: " + connection.get("host"));
        System.out.println("Database port: " + connection.get("port"));
    }
}
```

## ⚡ @ Operator Integration

### Environment Variables

```tsk
# Environment variable integration
[database]
host: @env("DB_HOST", "localhost")
port: @env("DB_PORT", "5432")
password: @env.secure("DB_PASSWORD")

[api]
key: @env("API_KEY")
url: @env("API_URL", "https://api.example.com")
```

### Date and Time

```tsk
[timestamps]
created_at: @date.now()
formatted_date: @date("yyyy-MM-dd HH:mm:ss")
yesterday: @date.subtract("1d")
next_week: @date.add("7d")
```

### Database Queries

```tsk
[dynamic_data]
user_count: @query("SELECT COUNT(*) FROM users")
active_users: @query("SELECT COUNT(*) FROM users WHERE active = true")
recent_orders: @query("SELECT COUNT(*) FROM orders WHERE created_at > ?", @date.subtract("7d"))
```

### Caching

```tsk
[expensive_operations]
user_profile: @cache("5m", "get_user_profile", @request.user_id)
analytics: @cache("1h", "get_analytics_data")
```

### Java @ Operator Usage

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.adapters.PostgreSQLAdapter;
import java.util.Map;

public class OperatorExample {
    public static void main(String[] args) {
        // Setup database adapter
        PostgreSQLAdapter db = new PostgreSQLAdapter(PostgreSQLConfig.builder()
            .host("localhost")
            .port(5432)
            .database("myapp")
            .user("postgres")
            .password("secret")
            .build());
        
        TuskLang parser = new TuskLang();
        parser.setDatabaseAdapter(db);
        
        // Parse with @ operators
        Map<String, Object> config = parser.parseFile("config.tsk");
        
        System.out.println("User count: " + config.get("dynamic_data"));
        System.out.println("Cached data: " + config.get("expensive_operations"));
    }
}
```

## 🔒 Validation and Error Handling

### Schema Validation

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.validation.ValidationResult;
import org.tusklang.java.validation.Validator;

public class ValidationExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        
        // Validate configuration
        ValidationResult result = parser.validate("config.tsk");
        
        if (result.isValid()) {
            System.out.println("✅ Configuration is valid");
            Map<String, Object> config = parser.parseFile("config.tsk");
        } else {
            System.out.println("❌ Configuration errors:");
            result.getErrors().forEach(error -> 
                System.err.println("  - " + error.getMessage())
            );
        }
    }
}
```

### Custom Validation

```java
import org.tusklang.java.validation.Validator;
import org.tusklang.java.validation.ValidationRule;
import java.util.Map;

public class CustomValidationExample {
    public static void main(String[] args) {
        Validator validator = new Validator();
        
        // Add custom validation rules
        validator.addRule(new ValidationRule("port", value -> {
            int port = Integer.parseInt(value.toString());
            return port > 0 && port <= 65535;
        }, "Port must be between 1 and 65535"));
        
        validator.addRule(new ValidationRule("database.host", value -> {
            return value != null && !value.toString().isEmpty();
        }, "Database host is required"));
        
        TuskLang parser = new TuskLang();
        parser.setValidator(validator);
        
        ValidationResult result = parser.validate("config.tsk");
        if (result.isValid()) {
            System.out.println("✅ Configuration passed custom validation");
        }
    }
}
```

## 🔧 Advanced Syntax Features

### Conditional Configuration

```tsk
# Environment-based configuration
[production]
debug: false
log_level: "ERROR"
cache_ttl: "1h"

[development]
debug: true
log_level: "DEBUG"
cache_ttl: "5m"

# Conditional loading
@if(@env("APP_ENV") == "production")
    @include("production.tsk")
@else
    @include("development.tsk")
@endif
```

### Template Variables

```tsk
# Define variables
@var base_url = "https://api.example.com"
@var version = "v1"

[api]
base_url: @var("base_url")
version: @var("version")
endpoints {
    users: @var("base_url") + "/" + @var("version") + "/users"
    orders: @var("base_url") + "/" + @var("version") + "/orders"
}
```

### Java Conditional Processing

```java
import org.tusklang.java.TuskLang;
import java.util.Map;

public class ConditionalExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        
        // Set environment variable
        System.setProperty("APP_ENV", "production");
        
        // Parse with conditional logic
        Map<String, Object> config = parser.parseFile("config.tsk");
        
        // Access environment-specific configuration
        String env = System.getProperty("APP_ENV", "development");
        Map<String, Object> envConfig = (Map<String, Object>) config.get(env);
        
        System.out.println("Environment: " + env);
        System.out.println("Debug: " + envConfig.get("debug"));
        System.out.println("Log level: " + envConfig.get("log_level"));
    }
}
```

## 📝 Best Practices

### 1. Consistent Syntax Style

Choose one syntax style and stick to it throughout your project:

```tsk
# Good: Consistent INI-style
[app]
name: "My App"

[database]
host: "localhost"

# Avoid mixing styles in the same file
```

### 2. Meaningful Section Names

```tsk
# Good: Clear, descriptive names
[user_management]
[payment_processing]
[analytics_dashboard]

# Avoid: Generic names
[config]
[settings]
[data]
```

### 3. Type Safety

```java
// Good: Use @TuskConfig classes
@TuskConfig
public class AppConfig {
    public String appName;
    public int port;
}

// Avoid: Raw Map access when possible
Map<String, Object> config = parser.parseFile("config.tsk");
String appName = (String) config.get("app");
```

### 4. Error Handling

```java
// Good: Comprehensive error handling
try {
    AppConfig config = parser.parseFile("config.tsk", AppConfig.class);
    // Use configuration
} catch (TuskParseException e) {
    System.err.println("Configuration parsing failed: " + e.getMessage());
    System.exit(1);
} catch (ValidationException e) {
    System.err.println("Configuration validation failed: " + e.getMessage());
    System.exit(1);
}
```

### 5. Environment Separation

```tsk
# Good: Environment-specific files
config/
├── base.tsk
├── development.tsk
├── staging.tsk
└── production.tsk

# Use @import to compose configurations
@import("config/base.tsk")
@import("config/" + @env("APP_ENV") + ".tsk")
```

## 🎯 Performance Considerations

### 1. Lazy Loading

```tsk
# Use @lazy for expensive operations
[expensive_data]
user_profiles: @lazy("load_user_profiles")
analytics: @lazy("load_analytics_data")
```

### 2. Caching

```tsk
# Cache frequently accessed data
[cached_data]
user_count: @cache("5m", "get_user_count")
system_status: @cache("1m", "get_system_status")
```

### 3. Parallel Processing

```tsk
# Use @async for independent operations
[parallel_data]
data1: @async("operation1")
data2: @async("operation2")
data3: @async("operation3")
```

## 🔧 Troubleshooting

### Common Syntax Errors

1. **Missing Section Brackets**
```tsk
# Error: Missing closing bracket
[app
name: "My App"

# Fix: Add closing bracket
[app]
name: "My App"
```

2. **Invalid Type Conversion**
```tsk
# Error: String to int conversion
port: "8080"  # String
# Fix: Use number
port: 8080    # Integer
```

3. **Unclosed Objects**
```tsk
# Error: Unclosed object
database {
    host: "localhost"
    # Missing closing brace

# Fix: Close object
database {
    host: "localhost"
}
```

### Debug Mode

```java
import org.tusklang.java.TuskLang;

public class DebugExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        parser.setDebug(true);  // Enable debug logging
        
        Map<String, Object> config = parser.parseFile("config.tsk");
        System.out.println("Parsed configuration: " + config);
    }
}
```

## 📚 Next Steps

1. **Master @ operators** - Environment variables, caching, database queries
2. **Explore FUJSEN** - Execute JavaScript functions in configuration
3. **Integrate with Spring Boot** - Build web applications
4. **Add validation** - Ensure configuration integrity
5. **Optimize performance** - Use caching and lazy loading

---

**"We don't bow to any king"** - You now have complete mastery of TuskLang syntax in Java! Choose your preferred style, leverage type safety, and build powerful configurations that adapt to your needs. 