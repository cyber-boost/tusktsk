# tsk db restore

Restore database from backup files.

## Synopsis

```bash
tsk db restore [OPTIONS] <file>
```

## Description

The `tsk db restore` command restores a database from backup files created by `tsk db backup`. It supports various backup formats and provides options for selective restoration.

## Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| --help | -h | Show help for this command | - |
| --database | -d | Target database name | - |
| --clean | -c | Drop existing objects before restore | false |
| --data-only | -a | Restore data only (no schema) | false |
| --schema-only | -s | Restore schema only (no data) | false |
| --tables | -t | Restore specific tables only | - |
| --exclude | -e | Exclude specific tables | - |
| --single-transaction | | Use single transaction | true |
| --json | -j | Output in JSON format | false |
| --quiet | -q | Suppress output, only return exit code | false |
| --verbose | -v | Show verbose output | false |

## Arguments

| Argument | Required | Description |
|----------|----------|-------------|
| file | Yes | Backup file path to restore from |

## Examples

### Basic Restore

```bash
# Restore from backup file
tsk db restore backup.sql

# Restore to specific database
tsk db restore --database myapp_new backup.sql
```

**Output:**
```
🔄 Restoring database from backup...
📍 Source file: backup.sql
📊 Target database: myapp
📊 Tables: 15
📊 Data size: 45.2 MB
⏱️ Duration: 25.7s
✅ Restore completed successfully
```

### Clean Restore

```bash
# Drop existing objects before restore
tsk db restore --clean backup.sql
```

### Selective Restore

```bash
# Restore specific tables only
tsk db restore --tables users,posts backup.sql

# Exclude specific tables
tsk db restore --exclude logs,temp_data backup.sql
```

### Schema Only Restore

```bash
# Restore schema without data
tsk db restore --schema-only backup.sql
```

### Data Only Restore

```bash
# Restore data to existing schema
tsk db restore --data-only backup.sql
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
    
    // Restore from backup
    result, err := dbClient.Restore("backup.sql", &cli.RestoreOptions{
        Database: "myapp",
        Clean:    false,
        DataOnly: false,
    })
    if err != nil {
        log.Fatal(err)
    }
    
    // Print results
    fmt.Printf("Restore completed in %v\n", result.Duration)
    fmt.Printf("Tables restored: %d\n", result.TablesRestored)
}
```

## Related Commands

- [tsk db backup](./backup.md) - Create database backup
- [tsk db status](./status.md) - Check database status
- [tsk db migrate](./migrate.md) - Run migrations 