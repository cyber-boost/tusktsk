# Configuration Commands

Configuration management tools for TuskLang Go CLI, providing hierarchical configuration loading, validation, and compilation capabilities.

## Available Commands

| Command | Description |
|---------|-------------|
| [tsk config get](./get.md) | Get configuration value |
| [tsk config check](./check.md) | Check configuration hierarchy |
| [tsk config validate](./validate.md) | Validate configuration |
| [tsk config compile](./compile.md) | Compile configurations |
| [tsk config docs](./docs.md) | Generate documentation |
| [tsk config clear-cache](./clear-cache.md) | Clear configuration cache |
| [tsk config stats](./stats.md) | Show configuration statistics |

## Common Use Cases

- **Configuration Access**: Retrieve values from hierarchical configs
- **Configuration Validation**: Validate syntax and structure
- **Configuration Compilation**: Compile to binary format
- **Configuration Documentation**: Generate config documentation
- **Configuration Monitoring**: Monitor config statistics and health

## Go-Specific Notes

The Go CLI configuration commands leverage Go's configuration management strengths:

- **Hierarchical Loading**: CSS-like cascading configuration
- **Type Safety**: Strong typing with struct mapping
- **Performance**: Binary format for production use
- **Validation**: Comprehensive configuration validation
- **Hot Reloading**: File watching for development

## Examples

### Configuration Access

```bash
# Get configuration values
tsk config get server.port
tsk config get database.host

# Check configuration hierarchy
tsk config check .
```

### Configuration Management

```bash
# Validate configuration
tsk config validate .

# Compile to binary
tsk config compile .

# Generate documentation
tsk config docs .
```

### Configuration Monitoring

```bash
# Show configuration statistics
tsk config stats

# Clear configuration cache
tsk config clear-cache
``` 