# Typed Values in TuskLang - Java Edition

**"We don't bow to any king" - Type Safety with Java Power**

While TuskLang is dynamically typed by default, the Java SDK provides robust type annotations and runtime type checking for enterprise-grade reliability. This guide covers TuskLang's type system through the lens of Java integration.

## 🎯 Java Type Integration

### @TuskConfig Type Annotations

```java
import org.tusklang.java.config.TuskConfig;
import org.tusklang.java.annotations.TuskType;
import org.tusklang.java.annotations.TuskRequired;
import org.tusklang.java.annotations.TuskDefault;

@TuskConfig
public class ApplicationConfig {
    
    @TuskType("string")
    @TuskRequired
    private String appName;
    
    @TuskType("number")
    @TuskDefault("8080")
    private int port;
    
    @TuskType("boolean")
    @TuskDefault("false")
    private boolean debug;
    
    @TuskType("object")
    private DatabaseConfig database;
    
    @TuskType("array")
    private List<String> allowedOrigins;
    
    // Getters and setters
    public String getAppName() { return appName; }
    public void setAppName(String appName) { this.appName = appName; }
    
    public int getPort() { return port; }
    public void setPort(int port) { this.port = port; }
    
    public boolean isDebug() { return debug; }
    public void setDebug(boolean debug) { this.debug = debug; }
    
    public DatabaseConfig getDatabase() { return database; }
    public void setDatabase(DatabaseConfig database) { this.database = database; }
    
    public List<String> getAllowedOrigins() { return allowedOrigins; }
    public void setAllowedOrigins(List<String> allowedOrigins) { this.allowedOrigins = allowedOrigins; }
}
```

### TuskLang Configuration with Type Hints

```tusk
# Java-integrated type annotations
app_name: string = "Enterprise TuskLang App"
port: number = 8080
debug: boolean = false

# Object type with nested configuration
database: object = {
    host: string = "localhost"
    port: number = 5432
    name: string = "tuskdb"
    ssl: boolean = true
}

# Array type with specific element types
allowed_origins: string[] = [
    "https://app.example.com"
    "https://api.example.com"
    "http://localhost:3000"
]

# Union types for flexible configuration
environment: "development" | "staging" | "production" = "development"
log_level: "debug" | "info" | "warn" | "error" = "info"
```

## 🏗️ Complex Type Definitions

### Nested Object Types

```java
@TuskConfig
public class DatabaseConfig {
    
    @TuskType("string")
    @TuskRequired
    private String host;
    
    @TuskType("number")
    @TuskDefault("5432")
    private int port;
    
    @TuskType("string")
    @TuskRequired
    private String name;
    
    @TuskType("string")
    private String user;
    
    @TuskType("string")
    private String password;
    
    @TuskType("boolean")
    @TuskDefault("false")
    private boolean ssl;
    
    @TuskType("object")
    private ConnectionPoolConfig pool;
    
    // Getters and setters
    public String getHost() { return host; }
    public void setHost(String host) { this.host = host; }
    
    public int getPort() { return port; }
    public void setPort(int port) { this.port = port; }
    
    public String getName() { return name; }
    public void setName(String name) { this.name = name; }
    
    public String getUser() { return user; }
    public void setUser(String user) { this.user = user; }
    
    public String getPassword() { return password; }
    public void setPassword(String password) { this.password = password; }
    
    public boolean isSsl() { return ssl; }
    public void setSsl(boolean ssl) { this.ssl = ssl; }
    
    public ConnectionPoolConfig getPool() { return pool; }
    public void setPool(ConnectionPoolConfig pool) { this.pool = pool; }
}

@TuskConfig
public class ConnectionPoolConfig {
    
    @TuskType("number")
    @TuskDefault("10")
    private int maxConnections;
    
    @TuskType("number")
    @TuskDefault("30")
    private int timeout;
    
    @TuskType("boolean")
    @TuskDefault("true")
    private boolean autoCommit;
    
    // Getters and setters
    public int getMaxConnections() { return maxConnections; }
    public void setMaxConnections(int maxConnections) { this.maxConnections = maxConnections; }
    
    public int getTimeout() { return timeout; }
    public void setTimeout(int timeout) { this.timeout = timeout; }
    
    public boolean isAutoCommit() { return autoCommit; }
    public void setAutoCommit(boolean autoCommit) { this.autoCommit = autoCommit; }
}
```

### TuskLang Configuration for Complex Types

```tusk
# Complex nested configuration with type safety
database: object = {
    host: string = "db.example.com"
    port: number = 5432
    name: string = "enterprise_app"
    user: string = @env("DB_USER", "postgres")
    password: string = @env.secure("DB_PASSWORD")
    ssl: boolean = true
    
    # Nested object configuration
    pool: object = {
        max_connections: number = 20
        timeout: number = 60
        auto_commit: boolean = true
    }
}

# Array of complex objects
servers: object[] = [
    {
        name: string = "primary"
        host: string = "server1.example.com"
        port: number = 8080
        weight: number = 1.0
    }
    {
        name: string = "secondary"
        host: string = "server2.example.com"
        port: number = 8080
        weight: number = 0.8
    }
]
```

## 🔄 Union Types and Optional Values

### Java Union Type Handling

```java
@TuskConfig
public class FlexibleConfig {
    
    @TuskType("string|number")
    private Object port;  // Can be String or Integer
    
    @TuskType("string|null")
    private String optionalApiKey;  // Can be String or null
    
    @TuskType("object|null")
    private CacheConfig cache;  // Can be CacheConfig or null
    
    @TuskType("string|number|boolean")
    private Object dynamicValue;  // Flexible type
    
    // Type-safe getters with conversion
    public int getPortAsInt() {
        if (port instanceof String) {
            return Integer.parseInt((String) port);
        }
        return (Integer) port;
    }
    
    public String getPortAsString() {
        return port.toString();
    }
    
    // Getters and setters
    public Object getPort() { return port; }
    public void setPort(Object port) { this.port = port; }
    
    public String getOptionalApiKey() { return optionalApiKey; }
    public void setOptionalApiKey(String optionalApiKey) { this.optionalApiKey = optionalApiKey; }
    
    public CacheConfig getCache() { return cache; }
    public void setCache(CacheConfig cache) { this.cache = cache; }
    
    public Object getDynamicValue() { return dynamicValue; }
    public void setDynamicValue(Object dynamicValue) { this.dynamicValue = dynamicValue; }
}
```

### TuskLang Union Type Configuration

```tusk
# Union types for flexible configuration
port: string | number = "8080"  # Can be string or number
api_key: string | null = @env("API_KEY")  # Optional string

# Dynamic configuration based on environment
cache_config: object | null = @if(@env("ENABLE_CACHE") == "true", {
    driver: string = "redis"
    ttl: number = 300
    host: string = "localhost"
}, null)

# Flexible value types
dynamic_setting: string | number | boolean = @env("DYNAMIC_SETTING", "default")
```

## 🛡️ Runtime Type Validation

### Java Type Validation

```java
import org.tusklang.java.validation.TuskValidator;
import org.tusklang.java.validation.TuskValidationException;
import org.tusklang.java.annotations.TuskValidate;

@TuskConfig
public class ValidatedConfig {
    
    @TuskType("string")
    @TuskValidate("email")
    private String email;
    
    @TuskType("number")
    @TuskValidate("range:1-65535")
    private int port;
    
    @TuskType("string")
    @TuskValidate("url")
    private String apiUrl;
    
    @TuskType("array")
    @TuskValidate("minLength:1")
    private List<String> requiredServices;
    
    // Custom validation method
    @TuskValidate("custom")
    public boolean validateConfiguration() {
        TuskValidator validator = new TuskValidator();
        
        // Email validation
        if (!validator.isValidEmail(email)) {
            throw new TuskValidationException("Invalid email format: " + email);
        }
        
        // Port range validation
        if (port < 1 || port > 65535) {
            throw new TuskValidationException("Port must be between 1 and 65535");
        }
        
        // URL validation
        if (!validator.isValidUrl(apiUrl)) {
            throw new TuskValidationException("Invalid API URL: " + apiUrl);
        }
        
        // Array validation
        if (requiredServices == null || requiredServices.isEmpty()) {
            throw new TuskValidationException("At least one required service must be specified");
        }
        
        return true;
    }
    
    // Getters and setters
    public String getEmail() { return email; }
    public void setEmail(String email) { this.email = email; }
    
    public int getPort() { return port; }
    public void setPort(int port) { this.port = port; }
    
    public String getApiUrl() { return apiUrl; }
    public void setApiUrl(String apiUrl) { this.apiUrl = apiUrl; }
    
    public List<String> getRequiredServices() { return requiredServices; }
    public void setRequiredServices(List<String> requiredServices) { this.requiredServices = requiredServices; }
}
```

### TuskLang Validation Configuration

```tusk
# Type validation in TuskLang configuration
email: string = @validate.email("admin@example.com")
port: number = @validate.range(1, 65535, 8080)
api_url: string = @validate.url("https://api.example.com")

# Array validation
required_services: string[] = @validate.minLength(1, [
    "database"
    "cache"
    "queue"
])

# Custom validation using @assert
database_config: object = @assert(@isObject(@self), {
    host: string = "localhost"
    port: number = 5432
    name: string = "app"
    
    # Validate connection string
    connection_string: string = @assert(@startsWith(@self, "postgresql://"), 
        "postgresql://${@self.host}:${@self.port}/${@self.name}")
})
```

## 🚀 Advanced Type Patterns

### Generic Type Support

```java
@TuskConfig
public class GenericConfig<T> {
    
    @TuskType("object")
    private T data;
    
    @TuskType("string")
    private String type;
    
    @TuskType("array")
    private List<T> items;
    
    // Type-safe generic methods
    public <R> R transformData(Function<T, R> transformer) {
        return transformer.apply(data);
    }
    
    public void processItems(Consumer<T> processor) {
        items.forEach(processor);
    }
    
    // Getters and setters
    public T getData() { return data; }
    public void setData(T data) { this.data = data; }
    
    public String getType() { return type; }
    public void setType(String type) { this.type = type; }
    
    public List<T> getItems() { return items; }
    public void setItems(List<T> items) { this.items = items; }
}
```

### TuskLang Generic Configuration

```tusk
# Generic configuration patterns
user_data: object = {
    id: number = 1
    name: string = "John Doe"
    email: string = "john@example.com"
    metadata: object = {
        created_at: string = @date.now()
        updated_at: string = @date.now()
    }
}

# Type-safe transformations
processed_data: object = @transform(@user_data, {
    full_name: string = @concat(@self.name, " (", @self.email, ")")
    is_active: boolean = @self.id > 0
    last_updated: string = @self.metadata.updated_at
})
```

## 🔧 Type Conversion and Coercion

### Java Type Conversion

```java
@TuskConfig
public class ConversionConfig {
    
    @TuskType("string")
    private String stringValue;
    
    @TuskType("number")
    private Number numericValue;
    
    @TuskType("boolean")
    private Boolean booleanValue;
    
    // Type conversion methods
    public int getNumericValueAsInt() {
        return numericValue.intValue();
    }
    
    public double getNumericValueAsDouble() {
        return numericValue.doubleValue();
    }
    
    public boolean getBooleanValueAsPrimitive() {
        return booleanValue != null ? booleanValue : false;
    }
    
    // Getters and setters
    public String getStringValue() { return stringValue; }
    public void setStringValue(String stringValue) { this.stringValue = stringValue; }
    
    public Number getNumericValue() { return numericValue; }
    public void setNumericValue(Number numericValue) { this.numericValue = numericValue; }
    
    public Boolean getBooleanValue() { return booleanValue; }
    public void setBooleanValue(Boolean booleanValue) { this.booleanValue = booleanValue; }
}
```

### TuskLang Type Conversion

```tusk
# Automatic type conversion
string_number: string = "42"
converted_number: number = @toNumber(@string_number)

boolean_string: string = "true"
converted_boolean: boolean = @toBoolean(@boolean_string)

# Explicit type conversion
explicit_int: number = @parseInt("123")
explicit_float: number = @parseFloat("123.45")
explicit_bool: boolean = @parseBoolean("true")
```

## 🎯 Best Practices

### Type Safety Guidelines

1. **Use @TuskType annotations** for all configuration properties
2. **Implement validation** for critical configuration values
3. **Handle null values** gracefully in Java code
4. **Use union types** for flexible configuration options
5. **Validate at runtime** for production deployments

### Performance Considerations

```java
// Efficient type checking
@TuskConfig
public class PerformanceConfig {
    
    @TuskType("object")
    private Map<String, Object> cachedData;
    
    // Use type caching for better performance
    private final Map<String, Class<?>> typeCache = new ConcurrentHashMap<>();
    
    public <T> T getTypedValue(String key, Class<T> type) {
        Object value = cachedData.get(key);
        if (value != null && type.isInstance(value)) {
            return type.cast(value);
        }
        return null;
    }
    
    // Getters and setters
    public Map<String, Object> getCachedData() { return cachedData; }
    public void setCachedData(Map<String, Object> cachedData) { this.cachedData = cachedData; }
}
```

## 🚨 Troubleshooting

### Common Type Issues

1. **Null pointer exceptions**: Always check for null values
2. **Type conversion errors**: Use explicit conversion methods
3. **Validation failures**: Implement proper error handling
4. **Union type confusion**: Use type guards and instanceof checks

### Debug Type Issues

```java
// Debug type information
public void debugTypes() {
    System.out.println("App name type: " + (appName != null ? appName.getClass() : "null"));
    System.out.println("Port type: " + (port != null ? port.getClass() : "null"));
    System.out.println("Debug type: " + (debug != null ? debug.getClass() : "null"));
}
```

## 🎯 Next Steps

1. **Explore validation patterns** in the validation documentation
2. **Learn about error handling** for type-related issues
3. **Master union types** for flexible configuration
4. **Implement custom validators** for domain-specific rules
5. **Optimize type performance** for high-throughput applications

---

**Ready to type your way to configuration greatness? TuskLang's Java integration gives you the power of static typing with the flexibility of dynamic configuration. We don't bow to any king - especially not type constraints!** 