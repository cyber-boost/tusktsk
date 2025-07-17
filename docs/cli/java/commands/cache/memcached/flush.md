# tsk cache memcached flush

Flush all data from Memcached server.

## Synopsis

```bash
tsk cache memcached flush [OPTIONS]
```

## Description

The `tsk cache memcached flush` command removes all data from the Memcached server. This is useful for clearing cache during development, testing, or maintenance operations.

## Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| --help | -h | Show help for this command | - |
| --confirm | -y | Skip confirmation prompt | false |
| --host | -H | Memcached host address | localhost |
| --port | -p | Memcached port number | 11211 |
| --timeout | -t | Connection timeout in seconds | 5 |

## Examples

### Basic Usage
```bash
# Flush all data with confirmation
tsk cache memcached flush
```

**Output:**
```
🗄️ Memcached Flush Operation
⚠️ This will remove ALL data from Memcached server
📊 Current items: 1,234
💾 Memory usage: 12.3MB

Are you sure you want to continue? (y/N): y
✅ Successfully flushed all data from Memcached
📊 Items removed: 1,234
💾 Memory freed: 12.3MB
```

### Skip Confirmation
```bash
# Flush without confirmation prompt
tsk cache memcached flush --confirm
```

## Java API Usage

```java
import org.tusklang.cli.CacheCommands;

public class MemcachedFlush {
    public void flushCache() {
        CacheCommands cache = new CacheCommands();
        
        // Flush with confirmation
        boolean flushed = cache.memcachedFlush();
        System.out.println("Flushed: " + flushed);
        
        // Flush without confirmation
        boolean autoFlushed = cache.memcachedFlush(true);
        System.out.println("Auto flushed: " + autoFlushed);
    }
}
```

## Exit Codes

| Code | Description |
|------|-------------|
| 0 | Success - Data flushed |
| 1 | Error - Flush failed |
| 2 | Cancelled - User cancelled operation |

## Related Commands

- [tsk cache memcached status](./status.md) - Check server status
- [tsk cache memcached stats](./stats.md) - Show statistics
- [tsk cache clear](../clear.md) - Clear application cache 