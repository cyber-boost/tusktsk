# 🔗 Database Relationships in TuskLang Java

**"We don't bow to any king" - Model relationships like a Java architect**

TuskLang Java provides sophisticated database relationship management that integrates seamlessly with Spring Boot, JPA, and modern Java patterns. Create, manage, and optimize database relationships with enterprise-grade performance and flexibility.

## 🎯 Overview

Database relationships in TuskLang Java combine the power of Java JPA relationship technologies with TuskLang's configuration system. From One-to-Many relationships to complex Many-to-Many associations, we'll show you how to build robust, scalable relationship systems.

## 🔧 Core Relationship Features

### 1. JPA Entity Relationships
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.relationships.TuskRelationshipManager;
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
    
    @Column(nullable = false)
    private String name;
    
    @Column(unique = true, nullable = false)
    private String email;
    
    // One-to-Many: User has many Orders
    @OneToMany(mappedBy = "user", cascade = CascadeType.ALL, fetch = FetchType.LAZY)
    private List<Order> orders;
    
    // One-to-Many: User has many Posts
    @OneToMany(mappedBy = "author", cascade = CascadeType.ALL, fetch = FetchType.LAZY)
    private List<Post> posts;
    
    // Many-to-Many: User has many Roles
    @ManyToMany(fetch = FetchType.LAZY)
    @JoinTable(
        name = "user_roles",
        joinColumns = @JoinColumn(name = "user_id"),
        inverseJoinColumns = @JoinColumn(name = "role_id")
    )
    private Set<Role> roles;
    
    // One-to-One: User has one Profile
    @OneToOne(mappedBy = "user", cascade = CascadeType.ALL, fetch = FetchType.LAZY)
    private UserProfile profile;
    
    // Constructors, getters, setters
    public User() {}
    
    public User(String name, String email) {
        this.name = name;
        this.email = email;
    }
    
    // Getters and setters
    public Long getId() { return id; }
    public void setId(Long id) { this.id = id; }
    
    public String getName() { return name; }
    public void setName(String name) { this.name = name; }
    
    public String getEmail() { return email; }
    public void setEmail(String email) { this.email = email; }
    
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
    
    @Column(name = "total_amount")
    private Double totalAmount;
    
    @Column(name = "status")
    private String status;
    
    // Many-to-One: Order belongs to User
    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "user_id")
    private User user;
    
    // One-to-Many: Order has many OrderItems
    @OneToMany(mappedBy = "order", cascade = CascadeType.ALL, fetch = FetchType.LAZY)
    private List<OrderItem> items;
    
    // Constructors, getters, setters
    public Order() {}
    
    public Order(Double totalAmount, String status) {
        this.totalAmount = totalAmount;
        this.status = status;
    }
    
    // Getters and setters
    public Long getId() { return id; }
    public void setId(Long id) { this.id = id; }
    
    public Double getTotalAmount() { return totalAmount; }
    public void setTotalAmount(Double totalAmount) { this.totalAmount = totalAmount; }
    
    public String getStatus() { return status; }
    public void setStatus(String status) { this.status = status; }
    
    public User getUser() { return user; }
    public void setUser(User user) { this.user = user; }
    
    public List<OrderItem> getItems() { return items; }
    public void setItems(List<OrderItem> items) { this.items = items; }
}

@Entity
@Table(name = "posts")
public class Post {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;
    
    @Column(nullable = false)
    private String title;
    
    @Column(columnDefinition = "TEXT")
    private String content;
    
    // Many-to-One: Post belongs to User (author)
    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "author_id")
    private User author;
    
    // Many-to-Many: Post has many Tags
    @ManyToMany(fetch = FetchType.LAZY)
    @JoinTable(
        name = "post_tags",
        joinColumns = @JoinColumn(name = "post_id"),
        inverseJoinColumns = @JoinColumn(name = "tag_id")
    )
    private Set<Tag> tags;
    
    // One-to-Many: Post has many Comments
    @OneToMany(mappedBy = "post", cascade = CascadeType.ALL, fetch = FetchType.LAZY)
    private List<Comment> comments;
    
    // Constructors, getters, setters
    public Post() {}
    
    public Post(String title, String content) {
        this.title = title;
        this.content = content;
    }
    
    // Getters and setters
    public Long getId() { return id; }
    public void setId(Long id) { this.id = id; }
    
    public String getTitle() { return title; }
    public void setTitle(String title) { this.title = title; }
    
    public String getContent() { return content; }
    public void setContent(String content) { this.content = content; }
    
    public User getAuthor() { return author; }
    public void setAuthor(User author) { this.author = author; }
    
    public Set<Tag> getTags() { return tags; }
    public void setTags(Set<Tag> tags) { this.tags = tags; }
    
    public List<Comment> getComments() { return comments; }
    public void setComments(List<Comment> comments) { this.comments = comments; }
}

public class RelationshipExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        
        String tskContent = """
            [jpa_relationships]
            # JPA Relationship Management Configuration
            enable_lazy_loading: true
            enable_eager_loading: false
            enable_cascade_operations: true
            
            [relationship_config]
            # Relationship configuration
            default_fetch_type: "LAZY"
            enable_bidirectional_relationships: true
            enable_orphan_removal: true
            
            [user_relationships]
            # User entity relationships
            entity_class: "com.example.User"
            
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
            
            [order_relationships]
            # Order entity relationships
            entity_class: "com.example.Order"
            
            relationships: {
                user: {
                    type: "MANY_TO_ONE"
                    target: "com.example.User"
                    join_column: "user_id"
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
            
            [relationship_methods]
            # Relationship management methods
            create_user_with_relationships: """
                function createUserWithRelationships(userData) {
                    let user = new User(userData.name, userData.email);
                    
                    // Create profile
                    let profile = new UserProfile();
                    profile.setBio(userData.bio || "");
                    profile.setAvatar(userData.avatar || "");
                    profile.setUser(user);
                    user.setProfile(profile);
                    
                    // Create orders
                    if (userData.orders) {
                        let orders = [];
                        for (let orderData of userData.orders) {
                            let order = new Order(orderData.totalAmount, orderData.status);
                            order.setUser(user);
                            orders.push(order);
                        }
                        user.setOrders(orders);
                    }
                    
                    // Create posts
                    if (userData.posts) {
                        let posts = [];
                        for (let postData of userData.posts) {
                            let post = new Post(postData.title, postData.content);
                            post.setAuthor(user);
                            posts.push(post);
                        }
                        user.setPosts(posts);
                    }
                    
                    return user;
                }
            """
            
            load_user_with_relationships: """
                function loadUserWithRelationships(userId, includeRelationships) {
                    let user = userRepository.findById(userId);
                    
                    if (includeRelationships.includeOrders) {
                        user.getOrders(); // Trigger lazy loading
                    }
                    
                    if (includeRelationships.includePosts) {
                        user.getPosts(); // Trigger lazy loading
                    }
                    
                    if (includeRelationships.includeRoles) {
                        user.getRoles(); // Trigger lazy loading
                    }
                    
                    if (includeRelationships.includeProfile) {
                        user.getProfile(); // Trigger lazy loading
                    }
                    
                    return user;
                }
            """
            
            create_post_with_relationships: """
                function createPostWithRelationships(postData) {
                    let post = new Post(postData.title, postData.content);
                    
                    // Set author
                    let author = userRepository.findById(postData.authorId);
                    post.setAuthor(author);
                    
                    // Add tags
                    if (postData.tagIds) {
                        let tags = tagRepository.findAllById(postData.tagIds);
                        post.setTags(new HashSet<>(tags));
                    }
                    
                    // Add comments
                    if (postData.comments) {
                        let comments = [];
                        for (let commentData of postData.comments) {
                            let comment = new Comment(commentData.content);
                            comment.setPost(post);
                            comment.setAuthor(userRepository.findById(commentData.authorId));
                            comments.push(comment);
                        }
                        post.setComments(comments);
                    }
                    
                    return post;
                }
            """
            """;
        
        Map<String, Object> config = parser.parse(tskContent);
        
        // Initialize relationship manager
        TuskRelationshipManager relationshipManager = new TuskRelationshipManager();
        relationshipManager.configure(config);
        
        // Create user with relationships
        Map<String, Object> userData = Map.of(
            "name", "John Doe",
            "email", "john@example.com",
            "bio", "Software Developer",
            "avatar", "avatar.jpg",
            "orders", List.of(
                Map.of("totalAmount", 299.99, "status", "completed"),
                Map.of("totalAmount", 149.99, "status", "pending")
            ),
            "posts", List.of(
                Map.of("title", "First Post", "content", "Hello World!"),
                Map.of("title", "Second Post", "content", "Another post")
            )
        );
        
        User user = relationshipManager.createEntityWithRelationships("relationship_methods", 
            "create_user_with_relationships", userData);
        
        // Load user with specific relationships
        Map<String, Object> includeRelationships = Map.of(
            "includeOrders", true,
            "includePosts", true,
            "includeRoles", false,
            "includeProfile", true
        );
        
        User loadedUser = relationshipManager.loadEntityWithRelationships("relationship_methods", 
            "load_user_with_relationships", Map.of(
                "userId", user.getId(),
                "includeRelationships", includeRelationships
            ));
        
        System.out.println("Created user: " + loadedUser.getName());
        System.out.println("Orders count: " + loadedUser.getOrders().size());
        System.out.println("Posts count: " + loadedUser.getPosts().size());
        System.out.println("Profile: " + loadedUser.getProfile().getBio());
    }
}
```

### 2. Spring Boot Relationship Management
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.relationships.TuskSpringRelationshipManager;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import java.util.Map;
import java.util.List;

@Service
@Transactional
public class UserService {
    private final UserRepository userRepository;
    private final OrderRepository orderRepository;
    private final PostRepository postRepository;
    private final RoleRepository roleRepository;
    
    public UserService(UserRepository userRepository, OrderRepository orderRepository, 
                      PostRepository postRepository, RoleRepository roleRepository) {
        this.userRepository = userRepository;
        this.orderRepository = orderRepository;
        this.postRepository = postRepository;
        this.roleRepository = roleRepository;
    }
    
    @Transactional
    public User createUserWithRelationships(String name, String email, List<Order> orders, 
                                          List<Post> posts, Set<Role> roles) {
        User user = new User(name, email);
        user.setOrders(orders);
        user.setPosts(posts);
        user.setRoles(roles);
        
        // Set bidirectional relationships
        if (orders != null) {
            for (Order order : orders) {
                order.setUser(user);
            }
        }
        
        if (posts != null) {
            for (Post post : posts) {
                post.setAuthor(user);
            }
        }
        
        return userRepository.save(user);
    }
    
    @Transactional(readOnly = true)
    public User getUserWithRelationships(Long userId, boolean includeOrders, boolean includePosts, 
                                       boolean includeRoles, boolean includeProfile) {
        User user = userRepository.findById(userId)
            .orElseThrow(() -> new RuntimeException("User not found: " + userId));
        
        if (includeOrders) {
            user.getOrders().size(); // Trigger lazy loading
        }
        
        if (includePosts) {
            user.getPosts().size(); // Trigger lazy loading
        }
        
        if (includeRoles) {
            user.getRoles().size(); // Trigger lazy loading
        }
        
        if (includeProfile) {
            user.getProfile(); // Trigger lazy loading
        }
        
        return user;
    }
}

public class SpringRelationshipExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        
        String tskContent = """
            [spring_relationships]
            # Spring Boot Relationship Management Configuration
            enable_service_integration: true
            enable_transaction_management: true
            enable_lazy_loading: true
            
            [service_relationships]
            # Service-level relationship configuration
            user_service: {
                class: "com.example.UserService"
                methods: {
                    createUserWithRelationships: {
                        transactional: true
                        readOnly: false
                    }
                    getUserWithRelationships: {
                        transactional: true
                        readOnly: true
                    }
                }
            }
            
            order_service: {
                class: "com.example.OrderService"
                methods: {
                    createOrderWithItems: {
                        transactional: true
                        readOnly: false
                    }
                    getOrderWithItems: {
                        transactional: true
                        readOnly: true
                    }
                }
            }
            
            [relationship_methods]
            # Spring relationship methods
            create_user_with_spring_relationships: """
                function createUserWithSpringRelationships(userService, userData) {
                    let orders = [];
                    if (userData.orders) {
                        for (let orderData of userData.orders) {
                            let order = new Order(orderData.totalAmount, orderData.status);
                            orders.push(order);
                        }
                    }
                    
                    let posts = [];
                    if (userData.posts) {
                        for (let postData of userData.posts) {
                            let post = new Post(postData.title, postData.content);
                            posts.push(post);
                        }
                    }
                    
                    let roles = new Set();
                    if (userData.roleIds) {
                        for (let roleId of userData.roleIds) {
                            let role = roleRepository.findById(roleId);
                            roles.add(role);
                        }
                    }
                    
                    return userService.createUserWithRelationships(
                        userData.name,
                        userData.email,
                        orders,
                        posts,
                        roles
                    );
                }
            """
            
            load_user_with_spring_relationships: """
                function loadUserWithSpringRelationships(userService, userId, includeRelationships) {
                    return userService.getUserWithRelationships(
                        userId,
                        includeRelationships.includeOrders,
                        includeRelationships.includePosts,
                        includeRelationships.includeRoles,
                        includeRelationships.includeProfile
                    );
                }
            """
            
            create_order_with_spring_relationships: """
                function createOrderWithSpringRelationships(orderService, orderData) {
                    let items = [];
                    if (orderData.items) {
                        for (let itemData of orderData.items) {
                            let item = new OrderItem(itemData.productId, itemData.quantity, itemData.price);
                            items.push(item);
                        }
                    }
                    
                    return orderService.createOrderWithItems(
                        orderData.userId,
                        orderData.totalAmount,
                        orderData.status,
                        items
                    );
                }
            """
            """;
        
        Map<String, Object> config = parser.parse(tskContent);
        
        // Initialize Spring relationship manager
        TuskSpringRelationshipManager springRelationshipManager = new TuskSpringRelationshipManager();
        springRelationshipManager.configure(config);
        
        // Create user with Spring relationships
        Map<String, Object> userData = Map.of(
            "name", "Jane Smith",
            "email", "jane@example.com",
            "orders", List.of(
                Map.of("totalAmount", 199.99, "status", "completed"),
                Map.of("totalAmount", 299.99, "status", "pending")
            ),
            "posts", List.of(
                Map.of("title", "My First Post", "content", "Welcome to my blog!"),
                Map.of("title", "Tech Talk", "content", "Discussing latest technologies")
            ),
            "roleIds", List.of(1L, 2L) // User and Premium roles
        );
        
        UserService userService = springRelationshipManager.getService("user_service");
        User user = springRelationshipManager.executeServiceMethod("relationship_methods", 
            "create_user_with_spring_relationships", Map.of(
                "userService", userService,
                "userData", userData
            ));
        
        // Load user with relationships
        Map<String, Object> includeRelationships = Map.of(
            "includeOrders", true,
            "includePosts", true,
            "includeRoles", true,
            "includeProfile", false
        );
        
        User loadedUser = springRelationshipManager.executeServiceMethod("relationship_methods", 
            "load_user_with_spring_relationships", Map.of(
                "userService", userService,
                "userId", user.getId(),
                "includeRelationships", includeRelationships
            ));
        
        System.out.println("Created user: " + loadedUser.getName());
        System.out.println("Orders count: " + loadedUser.getOrders().size());
        System.out.println("Posts count: " + loadedUser.getPosts().size());
        System.out.println("Roles count: " + loadedUser.getRoles().size());
    }
}
```

### 3. Complex Relationship Patterns
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.relationships.TuskComplexRelationshipManager;
import java.util.Map;
import java.util.List;

public class ComplexRelationshipExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        
        String tskContent = """
            [complex_relationships]
            # Complex Relationship Patterns Configuration
            enable_hierarchical_relationships: true
            enable_graph_relationships: true
            enable_polymorphic_relationships: true
            
            [hierarchical_relationships]
            # Hierarchical relationship patterns
            category_hierarchy: {
                type: "SELF_REFERENCING"
                entity: "com.example.Category"
                parent_field: "parent"
                children_field: "children"
                cascade: ["ALL"]
            }
            
            organization_hierarchy: {
                type: "SELF_REFERENCING"
                entity: "com.example.Department"
                parent_field: "parent"
                children_field: "subDepartments"
                cascade: ["ALL"]
            }
            
            [graph_relationships]
            # Graph relationship patterns
            social_network: {
                type: "GRAPH"
                entity: "com.example.User"
                relationships: {
                    friends: {
                        type: "MANY_TO_MANY"
                        target: "com.example.User"
                        join_table: "user_friends"
                    }
                    followers: {
                        type: "MANY_TO_MANY"
                        target: "com.example.User"
                        join_table: "user_followers"
                    }
                }
            }
            
            [polymorphic_relationships]
            # Polymorphic relationship patterns
            content_polymorphic: {
                type: "POLYMORPHIC"
                base_entity: "com.example.Content"
                implementations: {
                    post: "com.example.Post"
                    article: "com.example.Article"
                    video: "com.example.Video"
                }
                discriminator: "content_type"
            }
            
            [complex_methods]
            # Complex relationship methods
            create_category_hierarchy: """
                function createCategoryHierarchy(categoryData) {
                    let categories = [];
                    
                    for (let data of categoryData) {
                        let category = new Category(data.name, data.description);
                        
                        if (data.parentId) {
                            let parent = categoryRepository.findById(data.parentId);
                            category.setParent(parent);
                        }
                        
                        categories.push(category);
                    }
                    
                    return categoryRepository.saveAll(categories);
                }
            """
            
            create_social_network: """
                function createSocialNetwork(userData) {
                    let users = [];
                    
                    for (let data of userData) {
                        let user = new User(data.name, data.email);
                        users.push(user);
                    }
                    
                    users = userRepository.saveAll(users);
                    
                    // Create friendships
                    for (let i = 0; i < users.length; i++) {
                        for (let j = i + 1; j < users.length; j++) {
                            if (Math.random() > 0.5) { // 50% chance of friendship
                                users[i].getFriends().add(users[j]);
                                users[j].getFriends().add(users[i]);
                            }
                        }
                    }
                    
                    return userRepository.saveAll(users);
                }
            """
            
            create_polymorphic_content: """
                function createPolymorphicContent(contentData) {
                    let contents = [];
                    
                    for (let data of contentData) {
                        let content;
                        
                        switch (data.type) {
                            case 'post':
                                content = new Post(data.title, data.content);
                                break;
                            case 'article':
                                content = new Article(data.title, data.content, data.author);
                                break;
                            case 'video':
                                content = new Video(data.title, data.url, data.duration);
                                break;
                            default:
                                throw new Error("Unknown content type: " + data.type);
                        }
                        
                        content.setAuthor(userRepository.findById(data.authorId));
                        contents.push(content);
                    }
                    
                    return contentRepository.saveAll(contents);
                }
            """
            
            traverse_hierarchy: """
                function traverseHierarchy(rootId, maxDepth) {
                    let root = categoryRepository.findById(rootId);
                    let result = [];
                    
                    function traverse(node, depth) {
                        if (depth > maxDepth) return;
                        
                        result.push({
                            id: node.getId(),
                            name: node.getName(),
                            depth: depth
                        });
                        
                        for (let child of node.getChildren()) {
                            traverse(child, depth + 1);
                        }
                    }
                    
                    traverse(root, 0);
                    return result;
                }
            """
            
            find_social_connections: """
                function findSocialConnections(userId, maxDepth) {
                    let user = userRepository.findById(userId);
                    let visited = new Set();
                    let connections = [];
                    
                    function findConnections(currentUser, depth) {
                        if (depth > maxDepth || visited.has(currentUser.getId())) return;
                        
                        visited.add(currentUser.getId());
                        connections.push({
                            id: currentUser.getId(),
                            name: currentUser.getName(),
                            depth: depth
                        });
                        
                        for (let friend of currentUser.getFriends()) {
                            findConnections(friend, depth + 1);
                        }
                    }
                    
                    findConnections(user, 0);
                    return connections;
                }
            """
            """;
        
        Map<String, Object> config = parser.parse(tskContent);
        
        // Initialize complex relationship manager
        TuskComplexRelationshipManager complexRelationshipManager = new TuskComplexRelationshipManager();
        complexRelationshipManager.configure(config);
        
        // Create category hierarchy
        List<Map<String, Object>> categoryData = List.of(
            Map.of("name", "Electronics", "description", "Electronic devices", "parentId", null),
            Map.of("name", "Computers", "description", "Computer hardware", "parentId", 1L),
            Map.of("name", "Laptops", "description", "Portable computers", "parentId", 2L),
            Map.of("name", "Desktops", "description", "Desktop computers", "parentId", 2L),
            Map.of("name", "Phones", "description", "Mobile phones", "parentId", 1L),
            Map.of("name", "Smartphones", "description", "Smart mobile phones", "parentId", 5L)
        );
        
        List<Category> categories = complexRelationshipManager.executeComplexMethod("complex_methods", 
            "create_category_hierarchy", Map.of("categoryData", categoryData));
        
        // Create social network
        List<Map<String, Object>> userData = List.of(
            Map.of("name", "Alice", "email", "alice@example.com"),
            Map.of("name", "Bob", "email", "bob@example.com"),
            Map.of("name", "Charlie", "email", "charlie@example.com"),
            Map.of("name", "Diana", "email", "diana@example.com"),
            Map.of("name", "Eve", "email", "eve@example.com")
        );
        
        List<User> socialUsers = complexRelationshipManager.executeComplexMethod("complex_methods", 
            "create_social_network", Map.of("userData", userData));
        
        // Create polymorphic content
        List<Map<String, Object>> contentData = List.of(
            Map.of("type", "post", "title", "Hello World", "content", "My first post", "authorId", 1L),
            Map.of("type", "article", "title", "Java Programming", "content", "Learn Java", "author", "John Doe", "authorId", 2L),
            Map.of("type", "video", "title", "Tutorial Video", "url", "https://example.com/video", "duration", 300, "authorId", 3L)
        );
        
        List<Content> contents = complexRelationshipManager.executeComplexMethod("complex_methods", 
            "create_polymorphic_content", Map.of("contentData", contentData));
        
        // Traverse hierarchy
        List<Map<String, Object>> hierarchy = complexRelationshipManager.executeComplexMethod("complex_methods", 
            "traverse_hierarchy", Map.of("rootId", 1L, "maxDepth", 3));
        
        // Find social connections
        List<Map<String, Object>> connections = complexRelationshipManager.executeComplexMethod("complex_methods", 
            "find_social_connections", Map.of("userId", 1L, "maxDepth", 2));
        
        System.out.println("Created " + categories.size() + " categories");
        System.out.println("Created " + socialUsers.size() + " social users");
        System.out.println("Created " + contents.size() + " polymorphic contents");
        System.out.println("Hierarchy levels: " + hierarchy.size());
        System.out.println("Social connections: " + connections.size());
    }
}
```

### 4. Relationship Optimization
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.relationships.TuskRelationshipOptimizer;
import java.util.Map;
import java.util.List;

public class RelationshipOptimizationExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        
        String tskContent = """
            [relationship_optimization]
            # Relationship Optimization Configuration
            enable_query_optimization: true
            enable_lazy_loading_optimization: true
            enable_batch_loading: true
            
            [optimization_config]
            # Optimization configuration
            batch_size: 100
            enable_prefetch: true
            enable_query_caching: true
            enable_relationship_caching: true
            
            [optimization_methods]
            # Relationship optimization methods
            optimize_user_loading: """
                function optimizeUserLoading(userIds, includeRelationships) {
                    let users = userRepository.findAllById(userIds);
                    
                    if (includeRelationships.includeOrders) {
                        // Batch load orders for all users
                        let orderUserIds = users.map(u => u.getId());
                        let orders = orderRepository.findByUserIdIn(orderUserIds);
                        
                        // Group orders by user
                        let ordersByUser = {};
                        for (let order of orders) {
                            if (!ordersByUser[order.getUserId()]) {
                                ordersByUser[order.getUserId()] = [];
                            }
                            ordersByUser[order.getUserId()].push(order);
                        }
                        
                        // Set orders for each user
                        for (let user of users) {
                            user.setOrders(ordersByUser[user.getId()] || []);
                        }
                    }
                    
                    if (includeRelationships.includePosts) {
                        // Batch load posts for all users
                        let postUserIds = users.map(u => u.getId());
                        let posts = postRepository.findByAuthorIdIn(postUserIds);
                        
                        // Group posts by author
                        let postsByAuthor = {};
                        for (let post of posts) {
                            if (!postsByAuthor[post.getAuthorId()]) {
                                postsByAuthor[post.getAuthorId()] = [];
                            }
                            postsByAuthor[post.getAuthorId()].push(post);
                        }
                        
                        // Set posts for each user
                        for (let user of users) {
                            user.setPosts(postsByAuthor[user.getId()] || []);
                        }
                    }
                    
                    return users;
                }
            """
            
            optimize_order_loading: """
                function optimizeOrderLoading(orderIds, includeRelationships) {
                    let orders = orderRepository.findAllById(orderIds);
                    
                    if (includeRelationships.includeItems) {
                        // Batch load order items
                        let orderItemOrderIds = orders.map(o => o.getId());
                        let items = orderItemRepository.findByOrderIdIn(orderItemOrderIds);
                        
                        // Group items by order
                        let itemsByOrder = {};
                        for (let item of items) {
                            if (!itemsByOrder[item.getOrderId()]) {
                                itemsByOrder[item.getOrderId()] = [];
                            }
                            itemsByOrder[item.getOrderId()].push(item);
                        }
                        
                        // Set items for each order
                        for (let order of orders) {
                            order.setItems(itemsByOrder[order.getId()] || []);
                        }
                    }
                    
                    if (includeRelationships.includeUser) {
                        // Batch load users
                        let userIds = orders.map(o => o.getUserId());
                        let users = userRepository.findAllById(userIds);
                        
                        // Create user map
                        let userMap = {};
                        for (let user of users) {
                            userMap[user.getId()] = user;
                        }
                        
                        // Set user for each order
                        for (let order of orders) {
                            order.setUser(userMap[order.getUserId()]);
                        }
                    }
                    
                    return orders;
                }
            """
            
            optimize_post_loading: """
                function optimizePostLoading(postIds, includeRelationships) {
                    let posts = postRepository.findAllById(postIds);
                    
                    if (includeRelationships.includeComments) {
                        // Batch load comments
                        let commentPostIds = posts.map(p => p.getId());
                        let comments = commentRepository.findByPostIdIn(commentPostIds);
                        
                        // Group comments by post
                        let commentsByPost = {};
                        for (let comment of comments) {
                            if (!commentsByPost[comment.getPostId()]) {
                                commentsByPost[comment.getPostId()] = [];
                            }
                            commentsByPost[comment.getPostId()].push(comment);
                        }
                        
                        // Set comments for each post
                        for (let post of posts) {
                            post.setComments(commentsByPost[post.getId()] || []);
                        }
                    }
                    
                    if (includeRelationships.includeTags) {
                        // Batch load tags
                        let tagPostIds = posts.map(p => p.getId());
                        let postTags = postTagRepository.findByPostIdIn(tagPostIds);
                        
                        // Group tags by post
                        let tagsByPost = {};
                        for (let postTag of postTags) {
                            if (!tagsByPost[postTag.getPostId()]) {
                                tagsByPost[postTag.getPostId()] = [];
                            }
                            tagsByPost[postTag.getPostId()].push(postTag.getTag());
                        }
                        
                        // Set tags for each post
                        for (let post of posts) {
                            post.setTags(new Set(tagsByPost[post.getId()] || []));
                        }
                    }
                    
                    return posts;
                }
            """
            """;
        
        Map<String, Object> config = parser.parse(tskContent);
        
        // Initialize relationship optimizer
        TuskRelationshipOptimizer relationshipOptimizer = new TuskRelationshipOptimizer();
        relationshipOptimizer.configure(config);
        
        // Optimize user loading
        List<Long> userIds = List.of(1L, 2L, 3L, 4L, 5L);
        Map<String, Object> includeRelationships = Map.of(
            "includeOrders", true,
            "includePosts", true,
            "includeRoles", false,
            "includeProfile", false
        );
        
        List<User> optimizedUsers = relationshipOptimizer.executeOptimization("optimization_methods", 
            "optimize_user_loading", Map.of(
                "userIds", userIds,
                "includeRelationships", includeRelationships
            ));
        
        // Optimize order loading
        List<Long> orderIds = List.of(1L, 2L, 3L, 4L, 5L);
        Map<String, Object> orderRelationships = Map.of(
            "includeItems", true,
            "includeUser", true
        );
        
        List<Order> optimizedOrders = relationshipOptimizer.executeOptimization("optimization_methods", 
            "optimize_order_loading", Map.of(
                "orderIds", orderIds,
                "includeRelationships", orderRelationships
            ));
        
        // Optimize post loading
        List<Long> postIds = List.of(1L, 2L, 3L, 4L, 5L);
        Map<String, Object> postRelationships = Map.of(
            "includeComments", true,
            "includeTags", true
        );
        
        List<Post> optimizedPosts = relationshipOptimizer.executeOptimization("optimization_methods", 
            "optimize_post_loading", Map.of(
                "postIds", postIds,
                "includeRelationships", postRelationships
            ));
        
        System.out.println("Optimized loading of " + optimizedUsers.size() + " users");
        System.out.println("Optimized loading of " + optimizedOrders.size() + " orders");
        System.out.println("Optimized loading of " + optimizedPosts.size() + " posts");
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
import org.tusklang.java.spring.TuskRelationshipConfig;

@SpringBootApplication
@Configuration
public class RelationshipApplication {
    
    @Bean
    public TuskRelationshipConfig tuskRelationshipConfig() {
        TuskLang parser = new TuskLang();
        return parser.parseFile("relationships.tsk", TuskRelationshipConfig.class);
    }
    
    public static void main(String[] args) {
        SpringApplication.run(RelationshipApplication.class, args);
    }
}

@TuskConfig
public class TuskRelationshipConfig {
    private JPARelationshipConfig jpaRelationships;
    private SpringRelationshipConfig springRelationships;
    private ComplexRelationshipConfig complexRelationships;
    private RelationshipOptimizationConfig relationshipOptimization;
    
    // Getters and setters
    public JPARelationshipConfig getJpaRelationships() { return jpaRelationships; }
    public void setJpaRelationships(JPARelationshipConfig jpaRelationships) { this.jpaRelationships = jpaRelationships; }
    
    public SpringRelationshipConfig getSpringRelationships() { return springRelationships; }
    public void setSpringRelationships(SpringRelationshipConfig springRelationships) { this.springRelationships = springRelationships; }
    
    public ComplexRelationshipConfig getComplexRelationships() { return complexRelationships; }
    public void setComplexRelationships(ComplexRelationshipConfig complexRelationships) { this.complexRelationships = complexRelationships; }
    
    public RelationshipOptimizationConfig getRelationshipOptimization() { return relationshipOptimization; }
    public void setRelationshipOptimization(RelationshipOptimizationConfig relationshipOptimization) { this.relationshipOptimization = relationshipOptimization; }
}
```

## 🎯 Best Practices

### 1. Relationship Design Patterns
```java
// ✅ Use appropriate relationship types
- One-to-One: For unique associations
- One-to-Many: For parent-child relationships
- Many-to-Many: For complex associations
- Self-referencing: For hierarchical structures

// ✅ Implement proper cascading
- Use CASCADE.ALL for parent-child relationships
- Use CASCADE.PERSIST for dependent entities
- Use CASCADE.REMOVE carefully

// ✅ Choose appropriate fetch types
- Use LAZY for large collections
- Use EAGER for small, frequently accessed data
- Consider performance implications

// ✅ Implement bidirectional relationships
- Maintain consistency in both directions
- Use helper methods for relationship management
- Avoid infinite recursion
```

### 2. Performance Optimization
```java
// 1. Lazy Loading
- Use lazy loading for large collections
- Implement proper session management
- Avoid N+1 query problems

// 2. Batch Loading
- Load related entities in batches
- Use IN queries for multiple entities
- Implement custom query methods

// 3. Caching
- Cache frequently accessed relationships
- Use appropriate cache strategies
- Monitor cache performance

// 4. Query Optimization
- Use JOIN FETCH for eager loading
- Implement custom queries for complex relationships
- Monitor query performance
```

## 🚀 Summary

TuskLang Java database relationships provide:

- **JPA Entity Relationships**: Seamless JPA integration with relationship mapping
- **Spring Boot Relationships**: Native Spring Boot relationship management
- **Complex Relationship Patterns**: Hierarchical, graph, and polymorphic relationships
- **Relationship Optimization**: Performance optimization for complex relationships
- **Spring Boot Integration**: Native Spring Boot configuration support

With these relationship features, your Java applications will achieve enterprise-grade relationship management while maintaining the flexibility and power of TuskLang configuration.

**"We don't bow to any king" - Model relationships like a Java architect with TuskLang!** 