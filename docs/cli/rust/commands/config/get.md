# tsk config get

Get configuration value by key path with type-safe retrieval.

## Synopsis

```bash
tsk config get <key.path> [dir]
```

## Description

Retrieves a configuration value using a dot-separated key path. The command searches for configuration files in the specified directory (or current directory if not specified) and returns the value at the specified path.

The command supports hierarchical key paths like `server.database.host` and provides type-safe value retrieval with proper error handling.

## Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| --help | -h | Show help for this command | - |
| --json | | Output in JSON format | false |
| --quiet | -q | Suppress non-error output | false |
| --verbose | | Enable verbose output | false |

## Arguments

| Argument | Required | Description |
|----------|----------|-------------|
| key.path | Yes | Dot-separated path to configuration value (e.g., "server.port") |
| dir | No | Directory to search for configuration files (default: current directory) |

## Examples

### Basic Usage

```bash
# Get application name
tusk-rust config get app.name

# Get server port
tusk-rust config get server.port

# Get nested database configuration
tusk-rust config get database.connection.host
```

### Advanced Usage

```bash
# Get value from specific directory
tusk-rust config get app.name /path/to/config

# Get value with JSON output
tusk-rust config get server.port --json

# Get value with verbose output
tusk-rust config get database.url --verbose
```

### Configuration File Examples

**peanu.peanuts:**
```ini
[app]
name: "My Rust App"
version: "1.0.0"

[server]
host: "localhost"
port: 8080

[database]
connection:
  host: "db.example.com"
  port: 5432
  name: "myapp"
```

**Usage:**
```bash
$ tusk-rust config get app.name
✅ "My Rust App"

$ tusk-rust config get server.port
✅ 8080

$ tusk-rust config get database.connection.host
✅ "db.example.com"
```

## Rust API Usage

```rust
use tusklang_rust::peanut::{PeanutConfig, ConfigError};

// Basic usage
fn get_config_value() -> Result<(), ConfigError> {
    let config = PeanutConfig::new().load(".")?;
    
    let app_name: String = config.get("app.name")?;
    let port: u16 = config.get("server.port")?;
    
    println!("App: {}", app_name);
    println!("Port: {}", port);
    
    Ok(())
}

// With error handling
fn get_config_with_fallback() -> Result<(), ConfigError> {
    let config = PeanutConfig::new().load(".")?;
    
    // Get with default value
    let timeout = config.get_or("server.timeout", 30u64)?;
    let host = config.get_or("server.host", "localhost")?;
    
    println!("Timeout: {}", timeout);
    println!("Host: {}", host);
    
    Ok(())
}

// Type-safe struct deserialization
#[derive(Debug, Deserialize)]
struct ServerConfig {
    host: String,
    port: u16,
    timeout: u64,
}

fn get_server_config() -> Result<(), ConfigError> {
    let config = PeanutConfig::new().load(".")?;
    
    let server_config: ServerConfig = config.deserialize_key("server")?;
    
    println!("Server: {:?}", server_config);
    Ok(())
}
```

## Output

### Success Output

```
✅ <value>
```

### Error Output

```
❌ Key 'server.port' not found in configuration
❌ No configuration file found in '/path/to/config'
❌ Parse error: Invalid syntax at line 5
```

### JSON Output

When using `--json` flag:

```json
{
  "success": true,
  "value": 8080,
  "type": "number",
  "key": "server.port"
}
```

## Exit Codes

| Code | Description |
|------|-------------|
| 0 | Success - value retrieved |
| 1 | General error |
| 2 | Invalid arguments |
| 3 | File not found |
| 6 | Configuration error |

## Related Commands

- [tsk config check](./check.md) - Check configuration hierarchy
- [tsk config validate](./validate.md) - Validate configuration
- [tsk utility get](../utility/get.md) - Get value from specific file

## Notes

### Key Path Syntax

- Use dots (`.`) to separate nested keys
- Keys are case-sensitive
- Array indices are not supported (use `config get` for arrays)
- Environment variable substitution is supported in `.tsk` files

### Configuration File Priority

The command searches for configuration files in this order:
1. `peanu.pnt` (binary format)
2. `peanu.tsk` (TuskLang syntax)
3. `peanu.peanuts` (INI-like syntax)

### Type Inference

The command automatically infers the correct type for the value:
- Strings are returned as-is
- Numbers are parsed as appropriate numeric types
- Booleans are parsed as `true`/`false`
- Arrays and objects are returned as JSON

### Performance

- Binary `.pnt` files load 85% faster than text formats
- Values are cached after first access
- Type checking is performed at runtime

## See Also

- [Configuration Overview](./README.md)
- [Peanut Configuration Guide](../../../rust/docs/PNT_GUIDE.md)
- [Examples](../../examples/basic-usage.md) 