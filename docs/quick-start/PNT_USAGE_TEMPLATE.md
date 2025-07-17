# 🥜 Peanut Binary Configuration - [LANGUAGE] Usage Guide

## Overview

Peanut Binary Configuration (`.pntb`) provides hierarchical configuration management with ~85% performance improvement over text parsing. This guide covers [LANGUAGE]-specific usage of the Peanut configuration system.

## Installation

The Peanut configuration system is included with TuskLang [LANGUAGE] SDK. No additional installation required.

```[LANGUAGE_SPECIFIC_IMPORT]
# Python example:
from tusklang import PeanutConfig

# JavaScript example:
const { PeanutConfig } = require('tusklang');

# Ruby example:
require 'tusklang/peanut_config'
```

## Basic Usage

### Loading Configuration

```[LANGUAGE_CODE]
# Load configuration from current directory
config = PeanutConfig.load()

# Load from specific directory
config = PeanutConfig.load('/path/to/project')

# Get specific value
port = PeanutConfig.get('server.port', default=8080)
```

### Configuration File Formats

Peanut supports three file formats:
1. `.peanuts` - Human-readable text format
2. `.tsk` - TuskLang syntax format
3. `.pntb` - Binary compiled format (fastest)

### Example Configuration File

Create `peanu.peanuts`:
```ini
# Application configuration
[app]
name: "My TuskLang App"
version: "2.0.0"
debug: true

[server]
host: "0.0.0.0"
port: 8080
workers: 4
ssl_enabled: false

[database]
driver: "postgresql"
host: "localhost"
port: 5432
name: "myapp_db"
pool_size: 10

[cache]
enabled: true
backend: "redis"
ttl: 3600
```

## Hierarchical Configuration

Peanut uses CSS-like cascading configuration inheritance:

```
/project
├── peanu.peanuts          # Root configuration
├── src/
│   └── peanu.peanuts      # Overrides for src/
└── tests/
    └── peanu.peanuts      # Test-specific config
```

Child configurations override parent values:

```[LANGUAGE_CODE]
# In /project/peanu.peanuts
[database]
host: "production.db.com"
port: 5432

# In /project/tests/peanu.peanuts
[database]
host: "localhost"  # Overrides parent
# port remains 5432 (inherited)
```

## Binary Compilation

### Automatic Compilation

Binary files are automatically created when text configs are loaded:

```[LANGUAGE_CODE]
# First load compiles to binary
config = PeanutConfig.load()  # Creates peanu.pntb

# Subsequent loads use binary (85% faster)
config = PeanutConfig.load()  # Loads from peanu.pntb
```

### Manual Compilation

```[LANGUAGE_CODE]
# Compile specific file
PeanutConfig.compile('config.peanuts', 'config.pntb')

# Compile all configs in directory
PeanutConfig.compile_all('/path/to/project')
```

### CLI Compilation

```bash
# Compile single file
tsk peanuts compile config.peanuts

# Auto-compile directory
tsk peanuts auto-compile ./

# Verify binary file
tsk peanuts load config.pntb
```

## API Reference

### Loading Methods

```[LANGUAGE_CODE]
# Load with options
config = PeanutConfig.load(
    directory='.',
    auto_compile=True,
    watch=True
)

# Get with dot notation
value = PeanutConfig.get('section.subsection.key')

# Get with default
value = PeanutConfig.get('missing.key', default='fallback')

# Get entire section
database_config = PeanutConfig.get('database')
```

### Configuration Access

```[LANGUAGE_CODE]
# Direct access (after loading)
config = PeanutConfig.load()

# Dictionary-style access
db_host = config['database']['host']

# Dot notation
db_host = config.get('database.host')

# With type conversion
port = config.get_int('server.port')
debug = config.get_bool('app.debug')
workers = config.get_float('server.workers')
```

### File Watching

Enable automatic reload on file changes:

```[LANGUAGE_CODE]
# Enable file watching
config = PeanutConfig.load(watch=True)

# Manual check for changes
if config.has_changed():
    config.reload()

# Disable watching
config.stop_watching()
```

## Advanced Features

### Environment Variable Expansion

```ini
[database]
host: "${DB_HOST:-localhost}"
port: ${DB_PORT:-5432}
password: "${DB_PASSWORD}"
```

### Type Inference

Peanut automatically infers types:
- **Strings**: `"value"` or `'value'`
- **Numbers**: `42`, `3.14`
- **Booleans**: `true`, `false`
- **Arrays**: `value1, value2, value3`
- **Null**: `null`, `nil`, `none`

### Nested Structures

```ini
[services.api]
url: "https://api.example.com"
timeout: 30
retry_attempts: 3

[services.api.headers]
Authorization: "Bearer ${API_TOKEN}"
Content-Type: "application/json"
```

Access nested values:
```[LANGUAGE_CODE]
api_url = config.get('services.api.url')
auth_header = config.get('services.api.headers.Authorization')
```

## Performance Optimization

### Binary Format Benefits

1. **Parse Time**: ~85% faster than text parsing
2. **Memory Usage**: ~40% less memory during loading
3. **File Size**: Compressed format reduces disk usage
4. **Validation**: Checksum ensures data integrity

### Benchmarking

```[LANGUAGE_CODE]
# Run performance comparison
PeanutConfig.benchmark()

# Output:
# Text parsing (1000 iterations): 1200ms
# Binary loading (1000 iterations): 180ms
# Improvement: 85%
```

### Best Practices

1. **Always compile for production**
   ```bash
   tsk peanuts compile --optimize production.peanuts
   ```

2. **Use binary format in production**
   ```[LANGUAGE_CODE]
   # Production code
   config = PeanutConfig.load_binary('config.pntb')
   ```

3. **Cache configuration objects**
   ```[LANGUAGE_CODE]
   # Singleton pattern
   _config = None
   
   def get_config():
       global _config
       if _config is None:
           _config = PeanutConfig.load()
       return _config
   ```

## Migration Guide

### From JSON

```bash
# Convert JSON to Peanut format
tsk convert -i config.json -o config.peanuts

# Compile to binary
tsk peanuts compile config.peanuts
```

### From YAML

```bash
# Convert YAML to Peanut format
tsk convert -i config.yaml -o config.peanuts
```

### From Environment Variables

```ini
# Use env vars in Peanut config
[app]
name: "${APP_NAME:-MyApp}"
environment: "${NODE_ENV:-development}"
port: ${PORT:-3000}
```

## Error Handling

```[LANGUAGE_CODE]
try:
    config = PeanutConfig.load()
except FileNotFoundError:
    print("No configuration file found")
except InvalidFormatError as e:
    print(f"Configuration error: {e}")
except ChecksumMismatchError:
    print("Binary file corrupted, regenerating...")
    PeanutConfig.compile('config.peanuts')
```

## Security Considerations

1. **Validate Input**: Always validate configuration values
2. **Secure Storage**: Protect `.pntb` files containing sensitive data
3. **Environment Variables**: Use for secrets, not hardcoded values
4. **File Permissions**: Restrict access to configuration files

```[LANGUAGE_CODE]
# Validate configuration
if not config.validate():
    raise ValueError("Invalid configuration")

# Secure sensitive values
db_password = config.get('database.password')
if not db_password:
    db_password = os.environ.get('DB_PASSWORD')
    if not db_password:
        raise ValueError("Database password not configured")
```

## Troubleshooting

### Common Issues

1. **Binary file not updating**
   ```bash
   # Force recompilation
   rm *.pntb
   tsk peanuts compile config.peanuts
   ```

2. **Checksum mismatch errors**
   ```bash
   # Verify file integrity
   tsk peanuts verify config.pntb
   
   # Recompile if corrupted
   tsk peanuts compile --force config.peanuts
   ```

3. **Performance not improved**
   ```bash
   # Ensure binary format is used
   tsk config stats
   
   # Check file dates
   ls -la peanu.*
   ```

### Debug Mode

```[LANGUAGE_CODE]
# Enable debug logging
PeanutConfig.debug = True

# Or via environment
os.environ['PEANUT_DEBUG'] = '1'
```

## Examples

### Web Application Configuration

```ini
[web]
host: "0.0.0.0"
port: ${PORT:-3000}
public_dir: "./public"
views_dir: "./views"

[web.session]
secret: "${SESSION_SECRET}"
timeout: 3600
secure: true

[web.cors]
enabled: true
origins: "https://example.com, https://app.example.com"
credentials: true
```

### Microservice Configuration

```ini
[service]
name: "user-service"
version: "2.0.0"

[service.grpc]
host: "0.0.0.0"
port: 50051

[service.http]
host: "0.0.0.0"
port: 8080

[service.discovery]
consul_host: "${CONSUL_HOST:-consul.service.consul}"
consul_port: 8500
ttl: 30
```

### Database Configuration

```ini
[database.primary]
driver: "postgresql"
host: "${DB_HOST:-localhost}"
port: ${DB_PORT:-5432}
name: "${DB_NAME:-myapp}"
user: "${DB_USER:-postgres}"
password: "${DB_PASSWORD}"

[database.replica]
driver: "postgresql"
host: "${DB_REPLICA_HOST}"
port: ${DB_REPLICA_PORT:-5432}
read_only: true

[database.pool]
min_connections: 2
max_connections: 10
idle_timeout: 300
```

## Language-Specific Features

### [LANGUAGE_SPECIFIC_SECTION]

[Add language-specific examples, idioms, and integration patterns here]

## Additional Resources

- [TuskLang Documentation](https://tusklang.org/docs)
- [Peanut Binary Specification](./PEANUT_BINARY_SPEC.md)
- [Configuration Best Practices](https://tusklang.org/guides/config)
- [Performance Tuning Guide](https://tusklang.org/guides/performance)

---

For support, visit [https://tusklang.org/support](https://tusklang.org/support)