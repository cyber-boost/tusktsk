# Variable Naming in TuskLang - Java Edition

**"We don't bow to any king" - Naming Conventions with Java Power**

Variable naming in TuskLang follows powerful conventions that integrate seamlessly with Java naming patterns. This guide covers naming best practices, conventions, and how to maintain consistency between TuskLang configuration and Java code.

## 🎯 Java Naming Integration

### @TuskConfig Naming Conventions

```java
import org.tusklang.java.config.TuskConfig;
import org.tusklang.java.annotations.TuskName;
import org.tusklang.java.annotations.TuskAlias;

@TuskConfig
public class NamingConventionConfig {
    
    // Java camelCase properties that map to TuskLang snake_case
    @TuskName("app_name")
    private String appName;
    
    @TuskName("database_host")
    private String databaseHost;
    
    @TuskName("api_key")
    private String apiKey;
    
    @TuskName("max_connections")
    private int maxConnections;
    
    @TuskName("enable_ssl")
    private boolean enableSsl;
    
    @TuskName("connection_timeout")
    private int connectionTimeout;
    
    // Nested object with naming conventions
    @TuskName("db_config")
    private DatabaseConfig databaseConfig;
    
    @TuskName("server_config")
    private ServerConfig serverConfig;
    
    // Array with naming conventions
    @TuskName("allowed_origins")
    private List<String> allowedOrigins;
    
    // Getters and setters follow Java conventions
    public String getAppName() { return appName; }
    public void setAppName(String appName) { this.appName = appName; }
    
    public String getDatabaseHost() { return databaseHost; }
    public void setDatabaseHost(String databaseHost) { this.databaseHost = databaseHost; }
    
    public String getApiKey() { return apiKey; }
    public void setApiKey(String apiKey) { this.apiKey = apiKey; }
    
    public int getMaxConnections() { return maxConnections; }
    public void setMaxConnections(int maxConnections) { this.maxConnections = maxConnections; }
    
    public boolean isEnableSsl() { return enableSsl; }
    public void setEnableSsl(boolean enableSsl) { this.enableSsl = enableSsl; }
    
    public int getConnectionTimeout() { return connectionTimeout; }
    public void setConnectionTimeout(int connectionTimeout) { this.connectionTimeout = connectionTimeout; }
    
    public DatabaseConfig getDatabaseConfig() { return databaseConfig; }
    public void setDatabaseConfig(DatabaseConfig databaseConfig) { this.databaseConfig = databaseConfig; }
    
    public ServerConfig getServerConfig() { return serverConfig; }
    public void setServerConfig(ServerConfig serverConfig) { this.serverConfig = serverConfig; }
    
    public List<String> getAllowedOrigins() { return allowedOrigins; }
    public void setAllowedOrigins(List<String> allowedOrigins) { this.allowedOrigins = allowedOrigins; }
}

@TuskConfig
public class DatabaseConfig {
    
    @TuskName("db_host")
    private String host;
    
    @TuskName("db_port")
    private int port;
    
    @TuskName("db_name")
    private String name;
    
    @TuskName("db_user")
    private String user;
    
    @TuskName("db_password")
    private String password;
    
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
}

@TuskConfig
public class ServerConfig {
    
    @TuskName("server_host")
    private String host;
    
    @TuskName("server_port")
    private int port;
    
    @TuskName("server_ssl")
    private boolean ssl;
    
    // Getters and setters
    public String getHost() { return host; }
    public void setHost(String host) { this.host = host; }
    
    public int getPort() { return port; }
    public void setPort(int port) { this.port = port; }
    
    public boolean isSsl() { return ssl; }
    public void setSsl(boolean ssl) { this.ssl = ssl; }
}
```

### TuskLang Naming Configuration

```tusk
# TuskLang uses snake_case for variable names
app_name: string = "TuskLang Enterprise Application"
database_host: string = "localhost"
api_key: string = @env.secure("API_KEY")
max_connections: number = 100
enable_ssl: boolean = true
connection_timeout: number = 30

# Nested objects with consistent naming
db_config: object = {
    db_host: string = "db.example.com"
    db_port: number = 5432
    db_name: string = "enterprise_db"
    db_user: string = "app_user"
    db_password: string = @env.secure("DB_PASSWORD")
}

server_config: object = {
    server_host: string = "0.0.0.0"
    server_port: number = 8080
    server_ssl: boolean = false
}

# Arrays with descriptive names
allowed_origins: string[] = [
    "https://app.example.com"
    "https://api.example.com"
    "http://localhost:3000"
]

# Environment-specific naming
environment: string = @env("ENVIRONMENT", "development")
log_level: string = @if(@environment == "production", "warn", "debug")
```

## 🏗️ Naming Patterns and Conventions

### Java Naming Patterns

```java
@TuskConfig
public class NamingPatternsConfig {
    
    // Constants follow Java conventions
    public static final String DEFAULT_HOST = "localhost";
    public static final int DEFAULT_PORT = 8080;
    public static final boolean DEFAULT_SSL = false;
    
    // Configuration sections with prefixes
    @TuskName("app_")
    private AppConfig appConfig;
    
    @TuskName("db_")
    private DatabaseConfig databaseConfig;
    
    @TuskName("cache_")
    private CacheConfig cacheConfig;
    
    @TuskName("security_")
    private SecurityConfig securityConfig;
    
    // Environment-specific naming
    private String environment;
    private Map<String, Object> environmentConfigs;
    
    // Naming validation methods
    public boolean validateNamingConventions() {
        // Validate that all names follow conventions
        return validateSnakeCase() && validateDescriptiveNames();
    }
    
    private boolean validateSnakeCase() {
        // Check that TuskLang names use snake_case
        return true; // Simplified validation
    }
    
    private boolean validateDescriptiveNames() {
        // Check that names are descriptive and meaningful
        return true; // Simplified validation
    }
    
    // Getters and setters
    public AppConfig getAppConfig() { return appConfig; }
    public void setAppConfig(AppConfig appConfig) { this.appConfig = appConfig; }
    
    public DatabaseConfig getDatabaseConfig() { return databaseConfig; }
    public void setDatabaseConfig(DatabaseConfig databaseConfig) { this.databaseConfig = databaseConfig; }
    
    public CacheConfig getCacheConfig() { return cacheConfig; }
    public void setCacheConfig(CacheConfig cacheConfig) { this.cacheConfig = cacheConfig; }
    
    public SecurityConfig getSecurityConfig() { return securityConfig; }
    public void setSecurityConfig(SecurityConfig securityConfig) { this.securityConfig = securityConfig; }
    
    public String getEnvironment() { return environment; }
    public void setEnvironment(String environment) { this.environment = environment; }
    
    public Map<String, Object> getEnvironmentConfigs() { return environmentConfigs; }
    public void setEnvironmentConfigs(Map<String, Object> environmentConfigs) { this.environmentConfigs = environmentConfigs; }
}

@TuskConfig
public class AppConfig {
    
    @TuskName("app_name")
    private String name;
    
    @TuskName("app_version")
    private String version;
    
    @TuskName("app_environment")
    private String environment;
    
    // Getters and setters
    public String getName() { return name; }
    public void setName(String name) { this.name = name; }
    
    public String getVersion() { return version; }
    public void setVersion(String version) { this.version = version; }
    
    public String getEnvironment() { return environment; }
    public void setEnvironment(String environment) { this.environment = environment; }
}

@TuskConfig
public class CacheConfig {
    
    @TuskName("cache_driver")
    private String driver;
    
    @TuskName("cache_ttl")
    private int ttl;
    
    @TuskName("cache_host")
    private String host;
    
    // Getters and setters
    public String getDriver() { return driver; }
    public void setDriver(String driver) { this.driver = driver; }
    
    public int getTtl() { return ttl; }
    public void setTtl(int ttl) { this.ttl = ttl; }
    
    public String getHost() { return host; }
    public void setHost(String host) { this.host = host; }
}

@TuskConfig
public class SecurityConfig {
    
    @TuskName("security_jwt_secret")
    private String jwtSecret;
    
    @TuskName("security_token_expiry")
    private int tokenExpiry;
    
    @TuskName("security_allowed_origins")
    private List<String> allowedOrigins;
    
    // Getters and setters
    public String getJwtSecret() { return jwtSecret; }
    public void setJwtSecret(String jwtSecret) { this.jwtSecret = jwtSecret; }
    
    public int getTokenExpiry() { return tokenExpiry; }
    public void setTokenExpiry(int tokenExpiry) { this.tokenExpiry = tokenExpiry; }
    
    public List<String> getAllowedOrigins() { return allowedOrigins; }
    public void setAllowedOrigins(List<String> allowedOrigins) { this.allowedOrigins = allowedOrigins; }
}
```

### TuskLang Naming Patterns

```tusk
# Section-based naming with prefixes
app_name: string = "TuskLang Enterprise"
app_version: string = "1.0.0"
app_environment: string = @env("ENVIRONMENT", "development")

# Database configuration with db_ prefix
db_host: string = "localhost"
db_port: number = 5432
db_name: string = "enterprise_db"
db_user: string = "app_user"
db_password: string = @env.secure("DB_PASSWORD")

# Cache configuration with cache_ prefix
cache_driver: string = "redis"
cache_ttl: number = 300
cache_host: string = "redis.example.com"

# Security configuration with security_ prefix
security_jwt_secret: string = @env.secure("JWT_SECRET")
security_token_expiry: number = 3600
security_allowed_origins: string[] = [
    "https://app.example.com"
    "https://api.example.com"
]

# Environment-specific naming patterns
environment: string = @env("ENVIRONMENT", "development")
log_level: string = @if(@environment == "production", "warn", "debug")
debug_mode: boolean = @environment != "production"
```

## 🔄 Naming Transformation

### Java Naming Transformation

```java
import org.tusklang.java.utils.NamingUtils;

@TuskConfig
public class NamingTransformationConfig {
    
    // Automatic naming transformation
    @TuskName("user_authentication_config")
    private UserAuthConfig userAuthConfig;
    
    @TuskName("api_rate_limiting_config")
    private RateLimitConfig rateLimitConfig;
    
    // Manual naming transformation methods
    public String toSnakeCase(String camelCase) {
        return NamingUtils.camelToSnake(camelCase);
    }
    
    public String toCamelCase(String snakeCase) {
        return NamingUtils.snakeToCamel(snakeCase);
    }
    
    public String toPascalCase(String snakeCase) {
        return NamingUtils.snakeToPascal(snakeCase);
    }
    
    // Naming validation with transformation
    public boolean validateAndTransform(String inputName) {
        String transformedName = toSnakeCase(inputName);
        return NamingUtils.isValidTuskName(transformedName);
    }
    
    // Getters and setters
    public UserAuthConfig getUserAuthConfig() { return userAuthConfig; }
    public void setUserAuthConfig(UserAuthConfig userAuthConfig) { this.userAuthConfig = userAuthConfig; }
    
    public RateLimitConfig getRateLimitConfig() { return rateLimitConfig; }
    public void setRateLimitConfig(RateLimitConfig rateLimitConfig) { this.rateLimitConfig = rateLimitConfig; }
}

@TuskConfig
public class UserAuthConfig {
    
    @TuskName("auth_enabled")
    private boolean enabled;
    
    @TuskName("auth_provider")
    private String provider;
    
    @TuskName("auth_timeout")
    private int timeout;
    
    // Getters and setters
    public boolean isEnabled() { return enabled; }
    public void setEnabled(boolean enabled) { this.enabled = enabled; }
    
    public String getProvider() { return provider; }
    public void setProvider(String provider) { this.provider = provider; }
    
    public int getTimeout() { return timeout; }
    public void setTimeout(int timeout) { this.timeout = timeout; }
}

@TuskConfig
public class RateLimitConfig {
    
    @TuskName("rate_limit_enabled")
    private boolean enabled;
    
    @TuskName("rate_limit_requests_per_minute")
    private int requestsPerMinute;
    
    @TuskName("rate_limit_burst_size")
    private int burstSize;
    
    // Getters and setters
    public boolean isEnabled() { return enabled; }
    public void setEnabled(boolean enabled) { this.enabled = enabled; }
    
    public int getRequestsPerMinute() { return requestsPerMinute; }
    public void setRequestsPerMinute(int requestsPerMinute) { this.requestsPerMinute = requestsPerMinute; }
    
    public int getBurstSize() { return burstSize; }
    public void setBurstSize(int burstSize) { this.burstSize = burstSize; }
}
```

### TuskLang Naming Transformation

```tusk
# Automatic naming transformation in TuskLang
user_authentication_config: object = {
    auth_enabled: boolean = true
    auth_provider: string = "jwt"
    auth_timeout: number = 3600
}

api_rate_limiting_config: object = {
    rate_limit_enabled: boolean = true
    rate_limit_requests_per_minute: number = 100
    rate_limit_burst_size: number = 10
}

# Naming transformation functions
camel_case_name: string = "userAuthenticationConfig"
snake_case_name: string = @toSnakeCase(@camel_case_name)  # "user_authentication_config"

pascal_case_name: string = "UserAuthenticationConfig"
converted_snake: string = @toSnakeCase(@pascal_case_name)  # "user_authentication_config"
```

## 🚫 Reserved Naming Patterns

### Java Reserved Naming Handling

```java
@TuskConfig
public class ReservedNamingConfig {
    
    // Avoid reserved words in TuskLang
    @TuskName("config_type")  // Instead of "type"
    private String configType;
    
    @TuskName("config_class")  // Instead of "class"
    private String configClass;
    
    @TuskName("config_function")  // Instead of "function"
    private String configFunction;
    
    @TuskName("config_object")  // Instead of "object"
    private String configObject;
    
    @TuskName("config_array")  // Instead of "array"
    private String configArray;
    
    // Reserved naming validation
    public boolean validateReservedNames() {
        Set<String> reservedWords = Set.of(
            "type", "class", "function", "object", "array",
            "string", "number", "boolean", "null", "true", "false"
        );
        
        // Check that no reserved words are used
        return !reservedWords.contains(configType) &&
               !reservedWords.contains(configClass) &&
               !reservedWords.contains(configFunction);
    }
    
    // Getters and setters
    public String getConfigType() { return configType; }
    public void setConfigType(String configType) { this.configType = configType; }
    
    public String getConfigClass() { return configClass; }
    public void setConfigClass(String configClass) { this.configClass = configClass; }
    
    public String getConfigFunction() { return configFunction; }
    public void setConfigFunction(String configFunction) { this.configFunction = configFunction; }
    
    public String getConfigObject() { return configObject; }
    public void setConfigObject(String configObject) { this.configObject = configObject; }
    
    public String getConfigArray() { return configArray; }
    public void setConfigArray(String configArray) { this.configArray = configArray; }
}
```

### TuskLang Reserved Naming

```tusk
# Avoid reserved words in TuskLang configuration
config_type: string = "application"  # Instead of "type"
config_class: string = "main"        # Instead of "class"
config_function: string = "handler"  # Instead of "function"
config_object: string = "settings"   # Instead of "object"
config_array: string = "items"       # Instead of "array"

# Use descriptive names instead of reserved words
data_type: string = "json"
class_name: string = "UserService"
function_name: string = "processData"
object_name: string = "UserConfig"
array_name: string = "UserList"

# Reserved word validation
validation: object = {
    no_reserved_words: boolean = @assert(
        @config_type != "type" && 
        @config_class != "class" && 
        @config_function != "function",
        "Cannot use reserved words as variable names"
    )
}
```

## 🎯 Best Practices

### Naming Convention Guidelines

1. **Use snake_case in TuskLang** for consistency
2. **Use camelCase in Java** following Java conventions
3. **Use descriptive names** that explain the purpose
4. **Use prefixes for sections** (app_, db_, cache_, etc.)
5. **Avoid reserved words** in both TuskLang and Java

### Performance Considerations

```java
// Efficient naming validation
@TuskConfig
public class EfficientNamingConfig {
    
    private static final Set<String> RESERVED_WORDS = Set.of(
        "type", "class", "function", "object", "array"
    );
    
    private Map<String, String> nameCache = new ConcurrentHashMap<>();
    
    public String getCachedName(String originalName) {
        return nameCache.computeIfAbsent(originalName, this::transformName);
    }
    
    private String transformName(String name) {
        // Transform camelCase to snake_case
        return name.replaceAll("([a-z])([A-Z])", "$1_$2").toLowerCase();
    }
    
    public boolean isReservedWord(String name) {
        return RESERVED_WORDS.contains(name.toLowerCase());
    }
}
```

## 🚨 Troubleshooting

### Common Naming Issues

1. **Case sensitivity**: Ensure consistent case usage
2. **Reserved words**: Avoid using reserved words as names
3. **Naming conflicts**: Use unique names within scope
4. **Transformation errors**: Validate naming transformations

### Debug Naming Issues

```java
// Debug naming conventions
public void debugNaming() {
    System.out.println("App name: " + appName);
    System.out.println("Database host: " + databaseHost);
    System.out.println("API key: " + apiKey);
    System.out.println("Max connections: " + maxConnections);
}
```

## 🎯 Next Steps

1. **Explore naming validation** for consistency
2. **Learn about naming transformation** utilities
3. **Master naming patterns** for different contexts
4. **Implement naming conventions** across your project
5. **Build naming validation** tools

---

**Ready to name your way to configuration greatness? TuskLang's Java integration gives you the power of consistent naming with the flexibility of transformation. We don't bow to any king - especially not naming constraints!** 