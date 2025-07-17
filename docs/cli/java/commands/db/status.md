# tsk db status

Check database connection status and health information.

## Synopsis

```bash
tsk db status [OPTIONS]
```

## Description

The `tsk db status` command checks the health and status of the configured database connection. It provides information about connection status, database version, performance metrics, and configuration details.

## Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| --help | -h | Show help for this command | - |
| --verbose | -v | Show detailed connection information | false |
| --json | -j | Output results in JSON format | false |
| --performance | -p | Show performance metrics | false |
| --size | -s | Show database size information | false |
| --timeout | -t | Connection timeout in seconds | 30 |

## Arguments

| Argument | Required | Description |
|----------|----------|-------------|
| None | No | No arguments required |

## Examples

### Basic Usage
```bash
# Check basic database status
tsk db status
```

**Output:**
```
🗄️ Database Status Check
✅ Connected to SQLite database: app.db
📊 Version: SQLite 3.36.0
🔗 Connection Pool: 2/10 active
⏱️ Response Time: 12ms
📈 Status: Healthy
```

### Verbose Output
```bash
# Show detailed connection information
tsk db status --verbose
```

**Output:**
```
🗄️ Database Status Check (Verbose)
✅ Connected to SQLite database: app.db
📊 Version: SQLite 3.36.0
🔗 Connection Pool: 2/10 active (2 idle, 6 available)
⏱️ Response Time: 12ms
📈 Status: Healthy
🔧 Configuration:
   Driver: org.sqlite.JDBC
   URL: jdbc:sqlite:app.db
   Auto-commit: true
   Read-only: false
   Transaction isolation: TRANSACTION_SERIALIZABLE
📊 Performance Metrics:
   Active connections: 2
   Idle connections: 2
   Total connections: 4
   Connection wait time: 0ms
   Query execution time: 12ms avg
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
  "database": {
    "type": "sqlite",
    "name": "app.db",
    "version": "3.36.0"
  },
  "connection": {
    "pool": {
      "active": 2,
      "idle": 2,
      "total": 4,
      "max": 10
    },
    "response_time_ms": 12
  },
  "performance": {
    "queries_per_second": 45.2,
    "average_query_time_ms": 12,
    "slow_queries": 0
  },
  "timestamp": "2024-12-19T10:30:00Z"
}
```

### Performance Metrics
```bash
# Show performance information
tsk db status --performance
```

**Output:**
```
🗄️ Database Performance Metrics
📊 Query Statistics:
   Total queries: 1,234
   Queries per second: 45.2
   Average response time: 12ms
   Slow queries (>100ms): 0
   Failed queries: 0

🔗 Connection Pool:
   Active connections: 2
   Idle connections: 2
   Connection wait time: 0ms
   Connection timeout: 30s

📈 Performance Indicators:
   CPU usage: 2.3%
   Memory usage: 15.6MB
   Disk I/O: 2.1MB/s
   Cache hit ratio: 94.2%
```

### Database Size Information
```bash
# Show database size details
tsk db status --size
```

**Output:**
```
🗄️ Database Size Information
📁 Database file: app.db
📊 Total size: 2.4MB
📈 Table sizes:
   users: 856KB (1,234 rows)
   posts: 1.2MB (5,678 rows)
   comments: 324KB (12,345 rows)
   sessions: 12KB (89 rows)

🗂️ Index sizes:
   idx_users_email: 48KB
   idx_posts_created: 156KB
   idx_comments_post_id: 89KB

💾 Storage efficiency: 87.3%
```

## Java API Usage

```java
// Programmatic database status check
import org.tusklang.cli.DatabaseCommands;

public class DatabaseMonitor {
    
    public void checkDatabaseStatus() {
        DatabaseCommands db = new DatabaseCommands();
        
        // Basic status check
        DatabaseStatus status = db.status();
        System.out.println("Database: " + status.isConnected());
        
        // Verbose status with performance
        DatabaseStatus verbose = db.status(true, true);
        System.out.println("Response time: " + verbose.getResponseTimeMs() + "ms");
        
        // JSON output
        String jsonStatus = db.statusJson();
        System.out.println(jsonStatus);
    }
    
    public void monitorPerformance() {
        DatabaseCommands db = new DatabaseCommands();
        
        // Get performance metrics
        PerformanceMetrics metrics = db.getPerformanceMetrics();
        System.out.println("Queries/sec: " + metrics.getQueriesPerSecond());
        System.out.println("Avg response time: " + metrics.getAverageResponseTimeMs() + "ms");
    }
}
```

## Output

### Success Output

The command returns detailed database status information including:

- **Connection Status**: Whether the database is accessible
- **Database Information**: Type, name, and version
- **Connection Pool**: Active, idle, and total connections
- **Performance Metrics**: Response times and query statistics
- **Configuration**: Driver, URL, and connection settings

### Error Output

```bash
❌ Database connection failed
🔍 Error: Connection refused (Connection refused)
💡 Troubleshooting:
   - Check if database service is running
   - Verify connection string in configuration
   - Ensure network connectivity
   - Check firewall settings

🔧 Configuration used:
   Driver: org.postgresql.Driver
   URL: jdbc:postgresql://localhost:5432/app
   Username: postgres
```

## Exit Codes

| Code | Description |
|------|-------------|
| 0 | Success - Database is healthy |
| 1 | Error - Database connection failed |
| 2 | Warning - Database is accessible but has issues |
| 3 | Configuration error |

## Related Commands

- [tsk db migrate](./migrate.md) - Run database migrations
- [tsk db console](./console.md) - Interactive database console
- [tsk db backup](./backup.md) - Create database backup
- [tsk db restore](./restore.md) - Restore from backup
- [tsk db init](./init.md) - Initialize database

## Notes

- **Connection Pooling**: The status shows connection pool metrics when using connection pooling
- **Performance Monitoring**: Use `--performance` flag for detailed performance analysis
- **JSON Output**: JSON format is useful for automated monitoring and integration
- **Timeout**: Use `--timeout` to adjust connection timeout for slow networks
- **Caching**: Results may be cached for performance; use `--verbose` for real-time data

## See Also

- [Database Commands Overview](./README.md)
- [Configuration Management](../config/README.md)
- [Performance Tuning Guide](../../examples/performance-tuning.md) 