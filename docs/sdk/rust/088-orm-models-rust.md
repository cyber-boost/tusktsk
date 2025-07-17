# ORM Models in TuskLang for Rust

TuskLang's Rust ORM provides a type-safe, trait-based model system that leverages Rust's ownership model, compile-time guarantees, and async/await capabilities to create a revolutionary object-relational mapping experience.

## 🚀 **Why Rust ORM Models?**

Rust's trait system, ownership model, and type safety make it the perfect language for ORM development:

- **Trait-Based Design**: Compile-time polymorphism with zero runtime overhead
- **Ownership Safety**: Automatic memory management with no garbage collection
- **Type Safety**: Compile-time validation of model relationships
- **Async/Await**: Non-blocking database operations
- **Zero-Cost Abstractions**: No performance penalty for safety

## Basic Model Definition

```rust
use tusk_db::{Model, ModelBuilder, Result};
use serde::{Deserialize, Serialize};
use async_trait::async_trait;
use chrono::{DateTime, Utc};

// Define a basic model with Rust struct
#[derive(Debug, Serialize, Deserialize, Clone)]
struct User {
    pub id: Option<i32>,
    pub name: String,
    pub email: String,
    pub password_hash: String,
    pub email_verified_at: Option<DateTime<Utc>>,
    pub is_active: bool,
    pub created_at: Option<DateTime<Utc>>,
    pub updated_at: Option<DateTime<Utc>>,
}

// Implement the Model trait for type safety
#[async_trait]
impl Model for User {
    fn table_name() -> &'static str {
        "users"
    }
    
    fn primary_key() -> &'static str {
        "id"
    }
    
    fn fillable_fields() -> &'static [&'static str] {
        &["name", "email", "password_hash", "is_active"]
    }
    
    fn hidden_fields() -> &'static [&'static str] {
        &["password_hash"]
    }
    
    fn guarded_fields() -> &'static [&'static str] {
        &["id", "created_at", "updated_at"]
    }
    
    // Timestamps configuration
    fn uses_timestamps() -> bool {
        true
    }
    
    fn timestamp_fields() -> (&'static str, &'static str) {
        ("created_at", "updated_at")
    }
    
    // Soft deletes support
    fn uses_soft_deletes() -> bool {
        true
    }
    
    fn soft_delete_field() -> &'static str {
        "deleted_at"
    }
}
```

## Model Attributes and Configuration

```rust
use tusk_db::{ModelAttribute, CastType, ValidationRule};

// Advanced model with attributes and validation
#[derive(Debug, Serialize, Deserialize, Clone)]
struct Post {
    pub id: Option<i32>,
    pub title: String,
    pub content: String,
    pub user_id: i32,
    pub published: bool,
    pub view_count: i32,
    pub rating: Option<f64>,
    pub tags: Option<Vec<String>>,
    pub metadata: Option<serde_json::Value>,
    pub created_at: Option<DateTime<Utc>>,
    pub updated_at: Option<DateTime<Utc>>,
}

#[async_trait]
impl Model for Post {
    fn table_name() -> &'static str {
        "posts"
    }
    
    fn primary_key() -> &'static str {
        "id"
    }
    
    fn fillable_fields() -> &'static [&'static str] {
        &["title", "content", "user_id", "published", "tags", "metadata"]
    }
    
    fn hidden_fields() -> &'static [&'static str] {
        &[]
    }
    
    // Type casting for complex fields
    fn casts() -> &'static [(&'static str, CastType)] {
        &[
            ("tags", CastType::Json),
            ("metadata", CastType::Json),
            ("rating", CastType::Float),
            ("view_count", CastType::Integer),
        ]
    }
    
    // Validation rules
    fn validation_rules() -> &'static [(&'static str, ValidationRule)] {
        &[
            ("title", ValidationRule::Required),
            ("title", ValidationRule::MaxLength(255)),
            ("content", ValidationRule::Required),
            ("content", ValidationRule::MinLength(10)),
            ("user_id", ValidationRule::Required),
            ("user_id", ValidationRule::Exists("users", "id")),
        ]
    }
    
    // Default values
    fn defaults() -> &'static [(&'static str, &'static str)] {
        &[
            ("published", "false"),
            ("view_count", "0"),
        ]
    }
}
```

## Model Relationships

```rust
use tusk_db::{Relationship, HasMany, HasOne, BelongsTo, BelongsToMany};

// Define related models
#[derive(Debug, Serialize, Deserialize, Clone)]
struct Comment {
    pub id: Option<i32>,
    pub content: String,
    pub user_id: i32,
    pub post_id: i32,
    pub created_at: Option<DateTime<Utc>>,
}

#[derive(Debug, Serialize, Deserialize, Clone)]
struct UserProfile {
    pub id: Option<i32>,
    pub user_id: i32,
    pub bio: Option<String>,
    pub avatar_url: Option<String>,
    pub website: Option<String>,
}

#[derive(Debug, Serialize, Deserialize, Clone)]
struct Role {
    pub id: Option<i32>,
    pub name: String,
    pub description: Option<String>,
}

// Implement relationships for User model
#[async_trait]
impl User {
    // One-to-Many: User has many Posts
    async fn posts(&self) -> Result<Vec<Post>> {
        @has_many::<Post>(self.id.unwrap(), "user_id").await
    }
    
    // One-to-One: User has one Profile
    async fn profile(&self) -> Result<Option<UserProfile>> {
        @has_one::<UserProfile>(self.id.unwrap(), "user_id").await
    }
    
    // Many-to-Many: User belongs to many Roles
    async fn roles(&self) -> Result<Vec<Role>> {
        @belongs_to_many::<Role>(
            self.id.unwrap(),
            "user_roles",
            "user_id",
            "role_id"
        ).await
    }
    
    // Has Many Through: User has many Comments through Posts
    async fn comments(&self) -> Result<Vec<Comment>> {
        @has_many_through::<Comment, Post>(
            self.id.unwrap(),
            "user_id",
            "post_id"
        ).await
    }
}

// Implement relationships for Post model
#[async_trait]
impl Post {
    // Belongs To: Post belongs to User
    async fn user(&self) -> Result<User> {
        @belongs_to::<User>(self.user_id, "id").await
    }
    
    // Has Many: Post has many Comments
    async fn comments(&self) -> Result<Vec<Comment>> {
        @has_many::<Comment>(self.id.unwrap(), "post_id").await
    }
    
    // Has Many Through: Post has many Users through Comments
    async fn commenters(&self) -> Result<Vec<User>> {
        @has_many_through::<User, Comment>(
            self.id.unwrap(),
            "post_id",
            "user_id"
        ).await
    }
}
```

## Model Scopes and Query Building

```rust
use tusk_db::{QueryBuilder, Scope};

// Define scopes for reusable query logic
impl User {
    // Local scopes for query building
    fn scope_active(query: QueryBuilder) -> QueryBuilder {
        query.where_eq("is_active", true)
    }
    
    fn scope_recent(query: QueryBuilder) -> QueryBuilder {
        query.where_gt("created_at", Utc::now() - chrono::Duration::days(30))
    }
    
    fn scope_with_posts(query: QueryBuilder) -> QueryBuilder {
        query.with(&["posts"])
    }
    
    fn scope_popular(query: QueryBuilder) -> QueryBuilder {
        query.with(&["posts"])
            .where_exists(|q| {
                q.table("posts")
                    .where_raw("posts.user_id = users.id", &[])
                    .where_eq("published", true)
            })
    }
    
    // Global scopes (always applied)
    fn global_scopes() -> &'static [&'static str] {
        &["active_only"]
    }
    
    fn scope_active_only(query: QueryBuilder) -> QueryBuilder {
        query.where_eq("is_active", true)
    }
}

// Using scopes in queries
async fn get_active_users_with_posts() -> Result<Vec<User>> {
    let users = @User::query()
        .scope_active()
        .scope_with_posts()
        .order_by("name")
        .get()
        .await?;
    
    Ok(users)
}

async fn get_popular_users() -> Result<Vec<User>> {
    let users = @User::query()
        .scope_popular()
        .order_by("created_at", "DESC")
        .limit(10)
        .get()
        .await?;
    
    Ok(users)
}
```

## Model Accessors and Mutators

```rust
use tusk_db::{Accessor, Mutator};

impl User {
    // Accessors: Transform data when retrieving from database
    fn get_full_name_attribute(&self) -> String {
        format!("{} {}", self.first_name, self.last_name)
    }
    
    fn get_email_domain_attribute(&self) -> String {
        self.email.split('@').nth(1).unwrap_or("").to_string()
    }
    
    fn get_is_verified_attribute(&self) -> bool {
        self.email_verified_at.is_some()
    }
    
    // Mutators: Transform data before saving to database
    fn set_email_attribute(&mut self, value: String) {
        self.email = value.to_lowercase();
    }
    
    fn set_password_attribute(&mut self, value: String) {
        self.password_hash = hash_password(&value);
    }
    
    fn set_name_attribute(&mut self, value: String) {
        let parts: Vec<&str> = value.split_whitespace().collect();
        if parts.len() >= 2 {
            self.first_name = parts[0].to_string();
            self.last_name = parts[1..].join(" ");
        } else {
            self.first_name = value;
            self.last_name = String::new();
        }
    }
}

// Using accessors and mutators
async fn user_operations() -> Result<()> {
    // Create user with mutator
    let mut user = User {
        id: None,
        first_name: String::new(),
        last_name: String::new(),
        email: "JOHN@EXAMPLE.COM".to_string(), // Will be lowercased
        password_hash: String::new(),
        email_verified_at: None,
        is_active: true,
        created_at: None,
        updated_at: None,
    };
    
    // Mutators are applied automatically
    user.set_name("John Doe".to_string());
    user.set_password("secret123".to_string());
    
    let saved_user = @User::create(user).await?;
    
    // Accessors are applied when retrieving
    assert_eq!(saved_user.full_name, "John Doe");
    assert_eq!(saved_user.email_domain, "example.com");
    assert_eq!(saved_user.is_verified, false);
    
    Ok(())
}
```

## Model Events and Hooks

```rust
use tusk_db::{ModelEvent, EventHook};

// Define model events
#[derive(Debug)]
enum UserEvent {
    Creating,
    Created,
    Updating,
    Updated,
    Deleting,
    Deleted,
    Saving,
    Saved,
}

impl User {
    // Event hooks for model lifecycle
    async fn on_creating(&mut self) -> Result<()> {
        // Set default values before creation
        if self.created_at.is_none() {
            self.created_at = Some(Utc::now());
        }
        Ok(())
    }
    
    async fn on_created(&self) -> Result<()> {
        // Actions after creation
        @log::info!("User created", { user_id: self.id, email: &self.email });
        
        // Send welcome email
        @send_welcome_email(&self.email).await?;
        
        Ok(())
    }
    
    async fn on_updating(&mut self) -> Result<()> {
        // Set updated timestamp
        self.updated_at = Some(Utc::now());
        Ok(())
    }
    
    async fn on_updated(&self) -> Result<()> {
        // Actions after update
        @log::info!("User updated", { user_id: self.id });
        
        // Clear cache
        @cache::forget(&format!("user:{}", self.id.unwrap())).await;
        
        Ok(())
    }
    
    async fn on_deleting(&self) -> Result<()> {
        // Actions before deletion
        @log::warn!("User being deleted", { user_id: self.id });
        
        // Archive user data
        @archive_user_data(self.id.unwrap()).await?;
        
        Ok(())
    }
    
    async fn on_deleted(&self) -> Result<()> {
        // Actions after deletion
        @log::info!("User deleted", { user_id: self.id });
        
        // Notify administrators
        @notify_admin_user_deleted(self.id.unwrap()).await?;
        
        Ok(())
    }
}

// Register event listeners
async fn register_user_events() -> Result<()> {
    @User::listen(UserEvent::Created, |user| async {
        @send_welcome_email(&user.email).await?;
        Ok(())
    }).await;
    
    @User::listen(UserEvent::Updated, |user| async {
        @cache::forget(&format!("user:{}", user.id.unwrap())).await;
        Ok(())
    }).await;
    
    Ok(())
}
```

## Model Serialization and API Responses

```rust
use serde::{Serialize, Deserialize};
use tusk_db::{ModelSerializer, ApiResource};

// Define API resources for different contexts
#[derive(Debug, Serialize)]
struct UserResource {
    id: i32,
    name: String,
    email: String,
    is_active: bool,
    created_at: DateTime<Utc>,
    posts_count: Option<i64>,
}

#[derive(Debug, Serialize)]
struct UserDetailResource {
    id: i32,
    name: String,
    email: String,
    is_active: bool,
    email_verified_at: Option<DateTime<Utc>>,
    created_at: DateTime<Utc>,
    updated_at: DateTime<Utc>,
    profile: Option<UserProfileResource>,
    posts: Vec<PostResource>,
}

impl User {
    // Convert to API resource
    fn to_resource(&self) -> UserResource {
        UserResource {
            id: self.id.unwrap(),
            name: self.name.clone(),
            email: self.email.clone(),
            is_active: self.is_active,
            created_at: self.created_at.unwrap(),
            posts_count: None,
        }
    }
    
    // Convert to detailed resource with relationships
    async fn to_detail_resource(&self) -> Result<UserDetailResource> {
        let profile = self.profile().await?;
        let posts = self.posts().await?;
        
        Ok(UserDetailResource {
            id: self.id.unwrap(),
            name: self.name.clone(),
            email: self.email.clone(),
            is_active: self.is_active,
            email_verified_at: self.email_verified_at,
            created_at: self.created_at.unwrap(),
            updated_at: self.updated_at.unwrap(),
            profile: profile.map(|p| p.to_resource()),
            posts: posts.into_iter().map(|p| p.to_resource()).collect(),
        })
    }
    
    // Collection resource with pagination
    async fn to_collection_resource(users: Vec<User>) -> Result<ApiResource<UserResource>> {
        let resources: Vec<UserResource> = users
            .into_iter()
            .map(|u| u.to_resource())
            .collect();
        
        Ok(ApiResource {
            data: resources,
            meta: None,
            links: None,
        })
    }
}
```

## Model Validation and Error Handling

```rust
use tusk_db::{ValidationError, ValidationResult};
use validator::{Validate, ValidationError as ValidatorError};

// Custom validation for User model
impl User {
    fn validate(&self) -> ValidationResult {
        let mut errors = Vec::new();
        
        // Required fields
        if self.name.is_empty() {
            errors.push(ValidationError::new("name", "Name is required"));
        }
        
        if self.email.is_empty() {
            errors.push(ValidationError::new("email", "Email is required"));
        }
        
        // Email format validation
        if !self.email.contains('@') {
            errors.push(ValidationError::new("email", "Invalid email format"));
        }
        
        // Custom business logic validation
        if self.name.len() < 2 {
            errors.push(ValidationError::new("name", "Name must be at least 2 characters"));
        }
        
        if self.name.len() > 100 {
            errors.push(ValidationError::new("name", "Name must be less than 100 characters"));
        }
        
        if errors.is_empty() {
            Ok(())
        } else {
            Err(errors)
        }
    }
    
    // Async validation for complex rules
    async fn validate_async(&self) -> ValidationResult {
        let mut errors = Vec::new();
        
        // Check if email is unique
        if let Some(existing_user) = @User::where_eq("email", &self.email)
            .where_ne("id", self.id.unwrap_or(0))
            .first()
            .await?
        {
            errors.push(ValidationError::new("email", "Email already exists"));
        }
        
        // Check if user can be created (business rules)
        if !@can_create_user(&self.email).await? {
            errors.push(ValidationError::new("email", "User creation not allowed"));
        }
        
        if errors.is_empty() {
            Ok(())
        } else {
            Err(errors)
        }
    }
}

// Using validation in model operations
async fn create_validated_user(user_data: User) -> Result<User> {
    let mut user = user_data;
    
    // Validate before saving
    user.validate()?;
    user.validate_async().await?;
    
    // Create user if validation passes
    let saved_user = @User::create(user).await?;
    
    Ok(saved_user)
}
```

## Model Factories and Testing

```rust
use tusk_db::{ModelFactory, FactoryBuilder};

// Define factory for User model
struct UserFactory;

impl ModelFactory<User> for UserFactory {
    fn definition() -> FactoryBuilder<User> {
        FactoryBuilder::new()
            .field("name", |faker| faker.name().name())
            .field("email", |faker| faker.internet().email())
            .field("password_hash", |_| hash_password("password"))
            .field("is_active", |_| true)
            .field("email_verified_at", |_| Some(Utc::now()))
    }
    
    fn states() -> &'static [(&'static str, Box<dyn Fn(&mut User) + Send + Sync>)] {
        &[
            ("inactive", Box::new(|user| user.is_active = false)),
            ("unverified", Box::new(|user| user.email_verified_at = None)),
            ("admin", Box::new(|user| {
                user.name = "Admin User".to_string();
                user.email = "admin@example.com".to_string();
            })),
        ]
    }
}

// Using factories in tests
#[tokio::test]
async fn test_user_creation() -> Result<()> {
    let test_db = @TestDatabase::new().await?;
    let mut tx = test_db.begin_transaction().await?;
    
    // Create a basic user
    let user = @UserFactory::new().create().await?;
    assert!(user.id.is_some());
    assert!(!user.name.is_empty());
    assert!(!user.email.is_empty());
    
    // Create user with specific state
    let inactive_user = @UserFactory::new()
        .state("inactive")
        .create()
        .await?;
    assert_eq!(inactive_user.is_active, false);
    
    // Create multiple users
    let users = @UserFactory::new()
        .count(5)
        .create()
        .await?;
    assert_eq!(users.len(), 5);
    
    // Create user with specific attributes
    let admin_user = @UserFactory::new()
        .state("admin")
        .field("email", "custom@example.com")
        .create()
        .await?;
    assert_eq!(admin_user.email, "custom@example.com");
    
    tx.rollback().await?;
    Ok(())
}
```

## Model Performance Optimization

```rust
use tusk_db::{EagerLoading, QueryOptimizer};

impl User {
    // Eager loading to avoid N+1 queries
    async fn with_relationships() -> Result<Vec<User>> {
        let users = @User::query()
            .with(&["posts", "posts.comments", "profile", "roles"])
            .get()
            .await?;
        
        Ok(users)
    }
    
    // Selective loading for performance
    async fn with_selective_relationships() -> Result<Vec<User>> {
        let users = @User::query()
            .select(&["id", "name", "email"])
            .with(&[("posts", |query| {
                query.select(&["id", "title", "created_at"])
                    .where_eq("published", true)
                    .limit(5)
            })])
            .get()
            .await?;
        
        Ok(users)
    }
    
    // Caching frequently accessed data
    async fn cached_user(user_id: i32) -> Result<User> {
        let cache_key = format!("user:{}", user_id);
        
        let user = @cache::remember(&cache_key, 3600, || async {
            @User::find(user_id).await
        }).await?;
        
        Ok(user)
    }
    
    // Batch operations for performance
    async fn update_multiple_users(updates: Vec<(i32, HashMap<String, String>)>) -> Result<u64> {
        let mut affected = 0;
        
        for (user_id, fields) in updates {
            let result = @User::where_eq("id", user_id)
                .update(&fields)
                .await?;
            affected += result;
        }
        
        Ok(affected)
    }
}
```

## Best Practices for Rust ORM Models

1. **Use Strong Types**: Leverage Rust's type system for compile-time safety
2. **Implement Traits**: Use the Model trait for consistent behavior
3. **Handle Errors**: Use proper error types and Result handling
4. **Async/Await**: Use non-blocking operations for better performance
5. **Validation**: Implement comprehensive validation rules
6. **Events**: Use model events for business logic
7. **Relationships**: Define clear relationship boundaries
8. **Scopes**: Use scopes for reusable query logic
9. **Factories**: Use factories for testing and seeding
10. **Performance**: Optimize with eager loading and caching

## Related Topics

- `database-overview-rust` - Database integration overview
- `query-builder-rust` - Fluent query interface
- `migrations-rust` - Database schema versioning
- `relationships-rust` - Model relationships
- `database-transactions-rust` - Transaction handling

---

**Ready to build type-safe, performant ORM models with Rust and TuskLang?** 