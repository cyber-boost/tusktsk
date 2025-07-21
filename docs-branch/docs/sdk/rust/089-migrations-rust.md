# Database Migrations in TuskLang for Rust

TuskLang's Rust migration system provides a type-safe, version-controlled approach to database schema management with compile-time guarantees, async/await support, and zero-downtime deployment capabilities.

## 🚀 **Why Rust Migrations?**

Rust's type system and ownership model make it the perfect language for database migrations:

- **Type Safety**: Compile-time validation of schema changes
- **Version Control**: Automatic tracking of migration history
- **Rollback Safety**: Guaranteed rollback capabilities
- **Async/Await**: Non-blocking migration execution
- **Zero-Downtime**: Support for complex deployment strategies

## Basic Migration Structure

```rust
use tusk_db::{Migration, Schema, ColumnType, IndexType, Result};
use async_trait::async_trait;
use chrono::{DateTime, Utc};

// Basic migration with Rust traits
#[derive(Debug)]
struct CreateUsersTable;

#[async_trait]
impl Migration for CreateUsersTable {
    fn name() -> &'static str {
        "create_users_table"
    }
    
    fn version() -> &'static str {
        "2024_01_15_000001"
    }
    
    fn description() -> &'static str {
        "Create the users table with basic authentication fields"
    }
    
    async fn up(schema: &Schema) -> Result<()> {
        schema.create_table("users", |table| {
            table.id("id");
            table.string("name", 255).not_null();
            table.string("email", 255).unique().not_null();
            table.string("password_hash", 255).not_null();
            table.timestamp("email_verified_at").nullable();
            table.boolean("is_active").default(true);
            table.timestamp("created_at").default_current();
            table.timestamp("updated_at").default_current();
            
            // Indexes for performance
            table.index(&["email"], IndexType::BTree);
            table.index(&["created_at"], IndexType::BTree);
            table.index(&["is_active", "created_at"], IndexType::BTree);
        }).await?;
        
        Ok(())
    }
    
    async fn down(schema: &Schema) -> Result<()> {
        schema.drop_table_if_exists("users").await?;
        Ok(())
    }
}
```

## Advanced Migration Features

```rust
use tusk_db::{MigrationBuilder, ForeignKey, Constraint};

// Complex migration with relationships
#[derive(Debug)]
struct CreatePostsTable;

#[async_trait]
impl Migration for CreatePostsTable {
    fn name() -> &'static str {
        "create_posts_table"
    }
    
    fn version() -> &'static str {
        "2024_01_15_000002"
    }
    
    fn description() -> &'static str {
        "Create the posts table with user relationships"
    }
    
    async fn up(schema: &Schema) -> Result<()> {
        schema.create_table("posts", |table| {
            table.id("id");
            table.string("title", 255).not_null();
            table.text("content").not_null();
            table.integer("user_id").unsigned().not_null();
            table.boolean("published").default(false);
            table.integer("view_count").default(0);
            table.decimal("rating", 3, 2).nullable();
            table.json("tags").nullable();
            table.json("metadata").nullable();
            table.timestamp("published_at").nullable();
            table.timestamp("created_at").default_current();
            table.timestamp("updated_at").default_current();
            
            // Foreign key constraints
            table.foreign_key("user_id", "users", "id")
                .on_delete(ForeignKey::Cascade)
                .on_update(ForeignKey::Cascade);
            
            // Indexes
            table.index(&["user_id"], IndexType::BTree);
            table.index(&["published"], IndexType::BTree);
            table.index(&["created_at"], IndexType::BTree);
            table.index(&["user_id", "published"], IndexType::BTree);
            
            // Unique constraints
            table.unique(&["user_id", "title"]);
        }).await?;
        
        Ok(())
    }
    
    async fn down(schema: &Schema) -> Result<()> {
        schema.drop_table_if_exists("posts").await?;
        Ok(())
    }
}
```

## Migration Dependencies and Ordering

```rust
use tusk_db::{MigrationDependency, MigrationOrder};

// Migration with dependencies
#[derive(Debug)]
struct CreateCommentsTable;

#[async_trait]
impl Migration for CreateCommentsTable {
    fn name() -> &'static str {
        "create_comments_table"
    }
    
    fn version() -> &'static str {
        "2024_01_15_000003"
    }
    
    fn description() -> &'static str {
        "Create the comments table with post and user relationships"
    }
    
    // Define dependencies
    fn dependencies() -> &'static [&'static str] {
        &["create_users_table", "create_posts_table"]
    }
    
    async fn up(schema: &Schema) -> Result<()> {
        schema.create_table("comments", |table| {
            table.id("id");
            table.text("content").not_null();
            table.integer("user_id").unsigned().not_null();
            table.integer("post_id").unsigned().not_null();
            table.integer("parent_id").unsigned().nullable();
            table.boolean("is_approved").default(false);
            table.timestamp("approved_at").nullable();
            table.timestamp("created_at").default_current();
            table.timestamp("updated_at").default_current();
            
            // Foreign keys
            table.foreign_key("user_id", "users", "id")
                .on_delete(ForeignKey::Cascade);
            
            table.foreign_key("post_id", "posts", "id")
                .on_delete(ForeignKey::Cascade);
            
            table.foreign_key("parent_id", "comments", "id")
                .on_delete(ForeignKey::Cascade);
            
            // Indexes
            table.index(&["post_id"], IndexType::BTree);
            table.index(&["user_id"], IndexType::BTree);
            table.index(&["parent_id"], IndexType::BTree);
            table.index(&["is_approved"], IndexType::BTree);
            table.index(&["created_at"], IndexType::BTree);
        }).await?;
        
        Ok(())
    }
    
    async fn down(schema: &Schema) -> Result<()> {
        schema.drop_table_if_exists("comments").await?;
        Ok(())
    }
}
```

## Data Migrations and Seeding

```rust
use tusk_db::{DataMigration, Seeder};

// Data migration for seeding initial data
#[derive(Debug)]
struct SeedInitialData;

#[async_trait]
impl DataMigration for SeedInitialData {
    fn name() -> &'static str {
        "seed_initial_data"
    }
    
    fn version() -> &'static str {
        "2024_01_15_000004"
    }
    
    fn description() -> &'static str {
        "Seed initial users and roles"
    }
    
    async fn up(db: &Database) -> Result<()> {
        // Create admin user
        let admin_id = db.insert(
            "INSERT INTO users (name, email, password_hash, is_active, email_verified_at) VALUES (?, ?, ?, ?, ?)",
            &[
                "Admin User",
                "admin@example.com",
                &hash_password("admin123"),
                &true,
                &Utc::now(),
            ]
        ).await?;
        
        // Create default roles
        let admin_role_id = db.insert(
            "INSERT INTO roles (name, description) VALUES (?, ?)",
            &["admin", "Administrator with full access"]
        ).await?;
        
        let user_role_id = db.insert(
            "INSERT INTO roles (name, description) VALUES (?, ?)",
            &["user", "Regular user with limited access"]
        ).await?;
        
        // Assign admin role to admin user
        db.insert(
            "INSERT INTO user_roles (user_id, role_id) VALUES (?, ?)",
            &[&admin_id, &admin_role_id]
        ).await?;
        
        Ok(())
    }
    
    async fn down(db: &Database) -> Result<()> {
        // Remove seeded data
        db.delete("DELETE FROM user_roles WHERE user_id IN (SELECT id FROM users WHERE email = ?)", &["admin@example.com"]).await?;
        db.delete("DELETE FROM users WHERE email = ?", &["admin@example.com"]).await?;
        db.delete("DELETE FROM roles WHERE name IN (?, ?)", &["admin", "user"]).await?;
        
        Ok(())
    }
}
```

## Schema Modifications and Alterations

```rust
use tusk_db::{SchemaModification, ColumnModification};

// Migration for modifying existing tables
#[derive(Debug)]
struct AddUserProfileFields;

#[async_trait]
impl Migration for AddUserProfileFields {
    fn name() -> &'static str {
        "add_user_profile_fields"
    }
    
    fn version() -> &'static str {
        "2024_01_20_000001"
    }
    
    fn description() -> &'static str {
        "Add profile fields to users table"
    }
    
    async fn up(schema: &Schema) -> Result<()> {
        // Add new columns
        schema.alter_table("users", |table| {
            table.add_column("first_name", ColumnType::String(100)).nullable();
            table.add_column("last_name", ColumnType::String(100)).nullable();
            table.add_column("phone", ColumnType::String(20)).nullable();
            table.add_column("date_of_birth", ColumnType::Date).nullable();
            table.add_column("bio", ColumnType::Text).nullable();
            table.add_column("avatar_url", ColumnType::String(500)).nullable();
            table.add_column("website", ColumnType::String(500)).nullable();
            table.add_column("location", ColumnType::String(255)).nullable();
            table.add_column("timezone", ColumnType::String(50)).default("UTC");
            table.add_column("language", ColumnType::String(10)).default("en");
        }).await?;
        
        // Add indexes for new columns
        schema.alter_table("users", |table| {
            table.add_index(&["first_name", "last_name"], IndexType::BTree);
            table.add_index(&["phone"], IndexType::BTree);
            table.add_index(&["location"], IndexType::BTree);
        }).await?;
        
        Ok(())
    }
    
    async fn down(schema: &Schema) -> Result<()> {
        // Remove added columns
        schema.alter_table("users", |table| {
            table.drop_index("users_first_name_last_name_index");
            table.drop_index("users_phone_index");
            table.drop_index("users_location_index");
            table.drop_column("first_name");
            table.drop_column("last_name");
            table.drop_column("phone");
            table.drop_column("date_of_birth");
            table.drop_column("bio");
            table.drop_column("avatar_url");
            table.drop_column("website");
            table.drop_column("location");
            table.drop_column("timezone");
            table.drop_column("language");
        }).await?;
        
        Ok(())
    }
}
```

## Complex Schema Changes

```rust
use tusk_db::{SchemaChange, DataTransformation};

// Migration for complex schema changes
#[derive(Debug)]
struct MigrateUserNames;

#[async_trait]
impl Migration for MigrateUserNames {
    fn name() -> &'static str {
        "migrate_user_names"
    }
    
    fn version() -> &'static str {
        "2024_01_25_000001"
    }
    
    fn description() -> &'static str {
        "Migrate from single name field to first_name and last_name"
    }
    
    async fn up(schema: &Schema) -> Result<()> {
        // Add new columns
        schema.alter_table("users", |table| {
            table.add_column("first_name_temp", ColumnType::String(100)).nullable();
            table.add_column("last_name_temp", ColumnType::String(100)).nullable();
        }).await?;
        
        // Migrate data
        let users = schema.query("SELECT id, name FROM users").await?;
        
        for user in users {
            let name_parts: Vec<&str> = user["name"].as_str().unwrap().split_whitespace().collect();
            let first_name = name_parts.first().unwrap_or(&"");
            let last_name = if name_parts.len() > 1 {
                name_parts[1..].join(" ")
            } else {
                String::new()
            };
            
            schema.update(
                "UPDATE users SET first_name_temp = ?, last_name_temp = ? WHERE id = ?",
                &[first_name, &last_name, &user["id"]]
            ).await?;
        }
        
        // Drop old column and rename new ones
        schema.alter_table("users", |table| {
            table.drop_column("name");
            table.rename_column("first_name_temp", "first_name");
            table.rename_column("last_name_temp", "last_name");
        }).await?;
        
        // Make columns not null
        schema.alter_table("users", |table| {
            table.modify_column("first_name", |col| {
                col.not_null();
            });
        }).await?;
        
        Ok(())
    }
    
    async fn down(schema: &Schema) -> Result<()> {
        // Reverse the migration
        schema.alter_table("users", |table| {
            table.add_column("name_temp", ColumnType::String(255)).nullable();
        }).await?;
        
        // Combine first_name and last_name back to name
        let users = schema.query("SELECT id, first_name, last_name FROM users").await?;
        
        for user in users {
            let full_name = format!("{} {}", 
                user["first_name"].as_str().unwrap_or(""),
                user["last_name"].as_str().unwrap_or("")
            ).trim().to_string();
            
            schema.update(
                "UPDATE users SET name_temp = ? WHERE id = ?",
                &[&full_name, &user["id"]]
            ).await?;
        }
        
        // Drop new columns and rename old one
        schema.alter_table("users", |table| {
            table.drop_column("first_name");
            table.drop_column("last_name");
            table.rename_column("name_temp", "name");
        }).await?;
        
        Ok(())
    }
}
```

## Migration Runner and Management

```rust
use tusk_db::{MigrationRunner, MigrationStatus, MigrationHistory};

// Migration runner with Rust async patterns
async fn run_migrations() -> Result<()> {
    let runner = @MigrationRunner::new().await?;
    
    // Get pending migrations
    let pending = runner.get_pending_migrations().await?;
    
    if pending.is_empty() {
        println!("No pending migrations");
        return Ok(());
    }
    
    // Run migrations in order
    for migration in pending {
        println!("Running migration: {}", migration.name());
        
        match runner.run_migration(migration).await {
            Ok(_) => println!("✓ Migration completed successfully"),
            Err(e) => {
                println!("✗ Migration failed: {}", e);
                return Err(e);
            }
        }
    }
    
    println!("All migrations completed successfully");
    Ok(())
}

// Rollback migrations
async fn rollback_migrations(steps: usize) -> Result<()> {
    let runner = @MigrationRunner::new().await?;
    
    let history = runner.get_migration_history().await?;
    let to_rollback = history.iter().rev().take(steps).collect::<Vec<_>>();
    
    for migration in to_rollback {
        println!("Rolling back migration: {}", migration.name());
        
        match runner.rollback_migration(migration).await {
            Ok(_) => println!("✓ Rollback completed successfully"),
            Err(e) => {
                println!("✗ Rollback failed: {}", e);
                return Err(e);
            }
        }
    }
    
    println!("Rollback completed successfully");
    Ok(())
}

// Migration status and history
async fn migration_status() -> Result<()> {
    let runner = @MigrationRunner::new().await?;
    
    let status = runner.get_status().await?;
    let history = runner.get_migration_history().await?;
    
    println!("Migration Status:");
    println!("  Total migrations: {}", status.total);
    println!("  Run migrations: {}", status.run);
    println!("  Pending migrations: {}", status.pending);
    println!("  Last migration: {}", status.last_migration.unwrap_or("None".to_string()));
    
    println!("\nMigration History:");
    for migration in history {
        println!("  {} - {} ({})", 
            migration.version(),
            migration.name(),
            migration.run_at().unwrap_or_default()
        );
    }
    
    Ok(())
}
```

## Testing Migrations

```rust
use tusk_db::test_utils::{TestDatabase, TestMigration};

// Test migration with test database
#[tokio::test]
async fn test_create_users_table() -> Result<()> {
    let test_db = @TestDatabase::new().await?;
    
    // Run migration
    let migration = CreateUsersTable;
    test_db.run_migration(&migration).await?;
    
    // Verify table was created
    let tables = test_db.get_tables().await?;
    assert!(tables.contains(&"users".to_string()));
    
    // Verify columns exist
    let columns = test_db.get_table_columns("users").await?;
    assert!(columns.iter().any(|col| col.name == "id"));
    assert!(columns.iter().any(|col| col.name == "name"));
    assert!(columns.iter().any(|col| col.name == "email"));
    
    // Test rollback
    test_db.rollback_migration(&migration).await?;
    
    // Verify table was dropped
    let tables_after_rollback = test_db.get_tables().await?;
    assert!(!tables_after_rollback.contains(&"users".to_string()));
    
    Ok(())
}

// Integration test for complex migrations
#[tokio::test]
async fn test_complete_migration_chain() -> Result<()> {
    let test_db = @TestDatabase::new().await?;
    
    let migrations = vec![
        Box::new(CreateUsersTable),
        Box::new(CreatePostsTable),
        Box::new(CreateCommentsTable),
        Box::new(SeedInitialData),
    ];
    
    // Run all migrations
    for migration in &migrations {
        test_db.run_migration(migration.as_ref()).await?;
    }
    
    // Verify data integrity
    let user_count = test_db.query_one::<i64>("SELECT COUNT(*) FROM users").await?;
    assert_eq!(user_count, 1); // Admin user
    
    let post_count = test_db.query_one::<i64>("SELECT COUNT(*) FROM posts").await?;
    assert_eq!(post_count, 0); // No posts yet
    
    // Test rollback in reverse order
    for migration in migrations.iter().rev() {
        test_db.rollback_migration(migration.as_ref()).await?;
    }
    
    // Verify all tables were dropped
    let tables = test_db.get_tables().await?;
    assert!(tables.is_empty());
    
    Ok(())
}
```

## Production Migration Strategies

```rust
use tusk_db::{ProductionMigration, ZeroDowntimeMigration};

// Zero-downtime migration strategy
#[derive(Debug)]
struct AddUserIndexesZeroDowntime;

#[async_trait]
impl ZeroDowntimeMigration for AddUserIndexesZeroDowntime {
    fn name() -> &'static str {
        "add_user_indexes_zero_downtime"
    }
    
    fn version() -> &'static str {
        "2024_02_01_000001"
    }
    
    async fn up(schema: &Schema) -> Result<()> {
        // Add indexes concurrently to avoid locking
        schema.create_index_concurrently("users", &["email"], IndexType::BTree).await?;
        schema.create_index_concurrently("users", &["created_at"], IndexType::BTree).await?;
        schema.create_index_concurrently("users", &["is_active", "created_at"], IndexType::BTree).await?;
        
        Ok(())
    }
    
    async fn down(schema: &Schema) -> Result<()> {
        // Drop indexes concurrently
        schema.drop_index_concurrently("users", "users_email_index").await?;
        schema.drop_index_concurrently("users", "users_created_at_index").await?;
        schema.drop_index_concurrently("users", "users_is_active_created_at_index").await?;
        
        Ok(())
    }
    
    // Check if migration can be run safely
    async fn can_run_safely(schema: &Schema) -> Result<bool> {
        // Check table size
        let row_count = schema.query_one::<i64>("SELECT COUNT(*) FROM users").await?;
        
        // Check if indexes already exist
        let indexes = schema.get_table_indexes("users").await?;
        let email_index_exists = indexes.iter().any(|idx| idx.name == "users_email_index");
        
        Ok(row_count < 1_000_000 && !email_index_exists)
    }
}

// Blue-green deployment migration
#[derive(Debug)]
struct BlueGreenMigration;

#[async_trait]
impl ProductionMigration for BlueGreenMigration {
    fn name() -> &'static str {
        "blue_green_migration"
    }
    
    fn version() -> &'static str {
        "2024_02_01_000002"
    }
    
    async fn up(schema: &Schema) -> Result<()> {
        // Create new table with new schema
        schema.create_table("users_v2", |table| {
            table.id("id");
            table.string("name", 255).not_null();
            table.string("email", 255).unique().not_null();
            table.string("password_hash", 255).not_null();
            table.boolean("is_active").default(true);
            table.timestamp("created_at").default_current();
            table.timestamp("updated_at").default_current();
            // New fields
            table.string("first_name", 100).nullable();
            table.string("last_name", 100).nullable();
            table.string("phone", 20).nullable();
        }).await?;
        
        // Copy data from old table to new table
        schema.execute(
            "INSERT INTO users_v2 (id, name, email, password_hash, is_active, created_at, updated_at)
             SELECT id, name, email, password_hash, is_active, created_at, updated_at
             FROM users"
        ).await?;
        
        // Rename tables
        schema.rename_table("users", "users_old").await?;
        schema.rename_table("users_v2", "users").await?;
        
        Ok(())
    }
    
    async fn down(schema: &Schema) -> Result<()> {
        // Rollback by renaming tables back
        schema.rename_table("users", "users_v2").await?;
        schema.rename_table("users_old", "users").await?;
        schema.drop_table_if_exists("users_v2").await?;
        
        Ok(())
    }
}
```

## Migration Configuration and Environment

```rust
use tusk_db::{MigrationConfig, Environment};

// Migration configuration for different environments
async fn configure_migrations() -> Result<MigrationConfig> {
    let config = MigrationConfig {
        // Database connection
        database_url: @env("DATABASE_URL", "postgresql://localhost/myapp"),
        
        // Migration settings
        migrations_path: "migrations/",
        migration_table: "migrations",
        
        // Environment-specific settings
        environment: match @env("APP_ENV", "development").as_str() {
            "production" => Environment::Production,
            "staging" => Environment::Staging,
            "testing" => Environment::Testing,
            _ => Environment::Development,
        },
        
        // Production safety settings
        require_confirmation: @env("REQUIRE_MIGRATION_CONFIRMATION", "true").parse().unwrap_or(true),
        backup_before_migration: @env("BACKUP_BEFORE_MIGRATION", "true").parse().unwrap_or(true),
        max_execution_time: @env("MIGRATION_TIMEOUT", "300").parse().unwrap_or(300),
        
        // Rollback settings
        allow_rollback_in_production: false,
        max_rollback_steps: 1,
        
        // Logging
        verbose: @env("MIGRATION_VERBOSE", "false").parse().unwrap_or(false),
        log_queries: @env("MIGRATION_LOG_QUERIES", "false").parse().unwrap_or(false),
    };
    
    Ok(config)
}

// Environment-specific migration runner
async fn run_environment_migrations() -> Result<()> {
    let config = configure_migrations().await?;
    let runner = @MigrationRunner::with_config(config).await?;
    
    match config.environment {
        Environment::Development => {
            // Run all migrations without confirmation
            runner.run_all_migrations().await?;
        }
        Environment::Staging => {
            // Run with confirmation but allow rollback
            if runner.confirm_migrations().await? {
                runner.run_all_migrations().await?;
            }
        }
        Environment::Production => {
            // Strict production rules
            if !runner.can_run_safely().await? {
                return Err("Migrations cannot be run safely in production".into());
            }
            
            if runner.confirm_migrations().await? {
                runner.run_all_migrations().await?;
            }
        }
        Environment::Testing => {
            // Run in test mode
            runner.run_in_test_mode().await?;
        }
    }
    
    Ok(())
}
```

## Best Practices for Rust Migrations

1. **Version Control**: Always use semantic versioning for migrations
2. **Dependencies**: Define clear migration dependencies
3. **Rollback Safety**: Ensure all migrations can be rolled back
4. **Data Integrity**: Use transactions for data migrations
5. **Performance**: Use concurrent operations for large tables
6. **Testing**: Test migrations in all environments
7. **Documentation**: Document complex schema changes
8. **Backup**: Always backup before production migrations
9. **Monitoring**: Monitor migration performance and errors
10. **Zero-Downtime**: Use zero-downtime strategies for production

## Related Topics

- `database-overview-rust` - Database integration overview
- `query-builder-rust` - Fluent query interface
- `orm-models-rust` - Model definition and usage
- `relationships-rust` - Model relationships
- `database-transactions-rust` - Transaction handling

---

**Ready to manage your database schema with type-safe, version-controlled migrations in Rust?** 