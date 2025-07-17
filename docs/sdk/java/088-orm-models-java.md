# 🏗️ ORM Models in TuskLang Java

**"We don't bow to any king" - Build models like a Java architect**

TuskLang Java provides sophisticated ORM model management that integrates seamlessly with Spring Boot, JPA, and modern Java patterns. Create, configure, and manage your ORM models with enterprise-grade performance and flexibility.

## 🎯 Overview

ORM models in TuskLang Java combine the power of Java JPA technologies with TuskLang's configuration system. From entity mapping to complex relationships, we'll show you how to build robust, scalable ORM model systems.

## 🔧 Core ORM Features

### 1. JPA Entity Models
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.orm.TuskORMModelManager;
import javax.persistence.*;
import java.util.Map;
import java.util.List;
import java.util.Set;

@Entity
@Table(name = "users")
public class User {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;
    
    @Column(nullable = false, length = 255)
    private String name;
    
    @Column(unique = true, nullable = false, length = 255)
    private String email;
    
    @Column(name = "password_hash", nullable = false)
    private String passwordHash;
    
    @Column(name = "is_active", nullable = false)
    private boolean active = true;
    
    @Column(name = "created_at", nullable = false)
    @Temporal(TemporalType.TIMESTAMP)
    private java.util.Date createdAt;
    
    @Column(name = "updated_at")
    @Temporal(TemporalType.TIMESTAMP)
    private java.util.Date updatedAt;
    
    @Version
    @Column(name = "version")
    private Long version;
    
    // Relationships
    @OneToMany(mappedBy = "user", cascade = CascadeType.ALL, fetch = FetchType.LAZY)
    private List<Order> orders;
    
    @OneToMany(mappedBy = "author", cascade = CascadeType.ALL, fetch = FetchType.LAZY)
    private List<Post> posts;
    
    @ManyToMany(fetch = FetchType.LAZY)
    @JoinTable(
        name = "user_roles",
        joinColumns = @JoinColumn(name = "user_id"),
        inverseJoinColumns = @JoinColumn(name = "role_id")
    )
    private Set<Role> roles;
    
    @OneToOne(mappedBy = "user", cascade = CascadeType.ALL, fetch = FetchType.LAZY)
    private UserProfile profile;
    
    // Constructors
    public User() {
        this.createdAt = new java.util.Date();
    }
    
    public User(String name, String email, String passwordHash) {
        this();
        this.name = name;
        this.email = email;
        this.passwordHash = passwordHash;
    }
    
    // Lifecycle callbacks
    @PrePersist
    protected void onCreate() {
        this.createdAt = new java.util.Date();
        this.updatedAt = new java.util.Date();
    }
    
    @PreUpdate
    protected void onUpdate() {
        this.updatedAt = new java.util.Date();
    }
    
    // Getters and setters
    public Long getId() { return id; }
    public void setId(Long id) { this.id = id; }
    
    public String getName() { return name; }
    public void setName(String name) { this.name = name; }
    
    public String getEmail() { return email; }
    public void setEmail(String email) { this.email = email; }
    
    public String getPasswordHash() { return passwordHash; }
    public void setPasswordHash(String passwordHash) { this.passwordHash = passwordHash; }
    
    public boolean isActive() { return active; }
    public void setActive(boolean active) { this.active = active; }
    
    public java.util.Date getCreatedAt() { return createdAt; }
    public void setCreatedAt(java.util.Date createdAt) { this.createdAt = createdAt; }
    
    public java.util.Date getUpdatedAt() { return updatedAt; }
    public void setUpdatedAt(java.util.Date updatedAt) { this.updatedAt = updatedAt; }
    
    public Long getVersion() { return version; }
    public void setVersion(Long version) { this.version = version; }
    
    public List<Order> getOrders() { return orders; }
    public void setOrders(List<Order> orders) { this.orders = orders; }
    
    public List<Post> getPosts() { return posts; }
    public void setPosts(List<Post> posts) { this.posts = posts; }
    
    public Set<Role> getRoles() { return roles; }
    public void setRoles(Set<Role> roles) { this.roles = roles; }
    
    public UserProfile getProfile() { return profile; }
    public void setProfile(UserProfile profile) { this.profile = profile; }
}

@Entity
@Table(name = "orders")
public class Order {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;
    
    @Column(name = "order_number", unique = true, nullable = false)
    private String orderNumber;
    
    @Column(name = "total_amount", nullable = false, precision = 10, scale = 2)
    private java.math.BigDecimal totalAmount;
    
    @Enumerated(EnumType.STRING)
    @Column(name = "status", nullable = false)
    private OrderStatus status = OrderStatus.PENDING;
    
    @Column(name = "created_at", nullable = false)
    @Temporal(TemporalType.TIMESTAMP)
    private java.util.Date createdAt;
    
    @Column(name = "updated_at")
    @Temporal(TemporalType.TIMESTAMP)
    private java.util.Date updatedAt;
    
    @Version
    @Column(name = "version")
    private Long version;
    
    // Relationships
    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "user_id", nullable = false)
    private User user;
    
    @OneToMany(mappedBy = "order", cascade = CascadeType.ALL, fetch = FetchType.LAZY)
    private List<OrderItem> items;
    
    // Constructors
    public Order() {
        this.createdAt = new java.util.Date();
        this.orderNumber = generateOrderNumber();
    }
    
    public Order(User user, java.math.BigDecimal totalAmount) {
        this();
        this.user = user;
        this.totalAmount = totalAmount;
    }
    
    // Lifecycle callbacks
    @PrePersist
    protected void onCreate() {
        this.createdAt = new java.util.Date();
        this.updatedAt = new java.util.Date();
    }
    
    @PreUpdate
    protected void onUpdate() {
        this.updatedAt = new java.util.Date();
    }
    
    // Helper methods
    private String generateOrderNumber() {
        return "ORD-" + System.currentTimeMillis() + "-" + (int)(Math.random() * 1000);
    }
    
    // Getters and setters
    public Long getId() { return id; }
    public void setId(Long id) { this.id = id; }
    
    public String getOrderNumber() { return orderNumber; }
    public void setOrderNumber(String orderNumber) { this.orderNumber = orderNumber; }
    
    public java.math.BigDecimal getTotalAmount() { return totalAmount; }
    public void setTotalAmount(java.math.BigDecimal totalAmount) { this.totalAmount = totalAmount; }
    
    public OrderStatus getStatus() { return status; }
    public void setStatus(OrderStatus status) { this.status = status; }
    
    public java.util.Date getCreatedAt() { return createdAt; }
    public void setCreatedAt(java.util.Date createdAt) { this.createdAt = createdAt; }
    
    public java.util.Date getUpdatedAt() { return updatedAt; }
    public void setUpdatedAt(java.util.Date updatedAt) { this.updatedAt = updatedAt; }
    
    public Long getVersion() { return version; }
    public void setVersion(Long version) { this.version = version; }
    
    public User getUser() { return user; }
    public void setUser(User user) { this.user = user; }
    
    public List<OrderItem> getItems() { return items; }
    public void setItems(List<OrderItem> items) { this.items = items; }
}

public enum OrderStatus {
    PENDING, PROCESSING, COMPLETED, CANCELLED, REFUNDED
}

public class ORMModelExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        
        String tskContent = """
            [orm_models]
            # ORM Model Configuration
            enable_entity_mapping: true
            enable_relationship_mapping: true
            enable_validation: true
            
            [model_config]
            # Model configuration
            enable_auditing: true
            enable_versioning: true
            enable_soft_deletes: true
            enable_caching: true
            
            [user_model]
            # User entity model
            entity_class: "com.example.User"
            table_name: "users"
            
            fields: {
                id: {
                    type: "Long"
                    primary_key: true
                    generated_value: "IDENTITY"
                }
                name: {
                    type: "String"
                    nullable: false
                    length: 255
                }
                email: {
                    type: "String"
                    nullable: false
                    unique: true
                    length: 255
                }
                password_hash: {
                    type: "String"
                    nullable: false
                    column_name: "password_hash"
                }
                is_active: {
                    type: "boolean"
                    nullable: false
                    default_value: true
                    column_name: "is_active"
                }
                created_at: {
                    type: "Date"
                    nullable: false
                    temporal_type: "TIMESTAMP"
                    column_name: "created_at"
                }
                updated_at: {
                    type: "Date"
                    temporal_type: "TIMESTAMP"
                    column_name: "updated_at"
                }
                version: {
                    type: "Long"
                    version: true
                }
            }
            
            relationships: {
                orders: {
                    type: "ONE_TO_MANY"
                    target: "com.example.Order"
                    mapped_by: "user"
                    cascade: ["ALL"]
                    fetch: "LAZY"
                }
                posts: {
                    type: "ONE_TO_MANY"
                    target: "com.example.Post"
                    mapped_by: "author"
                    cascade: ["ALL"]
                    fetch: "LAZY"
                }
                roles: {
                    type: "MANY_TO_MANY"
                    target: "com.example.Role"
                    join_table: "user_roles"
                    join_column: "user_id"
                    inverse_join_column: "role_id"
                    fetch: "LAZY"
                }
                profile: {
                    type: "ONE_TO_ONE"
                    target: "com.example.UserProfile"
                    mapped_by: "user"
                    cascade: ["ALL"]
                    fetch: "LAZY"
                }
            }
            
            [order_model]
            # Order entity model
            entity_class: "com.example.Order"
            table_name: "orders"
            
            fields: {
                id: {
                    type: "Long"
                    primary_key: true
                    generated_value: "IDENTITY"
                }
                order_number: {
                    type: "String"
                    nullable: false
                    unique: true
                    column_name: "order_number"
                }
                total_amount: {
                    type: "BigDecimal"
                    nullable: false
                    precision: 10
                    scale: 2
                    column_name: "total_amount"
                }
                status: {
                    type: "OrderStatus"
                    nullable: false
                    enumerated: "STRING"
                    default_value: "PENDING"
                }
                created_at: {
                    type: "Date"
                    nullable: false
                    temporal_type: "TIMESTAMP"
                    column_name: "created_at"
                }
                updated_at: {
                    type: "Date"
                    temporal_type: "TIMESTAMP"
                    column_name: "updated_at"
                }
                version: {
                    type: "Long"
                    version: true
                }
            }
            
            relationships: {
                user: {
                    type: "MANY_TO_ONE"
                    target: "com.example.User"
                    join_column: "user_id"
                    nullable: false
                    fetch: "LAZY"
                }
                items: {
                    type: "ONE_TO_MANY"
                    target: "com.example.OrderItem"
                    mapped_by: "order"
                    cascade: ["ALL"]
                    fetch: "LAZY"
                }
            }
            
            [orm_methods]
            # ORM model methods
            create_user_model: """
                function createUserModel(userData) {
                    let user = new User();
                    user.setName(userData.name);
                    user.setEmail(userData.email);
                    user.setPasswordHash(userData.passwordHash);
                    user.setActive(userData.active !== false);
                    
                    if (userData.roles) {
                        user.setRoles(new HashSet<>(userData.roles));
                    }
                    
                    return user;
                }
            """
            
            create_order_model: """
                function createOrderModel(orderData) {
                    let order = new Order();
                    order.setUser(orderData.user);
                    order.setTotalAmount(orderData.totalAmount);
                    order.setStatus(orderData.status || OrderStatus.PENDING);
                    
                    if (orderData.items) {
                        let items = [];
                        for (let itemData of orderData.items) {
                            let item = new OrderItem();
                            item.setProductId(itemData.productId);
                            item.setQuantity(itemData.quantity);
                            item.setPrice(itemData.price);
                            item.setOrder(order);
                            items.push(item);
                        }
                        order.setItems(items);
                    }
                    
                    return order;
                }
            """
            
            validate_user_model: """
                function validateUserModel(user) {
                    let errors = [];
                    
                    if (!user.getName() || user.getName().trim().length === 0) {
                        errors.push("Name is required");
                    }
                    
                    if (!user.getEmail() || user.getEmail().trim().length === 0) {
                        errors.push("Email is required");
                    } else if (!isValidEmail(user.getEmail())) {
                        errors.push("Invalid email format");
                    }
                    
                    if (!user.getPasswordHash() || user.getPasswordHash().trim().length === 0) {
                        errors.push("Password hash is required");
                    }
                    
                    return {
                        valid: errors.length === 0,
                        errors: errors
                    };
                }
                
                function isValidEmail(email) {
                    let emailRegex = /^[^\\s@]+@[^\\s@]+\\.[^\\s@]+$/;
                    return emailRegex.test(email);
                }
            """
            
            validate_order_model: """
                function validateOrderModel(order) {
                    let errors = [];
                    
                    if (!order.getUser()) {
                        errors.push("User is required");
                    }
                    
                    if (!order.getTotalAmount() || order.getTotalAmount().compareTo(java.math.BigDecimal.ZERO) <= 0) {
                        errors.push("Total amount must be greater than zero");
                    }
                    
                    if (!order.getStatus()) {
                        errors.push("Status is required");
                    }
                    
                    if (!order.getItems() || order.getItems().length === 0) {
                        errors.push("Order must have at least one item");
                    }
                    
                    return {
                        valid: errors.length === 0,
                        errors: errors
                    };
                }
            """
            """;
        
        Map<String, Object> config = parser.parse(tskContent);
        
        // Initialize ORM model manager
        TuskORMModelManager ormModelManager = new TuskORMModelManager();
        ormModelManager.configure(config);
        
        // Create user model
        Map<String, Object> userData = Map.of(
            "name", "John Doe",
            "email", "john@example.com",
            "passwordHash", "hashed_password_123",
            "active", true,
            "roles", Set.of("user", "premium")
        );
        
        User user = ormModelManager.createModel("orm_methods", "create_user_model", userData);
        
        // Validate user model
        Map<String, Object> userValidation = ormModelManager.validateModel("orm_methods", 
            "validate_user_model", user);
        
        // Create order model
        Map<String, Object> orderData = Map.of(
            "user", user,
            "totalAmount", new java.math.BigDecimal("299.99"),
            "status", OrderStatus.PENDING,
            "items", List.of(
                Map.of("productId", 1L, "quantity", 2, "price", new java.math.BigDecimal("149.99")),
                Map.of("productId", 2L, "quantity", 1, "price", new java.math.BigDecimal("199.99"))
            )
        );
        
        Order order = ormModelManager.createModel("orm_methods", "create_order_model", orderData);
        
        // Validate order model
        Map<String, Object> orderValidation = ormModelManager.validateModel("orm_methods", 
            "validate_order_model", order);
        
        System.out.println("Created user: " + user.getName() + " (" + user.getEmail() + ")");
        System.out.println("User validation: " + (userValidation.get("valid") ? "Valid" : "Invalid"));
        if (!(Boolean) userValidation.get("valid")) {
            System.out.println("User errors: " + userValidation.get("errors"));
        }
        
        System.out.println("Created order: " + order.getOrderNumber() + " ($" + order.getTotalAmount() + ")");
        System.out.println("Order validation: " + (orderValidation.get("valid") ? "Valid" : "Invalid"));
        if (!(Boolean) orderValidation.get("valid")) {
            System.out.println("Order errors: " + orderValidation.get("errors"));
        }
    }
}
```

### 2. Spring Boot ORM Integration
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.orm.TuskSpringORMManager;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import java.util.Map;
import java.util.List;

@Service
@Transactional
public class UserService {
    private final UserRepository userRepository;
    private final OrderRepository orderRepository;
    
    public UserService(UserRepository userRepository, OrderRepository orderRepository) {
        this.userRepository = userRepository;
        this.orderRepository = orderRepository;
    }
    
    @Transactional
    public User createUser(String name, String email, String passwordHash) {
        User user = new User(name, email, passwordHash);
        return userRepository.save(user);
    }
    
    @Transactional(readOnly = true)
    public User getUserById(Long id) {
        return userRepository.findById(id)
            .orElseThrow(() -> new RuntimeException("User not found: " + id));
    }
    
    @Transactional
    public Order createOrder(Long userId, java.math.BigDecimal totalAmount, List<OrderItem> items) {
        User user = getUserById(userId);
        Order order = new Order(user, totalAmount);
        order.setItems(items);
        
        // Set bidirectional relationship
        for (OrderItem item : items) {
            item.setOrder(order);
        }
        
        return orderRepository.save(order);
    }
}

public class SpringORMExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        
        String tskContent = """
            [spring_orm]
            # Spring Boot ORM Configuration
            enable_service_integration: true
            enable_transaction_management: true
            enable_repository_integration: true
            
            [service_orm]
            # Service-level ORM configuration
            user_service: {
                class: "com.example.UserService"
                methods: {
                    createUser: {
                        transactional: true
                        readOnly: false
                    }
                    getUserById: {
                        transactional: true
                        readOnly: true
                    }
                    createOrder: {
                        transactional: true
                        readOnly: false
                    }
                }
            }
            
            [repository_orm]
            # Repository-level ORM configuration
            user_repository: {
                class: "com.example.UserRepository"
                entity: "com.example.User"
                enable_custom_queries: true
            }
            
            order_repository: {
                class: "com.example.OrderRepository"
                entity: "com.example.Order"
                enable_custom_queries: true
            }
            
            [spring_orm_methods]
            # Spring ORM methods
            create_user_with_spring: """
                function createUserWithSpring(userService, userData) {
                    return userService.createUser(
                        userData.name,
                        userData.email,
                        userData.passwordHash
                    );
                }
            """
            
            create_order_with_spring: """
                function createOrderWithSpring(userService, orderData) {
                    let items = [];
                    for (let itemData of orderData.items) {
                        let item = new OrderItem();
                        item.setProductId(itemData.productId);
                        item.setQuantity(itemData.quantity);
                        item.setPrice(itemData.price);
                        items.push(item);
                    }
                    
                    return userService.createOrder(
                        orderData.userId,
                        orderData.totalAmount,
                        items
                    );
                }
            """
            
            get_user_with_orders: """
                function getUserWithOrders(userService, userId) {
                    let user = userService.getUserById(userId);
                    
                    // Trigger lazy loading of orders
                    user.getOrders().size();
                    
                    return user;
                }
            """
            """;
        
        Map<String, Object> config = parser.parse(tskContent);
        
        // Initialize Spring ORM manager
        TuskSpringORMManager springORMManager = new TuskSpringORMManager();
        springORMManager.configure(config);
        
        // Create user with Spring ORM
        Map<String, Object> userData = Map.of(
            "name", "Jane Smith",
            "email", "jane@example.com",
            "passwordHash", "hashed_password_456"
        );
        
        UserService userService = springORMManager.getService("user_service");
        User user = springORMManager.executeServiceMethod("spring_orm_methods", 
            "create_user_with_spring", Map.of(
                "userService", userService,
                "userData", userData
            ));
        
        // Create order with Spring ORM
        Map<String, Object> orderData = Map.of(
            "userId", user.getId(),
            "totalAmount", new java.math.BigDecimal("199.99"),
            "items", List.of(
                Map.of("productId", 3L, "quantity", 1, "price", new java.math.BigDecimal("199.99"))
            )
        );
        
        Order order = springORMManager.executeServiceMethod("spring_orm_methods", 
            "create_order_with_spring", Map.of(
                "userService", userService,
                "orderData", orderData
            ));
        
        // Get user with orders
        User userWithOrders = springORMManager.executeServiceMethod("spring_orm_methods", 
            "get_user_with_orders", Map.of(
                "userService", userService,
                "userId", user.getId()
            ));
        
        System.out.println("Created user: " + user.getName() + " (" + user.getEmail() + ")");
        System.out.println("Created order: " + order.getOrderNumber() + " ($" + order.getTotalAmount() + ")");
        System.out.println("User orders count: " + userWithOrders.getOrders().size());
    }
}
```

### 3. Advanced ORM Patterns
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.orm.TuskAdvancedORMManager;
import java.util.Map;
import java.util.List;

public class AdvancedORMExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        
        String tskContent = """
            [advanced_orm]
            # Advanced ORM Patterns Configuration
            enable_inheritance_mapping: true
            enable_composite_keys: true
            enable_custom_types: true
            
            [inheritance_mapping]
            # Inheritance mapping configuration
            payment_inheritance: {
                strategy: "JOINED"
                base_class: "com.example.Payment"
                subclasses: {
                    credit_card: "com.example.CreditCardPayment"
                    bank_transfer: "com.example.BankTransferPayment"
                    paypal: "com.example.PayPalPayment"
                }
                discriminator: "payment_type"
            }
            
            content_inheritance: {
                strategy: "SINGLE_TABLE"
                base_class: "com.example.Content"
                subclasses: {
                    post: "com.example.Post"
                    article: "com.example.Article"
                    video: "com.example.Video"
                }
                discriminator: "content_type"
            }
            
            [composite_keys]
            # Composite key configuration
            order_item_composite: {
                entity: "com.example.OrderItem"
                composite_key: {
                    order_id: "Long"
                    product_id: "Long"
                }
                key_class: "com.example.OrderItemId"
            }
            
            user_role_composite: {
                entity: "com.example.UserRole"
                composite_key: {
                    user_id: "Long"
                    role_id: "Long"
                }
                key_class: "com.example.UserRoleId"
            }
            
            [custom_types]
            # Custom type configuration
            json_type: {
                type: "com.example.JsonType"
                java_type: "String"
                database_type: "JSON"
            }
            
            encrypted_type: {
                type: "com.example.EncryptedType"
                java_type: "String"
                database_type: "VARCHAR"
                encryption_key: @env("ENCRYPTION_KEY")
            }
            
            [advanced_orm_methods]
            # Advanced ORM methods
            create_payment_inheritance: """
                function createPaymentInheritance(paymentData) {
                    let payment;
                    
                    switch (paymentData.type) {
                        case 'credit_card':
                            payment = new CreditCardPayment();
                            payment.setCardNumber(paymentData.cardNumber);
                            payment.setExpiryDate(paymentData.expiryDate);
                            payment.setCvv(paymentData.cvv);
                            break;
                        case 'bank_transfer':
                            payment = new BankTransferPayment();
                            payment.setAccountNumber(paymentData.accountNumber);
                            payment.setRoutingNumber(paymentData.routingNumber);
                            break;
                        case 'paypal':
                            payment = new PayPalPayment();
                            payment.setPaypalEmail(paymentData.paypalEmail);
                            break;
                        default:
                            throw new Error("Unknown payment type: " + paymentData.type);
                    }
                    
                    payment.setAmount(paymentData.amount);
                    payment.setCurrency(paymentData.currency);
                    payment.setStatus(PaymentStatus.PENDING);
                    
                    return payment;
                }
            """
            
            create_content_inheritance: """
                function createContentInheritance(contentData) {
                    let content;
                    
                    switch (contentData.type) {
                        case 'post':
                            content = new Post();
                            content.setTitle(contentData.title);
                            content.setContent(contentData.content);
                            break;
                        case 'article':
                            content = new Article();
                            content.setTitle(contentData.title);
                            content.setContent(contentData.content);
                            content.setAuthor(contentData.author);
                            content.setPublishedDate(contentData.publishedDate);
                            break;
                        case 'video':
                            content = new Video();
                            content.setTitle(contentData.title);
                            content.setUrl(contentData.url);
                            content.setDuration(contentData.duration);
                            content.setThumbnail(contentData.thumbnail);
                            break;
                        default:
                            throw new Error("Unknown content type: " + contentData.type);
                    }
                    
                    content.setAuthor(contentData.author);
                    content.setCreatedAt(new Date());
                    
                    return content;
                }
            """
            
            create_composite_key_entity: """
                function createCompositeKeyEntity(entityData) {
                    let entity;
                    
                    if (entityData.type === 'order_item') {
                        let orderItemId = new OrderItemId();
                        orderItemId.setOrderId(entityData.orderId);
                        orderItemId.setProductId(entityData.productId);
                        
                        entity = new OrderItem();
                        entity.setId(orderItemId);
                        entity.setQuantity(entityData.quantity);
                        entity.setPrice(entityData.price);
                    } else if (entityData.type === 'user_role') {
                        let userRoleId = new UserRoleId();
                        userRoleId.setUserId(entityData.userId);
                        userRoleId.setRoleId(entityData.roleId);
                        
                        entity = new UserRole();
                        entity.setId(userRoleId);
                        entity.setAssignedAt(new Date());
                    }
                    
                    return entity;
                }
            """
            
            create_custom_type_entity: """
                function createCustomTypeEntity(entityData) {
                    let entity = new EntityWithCustomTypes();
                    
                    // JSON type
                    if (entityData.jsonData) {
                        entity.setJsonData(JSON.stringify(entityData.jsonData));
                    }
                    
                    // Encrypted type
                    if (entityData.encryptedData) {
                        entity.setEncryptedData(entityData.encryptedData);
                    }
                    
                    return entity;
                }
            """
            """;
        
        Map<String, Object> config = parser.parse(tskContent);
        
        // Initialize advanced ORM manager
        TuskAdvancedORMManager advancedORMManager = new TuskAdvancedORMManager();
        advancedORMManager.configure(config);
        
        // Create payment inheritance
        Map<String, Object> paymentData = Map.of(
            "type", "credit_card",
            "amount", new java.math.BigDecimal("299.99"),
            "currency", "USD",
            "cardNumber", "4111111111111111",
            "expiryDate", "12/25",
            "cvv", "123"
        );
        
        Payment payment = advancedORMManager.createAdvancedModel("advanced_orm_methods", 
            "create_payment_inheritance", paymentData);
        
        // Create content inheritance
        Map<String, Object> contentData = Map.of(
            "type", "article",
            "title", "Advanced ORM Patterns",
            "content", "Learn about advanced ORM patterns...",
            "author", "John Doe",
            "publishedDate", new java.util.Date()
        );
        
        Content content = advancedORMManager.createAdvancedModel("advanced_orm_methods", 
            "create_content_inheritance", contentData);
        
        // Create composite key entity
        Map<String, Object> compositeData = Map.of(
            "type", "order_item",
            "orderId", 1L,
            "productId", 123L,
            "quantity", 2,
            "price", new java.math.BigDecimal("149.99")
        );
        
        Object compositeEntity = advancedORMManager.createAdvancedModel("advanced_orm_methods", 
            "create_composite_key_entity", compositeData);
        
        // Create custom type entity
        Map<String, Object> customTypeData = Map.of(
            "jsonData", Map.of("key1", "value1", "key2", "value2"),
            "encryptedData", "sensitive_information"
        );
        
        Object customTypeEntity = advancedORMManager.createAdvancedModel("advanced_orm_methods", 
            "create_custom_type_entity", customTypeData);
        
        System.out.println("Created payment: " + payment.getClass().getSimpleName() + " ($" + payment.getAmount() + ")");
        System.out.println("Created content: " + content.getClass().getSimpleName() + " (" + content.getTitle() + ")");
        System.out.println("Created composite entity: " + compositeEntity.getClass().getSimpleName());
        System.out.println("Created custom type entity: " + customTypeEntity.getClass().getSimpleName());
    }
}
```

### 4. ORM Performance Optimization
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.orm.TuskORMPerformanceOptimizer;
import java.util.Map;
import java.util.List;

public class ORMPerformanceExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        
        String tskContent = """
            [orm_performance]
            # ORM Performance Optimization Configuration
            enable_query_optimization: true
            enable_caching: true
            enable_batch_processing: true
            
            [performance_config]
            # Performance configuration
            batch_size: 100
            enable_second_level_cache: true
            enable_query_cache: true
            enable_collection_cache: true
            
            [caching_config]
            # Caching configuration
            user_cache: {
                region: "user_cache"
                ttl: "1h"
                max_entries: 1000
            }
            
            order_cache: {
                region: "order_cache"
                ttl: "30m"
                max_entries: 500
            }
            
            [performance_methods]
            # Performance optimization methods
            optimize_user_queries: """
                function optimizeUserQueries(userIds) {
                    // Use batch loading
                    let users = userRepository.findAllById(userIds);
                    
                    // Pre-fetch related entities
                    for (let user of users) {
                        // Trigger lazy loading in batch
                        user.getOrders().size();
                        user.getPosts().size();
                        user.getRoles().size();
                    }
                    
                    return users;
                }
            """
            
            optimize_order_queries: """
                function optimizeOrderQueries(orderIds) {
                    // Use JOIN FETCH for eager loading
                    let orders = orderRepository.findOrdersWithItemsAndUser(orderIds);
                    
                    // Pre-fetch related entities
                    for (let order of orders) {
                        order.getItems().size();
                        order.getUser().getName(); // Trigger user loading
                    }
                    
                    return orders;
                }
            """
            
            batch_save_entities: """
                function batchSaveEntities(entities, batchSize) {
                    let saved = 0;
                    let batch = [];
                    
                    for (let entity of entities) {
                        batch.push(entity);
                        
                        if (batch.length >= batchSize) {
                            entityManager.persist(batch);
                            entityManager.flush();
                            entityManager.clear();
                            saved += batch.length;
                            batch = [];
                        }
                    }
                    
                    // Save remaining entities
                    if (batch.length > 0) {
                        entityManager.persist(batch);
                        entityManager.flush();
                        saved += batch.length;
                    }
                    
                    return saved;
                }
            """
            
            cache_entities: """
                function cacheEntities(entities, cacheRegion) {
                    let cache = getCache(cacheRegion);
                    let cached = 0;
                    
                    for (let entity of entities) {
                        let key = entity.getClass().getSimpleName() + "_" + entity.getId();
                        cache.put(key, entity);
                        cached++;
                    }
                    
                    return cached;
                }
            """
            """;
        
        Map<String, Object> config = parser.parse(tskContent);
        
        // Initialize ORM performance optimizer
        TuskORMPerformanceOptimizer performanceOptimizer = new TuskORMPerformanceOptimizer();
        performanceOptimizer.configure(config);
        
        // Optimize user queries
        List<Long> userIds = List.of(1L, 2L, 3L, 4L, 5L);
        List<User> optimizedUsers = performanceOptimizer.executeOptimization("performance_methods", 
            "optimize_user_queries", Map.of("userIds", userIds));
        
        // Optimize order queries
        List<Long> orderIds = List.of(1L, 2L, 3L, 4L, 5L);
        List<Order> optimizedOrders = performanceOptimizer.executeOptimization("performance_methods", 
            "optimize_order_queries", Map.of("orderIds", orderIds));
        
        // Batch save entities
        List<User> usersToSave = List.of(
            new User("User1", "user1@example.com", "hash1"),
            new User("User2", "user2@example.com", "hash2"),
            new User("User3", "user3@example.com", "hash3")
        );
        
        int savedCount = performanceOptimizer.executeOptimization("performance_methods", 
            "batch_save_entities", Map.of("entities", usersToSave, "batchSize", 100));
        
        // Cache entities
        int cachedCount = performanceOptimizer.executeOptimization("performance_methods", 
            "cache_entities", Map.of("entities", optimizedUsers, "cacheRegion", "user_cache"));
        
        System.out.println("Optimized user queries: " + optimizedUsers.size() + " users");
        System.out.println("Optimized order queries: " + optimizedOrders.size() + " orders");
        System.out.println("Batch saved: " + savedCount + " entities");
        System.out.println("Cached: " + cachedCount + " entities");
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
import org.tusklang.java.spring.TuskORMConfig;

@SpringBootApplication
@Configuration
public class ORMApplication {
    
    @Bean
    public TuskORMConfig tuskORMConfig() {
        TuskLang parser = new TuskLang();
        return parser.parseFile("orm.tsk", TuskORMConfig.class);
    }
    
    public static void main(String[] args) {
        SpringApplication.run(ORMApplication.class, args);
    }
}

@TuskConfig
public class TuskORMConfig {
    private ORMModelConfig ormModels;
    private SpringORMConfig springORM;
    private AdvancedORMConfig advancedORM;
    private ORMPerformanceConfig ormPerformance;
    
    // Getters and setters
    public ORMModelConfig getOrmModels() { return ormModels; }
    public void setOrmModels(ORMModelConfig ormModels) { this.ormModels = ormModels; }
    
    public SpringORMConfig getSpringORM() { return springORM; }
    public void setSpringORM(SpringORMConfig springORM) { this.springORM = springORM; }
    
    public AdvancedORMConfig getAdvancedORM() { return advancedORM; }
    public void setAdvancedORM(AdvancedORMConfig advancedORM) { this.advancedORM = advancedORM; }
    
    public ORMPerformanceConfig getOrmPerformance() { return ormPerformance; }
    public void setOrmPerformance(ORMPerformanceConfig ormPerformance) { this.ormPerformance = ormPerformance; }
}
```

## 🎯 Best Practices

### 1. ORM Model Design Patterns
```java
// ✅ Use appropriate entity mapping
- Use @Entity for persistent classes
- Use @Table for table mapping
- Use @Column for field mapping
- Use @Id for primary keys

// ✅ Implement proper relationships
- Use appropriate relationship types
- Set correct cascade options
- Choose appropriate fetch types
- Maintain bidirectional relationships

// ✅ Use lifecycle callbacks
- Use @PrePersist for creation
- Use @PreUpdate for updates
- Use @PreRemove for deletion
- Use @PostLoad for loading

// ✅ Implement validation
- Use Bean Validation annotations
- Implement custom validators
- Validate business rules
- Handle validation errors
```

### 2. Performance Optimization
```java
// 1. Query Optimization
- Use appropriate fetch types
- Implement batch loading
- Use JOIN FETCH for eager loading
- Monitor query performance

// 2. Caching Strategy
- Use second-level cache
- Implement query cache
- Use collection cache
- Monitor cache performance

// 3. Batch Processing
- Use batch operations
- Implement batch size optimization
- Use bulk operations
- Monitor batch performance

// 4. Memory Management
- Use lazy loading appropriately
- Implement proper cleanup
- Monitor memory usage
- Optimize entity lifecycle
```

## 🚀 Summary

TuskLang Java ORM models provide:

- **JPA Entity Models**: Seamless JPA integration with entity mapping
- **Spring Boot ORM Integration**: Native Spring Boot ORM support
- **Advanced ORM Patterns**: Inheritance, composite keys, and custom types
- **ORM Performance Optimization**: Query optimization and caching strategies
- **Spring Boot Integration**: Native Spring Boot configuration support

With these ORM features, your Java applications will achieve enterprise-grade ORM model management while maintaining the flexibility and power of TuskLang configuration.

**"We don't bow to any king" - Build models like a Java architect with TuskLang!** 