# tsk db status

Check database connection and health status.

## Synopsis

```bash
tsk db status [OPTIONS]
```

## Description

The `tsk db status` command checks the connection to the configured database and displays detailed health information including:

- Connection status
- Database version
- Active connections
- Database size
- Performance metrics
- Configuration details

This command is useful for:
- Verifying database connectivity
- Monitoring database health
- Debugging connection issues
- Checking configuration validity

## Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| --help | -h | Show help for this command | - |
| --config | -c | Use alternate config file | peanu.peanuts |
| --json | -j | Output in JSON format | false |
| --verbose | -v | Enable verbose output | false |
| --timeout | -t | Connection timeout in seconds | 30 |

## Examples

### Basic Usage

```bash
# Check database status using default configuration
tsk db status
```

**Output:**
```
✅ Database Connection: Connected
📍 Host: localhost:5432
📊 Database: myapp
👤 User: postgres
🔄 Version: PostgreSQL 14.5
📈 Active Connections: 5
💾 Database Size: 256 MB
⏱️  Response Time: 12ms
```

### Verbose Output

```bash
# Get detailed status information
tsk db status --verbose
```

**Output:**
```
✅ Database Connection: Connected
📍 Host: localhost:5432
📊 Database: myapp
👤 User: postgres
🔄 Version: PostgreSQL 14.5
📈 Active Connections: 5
💾 Database Size: 256 MB
⏱️  Response Time: 12ms

📋 Configuration Details:
  - Connection Pool: 10
  - SSL Mode: require
  - Timeout: 30s
  - Max Connections: 100

📊 Performance Metrics:
  - Queries per second: 150
  - Average query time: 8ms
  - Cache hit ratio: 95%
  - Index usage: 87%

🔧 Health Checks:
  - Connection Pool: ✅ Healthy
  - Query Performance: ✅ Good
  - Disk Space: ✅ Sufficient
  - Memory Usage: ✅ Normal
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
    "host": "localhost",
    "port": 5432,
    "name": "myapp",
    "user": "postgres",
    "version": "PostgreSQL 14.5"
  },
  "metrics": {
    "activeConnections": 5,
    "databaseSize": "256 MB",
    "responseTime": 12,
    "queriesPerSecond": 150,
    "averageQueryTime": 8,
    "cacheHitRatio": 95,
    "indexUsage": 87
  },
  "health": {
    "connectionPool": "healthy",
    "queryPerformance": "good",
    "diskSpace": "sufficient",
    "memoryUsage": "normal"
  },
  "configuration": {
    "poolSize": 10,
    "sslMode": "require",
    "timeout": 30,
    "maxConnections": 100
  }
}
```

### Custom Configuration

```bash
# Use specific configuration file
tsk db status --config production.peanuts

# Use custom timeout
tsk db status --timeout 60
```

## JavaScript API Usage

### Programmatic Status Check

```javascript
const { execSync } = require('child_process');
const { PeanutConfig } = require('tusklang');

// Check database status programmatically
function checkDatabaseStatus() {
    try {
        const output = execSync('tsk db status --json', { encoding: 'utf8' });
        const status = JSON.parse(output);
        
        if (status.status === 'connected') {
            console.log('✅ Database is healthy');
            return true;
        } else {
            console.log('❌ Database connection failed');
            return false;
        }
    } catch (error) {
        console.error('❌ Database status check failed:', error.message);
        return false;
    }
}

// Use in application
if (!checkDatabaseStatus()) {
    process.exit(1);
}
```

### Integration with Express.js

```javascript
const express = require('express');
const { execSync } = require('child_process');

const app = express();

// Health check endpoint
app.get('/health/db', (req, res) => {
    try {
        const output = execSync('tsk db status --json', { encoding: 'utf8' });
        const status = JSON.parse(output);
        
        if (status.status === 'connected') {
            res.json({
                status: 'healthy',
                database: status.database,
                metrics: status.metrics
            });
        } else {
            res.status(503).json({
                status: 'unhealthy',
                error: 'Database connection failed'
            });
        }
    } catch (error) {
        res.status(503).json({
            status: 'error',
            error: error.message
        });
    }
});
```

### Monitoring Script

```javascript
const { execSync } = require('child_process');
const fs = require('fs');

// Database monitoring script
function monitorDatabase() {
    const timestamp = new Date().toISOString();
    
    try {
        const output = execSync('tsk db status --json', { encoding: 'utf8' });
        const status = JSON.parse(output);
        
        // Log status
        const logEntry = {
            timestamp,
            status: status.status,
            responseTime: status.metrics.responseTime,
            activeConnections: status.metrics.activeConnections
        };
        
        fs.appendFileSync('db-monitor.log', JSON.stringify(logEntry) + '\n');
        
        // Alert if unhealthy
        if (status.status !== 'connected') {
            console.error(`❌ Database unhealthy at ${timestamp}`);
            // Send alert (email, Slack, etc.)
        }
        
    } catch (error) {
        console.error(`❌ Database check failed at ${timestamp}:`, error.message);
    }
}

// Run monitoring every 5 minutes
setInterval(monitorDatabase, 5 * 60 * 1000);
```

## Output

### Success Output

When the database is healthy and accessible:

```
✅ Database Connection: Connected
📍 Host: localhost:5432
📊 Database: myapp
👤 User: postgres
🔄 Version: PostgreSQL 14.5
📈 Active Connections: 5
💾 Database Size: 256 MB
⏱️  Response Time: 12ms
```

### Error Output

When the database is unreachable:

```
❌ Database Connection: Failed
📍 Host: localhost:5432
📊 Database: myapp
👤 User: postgres
🔍 Error: connection refused
💡 Suggestion: Check if database server is running
```

### Warning Output

When there are issues but connection works:

```
⚠️  Database Connection: Connected (with warnings)
📍 Host: localhost:5432
📊 Database: myapp
👤 User: postgres
🔄 Version: PostgreSQL 14.5
⚠️  Warning: High connection count (95/100)
⚠️  Warning: Slow response time (150ms)
```

## Exit Codes

| Code | Description |
|------|-------------|
| 0 | Success - Database is healthy |
| 1 | Error - Database connection failed |
| 2 | Warning - Database has issues but is accessible |

## Related Commands

- [tsk db migrate](./migrate.md) - Run database migrations
- [tsk db console](./console.md) - Interactive database console
- [tsk db backup](./backup.md) - Backup database
- [tsk db restore](./restore.md) - Restore from backup

## Notes

- The command uses the database configuration from your `peanu.peanuts` or `peanu.tsk` file
- Connection timeouts can be adjusted with the `--timeout` option
- JSON output is useful for programmatic integration
- Verbose mode provides detailed performance metrics
- The command is safe to run frequently for monitoring

## See Also

- [Database Commands Overview](./README.md)
- [Configuration Guide](../../../js/docs/PNT_GUIDE.md)
- [Examples](../../examples/database-monitoring.md) 