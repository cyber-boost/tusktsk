# Configuration Commands

Configuration management operations for TuskLang Java CLI, providing comprehensive configuration handling with hierarchical loading and binary compilation.

## Available Commands

| Command | Description |
|---------|-------------|
| [tsk config get](./get.md) | Get configuration value |
| [tsk config check](./check.md) | Check configuration hierarchy |
| [tsk config validate](./validate.md) | Validate configuration |
| [tsk config compile](./compile.md) | Compile configuration |
| [tsk config docs](./docs.md) | Generate documentation |
| [tsk config clear-cache](./clear-cache.md) | Clear configuration cache |
| [tsk config stats](./stats.md) | Show configuration statistics |

## Common Use Cases

- **Configuration Access**: Retrieve values using dot notation paths
- **Hierarchical Management**: Manage configuration inheritance
- **Validation**: Ensure configuration integrity
- **Performance**: Compile to binary format for speed
- **Documentation**: Generate configuration documentation

## Java-Specific Notes

### Configuration Types

The Java CLI supports multiple configuration formats:

- **`.peanuts`**: Human-readable INI-style configuration
- **`.tsk`**: TuskLang syntax with advanced features
- **`.pnt`**: Compiled binary format (85% faster)

### Hierarchical Loading

Configuration files are loaded in hierarchical order:

```
/project/
├── peanu.peanuts          # Parent config
├── src/
│   └── peanu.peanuts      # Child config (overrides parent)
└── src/main/
    └── peanu.peanuts      # Grandchild config (overrides both)
```

### Type Safety

```java
// Type-safe configuration access
PeanutConfig config = new PeanutConfig();
String host = config.get("server.host", String.class, "localhost");
Integer port = config.get("server.port", Integer.class, 8080);
Boolean debug = config.get("server.debug", Boolean.class, false);
```

## Examples

### Complete Configuration Workflow

```bash
# 1. Check configuration hierarchy
tsk config check ./

# 2. Validate configuration
tsk config validate ./

# 3. Get configuration value
tsk config get server.port

# 4. Compile to binary
tsk config compile ./

# 5. Generate documentation
tsk config docs ./
```

### Configuration Management

```bash
# Get nested configuration
tsk config get database.postgresql.host

# Validate with verbose output
tsk config validate ./ --verbose

# Show configuration statistics
tsk config stats

# Clear configuration cache
tsk config clear-cache
```

## Related Commands

- [Database Commands](../db/README.md) - Database operations
- [Cache Commands](../cache/README.md) - Cache management
- [Binary Commands](../binary/README.md) - Binary compilation 