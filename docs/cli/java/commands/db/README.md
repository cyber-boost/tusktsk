# Database Commands

Database operations for TuskLang Java CLI, providing comprehensive database management capabilities.

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

- **Development Setup**: Initialize local SQLite databases for development
- **Migration Management**: Apply schema changes across environments
- **Database Monitoring**: Check connection health and performance
- **Backup Operations**: Create and restore database backups
- **Interactive Debugging**: Use console for direct database queries

## Java-Specific Notes

### Database Drivers

The Java CLI supports multiple database drivers:

- **SQLite**: Built-in support, no additional drivers needed
- **PostgreSQL**: Requires `postgresql-jdbc` dependency
- **MySQL**: Requires `mysql-connector-java` dependency
- **H2**: Built-in support for embedded databases

### Connection Pooling

Java CLI uses HikariCP for connection pooling:

```properties
# Database connection pool settings
db.pool.maximum-pool-size=10
db.pool.minimum-idle=2
db.pool.connection-timeout=30000
db.pool.idle-timeout=600000
```

### Transaction Management

All database operations support transactions:

```java
// Automatic transaction handling
tsk db migrate schema.sql

// Manual transaction control
tsk db console
> BEGIN;
> INSERT INTO users (name) VALUES ('John');
> COMMIT;
```

### Error Handling

Database commands provide detailed error information:

```bash
# Verbose error output
tsk db status --verbose

# JSON error format
tsk db migrate schema.sql --json
```

## Examples

### Complete Database Workflow

```bash
# 1. Initialize database
tsk db init

# 2. Check status
tsk db status

# 3. Run migrations
tsk db migrate migrations/001_create_users.sql
tsk db migrate migrations/002_add_indexes.sql

# 4. Verify schema
tsk db console
> .schema users
> SELECT COUNT(*) FROM users;

# 5. Create backup
tsk db backup backup_$(date +%Y%m%d).sql

# 6. Monitor performance
tsk db status --performance
```

### Spring Boot Integration

```java
@Configuration
public class DatabaseConfig {
    
    @Bean
    public DataSource dataSource() {
        // TuskLang CLI manages database connections
        return new HikariDataSource();
    }
    
    @Bean
    public JdbcTemplate jdbcTemplate(DataSource dataSource) {
        return new JdbcTemplate(dataSource);
    }
}
```

### Migration Management

```bash
# Create migration file
cat > migrations/001_create_users.sql << EOF
CREATE TABLE users (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    email TEXT UNIQUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
EOF

# Apply migration
tsk db migrate migrations/001_create_users.sql

# Rollback if needed
tsk db restore backup_before_migration.sql
```

## Configuration

### Database Configuration File

Create `database.peanuts`:

```ini
[database]
driver: "sqlite"
url: "jdbc:sqlite:app.db"
username: ""
password: ""

[pool]
max_size: 10
min_idle: 2
connection_timeout: 30000
idle_timeout: 600000

[migrations]
directory: "migrations"
table: "schema_migrations"
```

### Environment-Specific Settings

```ini
# development.peanuts
[database]
url: "jdbc:sqlite:dev.db"

# production.peanuts  
[database]
driver: "postgresql"
url: "jdbc:postgresql://prod-db:5432/app"
username: "${DB_USER}"
password: "${DB_PASS}"
```

## Performance Tips

1. **Connection Pooling**: Use appropriate pool sizes for your workload
2. **Indexes**: Create indexes on frequently queried columns
3. **Batch Operations**: Use batch inserts for large datasets
4. **Query Optimization**: Monitor slow queries with `tsk db status --performance`
5. **Regular Backups**: Schedule automated backups with cron

## Troubleshooting

### Common Issues

#### Connection Refused
```bash
# Check database service
sudo systemctl status postgresql

# Verify connection string
tsk db status --verbose
```

#### Migration Failures
```bash
# Check migration status
tsk db console
> SELECT * FROM schema_migrations;

# Rollback and retry
tsk db restore backup.sql
tsk db migrate migrations/ --force
```

#### Performance Issues
```bash
# Analyze query performance
tsk db console
> EXPLAIN QUERY PLAN SELECT * FROM users WHERE email = 'test@example.com';

# Check database size
tsk db status --size
```

## Related Commands

- [Configuration Commands](../config/README.md) - Manage database configuration
- [Service Commands](../services/README.md) - Start/stop database services
- [Cache Commands](../cache/README.md) - Database query caching 