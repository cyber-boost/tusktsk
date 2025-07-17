# tsk config get

Get configuration values from the hierarchical configuration system.

## Synopsis

```bash
tsk config get [OPTIONS] <KEY_PATH>
```

## Description

The `tsk config get` command retrieves configuration values from the TuskLang hierarchical configuration system. It supports dot-notation paths to access nested configuration values and provides type-safe access to configuration data.

The command automatically loads the configuration hierarchy from the current directory and its parent directories, following the CSS-like cascading rules where child configurations override parent configurations.

## Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--help` | -h | Show help for this command | - |
| `--default` | -d | Default value if key not found | null |
| `--type` | -t | Expected data type (string, int, bool, etc.) | auto |
| `--format` | -f | Output format (text, json, yaml) | text |
| `--directory` | -D | Directory to load configuration from | current |

## Arguments

| Argument | Required | Description |
|----------|----------|-------------|
| `KEY_PATH` | Yes | Dot-notation path to configuration key (e.g., "app.name", "server.port") |

## Examples

### Basic Usage

```bash
# Get simple configuration value
tsk config get app.name
# Output: My TuskLang App

# Get nested configuration value
tsk config get server.database.host
# Output: localhost

# Get with default value
tsk config get server.port --default 8080
# Output: 3000 (or 8080 if not found)
```

### Advanced Usage

```bash
# Get typed value
tsk config get server.port --type int
# Output: 3000

# Get JSON output
tsk config get app --json
# Output: {"name": "My App", "version": "1.0.0"}

# Get from specific directory
tsk config get database.url --directory /path/to/config
# Output: postgresql://user:pass@localhost/db

# Get with YAML format
tsk config get server --format yaml
# Output:
# host: localhost
# port: 3000
# debug: true
```

### Complex Configuration Access

```bash
# Access array elements
tsk config get features[0]
# Output: authentication

# Access nested objects
tsk config get server.database.connection.pool_size
# Output: 10

# Access with type conversion
tsk config get server.debug --type bool
# Output: true
```

## C# API Usage

```csharp
// Programmatic usage
using TuskLang.CLI;

// Execute command
var result = await TskCommand.ExecuteAsync("config", "get", "app.name");
Console.WriteLine(result); // My TuskLang App

// With options
var jsonResult = await TskCommand.ExecuteAsync("config", "get", "app", "--json");
var config = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonResult);

// Using the configuration service
public class ConfigService
{
    private readonly ITskCommandService _tsk;

    public ConfigService(ITskCommandService tsk)
    {
        _tsk = tsk;
    }

    public async Task<string> GetAppNameAsync()
    {
        return await _tsk.ExecuteAsync("config", "get", "app.name");
    }

    public async Task<int> GetServerPortAsync()
    {
        var port = await _tsk.ExecuteAsync("config", "get", "server.port", "--type", "int");
        return int.Parse(port);
    }
}
```

## Output

### Success Output

```bash
# Simple value
tsk config get app.name
My TuskLang App

# Complex object (JSON format)
tsk config get server --json
{
  "host": "localhost",
  "port": 3000,
  "debug": true,
  "database": {
    "driver": "postgresql",
    "host": "db.example.com",
    "port": 5432
  }
}

# YAML format
tsk config get server --format yaml
host: localhost
port: 3000
debug: true
database:
  driver: postgresql
  host: db.example.com
  port: 5432
```

### Error Output

```bash
# Key not found
tsk config get invalid.key
Error: Configuration key 'invalid.key' not found

Available keys:
- app.name
- app.version
- server.host
- server.port

# Type conversion error
tsk config get app.name --type int
Error: Cannot convert 'My TuskLang App' to type 'int'

# Configuration file not found
tsk config get app.name
Error: No configuration files found in current directory or parents

Run 'tsk init' to create a new configuration file.
```

## Exit Codes

| Code | Description |
|------|-------------|
| 0 | Success - value retrieved |
| 1 | General error |
| 2 | Invalid arguments |
| 3 | Configuration key not found |
| 4 | Type conversion error |
| 5 | Configuration file not found |

## Configuration Hierarchy

The command follows the hierarchical configuration loading:

```
/project-root/
├── peanu.peanuts          # Base configuration
├── /src/
│   └── peanu.peanuts      # Overrides for src directory
└── /src/api/
    └── peanu.peanuts      # API-specific overrides
```

Child configurations override parent configurations:

```bash
# In /project-root/peanu.peanuts
[app]
name: "Base App"
port: 8080

# In /project-root/src/peanu.peanuts
[app]
name: "Source App"  # Overrides base
port: 3000          # Overrides base

# Result when running from /project-root/src/
tsk config get app.name
# Output: Source App

tsk config get app.port
# Output: 3000
```

## Type System

The command supports automatic type inference and conversion:

### Supported Types

- **string** - Text values (default)
- **int** - Integer numbers
- **long** - Long integers
- **double** - Floating-point numbers
- **bool** - Boolean values (true/false)
- **object** - Complex objects (JSON/YAML)

### Type Conversion Examples

```bash
# Automatic type inference
tsk config get server.port
# Output: 3000 (detected as int)

tsk config get server.debug
# Output: true (detected as bool)

# Explicit type conversion
tsk config get server.port --type string
# Output: "3000"

tsk config get server.debug --type int
# Output: 1 (true converted to int)
```

## Performance Considerations

### Caching

Configuration is cached after first load:

```bash
# First call - loads and parses configuration
tsk config get app.name  # ~50ms

# Subsequent calls - uses cache
tsk config get app.name  # ~1ms
tsk config get server.port  # ~1ms
```

### Binary Configuration

For better performance, compile to binary format:

```bash
# Compile to binary
tsk config compile peanu.peanuts

# Use binary configuration
tsk config get app.name --config peanu.pnt  # ~10ms faster
```

## Related Commands

- [tsk config set](./set.md) - Set configuration values
- [tsk config check](./check.md) - Check configuration status
- [tsk config validate](./validate.md) - Validate configuration
- [tsk config compile](./compile.md) - Compile to binary format
- [tsk config stats](./stats.md) - Show configuration statistics

## Notes

- Configuration keys are case-sensitive
- Use dot notation for nested access (e.g., "server.database.host")
- Array access is supported (e.g., "features[0]")
- The command automatically handles configuration file compilation
- Binary configuration files (.pnt) provide better performance
- Configuration is cached for subsequent commands

## See Also

- [Configuration Commands Overview](./README.md)
- [Quick Start Guide](../../quickstart.md)
- [Configuration Examples](../../examples/basic-usage.md) 