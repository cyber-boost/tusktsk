# Database Commands

Database operations for TuskLang Bash CLI.

## Available Commands

| Command | Description |
|---------|-------------|
| [tsk db status](./status.md) | Check database connection status |
| [tsk db migrate](./migrate.md) | Run migration file |
| [tsk db console](./console.md) | Open interactive database console |
| [tsk db backup](./backup.md) | Backup database |
| [tsk db restore](./restore.md) | Restore from backup file |
| [tsk db init](./init.md) | Initialize SQLite database |

## Common Use Cases

- **Development Setup**: Initialize and configure databases for development
- **Migration Management**: Apply schema changes and data migrations
- **Backup and Recovery**: Create and restore database backups
- **Connection Testing**: Verify database connectivity and health
- **Interactive Development**: Use database console for debugging

## Bash-Specific Notes

### Database Drivers

The Bash CLI supports multiple database drivers:

- **SQLite**: Built-in support, no additional dependencies
- **PostgreSQL**: Requires `psql` client
- **MySQL**: Requires `mysql` client
- **SQL Server**: Requires `sqlcmd` client

### Configuration

Database connections are configured through Peanut Configuration:

```bash
# Database configuration in peanu.peanuts
[database]
type: "sqlite"
path: "./data/app.db"

# Or for PostgreSQL
[database]
type: "postgresql"
host: "localhost"
port: 5432
name: "myapp"
user: "postgres"
password: "secret"
```

### Environment Variables

Set database credentials via environment variables:

```bash
# SQLite
export DB_PATH="./data/app.db"

# PostgreSQL
export DB_HOST="localhost"
export DB_PORT="5432"
export DB_NAME="myapp"
export DB_USER="postgres"
export DB_PASSWORD="secret"
```

### Error Handling

Database commands include comprehensive error handling:

```bash
# Check if database is accessible
if tsk db status --quiet; then
    echo "Database is ready"
else
    echo "Database connection failed"
    exit 1
fi
```

### Integration Examples

#### Development Script

```bash
#!/bin/bash
# dev-setup.sh

echo "Setting up development database..."

# Initialize database
tsk db init

# Run migrations
for migration in migrations/*.sql; do
    echo "Running migration: $migration"
    tsk db migrate "$migration"
done

echo "Database setup complete"
```

#### Backup Script

```bash
#!/bin/bash
# backup.sh

# Create backup with timestamp
BACKUP_FILE="backup-$(date +%Y%m%d-%H%M%S).sql"
tsk db backup "$BACKUP_FILE"

echo "Backup created: $BACKUP_FILE"
```

#### Health Check

```bash
#!/bin/bash
# health-check.sh

# Check database status
if tsk db status --json | jq -e '.status == "connected"'; then
    echo "✅ Database is healthy"
    exit 0
else
    echo "❌ Database is unhealthy"
    exit 1
fi
```

## Best Practices

1. **Always validate connections** before running migrations
2. **Use transactions** for data integrity
3. **Backup before migrations** in production
4. **Test migrations** in development first
5. **Use environment-specific configurations**

## Troubleshooting

### Common Issues

#### Connection Refused

```bash
# Check if database service is running
sudo systemctl status postgresql

# Check port availability
netstat -tlnp | grep 5432
```

#### Permission Denied

```bash
# Check file permissions for SQLite
ls -la data/app.db

# Check user permissions for PostgreSQL
sudo -u postgres psql -c "\du"
```

#### Migration Errors

```bash
# Check migration syntax
tsk db migrate migration.sql --dry-run

# Rollback if needed
tsk db restore backup.sql
```

## Related Commands

- [Configuration Commands](../config/README.md) - Database configuration
- [Service Commands](../services/README.md) - Database service management
- [Utility Commands](../utility/README.md) - General utilities 