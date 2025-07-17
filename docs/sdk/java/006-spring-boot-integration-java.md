# ☕ TuskLang Java Spring Boot Integration Guide

**"We don't bow to any king" - Java Edition**

Master TuskLang integration with Spring Boot for building powerful, configuration-driven web applications with auto-configuration, REST APIs, database integration, and production deployment.

## 🚀 Spring Boot Auto-Configuration

### Maven Dependencies

```xml
<parent>
    <groupId>org.springframework.boot</groupId>
    <artifactId>spring-boot-starter-parent</artifactId>
    <version>3.2.0</version>
</parent>

<dependencies>
    <dependency>
        <groupId>org.springframework.boot</groupId>
        <artifactId>spring-boot-starter-web</artifactId>
    </dependency>
    
    <dependency>
        <groupId>org.springframework.boot</groupId>
        <artifactId>spring-boot-starter-data-jpa</artifactId>
    </dependency>
    
    <dependency>
        <groupId>org.springframework.boot</groupId>
        <artifactId>spring-boot-starter-cache</artifactId>
    </dependency>
    
    <!-- TuskLang Spring Boot Starter -->
    <dependency>
        <groupId>org.tusklang</groupId>
        <artifactId>tusklang-spring-boot-starter</artifactId>
        <version>1.0.0</version>
    </dependency>
    
    <!-- Database Dependencies -->
    <dependency>
        <groupId>org.postgresql</groupId>
        <artifactId>postgresql</artifactId>
    </dependency>
    
    <dependency>
        <groupId>org.springframework.boot</groupId>
        <artifactId>spring-boot-starter-data-redis</artifactId>
    </dependency>
</dependencies>
```

### Application Properties

```properties
# TuskLang Configuration
tusk.config.file=config.tsk
tusk.config.watch=true
tusk.config.reload-on-change=true

# Database Configuration
spring.datasource.url=jdbc:postgresql://localhost:5432/myapp
spring.datasource.username=postgres
spring.datasource.password=secret
spring.jpa.hibernate.ddl-auto=update

# Redis Configuration
spring.redis.host=localhost
spring.redis.port=6379

# Logging
logging.level.org.tusklang=DEBUG
```

### Main Application Class

```java
import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.cache.annotation.EnableCaching;

@SpringBootApplication
@EnableCaching
public class TuskSpringApplication {
    public static void main(String[] args) {
        SpringApplication.run(TuskSpringApplication.class, args);
    }
}
```

## 🎯 Configuration Classes

### TuskLang Configuration Bean

```java
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.tusklang.java.TuskLang;
import org.tusklang.java.config.TuskConfig;
import org.tusklang.java.adapters.PostgreSQLAdapter;
import org.tusklang.java.cache.CacheManager;

@Configuration
public class TuskLangConfiguration {
    
    @Bean
    public TuskLang tuskLang() {
        TuskLang parser = new TuskLang();
        
        // Configure database adapter
        PostgreSQLAdapter db = new PostgreSQLAdapter(PostgreSQLConfig.builder()
            .host("localhost")
            .port(5432)
            .database("myapp")
            .user("postgres")
            .password("secret")
            .build());
        
        parser.setDatabaseAdapter(db);
        
        // Configure cache manager
        CacheManager cacheManager = new CacheManager();
        cacheManager.setDefaultTtl(300); // 5 minutes
        parser.setCacheManager(cacheManager);
        
        return parser;
    }
    
    @Bean
    public TuskConfig tuskConfig(TuskLang tuskLang) {
        return tuskLang.parseFile("config.tsk", TuskConfig.class);
    }
}
```

### Configuration Classes

```java
import org.tusklang.java.config.TuskConfig;
import org.springframework.stereotype.Component;

@TuskConfig
@Component
public class AppConfig {
    public String appName;
    public String version;
    public boolean debug;
    public int port;
    
    public DatabaseConfig database;
    public ServerConfig server;
    public FeaturesConfig features;
}

@TuskConfig
public class DatabaseConfig {
    public String host;
    public int port;
    public String name;
    public String user;
    public String password;
    
    public PoolConfig pool;
}

@TuskConfig
public class PoolConfig {
    public int minSize;
    public int maxSize;
    public int timeout;
}

@TuskConfig
public class ServerConfig {
    public String host;
    public int port;
    public boolean ssl;
}

@TuskConfig
public class FeaturesConfig {
    public boolean userManagement;
    public boolean paymentProcessing;
    public boolean analytics;
}
```

## 🌐 REST API Controllers

### Basic REST Controller

```java
import org.springframework.web.bind.annotation.*;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.ResponseEntity;
import org.tusklang.java.TuskLang;
import org.tusklang.java.config.TuskConfig;
import java.util.Map;

@RestController
@RequestMapping("/api")
public class ApiController {
    
    @Autowired
    private TuskConfig config;
    
    @Autowired
    private TuskLang parser;
    
    @GetMapping("/config")
    public ResponseEntity<Map<String, Object>> getConfig() {
        return ResponseEntity.ok(Map.of(
            "appName", config.getAppName(),
            "version", config.getVersion(),
            "debug", config.isDebug(),
            "port", config.getPort(),
            "features", config.getFeatures()
        ));
    }
    
    @GetMapping("/status")
    public ResponseEntity<Map<String, Object>> getStatus() {
        return ResponseEntity.ok(Map.of(
            "status", "running",
            "timestamp", System.currentTimeMillis(),
            "config", config
        ));
    }
    
    @GetMapping("/health")
    public ResponseEntity<Map<String, Object>> getHealth() {
        // Use TuskLang health checks
        Map<String, Object> healthChecks = parser.parse("""
            [health]
            database: @health.check("database_connection")
            redis: @health.check("redis_connection")
            external_api: @health.check("external_api")
            """);
        
        return ResponseEntity.ok(healthChecks);
    }
}
```

### Database Integration Controller

```java
import org.springframework.web.bind.annotation.*;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.ResponseEntity;
import org.tusklang.java.TuskLang;
import java.util.List;
import java.util.Map;

@RestController
@RequestMapping("/api/users")
public class UserController {
    
    @Autowired
    private TuskLang parser;
    
    @GetMapping
    public ResponseEntity<List<Map<String, Object>>> getUsers() {
        // Use TuskLang database queries
        List<Map<String, Object>> users = parser.query("""
            SELECT id, name, email, created_at 
            FROM users 
            WHERE active = true 
            ORDER BY created_at DESC
            """);
        
        return ResponseEntity.ok(users);
    }
    
    @GetMapping("/{id}")
    public ResponseEntity<Map<String, Object>> getUser(@PathVariable Long id) {
        List<Map<String, Object>> users = parser.query(
            "SELECT * FROM users WHERE id = ?", 
            id
        );
        
        if (users.isEmpty()) {
            return ResponseEntity.notFound().build();
        }
        
        return ResponseEntity.ok(users.get(0));
    }
    
    @PostMapping
    public ResponseEntity<Map<String, Object>> createUser(@RequestBody Map<String, Object> userData) {
        // Use TuskLang FUJSEN for user creation
        Map<String, Object> result = parser.executeFujsen(
            "user_management",
            "createUser",
            userData.get("name"),
            userData.get("email"),
            userData.get("role")
        );
        
        return ResponseEntity.ok(result);
    }
    
    @PutMapping("/{id}")
    public ResponseEntity<Map<String, Object>> updateUser(
            @PathVariable Long id, 
            @RequestBody Map<String, Object> updates) {
        
        Map<String, Object> result = parser.executeFujsen(
            "user_management",
            "updateUser",
            id,
            updates
        );
        
        return ResponseEntity.ok(result);
    }
    
    @GetMapping("/stats")
    public ResponseEntity<Map<String, Object>> getUserStats() {
        // Use TuskLang with database queries
        Map<String, Object> stats = parser.parse("""
            [stats]
            total_users: @query("SELECT COUNT(*) FROM users")
            active_users: @query("SELECT COUNT(*) FROM users WHERE active = true")
            recent_users: @query("SELECT COUNT(*) FROM users WHERE created_at > ?", @date.subtract("7d"))
            """);
        
        return ResponseEntity.ok(stats);
    }
}
```

### Payment Processing Controller

```java
import org.springframework.web.bind.annotation.*;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.ResponseEntity;
import org.tusklang.java.TuskLang;
import java.util.Map;

@RestController
@RequestMapping("/api/payments")
public class PaymentController {
    
    @Autowired
    private TuskLang parser;
    
    @PostMapping("/process")
    public ResponseEntity<Map<String, Object>> processPayment(@RequestBody Map<String, Object> paymentData) {
        // Validate payment data
        boolean isValid = parser.executeFujsen(
            "payment",
            "validate",
            paymentData.get("amount")
        );
        
        if (!isValid) {
            return ResponseEntity.badRequest().body(Map.of(
                "error", "Invalid payment amount"
            ));
        }
        
        // Process payment
        Map<String, Object> result = parser.executeFujsen(
            "payment",
            "process",
            paymentData.get("amount"),
            paymentData.get("recipient")
        );
        
        return ResponseEntity.ok(result);
    }
    
    @GetMapping("/analytics")
    public ResponseEntity<Map<String, Object>> getPaymentAnalytics() {
        Map<String, Object> analytics = parser.parse("""
            [analytics]
            total_revenue: @query("SELECT SUM(amount) FROM payments WHERE status = 'completed'")
            monthly_revenue: @query("SELECT SUM(amount) FROM payments WHERE status = 'completed' AND created_at > ?", @date.subtract("30d"))
            payment_count: @query("SELECT COUNT(*) FROM payments WHERE status = 'completed'")
            average_amount: @query("SELECT AVG(amount) FROM payments WHERE status = 'completed'")
            """);
        
        return ResponseEntity.ok(analytics);
    }
}
```

## 🗄️ Database Integration

### JPA Entity Integration

```java
import jakarta.persistence.*;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;
import org.springframework.stereotype.Service;

@Entity
@Table(name = "users")
public class User {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;
    
    @Column(name = "name")
    private String name;
    
    @Column(name = "email", unique = true)
    private String email;
    
    @Column(name = "role")
    private String role;
    
    @Column(name = "active")
    private boolean active;
    
    @Column(name = "created_at")
    private java.time.LocalDateTime createdAt;
    
    // Getters and setters
}

@Repository
public interface UserRepository extends JpaRepository<User, Long> {
    List<User> findByActiveTrue();
    Optional<User> findByEmail(String email);
    long countByActiveTrue();
}

@Service
public class UserService {
    private final UserRepository userRepository;
    private final TuskLang parser;
    
    public UserService(UserRepository userRepository, TuskLang parser) {
        this.userRepository = userRepository;
        this.parser = parser;
    }
    
    public Map<String, Object> getUserStats() {
        // Use TuskLang with JPA
        return parser.parse("""
            [stats]
            total_users: @query("SELECT COUNT(*) FROM users")
            active_users: @query("SELECT COUNT(*) FROM users WHERE active = true")
            recent_users: @query("SELECT COUNT(*) FROM users WHERE created_at > ?", @date.subtract("7d"))
            """);
    }
    
    public User createUser(String name, String email, String role) {
        // Validate using TuskLang
        boolean isValid = parser.executeFujsen("user_management", "validateUser", name, email);
        
        if (!isValid) {
            throw new IllegalArgumentException("Invalid user data");
        }
        
        User user = new User();
        user.setName(name);
        user.setEmail(email);
        user.setRole(role);
        user.setActive(true);
        user.setCreatedAt(java.time.LocalDateTime.now());
        
        return userRepository.save(user);
    }
}
```

### Transaction Management

```java
import org.springframework.transaction.annotation.Transactional;
import org.springframework.stereotype.Service;

@Service
@Transactional
public class TransactionalUserService {
    private final UserRepository userRepository;
    private final TuskLang parser;
    
    public TransactionalUserService(UserRepository userRepository, TuskLang parser) {
        this.userRepository = userRepository;
        this.parser = parser;
    }
    
    @Transactional
    public void createUserWithProfile(String name, String email, String bio) {
        // Create user
        User user = new User();
        user.setName(name);
        user.setEmail(email);
        user.setActive(true);
        user.setCreatedAt(java.time.LocalDateTime.now());
        
        User savedUser = userRepository.save(user);
        
        // Create profile using TuskLang
        parser.executeFujsen(
            "user_management",
            "createProfile",
            savedUser.getId(),
            bio
        );
    }
    
    @Transactional(readOnly = true)
    public Map<String, Object> getUserAnalytics() {
        return parser.parse("""
            [analytics]
            total_users: @query("SELECT COUNT(*) FROM users")
            active_users: @query("SELECT COUNT(*) FROM users WHERE active = true")
            """);
    }
}
```

## 🔄 Caching Integration

### Redis Cache Configuration

```java
import org.springframework.cache.annotation.EnableCaching;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.data.redis.cache.RedisCacheConfiguration;
import org.springframework.data.redis.cache.RedisCacheManager;
import org.springframework.data.redis.connection.RedisConnectionFactory;
import java.time.Duration;

@Configuration
@EnableCaching
public class CacheConfiguration {
    
    @Bean
    public RedisCacheManager cacheManager(RedisConnectionFactory connectionFactory) {
        RedisCacheConfiguration config = RedisCacheConfiguration.defaultCacheConfig()
            .entryTtl(Duration.ofMinutes(10))
            .disableCachingNullValues();
        
        return RedisCacheManager.builder(connectionFactory)
            .cacheDefaults(config)
            .build();
    }
}
```

### Cached Service Methods

```java
import org.springframework.cache.annotation.Cacheable;
import org.springframework.cache.annotation.CacheEvict;
import org.springframework.cache.annotation.CachePut;
import org.springframework.stereotype.Service;

@Service
public class CachedUserService {
    private final UserRepository userRepository;
    private final TuskLang parser;
    
    public CachedUserService(UserRepository userRepository, TuskLang parser) {
        this.userRepository = userRepository;
        this.parser = parser;
    }
    
    @Cacheable(value = "users", key = "#id")
    public User getUserById(Long id) {
        return userRepository.findById(id).orElse(null);
    }
    
    @Cacheable(value = "userStats", key = "'stats'")
    public Map<String, Object> getUserStats() {
        return parser.parse("""
            [stats]
            total_users: @query("SELECT COUNT(*) FROM users")
            active_users: @query("SELECT COUNT(*) FROM users WHERE active = true")
            """);
    }
    
    @CacheEvict(value = "users", key = "#user.id")
    @CacheEvict(value = "userStats", allEntries = true)
    public User updateUser(User user) {
        return userRepository.save(user);
    }
    
    @CacheEvict(value = "users", allEntries = true)
    @CacheEvict(value = "userStats", allEntries = true)
    public void deleteUser(Long id) {
        userRepository.deleteById(id);
    }
}
```

## 🔒 Security Integration

### Spring Security Configuration

```java
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;
import org.springframework.security.config.annotation.web.configuration.EnableWebSecurity;
import org.springframework.security.web.SecurityFilterChain;
import org.tusklang.java.TuskLang;

@Configuration
@EnableWebSecurity
public class SecurityConfiguration {
    
    @Bean
    public SecurityFilterChain filterChain(HttpSecurity http, TuskLang parser) throws Exception {
        // Use TuskLang for security configuration
        Map<String, Object> securityConfig = parser.parse("""
            [security]
            jwt_secret: @env.secure("JWT_SECRET")
            jwt_expiration: @env("JWT_EXPIRATION", "86400")
            cors_origins: @env("CORS_ORIGINS", "http://localhost:3000")
            """);
        
        http
            .csrf(csrf -> csrf.disable())
            .cors(cors -> cors.configurationSource(corsConfigurationSource()))
            .authorizeHttpRequests(authz -> authz
                .requestMatchers("/api/public/**").permitAll()
                .requestMatchers("/api/admin/**").hasRole("ADMIN")
                .anyRequest().authenticated()
            )
            .oauth2ResourceServer(oauth2 -> oauth2.jwt(jwt -> jwt.jwkSetUri("https://example.com/.well-known/jwks.json")));
        
        return http.build();
    }
    
    @Bean
    public CorsConfigurationSource corsConfigurationSource() {
        CorsConfiguration configuration = new CorsConfiguration();
        configuration.setAllowedOrigins(Arrays.asList("http://localhost:3000"));
        configuration.setAllowedMethods(Arrays.asList("GET", "POST", "PUT", "DELETE"));
        configuration.setAllowedHeaders(Arrays.asList("*"));
        
        UrlBasedCorsConfigurationSource source = new UrlBasedCorsConfigurationSource();
        source.registerCorsConfiguration("/**", configuration);
        return source;
    }
}
```

### JWT Token Service

```java
import org.springframework.stereotype.Service;
import io.jsonwebtoken.Jwts;
import io.jsonwebtoken.SignatureAlgorithm;
import org.tusklang.java.TuskLang;
import java.util.Date;
import java.util.Map;

@Service
public class JwtTokenService {
    private final TuskLang parser;
    
    public JwtTokenService(TuskLang parser) {
        this.parser = parser;
    }
    
    public String generateToken(String username) {
        Map<String, Object> jwtConfig = parser.parse("""
            [jwt]
            secret: @env.secure("JWT_SECRET")
            expiration: @env("JWT_EXPIRATION", "86400")
            """);
        
        String secret = (String) jwtConfig.get("jwt");
        long expiration = Long.parseLong(jwtConfig.get("jwt").toString());
        
        return Jwts.builder()
            .setSubject(username)
            .setIssuedAt(new Date())
            .setExpiration(new Date(System.currentTimeMillis() + expiration * 1000))
            .signWith(SignatureAlgorithm.HS512, secret)
            .compact();
    }
    
    public String getUsernameFromToken(String token) {
        Map<String, Object> jwtConfig = parser.parse("""
            [jwt]
            secret: @env.secure("JWT_SECRET")
            """);
        
        String secret = (String) jwtConfig.get("jwt");
        
        return Jwts.parser()
            .setSigningKey(secret)
            .parseClaimsJws(token)
            .getBody()
            .getSubject();
    }
}
```

## 📊 Monitoring and Metrics

### Actuator Integration

```java
import org.springframework.boot.actuator.endpoint.annotation.Endpoint;
import org.springframework.boot.actuator.endpoint.annotation.ReadOperation;
import org.springframework.stereotype.Component;
import org.tusklang.java.TuskLang;
import java.util.Map;

@Component
@Endpoint(id = "tuskconfig")
public class TuskConfigEndpoint {
    private final TuskLang parser;
    
    public TuskConfigEndpoint(TuskLang parser) {
        this.parser = parser;
    }
    
    @ReadOperation
    public Map<String, Object> getTuskConfig() {
        return parser.parse("""
            [config]
            app_name: @env("APP_NAME", "TuskLang App")
            version: @env("APP_VERSION", "1.0.0")
            environment: @env("APP_ENV", "development")
            """);
    }
}

@Component
@Endpoint(id = "tuskhealth")
public class TuskHealthEndpoint {
    private final TuskLang parser;
    
    public TuskHealthEndpoint(TuskLang parser) {
        this.parser = parser;
    }
    
    @ReadOperation
    public Map<String, Object> getTuskHealth() {
        return parser.parse("""
            [health]
            database: @health.check("database_connection")
            redis: @health.check("redis_connection")
            external_api: @health.check("external_api")
            """);
    }
}
```

### Metrics Integration

```java
import io.micrometer.core.instrument.MeterRegistry;
import org.springframework.stereotype.Component;
import org.tusklang.java.TuskLang;

@Component
public class MetricsService {
    private final MeterRegistry meterRegistry;
    private final TuskLang parser;
    
    public MetricsService(MeterRegistry meterRegistry, TuskLang parser) {
        this.meterRegistry = meterRegistry;
        this.parser = parser;
    }
    
    public void recordApiCall(String endpoint, long duration) {
        meterRegistry.counter("api_calls_total", "endpoint", endpoint).increment();
        meterRegistry.timer("api_response_time", "endpoint", endpoint).record(duration, java.util.concurrent.TimeUnit.MILLISECONDS);
    }
    
    public void recordDatabaseQuery(String query, long duration) {
        meterRegistry.counter("database_queries_total", "query", query).increment();
        meterRegistry.timer("database_query_time", "query", query).record(duration, java.util.concurrent.TimeUnit.MILLISECONDS);
    }
    
    public Map<String, Object> getMetrics() {
        return parser.parse("""
            [metrics]
            api_calls: @metrics.counter("api_calls_total")
            response_time: @metrics.histogram("api_response_time")
            database_queries: @metrics.counter("database_queries_total")
            """);
    }
}
```

## 🚀 Production Deployment

### Docker Configuration

```dockerfile
FROM openjdk:17-alpine

WORKDIR /app

# Copy application
COPY target/tusk-spring-app-1.0.0.jar app.jar

# Copy TuskLang configuration
COPY config.tsk config.tsk

# Create non-root user
RUN addgroup -g 1001 -S appgroup && \
    adduser -u 1001 -S appuser -G appgroup

# Change ownership
RUN chown -R appuser:appgroup /app

USER appuser

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/actuator/health || exit 1

# Run application
CMD ["java", "-jar", "app.jar"]
```

### Kubernetes Deployment

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: tusk-spring-app
spec:
  replicas: 3
  selector:
    matchLabels:
      app: tusk-spring-app
  template:
    metadata:
      labels:
        app: tusk-spring-app
    spec:
      containers:
      - name: app
        image: tusk-spring-app:latest
        ports:
        - containerPort: 8080
        env:
        - name: APP_ENV
          value: "production"
        - name: DB_HOST
          valueFrom:
            configMapKeyRef:
              name: app-config
              key: db_host
        - name: DB_PASSWORD
          valueFrom:
            secretKeyRef:
              name: app-secrets
              key: db_password
        - name: JWT_SECRET
          valueFrom:
            secretKeyRef:
              name: app-secrets
              key: jwt_secret
        resources:
          requests:
            memory: "512Mi"
            cpu: "250m"
          limits:
            memory: "1Gi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /actuator/health
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /actuator/health
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 5
---
apiVersion: v1
kind: Service
metadata:
  name: tusk-spring-app-service
spec:
  selector:
    app: tusk-spring-app
  ports:
  - port: 80
    targetPort: 8080
  type: LoadBalancer
```

### Application Properties for Production

```properties
# Production Configuration
spring.profiles.active=production

# TuskLang Configuration
tusk.config.file=config.tsk
tusk.config.watch=false
tusk.config.reload-on-change=false

# Database Configuration
spring.datasource.url=jdbc:postgresql://${DB_HOST}:5432/myapp
spring.datasource.username=${DB_USER}
spring.datasource.password=${DB_PASSWORD}
spring.jpa.hibernate.ddl-auto=validate

# Redis Configuration
spring.redis.host=${REDIS_HOST}
spring.redis.port=6379
spring.redis.password=${REDIS_PASSWORD}

# Security
spring.security.oauth2.resourceserver.jwt.issuer-uri=https://example.com
spring.security.oauth2.resourceserver.jwt.jwk-set-uri=https://example.com/.well-known/jwks.json

# Actuator
management.endpoints.web.exposure.include=health,info,metrics,tuskconfig,tuskhealth
management.endpoint.health.show-details=when-authorized

# Logging
logging.level.org.tusklang=INFO
logging.level.org.springframework=INFO
logging.pattern.console=%d{yyyy-MM-dd HH:mm:ss} [%thread] %-5level %logger{36} - %msg%n

# Performance
spring.jpa.properties.hibernate.jdbc.batch_size=20
spring.jpa.properties.hibernate.order_inserts=true
spring.jpa.properties.hibernate.order_updates=true
```

## 🧪 Testing

### Integration Tests

```java
import org.junit.jupiter.api.Test;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.autoconfigure.web.servlet.AutoConfigureWebMvc;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.beans.factory.annotation.Autowired;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.*;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.*;

@SpringBootTest
@AutoConfigureWebMvc
class TuskSpringIntegrationTest {
    
    @Autowired
    private MockMvc mockMvc;
    
    @Test
    void testGetConfig() throws Exception {
        mockMvc.perform(get("/api/config"))
            .andExpect(status().isOk())
            .andExpect(jsonPath("$.appName").exists())
            .andExpect(jsonPath("$.version").exists());
    }
    
    @Test
    void testGetHealth() throws Exception {
        mockMvc.perform(get("/api/health"))
            .andExpect(status().isOk())
            .andExpect(jsonPath("$.health").exists());
    }
    
    @Test
    void testCreateUser() throws Exception {
        String userJson = """
            {
                "name": "Test User",
                "email": "test@example.com",
                "role": "user"
            }
            """;
        
        mockMvc.perform(post("/api/users")
            .contentType("application/json")
            .content(userJson))
            .andExpect(status().isOk())
            .andExpect(jsonPath("$.success").value(true));
    }
}
```

## 📚 Next Steps

1. **Deploy to production** - Use Docker and Kubernetes
2. **Add monitoring** - Implement comprehensive observability
3. **Scale horizontally** - Use load balancers and multiple instances
4. **Implement security** - Add authentication and authorization
5. **Optimize performance** - Use caching and database optimization

---

**"We don't bow to any king"** - You now have complete mastery of TuskLang Spring Boot integration! Build powerful, configuration-driven web applications with enterprise-grade features, comprehensive monitoring, and production-ready deployment. 