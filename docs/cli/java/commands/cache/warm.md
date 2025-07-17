# tsk cache warm

Warm up application cache with frequently accessed data.

## Synopsis

```bash
tsk cache warm [OPTIONS]
```

## Description

The `tsk cache warm` command preloads the application cache with frequently accessed data to improve performance after cache clearing or application startup.

## Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| --help | -h | Show help for this command | - |
| --type | -t | Cache type to warm (all, config, db, session) | all |
| --parallel | -p | Number of parallel operations | 4 |
| --timeout | -T | Operation timeout in seconds | 60 |

## Examples

### Basic Usage
```bash
# Warm all application cache
tsk cache warm
```

**Output:**
```
🔥 Cache Warming Operation
📊 Warming up application cache...
   Configuration cache: ✅ (45 items)
   Database cache: ✅ (1,234 items)
   Session cache: ✅ (89 items)
   Memcached: ✅ (567 items)

📈 Performance improvement:
   Items loaded: 1,935
   Time taken: 12.3s
   Average load time: 6.4ms per item
   Cache hit ratio: 94.2%

✅ Cache warming completed successfully
```

### Warm Specific Cache Type
```bash
# Warm only database cache
tsk cache warm --type db
```

## Java API Usage

```java
import org.tusklang.cli.CacheCommands;

public class CacheWarm {
    public void warmCache() {
        CacheCommands cache = new CacheCommands();
        
        // Warm all cache
        WarmResult result = cache.warm();
        System.out.println("Items loaded: " + result.getItemsLoaded());
        
        // Warm specific type
        WarmResult dbResult = cache.warm("db");
        System.out.println("DB items: " + dbResult.getItemsLoaded());
    }
}
```

## Exit Codes

| Code | Description |
|------|-------------|
| 0 | Success - Cache warmed |
| 1 | Error - Warming failed |
| 2 | Warning - Partial warming |

## Related Commands

- [tsk cache status](./status.md) - Check cache status
- [tsk cache clear](./clear.md) - Clear cache
- [tsk cache memcached warm](./memcached/warm.md) - Warm Memcached 