# tsk db status

Check database connection status and health.

## Synopsis

```bash
tsk db status [OPTIONS]
```

## Description

The `tsk db status` command checks the connection status and health of the configured database. It verifies connectivity, authentication, and basic database operations.

This command is useful for:
- Verifying database connectivity before running migrations
- Health checks in deployment scripts
- Troubleshooting database connection issues
- Monitoring database availability

## Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| --help | -h | Show help for this command | - |
| --json | -j | Output in JSON format | false |
| --verbose | -v | Enable verbose output | false |
| --quiet | -q | Suppress non-error output | false |
| --timeout | -t | Connection timeout in seconds | 30 |

## Arguments

| Argument | Required | Description |
|----------|----------|-------------|
| None | No | No arguments required |

## Examples

### Basic Usage
```bash
# Check database status
tsk db status
```

Expected output:
```
✅ Database connection successful
Host: localhost
Port: 5432
Database: myapp
Version: PostgreSQL 13.4
```

### JSON Output
```bash
# Get status in JSON format
tsk db status --json
```

Expected output:
```json
{
  "status": "connected",
  "host": "localhost",
  "port": 5432,
  "database": "myapp",
  "version": "PostgreSQL 13.4",
  "uptime": "2 days, 3 hours",
  "connections": 15,
  "timestamp": "2024-12-19T10:30:00Z"
}
```

### Verbose Output
```bash
# Get detailed status information
tsk db status --verbose
```

Expected output:
```
🔍 Checking database connection...
📍 Connecting to localhost:5432
✅ Connection established
🔍 Verifying authentication...
✅ Authentication successful
🔍 Testing basic operations...
✅ SELECT query successful
✅ INSERT query successful
✅ UPDATE query successful
✅ DELETE query successful
📊 Database Statistics:
   - Active connections: 15
   - Database size: 256 MB
   - Cache hit ratio: 98.5%
   - Uptime: 2 days, 3 hours
✅ Database is healthy and ready
```

### With Timeout
```bash
# Set custom timeout
tsk db status --timeout 10
```

## Bash API Usage

```bash
#!/bin/bash
# check_db_health.sh

# Check database status
if tsk db status --quiet; then
    echo "Database is healthy"
    exit 0
else
    echo "Database connection failed"
    exit 1
fi
```

### Integration with Scripts

```bash
#!/bin/bash
# deploy.sh

echo "Checking database health..."

# Check database status
if ! tsk db status --quiet; then
    echo "❌ Database is not ready for deployment"
    exit 1
fi

echo "✅ Database is ready"
echo "Proceeding with deployment..."
```

### Health Check Function

```bash
#!/bin/bash
# health_check.sh

check_database_health() {
    local timeout="${1:-30}"
    
    echo "Checking database health..."
    
    if tsk db status --timeout "$timeout" --quiet; then
        echo "✅ Database is healthy"
        return 0
    else
        echo "❌ Database health check failed"
        return 1
    fi
}

# Usage
check_database_health 15
```

## Output

### Success Output

When the database is accessible and healthy:

```
✅ Database connection successful
Host: localhost
Port: 5432
Database: myapp
Version: PostgreSQL 13.4
Uptime: 2 days, 3 hours
Active connections: 15
```

### Error Output

When the database is not accessible:

```
❌ Database connection failed
Error: connection refused
Host: localhost
Port: 5432
Database: myapp
```

### JSON Output Format

```json
{
  "status": "connected|disconnected|error",
  "host": "string",
  "port": "number",
  "database": "string",
  "version": "string",
  "uptime": "string",
  "connections": "number",
  "error": "string (if status is error)",
  "timestamp": "ISO 8601 timestamp"
}
```

## Exit Codes

| Code | Description |
|------|-------------|
| 0 | Database is connected and healthy |
| 1 | Database connection failed |
| 2 | Invalid configuration |
| 3 | Timeout exceeded |
| 4 | Authentication failed |

## Related Commands

- [tsk db migrate](./migrate.md) - Run database migrations
- [tsk db console](./console.md) - Open interactive database console
- [tsk db backup](./backup.md) - Backup database
- [tsk db restore](./restore.md) - Restore from backup
- [tsk db init](./init.md) - Initialize database

## Notes

- The command uses the database configuration from your `peanu.peanuts` or `peanu.pnt` file
- Connection timeout defaults to 30 seconds but can be customized
- JSON output is useful for automation and monitoring scripts
- The command performs basic read/write tests to verify full functionality
- Database statistics are included when available

## See Also

- [Database Commands Overview](./README.md)
- [Configuration Commands](../config/README.md)
- [Troubleshooting](../../troubleshooting.md) 