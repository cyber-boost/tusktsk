# ☕ TuskLang Java Database Integration Guide

**"We don't bow to any king" - Java Edition**

Master database integration in TuskLang Java with comprehensive coverage of all database adapters, @query operators, ORM integration, and advanced database features.

## 🗄️ Database Adapters

TuskLang Java supports multiple database adapters for seamless integration:

### 1. SQLite Adapter

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.adapters.SQLiteAdapter;
import java.util.Map;

public class SQLiteExample {
    public static void main(String[] args) {
        // Create SQLite adapter
        SQLiteAdapter db = new SQLiteAdapter("app.db");
        
        // Create TuskLang parser with database
        TuskLang parser = new TuskLang();
        parser.setDatabaseAdapter(db);
        
        // TSK file with database queries
        String tskContent = """
            [database]
            user_count: @query("SELECT COUNT(*) FROM users")
            active_users: @query("SELECT COUNT(*) FROM users WHERE active = 1")
            recent_orders: @query("SELECT COUNT(*) FROM orders WHERE created_at > ?", @date.subtract("7d"))
            """;
        
        Map<String, Object> config = parser.parse(tskContent);
        System.out.println("User count: " + config.get("database"));
    }
}
```

### 2. PostgreSQL Adapter

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.adapters.PostgreSQLAdapter;
import org.tusklang.java.config.PostgreSQLConfig;

public class PostgreSQLExample {
    public static void main(String[] args) {
        // Configure PostgreSQL adapter
        PostgreSQLAdapter db = new PostgreSQLAdapter(PostgreSQLConfig.builder()
            .host("localhost")
            .port(5432)
            .database("myapp")
            .user("postgres")
            .password("secret")
            .poolSize(10)
            .connectionTimeout(30)
            .build());
        
        TuskLang parser = new TuskLang();
        parser.setDatabaseAdapter(db);
        
        String tskContent = """
            [analytics]
            total_revenue: @query("SELECT SUM(amount) FROM orders WHERE status = 'completed'")
            monthly_users: @query("SELECT COUNT(DISTINCT user_id) FROM orders WHERE created_at >= ?", @date.subtract("30d"))
            top_products: @query("SELECT product_name, COUNT(*) as sales FROM order_items GROUP BY product_name ORDER BY sales DESC LIMIT 5")
            """;
        
        Map<String, Object> config = parser.parse(tskContent);
        System.out.println("Analytics: " + config.get("analytics"));
    }
}
```

### 3. MySQL Adapter

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.adapters.MySQLAdapter;
import org.tusklang.java.config.MySQLConfig;

public class MySQLExample {
    public static void main(String[] args) {
        // Configure MySQL adapter
        MySQLAdapter db = new MySQLAdapter(MySQLConfig.builder()
            .host("localhost")
            .port(3306)
            .database("myapp")
            .user("root")
            .password("secret")
            .useSSL(false)
            .autoReconnect(true)
            .build());
        
        TuskLang parser = new TuskLang();
        parser.setDatabaseAdapter(db);
        
        String tskContent = """
            [users]
            total_count: @query("SELECT COUNT(*) FROM users")
            active_count: @query("SELECT COUNT(*) FROM users WHERE last_login > ?", @date.subtract("30d"))
            premium_count: @query("SELECT COUNT(*) FROM users WHERE subscription_type = 'premium'")
            """;
        
        Map<String, Object> config = parser.parse(tskContent);
        System.out.println("User stats: " + config.get("users"));
    }
}
```

### 4. MongoDB Adapter

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.adapters.MongoDBAdapter;
import org.tusklang.java.config.MongoDBConfig;

public class MongoDBExample {
    public static void main(String[] args) {
        // Configure MongoDB adapter
        MongoDBAdapter db = new MongoDBAdapter(MongoDBConfig.builder()
            .host("localhost")
            .port(27017)
            .database("myapp")
            .username("admin")
            .password("secret")
            .authDatabase("admin")
            .build());
        
        TuskLang parser = new TuskLang();
        parser.setDatabaseAdapter(db);
        
        String tskContent = """
            [analytics]
            user_count: @query("db.users.countDocuments()")
            recent_orders: @query("db.orders.countDocuments({createdAt: {$gte: ?}})", @date.subtract("7d"))
            top_categories: @query("db.orders.aggregate([{$group: {_id: '$category', count: {$sum: 1}}}, {$sort: {count: -1}}, {$limit: 5}])")
            """;
        
        Map<String, Object> config = parser.parse(tskContent);
        System.out.println("MongoDB analytics: " + config.get("analytics"));
    }
}
```

### 5. Redis Adapter

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.adapters.RedisAdapter;
import org.tusklang.java.config.RedisConfig;

public class RedisExample {
    public static void main(String[] args) {
        // Configure Redis adapter
        RedisAdapter db = new RedisAdapter(RedisConfig.builder()
            .host("localhost")
            .port(6379)
            .password("secret")
            .database(0)
            .timeout(30)
            .build());
        
        TuskLang parser = new TuskLang();
        parser.setDatabaseAdapter(db);
        
        String tskContent = """
            [cache]
            session_count: @query("SCARD sessions")
            active_keys: @query("KEYS *")
            memory_usage: @query("INFO memory")
            """;
        
        Map<String, Object> config = parser.parse(tskContent);
        System.out.println("Redis stats: " + config.get("cache"));
    }
}
```

## ⚡ @query Operator

### Basic Queries

```tsk
# Simple queries
[database]
user_count: @query("SELECT COUNT(*) FROM users")
total_orders: @query("SELECT COUNT(*) FROM orders")
revenue: @query("SELECT SUM(amount) FROM orders WHERE status = 'completed'")

# Parameterized queries
recent_users: @query("SELECT COUNT(*) FROM users WHERE created_at > ?", @date.subtract("7d"))
user_by_id: @query("SELECT * FROM users WHERE id = ?", @request.user_id)
orders_by_status: @query("SELECT * FROM orders WHERE status = ?", "pending")
```

### Complex Queries

```tsk
# Joins and aggregations
[analytics]
user_orders: @query("""
    SELECT u.name, COUNT(o.id) as order_count, SUM(o.amount) as total_spent
    FROM users u
    LEFT JOIN orders o ON u.id = o.user_id
    WHERE o.created_at > ?
    GROUP BY u.id, u.name
    ORDER BY total_spent DESC
    LIMIT 10
""", @date.subtract("30d"))

# Subqueries
top_customers: @query("""
    SELECT user_id, total_spent
    FROM (
        SELECT user_id, SUM(amount) as total_spent
        FROM orders
        WHERE status = 'completed'
        GROUP BY user_id
    ) user_totals
    WHERE total_spent > (
        SELECT AVG(total_spent) FROM (
            SELECT SUM(amount) as total_spent
            FROM orders
            WHERE status = 'completed'
            GROUP BY user_id
        ) avg_totals
    )
""")

# Window functions
user_rankings: @query("""
    SELECT 
        user_id,
        total_spent,
        RANK() OVER (ORDER BY total_spent DESC) as rank,
        PERCENT_RANK() OVER (ORDER BY total_spent DESC) as percentile
    FROM (
        SELECT user_id, SUM(amount) as total_spent
        FROM orders
        WHERE status = 'completed'
        GROUP BY user_id
    ) user_totals
""")
```

### Java Query Execution

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.adapters.PostgreSQLAdapter;
import java.util.List;
import java.util.Map;

public class QueryExample {
    public static void main(String[] args) {
        PostgreSQLAdapter db = new PostgreSQLAdapter(PostgreSQLConfig.builder()
            .host("localhost")
            .port(5432)
            .database("myapp")
            .user("postgres")
            .password("secret")
            .build());
        
        TuskLang parser = new TuskLang();
        parser.setDatabaseAdapter(db);
        
        // Execute queries directly
        List<Map<String, Object>> users = parser.query("SELECT * FROM users LIMIT 10");
        System.out.println("Users: " + users);
        
        // Execute with parameters
        List<Map<String, Object>> recentOrders = parser.query(
            "SELECT * FROM orders WHERE created_at > ?", 
            java.time.LocalDateTime.now().minusDays(7)
        );
        System.out.println("Recent orders: " + recentOrders);
        
        // Execute single value query
        Long userCount = parser.querySingle("SELECT COUNT(*) FROM users", Long.class);
        System.out.println("User count: " + userCount);
    }
}
```

## 🔄 ORM Integration

### JPA/Hibernate Integration

```java
import org.tusklang.java.TuskLang;
import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;
import org.springframework.stereotype.Service;
import jakarta.persistence.*;

@SpringBootApplication
public class OrmExample {
    public static void main(String[] args) {
        SpringApplication.run(OrmExample.class, args);
    }
}

@Entity
@Table(name = "users")
public class User {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;
    
    @Column(name = "name")
    private String name;
    
    @Column(name = "email")
    private String email;
    
    @Column(name = "active")
    private boolean active;
    
    // Getters and setters
}

@Repository
public interface UserRepository extends JpaRepository<User, Long> {
    List<User> findByActiveTrue();
    long countByActiveTrue();
}

@Service
public class UserService {
    private final UserRepository userRepository;
    private final TuskLang parser;
    
    public UserService(UserRepository userRepository) {
        this.userRepository = userRepository;
        this.parser = new TuskLang();
    }
    
    public Map<String, Object> getUserStats() {
        // Use TuskLang with JPA
        String tskContent = """
            [stats]
            total_users: @query("SELECT COUNT(*) FROM users")
            active_users: @query("SELECT COUNT(*) FROM users WHERE active = true")
            recent_users: @query("SELECT COUNT(*) FROM users WHERE created_at > ?", @date.subtract("7d"))
            """;
        
        return parser.parse(tskContent);
    }
}
```

### MyBatis Integration

```java
import org.tusklang.java.TuskLang;
import org.apache.ibatis.annotations.Mapper;
import org.apache.ibatis.annotations.Select;
import java.util.List;
import java.util.Map;

@Mapper
public interface UserMapper {
    @Select("SELECT COUNT(*) FROM users")
    long getUserCount();
    
    @Select("SELECT COUNT(*) FROM users WHERE active = true")
    long getActiveUserCount();
    
    @Select("SELECT * FROM users WHERE created_at > #{date}")
    List<User> getRecentUsers(java.time.LocalDateTime date);
}

@Service
public class MyBatisUserService {
    private final UserMapper userMapper;
    private final TuskLang parser;
    
    public MyBatisUserService(UserMapper userMapper) {
        this.userMapper = userMapper;
        this.parser = new TuskLang();
    }
    
    public Map<String, Object> getUserStats() {
        String tskContent = """
            [stats]
            total_users: @query("SELECT COUNT(*) FROM users")
            active_users: @query("SELECT COUNT(*) FROM users WHERE active = true")
            """;
        
        return parser.parse(tskContent);
    }
}
```

## 🔒 Transaction Management

### Manual Transaction Control

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.adapters.PostgreSQLAdapter;
import java.sql.Connection;

public class TransactionExample {
    public static void main(String[] args) {
        PostgreSQLAdapter db = new PostgreSQLAdapter(PostgreSQLConfig.builder()
            .host("localhost")
            .port(5432)
            .database("myapp")
            .user("postgres")
            .password("secret")
            .build());
        
        TuskLang parser = new TuskLang();
        parser.setDatabaseAdapter(db);
        
        // Execute in transaction
        parser.executeInTransaction(connection -> {
            // Execute multiple queries in transaction
            parser.query("INSERT INTO users (name, email) VALUES (?, ?)", "Alice", "alice@example.com");
            parser.query("INSERT INTO user_profiles (user_id, bio) VALUES (LAST_INSERT_ID(), ?)", "Software Developer");
            
            return true; // Commit transaction
        });
    }
}
```

### Spring Transaction Integration

```java
import org.springframework.transaction.annotation.Transactional;
import org.springframework.stereotype.Service;

@Service
public class TransactionalUserService {
    private final TuskLang parser;
    
    public TransactionalUserService() {
        this.parser = new TuskLang();
    }
    
    @Transactional
    public void createUserWithProfile(String name, String email, String bio) {
        // These queries will be executed in a Spring-managed transaction
        parser.query("INSERT INTO users (name, email) VALUES (?, ?)", name, email);
        parser.query("INSERT INTO user_profiles (user_id, bio) VALUES (LAST_INSERT_ID(), ?)", bio);
    }
    
    @Transactional(readOnly = true)
    public Map<String, Object> getUserStats() {
        String tskContent = """
            [stats]
            total_users: @query("SELECT COUNT(*) FROM users")
            active_users: @query("SELECT COUNT(*) FROM users WHERE active = true")
            """;
        
        return parser.parse(tskContent);
    }
}
```

## 📊 Advanced Database Features

### Connection Pooling

```java
import org.tusklang.java.adapters.PostgreSQLAdapter;
import org.tusklang.java.config.PostgreSQLConfig;
import com.zaxxer.hikari.HikariConfig;
import com.zaxxer.hikari.HikariDataSource;

public class ConnectionPoolExample {
    public static void main(String[] args) {
        // Configure connection pool
        HikariConfig hikariConfig = new HikariConfig();
        hikariConfig.setJdbcUrl("jdbc:postgresql://localhost:5432/myapp");
        hikariConfig.setUsername("postgres");
        hikariConfig.setPassword("secret");
        hikariConfig.setMaximumPoolSize(20);
        hikariConfig.setMinimumIdle(5);
        hikariConfig.setConnectionTimeout(30000);
        hikariConfig.setIdleTimeout(600000);
        hikariConfig.setMaxLifetime(1800000);
        
        HikariDataSource dataSource = new HikariDataSource(hikariConfig);
        
        // Create adapter with connection pool
        PostgreSQLAdapter db = new PostgreSQLAdapter(dataSource);
        
        TuskLang parser = new TuskLang();
        parser.setDatabaseAdapter(db);
        
        // Use with connection pooling
        Map<String, Object> config = parser.parseFile("config.tsk");
        System.out.println("Config with connection pool: " + config);
    }
}
```

### Read Replicas

```java
import org.tusklang.java.adapters.PostgreSQLAdapter;
import org.tusklang.java.config.PostgreSQLConfig;
import java.util.List;

public class ReadReplicaExample {
    public static void main(String[] args) {
        // Configure primary database
        PostgreSQLAdapter primary = new PostgreSQLAdapter(PostgreSQLConfig.builder()
            .host("primary-db.example.com")
            .port(5432)
            .database("myapp")
            .user("postgres")
            .password("secret")
            .build());
        
        // Configure read replicas
        List<PostgreSQLAdapter> replicas = List.of(
            new PostgreSQLAdapter(PostgreSQLConfig.builder()
                .host("replica1.example.com")
                .port(5432)
                .database("myapp")
                .user("readonly")
                .password("secret")
                .build()),
            new PostgreSQLAdapter(PostgreSQLConfig.builder()
                .host("replica2.example.com")
                .port(5432)
                .database("myapp")
                .user("readonly")
                .password("secret")
                .build())
        );
        
        // Create adapter with read replicas
        PostgreSQLAdapter db = new PostgreSQLAdapter(primary, replicas);
        
        TuskLang parser = new TuskLang();
        parser.setDatabaseAdapter(db);
        
        // Queries will automatically use read replicas for SELECT operations
        Map<String, Object> config = parser.parseFile("config.tsk");
        System.out.println("Config with read replicas: " + config);
    }
}
```

### Database Migration Integration

```java
import org.tusklang.java.TuskLang;
import org.flywaydb.core.Flyway;
import org.tusklang.java.adapters.PostgreSQLAdapter;

public class MigrationExample {
    public static void main(String[] args) {
        // Run database migrations
        Flyway flyway = Flyway.configure()
            .dataSource("jdbc:postgresql://localhost:5432/myapp", "postgres", "secret")
            .load();
        
        flyway.migrate();
        
        // Create TuskLang parser
        PostgreSQLAdapter db = new PostgreSQLAdapter(PostgreSQLConfig.builder()
            .host("localhost")
            .port(5432)
            .database("myapp")
            .user("postgres")
            .password("secret")
            .build());
        
        TuskLang parser = new TuskLang();
        parser.setDatabaseAdapter(db);
        
        // Use migrated database
        Map<String, Object> config = parser.parseFile("config.tsk");
        System.out.println("Config with migrations: " + config);
    }
}
```

## 🔧 Performance Optimization

### Query Caching

```tsk
# Cache expensive queries
[expensive_data]
user_analytics: @cache("5m", "get_user_analytics")
order_statistics: @cache("1h", "get_order_statistics")
revenue_metrics: @cache("30m", "get_revenue_metrics")
```

### Lazy Loading

```tsk
# Lazy load data when needed
[lazy_data]
user_profiles: @lazy("load_user_profiles")
detailed_analytics: @lazy("load_detailed_analytics")
```

### Parallel Queries

```tsk
# Execute queries in parallel
[parallel_data]
data1: @async("query1")
data2: @async("query2")
data3: @async("query3")
```

### Java Performance Implementation

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.cache.CacheManager;
import java.util.concurrent.CompletableFuture;

public class PerformanceExample {
    public static void main(String[] args) {
        // Configure cache
        CacheManager cacheManager = new CacheManager();
        cacheManager.setDefaultTtl(300); // 5 minutes
        
        TuskLang parser = new TuskLang();
        parser.setCacheManager(cacheManager);
        
        // Parallel query execution
        CompletableFuture<Map<String, Object>> future1 = parser.parseAsync("config1.tsk");
        CompletableFuture<Map<String, Object>> future2 = parser.parseAsync("config2.tsk");
        CompletableFuture<Map<String, Object>> future3 = parser.parseAsync("config3.tsk");
        
        // Wait for all to complete
        CompletableFuture.allOf(future1, future2, future3).join();
        
        Map<String, Object> config1 = future1.get();
        Map<String, Object> config2 = future2.get();
        Map<String, Object> config3 = future3.get();
        
        System.out.println("All configurations loaded in parallel");
    }
}
```

## 🔒 Security Best Practices

### Parameterized Queries

```tsk
# Always use parameterized queries
[secure_queries]
user_by_id: @query("SELECT * FROM users WHERE id = ?", @request.user_id)
orders_by_status: @query("SELECT * FROM orders WHERE status = ?", "pending")
recent_data: @query("SELECT * FROM data WHERE created_at > ?", @date.subtract("7d"))
```

### Connection Security

```java
import org.tusklang.java.adapters.PostgreSQLAdapter;
import org.tusklang.java.config.PostgreSQLConfig;

public class SecurityExample {
    public static void main(String[] args) {
        // Secure database configuration
        PostgreSQLAdapter db = new PostgreSQLAdapter(PostgreSQLConfig.builder()
            .host("localhost")
            .port(5432)
            .database("myapp")
            .user("app_user") // Use dedicated user
            .password(System.getenv("DB_PASSWORD")) // Use environment variable
            .ssl(true) // Enable SSL
            .sslMode("require") // Require SSL
            .build());
        
        TuskLang parser = new TuskLang();
        parser.setDatabaseAdapter(db);
        
        // Use secure configuration
        Map<String, Object> config = parser.parseFile("config.tsk");
        System.out.println("Secure configuration loaded");
    }
}
```

## 🧪 Testing Database Integration

### Unit Tests

```java
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.BeforeEach;
import static org.junit.jupiter.api.Assertions.*;
import org.tusklang.java.TuskLang;
import org.tusklang.java.adapters.SQLiteAdapter;

class DatabaseIntegrationTest {
    
    private TuskLang parser;
    private SQLiteAdapter db;
    
    @BeforeEach
    void setUp() {
        // Setup test database
        db = new SQLiteAdapter(":memory:");
        parser = new TuskLang();
        parser.setDatabaseAdapter(db);
        
        // Setup test data
        db.execute("""
            CREATE TABLE users (id INTEGER, name TEXT, active BOOLEAN);
            INSERT INTO users VALUES (1, 'Alice', 1), (2, 'Bob', 0);
            """);
    }
    
    @Test
    void testDatabaseQueries() {
        String tskContent = """
            [users]
            count: @query("SELECT COUNT(*) FROM users")
            active_count: @query("SELECT COUNT(*) FROM users WHERE active = 1")
            """;
        
        Map<String, Object> data = parser.parse(tskContent);
        
        assertEquals(2, data.get("users"));
        assertEquals(1, data.get("users"));
    }
    
    @Test
    void testParameterizedQueries() {
        String tskContent = """
            [user]
            alice: @query("SELECT * FROM users WHERE name = ?", "Alice")
            """;
        
        Map<String, Object> data = parser.parse(tskContent);
        assertNotNull(data.get("user"));
    }
}
```

## 🔧 Troubleshooting

### Common Database Issues

1. **Connection Timeout**
```java
// Increase connection timeout
PostgreSQLConfig config = PostgreSQLConfig.builder()
    .host("localhost")
    .port(5432)
    .database("myapp")
    .user("postgres")
    .password("secret")
    .connectionTimeout(60) // 60 seconds
    .build();
```

2. **Query Performance**
```java
// Enable query logging
parser.setDebug(true);

// Use query execution time monitoring
long startTime = System.currentTimeMillis();
Map<String, Object> result = parser.parse(tskContent);
long endTime = System.currentTimeMillis();
System.out.println("Query execution time: " + (endTime - startTime) + "ms");
```

3. **Memory Issues**
```java
// Use streaming for large result sets
List<Map<String, Object>> results = parser.queryStream("SELECT * FROM large_table");
results.forEach(row -> {
    // Process each row individually
    System.out.println("Processing row: " + row);
});
```

## 📚 Next Steps

1. **Master @query operators** - Complex queries and aggregations
2. **Integrate with ORM frameworks** - JPA, MyBatis, Hibernate
3. **Implement caching strategies** - Query caching and result caching
4. **Add transaction management** - ACID compliance and rollback handling
5. **Optimize performance** - Connection pooling and query optimization

---

**"We don't bow to any king"** - You now have complete mastery of database integration in TuskLang Java! From simple queries to complex ORM integration, you can build powerful applications with seamless database connectivity. 