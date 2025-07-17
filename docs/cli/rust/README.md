# TuskLang Rust CLI Documentation

Welcome to the comprehensive CLI documentation for TuskLang Rust SDK.

## Quick Links

- [Installation](./installation.md)
- [Quick Start](./quickstart.md)
- [Command Reference](./commands/README.md)
- [Examples](./examples/README.md)
- [Troubleshooting](./troubleshooting.md)

## Command Categories

- [Database Commands](./commands/db/README.md) - Database operations
- [Development Commands](./commands/dev/README.md) - Development tools
- [Testing Commands](./commands/test/README.md) - Test execution
- [Service Commands](./commands/services/README.md) - Service management
- [Cache Commands](./commands/cache/README.md) - Cache operations
- [Configuration Commands](./commands/config/README.md) - Config management
- [Binary Commands](./commands/binary/README.md) - Binary compilation
- [AI Commands](./commands/ai/README.md) - AI integrations
- [Utility Commands](./commands/utility/README.md) - General utilities

## Version

This documentation is for TuskLang Rust SDK v0.1.0

## Installation

```bash
# Install from crates.io
cargo install tusklang-rust

# Or build from source
git clone https://github.com/tusklang/tusklang-rust.git
cd tusklang-rust
cargo build --release
cargo install --path .
```

## Quick Start

```bash
# Check if installation was successful
tusk-rust --version

# Get help
tusk-rust --help

# Interactive mode
tusk-rust

# Parse a configuration file
tusk-rust utility parse config.tsk

# Validate configuration
tusk-rust utility validate config.tsk

# Get configuration value
tusk-rust config get app.name
```

## Rust-Specific Features

- **Zero-copy parsing** for maximum performance
- **Type-safe configuration** with Serde integration
- **Async/await support** for non-blocking operations
- **Memory-efficient** with minimal allocations
- **Cross-platform** compatibility (Linux, macOS, Windows)

## Global Options

All commands support these global options:

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| --help | -h | Show help for this command | - |
| --version | -v | Show version information | - |
| --verbose | | Enable verbose output | false |
| --quiet | -q | Suppress non-error output | false |
| --config | | Use alternate config file | - |
| --json | | Output in JSON format | false |

## Exit Codes

| Code | Description |
|------|-------------|
| 0 | Success |
| 1 | General error |
| 2 | Invalid arguments |
| 3 | File not found |
| 4 | Permission denied |
| 5 | Connection error |
| 6 | Configuration error |
| 7 | License error |

## Examples

### Basic Configuration Management

```bash
# Parse and validate configuration
tusk-rust utility parse peanu.tsk
tusk-rust utility validate peanu.tsk

# Get specific configuration values
tusk-rust config get server.port
tusk-rust config get database.url

# Compile to binary format
tusk-rust dev compile peanu.tsk
tusk-rust binary compile peanu.tsk
```

### Development Workflow

```bash
# Start development server
tusk-rust dev serve 3000

# Compile and optimize configuration
tusk-rust dev compile config.tsk
tusk-rust dev optimize config.tsk

# Run tests
tusk-rust test parser
tusk-rust test all
```

### Database Operations

```bash
# Check database status
tusk-rust db status

# Run migrations
tusk-rust db migrate schema.sql

# Backup database
tusk-rust db backup

# Restore from backup
tusk-rust db restore backup.sql
```

### Service Management

```bash
# Check service status
tusk-rust services status

# Start all services
tusk-rust services start

# Stop all services
tusk-rust services stop
```

### Cache Operations

```bash
# Check cache status
tusk-rust cache status

# Clear cache
tusk-rust cache clear

# Warm cache
tusk-rust cache warm
```

### AI Integration

```bash
# Query Claude AI
tusk-rust ai claude "How do I optimize this configuration?"

# Analyze configuration
tusk-rust ai analyze config.tsk

# Get security scan
tusk-rust ai security config.tsk
```

## Interactive Mode

When no command is provided, `tusk-rust` enters interactive mode:

```bash
$ tusk-rust
TuskLang v0.1.0 - Interactive Mode
Type 'help' for commands, 'exit' to quit
tsk> db status
✅ Database connected successfully
tsk> config get app.name
✅ "My Rust App"
tsk> exit
👋 Goodbye!
```

## Configuration Loading

The CLI follows this configuration loading hierarchy:

1. Command-line specified config (`--config`)
2. Current directory `peanu.pnt` or `peanu.tsk`
3. Parent directories (walking up)
4. User home directory `~/.tusklang/config.tsk`
5. System-wide `/etc/tusklang/config.tsk`

## Performance

The Rust CLI is optimized for performance:

- **Zero-copy parsing** using nom parser combinators
- **Minimal allocations** with efficient string handling
- **Binary format support** for 85% faster loading
- **Memory-efficient** configuration management

## Troubleshooting

Common issues and solutions:

### Command Not Found
```bash
# Ensure tusk-rust is in PATH
which tusk-rust

# Reinstall if needed
cargo install --force tusklang-rust
```

### Configuration File Not Found
```bash
# Check current directory
ls -la peanu.*

# Create default configuration
echo '[app]
name: "Default App"
version: "1.0.0"' > peanu.peanuts
```

### Permission Denied
```bash
# Check file permissions
ls -la config/

# Fix permissions
chmod 644 peanu.peanuts
```

## Contributing

To contribute to the Rust CLI:

1. Fork the repository
2. Create a feature branch
3. Add tests for new functionality
4. Ensure all tests pass
5. Submit a pull request

## License

MIT License - see [LICENSE](LICENSE) file for details.

## Support

- [Documentation](https://tusklang.org/docs/rust)
- [GitHub Issues](https://github.com/tusklang/tusklang-rust/issues)
- [Discord Community](https://discord.gg/tusklang) 