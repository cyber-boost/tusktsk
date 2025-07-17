# @ Variable Reference in TuskLang - Java Edition

**"We don't bow to any king" - Variable Reference Power with Java Integration**

@ variable references in TuskLang allow you to access and manipulate configuration values dynamically. The Java SDK provides robust variable reference handling with type safety and validation.

## 🎯 Java @ Variable Reference Integration

### @TuskConfig Variable Reference Support

```java
import org.tusklang.java.config.TuskConfig;
import org.tusklang.java.references.VariableReference;
import org.tusklang.java.references.ReferenceResolver;
import org.tusklang.java.annotations.TuskReference;

@TuskConfig
public class VariableReferenceConfig {
    
    // Basic variable references
    @TuskReference("@app.name")
    private String appName;
    
    @TuskReference("@database.host")
    private String databaseHost;
    
    @TuskReference("@server.port")
    private int serverPort;
    
    // Nested variable references
    @TuskReference("@config.database.connection.pool.max_size")
    private int maxPoolSize;
    
    @TuskReference("@config.security.jwt.secret")
    private String jwtSecret;
    
    // Array variable references
    @TuskReference("@servers[0].host")
    private String primaryServerHost;
    
    @TuskReference("@allowed_origins[1]")
    private String secondAllowedOrigin;
    
    // Dynamic variable references
    @TuskReference("@dynamic_config.${@environment}.host")
    private String dynamicHost;
    
    @TuskReference("@api_endpoints.${@version}.base_url")
    private String apiBaseUrl;
    
    // Computed variable references
    @TuskReference("@database.connection_string")
    private String connectionString;
    
    @TuskReference("@server.full_url")
    private String serverUrl;
    
    // Variable reference context
    private ReferenceResolver referenceResolver;
    private Map<String, Object> variableContext;
    
    public VariableReferenceConfig() {
        this.referenceResolver = new ReferenceResolver();
        this.variableContext = new ConcurrentHashMap<>();
        initializeVariableContext();
    }
    
    // Initialize variable context
    private void initializeVariableContext() {
        // Application variables
        variableContext.put("app", Map.of(
            "name", "TuskLang Enterprise",
            "version", "1.0.0",
            "environment", "production"
        ));
        
        // Database variables
        variableContext.put("database", Map.of(
            "host", "localhost",
            "port", 5432,
            "name", "tuskdb",
            "user", "postgres",
            "password", "secret"
        ));
        
        // Server variables
        variableContext.put("server", Map.of(
            "host", "0.0.0.0",
            "port", 8080,
            "ssl", true
        ));
        
        // Configuration variables
        variableContext.put("config", Map.of(
            "database", Map.of(
                "connection", Map.of(
                    "pool", Map.of(
                        "max_size", 20,
                        "min_size", 5,
                        "timeout", 30
                    )
                )
            ),
            "security", Map.of(
                "jwt", Map.of(
                    "secret", "super-secret-key",
                    "expiration", 3600
                )
            )
        ));
        
        // Server array
        variableContext.put("servers", Arrays.asList(
            Map.of("host", "server1.example.com", "port", 8080),
            Map.of("host", "server2.example.com", "port", 8080)
        ));
        
        // Allowed origins array
        variableContext.put("allowed_origins", Arrays.asList(
            "https://app.example.com",
            "https://api.example.com",
            "http://localhost:3000"
        ));
        
        // Dynamic configuration
        variableContext.put("dynamic_config", Map.of(
            "development", Map.of("host", "dev.example.com"),
            "staging", Map.of("host", "staging.example.com"),
            "production", Map.of("host", "prod.example.com")
        ));
        
        // API endpoints
        variableContext.put("api_endpoints", Map.of(
            "v1", Map.of("base_url", "https://api.example.com/v1"),
            "v2", Map.of("base_url", "https://api.example.com/v2")
        ));
        
        // Environment and version variables
        variableContext.put("environment", "production");
        variableContext.put("version", "v1");
    }
    
    // Resolve variable reference
    public Object resolveVariableReference(String reference) {
        try {
            return referenceResolver.resolve(reference, variableContext);
        } catch (Exception e) {
            throw new VariableReferenceException("Failed to resolve variable reference: " + reference, e);
        }
    }
    
    // Resolve variable reference with default value
    public Object resolveVariableReference(String reference, Object defaultValue) {
        try {
            Object value = referenceResolver.resolve(reference, variableContext);
            return value != null ? value : defaultValue;
        } catch (Exception e) {
            return defaultValue;
        }
    }
    
    // Set variable in context
    public void setVariable(String path, Object value) {
        referenceResolver.setVariable(path, value, variableContext);
    }
    
    // Get variable from context
    public Object getVariable(String path) {
        return referenceResolver.getVariable(path, variableContext);
    }
    
    // Validate variable references
    public boolean validateVariableReferences() {
        List<String> errors = new ArrayList<>();
        
        // Validate required references
        if (appName == null || appName.isEmpty()) {
            errors.add("Application name reference is required");
        }
        
        if (databaseHost == null || databaseHost.isEmpty()) {
            errors.add("Database host reference is required");
        }
        
        if (serverPort <= 0) {
            errors.add("Server port reference must be positive");
        }
        
        // Validate nested references
        if (maxPoolSize <= 0) {
            errors.add("Max pool size reference must be positive");
        }
        
        if (jwtSecret == null || jwtSecret.isEmpty()) {
            errors.add("JWT secret reference is required");
        }
        
        // Validate array references
        if (primaryServerHost == null || primaryServerHost.isEmpty()) {
            errors.add("Primary server host reference is required");
        }
        
        if (secondAllowedOrigin == null || secondAllowedOrigin.isEmpty()) {
            errors.add("Second allowed origin reference is required");
        }
        
        if (!errors.isEmpty()) {
            throw new VariableReferenceException("Variable reference validation failed: " + String.join(", ", errors));
        }
        
        return true;
    }
    
    // Compute derived variables
    public void computeDerivedVariables() {
        // Compute connection string
        String dbHost = (String) getVariable("database.host");
        int dbPort = (Integer) getVariable("database.port");
        String dbName = (String) getVariable("database.name");
        String dbUser = (String) getVariable("database.user");
        
        connectionString = String.format("postgresql://%s@%s:%d/%s", dbUser, dbHost, dbPort, dbName);
        setVariable("database.connection_string", connectionString);
        
        // Compute server URL
        String serverHost = (String) getVariable("server.host");
        int serverPort = (Integer) getVariable("server.port");
        boolean ssl = (Boolean) getVariable("server.ssl");
        
        String protocol = ssl ? "https" : "http";
        serverUrl = String.format("%s://%s:%d", protocol, serverHost, serverPort);
        setVariable("server.full_url", serverUrl);
    }
    
    // Getters and setters
    public String getAppName() { return appName; }
    public void setAppName(String appName) { this.appName = appName; }
    
    public String getDatabaseHost() { return databaseHost; }
    public void setDatabaseHost(String databaseHost) { this.databaseHost = databaseHost; }
    
    public int getServerPort() { return serverPort; }
    public void setServerPort(int serverPort) { this.serverPort = serverPort; }
    
    public int getMaxPoolSize() { return maxPoolSize; }
    public void setMaxPoolSize(int maxPoolSize) { this.maxPoolSize = maxPoolSize; }
    
    public String getJwtSecret() { return jwtSecret; }
    public void setJwtSecret(String jwtSecret) { this.jwtSecret = jwtSecret; }
    
    public String getPrimaryServerHost() { return primaryServerHost; }
    public void setPrimaryServerHost(String primaryServerHost) { this.primaryServerHost = primaryServerHost; }
    
    public String getSecondAllowedOrigin() { return secondAllowedOrigin; }
    public void setSecondAllowedOrigin(String secondAllowedOrigin) { this.secondAllowedOrigin = secondAllowedOrigin; }
    
    public String getDynamicHost() { return dynamicHost; }
    public void setDynamicHost(String dynamicHost) { this.dynamicHost = dynamicHost; }
    
    public String getApiBaseUrl() { return apiBaseUrl; }
    public void setApiBaseUrl(String apiBaseUrl) { this.apiBaseUrl = apiBaseUrl; }
    
    public String getConnectionString() { return connectionString; }
    public void setConnectionString(String connectionString) { this.connectionString = connectionString; }
    
    public String getServerUrl() { return serverUrl; }
    public void setServerUrl(String serverUrl) { this.serverUrl = serverUrl; }
}

// Variable reference resolver
public class ReferenceResolver {
    
    // Resolve variable reference
    public Object resolve(String reference, Map<String, Object> context) {
        if (!reference.startsWith("@")) {
            throw new VariableReferenceException("Variable reference must start with @");
        }
        
        String path = reference.substring(1);
        return getVariable(path, context);
    }
    
    // Get variable by path
    public Object getVariable(String path, Map<String, Object> context) {
        String[] parts = path.split("\\.");
        Object current = context;
        
        for (String part : parts) {
            if (current instanceof Map) {
                current = ((Map<String, Object>) current).get(part);
            } else if (current instanceof List) {
                // Handle array access
                if (part.matches("\\d+")) {
                    int index = Integer.parseInt(part);
                    current = ((List<?>) current).get(index);
                } else {
                    throw new VariableReferenceException("Invalid array index: " + part);
                }
            } else {
                throw new VariableReferenceException("Cannot access property " + part + " on " + current.getClass());
            }
            
            if (current == null) {
                return null;
            }
        }
        
        return current;
    }
    
    // Set variable by path
    public void setVariable(String path, Object value, Map<String, Object> context) {
        String[] parts = path.split("\\.");
        Object current = context;
        
        for (int i = 0; i < parts.length - 1; i++) {
            String part = parts[i];
            
            if (current instanceof Map) {
                Map<String, Object> map = (Map<String, Object>) current;
                if (!map.containsKey(part)) {
                    map.put(part, new HashMap<>());
                }
                current = map.get(part);
            } else {
                throw new VariableReferenceException("Cannot set property " + part + " on " + current.getClass());
            }
        }
        
        String lastPart = parts[parts.length - 1];
        if (current instanceof Map) {
            ((Map<String, Object>) current).put(lastPart, value);
        } else {
            throw new VariableReferenceException("Cannot set property " + lastPart + " on " + current.getClass());
        }
    }
}

// Variable reference exception
public class VariableReferenceException extends RuntimeException {
    
    public VariableReferenceException(String message) {
        super(message);
    }
    
    public VariableReferenceException(String message, Throwable cause) {
        super(message, cause);
    }
}
```

### TuskLang @ Variable Reference Examples

```tusk
# @ Variable reference examples in TuskLang configuration

# Basic variable references
app_name: string = @app.name
database_host: string = @database.host
server_port: number = @server.port
debug_mode: boolean = @app.debug

# Nested variable references
max_pool_size: number = @config.database.connection.pool.max_size
jwt_secret: string = @config.security.jwt.secret
connection_timeout: number = @config.database.connection.timeout

# Array variable references
primary_server_host: string = @servers[0].host
primary_server_port: number = @servers[0].port
second_server_host: string = @servers[1].host
second_allowed_origin: string = @allowed_origins[1]

# Dynamic variable references
environment: string = "production"
version: string = "v1"
dynamic_host: string = @dynamic_config[@environment].host
api_base_url: string = @api_endpoints[@version].base_url

# Computed variable references
connection_string: string = "postgresql://${@database.user}@${@database.host}:${@database.port}/${@database.name}"
server_url: string = "${@server.protocol}://${@server.host}:${@server.port}"
api_endpoint: string = "${@api_base_url}/users"

# Variable references in objects
database_config: object = {
    host: string = @database.host
    port: number = @database.port
    name: string = @database.name
    user: string = @database.user
    password: string = @database.password
    connection_string: string = @connection_string
}

server_config: object = {
    host: string = @server.host
    port: number = @server.port
    ssl: boolean = @server.ssl
    url: string = @server_url
}

# Variable references in arrays
server_list: array = [
    @servers[0]
    @servers[1]
]

allowed_origins: array = [
    @allowed_origins[0]
    @allowed_origins[1]
    @allowed_origins[2]
]

# Conditional variable references
environment_config: object = @if(@environment == "production", {
    ssl: boolean = true
    cache: boolean = true
    monitoring: boolean = true
    host: string = @dynamic_config.production.host
}, {
    ssl: boolean = false
    cache: boolean = false
    monitoring: boolean = false
    host: string = @dynamic_config.development.host
})

# Variable references with defaults
api_key: string = @env("API_KEY") || "default_key"
database_url: string = @env("DATABASE_URL") || @connection_string
log_level: string = @env("LOG_LEVEL") || "info"

# Computed variable references with functions
formatted_date: string = @date.format(@date.now(), "Y-m-d H:i:s")
encrypted_password: string = @encrypt(@database.password, "AES-256-GCM")
hashed_secret: string = @hash(@jwt_secret, "SHA-256")

# Variable references in validation
is_valid_config: boolean = @validate.required([
    @app.name
    @database.host
    @server.port
])

is_valid_port: boolean = @validate.range(@server.port, 1, 65535)
is_valid_host: boolean = @validate.hostname(@database.host)

# Variable references in database queries
user_count: number = @query("SELECT COUNT(*) FROM users WHERE environment = ?", @environment)
recent_users: array = @query("SELECT * FROM users WHERE created_at > ?", @date.subtract("7d"))
user_by_id: object = @query("SELECT * FROM users WHERE id = ?", @user_id)

# Variable references in HTTP requests
api_status: string = @http("GET", @api_base_url + "/status")
api_data: object = @http("POST", @api_base_url + "/data", {
    "environment": @environment
    "version": @version
    "timestamp": @date.now()
})

# Variable references in cache keys
cached_user: object = @cache("user_" + @user_id, "1h", @query("SELECT * FROM users WHERE id = ?", @user_id))
cached_config: object = @cache("config_" + @environment, "30m", @environment_config)

# Variable references in file operations
config_content: string = @file.read("config/" + @environment + ".json")
log_file: string = "logs/" + @app.name + "_" + @environment + ".log"
output_file: string = "output/" + @date.format(@date.now(), "Y-m-d") + "_data.json"

# Variable references in JSON operations
parsed_config: object = @json.parse(@config_content)
config_host: string = @json.path(@parsed_config, "database.host")
merged_config: object = @json.merge(@base_config, @environment_config)

# Variable references in string operations
formatted_message: string = @string.format("Hello {name}, welcome to {app}!", {
    "name": @user_name
    "app": @app.name
})

uppercase_app: string = @string.upper(@app.name)
lowercase_env: string = @string.lower(@environment)
trimmed_host: string = @string.trim(@database.host)

# Variable references in mathematical operations
total_servers: number = @math.add(@server_count, 1)
average_load: number = @math.avg(@server_loads)
scaled_port: number = @math.multiply(@base_port, @scale_factor)
rounded_timeout: number = @math.round(@connection_timeout, 2)
```

## 🔧 Variable Reference Patterns

### Java Variable Reference Patterns

```java
@TuskConfig
public class VariableReferencePatterns {
    
    // Pattern 1: Basic variable access
    @TuskReference("@config.value")
    private String configValue;
    
    // Pattern 2: Nested variable access
    @TuskReference("@config.nested.deep.value")
    private String nestedValue;
    
    // Pattern 3: Array variable access
    @TuskReference("@items[0].name")
    private String firstItemName;
    
    @TuskReference("@items[@index].value")
    private String indexedItemValue;
    
    // Pattern 4: Dynamic variable access
    @TuskReference("@config.${@environment}.setting")
    private String dynamicSetting;
    
    // Pattern 5: Computed variable access
    @TuskReference("@computed.value")
    private String computedValue;
    
    // Pattern 6: Conditional variable access
    @TuskReference("@conditional.value")
    private String conditionalValue;
    
    // Variable reference processor
    private VariableReferenceProcessor processor;
    
    public VariableReferencePatterns() {
        this.processor = new VariableReferenceProcessor();
    }
    
    // Process variable reference patterns
    public Object processVariableReference(String reference, Map<String, Object> context) {
        return processor.process(reference, context);
    }
    
    // Pattern matching methods
    public boolean isBasicReference(String reference) {
        return reference.matches("@\\w+(\\.\\w+)*");
    }
    
    public boolean isNestedReference(String reference) {
        return reference.matches("@\\w+(\\.\\w+)+");
    }
    
    public boolean isArrayReference(String reference) {
        return reference.matches("@\\w+\\[\\d+\\](\\.\\w+)*");
    }
    
    public boolean isDynamicReference(String reference) {
        return reference.contains("${");
    }
    
    public boolean isComputedReference(String reference) {
        return reference.startsWith("@computed.");
    }
    
    // Getters and setters
    public String getConfigValue() { return configValue; }
    public void setConfigValue(String configValue) { this.configValue = configValue; }
    
    public String getNestedValue() { return nestedValue; }
    public void setNestedValue(String nestedValue) { this.nestedValue = nestedValue; }
    
    public String getFirstItemName() { return firstItemName; }
    public void setFirstItemName(String firstItemName) { this.firstItemName = firstItemName; }
    
    public String getIndexedItemValue() { return indexedItemValue; }
    public void setIndexedItemValue(String indexedItemValue) { this.indexedItemValue = indexedItemValue; }
    
    public String getDynamicSetting() { return dynamicSetting; }
    public void setDynamicSetting(String dynamicSetting) { this.dynamicSetting = dynamicSetting; }
    
    public String getComputedValue() { return computedValue; }
    public void setComputedValue(String computedValue) { this.computedValue = computedValue; }
    
    public String getConditionalValue() { return conditionalValue; }
    public void setConditionalValue(String conditionalValue) { this.conditionalValue = conditionalValue; }
}

// Variable reference processor
public class VariableReferenceProcessor {
    
    // Process variable reference
    public Object process(String reference, Map<String, Object> context) {
        if (reference.startsWith("@computed.")) {
            return processComputedReference(reference, context);
        } else if (reference.contains("${")) {
            return processDynamicReference(reference, context);
        } else if (reference.matches("@\\w+\\[\\d+\\]")) {
            return processArrayReference(reference, context);
        } else {
            return processBasicReference(reference, context);
        }
    }
    
    // Process basic reference
    private Object processBasicReference(String reference, Map<String, Object> context) {
        String path = reference.substring(1);
        return getNestedValue(path, context);
    }
    
    // Process array reference
    private Object processArrayReference(String reference, Map<String, Object> context) {
        // Extract array name and index
        String arrayPath = reference.substring(1, reference.indexOf('['));
        String indexStr = reference.substring(reference.indexOf('[') + 1, reference.indexOf(']'));
        int index = Integer.parseInt(indexStr);
        
        Object array = getNestedValue(arrayPath, context);
        if (array instanceof List) {
            return ((List<?>) array).get(index);
        }
        
        throw new VariableReferenceException("Not an array: " + arrayPath);
    }
    
    // Process dynamic reference
    private Object processDynamicReference(String reference, Map<String, Object> context) {
        // Replace ${...} placeholders with actual values
        String processed = reference;
        while (processed.contains("${")) {
            int start = processed.indexOf("${");
            int end = processed.indexOf("}", start);
            
            if (end == -1) {
                throw new VariableReferenceException("Unclosed placeholder in: " + reference);
            }
            
            String placeholder = processed.substring(start + 2, end);
            Object value = getNestedValue(placeholder, context);
            
            processed = processed.substring(0, start) + value + processed.substring(end + 1);
        }
        
        return processBasicReference(processed, context);
    }
    
    // Process computed reference
    private Object processComputedReference(String reference, Map<String, Object> context) {
        String computation = reference.substring(10); // Remove "@computed."
        
        switch (computation) {
            case "connection_string":
                return computeConnectionString(context);
            case "server_url":
                return computeServerUrl(context);
            case "api_endpoint":
                return computeApiEndpoint(context);
            default:
                throw new VariableReferenceException("Unknown computation: " + computation);
        }
    }
    
    // Helper methods
    private Object getNestedValue(String path, Map<String, Object> context) {
        String[] parts = path.split("\\.");
        Object current = context;
        
        for (String part : parts) {
            if (current instanceof Map) {
                current = ((Map<String, Object>) current).get(part);
            } else {
                return null;
            }
        }
        
        return current;
    }
    
    private String computeConnectionString(Map<String, Object> context) {
        String host = (String) getNestedValue("database.host", context);
        int port = (Integer) getNestedValue("database.port", context);
        String name = (String) getNestedValue("database.name", context);
        String user = (String) getNestedValue("database.user", context);
        
        return String.format("postgresql://%s@%s:%d/%s", user, host, port, name);
    }
    
    private String computeServerUrl(Map<String, Object> context) {
        String host = (String) getNestedValue("server.host", context);
        int port = (Integer) getNestedValue("server.port", context);
        boolean ssl = (Boolean) getNestedValue("server.ssl", context);
        
        String protocol = ssl ? "https" : "http";
        return String.format("%s://%s:%d", protocol, host, port);
    }
    
    private String computeApiEndpoint(Map<String, Object> context) {
        String baseUrl = (String) getNestedValue("api.base_url", context);
        String version = (String) getNestedValue("api.version", context);
        
        return String.format("%s/%s", baseUrl, version);
    }
}
```

### TuskLang Variable Reference Patterns

```tusk
# Variable reference patterns in TuskLang

# Pattern 1: Basic variable access
app_name: string = @app.name
version: string = @app.version
environment: string = @app.environment

# Pattern 2: Nested variable access
database_host: string = @config.database.host
database_port: number = @config.database.port
jwt_secret: string = @config.security.jwt.secret

# Pattern 3: Array variable access
first_server: object = @servers[0]
second_server: object = @servers[1]
first_origin: string = @allowed_origins[0]

# Pattern 4: Dynamic variable access
env_host: string = @config[@environment].host
version_url: string = @api[@version].base_url
dynamic_setting: string = @settings[@feature].value

# Pattern 5: Computed variable access
connection_string: string = @computed.connection_string
server_url: string = @computed.server_url
api_endpoint: string = @computed.api_endpoint

# Pattern 6: Conditional variable access
ssl_enabled: boolean = @if(@environment == "production", true, false)
cache_enabled: boolean = @if(@config.cache.enabled, true, false)
monitoring_enabled: boolean = @if(@config.monitoring.enabled, true, false)

# Pattern 7: Variable references with defaults
api_key: string = @env("API_KEY") || @config.api.default_key
database_url: string = @env("DATABASE_URL") || @computed.connection_string
log_level: string = @env("LOG_LEVEL") || @config.logging.default_level

# Pattern 8: Variable references in functions
formatted_date: string = @date.format(@date.now(), "Y-m-d H:i:s")
encrypted_value: string = @encrypt(@sensitive_data, "AES-256-GCM")
hashed_value: string = @hash(@password, "SHA-256")

# Pattern 9: Variable references in validation
is_valid: boolean = @validate.required([
    @app.name
    @database.host
    @server.port
])

is_valid_port: boolean = @validate.range(@server.port, 1, 65535)
is_valid_host: boolean = @validate.hostname(@database.host)

# Pattern 10: Variable references in queries
user_count: number = @query("SELECT COUNT(*) FROM users WHERE environment = ?", @environment)
recent_data: array = @query("SELECT * FROM data WHERE created_at > ?", @date.subtract("7d"))
specific_user: object = @query("SELECT * FROM users WHERE id = ?", @user_id)

# Pattern 11: Variable references in HTTP requests
status_check: string = @http("GET", @api_base_url + "/status")
data_post: object = @http("POST", @api_base_url + "/data", {
    "environment": @environment
    "timestamp": @date.now()
})

# Pattern 12: Variable references in cache
cached_data: object = @cache("data_" + @user_id, "1h", @query("SELECT * FROM data WHERE user_id = ?", @user_id))
cached_config: object = @cache("config_" + @environment, "30m", @environment_config)
```

## 🎯 Best Practices

### Variable Reference Guidelines

1. **Use descriptive variable names** for clarity
2. **Validate variable references** before use
3. **Provide default values** for optional variables
4. **Use nested references** for organized configuration
5. **Handle missing variables** gracefully
6. **Document variable dependencies** clearly
7. **Use computed variables** for derived values
8. **Implement proper error handling** for variable resolution
9. **Cache expensive variable computations** when appropriate
10. **Test variable references** in all environments

### Performance Considerations

```java
// Efficient variable reference handling
@TuskConfig
public class EfficientVariableReferences {
    
    // Cache resolved variable references
    private Map<String, Object> referenceCache = new ConcurrentHashMap<>();
    
    // Resolve variable reference with caching
    public Object resolveWithCache(String reference, Map<String, Object> context) {
        return referenceCache.computeIfAbsent(reference, 
            key -> resolveVariableReference(key, context));
    }
    
    // Clear reference cache
    public void clearCache() {
        referenceCache.clear();
    }
    
    // Resolve variable reference
    private Object resolveVariableReference(String reference, Map<String, Object> context) {
        // Implementation here
        return null;
    }
}
```

## 🚨 Troubleshooting

### Common Variable Reference Issues

1. **Missing variables**: Use default values and validation
2. **Circular references**: Implement reference cycle detection
3. **Type mismatches**: Validate variable types before use
4. **Performance issues**: Cache expensive variable resolutions
5. **Security concerns**: Validate variable access permissions

### Debug Variable Reference Issues

```java
// Debug variable reference problems
public void debugVariableReferences(Map<String, Object> context) {
    System.out.println("Variable context:");
    for (Map.Entry<String, Object> entry : context.entrySet()) {
        System.out.println("  " + entry.getKey() + ": " + entry.getValue());
    }
    
    System.out.println("Variable references:");
    System.out.println("  App name: " + resolveVariableReference("@app.name", context));
    System.out.println("  Database host: " + resolveVariableReference("@database.host", context));
    System.out.println("  Server port: " + resolveVariableReference("@server.port", context));
}
```

## 🎯 Next Steps

1. **Explore variable reference chaining** and composition
2. **Learn about variable reference validation** and error handling
3. **Master dynamic variable references** for flexible configuration
4. **Implement custom variable reference processors** for your needs
5. **Optimize variable reference performance** in your applications

---

**Ready to master variable references with Java power? Variable references are the key to dynamic and flexible TuskLang configurations. We don't bow to any king - especially not static variable access!** 