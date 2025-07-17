# Database Commands - PHP CLI

Database operations and management commands for TuskLang PHP CLI.

## Available Commands

| Command | Description |
|---------|-------------|
| [tsk db status](./status.md) | Check database connection and status |
| [tsk db migrate](./migrate.md) | Run database migrations |
| [tsk db console](./console.md) | Open database console |
| [tsk db backup](./backup.md) | Create database backup |
| [tsk db restore](./restore.md) | Restore database from backup |
| [tsk db init](./init.md) | Initialize database schema |

## Common Use Cases

- **Development Setup** - Initialize and migrate databases for development
- **Production Deployment** - Run migrations and verify database connectivity
- **Backup Management** - Create and restore database backups
- **Troubleshooting** - Check database status and connectivity issues
- **Maintenance** - Perform database maintenance tasks

## PHP-Specific Notes

### PDO Integration

The database commands use PHP's PDO extension for database operations:

```php
<?php
// Example database connection
try {
    $pdo = new PDO(
        "mysql:host=localhost;dbname=myapp;charset=utf8mb4",
        "username",
        "password",
        [
            PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION,
            PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC,
            PDO::ATTR_EMULATE_PREPARES => false,
        ]
    );
} catch (PDOException $e) {
    throw new DatabaseException("Connection failed: " . $e->getMessage());
}
```

### Supported Databases

- **MySQL/MariaDB** - Full support with PDO MySQL driver
- **PostgreSQL** - Full support with PDO PostgreSQL driver
- **SQLite** - Full support with PDO SQLite driver
- **SQL Server** - Basic support with PDO SQL Server driver

### Configuration

Database connections are configured through TuskLang configuration files:

```ini
[database]
driver: "mysql"
host: "localhost"
port: 3306
name: "myapp"
user: "app_user"
password: "secure_password"
charset: "utf8mb4"
options: {
    "ATTR_ERRMODE": "PDO::ERRMODE_EXCEPTION",
    "ATTR_DEFAULT_FETCH_MODE": "PDO::FETCH_ASSOC"
}
```

## Examples

### Basic Database Operations

```bash
# Check database status
tsk db status

# Run migrations
tsk db migrate

# Open database console
tsk db console

# Create backup
tsk db backup myapp_backup.sql

# Restore from backup
tsk db restore myapp_backup.sql
```

### Development Workflow

```bash
# 1. Initialize database
tsk db init

# 2. Run migrations
tsk db migrate

# 3. Verify status
tsk db status

# 4. Open console for testing
tsk db console
```

### Production Deployment

```bash
# 1. Check database connectivity
tsk db status

# 2. Run migrations safely
tsk db migrate --dry-run
tsk db migrate

# 3. Create backup before changes
tsk db backup pre_deployment.sql

# 4. Verify deployment
tsk db status
```

## Error Handling

Database commands provide detailed error information:

```bash
# Check for specific errors
tsk db status --verbose

# Debug connection issues
tsk --debug db status

# Get JSON output for programmatic use
tsk db status --json
```

## Related Commands

- [Configuration Commands](../config/README.md) - Database configuration management
- [Service Commands](../services/README.md) - Database service management
- [Cache Commands](../cache/README.md) - Database cache operations

## See Also

- [Database Configuration Guide](../config/README.md)
- [Service Management](../services/README.md)
- [Troubleshooting Guide](../../troubleshooting.md)

**Strong. Secure. Scalable.** 🐘 