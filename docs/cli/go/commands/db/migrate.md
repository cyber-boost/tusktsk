# tsk db migrate

Run database migration files to update database schema.

## Synopsis

```bash
tsk db migrate [OPTIONS] <file>
```

## Description

The `tsk db migrate` command executes SQL migration files to update your database schema. It supports both individual files and directory-based migrations with automatic ordering and rollback capabilities.

## Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| --help | -h | Show help for this command | - |
| --dry-run | -d | Show what would be executed without running | false |
| --rollback | -r | Rollback the last migration | false |
| --force | -f | Force migration even if errors occur | false |
| --timeout | -t | Migration timeout in seconds | 300 |
| --json | -j | Output in JSON format | false |
| --quiet | -q | Suppress output, only return exit code | false |
| --verbose | -v | Show verbose output | false |

## Arguments

| Argument | Required | Description |
|----------|----------|-------------|
| file | Yes | Migration file or directory path |

## Examples

### Basic Migration

```bash
# Run single migration file
tsk db migrate schema.sql

# Run all migrations in directory
tsk db migrate migrations/
```

**Output:**
```
🔄 Running migration: schema.sql
✅ Migration completed successfully
📊 Tables created: 3
📊 Indexes created: 5
⏱️ Duration: 1.2s
```

### Dry Run

```bash
# Preview migration without executing
tsk db migrate --dry-run schema.sql
```

**Output:**
```
🔍 Dry run mode - no changes will be made
📋 Migration plan:
  - CREATE TABLE users (id SERIAL PRIMARY KEY, name VARCHAR(255))
  - CREATE TABLE posts (id SERIAL PRIMARY KEY, user_id INTEGER REFERENCES users(id))
  - CREATE INDEX idx_posts_user_id ON posts(user_id)
```

### Rollback Migration

```bash
# Rollback the last migration
tsk db migrate --rollback

# Rollback specific migration
tsk db migrate --rollback 20241219_001_create_users.sql
```

**Output:**
```
🔄 Rolling back migration: 20241219_001_create_users.sql
✅ Rollback completed successfully
📊 Tables dropped: 1
⏱️ Duration: 0.8s
```

### Verbose Output

```bash
# Show detailed migration progress
tsk db migrate --verbose migrations/
```

**Output:**
```
🔄 Starting migration process...
📍 Database: myapp
👤 User: postgres
📁 Migration directory: migrations/

🔄 Running: 20241219_001_create_users.sql
  📋 Executing: CREATE TABLE users...
  ✅ Table created successfully
  📋 Executing: CREATE INDEX idx_users_email...
  ✅ Index created successfully

🔄 Running: 20241219_002_create_posts.sql
  📋 Executing: CREATE TABLE posts...
  ✅ Table created successfully

✅ All migrations completed successfully
📊 Total migrations: 2
📊 Tables created: 2
📊 Indexes created: 3
⏱️ Total duration: 2.1s
```

### JSON Output

```bash
# Get migration results in JSON format
tsk db migrate --json schema.sql
```

**Output:**
```json
{
  "status": "success",
  "migration": "schema.sql",
  "duration_ms": 1200,
  "changes": {
    "tables_created": 3,
    "indexes_created": 5,
    "constraints_added": 2
  },
  "sql_statements": [
    "CREATE TABLE users (id SERIAL PRIMARY KEY, name VARCHAR(255))",
    "CREATE TABLE posts (id SERIAL PRIMARY KEY, user_id INTEGER REFERENCES users(id))",
    "CREATE INDEX idx_posts_user_id ON posts(user_id)"
  ]
}
```

## Go API Usage

```go
package main

import (
    "fmt"
    "log"
    "github.com/tusklang/go-sdk/cli"
)

func main() {
    // Create database client
    dbClient := cli.NewDatabaseClient()
    
    // Run migration
    result, err := dbClient.Migrate("schema.sql", &cli.MigrateOptions{
        DryRun: false,
        Force:  false,
        Timeout: 300,
    })
    if err != nil {
        log.Fatal(err)
    }
    
    // Print results
    fmt.Printf("Migration completed in %v\n", result.Duration)
    fmt.Printf("Tables created: %d\n", result.TablesCreated)
    fmt.Printf("Indexes created: %d\n", result.IndexesCreated)
}
```

## Migration File Format

### SQL Migration File

```sql
-- Migration: 20241219_001_create_users.sql
-- Description: Create users table
-- Author: John Doe
-- Date: 2024-12-19

-- Up migration
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    email VARCHAR(255) UNIQUE NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_users_email ON users(email);

-- Down migration (for rollback)
-- DROP TABLE users CASCADE;
```

### TuskLang Migration File

```tsk
// Migration: 20241219_002_create_posts.tsk
// Description: Create posts table with TuskLang syntax

migration {
    up {
        create_table "posts" {
            id: serial primary_key
            user_id: integer foreign_key("users.id")
            title: varchar(255) not_null
            content: text
            created_at: timestamp default(now())
            updated_at: timestamp default(now())
        }
        
        create_index "idx_posts_user_id" on "posts" ("user_id")
        create_index "idx_posts_created_at" on "posts" ("created_at")
    }
    
    down {
        drop_table "posts"
    }
}
```

## Output

### Success Output

When migration completes successfully:

- Migration status with ✅ symbol
- File or directory processed
- Number of changes made (tables, indexes, etc.)
- Execution duration
- Summary of operations

### Error Output

When migration fails:

- Error status with ❌ symbol
- Detailed error message
- Failed SQL statement
- Rollback instructions

**Example:**
```
❌ Migration failed
📍 File: schema.sql
💬 Error: relation "users" already exists
📋 Failed SQL: CREATE TABLE users (id SERIAL PRIMARY KEY)
💡 Suggestion: Use --force to ignore errors or check existing schema
```

## Exit Codes

| Code | Description |
|------|-------------|
| 0 | Success - Migration completed |
| 1 | Error - Migration failed |
| 2 | Error - Invalid migration file |
| 3 | Error - Database connection failed |
| 4 | Error - Rollback failed |

## Migration Best Practices

### 1. File Naming Convention

```
YYYYMMDD_HHMMSS_description.sql
20241219_143022_create_users_table.sql
20241219_143045_add_email_index.sql
```

### 2. Migration Content

- Include descriptive comments
- Use transactions for atomicity
- Provide rollback statements
- Test migrations in development first

### 3. Version Control

- Commit migration files to version control
- Never modify existing migration files
- Create new migrations for schema changes
- Document breaking changes

### 4. Production Deployment

```bash
# Backup before migration
tsk db backup pre_migration_backup.sql

# Run migration with dry-run first
tsk db migrate --dry-run migrations/

# Execute migration
tsk db migrate migrations/

# Verify migration
tsk db status
```

## Related Commands

- [tsk db status](./status.md) - Check database status
- [tsk db backup](./backup.md) - Create database backup
- [tsk db restore](./restore.md) - Restore from backup
- [tsk db init](./init.md) - Initialize database

## Notes

- **Transactions**: Migrations run in transactions for atomicity
- **Ordering**: Files are executed in alphabetical order
- **Rollback**: Use `--rollback` to undo the last migration
- **Safety**: Always backup before running migrations in production
- **Validation**: Migration files are validated before execution

## See Also

- [Database Commands Overview](./README.md)
- [Configuration Guide](../../../go/docs/PNT_GUIDE.md)
- [Examples](../../examples/database-migrations.md) 