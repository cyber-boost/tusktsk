# tsk cache status

Check application cache status and performance.

## Synopsis

```bash
tsk cache status [OPTIONS]
```

## Description

The `tsk cache status` command displays the status of all application caches including configuration cache, database cache, session cache, and external cache systems.

## Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| --help | -h | Show help for this command | - |
| --verbose | -v | Show detailed cache information | false |
| --json | -j | Output results in JSON format | false |
| --type | -t | Cache type to check (all, config, db, session) | all |

## Examples

### Basic Usage
```bash
# Check all cache status
tsk cache status
```

**Output:**
```
🗄️ Application Cache Status
✅ Configuration Cache: Active (45 items, 2.3MB)
✅ Database Cache: Active (1,234 items, 12.1MB)
✅ Session Cache: Active (89 items, 1.2MB)
✅ Memcached: Connected (localhost:11211)

📊 Overall Statistics:
   Total items: 1,368
   Total memory: 15.6MB
   Hit ratio: 87.3%
   Evictions: 0
   Status: Healthy
```

### Verbose Output
```bash
# Show detailed cache information
tsk cache status --verbose
```

## Java API Usage

```java
import org.tusklang.cli.CacheCommands;

public class CacheStatus {
    public void checkStatus() {
        CacheCommands cache = new CacheCommands();
        
        // Basic status
        CacheStatus status = cache.status();
        System.out.println("Healthy: " + status.isHealthy());
        
        // Verbose status
        CacheStatus verbose = cache.status(true);
        System.out.println("Hit ratio: " + verbose.getHitRatio() + "%");
    }
}
```

## Exit Codes

| Code | Description |
|------|-------------|
| 0 | Success - Cache is healthy |
| 1 | Error - Cache issues detected |
| 2 | Warning - Some cache problems |

## Related Commands

- [tsk cache clear](./clear.md) - Clear cache
- [tsk cache warm](./warm.md) - Warm cache
- [tsk cache memcached status](./memcached/status.md) - Memcached status 