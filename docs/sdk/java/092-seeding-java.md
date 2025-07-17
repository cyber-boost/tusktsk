# 🌱 Database Seeding in TuskLang Java

**"We don't bow to any king" - Seed your data like a Java master**

TuskLang Java provides sophisticated database seeding capabilities that integrate seamlessly with Spring Boot, JPA, and modern Java patterns. Create, manage, and execute database seeds with enterprise-grade reliability and performance.

## 🎯 Overview

Database seeding in TuskLang Java combines the power of Java database technologies with TuskLang's configuration system. From JPA entity seeding to Spring Boot data initialization, we'll show you how to build robust, scalable database seeding systems.

## 🔧 Core Seeding Features

### 1. JPA Entity Seeding
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.seeding.TuskEntitySeeder;
import javax.persistence.*;
import java.util.Map;
import java.util.List;

@Entity
@Table(name = "users")
public class User {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;
    
    @Column(nullable = false)
    private String name;
    
    @Column(unique = true, nullable = false)
    private String email;
    
    @Column(name = "role")
    private String role = "user";
    
    @Column(name = "active")
    private boolean active = true;
    
    @Column(name = "created_at")
    private java.time.LocalDateTime createdAt;
    
    // Constructors, getters, setters
    public User() {}
    
    public User(String name, String email, String role) {
        this.name = name;
        this.email = email;
        this.role = role;
        this.createdAt = java.time.LocalDateTime.now();
    }
    
    // Getters and setters
    public Long getId() { return id; }
    public void setId(Long id) { this.id = id; }
    
    public String getName() { return name; }
    public void setName(String name) { this.name = name; }
    
    public String getEmail() { return email; }
    public void setEmail(String email) { this.email = email; }
    
    public String getRole() { return role; }
    public void setRole(String role) { this.role = role; }
    
    public boolean isActive() { return active; }
    public void setActive(boolean active) { this.active = active; }
    
    public java.time.LocalDateTime getCreatedAt() { return createdAt; }
    public void setCreatedAt(java.time.LocalDateTime createdAt) { this.createdAt = createdAt; }
}

public class EntitySeedingExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        
        String tskContent = """
            [entity_seeding]
            # JPA Entity Seeding Configuration
            enable_transaction_management: true
            enable_rollback_on_error: true
            enable_duplicate_handling: true
            
            [user_seeding]
            # User entity seeding
            entity_class: "com.example.User"
            batch_size: 100
            clear_before_seed: false
            
            seed_data: [
                {
                    name: "Admin User",
                    email: "admin@example.com",
                    role: "admin",
                    active: true
                },
                {
                    name: "John Doe",
                    email: "john@example.com",
                    role: "user",
                    active: true
                },
                {
                    name: "Jane Smith",
                    email: "jane@example.com",
                    role: "premium",
                    active: true
                }
            ]
            
            seed_methods: {
                create_user_seed: """
                    function createUserSeed(data) {
                        return {
                            name: data.name,
                            email: data.email,
                            role: data.role || "user",
                            active: data.active !== false,
                            created_at: @date.now()
                        }
                    }
                """
                
                create_admin_users: """
                    function createAdminUsers() {
                        return [
                            {
                                name: "Super Admin",
                                email: "superadmin@example.com",
                                role: "admin",
                                active: true
                            },
                            {
                                name: "System Admin",
                                email: "sysadmin@example.com",
                                role: "admin",
                                active: true
                            }
                        ]
                    }
                """
                
                create_test_users: """
                    function createTestUsers(count) {
                        let users = [];
                        for (let i = 1; i <= count; i++) {
                            users.push({
                                name: "Test User " + i,
                                email: "test" + i + "@example.com",
                                role: "user",
                                active: true
                            });
                        }
                        return users;
                    }
                """
            }
            
            [order_seeding]
            # Order entity seeding
            entity_class: "com.example.Order"
            batch_size: 50
            clear_before_seed: false
            
            seed_data: [
                {
                    user_id: 1,
                    total_amount: 150.00,
                    status: "completed",
                    items: [
                        { product: "Product A", price: 75.00, quantity: 2 }
                    ]
                },
                {
                    user_id: 2,
                    total_amount: 299.99,
                    status: "pending",
                    items: [
                        { product: "Product B", price: 299.99, quantity: 1 }
                    ]
                }
            ]
            
            seed_methods: {
                create_order_seed: """
                    function createOrderSeed(data) {
                        return {
                            user_id: data.user_id,
                            total_amount: data.total_amount,
                            status: data.status || "pending",
                            items: data.items,
                            created_at: @date.now()
                        }
                    }
                """
                
                create_sample_orders: """
                    function createSampleOrders(userIds) {
                        let orders = [];
                        let products = ["Product A", "Product B", "Product C", "Product D"];
                        
                        for (let userId of userIds) {
                            let orderCount = Math.floor(Math.random() * 3) + 1;
                            for (let i = 0; i < orderCount; i++) {
                                let product = products[Math.floor(Math.random() * products.length)];
                                let price = Math.floor(Math.random() * 100) + 10;
                                let quantity = Math.floor(Math.random() * 3) + 1;
                                
                                orders.push({
                                    user_id: userId,
                                    total_amount: price * quantity,
                                    status: ["pending", "completed", "cancelled"][Math.floor(Math.random() * 3)],
                                    items: [
                                        { product: product, price: price, quantity: quantity }
                                    ]
                                });
                            }
                        }
                        return orders;
                    }
                """
            }
            """;
        
        Map<String, Object> config = parser.parse(tskContent);
        
        // Initialize entity seeder
        TuskEntitySeeder entitySeeder = new TuskEntitySeeder();
        entitySeeder.configure(config);
        
        // Seed entities
        List<User> users = entitySeeder.seedEntities("user_seeding", "create_user_seed");
        List<User> adminUsers = entitySeeder.seedEntities("user_seeding", "create_admin_users");
        List<User> testUsers = entitySeeder.seedEntities("user_seeding", "create_test_users", Map.of("count", 10));
        
        List<Order> orders = entitySeeder.seedEntities("order_seeding", "create_order_seed");
        List<Order> sampleOrders = entitySeeder.seedEntities("order_seeding", "create_sample_orders", 
            Map.of("userIds", List.of(1L, 2L, 3L)));
        
        System.out.println("Seeded " + users.size() + " users");
        System.out.println("Seeded " + adminUsers.size() + " admin users");
        System.out.println("Seeded " + testUsers.size() + " test users");
        System.out.println("Seeded " + orders.size() + " orders");
        System.out.println("Seeded " + sampleOrders.size() + " sample orders");
    }
}
```

### 2. Spring Boot Data Seeding
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.seeding.TuskSpringSeeder;
import org.springframework.boot.CommandLineRunner;
import org.springframework.stereotype.Component;
import java.util.Map;
import java.util.List;

@Component
public class DataSeeder implements CommandLineRunner {
    private final TuskSpringSeeder springSeeder;
    
    public DataSeeder(TuskSpringSeeder springSeeder) {
        this.springSeeder = springSeeder;
    }
    
    @Override
    public void run(String... args) throws Exception {
        // Seed data on application startup
        springSeeder.seedAll();
    }
}

public class SpringSeedingExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        
        String tskContent = """
            [spring_seeding]
            # Spring Boot Seeding Configuration
            enable_on_startup: true
            enable_conditional_seeding: true
            enable_environment_specific: true
            
            [startup_seeding]
            # Seeding on application startup
            seed_on_startup: true
            seed_environment: @env("SPRING_PROFILES_ACTIVE", "development")
            
            seed_conditions: {
                development: true
                test: true
                staging: false
                production: false
            }
            
            [user_service_seeding]
            # User service seeding
            service_class: "com.example.UserService"
            repository_class: "com.example.UserRepository"
            
            seed_methods: {
                seed_default_users: """
                    function seedDefaultUsers(userService) {
                        let users = [
                            {
                                name: "Default Admin",
                                email: "admin@example.com",
                                role: "admin"
                            },
                            {
                                name: "Default User",
                                email: "user@example.com",
                                role: "user"
                            }
                        ];
                        
                        for (let userData of users) {
                            try {
                                userService.createUser(userData.name, userData.email, userData.role);
                            } catch (Exception e) {
                                // User might already exist
                                console.log("User already exists: " + userData.email);
                            }
                        }
                        
                        return users.length;
                    }
                """
                
                seed_test_data: """
                    function seedTestData(userService, count) {
                        let created = 0;
                        for (let i = 1; i <= count; i++) {
                            try {
                                userService.createUser(
                                    "Test User " + i,
                                    "test" + i + "@example.com",
                                    "user"
                                );
                                created++;
                            } catch (Exception e) {
                                // Skip duplicates
                            }
                        }
                        return created;
                    }
                """
            }
            
            [order_service_seeding]
            # Order service seeding
            service_class: "com.example.OrderService"
            repository_class: "com.example.OrderRepository"
            
            seed_methods: {
                seed_sample_orders: """
                    function seedSampleOrders(orderService, userIds) {
                        let orders = [];
                        let products = ["Laptop", "Phone", "Tablet", "Headphones"];
                        
                        for (let userId of userIds) {
                            let orderCount = Math.floor(Math.random() * 5) + 1;
                            for (let i = 0; i < orderCount; i++) {
                                let product = products[Math.floor(Math.random() * products.length)];
                                let price = Math.floor(Math.random() * 1000) + 100;
                                let quantity = Math.floor(Math.random() * 3) + 1;
                                
                                let order = orderService.createOrder(userId, [
                                    { product: product, price: price, quantity: quantity }
                                ]);
                                orders.push(order);
                            }
                        }
                        
                        return orders.length;
                    }
                """
            }
            
            [configuration_seeding]
            # Configuration seeding
            config_class: "com.example.AppConfig"
            
            seed_methods: {
                seed_system_config: """
                    function seedSystemConfig(configService) {
                        let configs = [
                            { key: "app.name", value: "TuskLang Java App" },
                            { key: "app.version", value: "1.0.0" },
                            { key: "app.environment", value: @env("SPRING_PROFILES_ACTIVE") },
                            { key: "database.max_connections", value: "50" },
                            { key: "cache.ttl", value: "300" }
                        ];
                        
                        for (let config of configs) {
                            configService.setConfig(config.key, config.value);
                        }
                        
                        return configs.length;
                    }
                """
            }
            """;
        
        Map<String, Object> config = parser.parse(tskContent);
        
        // Initialize Spring seeder
        TuskSpringSeeder springSeeder = new TuskSpringSeeder();
        springSeeder.configure(config);
        
        // Seed data using Spring services
        int userCount = springSeeder.seedService("user_service_seeding", "seed_default_users");
        int testUserCount = springSeeder.seedService("user_service_seeding", "seed_test_data", Map.of("count", 20));
        int orderCount = springSeeder.seedService("order_service_seeding", "seed_sample_orders", 
            Map.of("userIds", List.of(1L, 2L, 3L)));
        int configCount = springSeeder.seedService("configuration_seeding", "seed_system_config");
        
        System.out.println("Seeded " + userCount + " default users");
        System.out.println("Seeded " + testUserCount + " test users");
        System.out.println("Seeded " + orderCount + " sample orders");
        System.out.println("Seeded " + configCount + " system configurations");
    }
}
```

### 3. Database Migration Seeding
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.seeding.TuskMigrationSeeder;
import java.util.Map;
import java.util.List;

public class MigrationSeedingExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        
        String tskContent = """
            [migration_seeding]
            # Database Migration Seeding Configuration
            enable_version_control: true
            enable_rollback_support: true
            enable_environment_specific: true
            
            [migration_001_initial_users]
            # Initial user migration
            version: "001"
            description: "Create initial users"
            environment: ["development", "test", "staging"]
            
            up_method: """
                function migrateUp(db) {
                    let users = [
                        {
                            name: "System Admin",
                            email: "admin@example.com",
                            role: "admin",
                            active: true,
                            created_at: @date.now()
                        },
                        {
                            name: "Default User",
                            email: "user@example.com",
                            role: "user",
                            active: true,
                            created_at: @date.now()
                        }
                    ];
                    
                    for (let user of users) {
                        db.execute("""
                            INSERT INTO users (name, email, role, active, created_at)
                            VALUES (?, ?, ?, ?, ?)
                        """, [user.name, user.email, user.role, user.active, user.created_at]);
                    }
                    
                    return users.length;
                }
            """
            
            down_method: """
                function migrateDown(db) {
                    db.execute("DELETE FROM users WHERE email IN (?, ?)", 
                        ["admin@example.com", "user@example.com"]);
                    return 2;
                }
            """
            
            [migration_002_sample_orders]
            # Sample orders migration
            version: "002"
            description: "Create sample orders"
            environment: ["development", "test"]
            
            up_method: """
                function migrateUp(db) {
                    let orders = [
                        {
                            user_id: 1,
                            total_amount: 299.99,
                            status: "completed",
                            created_at: @date.now()
                        },
                        {
                            user_id: 2,
                            total_amount: 149.99,
                            status: "pending",
                            created_at: @date.now()
                        }
                    ];
                    
                    for (let order of orders) {
                        db.execute("""
                            INSERT INTO orders (user_id, total_amount, status, created_at)
                            VALUES (?, ?, ?, ?)
                        """, [order.user_id, order.total_amount, order.status, order.created_at]);
                    }
                    
                    return orders.length;
                }
            """
            
            down_method: """
                function migrateDown(db) {
                    db.execute("DELETE FROM orders WHERE user_id IN (1, 2)");
                    return 2;
                }
            """
            
            [migration_003_system_config]
            # System configuration migration
            version: "003"
            description: "Create system configurations"
            environment: ["development", "test", "staging", "production"]
            
            up_method: """
                function migrateUp(db) {
                    let configs = [
                        { key: "app.name", value: "TuskLang Java App" },
                        { key: "app.version", value: "1.0.0" },
                        { key: "database.max_connections", value: "50" },
                        { key: "cache.ttl", value: "300" }
                    ];
                    
                    for (let config of configs) {
                        db.execute("""
                            INSERT INTO system_configs (config_key, config_value, created_at)
                            VALUES (?, ?, ?)
                        """, [config.key, config.value, @date.now()]);
                    }
                    
                    return configs.length;
                }
            """
            
            down_method: """
                function migrateDown(db) {
                    db.execute("DELETE FROM system_configs WHERE config_key IN (?, ?, ?, ?)",
                        ["app.name", "app.version", "database.max_connections", "cache.ttl"]);
                    return 4;
                }
            """
            """;
        
        Map<String, Object> config = parser.parse(tskContent);
        
        // Initialize migration seeder
        TuskMigrationSeeder migrationSeeder = new TuskMigrationSeeder();
        migrationSeeder.configure(config);
        
        // Execute migrations
        int userCount = migrationSeeder.migrateUp("migration_001_initial_users");
        int orderCount = migrationSeeder.migrateUp("migration_002_sample_orders");
        int configCount = migrationSeeder.migrateUp("migration_003_system_config");
        
        System.out.println("Migration 001: Created " + userCount + " users");
        System.out.println("Migration 002: Created " + orderCount + " orders");
        System.out.println("Migration 003: Created " + configCount + " configurations");
        
        // Rollback if needed
        // migrationSeeder.migrateDown("migration_003_system_config");
    }
}
```

### 4. Test Data Seeding
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.seeding.TuskTestSeeder;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import java.util.Map;
import java.util.List;

public class TestSeedingExample {
    private TuskTestSeeder testSeeder;
    
    @BeforeEach
    void setUp() {
        TuskLang parser = new TuskLang();
        
        String tskContent = """
            [test_seeding]
            # Test Data Seeding Configuration
            enable_test_isolation: true
            enable_cleanup_after_test: true
            enable_random_data: true
            
            [test_user_seeding]
            # Test user seeding
            entity_class: "com.example.User"
            clear_before_seed: true
            
            seed_methods: {
                create_test_user: """
                    function createTestUser(name, email, role) {
                        return {
                            name: name || "Test User",
                            email: email || "test@example.com",
                            role: role || "user",
                            active: true,
                            created_at: @date.now()
                        }
                    }
                """
                
                create_random_users: """
                    function createRandomUsers(count) {
                        let users = [];
                        let names = ["Alice", "Bob", "Charlie", "Diana", "Eve", "Frank"];
                        let roles = ["user", "premium", "admin"];
                        
                        for (let i = 0; i < count; i++) {
                            let name = names[Math.floor(Math.random() * names.length)];
                            let role = roles[Math.floor(Math.random() * roles.length)];
                            let email = name.toLowerCase() + i + "@test.com";
                            
                            users.push({
                                name: name + " " + i,
                                email: email,
                                role: role,
                                active: true,
                                created_at: @date.now()
                            });
                        }
                        
                        return users;
                    }
                """
            }
            
            [test_order_seeding]
            # Test order seeding
            entity_class: "com.example.Order"
            clear_before_seed: true
            
            seed_methods: {
                create_test_order: """
                    function createTestOrder(userId, totalAmount, status) {
                        return {
                            user_id: userId,
                            total_amount: totalAmount || 100.00,
                            status: status || "pending",
                            created_at: @date.now()
                        }
                    }
                """
                
                create_random_orders: """
                    function createRandomOrders(userIds, count) {
                        let orders = [];
                        let statuses = ["pending", "completed", "cancelled"];
                        
                        for (let i = 0; i < count; i++) {
                            let userId = userIds[Math.floor(Math.random() * userIds.length)];
                            let totalAmount = Math.floor(Math.random() * 1000) + 10;
                            let status = statuses[Math.floor(Math.random() * statuses.length)];
                            
                            orders.push({
                                user_id: userId,
                                total_amount: totalAmount,
                                status: status,
                                created_at: @date.now()
                            });
                        }
                        
                        return orders;
                    }
                """
            }
            
            [test_cleanup]
            # Test cleanup methods
            cleanup_methods: {
                cleanup_users: """
                    function cleanupUsers() {
                        // Clean up test users
                        return "DELETE FROM users WHERE email LIKE '%@test.com'";
                    }
                """
                
                cleanup_orders: """
                    function cleanupOrders() {
                        // Clean up test orders
                        return "DELETE FROM orders WHERE user_id IN (SELECT id FROM users WHERE email LIKE '%@test.com')";
                    }
                """
            }
            """;
        
        Map<String, Object> config = parser.parse(tskContent);
        
        // Initialize test seeder
        testSeeder = new TuskTestSeeder();
        testSeeder.configure(config);
    }
    
    @Test
    void testUserCreation() {
        // Create test user
        User testUser = testSeeder.createEntity("test_user_seeding", "create_test_user", 
            Map.of("name", "Test User", "email", "test@example.com", "role", "user"));
        
        // Assert user was created
        assert testUser.getName().equals("Test User");
        assert testUser.getEmail().equals("test@example.com");
        assert testUser.getRole().equals("user");
    }
    
    @Test
    void testRandomDataGeneration() {
        // Create random users
        List<User> randomUsers = testSeeder.createEntities("test_user_seeding", "create_random_users", 
            Map.of("count", 5));
        
        // Create random orders for these users
        List<Long> userIds = randomUsers.stream().map(User::getId).collect(java.util.stream.Collectors.toList());
        List<Order> randomOrders = testSeeder.createEntities("test_order_seeding", "create_random_orders",
            Map.of("userIds", userIds, "count", 10));
        
        // Assert data was created
        assert randomUsers.size() == 5;
        assert randomOrders.size() == 10;
    }
    
    @Test
    void testCleanup() {
        // Create test data
        testSeeder.createEntities("test_user_seeding", "create_random_users", Map.of("count", 3));
        
        // Clean up test data
        testSeeder.cleanup("test_cleanup", "cleanup_users");
        testSeeder.cleanup("test_cleanup", "cleanup_orders");
        
        // Verify cleanup
        // Add verification logic here
    }
}
```

## 🚀 Advanced Seeding Patterns

### 1. Conditional Seeding
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.seeding.TuskConditionalSeeder;
import java.util.Map;

public class ConditionalSeedingExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        
        String tskContent = """
            [conditional_seeding]
            # Conditional Seeding Configuration
            enable_environment_check: true
            enable_data_validation: true
            enable_conditional_logic: true
            
            [environment_specific_seeding]
            # Environment-specific seeding
            development: {
                seed_test_data: true
                seed_mock_data: true
                seed_debug_data: true
            }
            
            test: {
                seed_test_data: true
                seed_mock_data: true
                seed_debug_data: false
            }
            
            staging: {
                seed_test_data: false
                seed_mock_data: false
                seed_debug_data: false
            }
            
            production: {
                seed_test_data: false
                seed_mock_data: false
                seed_debug_data: false
            }
            
            [conditional_methods]
            # Conditional seeding methods
            seed_environment_data: """
                function seedEnvironmentData(environment) {
                    let config = @conditional_seeding.environment_specific_seeding[environment];
                    
                    if (config.seed_test_data) {
                        seedTestData();
                    }
                    
                    if (config.seed_mock_data) {
                        seedMockData();
                    }
                    
                    if (config.seed_debug_data) {
                        seedDebugData();
                    }
                    
                    return {
                        test_data: config.seed_test_data,
                        mock_data: config.seed_mock_data,
                        debug_data: config.seed_debug_data
                    };
                }
            """
            
            seed_based_on_conditions: """
                function seedBasedOnConditions(conditions) {
                    let seeded = [];
                    
                    if (conditions.has_users === false) {
                        seeded.push(seedDefaultUsers());
                    }
                    
                    if (conditions.has_orders === false) {
                        seeded.push(seedSampleOrders());
                    }
                    
                    if (conditions.has_config === false) {
                        seeded.push(seedSystemConfig());
                    }
                    
                    return seeded;
                }
            """
            """;
        
        Map<String, Object> config = parser.parse(tskContent);
        
        // Initialize conditional seeder
        TuskConditionalSeeder conditionalSeeder = new TuskConditionalSeeder();
        conditionalSeeder.configure(config);
        
        // Get current environment
        String environment = System.getProperty("spring.profiles.active", "development");
        
        // Seed based on environment
        Map<String, Object> environmentResult = conditionalSeeder.seedConditional("conditional_methods", 
            "seed_environment_data", Map.of("environment", environment));
        
        // Seed based on data conditions
        Map<String, Object> conditions = Map.of(
            "has_users", false,
            "has_orders", false,
            "has_config", false
        );
        
        List<String> conditionalResult = conditionalSeeder.seedConditional("conditional_methods", 
            "seed_based_on_conditions", Map.of("conditions", conditions));
        
        System.out.println("Environment seeding result: " + environmentResult);
        System.out.println("Conditional seeding result: " + conditionalResult);
    }
}
```

### 2. Bulk Seeding with Performance Optimization
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.seeding.TuskBulkSeeder;
import java.util.Map;
import java.util.List;

public class BulkSeedingExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        
        String tskContent = """
            [bulk_seeding]
            # Bulk Seeding Configuration
            enable_batch_processing: true
            enable_parallel_processing: true
            enable_memory_optimization: true
            
            [bulk_user_seeding]
            # Bulk user seeding
            batch_size: 1000
            parallel_threads: 4
            memory_limit: "512MB"
            
            bulk_methods: {
                create_bulk_users: """
                    function createBulkUsers(count) {
                        let users = [];
                        let names = ["Alice", "Bob", "Charlie", "Diana", "Eve", "Frank", "Grace", "Henry"];
                        let domains = ["example.com", "test.com", "demo.com"];
                        
                        for (let i = 0; i < count; i++) {
                            let name = names[i % names.length];
                            let domain = domains[i % domains.length];
                            let email = name.toLowerCase() + i + "@" + domain;
                            
                            users.push({
                                name: name + " " + i,
                                email: email,
                                role: "user",
                                active: true,
                                created_at: @date.now()
                            });
                        }
                        
                        return users;
                    }
                """
                
                create_bulk_orders: """
                    function createBulkOrders(userIds, count) {
                        let orders = [];
                        let products = ["Product A", "Product B", "Product C", "Product D", "Product E"];
                        let statuses = ["pending", "completed", "cancelled"];
                        
                        for (let i = 0; i < count; i++) {
                            let userId = userIds[i % userIds.length];
                            let product = products[i % products.length];
                            let price = Math.floor(Math.random() * 1000) + 10;
                            let quantity = Math.floor(Math.random() * 5) + 1;
                            let status = statuses[i % statuses.length];
                            
                            orders.push({
                                user_id: userId,
                                total_amount: price * quantity,
                                status: status,
                                created_at: @date.now()
                            });
                        }
                        
                        return orders;
                    }
                """
            }
            
            [performance_optimization]
            # Performance optimization settings
            enable_connection_pooling: true
            enable_statement_caching: true
            enable_batch_inserts: true
            
            optimization_settings: {
                connection_pool_size: 20
                statement_cache_size: 100
                batch_size: 1000
                flush_interval: "5s"
            }
            """;
        
        Map<String, Object> config = parser.parse(tskContent);
        
        // Initialize bulk seeder
        TuskBulkSeeder bulkSeeder = new TuskBulkSeeder();
        bulkSeeder.configure(config);
        
        // Create bulk users
        List<User> users = bulkSeeder.bulkSeed("bulk_user_seeding", "create_bulk_users", 
            Map.of("count", 10000));
        
        // Create bulk orders
        List<Long> userIds = users.stream().map(User::getId).collect(java.util.stream.Collectors.toList());
        List<Order> orders = bulkSeeder.bulkSeed("bulk_user_seeding", "create_bulk_orders",
            Map.of("userIds", userIds, "count", 50000));
        
        System.out.println("Bulk seeded " + users.size() + " users");
        System.out.println("Bulk seeded " + orders.size() + " orders");
    }
}
```

## 🔧 Spring Boot Integration

### 1. Spring Boot Configuration
```java
import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.tusklang.java.TuskLang;
import org.tusklang.java.spring.TuskSeedingConfig;

@SpringBootApplication
@Configuration
public class SeedingApplication {
    
    @Bean
    public TuskSeedingConfig tuskSeedingConfig() {
        TuskLang parser = new TuskLang();
        return parser.parseFile("seeding.tsk", TuskSeedingConfig.class);
    }
    
    public static void main(String[] args) {
        SpringApplication.run(SeedingApplication.class, args);
    }
}

@TuskConfig
public class TuskSeedingConfig {
    private EntitySeedingConfig entitySeeding;
    private SpringSeedingConfig springSeeding;
    private MigrationSeedingConfig migrationSeeding;
    private TestSeedingConfig testSeeding;
    
    // Getters and setters
    public EntitySeedingConfig getEntitySeeding() { return entitySeeding; }
    public void setEntitySeeding(EntitySeedingConfig entitySeeding) { this.entitySeeding = entitySeeding; }
    
    public SpringSeedingConfig getSpringSeeding() { return springSeeding; }
    public void setSpringSeeding(SpringSeedingConfig springSeeding) { this.springSeeding = springSeeding; }
    
    public MigrationSeedingConfig getMigrationSeeding() { return migrationSeeding; }
    public void setMigrationSeeding(MigrationSeedingConfig migrationSeeding) { this.migrationSeeding = migrationSeeding; }
    
    public TestSeedingConfig getTestSeeding() { return testSeeding; }
    public void setTestSeeding(TestSeedingConfig testSeeding) { this.testSeeding = testSeeding; }
}
```

## 🎯 Best Practices

### 1. Seeding Design Patterns
```java
// ✅ Use appropriate seeding patterns
- Entity Seeding: For JPA entities
- Service Seeding: For Spring Boot services
- Migration Seeding: For database migrations
- Test Seeding: For test data

// ✅ Implement data validation
- Validate seed data before insertion
- Check for duplicates
- Ensure data integrity

// ✅ Use transaction management
- Wrap seeding in transactions
- Enable rollback on errors
- Maintain data consistency

// ✅ Implement environment-specific seeding
- Different data for different environments
- Conditional seeding based on environment
- Production-safe seeding
```

### 2. Performance Optimization
```java
// 1. Batch Processing
- Use appropriate batch sizes
- Enable parallel processing
- Monitor memory usage

// 2. Connection Management
- Use connection pooling
- Optimize database connections
- Monitor connection usage

// 3. Data Validation
- Validate early and often
- Use efficient validation rules
- Cache validation results

// 4. Error Handling
- Implement proper error handling
- Log seeding errors
- Provide rollback mechanisms
```

## 🚀 Summary

TuskLang Java database seeding provides:

- **JPA Entity Seeding**: Seamless JPA integration with validation
- **Spring Boot Seeding**: Native Spring Boot service seeding
- **Migration Seeding**: Database migration with version control
- **Test Data Seeding**: Comprehensive test data management
- **Conditional Seeding**: Environment-specific data seeding
- **Bulk Seeding**: High-performance bulk data operations
- **Spring Boot Integration**: Native Spring Boot configuration support

With these seeding features, your Java applications will achieve enterprise-grade data initialization while maintaining the flexibility and power of TuskLang configuration.

**"We don't bow to any king" - Seed your data like a Java master with TuskLang!** 