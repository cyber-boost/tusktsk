# Database Seeding in TuskLang for Rust

TuskLang's Rust seeding system provides type-safe, efficient database seeding with compile-time guarantees, async/await support, and comprehensive data generation capabilities for development and testing environments.

## 🚀 **Why Rust Database Seeding?**

Rust's type system and ownership model make it the perfect language for database seeding:

- **Type Safety**: Compile-time validation of seed data structures
- **Performance**: Zero-cost abstractions with native speed
- **Async/Await**: Non-blocking seed operations
- **Memory Safety**: Automatic memory management for large datasets
- **Data Integrity**: Guaranteed referential integrity across seed data

## Basic Seeding Structure

```rust
use tusk_db::{Seeder, SeederBuilder, Result};
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

// Basic seeder with Rust traits
#[derive(Debug)]
struct UserSeeder;

#[async_trait]
impl Seeder for UserSeeder {
    fn name() -> &'static str {
        "user_seeder"
    }
    
    fn description() -> &'static str {
        "Seed users table with test data"
    }
    
    async fn run(&self, db: &Database) -> Result<()> {
        let users = vec![
            User {
                id: None,
                name: "Admin User".to_string(),
                email: "admin@example.com".to_string(),
                password_hash: hash_password("admin123"),
                is_active: true,
                created_at: None,
                updated_at: None,
            },
            User {
                id: None,
                name: "John Doe".to_string(),
                email: "john@example.com".to_string(),
                password_hash: hash_password("password123"),
                is_active: true,
                created_at: None,
                updated_at: None,
            },
            User {
                id: None,
                name: "Jane Smith".to_string(),
                email: "jane@example.com".to_string(),
                password_hash: hash_password("password123"),
                is_active: true,
                created_at: None,
                updated_at: None,
            },
        ];
        
        for user in users {
            @User::create(user).await?;
        }
        
        @log::info!("Seeded {} users", users.len());
        Ok(())
    }
    
    async fn rollback(&self, db: &Database) -> Result<()> {
        @db.delete("DELETE FROM users WHERE email IN (?, ?, ?)", &[
            "admin@example.com",
            "john@example.com", 
            "jane@example.com"
        ]).await?;
        
        @log::info!("Rolled back user seeder");
        Ok(())
    }
}
```

## Advanced Seeding with Factories

```rust
use tusk_db::{ModelFactory, FactoryBuilder, Faker};

// Define factory for User model
struct UserFactory;

impl ModelFactory<User> for UserFactory {
    fn definition() -> FactoryBuilder<User> {
        FactoryBuilder::new()
            .field("name", |faker| faker.name().name())
            .field("email", |faker| faker.internet().email())
            .field("password_hash", |_| hash_password("password"))
            .field("is_active", |_| true)
            .field("created_at", |_| Some(Utc::now()))
            .field("updated_at", |_| Some(Utc::now()))
    }
    
    fn states() -> &'static [(&'static str, Box<dyn Fn(&mut User) + Send + Sync>)] {
        &[
            ("inactive", Box::new(|user| user.is_active = false)),
            ("admin", Box::new(|user| {
                user.name = "Admin User".to_string();
                user.email = "admin@example.com".to_string();
            })),
            ("verified", Box::new(|user| {
                user.email_verified_at = Some(Utc::now());
            })),
        ]
    }
}

// Advanced seeder using factories
#[derive(Debug)]
struct AdvancedUserSeeder;

#[async_trait]
impl Seeder for AdvancedUserSeeder {
    fn name() -> &'static str {
        "advanced_user_seeder"
    }
    
    fn description() -> &'static str {
        "Seed users table with factory-generated data"
    }
    
    async fn run(&self, db: &Database) -> Result<()> {
        // Create admin user
        let admin = @UserFactory::new()
            .state("admin")
            .create()
            .await?;
        
        // Create regular users
        let regular_users = @UserFactory::new()
            .count(50)
            .create()
            .await?;
        
        // Create inactive users
        let inactive_users = @UserFactory::new()
            .state("inactive")
            .count(10)
            .create()
            .await?;
        
        // Create verified users
        let verified_users = @UserFactory::new()
            .state("verified")
            .count(25)
            .create()
            .await?;
        
        @log::info!("Seeded {} users total", 1 + regular_users.len() + inactive_users.len() + verified_users.len());
        Ok(())
    }
    
    async fn rollback(&self, db: &Database) -> Result<()> {
        @db.delete("DELETE FROM users WHERE email != ?", &["admin@example.com"]).await?;
        @log::info!("Rolled back advanced user seeder");
        Ok(())
    }
}
```

## Relationship Seeding

```rust
use tusk_db::{RelationshipSeeder, SeederDependency};

// Seeder with relationships
#[derive(Debug)]
struct PostSeeder;

#[async_trait]
impl Seeder for PostSeeder {
    fn name() -> &'static str {
        "post_seeder"
    }
    
    fn description() -> &'static str {
        "Seed posts table with user relationships"
    }
    
    fn dependencies() -> &'static [&'static str] {
        &["user_seeder"]
    }
    
    async fn run(&self, db: &Database) -> Result<()> {
        // Get existing users
        let users = @User::all().await?;
        
        if users.is_empty() {
            return Err("No users found. Run user_seeder first.".into());
        }
        
        // Create posts for each user
        for user in &users {
            let post_count = (1..=5).fake::<usize>();
            
            for _ in 0..post_count {
                let post = Post {
                    id: None,
                    title: Faker.fake::<String>(),
                    content: Faker.fake::<String>(),
                    user_id: user.id.unwrap(),
                    published: Faker.fake::<bool>(),
                    created_at: None,
                    updated_at: None,
                };
                
                @Post::create(post).await?;
            }
        }
        
        @log::info!("Seeded posts for {} users", users.len());
        Ok(())
    }
    
    async fn rollback(&self, db: &Database) -> Result<()> {
        @db.delete("DELETE FROM posts").await?;
        @log::info!("Rolled back post seeder");
        Ok(())
    }
}

// Factory for Post with relationships
struct PostFactory;

impl ModelFactory<Post> for PostFactory {
    fn definition() -> FactoryBuilder<Post> {
        FactoryBuilder::new()
            .field("title", |faker| faker.lorem().sentence())
            .field("content", |faker| faker.lorem().paragraph())
            .field("published", |faker| faker.bool())
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
            })),
        ]
    }
}

// Relationship seeder using factories
#[derive(Debug)]
struct RelationshipSeeder;

#[async_trait]
impl Seeder for RelationshipSeeder {
    fn name() -> &'static str {
        "relationship_seeder"
    }
    
    fn description() -> &'static str {
        "Seed posts with user relationships using factories"
    }
    
    fn dependencies() -> &'static [&'static str] {
        &["advanced_user_seeder"]
    }
    
    async fn run(&self, db: &Database) -> Result<()> {
        let users = @User::all().await?;
        
        for user in &users {
            // Create published posts
            let published_posts = @PostFactory::new()
                .state("published")
                .field("user_id", user.id.unwrap())
                .count(3)
                .create()
                .await?;
            
            // Create draft posts
            let draft_posts = @PostFactory::new()
                .state("draft")
                .field("user_id", user.id.unwrap())
                .count(2)
                .create()
                .await?;
            
            // Create featured posts for some users
            if user.is_active {
                let featured_posts = @PostFactory::new()
                    .state("featured")
                    .field("user_id", user.id.unwrap())
                    .count(1)
                    .create()
                    .await?;
            }
        }
        
        @log::info!("Seeded posts with relationships");
        Ok(())
    }
    
    async fn rollback(&self, db: &Database) -> Result<()> {
        @db.delete("DELETE FROM posts").await?;
        @log::info!("Rolled back relationship seeder");
        Ok(())
    }
}
```

## Conditional and Environment-Based Seeding

```rust
use tusk_db::{EnvironmentSeeder, ConditionalSeeder};

// Environment-specific seeder
#[derive(Debug)]
struct EnvironmentUserSeeder;

#[async_trait]
impl EnvironmentSeeder for EnvironmentUserSeeder {
    fn name() -> &'static str {
        "environment_user_seeder"
    }
    
    fn environments() -> &'static [&'static str] {
        &["development", "staging", "testing"]
    }
    
    async fn run(&self, db: &Database, environment: &str) -> Result<()> {
        match environment {
            "development" => {
                // Create development users
                let dev_users = vec![
                    User {
                        id: None,
                        name: "Dev User".to_string(),
                        email: "dev@example.com".to_string(),
                        password_hash: hash_password("dev123"),
                        is_active: true,
                        created_at: None,
                        updated_at: None,
                    },
                    User {
                        id: None,
                        name: "Test User".to_string(),
                        email: "test@example.com".to_string(),
                        password_hash: hash_password("test123"),
                        is_active: true,
                        created_at: None,
                        updated_at: None,
                    },
                ];
                
                for user in dev_users {
                    @User::create(user).await?;
                }
            }
            "staging" => {
                // Create staging users with more realistic data
                let staging_users = @UserFactory::new()
                    .count(100)
                    .create()
                    .await?;
            }
            "testing" => {
                // Create minimal test users
                let test_user = @UserFactory::new()
                    .field("email", "test@example.com")
                    .create()
                    .await?;
            }
            _ => {
                return Err("Unsupported environment".into());
            }
        }
        
        @log::info!("Seeded users for {} environment", environment);
        Ok(())
    }
}

// Conditional seeder based on data existence
#[derive(Debug)]
struct ConditionalSeeder;

#[async_trait]
impl ConditionalSeeder for ConditionalSeeder {
    fn name() -> &'static str {
        "conditional_seeder"
    }
    
    async fn should_run(&self, db: &Database) -> Result<bool> {
        // Only run if no users exist
        let user_count = @db.query_one::<i64>("SELECT COUNT(*) FROM users").await?;
        Ok(user_count == 0)
    }
    
    async fn run(&self, db: &Database) -> Result<()> {
        if !self.should_run(db).await? {
            @log::info!("Skipping conditional seeder - data already exists");
            return Ok(());
        }
        
        // Create initial data
        let users = @UserFactory::new()
            .count(10)
            .create()
            .await?;
        
        @log::info!("Created {} users via conditional seeder", users.len());
        Ok(())
    }
}
```

## Performance-Optimized Seeding

```rust
use tusk_db::{BatchSeeder, ChunkSeeder};

// Batch seeding for performance
#[derive(Debug)]
struct BatchUserSeeder;

#[async_trait]
impl BatchSeeder for BatchUserSeeder {
    fn name() -> &'static str {
        "batch_user_seeder"
    }
    
    fn batch_size() -> usize {
        1000
    }
    
    async fn run(&self, db: &Database) -> Result<()> {
        let total_users = 10000;
        let batch_size = Self::batch_size();
        
        for batch in 0..(total_users / batch_size) {
            let users: Vec<User> = (0..batch_size)
                .map(|_| User {
                    id: None,
                    name: Faker.fake::<String>(),
                    email: Faker.fake::<String>(),
                    password_hash: hash_password("password"),
                    is_active: true,
                    created_at: None,
                    updated_at: None,
                })
                .collect();
            
            // Batch insert for performance
            @db.table("users")
                .insert_batch(&users)
                .await?;
            
            @log::info!("Seeded batch {}/{}", batch + 1, total_users / batch_size);
        }
        
        Ok(())
    }
}

// Chunk processing for large datasets
#[derive(Debug)]
struct ChunkSeeder;

#[async_trait]
impl ChunkSeeder for ChunkSeeder {
    fn name() -> &'static str {
        "chunk_seeder"
    }
    
    fn chunk_size() -> usize {
        500
    }
    
    async fn run(&self, db: &Database) -> Result<()> {
        let users = @User::all().await?;
        
        @db.table("users")
            .chunk(Self::chunk_size(), |chunk| async {
                for user in chunk {
                    // Process each user in chunk
                    @process_user_seed_data(user).await?;
                }
                Ok::<(), Box<dyn std::error::Error>>(())
            })
            .await?;
        
        Ok(())
    }
}

// Parallel seeding for maximum performance
async fn parallel_seeding() -> Result<()> {
    let seeder_tasks = vec![
        tokio::spawn(async {
            let seeder = UserSeeder;
            seeder.run(&@db).await
        }),
        tokio::spawn(async {
            let seeder = PostSeeder;
            seeder.run(&@db).await
        }),
        tokio::spawn(async {
            let seeder = CommentSeeder;
            seeder.run(&@db).await
        }),
    ];
    
    let results = futures::future::join_all(seeder_tasks).await;
    
    for result in results {
        result??;
    }
    
    Ok(())
}
```

## Custom Data Generation

```rust
use tusk_db::{CustomSeeder, DataGenerator};

// Custom data generator
struct CustomUserGenerator;

impl DataGenerator<User> for CustomUserGenerator {
    fn generate(&self) -> User {
        let first_name = Faker.fake::<String>();
        let last_name = Faker.fake::<String>();
        let email = format!("{}.{}@example.com", 
            first_name.to_lowercase(), 
            last_name.to_lowercase()
        );
        
        User {
            id: None,
            name: format!("{} {}", first_name, last_name),
            email,
            password_hash: hash_password("password"),
            is_active: true,
            created_at: Some(Utc::now()),
            updated_at: Some(Utc::now()),
        }
    }
    
    fn generate_many(&self, count: usize) -> Vec<User> {
        (0..count).map(|_| self.generate()).collect()
    }
}

// Custom seeder with specific data
#[derive(Debug)]
struct CustomUserSeeder;

#[async_trait]
impl CustomSeeder for CustomUserSeeder {
    fn name() -> &'static str {
        "custom_user_seeder"
    }
    
    async fn run(&self, db: &Database) -> Result<()> {
        let generator = CustomUserGenerator;
        
        // Generate specific users
        let specific_users = vec![
            User {
                id: None,
                name: "Alice Johnson".to_string(),
                email: "alice.johnson@example.com".to_string(),
                password_hash: hash_password("alice123"),
                is_active: true,
                created_at: None,
                updated_at: None,
            },
            User {
                id: None,
                name: "Bob Wilson".to_string(),
                email: "bob.wilson@example.com".to_string(),
                password_hash: hash_password("bob123"),
                is_active: true,
                created_at: None,
                updated_at: None,
            },
        ];
        
        // Generate random users
        let random_users = generator.generate_many(50);
        
        // Create all users
        for user in specific_users.into_iter().chain(random_users.into_iter()) {
            @User::create(user).await?;
        }
        
        @log::info!("Seeded custom users");
        Ok(())
    }
}
```

## Seeding with External Data

```rust
use tusk_db::{ExternalDataSeeder, DataImporter};

// Seeder that imports external data
#[derive(Debug)]
struct ExternalDataSeeder;

#[async_trait]
impl ExternalDataSeeder for ExternalDataSeeder {
    fn name() -> &'static str {
        "external_data_seeder"
    }
    
    async fn run(&self, db: &Database) -> Result<()> {
        // Import from CSV
        let csv_data = @import_csv("data/users.csv").await?;
        
        for row in csv_data {
            let user = User {
                id: None,
                name: row.get("name")?,
                email: row.get("email")?,
                password_hash: hash_password("imported"),
                is_active: row.get("is_active")?.parse().unwrap_or(true),
                created_at: None,
                updated_at: None,
            };
            
            @User::create(user).await?;
        }
        
        // Import from JSON
        let json_data = @import_json("data/posts.json").await?;
        
        for post_data in json_data {
            let post = Post {
                id: None,
                title: post_data["title"].as_str().unwrap().to_string(),
                content: post_data["content"].as_str().unwrap().to_string(),
                user_id: post_data["user_id"].as_i64().unwrap() as i32,
                published: post_data["published"].as_bool().unwrap_or(false),
                created_at: None,
                updated_at: None,
            };
            
            @Post::create(post).await?;
        }
        
        @log::info!("Imported external data");
        Ok(())
    }
}

// API data seeder
async fn seed_from_api() -> Result<()> {
    let api_url = "https://jsonplaceholder.typicode.com/users";
    let response = @http::get(api_url).await?;
    let users_data: Vec<serde_json::Value> = response.json().await?;
    
    for user_data in users_data {
        let user = User {
            id: None,
            name: user_data["name"].as_str().unwrap().to_string(),
            email: user_data["email"].as_str().unwrap().to_string(),
            password_hash: hash_password("api_imported"),
            is_active: true,
            created_at: None,
            updated_at: None,
        };
        
        @User::create(user).await?;
    }
    
    Ok(())
}
```

## Seeding Management and Orchestration

```rust
use tusk_db::{SeederManager, SeederOrchestrator};

// Seeder manager
async fn run_seeders() -> Result<()> {
    let manager = @SeederManager::new().await?;
    
    // Register seeders
    manager.register(Box::new(UserSeeder)).await?;
    manager.register(Box::new(PostSeeder)).await?;
    manager.register(Box::new(CommentSeeder)).await?;
    
    // Run all seeders
    manager.run_all().await?;
    
    Ok(())
}

// Seeder orchestrator with dependencies
async fn orchestrate_seeding() -> Result<()> {
    let orchestrator = @SeederOrchestrator::new().await?;
    
    // Define seeding order
    orchestrator
        .add_seeder("user_seeder", Box::new(UserSeeder))
        .add_seeder("post_seeder", Box::new(PostSeeder), &["user_seeder"])
        .add_seeder("comment_seeder", Box::new(CommentSeeder), &["post_seeder"])
        .add_seeder("relationship_seeder", Box::new(RelationshipSeeder), &["user_seeder", "post_seeder"])
        .run()
        .await?;
    
    Ok(())
}

// Environment-specific seeding
async fn run_environment_seeding() -> Result<()> {
    let environment = @env("APP_ENV", "development");
    
    let orchestrator = @SeederOrchestrator::new().await?;
    
    match environment.as_str() {
        "development" => {
            orchestrator
                .add_seeder("dev_user_seeder", Box::new(EnvironmentUserSeeder))
                .add_seeder("dev_post_seeder", Box::new(PostSeeder))
                .run()
                .await?;
        }
        "staging" => {
            orchestrator
                .add_seeder("staging_user_seeder", Box::new(EnvironmentUserSeeder))
                .add_seeder("staging_post_seeder", Box::new(PostSeeder))
                .add_seeder("staging_comment_seeder", Box::new(CommentSeeder))
                .run()
                .await?;
        }
        "testing" => {
            orchestrator
                .add_seeder("test_user_seeder", Box::new(EnvironmentUserSeeder))
                .run()
                .await?;
        }
        _ => {
            return Err("Unsupported environment".into());
        }
    }
    
    Ok(())
}
```

## Testing Seeders

```rust
use tusk_db::test_utils::{TestSeeder, TestDatabase};

// Test seeder with test database
#[tokio::test]
async fn test_user_seeder() -> Result<()> {
    let test_db = @TestDatabase::new().await?;
    let mut tx = test_db.begin_transaction().await?;
    
    // Run seeder
    let seeder = UserSeeder;
    seeder.run(&test_db).await?;
    
    // Verify seeded data
    let users = @User::all().await?;
    assert_eq!(users.len(), 3); // Admin, John, Jane
    
    let admin = users.iter().find(|u| u.email == "admin@example.com");
    assert!(admin.is_some());
    assert_eq!(admin.unwrap().name, "Admin User");
    
    // Test rollback
    seeder.rollback(&test_db).await?;
    
    let users_after_rollback = @User::all().await?;
    assert_eq!(users_after_rollback.len(), 0);
    
    tx.rollback().await?;
    Ok(())
}

// Test factory-based seeder
#[tokio::test]
async fn test_factory_seeder() -> Result<()> {
    let test_db = @TestDatabase::new().await?;
    let mut tx = test_db.begin_transaction().await?;
    
    // Run factory seeder
    let seeder = AdvancedUserSeeder;
    seeder.run(&test_db).await?;
    
    // Verify factory-generated data
    let users = @User::all().await?;
    assert!(users.len() > 0);
    
    // Check that admin user exists
    let admin = users.iter().find(|u| u.email == "admin@example.com");
    assert!(admin.is_some());
    
    // Check that some users are inactive
    let inactive_users = users.iter().filter(|u| !u.is_active).count();
    assert!(inactive_users > 0);
    
    tx.rollback().await?;
    Ok(())
}
```

## Best Practices for Rust Seeding

1. **Use Strong Types**: Leverage Rust's type system for seed data safety
2. **Factories**: Use factories for generating realistic test data
3. **Dependencies**: Define clear seeder dependencies
4. **Performance**: Use batch operations for large datasets
5. **Environment**: Create environment-specific seeders
6. **Conditional**: Use conditional seeding to avoid duplicates
7. **Rollback**: Implement proper rollback mechanisms
8. **Testing**: Test all seeder scenarios
9. **Documentation**: Document complex seeding logic
10. **Monitoring**: Monitor seeding performance and errors

## Related Topics

- `database-overview-rust` - Database integration overview
- `query-builder-rust` - Fluent query interface
- `orm-models-rust` - Model definition and usage
- `migrations-rust` - Database schema versioning
- `relationships-rust` - Model relationships

---

**Ready to build type-safe, efficient database seeding with Rust and TuskLang?** 