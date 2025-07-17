# tsk db init

Initialize SQLite database for development and testing.

## Synopsis

```bash
tsk db init [OPTIONS]
```

## Description

The `tsk db init` command initializes a new SQLite database for development and testing purposes. It creates the database file and sets up basic schema if specified.

## Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| --help | -h | Show help for this command | - |
| --json | -j | Output in JSON format | false |
| --verbose | -v | Enable verbose output | false |
| --quiet | -q | Suppress non-error output | false |
| --schema | -s | Path to initial schema file | - |
| --force | -f | Overwrite existing database | false |

## Examples

### Basic Initialization
```bash
# Initialize SQLite database
tsk db init
```

Expected output:
```
🔄 Initializing SQLite database...
📍 Database: ./data/app.db
✅ Database initialized successfully
📊 Size: 28 KB
⏱️  Duration: 0.1s
```

### With Schema
```bash
# Initialize with initial schema
tsk db init --schema schema.sql
```

### Force Initialization
```bash
# Overwrite existing database
tsk db init --force
```

## Related Commands

- [tsk db status](./status.md) - Check database status
- [tsk db migrate](./migrate.md) - Run migrations
- [tsk db backup](./backup.md) - Backup database 