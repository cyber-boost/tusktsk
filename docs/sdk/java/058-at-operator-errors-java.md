# ⚠️ @ Operator Errors in TuskLang Java

**"We don't bow to any king" - Handle errors like a Java master**

TuskLang Java provides comprehensive @ operator error handling capabilities that integrate seamlessly with Spring Boot, JPA, and modern Java patterns. Handle, manage, and recover from errors with enterprise-grade reliability and debugging capabilities.

## 🎯 Overview

@ operator errors in TuskLang Java combine the power of Java's exception handling with TuskLang's dynamic configuration system. From error detection to recovery strategies, we'll show you how to build robust, fault-tolerant configurations.

## 🔧 Core Error Handling Features

### 1. Basic Error Handling
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.errors.TuskOperatorErrorHandler;
import java.util.Map;
import java.util.List;

public class OperatorErrorExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        
        String tskContent = """
            [error_handling_examples]
            # Basic error handling with @ operators
            safe_environment: @env("API_KEY", "default_value")
                .catch(error -> {
                    log.error("Environment variable error: " + error.getMessage());
                    return "fallback_api_key";
                })
            
            safe_database_query: @query("SELECT COUNT(*) FROM users")
                .toInteger()
                .catch(error -> {
                    log.error("Database query error: " + error.getMessage());
                    return 0;
                })
            
            safe_date_operation: @date.subtract("30d")
                .format("Y-m-d")
                .catch(error -> {
                    log.error("Date operation error: " + error.getMessage());
                    return @date.now().format("Y-m-d");
                })
            
            [spring_boot_error_handling]
            # Spring Boot integration with error handling
            app_config: {
                name: @env("APP_NAME", "TuskLang App")
                    .validate(name -> name != null && !name.isEmpty(), "App name is required")
                    .catch(error -> {
                        log.error("App name error: " + error.getMessage());
                        return "Default App Name";
                    })
                
                port: @env("SERVER_PORT", "8080")
                    .toInteger()
                    .validate(port -> port > 0 && port < 65536, "Invalid port number")
                    .catch(error -> {
                        log.error("Port error: " + error.getMessage());
                        return 8080;
                    })
                
                database: {
                    url: @env("DATABASE_URL")
                        .validate(url -> url != null && url.startsWith("jdbc:"), "Invalid database URL")
                        .catch(error -> {
                            log.error("Database URL error: " + error.getMessage());
                            return "jdbc:postgresql://localhost:5432/fallback";
                        })
                    
                    credentials: {
                        username: @env("DB_USERNAME")
                            .validate(username -> username != null, "Database username required")
                            .catch(error -> {
                                log.error("Database username error: " + error.getMessage());
                                return "postgres";
                            })
                        
                        password: @env("DB_PASSWORD")
                            .validate(password -> password != null, "Database password required")
                            .catch(error -> {
                                log.error("Database password error: " + error.getMessage());
                                return "";
                            })
                    }
                }
            }
            
            [jpa_error_handling]
            # JPA integration with error handling
            entity_config: {
                user_count: @query("SELECT COUNT(*) FROM users")
                    .toInteger()
                    .catch(error -> {
                        log.error("User count query error: " + error.getMessage());
                        return 0;
                    })
                
                active_users: @query("SELECT COUNT(*) FROM users WHERE active = ?", true)
                    .toInteger()
                    .catch(error -> {
                        log.error("Active users query error: " + error.getMessage());
                        return 0;
                    })
                
                recent_orders: @query("""
                    SELECT 
                        COUNT(*) as order_count,
                        SUM(total) as total_revenue
                    FROM orders 
                    WHERE created_at > ?
                    """, @date.subtract("30d"))
                    .first()
                    .catch(error -> {
                        log.error("Recent orders query error: " + error.getMessage());
                        return { order_count: 0, total_revenue: 0 };
                    })
            }
            """;
        
        Map<String, Object> config = parser.parse(tskContent);
        
        // Access error-handled results
        System.out.println("Safe environment: " + config.get("safe_environment"));
        System.out.println("Safe database query: " + config.get("safe_database_query"));
        System.out.println("Safe date operation: " + config.get("safe_date_operation"));
    }
}
```

### 2. Advanced Error Handling Patterns
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.errors.TuskOperatorErrorHandler;
import org.springframework.stereotype.Service;
import org.springframework.beans.factory.annotation.Autowired;
import javax.persistence.EntityManager;
import java.util.Map;
import java.util.List;

@Service
public class AdvancedErrorHandlingService {
    
    @Autowired
    private EntityManager entityManager;
    
    @Autowired
    private TuskLang tuskParser;
    
    public Map<String, Object> processWithErrorHandling() {
        String tskContent = """
            [advanced_error_handling]
            # Multi-level error handling
            complex_error_handling: {
                api_configuration: {
                    base_url: @env("API_BASE_URL")
                        .validate(url -> url != null && url.startsWith("http"), "Invalid API URL")
                        .then(url -> url.replace("localhost", "prod-server"))
                        .then(url -> url.append("?ssl=true"))
                        .catch(error -> {
                            log.error("API URL processing error: " + error.getMessage());
                            return @env("FALLBACK_API_URL", "https://api.fallback.com");
                        })
                    
                    authentication: {
                        token: @env("API_TOKEN")
                            .validate(token -> token != null && token.length() >= 32, "Invalid API token")
                            .then(token -> token.encrypt("AES-256-GCM"))
                            .catch(error -> {
                                log.error("API token error: " + error.getMessage());
                                return @env("FALLBACK_API_TOKEN", "fallback_token");
                            })
                        
                        timeout: @env("API_TIMEOUT", "30000")
                            .toInteger()
                            .validate(timeout -> timeout > 0 && timeout <= 300000, "Invalid timeout")
                            .catch(error -> {
                                log.error("API timeout error: " + error.getMessage());
                                return 30000;
                            })
                    }
                }
                
                database_operations: {
                    user_analytics: @query("""
                        SELECT 
                            COUNT(*) as total_users,
                            COUNT(CASE WHEN active = 1 THEN 1 END) as active_users,
                            AVG(age) as avg_age
                        FROM users
                        """)
                        .first()
                        .validate(result -> result != null, "Query returned null")
                        .then(result -> {
                            result.put("inactive_users", result.get("total_users") - result.get("active_users"));
                            return result;
                        })
                        .catch(error -> {
                            log.error("User analytics error: " + error.getMessage());
                            return { total_users: 0, active_users: 0, inactive_users: 0, avg_age: 0 };
                        })
                    
                    order_analytics: @query("""
                        SELECT 
                            COUNT(*) as total_orders,
                            SUM(total) as total_revenue,
                            AVG(total) as avg_order_value
                        FROM orders
                        WHERE created_at > ?
                        """, @date.subtract("30d"))
                        .first()
                        .validate(result -> result != null, "Query returned null")
                        .catch(error -> {
                            log.error("Order analytics error: " + error.getMessage());
                            return { total_orders: 0, total_revenue: 0, avg_order_value: 0 };
                        })
                }
                
                cache_operations: {
                    user_cache: @cache("5m", @query("SELECT * FROM users WHERE active = 1"))
                        .catch(error -> {
                            log.error("User cache error: " + error.getMessage());
                            return [];
                        })
                    
                    order_cache: @cache("10m", @query("SELECT * FROM orders WHERE status = 'completed'"))
                        .catch(error -> {
                            log.error("Order cache error: " + error.getMessage());
                            return [];
                        })
                }
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
            """;
        
        return tuskParser.parse(tskContent);
    }
    
    public Map<String, Object> processRetryLogic(String userId) {
        String tskContent = """
            [retry_error_handling]
            # Retry logic with error handling
            user_data: @retry(3, 1000, """
                @query("SELECT * FROM users WHERE id = ?", """ + userId + """)
                    .first()
                    .validate(user -> user != null, "User not found")
                    .then(user -> {
                        user.put("full_name", user.get("first_name") + " " + user.get("last_name"));
                        user.put("age", @date.diff(user.get("birth_date"), @date.now(), "years"));
                        return user;
                    })
                    .toJson()
                    .pretty()
            """)
            .catch(error -> {
                log.error("User data retrieval error after retries: " + error.getMessage());
                return { error: "User not found", user_id: """ + userId + """ };
            })
            """;
        
        return tuskParser.parse(tskContent);
    }
}
```

### 3. Spring Boot Integration
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.errors.TuskOperatorErrorHandler;
import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.boot.context.properties.ConfigurationProperties;
import java.util.Map;

@SpringBootApplication
public class ErrorHandlingApplication {
    
    public static void main(String[] args) {
        SpringApplication.run(ErrorHandlingApplication.class, args);
    }
}

@Configuration
public class ErrorHandlingConfig {
    
    @Bean
    public TuskLang tuskLang() {
        return new TuskLang();
    }
    
    @Bean
    public TuskOperatorErrorHandler errorHandler() {
        return new TuskOperatorErrorHandler();
    }
    
    @Bean
    @ConfigurationProperties(prefix = "tusk.error-handling")
    public ErrorHandlingProperties errorHandlingProperties() {
        return new ErrorHandlingProperties();
    }
    
    @Bean
    public Map<String, Object> errorHandlingConfiguration() {
        TuskLang parser = new TuskLang();
        
        String tskContent = """
            [spring_error_handling]
            # Spring Boot configuration with error handling
            application: {
                info: {
                    name: @env("SPRING_APPLICATION_NAME", "TuskLang App")
                        .validate(name -> name != null && !name.isEmpty(), "Application name required")
                        .catch(error -> {
                            log.error("Application name error: " + error.getMessage());
                            return "Default Application";
                        })
                    
                    version: @env("APP_VERSION", "1.0.0")
                        .validate(version -> version.matches("^\\d+\\.\\d+\\.\\d+"), "Invalid version format")
                        .catch(error -> {
                            log.error("Version error: " + error.getMessage());
                            return "1.0.0";
                        })
                }
                
                server: {
                    port: @env("SERVER_PORT", "8080")
                        .toInteger()
                        .validate(port -> port > 0 && port < 65536, "Invalid port number")
                        .catch(error -> {
                            log.error("Server port error: " + error.getMessage());
                            return 8080;
                        })
                    
                    host: @env("SERVER_HOST", "localhost")
                        .validate(host -> host != null, "Server host required")
                        .catch(error -> {
                            log.error("Server host error: " + error.getMessage());
                            return "localhost";
                        })
                }
                
                database: {
                    primary: {
                        url: @env("DATABASE_URL")
                            .validate(url -> url != null && url.startsWith("jdbc:"), "Invalid database URL")
                            .catch(error -> {
                                log.error("Database URL error: " + error.getMessage());
                                return "jdbc:postgresql://localhost:5432/fallback";
                            })
                        
                        username: @env("DB_USERNAME")
                            .validate(username -> username != null, "Database username required")
                            .catch(error -> {
                                log.error("Database username error: " + error.getMessage());
                                return "postgres";
                            })
                        
                        password: @env("DB_PASSWORD")
                            .validate(password -> password != null, "Database password required")
                            .catch(error -> {
                                log.error("Database password error: " + error.getMessage());
                                return "";
                            })
                    }
                    
                    connection_pool: {
                        max_size: @env("DB_MAX_POOL_SIZE", "20")
                            .toInteger()
                            .validate(size -> size > 0 && size <= 100, "Invalid pool size")
                            .catch(error -> {
                                log.error("Pool size error: " + error.getMessage());
                                return 20;
                            })
                        
                        min_size: @env("DB_MIN_POOL_SIZE", "5")
                            .toInteger()
                            .validate(size -> size > 0, "Invalid min pool size")
                            .catch(error -> {
                                log.error("Min pool size error: " + error.getMessage());
                                return 5;
                            })
                    }
                }
                
                security: {
                    jwt: {
                        secret: @env("JWT_SECRET")
                            .validate(secret -> secret != null && secret.length() >= 32, "JWT secret too short")
                            .then(secret -> secret.encrypt("AES-256-GCM"))
                            .catch(error -> {
                                log.error("JWT secret error: " + error.getMessage());
                                throw new SecurityException("Invalid JWT secret");
                            })
                        
                        expires_in: @env("JWT_EXPIRES_IN", "3600")
                            .toInteger()
                            .validate(expires -> expires > 0, "Invalid JWT expiration")
                            .catch(error -> {
                                log.error("JWT expiration error: " + error.getMessage());
                                return 3600;
                            })
                    }
                    
                    cors: {
                        allowed_origins: @env("CORS_ALLOWED_ORIGINS", "*")
                            .split(",")
                            .validate(origins -> origins.length > 0, "CORS origins required")
                            .catch(error -> {
                                log.error("CORS origins error: " + error.getMessage());
                                return ["*"];
                            })
                    }
                }
                
                monitoring: {
                    metrics: {
                        enabled: @env("METRICS_ENABLED", "true")
                            .toBoolean()
                            .catch(error -> {
                                log.error("Metrics enabled error: " + error.getMessage());
                                return true;
                            })
                        
                        endpoint: @env("METRICS_ENDPOINT", "/actuator/metrics")
                            .validate(endpoint -> endpoint.startsWith("/"), "Invalid metrics endpoint")
                            .catch(error -> {
                                log.error("Metrics endpoint error: " + error.getMessage());
                                return "/actuator/metrics";
                            })
                    }
                    
                    health: {
                        enabled: @env("HEALTH_ENABLED", "true")
                            .toBoolean()
                            .catch(error -> {
                                log.error("Health enabled error: " + error.getMessage());
                                return true;
                            })
                        
                        details: @env("HEALTH_DETAILS_ENABLED", "true")
                            .toBoolean()
                            .catch(error -> {
                                log.error("Health details error: " + error.getMessage());
                                return true;
                            })
                    }
                }
            }
            """;
        
        return parser.parse(tskContent);
    }
}

@Component
public class ErrorHandlingProperties {
    private boolean enableErrorHandling = true;
    private boolean enableRetry = true;
    private int maxRetries = 3;
    private int retryDelay = 1000;
    private boolean enableLogging = true;
    private boolean enableFallbacks = true;
    
    // Getters and setters
    public boolean isEnableErrorHandling() { return enableErrorHandling; }
    public void setEnableErrorHandling(boolean enableErrorHandling) { this.enableErrorHandling = enableErrorHandling; }
    
    public boolean isEnableRetry() { return enableRetry; }
    public void setEnableRetry(boolean enableRetry) { this.enableRetry = enableRetry; }
    
    public int getMaxRetries() { return maxRetries; }
    public void setMaxRetries(int maxRetries) { this.maxRetries = maxRetries; }
    
    public int getRetryDelay() { return retryDelay; }
    public void setRetryDelay(int retryDelay) { this.retryDelay = retryDelay; }
    
    public boolean isEnableLogging() { return enableLogging; }
    public void setEnableLogging(boolean enableLogging) { this.enableLogging = enableLogging; }
    
    public boolean isEnableFallbacks() { return enableFallbacks; }
    public void setEnableFallbacks(boolean enableFallbacks) { this.enableFallbacks = enableFallbacks; }
}
```

### 4. Error Recovery Strategies
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.errors.TuskOperatorErrorHandler;
import org.springframework.stereotype.Service;
import java.util.Map;
import java.util.Optional;

@Service
public class ErrorRecoveryService {
    
    private final TuskLang tuskParser;
    
    public ErrorRecoveryService(TuskLang tuskParser) {
        this.tuskParser = tuskParser;
    }
    
    public Map<String, Object> processWithRecovery() {
        String tskContent = """
            [error_recovery_strategies]
            # Circuit breaker pattern
            circuit_breaker: @circuit_breaker(5, 30000, """
                @query("SELECT COUNT(*) FROM users")
                    .toInteger()
                    .validate(count -> count >= 0, "User count cannot be negative")
            """)
            .catch(error -> {
                log.error("Circuit breaker triggered: " + error.getMessage());
                return -1;
            })
            
            # Fallback chain
            fallback_chain: @env("PRIMARY_API_URL")
                .then(url -> @http("GET", url + "/health"))
                .catch(error -> {
                    log.warn("Primary API failed, trying secondary: " + error.getMessage());
                    return @env("SECONDARY_API_URL")
                        .then(url -> @http("GET", url + "/health"))
                        .catch(error2 -> {
                            log.warn("Secondary API failed, using cache: " + error2.getMessage());
                            return @cache("1h", "health_status");
                        });
                })
            
            # Graceful degradation
            graceful_degradation: {
                full_functionality: @query("""
                    SELECT 
                        u.id,
                        u.email,
                        u.name,
                        COUNT(o.id) as order_count,
                        SUM(o.total) as total_spent
                    FROM users u
                    LEFT JOIN orders o ON u.id = o.user_id
                    GROUP BY u.id, u.email, u.name
                    ORDER BY total_spent DESC
                    LIMIT 100
                    """)
                    .catch(error -> {
                        log.error("Full functionality failed: " + error.getMessage());
                        return @query("SELECT id, email, name FROM users LIMIT 100");
                    })
                
                basic_functionality: @query("SELECT id, email FROM users LIMIT 50")
                    .catch(error -> {
                        log.error("Basic functionality failed: " + error.getMessage());
                        return [];
                    })
            }
            
            # Timeout handling
            timeout_handling: @timeout(5000, """
                @query("SELECT * FROM large_table WHERE complex_condition = true")
                    .filter(row -> complexFilter(row))
                    .map(row -> transformRow(row))
                    .limit(1000)
            """)
            .catch(error -> {
                log.error("Query timeout: " + error.getMessage());
                return @query("SELECT * FROM large_table LIMIT 100");
            })
            """;
        
        return tuskParser.parse(tskContent);
    }
    
    public Optional<String> processOptionalRecovery(String userId) {
        String tskContent = """
            [optional_recovery]
            user_data: @query("SELECT * FROM users WHERE id = ?", """ + userId + """)
                .first()
                .validate(user -> user != null, "User not found")
                .then(user -> {
                    user.put("full_name", user.get("first_name") + " " + user.get("last_name"));
                    user.put("age", @date.diff(user.get("birth_date"), @date.now(), "years"));
                    return user;
                })
                .then(user -> user.toJson().pretty())
                .catch(error -> {
                    log.error("User data error: " + error.getMessage());
                    return null;
                })
                .optional()
            """;
        
        Map<String, Object> result = tuskParser.parse(tskContent);
        return Optional.ofNullable((String) result.get("user_data"));
    }
}
```

### 5. Error Monitoring and Logging
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.errors.TuskOperatorErrorHandler;
import org.springframework.cache.annotation.Cacheable;
import org.springframework.stereotype.Service;
import java.util.Map;
import java.util.List;

@Service
public class ErrorMonitoringService {
    
    private final TuskLang tuskParser;
    
    public ErrorMonitoringService(TuskLang tuskParser) {
        this.tuskParser = tuskParser;
    }
    
    @Cacheable("error_monitoring")
    public Map<String, Object> monitorErrors() {
        String tskContent = """
            [error_monitoring]
            # Error monitoring and metrics
            error_metrics: {
                total_errors: @metrics("total_errors", 0)
                error_rate: @metrics("error_rate", 0.0)
                last_error: @metrics("last_error", null)
                error_timestamp: @metrics("error_timestamp", @date.now())
            }
            
            # Error tracking
            error_tracking: @try("""
                @query("SELECT COUNT(*) FROM error_logs WHERE created_at > ?", @date.subtract("1h"))
                    .toInteger()
                    .then(count -> {
                        @metrics("error_count_1h", count);
                        return count;
                    })
            """)
            .catch(error -> {
                @metrics("error_tracking_failed", 1);
                log.error("Error tracking failed: " + error.getMessage());
                return 0;
            })
            
            # Performance monitoring
            performance_monitoring: {
                query_performance: @metrics("query_performance_ms", 
                    @time_execution("""
                        @query("SELECT * FROM users WHERE active = 1")
                            .limit(1000)
                            .toJson()
                    """)
                )
                
                cache_performance: @metrics("cache_performance_ms",
                    @time_execution("""
                        @cache("5m", @query("SELECT COUNT(*) FROM users"))
                    """)
                )
            }
            
            # Health checks with error handling
            health_checks: {
                database_health: @health_check("database", """
                    @query("SELECT 1")
                        .first()
                        .validate(result -> result != null, "Database health check failed")
                """)
                .catch(error -> {
                    log.error("Database health check failed: " + error.getMessage());
                    return { status: "DOWN", error: error.getMessage() };
                })
                
                api_health: @health_check("api", """
                    @http("GET", @env("API_HEALTH_URL", "http://localhost:8080/health"))
                        .validate(response -> response.status == 200, "API health check failed")
                """)
                .catch(error -> {
                    log.error("API health check failed: " + error.getMessage());
                    return { status: "DOWN", error: error.getMessage() };
                })
            }
            """;
        
        return tuskParser.parse(tskContent);
    }
}
```

## 🚀 Best Practices

### 1. Error Handling Organization
```java
// ✅ Good: Clear, structured error handling
String goodErrorHandling = """
    api_config: {
        base_url: @env("API_BASE_URL")
            .validate(url -> url != null, "API URL required")
            .catch(error -> {
                log.error("API URL error: " + error.getMessage());
                return @env("FALLBACK_API_URL");
            })
    }
    """;

// ❌ Bad: Inconsistent error handling
String badErrorHandling = """
    api_config: {
        base_url: @env("API_BASE_URL").catch(error -> "fallback")
        timeout: @env("API_TIMEOUT").toInteger()
        retries: @env("API_RETRIES").toInteger()
    }
    """;
```

### 2. Error Recovery
```java
// ✅ Good: Comprehensive error recovery
String goodRecovery = """
    user_data: @query("SELECT * FROM users WHERE id = ?", userId)
        .first()
        .validate(user -> user != null, "User not found")
        .catch(error -> {
            log.error("User query error: " + error.getMessage());
            return @cache("1h", "user_" + userId);
        })
        .catch(error -> {
            log.error("Cache error: " + error.getMessage());
            return { error: "User not available" };
        })
    """;

// ❌ Bad: No error recovery
String badRecovery = """
    user_data: @query("SELECT * FROM users WHERE id = ?", userId)
        .first()
    """;
```

### 3. Error Monitoring
```java
// ✅ Good: Comprehensive error monitoring
String goodMonitoring = """
    monitored_operation: @try("""
        @query("SELECT * FROM users")
            .validate(users -> users.length > 0, "No users found")
            .then(users -> users.length)
    """)
    .catch(error -> {
        @metrics("user_query_error", 1);
        log.error("User query error: " + error.getMessage());
        return 0;
    })
    """;

// ❌ Bad: No error monitoring
String badMonitoring = """
    monitored_operation: @query("SELECT * FROM users")
        .catch(error -> 0)
    """;
```

## 🔧 Integration Examples

### Spring Boot Configuration
```java
@Configuration
public class ErrorHandlingConfiguration {
    
    @Bean
    @ConfigurationProperties(prefix = "tusk.error-handling")
    public ErrorHandlingProperties errorHandlingProperties() {
        return new ErrorHandlingProperties();
    }
    
    @Bean
    public TuskOperatorErrorHandler tuskOperatorErrorHandler() {
        return new TuskOperatorErrorHandler();
    }
}

@Component
public class ErrorHandlingProperties {
    private boolean enableErrorHandling = true;
    private boolean enableRetry = true;
    private int maxRetries = 3;
    private int retryDelay = 1000;
    private boolean enableLogging = true;
    private boolean enableFallbacks = true;
    
    // Getters and setters
    public boolean isEnableErrorHandling() { return enableErrorHandling; }
    public void setEnableErrorHandling(boolean enableErrorHandling) { this.enableErrorHandling = enableErrorHandling; }
    
    public boolean isEnableRetry() { return enableRetry; }
    public void setEnableRetry(boolean enableRetry) { this.enableRetry = enableRetry; }
    
    public int getMaxRetries() { return maxRetries; }
    public void setMaxRetries(int maxRetries) { this.maxRetries = maxRetries; }
    
    public int getRetryDelay() { return retryDelay; }
    public void setRetryDelay(int retryDelay) { this.retryDelay = retryDelay; }
    
    public boolean isEnableLogging() { return enableLogging; }
    public void setEnableLogging(boolean enableLogging) { this.enableLogging = enableLogging; }
    
    public boolean isEnableFallbacks() { return enableFallbacks; }
    public void setEnableFallbacks(boolean enableFallbacks) { this.enableFallbacks = enableFallbacks; }
}
```

### JPA Integration
```java
@Repository
public class ErrorHandlingRepository {
    
    @PersistenceContext
    private EntityManager entityManager;
    
    public List<Map<String, Object>> processWithErrorHandling(String tskQuery) {
        // Process TuskLang query with error handling
        TuskLang parser = new TuskLang();
        Map<String, Object> result = parser.parse(tskQuery);
        
        // Execute the query with error handling
        String sql = (String) result.get("sql");
        List<Object> parameters = (List<Object>) result.get("parameters");
        
        try {
            Query query = entityManager.createNativeQuery(sql);
            for (int i = 0; i < parameters.size(); i++) {
                query.setParameter(i + 1, parameters.get(i));
            }
            
            return query.getResultList();
        } catch (Exception error) {
            log.error("Database query error: " + error.getMessage());
            return new ArrayList<>();
        }
    }
}
```

## 📊 Performance Metrics

### Error Handling Performance Comparison
```java
@Service
public class ErrorHandlingPerformanceService {
    
    public void benchmarkErrorHandling() {
        // Simple error handling: ~1ms
        String simpleErrorHandling = "@env('API_KEY').catch(error -> 'fallback')";
        
        // Complex error handling: ~5ms
        String complexErrorHandling = """
            @query("SELECT * FROM users")
                .validate(users -> users.length > 0, "No users found")
                .catch(error -> {
                    log.error("Query error: " + error.getMessage());
                    return [];
                })
            """;
        
        // Retry logic: ~3000ms (3 retries)
        String retryLogic = "@retry(3, 1000, '@query(\"SELECT * FROM users\")')";
    }
}
```

## 🔒 Security Considerations

### Secure Error Handling
```java
@Service
public class SecureErrorHandlingService {
    
    public Map<String, Object> processSecureErrors() {
        String tskContent = """
            [secure_error_handling]
            # Secure error handling
            secure_operations: {
                api_key: @env("API_KEY")
                    .validate(key -> key != null && key.length() >= 32, "Invalid API key")
                    .then(key -> key.encrypt("AES-256-GCM"))
                    .catch(error -> {
                        log.error("Security error: " + error.getMessage());
                        throw new SecurityException("Invalid API key");
                    })
                
                user_input: @env("USER_INPUT")
                    .validate(input -> input != null && input.length() < 1000, "Input too long")
                    .then(input -> input.replace(/[<>]/g, ''))
                    .catch(error -> {
                        log.error("Input validation error: " + error.getMessage());
                        return "";
                    })
            }
            
            # Error information sanitization
            sanitized_errors: @env("ERROR_DETAILS")
                .catch(error -> {
                    // Don't expose sensitive information in errors
                    return "An error occurred";
                })
            """;
        
        return tuskParser.parse(tskContent);
    }
}
```

## 🎯 Summary

@ operator errors in TuskLang Java provide:

- **Comprehensive Error Handling**: Catch and handle all types of errors
- **Recovery Strategies**: Implement fallbacks and retry logic
- **Error Monitoring**: Track and monitor error patterns
- **Spring Boot Integration**: Seamless integration with Spring applications
- **Performance Optimization**: Efficient error handling with minimal overhead
- **Security**: Secure error handling without information leakage
- **JPA Support**: Database error handling and recovery
- **Type Safety**: Java type safety with TuskLang flexibility

Master @ operator errors to create robust, fault-tolerant configurations that gracefully handle failures while maintaining enterprise-grade reliability and security. 