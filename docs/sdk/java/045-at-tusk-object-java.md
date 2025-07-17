# @tusk - TuskLang Active Record ORM in Java

The `@tusk` operator provides an elegant Active Record pattern for database operations in Java applications, integrating with Spring Boot's JPA, Hibernate, and enterprise ORM solutions.

## Basic Syntax

```java
// TuskLang configuration
User: @tusk{
    table: "users"
    primary_key: "id"
    fillable: ["name", "email", "password"]
    hidden: ["password", "remember_token"]
    timestamps: true
}

user: @User.create({
    name: "John Doe"
    email: "john@example.com"
    password: @hash_password("secret")
})
```

```java
// Java Spring Boot integration
@Configuration
public class TuskConfig {
    
    @Bean
    public TuskService tuskService(JpaRepositoryFactory factory) {
        return TuskService.builder()
            .repositoryFactory(factory)
            .enableTimestamps(true)
            .enableSoftDeletes(true)
            .build();
    }
}
```

## Model Definition

```java
// Java Tusk model definition
@Entity
@Table(name = "users")
public class User extends TuskModel {
    
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;
    
    @Column(name = "name", nullable = false)
    private String name;
    
    @Column(name = "email", unique = true, nullable = false)
    private String email;
    
    @Column(name = "password", nullable = false)
    private String password;
    
    @Column(name = "remember_token")
    private String rememberToken;
    
    @Column(name = "created_at")
    private LocalDateTime createdAt;
    
    @Column(name = "updated_at")
    private LocalDateTime updatedAt;
    
    @Column(name = "deleted_at")
    private LocalDateTime deletedAt;
    
    // Getters and setters
    public Long getId() { return id; }
    public void setId(Long id) { this.id = id; }
    
    public String getName() { return name; }
    public void setName(String name) { this.name = name; }
    
    public String getEmail() { return email; }
    public void setEmail(String email) { this.email = email; }
    
    public String getPassword() { return password; }
    public void setPassword(String password) { this.password = password; }
    
    public String getRememberToken() { return rememberToken; }
    public void setRememberToken(String rememberToken) { this.rememberToken = rememberToken; }
    
    public LocalDateTime getCreatedAt() { return createdAt; }
    public void setCreatedAt(LocalDateTime createdAt) { this.createdAt = createdAt; }
    
    public LocalDateTime getUpdatedAt() { return updatedAt; }
    public void setUpdatedAt(LocalDateTime updatedAt) { this.updatedAt = updatedAt; }
    
    public LocalDateTime getDeletedAt() { return deletedAt; }
    public void setDeletedAt(LocalDateTime deletedAt) { this.deletedAt = deletedAt; }
    
    // TuskModel methods
    @Override
    public String getTableName() {
        return "users";
    }
    
    @Override
    public String getPrimaryKey() {
        return "id";
    }
    
    @Override
    public List<String> getFillableFields() {
        return Arrays.asList("name", "email", "password");
    }
    
    @Override
    public List<String> getHiddenFields() {
        return Arrays.asList("password", "remember_token");
    }
    
    @Override
    public boolean hasTimestamps() {
        return true;
    }
    
    @Override
    public boolean hasSoftDeletes() {
        return true;
    }
}
```

```java
// TuskLang model definition
Product: @tusk{
    table: "products"
    primary_key: "id"
    
    # Mass assignment protection
    fillable: ["name", "description", "price", "category_id"]
    guarded: ["id", "created_at", "updated_at"]
    
    # Hide sensitive fields
    hidden: ["cost", "supplier_id"]
    
    # Automatic timestamps
    timestamps: true  # created_at, updated_at
    
    # Soft deletes
    soft_deletes: true  # deleted_at
    
    # Type casting
    casts: {
        price: "float"
        active: "boolean"
        metadata: "json"
        published_at: "datetime"
    }
    
    # Default values
    defaults: {
        active: true
        price: 0.00
        views: 0
    }
}
```

## CRUD Operations

```java
// Java CRUD operations
@Component
public class UserService {
    
    @Autowired
    private TuskService tuskService;
    
    @Autowired
    private UserRepository userRepository;
    
    // Create
    public User createUser(UserDto userDto) {
        User user = new User();
        user.setName(userDto.getName());
        user.setEmail(userDto.getEmail());
        user.setPassword(hashPassword(userDto.getPassword()));
        user.setCreatedAt(LocalDateTime.now());
        user.setUpdatedAt(LocalDateTime.now());
        
        return userRepository.save(user);
    }
    
    public User createUser(Map<String, Object> data) {
        User user = new User();
        
        // Apply fillable fields
        List<String> fillable = user.getFillableFields();
        for (String field : fillable) {
            if (data.containsKey(field)) {
                setFieldValue(user, field, data.get(field));
            }
        }
        
        // Set timestamps
        if (user.hasTimestamps()) {
            user.setCreatedAt(LocalDateTime.now());
            user.setUpdatedAt(LocalDateTime.now());
        }
        
        return userRepository.save(user);
    }
    
    // Read
    public User findById(Long id) {
        return userRepository.findById(id)
            .orElseThrow(() -> new EntityNotFoundException("User not found with id: " + id));
    }
    
    public User findByIdOrFail(Long id) {
        return userRepository.findById(id)
            .orElseThrow(() -> new EntityNotFoundException("User not found with id: " + id));
    }
    
    public User findByEmail(String email) {
        return userRepository.findByEmail(email)
            .orElse(null);
    }
    
    public User first() {
        return userRepository.findFirstByOrderByIdAsc()
            .orElse(null);
    }
    
    // Update
    public User updateUser(Long id, UserDto userDto) {
        User user = findById(id);
        user.setName(userDto.getName());
        user.setEmail(userDto.getEmail());
        user.setUpdatedAt(LocalDateTime.now());
        
        return userRepository.save(user);
    }
    
    public User updateUser(Long id, Map<String, Object> data) {
        User user = findById(id);
        
        // Apply fillable fields
        List<String> fillable = user.getFillableFields();
        for (String field : fillable) {
            if (data.containsKey(field)) {
                setFieldValue(user, field, data.get(field));
            }
        }
        
        // Update timestamp
        if (user.hasTimestamps()) {
            user.setUpdatedAt(LocalDateTime.now());
        }
        
        return userRepository.save(user);
    }
    
    // Delete
    public void deleteUser(Long id) {
        User user = findById(id);
        
        if (user.hasSoftDeletes()) {
            // Soft delete
            user.setDeletedAt(LocalDateTime.now());
            userRepository.save(user);
        } else {
            // Hard delete
            userRepository.delete(user);
        }
    }
    
    public void forceDeleteUser(Long id) {
        User user = findById(id);
        userRepository.delete(user);
    }
    
    public void restoreUser(Long id) {
        User user = findById(id);
        if (user.hasSoftDeletes() && user.getDeletedAt() != null) {
            user.setDeletedAt(null);
            userRepository.save(user);
        }
    }
    
    private void setFieldValue(User user, String field, Object value) {
        try {
            Field userField = User.class.getDeclaredField(field);
            userField.setAccessible(true);
            userField.set(user, value);
        } catch (Exception e) {
            throw new RuntimeException("Failed to set field: " + field, e);
        }
    }
    
    private String hashPassword(String password) {
        // Implementation for password hashing
        return BCrypt.hashpw(password, BCrypt.gensalt());
    }
}
```

```java
// TuskLang CRUD operations
crud_operations: {
    # Create
    # Method 1: Using create()
    product: @Product.create({
        name: "Laptop"
        description: "High-performance laptop"
        price: 999.99
        category_id: 5
    })
    
    # Method 2: Using new() and save()
    product: @Product.new()
    product.name: "Tablet"
    product.price: 299.99
    product.save()
    
    # Read
    # Find by primary key
    product: @Product.find(1)
    
    # Find with exception if not found
    product: @Product.findOrFail(1)
    
    # Find by attributes
    user: @User.findBy("email", "john@example.com")
    
    # First record
    first_user: @User.first()
    
    # Update
    # Method 1: Direct update
    product: @Product.find(1)
    product.price: 899.99
    product.save()
    
    # Method 2: Mass update
    @Product.update(1, {
        price: 899.99
        description: "Updated description"
    })
    
    # Delete
    # Soft delete (if enabled)
    product.delete()
    
    # Force delete
    product.forceDelete()
    
    # Restore soft deleted
    product.restore()
}
```

## Querying

```java
// Java querying
@Component
public class UserQueryService {
    
    @Autowired
    private UserRepository userRepository;
    
    public List<User> getAllUsers() {
        return userRepository.findAll();
    }
    
    public List<User> getActiveUsers() {
        return userRepository.findByActiveTrue();
    }
    
    public List<User> getUsersByCondition(String field, Object value) {
        return userRepository.findByField(field, value);
    }
    
    public List<User> getUsersByMultipleConditions(Map<String, Object> conditions) {
        return userRepository.findByConditions(conditions);
    }
    
    public List<User> getUsersByOrCondition(String field1, Object value1, String field2, Object value2) {
        return userRepository.findByField1OrField2(field1, value1, field2, value2);
    }
    
    public List<User> getUsersByIds(List<Long> ids) {
        return userRepository.findAllById(ids);
    }
    
    public List<User> getUsersByRange(String field, Object minValue, Object maxValue) {
        return userRepository.findByFieldBetween(field, minValue, maxValue);
    }
    
    public List<User> getUsersByLike(String field, String pattern) {
        return userRepository.findByFieldLike(field, "%" + pattern + "%");
    }
    
    public List<User> getUsersOrdered(String field, String direction) {
        Sort sort = Sort.by(Sort.Direction.fromString(direction), field);
        return userRepository.findAll(sort);
    }
    
    public List<User> getUsersLimited(int limit) {
        PageRequest pageRequest = PageRequest.of(0, limit);
        return userRepository.findAll(pageRequest).getContent();
    }
    
    public Page<User> getUsersPaginated(int page, int size) {
        PageRequest pageRequest = PageRequest.of(page, size);
        return userRepository.findAll(pageRequest);
    }
}
```

```java
// TuskLang querying
querying: {
    # All records
    products: @Product.all()
    
    # Where conditions
    active_products: @Product.where("active", true).get()
    
    # Multiple conditions
    products: @Product
        .where("price", ">", 100)
        .where("category_id", 5)
        .get()
    
    # Or where
    products: @Product
        .where("category_id", 1)
        .orWhere("category_id", 2)
        .get()
    
    # Where in
    products: @Product.whereIn("id", [1, 2, 3, 4, 5]).get()
    
    # Where between
    products: @Product.whereBetween("price", [100, 500]).get()
    
    # Like queries
    products: @Product.where("name", "like", "%laptop%").get()
    
    # Ordering
    products: @Product.orderBy("price", "desc").get()
    
    # Limiting
    top_products: @Product.orderBy("sales", "desc").limit(10).get()
    
    # Pagination
    page: @request.get.page|1
    products: @Product.paginate(20, @page)
}
```

## Relationships

```java
// Java relationships
@Entity
@Table(name = "users")
public class User extends TuskModel {
    
    // ... existing fields ...
    
    // Has many posts
    @OneToMany(mappedBy = "user", cascade = CascadeType.ALL, fetch = FetchType.LAZY)
    private List<Post> posts = new ArrayList<>();
    
    // Has one profile
    @OneToOne(mappedBy = "user", cascade = CascadeType.ALL, fetch = FetchType.LAZY)
    private Profile profile;
    
    // Belongs to many roles
    @ManyToMany(fetch = FetchType.LAZY)
    @JoinTable(
        name = "user_roles",
        joinColumns = @JoinColumn(name = "user_id"),
        inverseJoinColumns = @JoinColumn(name = "role_id")
    )
    private List<Role> roles = new ArrayList<>();
    
    // Getters and setters for relationships
    public List<Post> getPosts() { return posts; }
    public void setPosts(List<Post> posts) { this.posts = posts; }
    
    public Profile getProfile() { return profile; }
    public void setProfile(Profile profile) { this.profile = profile; }
    
    public List<Role> getRoles() { return roles; }
    public void setRoles(List<Role> roles) { this.roles = roles; }
}

@Entity
@Table(name = "posts")
public class Post extends TuskModel {
    
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;
    
    @Column(name = "title")
    private String title;
    
    @Column(name = "content")
    private String content;
    
    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "user_id")
    private User user;
    
    @OneToMany(mappedBy = "post", cascade = CascadeType.ALL, fetch = FetchType.LAZY)
    private List<Comment> comments = new ArrayList<>();
    
    @ManyToMany(fetch = FetchType.LAZY)
    @JoinTable(
        name = "post_tags",
        joinColumns = @JoinColumn(name = "post_id"),
        inverseJoinColumns = @JoinColumn(name = "tag_id")
    )
    private List<Tag> tags = new ArrayList<>();
    
    // Getters and setters
    public Long getId() { return id; }
    public void setId(Long id) { this.id = id; }
    
    public String getTitle() { return title; }
    public void setTitle(String title) { this.title = title; }
    
    public String getContent() { return content; }
    public void setContent(String content) { this.content = content; }
    
    public User getUser() { return user; }
    public void setUser(User user) { this.user = user; }
    
    public List<Comment> getComments() { return comments; }
    public void setComments(List<Comment> comments) { this.comments = comments; }
    
    public List<Tag> getTags() { return tags; }
    public void setTags(List<Tag> tags) { this.tags = tags; }
}
```

```java
// TuskLang relationships
relationships: {
    # Define relationships
    User: @tusk{
        table: "users"
        
        # Has many posts
        posts: @hasMany("Post", "user_id")
        
        # Has one profile
        profile: @hasOne("Profile", "user_id")
        
        # Belongs to many roles
        roles: @belongsToMany("Role", "user_roles", "user_id", "role_id")
    }
    
    Post: @tusk{
        table: "posts"
        
        # Belongs to user
        user: @belongsTo("User", "user_id")
        
        # Has many comments
        comments: @hasMany("Comment", "post_id")
        
        # Has many tags through pivot
        tags: @belongsToMany("Tag", "post_tags", "post_id", "tag_id")
    }
    
    # Using relationships
    user: @User.find(1)
    user_posts: user.posts()
    
    # Eager loading
    users: @User.with(["posts", "profile"]).get()
    
    # Lazy eager loading
    user: @User.find(1)
    user.load("posts")
    
    # Query relationships
    users_with_posts: @User.has("posts").get()
    users_with_many_posts: @User.has("posts", ">", 5).get()
}
```

## Scopes

```java
// Java scopes
@Component
public class UserScopeService {
    
    @Autowired
    private UserRepository userRepository;
    
    public List<User> getActiveUsers() {
        return userRepository.findByActiveTrue();
    }
    
    public List<User> getExpensiveUsers(double minPrice) {
        return userRepository.findByTotalSpentGreaterThan(minPrice);
    }
    
    public List<User> getUsersInCategory(Long categoryId) {
        return userRepository.findByCategoryId(categoryId);
    }
    
    public List<User> getActiveExpensiveUsers(double minPrice) {
        return userRepository.findByActiveTrueAndTotalSpentGreaterThan(minPrice);
    }
    
    public List<User> getActiveUsersInCategory(Long categoryId) {
        return userRepository.findByActiveTrueAndCategoryId(categoryId);
    }
    
    public List<User> getUsersByScope(String scope, Object... parameters) {
        switch (scope) {
            case "active":
                return getActiveUsers();
            case "expensive":
                if (parameters.length > 0) {
                    return getExpensiveUsers((Double) parameters[0]);
                }
                return getExpensiveUsers(1000.0);
            case "category":
                if (parameters.length > 0) {
                    return getUsersInCategory((Long) parameters[0]);
                }
                return new ArrayList<>();
            default:
                return new ArrayList<>();
        }
    }
}
```

```java
// TuskLang scopes
scopes: {
    # Define scopes
    Product: @tusk{
        table: "products"
        
        # Scope methods
        scopes: {
            active: (query) => {
                query.where("active", true)
            }
            
            expensive: (query) => {
                query.where("price", ">", 1000)
            }
            
            inCategory: (query, category_id) => {
                query.where("category_id", @category_id)
            }
        }
    }
    
    # Using scopes
    active_products: @Product.active().get()
    expensive_active: @Product.active().expensive().get()
    category_products: @Product.inCategory(5).active().get()
}
```

## Accessors and Mutators

```java
// Java accessors and mutators
@Entity
@Table(name = "users")
public class User extends TuskModel {
    
    // ... existing fields ...
    
    @Column(name = "first_name")
    private String firstName;
    
    @Column(name = "last_name")
    private String lastName;
    
    @Column(name = "email")
    private String email;
    
    // Accessors (getters)
    public String getFullName() {
        return firstName + " " + lastName;
    }
    
    public String getEmailDomain() {
        if (email != null && email.contains("@")) {
            return email.substring(email.indexOf("@") + 1);
        }
        return null;
    }
    
    public boolean isActive() {
        return getDeletedAt() == null;
    }
    
    public int getAge() {
        if (getDateOfBirth() != null) {
            return Period.between(getDateOfBirth(), LocalDate.now()).getYears();
        }
        return 0;
    }
    
    // Mutators (setters)
    public void setEmail(String email) {
        this.email = email != null ? email.toLowerCase() : null;
    }
    
    public void setPassword(String password) {
        this.password = password != null ? BCrypt.hashpw(password, BCrypt.gensalt()) : null;
    }
    
    public void setFirstName(String firstName) {
        this.firstName = firstName != null ? firstName.trim() : null;
    }
    
    public void setLastName(String lastName) {
        this.lastName = lastName != null ? lastName.trim() : null;
    }
    
    // Getters and setters for new fields
    public String getFirstName() { return firstName; }
    public void setFirstName(String firstName) { this.firstName = firstName; }
    
    public String getLastName() { return lastName; }
    public void setLastName(String lastName) { this.lastName = lastName; }
    
    public String getEmail() { return email; }
    public void setEmail(String email) { this.email = email; }
    
    // Additional fields for examples
    @Column(name = "date_of_birth")
    private LocalDate dateOfBirth;
    
    public LocalDate getDateOfBirth() { return dateOfBirth; }
    public void setDateOfBirth(LocalDate dateOfBirth) { this.dateOfBirth = dateOfBirth; }
}
```

```java
// TuskLang accessors and mutators
accessors_mutators: {
    User: @tusk{
        table: "users"
        
        # Accessors (getters)
        accessors: {
            full_name: (user) => {
                return @user.first_name + " " + @user.last_name
            }
            
            email_domain: (user) => {
                return @user.email.split("@")[1] if @user.email contains "@"
            }
            
            is_active: (user) => {
                return @user.deleted_at == null
            }
            
            age: (user) => {
                return @date.diff(@user.date_of_birth, @date.now(), "years")
            }
        }
        
        # Mutators (setters)
        mutators: {
            email: (user, value) => {
                return @value.lowercase() if @value
            }
            
            password: (user, value) => {
                return @hash_password(@value) if @value
            }
            
            first_name: (user, value) => {
                return @value.trim() if @value
            }
            
            last_name: (user, value) => {
                return @value.trim() if @value
            }
        }
    }
}
```

## Model Events

```java
// Java model events
@Entity
@Table(name = "users")
public class User extends TuskModel {
    
    // ... existing fields ...
    
    @PrePersist
    protected void onCreate() {
        if (hasTimestamps()) {
            setCreatedAt(LocalDateTime.now());
            setUpdatedAt(LocalDateTime.now());
        }
    }
    
    @PreUpdate
    protected void onUpdate() {
        if (hasTimestamps()) {
            setUpdatedAt(LocalDateTime.now());
        }
    }
    
    @PreRemove
    protected void onDelete() {
        // Handle pre-delete logic
        log.info("Deleting user: {}", getId());
    }
    
    @PostLoad
    protected void onLoad() {
        // Handle post-load logic
        log.debug("Loaded user: {}", getId());
    }
    
    @PostPersist
    protected void onPostCreate() {
        // Handle post-create logic
        log.info("Created user: {}", getId());
    }
    
    @PostUpdate
    protected void onPostUpdate() {
        // Handle post-update logic
        log.info("Updated user: {}", getId());
    }
    
    @PostRemove
    protected void onPostDelete() {
        // Handle post-delete logic
        log.info("Deleted user: {}", getId());
    }
}

@Component
public class UserEventListener {
    
    @EventListener
    public void handleUserCreated(UserCreatedEvent event) {
        User user = event.getUser();
        // Send welcome email
        sendWelcomeEmail(user);
    }
    
    @EventListener
    public void handleUserUpdated(UserUpdatedEvent event) {
        User user = event.getUser();
        // Update search index
        updateSearchIndex(user);
    }
    
    @EventListener
    public void handleUserDeleted(UserDeletedEvent event) {
        User user = event.getUser();
        // Clean up related data
        cleanupUserData(user);
    }
    
    private void sendWelcomeEmail(User user) {
        // Implementation for sending welcome email
    }
    
    private void updateSearchIndex(User user) {
        // Implementation for updating search index
    }
    
    private void cleanupUserData(User user) {
        // Implementation for cleaning up user data
    }
}
```

```java
// TuskLang model events
model_events: {
    User: @tusk{
        table: "users"
        
        # Model events
        events: {
            # Before events
            before_create: (user) => {
                @user.password = @hash_password(@user.password)
            }
            
            before_update: (user) => {
                @user.updated_at = @date.now()
            }
            
            before_delete: (user) => {
                @log.info("Deleting user: " + @user.id)
            }
            
            # After events
            after_create: (user) => {
                @send_welcome_email(@user)
                @log.info("Created user: " + @user.id)
            }
            
            after_update: (user) => {
                @update_search_index(@user)
                @log.info("Updated user: " + @user.id)
            }
            
            after_delete: (user) => {
                @cleanup_user_data(@user)
                @log.info("Deleted user: " + @user.id)
            }
        }
    }
}
```

## Model Testing

```java
// JUnit test for Tusk models
@SpringBootTest
class UserModelTest {
    
    @Autowired
    private UserRepository userRepository;
    
    @Autowired
    private TuskService tuskService;
    
    @Test
    void testCreateUser() {
        User user = new User();
        user.setName("John Doe");
        user.setEmail("john@example.com");
        user.setPassword("secret");
        
        User savedUser = userRepository.save(user);
        
        assertThat(savedUser.getId()).isNotNull();
        assertThat(savedUser.getName()).isEqualTo("John Doe");
        assertThat(savedUser.getEmail()).isEqualTo("john@example.com");
        assertThat(savedUser.getCreatedAt()).isNotNull();
        assertThat(savedUser.getUpdatedAt()).isNotNull();
    }
    
    @Test
    void testFindUser() {
        User user = createTestUser();
        User foundUser = userRepository.findById(user.getId()).orElse(null);
        
        assertThat(foundUser).isNotNull();
        assertThat(foundUser.getName()).isEqualTo(user.getName());
    }
    
    @Test
    void testUpdateUser() {
        User user = createTestUser();
        user.setName("Jane Doe");
        
        User updatedUser = userRepository.save(user);
        
        assertThat(updatedUser.getName()).isEqualTo("Jane Doe");
        assertThat(updatedUser.getUpdatedAt()).isAfter(user.getCreatedAt());
    }
    
    @Test
    void testDeleteUser() {
        User user = createTestUser();
        userRepository.delete(user);
        
        User deletedUser = userRepository.findById(user.getId()).orElse(null);
        assertThat(deletedUser).isNull();
    }
    
    @Test
    void testSoftDeleteUser() {
        User user = createTestUser();
        user.setDeletedAt(LocalDateTime.now());
        userRepository.save(user);
        
        User softDeletedUser = userRepository.findById(user.getId()).orElse(null);
        assertThat(softDeletedUser).isNotNull();
        assertThat(softDeletedUser.getDeletedAt()).isNotNull();
    }
    
    @Test
    void testUserRelationships() {
        User user = createTestUser();
        Post post = new Post();
        post.setTitle("Test Post");
        post.setContent("Test Content");
        post.setUser(user);
        
        user.getPosts().add(post);
        userRepository.save(user);
        
        User savedUser = userRepository.findById(user.getId()).orElse(null);
        assertThat(savedUser.getPosts()).hasSize(1);
        assertThat(savedUser.getPosts().get(0).getTitle()).isEqualTo("Test Post");
    }
    
    private User createTestUser() {
        User user = new User();
        user.setName("Test User");
        user.setEmail("test@example.com");
        user.setPassword("secret");
        return userRepository.save(user);
    }
}
```

```java
// TuskLang model testing
test_tusk_models: {
    # Test user creation
    test_user: @User.create({
        name: "John Doe"
        email: "john@example.com"
        password: "secret"
    })
    assert(@test_user.id != null, "User should have an ID")
    assert(@test_user.name == "John Doe", "User name should match")
    assert(@test_user.created_at != null, "User should have created_at timestamp")
    
    # Test user finding
    found_user: @User.find(@test_user.id)
    assert(@found_user != null, "Should find user by ID")
    assert(@found_user.email == "john@example.com", "User email should match")
    
    # Test user update
    @found_user.name = "Jane Doe"
    @found_user.save()
    updated_user: @User.find(@test_user.id)
    assert(@updated_user.name == "Jane Doe", "User name should be updated")
    
    # Test user deletion
    @updated_user.delete()
    deleted_user: @User.find(@test_user.id)
    assert(@deleted_user == null, "User should be deleted")
    
    # Test relationships
    user_with_posts: @User.with("posts").find(@test_user.id)
    assert(@user_with_posts.posts != null, "User should have posts relationship")
}
```

## Best Practices

### 1. Model Design
```java
// Design models with proper separation of concerns
@Entity
@Table(name = "users")
public class User extends TuskModel {
    
    // Use proper annotations for validation
    @NotNull
    @Size(min = 2, max = 50)
    @Column(name = "name")
    private String name;
    
    @NotNull
    @Email
    @Column(name = "email", unique = true)
    private String email;
    
    @NotNull
    @Size(min = 8)
    @Column(name = "password")
    private String password;
    
    // Use enums for status fields
    @Enumerated(EnumType.STRING)
    @Column(name = "status")
    private UserStatus status = UserStatus.ACTIVE;
    
    // Use proper data types
    @Column(name = "date_of_birth")
    private LocalDate dateOfBirth;
    
    @Column(name = "last_login")
    private LocalDateTime lastLogin;
    
    // Use JSON for flexible data
    @Column(name = "metadata", columnDefinition = "json")
    private String metadata;
    
    // Implement proper equals and hashCode
    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;
        User user = (User) o;
        return Objects.equals(id, user.id);
    }
    
    @Override
    public int hashCode() {
        return Objects.hash(id);
    }
    
    // Implement toString for debugging
    @Override
    public String toString() {
        return "User{" +
                "id=" + id +
                ", name='" + name + '\'' +
                ", email='" + email + '\'' +
                ", status=" + status +
                '}';
    }
}
```

### 2. Repository Pattern
```java
// Implement repository pattern for data access
@Repository
public interface UserRepository extends JpaRepository<User, Long> {
    
    Optional<User> findByEmail(String email);
    
    List<User> findByActiveTrue();
    
    List<User> findByStatus(UserStatus status);
    
    List<User> findByCreatedAtBetween(LocalDateTime start, LocalDateTime end);
    
    @Query("SELECT u FROM User u WHERE u.totalSpent > :amount")
    List<User> findExpensiveUsers(@Param("amount") double amount);
    
    @Query("SELECT COUNT(u) FROM User u WHERE u.status = :status")
    long countByStatus(@Param("status") UserStatus status);
    
    @Query("SELECT u FROM User u JOIN u.posts p WHERE p.createdAt > :date")
    List<User> findUsersWithRecentPosts(@Param("date") LocalDateTime date);
}

@Service
public class UserService {
    
    @Autowired
    private UserRepository userRepository;
    
    public User createUser(UserDto userDto) {
        User user = new User();
        user.setName(userDto.getName());
        user.setEmail(userDto.getEmail());
        user.setPassword(hashPassword(userDto.getPassword()));
        
        return userRepository.save(user);
    }
    
    public User findByEmail(String email) {
        return userRepository.findByEmail(email)
            .orElseThrow(() -> new EntityNotFoundException("User not found"));
    }
    
    public List<User> getActiveUsers() {
        return userRepository.findByActiveTrue();
    }
    
    public List<User> getExpensiveUsers(double minAmount) {
        return userRepository.findExpensiveUsers(minAmount);
    }
}
```

### 3. Data Validation
```java
// Implement comprehensive data validation
@Component
public class UserValidationService {
    
    @Autowired
    private UserRepository userRepository;
    
    public ValidationResult validateUser(User user) {
        ValidationResult result = new ValidationResult();
        
        // Validate name
        if (user.getName() == null || user.getName().trim().isEmpty()) {
            result.addError("name", "Name is required");
        } else if (user.getName().length() < 2) {
            result.addError("name", "Name must be at least 2 characters");
        }
        
        // Validate email
        if (user.getEmail() == null || user.getEmail().trim().isEmpty()) {
            result.addError("email", "Email is required");
        } else if (!isValidEmail(user.getEmail())) {
            result.addError("email", "Invalid email format");
        } else if (isEmailDuplicate(user.getEmail(), user.getId())) {
            result.addError("email", "Email already exists");
        }
        
        // Validate password
        if (user.getPassword() == null || user.getPassword().isEmpty()) {
            result.addError("password", "Password is required");
        } else if (user.getPassword().length() < 8) {
            result.addError("password", "Password must be at least 8 characters");
        }
        
        return result;
    }
    
    private boolean isValidEmail(String email) {
        String emailRegex = "^[A-Za-z0-9+_.-]+@(.+)$";
        return email.matches(emailRegex);
    }
    
    private boolean isEmailDuplicate(String email, Long excludeId) {
        Optional<User> existingUser = userRepository.findByEmail(email);
        return existingUser.isPresent() && !existingUser.get().getId().equals(excludeId);
    }
}

public class ValidationResult {
    private Map<String, List<String>> errors = new HashMap<>();
    
    public void addError(String field, String message) {
        errors.computeIfAbsent(field, k -> new ArrayList<>()).add(message);
    }
    
    public boolean isValid() {
        return errors.isEmpty();
    }
    
    public Map<String, List<String>> getErrors() {
        return errors;
    }
}
```

The `@tusk` operator in Java provides a powerful Active Record pattern that simplifies database operations while maintaining the flexibility and performance of enterprise-grade ORM solutions. 