# @ Variable Fallback in TuskLang - Java Edition

**"We don't bow to any king" - Fallback Power with Java Integration**

@ variable fallback in TuskLang provides robust error handling and graceful degradation when variables are missing or unavailable. The Java SDK offers comprehensive fallback mechanisms with type safety and validation.

## 🎯 Java @ Variable Fallback Integration

### @TuskConfig Fallback Support

```java
import org.tusklang.java.config.TuskConfig;
import org.tusklang.java.fallback.FallbackHandler;
import org.tusklang.java.fallback.FallbackStrategy;
import org.tusklang.java.annotations.TuskFallback;

@TuskConfig
public class VariableFallbackConfig {
    
    // Basic fallback with default values
    @TuskFallback("localhost")
    private String databaseHost;
    
    @TuskFallback("5432")
    private int databasePort;
    
    @TuskFallback("tuskdb")
    private String databaseName;
    
    @TuskFallback("postgres")
    private String databaseUser;
    
    @TuskFallback("")
    private String databasePassword;
    
    // Environment variable fallback
    @TuskFallback("@env.DB_HOST")
    private String envDatabaseHost;
    
    @TuskFallback("@env.DB_PORT")
    private int envDatabasePort;
    
    @TuskFallback("@env.DB_NAME")
    private String envDatabaseName;
    
    // Configuration fallback
    @TuskFallback("@config.database.host")
    private String configDatabaseHost;
    
    @TuskFallback("@config.database.port")
    private int configDatabasePort;
    
    // Computed fallback
    @TuskFallback("@computed.default_host")
    private String computedDatabaseHost;
    
    @TuskFallback("@computed.default_connection_string")
    private String computedConnectionString;
    
    // Conditional fallback
    @TuskFallback("@if(@environment == 'production', 'prod-db.example.com', 'localhost')")
    private String conditionalDatabaseHost;
    
    @TuskFallback("@if(@ssl_enabled, 443, 80)")
    private int conditionalPort;
    
    // Multiple fallback levels
    @TuskFallback("@env.DB_HOST || @config.database.host || 'localhost'")
    private String multiLevelDatabaseHost;
    
    @TuskFallback("@env.DB_PORT || @config.database.port || 5432")
    private int multiLevelDatabasePort;
    
    // Fallback with validation
    @TuskFallback("@validate.hostname(@env.DB_HOST) || 'localhost'")
    private String validatedDatabaseHost;
    
    @TuskFallback("@validate.port(@env.DB_PORT) || 5432")
    private int validatedDatabasePort;
    
    // Fallback handler
    private FallbackHandler fallbackHandler;
    private Map<String, Object> fallbackContext;
    
    public VariableFallbackConfig() {
        this.fallbackHandler = new FallbackHandler();
        this.fallbackContext = new ConcurrentHashMap<>();
        initializeFallbackContext();
    }
    
    // Initialize fallback context
    private void initializeFallbackContext() {
        // Environment variables
        fallbackContext.put("env", Map.of(
            "DB_HOST", System.getenv("DB_HOST"),
            "DB_PORT", System.getenv("DB_PORT"),
            "DB_NAME", System.getenv("DB_NAME"),
            "DB_USER", System.getenv("DB_USER"),
            "DB_PASSWORD", System.getenv("DB_PASSWORD")
        ));
        
        // Configuration defaults
        fallbackContext.put("config", Map.of(
            "database", Map.of(
                "host", "localhost",
                "port", 5432,
                "name", "tuskdb",
                "user", "postgres",
                "password", ""
            )
        ));
        
        // Environment setting
        fallbackContext.put("environment", System.getenv("ENVIRONMENT"));
        fallbackContext.put("ssl_enabled", Boolean.parseBoolean(System.getenv("SSL_ENABLED")));
    }
    
    // Resolve variable with fallback
    public Object resolveWithFallback(String variable, String fallback) {
        try {
            Object value = fallbackHandler.resolve(variable, fallbackContext);
            if (value != null) {
                return value;
            }
            
            return fallbackHandler.resolve(fallback, fallbackContext);
        } catch (Exception e) {
            return fallbackHandler.resolve(fallback, fallbackContext);
        }
    }
    
    // Resolve variable with multiple fallbacks
    public Object resolveWithMultipleFallbacks(String variable, String... fallbacks) {
        // Try primary variable
        try {
            Object value = fallbackHandler.resolve(variable, fallbackContext);
            if (value != null) {
                return value;
            }
        } catch (Exception e) {
            // Continue to fallbacks
        }
        
        // Try each fallback in order
        for (String fallback : fallbacks) {
            try {
                Object value = fallbackHandler.resolve(fallback, fallbackContext);
                if (value != null) {
                    return value;
                }
            } catch (Exception e) {
                // Continue to next fallback
            }
        }
        
        // Return null if all fallbacks fail
        return null;
    }
    
    // Resolve with conditional fallback
    public Object resolveWithConditionalFallback(String variable, String condition, String trueFallback, String falseFallback) {
        try {
            Object value = fallbackHandler.resolve(variable, fallbackContext);
            if (value != null) {
                return value;
            }
            
            boolean conditionResult = fallbackHandler.evaluateCondition(condition, fallbackContext);
            String fallback = conditionResult ? trueFallback : falseFallback;
            
            return fallbackHandler.resolve(fallback, fallbackContext);
        } catch (Exception e) {
            return fallbackHandler.resolve(falseFallback, fallbackContext);
        }
    }
    
    // Validate fallback configuration
    public boolean validateFallbackConfiguration() {
        List<String> errors = new ArrayList<>();
        
        // Validate database configuration with fallbacks
        String dbHost = (String) resolveWithFallback("@database.host", "@env.DB_HOST || 'localhost'");
        if (dbHost == null || dbHost.isEmpty()) {
            errors.add("Database host fallback failed");
        }
        
        Integer dbPort = (Integer) resolveWithFallback("@database.port", "@env.DB_PORT || 5432");
        if (dbPort == null || dbPort <= 0) {
            errors.add("Database port fallback failed");
        }
        
        String dbName = (String) resolveWithFallback("@database.name", "@env.DB_NAME || 'tuskdb'");
        if (dbName == null || dbName.isEmpty()) {
            errors.add("Database name fallback failed");
        }
        
        // Validate computed fallbacks
        String computedHost = (String) resolveWithFallback("@computed.database_host", "@config.database.host");
        if (computedHost == null || computedHost.isEmpty()) {
            errors.add("Computed database host fallback failed");
        }
        
        if (!errors.isEmpty()) {
            throw new FallbackException("Fallback validation failed: " + String.join(", ", errors));
        }
        
        return true;
    }
    
    // Getters and setters
    public String getDatabaseHost() { return databaseHost; }
    public void setDatabaseHost(String databaseHost) { this.databaseHost = databaseHost; }
    
    public int getDatabasePort() { return databasePort; }
    public void setDatabasePort(int databasePort) { this.databasePort = databasePort; }
    
    public String getDatabaseName() { return databaseName; }
    public void setDatabaseName(String databaseName) { this.databaseName = databaseName; }
    
    public String getDatabaseUser() { return databaseUser; }
    public void setDatabaseUser(String databaseUser) { this.databaseUser = databaseUser; }
    
    public String getDatabasePassword() { return databasePassword; }
    public void setDatabasePassword(String databasePassword) { this.databasePassword = databasePassword; }
    
    public String getEnvDatabaseHost() { return envDatabaseHost; }
    public void setEnvDatabaseHost(String envDatabaseHost) { this.envDatabaseHost = envDatabaseHost; }
    
    public int getEnvDatabasePort() { return envDatabasePort; }
    public void setEnvDatabasePort(int envDatabasePort) { this.envDatabasePort = envDatabasePort; }
    
    public String getEnvDatabaseName() { return envDatabaseName; }
    public void setEnvDatabaseName(String envDatabaseName) { this.envDatabaseName = envDatabaseName; }
    
    public String getConfigDatabaseHost() { return configDatabaseHost; }
    public void setConfigDatabaseHost(String configDatabaseHost) { this.configDatabaseHost = configDatabaseHost; }
    
    public int getConfigDatabasePort() { return configDatabasePort; }
    public void setConfigDatabasePort(int configDatabasePort) { this.configDatabasePort = configDatabasePort; }
    
    public String getComputedDatabaseHost() { return computedDatabaseHost; }
    public void setComputedDatabaseHost(String computedDatabaseHost) { this.computedDatabaseHost = computedDatabaseHost; }
    
    public String getComputedConnectionString() { return computedConnectionString; }
    public void setComputedConnectionString(String computedConnectionString) { this.computedConnectionString = computedConnectionString; }
    
    public String getConditionalDatabaseHost() { return conditionalDatabaseHost; }
    public void setConditionalDatabaseHost(String conditionalDatabaseHost) { this.conditionalDatabaseHost = conditionalDatabaseHost; }
    
    public int getConditionalPort() { return conditionalPort; }
    public void setConditionalPort(int conditionalPort) { this.conditionalPort = conditionalPort; }
    
    public String getMultiLevelDatabaseHost() { return multiLevelDatabaseHost; }
    public void setMultiLevelDatabaseHost(String multiLevelDatabaseHost) { this.multiLevelDatabaseHost = multiLevelDatabaseHost; }
    
    public int getMultiLevelDatabasePort() { return multiLevelDatabasePort; }
    public void setMultiLevelDatabasePort(int multiLevelDatabasePort) { this.multiLevelDatabasePort = multiLevelDatabasePort; }
    
    public String getValidatedDatabaseHost() { return validatedDatabaseHost; }
    public void setValidatedDatabaseHost(String validatedDatabaseHost) { this.validatedDatabaseHost = validatedDatabaseHost; }
    
    public int getValidatedDatabasePort() { return validatedDatabasePort; }
    public void setValidatedDatabasePort(int validatedDatabasePort) { this.validatedDatabasePort = validatedDatabasePort; }
}

// Fallback handler for Java integration
public class FallbackHandler {
    
    private Map<String, FallbackStrategy> strategies;
    private VariableResolver variableResolver;
    
    public FallbackHandler() {
        this.strategies = new ConcurrentHashMap<>();
        this.variableResolver = new VariableResolver();
        registerDefaultStrategies();
    }
    
    // Register fallback strategy
    public void registerStrategy(String name, FallbackStrategy strategy) {
        strategies.put(name, strategy);
    }
    
    // Resolve variable with fallback
    public Object resolve(String expression, Map<String, Object> context) {
        if (expression.contains("||")) {
            return resolveMultipleFallbacks(expression, context);
        } else if (expression.startsWith("@if(")) {
            return resolveConditionalFallback(expression, context);
        } else if (expression.startsWith("@env.")) {
            return resolveEnvironmentFallback(expression, context);
        } else if (expression.startsWith("@config.")) {
            return resolveConfigFallback(expression, context);
        } else if (expression.startsWith("@computed.")) {
            return resolveComputedFallback(expression, context);
        } else {
            return resolveSimpleFallback(expression, context);
        }
    }
    
    // Resolve multiple fallbacks (|| operator)
    private Object resolveMultipleFallbacks(String expression, Map<String, Object> context) {
        String[] fallbacks = expression.split("\\|\\|");
        
        for (String level : fallbacks) {
            try {
                Object value = resolveSimpleFallback(level.trim(), context);
                if (value != null) {
                    return value;
                }
            } catch (Exception e) {
                // Continue to next level
            }
        }
        
        return null;
    }
    
    // Resolve conditional fallback
    private Object resolveConditionalFallback(String expression, Map<String, Object> context) {
        // Parse @if(condition, true_value, false_value)
        String content = expression.substring(4, expression.length() - 1);
        String[] parts = parseConditionalParts(content);
        
        if (parts.length == 3) {
            boolean condition = evaluateCondition(parts[0], context);
            String value = condition ? parts[1] : parts[2];
            return resolveSimpleFallback(value, context);
        }
        
        throw new FallbackException("Invalid conditional fallback: " + expression);
    }
    
    // Parse conditional parts
    private String[] parseConditionalParts(String content) {
        List<String> parts = new ArrayList<>();
        int depth = 0;
        StringBuilder current = new StringBuilder();
        
        for (char c : content.toCharArray()) {
            if (c == '(') depth++;
            else if (c == ')') depth--;
            else if (c == ',' && depth == 0) {
                parts.add(current.toString().trim());
                current = new StringBuilder();
                continue;
            }
            
            current.append(c);
        }
        
        parts.add(current.toString().trim());
        return parts.toArray(new String[0]);
    }
    
    // Evaluate condition
    public boolean evaluateCondition(String condition, Map<String, Object> context) {
        // Simple condition evaluation
        if (condition.contains("==")) {
            String[] parts = condition.split("==");
            Object left = resolveSimpleFallback(parts[0].trim(), context);
            Object right = resolveSimpleFallback(parts[1].trim(), context);
            return Objects.equals(left, right);
        } else if (condition.contains("!=")) {
            String[] parts = condition.split("!=");
            Object left = resolveSimpleFallback(parts[0].trim(), context);
            Object right = resolveSimpleFallback(parts[1].trim(), context);
            return !Objects.equals(left, right);
        }
        
        // Boolean evaluation
        Object value = resolveSimpleFallback(condition, context);
        if (value instanceof Boolean) {
            return (Boolean) value;
        }
        
        return value != null;
    }
    
    // Resolve environment fallback
    private Object resolveEnvironmentFallback(String expression, Map<String, Object> context) {
        String envVar = expression.substring(5); // Remove "@env."
        return System.getenv(envVar);
    }
    
    // Resolve config fallback
    private Object resolveConfigFallback(String expression, Map<String, Object> context) {
        String configPath = expression.substring(8); // Remove "@config."
        return variableResolver.resolve(configPath, context);
    }
    
    // Resolve computed fallback
    private Object resolveComputedFallback(String expression, Map<String, Object> context) {
        String computation = expression.substring(10); // Remove "@computed."
        
        switch (computation) {
            case "default_host":
                return "localhost";
            case "default_port":
                return 8080;
            case "default_database":
                return "tuskdb";
            default:
                return null;
        }
    }
    
    // Resolve simple fallback
    private Object resolveSimpleFallback(String expression, Map<String, Object> context) {
        if (expression.startsWith("@")) {
            return variableResolver.resolve(expression.substring(1), context);
        } else {
            // Try to parse as literal value
            return parseLiteralValue(expression);
        }
    }
    
    // Parse literal value
    private Object parseLiteralValue(String value) {
        value = value.trim();
        
        if (value.equals("true") || value.equals("false")) {
            return Boolean.parseBoolean(value);
        } else if (value.matches("\\d+")) {
            return Integer.parseInt(value);
        } else if (value.matches("\\d+\\.\\d+")) {
            return Double.parseDouble(value);
        } else if (value.startsWith("\"") && value.endsWith("\"")) {
            return value.substring(1, value.length() - 1);
        } else if (value.startsWith("'") && value.endsWith("'")) {
            return value.substring(1, value.length() - 1);
        } else {
            return value;
        }
    }
    
    // Compute database host
    private String computeDatabaseHost(Map<String, Object> context) {
        String environment = (String) context.get("environment");
        if ("production".equals(environment)) {
            return "prod-db.example.com";
        } else if ("staging".equals(environment)) {
            return "staging-db.example.com";
        } else {
            return "localhost";
        }
    }
    
    // Register default strategies
    private void registerDefaultStrategies() {
        registerStrategy("env", new EnvironmentFallbackStrategy());
        registerStrategy("config", new ConfigFallbackStrategy());
        registerStrategy("computed", new ComputedFallbackStrategy());
        registerStrategy("conditional", new ConditionalFallbackStrategy());
    }
}

// Fallback strategy interface
public interface FallbackStrategy {
    Object execute(String expression, Map<String, Object> context);
    String getName();
}

// Environment fallback strategy
public class EnvironmentFallbackStrategy implements FallbackStrategy {
    
    @Override
    public Object execute(String expression, Map<String, Object> context) {
        return System.getenv(expression);
    }
    
    @Override
    public String getName() {
        return "env";
    }
}

// Config fallback strategy
public class ConfigFallbackStrategy implements FallbackStrategy {
    
    @Override
    public Object execute(String expression, Map<String, Object> context) {
        VariableResolver resolver = new VariableResolver();
        return resolver.resolve(expression, context);
    }
    
    @Override
    public String getName() {
        return "config";
    }
}

// Computed fallback strategy
public class ComputedFallbackStrategy implements FallbackStrategy {
    
    @Override
    public Object execute(String expression, Map<String, Object> context) {
        // Implement computed fallback logic
        return null;
    }
    
    @Override
    public String getName() {
        return "computed";
    }
}

// Conditional fallback strategy
public class ConditionalFallbackStrategy implements FallbackStrategy {
    
    @Override
    public Object execute(String expression, Map<String, Object> context) {
        // Implement conditional fallback logic
        return null;
    }
    
    @Override
    public String getName() {
        return "conditional";
    }
}

// Variable resolver
public class VariableResolver {
    
    public Object resolve(String path, Map<String, Object> context) {
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
}

// Fallback exception
public class FallbackException extends RuntimeException {
    
    public FallbackException(String message) {
        super(message);
    }
    
    public FallbackException(String message, Throwable cause) {
        super(message, cause);
    }
}
```

### TuskLang @ Variable Fallback Examples

```tusk
# @ Variable fallback examples in TuskLang configuration

# Basic fallback with default values
database_host: string = @env("DB_HOST") || "localhost"
database_port: number = @env("DB_PORT") || 5432
database_name: string = @env("DB_NAME") || "tuskdb"
database_user: string = @env("DB_USER") || "postgres"
database_password: string = @env("DB_PASSWORD") || ""

# Environment variable fallback
api_key: string = @env("API_KEY") || @config.api.default_key
api_url: string = @env("API_URL") || @config.api.base_url
log_level: string = @env("LOG_LEVEL") || @config.logging.default_level

# Configuration fallback
server_host: string = @config.server.host || @env("SERVER_HOST") || "0.0.0.0"
server_port: number = @config.server.port || @env("SERVER_PORT") || 8080
ssl_enabled: boolean = @config.server.ssl || @env("SSL_ENABLED") || false

# Computed fallback
connection_string: string = @computed.connection_string || "postgresql://postgres@localhost:5432/tuskdb"
server_url: string = @computed.server_url || "http://localhost:8080"

# Conditional fallback
environment: string = @env("ENVIRONMENT") || "development"
database_host: string = @if(@environment == "production", "prod-db.example.com", "localhost")
server_port: number = @if(@ssl_enabled, 443, 80)

# Multiple fallback levels
database_config: object = {
    host: string = @env("DB_HOST") || @config.database.host || "localhost"
    port: number = @env("DB_PORT") || @config.database.port || 5432
    name: string = @env("DB_NAME") || @config.database.name || "tuskdb"
}

# Fallback with validation
validated_host: string = @validate.hostname(@env("DB_HOST")) || "localhost"
validated_port: number = @validate.port(@env("DB_PORT")) || 5432

# Fallback with functions
formatted_date: string = @date.format(@env("START_DATE")) || @date.now()
encrypted_value: string = @encrypt(@env("SECRET_VALUE")) || @encrypt("default_secret")

# Fallback in database queries
user_count: number = @query("SELECT COUNT(*) FROM users") || 0
recent_user: object = @query("SELECT * FROM users ORDER BY created_at DESC LIMIT 1") || {
    "id": 0
    "name": "Default User"
    "email": "default@example.com"
}

# Fallback in HTTP requests
api_status: string = @http("GET", @api_url + "/status") || "unknown"
api_data: object = @http("GET", @api_url + "/data") || {
    "status": "offline"
    "message": "API unavailable"
}

# Fallback in cache operations
cached_value: string = @cache("key", "5m", @expensive_operation) || "default_value"
cached_user: object = @cache("user_" + @user_id, "1h", @query("SELECT * FROM users WHERE id = ?", @user_id)) || {
    "id": @user_id
    "name": "Unknown User"
}

# Fallback in file operations
config_content: string = @file.read(@config_file) || @file.read("config/default.json")
log_content: string = @file.read(@log_file) || "No log file available"
output_data: string = @file.read(@output_file) || "No output data"

# Fallback in JSON operations
parsed_config: object = @json.parse(@config_content) || {
    "database": {
        "host": "localhost"
        "port": 5432
    }
    "server": {
        "host": "0.0.0.0"
        "port": 8080
    }
}

# Fallback with complex conditions
complex_fallback: string = @if(@environment == "production", 
    @env("PROD_API_KEY") || @config.api.production_key,
    @if(@environment == "staging",
        @env("STAGING_API_KEY") || @config.api.staging_key,
        @env("DEV_API_KEY") || @config.api.development_key
    )
)

# Fallback with mathematical operations
computed_port: number = @math.add(@base_port, @port_offset) || 8080
scaled_value: number = @math.multiply(@base_value, @scale_factor) || 100
rounded_timeout: number = @math.round(@connection_timeout) || 30

# Fallback with string operations
formatted_name: string = @string.upper(@user_name) || "UNKNOWN"
trimmed_host: string = @string.trim(@database_host) || "localhost"
replaced_url: string = @string.replace(@api_url, "http://", "https://") || "https://api.example.com"

# Fallback in validation
is_valid_config: boolean = @validate.required([
    @database_host || "localhost"
    @database_port || 5432
    @database_name || "tuskdb"
]) || false

# Fallback with error handling
safe_operation: string = @try(@expensive_operation, "fallback_value")
safe_query: object = @try(@query("SELECT * FROM users WHERE id = ?", @user_id), {
    "id": @user_id
    "name": "Unknown User"
})
safe_http: string = @try(@http("GET", @api_url), "API unavailable")

# Fallback with logging
logged_value: string = @log.info(@env("IMPORTANT_VALUE") || "default_value")
logged_operation: string = @log.warn(@expensive_operation || "operation_failed")
logged_error: string = @log.error(@failed_operation || "operation_error")

# Fallback with metrics
measured_value: number = @metrics("operation_duration", @operation_duration || 0)
measured_count: number = @metrics("operation_count", @operation_count || 1)
measured_rate: number = @metrics("success_rate", @success_rate || 100.0)

# Fallback with machine learning
optimized_setting: number = @learn("optimal_timeout", @connection_timeout || 30)
predicted_value: number = @learn("predicted_load", @current_load || 0)
smart_default: string = @learn("user_preference", @user_setting || "default")
```

## 🔧 Fallback Patterns

### Java Fallback Patterns

```java
@TuskConfig
public class FallbackPatterns {
    
    // Pattern 1: Simple fallback
    @TuskFallback("default_value")
    private String simpleFallback;
    
    // Pattern 2: Environment fallback
    @TuskFallback("@env.VARIABLE_NAME")
    private String environmentFallback;
    
    // Pattern 3: Configuration fallback
    @TuskFallback("@config.section.key")
    private String configFallback;
    
    // Pattern 4: Multiple fallback levels
    @TuskFallback("@env.VAR || @config.key || 'default'")
    private String multiLevelFallback;
    
    // Pattern 5: Conditional fallback
    @TuskFallback("@if(@condition, 'true_value', 'false_value')")
    private String conditionalFallback;
    
    // Pattern 6: Computed fallback
    @TuskFallback("@computed.value")
    private String computedFallback;
    
    // Pattern 7: Validated fallback
    @TuskFallback("@validate.type(@value) || 'default'")
    private String validatedFallback;
    
    // Pattern 8: Function fallback
    @TuskFallback("@function.operation(@input) || 'default'")
    private String functionFallback;
    
    // Fallback processor
    private FallbackProcessor processor;
    
    public FallbackPatterns() {
        this.processor = new FallbackProcessor();
    }
    
    // Process fallback patterns
    public Object processFallback(String fallback, Map<String, Object> context) {
        return processor.process(fallback, context);
    }
    
    // Pattern recognition methods
    public boolean isSimpleFallback(String fallback) {
        return !fallback.startsWith("@") && !fallback.contains("||");
    }
    
    public boolean isEnvironmentFallback(String fallback) {
        return fallback.startsWith("@env.");
    }
    
    public boolean isConfigFallback(String fallback) {
        return fallback.startsWith("@config.");
    }
    
    public boolean isMultiLevelFallback(String fallback) {
        return fallback.contains("||");
    }
    
    public boolean isConditionalFallback(String fallback) {
        return fallback.startsWith("@if(");
    }
    
    public boolean isComputedFallback(String fallback) {
        return fallback.startsWith("@computed.");
    }
    
    public boolean isValidatedFallback(String fallback) {
        return fallback.contains("@validate.");
    }
    
    public boolean isFunctionFallback(String fallback) {
        return fallback.contains("@function.") || fallback.contains("@math.") || fallback.contains("@string.");
    }
    
    // Getters and setters
    public String getSimpleFallback() { return simpleFallback; }
    public void setSimpleFallback(String simpleFallback) { this.simpleFallback = simpleFallback; }
    
    public String getEnvironmentFallback() { return environmentFallback; }
    public void setEnvironmentFallback(String environmentFallback) { this.environmentFallback = environmentFallback; }
    
    public String getConfigFallback() { return configFallback; }
    public void setConfigFallback(String configFallback) { this.configFallback = configFallback; }
    
    public String getMultiLevelFallback() { return multiLevelFallback; }
    public void setMultiLevelFallback(String multiLevelFallback) { this.multiLevelFallback = multiLevelFallback; }
    
    public String getConditionalFallback() { return conditionalFallback; }
    public void setConditionalFallback(String conditionalFallback) { this.conditionalFallback = conditionalFallback; }
    
    public String getComputedFallback() { return computedFallback; }
    public void setComputedFallback(String computedFallback) { this.computedFallback = computedFallback; }
    
    public String getValidatedFallback() { return validatedFallback; }
    public void setValidatedFallback(String validatedFallback) { this.validatedFallback = validatedFallback; }
    
    public String getFunctionFallback() { return functionFallback; }
    public void setFunctionFallback(String functionFallback) { this.functionFallback = functionFallback; }
}

// Fallback processor
public class FallbackProcessor {
    
    // Process fallback expression
    public Object process(String fallback, Map<String, Object> context) {
        if (fallback.contains("||")) {
            return processMultiLevelFallback(fallback, context);
        } else if (fallback.startsWith("@if(")) {
            return processConditionalFallback(fallback, context);
        } else if (fallback.startsWith("@env.")) {
            return processEnvironmentFallback(fallback, context);
        } else if (fallback.startsWith("@config.")) {
            return processConfigFallback(fallback, context);
        } else if (fallback.startsWith("@computed.")) {
            return processComputedFallback(fallback, context);
        } else if (fallback.startsWith("@validate.")) {
            return processValidatedFallback(fallback, context);
        } else if (fallback.startsWith("@function.") || fallback.startsWith("@math.") || fallback.startsWith("@string.")) {
            return processFunctionFallback(fallback, context);
        } else {
            return processSimpleFallback(fallback, context);
        }
    }
    
    // Process multi-level fallback
    private Object processMultiLevelFallback(String fallback, Map<String, Object> context) {
        String[] levels = fallback.split("\\|\\|");
        
        for (String level : levels) {
            try {
                Object value = process(level.trim(), context);
                if (value != null) {
                    return value;
                }
            } catch (Exception e) {
                // Continue to next level
            }
        }
        
        return null;
    }
    
    // Process conditional fallback
    private Object processConditionalFallback(String fallback, Map<String, Object> context) {
        // Implementation for conditional fallback
        return null;
    }
    
    // Process environment fallback
    private Object processEnvironmentFallback(String fallback, Map<String, Object> context) {
        String envVar = fallback.substring(5); // Remove "@env."
        return System.getenv(envVar);
    }
    
    // Process config fallback
    private Object processConfigFallback(String fallback, Map<String, Object> context) {
        String configPath = fallback.substring(8); // Remove "@config."
        return getNestedValue(configPath, context);
    }
    
    // Process computed fallback
    private Object processComputedFallback(String fallback, Map<String, Object> context) {
        String computation = fallback.substring(10); // Remove "@computed."
        
        switch (computation) {
            case "default_host":
                return "localhost";
            case "default_port":
                return 8080;
            case "default_database":
                return "tuskdb";
            default:
                return null;
        }
    }
    
    // Process validated fallback
    private Object processValidatedFallback(String fallback, Map<String, Object> context) {
        // Implementation for validated fallback
        return null;
    }
    
    // Process function fallback
    private Object processFunctionFallback(String fallback, Map<String, Object> context) {
        // Implementation for function fallback
        return null;
    }
    
    // Process simple fallback
    private Object processSimpleFallback(String fallback, Map<String, Object> context) {
        return parseLiteralValue(fallback);
    }
    
    // Parse literal value
    private Object parseLiteralValue(String value) {
        value = value.trim();
        
        if (value.equals("true") || value.equals("false")) {
            return Boolean.parseBoolean(value);
        } else if (value.matches("\\d+")) {
            return Integer.parseInt(value);
        } else if (value.matches("\\d+\\.\\d+")) {
            return Double.parseDouble(value);
        } else if (value.startsWith("\"") && value.endsWith("\"")) {
            return value.substring(1, value.length() - 1);
        } else if (value.startsWith("'") && value.endsWith("'")) {
            return value.substring(1, value.length() - 1);
        } else {
            return value;
        }
    }
    
    // Get nested value
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
}
```

### TuskLang Fallback Patterns

```tusk
# Fallback patterns in TuskLang

# Pattern 1: Simple fallback
app_name: string = @env("APP_NAME") || "TuskLang App"
debug_mode: boolean = @env("DEBUG") || false
timeout: number = @env("TIMEOUT") || 30

# Pattern 2: Environment fallback
database_host: string = @env("DB_HOST") || "localhost"
database_port: number = @env("DB_PORT") || 5432
api_key: string = @env("API_KEY") || "default_key"

# Pattern 3: Configuration fallback
server_host: string = @config.server.host || "0.0.0.0"
server_port: number = @config.server.port || 8080
ssl_enabled: boolean = @config.server.ssl || false

# Pattern 4: Multiple fallback levels
database_config: object = {
    host: string = @env("DB_HOST") || @config.database.host || "localhost"
    port: number = @env("DB_PORT") || @config.database.port || 5432
    name: string = @env("DB_NAME") || @config.database.name || "tuskdb"
}

# Pattern 5: Conditional fallback
environment: string = @env("ENVIRONMENT") || "development"
database_host: string = @if(@environment == "production", "prod-db.example.com", "localhost")
server_port: number = @if(@ssl_enabled, 443, 80)

# Pattern 6: Computed fallback
connection_string: string = @computed.connection_string || "postgresql://postgres@localhost:5432/tuskdb"
server_url: string = @computed.server_url || "http://localhost:8080"

# Pattern 7: Validated fallback
validated_host: string = @validate.hostname(@env("DB_HOST")) || "localhost"
validated_port: number = @validate.port(@env("DB_PORT")) || 5432

# Pattern 8: Function fallback
formatted_date: string = @date.format(@env("START_DATE")) || @date.now()
encrypted_value: string = @encrypt(@env("SECRET_VALUE")) || @encrypt("default_secret")

# Pattern 9: Complex conditional fallback
api_key: string = @if(@environment == "production", 
    @env("PROD_API_KEY") || @config.api.production_key,
    @if(@environment == "staging",
        @env("STAGING_API_KEY") || @config.api.staging_key,
        @env("DEV_API_KEY") || @config.api.development_key
    )
)

# Pattern 10: Safe operation fallback
safe_value: string = @try(@expensive_operation, "fallback_value")
safe_query: object = @try(@query("SELECT * FROM users WHERE id = ?", @user_id), {
    "id": @user_id
    "name": "Unknown User"
})
```

## 🎯 Best Practices

### Fallback Guidelines

1. **Use appropriate fallback levels** for different scenarios
2. **Provide meaningful default values** for all variables
3. **Validate fallback values** before use
4. **Log fallback usage** for debugging
5. **Use conditional fallbacks** for environment-specific values
6. **Implement graceful degradation** with fallbacks
7. **Test fallback behavior** in all environments
8. **Document fallback strategies** clearly
9. **Monitor fallback usage** in production
10. **Use computed fallbacks** for complex default values

### Performance Considerations

```java
// Efficient fallback handling
@TuskConfig
public class EfficientFallbacks {
    
    // Cache fallback results
    private Map<String, Object> fallbackCache = new ConcurrentHashMap<>();
    
    // Resolve fallback with caching
    public Object resolveWithCache(String fallback, Map<String, Object> context) {
        return fallbackCache.computeIfAbsent(fallback, 
            key -> resolveFallback(key, context));
    }
    
    // Clear fallback cache
    public void clearCache() {
        fallbackCache.clear();
    }
    
    // Resolve fallback
    private Object resolveFallback(String fallback, Map<String, Object> context) {
        // Implementation here
        return null;
    }
}
```

## 🚨 Troubleshooting

### Common Fallback Issues

1. **Missing fallback values**: Always provide defaults
2. **Circular fallbacks**: Avoid circular dependencies
3. **Type mismatches**: Validate fallback types
4. **Performance issues**: Cache expensive fallbacks
5. **Security concerns**: Validate fallback sources

### Debug Fallback Issues

```java
// Debug fallback problems
public void debugFallbacks(Map<String, Object> context) {
    System.out.println("Fallback context:");
    for (Map.Entry<String, Object> entry : context.entrySet()) {
        System.out.println("  " + entry.getKey() + ": " + entry.getValue());
    }
    
    System.out.println("Fallback resolution:");
    System.out.println("  Database host: " + resolveWithFallback("@database.host", "localhost"));
    System.out.println("  Server port: " + resolveWithFallback("@server.port", "8080"));
    System.out.println("  API key: " + resolveWithFallback("@api.key", "default_key"));
}
```

## 🎯 Next Steps

1. **Explore fallback chaining** and composition
2. **Learn about fallback validation** and error handling
3. **Master conditional fallbacks** for complex scenarios
4. **Implement custom fallback strategies** for your needs
5. **Optimize fallback performance** in your applications

---

**Ready to master fallbacks with Java power? Fallbacks are essential for robust and resilient TuskLang configurations. We don't bow to any king - especially not missing variables!** 