# tsk cache memcached restart

Restart the Memcached service.

## Synopsis

```bash
tsk cache memcached restart [OPTIONS]
```

## Description

The `tsk cache memcached restart` command stops and starts the Memcached service. This is useful for applying configuration changes or resolving service issues.

## Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| --help | -h | Show help for this command | - |
| --confirm | -y | Skip confirmation prompt | false |
| --wait | -w | Wait for service to be ready | true |
| --timeout | -t | Wait timeout in seconds | 30 |

## Examples

### Basic Usage
```bash
# Restart Memcached service
tsk cache memcached restart
```

**Output:**
```
🔄 Memcached Restart Operation
⚠️ This will restart the Memcached service
📊 Current connections: 12

Are you sure you want to continue? (y/N): y
🛑 Stopping Memcached service...
✅ Memcached service stopped
🚀 Starting Memcached service...
✅ Memcached service started
⏱️ Waiting for service to be ready...
✅ Memcached service is ready
📊 New connections: 0
```

### Skip Confirmation
```bash
# Restart without confirmation
tsk cache memcached restart --confirm
```

## Java API Usage

```java
import org.tusklang.cli.CacheCommands;

public class MemcachedRestart {
    public void restartService() {
        CacheCommands cache = new CacheCommands();
        
        // Restart with confirmation
        boolean restarted = cache.memcachedRestart();
        System.out.println("Restarted: " + restarted);
        
        // Restart without confirmation
        boolean autoRestarted = cache.memcachedRestart(true);
        System.out.println("Auto restarted: " + autoRestarted);
    }
}
```

## Exit Codes

| Code | Description |
|------|-------------|
| 0 | Success - Service restarted |
| 1 | Error - Restart failed |
| 2 | Cancelled - User cancelled operation |

## Related Commands

- [tsk cache memcached status](./status.md) - Check service status
- [tsk cache memcached stats](./stats.md) - Show statistics
- [tsk services restart](../../services/restart.md) - Restart all services 