# tsk db migrate

Run database migration files to update schema.

## Synopsis

```bash
tsk db migrate <file> [OPTIONS]
```

## Description

The `tsk db migrate` command executes SQL migration files to update the database schema. It supports both individual files and directories containing multiple migration files.

## Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| --help | -h | Show help for this command | - |
| --force | -f | Force migration even if already applied | false |
| --dry-run | -d | Show what would be executed without running | false |
| --verbose | -v | Show detailed migration information | false |
| --json | -j | Output results in JSON format | false |

## Arguments

| Argument | Required | Description |
|----------|----------|-------------|
| file | Yes | Migration file or directory path |

## Examples

### Basic Usage
```bash
# Run single migration file
tsk db migrate migrations/001_create_users.sql
```

**Output:**
```
🗄️ Database Migration
📁 Migration file: migrations/001_create_users.sql
📊 File size: 2.3KB
🔍 Checking migration status...

✅ Migration applied successfully
📈 Changes made:
   - Created table: users
   - Added columns: id, name, email, created_at
   - Created indexes: idx_users_email
   - Rows affected: 0

⏱️ Execution time: 45ms
📊 Migration recorded in schema_migrations table
```

### Directory Migration
```bash
# Run all migrations in directory
tsk db migrate migrations/
```

### Dry Run
```bash
# Preview migration without executing
tsk db migrate migrations/002_add_indexes.sql --dry-run
```

## Java API Usage

```java
import org.tusklang.cli.DatabaseCommands;

public class DatabaseMigration {
    public void runMigration() {
        DatabaseCommands db = new DatabaseCommands();
        
        // Run single migration
        MigrationResult result = db.migrate("migrations/001_create_users.sql");
        System.out.println("Success: " + result.isSuccess());
        
        // Run directory
        MigrationResult dirResult = db.migrateDirectory("migrations/");
        System.out.println("Migrations applied: " + dirResult.getAppliedCount());
        
        // Dry run
        MigrationResult dryRun = db.migrateDryRun("migrations/002_add_indexes.sql");
        System.out.println("Would apply: " + dryRun.getChanges());
    }
}
```

## Exit Codes

| Code | Description |
|------|-------------|
| 0 | Success - Migration applied |
| 1 | Error - Migration failed |
| 2 | Warning - Migration already applied |

## Related Commands

- [tsk db status](./status.md) - Check database status
- [tsk db backup](./backup.md) - Create backup
- [tsk db restore](./restore.md) - Restore from backup 