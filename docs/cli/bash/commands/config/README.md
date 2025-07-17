# Configuration Commands

Configuration management and validation for TuskLang projects.

## Available Commands

| Command | Description |
|---------|-------------|
| [tsk config get](./get.md) | Get configuration value |
| [tsk config check](./check.md) | Check configuration hierarchy |
| [tsk config validate](./validate.md) | Validate configuration |
| [tsk config compile](./compile.md) | Compile configurations |
| [tsk config docs](./docs.md) | Generate documentation |
| [tsk config clear-cache](./clear-cache.md) | Clear configuration cache |
| [tsk config stats](./stats.md) | Show statistics |

## Common Use Cases

- **Configuration Access**: Retrieve values from hierarchical configurations
- **Validation**: Ensure configuration syntax and structure are correct
- **Documentation**: Generate configuration documentation
- **Performance**: Optimize configuration loading and caching

## Bash-Specific Notes

### Configuration Loading
```bash
# Load configuration with inheritance
config=$(tsk config load)

# Access values with dot notation
APP_NAME=$(tsk config get app.name)
SERVER_PORT=$(tsk config get server.port)
```

### Environment Integration
```bash
#!/bin/bash
# load_config.sh

# Export all configuration as environment variables
while IFS='=' read -r key value; do
    [[ -z "$key" || "$key" =~ ^# ]] && continue
    export "${key//./_}"="$value"
done < <(tsk peanuts load peanu.pnt)

echo "Configuration loaded into environment"
```

## Related Commands

- [Database Commands](../db/README.md) - Database operations
- [Peanuts Commands](../peanuts/README.md) - Peanut configuration
- [Utility Commands](../utility/README.md) - General utilities 