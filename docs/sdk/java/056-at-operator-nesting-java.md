# 🪆 @ Operator Nesting in TuskLang Java

**"We don't bow to any king" - Nest operators like a Java architect**

TuskLang Java provides sophisticated @ operator nesting capabilities that integrate seamlessly with Spring Boot, JPA, and modern Java patterns. Create complex, nested configurations with enterprise-grade performance and maintainability.

## 🎯 Overview

@ operator nesting in TuskLang Java combines the power of Java's object-oriented design with TuskLang's dynamic configuration system. From nested environment variables to complex database queries, we'll show you how to build sophisticated nested configurations.

## 🔧 Core Nesting Features

### 1. Basic Operator Nesting
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.operators.TuskOperatorNesting;
import java.util.Map;
import java.util.List;

public class OperatorNestingExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        
        String tskContent = """
            [nesting_examples]
            # Basic nested operators
            simple_nesting: @env(@env("CONFIG_KEY", "API_KEY"), "default_value")
            
            # Nested date operations
            date_nesting: @date(@date.now().subtract("7d").format("Y-m-d"))
            
            # Nested database queries
            query_nesting: @query("SELECT * FROM users WHERE created_at > ?", 
                @date.subtract(@env("DAYS_BACK", "30").toInteger() + "d"))
            
            # Nested environment variables
            env_nesting: @env(@env("ENV_TYPE", "PROD") + "_API_URL", 
                @env("DEFAULT_API_URL", "http://localhost:3000"))
            
            [spring_boot_nesting]
            # Spring Boot integration with nesting
            app_config: {
                name: @env("APP_NAME", "TuskLang App")
                version: @env("APP_VERSION", "1.0.0")
                environment: @env("SPRING_PROFILES_ACTIVE", "dev")
                debug: @env("DEBUG_MODE", "false").toBoolean()
                
                database: {
                    url: @env(@env("DB_TYPE", "postgresql") + "_URL",
                        "jdbc:postgresql://localhost:5432/tusklang")
                    username: @env(@env("DB_TYPE", "postgresql") + "_USER", "postgres")
                    password: @env(@env("DB_TYPE", "postgresql") + "_PASSWORD", "")
                }
                
                api: {
                    base_url: @env(@env("ENVIRONMENT", "dev") + "_API_BASE_URL",
                        "https://api.tusklang.org")
                    timeout: @env(@env("ENVIRONMENT", "dev") + "_API_TIMEOUT", "30000").toInteger()
                    retries: @env(@env("ENVIRONMENT", "dev") + "_API_RETRIES", "3").toInteger()
                }
            }
            
            [jpa_nesting]
            # JPA integration with nested operators
            entity_config: {
                user_stats: {
                    total: @query("SELECT COUNT(*) FROM users")
                    active: @query("SELECT COUNT(*) FROM users WHERE active = ?", true)
                    recent: @query("SELECT COUNT(*) FROM users WHERE created_at > ?", 
                        @date.subtract(@env("RECENT_DAYS", "7").toInteger() + "d"))
                    premium: @query("SELECT COUNT(*) FROM users WHERE tier = ?", 
                        @env("PREMIUM_TIER", "premium"))
                }
                
                order_analytics: {
                    total_orders: @query("SELECT COUNT(*) FROM orders")
                    total_revenue: @query("SELECT SUM(total) FROM orders WHERE status = ?", 
                        @env("COMPLETED_STATUS", "completed"))
                    avg_order_value: @query("SELECT AVG(total) FROM orders WHERE created_at > ?", 
                        @date.subtract(@env("ANALYTICS_DAYS", "30").toInteger() + "d"))
                }
            }
            """;
        
        Map<String, Object> config = parser.parse(tskContent);
        
        // Access nested results
        System.out.println("Simple nesting: " + config.get("simple_nesting"));
        System.out.println("Date nesting: " + config.get("date_nesting"));
        System.out.println("Query nesting: " + config.get("query_nesting"));
    }
}
```

### 2. Advanced Nesting Patterns
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.operators.TuskOperatorNesting;
import org.springframework.stereotype.Service;
import org.springframework.beans.factory.annotation.Autowired;
import javax.persistence.EntityManager;
import java.util.Map;
import java.util.List;

@Service
public class AdvancedNestingService {
    
    @Autowired
    private EntityManager entityManager;
    
    @Autowired
    private TuskLang tuskParser;
    
    public Map<String, Object> processNestedOperations() {
        String tskContent = """
            [advanced_nesting]
            # Multi-level nested operators
            complex_nesting: {
                user_analytics: {
                    demographics: {
                        age_groups: @query("""
                            SELECT 
                                CASE 
                                    WHEN age < 25 THEN '18-24'
                                    WHEN age < 35 THEN '25-34'
                                    WHEN age < 45 THEN '35-44'
                                    ELSE '45+'
                                END as age_group,
                                COUNT(*) as count
                            FROM users 
                            WHERE created_at > ?
                            GROUP BY age_group
                            ORDER BY count DESC
                            """, @date.subtract(@env("ANALYTICS_PERIOD", "90").toInteger() + "d"))
                        
                        locations: @query("""
                            SELECT 
                                country,
                                COUNT(*) as user_count,
                                AVG(age) as avg_age
                            FROM users 
                            WHERE country IS NOT NULL
                            GROUP BY country
                            HAVING COUNT(*) > ?
                            ORDER BY user_count DESC
                            LIMIT ?
                            """, @env("MIN_USERS_PER_COUNTRY", "10").toInteger(),
                                @env("TOP_COUNTRIES_LIMIT", "20").toInteger())
                    }
                    
                    behavior: {
                        active_users: @query("""
                            SELECT 
                                DATE(created_at) as date,
                                COUNT(*) as new_users,
                                COUNT(CASE WHEN last_login > ? THEN 1 END) as active_users
                            FROM users 
                            WHERE created_at > ?
                            GROUP BY DATE(created_at)
                            ORDER BY date DESC
                            LIMIT ?
                            """, @date.subtract(@env("ACTIVE_THRESHOLD_DAYS", "7").toInteger() + "d"),
                                @date.subtract(@env("ANALYTICS_DAYS", "30").toInteger() + "d"),
                                @env("ANALYTICS_LIMIT", "100").toInteger())
                        
                        engagement: @query("""
                            SELECT 
                                u.id,
                                u.email,
                                COUNT(o.id) as order_count,
                                SUM(o.total) as total_spent,
                                AVG(o.total) as avg_order_value
                            FROM users u
                            LEFT JOIN orders o ON u.id = o.user_id
                            WHERE u.created_at > ?
                            GROUP BY u.id, u.email
                            HAVING COUNT(o.id) > ?
                            ORDER BY total_spent DESC
                            LIMIT ?
                            """, @date.subtract(@env("ENGAGEMENT_PERIOD", "60").toInteger() + "d"),
                                @env("MIN_ORDERS_THRESHOLD", "1").toInteger(),
                                @env("TOP_USERS_LIMIT", "50").toInteger())
                    }
                }
                
                system_config: {
                    database: {
                        connection_pool: {
                            max_size: @env(@env("DB_TYPE", "postgresql") + "_MAX_CONNECTIONS", "20").toInteger()
                            min_size: @env(@env("DB_TYPE", "postgresql") + "_MIN_CONNECTIONS", "5").toInteger()
                            timeout: @env(@env("DB_TYPE", "postgresql") + "_TIMEOUT", "30000").toInteger()
                        }
                        
                        replication: {
                            master: @env(@env("DB_TYPE", "postgresql") + "_MASTER_URL",
                                "jdbc:postgresql://master:5432/tusklang")
                            slave: @env(@env("DB_TYPE", "postgresql") + "_SLAVE_URL",
                                "jdbc:postgresql://slave:5432/tusklang")
                        }
                    }
                    
                    cache: {
                        redis: {
                            host: @env(@env("CACHE_TYPE", "redis") + "_HOST", "localhost")
                            port: @env(@env("CACHE_TYPE", "redis") + "_PORT", "6379").toInteger()
                            database: @env(@env("CACHE_TYPE", "redis") + "_DB", "0").toInteger()
                            ttl: @env(@env("CACHE_TYPE", "redis") + "_TTL", "3600").toInteger()
                        }
                    }
                }
            }
            """;
        
        return tuskParser.parse(tskContent);
    }
    
    public Map<String, Object> processConditionalNesting(String environment) {
        String tskContent = """
            [conditional_nesting]
            # Conditional nested operators
            environment_config: @env("ENVIRONMENT", """ + environment + """)
                .equals("production")
                .then({
                    api: {
                        base_url: @env("PROD_API_BASE_URL", "https://api.tusklang.org")
                        version: @env("PROD_API_VERSION", "v2")
                        timeout: @env("PROD_API_TIMEOUT", "30000").toInteger()
                        retries: @env("PROD_API_RETRIES", "3").toInteger()
                        auth: {
                            type: @env("PROD_AUTH_TYPE", "jwt")
                            secret: @env("PROD_JWT_SECRET").encrypt("AES-256-GCM")
                            expires_in: @env("PROD_JWT_EXPIRES", "3600").toInteger()
                        }
                    }
                    
                    database: {
                        url: @env("PROD_DB_URL", "jdbc:postgresql://prod-db:5432/tusklang")
                        pool: {
                            max_size: @env("PROD_DB_MAX_POOL", "50").toInteger()
                            min_size: @env("PROD_DB_MIN_POOL", "10").toInteger()
                        }
                    }
                })
                .else({
                    api: {
                        base_url: @env("DEV_API_BASE_URL", "http://localhost:3000")
                        version: @env("DEV_API_VERSION", "v1")
                        timeout: @env("DEV_API_TIMEOUT", "5000").toInteger()
                        retries: @env("DEV_API_RETRIES", "1").toInteger()
                        auth: {
                            type: @env("DEV_AUTH_TYPE", "basic")
                            username: @env("DEV_AUTH_USERNAME", "dev")
                            password: @env("DEV_AUTH_PASSWORD", "dev123")
                        }
                    }
                    
                    database: {
                        url: @env("DEV_DB_URL", "jdbc:postgresql://localhost:5432/tusklang_dev")
                        pool: {
                            max_size: @env("DEV_DB_MAX_POOL", "10").toInteger()
                            min_size: @env("DEV_DB_MIN_POOL", "2").toInteger()
                        }
                    }
                })
            """;
        
        return tuskParser.parse(tskContent);
    }
}
```

### 3. Spring Boot Integration
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.config.TuskConfig;
import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.boot.context.properties.ConfigurationProperties;
import java.util.Map;

@SpringBootApplication
public class NestingApplication {
    
    public static void main(String[] args) {
        SpringApplication.run(NestingApplication.class, args);
    }
}

@Configuration
public class TuskNestingConfig {
    
    @Bean
    public TuskLang tuskLang() {
        return new TuskLang();
    }
    
    @Bean
    @ConfigurationProperties(prefix = "tusk.nesting")
    public NestingProperties nestingProperties() {
        return new NestingProperties();
    }
    
    @Bean
    public Map<String, Object> nestedConfiguration() {
        TuskLang parser = new TuskLang();
        
        String tskContent = """
            [spring_nesting]
            # Spring Boot configuration with nested operators
            application: {
                info: {
                    name: @env("SPRING_APPLICATION_NAME", "TuskLang App")
                    version: @env("APP_VERSION", "1.0.0")
                    description: @env("APP_DESCRIPTION", "TuskLang Java Application")
                }
                
                profiles: {
                    active: @env("SPRING_PROFILES_ACTIVE", "dev")
                    default: @env("SPRING_PROFILES_DEFAULT", "dev")
                }
                
                server: {
                    port: @env("SERVER_PORT", "8080").toInteger()
                    host: @env("SERVER_HOST", "localhost")
                    context_path: @env("SERVER_CONTEXT_PATH", "/")
                    ssl: {
                        enabled: @env("SERVER_SSL_ENABLED", "false").toBoolean()
                        key_store: @env("SERVER_SSL_KEY_STORE", "")
                        key_store_password: @env("SERVER_SSL_KEY_STORE_PASSWORD", "")
                    }
                }
                
                database: {
                    primary: {
                        url: @env(@env("DB_TYPE", "postgresql") + "_URL",
                            "jdbc:postgresql://localhost:5432/tusklang")
                        username: @env(@env("DB_TYPE", "postgresql") + "_USERNAME", "postgres")
                        password: @env(@env("DB_TYPE", "postgresql") + "_PASSWORD", "")
                        driver: @env(@env("DB_TYPE", "postgresql") + "_DRIVER",
                            "org.postgresql.Driver")
                    }
                    
                    secondary: {
                        url: @env(@env("DB_TYPE", "postgresql") + "_SECONDARY_URL",
                            "jdbc:postgresql://localhost:5432/tusklang_read")
                        username: @env(@env("DB_TYPE", "postgresql") + "_SECONDARY_USERNAME", "readonly")
                        password: @env(@env("DB_TYPE", "postgresql") + "_SECONDARY_PASSWORD", "")
                    }
                }
                
                security: {
                    jwt: {
                        secret: @env("JWT_SECRET").validate(secret -> secret.length() >= 32, "JWT secret too short")
                        expires_in: @env("JWT_EXPIRES_IN", "3600").toInteger()
                        issuer: @env("JWT_ISSUER", "tusklang")
                        audience: @env("JWT_AUDIENCE", "tusklang-users")
                    }
                    
                    cors: {
                        allowed_origins: @env("CORS_ALLOWED_ORIGINS", "*").split(",")
                        allowed_methods: @env("CORS_ALLOWED_METHODS", "GET,POST,PUT,DELETE").split(",")
                        allowed_headers: @env("CORS_ALLOWED_HEADERS", "*").split(",")
                    }
                }
                
                monitoring: {
                    metrics: {
                        enabled: @env("METRICS_ENABLED", "true").toBoolean()
                        endpoint: @env("METRICS_ENDPOINT", "/actuator/metrics")
                        export: {
                            prometheus: @env("METRICS_PROMETHEUS_ENABLED", "true").toBoolean()
                            influxdb: @env("METRICS_INFLUXDB_ENABLED", "false").toBoolean()
                        }
                    }
                    
                    health: {
                        enabled: @env("HEALTH_ENABLED", "true").toBoolean()
                        endpoint: @env("HEALTH_ENDPOINT", "/actuator/health")
                        details: @env("HEALTH_DETAILS_ENABLED", "true").toBoolean()
                    }
                }
            }
            """;
        
        return parser.parse(tskContent);
    }
}

@Component
public class NestingProperties {
    private boolean enableNesting = true;
    private int maxNestingDepth = 10;
    private boolean enableValidation = true;
    
    // Getters and setters
    public boolean isEnableNesting() { return enableNesting; }
    public void setEnableNesting(boolean enableNesting) { this.enableNesting = enableNesting; }
    
    public int getMaxNestingDepth() { return maxNestingDepth; }
    public void setMaxNestingDepth(int maxNestingDepth) { this.maxNestingDepth = maxNestingDepth; }
    
    public boolean isEnableValidation() { return enableValidation; }
    public void setEnableValidation(boolean enableValidation) { this.enableValidation = enableValidation; }
}
```

### 4. Error Handling in Nested Operators
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.operators.TuskOperatorNesting;
import org.springframework.stereotype.Service;
import java.util.Map;
import java.util.Optional;

@Service
public class NestedErrorHandlingService {
    
    private final TuskLang tuskParser;
    
    public NestedErrorHandlingService(TuskLang tuskParser) {
        this.tuskParser = tuskParser;
    }
    
    public Map<String, Object> processWithErrorHandling() {
        String tskContent = """
            [error_handling_nesting]
            # Safe nested operators with error handling
            safe_nesting: {
                api_config: {
                    base_url: @env("API_BASE_URL")
                        .validate(url -> url != null && url.startsWith("http"), "Invalid API URL")
                        .catch(error -> @env("FALLBACK_API_URL", "http://localhost:3000"))
                    
                    auth: {
                        token: @env("API_TOKEN")
                            .validate(token -> token != null && token.length() >= 32, "Invalid API token")
                            .then(token -> token.encrypt("AES-256-GCM"))
                            .catch(error -> {
                                log.error("API token error: " + error.getMessage());
                                return @env("FALLBACK_API_TOKEN", "default-token");
                            })
                    }
                }
                
                database_config: {
                    url: @env(@env("DB_TYPE", "postgresql") + "_URL")
                        .validate(url -> url != null && url.startsWith("jdbc:"), "Invalid database URL")
                        .catch(error -> @env("FALLBACK_DB_URL", "jdbc:postgresql://localhost:5432/fallback"))
                    
                    credentials: {
                        username: @env(@env("DB_TYPE", "postgresql") + "_USERNAME")
                            .validate(username -> username != null, "Database username required")
                            .catch(error -> @env("FALLBACK_DB_USERNAME", "postgres"))
                        
                        password: @env(@env("DB_TYPE", "postgresql") + "_PASSWORD")
                            .validate(password -> password != null, "Database password required")
                            .then(password -> password.encrypt("AES-256"))
                            .catch(error -> @env("FALLBACK_DB_PASSWORD", ""))
                    }
                }
            }
            
            # Conditional error handling with nesting
            conditional_error_handling: @env("ENVIRONMENT")
                .equals("production")
                .then({
                    secure_config: {
                        api_key: @env("PROD_API_KEY")
                            .validate(key -> key != null, "Production API key required")
                            .then(key -> key.encrypt("AES-256-GCM"))
                            .catch(error -> {
                                log.error("Production API key error: " + error.getMessage());
                                throw new SecurityException("Invalid production API key");
                            })
                    }
                })
                .else({
                    secure_config: {
                        api_key: @env("DEV_API_KEY", "dev-key")
                    }
                })
            """;
        
        return tuskParser.parse(tskContent);
    }
    
    public Optional<String> processOptionalNesting(String userId) {
        String tskContent = """
            [optional_nesting]
            user_info: @query("SELECT * FROM users WHERE id = ?", """ + userId + """)
                .first()
                .validate(user -> user != null, "User not found")
                .then(user -> {
                    user.put("full_name", 
                        user.get("first_name") + " " + user.get("last_name"));
                    user.put("age", 
                        @date.diff(user.get("birth_date"), @date.now(), "years"));
                    return user;
                })
                .then(user -> user.toJson().pretty())
                .optional()
            """;
        
        Map<String, Object> result = tuskParser.parse(tskContent);
        return Optional.ofNullable((String) result.get("user_info"));
    }
}
```

### 5. Performance Optimization
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.operators.TuskOperatorNesting;
import org.springframework.cache.annotation.Cacheable;
import org.springframework.stereotype.Service;
import java.util.Map;
import java.util.List;

@Service
public class OptimizedNestingService {
    
    private final TuskLang tuskParser;
    
    public OptimizedNestingService(TuskLang tuskParser) {
        this.tuskParser = tuskParser;
    }
    
    @Cacheable("nested_configurations")
    public Map<String, Object> getOptimizedConfiguration() {
        String tskContent = """
            [optimized_nesting]
            # Cached nested operations
            cached_analytics: {
                user_stats: @query("""
                    SELECT 
                        COUNT(*) as total_users,
                        COUNT(CASE WHEN active = 1 THEN 1 END) as active_users,
                        AVG(age) as avg_age
                    FROM users
                    """)
                    .first()
                    .cache("5m")
                
                order_stats: @query("""
                    SELECT 
                        COUNT(*) as total_orders,
                        SUM(total) as total_revenue,
                        AVG(total) as avg_order_value
                    FROM orders
                    WHERE created_at > ?
                    """, @date.subtract(@env("ANALYTICS_DAYS", "30").toInteger() + "d"))
                    .first()
                    .cache("10m")
            }
            
            # Lazy evaluation with nesting
            lazy_config: @env("ENVIRONMENT")
                .equals("production")
                .then({
                    expensive_operation: @query("SELECT * FROM large_table")
                        .filter(row -> row.get("status") == "active")
                        .map(row -> row.get("id"))
                        .limit(1000)
                        .lazy()
                        .cache("1h")
                })
                .else({
                    expensive_operation: []
                })
            
            # Parallel processing with nesting
            parallel_data: @parallel([
                @query("SELECT COUNT(*) FROM users"),
                @query("SELECT COUNT(*) FROM orders"),
                @query("SELECT COUNT(*) FROM products")
            ])
            .map(results -> {
                return {
                    user_count: results[0],
                    order_count: results[1],
                    product_count: results[2]
                };
            })
            .cache("15m")
            """;
        
        return tuskParser.parse(tskContent);
    }
}
```

## 🚀 Best Practices

### 1. Nesting Organization
```java
// ✅ Good: Clear, readable nested structure
String goodNesting = """
    user_config: {
        profile: {
            name: @env("USER_NAME", "default")
            email: @env("USER_EMAIL", "default@example.com")
            preferences: {
                theme: @env("USER_THEME", "dark")
                language: @env("USER_LANGUAGE", "en")
            }
        }
    }
    """;

// ❌ Bad: Overly complex nesting
String badNesting = """
    user_config: @env("USER_NAME", "default").then(name -> {
        return {
            profile: {
                name: name,
                email: @env("USER_EMAIL", "default@example.com"),
                preferences: {
                    theme: @env("USER_THEME", "dark"),
                    language: @env("USER_LANGUAGE", "en")
                }
            }
        };
    })
    """;
```

### 2. Error Handling
```java
// ✅ Good: Comprehensive error handling in nested operators
String safeNesting = """
    api_config: {
        base_url: @env("API_BASE_URL")
            .validate(url -> url != null, "API URL required")
            .catch(error -> @env("FALLBACK_API_URL"))
        auth: {
            token: @env("API_TOKEN")
                .validate(token -> token != null, "API token required")
                .then(token -> token.encrypt("AES-256"))
                .catch(error -> @env("FALLBACK_API_TOKEN"))
        }
    }
    """;

// ❌ Bad: No error handling in nested operators
String unsafeNesting = """
    api_config: {
        base_url: @env("API_BASE_URL")
        auth: {
            token: @env("API_TOKEN").encrypt("AES-256")
        }
    }
    """;
```

### 3. Performance Considerations
```java
// ✅ Good: Cached and optimized nested operations
String optimizedNesting = """
    analytics: {
        user_stats: @query("SELECT COUNT(*) FROM users")
            .toInteger()
            .cache("5m")
        order_stats: @query("SELECT COUNT(*) FROM orders")
            .toInteger()
            .cache("10m")
    }
    """;

// ❌ Bad: No caching or optimization
String unoptimizedNesting = """
    analytics: {
        user_stats: @query("SELECT COUNT(*) FROM users").toInteger()
        order_stats: @query("SELECT COUNT(*) FROM orders").toInteger()
    }
    """;
```

## 🔧 Integration Examples

### Spring Boot Configuration
```java
@Configuration
public class NestingConfiguration {
    
    @Bean
    @ConfigurationProperties(prefix = "tusk.nesting")
    public NestingProperties nestingProperties() {
        return new NestingProperties();
    }
    
    @Bean
    public TuskOperatorNesting tuskOperatorNesting() {
        return new TuskOperatorNesting();
    }
}

@Component
public class NestingProperties {
    private boolean enableNesting = true;
    private int maxNestingDepth = 10;
    private boolean enableValidation = true;
    
    // Getters and setters
    public boolean isEnableNesting() { return enableNesting; }
    public void setEnableNesting(boolean enableNesting) { this.enableNesting = enableNesting; }
    
    public int getMaxNestingDepth() { return maxNestingDepth; }
    public void setMaxNestingDepth(int maxNestingDepth) { this.maxNestingDepth = maxNestingDepth; }
    
    public boolean isEnableValidation() { return enableValidation; }
    public void setEnableValidation(boolean enableValidation) { this.enableValidation = enableValidation; }
}
```

### JPA Integration
```java
@Repository
public class NestingRepository {
    
    @PersistenceContext
    private EntityManager entityManager;
    
    public List<Map<String, Object>> processNestedQuery(String tskQuery) {
        // Process TuskLang query with nesting
        TuskLang parser = new TuskLang();
        Map<String, Object> result = parser.parse(tskQuery);
        
        // Execute the nested query
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

### Nesting Performance Comparison
```java
@Service
public class NestingPerformanceService {
    
    public void benchmarkNesting() {
        // Simple nesting: ~2ms
        String simpleNesting = "@env(@env('CONFIG_KEY', 'API_KEY'), 'default')";
        
        // Complex nesting: ~8ms
        String complexNesting = """
            @query("SELECT * FROM users WHERE created_at > ?", 
                @date.subtract(@env("DAYS_BACK", "30").toInteger() + "d"))
                .filter(user -> user.get("active") == true)
                .map(user -> user.get("email"))
                .join(", ")
                .cache("1m")
            """;
        
        // Deep nesting: ~15ms
        String deepNesting = """
            @env("ENVIRONMENT")
                .equals("production")
                .then({
                    api: {
                        base_url: @env("PROD_API_BASE_URL"),
                        auth: {
                            token: @env("PROD_API_TOKEN").encrypt("AES-256")
                        }
                    }
                })
                .else({
                    api: {
                        base_url: @env("DEV_API_BASE_URL"),
                        auth: {
                            token: @env("DEV_API_TOKEN")
                        }
                    }
                })
            """;
    }
}
```

## 🔒 Security Considerations

### Secure Nesting
```java
@Service
public class SecureNestingService {
    
    public Map<String, Object> processSecureNesting() {
        String tskContent = """
            [secure_nesting]
            # Secure nested environment variables
            secure_config: {
                api: {
                    base_url: @env("API_BASE_URL")
                        .validate(url -> url != null && url.startsWith("https://"), "Secure URL required")
                        .catch(error -> {
                            log.error("Security error: " + error.getMessage());
                            throw new SecurityException("Invalid API URL");
                        })
                    
                    auth: {
                        token: @env("API_TOKEN")
                            .validate(token -> token != null && token.length() >= 32, "Invalid API token")
                            .then(token -> token.encrypt("AES-256-GCM"))
                            .catch(error -> {
                                log.error("Security error: " + error.getMessage());
                                throw new SecurityException("Invalid API token");
                            })
                    }
                }
                
                database: {
                    url: @env(@env("DB_TYPE", "postgresql") + "_URL")
                        .validate(url -> url != null && url.startsWith("jdbc:"), "Invalid database URL")
                        .catch(error -> {
                            log.error("Database error: " + error.getMessage());
                            return @env("FALLBACK_DB_URL");
                        })
                }
            }
            """;
        
        return tuskParser.parse(tskContent);
    }
}
```

## 🎯 Summary

@ operator nesting in TuskLang Java provides:

- **Complex Configurations**: Build sophisticated nested configurations
- **Spring Boot Integration**: Seamless integration with Spring applications
- **Error Handling**: Comprehensive error handling and fallbacks
- **Performance Optimization**: Caching and lazy evaluation
- **Security**: Built-in validation and security features
- **JPA Support**: Database integration with nesting
- **Type Safety**: Java type safety with TuskLang flexibility

Master @ operator nesting to create sophisticated, hierarchical configurations that adapt to your application's complexity while maintaining enterprise-grade performance and security. 