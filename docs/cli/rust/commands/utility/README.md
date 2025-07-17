# Utility Commands

General utility commands for TuskLang Rust SDK.

## Available Commands

| Command | Description |
|---------|-------------|
| [tsk utility parse](./parse.md) | Parse and display TSK file contents |
| [tsk utility validate](./validate.md) | Validate TSK file syntax |
| [tsk utility convert](./convert.md) | Convert between configuration formats |
| [tsk utility get](./get.md) | Get specific value by key path from file |
| [tsk utility set](./set.md) | Set value by key path in file |

## Common Use Cases

- **File Parsing**: Parse and inspect TuskLang configuration files
- **Syntax Validation**: Validate configuration files before deployment
- **Format Conversion**: Convert between different configuration formats
- **Value Manipulation**: Get and set specific configuration values
- **Debugging**: Inspect configuration files during development

## Rust-Specific Notes

### Zero-Copy Parsing
The Rust implementation provides ultra-fast parsing with minimal memory usage:

```rust
use tusklang_rust::{parse, serialize};

// Zero-copy parsing
let config = parse(&content)?;

// Serialize back to TuskLang format
let output = serialize(&config)?;
```

### Type Safety
All utility commands provide type-safe operations:

```rust
use tusklang_rust::value::Value;

// Type-safe value handling
match value {
    Value::String(s) => println!("String: {}", s),
    Value::Number(n) => println!("Number: {}", n),
    Value::Boolean(b) => println!("Boolean: {}", b),
    Value::Array(arr) => println!("Array: {:?}", arr),
    Value::Object(obj) => println!("Object: {:?}", obj),
    Value::Null => println!("Null value"),
}
```

### Error Handling
Comprehensive error handling with detailed error messages:

```rust
use tusklang_rust::error::TuskError;

match parse(&content) {
    Ok(config) => {
        println!("✅ Successfully parsed configuration");
        // Process configuration
    }
    Err(TuskError::ParseError { line, message }) => {
        eprintln!("❌ Parse error at line {}: {}", line, message);
    }
    Err(TuskError::ValidationError(message)) => {
        eprintln!("❌ Validation error: {}", message);
    }
    Err(e) => {
        eprintln!("❌ Unexpected error: {}", e);
    }
}
```

### Performance Features
- **Nom parser combinators** for efficient parsing
- **Minimal allocations** for memory efficiency
- **Streaming support** for large files
- **Parallel processing** for batch operations

### File Format Support
- **`.tsk`** - TuskLang syntax with advanced features
- **`.peanuts`** - Simple INI-like syntax
- **`.pnt`** - Binary format for maximum performance
- **`.json`** - JSON format for interoperability
- **`.yaml`** - YAML format for human readability 