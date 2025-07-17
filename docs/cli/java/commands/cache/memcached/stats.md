# tsk cache memcached stats

Show detailed Memcached server statistics and performance metrics.

## Synopsis

```bash
tsk cache memcached stats [OPTIONS]
```

## Description

The `tsk cache memcached stats` command displays comprehensive statistics from the Memcached server including performance metrics, memory usage, connection information, and operational statistics.

## Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| --help | -h | Show help for this command | - |
| --json | -j | Output results in JSON format | false |
| --host | -H | Memcached host address | localhost |
| --port | -p | Memcached port number | 11211 |
| --timeout | -t | Connection timeout in seconds | 5 |

## Examples

### Basic Usage
```bash
# Show all Memcached statistics
tsk cache memcached stats
```

**Output:**
```
📊 Memcached Statistics
🔧 Server Information:
   PID: 12345
   Uptime: 2 days, 3 hours, 45 minutes
   Version: 1.6.9
   Threads: 4
   Max Connections: 1024
   Current Connections: 12

📈 Performance Metrics:
   Gets: 1,234,567
   Sets: 567,890
   Hits: 1,123,456
   Misses: 111,111
   Hit Ratio: 91.0%
   Evictions: 0
   Reclaimed: 0
   Expired: 45

💾 Memory Usage:
   Total Memory: 64MB
   Used Memory: 12.3MB
   Free Memory: 51.7MB
   Memory Usage: 19.2%
   Items: 1,234

🔗 Connection Statistics:
   Connection Structures: 1024
   Current Connections: 12
   Total Connections: 15,678
   Connection Errors: 0
```

### JSON Output
```bash
# Get statistics in JSON format
tsk cache memcached stats --json
```

## Java API Usage

```java
import org.tusklang.cli.CacheCommands;

public class MemcachedStats {
    public void getStats() {
        CacheCommands cache = new CacheCommands();
        
        // Get all statistics
        MemcachedStats stats = cache.memcachedStats();
        System.out.println("Hit ratio: " + stats.getHitRatio() + "%");
        System.out.println("Memory usage: " + stats.getMemoryUsagePercent() + "%");
        
        // JSON output
        String jsonStats = cache.memcachedStatsJson();
        System.out.println(jsonStats);
    }
}
```

## Exit Codes

| Code | Description |
|------|-------------|
| 0 | Success |
| 1 | Connection error |
| 3 | Configuration error |

## Related Commands

- [tsk cache memcached status](./status.md) - Check connection status
- [tsk cache memcached flush](./flush.md) - Flush all data
- [tsk cache memcached restart](./restart.md) - Restart service 