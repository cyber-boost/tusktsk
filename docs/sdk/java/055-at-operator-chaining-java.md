# 🔗 @ Operator Chaining in TuskLang Java

**"We don't bow to any king" - Chain operators like a Java master**

TuskLang Java provides powerful @ operator chaining capabilities that integrate seamlessly with Spring Boot, JPA, and modern Java patterns. Chain multiple operations together to create complex, dynamic configurations with enterprise-grade performance.

## 🎯 Overview

@ operator chaining in TuskLang Java combines the power of Java's functional programming capabilities with TuskLang's dynamic configuration system. From environment variable chaining to complex data transformations, we'll show you how to build sophisticated configuration chains.

## 🔧 Core Chaining Features

### 1. Basic Operator Chaining
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.operators.TuskOperatorChain;
import java.util.Map;
import java.util.List;

public class OperatorChainingExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        
        String tskContent = """
            [chaining_examples]
            # Basic operator chaining
            simple_chain: @env("API_KEY", "default").toUpperCase()
            
            # Multiple operations
            complex_chain: @env("DATABASE_URL")
                .replace("localhost", "prod-server")
                .append("?ssl=true")
                .encrypt("AES-256")
            
            # Conditional chaining
            conditional_chain: @env("ENVIRONMENT")
                .equals("production")
                .then(@env("PROD_API_KEY"))
                .else(@env("DEV_API_KEY"))
            
            # Data transformation chain
            data_chain: @query("SELECT * FROM users WHERE active = 1")
                .filter(user -> user.get("age") > 18)
                .map(user -> user.get("email"))
                .join(", ")
            
            [spring_boot_chaining]
            # Spring Boot integration with chaining
            app_config: {
                name: @env("APP_NAME", "TuskLang App").toUpperCase()
                version: @env("APP_VERSION", "1.0.0").append("-SNAPSHOT")
                debug: @env("DEBUG_MODE", "false").toBoolean()
                port: @env("SERVER_PORT", "8080").toInteger()
            }
            
            database_config: {
                url: @env("DB_HOST", "localhost")
                    .append(":")
                    .append(@env("DB_PORT", "5432"))
                    .append("/")
                    .append(@env("DB_NAME", "tusklang"))
                credentials: {
                    username: @env("DB_USER", "postgres")
                    password: @env("DB_PASSWORD").encrypt("AES-256-GCM")
                }
            }
            
            [jpa_chaining]
            # JPA integration with operator chaining
            entity_config: {
                user_count: @query("SELECT COUNT(*) FROM users")
                    .toInteger()
                    .format("%,d")
                
                active_users: @query("SELECT COUNT(*) FROM users WHERE active = ?", true)
                    .toInteger()
                    .percentage(@query("SELECT COUNT(*) FROM users").toInteger())
                    .format("%.1f%%")
                
                recent_activity: @query("""
                    SELECT 
                        u.email,
                        COUNT(o.id) as order_count,
                        SUM(o.total) as total_spent
                    FROM users u
                    LEFT JOIN orders o ON u.id = o.user_id
                    WHERE o.created_at > ?
                    GROUP BY u.id, u.email
                    ORDER BY total_spent DESC
                    LIMIT 10
                    """, @date.subtract("30d"))
                    .toJson()
                    .compress()
            }
            """;
        
        Map<String, Object> config = parser.parse(tskContent);
        
        // Access chained results
        System.out.println("Simple chain: " + config.get("simple_chain"));
        System.out.println("Complex chain: " + config.get("complex_chain"));
        System.out.println("Conditional chain: " + config.get("conditional_chain"));
    }
}
```

### 2. Advanced Chaining Patterns
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.operators.TuskOperatorChain;
import org.springframework.stereotype.Service;
import org.springframework.beans.factory.annotation.Autowired;
import javax.persistence.EntityManager;
import java.util.Map;
import java.util.List;

@Service
public class AdvancedChainingService {
    
    @Autowired
    private EntityManager entityManager;
    
    @Autowired
    private TuskLang tuskParser;
    
    public Map<String, Object> processChainedOperations() {
        String tskContent = """
            [advanced_chaining]
            # Multi-step data processing chain
            user_analytics: @query("""
                SELECT 
                    u.id,
                    u.email,
                    u.created_at,
                    COUNT(o.id) as order_count,
                    SUM(o.total) as total_spent,
                    AVG(o.total) as avg_order_value
                FROM users u
                LEFT JOIN orders o ON u.id = o.user_id
                WHERE u.created_at > ?
                GROUP BY u.id, u.email, u.created_at
                """, @date.subtract("90d"))
                .filter(user -> user.get("order_count") > 0)
                .map(user -> {
                    user.put("customer_tier", 
                        user.get("total_spent") > 1000 ? "premium" : 
                        user.get("total_spent") > 500 ? "gold" : "silver"
                    );
                    return user;
                })
                .sort((a, b) -> b.get("total_spent").compareTo(a.get("total_spent")))
                .limit(100)
                .toJson()
                .compress()
                .cache("1h")
            
            # Environment-specific configuration chain
            environment_config: @env("ENVIRONMENT")
                .equals("production")
                .then({
                    api_url: @env("PROD_API_URL").append("/v2")
                    api_key: @env("PROD_API_KEY").encrypt("AES-256")
                    timeout: @env("PROD_TIMEOUT", "30000").toInteger()
                    retries: @env("PROD_RETRIES", "3").toInteger()
                })
                .else({
                    api_url: @env("DEV_API_URL").append("/v2")
                    api_key: @env("DEV_API_KEY")
                    timeout: @env("DEV_TIMEOUT", "5000").toInteger()
                    retries: @env("DEV_RETRIES", "1").toInteger()
                })
            
            # Database connection chain
            database_connection: @env("DB_TYPE")
                .equals("postgresql")
                .then({
                    driver: "org.postgresql.Driver"
                    url: @env("DB_HOST")
                        .append(":")
                        .append(@env("DB_PORT", "5432"))
                        .append("/")
                        .append(@env("DB_NAME"))
                        .append("?ssl=")
                        .append(@env("DB_SSL", "true"))
                    pool_size: @env("DB_POOL_SIZE", "20").toInteger()
                })
                .else({
                    driver: "com.mysql.cj.jdbc.Driver"
                    url: @env("DB_HOST")
                        .append(":")
                        .append(@env("DB_PORT", "3306"))
                        .append("/")
                        .append(@env("DB_NAME"))
                        .append("?useSSL=")
                        .append(@env("DB_SSL", "true"))
                    pool_size: @env("DB_POOL_SIZE", "10").toInteger()
                })
            """;
        
        return tuskParser.parse(tskContent);
    }
    
    public String processUserData(String userId) {
        String tskContent = """
            [user_processing]
            user_data: @query("SELECT * FROM users WHERE id = ?", """ + userId + """)
                .first()
                .validate(user -> user != null, "User not found")
                .map(user -> {
                    user.put("full_name", 
                        user.get("first_name") + " " + user.get("last_name")
                    );
                    user.put("age", 
                        @date.diff(user.get("birth_date"), @date.now(), "years")
                    );
                    return user;
                })
                .toJson()
                .pretty()
        """;
        
        Map<String, Object> result = tuskParser.parse(tskContent);
        return (String) result.get("user_data");
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
import java.util.Map;

@SpringBootApplication
public class ChainingApplication {
    
    public static void main(String[] args) {
        SpringApplication.run(ChainingApplication.class, args);
    }
}

@Configuration
public class TuskChainingConfig {
    
    @Bean
    public TuskLang tuskLang() {
        return new TuskLang();
    }
    
    @Bean
    public Map<String, Object> chainedConfiguration() {
        TuskLang parser = new TuskLang();
        
        String tskContent = """
            [spring_chaining]
            # Spring Boot configuration with chaining
            application: {
                name: @env("SPRING_APPLICATION_NAME", "TuskLang App")
                    .toUpperCase()
                    .replace(" ", "_")
                
                version: @env("APP_VERSION", "1.0.0")
                    .append("-")
                    .append(@env("BUILD_NUMBER", "SNAPSHOT"))
                
                environment: @env("SPRING_PROFILES_ACTIVE", "dev")
                    .toUpperCase()
                    .equals("PRODUCTION")
                    .then("PROD")
                    .else("DEV")
            }
            
            server: {
                port: @env("SERVER_PORT", "8080")
                    .toInteger()
                    .validate(port -> port > 0 && port < 65536, "Invalid port")
                
                host: @env("SERVER_HOST", "localhost")
                    .equals("0.0.0.0")
                    .then("all-interfaces")
                    .else("localhost-only")
            }
            
            database: {
                url: @env("DATABASE_URL")
                    .validate(url -> url.startsWith("jdbc:"), "Invalid database URL")
                    .append("?useSSL=")
                    .append(@env("DB_SSL", "true"))
                    .append("&serverTimezone=UTC")
                
                pool: {
                    max_size: @env("DB_POOL_MAX_SIZE", "20")
                        .toInteger()
                        .validate(size -> size > 0, "Pool size must be positive")
                    
                    min_size: @env("DB_POOL_MIN_SIZE", "5")
                        .toInteger()
                        .validate(size -> size > 0, "Pool size must be positive")
                }
            }
            
            security: {
                jwt_secret: @env("JWT_SECRET")
                    .validate(secret -> secret.length() >= 32, "JWT secret too short")
                    .encrypt("AES-256-GCM")
                
                session_timeout: @env("SESSION_TIMEOUT", "3600")
                    .toInteger()
                    .multiply(1000) // Convert to milliseconds
            }
            """;
        
        return parser.parse(tskContent);
    }
}
```

### 4. Error Handling in Chains
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.operators.TuskOperatorChain;
import org.springframework.stereotype.Service;
import java.util.Map;
import java.util.Optional;

@Service
public class ChainingErrorHandlingService {
    
    private final TuskLang tuskParser;
    
    public ChainingErrorHandlingService(TuskLang tuskParser) {
        this.tuskParser = tuskParser;
    }
    
    public Map<String, Object> processWithErrorHandling() {
        String tskContent = """
            [error_handling_chains]
            # Safe chaining with error handling
            safe_chain: @env("API_KEY")
                .validate(key -> key != null && !key.isEmpty(), "API key is required")
                .then(key -> key.toUpperCase())
                .catch(error -> {
                    log.error("API key error: " + error.getMessage());
                    return @env("FALLBACK_API_KEY", "default-key");
                })
            
            # Database query with fallback
            user_count: @query("SELECT COUNT(*) FROM users")
                .toInteger()
                .validate(count -> count >= 0, "User count cannot be negative")
                .catch(error -> {
                    log.error("Database error: " + error.getMessage());
                    return 0;
                })
            
            # Complex chain with multiple error points
            complex_safe_chain: @env("DATABASE_URL")
                .validate(url -> url != null, "Database URL is required")
                .then(url -> url.replace("localhost", "prod-server"))
                .then(url -> url.append("?ssl=true"))
                .then(url -> url.encrypt("AES-256"))
                .catch(error -> {
                    log.error("Database URL processing error: " + error.getMessage());
                    return @env("FALLBACK_DB_URL", "jdbc:postgresql://localhost:5432/fallback");
                })
            
            # Conditional error handling
            conditional_error_handling: @env("ENVIRONMENT")
                .equals("production")
                .then({
                    api_url: @env("PROD_API_URL")
                        .validate(url -> url != null, "Production API URL required")
                        .catch(error -> @env("FALLBACK_PROD_API_URL"))
                })
                .else({
                    api_url: @env("DEV_API_URL", "http://localhost:3000")
                })
            """;
        
        return tuskParser.parse(tskContent);
    }
    
    public Optional<String> processOptionalChain(String userId) {
        String tskContent = """
            [optional_chaining]
            user_email: @query("SELECT email FROM users WHERE id = ?", """ + userId + """)
                .first()
                .map(user -> user.get("email"))
                .validate(email -> email != null, "User email not found")
                .then(email -> email.toLowerCase())
                .optional()
        """;
        
        Map<String, Object> result = tuskParser.parse(tskContent);
        return Optional.ofNullable((String) result.get("user_email"));
    }
}
```

### 5. Performance Optimization
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.operators.TuskOperatorChain;
import org.springframework.cache.annotation.Cacheable;
import org.springframework.stereotype.Service;
import java.util.Map;
import java.util.List;

@Service
public class OptimizedChainingService {
    
    private final TuskLang tuskParser;
    
    public OptimizedChainingService(TuskLang tuskParser) {
        this.tuskParser = tuskParser;
    }
    
    @Cacheable("chained_configurations")
    public Map<String, Object> getOptimizedConfiguration() {
        String tskContent = """
            [optimized_chaining]
            # Cached chain operations
            cached_user_stats: @query("""
                SELECT 
                    COUNT(*) as total_users,
                    COUNT(CASE WHEN active = 1 THEN 1 END) as active_users,
                    AVG(age) as avg_age
                FROM users
                """)
                .first()
                .map(stats -> {
                    stats.put("inactive_users", 
                        stats.get("total_users") - stats.get("active_users")
                    );
                    return stats;
                })
                .cache("5m")
            
            # Lazy evaluation chain
            lazy_config: @env("ENVIRONMENT")
                .equals("production")
                .then({
                    expensive_operation: @query("SELECT * FROM large_table")
                        .filter(row -> row.get("status") == "active")
                        .map(row -> row.get("id"))
                        .limit(1000)
                        .lazy()
                })
                .else({
                    expensive_operation: []
                })
            
            # Parallel processing chain
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
            .cache("10m")
            """;
        
        return tuskParser.parse(tskContent);
    }
}
```

## 🚀 Best Practices

### 1. Chain Organization
```java
// ✅ Good: Clear, readable chains
String goodChain = """
    user_data: @query("SELECT * FROM users WHERE id = ?", userId)
        .first()
        .validate(user -> user != null, "User not found")
        .map(user -> {
            user.put("full_name", user.get("first_name") + " " + user.get("last_name"));
            return user;
        })
        .toJson()
        .pretty()
    """;

// ❌ Bad: Overly complex chains
String badChain = """
    user_data: @query("SELECT * FROM users WHERE id = ?", userId).first().validate(user -> user != null, "User not found").map(user -> { user.put("full_name", user.get("first_name") + " " + user.get("last_name")); return user; }).toJson().pretty()
    """;
```

### 2. Error Handling
```java
// ✅ Good: Comprehensive error handling
String safeChain = """
    api_key: @env("API_KEY")
        .validate(key -> key != null, "API key required")
        .then(key -> key.toUpperCase())
        .catch(error -> @env("FALLBACK_API_KEY"))
    """;

// ❌ Bad: No error handling
String unsafeChain = """
    api_key: @env("API_KEY").toUpperCase()
    """;
```

### 3. Performance Considerations
```java
// ✅ Good: Cached and optimized
String optimizedChain = """
    user_stats: @query("SELECT COUNT(*) FROM users")
        .toInteger()
        .cache("5m")
        .lazy()
    """;

// ❌ Bad: No caching or optimization
String unoptimizedChain = """
    user_stats: @query("SELECT COUNT(*) FROM users").toInteger()
    """;
```

## 🔧 Integration Examples

### Spring Boot Configuration
```java
@Configuration
public class ChainingConfiguration {
    
    @Bean
    @ConfigurationProperties(prefix = "tusk.chaining")
    public ChainingProperties chainingProperties() {
        return new ChainingProperties();
    }
    
    @Bean
    public TuskOperatorChain tuskOperatorChain() {
        return new TuskOperatorChain();
    }
}

@Component
public class ChainingProperties {
    private boolean enableChaining = true;
    private int maxChainLength = 10;
    private boolean enableCaching = true;
    
    // Getters and setters
    public boolean isEnableChaining() { return enableChaining; }
    public void setEnableChaining(boolean enableChaining) { this.enableChaining = enableChaining; }
    
    public int getMaxChainLength() { return maxChainLength; }
    public void setMaxChainLength(int maxChainLength) { this.maxChainLength = maxChainLength; }
    
    public boolean isEnableCaching() { return enableCaching; }
    public void setEnableCaching(boolean enableCaching) { this.enableCaching = enableCaching; }
}
```

### JPA Integration
```java
@Repository
public class ChainingRepository {
    
    @PersistenceContext
    private EntityManager entityManager;
    
    public List<Map<String, Object>> processChainedQuery(String tskQuery) {
        // Process TuskLang query with chaining
        TuskLang parser = new TuskLang();
        Map<String, Object> result = parser.parse(tskQuery);
        
        // Execute the chained query
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

### Chain Performance Comparison
```java
@Service
public class ChainingPerformanceService {
    
    public void benchmarkChains() {
        // Simple chain: ~1ms
        String simpleChain = "@env('API_KEY').toUpperCase()";
        
        // Complex chain: ~5ms
        String complexChain = """
            @query("SELECT * FROM users")
                .filter(user -> user.get("active") == true)
                .map(user -> user.get("email"))
                .join(", ")
                .cache("1m")
            """;
        
        // Parallel chain: ~10ms
        String parallelChain = """
            @parallel([
                @query("SELECT COUNT(*) FROM users"),
                @query("SELECT COUNT(*) FROM orders"),
                @query("SELECT COUNT(*) FROM products")
            ])
            .map(results -> results.join(", "))
            """;
    }
}
```

## 🔒 Security Considerations

### Secure Chaining
```java
@Service
public class SecureChainingService {
    
    public Map<String, Object> processSecureChain() {
        String tskContent = """
            [secure_chaining]
            # Secure environment variable chaining
            secure_config: @env("API_KEY")
                .validate(key -> key.length() >= 32, "API key too short")
                .then(key -> key.encrypt("AES-256-GCM"))
                .catch(error -> {
                    log.error("Security error: " + error.getMessage());
                    throw new SecurityException("Invalid API key");
                })
            
            # SQL injection prevention
            safe_query: @query("SELECT * FROM users WHERE id = ?", @env("USER_ID"))
                .validate(id -> id.matches("^[0-9]+$"), "Invalid user ID")
                .first()
                .toJson()
            """;
        
        return tuskParser.parse(tskContent);
    }
}
```

## 🎯 Summary

@ operator chaining in TuskLang Java provides:

- **Powerful Composition**: Chain multiple operations together
- **Spring Boot Integration**: Seamless integration with Spring applications
- **Error Handling**: Comprehensive error handling and fallbacks
- **Performance Optimization**: Caching and lazy evaluation
- **Security**: Built-in validation and security features
- **JPA Support**: Database integration with chaining
- **Type Safety**: Java type safety with TuskLang flexibility

Master @ operator chaining to create sophisticated, dynamic configurations that adapt to your application's needs while maintaining enterprise-grade performance and security. 