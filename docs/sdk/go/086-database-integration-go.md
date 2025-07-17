# Database Integration in TuskLang for Go

## Overview

Database integration in TuskLang provides powerful database configuration and query capabilities directly in your configuration files. These features enable you to define sophisticated database connections, query patterns, and data access strategies with Go integration for robust data-driven applications.

## Basic Database Configuration

```go
// TuskLang database configuration
database: {
    connections: {
        primary: {
            type: "postgresql"
            host: "@env('DB_HOST', 'localhost')"
            port: "@env('DB_PORT', '5432')"
            database: "@env('DB_NAME', 'myapp')"
            username: "@env('DB_USER', 'postgres')"
            password: "@env('DB_PASSWORD')"
            ssl_mode: "require"
            max_connections: 20
            idle_timeout: "5m"
        }
        
        read_replica: {
            type: "postgresql"
            host: "@env('DB_READ_HOST', 'localhost')"
            port: "@env('DB_READ_PORT', '5432')"
            database: "@env('DB_NAME', 'myapp')"
            username: "@env('DB_USER', 'postgres')"
            password: "@env('DB_PASSWORD')"
            ssl_mode: "require"
            max_connections: 10
            read_only: true
        }
        
        cache: {
            type: "redis"
            host: "@env('REDIS_HOST', 'localhost')"
            port: "@env('REDIS_PORT', '6379')"
            database: 0
            password: "@env('REDIS_PASSWORD')"
            pool_size: 10
        }
    }
    
    queries: {
        user_by_id: "SELECT id, name, email, created_at FROM users WHERE id = $1"
        users_by_role: "SELECT id, name, email FROM users WHERE role = $1 ORDER BY created_at DESC"
        user_count: "SELECT COUNT(*) FROM users WHERE active = true"
        create_user: "INSERT INTO users (name, email, role) VALUES ($1, $2, $3) RETURNING id"
        update_user: "UPDATE users SET name = $2, email = $3 WHERE id = $1"
        delete_user: "DELETE FROM users WHERE id = $1"
    }
    
    migrations: {
        enabled: true
        path: "./migrations"
        table: "schema_migrations"
        timeout: "5m"
    }
    
    pooling: {
        enabled: true
        max_open_connections: 25
        max_idle_connections: 5
        connection_max_lifetime: "1h"
        connection_max_idle_time: "15m"
    }
}
```

## Go Integration

```go
package main

import (
    "context"
    "database/sql"
    "fmt"
    "log"
    "time"
    
    _ "github.com/lib/pq"
    "github.com/go-redis/redis/v8"
    "github.com/tusklang/go-sdk"
)

type DatabaseConfig struct {
    Connections map[string]Connection `tsk:"connections"`
    Queries     map[string]string     `tsk:"queries"`
    Migrations  MigrationConfig       `tsk:"migrations"`
    Pooling     PoolingConfig         `tsk:"pooling"`
}

type Connection struct {
    Type                string `tsk:"type"`
    Host                string `tsk:"host"`
    Port                string `tsk:"port"`
    Database            string `tsk:"database"`
    Username            string `tsk:"username"`
    Password            string `tsk:"password"`
    SSLMode             string `tsk:"ssl_mode"`
    MaxConnections      int    `tsk:"max_connections"`
    IdleTimeout         string `tsk:"idle_timeout"`
    ReadOnly            bool   `tsk:"read_only"`
    PoolSize            int    `tsk:"pool_size"`
}

type MigrationConfig struct {
    Enabled bool   `tsk:"enabled"`
    Path    string `tsk:"path"`
    Table   string `tsk:"table"`
    Timeout string `tsk:"timeout"`
}

type PoolingConfig struct {
    Enabled                 bool   `tsk:"enabled"`
    MaxOpenConnections      int    `tsk:"max_open_connections"`
    MaxIdleConnections      int    `tsk:"max_idle_connections"`
    ConnectionMaxLifetime   string `tsk:"connection_max_lifetime"`
    ConnectionMaxIdleTime   string `tsk:"connection_max_idle_time"`
}

type DatabaseManager struct {
    config   DatabaseConfig
    primary  *sql.DB
    readReplica *sql.DB
    cache    *redis.Client
    queries  map[string]string
}

type User struct {
    ID        int       `json:"id"`
    Name      string    `json:"name"`
    Email     string    `json:"email"`
    Role      string    `json:"role"`
    Active    bool      `json:"active"`
    CreatedAt time.Time `json:"created_at"`
}

func main() {
    // Load database configuration
    config, err := tusk.LoadFile("database-config.tsk")
    if err != nil {
        log.Fatalf("Error loading database config: %v", err)
    }
    
    var dbConfig DatabaseConfig
    if err := config.Get("database", &dbConfig); err != nil {
        log.Fatalf("Error parsing database config: %v", err)
    }
    
    // Initialize database manager
    dbManager := NewDatabaseManager(dbConfig)
    defer dbManager.Close()
    
    // Run migrations
    if err := dbManager.RunMigrations(); err != nil {
        log.Fatalf("Error running migrations: %v", err)
    }
    
    // Example usage
    user, err := dbManager.GetUserByID(1)
    if err != nil {
        log.Printf("Error getting user: %v", err)
    } else {
        log.Printf("User: %+v", user)
    }
    
    // Create new user
    newUser := User{
        Name:  "John Doe",
        Email: "john@example.com",
        Role:  "user",
    }
    
    userID, err := dbManager.CreateUser(newUser)
    if err != nil {
        log.Printf("Error creating user: %v", err)
    } else {
        log.Printf("Created user with ID: %d", userID)
    }
}

func NewDatabaseManager(config DatabaseConfig) *DatabaseManager {
    manager := &DatabaseManager{
        config:  config,
        queries: config.Queries,
    }
    
    // Initialize primary database connection
    if primary, exists := config.Connections["primary"]; exists {
        db, err := manager.connectPostgreSQL(primary)
        if err != nil {
            log.Fatalf("Error connecting to primary database: %v", err)
        }
        manager.primary = db
    }
    
    // Initialize read replica connection
    if readReplica, exists := config.Connections["read_replica"]; exists {
        db, err := manager.connectPostgreSQL(readReplica)
        if err != nil {
            log.Printf("Warning: Error connecting to read replica: %v", err)
        } else {
            manager.readReplica = db
        }
    }
    
    // Initialize cache connection
    if cache, exists := config.Connections["cache"]; exists {
        redisClient, err := manager.connectRedis(cache)
        if err != nil {
            log.Printf("Warning: Error connecting to cache: %v", err)
        } else {
            manager.cache = redisClient
        }
    }
    
    return manager
}

func (dm *DatabaseManager) connectPostgreSQL(conn Connection) (*sql.DB, error) {
    dsn := fmt.Sprintf(
        "host=%s port=%s dbname=%s user=%s password=%s sslmode=%s",
        conn.Host, conn.Port, conn.Database, conn.Username, conn.Password, conn.SSLMode,
    )
    
    db, err := sql.Open("postgres", dsn)
    if err != nil {
        return nil, err
    }
    
    // Configure connection pooling
    if dm.config.Pooling.Enabled {
        db.SetMaxOpenConns(dm.config.Pooling.MaxOpenConnections)
        db.SetMaxIdleConns(dm.config.Pooling.MaxIdleConnections)
        
        if lifetime, err := time.ParseDuration(dm.config.Pooling.ConnectionMaxLifetime); err == nil {
            db.SetConnMaxLifetime(lifetime)
        }
        
        if idleTime, err := time.ParseDuration(dm.config.Pooling.ConnectionMaxIdleTime); err == nil {
            db.SetConnMaxIdleTime(idleTime)
        }
    }
    
    // Test connection
    if err := db.Ping(); err != nil {
        return nil, err
    }
    
    return db, nil
}

func (dm *DatabaseManager) connectRedis(conn Connection) (*redis.Client, error) {
    client := redis.NewClient(&redis.Options{
        Addr:     fmt.Sprintf("%s:%s", conn.Host, conn.Port),
        Password: conn.Password,
        DB:       conn.Database,
        PoolSize: conn.PoolSize,
    })
    
    // Test connection
    ctx := context.Background()
    if err := client.Ping(ctx).Err(); err != nil {
        return nil, err
    }
    
    return client, nil
}

// Database operations
func (dm *DatabaseManager) GetUserByID(id int) (*User, error) {
    // Try cache first
    if dm.cache != nil {
        if user, err := dm.getUserFromCache(id); err == nil {
            return user, nil
        }
    }
    
    // Query database
    query := dm.queries["user_by_id"]
    row := dm.getReadDB().QueryRow(query, id)
    
    user := &User{}
    err := row.Scan(&user.ID, &user.Name, &user.Email, &user.CreatedAt)
    if err != nil {
        return nil, err
    }
    
    // Cache the result
    if dm.cache != nil {
        dm.cacheUser(user)
    }
    
    return user, nil
}

func (dm *DatabaseManager) GetUsersByRole(role string) ([]*User, error) {
    query := dm.queries["users_by_role"]
    rows, err := dm.getReadDB().Query(query, role)
    if err != nil {
        return nil, err
    }
    defer rows.Close()
    
    var users []*User
    for rows.Next() {
        user := &User{}
        err := rows.Scan(&user.ID, &user.Name, &user.Email)
        if err != nil {
            return nil, err
        }
        users = append(users, user)
    }
    
    return users, nil
}

func (dm *DatabaseManager) GetUserCount() (int, error) {
    query := dm.queries["user_count"]
    row := dm.getReadDB().QueryRow(query)
    
    var count int
    err := row.Scan(&count)
    return count, err
}

func (dm *DatabaseManager) CreateUser(user User) (int, error) {
    query := dm.queries["create_user"]
    row := dm.primary.QueryRow(query, user.Name, user.Email, user.Role)
    
    var id int
    err := row.Scan(&id)
    if err != nil {
        return 0, err
    }
    
    // Invalidate cache
    if dm.cache != nil {
        dm.invalidateUserCache(id)
    }
    
    return id, nil
}

func (dm *DatabaseManager) UpdateUser(id int, user User) error {
    query := dm.queries["update_user"]
    _, err := dm.primary.Exec(query, id, user.Name, user.Email)
    if err != nil {
        return err
    }
    
    // Invalidate cache
    if dm.cache != nil {
        dm.invalidateUserCache(id)
    }
    
    return nil
}

func (dm *DatabaseManager) DeleteUser(id int) error {
    query := dm.queries["delete_user"]
    _, err := dm.primary.Exec(query, id)
    if err != nil {
        return err
    }
    
    // Invalidate cache
    if dm.cache != nil {
        dm.invalidateUserCache(id)
    }
    
    return nil
}

func (dm *DatabaseManager) getReadDB() *sql.DB {
    if dm.readReplica != nil {
        return dm.readReplica
    }
    return dm.primary
}

// Cache operations
func (dm *DatabaseManager) getUserFromCache(id int) (*User, error) {
    ctx := context.Background()
    key := fmt.Sprintf("user:%d", id)
    
    data, err := dm.cache.Get(ctx, key).Result()
    if err != nil {
        return nil, err
    }
    
    var user User
    if err := json.Unmarshal([]byte(data), &user); err != nil {
        return nil, err
    }
    
    return &user, nil
}

func (dm *DatabaseManager) cacheUser(user *User) {
    ctx := context.Background()
    key := fmt.Sprintf("user:%d", user.ID)
    
    data, err := json.Marshal(user)
    if err != nil {
        return
    }
    
    dm.cache.Set(ctx, key, data, time.Hour)
}

func (dm *DatabaseManager) invalidateUserCache(id int) {
    ctx := context.Background()
    key := fmt.Sprintf("user:%d", id)
    dm.cache.Del(ctx, key)
}

// Migration operations
func (dm *DatabaseManager) RunMigrations() error {
    if !dm.config.Migrations.Enabled {
        return nil
    }
    
    // Create migrations table if it doesn't exist
    createTableSQL := fmt.Sprintf(`
        CREATE TABLE IF NOT EXISTS %s (
            id SERIAL PRIMARY KEY,
            version VARCHAR(255) NOT NULL UNIQUE,
            applied_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        )
    `, dm.config.Migrations.Table)
    
    _, err := dm.primary.Exec(createTableSQL)
    if err != nil {
        return err
    }
    
    // Read migration files
    files, err := os.ReadDir(dm.config.Migrations.Path)
    if err != nil {
        return err
    }
    
    // Sort files by name
    var migrations []string
    for _, file := range files {
        if strings.HasSuffix(file.Name(), ".sql") {
            migrations = append(migrations, file.Name())
        }
    }
    sort.Strings(migrations)
    
    // Apply migrations
    for _, migration := range migrations {
        if err := dm.applyMigration(migration); err != nil {
            return err
        }
    }
    
    return nil
}

func (dm *DatabaseManager) applyMigration(filename string) error {
    // Check if migration already applied
    var count int
    query := fmt.Sprintf("SELECT COUNT(*) FROM %s WHERE version = $1", dm.config.Migrations.Table)
    err := dm.primary.QueryRow(query, filename).Scan(&count)
    if err != nil {
        return err
    }
    
    if count > 0 {
        return nil // Already applied
    }
    
    // Read migration file
    filepath := filepath.Join(dm.config.Migrations.Path, filename)
    content, err := os.ReadFile(filepath)
    if err != nil {
        return err
    }
    
    // Start transaction
    tx, err := dm.primary.Begin()
    if err != nil {
        return err
    }
    defer tx.Rollback()
    
    // Execute migration
    _, err = tx.Exec(string(content))
    if err != nil {
        return err
    }
    
    // Record migration
    recordQuery := fmt.Sprintf("INSERT INTO %s (version) VALUES ($1)", dm.config.Migrations.Table)
    _, err = tx.Exec(recordQuery, filename)
    if err != nil {
        return err
    }
    
    // Commit transaction
    return tx.Commit()
}

func (dm *DatabaseManager) Close() error {
    if dm.primary != nil {
        dm.primary.Close()
    }
    if dm.readReplica != nil {
        dm.readReplica.Close()
    }
    if dm.cache != nil {
        dm.cache.Close()
    }
    return nil
}
```

## Advanced Database Features

### Query Builder

```go
// TuskLang configuration with query builder
database: {
    query_builder: {
        enabled: true
        features: {
            joins: true
            where_clauses: true
            order_by: true
            group_by: true
            having: true
            limit_offset: true
        }
        
        templates: {
            user_search: {
                base: "SELECT id, name, email FROM users"
                where: "WHERE active = true"
                order: "ORDER BY created_at DESC"
                limit: "LIMIT $1 OFFSET $2"
            }
            
            product_list: {
                base: "SELECT p.id, p.name, p.price, c.name as category FROM products p"
                join: "JOIN categories c ON p.category_id = c.id"
                where: "WHERE p.active = true"
                order: "ORDER BY p.created_at DESC"
            }
        }
    }
}
```

### Connection Pooling

```go
// TuskLang configuration with advanced pooling
database: {
    pooling: {
        enabled: true
        strategies: {
            round_robin: {
                enabled: true
                connections: ["primary", "read_replica_1", "read_replica_2"]
            }
            
            weighted: {
                enabled: true
                weights: {
                    primary: 1
                    read_replica_1: 2
                    read_replica_2: 2
                }
            }
        }
        
        health_checks: {
            enabled: true
            interval: "30s"
            timeout: "5s"
            max_failures: 3
        }
    }
}
```

## Performance Considerations

- **Connection Pooling**: Use appropriate connection pool sizes
- **Query Optimization**: Optimize queries for performance
- **Caching**: Implement caching for frequently accessed data
- **Read Replicas**: Use read replicas for read-heavy workloads
- **Indexing**: Ensure proper database indexing

## Security Notes

- **Connection Security**: Use SSL/TLS for database connections
- **Credential Management**: Secure database credentials
- **SQL Injection**: Use parameterized queries to prevent SQL injection
- **Access Control**: Implement proper database access controls
- **Audit Logging**: Log database operations for security auditing

## Best Practices

1. **Connection Management**: Properly manage database connections
2. **Error Handling**: Implement comprehensive error handling
3. **Transaction Management**: Use transactions for data consistency
4. **Migration Strategy**: Use version-controlled database migrations
5. **Monitoring**: Monitor database performance and health
6. **Backup Strategy**: Implement proper database backup strategies

## Integration Examples

### With GORM

```go
import (
    "gorm.io/gorm"
    "gorm.io/driver/postgres"
    "github.com/tusklang/go-sdk"
)

func setupGORM(config tusk.Config) *gorm.DB {
    var dbConfig DatabaseConfig
    config.Get("database", &dbConfig)
    
    primary := dbConfig.Connections["primary"]
    dsn := fmt.Sprintf(
        "host=%s port=%s dbname=%s user=%s password=%s sslmode=%s",
        primary.Host, primary.Port, primary.Database, primary.Username, primary.Password, primary.SSLMode,
    )
    
    db, err := gorm.Open(postgres.Open(dsn), &gorm.Config{})
    if err != nil {
        log.Fatalf("Error connecting to database: %v", err)
    }
    
    return db
}
```

### With SQLx

```go
import (
    "github.com/jmoiron/sqlx"
    _ "github.com/lib/pq"
    "github.com/tusklang/go-sdk"
)

func setupSQLx(config tusk.Config) *sqlx.DB {
    var dbConfig DatabaseConfig
    config.Get("database", &dbConfig)
    
    primary := dbConfig.Connections["primary"]
    dsn := fmt.Sprintf(
        "host=%s port=%s dbname=%s user=%s password=%s sslmode=%s",
        primary.Host, primary.Port, primary.Database, primary.Username, primary.Password, primary.SSLMode,
    )
    
    db, err := sqlx.Connect("postgres", dsn)
    if err != nil {
        log.Fatalf("Error connecting to database: %v", err)
    }
    
    return db
}
```

This comprehensive database integration documentation provides Go developers with everything they need to build sophisticated database systems using TuskLang's powerful configuration capabilities. 