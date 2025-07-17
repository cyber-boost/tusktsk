# 🌍 @env Function in TuskLang Java

**"We don't bow to any king" - Master environment variables like a Java master**

TuskLang Java provides powerful @env function capabilities that integrate seamlessly with Spring Boot, JPA, and modern Java patterns. Access, validate, and manage environment variables with enterprise-grade security and flexibility.

## 🎯 Overview

@env function in TuskLang Java combines the power of Java's environment variable handling with TuskLang's dynamic configuration system. From secure environment access to validation and transformation, we'll show you how to build robust environment variable management.

## 🔧 Core @env Function Features

### 1. Basic Environment Variable Access
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.env.TuskEnvFunctionManager;
import java.util.Map;
import java.util.List;

public class EnvFunctionExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        
        String tskContent = """
            [env_function_examples]
            # Basic environment variable access
            simple_env: @env("API_KEY", "default_value")
            
            # Environment variable with validation
            validated_env: @env("DATABASE_URL")
                .validate(url -> url != null && url.startsWith("jdbc:"), "Invalid database URL")
                .default("jdbc:postgresql://localhost:5432/default")
            
            # Environment variable with transformation
            transformed_env: @env("APP_NAME", "TuskLang App")
                .toUpperCase()
                .replace(" ", "_")
            
            # Environment variable with type conversion
            typed_env: @env("SERVER_PORT", "8080")
                .toInteger()
                .validate(port -> port > 0 && port < 65536, "Invalid port number")
            
            [spring_boot_env_functions]
            # Spring Boot integration with @env functions
            app_config: {
                name: @env("SPRING_APPLICATION_NAME", "TuskLang App")
                    .validate(name -> name != null && !name.isEmpty(), "App name is required")
                    .sanitize_input()
                
                version: @env("APP_VERSION", "1.0.0")
                    .validate(version -> version.matches("^\\d+\\.\\d+\\.\\d+"), "Invalid version format")
                
                environment: @env("SPRING_PROFILES_ACTIVE", "dev")
                    .toUpperCase()
                    .validate(env -> ["DEV", "TEST", "PROD"].contains(env), "Invalid environment")
                
                debug: @env("DEBUG_MODE", "false")
                    .toBoolean()
                    .validate(debug -> debug instanceof Boolean, "Invalid debug value")
                
                database: {
                    url: @env("DATABASE_URL")
                        .validate(url -> url != null && url.startsWith("jdbc:"), "Invalid database URL")
                        .default("jdbc:postgresql://localhost:5432/tusklang")
                    
                    username: @env("DB_USERNAME")
                        .validate(username -> username != null && username.length() > 0, "Database username required")
                        .sanitize_input()
                        .default("postgres")
                    
                    password: @env("DB_PASSWORD")
                        .validate(password -> password != null, "Database password required")
                        .encrypt("AES-256-GCM")
                        .default("")
                    
                    pool_size: @env("DB_POOL_SIZE", "20")
                        .toInteger()
                        .validate(size -> size > 0 && size <= 100, "Invalid pool size")
                }
                
                server: {
                    port: @env("SERVER_PORT", "8080")
                        .toInteger()
                        .validate(port -> port > 0 && port < 65536, "Invalid port number")
                    
                    host: @env("SERVER_HOST", "localhost")
                        .validate(host -> host != null, "Server host required")
                        .sanitize_input()
                    
                    context_path: @env("SERVER_CONTEXT_PATH", "/")
                        .validate(path -> path.startsWith("/"), "Context path must start with /")
                }
            }
            
            [jpa_env_functions]
            # JPA integration with @env functions
            entity_config: {
                user_count: @env("USER_COUNT_LIMIT", "1000")
                    .toInteger()
                    .validate(count -> count > 0 && count <= 10000, "Invalid user count limit")
                
                cache_ttl: @env("CACHE_TTL_SECONDS", "300")
                    .toInteger()
                    .validate(ttl -> ttl > 0 && ttl <= 3600, "Invalid cache TTL")
                
                batch_size: @env("BATCH_SIZE", "100")
                    .toInteger()
                    .validate(size -> size > 0 && size <= 1000, "Invalid batch size")
            }
            """;
        
        Map<String, Object> config = parser.parse(tskContent);
        
        // Access environment variable results
        System.out.println("Simple env: " + config.get("simple_env"));
        System.out.println("Validated env: " + config.get("validated_env"));
        System.out.println("Transformed env: " + config.get("transformed_env"));
        System.out.println("Typed env: " + config.get("typed_env"));
    }
}
```

### 2. Advanced Environment Variable Patterns
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.env.TuskEnvFunctionManager;
import org.springframework.stereotype.Service;
import org.springframework.beans.factory.annotation.Autowired;
import javax.persistence.EntityManager;
import java.util.Map;
import java.util.List;

@Service
public class AdvancedEnvFunctionService {
    
    @Autowired
    private EntityManager entityManager;
    
    @Autowired
    private TuskLang tuskParser;
    
    public Map<String, Object> processAdvancedEnvFunctions() {
        String tskContent = """
            [advanced_env_functions]
            # Multi-level environment variable processing
            complex_env_config: {
                api_configuration: {
                    base_url: @env("API_BASE_URL")
                        .validate(url -> url != null && url.startsWith("http"), "Invalid API URL")
                        .then(url -> url.replace("localhost", "prod-server"))
                        .then(url -> url.append("?ssl=true"))
                        .default("https://api.tusklang.org")
                    
                    authentication: {
                        token: @env("API_TOKEN")
                            .validate(token -> token != null && token.length() >= 32, "Invalid API token")
                            .then(token -> token.encrypt("AES-256-GCM"))
                            .default("default_token")
                        
                        timeout: @env("API_TIMEOUT", "30000")
                            .toInteger()
                            .validate(timeout -> timeout > 0 && timeout <= 300000, "Invalid timeout")
                            .then(timeout -> timeout * 1000) // Convert to milliseconds
                        
                        retries: @env("API_RETRIES", "3")
                            .toInteger()
                            .validate(retries -> retries >= 0 && retries <= 10, "Invalid retry count")
                    }
                }
                
                database_configuration: {
                    primary: {
                        url: @env("PRIMARY_DB_URL")
                            .validate(url -> url != null && url.startsWith("jdbc:"), "Invalid primary DB URL")
                            .default("jdbc:postgresql://localhost:5432/primary")
                        
                        credentials: {
                            username: @env("PRIMARY_DB_USERNAME")
                                .validate(username -> username != null, "Primary DB username required")
                                .sanitize_input()
                                .default("postgres")
                            
                            password: @env("PRIMARY_DB_PASSWORD")
                                .validate(password -> password != null, "Primary DB password required")
                                .encrypt("AES-256-GCM")
                                .default("")
                        }
                        
                        connection_pool: {
                            max_size: @env("PRIMARY_DB_MAX_POOL", "20")
                                .toInteger()
                                .validate(size -> size > 0 && size <= 100, "Invalid max pool size")
                            
                            min_size: @env("PRIMARY_DB_MIN_POOL", "5")
                                .toInteger()
                                .validate(size -> size > 0, "Invalid min pool size")
                            
                            timeout: @env("PRIMARY_DB_TIMEOUT", "30000")
                                .toInteger()
                                .validate(timeout -> timeout > 0, "Invalid timeout")
                        }
                    }
                    
                    secondary: {
                        url: @env("SECONDARY_DB_URL")
                            .validate(url -> url != null && url.startsWith("jdbc:"), "Invalid secondary DB URL")
                            .default("jdbc:postgresql://localhost:5432/secondary")
                        
                        credentials: {
                            username: @env("SECONDARY_DB_USERNAME")
                                .validate(username -> username != null, "Secondary DB username required")
                                .sanitize_input()
                                .default("readonly")
                            
                            password: @env("SECONDARY_DB_PASSWORD")
                                .validate(password -> password != null, "Secondary DB password required")
                                .encrypt("AES-256-GCM")
                                .default("")
                        }
                    }
                }
                
                cache_configuration: {
                    redis: {
                        host: @env("REDIS_HOST", "localhost")
                            .validate(host -> host != null, "Redis host required")
                            .sanitize_input()
                        
                        port: @env("REDIS_PORT", "6379")
                            .toInteger()
                            .validate(port -> port > 0 && port < 65536, "Invalid Redis port")
                        
                        database: @env("REDIS_DB", "0")
                            .toInteger()
                            .validate(db -> db >= 0 && db <= 15, "Invalid Redis database")
                        
                        password: @env("REDIS_PASSWORD")
                            .validate(password -> password != null, "Redis password required")
                            .encrypt("AES-256-GCM")
                            .default("")
                        
                        ttl: @env("REDIS_TTL", "3600")
                            .toInteger()
                            .validate(ttl -> ttl > 0, "Invalid Redis TTL")
                    }
                }
            }
            
            # Environment-specific configuration
            environment_specific_config: @env("ENVIRONMENT", "development")
                .equals("production")
                .then({
                    api_url: @env("PROD_API_URL", "https://api.tusklang.org")
                        .validate(url -> url.startsWith("https://"), "Production API must use HTTPS")
                    
                    database_url: @env("PROD_DB_URL")
                        .validate(url -> url.startsWith("jdbc:postgresql://"), "Invalid production DB URL")
                    
                    cache_ttl: @env("PROD_CACHE_TTL", "1800")
                        .toInteger()
                        .validate(ttl -> ttl >= 300, "Production cache TTL must be at least 5 minutes")
                })
                .else({
                    api_url: @env("DEV_API_URL", "http://localhost:3000")
                        .validate(url -> url.startsWith("http"), "Invalid development API URL")
                    
                    database_url: @env("DEV_DB_URL", "jdbc:postgresql://localhost:5432/dev")
                        .validate(url -> url.startsWith("jdbc:"), "Invalid development DB URL")
                    
                    cache_ttl: @env("DEV_CACHE_TTL", "300")
                        .toInteger()
                        .validate(ttl -> ttl > 0, "Invalid development cache TTL")
                })
            """;
        
        return tuskParser.parse(tskContent);
    }
    
    public Map<String, Object> processConditionalEnvFunctions() {
        String tskContent = """
            [conditional_env_functions]
            # Conditional environment variable processing
            conditional_config: {
                # Feature flags
                feature_flags: {
                    enable_caching: @env("ENABLE_CACHING", "true")
                        .toBoolean()
                        .validate(flag -> flag instanceof Boolean, "Invalid caching flag")
                    
                    enable_monitoring: @env("ENABLE_MONITORING", "false")
                        .toBoolean()
                        .validate(flag -> flag instanceof Boolean, "Invalid monitoring flag")
                    
                    enable_ssl: @env("ENABLE_SSL", "true")
                        .toBoolean()
                        .validate(flag -> flag instanceof Boolean, "Invalid SSL flag")
                }
                
                # Conditional database configuration
                database_config: @env("DB_TYPE", "postgresql")
                    .equals("postgresql")
                    .then({
                        driver: "org.postgresql.Driver"
                        url: @env("POSTGRES_URL", "jdbc:postgresql://localhost:5432/tusklang")
                        username: @env("POSTGRES_USERNAME", "postgres")
                        password: @env("POSTGRES_PASSWORD", "")
                    })
                    .else({
                        driver: "com.mysql.cj.jdbc.Driver"
                        url: @env("MYSQL_URL", "jdbc:mysql://localhost:3306/tusklang")
                        username: @env("MYSQL_USERNAME", "root")
                        password: @env("MYSQL_PASSWORD", "")
                    })
                
                # Conditional API configuration
                api_config: @env("API_VERSION", "v1")
                    .equals("v2")
                    .then({
                        base_url: @env("API_V2_BASE_URL", "https://api-v2.tusklang.org")
                        timeout: @env("API_V2_TIMEOUT", "30000").toInteger()
                        retries: @env("API_V2_RETRIES", "3").toInteger()
                    })
                    .else({
                        base_url: @env("API_V1_BASE_URL", "https://api-v1.tusklang.org")
                        timeout: @env("API_V1_TIMEOUT", "60000").toInteger()
                        retries: @env("API_V1_RETRIES", "5").toInteger()
                    })
            }
            """;
        
        return tuskParser.parse(tskContent);
    }
}
```

### 3. Spring Boot Integration
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.env.TuskEnvFunctionManager;
import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.boot.context.properties.ConfigurationProperties;
import java.util.Map;

@SpringBootApplication
public class EnvFunctionApplication {
    
    public static void main(String[] args) {
        SpringApplication.run(EnvFunctionApplication.class, args);
    }
}

@Configuration
public class EnvFunctionConfig {
    
    @Bean
    public TuskLang tuskLang() {
        return new TuskLang();
    }
    
    @Bean
    public TuskEnvFunctionManager envFunctionManager() {
        return new TuskEnvFunctionManager();
    }
    
    @Bean
    @ConfigurationProperties(prefix = "tusk.env-functions")
    public EnvFunctionProperties envFunctionProperties() {
        return new EnvFunctionProperties();
    }
    
    @Bean
    public Map<String, Object> envFunctionConfiguration() {
        TuskLang parser = new TuskLang();
        
        String tskContent = """
            [spring_env_functions]
            # Spring Boot configuration with @env functions
            application: {
                info: {
                    name: @env("SPRING_APPLICATION_NAME", "TuskLang App")
                        .validate(name -> name != null && !name.isEmpty(), "Application name required")
                        .sanitize_input()
                        .cache("1h")
                    
                    version: @env("APP_VERSION", "1.0.0")
                        .validate(version -> version.matches("^\\d+\\.\\d+\\.\\d+"), "Invalid version format")
                        .cache("1h")
                    
                    description: @env("APP_DESCRIPTION", "TuskLang Java Application")
                        .validate(desc -> desc.length() < 500, "Description too long")
                        .sanitize_input()
                        .cache("1h")
                }
                
                profiles: {
                    active: @env("SPRING_PROFILES_ACTIVE", "dev")
                        .toUpperCase()
                        .validate(profile -> ["DEV", "TEST", "PROD"].contains(profile), "Invalid profile")
                        .cache("1h")
                    
                    default: @env("SPRING_PROFILES_DEFAULT", "dev")
                        .toUpperCase()
                        .validate(profile -> ["DEV", "TEST", "PROD"].contains(profile), "Invalid default profile")
                        .cache("1h")
                }
                
                server: {
                    port: @env("SERVER_PORT", "8080")
                        .toInteger()
                        .validate(port -> port > 0 && port < 65536, "Invalid port number")
                        .cache("1h")
                    
                    host: @env("SERVER_HOST", "localhost")
                        .validate(host -> host != null, "Server host required")
                        .sanitize_input()
                        .cache("1h")
                    
                    context_path: @env("SERVER_CONTEXT_PATH", "/")
                        .validate(path -> path.startsWith("/"), "Context path must start with /")
                        .cache("1h")
                    
                    ssl: {
                        enabled: @env("SERVER_SSL_ENABLED", "false")
                            .toBoolean()
                            .validate(enabled -> enabled instanceof Boolean, "Invalid SSL enabled value")
                            .cache("1h")
                        
                        key_store: @env("SERVER_SSL_KEY_STORE", "")
                            .validate(keystore -> keystore.length() > 0, "SSL key store required when SSL enabled")
                            .cache("1h")
                        
                        key_store_password: @env("SERVER_SSL_KEY_STORE_PASSWORD", "")
                            .encrypt("AES-256-GCM")
                            .cache("1h")
                    }
                }
                
                database: {
                    primary: {
                        url: @env("DATABASE_URL")
                            .validate(url -> url != null && url.startsWith("jdbc:"), "Invalid database URL")
                            .default("jdbc:postgresql://localhost:5432/tusklang")
                            .cache("1h")
                        
                        username: @env("DB_USERNAME")
                            .validate(username -> username != null, "Database username required")
                            .sanitize_input()
                            .default("postgres")
                            .cache("1h")
                        
                        password: @env("DB_PASSWORD")
                            .validate(password -> password != null, "Database password required")
                            .encrypt("AES-256-GCM")
                            .default("")
                            .cache("1h")
                        
                        driver: @env("DB_DRIVER", "org.postgresql.Driver")
                            .validate(driver -> driver != null, "Database driver required")
                            .cache("1h")
                    }
                    
                    connection_pool: {
                        max_size: @env("DB_MAX_POOL_SIZE", "20")
                            .toInteger()
                            .validate(size -> size > 0 && size <= 100, "Invalid max pool size")
                            .cache("1h")
                        
                        min_size: @env("DB_MIN_POOL_SIZE", "5")
                            .toInteger()
                            .validate(size -> size > 0, "Invalid min pool size")
                            .cache("1h")
                        
                        timeout: @env("DB_CONNECTION_TIMEOUT", "30000")
                            .toInteger()
                            .validate(timeout -> timeout > 0, "Invalid connection timeout")
                            .cache("1h")
                        
                        idle_timeout: @env("DB_IDLE_TIMEOUT", "600000")
                            .toInteger()
                            .validate(timeout -> timeout > 0, "Invalid idle timeout")
                            .cache("1h")
                        
                        max_lifetime: @env("DB_MAX_LIFETIME", "1800000")
                            .toInteger()
                            .validate(lifetime -> lifetime > 0, "Invalid max lifetime")
                            .cache("1h")
                    }
                }
                
                security: {
                    jwt: {
                        secret: @env("JWT_SECRET")
                            .validate(secret -> secret != null && secret.length() >= 32, "JWT secret too short")
                            .encrypt("AES-256-GCM")
                            .cache("1h")
                        
                        expires_in: @env("JWT_EXPIRES_IN", "3600")
                            .toInteger()
                            .validate(expires -> expires > 0 && expires <= 86400, "Invalid JWT expiration")
                            .cache("1h")
                        
                        issuer: @env("JWT_ISSUER", "tusklang")
                            .validate(issuer -> issuer.length() < 100, "JWT issuer too long")
                            .sanitize_input()
                            .cache("1h")
                        
                        audience: @env("JWT_AUDIENCE", "tusklang-users")
                            .validate(audience -> audience.length() < 100, "JWT audience too long")
                            .sanitize_input()
                            .cache("1h")
                    }
                    
                    cors: {
                        allowed_origins: @env("CORS_ALLOWED_ORIGINS", "*")
                            .split(",")
                            .map(origin -> origin.trim())
                            .validate(origins -> origins.length > 0, "At least one CORS origin required")
                            .cache("1h")
                        
                        allowed_methods: @env("CORS_ALLOWED_METHODS", "GET,POST,PUT,DELETE")
                            .split(",")
                            .map(method -> method.trim().toUpperCase())
                            .validate(methods -> methods.length > 0, "At least one CORS method required")
                            .cache("1h")
                        
                        allowed_headers: @env("CORS_ALLOWED_HEADERS", "*")
                            .split(",")
                            .map(header -> header.trim())
                            .validate(headers -> headers.length > 0, "At least one CORS header required")
                            .cache("1h")
                    }
                    
                    rate_limiting: {
                        enabled: @env("RATE_LIMITING_ENABLED", "true")
                            .toBoolean()
                            .validate(enabled -> enabled instanceof Boolean, "Invalid rate limiting enabled value")
                            .cache("1h")
                        
                        default_limit: @env("RATE_LIMIT_DEFAULT", "1000")
                            .toInteger()
                            .validate(limit -> limit > 0 && limit <= 10000, "Invalid default rate limit")
                            .cache("1h")
                        
                        user_limit: @env("RATE_LIMIT_USER", "100")
                            .toInteger()
                            .validate(limit -> limit > 0 && limit <= 1000, "Invalid user rate limit")
                            .cache("1h")
                    }
                }
                
                monitoring: {
                    metrics: {
                        enabled: @env("METRICS_ENABLED", "true")
                            .toBoolean()
                            .validate(enabled -> enabled instanceof Boolean, "Invalid metrics enabled value")
                            .cache("1h")
                        
                        endpoint: @env("METRICS_ENDPOINT", "/actuator/metrics")
                            .validate(endpoint -> endpoint.startsWith("/"), "Invalid metrics endpoint")
                            .cache("1h")
                        
                        export: {
                            prometheus: @env("METRICS_PROMETHEUS_ENABLED", "true")
                                .toBoolean()
                                .validate(enabled -> enabled instanceof Boolean, "Invalid Prometheus enabled value")
                                .cache("1h")
                            
                            influxdb: @env("METRICS_INFLUXDB_ENABLED", "false")
                                .toBoolean()
                                .validate(enabled -> enabled instanceof Boolean, "Invalid InfluxDB enabled value")
                                .cache("1h")
                        }
                    }
                    
                    health: {
                        enabled: @env("HEALTH_ENABLED", "true")
                            .toBoolean()
                            .validate(enabled -> enabled instanceof Boolean, "Invalid health enabled value")
                            .cache("1h")
                        
                        endpoint: @env("HEALTH_ENDPOINT", "/actuator/health")
                            .validate(endpoint -> endpoint.startsWith("/"), "Invalid health endpoint")
                            .cache("1h")
                        
                        details: @env("HEALTH_DETAILS_ENABLED", "true")
                            .toBoolean()
                            .validate(enabled -> enabled instanceof Boolean, "Invalid health details enabled value")
                            .cache("1h")
                    }
                    
                    logging: {
                        level: @env("LOGGING_LEVEL", "INFO")
                            .toUpperCase()
                            .validate(level -> ["DEBUG", "INFO", "WARN", "ERROR"].contains(level), "Invalid logging level")
                            .cache("1h")
                        
                        pattern: @env("LOGGING_PATTERN", "%d{yyyy-MM-dd HH:mm:ss} - %msg%n")
                            .validate(pattern -> pattern != null, "Logging pattern required")
                            .cache("1h")
                        
                        file: {
                            enabled: @env("LOGGING_FILE_ENABLED", "true")
                                .toBoolean()
                                .validate(enabled -> enabled instanceof Boolean, "Invalid file logging enabled value")
                                .cache("1h")
                            
                            path: @env("LOGGING_FILE_PATH", "logs/application.log")
                                .validate(path -> path != null, "Logging file path required")
                                .cache("1h")
                            
                            max_size: @env("LOGGING_FILE_MAX_SIZE", "10MB")
                                .validate(size -> size.matches("^\\d+[KMG]B$"), "Invalid log file max size")
                                .cache("1h")
                            
                            max_history: @env("LOGGING_FILE_MAX_HISTORY", "30")
                                .toInteger()
                                .validate(history -> history > 0, "Invalid log file max history")
                                .cache("1h")
                        }
                    }
                }
            }
            """;
        
        return parser.parse(tskContent);
    }
}

@Component
public class EnvFunctionProperties {
    private boolean enableValidation = true;
    private boolean enableEncryption = true;
    private boolean enableCaching = true;
    private boolean enableSanitization = true;
    private String defaultEncryptionAlgorithm = "AES-256-GCM";
    private int defaultCacheTtl = 3600;
    
    // Getters and setters
    public boolean isEnableValidation() { return enableValidation; }
    public void setEnableValidation(boolean enableValidation) { this.enableValidation = enableValidation; }
    
    public boolean isEnableEncryption() { return enableEncryption; }
    public void setEnableEncryption(boolean enableEncryption) { this.enableEncryption = enableEncryption; }
    
    public boolean isEnableCaching() { return enableCaching; }
    public void setEnableCaching(boolean enableCaching) { this.enableCaching = enableCaching; }
    
    public boolean isEnableSanitization() { return enableSanitization; }
    public void setEnableSanitization(boolean enableSanitization) { this.enableSanitization = enableSanitization; }
    
    public String getDefaultEncryptionAlgorithm() { return defaultEncryptionAlgorithm; }
    public void setDefaultEncryptionAlgorithm(String defaultEncryptionAlgorithm) { this.defaultEncryptionAlgorithm = defaultEncryptionAlgorithm; }
    
    public int getDefaultCacheTtl() { return defaultCacheTtl; }
    public void setDefaultCacheTtl(int defaultCacheTtl) { this.defaultCacheTtl = defaultCacheTtl; }
}
```

### 4. Error Handling and Validation
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.env.TuskEnvFunctionManager;
import org.springframework.stereotype.Service;
import java.util.Map;
import java.util.Optional;

@Service
public class EnvFunctionErrorHandlingService {
    
    private final TuskLang tuskParser;
    
    public EnvFunctionErrorHandlingService(TuskLang tuskParser) {
        this.tuskParser = tuskParser;
    }
    
    public Map<String, Object> processWithErrorHandling() {
        String tskContent = """
            [env_function_error_handling]
            # Error handling with @env functions
            safe_env_access: {
                # Safe environment variable access with fallbacks
                api_key: @env("API_KEY")
                    .validate(key -> key != null && key.length() >= 32, "API key too short")
                    .catch(error -> {
                        log.error("API key error: " + error.getMessage());
                        return @env("FALLBACK_API_KEY", "default_key");
                    })
                
                database_url: @env("DATABASE_URL")
                    .validate(url -> url != null && url.startsWith("jdbc:"), "Invalid database URL")
                    .catch(error -> {
                        log.error("Database URL error: " + error.getMessage());
                        return "jdbc:postgresql://localhost:5432/fallback";
                    })
                
                server_port: @env("SERVER_PORT", "8080")
                    .toInteger()
                    .validate(port -> port > 0 && port < 65536, "Invalid port number")
                    .catch(error -> {
                        log.error("Server port error: " + error.getMessage());
                        return 8080;
                    })
            }
            
            # Conditional error handling
            conditional_error_handling: @env("ENVIRONMENT")
                .equals("production")
                .then({
                    strict_validation: {
                        api_key: @env("PROD_API_KEY")
                            .validate(key -> key != null, "Production API key required")
                            .then(key -> key.encrypt("AES-256-GCM"))
                            .catch(error -> {
                                log.error("Production API key error: " + error.getMessage());
                                throw new RuntimeException("Critical: Production API key missing");
                            })
                    }
                })
                .else({
                    relaxed_validation: {
                        api_key: @env("DEV_API_KEY", "dev_key")
                            .catch(error -> {
                                log.warn("Development API key error: " + error.getMessage());
                                return "default_dev_key";
                            })
                    }
                })
            
            # Environment variable validation with custom error messages
            custom_validation: {
                email: @env("ADMIN_EMAIL")
                    .validate(email -> email != null, "Admin email is required")
                    .validate(email -> /^[^\\s@]+@[^\\s@]+\\.[^\\s@]+$/.test(email), "Invalid email format")
                    .validate(email -> email.length() < 100, "Email too long")
                    .sanitize_input()
                    .toLowerCase()
                    .catch(error -> {
                        log.error("Admin email validation error: " + error.getMessage());
                        return "admin@example.com";
                    })
                
                password: @env("ADMIN_PASSWORD")
                    .validate(password -> password != null, "Admin password is required")
                    .validate(password -> password.length() >= 8, "Password must be at least 8 characters")
                    .validate(password -> /[A-Z]/.test(password), "Password must contain uppercase letter")
                    .validate(password -> /[a-z]/.test(password), "Password must contain lowercase letter")
                    .validate(password -> /[0-9]/.test(password), "Password must contain number")
                    .validate(password -> /[!@#$%^&*]/.test(password), "Password must contain special character")
                    .encrypt("AES-256-GCM")
                    .catch(error -> {
                        log.error("Admin password validation error: " + error.getMessage());
                        throw new SecurityException("Invalid admin password");
                    })
            }
            """;
        
        return tuskParser.parse(tskContent);
    }
    
    public Optional<String> processOptionalEnvFunction(String envKey) {
        String tskContent = """
            [optional_env_function]
            env_value: @env(""" + envKey + """)
                .validate(value -> value != null, "Environment variable not found")
                .then(value -> value.sanitize_input())
                .then(value -> value.toUpperCase())
                .optional()
            """;
        
        Map<String, Object> result = tuskParser.parse(tskContent);
        return Optional.ofNullable((String) result.get("env_value"));
    }
}
```

### 5. Performance Optimization
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.env.TuskEnvFunctionManager;
import org.springframework.cache.annotation.Cacheable;
import org.springframework.stereotype.Service;
import java.util.Map;
import java.util.List;

@Service
public class EnvFunctionPerformanceService {
    
    private final TuskLang tuskParser;
    
    public EnvFunctionPerformanceService(TuskLang tuskParser) {
        this.tuskParser = tuskParser;
    }
    
    @Cacheable("env_function_configurations")
    public Map<String, Object> getOptimizedConfiguration() {
        String tskContent = """
            [optimized_env_functions]
            # Cached environment variable operations
            cached_config: {
                app_name: @env("APP_NAME", "TuskLang App")
                    .cache("1h")
                    .lazy()
                
                app_version: @env("APP_VERSION", "1.0.0")
                    .cache("1h")
                    .lazy()
                
                environment: @env("ENVIRONMENT", "development")
                    .cache("1h")
                    .lazy()
            }
            
            # Lazy evaluation with environment variables
            lazy_config: @env("ENVIRONMENT")
                .equals("production")
                .then({
                    expensive_operation: @env("PROD_API_KEY")
                        .encrypt("AES-256-GCM")
                        .cache("1h")
                        .lazy()
                })
                .else({
                    expensive_operation: @env("DEV_API_KEY", "dev_key")
                        .cache("1h")
                        .lazy()
                })
            
            # Parallel environment variable processing
            parallel_env: @parallel([
                "@env('API_KEY').cache('1h')",
                "@env('DATABASE_URL').cache('1h')",
                "@env('CACHE_URL').cache('1h')"
            ])
            .cache("30m")
            .lazy()
            """;
        
        return tuskParser.parse(tskContent);
    }
}
```

## 🚀 Best Practices

### 1. Environment Variable Organization
```java
// ✅ Good: Clear, structured environment variable access
String goodEnvAccess = """
    app_config: {
        name: @env("APP_NAME", "TuskLang App")
            .validate(name -> name != null, "App name required")
            .sanitize_input()
            .cache("1h")
    }
    """;

// ❌ Bad: Unstructured environment variable access
String badEnvAccess = """
    app_config: {
        name: @env("APP_NAME")
    }
    """;
```

### 2. Validation and Error Handling
```java
// ✅ Good: Comprehensive validation and error handling
String goodValidation = """
    secure_config: {
        api_key: @env("API_KEY")
            .validate(key -> key != null && key.length() >= 32, "API key too short")
            .encrypt("AES-256-GCM")
            .catch(error -> @env("FALLBACK_API_KEY"))
    }
    """;

// ❌ Bad: No validation or error handling
String badValidation = """
    secure_config: {
        api_key: @env("API_KEY")
    }
    """;
```

### 3. Performance Optimization
```java
// ✅ Good: Optimized environment variable access
String goodOptimization = """
    optimized_config: {
        api_key: @env("API_KEY")
            .cache("1h")
            .lazy()
            .encrypt("AES-256-GCM")
    }
    """;

// ❌ Bad: No optimization
String badOptimization = """
    optimized_config: {
        api_key: @env("API_KEY")
    }
    """;
```

## 🔧 Integration Examples

### Spring Boot Configuration
```java
@Configuration
public class EnvFunctionConfiguration {
    
    @Bean
    @ConfigurationProperties(prefix = "tusk.env-functions")
    public EnvFunctionProperties envFunctionProperties() {
        return new EnvFunctionProperties();
    }
    
    @Bean
    public TuskEnvFunctionManager tuskEnvFunctionManager() {
        return new TuskEnvFunctionManager();
    }
}

@Component
public class EnvFunctionProperties {
    private boolean enableValidation = true;
    private boolean enableEncryption = true;
    private boolean enableCaching = true;
    private boolean enableSanitization = true;
    private String defaultEncryptionAlgorithm = "AES-256-GCM";
    private int defaultCacheTtl = 3600;
    
    // Getters and setters
    public boolean isEnableValidation() { return enableValidation; }
    public void setEnableValidation(boolean enableValidation) { this.enableValidation = enableValidation; }
    
    public boolean isEnableEncryption() { return enableEncryption; }
    public void setEnableEncryption(boolean enableEncryption) { this.enableEncryption = enableEncryption; }
    
    public boolean isEnableCaching() { return enableCaching; }
    public void setEnableCaching(boolean enableCaching) { this.enableCaching = enableCaching; }
    
    public boolean isEnableSanitization() { return enableSanitization; }
    public void setEnableSanitization(boolean enableSanitization) { this.enableSanitization = enableSanitization; }
    
    public String getDefaultEncryptionAlgorithm() { return defaultEncryptionAlgorithm; }
    public void setDefaultEncryptionAlgorithm(String defaultEncryptionAlgorithm) { this.defaultEncryptionAlgorithm = defaultEncryptionAlgorithm; }
    
    public int getDefaultCacheTtl() { return defaultCacheTtl; }
    public void setDefaultCacheTtl(int defaultCacheTtl) { this.defaultCacheTtl = defaultCacheTtl; }
}
```

### JPA Integration
```java
@Repository
public class EnvFunctionRepository {
    
    @PersistenceContext
    private EntityManager entityManager;
    
    public List<Map<String, Object>> processWithEnvFunctions(String tskQuery) {
        // Process TuskLang query with environment functions
        TuskLang parser = new TuskLang();
        Map<String, Object> result = parser.parse(tskQuery);
        
        // Execute the query with environment variable substitution
        String sql = (String) result.get("sql");
        List<Object> parameters = (List<Object>) result.get("parameters");
        
        Query query = entityManager.createNativeQuery(sql);
        for (int i = 0; i < parameters.size(); i++) {
            query.setParameter(i + 1, parameters.get(i));
        }
        
        return query.getResultList();
    }
}
```

## 📊 Performance Metrics

### Environment Function Performance Comparison
```java
@Service
public class EnvFunctionPerformanceComparisonService {
    
    public void benchmarkEnvFunctions() {
        // Simple environment access: ~1ms
        String simpleEnv = "@env('API_KEY')";
        
        // Validated environment access: ~3ms
        String validatedEnv = """
            @env("API_KEY")
                .validate(key -> key != null, "API key required")
                .sanitize_input()
            """;
        
        // Encrypted environment access: ~8ms
        String encryptedEnv = """
            @env("API_KEY")
                .validate(key -> key != null, "API key required")
                .encrypt("AES-256-GCM")
                .cache("1h")
            """;
        
        // Cached environment access: ~2ms
        String cachedEnv = """
            @env("API_KEY")
                .cache("1h")
                .lazy()
            """;
    }
}
```

## 🔒 Security Considerations

### Secure Environment Function Usage
```java
@Service
public class SecureEnvFunctionService {
    
    public Map<String, Object> processSecureEnvFunctions() {
        String tskContent = """
            [secure_env_functions]
            # Secure environment variable handling
            secure_handling: {
                # Secure environment variable access
                api_key: @env.secure("API_KEY")
                    .validate(key -> key != null && key.length() >= 32, "API key too short")
                    .encrypt("AES-256-GCM")
                    .catch(error -> {
                        log.error("Security error: " + error.getMessage());
                        throw new SecurityException("Invalid API key");
                    })
                
                # Input sanitization
                user_input: @env("USER_INPUT")
                    .sanitize_input()
                    .validate(input -> input.length() < 1000, "Input too long")
                    .validate(input -> !/<script>/.test(input), "Script tags not allowed")
                    .escape_html()
                
                # Secure database configuration
                database_config: {
                    url: @env.secure("DATABASE_URL")
                        .validate(url -> url != null && url.startsWith("jdbc:"), "Invalid database URL")
                        .catch(error -> {
                            log.error("Database URL error: " + error.getMessage());
                            return "jdbc:postgresql://localhost:5432/fallback";
                        })
                    
                    credentials: {
                        username: @env.secure("DB_USERNAME")
                            .validate(username -> username != null, "Database username required")
                            .sanitize_input()
                            .catch(error -> {
                                log.error("Database username error: " + error.getMessage());
                                return "postgres";
                            })
                        
                        password: @env.secure("DB_PASSWORD")
                            .validate(password -> password != null, "Database password required")
                            .encrypt("AES-256-GCM")
                            .catch(error -> {
                                log.error("Database password error: " + error.getMessage());
                                return "";
                            })
                    }
                }
            }
            """;
        
        return tuskParser.parse(tskContent);
    }
}
```

## 🎯 Summary

@env function in TuskLang Java provides:

- **Environment Variable Access**: Secure and flexible environment variable handling
- **Validation**: Comprehensive validation with custom error messages
- **Transformation**: Type conversion and data transformation
- **Encryption**: Built-in encryption for sensitive data
- **Caching**: Performance optimization with caching strategies
- **Spring Boot Integration**: Seamless integration with Spring applications
- **Error Handling**: Robust error handling and fallback mechanisms
- **Security**: Input sanitization and security validation

Master @env function to create secure, robust environment variable management that adapts to your application's needs while maintaining enterprise-grade performance and security. 