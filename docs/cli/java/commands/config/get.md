# tsk config get

Get configuration value by key path.

## Synopsis

```bash
tsk config get <key.path> [dir] [OPTIONS]
```

## Description

The `tsk config get` command retrieves configuration values using dot notation paths. It searches through the configuration hierarchy and returns the first matching value found.

## Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| --help | -h | Show help for this command | - |
| --default | -d | Default value if key not found | null |
| --type | -t | Expected data type (string, int, bool, json) | auto |
| --json | -j | Output results in JSON format | false |
| --verbose | -v | Show detailed lookup information | false |

## Arguments

| Argument | Required | Description |
|----------|----------|-------------|
| key.path | Yes | Configuration key path (e.g., server.port) |
| dir | No | Configuration directory (default: current) |

## Examples

### Basic Usage
```bash
# Get server port configuration
tsk config get server.port
```

**Output:**
```
8080
```

### With Default Value
```bash
# Get configuration with default
tsk config get database.host --default localhost
```

### JSON Output
```bash
# Get configuration in JSON format
tsk config get server --json
```

**Output:**
```json
{
  "host": "localhost",
  "port": 8080,
  "workers": 4,
  "debug": true
}
```

### Verbose Lookup
```bash
# Show detailed lookup information
tsk config get app.name --verbose
```

**Output:**
```
🔍 Configuration Lookup
📁 Search directory: /path/to/project
🔑 Key path: app.name
📂 Files checked:
   - peanu.pnt (found)
   - peanu.peanuts (not found)
   - peanu.tsk (not found)

✅ Value found: "My Application"
📍 Source: peanu.pnt
```

## Java API Usage

```java
import org.tusklang.cli.ConfigCommands;

public class ConfigGet {
    public void getConfig() {
        ConfigCommands config = new ConfigCommands();
        
        // Get simple value
        String port = config.get("server.port", "3000");
        System.out.println("Port: " + port);
        
        // Get with type
        Integer workers = config.get("server.workers", Integer.class, 4);
        System.out.println("Workers: " + workers);
        
        // Get complex object
        Map<String, Object> server = config.get("server", Map.class, new HashMap<>());
        System.out.println("Server config: " + server);
    }
}
```

## Exit Codes

| Code | Description |
|------|-------------|
| 0 | Success - Value found |
| 1 | Error - Configuration error |
| 2 | Warning - Value not found, using default |

## Related Commands

- [tsk config check](./check.md) - Check configuration hierarchy
- [tsk config validate](./validate.md) - Validate configuration
- [tsk config compile](./compile.md) - Compile configuration 