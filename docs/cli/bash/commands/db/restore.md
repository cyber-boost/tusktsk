# tsk db restore

Restore database from backup file.

## Synopsis

```bash
tsk db restore <file> [OPTIONS]
```

## Description

The `tsk db restore` command restores your database from a previously created backup file. This is used for disaster recovery, data migration, or reverting to a previous state.

## Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| --help | -h | Show help for this command | - |
| --json | -j | Output in JSON format | false |
| --verbose | -v | Enable verbose output | false |
| --quiet | -q | Suppress non-error output | false |
| --force | -f | Force restore (overwrite existing data) | false |
| --dry-run | -d | Preview restore without executing | false |

## Examples

### Basic Restore
```bash
# Restore from backup file
tsk db restore backup-20241219-103000.sql
```

Expected output:
```
🔄 Restoring database from backup...
📍 File: backup-20241219-103000.sql
📍 Database: myapp
✅ Restore completed successfully
📊 Tables restored: 15
📊 Records restored: 1,234
⏱️  Duration: 3.2s
```

### Force Restore
```bash
# Force restore (overwrite existing data)
tsk db restore backup.sql --force
```

### Dry Run
```bash
# Preview restore without executing
tsk db restore backup.sql --dry-run
```

## Related Commands

- [tsk db status](./status.md) - Check database status
- [tsk db backup](./backup.md) - Create backup
- [tsk db migrate](./migrate.md) - Run migrations 