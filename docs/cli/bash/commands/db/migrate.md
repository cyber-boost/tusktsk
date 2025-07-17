# tsk db migrate

Run database migration files to update schema and data.

## Synopsis

```bash
tsk db migrate <file> [OPTIONS]
```

## Description

The `tsk db migrate` command executes SQL migration files to update your database schema and data. It supports both forward migrations and rollbacks, with transaction safety and error handling.

This command is essential for:
- Applying schema changes safely
- Managing database versioning
- Deploying database updates
- Rolling back problematic changes

## Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| --help | -h | Show help for this command | - |
| --json | -j | Output in JSON format | false |
| --verbose | -v | Enable verbose output | false |
| --quiet | -q | Suppress non-error output | false |
| --dry-run | -d | Show what would be executed without running | false |
| --rollback | -r | Rollback the specified migration | false |
| --force | -f | Force migration even if errors occur | false |
| --transaction | -t | Use transaction (default: true) | true |

## Arguments

| Argument | Required | Description |
|----------|----------|-------------|
| file | Yes | Path to migration SQL file |

## Examples

### Basic Migration
```bash
# Run a migration file
tsk db migrate schema.sql
```

Expected output:
```
🔄 Running migration: schema.sql
📍 Executing: CREATE TABLE users (id SERIAL PRIMARY KEY, name VARCHAR(255))
✅ Migration completed successfully
📊 1 statement executed
⏱️  Duration: 0.15s
```

### Dry Run
```bash
# Preview migration without executing
tsk db migrate schema.sql --dry-run
```

Expected output:
```
🔍 Dry run mode - no changes will be made
📍 Would execute: CREATE TABLE users (id SERIAL PRIMARY KEY, name VARCHAR(255))
📍 Would execute: CREATE INDEX idx_users_name ON users(name)
📊 2 statements would be executed
```

### Verbose Output
```bash
# Get detailed migration information
tsk db migrate schema.sql --verbose
```

Expected output:
```
🔄 Running migration: schema.sql
🔍 Checking database connection...
✅ Connection established
🔍 Validating migration file...
✅ File syntax is valid
🔍 Starting transaction...
📍 Executing: CREATE TABLE users (id SERIAL PRIMARY KEY, name VARCHAR(255))
✅ Statement 1 completed
📍 Executing: CREATE INDEX idx_users_name ON users(name)
✅ Statement 2 completed
🔍 Committing transaction...
✅ Transaction committed
📊 Migration Statistics:
   - Statements executed: 2
   - Tables created: 1
   - Indexes created: 1
   - Duration: 0.23s
✅ Migration completed successfully
```

### Rollback Migration
```bash
# Rollback a specific migration
tsk db migrate schema.sql --rollback
```

Expected output:
```
🔄 Rolling back migration: schema.sql
📍 Executing: DROP TABLE users CASCADE
✅ Rollback completed successfully
📊 1 statement executed
⏱️  Duration: 0.08s
```

### Force Migration
```bash
# Force migration even with warnings
tsk db migrate schema.sql --force
```

Expected output:
```
⚠️  Warning: Table 'users' already exists
🔄 Forcing migration: schema.sql
📍 Executing: DROP TABLE users CASCADE
📍 Executing: CREATE TABLE users (id SERIAL PRIMARY KEY, name VARCHAR(255))
✅ Migration completed successfully (forced)
📊 2 statements executed
⏱️  Duration: 0.31s
```

### JSON Output
```bash
# Get migration results in JSON format
tsk db migrate schema.sql --json
```

Expected output:
```json
{
  "status": "success",
  "file": "schema.sql",
  "statements_executed": 2,
  "duration_ms": 150,
  "tables_created": 1,
  "indexes_created": 1,
  "timestamp": "2024-12-19T10:30:00Z"
}
```

## Bash API Usage

```bash
#!/bin/bash
# run_migrations.sh

# Run all migration files
for migration in migrations/*.sql; do
    echo "Running migration: $migration"
    
    if tsk db migrate "$migration" --quiet; then
        echo "✅ Migration successful: $migration"
    else
        echo "❌ Migration failed: $migration"
        exit 1
    fi
done
```

### Migration Function

```bash
#!/bin/bash
# migration_utils.sh

run_migration() {
    local file="$1"
    local dry_run="${2:-false}"
    
    echo "Running migration: $file"
    
    if [[ "$dry_run" == "true" ]]; then
        tsk db migrate "$file" --dry-run
    else
        tsk db migrate "$file"
    fi
    
    return $?
}

# Usage
run_migration "schema.sql"
run_migration "data.sql" "true"  # dry run
```

### Deployment Script

```bash
#!/bin/bash
# deploy.sh

echo "Starting database deployment..."

# Check database status
if ! tsk db status --quiet; then
    echo "❌ Database is not ready"
    exit 1
fi

# Run migrations
for migration in migrations/*.sql; do
    echo "Applying migration: $(basename "$migration")"
    
    if ! tsk db migrate "$migration" --quiet; then
        echo "❌ Migration failed: $migration"
        echo "Rolling back..."
        tsk db migrate "$migration" --rollback
        exit 1
    fi
done

echo "✅ All migrations completed successfully"
```

## Migration File Format

### Basic SQL Migration
```sql
-- migrations/001_create_users.sql
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    email VARCHAR(255) UNIQUE NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_users_email ON users(email);
```

### Migration with Rollback
```sql
-- migrations/002_add_user_role.sql
-- Forward migration
ALTER TABLE users ADD COLUMN role VARCHAR(50) DEFAULT 'user';

-- Rollback (commented out, executed with --rollback flag)
-- ALTER TABLE users DROP COLUMN role;
```

### Complex Migration
```sql
-- migrations/003_user_permissions.sql
BEGIN;

-- Create permissions table
CREATE TABLE permissions (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) UNIQUE NOT NULL,
    description TEXT
);

-- Create user_permissions junction table
CREATE TABLE user_permissions (
    user_id INTEGER REFERENCES users(id) ON DELETE CASCADE,
    permission_id INTEGER REFERENCES permissions(id) ON DELETE CASCADE,
    granted_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (user_id, permission_id)
);

-- Insert default permissions
INSERT INTO permissions (name, description) VALUES
    ('read', 'Read access'),
    ('write', 'Write access'),
    ('admin', 'Administrative access');

COMMIT;
```

## Output

### Success Output

```
🔄 Running migration: schema.sql
📍 Executing: CREATE TABLE users (id SERIAL PRIMARY KEY, name VARCHAR(255))
✅ Migration completed successfully
📊 1 statement executed
⏱️  Duration: 0.15s
```

### Error Output

```
❌ Migration failed: schema.sql
Error: relation "users" already exists
📍 Failed at statement: CREATE TABLE users (id SERIAL PRIMARY KEY, name VARCHAR(255))
🔍 Use --force to override or --dry-run to preview
```

### JSON Output Format

```json
{
  "status": "success|error|rolled_back",
  "file": "string",
  "statements_executed": "number",
  "duration_ms": "number",
  "error": "string (if status is error)",
  "tables_created": "number",
  "indexes_created": "number",
  "timestamp": "ISO 8601 timestamp"
}
```

## Exit Codes

| Code | Description |
|------|-------------|
| 0 | Migration completed successfully |
| 1 | Migration failed |
| 2 | Invalid migration file |
| 3 | Database connection failed |
| 4 | Rollback failed |

## Related Commands

- [tsk db status](./status.md) - Check database status
- [tsk db console](./console.md) - Interactive database console
- [tsk db backup](./backup.md) - Backup before migration
- [tsk db restore](./restore.md) - Restore from backup
- [tsk db init](./init.md) - Initialize database

## Notes

- Always backup your database before running migrations
- Use `--dry-run` to preview changes before applying
- Migrations run in transactions by default for safety
- Use `--force` carefully as it can cause data loss
- Migration files should be idempotent when possible
- Consider using versioned migration files (001_, 002_, etc.)

## See Also

- [Database Commands Overview](./README.md)
- [Configuration Commands](../config/README.md)
- [Troubleshooting](../../troubleshooting.md) 