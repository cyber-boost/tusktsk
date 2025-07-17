# tsk db backup

Create database backup for data protection and recovery.

## Synopsis

```bash
tsk db backup [file] [OPTIONS]
```

## Description

The `tsk db backup` command creates a complete backup of your database, including schema and data. This is essential for data protection, disaster recovery, and migration purposes.

## Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| --help | -h | Show help for this command | - |
| --json | -j | Output in JSON format | false |
| --verbose | -v | Enable verbose output | false |
| --quiet | -q | Suppress non-error output | false |
| --compress | -c | Compress backup file | false |
| --format | -f | Backup format (sql, custom, directory) | sql |

## Examples

### Basic Backup
```bash
# Create backup with auto-generated filename
tsk db backup
```

Expected output:
```
🔄 Creating database backup...
📍 Database: myapp
📍 Format: SQL
✅ Backup completed: backup-20241219-103000.sql
📊 Size: 2.4 MB
⏱️  Duration: 1.2s
```

### Named Backup
```bash
# Create backup with specific filename
tsk db backup myapp-backup.sql
```

### Compressed Backup
```bash
# Create compressed backup
tsk db backup --compress
```

Expected output:
```
🔄 Creating compressed database backup...
✅ Backup completed: backup-20241219-103000.sql.gz
📊 Size: 856 KB (compressed from 2.4 MB)
⏱️  Duration: 1.8s
```

## Related Commands

- [tsk db status](./status.md) - Check database status
- [tsk db restore](./restore.md) - Restore from backup
- [tsk db migrate](./migrate.md) - Run migrations 