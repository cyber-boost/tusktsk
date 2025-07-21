# Model Factories in TuskLang for Rust

TuskLang's Rust model factory system provides type-safe, efficient data generation with compile-time guarantees, async/await support, and comprehensive factory patterns for testing and development.

## 🚀 **Why Rust Model Factories?**

Rust's type system and ownership model make it the perfect language for model factories:

- **Type Safety**: Compile-time validation of factory-generated data
- **Performance**: Zero-cost abstractions with native speed
- **Async/Await**: Non-blocking factory operations
- **Memory Safety**: Automatic memory management for generated objects
- **Factory Patterns**: Comprehensive factory patterns for complex data generation

## Basic Factory Structure

```rust
use tusk_db::{ModelFactory, FactoryBuilder, Result};
use serde::{Deserialize, Serialize};
use chrono::{DateTime, Utc};
use fake::{Fake, Faker};

#[derive(Debug, Serialize, Deserialize, Clone)]
struct User {
    pub id: Option<i32>,
    pub name: String,
    pub email: String,
    pub password_hash: String,
    pub is_active: bool,
    pub email_verified_at: Option<DateTime<Utc>>,
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
    pub view_count: i32,
    pub created_at: Option<DateTime<Utc>>,
    pub updated_at: Option<DateTime<Utc>>,
}

// Basic factory with Rust traits
struct UserFactory;

impl ModelFactory<User> for UserFactory {
    fn definition() -> FactoryBuilder<User> {
        FactoryBuilder::new()
            .field("name", |faker| faker.name().name())
            .field("email", |faker| faker.internet().email())
            .field("password_hash", |_| hash_password("password"))
            .field("is_active", |_| true)
            .field("email_verified_at", |_| Some(Utc::now()))
            .field("created_at", |_| Some(Utc::now()))
            .field("updated_at", |_| Some(Utc::now()))
    }
    
    fn states() -> &'static [(&'static str, Box<dyn Fn(&mut User) + Send + Sync>)] {
        &[
            ("inactive", Box::new(|user| user.is_active = false)),
            ("unverified", Box::new(|user| user.email_verified_at = None)),
            ("admin", Box::new(|user| {
                user.name = "Admin User".to_string();
                user.email = "admin@example.com".to_string();
            })),
            ("premium", Box::new(|user| {
                user.name = format!("Premium {}", user.name);
            })),
        ]
    }
    
    fn after_making() -> &'static [Box<dyn Fn(&mut User) + Send + Sync>] {
        &[
            Box::new(|user| {
                // Ensure email is lowercase
                user.email = user.email.to_lowercase();
            }),
            Box::new(|user| {
                // Add timestamp if not set
                if user.created_at.is_none() {
                    user.created_at = Some(Utc::now());
                }
            }),
        ]
    }
}
```

## Advanced Factory Features

```rust
use tusk_db::{FactoryState, FactoryCallback};

// Advanced factory with complex states
struct PostFactory;

impl ModelFactory<Post> for PostFactory {
    fn definition() -> FactoryBuilder<Post> {
        FactoryBuilder::new()
            .field("title", |faker| faker.lorem().sentence())
            .field("content", |faker| faker.lorem().paragraph())
            .field("user_id", |_| 1) // Will be overridden
            .field("published", |faker| faker.bool())
            .field("view_count", |faker| faker.number_between(0..1000))
            .field("created_at", |_| Some(Utc::now()))
            .field("updated_at", |_| Some(Utc::now()))
    }
    
    fn states() -> &'static [(&'static str, Box<dyn Fn(&mut Post) + Send + Sync>)] {
        &[
            ("published", Box::new(|post| post.published = true)),
            ("draft", Box::new(|post| post.published = false)),
            ("featured", Box::new(|post| {
                post.published = true;
                post.title = format!("Featured: {}", post.title);
                post.view_count = 10000;
            })),
            ("popular", Box::new(|post| {
                post.published = true;
                post.view_count = 50000;
            })),
            ("recent", Box::new(|post| {
                post.created_at = Some(Utc::now() - chrono::Duration::days(1));
            })),
        ]
    }
    
    fn after_making() -> &'static [Box<dyn Fn(&mut Post) + Send + Sync>] {
        &[
            Box::new(|post| {
                // Ensure title is not empty
                if post.title.is_empty() {
                    post.title = "Untitled Post".to_string();
                }
            }),
            Box::new(|post| {
                // Set updated_at to created_at if not set
                if post.updated_at.is_none() {
                    post.updated_at = post.created_at;
                }
            }),
        ]
    }
    
    fn after_creating() -> &'static [Box<dyn Fn(&Post) + Send + Sync>] {
        &[
            Box::new(|post| {
                @log::info!("Created post", { 
                    id: post.id, 
                    title: &post.title,
                    user_id: post.user_id 
                });
            }),
        ]
    }
}
```

## Factory Relationships and Associations

```rust
use tusk_db::{FactoryRelationship, AssociatedFactory};

// Factory with relationships
struct UserWithPostsFactory;

impl ModelFactory<User> for UserWithPostsFactory {
    fn definition() -> FactoryBuilder<User> {
        FactoryBuilder::new()
            .field("name", |faker| faker.name().name())
            .field("email", |faker| faker.internet().email())
            .field("password_hash", |_| hash_password("password"))
            .field("is_active", |_| true)
            .field("email_verified_at", |_| Some(Utc::now()))
            .field("created_at", |_| Some(Utc::now()))
            .field("updated_at", |_| Some(Utc::now()))
    }
    
    fn associations() -> &'static [(&'static str, AssociatedFactory)] {
        &[
            ("posts", AssociatedFactory::new::<PostFactory>()
                .count(3)
                .state("published")
                .callback(|post, user| {
                    post.user_id = user.id.unwrap();
                })
            ),
            ("draft_posts", AssociatedFactory::new::<PostFactory>()
                .count(2)
                .state("draft")
                .callback(|post, user| {
                    post.user_id = user.id.unwrap();
                })
            ),
        ]
    }
}

// Factory for creating related models
struct PostWithUserFactory;

impl ModelFactory<Post> for PostWithUserFactory {
    fn definition() -> FactoryBuilder<Post> {
        FactoryBuilder::new()
            .field("title", |faker| faker.lorem().sentence())
            .field("content", |faker| faker.lorem().paragraph())
            .field("published", |faker| faker.bool())
            .field("view_count", |faker| faker.number_between(0..1000))
            .field("created_at", |_| Some(Utc::now()))
            .field("updated_at", |_| Some(Utc::now()))
    }
    
    fn associations() -> &'static [(&'static str, AssociatedFactory)] {
        &[
            ("user", AssociatedFactory::new::<UserFactory>()
                .state("active")
                .callback(|user, post| {
                    post.user_id = user.id.unwrap();
                })
            ),
        ]
    }
}
```

## Factory Sequences and Callbacks

```rust
use tusk_db::{FactorySequence, FactoryCallback};

// Factory with sequences
struct UserSequenceFactory;

impl ModelFactory<User> for UserSequenceFactory {
    fn definition() -> FactoryBuilder<User> {
        FactoryBuilder::new()
            .field("name", |faker| faker.name().name())
            .field("email", |faker| faker.internet().email())
            .field("password_hash", |_| hash_password("password"))
            .field("is_active", |_| true)
            .field("email_verified_at", |_| Some(Utc::now()))
            .field("created_at", |_| Some(Utc::now()))
            .field("updated_at", |_| Some(Utc::now()))
    }
    
    fn sequences() -> &'static [(&'static str, FactorySequence)] {
        &[
            ("email", FactorySequence::new(|n| format!("user{}@example.com", n))),
            ("name", FactorySequence::new(|n| format!("User {}", n))),
            ("username", FactorySequence::new(|n| format!("user{}", n))),
        ]
    }
    
    fn callbacks() -> &'static [(&'static str, FactoryCallback<User>)] {
        &[
            ("after_making", Box::new(|user| {
                user.email = user.email.to_lowercase();
            })),
            ("before_saving", Box::new(|user| {
                if user.password_hash.is_empty() {
                    user.password_hash = hash_password("default_password");
                }
            })),
            ("after_creating", Box::new(|user| {
                @log::info!("Created user with sequence", { 
                    id: user.id, 
                    email: &user.email 
                });
            })),
        ]
    }
}
```

## Factory States and Traits

```rust
use tusk_db::{FactoryState, FactoryTrait};

// Factory with complex states
struct AdvancedUserFactory;

impl ModelFactory<User> for AdvancedUserFactory {
    fn definition() -> FactoryBuilder<User> {
        FactoryBuilder::new()
            .field("name", |faker| faker.name().name())
            .field("email", |faker| faker.internet().email())
            .field("password_hash", |_| hash_password("password"))
            .field("is_active", |_| true)
            .field("email_verified_at", |_| Some(Utc::now()))
            .field("created_at", |_| Some(Utc::now()))
            .field("updated_at", |_| Some(Utc::now()))
    }
    
    fn states() -> &'static [(&'static str, Box<dyn Fn(&mut User) + Send + Sync>)] {
        &[
            ("inactive", Box::new(|user| user.is_active = false)),
            ("unverified", Box::new(|user| user.email_verified_at = None)),
            ("admin", Box::new(|user| {
                user.name = "Admin User".to_string();
                user.email = "admin@example.com".to_string();
                user.is_active = true;
            })),
            ("premium", Box::new(|user| {
                user.name = format!("Premium {}", user.name);
            })),
            ("vip", Box::new(|user| {
                user.name = format!("VIP {}", user.name);
                user.email = format!("vip.{}", user.email);
            })),
            ("new_user", Box::new(|user| {
                user.created_at = Some(Utc::now() - chrono::Duration::days(1));
                user.email_verified_at = None;
            })),
            ("old_user", Box::new(|user| {
                user.created_at = Some(Utc::now() - chrono::Duration::days(365));
                user.email_verified_at = Some(Utc::now() - chrono::Duration::days(364));
            })),
        ]
    }
    
    fn traits() -> &'static [(&'static str, FactoryTrait<User>)] {
        &[
            ("verified", FactoryTrait::new(|user| {
                user.email_verified_at = Some(Utc::now());
            })),
            ("active", FactoryTrait::new(|user| {
                user.is_active = true;
            })),
            ("inactive", FactoryTrait::new(|user| {
                user.is_active = false;
            })),
        ]
    }
}
```

## Factory Usage Patterns

```rust
use tusk_db::{FactoryUsage, FactoryBuilder};

// Using factories in different ways
async fn factory_usage_examples() -> Result<()> {
    // Basic factory usage
    let user = @UserFactory::new().create().await?;
    assert!(user.id.is_some());
    assert!(!user.name.is_empty());
    
    // Factory with state
    let admin_user = @UserFactory::new()
        .state("admin")
        .create()
        .await?;
    assert_eq!(admin_user.email, "admin@example.com");
    
    // Factory with multiple states
    let premium_admin = @UserFactory::new()
        .state("admin")
        .state("premium")
        .create()
        .await?;
    assert!(premium_admin.name.contains("Premium"));
    
    // Factory with custom fields
    let custom_user = @UserFactory::new()
        .field("name", "Custom User")
        .field("email", "custom@example.com")
        .create()
        .await?;
    assert_eq!(custom_user.name, "Custom User");
    
    // Factory with traits
    let verified_user = @UserFactory::new()
        .trait("verified")
        .create()
        .await?;
    assert!(verified_user.email_verified_at.is_some());
    
    // Factory with associations
    let user_with_posts = @UserWithPostsFactory::new()
        .state("active")
        .create()
        .await?;
    
    let posts = user_with_posts.posts().await?;
    assert_eq!(posts.len(), 5); // 3 published + 2 draft
    
    // Factory with sequences
    let user1 = @UserSequenceFactory::new().create().await?;
    let user2 = @UserSequenceFactory::new().create().await?;
    
    assert_eq!(user1.email, "user1@example.com");
    assert_eq!(user2.email, "user2@example.com");
    
    Ok(())
}

// Batch factory operations
async fn batch_factory_operations() -> Result<()> {
    // Create multiple users
    let users = @UserFactory::new()
        .count(10)
        .create()
        .await?;
    assert_eq!(users.len(), 10);
    
    // Create users with different states
    let active_users = @UserFactory::new()
        .state("active")
        .count(5)
        .create()
        .await?;
    
    let inactive_users = @UserFactory::new()
        .state("inactive")
        .count(3)
        .create()
        .await?;
    
    assert_eq!(active_users.len(), 5);
    assert_eq!(inactive_users.len(), 3);
    
    // Create users with associations
    let users_with_posts = @UserWithPostsFactory::new()
        .count(3)
        .create()
        .await?;
    
    for user in &users_with_posts {
        let posts = user.posts().await?;
        assert_eq!(posts.len(), 5);
    }
    
    Ok(())
}
```

## Factory Testing and Validation

```rust
use tusk_db::{FactoryTest, FactoryValidator};

// Factory testing
#[tokio::test]
async fn test_user_factory() -> Result<()> {
    let test_db = @TestDatabase::new().await?;
    let mut tx = test_db.begin_transaction().await?;
    
    // Test basic factory
    let user = @UserFactory::new().create().await?;
    assert!(user.id.is_some());
    assert!(!user.name.is_empty());
    assert!(!user.email.is_empty());
    assert!(user.is_active);
    
    // Test factory with state
    let admin = @UserFactory::new()
        .state("admin")
        .create()
        .await?;
    assert_eq!(admin.email, "admin@example.com");
    assert_eq!(admin.name, "Admin User");
    
    // Test factory with custom fields
    let custom = @UserFactory::new()
        .field("name", "Test User")
        .field("email", "test@example.com")
        .create()
        .await?;
    assert_eq!(custom.name, "Test User");
    assert_eq!(custom.email, "test@example.com");
    
    // Test factory validation
    let validator = @FactoryValidator::new();
    let validation_result = validator.validate(&user).await?;
    assert!(validation_result.is_valid());
    
    tx.rollback().await?;
    Ok(())
}

// Factory validation
struct UserFactoryValidator;

impl FactoryValidator<User> for UserFactoryValidator {
    async fn validate(&self, user: &User) -> Result<ValidationResult> {
        let mut errors = Vec::new();
        
        // Validate required fields
        if user.name.is_empty() {
            errors.push("Name cannot be empty".to_string());
        }
        
        if user.email.is_empty() {
            errors.push("Email cannot be empty".to_string());
        }
        
        if !user.email.contains('@') {
            errors.push("Email must contain @".to_string());
        }
        
        if user.password_hash.is_empty() {
            errors.push("Password hash cannot be empty".to_string());
        }
        
        // Validate business rules
        if user.is_active && user.email_verified_at.is_none() {
            errors.push("Active users must have verified email".to_string());
        }
        
        if errors.is_empty() {
            Ok(ValidationResult::valid())
        } else {
            Ok(ValidationResult::invalid(errors))
        }
    }
}
```

## Factory Performance and Optimization

```rust
use tusk_db::{FactoryPerformance, FactoryOptimizer};

// Performance-optimized factory
struct OptimizedUserFactory;

impl ModelFactory<User> for OptimizedUserFactory {
    fn definition() -> FactoryBuilder<User> {
        FactoryBuilder::new()
            .field("name", |faker| faker.name().name())
            .field("email", |faker| faker.internet().email())
            .field("password_hash", |_| hash_password("password"))
            .field("is_active", |_| true)
            .field("email_verified_at", |_| Some(Utc::now()))
            .field("created_at", |_| Some(Utc::now()))
            .field("updated_at", |_| Some(Utc::now()))
    }
    
    fn performance_config() -> FactoryPerformance {
        FactoryPerformance {
            batch_size: 1000,
            use_transactions: true,
            parallel_creation: true,
            cache_generated_data: true,
        }
    }
}

// Factory with caching
async fn cached_factory_usage() -> Result<()> {
    let factory = @UserFactory::new()
        .with_caching(true)
        .cache_ttl(3600); // 1 hour
    
    // First creation - generates data
    let user1 = factory.create().await?;
    
    // Second creation - uses cached data
    let user2 = factory.create().await?;
    
    // Both users should have similar data due to caching
    assert_eq!(user1.name, user2.name);
    assert_eq!(user1.email, user2.email);
    
    Ok(())
}

// Parallel factory creation
async fn parallel_factory_creation() -> Result<()> {
    let factory = @UserFactory::new();
    
    let creation_tasks: Vec<_> = (0..10)
        .map(|_| {
            let factory = factory.clone();
            tokio::spawn(async move {
                factory.create().await
            })
        })
        .collect();
    
    let results = futures::future::join_all(creation_tasks).await;
    
    for result in results {
        let user = result??;
        assert!(user.id.is_some());
    }
    
    Ok(())
}
```

## Factory Customization and Extension

```rust
use tusk_db::{FactoryExtension, CustomFactory};

// Custom factory extension
struct CustomUserFactory;

impl CustomFactory<User> for CustomUserFactory {
    fn base_factory() -> Box<dyn ModelFactory<User>> {
        Box::new(UserFactory)
    }
    
    fn custom_fields() -> &'static [(&'static str, Box<dyn Fn() -> String + Send + Sync>)] {
        &[
            ("custom_field", Box::new(|| "custom_value".to_string())),
            ("generated_id", Box::new(|| uuid::Uuid::new_v4().to_string())),
        ]
    }
    
    fn custom_states() -> &'static [(&'static str, Box<dyn Fn(&mut User) + Send + Sync>)] {
        &[
            ("custom_state", Box::new(|user| {
                user.name = format!("Custom {}", user.name);
            })),
        ]
    }
}

// Factory with custom callbacks
struct CallbackUserFactory;

impl ModelFactory<User> for CallbackUserFactory {
    fn definition() -> FactoryBuilder<User> {
        FactoryBuilder::new()
            .field("name", |faker| faker.name().name())
            .field("email", |faker| faker.internet().email())
            .field("password_hash", |_| hash_password("password"))
            .field("is_active", |_| true)
            .field("email_verified_at", |_| Some(Utc::now()))
            .field("created_at", |_| Some(Utc::now()))
            .field("updated_at", |_| Some(Utc::now()))
    }
    
    fn callbacks() -> &'static [(&'static str, FactoryCallback<User>)] {
        &[
            ("before_making", Box::new(|user| {
                @log::debug!("About to create user", { email: &user.email });
            })),
            ("after_making", Box::new(|user| {
                user.email = user.email.to_lowercase();
            })),
            ("before_saving", Box::new(|user| {
                if user.password_hash.is_empty() {
                    user.password_hash = hash_password("default");
                }
            })),
            ("after_creating", Box::new(|user| {
                @log::info!("Created user", { 
                    id: user.id, 
                    email: &user.email 
                });
                
                // Send welcome email
                @send_welcome_email(&user.email).await?;
            })),
        ]
    }
}
```

## Factory Best Practices

```rust
// Best practices for factory usage
async fn factory_best_practices() -> Result<()> {
    // 1. Use meaningful state names
    let admin_user = @UserFactory::new()
        .state("admin")
        .create()
        .await?;
    
    // 2. Combine states for complex scenarios
    let premium_verified_admin = @UserFactory::new()
        .state("admin")
        .state("premium")
        .trait("verified")
        .create()
        .await?;
    
    // 3. Use sequences for unique data
    let user1 = @UserSequenceFactory::new().create().await?;
    let user2 = @UserSequenceFactory::new().create().await?;
    assert_ne!(user1.email, user2.email);
    
    // 4. Use associations for related data
    let user_with_posts = @UserWithPostsFactory::new()
        .state("active")
        .create()
        .await?;
    
    // 5. Validate factory-generated data
    let validator = @UserFactoryValidator::new();
    let validation_result = validator.validate(&user_with_posts).await?;
    assert!(validation_result.is_valid());
    
    // 6. Use batch operations for performance
    let users = @UserFactory::new()
        .count(100)
        .create()
        .await?;
    
    // 7. Use caching for repeated data
    let cached_factory = @UserFactory::new()
        .with_caching(true)
        .cache_ttl(1800); // 30 minutes
    
    Ok(())
}
```

## Best Practices for Rust Model Factories

1. **Use Strong Types**: Leverage Rust's type system for factory safety
2. **Meaningful States**: Use descriptive state names
3. **Associations**: Use associations for related data
4. **Sequences**: Use sequences for unique data
5. **Validation**: Validate factory-generated data
6. **Performance**: Use batch operations and caching
7. **Testing**: Test all factory scenarios
8. **Documentation**: Document complex factory logic
9. **Callbacks**: Use callbacks for side effects
10. **Reusability**: Create reusable factory components

## Related Topics

- `database-overview-rust` - Database integration overview
- `query-builder-rust` - Fluent query interface
- `orm-models-rust` - Model definition and usage
- `migrations-rust` - Database schema versioning
- `relationships-rust` - Model relationships

---

**Ready to build type-safe, efficient model factories with Rust and TuskLang?** 