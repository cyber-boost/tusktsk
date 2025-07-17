# Cache Commands

Cache management operations for TuskLang Java CLI, providing comprehensive caching capabilities for improved application performance.

## Available Commands

| Command | Description |
|---------|-------------|
| [tsk cache clear](./clear.md) | Clear application cache |
| [tsk cache status](./status.md) | Check cache status |
| [tsk cache warm](./warm.md) | Warm up cache |
| [tsk cache memcached status](./memcached/status.md) | Check Memcached status |
| [tsk cache memcached stats](./memcached/stats.md) | Show Memcached statistics |
| [tsk cache memcached flush](./memcached/flush.md) | Flush Memcached data |
| [tsk cache memcached restart](./memcached/restart.md) | Restart Memcached service |
| [tsk cache memcached test](./memcached/test.md) | Test Memcached connection |

## Common Use Cases

- **Performance Optimization**: Warm cache with frequently accessed data
- **Cache Management**: Clear and monitor cache performance
- **Memcached Operations**: Manage external Memcached server
- **Development Workflow**: Clear cache during development
- **Production Monitoring**: Monitor cache hit ratios and performance

## Java-Specific Notes

### Cache Types

The Java CLI supports multiple cache types:

- **Configuration Cache**: Cached configuration values
- **Database Cache**: Query result caching
- **Session Cache**: User session data
- **Application Cache**: General application data
- **Memcached**: External Memcached server

### Cache Configuration

```properties
# Cache settings in peanu.peanuts
[cache]
enabled: true
ttl: 3600
max_size: 1000
backend: "memory"

[memcached]
host: "localhost"
port: 11211
timeout: 5000
pool_size: 10
```

### Performance Monitoring

```java
// Monitor cache performance
CacheCommands cache = new CacheCommands();
CacheStatus status = cache.status();
System.out.println("Hit ratio: " + status.getHitRatio() + "%");
System.out.println("Memory usage: " + status.getMemoryUsage() + "MB");
```

## Examples

### Complete Cache Workflow

```bash
# 1. Check cache status
tsk cache status

# 2. Warm cache with data
tsk cache warm

# 3. Monitor performance
tsk cache status --verbose

# 4. Clear cache if needed
tsk cache clear

# 5. Restart Memcached
tsk cache memcached restart
```

### Memcached Management

```bash
# Check Memcached status
tsk cache memcached status

# View statistics
tsk cache memcached stats

# Test connection
tsk cache memcached test

# Flush all data
tsk cache memcached flush
```

## Related Commands

- [Database Commands](../db/README.md) - Database operations
- [Configuration Commands](../config/README.md) - Configuration management
- [Service Commands](../services/README.md) - Service management 