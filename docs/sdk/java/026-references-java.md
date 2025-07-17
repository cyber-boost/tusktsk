# References in TuskLang - Java Edition

**"We don't bow to any king" - Reference Power with Java Integration**

References in TuskLang allow you to create powerful links between configuration values, avoid duplication, and build complex configuration hierarchies. The Java SDK provides robust reference handling with type safety and validation.

## 🎯 Java Reference Integration

### @TuskConfig Reference Annotations

```java
import org.tusklang.java.config.TuskConfig;
import org.tusklang.java.annotations.TuskReference;
import org.tusklang.java.annotations.TuskAlias;
import org.tusklang.java.annotations.TuskInherit;

@TuskConfig
public class ReferenceConfig {
    
    @TuskReference("database.host")
    private String dbHost;
    
    @TuskReference("server.port")
    private int serverPort;
    
    @TuskAlias("app_name")
    private String applicationName;
    
    @TuskInherit("base_config")
    private BaseConfig baseSettings;
    
    @TuskReference("database")
    private DatabaseConfig database;
    
    // Computed references
    private String connectionString;
    private String apiUrl;
    
    // Reference resolution methods
    public String resolveConnectionString() {
        return String.format("postgresql://%s:%d/%s", 
            dbHost, serverPort, database.getName());
    }
    
    public String resolveApiUrl() {
        return String.format("http://%s:%d/api", dbHost, serverPort);
    }
    
    // Getters and setters
    public String getDbHost() { return dbHost; }
    public void setDbHost(String dbHost) { this.dbHost = dbHost; }
    
    public int getServerPort() { return serverPort; }
    public void setServerPort(int serverPort) { this.serverPort = serverPort; }
    
    public String getApplicationName() { return applicationName; }
    public void setApplicationName(String applicationName) { this.applicationName = applicationName; }
    
    public BaseConfig getBaseSettings() { return baseSettings; }
    public void setBaseSettings(BaseConfig baseSettings) { this.baseSettings = baseSettings; }
    
    public DatabaseConfig getDatabase() { return database; }
    public void setDatabase(DatabaseConfig database) { this.database = database; }
    
    public String getConnectionString() { return connectionString; }
    public void setConnectionString(String connectionString) { this.connectionString = connectionString; }
    
    public String getApiUrl() { return apiUrl; }
    public void setApiUrl(String apiUrl) { this.apiUrl = apiUrl; }
}

@TuskConfig
public class BaseConfig {
    
    private String environment;
    private boolean debug;
    private int timeout;
    
    // Getters and setters
    public String getEnvironment() { return environment; }
    public void setEnvironment(String environment) { this.environment = environment; }
    
    public boolean isDebug() { return debug; }
    public void setDebug(boolean debug) { this.debug = debug; }
    
    public int getTimeout() { return timeout; }
    public void setTimeout(int timeout) { this.timeout = timeout; }
}
```

### TuskLang Reference Configuration

```tusk
# Base configuration with references
base_config: object = {
    environment: string = "development"
    debug: boolean = true
    timeout: number = 30
}

# Database configuration
database: object = {
    host: string = "localhost"
    port: number = 5432
    name: string = "tuskdb"
    user: string = "postgres"
    password: string = @env("DB_PASSWORD")
}

# Server configuration
server: object = {
    host: string = "0.0.0.0"
    port: number = 8080
    ssl: boolean = false
}

# Application configuration with references
app_name: string = "TuskLang Enterprise App"
app_config: object = {
    # Direct references
    db_host: string = @database.host
    db_port: number = @database.port
    server_port: number = @server.port
    
    # Computed references
    connection_string: string = "postgresql://${@database.user}@${@database.host}:${@database.port}/${@database.name}"
    api_url: string = "http://${@server.host}:${@server.port}/api"
    
    # Inherited configuration
    ...@base_config
}
```

## 🔗 Cross-Object References

### Java Cross-Reference Handling

```java
@TuskConfig
public class CrossReferenceConfig {
    
    @TuskReference("primary.database")
    private DatabaseConfig primaryDB;
    
    @TuskReference("secondary.database")
    private DatabaseConfig secondaryDB;
    
    @TuskReference("load_balancer.servers")
    private List<ServerConfig> servers;
    
    @TuskReference("cache.redis")
    private RedisConfig redis;
    
    // Cross-reference validation
    public boolean validateCrossReferences() {
        // Ensure primary and secondary databases are different
        if (primaryDB.getHost().equals(secondaryDB.getHost()) && 
            primaryDB.getPort() == secondaryDB.getPort()) {
            throw new IllegalStateException("Primary and secondary databases must be different");
        }
        
        // Validate server configurations
        for (ServerConfig server : servers) {
            if (server.getPort() <= 0) {
                throw new IllegalStateException("Invalid server port: " + server.getPort());
            }
        }
        
        return true;
    }
    
    // Reference-based methods
    public String getPrimaryConnectionString() {
        return String.format("postgresql://%s:%d/%s", 
            primaryDB.getHost(), primaryDB.getPort(), primaryDB.getName());
    }
    
    public String getSecondaryConnectionString() {
        return String.format("postgresql://%s:%d/%s", 
            secondaryDB.getHost(), secondaryDB.getPort(), secondaryDB.getName());
    }
    
    // Getters and setters
    public DatabaseConfig getPrimaryDB() { return primaryDB; }
    public void setPrimaryDB(DatabaseConfig primaryDB) { this.primaryDB = primaryDB; }
    
    public DatabaseConfig getSecondaryDB() { return secondaryDB; }
    public void setSecondaryDB(DatabaseConfig secondaryDB) { this.secondaryDB = secondaryDB; }
    
    public List<ServerConfig> getServers() { return servers; }
    public void setServers(List<ServerConfig> servers) { this.servers = servers; }
    
    public RedisConfig getRedis() { return redis; }
    public void setRedis(RedisConfig redis) { this.redis = redis; }
}

@TuskConfig
public class ServerConfig {
    
    private String name;
    private String host;
    private int port;
    private double weight;
    
    // Getters and setters
    public String getName() { return name; }
    public void setName(String name) { this.name = name; }
    
    public String getHost() { return host; }
    public void setHost(String host) { this.host = host; }
    
    public int getPort() { return port; }
    public void setPort(int port) { this.port = port; }
    
    public double getWeight() { return weight; }
    public void setWeight(double weight) { this.weight = weight; }
}

@TuskConfig
public class RedisConfig {
    
    private String host;
    private int port;
    private String password;
    private int database;
    
    // Getters and setters
    public String getHost() { return host; }
    public void setHost(String host) { this.host = host; }
    
    public int getPort() { return port; }
    public void setPort(int port) { this.port = port; }
    
    public String getPassword() { return password; }
    public void setPassword(String password) { this.password = password; }
    
    public int getDatabase() { return database; }
    public void setDatabase(int database) { this.database = database; }
}
```

### TuskLang Cross-Reference Configuration

```tusk
# Primary database configuration
primary: object = {
    database: object = {
        host: string = "primary-db.example.com"
        port: number = 5432
        name: string = "primary_db"
        user: string = "primary_user"
        password: string = @env.secure("PRIMARY_DB_PASSWORD")
    }
}

# Secondary database configuration
secondary: object = {
    database: object = {
        host: string = "secondary-db.example.com"
        port: number = 5432
        name: string = "secondary_db"
        user: string = "secondary_user"
        password: string = @env.secure("SECONDARY_DB_PASSWORD")
    }
}

# Load balancer configuration
load_balancer: object = {
    servers: object[] = [
        {
            name: string = "server-1"
            host: string = "lb1.example.com"
            port: number = 8080
            weight: number = 1.0
        }
        {
            name: string = "server-2"
            host: string = "lb2.example.com"
            port: number = 8080
            weight: number = 0.8
        }
    ]
}

# Cache configuration
cache: object = {
    redis: object = {
        host: string = "redis.example.com"
        port: number = 6379
        password: string = @env.secure("REDIS_PASSWORD")
        database: number = 0
    }
}

# Cross-reference validation
validation: object = {
    # Ensure databases are different
    databases_different: boolean = @assert(
        @primary.database.host != @secondary.database.host || 
        @primary.database.port != @secondary.database.port,
        "Primary and secondary databases must be different"
    )
    
    # Validate server weights
    valid_weights: boolean = @all(@load_balancer.servers, @lambda(server, @server.weight > 0))
}
```

## 📁 File References and Imports

### Java File Reference Handling

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.config.TuskConfig;
import org.tusklang.java.annotations.TuskImport;

@TuskConfig
public class FileReferenceConfig {
    
    @TuskImport("database.tsk")
    private DatabaseConfig databaseConfig;
    
    @TuskImport("server.tsk")
    private ServerConfig serverConfig;
    
    @TuskImport("security.tsk")
    private SecurityConfig securityConfig;
    
    // Dynamic file loading based on environment
    private TuskLang parser;
    
    public void loadEnvironmentConfig(String environment) {
        String configFile = String.format("config/%s.tsk", environment);
        parser = new TuskLang();
        
        try {
            // Load environment-specific configuration
            EnvironmentConfig envConfig = parser.parseFile(configFile, EnvironmentConfig.class);
            mergeConfiguration(envConfig);
        } catch (Exception e) {
            // Fallback to default configuration
            loadDefaultConfig();
        }
    }
    
    private void mergeConfiguration(EnvironmentConfig envConfig) {
        // Merge environment-specific settings with base configuration
        if (envConfig.getDatabase() != null) {
            this.databaseConfig = envConfig.getDatabase();
        }
        if (envConfig.getServer() != null) {
            this.serverConfig = envConfig.getServer();
        }
    }
    
    private void loadDefaultConfig() {
        // Load default configuration
        databaseConfig = parser.parseFile("config/default/database.tsk", DatabaseConfig.class);
        serverConfig = parser.parseFile("config/default/server.tsk", ServerConfig.class);
    }
    
    // Getters and setters
    public DatabaseConfig getDatabaseConfig() { return databaseConfig; }
    public void setDatabaseConfig(DatabaseConfig databaseConfig) { this.databaseConfig = databaseConfig; }
    
    public ServerConfig getServerConfig() { return serverConfig; }
    public void setServerConfig(ServerConfig serverConfig) { this.serverConfig = serverConfig; }
    
    public SecurityConfig getSecurityConfig() { return securityConfig; }
    public void setSecurityConfig(SecurityConfig securityConfig) { this.securityConfig = securityConfig; }
}

@TuskConfig
public class EnvironmentConfig {
    
    private DatabaseConfig database;
    private ServerConfig server;
    private SecurityConfig security;
    
    // Getters and setters
    public DatabaseConfig getDatabase() { return database; }
    public void setDatabase(DatabaseConfig database) { this.database = database; }
    
    public ServerConfig getServer() { return server; }
    public void setServer(ServerConfig server) { this.server = server; }
    
    public SecurityConfig getSecurity() { return security; }
    public void setSecurity(SecurityConfig security) { this.security = security; }
}

@TuskConfig
public class SecurityConfig {
    
    private String jwtSecret;
    private int tokenExpiry;
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

### TuskLang File Reference Configuration

```tusk
# Main configuration file (app.tsk)
# Import other configuration files
@import("database.tsk")
@import("server.tsk")
@import("security.tsk")

# Application configuration
app: object = {
    name: string = "TuskLang Enterprise"
    version: string = "1.0.0"
    environment: string = @env("ENVIRONMENT", "development")
}

# Environment-specific imports
@if(@env("ENVIRONMENT") == "production", {
    @import("config/production.tsk")
}, {
    @import("config/development.tsk")
})

# Cross-file references
database_connection: string = "postgresql://${@database.host}:${@database.port}/${@database.name}"
api_endpoint: string = "https://${@server.host}:${@server.port}/api/v1"
```

## 🔄 Dynamic References

### Java Dynamic Reference Resolution

```java
@TuskConfig
public class DynamicReferenceConfig {
    
    private Map<String, Object> dynamicValues;
    private String environment;
    private String region;
    
    // Dynamic reference resolution
    public Object resolveDynamicReference(String referencePath) {
        String[] path = referencePath.split("\\.");
        Object current = dynamicValues;
        
        for (String key : path) {
            if (current instanceof Map) {
                current = ((Map<String, Object>) current).get(key);
            } else {
                return null;
            }
        }
        
        return current;
    }
    
    // Environment-based reference resolution
    public String resolveEnvironmentReference(String basePath) {
        String envPath = String.format("%s.%s", basePath, environment);
        Object value = resolveDynamicReference(envPath);
        
        if (value == null) {
            // Fallback to default
            value = resolveDynamicReference(basePath);
        }
        
        return value != null ? value.toString() : null;
    }
    
    // Region-based reference resolution
    public String resolveRegionalReference(String basePath) {
        String regionPath = String.format("%s.%s.%s", basePath, environment, region);
        Object value = resolveDynamicReference(regionPath);
        
        if (value == null) {
            // Fallback to environment-specific
            value = resolveEnvironmentReference(basePath);
        }
        
        return value != null ? value.toString() : null;
    }
    
    // Getters and setters
    public Map<String, Object> getDynamicValues() { return dynamicValues; }
    public void setDynamicValues(Map<String, Object> dynamicValues) { this.dynamicValues = dynamicValues; }
    
    public String getEnvironment() { return environment; }
    public void setEnvironment(String environment) { this.environment = environment; }
    
    public String getRegion() { return region; }
    public void setRegion(String region) { this.region = region; }
}
```

### TuskLang Dynamic Reference Configuration

```tusk
# Dynamic configuration based on environment
environments: object = {
    development: object = {
        database: object = {
            host: string = "localhost"
            port: number = 5432
        }
        cache: object = {
            ttl: number = 300
        }
    }
    staging: object = {
        database: object = {
            host: string = "staging-db.example.com"
            port: number = 5432
        }
        cache: object = {
            ttl: number = 600
        }
    }
    production: object = {
        database: object = {
            host: string = "prod-db.example.com"
            port: number = 5432
        }
        cache: object = {
            ttl: number = 3600
        }
    }
}

# Regional configuration
regions: object = {
    us_east: object = {
        database: object = {
            host: string = "us-east-db.example.com"
        }
    }
    us_west: object = {
        database: object = {
            host: string = "us-west-db.example.com"
        }
    }
    eu_west: object = {
        database: object = {
            host: string = "eu-west-db.example.com"
        }
    }
}

# Dynamic reference resolution
current_environment: string = @env("ENVIRONMENT", "development")
current_region: string = @env("REGION", "us_east")

# Resolve dynamic references
database_host: string = @environments[@current_environment].database.host
cache_ttl: number = @environments[@current_environment].cache.ttl
regional_db_host: string = @regions[@current_region].database.host
```

## 🛡️ Reference Validation

### Java Reference Validation

```java
@TuskConfig
public class ReferenceValidationConfig {
    
    @TuskReference("database.host")
    private String databaseHost;
    
    @TuskReference("server.port")
    private int serverPort;
    
    // Reference validation
    public boolean validateReferences() {
        // Check for circular references
        if (hasCircularReference()) {
            throw new IllegalStateException("Circular reference detected");
        }
        
        // Validate required references
        if (databaseHost == null || databaseHost.isEmpty()) {
            throw new IllegalStateException("Database host reference is required");
        }
        
        if (serverPort <= 0) {
            throw new IllegalStateException("Server port must be positive");
        }
        
        return true;
    }
    
    private boolean hasCircularReference() {
        // Implement circular reference detection
        // This is a simplified example
        return false;
    }
    
    // Reference resolution with validation
    public String resolveValidatedReference(String referencePath) {
        Object value = resolveReference(referencePath);
        
        if (value == null) {
            throw new IllegalStateException("Reference not found: " + referencePath);
        }
        
        return value.toString();
    }
    
    private Object resolveReference(String referencePath) {
        // Implement reference resolution logic
        return null;
    }
    
    // Getters and setters
    public String getDatabaseHost() { return databaseHost; }
    public void setDatabaseHost(String databaseHost) { this.databaseHost = databaseHost; }
    
    public int getServerPort() { return serverPort; }
    public void setServerPort(int serverPort) { this.serverPort = serverPort; }
}
```

### TuskLang Reference Validation

```tusk
# Reference validation in TuskLang
validation: object = {
    # Check for required references
    database_host_required: boolean = @assert(@database.host != null, "Database host is required")
    server_port_required: boolean = @assert(@server.port > 0, "Server port must be positive")
    
    # Validate reference types
    database_port_number: boolean = @assert(@isNumber(@database.port), "Database port must be a number")
    server_ssl_boolean: boolean = @assert(@isBoolean(@server.ssl), "Server SSL must be a boolean")
    
    # Check for circular references (simplified)
    no_circular_refs: boolean = @assert(@self != @database, "Circular reference detected")
}
```

## 🎯 Best Practices

### Reference Management Guidelines

1. **Use descriptive reference names** for clarity
2. **Validate references** at runtime for reliability
3. **Handle missing references** gracefully
4. **Avoid circular references** in configuration
5. **Use environment-specific references** for flexibility

### Performance Considerations

```java
// Efficient reference caching
@TuskConfig
public class CachedReferenceConfig {
    
    private Map<String, Object> referenceCache = new ConcurrentHashMap<>();
    
    public Object getCachedReference(String referencePath) {
        return referenceCache.computeIfAbsent(referencePath, this::resolveReference);
    }
    
    private Object resolveReference(String referencePath) {
        // Implement reference resolution logic
        return null;
    }
}
```

## 🚨 Troubleshooting

### Common Reference Issues

1. **Missing references**: Always provide fallback values
2. **Circular references**: Use validation to detect cycles
3. **Type mismatches**: Validate reference types
4. **File not found**: Handle import errors gracefully

### Debug Reference Issues

```java
// Debug reference resolution
public void debugReferences() {
    System.out.println("Database host: " + databaseHost);
    System.out.println("Server port: " + serverPort);
    System.out.println("Environment: " + environment);
}
```

## 🎯 Next Steps

1. **Explore file imports** for modular configuration
2. **Learn about validation** for reference integrity
3. **Master dynamic references** for flexible configuration
4. **Implement reference caching** for performance
5. **Build modular configuration** systems

---

**Ready to reference your way to configuration greatness? TuskLang's Java integration gives you the power of dynamic references with the safety of static typing. We don't bow to any king - especially not reference constraints!** 