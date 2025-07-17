# tsk db status

Check database connection status and health information.

## Synopsis

```bash
tsk db status [OPTIONS]
```

## Description

The `tsk db status` command checks the connection to your configured database and provides detailed health information including:

- Connection status and response time
- Database version and capabilities
- Connection pool statistics
- Active connections and queries
- Database size and performance metrics
- Configuration validation

This command is essential for monitoring database health during development and production operations.

## Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| --help | -h | Show help for this command | - |
| --detailed | -d | Show detailed status information | false |
| --timeout | -t | Connection timeout in seconds | 30 |
| --json | -j | Output in JSON format | false |
| --quiet | -q | Suppress output, only return exit code | false |
| --verbose | -v | Show verbose output | false |

## Arguments

This command does not accept any arguments.

## Examples

### Basic Usage

```bash
# Check database status
tsk db status
```

**Output:**
```
✅ Database connection successful
📍 Host: localhost:5432
📊 Database: myapp
🔄 Response time: 2.3ms
📈 Active connections: 3/25
💾 Database size: 45.2 MB
```

### Detailed Status

```bash
# Get detailed status information
tsk db status --detailed
```

**Output:**
```
✅ Database connection successful

Connection Details:
📍 Host: localhost:5432
📊 Database: myapp
👤 User: postgres
🔄 Response time: 2.3ms
🔒 SSL: disabled

Pool Statistics:
📈 Active connections: 3/25
⏳ Idle connections: 2
🔄 Wait count: 0
⏱️ Wait duration: 0ms

Database Information:
🐘 Version: PostgreSQL 14.5
💾 Size: 45.2 MB
📊 Tables: 23
🔍 Indexes: 45
👥 Users: 5

Performance Metrics:
⚡ Query cache hit ratio: 98.5%
📊 Buffer cache hit ratio: 95.2%
🔄 Transaction rate: 125/sec
⏱️ Average query time: 1.2ms
```

### JSON Output

```bash
# Get status in JSON format
tsk db status --json
```

**Output:**
```json
{
  "status": "connected",
  "connection": {
    "host": "localhost",
    "port": 5432,
    "database": "myapp",
    "user": "postgres",
    "ssl_mode": "disable",
    "response_time_ms": 2.3
  },
  "pool": {
    "active_connections": 3,
    "max_connections": 25,
    "idle_connections": 2,
    "wait_count": 0,
    "wait_duration_ms": 0
  },
  "database": {
    "version": "PostgreSQL 14.5",
    "size_mb": 45.2,
    "tables": 23,
    "indexes": 45,
    "users": 5
  },
  "performance": {
    "query_cache_hit_ratio": 98.5,
    "buffer_cache_hit_ratio": 95.2,
    "transaction_rate_per_sec": 125,
    "average_query_time_ms": 1.2
  }
}
```

### Quiet Mode

```bash
# Check status without output (useful in scripts)
tsk db status --quiet
echo $?  # 0 for success, 1 for failure
```

### Verbose Mode

```bash
# Show verbose output with connection details
tsk db status --verbose
```

**Output:**
```
🔍 Checking database connection...
📍 Connecting to localhost:5432/myapp
👤 Authenticating as postgres...
✅ Connection established successfully
📊 Retrieving database statistics...
📈 Collecting performance metrics...
✅ Database status check completed

✅ Database connection successful
📍 Host: localhost:5432
📊 Database: myapp
🔄 Response time: 2.3ms
📈 Active connections: 3/25
💾 Database size: 45.2 MB
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
    
    // Check database status
    status, err := dbClient.Status()
    if err != nil {
        log.Fatal(err)
    }
    
    // Print status information
    fmt.Printf("Database: %s\n", status.Database)
    fmt.Printf("Status: %s\n", status.Status)
    fmt.Printf("Response Time: %v\n", status.ResponseTime)
    fmt.Printf("Active Connections: %d/%d\n", 
        status.Pool.ActiveConnections, status.Pool.MaxConnections)
}
```

## Output

### Success Output

When the database connection is successful, the command displays:

- Connection status with ✅ symbol
- Database host and port
- Database name
- Response time
- Connection pool statistics
- Database size and basic metrics

### Error Output

When the database connection fails, the command displays:

- Error status with ❌ symbol
- Detailed error message
- Connection attempt details
- Troubleshooting suggestions

**Example:**
```
❌ Database connection failed
📍 Host: localhost:5432
📊 Database: myapp
💬 Error: connection refused
💡 Suggestion: Check if database server is running
```

## Exit Codes

| Code | Description |
|------|-------------|
| 0 | Success - Database connection established |
| 1 | Error - Database connection failed |
| 2 | Error - Configuration invalid |
| 3 | Error - Timeout exceeded |

## Related Commands

- [tsk db init](./init.md) - Initialize database
- [tsk db console](./console.md) - Interactive database console
- [tsk db backup](./backup.md) - Create database backup
- [tsk config check](../config/check.md) - Validate configuration

## Notes

- **Configuration**: Uses database settings from `peanu.peanuts` file
- **Performance**: Non-blocking operation with configurable timeout
- **Security**: Respects SSL settings and connection parameters
- **Monitoring**: Can be used in health check scripts and monitoring systems

## See Also

- [Database Commands Overview](./README.md)
- [Configuration Guide](../../../go/docs/PNT_GUIDE.md)
- [Examples](../../examples/database-operations.md) 