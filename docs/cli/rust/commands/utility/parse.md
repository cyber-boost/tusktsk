# tsk utility parse

Parse and display TuskLang file contents with detailed output.

## Synopsis

```bash
tsk utility parse <file>
```

## Description

Parses a TuskLang configuration file and displays its contents in a readable format. The command provides detailed information about the parsed configuration, including structure, types, and any parsing warnings.

This command is useful for debugging configuration files, inspecting their structure, and verifying that they parse correctly.

## Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| --help | -h | Show help for this command | - |
| --format | -f | Output format (tsk, json, yaml) | tsk |
| --pretty | -p | Pretty print output | false |
| --json | | Output in JSON format | false |
| --quiet | -q | Suppress non-error output | false |
| --verbose | | Enable verbose output | false |

## Arguments

| Argument | Required | Description |
|----------|----------|-------------|
| file | Yes | Path to the TuskLang file to parse |

## Examples

### Basic Usage

```bash
# Parse a TuskLang file
tusk-rust utility parse config.tsk

# Parse with pretty printing
tusk-rust utility parse config.tsk --pretty

# Parse and output as JSON
tusk-rust utility parse config.tsk --format json
```

### Advanced Usage

```bash
# Parse with verbose output
tusk-rust utility parse config.tsk --verbose

# Parse and output as YAML
tusk-rust utility parse config.tsk --format yaml --pretty

# Parse with JSON output
tusk-rust utility parse config.tsk --json
```

### File Examples

**config.tsk:**
```tsk
app_name: "My Rust App"
version: "1.0.0"
debug: true

server:
  host: "localhost"
  port: 8080
  timeout: 30s

database:
  url: "postgresql://localhost/myapp"
  pool_size: 10
  ssl: true

features:
  - logging
  - metrics
  - caching
```

**Usage:**
```bash
$ tusk-rust utility parse config.tsk
✅ Successfully parsed 'config.tsk'
📄 File contents:
app_name: "My Rust App"
version: "1.0.0"
debug: true

server:
  host: "localhost"
  port: 8080
  timeout: 30s

database:
  url: "postgresql://localhost/myapp"
  pool_size: 10
  ssl: true

features:
  - logging
  - metrics
  - caching
```

## Rust API Usage

```rust
use tusklang_rust::{parse, serialize, TuskResult};
use std::fs;

// Basic parsing
fn parse_config_file(file_path: &str) -> TuskResult<()> {
    let content = fs::read_to_string(file_path)?;
    let config = parse(&content)?;
    
    println!("✅ Successfully parsed '{}'", file_path);
    println!("📄 File contents:");
    println!("{}", serialize(&config)?);
    
    Ok(())
}

// Parse with error handling
fn parse_with_validation(file_path: &str) -> TuskResult<()> {
    let content = fs::read_to_string(file_path)?;
    
    match parse(&content) {
        Ok(config) => {
            println!("✅ Successfully parsed '{}'", file_path);
            
            // Validate required fields
            if config.get("app_name").is_none() {
                eprintln!("⚠️  Warning: app_name is missing");
            }
            
            if config.get("version").is_none() {
                eprintln!("⚠️  Warning: version is missing");
            }
            
            println!("📄 File contents:");
            println!("{}", serialize(&config)?);
        }
        Err(e) => {
            eprintln!("❌ Parse error: {}", e);
            if let Some(line) = e.line_number() {
                eprintln!("   Error occurred at line {}", line);
            }
            return Err(e);
        }
    }
    
    Ok(())
}

// Parse and convert to different formats
fn parse_and_convert(file_path: &str, format: &str) -> TuskResult<()> {
    let content = fs::read_to_string(file_path)?;
    let config = parse(&content)?;
    
    let output = match format {
        "json" => serde_json::to_string_pretty(&config)?,
        "yaml" => serde_yaml::to_string(&config)?,
        "tsk" => serialize(&config)?,
        _ => return Err(TuskError::ValidationError(
            format!("Unsupported format: {}", format)
        )),
    };
    
    println!("{}", output);
    Ok(())
}

// Parse with type checking
fn parse_with_types(file_path: &str) -> TuskResult<()> {
    let content = fs::read_to_string(file_path)?;
    let config = parse(&content)?;
    
    println!("📊 Configuration Analysis:");
    
    for (key, value) in &config {
        let value_type = match value {
            Value::String(_) => "String",
            Value::Number(_) => "Number",
            Value::Boolean(_) => "Boolean",
            Value::Array(_) => "Array",
            Value::Object(_) => "Object",
            Value::Null => "Null",
        };
        
        println!("  {}: {} ({})", key, value, value_type);
    }
    
    Ok(())
}
```

## Output

### Success Output

```
✅ Successfully parsed 'config.tsk'
📄 File contents:
[formatted configuration content]
```

### Error Output

```
❌ File not found: config.tsk
❌ Parse error: Invalid syntax at line 5
   Error occurred at line 5
```

### JSON Output

When using `--json` flag:

```json
{
  "success": true,
  "file": "config.tsk",
  "content": {
    "app_name": "My Rust App",
    "version": "1.0.0",
    "debug": true,
    "server": {
      "host": "localhost",
      "port": 8080,
      "timeout": 30
    }
  },
  "statistics": {
    "lines": 15,
    "keys": 8,
    "parse_time_ms": 1.2
  }
}
```

### Verbose Output

When using `--verbose` flag:

```
🔍 Parsing file: config.tsk
📏 File size: 245 bytes
⏱️  Parse time: 1.2ms
✅ Successfully parsed 'config.tsk'
📊 Statistics:
  - Lines: 15
  - Keys: 8
  - Nested objects: 2
  - Arrays: 1
📄 File contents:
[formatted configuration content]
```

## Exit Codes

| Code | Description |
|------|-------------|
| 0 | Success - file parsed successfully |
| 1 | General error |
| 2 | Invalid arguments |
| 3 | File not found |
| 6 | Configuration error |

## Related Commands

- [tsk utility validate](./validate.md) - Validate file syntax
- [tsk utility convert](./convert.md) - Convert between formats
- [tsk config get](../config/get.md) - Get specific configuration values

## Notes

### Supported File Formats

- **`.tsk`** - TuskLang syntax (recommended)
- **`.peanuts`** - Simple INI-like syntax
- **`.pnt`** - Binary format (read-only)

### Performance

- **Zero-copy parsing** for minimal memory usage
- **Nom parser combinators** for efficient parsing
- **Lazy evaluation** for large files
- **Parallel processing** for batch operations

### Error Reporting

The command provides detailed error information:
- Line numbers for syntax errors
- Specific error messages
- Suggestions for fixing common issues
- Context around the error location

### Output Formats

- **TSK** - Native TuskLang format (default)
- **JSON** - JSON format for interoperability
- **YAML** - YAML format for readability

## See Also

- [Utility Commands Overview](./README.md)
- [TuskLang Syntax Guide](../../../rust/docs/PNT_GUIDE.md)
- [Examples](../../examples/basic-usage.md) 