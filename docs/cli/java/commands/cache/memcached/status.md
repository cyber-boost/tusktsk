# tsk cache memcached status

Check Memcached service status and connection health.

## Synopsis

```bash
tsk cache memcached status [OPTIONS]
```

## Description

The `tsk cache memcached status` command checks the health and status of the Memcached service. It provides information about connection status, server statistics, memory usage, and performance metrics.

## Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| --help | -h | Show help for this command | - |
| --verbose | -v | Show detailed server information | false |
| --json | -j | Output results in JSON format | false |
| --timeout | -t | Connection timeout in seconds | 5 |
| --host | -H | Memcached host address | localhost |
| --port | -p | Memcached port number | 11211 |

## Arguments

| Argument | Required | Description |
|----------|----------|-------------|
| None | No | No arguments required |

## Examples

### Basic Usage
```bash
# Check basic Memcached status
tsk cache memcached status
```

**Output:**
```
🗄️ Memcached Status Check
✅ Connected to Memcached server: localhost:11211
📊 Version: 1.6.9
🔗 Connection: Established
⏱️ Response Time: 2ms
📈 Status: Healthy
```

### Verbose Output
```bash
# Show detailed server information
tsk cache memcached status --verbose
```

**Output:**
```
🗄️ Memcached Status Check (Verbose)
✅ Connected to Memcached server: localhost:11211
📊 Version: 1.6.9
🔗 Connection: Established
⏱️ Response Time: 2ms
📈 Status: Healthy

🔧 Server Information:
   PID: 12345
   Uptime: 2 days, 3 hours, 45 minutes
   Threads: 4
   Max Connections: 1024
   Current Connections: 12

📊 Memory Usage:
   Total Memory: 64MB
   Used Memory: 12.3MB
   Free Memory: 51.7MB
   Memory Usage: 19.2%

📈 Performance Metrics:
   Gets: 1,234,567
   Sets: 567,890
   Hits: 1,123,456
   Misses: 111,111
   Hit Ratio: 91.0%
   Evictions: 0
   Reclaimed: 0

🔗 Connection Pool:
   Active connections: 12
   Idle connections: 8
   Connection errors: 0
```

### JSON Output
```bash
# Get status in JSON format
tsk cache memcached status --json
```

**Output:**
```json
{
  "status": "connected",
  "server": {
    "host": "localhost",
    "port": 11211,
    "version": "1.6.9",
    "pid": 12345,
    "uptime_seconds": 183900
  },
  "connection": {
    "established": true,
    "response_time_ms": 2,
    "active_connections": 12,
    "idle_connections": 8
  },
  "memory": {
    "total_bytes": 67108864,
    "used_bytes": 12897485,
    "free_bytes": 54211379,
    "usage_percent": 19.2
  },
  "performance": {
    "gets": 1234567,
    "sets": 567890,
    "hits": 1123456,
    "misses": 111111,
    "hit_ratio": 91.0,
    "evictions": 0,
    "reclaimed": 0
  },
  "timestamp": "2024-12-19T10:30:00Z"
}
```

### Custom Host and Port
```bash
# Check remote Memcached server
tsk cache memcached status --host 192.168.1.100 --port 11211
```

**Output:**
```
🗄️ Memcached Status Check
✅ Connected to Memcached server: 192.168.1.100:11211
📊 Version: 1.6.9
🔗 Connection: Established
⏱️ Response Time: 15ms
📈 Status: Healthy
```

## Java API Usage

```java
// Programmatic Memcached status check
import org.tusklang.cli.CacheCommands;

public class MemcachedMonitor {
    
    public void checkMemcachedStatus() {
        CacheCommands cache = new CacheCommands();
        
        // Basic status check
        MemcachedStatus status = cache.memcachedStatus();
        System.out.println("Connected: " + status.isConnected());
        
        // Verbose status
        MemcachedStatus verbose = cache.memcachedStatus(true);
        System.out.println("Hit ratio: " + verbose.getHitRatio() + "%");
        
        // Custom connection
        MemcachedStatus remote = cache.memcachedStatus("192.168.1.100", 11211);
        System.out.println("Remote server: " + remote.getServerInfo());
        
        // JSON output
        String jsonStatus = cache.memcachedStatusJson();
        System.out.println(jsonStatus);
    }
    
    public void monitorPerformance() {
        CacheCommands cache = new CacheCommands();
        
        // Get performance metrics
        MemcachedMetrics metrics = cache.getMemcachedMetrics();
        System.out.println("Gets/sec: " + metrics.getGetsPerSecond());
        System.out.println("Hit ratio: " + metrics.getHitRatio() + "%");
        System.out.println("Memory usage: " + metrics.getMemoryUsagePercent() + "%");
    }
}
```

## Output

### Success Output

The command returns detailed Memcached status information including:

- **Connection Status**: Whether Memcached is accessible
- **Server Information**: Version, PID, uptime, threads
- **Memory Usage**: Total, used, free memory and usage percentage
- **Performance Metrics**: Gets, sets, hits, misses, hit ratio
- **Connection Pool**: Active and idle connections

### Error Output

```bash
❌ Memcached connection failed
🔍 Error: Connection refused (Connection refused)
💡 Troubleshooting:
   - Check if Memcached service is running
   - Verify host and port configuration
   - Ensure network connectivity
   - Check firewall settings

🔧 Configuration used:
   Host: localhost
   Port: 11211
   Timeout: 5s
```

## Exit Codes

| Code | Description |
|------|-------------|
| 0 | Success - Memcached is healthy |
| 1 | Error - Memcached connection failed |
| 2 | Warning - Memcached is accessible but has issues |
| 3 | Configuration error |

## Related Commands

- [tsk cache memcached stats](./stats.md) - Show detailed statistics
- [tsk cache memcached flush](./flush.md) - Flush all data
- [tsk cache memcached restart](./restart.md) - Restart Memcached service
- [tsk cache memcached test](./test.md) - Test connection and operations
- [tsk cache status](../status.md) - General cache status

## Notes

- **Connection Pooling**: The status shows connection pool metrics when using connection pooling
- **Performance Monitoring**: Use `--verbose` flag for detailed performance analysis
- **JSON Output**: JSON format is useful for automated monitoring and integration
- **Timeout**: Use `--timeout` to adjust connection timeout for slow networks
- **Remote Servers**: Use `--host` and `--port` to check remote Memcached servers

## See Also

- [Cache Commands Overview](../README.md)
- [Memcached Commands Overview](./README.md)
- [Performance Monitoring Guide](../../examples/performance-tuning.md) 