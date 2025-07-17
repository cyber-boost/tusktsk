# Database Commands

Database operations for TuskLang Go CLI, providing comprehensive database management capabilities.

## Available Commands

| Command | Description |
|---------|-------------|
| [tsk db status](./status.md) | Check database connection and status |
| [tsk db migrate](./migrate.md) | Run database migration files |
| [tsk db console](./console.md) | Interactive database console |
| [tsk db backup](./backup.md) | Backup database to file |
| [tsk db restore](./restore.md) | Restore database from backup |
| [tsk db init](./init.md) | Initialize SQLite database |

## Common Use Cases

- **Development Setup**: Initialize and configure local databases
- **Migration Management**: Apply schema changes across environments
- **Backup and Recovery**: Protect data with automated backups
- **Database Monitoring**: Check connection health and performance
- **Interactive Development**: Use console for quick queries and testing

## Go-Specific Notes

The Go CLI database commands leverage Go's excellent database/sql package and provide:

- **Connection Pooling**: Efficient database connection management
- **Transaction Support**: ACID-compliant transaction handling
- **Prepared Statements**: SQL injection protection
- **Context Support**: Timeout and cancellation handling
- **Multiple Driver Support**: PostgreSQL, MySQL, SQLite, and more

## Examples

### Basic Database Operations

```bash
# Check database status
tsk db status

# Initialize SQLite database
tsk db init

# Run migration
tsk db migrate schema.sql

# Create backup
tsk db backup myapp_backup.sql
```

### Development Workflow

```bash
# Start development with fresh database
tsk db init
tsk db migrate migrations/*.sql

# Check status during development
tsk db status

# Use console for testing
tsk db console
```

### Production Operations

```bash
# Create scheduled backup
tsk db backup --scheduled daily_backup.sql

# Restore from backup
tsk db restore production_backup.sql

# Monitor database health
tsk db status --detailed
```

## Configuration

Database commands use configuration from your `peanu.peanuts` file:

```ini
[database]
host: "localhost"
port: 5432
name: "myapp"
user: "postgres"
password: "secret"
ssl_mode: "disable"
max_connections: 25
```

## Error Handling

The Go CLI provides comprehensive error handling for database operations:

- **Connection Errors**: Automatic retry with exponential backoff
- **Migration Errors**: Rollback support and detailed error reporting
- **Backup Errors**: Integrity verification and recovery options
- **Permission Errors**: Clear guidance on required privileges

## Performance Considerations

- **Connection Pooling**: Efficiently manages database connections
- **Batch Operations**: Optimized for large datasets
- **Compression**: Automatic backup compression to save space
- **Parallel Processing**: Concurrent operations where supported 