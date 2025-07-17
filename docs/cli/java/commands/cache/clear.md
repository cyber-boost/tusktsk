# tsk cache clear

Clear application cache and stored data.

## Synopsis

```bash
tsk cache clear [OPTIONS]
```

## Description

The `tsk cache clear` command removes all cached data from the application cache, including configuration cache, database query cache, and session cache.

## Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| --help | -h | Show help for this command | - |
| --confirm | -y | Skip confirmation prompt | false |
| --type | -t | Cache type to clear (all, config, db, session) | all |

## Examples

### Basic Usage
```bash
# Clear all application cache
tsk cache clear
```

**Output:**
```
🗄️ Cache Clear Operation
⚠️ This will clear all application cache
📊 Cache statistics:
   Configuration cache: 45 items
   Database cache: 1,234 items
   Session cache: 89 items
   Total memory: 15.6MB

Are you sure you want to continue? (y/N): y
✅ Successfully cleared all cache
📊 Items removed: 1,368
💾 Memory freed: 15.6MB
```

### Clear Specific Cache Type
```bash
# Clear only configuration cache
tsk cache clear --type config
```

## Java API Usage

```java
import org.tusklang.cli.CacheCommands;

public class CacheClear {
    public void clearCache() {
        CacheCommands cache = new CacheCommands();
        
        // Clear all cache
        boolean cleared = cache.clear();
        System.out.println("Cleared: " + cleared);
        
        // Clear specific type
        boolean configCleared = cache.clear("config");
        System.out.println("Config cleared: " + configCleared);
    }
}
```

## Exit Codes

| Code | Description |
|------|-------------|
| 0 | Success - Cache cleared |
| 1 | Error - Clear failed |
| 2 | Cancelled - User cancelled operation |

## Related Commands

- [tsk cache status](./status.md) - Check cache status
- [tsk cache warm](./warm.md) - Warm cache
- [tsk cache memcached flush](./memcached/flush.md) - Flush Memcached 