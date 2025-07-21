# Model Relationships in TuskLang for Rust

TuskLang's Rust relationship system provides type-safe, async-first model relationships with compile-time guarantees, zero-cost abstractions, and comprehensive relationship management capabilities.

## 🚀 **Why Rust Relationships?**

Rust's type system and ownership model make it the perfect language for model relationships:

- **Type Safety**: Compile-time validation of relationship types
- **Ownership Safety**: Automatic memory management for related objects
- **Async/Await**: Non-blocking relationship loading
- **Zero-Cost Abstractions**: No performance penalty for safety
- **Relationship Integrity**: Guaranteed referential integrity

## Basic Relationship Types

```rust
use tusk_db::{Model, Relationship, Result};
use serde::{Deserialize, Serialize};
use async_trait::async_trait;
use chrono::{DateTime, Utc};

// Define related models
#[derive(Debug, Serialize, Deserialize, Clone)]
struct User {
    pub id: Option<i32>,
    pub name: String,
    pub email: String,
    pub is_active: bool,
    pub created_at: Option<DateTime<Utc>>,
    pub updated_at: Option<DateTime<Utc>>,
}

#[derive(Debug, Serialize, Deserialize, Clone)]
struct Post {
    pub id: Option<i32>,
    pub title: String,
    pub content: String,
    pub user_id: i32,
    pub published: bool,
    pub created_at: Option<DateTime<Utc>>,
    pub updated_at: Option<DateTime<Utc>>,
}

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

#[derive(Debug, Serialize, Deserialize, Clone)]
struct UserRole {
    pub id: Option<i32>,
    pub user_id: i32,
    pub role_id: i32,
    pub created_at: Option<DateTime<Utc>>,
}
```

## One-to-Many Relationships

```rust
use tusk_db::{HasMany, BelongsTo};

// User has many Posts
#[async_trait]
impl User {
    // One-to-Many: User has many Posts
    async fn posts(&self) -> Result<Vec<Post>> {
        @has_many::<Post>(self.id.unwrap(), "user_id").await
    }
    
    // One-to-Many with conditions
    async fn published_posts(&self) -> Result<Vec<Post>> {
        @has_many::<Post>(self.id.unwrap(), "user_id")
            .where_eq("published", true)
            .order_by("created_at", "DESC")
            .await
    }
    
    // One-to-Many with eager loading
    async fn posts_with_comments(&self) -> Result<Vec<Post>> {
        @has_many::<Post>(self.id.unwrap(), "user_id")
            .with(&["comments"])
            .where_eq("published", true)
            .await
    }
    
    // Count related records
    async fn posts_count(&self) -> Result<i64> {
        @has_many::<Post>(self.id.unwrap(), "user_id")
            .count()
            .await
    }
    
    // Check if has related records
    async fn has_posts(&self) -> Result<bool> {
        @has_many::<Post>(self.id.unwrap(), "user_id")
            .exists()
            .await
    }
}

// Post belongs to User
#[async_trait]
impl Post {
    // Belongs To: Post belongs to User
    async fn user(&self) -> Result<User> {
        @belongs_to::<User>(self.user_id, "id").await
    }
    
    // Belongs To with eager loading
    async fn user_with_profile(&self) -> Result<User> {
        @belongs_to::<User>(self.user_id, "id")
            .with(&["profile"])
            .await
    }
    
    // Belongs To with conditions
    async fn active_user(&self) -> Result<Option<User>> {
        @belongs_to::<User>(self.user_id, "id")
            .where_eq("is_active", true)
            .first()
            .await
    }
}
```

## One-to-One Relationships

```rust
use tusk_db::HasOne;

// User has one Profile
#[async_trait]
impl User {
    // One-to-One: User has one Profile
    async fn profile(&self) -> Result<Option<UserProfile>> {
        @has_one::<UserProfile>(self.id.unwrap(), "user_id").await
    }
    
    // One-to-One with conditions
    async fn active_profile(&self) -> Result<Option<UserProfile>> {
        @has_one::<UserProfile>(self.id.unwrap(), "user_id")
            .where_not_null("bio")
            .await
    }
    
    // Create or update one-to-one relationship
    async fn create_or_update_profile(&self, profile_data: UserProfile) -> Result<UserProfile> {
        if let Some(existing_profile) = self.profile().await? {
            // Update existing profile
            @UserProfile::update(existing_profile.id.unwrap(), profile_data).await
        } else {
            // Create new profile
            let mut new_profile = profile_data;
            new_profile.user_id = self.id.unwrap();
            @UserProfile::create(new_profile).await
        }
    }
}

// UserProfile belongs to User
#[async_trait]
impl UserProfile {
    async fn user(&self) -> Result<User> {
        @belongs_to::<User>(self.user_id, "id").await
    }
}
```

## Many-to-Many Relationships

```rust
use tusk_db::BelongsToMany;

// User belongs to many Roles
#[async_trait]
impl User {
    // Many-to-Many: User belongs to many Roles
    async fn roles(&self) -> Result<Vec<Role>> {
        @belongs_to_many::<Role>(
            self.id.unwrap(),
            "user_roles",
            "user_id",
            "role_id"
        ).await
    }
    
    // Many-to-Many with conditions
    async fn active_roles(&self) -> Result<Vec<Role>> {
        @belongs_to_many::<Role>(
            self.id.unwrap(),
            "user_roles",
            "user_id",
            "role_id"
        )
        .where_eq("roles.is_active", true)
        .await
    }
    
    // Attach role to user
    async fn attach_role(&self, role_id: i32) -> Result<()> {
        @belongs_to_many::<Role>(
            self.id.unwrap(),
            "user_roles",
            "user_id",
            "role_id"
        )
        .attach(role_id)
        .await
    }
    
    // Detach role from user
    async fn detach_role(&self, role_id: i32) -> Result<()> {
        @belongs_to_many::<Role>(
            self.id.unwrap(),
            "user_roles",
            "user_id",
            "role_id"
        )
        .detach(role_id)
        .await
    }
    
    // Sync roles (replace all roles)
    async fn sync_roles(&self, role_ids: Vec<i32>) -> Result<()> {
        @belongs_to_many::<Role>(
            self.id.unwrap(),
            "user_roles",
            "user_id",
            "role_id"
        )
        .sync(role_ids)
        .await
    }
    
    // Check if user has specific role
    async fn has_role(&self, role_name: &str) -> Result<bool> {
        @belongs_to_many::<Role>(
            self.id.unwrap(),
            "user_roles",
            "user_id",
            "role_id"
        )
        .where_eq("roles.name", role_name)
        .exists()
        .await
    }
}

// Role belongs to many Users
#[async_trait]
impl Role {
    async fn users(&self) -> Result<Vec<User>> {
        @belongs_to_many::<User>(
            self.id.unwrap(),
            "user_roles",
            "role_id",
            "user_id"
        ).await
    }
    
    // Count users with this role
    async fn users_count(&self) -> Result<i64> {
        @belongs_to_many::<User>(
            self.id.unwrap(),
            "user_roles",
            "role_id",
            "user_id"
        )
        .count()
        .await
    }
}
```

## Has Many Through Relationships

```rust
use tusk_db::HasManyThrough;

// User has many Comments through Posts
#[async_trait]
impl User {
    // Has Many Through: User has many Comments through Posts
    async fn comments(&self) -> Result<Vec<Comment>> {
        @has_many_through::<Comment, Post>(
            self.id.unwrap(),
            "user_id",
            "post_id"
        ).await
    }
    
    // Has Many Through with conditions
    async fn recent_comments(&self) -> Result<Vec<Comment>> {
        @has_many_through::<Comment, Post>(
            self.id.unwrap(),
            "user_id",
            "post_id"
        )
        .where_gt("comments.created_at", Utc::now() - chrono::Duration::days(7))
        .order_by("comments.created_at", "DESC")
        .await
    }
    
    // Has Many Through with eager loading
    async fn comments_with_posts(&self) -> Result<Vec<Comment>> {
        @has_many_through::<Comment, Post>(
            self.id.unwrap(),
            "user_id",
            "post_id"
        )
        .with(&["post"])
        .await
    }
}

// Post has many Users through Comments
#[async_trait]
impl Post {
    async fn commenters(&self) -> Result<Vec<User>> {
        @has_many_through::<User, Comment>(
            self.id.unwrap(),
            "post_id",
            "user_id"
        ).await
    }
    
    // Distinct commenters
    async fn distinct_commenters(&self) -> Result<Vec<User>> {
        @has_many_through::<User, Comment>(
            self.id.unwrap(),
            "post_id",
            "user_id"
        )
        .distinct()
        .await
    }
}
```

## Polymorphic Relationships

```rust
use tusk_db::MorphMany;

// Polymorphic relationship example
#[derive(Debug, Serialize, Deserialize, Clone)]
struct Activity {
    pub id: Option<i32>,
    pub subject_type: String,
    pub subject_id: i32,
    pub action: String,
    pub description: String,
    pub created_at: Option<DateTime<Utc>>,
}

// User can have many activities
#[async_trait]
impl User {
    // Polymorphic: User has many Activities
    async fn activities(&self) -> Result<Vec<Activity>> {
        @morph_many::<Activity>(
            self.id.unwrap(),
            "subject_id",
            "subject_type",
            "User"
        ).await
    }
    
    // Recent activities
    async fn recent_activities(&self) -> Result<Vec<Activity>> {
        @morph_many::<Activity>(
            self.id.unwrap(),
            "subject_id",
            "subject_type",
            "User"
        )
        .where_gt("created_at", Utc::now() - chrono::Duration::days(30))
        .order_by("created_at", "DESC")
        .limit(10)
        .await
    }
}

// Post can also have many activities
#[async_trait]
impl Post {
    async fn activities(&self) -> Result<Vec<Activity>> {
        @morph_many::<Activity>(
            self.id.unwrap(),
            "subject_id",
            "subject_type",
            "Post"
        ).await
    }
}
```

## Eager Loading and Performance

```rust
use tusk_db::{EagerLoading, WithClause};

// Eager loading relationships
async fn get_users_with_relationships() -> Result<Vec<User>> {
    let users = @User::query()
        .with(&["posts", "posts.comments", "profile", "roles"])
        .where_eq("is_active", true)
        .get()
        .await?;
    
    Ok(users)
}

// Selective eager loading
async fn get_users_with_selective_relationships() -> Result<Vec<User>> {
    let users = @User::query()
        .with(&[
            ("posts", |query| {
                query.where_eq("published", true)
                    .order_by("created_at", "DESC")
                    .limit(5)
            }),
            ("profile", |query| {
                query.select(&["id", "user_id", "bio"])
            }),
            ("roles", |query| {
                query.where_eq("is_active", true)
            })
        ])
        .get()
        .await?;
    
    Ok(users)
}

// Nested eager loading
async fn get_users_with_nested_relationships() -> Result<Vec<User>> {
    let users = @User::query()
        .with(&[
            "posts",
            "posts.comments",
            "posts.comments.user",
            "profile",
            "roles"
        ])
        .get()
        .await?;
    
    Ok(users)
}

// Lazy loading with caching
async fn get_user_with_cached_relationships(user_id: i32) -> Result<User> {
    let cache_key = format!("user:{}:with_relationships", user_id);
    
    let user = @cache::remember(&cache_key, 3600, || async {
        @User::find(user_id)
            .with(&["posts", "profile", "roles"])
            .await
    }).await?;
    
    Ok(user)
}
```

## Relationship Constraints and Validation

```rust
use tusk_db::{RelationshipConstraint, ValidationError};

// Relationship constraints
#[async_trait]
impl User {
    // Constrained relationship
    async fn active_posts(&self) -> Result<Vec<Post>> {
        @has_many::<Post>(self.id.unwrap(), "user_id")
            .where_eq("published", true)
            .where_eq("is_active", true)
            .order_by("created_at", "DESC")
            .await
    }
    
    // Relationship with validation
    async fn create_post(&self, post_data: Post) -> Result<Post> {
        // Validate user can create posts
        if !self.can_create_posts().await? {
            return Err(ValidationError::new("user", "User cannot create posts").into());
        }
        
        let mut post = post_data;
        post.user_id = self.id.unwrap();
        
        @Post::create(post).await
    }
    
    // Check if user can create posts
    async fn can_create_posts(&self) -> Result<bool> {
        // Business logic validation
        let post_count = self.posts_count().await?;
        let is_premium = self.has_role("premium").await?;
        
        Ok(is_premium || post_count < 10)
    }
}

// Relationship integrity checks
async fn validate_user_relationships(user_id: i32) -> Result<()> {
    let user = @User::find(user_id).await?;
    
    // Check if user has required relationships
    let profile = user.profile().await?;
    if profile.is_none() {
        return Err(ValidationError::new("profile", "User must have a profile").into());
    }
    
    // Check if user has at least one role
    let roles = user.roles().await?;
    if roles.is_empty() {
        return Err(ValidationError::new("roles", "User must have at least one role").into());
    }
    
    Ok(())
}
```

## Relationship Events and Hooks

```rust
use tusk_db::{RelationshipEvent, EventHook};

// Relationship events
#[async_trait]
impl User {
    // Event when user is created
    async fn on_created(&self) -> Result<()> {
        // Create default profile
        let profile = UserProfile {
            id: None,
            user_id: self.id.unwrap(),
            bio: None,
            avatar_url: None,
            website: None,
        };
        
        @UserProfile::create(profile).await?;
        
        // Assign default role
        let default_role = @Role::where_eq("name", "user").first().await?;
        if let Some(role) = default_role {
            self.attach_role(role.id.unwrap()).await?;
        }
        
        Ok(())
    }
    
    // Event when user is deleted
    async fn on_deleted(&self) -> Result<()> {
        // Clean up related data
        @Post::where_eq("user_id", self.id.unwrap()).delete().await?;
        @UserProfile::where_eq("user_id", self.id.unwrap()).delete().await?;
        
        // Detach all roles
        let roles = self.roles().await?;
        for role in roles {
            self.detach_role(role.id.unwrap()).await?;
        }
        
        Ok(())
    }
}

// Relationship event listeners
async fn register_relationship_events() -> Result<()> {
    @User::listen(RelationshipEvent::Created, |user| async {
        // Send welcome email
        @send_welcome_email(&user.email).await?;
        
        // Create default profile
        let profile = UserProfile {
            id: None,
            user_id: user.id.unwrap(),
            bio: None,
            avatar_url: None,
            website: None,
        };
        
        @UserProfile::create(profile).await?;
        
        Ok(())
    }).await;
    
    @User::listen(RelationshipEvent::Deleted, |user| async {
        // Archive user data
        @archive_user_data(user.id.unwrap()).await?;
        
        // Notify administrators
        @notify_admin_user_deleted(user.id.unwrap()).await?;
        
        Ok(())
    }).await;
    
    Ok(())
}
```

## Relationship Serialization and API Responses

```rust
use serde::{Serialize, Deserialize};
use tusk_db::{RelationshipSerializer, ApiResource};

// API resources with relationships
#[derive(Debug, Serialize)]
struct UserResource {
    id: i32,
    name: String,
    email: String,
    is_active: bool,
    created_at: DateTime<Utc>,
    posts_count: Option<i64>,
    roles: Vec<RoleResource>,
    profile: Option<UserProfileResource>,
}

#[derive(Debug, Serialize)]
struct UserDetailResource {
    id: i32,
    name: String,
    email: String,
    is_active: bool,
    created_at: DateTime<Utc>,
    updated_at: DateTime<Utc>,
    profile: Option<UserProfileResource>,
    posts: Vec<PostResource>,
    roles: Vec<RoleResource>,
    comments_count: Option<i64>,
}

impl User {
    // Convert to API resource with relationships
    async fn to_resource(&self) -> Result<UserResource> {
        let roles = self.roles().await?;
        let profile = self.profile().await?;
        let posts_count = self.posts_count().await?;
        
        Ok(UserResource {
            id: self.id.unwrap(),
            name: self.name.clone(),
            email: self.email.clone(),
            is_active: self.is_active,
            created_at: self.created_at.unwrap(),
            posts_count: Some(posts_count),
            roles: roles.into_iter().map(|r| r.to_resource()).collect(),
            profile: profile.map(|p| p.to_resource()),
        })
    }
    
    // Convert to detailed resource
    async fn to_detail_resource(&self) -> Result<UserDetailResource> {
        let profile = self.profile().await?;
        let posts = self.posts().await?;
        let roles = self.roles().await?;
        let comments_count = self.comments().await?.len() as i64;
        
        Ok(UserDetailResource {
            id: self.id.unwrap(),
            name: self.name.clone(),
            email: self.email.clone(),
            is_active: self.is_active,
            created_at: self.created_at.unwrap(),
            updated_at: self.updated_at.unwrap(),
            profile: profile.map(|p| p.to_resource()),
            posts: posts.into_iter().map(|p| p.to_resource()).collect(),
            roles: roles.into_iter().map(|r| r.to_resource()).collect(),
            comments_count: Some(comments_count),
        })
    }
}

// Collection resource with relationships
async fn get_users_with_relationships_resource() -> Result<ApiResource<UserResource>> {
    let users = @User::query()
        .with(&["roles", "profile"])
        .where_eq("is_active", true)
        .get()
        .await?;
    
    let resources: Vec<UserResource> = futures::future::join_all(
        users.into_iter().map(|u| u.to_resource())
    ).await
    .into_iter()
    .collect::<Result<Vec<_>>>()?;
    
    Ok(ApiResource {
        data: resources,
        meta: None,
        links: None,
    })
}
```

## Testing Relationships

```rust
use tusk_db::test_utils::{TestDatabase, TestRelationship};

// Test relationships with test database
#[tokio::test]
async fn test_user_relationships() -> Result<()> {
    let test_db = @TestDatabase::new().await?;
    let mut tx = test_db.begin_transaction().await?;
    
    // Create test user
    let user = @User::create(User {
        id: None,
        name: "Test User".to_string(),
        email: "test@example.com".to_string(),
        is_active: true,
        created_at: None,
        updated_at: None,
    }).await?;
    
    // Test one-to-many relationship
    let post = @Post::create(Post {
        id: None,
        title: "Test Post".to_string(),
        content: "Test content".to_string(),
        user_id: user.id.unwrap(),
        published: true,
        created_at: None,
        updated_at: None,
    }).await?;
    
    let user_posts = user.posts().await?;
    assert_eq!(user_posts.len(), 1);
    assert_eq!(user_posts[0].id, post.id);
    
    // Test belongs-to relationship
    let post_user = post.user().await?;
    assert_eq!(post_user.id, user.id);
    
    // Test many-to-many relationship
    let role = @Role::create(Role {
        id: None,
        name: "admin".to_string(),
        description: Some("Administrator".to_string()),
    }).await?;
    
    user.attach_role(role.id.unwrap()).await?;
    
    let user_roles = user.roles().await?;
    assert_eq!(user_roles.len(), 1);
    assert_eq!(user_roles[0].id, role.id);
    
    // Test relationship constraints
    let has_admin_role = user.has_role("admin").await?;
    assert!(has_admin_role);
    
    tx.rollback().await?;
    Ok(())
}
```

## Best Practices for Rust Relationships

1. **Use Strong Types**: Leverage Rust's type system for relationship safety
2. **Async/Await**: Use non-blocking operations for relationship loading
3. **Eager Loading**: Load relationships efficiently to avoid N+1 queries
4. **Caching**: Cache frequently accessed relationships
5. **Validation**: Validate relationship integrity
6. **Events**: Use relationship events for business logic
7. **Constraints**: Apply appropriate relationship constraints
8. **Performance**: Monitor relationship query performance
9. **Testing**: Test all relationship scenarios
10. **Documentation**: Document complex relationship logic

## Related Topics

- `database-overview-rust` - Database integration overview
- `query-builder-rust` - Fluent query interface
- `orm-models-rust` - Model definition and usage
- `migrations-rust` - Database schema versioning
- `database-transactions-rust` - Transaction handling

---

**Ready to build type-safe, performant model relationships with Rust and TuskLang?** 