# 🔍 Query Builder in TuskLang Java

**"We don't bow to any king" - Build queries like a Java master**

TuskLang Java provides sophisticated query building capabilities that integrate seamlessly with Spring Boot, JPA, and modern Java patterns. Create, compose, and execute complex database queries with enterprise-grade performance and flexibility.

## 🎯 Overview

Query building in TuskLang Java combines the power of Java query technologies with TuskLang's configuration system. From JPA Criteria API to Spring Data JPA, we'll show you how to build robust, scalable query systems.

## 🔧 Core Query Builder Features

### 1. JPA Criteria Query Builder
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.query.TuskCriteriaQueryBuilder;
import javax.persistence.*;
import javax.persistence.criteria.*;
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
    
    @Column(name = "is_active")
    private boolean active = true;
    
    @Column(name = "created_at")
    private java.time.LocalDateTime createdAt;
    
    @Column(name = "role")
    private String role;
    
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
    
    public boolean isActive() { return active; }
    public void setActive(boolean active) { this.active = active; }
    
    public java.time.LocalDateTime getCreatedAt() { return createdAt; }
    public void setCreatedAt(java.time.LocalDateTime createdAt) { this.createdAt = createdAt; }
    
    public String getRole() { return role; }
    public void setRole(String role) { this.role = role; }
}

public class CriteriaQueryExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        
        String tskContent = """
            [criteria_query_builder]
            # JPA Criteria Query Builder Configuration
            enable_criteria_api: true
            enable_dynamic_queries: true
            enable_query_composition: true
            
            [criteria_config]
            # Criteria configuration
            enable_metamodel: true
            enable_type_safety: true
            enable_query_optimization: true
            
            [user_criteria_queries]
            # User entity criteria queries
            entity_class: "com.example.User"
            
            queries: {
                find_active_users: """
                    function buildActiveUsersQuery(criteriaBuilder, root) {
                        return criteriaBuilder.createQuery(User.class)
                            .select(root)
                            .where(criteriaBuilder.equal(root.get("active"), true))
                            .orderBy(criteriaBuilder.asc(root.get("name")));
                    }
                """
                
                find_users_by_role: """
                    function buildUsersByRoleQuery(criteriaBuilder, root, role) {
                        return criteriaBuilder.createQuery(User.class)
                            .select(root)
                            .where(criteriaBuilder.equal(root.get("role"), role))
                            .orderBy(criteriaBuilder.asc(root.get("name")));
                    }
                """
                
                find_users_by_date_range: """
                    function buildUsersByDateRangeQuery(criteriaBuilder, root, startDate, endDate) {
                        return criteriaBuilder.createQuery(User.class)
                            .select(root)
                            .where(criteriaBuilder.between(root.get("createdAt"), startDate, endDate))
                            .orderBy(criteriaBuilder.desc(root.get("createdAt")));
                    }
                """
                
                find_users_with_complex_criteria: """
                    function buildComplexUserQuery(criteriaBuilder, root, criteria) {
                        let predicates = [];
                        
                        if (criteria.active !== undefined) {
                            predicates.push(criteriaBuilder.equal(root.get("active"), criteria.active));
                        }
                        
                        if (criteria.role) {
                            predicates.push(criteriaBuilder.equal(root.get("role"), criteria.role));
                        }
                        
                        if (criteria.namePattern) {
                            predicates.push(criteriaBuilder.like(
                                criteriaBuilder.lower(root.get("name")),
                                "%" + criteria.namePattern.toLowerCase() + "%"
                            ));
                        }
                        
                        if (criteria.startDate && criteria.endDate) {
                            predicates.push(criteriaBuilder.between(
                                root.get("createdAt"),
                                criteria.startDate,
                                criteria.endDate
                            ));
                        }
                        
                        let query = criteriaBuilder.createQuery(User.class).select(root);
                        
                        if (predicates.length > 0) {
                            query.where(criteriaBuilder.and(predicates.toArray()));
                        }
                        
                        return query.orderBy(criteriaBuilder.asc(root.get("name")));
                    }
                """
                
                count_users_by_criteria: """
                    function buildUserCountQuery(criteriaBuilder, root, criteria) {
                        let predicates = [];
                        
                        if (criteria.active !== undefined) {
                            predicates.push(criteriaBuilder.equal(root.get("active"), criteria.active));
                        }
                        
                        if (criteria.role) {
                            predicates.push(criteriaBuilder.equal(root.get("role"), criteria.role));
                        }
                        
                        let query = criteriaBuilder.createQuery(Long.class)
                            .select(criteriaBuilder.count(root));
                        
                        if (predicates.length > 0) {
                            query.where(criteriaBuilder.and(predicates.toArray()));
                        }
                        
                        return query;
                    }
                """
            }
            
            [order_criteria_queries]
            # Order entity criteria queries
            entity_class: "com.example.Order"
            
            queries: {
                find_orders_by_user: """
                    function buildOrdersByUserQuery(criteriaBuilder, root, userId) {
                        return criteriaBuilder.createQuery(Order.class)
                            .select(root)
                            .where(criteriaBuilder.equal(root.get("user").get("id"), userId))
                            .orderBy(criteriaBuilder.desc(root.get("createdAt")));
                    }
                """
                
                find_orders_by_status_and_amount: """
                    function buildOrdersByStatusAndAmountQuery(criteriaBuilder, root, status, minAmount) {
                        return criteriaBuilder.createQuery(Order.class)
                            .select(root)
                            .where(criteriaBuilder.and(
                                criteriaBuilder.equal(root.get("status"), status),
                                criteriaBuilder.greaterThanOrEqualTo(root.get("totalAmount"), minAmount)
                            ))
                            .orderBy(criteriaBuilder.desc(root.get("totalAmount")));
                    }
                """
                
                find_orders_with_items: """
                    function buildOrdersWithItemsQuery(criteriaBuilder, root, orderRoot) {
                        let join = root.join("items", JoinType.LEFT);
                        
                        return criteriaBuilder.createQuery(Order.class)
                            .select(root)
                            .distinct(true)
                            .orderBy(criteriaBuilder.desc(root.get("createdAt")));
                    }
                """
            }
            
            [criteria_methods]
            # Criteria query methods
            execute_criteria_query: """
                function executeCriteriaQuery(entityManager, queryName, parameters) {
                    let criteriaBuilder = entityManager.getCriteriaBuilder();
                    let criteriaQuery = criteriaBuilder.createQuery(User.class);
                    let root = criteriaQuery.from(User.class);
                    
                    let query;
                    switch (queryName) {
                        case 'find_active_users':
                            query = buildActiveUsersQuery(criteriaBuilder, root);
                            break;
                        case 'find_users_by_role':
                            query = buildUsersByRoleQuery(criteriaBuilder, root, parameters.role);
                            break;
                        case 'find_users_by_date_range':
                            query = buildUsersByDateRangeQuery(criteriaBuilder, root, parameters.startDate, parameters.endDate);
                            break;
                        case 'find_users_with_complex_criteria':
                            query = buildComplexUserQuery(criteriaBuilder, root, parameters);
                            break;
                        case 'count_users_by_criteria':
                            query = buildUserCountQuery(criteriaBuilder, root, parameters);
                            break;
                        default:
                            throw new Error("Unknown query: " + queryName);
                    }
                    
                    return entityManager.createQuery(query).getResultList();
                }
            """
            
            build_dynamic_query: """
                function buildDynamicQuery(entityManager, criteria) {
                    let criteriaBuilder = entityManager.getCriteriaBuilder();
                    let query = criteriaBuilder.createQuery(User.class);
                    let root = query.from(User.class);
                    
                    let predicates = [];
                    
                    // Add dynamic predicates based on criteria
                    if (criteria.name) {
                        predicates.push(criteriaBuilder.like(
                            criteriaBuilder.lower(root.get("name")),
                            "%" + criteria.name.toLowerCase() + "%"
                        ));
                    }
                    
                    if (criteria.email) {
                        predicates.push(criteriaBuilder.like(
                            criteriaBuilder.lower(root.get("email")),
                            "%" + criteria.email.toLowerCase() + "%"
                        ));
                    }
                    
                    if (criteria.active !== undefined) {
                        predicates.push(criteriaBuilder.equal(root.get("active"), criteria.active));
                    }
                    
                    if (criteria.role) {
                        predicates.push(criteriaBuilder.equal(root.get("role"), criteria.role));
                    }
                    
                    if (criteria.startDate && criteria.endDate) {
                        predicates.push(criteriaBuilder.between(
                            root.get("createdAt"),
                            criteria.startDate,
                            criteria.endDate
                        ));
                    }
                    
                    // Build query
                    query.select(root);
                    
                    if (predicates.length > 0) {
                        query.where(criteriaBuilder.and(predicates.toArray()));
                    }
                    
                    // Add ordering
                    if (criteria.orderBy) {
                        if (criteria.orderDirection === 'desc') {
                            query.orderBy(criteriaBuilder.desc(root.get(criteria.orderBy)));
                        } else {
                            query.orderBy(criteriaBuilder.asc(root.get(criteria.orderBy)));
                        }
                    }
                    
                    return entityManager.createQuery(query);
                }
            """
            """;
        
        Map<String, Object> config = parser.parse(tskContent);
        
        // Initialize criteria query builder
        TuskCriteriaQueryBuilder criteriaQueryBuilder = new TuskCriteriaQueryBuilder();
        criteriaQueryBuilder.configure(config);
        
        // Execute criteria queries
        List<User> activeUsers = criteriaQueryBuilder.executeCriteriaQuery("criteria_methods", 
            "execute_criteria_query", Map.of("queryName", "find_active_users"));
        
        List<User> adminUsers = criteriaQueryBuilder.executeCriteriaQuery("criteria_methods", 
            "execute_criteria_query", Map.of(
                "queryName", "find_users_by_role",
                "parameters", Map.of("role", "admin")
            ));
        
        Map<String, Object> complexCriteria = Map.of(
            "active", true,
            "role", "user",
            "namePattern", "john",
            "startDate", java.time.LocalDateTime.now().minusDays(30),
            "endDate", java.time.LocalDateTime.now()
        );
        
        List<User> complexUsers = criteriaQueryBuilder.executeCriteriaQuery("criteria_methods", 
            "execute_criteria_query", Map.of(
                "queryName", "find_users_with_complex_criteria",
                "parameters", complexCriteria
            ));
        
        // Build dynamic query
        Map<String, Object> dynamicCriteria = Map.of(
            "name", "john",
            "active", true,
            "role", "user",
            "orderBy", "name",
            "orderDirection", "asc"
        );
        
        List<User> dynamicUsers = criteriaQueryBuilder.buildDynamicQuery("criteria_methods", 
            "build_dynamic_query", dynamicCriteria);
        
        System.out.println("Active users: " + activeUsers.size());
        System.out.println("Admin users: " + adminUsers.size());
        System.out.println("Complex query users: " + complexUsers.size());
        System.out.println("Dynamic query users: " + dynamicUsers.size());
    }
}
```

### 2. Spring Data JPA Query Builder
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.query.TuskSpringDataQueryBuilder;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import java.util.Map;
import java.util.List;

public interface UserRepository extends JpaRepository<User, Long> {
    // Method name queries
    List<User> findByActive(boolean active);
    List<User> findByRole(String role);
    List<User> findByActiveAndRole(boolean active, String role);
    List<User> findByNameContainingIgnoreCase(String name);
    List<User> findByEmailContainingIgnoreCase(String email);
    List<User> findByCreatedAtBetween(java.time.LocalDateTime startDate, java.time.LocalDateTime endDate);
    
    // Custom queries
    @Query("SELECT u FROM User u WHERE u.active = :active AND u.role = :role")
    List<User> findActiveUsersByRole(@Param("active") boolean active, @Param("role") String role);
    
    @Query("SELECT u FROM User u WHERE u.name LIKE %:namePattern% OR u.email LIKE %:emailPattern%")
    List<User> findUsersByNameOrEmail(@Param("namePattern") String namePattern, @Param("emailPattern") String emailPattern);
    
    @Query("SELECT COUNT(u) FROM User u WHERE u.active = :active")
    long countActiveUsers(@Param("active") boolean active);
    
    @Query("SELECT u.role, COUNT(u) FROM User u GROUP BY u.role")
    List<Object[]> countUsersByRole();
}

public class SpringDataQueryExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        
        String tskContent = """
            [spring_data_query_builder]
            # Spring Data JPA Query Builder Configuration
            enable_method_queries: true
            enable_custom_queries: true
            enable_native_queries: true
            
            [spring_data_config]
            # Spring Data configuration
            enable_query_methods: true
            enable_specifications: true
            enable_querydsl: true
            
            [user_repository_queries]
            # User repository queries
            repository_class: "com.example.UserRepository"
            
            method_queries: {
                find_by_active: "findByActive"
                find_by_role: "findByRole"
                find_by_active_and_role: "findByActiveAndRole"
                find_by_name_containing: "findByNameContainingIgnoreCase"
                find_by_email_containing: "findByEmailContainingIgnoreCase"
                find_by_date_range: "findByCreatedAtBetween"
            }
            
            custom_queries: {
                find_active_users_by_role: "findActiveUsersByRole"
                find_users_by_name_or_email: "findUsersByNameOrEmail"
                count_active_users: "countActiveUsers"
                count_users_by_role: "countUsersByRole"
            }
            
            [order_repository_queries]
            # Order repository queries
            repository_class: "com.example.OrderRepository"
            
            method_queries: {
                find_by_user_id: "findByUserId"
                find_by_status: "findByStatus"
                find_by_total_amount_greater_than: "findByTotalAmountGreaterThan"
                find_by_created_at_between: "findByCreatedAtBetween"
            }
            
            custom_queries: {
                find_orders_by_user_and_status: "findOrdersByUserAndStatus"
                sum_total_amount_by_user: "sumTotalAmountByUser"
                count_orders_by_status: "countOrdersByStatus"
            }
            
            [spring_data_methods]
            # Spring Data query methods
            execute_method_query: """
                function executeMethodQuery(repository, methodName, parameters) {
                    switch (methodName) {
                        case 'findByActive':
                            return repository.findByActive(parameters.active);
                        case 'findByRole':
                            return repository.findByRole(parameters.role);
                        case 'findByActiveAndRole':
                            return repository.findByActiveAndRole(parameters.active, parameters.role);
                        case 'findByNameContainingIgnoreCase':
                            return repository.findByNameContainingIgnoreCase(parameters.name);
                        case 'findByEmailContainingIgnoreCase':
                            return repository.findByEmailContainingIgnoreCase(parameters.email);
                        case 'findByCreatedAtBetween':
                            return repository.findByCreatedAtBetween(parameters.startDate, parameters.endDate);
                        default:
                            throw new Error("Unknown method query: " + methodName);
                    }
                }
            """
            
            execute_custom_query: """
                function executeCustomQuery(repository, queryName, parameters) {
                    switch (queryName) {
                        case 'findActiveUsersByRole':
                            return repository.findActiveUsersByRole(parameters.active, parameters.role);
                        case 'findUsersByNameOrEmail':
                            return repository.findUsersByNameOrEmail(parameters.namePattern, parameters.emailPattern);
                        case 'countActiveUsers':
                            return repository.countActiveUsers(parameters.active);
                        case 'countUsersByRole':
                            return repository.countUsersByRole();
                        default:
                            throw new Error("Unknown custom query: " + queryName);
                    }
                }
            """
            
            build_dynamic_specification: """
                function buildDynamicSpecification(criteria) {
                    return (root, query, criteriaBuilder) -> {
                        let predicates = [];
                        
                        if (criteria.name) {
                            predicates.push(criteriaBuilder.like(
                                criteriaBuilder.lower(root.get("name")),
                                "%" + criteria.name.toLowerCase() + "%"
                            ));
                        }
                        
                        if (criteria.email) {
                            predicates.push(criteriaBuilder.like(
                                criteriaBuilder.lower(root.get("email")),
                                "%" + criteria.email.toLowerCase() + "%"
                            ));
                        }
                        
                        if (criteria.active !== undefined) {
                            predicates.push(criteriaBuilder.equal(root.get("active"), criteria.active));
                        }
                        
                        if (criteria.role) {
                            predicates.push(criteriaBuilder.equal(root.get("role"), criteria.role));
                        }
                        
                        if (criteria.startDate && criteria.endDate) {
                            predicates.push(criteriaBuilder.between(
                                root.get("createdAt"),
                                criteria.startDate,
                                criteria.endDate
                            ));
                        }
                        
                        return criteriaBuilder.and(predicates.toArray());
                    };
                }
            """
            """;
        
        Map<String, Object> config = parser.parse(tskContent);
        
        // Initialize Spring Data query builder
        TuskSpringDataQueryBuilder springDataQueryBuilder = new TuskSpringDataQueryBuilder();
        springDataQueryBuilder.configure(config);
        
        // Execute method queries
        UserRepository userRepository = springDataQueryBuilder.getRepository("user_repository_queries");
        
        List<User> activeUsers = springDataQueryBuilder.executeMethodQuery("spring_data_methods", 
            "execute_method_query", Map.of(
                "repository", userRepository,
                "methodName", "findByActive",
                "parameters", Map.of("active", true)
            ));
        
        List<User> adminUsers = springDataQueryBuilder.executeMethodQuery("spring_data_methods", 
            "execute_method_query", Map.of(
                "repository", userRepository,
                "methodName", "findByRole",
                "parameters", Map.of("role", "admin")
            ));
        
        List<User> nameUsers = springDataQueryBuilder.executeMethodQuery("spring_data_methods", 
            "execute_method_query", Map.of(
                "repository", userRepository,
                "methodName", "findByNameContainingIgnoreCase",
                "parameters", Map.of("name", "john")
            ));
        
        // Execute custom queries
        List<User> customUsers = springDataQueryBuilder.executeCustomQuery("spring_data_methods", 
            "execute_custom_query", Map.of(
                "repository", userRepository,
                "queryName", "findActiveUsersByRole",
                "parameters", Map.of("active", true, "role", "user")
            ));
        
        long activeCount = springDataQueryBuilder.executeCustomQuery("spring_data_methods", 
            "execute_custom_query", Map.of(
                "repository", userRepository,
                "queryName", "countActiveUsers",
                "parameters", Map.of("active", true)
            ));
        
        // Build dynamic specification
        Map<String, Object> dynamicCriteria = Map.of(
            "name", "john",
            "active", true,
            "role", "user"
        );
        
        Object specification = springDataQueryBuilder.buildDynamicSpecification("spring_data_methods", 
            "build_dynamic_specification", dynamicCriteria);
        
        System.out.println("Active users: " + activeUsers.size());
        System.out.println("Admin users: " + adminUsers.size());
        System.out.println("Name users: " + nameUsers.size());
        System.out.println("Custom query users: " + customUsers.size());
        System.out.println("Active count: " + activeCount);
    }
}
```

### 3. Native SQL Query Builder
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.query.TuskNativeQueryBuilder;
import java.util.Map;
import java.util.List;

public class NativeQueryExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        
        String tskContent = """
            [native_query_builder]
            # Native SQL Query Builder Configuration
            enable_native_queries: true
            enable_stored_procedures: true
            enable_function_calls: true
            
            [native_config]
            # Native query configuration
            enable_sql_injection_protection: true
            enable_parameter_validation: true
            enable_query_logging: true
            
            [user_native_queries]
            # User native queries
            entity_class: "com.example.User"
            
            queries: {
                find_users_by_role_sql: """
                    SELECT u.* FROM users u 
                    WHERE u.role = :role 
                    AND u.is_active = :active 
                    ORDER BY u.name ASC
                """
                
                find_users_with_orders_sql: """
                    SELECT u.*, COUNT(o.id) as order_count, SUM(o.total_amount) as total_spent
                    FROM users u
                    LEFT JOIN orders o ON u.id = o.user_id
                    WHERE u.is_active = :active
                    GROUP BY u.id, u.name, u.email, u.role, u.created_at
                    HAVING COUNT(o.id) >= :min_orders
                    ORDER BY total_spent DESC
                """
                
                find_users_by_date_range_sql: """
                    SELECT u.* FROM users u 
                    WHERE u.created_at BETWEEN :start_date AND :end_date
                    AND u.role IN (:roles)
                    ORDER BY u.created_at DESC
                """
                
                update_user_status_sql: """
                    UPDATE users 
                    SET is_active = :active, 
                        updated_at = CURRENT_TIMESTAMP 
                    WHERE id = :user_id
                """
                
                delete_inactive_users_sql: """
                    DELETE FROM users 
                    WHERE is_active = false 
                    AND created_at < :cutoff_date
                """
            }
            
            [order_native_queries]
            # Order native queries
            entity_class: "com.example.Order"
            
            queries: {
                find_orders_with_items_sql: """
                    SELECT o.*, oi.product_id, oi.quantity, oi.price
                    FROM orders o
                    JOIN order_items oi ON o.id = oi.order_id
                    WHERE o.user_id = :user_id
                    ORDER BY o.created_at DESC
                """
                
                find_order_statistics_sql: """
                    SELECT 
                        o.status,
                        COUNT(*) as order_count,
                        SUM(o.total_amount) as total_amount,
                        AVG(o.total_amount) as avg_amount
                    FROM orders o
                    WHERE o.created_at BETWEEN :start_date AND :end_date
                    GROUP BY o.status
                    ORDER BY total_amount DESC
                """
                
                find_top_customers_sql: """
                    SELECT 
                        u.id,
                        u.name,
                        u.email,
                        COUNT(o.id) as order_count,
                        SUM(o.total_amount) as total_spent
                    FROM users u
                    JOIN orders o ON u.id = o.user_id
                    WHERE o.status = 'COMPLETED'
                    AND o.created_at BETWEEN :start_date AND :end_date
                    GROUP BY u.id, u.name, u.email
                    HAVING SUM(o.total_amount) >= :min_spent
                    ORDER BY total_spent DESC
                    LIMIT :limit
                """
            }
            
            [native_methods]
            # Native query methods
            execute_native_query: """
                function executeNativeQuery(entityManager, queryName, parameters) {
                    let sql = @user_native_queries.queries[queryName];
                    
                    if (!sql) {
                        throw new Error("Unknown native query: " + queryName);
                    }
                    
                    let query = entityManager.createNativeQuery(sql);
                    
                    // Set parameters
                    for (let key in parameters) {
                        query.setParameter(key, parameters[key]);
                    }
                    
                    return query.getResultList();
                }
            """
            
            execute_native_update: """
                function executeNativeUpdate(entityManager, queryName, parameters) {
                    let sql = @user_native_queries.queries[queryName];
                    
                    if (!sql) {
                        throw new Error("Unknown native update: " + queryName);
                    }
                    
                    let query = entityManager.createNativeQuery(sql);
                    
                    // Set parameters
                    for (let key in parameters) {
                        query.setParameter(key, parameters[key]);
                    }
                    
                    return query.executeUpdate();
                }
            """
            
            execute_stored_procedure: """
                function executeStoredProcedure(entityManager, procedureName, parameters) {
                    let sql = "CALL " + procedureName + "(";
                    let paramPlaceholders = [];
                    
                    for (let i = 0; i < parameters.length; i++) {
                        paramPlaceholders.push("?");
                    }
                    
                    sql += paramPlaceholders.join(", ") + ")";
                    
                    let query = entityManager.createNativeQuery(sql);
                    
                    // Set parameters
                    for (let i = 0; i < parameters.length; i++) {
                        query.setParameter(i + 1, parameters[i]);
                    }
                    
                    return query.getResultList();
                }
            """
            
            build_dynamic_native_query: """
                function buildDynamicNativeQuery(baseQuery, conditions) {
                    let sql = baseQuery;
                    let parameters = {};
                    let paramIndex = 1;
                    
                    if (conditions.where) {
                        let whereClause = " WHERE ";
                        let whereConditions = [];
                        
                        for (let condition of conditions.where) {
                            whereConditions.push(condition.field + " " + condition.operator + " :param" + paramIndex);
                            parameters["param" + paramIndex] = condition.value;
                            paramIndex++;
                        }
                        
                        sql += whereClause + whereConditions.join(" AND ");
                    }
                    
                    if (conditions.orderBy) {
                        sql += " ORDER BY " + conditions.orderBy.field + " " + conditions.orderBy.direction;
                    }
                    
                    if (conditions.limit) {
                        sql += " LIMIT " + conditions.limit;
                    }
                    
                    return {
                        sql: sql,
                        parameters: parameters
                    };
                }
            """
            """;
        
        Map<String, Object> config = parser.parse(tskContent);
        
        // Initialize native query builder
        TuskNativeQueryBuilder nativeQueryBuilder = new TuskNativeQueryBuilder();
        nativeQueryBuilder.configure(config);
        
        // Execute native queries
        List<Object[]> usersByRole = nativeQueryBuilder.executeNativeQuery("native_methods", 
            "execute_native_query", Map.of(
                "queryName", "find_users_by_role_sql",
                "parameters", Map.of("role", "admin", "active", true)
            ));
        
        List<Object[]> usersWithOrders = nativeQueryBuilder.executeNativeQuery("native_methods", 
            "execute_native_query", Map.of(
                "queryName", "find_users_with_orders_sql",
                "parameters", Map.of("active", true, "min_orders", 1)
            ));
        
        List<Object[]> orderStatistics = nativeQueryBuilder.executeNativeQuery("native_methods", 
            "execute_native_query", Map.of(
                "queryName", "find_order_statistics_sql",
                "parameters", Map.of(
                    "start_date", java.time.LocalDateTime.now().minusDays(30),
                    "end_date", java.time.LocalDateTime.now()
                )
            ));
        
        // Execute native updates
        int updatedUsers = nativeQueryBuilder.executeNativeUpdate("native_methods", 
            "execute_native_update", Map.of(
                "queryName", "update_user_status_sql",
                "parameters", Map.of("user_id", 1L, "active", false)
            ));
        
        // Build dynamic native query
        Map<String, Object> dynamicConditions = Map.of(
            "where", List.of(
                Map.of("field", "u.role", "operator", "=", "value", "user"),
                Map.of("field", "u.is_active", "operator", "=", "value", true)
            ),
            "orderBy", Map.of("field", "u.name", "direction", "ASC"),
            "limit", 10
        );
        
        Map<String, Object> dynamicQuery = nativeQueryBuilder.buildDynamicNativeQuery("native_methods", 
            "build_dynamic_native_query", Map.of(
                "baseQuery", "SELECT u.* FROM users u",
                "conditions", dynamicConditions
            ));
        
        System.out.println("Users by role: " + usersByRole.size());
        System.out.println("Users with orders: " + usersWithOrders.size());
        System.out.println("Order statistics: " + orderStatistics.size());
        System.out.println("Updated users: " + updatedUsers);
        System.out.println("Dynamic query SQL: " + dynamicQuery.get("sql"));
    }
}
```

### 4. Query Composition and Chaining
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.query.TuskQueryComposer;
import java.util.Map;
import java.util.List;

public class QueryCompositionExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        
        String tskContent = """
            [query_composer]
            # Query Composition Configuration
            enable_query_chaining: true
            enable_query_composition: true
            enable_query_reuse: true
            
            [composer_config]
            # Composer configuration
            enable_fluent_api: true
            enable_builder_pattern: true
            enable_decorator_pattern: true
            
            [composer_methods]
            # Query composition methods
            build_user_query_chain: """
                function buildUserQueryChain(criteria) {
                    let query = new UserQueryBuilder();
                    
                    // Chain query conditions
                    if (criteria.active !== undefined) {
                        query = query.active(criteria.active);
                    }
                    
                    if (criteria.role) {
                        query = query.role(criteria.role);
                    }
                    
                    if (criteria.namePattern) {
                        query = query.nameLike(criteria.namePattern);
                    }
                    
                    if (criteria.emailPattern) {
                        query = query.emailLike(criteria.emailPattern);
                    }
                    
                    if (criteria.startDate && criteria.endDate) {
                        query = query.createdBetween(criteria.startDate, criteria.endDate);
                    }
                    
                    // Add ordering
                    if (criteria.orderBy) {
                        query = query.orderBy(criteria.orderBy.field, criteria.orderBy.direction);
                    }
                    
                    // Add pagination
                    if (criteria.page && criteria.size) {
                        query = query.page(criteria.page, criteria.size);
                    }
                    
                    return query.build();
                }
            """
            
            compose_complex_query: """
                function composeComplexQuery(components) {
                    let query = new ComplexQueryBuilder();
                    
                    // Add base query
                    if (components.baseQuery) {
                        query = query.base(components.baseQuery);
                    }
                    
                    // Add joins
                    if (components.joins) {
                        for (let join of components.joins) {
                            query = query.join(join.type, join.entity, join.alias, join.condition);
                        }
                    }
                    
                    // Add where conditions
                    if (components.where) {
                        for (let condition of components.where) {
                            query = query.where(condition.field, condition.operator, condition.value);
                        }
                    }
                    
                    // Add group by
                    if (components.groupBy) {
                        query = query.groupBy(components.groupBy);
                    }
                    
                    // Add having
                    if (components.having) {
                        for (let having of components.having) {
                            query = query.having(having.field, having.operator, having.value);
                        }
                    }
                    
                    // Add order by
                    if (components.orderBy) {
                        for (let order of components.orderBy) {
                            query = query.orderBy(order.field, order.direction);
                        }
                    }
                    
                    // Add limit and offset
                    if (components.limit) {
                        query = query.limit(components.limit);
                    }
                    
                    if (components.offset) {
                        query = query.offset(components.offset);
                    }
                    
                    return query.build();
                }
            """
            
            build_reusable_query: """
                function buildReusableQuery(queryTemplate, parameters) {
                    let query = queryTemplate.clone();
                    
                    // Replace placeholders with actual values
                    for (let key in parameters) {
                        query = query.replace(":" + key, parameters[key]);
                    }
                    
                    return query;
                }
            """
            
            chain_query_operations: """
                function chainQueryOperations(operations) {
                    let query = new QueryChain();
                    
                    for (let operation of operations) {
                        switch (operation.type) {
                            case 'filter':
                                query = query.filter(operation.field, operation.operator, operation.value);
                                break;
                            case 'sort':
                                query = query.sort(operation.field, operation.direction);
                                break;
                            case 'limit':
                                query = query.limit(operation.value);
                                break;
                            case 'offset':
                                query = query.offset(operation.value);
                                break;
                            case 'select':
                                query = query.select(operation.fields);
                                break;
                            case 'join':
                                query = query.join(operation.entity, operation.condition);
                                break;
                            default:
                                throw new Error("Unknown operation type: " + operation.type);
                        }
                    }
                    
                    return query.execute();
                }
            """
            """;
        
        Map<String, Object> config = parser.parse(tskContent);
        
        // Initialize query composer
        TuskQueryComposer queryComposer = new TuskQueryComposer();
        queryComposer.configure(config);
        
        // Build user query chain
        Map<String, Object> userCriteria = Map.of(
            "active", true,
            "role", "user",
            "namePattern", "john",
            "orderBy", Map.of("field", "name", "direction", "ASC"),
            "page", 0,
            "size", 10
        );
        
        Object userQuery = queryComposer.buildQueryChain("composer_methods", 
            "build_user_query_chain", userCriteria);
        
        // Compose complex query
        Map<String, Object> complexComponents = Map.of(
            "baseQuery", "SELECT u.* FROM users u",
            "joins", List.of(
                Map.of("type", "LEFT", "entity", "orders o", "alias", "o", "condition", "u.id = o.user_id")
            ),
            "where", List.of(
                Map.of("field", "u.active", "operator", "=", "value", true),
                Map.of("field", "u.role", "operator", "=", "value", "user")
            ),
            "groupBy", "u.id",
            "having", List.of(
                Map.of("field", "COUNT(o.id)", "operator", ">=", "value", 1)
            ),
            "orderBy", List.of(
                Map.of("field", "COUNT(o.id)", "direction", "DESC")
            ),
            "limit", 10
        );
        
        Object complexQuery = queryComposer.composeComplexQuery("composer_methods", 
            "compose_complex_query", complexComponents);
        
        // Chain query operations
        List<Map<String, Object>> operations = List.of(
            Map.of("type", "filter", "field", "active", "operator", "=", "value", true),
            Map.of("type", "filter", "field", "role", "operator", "=", "value", "admin"),
            Map.of("type", "sort", "field", "name", "direction", "ASC"),
            Map.of("type", "limit", "value", 5)
        );
        
        Object chainedQuery = queryComposer.chainQueryOperations("composer_methods", 
            "chain_query_operations", operations);
        
        System.out.println("User query chain built");
        System.out.println("Complex query composed");
        System.out.println("Query operations chained");
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
import org.tusklang.java.spring.TuskQueryBuilderConfig;

@SpringBootApplication
@Configuration
public class QueryBuilderApplication {
    
    @Bean
    public TuskQueryBuilderConfig tuskQueryBuilderConfig() {
        TuskLang parser = new TuskLang();
        return parser.parseFile("query-builder.tsk", TuskQueryBuilderConfig.class);
    }
    
    public static void main(String[] args) {
        SpringApplication.run(QueryBuilderApplication.class, args);
    }
}

@TuskConfig
public class TuskQueryBuilderConfig {
    private CriteriaQueryConfig criteriaQueries;
    private SpringDataQueryConfig springDataQueries;
    private NativeQueryConfig nativeQueries;
    private QueryComposerConfig queryComposer;
    
    // Getters and setters
    public CriteriaQueryConfig getCriteriaQueries() { return criteriaQueries; }
    public void setCriteriaQueries(CriteriaQueryConfig criteriaQueries) { this.criteriaQueries = criteriaQueries; }
    
    public SpringDataQueryConfig getSpringDataQueries() { return springDataQueries; }
    public void setSpringDataQueries(SpringDataQueryConfig springDataQueries) { this.springDataQueries = springDataQueries; }
    
    public NativeQueryConfig getNativeQueries() { return nativeQueries; }
    public void setNativeQueries(NativeQueryConfig nativeQueries) { this.nativeQueries = nativeQueries; }
    
    public QueryComposerConfig getQueryComposer() { return queryComposer; }
    public void setQueryComposer(QueryComposerConfig queryComposer) { this.queryComposer = queryComposer; }
}
```

## 🎯 Best Practices

### 1. Query Builder Design Patterns
```java
// ✅ Use appropriate query types
- Criteria API: For type-safe dynamic queries
- Spring Data JPA: For simple repository queries
- Native SQL: For complex performance-critical queries
- Query composition: For reusable query components

// ✅ Implement proper parameter handling
- Use parameterized queries
- Validate input parameters
- Prevent SQL injection
- Use appropriate data types

// ✅ Optimize query performance
- Use appropriate indexes
- Limit result sets
- Use pagination
- Monitor query performance

// ✅ Implement proper error handling
- Handle query exceptions
- Validate query results
- Provide meaningful error messages
- Log query errors
```

### 2. Performance Optimization
```java
// 1. Query Optimization
- Use appropriate fetch types
- Implement query caching
- Use batch operations
- Monitor query performance

// 2. Database Optimization
- Use appropriate indexes
- Optimize SQL queries
- Use connection pooling
- Monitor database performance

// 3. Memory Management
- Use pagination for large results
- Implement result streaming
- Use appropriate data structures
- Monitor memory usage

// 4. Caching Strategy
- Cache frequently used queries
- Use appropriate cache TTL
- Implement cache invalidation
- Monitor cache performance
```

## 🚀 Summary

TuskLang Java query builder provides:

- **JPA Criteria Query Builder**: Type-safe dynamic query building
- **Spring Data JPA Query Builder**: Repository-based query methods
- **Native SQL Query Builder**: Performance-critical native queries
- **Query Composition**: Reusable and chainable query components
- **Spring Boot Integration**: Native Spring Boot configuration support

With these query builder features, your Java applications will achieve enterprise-grade query building capabilities while maintaining the flexibility and power of TuskLang configuration.

**"We don't bow to any king" - Build queries like a Java master with TuskLang!** 