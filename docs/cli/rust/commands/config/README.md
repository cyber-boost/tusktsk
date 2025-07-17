# Configuration Commands

Configuration management commands for TuskLang Rust SDK.

## Available Commands

| Command | Description |
|---------|-------------|
| [tsk config get](./get.md) | Get configuration value by key path |
| [tsk config check](./check.md) | Check configuration hierarchy |
| [tsk config validate](./validate.md) | Validate entire configuration chain |
| [tsk config compile](./compile.md) | Auto-compile all peanu.tsk files |
| [tsk config docs](./docs.md) | Generate configuration documentation |
| [tsk config clear-cache](./clear-cache.md) | Clear configuration cache |
| [tsk config stats](./stats.md) | Show configuration performance statistics |

## Common Use Cases

- **Configuration Validation**: Validate configuration files before deployment
- **Value Retrieval**: Get specific configuration values for scripts and automation
- **Documentation Generation**: Generate documentation from configuration schemas
- **Performance Monitoring**: Monitor configuration loading performance
- **Cache Management**: Clear and manage configuration caches

## Rust-Specific Notes

### Type Safety
The Rust implementation provides full type safety for configuration values:

```rust
use tusklang_rust::peanut::PeanutConfig;

let config = PeanutConfig::new().load(".")?;

// Type-safe value retrieval
let port: u16 = config.get("server.port")?;
let host: String = config.get("server.host")?;
let debug: bool = config.get("app.debug")?;
```

### Hierarchical Loading
Configuration follows CSS-like cascading with Rust-specific optimizations:

```rust
let config = PeanutConfig::new()
    .with_search_paths(vec!["config", "etc"])
    .with_env_prefix("APP_")
    .with_auto_compile(true)
    .load(".")?;
```

### Performance Features
- **Binary format support** for 85% faster loading
- **Zero-copy parsing** for minimal memory usage
- **Lazy loading** for large configurations
- **Caching** for frequently accessed values

### Error Handling
Rust's strong error handling ensures robust configuration management:

```rust
use tusklang_rust::peanut::ConfigError;

match config.get("server.port") {
    Ok(port) => println!("Port: {}", port),
    Err(ConfigError::KeyNotFound(key)) => {
        eprintln!("Configuration key not found: {}", key);
    }
    Err(ConfigError::TypeMismatch(key, expected)) => {
        eprintln!("Type mismatch for {}: expected {}", key, expected);
    }
    Err(e) => eprintln!("Configuration error: {}", e),
}
``` 