# tsk db backup

Create database backup files for data protection and recovery.

## Synopsis

```bash
tsk db backup [OPTIONS] [file]
```

## Description

The `tsk db backup` command creates database backups in various formats. It supports full database dumps, incremental backups, and scheduled backup operations.

## Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| --help | -h | Show help for this command | - |
| --format | -f | Backup format (sql, custom, directory) | sql |
| --compress | -c | Compress backup file | true |
| --tables | -t | Backup specific tables only | - |
| --exclude | -e | Exclude specific tables | - |
| --data-only | -d | Backup data only (no schema) | false |
| --schema-only | -s | Backup schema only (no data) | false |
| --scheduled | | Create scheduled backup | false |
| --json | -j | Output in JSON format | false |
| --quiet | -q | Suppress output, only return exit code | false |
| --verbose | -v | Show verbose output | false |

## Arguments

| Argument | Required | Description |
|----------|----------|-------------|
| file | No | Output backup file path (default: backup_YYYYMMDD_HHMMSS.sql) |

## Examples

### Basic Backup

```bash
# Create backup with default name
tsk db backup

# Create backup with custom name
tsk db backup myapp_backup.sql
```

**Output:**
```
🔄 Creating database backup...
📍 Database: myapp
📁 Output file: backup_20241219_143022.sql
📊 Tables: 15
📊 Data size: 45.2 MB
⏱️ Duration: 12.3s
✅ Backup completed successfully
💾 File size: 12.8 MB (compressed)
```

### Compressed Backup

```bash
# Create compressed backup
tsk db backup --compress backup.sql.gz
```

### Specific Tables

```bash
# Backup only specific tables
tsk db backup --tables users,posts backup_users_posts.sql

# Exclude specific tables
tsk db backup --exclude logs,temp_data backup_excluding_logs.sql
```

### Data Only Backup

```bash
# Backup data without schema
tsk db backup --data-only data_backup.sql
```

### Schema Only Backup

```bash
# Backup schema without data
tsk db backup --schema-only schema_backup.sql
```

### Scheduled Backup

```bash
# Create scheduled backup
tsk db backup --scheduled daily_backup.sql
```

**Output:**
```
🔄 Creating scheduled backup...
📅 Schedule: daily at 02:00
📁 Output directory: /backups/daily/
📊 Retention: 7 days
✅ Scheduled backup created successfully
```

### Verbose Output

```bash
# Show detailed backup progress
tsk db backup --verbose backup.sql
```

**Output:**
```
🔄 Starting database backup...
📍 Database: myapp
👤 User: postgres
📁 Output file: backup.sql

📋 Backing up schema...
  ✅ Table: users
  ✅ Table: posts
  ✅ Table: comments
  ✅ Indexes: 25
  ✅ Constraints: 12

📋 Backing up data...
  📊 Table: users (1,234 rows)
  📊 Table: posts (5,678 rows)
  📊 Table: comments (12,345 rows)

📋 Compressing backup...
  📊 Original size: 45.2 MB
  📊 Compressed size: 12.8 MB
  📊 Compression ratio: 71.7%

✅ Backup completed successfully
⏱️ Total duration: 12.3s
💾 Final file size: 12.8 MB
```

### JSON Output

```bash
# Get backup results in JSON format
tsk db backup --json backup.sql
```

**Output:**
```json
{
  "status": "success",
  "backup_file": "backup.sql",
  "database": "myapp",
  "duration_ms": 12300,
  "file_size_bytes": 13421772,
  "compression_ratio": 71.7,
  "tables_backed_up": 15,
  "total_rows": 19257,
  "schema_objects": {
    "tables": 15,
    "indexes": 25,
    "constraints": 12,
    "functions": 3,
    "triggers": 8
  }
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
    
    // Create backup
    result, err := dbClient.Backup("backup.sql", &cli.BackupOptions{
        Format:    "sql",
        Compress:  true,
        DataOnly:  false,
        Verbose:   true,
    })
    if err != nil {
        log.Fatal(err)
    }
    
    // Print results
    fmt.Printf("Backup completed in %v\n", result.Duration)
    fmt.Printf("File size: %d bytes\n", result.FileSize)
    fmt.Printf("Tables backed up: %d\n", result.TablesBackedUp)
}
```

## Backup Formats

### SQL Format (Default)

```bash
tsk db backup --format sql backup.sql
```

Creates a standard SQL dump file with CREATE and INSERT statements.

### Custom Format

```bash
tsk db backup --format custom backup.backup
```

Creates a PostgreSQL custom format file (faster restore, smaller size).

### Directory Format

```bash
tsk db backup --format directory backup_dir/
```

Creates a directory with separate files for each table.

## Output

### Success Output

When backup completes successfully:

- Backup status with ✅ symbol
- Database name and output file
- Number of tables backed up
- File size and compression ratio
- Execution duration

### Error Output

When backup fails:

- Error status with ❌ symbol
- Detailed error message
- Failed operation details
- Recovery suggestions

**Example:**
```
❌ Backup failed
📍 Database: myapp
💬 Error: insufficient disk space
📊 Required space: 50 MB
📊 Available space: 10 MB
💡 Suggestion: Free up disk space or use compression
```

## Exit Codes

| Code | Description |
|------|-------------|
| 0 | Success - Backup completed |
| 1 | Error - Backup failed |
| 2 | Error - Insufficient disk space |
| 3 | Error - Database connection failed |
| 4 | Error - Invalid backup format |

## Backup Best Practices

### 1. Regular Backups

```bash
# Daily backup
tsk db backup --scheduled daily_backup.sql

# Weekly backup
tsk db backup --scheduled weekly_backup.sql

# Monthly backup
tsk db backup --scheduled monthly_backup.sql
```

### 2. Backup Strategy

- **Full backups**: Daily for small databases
- **Incremental backups**: For large databases
- **Point-in-time recovery**: Use WAL archiving
- **Off-site storage**: Store backups remotely

### 3. Verification

```bash
# Verify backup integrity
tsk db backup --verify backup.sql

# Test restore on staging
tsk db restore backup.sql --test
```

### 4. Automation

```bash
#!/bin/bash
# Backup script example

# Create backup
tsk db backup daily_backup_$(date +%Y%m%d).sql

# Compress old backups
find /backups -name "*.sql" -mtime +7 -exec gzip {} \;

# Clean up old backups
find /backups -name "*.sql.gz" -mtime +30 -delete
```

## Related Commands

- [tsk db status](./status.md) - Check database status
- [tsk db restore](./restore.md) - Restore from backup
- [tsk db migrate](./migrate.md) - Run migrations
- [tsk db init](./init.md) - Initialize database

## Notes

- **Compression**: Default compression reduces file size by ~70%
- **Scheduling**: Use cron or systemd for automated backups
- **Verification**: Always verify backup integrity
- **Storage**: Consider off-site backup storage
- **Retention**: Implement backup retention policies

## See Also

- [Database Commands Overview](./README.md)
- [Configuration Guide](../../../go/docs/PNT_GUIDE.md)
- [Examples](../../examples/database-backup.md) 